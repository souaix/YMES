<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

type FilterKey = 'roNo' | 'moNo' | 'productNo' | 'customerNo' | 'lotNo' | 'opNo' | 'orgLotNo' | 'psNo'
type LookupKind = 'customer' | 'product' | 'ps' | 'op'
interface WaitLotRow {
  lotNo: string
  productNo: string
  productName: string
  productVersion: string
  opNo: string
  brNo: string
  status: 2
  curQty: number
  curUnitNo: string
  roNo: string
  moNo: string
  customerNo: string
  orgLotNo: string
  psNo: string
}
interface LookupOption { value: string; label: string; extra?: string }

const emit = defineEmits<{ close: [] }>()
const sampleRows: WaitLotRow[] = [
  { lotNo: 'LOT-261119-B04', productNo: 'PRD-5002', productName: 'Demo Product Beta', productVersion: '01', opNo: 'OP035', brNo: 'BR-035', status: 2, curQty: 415.5, curUnitNo: 'PCS', roNo: 'RO-261119-02', moNo: 'MO-261119-03', customerNo: 'CUS-002', orgLotNo: 'ORG-261119-B04', psNo: 'PS-20' },
  { lotNo: 'LOT-261121-A01', productNo: 'PRD-4001', productName: 'Demo Product Alpha', productVersion: '03', opNo: 'OP020', brNo: 'BR-020', status: 2, curQty: 760, curUnitNo: 'PCS', roNo: 'RO-261121-01', moNo: 'MO-261121-01', customerNo: 'CUS-001', orgLotNo: 'ORG-261121-A01', psNo: 'PS-10' },
  { lotNo: 'LOT-261122-C08', productNo: 'PRD-7100', productName: 'Demo Product Gamma', productVersion: '02', opNo: 'OP045', brNo: 'BR-045', status: 2, curQty: 240.125, curUnitNo: 'PCS', roNo: 'RO-261122-04', moNo: 'MO-261122-02', customerNo: 'CUS-001', orgLotNo: 'ORG-261122-C08', psNo: 'PS-30' },
  { lotNo: 'LOT-261123-A02', productNo: 'PRD-4001', productName: 'Demo Product Alpha', productVersion: '03', opNo: 'OP020', brNo: 'BR-020', status: 2, curQty: 840, curUnitNo: 'PCS', roNo: 'RO-261123-01', moNo: 'MO-261123-01', customerNo: 'CUS-001', orgLotNo: 'ORG-261123-A02', psNo: 'PS-10' },
  { lotNo: 'LOT-261124-D03', productNo: 'PRD-5002', productName: 'Demo Product Beta', productVersion: '01', opNo: 'OP060', brNo: 'BR-060', status: 2, curQty: 55, curUnitNo: 'PCS', roNo: 'RO-261124-01', moNo: 'MO-261124-03', customerNo: 'CUS-002', orgLotNo: 'ORG-261124-D03', psNo: 'PS-20' },
  { lotNo: 'LOT-261125-F06', productNo: 'PRD-7100', productName: 'Demo Product Gamma', productVersion: '02', opNo: 'OP070', brNo: 'BR-070', status: 2, curQty: 8.75, curUnitNo: 'PCS', roNo: 'RO-261125-01', moNo: 'MO-261125-02', customerNo: 'CUS-002', orgLotNo: 'ORG-261125-F06', psNo: 'PS-30' },
]

const customers: LookupOption[] = [
  { value: 'CUS-001', label: 'CUS-001 · Demo Customer East' },
  { value: 'CUS-002', label: 'CUS-002 · Demo Customer West' },
]
const products: LookupOption[] = [
  { value: 'PRD-4001', extra: '03', label: 'PRD-4001 · Demo Product Alpha · Ver 03' },
  { value: 'PRD-5002', extra: '01', label: 'PRD-5002 · Demo Product Beta · Ver 01' },
  { value: 'PRD-7100', extra: '02', label: 'PRD-7100 · Demo Product Gamma · Ver 02' },
]
const productionSegments: LookupOption[] = [
  { value: 'PS-10', label: 'PS-10 · Assembly' }, { value: 'PS-20', label: 'PS-20 · Test' }, { value: 'PS-30', label: 'PS-30 · Pack' },
]
const operations: LookupOption[] = [
  { value: 'OP010', label: 'OP010 · Wafer Prep' }, { value: 'OP020', label: 'OP020 · Attach' },
  { value: 'OP035', label: 'OP035 · Test' }, { value: 'OP045', label: 'OP045 · Inspection' }, { value: 'OP060', label: 'OP060 · Stock In' },
  { value: 'OP070', label: 'OP070 · Final Inspection' },
]

const filters = reactive<Record<FilterKey, string>>({ roNo: '', moNo: '', productNo: '', customerNo: '', lotNo: '', opNo: '', orgLotNo: '', psNo: '' })
const enabled = reactive<Record<FilterKey, boolean>>({ roNo: false, moNo: false, productNo: false, customerNo: false, lotNo: false, opNo: false, orgLotNo: false, psNo: false })
const productName = ref('')
const productVersion = ref('')
const lookupKind = ref<LookupKind | null>(null)
const lookupText = ref('')
const resultRows = ref<WaitLotRow[]>([])
const hasSearched = ref(false)
const busy = ref(false)
const notice = ref('')

const statusLabels: Record<number, string> = { 0: 'Queue', 1: 'Running', 2: 'Wait', 3: 'Hold', 4: 'Complete', 11: 'INV', 99: 'Closed' }
const visibleRows = computed(() => resultRows.value.slice().sort((a, b) => a.lotNo.localeCompare(b.lotNo) || a.opNo.localeCompare(b.opNo)))
const currentLookupOptions = computed(() => {
  const options = lookupKind.value === 'customer' ? customers : lookupKind.value === 'product' ? products : lookupKind.value === 'ps' ? productionSegments : operations
  const search = lookupText.value.trim().toUpperCase()
  return options.filter((option) => option.label.toUpperCase().includes(search) || option.value.toUpperCase().includes(search))
})
const dialogTitle = computed(() => lookupKind.value === 'customer' ? '選擇 Customer No' : lookupKind.value === 'product' ? '選擇 Product No / Version' : lookupKind.value === 'ps' ? '選擇 PS No' : '選擇 OP No')
const criteriaSummary = computed(() => {
  const entries = (Object.keys(filters) as FilterKey[]).filter((key) => enabled[key] && filters[key].trim())
  return entries.length ? entries.map((key) => `${fieldLabels[key]}=${filters[key]}`).join(' · ') : '無額外條件'
})
const fieldLabels: Record<FilterKey, string> = { roNo: 'RO No', moNo: 'MO No', productNo: 'Product No', customerNo: 'Customer No', lotNo: 'Lot No', opNo: 'OP No', orgLotNo: 'Org Lot No', psNo: 'PS No' }

function setTextFilter(key: FilterKey) { enabled[key] = Boolean(filters[key].trim()) }
function setEnabled(key: FilterKey, value: boolean) {
  enabled[key] = value
  if (value) return
  filters[key] = ''
  if (key === 'productNo') { productName.value = ''; productVersion.value = '' }
}
function openLookup(kind: LookupKind) { lookupKind.value = kind; lookupText.value = '' }
function closeLookup() { lookupKind.value = null }
function chooseLookup(option: LookupOption) {
  switch (lookupKind.value) {
    case 'customer': filters.customerNo = option.value; enabled.customerNo = true; break
    case 'product': filters.productNo = option.value; productVersion.value = option.extra ?? ''; productName.value = option.label.split(' · ')[1] ?? ''; enabled.productNo = true; break
    case 'ps': filters.psNo = option.value; enabled.psNo = true; break
    case 'op': filters.opNo = option.value; enabled.opNo = true; break
  }
  closeLookup()
}
function likeMatches(value: string, pattern: string) {
  const escaped = pattern.replace(/[.*+?^${}()|[\]\\]/g, '\\$&').replace(/%/g, '.*').replace(/_/g, '.')
  return new RegExp(`^${escaped}$`, 'i').test(value)
}
function matches(row: WaitLotRow) {
  const active = (key: FilterKey) => enabled[key] && filters[key].trim()
  if (row.status !== 2) return false
  if (active('roNo') && !likeMatches(row.roNo, filters.roNo.trim())) return false
  if (active('moNo') && !likeMatches(row.moNo, filters.moNo.trim())) return false
  if (active('productNo') && !likeMatches(row.productNo, filters.productNo.trim())) return false
  if (active('productNo') && productVersion.value && !productVersion.value.split('|').includes(row.productVersion)) return false
  if (active('customerNo') && row.customerNo !== filters.customerNo.trim()) return false
  if (active('lotNo') && !filters.lotNo.split('|').map((value) => value.trim()).includes(row.lotNo)) return false
  if (active('opNo') && row.opNo !== filters.opNo.trim()) return false
  if (active('orgLotNo') && !likeMatches(row.orgLotNo, filters.orgLotNo.trim())) return false
  if (active('psNo') && row.psNo !== filters.psNo.trim()) return false
  return true
}
function runQuery() {
  if (busy.value) return
  busy.value = true
  for (const key of ['roNo', 'moNo', 'lotNo', 'orgLotNo'] as FilterKey[]) filters[key] = filters[key].trim().toUpperCase()
  resultRows.value = sampleRows.filter(matches)
  hasSearched.value = true
  notice.value = criteriaSummary.value === '無額外條件'
    ? '來源固定查詢 Status=2（Wait），空條件仍會執行 ReportMode 查詢；DEV 僅回傳有限假資料。正式 API 應授權、限量並分頁。'
    : ''
  window.setTimeout(() => { busy.value = false }, 160)
}
function quantity(value: number) { return value.toFixed(4).replace(/0+$/, '').replace(/\.$/, '') }
</script>

<template>
  <main class="wait-lot" aria-label="等待生產批查詢 DEV" :aria-busy="busy">
    <header class="page-heading">
      <div><div class="eyebrow">RPT / RT503</div><h1>等待生產批查詢</h1><p>查詢目前 Status=2（Wait）的生產批；此查詢不限定 LotState。</p></div>
      <span class="dev-badge">DEV 假資料</span>
    </header>

    <section class="query-panel" aria-label="查詢條件">
      <div class="query-grid">
        <div class="criteria-field"><label class="criteria-check"><input type="checkbox" :checked="enabled.customerNo" @change="setEnabled('customerNo', ($event.target as HTMLInputElement).checked)"><span>Customer No</span></label><div class="picker-input"><input :value="filters.customerNo" readonly placeholder="選擇客戶"><button type="button" aria-label="選擇客戶" @click="openLookup('customer')">…</button></div></div>
        <div class="criteria-field"><label class="criteria-check"><input type="checkbox" :checked="enabled.roNo" @change="setEnabled('roNo', ($event.target as HTMLInputElement).checked)"><span>RO No</span></label><input v-model="filters.roNo" type="text" placeholder="輸入 RO No" @input="setTextFilter('roNo')" @focus="($event.target as HTMLInputElement).select()" @blur="filters.roNo = filters.roNo.toUpperCase()"></div>
        <div class="criteria-field"><label class="criteria-check"><input type="checkbox" :checked="enabled.productNo" @change="setEnabled('productNo', ($event.target as HTMLInputElement).checked)"><span>Product No</span></label><div class="product-picker"><div class="picker-input"><input :value="filters.productNo" readonly placeholder="選擇產品"><button type="button" aria-label="選擇產品" @click="openLookup('product')">…</button></div><input :value="productName" readonly aria-label="Product Name" placeholder="Product Name"><input :value="productVersion" readonly aria-label="Product Version" placeholder="Ver"></div></div>
        <div class="criteria-field"><label class="criteria-check"><input type="checkbox" :checked="enabled.moNo" @change="setEnabled('moNo', ($event.target as HTMLInputElement).checked)"><span>MO No</span></label><input v-model="filters.moNo" type="text" placeholder="輸入 MO No" @input="setTextFilter('moNo')" @focus="($event.target as HTMLInputElement).select()" @blur="filters.moNo = filters.moNo.toUpperCase()"></div>
        <div class="criteria-field"><label class="criteria-check"><input type="checkbox" :checked="enabled.psNo" @change="setEnabled('psNo', ($event.target as HTMLInputElement).checked)"><span>PS No</span></label><div class="picker-input"><input :value="filters.psNo" readonly placeholder="選擇 PS"><button type="button" aria-label="選擇 PS" @click="openLookup('ps')">…</button></div></div>
        <div class="criteria-field"><label class="criteria-check"><input type="checkbox" :checked="enabled.orgLotNo" @change="setEnabled('orgLotNo', ($event.target as HTMLInputElement).checked)"><span>Org Lot No</span></label><input v-model="filters.orgLotNo" type="text" placeholder="輸入原始批號" @input="setTextFilter('orgLotNo')" @focus="($event.target as HTMLInputElement).select()" @blur="filters.orgLotNo = filters.orgLotNo.toUpperCase()"></div>
        <div class="criteria-field"><label class="criteria-check"><input type="checkbox" :checked="enabled.opNo" @change="setEnabled('opNo', ($event.target as HTMLInputElement).checked)"><span>OP No</span></label><div class="picker-input"><input :value="filters.opNo" readonly placeholder="選擇 OP"><button type="button" aria-label="選擇 OP" @click="openLookup('op')">…</button></div></div>
        <div class="criteria-field"><label class="criteria-check"><input type="checkbox" :checked="enabled.lotNo" @change="setEnabled('lotNo', ($event.target as HTMLInputElement).checked)"><span>Lot No</span></label><input v-model="filters.lotNo" type="text" placeholder="批號；可用 | 分隔" @input="setTextFilter('lotNo')" @focus="($event.target as HTMLInputElement).select()" @blur="filters.lotNo = filters.lotNo.toUpperCase()"></div>
      </div>
      <button type="button" class="query-button" :disabled="busy" @click="runQuery">{{ busy ? '查詢中…' : 'Query' }}</button>
    </section>

    <section class="result-panel" aria-label="等待生產批查詢結果">
      <div class="result-heading"><div><h2>Wait Lots</h2><p>{{ hasSearched ? `Status=2（Wait） · ${criteriaSummary}` : '固定 Status=2（Wait）；可不指定其他條件。' }}</p></div><span>{{ hasSearched ? `${visibleRows.length} 筆` : '尚未查詢' }}</span></div>
      <p v-if="notice" class="legacy-note">{{ notice }}</p>
      <div class="table-wrap">
        <table>
          <thead><tr><th>Product No</th><th>Product Name</th><th>Product Version</th><th aria-sort="ascending">Lot No ↑</th><th aria-sort="ascending">OP No ↑</th><th>BR No</th><th>Status</th><th class="qty-col">Cur Qty</th><th>CurUnit No</th></tr></thead>
          <tbody v-if="hasSearched && visibleRows.length">
            <tr v-for="row in visibleRows" :key="`${row.lotNo}|${row.opNo}`"><td class="mono">{{ row.productNo }}</td><td>{{ row.productName }}</td><td>{{ row.productVersion }}</td><td class="mono">{{ row.lotNo }}</td><td>{{ row.opNo }}</td><td>{{ row.brNo }}</td><td><span class="status-pill status-2">{{ statusLabels[row.status] }}</span></td><td class="qty">{{ quantity(row.curQty) }}</td><td>{{ row.curUnitNo }}</td></tr>
          </tbody>
          <tbody v-else><tr><td colspan="9" class="empty-state">{{ hasSearched ? '沒有符合條件的 Wait Lot' : '尚未執行查詢' }}</td></tr></tbody>
        </table>
      </div>
      <footer class="grid-footer"><span>排序：Lot No → OP No</span><span>Grid 隱藏來源 Status 欄，畫面顯示 StatusShow。</span></footer>
    </section>
    <footer class="page-footer"><span>查詢來源：有限 DEV 記憶體假資料</span><button class="close-button" type="button" @click="emit('close')">Close (X)</button></footer>

    <div v-if="lookupKind" class="modal-backdrop" @click.self="closeLookup">
      <section class="lookup-dialog" role="dialog" aria-modal="true" :aria-label="dialogTitle">
        <header><div><h2>{{ dialogTitle }}</h2><p>舊表單使用對應主檔選取視窗；這裡以有限 DEV 假資料模擬。</p></div><button type="button" aria-label="關閉" @click="closeLookup">×</button></header>
        <input v-model="lookupText" type="search" placeholder="搜尋編號或名稱">
        <ul v-if="currentLookupOptions.length"><li v-for="option in currentLookupOptions" :key="option.value + (option.extra ?? '')"><button type="button" @click="chooseLookup(option)">{{ option.label }}</button></li></ul>
        <p v-else class="no-options">沒有符合的假資料。</p>
      </section>
    </div>
  </main>
</template>

<style scoped>
.wait-lot{box-sizing:border-box;display:flex;flex-direction:column;gap:11px;min-height:0;height:100%;padding:12px;background:#f4f3ee;color:#292820;font:inherit;overflow:auto}.page-heading{display:flex;justify-content:space-between;gap:14px}.eyebrow{margin-bottom:3px;color:#777366;font-size:10px;letter-spacing:.1em}.page-heading h1{margin:0;font-size:19px;font-weight:650}.page-heading p{margin:4px 0 0;color:#777366;font-size:11px}.dev-badge{height:max-content;padding:5px 9px;border:1px solid #d3c79c;border-radius:3px;background:#f5efd8;color:#66571f;font-size:11px;font-weight:700}.query-panel{display:flex;align-items:flex-end;gap:10px;padding:10px;border:1px solid #cbc9bd;background:#fbfaf6}.query-grid{display:grid;flex:1;grid-template-columns:repeat(4,minmax(175px,1fr));gap:8px 12px}.criteria-field{display:grid;grid-template-columns:82px minmax(0,1fr);align-items:center;gap:5px;font-size:10px}.criteria-check{display:flex;align-items:center;gap:4px;white-space:nowrap}.criteria-check input{margin:0}.criteria-field>input[type=text],.picker-input input,.product-picker>input{box-sizing:border-box;min-width:0;width:100%;height:27px;padding:4px 6px;border:1px solid #aaa79b;border-radius:2px;background:#fff;color:#292820;font:inherit}.criteria-field input[readonly]{background:#f0efe9}.picker-input{display:flex;min-width:0}.picker-input input{border-radius:2px 0 0 2px}.picker-input button{width:28px;flex:none;border:1px solid #aaa79b;border-left:0;border-radius:0 2px 2px 0;background:#eeede7;cursor:pointer}.product-picker{display:grid;grid-template-columns:minmax(90px,1.2fr) minmax(65px,1fr) 38px;gap:4px}.product-picker .picker-input{grid-column:1}.product-picker>input:nth-child(2){grid-column:2}.product-picker>input:nth-child(3){grid-column:3}.query-button,.close-button{min-height:29px;padding:4px 12px;border:1px solid #3b3930;border-radius:3px;background:#3b3930;color:white;font:inherit;cursor:pointer;white-space:nowrap}.query-button:disabled{opacity:.6;cursor:wait}.result-panel{display:flex;flex:1;flex-direction:column;min-height:190px;border:1px solid #cbc9bd;background:#fbfaf6}.result-heading{display:flex;justify-content:space-between;align-items:center;gap:12px;padding:9px 11px;border-bottom:1px solid #d9d7cc}.result-heading h2{margin:0;font-size:13px}.result-heading p{margin:4px 0 0;color:#777366;font-size:10px}.result-heading>span{flex:none;color:#555247;font-size:11px}.legacy-note{margin:8px 11px 0;padding:6px 8px;border-left:3px solid #c69b43;background:#fbf5e5;color:#6e5929;font-size:10px}.table-wrap{flex:1;min-height:140px;overflow:auto}table{width:100%;border-collapse:collapse;font-size:10px;white-space:nowrap}th{position:sticky;top:0;z-index:1;padding:7px;border-right:1px solid #d5d3c7;border-bottom:1px solid #c5c2b5;background:#eeece3;text-align:left;font-weight:650}td{padding:6px 7px;border-right:1px solid #e3e1d8;border-bottom:1px solid #e3e1d8}.qty-col{width:75px}.qty{text-align:right;font-variant-numeric:tabular-nums}.mono{font-family:ui-monospace,SFMono-Regular,Consolas,monospace;font-size:9px}.status-pill{display:inline-block;padding:2px 5px;border-radius:2px;background:#f4e2d8;color:#844933;font-size:9px}.empty-state{height:90px;text-align:center;color:#858173}.grid-footer{display:flex;justify-content:space-between;gap:8px;padding:5px 9px;border-top:1px solid #d9d7cc;color:#777366;font-size:9px}.page-footer{display:flex;justify-content:space-between;align-items:center;color:#777366;font-size:10px}.modal-backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:18px;background:#1d1d1980}.lookup-dialog{box-sizing:border-box;width:min(460px,100%);max-height:85vh;display:flex;flex-direction:column;gap:10px;padding:14px;border:1px solid #aaa797;border-radius:4px;background:#fbfaf6;box-shadow:0 14px 40px #0003}.lookup-dialog header{display:flex;justify-content:space-between;align-items:flex-start}.lookup-dialog h2{margin:0;font-size:15px}.lookup-dialog header p{margin:4px 0 0;color:#777366;font-size:10px}.lookup-dialog header button{width:28px;height:28px;border:1px solid #c8c5b9;background:#f2f1ec;font-size:19px;cursor:pointer}.lookup-dialog>input{height:29px;padding:4px 7px;border:1px solid #aaa797;font:inherit}.lookup-dialog ul{margin:0;padding:0;overflow:auto;list-style:none}.lookup-dialog li+li{border-top:1px solid #e2e0d7}.lookup-dialog li button{width:100%;padding:8px;text-align:left;border:0;background:transparent;font:inherit;cursor:pointer}.lookup-dialog li button:hover{background:#eeeee5}.no-options{text-align:center;padding:18px}@media(max-width:1080px){.query-grid{grid-template-columns:repeat(2,minmax(170px,1fr))}.query-panel{align-items:flex-start;flex-direction:column}.query-button{align-self:flex-end}}@media(max-width:720px){.query-grid{grid-template-columns:1fr}.grid-footer{flex-direction:column}.page-heading h1{font-size:16px}}
</style>
