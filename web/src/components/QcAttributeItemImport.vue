<script setup lang="ts">
import { computed, ref } from 'vue'
import { createXlsxTemplate, parseFirstWorksheetXlsx, parseMaterialCsv } from './materialImportWorkbook'

type ImportRecord = Record<string, string | number | boolean>
interface ImportRow {
  id: number
  selected: boolean
  result: 0 | 1
  execute: string
  locked: boolean
  checked: boolean
  data: Record<string, string>
}
interface ApiRowResult { rowNo?: number; qcItemNo?: string; result?: number | string; succeeded?: boolean; execute?: string; message?: string }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const devMock = computed(() => import.meta.env.DEV && props.developmentPreview)
const apiRoot = 'api/qc/attribute-item-import'
const storageKey = 'ymes-dev-mock:qc-attribute-item-import:v1'
const fields = [
  'QCItemNo', 'QCItemName', 'LotIsVaild', 'PlanNo', 'ComponentCollect', 'RuleNo', 'Description', 'LimitLotNum', 'MClassNo',
  'FactorNo', 'FactorOnline', 'InlineOP', 'OfflineOP', 'INSPTYPE_FAI', 'INSPTYPE_IPQC', 'INSPTYPE_PQC', 'Reason', 'Creator', 'CreateDate',
]
const visibleFields = ['QCItemNo', 'QCItemName', 'LotIsVaild', 'PlanNo', 'ComponentCollect', 'RuleNo', 'Description', 'LimitLotNum', 'MClassNo', 'FactorNo', 'FactorOnline', 'InlineOP', 'OfflineOP', 'INSPTYPE_FAI', 'INSPTYPE_IPQC', 'INSPTYPE_PQC', 'Reason', 'Creator', 'CreateDate', 'Execute']
const fieldLabels: Record<string, string> = {
  QCItemNo: 'QCItemNo', QCItemName: 'QCItemName', LotIsVaild: 'LotIsVaild', PlanNo: 'PlanNo', ComponentCollect: 'ComponentCollect',
  RuleNo: 'RuleNo', Description: 'Description', LimitLotNum: 'LimitLotNum', MClassNo: 'MClassNo', Creator: 'Creator', CreateDate: 'CreateDate',
  FactorNo: 'FactorNo', FactorOnline: 'FactorOnline', InlineOP: 'InlineOP', OfflineOP: 'OfflineOP', INSPTYPE_FAI: 'INSPTYPE_FAI',
  INSPTYPE_IPQC: 'INSPTYPE_IPQC', INSPTYPE_PQC: 'INSPTYPE_PQC', Reason: 'Reason', Execute: 'Execute',
}
const rows = ref<ImportRow[]>([])
const fileName = ref('')
const busy = ref(false)
const error = ref('')
const notice = ref('')
const fileInput = ref<HTMLInputElement | null>(null)
const selectedGridRows = ref<number[]>([])
const anchorIndex = ref<number | null>(null)
const rowCount = computed(() => rows.value.length)
const selectedCount = computed(() => rows.value.filter(row => row.selected && !row.locked).length)
const checkedCount = computed(() => rows.value.filter(row => row.checked).length)
const templateHeaders = [...fields]
let nextRowId = 1

function emptyData(): Record<string, string> { return Object.fromEntries(fields.map(field => [field, ''])) }
function makeRow(data: Record<string, string>, id = nextRowId++): ImportRow {
  return { id, selected: false, result: 0, execute: '', locked: false, checked: false, data: { ...emptyData(), ...data } }
}
function seedRows(): ImportRow[] {
  const examples: Record<string, string>[] = [
    { QCItemNo: 'DEV-QCA-101', QCItemName: '外觀缺點判定（開發假資料）', LotIsVaild: '1', PlanNo: 'DEV-PLAN-LOT', ComponentCollect: '0', RuleNo: 'DEV-ATT-RULE-LOT', Description: '僅供介面操作示範', LimitLotNum: '1', FactorNo: 'PRODUCT;MATERIAL', FactorOnline: 'PRODUCT', InlineOP: 'DEV-OP-10', INSPTYPE_FAI: '1', INSPTYPE_IPQC: '1', INSPTYPE_PQC: '1', Reason: 'DEV-RSN-001;DEV-RSN-002', Creator: 'DEV001', CreateDate: '2026-11-17 08:00:00' },
    { QCItemNo: 'DEV-QCA-102', QCItemName: '元件外觀檢查（開發假資料）', LotIsVaild: '1', PlanNo: 'DEV-PLAN-WAFER', ComponentCollect: '1', RuleNo: 'DEV-ATT-RULE-WAFER', Description: '僅供介面操作示範', LimitLotNum: '2', FactorNo: 'MATERIAL', FactorOnline: 'MATERIAL', OfflineOP: 'DEV-OP-20', INSPTYPE_PQC: '1', Reason: 'DEV-RSN-003', Creator: 'DEV001', CreateDate: '2026-11-17 08:00:00' },
    { QCItemNo: 'DEV-QCA-103', QCItemName: '元件缺點（檢查失敗假資料）', LotIsVaild: '0', PlanNo: 'DEV-PLAN-WAFER', ComponentCollect: '1', LimitLotNum: '0', InlineOP: 'DEV-OP-10', Reason: 'DEV-RSN-999' },
  ]
  return examples.map(data => makeRow(data))
}
function readMockRows(): ImportRow[] {
  try {
    const value = localStorage.getItem(storageKey)
    if (value) {
      const parsed = JSON.parse(value) as ImportRow[]
      if (Array.isArray(parsed)) return parsed
    }
  } catch { /* initialize isolated DEV fixtures */ }
  const initial = seedRows()
  writeMockRows(initial)
  return initial
}
function writeMockRows(value: ImportRow[]): void { localStorage.setItem(storageKey, JSON.stringify(value)) }
function apiUrl(path: string): string {
  if (!props.apiBaseUrl.trim()) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(props.apiBaseUrl.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init })
  if (response.status === 401) { window.location.assign(`${import.meta.env.BASE_URL}login`); throw new Error('登入已逾期，請重新登入。') }
  const body = await response.text()
  if (!response.ok) throw new Error(body || `API 請求失敗 (${response.status})`)
  return (body ? JSON.parse(body) : undefined) as T
}
function normalizeData(raw: ImportRecord): Record<string, string> {
  const normalized = emptyData()
  const byUpper = new Map(Object.entries(raw).map(([key, value]) => [key.toUpperCase(), String(value ?? '').trim()]))
  for (const field of fields) normalized[field] = byUpper.get(field.toUpperCase()) ?? ''
  normalized.QCItemNo = normalized.QCItemNo.toUpperCase()
  normalized.PlanNo = normalized.PlanNo.toUpperCase()
  normalized.RuleNo = normalized.RuleNo.toUpperCase()
  normalized.MClassNo = normalized.MClassNo.toUpperCase()
  return normalized
}
function rowsFromMatrix(matrix: string[][], skipTemplateExample: boolean): ImportRow[] {
  if (!matrix.length) throw new Error('檔案沒有工作表資料。')
  const headers = matrix[0].map((header, index) => header.trim() || `unknow${index}`)
  const start = skipTemplateExample && matrix.length > 1 ? 2 : 1
  const result: ImportRow[] = []
  for (const cells of matrix.slice(start)) {
    if (!cells.some(cell => cell.trim())) break
    const data = emptyData()
    headers.forEach((header, index) => {
      const value = cells[index]?.trim() ?? ''
      if (header.toUpperCase() !== 'SELECT' && header.toUpperCase() !== 'RESULT' && header.toUpperCase() !== 'EXECUTE') data[header] = value
    })
    result.push(makeRow(normalizeData(data)))
  }
  return result
}
function normalizeResults(payload: unknown, submitted: ImportRow[], onlyCheck: boolean): void {
  const items = Array.isArray(payload) ? payload : (payload as { rows?: unknown[]; items?: unknown[] } | null)?.rows ?? (payload as { items?: unknown[] } | null)?.items ?? []
  const byIndex = new Map<number, ApiRowResult>(items.map((item, index) => [Number((item as ApiRowResult).rowNo ?? index + 1), item as ApiRowResult]))
  submitted.forEach((row, index) => {
    const result = byIndex.get(index + 1)
    const message = String(result?.execute ?? result?.message ?? '')
    const succeeded = typeof result?.succeeded === 'boolean'
      ? result.succeeded
      : result?.result === 0 || result?.result === '0' || message.toLowerCase() === 'success'
    row.result = succeeded ? 0 : 1
    row.execute = message || (succeeded ? (onlyCheck ? '' : 'success') : (result ? '資料檢查失敗' : 'API 未回傳此列結果'))
    row.checked = succeeded
    if (!onlyCheck && succeeded) { row.locked = true; row.selected = false }
  })
}
function validateDemoRow(data: Record<string, string>): string {
  const required = ['QCItemNo', 'QCItemName', 'LotIsVaild', 'PlanNo', 'LimitLotNum']
  const missing = required.find(field => !data[field]?.trim())
  if (missing) return `${missing} 不可空白`
  if (!['0', '1'].includes(data.LotIsVaild)) return 'LotIsVaild 必須為 0 或 1'
  if (!['0', '1'].includes(data.ComponentCollect || '0')) return 'ComponentCollect 必須為 0 或 1'
  if (!/^\d+$/.test(data.LimitLotNum) || Number(data.LimitLotNum) <= 0) return 'LimitLotNum 必須為大於 0 的整數'
  if (data.ComponentCollect === '1' && data.LotIsVaild !== '1') return 'ComponentCollect=1 時 LotIsVaild 必須為 1'
  if (data.PlanNo !== (data.ComponentCollect === '1' ? 'DEV-PLAN-WAFER' : 'DEV-PLAN-LOT')) return 'PlanNo 不符合開發用抽樣計畫類型'
  if (data.RuleNo && data.RuleNo !== (data.ComponentCollect === '1' ? 'DEV-ATT-RULE-WAFER' : 'DEV-ATT-RULE-LOT')) return 'RuleNo 必須符合抽樣計畫類型且為已核准的開發用假資料'
  const factorFields = ['FactorNo', 'FactorOnline'].map(field => Boolean(data[field]?.trim()))
  if (factorFields.some(Boolean) && !factorFields.every(Boolean)) return 'FactorNo、FactorOnline 必須同時填寫或同時空白'
  if (data.FactorNo && data.FactorOnline.split(';').some(value => !data.FactorNo.split(';').includes(value))) return 'FactorOnline 項目必須包含於 FactorNo'
  if (data.FactorNo && data.FactorNo.split(';').some(value => !['PRODUCT', 'MATERIAL'].includes(value))) return 'FactorNo 需是核准的開發用要因假資料'
  for (const field of ['INSPTYPE_FAI', 'INSPTYPE_IPQC', 'INSPTYPE_PQC']) if (data[field] && !['0', '1'].includes(data[field])) return `${field} 必須為 0 或 1`
  const inline = data.InlineOP.split(';').filter(Boolean), offline = data.OfflineOP.split(';').filter(Boolean)
  if (inline.length && data.LotIsVaild !== '1') return '有 InlineOP 時 LotIsVaild 必須為 1'
  if (inline.some(op => offline.includes(op))) return '同一作業站不可同時列入 InlineOP 與 OfflineOP'
  if ([...inline, ...offline].some(op => !['DEV-OP-10', 'DEV-OP-20'].includes(op))) return '作業站需是已核准的開發用假資料'
  const reasons = data.Reason.split(';').filter(Boolean)
  if (reasons.some(reason => !['DEV-RSN-001', 'DEV-RSN-002', 'DEV-RSN-003'].includes(reason))) return 'Reason 必須是已核准的開發用原因假資料'
  if (new Set(reasons).size !== reasons.length) return 'Reason 不可重複'
  return ''
}
function pickFile(): void { error.value = ''; notice.value = ''; fileInput.value?.click() }
function loadDevSample(): void {
  rows.value = readMockRows(); fileName.value = 'DEV 假資料（開發範例）'; selectedGridRows.value = []; anchorIndex.value = null
  error.value = ''; notice.value = '載入 3 筆開發範例；DEV 假資料不會寫入正式資料庫。'
}
async function handleFile(event: Event): Promise<void> {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file) return
  busy.value = true; error.value = ''; notice.value = ''; fileName.value = file.name
  try {
    if (devMock.value) {
      const extension = file.name.toLowerCase().split('.').pop()
      if (extension === 'xlsx') rows.value = rowsFromMatrix(await parseFirstWorksheetXlsx(file), true)
      else if (extension === 'csv') rows.value = rowsFromMatrix(parseMaterialCsv(await file.text()), false)
      else throw new Error('DEV 預覽支援 .xlsx 與 .csv；正式 API 可解析 WinForms 使用的 .xls。')
    } else {
      const form = new FormData(); form.append('file', file)
      const payload = await request<{ rows?: ImportRecord[] }>(`${apiRoot}/preview`, { method: 'POST', body: form })
      rows.value = (payload.rows ?? []).map(record => makeRow(normalizeData(record)))
    }
    selectedGridRows.value = []; anchorIndex.value = null
    notice.value = `載入 ${rows.value.length} 筆資料。請勾選要檢查或儲存的列。`
  } catch (reason) { rows.value = []; error.value = reason instanceof Error ? reason.message : '讀取匯入檔案失敗。' }
  finally { busy.value = false }
}
async function runRows(onlyCheck: boolean): Promise<void> {
  const target = rows.value.filter(row => row.selected && !row.locked)
  if (!target.length) { error.value = '請先勾選要處理的資料列。'; return }
  if (!onlyCheck && !window.confirm(`將儲存 ${target.length} 筆勾選資料；若 QCItemNo 已存在，舊定義會依來源流程替換。是否繼續？`)) return
  busy.value = true; error.value = ''; notice.value = ''
  try {
    if (devMock.value) {
      target.forEach(row => {
        const result = validateDemoRow(row.data)
        row.result = result ? 1 : 0
        row.execute = result || (!onlyCheck ? 'success' : '')
        row.checked = !result
        if (!result && !onlyCheck) { row.locked = true; row.selected = false }
      })
      writeMockRows(rows.value)
    } else {
      const response = await request<unknown>(`${apiRoot}/${onlyCheck ? 'check' : 'import'}`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ rows: target.map(row => row.data) }),
      })
      normalizeResults(response, target, onlyCheck)
    }
    const successes = target.filter(row => row.result === 0).length
    const failures = target.length - successes
    notice.value = onlyCheck
      ? `檢查完成：${successes} 筆通過${failures ? `，${failures} 筆錯誤` : ''}。`
      : `儲存完成：${successes} 筆成功${failures ? `，${failures} 筆失敗` : ''}。`
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '匯入作業失敗。' }
  finally { busy.value = false }
}
async function downloadTemplate(): Promise<void> {
  error.value = ''; notice.value = ''
  try {
    let blob: Blob
    let downloadName = 'AttributesQCItem_Template.xlsx'
    if (devMock.value) blob = createXlsxTemplate(templateHeaders, 'AttributesQCItem', [fields.map(() => '')])
    else {
      const response = await fetch(apiUrl(`${apiRoot}/template`), { credentials: 'include' })
      if (response.status === 401) { window.location.assign(`${import.meta.env.BASE_URL}login`); throw new Error('登入已逾期，請重新登入。') }
      if (!response.ok) throw new Error(`範本下載失敗 (${response.status})`)
      const disposition = response.headers.get('Content-Disposition') ?? ''
      const serverName = disposition.match(/filename\*?=(?:UTF-8''|\")?([^\";]+)/i)?.[1]
      if (serverName) downloadName = decodeURIComponent(serverName.trim())
      else if (/\.xls\b/i.test(disposition) || /ms-excel/i.test(response.headers.get('Content-Type') ?? '')) downloadName = 'AttributesQCItem_Template.xls'
      blob = await response.blob()
    }
    const url = URL.createObjectURL(blob); const anchor = document.createElement('a')
    anchor.href = url; anchor.download = downloadName; anchor.click(); URL.revokeObjectURL(url)
    notice.value = '範本已下載。'
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '範本下載失敗。' }
}
function selectAll(): void { rows.value.forEach(row => { if (!row.locked) row.selected = true }) }
function unselectAll(): void { rows.value.forEach(row => { row.selected = false }) }
function setGridSelection(index: number, event: MouseEvent): void {
  const selected = [...selectedGridRows.value]
  if (event.shiftKey && anchorIndex.value !== null) {
    const low = Math.min(anchorIndex.value, index), high = Math.max(anchorIndex.value, index)
    selectedGridRows.value = Array.from({ length: high - low + 1 }, (_, offset) => low + offset)
  } else if (event.ctrlKey || event.metaKey) {
    selectedGridRows.value = selected.includes(index) ? selected.filter(item => item !== index) : [...selected, index]
    anchorIndex.value = index
  } else { selectedGridRows.value = [index]; anchorIndex.value = index }
}
function multiSelect(): void { selectedGridRows.value.forEach(index => { const row = rows.value[index]; if (row && !row.locked) row.selected = true }) }
function removeSelectedRows(): void {
  if (!selectedGridRows.value.length) return
  const removed = new Set(selectedGridRows.value.map(index => rows.value[index]?.id))
  rows.value = rows.value.filter(row => !removed.has(row.id))
  selectedGridRows.value = []; anchorIndex.value = null
}
function onGridKeydown(event: KeyboardEvent): void { if (event.key === 'Delete' && selectedGridRows.value.length) { event.preventDefault(); removeSelectedRows() } }
function resultText(row: ImportRow): string { return row.locked ? 'success' : row.execute }
</script>

<template>
  <section class="qc-variable-import" @keydown="onGridKeydown">
    <header class="import-titlebar">
      <div><h1>計數品管項目匯入</h1><span class="function-code">QC215</span></div>
      <span v-if="devMock" class="dev-indicator">DEV 假資料</span>
    </header>
    <div class="import-workspace">
      <div class="import-grid-wrap">
        <div class="grid-filebar"><span class="grid-caption">Import Attributes QCItem</span><button v-if="devMock" class="dev-sample" type="button" @click="loadDevSample">載入開發範例</button><span class="file-label">{{ fileName || '尚未載入檔案' }}</span></div>
        <div class="table-scroll" tabindex="0" aria-label="匯入資料表格">
          <table class="import-grid">
            <thead><tr><th class="check-cell">Select</th><th class="row-number">#</th><th v-for="field in visibleFields" :key="field">{{ fieldLabels[field] }}</th></tr></thead>
            <tbody>
              <tr v-for="(row, index) in rows" :key="row.id" :tabindex="0" :class="{ 'row-failed': row.result === 1, 'row-success': row.locked, 'row-selected': selectedGridRows.includes(index) }" @click="setGridSelection(index, $event)">
                <td class="check-cell" @click.stop><input v-model="row.selected" type="checkbox" :disabled="row.locked" :aria-label="`選取第 ${index + 1} 列`" /></td>
                <td class="row-number">{{ index + 1 }}</td>
                <td v-for="field in visibleFields" :key="field" :title="field === 'Execute' ? resultText(row) : row.data[field]">
                  <span v-if="field === 'Execute'">{{ resultText(row) }}</span><span v-else>{{ row.data[field] }}</span>
                </td>
              </tr>
              <tr v-if="!rows.length"><td :colspan="visibleFields.length + 2" class="empty-grid">按「File」載入匯入檔案；開發預覽中的資料均為假資料。</td></tr>
            </tbody>
          </table>
        </div>
        <footer class="import-status"><span>資料列總數 {{ rowCount }}</span><span>已勾選 {{ selectedCount }}</span><span>已檢查 {{ checkedCount }}</span><span v-if="notice" class="notice-text">{{ notice }}</span><span v-if="error" class="error-text">{{ error }}</span></footer>
      </div>
      <aside class="button-rail" aria-label="匯入功能">
        <button type="button" @click="pickFile" :disabled="busy">File</button>
        <button type="button" @click="runRows(true)" :disabled="busy || !rows.length">Check Data</button>
        <button type="button" class="primary" @click="runRows(false)" :disabled="busy || !rows.length">Save</button>
        <button type="button" @click="downloadTemplate" :disabled="busy">Template<br />DownLoad</button>
        <div class="rail-gap"></div>
        <button type="button" @click="selectAll" :disabled="!rows.length">Select All</button>
        <button type="button" @click="unselectAll" :disabled="!rows.length">UnSelect All</button>
        <button type="button" @click="multiSelect" :disabled="!selectedGridRows.length">MultiSelect</button>
        <div class="rail-bottom"><button type="button" @click="emit('close')">Close</button></div>
      </aside>
    </div>
    <input ref="fileInput" class="hidden-file" type="file" accept=".xls,.xlsx,.csv" @change="handleFile" />
  </section>
</template>

<style scoped>
.qc-variable-import{height:100%;min-height:0;display:flex;flex-direction:column;background:#fff;color:#333;font-family:Arial,"Microsoft JhengHei",sans-serif;font-size:13px;border:1px solid #c9c9c9;box-sizing:border-box}.import-titlebar{height:42px;flex:none;display:flex;align-items:center;justify-content:space-between;padding:0 14px;border-bottom:1px solid #d7d7d7;background:#f6f4ed}.import-titlebar>div{display:flex;align-items:center;gap:12px}.import-titlebar h1{margin:0;font-size:18px;font-weight:600;color:#302b24}.function-code{font-size:11px;color:#6a6257;border:1px solid #d8d0c2;border-radius:3px;padding:2px 6px}.dev-indicator{font-size:11px;border-radius:3px;background:#fff0cf;color:#81550b;padding:4px 7px}.import-workspace{display:flex;gap:8px;min-height:0;flex:1;padding:10px 8px 8px}.import-grid-wrap{min-width:0;min-height:0;flex:1;display:flex;flex-direction:column;border:1px solid #c8c8c8}.grid-filebar{height:30px;display:flex;align-items:center;gap:10px;padding:0 8px;border-bottom:1px solid #c8c8c8;background:#f4f4f4}.grid-caption{font-weight:600}.dev-sample{border:0;background:transparent;color:#8b5d12;text-decoration:underline;cursor:pointer;font-size:11px}.file-label{margin-left:auto;max-width:50%;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;color:#777}.table-scroll{min-height:0;flex:1;overflow:auto}.import-grid{border-collapse:separate;border-spacing:0;min-width:max-content;width:100%;font-size:12px}.import-grid th{position:sticky;top:0;z-index:1;height:27px;padding:3px 7px;background:#e8e8e8;color:#333;border-right:1px solid #ccc;border-bottom:1px solid #bdbdbd;text-align:left;font-weight:600;white-space:nowrap}.import-grid td{height:25px;max-width:240px;padding:2px 7px;border-right:1px solid #d5d5d5;border-bottom:1px solid #d5d5d5;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}.import-grid tbody tr{cursor:pointer}.import-grid tbody tr:hover{background:#fff9e8}.import-grid tbody tr.row-selected{outline:1px solid #dda52c;outline-offset:-1px;background:#fff3cf}.import-grid tbody tr.row-failed{background:#ffe5e5}.import-grid tbody tr.row-success{background:#ededed;color:#777}.import-grid .check-cell{position:sticky;left:0;z-index:2;width:48px;min-width:48px;text-align:center;background:#f7f7f7}.import-grid td.check-cell{z-index:0}.import-grid .row-number{position:sticky;left:48px;z-index:2;width:38px;min-width:38px;text-align:center;background:#f7f7f7;color:#777}.import-grid input[type=checkbox]{accent-color:#e9a028}.empty-grid{height:90px!important;text-align:center;color:#888;vertical-align:middle!important}.import-status{min-height:27px;display:flex;align-items:center;gap:15px;flex-wrap:wrap;padding:3px 8px;border-top:1px solid #ccc;color:#777;font-size:12px}.notice-text{color:#47733a}.error-text{color:#b3261e}.button-rail{width:92px;flex:none;display:flex;flex-direction:column;gap:7px}.button-rail button{min-height:33px;padding:4px 5px;border:1px solid #bdbdbd;border-radius:4px;background:linear-gradient(#fff,#f2f2f2);color:#333;font-size:12px;cursor:pointer;box-shadow:0 1px 1px #00000010}.button-rail button:hover:not(:disabled){border-color:#d99a26;background:#fff8e9}.button-rail button.primary{border-color:#db941c;background:#f5a623;color:#fff;font-weight:600}.button-rail button:disabled{opacity:.48;cursor:default}.rail-gap{height:82px;flex:none}.rail-bottom{margin-top:auto}.hidden-file{display:none}@media(max-width:700px){.import-workspace{padding:6px;gap:5px}.button-rail{width:78px}.button-rail button{font-size:11px;padding:3px}.import-status{gap:7px;font-size:11px}.import-titlebar h1{font-size:16px}}
</style>
