<script setup lang="ts">
import { computed, ref } from 'vue'

type MeasureKind = 'variable' | 'attribute'
type ResultCode = 0 | 1 | 2 | 3 | 4
interface FactorDefinition { factorNo: string; name: string; options: string[] }
interface QcItem { no: string; name: string; kind: MeasureKind; issueState: number; factors: FactorDefinition[] }
interface DefectEntry { componentNo: string; errorNo: string; reason: string; reasonType: 'Scrap' | 'Defect'; qty: number; memo: string }
interface MeasureRecord {
  id: string; kind: MeasureKind; spcSerial: string; factorSerial: string; lotNo: string; qcItemNo: string; qcItemName: string
  creator: string; createDate: string; result: ResultCode; erfNo: string; memo: string; factors: Record<string, string>
  testValues: number[]; mean: number; min: number; max: number; stdDev: number; cp: number; cpk: number
  sampleQty: number; goodQty: number; scrapQty: number; defectQty: number; errorDetails: DefectEntry[]
}
interface LotGroup { lotNo: string; records: MeasureRecord[]; variableRecords: MeasureRecord[]; attributeRecords: MeasureRecord[]; latestDate: string }
interface QuerySnapshot {
  mode: 'lot' | 'item'; lots: string[]; fromDate: string; endDate: string; fromEnabled: boolean; endEnabled: boolean
  includeVariable: boolean; includeAttribute: boolean; variableItemNos: string[]; attributeItemNos: string[]; factorFilters: Record<string, string>
}

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const qcItems: QcItem[] = [
  { no: 'VAR-DEV-101', name: '軸心直徑', kind: 'variable', issueState: 2, factors: [{ factorNo: 'Product', name: '產品', options: ['PRD-A100', 'PRD-B220'] }, { factorNo: 'Machine', name: '設備', options: ['MC-11', 'MC-12'] }, { factorNo: 'Shift', name: '班別', options: ['A', 'B', 'C'] }] },
  { no: 'VAR-DEV-102', name: '壓合高度', kind: 'variable', issueState: 2, factors: [{ factorNo: 'Product', name: '產品', options: ['PRD-A100', 'PRD-B220'] }, { factorNo: 'Machine', name: '設備', options: ['MC-21', 'MC-22'] }] },
  { no: 'ATT-DEV-201', name: '外觀抽驗', kind: 'attribute', issueState: 2, factors: [{ factorNo: 'Product', name: '產品', options: ['PRD-A100', 'PRD-B220'] }, { factorNo: 'Shift', name: '班別', options: ['A', 'B', 'C'] }, { factorNo: 'Inspector', name: '檢驗員', options: ['DEV-QC-01', 'DEV-QC-02'] }] },
  { no: 'ATT-DEV-202', name: '端子缺陷抽驗', kind: 'attribute', issueState: -1, factors: [{ factorNo: 'Vendor', name: '供應商', options: ['V-1001', 'V-1002'] }, { factorNo: 'Product', name: '產品', options: ['PRD-A100', 'PRD-B220'] }] },
]
const qcGroups = [{ no: 'GRP-DEV-01', name: '軸心關鍵尺寸群組', issueState: 2, members: ['VAR-DEV-101', 'VAR-DEV-102'] }]
const records = ref<MeasureRecord[]>([
  { id: 'lot-a-var-1', kind: 'variable', spcSerial: 'DEV-SPC-V-5101', factorSerial: 'DEV-FACTOR-51', lotNo: 'LOT-SPC-261123-01', qcItemNo: 'VAR-DEV-101', qcItemName: '軸心直徑', creator: 'DEV-QC-01', createDate: '2026-11-23 08:25:00', result: 0, erfNo: '', memo: '抽樣首件', factors: { Product: 'PRD-A100', Machine: 'MC-11', Shift: 'A' }, testValues: [10.12, 10.08, 10.15, 10.11, 10.09], mean: 10.11, min: 10.08, max: 10.15, stdDev: 0.0274, cp: 1.28, cpk: 1.28, sampleQty: 5, goodQty: 5, scrapQty: 0, defectQty: 0, errorDetails: [] },
  { id: 'lot-a-var-2', kind: 'variable', spcSerial: 'DEV-SPC-V-5102', factorSerial: 'DEV-FACTOR-52', lotNo: 'LOT-SPC-261123-01', qcItemNo: 'VAR-DEV-102', qcItemName: '壓合高度', creator: 'DEV-QC-02', createDate: '2026-11-23 08:32:00', result: 1, erfNo: '', memo: '', factors: { Product: 'PRD-A100', Machine: 'MC-21' }, testValues: [2.03, 2.01, 2.02, 2.04], mean: 2.025, min: 2.01, max: 2.04, stdDev: 0.0129, cp: 1.17, cpk: 1.11, sampleQty: 4, goodQty: 4, scrapQty: 0, defectQty: 0, errorDetails: [] },
  { id: 'lot-a-att-1', kind: 'attribute', spcSerial: 'DEV-SPC-A-8101', factorSerial: 'DEV-ATT-FACTOR-8', lotNo: 'LOT-SPC-261123-01', qcItemNo: 'ATT-DEV-201', qcItemName: '外觀抽驗', creator: 'DEV-QC-01', createDate: '2026-11-23 08:40:00', result: 3, erfNo: '', memo: '', factors: { Product: 'PRD-A100', Shift: 'A', Inspector: 'DEV-QC-01' }, testValues: [], mean: 0, min: 0, max: 0, stdDev: 0, cp: 0, cpk: 0, sampleQty: 80, goodQty: 74, scrapQty: 2, defectQty: 4, errorDetails: [{ componentNo: 'COMP-001', errorNo: 'D-001', reason: '刮傷', reasonType: 'Scrap', qty: 2, memo: '上蓋外觀' }, { componentNo: 'COMP-014', errorNo: 'D-015', reason: '尺寸偏差', reasonType: 'Defect', qty: 4, memo: '' }] },
  { id: 'lot-b-var-1', kind: 'variable', spcSerial: 'DEV-SPC-V-5201', factorSerial: 'DEV-FACTOR-61', lotNo: 'LOT-SPC-261122-08', qcItemNo: 'VAR-DEV-101', qcItemName: '軸心直徑', creator: 'DEV-QC-03', createDate: '2026-11-22 15:05:00', result: 4, erfNo: 'DEV-ERF-031', memo: '', factors: { Product: 'PRD-B220', Machine: 'MC-12', Shift: 'B' }, testValues: [10.09, 10.08, 10.11, 10.35, 10.10], mean: 10.146, min: 10.08, max: 10.35, stdDev: 0.118, cp: 0.72, cpk: 0.72, sampleQty: 5, goodQty: 4, scrapQty: 0, defectQty: 1, errorDetails: [] },
  { id: 'lot-b-att-1', kind: 'attribute', spcSerial: 'DEV-SPC-A-8201', factorSerial: 'DEV-ATT-FACTOR-9', lotNo: 'LOT-SPC-261122-08', qcItemNo: 'ATT-DEV-201', qcItemName: '外觀抽驗', creator: 'DEV-QC-02', createDate: '2026-11-22 15:20:00', result: 1, erfNo: '', memo: '', factors: { Product: 'PRD-B220', Shift: 'B', Inspector: 'DEV-QC-02' }, testValues: [], mean: 0, min: 0, max: 0, stdDev: 0, cp: 0, cpk: 0, sampleQty: 60, goodQty: 56, scrapQty: 1, defectQty: 3, errorDetails: [{ componentNo: 'COMP-103', errorNo: 'D-006', reason: '色差', reasonType: 'Defect', qty: 3, memo: '色澤不均' }] },
  { id: 'lot-c-var-1', kind: 'variable', spcSerial: 'DEV-SPC-V-5301', factorSerial: 'DEV-FACTOR-71', lotNo: 'LOT-SPC-261121-03', qcItemNo: 'VAR-DEV-102', qcItemName: '壓合高度', creator: 'DEV-QC-04', createDate: '2026-11-21 13:10:00', result: 0, erfNo: '', memo: '', factors: { Product: 'PRD-A100', Machine: 'MC-22' }, testValues: [2.01, 2.02, 2.02, 2.03], mean: 2.02, min: 2.01, max: 2.03, stdDev: 0.0082, cp: 1.83, cpk: 1.75, sampleQty: 4, goodQty: 4, scrapQty: 0, defectQty: 0, errorDetails: [] },
])

const queryMode = ref<'lot' | 'item'>('lot')
const lotInput = ref('')
const fromEnabled = ref(false)
const endEnabled = ref(false)
const fromDate = ref('2026-11-01')
const endDate = ref('2026-11-23')
const includeVariable = ref(true)
const includeAttribute = ref(true)
const selectedVariableItems = ref<string[]>([])
const selectedAttributeItems = ref<string[]>([])
const selectedGroupNo = ref('')
const factorFilters = ref<Record<string, string>>({})
const factorPickerOpen = ref(false)
const shownFactors = ref<string[]>([])
const searched = ref(false)
const appliedQuery = ref<QuerySnapshot | null>(null)
const expandedLot = ref('')
const selectedRecord = ref<MeasureRecord | null>(null)
const notice = ref('選擇 Lot 或 QC Item 條件後按 Query；此頁只使用 DEV 假資料。')
const noticeKind = ref<'info' | 'success' | 'error'>('info')

const knownLots = computed(() => [...new Set(records.value.map((row) => row.lotNo))].sort())
const selectedGroup = computed(() => qcGroups.find((group) => group.no === selectedGroupNo.value) || null)
const variableItemNos = computed(() => [...new Set([...selectedVariableItems.value, ...(selectedGroup.value?.members || [])])])
const variableItems = computed(() => qcItems.filter((item) => item.kind === 'variable' && (item.issueState === 2 || item.issueState === -1)))
const attributeItems = computed(() => qcItems.filter((item) => item.kind === 'attribute' && (item.issueState === 2 || item.issueState === -1)))
function collectFactorDefinitions(selectedNos: Set<string>): FactorDefinition[] {
  const result = new Map<string, FactorDefinition>()
  qcItems.filter((item) => selectedNos.has(item.no)).forEach((item) => item.factors.forEach((factor) => {
    const current = result.get(factor.factorNo)
    result.set(factor.factorNo, current ? { ...current, options: [...new Set([...current.options, ...factor.options])] } : { ...factor, options: [...factor.options] })
  }))
  return [...result.values()]
}
const selectedFactorDefinitions = computed(() => collectFactorDefinitions(new Set([...variableItemNos.value, ...selectedAttributeItems.value])))
const resultFactorDefinitions = computed(() => {
  const query = appliedQuery.value
  if (!query) return []
  const selectedNos = query.mode === 'item'
    ? new Set([...query.variableItemNos, ...query.attributeItemNos])
    : new Set(filteredRecords.value.map((row) => row.qcItemNo))
  return collectFactorDefinitions(selectedNos)
})
const filteredRecords = computed(() => {
  const query = appliedQuery.value
  if (!query) return []
  const itemNos = new Set([...query.variableItemNos, ...query.attributeItemNos])
  return records.value.filter((row) => {
    if ((row.kind === 'variable' && !query.includeVariable) || (row.kind === 'attribute' && !query.includeAttribute)) return false
    if (query.fromEnabled && row.createDate.slice(0, 10) < query.fromDate) return false
    if (query.endEnabled && row.createDate.slice(0, 10) > query.endDate) return false
    if (query.mode === 'lot' && query.lots.length && !query.lots.includes(row.lotNo)) return false
    if (query.mode === 'item' && !itemNos.has(row.qcItemNo)) return false
    if (query.mode === 'item' && row.kind === 'attribute' && query.attributeItemNos.length === 0) return false
    if (query.mode === 'item' && row.kind === 'variable' && query.variableItemNos.length === 0) return false
    return query.mode !== 'item' || Object.entries(query.factorFilters).every(([factorNo, value]) => {
      const rowValue = row.factors[factorNo]
      return !value || rowValue === undefined || value.split(',').map((item) => item.trim()).includes(rowValue)
    })
  }).sort((a, b) => b.createDate.localeCompare(a.createDate))
})
const groupedLots = computed<LotGroup[]>(() => {
  const map = new Map<string, MeasureRecord[]>()
  filteredRecords.value.forEach((row) => map.set(row.lotNo, [...(map.get(row.lotNo) || []), row]))
  return [...map.entries()].map(([lotNo, lotRecords]) => ({
    lotNo,
    records: lotRecords,
    variableRecords: lotRecords.filter((row) => row.kind === 'variable'),
    attributeRecords: lotRecords.filter((row) => row.kind === 'attribute'),
    latestDate: lotRecords.reduce((latest, row) => row.createDate > latest ? row.createDate : latest, ''),
  })).sort((a, b) => b.latestDate.localeCompare(a.latestDate))
})
const visibleFactorDefinitions = computed(() => resultFactorDefinitions.value.filter((factor) => shownFactors.value.includes(factor.factorNo)))

function parseLotList(value: string): string[] { return [...new Set(value.split(',').map((lot) => lot.trim()).filter(Boolean))] }
function setNotice(text: string, kind: 'info' | 'success' | 'error' = 'info'): void { notice.value = text; noticeKind.value = kind }
function appendLot(lotNo: string): void {
  const lots = parseLotList(lotInput.value)
  if (!lots.includes(lotNo)) lots.push(lotNo)
  lotInput.value = lots.join(', ')
}
function setMode(mode: 'lot' | 'item'): void { queryMode.value = mode; searched.value = false; appliedQuery.value = null; expandedLot.value = ''; setNotice('請調整條件後按 Query。') }
function toggleItem(kind: MeasureKind, no: string, checked: boolean): void {
  const target = kind === 'variable' ? selectedVariableItems : selectedAttributeItems
  target.value = checked ? [...new Set([...target.value, no])] : target.value.filter((itemNo) => itemNo !== no)
}
function onItemChange(kind: MeasureKind, no: string, event: Event): void {
  toggleItem(kind, no, (event.target as HTMLInputElement).checked)
}
function toggleFactor(factorNo: string): void {
  shownFactors.value = shownFactors.value.includes(factorNo) ? shownFactors.value.filter((item) => item !== factorNo) : [...shownFactors.value, factorNo]
}
function runQuery(): void {
  if (!includeVariable.value && !includeAttribute.value) return setNotice('至少勾選計量或計數資料。', 'error')
  if (fromEnabled.value && endEnabled.value && fromDate.value > endDate.value) return setNotice('起日不能晚於迄日。', 'error')
  if (queryMode.value === 'lot' && parseLotList(lotInput.value).length === 0 && !fromEnabled.value && !endEnabled.value) return setNotice('請輸入 LotNo 或設定日期範圍，避免無條件查詢。', 'error')
  if (queryMode.value === 'item' && includeVariable.value && variableItemNos.value.length === 0) return setNotice('計量查詢請選擇 QC Item 或群組。', 'error')
  if (queryMode.value === 'item' && includeAttribute.value && selectedAttributeItems.value.length === 0) return setNotice('計數查詢請選擇 QC Item。', 'error')
  appliedQuery.value = {
    mode: queryMode.value,
    lots: parseLotList(lotInput.value),
    fromDate: fromDate.value,
    endDate: endDate.value,
    fromEnabled: fromEnabled.value,
    endEnabled: endEnabled.value,
    includeVariable: includeVariable.value,
    includeAttribute: includeAttribute.value,
    variableItemNos: [...variableItemNos.value],
    attributeItemNos: [...selectedAttributeItems.value],
    factorFilters: { ...factorFilters.value },
  }
  searched.value = true
  expandedLot.value = ''
  selectedRecord.value = null
  setNotice(`查詢完成：${filteredRecords.value.length} 筆 DEV 假資料、${groupedLots.value.length} 個 Lot。`, 'success')
}
function toggleLot(lotNo: string): void { expandedLot.value = expandedLot.value === lotNo ? '' : lotNo }
function resultLabel(result: ResultCode): string { return ['Pass', 'Warning', 'Wait', 'Hold', 'Alert'][result] || 'Unknown' }
function resultClass(result: ResultCode): string { return `result-${result}` }
function factorValue(row: MeasureRecord, factorNo: string): string { return row.factors[factorNo] || '—' }
function openRecord(row: MeasureRecord): void { selectedRecord.value = row }
</script>

<template>
  <section v-if="props.developmentPreview" class="lot-data-page" aria-label="Lot 檢測資料">
    <header class="page-head"><div><p class="eyebrow">SPC / SC202</p><h1>Lot 檢測資料</h1><p class="subtitle">依 Lot 或 QC Item 查詢計量與計數批次；結果唯讀。</p></div><button class="button" @click="emit('close')">Close (X)</button></header>

    <section class="query-panel panel">
      <div class="mode-tabs"><button :class="{ active: queryMode === 'lot' }" @click="setMode('lot')">By LotNo</button><button :class="{ active: queryMode === 'item' }" @click="setMode('item')">By QC Item</button><span>DEV 假資料 · 不呼叫 Service 或資料庫</span></div>
      <div class="type-filters"><label><input v-model="includeVariable" type="checkbox"> 計量資料</label><label><input v-model="includeAttribute" type="checkbox"> 計數資料</label></div>
      <div v-if="queryMode === 'lot'" class="lot-query">
        <label class="wide">LotNo（可輸入多筆，以逗號分隔）<input v-model="lotInput" placeholder="LOT-SPC-261123-01, LOT-SPC-261122-08"></label>
        <details class="quick-lots"><summary>選擇 DEV Lot</summary><div><button v-for="lot in knownLots" :key="lot" type="button" class="chip" @click="appendLot(lot)">{{ lot }}</button></div></details>
      </div>
      <div v-else class="item-query">
        <section v-if="includeVariable" class="item-group"><h2>計量 QC Item／群組</h2><label>群組<select v-model="selectedGroupNo"><option value="">不選群組</option><option v-for="group in qcGroups.filter((item) => item.issueState === 2)" :key="group.no" :value="group.no">{{ group.no }} — {{ group.name }}</option></select></label><div class="item-checks"><label v-for="item in variableItems" :key="item.no"><input type="checkbox" :checked="selectedVariableItems.includes(item.no)" @change="onItemChange('variable', item.no, $event)"><span>{{ item.no }} — {{ item.name }}</span></label></div><p v-if="selectedGroup" class="hint">群組成員：{{ selectedGroup.members.join('、') }}</p></section>
        <section v-if="includeAttribute" class="item-group"><h2>計數 QC Item</h2><div class="item-checks"><label v-for="item in attributeItems" :key="item.no"><input type="checkbox" :checked="selectedAttributeItems.includes(item.no)" @change="onItemChange('attribute', item.no, $event)"><span>{{ item.no }} — {{ item.name }} <small>IssueState {{ item.issueState }}</small></span></label></div></section>
      </div>
      <div class="date-query"><label class="check-date"><input v-model="fromEnabled" type="checkbox"> From Date</label><input v-model="fromDate" type="date" :disabled="!fromEnabled"><label class="check-date"><input v-model="endEnabled" type="checkbox"> End Date</label><input v-model="endDate" type="date" :disabled="!endEnabled"></div>
      <div v-if="queryMode === 'item' && selectedFactorDefinitions.length" class="factor-query"><strong>Collect Factor</strong><label v-for="factor in selectedFactorDefinitions" :key="factor.factorNo">{{ factor.name }}<select v-model="factorFilters[factor.factorNo]"><option value="">全部</option><option v-for="value in factor.options" :key="value" :value="value">{{ value }}</option></select></label></div>
      <div class="query-actions"><span>Lot 查詢可同時帶入計量與計數；日期條件可獨立啟用。</span><button class="button primary" @click="runQuery">Query</button></div>
    </section>

    <section v-if="searched" class="results-panel panel">
      <header class="results-head"><div><h2>查詢結果</h2><span>{{ filteredRecords.length }} 筆記錄 · {{ groupedLots.length }} 個 Lot</span></div><button class="button" @click="factorPickerOpen = !factorPickerOpen">Factor Select</button></header>
      <div v-if="factorPickerOpen" class="factor-picker"><strong>顯示要因欄位</strong><label v-for="factor in resultFactorDefinitions" :key="factor.factorNo"><input type="checkbox" :checked="shownFactors.includes(factor.factorNo)" @change="toggleFactor(factor.factorNo)">{{ factor.name }}（{{ factor.factorNo }}）</label><span v-if="!resultFactorDefinitions.length">沒有可顯示的 Collect Factor</span></div>
      <div class="table-scroll summary-scroll"><table class="summary-table"><thead><tr><th></th><th>LotNo</th><th>最新 CreateDate</th><th>計量批次</th><th>計數批次</th><th>Result 摘要</th></tr></thead><tbody>
        <template v-for="lot in groupedLots" :key="lot.lotNo"><tr class="summary-row" :class="{ expanded: expandedLot === lot.lotNo }" @click="toggleLot(lot.lotNo)"><td>{{ expandedLot === lot.lotNo ? '−' : '+' }}</td><td>{{ lot.lotNo }}</td><td>{{ lot.latestDate }}</td><td>{{ lot.variableRecords.length }}</td><td>{{ lot.attributeRecords.length }}</td><td><span v-for="result in [...new Set(lot.records.map((row) => row.result))]" :key="result" class="result-badge" :class="resultClass(result)">{{ resultLabel(result) }}</span></td></tr>
          <tr v-if="expandedLot === lot.lotNo"><td colspan="6" class="expanded-cell"><div class="lot-detail">
            <section class="record-section"><header><h3>計量檢測</h3><span>{{ lot.variableRecords.length }} 筆</span></header><div class="table-scroll"><table><thead><tr><th>SPCSerial</th><th>QC Item</th><th>CreateDate</th><th>Result</th><th>SampleSize</th><th>Mean</th><th>Min / Max</th><th>SD</th><th>Cp / Cpk</th><th v-for="factor in visibleFactorDefinitions" :key="factor.factorNo">{{ factor.name }}</th><th>ERFNo</th></tr></thead><tbody><tr v-for="row in lot.variableRecords" :key="row.id" class="data-row" @click="openRecord(row)"><td>{{ row.spcSerial }}</td><td>{{ row.qcItemNo }} — {{ row.qcItemName }}</td><td>{{ row.createDate }}</td><td><span class="result-badge" :class="resultClass(row.result)">{{ resultLabel(row.result) }}</span></td><td>{{ row.testValues.length }}</td><td>{{ row.mean }}</td><td>{{ row.min }} / {{ row.max }}</td><td>{{ row.stdDev }}</td><td>{{ row.cp }} / {{ row.cpk }}</td><td v-for="factor in visibleFactorDefinitions" :key="factor.factorNo">{{ factorValue(row, factor.factorNo) }}</td><td>{{ row.erfNo || '—' }}</td></tr><tr v-if="!lot.variableRecords.length"><td :colspan="10 + visibleFactorDefinitions.length" class="empty">沒有計量資料</td></tr></tbody></table></div></section>
            <section class="record-section"><header><h3>計數檢測</h3><span>{{ lot.attributeRecords.length }} 筆</span></header><div class="table-scroll"><table><thead><tr><th>SPCSerial</th><th>QC Item</th><th>CreateDate</th><th>Result</th><th>Sample</th><th>Good</th><th>Scrap</th><th>Defect</th><th v-for="factor in visibleFactorDefinitions" :key="factor.factorNo">{{ factor.name }}</th><th>ERFNo</th></tr></thead><tbody><tr v-for="row in lot.attributeRecords" :key="row.id" class="data-row" @click="openRecord(row)"><td>{{ row.spcSerial }}</td><td>{{ row.qcItemNo }} — {{ row.qcItemName }}</td><td>{{ row.createDate }}</td><td><span class="result-badge" :class="resultClass(row.result)">{{ resultLabel(row.result) }}</span></td><td>{{ row.sampleQty }}</td><td>{{ row.goodQty }}</td><td>{{ row.scrapQty }}</td><td>{{ row.defectQty }}</td><td v-for="factor in visibleFactorDefinitions" :key="factor.factorNo">{{ factorValue(row, factor.factorNo) }}</td><td>{{ row.erfNo || '—' }}</td></tr><tr v-if="!lot.attributeRecords.length"><td :colspan="9 + visibleFactorDefinitions.length" class="empty">沒有計數資料</td></tr></tbody></table></div></section>
          </div></td></tr>
        </template>
        <tr v-if="!groupedLots.length"><td colspan="6" class="empty">沒有符合條件的 DEV 假資料</td></tr>
      </tbody></table></div>
      <footer class="grid-foot">結果按 Lot 彙整；開啟列可檢視該 Lot 的計量與計數記錄。點選 SPCSerial 開啟唯讀明細。</footer>
    </section>
    <p class="notice" :class="noticeKind" role="status">{{ notice }}</p>

    <div v-if="selectedRecord" class="modal-backdrop" @click.self="selectedRecord = null"><section class="record-dialog" role="dialog" aria-modal="true" aria-label="SPC Lot 明細"><header class="dialog-head"><div><p class="eyebrow">{{ selectedRecord.kind === 'variable' ? 'VARIABLE MEASURE' : 'ATTRIBUTE INSPECTION' }}</p><h2>{{ selectedRecord.spcSerial }} <small>{{ selectedRecord.lotNo }}</small></h2></div><button class="button" @click="selectedRecord = null">Close</button></header>
      <dl class="record-basis"><div><dt>QC Item</dt><dd>{{ selectedRecord.qcItemNo }} — {{ selectedRecord.qcItemName }}</dd></div><div><dt>FactorSerial</dt><dd>{{ selectedRecord.factorSerial }}</dd></div><div><dt>Creator / CreateDate</dt><dd>{{ selectedRecord.creator }} / {{ selectedRecord.createDate }}</dd></div><div><dt>Result / ERFNo</dt><dd><span class="result-badge" :class="resultClass(selectedRecord.result)">{{ resultLabel(selectedRecord.result) }}</span> · {{ selectedRecord.erfNo || '—' }}</dd></div><div><dt>Memo</dt><dd>{{ selectedRecord.memo || '—' }}</dd></div></dl>
      <section class="record-factors"><h3>Collect Factor</h3><dl><div v-for="factor in qcItems.find((item) => item.no === selectedRecord?.qcItemNo)?.factors || []" :key="factor.factorNo"><dt>{{ factor.name }}</dt><dd>{{ factorValue(selectedRecord, factor.factorNo) }}</dd></div></dl></section>
      <template v-if="selectedRecord.kind === 'variable'"><section class="stat-cards"><div><strong>SampleSize</strong>{{ selectedRecord.testValues.length }}</div><div><strong>Mean</strong>{{ selectedRecord.mean }}</div><div><strong>Min / Max</strong>{{ selectedRecord.min }} / {{ selectedRecord.max }}</div><div><strong>SD</strong>{{ selectedRecord.stdDev }}</div><div><strong>Cp / Cpk</strong>{{ selectedRecord.cp }} / {{ selectedRecord.cpk }}</div></section><div class="table-scroll raw-values"><table><thead><tr><th>SerialNo</th><th>QCItemNo</th><th>TestValue</th></tr></thead><tbody><tr v-for="(value, index) in selectedRecord.testValues" :key="index"><td>{{ index + 1 }}</td><td>{{ selectedRecord.qcItemNo }}</td><td>{{ value }}</td></tr></tbody></table></div></template>
      <template v-else><section class="stat-cards"><div><strong>Sample</strong>{{ selectedRecord.sampleQty }}</div><div><strong>Good</strong>{{ selectedRecord.goodQty }}</div><div><strong>Scrap</strong>{{ selectedRecord.scrapQty }}</div><div><strong>Defect</strong>{{ selectedRecord.defectQty }}</div></section><h3 class="subheading">缺陷與元件明細</h3><div class="table-scroll raw-values"><table><thead><tr><th>ComponentNo</th><th>ErrorNo</th><th>Reason</th><th>Type</th><th>Qty</th><th>Memo</th></tr></thead><tbody><tr v-for="(error, index) in selectedRecord.errorDetails" :key="index"><td>{{ error.componentNo }}</td><td>{{ error.errorNo }}</td><td>{{ error.reason }}</td><td>{{ error.reasonType }}</td><td>{{ error.qty }}</td><td>{{ error.memo || '—' }}</td></tr><tr v-if="!selectedRecord.errorDetails.length"><td colspan="6" class="empty">沒有缺陷明細</td></tr></tbody></table></div></template>
      <p class="readonly-note">SC202 是唯讀查詢；此 DEV 明細不提供修改或刪除。</p>
    </section></div>
  </section>
  <section v-else class="pending">DEV 預覽已關閉。<button class="button" @click="emit('close')">Close</button></section>
</template>

<style scoped>
.lot-data-page{height:100%;min-height:0;overflow:auto;box-sizing:border-box;padding:14px;background:#f4f3ef;color:#292821;font:13px/1.45 "Segoe UI",Arial,sans-serif}.page-head{display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:12px}.eyebrow{margin:0;color:#9a6b00;font-size:11px;font-weight:700;letter-spacing:.08em}.page-head h1{margin:2px 0;font-size:22px}.subtitle{margin:0;color:#777366}.button{min-height:30px;padding:5px 11px;border:1px solid #aaa79a;border-radius:3px;background:#fff;color:#292821;font:inherit;cursor:pointer}.button:hover{background:#f9edbd;border-color:#c8a300}.button.primary{background:#e5c400;border-color:#c6a800;font-weight:700}.panel{margin-bottom:10px;border:1px solid #d4d1c7;border-radius:4px;background:#fff;overflow:hidden}.query-panel{padding:10px}.mode-tabs{display:flex;align-items:center;gap:5px;margin-bottom:10px}.mode-tabs button{padding:6px 12px;border:1px solid #d4d1c7;background:#faf9f6;color:#555246;font:inherit;cursor:pointer}.mode-tabs button.active{background:#fff0ad;border-color:#c6a800;color:#292821;font-weight:700}.mode-tabs span{margin-left:auto;color:#777366;font-size:11px}.type-filters{display:flex;gap:18px;margin-bottom:10px;color:#555246}.type-filters label,.check-date{display:flex;align-items:center;gap:5px}.lot-query{display:flex;align-items:end;gap:10px}.lot-query label,.item-group>label,.factor-query label{display:flex;flex-direction:column;gap:4px;color:#625f55;font-size:11px}.lot-query .wide{flex:1}.lot-query input,.item-group select,.date-query input[type=date],.factor-query select{min-height:31px;box-sizing:border-box;border:1px solid #c8c5bb;border-radius:3px;padding:5px 7px;background:#fff;color:#292821;font:inherit}.quick-lots{position:relative;color:#777366;font-size:11px}.quick-lots summary{padding:7px 9px;border:1px solid #d4d1c7;border-radius:3px;background:#faf9f6;cursor:pointer}.quick-lots div{position:absolute;z-index:4;right:0;top:32px;display:flex;flex-direction:column;min-width:210px;padding:5px;border:1px solid #d4d1c7;background:#fff;box-shadow:0 4px 12px #0002}.chip{margin:2px;padding:5px 7px;border:1px solid #e0ddd2;border-radius:12px;background:#fbfaf7;color:#555246;text-align:left;font:inherit;cursor:pointer}.item-query{display:grid;grid-template-columns:1fr 1fr;gap:10px}.item-group{padding:9px;border:1px solid #e0ddd2;border-radius:3px;background:#fbfaf7}.item-group h2{margin:0 0 7px;font-size:13px}.item-group>label select{max-width:100%}.item-checks{display:grid;grid-template-columns:1fr 1fr;gap:6px 12px;margin-top:8px}.item-checks label{display:flex;align-items:flex-start;gap:5px;color:#555246;font-size:11px}.item-checks small{display:block;color:#8b877a;font-size:10px}.hint{margin:7px 0 0;color:#777366;font-size:10px}.date-query{display:flex;align-items:center;gap:7px;margin-top:10px;padding-top:9px;border-top:1px solid #e9e6dc}.check-date{color:#625f55;font-size:11px}.date-query input:disabled{background:#efeee9;color:#999}.factor-query{display:flex;align-items:end;gap:8px;flex-wrap:wrap;margin-top:10px;padding-top:9px;border-top:1px solid #e9e6dc}.factor-query strong{padding:6px 0;color:#777366;font-size:11px}.factor-query label{min-width:130px}.query-actions{display:flex;justify-content:space-between;align-items:center;gap:8px;margin-top:10px}.query-actions span{color:#777366;font-size:10px}.results-head{display:flex;justify-content:space-between;align-items:center;padding:8px 10px;border-bottom:1px solid #e4e1d8;background:#faf9f6}.results-head h2{margin:0;font-size:14px}.results-head span{color:#777366;font-size:11px}.factor-picker{display:flex;align-items:center;gap:10px;flex-wrap:wrap;padding:8px 10px;border-bottom:1px solid #eceae4;background:#fffcf0;color:#625f55;font-size:11px}.factor-picker label{display:flex;align-items:center;gap:4px}.table-scroll{overflow:auto}.summary-scroll{max-height:520px}.summary-table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}.summary-table th,.summary-table td,table th,table td{padding:7px 8px;border-bottom:1px solid #eceae4;text-align:left}.summary-table th,table th{position:sticky;top:0;background:#f3f1ea;color:#5e5b50;font-weight:600}.summary-row{cursor:pointer}.summary-row:hover,.summary-row.expanded{background:#fff4c2}.result-badge{display:inline-block;margin:1px 3px 1px 0;padding:2px 6px;border-radius:9px;background:#e4f0e3;color:#36663a;font-size:10px}.result-1{background:#fff3ce;color:#846600}.result-2{background:#ece9fb;color:#53488c}.result-3,.result-4{background:#ffebe7;color:#9b3429}.expanded-cell{padding:0!important;background:#fbfaf6}.lot-detail{display:grid;gap:7px;padding:9px 12px}.record-section{overflow:hidden;border:1px solid #e0ddd2;border-radius:3px;background:#fff}.record-section>header{display:flex;justify-content:space-between;padding:6px 8px;border-bottom:1px solid #e9e6dc;background:#faf9f6}.record-section h3{margin:0;font-size:12px}.record-section>header span{color:#777366;font-size:10px}.record-section table,.raw-values table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:11px}.record-section th{top:0}.data-row{cursor:pointer}.data-row:hover{background:#fff4c2}.empty{text-align:center!important;color:#888!important;padding:15px!important}.grid-foot{padding:7px 10px;border-top:1px solid #eceae4;color:#777366;font-size:10px}.notice{position:sticky;bottom:0;margin:0;padding:8px 10px;border:1px solid #e3d9a8;border-radius:3px;background:#fff8d8;color:#63551b}.notice.success{border-color:#acd0ae;background:#eff8ef;color:#346238}.notice.error{border-color:#e0b2aa;background:#fff0ed;color:#922f25}.modal-backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:15px;background:#20201866}.record-dialog{width:min(980px,97vw);max-height:92vh;overflow:auto;padding:13px;border:1px solid #c5c1b4;border-radius:5px;background:#fff;box-shadow:0 16px 48px #0003}.dialog-head{display:flex;justify-content:space-between;align-items:center;padding-bottom:8px;border-bottom:1px solid #e6e3da}.dialog-head h2{margin:2px 0;font-size:18px}.dialog-head h2 small{font-size:12px;font-weight:400;color:#777366}.record-basis{display:grid;grid-template-columns:1fr 1fr;gap:0 15px;margin:8px 0}.record-basis div,.record-factors dl div{display:grid;grid-template-columns:125px minmax(0,1fr);gap:8px;padding:6px 2px;border-bottom:1px solid #f0eee8}.record-basis dt,.record-factors dt{color:#777366;font-size:10px}.record-basis dd,.record-factors dd{margin:0;overflow-wrap:anywhere}.record-factors{padding:8px;border:1px solid #e4e1d8;border-radius:3px}.record-factors h3,.subheading{margin:0 0 5px;font-size:12px}.record-factors dl{display:grid;grid-template-columns:1fr 1fr;gap:0 12px;margin:0}.stat-cards{display:flex;gap:7px;flex-wrap:wrap;margin:9px 0}.stat-cards div{min-width:90px;padding:7px;border:1px solid #e4e1d8;border-radius:3px;background:#faf9f6}.stat-cards strong{display:block;color:#777366;font-size:10px}.raw-values{max-height:330px}.subheading{margin-top:9px}.readonly-note{margin:8px 0 0;padding:6px;background:#f7f5ea;color:#777366;font-size:10px}.pending{padding:18px;color:#777366}.pending .button{margin-left:10px}
@media(max-width:1000px){.item-query{grid-template-columns:1fr}.item-checks{grid-template-columns:1fr 1fr}.mode-tabs{flex-wrap:wrap}.mode-tabs span{width:100%;margin-left:0}.lot-query{align-items:stretch;flex-direction:column}.date-query{flex-wrap:wrap}.record-basis,.record-factors dl{grid-template-columns:1fr}.record-dialog{max-height:96vh}}
</style>
