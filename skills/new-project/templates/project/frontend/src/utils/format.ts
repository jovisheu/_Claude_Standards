/** 以本地時間顯示日期時間（後端回傳的時間即為伺服器本地時間） */
export function formatDateTime(value: string | null | undefined) {
  if (!value) return ''
  return new Date(value).toLocaleString('zh-TW', { hour12: false })
}

/** 列表的異動時間：未修改過時顯示建立時間 */
export function formatModifiedAt(item: { createdAt: string; updatedAt: string | null }) {
  return formatDateTime(item.updatedAt ?? item.createdAt)
}
