<script setup lang="ts">
import { computed, ref } from 'vue'

type DataType = 'WIP' | 'SEMIWIP' | 'FGD' | 'SEMI' | 'RAW' | 'MTLWIP'
type ConfirmScenario = 'success' | 'partial-failure'
type CountRow = {
  id: string
  countSerial: string
  countDate: string
  dataType: DataType
  inventoryNo: string
  locatorNo: string
  lotNo: string
  materialNo: string
  materialLotNo: string
  opNo: string
  moNo: string
  productNo: string
  productVersion: string
  curQty: number
  sysQty: number
  curUnitNo: string
  sysUnitNo: string
  adjustState: 0 | 1 | 2
  adjustQty: number
  newCurQty: number | null
  adjustUserNo: string
  adjustDate: string
  sourceQty: number
}
type HistoryRow = { at: string; dataType: DataType; countSerial: string; key: string; action: string; oldQty: number; adjustQty: number; newQty: number; state: string; note: string }

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const dataTypes: { value: DataType; label: string; group: string }[] = [
  { value: 'WIP', label: 'WIP 生產批', group: '生產批' },
  { value: 'SEMIWIP', label: 'SEMI WIP 線邊半成品', group: '生產批' },
  { value: 'FGD', label: 'FGD 成品庫', group: '產品庫存' },
  { value: 'SEMI', label: 'SEMI 半成品庫', group: '產品庫存' },
  { value: 'RAW', label: 'RAW 原物料庫', group: '物料庫存' },
  { value: 'MTLWIP', label: 'MTL WIP 物料線邊庫', group: '物料庫存' },
]
const rows = ref<CountRow[]>(dev.value ? [
  { id: 'wip-1', countSerial: 'DEV-IC-608-001', countDate: '2026-11-20 18:00', dataType: 'WIP', inventoryNo: '', locatorNo: '', lotNo: 'DEV-LOT-608-001', materialNo: '', materialLotNo: '', opNo: 'DEV-OP-120', moNo: 'DEV-MO-608-01', productNo: 'DEV-PRD-608', productVersion: 'A1', curQty: 100, sysQty: 100, curUnitNo: 'PCS', sysUnitNo: 'PCS', adjustState: 0, adjustQty: 0, newCurQty: null, adjustUserNo: '', adjustDate: '', sourceQty: 100 },
  { id: 'wip-2', countSerial: 'DEV-IC-608-001', countDate: '2026-11-20 18:00', dataType: 'WIP', inventoryNo: '', locatorNo: '', lotNo: 'DEV-LOT-608-002', materialNo: '', materialLotNo: '', opNo: 'DEV-OP-120', moNo: 'DEV-MO-608-01', productNo: 'DEV-PRD-608', productVersion: 'A1', curQty: 64, sysQty: 64, curUnitNo: 'PCS', sysUnitNo: 'PCS', adjustState: 0, adjustQty: 0, newCurQty: null, adjustUserNo: '', adjustDate: '', sourceQty: 64 },
  { id: 'semiwip-1', countSerial: 'DEV-IC-608-001', countDate: '2026-11-20 18:00', dataType: 'SEMIWIP', inventoryNo: 'DEV-WIP-SEMI-01', locatorNo: '', lotNo: 'DEV-LOT-608-003', materialNo: '', materialLotNo: '', opNo: 'DEV-OP-240', moNo: 'DEV-MO-608-02', productNo: 'DEV-PRD-609', productVersion: 'B1', curQty: 35, sysQty: 70, curUnitNo: 'BOX', sysUnitNo: 'PCS', adjustState: 0, adjustQty: 0, newCurQty: null, adjustUserNo: '', adjustDate: '', sourceQty: 35 },
  { id: 'fgd-1', countSerial: 'DEV-IC-608-001', countDate: '2026-11-20 18:00', dataType: 'FGD', inventoryNo: 'DEV-FGD-01', locatorNo: 'DEV-LOC-A01', lotNo: 'DEV-LOT-608-004', materialNo: '', materialLotNo: '', opNo: '', moNo: 'DEV-MO-608-03', productNo: 'DEV-PRD-610', productVersion: 'A1', curQty: 40, sysQty: 40, curUnitNo: 'PCS', sysUnitNo: 'PCS', adjustState: 0, adjustQty: 0, newCurQty: null, adjustUserNo: '', adjustDate: '', sourceQty: 40 },
  { id: 'fgd-2', countSerial: 'DEV-IC-608-001', countDate: '2026-11-20 18:00', dataType: 'FGD', inventoryNo: 'DEV-FGD-01', locatorNo: 'DEV-LOC-A02', lotNo: 'DEV-LOT-608-005', materialNo: '', materialLotNo: '', opNo: '', moNo: 'DEV-MO-608-03', productNo: 'DEV-PRD-610', productVersion: 'A1', curQty: 18, sysQty: 18, curUnitNo: 'PCS', sysUnitNo: 'PCS', adjustState: 0, adjustQty: 0, newCurQty: null, adjustUserNo: '', adjustDate: '', sourceQty: 18 },
  { id: 'semi-1', countSerial: 'DEV-IC-608-001', countDate: '2026-11-20 18:00', dataType: 'SEMI', inventoryNo: 'DEV-SEMI-01', locatorNo: 'DEV-LOC-S01', lotNo: 'DEV-LOT-608-006', materialNo: '', materialLotNo: '', opNo: '', moNo: 'DEV-MO-608-04', productNo: 'DEV-PRD-611', productVersion: 'C2', curQty: 22, sysQty: 22, curUnitNo: 'PCS', sysUnitNo: 'PCS', adjustState: 0, adjustQty: 0, newCurQty: null, adjustUserNo: '', adjustDate: '', sourceQty: 22 },
  { id: 'raw-1', countSerial: 'DEV-IC-608-001', countDate: '2026-11-20 18:00', dataType: 'RAW', inventoryNo: 'DEV-RAW-01', locatorNo: 'DEV-LOC-R01', lotNo: '', materialNo: 'DEV-MTL-608', materialLotNo: 'DEV-MTLLOT-001', opNo: '', moNo: '', productNo: '', productVersion: '', curQty: 280, sysQty: 280, curUnitNo: 'KG', sysUnitNo: 'KG', adjustState: 0, adjustQty: 0, newCurQty: null, adjustUserNo: '', adjustDate: '', sourceQty: 980 },
  { id: 'raw-2', countSerial: 'DEV-IC-608-001', countDate: '2026-11-20 18:00', dataType: 'RAW', inventoryNo: 'DEV-RAW-01', locatorNo: 'DEV-LOC-R02', lotNo: '', materialNo: 'DEV-MTL-608', materialLotNo: 'DEV-MTLLOT-002', opNo: '', moNo: '', productNo: '', productVersion: '', curQty: 195, sysQty: 195, curUnitNo: 'KG', sysUnitNo: 'KG', adjustState: 0, adjustQty: 0, newCurQty: null, adjustUserNo: '', adjustDate: '', sourceQty: 980 },
  { id: 'mtlwip-1', countSerial: 'DEV-IC-608-001', countDate: '2026-11-20 18:00', dataType: 'MTLWIP', inventoryNo: 'DEV-WIP-RAW-01', locatorNo: '', lotNo: '', materialNo: 'DEV-MTL-609', materialLotNo: 'DEV-MTLLOT-003', opNo: '', moNo: '', productNo: '', productVersion: '', curQty: 72, sysQty: 72, curUnitNo: 'PCS', sysUnitNo: 'PCS', adjustState: 0, adjustQty: 0, newCurQty: null, adjustUserNo: '', adjustDate: '', sourceQty: 72 },
] : [])
const dataType = ref<DataType>('WIP')
const selectedId = ref('')
const queried = ref(false)
const editing = ref(false)
const showPrintPreview = ref(false)
const editNewCurQty = ref('')
const confirmScenario = ref<ConfirmScenario>('success')
const busy = ref(false)
const error = ref('')
const info = ref('')
const history = ref<HistoryRow[]>([])
const reportPrinted = ref(false)
const displayRows = computed(() => queried.value ? rows.value.filter(row => row.dataType === dataType.value) : [])
const selectedRow = computed(() => rows.value.find(row => row.id === selectedId.value))
const pendingRows = computed(() => rows.value.filter(row => row.dataType === dataType.value && row.adjustState === 1))
const lockedRows = computed(() => rows.value.filter(row => row.dataType === dataType.value && row.adjustState === 2))
const currentType = computed(() => dataTypes.find(item => item.value === dataType.value)!)
const isLotType = computed(() => dataType.value === 'WIP' || dataType.value === 'SEMIWIP')
const isProductType = computed(() => dataType.value === 'FGD' || dataType.value === 'SEMI')
const isMaterialType = computed(() => dataType.value === 'RAW' || dataType.value === 'MTLWIP')
const adjustDelta = computed(() => {
  const row = selectedRow.value
  const next = Number(editNewCurQty.value)
  return row && Number.isFinite(next) ? next - row.curQty : null
})
const fmt = (qty: number) => new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 4 }).format(qty)
const nowText = () => new Date().toLocaleString('zh-TW', { hour12: false })

function query(): void {
  error.value = ''
  info.value = ''
  selectedId.value = ''
  editing.value = false
  showPrintPreview.value = false
  if (!dev.value) { error.value = '正式盤點服務尚未接入；此頁不會讀取或寫入資料。'; return }
  queried.value = true
  info.value = `已載入 ${displayRows.value.length} 筆 ${dataType.value} 盤點已關帳明細（DEV）。`
}

function changeType(value: DataType): void {
  dataType.value = value
  queried.value = false
  selectedId.value = ''
  editing.value = false
  showPrintPreview.value = false
  error.value = ''
  info.value = ''
}

function selectRow(row: CountRow): void {
  selectedId.value = row.id
  editNewCurQty.value = row.newCurQty === null ? '' : String(row.newCurQty)
  editing.value = false
  error.value = ''
  info.value = `${row.lotNo || row.materialLotNo || row.productNo} · AdjustState=${row.adjustState}。`
}

function beginAdjust(): void {
  error.value = ''
  info.value = ''
  if (!selectedRow.value) { error.value = '請先選取一筆盤點明細。'; return }
  if (selectedRow.value.adjustState === 2) { error.value = '這筆盤點調帳已確認，不能再次修改。'; return }
  editing.value = true
}

function stageAdjustment(closeAfterSave = false): void {
  error.value = ''
  info.value = ''
  const row = selectedRow.value
  const value = Number(editNewCurQty.value)
  if (!row) { error.value = '請先選取一筆盤點明細。'; return }
  if (!Number.isFinite(value) || editNewCurQty.value.trim() === '') { error.value = 'New CurQty 必須填入數字。'; return }
  if (value < 0 || value === row.curQty) { error.value = 'New CurQty 不可小於 0，也不可等於目前 CurQty。'; return }
  if (row.adjustState === 2) { error.value = '這筆調帳已確認，不能再次修改。'; return }
  row.newCurQty = value
  row.adjustQty = value - row.curQty
  row.adjustState = 1
  row.adjustUserNo = 'DEVUSER'
  row.adjustDate = nowText()
  history.value.unshift({ at: row.adjustDate, dataType: row.dataType, countSerial: row.countSerial, key: row.lotNo || `${row.inventoryNo}/${row.locatorNo}/${row.materialNo}/${row.materialLotNo}`, action: '設定盤點數量', oldQty: row.curQty, adjustQty: row.adjustQty, newQty: value, state: '1 · 待確認', note: '先存入盤點明細；尚未更新正式庫存' })
  info.value = `已暫存調帳：${fmt(row.curQty)} ${row.curUnitNo} → ${fmt(value)} ${row.curUnitNo}（差額 ${fmt(row.adjustQty)}）。按 Confirm 才模擬更新庫存。`
  if (closeAfterSave) editing.value = false
}

function confirmAdjustments(): void {
  error.value = ''
  info.value = ''
  if (!dev.value || busy.value) return
  if (!queried.value || !displayRows.value.length) { error.value = '請先 Query 盤點明細。'; return }
  if (!pendingRows.value.length) { error.value = '沒有 AdjustState=1 的待確認調帳；先選一筆明細暫存 New CurQty。'; return }
  busy.value = true
  const at = nowText()
  const rowsToConfirm = confirmScenario.value === 'partial-failure' && pendingRows.value.length > 1 ? pendingRows.value.slice(0, 1) : confirmScenario.value === 'partial-failure' ? [] : [...pendingRows.value]
  for (const row of rowsToConfirm) {
    const oldQty = row.curQty
    const delta = row.adjustQty
    const newQty = row.newCurQty ?? row.curQty + delta
    row.curQty = newQty
    if (row.dataType === 'SEMIWIP' && row.sysUnitNo === row.curUnitNo) row.sysQty = newQty
    else if (row.dataType === 'WIP' || row.dataType === 'SEMIWIP') row.sysQty = row.sysUnitNo === row.curUnitNo ? newQty : newQty * 2
    row.adjustState = 2
    history.value.unshift({ at, dataType: row.dataType, countSerial: row.countSerial, key: row.lotNo || `${row.inventoryNo}/${row.locatorNo}/${row.materialNo}/${row.materialLotNo}`, action: 'Confirm 庫存調帳', oldQty, adjustQty: delta, newQty, state: '2 · 已確認', note: row.dataType === 'SEMIWIP' ? 'Lot CurQty／SysQty 與線邊 SEMI Qty 已更新' : row.dataType === 'WIP' ? 'Lot CurQty／SysQty 已更新' : row.dataType === 'RAW' ? 'LocQty 與原物料庫 CurQty 已更新' : row.dataType === 'MTLWIP' ? '物料線邊 Qty 已更新' : 'FGD／SEMI LocQty 已更新' })
  }
  busy.value = false
  if (confirmScenario.value === 'partial-failure') {
    error.value = rowsToConfirm.length ? `模擬第二筆更新失敗：${rowsToConfirm.length} 筆已確認，其餘仍為 AdjustState=1。舊 Service 逐筆執行 Core，失敗時可能留下部分更新。` : '只有一筆待確認資料；模擬第一筆 Core 更新即失敗，保持待確認狀態。'
    return
  }
  info.value = `盤點調帳確認完成：${rowsToConfirm.length} 筆更新庫存，AdjustState 由 1 改為 2。`
}

function openPrintPreview(): void {
  error.value = ''
  if (!queried.value || !displayRows.value.length) { error.value = '目前沒有盤點資料可預覽。'; return }
  showPrintPreview.value = true
  reportPrinted.value = false
}

function simulatePrint(): void {
  reportPrinted.value = true
  info.value = 'DEV 僅標示預覽已確認；沒有送到系統印表機。'
}

function statusLabel(state: CountRow['adjustState']): string {
  return state === 0 ? '未調帳' : state === 1 ? '已暫存 · 待確認' : '調帳已確認'
}
</script>

<template>
  <section class="inv-adjust">
    <header class="hero"><div><p class="eyebrow">WIP · INVENTORY CONTROL · WP608</p><h1>盤點調帳</h1><p class="subtitle">盤點關帳明細 → 設定 New CurQty → Confirm 更新庫存</p></div><div class="hero-actions"><span class="dev-pill">DEV 假資料</span><button class="quiet" type="button" @click="emit('close')">關閉</button></div></header>
    <div v-if="!dev" class="unavailable">正式盤點服務尚未接入；此頁不會讀取或寫入資料。</div>
    <template v-else>
      <div class="flow"><b>選庫存類型</b><span>→</span><b>查詢 State=0 盤點批次</b><span>→</span><b>單筆設定 New CurQty</b><span>→</span><b>Confirm 批次回寫庫存</b></div>
      <div v-if="error || info" class="notice" :class="{ bad: !!error }" role="status">{{ error || info }}</div>

      <section class="card filters"><div class="card-heading"><div><h2>盤點庫存類型</h2><p>每次只查詢一種 DataType；只載入 State=0 的盤點關帳明細。</p></div><span class="setting">CountSerial DEV-IC-608-001</span></div><div class="type-options"><label v-for="item in dataTypes" :key="item.value" :class="{ active: dataType === item.value }"><input type="radio" name="inv-count-type" :value="item.value" :checked="dataType === item.value" @change="changeType(item.value)"><span>{{ item.label }}</span><small>{{ item.group }}</small></label><button class="primary" type="button" @click="query">Query</button></div></section>

      <div class="summary"><div><small>DataType</small><strong>{{ dataType }}</strong><small>{{ currentType.label }}</small></div><div><small>明細筆數</small><strong>{{ displayRows.length }}</strong></div><div><small>待 Confirm</small><strong>{{ pendingRows.length }}</strong></div><div><small>已 Confirm</small><strong>{{ lockedRows.length }}</strong></div><div class="summary-spacer"></div><label class="scenario">確認結果情境<select v-model="confirmScenario"><option value="success">全部成功</option><option value="partial-failure">第二筆失敗（示範部分完成）</option></select></label><button class="quiet" type="button" :disabled="!displayRows.length" @click="openPrintPreview">Print Preview</button><button class="quiet" type="button" :disabled="!displayRows.length" @click="confirmAdjustments">Confirm</button><button class="primary" type="button" :disabled="busy || !selectedRow || selectedRow.adjustState === 2" @click="beginAdjust">{{ selectedRow?.adjustState === 1 ? '修改暫存調帳' : '調帳' }}</button></div>

      <section v-if="showPrintPreview" class="card print-preview"><div class="card-heading"><div><h2>盤點調帳報表預覽</h2><p>來源 ActiveReport：{{ isLotType ? 'arInvCountAdjust_General' : isMaterialType ? 'arInvCountAdjust_MTL' : 'arInvCountAdjust_PRD' }} · DEV 預覽不送印</p></div><div class="preview-actions"><button class="quiet" type="button" @click="showPrintPreview = false">關閉預覽</button><button class="primary" type="button" @click="simulatePrint">{{ reportPrinted ? '已模擬列印' : '模擬列印' }}</button></div></div><p v-if="reportPrinted" class="print-note">已完成 DEV 列印模擬，沒有呼叫作業系統印表機。</p></section>

      <section class="card grid-card"><div class="card-heading"><div><h2>{{ currentType.label }}盤點明細</h2><p>{{ queried ? '來源 Query：State=0 · 已依庫存類型排序' : '按 Query 載入 DEV 盤點關帳明細' }}</p></div><span class="filter-note">已暫存不等於已更新庫存</span></div><div class="table-wrap"><table><thead><tr><th>選取</th><th>CountDate</th><th v-if="isLotType">LotNo</th><th v-if="isLotType">OPNo</th><th v-if="isLotType">MO</th><th v-if="isLotType">ProductNo</th><th v-if="isMaterialType">InventoryNo</th><th v-if="isMaterialType || isProductType">LocatorNo</th><th v-if="isMaterialType">MaterialNo</th><th v-if="isMaterialType">MaterialLotNo</th><th v-if="isProductType">InventoryNo</th><th v-if="isProductType">LotNo</th><th v-if="isProductType">ProductNo</th><th class="num">CurQty</th><th>CurUnit</th><th v-if="isLotType">SysQty</th><th v-if="isLotType">ProductVersion</th><th class="num">NewCurQty</th><th class="num">AdjustQty</th><th>AdjustUserNo</th><th>AdjustDate</th><th>AdjustState</th></tr></thead><tbody><tr v-for="row in displayRows" :key="row.id" :class="{ selected: selectedId === row.id, staged: row.adjustState === 1, confirmed: row.adjustState === 2 }" @click="selectRow(row)"><td><input type="radio" name="count-row" :checked="selectedId === row.id" @click.stop="selectRow(row)"></td><td>{{ row.countDate }}</td><td v-if="isLotType" class="serial">{{ row.lotNo }}</td><td v-if="isLotType">{{ row.opNo }}</td><td v-if="isLotType">{{ row.moNo }}</td><td v-if="isLotType">{{ row.productNo }}</td><td v-if="isMaterialType">{{ row.inventoryNo }}</td><td v-if="isMaterialType || isProductType">{{ row.locatorNo }}</td><td v-if="isMaterialType">{{ row.materialNo }}</td><td v-if="isMaterialType" class="serial">{{ row.materialLotNo }}</td><td v-if="isProductType">{{ row.inventoryNo }}</td><td v-if="isProductType" class="serial">{{ row.lotNo }}</td><td v-if="isProductType">{{ row.productNo }}</td><td class="num">{{ fmt(row.curQty) }}</td><td>{{ row.curUnitNo }}</td><td v-if="isLotType">{{ fmt(row.sysQty) }} {{ row.sysUnitNo }}</td><td v-if="isLotType">{{ row.productVersion }}</td><td class="num">{{ row.newCurQty === null ? '—' : fmt(row.newCurQty) }}</td><td class="num">{{ row.adjustState === 0 ? '—' : fmt(row.adjustQty) }}</td><td>{{ row.adjustUserNo || '—' }}</td><td>{{ row.adjustDate || '—' }}</td><td><span class="status" :class="{ pending: row.adjustState === 1, done: row.adjustState === 2 }">{{ statusLabel(row.adjustState) }}</span></td></tr><tr v-if="!displayRows.length"><td colspan="22" class="empty">{{ queried ? '此 DataType 沒有 DEV 盤點明細' : '尚未查詢' }}</td></tr></tbody></table></div></section>

      <section v-if="editing && selectedRow" class="card editor"><div class="card-heading"><div><h2>設定盤點數量</h2><p>{{ selectedRow.countSerial }} · {{ selectedRow.lotNo || selectedRow.materialLotNo || selectedRow.productNo }} · 目前 {{ fmt(selectedRow.curQty) }} {{ selectedRow.curUnitNo }}</p></div><span class="setting">{{ dataType }}</span></div><div class="edit-fields"><template v-if="isLotType"><label>LotNo<input :value="selectedRow.lotNo" readonly></label><label>目前 CurQty<input :value="fmt(selectedRow.curQty)" readonly></label></template><template v-else-if="isProductType"><label>InventoryNo<input :value="selectedRow.inventoryNo" readonly></label><label>LocatorNo<input :value="selectedRow.locatorNo" readonly></label><label>LotNo<input :value="selectedRow.lotNo" readonly></label><label>目前 CurQty<input :value="fmt(selectedRow.curQty)" readonly></label></template><template v-else><label>InventoryNo<input :value="selectedRow.inventoryNo" readonly></label><label>LocatorNo<input :value="selectedRow.locatorNo || '—'" readonly></label><label>MaterialNo<input :value="selectedRow.materialNo" readonly></label><label>MaterialLotNo<input :value="selectedRow.materialLotNo" readonly></label><label>目前 CurQty<input :value="fmt(selectedRow.curQty)" readonly></label></template><label class="new-qty">New CurQty<input v-model="editNewCurQty" inputmode="decimal" type="number" min="0" step="0.0001" placeholder="輸入盤點後數量"></label><label>AdjustQty（New - Cur）<input :value="adjustDelta === null ? '—' : fmt(adjustDelta)" readonly></label></div><p class="caption">允許正數或負數差額；New CurQty 必須是非負數，且不能等於原 CurQty。Apply 只將 AdjustState 設為 1，Confirm 才更新庫存。</p><div class="actions"><button class="quiet" type="button" @click="editing = false">取消</button><button class="quiet" type="button" @click="stageAdjustment(false)">Apply · 保持開啟</button><button class="primary" type="button" @click="stageAdjustment(true)">確定並關閉</button></div></section>

      <section class="card history"><div class="card-heading"><div><h2>DEV 操作紀錄</h2><p>Confirm 逐筆更新庫存與 AdjustState；來源多筆失敗可能部分完成。</p></div></div><div class="table-wrap"><table><thead><tr><th>時間</th><th>DataType</th><th>CountSerial</th><th>盤點鍵</th><th>操作</th><th class="num">原 CurQty</th><th class="num">AdjustQty</th><th class="num">New CurQty</th><th>狀態</th><th>說明</th></tr></thead><tbody><tr v-for="(item, index) in history" :key="item.at + item.key + index"><td>{{ item.at }}</td><td>{{ item.dataType }}</td><td class="serial">{{ item.countSerial }}</td><td>{{ item.key }}</td><td>{{ item.action }}</td><td class="num">{{ fmt(item.oldQty) }}</td><td class="num">{{ fmt(item.adjustQty) }}</td><td class="num">{{ fmt(item.newQty) }}</td><td>{{ item.state }}</td><td>{{ item.note }}</td></tr><tr v-if="!history.length"><td colspan="10" class="empty">尚無操作紀錄</td></tr></tbody></table></div></section>
      <footer>DEV 只模擬盤點查詢、明細調帳、Confirm 與報表預覽；不呼叫 MES Service、資料庫或本機印表機。</footer>
    </template>
  </section>
</template>

<style scoped>
.inv-adjust{--ink:#1b2a3c;--muted:#708094;--line:#dce4eb;color:var(--ink);height:100%;min-height:0;overflow:auto;padding:22px 26px 30px;background:#f7f9fb;font:13px/1.45 "Segoe UI","Microsoft JhengHei",sans-serif}.hero{display:flex;align-items:center;justify-content:space-between;margin-bottom:15px}.eyebrow{margin:0;color:#73869a;font-size:10px;font-weight:750;letter-spacing:.13em}.hero h1{margin:2px 0 0;font-size:23px}.subtitle{margin:0;color:var(--muted);font-size:12px}.hero-actions{display:flex;align-items:center;gap:9px}.dev-pill,.setting,.status{display:inline-block;padding:4px 8px;border-radius:999px;background:#e8f4ed;color:#22724a;font-size:10px;font-weight:700}.setting{background:#edf2f8;color:#385a78}.quiet,.primary{height:34px;padding:0 12px;border:1px solid var(--line);border-radius:5px;background:white;color:#334d66;cursor:pointer;font:inherit}.primary{border-color:#173b5d;background:#173b5d;color:white;font-weight:700}.primary:disabled,.quiet:disabled{opacity:.45;cursor:not-allowed}.flow{display:flex;align-items:center;gap:10px;padding:10px 14px;margin-bottom:13px;border:1px solid #ccdae7;border-radius:6px;background:#eff5fa;color:#48637c;font-size:11px}.flow b{color:#234663}.card{margin-bottom:13px;border:1px solid var(--line);border-radius:7px;background:white;box-shadow:0 2px 5px #10283a08}.filters,.editor{padding:13px 15px}.card-heading{display:flex;align-items:center;justify-content:space-between;padding:11px 14px}.filters .card-heading,.editor .card-heading{padding:0 0 10px}.card-heading h2{margin:0;font-size:14px}.card-heading p{margin:2px 0 0;color:var(--muted);font-size:10px}.type-options{display:flex;align-items:center;gap:8px;flex-wrap:wrap}.type-options label{display:grid;grid-template-columns:18px auto;grid-template-rows:auto auto;align-items:center;column-gap:5px;padding:7px 10px;border:1px solid var(--line);border-radius:5px;background:#fff;color:#465d73;font-size:11px;cursor:pointer}.type-options label.active{border-color:#6695b4;background:#f0f7fb}.type-options input{grid-row:span 2;accent-color:#1d6289}.type-options small{grid-column:2;color:#8997a5;font-size:9px}.type-options .primary{margin-left:auto}.notice{margin:0 0 12px;padding:9px 12px;border:1px solid #bddcc9;border-radius:5px;background:#eef8f1;color:#286440}.notice.bad{border-color:#ebc5c1;background:#fff3f1;color:#9e3831}.summary{display:flex;align-items:center;gap:0;margin:12px 0;background:white;border:1px solid var(--line);border-radius:7px;overflow:hidden}.summary>div{min-width:100px;padding:8px 11px;border-right:1px solid var(--line);display:grid;grid-template-columns:1fr auto;align-items:center;column-gap:7px}.summary small{color:var(--muted);font-size:9px}.summary strong{grid-row:span 2;color:#244f71;font-size:16px}.summary .summary-spacer{flex:1;border:0}.scenario{display:flex;flex-direction:column;gap:3px;padding:4px 7px;color:#64778b;font-size:9px}.scenario select{height:28px;border:1px solid #cdd8e2;border-radius:4px;background:white;color:#334d66;font:inherit}.summary>.quiet,.summary>.primary{margin:6px 5px}.grid-card,.history{overflow:hidden}.filter-note{color:#73869a;font-size:10px}.table-wrap{overflow:auto;border-top:1px solid #e6ebf0}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:10px}th{position:sticky;top:0;background:#f1f5f8;color:#607388;font-size:9px;font-weight:700;text-align:left}th,td{padding:7px 8px;border-bottom:1px solid #ebeff3}tbody tr{cursor:pointer}tbody tr:hover{background:#f8fafc}tbody tr.selected{background:#edf6fb}tbody tr.staged{background:#fff8eb}tbody tr.confirmed{background:#f0f8f2}.serial{color:#60768b;font-family:Consolas,monospace}.num{text-align:right;font-variant-numeric:tabular-nums}input[type=radio]{accent-color:#1c628c}.status{border-radius:4px;background:#edf2f8;color:#536d86;font-size:9px}.status.pending{background:#fff1dc;color:#8a5a1f}.status.done{background:#e8f4ed;color:#22724a}.empty{text-align:center;color:#8a98a7;padding:17px}.edit-fields{display:grid;grid-template-columns:repeat(4,minmax(120px,1fr));gap:10px}.edit-fields label{display:flex;flex-direction:column;gap:5px;color:#5e7185;font-size:10px;font-weight:650}.edit-fields input{height:33px;min-width:0;border:1px solid #cdd8e2;border-radius:4px;padding:0 8px;background:white;color:var(--ink);font:inherit}.edit-fields input[readonly]{background:#f3f6f8;color:#66798e}.edit-fields .new-qty input{border-color:#7ba3be;background:#f7fbfd}.caption{margin:9px 0 0;color:#758496;font-size:10px}.actions{display:flex;justify-content:flex-end;gap:7px;padding-top:10px}.print-preview{padding:0 0 10px}.preview-actions{display:flex;gap:7px}.print-note{margin:0 14px;color:#22724a;font-size:10px}.history .card-heading{padding-bottom:9px}footer{padding:2px 2px 8px;color:#8391a0;font-size:10px}.unavailable{padding:16px;border:1px solid #e8d2ac;border-radius:6px;background:#fff8ea;color:#7a5622}@media(max-width:1100px){.inv-adjust{padding:14px}.flow{align-items:flex-start;flex-wrap:wrap}.summary{flex-wrap:wrap}.summary .summary-spacer{display:none}.edit-fields{grid-template-columns:repeat(2,minmax(120px,1fr))}}
</style>
