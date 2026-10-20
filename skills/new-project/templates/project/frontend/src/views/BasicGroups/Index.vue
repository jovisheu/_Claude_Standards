<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import AppPagination from '@/components/AppPagination.vue'
import BasicSyslogModal from '@/components/BasicSyslogModal.vue'
import { isHandled } from '@/api/http'
import { useAuthStore } from '@/stores/auth'
import { useMessageStore } from '@/stores/message'
import { formatModifiedAt } from '@/utils/format'
import BasicGroupFormModal from './components/BasicGroupFormModal.vue'
import {
  createBasicGroup,
  deleteBasicGroup,
  getBasicGroup,
  getBasicGroupBasicSyslogs,
  getBasicGroups,
  getBasicPermissionOptions,
  updateBasicGroup,
} from './api'
import type { BasicGroupDetail, BasicGroupFormData, BasicGroupListItem, BasicPermissionItem } from './types'

const auth = useAuthStore()
const message = useMessageStore()

const canCreate = computed(() => auth.hasBasicPermission('basicgroup.create'))
const canUpdate = computed(() => auth.hasBasicPermission('basicgroup.update'))
const canDelete = computed(() => auth.hasBasicPermission('basicgroup.delete'))
// 沒有編輯、刪除權限時不顯示操作欄（歷程需要編輯權限）
const hasActions = computed(() => canUpdate.value || canDelete.value)
const columnCount = computed(() => (hasActions.value ? 6 : 5))

const query = reactive({ page: 1, pageSize: 10, keyword: '' })
const items = ref<BasicGroupListItem[]>([])
const totalCount = ref(0)
const totalPages = ref(0)
const loading = ref(false)

const formOpen = ref(false)
const editingItem = ref<BasicGroupDetail | null>(null)
const basicPermissionOptions = ref<BasicPermissionItem[]>([])
const submitting = ref(false)

const syslogOpen = ref(false)
const syslogTarget = ref<BasicGroupListItem | null>(null)

async function load() {
  loading.value = true
  try {
    const response = await getBasicGroups({ page: query.page, pageSize: query.pageSize, keyword: query.keyword || undefined })
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

async function openForm(item: BasicGroupListItem | null) {
  try {
    const [options, detail] = await Promise.all([getBasicPermissionOptions(), item ? getBasicGroup(item.id) : Promise.resolve(null)])
    basicPermissionOptions.value = options.datas ?? []
    editingItem.value = detail?.datas ?? null
    formOpen.value = true
  } catch (error) {
    if (!isHandled(error)) message.error('操作失敗，請再試一次')
  }
}

async function save(data: BasicGroupFormData) {
  submitting.value = true
  try {
    const response = editingItem.value ? await updateBasicGroup(editingItem.value.id, data) : await createBasicGroup(data)
    formOpen.value = false
    await message.success(response.message || '儲存成功')
    await load()
    // 自己所屬群組的權限可能變更，重新取得權限
    await auth.refresh()
  } catch (error) {
    if (!isHandled(error)) message.error('操作失敗，請再試一次')
  } finally {
    submitting.value = false
  }
}

async function remove(item: BasicGroupListItem) {
  // 有成員的群組不可刪除（後端檢查），先提示成員數
  const memberNote = item.basicUserCount > 0 ? `\n此群組目前有 ${item.basicUserCount} 位成員，須先移出成員才能刪除。` : ''
  if (!(await message.confirm(`確定要刪除群組「${item.name}」嗎？${memberNote}`))) return
  try {
    const response = await deleteBasicGroup(item.id)
    await message.success(response.message || '刪除成功')
    if (items.value.length === 1 && query.page > 1) query.page--
    await load()
  } catch (error) {
    if (!isHandled(error)) message.error('操作失敗，請再試一次')
  }
}

function openSyslog(item: BasicGroupListItem) {
  syslogTarget.value = item
  syslogOpen.value = true
}

onMounted(load)
</script>

<template>
  <div class="space-y-4">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <h1 class="text-xl font-bold text-gray-900">群組與權限管理</h1>
      <button v-if="canCreate" type="button" class="btn-primary" @click="openForm(null)">新增群組</button>
    </div>

    <form class="card flex flex-wrap items-end gap-3 p-4" @submit.prevent="search">
      <div class="w-full sm:w-72">
        <label for="basic-group-keyword" class="form-label">關鍵字</label>
        <input id="basic-group-keyword" v-model.trim="query.keyword" class="form-input" maxlength="50" placeholder="群組名稱或說明" />
      </div>
      <button type="submit" class="btn-primary" :disabled="loading">查詢</button>
    </form>

    <div class="card overflow-hidden">
      <div class="overflow-x-auto">
        <table class="data-table">
          <thead>
            <tr>
              <th>群組名稱</th>
              <th>說明</th>
              <th>成員數</th>
              <th>權限數</th>
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
            <tr v-for="item in loading ? [] : items" :key="item.id">
              <td>
                {{ item.name }}
                <span v-if="item.isProtected" class="ml-1 rounded bg-amber-100 px-1.5 py-0.5 text-xs text-amber-800">受保護</span>
              </td>
              <td>{{ item.description || '-' }}</td>
              <td>{{ item.basicUserCount }}</td>
              <td>{{ item.basicPermissionCount }}</td>
              <td class="whitespace-nowrap">{{ formatModifiedAt(item) }}</td>
              <td v-if="hasActions">
                <div class="flex gap-1">
                  <button v-if="canUpdate && item.isManageable" type="button" class="btn-secondary btn-sm" @click="openForm(item)">編輯</button>
                  <button v-if="canUpdate" type="button" class="btn-secondary btn-sm" @click="openSyslog(item)">歷程</button>
                  <button v-if="canDelete && item.isManageable && !item.isProtected" type="button" class="btn-danger btn-sm" @click="remove(item)">
                    刪除
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <AppPagination :page="query.page" :total-pages="totalPages" :total-count="totalCount" @change="changePage" />
    </div>

    <BasicGroupFormModal
      v-model="formOpen"
      :item="editingItem"
      :basic-permission-options="basicPermissionOptions"
      :submitting="submitting"
      @submit="save"
    />
    <BasicSyslogModal
      v-if="syslogTarget"
      v-model="syslogOpen"
      :title="`異動歷程：${syslogTarget.name}`"
      :load="() => getBasicGroupBasicSyslogs(syslogTarget!.id)"
    />
  </div>
</template>
