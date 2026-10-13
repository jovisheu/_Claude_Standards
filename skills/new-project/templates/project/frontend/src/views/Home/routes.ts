import type { RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  {
    path: '',
    name: 'home',
    component: () => import('./Index.vue'),
    meta: { title: '首頁' },
  },
]

export default routes
