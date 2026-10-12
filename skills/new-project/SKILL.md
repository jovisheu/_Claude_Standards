---
name: new-project
description: 依 _Claude_Standards 通用規範建立新的 .NET 10 + Vue 3 專案。在使用者要求建立新專案、初始化專案骨架時使用；開工前先逐項詢問必要資訊，確認後才建立。
---

# 建立新專案

規則來源：`D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\CLAUDE.md` 與 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\references\auth-baseline.md`（登入、密碼、忘記密碼、前端登入狀態）。先完整讀過兩個檔案，建立的所有程式碼都須符合其中的技術棧、目錄結構、資安規範與 API 格式。

## 1. 環境檢查

先確認以下工具已安裝並記錄版本，缺少時告知使用者，不自行安裝：

```bash
dotnet --list-sdks   # 須有 10.x
node -v; npm -v
git --version
sqlcmd -?            # 選用，用於確認資料庫連線
```

同時確認目標目錄存在且為空；不為空時先列出內容並詢問，不覆蓋既有檔案。

## 2. 逐項詢問必要資訊

**未取得使用者確認前不得開始建立。** 一次列出下列清單（附預設值，含「將建立的資料表」），使用者可只回覆有意見的編號，其餘採預設。

### 一、專案基本資訊
1. 專案名稱／系統名稱（用於方案、資料庫、頁面標題、JWT Issuer/Audience）— 預設：目錄名稱
2. 專案用途（一兩句，寫入專案 CLAUDE.md）
3. 目錄結構 — 預設：`backend/` + `frontend/`，後端專案名 `backend`
4. 是否初始化 git — 預設：是，含 `.gitignore`

### 二、資料庫（MSSQL）
5. 連線字串 — 預設：讀取 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\DB_DefaultConnection.txt` 的 `DefaultConnection`（`<專案名稱>` 替換為第 1 項；含明文密碼，建立時以新專案的 AES 金鑰加密後寫入 `appsettings.json`）；檔案不存在時改問第 6、7 項
6. SQL Server 執行個體與資料庫名稱 — 預設：同第 5 項（資料庫 `<專案名稱>Db`）
7. 驗證方式：Windows 整合驗證／SQL 帳密 — 預設：同第 5 項
8. 建立後是否直接執行 Migration 建表 — 預設：是

**將建立的資料表**（列清單時須完整列出此表與種子資料，讓使用者確認；本表為基本資料表欄位的唯一定義來源）：

| 資料表 | 主要欄位 | 說明 |
| --- | --- | --- |
| `Users` | Id、Username（唯一）、Email（唯一）、PasswordHash、DisplayName、IsActive、CreatedAt、UpdatedAt、PasswordResetTokenHash、PasswordResetExpiresAt | 使用者 |
| `Groups` | Id、Name（唯一）、Description、CreatedAt | 群組（角色） |
| `Permissions` | Id、Name（唯一）、Description、Module | 權限，以靜態種子建立 |
| `UserGroups` | UserId + GroupId（複合主鍵） | 使用者與群組多對多 |
| `GroupPermissions` | GroupId + PermissionId（複合主鍵） | 群組與權限多對多 |
| `_LoginErr_log` | Id、Username、CreatedAt；索引 (Username, CreatedAt) | 登入失敗紀錄（鎖定判斷） |
| `_Pwd_log` | Id、Username、PasswordHash、CreatedAt；索引 (Username, CreatedAt) | 密碼歷程（重複與到期判斷） |

- 種子資料：權限（`users.read`、`groups.update` 等）、受保護的「系統管理員」群組（`Id = 1`）、第 9 項的預設管理員，以及一筆已逾期的初始密碼歷程（首次登入強制變更）。
- 第 23 項有業務功能時，另列出該功能的資料表與欄位一併確認。
- EF Core 另會自動建立 `__EFMigrationsHistory`。

### 三、預設管理員（種子資料）
9. 管理員帳號、Email、顯示名稱 — 預設帳號：`skyeyes`；Email：`jovi@skyeyes.tw`；、顯示名稱：`系統管理員`；
10. 管理員初始密碼 — 預設：`J@vi1212`（建表時寫入）

### 四、安全參數（寫入 `appsettings.json`）
11. 登入錯誤鎖定 — 預設：15 分鐘內錯 5 次，鎖定 15 分鐘
12. 密碼歷程／期限 — 預設：前 3 次不得重複、180 天須變更
13. JWT 有效時間 — 預設：60 分鐘
14. 重設密碼連結時效與申請頻率 — 預設：30 分鐘；同帳號 5 分鐘內限申請 1 次
15. 登入速率限制 — 預設：每 IP 每分鐘 10 次
16. 登入是否使用驗證碼 — 預設：是（後端產生的圖形驗證碼，5 碼）

### 五、郵件（忘記密碼）
17. SMTP：主機、Port、SSL、寄件者名稱與地址、帳密 — 預設：讀取 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\SendMail_Info.txt`（`Smtp` 區段，`<專案名稱>` 替換為第 1 項；`Password` 為明文，建立時以新專案的 AES 金鑰加密後寫入 `appsettings.json`）；檔案不存在時先寫入本機檔案（`Enabled = false`）
18. 前端網址（重設連結取自設定檔，須含部署子路徑）— 預設：`http://localhost/<專案名稱>`；正式網址若已知一併提供

### 六、部署
19. 部署方式：IIS（產生 `web.config` 移除標頭，需 IIS 10+）／Kestrel 獨立執行 — 預設：IIS；部署子路徑預設 `/<專案名稱>/`（站台根目錄為 `/`）
20. 前後端是否同源 — 預設：同源（SPA 由後端 `wwwroot` 提供，不開 CORS）；分開部署時提供來源白名單
21. 是否附發佈用 `.ps1` 腳本 — 預設：是（以 `publish-project` skill 的範本建立，UTF-8 with BOM）

### 七、功能範圍
22. 第一版基本功能 — 預設：登入／登出、忘記密碼、逾期變更密碼、使用者管理、群組與權限管理
23. 其他業務功能：功能名稱與主要欄位（依規範產生 Controller／Service／DTO／Model 與前端功能資料夾）
24. 版面需求 — 預設：側邊選單 + 頂列的後台版面；有主色或 Logo 請提供

## 3. 建立專案

取得確認後依序建立，每完成一個階段簡短回報：

1. **後端**：`dotnet new webapi`，套件寫明確版本號（不用浮動版本）；依規範建立 `Controllers/`、`Services/`、`DTOs/`、`Models/`、`Data/`、`Exceptions/`、`Extensions/`。
   - 資料表：依第 2 節「將建立的資料表」，加上第 23 項的業務資料表。

   - 種子（皆以 `HasData` 靜態種子，隨 Migration 建表寫入）：權限、受保護的「系統管理員」群組、預設管理員。
     - 預設管理員：第 9、10 項的帳密，加入系統管理員群組；建立時以 BCrypt 產生雜湊，程式與 Migration 只存雜湊，不出現明文密碼。
     - 同時寫入一筆 `_Pwd_log`（同一雜湊、`CreatedAt` 設為早於密碼有效天數的固定日期，如 `2000-01-01`），讓首次登入走逾期變更流程，且新密碼不得與初始密碼相同。
   - 全域例外 Middleware、`BusinessException`、`ApiResponse`／`PagedResult`、`InvalidModelStateResponseFactory`（模型驗證失敗回 `ApiResponse`；JSON 解析失敗或缺少 Body 時只回「請求資料格式不正確」，不洩漏型別或路徑）。
   - 失敗回應的 HTTP 狀態碼與 `ApiResponse`：JWT `OnChallenge`（呼叫 `HandleResponse()`）回 401、自訂 `IAuthorizationMiddlewareResultHandler` 回 403、速率限制 `OnRejected` 回 429、`/api/{**path}` 回 404（不落入 SPA fallback）；前端 Axios 於錯誤分支讀取 `message`，401 視為登入逾時。
   - 安全設定：JWT 驗證（簽章、Issuer、Audience、期限）、預設 `[Authorize]`、安全標頭、移除 `Server` 標頭、`Cache-Control: no-store`、Swagger 僅限 Development、速率限制。
   - 密碼設定統一由單一方法處理；郵件範本放 `Template/` 並於啟動時載入。
   - 連線字串、JWT 金鑰、SMTP 密碼：以 AES-256 加密後寫入 `appsettings.json`（SMTP 密碼為空時不解密）。
     - `AES256Key`（32 字元）、`AES256IV`（16 字元）以 `RandomNumberGenerator` 從英數字隨機產生，每個專案各自一組，寫在 `appsettings.json` 頂層。
     - `Extensions/ConfigEncryption.cs`：使用 .NET 內建 `Aes`（CBC、PKCS7），加密值以 Base64 儲存；解密失敗或長度不符時拒絕啟動，錯誤訊息不含原始值。
     - `Program.cs` 在註冊服務前解密，結果存入區域變數（連線字串直接傳給 `UseSqlServer`，JWT 金鑰以 `PostConfigure<JwtSettings>` 設定）；不寫回 `IConfiguration`，否則 appsettings 重新載入時會還原成加密值。
     - 提供工具模式 `dotnet run -- encrypt "<明文>"`，讀取輸出目錄的 `appsettings.json` 並印出加密值，用來產生上述加密值，以及之後更換時使用。
     - 連線字串取自 `DB_DefaultConnection.txt`、SMTP 密碼取自 `SendMail_Info.txt` 的明文，以本專案金鑰加密；不可沿用其他專案的加密值（金鑰不同會解密失敗、拒絕啟動），明文不得寫入專案任何檔案或回報內容。
     - JWT 金鑰以 `RandomNumberGenerator` 產生至少 48 位元組。
   - 不使用 `dotnet user-secrets`。
2. **前端**：Vite + Vue 3 + TypeScript，安裝 Pinia、Axios、Tailwind CSS v4，以 `package-lock.json` 鎖定版本；依規範建立 `src/views/[Feature]/` 結構、Axios interceptor（`handled` 標記）、登入狀態（檢查 `expiresAt`）、路由守衛。
3. **部署檔**：依第 19～21 項產生 `web.config`、SPA `index.html` 的 `Cache-Control: no-cache` 設定與發佈腳本。
   - 移除伺服器標頭：Kestrel 設定 `AddServerHeader = false`；IIS 於 `web.config` 以 `<requestFiltering removeServerHeader="true" />`（須 IIS 10 以上）與 `<customHeaders><remove name="X-Powered-By" /></customHeaders>` 移除 IIS 加上的標頭。
   - SPA 的 `index.html`（含 fallback）回應 `Cache-Control: no-cache`；assets 檔名含 hash，可正常快取。
   - 發佈腳本：複製 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\skills\publish-project\templates\publish.ps1` 至專案根目錄，目錄名稱與第 3 項不同時調整 `-FrontendDir`、`-BackendDir` 預設值；須維持 UTF-8 with BOM（以 `[IO.File]::WriteAllText(..., [Text.UTF8Encoding]::new($true))` 寫入）。
   - 後端 `.csproj` 排除 `publish/**`（避免下次發佈時被當成內容檔再複製一層）。
   - 部署子路徑（第 19 項）：前端 `.env` 設定 `VITE_APP_BASE`（以 `/` 開頭與結尾，納入版控）；`vite.config.ts` 只在建置時套用為 `base`（開發固定 `/`）；router `createWebHistory(import.meta.env.BASE_URL)`、Axios ``baseURL: `${import.meta.env.BASE_URL}api` ``；後端由 IIS 自動設定 `PathBase`，不需另外處理。
   - 專案 CLAUDE.md 註明發佈流程使用 `/publish-project`。
4. **專案 CLAUDE.md**：第一行以 `@D:/Jovi/Localhost/_ClaudeCode/_Claude_Standards/CLAUDE.md` 匯入通用規範，再記錄專案用途、目錄、啟動方式、專案特有設定與待辦。
5. **git**：初始化並加入 `.gitignore`（含 `bin/`、`obj/`、`node_modules/`、`dist/`、`publish/`、後端 `wwwroot/`（發佈腳本產生）、`.env.local`）；是否 commit 先詢問使用者。

## 4. 驗證

- `dotnet build`、`npm run build` 皆須成功；第 8 項為是時執行 Migration。
- 可啟動時實際啟動後端，確認登入流程與回應標頭。
- 執行 `owasp-check` skill 檢查整個專案，將結果附在完成回報中。
- 回報時明確列出：已完成項目、未執行的步驟與原因、預設管理員帳號（提醒首次登入須變更密碼），以及產生加密值的指令。
