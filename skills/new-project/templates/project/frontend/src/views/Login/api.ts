import http from '@/api/http'
import type { ApiResponse } from '@/types/api'
import type { AuthOptions, Captcha, CurrentUser, LoginRequest, LoginResult } from './types'

export async function getAuthOptions() {
  const { data } = await http.get<ApiResponse<AuthOptions>>('/auth/options')
  return data.datas!
}

export async function getCaptcha() {
  const { data } = await http.get<ApiResponse<Captcha>>('/auth/captcha')
  return data.datas!
}

export async function login(request: LoginRequest) {
  const { data } = await http.post<ApiResponse<LoginResult>>('/auth/login', request)
  return data.datas!
}

export async function getCurrentUser() {
  const { data } = await http.get<ApiResponse<CurrentUser>>('/auth/me')
  return data.datas!
}
