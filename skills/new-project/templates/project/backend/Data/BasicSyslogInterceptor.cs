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
/// 以 Scoped 註冊（每個 DbContext 一個實例）
/// </summary>
public class BasicSyslogInterceptor(IHttpContextAccessor httpContextAccessor, IBasicSyslogsService basicSyslogs) : SaveChangesInterceptor
{
    /// <summary>不記錄的資料表（系統紀錄表）</summary>
    private static readonly HashSet<Type> IgnoredEntityTypes =
        [typeof(BasicSyslog), typeof(BasicExceptionlog), typeof(BasicLoginErrlog), typeof(BasicPwdlog)];

    /// <summary>
    /// 不寫入歷程的機密欄位（{資料表}.{欄位}）；新增含機密欄位（密碼、token 等）的資料表時須加入。
    /// 只異動這些欄位時（如申請忘記密碼）不記錄
    /// </summary>
    private static readonly HashSet<string> IgnoredProperties =
        ["BasicUser.PasswordHash", "BasicUser.PasswordResetTokenHash", "BasicUser.PasswordResetExpiresAt"];

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
        return result;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
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
        var before = entries.Where(e => e.State != EntityState.Added).Select(e => Snapshot(e, original: true)).ToList();
        _pending = new PendingSyslog(action, before, entries.Where(e => e.State != EntityState.Deleted).ToList());

        // 業務資料與歷程在同一交易；呼叫端已開啟交易時沿用，由呼叫端提交
        if (db.Database.CurrentTransaction == null)
        {
            _transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        }
        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
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
                BeforeData = JsonSerializer.Serialize(pending.Before, JsonOptions),
                // 存檔後才取得新增資料的 Id，異動後資料於此時產生
                AfterData = JsonSerializer.Serialize(pending.AfterEntries.Select(e => Snapshot(e, original: false)), JsonOptions),
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

    public override async Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        // 業務資料存檔失敗：未提交的交易於 Dispose 時還原
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

    private static List<EntityEntry> GetAuditableEntries(DbContext db) =>
        db.ChangeTracker.Entries()
            .Where(e => !IgnoredEntityTypes.Contains(e.Metadata.ClrType))
            .Where(e => e.State is EntityState.Added or EntityState.Deleted
                || (e.State == EntityState.Modified && e.Properties.Any(p => p.IsModified && !IsIgnored(e, p))))
            .ToList();

    private static bool IsIgnored(EntityEntry entry, PropertyEntry property) =>
        IgnoredProperties.Contains($"{GetTableName(entry)}.{property.Metadata.Name}");

    private static string GetTableName(EntityEntry entry) => entry.Metadata.GetTableName() ?? entry.Metadata.ClrType.Name;

    private static Dictionary<string, object?> Snapshot(EntityEntry entry, bool original)
    {
        var data = new Dictionary<string, object?>();
        foreach (var property in entry.Properties)
        {
            if (IsIgnored(entry, property)) continue;
            data[property.Metadata.Name] = original ? property.OriginalValue : property.CurrentValue;
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

    private sealed record PendingSyslog(string Action, List<Dictionary<string, object?>> Before, List<EntityEntry> AfterEntries);
}
