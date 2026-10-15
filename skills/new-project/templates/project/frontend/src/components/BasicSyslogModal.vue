<script setup lang="ts">
import BaseModal from '@/components/BaseModal.vue'
import type { BasicSyslogAction, BasicSyslogHistoryResult } from '@/types/basicSyslog'
import { formatDateTime } from '@/utils/format'

defineProps<{
  title: string
  /** 查詢結果；載入中或失敗時為 null */
  result: BasicSyslogHistoryResult | null
  loading: boolean
}>()

const open = defineModel<boolean>({ required: true })

const actionLabels: Record<BasicSyslogAction, string> = { C: '新增', U: '修改', D: '刪除' }
const actionClasses: Record<BasicSyslogAction, string> = {
  C: 'bg-green-100 text-green-700',
  U: 'bg-primary-50 text-primary-700',
  D: 'bg-red-100 text-red-700',
}

function display(value: string | null) {
  return value === null || value === '' ? '（空白）' : value
}
</script>

<template>
  <BaseModal v-model="open" :title="title" width="max-w-3xl">
    <div class="max-h-[65vh] space-y-3 overflow-y-auto">
      <p v-if="loading || !result" class="py-8 text-center text-gray-400">載入中…</p>
      <template v-else>
        <p v-if="result.items.length === 0" class="py-8 text-center text-gray-400">查無異動歷程</p>
        <div v-for="item in result.items" :key="item.id" class="rounded border border-gray-200">
          <div class="flex flex-wrap items-center gap-2 border-b border-gray-100 bg-gray-50 px-3 py-2 text-sm">
            <span class="rounded px-2 py-0.5 text-xs" :class="actionClasses[item.action]">{{ actionLabels[item.action] }}</span>
            <span class="text-gray-700">{{ formatDateTime(item.createdAt) }}</span>
            <span class="text-gray-500">操作者：{{ item.username ?? '（未登入）' }}</span>
          </div>
          <div class="divide-y divide-gray-100">
            <div v-for="(row, index) in item.rows" :key="index" class="px-3 py-2 text-sm">
              <div class="mb-1 flex items-center gap-2 text-xs text-gray-500">
                <span>{{ row.tableDescription }}</span>
                <span class="rounded px-1.5 py-0.5" :class="actionClasses[row.action]">{{ actionLabels[row.action] }}</span>
              </div>
              <dl class="grid grid-cols-1 gap-x-4 gap-y-1 sm:grid-cols-[minmax(8rem,auto)_1fr]">
                <template v-for="field in row.fields" :key="field.name">
                  <dt class="text-gray-500">{{ field.description }}</dt>
                  <dd class="break-all text-gray-900">
                    <template v-if="row.action === 'U'">
                      <span class="text-gray-400 line-through">{{ display(field.before) }}</span>
                      <span class="mx-1 text-gray-400">→</span>
                      <span>{{ display(field.after) }}</span>
                    </template>
                    <span v-else-if="row.action === 'D'" class="text-gray-400 line-through">{{ display(field.before) }}</span>
                    <span v-else>{{ display(field.after) }}</span>
                  </dd>
                </template>
              </dl>
            </div>
          </div>
        </div>
        <p class="text-xs text-gray-400">只顯示保留天數 {{ result.retentionDays }} 天內的紀錄，較舊的紀錄已轉存至伺服器檔案。</p>
      </template>
    </div>
  </BaseModal>
</template>
