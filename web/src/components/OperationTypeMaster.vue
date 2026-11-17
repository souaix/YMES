<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import OperationFeatureDialog from './OperationFeatureDialog.vue'

type IssueState = -1 | 0 | 1 | 2
type Mode = 'new' | 'edit' | 'copy'
interface OperationTypeRow { opType: string; description: string; creator: string; createDate: string; issueState: IssueState; dataStamp: number | null; reviser: string; reviseDate: string }
interface ApiEnvelope<T> { items?: T[]; rows?: T[]; data?: T[]; message?: string; title?: string }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes.dev.op-type-master.v1'
const seedRows: OperationTypeRow[] = [
  { opType: 'DEV-ASSY', description: 'DEV 組裝作業站類別', creator: 'DEV001', createDate: '2026-11-01T08:30:00', issueState: 0, dataStamp: 0, reviser: '', reviseDate: '' },
  { opType: 'DEV-QC', description: 'DEV 品質檢驗作業站類別', creator: 'DEV001', createDate: '2026-10-28T09:00:00', issueState: 2, dataStamp: 3, reviser: 'DEV002', reviseDate: '2026-11-02T13:10:00' },
  { opType: 'DEV-OLD', description: 'DEV 停用範例', creator: 'DEV001', createDate: '2026-10-12T11:00:00', issueState: -1, dataStamp: 4, reviser: 'DEV002', reviseDate: '2026-10-20T15:30:00' },
]
const rows = ref<OperationTypeRow[]>([])
const selected = ref('')
const selectedKeys = ref<string[]>([])
const issueState = ref<IssueState>(0)
const keyword = ref('')
const loading = ref(false)
const busy = ref(false)
const error = ref('')
const notice = ref('')
const dialogMode = ref<Mode | ''>('')
const draft = ref({ opType: '', description: '' })
const editing = ref<OperationTypeRow | null>(null)
const dialogOpen = computed(() => dialogMode.value !== '')
const visibleRows = computed(() => rows.value.filter((row) => issueState.value === -1 || row.issueState === issueState.value))
const selectedRow = computed(() => rows.value.find((row) => row.opType === selected.value) ?? null)
const selectedRows = computed(() => rows.value.filter((row) => selectedKeys.value.includes(row.opType)))
const propertyOpen = ref(false)
const propertyTarget = ref('')

function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function displayDate(value: string): string {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return new Intl.DateTimeFormat('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23' }).format(date).replaceAll('-', '/')
}
function persistMock(): void { localStorage.setItem(storageKey, JSON.stringify(rows.value)) }
function loadMock(): void {
  try {
    const saved = localStorage.getItem(storageKey)
    rows.value = saved ? JSON.parse(saved) as OperationTypeRow[] : clone(seedRows)
    if (!saved) persistMock()
  } catch { rows.value = clone(seedRows) }
}
function apiUrl(path: string): string {
  if (!props.apiBaseUrl.trim()) throw new Error('目前尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(props.apiBaseUrl.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init, headers: { 'Content-Type': 'application/json', ...init.headers } })
  const raw = await response.text()
  let body: unknown = null
  try { body = raw ? JSON.parse(raw) : null } catch { body = raw }
  if (!response.ok) {
    const envelope = body as ApiEnvelope<unknown> | null
    throw new Error(envelope && typeof envelope === 'object' ? envelope.message ?? envelope.title ?? 'API 回應 ' + response.status : String(body || 'API 回應 ' + response.status))
  }
  return body as T
}
function normalizeRow(row: Record<string, unknown>): OperationTypeRow {
  return {
    opType: String(row.opType ?? row.optype ?? ''), description: String(row.description ?? ''),
    creator: String(row.creator ?? ''), createDate: String(row.createDate ?? row.createdate ?? ''),
    issueState: Number(row.issueState ?? row.issuestate ?? 0) as IssueState,
    dataStamp: row.dataStamp == null && row.datastamp == null ? null : Number(row.dataStamp ?? row.datastamp),
    reviser: String(row.reviser ?? ''), reviseDate: String(row.reviseDate ?? row.revisedate ?? ''),
  }
}
async function loadRows(): Promise<void> {
  loading.value = true; error.value = ''; notice.value = ''; selectedKeys.value = []
  try {
    if (mock.value) loadMock()
    else {
      const query = new URLSearchParams({ issueState: '-1' })
      if (keyword.value.trim()) query.set('opType', keyword.value.trim())
      const result = await api<OperationTypeRow[] | ApiEnvelope<OperationTypeRow>>('/api/operation-types?' + query.toString())
      const list = Array.isArray(result) ? result : result.items ?? result.rows ?? result.data ?? []
      rows.value = list.map((row) => normalizeRow(row as unknown as Record<string, unknown>))
    }
    if (selected.value && !rows.value.some((row) => row.opType === selected.value)) selected.value = ''
    notice.value = '資料總筆數 ' + visibleRows.value.length
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入作業站類別失敗。' }
  finally { loading.value = false }
}
function selectRow(row: OperationTypeRow, event?: MouseEvent): void {
  if (event?.ctrlKey || event?.metaKey) {
    const wasSelected = selectedKeys.value.includes(row.opType)
    selectedKeys.value = wasSelected ? selectedKeys.value.filter((key) => key !== row.opType) : [...selectedKeys.value, row.opType]
    if (wasSelected && selected.value === row.opType) selected.value = selectedKeys.value.at(-1) ?? ''
  } else {
    selectedKeys.value = [row.opType]
    selected.value = row.opType
  }
}

function openDialog(mode: Mode, row: OperationTypeRow | null = null): void {
  if ((mode === 'edit' || mode === 'copy') && !row) return
  if (mode === 'edit' && row?.issueState !== 0) { notice.value = '僅未簽核資料可修改。'; return }
  dialogMode.value = mode; editing.value = row
  draft.value = { opType: mode === 'new' ? '' : row?.opType ?? '', description: mode === 'new' ? '' : row?.description ?? '' }
  error.value = ''; notice.value = ''
}
function closeDialog(): void { dialogMode.value = ''; editing.value = null }
async function saveForm(closeAfter: boolean): Promise<void> {
  const opType = draft.value.opType.trim().toUpperCase()
  const description = draft.value.description.trim()
  if (!opType) { error.value = 'OP Type 為必填。'; return }
  if (opType.length > 20) { error.value = 'OP Type 最多 20 個字元。'; return }
  if (description.length > 255) { error.value = 'Description 最多 255 個字元。'; return }
  busy.value = true; error.value = ''; notice.value = ''
  try {
    if (mock.value) {
      const duplicate = rows.value.some((row) => row.opType.toUpperCase() === opType && (dialogMode.value !== 'edit' || row.opType !== editing.value?.opType))
      if (duplicate) throw new Error('OP Type ' + opType + ' 已存在。')
      const now = new Date().toISOString(); const user = props.currentUserNo || 'DEVUSER'
      if (dialogMode.value === 'edit' && editing.value) {
        rows.value = rows.value.map((row) => row.opType === editing.value?.opType ? { ...row, description, reviser: user, reviseDate: now, dataStamp: (row.dataStamp ?? 0) + 1 } : row)
      } else {
        rows.value.unshift({ opType, description, creator: user, createDate: now, issueState: 0, dataStamp: 0, reviser: '', reviseDate: '' })
      }
      persistMock()
    } else if (dialogMode.value === 'edit' && editing.value) {
      await api('/api/operation-types/' + encodeURIComponent(editing.value.opType), { method: 'PUT', body: JSON.stringify({ description, dataStamp: editing.value.dataStamp }) })
    } else {
      await api('/api/operation-types', { method: 'POST', body: JSON.stringify({ opType, description }) })
    }
    await loadRows(); selected.value = opType; notice.value = mock.value ? '已儲存於瀏覽器 DEV 假資料。' : '作業站類別已儲存。'
    const refreshed = rows.value.find((row) => row.opType === opType)
    if (dialogMode.value === 'edit' && refreshed) editing.value = refreshed
    if (closeAfter) closeDialog()
    else { draft.value.opType = opType; error.value = '' }
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '儲存作業站類別失敗。' }
  finally { busy.value = false }
}
async function deleteSelected(): Promise<void> {
  const targets = selectedRows.value.length ? [...selectedRows.value] : selectedRow.value ? [selectedRow.value] : []
  if (!targets.length) { notice.value = '請先選取要刪除的作業站類別。'; return }
  if (targets.some((row) => row.issueState === 1)) { error.value = '簽核中的資料不可刪除。'; return }
  if (!window.confirm('確定刪除/停用 ' + targets.length + ' 筆作業站類別？')) return
  busy.value = true; error.value = ''; notice.value = ''
  try {
    for (const row of targets) {
      if (mock.value) {
        if (row.issueState === 2) rows.value = rows.value.map((item) => item.opType === row.opType ? { ...item, issueState: -1, reviser: props.currentUserNo || 'DEVUSER', reviseDate: new Date().toISOString(), dataStamp: (item.dataStamp ?? 0) + 1 } : item)
        else rows.value = rows.value.filter((item) => item.opType !== row.opType)
        localStorage.removeItem('ymes.dev.operation.' + row.opType + '.property')
      } else {
        await api('/api/operation-types/' + encodeURIComponent(row.opType), { method: 'DELETE', body: JSON.stringify({ issueState: row.issueState, dataStamp: row.dataStamp }) })
      }
    }
    if (mock.value) persistMock()
    selected.value = ''; selectedKeys.value = []; await loadRows(); notice.value = '已完成 ' + targets.length + ' 筆刪除/停用。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '刪除作業站類別失敗。' }
  finally { busy.value = false }
}
async function approveSelected(): Promise<void> {
  const targets = selectedRows.value.length ? [...selectedRows.value] : selectedRow.value ? [selectedRow.value] : []
  if (!targets.length) { notice.value = '請先選取要核准的作業站類別。'; return }
  if (targets.some((row) => row.issueState !== 0)) { error.value = '僅未簽核資料可送核准。'; return }
  busy.value = true; error.value = ''; notice.value = ''
  try {
    for (const row of targets) {
      if (mock.value) rows.value = rows.value.map((item) => item.opType === row.opType ? { ...item, issueState: 2, reviser: props.currentUserNo || 'DEVUSER', reviseDate: new Date().toISOString(), dataStamp: (item.dataStamp ?? 0) + 1 } : item)
      else await api('/api/operation-types/' + encodeURIComponent(row.opType) + '/approve', { method: 'POST', body: JSON.stringify({ dataStamp: row.dataStamp }) })
    }
    if (mock.value) persistMock()
    selected.value = ''; selectedKeys.value = []; await loadRows(); notice.value = '已送出 ' + targets.length + ' 筆核准作業。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '核准作業站類別失敗。' }
  finally { busy.value = false }
}
function openProperty(): void {
  if (!selectedRow.value) { notice.value = '請先選取一筆作業站類別。'; return }
  if (!mock.value) { error.value = 'Property 子功能的正式 API 尚未完成。'; return }
  propertyTarget.value = selectedRow.value.opType; propertyOpen.value = true
}

watch(issueState, () => { selected.value = ''; selectedKeys.value = [] })
onMounted(() => { void loadRows() })
</script>

<template>
  <div class="operation-type-page">
    <header class="page-title"><h1>作業站類別資料</h1><span>OP TYPE · TBLOPTYPE</span></header>
    <form class="query-panel" @submit.prevent="loadRows">
      <fieldset class="state-filter"><legend>作業站類別</legend>
        <label v-for="state in [{value:0,label:'未簽核'},{value:1,label:'簽核中'},{value:2,label:'已簽核'},{value:-1,label:'不使用'}]" :key="state.value">
          <input v-model.number="issueState" type="radio" name="op-type-state" :value="state.value" />{{ state.label }}
        </label>
      </fieldset>
      <label class="search-field"><span>OP Type</span><input v-model="keyword" class="kd-field" maxlength="20" /></label><span class="wildcard">%</span>
      <button class="button" type="submit" :disabled="loading || busy">查詢</button>
    </form>
    <p v-if="error" class="message error-message" role="alert">{{ error }}</p>
    <p v-if="notice" class="message" role="status">{{ notice }}</p>
    <p v-if="mock" class="dev-note">開發區本機假資料：只存於目前瀏覽器 localStorage，不會寫入 MES 或 YMES 資料庫。</p>
    <section class="master-screen">
      <div class="grid-panel">
        <div class="table-scroll"><table class="data-grid">
          <thead><tr><th>OP Type</th><th>Description</th><th>Creator</th><th>Create Date</th><th>Reviser</th><th>Revise Date</th></tr></thead>
          <tbody><tr v-for="row in visibleRows" :key="row.opType" :class="{ selected: selected === row.opType }" :aria-selected="selected === row.opType" tabindex="0" @click="selectRow(row, $event)" @keydown.enter.prevent="selectRow(row)" @dblclick="row.issueState === 0 && openDialog('edit', row)">
            <td>{{ row.opType }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ displayDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ displayDate(row.reviseDate) }}</td>
          </tr><tr v-if="!loading && !visibleRows.length"><td colspan="6" class="empty-cell">沒有符合條件的作業站類別</td></tr><tr v-if="loading && !visibleRows.length"><td colspan="6" class="empty-cell">資料載入中…</td></tr></tbody>
        </table></div>
        <footer class="grid-footer">資料總筆數 {{ visibleRows.length }}</footer>
      </div>
      <aside class="action-panel">
        <button class="button primary" :disabled="busy" @click="openDialog('new')">新增(A)</button>
        <button class="button" :disabled="busy || selectedRow?.issueState !== 0" @click="openDialog('edit', selectedRow)">修改(E)</button>
        <button class="button" :disabled="busy || (!selectedRows.length && !selectedRow)" @click="deleteSelected">刪除(D)</button>
        <button class="button" :disabled="busy || !selectedRow" @click="openDialog('copy', selectedRow)">複製(C)</button>
        <button class="button" :disabled="busy || (!selectedRows.length && selectedRow?.issueState !== 0)" @click="approveSelected">核准</button>
        <button class="button" :disabled="busy || !selectedRow" @click="openProperty">Property</button>
        <button class="button close-grid" @click="emit('close')">關閉</button>
      </aside>
    </section>

    <div v-if="dialogOpen" class="dialog-backdrop" @click.self="closeDialog">
      <section class="definition-dialog" role="dialog" aria-modal="true" aria-labelledby="op-type-dialog-title">
        <h2 id="op-type-dialog-title">作業站類別資料 {{ dialogMode === 'new' ? '(新增)' : dialogMode === 'edit' ? '(修改)' : '(複製)' }}</h2>
        <fieldset class="data-group"><legend>OP Type Data</legend>
          <label class="form-row"><span>OP Type</span><input v-model="draft.opType" class="kd-field" maxlength="20" :readonly="dialogMode === 'edit'" @blur="draft.opType = draft.opType.trim().toUpperCase()" /></label>
          <label class="form-row description-row"><span>Description</span><textarea v-model="draft.description" maxlength="255" /></label>
        </fieldset>
        <fieldset class="audit-group"><legend>建立/修改資訊</legend><label><span>建立人</span><input :value="dialogMode === 'edit' ? editing?.creator : currentUserNo" disabled /></label><label><span>建立日期</span><input :value="dialogMode === 'edit' ? displayDate(editing?.createDate ?? '') : '儲存時由伺服器建立'" disabled /></label><label v-if="dialogMode === 'edit'"><span>修改人</span><input :value="currentUserNo" disabled /></label><label v-if="dialogMode === 'edit'"><span>修改日期</span><input :value="displayDate(new Date().toISOString())" disabled /></label></fieldset>
        <p v-if="error" class="message error-message" role="alert">{{ error }}</p>
        <footer class="dialog-actions"><button class="button primary" :disabled="busy" @click="saveForm(true)">確定</button><button class="button" :disabled="busy" @click="saveForm(false)">套用</button><button class="button" :disabled="busy" @click="closeDialog">關閉</button></footer>
      </section>
    </div>
    <OperationFeatureDialog v-if="propertyOpen" feature="property" :operation-no="propertyTarget" :operation-name="'作業站類別 ' + propertyTarget" @close="propertyOpen = false" @saved="notice = 'Property DEV 假資料已儲存。'" />
  </div>
</template>

<style scoped>
.operation-type-page{min-height:100%;padding:0 5px 12px;color:#353535;font:14px Arial,"Microsoft JhengHei",sans-serif}.page-title{display:flex;align-items:center;gap:14px;min-height:58px;padding:0 24px;border-bottom:1px solid #dedbd3;background:#f7f5ef}.page-title h1{margin:0;color:#342d22;font-size:21px;font-weight:600}.page-title>span{color:#827b70;font-size:12px}.query-panel{display:flex;align-items:center;gap:8px;min-height:48px;margin-top:8px;padding:5px 10px;border:1px solid #d2d2d2;background:#fff}.state-filter{display:flex;align-items:center;gap:12px;margin:0;padding:2px 7px;border:0}.state-filter legend{position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0)}.state-filter label,.search-field{display:flex;align-items:center;gap:5px;white-space:nowrap}.state-filter input{accent-color:#e89a1c}.search-field{margin-left:8px}.kd-field{height:29px;border:1px solid #929292;border-radius:2px;padding:4px 7px;background:#fff0b0;color:#303030;font:inherit}.kd-field:focus{outline:2px solid #e0a031;outline-offset:1px}.search-field .kd-field{width:190px}.wildcard{margin-left:-5px}.button{min-height:31px;padding:4px 12px;border:1px solid #bbb;border-radius:4px;background:linear-gradient(#fff,#f3f3f3);color:#333;cursor:pointer;font:inherit;white-space:nowrap}.button:hover:not(:disabled){border-color:#dd9b2d;background:#fff8e7}.button:disabled{color:#aaa;cursor:not-allowed}.button.primary{border-color:#f1a32c;background:#ff9f1c;color:#fff}.message{margin:6px 0 0;padding:7px 9px;color:#62522e;background:#fff9e8}.error-message{color:#8c251e;background:#fff0ed}.dev-note{margin:6px 0 0;padding:6px 9px;color:#695a35;background:#fff9e8;font-size:12px}.master-screen{display:grid;grid-template-columns:minmax(0,1fr) 96px;gap:6px;min-height:calc(100vh - 190px);margin-top:7px;border:1px solid #ccc;padding:6px;background:#fff}.grid-panel{display:flex;min-height:calc(100vh - 204px);min-width:0;flex-direction:column}.action-panel{display:flex;flex-direction:column;gap:8px;padding-left:2px}.action-panel .button{width:88px}.table-scroll{flex:1;min-height:300px;overflow:auto;border:1px solid #d0d0d0}.data-grid{width:100%;border-collapse:collapse;white-space:nowrap}.data-grid th,.data-grid td{padding:5px 8px;border:1px solid #d0d0d0;text-align:left}.data-grid th{position:sticky;top:0;z-index:1;background:#e7e9eb;color:#383838;font-weight:600}.data-grid tbody tr{cursor:pointer}.data-grid tbody tr:hover{background:#fff8e7}.data-grid tbody tr.selected{background:#ffecb4}.empty-cell{padding:22px!important;color:#777;text-align:center!important}.grid-footer{padding:6px 2px 0;color:#666;font-size:12px}.close-grid{margin-top:auto}.dialog-backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:14px;background:#17161166}.definition-dialog{width:min(497px,96vw);padding:10px 12px 12px;border:1px solid #aaa;background:#fff;box-shadow:0 12px 32px #0003}.definition-dialog h2{margin:0 0 7px;font-size:16px;font-weight:600}.data-group{min-height:137px;margin:0;padding:7px 10px 9px;border:1px solid #bcbcbc}.data-group legend,.audit-group legend{padding:0 5px}.form-row{display:grid;grid-template-columns:88px 1fr;align-items:start;gap:8px;margin:6px 0}.form-row>span{text-align:right;padding-top:5px}.form-row>.kd-field{width:152px}.description-row textarea{height:64px;resize:none;border:1px solid #888;padding:5px 7px;font:inherit}.audit-group{display:grid;grid-template-columns:1fr 1fr;gap:5px 8px;margin:7px 0;padding:7px;border:1px solid #bcbcbc}.audit-group label{display:grid;grid-template-columns:70px minmax(0,1fr);align-items:center;gap:5px;font-size:12px}.audit-group label>span{text-align:right}.audit-group input{width:100%;height:25px;border:1px solid #bbb;padding:3px 5px;background:#eee;color:#666;font:inherit}.dialog-actions{display:flex;justify-content:flex-end;gap:6px;margin-top:8px}.dialog-actions .primary{margin-left:auto}@media(max-width:760px){.query-panel{flex-wrap:wrap}.state-filter{flex-wrap:wrap}.master-screen{grid-template-columns:1fr;min-height:65vh}.grid-panel{min-height:55vh}.action-panel{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));padding:0}.action-panel .button{width:100%}.close-grid{margin:0}.audit-group{grid-template-columns:1fr}}
</style>
