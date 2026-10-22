namespace backend.Data;

/// <summary>
/// 記錄目前是否位於 SaveChanges 中（Scoped，與 AppDbContext 同一範圍），供 BasicSyslogCommandGuard 判斷 SQL 是否經由 SaveChanges 送出；
/// 系統紀錄表轉存後的批次刪除須以 AllowLogArchiveDelete 宣告
/// </summary>
public class BasicSyslogSaveTracker
{
    /// <summary>系統紀錄表：不寫入異動歷程，且只有這些表可宣告批次刪除</summary>
    public static readonly IReadOnlySet<string> LogTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        nameof(Models.BasicSyslog),
        nameof(Models.BasicExceptionlog),
        nameof(Models.BasicLoginErrlog),
        nameof(Models.BasicPwdlog),
        nameof(Models.BasicChecklogintimeSendMailLog),
    };

    private int _saveDepth;

    /// <summary>是否位於 SaveChanges 中（含巢狀存檔）</summary>
    public bool IsSaving => _saveDepth > 0;

    /// <summary>目前允許批次刪除的系統紀錄表（null 表示不允許）</summary>
    public string? AllowedArchiveDeleteTable { get; private set; }

    public void EnterSave() => _saveDepth++;

    public void ExitSave()
    {
        if (_saveDepth > 0)
        {
            _saveDepth--;
        }
    }

    /// <summary>宣告於範圍內允許指定系統紀錄表的批次刪除（轉存後使用）；傳入業務資料表會丟出例外</summary>
    public IDisposable AllowLogArchiveDelete(string tableName)
    {
        if (!LogTables.Contains(tableName))
        {
            throw new InvalidOperationException($"只有系統紀錄表可以批次刪除：{tableName}");
        }

        if (AllowedArchiveDeleteTable is not null)
        {
            throw new InvalidOperationException("不可巢狀宣告批次刪除");
        }

        AllowedArchiveDeleteTable = tableName;
        return new Scope(this);
    }

    private sealed class Scope(BasicSyslogSaveTracker tracker) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            tracker.AllowedArchiveDeleteTable = null;
            _disposed = true;
        }
    }
}
