<script setup lang="ts">
import { computed, ref } from 'vue'

type StateCode = -1 | 0 | 1 | 2 | 3
interface Inventory { inventoryNo: string; inventoryName: string }
interface ComponentRow { componentNo: string; goodQty: number; scrapQty: number; unitNo: string; minLifeGrade: string }
interface DetailRow { semiInNo: string; productNo: string; productName: string; lotNo: string; qty: number; unitNo: string; locatorNo: string; components: ComponentRow[] }
interface BasisRow { semiInNo: string; inventoryNo: string; state: StateCode; inputDate: string; description: string; creator: string; createDate: string; reviser: string; reviseDate: string }
interface Criteria { semiInNo: string; inventoryNo: string; state: string; fromCreateDate: string; endCreateDate: string; fromInputDate: string; endInputDate: string }

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: true })
const emit = defineEmits<{ close: [] }>()
const previewLabel = computed(() => props.developmentPreview ? 'DEV 假資料預覽' : 'DEV 模擬')
const inventories: Inventory[] = [
  { inventoryNo: 'SEMI-DEV-01', inventoryName: '半成品庫 A' },
  { inventoryNo: 'SEMI-DEV-02', inventoryName: '半成品庫 B' },
  { inventoryNo: 'SEMI-DEV-03', inventoryName: '半成品庫 C' },
]
const stateLabels: Record<StateCode, string> = { '-1': 'Scrap', 0: 'UnConfirm', 1: 'Handling', 2: 'Confirm', 3: 'Fail' }
const fixtureBasis: BasisRow[] = [
  { semiInNo: 'SI-DEV-260001', inventoryNo: 'SEMI-DEV-01', state: 2, inputDate: '2026-11-18 10:35:12', description: '外購控制板進料', creator: 'DEV001', createDate: '2026-11-18 09:12:00', reviser: 'DEV001', reviseDate: '2026-11-18 10:35:12' },
  { semiInNo: 'SI-DEV-260002', inventoryNo: 'SEMI-DEV-02', state: 0, inputDate: '', description: '未確認的外購感測器單', creator: 'DEV002', createDate: '2026-11-20 14:36:00', reviser: '', reviseDate: '' },
  { semiInNo: 'SI-DEV-260003', inventoryNo: 'SEMI-DEV-01', state: 1, inputDate: '', description: '進料確認處理中', creator: 'DEV001', createDate: '2026-11-21 08:20:00', reviser: '', reviseDate: '' },
  { semiInNo: 'SI-DEV-260004', inventoryNo: 'SEMI-DEV-03', state: 3, inputDate: '2026-11-22 17:05:00', description: '確認失敗，等待處理', creator: 'DEV003', createDate: '2026-11-22 16:48:00', reviser: 'DEV003', reviseDate: '2026-11-22 17:05:00' },
  { semiInNo: 'SI-DEV-260005', inventoryNo: 'SEMI-DEV-02', state: -1, inputDate: '2026-11-23 12:15:00', description: '報廢狀態範例', creator: 'DEV002', createDate: '2026-11-23 11:56:00', reviser: 'DEV002', reviseDate: '2026-11-23 12:15:00' },
]
const fixtureDetails: DetailRow[] = [
  { semiInNo: 'SI-DEV-260001', productNo: 'PRD-CONTROL-10', productName: '控制板總成', lotNo: 'SL-26021', qty: 2, unitNo: 'EA', locatorNo: 'S-A-01', components: [{ componentNo: 'CMP-PCB-01', goodQty: 1, scrapQty: 0, unitNo: 'EA', minLifeGrade: 'A' }, { componentNo: 'CMP-CONN-02', goodQty: 1, scrapQty: 0, unitNo: 'EA', minLifeGrade: '' }] },
  { semiInNo: 'SI-DEV-260001', productNo: 'PRD-CONTROL-10', productName: '控制板總成', lotNo: 'SL-26022', qty: 1, unitNo: 'EA', locatorNo: 'S-A-02', components: [] },
  { semiInNo: 'SI-DEV-260002', productNo: 'PRD-SENSOR-04', productName: '感測器模組', lotNo: 'VND-88421', qty: 48, unitNo: 'EA', locatorNo: 'N/A', components: [{ componentNo: 'CMP-SENSOR-11', goodQty: 1, scrapQty: 0, unitNo: 'EA', minLifeGrade: '' }] },
  { semiInNo: 'SI-DEV-260003', productNo: 'PRD-PANEL-07', productName: '顯示面板', lotNo: 'VND-88444', qty: 12, unitNo: 'PCS', locatorNo: 'S-A-04', components: [] },
  { semiInNo: 'SI-DEV-260004', productNo: 'PRD-VALVE-06', productName: '閥件模組', lotNo: 'VND-44109', qty: 6, unitNo: 'EA', locatorNo: 'S-C-03', components: [{ componentNo: 'CMP-VALVE-02', goodQty: 5, scrapQty: 1, unitNo: 'EA', minLifeGrade: 'B' }] },
]
const criteria = ref<Criteria>(blankCriteria())
const appliedCriteria = ref<Criteria | null>(null)
const activeNo = ref('')
const activeDetailKey = ref('')
const notice = ref('DEV 假資料：唯讀查詢，不呼叫 MES 或正式 API。設定條件後按 Query。')

const queriedBases = computed(() => appliedCriteria.value ? filterBasis(appliedCriteria.value) : [])
const activeBasis = computed(() => queriedBases.value.find((row) => row.semiInNo === activeNo.value) ?? queriedBases.value[0] ?? null)
const activeDetails = computed(() => activeBasis.value ? fixtureDetails.filter((row) => row.semiInNo === activeBasis.value?.semiInNo) : [])
const activeDetail = computed(() => activeDetails.value.find((row) => detailKey(row) === activeDetailKey.value) ?? activeDetails.value[0] ?? null)
const detailTotal = computed(() => activeDetails.value.reduce((sum, row) => sum + row.qty, 0))

function blankCriteria(): Criteria { return { semiInNo: '', inventoryNo: '', state: '', fromCreateDate: '', endCreateDate: '', fromInputDate: '', endInputDate: '' } }
function detailKey(row: DetailRow): string { return `${row.semiInNo}|${row.lotNo}|${row.locatorNo}` }
function dateMatches(value: string, from: string, end: string): boolean {
  if (!from && !end) return true
  if (!value) return false
  const day = value.slice(0, 10)
  return (!from || day >= from) && (!end || day <= end)
}
function filterBasis(query: Criteria): BasisRow[] {
  return fixtureBasis.filter((row) => {
    if (query.semiInNo && row.semiInNo !== query.semiInNo.trim()) return false
    if (query.inventoryNo && row.inventoryNo !== query.inventoryNo) return false
    if (query.state !== '' && String(row.state) !== query.state) return false
    if (!dateMatches(row.createDate, query.fromCreateDate, query.endCreateDate)) return false
    if (!dateMatches(row.inputDate, query.fromInputDate, query.endInputDate)) return false
    return true
  })
}
function runQuery(): void {
  const snapshot = { ...criteria.value, semiInNo: criteria.value.semiInNo.trim() }
  appliedCriteria.value = snapshot
  const rows = filterBasis(snapshot)
  activeNo.value = rows[0]?.semiInNo ?? ''
  activeDetailKey.value = ''
  notice.value = `DEV 假資料查詢完成：Source=0 外購進料單 ${rows.length} 筆。原 Core 未指定排序與分頁；此結果保留 fixture 順序。`
}
function resetQuery(): void {
  criteria.value = blankCriteria()
  appliedCriteria.value = null
  activeNo.value = ''
  activeDetailKey.value = ''
  notice.value = 'DEV 假資料：唯讀查詢，不呼叫 MES 或正式 API。設定條件後按 Query。'
}
function inventoryName(inventoryNo: string): string { return inventories.find((row) => row.inventoryNo === inventoryNo)?.inventoryName ?? inventoryNo }
function stateLabel(state: StateCode): string { return stateLabels[state] }
function formatQty(value: number): string { return new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 6 }).format(value) }
</script>

<template>
  <main class="semi-in-log" aria-label="半成品進料單記錄 DEV 預覽">
    <header class="page-head"><div><p class="eyebrow">INV / IV407 · {{ previewLabel }}</p><h1>半成品進料單記錄</h1><p class="sub">外購 Source=0 進料單唯讀查詢 · 不修改單據或庫存</p></div><button type="button" class="close" title="Close (X)" @click="emit('close')">×</button></header>
    <p class="notice" role="status">{{ notice }}</p>
    <section class="query-panel" aria-label="半成品進料單查詢條件">
      <div class="query-grid">
        <label>SEMIInNo<input v-model.trim="criteria.semiInNo" list="semi-in-log-numbers" autocomplete="off" placeholder="精確單號" @keydown.enter.prevent="runQuery" /><datalist id="semi-in-log-numbers"><option v-for="row in fixtureBasis" :key="row.semiInNo" :value="row.semiInNo" /></datalist></label>
        <label>InventoryNo<select v-model="criteria.inventoryNo"><option value="">全部半成品庫房</option><option v-for="row in inventories" :key="row.inventoryNo" :value="row.inventoryNo">{{ row.inventoryNo }} · {{ row.inventoryName }}</option></select></label>
        <label>State<select v-model="criteria.state"><option value="">全部狀態</option><option value="0">UnConfirm (0)</option><option value="1">Handling (1)</option><option value="2">Confirm (2)</option><option value="3">Fail (3)</option><option value="-1">Scrap (-1)</option></select></label>
        <fieldset class="date-filter"><legend>CreateDate</legend><label>From<input v-model="criteria.fromCreateDate" type="date" /></label><label>To<input v-model="criteria.endCreateDate" type="date" /></label></fieldset>
        <fieldset class="date-filter"><legend>ConfirmDate / InputDate</legend><label>From<input v-model="criteria.fromInputDate" type="date" /></label><label>To<input v-model="criteria.endInputDate" type="date" /></label></fieldset>
      </div>
      <footer class="query-actions"><span>固定 Source=0（外購）；日期可只設單邊，結束日包含當日。</span><div><button type="button" @click="resetQuery">Clear</button><button type="button" class="primary" @click="runQuery">Query</button></div></footer>
    </section>
    <section class="panel basis-panel" aria-label="半成品進料單記錄主檔">
      <header class="panel-title"><div><h2>SEMIIn Basis</h2><span>{{ activeBasis ? `${activeBasis.semiInNo} · ${inventoryName(activeBasis.inventoryNo)}` : '尚未選取單據' }}</span></div><small>{{ appliedCriteria ? `${queriedBases.length} 筆查詢結果` : '按 Query 載入結果' }}</small></header>
      <div class="table-wrap"><table><thead><tr><th>SEMIInNo</th><th>InventoryNo</th><th>State</th><th>ConfirmDate</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead><tbody>
        <tr v-for="row in queriedBases" :key="row.semiInNo" :class="{ active: activeBasis?.semiInNo === row.semiInNo }" @click="activeNo = row.semiInNo; activeDetailKey = ''"><td class="mono">{{ row.semiInNo }}</td><td>{{ row.inventoryNo }}</td><td><span class="state-badge" :class="`state-${row.state}`">{{ stateLabel(row.state) }}</span></td><td>{{ row.inputDate || '—' }}</td><td class="description">{{ row.description || '—' }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser || '—' }}</td><td>{{ row.reviseDate || '—' }}</td></tr>
        <tr v-if="!appliedCriteria"><td colspan="9" class="empty">設定條件後按 Query；未查詢時不載入主檔結果。</td></tr><tr v-else-if="!queriedBases.length"><td colspan="9" class="empty">沒有符合條件的外購半成品進料單。</td></tr>
      </tbody></table></div>
      <footer class="grid-footer"><span>{{ queriedBases.length }} 張單據</span><span>唯讀 · 不提供新增、修改或刪除</span></footer>
    </section>
    <section class="panel detail-panel" aria-label="進料單明細與 Component">
      <header class="panel-title"><div><h2>SEMIIn Detail</h2><span>{{ activeBasis?.semiInNo ?? '請先查詢並選取單據' }}</span></div><small>{{ activeDetails.length }} 筆 Lot · 合計 {{ formatQty(detailTotal) }}</small></header>
      <div class="table-wrap"><table><thead><tr><th>ProductNo</th><th>ProductName</th><th>LotNo</th><th>Qty</th><th>UnitNo</th><th>LocatorNo</th></tr></thead><tbody>
        <tr v-for="row in activeDetails" :key="detailKey(row)" :class="{ active: activeDetail && detailKey(activeDetail) === detailKey(row) }" @click="activeDetailKey = detailKey(row)"><td>{{ row.productNo }}</td><td>{{ row.productName }}</td><td class="mono">{{ row.lotNo }}</td><td class="numeric">{{ formatQty(row.qty) }}</td><td>{{ row.unitNo }}</td><td>{{ row.locatorNo }}</td></tr>
        <tr v-if="activeBasis && !activeDetails.length"><td colspan="6" class="empty">此進料單沒有明細。</td></tr><tr v-else-if="!activeBasis"><td colspan="6" class="empty">查詢並選取單頭後載入 Detail。</td></tr>
      </tbody></table></div>
      <div class="component-block"><header><strong>Component</strong><span>{{ activeDetail?.lotNo ?? '選取 Lot 查看 Component' }}</span></header><div class="table-wrap"><table><thead><tr><th>ComponentNo</th><th>GoodQty</th><th>ScrapQty</th><th>UnitNo</th><th>MinLifeGrade</th></tr></thead><tbody>
        <tr v-for="row in activeDetail?.components ?? []" :key="row.componentNo"><td class="mono">{{ row.componentNo }}</td><td class="numeric">{{ formatQty(row.goodQty) }}</td><td class="numeric">{{ formatQty(row.scrapQty) }}</td><td>{{ row.unitNo }}</td><td>{{ row.minLifeGrade || '—' }}</td></tr><tr v-if="activeDetail && !activeDetail.components.length"><td colspan="5" class="empty">此 Lot 沒有 Component 明細。</td></tr><tr v-else-if="!activeDetail"><td colspan="5" class="empty">尚未選取 Lot。</td></tr>
      </tbody></table></div></div>
      <footer class="grid-footer"><span>Component relation 依 SEMIInNo + LotNo 展開</span><span>DEV fixtures · local only</span></footer>
    </section>
  </main>
</template>

<style scoped>
.semi-in-log { height: 100%; min-height: 0; display: flex; flex-direction: column; gap: 10px; padding: 14px; box-sizing: border-box; overflow: auto; background: #f3f1e9; color: #26251f; font: inherit; }
.page-head, .panel-title, .query-actions, .query-actions > div, .component-block > header { display: flex; align-items: center; gap: 8px; }.page-head, .panel-title, .query-actions { justify-content: space-between; }.page-head h1, .panel-title h2 { margin: 0; font-weight: 600; }.page-head h1 { font-size: 20px; }.eyebrow { margin: 0 0 3px; color: #77715f; font-size: 10px; letter-spacing: .12em; }.sub, .panel-title span { margin: 4px 0 0; color: #6e6b60; font-size: 12px; }.close { border: 0; background: transparent; color: #625f54; font-size: 22px; cursor: pointer; }.notice { min-height: 16px; margin: 0; color: #365b43; font-size: 12px; }
.query-panel, .panel { border: 1px solid #cecabd; background: #fbfaf6; }.query-panel { padding: 9px 10px; }.query-grid { display: grid; grid-template-columns: repeat(3, minmax(160px, 1fr)); gap: 8px 10px; }.query-grid > label, .date-filter > label { display: flex; flex-direction: column; gap: 4px; color: #555246; font-size: 11px; }.query-grid input, .query-grid select { min-height: 27px; padding: 3px 7px; border: 1px solid #c7c3b7; border-radius: 3px; background: #fffefa; color: #26251f; font: inherit; }.date-filter { display: flex; align-items: end; gap: 8px; margin: 0; padding: 4px 7px 6px; border: 1px solid #d5d1c5; }.date-filter legend { padding: 0 4px; color: #555246; font-size: 11px; }.date-filter > label { flex: 1; }.date-filter input { min-width: 0; }.query-actions { margin-top: 8px; padding-top: 8px; border-top: 1px solid #e0ddd3; color: #716d60; font-size: 11px; }.query-actions button { min-height: 27px; padding: 4px 10px; border: 1px solid #aaa698; border-radius: 3px; background: #fffefa; color: #343229; font: inherit; cursor: pointer; }.query-actions .primary { border-color: #35342d; background: #35342d; color: white; }
.panel-title { min-height: 34px; padding: 5px 9px; border-bottom: 1px solid #dedbce; }.panel-title h2 { font-size: 14px; }.panel-title small { color: #706d62; }.basis-panel { flex: 1 1 42%; }.detail-panel { flex: 1 1 40%; }.table-wrap { overflow: auto; max-width: 100%; }table { width: 100%; border-collapse: collapse; white-space: nowrap; font-size: 11px; }th, td { padding: 6px 8px; border: 1px solid #e1ded3; text-align: left; }th { position: sticky; top: 0; z-index: 1; background: #efede4; color: #555246; font-weight: 600; }tbody tr { cursor: pointer; }tbody tr:hover { background: #f2f0e8; }tbody tr.active { background: #e8ecdf; }.mono { font-family: Consolas, monospace; }.numeric { text-align: right; }.description { max-width: 300px; overflow: hidden; text-overflow: ellipsis; }.empty { padding: 16px; text-align: center; color: #858173; }.state-badge { display: inline-block; padding: 2px 6px; border: 1px solid #d0cbbd; border-radius: 10px; color: #625e51; }.state-2 { border-color: #b8ccb2; background: #edf5e9; color: #365b43; }.state-3 { border-color: #dfb8b3; background: #fbefed; color: #913c35; }.state--1 { border-color: #d7c4c0; background: #eee7e4; color: #6f4b43; }.state-0 { color: #6e5928; }.grid-footer { display: flex; justify-content: space-between; gap: 8px; padding: 5px 9px; border-top: 1px solid #dedbce; color: #777366; font-size: 10px; }.component-block { border-top: 1px solid #dedbce; }.component-block > header { padding: 7px 9px 2px; font-size: 11px; }.component-block > header span { color: #777366; }
@media (max-width: 850px) { .query-grid { grid-template-columns: repeat(2, minmax(145px, 1fr)); }.query-actions { align-items: start; flex-direction: column; } }
</style>
