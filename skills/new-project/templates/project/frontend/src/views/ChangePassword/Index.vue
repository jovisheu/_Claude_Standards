<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import PasswordRules from '@/components/PasswordRules.vue'
import { isHandled } from '@/api/http'
import { useAuthStore } from '@/stores/auth'
import { useMessageStore } from '@/stores/message'
import { changeExpiredPassword } from './api'

const auth = useAuthStore()
const message = useMessageStore()
const router = useRouter()

const form = reactive({ newPassword: '', confirmPassword: '' })
const submitting = ref(false)

onMounted(async () => {
  // 變更憑證只存於記憶體，重新整理後須重新登入
  if (!auth.passwordChangeToken) {
    await message.info('請先登入')
    await router.replace({ name: 'login' })
  }
})

// 放棄變更時一併清除記憶體中的變更憑證
async function backToLogin() {
  auth.passwordChangeToken = null
  await router.push({ name: 'login' })
}

async function submit() {
  if (form.newPassword !== form.confirmPassword) {
    await message.error('兩次輸入的新密碼不一致')
    return
  }

  submitting.value = true
  try {
    const response = await changeExpiredPassword({ token: auth.passwordChangeToken ?? '', newPassword: form.newPassword })
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
      <h1 class="mb-2 text-xl font-bold text-primary-800">變更密碼</h1>
      <p class="mb-5 text-sm text-gray-600">您的密碼已逾期，請設定新密碼後重新登入。</p>
      <form class="space-y-4" @submit.prevent="submit">
        <div>
          <label for="change-new-password" class="form-label">新密碼</label>
          <input id="change-new-password" v-model="form.newPassword" type="password" class="form-input" autocomplete="new-password" maxlength="64" required />
        </div>
        <div>
          <label for="change-confirm-password" class="form-label">確認新密碼</label>
          <input id="change-confirm-password" v-model="form.confirmPassword" type="password" class="form-input" autocomplete="new-password" maxlength="64" required />
        </div>
        <PasswordRules />
        <button type="submit" class="btn-primary w-full" :disabled="submitting">{{ submitting ? '送出中…' : '變更密碼' }}</button>
      </form>
      <div class="mt-4 text-center text-sm">
        <button type="button" class="text-primary-700 hover:underline" @click="backToLogin">返回登入</button>
      </div>
    </div>
  </div>
</template>
