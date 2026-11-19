<script setup lang="ts">
import { computed, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'

type IssueState = -1 | 0 | 1 | 2
interface OrderProperty { propertyNo: string; propertyValue: string }
interface OrderLine {
  itemNo: number; productNo: string; customerNo: string; customerName: string; orderType: string
  orderQty: number; roUnitNo: string; priority: number; unitPrice: number; unitMeasure: string
  currency: string; orderDate: string; description: string; unreleaseQty: number; shipQty: number
  state: number; bookingFlag: number; shippingFlag: number
}
interface Order {
  roNo: string; customerNo: string; description: string; issueState: IssueState; roState: number
  creator: string; createDate: string; reviser: string; reviseDate: string
  properties: OrderProperty[]; lines: OrderLine[]; releasedToWorkOrder?: boolean
}

const props = defineProps<{ currentUserNo?: string; developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const isDev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()
const storageKey = 'ymes-dev-oe101-orders-v1'
const customers = [{ no: 'DEV-C01', name: '示範客戶 A' }, { no: 'DEV-C02', name: '示範客戶 B' }]
const products = ['DEV-PRD-1001', 'DEV-PRD-1002']
const states: { value: IssueState; label: string }[] = [
  { value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' },
]
const roStates: Record<number, string> = { 0: '未確認', 1: '簽核中', 2: '確認', 99: '結案' }
const now = () => new Date().toISOString()
const clone = <T,>(value: T): T => JSON.parse(JSON.stringify(value)) as T
const operator = () => props.currentUserNo || 'DEV001'

function newOrder(): Order {
  return {
    roNo: '', customerNo: '', description: '', issueState: 0, roState: 0, creator: operator(),
    createDate: now(), reviser: '', reviseDate: '', properties: [], lines: [],
  }
}

function newLine(customerNo: string): OrderLine {
  return {
    itemNo: 1, productNo: '', customerNo, customerName: customers.find(row => row.no === customerNo)?.name ?? '',
    orderType: '', orderQty: 0, roUnitNo: 'PCS', priority: 9, unitPrice: 0, unitMeasure: '', currency: '',
    orderDate: '', description: '', unreleaseQty: 0, shipQty: 0, state: 0, bookingFlag: 1, shippingFlag: 1,
  }
}

function loadOrders(): Order[] {
  if (!isDev.value) return []
  try {
    const raw = localStorage.getItem(storageKey)
    if (raw) return JSON.parse(raw) as Order[]
  } catch {
    // Restore example records when the local preview data is unavailable.
  }
  const sample = { ...newLine('DEV-C01'), productNo: products[0]!, orderType: 'DEV-STANDARD', orderQty: 100, unreleaseQty: 100 }
  return [
    { ...newOrder(), roNo: 'DEV-RO-001', customerNo: 'DEV-C01', description: '示範訂單', lines: [sample] },
    { ...newOrder(), roNo: 'DEV-RO-002', customerNo: 'DEV-C02', issueState: 1, roState: 1 },
    { ...newOrder(), roNo: 'DEV-RO-003', customerNo: 'DEV-C01', issueState: 2, roState: 2, releasedToWorkOrder: true, lines: [sample] },
    { ...newOrder(), roNo: 'DEV-RO-004', customerNo: 'DEV-C02', issueState: -1 },
  ]
}

const orders = ref(loadOrders())
const issueState = ref<IssueState>(0)
const search = ref('')
const appliedSearch = ref('')
const selectedNo = ref('')
const pageError = ref('')
const dialogError = ref('')
const dialogMode = ref<'add' | 'edit' | 'copy' | null>(null)
const draft = ref<Order>()
const detailNo = ref('')
const lineDraft = ref<OrderLine>()
const editingItem = ref<number | null>(null)
const lineError = ref('')

const visibleOrders = computed(() => orders.value.filter(row =>
  row.issueState === issueState.value && row.roNo.toUpperCase().includes(appliedSearch.value),
).sort((a, b) => a.roNo.localeCompare(b.roNo)))
const selected = computed(() => visibleOrders.value.find(row => row.roNo === selectedNo.value) ?? visibleOrders.value[0])
const detail = computed(() => orders.value.find(row => row.roNo === detailNo.value))
const detailEditable = computed(() => detail.value?.issueState === 0)

function persist(next: Order[]): boolean {
  if (!isDev.value) return false
  try {
    localStorage.setItem(storageKey, JSON.stringify(next))
    orders.value = next
    return true
  } catch {
    return false
  }
}

function searchOrders(): void {
  appliedSearch.value = search.value.trim().toUpperCase()
  selectedNo.value = ''
  pageError.value = ''
}

function selectState(value: IssueState): void {
  issueState.value = value
  selectedNo.value = ''
  pageError.value = ''
}

function openOrder(mode: 'add' | 'edit' | 'copy'): void {
  const source = selected.value
  if (mode !== 'add' && !source) return
  if (mode === 'edit' && source?.issueState !== 0) return
  dialogError.value = ''
  dialogMode.value = mode
  if (mode === 'edit') {
    draft.value = clone(source!)
  } else {
    draft.value = newOrder()
    if (mode === 'copy' && source) {
      draft.value.customerNo = source.customerNo
      draft.value.description = source.description
      draft.value.properties = clone(source.properties)
      draft.value.lines = source.lines.map(line => ({
        ...line, state: 0, unreleaseQty: line.orderQty, shipQty: 0, bookingFlag: 1, shippingFlag: 1,
      }))
    }
  }
}

function closeOrder(): void {
  draft.value = undefined
  dialogMode.value = null
}

function saveOrder(close: boolean): void {
  if (!draft.value) return
  const row = clone(draft.value)
  row.roNo = row.roNo.trim().toUpperCase()
  if (!row.roNo || !row.customerNo) { dialogError.value = '訂單編號及客戶必填。'; return }
  const updating = dialogMode.value === 'edit'
  const current = orders.value.find(item => item.roNo === row.roNo)
  if (!updating && current) { dialogError.value = '訂單編號已存在。'; return }
  if (updating && current?.issueState !== 0) { dialogError.value = '目前訂單狀態不可修改。'; return }
  const propertyNos = row.properties.map(property => property.propertyNo.trim().toUpperCase())
  if (propertyNos.some(value => !value) || new Set(propertyNos).size !== propertyNos.length) {
    dialogError.value = '屬性編號不可空白或重複。'
    return
  }
  row.properties.forEach((property, index) => { property.propertyNo = propertyNos[index]! })
  if (updating) { row.reviser = operator(); row.reviseDate = now() }
  const next = updating ? orders.value.map(item => item.roNo === row.roNo ? row : item) : [...orders.value, row]
  if (!persist(next)) { dialogError.value = '無法儲存本機訂單。'; return }
  selectState(0)
  search.value = ''
  appliedSearch.value = ''
  selectedNo.value = row.roNo
  if (close) closeOrder()
  else { draft.value = clone(row); dialogMode.value = 'edit'; dialogError.value = '' }
}

async function changeOrderState(action: 'delete' | 'approve'): Promise<void> {
  const row = selected.value
  if (!row || row.issueState === 1 || (action === 'approve' && row.issueState !== 0)) return
  const hardDelete = action === 'delete' && row.issueState !== 2
  pageError.value = ''
  if (action === 'approve' && !row.lines.length) { pageError.value = '至少需要一筆訂單明細。'; return }
  if (hardDelete && row.releasedToWorkOrder) { pageError.value = '此訂單已開立工單，無法刪除。'; return }
  const verb = action === 'approve' ? '核准' : hardDelete ? '刪除' : '停用'
  if (!await requestConfirmation('確定' + verb + '訂單 ' + row.roNo + '？')) return
  const next = hardDelete ? orders.value.filter(item => item !== row) : orders.value.map(item => {
    if (item !== row) return item
    return {
      ...item, issueState: action === 'approve' ? 2 as const : -1 as const,
      roState: action === 'approve' ? 2 : item.roState, reviser: operator(), reviseDate: now(),
    }
  })
  if (!persist(next)) pageError.value = '無法儲存本機訂單。'
}

function openDetail(): void {
  if (selected.value) { detailNo.value = selected.value.roNo; dialogError.value = '' }
}

function openLine(mode: 'add' | 'edit' | 'copy', row?: OrderLine): void {
  const order = detail.value
  if (!order || !detailEditable.value) return
  lineError.value = ''
  dialogError.value = ''
  editingItem.value = mode === 'edit' ? row?.itemNo ?? null : null
  const nextItem = Array.from({ length: 100 }, (_, index) => (index + 1) % 100)
    .find(itemNo => !order.lines.some(line => line.itemNo === itemNo))
  if (mode !== 'edit' && nextItem === undefined) { dialogError.value = '明細項次已用完。'; return }
  lineDraft.value = row ? clone(row) : newLine(order.customerNo)
  if (mode !== 'edit') lineDraft.value.itemNo = nextItem!
}

function updateLines(lines: OrderLine[]): boolean {
  return persist(orders.value.map(row => row.roNo === detailNo.value ? { ...row, lines } : row))
}

function saveLine(close: boolean): void {
  if (!detail.value || !detailEditable.value || !lineDraft.value) return
  const row = clone(lineDraft.value)
  row.customerName = customers.find(customer => customer.no === row.customerNo)?.name ?? ''
  if (!row.productNo || !row.customerName || !row.orderType || !row.roUnitNo) {
    lineError.value = '產品、客戶、訂單類型和訂單單位必填。'; return
  }
  if (!Number.isInteger(row.itemNo) || row.itemNo < 0 || row.itemNo > 99 ||
    !Number.isInteger(row.priority) || row.priority < 0 || row.priority > 99 ||
    !Number.isFinite(row.orderQty) || row.orderQty <= 0 || row.orderQty > 99_999_999.9999 ||
    !Number.isFinite(row.unitPrice) || row.unitPrice < 0 || row.unitPrice > 999_999.99) {
    lineError.value = '請確認項次、優先序、數量及單價的範圍。'; return
  }
  const lines = detail.value.lines
  if (lines.some(item => item.itemNo === row.itemNo && item.itemNo !== editingItem.value)) {
    lineError.value = '明細項次已存在。'; return
  }
  const previous = lines.find(item => item.itemNo === editingItem.value)
  row.unreleaseQty = previous ? Number((previous.unreleaseQty + row.orderQty - previous.orderQty).toFixed(4)) : row.orderQty
  if (row.unreleaseQty < 0) { lineError.value = '訂單數量低於已開工單數量。'; return }
  if (!previous) { row.state = 0; row.shipQty = 0; row.bookingFlag = 1; row.shippingFlag = 1 }
  const next = previous ? lines.map(item => item === previous ? row : item) : [...lines, row]
  if (!updateLines(next)) { lineError.value = '無法儲存本機明細。'; return }
  if (close) lineDraft.value = undefined
  else { editingItem.value = row.itemNo; lineDraft.value = clone(row); lineError.value = '' }
}

async function deleteLine(itemNo: number): Promise<void> {
  if (!detailEditable.value || !await requestConfirmation('確定刪除項次 ' + itemNo + '？')) return
  if (detail.value && detailEditable.value && !updateLines(detail.value.lines.filter(line => line.itemNo !== itemNo))) {
    dialogError.value = '無法儲存本機明細。'
  }
}
</script>

<template>
  <section class="order-page">
    <h2>訂單基本資料</h2>
    <template v-if="isDev">
      <form class="toolbar" @submit.prevent="searchOrders">
        <label>訂單編號 <input v-model="search" maxlength="25"></label>
        <button type="submit">查詢</button>
        <button v-for="status in states" :key="status.value" type="button" :aria-pressed="issueState === status.value" @click="selectState(status.value)">{{ status.label }}</button>
      </form>
      <p v-if="pageError" role="alert">{{ pageError }}</p>
      <div class="workspace">
        <div class="grid"><table>
          <thead><tr><th scope="col">選取</th><th>訂單編號</th><th>客戶</th><th>訂單狀態</th><th>說明</th><th>建立人員</th><th>建立時間</th><th>修改人員</th><th>修改時間</th></tr></thead>
          <tbody>
            <tr v-for="row in visibleOrders" :key="row.roNo" :class="{ selected: selected?.roNo === row.roNo }" @click="selectedNo = row.roNo">
              <td><input type="radio" name="selected-order" :aria-label="'選取 ' + row.roNo" :checked="selected?.roNo === row.roNo" @change="selectedNo = row.roNo"></td>
              <td>{{ row.roNo }}</td><td>{{ row.customerNo }}</td><td>{{ roStates[row.roState] ?? row.roState }}</td><td>{{ row.description }}</td>
              <td>{{ row.creator }}</td><td>{{ row.createDate.slice(0, 19).replace('T', ' ') }}</td><td>{{ row.reviser }}</td><td>{{ row.reviseDate.slice(0, 19).replace('T', ' ') }}</td>
            </tr>
            <tr v-if="!visibleOrders.length"><td colspan="9">沒有符合條件的訂單</td></tr>
          </tbody>
        </table></div>
        <div class="actions">
          <button type="button" @click="openOrder('add')">新增</button>
          <button type="button" :disabled="selected?.issueState !== 0" @click="openOrder('edit')">修改</button>
          <button type="button" :disabled="!selected || selected.issueState === 1" @click="changeOrderState('delete')">刪除</button>
          <button type="button" :disabled="!selected" @click="openOrder('copy')">複製</button>
          <button type="button" :disabled="selected?.issueState !== 0" @click="changeOrderState('approve')">核准</button>
          <button type="button" :disabled="!selected" @click="openDetail">明細</button>
        </div>
      </div>
      <p>資料總筆數 {{ visibleOrders.length }}</p>
    </template>
    <p v-else role="status">訂單基本資料尚未接入正式服務。</p>
    <button type="button" @click="emit('close')">關閉</button>

    <div v-if="draft" class="overlay">
      <form class="dialog" role="dialog" aria-modal="true" aria-label="訂單定義" @submit.prevent="saveOrder(true)">
        <h3>訂單定義</h3>
        <div class="fields">
          <label>訂單編號 <input v-model="draft.roNo" maxlength="25" :readonly="dialogMode === 'edit'" required></label>
          <label>客戶 <select v-model="draft.customerNo" required><option value="">請選擇</option><option v-for="customer in customers" :key="customer.no" :value="customer.no">{{ customer.no }} {{ customer.name }}</option></select></label>
          <label class="wide">說明 <textarea v-model="draft.description" maxlength="255"></textarea></label>
        </div>
        <fieldset>
          <legend>訂單屬性</legend>
          <table>
            <thead><tr><th>屬性編號</th><th>屬性值</th><th></th></tr></thead>
            <tbody><tr v-for="(property, index) in draft.properties" :key="index">
              <td><input v-model="property.propertyNo" aria-label="屬性編號" maxlength="20" required></td>
              <td><input v-model="property.propertyValue" aria-label="屬性值" maxlength="255"></td>
              <td><button type="button" @click="draft.properties.splice(index, 1)">移除</button></td>
            </tr></tbody>
          </table>
          <button type="button" @click="draft.properties.push({ propertyNo: '', propertyValue: '' })">新增屬性</button>
        </fieldset>
        <p v-if="dialogError" role="alert">{{ dialogError }}</p>
        <footer><button type="submit">確認</button><button type="button" @click="saveOrder(false)">套用</button><button type="button" @click="closeOrder">關閉</button></footer>
      </form>
    </div>

    <div v-if="detail" v-show="!lineDraft" class="overlay">
      <section class="dialog detail" role="dialog" aria-modal="true" aria-label="訂單明細">
        <h3>訂單明細 {{ detail.roNo }}</h3>
        <button type="button" :disabled="!detailEditable" @click="openLine('add')">新增明細</button>
        <div class="grid"><table>
          <thead><tr><th>項次</th><th>產品</th><th>客戶</th><th>訂單類型</th><th>訂單數量</th><th>未開單量</th><th>出貨量</th><th>單位</th><th>優先序</th><th>狀態</th><th>可開單</th><th>可出貨</th><th>操作</th></tr></thead>
          <tbody>
            <tr v-for="line in [...detail.lines].sort((a, b) => a.itemNo - b.itemNo)" :key="line.itemNo">
              <td>{{ line.itemNo }}</td><td>{{ line.productNo }}</td><td>{{ line.customerNo }}</td><td>{{ line.orderType }}</td><td>{{ line.orderQty }}</td><td>{{ line.unreleaseQty }}</td><td>{{ line.shipQty }}</td><td>{{ line.roUnitNo }}</td><td>{{ line.priority }}</td><td>{{ line.state }}</td><td>{{ line.bookingFlag }}</td><td>{{ line.shippingFlag }}</td>
              <td><button type="button" :disabled="!detailEditable" @click="openLine('edit', line)">修改</button><button type="button" :disabled="!detailEditable" @click="openLine('copy', line)">複製</button><button type="button" :disabled="!detailEditable" @click="deleteLine(line.itemNo)">刪除</button></td>
            </tr>
            <tr v-if="!detail.lines.length"><td colspan="13">沒有明細</td></tr>
          </tbody>
        </table></div>
        <p v-if="dialogError" role="alert">{{ dialogError }}</p>
        <footer><button type="button" @click="detailNo = ''">關閉</button></footer>
      </section>
    </div>

    <div v-if="lineDraft" class="overlay">
      <form class="dialog" role="dialog" aria-modal="true" aria-label="明細定義" @submit.prevent="saveLine(true)">
        <h3>明細定義 {{ detailNo }}</h3>
        <div class="fields">
          <label>項次 <input v-model.number="lineDraft.itemNo" type="number" min="0" max="99" :readonly="editingItem !== null" required></label>
          <label>產品 <select v-model="lineDraft.productNo" required><option value="">請選擇</option><option v-for="product in products" :key="product">{{ product }}</option></select></label>
          <label>客戶 <select v-model="lineDraft.customerNo" required><option value="">請選擇</option><option v-for="customer in customers" :key="customer.no" :value="customer.no">{{ customer.no }} {{ customer.name }}</option></select></label>
          <label>訂單類型 <select v-model="lineDraft.orderType" required><option value="">請選擇</option><option value="DEV-STANDARD">一般</option><option value="DEV-SAMPLE">樣品</option></select></label>
          <label>訂單數量 <input v-model.number="lineDraft.orderQty" type="number" min="0.0001" step="0.0001" max="99999999.9999" required></label>
          <label>訂單單位 <select v-model="lineDraft.roUnitNo"><option>PCS</option><option>SET</option></select></label>
          <label>優先序 <input v-model.number="lineDraft.priority" type="number" min="0" max="99" required></label>
          <label>單價 <input v-model.number="lineDraft.unitPrice" type="number" min="0" step="0.01" max="999999.99" required></label>
          <label>計價單位 <input v-model="lineDraft.unitMeasure" maxlength="10"></label>
          <label>幣別 <input v-model="lineDraft.currency" maxlength="10"></label>
          <label>訂單日期 <input v-model="lineDraft.orderDate" type="date"></label>
          <label class="wide">說明 <textarea v-model="lineDraft.description" maxlength="255"></textarea></label>
        </div>
        <p v-if="lineError" role="alert">{{ lineError }}</p>
        <footer><button type="submit">確認</button><button type="button" @click="($event.currentTarget as HTMLButtonElement).form?.reportValidity() && saveLine(false)">套用</button><button type="button" @click="lineDraft = undefined">返回</button></footer>
      </form>
    </div>
    <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
  </section>
</template>

<style scoped>
.order-page { display: grid; gap: 12px; padding: 16px; align-content: start; color: #253244; }
h2, h3, p { margin: 0; }
.toolbar, footer { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; }
.workspace { display: grid; grid-template-columns: minmax(0, 1fr) auto; gap: 12px; }
.actions { display: flex; flex-direction: column; gap: 8px; }
.grid { overflow: auto; border: 1px solid #bcc7d7; background: white; max-height: 54vh; }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
th, td { padding: 7px 9px; border-bottom: 1px solid #dce4ed; white-space: nowrap; text-align: left; }
th { background: #eef3f9; }
.selected td, button[aria-pressed="true"] { background: #d5e7fa; }
button, input, select, textarea { font: inherit; }
button { width: max-content; min-height: 30px; padding: 3px 10px; border: 1px solid #b8c5d6; border-radius: 4px; background: #f7f9fc; color: inherit; }
button:disabled { opacity: .55; }
.actions button { width: 100%; }
.overlay { position: fixed; inset: 0; z-index: 150; display: grid; place-items: center; background: #21334b55; padding: 18px; }
.dialog { width: min(620px, 100%); max-height: 90vh; overflow: auto; display: grid; gap: 14px; background: white; padding: 18px; border: 1px solid #8fa4bf; border-radius: 6px; }
.detail { width: min(1120px, 100%); }
.fields { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px; }
.fields label { display: grid; gap: 3px; }
.wide { grid-column: 1 / -1; }
.fields :is(input, select, textarea) { width: 100%; }
footer { justify-content: flex-end; }
[role="alert"] { color: #9d2727; }
</style>
