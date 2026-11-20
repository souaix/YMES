<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'

type Component = { componentNo: string; goodQty: number; scrapQty: number }
type Lot = {
  lotNo: string; baseLotNo: string; lotSerial: string; areaNo: string; psNo: string; processNo: string
  opNo: string; nodeId: string; brNo: string; customerNo: string; productNo: string; status: number
  lotState: number; partialMode: boolean; packed: boolean; curQty: number; curUnitNo: string
  sysQty: number; sysUnitNo: string; lotStamp: number; components: Component[]; erfNos: string[]
}
type HoldItem = { mClassNo: string; sClassNo: string; holdItemNo: string; holdItemName: string; description: string; createERF: boolean; ocapNo: string; ocapVersion: string }
type Reason = { key: string; holdItemNo: string; holdItemName: string; qty: number | ''; description: string; componentNo: string; areaNo: string; goodQty: number; scrapQty: number; ocapNo: string; ocapVersion: string }
type ERF = { erfNo: string; lotNo: string; lotStamp: number; erfType: 2; mClassNo: string; sClassNo: string; holdDescription: string; reasons: Reason[]; createERF: boolean; releaseEQP: boolean; equipmentState: string; attachmentNames: string[]; createdAt: string; actor: string }
type MockState = { lots: Lot[]; erfs: ERF[]; nextSerial: number }
type LookupField = 'areaNo' | 'psNo' | 'opNo' | 'customerNo' | 'productNo'

const props = defineProps<{ developmentPreview?: boolean; currentUserNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview !== false)
const unavailableMessage = '立即異常服務尚未提供，暫無法查詢或操作。'
const storageKey = 'ymes-dev-mock:wip-immediate-hold:state:v1'
const multipleHoldEnabled = true // DEV fixture for the ERFMultipleHold=1 branch
const statuses: Record<number, string> = { 0: 'Queue', 1: 'Running', 3: 'Hold', 4: 'Complete' }
const fixtures: Lot[] = [
  { lotNo: 'DEV-HOLD-001', baseLotNo: 'DEV-BASE-HOLD-01', lotSerial: 'DEV-HS-101', areaNo: 'A01', psNo: 'PS-01', processNo: 'PROC-A', opNo: 'OP-TEST', nodeId: 'N10', brNo: 'BR-TEST', customerNo: 'CUS-A', productNo: 'PRD-A', status: 0, lotState: 1, partialMode: false, packed: false, curQty: 12, curUnitNo: 'PCS', sysQty: 12, sysUnitNo: 'PCS', lotStamp: 50101, components: [{ componentNo: 'DEV-HC-01', goodQty: 6, scrapQty: 0 }, { componentNo: 'DEV-HC-02', goodQty: 6, scrapQty: 0 }], erfNos: [] },
  { lotNo: 'DEV-HOLD-HELD', baseLotNo: 'DEV-BASE-HOLD-02', lotSerial: 'DEV-HS-102', areaNo: 'A01', psNo: 'PS-01', processNo: 'PROC-A', opNo: 'OP-TEST', nodeId: 'N10', brNo: 'BR-TEST', customerNo: 'CUS-A', productNo: 'PRD-A', status: 3, lotState: 1, partialMode: false, packed: false, curQty: 5, curUnitNo: 'PCS', sysQty: 5, sysUnitNo: 'PCS', lotStamp: 50102, components: [], erfNos: ['DEV-ERF-OLDER'] },
  { lotNo: 'DEV-HOLD-PACK', baseLotNo: 'DEV-BASE-HOLD-03', lotSerial: 'DEV-HS-103', areaNo: 'A02', psNo: 'PS-02', processNo: 'PROC-B', opNo: 'OP-PACK', nodeId: 'N20', brNo: 'BR-PACK', customerNo: 'CUS-B', productNo: 'PRD-B', status: 4, lotState: 1, partialMode: false, packed: true, curQty: 8, curUnitNo: 'PCS', sysQty: 8, sysUnitNo: 'PCS', lotStamp: 50103, components: [], erfNos: [] },
  { lotNo: 'DEV-HOLD-PARTIAL', baseLotNo: 'DEV-BASE-HOLD-04', lotSerial: 'DEV-HS-104', areaNo: 'A02', psNo: 'PS-02', processNo: 'PROC-B', opNo: 'OP-PACK', nodeId: 'N20', brNo: 'BR-PACK', customerNo: 'CUS-B', productNo: 'PRD-B', status: 1, lotState: 1, partialMode: true, packed: false, curQty: 4, curUnitNo: 'PCS', sysQty: 4, sysUnitNo: 'PCS', lotStamp: 50104, components: [], erfNos: [] },
]
const holdItems: HoldItem[] = [
  { mClassNo: 'M-QUALITY', sClassNo: 'S-VISUAL', holdItemNo: 'H-001', holdItemName: '外觀異常', description: '外觀檢查異常', createERF: true, ocapNo: '', ocapVersion: '' },
  { mClassNo: 'M-QUALITY', sClassNo: 'S-VISUAL', holdItemNo: 'H-002', holdItemName: '尺寸異常', description: '尺寸超出規格', createERF: true, ocapNo: '', ocapVersion: '' },
  { mClassNo: 'M-EQUIPMENT', sClassNo: 'S-EQP', holdItemNo: 'H-003', holdItemName: '設備異常', description: '設備點檢異常', createERF: true, ocapNo: 'OCAP-01', ocapVersion: '1' },
]
const state = ref<MockState>({ lots: [], erfs: [], nextSerial: 1 })
const filters = reactive({ areaNo: '', psNo: '', opNo: '', customerNo: '', productNo: '', lotNo: '' })
const lookupField = ref<LookupField | null>(null)
const rows = ref<Lot[]>([])
const searched = ref(false)
const activeLotNo = ref('')
const modalLot = ref<Lot | null>(null)
const mClassNo = ref('')
const sClassNo = ref('')
const selectedHoldItems = ref<string[]>([])
const holdDescription = ref('')
const collectBy = ref<'lot' | 'component'>('lot')
const selectedComponents = ref<string[]>([])
const area = ref('All')
const reasons = ref<Reason[]>([])
const selectedReasons = ref<string[]>([])
const releaseEQP = ref(false)
const equipmentState = ref('')
const attachmentNames = ref<string[]>([])
const errorText = ref('')
const modalError = ref('')
const message = ref('請輸入至少一項條件，按 Query 查詢可設定異常的生產批。')

const visibleItems = computed(() => holdItems.filter(item => item.mClassNo === mClassNo.value && item.sClassNo === sClassNo.value))
const mClasses = [...new Set(holdItems.map(item => item.mClassNo))]
const sClasses = computed(() => [...new Set(holdItems.filter(item => item.mClassNo === mClassNo.value).map(item => item.sClassNo))])
const lookupOptions = computed(() => lookupField.value ? [...new Set(state.value.lots.map(item => item[lookupField.value!]))].sort() : [])
const previewERFNo = computed(() => `DEV-ERF-${String(state.value.nextSerial).padStart(4, '0')}`)

function seed(): MockState { return { lots: fixtures.map(item => ({ ...item, components: item.components.map(comp => ({ ...comp })), erfNos: [...item.erfNos] })), erfs: [], nextSerial: 1 } }
function load(): void {
  if (!dev.value) return
  try {
    const data = JSON.parse(window.localStorage.getItem(storageKey) || 'null') as unknown
    if (data && typeof data === 'object' && 'lots' in data && 'erfs' in data && 'nextSerial' in data && Array.isArray(data.lots) && Array.isArray(data.erfs)) { state.value = data as MockState; return }
  } catch { /* use fixtures */ }
  state.value = seed()
}
function norm(value: string): string { return value.trim().toUpperCase() }
function chooseLookup(value: string): void { if (lookupField.value) filters[lookupField.value] = value; lookupField.value = null }
function query(): void {
  errorText.value = ''; lookupField.value = null
  if (!dev.value) { errorText.value = unavailableMessage; return }
  for (const key of Object.keys(filters) as (keyof typeof filters)[]) filters[key] = norm(filters[key])
  if (!Object.values(filters).some(Boolean)) { errorText.value = '請至少輸入一個查詢條件。'; return }
  const allowed = multipleHoldEnabled ? [0, 1, 3, 4] : [0, 1, 4]
  rows.value = state.value.lots.filter(item => allowed.includes(item.status) && item.lotState !== 0 && item.curQty > 0 && !(item.partialMode && item.status !== 0) &&
    (Object.keys(filters) as (keyof typeof filters)[]).every(key => !filters[key] || item[key] === filters[key]))
  searched.value = true; activeLotNo.value = rows.value[0]?.lotNo || ''
  message.value = `查得 ${rows.value.length} 筆生產批。`
}
function openSet(): void {
  errorText.value = ''
  const lot = rows.value.find(item => item.lotNo === activeLotNo.value)
  if (!dev.value || !lot) { errorText.value = '請先選擇一筆生產批。'; return }
  if (lot.packed) { errorText.value = '已包裝的生產批不可執行立即異常設定。'; return }
  if (lot.partialMode && lot.status !== 0) { errorText.value = 'Partial Lot 僅 Queue 可設定立即異常。'; return }
  modalLot.value = lot; mClassNo.value = ''; sClassNo.value = ''; selectedHoldItems.value = []
  holdDescription.value = ''; collectBy.value = 'lot'; selectedComponents.value = []; area.value = 'All'
  reasons.value = []; selectedReasons.value = []; releaseEQP.value = false; equipmentState.value = ''
  attachmentNames.value = []; modalError.value = ''
}
function changeMClass(): void { sClassNo.value = ''; selectedHoldItems.value = []; reasons.value = []; selectedReasons.value = [] }
function changeSClass(): void { selectedHoldItems.value = []; reasons.value = []; selectedReasons.value = [] }
function addReasons(): void {
  modalError.value = ''
  const lot = modalLot.value
  if (!lot || !selectedHoldItems.value.length) { modalError.value = '請在左側選擇至少一個 HoldItem。'; return }
  if (collectBy.value === 'component' && !selectedComponents.value.length) { modalError.value = '請選擇元件。'; return }
  const components: Component[] = lot.components.length ? (collectBy.value === 'lot' ? lot.components : lot.components.filter(item => selectedComponents.value.includes(item.componentNo))) : [{ componentNo: 'N/A', goodQty: 0, scrapQty: 0 }]
  const areas = lot.components.length ? (area.value === 'All' ? ['T1', 'T2', 'T3', 'T4', 'T5'] : [area.value]) : ['N/A']
  for (const item of visibleItems.value.filter(row => selectedHoldItems.value.includes(row.holdItemNo))) {
    for (const comp of components) for (const areaNo of areas) {
      const key = `${item.holdItemNo}|${comp.componentNo}|${areaNo}`
      if (reasons.value.some(row => row.key === key)) continue
      reasons.value.push({ key, holdItemNo: item.holdItemNo, holdItemName: item.holdItemName, qty: 1, description: item.description, componentNo: comp.componentNo, areaNo, goodQty: comp.goodQty, scrapQty: comp.scrapQty, ocapNo: item.ocapNo, ocapVersion: item.ocapVersion })
    }
  }
  selectedHoldItems.value = []; message.value = `已加入 ${reasons.value.length} 筆異常原因。`
}
function removeReasons(): void { reasons.value = reasons.value.filter(item => !selectedReasons.value.includes(item.key)); selectedReasons.value = [] }
function addFiles(event: Event): void { attachmentNames.value = Array.from((event.target as HTMLInputElement).files || []).map(file => file.name) }
function validQty(value: number | ''): boolean {
  const qty = Number(value)
  return value !== '' && Number.isFinite(qty) && qty > 0 && Math.abs(qty * 10000 - Math.round(qty * 10000)) < 0.000001
}
function confirmERF(): void {
  modalError.value = ''
  const lot = modalLot.value
  if (!dev.value || !lot) return
  if (!mClassNo.value || !sClassNo.value || !holdDescription.value.trim() || !reasons.value.length) { modalError.value = '請填主／次分類、異常說明，並加入至少一筆 HoldReason。'; return }
  if (reasons.value.some(item => !validQty(item.qty))) { modalError.value = '每筆原因 Qty 必須大於 0 且最多四位小數。'; return }
  const ocapCount = reasons.value.filter(item => item.ocapNo.trim()).length
  if (ocapCount > 0 && ocapCount < reasons.value.length) { modalError.value = 'OCAPNO 必須全部填寫或全部留空。'; return }
  if (releaseEQP.value && !equipmentState.value) { modalError.value = 'Release EQP 為 Yes 時，請選擇 Equipment State。'; return }
  let current: MockState
  try {
    const data = JSON.parse(window.localStorage.getItem(storageKey) || 'null') as unknown
    current = data && typeof data === 'object' && 'lots' in data && 'erfs' in data && 'nextSerial' in data && Array.isArray(data.lots) && Array.isArray(data.erfs) ? data as MockState : seed()
  } catch { modalError.value = '無法讀取 DEV 模擬狀態。'; return }
  const live = current.lots.find(item => item.lotNo === lot.lotNo)
  if (!live || live.lotStamp !== lot.lotStamp || live.status !== lot.status || live.opNo !== lot.opNo || live.packed || (live.partialMode && live.status !== 0)) { modalError.value = 'LotStamp／狀態已改變，請關閉並重新 Query。'; return }
  if (live.status === 3 && !multipleHoldEnabled) { modalError.value = '目前參數不允許已 Hold 的 Lot 再開單。'; return }
  const erfNo = `DEV-ERF-${String(current.nextSerial).padStart(4, '0')}`
  if (current.erfs.some(item => item.erfNo === erfNo)) { modalError.value = '異常單號已使用，請重新開啟表單。'; return }
  const createERF = reasons.value.some(reason => holdItems.find(item => item.holdItemNo === reason.holdItemNo)?.createERF)
  const record: ERF = { erfNo, lotNo: live.lotNo, lotStamp: live.lotStamp, erfType: 2, mClassNo: mClassNo.value, sClassNo: sClassNo.value, holdDescription: holdDescription.value.trim(), reasons: reasons.value.map(item => ({ ...item, qty: Number(item.qty) })), createERF, releaseEQP: releaseEQP.value, equipmentState: releaseEQP.value ? equipmentState.value : '', attachmentNames: [...attachmentNames.value], createdAt: new Date().toISOString(), actor: props.currentUserNo || 'DEV' }
  current.erfs.push(record); current.nextSerial += 1; live.erfNos.push(erfNo); live.status = 3; live.lotStamp += 1
  try { window.localStorage.setItem(storageKey, JSON.stringify(current)) }
  catch { modalError.value = '瀏覽器無法儲存 DEV 模擬結果。'; return }
  state.value = current; modalLot.value = null; query()
  message.value = `DEV 已模擬開立 ${erfNo}（${record.reasons.length} 筆原因），Lot 已 Hold；未更新 MES、未上傳附件或產生 PDF。`
}
onMounted(load)
watch(dev, enabled => { if (enabled) load(); else { rows.value = []; modalLot.value = null; searched.value = false } })
</script>

<template>
  <section class="wp501-page">
    <div v-if="dev" class="dev-banner">DEV 假資料預覽：輸入 A01、DEV-HOLD-001；DEV-HOLD-HELD 可示範多重 Hold，DEV-HOLD-PACK 可示範包裝拒絕。</div>
    <h1>立即異常設定 <small>IMMEDIATE HOLD SET · WP501</small></h1>
    <p v-if="!dev" class="notice" role="status">{{ unavailableMessage }}</p><p v-else-if="errorText" class="notice error" role="alert">{{ errorText }}</p><p v-else class="notice" role="status">{{ message }}</p>
    <fieldset class="query-frame"><legend>Query</legend><div class="query-grid"><label>Area No<input v-model="filters.areaNo" class="mes-input yellow" :disabled="!dev" @keydown.enter.prevent="query"><button type="button" class="lookup-button" :disabled="!dev" @click.prevent="lookupField = lookupField === 'areaNo' ? null : 'areaNo'">...</button></label><label>PS No<input v-model="filters.psNo" class="mes-input yellow" :disabled="!dev" @keydown.enter.prevent="query"><button type="button" class="lookup-button" :disabled="!dev" @click.prevent="lookupField = lookupField === 'psNo' ? null : 'psNo'">...</button></label><label>OP No<input v-model="filters.opNo" class="mes-input yellow" :disabled="!dev" @keydown.enter.prevent="query"><button type="button" class="lookup-button" :disabled="!dev" @click.prevent="lookupField = lookupField === 'opNo' ? null : 'opNo'">...</button></label><label>Customer No<input v-model="filters.customerNo" class="mes-input yellow" :disabled="!dev" @keydown.enter.prevent="query"><button type="button" class="lookup-button" :disabled="!dev" @click.prevent="lookupField = lookupField === 'customerNo' ? null : 'customerNo'">...</button></label><label>Product No<input v-model="filters.productNo" class="mes-input yellow" :disabled="!dev" @keydown.enter.prevent="query"><button type="button" class="lookup-button" :disabled="!dev" @click.prevent="lookupField = lookupField === 'productNo' ? null : 'productNo'">...</button></label><label>Lot No<input v-model="filters.lotNo" class="mes-input yellow" :disabled="!dev" @keydown.enter.prevent="query"></label><button type="button" class="mes-button" :disabled="!dev" @click="query">Query</button></div><div v-if="dev && lookupField" class="lookup-options"><strong>{{ lookupField }} 查找</strong><button v-for="option in lookupOptions" :key="option" type="button" @click="chooseLookup(option)">{{ option }}</button></div></fieldset>
    <fieldset class="lot-frame"><legend>Lot</legend><div class="grid-wrap"><table><thead><tr><th>LotNo</th><th>Status</th><th>LotSerial</th><th>AreaNo</th><th>PSNo</th><th>ProcessNo</th><th>OPNo</th><th>NodeID</th><th>BRNo</th><th>CurQTY</th><th>CurUnitNo</th><th>SysQTY</th><th>SysUnitNo</th><th>BaseLotNo</th></tr></thead><tbody><tr v-for="item in rows" :key="item.lotNo" :class="{ active: activeLotNo === item.lotNo }" @click="activeLotNo = item.lotNo" @dblclick="openSet"><td>{{ item.lotNo }}</td><td>{{ statuses[item.status] }}</td><td>{{ item.lotSerial }}</td><td>{{ item.areaNo }}</td><td>{{ item.psNo }}</td><td>{{ item.processNo }}</td><td>{{ item.opNo }}</td><td>{{ item.nodeId }}</td><td>{{ item.brNo }}</td><td class="num">{{ item.curQty }}</td><td>{{ item.curUnitNo }}</td><td class="num">{{ item.sysQty }}</td><td>{{ item.sysUnitNo }}</td><td>{{ item.baseLotNo }}</td></tr><tr v-if="!rows.length"><td colspan="14" class="empty">{{ searched ? '沒有符合條件的生產批。' : '請先查詢。' }}</td></tr></tbody></table></div><div class="footer"><button type="button" class="mes-button primary" :disabled="!dev || !activeLotNo" @click="openSet">Set</button><button type="button" class="mes-button" @click="emit('close')">Close</button></div></fieldset>
    <div v-if="dev && modalLot" class="modal-backdrop"><div class="erf-dialog" role="dialog" aria-modal="true" aria-label="ERF Data"><h2>立即異常單 <small>WP_BRERFCreate · ERFType 2</small></h2><p v-if="modalError" class="notice error" role="alert">{{ modalError }}</p>
      <fieldset><legend>Lot State</legend><div class="field-row"><label>Lot No<input :value="modalLot.lotNo" readonly class="mes-input"></label><label>OP No<input :value="modalLot.opNo" readonly class="mes-input"></label><label>Product No<input :value="modalLot.productNo" readonly class="mes-input"></label><label>Release EQP<select v-model="releaseEQP" class="mes-input yellow"><option :value="false">No</option><option :value="true">Yes</option></select></label><label>Equipment State<select v-model="equipmentState" class="mes-input yellow" :disabled="!releaseEQP"><option value="">請選擇</option><option value="IDLE">IDLE</option><option value="MAINTAIN">MAINTAIN</option></select></label></div></fieldset>
      <fieldset class="erf-data"><legend>ERF Data</legend><div class="erf-top"><div class="erf-fields"><label>ERF No<input :value="previewERFNo" readonly class="mes-input"></label><label>MClass No<select v-model="mClassNo" class="mes-input yellow" @change="changeMClass"><option value="">選擇主分類</option><option v-for="key in mClasses" :key="key">{{ key }}</option></select></label><label>SClass No<select v-model="sClassNo" class="mes-input yellow" :disabled="!mClassNo" @change="changeSClass"><option value="">選擇次分類</option><option v-for="key in sClasses" :key="key">{{ key }}</option></select></label><label>Collect By<select v-model="collectBy" class="mes-input yellow" :disabled="!modalLot.components.length"><option value="lot">Lot</option><option value="component">Component</option></select></label><label v-if="modalLot.components.length">Area<select v-model="area" class="mes-input yellow"><option>All</option><option v-for="n in 5" :key="n">T{{ n }}</option></select></label></div><div class="erf-description"><label>Hold Description<textarea v-model="holdDescription" class="yellow"></textarea></label><label>Attach File<input type="file" multiple @change="addFiles"></label><small>DEV 僅記錄檔名，不上傳內容或產生報表。</small><span>{{ attachmentNames.join('、') }}</span></div></div>
        <div v-if="modalLot.components.length" class="component-picker"><span>Component</span><label v-for="comp in modalLot.components" :key="comp.componentNo"><input v-model="selectedComponents" type="checkbox" :value="comp.componentNo" :disabled="collectBy !== 'component'">{{ comp.componentNo }}</label></div>
        <div class="reason-panels"><div><h3>HoldItem</h3><table><thead><tr><th>Select</th><th>HoldItemNo</th><th>HoldItemName</th><th>Description</th></tr></thead><tbody><tr v-for="item in visibleItems" :key="item.holdItemNo"><td><input v-model="selectedHoldItems" type="checkbox" :value="item.holdItemNo"></td><td>{{ item.holdItemNo }}</td><td>{{ item.holdItemName }}</td><td>{{ item.description }}</td></tr><tr v-if="!visibleItems.length"><td colspan="4" class="empty">先選 MClass／SClass。</td></tr></tbody></table></div><div class="reason-buttons"><button type="button" class="mes-button" @click="addReasons">Add →</button><button type="button" class="mes-button" :disabled="!selectedReasons.length" @click="removeReasons">← Remove</button></div><div><h3>HoldReason</h3><div class="reason-scroll"><table><thead><tr><th>Select</th><th>HoldItemNo</th><th>HoldItemName</th><th>Qty</th><th>Description</th><th>OCAPNO</th><th>OCAPVERSION</th><th v-if="modalLot.components.length">ComponentNo</th><th v-if="modalLot.components.length">AreaNo</th></tr></thead><tbody><tr v-for="reason in reasons" :key="reason.key"><td><input v-model="selectedReasons" type="checkbox" :value="reason.key"></td><td>{{ reason.holdItemNo }}</td><td>{{ reason.holdItemName }}</td><td><input v-model.number="reason.qty" type="number" min="0" step="0.0001" class="grid-input qty"></td><td><input v-model="reason.description" class="grid-input"></td><td><input v-model="reason.ocapNo" class="grid-input"></td><td><input v-model="reason.ocapVersion" class="grid-input"></td><td v-if="modalLot.components.length">{{ reason.componentNo }}</td><td v-if="modalLot.components.length">{{ reason.areaNo }}</td></tr><tr v-if="!reasons.length"><td :colspan="modalLot.components.length ? 9 : 7" class="empty">加入異常原因。</td></tr></tbody></table></div></div></div>
      </fieldset><div class="dialog-footer"><button type="button" class="mes-button primary" @click="confirmERF">Confirm</button><button type="button" class="mes-button" @click="modalLot = null">Close</button></div>
    </div></div>
  </section>
</template>

<style scoped>
.wp501-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:7px;padding:9px;box-sizing:border-box;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif}.dev-banner{padding:6px 9px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}h1{margin:0;font-size:18px}h1 small,h2 small{font-size:12px;font-weight:400;color:#777}.notice{margin:0;min-height:18px}.notice.error{padding:5px 8px;border:1px solid #d6aaa6;background:#fff0ef;color:#99251e}fieldset{min-width:0;margin:0;padding:7px 8px;border:1px solid #bbb;background:#fff}legend{padding:0 5px}.query-frame{position:relative}.query-grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr)) 80px;gap:6px}.query-grid label,.field-row label,.erf-fields label{display:flex;align-items:center;justify-content:flex-end;gap:5px;white-space:nowrap}.query-grid label:nth-of-type(4){grid-column:1}.query-grid .mes-button{grid-column:4;grid-row:2}.mes-input{height:26px;min-width:0;padding:3px 5px;box-sizing:border-box;border:1px solid #888;font:inherit}.query-grid .mes-input{width:105px}.yellow{background:#fff0b3!important}.mes-button,.lookup-button{min-height:29px;padding:3px 10px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#f2f2f2);color:#333;font:inherit;cursor:pointer}.lookup-button{min-width:27px;padding:1px}.mes-button:disabled,.lookup-button:disabled{color:#999;cursor:default}.mes-button.primary{background:#f3a62a;border-color:#e99512;color:#fff}.lookup-options{position:absolute;z-index:5;top:100%;left:8px;display:flex;align-items:center;flex-wrap:wrap;gap:6px;padding:7px;border:1px solid #aaa;background:#fffbe9;box-shadow:0 3px 9px #0002}.lookup-options button{padding:3px 7px;border:1px solid #bbb;background:#fff;cursor:pointer}.lot-frame{flex:1;min-height:200px;display:flex;flex-direction:column}.grid-wrap{flex:1;min-height:100px;overflow:auto;border:1px solid #aaa}.grid-wrap table{border-collapse:collapse;min-width:1180px;white-space:nowrap}.grid-wrap th,.reason-panels th{position:sticky;top:0;background:#ead591;border:1px solid #d4d4d4;padding:5px 7px;text-align:left;font-weight:500}.grid-wrap td,.reason-panels td{border:1px solid #d4d4d4;padding:4px 6px;background:#ffe89c}.grid-wrap tr.active td{background:#ffdb73}.grid-wrap .num{text-align:right}.empty{text-align:center!important;background:#fff!important;color:#777;height:36px}.footer,.dialog-footer{display:flex;justify-content:flex-end;align-items:center;gap:8px;padding-top:7px}.modal-backdrop{position:fixed;inset:0;z-index:200;display:flex;align-items:center;justify-content:center;padding:20px;background:#0007}.erf-dialog{display:flex;flex-direction:column;gap:6px;width:min(900px,100%);max-height:calc(100vh - 40px);overflow:auto;padding:12px;border:1px solid #888;background:#f7f5ee;box-shadow:0 8px 28px #0005}.erf-dialog h2{margin:0;font-size:17px}.field-row{display:flex;align-items:center;gap:8px;flex-wrap:wrap}.field-row .mes-input{max-width:125px}.erf-data{min-height:350px}.erf-top{display:grid;grid-template-columns:1fr 1fr;gap:12px}.erf-fields{display:grid;grid-template-columns:1fr 1fr;gap:6px}.erf-fields label{justify-content:space-between}.erf-fields .mes-input{width:130px}.erf-description{display:flex;flex-direction:column;gap:5px}.erf-description label{display:flex;gap:6px}.erf-description textarea{flex:1;min-height:80px;padding:5px;border:1px solid #888;font:inherit}.erf-description small{color:#777}.component-picker{display:flex;align-items:center;flex-wrap:wrap;gap:8px;padding:6px 0}.reason-panels{display:grid;grid-template-columns:minmax(180px,1fr) 84px minmax(260px,1.35fr);gap:6px;margin-top:5px}.reason-panels h3{margin:0 0 4px;font-size:13px}.reason-panels table{border-collapse:collapse;width:100%;white-space:nowrap}.reason-panels>div:first-child{overflow:auto;max-height:205px}.reason-scroll{overflow:auto;max-height:205px}.reason-buttons{display:flex;flex-direction:column;justify-content:center;gap:8px}.grid-input{min-width:80px;max-width:155px;padding:3px;border:1px solid #aaa;background:#fff0b3;font:inherit}.grid-input.qty{min-width:55px;width:60px}@media(max-width:850px){.query-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.query-grid label:nth-of-type(4),.query-grid .mes-button{grid-column:auto;grid-row:auto}.erf-top{grid-template-columns:1fr}.reason-panels{grid-template-columns:1fr}.reason-buttons{flex-direction:row}}
</style>
