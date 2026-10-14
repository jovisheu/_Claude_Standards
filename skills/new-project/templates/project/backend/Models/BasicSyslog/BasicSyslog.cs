namespace backend.Models;

/// <summary>
/// 系統異動歷程（資料表 BasicSyslog；由 BasicSyslogInterceptor 自動寫入，超過保留天數於下次寫入前轉存為 txt 後刪除）
/// </summary>
public class BasicSyslog
{
    public long Id { get; set; }

    /// <summary>類別：C 新增、U 修改、D 刪除</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>異動來源（如 BasicUsersController.Update），同時作為轉存的檔名</summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>異動前資料（JSON 陣列，新增時為 []；不含機密欄位）</summary>
    public string BeforeData { get; set; } = "[]";

    /// <summary>異動後資料（JSON 陣列，刪除時為 []；不含機密欄位）</summary>
    public string AfterData { get; set; } = "[]";

    /// <summary>登入者帳號；未登入（如重設密碼）為 null</summary>
    public string? Username { get; set; }

    public DateTime CreatedAt { get; set; }
}
