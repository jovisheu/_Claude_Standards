@D:/Jovi/Localhost/_ClaudeCode/_Claude_Standards/CLAUDE.md

# __PROJECT_NAME__

__PROJECT_PURPOSE__

.NET 10 Web API + Vue 3 後台系統，含登入／登出、忘記密碼、逾期變更密碼、使用者管理、群組與權限管理。

## 目錄

| 路徑 | 說明 |
| --- | --- |
| `backend/` | .NET 10 Web API（專案名 `backend`），結構依通用規範 |
| `backend/Settings/` | 設定類別（`JwtSettings`、`SecuritySettings`、`AppSettings`、`SmtpSettings`） |
| `backend/Template/` | 郵件範本（非公開目錄，啟動時載入，遺失即無法啟動） |
| `backend/Data/Migrations/` | EF Core Migration |
| `frontend/` | Vite + Vue 3 + TypeScript + Pinia + Axios + Tailwind CSS v4 |
| `frontend/src/views/[Feature]/` | 功能資料夾（`Index.vue`、`api.ts`、`types.ts`、`routes.ts`、`components/`）；`routes.ts` 的 `meta.menu` 產生側邊選單與首頁功能卡片 |
| `frontend/src/router/featureRoutes.ts` | 自動載入各功能的 `routes.ts`（新增功能不需修改 router 與選單） |
| `publish.ps1` | 一鍵發佈腳本（UTF-8 with BOM） |
| `dotnet-tools.json` | 鎖定 CycloneDX 工具版本（發佈時產生 SBOM） |

## 啟動方式（開發）

```bash
# 後端（http://localhost:__BACKEND_PORT__，Swagger：/swagger，僅 Development）
dotnet run --project backend

# 前端（http://localhost:5173，/api 代理至後端）
npm run dev --prefix frontend
```

- 測試時關閉驗證碼：`dotnet run --project backend -- --Security:Login:CaptchaEnabled=false`
- 預設管理員：`__ADMIN_USERNAME__`（初始密碼見 new-project 設定），首次登入不強制變更密碼；密碼歷程從 __SEED_DATE__ 起算，180 天後到期。
- 預設一般使用者：`__GUEST_USERNAME__`（訪客群組，只能檢視；初始密碼見 new-project 設定），密碼歷程同上。正式上線前請停用或變更密碼。

## 資料庫

- MSSQL `__DB_SERVER__` / `__DB_NAME__`，__DB_AUTH__（SQL Server 2019 基準、資料表與欄位描述、命名規則見通用規範）。
- 新增 Migration：`dotnet ef migrations add <名稱> --project backend --output-dir Data/Migrations`
- 套用：`dotnet ef database update --project backend`
- 種子：權限 8 筆（`basicuser.*`、`basicgroup.*`）、受保護的「系統管理員」群組（Id = 1）、「訪客」群組（Id = 2，全部 `*.read`）、預設管理員（Id = 1）、預設一般使用者（Id = 2，訪客群組）、初始密碼歷程（__SEED_DATE__，首次登入不強制變更）。

## 專案特有設定

- **機密加密**：`appsettings.json` 的 `ConnectionStrings:DefaultConnection`、`Jwt:Key`、`Smtp:Password` 以本專案 `AES256Key`／`AES256IV` 加密。更換時以下列指令產生加密值（讀取輸出目錄的 `appsettings.json`，須先 build）：

  ```bash
  dotnet run --project backend -- encrypt "<明文>"
  ```

- **連線字串**含 `TrustServerCertificate=True`（本機 SQL Server 使用自簽憑證）；正式環境若有受信任憑證可移除後重新加密。
- **安全參數**皆在 `appsettings.json` 的 `Security` 區段（錯誤鎖定、密碼規則與期限、重設連結時效、速率限制、驗證碼），前端提示數值由 `GET /api/auth/options` 取得。
- **前端網址**：`App:FrontendUrl` 用於組成重設密碼連結（含部署子路徑）。Production 為 `__FRONTEND_URL__`，Development 覆寫為 `http://localhost:5173`；正式網址確定後修改 `appsettings.json`。
- **SMTP**：以 MailKit 寄送（465 為隱含 TLS，其餘 SSL 連線用 STARTTLS）；`Smtp:Enabled = false` 時郵件改寫入 `backend/MailPickup/`（`.eml`，已排除於版控）。
- **系統管理員群組**：只有系統管理員能指派、編輯與管理其成員（即使其他群組擁有全部權限）；群組仍有成員時不可刪除。
- **系統錯誤紀錄與異動歷程**：保留 30 天（`BasicExceptionlog:RetentionDays`、`BasicSyslog:RetentionDays`），逾期轉存至 `backend/Logs/`（已排除於版控）；IIS 應用程式集區帳號須有網站目錄 `Logs\` 的寫入權限，部署時保留該目錄。機制見通用規範與 `references/logging-baseline.md`。
- **驗證碼**：後端以 SkiaSharp 產生 PNG，答案存於 MemoryCache（一次性、限時）。多台伺服器部署時須改用分散式快取。
- **逾期變更憑證**同樣存於 MemoryCache；應用程式重啟後失效，使用者重新登入即可。
- **前端 `.env`**：只放非機密設定，內容須維持純 ASCII（不加中文註解），否則 Windows PowerShell 5.1 讀取時會亂碼，發佈腳本讀不到 `VITE_APP_BASE`。個人設定放 `.env.local`。
- **TypeScript 鎖定 6.0.3**：TypeScript 7 為原生版本，`vue-tsc` 3.x 無法使用，升級前須確認 `vue-tsc` 已支援。

## 部署

- __DEPLOY_DESCRIPTION__，部署子路徑 `/__PROJECT_NAME__/`，前後端同源（SPA 由後端 `wwwroot` 提供，不開 CORS）。
- 請求大小上限 1 MB（Kestrel 與 IIS 皆設定於 `Program.cs`）；日後有檔案上傳功能時須調整。
- 發佈流程使用 `/publish-project`（執行 `publish.ps1`，產出 `publish/` 與 `sbom/`）；上線前須另以 SBOM 掃描工具掃描該次 SBOM。
- `backend/web.config` 移除 `Server`、`X-Powered-By` 標頭；正式環境須於 IIS 綁定 HTTPS。

## 待辦

- [ ] 確定正式網址後更新 `App:FrontendUrl`（未更新時，重設密碼信中的連結只能在伺服器本機開啟）。
- [ ] 確認正式環境的 SMTP、速率限制、鎖定等參數；正式環境另有金鑰時，以該環境的金鑰重新產生加密值。
- [ ] 正式環境 IIS 綁定 HTTPS 憑證。
- [ ] 正式上線前停用預設一般使用者 `__GUEST_USERNAME__` 或變更其密碼（初始密碼為已知值）。
- [ ] 依需求新增業務功能：使用 `/add-feature`（資料表、權限、後端 API、前端頁面與 Migration）。
