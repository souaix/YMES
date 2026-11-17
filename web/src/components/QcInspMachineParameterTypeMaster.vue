<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

type IssueState = -1 | 0 | 1 | 2
type EditMode = 'add' | 'edit' | 'copy' | null
interface InspectionTypeRow {
  inspMachineType: string; inspMachineTypeName: string | null; inspType: number
  baudRate: number | null; parityBit: string | null; dataBit: number | null; stopBit: number | null
  endCode: string | null; format1: string | null; format2: string | null; chNo: string | null
  inspDriverNo: string | null; description: string | null; creator: string | null; createDate: string | null
  issueState: IssueState; dataStamp: number | null; reviser: string | null; reviseDate: string | null
}
interface DriverRow { inspDriverNo: string; functionName?: string | null; description?: string | null }
interface Envelope<T> { items?: T[]; rows?: T[]; data?: T[] }
interface EditorValue {
  inspMachineType: string; inspMachineTypeName: string; inspType: number
  baudRate: string; parityBit: string; dataBit: string; stopBit: string; endCode: string
  format1: string; format2: string; chNo: string; inspDriverNo: string; description: string
}

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes-dev-mock:qc-insp-machine-parameter-type:v1'
const statuses: { value: IssueState; label: string }[] = [
  { value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' },
]
const baudRates = ['600', '1200', '2400', '4800', '9600', '19200']
const parityOptions = ['None', 'Odd', 'Even', 'Mark']
const bitOptions = ['7', '8']
const stopOptions = ['1', '2']
const endCodes = ['0', '3', '10', '13']
const demoRows: InspectionTypeRow[] = [
  { inspMachineType: 'DEV-RS232-01', inspMachineTypeName: 'DEV RS232 電子秤', inspType: 0, baudRate: 9600, parityBit: 'None', dataBit: 8, stopBit: 1, endCode: '13', format1: 'WT,####.##', format2: 'NA', chNo: 'NA', inspDriverNo: null, description: 'DEV 範例：RS232 串列檢測儀器類別。', creator: 'DEV001', createDate: '2026-11-10T08:00:00+08:00', issueState: 0, dataStamp: 0, reviser: null, reviseDate: null },
  { inspMachineType: 'DEV-RS232-02', inspMachineTypeName: 'DEV 溫度計', inspType: 0, baudRate: 19200, parityBit: 'Even', dataBit: 7, stopBit: 2, endCode: '10', format1: 'TEMP,##.##', format2: '01', chNo: '01', inspDriverNo: null, description: 'DEV 簽核中樣本。', creator: 'DEV002', createDate: '2026-11-10T08:30:00+08:00', issueState: 1, dataStamp: 1, reviser: 'DEV001', reviseDate: '2026-11-10T09:00:00+08:00' },
  { inspMachineType: 'DEV-USER-01', inspMachineTypeName: 'DEV 自訂量測儀', inspType: 1, baudRate: null, parityBit: null, dataBit: null, stopBit: null, endCode: null, format1: null, format2: null, chNo: null, inspDriverNo: 'DEV-DRIVER-01', description: 'DEV 範例：以 UserDefine Driver 解析資料。', creator: 'DEV001', createDate: '2026-11-11T08:00:00+08:00', issueState: 2, dataStamp: 2, reviser: 'DEV002', reviseDate: '2026-11-12T10:00:00+08:00' },
  { inspMachineType: 'DEV-UNUSED-01', inspMachineTypeName: 'DEV 停用儀器類別', inspType: 1, baudRate: null, parityBit: null, dataBit: null, stopBit: null, endCode: null, format1: null, format2: null, chNo: null, inspDriverNo: 'DEV-DRIVER-02', description: 'DEV 不使用狀態範例。', creator: 'DEV001', createDate: '2026-11-12T08:00:00+08:00', issueState: -1, dataStamp: 3, reviser: 'DEV001', reviseDate: '2026-11-12T09:00:00+08:00' },
]
const demoDrivers: DriverRow[] = [
  { inspDriverNo: 'DEV-DRIVER-01', functionName: 'ParseMeasurement', description: 'DEV 數值解析範例' },
  { inspDriverNo: 'DEV-DRIVER-02', functionName: 'ParseDimension', description: 'DEV 尺寸解析範例' },
]
const rows = ref<InspectionTypeRow[]>([])
const issueState = ref<IssueState>(0)
const searchInput = ref('')
const appliedSearch = ref('')
const selectedKeys = ref<string[]>([])
const activeKey = ref('')
const loading = ref(false)
const busy = ref(false)
const error = ref('')
const notice = ref('')
const mode = ref<EditMode>(null)
const source = ref<InspectionTypeRow | null>(null)
const editor = ref<EditorValue>(blankEditor())
const editorError = ref('')
const driverRows = ref<DriverRow[]>([])
const driverPicker = ref(false)
const driverSearch = ref('')

function blankEditor(): EditorValue {
  return { inspMachineType: '', inspMachineTypeName: '', inspType: 0, baudRate: '', parityBit: '', dataBit: '', stopBit: '', endCode: '', format1: '', format2: '', chNo: '', inspDriverNo: '', description: '' }
}
function normalizeRow(value: Record<string, unknown>): InspectionTypeRow {
  const get = (name: string): unknown => value[name] ?? value[name[0].toUpperCase() + name.slice(1)]
  return {
    inspMachineType: String(get('inspMachineType') ?? ''), inspMachineTypeName: (get('inspMachineTypeName') as string | null) ?? null,
    inspType: Number(get('inspType') ?? 0), baudRate: get('baudRate') == null ? null : Number(get('baudRate')),
    parityBit: (get('parityBit') as string | null) ?? null, dataBit: get('dataBit') == null ? null : Number(get('dataBit')),
    stopBit: get('stopBit') == null ? null : Number(get('stopBit')), endCode: (get('endCode') as string | null) ?? null,
    format1: (get('format1') as string | null) ?? null, format2: (get('format2') as string | null) ?? null,
    chNo: (get('chNo') as string | null) ?? null, inspDriverNo: (get('inspDriverNo') as string | null) ?? null,
    description: (get('description') as string | null) ?? null, creator: (get('creator') as string | null) ?? null,
    createDate: (get('createDate') as string | null) ?? null, issueState: Number(get('issueState') ?? 0) as IssueState,
    dataStamp: get('dataStamp') == null ? null : Number(get('dataStamp')), reviser: (get('reviser') as string | null) ?? null,
    reviseDate: (get('reviseDate') as string | null) ?? null,
  }
}
function payloadRows<T>(payload: T[] | Envelope<T>): T[] { return Array.isArray(payload) ? payload : payload.items ?? payload.rows ?? payload.data ?? [] }
function apiUrl(path: string): string {
  if (!props.apiBaseUrl.trim()) throw new Error('目前尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(props.apiBaseUrl.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init })
  if (response.status === 401) { window.location.assign(import.meta.env.BASE_URL + 'login'); throw new Error('登入已逾期，請重新登入。') }
  const body = await response.text()
  if (!response.ok) throw new Error(body || 'API 請求失敗 (' + response.status + ')')
  return (body ? JSON.parse(body) : undefined) as T
}
function searchMatches(value: string, query: string): boolean { return value.toLocaleLowerCase().includes(query.toLocaleLowerCase()) }
const visibleRows = computed(() => rows.value.filter(row => row.issueState === issueState.value).filter(row => searchMatches(row.inspMachineType, appliedSearch.value)).sort((a, b) => a.inspMachineType.localeCompare(b.inspMachineType)))
const selectedRows = computed(() => visibleRows.value.filter(row => selectedKeys.value.includes(row.inspMachineType)))
const activeRow = computed(() => visibleRows.value.find(row => row.inspMachineType === activeKey.value) ?? null)
const filteredDrivers = computed(() => driverRows.value.filter(row => searchMatches(row.inspDriverNo + ' ' + (row.functionName ?? '') + ' ' + (row.description ?? ''), driverSearch.value)))
const modeTitle = computed(() => mode.value === 'add' ? '新增' : mode.value === 'copy' ? '複製' : '修改')
const currentUser = computed(() => props.currentUserNo?.trim() || (mock.value ? 'DEV001' : ''))
const editorLocked = computed(() => mode.value === 'edit' && source.value?.issueState === 1)

function persistDemo(): void { localStorage.setItem(storageKey, JSON.stringify(rows.value)) }
function loadDemo(): InspectionTypeRow[] {
  try { const stored = localStorage.getItem(storageKey); if (stored) return (JSON.parse(stored) as Record<string, unknown>[]).map(normalizeRow) } catch { /* reset invalid local preview data */ }
  return demoRows.map(row => ({ ...row }))
}
async function loadRows(): Promise<void> {
  loading.value = true; error.value = ''; notice.value = ''
  try {
    if (mock.value) rows.value = loadDemo()
    else {
      const query = new URLSearchParams({ issueState: String(issueState.value) })
      if (searchInput.value.trim()) query.set('inspMachineType', searchInput.value.trim())
      rows.value = payloadRows(await request<Record<string, unknown>[] | Envelope<Record<string, unknown>>>('/api/qc/inspection-machine-parameter-types?' + query)).map(normalizeRow)
    }
    appliedSearch.value = searchInput.value.trim()
    selectedKeys.value = []; activeKey.value = ''
    if (!visibleRows.value.length) notice.value = '沒有符合查詢條件的檢測儀器類別。'
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '載入檢測儀器類別失敗。' }
  finally { loading.value = false }
}
async function loadDrivers(): Promise<void> {
  if (mock.value) { driverRows.value = demoDrivers.map(row => ({ ...row })); return }
  const response = await request<DriverRow[] | Envelope<DriverRow>>('/api/qc/inspection-drivers')
  driverRows.value = payloadRows(response)
}
function setIssueState(value: IssueState): void { issueState.value = value; void loadRows() }
function toggleSelection(row: InspectionTypeRow, checked: boolean): void {
  activeKey.value = row.inspMachineType
  selectedKeys.value = checked ? [...new Set([...selectedKeys.value, row.inspMachineType])] : selectedKeys.value.filter(key => key !== row.inspMachineType)
}
function selectRow(row: InspectionTypeRow): void { activeKey.value = row.inspMachineType }
function openEditor(nextMode: Exclude<EditMode, null>): void {
  if (nextMode !== 'add' && !activeRow.value) { notice.value = '請先選取一筆資料。'; return }
  source.value = nextMode === 'add' ? null : activeRow.value
  const row = source.value
  editor.value = row ? {
    inspMachineType: nextMode === 'copy' ? '' : row.inspMachineType, inspMachineTypeName: row.inspMachineTypeName ?? '', inspType: row.inspType,
    baudRate: row.baudRate == null ? '' : String(row.baudRate), parityBit: row.parityBit ?? '', dataBit: row.dataBit == null ? '' : String(row.dataBit),
    stopBit: row.stopBit == null ? '' : String(row.stopBit), endCode: row.endCode ?? '', format1: row.format1 ?? '',
    format2: row.format2 === 'NA' ? '' : row.format2 ?? '', chNo: row.chNo === 'NA' ? '' : row.chNo ?? '', inspDriverNo: row.inspDriverNo ?? '', description: row.description ?? '',
  } : blankEditor()
  editorError.value = ''; notice.value = ''; mode.value = nextMode
  if (nextMode !== 'add' && row?.inspType === 1) void loadDrivers().catch(reason => { editorError.value = reason instanceof Error ? reason.message : '載入 Driver 清單失敗。' })
}
function validateEditor(): string {
  if (!editor.value.inspMachineType.trim()) return '請輸入 InspMachineType。'
  if (editor.value.inspMachineType.trim().length > 50) return 'InspMachineType 最長 50 個字元。'
  if (editor.value.inspMachineTypeName.length > 50) return 'InspMachineTypeName 最長 50 個字元。'
  if (editor.value.inspType === 1 && !editor.value.inspDriverNo.trim()) return 'UserDefine 類別必須選擇 InspDriverNo。'
  if (editor.value.inspType === 0) {
    const required: Array<[keyof EditorValue, string]> = [['baudRate', 'BaudRate'], ['parityBit', 'ParityBit'], ['dataBit', 'DataBit'], ['stopBit', 'StopBit'], ['endCode', 'EndCode'], ['format1', 'Format1']]
    const missing = required.find(([key]) => !String(editor.value[key]).trim())
    if (missing) return `RS232 類別必須輸入 ${missing[1]}。`
  }
  if (editor.value.description.length > 255) return 'Description 最長 255 個字元。'
  return ''
}
function editorPayload(): Record<string, unknown> {
  const serial = editor.value.inspType === 0
  return {
    inspMachineType: editor.value.inspMachineType.trim().toUpperCase(), inspMachineTypeName: editor.value.inspMachineTypeName.trim(), inspType: editor.value.inspType,
    baudRate: serial ? Number(editor.value.baudRate) : null, parityBit: serial ? editor.value.parityBit : null,
    dataBit: serial ? Number(editor.value.dataBit) : null, stopBit: serial ? Number(editor.value.stopBit) : null,
    endCode: serial ? editor.value.endCode : null, format1: serial ? editor.value.format1.trim().toUpperCase() : null,
    format2: serial ? (editor.value.format2.trim().toUpperCase() || 'NA') : null, chNo: serial ? (editor.value.chNo.trim() || 'NA') : null,
    inspDriverNo: serial ? null : editor.value.inspDriverNo.trim(), description: editor.value.description.trim() || null,
  }
}
async function saveEditor(keepOpen = false): Promise<void> {
  const validation = validateEditor(); if (validation) { editorError.value = validation; return }
  if (mode.value === 'edit' && source.value?.issueState === 1) { editorError.value = '簽核中資料依 WinForms Issue Control 不允許修改。'; return }
  busy.value = true; editorError.value = ''; error.value = ''; notice.value = ''
  const payload = editorPayload(); const now = new Date().toISOString(); const typeNo = String(payload.inspMachineType)
  try {
    let savedRow: InspectionTypeRow
    if (mock.value) {
      const existing = rows.value.find(row => row.inspMachineType.toUpperCase() === typeNo.toUpperCase())
      if (mode.value !== 'edit' && existing) throw new Error('InspMachineType 已存在。')
      const next = normalizeRow({ ...payload, creator: mode.value === 'edit' ? source.value?.creator : (currentUser.value || 'DEV001'), createDate: mode.value === 'edit' ? source.value?.createDate : now,
        issueState: mode.value === 'edit' ? source.value?.issueState : 0, dataStamp: mode.value === 'edit' ? (source.value?.dataStamp ?? 0) + 1 : 0,
        reviser: mode.value === 'edit' ? (currentUser.value || 'DEV001') : null, reviseDate: mode.value === 'edit' ? now : null })
      savedRow = next
      rows.value = mode.value === 'edit' ? rows.value.map(row => row.inspMachineType === source.value?.inspMachineType ? next : row) : [...rows.value, next]
      persistDemo()
    } else if (mode.value === 'edit' && source.value) {
      await request('/api/qc/inspection-machine-parameter-types/' + encodeURIComponent(source.value.inspMachineType), { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ ...payload, issueState: source.value.issueState, dataStamp: source.value.dataStamp }) })
      savedRow = normalizeRow({ ...source.value, ...payload, reviser: currentUser.value, reviseDate: now, dataStamp: (source.value.dataStamp ?? 0) + 1 })
    } else {
      await request('/api/qc/inspection-machine-parameter-types', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
      savedRow = normalizeRow({ ...payload, creator: currentUser.value, createDate: now, issueState: 0, dataStamp: 0, reviser: null, reviseDate: null })
    }
    if (keepOpen) { if (mode.value === 'edit') source.value = savedRow }
    else mode.value = null
    await loadRows(); notice.value = keepOpen ? '資料已套用。' : '資料已儲存。'
  } catch (reason) { editorError.value = reason instanceof Error ? reason.message : '儲存檢測儀器類別失敗。' }
  finally { busy.value = false }
}
async function deleteSelected(): Promise<void> {
  const actionRows = selectedRows.value.length ? selectedRows.value : activeRow.value ? [activeRow.value] : []
  const eligible = actionRows.filter(row => row.issueState === 0 || row.issueState === 2 || row.issueState === -1)
  if (!eligible.length) { notice.value = '請選取未簽核、已簽核或不使用狀態的資料。'; return }
  if (!window.confirm(`確定刪除選取的 ${eligible.length} 筆檢測儀器類別？`)) return
  busy.value = true; error.value = ''; notice.value = ''
  try {
    for (const row of eligible) {
      if (mock.value) rows.value = rows.value.filter(item => item.inspMachineType !== row.inspMachineType)
      else await request('/api/qc/inspection-machine-parameter-types/' + encodeURIComponent(row.inspMachineType) + '?issueState=' + row.issueState + '&dataStamp=' + (row.dataStamp ?? ''), { method: 'DELETE' })
    }
    if (mock.value) persistDemo()
    notice.value = `已刪除 ${eligible.length} 筆資料。`; await loadRows()
  } catch (reason) { const message = reason instanceof Error ? reason.message : '刪除檢測儀器類別失敗。'; await loadRows(); error.value = message }
  finally { busy.value = false }
}
async function approveSelected(): Promise<void> {
  const actionRows = selectedRows.value.length ? selectedRows.value : activeRow.value ? [activeRow.value] : []
  const eligible = actionRows.filter(row => row.issueState === 0)
  if (!eligible.length) { notice.value = '核准作業只允許選取未簽核資料。'; return }
  if (!window.confirm(`確定送出 ${eligible.length} 筆檢測儀器類別核准？`)) return
  busy.value = true; error.value = ''; notice.value = ''
  try {
    for (const row of eligible) {
      if (mock.value) rows.value = rows.value.map(item => item.inspMachineType === row.inspMachineType ? { ...item, issueState: 1, dataStamp: (item.dataStamp ?? 0) + 1, reviser: currentUser.value || 'DEV001', reviseDate: new Date().toISOString() } : item)
      else await request('/api/qc/inspection-machine-parameter-types/' + encodeURIComponent(row.inspMachineType) + '/approve', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ issueState: row.issueState, dataStamp: row.dataStamp, creator: currentUser.value }) })
    }
    if (mock.value) persistDemo()
    notice.value = `已送出 ${eligible.length} 筆核准。`; await loadRows()
  } catch (reason) { const message = reason instanceof Error ? reason.message : '核准作業失敗。'; await loadRows(); error.value = message }
  finally { busy.value = false }
}
function openDriverPicker(): void { driverSearch.value = ''; void loadDrivers().then(() => { driverPicker.value = true }).catch(reason => { editorError.value = reason instanceof Error ? reason.message : '載入 Driver 清單失敗。' }) }
function formatDate(value: string | null): string { return value ? new Date(value).toLocaleString('zh-TW', { hour12: false }) : '' }
function typeLabel(value: number): string { return value === 0 ? 'RS232' : 'UserDefine' }
watch(issueState, () => { selectedKeys.value = []; activeKey.value = '' })
onMounted(() => { void loadRows() })
</script>

<template>
  <main class="insp-type-page">
    <header class="page-heading"><h1>檢測儀器類別</h1><span v-if="mock" class="dev-badge">DEV 假資料</span></header>
    <p v-if="error" class="message error" role="alert">{{ error }}</p><p v-else-if="notice" class="message notice" role="status">{{ notice }}</p>
    <section class="query-panel" aria-label="檢索條件">
      <fieldset class="states"><legend>狀態</legend><label v-for="status in statuses" :key="status.value" class="state-option"><input type="radio" name="insp-issue-state" :checked="issueState === status.value" @change="setIssueState(status.value)">{{ status.label }}</label></fieldset>
      <label class="search-label">檢測儀器類別<input v-model="searchInput" maxlength="50" @keydown.enter="loadRows"></label><span class="wildcard">%</span><button class="button" :disabled="loading || busy" @click="loadRows">查詢</button>
    </section>
    <section class="list-panel">
      <div class="grid-wrap"><table class="data-grid"><thead><tr><th class="select-col"><input type="checkbox" aria-label="全選" :checked="visibleRows.length > 0 && selectedRows.length === visibleRows.length" @change="selectedKeys = ($event.target as HTMLInputElement).checked ? visibleRows.map(row => row.inspMachineType) : []"></th><th>InspMachineType</th><th>InspMachineTypeName</th><th>InspType</th><th>BaudRate</th><th>DataBit</th><th>StopBit</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
        <tbody><tr v-for="row in visibleRows" :key="row.inspMachineType" :class="{ active: row.inspMachineType === activeKey }" @click="selectRow(row)" @dblclick="openEditor('edit')">
          <td class="select-col"><input type="checkbox" :checked="selectedKeys.includes(row.inspMachineType)" :aria-label="'選取 ' + row.inspMachineType" @click.stop @change="toggleSelection(row, ($event.target as HTMLInputElement).checked)"></td><td>{{ row.inspMachineType }}</td><td>{{ row.inspMachineTypeName }}</td><td>{{ typeLabel(row.inspType) }}</td><td class="number-cell">{{ row.baudRate ?? '' }}</td><td class="number-cell">{{ row.dataBit ?? '' }}</td><td class="number-cell">{{ row.stopBit ?? '' }}</td><td class="description-cell">{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td>
        </tr><tr v-if="!visibleRows.length"><td colspan="12" class="empty-cell">{{ loading ? '載入中…' : '沒有資料' }}</td></tr></tbody></table></div>
      <aside class="action-column"><button class="button" :disabled="busy" @click="openEditor('add')">新增(A)</button><button class="button" :disabled="busy || !activeRow" @click="openEditor('edit')">修改(E)</button><button class="button" :disabled="busy || !activeRow" @click="openEditor('copy')">複製(C)</button><button class="button" :disabled="busy || (!selectedRows.length && !activeRow)" @click="approveSelected">核准</button><button class="button" :disabled="busy || (!selectedRows.length && !activeRow)" @click="deleteSelected">刪除(D)</button><button class="button close-button" @click="emit('close')">關閉</button></aside>
    </section>
    <footer class="list-footer">資料總筆數 {{ visibleRows.length }}　已選取 {{ selectedRows.length }}</footer>

    <div v-if="mode" class="backdrop" @click.self="mode = null"><section class="dialog" role="dialog" aria-modal="true" :aria-label="modeTitle + '檢測儀器類別'">
      <header class="titlebar"><h2>檢測儀器類別基本資料（{{ modeTitle }}）</h2><button class="x" aria-label="關閉" @click="mode = null">×</button></header>
      <form class="editor-form" @submit.prevent="saveEditor()">
        <fieldset class="basis-fields" :disabled="editorLocked"><legend>檢測儀器類別資料</legend>
          <label class="field"><span>InspMachineType</span><input v-model="editor.inspMachineType" maxlength="50" :readonly="mode === 'edit'" required></label>
          <label class="field"><span>InspMachineTypeName</span><input v-model="editor.inspMachineTypeName" maxlength="50"></label>
          <label class="field"><span>InspType</span><select v-model.number="editor.inspType" :disabled="mode === 'edit'"><option :value="0">0 — RS232</option><option :value="1">1 — UserDefine</option></select></label>
          <div v-if="editor.inspType === 0" class="serial-fields"><div class="section-tabs"><b>RS232</b><span>串列通訊參數</span></div><div class="serial-grid">
            <label>BaudRate<select v-model="editor.baudRate" required><option value="">請選擇</option><option v-for="item in baudRates" :key="item" :value="item">{{ item }}</option></select></label>
            <label>EndCode<select v-model="editor.endCode" required><option value="">請選擇</option><option v-for="item in endCodes" :key="item" :value="item">{{ item }}</option></select></label>
            <label>ParityBit<select v-model="editor.parityBit" required><option value="">請選擇</option><option v-for="item in parityOptions" :key="item" :value="item">{{ item }}</option></select></label>
            <label>Format1<input v-model="editor.format1" maxlength="50" required></label>
            <label>DataBit<select v-model="editor.dataBit" required><option value="">請選擇</option><option v-for="item in bitOptions" :key="item" :value="item">{{ item }}</option></select></label>
            <label>Format2<input v-model="editor.format2" maxlength="2"></label>
            <label>StopBit<select v-model="editor.stopBit" required><option value="">請選擇</option><option v-for="item in stopOptions" :key="item" :value="item">{{ item }}</option></select></label>
            <label>ChNo<input v-model="editor.chNo" maxlength="10"></label>
          </div><div class="end-code-hint">EndCode 對照：0 NUL、3 ETX、10 LF、13 CR</div></div>
          <div v-else class="driver-fields"><div class="section-tabs"><b>UserDefine</b><span>自訂檢測 Driver</span></div><label class="driver-label">InspDriverNo<input v-model="editor.inspDriverNo" readonly required><button type="button" class="button" @click="openDriverPicker">...</button></label></div>
          <label class="field description-field"><span>Description</span><textarea v-model="editor.description" maxlength="255" rows="3"></textarea></label>
        </fieldset>
        <fieldset class="audit-fields"><legend>建立／修改資訊</legend><label><span>建立人</span><input :value="mode === 'edit' ? source?.creator ?? '' : currentUser" readonly></label><label><span>建立日期</span><input :value="mode === 'edit' ? formatDate(source?.createDate ?? null) : '儲存時建立'" readonly></label><label><span>修改人</span><input :value="mode === 'edit' ? currentUser : ''" readonly></label><label><span>修改日期</span><input :value="mode === 'edit' ? '儲存時更新' : ''" readonly></label></fieldset>
        <p v-if="editorError" class="message error">{{ editorError }}</p><footer class="dialog-actions"><button class="button primary" type="submit" :disabled="busy || editorLocked">確定</button><button class="button" type="button" @click="mode = null">關閉</button><button class="button" type="button" :disabled="busy || editorLocked" @click="saveEditor(true)">套用</button></footer>
      </form>
    </section></div>
    <div v-if="driverPicker" class="backdrop" @click.self="driverPicker = false"><section class="dialog driver-dialog" role="dialog" aria-modal="true" aria-label="選取 InspDriverNo"><header class="titlebar"><h2>選取檢測 Driver</h2><button class="x" aria-label="關閉" @click="driverPicker = false">×</button></header><label class="driver-search">搜尋 Driver<input v-model="driverSearch" autofocus></label><div class="driver-grid"><table class="data-grid"><thead><tr><th>InspDriverNo</th><th>FunctionName</th><th>Description</th><th></th></tr></thead><tbody><tr v-for="row in filteredDrivers" :key="row.inspDriverNo"><td>{{ row.inspDriverNo }}</td><td>{{ row.functionName }}</td><td>{{ row.description }}</td><td><button class="button" @click="editor.inspDriverNo = row.inspDriverNo; driverPicker = false">選取</button></td></tr><tr v-if="!filteredDrivers.length"><td colspan="4" class="empty-cell">沒有可選 Driver</td></tr></tbody></table></div></section></div>
  </main>
</template>

<style scoped>
.insp-type-page{--line:#c8c4bb;--text:#30302e;--muted:#77746e;--field:#fff0ad;--orange:#f7a32b;display:flex;flex-direction:column;gap:7px;height:100%;min-height:0;padding:10px;color:var(--text);background:#f7f5ef;font:13px/1.35 "Segoe UI","Microsoft JhengHei",sans-serif}.page-heading{display:flex;align-items:center;justify-content:space-between;min-height:38px}.page-heading h1{margin:0;font-size:20px;font-weight:600}.dev-badge{padding:4px 9px;border:1px solid #d7a044;background:#fff1bd;color:#77551a;font-size:12px}.query-panel{display:flex;flex-wrap:wrap;align-items:center;gap:9px;min-height:48px;padding:5px 9px;border:1px solid var(--line);background:#fff}.states{display:flex;align-items:center;gap:10px;margin:0;padding:0;border:0}.states legend{position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0)}.state-option{display:inline-flex;align-items:center;gap:4px;white-space:nowrap;cursor:pointer}.state-option input{accent-color:#e18b17}.search-label{display:flex;align-items:center;gap:7px}.search-label input,.driver-search input{width:210px}.wildcard{margin-left:-7px}input,textarea,select{box-sizing:border-box;min-height:29px;padding:4px 7px;border:1px solid #a9a69f;border-radius:2px;background:var(--field);color:var(--text);font:inherit}input:focus,textarea:focus,select:focus{border-color:#d18a21;outline:2px solid rgb(209 138 33 / 20%)}input[readonly],input:disabled,textarea[readonly],select:disabled{background:#ecebe7;color:#777}textarea{resize:vertical}.button{min-height:30px;padding:4px 10px;border:1px solid #bdb9b0;border-radius:3px;background:linear-gradient(#fff,#f0efec);color:#343331;font:inherit;white-space:nowrap;cursor:pointer}.button:hover:not(:disabled){border-color:#d08a23;background:#fff6df}.button:disabled{opacity:.48;cursor:not-allowed}.button.primary{border-color:#df8612;background:var(--orange);color:#fff}.list-panel{display:grid;grid-template-columns:minmax(0,1fr) 88px;gap:8px;flex:1;min-height:0;padding:7px;border:1px solid var(--line);background:#fff}.grid-wrap{min-width:0;min-height:0;overflow:auto;border:1px solid #bcb8af}.data-grid{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}.data-grid th{position:sticky;top:0;z-index:1;padding:6px 8px;border:1px solid #c2bfb9;background:#eeeae0;text-align:left;font-weight:600}.data-grid td{height:29px;padding:3px 8px;border:1px solid #d1cec8;background:#fff}.data-grid tbody tr{cursor:pointer}.data-grid tbody tr:hover td{background:#fff7dd}.data-grid tbody tr.active td{background:#ffedb1}.select-col{width:30px;text-align:center}.data-grid input[type=checkbox]{min-height:auto;accent-color:#dc8e1c}.number-cell{text-align:right}.description-cell{min-width:180px;max-width:280px;overflow:hidden;text-overflow:ellipsis}.empty-cell{text-align:center!important;color:var(--muted)!important;height:46px!important}.action-column{display:flex;flex-direction:column;gap:7px}.action-column .button{width:100%}.close-button{margin-top:auto}.list-footer{min-height:19px;padding:0 2px;color:var(--muted);font-size:12px}.message{margin:0;padding:7px 10px;border:1px solid}.error{border-color:#d99b86;background:#fff0e9;color:#873820}.notice{border-color:#c9bc91;background:#fff8dc;color:#67551d}.backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:20px;background:rgb(25 24 22 / 30%)}.dialog{display:flex;flex-direction:column;width:min(710px,100%);max-height:calc(100vh - 36px);overflow:auto;border:1px solid #a8a49c;background:#f8f7f3;box-shadow:0 8px 28px rgb(0 0 0 / 22%)}.titlebar{display:flex;align-items:center;justify-content:space-between;padding:9px 13px;border-bottom:1px solid var(--line);background:#f0eee8}.titlebar h2{margin:0;font-size:17px;font-weight:600}.x{border:0;background:transparent;font-size:24px;line-height:1;cursor:pointer}.editor-form{display:flex;flex-direction:column;gap:8px;padding:10px}.basis-fields{display:grid;grid-template-columns:150px minmax(0,1fr);gap:8px 10px;margin:0;padding:10px;border:1px solid var(--line)}.basis-fields legend,.audit-fields legend{padding:0 5px;color:#57544e}.field{display:contents}.field>span{text-align:right;padding-top:6px}.field input,.field select,.field textarea{width:100%}.serial-fields,.driver-fields,.description-field{grid-column:1/-1}.serial-fields,.driver-fields{border:1px solid #cecbc4;background:#fff}.section-tabs{display:flex;align-items:center;gap:10px;padding:5px 8px;border-bottom:1px solid #d1cec8;background:#efede7}.section-tabs b{padding:2px 12px;border-bottom:2px solid var(--orange)}.section-tabs span{color:var(--muted)}.serial-grid{display:grid;grid-template-columns:1fr 1fr;gap:8px 14px;padding:9px}.serial-grid label{display:grid;grid-template-columns:92px minmax(0,1fr);align-items:center;gap:6px}.end-code-hint{padding:0 9px 7px;color:var(--muted);font-size:11px}.driver-label{display:grid;grid-template-columns:115px minmax(0,1fr) 42px;align-items:center;gap:7px;padding:10px}.description-field textarea{min-height:55px}.audit-fields{display:grid;grid-template-columns:1fr 1fr;gap:8px 12px;margin:0;padding:9px;border:1px solid var(--line)}.audit-fields label{display:grid;grid-template-columns:70px minmax(0,1fr);align-items:center;gap:6px}.audit-fields input{width:100%;background:#ecebe7}.dialog-actions{display:flex;justify-content:flex-end;gap:8px;padding-top:4px}.dialog-actions .primary{min-width:94px}.driver-dialog{width:min(760px,100%)}.driver-search{display:flex;align-items:center;gap:8px;padding:10px}.driver-grid{max-height:380px;overflow:auto;margin:0 10px 10px;border:1px solid var(--line)}
@media(max-width:850px){.query-panel{align-items:flex-start}.states{width:100%;flex-wrap:wrap}.list-panel{grid-template-columns:1fr}.action-column{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));grid-row:2}.close-button{margin-top:0}.basis-fields{grid-template-columns:1fr}.field{display:grid;grid-template-columns:145px minmax(0,1fr);grid-column:1/-1;gap:6px}.field>span{text-align:left}.serial-grid{grid-template-columns:1fr}.audit-fields{grid-template-columns:1fr}}
</style>
