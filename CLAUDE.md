# 專案開發規範 (CLAUDE.md)

修改本檔、共用 Skill 或 `references/` 前，先讀 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\README.md`（檔案分工與維護方式）。

## 專案技術棧 (Tech Stack)

* **Backend**: .NET 10 Web API, Entity Framework Core 10, JWT, MSSQL
* **Frontend**: Vue 3 (Composition API / `<script setup>`), TypeScript, Vite, Pinia, Axios, Tailwind CSS v4
* **Protocol**: RESTful API, JSON format (`Status`, `StatusCode`, `Message`, `Datas`)

## 通用原則

* 程式碼註解一律使用繁體中文。
* 時間一律使用伺服器系統時間 `DateTime.Now`，不用 `DateTime.UtcNow`；資料庫存本地時間，前端直接以本地時間解析顯示。

## 資安防禦規範 (OWASP Top 10 最新版 & Fortify Compliance)

一律依據 OWASP Top 10 **最新版本**，新增或修改功能時須逐項確認以下類別，Fortify 掃描結果依此對應處理。

1. **存取控制失效 (Broken Access Control)**:

   * 所有 Controller 預設加上 `[Authorize]`，開放存取點需明確標註 `[AllowAnonymous]`。
   * 端點 Policy 只決定能否執行某類操作；Service 層須再檢查能否操作該筆資料（不可越權存取他人資料、不可授予或管理高於自身的權限）。
   * 判斷操作者權限一律查資料庫，不信任 JWT 內可能已過時的權限。
   * 不可對自己做危險操作（如停用、刪除自己）；系統管理員群組須受保護（不可刪除、權限不可修改、至少保留一位啟用中的成員）。
   * 無權讀取的資料一律回「不存在」，不透露資料存在。
   * 前後端同源時不啟用 CORS；確有跨來源需求時僅允許白名單來源，嚴禁 `AllowAnyOrigin`。
   * 防範 SSRF：後端不得以使用者輸入的 URL 發出請求，必要時採白名單。
2. **安全設定錯誤 (Security Misconfiguration)**:

   * 正式環境不得啟用開發者例外頁、Swagger 等除錯功能（Swagger 僅限 Development 註冊與開放）。
   * 須加上安全標頭（`X-Content-Type-Options`、`X-Frame-Options`、CSP 等），且不回傳 `Server` 等揭露伺服器資訊的標頭。
   * API 回應含個資或 Token 時須加 `Cache-Control: no-store`，不可被瀏覽器或代理伺服器快取。
   * 不得保留範例金鑰；預設管理員與預設一般使用者首次登入不強制變更密碼（種子的密碼歷程設為建立專案當日，依密碼有效天數正常到期）。
   * 安全相關參數（錯誤次數、鎖定時間、密碼歷程次數、密碼有效天數、連結時效等）一律放在 `appsettings.json`，不寫死在程式中；前端提示的數值須由後端提供，不在前端寫死。
3. **軟體供應鏈失效 (Software Supply Chain Failures)**:

   * 套件僅從官方 NuGet / npm 取得並鎖定版本：後端 `.csproj` 寫明確版本號，不用 `10.*` 等浮動版本；前端以 `package-lock.json` + `npm ci` 鎖定；不得使用有已知弱點的版本。
4. **加密機制失效 (Cryptographic Failures)**:

   * 密碼僅存雜湊（BCrypt），嚴禁明文或可逆加密，嚴禁自行實作加解密演算法。
   * 安全用途亂數一律使用 `RandomNumberGenerator`，嚴禁 `System.Random`。
   * 一次性 token（如重設密碼連結）資料庫只存雜湊（高熵亂數可用 SHA-256），不存 token 本身。
   * 正式環境須使用 HTTPS；機密不得以明文存於版控，Base64 不視為加密。
   * 機密（連線字串、JWT 金鑰、SMTP 密碼）以 AES-256 加密後存於 `appsettings.json`，啟動時解密（金鑰 `AES256Key`、IV `AES256IV` 每個專案各自隨機產生，不跨專案共用）。
   * 前端 `VITE_` 開頭的變數會被打包進 JS，`.env` 只能放非機密設定；個人設定放 `.env.local`。
5. **注入 (Injection)**:

   * 後端資料庫存取僅能使用 EF Core LINQ 或帶參數的 SQL 指令，**嚴禁**使用字串拼接 SQL。
   * 前端**嚴禁**在不可信來源資料上使用 `v-html`，一律以 `{{ }}` 輸出；後端 Input DTO 必須進行 Validation（長度、格式、拒絕 HTML 標籤）。
   * 輸入含 HTML 標籤時直接拒絕，不把內容編碼後存入（避免二次編碼）。
   * 嚴禁以使用者輸入組成檔案路徑或系統指令；Log 一律使用結構化參數，不得字串串接（Log Forging）。
   * 組成 HTML 郵件內文時，放入的動態資料（姓名、連結等）一律先 HTML 編碼。
   * 郵件範本放在後端非公開目錄（如 `Template/`），**不可**放 `wwwroot`（會被直接下載）；範本於啟動時載入，檔案遺失即無法啟動，避免到寄信時才出錯（只有帳號相符才寄信，等於透露帳號是否存在）。
6. **不安全設計 (Insecure Design)**:

   * 安全規則一律於後端實作，前端檢查僅為使用體驗。
   * 預設拒絕（fail closed）；敏感流程使用一次性、限時的憑證。
7. **身分驗證失效 (Authentication Failures)**:

   * 驗證碼須由後端產生與比對，限時且一次性使用，嚴禁僅於前端檢查；預設 5 碼（字元數放設定檔）。
   * 登入端點須有速率限制與錯誤次數鎖定；帳號或密碼錯誤時統一回應「帳號或密碼錯誤」，不得透露帳號是否存在。
   * 設定密碼一律呼叫同一個方法，不得各自實作；嚴禁以 Email 寄送密碼。
   * 錯誤次數鎖定、密碼規則、逾期變更、忘記密碼、前端登入狀態的實作細節見 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\references\auth-baseline.md`；**建立或修改這些功能前須先讀該檔**。
8. **軟體或資料完整性失效 (Software or Data Integrity Failures)**:

   * JWT 須驗證簽章、Issuer、Audience 與有效期限。
   * 嚴禁反序列化不可信的型別資訊；Controller 僅接收 DTO，不得直接綁定 Entity（Mass Assignment）。
9. **記錄與警示失效 (Security Logging & Alerting Failures)**:

   * 登入失敗、密碼變更、未處理例外、郵件寄送失敗等安全事件須留存紀錄。
   * Log 嚴禁寫入密碼、Token、重設連結、連線字串等機密，也不記錄郵件收件人與內文等個資。
   * 後端錯誤（全域 Middleware 擷取的未處理例外、背景作業失敗）除寫入 `ILogger` 外，一律寫入 `BasicExceptionlog`：例外類別、MethodName（Controller 與方法名稱，如 `BasicUsersController.Create`）、錯誤訊息、StackTrace、登入者帳號（未登入或背景作業為 null）、時間；`BusinessException` 不記錄。
     * 統一呼叫 `IBasicExceptionlogsService.WriteAsync`：另建 DbContext 寫入（不沿用請求中可能含失敗變更的 DbContext），寫入失敗只寫 `ILogger`、不再丟出例外；例外訊息可能含個資時（如郵件寄送）以參數取代訊息。
     * 保留天數為 `appsettings.json` 的 `BasicExceptionlog:RetentionDays`（預設 30）。背景服務於啟動時與之後每天一次，將 `CreatedAt` 早於「今天 - 保留天數」的紀錄轉存為 `網站目錄\Logs\Exception\YYYY\MM\DD\{MethodName}.txt`（日期為錯誤發生日，同檔附加），寫檔成功才刪除資料庫紀錄；MethodName 作為檔名前須移除不合法字元。
   * 後端新增、修改、刪除資料（管理功能、業務功能、逾期變更與重設密碼）一律寫入系統異動歷程 `BasicSyslog`：類別（`C`／`U`／`D`）、MethodName、異動前資料（JSON 陣列）、異動後資料（JSON 陣列）、登入者帳號（未登入為 null）、時間。
     * 由 EF Core 的 `SaveChangesInterceptor`（`Data/BasicSyslogInterceptor.cs`）自動擷取，新功能不需另外撰寫；異動須使用 `SaveChangesAsync`（同步 `SaveChanges` 遇到需記錄的異動會丟出例外），歷程與業務資料在同一交易，歷程寫入失敗時業務資料一併還原。
     * 一次存檔一筆歷程：全部為新增記 `C`、全部為刪除記 `D`，其餘記 `U`；異動前後資料格式為 `[{ "Table": 資料表, "Data": { 欄位: 值 } }]`，含關聯表的異動，時間格式 `yyyy-MM-dd HH:mm:ss.fff`。
     * 不記錄系統紀錄表（`BasicSyslog`、`BasicExceptionlog`、`BasicLoginErrlog`、`BasicPwdlog`）；機密欄位（密碼雜湊、token）不得寫入，列於攔截器的 `IgnoredProperties`，新增含機密欄位的資料表時須加入；只異動機密欄位時（如申請忘記密碼）不記錄。
     * 保留天數為 `appsettings.json` 的 `BasicSyslog:RetentionDays`（預設 30）。寫入歷程前先檢查是否有 `CreatedAt` 早於「今天 - 保留天數」的紀錄，有則轉存為 `網站目錄\Logs\Syslog\YYYY\MM\DD\{MethodName}.txt`（日期為異動發生日，同檔附加），寫檔成功才刪除資料庫紀錄；同一天確認過即以記憶體標記略過，同一時間只允許一個轉存。轉存失敗寫入 `BasicExceptionlog`、10 分鐘後再試，不影響使用者的異動。
     * `Logs\` 位於網站目錄、不在 `wwwroot` 下（不會被下載）；IIS 應用程式集區帳號須有寫入權限，部署清空目錄時須保留。
10. **例外狀況處理不當 (Mishandling of Exceptional Conditions)**:

    * 伺服器內部 Exception 嚴禁將 StackTrace 或原始系統錯誤傳給前端。
    * 所有例外必須經由全域 Middleware 擷取，回傳標準 `ApiResponse` 格式（如：`Status: false`, `Message: "系統發生異常，請聯繫管理員"`）。
    * 可顯示給使用者的業務錯誤一律 `throw new BusinessException("訊息")`（可帶狀態碼，如越權為 403），由全域處理回傳其訊息；其他例外寫入 Log 與 `BasicExceptionlog`（見第 9 點），對外只回通用訊息。
    * 嚴禁吞掉例外後繼續執行。

## 後端架構與目錄規範 (Backend Structure)

後端採用功能模組導向（Feature Folder）的目錄結構，namespace 與資料夾一致（如 `backend.Services.Systems`）：

| 路徑 | 職責 |
| --- | --- |
| `Controllers/[Feature]/[Feature]Controller.cs` | 薄層：驗證、權限、呼叫 Service、回傳 `ApiResponse<T>` |
| `Services/[Feature]/I[Feature]Service.cs` + `[Feature]Service.cs` | 介面與實作成對，處理查詢與異動 |
| `DTOs/[Feature]/[ClassName].cs` | 一個類別一個檔案；`ApiResponse`、`PagedResult` 置於 `DTOs/` 根目錄 |
| `Models/[Entity]/[Entity].cs` | 資料表實體，只描述欄位；namespace 一律維持 `backend.Models`（避免與類別同名，並保持 Migration 快照型別名稱不變） |
| `Data/` | `AppDbContext`（關聯、唯一索引、靜態種子）、動態種子 |
| `Exceptions/BusinessException.cs` | 可顯示給使用者的業務錯誤 |
| `Extensions/` | 擴充方法（如 `User.GetBasicUserId()`） |

**基本資料表（驗證、權限與系統紀錄）：** 各專案共用 `BasicUser`、`BasicGroup`、`BasicPermission`、`BasicUserGroup`、`BasicGroupPermission`、`BasicLoginErrlog`、`BasicPwdlog`、`BasicExceptionlog`、`BasicSyslog`，名稱與欄位維持一致；欄位定義見 `skills/new-project/SKILL.md`，既有專案以專案內的 `Models/` 為準。

* 系統資料表以 `Basic` 前置、單數命名；資料表、`Models` 實體類別、`DbSet` 三者同名（如表 `BasicUser` ↔ 類別 `BasicUser` ↔ `db.BasicUser`），導覽屬性與 `SeedData` 陣列亦同名。外鍵欄位為 `{資料表名稱}Id`（如 `BasicUserId`、`BasicGroupId`、`BasicPermissionId`）；DTO 與前端型別中引用其他資料表的屬性同樣以資料表名稱開頭（如 `BasicGroupIds`、`BasicPermissionIds`、`BasicGroupNames`；前端為 `basicGroupIds`）。
* 功能層與 API 路徑採 REST 複數（資料表名稱加 s）：`Controllers/`、`Services/`、`DTOs/` 的資料夾與 namespace、前端 `src/views/` 與路由 path／name、API 路徑，如 `Controllers/BasicUsers/BasicUsersController.cs`、`/api/basicusers`、前端路由 `basicusers`。功能層不可用單數，否則 namespace（如 `backend.Services.BasicUser`）會與實體類別同名而無法解析。
* 功能 DTO、前端型別與元件以資料表名稱開頭、用途在後，前後端 DTO 與型別同名：如 `BasicUserListItem`、`BasicUserDetail`、`BasicUserCreateRequest`、`BasicGroupSaveRequest`、`BasicUserFormData`、`BasicUserFormModal.vue`。驗證等非資料表功能的 DTO 若指向資料表，名稱須含資料表名稱（如 `CurrentBasicUserResult`、`LoginResult.BasicUser`）。
* 方法與函式名稱提到資料表時使用資料表名稱：前端 API 函式如 `getBasicUsers()`、`createBasicUser()`、`getBasicGroupOptions()`；後端如 `GetBasicGroupOptionsAsync`、`GetBasicUserPermissionsAsync`、`GetCurrentBasicUserAsync`、`User.GetBasicUserId()`；常數如 `SeedData.AdminBasicUserId`。只操作自身資料表的方法維持動作命名（如 `GetPagedAsync`、`CreateAsync`）。
* 區域變數、參數、元件 props 與 HTML id 同樣以資料表名稱命名（camelCase／kebab-case）：如 `basicUser`、`basicUserId`、`newBasicGroupIds`、`operatorBasicPermissionIds`、`v-for="basicGroup in basicGroupOptions"`、`:basic-group-options`、`id="basic-user-form"`。LINQ／陣列方法的 lambda 參數可用單字母縮寫（如 `u => u.Id`、`ug`、`gp`）。
* 其他指向資料表的類別、屬性與路徑同樣使用資料表名稱：跨功能服務 `Services/BasicPermissions/BasicPermissionsService`、權限常數 `BasicPermissionNames`、授權 `BasicPermissionRequirement`、計數屬性 `BasicUserCount`、前端 store `auth.basicUser`／`auth.hasBasicPermission()`、選單與路由 `basicPermission`、API 子路徑 `/api/basicusers/basicgroup-options`、`/api/basicgroups/basicpermissions`。框架內建名稱（如 `ControllerBase.User`、`RequireAuthenticatedUser()`）不在此限。
* 權限名稱為 `{資料表名稱小寫}.{動作}`，與資料表一致，如 `basicuser.read`、`basicgroup.update`、`bulletins.create`；動作為 `read`、`create`、`update`、`delete`。授權 Policy 名稱與權限名稱相同。
* 撰寫原生 SQL（SSMS、報表、`FromSql`）時使用資料表名稱（如 `BasicUser`），不是 API 路徑或功能名稱。
* 權限一律指派給群組，不直接指派給使用者。
* 種子資料：「系統管理員」群組擁有全部權限且受保護；「訪客」群組擁有全部檢視（`.read`）權限；預設管理員（系統管理員群組）與預設一般使用者（訪客群組）以靜態種子於建表時建立（帳密見 `new-project` skill；一般使用者的初始密碼為已知值，正式上線前須停用或變更）。
* 建立資料表或新增欄位時，**必須同時**建立資料表描述與欄位描述，不得事後補上；含業務功能、既有專案後續新增的資料表與欄位。
  * EF Core：一律在 `AppDbContext` 以 `HasComment` 加上繁體中文描述（資料表為 `ToTable("名稱", t => t.HasComment("描述"))`），隨 Migration 寫入 SQL Server 的 `MS_Description`。
  * 原生 SQL 建表（SSMS、部署腳本）：同一腳本內以 `sp_addextendedproperty` 寫入 `MS_Description`（資料表與每個欄位）。
  * 建表或加欄位後，以唯讀查詢 `sys.extended_properties` 確認每張資料表與每個欄位都有 `MS_Description`。
  * 描述只說明欄位本身的意義（自動編號主鍵為「流水號」、外鍵註明指向的資料表欄位，如「使用者編號（BasicUser.Id）」），不寫特定資料的值或程式約定。`__EFMigrationsHistory` 由 EF Core 管理，不加描述。
* SQL Server 上可為 null 的欄位不建唯一索引，改建一般索引：EF Core 會建成篩選索引，以 sqlcmd 等預設 `QUOTED_IDENTIFIER OFF` 的工具更新該表時會失敗。

**Service 層寫法：**

* 新增或修改前先檢查唯一值（如帳號、Email、名稱），重複時丟 `BusinessException`，不依賴資料庫例外。
* 關聯 ID 寫入前先去除重複並確認存在。
* 主表與關聯表透過導覽屬性一起加入，**只呼叫一次 `SaveChangesAsync`**，讓 EF Core 包在同一個交易中。
* 異動資料的方法接收 `operatorId`（Controller 傳入目前登入者），用於資料層級的權限檢查。

## 前端架構與目錄規範 (Frontend Structure)

前端同樣按功能獨立資料夾存放：

```text
src/views/[Feature]/
├─ Index.vue      頁面（列表、流程控制、呼叫 API）
├─ api.ts         該功能 API
├─ types.ts       該功能型別
├─ routes.ts      該功能路由與權限 meta（`meta.basicPermission`）
└─ components/    該功能子元件，如 [Entity]FormModal.vue（`BasicUserFormModal.vue`）
```

**UI 慣例：**

* 操作結果（儲存成功、刪除成功、操作失敗）一律以訊息視窗顯示，**不使用頁面內提示列**。
* API 失敗時由 Axios interceptor 顯示錯誤並標記 `handled`；頁面 catch 只在 `!handled` 時補顯示「操作失敗，請再試一次」，避免重複提示。
* 新增／編輯共用同一個 Modal，放在該功能的 `components/`：以 `defineModel` 控制開關、`item` prop 區分新增或編輯（`null` 為新增），開啟時重設表單，送出以事件把資料交回 `Index.vue`。

## API 回傳統一格式

```json
{
  "status": true,       // 成功或失敗
  "statusCode": 200,    // 與 HTTP 狀態碼相同（如 200、400、401、403、404、429、500）
  "message": "",        // 成功或錯誤訊息
  "datas": [],          // 資料
  "totalCount": 0,      // 資料總筆數
  "page": 1,            // 目前頁數
  "pageSize": 10,       // 每頁筆數
  "totalPages": 0       // 總頁數
}
```

* **只有 HTTP 200 回傳 `status: true`**；其餘一律 `status: false`，並使用實際的 HTTP 狀態碼，`statusCode` 與 HTTP 狀態碼相同。
* 所有失敗回應（含框架產生的 401、403、429、404）都須是 `ApiResponse` 格式，不可回空 Body 或框架預設格式。
* 失敗訊息一律放在 `message`，不另設 `errors` 欄位；多筆驗證訊息以 `\n` 合併成單一字串。
* 分頁端點回傳 `PagedResult<T>`，展開至頂層的 `totalCount`、`page`、`pageSize`、`totalPages`。

## 部署

* 前端部署於子路徑（預設 `/<專案名稱>/`），站內路徑（路由、API、靜態資源、`window.open` 等）一律以 `import.meta.env.BASE_URL` 組成，不得寫死 `/`；後端產生的前端網址須含子路徑。
* `.ps1` 腳本須存為 **UTF-8 with BOM**，否則 Windows PowerShell 5.1 會把中文讀成亂碼。
