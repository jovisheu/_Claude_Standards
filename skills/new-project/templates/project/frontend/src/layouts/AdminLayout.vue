<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { adminRoutes } from '@/router/featureRoutes'
import { useAuthStore } from '@/stores/auth'
import { useMessageStore } from '@/stores/message'
import { buildMenu } from './menu'

const auth = useAuthStore()
const message = useMessageStore()
const route = useRoute()
const router = useRouter()
const systemName = import.meta.env.VITE_APP_TITLE
const sidebarOpen = ref(false)

const menuItems = computed(() =>
  buildMenu(adminRoutes).filter((item) => !item.basicPermission || auth.hasBasicPermission(item.basicPermission)),
)

// 任何換頁（含瀏覽器上一頁）都收合手機版選單
watch(
  () => route.fullPath,
  () => {
    sidebarOpen.value = false
  },
)

async function logout() {
  if (!(await message.confirm('確定要登出嗎？'))) return
  auth.clear()
  await router.push({ name: 'login' })
}
</script>

<template>
  <div class="flex h-full">
    <!-- 側邊選單 -->
    <aside
      class="fixed inset-y-0 left-0 z-30 flex w-60 flex-col bg-primary-900 text-white transition-transform lg:static lg:translate-x-0"
      :class="sidebarOpen ? 'translate-x-0' : '-translate-x-full'"
    >
      <div class="flex h-16 items-center gap-2 border-b border-primary-800 px-4">
        <span class="truncate text-lg font-bold">{{ systemName }}</span>
      </div>
      <nav class="flex-1 overflow-y-auto py-3">
        <RouterLink
          v-for="item in menuItems"
          :key="item.routeName"
          :to="{ name: item.routeName }"
          class="mx-2 mb-1 block rounded-md px-3 py-2 text-sm text-primary-100 hover:bg-primary-800 hover:text-white"
          exact-active-class="bg-primary-700 text-white"
        >
          {{ item.title }}
        </RouterLink>
      </nav>
    </aside>
    <div v-if="sidebarOpen" class="fixed inset-0 z-20 bg-black/40 lg:hidden" @click="sidebarOpen = false"></div>

    <!-- 主要區域 -->
    <div class="flex min-w-0 flex-1 flex-col">
      <header class="flex h-16 items-center gap-3 border-b border-gray-200 bg-white px-4 shadow-sm">
        <button type="button" class="btn-secondary btn-sm lg:hidden" aria-label="開啟選單" @click="sidebarOpen = true">☰</button>
        <h1 class="min-w-0 flex-1 truncate text-base font-semibold text-gray-800">{{ route.meta.title }}</h1>
        <div class="flex items-center gap-3 text-sm">
          <span class="hidden text-gray-600 sm:inline">{{ auth.basicUser?.displayName }}（{{ auth.basicUser?.username }}）</span>
          <button type="button" class="btn-secondary btn-sm" @click="logout">登出</button>
        </div>
      </header>
      <main class="flex-1 overflow-y-auto p-4 lg:p-6">
        <RouterView />
      </main>
    </div>
  </div>
</template>
