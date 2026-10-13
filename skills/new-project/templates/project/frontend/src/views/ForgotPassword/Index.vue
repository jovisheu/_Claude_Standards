<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { isHandled } from '@/api/http'
import { useAuthOptions } from '@/composables/useAuthOptions'
import { useMessageStore } from '@/stores/message'
import { getCaptcha } from '@/views/Login/api'
import type { Captcha } from '@/views/Login/types'
import { forgotPassword } from './api'

const router = useRouter()
const message = useMessageStore()
const { options, ready } = useAuthOptions()

const form = reactive({ username: '', email: '', captchaCode: '' })
const captcha = ref<Captcha | null>(null)
const submitting = ref(false)

async function refreshCaptcha() {
  form.captchaCode = ''
  if (!options.value?.captchaEnabled) return
  try {
    captcha.value = await getCaptcha()
  } catch {
    captcha.value = null
  }
}

async function submit() {
  if (!form.username.trim() || !form.email.trim()) {
    await message.error('請輸入帳號與 Email')
    return
  }
  if (options.value?.captchaEnabled && !form.captchaCode.trim()) {
    await message.error('請輸入驗證碼')
    return
  }

  submitting.value = true
  try {
    const result = await forgotPassword({
      username: form.username.trim(),
      email: form.email.trim(),
      captchaId: captcha.value?.captchaId,
      captchaCode: form.captchaCode.trim(),
    })
    await message.success(result.message, '已送出申請')
    await router.replace({ name: 'login' })
  } catch (error) {
    if (!isHandled(error)) await message.error('操作失敗，請再試一次')
    await refreshCaptcha()
  } finally {
    submitting.value = false
  }
}

onMounted(async () => {
  await ready()
  await refreshCaptcha()
})
</script>

<template>
  <div class="flex min-h-screen items-center justify-center bg-gradient-to-br from-primary-50 to-gray-100 p-4">
    <div class="card w-full max-w-sm p-8">
      <h1 class="mb-1 text-center text-xl font-bold text-gray-900">忘記密碼</h1>
      <p class="mb-6 text-center text-sm text-gray-500">輸入帳號與註冊的 Email，系統將寄送重設密碼連結</p>

      <form class="space-y-4" @submit.prevent="submit">
        <div>
          <label class="form-label" for="username">帳號</label>
          <input id="username" v-model="form.username" class="form-input" maxlength="50" autocomplete="username" />
        </div>
        <div>
          <label class="form-label" for="email">Email</label>
          <input id="email" v-model="form.email" type="email" class="form-input" maxlength="256" autocomplete="email" />
        </div>
        <div v-if="options?.captchaEnabled">
          <label class="form-label" for="captcha">驗證碼</label>
          <div class="flex items-center gap-2">
            <input
              id="captcha"
              v-model="form.captchaCode"
              class="form-input uppercase"
              :maxlength="options.captchaLength"
              autocomplete="off"
            />
            <button type="button" class="shrink-0" title="重新產生驗證碼" @click="refreshCaptcha">
              <img v-if="captcha" :src="captcha.image" alt="驗證碼" class="h-10 w-[125px] rounded border border-gray-200" />
              <span v-else class="flex h-10 w-[125px] items-center justify-center rounded border border-gray-200 text-xs text-gray-400">
                重新產生
              </span>
            </button>
          </div>
        </div>
        <p v-if="options" class="text-xs text-gray-500">重設連結 {{ options.resetTokenExpireMinutes }} 分鐘內有效，僅能使用一次。</p>
        <button type="submit" class="btn-primary w-full" :disabled="submitting">
          {{ submitting ? '處理中…' : '寄送重設連結' }}
        </button>
      </form>

      <div class="mt-4 text-center">
        <RouterLink :to="{ name: 'login' }" class="btn-link">返回登入</RouterLink>
      </div>
    </div>
  </div>
</template>
