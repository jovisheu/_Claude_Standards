export interface GroupListItem {
  id: number
  name: string
  description: string | null
  memberCount: number
  permissionCount: number
  isProtected: boolean
  createdAt: string
}

export interface GroupDetail {
  id: number
  name: string
  description: string | null
  isProtected: boolean
  permissionIds: number[]
  createdAt: string
}

export interface PermissionItem {
  id: number
  name: string
  description: string | null
  module: string
  /** 操作者是否擁有此權限（只能授予自己擁有的權限） */
  grantable: boolean
}

export interface GroupFormData {
  name: string
  description: string
  permissionIds: number[]
}
