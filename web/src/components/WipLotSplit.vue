<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type SplitAction = 0 | 1 | 2
type Lot = {
  lotNo: string
  baseLotNo: string
  curQty: number
  curUnitNo: string
  sysQty: number
  sysUnitNo: string
  productNo: string
  productVersion: string
  processNo: string
  processVersion: string
  opNo: string
  psNo: string
  status: number
  statusName: string
  lotStamp: number
  sapPrdQty: number
  sapPcsQty: number
  conversion: number
  packaged: boolean
  hasGoodBinState: boolean
  outsourceReturned: boolean
}
type SplitRow = {
  splitLotNo: string
  splitLotQty: string
  splitSysQty: string
  action: SplitAction
  processNo: string
  processVersion: string
  opNo: string
  nodeId: string
  productNo: string
  productVersion: string
  unitNo: string
  sapPrdQty: string
  sapPcsQty: string
  mother: boolean
}
type SplitRecord = { lotNo: string; rows: SplitRow[]; savedAt: string }
type PickerMode = 'routing' | 'product' | 'electricbox' | null

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const storageKey = 'ymes-dev-mock:wip-lot-split:records:v1'
const sequenceKey = 'ymes-dev-mock:wip-lot-split:sequence:v1'

const lotFixtures: Lot[] = [
  { lotNo: 'DEV-LOT-SPLIT-001', baseLotNo: 'DEV-BASE-001', curQty: 240, curUnitNo: 'PCS', sysQty: 24, sysUnitNo: 'BOX', productNo: 'DEV-PRODUCT-A', productVersion: '01', processNo: 'DEV-PROC-ETCH', processVersion: '03', opNo: 'DEV-OP-ETCH-20', psNo: 'DEV-PS-ETCH', status: 1, statusName: 'Running', lotStamp: 1042, sapPrdQty: 240, sapPcsQty: 24, conversion: 0.1, packaged: false, hasGoodBinState: false, outsourceReturned: false },
  { lotNo: 'DEV-LOT-OUT-005', baseLotNo: 'DEV-BASE-005', curQty: 80, curUnitNo: 'PCS', sysQty: 8, sysUnitNo: 'BOX', productNo: 'DEV-PRODUCT-B', productVersion: '02', processNo: 'DEV-PROC-PACK', processVersion: '01', opNo: 'DEV-OP-PACK-10', psNo: 'DEV-PS-PACK', status: 5, statusName: 'OutSourcing', lotStamp: 2051, sapPrdQty: 80, sapPcsQty: 8, conversion: 0.1, packaged: false, hasGoodBinState: false, outsourceReturned: false },
  { lotNo: 'DEV-LOT-BIN-009', baseLotNo: 'DEV-BASE-009', curQty: 120, curUnitNo: 'PCS', sysQty: 12, sysUnitNo: 'BOX', productNo: 'DEV-PRODUCT-C', productVersion: '01', processNo: 'DEV-PROC-TEST', processVersion: '02', opNo: 'DEV-OP-TEST-30', psNo: 'DEV-PS-TEST', status: 1, statusName: 'Running', lotStamp: 3099, sapPrdQty: 120, sapPcsQty: 12, conversion: 0.1, packaged: false, hasGoodBinState: true, outsourceReturned: true },
]
const products = [
  { productNo: 'DEV-PRODUCT-A', productVersion: '01', productName: 'DEV 一般產品 A', unitNo: 'PCS' },
  { productNo: 'DEV-PRODUCT-B', productVersion: '02', productName: 'DEV 聯產品 B', unitNo: 'PCS' },
  { productNo: 'DEV-PRODUCT-C', productVersion: '01', productName: 'DEV 測試產品 C', unitNo: 'PCS' },
]
const routeOptions = [
  { action: 1 as SplitAction, processNo: 'DEV-PROC-ETCH', processVersion: '03', opNo: 'DEV-OP-ETCH-30', nodeId: 'DEV-NODE-ETCH-30', label: 'Jump OP · DEV-OP-ETCH-30' },
  { action: 2 as SplitAction, processNo: 'DEV-PROC-PACK', processVersion: '01', opNo: 'DEV-OP-PACK-10', nodeId: 'DEV-NODE-PACK-10', label: 'Jump Process · DEV-PROC-PACK' },
]

const lotNo = ref('')
const lot = ref<Lot | null>(null)
const splitMode = ref<'count' | 'qty'>('count')
const splitAmount = ref('3')
const includeMother = ref(true)
const splitRows = ref<SplitRow[]>([])
const selectedRowNo = ref('')
const pickerMode = ref<PickerMode>(null)
const pickerSearch = ref('')
const message = ref('請輸入參考批號，查詢後設定分批條件。')
const errorText = ref('')
const submitted = ref(false)
const records = ref<SplitRecord[]>([])
const nextSequence = ref(1)
const currentTime = ref('')

const canEdit = computed(() => dev.value && lot.value !== null && !submitted.value)
const lotSum = computed(() => splitRows.value.reduce((sum, row) => sum + (Number(row.splitLotQty) || 0), 0))
const sysSum = computed(() => splitRows.value.reduce((sum, row) => sum + (Number(row.splitSysQty) || 0), 0))
const prdSum = computed(() => splitRows.value.reduce((sum, row) => sum + (Number(row.sapPrdQty) || 0), 0))
const pcsSum = computed(() => splitRows.value.reduce((sum, row) => sum + (Number(row.sapPcsQty) || 0), 0))
const activeRow = computed(() => splitRows.value.find(row => row.splitLotNo === selectedRowNo.value) ?? null)
const filteredProducts = computed(() => products.filter(item => !pickerSearch.value || (item.productNo + ' ' + item.productVersion + ' ' + item.productName).toLowerCase().includes(pickerSearch.value.toLowerCase().trim())))
const sourceLotPreview = computed(() => lot.value ? lot.value : null)
const canEditSystemQty = computed(() => canEdit.value && lot.value?.curUnitNo.toLowerCase() !== lot.value?.sysUnitNo.toLowerCase())
// The DEV fixture models a site where IC_FT_PKG is enabled; production reads licensed modules.
const icFtPkgEnabled = computed(() => dev.value)

function readDevState(): void {
  if (!dev.value) return
  try {
    records.value = JSON.parse(window.localStorage.getItem(storageKey) || '[]') as SplitRecord[]
    nextSequence.value = Math.max(1, Number(window.localStorage.getItem(sequenceKey) || '1'))
  } catch {
    records.value = []
    nextSequence.value = 1
  }
}

function saveDevState(): void {
  if (!dev.value) return
  window.localStorage.setItem(storageKey, JSON.stringify(records.value))
  window.localStorage.setItem(sequenceKey, String(nextSequence.value))
}

function clearDraft(): void {
  splitRows.value = []
  selectedRowNo.value = ''
  submitted.value = false
}

function queryLot(): void {
  errorText.value = ''
  clearDraft()
  if (!dev.value) {
    errorText.value = '目前頁面僅提供 DEV 假資料預覽。'
    return
  }
  const key = lotNo.value.trim().toUpperCase()
  if (!key) {
    lot.value = null
    errorText.value = 'Lot No 不可空白。'
    return
  }
  const found = lotFixtures.find(item => item.lotNo === key)
  if (!found) {
    lot.value = null
    errorText.value = 'DEV 假資料中找不到此 Lot No。可試 DEV-LOT-SPLIT-001。'
    return
  }
  if (found.packaged) {
    lot.value = null
    errorText.value = '已包裝 Lot 不可執行分批。'
    return
  }
  if (found.hasGoodBinState) {
    lot.value = null
    errorText.value = '此 Lot 有 GoodBinState，請改用「分批處理_等級」。'
    return
  }
  lot.value = { ...found }
  lotNo.value = found.lotNo
  message.value = '已載入參考批資料；分批列尚未建立。'
  currentTime.value = new Date().toLocaleTimeString('zh-TW', { hour12: false })
}

function round2(value: number): number {
  return Math.round((value + Number.EPSILON) * 100) / 100
}

function convertToSystemQty(value: number): number {
  if (!lot.value) return 0
  if (lot.value.curUnitNo.toLowerCase() === lot.value.sysUnitNo.toLowerCase()) return value
  return round2(value * lot.value.conversion)
}

function makeChildLotNo(index: number): string {
  return 'DEV-SPLIT-' + String(index).padStart(6, '0')
}

function buildSplitRows(): void {
  errorText.value = ''
  if (!canEdit.value || !lot.value) {
    errorText.value = '請先查詢有效的參考批。'
    return
  }
  const amount = Number(splitAmount.value)
  if (!Number.isFinite(amount) || amount <= 0) {
    errorText.value = 'Split Count / Qty 必須是大於 0 的數字。'
    return
  }
  if (splitMode.value === 'count' && (!Number.isInteger(amount) || amount > 100)) {
    errorText.value = 'DEV 批數需為 1 至 100 的整數。'
    return
  }
  if (splitMode.value === 'qty' && amount > lot.value.curQty) {
    errorText.value = 'Split Qty 不可大於參考批 CurQty。'
    return
  }
  if (splitMode.value === 'qty' && Math.ceil(lot.value.curQty / amount) > 100) {
    errorText.value = 'DEV 預覽最多建立 100 列；請提高 Split Qty。'
    return
  }

  const fullCount = splitMode.value === 'count' ? Math.floor(amount) : Math.floor(lot.value.curQty / amount)
  const remainder = splitMode.value === 'count'
    ? round2(lot.value.curQty - Math.floor(lot.value.curQty / fullCount) * (fullCount - 1))
    : round2(lot.value.curQty - fullCount * amount)
  const hasRemainder = splitMode.value === 'qty' && remainder > 0
  const motherRowCount = includeMother.value ? 1 : 0
  const rows: SplitRow[] = []
  let serialIndex = nextSequence.value
  const addRow = (qtyBasis: number, mother: boolean) => {
    const rowLotNo = mother ? lot.value!.lotNo : makeChildLotNo(serialIndex++)
    rows.push({
      splitLotNo: rowLotNo,
      splitLotQty: '',
      splitSysQty: String(convertToSystemQty(qtyBasis)),
      action: 0,
      processNo: '',
      processVersion: '',
      opNo: '',
      nodeId: '',
      productNo: lot.value!.productNo,
      productVersion: lot.value!.productVersion,
      unitNo: lot.value!.curUnitNo,
      sapPrdQty: '',
      sapPcsQty: '',
      mother,
    })
  }

  const fullQty = splitMode.value === 'count' ? Math.floor(lot.value.curQty / fullCount) : amount
  if (splitMode.value === 'count') {
    for (let index = 0; index < fullCount; index++) {
      const isLast = index === fullCount - 1
      const initialQty = isLast ? remainder : fullQty
      addRow(initialQty, includeMother.value && index === 0)
    }
  } else if (fullCount === 0 && includeMother.value) {
    addRow(lot.value.curQty, true)
  } else {
    if (includeMother.value) addRow(fullQty, true)
    const generatedFullRows = Math.max(0, fullCount - motherRowCount)
    for (let index = 0; index < generatedFullRows; index++) addRow(fullQty, false)
    if (hasRemainder) addRow(remainder, false)
    if (!rows.length) addRow(lot.value.curQty, false)
  }

  // The legacy grid leaves Lot Qty and both SAP quantities blank for operator entry.
  // The fake preview follows that behavior; only the corresponding system-unit quantity is prefilled.
  splitRows.value = rows
  selectedRowNo.value = splitRows.value[0]?.splitLotNo || ''
  message.value = '已建立 ' + splitRows.value.length + ' 列草稿；請輸入各列 Lot Qty 與 SAP 數量後確認。'
}

function setRowField(row: SplitRow, key: 'splitLotQty' | 'splitSysQty' | 'sapPrdQty' | 'sapPcsQty', value: string): void {
  row[key] = value
  errorText.value = ''
  if (!lot.value) return
  if (splitRows.value.length !== 2 && key === 'splitLotQty') row.splitSysQty = String(convertToSystemQty(Number(value) || 0))
  if (splitRows.value.length === 2 && key === 'splitLotQty') {
    const other = splitRows.value.find(item => item !== row)
    if (other && Number.isFinite(Number(value))) {
      const remainderQty = lot.value.curQty - Number(value)
      other.splitLotQty = String(Math.max(0, remainderQty))
      row.splitSysQty = String(convertToSystemQty(Number(value)))
      other.splitSysQty = String(convertToSystemQty(Math.max(0, remainderQty)))
    }
  }
  if (splitRows.value.length === 2 && key === 'splitSysQty') {
    const other = splitRows.value.find(item => item !== row)
    if (other && Number.isFinite(Number(value))) other.splitSysQty = String(Math.max(0, lot.value.sysQty - Number(value)))
  }
  if (splitRows.value.length === 2 && key === 'sapPrdQty') {
    const other = splitRows.value.find(item => item !== row)
    if (other && Number.isFinite(Number(value))) other.sapPrdQty = String(Math.max(0, lot.value.sapPrdQty - Number(value)))
  }
}

function changeAction(row: SplitRow, value: string): void {
  row.action = Number(value) as SplitAction
  row.processNo = ''
  row.processVersion = ''
  row.opNo = ''
  row.nodeId = ''
}

function openPicker(mode: PickerMode, row?: SplitRow): void {
  if (!canEdit.value) return
  if (row) selectedRowNo.value = row.splitLotNo
  pickerSearch.value = ''
  pickerMode.value = mode
}

function chooseRoute(route: (typeof routeOptions)[number]): void {
  if (!activeRow.value) return
  activeRow.value.action = route.action
  activeRow.value.processNo = route.processNo
  activeRow.value.processVersion = route.processVersion
  activeRow.value.opNo = route.opNo
  activeRow.value.nodeId = route.nodeId
  pickerMode.value = null
}

function chooseProduct(product: (typeof products)[number]): void {
  if (!activeRow.value) return
  activeRow.value.productNo = product.productNo
  activeRow.value.productVersion = product.productVersion
  activeRow.value.unitNo = product.unitNo
  pickerMode.value = null
}

function closePicker(): void {
  pickerMode.value = null
  pickerSearch.value = ''
}

function confirmSplit(): void {
  errorText.value = ''
  if (!dev.value || !lot.value || !splitRows.value.length) {
    errorText.value = '請先查詢參考批並建立分批草稿。'
    return
  }
  for (const row of splitRows.value) {
    const qty = Number(row.splitLotQty)
    const sysQty = Number(row.splitSysQty)
    const prdQty = Number(row.sapPrdQty)
    const pcsQty = Number(row.sapPcsQty)
    if (![qty, sysQty, prdQty, pcsQty].every(value => Number.isFinite(value) && value > 0)) {
      errorText.value = row.splitLotNo + ' 的 Lot Qty、Split Sys Qty、SAP_PRDQTY、SAP_PCSQTY 都必須大於 0。'
      return
    }
    if (row.action !== 0 && !row.nodeId) {
      errorText.value = row.splitLotNo + ' 選擇跳站／跳流程後，必須指定目標節點。'
      return
    }
  }
  const equals = (left: number, right: number) => Math.abs(left - right) < 0.0001
  if (!equals(lotSum.value, lot.value.curQty)) {
    errorText.value = 'Split Lot Qty 加總需等於參考批 CurQty（' + lot.value.curQty + '）。'
    return
  }
  if (!equals(sysSum.value, lot.value.sysQty)) {
    errorText.value = 'Split Sys Qty 加總需等於參考批 SysQty（' + lot.value.sysQty + '）。'
    return
  }
  if (!equals(prdSum.value, lot.value.sapPrdQty)) {
    errorText.value = 'SAP_PRDQTY 加總需等於參考批原始值（' + lot.value.sapPrdQty + '）。'
    return
  }
  if (pcsSum.value < lot.value.sapPcsQty) {
    errorText.value = 'SAP_PCSQTY 加總不可小於參考批原始值（' + lot.value.sapPcsQty + '）。'
    return
  }
  if (lot.value.status === 5 && !lot.value.outsourceReturned) {
    errorText.value = '參考批為委外未回廠狀態，不能分批。'
    return
  }

  const currentSequence = Math.max(1, Number(window.localStorage.getItem(sequenceKey) || '1'))
  const childRows = splitRows.value.filter(row => !row.mother)
  if (currentSequence !== nextSequence.value || childRows.some((row, index) => row.splitLotNo !== makeChildLotNo(nextSequence.value + index))) {
    errorText.value = 'DEV 子批號已變動，請重新按 Split 建立草稿。'
    nextSequence.value = currentSequence
    return
  }
  const reservedRows = splitRows.value.map(row => ({ ...row }))
  nextSequence.value += childRows.length
  records.value.push({ lotNo: lot.value.lotNo, rows: reservedRows, savedAt: new Date().toISOString() })
  saveDevState()
  splitRows.value = reservedRows
  submitted.value = true
  currentTime.value = new Date().toLocaleTimeString('zh-TW', { hour12: false })
  message.value = 'DEV 已模擬拆批；僅保存至此瀏覽器，未保留正式批號或更新 MES。'
}

function setClock(): void {
  currentTime.value = new Date().toLocaleTimeString('zh-TW', { hour12: false })
}

onMounted(() => {
  setClock()
  readDevState()
})
</script>

<template>
  <section class="wp207-page">
    <div v-if="dev" class="dev-banner">DEV 假資料預覽；分批結果與序號僅寫入此瀏覽器 localStorage。</div>
    <h1>分批處理 <small>LOT SPLIT · WP207</small></h1>
    <p v-if="errorText" class="notice error" role="alert">{{ errorText }}</p>
    <p v-else class="notice" role="status">{{ message }}</p>

    <fieldset class="reference-frame">
      <legend>Reference Lot</legend>
      <div class="query-row">
        <label for="wp207-lot-no">Lot No</label>
        <input id="wp207-lot-no" v-model="lotNo" class="mes-input editable" :disabled="!dev || submitted" @keydown.enter.prevent="queryLot">
        <button type="button" class="mes-button query-button" :disabled="!dev || submitted" @click="queryLot">Query</button>
      </div>
      <div class="grid-wrap reference-grid">
        <table>
          <thead><tr><th>Lot No</th><th>Base Lot No</th><th>Cur Qty</th><th>Cur Unit</th><th>Sys Qty</th><th>Sys Unit</th><th>Product No</th><th>Version</th><th>Process</th><th>OP No</th><th>Status</th></tr></thead>
          <tbody>
            <tr v-if="sourceLotPreview">
              <td>{{ sourceLotPreview.lotNo }}</td><td>{{ sourceLotPreview.baseLotNo }}</td><td class="number-cell">{{ sourceLotPreview.curQty }}</td><td>{{ sourceLotPreview.curUnitNo }}</td><td class="number-cell">{{ sourceLotPreview.sysQty }}</td><td>{{ sourceLotPreview.sysUnitNo }}</td><td>{{ sourceLotPreview.productNo }}</td><td>{{ sourceLotPreview.productVersion }}</td><td>{{ sourceLotPreview.processNo }} / {{ sourceLotPreview.processVersion }}</td><td>{{ sourceLotPreview.opNo }}</td><td>{{ sourceLotPreview.statusName }}</td>
            </tr>
            <tr v-else><td colspan="11" class="empty-row">查詢參考批後顯示資料。</td></tr>
          </tbody>
        </table>
      </div>
    </fieldset>

    <fieldset class="split-frame">
      <legend>Split</legend>
      <div class="split-toolbar">
        <label class="right-label">Split Type</label>
        <fieldset class="radio-frame"><label><input v-model="splitMode" type="radio" value="count" :disabled="!canEdit">Split by Count</label><label><input v-model="splitMode" type="radio" value="qty" :disabled="!canEdit">Split by Qty</label></fieldset>
        <label class="right-label" for="wp207-amount">Split Count / Qty</label>
        <input id="wp207-amount" v-model="splitAmount" class="mes-input editable amount-input" :disabled="!canEdit" inputmode="decimal">
        <button type="button" class="mes-button" :disabled="!canEdit" @click="buildSplitRows">Split</button>
        <span class="toolbar-spacer"></span>
        <button type="button" class="mes-button" :disabled="!canEdit || !activeRow" @click="openPicker('routing')">Change Routing</button>
        <button v-if="icFtPkgEnabled" type="button" class="mes-button" :disabled="!canEdit" @click="openPicker('electricbox')">Electricbox</button>
      </div>
      <div class="mother-row"><span>Include Mother Lot No</span><label><input v-model="includeMother" type="radio" :value="true" :disabled="!canEdit">Yes</label><label class="disabled-radio"><input type="radio" :checked="!includeMother" disabled>No</label></div>
      <div class="grid-wrap split-grid">
        <table>
          <thead><tr><th>Split Lot No</th><th>Split Lot Qty</th><th>Split Sys Qty</th><th>Action</th><th>Process No</th><th>Process Ver.</th><th>OP No</th><th>Product No</th><th>Product Ver.</th><th>Unit No</th><th>SAP_PRDQTY</th><th>SAP_PCSQTY</th></tr></thead>
          <tbody>
            <tr v-for="row in splitRows" :key="row.splitLotNo" :class="{ selected: row.splitLotNo === selectedRowNo }" @click="selectedRowNo = row.splitLotNo">
              <td class="lot-cell"><span>{{ row.splitLotNo }}</span><small v-if="row.mother">母批</small></td>
              <td><input class="grid-input editable number-cell" :value="row.splitLotQty" :disabled="!canEdit" inputmode="decimal" @input="setRowField(row, 'splitLotQty', ($event.target as HTMLInputElement).value)" /></td>
              <td><input class="grid-input number-cell" :class="{ readonly: !canEditSystemQty }" :value="row.splitSysQty" :disabled="!canEditSystemQty" inputmode="decimal" @input="setRowField(row, 'splitSysQty', ($event.target as HTMLInputElement).value)" /></td>
              <td><select class="grid-select" :value="row.action" :disabled="!canEdit" @change="changeAction(row, ($event.target as HTMLSelectElement).value)"><option value="0">Go</option><option value="1">Jump OP</option><option value="2">Jump Process</option></select></td>
              <td>{{ row.processNo }}</td><td>{{ row.processVersion }}</td><td>{{ row.opNo }}</td>
              <td><button type="button" class="cell-button" :disabled="!canEdit" @click.stop="openPicker('product', row)">{{ row.productNo }}</button></td><td>{{ row.productVersion }}</td><td>{{ row.unitNo }}</td>
              <td><input class="grid-input editable number-cell" :value="row.sapPrdQty" :disabled="!canEdit" inputmode="decimal" @input="setRowField(row, 'sapPrdQty', ($event.target as HTMLInputElement).value)" /></td>
              <td><input class="grid-input editable number-cell" :value="row.sapPcsQty" :disabled="!canEdit" inputmode="decimal" @input="setRowField(row, 'sapPcsQty', ($event.target as HTMLInputElement).value)" /></td>
            </tr>
            <tr v-if="!splitRows.length"><td colspan="12" class="empty-row">設定 Split Count / Qty 後按 Split 建立分批列。</td></tr>
          </tbody>
        </table>
      </div>
      <div class="totals-row" v-if="splitRows.length">
        <span>Lot Qty 合計 <strong>{{ lotSum }}</strong><em>/ {{ lot?.curQty }}</em></span>
        <span>Sys Qty 合計 <strong>{{ sysSum }}</strong><em>/ {{ lot?.sysQty }}</em></span>
        <span>SAP_PRDQTY 合計 <strong>{{ prdSum }}</strong><em>/ {{ lot?.sapPrdQty }}</em></span>
        <span>SAP_PCSQTY 合計 <strong>{{ pcsSum }}</strong><em>/ ≥ {{ lot?.sapPcsQty }}</em></span>
      </div>
      <div class="split-status">{{ splitRows.length }} row(s) · {{ currentTime }}</div>
    </fieldset>

    <footer class="form-footer">
      <span class="footer-note">{{ submitted ? 'DEV 模擬完成' : '確認前會檢查數量守恆、SAP 數量及跳站目標。' }}</span>
      <button type="button" class="mes-button confirm-button" :disabled="!canEdit || !splitRows.length" @click="confirmSplit">Confirm(&amp;C)</button>
      <button type="button" class="mes-button" @click="emit('close')">Close (&amp;X)</button>
    </footer>

    <div v-if="pickerMode" class="modal-backdrop" @click.self="closePicker">
      <section v-if="pickerMode === 'routing'" class="picker-dialog" role="dialog" aria-modal="true" aria-label="Change Routing">
        <header><strong>Change Routing</strong><button type="button" class="mes-button" @click="closePicker">Close</button></header>
        <p>請選擇子批後續執行的作業站或流程。</p>
        <button v-for="route in routeOptions" :key="route.nodeId" type="button" class="picker-row" @click="chooseRoute(route)">{{ route.label }}<small>{{ route.processNo }} / {{ route.processVersion }} · {{ route.nodeId }}</small></button>
      </section>
      <section v-else-if="pickerMode === 'product'" class="picker-dialog" role="dialog" aria-modal="true" aria-label="Change Product">
        <header><strong>Change Product</strong><button type="button" class="mes-button" @click="closePicker">Close</button></header>
        <input v-model="pickerSearch" class="mes-input picker-search" placeholder="Product No / Version">
        <button v-for="product in filteredProducts" :key="product.productNo + product.productVersion" type="button" class="picker-row" @click="chooseProduct(product)"><strong>{{ product.productNo }} / {{ product.productVersion }}</strong><small>{{ product.productName }} · {{ product.unitNo }}</small></button>
      </section>
      <section v-else class="picker-dialog electricbox-dialog" role="dialog" aria-modal="true" aria-label="Electricbox">
        <header><strong>Electricbox</strong><button type="button" class="mes-button" @click="closePicker">Close</button></header>
        <p>選擇靜電箱綁定作業；此視窗提供 DEV 模擬。</p>
        <label>Reference Lot<input class="mes-input readonly" :value="lot?.lotNo || ''" readonly></label>
        <button type="button" class="mes-button confirm-button" @click="message = 'DEV 靜電箱綁定視窗已模擬；沒有寫入設備。'; closePicker()">模擬確認</button>
      </section>
    </div>
  </section>
</template>

<style scoped>
.wp207-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:7px;padding:9px;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif;box-sizing:border-box}.dev-banner{padding:6px 9px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}.wp207-page h1{margin:0;font-size:18px;font-weight:600}.wp207-page h1 small{margin-left:8px;color:#777;font-size:12px;font-weight:400}.notice{margin:0;min-height:18px;color:#555}.notice.error{padding:5px 8px;border:1px solid #d6aaa6;background:#fff0ef;color:#99251e}.reference-frame,.split-frame{min-width:0;margin:0;border:1px solid #bdbdbd;background:#fff;box-sizing:border-box}.reference-frame{height:178px;flex:0 0 178px;padding:5px 8px 8px}.split-frame{flex:1;min-height:230px;display:flex;flex-direction:column;padding:5px 8px 8px}.reference-frame legend,.split-frame legend{padding:0 5px;color:#444}.query-row{height:29px;display:flex;align-items:center;gap:7px}.query-row label{width:82px;text-align:right}.mes-input{height:25px;padding:3px 6px;border:1px solid #888;border-radius:2px;background:#fff;color:#333;font:inherit;box-sizing:border-box}.editable{background:#fff0b3}.readonly{background:#eee;color:#555}.mes-button{min-height:29px;padding:3px 11px;border:1px solid #b9b9b9;border-radius:3px;background:linear-gradient(#fff,#f3f3f3);color:#333;font:inherit;white-space:nowrap;cursor:pointer}.mes-button:hover:not(:disabled){background:#fff5dc}.mes-button:disabled{color:#999;cursor:default}.query-button{min-width:58px}.grid-wrap{min-width:0;overflow:auto;border:1px solid #a7a7a7;background:#fff}.reference-grid{height:112px}.grid-wrap table{width:100%;border-collapse:collapse;white-space:nowrap}.grid-wrap th{position:sticky;top:0;z-index:1;padding:5px 7px;border:1px solid #d4d4d4;background:#ead591;color:#393326;font-weight:500;text-align:left}.grid-wrap td{padding:3px 6px;border:1px solid #d2d2d2;background:#ffe89c}.grid-wrap tbody tr.selected td{background:#ffecad}.number-cell{text-align:right;font-variant-numeric:tabular-nums}.empty-row{text-align:center!important;color:#777!important;background:#fff!important;height:45px}.split-toolbar{min-height:38px;display:flex;align-items:center;gap:7px;flex-wrap:wrap}.right-label{min-width:90px;text-align:right}.radio-frame{display:flex;gap:9px;margin:0;padding:4px 8px;border:1px solid #ccc}.radio-frame label,.mother-row label{display:flex;align-items:center;gap:3px;white-space:nowrap}.amount-input{width:96px;text-align:right}.toolbar-spacer{flex:1}.mother-row{min-height:31px;display:flex;align-items:center;gap:14px;padding-left:6px}.mother-row>span:first-child{width:150px;text-align:right}.disabled-radio{color:#999}.source-note{color:#777;font-size:11px}.split-grid{flex:1;min-height:105px}.split-grid th{padding:5px 6px}.split-grid td{height:31px;padding:2px 4px}.split-grid tbody tr{cursor:pointer}.split-grid tbody tr:hover td{background:#fff0bd}.split-grid tbody tr.selected td{background:#ffedb3}.lot-cell{display:flex;align-items:center;gap:6px}.lot-cell small{padding:1px 4px;border:1px solid #d2ad55;border-radius:2px;color:#765e20;background:#fff4c7}.grid-input,.grid-select{width:100%;min-width:78px;height:25px;border:1px solid #999;background:#fff;color:#333;font:inherit;box-sizing:border-box}.grid-input.editable{background:#fff0b3}.grid-select{min-width:88px}.cell-button{padding:2px 4px;border:0;background:transparent;color:#315d88;font:inherit;text-decoration:underline;cursor:pointer}.cell-button:disabled{color:#777;text-decoration:none;cursor:default}.totals-row{min-height:28px;display:flex;align-items:center;gap:16px;flex-wrap:wrap;color:#555}.totals-row strong{margin-left:4px;color:#333}.totals-row em{margin-left:4px;color:#777;font-style:normal}.split-status{min-height:21px;padding:3px 6px;border:1px solid #c5c5c5;background:#eee;color:#555;text-align:right;box-sizing:border-box}.form-footer{min-height:36px;display:flex;align-items:center;justify-content:flex-end;gap:8px}.footer-note{margin-right:auto;color:#777}.confirm-button{min-width:86px;color:#fff;background:#f3a62a;border-color:#e99512}.confirm-button:hover:not(:disabled){background:#e99a1b}.modal-backdrop{position:fixed;inset:0;z-index:1300;display:grid;place-items:center;padding:18px;background:#0005}.picker-dialog{width:min(560px,94vw);max-height:84vh;display:flex;flex-direction:column;gap:10px;padding:14px;border:1px solid #999;background:#fff;box-shadow:0 8px 28px #0004;box-sizing:border-box}.picker-dialog header{display:flex;align-items:center;justify-content:space-between}.picker-dialog p{margin:0;color:#666}.picker-row{display:flex;flex-direction:column;gap:4px;padding:9px;border:1px solid #ccc;background:#fff;text-align:left;color:#333;font:inherit;cursor:pointer}.picker-row:hover{background:#fff3cc}.picker-row small{color:#666}.picker-search{width:100%}.electricbox-dialog{width:min(380px,94vw)}.electricbox-dialog label{display:grid;gap:5px}@media(max-width:720px){.reference-frame{height:150px;flex-basis:150px}.reference-grid{height:86px}.split-toolbar{gap:5px}.right-label{min-width:78px}.radio-frame{gap:4px}.mother-row{gap:8px;flex-wrap:wrap}.mother-row>span:first-child{width:auto}.source-note{width:100%}.totals-row{gap:7px;font-size:11px}.form-footer{flex-wrap:wrap}.footer-note{width:100%}}
</style>
