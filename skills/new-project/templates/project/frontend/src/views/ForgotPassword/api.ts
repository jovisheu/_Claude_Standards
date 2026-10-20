import { get, post } from '@/api/http'
import type { CaptchaResult, ForgotPasswordFormData } from './types'

export function getCaptcha() {
  return get<CaptchaResult>('/auth/captcha')
}

export function forgotPassword(data: ForgotPasswordFormData) {
  return post<null>('/auth/forgot-password', data)
}
