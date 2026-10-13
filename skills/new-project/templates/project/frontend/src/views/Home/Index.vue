<script setup lang="ts">
import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { menuItems } from '@/layouts/menu'

const auth = useAuthStore()

const shortcuts = computed(() =>
  menuItems.filter((item) => item.name !== 'home' && (!item.permission || auth.hasPermission(item.permission))),
)
</script>

<template>
  <div class="space-y-6">
    <div class="card p-6">
      <h2 class="text-lg font-semibold text-gray-900">歡迎，{{ auth.user?.displayName }}</h2>
      <p class="mt-1 text-sm text-gray-500">請由左側選單選擇要使用的功能。</p>
    </div>

    <div v-if="shortcuts.length" class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      <RouterLink
        v-for="item in shortcuts"
        :key="item.name"
        :to="{ name: item.name }"
        class="card block p-5 transition hover:shadow-md"
      >
        <div class="font-medium text-gray-900">{{ item.title }}</div>
        <div class="mt-1 text-sm text-gray-500">{{ item.description }}</div>
      </RouterLink>
    </div>
  </div>
</template>
