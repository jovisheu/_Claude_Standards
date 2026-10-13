<script setup lang="ts">
import { useAuthOptions } from '@/composables/useAuthOptions'

// 規則數值由後端提供，不在前端寫死
const { options } = useAuthOptions()
</script>

<template>
  <ul v-if="options" class="list-disc space-y-0.5 pl-5 text-xs text-gray-500">
    <li>至少 {{ options.passwordMinLength }} 個半形字元，不可包含空白</li>
    <li>大寫字母、小寫字母、數字、特殊符號至少包含 {{ options.passwordRequiredCategories }} 種</li>
    <li>不得與前 {{ options.passwordHistoryCount }} 次使用的密碼相同</li>
    <li>每 {{ options.passwordExpireDays }} 天須變更密碼</li>
  </ul>
</template>
