<script setup lang="ts">
import { computed, reactive, watch } from 'vue'
import BaseModal from '@/components/BaseModal.vue'
import type { BasicGroupDetail, BasicGroupFormData, BasicPermissionItem } from '../types'

const props = defineProps<{
  /** null 為新增 */
  item: BasicGroupDetail | null
  basicPermissionOptions: BasicPermissionItem[]
  submitting: boolean
}>()
const emit = defineEmits<{ submit: [data: BasicGroupFormData] }>()
const open = defineModel<boolean>({ required: true })

const form = reactive<BasicGroupFormData>({ name: '', description: '', basicPermissionIds: [] })
const isProtected = computed(() => props.item?.isProtected ?? false)

/** 依模組分組；受保護群組或不可授予的權限只顯示文字，不渲染勾選框 */
const modules = computed(() => {
  const map = new Map<string, BasicPermissionItem[]>()
  for (const permission of props.basicPermissionOptions) {
    const list = map.get(permission.module) ?? []
    list.push(permission)
    map.set(permission.module, list)
  }
  return [...map.entries()].map(([name, permissions]) => ({
    name,
    editable: isProtected.value ? [] : permissions.filter((p) => p.isAssignable),
    readonly: permissions.filter((p) => (isProtected.value || !p.isAssignable) && form.basicPermissionIds.includes(p.id)),
  }))
})

// 開啟時重設表單
watch(open, (value) => {
  if (!value) return
  form.name = props.item?.name ?? ''
  form.description = props.item?.description ?? ''
  form.basicPermissionIds = [...(props.item?.basicPermissionIds ?? [])]
})

function submit() {
  emit('submit', { ...form, basicPermissionIds: [...form.basicPermissionIds] })
}
</script>

<template>
  <BaseModal v-model="open" :title="item ? '編輯群組' : '新增群組'" width="max-w-2xl">
    <form id="basic-group-form" class="space-y-4" @submit.prevent="submit">
      <div>
        <label for="basic-group-name" class="form-label">群組名稱</label>
        <input id="basic-group-name" v-model.trim="form.name" class="form-input" maxlength="50" required />
      </div>
      <div>
        <label for="basic-group-description" class="form-label">說明</label>
        <input id="basic-group-description" v-model.trim="form.description" class="form-input" maxlength="200" />
      </div>
      <div>
        <span class="form-label">權限</span>
        <p v-if="isProtected" class="mb-2 text-sm text-amber-700">系統管理員群組擁有全部權限，不可修改。</p>
        <div class="space-y-3">
          <div v-for="module in modules" :key="module.name" class="rounded-md border border-gray-200 p-3">
            <p class="mb-2 text-sm font-medium text-gray-800">{{ module.name }}</p>
            <div class="flex flex-wrap gap-x-4 gap-y-2">
              <label v-for="permission in module.editable" :key="permission.id" class="inline-flex items-center gap-2 text-sm">
                <input v-model="form.basicPermissionIds" type="checkbox" :value="permission.id" class="h-4 w-4 accent-primary-600" />
                {{ permission.description || permission.name }}
              </label>
            </div>
            <p v-if="module.readonly.length > 0" class="mt-2 text-sm text-gray-600">
              {{ isProtected ? '已擁有' : '不可變更' }}：{{ module.readonly.map((p) => p.description || p.name).join('、') }}
            </p>
          </div>
        </div>
      </div>
    </form>
    <template #footer>
      <button type="button" class="btn-secondary" @click="open = false">取消</button>
      <button type="submit" form="basic-group-form" class="btn-primary" :disabled="submitting">{{ submitting ? '儲存中…' : '儲存' }}</button>
    </template>
  </BaseModal>
</template>
