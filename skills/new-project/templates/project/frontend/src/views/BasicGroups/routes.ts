import type { RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  {
    path: 'basicgroups',
    name: 'basicgroups',
    component: () => import('./Index.vue'),
    meta: { title: '群組與權限管理', basicPermission: 'basicgroup.read', menu: { title: '群組與權限管理', description: '管理群組與其擁有的功能權限', order: 20 } },
  },
]

export default routes
