/** 目前登入者 */
export interface CurrentUser {
  id: number
  username: string
  displayName: string
  email: string
  permissions: string[]
}

export interface LoginRequest {
  username: string
  password: string
  captchaId?: string
  captchaCode?: string
}

/** 登入結果；密碼逾期時只回傳 changeToken */
export interface LoginResult {
  requirePasswordChange: boolean
  changeToken: string | null
  token: string
  expiresAt: string
  user: CurrentUser
}

export interface Captcha {
  captchaId: string
  image: string
}

/** 後端提供的安全參數 */
export interface AuthOptions {
  systemName: string
  captchaEnabled: boolean
  captchaLength: number
  passwordMinLength: number
  passwordRequiredCategories: number
  passwordHistoryCount: number
  passwordExpireDays: number
  resetTokenExpireMinutes: number
}
