import type { RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  {
    path: '/:pathMatch(.*)*',
    name: 'not-found',
    component: () => import('./Index.vue'),
    meta: { public: true, title: '找不到頁面' },
  },
]

export default routes
