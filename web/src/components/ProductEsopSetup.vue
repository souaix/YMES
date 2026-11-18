<script setup lang="ts">
import { computed, ref } from 'vue'

type EsopDraft = { opNo: string; subOpNo: string; positionNo: string; sopFile: string; pageNo: string; description: string }
type EsopRow = EsopDraft & { creator: string; createDate: string }
type Mode = 'add' | 'edit' | 'copy'
const props = defineProps<{
  productNo: string
  productVersion: string
  currentUser: string
  operationOptions: { opNo: string; opName: string }[]
  subOps: { opNo: string; subOpNo: string; subOpName: string }[]
  rows: EsopRow[]
  developmentPreview: boolean
  save: (rows: EsopRow[]) => boolean
}>()
const emit = defineEmits<{ close: [] }>()
const defaultOperations = [
  { opNo: 'DEV-OP-10', opName: '點膠作業' },
  { opNo: 'DEV-OP-20', opName: '組裝作業' },
  { opNo: 'DEV-OP-30', opName: '終檢作業' },
]
const operations = computed(() => [...new Map([...defaultOperations, ...props.operationOptions].map(row => [row.opNo, row])).values()])
const mode = ref<Mode | null>(null)
const configuredLookup = ref<'position' | 'sub-op'>('position')
const draftLookup = ref<'position' | 'sub-op'>('position')
const selected = ref<string[]>([])
const activeKey = ref('')
const error = ref('')
const notice = ref('')
const draft = ref<EsopDraft>({ opNo: '', subOpNo: 'N/A', positionNo: '', sopFile: '', pageNo: '', description: '' })
const baseline = ref('')
const dirty = computed(() => JSON.stringify(draft.value) !== baseline.value)
const active = computed(() => props.rows.find(row => identity(row) === activeKey.value))
const subOpOptions = computed(() => props.subOps.filter(row => row.opNo === draft.value.opNo))
const positionOptions = computed(() => draft.value.opNo ? [draft.value.opNo + '-POS-01', draft.value.opNo + '-POS-02'] : [])
const fileOptions = computed(() => !draft.value.opNo ? [] : [
  'SOP-' + draft.value.opNo + '-STD.pdf',
  'SOP-' + draft.value.opNo + '-QA.pdf',
])

function identity(row: EsopDraft): string {
  return JSON.stringify([row.opNo, row.subOpNo, row.positionNo])
}

function open(action: Mode): void {
  if (!props.developmentPreview || (action !== 'add' && !active.value)) return
  const source = action === 'add' ? undefined : active.value
  draftLookup.value = action === 'edit' && source
    ? (source.positionNo === 'N/A' ? 'sub-op' : 'position')
    : configuredLookup.value
  draft.value = source ? {
    opNo: source.opNo, subOpNo: source.subOpNo, positionNo: source.positionNo,
    sopFile: source.sopFile, pageNo: source.pageNo, description: source.description,
  } : { opNo: '', subOpNo: 'N/A', positionNo: '', sopFile: '', pageNo: '', description: '' }
  if (action !== 'edit') {
    if (draftLookup.value === 'position') {
      draft.value.subOpNo = 'N/A'
      if (draft.value.positionNo === 'N/A') draft.value.positionNo = ''
    } else {
      draft.value.positionNo = 'N/A'
    }
  }
  mode.value = action
  baseline.value = JSON.stringify(draft.value)
  notice.value = ''
  error.value = ''
}

function changeOperation(): void {
  draft.value.subOpNo = 'N/A'
  draft.value.positionNo = draftLookup.value === 'position' ? '' : 'N/A'
  draft.value.sopFile = ''
}

function chooseFile(event: Event): void {
  const input = event.target as HTMLInputElement
  if (input.files?.[0]) draft.value.sopFile = input.files[0].name
}

function save(closeAfter: boolean): void {
  if (!props.developmentPreview || !mode.value) return
  const row = { ...draft.value, pageNo: draft.value.pageNo.trim().toUpperCase() || '1', description: draft.value.description.trim() }
  if (!row.opNo || !row.subOpNo || !row.positionNo || !row.sopFile.trim() || row.sopFile.length > 255) {
    error.value = '請選擇 OPNo、SubOPNo／PositionNo 和 SOPFile（最多 255 字）。'
    return
  }
  const key = identity(row)
  const old = props.rows.find(item => identity(item) === key)
  if (mode.value !== 'edit' && old) {
    error.value = '此 OPNo、SubOPNo、PositionNo 的組合已存在。'
    return
  }
  if (mode.value === 'edit' && !old) {
    error.value = '找不到要更新的 eSOP 設定。'
    return
  }
  const saved = { ...row, creator: old?.creator ?? props.currentUser, createDate: old?.createDate ?? new Date().toISOString() }
  if (!props.save([...props.rows.filter(item => identity(item) !== key), saved])) {
    error.value = 'eSOP 假資料儲存失敗。'
    return
  }
  draft.value = row
  baseline.value = JSON.stringify(row)
  activeKey.value = key
  selected.value = [key]
  error.value = ''
  notice.value = '已儲存 eSOP 假資料。'
  if (closeAfter) mode.value = null
}

function remove(): void {
  const keys = selected.value.length ? selected.value : [activeKey.value]
  if (!props.developmentPreview || !keys[0] || !window.confirm('確定刪除選取的 eSOP 設定？')) return
  if (!props.save(props.rows.filter(row => !keys.includes(identity(row))))) {
    error.value = 'eSOP 假資料儲存失敗。'
    return
  }
  activeKey.value = ''
  selected.value = []
  error.value = ''
  notice.value = '已刪除選取的 eSOP 設定。'
}
</script>

<template>
  <section class="esop-window" role="dialog" aria-modal="true" aria-labelledby="esop-title">
    <header class="esop-bar">
      <h2 id="esop-title">PRD eSOP Basis</h2><small v-if="developmentPreview">DEV 假資料</small>
      <button class="esop-close" type="button" aria-label="關閉 eSOP" @click="emit('close')">×</button>
    </header>
    <div class="esop-body">
      <p>ProductNo <strong>{{ productNo }}</strong>　ProductVersion <strong>{{ productVersion }}</strong></p>
      <p v-if="!developmentPreview">正式 eSOP API 尚未接線。</p>
      <template v-else>
        <div class="esop-bar esop-actions">
          <button type="button" :disabled="!!mode" @click="open('add')">Add</button>
          <button type="button" :disabled="!active || !!mode" @click="open('edit')">Edit</button>
          <button type="button" :disabled="(!active && !selected.length) || !!mode" @click="remove">Delete</button>
          <button type="button" :disabled="!active || !!mode" @click="open('copy')">Copy</button>
          <label class="esop-close">Execute_eSOP <select v-model="configuredLookup" :disabled="!!mode"><option value="position">1 — Position／文件目錄</option><option value="sub-op">0 — Sub OP／本機文件</option></select></label>
        </div>
        <div class="esop-table"><table>
          <thead><tr><th>選取</th><th>OPNo</th><th>SubOPNo</th><th>PositionNo</th><th>SOPFile</th><th>PageNo</th><th>Description</th><th>Creator</th><th>CreateDate</th></tr></thead>
          <tbody>
            <tr v-for="row in rows" :key="identity(row)" :class="{ focused: activeKey === identity(row) }" @click="activeKey = identity(row)">
              <td><input v-model="selected" type="checkbox" :value="identity(row)" :aria-label="'選取 ' + row.opNo + ' ' + row.subOpNo + ' ' + row.positionNo" @focus="activeKey = identity(row)"></td>
              <td>{{ row.opNo }}</td><td>{{ row.subOpNo }}</td><td>{{ row.positionNo }}</td><td>{{ row.sopFile }}</td><td>{{ row.pageNo }}</td>
              <td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ new Date(row.createDate).toLocaleString('sv-SE') }}</td>
            </tr>
            <tr v-if="!rows.length"><td colspan="9">此產品版次尚未設定 eSOP。</td></tr>
          </tbody>
        </table></div>
        <form v-if="mode" class="esop-editor" @submit.prevent="save(true)">
          <h3>{{ { add: 'Add', edit: 'Edit', copy: 'Copy' }[mode] }} eSOP Basis</h3>
          <div class="esop-fields">
            <label>OPNo <select v-model="draft.opNo" :disabled="mode === 'edit'" @change="changeOperation">
              <option value="">請選擇作業站</option>
              <option v-for="row in operations" :key="row.opNo" :value="row.opNo">{{ row.opNo }} {{ row.opName }}</option>
            </select></label>
            <label>SubOPNo <select v-model="draft.subOpNo" :disabled="draftLookup === 'position' || mode === 'edit' || !draft.opNo">
              <option>N/A</option>
              <option v-if="draft.subOpNo !== 'N/A' && !subOpOptions.some(row => row.subOpNo === draft.subOpNo)">{{ draft.subOpNo }}</option>
              <option v-for="row in subOpOptions" :key="row.subOpNo" :value="row.subOpNo">{{ row.subOpNo }} {{ row.subOpName }}</option>
            </select></label>
            <label>PositionNo <select v-model="draft.positionNo" :disabled="draftLookup === 'sub-op' || mode === 'edit' || !draft.opNo">
              <option value="">請選擇 Position</option><option v-if="draft.positionNo === 'N/A'">N/A</option>
              <option v-for="option in positionOptions" :key="option">{{ option }}</option>
            </select></label>
            <label v-if="mode === 'edit' || draftLookup === 'sub-op'">SOPFile <input :value="draft.sopFile" readonly></label>
            <label v-else>SOPFile <select v-model="draft.sopFile" :disabled="!draft.opNo">
              <option value="">請選擇 DEV 文件</option>
              <option v-if="draft.sopFile && !fileOptions.includes(draft.sopFile)">{{ draft.sopFile }}</option>
              <option v-for="option in fileOptions" :key="option">{{ option }}</option>
            </select></label>
            <label v-if="mode !== 'edit' && draftLookup === 'sub-op'">選擇文件（僅記錄檔名）<input type="file" :disabled="!draft.opNo" @change="chooseFile"></label>
            <label>PageNo <input v-model="draft.pageNo" maxlength="20" placeholder="1"></label>
            <label>Description <input v-model="draft.description" maxlength="255"></label>
          </div>
          <div class="esop-bar esop-actions">
            <button type="button" :disabled="!dirty" @click="save(false)">Apply</button>
            <button type="submit" :disabled="!dirty">Confirm</button>
            <button type="button" @click="mode = null; error = ''">Close</button>
          </div>
        </form>
      </template>
      <p v-if="error" class="esop-error" role="alert">{{ error }}</p><p v-else-if="notice" role="status">{{ notice }}</p>
    </div>
    <footer class="esop-bar"><button class="esop-close" type="button" @click="emit('close')">Close</button></footer>
  </section>
</template>

<style scoped>
.esop-window { width: min(1050px, 96vw); max-height: 92vh; overflow: auto; background: #f7f5ee; color: var(--ink, #26251f); border: 1px solid #a9a69d; box-shadow: 0 8px 30px #0004; }
.esop-bar { display: flex; align-items: center; flex-wrap: wrap; gap: 7px 10px; padding: 8px 12px; border-block: 1px solid var(--line, #e5e1d5); }
.esop-close { margin-left: auto; }
.esop-bar h2, .esop-editor h3 { margin: 0; font-size: 15px; }
.esop-body { padding: 8px 12px; }
.esop-body p { margin: 5px 0 8px; }
.esop-actions { border: 0; padding: 0; }
.esop-window button { min-height: 26px; border: 1px solid #b7ae90; background: #fff; padding: 3px 8px; color: inherit; font: inherit; cursor: pointer; }
.esop-window button:disabled { opacity: .5; cursor: default; }
.esop-window input:not([type="checkbox"], [type="file"]), .esop-window select { min-width: 0; border: 1px solid #b7ae90; padding: 4px; background: #fff2bd; color: inherit; }
.esop-window input:read-only:not([type="checkbox"], [type="file"]), .esop-window select:disabled { background: #eeece4; }
.esop-table { overflow: auto; max-height: 38vh; margin: 7px 0; background: white; }
.esop-table table { width: 100%; border-collapse: collapse; white-space: nowrap; font-size: 12px; }
.esop-table th, .esop-table td { padding: 5px; border: 1px solid var(--line, #e5e1d5); text-align: left; }
.esop-table th { position: sticky; top: 0; background: #eeece4; }
.esop-table tr.focused { background: #fff2bd; }
.esop-editor { padding: 8px; border: 1px solid var(--line, #e5e1d5); }
.esop-fields { display: grid; grid-template-columns: repeat(auto-fit, minmax(190px, 1fr)); gap: 7px 10px; margin: 8px 0; }
.esop-fields label { display: grid; gap: 4px; }
.esop-error { color: #941b18; }
</style>
