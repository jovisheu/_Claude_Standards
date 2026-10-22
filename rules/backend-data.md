---
paths:
  - "**/backend/**/*.cs"
  - "**/*.sql"
---

# 後端資料層規範（_Claude_Standards）

通用規範（`D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\CLAUDE.md`）的一部分，處理後端程式或 SQL 時載入。

## 異動歷程（BasicSyslog）的寫入規則

系統紀錄表（`BasicSyslog`、`BasicExceptionlog`、`BasicLoginErrlog`、`BasicPwdlog`、`BasicChecklogintimeSendMailLog`）以外的**所有**新增、修改、刪除都須寫入 `BasicSyslog`，由 `Data/BasicSyslogInterceptor.cs` 自動寫入，新功能不需另外撰寫，但須遵守：

* 異動一律載入實體後以 `SaveChangesAsync` 存檔；不可用同步 `SaveChanges`、`ExecuteUpdate`／`ExecuteDelete`、`ExecuteSql` 等原生 SQL 異動。`Data/BasicSyslogCommandGuard.cs` 會在執行時拒絕不經 `SaveChanges` 的異動（fail closed），唯一例外是系統紀錄表轉存後以 `AllowLogArchiveDelete` 宣告的批次刪除。
* DbContext 一律由 DI 取得，不可自行 `new AppDbContext(...)`（不會掛上攔截器）。
* 只改機密欄位的異動同樣記錄；新增含機密欄位（密碼雜湊、token、金鑰）的資料表時，將欄位加入攔截器的 `MaskedProperties`，歷程中的值以 `***` 遮蔽。
* 業務說明 `Description`（選填）：寫入端點標上 `[BasicSyslogDescription("編輯公告")]` 作為預設說明；需依結果區分時（審核通過／退回、批次匯入、系統自動處理），在 `SaveChangesAsync` 前呼叫 `BasicSyslogContext.Describe("...")`，優先於端點預設。說明會顯示於歷程畫面並轉存 txt，不可含密碼、token 等機密或不必要的個資，也不直接放入使用者輸入的長文。
* 程式以外的修改（SSMS、部署腳本、Migration 種子）不在攔截範圍，正式環境不直接修改資料，資料修正經由系統功能或 Migration。
* 各功能提供 `GET /api/{功能}/{id}/basicsyslogs`（Policy 為該資料表的 `update` 權限），呼叫 `IBasicSyslogHistoryService.GetRecordHistoryAsync(主表, id, 關聯表...)`。

## 資料表規則

* 權限一律指派給群組，不直接指派給使用者。「系統管理員」群組擁有全部權限且受保護；「訪客」群組擁有全部檢視（`.read`）權限。
* 建立資料表或新增欄位時，**必須同時**建立資料表描述與每個欄位的描述，不得事後補上：
  * EF Core：在 `AppDbContext` 以 `HasComment` 加上繁體中文描述（資料表為 `ToTable("名稱", t => t.HasComment("描述"))`），隨 Migration 寫入 `MS_Description`。
  * 原生 SQL 建表（SSMS、部署腳本）：同一腳本內以 `sp_addextendedproperty` 寫入 `MS_Description`（資料表與每個欄位）。
  * 建表或加欄位後，以唯讀查詢 `sys.extended_properties` 確認都有 `MS_Description`。
  * 描述只說明欄位本身的意義（自動編號主鍵為「流水號」、外鍵註明指向的資料表欄位，如「使用者編號（BasicUser.Id）」），不寫特定資料的值或程式約定。`__EFMigrationsHistory` 不加描述。
* 業務資料表與可在系統中新增、修改的基本資料表（`BasicUser`、`BasicGroup`）一律含 `CreatedAt`（建立時間，不可為 null）與 `UpdatedAt`（最後修改時間，可為 null）。
  * 新增時由 Service 設 `CreatedAt = DateTime.Now`、`UpdatedAt` 為 null；修改時一律設 `UpdatedAt = DateTime.Now`（含只改關聯表的修改，異動歷程查詢依此找出該筆資料）。
  * 列表 DTO 一併回傳 `CreatedAt`、`UpdatedAt`，供前端顯示異動時間。
  * 只異動機密欄位時（如申請忘記密碼寫入重設 token 雜湊）不更新 `UpdatedAt`（屬系統動作，非資料內容的修改；異動歷程仍會記錄）。
  * 不適用：系統紀錄表（只新增，僅 `CreatedAt`）、關聯表（只有複合主鍵）、以靜態種子建立且不提供異動功能的 `BasicPermission`。
* SQL Server 上可為 null 的欄位不建唯一索引，改建一般索引：EF Core 會建成篩選索引，以 sqlcmd 等預設 `QUOTED_IDENTIFIER OFF` 的工具更新該表時會失敗。

## Service 層寫法

* 新增或修改前先檢查唯一值（如帳號、Email、名稱），重複時丟 `BusinessException`，不依賴資料庫例外。
* 關聯 ID 寫入前先去除重複並確認存在。
* 主表與關聯表透過導覽屬性一起加入，**只呼叫一次 `SaveChangesAsync`**，讓 EF Core 包在同一個交易中。
* 異動資料的方法接收 `operatorId`（Controller 傳入目前登入者），用於資料層級的權限檢查。
