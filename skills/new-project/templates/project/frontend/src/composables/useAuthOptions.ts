import { ref } from 'vue'
import { getAuthOptions } from '@/views/Login/api'
import type { AuthOptions } from '@/views/Login/types'

const options = ref<AuthOptions | null>(null)
let loading: Promise<AuthOptions | null> | null = null

/**
 * 後端提供的安全參數（驗證碼、密碼規則），整個應用程式只取一次
 */
export function useAuthOptions() {
  if (!options.value && !loading) {
    loading = getAuthOptions()
      .then((data) => {
        options.value = data
        return data
      })
      .catch(() => {
        loading = null
        return null
      })
  }

  return {
    options,
    ready: () => loading ?? Promise.resolve(options.value),
  }
}
