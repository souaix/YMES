<script setup lang="ts">
import { computed, ref } from 'vue'

type ResourceRow = {
  id: number; customerNo: string; productNo: string; productVersion: string; roNo: string
  moNo: string; orgLotNo: string; lotNo: string; resClass: number; resType: string
  resValue: number; stdValue: number; inputQty: number; unitNo: string
}
type ResourceField = keyof Pick<ResourceRow, 'moNo' | 'orgLotNo' | 'lotNo' | 'resClass' | 'resType' | 'resValue' | 'stdValue' | 'inputQty' | 'unitNo'>
type LookupKind = 'customer' | 'product'
type LookupOption = { code: string; name: string; version?: string }

const emit = defineEmits<{ close: [] }>()
const customerNo = ref('')
const productNo = ref('')
const productVersion = ref('')
const roNo = ref('')
const moNo = ref('')
const customerEnabled = ref(false)
const productEnabled = ref(false)
const roEnabled = ref(false)
const moEnabled = ref(false)
const activeConditions = ref({ customerNo: '', productNo: '', productVersion: '', roNo: '', moNo: '' })
const searched = ref(false)
const loading = ref(false)
const error = ref('')
const sortField = ref<ResourceField>('moNo')
const sortDirection = ref<'asc' | 'desc'>('asc')
const lookupKind = ref<LookupKind | null>(null)
const lookupSearch = ref('')

const sampleRows: ResourceRow[] = [
  { id: 1, customerNo: 'CUST-001', productNo: 'PD-1001', productVersion: 'A', roNo: 'RO-26041', moNo: 'MO-26041-01', orgLotNo: 'ORG-26041', lotNo: 'LOT-26041-01', resClass: 0, resType: 'EMP-018', resValue: 1.25, stdValue: 1.1, inputQty: 24, unitNo: 'EA' },
  { id: 2, customerNo: 'CUST-001', productNo: 'PD-1001', productVersion: 'A', roNo: 'RO-26041', moNo: 'MO-26041-01', orgLotNo: 'ORG-26041', lotNo: 'LOT-26041-01', resClass: 1, resType: 'EQP-LAM-02', resValue: 0.8, stdValue: 0.75, inputQty: 24, unitNo: 'EA' },
  { id: 3, customerNo: 'CUST-001', productNo: 'PD-1001', productVersion: 'A', roNo: 'RO-26041', moNo: 'MO-26041-01', orgLotNo: 'ORG-26041', lotNo: 'LOT-26041-01', resClass: 2, resType: 'OS-PLATING-01', resValue: 24, stdValue: 24, inputQty: 24, unitNo: 'EA' },
  { id: 4, customerNo: 'CUST-001', productNo: 'PD-1001', productVersion: 'A', roNo: 'RO-26041', moNo: 'MO-26041-01', orgLotNo: 'ORG-26041', lotNo: 'LOT-26041-01', resClass: 3, resType: 'MAT-ADH-010', resValue: 19.68, stdValue: 18, inputQty: 0, unitNo: 'G' },
  { id: 5, customerNo: 'CUST-001', productNo: 'PD-1001', productVersion: 'A', roNo: 'RO-26041', moNo: 'MO-26041-01', orgLotNo: 'ORG-26041', lotNo: 'LOT-26041-02', resClass: 0, resType: 'EMP-021', resValue: 0.92, stdValue: 1.1, inputQty: 18, unitNo: 'EA' },
  { id: 6, customerNo: 'CUST-001', productNo: 'PD-1001', productVersion: 'A', roNo: 'RO-26041', moNo: 'MO-26041-01', orgLotNo: 'ORG-26041', lotNo: 'LOT-26041-02', resClass: 1, resType: 'EQP-LAM-01', resValue: 0.67, stdValue: 0.75, inputQty: 18, unitNo: 'EA' },
  { id: 7, customerNo: 'CUST-002', productNo: 'PD-1002', productVersion: 'B', roNo: 'RO-26058', moNo: 'MO-26058-02', orgLotNo: 'ORG-26058', lotNo: 'LOT-26058-01', resClass: 0, resType: 'EMP-014', resValue: 2.1, stdValue: 1.8, inputQty: 36, unitNo: 'EA' },
  { id: 8, customerNo: 'CUST-002', productNo: 'PD-1002', productVersion: 'B', roNo: 'RO-26058', moNo: 'MO-26058-02', orgLotNo: 'ORG-26058', lotNo: 'LOT-26058-01', resClass: 1, resType: 'EQP-TEST-07', resValue: 1.45, stdValue: 1.6, inputQty: 36, unitNo: 'EA' },
  { id: 9, customerNo: 'CUST-002', productNo: 'PD-1002', productVersion: 'B', roNo: 'RO-26058', moNo: 'MO-26058-02', orgLotNo: 'ORG-26058', lotNo: 'LOT-26058-01', resClass: 3, resType: 'MAT-FILM-006', resValue: 36, stdValue: 36, inputQty: 0, unitNo: 'EA' },
  { id: 10, customerNo: 'CUST-003', productNo: 'PD-1003', productVersion: 'A', roNo: 'RO-26073', moNo: 'MO-26073-01', orgLotNo: 'ORG-26073', lotNo: 'LOT-26073-01', resClass: 0, resType: 'EMP-032', resValue: 1.72, stdValue: 1.5, inputQty: 16, unitNo: 'EA' },
  { id: 11, customerNo: 'CUST-003', productNo: 'PD-1003', productVersion: 'A', roNo: 'RO-26073', moNo: 'MO-26073-01', orgLotNo: 'ORG-26073', lotNo: 'LOT-26073-01', resClass: 1, resType: 'EQP-WASH-03', resValue: 0.56, stdValue: 0.62, inputQty: 16, unitNo: 'EA' },
  { id: 12, customerNo: 'CUST-003', productNo: 'PD-1003', productVersion: 'A', roNo: 'RO-26073', moNo: 'MO-26073-01', orgLotNo: 'ORG-26073', lotNo: 'LOT-26073-01', resClass: 3, resType: 'MAT-SOLV-002', resValue: 5.76, stdValue: 5.2, inputQty: 0, unitNo: 'L' },
]

const filteredRows = computed(() => {
  if (!searched.value) return []
  const criteria = activeConditions.value
  return sampleRows.filter(row =>
    (!criteria.customerNo || row.customerNo === criteria.customerNo) &&
    (!criteria.productNo || (row.productNo === criteria.productNo && row.productVersion === criteria.productVersion)) &&
    (!criteria.roNo || row.roNo === criteria.roNo) &&
    (!criteria.moNo || row.moNo === criteria.moNo),
  )
})
const visibleRows = computed(() => [...filteredRows.value].sort((left, right) => {
  const a = left[sortField.value]
  const b = right[sortField.value]
  const difference = typeof a === 'number' && typeof b === 'number'
    ? a - b
    : String(a).localeCompare(String(b), 'zh-TW', { numeric: true })
  return sortDirection.value === 'asc' ? difference : -difference
}))
const lookupOptions = computed<LookupOption[]>(() => {
  const options: Record<LookupKind, LookupOption[]> = {
    customer: [
      { code: 'CUST-001', name: '台灣精密科技' },
      { code: 'CUST-002', name: '新世代電子' },
      { code: 'CUST-003', name: '光學應用股份有限公司' },
    ],
    product: [
      { code: 'PD-1001', name: '控制器模組', version: 'A' },
      { code: 'PD-1002', name: '感測器基板', version: 'B' },
      { code: 'PD-1003', name: '光學模組', version: 'A' },
    ],
  }
  const needle = lookupSearch.value.trim().toUpperCase()
  return options[lookupKind.value ?? 'customer'].filter(item => !needle || item.code.includes(needle) || item.name.toUpperCase().includes(needle))
})

function setEnabled(field: 'customer' | 'product' | 'ro' | 'mo', enabled: boolean): void {
  if (field === 'customer') { customerEnabled.value = enabled; if (!enabled) customerNo.value = '' }
  if (field === 'product') { productEnabled.value = enabled; if (!enabled) { productNo.value = ''; productVersion.value = '' } }
  if (field === 'ro') { roEnabled.value = enabled; if (!enabled) roNo.value = '' }
  if (field === 'mo') { moEnabled.value = enabled; if (!enabled) moNo.value = '' }
}
function syncTextCondition(field: 'ro' | 'mo'): void {
  if (field === 'ro') roEnabled.value = roNo.value.trim().length > 0
  else moEnabled.value = moNo.value.trim().length > 0
}
function openLookup(kind: LookupKind): void {
  lookupKind.value = kind
  lookupSearch.value = kind === 'customer' ? customerNo.value : productNo.value
}
function selectLookup(item: LookupOption): void {
  if (lookupKind.value === 'customer') { customerNo.value = item.code; customerEnabled.value = true }
  else if (lookupKind.value === 'product') { productNo.value = item.code; productVersion.value = item.version ?? ''; productEnabled.value = true }
  lookupKind.value = null
}
function toggleSort(field: ResourceField): void {
  if (sortField.value === field) sortDirection.value = sortDirection.value === 'asc' ? 'desc' : 'asc'
  else { sortField.value = field; sortDirection.value = 'asc' }
}
function sortMark(field: ResourceField): string {
  return sortField.value === field ? (sortDirection.value === 'asc' ? ' ▲' : ' ▼') : ''
}
function groupKey(row: ResourceRow): string { return `${row.moNo}|${row.orgLotNo}|${row.lotNo}` }
function isGroupStart(index: number): boolean { return index === 0 || groupKey(visibleRows.value[index - 1]) !== groupKey(visibleRows.value[index]) }
function classLabel(value: number): string { return ['EMP', 'EQP', 'OS', 'MTL'][value] ?? String(value) }
function formatNumber(value: number): string { return value.toLocaleString('zh-TW', { maximumFractionDigits: 8 }) }
async function query(): Promise<void> {
  error.value = ''
  const selected = [customerEnabled.value, productEnabled.value, roEnabled.value, moEnabled.value]
  if (!selected.some(Boolean)) { error.value = '請至少選擇一項查詢條件。'; searched.value = false; return }
  if (customerEnabled.value && !customerNo.value.trim()) error.value = '已勾選 Customer No，請選擇客戶。'
  else if (productEnabled.value && (!productNo.value.trim() || !productVersion.value.trim())) error.value = '已勾選 Product No，請選擇產品及版本。'
  else if (roEnabled.value && !roNo.value.trim()) error.value = '已勾選 RO No，請輸入工單。'
  else if (moEnabled.value && !moNo.value.trim()) error.value = '已勾選 MO No，請輸入工單。'
  if (error.value) return
  loading.value = true
  searched.value = false
  await new Promise<void>(resolve => window.setTimeout(resolve, 150))
  activeConditions.value = {
    customerNo: customerEnabled.value ? customerNo.value.trim().toUpperCase() : '',
    productNo: productEnabled.value ? productNo.value.trim().toUpperCase() : '',
    productVersion: productEnabled.value ? productVersion.value.trim().toUpperCase() : '',
    roNo: roEnabled.value ? roNo.value.trim().toUpperCase() : '',
    moNo: moEnabled.value ? moNo.value.trim().toUpperCase() : '',
  }
  searched.value = true
  loading.value = false
}
</script>

<template>
  <main class="mo-resource-page" aria-label="工單耗用資源檢視 DEV 預覽">
    <header class="page-heading">
      <div><span class="eyebrow">RPT / RT605</span><h1>工單耗用資源檢視</h1><p>依客戶、產品、RO 或 MO 查詢在製批次資源耗用。</p></div>
      <span class="dev-badge">DEV 假資料</span>
    </header>

    <form class="query-panel" @submit.prevent="query">
      <div class="condition"><label class="condition-title"><input v-model="customerEnabled" type="checkbox" @change="setEnabled('customer', customerEnabled)"><span>Customer No</span></label>
        <input v-model="customerNo" type="text" readonly placeholder="選擇客戶" :disabled="!customerEnabled" @click="customerEnabled && openLookup('customer')">
        <button type="button" class="picker" :disabled="!customerEnabled" aria-label="選擇客戶" @click="openLookup('customer')">…</button>
      </div>
      <div class="condition product-condition"><label class="condition-title"><input v-model="productEnabled" type="checkbox" @change="setEnabled('product', productEnabled)"><span>Product No</span></label>
        <input v-model="productNo" type="text" readonly placeholder="選擇產品" :disabled="!productEnabled" @click="productEnabled && openLookup('product')">
        <input v-model="productVersion" class="version" type="text" readonly placeholder="Ver" :disabled="!productEnabled" @click="productEnabled && openLookup('product')">
        <button type="button" class="picker" :disabled="!productEnabled" aria-label="選擇產品" @click="openLookup('product')">…</button>
      </div>
      <div class="condition"><label class="condition-title"><input v-model="roEnabled" type="checkbox" @change="setEnabled('ro', roEnabled)"><span>RO No</span></label>
        <input v-model="roNo" type="text" maxlength="50" placeholder="輸入 RO No" @input="syncTextCondition('ro')" @blur="roNo = roNo.trim().toUpperCase()">
      </div>
      <div class="condition"><label class="condition-title"><input v-model="moEnabled" type="checkbox" @change="setEnabled('mo', moEnabled)"><span>MO No</span></label>
        <input v-model="moNo" type="text" maxlength="50" placeholder="輸入 MO No" @input="syncTextCondition('mo')" @blur="moNo = moNo.trim().toUpperCase()">
      </div>
      <div class="form-actions"><button type="submit" class="primary" :disabled="loading">{{ loading ? '查詢中…' : 'Query' }}</button><button type="button" @click="emit('close')">Close (X)</button></div>
      <p v-if="error" class="error" role="alert">{{ error }}</p>
    </form>

    <section class="result-panel" aria-label="耗用資源結果">
      <div class="table-scroll"><table>
        <thead><tr>
          <th><button type="button" @click="toggleSort('moNo')">MONo{{ sortMark('moNo') }}</button></th>
          <th><button type="button" @click="toggleSort('orgLotNo')">OrgLotNo{{ sortMark('orgLotNo') }}</button></th>
          <th><button type="button" @click="toggleSort('lotNo')">LotNo{{ sortMark('lotNo') }}</button></th>
          <th><button type="button" @click="toggleSort('resClass')">RESClass{{ sortMark('resClass') }}</button></th>
          <th><button type="button" @click="toggleSort('resType')">RESType{{ sortMark('resType') }}</button></th>
          <th class="numeric"><button type="button" @click="toggleSort('resValue')">RESValue{{ sortMark('resValue') }}</button></th>
          <th class="numeric"><button type="button" @click="toggleSort('stdValue')">STDValue{{ sortMark('stdValue') }}</button></th>
          <th class="numeric"><button type="button" @click="toggleSort('inputQty')">InputQty{{ sortMark('inputQty') }}</button></th>
          <th><button type="button" @click="toggleSort('unitNo')">UnitNo{{ sortMark('unitNo') }}</button></th>
        </tr></thead>
        <tbody v-if="visibleRows.length"><template v-for="(row, index) in visibleRows" :key="row.id">
          <tr v-if="isGroupStart(index)" class="group-heading"><td colspan="9"><span>{{ row.moNo }}</span><span>{{ row.orgLotNo }}</span><span>{{ row.lotNo }}</span></td></tr>
          <tr><td>{{ row.moNo }}</td><td>{{ row.orgLotNo }}</td><td>{{ row.lotNo }}</td><td><span class="resource-class" :class="`class-${row.resClass}`">{{ classLabel(row.resClass) }}</span></td><td>{{ row.resType }}</td><td class="numeric">{{ formatNumber(row.resValue) }}</td><td class="numeric">{{ formatNumber(row.stdValue) }}</td><td class="numeric">{{ formatNumber(row.inputQty) }}</td><td>{{ row.unitNo }}</td></tr>
        </template></tbody>
        <tbody v-else><tr><td colspan="9" class="empty-state">{{ loading ? '載入中…' : searched ? '查無符合資料。' : '設定一項以上條件後按 Query。' }}</td></tr></tbody>
      </table></div>
      <footer class="table-footer">固定虛構資料，只在本機記憶體篩選；不呼叫 API、Oracle 或 PostgreSQL。資源類別：0 EMP／1 EQP／2 OS／3 MTL。</footer>
    </section>

    <div v-if="lookupKind" class="modal-backdrop" @click.self="lookupKind = null">
      <section class="lookup-modal" role="dialog" aria-modal="true" :aria-label="lookupKind === 'customer' ? '選擇客戶' : '選擇產品'">
        <header><div><span class="eyebrow">DEV LOOKUP</span><h2>{{ lookupKind === 'customer' ? 'Customer No' : 'Product No / Version' }}</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="lookupKind = null">×</button></header>
        <form class="lookup-search" @submit.prevent><input v-model="lookupSearch" type="search" :placeholder="lookupKind === 'customer' ? '搜尋客戶編號或名稱' : '搜尋產品編號或名稱'"><button type="submit">搜尋</button></form>
        <div class="lookup-list"><button v-for="item in lookupOptions" :key="`${item.code}-${item.version ?? ''}`" type="button" @click="selectLookup(item)"><strong>{{ item.code }}<small v-if="item.version"> / {{ item.version }}</small></strong><span>{{ item.name }}</span></button><p v-if="!lookupOptions.length" class="lookup-empty">沒有符合的 DEV 選項。</p></div>
      </section>
    </div>
  </main>
</template>

<style scoped>
.mo-resource-page{box-sizing:border-box;display:flex;height:100%;min-height:0;flex-direction:column;gap:10px;overflow:hidden;padding:12px;background:#f3f2ed;color:#292820;font:inherit}.page-heading{display:flex;align-items:flex-start;justify-content:space-between;gap:12px}.eyebrow{color:#777366;font-size:9px;letter-spacing:.1em}.page-heading h1{margin:2px 0;font-size:18px}.page-heading p{margin:0;color:#777366;font-size:10px}.dev-badge{padding:4px 8px;border:1px solid #d6c995;border-radius:12px;background:#f4efdc;color:#705c17;font-size:10px;font-weight:700}.query-panel,.result-panel{border:1px solid #d4d1c5;background:#fffefa;box-shadow:0 1px 2px #26251f0a}.query-panel{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:8px;padding:10px}.condition{display:flex;align-items:center;gap:6px;min-width:0;font-size:10px}.condition>span{width:75px;flex:0 0 75px;font-weight:700}.condition input[type=checkbox]{accent-color:#344b45}.condition input[type=text]{box-sizing:border-box;min-width:0;width:100%;height:26px;padding:3px 6px;border:1px solid #c9c6bb;border-radius:2px;background:#fff;color:#292820;font:inherit}.condition input:disabled{background:#efeee8;color:#8b887e}.condition .version{max-width:58px}.picker{height:26px;min-width:26px;border:1px solid #c9c6bb;background:#f3f2ed;color:#343329;font:inherit;cursor:pointer}.picker:disabled{color:#aaa79b;cursor:default}.form-actions{grid-column:1/-1;display:flex;justify-content:flex-end;gap:6px}.form-actions button{min-height:27px;padding:4px 12px;border:1px solid #c9c6bb;border-radius:2px;background:#f3f2ed;color:#343329;font:inherit;cursor:pointer}.form-actions button.primary{border-color:#364d46;background:#364d46;color:white}.form-actions button:disabled{opacity:.6;cursor:wait}.error,.lookup-error{grid-column:1/-1;margin:0;color:#a32121;font-size:10px}.result-panel{display:flex;min-height:0;flex:1;flex-direction:column;overflow:hidden}.table-scroll{min-height:0;flex:1;overflow:auto}table{width:100%;border-collapse:collapse;font-size:10px;white-space:nowrap}th{position:sticky;top:0;z-index:1;background:#ecebe5;text-align:left}th,td{padding:5px 7px;border-bottom:1px solid #e6e4dc}th button{padding:0;border:0;background:none;color:inherit;font:inherit;font-weight:700;text-align:left;cursor:pointer}.numeric{text-align:right;font-variant-numeric:tabular-nums}.group-heading td{background:#f6f3e7;color:#615a43;font-weight:700}.group-heading span{margin-right:18px}.resource-class{display:inline-block;min-width:30px;padding:2px 4px;border-radius:2px;text-align:center;font-weight:700}.class-0{background:#e9f0e9;color:#375c42}.class-1{background:#e7edf4;color:#365a78}.class-2{background:#f2e9df;color:#835322}.class-3{background:#f0ecdc;color:#726125}.empty-state{padding:26px;text-align:center;color:#777366}.table-footer{padding:7px 9px;border-top:1px solid #e6e4dc;color:#777366;font-size:9px}.modal-backdrop{position:fixed;inset:0;z-index:20;display:grid;place-items:center;padding:16px;background:#17181266}.lookup-modal{box-sizing:border-box;width:min(470px,100%);max-height:80vh;overflow:hidden;border:1px solid #bcb8aa;background:#fffefa;box-shadow:0 12px 35px #17181240}.lookup-modal>header{display:flex;align-items:center;justify-content:space-between;padding:12px;border-bottom:1px solid #e6e4dc}.lookup-modal h2{margin:2px 0 0;font-size:15px}.icon-button{width:27px;height:27px;border:1px solid #d4d1c5;background:#f3f2ed;font-size:19px;cursor:pointer}.lookup-search{display:flex;gap:6px;padding:10px}.lookup-search input{min-width:0;flex:1;height:28px;padding:3px 7px;border:1px solid #c9c6bb;font:inherit}.lookup-search button{padding:4px 10px;border:1px solid #364d46;background:#364d46;color:#fff;font:inherit;cursor:pointer}.lookup-list{max-height:48vh;overflow:auto;padding:0 10px 10px}.lookup-list>button{display:flex;width:100%;justify-content:space-between;gap:10px;padding:9px 8px;border:0;border-bottom:1px solid #e6e4dc;background:white;color:#292820;text-align:left;font:inherit;cursor:pointer}.lookup-list>button:hover{background:#f3f2ed}.lookup-list small{color:#777366}.lookup-empty{padding:14px;text-align:center;color:#777366;font-size:10px}@media(max-width:700px){.query-panel{grid-template-columns:1fr}.form-actions{grid-column:auto}.condition>span{width:68px;flex-basis:68px}.group-heading span{margin-right:8px}}
.condition-title{display:flex;align-items:center;gap:4px;width:91px;flex:0 0 91px;font-weight:700}.condition-title input{accent-color:#344b45}.condition-title span{font-weight:700}@media(max-width:700px){.condition-title{width:82px;flex-basis:82px}}
</style>
