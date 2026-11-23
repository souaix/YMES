<script setup lang="ts">
import { computed, ref } from 'vue'
import { createXlsxTemplate, parseFirstWorksheetXlsx, parseMaterialCsv } from './materialImportWorkbook'

const props = withDefaults(defineProps<{ developmentPreview?: boolean; currentUserNo?: string }>(), { developmentPreview: false, currentUserNo: '' })
const emit = defineEmits<{ close: [] }>()
const devPreview = computed(() => import.meta.env.DEV && props.developmentPreview)
type ImportedRow = { id: number; selected: boolean; execute: string; result: string; checked: boolean; locked: boolean; values: Record<string, string> }

const ordinaryHeaders = ['LOTNO', 'OPNO', 'QCITEMNO', 'EMPLOYEE', 'S01', 'S02', 'S03']
const componentHeaders = ['LOTNO', 'OPNO', 'QCITEMNO', 'COMPONENTNO', 'EMPLOYEE', 'S01', 'S02', 'S03']
const rows = ref<ImportedRow[]>([])
const headers = ref<string[]>([])
const fileName = ref('尚未載入檔案')
const componentTemplate = ref(false)
const busy = ref(false)
const error = ref('')
const notice = ref('請載入 Excel／CSV 或開發範例，再選取資料執行 Check 或 Save。')
const fileInput = ref<HTMLInputElement | null>(null)
let rowId = 1

const measureHeaders = computed(() => headers.value.filter((header) => /^S\d{2}$/.test(header)).sort((a, b) => Number(a.slice(1)) - Number(b.slice(1))))
const visibleHeaders = computed(() => headers.value)
const pendingCount = computed(() => rows.value.filter((row) => !row.locked).length)
const selectedCount = computed(() => rows.value.filter((row) => row.selected && !row.locked).length)
const checkedCount = computed(() => rows.value.filter((row) => row.checked).length)
const importedComponentData = computed(() => headers.value.includes('COMPONENTNO'))

function normalizeHeader(header: string, index: number): string {
  return (header.trim() || `UNKNOW${index}`).toUpperCase()
}

function rowsFromMatrix(matrix: string[][]): { headers: string[]; rows: ImportedRow[] } {
  if (!matrix.length) throw new Error('檔案沒有工作表資料。')
  const names = matrix[0].map(normalizeHeader)
  if (new Set(names).size !== names.length) throw new Error('Excel 欄名重複，請確認範本欄位。')
  if (!names.some((name) => /^S\d{2}$/.test(name))) throw new Error('找不到 S01 至 S99 的計量欄位。')
  const imported: ImportedRow[] = []
  for (const cells of matrix.slice(1)) {
    if (!cells.some((cell) => cell.trim())) break
    const values = Object.fromEntries(names.map((name, index) => [name, cells[index]?.trim() ?? '']))
    imported.push({ id: rowId++, selected: false, execute: '', result: '', checked: false, locked: false, values })
  }
  return { headers: names, rows: imported }
}

function loadSample(): void {
  const names = componentTemplate.value ? componentHeaders : ordinaryHeaders
  const base: Record<string, string>[] = [
    { LOTNO: 'LOT-SC107-001', OPNO: 'OP-DEV-10', QCITEMNO: 'DEV-QCV-101', EMPLOYEE: props.currentUserNo || 'DEV-QC-01', S01: '10.12', S02: '10.08', S03: '10.15' },
    { LOTNO: 'LOT-SC107-002', OPNO: 'OP-DEV-20', QCITEMNO: 'DEV-QCV-102', EMPLOYEE: props.currentUserNo || 'DEV-QC-02', S01: '10.10', S02: '10.11', S03: '10.09' },
    { LOTNO: 'LOT-DEV-404', OPNO: 'OP-DEV-10', QCITEMNO: 'DEV-QCV-101', EMPLOYEE: props.currentUserNo || 'DEV-QC-01', S01: '10.10', S02: '10.11', S03: '10.09' },
    { LOTNO: 'LOT-SC107-001', OPNO: 'OP-DEV-10', QCITEMNO: 'DEV-QCV-101', EMPLOYEE: props.currentUserNo || 'DEV-QC-01', S01: '10.10', S02: 'not-a-number', S03: '10.09' },
  ]
  headers.value = [...names]
  rows.value = base.map((record, index) => {
    const values = Object.fromEntries(names.map((name) => [name, name === 'COMPONENTNO' ? `DEV-COMP-${String(index + 1).padStart(3, '0')}` : record[name] ?? '']))
    return { id: rowId++, selected: index !== 2, execute: '', result: '', checked: false, locked: false, values }
  })
  fileName.value = `DEV 假資料（${componentTemplate.value ? '元件' : '一般'}格式）`
  error.value = ''
  notice.value = `載入 ${rows.value.length} 筆開發範例；資料只存在此頁記憶體。`
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
    const extension = file.name.toLowerCase().split('.').pop()
    let matrix: string[][]
    if (extension === 'xlsx') matrix = await parseFirstWorksheetXlsx(file)
    else if (extension === 'csv') matrix = parseMaterialCsv(await file.text())
    else throw new Error('DEV 預覽支援 .xlsx 與 .csv；WinForms 原生 .xls 解析留給正式 Service。')
    const imported = rowsFromMatrix(matrix)
    headers.value = imported.headers
    rows.value = imported.rows
    notice.value = `載入 ${rows.value.length} 筆資料。請勾選要處理的列。`
  } catch (reason) {
    rows.value = []
    headers.value = []
    error.value = reason instanceof Error ? reason.message : '讀取匯入檔案失敗。'
  } finally { busy.value = false }
}

function validateRow(row: ImportedRow, checkOnly: boolean): string {
  const lotNo = row.values.LOTNO?.trim() ?? ''
  const qcItemNo = row.values.QCITEMNO?.trim().toUpperCase() ?? ''
  if (!lotNo || !qcItemNo) return 'LotNo / QCItemNo 不可空白'
  if (!measureHeaders.value.length) return 'No Data'
  for (const field of measureHeaders.value) {
    const value = row.values[field]?.trim() ?? ''
    if (!value) return `${field} 不可空白`
    if (!Number.isFinite(Number(value))) return `${field} 必須為數值`
  }
  if (importedComponentData.value && !row.values.COMPONENTNO?.trim()) return 'ComponentNo 不可空白'
  if (!['LOT-SC107-001', 'LOT-SC107-002'].includes(lotNo)) return 'LotData Error'
  if (!['DEV-QCV-101', 'DEV-QCV-102'].includes(qcItemNo)) return 'QCItemNo Error'
  if (checkOnly && lotNo === 'LOT-SC107-002' && qcItemNo === 'DEV-QCV-102') return 'Already Exist'
  return ''
}

function selectAll(): void { rows.value.forEach((row) => { if (!row.locked) row.selected = true }) }
function unselectAll(): void { rows.value.forEach((row) => { row.selected = false }) }

function runCheck(): void {
  if (!rows.value.length) { error.value = '請先載入匯入檔案。'; return }
  const selected = rows.value.filter((row) => row.selected && !row.locked)
  if (!selected.length) { error.value = '請先勾選要檢查的資料列。'; return }
  error.value = ''
  selected.forEach((row) => {
    const result = validateRow(row, true)
    row.checked = true
    row.result = result
    if (result) row.selected = false
  })
  const passed = selected.filter((row) => !row.result).length
  notice.value = `檢查完成：${passed} 筆通過，${selected.length - passed} 筆錯誤。通過列會保留勾選供 Save 使用。`
}

function runSave(): void {
  if (!rows.value.length) { error.value = '請先載入匯入檔案。'; return }
  const selected = rows.value.filter((row) => row.selected && !row.locked)
  if (!selected.length) { error.value = '請先勾選要儲存的資料列。'; return }
  error.value = ''
  selected.forEach((row) => {
    const result = validateRow(row, false)
    row.checked = true
    row.result = result || 'success'
    if (result) {
      row.execute = result
      return
    }
    row.execute = 'True'
    row.locked = true
    row.selected = false
  })
  const succeeded = selected.filter((row) => row.locked).length
  notice.value = `DEV 模擬儲存完成：${succeeded} 筆成功，${selected.length - succeeded} 筆失敗。未寫入 MES／Oracle／PostgreSQL。`
}

function downloadTemplate(): void {
  const names = componentTemplate.value ? componentHeaders : ordinaryHeaders
  const blob = createXlsxTemplate(names, 'SPCData', [names.map(() => '')])
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = componentTemplate.value ? 'SPCData_ComponentNo_Template.xlsx' : 'SPCData_Template.xlsx'
  anchor.click()
  window.setTimeout(() => URL.revokeObjectURL(url), 1000)
  notice.value = `已產生 ${anchor.download} 開發用範本；不代表正式系統下載的範本內容。`
}

function displayState(row: ImportedRow): string {
  if (row.locked) return '已儲存'
  if (row.checked && !row.result) return '檢查通過'
  if (row.result && row.result !== 'success') return '錯誤'
  return '待處理'
}
</script>

<template>
  <section v-if="devPreview" class="spc-import-page" aria-label="匯入計量檢測數據">
    <header class="page-heading">
      <div><p class="eyebrow">SPC / SC107</p><h1>匯入計量檢測數據</h1><p class="subheading">以 Excel 欄位列匯入批次計量值，逐列檢查後儲存。</p></div>
      <button class="close-button" type="button" @click="emit('close')">Close (X)</button>
    </header>

    <div class="toolbar">
      <button type="button" @click="fileInput?.click()" :disabled="busy">File</button>
      <button type="button" @click="runCheck" :disabled="busy">Check</button>
      <button type="button" @click="selectAll" :disabled="busy || !rows.length">SelectAll</button>
      <button type="button" @click="unselectAll" :disabled="busy || !rows.length">UnSelectAll</button>
      <button class="primary" type="button" @click="runSave" :disabled="busy || !rows.length">Save</button>
      <button type="button" @click="downloadTemplate">Template DownLoad</button>
      <input ref="fileInput" class="visually-hidden" type="file" accept=".xlsx,.csv" @change="handleFile">
    </div>

    <div class="template-mode" role="group" aria-label="範本欄位類型">
      <span>TemplateFile：</span>
      <label><input v-model="componentTemplate" type="radio" :value="false"> SPCData_Template</label>
      <label><input v-model="componentTemplate" type="radio" :value="true"> SPCData_ComponentNo_Template</label>
      <button type="button" class="sample-button" @click="loadSample">載入 DEV 範例資料</button>
    </div>

    <div class="file-line"><span>File：</span><strong>{{ fileName }}</strong><span class="counter">資料 {{ rows.length }} 列 · 待處理 {{ pendingCount }} · 勾選 {{ selectedCount }} · 已檢查 {{ checkedCount }}</span></div>
    <p class="notice" role="status">{{ notice }}</p>
    <p v-if="error" class="error" role="alert">{{ error }}</p>

    <div class="grid-wrap">
      <table>
        <thead><tr><th class="check-cell">Select</th><th class="index-cell">#</th><th v-for="field in visibleHeaders" :key="field">{{ field }}</th><th>Execute</th><th>Result</th><th>狀態</th></tr></thead>
        <tbody>
          <tr v-for="(row, index) in rows" :key="row.id" :class="{ saved: row.locked, failed: row.result && row.result !== 'success' }">
            <td class="check-cell"><input v-model="row.selected" type="checkbox" :disabled="row.locked" :aria-label="`選取第 ${index + 1} 列`"></td>
            <td class="index-cell">{{ index + 1 }}</td>
            <td v-for="field in visibleHeaders" :key="field" class="value-cell">{{ row.values[field] || '—' }}</td>
            <td>{{ row.execute || '—' }}</td><td>{{ row.result || '—' }}</td><td>{{ displayState(row) }}</td>
          </tr>
          <tr v-if="!rows.length"><td class="empty" :colspan="visibleHeaders.length + 5">尚未載入資料。可選擇 File、載入 DEV 範例，或下載範本。</td></tr>
        </tbody>
      </table>
    </div>
    <footer class="footnote">DEV 假資料只在此頁記憶體中；Check／Save 不呼叫 MES 服務或寫入資料庫。{{ importedComponentData ? '目前資料含 ComponentNo，依元件欄位提交假流程。' : '目前資料為一般批號欄位格式。' }}</footer>
  </section>
  <section v-else class="office-pending-page" aria-label="匯入計量檢測數據"><p role="status">正式環境尚未開放此功能。</p><button type="button" class="button" @click="emit('close')">Close (X)</button></section>
</template>

<style scoped>
.spc-import-page { display: flex; flex-direction: column; min-height: 0; height: 100%; padding: 12px; box-sizing: border-box; color: #26251f; background: #f7f5ee; font: inherit; }
.page-heading { display: flex; justify-content: space-between; align-items: flex-start; gap: 14px; margin-bottom: 10px; }
.eyebrow { margin: 0 0 2px; color: #777366; font-size: 11px; letter-spacing: .08em; }
h1 { margin: 0; font-size: 19px; font-weight: 650; }
.subheading { margin: 3px 0 0; color: #777366; font-size: 12px; }
button { min-height: 28px; padding: 4px 10px; border: 1px solid #8b887c; border-radius: 3px; color: #26251f; background: #fffef9; font: inherit; cursor: pointer; }
button:hover:not(:disabled) { background: #efeddf; }
button:disabled { opacity: .5; cursor: default; }
button:focus-visible, input:focus-visible { outline: 2px solid #d0b500; outline-offset: 2px; }
.close-button, button.primary { border-color: #3b3930; color: white; background: #3b3930; }
.toolbar { display: flex; flex-wrap: wrap; gap: 6px; padding: 8px; border: 1px solid #cbc8bb; background: #eeece2; }
.template-mode { display: flex; flex-wrap: wrap; align-items: center; gap: 12px; padding: 9px 8px; border: 1px solid #cbc8bb; border-top: 0; font-size: 12px; }
.template-mode label { display: inline-flex; align-items: center; gap: 4px; }
.sample-button { margin-left: auto; }
.file-line { display: flex; flex-wrap: wrap; align-items: center; gap: 6px; padding: 9px 2px 5px; font-size: 12px; }
.file-line strong { font-weight: 600; }
.counter { margin-left: auto; color: #625f53; }
.notice, .error { margin: 2px 0 7px; padding: 7px 9px; border: 1px solid #c9c5b5; background: #fffef9; color: #514f45; font-size: 12px; }
.error { border-color: #c77e72; color: #8c2b1e; background: #fff3f0; }
.grid-wrap { flex: 1; min-height: 140px; overflow: auto; border: 1px solid #bcb9ac; background: white; }
table { width: 100%; border-collapse: separate; border-spacing: 0; font-size: 12px; white-space: nowrap; }
th, td { padding: 6px 8px; border-right: 1px solid #dedbd0; border-bottom: 1px solid #dedbd0; text-align: left; }
th { position: sticky; top: 0; z-index: 1; background: #e8e6dc; font-weight: 650; }
tbody tr:nth-child(even) { background: #faf9f4; }
tbody tr.saved { color: #666458; background: #eef3e9; }
tbody tr.failed { background: #fff2ef; }
.check-cell { position: sticky; left: 0; z-index: 2; min-width: 54px; text-align: center; background: #f0eee5; }
thead .check-cell { z-index: 3; }
.index-cell { min-width: 28px; text-align: right; }
.value-cell { min-width: 92px; max-width: 220px; overflow: hidden; text-overflow: ellipsis; }
.empty { padding: 26px 12px; text-align: center; color: #777366; }
.footnote { padding-top: 8px; color: #6d695d; font-size: 11px; }
.visually-hidden { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0, 0, 0, 0); white-space: nowrap; clip-path: inset(50%); }
.office-pending-page { padding: 12px; min-height: 0; box-sizing: border-box; background: #f7f5ee; color: #26251f; font: inherit; }
.office-pending-page p { margin: 0 0 10px; color: #777366; }
.office-pending-page .button { border-color: #3b3930; background: #3b3930; color: white; }
</style>
