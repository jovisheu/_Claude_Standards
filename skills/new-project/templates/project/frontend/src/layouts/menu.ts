/** 側邊選單項目（permission 為空表示登入即可瀏覽） */
export interface MenuItem {
  name: string
  title: string
  description: string
  permission?: string
}

export const menuItems: MenuItem[] = [
  { name: 'home', title: '首頁', description: '系統首頁' },
  { name: 'users', title: '使用者管理', description: '管理使用者帳號、狀態與所屬群組', permission: 'users.read' },
  { name: 'groups', title: '群組管理', description: '管理群組與群組權限', permission: 'groups.read' },
]
