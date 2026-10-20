<script setup lang="ts">
import { ref, watch } from 'vue'
import BaseModal from '@/components/BaseModal.vue'
import { isHandled } from '@/api/http'
import { useMessageStore } from '@/stores/message'
import type { ApiResponse } from '@/types/api'
import type { BasicSyslogAction, BasicSyslogHistoryResult } from '@/types/basicSyslog'
import { formatDateTime } from '@/utils/format'

const props = defineProps<{
  title: string
  load: () => Promise<ApiResponse<BasicSyslogHistoryResult>>
}>()
const open = defineModel<boolean>({ required: true })

const message = useMessageStore()
const loading = ref(false)
const result = ref<BasicSyslogHistoryResult | null>(null)

const actionLabels: Record<BasicSyslogAction, string> = { C: '新增', U: '修改', D: '刪除' }
const actionClasses: Record<BasicSyslogAction, string> = {
  C: 'bg-primary-100 text-primary-800',
  U: 'bg-blue-100 text-blue-800',
  D: 'bg-red-100 text-red-800',
}

watch(open, async (value) => {
  if (!value) return
  result.value = null
  loading.value = true
  try {
    result.value = (await props.load()).datas
  } catch (error) {
    if (!isHandled(error)) message.error('操作失敗，請再試一次')
    open.value = false
  } finally {
    loading.value = false
  }
}, { immediate: true })

function display(value: string | null): string {
  return value === null || value === '' ? '（空白）' : value
}
</script>

<template>
  <BaseModal v-model="open" :title="title" width="max-w-3xl">
    <p v-if="loading" class="py-6 text-center text-sm text-gray-500">載入中…</p>
    <template v-else-if="result">
      <p class="mb-3 text-xs text-gray-500">僅顯示最近 {{ result.retentionDays }} 天內的異動紀錄。</p>
      <p v-if="result.items.length === 0" class="py-6 text-center text-sm text-gray-500">沒有異動紀錄</p>
      <ol v-else class="space-y-4">
        <li v-for="item in result.items" :key="item.id" class="rounded-md border border-gray-200">
          <div class="flex flex-wrap items-center gap-2 border-b border-gray-100 bg-gray-50 px-3 py-2 text-sm">
            <span class="rounded px-2 py-0.5 text-xs font-medium" :class="actionClasses[item.action]">{{ actionLabels[item.action] }}</span>
            <span v-if="item.description" class="font-medium text-gray-800">{{ item.description }}</span>
            <span class="ml-auto text-gray-500">{{ formatDateTime(item.createdAt) }}　{{ item.username ?? '系統' }}</span>
          </div>
          <div v-for="(row, rowIndex) in item.rows" :key="rowIndex" class="px-3 py-2">
            <p class="mb-1 text-xs text-gray-500">{{ row.tableLabel }}（{{ actionLabels[row.action] }}）</p>
            <table class="w-full text-xs">
              <thead>
                <tr class="text-left text-gray-500">
                  <th class="w-1/4 py-1 pr-2 font-medium">欄位</th>
                  <th v-if="row.action !== 'C'" class="py-1 pr-2 font-medium">異動前</th>
                  <th v-if="row.action !== 'D'" class="py-1 font-medium">異動後</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="field in row.fields" :key="field.name" class="border-t border-gray-100 align-top">
                  <td class="py-1 pr-2 text-gray-600">{{ field.label }}</td>
                  <td v-if="row.action !== 'C'" class="py-1 pr-2 break-all text-gray-800">{{ display(field.before) }}</td>
                  <td v-if="row.action !== 'D'" class="py-1 break-all text-gray-800">{{ display(field.after) }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </li>
      </ol>
    </template>
    <template #footer>
      <button type="button" class="btn-secondary" @click="open = false">關閉</button>
    </template>
  </BaseModal>
</template>
