<script setup lang="ts">
import { computed } from 'vue'
import { useMessageStore } from '@/stores/message'

const message = useMessageStore()

const iconClass = computed(() => {
  switch (message.current?.type) {
    case 'success':
      return 'bg-green-100 text-green-600'
    case 'error':
      return 'bg-red-100 text-red-600'
    case 'confirm':
      return 'bg-amber-100 text-amber-600'
    default:
      return 'bg-primary-100 text-primary-600'
  }
})

const iconText = computed(() => {
  switch (message.current?.type) {
    case 'success':
      return '✓'
    case 'error':
      return '!'
    case 'confirm':
      return '?'
    default:
      return 'i'
  }
})
</script>

<template>
  <div v-if="message.current" class="fixed inset-0 z-[100] flex items-center justify-center bg-black/40 p-4">
    <div class="w-full max-w-sm rounded-lg bg-white p-6 shadow-xl" role="alertdialog" aria-modal="true">
      <div class="flex items-start gap-4">
        <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-full text-lg font-bold" :class="iconClass">
          {{ iconText }}
        </div>
        <div class="min-w-0 flex-1">
          <h3 class="text-base font-semibold text-gray-900">{{ message.current.title }}</h3>
          <!-- 以 {{ }} 輸出，whitespace-pre-line 呈現多筆訊息的換行 -->
          <p class="mt-2 text-sm break-words whitespace-pre-line text-gray-600">{{ message.current.text }}</p>
        </div>
      </div>
      <div class="mt-6 flex justify-end gap-2">
        <button v-if="message.current.type === 'confirm'" type="button" class="btn-secondary" @click="message.close(false)">
          取消
        </button>
        <button type="button" class="btn-primary" @click="message.close(true)">確定</button>
      </div>
    </div>
  </div>
</template>
