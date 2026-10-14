---
name: owasp-check
description: 依 OWASP Top 10 最新版與 Fortify 規範，檢查 .NET + Vue 專案（或本次變更）的資安狀況並產出報告。在使用者要求資安檢查、發佈前、或完成涉及驗證／權限／輸入處理的功能後使用。
---

# OWASP 資安檢查

規則來源：`D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\CLAUDE.md` 的「資安防禦規範」，以及 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\references\auth-baseline.md`（身分驗證類別的細節）。先讀這兩處，再依下列步驟逐類檢查。

- 依 OWASP Top 10 最新版，規範中的類別不標註編號（A01、A02⋯），因各版編號不同；報告與對照 Fortify 時以官方最新版編號為準。
- OWASP 發布新版時，提醒使用者依新版類別重新檢視並更新通用規範的「資安防禦規範」，補上新增的類別。

**範圍：** 使用者指定時依其範圍；未指定時，有未 commit 變更就檢查變更，否則檢查整個專案。只檢查與回報，**不自行修改程式**；修正方式列入報告，由使用者決定。

## 1. 自動化檢查

在專案中執行，結果原文保留供報告引用：

```bash
# 套件弱點（軟體供應鏈）
cd <frontend> && npm audit
cd <backend> && dotnet list package --vulnerable --include-transitive
```

以 Grep 搜尋下列樣式（排除 `bin/`、`obj/`、`node_modules/`、`dist/`、`publish/`、`wwwroot/`），每筆命中都要讀上下文判斷是否真的有問題：

| 類別 | 樣式 | 判斷 |
| --- | --- | --- |
| 供應鏈 | `.csproj` 中 `Version="[^"]*\*` | 浮動版本，須改明確版本 |
| 注入 | `FromSqlRaw\|ExecuteSqlRaw\|SqlCommand` | 是否以字串串接或插值組 SQL |
| 注入 | `v-html` | 資料來源是否可信 |
| 注入 | `Log\w*\(\$"` 或 Log 內 `+` 串接 | Log Forging，應改結構化參數 |
| 注入 | `Process.Start\|Path.Combine` 搭配使用者輸入 | 指令／路徑注入 |
| 加密 | `new Random\(\|Random.Shared` | 是否用於安全用途 |
| 加密 | `appsettings*.json` 中的 `Password\|Key\|Secret\|pwd\|ConnectionStrings` | 連線字串、`Jwt:Key`、`Mail:Password` 須為 AES 加密值（含 `Password=` 或可直接讀出即為明文）；`AES256Key` 不可與其他專案相同 |
| 身分驗證 | 種子資料（`HasData`）中的預設管理員與預設一般使用者 | 只存雜湊、不含明文密碼，且有對應的 `BasicPwdlog`（建立日期為建立專案當日；首次登入不強制變更）；一般使用者只屬訪客群組、僅有 `*.read` 權限。一般使用者初始密碼為已知值，正式環境仍啟用且未變更密碼時列為待辦（專案 CLAUDE.md 已記錄） |
| 存取控制 | `AllowAnyOrigin\|WithOrigins` | CORS 範圍 |
| 存取控制 | `\[AllowAnonymous\]` | 逐一確認確實需要開放 |
| 存取控制 | Controller 缺少 `[Authorize(Policy` 的寫入端點 | 權限是否足夠 |
| 完整性 | `TypeNameHandling\|BinaryFormatter` | 不可信反序列化 |
| 安全設定 | `UseSwagger\|UseDeveloperExceptionPage` | 是否限開發環境 |
| 例外 | `catch\s*\(\w*\)\s*\{\s*\}` 或空 catch | 吞例外 |
| 身分驗證 | 忘記密碼寄信內容、連結組成方式（`Request.Host`） | 不可寄密碼、不可用 Host 標頭組連結 |

## 2. 執行中檢查（後端可啟動時）

以 `curl -s -D - -o /dev/null <url>` 檢查一般 API 與首頁的回應標頭：

- 應有 `X-Content-Type-Options`、`X-Frame-Options`（或 CSP `frame-ancestors`）、`Content-Security-Policy`；API 應有 `Cache-Control: no-store`。
- 不應有 `Server`、`X-Powered-By` 等揭露伺服器資訊的標頭（Kestrel `AddServerHeader = false`；IIS 由 `web.config` 的 `removeServerHeader` 與移除 `X-Powered-By` 處理）。
- SPA 的 `index.html` 應為 `Cache-Control: no-cache`。
- 正式環境設定（`ASPNETCORE_ENVIRONMENT=Production`）下 Swagger 應回 404。

## 3. 人工檢視

自動化抓不到的部分，對照規範逐類閱讀相關程式：

- 身分驗證：驗證碼是否後端比對且一次性、登入限流與鎖定、錯誤訊息是否透露帳號存在；並逐條對照 `auth-baseline.md`（鎖定、密碼規則與統一方法、逾期變更、忘記密碼、前端登入狀態）。
- 存取控制：Service 層是否檢查資料層級權限（越權、提權、對自己的危險操作）。
- 記錄：安全事件是否記錄；Log 是否含密碼、Token、重設連結、個資；未處理例外與背景作業失敗是否寫入 `BasicExceptionlog`（另建 DbContext、寫入失敗不丟例外），轉存檔名是否移除不合法字元、`Logs\` 是否不在 `wwwroot` 下；新增、修改、刪除是否寫入 `BasicSyslog`（無同步 `SaveChanges`、無繞過攔截器的 `ExecuteUpdate`／`ExecuteDelete` 業務異動），新增的機密欄位是否已列入 `BasicSyslogInterceptor.IgnoredProperties`。
- 例外：是否統一由全域處理、不洩漏 StackTrace。

## 4. 報告格式

依規範的十個類別逐一列出，每類標示 **符合／不符合／待辦（已知未處理）／不適用**：

| 類別 | 結果 | 說明（檔案:行號） | 建議修正 |
| --- | --- | --- | --- |

- 不符合項目依嚴重度排序，附具體位置與修正方式。
- 專案 CLAUDE.md 已記錄為「待辦」的項目標示為待辦，不重複當成新發現。
- 明確說明哪些檢查未執行（如後端未啟動、無法存取正式環境）。
