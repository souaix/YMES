<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'

type FilterKey = 'areaNo' | 'psNo' | 'opNo' | 'customerNo' | 'productNo' | 'lotNo'
type Lot = { lotNo: string; opNo: string; opName: string; curQty: number; status: 0 | 1 | 4; productNo: string; customerNo: string; areaNo: string; psNo: string; partialMode: boolean; packed: boolean; lotStamp: number }
type FutureHold = { lotNo: string; opNo: string; holdTime: 0 | 1; moduleNo: 'N/A'; moduleVersion: 'N/A'; futureHoldSerial: 'N/A'; mClassNo: string; holdDescription: string; holdItemNos: string[]; creator: string; createdAt: string }
type MockState = { lots: Lot[]; holds: FutureHold[] }

const props = defineProps<{ developmentPreview?: boolean; currentUserNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview !== false)
const storageKey = 'ymes-dev-mock:wip-future-hold-set:state:v1'
const fields: { key: FilterKey; label: string; lookup: boolean }[] = [
  { key: 'areaNo', label: 'Area No', lookup: true }, { key: 'psNo', label: 'PS No', lookup: true }, { key: 'opNo', label: 'OP No', lookup: true },
  { key: 'customerNo', label: 'Customer No', lookup: true }, { key: 'productNo', label: 'Product No', lookup: true }, { key: 'lotNo', label: 'LotNo', lookup: false },
]
const statusText: Record<Lot['status'], string> = { 0: 'Queue', 1: 'Running', 4: 'Complete' }
const psOps: Record<string, { opNo: string; opName: string }[]> = {
  'PS-01': [{ opNo: 'OP-CUT', opName: '切割' }, { opNo: 'OP-TEST', opName: '測試' }],
  'PS-02': [{ opNo: 'OP-PACK', opName: '包裝' }],
}
const holdItems = [
  { mClassNo: 'M-QUALITY', sClassNo: 'S-VISUAL', holdItemNo: 'H-001', holdItemName: '外觀異常' },
  { mClassNo: 'M-QUALITY', sClassNo: 'S-VISUAL', holdItemNo: 'H-002', holdItemName: '尺寸異常' },
  { mClassNo: 'M-EQUIPMENT', sClassNo: 'S-EQP', holdItemNo: 'H-003', holdItemName: '設備異常' },
]
const descriptions: Record<string, string[]> = { 'M-QUALITY': ['外觀檢查異常', '尺寸超出規格'], 'M-EQUIPMENT': ['設備點檢異常'] }
const firstGroupOps: Record<string, string[]> = { 'M-QUALITY': ['OP-CUT'], 'M-EQUIPMENT': ['OP-PACK'] }
const fixtures: Lot[] = [
  { lotNo: 'DEV-FH-001', opNo: 'OP-CUT', opName: '切割', curQty: 8, status: 0, productNo: 'PRD-A', customerNo: 'CUS-A', areaNo: 'A01', psNo: 'PS-01', partialMode: false, packed: false, lotStamp: 50401 },
  { lotNo: 'DEV-FH-002', opNo: 'OP-CUT', opName: '切割', curQty: 6, status: 1, productNo: 'PRD-A', customerNo: 'CUS-A', areaNo: 'A01', psNo: 'PS-01', partialMode: false, packed: false, lotStamp: 50402 },
  { lotNo: 'DEV-FH-PACK', opNo: 'OP-CUT', opName: '切割', curQty: 4, status: 4, productNo: 'PRD-A', customerNo: 'CUS-A', areaNo: 'A01', psNo: 'PS-01', partialMode: false, packed: true, lotStamp: 50403 },
  { lotNo: 'DEV-FH-PART', opNo: 'OP-PACK', opName: '包裝', curQty: 3, status: 0, productNo: 'PRD-B', customerNo: 'CUS-B', areaNo: 'A02', psNo: 'PS-02', partialMode: true, packed: false, lotStamp: 50404 },
]

const state = ref<MockState>({ lots: [], holds: [] })
const filters = reactive<Record<FilterKey, string>>({ areaNo: '', psNo: '', opNo: '', customerNo: '', productNo: '', lotNo: '' })
const lookupField = ref<FilterKey | null>(null)
const lookupOptions = computed(() => lookupField.value ? [...new Set(state.value.lots.map(item => item[lookupField.value!]))].sort() : [])
const rows = ref<Lot[]>([]); const selectedLotNos = ref<string[]>([]); const selectedOnly = ref(false); const searched = ref(false); const lotScan = ref('')
const visibleRows = computed(() => selectedOnly.value ? rows.value.filter(item => selectedLotNos.value.includes(item.lotNo)) : rows.value)
const holdTime = ref<0 | 1>(0); const holdPsNo = ref(''); const selectedOps = ref<string[]>([])
const opOptions = computed(() => psOps[holdPsNo.value] || [])
const psLookupOpen = ref(false)
const mClassNo = ref(''); const sClassNo = ref(''); const holdDescription = ref(''); const selectedHoldItemNos = ref<string[]>([]); const holdItemScan = ref('')
const mClasses = [...new Set(holdItems.map(item => item.mClassNo))]
const sClasses = computed(() => [...new Set(holdItems.filter(item => item.mClassNo === mClassNo.value).map(item => item.sClassNo))])
const visibleHoldItems = computed(() => holdItems.filter(item => item.mClassNo === mClassNo.value && item.sClassNo === sClassNo.value))
const missingFirstGroup = computed(() => selectedOps.value.filter(opNo => !firstGroupOps[mClassNo.value]?.includes(opNo)))
const acceptMissingFirstGroup = ref(false)
const errorText = ref(''); const message = ref('請輸入查詢條件，選擇 Lot 與 Hold Data 後按 Confirm。')

function seed(): MockState { return { lots: fixtures.map(item => ({ ...item })), holds: [] } }
function load(): void {
  if (!dev.value) return
  try {
    const data = JSON.parse(window.localStorage.getItem(storageKey) || 'null') as unknown
    if (data && typeof data === 'object' && 'lots' in data && 'holds' in data && Array.isArray(data.lots) && Array.isArray(data.holds)) { state.value = data as MockState; return }
  } catch { /* use fixtures */ }
  state.value = seed()
}
function chooseLookup(value: string): void { if (lookupField.value) filters[lookupField.value] = value; lookupField.value = null }
function query(): void {
  errorText.value = ''; lookupField.value = null
  if (!dev.value) { errorText.value = '此頁目前僅提供 DEV 假資料預覽。'; return }
  for (const field of fields) filters[field.key] = filters[field.key].trim().toUpperCase()
  if (!Object.values(filters).some(Boolean)) { errorText.value = '請至少輸入一個查詢條件。'; return }
  rows.value = state.value.lots.filter(item => !item.partialMode && [0, 1, 4].includes(item.status) && fields.every(field => !filters[field.key] || item[field.key] === filters[field.key])).sort((a, b) => a.lotNo.localeCompare(b.lotNo))
  selectedLotNos.value = []; selectedOnly.value = false; searched.value = true; message.value = `查得 ${rows.value.length} 筆 Lot。`
}
function scanLot(): void {
  const value = lotScan.value.trim().toUpperCase()
  if (!value) return
  if (!rows.value.some(item => item.lotNo === value)) { errorText.value = `${value} 不在目前查詢結果`; return }
  if (!selectedLotNos.value.includes(value)) selectedLotNos.value.push(value)
  lotScan.value = ''; errorText.value = ''
}
function setHoldPs(value: string): void { holdPsNo.value = value; selectedOps.value = []; acceptMissingFirstGroup.value = false; psLookupOpen.value = false }
function setHoldPsFromEvent(event: Event): void { setHoldPs((event.target as HTMLSelectElement).value) }
function setMClassFromEvent(event: Event): void { setMClass((event.target as HTMLSelectElement).value) }
function setSClassFromEvent(event: Event): void { setSClass((event.target as HTMLSelectElement).value) }
function setMClass(value: string): void { mClassNo.value = value; sClassNo.value = ''; holdDescription.value = ''; selectedHoldItemNos.value = []; acceptMissingFirstGroup.value = false }
function setSClass(value: string): void { sClassNo.value = value; selectedHoldItemNos.value = [] }
function scanHoldItem(): void {
  const value = holdItemScan.value.trim().toUpperCase()
  if (!value) return
  if (!visibleHoldItems.value.some(item => item.holdItemNo === value)) { errorText.value = `${value} 不在目前 Hold Item 結果`; return }
  if (!selectedHoldItemNos.value.includes(value)) selectedHoldItemNos.value.push(value)
  holdItemScan.value = ''; errorText.value = ''
}
function resetHoldData(): void {
  selectedLotNos.value = []; selectedOnly.value = false; holdTime.value = 0; selectedOps.value = []; mClassNo.value = ''; sClassNo.value = ''; holdDescription.value = ''; selectedHoldItemNos.value = []; acceptMissingFirstGroup.value = false
}
function confirm(): void {
  errorText.value = ''
  if (!dev.value) return
  if (!selectedLotNos.value.length) { errorText.value = '請選擇 Lot。'; return }
  if (!holdPsNo.value || !selectedOps.value.length || !mClassNo.value || !sClassNo.value || !holdDescription.value.trim() || !selectedHoldItemNos.value.length) { errorText.value = '請填 PS No、OP、MClass No、SClass No、Hold Description，並勾選 Hold Item。'; return }
  if (missingFirstGroup.value.length && !acceptMissingFirstGroup.value) { errorText.value = `${missingFirstGroup.value.join('、')} 未設定第一處理群組；請確認後繼續。`; return }
  const selected = rows.value.filter(item => selectedLotNos.value.includes(item.lotNo))
  if (selected.length !== selectedLotNos.value.length) { errorText.value = 'Lot 選取資料已變更，請重新查詢。'; return }
  let current: MockState
  try {
    const data = JSON.parse(window.localStorage.getItem(storageKey) || 'null') as unknown
    current = data && typeof data === 'object' && 'lots' in data && 'holds' in data && Array.isArray(data.lots) && Array.isArray(data.holds) ? data as MockState : seed()
  } catch { errorText.value = '無法讀取 DEV 狀態'; return }
  for (const snapshot of selected) {
    const live = current.lots.find(item => item.lotNo === snapshot.lotNo)
    if (!live || live.lotStamp !== snapshot.lotStamp || live.partialMode || ![0, 1, 4].includes(live.status)) { errorText.value = `${snapshot.lotNo} 狀態已變更`; return }
    if (live.packed) { errorText.value = `${snapshot.lotNo} 已包裝，不能設定預先異常`; return }
  }
  const targetSet = new Set(selected.map(item => item.lotNo))
  const opSet = new Set(selectedOps.value)
  current.holds = current.holds.filter(item => !(targetSet.has(item.lotNo) && opSet.has(item.opNo) && item.holdTime === holdTime.value && item.moduleNo === 'N/A' && item.moduleVersion === 'N/A'))
  const now = new Date().toISOString()
  for (const lot of selected) for (const opNo of selectedOps.value) current.holds.push({ lotNo: lot.lotNo, opNo, holdTime: holdTime.value, moduleNo: 'N/A', moduleVersion: 'N/A', futureHoldSerial: 'N/A', mClassNo: mClassNo.value, holdDescription: holdDescription.value.trim(), holdItemNos: [...selectedHoldItemNos.value], creator: props.currentUserNo || 'DEV', createdAt: now })
  try { window.localStorage.setItem(storageKey, JSON.stringify(current)) } catch { errorText.value = '瀏覽器無法儲存 DEV 狀態'; return }
  state.value = current; const count = selected.length * selectedOps.value.length; resetHoldData()
  message.value = `DEV 已模擬設定 ${count} 組 Lot × OP 預先異常，未更新 MES。`
}
onMounted(load)
watch(dev, enabled => { if (enabled) load(); else { rows.value = []; selectedLotNos.value = []; searched.value = false } })
</script>

<template>
  <section class="wp504-page">
    <div v-if="dev" class="dev-banner">DEV 假資料：Area A01 可查兩筆可設定批與一筆已包裝批；包裝預檢會逐筆執行。</div>
    <h1>預先異常設定 <small>FUTURE HOLD SET · WP504</small></h1>
    <p v-if="errorText" class="notice error" role="alert">{{ errorText }}</p><p v-else class="notice" role="status">{{ message }}</p>
    <fieldset><legend>Query</legend><div class="query-grid"><label v-for="field in fields" :key="field.key">{{ field.label }}<input v-model="filters[field.key]" class="mes-input yellow" :disabled="!dev" @keydown.enter.prevent="query"><button v-if="field.lookup" type="button" class="lookup-button" :disabled="!dev" @click="lookupField = lookupField === field.key ? null : field.key">...</button></label><button type="button" class="mes-button" :disabled="!dev" @click="query">Query</button></div><div v-if="lookupField" class="lookup-options"><strong>{{ lookupField }} 查找</strong><button v-for="option in lookupOptions" :key="option" type="button" @click="chooseLookup(option)">{{ option }}</button></div></fieldset>
    <fieldset class="lot-frame"><legend>Lot State</legend><div class="lot-tools"><label><input v-model="selectedOnly" type="checkbox" :disabled="!dev"> Selected Lot</label><label>LotNo <input v-model="lotScan" class="mes-input yellow" :disabled="!dev" @keydown.enter.prevent="scanLot"></label><button type="button" class="mes-button" :disabled="!dev" @click="scanLot">Scan</button></div><div class="grid-wrap"><table><thead><tr><th>Check</th><th>LotNo</th><th>OPNo</th><th>OPName</th><th>CurQty</th><th>Status</th><th>Product No</th><th>Customer No</th></tr></thead><tbody><tr v-for="item in visibleRows" :key="item.lotNo"><td><input v-model="selectedLotNos" type="checkbox" :value="item.lotNo" :disabled="!dev"></td><td>{{ item.lotNo }}</td><td>{{ item.opNo }}</td><td>{{ item.opName }}</td><td class="num">{{ item.curQty }}</td><td>{{ statusText[item.status] }}</td><td>{{ item.productNo }}</td><td>{{ item.customerNo }}</td></tr><tr v-if="!visibleRows.length"><td colspan="8" class="empty">{{ searched ? '沒有符合條件的 Lot。' : '請先查詢。' }}</td></tr></tbody></table></div></fieldset>
    <fieldset class="hold-frame"><legend>Hold Data</legend><div class="hold-grid"><div class="hold-left"><div class="hold-time">Hold Time <span><label><input v-model="holdTime" type="radio" :value="0" :disabled="!dev"> Start</label><label><input v-model="holdTime" type="radio" :value="1" :disabled="!dev"> End</label></span></div><label>PS No <select :value="holdPsNo" class="mes-input yellow" :disabled="!dev" @change="setHoldPsFromEvent"><option value="">請選擇</option><option v-for="ps in Object.keys(psOps)" :key="ps">{{ ps }}</option></select><button type="button" class="lookup-button" :disabled="!dev" @click="psLookupOpen = !psLookupOpen">...</button></label><div v-if="psLookupOpen" class="ps-lookup"><button v-for="ps in Object.keys(psOps)" :key="ps" type="button" @click="setHoldPs(ps)">{{ ps }}</button></div><div class="op-list"><strong>OPNo</strong><label v-for="op in opOptions" :key="op.opNo"><input v-model="selectedOps" type="checkbox" :value="op.opNo" :disabled="!dev">{{ op.opNo }} · {{ op.opName }}</label><span v-if="!opOptions.length">先選 PS No</span></div></div><div class="hold-right"><div class="class-fields"><label>MClass No <select :value="mClassNo" class="mes-input yellow" :disabled="!dev" @change="setMClassFromEvent"><option value="">請選擇</option><option v-for="item in mClasses" :key="item">{{ item }}</option></select></label><label>SClass No <select :value="sClassNo" class="mes-input yellow" :disabled="!dev || !mClassNo" @change="setSClassFromEvent"><option value="">請選擇</option><option v-for="item in sClasses" :key="item">{{ item }}</option></select></label></div><label class="description">Hold Description <textarea v-model="holdDescription" class="yellow" :disabled="!dev"></textarea></label><div class="description-options"><button v-for="item in descriptions[mClassNo] || []" :key="item" type="button" :disabled="!dev" @click="holdDescription = item">{{ item }}</button></div><div class="hold-item-tools"><strong>Hold Item</strong><label>HoldItemNo <input v-model="holdItemScan" class="mes-input yellow" :disabled="!dev" @keydown.enter.prevent="scanHoldItem"></label><button type="button" class="mes-button" :disabled="!dev" @click="scanHoldItem">Scan</button></div><div class="hold-item-wrap"><table><thead><tr><th>Check</th><th>HoldItemNo</th><th>HoldItemName</th></tr></thead><tbody><tr v-for="item in visibleHoldItems" :key="item.holdItemNo"><td><input v-model="selectedHoldItemNos" type="checkbox" :value="item.holdItemNo" :disabled="!dev"></td><td>{{ item.holdItemNo }}</td><td>{{ item.holdItemName }}</td></tr><tr v-if="!visibleHoldItems.length"><td colspan="3" class="empty">選擇 MClass／SClass</td></tr></tbody></table></div></div></div><label v-if="missingFirstGroup.length && mClassNo" class="first-group"><input v-model="acceptMissingFirstGroup" type="checkbox" :disabled="!dev"> {{ missingFirstGroup.join('、') }} 未設定第一處理群組，仍繼續</label><div class="footer"><button type="button" class="mes-button primary" :disabled="!dev" @click="confirm">Confirm (C)</button><button type="button" class="mes-button" @click="emit('close')">Close (X)</button></div></fieldset>
  </section>
</template>

<style scoped>
.wp504-page{height:100%;min-height:0;box-sizing:border-box;display:flex;flex-direction:column;gap:7px;padding:9px;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif}.dev-banner{padding:6px 9px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}h1{margin:0;font-size:18px}h1 small{font-size:12px;font-weight:400;color:#777}.notice{margin:0;min-height:18px}.notice.error{padding:5px 8px;border:1px solid #d6aaa6;background:#fff0ef;color:#99251e}fieldset{position:relative;min-width:0;margin:0;padding:7px 8px;border:1px solid #bbb;background:#fff}legend{padding:0 5px}.query-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:6px}.query-grid label{display:flex;align-items:center;justify-content:flex-end;gap:4px}.query-grid .mes-input{width:105px}.mes-input{height:26px;min-width:0;padding:3px 5px;box-sizing:border-box;border:1px solid #888;font:inherit}.yellow{background:#fff0b3!important}.mes-button,.lookup-button{min-height:29px;padding:3px 10px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#f2f2f2);color:#333;font:inherit;cursor:pointer}.lookup-button{min-width:27px;padding:1px}.mes-button:disabled,.lookup-button:disabled{color:#999;cursor:default}.mes-button.primary{background:#f3a62a;border-color:#e99512;color:#fff}.lookup-options{position:absolute;z-index:5;top:100%;left:8px;display:flex;align-items:center;flex-wrap:wrap;gap:6px;padding:7px;border:1px solid #aaa;background:#fffbe9;box-shadow:0 3px 9px #0002}.lookup-options button,.description-options button{padding:3px 7px;border:1px solid #bbb;background:#fff;cursor:pointer}.lot-frame{flex:1;min-height:180px;display:flex;flex-direction:column}.lot-tools,.hold-item-tools{display:flex;align-items:center;gap:10px;padding-bottom:6px}.grid-wrap{flex:1;overflow:auto;min-height:90px;border:1px solid #aaa}.grid-wrap table,.hold-item-wrap table{width:100%;border-collapse:collapse;white-space:nowrap}.grid-wrap th,.hold-item-wrap th{position:sticky;top:0;padding:5px 7px;border:1px solid #d4d4d4;background:#ead591;text-align:left;font-weight:500}.grid-wrap td,.hold-item-wrap td{padding:4px 6px;border:1px solid #d4d4d4;background:#ffe89c}.num{text-align:right}.empty{text-align:center!important;background:#fff!important;color:#777;height:33px}.hold-frame{min-height:240px}.hold-grid{display:grid;grid-template-columns:38% 62%;gap:8px}.hold-left,.hold-right{display:flex;flex-direction:column;gap:6px}.hold-left>label{display:flex;align-items:center;gap:8px}.hold-time{display:flex;align-items:center;gap:8px}.hold-time>span{display:flex;gap:12px}.ps-lookup{display:flex;gap:6px}.ps-lookup button{padding:3px 7px;border:1px solid #bbb;background:#fffbe9;cursor:pointer}.op-list{display:flex;flex-direction:column;gap:5px;max-height:120px;overflow:auto;padding:6px;border:1px solid #aaa}.class-fields{display:flex;gap:12px}.description{display:flex;align-items:flex-start;gap:8px}.description textarea{flex:1;min-height:47px;padding:4px;border:1px solid #888;font:inherit}.description-options{display:flex;flex-wrap:wrap;gap:5px}.hold-item-wrap{max-height:115px;overflow:auto;border:1px solid #aaa}.first-group{display:block;margin-top:5px;color:#754e09}.footer{display:flex;justify-content:flex-end;gap:8px;padding-top:7px}@media(max-width:850px){.query-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.hold-grid{grid-template-columns:1fr}}
</style>
