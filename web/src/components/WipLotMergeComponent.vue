<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type Component = { componentNo: string; goodQty: number; scrapQty: number; unitNo: string; executeState: number; status: number }
type Lot = {
  lotNo: string; moNo: string; curQty: number; lotStamp: number; status: number
  productNo: string; processNo: string; processVersion: string; opNo: string; opName: string; nodeId: string
  brNo: string; unitNo: string; haveLevel: boolean; inCarrier: boolean; allowDiffMO: boolean
  components: Component[]
}
type MergeRecord = { refLotNo: string; newLotNo: string; sourceLots: string[]; lotStamps: number[]; componentNos: string[]; action: 0 | 1; nodeId: string; qty: number; savedAt: string }

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview !== false)
const storageKey = 'ymes-dev-mock:wip-lot-merge-component:records:v1'
const fixtures: Lot[] = [
  { lotNo: 'DEV-CMERGE-001', moNo: 'DEV-MO-A', curQty: 2, lotStamp: 21201, status: 1, productNo: 'DEV-PROD-C', processNo: 'DEV-PROC-C', processVersion: '01', opNo: 'DEV-OP-C10', opName: '元件作業站 C10', nodeId: 'DEV-NODE-C10', brNo: 'DEV-BR-C', unitNo: 'PCS', haveLevel: false, inCarrier: false, allowDiffMO: true, components: [
    { componentNo: 'DEV-CMP-101', goodQty: 1, scrapQty: 0, unitNo: 'PCS', executeState: 1, status: 1 },
    { componentNo: 'DEV-CMP-102', goodQty: 1, scrapQty: 0, unitNo: 'PCS', executeState: 1, status: 1 },
  ] },
  { lotNo: 'DEV-CMERGE-002', moNo: 'DEV-MO-A', curQty: 2, lotStamp: 21202, status: 1, productNo: 'DEV-PROD-C', processNo: 'DEV-PROC-C', processVersion: '01', opNo: 'DEV-OP-C10', opName: '元件作業站 C10', nodeId: 'DEV-NODE-C10', brNo: 'DEV-BR-C', unitNo: 'PCS', haveLevel: false, inCarrier: false, allowDiffMO: true, components: [
    { componentNo: 'DEV-CMP-201', goodQty: 2, scrapQty: 0, unitNo: 'PCS', executeState: 1, status: 1 },
  ] },
  { lotNo: 'DEV-CMERGE-003', moNo: 'DEV-MO-B', curQty: 1, lotStamp: 21203, status: 1, productNo: 'DEV-PROD-C', processNo: 'DEV-PROC-C', processVersion: '01', opNo: 'DEV-OP-C10', opName: '元件作業站 C10', nodeId: 'DEV-NODE-C10B', brNo: 'DEV-BR-C', unitNo: 'PCS', haveLevel: false, inCarrier: false, allowDiffMO: true, components: [
    { componentNo: 'DEV-CMP-301', goodQty: 1, scrapQty: 0, unitNo: 'PCS', executeState: 1, status: 1 },
  ] },
  { lotNo: 'DEV-CMERGE-CARRIER', moNo: 'DEV-MO-A', curQty: 1, lotStamp: 21204, status: 1, productNo: 'DEV-PROD-C', processNo: 'DEV-PROC-C', processVersion: '01', opNo: 'DEV-OP-C10', opName: '元件作業站 C10', nodeId: 'DEV-NODE-C10', brNo: 'DEV-BR-C', unitNo: 'PCS', haveLevel: false, inCarrier: true, allowDiffMO: true, components: [
    { componentNo: 'DEV-CMP-CAR', goodQty: 1, scrapQty: 0, unitNo: 'PCS', executeState: 1, status: 1 },
  ] },
  { lotNo: 'DEV-CMERGE-LOCKED', moNo: 'DEV-MO-LOCK', curQty: 1, lotStamp: 21205, status: 0, productNo: 'DEV-PROD-C', processNo: 'DEV-PROC-C', processVersion: '01', opNo: 'DEV-OP-C10', opName: '元件作業站 C10', nodeId: 'DEV-NODE-C10', brNo: 'DEV-BR-C', unitNo: 'PCS', haveLevel: false, inCarrier: false, allowDiffMO: false, components: [
    { componentNo: 'DEV-CMP-LOCK', goodQty: 1, scrapQty: 0, unitNo: 'PCS', executeState: 0, status: 0 },
  ] },
]
const routeOptions = [
  { processNo: 'DEV-PROC-C', processVersion: '01', opNo: 'DEV-OP-C20', nodeId: 'DEV-NODE-C20' },
  { processNo: 'DEV-PROC-C', processVersion: '01', opNo: 'DEV-OP-C30', nodeId: 'DEV-NODE-C30' },
]

const refInput = ref('')
const refLot = ref<Lot | null>(null)
const outputLotNo = ref('')
const candidates = ref<Lot[]>([])
const selectedLots = ref<string[]>([])
const activeLotNo = ref('')
const moFilters = ref<string[] | null>(null)
const moDraft = ref<string[]>([])
const action = ref<0 | 1>(0)
const targetRoute = ref<(typeof routeOptions)[number] | null>(null)
const records = ref<MergeRecord[]>([])
const submitted = ref(false)
const errorText = ref('')
const message = ref('請輸入參考批號，並從候選批選取要合併的元件批。')
const dialog = ref<'reference' | 'mo' | 'route' | 'nodeWarning' | null>(null)

const canEdit = computed(() => dev.value && refLot.value !== null && !submitted.value)
const visibleLots = computed(() => moFilters.value === null ? candidates.value : candidates.value.filter(item => moFilters.value?.includes(item.moNo)))
const selectedRows = computed(() => candidates.value.filter(item => selectedLots.value.includes(item.lotNo)))
const activeLot = computed(() => candidates.value.find(item => item.lotNo === activeLotNo.value) || null)
const activeComponents = computed(() => activeLot.value?.components || [])
const allMOs = computed(() => [...new Set(candidates.value.map(item => item.moNo))].sort())
const totalQty = computed(() => selectedRows.value.reduce((sum, item) => sum + item.curQty, 0))
const selectedComponentCount = computed(() => selectedRows.value.reduce((sum, item) => sum + item.components.length, 0))
const activeGoodQty = computed(() => activeComponents.value.reduce((sum, item) => sum + item.goodQty, 0))
const activeScrapQty = computed(() => activeComponents.value.reduce((sum, item) => sum + item.scrapQty, 0))

function loadRecords(): void {
  if (!dev.value) return
  try { const raw = JSON.parse(window.localStorage.getItem(storageKey) || '[]') as unknown; records.value = Array.isArray(raw) ? raw as MergeRecord[] : [] }
  catch { records.value = [] }
}
function clearSelection(): void {
  candidates.value = []; selectedLots.value = []; activeLotNo.value = ''; outputLotNo.value = ''
  moFilters.value = null; moDraft.value = []; action.value = 0; targetRoute.value = null
  submitted.value = false; dialog.value = null
}
function invalidateReference(): void {
  if (refLot.value && refInput.value.trim().toUpperCase() !== refLot.value.lotNo) {
    clearSelection(); refLot.value = null; message.value = '參考批號已變更，請重新查詢。'
  }
}
function eligible(reference: Lot, item: Lot): boolean {
  return !item.haveLevel && item.status === reference.status && item.productNo === reference.productNo &&
    item.opNo === reference.opNo && item.brNo === reference.brNo && item.unitNo === reference.unitNo &&
    (reference.allowDiffMO || item.moNo === reference.moNo)
}
function queryReference(): void {
  errorText.value = ''; clearSelection(); refLot.value = null
  if (!dev.value) { errorText.value = '此頁目前僅提供 DEV 假資料預覽。'; return }
  const source = fixtures.find(item => item.lotNo === refInput.value.trim().toUpperCase())
  if (!source) { errorText.value = '找不到 DEV 參考批；可試 DEV-CMERGE-001。'; return }
  if (records.value.some(record => record.sourceLots.includes(source.lotNo))) { errorText.value = '此 DEV 批次已被模擬併批。'; return }
  refLot.value = source; refInput.value = source.lotNo; outputLotNo.value = source.lotNo
  candidates.value = fixtures.filter(item => eligible(source, item) && !records.value.some(record => record.sourceLots.includes(item.lotNo)))
  selectedLots.value = [source.lotNo]; activeLotNo.value = source.lotNo
  message.value = `已載入 ${candidates.value.length} 筆含元件候選批；點選列可查看元件。`
}
function chooseReference(lotNo: string): void { refInput.value = lotNo; queryReference() }
function setSelected(lotNo: string, checked: boolean): void {
  if (!canEdit.value || lotNo === refLot.value?.lotNo) return
  selectedLots.value = checked ? [...new Set([...selectedLots.value, lotNo])] : selectedLots.value.filter(item => item !== lotNo)
}
function selectAll(): void {
  if (!canEdit.value) return
  selectedLots.value = (moFilters.value === null ? candidates.value : visibleLots.value).map(item => item.lotNo)
  if (refLot.value && !selectedLots.value.includes(refLot.value.lotNo)) selectedLots.value.push(refLot.value.lotNo)
}
function unselectAll(): void { if (canEdit.value && refLot.value) selectedLots.value = [refLot.value.lotNo] }
function openMO(): void { if (!canEdit.value) return; moDraft.value = moFilters.value ? [...moFilters.value] : [...allMOs.value]; dialog.value = 'mo' }
function applyMO(): void {
  if (!refLot.value) return
  const selected = new Set(moDraft.value); selected.add(refLot.value.moNo)
  moFilters.value = [...selected]
  const allowed = new Set(candidates.value.filter(item => selected.has(item.moNo)).map(item => item.lotNo))
  selectedLots.value = selectedLots.value.filter(item => allowed.has(item))
  if (!selectedLots.value.includes(refLot.value.lotNo)) selectedLots.value.push(refLot.value.lotNo)
  if (!visibleLots.value.some(item => item.lotNo === activeLotNo.value)) activeLotNo.value = refLot.value.lotNo
  dialog.value = null
}
function clearMO(): void { moFilters.value = null; dialog.value = null }
function changeAction(next: 0 | 1): void { action.value = next; if (next === 0) targetRoute.value = null }
function chooseRoute(option: (typeof routeOptions)[number]): void { targetRoute.value = option; dialog.value = null }

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
  if (selected.length < 2 || !selected.some(item => item.lotNo === source.lotNo)) { errorText.value = '請保留參考批並至少再選一筆批次。'; return }
  if (selected.some(item => item.curQty === 0)) { errorText.value = '選取批次 CurQty 不可為 0。'; return }
  if (selected.some(item => item.inCarrier)) { errorText.value = '選取批次已在載具上，請改用「併批處理_載具」。'; return }
  if (selected.some(item => !item.components.length)) { errorText.value = '含元件批必須有元件現況。'; return }
  if (!ackNodeDifference && selected.some(item => item.opNo === source.opNo && item.nodeId !== source.nodeId)) { dialog.value = 'nodeWarning'; return }
  let current: MergeRecord[]
  try { const raw = JSON.parse(window.localStorage.getItem(storageKey) || '[]') as unknown; current = Array.isArray(raw) ? raw as MergeRecord[] : [] }
  catch { errorText.value = '無法讀取 DEV 模擬狀態。'; return }
  if (current.some(record => record.newLotNo === newLotNo || selected.some(item => record.sourceLots.includes(item.lotNo)))) { errorText.value = 'Merge LotNo 或來源批次已被其他 DEV 併批使用。'; dialog.value = null; return }
  const componentNos = selected.flatMap(item => item.components.map(component => component.componentNo))
  if (new Set(componentNos).size !== componentNos.length) { errorText.value = '選取批次的元件編號重複。'; dialog.value = null; return }
  const record: MergeRecord = { refLotNo: source.lotNo, newLotNo, sourceLots: selected.map(item => item.lotNo), lotStamps: selected.map(item => item.lotStamp), componentNos, action: action.value, nodeId: targetRoute.value?.nodeId || '', qty: totalQty.value, savedAt: new Date().toISOString() }
  try { window.localStorage.setItem(storageKey, JSON.stringify([...current, record])) }
  catch { errorText.value = '瀏覽器無法儲存 DEV 模擬結果。'; dialog.value = null; return }
  records.value = [...current, record]; submitted.value = true; dialog.value = null
  message.value = `DEV 已模擬 ${selected.length} 批、${componentNos.length} 顆元件併為 ${newLotNo}，總量 ${totalQty.value}；未更新 MES。`
}
onMounted(loadRecords)
</script>

<template>
  <section class="wp212-page">
    <div v-if="dev" class="dev-banner">DEV 假資料預覽；參考批 DEV-CMERGE-001，載具拒絕案例 DEV-CMERGE-CARRIER。</div>
    <h1>併批處理_元件 <small>LOT MERGE_COMP · WP212</small></h1>
    <p v-if="errorText" class="notice error" role="alert">{{ errorText }}</p><p v-else class="notice" role="status">{{ message }}</p>
    <fieldset class="reference-frame"><legend>Reference Lot</legend>
      <div class="query-row"><label for="wp212-ref">RefLot No</label><input id="wp212-ref" v-model="refInput" class="mes-input editable" :disabled="!dev" @input="invalidateReference" @keydown.enter.prevent="queryReference"><button type="button" class="mes-button" :disabled="!dev" @click="dialog = 'reference'">...</button><button type="button" class="mes-button" :disabled="!dev" @click="queryReference">Query</button></div>
      <div class="grid-wrap reference-grid"><table><thead><tr><th>LotNo</th><th>CurQty</th><th>ProcessNo</th><th>OPNo</th><th>OPName</th><th>NodeID</th></tr></thead><tbody><tr v-if="refLot"><td>{{ refLot.lotNo }}</td><td class="number">{{ refLot.curQty }}</td><td>{{ refLot.processNo }}</td><td>{{ refLot.opNo }}</td><td>{{ refLot.opName }}</td><td>{{ refLot.nodeId }}</td></tr><tr v-else><td colspan="6" class="empty">請查詢參考批。</td></tr></tbody></table></div>
    </fieldset>
    <fieldset class="merge-frame"><legend>Merge Lot</legend>
      <div class="merge-controls"><button type="button" class="mes-button" :disabled="!canEdit" @click="selectAll">Select All</button><button type="button" class="mes-button" :disabled="!canEdit" @click="unselectAll">UnSelect All</button><label for="wp212-mo">MO No</label><input id="wp212-mo" :value="moFilters?.join(', ') ?? ''" class="mes-input" readonly><button type="button" class="mes-button" :disabled="!canEdit" @click="openMO">...</button></div>
      <div class="merge-grids"><div class="grid-wrap merge-grid"><table><thead><tr><th>Check</th><th>LotNo</th><th>CurQty</th><th>MONo</th><th>NodeID</th><th>CurUnitNo</th></tr></thead><tbody><tr v-for="item in visibleLots" :key="item.lotNo" :class="{ reference: item.lotNo === refLot?.lotNo, active: item.lotNo === activeLotNo }" @click="activeLotNo = item.lotNo"><td><input type="checkbox" :checked="selectedLots.includes(item.lotNo)" :disabled="!canEdit || item.lotNo === refLot?.lotNo" :aria-label="'選取 ' + item.lotNo" @click.stop @change="setSelected(item.lotNo, ($event.target as HTMLInputElement).checked)"></td><td>{{ item.lotNo }}</td><td class="number">{{ item.curQty }}</td><td>{{ item.moNo }}</td><td>{{ item.nodeId }}</td><td>{{ item.unitNo }}</td></tr><tr v-if="!visibleLots.length"><td colspan="6" class="empty">查詢參考批後顯示候選批。</td></tr></tbody></table></div>
        <div class="component-pane"><div class="pane-title">Lot's component <strong>{{ activeLotNo || '—' }}</strong></div><div class="grid-wrap component-grid"><table><thead><tr><th>ComponentNo</th><th>GoodQty</th><th>ScrapQty</th><th>UnitNo</th></tr></thead><tbody><tr v-for="item in activeComponents" :key="item.componentNo"><td>{{ item.componentNo }}</td><td class="number">{{ item.goodQty }}</td><td class="number">{{ item.scrapQty }}</td><td>{{ item.unitNo }}</td></tr><tr v-if="!activeComponents.length"><td colspan="4" class="empty">點選左側批次查看元件。</td></tr></tbody></table></div><div class="summary-row">GoodQty {{ activeGoodQty }} · ScrapQty {{ activeScrapQty }}</div></div></div>
      <div class="summary-row">已選 {{ selectedRows.length }} 批 · CurQty {{ totalQty }} · 元件 {{ selectedComponentCount }}</div>
    </fieldset>
    <fieldset class="process-frame"><legend>Transfer Type</legend><label><input type="radio" :checked="action === 0" :disabled="!canEdit" @change="changeAction(0)">Go</label><label><input type="radio" :checked="action === 1" :disabled="!canEdit" @change="changeAction(1)">Assign</label><label for="wp212-output">LotNo</label><input id="wp212-output" v-model="outputLotNo" class="mes-input editable" :disabled="!canEdit"><label>Process No <input :value="targetRoute?.processNo || ''" class="mes-input" readonly></label><label>Version <input :value="targetRoute?.processVersion || ''" class="mes-input" readonly></label><label>OP No <input :value="targetRoute?.opNo || ''" class="mes-input" readonly></label><button type="button" class="mes-button" :disabled="!canEdit || action !== 1" @click="dialog = 'route'">...</button></fieldset>
    <div class="rule-text">Required Merge Rule: Same OPNo, Same ProductNo, Same BRNo, Same Status, Same CurUnitNo</div>
    <footer class="form-footer"><button type="button" class="mes-button confirm" :disabled="!canEdit" @click="confirmMerge()">Confirm(&amp;C)</button><button type="button" class="mes-button" @click="emit('close')">Close(&amp;X)</button></footer>
    <div v-if="dialog" class="modal-backdrop" @click.self="dialog = null">
      <section v-if="dialog === 'reference'" class="dialog" role="dialog" aria-modal="true" aria-label="Reference Lot"><header><strong>Reference Lot</strong></header><button v-for="item in fixtures" :key="item.lotNo" type="button" class="option" @click="chooseReference(item.lotNo)">{{ item.lotNo }}<small>{{ item.moNo }} · Qty {{ item.curQty }} · Status {{ item.status }}</small></button><footer><button type="button" class="mes-button" @click="dialog = null">Close</button></footer></section>
      <section v-else-if="dialog === 'mo'" class="dialog" role="dialog" aria-modal="true" aria-label="MO No"><header><strong>MO No</strong><span>參考批工單固定保留</span></header><label v-for="mo in allMOs" :key="mo" class="mo-option"><input v-model="moDraft" type="checkbox" :value="mo" :disabled="mo === refLot?.moNo">{{ mo }}</label><footer><button type="button" class="mes-button confirm" @click="applyMO">確定</button><button type="button" class="mes-button" @click="clearMO">清除篩選</button><button type="button" class="mes-button" @click="dialog = null">取消</button></footer></section>
      <section v-else-if="dialog === 'route'" class="dialog" role="dialog" aria-modal="true" aria-label="Assign Routing"><header><strong>Assign Routing</strong></header><button v-for="option in routeOptions" :key="option.nodeId" type="button" class="option" @click="chooseRoute(option)">{{ option.processNo }} / {{ option.processVersion }} / {{ option.opNo }}<small>{{ option.nodeId }}</small></button><footer><button type="button" class="mes-button" @click="dialog = null">Close</button></footer></section>
      <section v-else class="dialog" role="dialog" aria-modal="true" aria-label="Node ID 不同"><header><strong>相同作業站，不同 NodeID</strong></header><p>選取批次與參考批的 OP No 相同，NodeID 不同。仍要執行 DEV 元件併批嗎？</p><footer><button type="button" class="mes-button confirm" @click="confirmMerge(true)">繼續</button><button type="button" class="mes-button" @click="dialog = null">取消</button></footer></section>
    </div>
  </section>
</template>

<style scoped>
.wp212-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:7px;padding:9px;box-sizing:border-box;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif}.dev-banner{padding:6px 9px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}h1{margin:0;font-size:18px;font-weight:600}h1 small{margin-left:8px;color:#777;font-size:12px;font-weight:400}.notice{margin:0;min-height:18px;color:#555}.notice.error{padding:5px 8px;border:1px solid #d6aaa6;background:#fff0ef;color:#99251e}.reference-frame,.merge-frame,.process-frame{min-width:0;margin:0;padding:5px 8px 8px;box-sizing:border-box;border:1px solid #bdbdbd;background:#fff}legend{padding:0 5px}.reference-frame{height:142px;flex:0 0 142px}.merge-frame{flex:1;min-height:300px;display:flex;flex-direction:column}.query-row,.merge-controls,.process-frame{display:flex;align-items:center;gap:8px;flex-wrap:wrap}.query-row{min-height:40px}.query-row label{width:100px;text-align:right}.mes-input{height:26px;padding:3px 6px;border:1px solid #888;border-radius:2px;background:#fff;color:#333;font:inherit;box-sizing:border-box}.query-row .mes-input{width:190px}.editable{background:#fff0b3}.mes-button{min-height:29px;padding:3px 10px;border:1px solid #b9b9b9;border-radius:3px;background:linear-gradient(#fff,#f3f3f3);color:#333;font:inherit;white-space:nowrap;cursor:pointer}.mes-button:hover:not(:disabled){background:#fff5dc}.mes-button:disabled{color:#999;cursor:default}.confirm{color:#fff;background:#f3a62a;border-color:#e99512}.confirm:hover:not(:disabled){background:#e99a1b}.grid-wrap{min-width:0;overflow:auto;border:1px solid #a7a7a7;background:#fff}.reference-grid{height:82px}.merge-controls{min-height:40px}.merge-controls input{width:165px}.merge-grids{display:grid;grid-template-columns:minmax(0,1.55fr) minmax(270px,1fr);gap:8px;flex:1;min-height:0}.merge-grid{min-height:210px}.component-pane{min-width:0;display:flex;flex-direction:column;min-height:0}.pane-title{padding:5px 6px;border:1px solid #bbb;border-bottom:0;background:#eee}.pane-title strong{font-weight:500;margin-left:8px}.component-grid{flex:1}.grid-wrap table{width:100%;border-collapse:collapse;white-space:nowrap}.grid-wrap th{position:sticky;top:0;z-index:1;padding:5px 7px;border:1px solid #d4d4d4;background:#ead591;color:#393326;font-weight:500;text-align:left}.grid-wrap td{padding:4px 6px;border:1px solid #d2d2d2;background:#ffe89c}.grid-wrap tr.reference td{background:#ffea78}.grid-wrap tr.active td{outline:1px solid #b17c1b;outline-offset:-1px}.grid-wrap tbody tr:not(.empty){cursor:pointer}.number{text-align:right;font-variant-numeric:tabular-nums}.empty{text-align:center!important;background:#fff!important;color:#777!important;height:40px}.summary-row{min-height:22px;padding:4px 6px;border:1px solid #c5c5c5;background:#eee;text-align:right;color:#555;box-sizing:border-box}.process-frame{min-height:80px;flex:0 0 auto}.process-frame label{display:flex;align-items:center;gap:4px}.process-frame label .mes-input{width:100px}.process-frame #wp212-output{width:155px}.rule-text{padding:3px 8px;color:#6c5a3a}.form-footer{min-height:36px;display:flex;justify-content:flex-end;align-items:center;gap:8px}.modal-backdrop{position:fixed;inset:0;z-index:1300;display:grid;place-items:center;padding:18px;background:#0005}.dialog{width:min(600px,94vw);max-height:84vh;overflow:auto;display:flex;flex-direction:column;gap:9px;padding:14px;border:1px solid #999;background:#fff;box-shadow:0 8px 28px #0004;box-sizing:border-box}.dialog header{display:flex;justify-content:space-between;gap:12px}.dialog header span{color:#666}.dialog footer{display:flex;justify-content:flex-end;gap:8px}.dialog p{margin:0}.option{display:flex;flex-direction:column;gap:4px;padding:9px;border:1px solid #ccc;background:#fff;text-align:left;color:#333;font:inherit;cursor:pointer}.option:hover{background:#fff3cc}.option small{color:#666}.mo-option{display:flex;align-items:center;gap:8px;padding:5px}@media(max-width:860px){.merge-grids{grid-template-columns:1fr}.component-pane{min-height:160px}.reference-frame{height:135px;flex-basis:135px}}
</style>
