<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useMessageStore } from '@/stores/message'
import { menuItems } from './menu'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const message = useMessageStore()

const appTitle = import.meta.env.VITE_APP_TITLE
const sidebarOpen = ref(false)

const visibleMenu = computed(() => menuItems.filter((item) => !item.basicPermission || auth.hasBasicPermission(item.basicPermission)))

// 小螢幕切換頁面後收合選單
watch(
  () => route.fullPath,
  () => {
    sidebarOpen.value = false
  },
)

async function logout() {
  if (!(await message.confirm('確定要登出嗎？'))) return
  auth.clear()
  await router.replace({ name: 'login' })
}
</script>

<template>
  <div class="flex min-h-screen">
    <!-- 側邊選單 -->
    <aside
      class="fixed inset-y-0 left-0 z-40 w-60 transform bg-gray-900 text-gray-200 transition-transform lg:static lg:translate-x-0"
      :class="sidebarOpen ? 'translate-x-0' : '-translate-x-full'"
    >
      <div class="flex h-14 items-center border-b border-gray-800 px-5 text-lg font-bold text-white">{{ appTitle }}</div>
      <nav class="space-y-1 p-3">
        <RouterLink
          v-for="item in visibleMenu"
          :key="item.name"
          :to="{ name: item.name }"
          class="block rounded-md px-3 py-2 text-sm hover:bg-gray-800 hover:text-white"
          :class="route.name === item.name ? 'bg-primary-600 text-white hover:bg-primary-600' : ''"
        >
          {{ item.title }}
        </RouterLink>
      </nav>
    </aside>
    <div v-if="sidebarOpen" class="fixed inset-0 z-30 bg-black/40 lg:hidden" @click="sidebarOpen = false" />

    <div class="flex min-w-0 flex-1 flex-col">
      <!-- 頂列 -->
      <header class="flex h-14 items-center justify-between border-b border-gray-200 bg-white px-4 shadow-sm">
        <div class="flex items-center gap-3">
          <button type="button" class="rounded p-1 text-xl text-gray-600 hover:bg-gray-100 lg:hidden" aria-label="開啟選單" @click="sidebarOpen = true">
            ☰
          </button>
          <span class="font-medium text-gray-700">{{ route.meta.title }}</span>
        </div>
        <div class="flex items-center gap-3 text-sm">
          <span class="hidden text-gray-600 sm:inline">{{ auth.basicUser?.displayName }}（{{ auth.basicUser?.username }}）</span>
          <button type="button" class="btn-secondary px-3 py-1.5" @click="logout">登出</button>
        </div>
      </header>

      <main class="flex-1 p-4 sm:p-6">
        <RouterView />
      </main>
    </div>
  </div>
</template>
