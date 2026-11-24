<script setup lang="ts">
import { computed, ref } from 'vue'

defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()

type ReturnState = 0 | 1 | 2 | 3
interface BasisRow { materialReturnNo: string; inventoryNo: string; vendorNo: string; state: ReturnState; returnDate: string; description: string; creator: string; createDate: string }
interface DetailRow { key: string; materialReturnNo: string; materialNo: string; materialLotNo: string; locatorNo: string; unitNo: string; qty: number; creator: string; createDate: string; componentIds: string[] }
interface ReturnComponent { componentId: string; componentNo: string; cassetteNo: string; slotNo: string }
interface Criteria { materialReturnNo: string; inventoryNo: string; vendorNo: string; state: string; fromCreateDate: string; endCreateDate: string; fromReturnDate: string; endReturnDate: string; useMaterialReturnNo: boolean; useInventoryNo: boolean; useVendorNo: boolean; useState: boolean; useFromCreateDate: boolean; useEndCreateDate: boolean; useFromReturnDate: boolean; useEndReturnDate: boolean }

const states: Record<ReturnState, string> = { 0: 'UnConfirm', 1: 'Handling', 2: 'Confirm', 3: 'Failed' }
const inventories = [{ inventoryNo: 'RAW-A', name: '原料庫 A' }, { inventoryNo: 'RAW-B', name: '原料庫 B' }]
const vendors = [{ vendorNo: 'V-DEV-01', name: 'DEV 供應商甲' }, { vendorNo: 'V-DEV-02', name: 'DEV 供應商乙' }]
const bases: BasisRow[] = [
  { materialReturnNo: 'MR-DEV-26001', inventoryNo: 'RAW-A', vendorNo: 'V-DEV-01', state: 2, returnDate: '2026-11-20 10:48:00', description: '已完成退料確認，包含元件批號。', creator: 'DEV001', createDate: '2026-11-20 10:12:00' },
  { materialReturnNo: 'MR-DEV-26002', inventoryNo: 'RAW-B', vendorNo: 'V-DEV-02', state: 0, returnDate: '', description: '待確認退料草稿，尚未扣帳。', creator: 'DEV002', createDate: '2026-11-22 13:40:00' },
  { materialReturnNo: 'MR-DEV-26003', inventoryNo: 'RAW-A', vendorNo: 'V-DEV-01', state: 1, returnDate: '', description: '確認處理中假資料。', creator: 'DEV003', createDate: '2026-11-24 08:25:00' },
  { materialReturnNo: 'MR-DEV-25998', inventoryNo: 'RAW-B', vendorNo: 'V-DEV-02', state: 3, returnDate: '2026-11-18 16:30:00', description: '確認失敗假資料。', creator: 'DEV003', createDate: '2026-11-18 15:54:00' },
]
const details: DetailRow[] = [
  { key: 'mr-26001-sensor', materialReturnNo: 'MR-DEV-26001', materialNo: 'RM-SENSOR-05', materialLotNo: 'SNS-2611-005', locatorNo: 'A-03-01', unitNo: 'EA', qty: 2, creator: 'DEV001', createDate: '2026-11-20 10:20:00', componentIds: ['ret-comp-01', 'ret-comp-02'] },
  { key: 'mr-26001-resin', materialReturnNo: 'MR-DEV-26001', materialNo: 'RM-RESIN-01', materialLotNo: 'RES-2611-001', locatorNo: 'A-01-01', unitNo: 'KG', qty: 1.25, creator: 'DEV001', createDate: '2026-11-20 10:25:00', componentIds: [] },
  { key: 'mr-26002-copper', materialReturnNo: 'MR-DEV-26002', materialNo: 'RM-COPPER-03', materialLotNo: 'CU-2611-020', locatorNo: 'B-01-02', unitNo: 'RL', qty: 3, creator: 'DEV002', createDate: '2026-11-22 13:45:00', componentIds: [] },
]
const components: ReturnComponent[] = [
  { componentId: 'ret-comp-01', componentNo: 'COMP-SNS-A', cassetteNo: 'CAS-01', slotNo: '01' },
  { componentId: 'ret-comp-02', componentNo: 'COMP-SNS-B', cassetteNo: 'CAS-01', slotNo: '02' },
]
function localDateOffset(offset: number): string {
  const date = new Date()
  date.setDate(date.getDate() + offset)
  return date.toLocaleDateString('sv-SE')
}
function initialCriteria(): Criteria {
  return { materialReturnNo: '', inventoryNo: '', vendorNo: '', state: '', fromCreateDate: localDateOffset(-14), endCreateDate: localDateOffset(0), fromReturnDate: localDateOffset(-14), endReturnDate: localDateOffset(0), useMaterialReturnNo: false, useInventoryNo: false, useVendorNo: false, useState: false, useFromCreateDate: false, useEndCreateDate: false, useFromReturnDate: false, useEndReturnDate: false }
}
function inventoryName(no: string): string { return inventories.find((row) => row.inventoryNo === no)?.name ?? no }
function vendorName(no: string): string { return vendors.find((row) => row.vendorNo === no)?.name ?? no }
function stateName(state: ReturnState): string { return states[state] }
function qtyText(qty: number): string { return new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 8 }).format(qty) }
function dateMatches(value: string, from: string, end: string): boolean {
  if (!value) return false
  const day = value.slice(0, 10)
  return (!from || day >= from) && (!end || day <= end)
}

const criteria = ref<Criteria>(initialCriteria())
const appliedCriteria = ref<Criteria | null>(null)
const queryExecuted = ref(false)
const selectedReturnNo = ref('')
const expandedDetailKeys = ref<string[]>([])
const notice = ref('DEV 假資料：唯讀查詢不呼叫正式 API；退料日依 Basis.ReturnDate（畫面改名 ConfirmDate）。')
const resultRows = computed(() => {
  const query = appliedCriteria.value
  if (!queryExecuted.value || !query) return []
  return bases.filter((row) => {
    if (query.useMaterialReturnNo && query.materialReturnNo && row.materialReturnNo !== query.materialReturnNo) return false
    if (query.useInventoryNo && query.inventoryNo && row.inventoryNo !== query.inventoryNo) return false
    if (query.useVendorNo && query.vendorNo && row.vendorNo !== query.vendorNo) return false
    if (query.useState && query.state !== '' && row.state !== Number(query.state)) return false
    if (query.useFromCreateDate && !dateMatches(row.createDate, query.fromCreateDate, '')) return false
    if (query.useEndCreateDate && !dateMatches(row.createDate, '', query.endCreateDate)) return false
    if (query.useFromReturnDate && !dateMatches(row.returnDate, query.fromReturnDate, '')) return false
    if (query.useEndReturnDate && !dateMatches(row.returnDate, '', query.endReturnDate)) return false
    return true
  })
})
const activeBasis = computed(() => resultRows.value.find((row) => row.materialReturnNo === selectedReturnNo.value) ?? null)
const visibleDetails = computed(() => activeBasis.value ? details.filter((row) => row.materialReturnNo === activeBasis.value?.materialReturnNo) : [])
function runQuery(): void {
  appliedCriteria.value = { ...criteria.value }
  queryExecuted.value = true
  selectedReturnNo.value = ''
  expandedDetailKeys.value = []
  const first = resultRows.value[0]
  selectedReturnNo.value = first?.materialReturnNo ?? ''
  notice.value = `DEV 查詢完成：${resultRows.value.length} 張退料單。主檔無明確排序／分頁，結果僅為固定假資料。`
}
function selectBasis(row: BasisRow): void { selectedReturnNo.value = row.materialReturnNo; expandedDetailKeys.value = [] }
function toggleDetail(key: string): void { expandedDetailKeys.value = expandedDetailKeys.value.includes(key) ? expandedDetailKeys.value.filter((item) => item !== key) : [...expandedDetailKeys.value, key] }
function componentRows(row: DetailRow): ReturnComponent[] { return components.filter((item) => row.componentIds.includes(item.componentId)) }
function close(): void { emit('close') }
</script>

<template>
  <main class="material-return-log-page" aria-label="退料單記錄 DEV">
    <header class="page-heading"><div><p>INVENTORY · IV211</p><h1>Material Return Log <span>退料單記錄</span></h1><small>唯讀查詢 · Return Date 來自 Basis.ReturnDate</small></div><button type="button" class="close-button" @click="close">Close (X)</button></header>
    <p class="notice" role="status">{{ notice }}</p>
    <section class="query-panel" aria-label="退料單記錄查詢">
      <label class="filter-field"><span><input v-model="criteria.useMaterialReturnNo" type="checkbox" /> MaterialReturnNo</span><input v-model="criteria.materialReturnNo" autocomplete="off" placeholder="精確單號" /></label>
      <label class="filter-field"><span><input v-model="criteria.useInventoryNo" type="checkbox" /> InventoryNo</span><select v-model="criteria.inventoryNo"><option value="">全部庫房</option><option v-for="row in inventories" :key="row.inventoryNo" :value="row.inventoryNo">{{ row.inventoryNo }} · {{ row.name }}</option></select></label>
      <label class="filter-field"><span><input v-model="criteria.useVendorNo" type="checkbox" /> VendorNo</span><select v-model="criteria.vendorNo"><option value="">全部供應商</option><option v-for="row in vendors" :key="row.vendorNo" :value="row.vendorNo">{{ row.vendorNo }} · {{ row.name }}</option></select></label>
      <label class="filter-field"><span><input v-model="criteria.useState" type="checkbox" /> State</span><select v-model="criteria.state"><option value="">全部狀態</option><option value="0">0 · UnConfirm</option><option value="1">1 · Handling</option><option value="2">2 · Confirm</option><option value="3">3 · Failed</option></select></label>
      <label class="filter-field"><span><input v-model="criteria.useFromCreateDate" type="checkbox" /> From CreateDate</span><input v-model="criteria.fromCreateDate" type="date" /></label>
      <label class="filter-field"><span><input v-model="criteria.useEndCreateDate" type="checkbox" /> End CreateDate</span><input v-model="criteria.endCreateDate" type="date" /></label>
      <label class="filter-field"><span><input v-model="criteria.useFromReturnDate" type="checkbox" /> From ReturnDate</span><input v-model="criteria.fromReturnDate" type="date" /></label>
      <label class="filter-field"><span><input v-model="criteria.useEndReturnDate" type="checkbox" /> End ReturnDate</span><input v-model="criteria.endReturnDate" type="date" /></label>
      <div class="query-footer"><span>日期預設最近 14 天至今天，但各條件需勾選後才送出。</span><button type="button" class="primary" @click="runQuery">Query</button></div>
    </section>
    <section class="panel basis-panel" aria-label="退料單主檔結果">
      <header class="panel-heading"><div><h2>MaterialReturn Basis</h2><span>{{ resultRows.length }} 筆查詢結果</span></div><small>{{ activeBasis ? `${activeBasis.materialReturnNo} · ${inventoryName(activeBasis.inventoryNo)} · ${vendorName(activeBasis.vendorNo)}` : '尚未選取單據' }}</small></header>
      <div class="table-wrap basis-table-wrap"><table><thead><tr><th>MaterialReturnNo</th><th>InventoryNo</th><th>VendorNo</th><th>State</th><th>ConfirmDate</th><th>Description</th><th>Creator</th><th>CreateDate</th></tr></thead><tbody>
        <tr v-for="row in resultRows" :key="row.materialReturnNo" :class="{ active: activeBasis?.materialReturnNo === row.materialReturnNo }" @click="selectBasis(row)"><td class="mono">{{ row.materialReturnNo }}</td><td>{{ row.inventoryNo }}</td><td>{{ row.vendorNo }}</td><td><span class="state-badge" :class="`state-${row.state}`">{{ row.state }} · {{ stateName(row.state) }}</span></td><td>{{ row.returnDate || '—' }}</td><td class="description">{{ row.description || '—' }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td></tr>
        <tr v-if="!queryExecuted"><td colspan="8" class="empty">請設定條件並按 Query；Form 開啟時不自動查詢。</td></tr><tr v-else-if="!resultRows.length"><td colspan="8" class="empty">沒有符合條件的退料單。</td></tr>
      </tbody></table></div><footer class="grid-footer"><span>Return Date 使用單頭欄位，Form 將 ReturnDate 顯示為 ConfirmDate。</span><span>唯讀查詢</span></footer>
    </section>
    <section class="panel detail-panel" aria-label="退料單明細結果">
      <header class="panel-heading"><div><h2>MaterialReturn Detail</h2><span>{{ activeBasis?.materialReturnNo ?? '請選取查詢結果' }} · {{ visibleDetails.length }} 筆明細</span></div><small>唯讀</small></header>
      <div class="table-wrap detail-table-wrap"><table><thead><tr><th>MaterialNo</th><th>MaterialLotNo</th><th>LocatorNo</th><th>UnitNo</th><th class="numeric">Qty</th><th>Component</th></tr></thead><tbody>
        <template v-for="row in visibleDetails" :key="row.key"><tr><td class="mono">{{ row.materialNo }}</td><td class="mono">{{ row.materialLotNo }}</td><td>{{ row.locatorNo }}</td><td>{{ row.unitNo }}</td><td class="numeric">{{ qtyText(row.qty) }}</td><td><button v-if="row.componentIds.length" type="button" class="text-button" @click="toggleDetail(row.key)">{{ expandedDetailKeys.includes(row.key) ? '收合' : `查看 ${row.componentIds.length} 個` }}</button><span v-else>—</span></td></tr>
          <tr v-if="row.componentIds.length && expandedDetailKeys.includes(row.key)" class="component-row"><td colspan="6"><strong>MaterialReturn Component</strong><span v-for="item in componentRows(row)" :key="item.componentId">{{ item.componentNo }} · Cassette {{ item.cassetteNo }} · Slot {{ item.slotNo }}</span></td></tr>
        </template><tr v-if="activeBasis && !visibleDetails.length"><td colspan="6" class="empty">此 DEV 假單沒有明細。</td></tr><tr v-else-if="!activeBasis"><td colspan="6" class="empty">選取單頭後載入該單明細。</td></tr>
      </tbody></table></div><footer class="grid-footer"><span>明細只依目前選取 MaterialReturnNo 載入，空單號回傳空列。</span><span>不顯示整張 Detail table</span></footer>
    </section>
  </main>
</template>

<style scoped>
.material-return-log-page { display:flex; flex-direction:column; gap:10px; min-height:0; height:100%; overflow:auto; padding:12px; box-sizing:border-box; background:#f7f5ee; color:#26251f; font:inherit; }
.page-heading,.panel-heading { display:flex; align-items:center; justify-content:space-between; gap:12px; }.page-heading p { margin:0 0 3px; color:#777366; font-size:11px; letter-spacing:.09em; }.page-heading h1 { margin:0; font-size:22px; font-weight:650; }.page-heading h1 span { margin-left:8px; color:#777366; font-size:16px; font-weight:450; }.page-heading small { display:block; margin-top:4px; color:#777366; }
button,input,select { font:inherit; } button { cursor:pointer; } button:disabled { opacity:.45; cursor:not-allowed; }.close-button,.query-footer button { min-height:29px; padding:4px 10px; border:1px solid #bdb9aa; border-radius:3px; background:#fffefa; color:#26251f; }.query-footer .primary,.primary { border-color:#3b3930; background:#3b3930; color:white; }.notice { margin:0; padding:7px 10px; border:1px solid #d9d3bf; border-radius:3px; background:#f1eddf; color:#625b46; font-size:12px; }
.query-panel,.panel { border:1px solid #c9c5b9; border-radius:4px; background:#fffefa; }.query-panel { display:grid; grid-template-columns:repeat(4,minmax(160px,1fr)); gap:8px 12px; padding:10px 12px; }.filter-field { display:flex; flex-direction:column; gap:4px; min-width:0; color:#4d4a40; font-size:11px; }.filter-field span { display:flex; align-items:center; gap:5px; }.filter-field>input,.filter-field>select { min-height:28px; box-sizing:border-box; padding:4px 7px; border:1px solid #c9c5b9; border-radius:3px; background:white; color:#26251f; }.query-footer { grid-column:1/-1; display:flex; align-items:center; justify-content:space-between; gap:10px; padding-top:4px; color:#777366; font-size:11px; }
.panel { display:flex; flex-direction:column; min-height:160px; overflow:hidden; }.panel-heading { padding:9px 11px; border-bottom:1px solid #d8d4ca; }.panel-heading h2 { margin:0; font-size:15px; }.panel-heading span,.panel-heading small { color:#777366; font-size:11px; }.panel-heading>div:first-child { display:flex; flex-direction:column; gap:2px; }.table-wrap { overflow:auto; flex:1; } table { width:100%; border-collapse:collapse; font-size:12px; white-space:nowrap; } th,td { padding:6px 8px; border-bottom:1px solid #e6e3db; text-align:left; } th { position:sticky; top:0; z-index:1; background:#efede6; color:#5c594f; font-weight:600; } tbody tr { cursor:pointer; } tbody tr:hover { background:#f6f4ec; } tbody tr.active { background:#e8edf1; }.mono { font-family:Consolas,'Courier New',monospace; }.numeric { text-align:right; font-variant-numeric:tabular-nums; }.description { min-width:180px; white-space:normal; }.state-badge { display:inline-block; padding:2px 6px; border-radius:10px; background:#f3ead1; color:#745d24; font-size:10px; }.state-2 { background:#e3efdf; color:#42683e; }.state-3 { background:#f6e2de; color:#91483c; }.empty { padding:20px; color:#777366; text-align:center; cursor:default; }.grid-footer { display:flex; justify-content:space-between; gap:10px; padding:6px 10px; border-top:1px solid #e6e3db; color:#777366; font-size:11px; }.detail-panel { flex:1; min-height:220px; }.text-button { padding:0; border:0; background:transparent; color:#315b79; text-decoration:underline; }.component-row { background:#f7f5ee; cursor:default; }.component-row td { display:flex; align-items:center; gap:14px; flex-wrap:wrap; }.component-row strong { color:#625b46; }
@media (max-width:900px) { .query-panel { grid-template-columns:repeat(2,minmax(130px,1fr)); } }
@media (max-width:650px) { .page-heading { align-items:flex-start; }.page-heading h1 { font-size:18px; }.query-panel { grid-template-columns:1fr; }.query-footer { align-items:flex-start; flex-direction:column; } }
</style>
