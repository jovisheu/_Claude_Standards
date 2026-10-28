/** 後端時間（yyyy-MM-ddTHH:mm:ss，本地時間）轉為 yyyy-MM-dd HH:mm */
export function formatDateTime(value?: string | null): string {
  if (!value) return '-'
  return value.replace('T', ' ').slice(0, 16)
}

/** 列表的「項次」：依頁數接續編號（每頁 10 筆時第 2 頁從 11 起） */
export function formatRowNumber(page: number, pageSize: number, index: number): number {
  return (page - 1) * pageSize + index + 1
}

/** 列表的「異動時間」：updatedAt 為 null 時顯示 createdAt */
export function formatModifiedAt(item: { createdAt: string; updatedAt?: string | null }): string {
  return formatDateTime(item.updatedAt ?? item.createdAt)
}
