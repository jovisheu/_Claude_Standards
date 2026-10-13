import type { RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  {
    path: '/forgot-password',
    name: 'forgot-password',
    component: () => import('./Index.vue'),
    meta: { public: true, title: '忘記密碼' },
  },
]

export default routes
