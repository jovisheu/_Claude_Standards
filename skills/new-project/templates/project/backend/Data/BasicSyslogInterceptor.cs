using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using backend.Extensions;
using backend.Models;
using backend.Services.BasicSyslogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace backend.Data;

/// <summary>
/// 系統異動歷程：自動記錄每次 SaveChangesAsync 的新增／修改／刪除，與業務資料在同一交易中寫入 BasicSyslog。
/// 一次存檔一筆歷程：全部為新增記 C、全部為刪除記 D、其餘記 U；異動前後資料為 JSON 陣列 [{ "Table", "Data" }]。
/// 不經 SaveChanges 的異動由 BasicSyslogCommandGuard 拒絕執行，兩者共用 BasicSyslogSaveTracker 判斷存檔狀態。
/// 以 Scoped 註冊（每個 DbContext 一個實例）
/// </summary>
public class BasicSyslogInterceptor(
    IHttpContextAccessor httpContextAccessor, IBasicSyslogsService basicSyslogs, BasicSyslogSaveTracker tracker,
    BasicSyslogContext basicSyslogContext) : SaveChangesInterceptor
{
    /// <summary>
    /// 遮蔽的機密欄位（{資料表}.{欄位}）：照常記錄異動，但值以 *** 取代（null 仍為 null，可看出設定或清除）；
    /// 新增含機密欄位（密碼、token、金鑰等）的資料表時須加入
    /// </summary>
    private static readonly HashSet<string> MaskedProperties =
        ["BasicUser.PasswordHash", "BasicUser.PasswordResetTokenHash", "BasicUser.PasswordResetExpiresAt"];

    private const string MaskedValue = "***";

    // 異動後與異動前不同時加註，讓歷程查詢能判斷遮蔽欄位有變更
    private const string MaskedChangedValue = "***（已變更）";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        Converters = { new LocalDateTimeConverter() }
    };

    private PendingSyslog? _pending;
    private IDbContextTransaction? _transaction;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        // 同步存檔無法在同一交易中補寫歷程，有需記錄的異動時一律使用 SaveChangesAsync
        if (eventData.Context != null && GetAuditableEntries(eventData.Context).Count > 0)
        {
            throw new InvalidOperationException("有需記錄異動歷程的資料時，須使用 SaveChangesAsync");
        }
        tracker.EnterSave();
        return result;
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        tracker.ExitSave();
        return result;
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData) => tracker.ExitSave();

    public override void SaveChangesCanceled(DbContextEventData eventData) => tracker.ExitSave();

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        tracker.EnterSave();
        try
        {
            var db = eventData.Context;
            if (db == null) return result;

            var entries = GetAuditableEntries(db);
            if (entries.Count == 0) return result;

            // 寫入歷程前，先將超過保留天數的舊紀錄轉存為 txt
            await basicSyslogs.ArchiveIfDueAsync();

            var action = entries.All(e => e.State == EntityState.Added) ? "C"
                : entries.All(e => e.State == EntityState.Deleted) ? "D"
                : "U";
            var before = entries.Where(e => e.State != EntityState.Added).Select(e => Snapshot(e, original: true, [])).ToList();
            // 存檔後修改狀態會被清除，先記下有變更的遮蔽欄位
            var changedMasked = entries
                .SelectMany(e => e.Properties.Where(p => p.IsModified && IsMasked(e, p)).Select(p => (e.Entity, p.Metadata.Name)))
                .ToHashSet();
            // 有需記錄的異動時才取出說明；只寫入系統紀錄表的存檔不會用掉 Describe 指定的說明
            _pending = new PendingSyslog(
                action, before, entries.Where(e => e.State != EntityState.Deleted).ToList(), changedMasked, basicSyslogContext.Take());

            // 業務資料與歷程在同一交易；呼叫端已開啟交易時沿用，由呼叫端提交
            if (db.Database.CurrentTransaction == null)
            {
                _transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            }
            return result;
        }
        catch
        {
            // SavingChanges 丟出例外時 EF Core 不會呼叫 SaveChangesFailed，於此結束存檔狀態
            tracker.ExitSave();
            throw;
        }
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = eventData.Context;
            var pending = _pending;
            if (db == null || pending == null) return result;
            _pending = null;

            try
            {
                var httpContext = httpContextAccessor.HttpContext;
                var username = httpContext?.User.Identity?.Name;
                db.Set<BasicSyslog>().Add(new BasicSyslog
                {
                    Action = pending.Action,
                    MethodName = httpContext.GetMethodName(),
                    // 說明：Service 指定 > 端點預設 > null
                    Description = pending.Description
                        ?? httpContext?.GetEndpoint()?.Metadata.GetMetadata<BasicSyslogDescriptionAttribute>()?.Description,
                    BeforeData = JsonSerializer.Serialize(pending.Before, JsonOptions),
                    // 存檔後才取得新增資料的 Id，異動後資料於此時產生
                    AfterData = JsonSerializer.Serialize(
                        pending.AfterEntries.Select(e => Snapshot(e, original: false, pending.ChangedMasked)), JsonOptions),
                    Username = string.IsNullOrEmpty(username) ? null : username,
                    CreatedAt = DateTime.Now
                });
                await db.SaveChangesAsync(cancellationToken);

                if (_transaction != null)
                {
                    await _transaction.CommitAsync(cancellationToken);
                }
            }
            catch
            {
                // 歷程寫入失敗時業務資料一併還原，不留下無歷程的異動
                if (_transaction != null)
                {
                    await _transaction.RollbackAsync(CancellationToken.None);
                }
                throw;
            }
            finally
            {
                await DisposeTransactionAsync();
            }

            return result;
        }
        finally
        {
            // 歷程本身的存檔（巢狀）完成後才結束外層存檔狀態
            tracker.ExitSave();
        }
    }

    public override async Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        tracker.ExitSave();
        // 業務資料存檔失敗：未提交的交易於 Dispose 時還原
        if (_pending == null) return;
        _pending = null;
        await DisposeTransactionAsync();
    }

    public override async Task SaveChangesCanceledAsync(DbContextEventData eventData, CancellationToken cancellationToken = default)
    {
        tracker.ExitSave();
        if (_pending == null) return;
        _pending = null;
        await DisposeTransactionAsync();
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction == null) return;
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    /// <summary>系統紀錄表以外的新增、刪除，以及有任何欄位變更的修改（含只改機密欄位）</summary>
    private static List<EntityEntry> GetAuditableEntries(DbContext db) =>
        db.ChangeTracker.Entries()
            .Where(e => !BasicSyslogSaveTracker.LogTables.Contains(GetTableName(e)))
            .Where(e => e.State is EntityState.Added or EntityState.Deleted
                || (e.State == EntityState.Modified && e.Properties.Any(p => p.IsModified)))
            .ToList();

    private static bool IsMasked(EntityEntry entry, PropertyEntry property) =>
        MaskedProperties.Contains($"{GetTableName(entry)}.{property.Metadata.Name}");

    private static string GetTableName(EntityEntry entry) => entry.Metadata.GetTableName() ?? entry.Metadata.ClrType.Name;

    private static Dictionary<string, object?> Snapshot(EntityEntry entry, bool original, HashSet<(object, string)> changedMasked)
    {
        var data = new Dictionary<string, object?>();
        foreach (var property in entry.Properties)
        {
            var value = original ? property.OriginalValue : property.CurrentValue;
            if (IsMasked(entry, property) && value != null)
            {
                value = !original && changedMasked.Contains((entry.Entity, property.Metadata.Name)) ? MaskedChangedValue : MaskedValue;
            }
            data[property.Metadata.Name] = value;
        }
        return new Dictionary<string, object?> { ["Table"] = GetTableName(entry), ["Data"] = data };
    }

    /// <summary>
    /// 時間一律以本地時間格式輸出：新增時的值帶時區（Local）、自資料庫讀出的值不帶（Unspecified），統一格式避免前後不一致
    /// </summary>
    private sealed class LocalDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.GetDateTime();

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString("yyyy-MM-dd HH:mm:ss.fff"));
    }

    private sealed record PendingSyslog(
        string Action, List<Dictionary<string, object?>> Before, List<EntityEntry> AfterEntries,
        HashSet<(object, string)> ChangedMasked, string? Description);
}
