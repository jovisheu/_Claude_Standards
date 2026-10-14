<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { isHandled } from '@/api/http'
import AppPagination from '@/components/AppPagination.vue'
import { useAuthStore } from '@/stores/auth'
import { useMessageStore } from '@/stores/message'
import BasicGroupFormModal from './components/BasicGroupFormModal.vue'
import { createBasicGroup, deleteBasicGroup, getBasicGroup, getBasicGroups, getBasicPermissions, updateBasicGroup } from './api'
import type { BasicGroupDetail, BasicGroupFormData, BasicGroupListItem, BasicPermissionItem } from './types'

const auth = useAuthStore()
const message = useMessageStore()

const canCreate = computed(() => auth.hasBasicPermission('basicgroup.create'))
const canUpdate = computed(() => auth.hasBasicPermission('basicgroup.update'))
const canDelete = computed(() => auth.hasBasicPermission('basicgroup.delete'))

const query = reactive({ keyword: '', page: 1, pageSize: 10 })
const items = ref<BasicGroupListItem[]>([])
const totalCount = ref(0)
const totalPages = ref(0)
const loading = ref(false)

const modalOpen = ref(false)
const editingItem = ref<BasicGroupDetail | null>(null)
const basicPermissions = ref<BasicPermissionItem[]>([])
const submitting = ref(false)

async function load() {
  loading.value = true
  try {
    const result = await getBasicGroups({ keyword: query.keyword.trim() || undefined, page: query.page, pageSize: query.pageSize })
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
    basicPermissions.value = await getBasicPermissions()
    editingItem.value = null
    modalOpen.value = true
  } catch (error) {
    if (!isHandled(error)) await message.error('操作失敗，請再試一次')
  }
}

async function openEdit(id: number) {
  try {
    const [detail, list] = await Promise.all([getBasicGroup(id), getBasicPermissions()])
    basicPermissions.value = list
    editingItem.value = detail
    modalOpen.value = true
  } catch (error) {
    if (!isHandled(error)) await message.error('操作失敗，請再試一次')
  }
}

async function save(form: BasicGroupFormData) {
  submitting.value = true
  try {
    const result = editingItem.value ? await updateBasicGroup(editingItem.value.id, form) : await createBasicGroup(form)
    modalOpen.value = false
    await message.success(result.message || '儲存成功')
    await load()
  } catch (error) {
    if (!isHandled(error)) await message.error('操作失敗，請再試一次')
  } finally {
    submitting.value = false
  }
}

async function remove(item: BasicGroupListItem) {
  const extra = item.basicUserCount > 0 ? `\n此群組目前有 ${item.basicUserCount} 位成員，刪除後成員將失去此群組的權限。` : ''
  if (!(await message.confirm(`確定要刪除群組「${item.name}」嗎？${extra}`))) return
  try {
    const result = await deleteBasicGroup(item.id)
    await message.success(result.message || '刪除成功')
    if (items.value.length === 1 && query.page > 1) query.page--
    await load()
  } catch (error) {
    if (!isHandled(error)) await message.error('操作失敗，請再試一次')
  }
}

onMounted(load)
</script>

<template>
  <div class="space-y-4">
    <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
      <h2 class="text-xl font-semibold text-gray-900">群組管理</h2>
      <button v-if="canCreate" type="button" class="btn-primary" @click="openCreate">＋ 新增群組</button>
    </div>

    <form class="card flex flex-col gap-2 p-4 sm:flex-row" @submit.prevent="search">
      <input v-model="query.keyword" class="form-input sm:max-w-xs" maxlength="50" placeholder="群組名稱或說明" />
      <button type="submit" class="btn-secondary">查詢</button>
    </form>

    <div class="card overflow-hidden">
      <div class="overflow-x-auto">
        <table class="table-base">
          <thead>
            <tr>
              <th>群組名稱</th>
              <th>說明</th>
              <th class="text-right">成員數</th>
              <th class="text-right">權限數</th>
              <th v-if="canUpdate || canDelete" class="text-right">操作</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr v-if="loading">
              <td colspan="5" class="py-8 text-center text-gray-400">載入中…</td>
            </tr>
            <tr v-else-if="items.length === 0">
              <td colspan="5" class="py-8 text-center text-gray-400">查無資料</td>
            </tr>
            <tr v-for="item in items" v-else :key="item.id" class="hover:bg-gray-50">
              <td class="font-medium whitespace-nowrap">
                {{ item.name }}
                <span v-if="item.isProtected" class="ml-1 rounded bg-amber-100 px-1.5 py-0.5 text-xs text-amber-700">受保護</span>
              </td>
              <td>{{ item.description }}</td>
              <td class="text-right">{{ item.basicUserCount }}</td>
              <td class="text-right">{{ item.basicPermissionCount }}</td>
              <td v-if="canUpdate || canDelete" class="text-right whitespace-nowrap">
                <button v-if="canUpdate" type="button" class="btn-link mr-3" @click="openEdit(item.id)">編輯</button>
                <button
                  v-if="canDelete && !item.isProtected"
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

    <BasicGroupFormModal v-model="modalOpen" :item="editingItem" :basic-permissions="basicPermissions" :submitting="submitting" @submit="save" />
  </div>
</template>
