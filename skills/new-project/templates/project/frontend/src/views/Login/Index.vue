<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { isHandled } from '@/api/http'
import { useAuthOptions } from '@/composables/useAuthOptions'
import { safeRedirect } from '@/router'
import { useAuthStore } from '@/stores/auth'
import { useMessageStore } from '@/stores/message'
import { getCaptcha, login } from './api'
import type { CaptchaResult } from './types'

const auth = useAuthStore()
const message = useMessageStore()
const route = useRoute()
const router = useRouter()
const { options, load } = useAuthOptions()
const systemName = import.meta.env.VITE_APP_TITLE

const form = reactive({ username: '', password: '', captchaCode: '' })
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
    const response = await login({
      username: form.username,
      password: form.password,
      captchaId: captcha.value?.captchaId,
      captchaCode: form.captchaCode,
    })
    const result = response.datas
    if (!result) return

    if (result.requirePasswordChange && result.passwordChangeToken) {
      auth.passwordChangeToken = result.passwordChangeToken
      await message.info(response.message)
      await router.push({ name: 'change-password' })
      return
    }

    if (result.token && result.expiresAt && result.basicUser) {
      auth.setLogin(result.token, result.expiresAt, result.basicUser)
      await router.push(safeRedirect(route.query.redirect))
    }
  } catch (error) {
    if (!isHandled(error)) message.error('操作失敗，請再試一次')
    form.password = ''
    await refreshCaptcha()
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="flex min-h-full items-center justify-center bg-gradient-to-br from-primary-50 to-primary-100 p-4">
    <div class="card w-full max-w-sm p-8">
      <div class="mb-6 flex flex-col items-center gap-2">
        <h1 class="text-xl font-bold text-primary-800">{{ systemName }}</h1>
      </div>
      <form class="space-y-4" @submit.prevent="submit">
        <div>
          <label for="login-username" class="form-label">帳號</label>
          <input id="login-username" v-model.trim="form.username" class="form-input" autocomplete="username" maxlength="50" required />
        </div>
        <div>
          <label for="login-password" class="form-label">密碼</label>
          <input id="login-password" v-model="form.password" type="password" class="form-input" autocomplete="current-password" maxlength="64" required />
        </div>
        <div v-if="options?.captchaEnabled">
          <label for="login-captcha" class="form-label">驗證碼</label>
          <div class="flex gap-2">
            <input
              id="login-captcha"
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
        <button type="submit" class="btn-primary w-full" :disabled="submitting">{{ submitting ? '登入中…' : '登入' }}</button>
      </form>
      <div class="mt-4 text-center text-sm">
        <RouterLink :to="{ name: 'forgot-password' }" class="text-primary-700 hover:underline">忘記密碼？</RouterLink>
      </div>
    </div>
  </div>
</template>
