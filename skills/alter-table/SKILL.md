---
name: alter-table
description: 在依 _Claude_Standards 建立的既有專案中，為既有業務資料表新增欄位，並同步更新 Model、描述、DTO、Service、前端表單與列表、Migration。在使用者要求替既有資料表加欄位、或調整既有功能的欄位時使用；修改型別、更名、刪除欄位屬破壞性變更，一律停下詢問。全部綠燈時直接修改不詢問；不 commit、不發佈。
---

# 修改既有資料表

## 用途

- **做什麼**：替 `/add-feature` 建立的業務資料表新增欄位，從資料庫到前端一次改完：Model、`HasComment` 描述、DTO 與驗證、Service、前端型別／表單／列表、Migration，完成後驗證並執行 `owasp-check`。
- **何時使用**：既有業務功能需要新增欄位時（如公告加「置頂」）。
- **不適用**：新增整個功能或資料表（用 `/add-feature`）；基本資料表（`Basic*`）的調整（直接依通用規範與 `references\basic-tables.md` 修改，並同步範本）。
- **使用方式**：`/alter-table <資料表或功能名稱> 新增 <欄位> <型別> [必填] [唯一] [預設值]、...`，例：`/alter-table Bulletin 新增 置頂 bit 必填 預設 0`。
- **產出**：修改的前後端程式與 Migration `Alter<Table><說明>`（已套用至開發資料庫）、完成回報；**不 commit、不發佈**。

規則來源：`D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\CLAUDE.md`（通用規範），再讀專案的 `CLAUDE.md`。指令寫法依 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\references\command-style.md`。

型別對應、命名、DTO 驗證、前端寫法同 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\skills\add-feature\SKILL.md` 第 1.1、2.1、2.2 節；先讀該檔，再讀本功能既有的 Model、`AppDbContext` 設定、DTO、Service、Controller 與 `src/views/<Feature>/`，修改方式與既有程式一致。

## 1. 規格與紅綠燈

整理每個欄位的名稱（PascalCase）、型別、長度、必填、唯一、中文描述、預設值、是否顯示於列表與查詢；能合理推得的採預設值（顯示於表單；文字欄位加入關鍵字查詢；不加入列表，除非使用者要求）。

| 項目 | 紅燈條件（停下詢問） |
| --- | --- |
| 對象 | 資料表不存在；為 `Basic*` 基本資料表 |
| 破壞性 | 修改既有欄位型別或長度（縮短）、更名、刪除欄位、改變可否為 null（可為 null → 不可）、刪除資料 |
| 既有資料 | 新增**不可為 null** 的欄位而未指定預設值（既有資料列無法填值） |
| 唯一 | 新增唯一欄位時，既有資料可能重複（以 `sqlcmd` 唯讀查詢確認）；可為 null 的欄位不建唯一索引 |
| 機密欄位 | 含密碼、token、金鑰等（須確認雜湊或加密、加入 `MaskedProperties`、不回傳前端） |
| 新套件 | 需要專案尚未使用的套件（取得同意後依 `add-package` skill 加入） |
| 資料庫 | 無法連線 |
| Migration | 產生的 Migration 含 `DropTable`、`DropColumn`、`RenameColumn`、非預期的 `AlterColumn`，或 EF Core 提示可能遺失資料 |
| 建置與驗證 | `dotnet build`、`npm run build` 或 Migration 失敗，自行修正 2 次仍失敗 |

- **全部綠燈**：不詢問，輸出一次「採用規格摘要」（欄位、型別、描述、預設值、影響的檔案），直接修改。
- **任一紅燈**：只列出紅燈項目、原因與建議做法詢問；破壞性變更取得同意後，完成回報須強調正式資料庫須先備份。

## 2. 修改

1. **Model**：加入屬性；非必填用可為 null 的型別。
2. **AppDbContext**：每個新欄位以 `HasComment` 加上描述；`HasMaxLength`／`HasPrecision`；有預設值時 `HasDefaultValue`；唯一值 `HasIndex(...).IsUnique()`。
3. **DTO**：`<Table>ListItem`（有顯示於列表時）、`<Table>Detail`、輸入 DTO 加入欄位與驗證（`[Required]`、`[MaxLength]`、文字 `[NoHtml]`、`[Range]`，訊息為繁體中文）。
4. **Service**：查詢投影、新增、修改加入欄位（文字 `Trim()`、選填空白存 `null`）；唯一值於新增與修改前檢查；文字欄位視需要加入關鍵字查詢。修改仍設 `UpdatedAt = DateTime.Now`。
5. **機密欄位**（有時）：加入 `Data/BasicSyslogInterceptor.cs` 的 `MaskedProperties`，不放入回傳 DTO。
6. **前端**：`types.ts` 同步型別；`components/<Table>FormModal.vue` 加入輸入欄位（長度上限與後端一致，開啟時重設）；`Index.vue` 依規格加入列表欄位；一律以 `{{ }}` 輸出。
7. **Migration**：`dotnet ef migrations add Alter<Table><說明> --project <backend 絕對路徑>\backend.csproj --startup-project <backend 絕對路徑>\backend.csproj --output-dir Data/Migrations`；讀過內容，只應有 `AddColumn`（與 `CreateIndex`），否則依第 1 節紅燈處理；再 `dotnet ef database update`（同樣指定 `--project` 與 `--startup-project`）。
8. 以 `sqlcmd` 唯讀查詢 `sys.extended_properties`，確認新欄位都有 `MS_Description`。
9. 專案 `CLAUDE.md` 有記錄此功能的特殊規則時同步更新。

## 3. 驗證

- `dotnet build`、`npm run build` 皆須成功。
- 背景啟動後端（`--Security:Login:CaptchaEnabled=false`），以預設管理員：`GET /api/<feature>` 回 200 且含新欄位（有顯示於列表時）；`POST` 送缺少必填新欄位或文字含 `<b>` 時回 400 與中文訊息。
- 寫入實測依 `add-feature` 第 3 節的前提與清理規則，只做「新增一筆含新欄位 → 讀回比對 → 修改新欄位 → 確認歷程有 `U` 且只列出變動欄位 → 刪除」。
- 驗證完即停止背景後端；執行 `owasp-check`，範圍為本次變更。

## 4. 完成回報

- 修改的檔案、新增的欄位與描述、Migration 名稱。
- 驗證結果、未實測項目、測試資料清理狀況。
- `owasp-check` 結果。
- 部署提醒：正式資料庫須先備份再套用 Migration（程式啟動時不會自動套用）；有不可為 null 的新欄位時，說明既有資料會填入的預設值。
- 尚未 commit：請確認後自行 commit，再以 `/publish-project` 發佈。
