import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import type { CurrentBasicUser, LoginResult } from '@/views/Login/types'

const STORAGE_KEY = `${import.meta.env.VITE_APP_TITLE}.auth`

interface StoredAuth {
  token: string
  expiresAt: string
  basicUser: CurrentBasicUser | null
}

function load(): StoredAuth | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    return raw ? (JSON.parse(raw) as StoredAuth) : null
  } catch {
    return null
  }
}

export const useAuthStore = defineStore('auth', () => {
  const stored = load()
  const token = ref<string | null>(stored?.token ?? null)
  const expiresAt = ref<string | null>(stored?.expiresAt ?? null)
  const basicUser = ref<CurrentBasicUser | null>(stored?.basicUser ?? null)

  /** 本次開啟頁面後是否已向後端取得最新權限 */
  const basicUserRefreshed = ref(false)

  /** 逾期變更密碼憑證（只存於記憶體，不寫入 localStorage 或網址） */
  const changeToken = ref<string | null>(null)

  /** 須同時檢查 token 存在且未超過 expiresAt */
  const isLoggedIn = computed(() => {
    if (!token.value || !expiresAt.value) return false
    const expires = new Date(expiresAt.value).getTime()
    return !Number.isNaN(expires) && expires > Date.now()
  })

  /** 曾登入但已逾時 */
  const isExpired = computed(() => !!token.value && !isLoggedIn.value)

  function persist() {
    if (token.value && expiresAt.value) {
      const data: StoredAuth = { token: token.value, expiresAt: expiresAt.value, basicUser: basicUser.value }
      localStorage.setItem(STORAGE_KEY, JSON.stringify(data))
    } else {
      localStorage.removeItem(STORAGE_KEY)
    }
  }

  function setLogin(result: LoginResult) {
    token.value = result.token
    expiresAt.value = result.expiresAt
    basicUser.value = result.basicUser
    basicUserRefreshed.value = true
    changeToken.value = null
    persist()
  }

  function setBasicUser(value: CurrentBasicUser) {
    basicUser.value = value
    basicUserRefreshed.value = true
    persist()
  }

  function setChangeToken(value: string | null) {
    changeToken.value = value
  }

  function clear() {
    token.value = null
    expiresAt.value = null
    basicUser.value = null
    basicUserRefreshed.value = false
    changeToken.value = null
    persist()
  }

  function hasBasicPermission(basicPermission: string) {
    return basicUser.value?.basicPermissionNames.includes(basicPermission) ?? false
  }

  return {
    token,
    expiresAt,
    basicUser,
    basicUserRefreshed,
    changeToken,
    isLoggedIn,
    isExpired,
    setLogin,
    setBasicUser,
    setChangeToken,
    clear,
    hasBasicPermission,
  }
})
