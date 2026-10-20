<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import PasswordRules from '@/components/PasswordRules.vue'
import { isHandled } from '@/api/http'
import { useAuthStore } from '@/stores/auth'
import { useMessageStore } from '@/stores/message'
import { resetPassword } from './api'

const auth = useAuthStore()
const message = useMessageStore()
const route = useRoute()
const router = useRouter()

const token = ref('')
const form = reactive({ newPassword: '', confirmPassword: '' })
const submitting = ref(false)

onMounted(async () => {
  // 讀取 token 後立即從網址移除，避免留在瀏覽紀錄或 Referer
  token.value = typeof route.query.token === 'string' ? route.query.token : ''
  if (route.query.token !== undefined) {
    await router.replace({ name: 'reset-password' })
  }
})

async function submit() {
  if (form.newPassword !== form.confirmPassword) {
    await message.error('兩次輸入的新密碼不一致')
    return
  }

  submitting.value = true
  try {
    const response = await resetPassword({ token: token.value, newPassword: form.newPassword })
    // 成功後先清除登入狀態，再以新密碼重新登入
    auth.clear()
    await message.success(response.message)
    await router.push({ name: 'login' })
  } catch (error) {
    if (!isHandled(error)) message.error('操作失敗，請再試一次')
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="flex min-h-full items-center justify-center bg-gradient-to-br from-primary-50 to-primary-100 p-4">
    <div class="card w-full max-w-sm p-8">
      <h1 class="mb-4 text-xl font-bold text-primary-800">重設密碼</h1>
      <template v-if="token">
        <form class="space-y-4" @submit.prevent="submit">
          <div>
            <label for="reset-new-password" class="form-label">新密碼</label>
            <input id="reset-new-password" v-model="form.newPassword" type="password" class="form-input" autocomplete="new-password" maxlength="64" required />
          </div>
          <div>
            <label for="reset-confirm-password" class="form-label">確認新密碼</label>
            <input id="reset-confirm-password" v-model="form.confirmPassword" type="password" class="form-input" autocomplete="new-password" maxlength="64" required />
          </div>
          <PasswordRules />
          <button type="submit" class="btn-primary w-full" :disabled="submitting">{{ submitting ? '送出中…' : '設定新密碼' }}</button>
        </form>
      </template>
      <p v-else class="text-sm text-gray-600">重設連結無效或已過期，請重新申請忘記密碼。</p>
      <div class="mt-4 flex justify-center gap-4 text-sm">
        <RouterLink :to="{ name: 'forgot-password' }" class="text-primary-700 hover:underline">重新申請</RouterLink>
        <RouterLink :to="{ name: 'login' }" class="text-primary-700 hover:underline">返回登入</RouterLink>
      </div>
    </div>
  </div>
</template>
