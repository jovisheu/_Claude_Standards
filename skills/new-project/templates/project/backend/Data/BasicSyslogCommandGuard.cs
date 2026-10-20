using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace backend.Data;

/// <summary>
/// 把關：不在 SaveChanges 中卻含 INSERT／UPDATE／DELETE／MERGE 的 SQL 一律拒絕（fail closed），
/// 涵蓋 ExecuteUpdate／ExecuteDelete、ExecuteSql 等不會寫入異動歷程的方式。
/// 例外：系統紀錄表以 AllowLogArchiveDelete 宣告的批次刪除；dotnet ef（EF.IsDesignTime）。
/// </summary>
public partial class BasicSyslogCommandGuard(BasicSyslogSaveTracker tracker) : DbCommandInterceptor
{
    // 資料異動關鍵字；排除 [Update]、[Delete] 等以中括號包住的識別字與 UpdatedAt 等字首相同的名稱
    [GeneratedRegex(@"(?<![\[\w])(INSERT|UPDATE|DELETE|MERGE)(?![\]\w])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WriteKeywordRegex();

    [GeneratedRegex(@"(?<![\[\w])(INSERT|UPDATE|MERGE)(?![\]\w])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NonDeleteWriteKeywordRegex();

    // SQL 註解行（如 TagWith 產生的 -- 說明）不參與判斷
    [GeneratedRegex(@"^\s*--.*$", RegexOptions.Multiline)]
    private static partial Regex CommentLineRegex();

    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        Check(command);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        Check(command);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        Check(command);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Check(command);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        Check(command);
        return result;
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
    {
        Check(command);
        return ValueTask.FromResult(result);
    }

    private void Check(DbCommand command)
    {
        if (EF.IsDesignTime || tracker.IsSaving)
        {
            return;
        }

        var sql = CommentLineRegex().Replace(command.CommandText, string.Empty);
        if (!WriteKeywordRegex().IsMatch(sql))
        {
            return;
        }

        if (IsAllowedArchiveDelete(sql))
        {
            return;
        }

        throw new InvalidOperationException("資料異動須載入實體後以 SaveChangesAsync 存檔（以寫入異動歷程），不可使用 ExecuteUpdate／ExecuteDelete 或原生 SQL 異動");
    }

    /// <summary>只放行已宣告之系統紀錄表的 DELETE</summary>
    private bool IsAllowedArchiveDelete(string sql)
    {
        var table = tracker.AllowedArchiveDeleteTable;
        if (table is null)
        {
            return false;
        }

        var trimmed = sql.TrimStart();
        return trimmed.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase)
            && !NonDeleteWriteKeywordRegex().IsMatch(sql)
            && sql.Contains($"FROM [{table}]", StringComparison.OrdinalIgnoreCase);
    }
}
