---
name: verify-template
description: 驗證 _Claude_Standards 的新專案範本（skills/new-project/templates）在修改後仍可建立可用的專案：複製到 scratchpad、以測試值替換佔位符、建置並啟動檢查回應。只在維護範本後由使用者要求時使用。
disable-model-invocation: true
---

# 驗證新專案範本

## 用途

- **做什麼**：模擬 `new-project` 的範本流程，在 scratchpad 建立一個測試專案，確認佔位符完整、`dotnet build`、`npm ci`、`npm run build`、發佈腳本皆成功，並啟動檢查回應格式與安全標頭。
- **何時使用**：修改範本程式、升級範本套件（`upgrade-packages` 完成後）、規範變更後同步範本時。
- **不適用**：驗證已建立的專案（用 `owasp-check`、`publish-project`）。
- **使用方式**：`/verify-template [含資料庫]`。預設不建資料庫；加「含資料庫」時另建測試資料庫並驗證登入流程。
- **產出**：驗證報告；**不修改範本**（發現問題時列出檔案與修正方式，由使用者決定），scratchpad 的測試專案保留供檢查。

指令寫法依 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\references\command-style.md`。以下 `<範本>` 為 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\skills\new-project\templates`，`<測試>` 為 scratchpad 下的 `TemplateVerify`。

## 1. 建立測試專案

先讀 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\skills\new-project\SKILL.md` 第 3、4 節，依其步驟執行，差異如下：

1. **複製**：`<範本>\project` → `<測試>`；`publish.ps1` 由 `publish-project` 範本複製。`<測試>` 已存在時為紅燈（詢問是否改用新名稱，如 `TemplateVerify2`）。
2. **keygen**：照常產生，密碼使用測試值 `Verify@1234`、`Guest@1234`。
3. **佔位符**：專案名稱 `TemplateVerify`、用途「範本驗證」、埠號 5999、管理員 `verifyadmin`／`verifyadmin@example.com`、一般使用者 `verifyguest`／`verifyguest@example.com`、SMTP `Enabled = false` 其餘留空、`__FRONTEND_URL__` 為 `http://localhost/TemplateVerify`、日期為今天、資料庫描述填「範本驗證」。
4. **連線字串**：
   - 預設：`Server=localhost;Database=TemplateVerifyDb;Integrated Security=True;TrustServerCertificate=True;`（不建資料庫，只用於加密與啟動）。
   - 含資料庫：讀 `_Project_Info\DB_DefaultConnection.txt`，資料庫名稱改為 `TemplateVerifyDb`；資料庫已存在且含資料表時為紅燈，不覆蓋。
5. 替換完成後 Grep `__[A-Z0-9_]+__`（排除 `node_modules`）須為 0 筆；有殘留表示範本新增了佔位符但 `new-project` 第 3 節的佔位符表未更新，列為問題。

## 2. 建置

- `dotnet build`、`npm ci --prefix <測試>\frontend`、`npm run build --prefix <測試>\frontend` 皆須成功。
- 含資料庫時：`dotnet ef migrations add InitialCreate` 與 `dotnet ef database update`（指定 `--project`、`--startup-project`），並以 `sqlcmd` 唯讀查詢確認 `MS_Description` 完整。
- `powershell -NoProfile -File "<測試>\publish.ps1" -SkipNpmInstall` 須成功（含弱點卡控與 SBOM）；因弱點停止時記錄弱點清單，建議執行 `/upgrade-packages`。

## 3. 啟動檢查

背景啟動後端（`--Security:Login:CaptchaEnabled=false`），以 `curl` 檢查，完成即停止：

- 不需資料庫：`GET /api/auth/options` 回 200 且為 `ApiResponse` 格式；無 Token 呼叫 `/api/auth/me` 回 401 `ApiResponse`；`/api/not-exist` 回 404 `ApiResponse`；錯誤 Content-Type 回 415；回應含安全標頭、`Cache-Control: no-store`、無 `Server` 標頭。
- 含資料庫：另依 `new-project` 第 4 節「可測」項目驗證登入（管理員與一般使用者）與權限；其中登入、權限與回應格式以 `node D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\scripts\api-smoke.js --base http://localhost:<port> --feature basicusers --admin <帳號>:<密碼> --guest <帳號>:<密碼>` 檢查（不加 `--write`）。
- 發佈結果：依 `new-project` 第 5 節第 3 步，以 Production 啟動 `publish\backend.dll`，確認子路徑首頁、`/swagger` 404、安全標頭。

## 4. 報告

- 每個步驟的成功／失敗與錯誤輸出摘要。
- 發現的範本問題：檔案、原因、建議修正。
- 含資料庫時提醒：測試資料庫 `TemplateVerifyDb` 保留未刪除，不需要時請自行於 SSMS 刪除；scratchpad 的 `<測試>` 同樣保留。
