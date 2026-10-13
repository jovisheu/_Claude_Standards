import type { RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  {
    path: 'groups',
    name: 'groups',
    component: () => import('./Index.vue'),
    meta: { title: '群組管理', permission: 'groups.read' },
  },
]

export default routes
