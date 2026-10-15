using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace backend.Data;

/// <summary>
/// 異動歷程把關：BasicSyslog 只能在 SaveChanges 時記錄，因此不經 SaveChanges 的資料異動
/// （ExecuteUpdate／ExecuteDelete、ExecuteSql 等原生 SQL）一律拒絕執行（fail closed），避免留下無歷程的異動。
/// 例外：系統紀錄表轉存後的批次刪除（BasicSyslogSaveTracker.AllowLogArchiveDelete）、dotnet ef 設計階段（Migration）。
/// 以 Scoped 註冊
/// </summary>
public partial class BasicSyslogCommandGuard(BasicSyslogSaveTracker tracker) : DbCommandInterceptor
{
    // 資料異動關鍵字；排除 [Delete] 等以中括號包住的識別字與 UpdatedAt 等字首相同的名稱
    [GeneratedRegex(@"(?<![\[\w])(INSERT|UPDATE|DELETE|MERGE)(?![\]\w])", RegexOptions.IgnoreCase)]
    private static partial Regex DmlPattern();

    [GeneratedRegex(@"(?<![\[\w])(INSERT|UPDATE|MERGE)(?![\]\w])", RegexOptions.IgnoreCase)]
    private static partial Regex NonDeletePattern();

    [GeneratedRegex(@"^\s*--.*$", RegexOptions.Multiline)]
    private static partial Regex CommentLine();

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        Check(command);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Check(command);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        Check(command);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        Check(command);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        Check(command);
        return result;
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
    {
        Check(command);
        return ValueTask.FromResult(result);
    }

    private void Check(DbCommand command)
    {
        // SaveChanges 中的指令由 BasicSyslogInterceptor 記錄；dotnet ef（Migration）不經應用程式，不在此把關
        if (tracker.IsSaving || EF.IsDesignTime) return;

        var sql = CommentLine().Replace(command.CommandText, string.Empty);
        if (!DmlPattern().IsMatch(sql)) return;
        if (IsAllowedArchiveDelete(sql)) return;

        throw new InvalidOperationException(
            "偵測到不經 SaveChangesAsync 的資料異動（ExecuteUpdate／ExecuteDelete／原生 SQL），不會寫入 BasicSyslog，已拒絕執行；請改為載入實體後以 SaveChangesAsync 存檔");
    }

    /// <summary>轉存範圍內，只允許刪除指定的系統紀錄表</summary>
    private bool IsAllowedArchiveDelete(string sql)
    {
        var table = tracker.ArchiveTable;
        return table != null
            && sql.TrimStart().StartsWith("DELETE", StringComparison.OrdinalIgnoreCase)
            && sql.Contains($"[{table}]", StringComparison.Ordinal)
            && !NonDeletePattern().IsMatch(sql);
    }
}
