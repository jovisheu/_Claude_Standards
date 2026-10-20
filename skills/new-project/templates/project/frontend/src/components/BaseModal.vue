<script setup lang="ts">
import { onBeforeUnmount, watch } from 'vue'
import { useMessageStore } from '@/stores/message'

withDefaults(defineProps<{ title: string; width?: string }>(), { width: 'max-w-lg' })
const open = defineModel<boolean>({ required: true })
const message = useMessageStore()

function onKeydown(event: KeyboardEvent) {
  // 訊息視窗在上層時，Esc 不關閉下層的 Modal
  if (event.key === 'Escape' && !message.visible) open.value = false
}

// immediate：元件建立時 open 已為 true（如以 v-if 建立）也要掛上監聽
watch(
  open,
  (value) => {
    if (value) {
      window.addEventListener('keydown', onKeydown)
    } else {
      window.removeEventListener('keydown', onKeydown)
    }
  },
  { immediate: true },
)

onBeforeUnmount(() => window.removeEventListener('keydown', onKeydown))
</script>

<template>
  <Teleport to="body">
    <div v-if="open" class="fixed inset-0 z-40 flex items-start justify-center overflow-y-auto bg-black/40 p-4 sm:items-center">
      <div class="card w-full" :class="width" role="dialog" aria-modal="true">
        <div class="flex items-center justify-between border-b border-gray-200 px-5 py-3">
          <h2 class="text-base font-semibold text-gray-900">{{ title }}</h2>
          <button type="button" class="text-xl leading-none text-gray-400 hover:text-gray-600" aria-label="關閉" @click="open = false">×</button>
        </div>
        <div class="max-h-[70vh] overflow-y-auto px-5 py-4">
          <slot />
        </div>
        <div v-if="$slots.footer" class="flex justify-end gap-2 border-t border-gray-200 px-5 py-3">
          <slot name="footer" />
        </div>
      </div>
    </div>
  </Teleport>
</template>
