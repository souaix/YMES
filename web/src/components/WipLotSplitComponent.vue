<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type SplitAction = 0 | 1 | 2
type ReturnType = 0 | 1 | 2
type Lot = {
  lotNo: string
  baseLotNo: string
  orgLotNo: string
  curQty: number
  productNo: string
  productVersion: string
  processNo: string
  processVersion: string
  opNo: string
  opName: string
  status: number
  statusName: string
  lotStamp: number
  carrier: boolean
  components: string[]
}
type SplitRow = {
  splitLotNo: string
  splitLotQty: number
  action: SplitAction
  returnType: ReturnType
  processNo: string
  processVersion: string
  opNo: string
  nodeId: string
  productNo: string
  productVersion: string
  mother: boolean
}
type ComponentAssignment = { componentNo: string; splitLotNo: string }
type SplitRecord = {
  lotNo: string
  lotStamp: number
  rows: SplitRow[]
  assignments: ComponentAssignment[]
  savedAt: string
}

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview !== false)
const storageKey = 'ymes-dev-mock:wip-lot-split-component:records:v1'
const sequenceKey = 'ymes-dev-mock:wip-lot-split-component:sequence:v1'
const unassigned = 'N/A'

const lotFixtures: Lot[] = [
  { lotNo: 'DEV-LOT-COMP-001', baseLotNo: 'DEV-LOT-COMP-001', orgLotNo: 'DEV-ORG-001', curQty: 8, productNo: 'DEV-PRODUCT-A', productVersion: '01', processNo: 'DEV-PROC-ASSY', processVersion: '02', opNo: 'DEV-OP-ASSY-10', opName: '組裝', status: 1, statusName: 'Running', lotStamp: 1801, carrier: false, components: Array.from({ length: 8 }, (_, i) => `DEV-COMP-A${String(i + 1).padStart(3, '0')}`) },
  { lotNo: 'DEV-LOT-COMP-002', baseLotNo: 'DEV-LOT-COMP-002', orgLotNo: 'DEV-ORG-002', curQty: 6, productNo: 'DEV-PRODUCT-B', productVersion: '01', processNo: 'DEV-PROC-PACK', processVersion: '01', opNo: 'DEV-OP-PACK-20', opName: '包裝', status: 0, statusName: 'Queue', lotStamp: 1802, carrier: false, components: Array.from({ length: 6 }, (_, i) => `DEV-COMP-B${String(i + 1).padStart(3, '0')}`) },
  { lotNo: 'DEV-LOT-COMP-COMPLETE', baseLotNo: 'DEV-LOT-COMP-COMPLETE', orgLotNo: 'DEV-ORG-003', curQty: 4, productNo: 'DEV-PRODUCT-A', productVersion: '01', processNo: 'DEV-PROC-ASSY', processVersion: '02', opNo: 'DEV-OP-ASSY-10', opName: '組裝', status: 4, statusName: 'Complete', lotStamp: 1803, carrier: false, components: ['DEV-COMP-C001', 'DEV-COMP-C002', 'DEV-COMP-C003', 'DEV-COMP-C004'] },
  { lotNo: 'DEV-LOT-COMP-CARRIER', baseLotNo: 'DEV-LOT-COMP-CARRIER', orgLotNo: 'DEV-ORG-004', curQty: 4, productNo: 'DEV-PRODUCT-A', productVersion: '01', processNo: 'DEV-PROC-ASSY', processVersion: '02', opNo: 'DEV-OP-ASSY-10', opName: '組裝', status: 1, statusName: 'Running', lotStamp: 1804, carrier: true, components: ['DEV-COMP-D001', 'DEV-COMP-D002', 'DEV-COMP-D003', 'DEV-COMP-D004'] },
]
const routeOptions = [
  { action: 1 as SplitAction, processNo: 'DEV-PROC-ASSY', processVersion: '02', opNo: 'DEV-OP-ASSY-30', nodeId: 'DEV-NODE-ASSY-30', label: 'Jump OP · DEV-OP-ASSY-30' },
  { action: 2 as SplitAction, processNo: 'DEV-PROC-PACK', processVersion: '01', opNo: 'DEV-OP-PACK-20', nodeId: 'DEV-NODE-PACK-20', label: 'Jump Process · DEV-PROC-PACK' },
]
const products = [
  { productNo: 'DEV-PRODUCT-A', productVersion: '01', name: 'DEV 產品 A' },
  { productNo: 'DEV-PRODUCT-B', productVersion: '01', name: 'DEV 產品 B' },
]
// This DEV fixture represents AllowLotProductChange=1. Production must read the server parameter.
const allowProductChange = computed(() => dev.value)

const lotNoInput = ref('')
const lot = ref<Lot | null>(null)
const splitMode = ref<'count' | 'qty'>('count')
const splitAmount = ref('2')
const includeMother = ref(false)
const rows = ref<SplitRow[]>([])
const assignments = ref<ComponentAssignment[]>([])
const draftSettings = ref('')
const selectedLotNo = ref('')
const picker = ref<'component' | 'routing' | 'product' | null>(null)
const pickerChecks = ref<Record<string, boolean>>({})
const message = ref('請輸入有元件的參考批號，查詢後建立分批草稿。')
const errorText = ref('')
const submitted = ref(false)
const records = ref<SplitRecord[]>([])
const nextSequence = ref(1)

const activeRow = computed(() => rows.value.find(row => row.splitLotNo === selectedLotNo.value) ?? null)
const canEdit = computed(() => dev.value && lot.value !== null && !submitted.value)
const visibleAssignments = computed(() => assignments.value.filter(item => item.splitLotNo === selectedLotNo.value))
const pickerCandidates = computed(() => assignments.value.filter(item => item.splitLotNo === unassigned || item.splitLotNo === selectedLotNo.value))
const assignedCount = computed(() => assignments.value.filter(item => item.splitLotNo !== unassigned).length)
const assignedTotal = computed(() => rows.value.reduce((sum, row) => sum + row.splitLotQty, 0))
const allCandidatesChecked = computed(() => pickerCandidates.value.length > 0 && pickerCandidates.value.every(item => pickerChecks.value[item.componentNo]))

function readState(): void {
  if (!dev.value) return
  try {
    const stored = JSON.parse(window.localStorage.getItem(storageKey) || '[]') as SplitRecord[]
    records.value = Array.isArray(stored) ? stored : []
    nextSequence.value = Math.max(1, Number(window.localStorage.getItem(sequenceKey) || '1') || 1)
  } catch {
    records.value = []
    nextSequence.value = 1
  }
}

function saveState(): void {
  if (!dev.value) return
  window.localStorage.setItem(storageKey, JSON.stringify(records.value))
  window.localStorage.setItem(sequenceKey, String(nextSequence.value))
}

function clearDraft(): void {
  rows.value = []
  assignments.value = []
  draftSettings.value = ''
  selectedLotNo.value = ''
  picker.value = null
  submitted.value = false
}

function queryLot(): void {
  errorText.value = ''
  clearDraft()
  if (!dev.value) {
    errorText.value = '此頁目前僅提供 DEV 假資料預覽。'
    return
  }
  const key = lotNoInput.value.trim().toUpperCase()
  const found = lotFixtures.find(item => item.lotNo === key)
  if (!found) {
    lot.value = null
    errorText.value = '找不到有元件的 DEV Lot。可輸入 DEV-LOT-COMP-001。'
    return
  }
  if (found.status === 4) {
    lot.value = null
    errorText.value = 'Complete 狀態不允許分批。'
    return
  }
  if (found.carrier) {
    lot.value = null
    errorText.value = '此 Lot 在載具中，請使用 LOT SPLIT_CARRIER。'
    return
  }
  if (found.curQty <= 1) {
    lot.value = null
    errorText.value = 'CurQty 為 1，不允許分批。'
    return
  }
  if (records.value.some(record => record.lotNo === found.lotNo && record.lotStamp === found.lotStamp)) {
    lot.value = null
    errorText.value = '此 DEV Lot 已在本瀏覽器模擬分批。請使用另一筆假資料。'
    return
  }
  lot.value = { ...found, components: [...found.components] }
  lotNoInput.value = found.lotNo
  assignments.value = found.components.map(componentNo => ({ componentNo, splitLotNo: unassigned }))
  message.value = `已載入 ${found.components.length} 個元件；請按 Split 建立子批。`
}

function childLotNo(sequence: number): string {
  return `DEV-COMP-SPLIT-${String(sequence).padStart(6, '0')}`
}

function buildRows(): void {
  errorText.value = ''
  if (!canEdit.value || !lot.value) {
    errorText.value = '請先查詢有效的參考批。'
    return
  }
  const amount = Number(splitAmount.value)
  if (!Number.isFinite(amount) || amount <= 0) {
    errorText.value = 'Split Count / Qty 必須大於 0。'
    return
  }
  if (splitMode.value === 'count' && !Number.isInteger(amount)) {
    errorText.value = 'Split by Count 必須輸入整數。'
    return
  }
  if (splitMode.value === 'qty' && !Number.isInteger(amount)) {
    errorText.value = '此 DEV 元件假資料以每件 1 Qty 模擬，Split by Qty 請輸入整數。'
    return
  }
  const quantities: number[] = []
  if (splitMode.value === 'count') {
    if (amount < 2 || amount > lot.value.curQty) {
      errorText.value = `批數須介於 2 與 CurQty ${lot.value.curQty} 之間。`
      return
    }
    const base = Math.floor(lot.value.curQty / amount)
    for (let index = 0; index < amount; index++) quantities.push(index === amount - 1 ? lot.value.curQty - base * (amount - 1) : base)
  } else {
    if (amount >= lot.value.curQty) {
      errorText.value = 'Split Qty 必須小於參考批 CurQty，且至少產生兩列。'
      return
    }
    const full = Math.floor(lot.value.curQty / amount)
    const remainder = lot.value.curQty - full * amount
    for (let index = 0; index < full; index++) quantities.push(amount)
    if (remainder > 0) quantities.push(remainder)
  }
  if (quantities.length < 2 || quantities.length > 100) {
    errorText.value = 'DEV 預覽請產生 2 至 100 列。'
    return
  }
  const source = lot.value
  let sequence = nextSequence.value
  rows.value = quantities.map((qty, index) => {
    const mother = includeMother.value && index === 0
    return {
      splitLotNo: mother ? source.lotNo : childLotNo(sequence++),
      splitLotQty: qty,
      action: 0 as SplitAction,
      returnType: 0 as ReturnType,
      processNo: '', processVersion: '', opNo: '', nodeId: '',
      productNo: source.productNo, productVersion: source.productVersion,
      mother,
    }
  })
  assignments.value = source.components.map(componentNo => ({ componentNo, splitLotNo: unassigned }))
  draftSettings.value = `${splitMode.value}|${splitAmount.value}|${includeMother.value}`
  selectedLotNo.value = rows.value[0]?.splitLotNo || ''
  message.value = `已建立 ${rows.value.length} 列；逐列按 Select Comp 分配元件。`
}

function changeAction(row: SplitRow, value: string): void {
  row.action = Number(value) as SplitAction
  row.processNo = ''
  row.processVersion = ''
  row.opNo = ''
  row.nodeId = ''
  errorText.value = ''
}

function changeReturnType(row: SplitRow, value: string): void {
  row.returnType = Number(value) as ReturnType
  errorText.value = ''
}

function openComponentPicker(): void {
  if (!canEdit.value || !activeRow.value) return
  pickerChecks.value = Object.fromEntries(pickerCandidates.value.map(item => [item.componentNo, item.splitLotNo === selectedLotNo.value]))
  picker.value = 'component'
  errorText.value = ''
}

function toggleAllCandidates(): void {
  const checked = !allCandidatesChecked.value
  pickerChecks.value = Object.fromEntries(pickerCandidates.value.map(item => [item.componentNo, checked]))
}

function confirmComponentPicker(): void {
  if (!lot.value || !activeRow.value) return
  const selected = pickerCandidates.value.filter(item => pickerChecks.value[item.componentNo])
  if (selected.length === lot.value.curQty) {
    errorText.value = '選取的元件數必須小於母批 CurQty。'
    return
  }
  const candidateNumbers = new Set(pickerCandidates.value.map(item => item.componentNo))
  assignments.value = assignments.value.map(item => candidateNumbers.has(item.componentNo)
    ? { ...item, splitLotNo: pickerChecks.value[item.componentNo] ? selectedLotNo.value : unassigned }
    : item)
  activeRow.value.splitLotQty = selected.length
  picker.value = null
  errorText.value = ''
  message.value = `${selectedLotNo.value} 已分配 ${selected.length} 個元件。`
}

function chooseRoute(option: (typeof routeOptions)[number]): void {
  if (!activeRow.value) return
  activeRow.value.action = option.action
  activeRow.value.processNo = option.processNo
  activeRow.value.processVersion = option.processVersion
  activeRow.value.opNo = option.opNo
  activeRow.value.nodeId = option.nodeId
  picker.value = null
}

function chooseProduct(option: (typeof products)[number]): void {
  if (!activeRow.value) return
  activeRow.value.productNo = option.productNo
  activeRow.value.productVersion = option.productVersion
  // WinForms ReturnSelectProcess clears routing/action when the product changes.
  changeAction(activeRow.value, '0')
  picker.value = null
}

function confirmSplit(): void {
  errorText.value = ''
  if (!dev.value || !lot.value || rows.value.length < 2 || submitted.value) {
    errorText.value = '請先查詢參考批並建立分批草稿。'
    return
  }
  if (draftSettings.value !== `${splitMode.value}|${splitAmount.value}|${includeMother.value}`) {
    errorText.value = '分批條件已變更，請重新按 Split 建立草稿。'
    return
  }
  for (const row of rows.value) {
    const count = assignments.value.filter(item => item.splitLotNo === row.splitLotNo).length
    if (count === 0 || count !== row.splitLotQty) {
      errorText.value = `${row.splitLotNo} 的 Split Lot Qty 必須等於已選元件數，且不可為 0。`
      return
    }
    if ((row.action === 0 || row.action === 1) && row.returnType !== 0) {
      errorText.value = `${row.splitLotNo} 的 Go／Jump OP 只能選 Not Return。`
      return
    }
    if (row.action !== 0 && !row.nodeId) {
      errorText.value = `${row.splitLotNo} 跳站／跳流程時必須指定 NodeID；請使用 Change Routing。`
      return
    }
  }
  if (assignedCount.value !== lot.value.curQty || assignedTotal.value !== lot.value.curQty) {
    errorText.value = `所有 ${lot.value.curQty} 個元件須恰好分配至子批，且數量合計一致。`
    return
  }
  const currentSequence = Math.max(1, Number(window.localStorage.getItem(sequenceKey) || '1') || 1)
  const children = rows.value.filter(row => !row.mother)
  if (currentSequence !== nextSequence.value || children.some((row, index) => row.splitLotNo !== childLotNo(currentSequence + index))) {
    nextSequence.value = currentSequence
    errorText.value = 'DEV 子批預覽號已變動，請重新按 Split。'
    return
  }
  if (records.value.some(record => record.lotNo === lot.value!.lotNo && record.lotStamp === lot.value!.lotStamp)) {
    errorText.value = '此 DEV Lot 已模擬提交，請改查其他批號。'
    return
  }
  records.value.push({ lotNo: lot.value.lotNo, lotStamp: lot.value.lotStamp, rows: rows.value.map(row => ({ ...row })), assignments: assignments.value.map(item => ({ ...item })), savedAt: new Date().toISOString() })
  nextSequence.value += children.length
  saveState()
  submitted.value = true
  message.value = 'DEV 已模擬分批；元件對應只保存在此瀏覽器，沒有更新 MES。'
}

onMounted(readState)
</script>

<template>
  <section class="wp208-page">
    <div v-if="dev" class="dev-banner">DEV 假資料預覽；元件分配與序號只寫入此瀏覽器 localStorage。</div>
    <h1>分批處理_元件 <small>LOT SPLIT_COMP · WP208</small></h1>
    <p v-if="errorText" class="notice error" role="alert">{{ errorText }}</p>
    <p v-else class="notice" role="status">{{ message }}</p>

    <fieldset class="reference-frame">
      <legend>MLot</legend>
      <div class="query-row">
        <label for="wp208-lot">Lot No</label>
        <input id="wp208-lot" v-model="lotNoInput" class="mes-input editable" :disabled="!dev" @keydown.enter.prevent="queryLot">
        <button type="button" class="mes-button" :disabled="!dev" @click="queryLot">Query</button>
      </div>
      <div class="grid-wrap reference-grid">
        <table>
          <thead><tr><th>Lot No</th><th>Cur Qty</th><th>Process No</th><th>OP No</th><th>OP Name</th><th>Base Lot No</th><th>Org Lot No</th><th>Product No</th><th>Product Version</th><th>Status</th></tr></thead>
          <tbody>
            <tr v-if="lot"><td>{{ lot.lotNo }}</td><td class="number">{{ lot.curQty }}</td><td>{{ lot.processNo }}</td><td>{{ lot.opNo }}</td><td>{{ lot.opName }}</td><td>{{ lot.baseLotNo }}</td><td>{{ lot.orgLotNo }}</td><td>{{ lot.productNo }}</td><td>{{ lot.productVersion }}</td><td>{{ lot.statusName }}</td></tr>
            <tr v-else><td colspan="10" class="empty-row">請輸入 Lot No 並按 Query。</td></tr>
          </tbody>
        </table>
      </div>
    </fieldset>

    <fieldset class="split-frame">
      <legend>Split</legend>
      <div class="toolbar-row">
        <span class="field-label">Split Type</span>
        <fieldset class="radio-frame"><label><input v-model="splitMode" type="radio" value="count" :disabled="!canEdit">Split by Count</label><label><input v-model="splitMode" type="radio" value="qty" :disabled="!canEdit">Split by Qty</label></fieldset>
        <label for="wp208-amount">Split Count / Qty</label>
        <input id="wp208-amount" v-model="splitAmount" class="mes-input editable amount" inputmode="numeric" :disabled="!canEdit">
      </div>
      <div class="toolbar-row second-row">
        <span class="field-label">Include Mother Lot No</span>
        <fieldset class="radio-frame"><label><input v-model="includeMother" type="radio" :value="true" :disabled="!canEdit">Yes</label><label><input v-model="includeMother" type="radio" :value="false" :disabled="!canEdit">No</label></fieldset>
        <span class="spacer"></span>
        <button type="button" class="mes-button" :disabled="!canEdit" @click="buildRows">Split</button>
        <button type="button" class="mes-button" :disabled="!canEdit || !activeRow" @click="openComponentPicker">Select Comp</button>
        <button type="button" class="mes-button" :disabled="!canEdit || !activeRow" @click="picker = 'routing'">Change Routing</button>
      </div>
      <div class="split-grids">
        <div class="grid-wrap split-grid">
          <table>
            <thead><tr><th>Split Lot No</th><th>Split Lot Qty</th><th>Action</th><th>Process No</th><th>Process Ver.</th><th>OP No</th><th>Return Type</th><th>Product No</th><th>Product Ver.</th></tr></thead>
            <tbody>
              <tr v-for="row in rows" :key="row.splitLotNo" :class="{ selected: row.splitLotNo === selectedLotNo }" @click="selectedLotNo = row.splitLotNo">
                <td>{{ row.splitLotNo }} <small v-if="row.mother">母批</small></td><td class="number readonly-cell">{{ row.splitLotQty }}</td>
                <td><select class="grid-select" :value="row.action" :disabled="!canEdit" @change="changeAction(row, ($event.target as HTMLSelectElement).value)"><option value="0">Go</option><option value="1">Jump OP</option><option value="2">Jump Process</option></select></td>
                <td class="readonly-cell">{{ row.processNo }}</td><td class="readonly-cell">{{ row.processVersion }}</td><td class="readonly-cell">{{ row.opNo }}</td>
                <td><select class="grid-select" :value="row.returnType" :disabled="!canEdit" @change="changeReturnType(row, ($event.target as HTMLSelectElement).value)"><option value="0">Not Return</option><option value="1">Return to Current OP</option><option value="2">Return to Next OP</option></select></td>
                <td><button v-if="allowProductChange" type="button" class="cell-button" :disabled="!canEdit" @click.stop="selectedLotNo = row.splitLotNo; picker = 'product'">{{ row.productNo }}</button><span v-else>{{ row.productNo }}</span></td>
                <td class="readonly-cell">{{ row.productVersion }}</td>
              </tr>
              <tr v-if="!rows.length"><td colspan="9" class="empty-row">按 Split 建立分批列。</td></tr>
            </tbody>
          </table>
        </div>
        <div class="grid-wrap component-grid">
          <table>
            <thead><tr><th>Component No</th></tr></thead>
            <tbody><tr v-for="item in visibleAssignments" :key="item.componentNo"><td>{{ item.componentNo }}</td></tr><tr v-if="!visibleAssignments.length"><td class="empty-row">選取子批後顯示已分配元件。</td></tr></tbody>
          </table>
        </div>
      </div>
      <div class="split-status">{{ rows.length }} row(s) · 已分配 {{ assignedCount }} / {{ lot?.curQty ?? 0 }} Component</div>
    </fieldset>

    <footer class="form-footer">
      <span class="footer-note">{{ submitted ? 'DEV 模擬完成' : '確認前檢查每列元件、數量及路由。' }}</span>
      <button type="button" class="mes-button confirm-button" :disabled="!canEdit || rows.length === 0" @click="confirmSplit">Confirm</button>
      <button type="button" class="mes-button" @click="emit('close')">Close</button>
    </footer>

    <div v-if="picker" class="modal-backdrop" @click.self="picker = null">
      <section v-if="picker === 'component'" class="picker-dialog" role="dialog" aria-modal="true" aria-label="Lot Split Component Select">
        <header><strong>Lot Split Component Select</strong><span>Component of SplitLotNo: {{ selectedLotNo }}</span></header>
        <label class="select-all"><input type="checkbox" :checked="allCandidatesChecked" @change="toggleAllCandidates">Select All</label>
        <div class="picker-grid"><table><thead><tr><th>Check</th><th>Component No</th></tr></thead><tbody><tr v-for="item in pickerCandidates" :key="item.componentNo"><td><input v-model="pickerChecks[item.componentNo]" type="checkbox"></td><td>{{ item.componentNo }}</td></tr><tr v-if="!pickerCandidates.length"><td colspan="2" class="empty-row">目前沒有可指派元件。</td></tr></tbody></table></div>
        <footer><button type="button" class="mes-button confirm-button" @click="confirmComponentPicker">Confirm</button><button type="button" class="mes-button" @click="picker = null">Close</button></footer>
      </section>
      <section v-else-if="picker === 'routing'" class="picker-dialog" role="dialog" aria-modal="true" aria-label="Change Routing">
        <header><strong>Change Routing</strong><span>{{ selectedLotNo }}</span></header>
        <button v-for="option in routeOptions" :key="option.nodeId" type="button" class="picker-option" @click="chooseRoute(option)">{{ option.label }}<small>{{ option.processNo }} / {{ option.processVersion }} · {{ option.nodeId }}</small></button>
        <footer><button type="button" class="mes-button" @click="picker = null">Close</button></footer>
      </section>
      <section v-else class="picker-dialog" role="dialog" aria-modal="true" aria-label="Change Product">
        <header><strong>Change Product</strong><span>{{ selectedLotNo }}</span></header>
        <button v-for="option in products" :key="option.productNo + option.productVersion" type="button" class="picker-option" @click="chooseProduct(option)">{{ option.productNo }} / {{ option.productVersion }}<small>{{ option.name }}</small></button>
        <footer><button type="button" class="mes-button" @click="picker = null">Close</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.wp208-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:7px;padding:9px;box-sizing:border-box;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif}
.dev-banner{padding:6px 9px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}
h1{margin:0;font-size:18px;font-weight:600}h1 small{margin-left:8px;color:#777;font-size:12px;font-weight:400}
.notice{margin:0;min-height:18px;color:#555}.notice.error{padding:5px 8px;border:1px solid #d6aaa6;background:#fff0ef;color:#99251e}
.reference-frame,.split-frame{min-width:0;margin:0;padding:5px 8px 8px;box-sizing:border-box;border:1px solid #bdbdbd;background:#fff}.reference-frame{height:186px;flex:0 0 186px}.split-frame{flex:1;min-height:245px;display:flex;flex-direction:column}
legend{padding:0 5px;color:#444}.query-row{height:32px;display:flex;align-items:center;gap:8px}.query-row label{min-width:120px;text-align:right}.mes-input{height:25px;padding:3px 6px;border:1px solid #888;border-radius:2px;background:#fff;color:#333;font:inherit;box-sizing:border-box}.editable{background:#fff0b3}.query-row .mes-input{width:162px}
.mes-button{min-height:29px;padding:3px 11px;border:1px solid #b9b9b9;border-radius:3px;background:linear-gradient(#fff,#f3f3f3);color:#333;font:inherit;white-space:nowrap;cursor:pointer}.mes-button:hover:not(:disabled){background:#fff5dc}.mes-button:disabled{color:#999;cursor:default}
.grid-wrap{min-width:0;overflow:auto;border:1px solid #a7a7a7;background:#fff}.reference-grid{height:132px}.grid-wrap table,.picker-grid table{width:100%;border-collapse:collapse;white-space:nowrap}.grid-wrap th,.picker-grid th{position:sticky;top:0;z-index:1;padding:5px 7px;border:1px solid #d4d4d4;background:#ead591;color:#393326;font-weight:500;text-align:left}.grid-wrap td,.picker-grid td{padding:4px 6px;border:1px solid #d2d2d2;background:#ffe89c}.grid-wrap tbody tr.selected td{background:#ffedb3}.number{text-align:right;font-variant-numeric:tabular-nums}.empty-row{text-align:center!important;color:#777!important;background:#fff!important;height:45px}
.toolbar-row{min-height:40px;display:flex;align-items:center;gap:9px;flex-wrap:wrap}.field-label{min-width:120px;text-align:right}.radio-frame{display:flex;gap:14px;margin:0;padding:4px 8px;border:1px solid #ccc}.radio-frame label{display:flex;align-items:center;gap:3px;white-space:nowrap}.amount{width:70px;text-align:right}.second-row{min-height:46px}.spacer{flex:1}
.split-grids{flex:1;min-height:125px;display:grid;grid-template-columns:minmax(0,1fr) 170px;gap:6px}.split-grid tbody tr{cursor:pointer}.split-grid tbody tr:hover td{background:#fff0bd}.split-grid td{height:30px;padding:2px 5px}.split-grid small{padding:1px 4px;border:1px solid #d2ad55;background:#fff4c7;color:#765e20}.readonly-cell{background:#ececec!important}.grid-select{min-width:90px;max-width:160px;width:100%;height:25px;border:1px solid #999;background:#fff;color:#333;font:inherit}.cell-button{padding:2px 4px;border:0;background:transparent;color:#6b5222;font:inherit;text-decoration:underline;cursor:pointer}.cell-button:disabled{color:#777;text-decoration:none;cursor:default}.component-grid td{height:30px}
.split-status{min-height:24px;padding:4px 6px;border:1px solid #c5c5c5;background:#eee;color:#555;text-align:right;box-sizing:border-box}.form-footer{min-height:36px;display:flex;align-items:center;justify-content:flex-end;gap:8px}.footer-note{margin-right:auto;color:#777}.confirm-button{min-width:82px;color:#fff;background:#f3a62a;border-color:#e99512}.confirm-button:hover:not(:disabled){background:#e99a1b}
.modal-backdrop{position:fixed;inset:0;z-index:1300;display:grid;place-items:center;padding:18px;background:#0005}.picker-dialog{width:min(560px,94vw);max-height:84vh;display:flex;flex-direction:column;gap:10px;padding:14px;border:1px solid #999;background:#fff;box-shadow:0 8px 28px #0004;box-sizing:border-box}.picker-dialog header{display:flex;justify-content:space-between;gap:12px;align-items:center}.picker-dialog header span{color:#666}.picker-dialog footer{display:flex;justify-content:flex-end;gap:8px}.select-all{display:flex;align-items:center;gap:5px}.picker-grid{overflow:auto;min-height:90px;max-height:48vh}.picker-option{display:flex;flex-direction:column;gap:4px;padding:9px;border:1px solid #ccc;background:#fff;text-align:left;color:#333;font:inherit;cursor:pointer}.picker-option:hover{background:#fff3cc}.picker-option small{color:#666}
@media(max-width:720px){.reference-frame{height:150px;flex-basis:150px}.reference-grid{height:94px}.split-grids{grid-template-columns:minmax(0,1fr) 135px}.field-label{min-width:90px}.toolbar-row{gap:5px}.footer-note{display:none}}
</style>
