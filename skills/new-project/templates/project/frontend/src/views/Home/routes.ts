import type { RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  {
    path: '',
    name: 'home',
    component: () => import('./Index.vue'),
    meta: { title: '首頁', menu: { title: '首頁', description: '系統首頁', order: 0 } },
  },
]

export default routes
