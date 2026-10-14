export interface BasicUserListItem {
  id: number
  username: string
  email: string
  displayName: string
  isActive: boolean
  basicGroupNames: string[]
  createdAt: string
}

export interface BasicUserDetail {
  id: number
  username: string
  email: string
  displayName: string
  isActive: boolean
  basicGroupIds: number[]
  createdAt: string
  updatedAt: string | null
}

export interface BasicGroupOption {
  id: number
  name: string
  /** 操作者是否可指派此群組 */
  assignable: boolean
}

/** 新增／編輯表單送出的資料（編輯時 username 不送出、password 留白表示不變更） */
export interface BasicUserFormData {
  username: string
  email: string
  displayName: string
  password: string
  isActive: boolean
  basicGroupIds: number[]
}
