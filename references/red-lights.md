# 共用紅綠燈規則

修改既有專案的共用 Skill（`add-feature`、`alter-table`）共用的燈號處理與紅燈條件；各 Skill 只列特有的紅燈條件。

## 燈號處理

* **全部綠燈**：不詢問、不等待回覆，輸出一次採用摘要（內容依各 Skill 規定）後直接開工。
* **任一紅燈**：只列出紅燈項目、原因與建議值（或建議做法）詢問，綠燈項目不再列出；取得回覆後即開工，不再另外確認。
* 過程中才出現的紅燈（如建置失敗、Migration 出現破壞性操作）同樣停下詢問；其餘步驟不中途詢問。
* 工作目錄有其他未 commit 的變更不是紅燈，但於摘要中列出，提醒使用者本次變更會與其混在一起。

## 共用紅燈條件

| 項目 | 紅燈條件（停下詢問） |
| --- | --- |
| 破壞性動作 | 需修改或刪除既有資料表、欄位、資料或檔案 |
| 機密欄位 | 含密碼、token、金鑰等欄位（須確認處理方式：雜湊或加密、列入 `BasicSyslogInterceptor` 的 `MaskedProperties`、不回傳前端） |
| 新套件 | 需要加入專案尚未使用的 NuGet 或 npm 套件（說明用途、建議的套件與版本、是否有內建或既有套件可替代，取得同意後依 `add-package` skill 加入） |
| 資料庫 | 無法連線，或版本低於 SQL Server 2019 |
| Migration | `Up()` 含 `DropTable`、`DropColumn`、`RenameColumn`、非預期的 `AlterColumn`（只因描述變更產生者可說明後繼續），或 EF Core 提示可能遺失資料；`hooks/check-migration.js` 於產生後自動回報 |
| 建置與驗證 | `dotnet build`、`npm run build` 或 Migration 失敗，自行修正 2 次仍失敗 |
