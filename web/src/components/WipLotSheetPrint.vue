<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type Scope = 'current' | 'original'
type Lot = {
  lotNo: string; lotState: number; status: number; areaNo: string; psNo: string; opNo: string
  moNo: string; moCreateDate: string; customerLotNo: string; productNo: string; productVersion: string
  productName: string; processNo: string; processVersion: string; mainProcessNo: string; mainProcessVersion: string
  nodeId: string; moduleNo: string; moduleVersion: string; moduleNodeId: string; opReference: number
  curQty: number; bigQty: number; sortQty: number; inputQty: number; hotLot: number; priority: number
  moState: number; moType: string; finishDate: string; wipDate: string; lotRowsCount: number; printCount: number
  inspectionNo: string; cimLotNo: string; currentSteps: Step[]; originalSteps: Step[]; resumeSteps: Step[]; netFlow: boolean
}
type Step = { opNo: string; opName: string; description: string; spec: string; recipe: string; remark: string; status: string }
type ProductOption = { productNo: string; productVersion: string; productName: string }
type ReportCard = { lot: Lot | null; productNo: string; productVersion: string; productName: string; emptyVersion: boolean; steps: Step[] }

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const printerKey = 'ymes-dev-mock:wip-lot-sheet-print:printer:v1'
const printCountKey = 'ymes-dev-mock:wip-lot-sheet-print:counts:v1'
const activeReportStyle = 0

const productOptions: ProductOption[] = [
  { productNo: 'DEV-PROD-GLASS-01', productVersion: 'A', productName: 'DEV 光學玻璃基板' },
  { productNo: 'DEV-PROD-GLASS-02', productVersion: 'B', productName: 'DEV 精密玻璃基板' },
]
const commonSteps: Step[] = [
  { opNo: 'DEV-OP-CUT-10', opName: '切割 10', description: '切割檢查', spec: 'WIP_INST_SOP_CUT/DEV-SOP-10', recipe: 'DEV-MATRIX-CUT-01', remark: '確認崩邊與尺寸', status: 'Complete' },
  { opNo: 'DEV-OP-GRIND-20', opName: '研磨 20', description: '研磨製程', spec: 'WIP_SPEC_GRIND/厚度依圖面', recipe: 'DEV-MATRIX-GRIND-02', remark: '量測厚度', status: 'Running' },
  { opNo: 'DEV-OP-CLEAN-30', opName: '清洗 30', description: '清洗與乾燥', spec: 'WIP_COMM_SOP_CLEAN/DEV-SOP-30', recipe: 'DEV-MATRIX-CLEAN-03', remark: '', status: 'Queue' },
]
const sampleLots: Lot[] = [
  { lotNo: 'DEV-LOT-SHEET-001', lotState: 1, status: 1, areaNo: 'DEV-AREA-GRIND', psNo: 'DEV-PS-GLASS', opNo: 'DEV-OP-GRIND-20', moNo: 'DEV-WO-261120-01', moCreateDate: '2026/11/20 08:05:00', customerLotNo: 'DEV-CUST-GL-001', productNo: 'DEV-PROD-GLASS-01', productVersion: 'A', productName: 'DEV 光學玻璃基板', processNo: 'DEV-PROC-REWORK', processVersion: '01', mainProcessNo: 'DEV-PROC-GLASS', mainProcessVersion: '03', nodeId: 'DEV-NODE-RW20', moduleNo: '', moduleVersion: '', moduleNodeId: '', opReference: 0, curQty: 88, bigQty: 11, sortQty: 8, inputQty: 96, hotLot: 1, priority: 10, moState: 3, moType: 'DEV量產工單', finishDate: '2026/11/24', wipDate: '2026/11/20 08:10:00', lotRowsCount: 4, printCount: 2, inspectionNo: 'DEV-INSP-001', cimLotNo: 'DEV-CIM-001', currentSteps: [{ opNo: 'DEV-OP-RW-10', opName: '重工確認 10', description: '重工前外觀檢查', spec: 'DEV-RW-SPEC-10', recipe: 'DEV-MATRIX-RW-10', remark: '重工流程', status: 'Complete' }, { opNo: 'DEV-OP-GRIND-20', opName: '研磨 20', description: '研磨製程', spec: 'WIP_SPEC_GRIND/厚度依圖面', recipe: 'DEV-MATRIX-GRIND-02', remark: '目前站', status: 'Running' }], originalSteps: commonSteps, resumeSteps: [{ opNo: 'DEV-OP-POLISH-40', opName: '拋光 40', description: '主流程恢復站', spec: 'DEV-POLISH-SPEC', recipe: 'DEV-MATRIX-POLISH-04', remark: '從處置退回站接續', status: 'Queue' }], netFlow: false },
  { lotNo: 'DEV-LOT-SHEET-002', lotState: 1, status: 0, areaNo: 'DEV-AREA-CLEAN', psNo: 'DEV-PS-GLASS', opNo: 'DEV-OP-CLEAN-30', moNo: 'DEV-WO-261120-01', moCreateDate: '2026/11/20 08:05:00', customerLotNo: 'DEV-CUST-GL-002', productNo: 'DEV-PROD-GLASS-01', productVersion: 'A', productName: 'DEV 光學玻璃基板', processNo: 'DEV-PROC-GLASS', processVersion: '03', mainProcessNo: 'DEV-PROC-GLASS', mainProcessVersion: '03', nodeId: 'DEV-NODE-CL30', moduleNo: '', moduleVersion: '', moduleNodeId: '', opReference: 0, curQty: 96, bigQty: 12, sortQty: 8, inputQty: 96, hotLot: 0, priority: 20, moState: 3, moType: 'DEV量產工單', finishDate: '2026/11/24', wipDate: '2026/11/20 08:20:00', lotRowsCount: 4, printCount: 0, inspectionNo: 'DEV-INSP-002', cimLotNo: 'DEV-CIM-002', currentSteps: commonSteps, originalSteps: commonSteps, resumeSteps: [], netFlow: false },
  { lotNo: 'DEV-LOT-SHEET-003', lotState: 1, status: 11, areaNo: 'DEV-AREA-STORE', psNo: 'DEV-PS-GLASS', opNo: 'DEV-OP-END-90', moNo: 'DEV-WO-261120-01', moCreateDate: '2026/11/20 08:05:00', customerLotNo: 'DEV-CUST-GL-003', productNo: 'DEV-PROD-GLASS-01', productVersion: 'A', productName: 'DEV 光學玻璃基板', processNo: 'DEV-PROC-GLASS', processVersion: '03', mainProcessNo: 'DEV-PROC-GLASS', mainProcessVersion: '03', nodeId: 'DEV-NODE-END', moduleNo: '', moduleVersion: '', moduleNodeId: '', opReference: 0, curQty: 96, bigQty: 12, sortQty: 8, inputQty: 96, hotLot: 0, priority: 30, moState: 3, moType: 'DEV量產工單', finishDate: '2026/11/24', wipDate: '2026/11/20 08:40:00', lotRowsCount: 4, printCount: 1, inspectionNo: 'DEV-INSP-003', cimLotNo: 'DEV-CIM-003', currentSteps: commonSteps, originalSteps: commonSteps, resumeSteps: [], netFlow: false },
  { lotNo: 'DEV-LOT-SHEET-004', lotState: 1, status: 1, areaNo: 'DEV-AREA-CUT', psNo: 'DEV-PS-GLASS', opNo: 'DEV-OP-CUT-10', moNo: 'DEV-WO-261119-02', moCreateDate: '2026/11/19 16:20:00', customerLotNo: 'DEV-CUST-GL-004', productNo: 'DEV-PROD-GLASS-02', productVersion: 'B', productName: 'DEV 精密玻璃基板', processNo: 'DEV-PROC-GLASS-B', processVersion: '02', mainProcessNo: 'DEV-PROC-GLASS-B', mainProcessVersion: '02', nodeId: 'DEV-NODE-CUT10', moduleNo: '', moduleVersion: '', moduleNodeId: '', opReference: 0, curQty: 48, bigQty: 6, sortQty: 8, inputQty: 48, hotLot: 0, priority: 40, moState: 3, moType: 'DEV量產工單', finishDate: '2026/11/25', wipDate: '2026/11/19 17:00:00', lotRowsCount: 2, printCount: 0, inspectionNo: '', cimLotNo: '', currentSteps: commonSteps, originalSteps: commonSteps, resumeSteps: [], netFlow: true },
  { lotNo: 'DEV-LOT-SHEET-005', lotState: 0, status: 1, areaNo: 'DEV-AREA-CUT', psNo: 'DEV-PS-GLASS', opNo: 'DEV-OP-CUT-10', moNo: 'DEV-WO-261119-02', moCreateDate: '2026/11/19 16:20:00', customerLotNo: 'DEV-CUST-GL-005', productNo: 'DEV-PROD-GLASS-02', productVersion: 'B', productName: 'DEV 精密玻璃基板', processNo: 'DEV-PROC-GLASS-B', processVersion: '02', mainProcessNo: 'DEV-PROC-GLASS-B', mainProcessVersion: '02', nodeId: 'DEV-NODE-CUT10', moduleNo: '', moduleVersion: '', moduleNodeId: '', opReference: 0, curQty: 48, bigQty: 6, sortQty: 8, inputQty: 48, hotLot: 0, priority: 50, moState: 3, moType: 'DEV量產工單', finishDate: '2026/11/25', wipDate: '2026/11/19 17:00:00', lotRowsCount: 2, printCount: 0, inspectionNo: '', cimLotNo: '', currentSteps: commonSteps, originalSteps: commonSteps, resumeSteps: [], netFlow: true },
]
const fakePrinters = ['DEV Default Production Printer', 'DEV Office Printer', 'Microsoft Print to PDF']

const filters = ref({ moNo: '', useCreateRange: false, start: '', end: '' })
const scope = ref<Scope>('current')
const rows = ref<Lot[]>([])
const activeLotNo = ref('')
const selectedLotNos = ref<string[]>([])
const queried = ref(false)
const errorText = ref('')
const warningText = ref('')
const statusText = ref('Ready')
const printerName = ref('')
const printerOpen = ref(false)
const moLookupOpen = ref(false)
const productLookupOpen = ref(false)
const productSearch = ref('')
const reportOpen = ref(false)
const reportCards = ref<ReportCard[]>([])
const reportIndex = ref(0)
const reportMode = ref<'preview' | 'print' | 'empty'>('preview')
const storedCounts = ref<Record<string, number>>({})

const activeLot = computed(() => rows.value.find(item => item.lotNo === activeLotNo.value) || null)
const selectedRows = computed(() => rows.value.filter(item => selectedLotNos.value.includes(item.lotNo)))
const currentCard = computed(() => reportCards.value[reportIndex.value] || null)
const productResults = computed(() => {
  const term = productSearch.value.trim().toLowerCase()
  return productOptions.filter(item => `${item.productNo} ${item.productName} ${item.productVersion}`.toLowerCase().includes(term))
})
const reportSteps = computed(() => currentCard.value?.steps || [])

function pad(value: number): string { return String(value).padStart(2, '0') }
function dateTimeInput(date: Date): string { return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}` }
function initializeDateRange(): void {
  const now = new Date()
  const start = new Date(now)
  if (now.getHours() < 7 || (now.getHours() === 7 && now.getMinutes() < 30)) start.setDate(start.getDate() - 1)
  start.setHours(7, 30, 0, 0)
  filters.value.start = dateTimeInput(start)
  filters.value.end = dateTimeInput(now)
}
function parseDate(value: string): number { return value ? new Date(value.replace(/\//g, '-').replace(' ', 'T')).getTime() : Number.NaN }
function statusLabel(status: number): string { return status === 0 ? 'Queue' : status === 1 ? 'Running' : status === 4 ? 'Complete' : status === 11 ? 'Inventory' : String(status) }
function countFor(lot: Lot): number { return storedCounts.value[lot.lotNo] ?? lot.printCount }
function readStorage(): void {
  if (!dev.value) return
  try {
    printerName.value = window.localStorage.getItem(printerKey) || 'DEV Default Production Printer'
    const raw = window.localStorage.getItem(printCountKey)
    if (raw) {
      const value = JSON.parse(raw) as Record<string, number>
      if (value && typeof value === 'object') storedCounts.value = value
    }
  } catch { printerName.value = 'DEV Default Production Printer'; storedCounts.value = {} }
}
function query(): void {
  errorText.value = ''
  warningText.value = ''
  if (!filters.value.moNo.trim() && !filters.value.useCreateRange) {
    errorText.value = '請輸入 MO No，或勾選 WO Create 日期區間。'
    return
  }
  const start = filters.value.useCreateRange ? parseDate(filters.value.start) : Number.NaN
  const end = filters.value.useCreateRange ? parseDate(filters.value.end) : Number.NaN
  if (filters.value.useCreateRange && (!Number.isFinite(start) || !Number.isFinite(end))) { errorText.value = 'Start Date 與 End Date 不可空白。'; return }
  if (filters.value.useCreateRange && end <= start) { errorText.value = 'Start Date 必須早於 End Date。'; return }
  queried.value = true
  if (!dev.value) { rows.value = []; activeLotNo.value = ''; selectedLotNos.value = []; return }
  rows.value = sampleLots.filter(lot => lot.lotState !== 0 &&
    (!filters.value.moNo.trim() || lot.moNo.toUpperCase() === filters.value.moNo.trim().toUpperCase()) &&
    (!filters.value.useCreateRange || (parseDate(lot.moCreateDate.replace(' ', 'T')) >= start && parseDate(lot.moCreateDate.replace(' ', 'T')) <= end)))
    .sort((left, right) => right.hotLot - left.hotLot || left.priority - right.priority || left.lotNo.localeCompare(right.lotNo))
  activeLotNo.value = rows.value[0]?.lotNo || ''
  selectedLotNos.value = []
  statusText.value = `Rows: ${rows.value.length}`
}
function toggleSelected(lot: Lot, checked: boolean): void {
  activeLotNo.value = lot.lotNo
  if (checked && !selectedLotNos.value.includes(lot.lotNo)) selectedLotNos.value = [...selectedLotNos.value, lot.lotNo]
  if (!checked) selectedLotNos.value = selectedLotNos.value.filter(no => no !== lot.lotNo)
}
function activate(lot: Lot): void { activeLotNo.value = lot.lotNo }
function showMoLookup(): void { moLookupOpen.value = true }
function chooseMo(moNo: string): void { filters.value.moNo = moNo; moLookupOpen.value = false }
function makeCard(lot: Lot): ReportCard {
  let steps = lot.netFlow ? commonSteps : scope.value === 'original' ? lot.originalSteps : lot.currentSteps
  if (scope.value === 'current' && lot.processNo !== lot.mainProcessNo && !lot.netFlow) {
    steps = [...lot.currentSteps.map(step => ({ ...step, opNo: step.opNo + '*' })), ...lot.resumeSteps]
  }
  const unique = new Set<string>()
  return { lot, productNo: lot.productNo, productVersion: lot.productVersion, productName: lot.productName, emptyVersion: false, steps: steps.filter(step => { if (unique.has(step.opNo.replace(/\*$/, ''))) return false; unique.add(step.opNo.replace(/\*$/, '')); return true }) }
}
function openReport(mode: 'preview' | 'print'): void {
  errorText.value = ''
  warningText.value = ''
  if (!activeLot.value) { errorText.value = '請先選取批號。'; return }
  if (!selectedLotNos.value.includes(activeLot.value.lotNo)) selectedLotNos.value = [...selectedLotNos.value, activeLot.value.lotNo]
  if (activeReportStyle === 0) {
    const workOrders = [...new Set(selectedRows.value.map(item => item.moNo))]
    if (workOrders.length > 1) { errorText.value = '所選批號屬於多張工單，無法合併列印，請重新選取。'; return }
  }
  const resignedRoles = ['DEV 品管主管（示範：離職）']
  warningText.value = `生管／品管／產品管理人員中有資料需重新確認：${resignedRoles.join('、')}`
  const targets = selectedRows.value
  reportCards.value = targets.map(makeCard)
  reportIndex.value = 0
  reportMode.value = mode
  if (mode === 'print' && dev.value) {
    for (const lot of targets) storedCounts.value[lot.lotNo] = countFor(lot) + 1
    window.localStorage.setItem(printCountKey, JSON.stringify(storedCounts.value))
  }
  reportOpen.value = true
}
function choosePrinter(name: string): void {
  printerName.value = name
  printerOpen.value = false
  if (dev.value) window.localStorage.setItem(printerKey, name)
}
function openProductPicker(): void { productSearch.value = ''; productLookupOpen.value = true }
function chooseProduct(product: ProductOption): void {
  productLookupOpen.value = false
  reportCards.value = [{ lot: null, productNo: product.productNo, productVersion: product.productVersion, productName: product.productName, emptyVersion: true, steps: commonSteps.map(item => ({ ...item, status: 'Template' })) }]
  reportIndex.value = 0
  reportMode.value = 'empty'
  reportOpen.value = true
}
onMounted(() => { initializeDateRange(); readStorage() })
</script>

<template>
  <section class="wp113-page" aria-label="LOT SHEET 列印">
    <div v-if="!dev" class="notice">目前為唯讀預覽；切換至 DEV 假資料模式後可操作示範 Lot。</div>
    <div v-if="errorText" class="notice notice-error" role="alert">{{ errorText }}</div>
    <div v-if="warningText" class="notice notice-warning" role="status">{{ warningText }}</div>

    <fieldset class="frame query-frame"><legend>Query</legend>
      <div class="query-layout">
        <label for="wp113-mo">MO No</label>
        <div class="mo-entry"><input id="wp113-mo" v-model.trim="filters.moNo" class="mes-input" :disabled="!dev" @input="filters.moNo = filters.moNo.toUpperCase()" @keydown.enter.prevent="query"><button type="button" class="mes-button lookup" :disabled="!dev" @click="showMoLookup">...</button></div>
        <button type="button" class="mes-button query-button" :disabled="!dev" @click="query">Query</button>
        <label class="range-check"><input v-model="filters.useCreateRange" type="checkbox" :disabled="!dev"> WO Create</label>
        <label for="wp113-start">From</label><input id="wp113-start" v-model="filters.start" type="datetime-local" step="1" class="mes-input date-input" :disabled="!dev || !filters.useCreateRange">
        <label for="wp113-end" class="to-label">To</label><input id="wp113-end" v-model="filters.end" type="datetime-local" step="1" class="mes-input date-input" :disabled="!dev || !filters.useCreateRange">
      </div>
    </fieldset>

    <fieldset class="frame printer-frame"><legend>SetUpPrinter</legend><label for="wp113-printer">PrinterName</label><input id="wp113-printer" class="mes-input printer-input" :value="printerName" disabled><button type="button" class="mes-button lookup" :disabled="!dev" aria-label="選擇印表機" @click="printerOpen = true">...</button></fieldset>

    <fieldset class="frame lot-frame"><legend>Lot State</legend>
      <div class="lot-layout">
        <div class="grid-main">
          <div class="scope-row"><label><input v-model="scope" type="radio" value="current" :disabled="!dev"> Current Process</label><label><input v-model="scope" type="radio" value="original" :disabled="!dev"> Original Process</label></div>
          <div class="table-scroll"><table class="lot-grid"><thead><tr><th class="select-col"></th><th>LotNo</th><th>Status</th><th>AreaNo</th><th>PSNo</th><th>OPNo</th><th>MONo</th><th>CustomerLotNo</th><th>ProductNo</th><th>CurQty</th><th>Print_Count</th></tr></thead><tbody>
            <tr v-for="lot in rows" :key="lot.lotNo" :class="{ active: activeLotNo === lot.lotNo, selected: selectedLotNos.includes(lot.lotNo) }" tabindex="0" @click="activate(lot)" @keydown.enter="activate(lot)">
              <td class="select-col"><input type="checkbox" :checked="selectedLotNos.includes(lot.lotNo)" :disabled="!dev" :aria-label="`選取 ${lot.lotNo}`" @click.stop @change="toggleSelected(lot, ($event.target as HTMLInputElement).checked)"></td><td>{{ lot.lotNo }}</td><td>{{ statusLabel(lot.status) }}</td><td>{{ lot.areaNo }}</td><td>{{ lot.psNo }}</td><td>{{ lot.opNo }}</td><td>{{ lot.moNo }}</td><td>{{ lot.customerLotNo }}</td><td>{{ lot.productNo }}</td><td class="number">{{ lot.curQty }}</td><td class="number">{{ countFor(lot) }}</td>
            </tr><tr v-if="rows.length === 0"><td colspan="11" class="empty">{{ queried ? '查無資料' : '輸入 MO No 或勾選 WO Create 後按 Query' }}</td></tr>
          </tbody></table></div>
          <div class="statusbar"><span>{{ statusText }}</span><span>Rows: {{ rows.length }}</span><span>Selected: {{ selectedLotNos.length }}</span></div>
        </div>
        <aside class="actions"><button type="button" class="mes-button" :disabled="!dev" @click="openReport('preview')">Preview(&amp;V)</button><button type="button" class="mes-button" :disabled="!dev" @click="openReport('print')">Print(&amp;P)</button><button type="button" class="mes-button" :disabled="!dev" @click="openProductPicker">Print Empty Version</button><span class="spacer"></span><button type="button" class="mes-button" @click="emit('close')">Close (&amp;X)</button></aside>
      </div>
    </fieldset>

    <div v-if="moLookupOpen" class="overlay" @click.self="moLookupOpen = false"><section class="dialog" role="dialog" aria-modal="true" aria-label="MO No lookup"><header><strong>MO No</strong><button type="button" class="mes-button" @click="moLookupOpen = false">Close</button></header><button v-for="mo in [...new Set(sampleLots.map(item => item.moNo))]" :key="mo" type="button" class="lookup-row" @click="chooseMo(mo)">{{ mo }}<small>DEV MO</small></button></section></div>
    <div v-if="productLookupOpen" class="overlay" @click.self="productLookupOpen = false"><section class="dialog" role="dialog" aria-modal="true" aria-label="選擇產品"><header><strong>Product Basis</strong><button type="button" class="mes-button" @click="productLookupOpen = false">Close</button></header><input v-model.trim="productSearch" class="mes-input" placeholder="ProductNo / ProductName"><button v-for="product in productResults" :key="`${product.productNo}-${product.productVersion}`" type="button" class="lookup-row" @click="chooseProduct(product)"><strong>{{ product.productNo }} / {{ product.productVersion }}</strong><small>{{ product.productName }}</small></button></section></div>
    <div v-if="printerOpen" class="overlay" @click.self="printerOpen = false"><section class="dialog" role="dialog" aria-modal="true" aria-label="選擇印表機"><header><strong>Printer</strong><button type="button" class="mes-button" @click="printerOpen = false">Close</button></header><button v-for="printer in fakePrinters" :key="printer" type="button" class="lookup-row" @click="choosePrinter(printer)">{{ printer }}</button></section></div>

    <div v-if="reportOpen && currentCard" class="overlay report-overlay" @click.self="reportOpen = false"><section class="dialog report-dialog" role="dialog" aria-modal="true" aria-label="LOT SHEET preview"><header><div><strong>{{ currentCard.emptyVersion ? 'LOT SHEET Empty Version' : reportMode === 'print' ? 'LOT SHEET Print Preview' : 'LOT SHEET Preview' }}</strong><span>{{ reportIndex + 1 }} / {{ reportCards.length }}</span></div><button type="button" class="mes-button" @click="reportOpen = false">Close</button></header>
      <div class="paper"><div class="paper-title">LOT SHEET</div><div class="report-fields">
        <span><b>LotNo</b>{{ currentCard.lot?.lotNo || 'EMPTY VERSION' }}</span><span><b>MO No</b>{{ currentCard.lot?.moNo || '—' }}</span>
        <span><b>Product</b>{{ currentCard.productNo }} / {{ currentCard.productVersion }}</span><span><b>Product Name</b>{{ currentCard.productName }}</span>
        <span><b>MO Type</b>{{ currentCard.lot?.moType || '—' }}</span><span><b>CustomerLotNo</b>{{ currentCard.lot?.customerLotNo || '—' }}</span>
        <span><b>BigQty / SortQty</b>{{ currentCard.lot ? `${currentCard.lot.bigQty} / ${currentCard.lot.sortQty}` : '—' }}</span><span><b>Lot Qty</b>{{ currentCard.lot?.curQty ?? '—' }}</span>
        <span><b>Inspection No</b>{{ currentCard.lot?.inspectionNo || '—' }}</span><span><b>CIMLotNo</b>{{ currentCard.lot?.cimLotNo || '—' }}</span>
        <span><b>Plan Finish</b>{{ currentCard.lot?.finishDate || '—' }}</span><span><b>Print Count</b>{{ currentCard.lot ? countFor(currentCard.lot) : '—' }}</span>
      </div><table class="report-grid"><thead><tr><th>ItemNo</th><th>OPNo</th><th>OPName</th><th>Spec / SOP</th><th>Recipe Matrix</th><th>Remark</th><th>Status</th></tr></thead><tbody><tr v-for="(step, index) in reportSteps" :key="`${index}-${step.opNo}`"><td>{{ index + 1 }}</td><td>{{ step.opNo }}</td><td>{{ step.opName }}</td><td>{{ step.spec }}</td><td>{{ step.recipe }}</td><td>{{ step.remark }}</td><td>{{ step.status }}</td></tr><tr v-if="reportSteps.length === 0"><td colspan="7" class="empty">無流程站資料</td></tr></tbody></table>
        <div class="personnel">PMC：DEV 生管　QC：DEV 品管　Product：DEV 產品工程</div><div class="disclaimer">DEV 範例報表；未連線正式資料，也未送出印表機工作。</div></div>
      <footer><button type="button" class="mes-button" :disabled="reportIndex === 0" @click="reportIndex--">Previous Lot</button><button type="button" class="mes-button" :disabled="reportIndex >= reportCards.length - 1" @click="reportIndex++">Next Lot</button><span class="spacer"></span><button type="button" class="mes-button" @click="reportOpen = false">Close</button></footer>
    </section></div>
  </section>
</template>

<style scoped>
.wp113-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:6px;padding:6px;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif;box-sizing:border-box}.notice{padding:7px 10px;border:1px solid #d7c790;background:#fff8dd;color:#5c4a16}.notice-error{border-color:#d8a7a2;background:#fff1ef;color:#8f261f}.notice-warning{border-color:#d7c790;background:#fff8dd;color:#765b16}.frame{min-width:0;margin:0;padding:8px 10px 10px;border:1px solid #bdbdbd;background:#fff;box-sizing:border-box}.frame legend{padding:0 5px}.query-frame{height:122px;flex:0 0 122px}.query-layout{display:grid;grid-template-columns:105px 205px 32px 24px 52px 160px 32px 52px 160px 1fr;grid-template-rows:32px 32px;align-items:center;gap:4px 7px}.query-layout>label:first-child{text-align:right}.mo-entry{display:flex;gap:3px}.mo-entry input{flex:1}.range-check{grid-column:1 / 2;grid-row:2;text-align:right;display:flex;justify-content:flex-end;align-items:center;gap:3px}.query-layout label[for="wp113-start"]{grid-column:2;grid-row:2;text-align:left;margin-left:4px}.query-layout #wp113-start{grid-column:3 / 7;grid-row:2;width:160px}.to-label{grid-column:7;grid-row:2}.query-layout #wp113-end{grid-column:8 / 10;grid-row:2;width:160px}.mes-input{height:24px;min-width:0;border:1px solid #8c8c8c;background:#fff3bd;padding:2px 6px;box-sizing:border-box;color:#333}.mes-input:disabled{background:#eee;color:#777}.mes-button{min-height:27px;padding:3px 11px;border:1px solid #b7b7b7;border-radius:3px;background:linear-gradient(#fff,#f5f5f5);color:#333;white-space:nowrap;cursor:pointer}.mes-button:hover:not(:disabled){border-color:#888;background:#fff8e8}.mes-button:disabled{color:#999;cursor:default}.lookup{width:30px;min-width:30px;padding:2px}.query-button{grid-column:10;grid-row:2;justify-self:end;width:84px;height:31px}.date-input{width:160px}.printer-frame{height:50px;flex:0 0 50px;display:flex;align-items:center;gap:7px}.printer-frame label{width:108px;text-align:right}.printer-input{width:min(300px,45vw)}.lot-frame{display:flex;flex:1 1 auto;min-height:180px;flex-direction:column}.lot-layout{display:flex;gap:8px;flex:1;min-height:0}.grid-main{display:flex;flex:1;min-width:0;min-height:0;flex-direction:column}.scope-row{height:31px;flex:0 0 31px;display:flex;align-items:center;gap:18px}.scope-row label{display:flex;align-items:center;gap:4px}.scope-row input{accent-color:#1674cc}.table-scroll{flex:1;min-height:0;overflow:auto;border:1px solid #b7b7b7}.lot-grid,.report-grid{width:100%;border-collapse:collapse;white-space:nowrap}.lot-grid th,.report-grid th{position:sticky;top:0;z-index:1;padding:5px 7px;border:1px solid #b7b7b7;background:#dfeaf2;color:#333;text-align:left;font-weight:600}.lot-grid td,.report-grid td{height:27px;padding:3px 7px;border:1px solid #c8c8c8;background:#fff3bd}.lot-grid tbody tr{cursor:pointer}.lot-grid tbody tr.active td{outline:1px solid #ca9b31;outline-offset:-2px}.lot-grid tbody tr.selected td{background:#ffe7a0}.select-col{width:26px;text-align:center}.lot-grid input{accent-color:#d38d22}.number{text-align:right}.empty{text-align:center!important;color:#777!important;background:#fff!important;height:44px!important}.statusbar{height:24px;flex:0 0 24px;display:flex;align-items:center;gap:20px;padding:0 4px;color:#555;border-bottom:1px solid #c7c7c7}.actions{display:flex;flex:0 0 96px;flex-direction:column;gap:7px;padding-top:33px}.actions .mes-button{width:96px;height:31px;padding:3px 5px}.spacer{flex:1}.overlay{position:fixed;inset:0;z-index:1600;display:grid;place-items:center;padding:20px;background:#0006}.dialog{width:min(620px,94vw);max-height:min(680px,90vh);display:flex;flex-direction:column;gap:10px;padding:14px;background:#fff;border:1px solid #888;box-shadow:0 8px 26px #0004;box-sizing:border-box}.dialog>header,.dialog>footer{display:flex;align-items:center;justify-content:space-between;gap:8px}.dialog header div{display:flex;align-items:center;gap:12px}.lookup-row{width:100%;display:flex;justify-content:space-between;align-items:center;padding:9px 10px;border:0;border-bottom:1px solid #ddd;background:#fff;text-align:left;color:#333;cursor:pointer}.lookup-row:hover{background:#fff2cc}.lookup-row small{color:#666}.report-overlay{z-index:1700}.report-dialog{width:min(1180px,96vw);height:min(800px,92vh);max-height:92vh}.paper{flex:1;overflow:auto;padding:22px;border:1px solid #aaa;background:#fff}.paper-title{text-align:center;font-size:22px;font-weight:700;letter-spacing:4px;margin-bottom:18px}.report-fields{display:grid;grid-template-columns:1fr 1fr;gap:7px 20px;padding-bottom:15px;border-bottom:1px solid #888}.report-fields span{display:flex;gap:7px}.report-fields b{width:115px}.report-grid{margin-top:15px}.report-grid th{position:static}.report-grid td{white-space:normal}.personnel{margin-top:22px;padding:12px 0;border-top:1px solid #888}.disclaimer{padding:8px;background:#fff7d8;color:#765b16}@media(max-width:950px){.query-layout{grid-template-columns:80px minmax(140px,190px) 28px 18px 45px 145px 25px 42px 145px 1fr;gap:4px}.scope-row{gap:8px;font-size:12px}.actions{flex-basis:82px}.actions .mes-button{width:82px}.lot-grid{font-size:11px}}
</style>
