# 不使用範本時的建立流程

`Project_Info.txt` 的 `使用範本` 為「否」時，以本檔取代 SKILL.md 第 3 節的範本流程，依規範重新產生所有程式碼。其餘章節（指令寫法、環境檢查、紅綠燈、驗證）仍依 SKILL.md。

- 不參考 `存放路徑` 下的其他既有專案，也不複製 `templates/project/`；程式依規範重新產生，UI 樣式可能與其他專案不同。
- 金鑰與雜湊仍可使用 `templates/keygen/`（複製到 scratchpad 執行，見 SKILL.md 第 3 節第 2 步）。

燈號判斷完成（全部綠燈，或紅燈已取得回覆）後依序建立，每完成一個階段簡短回報，不中途詢問：

1. **後端**：以 `dotnet new web -o <專案目錄>\backend -n backend` 建立（空白範本，只產生 `backend.csproj`、`Program.cs`、`appsettings*.json`、`Properties/launchSettings.json`，不含範本檔與 OpenApi 套件）；Controller、Swagger 等依規範自行加入，套件以 `dotnet add package <套件> --version <版本>` 加入並寫明確版本號（不用浮動版本）；依規範建立 `Controllers/`、`Services/`、`DTOs/`、`Models/`、`Data/`、`Exceptions/`、`Extensions/`。
   - 必要套件：`BCrypt.Net-Next`、`Microsoft.AspNetCore.Authentication.JwtBearer`、`Microsoft.EntityFrameworkCore.SqlServer`、`Microsoft.EntityFrameworkCore.Design`、`Swashbuckle.AspNetCore`、`MailKit`（郵件）、`SkiaSharp`（圖形驗證碼）；郵件與驗證碼不可改用 `SmtpClient` 或自行實作的影像編碼（見通用規範「專案技術棧」）。
   - 套件版本：範本 `templates/project/backend/backend.csproj` 已有的套件沿用範本的版本（範本版本已經 `upgrade-packages` 檢查，符合發佈滿 14 天）；範本沒有的套件依 `add-package` skill 選用發佈滿 14 天的最新穩定版，並取得使用者同意。
   - **每次 `dotnet add package` 後**立即執行 `dotnet list <backend 絕對路徑>\backend.csproj package --vulnerable --include-transitive`：遞移相依有 High 或 Critical 弱點時，以 `dotnet add package <該套件> --version <修正版>` 明確指定修正版，並於 `.csproj` 註明是哪個套件帶進的弱點版本（例：`Microsoft.EntityFrameworkCore.SqlServer` 經 `Microsoft.Data.SqlClient` 帶進的 `System.Security.Cryptography.Pkcs 9.0.11`，須指定 10.0.13 以上）；全部加入後再執行一次，須無 High 或 Critical。
   - 資料表：依 `references\basic-tables.md`，加上第 23 項的業務資料表（業務功能的後端、前端依 `skills\add-feature\SKILL.md` 第 2.1、2.2 節建立；一律含 `CreatedAt`、`UpdatedAt`；列表的異動時間依通用規範、異動歷程與錯誤紀錄依 `references\logging-baseline.md`，`BasicSyslogHistoryService` 與 `BasicSyslogModal.vue` 可參考範本；歷程畫面以資料表與欄位描述作為顯示名稱時，描述只存在設計時模型，須以 `db.GetService<IDesignTimeModel>().Model` 讀取，從 `db.Model` 讀取會在執行時丟出例外）；資料表與每個欄位以 `HasComment` 加上描述（基本資料表的描述沿用範本 `templates/project/backend/Data/AppDbContext.cs`）。
   - 種子（皆以 `HasData` 靜態種子，隨 Migration 建表寫入）：權限、受保護的「系統管理員」群組（`Id = 1`）、「訪客」群組（`Id = 2`）、預設管理員、預設一般使用者。
     - 預設管理員：第 9、10 項的管理員帳密，加入系統管理員群組；建立時以 BCrypt 產生雜湊，程式與 Migration 只存雜湊，不出現明文密碼。
     - 預設一般使用者：第 9、10 項的一般使用者帳密（雜湊方式同上），加入訪客群組；訪客群組的權限以 `SeedData.BasicPermission` 中名稱以 `.read` 結尾者產生，新增業務功能權限時自動擁有檢視權限。訪客群組不受保護，可由管理員調整或刪除。
     - 兩個帳號各寫入一筆 `BasicPwdlog`（同一雜湊、`CreatedAt` 設為建立專案當日的固定日期），首次登入不強制變更密碼，之後依密碼有效天數正常到期；無歷程會被視為逾期，因此這筆歷程不可省略。
   - 全域例外 Middleware、`BusinessException`、`ApiResponse`／`PagedResult`、`InvalidModelStateResponseFactory`（模型驗證失敗回 `ApiResponse`；JSON 解析失敗或缺少 Body 時只回「請求資料格式不正確」，不洩漏型別或路徑）；設定 `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true`，DTO 一律明確標註中文驗證訊息。
   - 系統錯誤紀錄：`Services/BasicExceptionlogs/`（`IBasicExceptionlogsService` 以 Singleton 註冊，`WriteAsync` 以 `IServiceScopeFactory` 另建 DbContext 寫入、失敗只寫 `ILogger`；`ArchiveAsync` 分批轉存、先寫檔再以 `ExecuteDeleteAsync` 刪除）、`BasicExceptionlogArchiveService`（`BackgroundService`，啟動時執行一次，之後以 `PeriodicTimer` 每天一次；轉存失敗時寫入 `ILogger` 並呼叫 `WriteAsync`，不中斷服務）、`Settings/BasicExceptionlogSettings.cs`（`BasicExceptionlog:RetentionDays`）；全域例外 Middleware 與郵件背景服務呼叫 `WriteAsync`，規則見通用規範第 9 點。`.gitignore` 加入 `backend/Logs/`。
   - 系統異動歷程：`Data/BasicSyslogInterceptor.cs`（`SaveChangesInterceptor`，以 Scoped 註冊並以 `AddDbContext((sp, options) => ... .AddInterceptors(...))` 掛上；`SavingChangesAsync` 擷取異動前資料、呼叫轉存、無交易時開啟交易，`SavedChangesAsync` 取得新增資料的 Id 後寫入 `BasicSyslog` 並提交，`SaveChangesFailedAsync` 還原；需 `AddHttpContextAccessor` 取 MethodName 與帳號；機密欄位以 `MaskedProperties` 遮蔽記錄；業務說明取自 `Data/BasicSyslogContext.cs` 或端點的 `Extensions/BasicSyslogDescriptionAttribute.cs`）、`Data/BasicSyslogCommandGuard.cs` 與 `Data/BasicSyslogSaveTracker.cs`（拒絕不經 SaveChanges 的異動，與攔截器一同掛上；轉存服務以 `AllowLogArchiveDelete` 宣告批次刪除，細節見 `references\logging-baseline.md`）、`Services/BasicSyslogs/`（`IBasicSyslogsService.ArchiveIfDueAsync`，Singleton，以 `SemaphoreSlim` 確保同時只有一個轉存，當天確認過即略過）、`Settings/BasicSyslogSettings.cs`（`BasicSyslog:RetentionDays`）。轉存路徑與 MethodName 共用 `Extensions/LogArchive.cs`、`Extensions/HttpContextExtensions.cs`，規則見通用規範第 9 點。
   - 失敗回應的 HTTP 狀態碼與 `ApiResponse`：
     - JWT `OnChallenge`（呼叫 `HandleResponse()`）回 401、自訂 `IAuthorizationMiddlewareResultHandler` 回 403、速率限制 `OnRejected` 回 429、`/api/{**path}` 回 404（不落入 SPA fallback）。
     - `SuppressMapClientErrors = true` 並以 `UseStatusCodePages` 將 `/api` 下沒有 Body 的錯誤（405、415 等）轉為 `ApiResponse`。
     - 預設授權 Policy 會擋下沒有對應端點的請求，須以 `MapFallback("{**path:file}", ...)`（`AllowAnonymous`）讓不存在的檔案路徑回 404，不回空 Body 的 401。
     - 前端 Axios 於錯誤分支讀取 `message`，401 視為登入逾時。
   - 安全設定：JWT 驗證（簽章、Issuer、Audience、期限；`OnTokenValidated` 確認使用者仍啟用且安全戳記相符，見 `references\auth-baseline.md`）、預設 `[Authorize]`、安全標頭、移除 `Server` 標頭、`Cache-Control: no-store`、Swagger 僅限 Development（非 Development 時 `/swagger` 明確回 404，不落入 SPA fallback）、速率限制。
   - 密碼設定統一由單一方法處理；郵件範本放 `Template/` 並於啟動時載入。
   - 連線字串、JWT 金鑰、SMTP 密碼：以 AES-256 加密後寫入 `appsettings.json`（SMTP 密碼為空時不解密）。
     - `AES256Key`（32 字元）、`AES256IV`（16 字元）以 `RandomNumberGenerator` 從英數字隨機產生，每個專案各自一組，寫在 `appsettings.json` 頂層。
     - `Extensions/ConfigEncryption.cs`：使用 .NET 內建 `Aes`（CBC、PKCS7），加密值以 Base64 儲存；解密失敗或長度不符時拒絕啟動，錯誤訊息不含原始值。
     - `Program.cs` 在註冊服務前解密，結果存入區域變數（連線字串直接傳給 `UseSqlServer`，並設定 `o => o.UseCompatibilityLevel(150)`（SQL Server 2019 基準，見通用規範「資料庫版本」）；JWT 金鑰以 `PostConfigure<JwtSettings>` 設定）；不寫回 `IConfiguration`，否則 appsettings 重新載入時會還原成加密值。
     - 提供工具模式 `dotnet run -- encrypt "<明文>"`，讀取輸出目錄的 `appsettings.json` 並印出加密值，用來產生上述加密值，以及之後更換時使用。
     - 連線字串取自 `DB_DefaultConnection.txt`、SMTP 密碼取自 `SendMail_Info.txt` 的明文，以本專案金鑰加密；連線字串未含 `TrustServerCertificate` 時加上 `TrustServerCertificate=True;`（SqlClient 預設加密連線，本機 SQL Server 為自簽憑證）。不可沿用其他專案的加密值（金鑰不同會解密失敗、拒絕啟動），明文不得寫入專案任何檔案或回報內容。
     - JWT 金鑰以 `RandomNumberGenerator` 產生至少 48 位元組；金鑰格式（Base64 或一般字串）須與簽章程式的解碼方式一致。
   - 不使用 `dotnet user-secrets`。
   - `Program.cs`、`appsettings*.json`、`launchSettings.json` 直接以 Write 覆寫。
2. **前端**：Vite + Vue 3 + TypeScript，安裝 Pinia、Axios、Tailwind CSS v4，以 `package-lock.json` 鎖定版本；依規範建立 `src/views/[Feature]/` 結構、Axios interceptor（`handled` 標記）、登入狀態（檢查 `expiresAt`）、路由守衛；版面為側邊選單 + 頂列的後台版面，主色、Logo 依第 24 項。
   - 不使用 `npm create vite`（會產生 `HelloWorld.vue`、`src/assets/`、`public/icons.svg`、`README.md` 等需刪除的範本檔）。以 Write 直接寫入 `package.json`（`name`、`private`、`type: "module"`、`scripts`：`dev`／`build`（`vue-tsc -b && vite build`）／`preview`，不寫 dependencies）、`index.html`、`vite.config.ts`、`tsconfig.json`、`tsconfig.app.json`、`tsconfig.node.json`、`env.d.ts`、`src/` 下所有檔案。
   - 再以 `npm install --prefix <frontend 絕對路徑> --save-exact --before=<今天 - 14 天，yyyy-MM-dd> vue vue-router pinia axios` 與 `npm install --prefix <frontend 絕對路徑> --save-exact --before=<同上> -D vite @vitejs/plugin-vue typescript vue-tsc @vue/tsconfig @types/node tailwindcss @tailwindcss/vite` 安裝，版本寫入 `package.json` 並產生 `package-lock.json`。`--before` 讓 npm 只選用該日期以前發佈的版本（含相依套件），符合「發佈滿 14 天」的規則。
   - 安裝後執行 `npm audit --prefix <frontend 絕對路徑>`，須無 High 或 Critical；有時依 `upgrade-packages` 的方式處理，不使用 `npm audit fix --force`。
   - `vue-tsc` 不支援 TypeScript 7（原生版本）；安裝到 7.x 時改以 `npm install --prefix <frontend 絕對路徑> --save-exact --before=<同上> -D typescript@6` 固定為 6.x，並記錄於專案 CLAUDE.md。
   - Tailwind CSS v4 的自訂 class 要被 `@apply` 引用時須以 `@utility` 定義（`@layer components` 內的 class 無法被 `@apply`）。
   - 前端 `.env` 內容須維持純 ASCII（不加中文註解），否則 Windows PowerShell 5.1 讀取時會亂碼，發佈腳本讀不到 `VITE_APP_BASE`；說明寫在專案 CLAUDE.md。
3. **部署檔**：依第 19～21 項產生 `web.config`、SPA `index.html` 的 `Cache-Control: no-cache` 設定與發佈腳本。
   - 移除伺服器標頭：Kestrel 設定 `AddServerHeader = false`；IIS 於 `web.config` 以 `<requestFiltering removeServerHeader="true" />`（須 IIS 10 以上）與 `<customHeaders><remove name="X-Powered-By" /></customHeaders>` 移除 IIS 加上的標頭。
   - `web.config` 的 `aspNetCore` 設定 `processPath=".\backend.exe" arguments=""`（發佈指定 RID 會產生 apphost，若寫 `dotnet` 與 `.\backend.dll`，轉換後會留下多餘參數）。
   - SPA 的 `index.html`（含 fallback）回應 `Cache-Control: no-cache`；assets 檔名含 hash，可正常快取。
   - 發佈腳本：複製 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\skills\publish-project\templates\publish.ps1` 至專案根目錄，目錄名稱與第 3 項不同時調整 `-FrontendDir`、`-BackendDir` 預設值；以 `Copy-Item` 複製（範本本身為 UTF-8 with BOM，複製即保留），不用 `[IO.File]::WriteAllText` 等 .NET 呼叫；需調整預設值時以 Edit 工具修改（Edit 會保留原檔的 BOM）。同時以 `Copy-Item` 複製範本的 `templates\project\dotnet-tools.json` 至專案根目錄（發佈腳本產生 SBOM 用，鎖定 CycloneDX 工具版本），再依 SKILL.md 第 3 節第 7 步「SBOM 工具版本」選用發佈滿 14 天的最新穩定版。
   - 後端 `.csproj` 排除 `publish/**`（避免下次發佈時被當成內容檔再複製一層），並於發佈時排除套件附帶的 `.pdb`（如 `libSkiaSharp.pdb` 約 90 MB）。
   - 部署子路徑（第 19 項）：前端 `.env` 設定 `VITE_APP_BASE`（以 `/` 開頭與結尾，納入版控）；`vite.config.ts` 只在建置時套用為 `base`（開發固定 `/`）；router `createWebHistory(import.meta.env.BASE_URL)`、Axios ``baseURL: `${import.meta.env.BASE_URL}api` ``；後端由 IIS 自動設定 `PathBase`，不需另外處理。
   - 專案 CLAUDE.md 註明發佈流程使用 `/publish-project`。
4. **專案 CLAUDE.md**：第一行以 `@D:/Jovi/Localhost/_ClaudeCode/_Claude_Standards/CLAUDE.md` 匯入通用規範，再記錄專案用途、目錄、啟動方式、專案特有設定與待辦，並註明「本專案未使用 new-project 範本建立」。
5. **Migration**（第 8 項為是時）：`dotnet ef migrations add InitialCreate --project <backend 絕對路徑>\backend.csproj --startup-project <backend 絕對路徑>\backend.csproj --output-dir Data/Migrations`，再 `dotnet ef database update --project <backend 絕對路徑>\backend.csproj --startup-project <backend 絕對路徑>\backend.csproj`；確認產生的 Migration 不含明文密碼，並以 `sqlcmd` 唯讀查詢確認每張資料表與欄位都有 `MS_Description`。
6. **git**：初始化並加入 `.gitignore`（含 `bin/`、`obj/`、`node_modules/`、`dist/`、`publish/`、後端 `wwwroot/`（發佈腳本產生）、`MailPickup/`、`Logs/`、`.env.local`、`sbom/`）；**不 commit**，於完成回報提醒使用者自行 commit。
