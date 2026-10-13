import http from '@/api/http'
import type { ApiResponse, PagedQuery } from '@/types/api'
import type { GroupDetail, GroupFormData, GroupListItem, PermissionItem } from './types'

export async function getGroups(query: PagedQuery) {
  const { data } = await http.get<ApiResponse<GroupListItem[]>>('/groups', { params: query })
  return data
}

export async function getGroup(id: number) {
  const { data } = await http.get<ApiResponse<GroupDetail>>(`/groups/${id}`)
  return data.datas!
}

export async function getPermissions() {
  const { data } = await http.get<ApiResponse<PermissionItem[]>>('/groups/permissions')
  return data.datas ?? []
}

export async function createGroup(form: GroupFormData) {
  const { data } = await http.post<ApiResponse<number>>('/groups', form)
  return data
}

export async function updateGroup(id: number, form: GroupFormData) {
  const { data } = await http.put<ApiResponse>(`/groups/${id}`, form)
  return data
}

export async function deleteGroup(id: number) {
  const { data } = await http.delete<ApiResponse>(`/groups/${id}`)
  return data
}
