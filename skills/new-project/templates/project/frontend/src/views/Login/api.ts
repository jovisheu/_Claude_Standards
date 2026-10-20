import { get, post } from '@/api/http'
import type { CaptchaResult, LoginFormData, LoginResult } from './types'

export function getCaptcha() {
  return get<CaptchaResult>('/auth/captcha')
}

export function login(data: LoginFormData) {
  return post<LoginResult>('/auth/login', data)
}
