import type { RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  {
    path: 'basicgroups',
    name: 'basicgroups',
    component: () => import('./Index.vue'),
    meta: { title: '群組管理', basicPermission: 'basicgroup.read' },
  },
]

export default routes
