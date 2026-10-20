import type { RouteRecordRaw } from 'vue-router'

export interface MenuItem {
  title: string
  description: string
  routeName: string
  basicPermission?: string
  order: number
}

/** 由各功能 routes.ts 的 meta.menu 產生側邊選單與首頁卡片；權限過濾由使用端依登入者權限處理 */
export function buildMenu(routes: RouteRecordRaw[]): MenuItem[] {
  return routes
    .filter((r) => r.meta?.menu && typeof r.name === 'string')
    .map((r) => ({
      title: r.meta!.menu!.title,
      description: r.meta!.menu!.description,
      routeName: r.name as string,
      basicPermission: r.meta!.basicPermission,
      order: r.meta!.menu!.order,
    }))
    .sort((a, b) => a.order - b.order)
}
