/** 側邊選單項目（basicPermission 為空表示登入即可瀏覽） */
export interface MenuItem {
  name: string
  title: string
  description: string
  basicPermission?: string
}

export const menuItems: MenuItem[] = [
  { name: 'home', title: '首頁', description: '系統首頁' },
  { name: 'basicusers', title: '使用者管理', description: '管理使用者帳號、狀態與所屬群組', basicPermission: 'basicuser.read' },
  { name: 'basicgroups', title: '群組管理', description: '管理群組與群組權限', basicPermission: 'basicgroup.read' },
]
