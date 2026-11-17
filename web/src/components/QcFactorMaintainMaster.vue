<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

interface FactorParameter {
  parameterNo: string
  parameterName: string
  tableName: string
}
interface FactorColumn {
  name: string
  dataType: string
  nullable: boolean
  dataLength: number | null
}
type FactorRow = Record<string, string>
interface FactorDataset {
  tableName: string
  columns: FactorColumn[]
  rows: FactorRow[]
}
interface FactorMockStore {
  parameters: FactorParameter[]
  datasets: Record<string, FactorDataset>
}
type ListPayload<T> = T[] | { items?: T[]; parameters?: T[] }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes-dev-mock:qc-factor-maintain:v1'
const demoParameters: FactorParameter[] = [
  { parameterNo: 'DEV-FC-POSIONLIST', parameterName: '要因位置清單（DEMO）', tableName: 'FC_POSIONLIST' },
  { parameterNo: 'DEV-FC-CALLBACKMAP', parameterName: '品管表單回填對應（DEMO）', tableName: 'FC_QCFORMCALLBACKITEMMAP' },
  { parameterNo: 'DEV-FC-THICKNESS', parameterName: '厚度規格（DEMO）', tableName: 'FC_THICKNESS' },
]
const demoDatasets: Record<string, FactorDataset> = {
  'DEV-FC-POSIONLIST': {
    tableName: 'FC_POSIONLIST',
    columns: [{ name: 'POSION_NO', dataType: 'VARCHAR2', nullable: false, dataLength: 50 }],
    rows: [{ POSION_NO: 'DEV-POS-01' }, { POSION_NO: 'DEV-POS-02' }],
  },
  'DEV-FC-CALLBACKMAP': {
    tableName: 'FC_QCFORMCALLBACKITEMMAP',
    columns: [
      { name: 'FIELDNAME', dataType: 'VARCHAR2', nullable: false, dataLength: 30 },
      { name: 'QCITEMNO', dataType: 'VARCHAR2', nullable: false, dataLength: 50 },
      { name: 'DESCRIPTION', dataType: 'VARCHAR2', nullable: true, dataLength: 500 },
    ],
    rows: [
      { FIELDNAME: 'DEV_FIELD_01', QCITEMNO: 'DEV_QC_001', DESCRIPTION: '開發用回填欄位' },
      { FIELDNAME: 'DEV_FIELD_02', QCITEMNO: 'DEV_QC_002', DESCRIPTION: '' },
    ],
  },
  'DEV-FC-THICKNESS': {
    tableName: 'FC_THICKNESS',
    columns: [{ name: 'SPEC', dataType: 'VARCHAR2', nullable: false, dataLength: 50 }],
    rows: [{ SPEC: 'DEV-1.0mm' }, { SPEC: 'DEV-1.5mm' }],
  },
}

const parameters = ref<FactorParameter[]>([])
const selectedParameterNo = ref('')
const loadedParameterNo = ref('')
const columns = ref<FactorColumn[]>([])
const rows = ref<FactorRow[]>([])
const activeRowIndex = ref(-1)
const activeColumnIndex = ref(0)
const loadingParameters = ref(false)
const loadingData = ref(false)
const saving = ref(false)
const dirty = ref(false)
const errorMessage = ref('')
const notice = ref('')
const statusText = ref('Stop')
const importInput = ref<HTMLInputElement | null>(null)
const currentUser = computed(() => props.currentUserNo?.trim() || (mock.value ? 'DEV001' : ''))
const selectedParameter = computed(() => parameters.value.find(item => item.parameterNo === selectedParameterNo.value) ?? null)
const loadedParameter = computed(() => parameters.value.find(item => item.parameterNo === loadedParameterNo.value) ?? null)
const gridTitle = computed(() => loadedParameter.value?.tableName ?? '')

function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function listOf<T>(payload: ListPayload<T>): T[] { return Array.isArray(payload) ? payload : payload.items ?? payload.parameters ?? [] }
function normalizeParameter(value: FactorParameter | Record<string, unknown>): FactorParameter {
  const source = value as Record<string, unknown>
  return {
    parameterNo: String(source.parameterNo ?? source.PARAMETERNO ?? ''),
    parameterName: String(source.parameterName ?? source.parametername ?? source.PARAMETERNAME ?? ''),
    tableName: String(source.tableName ?? source.tablename ?? source.TABLENAME ?? ''),
  }
}
function normalizeColumn(value: FactorColumn | Record<string, unknown>): FactorColumn {
  const source = value as Record<string, unknown>
  const nullableValue = source.nullable ?? source.isNullable ?? source.IS_NULLABLE
  return {
    name: String(source.name ?? source.columnName ?? source.COLUMN_NAME ?? ''),
    dataType: String(source.dataType ?? source.typeName ?? source.DATA_TYPE ?? ''),
    nullable: nullableValue === true || String(nullableValue).toUpperCase() === 'YES' || String(nullableValue) === '1',
    dataLength: Number(source.dataLength ?? source.characterMaximumLength ?? source.DATA_LENGTH) || null,
  }
}
function normalizeRows(value: unknown, columnList: FactorColumn[]): FactorRow[] {
  if (!Array.isArray(value)) return []
  return value.map((item: unknown) => {
    const source = item as Record<string, unknown>
    const row: FactorRow = {}
    for (const column of columnList) {
      const cell = source[column.name] ?? source[column.name.toLowerCase()] ?? source[column.name.toUpperCase()]
      row[column.name] = cell === null || cell === undefined ? '' : String(cell)
    }
    return row
  })
}
function readMockStore(): FactorMockStore {
  try {
    const raw = window.localStorage.getItem(storageKey)
    if (raw) {
      const value = JSON.parse(raw) as FactorMockStore
      if (Array.isArray(value.parameters) && value.datasets) return value
    }
  } catch { /* use fresh DEV-only sample data */ }
  const initial = { parameters: clone(demoParameters), datasets: clone(demoDatasets) }
  window.localStorage.setItem(storageKey, JSON.stringify(initial))
  return initial
}
function writeMockStore(value: FactorMockStore): void { window.localStorage.setItem(storageKey, JSON.stringify(value)) }
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body && !(init.body instanceof FormData) && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init, headers })
  if (response.status === 401) { window.location.assign(appBase + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; title?: string; message?: string } | null
    throw new Error(problem?.detail || problem?.message || problem?.title || 'API 呼叫失敗（' + response.status + '）。')
  }
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}
async function loadParameters(): Promise<void> {
  loadingParameters.value = true
  errorMessage.value = ''
  try {
    const result = mock.value ? clone(readMockStore().parameters) : listOf(await request<ListPayload<Record<string, unknown>>>('api/qc/factor-maintain/parameters'))
    parameters.value = result.map(normalizeParameter)
    if (!parameters.value.some(item => item.parameterNo === selectedParameterNo.value)) selectedParameterNo.value = parameters.value[0]?.parameterNo ?? ''
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '載入 MES Parameter 清單失敗。' }
  finally { loadingParameters.value = false }
}
function canLeaveLoadedData(): boolean {
  if (!dirty.value) return true
  if (!window.confirm('目前資料尚未儲存，放棄變更並切換資料表？')) return false
  dirty.value = false
  return true
}
async function selectParameter(parameter: FactorParameter): Promise<void> {
  if (parameter.parameterNo === selectedParameterNo.value) return
  if (!canLeaveLoadedData()) return
  selectedParameterNo.value = parameter.parameterNo
  errorMessage.value = ''
  notice.value = ''
}
async function loadDetailData(): Promise<void> {
  if (!selectedParameter.value) { errorMessage.value = '請先選取 Parameter。'; return }
  if (dirty.value && selectedParameterNo.value !== loadedParameterNo.value) { errorMessage.value = '目前資料尚未儲存，請先保存或放棄變更，再切換資料表。'; return }
  if (dirty.value && !window.confirm('重新載入會放棄目前尚未儲存的變更，是否繼續？')) return
  loadingData.value = true
  errorMessage.value = ''
  notice.value = ''
  statusText.value = 'Loading...'
  try {
    const key = selectedParameter.value.parameterNo
    const result = mock.value
      ? readMockStore().datasets[key]
      : await request<{ tableName: string; columns: Record<string, unknown>[]; rows: unknown[] }>('api/qc/factor-maintain/parameters/' + encodeURIComponent(key) + '/data')
    if (!result) throw new Error('查無此 Parameter 對應的資料表。')
    columns.value = result.columns.map(normalizeColumn).filter(column => column.name)
    rows.value = normalizeRows(result.rows, columns.value)
    loadedParameterNo.value = key
    activeRowIndex.value = rows.value.length ? 0 : -1
    activeColumnIndex.value = 0
    dirty.value = false
    statusText.value = 'Stop'
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '載入資料表內容失敗。'; statusText.value = 'Error' }
  finally { loadingData.value = false }
}
function markDirty(rowIndex = -1, columnIndex = 0): void {
  dirty.value = true
  errorMessage.value = ''
  activeRowIndex.value = rowIndex
  activeColumnIndex.value = columnIndex
  statusText.value = 'Modified'
}
function finalizeCellEdit(row: FactorRow, column: FactorColumn, rowIndex: number, columnIndex: number): void {
  row[column.name] = row[column.name]?.toUpperCase() ?? ''
  markDirty(rowIndex, columnIndex)
}
function addRow(): void {
  if (!loadedParameterNo.value || !columns.value.length) { errorMessage.value = '請先載入 DetailData。'; return }
  const next: FactorRow = Object.fromEntries(columns.value.map(column => [column.name, '']))
  rows.value.push(next)
  markDirty(rows.value.length - 1, 0)
}
function deleteRow(): void {
  if (activeRowIndex.value < 0 || activeRowIndex.value >= rows.value.length) { errorMessage.value = '請先選取要刪除的資料列。'; return }
  if (!window.confirm('確定刪除目前選取的資料列？按 Save 後才會寫入資料庫。')) return
  rows.value.splice(activeRowIndex.value, 1)
  activeRowIndex.value = Math.min(activeRowIndex.value, rows.value.length - 1)
  markDirty(activeRowIndex.value)
}
function rowsForSave(): FactorRow[] {
  return rows.value.map((row, rowIndex) => {
    const next: FactorRow = {}
    for (const column of columns.value) {
      const value = (row[column.name] ?? '').trim()
      if (!column.nullable && value === '') throw new Error('第 ' + (rowIndex + 1) + ' 列的 ' + column.name + ' 為必填。')
      next[column.name] = value
    }
    return next
  })
}
async function save(): Promise<void> {
  if (!loadedParameterNo.value) { errorMessage.value = '請先載入 DetailData。'; return }
  if (!dirty.value) { notice.value = '目前沒有未儲存的變更。'; return }
  if (!window.confirm('確定儲存更新後的資料表？此動作會以目前 Grid 內容取代設定條件內的資料。')) return
  let payload: FactorRow[]
  try { payload = rowsForSave() } catch (error) { errorMessage.value = error instanceof Error ? error.message : '欄位驗證失敗。'; return }
  saving.value = true
  errorMessage.value = ''
  notice.value = ''
  statusText.value = 'Updating...'
  try {
    if (mock.value) {
      const store = readMockStore()
      const dataset = store.datasets[loadedParameterNo.value]
      if (!dataset) throw new Error('DEV 假資料表已不存在。')
      dataset.rows = clone(payload)
      writeMockStore(store)
      rows.value = clone(payload)
    } else {
      await request('api/qc/factor-maintain/parameters/' + encodeURIComponent(loadedParameterNo.value) + '/data', { method: 'PUT', body: JSON.stringify({ rows: payload }) })
    }
    dirty.value = false
    statusText.value = 'Stop'
    notice.value = mock.value ? 'DEV 假資料已保存至瀏覽器 localStorage。' : '資料已更新。'
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '儲存失敗。'; statusText.value = 'Error' }
  finally { saving.value = false }
}
function parseDelimitedLine(line: string, delimiter: string): string[] {
  const cells: string[] = []
  let value = ''
  let quoted = false
  for (let index = 0; index < line.length; index++) {
    const char = line[index]
    if (char === '"' && quoted && line[index + 1] === '"') { value += '"'; index++ }
    else if (char === '"') quoted = !quoted
    else if (char === delimiter && !quoted) { cells.push(value); value = '' }
    else value += char
  }
  cells.push(value)
  return cells
}
function appendImportedRows(values: string[][]): void {
  if (!columns.value.length) throw new Error('請先載入 DetailData。')
  for (const valuesRow of values) {
    if (!valuesRow.some(value => value.trim())) continue
    const row: FactorRow = Object.fromEntries(columns.value.map((column, index) => [column.name, valuesRow[index] ?? '']))
    rows.value.push(row)
  }
  markDirty(rows.value.length - 1, 0)
}
async function importFile(event: Event): Promise<void> {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file) return
  if (!loadedParameterNo.value) { errorMessage.value = '請先載入 DetailData。'; return }
  errorMessage.value = ''
  try {
    const extension = file.name.split('.').pop()?.toLowerCase() ?? ''
    if (mock.value) {
      if (extension !== 'csv') throw new Error('DEV 預覽支援 CSV；WinForms .xls 檔由正式 API 端解析。')
      const lines = (await file.text()).replace(/^\uFEFF/, '').split(/\r?\n/).filter(line => line.trim())
      appendImportedRows(lines.map(line => parseDelimitedLine(line, ',')))
    } else {
      const form = new FormData()
      form.append('file', file)
      const result = await request<{ rows?: string[][]; items?: string[][] }>('api/qc/factor-maintain/parameters/' + encodeURIComponent(loadedParameterNo.value) + '/import-preview', { method: 'POST', body: form })
      appendImportedRows(result.rows ?? result.items ?? [])
    }
    notice.value = '匯入資料已載入 Grid，按 Save 後才會寫入資料庫。'
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '匯入失敗。' }
}
async function pasteFromClipboard(): Promise<void> {
  if (!loadedParameterNo.value || !columns.value.length) { errorMessage.value = '請先載入 DetailData。'; return }
  try {
    const content = await navigator.clipboard.readText()
    const delimiter = content.includes('\t') ? '\t' : ','
    const lines = content.replace(/\r/g, '').split('\n').filter(line => line.length > 0).map(line => parseDelimitedLine(line, delimiter))
    if (!lines.length) return
    const firstRow = activeRowIndex.value < 0 ? rows.value.length : activeRowIndex.value
    for (let offset = 0; offset < lines.length; offset++) {
      const index = firstRow + offset
      if (!rows.value[index]) rows.value[index] = Object.fromEntries(columns.value.map(column => [column.name, '']))
      lines[offset].forEach((value, columnOffset) => {
        const column = columns.value[activeColumnIndex.value + columnOffset]
        if (column) rows.value[index][column.name] = value
      })
    }
    markDirty(Math.min(firstRow + lines.length - 1, rows.value.length - 1), activeColumnIndex.value)
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '無法讀取剪貼簿內容。請確認瀏覽器剪貼簿權限。' }
}
function closePage(): void {
  if (dirty.value && !window.confirm('更新的資料尚未儲存，確定離開？')) return
  emit('close')
}
function selectCell(rowIndex: number, columnIndex: number): void { activeRowIndex.value = rowIndex; activeColumnIndex.value = columnIndex }

onMounted(loadParameters)
</script>

<template>
  <main class="factor-maintain-page">
    <header class="page-heading"><h1>要因資料維護</h1><span v-if="mock" class="dev-badge">DEV 假資料</span></header>
    <section class="parameter-panel" aria-label="MES Parameter 清單">
      <div class="parameter-grid-wrap"><table class="parameter-grid"><thead><tr><th>ParameterNo</th><th>ParameterName</th><th>TableName</th></tr></thead><tbody>
        <tr v-for="item in parameters" :key="item.parameterNo" :class="{ active: selectedParameterNo === item.parameterNo }" @click="selectParameter(item)"><td>{{ item.parameterNo }}</td><td>{{ item.parameterName }}</td><td>{{ item.tableName }}</td></tr>
        <tr v-if="!parameters.length"><td colspan="3" class="empty">{{ loadingParameters ? '載入中…' : '查無可維護的參數表' }}</td></tr>
      </tbody></table></div>
      <button type="button" class="win-button detail-button" :disabled="loadingData || !selectedParameter" @click="loadDetailData">DetailData</button>
    </section>
    <section class="data-panel" :aria-label="gridTitle || '要因資料'">
      <div class="data-grid-wrap"><table class="data-grid"><thead><tr><th v-for="column in columns" :key="column.name" :class="{ required: !column.nullable }">{{ column.name }}</th></tr></thead><tbody>
        <tr v-for="(row, rowIndex) in rows" :key="rowIndex" :class="{ active: activeRowIndex === rowIndex }" @click="selectCell(rowIndex, activeColumnIndex)"><td v-for="(column, columnIndex) in columns" :key="column.name" :class="{ required: !column.nullable, activeCell: activeRowIndex === rowIndex && activeColumnIndex === columnIndex }" @click.stop="selectCell(rowIndex, columnIndex)"><input v-model="row[column.name]" :maxlength="column.dataLength ?? undefined" :aria-label="column.name + ' 第 ' + (rowIndex + 1) + ' 列'" @focus="selectCell(rowIndex, columnIndex)" @input="markDirty(rowIndex, columnIndex)" @change="finalizeCellEdit(row, column, rowIndex, columnIndex)"></td></tr>
        <tr v-if="!rows.length"><td :colspan="Math.max(columns.length, 1)" class="empty">{{ loadingData ? '載入中…' : loadedParameterNo ? '查無資料，新增列可建立資料。' : '請選取 Parameter 後按 DetailData。' }}</td></tr>
        <tr v-if="columns.length" class="template-row" @click="addRow"><td :colspan="columns.length">＋ 新增資料列</td></tr>
      </tbody></table></div>
      <div class="action-column"><button type="button" class="win-button" :disabled="!rows.length || saving" @click="deleteRow">Delete</button><button type="button" class="win-button" :disabled="!loadedParameterNo || saving" @click="importInput?.click()">Import</button><input ref="importInput" class="hidden-file" type="file" accept=".csv,.xls" @change="importFile"><button type="button" class="win-button" :disabled="!loadedParameterNo || saving" @click="pasteFromClipboard">Paste</button><button type="button" class="win-button" :disabled="!loadedParameterNo || saving || !dirty" @click="save">Save</button><button type="button" class="win-button close-button" :disabled="saving" @click="closePage">Close</button></div>
    </section>
    <footer class="status-bar"><span>UserNo: {{ currentUser || '—' }}</span><span>SystemDateTime: {{ new Date().toLocaleString('zh-TW', { hour12: false }) }}</span><span class="status-text">{{ statusText }}</span></footer>
    <p v-if="errorMessage" class="message error" role="alert">{{ errorMessage }}</p><p v-if="notice" class="message notice" role="status">{{ notice }}</p>
  </main>
</template>

<style scoped>
.factor-maintain-page{--line:#c8c4bb;--text:#30302e;--muted:#77746e;--field:#fff0ad;display:flex;flex-direction:column;gap:3px;height:100%;min-height:0;padding:0;color:var(--text);background:#f7f5ef;font:14px/1.3 "Segoe UI","Microsoft JhengHei",sans-serif}.page-heading{display:flex;align-items:center;justify-content:space-between;min-height:34px;padding:0 10px}.page-heading h1{margin:0;font-size:20px;font-weight:600}.dev-badge{padding:3px 8px;border:1px solid #d7a044;background:#fff1bd;color:#77551a;font-size:12px}.parameter-panel{display:grid;grid-template-columns:minmax(0,1fr) 92px;gap:6px;height:160px;min-height:100px;padding:8px;border:1px solid var(--line);background:#fff}.parameter-grid-wrap,.data-grid-wrap{min-width:0;min-height:0;overflow:auto;border:1px solid #bcb8af}.parameter-grid,.data-grid{width:100%;border-collapse:collapse;white-space:nowrap;font-size:13px}.parameter-grid th,.data-grid th{position:sticky;top:0;z-index:1;padding:5px 7px;border:1px solid #c2bfb9;background:#eeeae0;text-align:left;font-weight:600}.parameter-grid td,.data-grid td{height:27px;padding:2px 5px;border:1px solid #d1cec8;background:#fff}.parameter-grid tbody tr{cursor:pointer}.parameter-grid tbody tr:hover td{background:#fff7dd}.parameter-grid tbody tr.active td{background:#ffedb1}.parameter-grid th:nth-child(1){width:180px}.parameter-grid th:nth-child(2){width:260px}.detail-button{align-self:start}.win-button{min-height:32px;padding:4px 10px;border:1px solid #bdb9b0;border-radius:3px;background:linear-gradient(#fff,#f0efec);color:#343331;font:inherit;white-space:nowrap;cursor:pointer}.win-button:hover:not(:disabled){border-color:#d08a23;background:#fff6df}.win-button:disabled{opacity:.48;cursor:not-allowed}.data-panel{display:grid;grid-template-columns:minmax(0,1fr) 92px;gap:6px;flex:1;min-height:160px;padding:8px;border:1px solid var(--line);background:#fff}.data-grid{width:max-content;min-width:100%}.data-grid th{background:#eeeae0}.data-grid td{padding:0}.data-grid td.required{background:var(--field)}.data-grid td.activeCell{outline:2px solid #d18a21;outline-offset:-2px}.data-grid td input{box-sizing:border-box;width:100%;min-width:100px;height:27px;padding:3px 6px;border:0;background:transparent;color:var(--text);font:inherit;outline:0}.data-grid td input:focus{background:#fff8dc}.data-grid tbody tr.active td:not(.activeCell){background:#fff7df}.data-grid .empty{padding:15px;text-align:center;color:var(--muted)}.template-row td{background:#eeeae0;color:#555;cursor:pointer}.template-row:hover td{background:#fff1bd}.action-column{display:flex;flex-direction:column;gap:7px}.action-column .win-button{width:100%}.close-button{margin-top:auto}.hidden-file{display:none}.status-bar{display:grid;grid-template-columns:auto auto 1fr;gap:18px;min-height:24px;align-items:center;padding:0 10px;border-top:1px solid #aaa69e;background:#efede7;color:#555;font-size:12px}.status-text{text-align:right}.message{margin:0 8px;padding:7px 10px;border:1px solid}.error{border-color:#d99b86;background:#fff0e9;color:#873820}.notice{border-color:#c9bc91;background:#fff8dc;color:#67551d}@media(max-width:700px){.parameter-panel,.data-panel{grid-template-columns:1fr;min-height:210px}.parameter-panel{height:220px}.parameter-panel .detail-button{grid-row:2;justify-self:end}.action-column{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));grid-row:2}.close-button{margin-top:0}.status-bar{grid-template-columns:1fr;gap:2px;padding:4px 8px}.status-text{text-align:left}}
</style>
