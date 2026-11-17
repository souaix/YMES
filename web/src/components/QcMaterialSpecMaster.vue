<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type DetailField = 'us' | 'cs' | 'ls' | 'ucl' | 'cl' | 'lcl'
interface MaterialRow { materialType: string; materialNo: string; materialName: string; selected: boolean; issueState?: number }
interface QcFactor { factorNo: string; factorSpec: boolean }
interface QcItem { qcItemNo: string; qcItemName: string; chartType: number; specLineType: number; factors: QcFactor[] }
interface SpecRow {
  qcItemNo: string; qcItemName: string; chartType: number; specLineType: number; spcId: number; chartName: string
  us: string; cs: string; ls: string; ucl: string; cl: string; lcl: string; editable: boolean
}
interface ApiEnvelope<T> { items?: T[]; rows?: T[]; data?: T[] }
interface DemoMaterial extends MaterialRow { qcItems: QcItem[]; savedDetails: Record<string, Partial<SpecRow>[]> }
interface MaterialTypeOption { value?: string; code?: string; name?: string; label?: string; materialType?: string }
interface BatchResultItem { materialNo: string; qcItemNo?: string; status: 'updated' | 'skipped' | 'failed'; message?: string }
interface BatchResult { updatedCount?: number; skippedCount?: number; failedCount?: number; results?: BatchResultItem[] }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const devMock = computed(() => import.meta.env.DEV && props.developmentPreview)
const materialType = ref('')
const materialNo = ref('')
const materials = ref<MaterialRow[]>([])
const details = ref<SpecRow[]>([])
const qcItems = ref<QcItem[]>([])
const currentMaterialNo = ref('')
const selectedDetailKey = ref('')
const startPaste = ref<{ key: string; field: DetailField } | null>(null)
const selectedCharts = ref<Record<number, boolean>>({ 0: true, 1: false, 2: false, 3: false, 4: false, 5: false })
const cs = ref('')
const tolerance = ref('')
const memo = ref('')
const busy = ref(false)
const error = ref('')
const message = ref('')
const materialTypePicker = ref(false)
const materialPicker = ref(false)
const typeOptions = ref<string[]>([])
const pasteDialog = ref(false)
const pasteText = ref('')
const audit = ref<string[]>([])

const chartOptions = [
  { id: 0, label: 'Trend Chart' }, { id: 1, label: 'Xbar Chart' }, { id: 2, label: 'R Chart' },
  { id: 3, label: 'S Chart' }, { id: 4, label: 'Rm Chart' }, { id: 5, label: 'X Chart' },
]
const visibleDetails = computed(() => details.value.filter(row => selectedCharts.value[row.spcId]))
const selectedMaterials = computed(() => materials.value.filter(row => row.selected))
const activeDetail = computed(() => details.value.find(row => detailKey(row) === selectedDetailKey.value) ?? null)
const activeMaterial = computed(() => materials.value.find(row => row.materialNo === currentMaterialNo.value) ?? null)
const eligibleQcItems = computed(() => qcItems.value.filter(item => {
  const factors = item.factors.filter(factor => factor.factorSpec)
  return item.chartType !== -1 && factors.length === 1 && factors[0].factorNo.toUpperCase() === 'MATERIAL'
}))

function detailKey(row: Pick<SpecRow, 'qcItemNo' | 'spcId'>): string { return row.qcItemNo + '|' + row.spcId }
function blankDetails(item: QcItem): SpecRow[] {
  let charts: Array<[number, string, boolean]> = []
  if (item.chartType === 0) charts = [[0, 'Trend Chart', true], [1, 'Xbar Chart', true], [2, 'R Chart', true]]
  else if (item.chartType === 1) charts = [[0, 'Trend Chart', true], [1, 'Xbar Chart', true], [3, 'S Chart', true]]
  else if (item.chartType === 2) charts = [[1, 'Xbar Chart', false], [4, 'Rm Chart', true], [5, 'X Chart', true]]
  return charts.map(([spcId, chartName, editable]) => ({ qcItemNo: item.qcItemNo, qcItemName: item.qcItemName,
    chartType: item.chartType, specLineType: item.specLineType, spcId, chartName, us: '', cs: '', ls: '', ucl: '', cl: '', lcl: '', editable }))
}
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
function rows<T>(payload: T[] | ApiEnvelope<T>): T[] {
  if (Array.isArray(payload)) return payload
  return payload.items ?? payload.rows ?? payload.data ?? []
}
function demoData(): DemoMaterial[] {
  const q1: QcItem = { qcItemNo: 'QCV-MTL-01', qcItemName: '清洗液濃度', chartType: 0, specLineType: 0, factors: [{ factorNo: 'MATERIAL', factorSpec: true }] }
  const q2: QcItem = { qcItemNo: 'QCV-MTL-02', qcItemName: '原料粒徑', chartType: 1, specLineType: 1, factors: [{ factorNo: 'MATERIAL', factorSpec: true }] }
  const q3: QcItem = { qcItemNo: 'QCV-MTL-03', qcItemName: '薄膜厚度', chartType: 2, specLineType: 0, factors: [{ factorNo: 'MATERIAL', factorSpec: true }] }
  return [
    { materialType: 'CHEMICAL', materialNo: 'CHEM-001', materialName: '鹼性清洗液 A', selected: false, issueState: 2,
      qcItems: [q1, q3], savedDetails: {
        [q1.qcItemNo]: [{ spcId: 0, us: '12', cs: '10', ls: '8', ucl: '11.5', cl: '10', lcl: '8.5' }, { spcId: 1, us: '11.5', cs: '10', ls: '8.5', ucl: '11', cl: '10', lcl: '9' }, { spcId: 2, us: '3', cs: '2', ls: '1', ucl: '2.8', cl: '2', lcl: '1.2' }],
        [q3.qcItemNo]: [{ spcId: 0, us: '5.5', cs: '5', ls: '4.5', ucl: '5.4', cl: '5', lcl: '4.6' }, { spcId: 4, us: '1.5', cs: '1', ls: '0.5', ucl: '1.4', cl: '1', lcl: '0.6' }],
      } },
    { materialType: 'CHEMICAL', materialNo: 'CHEM-002', materialName: '酸性清洗液 B', selected: false, issueState: 2,
      qcItems: [q1, q2], savedDetails: { [q1.qcItemNo]: [], [q2.qcItemNo]: [] } },
    { materialType: 'RAW', materialNo: 'RAW-010', materialName: '矽粉原料', selected: false, issueState: 2,
      qcItems: [q2], savedDetails: { [q2.qcItemNo]: [{ spcId: 0, us: '18', cs: '15', ls: '12', ucl: '17', cl: '15', lcl: '13' }] } },
  ]
}
const demoMaterials = ref<DemoMaterial[]>([])
function addAudit(action: string, note: string): void {
  audit.value.unshift(new Date().toLocaleString('zh-TW', { hour12: false }) + ' ' + (props.currentUserNo || 'DEV001') + ' ' + action + ' ' + note)
}
async function loadTypeOptions(): Promise<void> {
  if (devMock.value) { typeOptions.value = [...new Set(demoMaterials.value.map(row => row.materialType))]; return }
  const result = await request<{ materialTypes?: MaterialTypeOption[] }>('/api/materials/lookups/options')
  typeOptions.value = (result.materialTypes ?? []).map(option => option.value ?? option.code ?? option.materialType ?? option.name ?? option.label ?? '').filter(Boolean)
}
async function queryMaterials(): Promise<void> {
  if (!materialType.value.trim() && !materialNo.value.trim()) { error.value = 'MaterialType 與 MaterialNo 至少需輸入一項。'; return }
  busy.value = true; error.value = ''; message.value = ''; details.value = []; qcItems.value = []; currentMaterialNo.value = ''; selectedDetailKey.value = ''
  try {
    if (devMock.value) {
      materials.value = demoMaterials.value.filter(row => row.issueState === 2 &&
        (!materialType.value.trim() || row.materialType.toUpperCase() === materialType.value.trim().toUpperCase()) &&
        (!materialNo.value.trim() || row.materialNo.toUpperCase().startsWith(materialNo.value.trim().toUpperCase())))
        .map(({ materialType, materialNo, materialName, selected, issueState }) => ({ materialType, materialNo, materialName, selected, issueState }))
    } else {
      const query = new URLSearchParams()
      if (materialType.value.trim()) query.set('materialType', materialType.value.trim())
      if (materialNo.value.trim()) query.set('materialNoPrefix', materialNo.value.trim())
      materials.value = rows(await request<MaterialRow[] | ApiEnvelope<MaterialRow>>('/api/qc/material-spec/materials?' + query))
        .map(row => ({ ...row, selected: false }))
    }
    if (!materials.value.length) message.value = '沒有符合條件且可設定品管規格的 ACTIVE 物料。'
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '查詢物料資料失敗。' }
  finally { busy.value = false }
}
async function loadMaterialSpec(row: MaterialRow): Promise<void> {
  busy.value = true; error.value = ''; message.value = ''; currentMaterialNo.value = row.materialNo
  row.selected = true; details.value = []; qcItems.value = []; selectedDetailKey.value = ''
  try {
    let itemList: QcItem[]
    let persisted: Array<Partial<SpecRow> & { qcItemNo: string; spcId: number }>
    if (devMock.value) {
      const source = demoMaterials.value.find(item => item.materialNo === row.materialNo)!
      itemList = source.qcItems; persisted = Object.entries(source.savedDetails).flatMap(([qcItemNo, values]) => values.map(value => ({ ...value, qcItemNo, spcId: value.spcId ?? 0 })))
    } else {
      itemList = rows(await request<QcItem[] | ApiEnvelope<QcItem>>('/api/qc/material-spec/' + encodeURIComponent(row.materialNo) + '/qc-items'))
      persisted = rows(await request<Array<Partial<SpecRow> & { qcItemNo: string; spcId: number }> | ApiEnvelope<Partial<SpecRow> & { qcItemNo: string; spcId: number }>>('/api/qc/material-spec/' + encodeURIComponent(row.materialNo) + '/chart-details'))
    }
    qcItems.value = itemList
    const detailRows = eligibleQcItems.value.flatMap(item => blankDetails(item))
    for (const saved of persisted) {
      const item = eligibleQcItems.value.find(qcItem => qcItem.qcItemNo === saved.qcItemNo)
      if (!item) continue
      const displaySpcId = item.chartType === 2 && Number(saved.spcId) === 0 ? 5 : Number(saved.spcId)
      const target = detailRows.find(detail => detail.qcItemNo === saved.qcItemNo && detail.spcId === displaySpcId)
      if (target) for (const field of ['us', 'cs', 'ls', 'ucl', 'cl', 'lcl'] as DetailField[]) target[field] = saved[field] == null ? '' : String(saved[field])
    }
    details.value = detailRows
    selectedDetailKey.value = detailRows[0] ? detailKey(detailRows[0]) : ''
    if (!eligibleQcItems.value.length) message.value = '此物料沒有「單一 FactorSpec=MATERIAL」的計量 QC Item。'
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '載入物料規格失敗。' }
  finally { busy.value = false }
}
function selectAll(value: boolean): void { materials.value.forEach(row => { row.selected = value }) }
async function openTypePicker(): Promise<void> {
  try { await loadTypeOptions(); materialTypePicker.value = true }
  catch (reason) { error.value = reason instanceof Error ? reason.message : '讀取物料類別失敗。' }
}
async function openMaterialPicker(): Promise<void> {
  if (!materialType.value.trim() && !materialNo.value.trim()) { error.value = '請先輸入物料類別或物料編號前綴。'; return }
  await queryMaterials()
  if (materials.value.length) materialPicker.value = true
}
function updateFromCenter(row: SpecRow): void {
  if (!cs.value.trim() || !tolerance.value.trim() || !Number.isFinite(Number(cs.value)) || !Number.isFinite(Number(tolerance.value))) {
    error.value = 'CS 與 Tolerance 都必須是數值。'; return
  }
  if (Number(cs.value) < 0 || Number(tolerance.value) < 0) { error.value = 'CS 與 Tolerance 必須大於或等於 0。'; return }
  const center = Number(cs.value); const spread = Number(tolerance.value)
  const precision = Math.min(12, Math.max(cs.value.split('.')[1]?.length ?? 0, tolerance.value.split('.')[1]?.length ?? 0))
  row.us = String(Number((center + spread).toFixed(precision))); row.ls = String(Number((center - spread).toFixed(precision))); row.cs = String(center)
  cs.value = ''; tolerance.value = ''; error.value = ''; selectedDetailKey.value = detailKey(row)
}
function numericText(value: string): boolean { return !value.trim() || Number.isFinite(Number(value)) }
function validate(): string {
  if (!details.value.length || !activeDetail.value) return '請先選取一筆規格明細。'
  if (!selectedMaterials.value.length) return '請至少勾選一筆物料。'
  if (!memo.value.trim()) return 'MEMO 為修改原因，必須填寫。'
  for (const row of details.value) {
    if (row.spcId === 0) {
      if ((row.specLineType === 0 || row.specLineType === 1) && !row.us.trim()) return row.qcItemNo + ' ' + row.chartName + ' 的 US 必填。'
      if ((row.specLineType === 0 || row.specLineType === 2) && !row.ls.trim()) return row.qcItemNo + ' ' + row.chartName + ' 的 LS 必填。'
    }
    for (const field of ['us', 'ls', 'cs', 'ucl', 'cl', 'lcl'] as DetailField[]) {
      if ((field === 'us' || field === 'ls') && row.spcId !== 0 && row.spcId !== 5) continue
      if (!numericText(row[field])) return row.qcItemNo + ' ' + row.chartName + ' 的 ' + field.toUpperCase() + ' 必須是數值。'
    }
    const limits = [row.us, row.ucl, row.cl, row.lcl, row.ls].filter(value => value.trim()).map(Number)
    for (let index = 1; index < limits.length; index++) if (limits[index - 1] < limits[index]) return row.qcItemNo + ' ' + row.chartName + ' 界限順序需符合 US ≥ UCL ≥ CL ≥ LCL ≥ LS。'
  }
  return ''
}
async function confirmChanges(): Promise<void> {
  error.value = ''; message.value = ''
  const issue = validate(); if (issue) { error.value = issue; return }
  if (!window.confirm('確定將目前的 QC 規格套用至 ' + selectedMaterials.value.length + ' 筆已勾選物料？')) return
  busy.value = true
  try {
    const command = { materialNos: selectedMaterials.value.map(row => row.materialNo), details: details.value.map(row => ({ qcItemNo: row.qcItemNo, spcId: row.spcId === 5 ? 0 : row.spcId, us: row.us, cs: row.cs, ls: row.ls, ucl: row.ucl, cl: row.cl, lcl: row.lcl })), memo: memo.value }
    let outcome: BatchResult
    if (devMock.value) {
      const results: BatchResultItem[] = []
      for (const material of selectedMaterials.value) {
        const target = demoMaterials.value.find(row => row.materialNo === material.materialNo)
        if (!target) continue
        for (const item of eligibleQcItems.value) {
          const targetItem = target.qcItems.find(row => row.qcItemNo === item.qcItemNo)
          if (!targetItem) { results.push({ materialNo: material.materialNo, qcItemNo: item.qcItemNo, status: 'skipped', message: '物料未直接綁定此 QC Item。' }); continue }
          if (targetItem.factors.filter(factor => factor.factorSpec).length !== 1 || targetItem.factors.filter(factor => factor.factorSpec)[0]?.factorNo.toUpperCase() !== 'MATERIAL') {
            results.push({ materialNo: material.materialNo, qcItemNo: item.qcItemNo, status: 'skipped', message: 'FactorSpec 不符合 MATERIAL 規則。' }); continue
          }
          target.savedDetails[item.qcItemNo] = details.value.filter(detail => detail.qcItemNo === item.qcItemNo).map(detail => ({ ...detail, spcId: detail.spcId === 5 ? 0 : detail.spcId }))
          results.push({ materialNo: material.materialNo, qcItemNo: item.qcItemNo, status: 'updated' })
        }
      }
      addAudit('套用物料規格', command.materialNos.join(','))
      outcome = { results }
    } else outcome = await request<BatchResult>('/api/qc/material-spec/batch-update', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(command) })
    const resultRows = outcome.results ?? []
    const updated = outcome.updatedCount ?? resultRows.filter(row => row.status === 'updated').length
    const skipped = outcome.skippedCount ?? resultRows.filter(row => row.status === 'skipped').length
    const failed = outcome.failedCount ?? resultRows.filter(row => row.status === 'failed').length
    const resultMessage = `已更新 ${updated} 個物料/QC Item 規格；略過 ${skipped}、失敗 ${failed}。`
    const resultError = failed ? (resultRows.filter(row => row.status === 'failed').map(row => `${row.materialNo}${row.qcItemNo ? '/' + row.qcItemNo : ''}：${row.message || '更新失敗'}`).join('；') || `${failed} 個物料/QC Item 規格更新失敗。`) : ''
    memo.value = ''; details.value = []; qcItems.value = []; currentMaterialNo.value = ''; selectedDetailKey.value = ''
    await queryMaterials()
    message.value = resultMessage; error.value = resultError
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '儲存物料規格失敗。' }
  finally { busy.value = false }
}
async function openPaste(): Promise<void> {
  if (!visibleDetails.value.length) { error.value = '請先查詢物料並載入規格明細。'; return }
  pasteText.value = ''; pasteDialog.value = true
  try { if (navigator.clipboard?.readText) pasteText.value = await navigator.clipboard.readText() }
  catch { /* Clipboard read can be denied; the dialog still accepts Ctrl+V. */ }
}
function applyPaste(): void {
  error.value = ''
  if (!startPaste.value) { error.value = '請先點選貼上起始儲存格。'; return }
  const lines = pasteText.value.replace(/\r/g, '').split('\n').filter(line => line.length > 0)
  const grid = visibleDetails.value
  const startRow = grid.findIndex(row => detailKey(row) === startPaste.value?.key)
  const fields: DetailField[] = ['us', 'cs', 'ls', 'ucl', 'cl', 'lcl']
  const startColumn = fields.indexOf(startPaste.value.field)
  if (!lines.length || startRow < 0 || startColumn < 0) { error.value = '貼上資料或起始位置無效。'; return }
  for (let rowIndex = 0; rowIndex < lines.length && startRow + rowIndex < grid.length; rowIndex++) {
    const cells = lines[rowIndex].includes('\t') ? lines[rowIndex].split('\t') : lines[rowIndex].split(',')
    const target = grid[startRow + rowIndex]
    if (!target.editable) continue
    for (let column = 0; column < cells.length && startColumn + column < fields.length; column++) target[fields[startColumn + column]] = cells[column].replace(/^"|"$/g, '').trim()
  }
  pasteDialog.value = false; message.value = '已貼入可編輯的圖表規格欄位。'
}
onMounted(() => {
  demoMaterials.value = demoData()
  void loadTypeOptions().catch(reason => { error.value = reason instanceof Error ? reason.message : '讀取物料類別失敗。' })
})
</script>

<template>
  <main class="qc-material-spec-page">
    <header class="qc-ms-heading"><div><h1>物料規格設定</h1><span>MTLQCITEM　｜　QC304</span></div><button class="qc-ms-button" @click="emit('close')">關閉</button></header>
    <div v-if="devMock" class="qc-ms-note">DEV 假資料模式：套用結果僅保存在本頁記憶體。</div>
    <div v-if="error" class="qc-ms-message error" role="alert">{{ error }}</div><div v-if="message" class="qc-ms-message">{{ message }}</div>

    <section class="qc-ms-panel qc-ms-query"><div class="qc-ms-panel-title">Query</div><div class="qc-ms-query-row">
      <label>Material Type<input v-model="materialType" @keydown.enter="queryMaterials"></label><button class="qc-ms-button lookup" aria-label="查找物料類別" @click="openTypePicker">...</button>
      <label>Material No<input v-model="materialNo" placeholder="編號前綴" @keydown.enter="queryMaterials"></label><button class="qc-ms-button lookup" aria-label="查找物料" @click="openMaterialPicker">...</button>
      <button class="qc-ms-button primary query-action" :disabled="busy" @click="queryMaterials">Query</button>
    </div></section>

    <section class="qc-ms-panel qc-ms-material-panel"><div class="qc-ms-panel-title">MaterialData</div>
      <div class="qc-ms-toolbar"><button class="qc-ms-button" :disabled="!materials.length" @click="selectAll(true)">SelectAll</button><button class="qc-ms-button" :disabled="!materials.length" @click="selectAll(false)">UnSelectAll</button><span>查詢結果 {{ materials.length }}　已選 {{ selectedMaterials.length }}</span></div>
      <div class="qc-ms-scroll"><table class="qc-ms-table"><thead><tr><th>Selected</th><th>MaterialType</th><th>MaterialNo</th><th>MaterialName</th><th>SPEC</th></tr></thead><tbody>
        <tr v-for="row in materials" :key="row.materialNo" :class="{ selected: row.materialNo === currentMaterialNo }">
          <td class="check-cell"><input v-model="row.selected" type="checkbox" :aria-label="'選取 ' + row.materialNo"></td><td>{{ row.materialType }}</td><td>{{ row.materialNo }}</td><td>{{ row.materialName }}</td><td><button class="qc-ms-button spec-button" @click="loadMaterialSpec(row)">Spec</button></td>
        </tr>
        <tr v-if="!materials.length"><td colspan="5" class="qc-ms-empty">{{ busy ? '載入中…' : '請輸入條件後查詢物料' }}</td></tr>
      </tbody></table></div>
    </section>

    <section class="qc-ms-panel qc-ms-detail-panel"><div class="qc-ms-panel-title">QCItem Spec <span v-if="currentMaterialNo">目前 SPEC：{{ currentMaterialNo }}</span></div>
      <div class="qc-ms-detail-toolbar"><label>CS<input v-model="cs" inputmode="decimal" @keydown.enter="activeDetail && updateFromCenter(activeDetail)"></label><label>Tolerance<input v-model="tolerance" inputmode="decimal" @keydown.enter="activeDetail && updateFromCenter(activeDetail)"></label><button class="qc-ms-button" :disabled="!activeDetail" @click="activeDetail && updateFromCenter(activeDetail)">Edit</button>
        <label v-for="chart in chartOptions" :key="chart.id" class="chart-filter"><input v-model="selectedCharts[chart.id]" type="checkbox">{{ chart.label }}</label><button class="qc-ms-button paste-button" :disabled="!details.length" @click="openPaste">Paste</button>
      </div>
      <div class="qc-ms-scroll qc-ms-detail-scroll"><table class="qc-ms-table qc-ms-detail-table"><thead><tr><th>QCItemNo</th><th>QCItemName</th><th>SPCID</th><th>US</th><th>CS</th><th>LS</th><th>UCL</th><th>CL</th><th>LCL</th></tr></thead><tbody>
        <tr v-for="row in visibleDetails" :key="detailKey(row)" :class="{ selected: detailKey(row) === selectedDetailKey, disabled: !row.editable }" @click="selectedDetailKey = detailKey(row)">
          <td>{{ row.qcItemNo }}</td><td>{{ row.qcItemName }}</td><td>{{ row.chartName }}</td>
          <td v-for="field in (['us','cs','ls','ucl','cl','lcl'] as DetailField[])" :key="field" :class="{ 'required-cell': row.spcId === 0 && ((row.specLineType === 0 && (field === 'us' || field === 'ls')) || (row.specLineType === 1 && field === 'us') || (row.specLineType === 2 && field === 'ls')) }">
            <input v-model="row[field]" :disabled="!row.editable" inputmode="decimal" :aria-label="row.qcItemNo + ' ' + row.chartName + ' ' + field.toUpperCase()" @focus="selectedDetailKey = detailKey(row); startPaste = { key: detailKey(row), field }">
          </td>
        </tr>
        <tr v-if="!visibleDetails.length"><td colspan="9" class="qc-ms-empty">{{ details.length ? '目前篩選條件沒有圖表資料' : '請在物料清單按 Spec 載入資料' }}</td></tr>
      </tbody></table></div>
    </section>

    <footer class="qc-ms-footer"><label>MEMO<input v-model="memo" placeholder="請填寫規格修改原因"></label><div><button class="qc-ms-button primary" :disabled="busy || !details.length" @click="confirmChanges">Confirm</button><button class="qc-ms-button" @click="emit('close')">Close</button></div></footer>
    <section v-if="devMock && audit.length" class="qc-ms-audit"><b>DEV 操作記錄</b><span v-for="entry in audit.slice(0,4)" :key="entry">{{ entry }}</span></section>

    <div v-if="materialTypePicker" class="qc-ms-backdrop" @click.self="materialTypePicker = false"><section class="qc-ms-modal" role="dialog" aria-modal="true" aria-label="選取物料類別"><header><b>選取 Material Type</b><button @click="materialTypePicker = false">×</button></header><div class="qc-ms-modal-list"><button v-for="option in typeOptions" :key="option" class="qc-ms-button" @click="materialType = option; materialTypePicker = false">{{ option }}</button><p v-if="!typeOptions.length">沒有可選物料類別</p></div></section></div>
    <div v-if="materialPicker" class="qc-ms-backdrop" @click.self="materialPicker = false"><section class="qc-ms-modal" role="dialog" aria-modal="true" aria-label="選取物料"><header><b>選取 Material No</b><button @click="materialPicker = false">×</button></header><div class="qc-ms-modal-list"><button v-for="row in materials" :key="row.materialNo" class="qc-ms-button" @click="materialNo = row.materialNo; materialPicker = false">{{ row.materialNo }}　{{ row.materialName }}</button></div></section></div>
    <div v-if="pasteDialog" class="qc-ms-backdrop" @click.self="pasteDialog = false"><section class="qc-ms-modal qc-ms-paste-modal" role="dialog" aria-modal="true" aria-label="貼上規格"><header><div><b>Paste QCItem Spec</b><small>點選表格儲存格後，在下方貼上 Excel 的 tab 分隔資料。</small></div><button @click="pasteDialog = false">×</button></header><textarea v-model="pasteText" rows="9" autofocus placeholder="Ctrl+V"></textarea><footer><button class="qc-ms-button primary" @click="applyPaste">套用</button><button class="qc-ms-button" @click="pasteDialog = false">Close</button></footer></section></div>
  </main>
</template>

<style scoped>
.qc-material-spec-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:6px;padding:9px 12px 11px;background:#f8f7f3;color:#242424;font:12px Arial,"Microsoft JhengHei",sans-serif;overflow:auto}
.qc-ms-heading{display:flex;align-items:center;justify-content:space-between;padding:0 2px 6px;border-bottom:1px solid #d6d2ca}.qc-ms-heading h1{font-size:20px;font-weight:600;margin:0}.qc-ms-heading span{display:block;color:#777;font-size:10px;margin-top:2px}
.qc-ms-note,.qc-ms-message{padding:5px 9px;background:#fff3cd;border:1px solid #e8d59c;color:#66551d}.qc-ms-message{background:#eff6e8;border-color:#c7d6b6;color:#315322}.qc-ms-message.error{background:#fff0ef;border-color:#e1b8b4;color:#8a2720}
.qc-ms-panel{border:1px solid #bcb9b2;background:#fff;min-width:0}.qc-ms-panel-title{height:24px;padding:4px 8px;background:#f2f0eb;border-bottom:1px solid #cbc8c1;font-weight:600}.qc-ms-panel-title span{float:right;font-weight:400;color:#70695d}.qc-ms-query{flex:0 0 72px}.qc-ms-query-row{display:flex;align-items:center;gap:7px;padding:6px 8px}.qc-ms-query-row label,.qc-ms-detail-toolbar>label:not(.chart-filter),.qc-ms-footer label{display:flex;align-items:center;gap:6px}.qc-ms-query-row label{min-width:245px}.qc-ms-query-row input,.qc-ms-detail-toolbar>label:not(.chart-filter) input,.qc-ms-footer input{height:27px;border:1px solid #999;background:#fff0aa;padding:4px 7px;color:#242424}.qc-ms-query-row label input{flex:1;min-width:80px}
.qc-ms-material-panel{flex:1;min-height:150px;display:flex;flex-direction:column}.qc-ms-toolbar{display:flex;align-items:center;gap:6px;padding:5px 6px}.qc-ms-toolbar span{margin-left:auto;color:#777;font-size:11px}.qc-ms-scroll{overflow:auto;min-height:0;flex:1}.qc-ms-table{border-collapse:collapse;white-space:nowrap;width:100%;font-size:11px}.qc-ms-table th{height:24px;padding:3px 7px;text-align:left;background:#f0ede6;color:#35322c;border:1px solid #d0cbc1;font-weight:600}.qc-ms-table td{height:24px;padding:2px 7px;border:1px solid #d5d1c9;background:#fff}.qc-ms-table tbody tr{cursor:pointer}.qc-ms-table tbody tr:hover td{background:#fff8e5}.qc-ms-table tbody tr.selected td{background:#ffe69a}.qc-ms-table .check-cell{text-align:center;width:72px}.qc-ms-table .check-cell input,.chart-filter input{accent-color:#ff9f24}.qc-ms-empty{text-align:center!important;color:#888!important;padding:11px!important;cursor:default!important}.qc-ms-button{min-height:27px;padding:4px 9px;border:1px solid #c7c4be;border-radius:3px;background:linear-gradient(#fff,#f5f4f1);color:#302f2c;cursor:pointer;white-space:nowrap;font-size:11px}.qc-ms-button:hover:not(:disabled){border-color:#ff9f24;background:#fff7e7}.qc-ms-button.primary{background:#ff9f24;border-color:#ed941d;color:#fff;font-weight:600}.qc-ms-button:disabled{opacity:.48;cursor:default}.qc-ms-button.lookup{min-width:27px;padding:2px 5px}.qc-ms-button.spec-button{min-height:21px;padding:1px 9px}.qc-ms-query-row .query-action{margin-left:auto;min-width:74px}
.qc-ms-detail-panel{flex:1;min-height:170px;display:flex;flex-direction:column}.qc-ms-detail-toolbar{display:flex;align-items:center;gap:6px;flex-wrap:wrap;padding:5px 6px;border-bottom:1px solid #e4e1db}.qc-ms-detail-toolbar>label:not(.chart-filter){gap:4px;font-size:11px}.qc-ms-detail-toolbar>label:not(.chart-filter) input{width:72px}.chart-filter{display:inline-flex;align-items:center;gap:3px;font-size:10px;cursor:pointer}.paste-button{margin-left:auto}.qc-ms-detail-scroll{flex:1}.qc-ms-detail-table th,.qc-ms-detail-table td{text-align:right}.qc-ms-detail-table th:nth-child(-n+3),.qc-ms-detail-table td:nth-child(-n+3){text-align:left}.qc-ms-detail-table td:nth-child(1){min-width:96px}.qc-ms-detail-table td:nth-child(2){min-width:150px}.qc-ms-detail-table td:nth-child(n+4){min-width:82px}.qc-ms-detail-table td input{width:100%;min-width:66px;height:21px;padding:2px 5px;border:1px solid #b8b2a8;background:#fff0aa;text-align:right;color:#242424}.qc-ms-detail-table td input:disabled{background:#e9e8e5;color:#777}.qc-ms-detail-table td.required-cell input{background:#ffe69a}.qc-ms-detail-table tr.disabled td:first-child,.qc-ms-detail-table tr.disabled td:nth-child(2),.qc-ms-detail-table tr.disabled td:nth-child(3){color:#777}
.qc-ms-footer{flex:0 0 40px;display:flex;align-items:center;justify-content:space-between;padding:3px 4px}.qc-ms-footer label{min-width:50%;justify-content:flex-start}.qc-ms-footer input{flex:1;max-width:460px}.qc-ms-footer>div{display:flex;gap:6px}.qc-ms-audit{display:flex;gap:10px;flex-wrap:wrap;padding:4px 7px;border:1px solid #dedbd4;background:#fff;color:#777;font-size:10px}
.qc-ms-backdrop{position:fixed;inset:0;z-index:1500;background:#14141466;display:flex;align-items:center;justify-content:center;padding:18px}.qc-ms-modal{width:min(520px,94vw);max-height:85vh;display:flex;flex-direction:column;gap:9px;background:#f8f7f3;border:1px solid #a9a69e;box-shadow:0 8px 30px #0004;padding:12px}.qc-ms-modal>header{display:flex;align-items:center;justify-content:space-between;padding-bottom:7px;border-bottom:1px solid #c8c5bf}.qc-ms-modal>header button{border:0;background:transparent;font-size:22px;cursor:pointer}.qc-ms-modal-list{display:flex;flex-direction:column;align-items:stretch;gap:5px;overflow:auto}.qc-ms-modal-list p{color:#777;text-align:center}.qc-ms-paste-modal{width:min(720px,95vw)}.qc-ms-paste-modal small{display:block;color:#777;margin-top:4px}.qc-ms-paste-modal textarea{width:100%;resize:vertical;border:1px solid #999;padding:7px;font:12px Consolas,monospace}.qc-ms-paste-modal footer{display:flex;justify-content:flex-end;gap:6px}
@media(max-width:850px){.qc-ms-query-row{flex-wrap:wrap}.qc-ms-query-row label{flex:1;min-width:220px}.qc-ms-query-row .query-action{margin-left:0}.qc-ms-detail-toolbar{align-items:flex-start}.qc-ms-footer label{min-width:0;flex:1}.qc-ms-footer input{max-width:none}.qc-ms-footer{gap:8px}}
</style>
