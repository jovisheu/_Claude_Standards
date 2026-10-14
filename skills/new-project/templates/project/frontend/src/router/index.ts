import { createRouter, createWebHistory } from 'vue-router'
import AdminLayout from '@/layouts/AdminLayout.vue'
import { useAuthStore } from '@/stores/auth'
import { useMessageStore } from '@/stores/message'
import { getCurrentBasicUser } from '@/views/Login/api'
import basicGroupsRoutes from '@/views/BasicGroups/routes'
import basicUsersRoutes from '@/views/BasicUsers/routes'
import changePasswordRoutes from '@/views/ChangePassword/routes'
import forgotPasswordRoutes from '@/views/ForgotPassword/routes'
import homeRoutes from '@/views/Home/routes'
import loginRoutes from '@/views/Login/routes'
import notFoundRoutes from '@/views/NotFound/routes'
import resetPasswordRoutes from '@/views/ResetPassword/routes'

declare module 'vue-router' {
  interface RouteMeta {
    /** 不需登入即可瀏覽 */
    public?: boolean
    /** 頁面標題 */
    title?: string
    /** 瀏覽所需權限 */
    basicPermission?: string
  }
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    ...loginRoutes,
    ...forgotPasswordRoutes,
    ...resetPasswordRoutes,
    ...changePasswordRoutes,
    {
      path: '/',
      component: AdminLayout,
      children: [...homeRoutes, ...basicUsersRoutes, ...basicGroupsRoutes],
    },
    ...notFoundRoutes,
  ],
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()
  const message = useMessageStore()

  if (to.meta.public) {
    // 已登入時不需再進入登入頁
    if (to.name === 'login' && auth.isLoggedIn) return { name: 'home' }
    return true
  }

  if (!auth.isLoggedIn) {
    // 曾登入但已超過 expiresAt：清除登入狀態並提示
    if (auth.isExpired) {
      auth.clear()
      await message.error('登入已逾時，請重新登入')
    }
    return { name: 'login', query: to.fullPath === '/' ? undefined : { redirect: to.fullPath } }
  }

  // 每次開啟頁面後向後端取得一次最新權限
  if (!auth.basicUserRefreshed) {
    try {
      auth.setBasicUser(await getCurrentBasicUser())
    } catch {
      // 401 已由 interceptor 處理並導向登入頁
      return auth.isLoggedIn ? false : { name: 'login' }
    }
  }

  if (to.meta.basicPermission && !auth.hasBasicPermission(to.meta.basicPermission)) {
    await message.error('權限不足，無法瀏覽此頁面')
    return { name: 'home' }
  }

  return true
})

router.afterEach((to) => {
  const appTitle = import.meta.env.VITE_APP_TITLE
  document.title = to.meta.title ? `${to.meta.title} - ${appTitle}` : appTitle
})

export default router
