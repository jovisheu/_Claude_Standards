# 專案開發規範 (CLAUDE.md)

修改本檔、共用 Skill 或 `references/` 前，先讀 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\README.md`（檔案分工與維護方式）。

## 專案技術棧 (Tech Stack)

* **Backend**: .NET 10 Web API, Entity Framework Core 10, JWT, MSSQL
* **資料庫版本**: 開發與正式環境以 **Microsoft SQL Server 2019**（資料庫相容性層級 150）為基準，程式只能使用 SQL Server 2019 支援的語法：
  * EF Core 一律 `UseSqlServer(連線字串, o => o.UseCompatibilityLevel(150))`，避免 EF Core 依較新版本產生 2019 不支援的 SQL；開發機為較新版本時同樣以 150 為準。
  * 原生 SQL（`FromSql`、SSMS、部署腳本）不得使用 SQL Server 2022 以後才有的語法，如 `GREATEST`／`LEAST`、`DATETRUNC`、`DATE_BUCKET`、`GENERATE_SERIES`、`JSON_OBJECT`／`JSON_ARRAY`、`IS [NOT] DISTINCT FROM`、`STRING_SPLIT` 的第三個參數、`LTRIM`／`RTRIM` 指定字元、`WINDOW` 子句、`json` 資料型別。
  * 資料庫相容性層級須 150 以上（從舊版還原的資料庫須確認，低於 150 時以 `ALTER DATABASE ... SET COMPATIBILITY_LEVEL = 150` 調整）。
* **Frontend**: Vue 3 (Composition API / `<script setup>`), TypeScript, Vite, Pinia, Axios, Tailwind CSS v4
* **Protocol**: RESTful API, JSON format (`Status`, `StatusCode`, `Message`, `Datas`)

## 通用原則

* 程式碼註解一律使用繁體中文。
* 時間一律使用伺服器系統時間 `DateTime.Now`，不用 `DateTime.UtcNow`；資料庫存本地時間，前端直接以本地時間解析顯示。
* 後端回傳的時間一律為 `yyyy-MM-ddTHH:mm:ss`（不含毫秒與時區），由 `Extensions/LocalDateTimeJsonConverter` 於 `AddControllers().AddJsonOptions` 全域套用，DTO 不各自格式化。

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
   * 不得保留範例金鑰。
   * 安全相關參數（錯誤次數、鎖定時間、密碼歷程次數、密碼有效天數、連結時效等）一律放在 `appsettings.json`，不寫死在程式中；前端提示的數值須由後端提供，不在前端寫死。
3. **軟體供應鏈失效 (Software Supply Chain Failures)**:

   * 套件僅從官方 NuGet / npm 取得並鎖定版本：後端 `.csproj` 寫明確版本號，不用 `10.*` 等浮動版本；前端以 `package-lock.json` + `npm ci` 鎖定；不得使用有已知弱點的版本。
   * 新增套件一律使用 `/add-package`：先確認沒有內建功能或既有套件可替代，經使用者同意後才加入，有 High 或 Critical 弱點即不採用。
   * 發佈時 `publish.ps1` 有 **High 或 Critical** 弱點即停止（fail closed），並產生 CycloneDX SBOM；不得修改腳本或略過檢查來繞過，**不設例外清單**。卡控與 SBOM 細節見 `publish-project` skill。
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
   * 組成 HTML 郵件內文時，放入的動態資料（姓名、連結等）一律先 HTML 編碼；郵件範本放在後端非公開目錄（如 `Template/`），不可放 `wwwroot`。
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
   * 未處理例外與背景作業失敗除寫入 `ILogger` 外，統一呼叫 `IBasicExceptionlogsService.WriteAsync` 寫入 `BasicExceptionlog`；`BusinessException` 不記錄。
   * 系統紀錄表（`BasicSyslog`、`BasicExceptionlog`、`BasicLoginErrlog`、`BasicPwdlog`）以外的**所有**新增、修改、刪除都須寫入 `BasicSyslog`，由 `Data/BasicSyslogInterceptor.cs` 自動寫入，新功能不需另外撰寫，但須遵守：
     * 異動一律載入實體後以 `SaveChangesAsync` 存檔；不可用同步 `SaveChanges`、`ExecuteUpdate`／`ExecuteDelete`、`ExecuteSql` 等原生 SQL 異動。`Data/BasicSyslogCommandGuard.cs` 會在執行時拒絕不經 `SaveChanges` 的異動（fail closed），唯一例外是系統紀錄表轉存後以 `AllowLogArchiveDelete` 宣告的批次刪除。
     * DbContext 一律由 DI 取得，不可自行 `new AppDbContext(...)`（不會掛上攔截器）。
     * 只改機密欄位的異動同樣記錄；新增含機密欄位（密碼雜湊、token、金鑰）的資料表時，將欄位加入攔截器的 `MaskedProperties`，歷程中的值以 `***` 遮蔽。
     * 業務說明 `Description`（選填）：寫入端點標上 `[BasicSyslogDescription("編輯公告")]` 作為預設說明；需依結果區分時（審核通過／退回、批次匯入、系統自動處理），在 `SaveChangesAsync` 前呼叫 `BasicSyslogContext.Describe("...")`，優先於端點預設。說明會顯示於歷程畫面並轉存 txt，不可含密碼、token 等機密或不必要的個資，也不直接放入使用者輸入的長文。
     * 程式以外的修改（SSMS、部署腳本、Migration 種子）不在攔截範圍，正式環境不直接修改資料，資料修正經由系統功能或 Migration。
     * 各功能提供 `GET /api/{功能}/{id}/basicsyslogs`（Policy 為該資料表的 `update` 權限），呼叫 `IBasicSyslogHistoryService.GetRecordHistoryAsync(主表, id, 關聯表...)`。
   * 紀錄欄位、保留天數、轉存至 `Logs\` 與歷程查詢的實作細節見 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\references\logging-baseline.md`；**修改這些機制前須先讀該檔**。
10. **例外狀況處理不當 (Mishandling of Exceptional Conditions)**:

    * 伺服器內部 Exception 嚴禁將 StackTrace 或原始系統錯誤傳給前端。
    * 所有例外必須經由全域 Middleware 擷取，回傳標準 `ApiResponse` 格式（如：`Status: false`, `Message: "系統發生異常，請聯繫管理員"`）。
    * 可顯示給使用者的業務錯誤一律 `throw new BusinessException("訊息")`（可帶狀態碼，如越權為 403），由全域處理回傳其訊息；其他例外寫入 Log 與 `BasicExceptionlog`，對外只回通用訊息。
    * 嚴禁吞掉例外後繼續執行。

## 後端架構與目錄規範 (Backend Structure)

新增業務功能（資料表、權限、API、前端頁面）時使用 `/add-feature`，替既有業務資料表加欄位時使用 `/alter-table`，依其步驟建立與驗證。

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

**基本資料表（驗證、權限與系統紀錄）：** 各專案共用 `BasicUser`、`BasicGroup`、`BasicPermission`、`BasicUserGroup`、`BasicGroupPermission`、`BasicLoginErrlog`、`BasicPwdlog`、`BasicExceptionlog`、`BasicSyslog`，名稱與欄位維持一致；欄位與種子定義見 `references/basic-tables.md`，既有專案以專案內的 `Models/` 為準。系統資料表以 `Basic` 前置，業務資料表不可用 `Basic` 開頭。

**命名：一律以資料表名稱（單數 PascalCase）為基準**，以 `BasicUser` 為例：

| 對象 | 規則 | 例 |
| --- | --- | --- |
| 資料表、`Models` 類別、`DbSet`、導覽屬性、`SeedData` 陣列 | 與資料表同名（單數） | `BasicUser`、`db.BasicUser` |
| 外鍵欄位 | `{資料表}Id` | `BasicUserId`、`BasicGroupId` |
| 功能層：`Controllers/`、`Services/`、`DTOs/` 資料夾與 namespace、前端 `src/views/`、路由 path／name、API 路徑 | REST 複數（資料表加 s）；不可用單數，否則 namespace 會與實體類別同名而無法解析 | `Controllers/BasicUsers/BasicUsersController.cs`、`/api/basicusers`、路由 `basicusers` |
| DTO、前端型別、元件 | 資料表在前、用途在後，前後端同名 | `BasicUserListItem`、`BasicUserDetail`、`BasicGroupSaveRequest`、`BasicUserFormData`、`BasicUserFormModal.vue` |
| 引用其他資料表的屬性、方法、函式、常數、變數、props、HTML id、API 子路徑 | 含資料表名稱（camelCase／kebab-case 依語境） | `BasicGroupIds`、`GetBasicGroupOptionsAsync`、`getBasicUsers()`、`User.GetBasicUserId()`、`SeedData.AdminBasicUserId`、`newBasicGroupIds`、`:basic-group-options`、`id="basic-user-form"`、`/api/basicusers/basicgroup-options` |
| 權限名稱與授權 Policy | `{資料表小寫}.{read\|create\|update\|delete}` | `basicuser.read`、`bulletin.create` |
| 原生 SQL（SSMS、報表、`FromSql`） | 資料表名稱，不是 API 路徑或功能名稱 | `SELECT ... FROM BasicUser` |

* 例外：只操作自身資料表的方法維持動作命名（`GetPagedAsync`、`CreateAsync`）；LINQ／陣列方法的 lambda 參數可用單字母（`u => u.Id`、`ug`）；框架內建名稱（`ControllerBase.User`、`RequireAuthenticatedUser()`）不在此限。
* 驗證等非資料表功能的 DTO 若指向資料表，名稱須含資料表名稱（如 `CurrentBasicUserResult`、`LoginResult.BasicUser`）。

**資料表規則：**

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
* 列表顯示「異動時間」欄：`updatedAt` 為 null 時顯示 `createdAt`（共用 `src/utils/format.ts` 的 `formatModifiedAt`），不另顯示建立時間欄。
* 沒有該功能的 `create`／`update`／`delete` 權限，或資料受保護（如系統管理員群組不可刪除、權限不可修改；不可停用或刪除自己；不可授予高於自身的權限）時，對應的按鈕、輸入欄位與勾選項一律以 `v-if` **不渲染**，不可用 `disabled`、`v-show` 或 CSS 隱藏（元素仍在 DOM 中，可被瀏覽器開發工具改回可用）；需讓使用者知道現況時改以純文字顯示（如受保護群組的權限清單、不可變更的帳號）。
  * 前端不渲染只是體驗與減少誤用，後端仍須檢查（見「存取控制失效」）。
  * `disabled` 只用於與權限無關的暫時狀態（送出中、分頁邊界、必填未填）。
* 列表操作欄提供「歷程」按鈕（有該功能的 `update` 權限才顯示），以共用元件 `src/components/BasicSyslogModal.vue` 顯示該筆資料的異動歷程；API 函式命名如 `getBasicUserBasicSyslogs(id)`。
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
