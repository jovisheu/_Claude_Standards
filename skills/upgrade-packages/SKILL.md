---
name: upgrade-packages
description: 檢查並升級 .NET + Vue 專案（或 _Claude_Standards 新專案範本）的 NuGet／npm 套件：列出弱點與過期套件，選用發佈滿 14 天的穩定版、鎖定版本，升級後重新建置驗證。在使用者要求升級套件、處理弱點、發佈被弱點卡控擋下、或定期維護範本時使用；主版本升級需詢問；不 commit、不發佈。
---

# 升級套件

## 用途

- **做什麼**：以 `dotnet list package` 與 `npm audit`／`npm outdated` 找出有弱點或過期的套件，依通用規範的供應鏈規則升級並鎖定版本，再建置、啟動驗證、執行 `owasp-check`。
- **何時使用**：`publish.ps1` 因 High／Critical 弱點停止發佈時；定期維護（建議每月）；維護新專案範本時。
- **不適用**：新增套件（用 `/add-package`）；升級 .NET SDK 或 Node.js 本身。
- **使用方式**：`/upgrade-packages [弱點|全部]`。預設「弱點」：只升級有弱點的套件（含間接相依）；「全部」：另升級所有過期套件的次版本與修訂版。
- **產出**：更新的 `backend.csproj`、`package.json`、`package-lock.json`（範本另含 `keygen.csproj`、`dotnet-tools.json`）與升級報告；**不 commit、不發佈**。

規則來源：`D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\CLAUDE.md`「軟體供應鏈失效」。指令寫法依 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\references\command-style.md`。版本選擇方式同 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\skills\add-package\SKILL.md` 第 2 節。

**對象：** 目前目錄為一般專案時，升級該專案；目前目錄為 `_Claude_Standards` 時，升級 `skills\new-project\templates\project\`（`keygen.csproj` 的 BCrypt 版本須與 `backend.csproj` 一致），完成後以 Read 讀取 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\skills\verify-template\SKILL.md` 並依其流程驗證（該 skill 設有 `disable-model-invocation`，無法以 Skill 工具呼叫）。

## 1. 盤點

依序執行，結果原文保留：

- `dotnet list <backend 絕對路徑>\backend.csproj package --vulnerable --include-transitive`
- `dotnet list <backend 絕對路徑>\backend.csproj package --outdated`
- `npm audit --prefix <frontend 絕對路徑>`
- `npm outdated --prefix <frontend 絕對路徑>`
- CycloneDX 工具：以 NuGet API 查詢有無發佈滿 14 天的較新版本（`dotnet-tools.json`）。

## 2. 紅綠燈

| 項目 | 紅燈條件（停下詢問） |
| --- | --- |
| 主版本 | 修正版本或建議版本為新的主版本（如 EF Core 10 → 11、Vue 3 → 4），或與 .NET 10 不相容 |
| 無修正版 | 有 High／Critical 弱點但沒有發佈滿 14 天的修正版（列出替代套件或需移除的功能，由使用者決定；不設例外清單） |
| 間接相依 | 弱點來自間接相依，須以直接加入修正版（NuGet）或 `overrides`（npm）處理 |
| 已知鎖定 | 專案 CLAUDE.md 記錄的版本鎖定（如 TypeScript 6.0.3，因 `vue-tsc` 不支援 7）；確認限制已解除才升級 |
| 建置與驗證 | 升級後 `dotnet build`、`npm run build` 失敗，自行修正 2 次仍失敗（回復該套件原版本並回報） |

全部綠燈時不詢問，輸出一次「升級清單」（套件、目前版本 → 目標版本、發佈日期、原因）後直接升級。

## 3. 升級

- 後端：`dotnet add <backend 絕對路徑>\backend.csproj package <套件> --version <版本>`（明確版本號）。EF Core 相關套件（`Microsoft.EntityFrameworkCore.*`、`dotnet-ef` 工具）版本一致。
- 前端：`npm install <套件>@<版本> --save-exact --prefix <frontend 絕對路徑>`（開發套件加 `--save-dev`）；間接相依以 `package.json` 的 `overrides` 指定後 `npm install --prefix <frontend 絕對路徑>`。不使用 `npm audit fix --force`。
- CycloneDX：`dotnet tool update CycloneDX --version <版本> --tool-manifest <專案或範本>\dotnet-tools.json`（只在使用者同意或範本維護時）。

## 4. 驗證

- 重新執行第 1 節的弱點檢查：不得再有 High／Critical。
- `dotnet build`、`npm ci --prefix <frontend 絕對路徑>`、`npm run build --prefix <frontend 絕對路徑>` 皆須成功。
- 一般專案：背景啟動後端（`--Security:Login:CaptchaEnabled=false`），以 `node D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\scripts\api-smoke.js --base http://localhost:<port> --feature basicusers --admin <帳號>:<密碼> --guest <帳號>:<密碼>` 驗證登入、權限與回應格式（不加 `--write`，不寫入資料），並確認回應標頭不變，驗證完即停止。
- 範本：依 `verify-template` 的 SKILL.md 驗證（見上方「對象」）。
- 執行 `owasp-check`，範圍為本次變更。

## 5. 完成回報

- 升級清單（目前 → 新版本、發佈日期、原因）、未升級的套件與原因（含主版本待決定項目）。
- 升級前後的弱點檢查結果；High 以下（Moderate、Low）殘留弱點列為待辦。
- 驗證與 `owasp-check` 結果。
- 尚未 commit：請確認後自行 commit；一般專案再以 `/publish-project` 發佈。範本升級時提醒既有專案不會自動更新。
