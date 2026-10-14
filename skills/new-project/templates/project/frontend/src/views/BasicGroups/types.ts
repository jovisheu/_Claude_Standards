export interface BasicGroupListItem {
  id: number
  name: string
  description: string | null
  basicUserCount: number
  basicPermissionCount: number
  isProtected: boolean
  createdAt: string
}

export interface BasicGroupDetail {
  id: number
  name: string
  description: string | null
  isProtected: boolean
  basicPermissionIds: number[]
  createdAt: string
}

export interface BasicPermissionItem {
  id: number
  name: string
  description: string | null
  module: string
  /** 操作者是否擁有此權限（只能授予自己擁有的權限） */
  grantable: boolean
}

export interface BasicGroupFormData {
  name: string
  description: string
  basicPermissionIds: number[]
}
