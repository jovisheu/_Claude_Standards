<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  page: number
  totalPages: number
  totalCount: number
}>()

const emit = defineEmits<{
  change: [page: number]
}>()

/** 顯示目前頁前後各 2 頁 */
const pages = computed(() => {
  const start = Math.max(1, props.page - 2)
  const end = Math.min(props.totalPages, props.page + 2)
  return Array.from({ length: Math.max(0, end - start + 1) }, (_, i) => start + i)
})

function go(page: number) {
  if (page >= 1 && page <= props.totalPages && page !== props.page) emit('change', page)
}
</script>

<template>
  <div class="flex flex-col items-center justify-between gap-3 px-4 py-3 text-sm text-gray-600 sm:flex-row">
    <span>共 {{ totalCount }} 筆，第 {{ totalPages === 0 ? 0 : page }} / {{ totalPages }} 頁</span>
    <div v-if="totalPages > 1" class="flex items-center gap-1">
      <button type="button" class="rounded px-2 py-1 hover:bg-gray-100 disabled:opacity-40" :disabled="page <= 1" @click="go(page - 1)">
        上一頁
      </button>
      <button
        v-for="p in pages"
        :key="p"
        type="button"
        class="min-w-8 rounded px-2 py-1"
        :class="p === page ? 'bg-primary-600 text-white' : 'hover:bg-gray-100'"
        @click="go(p)"
      >
        {{ p }}
      </button>
      <button
        type="button"
        class="rounded px-2 py-1 hover:bg-gray-100 disabled:opacity-40"
        :disabled="page >= totalPages"
        @click="go(page + 1)"
      >
        下一頁
      </button>
    </div>
  </div>
</template>
