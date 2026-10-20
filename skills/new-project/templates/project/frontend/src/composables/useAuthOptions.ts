import { ref } from 'vue'
import { get } from '@/api/http'

/** 後端提供的安全參數（前端提示的數值不寫死） */
export interface AuthOptions {
  captchaEnabled: boolean
  captchaLength: number
  passwordMinLength: number
  passwordRequiredCategoryCount: number
  passwordHistoryCount: number
  passwordExpireDays: number
  passwordResetTokenExpireMinutes: number
}

const options = ref<AuthOptions | null>(null)
let loading: Promise<void> | null = null

export function useAuthOptions() {
  async function load() {
    if (options.value) return
    loading ??= get<AuthOptions>('/auth/options')
      .then((response) => {
        options.value = response.datas
      })
      .finally(() => {
        loading = null
      })
    await loading
  }

  return { options, load }
}
