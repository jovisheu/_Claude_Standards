import type { RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  {
    path: 'basicusers',
    name: 'basicusers',
    component: () => import('./Index.vue'),
    meta: { title: '使用者管理', basicPermission: 'basicuser.read', menu: { title: '使用者管理', description: '管理帳號、所屬群組與啟用狀態', order: 10 } },
  },
]

export default routes
