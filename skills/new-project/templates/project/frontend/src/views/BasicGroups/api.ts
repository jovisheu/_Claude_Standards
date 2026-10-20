import { del, get, post, put } from '@/api/http'
import type { PagedQuery } from '@/types/api'
import type { BasicSyslogHistoryResult } from '@/types/basicSyslog'
import type { BasicGroupDetail, BasicGroupFormData, BasicGroupListItem, BasicPermissionItem } from './types'

export function getBasicGroups(query: PagedQuery) {
  return get<BasicGroupListItem[]>('/basicgroups', query)
}

export function getBasicGroup(id: number) {
  return get<BasicGroupDetail>(`/basicgroups/${id}`)
}

export function getBasicPermissionOptions() {
  return get<BasicPermissionItem[]>('/basicgroups/basicpermission-options')
}

export function createBasicGroup(data: BasicGroupFormData) {
  return post<number>('/basicgroups', data)
}

export function updateBasicGroup(id: number, data: BasicGroupFormData) {
  return put<null>(`/basicgroups/${id}`, data)
}

export function deleteBasicGroup(id: number) {
  return del<null>(`/basicgroups/${id}`)
}

export function getBasicGroupBasicSyslogs(id: number) {
  return get<BasicSyslogHistoryResult>(`/basicgroups/${id}/basicsyslogs`)
}
