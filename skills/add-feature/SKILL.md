---
name: add-feature
description: 在依 _Claude_Standards 建立的既有專案中新增業務功能（資料表、權限、後端 API、前端管理頁面、Migration）。在使用者要求新增功能、資料表或管理頁面時使用；比照範本的使用者管理（BasicUsers）實作，完成後驗證並執行 owasp-check；不 commit、不發佈。全部綠燈時直接建立不詢問，只有紅燈項目才詢問。
---

# 新增業務功能

## 用途

- **做什麼**：在既有專案加入一個完整的資料管理功能：資料表與 Migration、權限種子、後端 Model／DTO／Service／Controller（含異動歷程端點）、前端列表與新增／編輯頁面、路由與選單；完成後驗證並執行 `owasp-check`。
- **何時使用**：專案需要新的資料表或管理頁面時（如公告、分類、訂單）。專案須由 `/new-project` 建立或符合同樣的結構。
- **不適用**：修改或刪除既有資料表、欄位或資料（屬破壞性變更，會停下詢問）；登入、密碼等基本功能的調整（直接依通用規範與 `auth-baseline.md` 修改）；非資料管理類的功能（如報表、排程），可參考本 skill 的規則但需另行設計。
- **使用方式**：`/add-feature <功能名稱>(<欄位> <型別> [必填] [唯一]、...)`，例：`/add-feature 公告(標題 nvarchar(100) 必填、內容 nvarchar(max)、發布日期 date)`；規格不足時只詢問缺少的部分。
- **產出**：新增的前後端程式與 Migration `Add<Table>`（已套用至開發資料庫）、更新的專案 `CLAUDE.md` 與完成回報；**不 commit、不發佈**，完成後請自行 commit 再 `/publish-project`。

規則來源：`D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\CLAUDE.md`（通用規範）。先完整讀過，再讀專案的 `CLAUDE.md`；涉及登入、密碼或帳號時另讀 `references\auth-baseline.md`。所有程式碼須符合通用規範的命名、目錄、資安、API 格式與資料庫版本（SQL Server 2019）規定。

實作一律比照專案內既有的使用者管理（`BasicUsers`）：後端 `Models/BasicUser/`、`DTOs/BasicUsers/`、`Services/BasicUsers/`、`Controllers/BasicUsers/`，前端 `src/views/BasicUsers/`。先讀過這些檔案再撰寫，寫法、註解密度與 UI 樣式與其一致；不參考其他專案。

## 0. 指令寫法

同 `new-project` 第 0 節：一次一個指令、使用絕對路徑，不以 `cd ... &&` 串接；讀寫檔用 Read、Grep／Glob、Write、Edit 工具；全程不刪除檔案；`sqlcmd` 只用於唯讀查詢，不異動資料庫資料（第 3 節的寫入實測一律經由 API 進行）。

## 1. 規格與紅綠燈

### 1.1 取得規格

從使用者的需求整理下列項目；能合理推得的採預設值，不逐項詢問：

| 項目 | 預設 |
| --- | --- |
| 功能名稱（中文，選單與頁面標題） | 取自需求 |
| 資料表名稱（英文單數 PascalCase） | 由功能名稱翻譯，如「公告」→ `Bulletin`；不可以 `Basic` 開頭（保留給系統資料表） |
| 功能層名稱（REST 複數） | 資料表名稱加 `s`，如 `Bulletins`；API 路徑與前端路由為小寫 `bulletins` |
| 權限 | `{資料表名稱小寫}.read/create/update/delete`，如 `bulletin.read`；`Module` 為功能名稱 |
| 欄位 | 名稱、型別、長度、必填、唯一、中文描述；另一律加上 `Id`（流水號）、`CreatedAt`、`UpdatedAt` |
| 外鍵 | 欄位名 `{資料表}Id`，刪除規則預設 `Restrict`（被參照的資料不可刪除） |
| 列表欄位與查詢 | 列表顯示主要文字欄位與「異動時間」；關鍵字查詢比對文字欄位 |
| 選單位置 | 加在 `menu.ts` 的最後 |

型別對應：文字 `string` + `HasMaxLength`（未指定長度時預設 100；`nvarchar(max)` 不加長度且不建索引）、整數 `int`、金額 `decimal` + `HasPrecision(18, 2)`、日期 `DateOnly`、日期時間 `DateTime`、是否 `bool`；非必填用可為 null 的型別。

### 1.2 紅綠燈

| 項目 | 紅燈條件（停下詢問） |
| --- | --- |
| 專案結構 | 缺少 `backend/Data/AppDbContext.cs`、`Data/BasicPermissionNames.cs`、`Data/SeedData.cs`、`Services/BasicSyslogs/IBasicSyslogHistoryService.cs`、`frontend/src/components/BasicSyslogModal.vue` 等範本結構（非依規範建立的專案） |
| 規格 | 無法判斷欄位、型別、長度或關聯 |
| 名稱 | 資料表、`DbSet`、功能資料夾、API 路徑、權限名稱或前端路由已存在；名稱不可作為識別字；以 `Basic` 開頭 |
| 關聯 | 外鍵指向的資料表不存在 |
| 機密欄位 | 含密碼、token、金鑰等欄位（須確認處理方式：雜湊或加密、列入 `IgnoredProperties`、不回傳前端） |
| 既有資料表 | 需求須修改或刪除既有資料表、欄位或資料（破壞性動作） |
| 新套件 | 需要加入專案尚未使用的 NuGet 或 npm 套件（說明用途、建議的套件與版本、是否有內建或既有套件可替代，取得同意後依第 2.0 節加入） |
| 資料庫 | 無法連線，或版本低於 SQL Server 2019 |
| Migration | 產生的 Migration 含 `DropTable`、`DropColumn`、`AlterColumn`、`RenameColumn` 或 EF Core 提示可能遺失資料（新增功能不應出現） |
| 建置與驗證 | `dotnet build`、`npm run build` 或 Migration 失敗，自行修正 2 次仍失敗 |

工作目錄有其他未 commit 的變更不是紅燈，但於摘要中列出，提醒使用者本次變更會與其混在一起。

- **全部綠燈**：不詢問，輸出一次「採用規格摘要」（資料表與欄位含描述、權限與 Id、API 端點、前端路由與選單），直接進入第 2 節。
- **任一紅燈**：只列出紅燈項目、原因與建議值詢問；取得回覆後即開工，不再另外確認。

## 2. 建立

以下 `<Table>` 為資料表名稱（如 `Bulletin`）、`<Feature>` 為功能層名稱（如 `Bulletins`）、`<table>`／`<feature>` 為小寫。

### 2.0 新套件（只在第 1.2 節取得同意後）

優先使用 .NET、Vue 內建功能與專案既有套件；確定需要新套件時：

1. **來源與版本**：只從官方 NuGet／npm 取得；選用「發佈滿 14 天的最新穩定版」（不含預發佈版）。
   - NuGet：以 `curl -s https://api.nuget.org/v3-flatcontainer/<套件小寫>/index.json` 取得版本清單，再以 `curl -s https://api.nuget.org/v3/registration5-semver1/<套件小寫>/<版本>.json` 讀取 `published`。
   - npm：以 `npm view <套件> time --json` 取得各版本發佈時間。
   - 後端套件的主版本須與專案的 .NET 10、EF Core 10 相容。
2. **加入並鎖定版本**：
   - 後端：`dotnet add <backend 絕對路徑>\backend.csproj package <套件> --version <版本>`（`.csproj` 為明確版本號，不用浮動版本）。
   - 前端：`npm install <套件>@<版本> --save-exact --prefix <frontend 絕對路徑>`（`package.json` 為明確版本，`package-lock.json` 一併更新並進版控）；只在建置時使用的工具加 `--save-dev`。
3. **立即檢查弱點**（與發佈卡控相同標準）：
   - `dotnet list <backend 絕對路徑>\backend.csproj package --vulnerable --include-transitive`
   - `npm audit --audit-level=high --prefix <frontend 絕對路徑>`
   - 新套件或其相依套件有 High 或 Critical 弱點時不採用：改選其他版本或替代套件，再以紅燈詢問；不設例外清單。
4. 完成回報列出新增的套件、版本、發佈日期與弱點檢查結果。

### 2.1 後端

1. **Model**：`Models/<Table>/<Table>.cs`，namespace 維持 `backend.Models`，只描述欄位與導覽屬性；含 `CreatedAt`（`DateTime`）與 `UpdatedAt`（`DateTime?`）。
2. **AppDbContext**：
   - 新增 `public DbSet<<Table>> <Table> => Set<<Table>>();`（與既有 `DbSet` 寫法一致）。
   - `modelBuilder.Entity<<Table>>` 中以 `ToTable("<Table>", t => t.HasComment("描述"))` 加上資料表描述，**每個欄位**都以 `HasComment` 加上描述（主鍵「流水號」、外鍵註明指向，如「分類編號（BulletinCategory.Id）」、`CreatedAt`「建立時間」、`UpdatedAt`「最後修改時間」）。
   - 唯一值以 `HasIndex(...).IsUnique()`；可為 null 的欄位不建唯一索引，改建一般索引。外鍵設定 `OnDelete(DeleteBehavior.Restrict)`（除非規格另有指定）。
3. **權限**：
   - `Data/BasicPermissionNames.cs` 新增 4 個常數（`<Table>Read` 等）並加入 `All`。
   - `Data/SeedData.cs` 的 `BasicPermission` 陣列往後新增，Id 接續目前最大值，不改動既有 Id；`Description` 為「檢視／新增／編輯／刪除<功能名稱>」。系統管理員群組自動擁有全部權限、訪客群組自動擁有 `.read`，不需另外設定。
4. **DTO**（`DTOs/<Feature>/`，一個類別一個檔案，名稱以資料表開頭）：
   - `<Table>ListItem`（列表，含 `CreatedAt`、`UpdatedAt`）、`<Table>Detail`（編輯用，含 `CreatedAt`、`UpdatedAt`）、`<Table>SaveRequest`（新增與編輯共用；若新增與編輯欄位不同，改為 `<Table>CreateRequest`、`<Table>UpdateRequest`）。
   - 輸入 DTO 每個欄位加上驗證：`[Required(ErrorMessage = "請輸入…")]`、`[MaxLength(n, ErrorMessage = "…最多 n 個字")]`、文字欄位 `[NoHtml]`（拒絕 HTML 標籤）、數值 `[Range]`；訊息為繁體中文。不接收 `Id`、`CreatedAt`、`UpdatedAt` 等由後端決定的欄位。
5. **Service**（`Services/<Feature>/I<Feature>Service.cs` + `<Feature>Service.cs`）：
   - `GetPagedAsync(PagedQuery)`（關鍵字查詢、依 `Id` 排序、回傳 `PagedResult`）、`GetAsync(id)`（不存在丟 `BusinessException(NotFoundMessage, 404)`）、`CreateAsync(request, operatorId)`、`UpdateAsync(id, request, operatorId)`、`DeleteAsync(id, operatorId)`。
   - 新增或修改前先檢查唯一值，重複時丟 `BusinessException`；外鍵 Id 先確認存在；刪除前確認沒有被其他資料參照，有則丟 `BusinessException` 說明原因。
   - 新增時設 `CreatedAt = DateTime.Now`；修改時**一律**設 `UpdatedAt = DateTime.Now`（含只改關聯表的修改，異動歷程查詢依此找出該筆資料）。
   - 文字欄位 `Trim()`；選填文字為空白時存 `null`。
   - 主表與關聯表透過導覽屬性一起加入，只呼叫一次 `SaveChangesAsync`（不可用同步 `SaveChanges`，也不可用 `ExecuteUpdate`／`ExecuteDelete` 做業務異動，否則不會留下異動歷程）。
   - Log 使用結構化參數（如 `logger.LogInformation("新增公告：{BulletinId}，操作者：{OperatorId}", ...)`），不記錄個資或機密。
   - 需要資料層級權限時（如只能修改自己建立的資料），以 `operatorId` 檢查，無權讀取的資料回「不存在」。
6. **Controller**（`Controllers/<Feature>/<Feature>Controller.cs`）：
   - `[ApiController]`、`[Route("api/<feature>")]`、`[Authorize]`；每個端點標註 `[Authorize(Policy = BasicPermissionNames.<Table>Read/Create/Update/Delete)]`。
   - 只接收 DTO，回傳 `ApiResponse<T>`；分頁用 `ApiResponse.Paged`；操作者以 `User.GetBasicUserId()` 傳入 Service。
   - 異動歷程端點：`[HttpGet("{id:int}/basicsyslogs")]`、Policy 為 `<Table>Update`，呼叫 `basicSyslogHistoryService.GetRecordHistoryAsync(nameof(<Table>), id, 關聯表...)`，回傳 `ApiResponse<BasicSyslogHistoryResult>`。
7. **Program.cs**：`builder.Services.AddScoped<I<Feature>Service, <Feature>Service>();`（加在既有業務服務之後）。
8. **機密欄位**（有時）：加入 `Data/BasicSyslogInterceptor.cs` 的 `IgnoredProperties`（`"<Table>.<欄位>"`），且不放入任何回傳 DTO。

### 2.2 前端

1. `src/views/<Feature>/`：
   - `types.ts`：`<Table>ListItem`、`<Table>Detail`、`<Table>FormData`（與後端 DTO 同名，屬性 camelCase；`updatedAt: string | null`）。
   - `api.ts`：`get<Feature>(query)`、`get<Table>(id)`、`get<Table>BasicSyslogs(id)`（回傳 `BasicSyslogHistoryResult`）、`create<Table>(form)`、`update<Table>(id, form)`、`delete<Table>(id)`。
   - `routes.ts`：path 與 name 為 `<feature>`，`meta: { title: '<功能名稱>', basicPermission: '<table>.read' }`。
   - `components/<Table>FormModal.vue`：新增與編輯共用，`defineModel` 控制開關、`item` prop（`null` 為新增），開啟時重設表單，送出以事件交回 `Index.vue`；前端檢查只為使用體驗，長度上限與後端一致。
   - `Index.vue`：比照 `BasicUsers/Index.vue`——查詢列、表格、分頁；「異動時間」欄以 `formatModifiedAt(item)` 顯示；操作欄依 `update`／`delete` 權限顯示「編輯」「歷程」「刪除」；「歷程」以 `BasicSyslogModal` 顯示；操作結果以訊息視窗顯示，catch 只在 `!isHandled(error)` 時補顯示「操作失敗，請再試一次」。
   - 一律以 `{{ }}` 輸出，不使用 `v-html`；站內路徑以 `import.meta.env.BASE_URL` 組成。
2. `src/router/index.ts` 匯入並加入路由；`src/layouts/menu.ts` 加入選單項目（`basicPermission: '<table>.read'`，首頁卡片同步出現）。

### 2.3 Migration

1. `dotnet ef migrations add Add<Table> --project <backend 絕對路徑>\backend.csproj --startup-project <backend 絕對路徑>\backend.csproj --output-dir Data/Migrations`。
2. 讀過產生的 Migration：只應有 `CreateTable`／`CreateIndex`、權限與群組權限的 `InsertData`；出現第 1.2 節列出的破壞性操作或明文機密即為紅燈。
3. `dotnet ef database update --project <backend 絕對路徑>\backend.csproj --startup-project <backend 絕對路徑>\backend.csproj`（套用至開發資料庫）。
4. 以 `sqlcmd` 唯讀查詢 `sys.extended_properties`，確認新資料表與每個欄位都有 `MS_Description`。

### 2.4 文件

更新專案 `CLAUDE.md`：種子權限筆數、功能清單（如有）；有特殊規則時（資料層級權限、刪除限制）簡短記錄。

## 3. 驗證

- `dotnet build`、`npm run build` 皆須成功。
- 背景啟動後端（`dotnet run --project <backend 絕對路徑> -- --Security:Login:CaptchaEnabled=false`），以 `curl` 只做**不寫入資料**的請求：
  - 預設管理員：`GET /api/<feature>` 回 200 且格式為分頁 `ApiResponse`；`GET /api/<feature>/{不存在的 id}` 回 404；`GET /api/<feature>/{id}/basicsyslogs` 回 200；`POST /api/<feature>` 送 `{}` 回 400 與中文驗證訊息；文字欄位含 `<b>` 時回 400。
  - 預設一般使用者（訪客）：`GET /api/<feature>` 回 200；`POST` 回 403；`GET .../basicsyslogs` 回 403。
  - 無 Token 回 401。
- **寫入實測**（固定步驟，只在開發資料庫執行）：
  - 前提：連線字串的伺服器為本機（`localhost`、`127.0.0.1`、`.`、`(local)`）；不是本機時為紅燈，詢問是否為開發資料庫，未確認前不執行寫入實測，列為「未實測」。
  - 測試資料一律經由 API 寫入（不以 `sqlcmd` 異動資料），文字欄位以 `[測試]` 開頭；需要外鍵資料時先以既有資料測試，沒有時同樣經由 API 建立並於最後刪除。
  - 依序執行並確認結果（皆以預設管理員）：
    1. `POST` 新增一筆 → 200，取得 Id。
    2. `GET {id}` → 200，欄位值與送出的相同，`updatedAt` 為 null；`GET` 列表查詢 `[測試]` 可找到。
    3. 有唯一欄位時，`POST` 相同值 → 400 與「已存在」訊息；有外鍵時，`POST` 不存在的外鍵 Id → 400。
    4. `PUT {id}` 修改一個欄位 → 200；`GET {id}` 確認已更新且 `updatedAt` 有值。
    5. 以預設一般使用者（訪客）`PUT {id}`、`DELETE {id}` → 403。
    6. `GET {id}/basicsyslogs` → 有新增（`C`）與修改（`U`）兩筆，修改只列出變動的欄位。
    7. 有刪除限制時，先建立參照資料確認 `DELETE` 回 400，再刪除參照資料。
    8. `DELETE {id}` → 200；`GET {id}` → 404；`GET {id}/basicsyslogs` 多一筆刪除（`D`）。
  - 清理：測試建立的資料一律經由 API 刪除；中途失敗時同樣先刪除已建立的測試資料再回報。無法刪除時於回報列出殘留資料的資料表與 Id。
  - 異動歷程（`BasicSyslog`）中的測試紀錄保留不刪（稽核紀錄），於回報註明其時間範圍，可由 `[測試]` 辨識。
- 驗證完即停止背景後端。
- 執行 `owasp-check`，範圍為本次變更；發現問題直接修正後重新建置，涉及第 1.2 節紅燈條件時才詢問。

## 4. 完成回報

- 新增與修改的檔案（後端、前端、Migration）；有新套件時列出套件、版本、發佈日期與弱點檢查結果。
- 資料表與欄位、權限名稱與 Id、API 端點、前端路由。
- 驗證結果（含寫入實測各步驟的結果）、未實測項目，以及測試資料的清理狀況與 `BasicSyslog` 中測試紀錄的時間範圍。
- `owasp-check` 結果。
- 部署提醒：本次新增 Migration `Add<Table>`，正式資料庫須先備份再套用（程式啟動時不會自動套用）。
- 尚未 commit：請確認後自行 commit，再以 `/publish-project` 發佈。
