import type { RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  {
    path: '/reset-password',
    name: 'reset-password',
    component: () => import('./Index.vue'),
    meta: { public: true, title: '重設密碼' },
  },
]

export default routes
