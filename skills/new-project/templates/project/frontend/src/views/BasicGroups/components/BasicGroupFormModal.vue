<script setup lang="ts">
import { computed, reactive, watch } from 'vue'
import BaseModal from '@/components/BaseModal.vue'
import type { BasicGroupDetail, BasicGroupFormData, BasicPermissionItem } from '../types'

const props = defineProps<{
  /** null 為新增 */
  item: BasicGroupDetail | null
  basicPermissions: BasicPermissionItem[]
  submitting: boolean
}>()

const emit = defineEmits<{
  submit: [form: BasicGroupFormData]
}>()

const open = defineModel<boolean>({ required: true })

const isEdit = computed(() => props.item !== null)
const isProtected = computed(() => props.item?.isProtected ?? false)

const form = reactive<BasicGroupFormData>({ name: '', description: '', basicPermissionIds: [] })

// 開啟時重設表單
watch(open, (value) => {
  if (!value) return
  form.name = props.item?.name ?? ''
  form.description = props.item?.description ?? ''
  form.basicPermissionIds = [...(props.item?.basicPermissionIds ?? [])]
})

/** 依模組分組顯示 */
const modules = computed(() => {
  const map = new Map<string, BasicPermissionItem[]>()
  for (const basicPermission of props.basicPermissions) {
    const list = map.get(basicPermission.module) ?? []
    list.push(basicPermission)
    map.set(basicPermission.module, list)
  }
  return [...map.entries()].map(([name, items]) => ({ name, items }))
})

/** 受保護群組的權限不可修改；只能授予自己擁有的權限（不可變更者不渲染勾選框，只以文字顯示現況） */
function isBasicPermissionEditable(basicPermission: BasicPermissionItem) {
  return !isProtected.value && basicPermission.grantable
}

function hasBasicPermission(basicPermission: BasicPermissionItem) {
  return form.basicPermissionIds.includes(basicPermission.id)
}

function submit() {
  emit('submit', { ...form, basicPermissionIds: [...form.basicPermissionIds] })
}
</script>

<template>
  <BaseModal v-model="open" :title="isEdit ? '編輯群組' : '新增群組'" width="max-w-2xl">
    <form id="basic-group-form" class="space-y-4" @submit.prevent="submit">
      <div>
        <label class="form-label" for="basic-group-name">群組名稱</label>
        <input id="basic-group-name" v-model="form.name" class="form-input" maxlength="50" />
      </div>
      <div>
        <label class="form-label" for="basic-group-description">說明</label>
        <textarea id="basic-group-description" v-model="form.description" class="form-input" rows="2" maxlength="200" />
      </div>
      <div>
        <span class="form-label">權限</span>
        <p v-if="isProtected" class="mb-2 text-xs text-amber-600">系統管理員群組擁有全部權限，不可修改</p>
        <div class="space-y-3">
          <fieldset v-for="mod in modules" :key="mod.name" class="rounded-md border border-gray-200 p-3">
            <legend class="px-1 text-sm font-medium text-gray-700">{{ mod.name }}</legend>
            <div class="grid gap-2 sm:grid-cols-4">
              <template v-for="basicPermission in mod.items" :key="basicPermission.id">
                <label
                  v-if="isBasicPermissionEditable(basicPermission)"
                  class="inline-flex items-center gap-2 text-sm text-gray-700"
                  :title="basicPermission.name"
                >
                  <input v-model="form.basicPermissionIds" type="checkbox" class="h-4 w-4" :value="basicPermission.id" />
                  {{ basicPermission.description || basicPermission.name }}
                </label>
                <!-- 不可變更：不渲染勾選框，已擁有的權限以文字顯示 -->
                <span
                  v-else-if="hasBasicPermission(basicPermission)"
                  class="inline-flex items-center gap-1 text-sm text-gray-500"
                  :title="basicPermission.name"
                >
                  ✓ {{ basicPermission.description || basicPermission.name }}
                </span>
              </template>
            </div>
          </fieldset>
        </div>
      </div>
    </form>

    <template #footer>
      <button type="button" class="btn-secondary" @click="open = false">取消</button>
      <button type="submit" form="basic-group-form" class="btn-primary" :disabled="submitting">
        {{ submitting ? '儲存中…' : '儲存' }}
      </button>
    </template>
  </BaseModal>
</template>
