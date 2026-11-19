<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import { rowsKey, seedRows } from './productBasisDevelopment'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'
import { apiErrorMessage } from '../utils/apiError'

type Rule = {
  product: string; equipment: string; operation: string; module: string | null; method: number
  count: number | null | ''; shiftA: string | null; shiftB: string | null; item: string | null
  itemType: number | null; description: string | null; stamp: number | null; creator: string | null
  createDate?: string | null; reviser?: string | null; reviseDate?: string | null
}
interface Dto {
  productNo: string; equipmentNo: string; opNo: string; moduleNo: string | null; qcMethod: number
  qcNum: number | null; shiftA: string | null; shiftB: string | null; qcItem: string | null
  qcType: number | null; description: string | null; dataStamp: number | null; creator: string | null
  createDate: string | null; reviser: string | null; reviseDate: string | null
}
interface Option { value: string; label: string | null }
type Mode = 'add' | 'edit' | 'copy' | ''
const props = withDefaults(defineProps<{ apiBaseUrl?: string; developmentPreview?: boolean; currentUserNo?: string }>(),
  { apiBaseUrl: '', developmentPreview: false, currentUserNo: '' })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()
const storageKey = 'ymes.dev.prd206-qc-frequency.v1'
const rows = ref<Rule[]>([])
const products = ref<Option[]>([])
const equipment = ref<Option[]>([])
const operations = ref<Option[]>([])
const methods = ['多批抽一批', '每天第一批', '每班第一批']
const selected = ref('')
const filter = ref('')
const mode = ref<Mode>('')
const draft = ref<Rule>(blank())
const original = ref('')
const error = ref('')
const notice = ref('')
const lookupNotice = ref('')
const busy = ref(false)
const lookupBusy = ref(false)
const confirmingClose = ref(false)
const page = ref<HTMLElement | null>(null)
const editor = ref<HTMLFormElement | null>(null)
let previousEditorFocus: HTMLElement | null = null
const key = (row: Rule) => JSON.stringify([row.product, row.equipment, row.operation])
const filtered = computed(() => rows.value.filter(row =>
  [row.product, row.equipment, row.operation, row.description ?? ''].some(value => value.toUpperCase().includes(filter.value.trim().toUpperCase())),
))
const active = computed(() => filtered.value.find(row => key(row) === selected.value) ?? filtered.value[0])
function selectRow(row: Rule): void {
  if (busy.value || confirmingClose.value || mode.value) return
  selected.value = key(row)
}
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function read<T>(name: string, fallback: T): T {
  if (!mock.value) return fallback
  try { const stored: unknown = JSON.parse(localStorage.getItem(name) ?? 'null'); if (Array.isArray(stored)) return stored as T } catch { /* DEV fallback only */ }
  return fallback
}
function blank(): Rule {
  return { product: '*', equipment: '*', operation: '*', module: 'WIP', method: 0, count: '', shiftA: '', shiftB: '',
    item: null, itemType: null, description: '', stamp: null, creator: null }
}
function fromDto(row: Dto): Rule {
  return { product: row.productNo, equipment: row.equipmentNo, operation: row.opNo, module: row.moduleNo,
    method: row.qcMethod, count: row.qcNum, shiftA: row.shiftA, shiftB: row.shiftB, item: row.qcItem,
    itemType: row.qcType, description: row.description, stamp: row.dataStamp, creator: row.creator,
    createDate: row.createDate, reviser: row.reviser, reviseDate: row.reviseDate }
}
function apiUrl(path = ''): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL('api/prd/qc-frequency-rules' + path, new URL(base.replace(/\/+$/, '') + '/', location.origin)).toString()
}
async function request<T>(path = '', init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body) headers.set('Content-Type', 'application/json')
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init, headers })
  if (response.status === 401) { location.assign(import.meta.env.BASE_URL); throw new Error('登入已逾期，請重新登入。') }
  if (!response.ok) throw new Error(await apiErrorMessage(response))
  return await response.json() as T
}
function identity(row: Rule) { return { productNo: row.product, equipmentNo: row.equipment, opNo: row.operation } }
async function loadRows(prefer = ''): Promise<void> {
  const next = mock.value
    ? read<Rule[]>(storageKey, [{ ...blank(), product: 'DEV-PRD-1002', operation: 'DEV-OP-10', count: 5,
        description: 'DEV 每五批抽一批', creator: 'DEV001', stamp: 0 }])
    : (await request<Dto[]>()).map(fromDto)
  rows.value = clone(next)
  selected.value = next.some(row => key(row) === prefer) ? prefer : (next[0] ? key(next[0]) : '')
}
async function query(): Promise<void> {
  if (busy.value || confirmingClose.value || mode.value) return
  busy.value = true; error.value = ''; notice.value = ''
  try { await loadRows(selected.value) } catch (reason) { error.value = reason instanceof Error ? reason.message : '讀取抽樣頻率失敗。' }
  finally { busy.value = false }
}
async function loadOptions(): Promise<void> {
  lookupBusy.value = true; lookupNotice.value = ''
  if (mock.value) {
    products.value = [...new Set(read(rowsKey, seedRows).filter(row => Number(row.issueState) === 2).map(row => String(row.productNo)))].map(value => ({ value, label: null }))
    equipment.value = ['DEV-EQP-01', 'DEV-EQP-02'].map(value => ({ value, label: null }))
    operations.value = ['DEV-OP-10', 'DEV-OP-20', 'DEV-OP-30'].map(value => ({ value, label: null }))
  } else {
    const tasks = [{ path: '/options/products', target: products, label: '產品' },
      { path: '/options/equipment', target: equipment, label: '設備' }, { path: '/options/operations', target: operations, label: '作業站' }]
    const results = await Promise.allSettled(tasks.map(async task => { task.target.value = await request<Option[]>(task.path) }))
    results.forEach((result, index) => {
      if (result.status === 'rejected') {
        tasks[index]!.target.value = []
        lookupNotice.value += tasks[index]!.label + '候選無法載入：' + (result.reason instanceof Error ? result.reason.message : '請稍後重試。') + ' '
      }
    })
  }
  lookupBusy.value = false
  if (mode.value && !editor.value?.contains(document.activeElement)) await restoreEditorFocus()
}
async function begin(next: Exclude<Mode, ''>): Promise<void> {
  if (busy.value || confirmingClose.value || mode.value) return
  const row = active.value
  if (next !== 'add' && !row) return
  previousEditorFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null
  busy.value = true; error.value = ''; notice.value = ''
  try {
    let value = next === 'add' || !row ? blank() : clone(row)
    if (next !== 'add' && row && !mock.value)
      value = fromDto(await request<Dto>('/item?' + new URLSearchParams(identity(row)).toString()))
    if (next === 'copy') { value.module = 'WIP'; value.item = null; value.itemType = null; value.stamp = null; value.creator = null; value.reviser = null }
    draft.value = value; original.value = JSON.stringify(value); mode.value = next
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '讀取目前資料失敗。' }
  finally { busy.value = false; await restoreEditorFocus() }
}
function validShift(value: string | null): boolean { return /^(?:[01]\d|2[0-4]):[0-5]\d$/.test(value ?? '') }
function validation(value: Rule): string {
  if (!value.product || value.product.length > 25 || !value.equipment || value.equipment.length > 50 || !value.operation || value.operation.length > 30) return '請選擇有效編號；產品最多25字、設備50字、作業站30字。'
  if (![0, 1, 2].includes(value.method)) return '請選擇已知頻率。'
  if (value.method === 0 && (typeof value.count !== 'number' || !Number.isInteger(value.count) || value.count < 1 || value.count > 99)) return '抽樣批次數須為 1 到 99 的整數。'
  if (value.method !== 0 && !validShift(value.shiftA)) return '請輸入 A 班時分，格式 HH:mm，時00–24、分00–59。'
  if (value.method === 2 && !validShift(value.shiftB)) return '請輸入 B 班時分，格式 HH:mm，時00–24、分00–59。'
  return ''
}
async function save(close: boolean): Promise<void> {
  if (busy.value || confirmingClose.value || !mode.value) return
  const value = clone(draft.value); const nextMode = mode.value
  error.value = validation(value); if (error.value) return
  busy.value = true; notice.value = ''
  try {
    let saved: Rule
    if (mock.value) {
      const existing = rows.value.find(row => key(row) === key(value))
      if (nextMode !== 'edit' && existing) throw new Error('這組產品、設備與作業站已有規則。')
      if (nextMode === 'edit' && (!existing || existing.stamp !== value.stamp)) throw new Error('資料已變更，請重新查詢。')
      if (value.method !== 0) value.count = null
      if (value.method === 0) value.shiftA = null
      if (value.method !== 2) value.shiftB = null
      value.stamp = existing ? (existing.stamp ?? 0) + 1 : 0
      value.creator = existing?.creator ?? props.currentUserNo
      value.reviser = existing ? props.currentUserNo : null
      const values = existing ? rows.value.map(row => key(row) === key(value) ? value : row) : [...rows.value, value]
      localStorage.setItem(storageKey, JSON.stringify(values)); saved = value
    } else {
      const body = { ...identity(value), qcMethod: value.method, qcNum: value.count === '' ? null : value.count,
        shiftA: value.shiftA, shiftB: value.shiftB, description: value.description,
        ...(nextMode === 'edit' ? { dataStamp: value.stamp } : {}) }
      saved = fromDto(await request<Dto>('', { method: nextMode === 'edit' ? 'PUT' : 'POST', body: JSON.stringify(body) }))
    }
    const savedKey = key(saved)
    const committed = clone(saved)
    rows.value = rows.value.some(row => key(row) === savedKey)
      ? rows.value.map(row => key(row) === savedKey ? committed : row)
      : [...rows.value, committed]
    selected.value = savedKey
    draft.value = clone(saved); original.value = JSON.stringify(saved)
    mode.value = close ? '' : 'edit'
    filter.value = ''
    notice.value = '已儲存抽樣頻率規則。'
    try { await loadRows(savedKey) } catch (reason) { error.value = '保存已完成，但清單重查失敗：' + (reason instanceof Error ? reason.message : '請稍後重查。') }
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : '保存抽樣頻率失敗。'
    try { await loadRows() } catch { /* Keep original failure; no automatic write retry. */ }
  } finally { busy.value = false; await restoreEditorFocus() }
}
async function remove(): Promise<void> {
  const row = active.value
  if (busy.value || confirmingClose.value || mode.value || !row) return
  busy.value = true; error.value = ''; notice.value = ''
  try {
    if (!await requestConfirmation('確定刪除此產品、設備與作業站的抽樣頻率？')) return
    if (mock.value) {
      const values = rows.value.filter(item => key(item) !== key(row)); localStorage.setItem(storageKey, JSON.stringify(values))
    } else await request<Dto>('', { method: 'DELETE', body: JSON.stringify({ ...identity(row), dataStamp: row.stamp }) })
    const deletedKey = key(row)
    rows.value = rows.value.filter(item => key(item) !== deletedKey)
    selected.value = filtered.value[0] ? key(filtered.value[0]) : ''
    notice.value = '已刪除抽樣頻率規則。'
    try { await loadRows(selected.value) }
    catch (reason) { error.value = '刪除已完成，但清單重查失敗：' + (reason instanceof Error ? reason.message : '請稍後重查。') }
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : '刪除抽樣頻率失敗。'
    try { await loadRows() } catch { /* Keep the operation error. */ }
  } finally { busy.value = false }
}
async function allowClose(): Promise<boolean> {
  if (busy.value || confirmingClose.value) return false
  confirmingClose.value = true
  try {
    return !mode.value || JSON.stringify(draft.value) === original.value
      || await requestConfirmation('放棄未保存的變更並關閉？')
  } finally { confirmingClose.value = false }
}
async function closeEditor(): Promise<void> {
  if (await allowClose()) { mode.value = ''; error.value = '' }
  await restoreEditorFocus()
}
async function closePage(): Promise<void> {
  if (mode.value || confirmationMessage.value !== null) return
  if (!await allowClose()) return
  mode.value = ''; error.value = ''; emit('close')
}
function canFocus(element: HTMLElement): boolean {
  return element.isConnected && element.getClientRects().length > 0 && !element.matches(':disabled') && !element.closest('[inert]')
}
function editorControls(): HTMLElement[] {
  return Array.from(editor.value?.querySelectorAll<HTMLElement>('button:not(:disabled),input:not(:disabled),select:not(:disabled),textarea:not(:disabled),[tabindex="0"]') ?? []).filter(canFocus)
}
async function restoreEditorFocus(): Promise<void> {
  await nextTick()
  if (busy.value || confirmingClose.value || confirmationMessage.value !== null) return
  if (mode.value && editor.value) {
    const preferred = Array.from(editor.value.querySelectorAll<HTMLElement>('input:not([readonly]):not(:disabled),select:not(:disabled),textarea:not(:disabled)')).find(canFocus)
    ;(preferred ?? editorControls()[0] ?? editor.value).focus()
  } else if (previousEditorFocus && page.value?.contains(previousEditorFocus) && canFocus(previousEditorFocus)) previousEditorFocus.focus()
  else {
    const first = Array.from(page.value?.querySelectorAll<HTMLElement>('input:not(:disabled),button:not(:disabled),[tabindex="0"]') ?? []).find(canFocus)
    ;(first ?? page.value)?.focus()
  }
}
function keepEditorFocus(event: KeyboardEvent): void {
  if (event.key !== 'Tab' || !mode.value || confirmationMessage.value !== null) return
  if (busy.value || confirmingClose.value) { event.preventDefault(); return }
  const controls = editorControls(), first = controls[0], last = controls[controls.length - 1]
  if (!first || !last) { event.preventDefault(); editor.value?.focus(); return }
  const active = document.activeElement
  if (!editor.value?.contains(active) || active === editor.value) { event.preventDefault(); (event.shiftKey ? last : first).focus() }
  else if (event.shiftKey && active === first) { event.preventDefault(); last.focus() }
  else if (!event.shiftKey && active === last) { event.preventDefault(); first.focus() }
}
onMounted(() => { document.addEventListener('keydown', keepEditorFocus, true); void query(); void loadOptions() })
onBeforeUnmount(() => { document.removeEventListener('keydown', keepEditorFocus, true) })
</script>

<template>
  <main ref="page" class="frequency" tabindex="-1">
    <header :inert="!!mode || confirmationMessage !== null"><h1>產品抽樣頻率規則 <small v-if="mock">DEV 假資料</small></h1><button :disabled="busy || confirmingClose || !!mode" @click="closePage">關閉</button></header>
    <div class="toolbar" :inert="!!mode || confirmationMessage !== null">
      <label>產品、設備、作業站或說明 <input v-model="filter" :disabled="busy || !!mode || confirmingClose" @keyup.enter="query" /></label>
      <button :disabled="busy || !!mode || confirmingClose" @click="query">重新查詢</button>
      <button :disabled="busy || !!mode || confirmingClose" @click="begin('add')">新增</button>
      <button :disabled="busy || !!mode || confirmingClose || !active" @click="begin('edit')">修改</button>
      <button :disabled="busy || !!mode || confirmingClose || !active" @click="begin('copy')">複製</button>
      <button :disabled="busy || !!mode || confirmingClose || !active" @click="remove">刪除</button>
    </div>
    <p v-if="notice" role="status">{{ notice }}</p>
    <p v-if="error && !mode" role="alert">{{ error }}</p>
    <div class="grid" :inert="!!mode || confirmationMessage !== null">
      <table>
        <thead><tr><th>產品</th><th>設備</th><th>作業站</th><th>模組</th><th>頻率</th><th>抽樣批次數</th><th>A 班</th><th>B 班</th><th>說明</th></tr></thead>
        <tbody>
          <tr v-for="row in filtered" :key="key(row)" :class="{ selected: row === active }">
            <td><button :disabled="busy || !!mode || confirmingClose" :aria-pressed="row === active" @click="selectRow(row)">{{ row.product }}</button></td>
            <td>{{ row.equipment }}</td><td>{{ row.operation }}</td><td>{{ row.module }}</td><td>{{ methods[row.method] ?? ('代碼 ' + row.method) }}</td><td>{{ row.count }}</td><td>{{ row.shiftA }}</td><td>{{ row.shiftB }}</td><td>{{ row.description }}</td>
          </tr>
          <tr v-if="!filtered.length"><td colspan="9">{{ busy ? '資料讀取中…' : '無抽樣頻率規則' }}</td></tr>
        </tbody>
      </table>
    </div>
    <div v-if="mode" class="overlay">
      <form ref="editor" class="editor" role="dialog" aria-modal="true" aria-label="抽樣頻率規則" tabindex="-1" :inert="confirmingClose || confirmationMessage !== null" @submit.prevent="save(true)">
        <header><h2>抽樣頻率規則</h2><button type="button" :disabled="busy || confirmingClose" @click="closeEditor">關閉</button></header>
        <p v-if="lookupBusy" role="status">候選載入中…</p>
        <p v-if="lookupNotice" role="alert">{{ lookupNotice }}<button type="button" :disabled="busy || lookupBusy" @click="loadOptions">重試候選</button></p>
        <label>模組 <input :value="draft.module" readonly /></label>
        <label>產品
          <input v-if="mode === 'edit'" :value="draft.product" readonly />
          <select v-else v-model="draft.product" :disabled="busy"><option value="*">*（全部）</option><option v-if="draft.product !== '*' && !products.some(option => option.value === draft.product)" :value="draft.product" disabled>{{ draft.product }}（目前資料，請重選）</option><option v-for="option in products" :key="option.value" :value="option.value" :disabled="option.value.length > 25">{{ option.value }} {{ option.label }}{{ option.value.length > 25 ? '（超過25字）' : '' }}</option></select>
        </label>
        <label>設備
          <input v-if="mode === 'edit'" :value="draft.equipment" readonly />
          <select v-else v-model="draft.equipment" :disabled="busy"><option value="*">*（全部）</option><option v-if="draft.equipment !== '*' && !equipment.some(option => option.value === draft.equipment)" :value="draft.equipment" disabled>{{ draft.equipment }}（目前資料，請重選）</option><option v-for="option in equipment" :key="option.value" :value="option.value">{{ option.value }} {{ option.label }}</option></select>
        </label>
        <label>作業站
          <input v-if="mode === 'edit'" :value="draft.operation" readonly />
          <select v-else v-model="draft.operation" :disabled="busy"><option value="*">*（全部）</option><option v-if="draft.operation !== '*' && !operations.some(option => option.value === draft.operation)" :value="draft.operation" disabled>{{ draft.operation }}（目前資料，請重選）</option><option v-for="option in operations" :key="option.value" :value="option.value">{{ option.value }} {{ option.label }}</option></select>
        </label>
        <label v-if="draft.module === 'SPC'">品管項目 <input :value="draft.item" readonly /></label>
        <label>頻率 <select v-model.number="draft.method" :disabled="busy"><option v-if="![0, 1, 2].includes(draft.method)" :value="draft.method" disabled>代碼 {{ draft.method }}（請重選）</option><option v-for="(method, index) in methods" :key="method" :value="index">{{ method }}</option></select></label>
        <label v-if="draft.method === 0">抽樣批次數 <input v-model.number="draft.count" :disabled="busy" type="number" min="1" max="99" step="1" required /></label>
        <label v-else>A 班開始 <input v-model.trim="draft.shiftA" :disabled="busy" placeholder="HH:mm" maxlength="5" required /></label>
        <label v-if="draft.method === 2">B 班開始 <input v-model.trim="draft.shiftB" :disabled="busy" placeholder="HH:mm" maxlength="5" required /></label>
        <label>說明 <input v-model="draft.description" :disabled="busy" maxlength="255" /></label>
        <p v-if="error" role="alert">{{ error }}</p>
        <footer><button :disabled="busy || confirmingClose">{{ busy ? '處理中…' : '確定' }}</button><button type="button" :disabled="busy || confirmingClose" @click="save(false)">套用</button></footer>
      </form>
    </div>
  </main>
  <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
</template>

<style scoped>
.frequency { display: grid; align-content: start; gap: 8px; min-width: 0; padding: 10px; background: #fff; color: #292820; font: 12px "Segoe UI", "Microsoft JhengHei", sans-serif; }
header, .toolbar, footer { display: flex; align-items: center; flex-wrap: wrap; gap: 8px; }
header { justify-content: space-between; }
h1, h2, p { margin: 0; }
h1 { font-size: 17px; }
.toolbar, thead { background: #f5f3eb; }
.toolbar { padding: 6px; border: 1px solid #dedbd0; }
button, input, select { font: inherit; }
button { padding: 4px 10px; border: 1px solid #aaa797; border-radius: 3px; background: #f8f7f2; color: inherit; }
button:disabled { opacity: .5; }
.grid { overflow: auto; border: 1px solid #d9d6cb; }
table { width: 100%; border-collapse: collapse; text-align: left; white-space: nowrap; }
th, td { padding: 6px; border: 1px solid #e3e0d6; }
.selected { background: #fff2c6; }
.overlay { position: fixed; inset: 0; z-index: 91; display: grid; place-items: center; padding: 12px; background: #0005; }
.editor { display: grid; gap: 12px; width: min(540px, 95vw); max-height: 90vh; overflow: auto; padding: 16px; background: #fff; box-shadow: 0 8px 30px #0004; }
.editor > label { display: flex; gap: 8px; align-items: center; }
footer { justify-content: flex-end; }
[role=alert] { color: #a32020; }
</style>
