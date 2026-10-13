<script setup lang="ts">
defineProps<{
  title: string
  width?: string
}>()

const open = defineModel<boolean>({ required: true })
</script>

<template>
  <div v-if="open" class="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-black/40 p-4 sm:items-center">
    <div class="w-full rounded-lg bg-white shadow-xl" :class="width ?? 'max-w-lg'" role="dialog" aria-modal="true">
      <div class="flex items-center justify-between border-b border-gray-200 px-6 py-4">
        <h3 class="text-lg font-semibold text-gray-900">{{ title }}</h3>
        <button type="button" class="text-2xl leading-none text-gray-400 hover:text-gray-600" aria-label="關閉" @click="open = false">
          ×
        </button>
      </div>
      <div class="px-6 py-4">
        <slot />
      </div>
      <div v-if="$slots.footer" class="flex justify-end gap-2 border-t border-gray-200 px-6 py-4">
        <slot name="footer" />
      </div>
    </div>
  </div>
</template>
