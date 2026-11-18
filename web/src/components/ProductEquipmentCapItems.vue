<script setup lang="ts">
import { computed, ref } from 'vue'

type CapRow = {
  opNo: string
  capItem: string
  capValue: string
  description: string
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
  dataStamp: number
}
type Draft = Pick<CapRow, 'opNo' | 'capItem' | 'capValue' | 'description'>
type Mode = 'add' | 'edit' | 'copy'
const props = defineProps<{
  productNo: string
  productVersion: string
  issueState: number
  currentUser: string
  rows: CapRow[]
  developmentPreview: boolean
  save: (rows: CapRow[]) => boolean
}>()
const emit = defineEmits<{ close: [] }>()
const operations = [
  { no: 'DEV-OP-10', ps: 'DEV-PS-10', name: '點膠作業' },
  { no: 'DEV-OP-20', ps: 'DEV-PS-10', name: '組裝作業' },
  { no: 'DEV-OP-30', ps: 'DEV-PS-20', name: '終檢作業' },
]
const capOptions = [
  { no: 'DEV-CAP-DISPENSER', name: '點膠設備能力' },
  { no: 'DEV-CAP-VISION', name: '影像檢查能力' },
  { no: 'DEV-CAP-TORQUE', name: '鎖附扭力能力' },
]
const mode = ref<Mode | null>(null)
const draft = ref<Draft>({ opNo: '', capItem: '', capValue: '', description: '' })
const baseline = ref('')
const activeKey = ref('')
const selection = ref<string[]>([])
const error = ref('')
const notice = ref('')
const canEdit = computed(() => props.developmentPreview && props.issueState === 0)
const active = computed(() => props.rows.find(row => keyOf(row) === activeKey.value))
const displayRows = computed(() => [...props.rows].sort((a, b) => a.opNo.localeCompare(b.opNo) || a.capItem.localeCompare(b.capItem)))
const dirty = computed(() => JSON.stringify(draft.value) !== baseline.value)

function keyOf(row: Pick<CapRow, 'opNo' | 'capItem'>): string {
  return JSON.stringify([row.opNo, row.capItem])
}

function select(key: string, checked: boolean): void {
  activeKey.value = key
  selection.value = checked ? [...new Set([...selection.value, key])] : selection.value.filter(item => item !== key)
}

function open(action: Mode): void {
  if (!canEdit.value || (action !== 'add' && !active.value)) return
  const source = action === 'add' ? undefined : active.value
  draft.value = {
    opNo: source?.opNo ?? '', capItem: source?.capItem ?? '', capValue: source?.capValue ?? '', description: source?.description ?? '',
  }
  mode.value = action
  baseline.value = JSON.stringify(draft.value)
  error.value = ''
  notice.value = ''
}

function save(closeAfter: boolean): void {
  if (!canEdit.value || !mode.value) return
  const normalized = { ...draft.value, capValue: draft.value.capValue.trim().toUpperCase(), description: draft.value.description.trim() }
  if (!normalized.opNo || !normalized.capItem || !normalized.capValue || normalized.capValue.length > 20) {
    error.value = '請選擇 OPNo、CapItem，並輸入 1–20 字的 CapValue。'
    return
  }
  const key = keyOf(normalized)
  const old = props.rows.find(row => keyOf(row) === key)
  if (mode.value !== 'edit' && old) {
    error.value = '這個作業站已設定相同 CapItem。'
    return
  }
  if (mode.value === 'edit' && !old) {
    error.value = '找不到要更新的 CapItem。'
    return
  }
  const now = new Date().toISOString()
  const row = old ? {
    ...old, ...normalized, reviser: props.currentUser, reviseDate: now, dataStamp: old.dataStamp + 1,
  } : {
    ...normalized, creator: props.currentUser, createDate: now, reviser: '', reviseDate: '', dataStamp: 0,
  }
  if (!props.save([...props.rows.filter(item => keyOf(item) !== key), row])) {
    error.value = '設備能力假資料儲存失敗。'
    return
  }
  activeKey.value = key
  selection.value = [key]
  draft.value = normalized
  baseline.value = JSON.stringify(draft.value)
  error.value = ''
  notice.value = '已儲存設備能力假資料。'
  if (closeAfter) mode.value = null
}

function removeSelected(): void {
  const keys = selection.value.length ? selection.value : [activeKey.value]
  if (!canEdit.value || !keys[0] || !window.confirm('確定刪除選取的設備能力項目？')) return
  if (!props.save(props.rows.filter(row => !keys.includes(keyOf(row))))) {
    error.value = '設備能力假資料儲存失敗。'
    return
  }
  selection.value = []
  activeKey.value = ''
  error.value = ''
  notice.value = '已刪除選取的項目。'
}

function dateText(value: string): string {
  return value ? new Date(value).toLocaleString('sv-SE') : ''
}
</script>

<template>
  <section class="cap-window" role="dialog" aria-modal="true" aria-labelledby="cap-title">
    <header class="cap-bar">
      <h2 id="cap-title">PRD OP CapItem</h2>
      <small v-if="developmentPreview">DEV 假資料</small>
      <button class="cap-close" type="button" aria-label="關閉設備能力項目" @click="emit('close')">×</button>
    </header>
    <div class="cap-body">
      <p>ProductNo <strong>{{ productNo }}</strong>　ProductVersion <strong>{{ productVersion }}</strong></p>
      <p v-if="!developmentPreview">正式 EQPCapItem API 尚未接線。</p>
      <template v-else>
        <div class="cap-actions">
          <button type="button" :disabled="!canEdit || !!mode" @click="open('add')">Add</button>
          <button type="button" :disabled="!canEdit || !active || !!mode" @click="open('edit')">Edit</button>
          <button type="button" :disabled="!canEdit || (!active && !selection.length) || !!mode" @click="removeSelected">Delete</button>
          <button type="button" :disabled="!canEdit || !active || !!mode" @click="open('copy')">Copy</button>
        </div>
        <div class="cap-table"><table>
          <thead><tr><th>選取</th><th>OPNo</th><th>CapItem</th><th>CapValue</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
          <tbody>
            <tr v-for="row in displayRows" :key="keyOf(row)" :class="{ focused: activeKey === keyOf(row) }" @click="activeKey = keyOf(row)">
              <td><input type="checkbox" :aria-label="'選取 ' + row.opNo + ' ' + row.capItem" :checked="selection.includes(keyOf(row))"
                @change="select(keyOf(row), ($event.target as HTMLInputElement).checked)"></td>
              <td>{{ row.opNo }}</td><td>{{ row.capItem }}</td><td>{{ row.capValue }}</td><td>{{ row.description }}</td>
              <td>{{ row.creator }}</td><td>{{ dateText(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ dateText(row.reviseDate) }}</td>
            </tr>
            <tr v-if="!displayRows.length"><td colspan="9">此產品版次尚未設定設備能力項目。</td></tr>
          </tbody>
        </table></div>
        <form v-if="mode" class="cap-editor" @submit.prevent="save(true)">
          <h3>{{ { add: 'Add', edit: 'Edit', copy: 'Copy' }[mode] }} PRD OP CapItem</h3>
          <div class="cap-fields">
            <label>OPNo <select v-model="draft.opNo" :disabled="mode === 'edit'">
              <option value="">請選擇作業站</option>
              <option v-if="draft.opNo && !operations.some(row => row.no === draft.opNo)" :value="draft.opNo">{{ draft.opNo }}</option>
              <option v-for="row in operations" :key="row.no" :value="row.no">{{ row.ps }} / {{ row.no }} {{ row.name }}</option>
            </select></label>
            <label>CapItem <select v-model="draft.capItem" :disabled="mode === 'edit'">
              <option value="">請選擇能力項目</option>
              <option v-if="draft.capItem && !capOptions.some(row => row.no === draft.capItem)" :value="draft.capItem">{{ draft.capItem }}</option>
              <option v-for="row in capOptions" :key="row.no" :value="row.no">{{ row.no }} {{ row.name }}</option>
            </select></label>
            <label>CapValue <input v-model="draft.capValue" maxlength="20" @blur="draft.capValue = draft.capValue.trim().toUpperCase()"></label>
            <label>Description <input v-model="draft.description" maxlength="255"></label>
          </div>
          <div class="cap-actions">
            <button type="button" :disabled="!dirty" @click="save(false)">Apply</button>
            <button type="submit" :disabled="!dirty">Confirm</button>
            <button type="button" @click="mode = null; error = ''">Close</button>
          </div>
        </form>
      </template>
      <p v-if="error" class="cap-error" role="alert">{{ error }}</p>
      <p v-else-if="notice" role="status">{{ notice }}</p>
    </div>
    <footer class="cap-bar"><button class="cap-close" type="button" @click="emit('close')">Close</button></footer>
  </section>
</template>

<style scoped>
.cap-window { width: min(990px, 96vw); max-height: 92vh; overflow: auto; background: #f7f5ee; color: var(--ink, #26251f); border: 1px solid #a9a69d; box-shadow: 0 8px 30px #0004; }
.cap-bar, .cap-actions { display: flex; align-items: center; flex-wrap: wrap; gap: 8px 12px; }
.cap-bar, .cap-body { padding: 8px 12px; }
.cap-bar { border-block: 1px solid var(--line, #e5e1d5); }
.cap-close { margin-left: auto; }
.cap-bar h2, .cap-editor h3 { margin: 0; font-size: 15px; }
.cap-body p { margin: 5px 0 10px; }
.cap-window button { min-height: 26px; border: 1px solid #b7ae90; background: #fff; padding: 3px 8px; color: inherit; font: inherit; cursor: pointer; }
.cap-window button:disabled { opacity: .5; cursor: default; }
.cap-table { overflow: auto; max-height: 40vh; margin: 7px 0; background: white; }
.cap-table table { width: 100%; border-collapse: collapse; white-space: nowrap; font-size: 12px; }
.cap-table th, .cap-table td { padding: 5px; border: 1px solid var(--line, #e5e1d5); text-align: left; }
.cap-table th { position: sticky; top: 0; background: #eeece4; }
.cap-table tr.focused { background: #fff2bd; }
.cap-editor { padding: 8px; border: 1px solid var(--line, #e5e1d5); }
.cap-fields { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 6px 10px; margin: 8px 0; }
.cap-fields label { display: grid; gap: 4px; }
.cap-fields input, .cap-fields select { min-width: 0; border: 1px solid #b7ae90; padding: 4px; background: #fff2bd; color: inherit; }
.cap-error { color: #941b18; }
</style>
