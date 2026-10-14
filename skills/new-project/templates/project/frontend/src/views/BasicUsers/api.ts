import http from '@/api/http'
import type { ApiResponse, PagedQuery } from '@/types/api'
import type { BasicGroupOption, BasicUserDetail, BasicUserFormData, BasicUserListItem } from './types'

export async function getBasicUsers(query: PagedQuery) {
  const { data } = await http.get<ApiResponse<BasicUserListItem[]>>('/basicusers', { params: query })
  return data
}

export async function getBasicUser(id: number) {
  const { data } = await http.get<ApiResponse<BasicUserDetail>>(`/basicusers/${id}`)
  return data.datas!
}

export async function getBasicGroupOptions() {
  const { data } = await http.get<ApiResponse<BasicGroupOption[]>>('/basicusers/basicgroup-options')
  return data.datas ?? []
}

export async function createBasicUser(form: BasicUserFormData) {
  const { data } = await http.post<ApiResponse<number>>('/basicusers', form)
  return data
}

export async function updateBasicUser(id: number, form: BasicUserFormData) {
  const { email, displayName, password, isActive, basicGroupIds } = form
  const { data } = await http.put<ApiResponse>(`/basicusers/${id}`, {
    email,
    displayName,
    password: password || null,
    isActive,
    basicGroupIds,
  })
  return data
}

export async function deleteBasicUser(id: number) {
  const { data } = await http.delete<ApiResponse>(`/basicusers/${id}`)
  return data
}
