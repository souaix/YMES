<script setup lang="ts">
import { computed, ref } from 'vue'

type MaterialOutState = 0 | 1 | 2 | 3
type PutInPlaceType = 0 | 1 | 2 | 3 | 4
interface BasisRow {
  materialOutNo: string
  inventoryNo: string
  preOrderNo: string
  state: MaterialOutState
  confirmDate: string
  description: string
  creator: string
  createDate: string
  confirmUserNo: string
  transactionDates: string[]
}
interface DetailRow {
  materialOutNo: string
  materialNo: string
  materialLotNo: string
  locatorNo: string
  putInPlaceType: PutInPlaceType
  putInPlaceNo: string
  unitNo: string
  qty: number
}
interface ComponentRow {
  materialOutNo: string
  materialNo: string
  materialLotNo: string
  devSlotNo: string
  componentNo: string
  devCassetteNo: string
}
interface QueryCriteria {
  materialOutNo: string
  inventoryNo: string
  preOrderNo: string
  state: string
  fromCreateDate: string
  endCreateDate: string
  fromMaterialOutDate: string
  endMaterialOutDate: string
}

withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()

const inventories = [
  { inventoryNo: 'RAW-A', inventoryName: '一廠原物料倉' },
  { inventoryNo: 'RAW-B', inventoryName: '二廠原物料倉' },
]
const stateNames: Record<MaterialOutState, string> = { 0: 'UnConfirm', 1: 'Handling', 2: 'Confirm', 3: 'Fail' }
const putInPlaceNames: Record<PutInPlaceType, string> = { 0: 'EQP', 1: 'OP', 2: 'WIP INV', 3: 'MO', 4: 'Unproductive' }
const fixtureBases: BasisRow[] = [
  { materialOutNo: 'MOUT-DEV-26001', inventoryNo: 'RAW-A', preOrderNo: 'PRE-DEV-011', state: 2, confirmDate: '2026-11-21 10:35:12', description: 'DEV 假資料：已確認並留下出庫交易。', creator: 'DEV001', createDate: '2026-11-20 09:12:00', confirmUserNo: 'DEV002', transactionDates: ['2026-11-21 10:35:12'] },
  { materialOutNo: 'MOUT-DEV-26002', inventoryNo: 'RAW-B', preOrderNo: '', state: 0, confirmDate: '', description: 'DEV 假資料：尚未確認，沒有出庫交易日期。', creator: 'DEV002', createDate: '2026-11-22 14:36:00', confirmUserNo: '', transactionDates: [] },
  { materialOutNo: 'MOUT-DEV-26003', inventoryNo: 'RAW-A', preOrderNo: 'PRE-DEV-014', state: 3, confirmDate: '2026-11-23 16:22:40', description: 'DEV 假資料：交易失敗狀態。', creator: 'DEV003', createDate: '2026-11-23 15:58:00', confirmUserNo: 'DEV002', transactionDates: ['2026-11-23 16:22:40'] },
  { materialOutNo: 'MOUT-DEV-26004', inventoryNo: 'RAW-B', preOrderNo: '', state: 1, confirmDate: '', description: 'DEV 假資料：交易處理中。', creator: 'DEV001', createDate: '2026-11-24 08:04:00', confirmUserNo: '', transactionDates: ['2026-11-24 08:04:33'] },
]
const fixtureDetails: DetailRow[] = [
  { materialOutNo: 'MOUT-DEV-26001', materialNo: 'RM-RESIN-01', materialLotNo: 'RES-2611-001', locatorNo: 'A-01-01', putInPlaceType: 3, putInPlaceNo: 'MO-DEV-0042', unitNo: 'KG', qty: 18 },
  { materialOutNo: 'MOUT-DEV-26001', materialNo: 'RM-SOLVENT-02', materialLotNo: 'SOL-2611-020', locatorNo: 'A-01-02', putInPlaceType: 1, putInPlaceNo: 'OP-DEV-07', unitNo: 'L', qty: 6.5 },
  { materialOutNo: 'MOUT-DEV-26002', materialNo: 'RM-COPPER-03', materialLotNo: 'CU-2611-010', locatorNo: 'B-02-03', putInPlaceType: 2, putInPlaceNo: 'WIP-DEV-015', unitNo: 'RL', qty: 5 },
  { materialOutNo: 'MOUT-DEV-26003', materialNo: 'RM-RESIN-01', materialLotNo: 'RES-2611-ERR', locatorNo: 'A-02-01', putInPlaceType: 0, putInPlaceNo: 'EQP-DEV-12', unitNo: 'KG', qty: 2.5 },
  { materialOutNo: 'MOUT-DEV-26004', materialNo: 'RM-COPPER-03', materialLotNo: 'CU-2611-011', locatorNo: 'B-02-04', putInPlaceType: 4, putInPlaceNo: 'UNPRODUCTIVE', unitNo: 'RL', qty: 3 },
]
const fixtureComponents: ComponentRow[] = [
  { materialOutNo: 'MOUT-DEV-26001', materialNo: 'RM-RESIN-01', materialLotNo: 'RES-2611-001', devSlotNo: '01', componentNo: 'COMP-DEV-001', devCassetteNo: 'CAS-DEV-01' },
  { materialOutNo: 'MOUT-DEV-26001', materialNo: 'RM-RESIN-01', materialLotNo: 'RES-2611-001', devSlotNo: '02', componentNo: 'COMP-DEV-002', devCassetteNo: 'CAS-DEV-01' },
]

const criteria = ref<QueryCriteria>(blankCriteria())
const appliedCriteria = ref<QueryCriteria | null>(null)
const selectedBasisNo = ref('')
const lookupKind = ref<'materialOut' | 'inventory' | null>(null)
const notice = ref('DEV 假資料：唯讀查詢，不呼叫 MES 或正式 API。請設定條件後按 Query。')

const visibleBases = computed(() => appliedCriteria.value ? filterBases(appliedCriteria.value) : [])
const activeBasis = computed(() => visibleBases.value.find((row) => row.materialOutNo === selectedBasisNo.value) ?? null)
const visibleDetails = computed(() => activeBasis.value
  ? fixtureDetails.filter((row) => row.materialOutNo === activeBasis.value?.materialOutNo)
  : [])
const lookupOptions = computed(() => lookupKind.value === 'materialOut'
  ? fixtureBases.map((row) => ({ value: row.materialOutNo, label: row.materialOutNo }))
  : inventories.map((row) => ({ value: row.inventoryNo, label: `${row.inventoryNo} · ${row.inventoryName}` })))
const lookupTitle = computed(() => lookupKind.value === 'materialOut' ? 'Material Out No' : 'Inventory No')

function blankCriteria(): QueryCriteria {
  return { materialOutNo: '', inventoryNo: '', preOrderNo: '', state: '', fromCreateDate: '', endCreateDate: '', fromMaterialOutDate: '', endMaterialOutDate: '' }
}
function dateMatches(values: string[], from: string, end: string): boolean {
  if (!from && !end) return true
  if (values.length === 0) return false
  return values.some((value) => {
    const day = value.slice(0, 10)
    return (!from || day >= from) && (!end || day <= end)
  })
}
function filterBases(query: QueryCriteria): BasisRow[] {
  return fixtureBases.filter((row) => {
    if (query.materialOutNo && row.materialOutNo !== query.materialOutNo) return false
    if (query.inventoryNo && row.inventoryNo !== query.inventoryNo) return false
    if (query.preOrderNo && row.preOrderNo !== query.preOrderNo) return false
    if (query.state !== '' && String(row.state) !== query.state) return false
    if (!dateMatches([row.createDate], query.fromCreateDate, query.endCreateDate)) return false
    if (!dateMatches(row.transactionDates, query.fromMaterialOutDate, query.endMaterialOutDate)) return false
    return true
  })
}
function runQuery(): void {
  const snapshot = { ...criteria.value }
  const rows = filterBases(snapshot)
  appliedCriteria.value = snapshot
  selectedBasisNo.value = rows[0]?.materialOutNo ?? ''
  notice.value = `DEV 假資料查詢完成：${rows.length} 張領料單。`
}
function selectBasis(row: BasisRow): void { selectedBasisNo.value = row.materialOutNo }
function selectLookup(value: string): void {
  if (lookupKind.value === 'materialOut') criteria.value.materialOutNo = value
  if (lookupKind.value === 'inventory') criteria.value.inventoryNo = value
  lookupKind.value = null
}
function stateLabel(state: MaterialOutState): string { return stateNames[state] }
function putInPlaceLabel(type: PutInPlaceType): string { return putInPlaceNames[type] }
function componentsFor(row: DetailRow): ComponentRow[] {
  return fixtureComponents.filter((component) => component.materialOutNo === row.materialOutNo
    && component.materialNo === row.materialNo && component.materialLotNo === row.materialLotNo)
}
function detailKey(row: DetailRow, index: number): string {
  return `${row.materialOutNo}|${row.materialNo}|${row.materialLotNo}|${row.locatorNo}|${index}`
}
function qty(value: number): string { return new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 6 }).format(value) }
function inventoryName(inventoryNo: string): string { return inventories.find((row) => row.inventoryNo === inventoryNo)?.inventoryName ?? inventoryNo }
</script>

<template>
  <main class="material-out-log-page" aria-label="領料單記錄 DEV">
    <header class="page-heading">
      <div><p>INVENTORY · IV208</p><h1>Material Out Log <span>領料單記錄</span></h1><small>只讀瀏覽領料單與元件出庫歷程</small></div>
      <button type="button" class="close-button" @click="emit('close')">Close (X)</button>
    </header>

    <p class="notice" role="status">{{ notice }}</p>

    <form class="query-panel" aria-label="領料單記錄查詢條件" @submit.prevent="runQuery">
      <div class="query-grid">
        <label>Material Out No
          <span class="query-control"><input v-model="criteria.materialOutNo" list="material-out-log-numbers" autocomplete="off" placeholder="精確單號" /><button type="button" class="lookup-button" aria-label="查找領料單" @click="lookupKind = 'materialOut'">…</button></span>
          <datalist id="material-out-log-numbers"><option v-for="row in fixtureBases" :key="row.materialOutNo" :value="row.materialOutNo" /></datalist>
        </label>
        <label>Inventory No
          <span class="query-control"><input v-model="criteria.inventoryNo" list="material-out-log-inventories" autocomplete="off" placeholder="全部庫房" /><button type="button" class="lookup-button" aria-label="查找庫房" @click="lookupKind = 'inventory'">…</button></span>
          <datalist id="material-out-log-inventories"><option v-for="row in inventories" :key="row.inventoryNo" :value="row.inventoryNo">{{ row.inventoryName }}</option></datalist>
        </label>
        <label>PreOrder No<input v-model="criteria.preOrderNo" autocomplete="off" placeholder="精確預訂單號" /></label>
        <label>State
          <select v-model="criteria.state"><option value="">全部狀態</option><option value="0">UnConfirm</option><option value="1">Handling</option><option value="2">Confirm</option><option value="3">Fail</option></select>
        </label>
        <label>From CreateDate<input v-model="criteria.fromCreateDate" type="date" /></label>
        <label>End CreateDate<input v-model="criteria.endCreateDate" type="date" /></label>
        <label>From MaterialOut Date<input v-model="criteria.fromMaterialOutDate" type="date" /></label>
        <label>End MaterialOut Date<input v-model="criteria.endMaterialOutDate" type="date" /></label>
      </div>
      <div class="query-actions"><span>DEV fixtures · 查詢需按 Query 套用</span><button type="submit" class="primary">Query</button></div>
    </form>

    <section class="panel basis-panel" aria-label="領料單主檔查詢結果">
      <header class="panel-heading"><div><h2>MaterialOut Basis</h2><span>{{ visibleBases.length }} 筆查詢結果</span></div><small>{{ activeBasis ? `${activeBasis.materialOutNo} · ${inventoryName(activeBasis.inventoryNo)}` : '尚未選取單據' }}</small></header>
      <div class="table-wrap basis-table-wrap">
        <table><thead><tr><th>MaterialOutNo</th><th>InventoryNo</th><th>PreOrderNo</th><th>State</th><th>ConfirmDate</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>ConfirmUserNo</th></tr></thead>
          <tbody>
            <tr v-for="row in visibleBases" :key="row.materialOutNo" :class="{ active: activeBasis?.materialOutNo === row.materialOutNo }" @click="selectBasis(row)">
              <td class="mono">{{ row.materialOutNo }}</td><td>{{ row.inventoryNo }}</td><td>{{ row.preOrderNo || '—' }}</td><td><span class="state-badge" :class="`state-${row.state}`">{{ stateLabel(row.state) }}</span></td><td>{{ row.confirmDate || '—' }}</td><td class="description">{{ row.description || '—' }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.confirmUserNo || '—' }}</td>
            </tr>
            <tr v-if="!appliedCriteria"><td colspan="9" class="empty">輸入查詢條件後按 Query；未按 Query 時不載入主檔結果。</td></tr>
            <tr v-else-if="visibleBases.length === 0"><td colspan="9" class="empty">沒有符合條件的領料單。</td></tr>
          </tbody>
        </table>
      </div>
      <footer class="grid-footer"><span>{{ visibleBases.length }} 張單據</span><span>唯讀查詢 · 不提供新增、修改或刪除</span></footer>
    </section>

    <section class="panel detail-panel" aria-label="選取領料單的明細">
      <header class="panel-heading"><div><h2>MaterialOut Detail</h2><span>{{ activeBasis?.materialOutNo ?? '請先查詢並選取單據' }}</span></div><small>{{ visibleDetails.length }} 筆明細</small></header>
      <div class="table-wrap detail-table-wrap">
        <table><thead><tr><th>MaterialNo</th><th>MaterialLotNo</th><th>LocatorNo</th><th>PutInPlaceType</th><th>PutInPlaceNo</th><th>UnitNo</th><th class="numeric">Qty</th></tr></thead>
          <tbody>
            <template v-for="(row, index) in visibleDetails" :key="detailKey(row, index)">
              <tr class="detail-row">
                <td class="mono">{{ row.materialNo }}</td><td class="mono">{{ row.materialLotNo }}</td><td>{{ row.locatorNo }}</td><td>{{ putInPlaceLabel(row.putInPlaceType) }}</td><td>{{ row.putInPlaceNo }}</td><td>{{ row.unitNo }}</td><td class="numeric">{{ qty(row.qty) }}</td>
              </tr>
              <tr v-for="component in componentsFor(row)" :key="`${detailKey(row, index)}|${component.componentNo}`" class="component-row">
                <td colspan="3">元件批次</td><td>{{ component.devSlotNo }}</td><td class="mono">{{ component.componentNo }}</td><td colspan="2">{{ component.devCassetteNo }}</td>
              </tr>
            </template>
            <tr v-if="activeBasis && visibleDetails.length === 0"><td colspan="7" class="empty">此 DEV 單據沒有明細。</td></tr>
            <tr v-else-if="!activeBasis"><td colspan="7" class="empty">選取上方單據後載入明細。</td></tr>
          </tbody>
        </table>
      </div>
      <footer class="grid-footer"><span>{{ visibleDetails.length }} 筆明細</span><span>元件子列只顯示 Slot、Component、Cassette</span></footer>
    </section>

    <div v-if="lookupKind" class="lookup-backdrop" @click.self="lookupKind = null">
      <section class="lookup-dialog" role="dialog" aria-modal="true" :aria-label="`選擇${lookupTitle}`">
        <header><h2>{{ lookupTitle }}</h2><button type="button" aria-label="關閉" @click="lookupKind = null">×</button></header>
        <p>DEV 假資料查找</p>
        <button v-for="option in lookupOptions" :key="option.value" type="button" class="lookup-option" @click="selectLookup(option.value)">{{ option.label }}</button>
        <button type="button" class="lookup-cancel" @click="lookupKind = null">Close (X)</button>
      </section>
    </div>
  </main>
</template>

<style scoped>
.material-out-log-page { display: flex; flex-direction: column; gap: 12px; width: 100%; height: 100%; min-height: 0; padding: 14px; box-sizing: border-box; overflow: auto; background: #f1efe6; color: #28271f; font: inherit; }
.page-heading { display: flex; align-items: center; justify-content: space-between; gap: 16px; padding: 2px 2px 4px; }
.page-heading p { margin: 0 0 4px; color: #827d6c; font-size: 11px; font-weight: 800; letter-spacing: .12em; }
.page-heading h1 { margin: 0; font-size: 22px; line-height: 1.2; }
.page-heading h1 span { margin-left: 8px; color: #696655; font-size: 15px; font-weight: 500; }
.page-heading small { display: block; margin-top: 4px; color: #777366; }
.close-button, .query-actions button { min-height: 31px; padding: 5px 11px; border: 1px solid #c4c0b2; border-radius: 4px; background: #fffdf7; color: #39372e; font: inherit; cursor: pointer; }
.notice { margin: 0; padding: 8px 10px; border: 1px solid #d7d1bd; border-radius: 4px; background: #faf7ed; color: #625d4d; font-size: 12px; }
.query-panel, .panel { border: 1px solid #c9c5b7; border-radius: 5px; background: #fbfaf5; box-shadow: 0 1px 3px #302d2112; }
.query-panel { padding: 11px 12px 9px; }
.query-grid { display: grid; grid-template-columns: repeat(4, minmax(130px, 1fr)); gap: 9px 12px; }
.query-grid label { display: flex; flex-direction: column; gap: 4px; min-width: 0; color: #676353; font-size: 11px; font-weight: 700; }
.query-control { display: flex; min-width: 0; gap: 4px; }
.query-grid input, .query-grid select { width: 100%; min-width: 0; min-height: 32px; padding: 5px 7px; box-sizing: border-box; border: 1px solid #c9c5b7; border-radius: 3px; background: white; color: #2d2b23; font: inherit; font-size: 12px; }
.lookup-button { flex: 0 0 32px; min-height: 32px; border: 1px solid #c9c5b7; border-radius: 3px; background: #efede4; color: #39372e; font: inherit; cursor: pointer; }
.query-actions { display: flex; align-items: center; justify-content: space-between; gap: 12px; margin-top: 10px; color: #777366; font-size: 11px; }
.query-actions .primary { min-height: 31px; padding: 5px 14px; border: 1px solid #3c4c3b; border-radius: 4px; background: #3c4c3b; color: white; font: inherit; cursor: pointer; }
.panel { display: flex; flex-direction: column; min-height: 0; overflow: hidden; }
.basis-panel { flex: 1.15 1 230px; }
.detail-panel { flex: .85 1 175px; }
.panel-heading { display: flex; align-items: center; justify-content: space-between; gap: 12px; min-height: 43px; padding: 7px 10px; border-bottom: 1px solid #d8d4c7; background: #f7f5ee; }
.panel-heading h2 { margin: 0; font-size: 14px; }
.panel-heading span, .panel-heading small { color: #777366; font-size: 11px; }
.panel-heading span { margin-left: 8px; }
.table-wrap { flex: 1; min-height: 0; overflow: auto; }
table { width: 100%; border-collapse: collapse; white-space: nowrap; font-size: 12px; }
th { position: sticky; top: 0; z-index: 1; padding: 7px 9px; border-bottom: 1px solid #c9c5b7; background: #efede4; color: #625e50; text-align: left; font-weight: 700; }
td { padding: 7px 9px; border-bottom: 1px solid #ece9df; }
.basis-table-wrap tbody tr { cursor: pointer; }
tbody tr:hover { background: #f3f0e6; }
tbody tr.active { background: #e7eadf; }
.detail-panel tbody tr { cursor: default; }
.detail-row { background: #fffdf7; }
.component-row { background: #f4f1e8; color: #625e50; font-size: 11px; }
.component-row td:first-child { padding-left: 18px; }
.mono { font-variant-numeric: tabular-nums; font-family: ui-monospace, SFMono-Regular, Consolas, monospace; }
.numeric { text-align: right; font-variant-numeric: tabular-nums; }
.description { max-width: 260px; white-space: normal; }
.state-badge { display: inline-flex; min-width: 74px; justify-content: center; padding: 2px 6px; border-radius: 10px; background: #e6e3d8; color: #514f45; font-size: 10px; }
.state-0 { background: #f5e6c6; color: #77551d; }
.state-1 { background: #dfe8ef; color: #355b75; }
.state-2 { background: #deeadc; color: #355a37; }
.state-3 { background: #f0dedd; color: #7c3934; }
.empty { padding: 18px; color: #827d6c; text-align: center; }
.grid-footer { display: flex; justify-content: space-between; gap: 12px; padding: 7px 10px; border-top: 1px solid #e4e0d5; color: #777366; font-size: 10px; }
.lookup-backdrop { position: fixed; inset: 0; z-index: 50; display: grid; place-items: center; padding: 16px; background: #1f211dcc; }
.lookup-dialog { display: flex; flex-direction: column; gap: 8px; width: min(390px, 100%); max-height: min(540px, 90vh); overflow: auto; padding: 14px; border: 1px solid #d2cebf; border-radius: 6px; background: #fbfaf5; box-shadow: 0 16px 50px #0004; }
.lookup-dialog header { display: flex; align-items: center; justify-content: space-between; }
.lookup-dialog h2 { margin: 0; font-size: 15px; }
.lookup-dialog header button { width: 28px; height: 28px; border: 1px solid #c9c5b7; border-radius: 4px; background: white; cursor: pointer; }
.lookup-dialog p { margin: 0; color: #777366; font-size: 11px; }
.lookup-option, .lookup-cancel { min-height: 34px; padding: 7px 9px; border: 1px solid #ddd9cc; border-radius: 4px; background: white; color: #39372e; text-align: left; font: inherit; cursor: pointer; }
.lookup-option:hover { background: #e7eadf; }
.lookup-cancel { text-align: center; background: #efede4; }
@media (max-width: 850px) { .query-grid { grid-template-columns: repeat(2, minmax(120px, 1fr)); } }
@media (max-width: 560px) { .material-out-log-page { padding: 9px; } .query-grid { grid-template-columns: 1fr; } .page-heading h1 { font-size: 18px; } .page-heading h1 span { display: block; margin: 4px 0 0; } .grid-footer { flex-direction: column; gap: 3px; } }
</style>
