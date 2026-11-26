<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type AdjustType = 0 | 1
type LookupMode = 'document' | 'inventory' | null
interface ComponentRow { componentNo: string; cassetteNo: string; slotNo: string }
interface Basis { no: string; inventoryNo: string; adjustType: AdjustType; state: number; confirmDate: string; description: string; creator: string; createDate: string; invoiceDate: string }
interface Detail { key: string; no: string; materialNo: string; materialName: string; lotNo: string; locatorNo: string; finalLocatorNo: string; unitNo: string; orgQty: number; finalQty: number; diffQty: number; components: ComponentRow[] }
interface Criteria { no: string; inventoryNo: string; state: string; fromCreateDate: string; endCreateDate: string; fromInvDate: string; endInvDate: string }

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: true })
const emit = defineEmits<{ close: [] }>()
const previewLabel = computed(() => props.developmentPreview ? 'DEV 假資料預覽' : 'DEV 模擬')
const inventories = [
  { no: 'RAW-DEV-01', name: '原料庫 A' },
  { no: 'RAW-DEV-02', name: '原料冷藏庫 B' },
]
const bases: Basis[] = [
  { no: 'IA-DEV-26001', inventoryNo: 'RAW-DEV-01', adjustType: 0, state: 0, confirmDate: '', description: '草稿：盤點後調整樹脂數量。', creator: 'DEV001', createDate: '2026-11-21 09:30:00', invoiceDate: '' },
  { no: 'IA-DEV-26002', inventoryNo: 'RAW-DEV-01', adjustType: 1, state: 1, confirmDate: '', description: '處理中：感測器批次移儲位。', creator: 'DEV002', createDate: '2026-11-22 14:10:00', invoiceDate: '' },
  { no: 'IA-DEV-25970', inventoryNo: 'RAW-DEV-02', adjustType: 0, state: 2, confirmDate: '2026-11-18 16:40:00', description: '已確認：銅箔數量調整。', creator: 'DEV003', createDate: '2026-11-18 15:50:00', invoiceDate: '2026-11-18 16:40:00' },
  { no: 'IA-DEV-25969', inventoryNo: 'RAW-DEV-01', adjustType: 1, state: 3, confirmDate: '', description: '失敗：目的儲位容量不足。', creator: 'DEV002', createDate: '2026-11-17 10:15:00', invoiceDate: '2026-11-17 10:45:00' },
]
const details: Detail[] = [
  { key: 'd-26001-resin', no: 'IA-DEV-26001', materialNo: 'RM-RESIN-01', materialName: '環氧樹脂 A', lotNo: 'RES-2611-001', locatorNo: 'A-01', finalLocatorNo: 'A-01', unitNo: 'KG', orgQty: 20, finalQty: 22, diffQty: 2, components: [] },
  { key: 'd-26002-sensor', no: 'IA-DEV-26002', materialNo: 'RM-SENSOR-05', materialName: '感測器晶圓 S', lotNo: 'SNS-2611-005', locatorNo: 'A-03', finalLocatorNo: 'A-02', unitNo: 'EA', orgQty: 4, finalQty: 0, diffQty: 0, components: [
    { componentNo: 'COMP-SNS-A', cassetteNo: 'CAS-01', slotNo: '01' },
    { componentNo: 'COMP-SNS-B', cassetteNo: 'CAS-01', slotNo: '02' },
  ] },
  { key: 'd-25970-copper', no: 'IA-DEV-25970', materialNo: 'RM-COPPER-03', materialName: '銅箔 C', lotNo: 'CU-2611-020', locatorNo: 'B-01', finalLocatorNo: 'B-01', unitNo: 'RL', orgQty: 32, finalQty: 30, diffQty: -2, components: [] },
  { key: 'd-25969-sensor', no: 'IA-DEV-25969', materialNo: 'RM-SENSOR-05', materialName: '感測器晶圓 S', lotNo: 'SNS-2610-088', locatorNo: 'A-03', finalLocatorNo: 'A-01', unitNo: 'EA', orgQty: 6, finalQty: 0, diffQty: 0, components: [
    { componentNo: 'COMP-SNS-X', cassetteNo: 'CAS-08', slotNo: '01' },
  ] },
]
const initialCriteria = (): Criteria => ({ no: '', inventoryNo: '', state: '', fromCreateDate: '', endCreateDate: '', fromInvDate: '', endInvDate: '' })
function datePart(value: string): string { return value.slice(0, 10) }
function within(value: string, from: string, to: string): boolean {
  if (!value) return !from && !to
  const date = datePart(value)
  return (!from || date >= from) && (!to || date <= to)
}
function qtyText(value: number): string { return new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 8 }).format(value) }
function typeName(value: AdjustType): string { return value === 0 ? '調帳' : '儲位調撥' }
function stateName(value: number): string { return ({ 0: '未確認', 1: '處理中', 2: '已確認', 3: '失敗' } as Record<number, string>)[value] ?? String(value) }
function rowKey(row: Detail): string { return [row.no, row.materialNo, row.lotNo, row.locatorNo].join('|') }

const criteria = ref<Criteria>(initialCriteria())
const appliedCriteria = ref<Criteria | null>(null)
const visibleBases = computed(() => {
  if (!appliedCriteria.value) return []
  const query = appliedCriteria.value
  return bases.filter((row) => (!query.no || row.no === query.no)
    && (!query.inventoryNo || row.inventoryNo === query.inventoryNo)
    && (query.state === '' || row.state === Number(query.state))
    && within(row.createDate, query.fromCreateDate, query.endCreateDate)
    && within(row.invoiceDate, query.fromInvDate, query.endInvDate))
    .slice().sort((a, b) => a.no.localeCompare(b.no))
})
const selectedBasisNo = ref('')
const activeBasis = computed(() => visibleBases.value.find((row) => row.no === selectedBasisNo.value) ?? null)
const visibleDetails = computed(() => activeBasis.value ? details.filter((row) => row.no === activeBasis.value?.no)
  .slice().sort((a, b) => [a.materialNo, a.lotNo, a.locatorNo].join('|').localeCompare([b.materialNo, b.lotNo, b.locatorNo].join('|'))) : [])
const selectedDetailKey = ref('')
const expandedDetailKey = ref('')
const lookupMode = ref<LookupMode>(null)
const lookupText = ref('')
const lookupSelection = ref('')
const notice = ref('DEV 假資料：按 Query 載入庫存調整記錄；查詢與明細只讀，不呼叫正式 API。')
const filteredLookupBases = computed(() => bases.filter((row) => !lookupText.value || row.no.toLowerCase().includes(lookupText.value.toLowerCase())))
const filteredLookupInventories = computed(() => inventories.filter((row) => !lookupText.value || row.no.toLowerCase().includes(lookupText.value.toLowerCase()) || row.name.includes(lookupText.value)))
const selectedDetail = computed(() => visibleDetails.value.find((row) => rowKey(row) === selectedDetailKey.value) ?? null)
const stateCounts = computed(() => ({ draft: visibleBases.value.filter((row) => row.state === 0).length, handling: visibleBases.value.filter((row) => row.state === 1).length, confirmed: visibleBases.value.filter((row) => row.state === 2).length, failed: visibleBases.value.filter((row) => row.state === 3).length }))

watch(visibleBases, (rows) => {
  if (!rows.some((row) => row.no === selectedBasisNo.value)) selectedBasisNo.value = rows[0]?.no ?? ''
}, { immediate: true })
watch(activeBasis, () => { selectedDetailKey.value = ''; expandedDetailKey.value = '' })

function runQuery(): void {
  appliedCriteria.value = { ...criteria.value }
  selectedBasisNo.value = ''
  selectedDetailKey.value = ''
  notice.value = `DEV 查詢完成：${visibleBases.value.length} 筆調整單。`
}
function clearQuery(): void {
  criteria.value = initialCriteria(); appliedCriteria.value = null; selectedBasisNo.value = ''; selectedDetailKey.value = ''; expandedDetailKey.value = ''
  notice.value = '查詢條件已清除；請按 Query 載入記錄。'
}
function selectBasis(row: Basis): void { selectedBasisNo.value = row.no; selectedDetailKey.value = ''; expandedDetailKey.value = '' }
function toggleDetail(row: Detail): void { const key = rowKey(row); selectedDetailKey.value = selectedDetailKey.value === key ? '' : key; expandedDetailKey.value = expandedDetailKey.value === key ? '' : key }
function openLookup(mode: Exclude<LookupMode, null>): void { lookupMode.value = mode; lookupText.value = ''; lookupSelection.value = mode === 'document' ? criteria.value.no : criteria.value.inventoryNo }
function chooseLookup(): void {
  if (!lookupSelection.value) return
  if (lookupMode.value === 'document') criteria.value.no = lookupSelection.value
  if (lookupMode.value === 'inventory') criteria.value.inventoryNo = lookupSelection.value
  lookupMode.value = null
}
function clearLookup(): void { if (lookupMode.value === 'document') criteria.value.no = ''; if (lookupMode.value === 'inventory') criteria.value.inventoryNo = ''; lookupMode.value = null }
</script>

<template>
  <main class="adjust-log-page" aria-label="庫存調整單記錄 DEV 預覽">
    <header class="page-head"><div><p class="eyebrow">INV / IV209 · {{ previewLabel }}</p><h1>庫存調整單記錄</h1><p class="sub">原物料倉 · 唯讀查詢</p></div><button type="button" class="close" title="Close (X)" @click="emit('close')">×</button></header>
    <p class="notice" role="status">{{ notice }}</p>
    <section class="query-panel" aria-label="調整單查詢條件">
      <label>Material Adjust No<div class="lookup-field"><input v-model.trim="criteria.no" placeholder="輸入完整單號" @keyup.enter="runQuery" /><button type="button" @click="openLookup('document')">…</button></div></label>
      <label>Inventory No<div class="lookup-field"><input v-model.trim="criteria.inventoryNo" placeholder="輸入庫房代碼" @keyup.enter="runQuery" /><button type="button" @click="openLookup('inventory')">…</button></div></label>
      <label>State<select v-model="criteria.state"><option value="">全部狀態</option><option value="0">未確認 (0)</option><option value="1">處理中 (1)</option><option value="2">已確認 (2)</option><option value="3">失敗 (3)</option></select></label>
      <label>From CreateDate<input v-model="criteria.fromCreateDate" type="date" /></label>
      <label>End CreateDate<input v-model="criteria.endCreateDate" type="date" /></label>
      <label>From INV Date<input v-model="criteria.fromInvDate" type="date" /></label>
      <label>End INV Date<input v-model="criteria.endInvDate" type="date" /></label>
      <div class="query-actions"><span>日期為可選條件；結束日包含當日。INV Date 對應 Source=3 交易記錄。</span><div><button type="button" @click="clearQuery">Clear</button><button type="button" class="primary" @click="runQuery">Query</button></div></div>
    </section>
    <section class="state-strip"><span>查詢結果 <strong>{{ visibleBases.length }}</strong></span><span>未確認 <strong>{{ stateCounts.draft }}</strong></span><span>處理中 <strong>{{ stateCounts.handling }}</strong></span><span>已確認 <strong>{{ stateCounts.confirmed }}</strong></span><span>失敗 <strong>{{ stateCounts.failed }}</strong></span></section>
    <section class="panel basis-panel" aria-label="庫存調整單主檔記錄">
      <header class="panel-title"><div><h2>MaterialAdjust Basis</h2><span>{{ appliedCriteria ? '依單號排序' : '請設定條件後按 Query' }}</span></div><small>{{ activeBasis?.no || '未選取單據' }}</small></header>
      <div class="table-wrap"><table class="basis-table"><thead><tr><th>MaterialAdjustNo</th><th>InventoryNo</th><th>AdjustType</th><th>State</th><th>ConfirmDate</th><th>Description</th><th>Creator</th><th>CreateDate</th></tr></thead>
        <tbody><tr v-for="row in visibleBases" :key="row.no" :class="{ active: activeBasis?.no === row.no }" @click="selectBasis(row)"><td class="mono">{{ row.no }}</td><td>{{ row.inventoryNo }}</td><td>{{ typeName(row.adjustType) }}</td><td><span class="state" :class="'state-' + row.state">{{ stateName(row.state) }}</span></td><td>{{ row.confirmDate || '—' }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td></tr>
          <tr v-if="!appliedCriteria"><td colspan="8" class="empty">設定條件後按 Query；查詢結果尚未載入。</td></tr><tr v-else-if="!visibleBases.length"><td colspan="8" class="empty">沒有符合條件的庫存調整單。</td></tr></tbody></table></div>
    </section>
    <section class="panel detail-panel" aria-label="庫存調整單明細記錄">
      <header class="panel-title"><div><h2>MaterialAdjust Detail</h2><span v-if="activeBasis">{{ activeBasis.no }} · {{ typeName(activeBasis.adjustType) }}</span><span v-else>選取主檔載入明細</span></div><small>{{ visibleDetails.length }} 筆明細</small></header>
      <div class="table-wrap"><table class="detail-table"><thead><tr><th>MaterialNo</th><th>MaterialLotNo</th><th>LocatorNo</th><th>FinalLocatorNo</th><th>UnitNo</th><th>OrgQty</th><th>FinalQty</th><th>DiffQty</th></tr></thead>
        <tbody><template v-for="row in visibleDetails" :key="row.key"><tr :class="{ active: selectedDetailKey === rowKey(row) }" @click="toggleDetail(row)"><td class="mono">{{ row.materialNo }}</td><td class="mono">{{ row.lotNo }}</td><td>{{ row.locatorNo }}</td><td>{{ row.finalLocatorNo }}</td><td>{{ row.unitNo }}</td><td class="num">{{ qtyText(row.orgQty) }}</td><td class="num">{{ qtyText(row.finalQty) }}</td><td class="num" :class="row.diffQty < 0 ? 'negative' : row.diffQty > 0 ? 'positive' : ''">{{ qtyText(row.diffQty) }}</td></tr>
            <tr v-if="expandedDetailKey === rowKey(row) && row.components.length" class="component-row"><td colspan="8"><div class="component-list"><strong>Component Snapshot</strong><span v-for="item in row.components" :key="item.componentNo + item.slotNo">{{ item.componentNo }} · Cassette {{ item.cassetteNo || '—' }} · Slot {{ item.slotNo || '—' }}</span></div></td></tr>
          </template><tr v-if="activeBasis && !visibleDetails.length"><td colspan="8" class="empty">此單據沒有明細。</td></tr><tr v-if="appliedCriteria && !activeBasis"><td colspan="8" class="empty">選取一筆主檔後載入明細。</td></tr><tr v-if="!appliedCriteria"><td colspan="8" class="empty">查詢後選取主檔載入明細。</td></tr></tbody></table></div>
      <footer class="read-only-note"><span>此功能唯讀，不提供建立、編輯、刪除或確認操作。</span><span v-if="selectedDetail">所選明細 Component：{{ selectedDetail.components.length }} 筆</span></footer>
    </section>
    <aside class="source-note"><strong>DEV 模擬</strong><span>Basis、Detail 與 Component 均為虛構資料。正式 API 必須要求父單號，避免舊 Core 在空單號時讀取全表。</span></aside>

    <div v-if="lookupMode" class="backdrop" role="presentation" @click.self="lookupMode = null"><section class="dialog" role="dialog" aria-modal="true" :aria-label="lookupMode === 'document' ? '選擇調整單' : '選擇原料庫房'"><header><div><p class="eyebrow">INV / IV209</p><h2>{{ lookupMode === 'document' ? 'Material Adjust No Lookup' : 'Inventory Lookup' }}</h2></div><button type="button" class="close" @click="lookupMode = null">×</button></header>
        <label class="lookup-search">搜尋<input v-model.trim="lookupText" :placeholder="lookupMode === 'document' ? '調整單號' : '庫房代碼或名稱'" /></label>
        <div class="lookup-results"><template v-if="lookupMode === 'document'"><button v-for="row in filteredLookupBases" :key="row.no" type="button" :class="{ active: lookupSelection === row.no }" @click="lookupSelection = row.no"><strong>{{ row.no }}</strong><span>{{ row.inventoryNo }} · {{ typeName(row.adjustType) }} · {{ stateName(row.state) }}</span></button></template><template v-else><button v-for="row in filteredLookupInventories" :key="row.no" type="button" :class="{ active: lookupSelection === row.no }" @click="lookupSelection = row.no"><strong>{{ row.no }}</strong><span>{{ row.name }}</span></button></template><p v-if="lookupMode === 'document' && !filteredLookupBases.length" class="empty">沒有符合單據。</p><p v-if="lookupMode === 'inventory' && !filteredLookupInventories.length" class="empty">沒有符合庫房。</p></div>
        <footer><button type="button" @click="clearLookup">Clear</button><button type="button" @click="lookupMode = null">Cancel</button><button type="button" class="primary" :disabled="!lookupSelection" @click="chooseLookup">Select</button></footer>
      </section></div>
  </main>
</template>

<style scoped>
.adjust-log-page { height: 100%; min-height: 0; box-sizing: border-box; display: flex; flex-direction: column; gap: 8px; overflow: auto; padding: 10px 12px 14px; background: #f7f5ee; color: #26251f; font: inherit; }
.page-head, .panel-title, .query-actions, .dialog header, .dialog footer, .read-only-note { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.page-head { flex: 0 0 auto; padding: 2px 2px 0; }
.page-head h1 { margin: 2px 0; font-size: 21px; }
.eyebrow { margin: 0; color: #777366; font-size: 11px; letter-spacing: .08em; text-transform: uppercase; }
.sub, .panel-title span, .query-actions > span, .read-only-note { color: #777366; font-size: 12px; }
.sub { margin: 0; }
.close { border: 0; background: transparent; color: #555247; font-size: 24px; cursor: pointer; }
.notice { flex: 0 0 auto; margin: 0; padding: 7px 10px; border: 1px solid #d9d1b8; border-radius: 4px; background: #fffbed; color: #5b543f; font-size: 12px; }
.query-panel, .panel { border: 1px solid #d9d5c8; border-radius: 4px; background: #fff; box-shadow: 0 1px 2px #16130a0a; }
.query-panel { flex: 0 0 auto; display: grid; grid-template-columns: repeat(4, minmax(150px, 1fr)); align-items: end; gap: 10px 12px; padding: 10px; }
label { display: flex; flex-direction: column; gap: 4px; color: #5e5a4e; font-size: 12px; }
input, select { min-width: 0; box-sizing: border-box; min-height: 30px; border: 1px solid #ccc7b8; border-radius: 3px; background: #fff; color: #26251f; padding: 5px 7px; font: inherit; }
button { min-height: 29px; border: 1px solid #b9b3a3; border-radius: 3px; background: #f7f5ee; color: #302f29; padding: 4px 10px; font: inherit; cursor: pointer; }
button:hover:not(:disabled) { background: #ede9dc; }
button:disabled { cursor: not-allowed; opacity: .48; }
button.primary { border-color: #454237; background: #454237; color: #fff; }
.lookup-field { display: flex; gap: 5px; }
.lookup-field input { flex: 1; }
.query-actions { grid-column: 1 / -1; }
.query-actions > div { display: flex; gap: 6px; }
.state-strip { flex: 0 0 auto; display: flex; flex-wrap: wrap; gap: 16px; padding: 2px 4px; color: #777366; font-size: 12px; }
.state-strip strong { color: #37352e; font-size: 14px; }
.panel { flex: 1 1 0; display: flex; min-height: 140px; flex-direction: column; overflow: hidden; }
.panel-title { flex: 0 0 auto; min-height: 37px; padding: 6px 10px; border-bottom: 1px solid #e5e1d6; }
.panel-title h2 { display: inline-block; margin: 0 8px 0 0; font-size: 14px; }
.panel-title small { color: #777366; font-size: 11px; }
.table-wrap { min-height: 0; flex: 1 1 auto; overflow: auto; }
table { width: 100%; border-collapse: collapse; white-space: nowrap; font-size: 12px; }
th, td { padding: 6px 8px; border-bottom: 1px solid #ebe8df; text-align: left; }
th { position: sticky; top: 0; z-index: 1; background: #f1efe8; color: #5b584e; font-weight: 600; }
tbody tr { cursor: pointer; }
tbody tr:hover { background: #faf8f0; }
tbody tr.active { background: #e9f0e8; }
.mono { font-family: Consolas, 'Courier New', monospace; }
.num { text-align: right; font-variant-numeric: tabular-nums; }
.negative { color: #a04437; }
.positive { color: #3b6b45; }
.state { display: inline-block; border: 1px solid #d3c89e; border-radius: 9px; background: #f9f2d8; padding: 2px 8px; color: #63562b; }
.state-2 { border-color: #b3cfb0; background: #edf7eb; color: #37623a; }
.state-3 { border-color: #ddb5ad; background: #faece8; color: #91483e; }
.empty { height: 55px; color: #898578; text-align: center; }
.component-row td { background: #f8f7f2; }
.component-list { display: flex; flex-wrap: wrap; gap: 8px 18px; color: #625f53; }
.component-list strong { color: #36342e; }
.read-only-note { flex: 0 0 auto; min-height: 30px; padding: 4px 9px; border-top: 1px solid #e5e1d6; background: #fbfaf6; }
.source-note { flex: 0 0 auto; display: flex; gap: 10px; padding: 2px; color: #777366; font-size: 11px; }
.source-note strong { color: #4b493f; }
.backdrop { position: fixed; inset: 0; z-index: 20; display: grid; place-items: center; padding: 16px; background: #211f1980; }
.dialog { width: min(520px, 100%); max-height: min(85vh, 650px); overflow: auto; border: 1px solid #c9c3b3; border-radius: 5px; background: #fff; box-shadow: 0 18px 55px #17150f40; }
.dialog > header { padding: 11px 15px; border-bottom: 1px solid #e5e1d6; }
.dialog h2 { margin: 2px 0 0; font-size: 16px; }
.lookup-search { margin: 12px 15px; }
.lookup-results { max-height: 320px; overflow: auto; padding: 0 15px 12px; }
.lookup-results button { display: flex; width: 100%; min-height: 38px; align-items: center; justify-content: space-between; margin-bottom: 5px; text-align: left; }
.lookup-results button.active { border-color: #68735a; background: #e9f0e8; }
.lookup-results span { color: #777366; }
.dialog footer { padding: 10px 15px; border-top: 1px solid #e5e1d6; }
@media (max-width: 900px) { .query-panel { grid-template-columns: repeat(2, minmax(140px, 1fr)); } }
@media (max-width: 560px) { .query-panel { grid-template-columns: 1fr; } .query-actions { align-items: flex-start; flex-direction: column; } .state-strip { gap: 8px 12px; } }
</style>
