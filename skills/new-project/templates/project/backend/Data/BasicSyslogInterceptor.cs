using System.Text.Encodings.Web;
using System.Text.Json;
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
/// 系統紀錄表以外的新增、修改、刪除自動寫入 BasicSyslog（一次存檔一筆），與業務資料在同一交易；
/// 同步 SaveChanges 遇到需記錄的異動會丟出例外。以 Scoped 註冊並掛在 AppDbContext 上。
/// </summary>
public class BasicSyslogInterceptor(
    BasicSyslogSaveTracker tracker,
    BasicSyslogContext syslogContext,
    IHttpContextAccessor httpContextAccessor,
    IBasicSyslogsService basicSyslogsService) : SaveChangesInterceptor
{
    /// <summary>機密欄位（{資料表}.{欄位}），歷程中的值以 *** 遮蔽</summary>
    public static readonly IReadOnlySet<string> MaskedProperties = new HashSet<string>(StringComparer.Ordinal)
    {
        $"{nameof(BasicUser)}.{nameof(BasicUser.PasswordHash)}",
        $"{nameof(BasicUser)}.{nameof(BasicUser.PasswordResetTokenHash)}",
    };

    private const string MaskedValue = "***";
    private const string MaskedChangedValue = "***（已變更）";
    private const string TimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
    private const int MethodNameMaxLength = 200;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    private List<CapturedEntry>? _pending;
    private IDbContextTransaction? _ownedTransaction;
    private bool _writingLog;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null && Capture(eventData.Context).Count > 0)
        {
            throw new InvalidOperationException("需記錄異動歷程的存檔須使用 SaveChangesAsync");
        }

        tracker.EnterSave();
        return result;
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        tracker.ExitSave();
        return result;
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        tracker.ExitSave();
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        tracker.EnterSave();
        try
        {
            var context = eventData.Context;
            if (context is null || _writingLog)
            {
                return result;
            }

            var entries = Capture(context);
            if (entries.Count == 0)
            {
                _pending = null;
                return result;
            }

            // 超過保留天數的歷程先轉存（失敗不影響本次異動；不傳入請求的取消權杖，避免寫檔後、刪除前被中斷而重複轉存）
            await basicSyslogsService.ArchiveIfDueAsync();

            if (context.Database.CurrentTransaction is null)
            {
                _ownedTransaction = await context.Database.BeginTransactionAsync(cancellationToken);
            }

            _pending = entries;
            return result;
        }
        catch
        {
            // SavingChanges 丟出例外時 EF Core 不會呼叫 SaveChangesFailed，於此還原計數與交易
            tracker.ExitSave();
            await RollbackAsync();
            throw;
        }
    }

    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null || _writingLog || _pending is null)
        {
            tracker.ExitSave();
            return result;
        }

        var entries = _pending;
        _pending = null;

        var log = BuildLog(entries);
        _writingLog = true;
        try
        {
            context.Set<BasicSyslog>().Add(log);
            await context.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _writingLog = false;
        }

        // 歷程寫入失敗時例外會往外拋，由 SaveChangesFailedAsync 還原交易
        if (_ownedTransaction is not null)
        {
            await _ownedTransaction.CommitAsync(cancellationToken);
            await _ownedTransaction.DisposeAsync();
            _ownedTransaction = null;
        }

        tracker.ExitSave();
        return result;
    }

    // 取消存檔（OperationCanceledException）時 EF Core 只呼叫 Canceled、不呼叫 Failed，須同樣還原計數與交易，
    // 否則存檔計數會卡住，BasicSyslogCommandGuard 在此範圍內會全部放行
    public override void SaveChangesCanceled(DbContextEventData eventData)
    {
        tracker.ExitSave();
        if (_writingLog)
        {
            return;
        }

        _pending = null;
        if (_ownedTransaction is not null)
        {
            try
            {
                _ownedTransaction.Rollback();
            }
            finally
            {
                _ownedTransaction.Dispose();
                _ownedTransaction = null;
            }
        }
    }

    public override async Task SaveChangesCanceledAsync(DbContextEventData eventData, CancellationToken cancellationToken = default)
    {
        tracker.ExitSave();
        if (_writingLog)
        {
            return;
        }

        _pending = null;
        await RollbackAsync();
    }

    public override async Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        tracker.ExitSave();
        if (_writingLog)
        {
            // 歷程本身寫入失敗：由外層存檔的失敗處理還原交易
            return;
        }

        _pending = null;
        await RollbackAsync();
    }

    private async Task RollbackAsync()
    {
        if (_ownedTransaction is null)
        {
            return;
        }

        try
        {
            await _ownedTransaction.RollbackAsync();
        }
        finally
        {
            await _ownedTransaction.DisposeAsync();
            _ownedTransaction = null;
        }
    }

    /// <summary>擷取需記錄的異動與異動前資料（系統紀錄表除外）</summary>
    private static List<CapturedEntry> Capture(DbContext context)
    {
        // EF Core 於攔截器之後才偵測變更，先自行偵測
        context.ChangeTracker.DetectChanges();

        var list = new List<CapturedEntry>();
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var table = entry.Metadata.GetTableName() ?? entry.Metadata.ClrType.Name;
            if (BasicSyslogSaveTracker.LogTables.Contains(table))
            {
                continue;
            }

            var before = entry.State == EntityState.Added ? null : Snapshot(entry, table, useOriginal: true, changedMasked: null);
            var changedMasked = entry.Properties
                .Where(p => IsMasked(table, p.Metadata.Name) && (entry.State == EntityState.Added || p.IsModified))
                .Select(p => p.Metadata.Name)
                .ToHashSet(StringComparer.Ordinal);

            list.Add(new CapturedEntry(entry, entry.State, table, before, changedMasked));
        }

        return list;
    }

    private BasicSyslog BuildLog(List<CapturedEntry> entries)
    {
        var beforeData = new List<object>();
        var afterData = new List<object>();
        foreach (var captured in entries)
        {
            if (captured.Before is not null)
            {
                beforeData.Add(new { Table = captured.Table, Data = captured.Before });
            }

            if (captured.State != EntityState.Deleted)
            {
                // 新增的資料於存檔後才有 Id，存檔後再取值
                afterData.Add(new { Table = captured.Table, Data = Snapshot(captured.Entry, captured.Table, useOriginal: false, captured.ChangedMasked) });
            }
        }

        var action = entries.All(e => e.State == EntityState.Added) ? "C"
            : entries.All(e => e.State == EntityState.Deleted) ? "D"
            : "U";

        var httpContext = httpContextAccessor.HttpContext;
        var description = syslogContext.Take()
            ?? httpContext?.GetEndpoint()?.Metadata.GetMetadata<BasicSyslogDescriptionAttribute>()?.Description;

        // MethodName 欄位上限 200，超過會使歷程寫入失敗並連帶還原業務資料
        var methodName = httpContext?.GetMethodName() ?? "System";
        return new BasicSyslog
        {
            Action = action,
            MethodName = methodName.Length > MethodNameMaxLength ? methodName[..MethodNameMaxLength] : methodName,
            Description = description,
            BeforeData = JsonSerializer.Serialize(beforeData, JsonOptions),
            AfterData = JsonSerializer.Serialize(afterData, JsonOptions),
            Username = httpContext?.GetUsername(),
            CreatedAt = DateTime.Now,
        };
    }

    private static Dictionary<string, object?> Snapshot(EntityEntry entry, string table, bool useOriginal, HashSet<string>? changedMasked)
    {
        var data = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;
            var value = useOriginal ? property.OriginalValue : property.CurrentValue;
            if (IsMasked(table, name))
            {
                data[name] = value is null ? null
                    : changedMasked is not null && changedMasked.Contains(name) ? MaskedChangedValue
                    : MaskedValue;
                continue;
            }

            data[name] = value switch
            {
                DateTime dateTime => dateTime.ToString(TimeFormat),
                _ => value,
            };
        }

        return data;
    }

    private static bool IsMasked(string table, string property) => MaskedProperties.Contains($"{table}.{property}");

    private sealed record CapturedEntry(
        EntityEntry Entry,
        EntityState State,
        string Table,
        Dictionary<string, object?>? Before,
        HashSet<string> ChangedMasked);
}
