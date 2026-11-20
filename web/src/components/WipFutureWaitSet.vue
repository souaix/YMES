<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'

type FilterKey = 'areaNo' | 'psNo' | 'opNo' | 'customerNo' | 'productNo' | 'lotNo'
type Lot = { lotNo: string; opNo: string; curQty: number; status: 0 | 1 | 4; productNo: string; customerNo: string; areaNo: string; psNo: string; partialMode: boolean; packed: boolean; lotStamp: number }
type FutureWait = { lotNo: string; opNo: string; waitTime: 0 | 1; description: string; creator: string; createdAt: string }
type MockState = { lots: Lot[]; waits: FutureWait[] }

const props = defineProps<{ developmentPreview?: boolean; currentUserNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview !== false)
const unavailableText = '正式環境尚未開放此功能。'
const storageKey = 'ymes-dev-mock:wip-future-wait-set:state:v1'
const fields: { key: FilterKey; label: string; lookup: boolean }[] = [
  { key: 'areaNo', label: 'Area No', lookup: true }, { key: 'psNo', label: 'PS No', lookup: true }, { key: 'opNo', label: 'OP No', lookup: true },
  { key: 'customerNo', label: 'Customer No', lookup: true }, { key: 'productNo', label: 'Product No', lookup: true }, { key: 'lotNo', label: 'LotNo', lookup: false },
]
const statuses: Record<Lot['status'], string> = { 0: 'Queue', 1: 'Running', 4: 'Complete' }
const psOps: Record<string, { opNo: string; opName: string }[]> = {
  'PS-01': [{ opNo: 'OP-CUT', opName: '切割' }, { opNo: 'OP-TEST', opName: '測試' }],
  'PS-02': [{ opNo: 'OP-PACK', opName: '包裝' }],
}
const fixtures: Lot[] = [
  { lotNo: 'DEV-FW-001', opNo: 'OP-CUT', curQty: 8, status: 0, productNo: 'PRD-A', customerNo: 'CUS-A', areaNo: 'A01', psNo: 'PS-01', partialMode: false, packed: false, lotStamp: 50501 },
  { lotNo: 'DEV-FW-002', opNo: 'OP-CUT', curQty: 6, status: 1, productNo: 'PRD-A', customerNo: 'CUS-A', areaNo: 'A01', psNo: 'PS-01', partialMode: false, packed: false, lotStamp: 50502 },
  { lotNo: 'DEV-FW-PACK', opNo: 'OP-CUT', curQty: 4, status: 4, productNo: 'PRD-A', customerNo: 'CUS-A', areaNo: 'A01', psNo: 'PS-01', partialMode: false, packed: true, lotStamp: 50503 },
  { lotNo: 'DEV-FW-PART', opNo: 'OP-PACK', curQty: 3, status: 0, productNo: 'PRD-B', customerNo: 'CUS-B', areaNo: 'A02', psNo: 'PS-02', partialMode: true, packed: false, lotStamp: 50504 },
]

const state = ref<MockState>({ lots: [], waits: [] })
const filters = reactive<Record<FilterKey, string>>({ areaNo: '', psNo: '', opNo: '', customerNo: '', productNo: '', lotNo: '' })
const lookupField = ref<FilterKey | null>(null)
const lookupOptions = computed(() => dev.value && lookupField.value ? [...new Set(state.value.lots.map(item => item[lookupField.value!]))].sort() : [])
const rows = ref<Lot[]>([]); const selectedLotNos = ref<string[]>([]); const selectedOnly = ref(false); const searched = ref(false); const lotScan = ref('')
const visibleRows = computed(() => selectedOnly.value ? rows.value.filter(item => selectedLotNos.value.includes(item.lotNo)) : rows.value)
const waitTime = ref<0 | 1>(0); const waitPsNo = ref(''); const selectedOps = ref<string[]>([]); const description = ref(''); const psLookupOpen = ref(false)
const psOptions = computed(() => dev.value ? Object.keys(psOps) : [])
const opOptions = computed(() => dev.value ? psOps[waitPsNo.value] || [] : [])
const initialMessage = '請輸入查詢條件，選擇 Lot 與 Wait Data 後按 Confirm。'
const errorText = ref(''); const message = ref(dev.value ? initialMessage : unavailableText)
function unavailable(): boolean { if (dev.value) return false; errorText.value = ''; message.value = unavailableText; return true }

function seed(): MockState { return { lots: fixtures.map(item => ({ ...item })), waits: [] } }
function load(): void { if (unavailable()) return;
  try {
    const data = JSON.parse(window.localStorage.getItem(storageKey) || 'null') as unknown
    if (data && typeof data === 'object' && 'lots' in data && 'waits' in data && Array.isArray(data.lots) && Array.isArray(data.waits)) { state.value = data as MockState; return }
  } catch { /* use fixtures */ }
  state.value = seed()
}
function chooseLookup(value: string): void { if (unavailable()) return; if (lookupField.value) filters[lookupField.value] = value; lookupField.value = null }
function query(): void { if (unavailable()) return;
  errorText.value = ''; lookupField.value = null
  for (const field of fields) filters[field.key] = filters[field.key].trim().toUpperCase()
  if (!Object.values(filters).some(Boolean)) { errorText.value = '請至少輸入一個查詢條件。'; return }
  rows.value = state.value.lots.filter(item => !item.partialMode && [0, 1, 4].includes(item.status) && fields.every(field => !filters[field.key] || item[field.key] === filters[field.key])).sort((a, b) => a.lotNo.localeCompare(b.lotNo))
  selectedLotNos.value = []; selectedOnly.value = false; searched.value = true; message.value = `查得 ${rows.value.length} 筆 Lot。`
}
function scanLot(): void { if (unavailable()) return;
  const value = lotScan.value.trim().toUpperCase()
  if (!value) return
  if (!rows.value.some(item => item.lotNo === value)) { errorText.value = `${value} 不在目前查詢結果`; return }
  if (!selectedLotNos.value.includes(value)) selectedLotNos.value.push(value)
  lotScan.value = ''; errorText.value = ''
}
function setWaitPs(value: string): void { if (unavailable()) return; waitPsNo.value = value; selectedOps.value = []; psLookupOpen.value = false }
function setWaitPsFromEvent(event: Event): void { if (unavailable()) return; setWaitPs((event.target as HTMLSelectElement).value) }
function resetWaitData(): void { if (unavailable()) return; selectedLotNos.value = []; selectedOnly.value = false; waitTime.value = 0; selectedOps.value = []; description.value = '' }
function confirm(): void { if (unavailable()) return;
  errorText.value = ''
  if (!selectedLotNos.value.length) { errorText.value = '請選擇 Lot。'; return }
  if (!selectedOps.value.length) { errorText.value = '請勾選至少一個 OP。'; return }
  if (description.value.length > 3000) { errorText.value = 'Description 不可超過 3000 字元。'; return }
  const selected = rows.value.filter(item => selectedLotNos.value.includes(item.lotNo))
  if (selected.length !== selectedLotNos.value.length) { errorText.value = 'Lot 選取資料已變更，請重新查詢。'; return }
  let current: MockState
  try {
    const data = JSON.parse(window.localStorage.getItem(storageKey) || 'null') as unknown
    current = data && typeof data === 'object' && 'lots' in data && 'waits' in data && Array.isArray(data.lots) && Array.isArray(data.waits) ? data as MockState : seed()
  } catch { errorText.value = '無法讀取 DEV 狀態'; return }
  for (const snapshot of selected) {
    const live = current.lots.find(item => item.lotNo === snapshot.lotNo)
    if (!live || live.lotStamp !== snapshot.lotStamp || live.partialMode || ![0, 1, 4].includes(live.status)) { errorText.value = `${snapshot.lotNo} 狀態已變更`; return }
    if (live.packed) { errorText.value = `${snapshot.lotNo} 已包裝，不能設定預先等待`; return }
  }
  const targetSet = new Set(selected.map(item => item.lotNo)); const opSet = new Set(selectedOps.value)
  current.waits = current.waits.filter(item => !(targetSet.has(item.lotNo) && opSet.has(item.opNo) && item.waitTime === waitTime.value))
  const now = new Date().toISOString()
  for (const lot of selected) for (const opNo of selectedOps.value) current.waits.push({ lotNo: lot.lotNo, opNo, waitTime: waitTime.value, description: description.value, creator: props.currentUserNo || 'DEV', createdAt: now })
  try { window.localStorage.setItem(storageKey, JSON.stringify(current)) } catch { errorText.value = '瀏覽器無法儲存 DEV 狀態'; return }
  state.value = current; const count = selected.length * selectedOps.value.length; resetWaitData()
  message.value = `DEV 已模擬設定 ${count} 組 Lot × OP 預先等待，未更新 MES。`
}
onMounted(load)
watch(dev, enabled => { if (enabled) { errorText.value = ''; message.value = initialMessage; load() } else { state.value = { lots: [], waits: [] }; rows.value = []; selectedLotNos.value = []; selectedOnly.value = false; searched.value = false; lookupField.value = null; psLookupOpen.value = false; waitPsNo.value = ''; waitTime.value = 0; selectedOps.value = []; description.value = ''; lotScan.value = ''; for (const field of fields) filters[field.key] = ''; errorText.value = ''; message.value = unavailableText } })
</script>

<template>
  <section class="wp505-page">
    <div v-if="dev" class="dev-banner">DEV 假資料：Area A01 可查兩筆可設定批與一筆已包裝批；包裝預檢會逐筆執行。</div>
    <h1>預先等待設定 <small>FUTURE WAIT SET · WP505</small></h1>
    <p v-if="errorText" class="notice error" role="alert">{{ errorText }}</p><p v-else class="notice" role="status">{{ message }}</p>
    <fieldset><legend>Query</legend><div class="query-grid"><label v-for="field in fields" :key="field.key">{{ field.label }}<input v-model="filters[field.key]" class="mes-input yellow" :disabled="!dev" @keydown.enter.prevent="query"><button v-if="field.lookup" type="button" class="lookup-button" :disabled="!dev" @click="dev && (lookupField = lookupField === field.key ? null : field.key)">...</button></label><button type="button" class="mes-button" :disabled="!dev" @click="query">Query</button></div><div v-if="dev && lookupField" class="lookup-options"><strong>{{ lookupField }} 查找</strong><button v-for="option in lookupOptions" :key="option" type="button" @click="chooseLookup(option)">{{ option }}</button></div></fieldset>
    <fieldset class="lot-frame"><legend>Lot State</legend><div class="lot-tools"><label><input v-model="selectedOnly" type="checkbox" :disabled="!dev"> Selected Lot</label><label>LotNo <input v-model="lotScan" class="mes-input yellow" :disabled="!dev" @keydown.enter.prevent="scanLot"></label><button type="button" class="mes-button" :disabled="!dev" @click="scanLot">Scan</button></div><div class="grid-wrap"><table><thead><tr><th>Check</th><th>LotNo</th><th>CurQty</th><th>Status</th><th>Product No</th><th>Customer No</th></tr></thead><tbody><tr v-for="item in visibleRows" :key="item.lotNo"><td><input v-model="selectedLotNos" type="checkbox" :value="item.lotNo" :disabled="!dev"></td><td>{{ item.lotNo }}</td><td class="num">{{ item.curQty }}</td><td>{{ statuses[item.status] }}</td><td>{{ item.productNo }}</td><td>{{ item.customerNo }}</td></tr><tr v-if="!visibleRows.length"><td colspan="6" class="empty">{{ !dev ? unavailableText : searched ? '沒有符合條件的 Lot。' : '請先查詢。' }}</td></tr></tbody></table></div></fieldset>
    <fieldset class="wait-frame"><legend>Wait Data</legend><div class="wait-grid"><div class="wait-left"><div class="wait-time">Wait Time <span><label><input v-model="waitTime" type="radio" :value="0" :disabled="!dev"> Start</label><label><input v-model="waitTime" type="radio" :value="1" :disabled="!dev"> End</label></span></div><label>PS No <select :value="waitPsNo" class="mes-input yellow" :disabled="!dev" @change="setWaitPsFromEvent"><option value="">請選擇</option><option v-for="ps in psOptions" :key="ps">{{ ps }}</option></select><button type="button" class="lookup-button" :disabled="!dev" @click="dev && (psLookupOpen = !psLookupOpen)">...</button></label><div v-if="dev && psLookupOpen" class="ps-lookup"><button v-for="ps in psOptions" :key="ps" type="button" @click="setWaitPs(ps)">{{ ps }}</button></div><div class="op-list"><strong>OPNo</strong><label v-for="op in opOptions" :key="op.opNo"><input v-model="selectedOps" type="checkbox" :value="op.opNo" :disabled="!dev">{{ op.opNo }} · {{ op.opName }}</label><span v-if="!opOptions.length">先選 PS No</span></div></div><label class="description">Description <textarea v-model="description" class="yellow" :disabled="!dev"></textarea></label></div><div class="footer"><button type="button" class="mes-button primary" :disabled="!dev" @click="confirm">Confirm (C)</button><button type="button" class="mes-button" @click="emit('close')">Close (X)</button></div></fieldset>
  </section>
</template>

<style scoped>
.wp505-page{height:100%;min-height:0;box-sizing:border-box;display:flex;flex-direction:column;gap:7px;padding:9px;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif}.dev-banner{padding:6px 9px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}h1{margin:0;font-size:18px}h1 small{font-size:12px;font-weight:400;color:#777}.notice{margin:0;min-height:18px}.notice.error{padding:5px 8px;border:1px solid #d6aaa6;background:#fff0ef;color:#99251e}fieldset{position:relative;min-width:0;margin:0;padding:7px 8px;border:1px solid #bbb;background:#fff}legend{padding:0 5px}.query-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:6px}.query-grid label{display:flex;align-items:center;justify-content:flex-end;gap:4px}.query-grid .mes-input{width:105px}.mes-input{height:26px;min-width:0;padding:3px 5px;box-sizing:border-box;border:1px solid #888;font:inherit}.yellow{background:#fff0b3!important}.mes-button,.lookup-button{min-height:29px;padding:3px 10px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#f2f2f2);color:#333;font:inherit;cursor:pointer}.lookup-button{min-width:27px;padding:1px}.mes-button:disabled,.lookup-button:disabled{color:#999;cursor:default}.mes-button.primary{background:#f3a62a;border-color:#e99512;color:#fff}.lookup-options{position:absolute;z-index:5;top:100%;left:8px;display:flex;align-items:center;flex-wrap:wrap;gap:6px;padding:7px;border:1px solid #aaa;background:#fffbe9;box-shadow:0 3px 9px #0002}.lookup-options button{padding:3px 7px;border:1px solid #bbb;background:#fff;cursor:pointer}.lot-frame{flex:1;min-height:180px;display:flex;flex-direction:column}.lot-tools{display:flex;align-items:center;gap:10px;padding-bottom:6px}.grid-wrap{flex:1;overflow:auto;min-height:90px;border:1px solid #aaa}.grid-wrap table{width:100%;border-collapse:collapse;white-space:nowrap}.grid-wrap th{position:sticky;top:0;padding:5px 7px;border:1px solid #d4d4d4;background:#ead591;text-align:left;font-weight:500}.grid-wrap td{padding:4px 6px;border:1px solid #d4d4d4;background:#ffe89c}.num{text-align:right}.empty{text-align:center!important;background:#fff!important;color:#777;height:33px}.wait-frame{min-height:185px}.wait-grid{display:grid;grid-template-columns:40% 60%;gap:8px}.wait-left{display:flex;flex-direction:column;gap:6px}.wait-time,.wait-left>label{display:flex;align-items:center;gap:8px}.wait-time>span{display:flex;gap:12px}.ps-lookup{display:flex;gap:6px}.ps-lookup button{padding:3px 7px;border:1px solid #bbb;background:#fffbe9;cursor:pointer}.op-list{display:flex;flex-direction:column;gap:5px;max-height:115px;overflow:auto;padding:6px;border:1px solid #aaa}.description{display:flex;align-items:flex-start;gap:8px}.description textarea{flex:1;min-height:120px;padding:4px;border:1px solid #888;font:inherit}.footer{display:flex;justify-content:flex-end;gap:8px;padding-top:7px}@media(max-width:850px){.query-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.wait-grid{grid-template-columns:1fr}}
</style>
