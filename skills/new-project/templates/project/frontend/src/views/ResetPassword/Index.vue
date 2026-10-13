<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { isHandled } from '@/api/http'
import PasswordRules from '@/components/PasswordRules.vue'
import { useAuthStore } from '@/stores/auth'
import { useMessageStore } from '@/stores/message'
import { resetPassword } from './api'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const message = useMessageStore()

const form = reactive({ newPassword: '', confirmPassword: '' })
const token = ref('')
const submitting = ref(false)

async function submit() {
  if (!token.value) {
    await message.error('重設連結無效或已過期，請重新申請')
    return
  }
  if (!form.newPassword) {
    await message.error('請輸入新密碼')
    return
  }
  if (form.newPassword !== form.confirmPassword) {
    await message.error('兩次輸入的密碼不一致')
    return
  }

  submitting.value = true
  try {
    const result = await resetPassword({
      token: token.value,
      newPassword: form.newPassword,
      confirmPassword: form.confirmPassword,
    })
    token.value = ''
    // 成功後先清除登入狀態再導向登入頁
    auth.clear()
    await message.success(result.message)
    await router.replace({ name: 'login' })
  } catch (error) {
    if (!isHandled(error)) await message.error('操作失敗，請再試一次')
  } finally {
    submitting.value = false
  }
}

onMounted(async () => {
  // 讀取 token 後立即從網址移除，避免留在瀏覽紀錄或 Referer
  const value = route.query.token
  token.value = typeof value === 'string' ? value : ''
  await router.replace({ name: 'reset-password' })

  if (!token.value) {
    await message.error('重設連結無效或已過期，請重新申請')
  }
})
</script>

<template>
  <div class="flex min-h-screen items-center justify-center bg-gradient-to-br from-primary-50 to-gray-100 p-4">
    <div class="card w-full max-w-sm p-8">
      <h1 class="mb-1 text-center text-xl font-bold text-gray-900">重設密碼</h1>
      <p class="mb-6 text-center text-sm text-gray-500">請設定新密碼，完成後以新密碼重新登入</p>

      <form v-if="token" class="space-y-4" @submit.prevent="submit">
        <div>
          <label class="form-label" for="newPassword">新密碼</label>
          <input
            id="newPassword"
            v-model="form.newPassword"
            type="password"
            class="form-input"
            maxlength="64"
            autocomplete="new-password"
          />
        </div>
        <div>
          <label class="form-label" for="confirmPassword">確認新密碼</label>
          <input
            id="confirmPassword"
            v-model="form.confirmPassword"
            type="password"
            class="form-input"
            maxlength="64"
            autocomplete="new-password"
          />
        </div>
        <PasswordRules />
        <button type="submit" class="btn-primary w-full" :disabled="submitting">
          {{ submitting ? '處理中…' : '重設密碼' }}
        </button>
      </form>

      <div class="mt-4 flex justify-center gap-4">
        <RouterLink :to="{ name: 'forgot-password' }" class="btn-link">重新申請</RouterLink>
        <RouterLink :to="{ name: 'login' }" class="btn-link">返回登入</RouterLink>
      </div>
    </div>
  </div>
</template>
