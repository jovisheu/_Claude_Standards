<script setup lang="ts">
import { computed } from 'vue'
import { useMessageStore } from '@/stores/message'

const message = useMessageStore()

const defaultTitles = { success: '操作成功', error: '操作失敗', confirm: '請確認', info: '訊息' } as const
const iconClasses = {
  success: 'bg-primary-100 text-primary-700',
  error: 'bg-red-100 text-red-700',
  confirm: 'bg-amber-100 text-amber-700',
  info: 'bg-blue-100 text-blue-700',
} as const
const icons = { success: '✓', error: '!', confirm: '?', info: 'i' } as const

const title = computed(() => (message.current ? message.current.title || defaultTitles[message.current.type] : ''))
</script>

<template>
  <Teleport to="body">
    <!-- z-index 高於所有 Modal，訊息一律顯示在最上層 -->
    <div v-if="message.current" class="fixed inset-0 z-[100] flex items-center justify-center bg-black/40 p-4">
      <div class="card w-full max-w-sm p-5" role="alertdialog" aria-modal="true">
        <div class="flex items-start gap-3">
          <span class="flex h-8 w-8 shrink-0 items-center justify-center rounded-full font-bold" :class="iconClasses[message.current.type]">
            {{ icons[message.current.type] }}
          </span>
          <div class="min-w-0 flex-1">
            <h2 class="mb-1 font-semibold text-gray-900">{{ title }}</h2>
            <p class="text-sm break-words whitespace-pre-line text-gray-700">{{ message.current.text }}</p>
          </div>
        </div>
        <div class="mt-5 flex justify-end gap-2">
          <button v-if="message.current.type === 'confirm'" type="button" class="btn-secondary" @click="message.close(false)">取消</button>
          <button type="button" :class="message.current.type === 'error' ? 'btn-danger' : 'btn-primary'" @click="message.close(true)">確定</button>
        </div>
      </div>
    </div>
  </Teleport>
</template>
