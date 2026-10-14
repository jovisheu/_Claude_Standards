import http from '@/api/http'
import type { ApiResponse, PagedQuery } from '@/types/api'
import type { BasicGroupDetail, BasicGroupFormData, BasicGroupListItem, BasicPermissionItem } from './types'

export async function getBasicGroups(query: PagedQuery) {
  const { data } = await http.get<ApiResponse<BasicGroupListItem[]>>('/basicgroups', { params: query })
  return data
}

export async function getBasicGroup(id: number) {
  const { data } = await http.get<ApiResponse<BasicGroupDetail>>(`/basicgroups/${id}`)
  return data.datas!
}

export async function getBasicPermissions() {
  const { data } = await http.get<ApiResponse<BasicPermissionItem[]>>('/basicgroups/basicpermissions')
  return data.datas ?? []
}

export async function createBasicGroup(form: BasicGroupFormData) {
  const { data } = await http.post<ApiResponse<number>>('/basicgroups', form)
  return data
}

export async function updateBasicGroup(id: number, form: BasicGroupFormData) {
  const { data } = await http.put<ApiResponse>(`/basicgroups/${id}`, form)
  return data
}

export async function deleteBasicGroup(id: number) {
  const { data } = await http.delete<ApiResponse>(`/basicgroups/${id}`)
  return data
}
