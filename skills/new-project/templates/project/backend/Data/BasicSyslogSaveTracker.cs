namespace backend.Data;

/// <summary>
/// 記錄目前 DbContext 是否正在 SaveChanges 中，供 BasicSyslogCommandGuard 判斷資料異動是否會留下異動歷程。
/// 以 Scoped 註冊，與 AppDbContext、BasicSyslogInterceptor 同一範圍
/// </summary>
public sealed class BasicSyslogSaveTracker
{
    /// <summary>系統紀錄表：不記錄異動歷程，只允許轉存後批次刪除</summary>
    public static readonly IReadOnlySet<string> LogTables =
        new HashSet<string> { "BasicSyslog", "BasicExceptionlog", "BasicLoginErrlog", "BasicPwdlog" };

    // 巢狀存檔（攔截器在存檔完成後再存一次歷程）時以計數判斷
    private int _saveDepth;

    public bool IsSaving => _saveDepth > 0;

    /// <summary>目前允許批次刪除的系統紀錄表（轉存期間）</summary>
    public string? ArchiveTable { get; private set; }

    public void EnterSave() => _saveDepth++;

    public void ExitSave()
    {
        if (_saveDepth > 0) _saveDepth--;
    }

    /// <summary>
    /// 允許在範圍內以 ExecuteDelete 刪除已轉存的系統紀錄；只限系統紀錄表，業務資料表一律不可
    /// </summary>
    public IDisposable AllowLogArchiveDelete(string table)
    {
        if (!LogTables.Contains(table))
        {
            throw new InvalidOperationException($"{table} 不是系統紀錄表，不可略過異動歷程");
        }
        ArchiveTable = table;
        return new ArchiveScope(this);
    }

    private sealed class ArchiveScope(BasicSyslogSaveTracker tracker) : IDisposable
    {
        public void Dispose() => tracker.ArchiveTable = null;
    }
}
