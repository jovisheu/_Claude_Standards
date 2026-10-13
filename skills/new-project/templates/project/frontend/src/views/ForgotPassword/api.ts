import http from '@/api/http'
import type { ApiResponse } from '@/types/api'
import type { ForgotPasswordRequest } from './types'

export async function forgotPassword(request: ForgotPasswordRequest) {
  const { data } = await http.post<ApiResponse>('/auth/forgot-password', request)
  return data
}
