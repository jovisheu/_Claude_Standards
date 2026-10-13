<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { isHandled } from '@/api/http'
import PasswordRules from '@/components/PasswordRules.vue'
import { useAuthStore } from '@/stores/auth'
import { useMessageStore } from '@/stores/message'
import { changeExpiredPassword } from './api'

const router = useRouter()
const auth = useAuthStore()
const message = useMessageStore()

const form = reactive({ newPassword: '', confirmPassword: '' })
const submitting = ref(false)

async function backToLogin() {
  auth.clear()
  await router.replace({ name: 'login' })
}

async function submit() {
  if (!auth.changeToken) {
    await message.error('變更憑證無效或已過期，請重新登入')
    await backToLogin()
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
    const result = await changeExpiredPassword({
      changeToken: auth.changeToken,
      newPassword: form.newPassword,
      confirmPassword: form.confirmPassword,
    })
    // 成功後先清除登入狀態再導向登入頁
    await message.success(result.message)
    await backToLogin()
  } catch (error) {
    if (!isHandled(error)) await message.error('操作失敗，請再試一次')
  } finally {
    submitting.value = false
  }
}

onMounted(async () => {
  // 變更憑證只存於記憶體，重新整理後須重新登入
  if (!auth.changeToken) {
    await message.error('變更憑證無效或已過期，請重新登入')
    await backToLogin()
  }
})
</script>

<template>
  <div class="flex min-h-screen items-center justify-center bg-gradient-to-br from-primary-50 to-gray-100 p-4">
    <div class="card w-full max-w-sm p-8">
      <h1 class="mb-1 text-center text-xl font-bold text-gray-900">變更密碼</h1>
      <p class="mb-6 text-center text-sm text-gray-500">密碼已逾期，請設定新密碼後重新登入</p>

      <form class="space-y-4" @submit.prevent="submit">
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
          {{ submitting ? '處理中…' : '變更密碼' }}
        </button>
      </form>

      <div class="mt-4 text-center">
        <button type="button" class="btn-link" @click="backToLogin">返回登入</button>
      </div>
    </div>
  </div>
</template>
