import { post } from '@/api/http'
import type { ResetPasswordFormData } from './types'

export function resetPassword(data: ResetPasswordFormData) {
  return post<null>('/auth/reset-password', data)
}
