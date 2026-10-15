/** 異動類別：C 新增、U 修改、D 刪除 */
export type BasicSyslogAction = 'C' | 'U' | 'D'

export interface BasicSyslogHistoryField {
  name: string
  description: string
  before: string | null
  after: string | null
}

export interface BasicSyslogHistoryRow {
  table: string
  tableDescription: string
  action: BasicSyslogAction
  /** 修改時只列出有變動的欄位 */
  fields: BasicSyslogHistoryField[]
}

/** 單筆資料的一次異動歷程 */
export interface BasicSyslogHistoryItem {
  id: number
  action: BasicSyslogAction
  /** 業務說明（選填，如審核通過） */
  description: string | null
  username: string | null
  createdAt: string
  rows: BasicSyslogHistoryRow[]
}

/** 單筆資料的異動歷程查詢結果 */
export interface BasicSyslogHistoryResult {
  /** 資料庫保留天數，較舊的紀錄已轉存至伺服器檔案 */
  retentionDays: number
  items: BasicSyslogHistoryItem[]
}
