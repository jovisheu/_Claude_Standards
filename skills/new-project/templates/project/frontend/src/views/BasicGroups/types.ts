export interface BasicGroupListItem {
  id: number
  name: string
  description: string | null
  isProtected: boolean
  isManageable: boolean
  basicUserCount: number
  basicPermissionCount: number
  createdAt: string
  updatedAt: string | null
}

export interface BasicGroupDetail {
  id: number
  name: string
  description: string | null
  isProtected: boolean
  isManageable: boolean
  basicPermissionIds: number[]
  createdAt: string
  updatedAt: string | null
}

export interface BasicPermissionItem {
  id: number
  name: string
  description: string | null
  module: string
  isAssignable: boolean
}

export interface BasicGroupFormData {
  name: string
  description: string
  basicPermissionIds: number[]
}
