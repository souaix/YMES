<script setup lang="ts">
import { computed, ref } from 'vue'

type PreOrderState = 0 | 1 | 2 | 3 | 4
type LookupKind = 'preOrder' | 'inventory'
interface BasisRow {
  preOrderNo: string
  inventoryNo: string
  state: PreOrderState
  confirmDate: string
  description: string
  creator: string
  createDate: string
  invDates: string[]
}
interface DetailRow { preOrderNo: string; materialNo: string; unitNo: string; qty: number }
interface QueryCriteria {
  preOrderNo: string
  inventoryNo: string
  state: string
  fromCreateDate: string
  endCreateDate: string
  fromINVDate: string
  endINVDate: string
}

withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()

const stateNames: Record<PreOrderState, string> = { 0: 'UnConfirm', 1: 'Handling', 2: 'Confirm', 3: 'Fail', 4: 'Used' }
const states: PreOrderState[] = [0, 1, 2, 3, 4]
const fixtureBases: BasisRow[] = [
  { preOrderNo: 'PRE-DEV-1001', inventoryNo: 'RAW-A', state: 0, confirmDate: '', description: 'DEV 假資料：未確認草稿。', creator: 'DEV001', createDate: '2026-11-20 09:12:00', invDates: [] },
  { preOrderNo: 'PRE-DEV-1002', inventoryNo: 'RAW-B', state: 1, confirmDate: '', description: 'DEV 假資料：處理中。', creator: 'DEV002', createDate: '2026-11-21 14:36:00', invDates: ['2026-11-21 14:42:18'] },
  { preOrderNo: 'PRE-DEV-1003', inventoryNo: 'RAW-A', state: 2, confirmDate: '2026-11-22 09:45:30', description: 'DEV 假資料：已確認。', creator: 'DEV003', createDate: '2026-11-22 08:04:00', invDates: ['2026-11-22 09:45:30'] },
  { preOrderNo: 'PRE-DEV-1004', inventoryNo: 'RAW-B', state: 3, confirmDate: '', description: 'DEV 假資料：交易失敗。', creator: 'DEV001', createDate: '2026-11-23 15:58:00', invDates: ['2026-11-23 16:22:40'] },
  { preOrderNo: 'PRE-DEV-1005', inventoryNo: 'RAW-A', state: 4, confirmDate: '2026-11-23 10:11:06', description: 'DEV 假資料：已使用。', creator: 'DEV002', createDate: '2026-11-24 08:10:00', invDates: ['2026-11-23 10:11:06'] },
]
const fixtureDetails: DetailRow[] = [
  { preOrderNo: 'PRE-DEV-1001', materialNo: 'RM-RESIN-01', unitNo: 'KG', qty: 18 },
  { preOrderNo: 'PRE-DEV-1001', materialNo: 'RM-SOLVENT-02', unitNo: 'L', qty: 6.5 },
  { preOrderNo: 'PRE-DEV-1002', materialNo: 'RM-COPPER-03', unitNo: 'RL', qty: 12 },
  { preOrderNo: 'PRE-DEV-1003', materialNo: 'RM-RESIN-01', unitNo: 'KG', qty: 25.75 },
  { preOrderNo: 'PRE-DEV-1003', materialNo: 'RM-FILM-04', unitNo: 'M', qty: 240 },
  { preOrderNo: 'PRE-DEV-1004', materialNo: 'RM-SOLVENT-02', unitNo: 'L', qty: 4 },
  { preOrderNo: 'PRE-DEV-1005', materialNo: 'RM-COPPER-03', unitNo: 'RL', qty: 8.25 },
]
const inventories = [
  { inventoryNo: 'RAW-A', inventoryName: '一廠原物料倉' },
  { inventoryNo: 'RAW-B', inventoryName: '二廠原物料倉' },
]

function blankCriteria(): QueryCriteria {
  return { preOrderNo: '', inventoryNo: '', state: '', fromCreateDate: '', endCreateDate: '', fromINVDate: '', endINVDate: '' }
}
const criteria = ref<QueryCriteria>(blankCriteria())
const appliedCriteria = ref<QueryCriteria | null>(null)
const selectedPreOrderNo = ref('')
const lookupKind = ref<LookupKind | null>(null)
const notice = ref('DEV 假資料：唯讀查詢。INV Date 依 Source=4 預訂交易的 TransactionLog CreateDate。請設定條件後按 Query。')

const visibleBases = computed(() => appliedCriteria.value ? filterBases(appliedCriteria.value) : [])
const activeBasis = computed(() => visibleBases.value.find((row) => row.preOrderNo === selectedPreOrderNo.value) ?? null)
const visibleDetails = computed(() => activeBasis.value
  ? fixtureDetails.filter((row) => row.preOrderNo === activeBasis.value?.preOrderNo)
  : [])
const lookupOptions = computed(() => lookupKind.value === 'preOrder'
  ? fixtureBases.map((row) => ({ value: row.preOrderNo, label: row.preOrderNo }))
  : inventories.map((row) => ({ value: row.inventoryNo, label: `${row.inventoryNo} · ${row.inventoryName}` })))
const lookupTitle = computed(() => lookupKind.value === 'preOrder' ? 'PreOrder No' : 'Inventory No')

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
    if (query.preOrderNo && row.preOrderNo !== query.preOrderNo.trim()) return false
    if (query.inventoryNo && row.inventoryNo !== query.inventoryNo.trim()) return false
    if (query.state !== '' && row.state !== Number(query.state)) return false
    if (!dateMatches([row.createDate], query.fromCreateDate, query.endCreateDate)) return false
    if (!dateMatches(row.invDates, query.fromINVDate, query.endINVDate)) return false
    return true
  })
}
function runQuery(): void {
  appliedCriteria.value = { ...criteria.value }
  selectedPreOrderNo.value = filterBases(appliedCriteria.value)[0]?.preOrderNo ?? ''
  const count = filterBases(appliedCriteria.value).length
  notice.value = `DEV 假資料查詢完成：${count} 張預訂單。${count === 0 ? '沒有符合條件的單據。' : '選取單頭可載入明細。'}`
}
function clearQuery(): void {
  criteria.value = blankCriteria()
  appliedCriteria.value = null
  selectedPreOrderNo.value = ''
  notice.value = '條件已清除；按 Query 顯示 DEV 假資料。'
}
function selectBasis(row: BasisRow): void { selectedPreOrderNo.value = row.preOrderNo }
function chooseLookup(value: string): void {
  if (lookupKind.value === 'preOrder') criteria.value.preOrderNo = value
  else if (lookupKind.value === 'inventory') criteria.value.inventoryNo = value
  lookupKind.value = null
}
function stateLabel(state: PreOrderState): string { return stateNames[state] }
function qty(value: number): string { return new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 6 }).format(value) }
</script>

<template>
  <main class="preorder-log-page" aria-label="預訂單記錄 DEV">
    <header class="page-heading"><div><p>INVENTORY · IV210</p><h1>Material PreOrder Log <span>預訂單記錄</span></h1><small>唯讀查詢 · INV Date 依 Source=4 交易建立時間</small></div><button type="button" class="close-button" @click="emit('close')">Close (X)</button></header>
    <p class="notice" role="status">{{ notice }}</p>

    <section class="query-panel" aria-label="預訂單記錄查詢">
      <form class="query-form" @submit.prevent="runQuery">
        <label>PreOrder No <span class="query-control"><input v-model="criteria.preOrderNo" autocomplete="off" placeholder="精確單號" /><button type="button" class="lookup-button" aria-label="選擇預訂單" @click="lookupKind = 'preOrder'">…</button></span></label>
        <label>Inventory No <span class="query-control"><input v-model="criteria.inventoryNo" autocomplete="off" placeholder="精確庫房" /><button type="button" class="lookup-button" aria-label="選擇庫房" @click="lookupKind = 'inventory'">…</button></span></label>
        <label>State<select v-model="criteria.state"><option value="">全部</option><option v-for="state in states" :key="state" :value="String(state)">{{ state }} · {{ stateLabel(state) }}</option></select></label>
        <label>From CreateDate<input v-model="criteria.fromCreateDate" type="date" /></label>
        <label>End CreateDate<input v-model="criteria.endCreateDate" type="date" /></label>
        <label>From INV Date<input v-model="criteria.fromINVDate" type="date" /></label>
        <label>End INV Date<input v-model="criteria.endINVDate" type="date" /></label>
        <div class="query-actions"><button class="primary" type="submit">Query</button><button type="button" @click="clearQuery">Clear</button></div>
      </form>
      <small class="query-note">預訂日期只對 Source=4 的 TransactionLog CreateDate 篩選；不以 ConfirmDate 代替。起訖日含整日。</small>
    </section>

    <section class="panel basis-panel" aria-label="預訂單主檔結果">
      <header class="panel-heading"><div><h2>PreOrder Basis</h2><span>{{ visibleBases.length }} 張單據</span></div><small>{{ activeBasis?.preOrderNo ?? '尚未選取單據' }}</small></header>
      <div class="table-wrap basis-table-wrap"><table><thead><tr><th>PreOrderNo</th><th>InventoryNo</th><th>State</th><th>ConfirmDate</th><th>Description</th><th>Creator</th><th>CreateDate</th></tr></thead><tbody>
        <tr v-for="row in visibleBases" :key="row.preOrderNo" :class="{ active: activeBasis?.preOrderNo === row.preOrderNo }" @click="selectBasis(row)"><td class="mono">{{ row.preOrderNo }}</td><td>{{ row.inventoryNo }}</td><td><span class="state-badge" :class="`state-${row.state}`">{{ stateLabel(row.state) }}</span></td><td>{{ row.confirmDate || '—' }}</td><td class="description">{{ row.description || '—' }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td></tr>
        <tr v-if="visibleBases.length === 0"><td colspan="7" class="empty">{{ appliedCriteria ? '沒有符合條件的預訂單。' : '設定條件後按 Query。' }}</td></tr>
      </tbody></table></div>
      <footer class="grid-footer"><span>{{ visibleBases.length }} 張單據</span><span>唯讀 · Source=4 預訂記錄</span></footer>
    </section>

    <section class="panel detail-panel" aria-label="預訂單明細結果">
      <header class="panel-heading"><div><h2>PreOrder Detail</h2><span>{{ activeBasis?.preOrderNo ?? '請先查詢並選取單據' }}</span></div><small>{{ visibleDetails.length }} 筆明細</small></header>
      <div class="table-wrap detail-table-wrap"><table><thead><tr><th>MaterialNo</th><th>UnitNo</th><th class="numeric">Qty</th></tr></thead><tbody>
        <tr v-for="(row, index) in visibleDetails" :key="`${row.preOrderNo}-${row.materialNo}-${index}`"><td class="mono">{{ row.materialNo }}</td><td>{{ row.unitNo }}</td><td class="numeric">{{ qty(row.qty) }}</td></tr>
        <tr v-if="activeBasis && visibleDetails.length === 0"><td colspan="3" class="empty">此預訂單沒有 DEV 明細。</td></tr><tr v-else-if="!activeBasis"><td colspan="3" class="empty">查詢並選取上方預訂單後載入明細。</td></tr>
      </tbody></table></div>
      <footer class="grid-footer"><span>{{ visibleDetails.length }} 筆明細</span><span>Qty 顯示最多六位小數</span></footer>
    </section>

    <div v-if="lookupKind" class="backdrop" @click.self="lookupKind = null"><section class="dialog lookup-dialog" role="dialog" aria-modal="true" :aria-label="lookupTitle"><header><div><p>DEV LOOKUP</p><h2>{{ lookupTitle }}</h2></div><button type="button" class="icon" aria-label="關閉" @click="lookupKind = null">×</button></header><button v-for="option in lookupOptions" :key="option.value" type="button" class="lookup-option" @click="chooseLookup(option.value)">{{ option.label }}</button><p v-if="lookupOptions.length === 0" class="dialog-note">沒有可用的 DEV 選項。</p><footer><button type="button" @click="lookupKind = null">Close (X)</button></footer></section></div>
  </main>
</template>

<style scoped>
.preorder-log-page { display:flex; flex-direction:column; gap:10px; width:100%; height:100%; min-height:0; padding:14px; box-sizing:border-box; overflow:auto; background:#f1efe6; color:#28271f; font:inherit; }
.page-heading { display:flex; align-items:center; justify-content:space-between; gap:16px; }.page-heading p,.dialog header p { margin:0 0 4px; color:#827d6c; font-size:11px; font-weight:800; letter-spacing:.12em; }.page-heading h1 { margin:0; font-size:22px; }.page-heading h1 span { margin-left:8px; color:#696655; font-size:15px; font-weight:500; }.page-heading small { display:block; margin-top:4px; color:#777366; }
.close-button,button { min-height:31px; padding:5px 10px; border:1px solid #c4c0b2; border-radius:4px; background:#fffdf7; color:#39372e; font:inherit; cursor:pointer; }button:disabled { opacity:.45; cursor:not-allowed; }.notice { margin:0; padding:8px 10px; border:1px solid #d7d1bd; border-radius:4px; background:#faf7ed; color:#625d4d; font-size:12px; }
.query-panel,.panel { border:1px solid #c9c5b7; border-radius:5px; background:#fbfaf5; box-shadow:0 1px 3px #302d2112; }.query-panel { padding:10px 12px; }.query-form { display:grid; grid-template-columns:repeat(4,minmax(150px,1fr)); align-items:end; gap:9px 12px; }.query-form label { display:flex; min-width:0; flex-direction:column; gap:4px; color:#676353; font-size:11px; font-weight:700; }.query-form input,.query-form select { width:100%; min-width:0; min-height:32px; padding:5px 7px; box-sizing:border-box; border:1px solid #c9c5b7; border-radius:3px; background:white; color:#2d2b23; font:inherit; font-size:12px; }.query-control { display:flex; gap:4px; }.query-control input { flex:1; }.lookup-button { flex:0 0 35px; }.query-actions { display:flex; gap:6px; }.primary { border-color:#3c4c3b; background:#3c4c3b; color:white; }.query-note { display:block; margin-top:8px; color:#777366; font-size:10px; }
.panel { display:flex; flex-direction:column; min-height:0; overflow:hidden; }.basis-panel { flex:1.15 1 240px; }.detail-panel { flex:.85 1 170px; }.panel-heading { display:flex; align-items:center; justify-content:space-between; gap:12px; min-height:40px; padding:7px 10px; border-bottom:1px solid #d8d4c7; background:#f7f5ee; }.panel-heading h2 { display:inline; margin:0; font-size:14px; }.panel-heading span,.panel-heading small { color:#777366; font-size:11px; }.panel-heading span { margin-left:8px; }.table-wrap { flex:1; min-height:0; overflow:auto; }table { width:100%; border-collapse:collapse; white-space:nowrap; font-size:12px; }th { position:sticky; top:0; z-index:1; padding:7px 9px; border-bottom:1px solid #c9c5b7; background:#efede4; color:#625e50; text-align:left; font-weight:700; }td { padding:7px 9px; border-bottom:1px solid #ece9df; }tbody tr { cursor:pointer; }tbody tr:hover { background:#f3f0e6; }tbody tr.active { background:#e7eadf; }.mono { font-family:ui-monospace,SFMono-Regular,Consolas,monospace; }.numeric { text-align:right; font-variant-numeric:tabular-nums; }.description { max-width:260px; white-space:normal; }.state-badge { display:inline-flex; min-width:66px; justify-content:center; padding:2px 6px; border-radius:10px; background:#ece9df; color:#575344; font-size:10px; }.state-0 { background:#f5e6c6;color:#77551d; }.state-1 { background:#dce8f3;color:#315c7b; }.state-2 { background:#dcebdc;color:#2d6638; }.state-3 { background:#f3ddda;color:#863e36; }.state-4 { background:#e6e0ef;color:#59447a; }.empty { padding:18px; color:#827d6c; text-align:center; }.grid-footer { display:flex; justify-content:space-between; gap:12px; padding:7px 10px; border-top:1px solid #e4e0d5; color:#777366; font-size:10px; }
.backdrop { position:fixed; inset:0; z-index:50; display:grid; place-items:center; padding:16px; background:#1f211dcc; }.dialog { display:flex; flex-direction:column; gap:10px; width:min(380px,100%); max-height:90vh; overflow:auto; padding:15px; border:1px solid #d2cebf; border-radius:6px; background:#fbfaf5; box-shadow:0 16px 50px #0004; }.dialog header { display:flex; align-items:center; justify-content:space-between; }.dialog header h2 { margin:0; font-size:16px; }.dialog .icon { width:29px; min-height:29px; padding:2px; font-size:18px; }.dialog footer { display:flex; justify-content:flex-end; gap:8px; }.lookup-option { text-align:left; }.dialog-note { margin:0; color:#777366; font-size:11px; }
@media(max-width:900px) { .query-form { grid-template-columns:repeat(2,minmax(130px,1fr)); }.page-heading h1 { font-size:18px; }.panel-heading { align-items:flex-start; flex-direction:column; }.grid-footer { flex-direction:column; gap:3px; } }
@media(max-width:560px) { .query-form { grid-template-columns:1fr; } }
</style>
