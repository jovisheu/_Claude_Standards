<script setup lang="ts">
const props = defineProps<{ page: number; totalPages: number; totalCount: number }>()
const emit = defineEmits<{ change: [page: number] }>()

function go(page: number) {
  if (page < 1 || page > props.totalPages || page === props.page) return
  emit('change', page)
}
</script>

<template>
  <div class="flex flex-wrap items-center justify-between gap-2 px-4 py-3 text-sm text-gray-600">
    <span>共 {{ totalCount }} 筆，第 {{ totalPages === 0 ? 0 : page }} / {{ totalPages }} 頁</span>
    <div class="flex gap-1">
      <button type="button" class="btn-secondary btn-sm" :disabled="page <= 1" @click="go(1)">第一頁</button>
      <button type="button" class="btn-secondary btn-sm" :disabled="page <= 1" @click="go(page - 1)">上一頁</button>
      <button type="button" class="btn-secondary btn-sm" :disabled="page >= totalPages" @click="go(page + 1)">下一頁</button>
      <button type="button" class="btn-secondary btn-sm" :disabled="page >= totalPages" @click="go(totalPages)">最末頁</button>
    </div>
  </div>
</template>
