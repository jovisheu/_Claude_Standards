<script setup lang="ts">
import { computed, reactive, watch } from 'vue'
import BaseModal from '@/components/BaseModal.vue'
import type { GroupDetail, GroupFormData, PermissionItem } from '../types'

const props = defineProps<{
  /** null 為新增 */
  item: GroupDetail | null
  permissions: PermissionItem[]
  submitting: boolean
}>()

const emit = defineEmits<{
  submit: [form: GroupFormData]
}>()

const open = defineModel<boolean>({ required: true })

const isEdit = computed(() => props.item !== null)
const isProtected = computed(() => props.item?.isProtected ?? false)

const form = reactive<GroupFormData>({ name: '', description: '', permissionIds: [] })

// 開啟時重設表單
watch(open, (value) => {
  if (!value) return
  form.name = props.item?.name ?? ''
  form.description = props.item?.description ?? ''
  form.permissionIds = [...(props.item?.permissionIds ?? [])]
})

/** 依模組分組顯示 */
const modules = computed(() => {
  const map = new Map<string, PermissionItem[]>()
  for (const permission of props.permissions) {
    const list = map.get(permission.module) ?? []
    list.push(permission)
    map.set(permission.module, list)
  }
  return [...map.entries()].map(([name, items]) => ({ name, items }))
})

/** 受保護群組的權限不可修改；只能授予自己擁有的權限 */
function isPermissionDisabled(permission: PermissionItem) {
  return isProtected.value || !permission.grantable
}

function submit() {
  emit('submit', { ...form, permissionIds: [...form.permissionIds] })
}
</script>

<template>
  <BaseModal v-model="open" :title="isEdit ? '編輯群組' : '新增群組'" width="max-w-2xl">
    <form id="group-form" class="space-y-4" @submit.prevent="submit">
      <div>
        <label class="form-label" for="group-name">群組名稱</label>
        <input id="group-name" v-model="form.name" class="form-input" maxlength="50" />
      </div>
      <div>
        <label class="form-label" for="group-description">說明</label>
        <textarea id="group-description" v-model="form.description" class="form-input" rows="2" maxlength="200" />
      </div>
      <div>
        <span class="form-label">權限</span>
        <p v-if="isProtected" class="mb-2 text-xs text-amber-600">系統管理員群組擁有全部權限，不可修改</p>
        <div class="space-y-3">
          <fieldset v-for="mod in modules" :key="mod.name" class="rounded-md border border-gray-200 p-3">
            <legend class="px-1 text-sm font-medium text-gray-700">{{ mod.name }}</legend>
            <div class="grid gap-2 sm:grid-cols-4">
              <label
                v-for="permission in mod.items"
                :key="permission.id"
                class="inline-flex items-center gap-2 text-sm"
                :class="isPermissionDisabled(permission) ? 'text-gray-400' : 'text-gray-700'"
                :title="permission.name"
              >
                <input
                  v-model="form.permissionIds"
                  type="checkbox"
                  class="h-4 w-4"
                  :value="permission.id"
                  :disabled="isPermissionDisabled(permission)"
                />
                {{ permission.description || permission.name }}
              </label>
            </div>
          </fieldset>
        </div>
      </div>
    </form>

    <template #footer>
      <button type="button" class="btn-secondary" @click="open = false">取消</button>
      <button type="submit" form="group-form" class="btn-primary" :disabled="submitting">
        {{ submitting ? '儲存中…' : '儲存' }}
      </button>
    </template>
  </BaseModal>
</template>
