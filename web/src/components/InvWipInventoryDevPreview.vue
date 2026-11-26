<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type Tab = 'raw' | 'semiLot' | 'component' | 'bin'
interface Inventory { no: string; name: string }
interface Criteria { inventoryNo: string; lotPrefix: string; useLot: boolean; useDate: boolean; fromDate: string; endDate: string }
interface RawRow { inventoryNo: string; materialNo: string; materialName: string; lotNo: string; locatorNo: string; qty: number; unitNo: string; inputDate: string }
interface SemiRow { inventoryNo: string; baseLotNo: string; lotNo: string; productNo: string; productName: string; locatorNo: string; qty: number; unitNo: string; customerNo: string; inputDate: string; lotStatus: string; kind: 'lot' | 'component' | 'bin' }
interface ComponentRow { inventoryNo: string; lotNo: string; componentNo: string; goodQty: number; scrapQty: number; unitNo: string }
interface BinRow { lotNo: string; binNo: string; passQty: number; failQty: number; unitNo: string }

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: true })
const emit = defineEmits<{ close: [] }>()
const previewLabel = computed(() => props.developmentPreview ? 'DEV 假資料預覽' : 'DEV 模擬')
const inventories: Inventory[] = [
  { no: 'WIP-DEV-01', name: '在製品庫 A' },
  { no: 'WIP-DEV-02', name: '在製品庫 B' },
]
const rawRows: RawRow[] = [
  { inventoryNo: 'WIP-DEV-01', materialNo: 'RM-DEV-301', materialName: '晶圓保護膜', lotNo: 'RM-2611-A01', locatorNo: 'R-A-01', qty: 20, unitNo: 'ROLL', inputDate: '2026-11-24T08:30' },
  { inventoryNo: 'WIP-DEV-01', materialNo: 'RM-DEV-302', materialName: '切割膠帶', lotNo: 'RM-2611-A02', locatorNo: 'R-A-02', qty: 6, unitNo: 'ROLL', inputDate: '2026-11-23T15:40' },
  { inventoryNo: 'WIP-DEV-02', materialNo: 'RM-DEV-303', materialName: '清洗液', lotNo: 'RM-2611-B01', locatorNo: 'R-B-03', qty: 35.5, unitNo: 'L', inputDate: '2026-11-21T11:15' },
]
const semiRows: SemiRow[] = [
  { inventoryNo: 'WIP-DEV-01', baseLotNo: 'BASE-2611-01', lotNo: 'LOT-DEV-501', productNo: 'PRD-DEV-01', productName: '感測器晶片', locatorNo: 'S-A-01', qty: 120, unitNo: 'EA', customerNo: 'CUS-DEV-01', inputDate: '2026-11-24T09:20', lotStatus: '11 在製', kind: 'lot' },
  { inventoryNo: 'WIP-DEV-01', baseLotNo: 'BASE-2611-02', lotNo: 'LOT-DEV-502', productNo: 'PRD-DEV-02', productName: '電源管理晶片', locatorNo: 'S-A-02', qty: 80, unitNo: 'EA', customerNo: 'CUS-DEV-02', inputDate: '2026-11-22T13:05', lotStatus: '99 暫存', kind: 'lot' },
  { inventoryNo: 'WIP-DEV-02', baseLotNo: 'BASE-2611-03', lotNo: 'LOT-DEV-503', productNo: 'PRD-DEV-03', productName: '影像感測晶片', locatorNo: 'S-B-01', qty: 64, unitNo: 'EA', customerNo: 'CUS-DEV-03', inputDate: '2026-11-21T16:10', lotStatus: '11 在製', kind: 'lot' },
  { inventoryNo: 'WIP-DEV-01', baseLotNo: 'BASE-2611-04', lotNo: 'LOT-DEV-601', productNo: 'PRD-DEV-01', productName: '感測器晶片', locatorNo: 'S-A-03', qty: 40, unitNo: 'EA', customerNo: 'CUS-DEV-01', inputDate: '2026-11-23T10:40', lotStatus: '11 在製', kind: 'component' },
  { inventoryNo: 'WIP-DEV-01', baseLotNo: 'BASE-2611-05', lotNo: 'LOT-DEV-602', productNo: 'PRD-DEV-02', productName: '電源管理晶片', locatorNo: 'S-A-04', qty: 32, unitNo: 'EA', customerNo: 'CUS-DEV-02', inputDate: '2026-11-20T14:25', lotStatus: '11 在製', kind: 'component' },
  { inventoryNo: 'WIP-DEV-01', baseLotNo: 'BASE-2611-06', lotNo: 'LOT-DEV-701', productNo: 'PRD-DEV-01', productName: '感測器晶片', locatorNo: 'S-A-05', qty: 20, unitNo: 'EA', customerNo: 'CUS-DEV-01', inputDate: '2026-11-24T12:00', lotStatus: '11 在製', kind: 'bin' },
  { inventoryNo: 'WIP-DEV-02', baseLotNo: 'BASE-2611-07', lotNo: 'LOT-DEV-702', productNo: 'PRD-DEV-03', productName: '影像感測晶片', locatorNo: 'S-B-02', qty: 16, unitNo: 'EA', customerNo: 'CUS-DEV-03', inputDate: '2026-11-19T09:45', lotStatus: '11 在製', kind: 'bin' },
]
const componentRows: ComponentRow[] = [
  { inventoryNo: 'WIP-DEV-01', lotNo: 'LOT-DEV-601', componentNo: 'COMP-DEV-601-A', goodQty: 20, scrapQty: 0, unitNo: 'EA' },
  { inventoryNo: 'WIP-DEV-01', lotNo: 'LOT-DEV-601', componentNo: 'COMP-DEV-601-B', goodQty: 19, scrapQty: 1, unitNo: 'EA' },
  { inventoryNo: 'WIP-DEV-01', lotNo: 'LOT-DEV-602', componentNo: 'COMP-DEV-602-A', goodQty: 16, scrapQty: 0, unitNo: 'EA' },
  { inventoryNo: 'WIP-DEV-01', lotNo: 'LOT-DEV-602', componentNo: 'COMP-DEV-602-B', goodQty: 15, scrapQty: 1, unitNo: 'EA' },
]
const binRows: BinRow[] = [
  { lotNo: 'LOT-DEV-701', binNo: 'BIN-01', passQty: 12, failQty: 0, unitNo: 'EA' },
  { lotNo: 'LOT-DEV-701', binNo: 'BIN-02', passQty: 7, failQty: 1, unitNo: 'EA' },
  { lotNo: 'LOT-DEV-702', binNo: 'BIN-01', passQty: 10, failQty: 0, unitNo: 'EA' },
  { lotNo: 'LOT-DEV-702', binNo: 'BIN-03', passQty: 5, failQty: 1, unitNo: 'EA' },
]

function localDateTime(value: Date): string {
  const pad = (part: number) => String(part).padStart(2, '0')
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}`
}
function defaultCriteria(): Criteria {
  const end = new Date(); end.setHours(23, 59, 0, 0)
  const start = new Date(end); start.setDate(start.getDate() - 7); start.setHours(0, 0, 0, 0)
  return { inventoryNo: '', lotPrefix: '', useLot: false, useDate: false, fromDate: localDateTime(start), endDate: localDateTime(end) }
}
function dateText(value: string): string { return value.replace('T', ' ') + (value.length === 16 ? ':00' : '') }
function isInRange(value: string, from: string, end: string): boolean {
  const stamp = value.replace('T', ' ')
  return (!from || stamp >= from.replace('T', ' ')) && (!end || stamp <= end.replace('T', ' '))
}
function qty(value: number): string { return new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 6 }).format(value) }
function semiKey(row: SemiRow): string { return [row.inventoryNo, row.lotNo, row.locatorNo, row.kind].join('|') }

const criteria = ref<Criteria>(defaultCriteria())
const appliedCriteria = ref<Criteria | null>(null)
const activeTab = ref<Tab>('raw')
const selectedComponentLot = ref('')
const selectedBinLot = ref('')
const notice = ref('DEV 假資料；請選擇在製品庫並按 Query 載入庫存。')
const visibleRaw = computed(() => {
  const query = appliedCriteria.value
  if (!query) return []
  return rawRows.filter((row) => row.inventoryNo === query.inventoryNo
    && (!query.useLot || row.lotNo.startsWith(query.lotPrefix))
    && (!query.useDate || isInRange(row.inputDate, query.fromDate, query.endDate)))
    .slice().sort((a, b) => [a.materialNo, a.lotNo, a.locatorNo].join('|').localeCompare([b.materialNo, b.lotNo, b.locatorNo].join('|')))
})
const visibleSemi = computed(() => {
  const query = appliedCriteria.value
  if (!query) return []
  return semiRows.filter((row) => row.inventoryNo === query.inventoryNo
    && (!query.useLot || row.lotNo.startsWith(query.lotPrefix))
    && (!query.useDate || isInRange(row.inputDate, query.fromDate, query.endDate)))
})
const visibleSemiLots = computed(() => visibleSemi.value.filter((row) => row.kind === 'lot').slice().sort((a, b) => [a.productNo, a.lotNo, a.locatorNo].join('|').localeCompare([b.productNo, b.lotNo, b.locatorNo].join('|'))))
const visibleComponents = computed(() => visibleSemi.value.filter((row) => row.kind === 'component').slice().sort((a, b) => [a.productNo, a.lotNo, a.locatorNo].join('|').localeCompare([b.productNo, b.lotNo, b.locatorNo].join('|'))))
const visibleBins = computed(() => visibleSemi.value.filter((row) => row.kind === 'bin').slice().sort((a, b) => [a.productNo, a.lotNo, a.locatorNo].join('|').localeCompare([b.productNo, b.lotNo, b.locatorNo].join('|'))))
const selectedComponent = computed(() => visibleComponents.value.find((row) => semiKey(row) === selectedComponentLot.value) ?? null)
const selectedBin = computed(() => visibleBins.value.find((row) => semiKey(row) === selectedBinLot.value) ?? null)
const componentDetails = computed(() => selectedComponent.value ? componentRows.filter((row) => row.inventoryNo === selectedComponent.value?.inventoryNo && row.lotNo === selectedComponent.value?.lotNo) : [])
const binDetails = computed(() => selectedBin.value ? binRows.filter((row) => row.lotNo === selectedBin.value?.lotNo) : [])
const selectedInventory = computed(() => inventories.find((item) => item.no === appliedCriteria.value?.inventoryNo)?.name ?? '')
const totalRows = computed(() => visibleRaw.value.length + visibleSemiLots.value.length + visibleComponents.value.length + visibleBins.value.length)

watch(visibleComponents, (rows) => { if (!rows.some((row) => semiKey(row) === selectedComponentLot.value)) selectedComponentLot.value = rows[0] ? semiKey(rows[0]) : '' }, { immediate: true })
watch(visibleBins, (rows) => { if (!rows.some((row) => semiKey(row) === selectedBinLot.value)) selectedBinLot.value = rows[0] ? semiKey(rows[0]) : '' }, { immediate: true })

function runQuery(): void {
  if (!criteria.value.inventoryNo) { notice.value = '請先選擇 InventoryNo。'; return }
  if (criteria.value.useLot && !criteria.value.lotPrefix.trim()) { notice.value = '勾選 LotNo 時請輸入批號前綴。'; return }
  if (criteria.value.useDate && criteria.value.fromDate && criteria.value.endDate && criteria.value.fromDate > criteria.value.endDate) { notice.value = 'INVDate 起日不可晚於迄日。'; return }
  appliedCriteria.value = { ...criteria.value, lotPrefix: criteria.value.lotPrefix.trim() }
  selectedComponentLot.value = ''; selectedBinLot.value = ''
  notice.value = `DEV 查詢完成：${selectedInventory.value}，合計 ${totalRows.value} 列庫存資料。`
}
function clearQuery(): void {
  criteria.value = defaultCriteria(); appliedCriteria.value = null; selectedComponentLot.value = ''; selectedBinLot.value = ''; activeTab.value = 'raw'
  notice.value = '查詢結果已清除；請選擇在製品庫並按 Query。'
}
function selectComponent(row: SemiRow): void { selectedComponentLot.value = semiKey(row) }
function selectBin(row: SemiRow): void { selectedBinLot.value = semiKey(row) }
function syncLotFilter(): void { criteria.value.useLot = criteria.value.lotPrefix.length > 0 }
function toggleLotFilter(): void { if (!criteria.value.useLot) criteria.value.lotPrefix = '' }
</script>

<template>
  <main class="wip-inventory" aria-label="在製品庫庫房資料">
    <header class="page-head">
      <div><p class="eyebrow">INV · IV301</p><h1>在製品庫庫房資料</h1><p class="subtitle">WIP Inventory · 在製品原料、批號、元件及 Bin 庫存查詢</p></div>
      <div class="head-actions"><span class="dev-tag">{{ previewLabel }}</span><button type="button" class="close" @click="emit('close')">Close (X)</button></div>
    </header>

    <section class="query-card" aria-label="Query">
      <div class="query-title"><div><h2>Query</h2><p>必選 InventoryNo；LotNo 與 INVDate 為可選條件</p></div><span class="source-note">日期欄位：InputDate</span></div>
      <div class="criteria-grid">
        <label class="field inventory-field"><span>InventoryNo <b>*</b></span><select v-model="criteria.inventoryNo"><option value="">請選擇庫房</option><option v-for="item in inventories" :key="item.no" :value="item.no">{{ item.no }} · {{ item.name }}</option></select></label>
        <label class="check-field"><input v-model="criteria.useLot" type="checkbox" @change="toggleLotFilter"><span>LotNo</span></label>
        <label class="field lot-field"><span>批號前綴</span><input v-model="criteria.lotPrefix" @input="syncLotFilter" placeholder="輸入 LotNo 前綴"></label>
        <label class="check-field date-check"><input v-model="criteria.useDate" type="checkbox"><span>INVDate</span></label>
        <label class="field"><span>From</span><input v-model="criteria.fromDate" type="datetime-local" :disabled="!criteria.useDate"></label>
        <label class="field"><span>To</span><input v-model="criteria.endDate" type="datetime-local" :disabled="!criteria.useDate"></label>
      </div>
      <div class="query-footer"><p class="hint">舊畫面預設顯示最近一週，但日期條件需勾選才送出。</p><div class="actions"><button type="button" class="secondary" @click="clearQuery">Clear</button><button type="button" class="primary" @click="runQuery">Query</button></div></div>
    </section>

    <section class="results-card">
      <div class="result-summary"><div><h2>庫存結果</h2><p>{{ appliedCriteria ? `${selectedInventory} · ${totalRows} 列` : '尚未查詢' }}</p></div><span v-if="appliedCriteria" class="read-only">唯讀查詢</span></div>
      <div class="tabs" role="tablist" aria-label="庫存類型">
        <button type="button" role="tab" :aria-selected="activeTab === 'raw'" @click="activeTab = 'raw'">RAW <span>{{ visibleRaw.length }}</span></button>
        <button type="button" role="tab" :aria-selected="activeTab === 'semiLot'" @click="activeTab = 'semiLot'">SEMI Lot <span>{{ visibleSemiLots.length }}</span></button>
        <button type="button" role="tab" :aria-selected="activeTab === 'component'" @click="activeTab = 'component'">SEMI Component <span>{{ visibleComponents.length }}</span></button>
        <button type="button" role="tab" :aria-selected="activeTab === 'bin'" @click="activeTab = 'bin'">SEMI Bin <span>{{ visibleBins.length }}</span></button>
      </div>
      <p class="notice" role="status">{{ notice }}</p>

      <div v-if="activeTab === 'raw'" class="grid-wrap">
        <table><thead><tr><th>MaterialNo</th><th>MaterialName</th><th>MaterialLotNo</th><th>LocatorNo</th><th class="numeric">Qty</th><th>UnitNo</th><th>InputDate</th></tr></thead>
          <tbody><tr v-for="row in visibleRaw" :key="`${row.inventoryNo}|${row.materialNo}|${row.lotNo}|${row.locatorNo}`"><td class="code">{{ row.materialNo }}</td><td>{{ row.materialName }}</td><td class="code">{{ row.lotNo }}</td><td class="code">{{ row.locatorNo }}</td><td class="numeric">{{ qty(row.qty) }}</td><td>{{ row.unitNo }}</td><td>{{ dateText(row.inputDate) }}</td></tr><tr v-if="!visibleRaw.length"><td colspan="7" class="empty">{{ appliedCriteria ? '沒有符合的 RAW 庫存' : '按 Query 後顯示資料' }}</td></tr></tbody>
        </table>
      </div>
      <div v-else-if="activeTab === 'semiLot'" class="grid-wrap">
        <table><thead><tr><th>ProductNo</th><th>ProductName</th><th>LotNo</th><th>BaseLotNo</th><th>LocatorNo</th><th class="numeric">Qty</th><th>UnitNo</th><th>CustomerNo</th><th>InputDate</th><th>Status</th></tr></thead>
          <tbody><tr v-for="row in visibleSemiLots" :key="semiKey(row)"><td class="code">{{ row.productNo }}</td><td>{{ row.productName }}</td><td class="code">{{ row.lotNo }}</td><td class="code">{{ row.baseLotNo }}</td><td class="code">{{ row.locatorNo }}</td><td class="numeric">{{ qty(row.qty) }}</td><td>{{ row.unitNo }}</td><td class="code">{{ row.customerNo }}</td><td>{{ dateText(row.inputDate) }}</td><td><span class="status">{{ row.lotStatus }}</span></td></tr><tr v-if="!visibleSemiLots.length"><td colspan="10" class="empty">{{ appliedCriteria ? '沒有符合的 SEMI Lot 庫存' : '按 Query 後顯示資料' }}</td></tr></tbody>
        </table>
      </div>
      <div v-else-if="activeTab === 'component'" class="split-grid">
        <div class="grid-wrap master-grid"><div class="grid-caption">SEMI Component 庫存 · 選列載入 Component State</div><table><thead><tr><th>ProductNo</th><th>ProductName</th><th>LotNo</th><th>LocatorNo</th><th class="numeric">Qty</th><th>InputDate</th></tr></thead>
          <tbody><tr v-for="row in visibleComponents" :key="semiKey(row)" :class="{ selected: semiKey(row) === selectedComponentLot }" @click="selectComponent(row)"><td class="code">{{ row.productNo }}</td><td>{{ row.productName }}</td><td class="code">{{ row.lotNo }}</td><td class="code">{{ row.locatorNo }}</td><td class="numeric">{{ qty(row.qty) }} {{ row.unitNo }}</td><td>{{ dateText(row.inputDate) }}</td></tr><tr v-if="!visibleComponents.length"><td colspan="6" class="empty">{{ appliedCriteria ? '沒有符合的 SEMI Component 庫存' : '按 Query 後顯示資料' }}</td></tr></tbody>
        </table></div>
        <div class="grid-wrap detail-grid"><div class="grid-caption">Component State <span v-if="selectedComponent">· {{ selectedComponent.lotNo }}</span></div><table><thead><tr><th>ComponentNo</th><th class="numeric">GoodQty</th><th class="numeric">ScrapQty</th><th>UnitNo</th></tr></thead>
          <tbody><tr v-for="row in componentDetails" :key="row.componentNo"><td class="code">{{ row.componentNo }}</td><td class="numeric">{{ qty(row.goodQty) }}</td><td class="numeric">{{ qty(row.scrapQty) }}</td><td>{{ row.unitNo }}</td></tr><tr v-if="!componentDetails.length"><td colspan="4" class="empty">{{ selectedComponent ? '此批號沒有元件明細' : '選取上方批號查看元件' }}</td></tr></tbody>
        </table></div>
      </div>
      <div v-else class="split-grid bin-split">
        <div class="grid-wrap master-grid"><div class="grid-caption">SEMI Bin 庫存 · 選列載入 GoodBin State</div><table><thead><tr><th>ProductNo</th><th>ProductName</th><th>LotNo</th><th>LocatorNo</th><th class="numeric">Qty</th><th>InputDate</th></tr></thead>
          <tbody><tr v-for="row in visibleBins" :key="semiKey(row)" :class="{ selected: semiKey(row) === selectedBinLot }" @click="selectBin(row)"><td class="code">{{ row.productNo }}</td><td>{{ row.productName }}</td><td class="code">{{ row.lotNo }}</td><td class="code">{{ row.locatorNo }}</td><td class="numeric">{{ qty(row.qty) }} {{ row.unitNo }}</td><td>{{ dateText(row.inputDate) }}</td></tr><tr v-if="!visibleBins.length"><td colspan="6" class="empty">{{ appliedCriteria ? '沒有符合的 SEMI Bin 庫存' : '按 Query 後顯示資料' }}</td></tr></tbody>
        </table></div>
        <div class="grid-wrap detail-grid"><div class="grid-caption">GoodBin State <span v-if="selectedBin">· {{ selectedBin.lotNo }}</span></div><table><thead><tr><th>BinNo</th><th class="numeric">PassQty</th><th class="numeric">FailQty</th><th>UnitNo</th></tr></thead>
          <tbody><tr v-for="row in binDetails" :key="row.binNo"><td class="code">{{ row.binNo }}</td><td class="numeric">{{ qty(row.passQty) }}</td><td class="numeric">{{ qty(row.failQty) }}</td><td>{{ row.unitNo }}</td></tr><tr v-if="!binDetails.length"><td colspan="4" class="empty">{{ selectedBin ? '此批號沒有 Bin 明細' : '選取上方批號查看 Bin' }}</td></tr></tbody>
        </table></div>
      </div>
      <footer class="result-footer"><span>資料僅存在本頁記憶體，不會呼叫正式 API 或寫入資料庫。</span><span>IV301 · InventoryType=6</span></footer>
    </section>
  </main>
</template>

<style scoped>
.wip-inventory { min-height: 0; height: 100%; overflow: auto; box-sizing: border-box; padding: 18px 22px 22px; background: #f1f0ea; color: #2e302d; font: 13px/1.45 "Segoe UI", "Microsoft JhengHei", sans-serif; }
.page-head,.head-actions,.query-title,.query-footer,.actions,.result-summary,.result-footer { display: flex; align-items: center; justify-content: space-between; gap: 14px; }
.page-head { margin: 0 auto 16px; max-width: 1440px; }
.page-head h1 { margin: 0; font-size: 22px; line-height: 1.25; color: #202820; }
.eyebrow { margin: 0 0 3px; color: #548066; font-size: 11px; font-weight: 700; letter-spacing: .12em; }
.subtitle,.query-title p,.result-summary p { margin: 3px 0 0; color: #73766e; }
.head-actions { flex-shrink: 0; }
.dev-tag,.read-only { border: 1px solid #b9d0bd; border-radius: 999px; padding: 4px 10px; color: #376448; background: #eaf3e9; font-size: 11px; font-weight: 700; }
.close,.primary,.secondary { min-height: 32px; border: 1px solid #c5c7bf; border-radius: 5px; padding: 5px 13px; background: #fff; color: #3c423b; font: inherit; cursor: pointer; }
.primary { border-color: #427b57; background: #427b57; color: #fff; font-weight: 700; }
.query-card,.results-card { max-width: 1440px; margin: 0 auto 15px; border: 1px solid #d6d7d0; border-radius: 8px; background: #fff; box-shadow: 0 2px 8px #26251f0a; }
.query-card { padding: 14px 16px 12px; }
.query-title { margin-bottom: 13px; }
.query-title h2,.result-summary h2 { margin: 0; font-size: 15px; color: #2a352d; }
.query-title p { font-size: 12px; }
.source-note { color: #777b71; font-size: 11px; }
.criteria-grid { display: grid; grid-template-columns: minmax(240px, 1.2fr) 82px minmax(170px, 1fr) 88px minmax(205px, 1fr) minmax(205px, 1fr); align-items: end; gap: 10px; }
.field { display: grid; gap: 4px; min-width: 0; color: #60645c; font-size: 11px; }
.field > span b { color: #ba4b42; }
.field input,.field select { box-sizing: border-box; width: 100%; min-height: 32px; border: 1px solid #d2d5ce; border-radius: 4px; padding: 5px 8px; color: #30342f; background: #fff; font: inherit; font-size: 12px; }
.field input:disabled { background: #f2f3f0; color: #9a9d96; }
.check-field { display: flex; align-items: center; min-height: 32px; gap: 6px; color: #4c514a; font-weight: 600; }
.check-field input { accent-color: #477e5b; }
.date-check { margin-left: 5px; }
.query-footer { margin-top: 12px; border-top: 1px solid #ecece7; padding-top: 10px; }
.hint { margin: 0; color: #797d75; font-size: 11px; }
.actions { justify-content: flex-end; }
.results-card { overflow: hidden; }
.result-summary { padding: 13px 16px 11px; }
.result-summary p { font-size: 11px; }
.tabs { display: flex; gap: 3px; padding: 0 12px; border-bottom: 1px solid #dfe1db; background: #f7f8f5; }
.tabs button { display: flex; align-items: center; gap: 8px; min-height: 40px; padding: 0 13px; border: 0; border-bottom: 2px solid transparent; background: transparent; color: #686d65; font: inherit; font-weight: 650; cursor: pointer; }
.tabs button[aria-selected="true"] { border-bottom-color: #4f8a63; color: #326044; }
.tabs button span { min-width: 20px; border-radius: 10px; padding: 1px 5px; background: #e9ece7; color: #646b61; text-align: center; font-size: 10px; }
.notice { margin: 10px 14px; border-left: 3px solid #78a482; padding: 5px 9px; color: #5c675b; background: #f4f8f2; font-size: 11px; }
.grid-wrap { overflow: auto; }
table { width: 100%; min-width: 760px; border-collapse: collapse; white-space: nowrap; font-size: 11px; }
th { position: sticky; top: 0; z-index: 1; background: #f1f4ef; color: #556052; text-align: left; font-weight: 700; }
th,td { border-bottom: 1px solid #e7e9e4; padding: 8px 10px; }
tbody tr:hover { background: #f7faf6; }
tbody tr.selected { background: #eaf3e9; }
.numeric { text-align: right; font-variant-numeric: tabular-nums; }
.code { font-family: Consolas, "Courier New", monospace; color: #3c5845; }
.status { display: inline-block; border-radius: 4px; padding: 2px 6px; background: #eef2e6; color: #5c713e; }
.empty { height: 72px; color: #8a8e85; text-align: center; vertical-align: middle; }
.split-grid { display: grid; grid-template-rows: minmax(165px, 1fr) minmax(140px, .75fr); min-height: 400px; }
.master-grid,.detail-grid { border-bottom: 1px solid #dfe1db; }
.grid-caption { position: sticky; left: 0; padding: 8px 10px; border-bottom: 1px solid #e7e9e4; background: #fbfcfa; color: #596253; font-size: 11px; font-weight: 700; }
.grid-caption span { color: #467253; font-family: Consolas, "Courier New", monospace; }
.split-grid table { min-width: 680px; }
.result-footer { padding: 9px 14px; color: #82867d; background: #fafbf9; font-size: 10px; }
@media (max-width: 1120px) { .criteria-grid { grid-template-columns: repeat(3, minmax(170px, 1fr)); } }
@media (max-width: 720px) { .wip-inventory { padding: 12px; } .page-head { align-items: flex-start; } .head-actions { align-items: flex-end; flex-direction: column; gap: 6px; } .criteria-grid { grid-template-columns: 1fr 1fr; } .inventory-field { grid-column: 1 / -1; } .query-footer { align-items: flex-start; flex-direction: column; } .actions { width: 100%; } .actions button { flex: 1; } .tabs { overflow: auto; } .tabs button { flex: 0 0 auto; } .result-footer { align-items: flex-start; flex-direction: column; gap: 4px; } }
</style>
