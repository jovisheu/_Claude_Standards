export interface ChangeExpiredPasswordRequest {
  changeToken: string
  newPassword: string
  confirmPassword: string
}
