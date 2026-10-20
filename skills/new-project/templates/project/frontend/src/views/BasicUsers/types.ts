export interface BasicUserListItem {
  id: number
  username: string
  email: string
  displayName: string
  isActive: boolean
  basicGroupNames: string[]
  isManageable: boolean
  isSelf: boolean
  createdAt: string
  updatedAt: string | null
}

export interface BasicUserDetail {
  id: number
  username: string
  email: string
  displayName: string
  isActive: boolean
  basicGroupIds: number[]
  isManageable: boolean
  isSelf: boolean
  createdAt: string
  updatedAt: string | null
}

export interface BasicGroupOption {
  id: number
  name: string
  isAssignable: boolean
}

/** 表單資料（新增時含帳號；編輯時密碼留白表示不變更） */
export interface BasicUserFormData {
  username: string
  email: string
  displayName: string
  password: string
  isActive: boolean
  basicGroupIds: number[]
}
