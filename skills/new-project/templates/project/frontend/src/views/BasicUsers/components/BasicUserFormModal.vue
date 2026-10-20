<script setup lang="ts">
import { computed, reactive, watch } from 'vue'
import BaseModal from '@/components/BaseModal.vue'
import PasswordRules from '@/components/PasswordRules.vue'
import type { BasicGroupOption, BasicUserDetail, BasicUserFormData } from '../types'

const props = defineProps<{
  /** null 為新增 */
  item: BasicUserDetail | null
  basicGroupOptions: BasicGroupOption[]
  submitting: boolean
}>()
const emit = defineEmits<{ submit: [data: BasicUserFormData] }>()
const open = defineModel<boolean>({ required: true })

const form = reactive<BasicUserFormData>({ username: '', email: '', displayName: '', password: '', isActive: true, basicGroupIds: [] })
const isEdit = computed(() => props.item !== null)
const isSelf = computed(() => props.item?.isSelf ?? false)

// 不可指派的群組與本人的群組只顯示文字，不渲染勾選框
const editableGroups = computed(() => (isSelf.value ? [] : props.basicGroupOptions.filter((g) => g.isAssignable)))
const readonlyGroupNames = computed(() =>
  props.basicGroupOptions
    .filter((g) => (isSelf.value || !g.isAssignable) && form.basicGroupIds.includes(g.id))
    .map((g) => g.name),
)

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

function submit() {
  emit('submit', { ...form, basicGroupIds: [...form.basicGroupIds] })
}
</script>

<template>
  <BaseModal v-model="open" :title="isEdit ? '編輯使用者' : '新增使用者'">
    <form id="basic-user-form" class="space-y-4" @submit.prevent="submit">
      <div>
        <label for="basic-user-username" class="form-label">帳號</label>
        <input v-if="!isEdit" id="basic-user-username" v-model.trim="form.username" class="form-input" maxlength="50" autocomplete="off" required />
        <p v-else class="text-sm text-gray-800">{{ form.username }}</p>
      </div>
      <div>
        <label for="basic-user-display-name" class="form-label">顯示名稱</label>
        <input id="basic-user-display-name" v-model.trim="form.displayName" class="form-input" maxlength="50" required />
      </div>
      <div>
        <label for="basic-user-email" class="form-label">Email</label>
        <input id="basic-user-email" v-model.trim="form.email" type="email" class="form-input" maxlength="256" required />
      </div>
      <div>
        <label for="basic-user-password" class="form-label">{{ isEdit ? '密碼（留白表示不變更）' : '密碼' }}</label>
        <input
          id="basic-user-password"
          v-model="form.password"
          type="password"
          class="form-input"
          maxlength="64"
          autocomplete="new-password"
          :required="!isEdit"
        />
        <div class="mt-1"><PasswordRules /></div>
      </div>
      <div>
        <span class="form-label">狀態</span>
        <label v-if="!isSelf" class="inline-flex items-center gap-2 text-sm">
          <input v-model="form.isActive" type="checkbox" class="h-4 w-4 accent-primary-600" />
          啟用
        </label>
        <p v-else class="text-sm text-gray-800">啟用（不可停用自己）</p>
      </div>
      <div>
        <span class="form-label">群組</span>
        <div class="flex flex-wrap gap-x-4 gap-y-2">
          <label v-for="group in editableGroups" :key="group.id" class="inline-flex items-center gap-2 text-sm">
            <input v-model="form.basicGroupIds" type="checkbox" :value="group.id" class="h-4 w-4 accent-primary-600" />
            {{ group.name }}
          </label>
        </div>
        <p v-if="readonlyGroupNames.length > 0" class="mt-2 text-sm text-gray-600">
          {{ isSelf ? '目前群組（不可變更自己的群組）' : '不可變更的群組' }}：{{ readonlyGroupNames.join('、') }}
        </p>
      </div>
    </form>
    <template #footer>
      <button type="button" class="btn-secondary" @click="open = false">取消</button>
      <button type="submit" form="basic-user-form" class="btn-primary" :disabled="submitting">{{ submitting ? '儲存中…' : '儲存' }}</button>
    </template>
  </BaseModal>
</template>
