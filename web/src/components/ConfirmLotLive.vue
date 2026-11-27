<script setup lang="ts">
import { computed, onBeforeUnmount, reactive, ref } from 'vue'

type FilterKey = 'roNo' | 'moNo' | 'productNo' | 'customerNo' | 'lotNo' | 'opNo' | 'orgLotNo' | 'psNo'
type LookupKind = 'customer' | 'product' | 'ps' | 'op'
type DetailKind = 'lot' | 'component'
interface ConfirmLotRow {
  productNo: string
  productName: string
  productVersion: string
  lotNo: string
  opNo: string
  brNo: string
  status: number
  curQty: number
  curUnitNo: string
}
interface ConfirmLotPage { page: number; pageSize: number; hasMore: boolean; items: ConfirmLotRow[] }
interface LookupOption { value: string; label: string; extra?: string; name?: string }
interface LookupRow { [key: string]: unknown }

const props = defineProps<{ apiBaseUrl: string }>()
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const filters = reactive<Record<FilterKey, string>>({ roNo: '', moNo: '', productNo: '', customerNo: '', lotNo: '', opNo: '', orgLotNo: '', psNo: '' })
const enabled = reactive<Record<FilterKey, boolean>>({ roNo: false, moNo: false, productNo: false, customerNo: false, lotNo: false, opNo: false, orgLotNo: false, psNo: false })
const productName = ref('')
const productVersion = ref('')
const lookupKind = ref<LookupKind | null>(null)
const lookupText = ref('')
const lookupOptions = ref<LookupOption[]>([])
const lookupBusy = ref(false)
const lookupError = ref('')
const detailKind = ref<DetailKind | null>(null)
const resultRows = ref<ConfirmLotRow[]>([])
const hasSearched = ref(false)
const busy = ref(false)
const selectedKey = ref('')
const errorMessage = ref('')
const page = ref(1)
const hasMore = ref(false)
let queryController: AbortController | null = null
let lookupController: AbortController | null = null
onBeforeUnmount(() => { queryController?.abort(); lookupController?.abort() })

const statusLabels: Record<number, string> = { 0: 'Queue', 1: 'Running', 2: 'Wait', 3: 'Hold', 4: 'Complete', 5: 'OS', 11: 'INV', 12: 'Scrap Inv' }
const visibleRows = computed(() => resultRows.value)
const selectedRow = computed(() => visibleRows.value.find(row => rowKey(row) === selectedKey.value) ?? null)
const activeCriteria = computed(() => (Object.keys(filters) as FilterKey[]).filter(key => enabled[key] && filters[key].trim()))
const criteriaSummary = computed(() => activeCriteria.value.map(key => `${fieldLabels[key]}=${filters[key]}`).join(' · ') || '尚未指定')
const fieldLabels: Record<FilterKey, string> = { roNo: 'RO No', moNo: 'MO No', productNo: 'Product No', customerNo: 'Customer No', lotNo: 'Lot No', opNo: 'OP No', orgLotNo: 'Org Lot No', psNo: 'PS No' }
const lookupTitle = computed(() => lookupKind.value === 'customer' ? '選擇 Customer No' : lookupKind.value === 'product' ? '選擇 Product No / Version' : lookupKind.value === 'ps' ? '選擇 PS No' : '選擇 OP No')
const detailTitle = computed(() => detailKind.value === 'lot' ? 'Lot Property' : 'Component Property')

function rowKey(row: ConfirmLotRow): string { return `${row.lotNo}|${row.opNo}|${row.status}|${row.brNo}` }
function setTextFilter(key: FilterKey): void { enabled[key] = Boolean(filters[key].trim()) }
function setEnabled(key: FilterKey, value: boolean): void {
  enabled[key] = value
  if (value) return
  filters[key] = ''
  if (key === 'productNo') { productName.value = ''; productVersion.value = '' }
}
function apiUrl(path: string, query: URLSearchParams): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  const url = new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin))
  query.forEach((value, key) => url.searchParams.set(key, value))
  return url.toString()
}
async function requestJson<T>(path: string, query: URLSearchParams, signal: AbortSignal): Promise<T> {
  const response = await fetch(apiUrl(path, query), { credentials: 'include', headers: { Accept: 'application/json' }, cache: 'no-store', signal })
  if (response.status === 401) { window.location.assign(appBase + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  const payload = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
  if (!response.ok) throw new Error(payload?.detail || payload?.message || payload?.title || `API 回應錯誤 (${response.status})。`)
  return payload as unknown as T
}
function queryParams(targetPage: number): URLSearchParams {
  const query = new URLSearchParams({ page: String(targetPage), pageSize: '100' })
  for (const key of activeCriteria.value) {
    const value = filters[key].trim()
    if (key === 'productNo') {
      query.set('productNo', value)
      if (productVersion.value.trim()) query.set('productVersion', productVersion.value.trim())
    } else query.set(key, value)
  }
  return query
}
async function runQuery(targetPage = 1): Promise<void> {
  if (busy.value) return
  errorMessage.value = ''
  if (!activeCriteria.value.length) {
    errorMessage.value = '請至少輸入或選擇一項查詢條件。'
    resultRows.value = []; selectedKey.value = ''; hasMore.value = false; page.value = 1; hasSearched.value = true; detailKind.value = null
    return
  }
  queryController?.abort()
  const controller = new AbortController()
  queryController = controller
  busy.value = true
  hasSearched.value = true
  resultRows.value = []
  selectedKey.value = ''
  detailKind.value = null
  try {
    const result = await requestJson<ConfirmLotPage>('api/reports/confirm-lots', queryParams(targetPage), controller.signal)
    if (queryController !== controller) return
    resultRows.value = Array.isArray(result.items) ? result.items : []
    page.value = result.page
    hasMore.value = result.hasMore
  } catch (cause) {
    if (controller.signal.aborted) return
    errorMessage.value = cause instanceof Error ? cause.message : '生產批資料查詢失敗。'
    resultRows.value = []
    hasMore.value = false
  } finally {
    if (queryController === controller) { queryController = null; busy.value = false }
  }
}
function rowsFrom(payload: unknown): LookupRow[] {
  if (Array.isArray(payload)) return payload.filter((item): item is LookupRow => typeof item === 'object' && item !== null)
  if (typeof payload === 'object' && payload !== null && 'items' in payload && Array.isArray(payload.items))
    return payload.items.filter((item): item is LookupRow => typeof item === 'object' && item !== null)
  return []
}
function textField(row: LookupRow, ...keys: string[]): string {
  for (const key of keys) if (typeof row[key] === 'string') return row[key] as string
  return ''
}
function mapLookup(kind: LookupKind, row: LookupRow): LookupOption | null {
  if (kind === 'customer') {
    const value = textField(row, 'customerNo'); const name = textField(row, 'customerName', 'customerSName')
    return value ? { value, name, label: [value, name].filter(Boolean).join(' · ') } : null
  }
  if (kind === 'product') {
    const value = textField(row, 'productNo'); const version = textField(row, 'productVersion'); const name = textField(row, 'productName')
    return value ? { value, extra: version, name, label: `${value} · ${name} · Ver ${version}` } : null
  }
  if (kind === 'ps') {
    const value = textField(row, 'psNo'); const name = textField(row, 'psName')
    return value ? { value, label: [value, name].filter(Boolean).join(' · ') } : null
  }
  const value = textField(row, 'operationNo', 'opNo'); const name = textField(row, 'operationName', 'opName')
  return value ? { value, label: [value, name].filter(Boolean).join(' · ') } : null
}
function openLookup(kind: LookupKind): void {
  lookupController?.abort()
  lookupKind.value = kind
  lookupText.value = ''
  lookupOptions.value = []
  lookupError.value = ''
}
function closeLookup(): void { lookupController?.abort(); lookupController = null; lookupKind.value = null; lookupBusy.value = false }
async function searchLookup(): Promise<void> {
  const kind = lookupKind.value
  const search = lookupText.value.trim()
  lookupError.value = ''
  lookupOptions.value = []
  if (!kind) return
  if (search.length < 3) { lookupError.value = '請輸入至少 3 個字元再搜尋。'; return }
  if (search.includes('%') || search.includes('_')) { lookupError.value = 'Lookup 搜尋不接受 % 或 _ 萬用字元。'; return }
  lookupController?.abort()
  const controller = new AbortController()
  lookupController = controller
  lookupBusy.value = true
  const query = new URLSearchParams()
  let path = ''
  if (kind === 'customer') { path = 'api/ent/customer-basis'; query.set('keyword', search) }
  else if (kind === 'product') { path = 'api/products'; query.set('keyword', search); query.set('issueState', '2') }
  else if (kind === 'ps') { path = 'api/production-segments'; query.set('psNo', search) }
  else { path = 'api/operations'; query.set('keyword', search); query.set('issueState', '2') }
  try {
    const payload = await requestJson<unknown>(path, query, controller.signal)
    if (lookupController !== controller) return
    lookupOptions.value = rowsFrom(payload).map(row => mapLookup(kind, row)).filter((option): option is LookupOption => option !== null).slice(0, 200)
    if (!lookupOptions.value.length) lookupError.value = '沒有符合的選項。'
  } catch (cause) {
    if (controller.signal.aborted) return
    lookupError.value = cause instanceof Error ? cause.message : '選項查詢失敗。'
  } finally {
    if (lookupController === controller) { lookupController = null; lookupBusy.value = false }
  }
}
function chooseLookup(option: LookupOption): void {
  switch (lookupKind.value) {
    case 'customer': filters.customerNo = option.value; enabled.customerNo = true; break
    case 'product': filters.productNo = option.value; productVersion.value = option.extra ?? ''; productName.value = option.name ?? ''; enabled.productNo = true; break
    case 'ps': filters.psNo = option.value; enabled.psNo = true; break
    case 'op': filters.opNo = option.value; enabled.opNo = true; break
  }
  closeLookup()
}
function showDetails(kind: DetailKind): void { if (selectedRow.value) detailKind.value = kind }
function quantity(value: number): string { return Number(value).toFixed(4).replace(/0+$/, '').replace(/\.$/, '') }
</script>

<template>
  <main class="confirm-lot" aria-label="生產批狀態查詢">
    <header class="page-heading"><div><div class="eyebrow">RPT / RT501</div><h1>生產批狀態查詢</h1><p>查詢已確認的生產批及目前作業站狀態。</p></div><span class="live-badge">API 唯讀</span></header>
    <section class="query-panel" aria-label="查詢條件">
      <div class="query-grid">
        <div class="criteria-field"><label class="criteria-check"><input type="checkbox" :checked="enabled.customerNo" @change="setEnabled('customerNo', ($event.target as HTMLInputElement).checked)"><span>Customer No</span></label><div class="picker-input"><input :value="filters.customerNo" readonly placeholder="選擇客戶"><button type="button" aria-label="選擇客戶" @click="openLookup('customer')">…</button></div></div>
        <div class="criteria-field"><label class="criteria-check"><input type="checkbox" :checked="enabled.roNo" @change="setEnabled('roNo', ($event.target as HTMLInputElement).checked)"><span>RO No</span></label><input v-model="filters.roNo" type="text" placeholder="輸入 RO No" @input="setTextFilter('roNo')" @blur="filters.roNo = filters.roNo.toUpperCase()"></div>
        <div class="criteria-field"><label class="criteria-check"><input type="checkbox" :checked="enabled.productNo" @change="setEnabled('productNo', ($event.target as HTMLInputElement).checked)"><span>Product No</span></label><div class="product-picker"><div class="picker-input"><input :value="filters.productNo" readonly placeholder="選擇產品"><button type="button" aria-label="選擇產品" @click="openLookup('product')">…</button></div><input :value="productName" readonly aria-label="Product Name" placeholder="Product Name"><input :value="productVersion" readonly aria-label="Product Version" placeholder="Ver"></div></div>
        <div class="criteria-field"><label class="criteria-check"><input type="checkbox" :checked="enabled.moNo" @change="setEnabled('moNo', ($event.target as HTMLInputElement).checked)"><span>MO No</span></label><input v-model="filters.moNo" type="text" placeholder="輸入 MO No" @input="setTextFilter('moNo')" @blur="filters.moNo = filters.moNo.toUpperCase()"></div>
        <div class="criteria-field"><label class="criteria-check"><input type="checkbox" :checked="enabled.psNo" @change="setEnabled('psNo', ($event.target as HTMLInputElement).checked)"><span>PS No</span></label><div class="picker-input"><input :value="filters.psNo" readonly placeholder="選擇 PS"><button type="button" aria-label="選擇 PS" @click="openLookup('ps')">…</button></div></div>
        <div class="criteria-field"><label class="criteria-check"><input type="checkbox" :checked="enabled.orgLotNo" @change="setEnabled('orgLotNo', ($event.target as HTMLInputElement).checked)"><span>Org Lot No</span></label><input v-model="filters.orgLotNo" type="text" placeholder="輸入原始批號" @input="setTextFilter('orgLotNo')" @blur="filters.orgLotNo = filters.orgLotNo.toUpperCase()"></div>
        <div class="criteria-field"><label class="criteria-check"><input type="checkbox" :checked="enabled.opNo" @change="setEnabled('opNo', ($event.target as HTMLInputElement).checked)"><span>OP No</span></label><div class="picker-input"><input :value="filters.opNo" readonly placeholder="選擇 OP"><button type="button" aria-label="選擇 OP" @click="openLookup('op')">…</button></div></div>
        <div class="criteria-field"><label class="criteria-check"><input type="checkbox" :checked="enabled.lotNo" @change="setEnabled('lotNo', ($event.target as HTMLInputElement).checked)"><span>Lot No</span></label><input v-model="filters.lotNo" type="text" placeholder="批號；可用 | 分隔" @input="setTextFilter('lotNo')" @blur="filters.lotNo = filters.lotNo.toUpperCase()"></div>
      </div>
      <button type="button" class="query-button" :disabled="busy" @click="runQuery(1)">{{ busy ? '查詢中…' : 'Query' }}</button>
    </section>
    <p v-if="errorMessage" class="message error" role="alert">{{ errorMessage }}</p>
    <section class="result-panel" aria-label="生產批查詢結果">
      <header class="result-heading"><div><h2>查詢結果</h2><p>{{ criteriaSummary }} · 固定 LotState=Confirm · 每頁最多 100 筆</p></div><span>{{ busy ? '載入中…' : `${resultRows.length} 筆` }}</span></header>
      <div class="grid-layout"><div class="table-wrap"><table><thead><tr><th>Product No</th><th>Product Name</th><th>Product Version</th><th>Lot No</th><th>OP No</th><th>BR No</th><th>Status</th><th class="qty-col">Cur Qty</th><th>Cur Unit</th></tr></thead>
        <tbody v-if="hasSearched && visibleRows.length"><tr v-for="row in visibleRows" :key="rowKey(row)" :class="{ selected: selectedKey === rowKey(row) }" @click="selectedKey = rowKey(row)"><td class="mono">{{ row.productNo }}</td><td>{{ row.productName }}</td><td>{{ row.productVersion }}</td><td class="mono">{{ row.lotNo }}</td><td>{{ row.opNo }}</td><td>{{ row.brNo }}</td><td><span class="status-pill" :class="`status-${row.status}`">{{ statusLabels[row.status] ?? `Status ${row.status}` }}</span></td><td class="qty">{{ quantity(row.curQty) }}</td><td>{{ row.curUnitNo }}</td></tr></tbody>
        <tbody v-else><tr><td colspan="9" class="empty-state">{{ errorMessage ? '查詢失敗' : hasSearched ? '沒有符合條件的確認批' : '請輸入至少一項條件後查詢' }}</td></tr></tbody>
      </table></div><aside class="row-actions" aria-label="所選生產批明細"><div class="selection-card"><small>目前選取</small><strong>{{ selectedRow?.lotNo ?? '未選取' }}</strong><span>{{ selectedRow ? `${selectedRow.productNo} · ${selectedRow.opNo}` : '請先選擇結果列' }}</span></div><button type="button" :disabled="!selectedRow" @click="showDetails('lot')">Lot Property</button><button type="button" :disabled="!selectedRow" @click="showDetails('component')">Comp Property</button></aside></div>
      <footer class="grid-footer"><span>{{ hasSearched ? `第 ${page} 頁 · ${resultRows.length} 筆` : '尚未執行查詢' }}</span><div class="pager"><button type="button" :disabled="busy || page <= 1" @click="runQuery(page - 1)">上一頁</button><button type="button" :disabled="busy || !hasMore" @click="runQuery(page + 1)">下一頁</button></div></footer>
    </section>
    <footer class="page-footer"><span>正式模式只讀取登入保護的 YMES API，結果不做本機假資料補值。</span><button type="button" class="close-button" @click="emit('close')">Close (X)</button></footer>

    <div v-if="lookupKind" class="modal-backdrop" @click.self="closeLookup"><section class="lookup-dialog" role="dialog" aria-modal="true" :aria-label="lookupTitle"><header><div><h2>{{ lookupTitle }}</h2><p>輸入至少 3 個字元；結果最多顯示 200 筆。</p></div><button type="button" aria-label="關閉" @click="closeLookup">×</button></header><form class="lookup-search" @submit.prevent="searchLookup"><input v-model="lookupText" type="search" maxlength="100" autofocus placeholder="輸入編號或名稱"><button type="submit" :disabled="lookupBusy">{{ lookupBusy ? '搜尋中…' : '搜尋' }}</button></form><p v-if="lookupError" class="message" role="status">{{ lookupError }}</p><ul v-else-if="lookupOptions.length"><li v-for="option in lookupOptions" :key="`${option.value}|${option.extra ?? ''}`"><button type="button" @click="chooseLookup(option)">{{ option.label }}</button></li></ul><p v-else class="no-options">請輸入搜尋條件。</p></section></div>
    <div v-if="detailKind && selectedRow" class="modal-backdrop" @click.self="detailKind = null"><section class="detail-dialog" role="dialog" aria-modal="true" :aria-label="detailTitle"><header><div><h2>{{ detailTitle }}</h2><p>{{ selectedRow.lotNo }}</p></div><button type="button" aria-label="關閉" @click="detailKind = null">×</button></header><dl><dt>Lot No</dt><dd>{{ selectedRow.lotNo }}</dd><dt>Product</dt><dd>{{ selectedRow.productNo }} · {{ selectedRow.productName }} · Ver {{ selectedRow.productVersion }}</dd><dt>Operation</dt><dd>{{ selectedRow.opNo }} · {{ selectedRow.brNo }}</dd><dt>Current Qty</dt><dd>{{ quantity(selectedRow.curQty) }} {{ selectedRow.curUnitNo }}</dd></dl><p class="detail-note">此 API 只提供查詢結果欄位；Lot／Component Property 的獨立正式資料來源尚未接入。</p><button type="button" class="query-button" @click="detailKind = null">Close</button></section></div>
  </main>
</template>

<style scoped>
.confirm-lot{box-sizing:border-box;display:flex;flex-direction:column;gap:11px;min-height:0;height:100%;padding:12px;background:#f4f3ee;color:#292820;font:inherit;overflow:auto}.page-heading{display:flex;justify-content:space-between;gap:14px}.eyebrow{margin-bottom:3px;color:#777366;font-size:10px;letter-spacing:.1em}.page-heading h1{margin:0;font-size:19px;font-weight:650}.page-heading p{margin:4px 0 0;color:#777366;font-size:11px}.live-badge{height:max-content;padding:5px 9px;border:1px solid #b9d0bf;border-radius:3px;background:#e5f0e7;color:#285c43;font-size:11px;font-weight:700}
.query-panel{display:flex;align-items:flex-end;gap:10px;padding:10px;border:1px solid #cbc9bd;background:#fbfaf6}.query-grid{display:grid;flex:1;grid-template-columns:repeat(4,minmax(175px,1fr));gap:8px 12px}.criteria-field{display:grid;grid-template-columns:82px minmax(0,1fr);align-items:center;gap:5px;font-size:11px}.criteria-check{display:flex;align-items:center;gap:4px;white-space:nowrap;font-size:10px}.criteria-check input{margin:0}.criteria-field>input[type=text],.picker-input input,.product-picker>input{box-sizing:border-box;min-width:0;width:100%;height:27px;padding:4px 6px;border:1px solid #aaa79b;border-radius:2px;background:#fff;color:#292820;font:inherit}.criteria-field input[readonly]{background:#f0efe9}.picker-input{display:flex;min-width:0}.picker-input input{border-radius:2px 0 0 2px}.picker-input button{width:28px;flex:none;border:1px solid #aaa79b;border-left:0;border-radius:0 2px 2px 0;background:#eeede7;cursor:pointer}.product-picker{display:grid;grid-template-columns:minmax(95px,1.2fr) minmax(75px,1fr) 38px;gap:4px}.query-button,.close-button{min-height:29px;padding:4px 12px;border:1px solid #3b3930;border-radius:3px;background:#3b3930;color:white;font:inherit;cursor:pointer;white-space:nowrap}.query-button:disabled,.close-button:disabled{opacity:.6;cursor:wait}
.result-panel{display:flex;flex:1;flex-direction:column;min-height:190px;border:1px solid #cbc9bd;background:#fbfaf6}.result-heading{display:flex;justify-content:space-between;align-items:center;gap:12px;padding:9px 11px;border-bottom:1px solid #d9d7cc}.result-heading h2{margin:0;font-size:13px}.result-heading p{margin:4px 0 0;color:#777366;font-size:10px}.result-heading>span{flex:none;color:#555247;font-size:11px}.grid-layout{display:flex;flex:1;min-height:140px}.table-wrap{flex:1;min-width:0;overflow:auto}table{width:100%;border-collapse:collapse;font-size:10px;white-space:nowrap}th{position:sticky;top:0;z-index:1;padding:7px;border-right:1px solid #d5d3c7;border-bottom:1px solid #c5c2b5;background:#eeece3;text-align:left;font-weight:650}td{padding:6px 7px;border-right:1px solid #e3e1d8;border-bottom:1px solid #e3e1d8}tbody tr{cursor:pointer}tbody tr:hover{background:#f1f2ea}tbody tr.selected{background:#e4eadf}.qty-col{width:75px}.qty{text-align:right;font-variant-numeric:tabular-nums}.mono{font-family:ui-monospace,SFMono-Regular,Consolas,monospace;font-size:9px}.status-pill{display:inline-block;padding:2px 5px;border-radius:2px;background:#e9e7dd;color:#555247;font-size:9px}.status-1{background:#dcebe4;color:#285c43}.status-3{background:#f4e2d8;color:#844933}.status-11{background:#dfe8f4;color:#385879}.status-12{background:#f3e8cd;color:#765b1d}.empty-state{height:90px;text-align:center;color:#858173;cursor:default}.row-actions{display:flex;flex:none;flex-direction:column;gap:6px;width:130px;padding:8px;border-left:1px solid #e0ded4}.row-actions>button,.pager button{min-height:28px;border:1px solid #aaa79b;border-radius:2px;background:#f6f5ef;font:inherit;cursor:pointer}.row-actions>button:disabled,.pager button:disabled{opacity:.45;cursor:not-allowed}.selection-card{display:flex;flex-direction:column;gap:4px;min-height:52px;padding:7px;border:1px solid #e0ded4;background:#f6f5ef}.selection-card small{color:#858173;font-size:9px}.selection-card strong{overflow:hidden;text-overflow:ellipsis;font-size:10px}.selection-card span{color:#777366;font-size:9px}.grid-footer{display:flex;justify-content:space-between;gap:8px;padding:5px 9px;border-top:1px solid #d9d7cc;color:#777366;font-size:9px}.pager{display:flex;gap:6px}.page-footer{display:flex;justify-content:space-between;align-items:center;color:#777366;font-size:10px}.message{margin:0;padding:7px 9px;border-left:3px solid #c69b43;background:#fbf5e5;color:#6e5929;font-size:11px}.message.error{border-color:#b95b48;background:#faece8;color:#8a3426}
.modal-backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:18px;background:#1d1d1980}.lookup-dialog,.detail-dialog{box-sizing:border-box;width:min(460px,100%);max-height:85vh;display:flex;flex-direction:column;gap:10px;padding:14px;border:1px solid #aaa797;border-radius:4px;background:#fbfaf6;box-shadow:0 14px 40px #0003}.lookup-dialog header,.detail-dialog header{display:flex;justify-content:space-between;align-items:flex-start}.lookup-dialog h2,.detail-dialog h2{margin:0;font-size:15px}.lookup-dialog p,.detail-dialog header p{margin:4px 0 0;color:#777366;font-size:10px}.lookup-dialog header button,.detail-dialog header button{width:28px;height:28px;border:1px solid #c8c5b9;background:#f2f1ec;font-size:19px;cursor:pointer}.lookup-search{display:flex;gap:6px}.lookup-search input{flex:1;min-width:0;height:29px;padding:4px 7px;border:1px solid #aaa797;font:inherit}.lookup-search button{min-width:60px;border:1px solid #aaa797;background:#f2f1ec;font:inherit;cursor:pointer}.lookup-search button:disabled{opacity:.6}.lookup-dialog ul{margin:0;padding:0;overflow:auto;list-style:none}.lookup-dialog li+li{border-top:1px solid #e2e0d7}.lookup-dialog li button{width:100%;padding:8px;text-align:left;border:0;background:transparent;font:inherit;cursor:pointer}.lookup-dialog li button:hover{background:#eeeee5}.no-options{text-align:center;padding:18px}.detail-dialog dl{display:grid;grid-template-columns:100px 1fr;gap:7px;margin:0}.detail-dialog dt{color:#777366;font-size:11px}.detail-dialog dd{margin:0;font-size:11px}.detail-note{margin:4px 0;color:#777366;font-size:10px}.detail-dialog>.query-button{align-self:flex-end}
@media(max-width:1080px){.query-grid{grid-template-columns:repeat(2,minmax(170px,1fr))}.query-panel{align-items:flex-start;flex-direction:column}.query-button{align-self:flex-end}}@media(max-width:720px){.query-grid{grid-template-columns:1fr}.grid-layout{flex-direction:column}.row-actions{width:auto;flex-direction:row;border-left:0;border-top:1px solid #e0ded4}.selection-card{flex:1}.row-actions>button{min-width:100px}.grid-footer,.page-footer{align-items:flex-start;flex-direction:column}.page-heading h1{font-size:16px}}
</style>
