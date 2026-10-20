# _Claude_Standards

跨專案共用的開發規範與 Skill。維護本資料夾的規範或 Skill 時先讀本檔；本檔不會自動載入。

## 檔案

| 路徑 | 說明 | 載入時機 |
| --- | --- | --- |
| `CLAUDE.md` | 通用規範 | 各專案 CLAUDE.md 匯入，每次對話載入 |
| `references/auth-baseline.md` | 登入、密碼、忘記密碼、前端登入狀態的實作細節 | 建立或修改這些功能、資安檢查時 |
| `references/logging-baseline.md` | 系統錯誤紀錄（`BasicExceptionlog`）與異動歷程（`BasicSyslog`）的欄位、保留、轉存、歷程查詢 | 不使用範本建立專案、修改這些機制、資安檢查時 |
| `references/basic-tables.md` | 基本資料表欄位與種子資料（唯一定義來源） | `new-project` 建立專案、修改基本資料表時 |
| `rules/backend-data.md` | 異動歷程（`BasicSyslog`）寫入規則、資料表規則、Service 層寫法（自 `CLAUDE.md` 移出） | 以 junction 掛到 `%USERPROFILE%\.claude\rules`，frontmatter `paths` 為 `**/backend/**/*.cs`、`**/*.sql`：讀寫符合的檔案時自動載入（工作階段啟動時掃描，新增 rule 須開新工作階段才生效）；`add-feature`、`alter-table`、`new-project`、`owasp-check` 以 Read 明確讀取 |
| `rules/frontend-ui.md` | 前端 UI 慣例（自 `CLAUDE.md` 移出） | 同上，`paths` 為 `**/frontend/src/**/*.{vue,ts}` |
| `references/red-lights.md` | 共用燈號處理與紅燈條件 | `add-feature`、`alter-table` 判斷紅綠燈時 |
| `references/command-style.md` | Skill 執行時的指令寫法（減少權限詢問） | 各共用 Skill 執行時 |
| `skills/<名稱>/SKILL.md` | 共用 Skill | 執行該 Skill 時 |
| `hooks/check-standards.js` | 規範檢查腳本（`UtcNow`、同步 `SaveChanges`、`ExecuteUpdate／Delete／Sql`、`Random`、SQL／Log 字串插值、`AllowAnyOrigin`、不安全反序列化、`System.Net.Mail`（郵件須用 MailKit）、SQL Server 2022 語法、`UseCompatibilityLevel(150)`、Controller 缺 `[Authorize]`、Models namespace、空 catch、`v-html`、寫死根路徑、`.csproj`／`package.json` 浮動版本、`appsettings` 明文連線字串、`.ps1` BOM、`.env` 非 ASCII 與機密） | 使用者層級 `settings.json` 的 PostToolUse（Write\|Edit）hook，寫檔後自動執行：Write 檢查整個檔案，Edit 只檢查修改的片段（整檔結構規則仍檢查整個檔案）；只檢查 `Jovi\Localhost` 下的檔案，有違規時回報、不阻擋。也可直接執行 `node hooks/check-standards.js <檔案或目錄...>`，目錄會遞迴掃描（`owasp-check` 使用） |
| `hooks/guard-commands.js` | 指令護欄：`sqlcmd` 含異動關鍵字（`INSERT`、`UPDATE`、`DELETE`、`DROP`、`EXEC`、`INTO`、`sp_*` 等，含 `-i` 輸入檔內容）、`npm audit fix --force`；只檢查實際執行 `sqlcmd`／`npm` 的指令段（依引號外的 `;`、`&&`、`||`、換行、管線切分），commit 訊息等文字提到這些字不會誤擋 | 使用者層級 `settings.json` 的 PreToolUse（Bash\|PowerShell）hook，執行前以 `deny` 擋下並說明原因；適用所有目錄。可直接執行 `node hooks/guard-commands.js "<指令>"` 確認判斷 |
| `hooks/check-migration.js` | Migration 檢查：只檢查 `Up()` 的破壞性操作（`DropTable`、`DropColumn`、`Rename*`、`AlterColumn`、`DeleteData` 等）、`migrationBuilder.Sql` 原生 SQL、資料表與欄位缺少描述（`comment:`） | 使用者層級 `settings.json` 的 PostToolUse（Bash\|PowerShell）hook，指令為 `dotnet ef migrations add` 時檢查 10 分鐘內產生的最新 Migration（依 `--project`、`--output-dir` 推得目錄），有問題時回報、不阻擋。也可直接執行 `node hooks/check-migration.js <Migration 檔或目錄...>` |
| `scripts/api-smoke.js` | API 驗證腳本：登入管理員與訪客，檢查一個功能的 401／403／404／400、分頁格式、拒絕 HTML、異動歷程；加 `--write` 執行寫入實測（新增→讀回→唯一值／外鍵→修改→訪客 403→歷程 C／U→刪除→歷程 D），失敗時清理測試資料；任一項失敗時結束代碼為 1 | `add-feature`、`alter-table` 第 3 節，`new-project` 第 4 節（各功能，不加 `--write`），`upgrade-packages`、`verify-template` 的登入與權限檢查（不加 `--write`）；用法見腳本開頭 |
| `scripts/project-smoke.js` | 專案驗證腳本（不寫入資料）：回應標頭、401／404／415、JSON 格式錯誤、缺少 Body、模型驗證、驗證碼圖片與驗證碼錯誤；提供管理員與訪客帳密時檢查登入、`/api/auth/me` 與訪客權限；最後連續登入請求直到 429（`--skip-basic`、`--skip-rate-limit` 可略過）；任一項失敗時結束代碼為 1 | `new-project` 第 4 節（驗證碼啟用時執行基本檢查與 429，關閉驗證碼重啟後執行登入檢查）；用法見腳本開頭 |
| `hooks/test/` | 刻意違規的測試樣本（每條規則各一例；`Migrations/` 為 `check-migration.js` 的樣本） | 修改檢查腳本後以 `node hooks/check-standards.js hooks/test` 確認每條規則仍能抓到，再以 `node hooks/check-standards.js skills/new-project/templates/project` 確認範本沒有誤判 |
| `skills/new-project/templates/project/` | 新專案範本（後端、前端、UI 樣式），專案特有值以 `__名稱__` 佔位符表示 | `new-project` 複製建立專案 |
| `skills/new-project/templates/keygen/` | 產生 AES 金鑰／IV、JWT 金鑰、管理員密碼雜湊的一次性工具 | `new-project` 複製到 scratchpad 執行 |
| `_Project_Info/Project_Info.txt` | 新專案的答案卷（專案名稱、用途必填，其餘選填） | 只在 `new-project` 建立專案時 |
| `_Project_Info/DB_DefaultConnection.txt` | 預設資料庫連線字串（明文密碼） | 只在 `new-project` 建立專案時 |
| `_Project_Info/SendMail_Info.txt` | 預設寄信設定（明文密碼） | 只在 `new-project` 建立專案時 |

`_Project_Info/` 內的檔案含明文密碼，不得複製進專案或納入任何版控（本資料夾納入 git 時須將 `_Project_Info/` 加入 `.gitignore`）。

## 規範與 Skill 的分工

* **CLAUDE.md**（本資料夾與各專案 CLAUDE.md）：每次對話都會載入，只放「寫程式時一律遵守」的規則與專案架構。專案 CLAUDE.md 以 `@D:/Jovi/Localhost/_ClaudeCode/_Claude_Standards/CLAUDE.md` 匯入（本資料夾不在專案上層目錄，不會自動載入）。
  * 只在特定流程用得到的內容（建立骨架、發佈、檢查步驟、一次性的基礎設定），放到 Skill 或 `references/`，CLAUDE.md 只留原則與指向。
  * 範本已實作、一般寫程式不會碰到的機制細節（如紀錄轉存、歷程查詢），放 `references/`，CLAUDE.md 只留「寫新功能時須遵守」的部分。
  * 專案 CLAUDE.md 已匯入通用規範，只記錄專案特有的值與設定，不重寫通用規則。
* **Skill**：需要時才載入，放「偶爾執行、步驟固定」的流程（檢查、發佈、測試、新增功能）。
  * 專案專用：`<專案>/.claude/skills/<名稱>/SKILL.md`，隨專案進版控。
  * 跨專案共用：實體檔放在 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\skills\<名稱>\`，再以 junction 掛到 `%USERPROFILE%\.claude\skills\<名稱>`（PowerShell：`New-Item -ItemType Junction -Path <連結> -Target <實體目錄>`，不需管理員權限）。
* **rules/**：寫程式時一律遵守、但只和前端或後端其中一邊有關的規則，以 `paths` 條件載入，減少每次對話的載入量。`CLAUDE.md` 對應位置保留一行摘要與「未載入時先讀」的指向（使用者層級 rules 的 glob 起算點未見官方文件說明，以此作為保險）。`paths` 只比對專案內的相對路徑，其他專案有符合的檔案時同樣會載入。
  * 共用 Skill 以 Read 讀取 `CLAUDE.md` 時不會帶入 rules，Skill 的「規則來源」須一併列出需要的 rule 檔。
* **references/**：多個 Skill 共用、或修改特定功能時才需要的細節規範。

## 新專案範本維護

* 範本由 TestProject 整理而來，`new-project` 預設複製範本建立專案，確保各專案的程式結構與 UI 一致；要調整新專案的 UI 或共用程式時，直接修改範本。
* `_Project_Info/Project_Info.txt` 的 `使用範本` 填「否」時，改依 `skills/new-project/no-template.md` 依規範重新產生程式（不複製範本，UI 可能與其他專案不同）。該檔記錄了重新產生時容易出錯的地方（TypeScript 7、狀態碼格式、`web.config` 等），規範變更時一併檢查。
* 範本不含 `bin/`、`obj/`、`node_modules/`、`dist/`、`wwwroot/`、`publish/`、`Data/Migrations/`（Migration 於建立專案時產生）。
* 新增佔位符時，同步更新 `new-project` SKILL.md 第 3 節的佔位符表。
* 修改範本後執行 `/verify-template`（複製到 scratchpad、以測試值替換佔位符、建置、發佈並啟動檢查回應格式）。
* 規範新增可機械判斷的規則時（固定樣式即可判斷違規），同步加入 `hooks/check-standards.js` 與 `hooks/test/` 樣本，並以範本確認沒有誤判；`owasp-check` 第 1 節的 Grep 表只列腳本未涵蓋的樣式，規則移入腳本時一併從表中移除。
* 禁止執行的指令（寫在規範或 `command-style.md`、固定樣式即可判斷）優先加入 `hooks/guard-commands.js`；只能整體封鎖的指令才用 `settings.json` 的 `deny`／`ask`（使用者層級規則會影響所有專案，如 python 不封鎖，因其他 Skill 需要）。
* 使用者層級 `settings.json` 另將 `Jovi\Localhost` 下的 `publish.ps1` 修改設為 `ask`（規範禁止修改腳本繞過弱點卡控；維護範本的 `publish.ps1` 時同樣會詢問）。
* 範本根目錄的 `dotnet-tools.json` 鎖定 CycloneDX 工具版本（`publish.ps1` 產生 SBOM 用），作為無法連線 NuGet 時的預設值；`new-project` 建立專案時會改選發佈滿 14 天的最新穩定版並鎖定於該專案。升級範本時以 `dotnet tool update CycloneDX --version <版本> --tool-manifest <範本>\dotnet-tools.json` 更新（同樣須發佈滿 14 天），並重新驗證發佈。
* 套件版本固定在範本的 `backend.csproj` 與 `frontend/package-lock.json`；定期（建議每月）在本資料夾執行 `/upgrade-packages`，升級後依 `verify-template` 驗證。`keygen.csproj` 的 BCrypt 版本須與 `backend.csproj` 一致。
* 規範（`CLAUDE.md`、`references/`）變更時，同步檢查範本是否仍符合。

## 目前共用 Skill

* `new-project`：預設複製新專案範本並替換佔位符建立新專案；`使用範本` 填「否」時依規範重新產生。設定取自 `_Project_Info/` 與預設值，採紅綠燈判斷：全部綠燈直接建立不詢問，只有紅燈項目（環境缺漏、目錄不為空、資料庫有既有資料表、建置失敗等，見 Skill 第 2 節）才詢問；驗證與 `owasp-check` 後依 `publish-project` 流程發佈一次並測試發佈結果（略過 commit 檢查）；完成後不 commit，只提醒。
* `add-feature`：在既有專案新增業務功能（資料表、權限、後端 API、前端管理頁面、Migration），比照範本的使用者管理實作；紅綠燈判斷同 `new-project`，完成後以不寫入資料的請求驗證並執行 `owasp-check`，不 commit、不發佈。`new-project` 建立時的業務功能（第 23 項）同樣依此 skill 第 2 節建立。
* `alter-table`：替既有業務資料表新增欄位（Model、描述、DTO、Service、前端、Migration）；修改型別、更名、刪除欄位為紅燈；不 commit、不發佈。
* `upgrade-packages`：盤點並升級有弱點或過期的套件（專案或範本），主版本升級為紅燈；範本升級後接 `verify-template`。
* `verify-template`：範本修改後在 scratchpad 建立測試專案驗證（設有 `disable-model-invocation`，只在使用者要求或 `upgrade-packages` 讀取其 SKILL.md 時執行）。
* `add-package`：新增 NuGet／npm 套件（確認無替代、取得同意、發佈滿 14 天的穩定版、鎖定版本、立即檢查 High／Critical 弱點）；`add-feature` 需要新套件時讀取其 SKILL.md 沿用。
* `publish-project`：發佈專案並整理部署檢查清單（只在使用者明確要求時執行；設有 `disable-model-invocation`，`new-project` 以讀取其 SKILL.md 的方式沿用流程）。
* `owasp-check`：依通用規範的資安規範與 `references/auth-baseline.md` 檢查專案。
