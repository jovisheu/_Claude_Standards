<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { isHandled } from '@/api/http'
import { useAuthOptions } from '@/composables/useAuthOptions'
import { useAuthStore } from '@/stores/auth'
import { useMessageStore } from '@/stores/message'
import { getCaptcha, login } from './api'
import type { Captcha } from './types'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const message = useMessageStore()
const { options, ready } = useAuthOptions()

const form = reactive({ username: '', password: '', captchaCode: '' })
const captcha = ref<Captcha | null>(null)
const submitting = ref(false)
const appTitle = import.meta.env.VITE_APP_TITLE

async function refreshCaptcha() {
  form.captchaCode = ''
  if (!options.value?.captchaEnabled) return
  try {
    captcha.value = await getCaptcha()
  } catch {
    captcha.value = null
  }
}

/** 只接受站內路徑，避免開放式重新導向 */
function safeRedirect(): string {
  const redirect = route.query.redirect
  if (typeof redirect === 'string' && redirect.startsWith('/') && !redirect.startsWith('//')) return redirect
  return '/'
}

async function submit() {
  if (!form.username.trim() || !form.password) {
    await message.error('請輸入帳號與密碼')
    return
  }
  if (options.value?.captchaEnabled && !form.captchaCode.trim()) {
    await message.error('請輸入驗證碼')
    return
  }

  submitting.value = true
  try {
    const result = await login({
      username: form.username.trim(),
      password: form.password,
      captchaId: captcha.value?.captchaId,
      captchaCode: form.captchaCode.trim(),
    })

    form.password = ''
    if (result.requirePasswordChange && result.changeToken) {
      auth.setChangeToken(result.changeToken)
      await message.info('密碼已逾期（或為初始密碼），請先變更密碼')
      await router.replace({ name: 'change-password' })
      return
    }

    auth.setLogin(result)
    await router.replace(safeRedirect())
  } catch (error) {
    if (!isHandled(error)) await message.error('操作失敗，請再試一次')
    // 驗證碼為一次性，失敗後重新產生
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
      <h1 class="mb-1 text-center text-2xl font-bold text-gray-900">{{ options?.systemName || appTitle }}</h1>
      <p class="mb-6 text-center text-sm text-gray-500">請登入以繼續</p>

      <form class="space-y-4" autocomplete="on" @submit.prevent="submit">
        <div>
          <label class="form-label" for="username">帳號</label>
          <input id="username" v-model="form.username" class="form-input" maxlength="50" autocomplete="username" autofocus />
        </div>
        <div>
          <label class="form-label" for="password">密碼</label>
          <input
            id="password"
            v-model="form.password"
            type="password"
            class="form-input"
            maxlength="64"
            autocomplete="current-password"
          />
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

        <button type="submit" class="btn-primary w-full" :disabled="submitting">
          {{ submitting ? '登入中…' : '登入' }}
        </button>
      </form>

      <div class="mt-4 text-center">
        <RouterLink :to="{ name: 'forgot-password' }" class="btn-link">忘記密碼？</RouterLink>
      </div>
    </div>
  </div>
</template>
