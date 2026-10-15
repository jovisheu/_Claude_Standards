---
name: publish-project
description: 依 _Claude_Standards 通用規範發佈 .NET 10 + Vue 3 專案（套件弱點卡控、前端打包、更新 wwwroot、dotnet publish、產生 SBOM），並整理部署至 IIS 前後的檢查清單。只在使用者明確要求發佈或部署時使用。
disable-model-invocation: true
---

# 發佈與部署（通用）

## 用途

- **做什麼**：發佈前檢查（已 commit、`appsettings.json` 機密與網址、部署子路徑、卡控設定），以專案的 `publish.ps1` 產生部署檔：套件弱點卡控（High、Critical 即停止）→ 前端打包 → 更新 `wwwroot` → `dotnet publish` → 產生 SBOM；最後回報結果並列出 IIS 部署提醒。
- **何時使用**：要把專案部署到測試或正式環境時；`/new-project` 完成時會依本流程發佈一次供驗證。
- **不適用**：實際部署到 IIS（Claude 沒有 IIS 權限，由使用者執行）；SBOM 弱點掃描（須另以 OSV-Scanner 等工具執行）；正式資料庫的 Migration（須使用者備份後手動套用）。
- **使用方式**：`/publish-project`（本 skill 設有 `disable-model-invocation`，只能由使用者明確呼叫）。工作目錄未 commit 時會先詢問。
- **產出**：`publish\`（部署檔）、`sbom\yyyy\MM\dd\<HHmmss>_<commit>_*.cdx.json`（每次保留一份）、發佈前檢查結果與部署提醒。

規則來源：`D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\CLAUDE.md`（「資安防禦規範」、「部署」兩節）。

**專案專用 skill 優先：** 專案有 `.claude/skills/publish/SKILL.md` 時改依該檔執行，本 skill 只補充其未涵蓋的檢查。另先讀專案 CLAUDE.md，取得目錄結構、部署子路徑、正式環境網址等專案設定。

## 1. 發佈前

1. **工作目錄須已 commit：** 先執行 `git status`。有未 commit 的變更時，停下來詢問使用者是否先 commit，不可直接發佈（發佈結果要能對應到確切的 commit）。
   - 例外：`new-project` 建立完成後依本流程發佈一次僅供驗證（新專案尚未 commit），由 `new-project` 第 5 節說明略過此項；正式部署前仍須 commit 後重新發佈。
2. **檢查後端 `appsettings.json`（輸出會包含此檔）：** 只列出問題請使用者確認，**不自行改動設定值**。
   - 機密（連線字串、JWT 金鑰、SMTP 密碼）須為 AES-256 加密值，不可為明文或僅 Base64 編碼；JWT 金鑰不可為範例值。
   - `AES256Key`、`AES256IV` 須為本專案自己的值，不可沿用其他專案。
   - 前端網址（忘記密碼連結等）須為正式環境外部可連線的網址（含子路徑），不可是 `localhost`，否則信中的連結只有在伺服器本機才打得開。
   - SMTP、速率限制、鎖定等參數是否為正式環境的值。
   - 若正式環境另外保留了自己的 `appsettings.json`，提醒使用者比對本次新增或移除的設定鍵；正式環境的加密值須以該環境的 `AES256Key`／`AES256IV` 產生（`dotnet run -- encrypt "<明文>"`）。
3. **前端部署子路徑：** 確認 `frontend/.env` 的 `VITE_APP_BASE` 與 IIS 應用程式名稱一致（預設 `/<專案名稱>/`，以 `/` 開頭與結尾；部署在站台根目錄時為 `/`）。未設定時詢問使用者，不可直接以根目錄發佈。
   - `vite.config.ts` 只在建置時套用 `VITE_APP_BASE`（開發固定 `/`）；router、Axios 須以 `import.meta.env.BASE_URL` 組路徑。後端由 IIS 自動設定 `PathBase`。
4. 建議先執行 `/owasp-check`，有已知弱點的套件不可上線。
5. **供應鏈卡控與 SBOM：** 確認專案根目錄有 `dotnet-tools.json`（鎖定 CycloneDX 工具版本）、`.gitignore` 含 `sbom/`。缺少時詢問使用者是否依本 skill 的範本補上（`publish.ps1` 與 new-project 範本的 `dotnet-tools.json`），不可略過卡控直接發佈。

## 2. 執行發佈（專案根目錄，PowerShell）

發佈產物只能由專案的發佈腳本產生，不手動複製或修改 `wwwroot/`、`publish/`。

- **專案已有發佈腳本**（通常為根目錄 `publish.ps1`）：先讀過腳本確認參數，再執行。
- **專案沒有發佈腳本**：詢問使用者是否以本 skill 的範本 `templates/publish.ps1` 建立。同意後複製到專案根目錄，依專案目錄調整 `-FrontendDir`、`-BackendDir` 預設值，存為 **UTF-8 with BOM**，並確認後端 `.csproj` 已排除 `publish/**`（避免下次發佈時被當成內容檔再複製一層）、`.gitignore` 已包含 `wwwroot/` 與 `publish/`。建立後提醒使用者 commit。

範本用法：

```powershell
.\publish.ps1                    # 弱點檢查 → npm ci → npm run build → 重建 wwwroot → dotnet publish 至專案根目錄 publish/ → 產生 SBOM 至 sbom/
.\publish.ps1 -SkipNpmInstall    # 略過 npm ci
.\publish.ps1 -Output D:\Deploy  # 指定輸出目錄
.\publish.ps1 -Runtime win-x86   # 指定目標平台（預設 win-x64）
```

- 以框架相依、指定 RID 方式發佈，只帶入目標平台的原生程式庫；執行環境由伺服器的 ASP.NET Core Hosting Bundle 提供。
- 若要縮小輸出，可在 `.csproj` 設定 `SatelliteResourceLanguages`（如 `zh-Hant`），只保留需要的語系資源。
- 腳本最後會驗證 `wwwroot/index.html` 的資源路徑使用 `VITE_APP_BASE`，不符即失敗。
- 腳本第一步以 `npm audit --audit-level=high` 與 `dotnet list package --vulnerable` 檢查，有 High 或 Critical 弱點（或無法連線查詢）即停止；回報弱點套件與建議升級的版本，由使用者決定是否升級，不可略過。不設例外清單：沒有修正版時，回報可替代的套件或需移除的功能，由使用者決定。
- 最後產生 CycloneDX SBOM 至 `sbom\yyyy\MM\dd\`，檔名為 `<HHmmss>_<commit>_backend.cdx.json`、`<HHmmss>_<commit>_frontend.cdx.json`（每次保留一份，不在輸出目錄內）。
- CycloneDX 工具版本固定於 `dotnet-tools.json`；腳本會查詢 NuGet，有發佈滿 14 天的較新版本時只提醒、不切換。回報時一併列出，由使用者決定是否以 `dotnet tool update CycloneDX --version <版本>` 升級（升級後 commit 並重新發佈）；查詢失敗不影響發佈。
- 執行失敗時回報錯誤輸出，**不要自行修改腳本繞過**。常見原因：輸出目錄被 IIS 應用程式集區或 `dotnet run` 鎖住（請使用者先停止）、`vue-tsc` 型別錯誤、`npm ci` 與 `package-lock.json` 不一致。

## 3. 發佈後回報與部署提醒

回報 commit、輸出目錄、檔案數量、大小、部署子路徑、弱點檢查結果與 SBOM 位置，並提醒使用者部署時：

- **上線前須另以 SBOM 掃描工具（如 OSV-Scanner、Grype、Dependency-Track）掃描本次的 SBOM**：發佈腳本只用 npm 與 dotnet 內建檢查卡控，涵蓋範圍有限。

- IIS 伺服器須安裝與專案相同主版本的 **ASP.NET Core Hosting Bundle**（.NET 10 專案須安裝 10.x）。
- 部署為 IIS 子應用程式時，名稱須與 `VITE_APP_BASE` 一致。
- **先清空 IIS 目錄再複製**，避免殘留舊 assets；**保留 `Logs\` 目錄**（系統錯誤紀錄轉存檔）。若正式環境保留自己的 `appsettings.json`，清空前先備份，複製後還原。
- 應用程式集區帳號須有網站目錄 `Logs\` 的寫入權限（系統錯誤紀錄每日轉存）。
- 應用程式集區的 `ASPNETCORE_ENVIRONMENT` 須為 `Production`（Swagger 關閉、啟用 HSTS 與 HTTPS 導向）。
- 有 HTTPS 繫結時 HTTP 會自動導向 HTTPS；正式環境應設定 HTTPS 繫結。
- IIS 加上的 `Server`、`X-Powered-By` 標頭由 `web.config` 移除（`removeServerHeader` 須 **IIS 10 以上**）。若出現 HTTP 500.19，代表 IIS 版本過舊或伺服器鎖定了 `requestFiltering`／`httpProtocol` 區段，須改由 IIS 端處理。
- 啟動時自動套用 migration 的專案，部署前確認資料庫已備份；本次有新增 migration 時特別提醒。
- 部署後以瀏覽器確認：首頁可載入、登入可用、回應標頭（安全標頭、無 `Server`／`X-Powered-By`）、`/swagger` 無法開啟。

以 Kestrel 獨立執行（非 IIS）的專案，改提醒：以 Windows 服務或反向代理執行、設定 `ASPNETCORE_URLS` 與 `ASPNETCORE_ENVIRONMENT=Production`、HTTPS 憑證設定。

Claude 沒有 IIS 管理權限，無法代為部署或讀取 IIS 設定；部署由使用者執行。
