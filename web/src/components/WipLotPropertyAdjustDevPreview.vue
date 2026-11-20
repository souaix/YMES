<script setup lang="ts">
import { computed, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
const props = defineProps<{ developmentPreview: boolean }>()
defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview)
const fixtures = dev.value ? [
  { no: 'DEV-PROP001', product: 'DEV-IC/A', components: ['DEV-C01', 'DEV-C02'] },
  { no: 'DEV-PROP002', product: 'DEV-PKG/B', components: [] },
] : []
const definitions = { lot: { LOT_GRADE: '批級別', TRACE_TAG: '追蹤標記' }, component: { PART_GRADE: '元件級別', TEST_TAG: '測試標記' } }
interface Row { lot: string; component: string; property: string; value: string }
const rows = ref<Row[]>(dev.value ? [
  { lot: 'DEV-PROP001', component: '', property: 'LOT_GRADE', value: 'A' },
  { lot: 'DEV-PROP001', component: 'DEV-C01', property: 'PART_GRADE', value: 'B' },
] : [])
const input = ref('')
const lotNo = ref('')
const member = ref('')
const lot = computed(() => fixtures.find(item => item.no === lotNo.value))
const lotRows = computed(() => rows.value.filter(row => row.lot === lotNo.value && !row.component))
const memberRows = computed(() => rows.value.filter(row => row.lot === lotNo.value && !!member.value && row.component === member.value))
const draft = ref<{ operation: 'add' | 'edit' | 'delete'; data: Row; oldValue: string; reason: string }>()
const items = computed(() => Object.entries(draft.value?.data.component ? definitions.component : definitions.lot))
const error = ref('')
const result = ref('')
const confirmation = ref<string | null>(null)
const actions = { add: '新增', edit: '修改', delete: '刪除' }
function search(): void {
  lotNo.value = input.value.trim().toUpperCase()
  member.value = result.value = ''
  draft.value = undefined
  error.value = lot.value ? '' : '查無生產批。'
}
function begin(operation: 'add' | 'edit' | 'delete', row: Row): void {
  draft.value = { operation, data: { ...row }, oldValue: row.value, reason: '' }
  error.value = result.value = ''
}
function add(component: string): void {
  begin('add', { lot: lotNo.value, component, property: '', value: '' })
}
function sameKey(a: Row, b: Row): boolean {
  return a.lot === b.lot && a.component === b.component && a.property === b.property
}
function confirm(): void {
  const edit = draft.value
  if (!edit) return
  error.value = ''
  const current = rows.value.find(row => sameKey(row, edit.data))
  if (!edit.reason.trim()) error.value = '請填寫調整原因。'
  else if (!edit.data.property || (edit.operation === 'add' && !edit.data.value.trim())) error.value = '請選擇屬性並填寫新增值。'
  else if (edit.operation === 'add' && current) error.value = '此屬性已存在。'
  else if (edit.operation !== 'add' && current?.value !== edit.oldValue) error.value = '舊值已變更，請重新查詢。'
  if (!error.value) confirmation.value = `確認${actions[edit.operation]} ${edit.data.property}？`
}
function resolve(accepted: boolean): void {
  confirmation.value = null
  const edit = draft.value
  if (!accepted || !edit) return
  if (edit.operation === 'add') rows.value.push(edit.data)
  if (edit.operation === 'edit') rows.value = rows.value.map(row => sameKey(row, edit.data) ? edit.data : row)
  if (edit.operation === 'delete') rows.value = rows.value.filter(row => !sameKey(row, edit.data))
  result.value = `DEV 已${actions[edit.operation]} ${edit.data.property}。`
  draft.value = undefined
}
</script>

<template>
  <section class="property-page" aria-label="生產批屬性調整">
    <template v-if="dev">
      <form v-if="!draft" class="toolbar" @submit.prevent="search">
        <label>生產批號 <input v-model="input" list="property-lots" maxlength="50"></label>
        <datalist id="property-lots"><option v-for="item in fixtures" :key="item.no" :value="item.no"></option></datalist>
        <button type="submit" class="query-button">查詢</button><button type="button" @click="$emit('close')">關閉</button>
      </form>
      <template v-if="lot && !draft">
        <p>{{ lot.no }} · {{ lot.product }}</p>
        <div v-for="group in [{ label: '批屬性', component: '', data: lotRows }, { label: '元件屬性', component: member, data: memberRows }]" :key="group.label" class="group">
          <div class="toolbar">
            <strong>{{ group.label }}</strong>
            <label v-if="group.label === '元件屬性'">元件 <select v-model="member"><option value="">請選擇</option><option v-for="item in lot.components" :key="item">{{ item }}</option></select></label>
            <button type="button" :disabled="group.label === '元件屬性' && !member" @click="add(group.component)">新增</button>
          </div>
          <div class="grid"><table :aria-label="group.label">
            <thead><tr><th scope="col">屬性</th><th scope="col">值</th><th scope="col">操作</th></tr></thead>
            <tbody>
              <tr v-for="row in group.data" :key="row.property"><td>{{ row.property }}</td><td>{{ row.value }}</td><td><button type="button" @click="begin('edit', row)">修改</button> <button type="button" @click="begin('delete', row)">刪除</button></td></tr>
              <tr v-if="!group.data.length"><td colspan="3">無屬性記錄。</td></tr>
            </tbody>
          </table></div>
        </div>
      </template>
      <form v-if="draft" class="group" @submit.prevent="confirm">
        <h2>{{ actions[draft.operation] }}{{ draft.data.component ? '元件' : '批' }}屬性</h2>
        <div class="toolbar">
          <label>屬性 <select v-model="draft.data.property" :disabled="draft.operation !== 'add'"><option value="">請選擇</option><option v-for="[key, label] in items" :key="key" :value="key">{{ key }} {{ label }}</option></select></label>
          <label>屬性值 <input v-model="draft.data.value" :readonly="draft.operation === 'delete'" maxlength="255"></label>
          <label>調整原因 <input v-model="draft.reason" maxlength="255"></label>
        </div>
        <div class="toolbar"><button type="submit" class="primary">確定</button><button type="button" @click="draft = undefined; error = ''">取消</button></div>
      </form>
      <p v-if="error" role="alert">{{ error }}</p><p v-if="result" role="status">{{ result }}</p>
      <ActionConfirmationDialog :message="confirmation" @resolve="resolve" />
    </template>
    <template v-else>
      <p role="status">正式環境尚未開放此功能。</p><button type="button" @click="$emit('close')">關閉</button>
    </template>
  </section>
</template>

<style scoped>
.property-page, .group { display: grid; align-content: start; gap: 12px; padding: 16px; color: var(--ink); }
.group { background: white; border: 1px solid var(--line); }
.toolbar, label { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.grid { overflow: auto; border: 1px solid var(--line); }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
td, th { padding: 6px 9px; text-align: left; white-space: nowrap; border-bottom: 1px solid var(--line); }
p, h2 { margin: 0; }
[role='alert'] { color: #9d2727; }
</style>
