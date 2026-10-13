import type { RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'login',
    component: () => import('./Index.vue'),
    meta: { public: true, title: '登入' },
  },
]

export default routes
