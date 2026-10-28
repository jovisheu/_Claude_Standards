---
paths:
  - "**/frontend/src/**/*.{vue,ts}"
---

# 前端 UI 慣例（_Claude_Standards）

通用規範（`D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\CLAUDE.md`）的一部分，處理前端程式時載入。

* 操作結果（儲存成功、刪除成功、操作失敗）一律以訊息視窗顯示，**不使用頁面內提示列**。
* API 失敗時由 Axios interceptor 顯示錯誤並標記 `handled`；頁面 catch 只在 `!handled` 時補顯示「操作失敗，請再試一次」，避免重複提示。
* 資料列表一律使用共用樣式（`src/style.css`）：表格外框為 `<div class="data-table-wrap">`（框內捲動，標題列固定）、表格為 `<table class="data-table">`（偶數列底色、滑鼠移入變色）；不在頁面另寫表格的底色、框線或捲動樣式，要調整時改 `style.css`，讓所有列表一致。
* 列表第一欄一律為「項次」：`<th class="row-number">項次</th>`，內容以 `formatRowNumber(query.page, query.pageSize, index)`（`src/utils/format.ts`）依頁數接續編號（每頁 10 筆時第 2 頁從 11 起）；載入中、查無資料列的 `colspan` 須含此欄。
* 列表顯示「異動時間」欄：`updatedAt` 為 null 時顯示 `createdAt`（共用 `src/utils/format.ts` 的 `formatModifiedAt`），不另顯示建立時間欄。
* 沒有該功能的 `create`／`update`／`delete` 權限，或資料受保護（如系統管理員群組不可刪除、權限不可修改；不可停用或刪除自己；不可授予高於自身的權限）時，對應的按鈕、輸入欄位與勾選項一律以 `v-if` **不渲染**，不可用 `disabled`、`v-show` 或 CSS 隱藏（元素仍在 DOM 中，可被瀏覽器開發工具改回可用）；需讓使用者知道現況時改以純文字顯示（如受保護群組的權限清單、不可變更的帳號）。
  * 前端不渲染只是體驗與減少誤用，後端仍須檢查（見通用規範「存取控制失效」）。
  * `disabled` 只用於與權限無關的暫時狀態（送出中、分頁邊界、必填未填）。
* 列表操作欄提供「歷程」按鈕（有該功能的 `update` 權限才顯示），以共用元件 `src/components/BasicSyslogModal.vue` 顯示該筆資料的異動歷程；API 函式命名如 `getBasicUserBasicSyslogs(id)`。
* 新增／編輯共用同一個 Modal，放在該功能的 `components/`：以 `defineModel` 控制開關、`item` prop 區分新增或編輯（`null` 為新增），開啟時重設表單，送出以事件把資料交回 `Index.vue`。
