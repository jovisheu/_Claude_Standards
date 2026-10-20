import type { CurrentBasicUser } from '@/stores/auth'

export interface CaptchaResult {
  captchaId: string
  image: string
  expireSeconds: number
}

export interface LoginFormData {
  username: string
  password: string
  captchaId?: string
  captchaCode?: string
}

export interface LoginResult {
  requirePasswordChange: boolean
  passwordChangeToken: string | null
  token: string | null
  expiresAt: string | null
  basicUser: CurrentBasicUser | null
}
