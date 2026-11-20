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
  status: 0 | -1
  creator: string
  createdAt: string
  stamp: number
}

const props = defineProps<{ developmentPreview?: boolean; currentUserNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()
const isDev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const storageKey = 'ymes-dev-mock:wip-outsource:orders:v1'
const stations = ['DEV-OS-OP', 'DEV-QC-OP']
const vendors = ['DEV-VENDOR-01', 'DEV-VENDOR-02']
const preparers = ['DEV001', 'DEV002']
const returnStations = [
  { node: 'DEV-CP-020', op: 'DEV-QC-OP', process: 'DEV-CP' },
  { node: 'DEV-FT-020', op: 'DEV-QC-OP', process: 'DEV-FT' },
]
const lots: Lot[] = [
  { lotNo: 'DEV-LOT-401A', moNo: 'DEV-MO-401', productNo: 'DEV-PROD-CP', processNo: 'DEV-CP', opNo: 'DEV-OS-OP', componentNo: 'DEV-COMP-A', qty: 18, unit: 'PCS' },
  { lotNo: 'DEV-LOT-401B', moNo: 'DEV-MO-401', productNo: 'DEV-PROD-CP', processNo: 'DEV-CP', opNo: 'DEV-OS-OP', componentNo: 'DEV-COMP-B', qty: 20, unit: 'PCS' },
  { lotNo: 'DEV-LOT-401C', moNo: 'DEV-MO-401', productNo: 'DEV-PROD-CP', processNo: 'DEV-CP', opNo: 'DEV-QC-OP', componentNo: 'DEV-COMP-C', qty: 12.5, unit: 'PCS' },
  { lotNo: 'DEV-LOT-401D', moNo: 'DEV-MO-402', productNo: 'DEV-PROD-FT', processNo: 'DEV-FT', opNo: 'DEV-OS-OP', componentNo: 'DEV-COMP-D', qty: 30, unit: 'PCS' },
]
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
const filters = ref({ osNo: '', opNo: '', vendorNo: '', creator: '', from: '', to: '' })
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

const visibleOrders = computed(() => {
  const filters = appliedFilters.value
  return orders.value.filter(row => row.status === 0 &&
    mainFields.every(({ key }) => !upper(filters[key]) || upper(row[key]) === upper(filters[key])) &&
    (!filters.from || row.createdAt >= filters.from) && (!filters.to || row.createdAt <= filters.to))
    .sort((a, b) => a.osNo.localeCompare(b.osNo))
})
const active = computed(() => visibleOrders.value.find(row => row.osNo === selectedNo.value) ?? visibleOrders.value[0])
const reference = computed(() => draft.value?.lots[0])
const requiresReturn = computed(() => draft.value?.itemNo === '02')
const availableReturns = computed(() => returnStations.filter(row => row.process === reference.value?.processNo))
const picked = computed(() => new Set(draft.value?.lots.map(lot => lot.lotNo)))

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
  error.value = ''
  notice.value = ''
  if (filters.value.from && filters.value.to && filters.value.from > filters.value.to) {
    error.value = '建立日期的起日不可晚於迄日。'
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
  if (!draft.value || mode.value !== 'add') return
  const used = new Set(orders.value.map(row => row.osNo))
  let serial = 1
  while (used.has('DEV-OS-' + String(serial).padStart(4, '0'))) serial++
  draft.value.osNo = 'DEV-OS-' + String(serial).padStart(4, '0')
}

function isAvailable(lot: Lot): boolean {
  return !orders.value.some(row => row.status === 0 && (mode.value !== 'edit' || row.osNo !== draft.value?.osNo) &&
    row.opNo === draft.value?.opNo && row.lots.some(item => item.lotNo === lot.lotNo))
}

function searchReference(): void {
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
  if (!draft.value || reference.value?.lotNo === lot.lotNo) return
  draft.value.lots = [lot]
  draft.value.returnNode = ''
  candidateLots.value = [lot]
  referenceCandidates.value = []
  editError.value = ''
}

function eligible(lot: Lot): boolean {
  return !!reference.value && lot.processNo === reference.value.processNo &&
    lot.unit === reference.value.unit && isAvailable(lot)
}

function scanLot(): void {
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
</script>

<template>
  <section class="outsource">
    <h2>外包單開立</h2>
    <template v-if="isDev">
      <form class="toolbar" @submit.prevent="search">
        <label v-for="field in mainFields" :key="field.key">{{ field.label }} <input v-model="filters[field.key]" maxlength="20"></label>
        <label>建立起日 <input v-model="filters.from" type="date"></label>
        <label>建立迄日 <input v-model="filters.to" type="date"></label>
        <button type="submit">查詢</button>
      </form>
      <div class="toolbar">
        <button type="button" @click="openEditor('add')">新增</button>
        <button type="button" :disabled="!active" @click="openEditor('edit')">修改</button>
        <button type="button" :disabled="!active" @click="remove('delete')">刪除</button>
        <button type="button" :disabled="!active" @click="remove('abort')">作廢</button>
      </div>
      <p v-if="error" role="alert">{{ error }}</p><p v-if="notice" role="status">{{ notice }}</p>
      <div class="grid"><table>
        <thead><tr><th>選取</th><th>外包單號</th><th>外包站</th><th>項目</th><th>外包商</th><th>承辦人</th><th>需求日</th><th>外包量</th><th>狀態</th><th>建立人員</th><th>建立日</th><th>說明</th></tr></thead>
        <tbody>
          <tr v-for="row in visibleOrders" :key="row.osNo" :class="{ active: active?.osNo === row.osNo }" @click="selectedNo = row.osNo">
            <td><input v-model="selected" :aria-label="'選取 ' + row.osNo" type="checkbox" :value="row.osNo"></td>
            <td><button type="button" class="link" @click="selectedNo = row.osNo">{{ row.osNo }}</button></td>
            <td>{{ row.opNo }}</td><td>{{ row.itemNo }}</td><td>{{ row.vendorNo }}</td><td>{{ row.preparer }}</td><td>{{ row.due }}</td>
            <td>{{ lotTotal(row.lots) }}</td><td>未確認</td><td>{{ row.creator }}</td><td>{{ row.createdAt }}</td><td>{{ row.description }}</td>
          </tr>
          <tr v-if="!visibleOrders.length"><td colspan="12">查無未確認的臨時外包單</td></tr>
        </tbody>
      </table></div>
      <h3>批號明細 {{ active?.osNo }}</h3>
      <div class="grid"><table>
        <thead><tr><th>批號</th><th>投入量</th><th>單位</th><th>回貨狀態</th></tr></thead>
        <tbody>
          <tr v-for="lot in active?.lots ?? []" :key="lot.lotNo"><td>{{ lot.lotNo }}</td><td>{{ lot.qty }}</td><td>{{ lot.unit }}</td><td>待回貨</td></tr>
          <tr v-if="!active"><td colspan="4">尚未選取外包單</td></tr>
        </tbody>
      </table></div>
    </template>
    <p v-else role="status">外包單開立尚未接入正式服務。</p>
    <button type="button" @click="emit('close')">關閉</button>

    <div v-if="draft" class="overlay">
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
    <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
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
