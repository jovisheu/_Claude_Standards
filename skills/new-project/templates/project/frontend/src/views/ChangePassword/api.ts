import { post } from '@/api/http'
import type { ChangeExpiredPasswordFormData } from './types'

export function changeExpiredPassword(data: ChangeExpiredPasswordFormData) {
  return post<null>('/auth/change-expired-password', data)
}
