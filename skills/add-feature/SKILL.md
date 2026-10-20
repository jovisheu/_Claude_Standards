---
name: add-feature
description: 在依 _Claude_Standards 建立的既有專案中新增業務功能（資料表、權限、後端 API、前端管理頁面、Migration）。在使用者要求新增功能、資料表或管理頁面時使用；比照範本的使用者管理（BasicUsers）實作，完成後驗證並執行 owasp-check；不 commit、不發佈。全部綠燈時直接建立不詢問，只有紅燈項目才詢問。
---

# 新增業務功能

## 用途

- **做什麼**：在既有專案加入一個完整的資料管理功能：資料表與 Migration、權限種子、後端 Model／DTO／Service／Controller（含異動歷程端點）、前端列表與新增／編輯頁面、路由與選單；完成後驗證並執行 `owasp-check`。
- **何時使用**：專案需要新的資料表或管理頁面時（如公告、分類、訂單）。專案須由 `/new-project` 建立或符合同樣的結構。
- **不適用**：替既有資料表加欄位（用 `/alter-table`）；修改或刪除既有資料表、欄位或資料（屬破壞性變更，會停下詢問）；登入、密碼等基本功能的調整（直接依通用規範與 `auth-baseline.md` 修改）；非資料管理類的功能（如報表、排程），可參考本 skill 的規則但需另行設計。
- **使用方式**：`/add-feature <功能名稱>(<欄位> <型別> [必填] [唯一]、...)`，例：`/add-feature 公告(標題 nvarchar(100) 必填、內容 nvarchar(max)、發布日期 date)`；規格不足時只詢問缺少的部分。
- **產出**：新增的前後端程式與 Migration `Add<Table>`（已套用至開發資料庫）、更新的專案 `CLAUDE.md` 與完成回報；**不 commit、不發佈**，完成後請自行 commit 再 `/publish-project`。

規則來源：`D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\CLAUDE.md`（通用規範）與同目錄 `rules\` 下的 `backend-data.md`、`frontend-ui.md`。先完整讀過，再讀專案的 `CLAUDE.md`；涉及登入、密碼或帳號時另讀 `references\auth-baseline.md`。所有程式碼須符合通用規範的命名、目錄、資安、API 格式與資料庫版本（SQL Server 2019）規定。

實作一律比照專案內既有的使用者管理（`BasicUsers`）：後端 `Models/BasicUser/`、`DTOs/BasicUsers/`、`Services/BasicUsers/`、`Controllers/BasicUsers/`，前端 `src/views/BasicUsers/`。先讀過這些檔案再撰寫，寫法、註解密度與 UI 樣式與其一致；不參考其他專案。

## 0. 指令寫法

一律依 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\references\command-style.md`；第 3 節的寫入實測一律經由 API 進行。

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
| 選單位置 | 功能 `routes.ts` 的 `meta.menu.order` 接續既有最大值（排在最後） |

型別對應：文字 `string` + `HasMaxLength`（未指定長度時預設 100；`nvarchar(max)` 不加長度且不建索引）、整數 `int`、金額 `decimal` + `HasPrecision(18, 2)`、日期 `DateOnly`、日期時間 `DateTime`、是否 `bool`；非必填用可為 null 的型別。

### 1.2 紅綠燈

燈號處理與共用紅燈條件（破壞性動作、機密欄位、新套件、資料庫、Migration、建置與驗證）見 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\references\red-lights.md`；本 skill 另有：

| 項目 | 紅燈條件（停下詢問） |
| --- | --- |
| 專案結構 | 缺少 `backend/Data/AppDbContext.cs`、`Data/BasicPermissionNames.cs`、`Data/SeedData.cs`、`Services/BasicSyslogs/IBasicSyslogHistoryService.cs`、`frontend/src/components/BasicSyslogModal.vue` 等範本結構（非依規範建立的專案） |
| 規格 | 無法判斷欄位、型別、長度或關聯 |
| 名稱 | 資料表、`DbSet`、功能資料夾、API 路徑、權限名稱或前端路由已存在；名稱不可作為識別字；以 `Basic` 開頭 |
| 關聯 | 外鍵指向的資料表不存在 |
| Migration | 新增功能的 Migration 不應有任何 `AlterColumn`（共用條件中的「只因描述變更」例外不適用） |

全部綠燈時輸出的「採用規格摘要」：資料表與欄位含描述、權限與 Id、API 端點、前端路由與選單，然後直接進入第 2 節。

## 2. 建立

以下 `<Table>` 為資料表名稱（如 `Bulletin`）、`<Feature>` 為功能層名稱（如 `Bulletins`）、`<table>`／`<feature>` 為小寫。

### 2.0 新套件（只在第 1.2 節取得同意後）

讀取 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\skills\add-package\SKILL.md`，依其第 2～4 節選版本、加入並檢查弱點（同意已於第 1.2 節取得）；有 High 或 Critical 弱點時改選其他版本或替代套件，再以紅燈詢問。完成回報列出新增的套件、版本、發佈日期與弱點檢查結果。

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
   - 主表與關聯表透過導覽屬性一起加入，只呼叫一次 `SaveChangesAsync`（不可用同步 `SaveChanges`，也不可用 `ExecuteUpdate`／`ExecuteDelete`／`ExecuteSql` 做異動：不會留下異動歷程，執行時會被 `BasicSyslogCommandGuard` 拒絕）。大量資料同樣載入實體後分批 `SaveChangesAsync`。
   - Log 使用結構化參數（如 `logger.LogInformation("新增公告：{BulletinId}，操作者：{OperatorId}", ...)`），不記錄個資或機密。
   - 需要資料層級權限時（如只能修改自己建立的資料），以 `operatorId` 檢查，無權讀取的資料回「不存在」；`GetPagedAsync`／`GetAsync` 也接收 `operatorId`，並在 `<Table>ListItem`／`<Table>Detail` 回傳 `IsManageable`（比照 `BasicUsers` 的 `IsManageable`／`IsSelf`），前端據此以 `v-if` 不渲染「編輯」「刪除」。
6. **Controller**（`Controllers/<Feature>/<Feature>Controller.cs`）：
   - `[ApiController]`、`[Route("api/<feature>")]`、`[Authorize]`；每個端點標註 `[Authorize(Policy = BasicPermissionNames.<Table>Read/Create/Update/Delete)]`。
   - 只接收 DTO，回傳 `ApiResponse<T>`；分頁用 `ApiResponse.Paged`；操作者以 `User.GetBasicUserId()` 傳入 Service。
   - 寫入端點標上異動歷程預設說明：`[BasicSyslogDescription("新增<功能名稱>")]`、`("編輯<功能名稱>")`、`("刪除<功能名稱>")`；規格含審核、狀態變更等需依結果區分的動作時，Service 於 `SaveChangesAsync` 前呼叫 `basicSyslogContext.Describe(...)`（如「審核通過」「退回：<原因代碼>」）。
   - 異動歷程端點：`[HttpGet("{id:int}/basicsyslogs")]`、Policy 為 `<Table>Update`，呼叫 `basicSyslogHistoryService.GetRecordHistoryAsync(nameof(<Table>), id, 關聯表...)`，回傳 `ApiResponse<BasicSyslogHistoryResult>`。
7. **Program.cs**：`builder.Services.AddScoped<I<Feature>Service, <Feature>Service>();`（加在既有業務服務之後）。
8. **機密欄位**（有時）：加入 `Data/BasicSyslogInterceptor.cs` 的 `MaskedProperties`（`"<Table>.<欄位>"`），且不放入任何回傳 DTO。

### 2.2 前端

1. `src/views/<Feature>/`：
   - `types.ts`：`<Table>ListItem`、`<Table>Detail`、`<Table>FormData`（與後端 DTO 同名，屬性 camelCase；`updatedAt: string | null`）。
   - `api.ts`：`get<Feature>(query)`、`get<Table>(id)`、`get<Table>BasicSyslogs(id)`、`create<Table>(form)`、`update<Table>(id, form)`、`delete<Table>(id)`；一律使用 `src/api/http.ts` 的 `get`／`post`／`put`／`del<T>`，回傳 `ApiResponse<T>`（資料取 `.datas`）。
   - `routes.ts`：path 與 name 為 `<feature>`，`meta: { title: '<功能名稱>', basicPermission: '<table>.read', menu: { title: '<功能名稱>', description: '<一句說明>', order: <接續最大值> } }`。
   - `components/<Table>FormModal.vue`：新增與編輯共用，`defineModel` 控制開關、`item` prop（`null` 為新增），開啟時重設表單，送出以事件交回 `Index.vue`；前端檢查只為使用體驗，長度上限與後端一致。
   - `Index.vue`：比照 `BasicUsers/Index.vue`——查詢列、表格、分頁；「異動時間」欄以 `formatModifiedAt(item)` 顯示；操作欄依 `update`／`delete` 權限以 `v-if` 渲染「編輯」「歷程」「刪除」（無權限或資料受保護時不渲染，不用 `disabled`／`v-show`，見 `rules\frontend-ui.md`；兩種權限都沒有時整個操作欄不渲染）；「歷程」以 `BasicSyslogModal` 顯示（傳入 `:load="() => get<Table>BasicSyslogs(id)"`，元件自行載入）；操作結果以訊息視窗顯示，catch 只在 `!isHandled(error)` 時補顯示「操作失敗，請再試一次」。
   - 一律以 `{{ }}` 輸出，不使用 `v-html`；站內路徑以 `import.meta.env.BASE_URL` 組成。
2. 不需修改 `src/router/index.ts` 與 `src/layouts/menu.ts`：`src/router/featureRoutes.ts` 自動載入各功能的 `routes.ts`，`meta.menu` 同時產生側邊選單與首頁功能卡片（依 `meta.basicPermission` 過濾）。
   - 專案沒有 `src/router/featureRoutes.ts`（以舊版範本建立）時，沿用該專案的寫法：`src/router/index.ts` 匯入並加入路由、`src/layouts/menu.ts` 加入選單項目（首頁卡片同步出現）；`api.ts` 與 `BasicSyslogModal` 的用法也比照該專案既有的 `BasicUsers`。

### 2.3 Migration

1. `dotnet ef migrations add Add<Table> --project <backend 絕對路徑>\backend.csproj --startup-project <backend 絕對路徑>\backend.csproj --output-dir Data/Migrations`。
2. 讀過產生的 Migration：只應有 `CreateTable`／`CreateIndex`、權限與群組權限的 `InsertData`；出現第 1.2 節列出的破壞性操作或明文機密即為紅燈。產生後 `hooks/check-migration.js` 會自動回報 `Up()` 中的破壞性操作、原生 SQL 與缺少描述的資料表／欄位；缺少描述時補上 `HasComment`，以 `dotnet ef migrations remove` 移除後重新產生。
3. `dotnet ef database update --project <backend 絕對路徑>\backend.csproj --startup-project <backend 絕對路徑>\backend.csproj`（套用至開發資料庫）。
4. 以 `sqlcmd` 唯讀查詢 `sys.extended_properties`，確認新資料表與每個欄位都有 `MS_Description`。

### 2.4 文件

更新專案 `CLAUDE.md`：種子權限筆數、功能清單（如有）；有特殊規則時（資料層級權限、刪除限制）簡短記錄。

## 3. 驗證

- `dotnet build`、`npm run build` 皆須成功。
- 背景啟動後端（`dotnet run --project <backend 絕對路徑> -- --Security:Login:CaptchaEnabled=false`），等待 log 出現 `Now listening` 後，以 API 驗證腳本檢查（預設管理員與預設一般使用者的帳密依專案 CLAUDE.md 或 new-project 設定）：

  ```bash
  node D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\scripts\api-smoke.js --base http://localhost:<port> --feature <feature> --admin <帳號>:<密碼> --guest <帳號>:<密碼> --sample '<新增用 JSON>' --update '<修改的欄位 JSON>' [--unique <欄位>] [--fk <欄位>] [--text-field <欄位>] [--write]
  ```

  - `--sample` 為一筆合法的新增資料（`<Table>SaveRequest` 的 JSON），`--update` 為修改時變更的欄位；有唯一欄位時加 `--unique`、有外鍵時加 `--fk`；`--text-field` 預設為 `--sample` 中第一個文字欄位（用於 `[測試]` 前綴、HTML 與關鍵字檢查）。
  - 未加 `--write` 時只做**不寫入資料**的檢查：無 Token 401；管理員列表 200 且為分頁 `ApiResponse`、不存在的 Id 404、歷程 200、送 `{}` 400、文字含 `<b>` 400；訪客列表 200、`POST` 403、歷程 403。
- **寫入實測**（加 `--write`，只在開發資料庫執行）：
  - 前提：連線字串的伺服器為本機（`localhost`、`127.0.0.1`、`.`、`(local)`）；不是本機時為紅燈，詢問是否為開發資料庫，未確認前不加 `--write`，列為「未實測」。
  - 腳本依序執行：新增 → 讀回比對與 `updatedAt` 為 null、列表查詢 `[測試]` → 唯一值／外鍵 400 → 修改後讀回與 `updatedAt` 有值 → 訪客 `PUT`／`DELETE` 403 → 歷程有 `C` 與 `U`、皆有業務說明、修改只列出變動欄位 → 刪除 200、讀回 404、歷程有 `D`。測試資料經由 API 寫入，文字欄位以 `[測試]` 開頭，中途失敗時先刪除已建立的資料。
  - 腳本未涵蓋：刪除限制（有時先經由 API 建立參照資料，確認 `DELETE` 回 400 後再刪除參照資料）；需要外鍵資料時先以既有資料測試，沒有時同樣經由 API 建立並於最後刪除。
  - 腳本列出的殘留資料（無法刪除時）與 `BasicSyslog` 測試紀錄的時間範圍（保留不刪，為稽核紀錄）寫入完成回報。
- 腳本任一項失敗（結束代碼 1）時修正後重跑；驗證完即停止背景後端。
- 執行 `owasp-check`，範圍為本次變更；發現問題直接修正後重新建置，涉及第 1.2 節紅燈條件時才詢問。

## 4. 完成回報

- 新增與修改的檔案（後端、前端、Migration）；有新套件時列出套件、版本、發佈日期與弱點檢查結果。
- 資料表與欄位、權限名稱與 Id、API 端點、前端路由。
- 驗證結果（含寫入實測各步驟的結果）、未實測項目，以及測試資料的清理狀況與 `BasicSyslog` 中測試紀錄的時間範圍。
- `owasp-check` 結果。
- 部署提醒：本次新增 Migration `Add<Table>`，正式資料庫須先備份再套用（程式啟動時不會自動套用）。
- 尚未 commit：請確認後自行 commit，再以 `/publish-project` 發佈。
