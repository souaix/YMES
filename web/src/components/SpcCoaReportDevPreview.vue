<script setup lang="ts">
import { computed, ref } from 'vue'

type ItemType = 'variable' | 'attribute'
type QCItem = { qcItemNo: string; qcItemName: string; type: ItemType; groupNo: string; factors: string[] }
type Reason = { reasonNo: string; reasonName: string; qty: number }
type VariableStats = { max: number; min: number; mean: number; sd: number; usl: number; lsl: number; samples: string[] }
type ReportRow = {
  lotNo: string; qcItemNo: string; qcItemName: string; type: ItemType; spcSerial: string; createDate: Date
  result: number; erfNo: string; mo: string; productNo: string; opNo: string; factors: Record<string, string>
  variable?: VariableStats; sampleQty?: number; goodQty?: number; scrapQty?: number; defectQty?: number; rankDetail?: string; reasons: Reason[]
}
type PickerMode = ItemType | 'factor' | 'template' | ''

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const today = new Date()
const fromValue = ref(toDateTimeInput(new Date(today.getFullYear(), today.getMonth(), today.getDate() - 1, 0, 0)))
const toValue = ref(toDateTimeInput(new Date(today.getFullYear(), today.getMonth(), today.getDate() - 1, 23, 59)))
const useFrom = ref(false)
const useTo = ref(false)
const useVariable = ref(true)
const useAttribute = ref(true)
const selectedVariableNos = ref<string[]>([])
const selectedAttributeNos = ref<string[]>([])
const lotNoEnabled = ref(false)
const lotNoText = ref('')
const moEnabled = ref(false)
const moText = ref('')
const productEnabled = ref(false)
const productText = ref('')
const opEnabled = ref(false)
const opText = ref('')
const pickerMode = ref<PickerMode>('')
const pickerSearch = ref('')
const variableRows = ref<ReportRow[]>([])
const attributeRows = ref<ReportRow[]>([])
const queried = ref(false)
const selectedSerial = ref('')
const notice = ref('選擇計量或計數 QC Item 後執行 Query；日期預設不套用。')
const feedback = ref('')
const showFactorPicker = ref(false)
const visibleFactors = ref<string[]>([])
const factorDraft = ref<string[]>([])
const reportTemplate = ref('')
const outputName = ref('COA_Report.xlsx')
const exportModalOpen = ref(false)
const templatePreviewOpen = ref(false)
const factorLabels = new Map<string, string>([
  ['LINE', '產線'], ['MACHINE', '機台'], ['SHIFT', '班別'], ['PRODUCT', '產品類別'], ['MATERIAL', '材料批號'], ['CAVITY', '模穴'],
])
const items: QCItem[] = [
  { qcItemNo: 'V-COA-001', qcItemName: 'Lead Frame Width', type: 'variable', groupNo: 'VG-COA-01', factors: ['LINE', 'MACHINE', 'SHIFT'] },
  { qcItemNo: 'V-COA-002', qcItemName: 'Plating Thickness', type: 'variable', groupNo: 'VG-COA-01', factors: ['LINE', 'MATERIAL', 'SHIFT'] },
  { qcItemNo: 'V-COA-003', qcItemName: 'Wire Bond Pull', type: 'variable', groupNo: 'VG-COA-02', factors: ['LINE', 'MACHINE', 'PRODUCT'] },
  { qcItemNo: 'A-COA-101', qcItemName: 'Surface Defect', type: 'attribute', groupNo: '', factors: ['LINE', 'MACHINE', 'SHIFT'] },
  { qcItemNo: 'A-COA-102', qcItemName: 'Contamination Check', type: 'attribute', groupNo: '', factors: ['LINE', 'MATERIAL', 'CAVITY'] },
]
const lotOptions = ['LOT-26K-101', 'LOT-26K-102', 'LOT-26K-103', 'LOT-26K-104', 'LOT-26K-105', 'LOT-26K-106', 'LOT-26K-107', 'LOT-26K-108']
const moOptions = ['MO-260101', 'MO-260102', 'MO-260103', 'MO-260104']
const productOptions = ['PRD-A100', 'PRD-A200', 'PRD-B100']
const opOptions = ['OP-001', 'OP-015', 'OP-021']
const templates = [
  { xlsNo: 'COA1', xlsName: 'COA 客戶標準報告', fileName: 'COAExport_STD01.xls' },
  { xlsNo: 'COA2', xlsName: 'COA 內部分析報告', fileName: 'COAExport_STD02.xls' },
]

function toDateTimeInput(value: Date): string {
  return `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, '0')}-${String(value.getDate()).padStart(2, '0')}T${String(value.getHours()).padStart(2, '0')}:${String(value.getMinutes()).padStart(2, '0')}`
}
function splitValues(value: string): string[] {
  return [...new Set(value.split(',').map((entry) => entry.trim().toUpperCase()).filter(Boolean))]
}
function itemByNo(qcItemNo: string): QCItem | undefined { return items.find((item) => item.qcItemNo === qcItemNo) }
function seedRows(): ReportRow[] {
  const rows: ReportRow[] = []
  items.forEach((item, itemIndex) => {
    lotOptions.forEach((lotNo, lotIndex) => {
      for (let revision = 0; revision < 2; revision += 1) {
        const ageDays = 100 - lotIndex * 7 - revision * 3
        const createDate = new Date(today.getFullYear(), today.getMonth(), today.getDate() - ageDays, 8 + (lotIndex + itemIndex) % 9, (lotIndex * 7) % 60)
        const factors: Record<string, string> = {
          LINE: ['LN-A', 'LN-B', 'LN-C'][lotIndex % 3],
          MACHINE: ['MC-03', 'MC-05', 'MC-08'][(lotIndex + itemIndex) % 3],
          SHIFT: ['A', 'B', 'C'][lotIndex % 3],
          PRODUCT: productOptions[(lotIndex + itemIndex) % productOptions.length],
          MATERIAL: `MAT-${String(410 + lotIndex % 4)}`,
          CAVITY: `C${1 + lotIndex % 8}`,
        }
        const serial = `${item.type === 'variable' ? 'SPC-VAR' : 'SPC-ATT'}-${String(itemIndex + 1).padStart(2, '0')}-${String(lotIndex + 1).padStart(3, '0')}-${revision}`
        const variable = item.type === 'variable'
          ? { max: 10.13 + lotIndex / 100 + revision / 20, min: 9.78 + lotIndex / 150, mean: 9.96 + lotIndex / 180 + revision / 100, sd: 0.035 + (lotIndex % 4) / 1000, usl: 10.2, lsl: 9.7, samples: Array.from({ length: 5 }, (_, index) => `${index + 1}: ${(9.84 + ((lotIndex + index * 2 + revision) % 11) / 100).toFixed(3)}`) }
          : undefined
        const scrapQty = item.type === 'attribute' ? (lotIndex + itemIndex + revision) % 4 : undefined
        const defectQty = item.type === 'attribute' ? (lotIndex * 2 + itemIndex + revision) % 6 : undefined
        const sampleQty = item.type === 'attribute' ? 72 + (lotIndex * 17) % 55 : undefined
        rows.push({
          lotNo, qcItemNo: item.qcItemNo, qcItemName: item.qcItemName, type: item.type, spcSerial: serial, createDate,
          result: (lotIndex + itemIndex + revision) % 9 === 0 ? 1 : 0,
          erfNo: (lotIndex + itemIndex) % 13 === 5 && revision === 1 ? `ERF-26-${String(2200 + lotIndex)}` : '',
          mo: moOptions[(lotIndex + itemIndex) % moOptions.length], productNo: factors.PRODUCT,
          opNo: opOptions[(lotIndex + itemIndex) % opOptions.length], factors, variable,
          sampleQty, scrapQty, goodQty: sampleQty === undefined || scrapQty === undefined ? undefined : Math.max(0, sampleQty - scrapQty),
          defectQty, rankDetail: item.type === 'attribute' ? `R${1 + lotIndex % 4} / R${1 + itemIndex % 3} / R${1 + revision}` : undefined,
          reasons: item.type === 'attribute' && (scrapQty || defectQty)
            ? [{ reasonNo: lotIndex % 2 ? 'E-110' : 'E-204', reasonName: lotIndex % 2 ? '表面刮傷' : '鍍層異物', qty: (scrapQty || 0) + (defectQty || 0) }]
            : [],
        })
      }
    })
  })
  return rows
}
const allRows = seedRows()
const selectedSerialRow = computed(() => [...variableRows.value, ...attributeRows.value].find((row) => row.spcSerial === selectedSerial.value) || null)
const selectedItems = computed(() => items.filter((item) => (item.type === 'variable' && useVariable.value && selectedVariableNos.value.includes(item.qcItemNo)) || (item.type === 'attribute' && useAttribute.value && selectedAttributeNos.value.includes(item.qcItemNo))))
const factorChoices = computed(() => [...new Set(selectedItems.value.flatMap((item) => item.factors))].map((factorNo) => ({ factorNo, factorName: factorLabels.get(factorNo) || factorNo })))
const visibleVariableRows = computed(() => variableRows.value)
const visibleAttributeRows = computed(() => attributeRows.value)
const filteredPickerItems = computed(() => {
  if (pickerMode.value !== 'variable' && pickerMode.value !== 'attribute') return []
  const phrase = pickerSearch.value.trim().toLowerCase()
  return items.filter((item) => item.type === pickerMode.value && `${item.qcItemNo} ${item.qcItemName} ${item.groupNo}`.toLowerCase().includes(phrase))
})
const visibleFactorText = computed(() => visibleFactors.value.length ? visibleFactors.value.map((factor) => factorLabels.get(factor) || factor).join('、') : '尚未顯示任何 Factor 欄位')
const queryRowCount = computed(() => variableRows.value.length + attributeRows.value.length)
const hasRequestedOp = computed(() => opEnabled.value && splitValues(opText.value).length > 0)
const showOpNote = computed(() => queried.value && hasRequestedOp.value)
const tokenExamples = computed(() => {
  const rows = [...variableRows.value, ...attributeRows.value]
  const first = rows[0]
  if (!first) return []
  const rowIndex = rows.findIndex((row) => row.spcSerial === first.spcSerial) + 1
  const prefix = first.type === 'variable' ? 'VAR' : 'ATT'
  return [
    `[(${prefix},${first.qcItemNo}_${rowIndex},QCITEMNO,0)]`,
    `[(${prefix},${first.qcItemNo}_${rowIndex},LOTNO,1)]`,
    `[(${prefix},${first.qcItemNo}_${rowIndex},CREATEDATE,1)]`,
    ...(first.type === 'variable' ? [`[(VAR,${first.qcItemNo}_${rowIndex},MEANVALUE,1)]`, `[(VAR,${first.qcItemNo}_${rowIndex},SDVALUE,1)]`] : [`[(ATT,${first.qcItemNo}_${rowIndex},SCRAPQTY,1)]`, `[(ATT,${first.qcItemNo}_${rowIndex},DEFECTQTY,1)]`]),
  ]
})

function openPicker(mode: ItemType): void { pickerMode.value = mode; pickerSearch.value = ''; }
function closePicker(): void { pickerMode.value = ''; pickerSearch.value = ''; }
function toggleItem(item: QCItem): void {
  const target = item.type === 'variable' ? selectedVariableNos : selectedAttributeNos
  target.value = target.value.includes(item.qcItemNo) ? target.value.filter((value) => value !== item.qcItemNo) : [...target.value, item.qcItemNo]
}
function addVariableGroup(groupNo: string): void {
  const groupItems = items.filter((item) => item.type === 'variable' && item.groupNo === groupNo).map((item) => item.qcItemNo)
  selectedVariableNos.value = [...new Set([...selectedVariableNos.value, ...groupItems])]
}
function latestRows(itemNos: string[]): ReportRow[] {
  const latest = new Map<string, ReportRow>()
  for (const row of allRows) {
    if (!itemNos.includes(row.qcItemNo)) continue
    const key = `${row.qcItemNo}|${row.lotNo}`
    const current = latest.get(key)
    if (!current || row.createDate > current.createDate) latest.set(key, row)
  }
  return [...latest.values()]
}
function filterRows(rows: ReportRow[]): ReportRow[] {
  const lots = splitValues(lotNoText.value)
  const mos = splitValues(moText.value)
  const products = splitValues(productText.value)
  const from = useFrom.value ? new Date(fromValue.value) : null
  const to = useTo.value ? new Date(toValue.value) : null
  return rows.filter((row) => (!from || row.createDate >= from) && (!to || row.createDate <= to))
    .filter((row) => !lotNoEnabled.value || !lots.length || lots.includes(row.lotNo.toUpperCase()))
    .filter((row) => !moEnabled.value || !mos.length || mos.includes(row.mo.toUpperCase()))
    .filter((row) => !productEnabled.value || !products.length || products.includes(row.productNo.toUpperCase()))
    .sort((left, right) => left.spcSerial.localeCompare(right.spcSerial))
}
function runQuery(): void {
  const variableNos = useVariable.value ? selectedVariableNos.value : []
  const attributeNos = useAttribute.value ? selectedAttributeNos.value : []
  if (!variableNos.length && !attributeNos.length) { notice.value = '請至少選取一個計量或計數 QC Item，並啟用相應查詢類型。'; return }
  if (productEnabled.value && (!useFrom.value || !useTo.value)) { notice.value = '勾選 ProductNo 時，來源要求同時啟用起始與結束日期。'; return }
  if (useFrom.value && useTo.value && new Date(fromValue.value) >= new Date(toValue.value)) { notice.value = '起始日期必須早於結束日期。'; return }
  const nextVariable = filterRows(latestRows(variableNos))
  const nextAttribute = filterRows(latestRows(attributeNos))
  variableRows.value = nextVariable
  attributeRows.value = nextAttribute
  queried.value = true
  visibleFactors.value = []
  selectedSerial.value = nextVariable[0]?.spcSerial || nextAttribute[0]?.spcSerial || ''
  feedback.value = ''
  notice.value = `查詢完成：計量 ${nextVariable.length} 列、計數 ${nextAttribute.length} 列。COA 模式每組 Lot／QC Item 只保留最新紀錄。`
  if (hasRequestedOp.value) notice.value += ' 已輸入的 OPNo 會顯示在條件摘要，但舊 Service/Core 未套用此條件。'
}
function openFactorPicker(): void { factorDraft.value = [...visibleFactors.value]; showFactorPicker.value = true; }
function applyFactorSelection(): void { visibleFactors.value = [...factorDraft.value]; showFactorPicker.value = false; }
function toggleFactor(factorNo: string): void {
  factorDraft.value = factorDraft.value.includes(factorNo) ? factorDraft.value.filter((item) => item !== factorNo) : [...factorDraft.value, factorNo]
}
function openTemplatePreview(): void {
  templatePreviewOpen.value = true
  feedback.value = ''
}
function openExport(): void {
  if (!reportTemplate.value) reportTemplate.value = templates[0].xlsNo
  exportModalOpen.value = true
  feedback.value = ''
}
function simulateExport(): void {
  const selected = templates.find((template) => template.xlsNo === reportTemplate.value)
  if (!selected) { feedback.value = '請選擇 COA 樣板。'; return }
  feedback.value = `已模擬套用 ${selected.xlsName} 至 ${outputName.value || 'COA_Report.xlsx'}；DEV 沒有讀取樣板 bytes、呼叫 AddXLSFile 或建立檔案。`
  exportModalOpen.value = false
}
function formatDate(value: Date): string { return value.toLocaleString('zh-TW', { hour12: false }) }
function formatReasons(row: ReportRow): string { return row.reasons.map((reason) => `${reason.reasonName}(${reason.qty})`).join(' ') || 'N/A' }
function selectedItemNames(itemNos: string[]): string {
  const names = itemNos.map((itemNo) => itemByNo(itemNo)?.qcItemName || itemNo)
  return names.length ? names.join('、') : '尚未選擇 QC Item'
}
</script>

<template>
  <main class="coa-page" :data-development-preview="props.developmentPreview">
    <header class="page-head">
      <div><p class="eyebrow">SPC / SC213 · CERTIFICATE OF ANALYSIS</p><h1>出貨報告</h1><p class="subtitle">以最新 Lot／QC Item 量測資料套入 COA Excel 樣板；所有資料與輸出操作均為 DEV 假資料。</p></div>
      <button class="button" type="button" @click="emit('close')">Close (X)</button>
    </header>

    <section class="panel criteria">
      <div class="filter-grid">
        <label class="filter"><span><input v-model="moEnabled" type="checkbox"> MONo</span><input v-model="moText" list="coa-mo-options" type="text" :disabled="!moEnabled" placeholder="逗號分隔多筆 MO"><small>可從計量 FactorLog 的 MO 選取</small></label>
        <label class="filter"><span><input v-model="productEnabled" type="checkbox"> ProductNo</span><input v-model="productText" list="coa-product-options" type="text" :disabled="!productEnabled" placeholder="逗號分隔多筆 Product"><small>啟用時必須指定起訖日期</small></label>
        <label class="filter"><span><input v-model="lotNoEnabled" type="checkbox"> LotNo</span><input v-model="lotNoText" list="coa-lot-options" type="text" :disabled="!lotNoEnabled" placeholder="逗號分隔多筆 Lot"><small>可輸入或由假資料清單挑選</small></label>
        <label class="filter"><span><input v-model="opEnabled" type="checkbox"> OPNo</span><input v-model="opText" list="coa-op-options" type="text" :disabled="!opEnabled" placeholder="逗號分隔多筆 OP"><small>Legacy service/core 未套用 OPNo 條件</small></label>
        <label class="filter"><span><input v-model="useFrom" type="checkbox"> 起始日期</span><input v-model="fromValue" type="datetime-local" :disabled="!useFrom"></label>
        <label class="filter"><span><input v-model="useTo" type="checkbox"> 結束日期</span><input v-model="toValue" type="datetime-local" :disabled="!useTo"></label>
        <button class="button primary query-button" type="button" @click="runQuery">Query</button>
      </div>
      <datalist id="coa-mo-options"><option v-for="value in moOptions" :key="value" :value="value"></option></datalist>
      <datalist id="coa-product-options"><option v-for="value in productOptions" :key="value" :value="value"></option></datalist>
      <datalist id="coa-lot-options"><option v-for="value in lotOptions" :key="value" :value="value"></option></datalist>
      <datalist id="coa-op-options"><option v-for="value in opOptions" :key="value" :value="value"></option></datalist>
      <div class="item-grid">
        <section class="item-panel">
          <header><label><input v-model="useVariable" type="checkbox"> Query QCItem For Variables</label><span>計量 QC Item</span></header>
          <div class="item-actions"><button class="button" type="button" :disabled="!useVariable" @click="openPicker('variable')">選計量 QC Item</button><button class="button" type="button" :disabled="!useVariable" @click="addVariableGroup('VG-COA-01')">加入群組 VG-COA-01</button><button class="button" type="button" :disabled="!useVariable" @click="addVariableGroup('VG-COA-02')">加入群組 VG-COA-02</button></div>
          <p>{{ selectedItemNames(selectedVariableNos) }}</p>
        </section>
        <section class="item-panel">
          <header><label><input v-model="useAttribute" type="checkbox"> Query QCItem For Attributes</label><span>計數 QC Item</span></header>
          <div class="item-actions"><button class="button" type="button" :disabled="!useAttribute" @click="openPicker('attribute')">選計數 QC Item</button></div>
          <p>{{ selectedItemNames(selectedAttributeNos) }}</p>
        </section>
      </div>
      <div class="criteria-foot"><span>日期預設為昨天 00:00–23:59，但未啟用；查詢至少需要一個 QC Item。</span><span>Variable 與 Attribute 條件分開查詢，結果合併供 COA 匯出。</span></div>
    </section>

    <section class="panel notice-panel"><p role="status">{{ notice }}</p><p v-if="feedback" class="feedback" role="status">{{ feedback }}</p></section>
    <p v-if="showOpNote" class="warning">輸入的 OPNo：{{ opText }}。舊 `wsSPC.LoadLotSPCMeasureData` 不解析 OPNo，Core `LoadLotMeasureData` 也沒有 OPNo 參數，故此條件不會縮小結果。</p>

    <section v-if="queried" class="panel results">
      <header class="result-head"><div><p class="eyebrow">LATEST SPC LOGS · ISCOA=TRUE</p><h2>Query Result</h2><p>依 SPCSerial 顯示；在同一 Lot 與 QC Item 多筆紀錄中保留 CreateDate 最新的一筆。</p></div><div class="result-actions"><button class="button" type="button" @click="openFactorPicker" :disabled="!factorChoices.length">顯示要因</button><button class="button" type="button" @click="openTemplatePreview" :disabled="!queryRowCount">Export Template</button><button class="button primary" type="button" @click="openExport" :disabled="!queryRowCount">Export COA</button></div></header>
      <div class="summary-row"><span><b>{{ queryRowCount }}</b> rows</span><span>計量 {{ variableRows.length }}</span><span>計數 {{ attributeRows.length }}</span><span>顯示要因：{{ visibleFactorText }}</span><span v-if="hasRequestedOp" class="warning-inline">OPNo 未套用</span></div>

      <section class="data-section"><header><h3>Variable QC Item</h3><span>{{ visibleVariableRows.length }} rows</span></header><div class="table-wrap"><table><thead><tr><th>LotNo</th><th>QCItemNo</th><th>QCItemName</th><th>CreateDate</th><th>Result</th><th>ERFNo</th><th>MO</th><th>ProductNo</th><th>Max</th><th>Min</th><th>Mean</th><th>SD</th><th>Samples</th><th v-for="factor in visibleFactors" :key="`v-${factor}`">{{ factorLabels.get(factor) || factor }}</th></tr></thead><tbody>
        <tr v-for="row in visibleVariableRows" :key="row.spcSerial" :class="{ active: selectedSerial === row.spcSerial }" @click="selectedSerial = row.spcSerial"><td class="code">{{ row.lotNo }}</td><td class="code">{{ row.qcItemNo }}</td><td>{{ row.qcItemName }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.result === 0 ? 'Accept' : 'Reject' }}</td><td>{{ row.erfNo || '—' }}</td><td>{{ row.mo }}</td><td>{{ row.productNo }}</td><td class="number">{{ row.variable?.max.toFixed(4) }}</td><td class="number">{{ row.variable?.min.toFixed(4) }}</td><td class="number">{{ row.variable?.mean.toFixed(4) }}</td><td class="number">{{ row.variable?.sd.toFixed(4) }}</td><td>{{ row.variable?.samples.join(' · ') }}</td><td v-for="factor in visibleFactors" :key="`${row.spcSerial}-${factor}`">{{ row.factors[factor] || '—' }}</td></tr>
        <tr v-if="!visibleVariableRows.length"><td :colspan="13 + visibleFactors.length" class="empty">沒有計量量測資料。</td></tr>
      </tbody></table></div></section>

      <section class="data-section"><header><h3>Attribute QC Item</h3><span>{{ visibleAttributeRows.length }} rows</span></header><div class="table-wrap"><table><thead><tr><th>LotNo</th><th>QCItemNo</th><th>QCItemName</th><th>CreateDate</th><th>Result</th><th>ERFNo</th><th>MO</th><th>ProductNo</th><th>SampleQty</th><th>GoodQty</th><th>ScrapQty</th><th>DefectQty</th><th>RankDetail</th><th>ErrorDetail</th><th v-for="factor in visibleFactors" :key="`a-${factor}`">{{ factorLabels.get(factor) || factor }}</th></tr></thead><tbody>
        <tr v-for="row in visibleAttributeRows" :key="row.spcSerial" :class="{ active: selectedSerial === row.spcSerial }" @click="selectedSerial = row.spcSerial"><td class="code">{{ row.lotNo }}</td><td class="code">{{ row.qcItemNo }}</td><td>{{ row.qcItemName }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.result === 0 ? 'Accept' : 'Reject' }}</td><td>{{ row.erfNo || '—' }}</td><td>{{ row.mo }}</td><td>{{ row.productNo }}</td><td class="number">{{ row.sampleQty }}</td><td class="number">{{ row.goodQty }}</td><td class="number">{{ row.scrapQty }}</td><td class="number">{{ row.defectQty }}</td><td>{{ row.rankDetail }}</td><td>{{ formatReasons(row) }}</td><td v-for="factor in visibleFactors" :key="`${row.spcSerial}-${factor}`">{{ row.factors[factor] || '—' }}</td></tr>
        <tr v-if="!visibleAttributeRows.length"><td :colspan="14 + visibleFactors.length" class="empty">沒有計數量測資料。</td></tr>
      </tbody></table></div></section>

      <section v-if="selectedSerialRow" class="detail-card"><header><h3>Selected SPC Row</h3><span class="code">{{ selectedSerialRow.spcSerial }} · {{ selectedSerialRow.qcItemNo }}</span></header><div v-if="selectedSerialRow.type === 'attribute'" class="reason-list"><span v-for="reason in selectedSerialRow.reasons" :key="reason.reasonNo">{{ reason.reasonNo }} / {{ reason.reasonName }} / {{ reason.qty }}</span><span v-if="!selectedSerialRow.reasons.length">無 Error detail；DEV 不連線至資料庫。</span></div><div v-else class="reason-list"><span v-for="sample in selectedSerialRow.variable?.samples || []" :key="sample">{{ sample }}</span><span>USL {{ selectedSerialRow.variable?.usl }} · LSL {{ selectedSerialRow.variable?.lsl }}</span></div></section>
      <p class="disclaimer">結果採 SPC COA 模式，並使用每批最新統計資料；TBLSPCVARSTATISTICLOG、變數 SPCID=0 limit content、Attribute reason／component detail 僅以假資料表示。Export Template／Export COA 不呼叫樣板 API、Excel 或檔案系統。</p>
    </section>

    <div v-if="pickerMode === 'variable' || pickerMode === 'attribute'" class="modal-backdrop" @click.self="closePicker"><section class="dialog" role="dialog" aria-modal="true" aria-label="QC Item 選擇器"><header><div><p class="eyebrow">QC ITEM LOOKUP · ISSUESTATE=ALL</p><h2>選取{{ pickerMode === 'variable' ? '計量' : '計數' }} QC Item</h2></div><button class="button" type="button" @click="closePicker">Close</button></header><input v-model="pickerSearch" class="search-input" autofocus placeholder="搜尋編號、名稱或群組"><div class="picker-list"><label v-for="item in filteredPickerItems" :key="item.qcItemNo" class="picker-row"><input type="checkbox" :checked="(item.type === 'variable' ? selectedVariableNos : selectedAttributeNos).includes(item.qcItemNo)" @change="toggleItem(item)"><b class="code">{{ item.qcItemNo }}</b><span>{{ item.qcItemName }}</span><small>{{ item.groupNo || 'Attribute' }} · {{ item.factors.length }} factors</small></label><p v-if="!filteredPickerItems.length" class="empty">沒有符合的 QC Item。</p></div><footer><button class="button primary" type="button" @click="closePicker">完成</button></footer></section></div>

    <div v-if="showFactorPicker" class="modal-backdrop" @click.self="showFactorPicker = false"><section class="dialog compact-dialog" role="dialog" aria-modal="true" aria-label="顯示 Factor 欄位"><header><div><p class="eyebrow">COLUMN DISPLAY · NOT A QUERY FILTER</p><h2>顯示要因欄位</h2></div><button class="button" type="button" @click="showFactorPicker = false">Close</button></header><div class="choice-list"><label v-for="factor in factorChoices" :key="factor.factorNo"><input type="checkbox" :checked="factorDraft.includes(factor.factorNo)" @change="toggleFactor(factor.factorNo)"><span>{{ factor.factorName }}</span><code>{{ factor.factorNo }}</code></label><p v-if="!factorChoices.length" class="empty">目前結果沒有要因欄位。</p></div><footer><button class="button" type="button" @click="factorDraft = []">清除</button><button class="button primary" type="button" @click="applyFactorSelection">套用</button></footer></section></div>

    <div v-if="templatePreviewOpen" class="modal-backdrop" @click.self="templatePreviewOpen = false"><section class="dialog wide-dialog" role="dialog" aria-modal="true" aria-label="COA Template 欄位樣板"><header><div><p class="eyebrow">GENERATED TEMPLATE TOKENS · LOCAL PREVIEW</p><h2>Export Template</h2></div><button class="button" type="button" @click="templatePreviewOpen = false">Close</button></header><p class="modal-copy">來源依目前查詢列產生 VAR／ATT 工作表與欄位 token；DEV 只預覽 token，不寫入 .xls 檔。</p><div class="token-grid"><code v-for="token in tokenExamples" :key="token">{{ token }}</code></div><footer><button class="button primary" type="button" @click="templatePreviewOpen = false; feedback = '已完成 COA Template token 預覽；DEV 沒有建立活頁簿或檔案。'">完成</button></footer></section></div>

    <div v-if="exportModalOpen" class="modal-backdrop" @click.self="exportModalOpen = false"><section class="dialog compact-dialog" role="dialog" aria-modal="true" aria-label="選擇 COA 樣板與輸出檔名"><header><div><p class="eyebrow">XLS TYPE 3 · TBLSPCXLSFILE</p><h2>Export COA</h2></div><button class="button" type="button" @click="exportModalOpen = false">Close</button></header><label class="form-field">COA 樣板<select v-model="reportTemplate"><option v-for="template in templates" :key="template.xlsNo" :value="template.xlsNo">{{ template.xlsNo }} · {{ template.xlsName }}</option></select></label><label class="form-field">輸出檔名<input v-model="outputName" type="text" maxlength="120"></label><p class="modal-copy">正式來源會取樣板 bytes 並填入 VAR／ATT token；本頁只模擬選擇與完成狀態。</p><footer><button class="button" type="button" @click="exportModalOpen = false">取消</button><button class="button primary" type="button" @click="simulateExport">模擬輸出</button></footer></section></div>
  </main>
</template>

<style scoped>
.coa-page { --ink:#27332b; --muted:#66736a; --line:#d5dfd7; --green:#287552; height:100%; min-height:0; overflow:auto; padding:18px; box-sizing:border-box; background:#f2f5f2; color:var(--ink); font:inherit; }
.page-head,.result-head,.item-panel>header,.data-section>header,.detail-card>header { display:flex; align-items:center; justify-content:space-between; gap:12px; }
.page-head { margin-bottom:14px; align-items:flex-start; }.page-head h1,.result-head h2 { margin:2px 0; font-size:22px; }.page-head p,.result-head p { margin:4px 0 0; color:var(--muted); }.eyebrow { margin:0; color:var(--green); font-size:10px; font-weight:800; letter-spacing:.1em; }.subtitle { font-size:13px; }
.panel { margin-bottom:11px; border:1px solid var(--line); border-radius:6px; background:#fff; box-shadow:0 1px 3px #28342b0b; }.criteria,.results { padding:13px; }
.filter-grid { display:grid; grid-template-columns:repeat(3,minmax(190px,1fr)) auto; align-items:end; gap:9px; }.filter { display:grid; gap:4px; color:var(--muted); font-size:11px; }.filter>span { display:flex; align-items:center; gap:5px; color:var(--ink); font-weight:700; }.filter input:not([type=checkbox]),.search-input,.form-field input,.form-field select { min-height:31px; width:100%; padding:5px 8px; box-sizing:border-box; border:1px solid #c8d0ca; border-radius:4px; background:#fff; color:var(--ink); font:inherit; }.filter input:disabled { background:#f0f2f0; }.filter small { color:#8a948d; font-size:10px; }.query-button { min-width:74px; min-height:35px; }
.item-grid { display:grid; grid-template-columns:1fr 1fr; gap:10px; margin-top:13px; }.item-panel { padding:10px; border:1px solid #e1e7e2; border-radius:5px; background:#fbfcfb; }.item-panel>header { font-size:12px; }.item-panel>header label { display:flex; align-items:center; gap:6px; font-weight:750; }.item-panel>header span { color:var(--muted); font-size:10px; }.item-panel p { min-height:20px; margin:7px 0 0; color:#46564a; font-size:11px; }.item-actions { display:flex; flex-wrap:wrap; gap:5px; margin-top:8px; }.criteria-foot { display:flex; justify-content:space-between; flex-wrap:wrap; gap:5px 15px; margin-top:9px; color:var(--muted); font-size:10px; }
.notice-panel { padding:9px 13px; color:#536158; font-size:12px; }.notice-panel p { margin:0; }.notice-panel .feedback { margin-top:4px; color:var(--green); }.warning { padding:8px 12px; border:1px solid #e4ca9d; border-radius:5px; background:#fff8e8; color:#805b16; font-size:11px; }.result-head { align-items:flex-start; margin-bottom:12px; }.result-head h2 { font-size:18px; }.result-actions { display:flex; flex-wrap:wrap; justify-content:flex-end; gap:6px; }.summary-row { display:flex; flex-wrap:wrap; align-items:center; gap:8px 16px; padding:8px 10px; border:1px solid #e4ebe5; border-radius:4px; background:#f8faf8; color:var(--muted); font-size:10px; }.summary-row b { color:var(--ink); font-size:14px; }.warning-inline { color:#9a6714; font-weight:800; }
.data-section { margin-top:12px; border:1px solid var(--line); border-radius:5px; overflow:hidden; }.data-section>header,.detail-card>header { padding:8px 10px; border-bottom:1px solid var(--line); background:#f8faf8; }.data-section h3,.detail-card h3 { margin:0; font-size:12px; }.data-section>header span,.detail-card>header span { color:var(--muted); font-size:10px; }.table-wrap { max-width:100%; max-height:310px; overflow:auto; }table { width:100%; border-collapse:collapse; white-space:nowrap; font-size:10px; }th,td { padding:6px 8px; border-bottom:1px solid #e8ece8; text-align:left; }th { position:sticky; top:0; z-index:1; background:#f0f5f0; color:#5a685e; font-size:9px; }tbody tr { cursor:pointer; }tbody tr:hover,tbody tr.active { background:#e9f3eb; }.code { color:#246a4e; font-family:Consolas,monospace; }.number { text-align:right; font-variant-numeric:tabular-nums; }.empty { padding:12px; color:#89928b; text-align:center; }.detail-card { margin-top:12px; border:1px solid var(--line); border-radius:5px; background:#fff; }.reason-list { display:flex; flex-wrap:wrap; gap:8px; padding:9px 11px; color:#4a5a4d; font-size:11px; }.reason-list span { padding:4px 7px; border-radius:4px; background:#f0f5f0; }.disclaimer { margin:9px 0 0; color:var(--muted); font-size:10px; }
.button { min-height:29px; padding:4px 9px; border:1px solid #bcc8bf; border-radius:4px; background:#fff; color:#344238; font:inherit; font-size:10px; cursor:pointer; }.button:hover:not(:disabled) { border-color:var(--green); color:var(--green); }.button.primary { border-color:var(--green); background:var(--green); color:#fff; }.button:disabled { cursor:not-allowed; opacity:.5; }
.modal-backdrop { position:fixed; inset:0; z-index:120; display:grid; place-items:center; padding:18px; background:#1d2822a8; }.dialog { width:min(760px,96vw); max-height:85vh; overflow:hidden; border-radius:7px; background:white; box-shadow:0 15px 55px #0004; }.dialog header { display:flex; justify-content:space-between; align-items:center; padding:12px 14px; border-bottom:1px solid var(--line); }.dialog h2 { margin:2px 0 0; font-size:17px; }.dialog footer { display:flex; justify-content:flex-end; gap:6px; padding:10px 14px; border-top:1px solid #edf0ed; }.picker-list,.choice-list { max-height:55vh; overflow:auto; padding:6px 12px; }.picker-row { display:grid; grid-template-columns:22px 155px 1fr 130px; align-items:center; gap:7px; padding:9px 5px; border-bottom:1px solid #edf0ed; font-size:11px; }.picker-row small { color:var(--muted); }.search-input { margin:10px 14px 0; width:calc(100% - 28px); }.compact-dialog { width:min(520px,96vw); }.choice-list label { display:flex; align-items:center; gap:9px; padding:8px 5px; border-bottom:1px solid #edf0ed; font-size:11px; }.choice-list label code { margin-left:auto; color:#52735d; }.wide-dialog { width:min(800px,96vw); }.modal-copy { margin:10px 14px; color:var(--muted); font-size:11px; }.token-grid { display:grid; gap:5px; padding:0 14px 12px; }.token-grid code { overflow-wrap:anywhere; padding:6px 8px; border-radius:3px; background:#f0f5f0; color:#246a4e; font-size:10px; }.form-field { display:grid; gap:5px; margin:11px 14px; color:var(--muted); font-size:11px; font-weight:700; }
@media (max-width:1000px) { .filter-grid { grid-template-columns:repeat(2,minmax(180px,1fr)); }.item-grid { grid-template-columns:1fr; }.result-head { flex-direction:column; }.result-actions { justify-content:flex-start; } }
@media (max-width:620px) { .coa-page { padding:10px; }.filter-grid { grid-template-columns:1fr; }.page-head { flex-direction:column; }.picker-row { grid-template-columns:22px 1fr; }.picker-row small { grid-column:2; } }
</style>
