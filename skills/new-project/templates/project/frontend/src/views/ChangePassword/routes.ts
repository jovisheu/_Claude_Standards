import type { RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  {
    path: '/change-password',
    name: 'change-password',
    component: () => import('./Index.vue'),
    meta: { public: true, title: '變更密碼' },
  },
]

export default routes
