export interface UserListItem {
  id: number
  username: string
  email: string
  displayName: string
  isActive: boolean
  groupNames: string[]
  createdAt: string
}

export interface UserDetail {
  id: number
  username: string
  email: string
  displayName: string
  isActive: boolean
  groupIds: number[]
  createdAt: string
  updatedAt: string | null
}

export interface GroupOption {
  id: number
  name: string
  /** 操作者是否可指派此群組 */
  assignable: boolean
}

/** 新增／編輯表單送出的資料（編輯時 username 不送出、password 留白表示不變更） */
export interface UserFormData {
  username: string
  email: string
  displayName: string
  password: string
  isActive: boolean
  groupIds: number[]
}
