# 基本資料表與種子資料

各專案共用的基本資料表（驗證、權限與系統紀錄）定義，為欄位的唯一定義來源；既有專案以專案內的 `Models/` 為準。不會自動載入，以下時機須先讀本檔：

* `new-project` 建立專案（「採用設定摘要」或紅燈詢問時須完整列出此表與種子資料）。
* 修改基本資料表或種子資料時。

## 資料表

| 資料表 | 主要欄位 | 說明 |
| --- | --- | --- |
| `BasicUser` | Id、Username（唯一）、Email（唯一）、PasswordHash、DisplayName、IsActive、CreatedAt、UpdatedAt、PasswordResetTokenHash、PasswordResetExpiresAt | 使用者 |
| `BasicGroup` | Id、Name（唯一）、Description、CreatedAt、UpdatedAt | 群組（角色） |
| `BasicPermission` | Id、Name（唯一）、Description、Module | 權限，以靜態種子建立 |
| `BasicUserGroup` | BasicUserId + BasicGroupId（複合主鍵） | 使用者與群組多對多 |
| `BasicGroupPermission` | BasicGroupId + BasicPermissionId（複合主鍵） | 群組與權限多對多 |
| `BasicLoginErrlog` | Id、Username、CreatedAt；索引 (Username, CreatedAt) | 登入失敗紀錄（鎖定判斷） |
| `BasicPwdlog` | Id、Username、PasswordHash、CreatedAt；索引 (Username, CreatedAt) | 密碼歷程（重複與到期判斷） |
| `BasicChecklogintimeSendMailLog` | Id、Username、Email（寄送時的地址）、CreatedAt（寄送時間）；索引 (Username, CreatedAt) | 密碼即將到期通知寄送紀錄（判斷當日是否已寄送，規則見 `auth-baseline.md`） |
| `BasicExceptionlog` | Id、ExceptionType、MethodName、Message、StackTrace、Username（可為 null）、CreatedAt；索引 (CreatedAt) | 系統錯誤紀錄（超過保留天數轉存 txt 後刪除） |
| `BasicSyslog` | Id、Action（C／U／D）、MethodName、Description（業務說明，可為 null）、BeforeData（JSON 陣列）、AfterData（JSON 陣列）、Username（可為 null）、CreatedAt；索引 (CreatedAt) | 系統異動歷程（寫入前將超過保留天數的紀錄轉存 txt 後刪除） |

* 所有資料表與欄位建立時一併以 `HasComment` 加上描述；基本資料表的描述以範本 `backend/Data/AppDbContext.cs` 為準。
* EF Core 另會自動建立 `__EFMigrationsHistory`。

## 種子資料

* 權限：`basicuser.*`、`basicgroup.*` 共 8 筆。
* 受保護的「系統管理員」群組（`Id = 1`，全部權限）；「訪客」群組（`Id = 2`，全部 `*.read` 權限，含業務功能的檢視權限）。
* 預設管理員（`Id = 1`，系統管理員群組）與預設一般使用者（`Id = 2`，訪客群組），以靜態種子於建表時建立；帳號、Email、初始密碼見 `new-project` 第 2 節第 9、10 項。
* 兩人各一筆初始密碼歷程，建立日期為建立專案當日，首次登入不強制變更密碼（依密碼有效天數正常到期）。
* 一般使用者的初始密碼為已知值，完成回報與專案 CLAUDE.md 待辦須提醒正式上線前停用或變更。
