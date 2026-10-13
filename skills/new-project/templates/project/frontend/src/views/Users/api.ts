import http from '@/api/http'
import type { ApiResponse, PagedQuery } from '@/types/api'
import type { GroupOption, UserDetail, UserFormData, UserListItem } from './types'

export async function getUsers(query: PagedQuery) {
  const { data } = await http.get<ApiResponse<UserListItem[]>>('/users', { params: query })
  return data
}

export async function getUser(id: number) {
  const { data } = await http.get<ApiResponse<UserDetail>>(`/users/${id}`)
  return data.datas!
}

export async function getGroupOptions() {
  const { data } = await http.get<ApiResponse<GroupOption[]>>('/users/group-options')
  return data.datas ?? []
}

export async function createUser(form: UserFormData) {
  const { data } = await http.post<ApiResponse<number>>('/users', form)
  return data
}

export async function updateUser(id: number, form: UserFormData) {
  const { email, displayName, password, isActive, groupIds } = form
  const { data } = await http.put<ApiResponse>(`/users/${id}`, {
    email,
    displayName,
    password: password || null,
    isActive,
    groupIds,
  })
  return data
}

export async function deleteUser(id: number) {
  const { data } = await http.delete<ApiResponse>(`/users/${id}`)
  return data
}
