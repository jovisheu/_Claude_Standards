import { defineStore } from 'pinia'
import { computed, ref } from 'vue'

export type MessageType = 'success' | 'error' | 'info' | 'confirm'

interface MessageItem {
  type: MessageType
  text: string
  title?: string
  resolve: (value: boolean) => void
}

/**
 * 操作結果一律以訊息視窗顯示；多則訊息依序顯示（佇列），
 * 避免確認視窗開啟時被其他錯誤訊息覆蓋而誤判為「取消」
 */
export const useMessageStore = defineStore('message', () => {
  const queue = ref<MessageItem[]>([])
  const current = computed(() => queue.value[0] ?? null)
  const visible = computed(() => current.value !== null)

  function open(type: MessageType, text: string, title?: string): Promise<boolean> {
    return new Promise<boolean>((resolve) => {
      queue.value.push({ type, text, title, resolve })
    })
  }

  function close(result: boolean) {
    const item = queue.value.shift()
    item?.resolve(result)
  }

  return {
    current,
    visible,
    success: (text: string, title?: string) => open('success', text, title),
    error: (text: string, title?: string) => open('error', text, title),
    info: (text: string, title?: string) => open('info', text, title),
    confirm: (text: string, title?: string) => open('confirm', text, title),
    close,
  }
})
