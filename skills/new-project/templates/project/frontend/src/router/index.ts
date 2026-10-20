import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import AdminLayout from '@/layouts/AdminLayout.vue'
import { useAuthStore } from '@/stores/auth'
import { useMessageStore } from '@/stores/message'
import { adminRoutes, catchAllRoutes, publicRoutes } from './featureRoutes'

const routes: RouteRecordRaw[] = [
  ...publicRoutes,
  { path: '/', component: AdminLayout, children: adminRoutes },
  ...catchAllRoutes,
]

const router = createRouter({
  // 部署子路徑取自 BASE_URL
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
})

/** 只接受站內路徑，避免開放式重新導向 */
export function safeRedirect(value: unknown): string {
  return typeof value === 'string' && value.startsWith('/') && !value.startsWith('//') && !value.startsWith('/\\') ? value : '/'
}

router.beforeEach(async (to) => {
  const auth = useAuthStore()
  const loggedIn = auth.checkLoggedIn()

  if (to.meta.public) {
    if (to.name === 'login' && loggedIn) {
      return { path: safeRedirect(to.query.redirect) }
    }
    return true
  }

  if (!loggedIn) {
    // 有 token 但已過期：清除登入狀態並提示
    if (auth.token) {
      auth.clear()
      useMessageStore().info('登入已逾時，請重新登入')
    }
    return { name: 'login', query: to.fullPath === '/' ? {} : { redirect: to.fullPath } }
  }

  // 權限以後端資料庫為準：本次開啟頁面後第一次導覽前先取得最新權限，避免以快取的舊權限判斷
  if (!auth.basicUserRefreshed) {
    try {
      await auth.refresh()
    } catch {
      // 錯誤已由 interceptor 處理（401 時已導向登入頁）
      return false
    }
  }

  if (to.meta.basicPermission && !auth.hasBasicPermission(to.meta.basicPermission)) {
    useMessageStore().error('您沒有使用此功能的權限')
    return { name: 'home' }
  }

  return true
})

router.afterEach((to) => {
  const systemName = import.meta.env.VITE_APP_TITLE
  document.title = to.meta.title ? `${to.meta.title} - ${systemName}` : systemName
})

export default router
