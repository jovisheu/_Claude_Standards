# 系統錯誤紀錄與異動歷程實作規範

自 `CLAUDE.md`「記錄與警示失效」移出的實作細節，不會自動載入。範本已實作，一般業務功能只需遵守 `CLAUDE.md` 的原則。以下時機須先讀本檔：

* `new-project` 不使用範本、重新產生 `BasicExceptionlog`、`BasicSyslog` 相關程式時。
* 修改全域例外處理、`BasicSyslogInterceptor`、歷程查詢或轉存機制時。
* `owasp-check` 檢查「記錄與警示失效」類別時。

## 系統錯誤紀錄（BasicExceptionlog）

* 後端錯誤（全域 Middleware 擷取的未處理例外、背景作業失敗）除寫入 `ILogger` 外，一律寫入 `BasicExceptionlog`：例外類別、MethodName（Controller 與方法名稱，如 `BasicUsersController.Create`）、錯誤訊息、StackTrace、登入者帳號（未登入或背景作業為 null）、時間；`BusinessException` 不記錄。
* 統一呼叫 `IBasicExceptionlogsService.WriteAsync`：另建 DbContext 寫入（不沿用請求中可能含失敗變更的 DbContext），寫入失敗只寫 `ILogger`、不再丟出例外；例外訊息可能含個資時（如郵件寄送）以參數取代訊息。
* 保留天數為 `appsettings.json` 的 `BasicExceptionlog:RetentionDays`（預設 30）。背景服務於啟動時與之後每天一次，將 `CreatedAt` 早於「今天 - 保留天數」的紀錄轉存為 `網站目錄\Logs\Exception\YYYY\MM\DD\{MethodName}.txt`（日期為錯誤發生日，同檔附加），寫檔成功才刪除資料庫紀錄；MethodName 作為檔名前須移除不合法字元。

## 系統異動歷程（BasicSyslog）

* 後端新增、修改、刪除資料（管理功能、業務功能、逾期變更與重設密碼）一律寫入 `BasicSyslog`：類別（`C`／`U`／`D`）、MethodName、業務說明（選填）、異動前資料（JSON 陣列）、異動後資料（JSON 陣列）、登入者帳號（未登入為 null）、時間。
* 業務說明 `Description`（`nvarchar(200)`，可為 null）依序取：
  1. `BasicSyslogContext.Describe(...)`（Scoped）：Service 在 `SaveChangesAsync` 前指定，去除前後空白、超過 200 字截斷。有需記錄的異動時才取出並清除，只寫入系統紀錄表的存檔不會用掉。
  2. 端點的 `[BasicSyslogDescription("...")]`（`Extensions/BasicSyslogDescriptionAttribute.cs`）：固定文字的預設說明。
  3. 皆無為 null。一次存檔一筆歷程，因此一次存檔只有一個說明；同一請求多次存檔需不同說明時，每次存檔前各自呼叫 `Describe`。
* 歷程查詢回傳 `BasicSyslogHistoryItem.Description`，`BasicSyslogModal.vue` 顯示於類別標籤旁；轉存 txt 有說明時另起一行「說明：」。
* 由 EF Core 的 `SaveChangesInterceptor`（`Data/BasicSyslogInterceptor.cs`）自動擷取；同步 `SaveChanges` 遇到需記錄的異動會丟出例外；歷程與業務資料在同一交易，歷程寫入失敗時業務資料一併還原。
* 把關（`Data/BasicSyslogCommandGuard.cs`，`DbCommandInterceptor`）：檢查每一句送出的 SQL，不在 `SaveChanges` 中（由 `BasicSyslogSaveTracker` 的存檔計數判斷，巢狀存檔亦同）卻含 `INSERT`／`UPDATE`／`DELETE`／`MERGE` 時丟出例外拒絕執行，涵蓋 `ExecuteUpdate`／`ExecuteDelete`、`ExecuteSql` 等原生 SQL。
  * 例外一：系統紀錄表轉存後的批次刪除，須在 `using (tracker.AllowLogArchiveDelete(nameof(表)))` 範圍內，且只放行該表的 `DELETE`；傳入業務資料表會丟出例外。
  * 例外二：`EF.IsDesignTime`（`dotnet ef` 執行 Migration）時不檢查。
  * 三個類別皆以 Scoped 註冊，與 AppDbContext 同一範圍（`AddDbContext((sp, options) => ...AddInterceptors(...))`）。
* 一次存檔一筆歷程：全部為新增記 `C`、全部為刪除記 `D`，其餘記 `U`；異動前後資料格式為 `[{ "Table": 資料表, "Data": { 欄位: 值 } }]`，含關聯表的異動，時間格式 `yyyy-MM-dd HH:mm:ss.fff`（回傳前端時去除毫秒）。
* 不記錄系統紀錄表（`BasicSyslog`、`BasicExceptionlog`、`BasicLoginErrlog`、`BasicPwdlog`、`BasicChecklogintimeSendMailLog`，清單在 `BasicSyslogSaveTracker.LogTables`）；其餘資料表的新增、刪除與任何欄位的修改皆記錄，含只異動機密欄位（如申請忘記密碼）。
* 機密欄位列於攔截器的 `MaskedProperties`，值以 `***` 遮蔽：null 仍記為 null（可看出設定或清除）；異動後的值有變更時記為 `***（已變更）`，歷程查詢才會列出該欄位。
* 保留天數為 `appsettings.json` 的 `BasicSyslog:RetentionDays`（預設 30）。寫入歷程前先檢查是否有 `CreatedAt` 早於「今天 - 保留天數」的紀錄，有則轉存為 `網站目錄\Logs\Syslog\YYYY\MM\DD\{MethodName}.txt`（日期為異動發生日，同檔附加），寫檔成功才刪除資料庫紀錄；同一天確認過即以記憶體標記略過，同一時間只允許一個轉存。轉存失敗寫入 `BasicExceptionlog`、10 分鐘後再試，不影響使用者的異動。

## 單筆資料的歷程查詢

* 各功能提供 `GET /api/{功能}/{id}/basicsyslogs`，Policy 為該資料表的 `update` 權限（如 `basicuser.update`；可編輯者才能檢視，歷程含異動前後的完整欄位值與操作者），呼叫 `IBasicSyslogHistoryService.GetRecordHistoryAsync(主表, id, 關聯表...)`。
* 以 `OPENJSON`／`JSON_VALUE` 參數化查詢比對主表的 `Id`（SQL Server 2019 支援），並列出關聯表中 `{主表}Id` 相符的異動；因此修改時一律更新主表的 `UpdatedAt`，只改關聯表的異動才會被查到。
* 以資料表與欄位描述（`HasComment`）作為畫面上的顯示名稱時，描述只存在設計時模型，須以 `db.GetService<IDesignTimeModel>().Model` 讀取（可快取）；從 `db.Model`（執行時模型）呼叫 `GetComment()` 會丟出 `InvalidOperationException`。
* 畫面不顯示內部編號：單一欄位主鍵（流水號）不列出；外鍵改顯示指向資料的名稱（使用者顯示名稱、群組名稱、權限說明，不顯示 `basicuser.read` 等代碼），欄位名稱去除描述中的「（資料表.欄位）」與結尾的「編號」（如「權限編號（BasicPermission.Id）」顯示為「權限」）；指向的資料已刪除時顯示「（已刪除）」。名稱來源列於 `BasicSyslogHistoryService.NameLoaders`，業務資料表被其他資料表以外鍵參照時須加入（未列出者顯示原始編號）。轉存 txt 保留原始值，不換成名稱。
* 只顯示資料庫內保留天數內的紀錄；回傳 `BasicSyslogHistoryResult`（`RetentionDays` + `Items`），前端提示的保留天數取自 `RetentionDays`，不寫死。

## 轉存目錄

* `Logs\` 位於網站目錄、不在 `wwwroot` 下（不會被下載）；IIS 應用程式集區帳號須有寫入權限，部署清空目錄時須保留。
