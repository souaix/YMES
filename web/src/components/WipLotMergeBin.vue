<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type Bin = { binNo: string; binOrder: number }
type Lot = {
  lotNo: string; moNo: string; curQty: number; lotStamp: number; status: number
  productNo: string; processNo: string; processVersion: string; opNo: string; nodeId: string
  brNo: string; unitNo: string; haveGoodBin: boolean; allowDiffMO: boolean; bins: Record<string, number>
}
type MergeRecord = { refLotNo: string; newLotNo: string; sourceLots: string[]; lotStamps: number[]; bins: Record<string, number>; action: 0 | 1; nodeId: string; electricBox: boolean; qty: number; savedAt: string }

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview !== false)
const storageKey = 'ymes-dev-mock:wip-lot-merge-bin:records:v1'
// DEV fixture only. Production Bin columns must come from wsSYS.LoadBin/TBLSYSBIN.
const binCatalog: Bin[] = [
  { binNo: 'BIN1', binOrder: 10 }, { binNo: 'BIN2', binOrder: 20 }, { binNo: 'BIN3', binOrder: 30 },
]
const fixtures: Lot[] = [
  { lotNo: 'DEV-BMERGE-001', moNo: 'DEV-MO-BIN-A', curQty: 5, lotStamp: 21301, status: 1, productNo: 'DEV-PROD-BIN', processNo: 'DEV-PROC-BIN', processVersion: '01', opNo: 'DEV-OP-BIN', nodeId: 'DEV-NODE-BIN-A', brNo: 'DEV-BR-BIN', unitNo: 'PCS', haveGoodBin: true, allowDiffMO: true, bins: { BIN1: 3, BIN2: 2, BIN3: 0 } },
  { lotNo: 'DEV-BMERGE-002', moNo: 'DEV-MO-BIN-A', curQty: 3, lotStamp: 21302, status: 1, productNo: 'DEV-PROD-BIN', processNo: 'DEV-PROC-BIN', processVersion: '01', opNo: 'DEV-OP-BIN', nodeId: 'DEV-NODE-BIN-A', brNo: 'DEV-BR-BIN', unitNo: 'PCS', haveGoodBin: true, allowDiffMO: true, bins: { BIN1: 0, BIN2: 1, BIN3: 2 } },
  { lotNo: 'DEV-BMERGE-003', moNo: 'DEV-MO-BIN-B', curQty: 2, lotStamp: 21303, status: 1, productNo: 'DEV-PROD-BIN', processNo: 'DEV-PROC-BIN', processVersion: '01', opNo: 'DEV-OP-BIN', nodeId: 'DEV-NODE-BIN-B', brNo: 'DEV-BR-BIN', unitNo: 'PCS', haveGoodBin: true, allowDiffMO: true, bins: { BIN1: 1, BIN2: 0, BIN3: 1 } },
  { lotNo: 'DEV-BMERGE-NOBIN', moNo: 'DEV-MO-BIN-A', curQty: 2, lotStamp: 21304, status: 1, productNo: 'DEV-PROD-BIN', processNo: 'DEV-PROC-BIN', processVersion: '01', opNo: 'DEV-OP-BIN', nodeId: 'DEV-NODE-BIN-A', brNo: 'DEV-BR-BIN', unitNo: 'PCS', haveGoodBin: false, allowDiffMO: true, bins: { BIN1: 0, BIN2: 0, BIN3: 0 } },
]
const routeOptions = [
  { processNo: 'DEV-PROC-BIN', processVersion: '01', opNo: 'DEV-OP-BIN-20', nodeId: 'DEV-NODE-BIN-20' },
  { processNo: 'DEV-PROC-BIN', processVersion: '01', opNo: 'DEV-OP-BIN-30', nodeId: 'DEV-NODE-BIN-30' },
]

const refInput = ref('')
const refLot = ref<Lot | null>(null)
const candidates = ref<Lot[]>([])
const selectedLots = ref<string[]>([])
const outputLotNo = ref('')
const action = ref<0 | 1>(0)
const targetRoute = ref<(typeof routeOptions)[number] | null>(null)
const electricBox = ref(false)
const submitted = ref(false)
const records = ref<MergeRecord[]>([])
const dialog = ref<'reference' | 'route' | 'nodeWarning' | 'electricBox' | null>(null)
const errorText = ref('')
const message = ref('請輸入含等級資料的參考批號。')

const orderedBins = computed(() => [...binCatalog].sort((a, b) => a.binOrder - b.binOrder || a.binNo.localeCompare(b.binNo)))
const canEdit = computed(() => dev.value && refLot.value !== null && !submitted.value)
const selectedRows = computed(() => candidates.value.filter(item => selectedLots.value.includes(item.lotNo)))
const totalQty = computed(() => selectedRows.value.reduce((sum, item) => sum + item.curQty, 0))
const binTotals = computed<Record<string, number>>(() => Object.fromEntries(orderedBins.value.map(bin => [bin.binNo, selectedRows.value.reduce((sum, item) => sum + (item.bins[bin.binNo] || 0), 0)])))

function loadRecords(): void {
  if (!dev.value) return
  try { const raw = JSON.parse(window.localStorage.getItem(storageKey) || '[]') as unknown; records.value = Array.isArray(raw) ? raw as MergeRecord[] : [] }
  catch { records.value = [] }
}
function clearSelection(): void {
  candidates.value = []; selectedLots.value = []; outputLotNo.value = ''
  action.value = 0; targetRoute.value = null; electricBox.value = false
  submitted.value = false; dialog.value = null
}
function invalidateReference(): void {
  if (refLot.value && refInput.value.trim().toUpperCase() !== refLot.value.lotNo) {
    clearSelection(); refLot.value = null; message.value = '參考批號已變更，請重新查詢。'
  }
}
function eligible(reference: Lot, item: Lot): boolean {
  return item.haveGoodBin && item.status === reference.status && item.productNo === reference.productNo &&
    item.opNo === reference.opNo && item.brNo === reference.brNo && item.unitNo === reference.unitNo &&
    (reference.allowDiffMO || item.moNo === reference.moNo)
}
function queryReference(): void {
  errorText.value = ''; clearSelection(); refLot.value = null
  if (!dev.value) { errorText.value = '此頁目前僅提供 DEV 假資料預覽。'; return }
  const source = fixtures.find(item => item.lotNo === refInput.value.trim().toUpperCase())
  if (!source) { errorText.value = '找不到 DEV 參考批；可試 DEV-BMERGE-001。'; return }
  if (!source.haveGoodBin) { errorText.value = '參考批沒有 GoodBinState，請使用其他併批入口。'; return }
  if (records.value.some(record => record.sourceLots.includes(source.lotNo))) { errorText.value = '此 DEV 批次已被模擬併批。'; return }
  refLot.value = source; refInput.value = source.lotNo; outputLotNo.value = source.lotNo
  candidates.value = fixtures.filter(item => eligible(source, item) && !records.value.some(record => record.sourceLots.includes(item.lotNo)))
  selectedLots.value = [source.lotNo]
  message.value = `已載入 ${candidates.value.length} 筆含 GoodBin 候選批；Bin 欄依目錄順序動態顯示。`
}
function chooseReference(lotNo: string): void { refInput.value = lotNo; queryReference() }
function setSelected(lotNo: string, checked: boolean): void {
  if (!canEdit.value || lotNo === refLot.value?.lotNo) return
  selectedLots.value = checked ? [...new Set([...selectedLots.value, lotNo])] : selectedLots.value.filter(item => item !== lotNo)
}
function selectAll(): void { if (canEdit.value) selectedLots.value = candidates.value.map(item => item.lotNo) }
function unselectAll(): void { if (canEdit.value && refLot.value) selectedLots.value = [refLot.value.lotNo] }
function changeAction(next: 0 | 1): void { action.value = next; if (next === 0) targetRoute.value = null }
function chooseRoute(option: (typeof routeOptions)[number]): void { targetRoute.value = option; dialog.value = null }
function bindElectricBox(): void { electricBox.value = true; dialog.value = null; message.value = 'DEV 靜電箱入口已模擬；未寫入 MES。' }

function confirmMerge(ackNodeDifference = false): void {
  errorText.value = ''
  const source = refLot.value
  if (!canEdit.value || !source) { errorText.value = '請先查詢參考批。'; return }
  const newLotNo = outputLotNo.value.trim().toUpperCase()
  if (!newLotNo) { errorText.value = 'Merge LotNo 不可空白。'; return }
  if (!newLotNo.startsWith('DEV-')) { errorText.value = 'DEV 預覽的新批號需以 DEV- 開頭。'; return }
  if (newLotNo !== source.lotNo && fixtures.some(item => item.lotNo === newLotNo)) { errorText.value = 'Merge LotNo 已存在。'; return }
  if (action.value === 1 && !targetRoute.value) { errorText.value = 'Assign 需要指定 Process、Version、OP 與 NodeID。'; return }
  const selected = selectedRows.value
  if (selected.length < 2 || !selected.some(item => item.lotNo === source.lotNo)) { errorText.value = '請保留參考批並至少再選一筆。'; return }
  if (selected.some(item => item.curQty === 0)) { errorText.value = '選取批次 CurQty 不可為 0。'; return }
  if (selected.some(item => orderedBins.value.some(bin => !Number.isInteger(item.bins[bin.binNo]) || item.bins[bin.binNo] < 0))) { errorText.value = 'DEV Bin 數量須為非負整數。'; return }
  if (!ackNodeDifference && selected.some(item => item.opNo === source.opNo && item.nodeId !== source.nodeId)) { dialog.value = 'nodeWarning'; return }
  let current: MergeRecord[]
  try { const raw = JSON.parse(window.localStorage.getItem(storageKey) || '[]') as unknown; current = Array.isArray(raw) ? raw as MergeRecord[] : [] }
  catch { errorText.value = '無法讀取 DEV 模擬狀態。'; return }
  if (current.some(record => record.newLotNo === newLotNo || selected.some(item => record.sourceLots.includes(item.lotNo)))) { errorText.value = 'Merge LotNo 或來源批次已被其他 DEV 併批使用。'; dialog.value = null; return }
  const record: MergeRecord = { refLotNo: source.lotNo, newLotNo, sourceLots: selected.map(item => item.lotNo), lotStamps: selected.map(item => item.lotStamp), bins: { ...binTotals.value }, action: action.value, nodeId: targetRoute.value?.nodeId || '', electricBox: electricBox.value, qty: totalQty.value, savedAt: new Date().toISOString() }
  try { window.localStorage.setItem(storageKey, JSON.stringify([...current, record])) }
  catch { errorText.value = '瀏覽器無法儲存 DEV 模擬結果。'; dialog.value = null; return }
  records.value = [...current, record]; submitted.value = true; dialog.value = null
  message.value = `DEV 已模擬 ${selected.length} 批併為 ${newLotNo}，CurQty ${totalQty.value}；未更新 MES。`
}
onMounted(loadRecords)
</script>

<template>
  <section class="wp213-page">
    <div v-if="dev" class="dev-banner">DEV 假資料預覽；參考批 DEV-BMERGE-001，無 GoodBin 案例 DEV-BMERGE-NOBIN。</div>
    <h1>併批處理_等級 <small>LOT MERGE_BIN · WP213</small></h1>
    <p v-if="errorText" class="notice error" role="alert">{{ errorText }}</p><p v-else class="notice" role="status">{{ message }}</p>
    <fieldset class="reference-frame"><legend>Reference Lot</legend><div class="query-row"><label for="wp213-ref">RefLot No</label><input id="wp213-ref" v-model="refInput" class="mes-input editable" :disabled="!dev" @input="invalidateReference" @keydown.enter.prevent="queryReference"><button type="button" class="mes-button" :disabled="!dev" @click="dialog = 'reference'">...</button><button type="button" class="mes-button" :disabled="!dev" @click="queryReference">Query</button></div><div class="grid-wrap reference-grid"><table><thead><tr><th>LotNo</th><th>CurQty</th><th>ProductNo</th><th>OPNo</th><th>NodeID</th><th>Status</th></tr></thead><tbody><tr v-if="refLot"><td>{{ refLot.lotNo }}</td><td class="number">{{ refLot.curQty }}</td><td>{{ refLot.productNo }}</td><td>{{ refLot.opNo }}</td><td>{{ refLot.nodeId }}</td><td>{{ refLot.status }}</td></tr><tr v-else><td colspan="6" class="empty">請查詢參考批。</td></tr></tbody></table></div></fieldset>
    <fieldset class="merge-frame"><legend>Merge Lot</legend><div class="merge-controls"><label for="wp213-output">LotNo</label><input id="wp213-output" v-model="outputLotNo" class="mes-input editable" :disabled="!canEdit"><button type="button" class="mes-button" :disabled="!canEdit" @click="selectAll">Select All</button><button type="button" class="mes-button" :disabled="!canEdit" @click="unselectAll">UnSelect All</button><button v-if="dev" type="button" class="mes-button" :disabled="!canEdit || !outputLotNo" @click="dialog = 'electricBox'">Eletricbox</button></div>
      <fieldset class="process-frame"><legend>Transfer Type</legend><label><input type="radio" :checked="action === 0" :disabled="!canEdit" @change="changeAction(0)">Go</label><label><input type="radio" :checked="action === 1" :disabled="!canEdit" @change="changeAction(1)">Assign</label><label>Process No <input :value="targetRoute?.processNo || ''" class="mes-input" readonly></label><label>Version <input :value="targetRoute?.processVersion || ''" class="mes-input" readonly></label><label>OP No <input :value="targetRoute?.opNo || ''" class="mes-input" readonly></label><button type="button" class="mes-button" :disabled="!canEdit || action !== 1" @click="dialog = 'route'">...</button></fieldset>
      <div class="grid-wrap merge-grid"><table><thead><tr><th>Check</th><th>LotNo</th><th>CurQty</th><th>NodeID</th><th v-for="bin in orderedBins" :key="bin.binNo">{{ bin.binNo }}</th></tr></thead><tbody><tr v-for="item in candidates" :key="item.lotNo" :class="{ reference: item.lotNo === refLot?.lotNo }"><td><input type="checkbox" :checked="selectedLots.includes(item.lotNo)" :disabled="!canEdit || item.lotNo === refLot?.lotNo" :aria-label="'選取 ' + item.lotNo" @change="setSelected(item.lotNo, ($event.target as HTMLInputElement).checked)"></td><td>{{ item.lotNo }}</td><td class="number">{{ item.curQty }}</td><td>{{ item.nodeId }}</td><td v-for="bin in orderedBins" :key="bin.binNo" class="number">{{ item.bins[bin.binNo] ?? 0 }}</td></tr><tr v-if="!candidates.length"><td :colspan="4 + orderedBins.length" class="empty">查詢參考批後顯示候選批與 Bin 數量。</td></tr></tbody><tfoot><tr><th>已選</th><th>{{ selectedRows.length }} 筆</th><th class="number">{{ totalQty }}</th><th>Bin 加總</th><th v-for="bin in orderedBins" :key="bin.binNo" class="number">{{ binTotals[bin.binNo] }}</th></tr></tfoot></table></div>
    </fieldset>
    <footer class="form-footer"><button type="button" class="mes-button confirm" :disabled="!canEdit" @click="confirmMerge()">Confirm(&amp;C)</button><button type="button" class="mes-button" @click="emit('close')">Close(&amp;X)</button></footer>
    <div v-if="dialog" class="modal-backdrop" @click.self="dialog = null"><section v-if="dialog === 'reference'" class="dialog" role="dialog" aria-modal="true" aria-label="Reference Lot"><header><strong>Reference Lot</strong></header><button v-for="item in fixtures" :key="item.lotNo" type="button" class="option" @click="chooseReference(item.lotNo)">{{ item.lotNo }}<small>{{ item.moNo }} · Qty {{ item.curQty }} · GoodBin {{ item.haveGoodBin ? '有' : '無' }}</small></button><footer><button type="button" class="mes-button" @click="dialog = null">Close</button></footer></section>
      <section v-else-if="dialog === 'route'" class="dialog" role="dialog" aria-modal="true" aria-label="Assign Routing"><header><strong>Assign Routing</strong></header><button v-for="option in routeOptions" :key="option.nodeId" type="button" class="option" @click="chooseRoute(option)">{{ option.processNo }} / {{ option.processVersion }} / {{ option.opNo }}<small>{{ option.nodeId }}</small></button><footer><button type="button" class="mes-button" @click="dialog = null">Close</button></footer></section>
      <section v-else-if="dialog === 'nodeWarning'" class="dialog" role="dialog" aria-modal="true" aria-label="Node ID 不同"><header><strong>相同作業站，不同 NodeID</strong></header><p>選取批次與參考批的 OP No 相同，NodeID 不同。仍要執行 DEV 等級併批嗎？</p><footer><button type="button" class="mes-button confirm" @click="confirmMerge(true)">繼續</button><button type="button" class="mes-button" @click="dialog = null">取消</button></footer></section>
      <section v-else class="dialog" role="dialog" aria-modal="true" aria-label="Eletricbox"><header><strong>Eletricbox</strong><span>{{ outputLotNo }}</span></header><p>此視窗只模擬合併目標批的靜電箱入口。</p><footer><button type="button" class="mes-button confirm" @click="bindElectricBox">模擬確認</button><button type="button" class="mes-button" @click="dialog = null">Close</button></footer></section></div>
  </section>
</template>

<style scoped>
.wp213-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:7px;padding:9px;box-sizing:border-box;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif}.dev-banner{padding:6px 9px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}h1{margin:0;font-size:18px;font-weight:600}h1 small{margin-left:8px;color:#777;font-size:12px;font-weight:400}.notice{margin:0;min-height:18px;color:#555}.notice.error{padding:5px 8px;border:1px solid #d6aaa6;background:#fff0ef;color:#99251e}.reference-frame,.merge-frame,.process-frame{min-width:0;margin:0;padding:5px 8px 8px;box-sizing:border-box;border:1px solid #bdbdbd;background:#fff}legend{padding:0 5px}.reference-frame{height:145px;flex:0 0 145px}.merge-frame{flex:1;min-height:260px;display:flex;flex-direction:column}.query-row,.merge-controls,.process-frame{display:flex;align-items:center;gap:8px;flex-wrap:wrap}.query-row{min-height:40px}.query-row label{width:100px;text-align:right}.mes-input{height:26px;padding:3px 6px;border:1px solid #888;border-radius:2px;background:#fff;color:#333;font:inherit;box-sizing:border-box}.query-row .mes-input{width:190px}.editable{background:#fff0b3}.mes-button{min-height:29px;padding:3px 10px;border:1px solid #b9b9b9;border-radius:3px;background:linear-gradient(#fff,#f3f3f3);color:#333;font:inherit;white-space:nowrap;cursor:pointer}.mes-button:hover:not(:disabled){background:#fff5dc}.mes-button:disabled{color:#999;cursor:default}.confirm{color:#fff;background:#f3a62a;border-color:#e99512}.confirm:hover:not(:disabled){background:#e99a1b}.grid-wrap{min-width:0;overflow:auto;border:1px solid #a7a7a7;background:#fff}.reference-grid{height:83px}.merge-controls{min-height:41px}.merge-controls .mes-input{width:180px}.merge-controls button:last-child{margin-left:auto}.process-frame{min-height:78px;margin-bottom:8px}.process-frame label{display:flex;align-items:center;gap:4px}.process-frame label .mes-input{width:100px}.merge-grid{flex:1;min-height:130px}.grid-wrap table{width:100%;border-collapse:collapse;white-space:nowrap}.grid-wrap th{position:sticky;top:0;z-index:1;padding:5px 7px;border:1px solid #d4d4d4;background:#ead591;color:#393326;font-weight:500;text-align:left}.grid-wrap td{padding:4px 6px;border:1px solid #d2d2d2;background:#ffe89c}.grid-wrap tr.reference td{background:#ffea78}.grid-wrap tfoot th{position:sticky;bottom:0;top:auto;background:#e8e4d7}.number{text-align:right;font-variant-numeric:tabular-nums}.empty{text-align:center!important;background:#fff!important;color:#777!important;height:40px}.form-footer{min-height:36px;display:flex;justify-content:flex-end;align-items:center;gap:8px}.modal-backdrop{position:fixed;inset:0;z-index:1300;display:grid;place-items:center;padding:18px;background:#0005}.dialog{width:min(600px,94vw);max-height:84vh;overflow:auto;display:flex;flex-direction:column;gap:9px;padding:14px;border:1px solid #999;background:#fff;box-shadow:0 8px 28px #0004;box-sizing:border-box}.dialog header{display:flex;justify-content:space-between;gap:12px}.dialog header span{color:#666}.dialog footer{display:flex;justify-content:flex-end;gap:8px}.dialog p{margin:0}.option{display:flex;flex-direction:column;gap:4px;padding:9px;border:1px solid #ccc;background:#fff;text-align:left;color:#333;font:inherit;cursor:pointer}.option:hover{background:#fff3cc}.option small{color:#666}@media(max-width:740px){.reference-frame{height:135px;flex-basis:135px}}
</style>
