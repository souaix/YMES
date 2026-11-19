<script setup lang="ts">
import { computed, ref } from 'vue'

type LotStatus = 0 | 1 | 2 | 3 | 4 | 5
type ScanField = 'lotNo' | 'carrierNo' | 'componentNo' | 'batchSerial'
type FilterField = 'areaNo' | 'psNo' | 'opNo' | 'lotNo' | 'moNo'
type SimulatedOutcome = 'success' | 'future-wait' | 'future-hold' | 'future-os' | 'stop'

interface LotRow {
  lotNo: string
  moNo: string
  hotLot: boolean
  priority: number
  status: LotStatus
  productNo: string
  productName: string
  customerName: string
  productVersion: string
  areaNo: string
  psNo: string
  processNo: string
  opNo: string
  opName: string
  brNo: string
  curQty: number
  curUnitNo: string
  sysQty: number
  sysUnitNo: string
  opGroupNo: string
  lotStamp: number
  ruleMethod: 'automatic' | 'manual'
  nextOpNo: string
  nextOpName: string
  componentNo: string
  batchSerial: string
  carrierNo: string
  carrierPosition: number
  simulatedOutcome: SimulatedOutcome
}

interface ExecutionRecord { lotNo: string; time: string; message: string; status: LotStatus }
interface DevState { version: 1; lots: LotRow[]; records: ExecutionRecord[] }

const props = defineProps<{ currentUserNo?: string; developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const storageKey = 'ymes.dev.wp201-lot-execution.v1'
const statusNames: Record<LotStatus, string> = { 0: 'Queue', 1: 'Running', 2: 'Wait', 3: 'Hold', 4: 'Complete', 5: 'OS' }
const employeeNo = ref(props.currentUserNo || 'DEV001')
const now = () => new Date().toISOString()
const qty = (value: number) => Number.isFinite(value) ? value.toFixed(4).replace(/0+$/, '').replace(/\.$/, '') : ''

function seedLots(): LotRow[] {
  const make = (overrides: Partial<LotRow> & Pick<LotRow, 'lotNo' | 'moNo' | 'status' | 'areaNo' | 'psNo' | 'opNo'>): LotRow => ({
    lotNo: overrides.lotNo, moNo: overrides.moNo, hotLot: false, priority: 50, status: overrides.status,
    productNo: 'DEV-PRD-201-01', productName: 'DEV 示範產品 A', customerName: 'DEV 客戶一', productVersion: '01',
    areaNo: overrides.areaNo, psNo: overrides.psNo, processNo: 'DEV-PROC-01', opNo: overrides.opNo,
    opName: 'DEV 作業站 01', brNo: 'DEV-BR-AUTO', curQty: 25, curUnitNo: 'PCS', sysQty: 25, sysUnitNo: 'PCS',
    opGroupNo: 'DEV-OPG-01', lotStamp: 1, ruleMethod: 'automatic', nextOpNo: 'DEV-OP-02', nextOpName: 'DEV 作業站 02',
    componentNo: '', batchSerial: '', carrierNo: '', carrierPosition: 0, simulatedOutcome: 'success', ...overrides,
  })
  return [
    make({ lotNo: 'DEV-WP201-000001', moNo: 'DEV-MO-201-001', status: 0, areaNo: 'DEV-AREA-01', psNo: 'DEV-PS-01', opNo: 'DEV-OP-01', hotLot: true, priority: 10, componentNo: 'DEV-COMP-0001', batchSerial: 'DEV-BATCH-001', carrierNo: 'DEV-CAR-01' }),
    make({ lotNo: 'DEV-WP201-000002', moNo: 'DEV-MO-201-001', status: 1, areaNo: 'DEV-AREA-01', psNo: 'DEV-PS-01', opNo: 'DEV-OP-02', processNo: 'DEV-PROC-02', opName: 'DEV 作業站 02', brNo: 'DEV-BR-MANUAL', ruleMethod: 'manual', nextOpNo: 'DEV-OP-03', nextOpName: 'DEV 作業站 03', componentNo: 'DEV-COMP-0002', batchSerial: 'DEV-BATCH-001', carrierNo: 'DEV-CAR-01', carrierPosition: 1 }),
    make({ lotNo: 'DEV-WP201-000003', moNo: 'DEV-MO-201-002', status: 0, areaNo: 'DEV-AREA-02', psNo: 'DEV-PS-02', opNo: 'DEV-OP-01', priority: 20, ruleMethod: 'manual', brNo: 'DEV-BR-MANUAL', productNo: 'DEV-PRD-201-02', productName: 'DEV 示範產品 B', productVersion: '02', customerName: 'DEV 客戶二', curQty: 12.5, sysQty: 12.5, componentNo: 'DEV-COMP-0003', batchSerial: 'DEV-BATCH-002', carrierNo: 'DEV-CAR-02' }),
    make({ lotNo: 'DEV-WP201-000004', moNo: 'DEV-MO-201-002', status: 0, areaNo: 'DEV-AREA-02', psNo: 'DEV-PS-02', opNo: 'DEV-OP-02', priority: 30, simulatedOutcome: 'future-wait', componentNo: 'DEV-COMP-0004', batchSerial: 'DEV-BATCH-002', carrierNo: 'DEV-CAR-02', carrierPosition: 1 }),
    make({ lotNo: 'DEV-WP201-000005', moNo: 'DEV-MO-201-003', status: 2, areaNo: 'DEV-AREA-03', psNo: 'DEV-PS-03', opNo: 'DEV-OP-03', brNo: 'DEV-BR-FUTURE-WAIT', simulatedOutcome: 'future-wait' }),
    make({ lotNo: 'DEV-WP201-000006', moNo: 'DEV-MO-201-003', status: 3, areaNo: 'DEV-AREA-03', psNo: 'DEV-PS-03', opNo: 'DEV-OP-01', hotLot: true, priority: 5, brNo: 'DEV-BR-FUTURE-HOLD', simulatedOutcome: 'future-hold' }),
    make({ lotNo: 'DEV-WP201-000007', moNo: 'DEV-MO-201-004', status: 4, areaNo: 'DEV-AREA-01', psNo: 'DEV-PS-01', opNo: 'DEV-OP-04', brNo: 'PASS' }),
    make({ lotNo: 'DEV-WP201-000008', moNo: 'DEV-MO-201-005', status: 5, areaNo: 'DEV-AREA-02', psNo: 'DEV-PS-02', opNo: 'DEV-OP-03', brNo: 'OS', simulatedOutcome: 'future-os' }),
    make({ lotNo: 'DEV-WP201-000009', moNo: 'DEV-MO-201-005', status: 0, areaNo: 'DEV-AREA-01', psNo: 'DEV-PS-01', opNo: 'DEV-OP-01', priority: 40, productNo: 'DEV-PRD-201-03', productName: 'DEV 示範產品 C', productVersion: 'A', customerName: 'DEV 客戶三', curQty: 48.5, sysQty: 48.5, simulatedOutcome: 'stop' }),
    make({ lotNo: 'DEV-WP201-000010', moNo: 'DEV-MO-201-006', status: 0, areaNo: 'DEV-AREA-04', psNo: 'DEV-PS-04', opNo: 'DEV-OP-01', priority: 60, nextOpNo: '', nextOpName: '', curQty: 5, sysQty: 5 }),
  ]
}

function readState(): DevState {
  const initial: DevState = { version: 1, lots: seedLots(), records: [] }
  if (!dev.value) return initial
  try {
    const value = JSON.parse(localStorage.getItem(storageKey) || 'null') as Partial<DevState> | null
    if (value?.version === 1 && Array.isArray(value.lots) && Array.isArray(value.records))
      return { version: 1, lots: value.lots as LotRow[], records: value.records as ExecutionRecord[] }
  } catch { /* invalid DEV snapshot falls back to seeded data */ }
  return initial
}

const initialState = readState()
const lots = ref<LotRow[]>(initialState.lots)
const records = ref<ExecutionRecord[]>(initialState.records)
const filters = ref<Record<FilterField, string>>({ areaNo: '', psNo: '', opNo: '', lotNo: '', moNo: '' })
const hasQueried = ref(false)
const selectedLotNo = ref('')
const scan = ref<Record<ScanField, string>>({ lotNo: '', carrierNo: '', componentNo: '', batchSerial: '' })
const requirePassword = ref(false)
const password = ref('')
const carrierScanAllowed = ref(true)
const sampleEnterprise = ref(false)
const showDevSettings = ref(false)
const message = ref('')
const error = ref('')
const pendingLotNo = ref('')
const automaticDialog = ref(false)
const manualDialog = ref(false)
const lookupField = ref<FilterField | null>(null)
const detailLot = ref<LotRow | null>(null)
const orderLot = ref<LotRow | null>(null)

const filteredLots = computed(() => {
  if (!hasQueried.value) return []
  return lots.value
    .filter(row => (Object.keys(filters.value) as FilterField[]).every(key => !filters.value[key].trim() || row[key].toUpperCase() === filters.value[key].trim().toUpperCase()))
    .filter(row => row.status >= 0 && row.status <= 5)
    .sort((a, b) => Number(b.hotLot) - Number(a.hotLot) || a.priority - b.priority || a.lotNo.localeCompare(b.lotNo))
})
const pendingLot = computed(() => lots.value.find(row => row.lotNo === pendingLotNo.value) ?? null)
const lookupOptions = computed(() => {
  switch (lookupField.value) {
    case 'areaNo': return ['DEV-AREA-01', 'DEV-AREA-02', 'DEV-AREA-03', 'DEV-AREA-04']
    case 'psNo': return ['DEV-PS-01', 'DEV-PS-02', 'DEV-PS-03', 'DEV-PS-04']
    case 'opNo': return [...new Set(lots.value.map(row => row.opNo))].filter(op => !filters.value.psNo || lots.value.some(row => row.psNo === filters.value.psNo && row.opNo === op))
    case 'lotNo': return lots.value.map(row => row.lotNo)
    case 'moNo': return [...new Set(lots.value.map(row => row.moNo))]
    default: return []
  }
})

function saveState(nextLots = lots.value, nextRecords = records.value): void {
  if (!dev.value) return
  localStorage.setItem(storageKey, JSON.stringify({ version: 1, lots: nextLots, records: nextRecords } satisfies DevState))
}
function queryLots(): void {
  error.value = ''; message.value = ''
  if (!Object.values(filters.value).some(value => value.trim())) { error.value = '請至少輸入一項查詢條件。'; return }
  hasQueried.value = true
}
function clearQuery(): void {
  filters.value = { areaNo: '', psNo: '', opNo: '', lotNo: '', moNo: '' }
  hasQueried.value = false; selectedLotNo.value = ''; scan.value.lotNo = ''; error.value = ''; message.value = ''
}
function openLookup(field: FilterField): void { lookupField.value = field }
function chooseLookup(value: string): void {
  if (lookupField.value) filters.value[lookupField.value] = value
  lookupField.value = null
}
function statusName(status: LotStatus): string { return statusNames[status] }
function selectRow(row: LotRow): void {
  selectedLotNo.value = row.lotNo
  scan.value.lotNo = row.lotNo
}
function detailFor(row: LotRow): void { detailLot.value = row }
function showOrder(row: LotRow): void { orderLot.value = row }

function resolveScan(): LotRow | null {
  if (scan.value.lotNo.trim())
    return lots.value.find(row => row.lotNo.toUpperCase() === scan.value.lotNo.trim().toUpperCase()) ?? null
  if (scan.value.carrierNo.trim()) {
    const candidates = lots.value.filter(row => row.carrierNo.toUpperCase() === scan.value.carrierNo.trim().toUpperCase())
    return candidates.sort((a, b) => Number(b.status === 1) - Number(a.status === 1) || Number(b.status === 0) - Number(a.status === 0) || a.carrierPosition - b.carrierPosition)[0] ?? null
  }
  if (scan.value.componentNo.trim())
    return lots.value.find(row => row.componentNo && row.componentNo.toUpperCase() === scan.value.componentNo.trim().toUpperCase()) ?? null
  if (scan.value.batchSerial.trim())
    return lots.value.find(row => row.batchSerial && row.batchSerial.toUpperCase() === scan.value.batchSerial.trim().toUpperCase()) ?? null
  return null
}
function executeScan(): void {
  error.value = ''; message.value = ''
  if (!scan.value.lotNo.trim() && !scan.value.carrierNo.trim() && !scan.value.componentNo.trim() && !scan.value.batchSerial.trim()) {
    error.value = '請輸入 Lot No、Component No、Carrier No 或 Batch Serial。'; return
  }
  if (!employeeNo.value.trim()) { error.value = 'Employee No 不可空白。'; return }
  if (requirePassword.value && !password.value.trim()) { error.value = '目前 DEV 模擬設定要求輸入 Password。'; return }
  const scanField: ScanField = scan.value.lotNo.trim() ? 'lotNo' : scan.value.carrierNo.trim() ? 'carrierNo' : scan.value.componentNo.trim() ? 'componentNo' : 'batchSerial'
  if (scanField === 'carrierNo' && !carrierScanAllowed.value) { error.value = 'DEV 系統參數 AllowExecLotByCarrier 未啟用。'; return }
  if (scanField === 'batchSerial' && !sampleEnterprise.value) { error.value = 'Batch Serial 僅在 SAMPLE Enterprise 顯示。'; return }
  const row = resolveScan()
  if (!row) { error.value = 'DEV 假資料中找不到對應的 Lot／Component／Carrier／Batch Serial。'; return }
  pendingLotNo.value = row.lotNo
  selectRow(row)
  if (row.status === 2) { error.value = `Lot ${row.lotNo} 目前為 Wait，需先完成等待處置。`; return }
  if (row.status === 3) { error.value = `Lot ${row.lotNo} 目前為 Hold，需先解除異常。`; return }
  if (row.status === 5) { error.value = `Lot ${row.lotNo} 為 OS，請使用臨時外包功能。`; return }
  if (row.status === 4 && row.brNo.toUpperCase() === 'HOLD') { error.value = `Lot ${row.lotNo} 完成狀態仍連結 HOLD，需先處理異常單。`; return }
  if (row.ruleMethod === 'automatic' && row.status === 0) automaticDialog.value = true
  else manualDialog.value = true
}
function finishSimulation(): void {
  const current = pendingLot.value
  if (!current) { automaticDialog.value = false; manualDialog.value = false; error.value = '選取的假 Lot 已不存在。'; return }
  const nextLots = lots.value.map(row => ({ ...row }))
  const updated = nextLots.find(row => row.lotNo === current.lotNo)!
  let outcomeMessage = ''
  let lotChanged = true
  if (updated.simulatedOutcome === 'future-wait') {
    updated.status = 2; updated.brNo = 'DEV-BR-FUTURE-WAIT'; outcomeMessage = 'DEV 模擬回傳 Future Wait；批次狀態已標示 Wait。'
  } else if (updated.simulatedOutcome === 'future-hold') {
    updated.status = 3; updated.brNo = 'DEV-BR-FUTURE-HOLD'; outcomeMessage = 'DEV 模擬回傳 Future Hold；批次狀態已標示 Hold。'
  } else if (updated.simulatedOutcome === 'future-os') {
    error.value = 'DEV 模擬回傳 Future OS；請改用臨時外包功能。'; automaticDialog.value = false; manualDialog.value = false; return
  } else if (updated.simulatedOutcome === 'stop') {
    lotChanged = false; outcomeMessage = 'DEV 模擬回傳 StopFlag；流程停止，Lot 狀態不變。'
  } else if (updated.status === 0) {
    updated.status = 1; outcomeMessage = 'DEV 模擬完成 Trigger OP：Lot 由 Queue 轉為 Running。'
  } else if (updated.nextOpNo) {
    updated.status = 0; updated.opNo = updated.nextOpNo; updated.opName = updated.nextOpName; updated.processNo = 'DEV-PROC-' + updated.nextOpNo.slice(-2)
    updated.brNo = 'DEV-BR-AUTO'; updated.ruleMethod = 'automatic'; outcomeMessage = `DEV 模擬轉站至 ${updated.opNo}：Lot 回到 Queue。`
  } else {
    updated.status = 4; updated.brNo = 'PASS'; outcomeMessage = 'DEV 模擬完成最後站：Lot 狀態為 Complete。'
  }
  if (lotChanged) updated.lotStamp += 1
  const record: ExecutionRecord = { lotNo: updated.lotNo, time: now(), message: outcomeMessage, status: updated.status }
  const nextRecords = [record, ...records.value].slice(0, 20)
  try {
    saveState(nextLots, nextRecords)
    lots.value = nextLots; records.value = nextRecords
    message.value = `${updated.lotNo}：${outcomeMessage}`
  } catch { error.value = '無法保存 DEV 假資料，請確認瀏覽器允許本機儲存。' }
  automaticDialog.value = false; manualDialog.value = false
  scan.value = { lotNo: '', carrierNo: '', componentNo: '', batchSerial: '' }; password.value = ''; employeeNo.value = ''
}
</script>

<template>
  <section v-if="dev" class="execution-page">
    <header class="page-head"><div><small>在製品管理 / 生產報工 / WP201</small><h2>生產批執行 <span class="dev-tag">DEV 假資料</span></h2></div><button type="button" @click="emit('close')">關閉</button></header>
    <section class="panel execute-panel">
      <h3>Execute</h3>
      <div class="execute-grid">
        <label>Employee No<input v-model.trim="employeeNo" class="required" @keydown.enter.prevent="executeScan()"></label>
        <label v-if="requirePassword">Password<input v-model="password" type="password" autocomplete="off" @keydown.enter.prevent="executeScan()"></label>
        <label>Lot No<input v-model.trim="scan.lotNo" class="required" @keydown.enter.prevent="executeScan()"></label>
        <label>Component No<input v-model.trim="scan.componentNo" @keydown.enter.prevent="executeScan()"></label>
        <label v-if="carrierScanAllowed">Carrier No<input v-model.trim="scan.carrierNo" @keydown.enter.prevent="executeScan()"></label>
        <label v-if="sampleEnterprise">Batch Serial<input v-model.trim="scan.batchSerial" @keydown.enter.prevent="executeScan()"></label>
      </div>
      <details v-if="dev" class="dev-settings"><summary>DEV 系統參數預覽</summary><div>
        <label><input v-model="requirePassword" type="checkbox">WIPChkPassword = 1</label>
        <label><input v-model="carrierScanAllowed" type="checkbox">AllowExecLotByCarrier = 1</label>
        <label><input v-model="sampleEnterprise" type="checkbox">EnterpriseNo = SAMPLE（顯示 Batch Serial）</label>
      </div></details>
    </section>

    <section class="panel query-panel">
      <h3>Query</h3>
      <div class="filter-grid">
        <label>Area No<span class="lookup"><input v-model.trim="filters.areaNo" @keydown.enter.prevent="queryLots"><button type="button" @click="openLookup('areaNo')">...</button></span></label>
        <label>PS No<span class="lookup"><input v-model.trim="filters.psNo" @keydown.enter.prevent="queryLots"><button type="button" @click="openLookup('psNo')">...</button></span></label>
        <label>OP No<span class="lookup"><input v-model.trim="filters.opNo" @keydown.enter.prevent="queryLots"><button type="button" @click="openLookup('opNo')">...</button></span></label>
        <label>Lot No<span class="lookup"><input v-model.trim="filters.lotNo" @keydown.enter.prevent="queryLots"><button type="button" @click="openLookup('lotNo')">...</button></span></label>
        <label>MO No<span class="lookup"><input v-model.trim="filters.moNo" @keydown.enter.prevent="queryLots"><button type="button" @click="openLookup('moNo')">...</button></span></label>
        <div class="query-actions"><button class="primary" type="button" @click="queryLots">Query</button><button type="button" @click="clearQuery">Clear</button></div>
      </div>
      <p class="query-hint">至少輸入一項條件；查詢 LotState=1，顯示 Status 0–5，依 HotLot、Priority、LotNo 排序。</p>
    </section>

    <section class="panel lot-state-panel">
      <h3>Lot State</h3>
      <div class="grid"><table><thead><tr>
        <th>LotNo</th><th>MONo</th><th>HotLot</th><th>Priority</th><th>Status</th><th>ProductNo</th><th>ProductName</th><th>CustomerName</th><th>ProductVersion</th><th>AreaNo</th><th>PSNo</th><th>ProcessNo</th><th>OPNo</th><th>OPName</th><th>BRNo</th><th>CurQTY</th><th>CurUnitNo</th><th>SysQTY</th><th>SysUnitNo</th><th>OPGroupNo</th>
      </tr></thead><tbody>
        <tr v-for="row in filteredLots" :key="row.lotNo" :class="[{selected:row.lotNo===selectedLotNo},'status-'+row.status]" @click="selectRow(row)" @dblclick="selectRow(row)">
          <td><button class="cell-link" type="button" @click.stop="detailFor(row)">{{row.lotNo}}</button></td><td><button class="cell-link" type="button" @click.stop="showOrder(row)">{{row.moNo}}</button></td><td>{{row.hotLot?'Yes':'No'}}</td><td class="number">{{row.priority}}</td><td><span class="status-pill">{{statusName(row.status)}}</span></td><td>{{row.productNo}}</td><td>{{row.productName}}</td><td>{{row.customerName}}</td><td>{{row.productVersion}}</td><td>{{row.areaNo}}</td><td>{{row.psNo}}</td><td>{{row.processNo}}</td><td>{{row.opNo}}</td><td>{{row.opName}}</td><td>{{row.brNo}}</td><td class="number">{{qty(row.curQty)}}</td><td>{{row.curUnitNo}}</td><td class="number">{{qty(row.sysQty)}}</td><td>{{row.sysUnitNo}}</td><td>{{row.opGroupNo}}</td>
        </tr>
        <tr v-if="!filteredLots.length"><td colspan="20" class="empty">{{hasQueried?'查無符合條件的假資料':'輸入查詢條件後按 Query'}}</td></tr>
      </tbody></table></div>
      <div class="grid-footer"><span>資料總筆數：{{filteredLots.length}}</span><span>雙擊 Lot 列可帶入 Lot No；點 LotNo／MONo 查看明細。</span></div>
    </section>

    <p v-if="error" class="message error" role="alert">{{error}}</p><p v-if="message" class="message success" role="status">{{message}}</p>
    <section v-if="records.length" class="panel history-panel"><h3>DEV 執行結果</h3><ul><li v-for="(record,index) in records.slice(0,5)" :key="record.time+index"><b>{{record.lotNo}}</b> · {{statusName(record.status)}} · {{record.message}} <small>{{new Date(record.time).toLocaleString()}}</small></li></ul></section>
    <footer><button class="primary" type="button" @click="executeScan()">Execute (E)</button><button type="button" @click="emit('close')">Close (X)</button></footer>

    <div v-if="lookupField" class="overlay" @click.self="lookupField=null"><section class="dialog" role="dialog" aria-modal="true" aria-label="查詢選擇">
      <h3>選擇 {{lookupField}}</h3><div class="lookup-list"><button v-for="option in lookupOptions" :key="option" type="button" @click="chooseLookup(option)">{{option}}</button><p v-if="!lookupOptions.length" class="empty">沒有可選假資料</p></div><footer><button type="button" @click="lookupField=null">Close</button></footer>
    </section></div>
    <div v-if="automaticDialog&&pendingLot" class="overlay" @click.self="automaticDialog=false"><section class="dialog" role="dialog" aria-modal="true" aria-label="確認自動流程">
      <h3>Automatic Business Rule</h3><p>{{pendingLot.brNo}} 使用自動執行方式，是否繼續？</p><div class="read-fields"><label>Lot No<input :value="pendingLot.lotNo" readonly></label><label>Current OP<input :value="pendingLot.opNo+' / '+pendingLot.opName" readonly></label><label>Employee No<input :value="employeeNo" readonly></label></div><footer><button class="primary" type="button" @click="finishSimulation">Yes</button><button type="button" @click="automaticDialog=false">No</button></footer>
    </section></div>
    <div v-if="manualDialog&&pendingLot" class="overlay" @click.self="manualDialog=false"><section class="dialog" role="dialog" aria-modal="true" aria-label="人工流程">
      <h3>Manual Business Rule <small>DEV 簡化預覽</small></h3><p>WinForms 依 BRNo 載入不同規則 DLL 表單；此頁用單一 DEV 確認視窗呈現流程入口。</p><div class="read-fields"><label>Lot No<input :value="pendingLot.lotNo" readonly></label><label>BR No<input :value="pendingLot.brNo" readonly></label><label>Current OP<input :value="pendingLot.opNo+' / '+pendingLot.opName" readonly></label><label>Employee No<input :value="employeeNo" readonly></label></div><footer><button class="primary" type="button" @click="finishSimulation">DEV 模擬完成</button><button type="button" @click="manualDialog=false">Cancel</button></footer>
    </section></div>
    <div v-if="detailLot" class="overlay" @click.self="detailLot=null"><section class="dialog" role="dialog" aria-modal="true" aria-label="Lot 明細">
      <h3>Lot Detail</h3><div class="read-fields"><label>LotNo<input :value="detailLot.lotNo" readonly></label><label>MONo<input :value="detailLot.moNo" readonly></label><label>Status<input :value="statusName(detailLot.status)" readonly></label><label>LotStamp<input :value="detailLot.lotStamp" readonly></label><label>OPNo<input :value="detailLot.opNo" readonly></label><label>BRNo<input :value="detailLot.brNo" readonly></label></div><footer><button type="button" @click="detailLot=null">Close</button></footer>
    </section></div>
    <div v-if="orderLot" class="overlay" @click.self="orderLot=null"><section class="dialog" role="dialog" aria-modal="true" aria-label="MO 明細">
      <h3>MO Information</h3><div class="read-fields"><label>MONo<input :value="orderLot.moNo" readonly></label><label>Product<input :value="orderLot.productNo+' / '+orderLot.productVersion" readonly></label><label>Customer<input :value="orderLot.customerName" readonly></label><label>Current Lot<input :value="orderLot.lotNo" readonly></label></div><footer><button type="button" @click="orderLot=null">Close</button></footer>
    </section></div>
  </section>
  <p v-else role="status">此功能目前僅提供 DEV 假資料預覽。</p>
</template>

<style scoped>
.execution-page{display:grid;gap:10px;padding:14px;align-content:start;color:#26313e;min-height:0}.page-head{display:flex;justify-content:space-between;align-items:center;border-bottom:1px solid #c4cbd3;padding-bottom:8px}.page-head small{color:#687582}.page-head h2{margin:2px 0 0;font-size:21px}.dev-tag{font-size:12px;font-weight:400;color:#795b00;background:#fff0b2;padding:3px 8px;border-radius:3px;vertical-align:middle}
.panel{display:grid;gap:8px;border:1px solid #c4cbd3;background:#fff;padding:10px;min-width:0}.panel h3{font-size:15px;margin:0;border-bottom:1px solid #e0e3e6;padding-bottom:6px}.execute-grid{display:grid;grid-template-columns:repeat(3,minmax(170px,1fr));gap:8px 12px}.execute-grid label,.filter-grid>label,.read-fields label{display:grid;gap:4px;font-size:13px}.execute-grid input,.filter-grid input,.read-fields input{min-width:0;min-height:28px;border:1px solid #9ba4ad;border-radius:2px;padding:3px 6px;color:#26313e;background:#fff}.execute-grid input[readonly],.read-fields input{background:#f0f0f0;color:#5d6670}.execute-grid .required{background:#fff0b2}.dev-settings{font-size:12px;color:#65717d}.dev-settings summary{cursor:pointer;width:max-content}.dev-settings>div{display:flex;gap:16px;flex-wrap:wrap;padding:8px 0 0}.dev-settings label{display:flex;align-items:center;gap:5px}.dev-settings input{min-height:unset}
.filter-grid{display:grid;grid-template-columns:repeat(5,minmax(140px,1fr)) auto;gap:8px 10px;align-items:end}.lookup{display:flex;gap:4px}.lookup input{flex:1}.lookup button{min-width:31px}.query-actions{display:flex;gap:6px}.query-hint{margin:0;color:#687582;font-size:12px}.grid{overflow:auto;border:1px solid #bbc4cd;background:#fff;max-height:48vh;min-width:0}table{border-collapse:collapse;width:max-content;min-width:100%;font-size:13px}th,td{border:1px solid #c9cfd6;padding:5px 7px;white-space:nowrap;text-align:left}th{position:sticky;top:0;background:#e9edf1;font-weight:600;z-index:1}tbody tr{cursor:pointer}tbody tr:hover td{background:#fbf5e3}tbody tr.selected td{background:#fff0b2}.cell-link{border:0;background:none;padding:0;color:#1e5d8e;text-decoration:underline;cursor:pointer}.number{text-align:right;font-variant-numeric:tabular-nums}.status-pill{display:inline-block;min-width:64px;text-align:center;padding:2px 5px;border:1px solid #c4cbd3;background:#f5f6f7;border-radius:2px}.status-0 .status-pill{background:#edf3fa}.status-1 .status-pill{background:#e6f3e5}.status-2 .status-pill,.status-3 .status-pill{background:#fff0d5}.status-4 .status-pill{background:#f0f0f0}.status-5 .status-pill{background:#e9eef5}.empty{text-align:center;color:#78838d;padding:16px}.grid-footer{display:flex;justify-content:space-between;color:#687582;font-size:12px}.message{margin:0;padding:8px 10px;border:1px solid #c4cbd3}.error{color:#962c20;background:#fff0ed;border-color:#d59a90}.success{color:#315a25;background:#edf6e8;border-color:#a9c79a}.history-panel ul{margin:0;padding-left:20px;display:grid;gap:5px;font-size:12px}.history-panel small{color:#687582;margin-left:6px}footer{display:flex;justify-content:flex-end;gap:8px;border-top:1px solid #e0e3e6;padding-top:9px}button{font:inherit;min-height:29px;padding:3px 11px;border:1px solid #b8c0c8;border-radius:3px;background:#f6f6f6;color:inherit;cursor:pointer}button:hover{background:#fff4cf}.primary{background:#ffad32;border-color:#eb9b24;color:#fff;font-weight:600}.overlay{position:fixed;inset:0;z-index:160;display:grid;place-items:center;background:#26334266;padding:16px}.dialog{width:min(760px,100%);max-height:90vh;overflow:auto;display:grid;gap:12px;background:#fff;border:1px solid #87939f;padding:16px;box-shadow:0 8px 28px #1113}.dialog h3{margin:0}.dialog p{margin:0}.read-fields{display:grid;grid-template-columns:repeat(2,minmax(130px,1fr));gap:9px 12px}.lookup-list{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:6px;max-height:50vh;overflow:auto}
@media(max-width:1100px){.filter-grid{grid-template-columns:repeat(3,minmax(130px,1fr))}.query-actions{grid-column:1/-1}.execute-grid{grid-template-columns:repeat(2,minmax(170px,1fr))}}@media(max-width:650px){.filter-grid,.execute-grid{grid-template-columns:1fr}.grid-footer{align-items:flex-start;gap:5px;flex-direction:column}.dev-settings>div{display:grid}.lookup-list{grid-template-columns:1fr}}
</style>
