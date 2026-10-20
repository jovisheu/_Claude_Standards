import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { get } from '@/api/http'

/** 目前登入者（權限取自後端資料庫） */
export interface CurrentBasicUser {
  id: number
  username: string
  displayName: string
  email: string
  basicPermissionNames: string[]
}

interface StoredAuth {
  token: string
  expiresAt: string
  basicUser: CurrentBasicUser
}

// 以系統名稱區分，同一網域部署多個系統時不互相覆蓋
const STORAGE_KEY = `${import.meta.env.VITE_APP_TITLE}.auth`

function readStorage(): StoredAuth | null {
  try {
    const text = localStorage.getItem(STORAGE_KEY)
    return text ? (JSON.parse(text) as StoredAuth) : null
  } catch {
    return null
  }
}

function writeStorage(value: StoredAuth | null) {
  try {
    if (value) {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(value))
    } else {
      localStorage.removeItem(STORAGE_KEY)
    }
  } catch {
    // 無法使用 localStorage（無痕模式、容量已滿）時只保留於記憶體
  }
}

export const useAuthStore = defineStore('auth', () => {
  const stored = readStorage()
  const token = ref<string | null>(stored?.token ?? null)
  const expiresAt = ref<string | null>(stored?.expiresAt ?? null)
  const basicUser = ref<CurrentBasicUser | null>(stored?.basicUser ?? null)

  /** 本次開啟頁面後是否已由後端取得最新權限（路由守衛在第一次導覽前取得） */
  const basicUserRefreshed = ref(false)

  /** 逾期變更密碼的一次性憑證（只存於記憶體） */
  const passwordChangeToken = ref<string | null>(null)

  /** 須同時有 token 且未超過 expiresAt（後端時間為本地時間 yyyy-MM-ddTHH:mm:ss） */
  function checkLoggedIn(): boolean {
    if (!token.value || !expiresAt.value) return false
    const expires = new Date(expiresAt.value).getTime()
    return !Number.isNaN(expires) && Date.now() < expires
  }

  const isLoggedIn = computed(() => checkLoggedIn())

  function hasBasicPermission(name: string): boolean {
    return basicUser.value?.basicPermissionNames.includes(name) ?? false
  }

  function setLogin(newToken: string, newExpiresAt: string, user: CurrentBasicUser) {
    token.value = newToken
    expiresAt.value = newExpiresAt
    basicUser.value = user
    basicUserRefreshed.value = true
    passwordChangeToken.value = null
    writeStorage({ token: newToken, expiresAt: newExpiresAt, basicUser: user })
  }

  function clear() {
    token.value = null
    expiresAt.value = null
    basicUser.value = null
    basicUserRefreshed.value = false
    passwordChangeToken.value = null
    writeStorage(null)
  }

  /** 由後端重新取得登入者與權限 */
  async function refresh() {
    const response = await get<CurrentBasicUser>('/auth/me')
    if (response.datas && token.value && expiresAt.value) {
      basicUser.value = response.datas
      basicUserRefreshed.value = true
      writeStorage({ token: token.value, expiresAt: expiresAt.value, basicUser: response.datas })
    }
  }

  return {
    token,
    expiresAt,
    basicUser,
    basicUserRefreshed,
    passwordChangeToken,
    isLoggedIn,
    checkLoggedIn,
    hasBasicPermission,
    setLogin,
    clear,
    refresh,
  }
})
