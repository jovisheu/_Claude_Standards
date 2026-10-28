<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import AppPagination from '@/components/AppPagination.vue'
import BasicSyslogModal from '@/components/BasicSyslogModal.vue'
import { isHandled } from '@/api/http'
import { useAuthStore } from '@/stores/auth'
import { useMessageStore } from '@/stores/message'
import { formatModifiedAt, formatRowNumber } from '@/utils/format'
import BasicUserFormModal from './components/BasicUserFormModal.vue'
import {
  createBasicUser,
  deleteBasicUser,
  getBasicGroupOptions,
  getBasicUser,
  getBasicUserBasicSyslogs,
  getBasicUsers,
  updateBasicUser,
} from './api'
import type { BasicGroupOption, BasicUserDetail, BasicUserFormData, BasicUserListItem } from './types'

const auth = useAuthStore()
const message = useMessageStore()

const canCreate = computed(() => auth.hasBasicPermission('basicuser.create'))
const canUpdate = computed(() => auth.hasBasicPermission('basicuser.update'))
const canDelete = computed(() => auth.hasBasicPermission('basicuser.delete'))
// 沒有編輯、刪除權限時不顯示操作欄（歷程需要編輯權限）
const hasActions = computed(() => canUpdate.value || canDelete.value)
const columnCount = computed(() => (hasActions.value ? 8 : 7))

const query = reactive({ page: 1, pageSize: 10, keyword: '' })
const items = ref<BasicUserListItem[]>([])
const totalCount = ref(0)
const totalPages = ref(0)
const loading = ref(false)

const formOpen = ref(false)
const editingItem = ref<BasicUserDetail | null>(null)
const basicGroupOptions = ref<BasicGroupOption[]>([])
const submitting = ref(false)

const syslogOpen = ref(false)
const syslogTarget = ref<BasicUserListItem | null>(null)

async function load() {
  loading.value = true
  try {
    const response = await getBasicUsers({ page: query.page, pageSize: query.pageSize, keyword: query.keyword || undefined })
    items.value = response.datas ?? []
    totalCount.value = response.totalCount
    totalPages.value = response.totalPages
  } catch (error) {
    if (!isHandled(error)) message.error('操作失敗，請再試一次')
  } finally {
    loading.value = false
  }
}

function search() {
  query.page = 1
  load()
}

function changePage(page: number) {
  query.page = page
  load()
}

async function openForm(item: BasicUserListItem | null) {
  try {
    const [options, detail] = await Promise.all([getBasicGroupOptions(), item ? getBasicUser(item.id) : Promise.resolve(null)])
    basicGroupOptions.value = options.datas ?? []
    editingItem.value = detail?.datas ?? null
    formOpen.value = true
  } catch (error) {
    if (!isHandled(error)) message.error('操作失敗，請再試一次')
  }
}

async function save(data: BasicUserFormData) {
  submitting.value = true
  try {
    const response = editingItem.value ? await updateBasicUser(editingItem.value.id, data) : await createBasicUser(data)
    formOpen.value = false
    await message.success(response.message || '儲存成功')
    await load()
  } catch (error) {
    if (!isHandled(error)) message.error('操作失敗，請再試一次')
  } finally {
    submitting.value = false
  }
}

async function remove(item: BasicUserListItem) {
  if (!(await message.confirm(`確定要刪除使用者「${item.displayName}（${item.username}）」嗎？`))) return
  try {
    const response = await deleteBasicUser(item.id)
    await message.success(response.message || '刪除成功')
    if (items.value.length === 1 && query.page > 1) query.page--
    await load()
  } catch (error) {
    if (!isHandled(error)) message.error('操作失敗，請再試一次')
  }
}

function openSyslog(item: BasicUserListItem) {
  syslogTarget.value = item
  syslogOpen.value = true
}

onMounted(load)
</script>

<template>
  <div class="space-y-4">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <h1 class="text-xl font-bold text-gray-900">使用者管理</h1>
      <button v-if="canCreate" type="button" class="btn-primary" @click="openForm(null)">新增使用者</button>
    </div>

    <form class="card flex flex-wrap items-end gap-3 p-4" @submit.prevent="search">
      <div class="w-full sm:w-72">
        <label for="basic-user-keyword" class="form-label">關鍵字</label>
        <input id="basic-user-keyword" v-model.trim="query.keyword" class="form-input" maxlength="50" placeholder="帳號、顯示名稱或 Email" />
      </div>
      <button type="submit" class="btn-primary" :disabled="loading">查詢</button>
    </form>

    <div class="card overflow-hidden">
      <div class="data-table-wrap">
        <table class="data-table">
          <thead>
            <tr>
              <th class="row-number">項次</th>
              <th>帳號</th>
              <th>顯示名稱</th>
              <th>Email</th>
              <th>群組</th>
              <th>狀態</th>
              <th>異動時間</th>
              <th v-if="hasActions" class="w-1">操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="loading">
              <td :colspan="columnCount" class="py-8 text-center text-gray-500">載入中…</td>
            </tr>
            <tr v-else-if="items.length === 0">
              <td :colspan="columnCount" class="py-8 text-center text-gray-500">查無資料</td>
            </tr>
            <tr v-for="(item, index) in loading ? [] : items" :key="item.id">
              <td class="row-number">{{ formatRowNumber(query.page, query.pageSize, index) }}</td>
              <td>{{ item.username }}</td>
              <td>{{ item.displayName }}</td>
              <td class="break-all">{{ item.email }}</td>
              <td>{{ item.basicGroupNames.join('、') || '-' }}</td>
              <td>
                <span
                  class="rounded px-2 py-0.5 text-xs font-medium"
                  :class="item.isActive ? 'bg-primary-100 text-primary-800' : 'bg-gray-200 text-gray-600'"
                >
                  {{ item.isActive ? '啟用' : '停用' }}
                </span>
              </td>
              <td class="whitespace-nowrap">{{ formatModifiedAt(item) }}</td>
              <td v-if="hasActions">
                <div class="flex gap-1">
                  <button v-if="canUpdate && item.isManageable" type="button" class="btn-secondary btn-sm" @click="openForm(item)">編輯</button>
                  <button v-if="canUpdate" type="button" class="btn-secondary btn-sm" @click="openSyslog(item)">歷程</button>
                  <button v-if="canDelete && item.isManageable && !item.isSelf" type="button" class="btn-danger btn-sm" @click="remove(item)">刪除</button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <AppPagination :page="query.page" :total-pages="totalPages" :total-count="totalCount" @change="changePage" />
    </div>

    <BasicUserFormModal
      v-model="formOpen"
      :item="editingItem"
      :basic-group-options="basicGroupOptions"
      :submitting="submitting"
      @submit="save"
    />
    <BasicSyslogModal
      v-if="syslogTarget"
      v-model="syslogOpen"
      :title="`異動歷程：${syslogTarget.displayName}（${syslogTarget.username}）`"
      :load="() => getBasicUserBasicSyslogs(syslogTarget!.id)"
    />
  </div>
</template>
