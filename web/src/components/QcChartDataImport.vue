<script setup lang="ts">
import { computed, ref } from 'vue'
import { apiErrorMessage } from '../utils/apiError'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'
import { createXlsxTemplate, parseFirstWorksheetXlsx, parseMaterialCsv } from './materialImportWorkbook'

type ImportRecord = Record<string, string | number | boolean | null | undefined>
interface ImportRow {
  id: number
  data: Record<string, string>
  execute: string
  factorSerial: string
  state: 'ready' | 'success' | 'failed'
}
interface ApiRowResult {
  rowNo?: number
  succeeded?: boolean
  execute?: string
  message?: string
  factorSerial?: string
  result?: number | string
}

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()
const devMock = computed(() => import.meta.env.DEV && props.developmentPreview)
const apiRoot = 'api/qc/chart-data-import'
const fileInput = ref<HTMLInputElement | null>(null)
const sourceFields = ref<string[]>([])
const rows = ref<ImportRow[]>([])
const fileName = ref('')
const busy = ref(false)
const hasExecuted = ref(false)
const formatValue = ref(false)
const rightDigits = ref(1)
const error = ref('')
const notice = ref('')
const selectedGridRows = ref<number[]>([])
const anchorIndex = ref<number | null>(null)
const rowCount = computed(() => rows.value.length)
const resultCount = computed(() => rows.value.filter(row => row.state !== 'ready').length)
const successCount = computed(() => rows.value.filter(row => row.state === 'success').length)
const templateHeaders = [
  'QCItemNo', 'SampleSize', 'Creator', 'CreateDate', 'ChartTitle', 'KeyNode', 'Sponsor', 'PlanNo', 'MEMO',
  'PRODUCT', 'MATERIAL', 'TUS', 'TCS', 'TLS', 'UCL', 'CL', 'LCL', 'Target', 'CPKGoal', 'TYAxisTo', 'TYAxisFrom',
  'XUS', 'XCS', 'XLS', 'XUCL', 'XCL', 'XLCL', 'XYAxisTo', 'XYAxisFrom', 'RUS', 'RCS', 'RLS', 'RUCL', 'RCL', 'RLCL', 'RYAxisTo', 'RYAxisFrom',
  'SUS', 'SCS', 'SLS', 'SUCL', 'SCL', 'SLCL', 'SYAxisTo', 'SYAxisFrom', 'MUS', 'MCS', 'MLS', 'MUCL', 'MCL', 'MLCL', 'MYAxisTo', 'MYAxisFrom',
  'TRule', 'XRule', 'RRule', 'SRule', 'MRule',
]
const totalColumns = computed(() => sourceFields.value.length + 2)
let nextRowId = 1

function makeRow(data: Record<string, string>): ImportRow {
  return { id: nextRowId++, data: { ...data }, execute: '', factorSerial: '', state: 'ready' }
}
function normalizeRecord(record: ImportRecord): Record<string, string> {
  const normalized: Record<string, string> = {}
  for (const [key, value] of Object.entries(record)) {
    const name = key.trim()
    if (!name || ['EXECUTE', 'FACTORSERIAL', 'SELECT', 'RESULT'].includes(name.toUpperCase())) continue
    normalized[name] = String(value ?? '').trim()
  }
  return normalized
}
function rowsFromMatrix(matrix: string[][], skipExampleRow: boolean): { fields: string[]; rows: ImportRow[] } {
  if (!matrix.length) throw new Error('檔案沒有工作表資料。')
  const fields = matrix[0].map((cell, index) => cell.trim() || `unknow${index}`)
    .filter(field => !['EXECUTE', 'FACTORSERIAL', 'SELECT', 'RESULT'].includes(field.toUpperCase()))
  if (new Set(fields.map(field => field.toUpperCase())).size !== fields.length) throw new Error('Excel 欄名重複，請確認範本欄位。')
  const startAt = skipExampleRow && matrix.length > 1 ? 2 : 1
  const imported: ImportRow[] = []
  for (const cells of matrix.slice(startAt)) {
    if (!cells.some(cell => cell.trim())) break
    const data: Record<string, string> = Object.fromEntries(fields.map(field => [field, '']))
    fields.forEach((field, index) => { data[field] = cells[index]?.trim() ?? '' })
    imported.push(makeRow(data))
  }
  return { fields, rows: imported }
}
function apiUrl(path: string): string {
  if (!props.apiBaseUrl.trim()) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(props.apiBaseUrl.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init })
  if (response.status === 401) {
    window.location.assign(`${import.meta.env.BASE_URL}login`)
    throw new Error('登入已逾期，請重新登入。')
  }
  if (!response.ok) throw new Error(await apiErrorMessage(response))
  const body = await response.text()
  return (body ? JSON.parse(body) : undefined) as T
}
function pickFile(): void { error.value = ''; notice.value = ''; fileInput.value?.click() }
function loadDevSample(): void {
  sourceFields.value = [...templateHeaders]
  const samples: Record<string, string>[] = [
    {
      QCItemNo: 'DEV-QCV-CHART-01', SampleSize: '5', Creator: 'DEV001', CreateDate: '2026-11-17 09:00:00',
      ChartTitle: '開發用趨勢管制圖', KeyNode: 'N', Sponsor: '開發假資料', PlanNo: 'DEV-PLAN-01', MEMO: 'DEV only',
      PRODUCT: 'DEV-PRODUCT-01', MATERIAL: 'DEV-MATERIAL-01', TUS: '20', TCS: '15', TLS: '10', UCL: '18', CL: '15', LCL: '12', Target: '15', CPKGoal: '1.33', TYAxisTo: '25', TYAxisFrom: '5',
      XUS: '20', XCS: '15', XLS: '10', XUCL: '18', XCL: '15', XLCL: '12', XYAxisTo: '25', XYAxisFrom: '5',
      RUS: '8', RCS: '5', RLS: '2', RUCL: '7', RCL: '5', RLCL: '3', RYAxisTo: '10', RYAxisFrom: '0',
      SUS: '8', SCS: '5', SLS: '2', SUCL: '7', SCL: '5', SLCL: '3', SYAxisTo: '10', SYAxisFrom: '0',
      MUS: '8', MCS: '5', MLS: '2', MUCL: '7', MCL: '5', MLCL: '3', MYAxisTo: '10', MYAxisFrom: '0',
      TRule: 'DEV-RULE-01,warning', XRule: 'DEV-RULE-01,hold', RRule: '', SRule: '', MRule: '',
    },
    {
      QCItemNo: 'DEV-QCV-CHART-02', SampleSize: '3', Creator: 'DEV001', CreateDate: '2026-11-17 09:00:00',
      ChartTitle: '開發用管制圖二', KeyNode: 'Y', Sponsor: '開發假資料', PlanNo: '', MEMO: '',
      PRODUCT: 'DEV-PRODUCT-02', MATERIAL: 'DEV-MATERIAL-02', TUS: '12', TCS: '10', TLS: '8', UCL: '11', CL: '10', LCL: '9', Target: '10', CPKGoal: '1.00', TYAxisTo: '15', TYAxisFrom: '5',
      XUS: '12', XCS: '10', XLS: '8', XUCL: '11', XCL: '10', XLCL: '9', XYAxisTo: '15', XYAxisFrom: '5',
      RUS: '5', RCS: '3', RLS: '1', RUCL: '4', RCL: '3', RLCL: '2', RYAxisTo: '7', RYAxisFrom: '0',
      SUS: '5', SCS: '3', SLS: '1', SUCL: '4', SCL: '3', SLCL: '2', SYAxisTo: '7', SYAxisFrom: '0',
      MUS: '5', MCS: '3', MLS: '1', MUCL: '4', MCL: '3', MLCL: '2', MYAxisTo: '7', MYAxisFrom: '0',
      TRule: '', XRule: '', RRule: '', SRule: '', MRule: '',
    },
    {
      QCItemNo: 'DEV-QCV-CHART-03', SampleSize: '5', Creator: 'DEV001', CreateDate: '2026-11-17 09:00:00',
      ChartTitle: '檢查錯誤示範', KeyNode: 'N', Sponsor: '開發假資料', PlanNo: '', MEMO: '',
      PRODUCT: 'DEV-PRODUCT-03', MATERIAL: 'DEV-MATERIAL-03', TUS: 'bad-number', TCS: '', TLS: '10', UCL: '', CL: '', LCL: '', Target: '', CPKGoal: '', TYAxisTo: '20', TYAxisFrom: '5',
      XUS: '', XCS: '', XLS: '', XUCL: '', XCL: '', XLCL: '', XYAxisTo: '', XYAxisFrom: '', RUS: '', RCS: '', RLS: '', RUCL: '', RCL: '', RLCL: '', RYAxisTo: '', RYAxisFrom: '',
      SUS: '', SCS: '', SLS: '', SUCL: '', SCL: '', SLCL: '', SYAxisTo: '', SYAxisFrom: '', MUS: '', MCS: '', MLS: '', MUCL: '', MCL: '', MLCL: '', MYAxisTo: '', MYAxisFrom: '',
      TRule: '', XRule: '', RRule: '', SRule: '', MRule: '',
    },
  ]
  rows.value = samples.map(sample => makeRow(sample))
  fileName.value = 'DEV 假資料（開發範例）'
  hasExecuted.value = false
  selectedGridRows.value = []
  anchorIndex.value = null
  error.value = ''
  notice.value = '載入 3 筆開發範例；此頁只使用瀏覽器假資料，不會寫入正式資料庫。'
}
async function handleFile(event: Event): Promise<void> {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file) return
  busy.value = true
  error.value = ''
  notice.value = ''
  fileName.value = file.name
  try {
    let imported: { fields: string[]; rows: ImportRow[] }
    if (devMock.value) {
      const extension = file.name.toLowerCase().split('.').pop()
      if (extension === 'xlsx') imported = rowsFromMatrix(await parseFirstWorksheetXlsx(file), true)
      else if (extension === 'csv') imported = rowsFromMatrix(parseMaterialCsv(await file.text()), false)
      else throw new Error('DEV 預覽支援 .xlsx 與 .csv；正式 API 可解析 WinForms 使用的 .xls。')
    } else {
      const form = new FormData()
      form.append('file', file)
      const payload = await request<{ columns?: string[]; rows?: ImportRecord[] }>(`${apiRoot}/preview`, { method: 'POST', body: form })
      const records = payload.rows ?? []
      const fields = (payload.columns?.length ? payload.columns : Object.keys(records[0] ?? {}))
        .filter(field => !['EXECUTE', 'FACTORSERIAL', 'SELECT', 'RESULT'].includes(field.toUpperCase()))
      imported = { fields, rows: records.map(record => makeRow(normalizeRecord(record))) }
    }
    sourceFields.value = imported.fields
    rows.value = imported.rows
    hasExecuted.value = false
    selectedGridRows.value = []
    anchorIndex.value = null
    notice.value = `載入 ${rows.value.length} 筆資料。請確認內容後按 Save。`
  } catch (reason) {
    sourceFields.value = []
    rows.value = []
    error.value = reason instanceof Error ? reason.message : '讀取匯入檔案失敗。'
  } finally { busy.value = false }
}
function shouldFormatColumn(field: string): boolean {
  const name = field.toUpperCase()
  return ['CL', 'TARGET', 'CPKGOAL'].includes(name)
    || name.includes('YAXIS')
    || (name.length === 3 && ['US', 'CL', 'LS'].includes(name.slice(-2)))
    || (name.length === 4 && ['LCL', 'UCL'].includes(name.slice(-3)))
}
function formatLimits(): void {
  if (!formatValue.value) return
  const digits = Math.max(1, Math.min(13, rightDigits.value))
  for (const row of rows.value) for (const field of sourceFields.value) {
    const value = row.data[field]?.trim() ?? ''
    if (!shouldFormatColumn(field) || value === '' || !Number.isFinite(Number(value))) continue
    row.data[field] = Number(value).toFixed(digits)
  }
}
function validateDemoRow(data: Record<string, string>): string {
  if (!data.QCItemNo?.trim()) return 'QCItemNo 不可空白'
  if (data.SampleSize && (!/^\d+$/.test(data.SampleSize) || Number(data.SampleSize) <= 0)) return 'SampleSize 必須是大於 0 的整數'
  for (const factor of ['PRODUCT', 'MATERIAL']) if (sourceFields.value.includes(factor) && !data[factor]?.trim()) return `${factor} 要因值不可空白`
  for (const field of sourceFields.value) {
    if (!shouldFormatColumn(field) && !/^[TXRSM](US|CS|LS|UCL|CL|LCL|YAXISTO|YAXISFROM)$/i.test(field)) continue
    const value = data[field]?.trim() ?? ''
    if (value && !Number.isFinite(Number(value))) return `${field} 必須是數值`
  }
  if (!data.TUS?.trim() || !data.TLS?.trim()) return '開發假資料以雙邊規格示範，TUS 與 TLS 不可空白'
  const yTop = data.TYAxisTo?.trim(), yBottom = data.TYAxisFrom?.trim()
  if (yTop && yBottom && Number(yBottom) >= Number(yTop)) return '趨勢圖 Y 軸下限必須小於上限'
  for (const field of ['TRule', 'XRule', 'RRule', 'SRule', 'MRule']) {
    const entries = data[field]?.split(';').map(item => item.trim()).filter(Boolean) ?? []
    for (const entry of entries) if (!/^DEV-RULE-01,(warning|wait|hold|alert)$/i.test(entry)) return `${field} 格式或開發用核准法則不正確`
  }
  return ''
}
function normalizeResults(payload: unknown): void {
  const items = Array.isArray(payload) ? payload : (payload as { rows?: unknown[]; items?: unknown[] } | null)?.rows ?? (payload as { items?: unknown[] } | null)?.items ?? []
  const byIndex = new Map<number, ApiRowResult>()
  items.forEach((item, index) => byIndex.set(Number((item as ApiRowResult).rowNo ?? index + 1), item as ApiRowResult))
  rows.value.forEach((row, index) => {
    const result = byIndex.get(index + 1)
    const message = String(result?.execute ?? result?.message ?? '')
    const succeeded = typeof result?.succeeded === 'boolean'
      ? result.succeeded
      : result?.result === 0 || result?.result === '0' || message.toLowerCase() === 'success'
    row.state = succeeded ? 'success' : 'failed'
    row.execute = message || (succeeded ? 'success' : (result ? '匯入失敗' : 'API 未回傳此列結果'))
    row.factorSerial = result?.factorSerial ?? ''
  })
}
async function save(): Promise<void> {
  if (busy.value || confirmationMessage.value) return
  if (hasExecuted.value) { error.value = '這批資料已執行，請選擇另一個檔案。'; return }
  if (!rows.value.length) { error.value = '請先載入匯入檔案。'; return }
  if (!await requestConfirmation(`將匯入 ${rows.value.length} 筆管制圖資料。已存在的 QC Item/要因組合會更新既有管制圖。是否繼續？`)) return
  busy.value = true
  error.value = ''
  notice.value = ''
  try {
    formatLimits()
    if (devMock.value) {
      for (const row of rows.value) {
        const result = validateDemoRow(row.data)
        row.state = result ? 'failed' : 'success'
        row.execute = result || 'success'
        row.factorSerial = result ? '' : `DEV-FS-${String(row.id).padStart(4, '0')}`
      }
    } else {
      const response = await request<unknown>(`${apiRoot}/import`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ rows: rows.value.map(row => row.data) }),
      })
      normalizeResults(response)
    }
    hasExecuted.value = true
    const failures = rows.value.filter(row => row.state === 'failed').length
    notice.value = `處理完成：${successCount.value} 筆成功${failures ? `，${failures} 筆失敗` : ''}。請選擇另一個檔案開始下一批。`
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '管制圖匯入失敗。' }
  finally { busy.value = false }
}
async function downloadTemplate(): Promise<void> {
  error.value = ''
  notice.value = ''
  try {
    let blob: Blob
    let downloadName = 'QCChart_Template.xlsx'
    if (devMock.value) blob = createXlsxTemplate(templateHeaders, 'QCChart', [templateHeaders.map(() => '')])
    else {
      const response = await fetch(apiUrl(`${apiRoot}/template`), { credentials: 'include' })
      if (response.status === 401) { window.location.assign(`${import.meta.env.BASE_URL}login`); throw new Error('登入已逾期，請重新登入。') }
      if (!response.ok) throw new Error(await apiErrorMessage(response))
      const disposition = response.headers.get('Content-Disposition') ?? ''
      const serverName = disposition.match(/filename\*?=(?:UTF-8''|\")?([^\";]+)/i)?.[1]
      if (serverName) downloadName = decodeURIComponent(serverName.trim())
      else if (/\.xls\b/i.test(disposition) || /ms-excel/i.test(response.headers.get('Content-Type') ?? '')) downloadName = 'QCChart_Template.xls'
      blob = await response.blob()
    }
    const url = URL.createObjectURL(blob)
    const anchor = document.createElement('a')
    anchor.href = url
    anchor.download = downloadName
    anchor.click()
    window.setTimeout(() => URL.revokeObjectURL(url), 0)
    notice.value = '範本已下載。'
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '範本下載失敗。' }
}
function setGridSelection(index: number, event: MouseEvent): void {
  if (event.shiftKey && anchorIndex.value !== null) {
    const low = Math.min(anchorIndex.value, index), high = Math.max(anchorIndex.value, index)
    selectedGridRows.value = Array.from({ length: high - low + 1 }, (_, offset) => low + offset)
  } else if (event.ctrlKey || event.metaKey) {
    selectedGridRows.value = selectedGridRows.value.includes(index)
      ? selectedGridRows.value.filter(item => item !== index)
      : [...selectedGridRows.value, index]
    anchorIndex.value = index
  } else {
    selectedGridRows.value = [index]
    anchorIndex.value = index
  }
}
function removeSelectedRows(): void {
  if (!selectedGridRows.value.length || hasExecuted.value) return
  const removedIds = new Set(selectedGridRows.value.map(index => rows.value[index]?.id))
  rows.value = rows.value.filter(row => !removedIds.has(row.id))
  selectedGridRows.value = []
  anchorIndex.value = null
}
function onGridKeydown(event: KeyboardEvent): void {
  if (event.key === 'Delete' && selectedGridRows.value.length && !hasExecuted.value) {
    event.preventDefault()
    removeSelectedRows()
  }
}
</script>

<template>
  <section class="qc-chart-import" @keydown="onGridKeydown">
    <header class="chart-titlebar">
      <div><h1>匯入管制圖內容</h1><span class="function-code">QC302</span></div>
      <span v-if="devMock" class="dev-indicator">DEV 假資料</span>
    </header>
    <div class="format-toolbar">
      <label class="format-check"><input v-model="formatValue" type="checkbox" /> Defined Format To:</label>
      <input v-model.number="rightDigits" class="digits-input" type="number" min="1" max="13" :disabled="!formatValue || busy || hasExecuted" />
      <span class="toolbar-hint">小數點位數（1–13）</span>
      <button v-if="devMock" class="dev-sample" type="button" @click="loadDevSample">載入開發範例</button>
      <span class="file-label">{{ fileName || '尚未載入檔案' }}</span>
    </div>
    <div class="chart-workspace">
      <div class="chart-grid-wrap">
        <div class="table-scroll" tabindex="0" aria-label="管制圖匯入資料表格">
          <table class="chart-grid">
            <thead><tr><th class="execute-column">Execute</th><th v-for="field in sourceFields" :key="field">{{ field }}</th><th>FactorSerial</th></tr></thead>
            <tbody>
              <tr v-for="(row, index) in rows" :key="row.id" :class="[`row-${row.state}`, { 'row-selected': selectedGridRows.includes(index) }]" @click="setGridSelection(index, $event)">
                <td class="execute-column" :title="row.execute">{{ row.execute }}</td>
                <td v-for="field in sourceFields" :key="field" :title="row.data[field]">
                  <input v-model="row.data[field]" :aria-label="`${field} 第 ${index + 1} 列`" :disabled="busy || hasExecuted" />
                </td>
                <td>{{ row.factorSerial }}</td>
              </tr>
              <tr v-if="!rows.length"><td :colspan="totalColumns" class="empty-grid">按「File」載入 Excel 檔案。<template v-if="devMock">亦可載入 DEV 預覽假資料。</template></td></tr>
            </tbody>
          </table>
        </div>
        <footer class="chart-status">
          <span>資料列總數 {{ rowCount }}</span><span>已執行 {{ resultCount }}</span><span>成功 {{ successCount }}</span>
          <span v-if="notice" class="notice-text" role="status">{{ notice }}</span><span v-if="error" class="error-text" role="alert">{{ error }}</span>
        </footer>
      </div>
      <aside class="button-rail" aria-label="管制圖匯入功能">
        <button type="button" @click="pickFile" :disabled="busy">File</button>
        <button type="button" class="primary" @click="save" :disabled="busy || !rows.length || hasExecuted">Save</button>
        <button type="button" @click="downloadTemplate" :disabled="busy">Template<br />DownLoad</button>
        <div class="rail-bottom"><button type="button" @click="emit('close')">Close</button></div>
      </aside>
    </div>
    <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
    <input ref="fileInput" class="hidden-file" type="file" :accept="devMock ? '.xls,.xlsx,.csv' : '.xls,.xlsx'" @change="handleFile" />
  </section>
</template>

<style scoped>
.qc-chart-import{height:100%;min-height:0;display:flex;flex-direction:column;background:#fff;color:#333;font-family:Arial,"Microsoft JhengHei",sans-serif;font-size:13px;border:1px solid #c9c9c9;box-sizing:border-box}.chart-titlebar{height:42px;flex:none;display:flex;align-items:center;justify-content:space-between;padding:0 14px;border-bottom:1px solid #d7d7d7;background:#f6f4ed}.chart-titlebar>div{display:flex;align-items:center;gap:12px}.chart-titlebar h1{margin:0;font-size:18px;font-weight:600;color:#302b24}.function-code{font-size:11px;color:#6a6257;border:1px solid #d8d0c2;border-radius:3px;padding:2px 6px}.dev-indicator{font-size:11px;border-radius:3px;background:#fff0cf;color:#81550b;padding:4px 7px}.format-toolbar{height:40px;flex:none;display:flex;align-items:center;gap:8px;padding:0 12px;border-bottom:1px solid #ccc;background:#fafafa}.format-check{display:flex;align-items:center;gap:5px;white-space:nowrap}.format-check input{accent-color:#e9a028}.digits-input{width:48px;height:25px;border:1px solid #aaa;border-radius:2px;padding:2px 4px;text-align:right}.digits-input:disabled{background:#eee;color:#888}.toolbar-hint{color:#777;font-size:11px}.dev-sample{border:0;background:transparent;color:#8b5d12;text-decoration:underline;cursor:pointer;font-size:11px}.file-label{margin-left:auto;max-width:40%;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;color:#777}.chart-workspace{display:flex;gap:8px;min-height:0;flex:1;padding:10px 8px 8px}.chart-grid-wrap{min-width:0;min-height:0;flex:1;display:flex;flex-direction:column;border:1px solid #c8c8c8}.table-scroll{min-height:0;flex:1;overflow:auto}.chart-grid{border-collapse:separate;border-spacing:0;min-width:max-content;width:100%;font-size:12px}.chart-grid th{position:sticky;top:0;z-index:1;height:27px;padding:3px 7px;background:#e8e8e8;color:#333;border-right:1px solid #ccc;border-bottom:1px solid #bdbdbd;text-align:left;font-weight:600;white-space:nowrap}.chart-grid td{height:25px;max-width:200px;padding:1px 4px;border-right:1px solid #d5d5d5;border-bottom:1px solid #d5d5d5;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}.chart-grid tbody tr{cursor:pointer}.chart-grid tbody tr:hover{background:#fff9e8}.chart-grid tbody tr.row-selected{outline:1px solid #dda52c;outline-offset:-1px;background:#fff3cf}.chart-grid tbody tr.row-failed{background:#ffe5e5}.chart-grid tbody tr.row-success{background:#ededed;color:#777}.chart-grid .execute-column{position:sticky;left:0;z-index:2;width:84px;min-width:84px;max-width:180px;background:#f7f7f7}.chart-grid td.execute-column{z-index:0}.chart-grid td input{width:100%;min-width:64px;height:22px;box-sizing:border-box;padding:2px 4px;border:1px solid transparent;background:transparent;color:inherit;font:inherit}.chart-grid td input:focus{border-color:#d99a26;outline:none;background:#fffdf5}.chart-grid td input:disabled{color:#777}.empty-grid{height:90px!important;text-align:center;color:#888;vertical-align:middle!important}.chart-status{min-height:27px;display:flex;align-items:center;gap:15px;flex-wrap:wrap;padding:3px 8px;border-top:1px solid #ccc;color:#777;font-size:12px}.notice-text{color:#47733a}.error-text{color:#b3261e}.button-rail{width:92px;flex:none;display:flex;flex-direction:column;gap:7px}.button-rail button{min-height:33px;padding:4px 5px;border:1px solid #bdbdbd;border-radius:4px;background:linear-gradient(#fff,#f2f2f2);color:#333;font-size:12px;cursor:pointer;box-shadow:0 1px 1px #00000010}.button-rail button:hover:not(:disabled){border-color:#d99a26;background:#fff8e9}.button-rail button.primary{border-color:#db941c;background:#f5a623;color:#fff;font-weight:600}.button-rail button:disabled{opacity:.48;cursor:default}.rail-bottom{margin-top:auto}.hidden-file{display:none}@media(max-width:700px){.chart-workspace{padding:6px;gap:5px}.button-rail{width:78px}.button-rail button{font-size:11px;padding:3px}.chart-status{gap:7px;font-size:11px}.chart-titlebar h1{font-size:16px}.format-toolbar{gap:5px;padding:0 7px}.toolbar-hint{display:none}.file-label{max-width:28%}}
</style>
