import type { RouteRecordRaw } from 'vue-router'

declare module 'vue-router' {
  interface RouteMeta {
    /** 不需登入的頁面（登入、忘記密碼等），不套用後台版面 */
    public?: boolean
    /** 頁面標題 */
    title?: string
    /** 進入頁面所需權限（後端仍會再檢查） */
    basicPermission?: string
    /** 側邊選單與首頁卡片（有設定才顯示）；order 小者在前 */
    menu?: { title: string; description: string; order: number }
  }
}

// 各功能的 routes.ts 自動載入；新增功能只需建立 src/views/<Feature>/routes.ts，不需修改 router 與選單
const modules = import.meta.glob<{ default: RouteRecordRaw[] }>('../views/*/routes.ts', { eager: true })
const featureRoutes = Object.values(modules).flatMap((m) => m.default)

const isCatchAll = (route: RouteRecordRaw) => route.path.includes(':pathMatch')

/** 套用後台版面的頁面 */
export const adminRoutes = featureRoutes.filter((r) => !r.meta?.public)

/** 不需登入的頁面（不含 404） */
export const publicRoutes = featureRoutes.filter((r) => r.meta?.public && !isCatchAll(r))

/** 404（須放在最後） */
export const catchAllRoutes = featureRoutes.filter((r) => isCatchAll(r))
