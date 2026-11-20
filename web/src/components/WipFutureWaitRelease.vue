<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'

type Lot = { lotNo: string; opNo: string; curQty: number; status: 0 | 1 | 4; productNo: string; customerNo: string; areaNo: string; psNo: string; partialMode: boolean; packed: boolean; lotStamp: number }
type Wait = { lotNo: string; opNo: string; waitTime: 0 | 1; description: string; creator: string; createdAt: string }
type MockState = { lots: Lot[]; waits: Wait[] }
type TextFilterKey = 'lotNo' | 'opNo'

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview !== false)
const storageKey = 'ymes-dev-mock:wip-future-wait-set:state:v1'
const state = ref<MockState>({ lots: [], waits: [] })
const filters = reactive<Record<TextFilterKey, string>>({ lotNo: '', opNo: '' })
const checked = reactive<Record<TextFilterKey, boolean>>({ lotNo: false, opNo: false })
const waitTimeEnabled = ref(false); const waitTime = ref<0 | 1 | null>(null)
const lookupOpen = ref(false)
const opOptions = computed(() => [...new Set([...state.value.lots.map(item => item.opNo), ...state.value.waits.map(item => item.opNo)].filter(Boolean))].sort())
const rows = ref<Wait[]>([]); const selectedKey = ref(''); const searched = ref(false)
const selected = computed(() => rows.value.find(item => rowKey(item) === selectedKey.value) || null)
const errorText = ref(''); const message = ref('按 Query 查詢預先等待，選取一筆後按 Release。')

function seed(): MockState {
  return {
    lots: [
      { lotNo: 'DEV-FW-001', opNo: 'OP-CUT', curQty: 8, status: 0, productNo: 'PRD-A', customerNo: 'CUS-A', areaNo: 'A01', psNo: 'PS-01', partialMode: false, packed: false, lotStamp: 50501 },
      { lotNo: 'DEV-FW-002', opNo: 'OP-CUT', curQty: 6, status: 1, productNo: 'PRD-A', customerNo: 'CUS-A', areaNo: 'A01', psNo: 'PS-01', partialMode: false, packed: false, lotStamp: 50502 },
      { lotNo: 'DEV-FW-PACK', opNo: 'OP-CUT', curQty: 4, status: 4, productNo: 'PRD-A', customerNo: 'CUS-A', areaNo: 'A01', psNo: 'PS-01', partialMode: false, packed: true, lotStamp: 50503 },
      { lotNo: 'DEV-FW-PART', opNo: 'OP-PACK', curQty: 3, status: 0, productNo: 'PRD-B', customerNo: 'CUS-B', areaNo: 'A02', psNo: 'PS-02', partialMode: true, packed: false, lotStamp: 50504 },
    ],
    waits: [
      { lotNo: 'DEV-FW-001', opNo: 'OP-CUT', waitTime: 0, description: '製程確認', creator: 'DEV', createdAt: '2026-11-20T08:00:00' },
      { lotNo: 'DEV-FW-002', opNo: 'OP-TEST', waitTime: 1, description: '', creator: 'DEV', createdAt: '2026-11-20T09:00:00' },
    ],
  }
}
function load(): void {
  if (!dev.value) return
  try {
    const data = JSON.parse(window.localStorage.getItem(storageKey) || 'null') as unknown
    if (data && typeof data === 'object' && 'lots' in data && 'waits' in data && Array.isArray(data.lots) && Array.isArray(data.waits)) { state.value = data as MockState; return }
  } catch { /* use fixtures */ }
  state.value = seed()
}
function rowKey(item: Wait): string { return [item.lotNo, item.opNo, item.waitTime].join('|') }
function setFilterEnabled(key: TextFilterKey): void { if (!checked[key]) filters[key] = '' }
function setWaitTimeEnabled(): void { if (!waitTimeEnabled.value) waitTime.value = null }
function chooseOp(opNo: string): void { filters.opNo = opNo; checked.opNo = true; lookupOpen.value = false }
function query(): void {
  errorText.value = ''; lookupOpen.value = false
  if (!dev.value) { errorText.value = '此頁目前僅提供 DEV 假資料預覽。'; return }
  filters.lotNo = filters.lotNo.trim().toUpperCase(); filters.opNo = filters.opNo.trim().toUpperCase()
  rows.value = state.value.waits.filter(item =>
    (!checked.lotNo || !filters.lotNo || item.lotNo === filters.lotNo) &&
    (!checked.opNo || !filters.opNo || item.opNo === filters.opNo) &&
    (!waitTimeEnabled.value || waitTime.value === null || item.waitTime === waitTime.value))
    .sort((a, b) => a.lotNo.localeCompare(b.lotNo) || a.opNo.localeCompare(b.opNo) || a.waitTime - b.waitTime)
  selectedKey.value = ''; searched.value = true; message.value = `查得 ${rows.value.length} 筆預先等待。`
}
function release(): void {
  errorText.value = ''
  if (!dev.value || !selected.value) { errorText.value = '請先選取一筆預先等待。'; return }
  const picked = selected.value
  let current: MockState
  try {
    const data = JSON.parse(window.localStorage.getItem(storageKey) || 'null') as unknown
    current = data && typeof data === 'object' && 'lots' in data && 'waits' in data && Array.isArray(data.lots) && Array.isArray(data.waits) ? data as MockState : seed()
  } catch { errorText.value = '無法讀取 DEV 狀態'; return }
  const live = current.waits.find(item => rowKey(item) === rowKey(picked))
  if (!live || live.createdAt !== picked.createdAt || live.description !== picked.description) { errorText.value = '設定已變更，請重新查詢。'; return }
  current.waits = current.waits.filter(item => rowKey(item) !== rowKey(picked))
  try { window.localStorage.setItem(storageKey, JSON.stringify(current)) } catch { errorText.value = '瀏覽器無法儲存 DEV 狀態'; return }
  state.value = current; query(); message.value = `DEV 已模擬解除 ${picked.lotNo} / ${picked.opNo} 的 ${picked.waitTime === 0 ? 'Start' : 'End'} 預先等待，未更新 MES。`
}
onMounted(load)
watch(dev, enabled => { if (enabled) load(); else { rows.value = []; selectedKey.value = ''; searched.value = false } })
</script>

<template>
  <section class="wp507-page">
    <div v-if="dev" class="dev-banner">DEV 假資料；若已在 WP505 設定，會讀取同一份本機資料。Release 依 Lot/OP/WaitTime 解除一筆。</div>
    <h1>預先等待解除 <small>FUTURE WAIT RELEASE · WP507</small></h1>
    <p v-if="errorText" class="notice error" role="alert">{{ errorText }}</p><p v-else class="notice" role="status">{{ message }}</p>
    <fieldset class="query-frame">
      <legend>Query</legend>
      <div class="query-fields">
        <label class="lot-field"><input v-model="checked.lotNo" type="checkbox" :disabled="!dev" @change="setFilterEnabled('lotNo')"> Lot No <input v-model="filters.lotNo" class="mes-input yellow" :disabled="!dev" @keydown.enter.prevent="query"></label>
        <label class="op-field"><input v-model="checked.opNo" type="checkbox" :disabled="!dev" @change="setFilterEnabled('opNo')"> OP No <input v-model="filters.opNo" class="mes-input yellow" :disabled="!dev" @keydown.enter.prevent="query"><button type="button" class="lookup-button" :disabled="!dev" @click="lookupOpen = !lookupOpen">...</button></label>
        <div class="time-field"><label><input v-model="waitTimeEnabled" type="checkbox" :disabled="!dev" @change="setWaitTimeEnabled"> Wait Time</label><span><label><input v-model="waitTime" type="radio" :value="0" :disabled="!dev" @change="waitTimeEnabled = true"> Start</label><label><input v-model="waitTime" type="radio" :value="1" :disabled="!dev" @change="waitTimeEnabled = true"> End</label></span></div>
        <button type="button" class="mes-button query-button" :disabled="!dev" @click="query">Query</button>
      </div>
      <div v-if="lookupOpen" class="lookup-options"><button v-for="opNo in opOptions" :key="opNo" type="button" @click="chooseOp(opNo)">{{ opNo }}</button></div>
    </fieldset>
    <fieldset class="data-frame">
      <legend>Future Wait Data</legend>
      <div class="data-main">
        <div class="grid-wrap"><table><thead><tr><th>Lot No.</th><th>OP No.</th><th>Wait Time</th><th>Description</th><th>Creator</th><th>Create Date</th></tr></thead><tbody><tr v-for="item in rows" :key="rowKey(item)" :class="{active: selectedKey === rowKey(item)}" tabindex="0" @click="selectedKey = rowKey(item)" @keydown.enter="selectedKey = rowKey(item)"><td>{{ item.lotNo }}</td><td>{{ item.opNo }}</td><td>{{ item.waitTime === 0 ? 'Start' : 'End' }}</td><td>{{ item.description }}</td><td>{{ item.creator }}</td><td>{{ item.createdAt.slice(0,16).replace('T',' ') }}</td></tr><tr v-if="!rows.length"><td colspan="6" class="empty">{{ searched ? '沒有符合條件的預先等待。' : '請按 Query。' }}</td></tr></tbody></table></div>
        <div class="actions"><button type="button" class="mes-button primary" :disabled="!dev || !selected" @click="release">Release</button><button type="button" class="mes-button" @click="emit('close')">Close (X)</button></div>
      </div>
      <p class="count">資料總筆數 {{ rows.length }}</p>
    </fieldset>
  </section>
</template>

<style scoped>
.wp507-page{height:100%;min-height:0;box-sizing:border-box;display:flex;flex-direction:column;gap:7px;padding:9px;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif}
.dev-banner{padding:6px 9px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}h1{margin:0;font-size:18px}h1 small{font-size:12px;font-weight:400;color:#777}.notice{margin:0;min-height:18px}.notice.error{padding:5px 8px;border:1px solid #d6aaa6;background:#fff0ef;color:#99251e}
fieldset{position:relative;min-width:0;margin:0;padding:7px 8px;border:1px solid #bbb;background:#fff}legend{padding:0 5px}
.query-fields{display:grid;grid-template-columns:300px 310px minmax(10px,1fr) 86px;grid-template-areas:"lot op . ." "time . . query";align-items:center;gap:7px 12px}.query-fields>label{display:flex;align-items:center;gap:4px;white-space:nowrap}.lot-field{grid-area:lot}.op-field{grid-area:op}.time-field{grid-area:time;display:flex;align-items:center;gap:5px;white-space:nowrap}.time-field>span{display:flex;gap:9px;padding:4px 8px;border:1px solid #bbb}.query-button{grid-area:query}.query-fields .mes-input{width:125px;margin-left:auto}
.mes-input{height:26px;min-width:0;padding:3px 5px;box-sizing:border-box;border:1px solid #888;font:inherit}.yellow{background:#fff0b3!important}.mes-button,.lookup-button{min-height:29px;padding:3px 10px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#f2f2f2);color:#333;font:inherit;cursor:pointer}.lookup-button{min-width:27px;padding:1px}.mes-button:disabled,.lookup-button:disabled{color:#999;cursor:default}.mes-button.primary{background:#f3a62a;border-color:#e99512;color:#fff}
.lookup-options{position:absolute;z-index:5;top:100%;left:315px;display:flex;gap:5px;padding:7px;border:1px solid #aaa;background:#fffbe9;box-shadow:0 3px 9px #0002}.lookup-options button{padding:3px 7px;border:1px solid #bbb;background:#fff;cursor:pointer}.data-frame{flex:1;min-height:200px;display:flex;flex-direction:column}.data-main{display:flex;flex:1;min-height:0;gap:10px}.grid-wrap{flex:1;min-width:0;min-height:100px;overflow:auto;border:1px solid #aaa}.grid-wrap table{width:100%;min-width:650px;border-collapse:collapse;white-space:nowrap}.grid-wrap th{position:sticky;top:0;padding:5px 7px;border:1px solid #d4d4d4;background:#ead591;text-align:left;font-weight:500}.grid-wrap td{padding:4px 6px;border:1px solid #d4d4d4;background:#ffe89c}.grid-wrap tr.active td{background:#f4ca68}.grid-wrap tr{cursor:pointer}.empty{text-align:center!important;background:#fff!important;color:#777;height:33px}.actions{display:flex;flex-direction:column;justify-content:space-between;gap:8px}.actions .mes-button{width:86px}.count{margin:4px 0 0;color:#777}
@media(max-width:810px){.query-fields{grid-template-columns:minmax(0,1fr) minmax(0,1fr);grid-template-areas:"lot op" "time ." "query query"}.query-button{justify-self:end;width:86px}}
</style>
