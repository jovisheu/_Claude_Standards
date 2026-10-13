import axios, { AxiosError } from 'axios'
import type { ApiResponse } from '@/types/api'
import { useAuthStore } from '@/stores/auth'
import { useMessageStore } from '@/stores/message'

/** 已由 interceptor 顯示過錯誤的例外 */
export interface HandledError {
  handled?: boolean
}

/** 判斷錯誤是否已顯示過（頁面 catch 只在未處理時補顯示） */
export function isHandled(error: unknown): boolean {
  return typeof error === 'object' && error !== null && (error as HandledError).handled === true
}

let unauthorizedHandler: (() => void) | null = null

/** 401 時的導向處理（由 main.ts 設定，避免與 router 循環相依） */
export function setUnauthorizedHandler(handler: () => void) {
  unauthorizedHandler = handler
}

const http = axios.create({
  baseURL: `${import.meta.env.BASE_URL}api`,
  timeout: 30000,
})

http.interceptors.request.use((config) => {
  const auth = useAuthStore()
  if (auth.isLoggedIn && auth.token) {
    config.headers.Authorization = `Bearer ${auth.token}`
  }
  return config
})

http.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiResponse>) => {
    const message = useMessageStore()
    const status = error.response?.status
    const serverMessage = error.response?.data?.message

    if (status === 401) {
      // 401 視為登入逾時
      useAuthStore().clear()
      await message.error(serverMessage || '登入已逾時，請重新登入')
      unauthorizedHandler?.()
    } else if (!error.response) {
      await message.error('無法連線至伺服器，請稍後再試')
    } else {
      await message.error(serverMessage || '操作失敗，請再試一次')
    }

    ;(error as AxiosError & HandledError).handled = true
    return Promise.reject(error)
  },
)

export default http
