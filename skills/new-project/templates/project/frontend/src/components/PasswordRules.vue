<script setup lang="ts">
import { onMounted } from 'vue'
import { useAuthOptions } from '@/composables/useAuthOptions'

const { options, load } = useAuthOptions()

onMounted(async () => {
  try {
    await load()
  } catch {
    // 錯誤已由 interceptor 處理
  }
})
</script>

<template>
  <ul v-if="options" class="list-disc space-y-0.5 pl-5 text-xs text-gray-500">
    <li>至少 {{ options.passwordMinLength }} 個半形字元（英文、數字、符號，不含空白）</li>
    <li>大寫字母、小寫字母、數字、特殊符號至少包含 {{ options.passwordRequiredCategoryCount }} 種</li>
    <li>不得與前 {{ options.passwordHistoryCount }} 次使用的密碼相同</li>
    <li>每 {{ options.passwordExpireDays }} 天須變更一次</li>
  </ul>
</template>
