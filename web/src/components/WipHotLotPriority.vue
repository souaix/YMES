<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'

type LookupField = 'roNo' | 'areaNo' | 'opNo' | 'productNo'
type Lot = {
  lotNo: string; baseLotNo: string; roNo: string; areaNo: string; moNo: string; opNo: string
  productNo: string; psNo: string; processNo: string; opName: string; brNo: string; opGroupNo: string
  curQty: number; curUnitNo: string; sysQty: number; sysUnitNo: string
  status: number; lotState: number; hotLot: 0 | 1; priority: number; version: number
}
type Change = { baseLotNo: string; lotNos: string[]; hotLot: 0 | 1; priority: number; at: string }
type MockState = { lots: Lot[]; changes: Change[] }

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview !== false)
const storageKey = 'ymes-dev-mock:wip-hotlot-priority:state:v1'
const fixtures: Lot[] = [
  { lotNo: 'DEV-HOT-001', baseLotNo: 'DEV-BASE-HOT-01', roNo: 'RO-01', areaNo: 'A01', moNo: 'MO-501', opNo: 'OP-PACK', productNo: 'PRD-A', psNo: 'PS-01', processNo: 'PROC-A', opName: '包裝', brNo: 'BR-PACK', opGroupNo: 'OG-01', curQty: 120, curUnitNo: 'PCS', sysQty: 120, sysUnitNo: 'PCS', status: 0, lotState: 1, hotLot: 0, priority: 50, version: 1 },
  { lotNo: 'DEV-HOT-002', baseLotNo: 'DEV-BASE-HOT-02', roNo: 'RO-01', areaNo: 'A01', moNo: 'MO-501', opNo: 'OP-TEST', productNo: 'PRD-A', psNo: 'PS-01', processNo: 'PROC-A', opName: '測試', brNo: 'BR-TEST', opGroupNo: 'OG-02', curQty: 80, curUnitNo: 'PCS', sysQty: 100, sysUnitNo: 'PCS', status: 1, lotState: 1, hotLot: 1, priority: 10, version: 1 },
  { lotNo: 'DEV-HOT-003', baseLotNo: 'DEV-BASE-HOT-03', roNo: 'RO-02', areaNo: 'A02', moNo: 'MO-502', opNo: 'OP-TEST', productNo: 'PRD-B', psNo: 'PS-02', processNo: 'PROC-B', opName: '測試', brNo: 'BR-TEST', opGroupNo: 'OG-02', curQty: 30, curUnitNo: 'PCS', sysQty: 30, sysUnitNo: 'PCS', status: 3, lotState: 1, hotLot: 0, priority: 99, version: 1 },
  { lotNo: 'DEV-HOT-004', baseLotNo: 'DEV-BASE-HOT-01', roNo: 'RO-01', areaNo: 'A01', moNo: 'MO-501', opNo: 'OP-PACK', productNo: 'PRD-A', psNo: 'PS-01', processNo: 'PROC-A', opName: '包裝', brNo: 'BR-PACK', opGroupNo: 'OG-01', curQty: 15, curUnitNo: 'PCS', sysQty: 15, sysUnitNo: 'PCS', status: 4, lotState: 1, hotLot: 0, priority: 50, version: 1 },
  { lotNo: 'DEV-HOT-CLOSED', baseLotNo: 'DEV-BASE-CLOSED', roNo: 'RO-02', areaNo: 'A02', moNo: 'MO-502', opNo: 'OP-TEST', productNo: 'PRD-B', psNo: 'PS-02', processNo: 'PROC-B', opName: '測試', brNo: 'BR-TEST', opGroupNo: 'OG-02', curQty: 0, curUnitNo: 'PCS', sysQty: 30, sysUnitNo: 'PCS', status: 99, lotState: 0, hotLot: 0, priority: 99, version: 1 },
]
const state = ref<MockState>({ lots: [], changes: [] })
const filters = reactive({ roNo: '', moNo: '', productNo: '', areaNo: '', opNo: '', lotNo: '' })
const searched = ref(false)
const rows = ref<Lot[]>([])
const selected = ref<string[]>([])
const scanLotNo = ref('')
const hotLotChoice = ref<0 | 1 | null>(null)
const priorityInput = ref('')
const lookup = ref<LookupField | null>(null)
const message = ref('請輸入至少一項查詢條件，按 Query 載入生產批。')
const errorText = ref('')
const statusName: Record<number, string> = { 0: 'Queue', 1: 'Running', 2: 'Wait', 3: 'Hold', 4: 'Complete', 5: 'OS' }
const lookupOptions = computed(() => lookup.value ? [...new Set(state.value.lots.filter(item => item.lotState === 1).map(item => item[lookup.value!]))].sort() : [])
const visibleSelected = computed(() => rows.value.filter(item => selected.value.includes(item.lotNo)))

function seed(): MockState { return { lots: fixtures.map(item => ({ ...item })), changes: [] } }
function load(): void {
  if (!dev.value) return
  try {
    const data = JSON.parse(window.localStorage.getItem(storageKey) || 'null') as unknown
    if (data && typeof data === 'object' && 'lots' in data && 'changes' in data && Array.isArray(data.lots) && Array.isArray(data.changes)) { state.value = data as MockState; return }
  } catch { /* use fixtures */ }
  state.value = seed()
}
function norm(value: string): string { return value.trim().toUpperCase() }
function query(): void {
  errorText.value = ''; lookup.value = null
  if (!dev.value) { errorText.value = '此頁目前僅提供 DEV 假資料預覽。'; return }
  for (const key of Object.keys(filters) as (keyof typeof filters)[]) filters[key] = norm(filters[key])
  if (!Object.values(filters).some(Boolean)) { errorText.value = '請至少輸入一個查詢條件。'; return }
  rows.value = state.value.lots.filter(item => item.lotState === 1 && item.status >= 0 && item.status <= 5 &&
    (Object.keys(filters) as (keyof typeof filters)[]).every(key => !filters[key] || item[key] === filters[key]))
    .sort((a, b) => b.hotLot - a.hotLot || a.priority - b.priority || a.lotNo.localeCompare(b.lotNo))
  searched.value = true; selected.value = []; scanLotNo.value = ''; hotLotChoice.value = null; priorityInput.value = '99'
  message.value = `查得 ${rows.value.length} 筆生產批。`
}
function scan(): void {
  errorText.value = ''
  const no = norm(scanLotNo.value)
  if (!no) return
  const row = rows.value.find(item => item.lotNo === no)
  if (!row) { errorText.value = `${no} 不在目前查詢結果。`; return }
  if (!selected.value.includes(no)) selected.value.push(no)
  scanLotNo.value = ''; message.value = `已選取 ${no}。`
}
function chooseLookup(value: string): void { if (lookup.value) filters[lookup.value] = value; lookup.value = null }
function setPriority(): void {
  errorText.value = ''
  if (!dev.value || !visibleSelected.value.length) { errorText.value = '請先選取至少一筆生產批。'; return }
  const newHotLot = hotLotChoice.value
  if (newHotLot === null) { errorText.value = '請選擇 HotLot Yes 或 No。'; return }
  const priority = Number(priorityInput.value)
  if (!priorityInput.value.trim() || !Number.isInteger(priority) || priority < 0 || priority > 99) { errorText.value = 'Priority 必須是 0–99 的整數。'; return }
  let current: MockState
  try {
    const data = JSON.parse(window.localStorage.getItem(storageKey) || 'null') as unknown
    current = data && typeof data === 'object' && 'lots' in data && 'changes' in data && Array.isArray(data.lots) && Array.isArray(data.changes) ? data as MockState : seed()
  } catch { errorText.value = '無法讀取 DEV 模擬狀態。'; return }
  const baseNos = new Set(visibleSelected.value.map(item => item.baseLotNo))
  if (visibleSelected.value.some(item => current.lots.find(now => now.lotNo === item.lotNo)?.version !== item.version)) { errorText.value = '生產批狀態已變更，請重新 Query。'; return }
  const changedLots = current.lots.filter(item => baseNos.has(item.baseLotNo))
  for (const item of changedLots) { item.hotLot = newHotLot; item.priority = priority; item.version += 1 }
  for (const baseLotNo of baseNos) current.changes.push({ baseLotNo, lotNos: changedLots.filter(item => item.baseLotNo === baseLotNo).map(item => item.lotNo), hotLot: newHotLot, priority, at: new Date().toISOString() })
  try { window.localStorage.setItem(storageKey, JSON.stringify(current)) }
  catch { errorText.value = '瀏覽器無法儲存 DEV 模擬結果。'; return }
  state.value = current; query()
  message.value = `DEV 已模擬更新 ${baseNos.size} 個 BaseLotNo、${changedLots.length} 筆 Lot；未更新 MES。`
}
onMounted(load)
watch(dev, enabled => {
  if (enabled) load()
  else { rows.value = []; selected.value = []; searched.value = false; lookup.value = null }
})
</script>

<template>
  <section class="wp914-page">
    <div v-if="dev" class="dev-banner">DEV 假資料預覽：可輸入 RO-01、MO-501 或 DEV-HOT-001 查詢。</div>
    <h1>生產批緊急狀態／優先權調整 <small>WP914 · LOT HOTLOT AND PRIPORITY CHANGE</small></h1>
    <p v-if="errorText" class="notice error" role="alert">{{ errorText }}</p><p v-else class="notice" role="status">{{ message }}</p>
    <fieldset class="query-frame"><legend>Lot Query</legend>
      <div class="query-grid">
        <label>RO No<input v-model="filters.roNo" :disabled="!dev" class="mes-input yellow" @keydown.enter.prevent="query"></label><button type="button" class="lookup-button" :disabled="!dev" @click="lookup = lookup === 'roNo' ? null : 'roNo'">...</button>
        <label>MO No<input v-model="filters.moNo" :disabled="!dev" class="mes-input yellow" @keydown.enter.prevent="query"></label><span></span>
        <label>Product No<input v-model="filters.productNo" :disabled="!dev" class="mes-input yellow" @keydown.enter.prevent="query"></label><button type="button" class="lookup-button" :disabled="!dev" @click="lookup = lookup === 'productNo' ? null : 'productNo'">...</button>
        <label>Area No<input v-model="filters.areaNo" :disabled="!dev" class="mes-input yellow" @keydown.enter.prevent="query"></label><button type="button" class="lookup-button" :disabled="!dev" @click="lookup = lookup === 'areaNo' ? null : 'areaNo'">...</button>
        <label>OP No<input v-model="filters.opNo" :disabled="!dev" class="mes-input yellow" @keydown.enter.prevent="query"></label><button type="button" class="lookup-button" :disabled="!dev" @click="lookup = lookup === 'opNo' ? null : 'opNo'">...</button>
        <label>Lot No<input v-model="filters.lotNo" :disabled="!dev" class="mes-input yellow" @keydown.enter.prevent="query"></label><button type="button" class="mes-button" :disabled="!dev" @click="query">Query</button>
      </div>
      <div v-if="lookup" class="lookup-list"><strong>{{ lookup }} 查找</strong><button v-for="option in lookupOptions" :key="option" type="button" @click="chooseLookup(option)">{{ option }}</button></div>
    </fieldset>
    <fieldset class="lot-frame"><legend>Lot</legend>
      <div class="select-row"><button type="button" class="mes-button" :disabled="!dev || !rows.length" @click="selected = rows.map(item => item.lotNo); scanLotNo = ''">Select All</button><button type="button" class="mes-button" :disabled="!dev || !rows.length" @click="selected = []; scanLotNo = ''">UnSelect All</button><label>Lot No <input v-model="scanLotNo" :disabled="!dev || !rows.length" class="mes-input yellow" @keydown.enter.prevent="scan"></label><small>掃描後按 Enter 勾選</small><span class="count">已選 {{ visibleSelected.length }} / {{ rows.length }}</span></div>
      <div class="grid-wrap"><table><thead><tr><th>Select</th><th>LotNo</th><th>MONo</th><th>HotLot</th><th>Priority</th><th>Status</th><th>ProductNo</th><th>AreaNo</th><th>PSNo</th><th>ProcessNo</th><th>OPNo</th><th>OPName</th><th>BRNo</th><th>CurQTY</th><th>CurUnitNo</th><th>SysQTY</th><th>SysUnitNo</th><th>OPGroupNo</th></tr></thead><tbody><tr v-for="item in rows" :key="item.lotNo"><td><input v-model="selected" type="checkbox" :value="item.lotNo" :disabled="!dev" :aria-label="'Select ' + item.lotNo"></td><td>{{ item.lotNo }}</td><td>{{ item.moNo }}</td><td>{{ item.hotLot ? 'Yes' : 'No' }}</td><td class="num">{{ item.priority }}</td><td>{{ statusName[item.status] }}</td><td>{{ item.productNo }}</td><td>{{ item.areaNo }}</td><td>{{ item.psNo }}</td><td>{{ item.processNo }}</td><td>{{ item.opNo }}</td><td>{{ item.opName }}</td><td>{{ item.brNo }}</td><td class="num">{{ item.curQty }}</td><td>{{ item.curUnitNo }}</td><td class="num">{{ item.sysQty }}</td><td>{{ item.sysUnitNo }}</td><td>{{ item.opGroupNo }}</td></tr><tr v-if="!rows.length"><td colspan="18" class="empty">{{ searched ? '沒有符合條件的生產批。' : '請先查詢。' }}</td></tr></tbody></table></div>
      <div class="actions"><span>HotLot</span><label class="radio"><input v-model="hotLotChoice" type="radio" :value="0" :disabled="!dev">No</label><label class="radio"><input v-model="hotLotChoice" type="radio" :value="1" :disabled="!dev">Yes</label><label>Priority <input v-model="priorityInput" inputmode="numeric" class="mes-input yellow priority" :disabled="!dev"></label><button type="button" class="mes-button set" :disabled="!dev || !visibleSelected.length" @click="setPriority">Set</button><button type="button" class="mes-button close" @click="emit('close')">Close</button></div>
    </fieldset>
  </section>
</template>

<style scoped>
.wp914-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:7px;padding:9px;box-sizing:border-box;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif}.dev-banner{padding:6px 9px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}h1{margin:0;font-size:18px;font-weight:600}h1 small{font-size:12px;font-weight:400;color:#777}.notice{margin:0;min-height:18px}.notice.error{padding:5px 8px;background:#fff0ef;border:1px solid #d6aaa6;color:#99251e}fieldset{min-width:0;margin:0;padding:8px;box-sizing:border-box;border:1px solid #bbb;background:#fff}legend{padding:0 5px}.query-frame{position:relative;flex:none}.query-grid{display:grid;grid-template-columns:minmax(0,1fr) 31px minmax(0,1fr) 31px minmax(0,1fr) 80px;gap:5px 7px;align-items:center}.query-grid label{display:flex;align-items:center;justify-content:flex-end;gap:5px;white-space:nowrap}.mes-input{height:25px;min-width:0;padding:3px 5px;border:1px solid #888;box-sizing:border-box;font:inherit}.query-grid .mes-input{width:110px}.yellow{background:#fff0b3}.mes-button,.lookup-button{min-height:27px;padding:3px 9px;border:1px solid #b9b9b9;border-radius:3px;background:linear-gradient(#fff,#f2f2f2);color:#333;font:inherit;cursor:pointer}.mes-button:disabled,.lookup-button:disabled{color:#999;cursor:default}.lookup-button{width:28px;padding:2px}.lookup-list{position:absolute;z-index:5;top:100%;left:10px;display:flex;align-items:center;gap:5px;max-width:80%;flex-wrap:wrap;padding:8px;border:1px solid #aaa;background:#fffbe9;box-shadow:0 3px 9px #0002}.lookup-list button{padding:3px 7px;border:1px solid #bbb;background:#fff;cursor:pointer}.lot-frame{flex:1;min-height:240px;display:flex;flex-direction:column}.select-row{display:flex;align-items:center;gap:7px;min-height:32px}.select-row label{display:flex;align-items:center;gap:5px;margin-left:5px}.select-row small{color:#777}.count{margin-left:auto;color:#666}.grid-wrap{flex:1;min-height:100px;overflow:auto;border:1px solid #aaa}.grid-wrap table{border-collapse:collapse;min-width:1420px;white-space:nowrap}.grid-wrap th{position:sticky;top:0;z-index:1;background:#ead591;border:1px solid #d3d3d3;padding:5px 7px;text-align:left;font-weight:500}.grid-wrap td{background:#ffe89c;border:1px solid #d3d3d3;padding:4px 6px}.grid-wrap tr:has(input:checked) td{background:#ffdb73}.grid-wrap td:first-child{text-align:center}.grid-wrap .num{text-align:right;font-variant-numeric:tabular-nums}.grid-wrap .empty{text-align:center;background:#fff;color:#777;height:44px}.actions{display:flex;align-items:center;gap:8px;min-height:45px}.actions label{display:flex;align-items:center;gap:5px}.actions .priority{width:65px}.actions .radio{gap:2px}.actions .set{min-width:78px}.actions .close{margin-left:auto;min-width:78px}@media(max-width:860px){.query-grid{grid-template-columns:minmax(0,1fr) 31px minmax(0,1fr) 65px}.query-grid .mes-input{width:100%}.actions{flex-wrap:wrap}}
</style>
