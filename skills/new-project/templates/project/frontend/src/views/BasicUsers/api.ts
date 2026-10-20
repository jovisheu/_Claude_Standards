import { del, get, post, put } from '@/api/http'
import type { PagedQuery } from '@/types/api'
import type { BasicSyslogHistoryResult } from '@/types/basicSyslog'
import type { BasicGroupOption, BasicUserDetail, BasicUserFormData, BasicUserListItem } from './types'

export function getBasicUsers(query: PagedQuery) {
  return get<BasicUserListItem[]>('/basicusers', query)
}

export function getBasicUser(id: number) {
  return get<BasicUserDetail>(`/basicusers/${id}`)
}

export function getBasicGroupOptions() {
  return get<BasicGroupOption[]>('/basicusers/basicgroup-options')
}

export function createBasicUser(data: BasicUserFormData) {
  return post<number>('/basicusers', data)
}

export function updateBasicUser(id: number, data: BasicUserFormData) {
  const { email, displayName, password, isActive, basicGroupIds } = data
  return put<null>(`/basicusers/${id}`, { email, displayName, password: password || null, isActive, basicGroupIds })
}

export function deleteBasicUser(id: number) {
  return del<null>(`/basicusers/${id}`)
}

export function getBasicUserBasicSyslogs(id: number) {
  return get<BasicSyslogHistoryResult>(`/basicusers/${id}/basicsyslogs`)
}
