export interface CaptchaResult {
  captchaId: string
  image: string
  expireSeconds: number
}

export interface ForgotPasswordFormData {
  username: string
  email: string
  captchaId?: string
  captchaCode?: string
}
