<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type Row = Record<string, any>
type Mode = 'add' | 'edit'
type LookupKind = '' | 'process' | 'operation'
type OpOption = { opNo: string; opName: string; description: string; processNo: string; processVersion: string }

const window = globalThis.window
const props = defineProps<{
  product: Row
  processes: Row[]
  opDescriptions: Row[] | null
  developmentPreview: boolean
  canEdit: boolean
}>()
const emit = defineEmits<{ save: [rows: Row[]]; close: [] }>()

const working = ref<Row[]>([])
const selectedKeys = ref<string[]>([])
const activeKey = ref('')
const definitionOpen = ref(false)
const definitionMode = ref<Mode>('add')
const definitionSaved = ref(false)
const lookupKind = ref<LookupKind>('')
const draft = ref<Row>({})
const dirty = ref(false)
const error = ref('')
const notice = ref('')

const operationCatalog: OpOption[] = [
  { opNo: 'DEV-OP-10', opName: '點膠作業', description: '點膠並確認膠量。', processNo: 'DEV-PROC-01', processVersion: '01' },
  { opNo: 'DEV-OP-20', opName: '組裝作業', description: '執行組裝及外觀確認。', processNo: 'DEV-PROC-01', processVersion: '01' },
  { opNo: 'DEV-OP-30', opName: '固化作業', description: '依產品條件完成固化。', processNo: 'DEV-PROC-02', processVersion: '03' },
  { opNo: 'DEV-OP-40', opName: '終檢作業', description: '記錄終檢結果。', processNo: 'DEV-PROC-02', processVersion: '03' },
]

const readOnly = computed(() => !props.developmentPreview || !props.canEdit)
const productNo = computed(() => String(props.product.productNo ?? props.product.ProductNo ?? ''))
const productVersion = computed(() => String(props.product.productVersion ?? props.product.ProductVersion ?? ''))
const processOptions = computed(() => props.processes.map((row: Row) => ({
  processNo: String(row.processNo ?? row.ProcessNo ?? ''),
  processVersion: String(row.processVersion ?? row.ProcessVersion ?? ''),
  psNo: String(row.psNo ?? row.PSNo ?? ''),
})).filter(row => row.processNo))
const chosenProcess = computed(() => processOptions.value.find(row => row.processNo === String(draft.value.processNo ?? '') && row.processVersion === String(draft.value.processVersion ?? '')) ?? null)
const operationOptions = computed(() => operationCatalog.filter(item =>
  item.processNo === String(draft.value.processNo ?? '') &&
  item.processVersion === String(draft.value.processVersion ?? '') &&
  !working.value.some(row => String(row.opNo ?? row.OPNo) === item.opNo),
))
const lookupProcessOptions = computed(() => processOptions.value)
const displayRows = computed(() => working.value.map(row => ({
  ...row,
  key: String(row.opNo ?? row.OPNo ?? ''),
  opNo: String(row.opNo ?? row.OPNo ?? ''),
  description: String(row.description ?? row.Description ?? ''),
})))
const selectedRows = computed(() => displayRows.value.filter(row => selectedKeys.value.includes(row.key)))
const canEditRow = computed(() => !readOnly.value && Boolean(activeKey.value))
const canSubmit = computed(() => !readOnly.value && Boolean(draft.value.opNo) && String(draft.value.description ?? '').length <= 255 && (definitionMode.value === 'add' || dirty.value))

watch(() => props.opDescriptions, value => {
  working.value = clone(Array.isArray(value) ? value : [])
  if (!working.value.some(row => String(row.opNo ?? row.OPNo) === activeKey.value)) activeKey.value = working.value[0] ? String(working.value[0].opNo ?? working.value[0].OPNo ?? '') : ''
  selectedKeys.value = selectedKeys.value.filter(key => working.value.some(row => String(row.opNo ?? row.OPNo) === key))
}, { immediate: true, deep: true })

function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function userNo(): string { return 'DEV001' }
function keyOf(row: Row): string { return String(row.opNo ?? row.OPNo ?? '') }
function formatDate(value: unknown): string {
  if (!value) return ''
  const date = new Date(String(value))
  if (Number.isNaN(date.getTime())) return String(value).replace('T', ' ').replaceAll('-', '/').slice(0, 19)
  const pad = (number: number) => String(number).padStart(2, '0')
  return `${date.getFullYear()}/${pad(date.getMonth() + 1)}/${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
}
function persist(): void {
  const value = clone(working.value)
  emit('save', value)
  notice.value = 'DEV 產品 OP 說明假資料已儲存。'
}
function selectRow(row: Row, event: MouseEvent): void {
  const key = row.key
  activeKey.value = key
  if (event.ctrlKey || event.metaKey) {
    selectedKeys.value = selectedKeys.value.includes(key) ? selectedKeys.value.filter(item => item !== key) : [...selectedKeys.value, key]
  } else if (event.shiftKey && selectedKeys.value.length) {
    const keys = displayRows.value.map(item => item.key)
    const from = keys.indexOf(selectedKeys.value[selectedKeys.value.length - 1])
    const to = keys.indexOf(key)
    selectedKeys.value = keys.slice(Math.min(from, to), Math.max(from, to) + 1)
  } else selectedKeys.value = [key]
}
function openAdd(): void {
  error.value = ''
  if (!processOptions.value.length) { error.value = '請先為產品設定流程，或確認 PS 權限。'; return }
  const first = processOptions.value[0]
  definitionMode.value = 'add'
  definitionSaved.value = false
  draft.value = { processNo: first.processNo, processVersion: first.processVersion, opNo: '', opName: '', description: '', dataStamp: 0 }
  dirty.value = false
  definitionOpen.value = true
}
function openEdit(): void {
  error.value = ''
  const row = displayRows.value.find(item => item.key === activeKey.value)
  if (!row || readOnly.value) return
  definitionMode.value = 'edit'
  definitionSaved.value = true
  const process = processOptions.value.find(item => operationCatalog.some(op => op.opNo === row.opNo && op.processNo === item.processNo))
  draft.value = { ...clone(row), processNo: process?.processNo ?? '', processVersion: process?.processVersion ?? '', opName: row.opName ?? '' }
  dirty.value = false
  definitionOpen.value = true
}
function showLookup(kind: LookupKind): void {
  if (readOnly.value || definitionMode.value !== 'add') return
  if (kind === 'operation' && !chosenProcess.value) { error.value = '請先選擇 Process No。'; return }
  lookupKind.value = kind
}
function chooseProcess(row: Row): void {
  draft.value = { ...draft.value, processNo: row.processNo, processVersion: row.processVersion, opNo: '', opName: '', description: '' }
  dirty.value = true
  error.value = ''
  lookupKind.value = ''
}
function chooseOperation(row: OpOption): void {
  draft.value = { ...draft.value, opNo: row.opNo, opName: row.opName, description: row.description }
  dirty.value = true
  error.value = ''
  lookupKind.value = ''
}
function saveDefinition(closeAfter: boolean): void {
  if (readOnly.value) return
  if (!String(draft.value.opNo ?? '').trim()) { error.value = '請選擇 OP No。'; return }
  if (String(draft.value.description ?? '').length > 255) { error.value = 'Description 最多 255 個字元。'; return }
  const opNo = String(draft.value.opNo)
  const existing = working.value.find(row => keyOf(row) === opNo)
  if (definitionMode.value === 'add' && existing) { error.value = '此產品版本已設定這個 OP No。'; return }
  const now = new Date().toISOString()
  if (definitionMode.value === 'add') {
    const row = { productNo: productNo.value, productVersion: productVersion.value, opNo, opName: String(draft.value.opName ?? ''), description: String(draft.value.description ?? ''), dataStamp: 0, creator: userNo(), createDate: now, reviser: '', reviseDate: '' }
    working.value = [...working.value, row]
    activeKey.value = opNo
    selectedKeys.value = [opNo]
    definitionMode.value = 'edit'
    definitionSaved.value = true
    draft.value = { ...draft.value, dataStamp: 0, creator: row.creator, createDate: row.createDate, reviser: row.reviser, reviseDate: row.reviseDate }
  } else {
    working.value = working.value.map(row => keyOf(row) === opNo ? { ...row, description: String(draft.value.description ?? ''), dataStamp: Number(row.dataStamp ?? row.DataStamp ?? 0) + 1, reviser: userNo(), reviseDate: now } : row)
    draft.value = { ...draft.value, dataStamp: Number(draft.value.dataStamp ?? 0) + 1, reviser: userNo(), reviseDate: now }
  }
  dirty.value = false
  persist()
  error.value = ''
  if (closeAfter) definitionOpen.value = false
}
function confirmDefinition(): void {
  if (definitionSaved.value && definitionMode.value === 'edit' && !dirty.value) {
    closeDefinition()
    return
  }
  saveDefinition(true)
}
function deleteSelected(): void {
  if (readOnly.value) return
  const keys = selectedRows.value.length ? selectedRows.value.map(row => row.key) : activeKey.value ? [activeKey.value] : []
  if (!keys.length) { error.value = '請先選取資料。'; return }
  if (!window.confirm(`確定刪除選取的 ${keys.length} 筆 OP 說明？`)) return
  working.value = working.value.filter(row => !keys.includes(keyOf(row)))
  selectedKeys.value = []
  activeKey.value = working.value[0] ? keyOf(working.value[0]) : ''
  persist()
  error.value = ''
}
function closeDefinition(): void {
  definitionOpen.value = false
  lookupKind.value = ''
  error.value = ''
}
</script>

<template>
  <section class="opd-window" role="dialog" aria-modal="true" aria-label="ProductOPDescription">
    <header class="opd-titlebar"><span>ProductOPDescription</span><button type="button" aria-label="關閉" @click="emit('close')">×</button></header>
    <div class="opd-context">產品 {{ productNo }} / {{ productVersion }}<span v-if="processOptions[0]">　流程 {{ processOptions[0].processNo }} / {{ processOptions[0].processVersion }}</span></div>
    <p v-if="!developmentPreview" class="opd-note">Production API 尚未接線；正式資料唯讀。</p>
    <p v-else-if="readOnly" class="opd-note">產品 IssueState 不為未核准，資料唯讀。</p>
    <p v-if="notice" class="opd-message">{{ notice }}</p>
    <p v-if="error && !definitionOpen" class="opd-message is-error">{{ error }}</p>
    <main class="opd-main">
      <div class="opd-grid-wrap">
        <table class="opd-grid">
          <thead><tr><th>OP No</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
          <tbody>
            <tr v-for="row in displayRows" :key="row.key" :class="{ selected: selectedKeys.includes(row.key), active: activeKey === row.key }" @click="selectRow(row, $event)">
              <td>{{ row.opNo }}</td><td class="description-cell">{{ row.description }}</td><td>{{ row.creator ?? row.Creator }}</td><td>{{ formatDate(row.createDate ?? row.CreateDate) }}</td><td>{{ row.reviser ?? row.Reviser }}</td><td>{{ formatDate(row.reviseDate ?? row.ReviseDate) }}</td>
            </tr>
            <tr v-if="!displayRows.length"><td colspan="6" class="opd-empty">目前產品版本尚無 OP 說明。</td></tr>
          </tbody>
        </table>
      </div>
      <aside class="opd-actions">
        <button class="opd-button confirm" type="button" :disabled="readOnly" @click="openAdd">ADD (&amp;A)</button>
        <button class="opd-button" type="button" :disabled="!canEditRow" @click="openEdit">EDIT (&amp;E)</button>
        <button class="opd-button" type="button" :disabled="readOnly || (!selectedKeys.length && !activeKey)" @click="deleteSelected">DEL (&amp;D)</button>
        <span class="opd-spacer"></span>
        <button class="opd-button" type="button" @click="emit('close')">Close (&amp;X)</button>
      </aside>
    </main>

    <div v-if="definitionOpen" class="opd-form-backdrop">
      <section class="opd-definition" role="dialog" aria-modal="true" aria-label="Product OP Description Define">
        <header class="opd-titlebar"><span>Product OP Description Define</span><button type="button" aria-label="關閉" @click="closeDefinition">×</button></header>
        <fieldset class="opd-fields">
          <legend>OP Description</legend>
          <label class="opd-label process-label">Process No</label>
          <input class="opd-readonly process-no" :value="draft.processNo" readonly>
          <input class="opd-readonly process-version" :value="draft.processVersion" readonly aria-label="Process Version">
          <button v-if="definitionMode === 'add'" class="opd-lookup process-lookup" type="button" @click="showLookup('process')">...</button>
          <label class="opd-label op-label">OP No</label>
          <input class="opd-readonly op-no" :value="draft.opNo" readonly>
          <button v-if="definitionMode === 'add'" class="opd-lookup op-lookup" type="button" :disabled="!draft.processNo" @click="showLookup('operation')">...</button>
          <label class="opd-label description-label">Description</label>
          <textarea v-model="draft.description" class="opd-description" maxlength="255" :readonly="readOnly" @input="dirty = true"></textarea>
          <small class="opd-length">{{ String(draft.description ?? '').length }}/255</small>
        </fieldset>
        <div class="opd-audit"><span>建立人 {{ draft.creator || (definitionMode === 'add' ? userNo() : '') }}</span><span>建立日期 {{ formatDate(draft.createDate) }}</span><span>修改人 {{ draft.reviser || '' }}</span><span>修改日期 {{ formatDate(draft.reviseDate) }}</span></div>
        <p v-if="error" class="opd-message is-error form-error">{{ error }}</p>
        <footer class="opd-form-actions">
          <button class="opd-button" type="button" :disabled="!canSubmit" @click="saveDefinition(false)">套用</button>
          <button class="opd-button confirm" type="button" :disabled="readOnly || !draft.opNo || String(draft.description ?? '').length > 255" @click="confirmDefinition">確定</button>
          <button class="opd-button" type="button" @click="closeDefinition">關閉</button>
        </footer>
        <div v-if="lookupKind" class="opd-lookup-backdrop" @click.self="lookupKind = ''">
          <section class="opd-lookup-dialog" role="dialog" aria-modal="true">
            <header><strong>{{ lookupKind === 'process' ? '選擇產品流程' : '選擇流程作業站' }}</strong><button type="button" aria-label="關閉查找" @click="lookupKind = ''">×</button></header>
            <table v-if="lookupKind === 'process'"><thead><tr><th>PSNo</th><th>ProcessNo</th><th>ProcessVersion</th></tr></thead><tbody><tr v-for="row in lookupProcessOptions" :key="row.processNo + row.processVersion" @click="chooseProcess(row)"><td>{{ row.psNo }}</td><td>{{ row.processNo }}</td><td>{{ row.processVersion }}</td></tr><tr v-if="!lookupProcessOptions.length"><td colspan="3">沒有可選流程。</td></tr></tbody></table>
            <table v-else><thead><tr><th>OPNo</th><th>OPName</th><th>Description</th></tr></thead><tbody><tr v-for="row in operationOptions" :key="row.opNo" @click="chooseOperation(row)"><td>{{ row.opNo }}</td><td>{{ row.opName }}</td><td>{{ row.description }}</td></tr><tr v-if="!operationOptions.length"><td colspan="3">此流程沒有其他可新增作業站。</td></tr></tbody></table>
          </section>
        </div>
      </section>
    </div>
  </section>
</template>

<style scoped>
.opd-window{position:relative;width:min(684px,96vw);height:min(486px,calc(100vh - 24px));min-height:320px;background:#fff;border:1px solid #aaa79c;box-shadow:0 14px 44px #0004;color:#28271f;font:12px "Segoe UI","Microsoft JhengHei",sans-serif;display:flex;flex-direction:column;overflow:hidden}
.opd-titlebar{height:30px;min-height:30px;display:flex;align-items:center;justify-content:space-between;padding:0 8px 0 12px;background:#f5f4f0;border-bottom:1px solid #d4d1c8;color:#333}.opd-titlebar button{border:0;background:transparent;font-size:20px;color:#555;cursor:pointer;width:26px;height:26px}
.opd-context{padding:5px 12px;color:#625f55;border-bottom:1px solid #e3e1d9}.opd-note,.opd-message{margin:5px 12px 0;padding:4px 7px;background:#fff8df;border:1px solid #e4d7a3;color:#605326}.opd-message.is-error{background:#fff0ed;border-color:#d5a59b;color:#7a261c}.opd-main{display:grid;grid-template-columns:minmax(0,1fr) 80px;gap:12px;padding:14px 12px 12px;flex:1;min-height:0}.opd-grid-wrap{overflow:auto;border:1px solid #aaa79c}.opd-grid{width:100%;border-collapse:collapse;table-layout:fixed}.opd-grid th{position:sticky;top:0;background:#f1d982;border:1px solid #c6b983;padding:5px 4px;text-align:left;font-weight:600;white-space:nowrap}.opd-grid td{border:1px solid #d3d0c7;padding:4px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}.opd-grid th:nth-child(1){width:90px}.opd-grid th:nth-child(2){width:190px}.opd-grid th:nth-child(3),.opd-grid th:nth-child(5){width:68px}.opd-grid th:nth-child(4),.opd-grid th:nth-child(6){width:116px}.opd-grid tr{cursor:pointer}.opd-grid tbody tr.selected{background:#fff0b0}.opd-grid tbody tr.active{outline:1px solid #bf982e;outline-offset:-1px}.opd-grid .description-cell{white-space:pre-wrap}.opd-empty{text-align:center;color:#777;padding:20px!important}.opd-actions{display:flex;flex-direction:column;gap:6px}.opd-spacer{flex:1}.opd-button{min-height:29px;padding:4px 6px;border:1px solid #aaa79c;border-radius:3px;background:linear-gradient(#fff,#f1efe9);color:#292820;font:inherit;white-space:nowrap;cursor:pointer}.opd-button:hover:not(:disabled),.opd-lookup:hover:not(:disabled){border-color:#c49a35;background:#fff5d6}.opd-button.confirm{background:#f5a62a;border-color:#dc941d;color:white;font-weight:600}.opd-button:disabled,.opd-lookup:disabled{opacity:.5;cursor:not-allowed}
.opd-form-backdrop,.opd-lookup-backdrop{position:absolute;inset:0;background:#0005;display:grid;place-items:center;z-index:2}.opd-definition{position:relative;width:min(496px,96vw);height:min(276px,calc(100vh - 44px));min-height:260px;background:#fff;border:1px solid #aaa79c;box-shadow:0 12px 36px #0005;display:flex;flex-direction:column}.opd-fields{position:relative;margin:11px 15px 0;border:1px solid #aaa79c;padding:13px 9px 8px;height:137px;min-height:137px}.opd-fields legend{padding:0 5px;color:#555}.opd-label{position:absolute;text-align:right;color:#444}.process-label{left:4px;top:24px;width:78px}.op-label{left:4px;top:53px;width:78px}.description-label{left:4px;top:82px;width:78px}.opd-readonly,.opd-description{position:absolute;border:1px solid #888;padding:3px 5px;font:12px "Segoe UI","Microsoft JhengHei",sans-serif}.opd-readonly{background:#fff0b0;height:22px}.process-no{left:88px;top:20px;width:160px}.process-version{left:252px;top:20px;width:50px}.process-lookup{left:307px;top:19px}.op-no{left:88px;top:49px;width:208px}.op-lookup{left:307px;top:48px}.opd-lookup{position:absolute;width:25px;height:23px;border:1px solid #aaa79c;background:#f8f7f3;cursor:pointer}.opd-description{left:88px;top:78px;width:240px;height:44px;resize:vertical;white-space:pre-wrap}.opd-length{position:absolute;right:8px;bottom:6px;color:#777}.opd-audit{display:grid;grid-template-columns:1fr 1fr;gap:3px 12px;margin:8px 16px 0;padding:5px 8px;border:1px solid #dedbd1;color:#666;font-size:11px}.opd-form-actions{display:flex;justify-content:flex-end;gap:6px;margin:auto 12px 8px}.form-error{margin:4px 15px 0;padding:3px 6px}.opd-lookup-backdrop{inset:30px 0 0;z-index:3}.opd-lookup-dialog{width:min(430px,94vw);max-height:70%;overflow:auto;border:1px solid #aaa79c;background:#fff;box-shadow:0 12px 35px #0005}.opd-lookup-dialog header{display:flex;justify-content:space-between;align-items:center;padding:7px 10px;background:#f5f4f0;border-bottom:1px solid #d4d1c8}.opd-lookup-dialog header button{border:0;background:none;font-size:18px;cursor:pointer}.opd-lookup-dialog table{width:100%;border-collapse:collapse}.opd-lookup-dialog th,.opd-lookup-dialog td{border:1px solid #d3d0c7;padding:6px;text-align:left}.opd-lookup-dialog th{position:sticky;top:0;background:#f1d982}.opd-lookup-dialog tbody tr{cursor:pointer}.opd-lookup-dialog tbody tr:hover{background:#fff0b0}
@media(max-width:600px){.opd-main{grid-template-columns:minmax(0,1fr);grid-template-rows:minmax(150px,1fr) auto}.opd-actions{flex-direction:row;flex-wrap:wrap}.opd-spacer{display:none}.opd-grid{min-width:550px}.opd-definition{height:276px}.opd-audit{grid-template-columns:1fr 1fr}}
</style>
