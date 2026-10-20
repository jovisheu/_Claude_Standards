<script setup lang="ts">
import { computed } from 'vue'
import { buildMenu } from '@/layouts/menu'
import { adminRoutes } from '@/router/featureRoutes'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const systemName = import.meta.env.VITE_APP_TITLE

// 依權限顯示功能卡片（取自各功能 routes.ts 的 meta.menu，首頁本身除外）
const cards = computed(() =>
  buildMenu(adminRoutes).filter(
    (item) => item.routeName !== 'home' && (!item.basicPermission || auth.hasBasicPermission(item.basicPermission)),
  ),
)
</script>

<template>
  <div class="space-y-4">
    <div class="card p-6">
      <h2 class="mb-1 text-xl font-bold text-gray-900">歡迎使用 {{ systemName }}</h2>
      <p class="text-gray-600">{{ auth.basicUser?.displayName }} 您好，請選擇要使用的功能。</p>
    </div>
    <div v-if="cards.length > 0" class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
      <RouterLink
        v-for="card in cards"
        :key="card.routeName"
        :to="{ name: card.routeName }"
        class="card block p-5 transition hover:border-primary-300 hover:shadow-md"
      >
        <h3 class="mb-1 font-semibold text-primary-800">{{ card.title }}</h3>
        <p class="text-sm text-gray-600">{{ card.description }}</p>
      </RouterLink>
    </div>
    <p v-else class="text-sm text-gray-500">目前沒有可使用的功能，請聯繫管理員開通權限。</p>
  </div>
</template>
