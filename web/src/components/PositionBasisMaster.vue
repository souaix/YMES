<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

interface PositionRow {
  pdLineNo: string
  positionNo: string
  description: string | null
  creator: string
  createDate: string
  reviser: string | null
  reviseDate: string | null
  dataStamp: number | null
}
interface PositionForm { pdLineNo: string; positionNo: string; description: string }
interface LookupRow { value: string; label: string; detail: string }
type FormMode = 'add' | 'edit' | 'copy'

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mockMode = computed(() => import.meta.env.DEV && props.developmentPreview)
const collection = 'api/smd/pd-line-positions'
const storageKey = 'ymes-dev-mock:smd-pd-line-position-basis:v1'
const seedRows: PositionRow[] = [
  { pdLineNo: 'DEV-OP-01', positionNo: 'ST01', description: '開發用作業站 01', creator: 'DEV001', createDate: '2026-11-10T08:30:00+08:00', reviser: 'DEV001', reviseDate: '2026-11-10T08:30:00+08:00', dataStamp: 1 },
  { pdLineNo: 'DEV-OP-01', positionNo: 'ST02', description: '開發用作業站 02', creator: 'DEV001', createDate: '2026-11-10T08:40:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
  { pdLineNo: 'DEV-OP-02', positionNo: 'PACK-01', description: '包裝站', creator: 'DEV002', createDate: '2026-11-11T10:00:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
]
const rows = ref<PositionRow[]>([])
const queryLine = ref('')
const queryPosition = ref('')
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const selectedKeys = ref<string[]>([])
const activeKey = ref('')
const selectionAnchor = ref('')
const formMode = ref<FormMode | null>(null)
const form = ref<PositionForm>(blankForm())
const original = ref<PositionRow | null>(null)
const formDataStamp = ref<number | null>(null)
const audit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const formError = ref('')
const pickerMode = ref<'area' | 'esop' | null>(null)
const pickerRows = ref<LookupRow[]>([])
const pickerError = ref('')
const esopEnabled = ref(false)

function blankForm(): PositionForm { return { pdLineNo: '', positionNo: '', description: '' } }
function rowKey(row: Pick<PositionRow, 'pdLineNo' | 'positionNo'>): string { return JSON.stringify([row.pdLineNo, row.positionNo]) }
function dateNow(): string { return new Date().toISOString() }
function dataStampQuery(value: number | null): string { return value === null ? 'dataStamp=0&dataStampIsNull=true' : 'dataStamp=' + encodeURIComponent(String(value)) }
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function requestJson<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init })
  if (response.status === 401) { window.location.assign(appBase + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    if (response.status === 404) throw new Error('工位基本資料 API 尚未建立；請依 todolist_homepc.md 完成 SMD SD109。')
    throw new Error(problem?.detail || problem?.message || problem?.title || ('API 回應錯誤 (' + response.status + ')。'))
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function readMockRows(): PositionRow[] {
  try { const value = window.localStorage.getItem(storageKey); return value ? JSON.parse(value) as PositionRow[] : structuredClone(seedRows) }
  catch { return structuredClone(seedRows) }
}
function writeMockRows(value: PositionRow[]): void { window.localStorage.setItem(storageKey, JSON.stringify(value)) }
const visibleRows = computed(() => rows.value.slice().sort((a, b) => a.pdLineNo.localeCompare(b.pdLineNo) || a.positionNo.localeCompare(b.positionNo)))
const activeRow = computed(() => rows.value.find(row => rowKey(row) === activeKey.value) ?? null)
const selectedRows = computed(() => rows.value.filter(row => selectedKeys.value.includes(rowKey(row))))
const isEditing = computed(() => formMode.value === 'edit')
const formTitle = computed(() => formMode.value === 'add' ? '作業站基本資料 (新增)' : formMode.value === 'copy' ? '作業站基本資料 (複製)' : '作業站基本資料 (修改)')

async function loadRows(): Promise<void> {
  loading.value = true; errorMessage.value = ''
  try {
    const line = queryLine.value.trim()
    const position = queryPosition.value.trim()
    if (mockMode.value) {
      rows.value = readMockRows().filter(row =>
        (!line || row.pdLineNo.toLocaleLowerCase().includes(line.toLocaleLowerCase())) &&
        (!position || row.positionNo.toLocaleLowerCase().includes(position.toLocaleLowerCase())))
    } else {
      const query = new URLSearchParams()
      if (line) query.set('pdLineNo', line)
      if (position) query.set('positionNo', position)
      const queryString = query.toString()
      rows.value = await requestJson<PositionRow[]>(collection + (queryString ? '?' + queryString : ''))
    }
    selectedKeys.value = []
    activeKey.value = ''
    selectionAnchor.value = ''
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '工位資料載入失敗。' }
  finally { loading.value = false }
}
async function loadEsopOption(): Promise<void> {
  if (mockMode.value) { esopEnabled.value = true; return }
  try {
    const options = await requestJson<{ executeEsop: boolean }>(collection + '/options')
    esopEnabled.value = options.executeEsop
  } catch { esopEnabled.value = false }
}
function selectRow(row: PositionRow, event: MouseEvent): void {
  const key = rowKey(row)
  const current = visibleRows.value.findIndex(item => rowKey(item) === key)
  if (event.shiftKey && selectionAnchor.value) {
    const anchor = visibleRows.value.findIndex(item => rowKey(item) === selectionAnchor.value)
    if (anchor >= 0) selectedKeys.value = visibleRows.value.slice(Math.min(anchor, current), Math.max(anchor, current) + 1).map(rowKey)
  } else if (event.ctrlKey || event.metaKey) {
    selectedKeys.value = selectedKeys.value.includes(key) ? selectedKeys.value.filter(item => item !== key) : [...selectedKeys.value, key]
    selectionAnchor.value = key
  } else {
    selectedKeys.value = [key]
    selectionAnchor.value = key
  }
  activeKey.value = key
}
function openForm(mode: FormMode): void {
  if ((mode === 'edit' || mode === 'copy') && !activeRow.value) return
  const source = activeRow.value
  original.value = mode === 'edit' ? source : null
  formDataStamp.value = mode === 'edit' ? source?.dataStamp ?? null : null
  formMode.value = mode
  formError.value = ''
  form.value = source && mode !== 'add'
    ? { pdLineNo: source.pdLineNo, positionNo: source.positionNo, description: source.description ?? '' }
    : blankForm()
  audit.value = mode === 'edit' && source
    ? { creator: source.creator, createDate: source.createDate, reviser: source.reviser ?? '', reviseDate: source.reviseDate ?? '' }
    : { creator: props.currentUserNo || 'DEV001', createDate: dateNow(), reviser: '', reviseDate: '' }
}
function closeForm(): void { formMode.value = null; original.value = null; formError.value = '' }
function formatDate(value: string | null | undefined): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat('zh-TW', { dateStyle: 'short', timeStyle: 'medium', hour12: false }).format(date)
}
async function openPicker(kind: 'area' | 'esop'): Promise<void> {
  pickerMode.value = kind; pickerError.value = ''; pickerRows.value = []
  try {
    if (mockMode.value) {
      pickerRows.value = kind === 'area'
        ? [{ value: 'DEV-OP-01', label: 'DEV-OP-01', detail: '開發用作業區' }, { value: 'DEV-OP-02', label: 'DEV-OP-02', detail: '包裝作業區' }]
        : [{ value: 'ST01', label: 'ST01', detail: '切割工位' }, { value: 'ST02', label: 'ST02', detail: '檢查工位' }, { value: 'PACK-01', label: 'PACK-01', detail: '包裝工位' }]
    } else if (kind === 'area') {
      const options = await requestJson<{ areaNo: string; areaName: string }[]>('api/smd/areas?areaClass=0&areaType=0&issueState=2')
      pickerRows.value = options.map(row => ({ value: row.areaNo, label: row.areaNo, detail: row.areaName }))
    } else {
      const options = await requestJson<{ positionNo: string; stationName?: string }[]>(collection + '/esop-options?pdLineNo=' + encodeURIComponent(form.value.pdLineNo))
      pickerRows.value = options.map(row => ({ value: row.positionNo, label: row.positionNo, detail: row.stationName ?? '' }))
    }
  } catch (error) { pickerError.value = error instanceof Error ? error.message : '選項載入失敗。' }
}
function chooseLookup(row: LookupRow): void {
  if (pickerMode.value === 'area') form.value.pdLineNo = row.value
  if (pickerMode.value === 'esop') form.value.positionNo = row.value.toUpperCase()
  pickerMode.value = null
}
async function saveForm(closeAfter: boolean): Promise<void> {
  if (!formMode.value || busy.value) return
  formError.value = ''
  form.value.pdLineNo = form.value.pdLineNo.trim().toUpperCase()
  form.value.positionNo = form.value.positionNo.trim().toUpperCase()
  form.value.description = form.value.description.trim()
  if (!form.value.pdLineNo) { formError.value = '請選擇作業區編號。'; return }
  if (!form.value.positionNo) { formError.value = '請輸入工位編號。'; return }
  if (form.value.pdLineNo.length > 50 || form.value.positionNo.length > 50 || form.value.description.length > 50) { formError.value = '欄位長度不可超過 50 個字元。'; return }
  busy.value = true
  try {
    const mode = formMode.value
    if (mockMode.value) {
      const all = readMockRows()
      const duplicate = all.some(row => row.pdLineNo === form.value.pdLineNo && row.positionNo === form.value.positionNo &&
        !(mode === 'edit' && original.value && rowKey(row) === rowKey(original.value)))
      if (duplicate) throw new Error('相同作業區與工位編號已存在。')
      const now = dateNow()
      const saved: PositionRow = mode === 'edit' && original.value
        ? { ...original.value, description: form.value.description, reviser: props.currentUserNo || 'DEV001', reviseDate: now, dataStamp: (formDataStamp.value ?? 0) + 1 }
        : { ...form.value, creator: props.currentUserNo || 'DEV001', createDate: now, reviser: null, reviseDate: null, dataStamp: 0 }
      const next = mode === 'edit' && original.value
        ? all.map(row => rowKey(row) === rowKey(original.value!) ? saved : row)
        : [...all, saved]
      writeMockRows(next)
    } else if (mode === 'edit' && original.value) {
      await requestJson(collection + '/' + encodeURIComponent(original.value.pdLineNo) + '/' + encodeURIComponent(original.value.positionNo) + '?' + dataStampQuery(formDataStamp.value), {
        method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ description: form.value.description }),
      })
    } else {
      await requestJson(collection, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(form.value) })
    }
    queryLine.value = form.value.pdLineNo
    queryPosition.value = form.value.positionNo
    await loadRows()
    if (closeAfter) closeForm()
    else {
      formMode.value = 'edit'
      const refreshed = rows.value.find(row => row.pdLineNo === form.value.pdLineNo && row.positionNo === form.value.positionNo)
      if (refreshed) {
        original.value = refreshed; formDataStamp.value = refreshed.dataStamp
        audit.value = { creator: refreshed.creator, createDate: refreshed.createDate, reviser: refreshed.reviser ?? '', reviseDate: refreshed.reviseDate ?? '' }
      }
    }
  } catch (error) { formError.value = error instanceof Error ? error.message : '工位資料儲存失敗。' }
  finally { busy.value = false }
}
async function deleteSelected(): Promise<void> {
  if (!activeRow.value) return
  if (!selectedKeys.value.includes(activeKey.value)) selectedKeys.value = [activeKey.value]
  const targets = selectedRows.value.slice().reverse()
  if (!targets.length || !window.confirm('確定刪除選取的 ' + targets.length + ' 筆工位資料？')) return
  busy.value = true; errorMessage.value = ''
  try {
    if (mockMode.value) {
      const keys = new Set(targets.map(rowKey))
      writeMockRows(readMockRows().filter(row => !keys.has(rowKey(row))))
    } else {
      for (const row of targets) await requestJson(collection + '/' + encodeURIComponent(row.pdLineNo) + '/' + encodeURIComponent(row.positionNo) + '?' + dataStampQuery(row.dataStamp), { method: 'DELETE' })
    }
    await loadRows()
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '工位資料刪除失敗，已依序停止後續刪除。'; await loadRows() }
  finally { busy.value = false }
}
onMounted(() => { void loadRows(); void loadEsopOption() })
</script>

<template>
  <section class="position-page">
    <header class="page-title"><h1>工位基本資料</h1></header>
    <div class="query-strip">
      <label><span>作業區編號</span><input v-model="queryLine" class="kd-input" @keyup.enter="loadRows"></label>
      <label><span>工位編號</span><input v-model="queryPosition" class="kd-input" @keyup.enter="loadRows"></label>
      <button class="win-btn" :disabled="loading" @click="loadRows">{{ loading ? '查詢中' : '查詢' }}</button>
    </div>
    <div v-if="errorMessage" class="message error">{{ errorMessage }}</div>
    <main class="work-area">
      <div class="grid-wrap">
        <table>
          <thead><tr><th>作業區編號</th><th>工位編號</th><th>說明</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th></tr></thead>
          <tbody>
            <tr v-for="row in visibleRows" :key="rowKey(row)" :class="{ selected: selectedKeys.includes(rowKey(row)), active: activeKey === rowKey(row) }" @click="selectRow(row, $event)" @dblclick="openForm('edit')">
              <td>{{ row.pdLineNo }}</td><td>{{ row.positionNo }}</td><td class="description">{{ row.description }}</td>
              <td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td>
            </tr>
            <tr v-if="!loading && visibleRows.length === 0"><td colspan="7" class="empty">沒有符合條件的資料</td></tr>
          </tbody>
        </table>
      </div>
      <aside class="actions">
        <button class="win-btn primary" :disabled="busy" @click="openForm('add')">新增(A)</button>
        <button class="win-btn" :disabled="busy || !activeRow" @click="openForm('edit')">修改(E)</button>
        <button class="win-btn" :disabled="busy || !activeRow" @click="openForm('copy')">複製(C)</button>
        <button class="win-btn danger" :disabled="busy || !activeRow" @click="deleteSelected">刪除(D)</button>
        <button class="win-btn close" @click="emit('close')">關閉</button>
      </aside>
    </main>
    <footer class="status-line"><span>資料總筆數 {{ visibleRows.length }}</span><span>使用者：{{ currentUserNo || '—' }}</span></footer>

    <div v-if="formMode" class="backdrop" @click.self="closeForm">
      <form class="editor" @submit.prevent="saveForm(true)">
        <header class="editor-title"><h2>{{ formTitle }}</h2><button type="button" class="x-button" @click="closeForm">×</button></header>
        <fieldset class="main-fields"><legend>作業站主檔資料</legend>
          <label class="field-row"><span>作業區編號</span><div class="picker-row"><input v-model="form.pdLineNo" class="kd-input" readonly :disabled="isEditing"><button type="button" class="win-btn picker" :disabled="isEditing" @click="openPicker('area')">...</button></div></label>
          <label class="field-row"><span>工位編號</span><div class="picker-row"><input v-model.trim="form.positionNo" class="kd-input" maxlength="50" :readonly="isEditing" @blur="form.positionNo = form.positionNo.toUpperCase()"><button type="button" class="win-btn picker" :disabled="isEditing || !form.pdLineNo || !esopEnabled" title="從 eSOP 取得工位" @click="openPicker('esop')">...</button></div></label>
          <label class="field-row description-row"><span>說明</span><textarea v-model="form.description" class="kd-input" maxlength="50"></textarea></label>
        </fieldset>
        <fieldset class="audit"><legend>資料建立 / 修改資訊</legend>
          <label>建立人<input :value="audit.creator" readonly></label><label>建立日期<input :value="formatDate(audit.createDate)" readonly></label>
          <label>修改人<input :value="audit.reviser" readonly></label><label>修改日期<input :value="formatDate(audit.reviseDate)" readonly></label>
        </fieldset>
        <p v-if="formError" class="message error">{{ formError }}</p>
        <footer class="editor-actions"><button class="win-btn primary" :disabled="busy">確定</button><button type="button" class="win-btn" :disabled="busy" @click="closeForm">關閉</button><button type="button" class="win-btn" :disabled="busy" @click="saveForm(false)">套用</button></footer>
      </form>
    </div>
    <div v-if="pickerMode" class="backdrop nested" @click.self="pickerMode = null">
      <section class="picker-dialog">
        <header class="editor-title"><h2>{{ pickerMode === 'area' ? '選擇已核准作業區' : '選擇 eSOP 工位' }}</h2><button class="x-button" @click="pickerMode = null">×</button></header>
        <p v-if="pickerError" class="message error">{{ pickerError }}</p>
        <table><thead><tr><th>編號</th><th>名稱 / 說明</th></tr></thead><tbody>
          <tr v-for="row in pickerRows" :key="row.value" @dblclick="chooseLookup(row)" @click="chooseLookup(row)"><td>{{ row.label }}</td><td>{{ row.detail }}</td></tr>
          <tr v-if="!pickerError && pickerRows.length === 0"><td colspan="2" class="empty">沒有可選資料</td></tr>
        </tbody></table>
      </section>
    </div>
  </section>
</template>

<style scoped>
.position-page{height:100%;min-height:0;display:flex;flex-direction:column;background:#fff;color:#292929;font:14px "Microsoft JhengHei",Arial,sans-serif}.page-title{height:50px;flex:none;display:flex;align-items:center;padding:0 24px;border-bottom:1px solid #ddd8ce;background:#f7f5ef}.page-title h1{margin:0;font-size:20px;font-weight:500}.query-strip{min-height:48px;display:flex;align-items:center;gap:10px;padding:6px 12px;border-bottom:1px solid #ddd}.query-strip label{display:flex;align-items:center;gap:8px}.query-strip label span{white-space:nowrap}.kd-input{box-sizing:border-box;height:28px;min-width:160px;border:1px solid #999;border-radius:0;background:#ffffc0;padding:4px 7px;font:inherit;color:#222}.kd-input:read-only,.kd-input:disabled{background:#eee}.win-btn{min-height:30px;padding:4px 12px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#f2f0ec);color:#222;font:inherit;white-space:nowrap;cursor:pointer}.win-btn:hover:not(:disabled){border-color:#e59a36;background:#fff8e8}.win-btn:disabled{opacity:.5;cursor:default}.win-btn.primary{border-color:#e99622;background:#f7a52a;color:white;font-weight:600}.win-btn.danger{color:#8e3030}.win-btn.close{margin-top:auto}.message{margin:5px 12px;padding:6px 9px;border:1px solid #ddd}.message.error{color:#8d2222;background:#fff3f1}.work-area{min-height:0;flex:1;display:grid;grid-template-columns:minmax(0,1fr) 92px;gap:10px;padding:10px 12px 5px}.grid-wrap{min-width:0;overflow:auto;border:1px solid #aaa}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:13px}th,td{height:29px;padding:3px 8px;border-right:1px solid #c9c9c9;border-bottom:1px solid #c9c9c9;text-align:left}th{position:sticky;top:0;background:#e8e6e0;font-weight:500;z-index:1}tbody tr{cursor:pointer}tbody tr:hover{background:#fff8e1}tbody tr.selected{background:#fff1c2}tbody tr.active{outline:1px solid #d89b34;outline-offset:-1px}.description{white-space:normal;min-width:180px}.empty{text-align:center;color:#777;height:60px}.actions{display:flex;flex-direction:column;gap:7px}.status-line{flex:none;height:25px;display:flex;align-items:center;justify-content:space-between;padding:0 12px;border-top:1px solid #ccc;color:#666;font-size:12px}.backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:12px;background:#0005}.backdrop.nested{z-index:1010}.editor,.picker-dialog{box-sizing:border-box;width:min(511px,96vw);max-height:95vh;display:flex;flex-direction:column;border:1px solid #999;background:#f9f8f4;box-shadow:0 8px 28px #0004}.editor{height:312px}.editor-title{height:36px;flex:none;display:flex;align-items:center;justify-content:space-between;padding:0 10px;border-bottom:1px solid #ccc;background:#f1efe9}.editor-title h2{margin:0;font-size:15px;font-weight:500}.x-button{border:0;background:transparent;font-size:24px;line-height:1;color:#555;cursor:pointer}.main-fields{height:168px;flex:none;margin:9px 10px 5px;padding:7px 9px;border:1px solid #aaa}.main-fields legend,.audit legend{padding:0 5px;color:#555}.field-row{display:grid;grid-template-columns:95px minmax(0,1fr);align-items:center;gap:8px;min-height:29px}.field-row>span{text-align:right}.picker-row{display:flex;gap:5px}.picker-row input{min-width:0;flex:1}.win-btn.picker{min-width:34px;padding:2px 6px}.description-row{align-items:start;padding-top:5px}.description-row textarea{height:59px;resize:none}.audit{height:55px;flex:none;display:grid;grid-template-columns:1fr 1fr;gap:4px 9px;margin:0 10px;padding:4px 8px;border:1px solid #aaa}.audit label{display:grid;grid-template-columns:54px minmax(0,1fr);align-items:center;gap:4px;font-size:11px;white-space:nowrap}.audit input{min-width:0;width:100%;height:20px;box-sizing:border-box;border:1px solid #bbb;background:#eee;padding:2px 4px}.editor-actions{display:flex;justify-content:flex-end;gap:7px;margin-top:auto;padding:5px 10px 7px}.editor .message{margin:1px 10px}.picker-dialog{width:min(560px,96vw);padding-bottom:10px}.picker-dialog table{margin:8px 10px 0;width:calc(100% - 20px);border:1px solid #aaa}.picker-dialog tbody tr:hover{background:#fff1c2}@media(max-width:760px){.query-strip{flex-wrap:wrap}.work-area{grid-template-columns:1fr}.actions{display:grid;grid-template-columns:repeat(4,minmax(70px,1fr))}.win-btn.close{margin-top:0}.audit{grid-template-columns:1fr 1fr}.editor{height:auto;min-height:312px}.main-fields{height:auto}.query-strip label{flex:1}.kd-input{min-width:100px}}
</style>
