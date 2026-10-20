import axios, { AxiosError, type AxiosRequestConfig } from 'axios'
import type { ApiResponse } from '@/types/api'
import { useAuthStore } from '@/stores/auth'
import { useMessageStore } from '@/stores/message'

/** 已由 interceptor 顯示過錯誤訊息的例外（頁面 catch 只在 !handled 時補顯示） */
export interface HandledError extends AxiosError<ApiResponse> {
  handled?: boolean
}

export function isHandled(error: unknown): boolean {
  return (error as HandledError)?.handled === true
}

/** 登入逾時的導向處理（由 main.ts 註冊，避免與 router 循環相依） */
let unauthorizedHandler: (() => Promise<void> | void) | null = null

export function setUnauthorizedHandler(handler: () => Promise<void> | void) {
  unauthorizedHandler = handler
}

const http = axios.create({
  // 部署子路徑一律取自 BASE_URL，不寫死 /
  baseURL: `${import.meta.env.BASE_URL}api`,
  timeout: 30000,
  headers: { 'Content-Type': 'application/json' },
})

http.interceptors.request.use((config) => {
  const auth = useAuthStore()
  if (auth.checkLoggedIn() && auth.token) {
    config.headers.Authorization = `Bearer ${auth.token}`
  }
  return config
})

http.interceptors.response.use(
  (response) => response,
  async (error: HandledError) => {
    const message = useMessageStore()
    const status = error.response?.status
    const text = error.response?.data?.message

    if (status === 401) {
      // 登入逾時：清除登入狀態並導向登入頁
      const auth = useAuthStore()
      const hadLogin = !!auth.token
      auth.clear()
      if (hadLogin) {
        await unauthorizedHandler?.()
      }
      message.error(text || '登入已逾時，請重新登入')
    } else if (!error.response) {
      message.error('無法連線到伺服器，請稍後再試')
    } else {
      message.error(text || '操作失敗，請再試一次')
    }

    error.handled = true
    return Promise.reject(error)
  },
)

export async function get<T>(url: string, params?: object, config?: AxiosRequestConfig): Promise<ApiResponse<T>> {
  const response = await http.get<ApiResponse<T>>(url, { ...config, params })
  return response.data
}

export async function post<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<ApiResponse<T>> {
  const response = await http.post<ApiResponse<T>>(url, data ?? {}, config)
  return response.data
}

export async function put<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<ApiResponse<T>> {
  const response = await http.put<ApiResponse<T>>(url, data ?? {}, config)
  return response.data
}

export async function del<T>(url: string, config?: AxiosRequestConfig): Promise<ApiResponse<T>> {
  const response = await http.delete<ApiResponse<T>>(url, config)
  return response.data
}

export default http
