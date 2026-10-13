import type { RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  {
    path: 'users',
    name: 'users',
    component: () => import('./Index.vue'),
    meta: { title: '使用者管理', permission: 'users.read' },
  },
]

export default routes
