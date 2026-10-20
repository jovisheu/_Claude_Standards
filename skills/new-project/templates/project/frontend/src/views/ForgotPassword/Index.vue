<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { isHandled } from '@/api/http'
import { useAuthOptions } from '@/composables/useAuthOptions'
import { useMessageStore } from '@/stores/message'
import { forgotPassword, getCaptcha } from './api'
import type { CaptchaResult } from './types'

const message = useMessageStore()
const router = useRouter()
const { options, load } = useAuthOptions()
const form = reactive({ username: '', email: '', captchaCode: '' })
const captcha = ref<CaptchaResult | null>(null)
const submitting = ref(false)

async function refreshCaptcha() {
  form.captchaCode = ''
  if (!options.value?.captchaEnabled) return
  try {
    captcha.value = (await getCaptcha()).datas
  } catch {
    // 錯誤已由 interceptor 處理
  }
}

onMounted(async () => {
  try {
    await load()
    await refreshCaptcha()
  } catch {
    // 錯誤已由 interceptor 處理
  }
})

async function submit() {
  submitting.value = true
  try {
    const response = await forgotPassword({
      username: form.username,
      email: form.email,
      captchaId: captcha.value?.captchaId,
      captchaCode: form.captchaCode,
    })
    await message.success(response.message)
    await router.push({ name: 'login' })
  } catch (error) {
    if (!isHandled(error)) message.error('操作失敗，請再試一次')
    // 驗證碼為一次性，失敗後重新產生
    await refreshCaptcha()
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="flex min-h-full items-center justify-center bg-gradient-to-br from-primary-50 to-primary-100 p-4">
    <div class="card w-full max-w-sm p-8">
      <h1 class="mb-2 text-xl font-bold text-primary-800">忘記密碼</h1>
      <p class="mb-5 text-sm text-gray-600">
        請輸入帳號與註冊的 Email，系統將寄送重設密碼連結<span v-if="options">（{{ options.passwordResetTokenExpireMinutes }} 分鐘內有效）</span>。
      </p>
      <form class="space-y-4" @submit.prevent="submit">
        <div>
          <label for="forgot-username" class="form-label">帳號</label>
          <input id="forgot-username" v-model.trim="form.username" class="form-input" autocomplete="username" maxlength="50" required />
        </div>
        <div>
          <label for="forgot-email" class="form-label">Email</label>
          <input id="forgot-email" v-model.trim="form.email" type="email" class="form-input" autocomplete="email" maxlength="256" required />
        </div>
        <div v-if="options?.captchaEnabled">
          <label for="forgot-captcha" class="form-label">驗證碼</label>
          <div class="flex gap-2">
            <input
              id="forgot-captcha"
              v-model.trim="form.captchaCode"
              class="form-input uppercase"
              autocomplete="off"
              :maxlength="options.captchaLength"
              required
            />
            <button type="button" class="shrink-0 rounded border border-gray-300" title="重新產生驗證碼" @click="refreshCaptcha">
              <img v-if="captcha" :src="captcha.image" alt="驗證碼" class="h-10" />
              <span v-else class="block px-3 py-2 text-xs text-gray-500">取得驗證碼</span>
            </button>
          </div>
          <p class="mt-1 text-xs text-gray-500">看不清楚？點選圖片重新產生</p>
        </div>
        <button type="submit" class="btn-primary w-full" :disabled="submitting">{{ submitting ? '送出中…' : '寄送重設連結' }}</button>
      </form>
      <div class="mt-4 text-center text-sm">
        <RouterLink :to="{ name: 'login' }" class="text-primary-700 hover:underline">返回登入</RouterLink>
      </div>
    </div>
  </div>
</template>
