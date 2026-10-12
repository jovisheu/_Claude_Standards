# _Claude_Standards

跨專案共用的開發規範與 Skill。維護本資料夾的規範或 Skill 時先讀本檔；本檔不會自動載入。

## 檔案

| 路徑 | 說明 | 載入時機 |
| --- | --- | --- |
| `CLAUDE.md` | 通用規範 | 各專案 CLAUDE.md 匯入，每次對話載入 |
| `references/auth-baseline.md` | 登入、密碼、忘記密碼、前端登入狀態的實作細節 | 建立或修改這些功能、資安檢查時 |
| `skills/<名稱>/SKILL.md` | 共用 Skill | 執行該 Skill 時 |
| `DB_DefaultConnection.txt` | 預設資料庫連線字串（明文密碼） | 只在 `new-project` 建立專案時 |
| `SendMail_Info.txt` | 預設寄信設定（明文密碼） | 只在 `new-project` 建立專案時 |

兩個 `.txt` 含明文密碼，不得複製進專案或納入任何版控。

## 規範與 Skill 的分工

* **CLAUDE.md**（本資料夾與各專案 CLAUDE.md）：每次對話都會載入，只放「寫程式時一律遵守」的規則與專案架構。專案 CLAUDE.md 以 `@D:/Jovi/Localhost/_ClaudeCode/_Claude_Standards/CLAUDE.md` 匯入（本資料夾不在專案上層目錄，不會自動載入）。
  * 只在特定流程用得到的內容（建立骨架、發佈、檢查步驟、一次性的基礎設定），放到 Skill 或 `references/`，CLAUDE.md 只留原則與指向。
* **Skill**：需要時才載入，放「偶爾執行、步驟固定」的流程（檢查、發佈、測試、新增功能）。
  * 專案專用：`<專案>/.claude/skills/<名稱>/SKILL.md`，隨專案進版控。
  * 跨專案共用：實體檔放在 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\skills\<名稱>\`，再以 junction 掛到 `%USERPROFILE%\.claude\skills\<名稱>`（PowerShell：`New-Item -ItemType Junction -Path <連結> -Target <實體目錄>`，不需管理員權限）。
* **references/**：多個 Skill 共用、或修改特定功能時才需要的細節規範。

## 目前共用 Skill

* `new-project`：依通用規範建立新專案，開工前逐項詢問必要資訊。
* `publish-project`：發佈專案並整理部署檢查清單（只在使用者明確要求時執行）。
* `owasp-check`：依通用規範的資安規範與 `references/auth-baseline.md` 檢查專案。
