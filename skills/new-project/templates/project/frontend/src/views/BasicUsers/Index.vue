<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { isHandled } from '@/api/http'
import AppPagination from '@/components/AppPagination.vue'
import { useAuthStore } from '@/stores/auth'
import { useMessageStore } from '@/stores/message'
import BasicUserFormModal from './components/BasicUserFormModal.vue'
import { createBasicUser, deleteBasicUser, getBasicGroupOptions, getBasicUser, getBasicUsers, updateBasicUser } from './api'
import type { BasicGroupOption, BasicUserDetail, BasicUserFormData, BasicUserListItem } from './types'

const auth = useAuthStore()
const message = useMessageStore()

const canCreate = computed(() => auth.hasBasicPermission('basicuser.create'))
const canUpdate = computed(() => auth.hasBasicPermission('basicuser.update'))
const canDelete = computed(() => auth.hasBasicPermission('basicuser.delete'))

const query = reactive({ keyword: '', page: 1, pageSize: 10 })
const items = ref<BasicUserListItem[]>([])
const totalCount = ref(0)
const totalPages = ref(0)
const loading = ref(false)

const modalOpen = ref(false)
const editingItem = ref<BasicUserDetail | null>(null)
const basicGroupOptions = ref<BasicGroupOption[]>([])
const submitting = ref(false)

async function load() {
  loading.value = true
  try {
    const result = await getBasicUsers({ keyword: query.keyword.trim() || undefined, page: query.page, pageSize: query.pageSize })
    items.value = result.datas ?? []
    totalCount.value = result.totalCount
    totalPages.value = result.totalPages
  } catch (error) {
    if (!isHandled(error)) await message.error('操作失敗，請再試一次')
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

async function openCreate() {
  try {
    basicGroupOptions.value = await getBasicGroupOptions()
    editingItem.value = null
    modalOpen.value = true
  } catch (error) {
    if (!isHandled(error)) await message.error('操作失敗，請再試一次')
  }
}

async function openEdit(id: number) {
  try {
    const [detail, options] = await Promise.all([getBasicUser(id), getBasicGroupOptions()])
    basicGroupOptions.value = options
    editingItem.value = detail
    modalOpen.value = true
  } catch (error) {
    if (!isHandled(error)) await message.error('操作失敗，請再試一次')
  }
}

async function save(form: BasicUserFormData) {
  submitting.value = true
  try {
    const result = editingItem.value ? await updateBasicUser(editingItem.value.id, form) : await createBasicUser(form)
    modalOpen.value = false
    await message.success(result.message || '儲存成功')
    await load()
  } catch (error) {
    if (!isHandled(error)) await message.error('操作失敗，請再試一次')
  } finally {
    submitting.value = false
  }
}

async function remove(item: BasicUserListItem) {
  if (!(await message.confirm(`確定要刪除使用者「${item.displayName}」嗎？`))) return
  try {
    const result = await deleteBasicUser(item.id)
    await message.success(result.message || '刪除成功')
    if (items.value.length === 1 && query.page > 1) query.page--
    await load()
  } catch (error) {
    if (!isHandled(error)) await message.error('操作失敗，請再試一次')
  }
}

function formatDate(value: string) {
  return new Date(value).toLocaleString('zh-TW', { hour12: false })
}

onMounted(load)
</script>

<template>
  <div class="space-y-4">
    <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
      <h2 class="text-xl font-semibold text-gray-900">使用者管理</h2>
      <button v-if="canCreate" type="button" class="btn-primary" @click="openCreate">＋ 新增使用者</button>
    </div>

    <form class="card flex flex-col gap-2 p-4 sm:flex-row" @submit.prevent="search">
      <input v-model="query.keyword" class="form-input sm:max-w-xs" maxlength="50" placeholder="帳號、顯示名稱或 Email" />
      <button type="submit" class="btn-secondary">查詢</button>
    </form>

    <div class="card overflow-hidden">
      <div class="overflow-x-auto">
        <table class="table-base">
          <thead>
            <tr>
              <th>帳號</th>
              <th>顯示名稱</th>
              <th>Email</th>
              <th>群組</th>
              <th>狀態</th>
              <th>建立時間</th>
              <th v-if="canUpdate || canDelete" class="text-right">操作</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr v-if="loading">
              <td colspan="7" class="py-8 text-center text-gray-400">載入中…</td>
            </tr>
            <tr v-else-if="items.length === 0">
              <td colspan="7" class="py-8 text-center text-gray-400">查無資料</td>
            </tr>
            <tr v-for="item in items" v-else :key="item.id" class="hover:bg-gray-50">
              <td class="font-medium">{{ item.username }}</td>
              <td>{{ item.displayName }}</td>
              <td>{{ item.email }}</td>
              <td>
                <span
                  v-for="name in item.basicGroupNames"
                  :key="name"
                  class="mr-1 mb-1 inline-block rounded bg-primary-50 px-2 py-0.5 text-xs text-primary-700"
                >
                  {{ name }}
                </span>
              </td>
              <td>
                <span
                  class="rounded px-2 py-0.5 text-xs"
                  :class="item.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-200 text-gray-600'"
                >
                  {{ item.isActive ? '啟用' : '停用' }}
                </span>
              </td>
              <td class="whitespace-nowrap">{{ formatDate(item.createdAt) }}</td>
              <td v-if="canUpdate || canDelete" class="text-right whitespace-nowrap">
                <button v-if="canUpdate" type="button" class="btn-link mr-3" @click="openEdit(item.id)">編輯</button>
                <button
                  v-if="canDelete && item.id !== auth.basicUser?.id"
                  type="button"
                  class="text-sm text-red-600 hover:underline"
                  @click="remove(item)"
                >
                  刪除
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <AppPagination :page="query.page" :total-pages="totalPages" :total-count="totalCount" @change="changePage" />
    </div>

    <BasicUserFormModal
      v-model="modalOpen"
      :item="editingItem"
      :basic-group-options="basicGroupOptions"
      :is-self="editingItem?.id === auth.basicUser?.id"
      :submitting="submitting"
      @submit="save"
    />
  </div>
</template>
