/** API 統一回傳格式 */
export interface ApiResponse<T = unknown> {
  status: boolean
  statusCode: number
  message: string
  datas: T | null
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

/** 分頁查詢條件 */
export interface PagedQuery {
  page: number
  pageSize: number
  keyword?: string
}
