<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type Mode = 'undo' | 'cancel'
type Mo = { moNo: string; moState: number }
type Lot = {
  lotNo: string; moNo: string; status: number; exLotState1: boolean
  productNo: string; productName: string; productVersion: string; psNo: string
  processNo: string; opNo: string; opName: string; curQty: number; curUnitNo: string
  areaNo: string; inventoryNo: string; hasReport: boolean; sapReady: boolean
}
type History = { lotNo: string; mode: Mode; actor: string; time: string; result: string }
type Saved = { lots: Lot[]; history: History[] }

const props = defineProps<{ developmentPreview?: boolean; currentUserNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const unavailableMessage = "正式環境尚未開放此功能。"
const storageKey = 'ymes-dev-mock:wip-undo-dispatch:state:v1'
const mos: Mo[] = dev.value ? ([
  { moNo: 'DEV-MO-001', moState: 3 },
  { moNo: 'DEV-MO-002', moState: 6 },
  { moNo: 'DEV-MO-CLOSED', moState: 99 },
]) : ([])
const columns: { key: keyof Lot; label: string }[] = [
  { key: 'lotNo', label: 'LotNo' }, { key: 'moNo', label: 'MONo' },
  { key: 'status', label: 'Status' }, { key: 'productNo', label: 'ProductNo' },
  { key: 'productName', label: 'ProductName' }, { key: 'productVersion', label: 'ProductVersion' },
  { key: 'psNo', label: 'PSNo' }, { key: 'processNo', label: 'ProcessNo' },
  { key: 'opNo', label: 'OPNo' }, { key: 'opName', label: 'OPName' },
  { key: 'curQty', label: 'CurQty' }, { key: 'curUnitNo', label: 'CurUnitNo' },
]
const mode = ref<Mode>('undo')
const moNo = ref('')
const lookupOpen = ref(false)
const lookupText = ref('')
const queried = ref(false)
const selectedNos = ref<string[]>([])
const confirmOpen = ref(false)
const error = ref('')
const message = ref(dev.value ? '請選擇工單並查詢。' : unavailableMessage)
const saved = ref<Saved>(dev.value ? (fixtures()) : ({ lots: [], history: [] }))
const eligibleMos = computed(() => dev.value ? (mos.filter(item => [3, 6].includes(item.moState) && item.moNo.includes(lookupText.value.trim().toUpperCase()))) : ([]))
const rows = computed(() => dev.value ? (queried.value ? saved.value.lots.filter(item => item.moNo === moNo.value && (mode.value === 'undo' ? item.status === 3 : item.status === 99 && item.exLotState1)) : []) : ([]))

function fixtures(): Saved {
  if (!dev.value) return { lots: [], history: [] }
 return { lots: [
  { lotNo: 'DEV-LOT-001', moNo: 'DEV-MO-001', status: 3, exLotState1: false, productNo: 'DEV-PRD-1001', productName: '示意產品 A', productVersion: '01', psNo: 'DEV-PS-01', processNo: 'DEV-PROC-01', opNo: 'DEV-OP-01', opName: '示意作業站', curQty: 100, curUnitNo: 'PCS', areaNo: 'DEV-AREA-A', inventoryNo: 'DEV-INV-A', hasReport: true, sapReady: true },
  { lotNo: 'DEV-LOT-002', moNo: 'DEV-MO-001', status: 3, exLotState1: false, productNo: 'DEV-PRD-1001', productName: '示意產品 A', productVersion: '01', psNo: 'DEV-PS-01', processNo: 'DEV-PROC-01', opNo: 'DEV-OP-02', opName: '示意測試站', curQty: 50, curUnitNo: 'PCS', areaNo: 'DEV-AREA-A', inventoryNo: 'DEV-INV-A', hasReport: true, sapReady: true },
  { lotNo: 'DEV-LOT-003', moNo: 'DEV-MO-002', status: 99, exLotState1: true, productNo: 'DEV-PRD-1002', productName: '示意產品 B', productVersion: '02', psNo: 'DEV-PS-02', processNo: 'DEV-PROC-02', opNo: 'DEV-OP-03', opName: '示意包裝站', curQty: 25, curUnitNo: 'PCS', areaNo: 'DEV-AREA-B', inventoryNo: 'DEV-INV-B', hasReport: true, sapReady: true },
  { lotNo: 'DEV-LOT-004', moNo: 'DEV-MO-CLOSED', status: 99, exLotState1: true, productNo: 'DEV-PRD-1002', productName: '示意產品 B', productVersion: '02', psNo: 'DEV-PS-02', processNo: 'DEV-PROC-02', opNo: 'DEV-OP-03', opName: '示意包裝站', curQty: 10, curUnitNo: 'PCS', areaNo: 'DEV-AREA-B', inventoryNo: 'DEV-INV-B', hasReport: true, sapReady: true },
], history: [] } }
function load(): void {
  if (!dev.value) return
  try {
    const raw = window.localStorage.getItem(storageKey)
    const parsed = raw ? JSON.parse(raw) as Saved : null
    saved.value = parsed && Array.isArray(parsed.lots) && Array.isArray(parsed.history) ? parsed : fixtures()
  } catch { saved.value = fixtures(); error.value = 'DEV 假資料無法讀取，已使用初始資料。' }
}
function persist(): boolean {
  if (!dev.value) return false

  try { window.localStorage.setItem(storageKey, JSON.stringify(saved.value)); return true }
  catch { error.value = 'DEV 假資料無法儲存。'; return false }
}
function changeMode(value: Mode): void {
  if (!dev.value) return

  mode.value = value; queried.value = false; selectedNos.value = []; error.value = ''
  message.value = '模式已切換，請重新 Query。'
}
function chooseMo(value: string): void {
  if (!dev.value) return

  moNo.value = value; lookupOpen.value = false; queried.value = false; selectedNos.value = []
}
function query(): void {
  error.value = ''
  if (!dev.value) { error.value = unavailableMessage; return }
  if (!moNo.value) { error.value = '請先選擇 MONo。'; return }
  load()
  const mo = mos.find(item => item.moNo === moNo.value)
  if (!mo) { error.value = '找不到工單。'; return }
  if (mode.value === 'cancel' && mo.moState === 99) { error.value = '已關結工單不可取消撤派工。'; return }
  queried.value = true; selectedNos.value = []
  message.value = `DEV 查得 ${rows.value.length} 筆${mode.value === 'undo' ? '可撤派工' : '可取消撤派工'} Lot。`
}
function toggle(no: string): void {
  if (!dev.value) return

  selectedNos.value = selectedNos.value.includes(no) ? selectedNos.value.filter(value => value !== no) : [...selectedNos.value, no]
}
function selectAll(value: boolean): void {
  if (!dev.value) return
 selectedNos.value = value ? rows.value.map(row => row.lotNo) : [] }
function openConfirm(): void {
  if (!dev.value) return

  error.value = ''
  if (!selectedNos.value.length) { error.value = '至少勾選一筆 Lot。'; return }
  confirmOpen.value = true
}
function confirm(): void {
  if (!dev.value) return

  confirmOpen.value = false; error.value = ''
  if (!dev.value) return
  const action = mode.value
  const selected = [...selectedNos.value]
  const mo = mos.find(item => item.moNo === moNo.value)
  if (action === 'cancel' && mo?.moState === 99) { error.value = '已關結工單不可取消撤派工。'; return }
  load()
  let done = 0
  for (const no of selected) {
    const row = saved.value.lots.find(item => item.lotNo === no && item.moNo === moNo.value)
    if (!row || (action === 'undo' ? row.status !== 3 : row.status !== 99 || !row.exLotState1)) { error.value = `${no} 的 Lot 狀態已變更；已處理 ${done} 筆。`; break }
    if (row.hasReport && !row.sapReady) { error.value = `${no} 的 DEV SAP 資料不完整；已處理 ${done} 筆。`; break }
    if (action === 'cancel' && !row.inventoryNo) { error.value = `${no} 找不到 Area 預設庫房；已處理 ${done} 筆。`; break }
    const before = structuredClone(saved.value)
    row.status = action === 'undo' ? 99 : 12
    row.exLotState1 = action === 'undo'
    saved.value.history.push({ lotNo: no, mode: action, actor: props.currentUserNo || 'DEV', time: new Date().toISOString(), result: action === 'cancel' ? `DEV ZPPT0025A/B/C1 + INVWIP_Scrap ${row.curQty} ${row.curUnitNo}` : 'DEV ZPPT0025A/B/C1' })
    if (!persist()) { saved.value = before; break }
    done += 1
  }
  const failure = error.value
  query()
  if (failure) error.value = failure
  message.value = `DEV 已${action === 'undo' ? '撤派工' : '取消撤派工'} ${done} 筆；逐批假狀態，未寫 SAP 或資料庫。`
}
function openMoLookup(): void { if (!dev.value) return; lookupText.value = ''; lookupOpen.value = true }
onMounted(load)
</script>

<template>
  <section class="undo-page">
    <h1>工單撤派工 <small>UNDO DISPATCH · WP112</small></h1>
    <p class="note">{{ dev ? 'DEV 假資料：Confirm 僅示意 Lot 狀態；SAP 中間表、庫房、Log 與正式交易待 API。' : unavailableMessage }}</p>
    <p v-if="error" class="error" role="alert">{{ error }}</p>
    <fieldset class="query" :disabled="!dev"><legend>Undo Dispatch Query</legend>
      <div class="query-top"><label><input :checked="mode === 'undo'" type="radio" name="undo-mode" @change="changeMode('undo')"> Undo Dispatch</label><label><input :checked="mode === 'cancel'" type="radio" name="undo-mode" @change="changeMode('cancel')"> Cancel Undo Dispatch</label></div>
      <div class="query-bottom"><label>MONo <input :value="moNo" readonly class="yellow"></label><button type="button" class="button" :disabled="!dev" @click="openMoLookup">...</button><button type="button" class="button query-button" :disabled="!dev" @click="query">Query</button></div>
    </fieldset>
    <div class="select-bar"><button type="button" class="button" :disabled="!rows.length" @click="selectAll(true)">SelectAll</button><button type="button" class="button" :disabled="!rows.length" @click="selectAll(false)">UnSelectAll</button></div>
    <fieldset class="data" :disabled="!dev"><legend>Undo Dispatch Data</legend><div class="data-head"><span>{{ message }}</span><button type="button" class="button" :disabled="!selectedNos.length || !dev" @click="openConfirm">Confirm</button></div>
      <div class="scroll"><table><thead><tr><th>Select</th><th v-for="column in columns" :key="column.key">{{ column.label }}</th></tr></thead><tbody><tr v-for="row in rows" :key="row.lotNo" :class="{ selected: selectedNos.includes(row.lotNo) }" @click="toggle(row.lotNo)"><td><input type="checkbox" :checked="selectedNos.includes(row.lotNo)" aria-label="選取 Lot" @click.stop="toggle(row.lotNo)"></td><td v-for="column in columns" :key="column.key">{{ row[column.key] }}</td></tr><tr v-if="!rows.length"><td colspan="13" class="empty">{{ !dev ? unavailableMessage : queried ? '沒有符合條件的 Lot。' : '請先 Query。' }}</td></tr></tbody></table></div>
      <div class="status-bar">資料總筆數 {{ rows.length }} <span>已選 {{ selectedNos.length }}</span></div>
    </fieldset>
    <div class="footer"><button type="button" class="button" @click="emit('close')">Close</button></div>
    <div v-if="dev && lookupOpen" class="overlay"><div class="dialog" role="dialog" aria-modal="true"><h2>MONo 查找</h2><input v-model="lookupText" placeholder="搜尋 MONo" autofocus><div class="lookup-list"><button v-for="mo in eligibleMos" :key="mo.moNo" type="button" class="button" @click="chooseMo(mo.moNo)">{{ mo.moNo }} · MOState {{ mo.moState }}</button><p v-if="!eligibleMos.length">沒有 MOState 3／6 的工單。</p></div><button type="button" class="button" @click="lookupOpen = false">Close</button></div></div>
    <div v-if="dev && confirmOpen" class="overlay"><div class="dialog" role="alertdialog" aria-modal="true"><h2>{{ mode === 'undo' ? 'Undo Dispatch' : 'Cancel Undo Dispatch' }}</h2><p>將逐筆處理 {{ selectedNos.length }} 個 Lot；遇錯停止。</p><p class="note">正式 SAP／MES 交易尚未接入，本次只改 DEV 假資料。</p><div class="dialog-actions"><button type="button" class="button" @click="confirmOpen = false">Cancel</button><button type="button" class="button primary" @click="confirm">Confirm</button></div></div></div>
  </section>
</template>

<style scoped>
.undo-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:7px;padding:9px;box-sizing:border-box;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif}
h1{margin:0;font-size:18px}h1 small{font-size:12px;font-weight:400;color:#777}.note,.error{margin:0;padding:6px;border:1px solid #d8c994;background:#fff8dc}.error{border-color:#d8a7a2;background:#fff1ef;color:#8f261f}
fieldset{min-width:0;margin:0;padding:7px;border:1px solid #bbb;background:#fff}legend{padding:0 5px}.query-top,.query-bottom,.select-bar{display:flex;align-items:center;gap:8px}.query-top{padding-bottom:7px}.query-bottom label{display:flex;align-items:center;gap:8px}.query-bottom input{height:25px;border:1px solid #999;padding:2px 5px}.yellow{background:#fff0b3}.query-button{margin-left:auto}.button{min-height:27px;padding:3px 9px;border:1px solid #aaa;border-radius:3px;background:linear-gradient(#fff,#eee);color:#333;font:inherit;cursor:pointer}.button:disabled{color:#aaa;cursor:default}.select-bar{min-height:30px}
.data{flex:1;min-height:220px;display:flex;flex-direction:column}.data-head{display:flex;align-items:center;justify-content:space-between;min-height:32px;padding-bottom:5px;color:#666}.scroll{flex:1;min-height:0;overflow:auto;border:1px solid #aaa}table{min-width:1350px;width:100%;border-collapse:collapse;white-space:nowrap}th,td{padding:5px 7px;border:1px solid #ccc;text-align:left}th{position:sticky;top:0;background:#ead591;font-weight:500}td{background:#f4f3ef}tr.selected td{background:#ffe89c}td:first-child{text-align:center;background:#fff}.empty{text-align:center;color:#777;height:40px}.status-bar{display:flex;justify-content:space-between;padding-top:5px;color:#777}.footer{display:flex;justify-content:flex-end}.overlay{position:fixed;inset:0;z-index:2000;display:flex;align-items:center;justify-content:center;padding:12px;background:#0007}.dialog{width:min(520px,96vw);max-height:90vh;overflow:auto;padding:12px;border:1px solid #888;background:#f7f5ee;box-shadow:0 8px 28px #0005}.dialog h2{margin:0 0 10px}.dialog>input{width:100%;box-sizing:border-box;height:27px;border:1px solid #999}.lookup-list{display:flex;flex-direction:column;gap:5px;margin:10px 0}.dialog-actions{display:flex;justify-content:flex-end;gap:7px;margin-top:12px}.primary{background:#f3a62a;border-color:#e99512;color:#fff}
</style>
