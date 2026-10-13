import http from '@/api/http'
import type { ApiResponse } from '@/types/api'
import type { ChangeExpiredPasswordRequest } from './types'

export async function changeExpiredPassword(request: ChangeExpiredPasswordRequest) {
  const { data } = await http.post<ApiResponse>('/auth/change-expired-password', request)
  return data
}
