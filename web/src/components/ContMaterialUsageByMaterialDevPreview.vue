<script setup lang="ts">
import { computed, ref } from 'vue'

type SearchMode = 'material' | 'product'
type UsageRow = {
  rowNo: number; lotNo: string; opNo: string; materialType: string; materialNo: string
  useQty: number; unitNo: string; materialLotNo: string; lotQty: number
  equipmentNo: string; description: string; eventTime: string; productNo: string
}
type SortField = Exclude<keyof UsageRow, 'rowNo' | 'productNo'>
type LookupKind = 'material' | 'product'
type LookupOption = { code: string; name: string }

const emit = defineEmits<{ close: [] }>()
const mode = ref<SearchMode>('material')
const materialNo = ref('')
const productNo = ref('')
const deductFromEnabled = ref(false)
const deductToEnabled = ref(false)
const now = new Date()
const fromTime = ref(toLocalInput(new Date(now.getFullYear() - 1, now.getMonth(), now.getDate(), now.getHours(), now.getMinutes())))
const toTime = ref(toLocalInput(now))
const activeMode = ref<SearchMode | ''>('')
const activePattern = ref('')
const hasSearched = ref(false)
const loading = ref(false)
const error = ref('')
const sortField = ref<SortField>('lotNo')
const sortDirection = ref<'asc' | 'desc'>('asc')
const lookupKind = ref<LookupKind | null>(null)
const lookupSearch = ref('')
const lookupError = ref('')

function toLocalInput(date: Date): string {
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

const sampleRows: UsageRow[] = [
  { rowNo: 1, lotNo: 'LOT-RT602-001', opNo: 'OP210', materialType: 'MAT', materialNo: 'MAT-ADH-010', useQty: 0.82, unitNo: 'G', materialLotNo: 'ADH-26001', lotQty: 24, equipmentNo: 'EQP-LAM-02', description: '主線耗用', eventTime: '2026-11-24T09:24:35', productNo: 'PD-1001' },
  { rowNo: 2, lotNo: 'LOT-RT602-002', opNo: 'OP210', materialType: 'MAT', materialNo: 'MAT-ADH-010', useQty: 0.61, unitNo: 'G', materialLotNo: 'ADH-26003', lotQty: 18, equipmentNo: 'EQP-LAM-01', description: '', eventTime: '2026-11-24T10:52:11', productNo: 'PD-1002' },
  { rowNo: 3, lotNo: 'LOT-RT602-003', opNo: 'OP340', materialType: 'MAT', materialNo: 'MAT-FILM-006', useQty: 1, unitNo: 'EA', materialLotNo: 'FILM-26114', lotQty: 24, equipmentNo: 'EQP-TEST-07', description: '保護膜替換', eventTime: '2026-11-25T14:44:05', productNo: 'PD-1001' },
  { rowNo: 4, lotNo: 'LOT-RT602-004', opNo: 'OP115', materialType: 'MAT', materialNo: 'MAT-SOLV-002', useQty: 0.36, unitNo: 'L', materialLotNo: 'SOL-1126', lotQty: 16, equipmentNo: 'EQP-WASH-03', description: '批次清洗', eventTime: '2026-11-26T08:29:48', productNo: 'PD-1003' },
  { rowNo: 5, lotNo: 'LOT-RT602-005', opNo: 'OP210', materialType: 'MAT', materialNo: 'MAT-ADH-010', useQty: 0.77, unitNo: 'G', materialLotNo: 'ADH-26004', lotQty: 20, equipmentNo: 'EQP-LAM-03', description: 'DEV 假資料', eventTime: '2026-11-26T13:05:22', productNo: 'PD-1001' },
]

function wildcardMatch(value: string, pattern: string): boolean {
  const escaped = pattern.split('%').map(part => part.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')).join('.*')
  return new RegExp(`^${escaped}$`, 'i').test(value)
}
const filteredRows = computed(() => sampleRows.filter(row => {
  if (!activePattern.value || !activeMode.value) return false
  const target = activeMode.value === 'material' ? row.materialNo : row.productNo
  if (!wildcardMatch(target, activePattern.value)) return false
  const event = new Date(row.eventTime).getTime()
  if (deductFromEnabled.value && event < new Date(fromTime.value).getTime()) return false
  if (deductToEnabled.value && event > new Date(toTime.value).getTime()) return false
  return true
}))
const visibleRows = computed(() => [...filteredRows.value].sort((left, right) => {
  const field = sortField.value
  const a = left[field]
  const b = right[field]
  const difference = typeof a === 'number' && typeof b === 'number'
    ? a - b
    : String(a ?? '').localeCompare(String(b ?? ''), 'zh-TW', { numeric: true })
  return sortDirection.value === 'asc' ? difference : -difference
}))
const lookupOptions = computed<LookupOption[]>(() => {
  const material = [
    { code: 'MAT-ADH-010', name: '導電膠 A' },
    { code: 'MAT-FILM-006', name: '保護膜 6 吋' },
    { code: 'MAT-SOLV-002', name: '清洗液 B' },
  ]
  const product = [
    { code: 'PD-1001', name: '控制器模組 A' },
    { code: 'PD-1002', name: '感測器基板 B' },
    { code: 'PD-1003', name: '光學模組 C' },
  ]
  const source = lookupKind.value === 'material' ? material : product
  const needle = lookupSearch.value.trim().replace(/%/g, '').toUpperCase()
  if (lookupKind.value === 'product' && needle.length < 3) return []
  return source.filter(item => !needle || item.code.includes(needle) || item.name.toUpperCase().includes(needle))
})

function setMode(value: SearchMode): void {
  mode.value = value
  materialNo.value = ''
  productNo.value = ''
  error.value = ''
}
function openLookup(kind: LookupKind): void {
  if (kind === 'product' && productNo.value.trim().replace(/%/g, '').length < 3) {
    error.value = 'Product No 選取需至少 3 個固定字元。'
    return
  }
  lookupKind.value = kind
  lookupSearch.value = kind === 'material' ? materialNo.value : productNo.value
  lookupError.value = ''
}
function selectLookup(item: LookupOption): void {
  if (lookupKind.value === 'material') { materialNo.value = item.code; setMode('material'); materialNo.value = item.code }
  else if (lookupKind.value === 'product') { productNo.value = item.code; setMode('product'); productNo.value = item.code }
  lookupKind.value = null
}
function closeLookup(): void { lookupKind.value = null; lookupError.value = '' }
function validateProductLookup(): void {
  const fixedCharacters = lookupSearch.value.trim().replace(/%/g, '').length
  lookupError.value = fixedCharacters < 3 ? 'Product No 選取需至少 3 個固定字元。' : ''
}
function toggleSort(field: SortField): void {
  if (sortField.value === field) sortDirection.value = sortDirection.value === 'asc' ? 'desc' : 'asc'
  else { sortField.value = field; sortDirection.value = 'asc' }
}
function sortMark(field: SortField): string {
  return sortField.value === field ? (sortDirection.value === 'asc' ? ' ▲' : ' ▼') : ''
}
function formatNumber(value: number): string { return value.toLocaleString('zh-TW', { maximumFractionDigits: 4 }) }
function formatTime(value: string): string { return new Date(value).toLocaleString('zh-TW', { hour12: false }) }
async function query(): Promise<void> {
  const pattern = (mode.value === 'material' ? materialNo.value : productNo.value).trim().toUpperCase()
  if (!pattern) { error.value = '請輸入 Material No 或 Product No。'; hasSearched.value = false; activePattern.value = ''; return }
  if (deductFromEnabled.value && !fromTime.value) { error.value = '請輸入 DeductTime 起始時間。'; return }
  if (deductToEnabled.value && !toTime.value) { error.value = '請輸入 To 結束時間。'; return }
  if ((deductFromEnabled.value && !Number.isFinite(new Date(fromTime.value).getTime())) || (deductToEnabled.value && !Number.isFinite(new Date(toTime.value).getTime()))) {
    error.value = '請確認 DeductTime 日期格式。'
    return
  }
  if (deductFromEnabled.value && deductToEnabled.value && new Date(fromTime.value).getTime() > new Date(toTime.value).getTime()) {
    error.value = 'DeductTime 起始時間不可晚於 To 結束時間。'
    return
  }
  loading.value = true
  error.value = ''
  if (mode.value === 'material') materialNo.value = pattern
  else productNo.value = pattern
  await new Promise<void>(resolve => window.setTimeout(resolve, 160))
  activeMode.value = mode.value
  activePattern.value = pattern
  hasSearched.value = true
  loading.value = false
}
</script>

<template>
  <main class="material-query-page" aria-label="物料用於生產批查詢 DEV 預覽">
    <header class="page-heading">
      <div><span class="eyebrow">RPT / RT602</span><h1>物料用於生產批查詢</h1><p>依物料或產品編號查詢物料耗用與 Lot 歷程。</p></div>
      <span class="dev-badge">DEV 假資料</span>
    </header>

    <form class="query-panel" @submit.prevent="query">
      <fieldset class="search-mode">
        <legend>Query by</legend>
        <label><input type="radio" name="search-mode" value="material" :checked="mode === 'material'" @change="setMode('material')"> Material No</label>
        <label><input type="radio" name="search-mode" value="product" :checked="mode === 'product'" @change="setMode('product')"> Product No</label>
      </fieldset>
      <div class="search-input-row">
        <label class="search-field"><span>{{ mode === 'material' ? 'Material No' : 'Product No' }}</span>
          <input v-if="mode === 'material'" v-model="materialNo" type="text" maxlength="50" autocomplete="off" placeholder="可使用 % 萬用字元" @blur="materialNo = materialNo.trim().toUpperCase()">
          <input v-else v-model="productNo" type="text" maxlength="50" autocomplete="off" placeholder="可使用 % 萬用字元" @blur="productNo = productNo.trim().toUpperCase()">
        </label>
        <button type="button" class="picker-button" :aria-label="`選擇${mode === 'material' ? '物料' : '產品'}`" @click="openLookup(mode)">…</button>
        <span class="wildcard-note">The query can use wildcard characters "%"</span>
      </div>
      <div class="date-row">
        <label class="date-check"><input v-model="deductFromEnabled" type="checkbox"> DeductTime From</label>
        <input v-model="fromTime" type="datetime-local" :disabled="!deductFromEnabled" aria-label="DeductTime From">
        <label class="date-check"><input v-model="deductToEnabled" type="checkbox"> To</label>
        <input v-model="toTime" type="datetime-local" :disabled="!deductToEnabled" aria-label="DeductTime To">
        <button type="submit" class="query-button" :disabled="loading">{{ loading ? '查詢中…' : 'Query' }}</button>
        <button type="button" class="close-button" @click="emit('close')">Close (X)</button>
      </div>
      <p v-if="error" class="error-message" role="alert">{{ error }}</p>
      <p class="source-note">正式來源的 Product No／萬用字元篩選和 UI 標示存在差異，詳見 RT602 規格。</p>
    </form>

    <section class="result-panel" aria-label="物料耗用結果">
      <div class="result-toolbar"><strong>Material Usage</strong><span v-if="hasSearched">{{ visibleRows.length }} 筆</span><span v-else>尚未查詢</span></div>
      <div class="table-scroll">
        <table>
          <thead><tr>
            <th><button type="button" @click="toggleSort('lotNo')">Lot No{{ sortMark('lotNo') }}</button></th>
            <th><button type="button" @click="toggleSort('opNo')">OP No{{ sortMark('opNo') }}</button></th>
            <th><button type="button" @click="toggleSort('materialType')">Material Type{{ sortMark('materialType') }}</button></th>
            <th><button type="button" @click="toggleSort('materialNo')">Material No{{ sortMark('materialNo') }}</button></th>
            <th class="numeric"><button type="button" @click="toggleSort('useQty')">Use Qty{{ sortMark('useQty') }}</button></th>
            <th><button type="button" @click="toggleSort('unitNo')">Unit No{{ sortMark('unitNo') }}</button></th>
            <th><button type="button" @click="toggleSort('materialLotNo')">MaterialLot No{{ sortMark('materialLotNo') }}</button></th>
            <th class="numeric"><button type="button" @click="toggleSort('lotQty')">Lot Qty{{ sortMark('lotQty') }}</button></th>
            <th><button type="button" @click="toggleSort('equipmentNo')">Equipment No{{ sortMark('equipmentNo') }}</button></th>
            <th><button type="button" @click="toggleSort('description')">Description{{ sortMark('description') }}</button></th>
            <th><button type="button" @click="toggleSort('eventTime')">EventTime{{ sortMark('eventTime') }}</button></th>
          </tr></thead>
          <tbody v-if="visibleRows.length"><tr v-for="row in visibleRows" :key="row.rowNo">
            <td>{{ row.lotNo }}</td><td>{{ row.opNo }}</td><td>{{ row.materialType }}</td><td>{{ row.materialNo }}</td>
            <td class="numeric">{{ formatNumber(row.useQty) }}</td><td>{{ row.unitNo }}</td><td>{{ row.materialLotNo }}</td>
            <td class="numeric">{{ formatNumber(row.lotQty) }}</td><td>{{ row.equipmentNo }}</td><td>{{ row.description }}</td><td>{{ formatTime(row.eventTime) }}</td>
          </tr></tbody>
          <tbody v-else><tr><td colspan="11" class="empty-state">{{ loading ? '載入中…' : hasSearched ? '查無符合資料。' : '設定條件後按 Query。' }}</td></tr></tbody>
        </table>
      </div>
      <footer class="table-footer">固定虛構資料，只在本機記憶體篩選；不呼叫 API、Oracle 或 PostgreSQL。</footer>
    </section>

    <div v-if="lookupKind" class="modal-backdrop" @click.self="closeLookup">
      <section class="lookup-modal" role="dialog" aria-modal="true" :aria-label="lookupKind === 'material' ? '選擇物料' : '選擇產品'">
        <header><div><span class="eyebrow">DEV LOOKUP</span><h2>{{ lookupKind === 'material' ? 'Material No' : 'Product No' }}</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="closeLookup">×</button></header>
        <form class="lookup-search" @submit.prevent="lookupKind === 'product' && validateProductLookup()">
          <input v-model="lookupSearch" type="search" :placeholder="lookupKind === 'material' ? '搜尋物料編號或名稱' : 'Product No 至少輸入 3 個固定字元'" @input="lookupKind === 'product' && validateProductLookup()">
          <button type="submit">搜尋</button>
        </form>
        <p v-if="lookupError" class="lookup-error" role="alert">{{ lookupError }}</p>
        <div class="lookup-list"><button v-for="item in lookupOptions" :key="item.code" type="button" @click="selectLookup(item)"><strong>{{ item.code }}</strong><span>{{ item.name }}</span></button><p v-if="!lookupOptions.length" class="lookup-empty">沒有符合的 DEV 選項。</p></div>
      </section>
    </div>
  </main>
</template>

<style scoped>
.material-query-page{box-sizing:border-box;display:flex;height:100%;min-height:0;flex-direction:column;gap:10px;overflow:hidden;padding:12px;background:#f3f2ed;color:#292820;font:inherit}.page-heading{display:flex;align-items:flex-start;justify-content:space-between;gap:12px}.eyebrow{color:#777366;font-size:9px;letter-spacing:.1em}.page-heading h1{margin:2px 0;font-size:18px}.page-heading p{margin:0;color:#777366;font-size:10px}.dev-badge{padding:4px 8px;border:1px solid #d6c995;border-radius:12px;background:#f4efdc;color:#705c17;font-size:10px;font-weight:700}.query-panel,.result-panel{border:1px solid #d4d1c5;background:#fffefa;box-shadow:0 1px 2px #26251f0a}.query-panel{display:grid;gap:8px;padding:10px}.search-mode{display:flex;align-items:center;gap:14px;margin:0;padding:0;border:0}.search-mode legend{float:left;margin-right:10px;color:#565347;font-size:10px;font-weight:700}.search-mode label,.date-check{display:flex;align-items:center;gap:5px;color:#565347;font-size:10px}.search-mode input,.date-check input{accent-color:#3c6a54}.search-input-row{display:flex;align-items:end;gap:7px}.search-field{display:grid;width:min(420px,52vw);gap:4px}.search-field span{color:#565347;font-size:10px;font-weight:700}.search-field input,.lookup-search input{box-sizing:border-box;width:100%;height:29px;padding:4px 8px;border:1px solid #cbc8bc;background:#fff;color:#292820;font:inherit}.picker-button{width:30px;height:29px;border:1px solid #cbc8bc;background:#eeeae0;font:inherit;cursor:pointer}.wildcard-note{align-self:center;margin-left:5px;color:#777366;font-size:10px}.date-row{display:flex;align-items:center;gap:7px;flex-wrap:wrap}.date-row>input[type=datetime-local]{box-sizing:border-box;width:190px;height:29px;padding:4px 6px;border:1px solid #cbc8bc;background:#fff;color:#292820;font:inherit}.date-row>input:disabled{background:#f1f0ec;color:#999585}.query-button,.close-button{min-height:29px;padding:4px 12px;border:1px solid #3c6a54;border-radius:3px;background:#3c6a54;color:#fff;font:inherit;font-weight:700;cursor:pointer}.close-button{border-color:#777366;background:#fffefa;color:#4e4a40}.query-button:disabled{opacity:.6;cursor:wait}.error-message,.source-note{margin:0;font-size:9px}.error-message{color:#b03939}.source-note{color:#8b6b18}.result-panel{display:flex;min-height:0;flex:1;flex-direction:column}.result-toolbar{display:flex;align-items:center;justify-content:space-between;padding:9px 10px;border-bottom:1px solid #e0ddd2}.result-toolbar strong{font-size:12px}.result-toolbar span{color:#777366;font-size:10px}.table-scroll{min-height:0;flex:1;overflow:auto}table{width:max-content;min-width:100%;border-collapse:collapse;white-space:nowrap;font-size:10px}th{position:sticky;top:0;z-index:1;padding:0;background:#eeece4;color:#514e43;text-align:left}th button{width:100%;height:30px;padding:6px;border:0;background:transparent;color:inherit;text-align:inherit;font:inherit;font-weight:700;cursor:pointer}th button:hover{background:#e5e2d8}td{max-width:260px;padding:7px 6px;border-top:1px solid #eeece6;color:#3b3932;overflow:hidden;text-overflow:ellipsis}.numeric{text-align:right;font-variant-numeric:tabular-nums}.empty-state{height:130px;text-align:center;color:#827d6d}.table-footer{padding:7px 9px;border-top:1px solid #e4e1d7;color:#777366;font-size:9px}.modal-backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:20px;background:#201f1bcc}.lookup-modal{display:flex;width:min(490px,100%);max-height:min(580px,90vh);flex-direction:column;border:1px solid #c9c5b8;background:#fffefa;box-shadow:0 16px 55px #0005}.lookup-modal>header{display:flex;align-items:center;justify-content:space-between;padding:12px;border-bottom:1px solid #e1ded4}.lookup-modal h2{margin:3px 0 0;font-size:15px}.icon-button{width:28px;height:28px;border:0;background:transparent;font-size:22px;cursor:pointer}.lookup-search{display:flex;gap:6px;margin:10px 12px}.lookup-search input{flex:1;min-width:0}.lookup-search button{min-width:68px;border:1px solid #cbc8bc;background:#f1efe8;font:inherit}.lookup-list{overflow:auto;min-height:120px;padding:0 12px 10px}.lookup-list>button{display:flex;width:100%;min-height:36px;align-items:center;gap:10px;padding:6px 8px;border:0;border-bottom:1px solid #efede6;background:#fffefa;color:#514e43;text-align:left;font:inherit;cursor:pointer}.lookup-list>button:hover{background:#f3f1e9}.lookup-list strong{min-width:120px;color:#343229}.lookup-list span,.lookup-empty{color:#777366}.lookup-empty{text-align:center;padding:25px}.lookup-error{margin:0 12px 8px;color:#b03939;font-size:10px}@media(max-width:760px){.material-query-page{overflow:auto}.search-input-row{flex-wrap:wrap}.search-field{width:100%}.wildcard-note{width:100%}.date-row>input[type=datetime-local]{width:min(190px,100%)}.result-panel{min-height:360px}.table-scroll{max-height:65vh}}
</style>
