<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type Page<T> = { items: T[]; totalCount: number; page: number; pageSize: number }
type Inventory = { inventoryNo: string; inventoryName: string | null }
type StockRow = {
  inventoryNo: string; productNo: string; locatorNo: string; lotNo: string; locQty: number; unitNo: string | null
  belongToType: number; belongToNo: string | null; inputDate: string | null; fgdInNo: string | null
}
type InventoryDetails = {
  components: Array<{ componentNo: string; goodQty: number | null; scrapQty: number | null; unitNo: string | null }>
  binDefinitions: Array<{ binNo: string; binOrder: number }>
  goodBinState: { lotNo: string; lotSerial: string | null; bins: Array<{ sourceColumn: string; quantity: number | null }> } | null
}
type FgdIn = {
  fgdInNo: string; state: number; description: string | null; creator: string | null; createDate: string
  inventoryNo: string; inputDate: string | null; source: number; fromInventoryNo: string | null; toErp: number | null; dataStamp: number | null
}
type FgdInDetails = {
  basis: FgdIn
  details: Array<{ productNo: string; productVersion: string; lotNo: string; qty: number; unitNo: string; locatorNo: string; baseLotNo: string; inputDate: string | null; fromInventoryNo: string | null }>
  components: Array<{ lotNo: string; componentNo: string; goodQty: number | null; scrapQty: number | null; unitNo: string | null }>
}

const props = defineProps<{ apiBaseUrl: string }>()
const emit = defineEmits<{ close: [] }>()
const tab = ref<'stock' | 'confirmations'>('stock')
const inventoryOptions = ref<Page<Inventory> | null>(null)
const inventoryLookupPage = ref(1)
const inventoryNo = ref('')
const appliedInventoryNo = ref('')
const stockPage = ref<Page<StockRow> | null>(null)
const stockPageNumber = ref(1)
const selectedStock = ref<StockRow | null>(null)
const inventoryDetails = ref<InventoryDetails | null>(null)
const confirmations = ref<Page<FgdIn> | null>(null)
const confirmationPageNumber = ref(1)
const selectedFgdInNo = ref('')
const confirmationDetails = ref<FgdInDetails | null>(null)
const loading = ref(false)
const error = ref('')
const notice = ref('選擇成品庫房後查詢；資料為正式資料庫唯讀結果。')
const inventoryPageCount = computed(() => Math.max(1, Math.ceil((inventoryOptions.value?.totalCount ?? 0) / 100)))
const stockPageCount = computed(() => Math.max(1, Math.ceil((stockPage.value?.totalCount ?? 0) / 100)))
const confirmationPageCount = computed(() => Math.max(1, Math.ceil((confirmations.value?.totalCount ?? 0) / 100)))

function apiUrl(path: string, query?: Record<string, string | number>): URL {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  const root = new URL(base, window.location.origin)
  if (root.protocol !== 'https:' && root.hostname !== 'localhost' && root.hostname !== '127.0.0.1')
    throw new Error('API 連線必須使用 HTTPS。')
  root.pathname = `${root.pathname.replace(/\/+$/, '')}/api/inventory/fgd-inventory/${path.replace(/^\/+/, '')}`.replace(/\/$/, '')
  root.search = ''
  if (query) for (const [key, value] of Object.entries(query)) root.searchParams.set(key, String(value))
  return root
}

async function request<T>(path: string, query?: Record<string, string | number>): Promise<T> {
  const response = await fetch(apiUrl(path, query), { credentials: 'include', headers: { Accept: 'application/json' }, cache: 'no-store' })
  const body = await response.json().catch(() => ({})) as { detail?: string; title?: string; message?: string }
  if (!response.ok) throw new Error(body.detail || body.message || body.title || `讀取失敗（HTTP ${response.status}）。`)
  return body as T
}

async function loadInventoryOptions(page = 1): Promise<void> {
  loading.value = true; error.value = ''
  try {
    inventoryOptions.value = await request<Page<Inventory>>('lookups/inventories', { page, pageSize: 100 })
    inventoryLookupPage.value = page
    if (!inventoryNo.value && inventoryOptions.value.items.length) inventoryNo.value = inventoryOptions.value.items[0].inventoryNo
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '讀取成品庫房失敗。' }
  finally { loading.value = false }
}

async function queryStock(page = 1): Promise<void> {
  const selected = inventoryNo.value.trim()
  if (!selected) { error.value = '請先選擇成品庫房。'; return }
  loading.value = true; error.value = ''; selectedStock.value = null; inventoryDetails.value = null
  try {
    appliedInventoryNo.value = selected
    stockPageNumber.value = page
    stockPage.value = await request<Page<StockRow>>('', { inventoryNo: selected, page, pageSize: 100 })
    notice.value = `查詢完成：${stockPage.value.totalCount.toLocaleString()} 筆正數庫存。`
  } catch (cause) { stockPage.value = null; error.value = cause instanceof Error ? cause.message : '查詢成品庫存失敗。' }
  finally { loading.value = false }
}

async function selectStock(row: StockRow): Promise<void> {
  selectedStock.value = row; inventoryDetails.value = null; error.value = ''; loading.value = true
  try {
    inventoryDetails.value = await request<InventoryDetails>(`${encodeURIComponent(row.inventoryNo)}/${encodeURIComponent(row.lotNo)}/${encodeURIComponent(row.locatorNo)}/details`)
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '讀取庫存明細失敗。' }
  finally { loading.value = false }
}

async function loadConfirmations(page = 1): Promise<void> {
  loading.value = true; error.value = ''; selectedFgdInNo.value = ''; confirmationDetails.value = null
  try {
    confirmationPageNumber.value = page
    confirmations.value = await request<Page<FgdIn>>('confirmations', { page, pageSize: 100 })
    notice.value = `待確認入庫單：${confirmations.value.totalCount.toLocaleString()} 筆。`
  } catch (cause) { confirmations.value = null; error.value = cause instanceof Error ? cause.message : '讀取待確認入庫單失敗。' }
  finally { loading.value = false }
}

async function selectConfirmation(row: FgdIn): Promise<void> {
  selectedFgdInNo.value = row.fgdInNo; confirmationDetails.value = null; error.value = ''; loading.value = true
  try { confirmationDetails.value = await request<FgdInDetails>(`confirmations/${encodeURIComponent(row.fgdInNo)}`) }
  catch (cause) { error.value = cause instanceof Error ? cause.message : '讀取入庫單明細失敗。' }
  finally { loading.value = false }
}

function switchTab(next: 'stock' | 'confirmations'): void {
  tab.value = next
  if (next === 'confirmations' && !confirmations.value) void loadConfirmations(1)
}
function fmt(value: string | null | undefined): string { return value ? value.replace('T', ' ').replace(/\.\d+$/, '') : '—' }
function fmtQty(value: number | null | undefined): string { return value == null ? '—' : value.toLocaleString(undefined, { maximumFractionDigits: 4 }) }
function itemRange(page: Page<unknown> | null): string {
  if (!page || page.totalCount === 0) return '0 筆'
  return `${((page.page - 1) * page.pageSize + 1).toLocaleString()}–${Math.min(page.page * page.pageSize, page.totalCount).toLocaleString()} / ${page.totalCount.toLocaleString()} 筆`
}

onMounted(() => { void loadInventoryOptions(1) })
</script>

<template>
  <section class="iv501-api" aria-label="成品庫庫房資料">
    <header class="iv501-api__header">
      <div><p class="iv501-api__eyebrow">IV501 · FGD INVENTORY</p><h1>成品庫庫房資料</h1></div>
      <button type="button" class="iv501-api__button" @click="emit('close')">關閉</button>
    </header>
    <nav class="iv501-api__tabs" aria-label="成品庫功能">
      <button type="button" :class="{ active: tab === 'stock' }" @click="switchTab('stock')">成品庫存</button>
      <button type="button" :class="{ active: tab === 'confirmations' }" @click="switchTab('confirmations')">入庫待確認</button>
    </nav>
    <p class="iv501-api__notice" role="status">{{ error || notice }}</p>

    <template v-if="tab === 'stock'">
      <section class="iv501-api__panel">
        <div class="iv501-api__query">
          <label for="iv501-inventory">成品庫房</label>
          <select id="iv501-inventory" v-model="inventoryNo" :disabled="loading || !inventoryOptions?.items.length">
            <option v-for="option in inventoryOptions?.items ?? []" :key="option.inventoryNo" :value="option.inventoryNo">{{ option.inventoryNo }}{{ option.inventoryName ? ` · ${option.inventoryName}` : '' }}</option>
          </select>
          <button type="button" class="iv501-api__button iv501-api__button--primary" :disabled="loading" @click="queryStock(1)">{{ loading ? '讀取中…' : '查詢' }}</button>
        </div>
        <div v-if="inventoryOptions && inventoryPageCount > 1" class="iv501-api__lookup-pages">
          <span>庫房 {{ inventoryLookupPage }} / {{ inventoryPageCount }}</span>
          <button type="button" :disabled="loading || inventoryLookupPage <= 1" @click="loadInventoryOptions(inventoryLookupPage - 1)">上一頁</button>
          <button type="button" :disabled="loading || inventoryLookupPage >= inventoryPageCount" @click="loadInventoryOptions(inventoryLookupPage + 1)">下一頁</button>
        </div>
      </section>
      <section class="iv501-api__panel">
        <header class="iv501-api__section-head"><h2>庫存</h2><span>{{ itemRange(stockPage) }}</span></header>
        <div class="iv501-api__table-wrap"><table><thead><tr><th>產品</th><th>儲位</th><th>批號</th><th>數量</th><th>單位</th><th>來源</th><th>來源單號</th><th>入庫日期</th><th>入庫單號</th></tr></thead>
          <tbody><tr v-for="row in stockPage?.items ?? []" :key="`${row.inventoryNo}|${row.lotNo}|${row.locatorNo}`" :class="{ selected: selectedStock?.lotNo === row.lotNo && selectedStock?.locatorNo === row.locatorNo }" @click="selectStock(row)">
            <td>{{ row.productNo }}</td><td>{{ row.locatorNo }}</td><td>{{ row.lotNo }}</td><td class="numeric">{{ fmtQty(row.locQty) }}</td><td>{{ row.unitNo || '—' }}</td><td>{{ row.belongToType === 0 ? 'MO' : '—' }}</td><td>{{ row.belongToNo || '—' }}</td><td>{{ fmt(row.inputDate) }}</td><td>{{ row.fgdInNo || '—' }}</td>
          </tr><tr v-if="stockPage && !stockPage.items.length"><td colspan="9" class="empty">查無正數庫存。</td></tr><tr v-if="!stockPage"><td colspan="9" class="empty">選擇庫房後查詢。</td></tr></tbody>
        </table></div>
        <footer v-if="stockPage && stockPageCount > 1" class="iv501-api__pager"><span>第 {{ stockPageNumber }} / {{ stockPageCount }} 頁</span><button type="button" :disabled="loading || stockPageNumber <= 1" @click="queryStock(stockPageNumber - 1)">上一頁</button><button type="button" :disabled="loading || stockPageNumber >= stockPageCount" @click="queryStock(stockPageNumber + 1)">下一頁</button></footer>
      </section>
      <section v-if="selectedStock" class="iv501-api__panel">
        <header class="iv501-api__section-head"><h2>批號明細</h2><span>{{ selectedStock.lotNo }} · {{ selectedStock.locatorNo }}</span></header>
        <template v-if="inventoryDetails?.components.length">
          <div class="iv501-api__table-wrap"><table><thead><tr><th>Component</th><th>Good Qty</th><th>Scrap Qty</th><th>Unit</th></tr></thead><tbody>
            <tr v-for="row in inventoryDetails.components" :key="row.componentNo"><td>{{ row.componentNo }}</td><td class="numeric">{{ fmtQty(row.goodQty) }}</td><td class="numeric">{{ fmtQty(row.scrapQty) }}</td><td>{{ row.unitNo || '—' }}</td></tr>
          </tbody></table></div>
        </template>
        <template v-else-if="inventoryDetails?.goodBinState">
          <div class="iv501-api__table-wrap"><table><thead><tr><th v-for="bin in inventoryDetails.goodBinState.bins" :key="bin.sourceColumn">{{ bin.sourceColumn }}</th></tr></thead><tbody><tr><td v-for="bin in inventoryDetails.goodBinState.bins" :key="bin.sourceColumn" class="numeric">{{ fmtQty(bin.quantity) }}</td></tr></tbody></table></div>
        </template>
        <p v-else-if="inventoryDetails" class="empty">此批號沒有 Component 或 Good Bin 明細。</p>
      </section>
    </template>

    <template v-else>
      <section class="iv501-api__panel">
        <header class="iv501-api__section-head"><h2>待確認入庫單</h2><span>{{ itemRange(confirmations) }}</span></header>
        <div class="iv501-api__table-wrap"><table><thead><tr><th>FGD In No.</th><th>庫房</th><th>描述</th><th>建立人員</th><th>建立日期</th><th>Source</th></tr></thead>
          <tbody><tr v-for="row in confirmations?.items ?? []" :key="row.fgdInNo" :class="{ selected: selectedFgdInNo === row.fgdInNo }" @click="selectConfirmation(row)"><td>{{ row.fgdInNo }}</td><td>{{ row.inventoryNo }}</td><td>{{ row.description || '—' }}</td><td>{{ row.creator || '—' }}</td><td>{{ fmt(row.createDate) }}</td><td>{{ row.source }}</td></tr>
            <tr v-if="confirmations && !confirmations.items.length"><td colspan="6" class="empty">目前沒有待確認單據。</td></tr><tr v-if="!confirmations"><td colspan="6" class="empty">尚未載入待確認單據。</td></tr></tbody>
        </table></div>
        <footer v-if="confirmations && confirmationPageCount > 1" class="iv501-api__pager"><span>第 {{ confirmationPageNumber }} / {{ confirmationPageCount }} 頁</span><button type="button" :disabled="loading || confirmationPageNumber <= 1" @click="loadConfirmations(confirmationPageNumber - 1)">上一頁</button><button type="button" :disabled="loading || confirmationPageNumber >= confirmationPageCount" @click="loadConfirmations(confirmationPageNumber + 1)">下一頁</button></footer>
      </section>
      <section v-if="confirmationDetails" class="iv501-api__panel">
        <header class="iv501-api__section-head"><h2>入庫單明細</h2><span>{{ confirmationDetails.basis.fgdInNo }} · {{ confirmationDetails.basis.inventoryNo }}</span></header>
        <div class="iv501-api__table-wrap"><table><thead><tr><th>產品</th><th>版次</th><th>批號</th><th>數量</th><th>單位</th><th>儲位</th><th>Base Lot</th><th>來源庫房</th></tr></thead><tbody>
          <tr v-for="row in confirmationDetails.details" :key="`${row.lotNo}|${row.locatorNo}`"><td>{{ row.productNo }}</td><td>{{ row.productVersion }}</td><td>{{ row.lotNo }}</td><td class="numeric">{{ fmtQty(row.qty) }}</td><td>{{ row.unitNo }}</td><td>{{ row.locatorNo }}</td><td>{{ row.baseLotNo }}</td><td>{{ row.fromInventoryNo || '—' }}</td></tr>
          <tr v-if="!confirmationDetails.details.length"><td colspan="8" class="empty">此單沒有明細。</td></tr>
        </tbody></table></div>
        <div v-if="confirmationDetails.components.length" class="iv501-api__table-wrap iv501-api__component-table"><table><thead><tr><th>Lot</th><th>Component</th><th>Good Qty</th><th>Scrap Qty</th><th>Unit</th></tr></thead><tbody>
          <tr v-for="row in confirmationDetails.components" :key="`${row.lotNo}|${row.componentNo}`"><td>{{ row.lotNo }}</td><td>{{ row.componentNo }}</td><td class="numeric">{{ fmtQty(row.goodQty) }}</td><td class="numeric">{{ fmtQty(row.scrapQty) }}</td><td>{{ row.unitNo || '—' }}</td></tr>
        </tbody></table></div>
      </section>
      <p class="iv501-api__readonly-note">此版本僅提供查詢。確認過帳功能尚未開放。</p>
    </template>
  </section>
</template>

<style scoped>
.iv501-api { display: grid; gap: 10px; min-height: 0; padding: 12px; color: #302f29; background: #f7f5ee; font: inherit; }
.iv501-api__header,.iv501-api__section-head,.iv501-api__pager,.iv501-api__query,.iv501-api__lookup-pages { display: flex; align-items: center; gap: 10px; }
.iv501-api__header,.iv501-api__section-head,.iv501-api__pager { justify-content: space-between; }
.iv501-api__header h1 { margin: 0; font-size: 22px; }
.iv501-api__eyebrow { margin: 0 0 3px; color: #8c812d; font-size: 11px; font-weight: 700; letter-spacing: .08em; }
.iv501-api__tabs { display: flex; gap: 4px; border-bottom: 1px solid #ddd9cc; }
.iv501-api__tabs button { border: 0; border-bottom: 2px solid transparent; padding: 8px 12px; background: transparent; color: #68665d; cursor: pointer; font: inherit; }
.iv501-api__tabs button.active { border-color: #a89524; color: #4b461f; font-weight: 700; }
.iv501-api__notice { min-height: 20px; margin: 0; color: #69675e; font-size: 13px; }
.iv501-api__panel { min-width: 0; padding: 12px; border: 1px solid #e4e0d5; border-radius: 8px; background: #fff; }
.iv501-api__query { flex-wrap: wrap; }
.iv501-api__query label { font-weight: 700; }
.iv501-api__query select { min-width: min(360px, 100%); min-height: 36px; padding: 6px 9px; border: 1px solid #d8d4c8; border-radius: 5px; background: #fff; color: inherit; font: inherit; }
.iv501-api__button,.iv501-api__pager button,.iv501-api__lookup-pages button { min-height: 32px; padding: 5px 12px; border: 1px solid #d8d4c8; border-radius: 5px; background: #fff; color: inherit; cursor: pointer; font: inherit; }
.iv501-api__button--primary { border-color: #a89524; background: #3b3930; color: #fff; }
.iv501-api button:disabled { opacity: .48; cursor: not-allowed; }
.iv501-api__lookup-pages { margin: 8px 0 0; color: #706e64; font-size: 12px; }
.iv501-api__section-head { margin-bottom: 8px; color: #77746a; font-size: 12px; }
.iv501-api__section-head h2 { margin: 0; color: #34332d; font-size: 16px; }
.iv501-api__table-wrap { max-width: 100%; overflow: auto; border: 1px solid #ebe8e0; border-radius: 5px; }
.iv501-api table { width: 100%; border-collapse: collapse; white-space: nowrap; font-size: 12px; }
.iv501-api th,.iv501-api td { padding: 8px 9px; border-bottom: 1px solid #eeeae1; text-align: left; }
.iv501-api th { position: sticky; top: 0; background: #f7f5ee; color: #57554b; font-weight: 700; }
.iv501-api tbody tr { cursor: pointer; }
.iv501-api tbody tr:hover { background: #faf8ee; }
.iv501-api tbody tr.selected { background: #fff8d9; }
.iv501-api td.numeric { text-align: right; font-variant-numeric: tabular-nums; }
.iv501-api .empty { padding: 18px; color: #77746a; text-align: center; cursor: default; }
.iv501-api__pager { justify-content: flex-end; margin-top: 8px; color: #77746a; font-size: 12px; }
.iv501-api__component-table { margin-top: 10px; }
.iv501-api__readonly-note { margin: 0; color: #77746a; font-size: 12px; }
@media (max-width: 700px) { .iv501-api { padding: 8px; } .iv501-api__panel { padding: 8px; } .iv501-api__header h1 { font-size: 19px; } }
</style>
