import { defineStore } from 'pinia'
import { ref } from 'vue'

export type MessageType = 'success' | 'error' | 'info' | 'confirm'

interface MessageItem {
  type: MessageType
  title: string
  text: string
  resolve: (value: boolean) => void
}

/**
 * 訊息視窗：操作結果一律以訊息視窗顯示，不使用頁面內提示列
 */
export const useMessageStore = defineStore('message', () => {
  const queue = ref<MessageItem[]>([])
  const current = ref<MessageItem | null>(null)

  function next() {
    current.value = queue.value.shift() ?? null
  }

  function push(type: MessageType, text: string, title: string): Promise<boolean> {
    return new Promise((resolve) => {
      queue.value.push({ type, text, title, resolve })
      if (!current.value) next()
    })
  }

  function close(result: boolean) {
    current.value?.resolve(result)
    next()
  }

  const success = (text: string, title = '操作成功') => push('success', text, title).then(() => undefined)
  const error = (text: string, title = '操作失敗') => push('error', text, title).then(() => undefined)
  const info = (text: string, title = '提示') => push('info', text, title).then(() => undefined)
  const confirm = (text: string, title = '請確認') => push('confirm', text, title)

  return { current, close, success, error, info, confirm }
})
