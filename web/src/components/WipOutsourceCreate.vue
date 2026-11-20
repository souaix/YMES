<script setup lang="ts">
import { computed, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'

interface Lot {
  lotNo: string
  moNo: string
  productNo: string
  processNo: string
  opNo: string
  componentNo: string
  qty: number
  unit: string
  receipt?: { good: number; scrap: number; other: number; unitPrice: number; unit: string; at: string; by: string }
}

interface Order {
  osNo: string
  opNo: string
  vendorNo: string
  itemNo: string
  preparer: string
  due: string
  description: string
  returnNode: string
  lots: Lot[]
  status: 0 | 1 | 10 | 11 | -1
  creator: string
  createdAt: string
  stamp: number
  shippedAt?: string
  shippedBy?: string
  erpNo?: string
  reverseLots?: string[]
}

const props = defineProps<{ developmentPreview?: boolean; currentUserNo?: string; pageMode?: 'confirm' | 'return' | 'cancel' }>()
const emit = defineEmits<{ close: [] }>()
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()
const isDev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const unavailableMessage = "正式環境尚未開放此功能。"
const confirmationOnly = computed(() => props.pageMode === 'confirm')
const returnOnly = computed(() => props.pageMode === 'return')
const cancelOnly = computed(() => props.pageMode === 'cancel')
const pageTitles = { confirm: '外包單確認', return: '外包回貨', cancel: '取消外包出貨 ERP' }
const pageTitle = computed(() => props.pageMode ? pageTitles[props.pageMode] : '外包單開立')
const storageKey = 'ymes-dev-mock:wip-outsource:orders:v1'
const stations = isDev.value ? (['DEV-OS-OP', 'DEV-QC-OP']) : ([])
const vendors = isDev.value ? (['DEV-VENDOR-01', 'DEV-VENDOR-02']) : ([])
const preparers = isDev.value ? (['DEV001', 'DEV002']) : ([])
const returnStations = isDev.value ? ([
  { node: 'DEV-CP-020', op: 'DEV-QC-OP', process: 'DEV-CP' },
  { node: 'DEV-FT-020', op: 'DEV-QC-OP', process: 'DEV-FT' },
]) : ([])
const lots: Lot[] = isDev.value ? ([
  { lotNo: 'DEV-LOT-401A', moNo: 'DEV-MO-401', productNo: 'DEV-PROD-CP', processNo: 'DEV-CP', opNo: 'DEV-OS-OP', componentNo: 'DEV-COMP-A', qty: 18, unit: 'PCS' },
  { lotNo: 'DEV-LOT-401B', moNo: 'DEV-MO-401', productNo: 'DEV-PROD-CP', processNo: 'DEV-CP', opNo: 'DEV-OS-OP', componentNo: 'DEV-COMP-B', qty: 20, unit: 'PCS' },
  { lotNo: 'DEV-LOT-401C', moNo: 'DEV-MO-401', productNo: 'DEV-PROD-CP', processNo: 'DEV-CP', opNo: 'DEV-QC-OP', componentNo: 'DEV-COMP-C', qty: 12.5, unit: 'PCS' },
  { lotNo: 'DEV-LOT-401D', moNo: 'DEV-MO-402', productNo: 'DEV-PROD-FT', processNo: 'DEV-FT', opNo: 'DEV-OS-OP', componentNo: 'DEV-COMP-D', qty: 30, unit: 'PCS' },
]) : ([])
const upper = (value: string) => value.trim().toUpperCase()
const today = () => new Intl.DateTimeFormat('sv-SE', { timeZone: 'Asia/Taipei' }).format(new Date())
const lotTotal = (items: Lot[]) => Math.round(items.reduce((sum, lot) => sum + lot.qty, 0) * 10000) / 10000
const mainFields = [
  { key: 'osNo', label: '外包單號' }, { key: 'opNo', label: '外包站' },
  { key: 'vendorNo', label: '外包商' }, { key: 'creator', label: '建立人員' },
] as const
const lotFields = [
  { key: 'productNo', label: '產品' }, { key: 'moNo', label: '工單' },
  { key: 'lotNo', label: '批號' }, { key: 'componentNo', label: '元件' },
] as const
const filters = ref({ osNo: '', opNo: '', vendorNo: '', creator: '', from: '', to: '', shippedFrom: '', shippedTo: '' })
const appliedFilters = ref({ ...filters.value })
const orders = ref<Order[]>(readOrders())
const selected = ref<string[]>([])
const selectedNo = ref('')
const draft = ref<Order>()
const mode = ref<'add' | 'edit'>('add')
const lotFilters = ref({ productNo: '', moNo: '', lotNo: '', componentNo: '' })
const referenceCandidates = ref<Lot[]>([])
const candidateLots = ref<Lot[]>([])
const scannedLotNo = ref('')
const error = ref('')
const editError = ref('')
const notice = ref('')
const simulateErp = ref(false)
const receiptRows = ref<{ lot: Lot; checked: boolean; good: number; scrap: number; unitPrice: number; unit: string }[]>()
const receiptOrder = ref<Order>()

const visibleOrders = computed(() => {
  if (!isDev.value) return []

  const filters = appliedFilters.value
  return orders.value.filter(row => (returnOnly.value ? row.status === 1 || row.status === 10 : row.status === (cancelOnly.value ? 1 : 0)) &&
    mainFields.every(({ key }) => !upper(filters[key]) || upper(row[key]) === upper(filters[key])) &&
    (!filters.from || row.createdAt >= filters.from) && (!filters.to || row.createdAt <= filters.to) &&
    (!filters.shippedFrom || !!row.shippedAt && row.shippedAt >= filters.shippedFrom) &&
    (!filters.shippedTo || !!row.shippedAt && row.shippedAt <= filters.shippedTo))
    .sort((a, b) => a.osNo.localeCompare(b.osNo))
})
const active = computed(() => isDev.value ? (visibleOrders.value.find(row => row.osNo === selectedNo.value) ?? visibleOrders.value[0]) : (undefined))
const reference = computed(() => isDev.value ? (draft.value?.lots[0]) : (undefined))
const requiresReturn = computed(() => isDev.value ? (draft.value?.itemNo === '02') : (false))
const availableReturns = computed(() => isDev.value ? (returnStations.filter(row => row.process === reference.value?.processNo)) : ([]))
const picked = computed(() => isDev.value ? (new Set(draft.value?.lots.map(lot => lot.lotNo))) : (new Set<string>()))

function readOrders(): Order[] {
  if (!isDev.value) return []
  try {
    const raw = localStorage.getItem(storageKey)
    if (raw) {
      const rows: unknown = JSON.parse(raw)
      if (Array.isArray(rows)) return rows as Order[]
    }
  } catch {
    // Start with sample rows after a local read error.
  }
  return [{
    osNo: 'DEV-OS-0001', opNo: stations[0]!, vendorNo: vendors[0]!, itemNo: '02', preparer: 'DEV001', due: '',
    description: '外包測試範例', returnNode: 'DEV-CP-020', lots: [lots[0]!], status: 0,
    creator: props.currentUserNo || 'DEV001', createdAt: today(), stamp: 0,
  }]
}

function persist(rows: Order[]): boolean {
  if (!isDev.value) return false
  try {
    localStorage.setItem(storageKey, JSON.stringify(rows))
    orders.value = rows
    return true
  } catch {
    return false
  }
}

function search(): void {
  if (!isDev.value) return

  error.value = ''
  notice.value = ''
  if ((filters.value.from && filters.value.to && filters.value.from > filters.value.to) ||
    (filters.value.shippedFrom && filters.value.shippedTo && filters.value.shippedFrom > filters.value.shippedTo)) {
    error.value = '日期起日不可晚於迄日。'
    return
  }
  appliedFilters.value = { ...filters.value }
  selected.value = []
  selectedNo.value = ''
}

function openEditor(nextMode: typeof mode.value): void {
  if (!isDev.value || (nextMode === 'edit' && !active.value)) return
  mode.value = nextMode
  draft.value = nextMode === 'edit' && active.value ? { ...active.value, lots: [...active.value.lots] } : {
    osNo: '', opNo: '', vendorNo: '', itemNo: '', preparer: '', due: '', description: '', returnNode: '',
    lots: [], status: 0, creator: props.currentUserNo || 'DEV001', createdAt: today(), stamp: 0,
  }
  candidateLots.value = [...draft.value.lots]
  lotFilters.value = { productNo: '', moNo: '', lotNo: '', componentNo: '' }
  referenceCandidates.value = []
  scannedLotNo.value = ''
  editError.value = ''
}

function useNumber(): void {
  if (!isDev.value) return

  if (!draft.value || mode.value !== 'add') return
  const used = new Set(orders.value.map(row => row.osNo))
  let serial = 1
  while (used.has('DEV-OS-' + String(serial).padStart(4, '0'))) serial++
  draft.value.osNo = 'DEV-OS-' + String(serial).padStart(4, '0')
}

function isAvailable(lot: Lot): boolean {
  if (!isDev.value) return false

  return !orders.value.some(row => row.status !== -1 && (mode.value !== 'edit' || row.osNo !== draft.value?.osNo) &&
    (row.status === 0 ? row.opNo === draft.value?.opNo : row.status !== 11) &&
    row.lots.some(item => item.lotNo === lot.lotNo && !item.receipt))
}

function searchReference(): void {
  if (!isDev.value) return

  editError.value = ''
  if (!lotFields.some(({ key }) => upper(lotFilters.value[key]))) {
    editError.value = '請至少輸入一個參考批查詢條件。'
    return
  }
  referenceCandidates.value = lots.filter(lot => isAvailable(lot) &&
    lotFields.every(({ key }) => !upper(lotFilters.value[key]) || upper(lot[key]) === upper(lotFilters.value[key])))
  if (!referenceCandidates.value.length) editError.value = '沒有符合條件的參考批。'
}

function pickReference(lot: Lot): void {
  if (!isDev.value) return

  if (!draft.value || reference.value?.lotNo === lot.lotNo) return
  draft.value.lots = [lot]
  draft.value.returnNode = ''
  candidateLots.value = [lot]
  referenceCandidates.value = []
  editError.value = ''
}

function eligible(lot: Lot): boolean {
  if (!isDev.value) return false

  return !!reference.value && lot.processNo === reference.value.processNo &&
    lot.unit === reference.value.unit && isAvailable(lot)
}

function scanLot(): void {
  if (!isDev.value) return

  if (!draft.value || !reference.value) {
    editError.value = '請先選擇參考批。'
    return
  }
  const lot = lots.find(row => row.lotNo === upper(scannedLotNo.value))
  if (!lot || !eligible(lot)) {
    editError.value = '查無可用批號，或流程、單位與參考批不符。'
    return
  }
  if (!candidateLots.value.some(row => row.lotNo === lot.lotNo)) candidateLots.value.push(lot)
  if (!picked.value.has(lot.lotNo)) draft.value.lots.push(lot)
  scannedLotNo.value = ''
  editError.value = ''
}

function chooseLot(lot: Lot, checked: boolean): void {
  if (!isDev.value) return

  if (!draft.value || lot.lotNo === reference.value?.lotNo) return
  if (!checked) draft.value.lots = draft.value.lots.filter(row => row.lotNo !== lot.lotNo)
  else if (eligible(lot) && !picked.value.has(lot.lotNo)) draft.value.lots.push(lot)
}

function save(close: boolean): void {
  if (!draft.value || !isDev.value) return
  const row = { ...draft.value, osNo: upper(draft.value.osNo), returnNode: requiresReturn.value ? draft.value.returnNode : '' }
  const current = orders.value.find(item => item.osNo === row.osNo)
  if (!row.osNo || !row.opNo || !row.vendorNo || !row.itemNo || !row.lots.length) {
    editError.value = '外包單號、外包站、外包項目、外包商及參考批為必填。'
    return
  }
  if (requiresReturn.value && !availableReturns.value.some(node => node.node === row.returnNode)) {
    editError.value = '請選擇參考批原流程的回貨站。'
    return
  }
  if (row.lots.some(lot => !eligible(lot))) {
    editError.value = '已有批號被預約，或流程、單位不符。請重新選批。'
    return
  }
  if (mode.value === 'add' ? !!current : current?.status !== 0 || current.stamp !== row.stamp) {
    editError.value = mode.value === 'add' ? '外包單號已存在。' : '外包單已變更，請重新查詢。'
    return
  }
  const saved = { ...row, stamp: current ? current.stamp + 1 : 0 }
  const next = current ? orders.value.map(item => item.osNo === row.osNo ? saved : item) : [...orders.value, saved]
  if (!persist(next)) { editError.value = '無法儲存 DEV 外包單。'; return }
  selectedNo.value = row.osNo
  selected.value = [row.osNo]
  notice.value = '已儲存 DEV 外包單 ' + row.osNo
  editError.value = ''
  mode.value = 'edit'
  draft.value = close ? undefined : { ...saved, lots: [...saved.lots] }
}

async function remove(action: 'delete' | 'abort'): Promise<void> {
  if (!isDev.value) return
  const rows = visibleOrders.value.filter(row => selected.value.includes(row.osNo))
  const targets = rows.length ? rows : active.value ? [active.value] : []
  const verb = action === 'delete' ? '刪除' : '作廢'
  if (!targets.length || !await requestConfirmation('確定' + verb + ' ' + targets.length + ' 筆外包單？')) return
  const keys = new Set(targets.map(row => row.osNo))
  const next = orders.value.flatMap(row => !keys.has(row.osNo) || row.status !== 0 ? [row] :
    action === 'delete' ? [] : [{ ...row, status: -1 as const, stamp: row.stamp + 1 }])
  if (!persist(next)) { error.value = '無法儲存 DEV 異動。'; return }
  selected.value = []
  selectedNo.value = ''
  notice.value = '已' + verb + ' ' + keys.size + ' 筆 DEV 外包單。'
}

async function confirmShipment(): Promise<void> {
  const order = active.value
  if (!isDev.value || !confirmationOnly.value || !order) return
  error.value = ''
  notice.value = ''
  const blockedLot = order.lots.find(lot => lot.opNo !== order.opNo)
  if (!order.lots.length || blockedLot) {
    error.value = blockedLot ? blockedLot.lotNo + ' 不在指定外包站。' : '外包單未設定批號。'
    return
  }
  if (!await requestConfirmation('確定模擬確認外包單 ' + order.osNo + '？')) return
  const current = orders.value.find(row => row.osNo === order.osNo)
  if (!current || current.status !== 0 || current.stamp !== order.stamp) {
    error.value = '外包單已變更，請重新查詢。'
    return
  }
  const updated: Order = { ...current, status: 1, stamp: current.stamp + 1,
    shippedAt: today(), shippedBy: props.currentUserNo || 'DEV001',
    erpNo: simulateErp.value ? 'DEV-ERP-' + current.osNo : undefined,
    reverseLots: current.lots.map(lot => lot.lotNo) }
  if (!persist(orders.value.map(row => row.osNo === updated.osNo ? updated : row))) {
    error.value = '無法儲存 DEV 確認。'
    return
  }
  selectedNo.value = ''
  notice.value = '已模擬確認 ' + updated.osNo + '。'
}

function openReceipt(): void {
  if (!isDev.value || !returnOnly.value || !active.value) return
  receiptOrder.value = active.value
  receiptRows.value = active.value.lots.map(lot => ({
    lot, checked: false, good: lot.receipt?.good ?? lot.qty, scrap: lot.receipt?.scrap ?? 0,
    unitPrice: lot.receipt?.unitPrice ?? 0, unit: lot.receipt?.unit ?? lot.unit,
  }))
  editError.value = ''
  notice.value = ''
}

function otherQty(row: { lot: Lot; good: number; scrap: number }): number {
  return Math.round((row.lot.qty - row.good - row.scrap) * 10000) / 10000
}

async function saveReceipt(): Promise<void> {
  const original = receiptOrder.value
  if (!isDev.value || !returnOnly.value || !original) return
  const selected = receiptRows.value?.filter(row => row.checked && !row.lot.receipt) ?? []
  if (!selected.length) { editError.value = '請選擇至少一筆待回貨批號。'; return }
  const bad = selected.find(row => ![row.good, row.scrap, row.unitPrice].every(value => Number.isFinite(value) && value >= 0) ||
    otherQty(row) < 0 || !row.unit)
  if (bad) { editError.value = bad.lot.lotNo + ' 的數量、單價或回貨單位有誤。'; return }
  if (!await requestConfirmation('確定模擬回貨 ' + selected.length + ' 批？')) return
  const current = orders.value.find(row => row.osNo === original.osNo)
  if (!current || current.stamp !== original.stamp || (current.status !== 1 && current.status !== 10)) {
    editError.value = '外包單已變更，請重新查詢。'
    return
  }
  const at = today()
  const by = props.currentUserNo || 'DEV001'
  const lots = current.lots.map(lot => {
    const row = selected.find(item => item.lot.lotNo === lot.lotNo)
    return row ? { ...lot, receipt: { good: row.good, scrap: row.scrap, other: otherQty(row), unitPrice: row.unitPrice, unit: row.unit, at, by } } : lot
  })
  const updated: Order = { ...current, lots, status: lots.every(lot => lot.receipt) ? 11 : 10, stamp: current.stamp + 1 }
  if (!persist(orders.value.map(row => row.osNo === updated.osNo ? updated : row))) {
    editError.value = '無法儲存 DEV 回貨。'
    return
  }
  receiptRows.value = undefined
  receiptOrder.value = undefined
  notice.value = '已模擬回貨 ' + selected.length + ' 批。'
}

async function cancelShipment(): Promise<void> {
  const row = active.value
  if (!cancelOnly.value || !isDev.value || !row) return
  error.value = ''
  notice.value = ''
  if (!row.erpNo) { error.value = '這筆沒有可取消的 ERP 出貨單號。'; return }
  const blocked = row.lots.filter(lot => lot.receipt || lot.opNo !== row.opNo || !row.reverseLots?.includes(lot.lotNo))
  if (blocked.length) {
    error.value = '缺少可回復記錄的批號 ' + blocked.map(lot => lot.lotNo).join('、')
    return
  }
  if (!await requestConfirmation('確定模擬取消出貨 ' + row.erpNo + '？')) return
  const current = orders.value.find(order => order.osNo === row.osNo)
  if (!current || current.stamp !== row.stamp || current.status !== 1) {
    error.value = '外包單已變更，請重新查詢。'
    return
  }
  const updated: Order = { ...current, status: 0, stamp: current.stamp + 1,
    erpNo: undefined, shippedAt: undefined, shippedBy: undefined, reverseLots: undefined }
  if (!persist(orders.value.map(order => order.osNo === updated.osNo ? updated : order))) {
    error.value = '無法儲存 DEV 取消出貨。'
    return
  }
  notice.value = '已模擬取消出貨 ' + current.osNo + '，單據已回到未確認。'
}
</script>

<template>
  <section class="outsource">
    <h2>{{ pageTitle }}</h2>
    <template v-if="isDev">
      <form class="toolbar" @submit.prevent="search">
        <label v-for="field in mainFields" :key="field.key">{{ field.label }} <input v-model="filters[field.key]" maxlength="20"></label>
        <label>建立起日 <input v-model="filters.from" type="date"></label>
        <label>建立迄日 <input v-model="filters.to" type="date"></label>
        <template v-if="returnOnly || cancelOnly">
          <label>出貨起日 <input v-model="filters.shippedFrom" type="date"></label>
          <label>出貨迄日 <input v-model="filters.shippedTo" type="date"></label>
        </template>
        <button type="submit">查詢</button>
      </form>
      <div v-if="returnOnly" class="toolbar">
        <button type="button" :disabled="!active" @click="openReceipt">外包回貨</button>
      </div>
      <div v-else-if="cancelOnly" class="toolbar">
        <button type="button" :disabled="!active" @click="cancelShipment">模擬取消出貨</button>
      </div>
      <div v-else-if="confirmationOnly" class="toolbar">
        <label><input v-model="simulateErp" type="checkbox"> 產生 DEV ERP 單號</label>
        <button type="button" :disabled="!active" @click="confirmShipment">確認</button>
      </div>
      <div v-else class="toolbar">
        <button type="button" @click="openEditor('add')">新增</button>
        <button type="button" :disabled="!active" @click="openEditor('edit')">修改</button>
        <button type="button" :disabled="!active" @click="remove('delete')">刪除</button>
        <button type="button" :disabled="!active" @click="remove('abort')">作廢</button>
      </div>
      <p v-if="error" role="alert">{{ error }}</p><p v-if="notice" role="status">{{ notice }}</p>
      <div class="grid"><table>
        <thead><tr><th>選取</th><th>外包單號</th><th>外包站</th><th>項目</th><th>外包商</th><th>承辦人</th><th>需求日</th><th>外包量</th><th>狀態</th><th>建立人員</th><th>建立日</th><th>說明</th><th v-if="cancelOnly">ERP 單號</th></tr></thead>
        <tbody>
          <tr v-for="row in visibleOrders" :key="row.osNo" :class="{ active: active?.osNo === row.osNo }" @click="selectedNo = row.osNo">
            <td>
              <input v-if="pageMode" name="os-order" type="radio" :aria-label="'選取 ' + row.osNo" :checked="active?.osNo === row.osNo" @change="selectedNo = row.osNo">
              <input v-else v-model="selected" :aria-label="'選取 ' + row.osNo" type="checkbox" :value="row.osNo">
            </td>
            <td><button type="button" class="link" @click="selectedNo = row.osNo">{{ row.osNo }}</button></td>
            <td>{{ row.opNo }}</td><td>{{ row.itemNo }}</td><td>{{ row.vendorNo }}</td><td>{{ row.preparer }}</td><td>{{ row.due }}</td>
            <td>{{ lotTotal(row.lots) }}</td><td>{{ row.status === 10 ? '部分回貨' : row.status === 1 ? '已出貨' : '未確認' }}</td><td>{{ row.creator }}</td><td>{{ row.createdAt }}</td><td>{{ row.description }}</td>
            <td v-if="cancelOnly">{{ row.erpNo }}</td>
          </tr>
          <tr v-if="!visibleOrders.length"><td :colspan="cancelOnly ? 13 : 12">{{ cancelOnly ? '查無已出貨外包單' : returnOnly ? '查無待回貨外包單' : '查無未確認的臨時外包單' }}</td></tr>
        </tbody>
      </table></div>
      <h3>批號明細 {{ active?.osNo }}</h3>
      <div class="grid"><table>
        <thead><tr><th>批號</th><th>投入量</th><th>單位</th><th>回貨狀態</th></tr></thead>
        <tbody>
          <tr v-for="lot in active?.lots ?? []" :key="lot.lotNo"><td>{{ lot.lotNo }}</td><td>{{ lot.qty }}</td><td>{{ lot.unit }}</td><td>{{ lot.receipt ? '已回貨' : '待回貨' }}</td></tr>
          <tr v-if="!active"><td colspan="4">尚未選取外包單</td></tr>
        </tbody>
      </table></div>
    </template>
    <p v-else role="status">{{ unavailableMessage }}</p>
    <button type="button" @click="emit('close')">關閉</button>

    <div v-if="isDev && draft" class="overlay">
      <section class="dialog" role="dialog" aria-modal="true" aria-label="外包單定義">
        <h3>{{ mode === 'add' ? '新增外包單' : '修改外包單' }}</h3>
        <div class="fields">
          <label>外包單號 <input v-model="draft.osNo" :readonly="mode === 'edit'" maxlength="20"></label>
          <button type="button" :disabled="mode === 'edit'" @click="useNumber">產生 DEV 單號</button>
          <label>外包站 <select v-model="draft.opNo"><option value="">請選擇</option><option v-for="station in stations" :key="station">{{ station }}</option></select></label>
          <label>外包項目 <select v-model="draft.itemNo" @change="draft.returnNode = ''"><option value="">請選擇</option><option value="01">01 不回原流程</option><option value="02">02 回原流程指定站</option></select></label>
          <label>外包商 <select v-model="draft.vendorNo"><option value="">請選擇</option><option v-for="vendor in vendors" :key="vendor">{{ vendor }}</option></select></label>
          <label>承辦人 <select v-model="draft.preparer"><option value="">未指定</option><option v-for="user in preparers" :key="user">{{ user }}</option></select></label>
          <label>需求日 <input v-model="draft.due" type="date"></label>
          <label v-if="requiresReturn">回貨站 <select v-model="draft.returnNode" :disabled="!reference"><option value="">請選擇</option><option v-for="node in availableReturns" :key="node.node" :value="node.node">{{ node.op }} / {{ node.node }}</option></select></label>
          <label>說明 <input v-model="draft.description" maxlength="255"></label>
        </div>
        <h3>參考批與明細</h3>
        <form class="toolbar" @submit.prevent="searchReference">
          <label v-for="field in lotFields" :key="field.key">{{ field.label }} <input v-model="lotFilters[field.key]" maxlength="50"></label>
          <button type="submit">查詢參考批</button>
        </form>
        <ul v-if="referenceCandidates.length" class="toolbar">
          <li v-for="lot in referenceCandidates" :key="lot.lotNo"><button type="button" @click="pickReference(lot)">{{ lot.lotNo }} · {{ lot.processNo }} · {{ lot.qty }} {{ lot.unit }}</button></li>
        </ul>
        <form class="toolbar" @submit.prevent="scanLot">
          <label>參考批 <input :value="reference?.lotNo" readonly></label>
          <label>刷入批號 <input v-model="scannedLotNo" :disabled="!reference" maxlength="50"></label>
          <button type="submit" :disabled="!reference">加入批號</button>
          <button type="button" :disabled="!reference" @click="candidateLots.forEach(lot => chooseLot(lot, true))">全選</button>
          <button type="button" :disabled="!reference" @click="candidateLots.forEach(lot => chooseLot(lot, false))">取消全選</button>
        </form>
        <div class="grid"><table>
          <thead><tr><th>納入</th><th>批號</th><th>工單</th><th>產品</th><th>流程</th><th>作業站</th><th>數量</th><th>單位</th></tr></thead>
          <tbody><tr v-for="lot in candidateLots" :key="lot.lotNo">
            <td><input type="checkbox" :aria-label="'納入 ' + lot.lotNo" :checked="picked.has(lot.lotNo)" :disabled="lot.lotNo === reference?.lotNo"
              @change="chooseLot(lot, ($event.target as HTMLInputElement).checked)"></td>
            <td>{{ lot.lotNo }}</td><td>{{ lot.moNo }}</td><td>{{ lot.productNo }}</td><td>{{ lot.processNo }}</td><td>{{ lot.opNo }}</td><td>{{ lot.qty }}</td><td>{{ lot.unit }}</td>
          </tr></tbody>
        </table></div>
        <p>外包量 {{ lotTotal(draft.lots) }} {{ reference?.unit }}</p>
        <p v-if="editError" role="alert">{{ editError }}</p>
        <footer><button type="button" @click="save(true)">確認</button><button type="button" @click="save(false)">套用</button><button type="button" @click="draft = undefined">關閉</button></footer>
      </section>
    </div>
    <div v-if="isDev && receiptRows" class="overlay">
      <section class="dialog" role="dialog" aria-modal="true" aria-label="外包回貨明細">
        <h3>外包回貨 {{ receiptOrder?.osNo }}</h3>
        <div class="toolbar">
          <button type="button" @click="receiptRows.forEach(row => row.checked = !row.lot.receipt)">全選待回貨</button>
          <button type="button" @click="receiptRows.forEach(row => row.checked = false)">取消全選</button>
        </div>
        <div class="grid"><table>
          <thead><tr><th>納入</th><th>批號</th><th>投入量</th><th>良品量</th><th>報廢量</th><th>其他量</th><th>單價</th><th>回貨單位</th></tr></thead>
          <tbody><tr v-for="row in receiptRows" :key="row.lot.lotNo">
            <td><input v-model="row.checked" type="checkbox" :aria-label="'回貨 ' + row.lot.lotNo" :disabled="!!row.lot.receipt"></td>
            <td>{{ row.lot.lotNo }}</td><td>{{ row.lot.qty }}</td>
            <td><input v-model.number="row.good" type="number" step="0.0001" min="0" aria-label="良品量" :disabled="!row.checked"></td>
            <td><input v-model.number="row.scrap" type="number" step="0.0001" min="0" aria-label="報廢量" :disabled="!row.checked"></td>
            <td>{{ otherQty(row) }}</td>
            <td><input v-model.number="row.unitPrice" type="number" step="0.0001" min="0" aria-label="單價" :disabled="!row.checked"></td>
            <td><input v-model="row.unit" maxlength="30" aria-label="回貨單位" :disabled="!row.checked"></td>
          </tr></tbody>
        </table></div>
        <p v-if="editError" role="alert">{{ editError }}</p>
        <footer><button type="button" @click="saveReceipt">確認</button><button type="button" @click="receiptRows = undefined">關閉</button></footer>
      </section>
    </div>
    <ActionConfirmationDialog v-if="isDev" :message="confirmationMessage" @resolve="resolveConfirmation" />
  </section>
</template>

<style scoped>
.outsource { display: grid; align-content: start; gap: 12px; padding: 16px; color: var(--ink); }
h2, h3, p, ul { margin: 0; }
.toolbar, footer { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
ul.toolbar { list-style: none; padding: 0; }
.grid { overflow: auto; border: 1px solid var(--line); background: white; }
table { border-collapse: collapse; width: 100%; font-size: 13px; }
td, th { padding: 6px 9px; text-align: left; white-space: nowrap; border-bottom: 1px solid var(--line); }
th { background: #f7f5ee; }
.active td { background: #fff6c5; }
button { width: max-content; min-height: 30px; }
.link { background: none; border: none; text-decoration: underline; color: inherit; }
.overlay { position: fixed; inset: 0; z-index: 150; display: grid; place-items: center; padding: 16px; background: #28251f55; }
.dialog { width: min(940px, 100%); max-height: 90vh; overflow: auto; padding: 18px; background: white; border: 1px solid var(--line); border-radius: 6px; display: grid; gap: 12px; }
.fields { display: grid; gap: 9px; grid-template-columns: repeat(auto-fit, minmax(175px, 1fr)); }
.fields label { display: grid; gap: 4px; }
footer { justify-content: flex-end; }
[role="alert"] { color: #9d2727; }
</style>
