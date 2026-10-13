export interface ForgotPasswordRequest {
  username: string
  email: string
  captchaId?: string
  captchaCode?: string
}
