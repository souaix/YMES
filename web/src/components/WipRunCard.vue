<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type LookupKind = 'area' | 'ps' | 'op' | 'mo'
type ProcessScope = 'current-process' | 'current-ps' | 'all-processes'
type Lot = {
  lotNo: string; lotState: number; status: number; areaNo: string; psNo: string; opNo: string
  moNo: string; customerLotNo: string; productNo: string; productVersion: string
  processNo: string; processVersion: string; moduleNo: string; moduleVersion: string
  opReference: number; nodeId: string; moduleNodeId: string; curQty: number; inputQty: number
  hotLot: number; priority: number; customerNo: string; planFinishDate: string
}
type RouteStep = { processNo: string; processName: string; psNo: string; opNo: string; opName: string; nodeId: string; description: string; status: string }
type LotLog = { time: string; opNo: string; description: string; userNo: string }
type LookupRow = { value: string; name: string; extra?: string }
type ReportCard = { lot: Lot; steps: RouteStep[]; logs: LotLog[] }

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const printerStorageKey = 'ymes-dev-mock:wip-run-card:printer:v1'
const activeReportStyle = 0 // WinForms RunCardStyle defaults to ActiveReports (0) when no parameter is configured.

const sampleLots: Lot[] = [
  { lotNo: 'DEV-LOT-26011', lotState: 1, status: 1, areaNo: 'DEV-AREA-ETCH', psNo: 'DEV-PS-WAFER', opNo: 'DEV-OP-ETCH-20', moNo: 'DEV-MO-26011', customerLotNo: 'CUST-LOT-8011', productNo: 'DEV-PROD-A1', productVersion: 'A', processNo: 'DEV-PROC-WAFER', processVersion: '03', moduleNo: '', moduleVersion: '', opReference: 0, nodeId: 'N-020', moduleNodeId: '', curQty: 84, inputQty: 96, hotLot: 1, priority: 10, customerNo: 'DEV-CUSTOMER', planFinishDate: '2026/11/21' },
  { lotNo: 'DEV-LOT-26012', lotState: 1, status: 0, areaNo: 'DEV-AREA-CMP', psNo: 'DEV-PS-WAFER', opNo: 'DEV-OP-CMP-10', moNo: 'DEV-MO-26012', customerLotNo: 'CUST-LOT-8012', productNo: 'DEV-PROD-A1', productVersion: 'A', processNo: 'DEV-PROC-WAFER', processVersion: '03', moduleNo: 'DEV-MOD-CMP', moduleVersion: '01', opReference: 1, nodeId: 'N-030', moduleNodeId: 'MN-030', curQty: 96, inputQty: 96, hotLot: 0, priority: 20, customerNo: 'DEV-CUSTOMER', planFinishDate: '2026/11/22' },
  { lotNo: 'DEV-LOT-26013', lotState: 1, status: 4, areaNo: 'DEV-AREA-ETCH', psNo: 'DEV-PS-MODULE', opNo: 'DEV-OP-CLEAN-30', moNo: 'DEV-MO-26013', customerLotNo: 'CUST-LOT-8013', productNo: 'DEV-PROD-B2', productVersion: 'B', processNo: 'DEV-PROC-MODULE', processVersion: '02', moduleNo: '', moduleVersion: '', opReference: 0, nodeId: 'N-030', moduleNodeId: '', curQty: 24, inputQty: 24, hotLot: 0, priority: 30, customerNo: 'DEV-CUSTOMER', planFinishDate: '2026/11/23' },
  { lotNo: 'DEV-LOT-26014', lotState: 1, status: 1, areaNo: 'DEV-AREA-CMP', psNo: 'DEV-PS-WAFER', opNo: 'DEV-OP-CMP-10', moNo: 'DEV-MO-26014', customerLotNo: 'CUST-LOT-8014', productNo: 'DEV-PROD-A1', productVersion: 'A', processNo: 'DEV-PROC-WAFER', processVersion: '03', moduleNo: '', moduleVersion: '', opReference: 0, nodeId: 'N-030', moduleNodeId: '', curQty: 18, inputQty: 20, hotLot: 0, priority: 40, customerNo: 'DEV-CUSTOMER', planFinishDate: '2026/11/24' },
  { lotNo: 'DEV-LOT-26015', lotState: 0, status: 1, areaNo: 'DEV-AREA-ETCH', psNo: 'DEV-PS-WAFER', opNo: 'DEV-OP-ETCH-20', moNo: 'DEV-MO-26015', customerLotNo: 'CUST-LOT-8015', productNo: 'DEV-PROD-A1', productVersion: 'A', processNo: 'DEV-PROC-WAFER', processVersion: '03', moduleNo: '', moduleVersion: '', opReference: 0, nodeId: 'N-020', moduleNodeId: '', curQty: 48, inputQty: 48, hotLot: 0, priority: 50, customerNo: 'DEV-CUSTOMER', planFinishDate: '2026/11/25' },
  { lotNo: 'DEV-LOT-26016', lotState: 1, status: 11, areaNo: 'DEV-AREA-CMP', psNo: 'DEV-PS-WAFER', opNo: 'DEV-OP-CMP-10', moNo: 'DEV-MO-26016', customerLotNo: 'CUST-LOT-8016', productNo: 'DEV-PROD-A1', productVersion: 'A', processNo: 'DEV-PROC-WAFER', processVersion: '03', moduleNo: '', moduleVersion: '', opReference: 0, nodeId: 'N-030', moduleNodeId: '', curQty: 10, inputQty: 10, hotLot: 0, priority: 60, customerNo: 'DEV-CUSTOMER', planFinishDate: '2026/11/26' },
]
const routeSteps: RouteStep[] = [
  { processNo: 'DEV-PROC-WAFER', processName: 'DEV 晶圓主流程', psNo: 'DEV-PS-WAFER', opNo: 'DEV-OP-START', opName: '投批', nodeId: 'N-001', description: '確認工單與批號資料', status: 'Complete' },
  { processNo: 'DEV-PROC-WAFER', processName: 'DEV 晶圓主流程', psNo: 'DEV-PS-WAFER', opNo: 'DEV-OP-ETCH-20', opName: '蝕刻 20', nodeId: 'N-020', description: '蝕刻站標準作業', status: 'Running' },
  { processNo: 'DEV-PROC-WAFER', processName: 'DEV 晶圓主流程', psNo: 'DEV-PS-WAFER', opNo: 'DEV-OP-CMP-10', opName: 'CMP 10', nodeId: 'N-030', description: '研磨後量測與外觀確認', status: 'Queue' },
  { processNo: 'DEV-PROC-WAFER', processName: 'DEV 晶圓主流程', psNo: 'DEV-PS-MODULE', opNo: 'DEV-OP-CLEAN-30', opName: '清洗 30', nodeId: 'N-040', description: '清洗與乾燥', status: 'Queue' },
  { processNo: 'DEV-PROC-MODULE', processName: 'DEV 模組流程', psNo: 'DEV-PS-MODULE', opNo: 'DEV-OP-CLEAN-30', opName: '清洗 30', nodeId: 'N-030', description: '模組流程清洗站', status: 'Complete' },
]
const sampleLogs: Record<string, LotLog[]> = {
  'DEV-LOT-26011': [
    { time: '2026/11/20 08:12:05', opNo: 'DEV-OP-START', description: '建立生產批', userNo: 'DEVUSER' },
    { time: '2026/11/20 09:43:18', opNo: 'DEV-OP-ETCH-20', description: '開始作業', userNo: 'DEVUSER' },
  ],
  'DEV-LOT-26012': [{ time: '2026/11/20 07:52:40', opNo: 'DEV-OP-START', description: '批次投入', userNo: 'DEVUSER' }],
  'DEV-LOT-26013': [{ time: '2026/11/19 18:10:12', opNo: 'DEV-OP-CLEAN-30', description: '完成清洗', userNo: 'DEVUSER' }],
}
const lookupRows: Record<LookupKind, LookupRow[]> = {
  area: [{ value: 'DEV-AREA-ETCH', name: 'DEV 蝕刻區' }, { value: 'DEV-AREA-CMP', name: 'DEV CMP 區' }],
  ps: [{ value: 'DEV-PS-WAFER', name: 'DEV 晶圓區段' }, { value: 'DEV-PS-MODULE', name: 'DEV 模組區段' }],
  op: [{ value: 'DEV-OP-START', name: '投批' }, { value: 'DEV-OP-ETCH-20', name: '蝕刻 20' }, { value: 'DEV-OP-CMP-10', name: 'CMP 10' }, { value: 'DEV-OP-CLEAN-30', name: '清洗 30' }],
  mo: [{ value: 'DEV-MO-26011', name: 'DEV 工單 26011', extra: 'DEV-PROD-A1' }, { value: 'DEV-MO-26012', name: 'DEV 工單 26012', extra: 'DEV-PROD-A1' }, { value: 'DEV-MO-26013', name: 'DEV 工單 26013', extra: 'DEV-PROD-B2' }],
}
const samplePrinters = ['DEV Office Printer', 'DEV Production Label Printer', 'Microsoft Print to PDF']

const filters = ref({ areaNo: '', psNo: '', opNo: '', moNo: '', customerLotNo: '', lotNo: '' })
const scope = ref<ProcessScope>('current-process')
const includeLog = ref(true)
const rows = ref<Lot[]>([])
const activeLotNo = ref('')
const selectedLotNos = ref<string[]>([])
const queried = ref(false)
const errorText = ref('')
const statusMessage = ref('Ready')
const lookupKind = ref<LookupKind | null>(null)
const lookupSearch = ref('')
const printerPickerOpen = ref(false)
const printerName = ref('')
const reportOpen = ref(false)
const reportCards = ref<ReportCard[]>([])
const reportMode = ref<'preview' | 'print'>('preview')
const reportIndex = ref(0)

const activeLot = computed(() => rows.value.find(row => row.lotNo === activeLotNo.value) || null)
const selectedRows = computed(() => rows.value.filter(row => selectedLotNos.value.includes(row.lotNo)))
const visibleLookupRows = computed(() => {
  if (!lookupKind.value) return []
  const term = lookupSearch.value.trim().toLowerCase()
  return lookupRows[lookupKind.value].filter(row => `${row.value} ${row.name} ${row.extra || ''}`.toLowerCase().includes(term))
})
const currentReport = computed(() => reportCards.value[reportIndex.value] || null)
const reportSteps = computed(() => currentReport.value?.steps || [])
const reportTimeline = computed(() => {
  const logs = includeLog.value && currentReport.value ? currentReport.value.logs : []
  return [...logs.map((log, index) => ({ itemNo: index + 1, opNo: log.opNo, description: `${log.description}（${log.userNo}，${log.time}）`, kind: 'Log' })), ...reportSteps.value.map((step, index) => ({ itemNo: logs.length + index + 1, opNo: step.opNo, description: `${step.opName}：${step.description}`, kind: step.status }))]
})

function match(value: string, filter: string): boolean { return !filter.trim() || value.toUpperCase() === filter.trim().toUpperCase() }
function statusLabel(status: number): string { return status === 0 ? 'Queue' : status === 1 ? 'Running' : status === 4 ? 'Complete' : status === 11 ? 'Inventory' : String(status) }
function query(): void {
  errorText.value = ''
  queried.value = true
  if (!dev.value) { rows.value = []; activeLotNo.value = ''; selectedLotNos.value = []; return }
  rows.value = sampleLots.filter(row => [0, 1, 4].includes(row.status) && row.lotState !== 0 &&
    match(row.areaNo, filters.value.areaNo) && match(row.psNo, filters.value.psNo) &&
    match(row.opNo, filters.value.opNo) && match(row.moNo, filters.value.moNo) &&
    match(row.customerLotNo, filters.value.customerLotNo) && match(row.lotNo, filters.value.lotNo))
    .sort((left, right) => right.hotLot - left.hotLot || left.priority - right.priority || left.lotNo.localeCompare(right.lotNo))
  activeLotNo.value = rows.value[0]?.lotNo || ''
  selectedLotNos.value = []
  statusMessage.value = `Rows: ${rows.value.length}`
}
function setFilter(kind: LookupKind, value: string): void {
  const key: Record<LookupKind, keyof typeof filters.value> = { area: 'areaNo', ps: 'psNo', op: 'opNo', mo: 'moNo' }
  filters.value[key[kind]] = value
  lookupKind.value = null
  lookupSearch.value = ''
}
function openLookup(kind: LookupKind): void { lookupKind.value = kind; lookupSearch.value = '' }
function toggleSelected(row: Lot, checked: boolean): void {
  activeLotNo.value = row.lotNo
  if (checked && !selectedLotNos.value.includes(row.lotNo)) selectedLotNos.value = [...selectedLotNos.value, row.lotNo]
  if (!checked) selectedLotNos.value = selectedLotNos.value.filter(value => value !== row.lotNo)
}
function activate(row: Lot): void { activeLotNo.value = row.lotNo }
function cardFor(lot: Lot): ReportCard {
  const matchingSteps = routeSteps.filter(step => {
    if (scope.value === 'current-process') return step.processNo === lot.processNo
    if (scope.value === 'current-ps') return step.psNo === lot.psNo
    return true
  })
  return { lot, steps: matchingSteps, logs: includeLog.value ? (sampleLogs[lot.lotNo] || []) : [] }
}
function openPreview(mode: 'preview' | 'print'): void {
  errorText.value = ''
  if (!activeLot.value) { errorText.value = '請先選擇一筆 Lot State。'; return }
  if (!selectedLotNos.value.includes(activeLot.value.lotNo)) selectedLotNos.value = [...selectedLotNos.value, activeLot.value.lotNo]
  if (mode === 'print' && activeReportStyle !== 0 && !printerName.value.trim()) {
    errorText.value = '請先選擇 PrinterName。'
    return
  }
  // The legacy ActiveReports Print path prints only the active row; Preview handles all selected rows.
  const targets = mode === 'print' && activeReportStyle === 0 ? [activeLot.value] : selectedRows.value
  reportCards.value = targets.map(cardFor)
  reportIndex.value = 0
  reportMode.value = mode
  reportOpen.value = true
}
function choosePrinter(name: string): void {
  printerName.value = name
  printerPickerOpen.value = false
  if (dev.value) window.localStorage.setItem(printerStorageKey, name)
}
function loadPrinter(): void {
  if (!dev.value) return
  try { printerName.value = window.localStorage.getItem(printerStorageKey) || '' } catch { printerName.value = '' }
}
function showShippingNoStatus(): void {
  statusMessage.value = 'ShippingNO：原 WinForms Click handler 已於 2020/07/15 註解，無可執行流程。'
}
onMounted(loadPrinter)
</script>

<template>
  <section class="wp103-page" aria-label="列印流程卡">
    <div v-if="!dev" class="preview-note">目前為唯讀預覽；切換至 DEV 假資料模式後可操作範例批號。</div>
    <div v-if="errorText" class="message error-message" role="alert">{{ errorText }}</div>

    <fieldset class="wp103-frame query-frame">
      <legend>Query</legend>
      <div class="wp103-query-grid">
        <label for="wp103-area">Area No</label>
        <div class="wp103-lookup"><input id="wp103-area" v-model.trim="filters.areaNo" class="mes-input" :disabled="!dev" @input="filters.areaNo = filters.areaNo.toUpperCase()" @keydown.enter.prevent="query"><button type="button" class="mes-button lookup-button" :disabled="!dev" @click="openLookup('area')">...</button></div>
        <label for="wp103-ps">PS No</label>
        <div class="wp103-lookup"><input id="wp103-ps" v-model.trim="filters.psNo" class="mes-input" :disabled="!dev" @input="filters.psNo = filters.psNo.toUpperCase()" @keydown.enter.prevent="query"><button type="button" class="mes-button lookup-button" :disabled="!dev" @click="openLookup('ps')">...</button></div>
        <button type="button" class="mes-button query-button" :disabled="!dev" @click="query">Query</button>

        <label for="wp103-op">OP No</label>
        <div class="wp103-lookup"><input id="wp103-op" v-model.trim="filters.opNo" class="mes-input" :disabled="!dev" @input="filters.opNo = filters.opNo.toUpperCase()" @keydown.enter.prevent="query"><button type="button" class="mes-button lookup-button" :disabled="!dev" @click="openLookup('op')">...</button></div>
        <label for="wp103-mo">MO No</label>
        <div class="wp103-lookup"><input id="wp103-mo" v-model.trim="filters.moNo" class="mes-input" :disabled="!dev" @input="filters.moNo = filters.moNo.toUpperCase()" @keydown.enter.prevent="query"><button type="button" class="mes-button lookup-button" :disabled="!dev" @click="openLookup('mo')">...</button></div>
        <span></span>

        <label for="wp103-customer-lot">CustomerLotNo</label>
        <input id="wp103-customer-lot" v-model.trim="filters.customerLotNo" class="mes-input" :disabled="!dev" @input="filters.customerLotNo = filters.customerLotNo.toUpperCase()" @keydown.enter.prevent="query">
        <label for="wp103-lot">Lot No</label>
        <input id="wp103-lot" v-model.trim="filters.lotNo" class="mes-input" :disabled="!dev" @input="filters.lotNo = filters.lotNo.toUpperCase()" @keydown.enter.prevent="query">
        <span></span>
      </div>
      <span class="wp103-hidden-inventory" aria-hidden="true">In Inventory (hidden by WinForms)</span>
    </fieldset>

    <fieldset class="wp103-frame printer-frame">
      <legend>SetUpPrinter</legend>
      <label for="wp103-printer">PrinterName</label>
      <input id="wp103-printer" v-model="printerName" class="mes-input printer-input" disabled>
      <button type="button" class="mes-button lookup-button" :disabled="!dev" aria-label="選擇印表機" @click="printerPickerOpen = true">...</button>
    </fieldset>

    <fieldset class="wp103-frame lot-frame">
      <legend>Lot State</legend>
      <div class="wp103-lot-layout">
        <div class="wp103-lot-main">
          <div class="wp103-options">
            <label><input v-model="scope" type="radio" value="current-process" :disabled="!dev"> Current Process</label>
            <label><input v-model="scope" type="radio" value="current-ps" :disabled="!dev"> Current PS Processes</label>
            <label><input v-model="scope" type="radio" value="all-processes" :disabled="!dev"> All Processes</label>
            <label><input v-model="includeLog" type="checkbox" :disabled="!dev"> Include Log</label>
          </div>
          <div class="wp103-grid-scroll">
            <table class="wp103-grid">
              <thead><tr><th class="check-col"></th><th>LotNo</th><th>Status</th><th>AreaNo</th><th>PSNo</th><th>OPNo</th><th>MONo</th><th>CustomerLotNo</th><th>ProductNo</th><th>CurQty</th><th>HotLot</th></tr></thead>
              <tbody>
                <tr v-for="row in rows" :key="row.lotNo" :class="{ active: activeLotNo === row.lotNo, selected: selectedLotNos.includes(row.lotNo) }" tabindex="0" @click="activate(row)" @keydown.enter="activate(row)">
                  <td class="check-col"><input type="checkbox" :checked="selectedLotNos.includes(row.lotNo)" :disabled="!dev" :aria-label="`選取 ${row.lotNo}`" @click.stop @change="toggleSelected(row, ($event.target as HTMLInputElement).checked)"></td>
                  <td>{{ row.lotNo }}</td><td>{{ statusLabel(row.status) }}</td><td>{{ row.areaNo }}</td><td>{{ row.psNo }}</td><td>{{ row.opNo }}</td><td>{{ row.moNo }}</td><td>{{ row.customerLotNo }}</td><td>{{ row.productNo }}</td><td class="number-cell">{{ row.curQty }}</td><td>{{ row.hotLot ? 'Yes' : 'No' }}</td>
                </tr>
                <tr v-if="rows.length === 0"><td colspan="11" class="empty-row">{{ queried ? '查無資料' : '按 Query 查詢' }}</td></tr>
              </tbody>
            </table>
          </div>
          <div class="wp103-statusbar"><span>{{ statusMessage }}</span><span>Rows: {{ rows.length }}</span><span>Selected: {{ selectedLotNos.length }}</span></div>
        </div>
        <aside class="wp103-actions">
          <button type="button" class="mes-button" :disabled="!dev" @click="openPreview('preview')">Preview(&amp;V)</button>
          <button type="button" class="mes-button" :disabled="!dev" @click="openPreview('print')">Print(&amp;P)</button>
          <button type="button" class="mes-button" :disabled="!dev" title="WinForms 的 ShippingNO Click handler 已於 2020/07/15 註解" @click="showShippingNoStatus">ShippingNO</button>
          <span class="wp103-action-spacer"></span>
          <button type="button" class="mes-button" @click="emit('close')">Close (&amp;X)</button>
        </aside>
      </div>
    </fieldset>

    <div v-if="lookupKind" class="wp103-overlay" @click.self="lookupKind = null">
      <section class="wp103-dialog lookup-dialog" role="dialog" aria-modal="true" :aria-label="`${lookupKind} lookup`">
        <header><strong>{{ lookupKind === 'area' ? 'Area No' : lookupKind === 'ps' ? 'PS No' : lookupKind === 'op' ? 'OP No' : 'MO No' }}</strong><button type="button" class="mes-button" @click="lookupKind = null">Close</button></header>
        <input v-model.trim="lookupSearch" class="mes-input" placeholder="Search" autofocus>
        <div class="wp103-lookup-list"><button v-for="row in visibleLookupRows" :key="row.value" type="button" class="wp103-lookup-item" @click="setFilter(lookupKind!, row.value)"><strong>{{ row.value }}</strong><span>{{ row.name }}</span><small v-if="row.extra">{{ row.extra }}</small></button><div v-if="visibleLookupRows.length === 0" class="empty-row">查無資料</div></div>
      </section>
    </div>

    <div v-if="printerPickerOpen" class="wp103-overlay" @click.self="printerPickerOpen = false">
      <section class="wp103-dialog printer-dialog" role="dialog" aria-modal="true" aria-label="選擇印表機">
        <header><strong>Printer</strong><button type="button" class="mes-button" @click="printerPickerOpen = false">Close</button></header>
        <button v-for="printer in samplePrinters" :key="printer" type="button" class="wp103-lookup-item" @click="choosePrinter(printer)">{{ printer }}</button>
      </section>
    </div>

    <div v-if="reportOpen && currentReport" class="wp103-overlay report-overlay" @click.self="reportOpen = false">
      <section class="wp103-dialog report-dialog" role="dialog" aria-modal="true" aria-label="流程卡預覽">
        <header><div><strong>{{ reportMode === 'preview' ? 'Run Card Preview' : 'Run Card Print Preview' }}</strong><span class="report-count">{{ reportIndex + 1 }} / {{ reportCards.length }}</span></div><button type="button" class="mes-button" @click="reportOpen = false">Close</button></header>
        <div class="report-paper">
          <div class="report-title">RUN CARD</div>
          <div class="report-fields">
            <span><b>LotNo</b>{{ currentReport.lot.lotNo }}</span><span><b>ProductNo</b>{{ currentReport.lot.productNo }} / {{ currentReport.lot.productVersion }}</span>
            <span><b>CustomerLotNo</b>{{ currentReport.lot.customerLotNo }}</span><span><b>MO No</b>{{ currentReport.lot.moNo }}</span>
            <span><b>Process</b>{{ currentReport.lot.processNo }} / {{ currentReport.lot.processVersion }}</span><span><b>Release Qty</b>{{ currentReport.lot.inputQty }}</span>
            <span><b>Current OP</b>{{ currentReport.lot.opNo }}</span><span><b>Finish Date</b>{{ currentReport.lot.planFinishDate }}</span>
          </div>
          <table class="wp103-report-grid"><thead><tr><th>ItemNo</th><th>OPNo</th><th>Description</th><th>Step</th></tr></thead><tbody>
            <tr v-for="row in reportTimeline" :key="`${row.itemNo}-${row.opNo}`"><td>{{ row.itemNo }}</td><td>{{ row.opNo }}</td><td>{{ row.description }}</td><td>{{ row.kind }}</td></tr>
            <tr v-if="reportTimeline.length === 0"><td colspan="4" class="empty-row">無流程站資料</td></tr>
          </tbody></table>
          <div class="report-disclaimer">DEV 假資料預覽；此頁不會送出實體列印工作。</div>
        </div>
        <footer><button type="button" class="mes-button" :disabled="reportIndex === 0" @click="reportIndex--">Previous Lot</button><button type="button" class="mes-button" :disabled="reportIndex >= reportCards.length - 1" @click="reportIndex++">Next Lot</button><span></span><button type="button" class="mes-button" @click="reportOpen = false">Close</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.wp103-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:6px;padding:6px;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif;box-sizing:border-box}
.preview-note,.message{padding:7px 10px;border:1px solid #d7c790;background:#fff8dd;color:#5c4a16}.error-message{border-color:#d8a7a2;background:#fff1ef;color:#8f261f}
.wp103-frame{min-width:0;margin:0;padding:8px 10px 10px;border:1px solid #bdbdbd;background:#fff;box-sizing:border-box}.wp103-frame legend{padding:0 5px;color:#444}.query-frame{height:122px;flex:0 0 122px;position:relative}.wp103-query-grid{display:grid;grid-template-columns:112px minmax(150px,1fr) 112px minmax(150px,1fr) 88px;grid-template-rows:repeat(3,28px);align-items:center;gap:2px 7px}.wp103-query-grid>label{text-align:right;white-space:nowrap}.mes-input{height:24px;min-width:0;border:1px solid #8c8c8c;background:#fff3bd;padding:2px 6px;box-sizing:border-box;color:#333}.mes-input:disabled{background:#eee;color:#777}.wp103-lookup{display:flex;gap:3px;min-width:0}.wp103-lookup>.mes-input{flex:1}.mes-button{min-height:27px;padding:3px 11px;border:1px solid #b7b7b7;border-radius:3px;background:linear-gradient(#fff,#f5f5f5);color:#333;white-space:nowrap;cursor:pointer}.mes-button:hover:not(:disabled){border-color:#888;background:#fff8e8}.mes-button:disabled{color:#999;cursor:default}.lookup-button{width:30px;min-width:30px;padding:2px}.query-button{grid-column:5;grid-row:2 / span 2;justify-self:end;width:82px;height:31px}.wp103-hidden-inventory{display:none}
.printer-frame{height:50px;flex:0 0 50px;display:flex;align-items:center;gap:7px}.printer-frame legend{font-size:12px}.printer-frame label{width:108px;text-align:right}.printer-input{width:min(300px,45vw)}
.lot-frame{display:flex;flex:1 1 auto;min-height:180px;flex-direction:column}.wp103-lot-layout{display:flex;gap:8px;flex:1;min-height:0}.wp103-lot-main{display:flex;flex:1;min-width:0;min-height:0;flex-direction:column}.wp103-options{height:31px;flex:0 0 31px;display:flex;align-items:center;gap:17px;white-space:nowrap}.wp103-options label{display:flex;align-items:center;gap:4px}.wp103-options input{accent-color:#1674cc}.wp103-grid-scroll{flex:1;min-height:0;overflow:auto;border:1px solid #b7b7b7}.wp103-grid,.wp103-report-grid{width:100%;border-collapse:collapse;white-space:nowrap}.wp103-grid th,.wp103-report-grid th{position:sticky;top:0;z-index:1;padding:5px 7px;border:1px solid #b7b7b7;background:#dfeaf2;color:#333;text-align:left;font-weight:600}.wp103-grid td,.wp103-report-grid td{height:27px;padding:3px 7px;border:1px solid #c8c8c8;background:#fff3bd}.wp103-grid tbody tr{cursor:pointer}.wp103-grid tbody tr.active td{outline:1px solid #ca9b31;outline-offset:-2px}.wp103-grid tbody tr.selected td{background:#ffe7a0}.wp103-grid .check-col{width:26px;text-align:center}.wp103-grid input{accent-color:#d38d22}.number-cell{text-align:right}.empty-row{text-align:center!important;color:#777!important;background:#fff!important;height:44px!important}.wp103-statusbar{height:24px;flex:0 0 24px;display:flex;align-items:center;gap:20px;padding:0 4px;color:#555;border-bottom:1px solid #c7c7c7}.wp103-actions{display:flex;flex:0 0 96px;flex-direction:column;gap:7px;padding-top:33px}.wp103-actions .mes-button{width:96px;height:31px}.wp103-action-spacer{flex:1}
.wp103-overlay{position:fixed;inset:0;z-index:1600;display:grid;place-items:center;padding:20px;background:#0006}.wp103-dialog{width:min(650px,94vw);max-height:min(680px,90vh);display:flex;flex-direction:column;gap:10px;padding:14px;background:#fff;border:1px solid #888;box-shadow:0 8px 26px #0004;box-sizing:border-box}.wp103-dialog>header,.wp103-dialog>footer{display:flex;align-items:center;justify-content:space-between;gap:8px}.wp103-lookup-list{overflow:auto;border:1px solid #ccc}.wp103-lookup-item{width:100%;display:flex;gap:14px;align-items:center;padding:9px 10px;border:0;border-bottom:1px solid #ddd;background:#fff;text-align:left;cursor:pointer;color:#333}.wp103-lookup-item:hover{background:#fff2cc}.wp103-lookup-item small{margin-left:auto;color:#666}.printer-dialog{width:min(480px,94vw)}.report-overlay{z-index:1700}.report-dialog{width:min(1080px,96vw);height:min(780px,92vh);max-height:92vh}.report-dialog>header div{display:flex;align-items:center;gap:14px}.report-count{color:#666}.report-paper{flex:1;overflow:auto;padding:24px;border:1px solid #aaa;background:#fff;box-shadow:inset 0 0 0 1px #eee}.report-title{text-align:center;font-size:21px;font-weight:700;letter-spacing:3px;margin:0 0 18px}.report-fields{display:grid;grid-template-columns:1fr 1fr;gap:8px 22px;padding:12px 0 18px;border-bottom:1px solid #777}.report-fields span{display:flex;gap:8px}.report-fields b{width:105px}.wp103-report-grid{margin-top:16px}.wp103-report-grid th{position:static}.wp103-report-grid td{white-space:normal}.report-disclaimer{margin-top:20px;padding:8px;color:#765b16;background:#fff7d8}.report-dialog>footer span{flex:1}
@media(max-width:900px){.wp103-query-grid{grid-template-columns:88px minmax(120px,1fr) 88px minmax(120px,1fr) 78px;gap:2px 5px}.wp103-options{gap:8px;font-size:12px}.wp103-actions{flex-basis:82px}.wp103-actions .mes-button{width:82px;padding:3px 5px}.wp103-grid{font-size:11px}}
</style>
