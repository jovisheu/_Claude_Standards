/** 單筆資料的異動歷程（後端 BasicSyslogHistoryResult） */
export interface BasicSyslogHistoryResult {
  retentionDays: number
  items: BasicSyslogHistoryItem[]
}

export type BasicSyslogAction = 'C' | 'U' | 'D'

export interface BasicSyslogHistoryItem {
  id: number
  action: BasicSyslogAction
  description: string | null
  username: string | null
  createdAt: string
  rows: BasicSyslogHistoryRow[]
}

export interface BasicSyslogHistoryRow {
  table: string
  tableLabel: string
  action: BasicSyslogAction
  fields: BasicSyslogHistoryField[]
}

export interface BasicSyslogHistoryField {
  name: string
  label: string
  before: string | null
  after: string | null
}
