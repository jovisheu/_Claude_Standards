---
name: owasp-check
description: 依 OWASP Top 10 最新版與 Fortify 規範，檢查 .NET + Vue 專案（或本次變更）的資安狀況並產出報告。在使用者要求資安檢查、發佈前、或完成涉及驗證／權限／輸入處理的功能後使用。
---

# OWASP 資安檢查

## 用途

- **做什麼**：依 OWASP Top 10 最新版與通用規範的「資安防禦規範」，以套件弱點掃描、程式碼樣式搜尋、執行中的回應標頭檢查與人工檢視，逐類判斷專案是否符合，產出「符合／不符合／待辦／不適用」報告與修正建議；同時確認供應鏈卡控與 SBOM 步驟仍在。
- **何時使用**：發佈前；完成涉及驗證、權限、輸入處理的功能後；`/new-project`、`/add-feature` 完成時會自動執行；或使用者要求資安檢查時。
- **不適用**：取代正式的弱點掃描或滲透測試（如 Fortify、SBOM 掃描工具），本 skill 只做規範層面的檢查；無法檢查正式環境的 IIS 設定。
- **使用方式**：`/owasp-check`（未指定範圍時，有未 commit 變更就檢查變更，否則檢查整個專案）；也可指定範圍，如 `/owasp-check backend/Services/Bulletins`。
- **產出**：十個類別的檢查報告（不符合項目依嚴重度排序，附檔案位置與修正方式）。單獨執行時只報告不修改；由 `/new-project`、`/add-feature` 呼叫時依其規定直接修正。

規則來源：`D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\CLAUDE.md` 的「資安防禦規範」、同目錄 `rules\` 下的 `backend-data.md`（異動歷程寫入規則）與 `frontend-ui.md`（無權限時不渲染），以及 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\references\` 下的 `auth-baseline.md`（身分驗證類別的細節）、`logging-baseline.md`（記錄類別的細節）。先讀這些檔案，再依下列步驟逐類檢查。指令寫法依同目錄的 `command-style.md`。

- 依 OWASP Top 10 最新版，規範中的類別不標註編號（A01、A02⋯），因各版編號不同；報告與對照 Fortify 時以官方最新版編號為準。
- OWASP 發布新版時，提醒使用者依新版類別重新檢視並更新通用規範的「資安防禦規範」，補上新增的類別。

**範圍：** 使用者指定時依其範圍；未指定時，有未 commit 變更就檢查變更，否則檢查整個專案。單獨執行時只檢查與回報，**不自行修改程式**，修正方式列入報告由使用者決定；由 `/new-project`、`/add-feature` 呼叫時依呼叫端的規定直接修正。

## 1. 自動化檢查

在專案中執行，結果原文保留供報告引用：

```bash
# 套件弱點（軟體供應鏈），一次一個指令、使用絕對路徑
npm audit --prefix <frontend 絕對路徑>
dotnet list <backend 絕對路徑>\backend.csproj package --vulnerable --include-transitive
```

並確認供應鏈卡控仍在（缺少時列為不符合）：`publish.ps1` 含 `npm audit --audit-level=high` 與 High／Critical 的 dotnet 弱點檢查、產生 SBOM 的步驟；專案根目錄 `dotnet-tools.json` 鎖定 CycloneDX 工具版本；`.gitignore` 含 `sbom/`，且 `wwwroot/`、`publish/` 內沒有 SBOM 檔。內建檢查有 High 以下（Moderate、Low）的弱點時列為待辦，並提醒上線前另以 SBOM 掃描工具掃描。

執行規範檢查腳本，掃描整個專案或指定範圍（目錄或檔案皆可，自動排除 `bin/`、`obj/`、`node_modules/`、`dist/`、`publish/`、`wwwroot/`、`Migrations/`）：

```bash
node D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\hooks\check-standards.js <專案或範圍絕對路徑>
```

腳本涵蓋可機械判斷的樣式（`UtcNow`、同步 `SaveChanges`、`ExecuteUpdate／Delete／Sql`、`Random`、SQL／Log 字串插值、`AllowAnyOrigin`、`BinaryFormatter`／`TypeNameHandling`、`System.Net.Mail`、SQL Server 2022 語法、`UseCompatibilityLevel(150)`、Controller 缺 `[Authorize]`、Models namespace、空 catch、`v-html`、寫死根路徑、`.csproj`／`package.json` 浮動版本、`appsettings` 明文連線字串、`.env` 機密與非 ASCII、`.ps1` BOM；完整清單見腳本）。每筆命中都要讀上下文判斷是否真的有問題（如 `v-html` 的資料來源是否可信、`Random` 是否用於安全用途）。

腳本未涵蓋、須讀上下文判斷的樣式，以 Grep 搜尋（排除目錄同上）：

| 類別 | 樣式 | 判斷 |
| --- | --- | --- |
| 注入 | `FromSqlRaw\|ExecuteSqlRaw\|SqlCommand` | 是否以字串串接（`+`）組 SQL（插值已由腳本檢查） |
| 注入 | Log 內 `+` 串接 | Log Forging，應改結構化參數（插值已由腳本檢查） |
| 注入 | `Process.Start\|Path.Combine` 搭配使用者輸入 | 指令／路徑注入 |
| 加密 | `appsettings*.json` 中的 `Password\|Key\|Secret\|pwd\|ConnectionStrings` | 連線字串、`Jwt:Key`、`Smtp:Password` 須為 AES 加密值（可直接讀出即為明文）；`AES256Key` 不可與其他專案相同 |
| 身分驗證 | 種子資料（`HasData`）中的預設管理員與預設一般使用者 | 只存雜湊、不含明文密碼，且有對應的 `BasicPwdlog`（建立日期為建立專案當日；首次登入不強制變更）；一般使用者只屬訪客群組、僅有 `*.read` 權限。一般使用者初始密碼為已知值，正式環境仍啟用且未變更密碼時列為待辦（專案 CLAUDE.md 已記錄） |
| 存取控制 | `WithOrigins` | CORS 白名單範圍 |
| 存取控制 | `\[AllowAnonymous\]` | 逐一確認確實需要開放 |
| 存取控制 | Controller 缺少 `[Authorize(Policy` 的寫入端點 | 權限是否足夠 |
| 安全設定 | `UseSwagger\|UseDeveloperExceptionPage` | 是否限開發環境 |
| 例外 | 只有註解的 catch 區塊 | 註解是否說明了刻意忽略的理由（完全空白的 catch 已由腳本檢查） |
| 身分驗證 | 忘記密碼寄信內容、連結組成方式（`Request.Host`） | 不可寄密碼、不可用 Host 標頭組連結 |

## 2. 執行中檢查（後端可啟動時）

以 `curl -s -D - -o /dev/null <url>` 檢查一般 API 與首頁的回應標頭：

- 應有 `X-Content-Type-Options`、`X-Frame-Options`（或 CSP `frame-ancestors`）、`Content-Security-Policy`；API 應有 `Cache-Control: no-store`。
- 不應有 `Server`、`X-Powered-By` 等揭露伺服器資訊的標頭（Kestrel `AddServerHeader = false`；IIS 由 `web.config` 的 `removeServerHeader` 與移除 `X-Powered-By` 處理）。
- SPA 的 `index.html` 應為 `Cache-Control: no-cache`。
- 正式環境設定（`ASPNETCORE_ENVIRONMENT=Production`）下 Swagger 應回 404。

## 3. 人工檢視

自動化抓不到的部分，對照規範逐類閱讀相關程式：

- 身分驗證：驗證碼是否後端比對且一次性、登入與忘記密碼是否都檢查驗證碼（共用 `CaptchaEnabled` 開關）、登入限流與鎖定、錯誤訊息是否透露帳號存在；並逐條對照 `auth-baseline.md`（鎖定、密碼規則與統一方法、逾期變更、忘記密碼、前端登入狀態）。
- 存取控制：Service 層是否檢查資料層級權限（越權、提權、對自己的危險操作）；前端無 CUD 權限或資料受保護時，對應按鈕、欄位、勾選項是否以 `v-if` 不渲染（以 Grep 搜尋 `:disabled=` 與 `v-show`，與權限、受保護、本人相關者列為不符合；送出中、分頁邊界等暫時狀態不在此限）。
- 記錄：安全事件是否記錄；Log 是否含密碼、Token、重設連結、個資；未處理例外與背景作業失敗是否寫入 `BasicExceptionlog`（另建 DbContext、寫入失敗不丟例外），轉存檔名是否移除不合法字元、`Logs\` 是否不在 `wwwroot` 下；新增、修改、刪除是否寫入 `BasicSyslog`（無同步 `SaveChanges`、無 `ExecuteUpdate`／`ExecuteDelete`／`ExecuteSql` 異動、無自行 `new AppDbContext`），`Program.cs` 是否以 `AddInterceptors` 同時掛上 `BasicSyslogInterceptor` 與 `BasicSyslogCommandGuard`，`AllowLogArchiveDelete` 是否只出現在系統紀錄表的轉存服務，`BasicSyslogDescription` 與 `Describe(...)` 的說明是否不含機密或不必要的個資（寫入端點未標說明列為待辦，不算不符合），新增的機密欄位是否已列入 `BasicSyslogInterceptor.MaskedProperties`。
- 例外：是否統一由全域處理、不洩漏 StackTrace。

## 4. 報告格式

依規範的十個類別逐一列出，每類標示 **符合／不符合／待辦（已知未處理）／不適用**：

| 類別 | 結果 | 說明（檔案:行號） | 建議修正 |
| --- | --- | --- | --- |

- 不符合項目依嚴重度排序，附具體位置與修正方式。
- 專案 CLAUDE.md 已記錄為「待辦」的項目標示為待辦，不重複當成新發現。
- 明確說明哪些檢查未執行（如後端未啟動、無法存取正式環境）。
