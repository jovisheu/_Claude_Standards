import http from '@/api/http'
import type { ApiResponse } from '@/types/api'
import type { ResetPasswordRequest } from './types'

export async function resetPassword(request: ResetPasswordRequest) {
  const { data } = await http.post<ApiResponse>('/auth/reset-password', request)
  return data
}
