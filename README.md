# _Claude_Standards

跨專案共用的開發規範與 Skill。維護本資料夾的規範或 Skill 時先讀本檔；本檔不會自動載入。

## 檔案

| 路徑 | 說明 | 載入時機 |
| --- | --- | --- |
| `CLAUDE.md` | 通用規範 | 各專案 CLAUDE.md 匯入，每次對話載入 |
| `references/auth-baseline.md` | 登入、密碼、忘記密碼、前端登入狀態的實作細節 | 建立或修改這些功能、資安檢查時 |
| `references/logging-baseline.md` | 系統錯誤紀錄（`BasicExceptionlog`）與異動歷程（`BasicSyslog`）的欄位、保留、轉存、歷程查詢 | 不使用範本建立專案、修改這些機制、資安檢查時 |
| `references/basic-tables.md` | 基本資料表欄位與種子資料（唯一定義來源） | `new-project` 建立專案、修改基本資料表時 |
| `references/command-style.md` | Skill 執行時的指令寫法（減少權限詢問） | 各共用 Skill 執行時 |
| `skills/<名稱>/SKILL.md` | 共用 Skill | 執行該 Skill 時 |
| `hooks/check-standards.js` | 規範檢查腳本（`UtcNow`、同步 `SaveChanges`、`ExecuteUpdate／Delete`、`Random`、SQL／Log 字串插值、`v-html`、寫死根路徑、浮動版本、`.ps1` BOM、`.env` 非 ASCII） | 使用者層級 `settings.json` 的 PostToolUse（Write\|Edit）hook，寫檔後自動執行；只檢查 `Jovi\Localhost` 下的檔案，有違規時回報、不阻擋 |
| `hooks/test/` | 刻意違規的測試樣本（每條規則各一例） | 修改檢查腳本後以 `node hooks/check-standards.js hooks/test/Sample.cs ...` 確認仍能抓到 |
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
* **references/**：多個 Skill 共用、或修改特定功能時才需要的細節規範。

## 新專案範本維護

* 範本由 TestProject 整理而來，`new-project` 預設複製範本建立專案，確保各專案的程式結構與 UI 一致；要調整新專案的 UI 或共用程式時，直接修改範本。
* `_Project_Info/Project_Info.txt` 的 `使用範本` 填「否」時，改依 `skills/new-project/no-template.md` 依規範重新產生程式（不複製範本，UI 可能與其他專案不同）。該檔記錄了重新產生時容易出錯的地方（TypeScript 7、狀態碼格式、`web.config` 等），規範變更時一併檢查。
* 範本不含 `bin/`、`obj/`、`node_modules/`、`dist/`、`wwwroot/`、`publish/`、`Data/Migrations/`（Migration 於建立專案時產生）。
* 新增佔位符時，同步更新 `new-project` SKILL.md 第 3 節的佔位符表。
* 修改範本後執行 `/verify-template`（複製到 scratchpad、以測試值替換佔位符、建置、發佈並啟動檢查回應格式）。
* 規範新增可機械判斷的規則時（固定樣式即可判斷違規），同步加入 `hooks/check-standards.js` 與 `hooks/test/` 樣本，並以範本確認沒有誤判。
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
