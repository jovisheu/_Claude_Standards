import type { RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  {
    path: 'basicusers',
    name: 'basicusers',
    component: () => import('./Index.vue'),
    meta: { title: '使用者管理', basicPermission: 'basicuser.read' },
  },
]

export default routes
