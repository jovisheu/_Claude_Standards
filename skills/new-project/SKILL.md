---
name: new-project
description: 依 _Claude_Standards 通用規範建立新的 .NET 10 + Vue 3 專案。在使用者要求建立新專案、初始化專案骨架時使用；設定取自 _Project_Info 與預設值，全部綠燈時直接建立不詢問，只有紅燈項目才詢問。
---


# 建立新專案

規則來源：`D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\CLAUDE.md` 與 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\references\auth-baseline.md`（登入、密碼、忘記密碼、前端登入狀態）。先完整讀過兩個檔案，建立的所有程式碼都須符合其中的技術棧、目錄結構、資安規範與 API 格式。

建立方式依第 25 項（`Project_Info.txt` 的 `使用範本`）決定：

- **使用範本（預設）**：程式碼一律取自本 skill 的專案範本 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\skills\new-project\templates\project\`（後端、前端、UI 樣式皆固定，確保每個專案一致），只替換佔位符與加入第 23 項業務功能，依第 3 節建立；不重新撰寫範本已有的程式。範本與規範不一致時以規範為準，並提醒使用者更新範本。
- **不使用範本**：改讀 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\skills\new-project\no-template.md`，以其步驟取代第 3 節，依規範重新產生所有程式碼。

兩種方式都不參考 `存放路徑` 下的其他既有專案。

## 0. 指令寫法（減少權限詢問）

紅綠燈只決定是否詢問設定值；Claude Code 的工具權限確認另外判斷，skill 無法略過。為了只在必要時跳出確認，指令一律依下列方式撰寫：

- 一次一個指令、使用絕對路徑；不以 `cd ... &&`、`;` 串接多個指令，也不在指令前加變數設定（`VAR=... cmd`）或用 `$env:` 設定環境變數；需要指定環境或覆寫設定時改用命令列參數（如 `--environment Production`、`--Security:Login:CaptchaEnabled=false`），讓指令以 `dotnet` 開頭，符合允許規則。
- 讀檔、搜尋、寫檔、修改分別用 Read、Grep／Glob、Write、Edit 工具，不用 `cat`、`sed -i`、`echo >`、heredoc；列目錄可用 `ls`。
- 不用 `python`（Windows 可能是會卡住的商店捷徑）。
- 全程不刪除檔案：使用範本時由範本複製建立；不使用範本時後端以空白範本 `dotnet new web` 建立、前端不使用 `npm create vite`（見 `no-template.md`）。兩者都不會產生需要刪除的檔案；`Remove-Item`、`rm` 在權限設定中一律詢問，因此不應出現在建立流程中。
- 佔位符一律以 Edit 工具替換（同一檔案多處相同值可用 `replace_all`），不用 `sed -i` 或 PowerShell 字串取代（PowerShell 5.1 寫檔可能改變編碼或 BOM）。
- 驗證時不以 `sqlcmd` 或其他方式異動資料庫資料（見第 4 節）；`sqlcmd` 只用於連線測試與唯讀查詢。
- 讀取 `_Project_Info` 的明文密碼時，以 Read 工具讀取後直接作為參數使用，不在回報或指令輸出中顯示。

## 1. 環境檢查

先確認以下工具已安裝並記錄版本，缺少時為紅燈（告知使用者，不自行安裝）：

```bash
dotnet --list-sdks   # 須有 10.x
node -v; npm -v
git --version
sqlcmd -?            # 選用，用於確認資料庫連線
```

同時確認目標目錄（第 3 項的 `<存放路徑>\<專案名稱>`）不存在或為空；不為空時為紅燈（列出內容），不覆蓋既有檔案。

## 2. 紅綠燈判斷

先讀取 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\_Project_Info\` 下的檔案並完成第 1 節檢查，再依下表判斷燈號；下列 25 項的值一律取自 `_Project_Info` 或預設值，不逐項詢問。

- **全部綠燈**：不詢問、不等待回覆，直接進入第 3 節（不使用範本時改依 `no-template.md`）。開工時輸出一次「採用設定摘要」（各項採用值，含第 25 項建立方式，以及「將建立的資料表」，含第 23 項的業務資料表），明文密碼以 `***` 表示。
- **任一紅燈**：只列出紅燈項目、原因與建議值詢問，綠燈項目不再列出；取得回覆後即開工，不再另外確認。
- 建立過程中才出現的紅燈（如建置失敗、要覆蓋既有資料）同樣停下詢問；其餘步驟不中途詢問。

| 項目 | 紅燈條件 |
| --- | --- |
| 環境 | 缺 .NET 10 SDK、node 或 git |
| 目標目錄（第 3 項） | `存放路徑` 不是絕對路徑、磁碟機不存在，或目標目錄已存在且不為空 |
| 專案名稱（第 1 項） | `Project_Info.txt` 不存在、`專案名稱` 空白，或不可作為識別字（含空白、特殊字元、以數字開頭） |
| 資料庫（第 5～8 項） | `DB_DefaultConnection.txt` 不存在、連線測試失敗，或目標資料庫已存在且含資料表 |
| 業務功能（第 23 項） | 有填，但無法判斷資料表欄位、型別或關聯 |
| 部署（第 19、20 項） | `部署方式` 不是 IIS／Kestrel，或指定前後端分開部署卻未提供來源白名單 |
| 建立方式（第 25 項） | `使用範本` 不是留白、「是」或「否」；或使用範本但 `templates\project\` 不存在 |
| 建置與驗證 | `dotnet build`、`npm run build` 或 Migration 失敗，自行修正 2 次仍失敗 |
| 破壞性動作 | 任何會覆蓋、刪除既有檔案或資料庫的步驟 |

`SendMail_Info.txt` 不存在、`Project_Info.txt` 的選填欄位留白、`sqlcmd` 未安裝（改以 Migration 結果確認連線）皆為綠燈，採預設值。

### 一、專案基本資訊
1. 專案名稱／系統名稱（用於方案、資料庫、頁面標題、JWT Issuer/Audience）— 預設：讀取 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\_Project_Info\Project_Info.txt`的`專案名稱`
2. 專案用途（一兩句，寫入專案 CLAUDE.md）— 預設：讀取 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\_Project_Info\Project_Info.txt`的`專案用途`
3. 專案目錄與結構 — 專案建立於 `<存放路徑>\<專案名稱>`，`存放路徑` 預設：讀取 `Project_Info.txt` 的 `存放路徑`，留白為 `D:\Jovi\Localhost\_ClaudeCode`（上層資料夾不存在時自動建立）；結構預設：`backend/` + `frontend/`，後端專案名 `backend`
4. 是否初始化 git — 預設：是，含 `.gitignore`

### 二、資料庫（MSSQL）
5. 連線字串 — 預設：讀取 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\_Project_Info\DB_DefaultConnection.txt` 的 `DefaultConnection`（`<專案名稱>` 替換為第 1 項；含明文密碼，建立時以新專案的 AES 金鑰加密後寫入 `appsettings.json`）；檔案不存在時為紅燈，詢問第 6、7 項
6. SQL Server 執行個體與資料庫名稱 — 預設：同第 5 項（資料庫 `<專案名稱>Db`）
7. 驗證方式：Windows 整合驗證／SQL 帳密 — 預設：同第 5 項
8. 建立後是否直接執行 Migration 建表 — 預設：是

**將建立的資料表**（「採用設定摘要」或紅燈詢問時須完整列出此表與種子資料；本表為基本資料表欄位的唯一定義來源）：

| 資料表 | 主要欄位 | 說明 |
| --- | --- | --- |
| `Users` | Id、Username（唯一）、Email（唯一）、PasswordHash、DisplayName、IsActive、CreatedAt、UpdatedAt、PasswordResetTokenHash、PasswordResetExpiresAt | 使用者 |
| `Groups` | Id、Name（唯一）、Description、CreatedAt | 群組（角色） |
| `Permissions` | Id、Name（唯一）、Description、Module | 權限，以靜態種子建立 |
| `UserGroups` | UserId + GroupId（複合主鍵） | 使用者與群組多對多 |
| `GroupPermissions` | GroupId + PermissionId（複合主鍵） | 群組與權限多對多 |
| `_LoginErr_log` | Id、Username、CreatedAt；索引 (Username, CreatedAt) | 登入失敗紀錄（鎖定判斷） |
| `_Pwd_log` | Id、Username、PasswordHash、CreatedAt；索引 (Username, CreatedAt) | 密碼歷程（重複與到期判斷） |

- 種子資料：權限（`users.read`、`groups.update` 等）、受保護的「系統管理員」群組（`Id = 1`）、第 9 項的預設管理員，以及一筆初始密碼歷程（建立日期為建立專案當日，首次登入不強制變更密碼）。
- 第 23 項有業務功能時，另列出該功能的資料表與欄位。
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
17. SMTP：主機、Port、SSL、寄件者名稱與地址、帳密 — 預設：讀取 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\_Project_Info\SendMail_Info.txt`（`Smtp` 區段，`<專案名稱>` 替換為第 1 項；`Password` 為明文，建立時以新專案的 AES 金鑰加密後寫入 `appsettings.json`）；檔案不存在時先寫入本機檔案（`Enabled = false`）
18. 前端網址（重設連結取自設定檔，須含部署子路徑）— 預設：`http://localhost/<專案名稱>`；`Project_Info.txt` 的 `正式網址` 有填時一併寫入

### 六、部署
19. 部署方式：IIS（產生 `web.config` 移除標頭，需 IIS 10+）／Kestrel 獨立執行 — 預設：`Project_Info.txt` 的 `部署方式`，留白為 IIS；部署子路徑預設 `/<專案名稱>/`（站台根目錄為 `/`）
20. 前後端是否同源 — 預設：同源（SPA 由後端 `wwwroot` 提供，不開 CORS）；分開部署時提供來源白名單
21. 是否附發佈用 `.ps1` 腳本 — 預設：是（以 `publish-project` skill 的範本建立，UTF-8 with BOM）

### 七、功能範圍
22. 第一版基本功能 — 預設：登入／登出、忘記密碼、逾期變更密碼、使用者管理、群組與權限管理
23. 其他業務功能：功能名稱與主要欄位（依規範產生 Controller／Service／DTO／Model 與前端功能資料夾）— 預設：`Project_Info.txt` 的 `業務功能`，留白為無
24. 版面需求 — 側邊選單 + 頂列的後台版面（使用範本時沿用範本版面）；主色、Logo 取自 `Project_Info.txt` 的 `主色`、`Logo`（Logo 填檔案路徑），留白時：使用範本沿用範本樣式（主色 `#1e6fd9`、側邊選單顯示系統名稱文字），不使用範本則自行採用預設樣式

### 八、建立方式
25. 是否使用範本 — 預設：`Project_Info.txt` 的 `使用範本`，留白為「是」（複製 `templates\project\`，程式與 UI 和範本一致）；「否」時依 `no-template.md` 重新產生

## 3. 建立專案（使用範本）

第 25 項為「否」時跳過本節，改依 `no-template.md` 建立，完成後接第 4 節驗證。

燈號判斷完成（全部綠燈，或紅燈已取得回覆）後依序建立，每完成一個階段簡短回報，不中途詢問。以下 `<範本>` 指 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\skills\new-project\templates`。

範本已包含規範要求的所有基本功能與資安設定（JWT 驗證、預設 `[Authorize]`、權限查資料庫、全域例外處理、各狀態碼的 `ApiResponse`、安全標頭、速率限制、驗證碼、AES 加密設定與 `encrypt` 工具模式、單一密碼設定方法、郵件範本、前端 interceptor 與登入狀態、部署子路徑、`web.config`），不需另外撰寫。

1. **複製範本**：
   - `Copy-Item -LiteralPath "<範本>\project" -Destination "<專案目錄>" -Recurse`（目標目錄不存在時會一併建立；上層資料夾不存在時先以 `New-Item -ItemType Directory` 建立）。
   - 發佈腳本另外複製：`Copy-Item -LiteralPath "D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\skills\publish-project\templates\publish.ps1" -Destination "<專案目錄>\publish.ps1"`（範本為 UTF-8 with BOM，複製即保留）。
2. **產生金鑰與雜湊**：將 `<範本>\keygen` 以 `Copy-Item -Recurse` 複製到 scratchpad，執行 `dotnet run --project <scratchpad>\keygen\keygen.csproj -- "<第 10 項密碼>"`，取得 `AES256Key`、`AES256IV`、`JwtKey`（Base64，`TokenService` 以 Base64 解碼）、`AdminPasswordHash`。輸出含機密，只用於填入範本，不寫入回報。不可沿用其他專案的值。
3. **替換佔位符**：以 Edit 工具將範本中的 `__名稱__` 換成實際值：

   | 佔位符 | 值 | 所在檔案 |
   | --- | --- | --- |
   | `__PROJECT_NAME__` | 第 1 項 | `backend/appsettings.json`、`backend/Program.cs`、`frontend/.env`、`CLAUDE.md` |
   | `__PROJECT_NAME_LOWER__` | 第 1 項轉小寫 | `frontend/package.json`、`frontend/package-lock.json`（兩處） |
   | `__PROJECT_PURPOSE__` | 第 2 項 | `CLAUDE.md` |
   | `__BACKEND_PORT__` | 5100～5999 中未被其他專案使用的埠號 | `backend/Properties/launchSettings.json`、`frontend/vite.config.ts`、`CLAUDE.md` |
   | `__AES256_KEY__`、`__AES256_IV__` | keygen 輸出 | `backend/appsettings.json` |
   | `__ENC_CONNECTION_STRING__`、`__ENC_JWT_KEY__`、`__ENC_SMTP_PASSWORD__` | 第 4 步產生的加密值 | `backend/appsettings.json` |
   | `__FRONTEND_URL__` | 第 18 項（`正式網址` 有填時用正式網址，否則 `http://localhost/<專案名稱>`） | `backend/appsettings.json`、`CLAUDE.md` |
   | `__SMTP_ENABLED__`、`__SMTP_HOST__`、`__SMTP_PORT__`、`__SMTP_USE_SSL__`、`__SMTP_SENDER_NAME__`、`__SMTP_SENDER_EMAIL__`、`__SMTP_USERNAME__` | 第 17 項（`SendMail_Info.txt` 不存在時 `Enabled` 為 `false`、其餘留空字串、Port `587`、UseSsl `true`） | `backend/appsettings.json` |
   | `__SEED_YEAR__`、`__SEED_MONTH__`、`__SEED_DAY__` | 建立專案當日（數字，不補 0） | `backend/Data/SeedData.cs` |
   | `__SEED_DATE__` | 建立專案當日（`yyyy-MM-dd`） | `CLAUDE.md` |
   | `__ADMIN_USERNAME__`、`__ADMIN_EMAIL__`、`__ADMIN_DISPLAY_NAME__` | 第 9 項 | `backend/Data/SeedData.cs`（帳號兩處）、`CLAUDE.md` |
   | `__ADMIN_PASSWORD_HASH__` | keygen 輸出的 `AdminPasswordHash` | `backend/Data/SeedData.cs` |
   | `__DB_SERVER__`、`__DB_NAME__`、`__DB_AUTH__` | 第 6、7 項（`__DB_AUTH__` 為「SQL 帳密驗證」或「Windows 整合驗證」） | `CLAUDE.md` |
   | `__DEPLOY_DESCRIPTION__` | 第 19 項：「IIS（需 IIS 10 以上與 ASP.NET Core Hosting Bundle）」或「Kestrel 獨立執行」 | `CLAUDE.md` |

   - 加密值須先完成上表其餘替換（至少 `__AES256_KEY__`、`__AES256_IV__`）並 `dotnet build`，才能以工具模式產生，見第 4 步。
   - 替換完成後以 Grep 搜尋 `__[A-Z0-9_]+__`（排除 `node_modules`），須為 0 筆。
4. **加密機密**：`dotnet build <backend 絕對路徑>\backend.csproj` 後，以 `dotnet run --project <backend 絕對路徑>\backend.csproj --no-build -- encrypt "<明文>"` 逐一產生：
   - 連線字串：`DB_DefaultConnection.txt` 的 `DefaultConnection`（`<專案名稱>` 替換為第 1 項）；未含 `TrustServerCertificate` 時加上 `TrustServerCertificate=True;`（SqlClient 預設加密連線，本機 SQL Server 為自簽憑證）。
   - JWT 金鑰：keygen 輸出的 `JwtKey`。
   - SMTP 密碼：`SendMail_Info.txt` 的 `Password`；檔案不存在時 `__ENC_SMTP_PASSWORD__` 換成空字串（為空時不解密）。
   - 明文不得寫入專案任何檔案或回報內容。
5. **主色與 Logo**（第 24 項，留白則略過）：
   - 主色：將 `frontend/src/style.css` 的 `@theme` 中 `--color-primary-50`～`--color-primary-900` 換成以主色為 `600` 的同色系深淺。
   - Logo：複製圖檔至 `frontend/src/assets/logo.<副檔名>`，在 `frontend/src/layouts/AdminLayout.vue` 側邊選單標題與 `frontend/src/views/Login/Index.vue` 標題前加上 `<img>`（`alt` 為系統名稱），保留系統名稱文字。
6. **業務功能**（第 23 項，留白則略過）：以範本的使用者管理（`Users`）為樣板，依規範新增：
   - 後端：`Models/<Entity>/`、`AppDbContext` 的 `DbSet` 與欄位設定、`DTOs/<Feature>/`、`Services/<Feature>/`、`Controllers/<Feature>/`；權限加入 `Data/PermissionNames.cs`，並在 `Data/SeedData.cs` 的 `Permissions` 往後新增（Id 接續，不改動既有 Id；系統管理員群組自動擁有全部權限）。
   - 前端：`src/views/<Feature>/`（`Index.vue`、`api.ts`、`types.ts`、`routes.ts`、`components/<Feature>FormModal.vue`），版面、表格、Modal、按鈕樣式比照 `Users`；在 `src/router/index.ts` 加入路由、`src/layouts/menu.ts` 加入選單項目（首頁卡片同步出現）。
   - 專案 CLAUDE.md 的種子說明更新權限筆數。
7. **安裝前端套件**：`npm ci --prefix <frontend 絕對路徑>`（依範本的 `package-lock.json` 安裝，不升級版本）。
8. **Migration**（第 8 項為是時）：`dotnet ef migrations add InitialCreate --project <backend 絕對路徑>\backend.csproj --output-dir Data/Migrations`，再 `dotnet ef database update --project <backend 絕對路徑>\backend.csproj`；確認產生的 Migration 不含明文密碼。
9. **git**：`git init <專案目錄>`（範本已含 `.gitignore`）；**不 commit**，於完成回報提醒使用者自行 commit。

## 4. 驗證

- 使用範本時：佔位符已全部替換（Grep `__[A-Z0-9_]+__` 為 0 筆）。
- `dotnet build`、`npm run build` 皆須成功；第 8 項為是時執行 Migration。失敗時自行修正，2 次仍失敗為紅燈，停下回報錯誤並詢問。使用範本時，範本本身的問題（非佔位符或業務功能造成）修正後，於回報中提醒使用者同步更新範本。
- 可啟動時實際啟動後端（背景執行），以 `curl -s http://localhost:<port>/...` 確認登入流程與回應標頭。**驗證只做不留下資料的請求**，建完的資料庫只含種子資料：
  - 可測：回應標頭、`ApiResponse` 格式、JSON 格式錯誤與模型驗證、驗證碼錯誤（不計入錯誤次數）、401（無 Token、無效 Token）、`/api` 不存在路徑 404、錯誤 Content-Type 回 415 `ApiResponse`、連續送出驗證碼錯誤的登入直到 429、以命令列參數關閉驗證碼啟動（`dotnet run --project <backend 絕對路徑> -- --Security:Login:CaptchaEnabled=false`，`Program.cs` 須以 `WebApplication.CreateBuilder(args)` 讀取命令列設定）後用預設管理員登入，確認直接回傳 JWT 與 `expiresAt`（`requirePasswordChange` 為 `false`），並以該 Token 呼叫需登入的端點（如 `/api/auth/me`）。
  - 不測：完成逾期變更或重設密碼、帳密錯誤登入（會寫入 `_LoginErr_log`）、新增／編輯／刪除資料、寄送郵件。這些流程只做程式檢視，於回報中列為「未實測」。
  - 不以 `sqlcmd` 或其他方式異動資料庫資料；若不慎寫入，視為破壞性動作的紅燈，停下詢問。
- 已執行發佈腳本時，以 `dotnet <publish 絕對路徑>\backend.dll --environment Production --contentRoot <publish 絕對路徑> --urls http://localhost:<port>` 啟動發佈結果（須指定 `--contentRoot`，否則找不到 `appsettings.json` 與 `wwwroot`），確認 `/swagger` 與不存在的檔案路徑（如 `/missing.js`）回 404、`index.html` 與 SPA fallback 為 `Cache-Control: no-cache`；測試完畢即停止程式。測試指令勿 `cd` 進 `publish/`，否則該目錄被占用，下次發佈無法清除。
- 執行 `owasp-check` skill 檢查整個專案，將結果附在完成回報中（發現問題直接修正，不另外詢問；涉及第 2 節紅燈條件時才詢問）。
- 回報時明確列出：已完成項目、未執行的步驟與原因、預設管理員帳號，產生加密值的指令，以及「尚未 commit，請確認後自行 commit」。
