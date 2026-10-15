<script setup lang="ts">
import { computed, reactive, watch } from 'vue'
import BaseModal from '@/components/BaseModal.vue'
import PasswordRules from '@/components/PasswordRules.vue'
import type { BasicGroupOption, BasicUserDetail, BasicUserFormData } from '../types'

const props = defineProps<{
  /** null 為新增 */
  item: BasicUserDetail | null
  basicGroupOptions: BasicGroupOption[]
  /** 是否為目前登入者本人（不可停用自己、不可變更自己的群組） */
  isSelf: boolean
  submitting: boolean
}>()

const emit = defineEmits<{
  submit: [form: BasicUserFormData]
}>()

const open = defineModel<boolean>({ required: true })

const isEdit = computed(() => props.item !== null)

const form = reactive<BasicUserFormData>({
  username: '',
  email: '',
  displayName: '',
  password: '',
  isActive: true,
  basicGroupIds: [],
})

// 開啟時重設表單
watch(open, (value) => {
  if (!value) return
  form.username = props.item?.username ?? ''
  form.email = props.item?.email ?? ''
  form.displayName = props.item?.displayName ?? ''
  form.password = ''
  form.isActive = props.item?.isActive ?? true
  form.basicGroupIds = [...(props.item?.basicGroupIds ?? [])]
})

/** 無法指派的群組或本人：不渲染勾選框，保留既有勾選並以文字顯示 */
function isBasicGroupEditable(basicGroup: BasicGroupOption) {
  return !props.isSelf && basicGroup.assignable
}

function hasBasicGroup(basicGroup: BasicGroupOption) {
  return form.basicGroupIds.includes(basicGroup.id)
}

function submit() {
  emit('submit', { ...form, basicGroupIds: [...form.basicGroupIds] })
}
</script>

<template>
  <BaseModal v-model="open" :title="isEdit ? '編輯使用者' : '新增使用者'">
    <form id="basic-user-form" class="space-y-4" @submit.prevent="submit">
      <div>
        <template v-if="isEdit">
          <span class="form-label">帳號</span>
          <p class="py-2 text-sm text-gray-900">{{ form.username }}</p>
        </template>
        <template v-else>
          <label class="form-label" for="basic-user-username">帳號</label>
          <input id="basic-user-username" v-model="form.username" class="form-input" maxlength="50" autocomplete="off" />
        </template>
      </div>
      <div>
        <label class="form-label" for="basic-user-display-name">顯示名稱</label>
        <input id="basic-user-display-name" v-model="form.displayName" class="form-input" maxlength="50" />
      </div>
      <div>
        <label class="form-label" for="basic-user-email">Email</label>
        <input id="basic-user-email" v-model="form.email" type="email" class="form-input" maxlength="256" />
      </div>
      <div>
        <label class="form-label" for="basic-user-password">{{ isEdit ? '密碼（留白表示不變更）' : '密碼' }}</label>
        <input
          id="basic-user-password"
          v-model="form.password"
          type="password"
          class="form-input"
          maxlength="64"
          autocomplete="new-password"
        />
        <div class="mt-2">
          <PasswordRules />
        </div>
      </div>
      <div>
        <!-- 不可停用自己：本人不渲染勾選框 -->
        <p v-if="isSelf" class="text-sm text-gray-700">狀態：{{ form.isActive ? '啟用' : '停用' }}（不可變更自己的狀態）</p>
        <label v-else class="inline-flex items-center gap-2 text-sm text-gray-700">
          <input v-model="form.isActive" type="checkbox" class="h-4 w-4" />
          啟用
        </label>
      </div>
      <div>
        <span class="form-label">群組</span>
        <p v-if="isSelf" class="mb-2 text-xs text-gray-500">不可變更自己的群組</p>
        <div class="grid gap-2 sm:grid-cols-2">
          <template v-for="basicGroup in basicGroupOptions" :key="basicGroup.id">
            <label v-if="isBasicGroupEditable(basicGroup)" class="inline-flex items-center gap-2 text-sm text-gray-700">
              <input v-model="form.basicGroupIds" type="checkbox" class="h-4 w-4" :value="basicGroup.id" />
              {{ basicGroup.name }}
            </label>
            <!-- 不可變更：不渲染勾選框，已加入的群組以文字顯示 -->
            <span v-else-if="hasBasicGroup(basicGroup)" class="inline-flex items-center gap-1 text-sm text-gray-500">✓ {{ basicGroup.name }}</span>
          </template>
        </div>
      </div>
    </form>

    <template #footer>
      <button type="button" class="btn-secondary" @click="open = false">取消</button>
      <button type="submit" form="basic-user-form" class="btn-primary" :disabled="submitting">
        {{ submitting ? '儲存中…' : '儲存' }}
      </button>
    </template>
  </BaseModal>
</template>
