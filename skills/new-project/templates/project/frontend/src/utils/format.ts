/** 後端時間（yyyy-MM-ddTHH:mm:ss，本地時間）轉為 yyyy-MM-dd HH:mm */
export function formatDateTime(value?: string | null): string {
  if (!value) return '-'
  return value.replace('T', ' ').slice(0, 16)
}

/** 列表的「異動時間」：updatedAt 為 null 時顯示 createdAt */
export function formatModifiedAt(item: { createdAt: string; updatedAt?: string | null }): string {
  return formatDateTime(item.updatedAt ?? item.createdAt)
}
