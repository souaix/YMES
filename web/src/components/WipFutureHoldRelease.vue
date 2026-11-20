<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'

type Hold = { lotNo: string; opNo: string; holdTime: 0 | 1; moduleNo: string; moduleVersion: string; futureHoldSerial: string; mClassNo: string; holdDescription: string; holdItemNos: string[]; creator: string; createdAt: string; lockFlag?: boolean }
type Lot = { lotNo: string; moNo?: string; opNo: string; opName: string; curQty: number; status: 0 | 1 | 4; productNo: string; customerNo: string; areaNo: string; psNo: string; partialMode: boolean; packed: boolean; lotStamp: number }
type MockState = { lots: Lot[]; holds: Hold[] }
type FilterKey = 'opNo' | 'mClassNo'
type TextFilterKey = FilterKey | 'lotNo'

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview !== false)
const unavailableText = '正式環境尚未開放此功能。'
const storageKey = 'ymes-dev-mock:wip-future-hold-set:state:v1'
const state = ref<MockState>({ lots: [], holds: [] })
const filters = reactive<Record<TextFilterKey, string>>({ lotNo: '', opNo: '', mClassNo: '' })
const checked = reactive<Record<TextFilterKey, boolean>>({ lotNo: false, opNo: false, mClassNo: false })
const holdTimeEnabled = ref(false); const holdTime = ref<0 | 1 | null>(null)
const lookupField = ref<FilterKey | null>(null)
const lookupOptions = computed(() => dev.value && lookupField.value ? [...new Set(state.value.holds.map(item => item[lookupField.value!]).filter(Boolean))].sort() : [])
const rows = ref<Hold[]>([]); const selectedKey = ref(''); const searched = ref(false)
const selected = computed(() => rows.value.find(item => rowKey(item) === selectedKey.value) || null)
const initialMessage = '按 Query 查詢預先異常，選取一筆後按 Release。'
const errorText = ref(''); const message = ref(dev.value ? initialMessage : unavailableText)
function unavailable(): boolean { if (dev.value) return false; errorText.value = ''; message.value = unavailableText; return true }

function seed(): MockState {
  return {
    lots: [
      { lotNo: 'DEV-FH-001', opNo: 'OP-CUT', opName: '切割', curQty: 8, status: 0, productNo: 'PRD-A', customerNo: 'CUS-A', areaNo: 'A01', psNo: 'PS-01', partialMode: false, packed: false, lotStamp: 50401, moNo: 'MO-501' },
      { lotNo: 'DEV-FH-002', opNo: 'OP-CUT', opName: '切割', curQty: 6, status: 1, productNo: 'PRD-A', customerNo: 'CUS-A', areaNo: 'A01', psNo: 'PS-01', partialMode: false, packed: false, lotStamp: 50402, moNo: 'MO-501' },
      { lotNo: 'DEV-FH-PACK', opNo: 'OP-CUT', opName: '切割', curQty: 4, status: 4, productNo: 'PRD-A', customerNo: 'CUS-A', areaNo: 'A01', psNo: 'PS-01', partialMode: false, packed: true, lotStamp: 50403 },
      { lotNo: 'DEV-FH-PART', opNo: 'OP-PACK', opName: '包裝', curQty: 3, status: 0, productNo: 'PRD-B', customerNo: 'CUS-B', areaNo: 'A02', psNo: 'PS-02', partialMode: true, packed: false, lotStamp: 50404 },
    ],
    holds: [
      { lotNo: 'DEV-FH-001', opNo: 'OP-CUT', holdTime: 0, moduleNo: 'N/A', moduleVersion: 'N/A', futureHoldSerial: 'N/A', mClassNo: 'M-QUALITY', holdDescription: '外觀檢查異常', holdItemNos: ['H-001'], creator: 'DEV', createdAt: '2026-11-20T08:00:00', lockFlag: false },
      { lotNo: 'DEV-FH-002', opNo: 'OP-TEST', holdTime: 1, moduleNo: 'N/A', moduleVersion: 'N/A', futureHoldSerial: 'N/A', mClassNo: 'M-EQUIPMENT', holdDescription: '設備點檢異常', holdItemNos: ['H-003'], creator: 'DEV', createdAt: '2026-11-20T09:00:00', lockFlag: true },
    ],
  }
}
function load(): void { if (unavailable()) return;
  try {
    const data = JSON.parse(window.localStorage.getItem(storageKey) || 'null') as unknown
    if (data && typeof data === 'object' && 'lots' in data && 'holds' in data && Array.isArray(data.lots) && Array.isArray(data.holds)) { state.value = data as MockState; return }
  } catch { /* use fixtures */ }
  state.value = seed()
}
function rowKey(item: Hold): string { return [item.lotNo, item.opNo, item.holdTime, item.moduleNo, item.moduleVersion, item.futureHoldSerial].join('|') }
function setFilterEnabled(key: TextFilterKey): void { if (unavailable()) return; if (!checked[key]) filters[key] = '' }
function setHoldTimeEnabled(): void { if (unavailable()) return; if (!holdTimeEnabled.value) holdTime.value = null }
function chooseLookup(value: string): void { if (unavailable()) return; if (lookupField.value) { filters[lookupField.value] = value; checked[lookupField.value] = true }; lookupField.value = null }
function query(): void { if (unavailable()) return;
  errorText.value = ''; lookupField.value = null
  for (const key of ['lotNo', 'opNo', 'mClassNo'] as TextFilterKey[]) filters[key] = filters[key].trim().toUpperCase()
  const existingLots = new Set(state.value.lots.map(item => item.lotNo))
  rows.value = state.value.holds.filter(item => existingLots.has(item.lotNo) &&
    (['lotNo', 'opNo', 'mClassNo'] as TextFilterKey[]).every(key => !checked[key] || !filters[key] || item[key] === filters[key]) &&
    (!holdTimeEnabled.value || holdTime.value === null || item.holdTime === holdTime.value)).sort((a, b) => a.lotNo.localeCompare(b.lotNo) || a.opNo.localeCompare(b.opNo) || a.holdTime - b.holdTime)
  selectedKey.value = ''; searched.value = true; message.value = `查得 ${rows.value.length} 筆預先異常。`
}
function release(): void { if (unavailable()) return;
  errorText.value = ''
  if (!dev.value || !selected.value) { errorText.value = '請先選取一筆預先異常。'; return }
  const picked = selected.value
  let current: MockState
  try {
    const data = JSON.parse(window.localStorage.getItem(storageKey) || 'null') as unknown
    current = data && typeof data === 'object' && 'lots' in data && 'holds' in data && Array.isArray(data.lots) && Array.isArray(data.holds) ? data as MockState : seed()
  } catch { errorText.value = '無法讀取 DEV 狀態'; return }
  const scope = current.holds.filter(item => item.lotNo === picked.lotNo && item.opNo === picked.opNo && item.holdTime === picked.holdTime)
  if (!scope.some(item => rowKey(item) === rowKey(picked))) { errorText.value = '設定已變更，請重新查詢。'; return }
  if (scope.some(item => item.lockFlag)) { errorText.value = '此刪除範圍已有設定進入異常處置，不能解除。'; return }
  current.holds = current.holds.filter(item => !(item.lotNo === picked.lotNo && item.opNo === picked.opNo && item.holdTime === picked.holdTime))
  try { window.localStorage.setItem(storageKey, JSON.stringify(current)) } catch { errorText.value = '瀏覽器無法儲存 DEV 狀態'; return }
  state.value = current; const deleted = scope.length; query(); message.value = `DEV 已模擬解除 ${deleted} 筆同 Lot/OP/HoldTime 設定，未更新 MES。`
}
onMounted(load)
watch(dev, enabled => { if (enabled) { errorText.value = ''; message.value = initialMessage; load() } else { state.value = { lots: [], holds: [] }; rows.value = []; selectedKey.value = ''; lookupField.value = null; searched.value = false; for (const key of Object.keys(filters) as TextFilterKey[]) { filters[key] = ''; checked[key] = false }; holdTimeEnabled.value = false; holdTime.value = null; errorText.value = ''; message.value = unavailableText } })
</script>

<template>
  <section class="wp506-page">
    <div v-if="dev" class="dev-banner">DEV 假資料；若已在 WP504 設定，會讀取同一份本機資料。Release 依原 Service 的 Lot/OP/HoldTime 範圍刪除。</div>
    <h1>預先異常解除 <small>FUTURE HOLD RELEASE · WP506</small></h1>
    <p v-if="errorText" class="notice error" role="alert">{{ errorText }}</p><p v-else class="notice" role="status">{{ message }}</p>
    <fieldset class="query-frame">
      <legend>Query</legend>
      <div class="query-fields">
        <label class="lot-field"><input v-model="checked.lotNo" type="checkbox" :disabled="!dev" @change="setFilterEnabled('lotNo')"> Lot No <input v-model="filters.lotNo" class="mes-input yellow" :disabled="!dev" @input="dev && (checked.lotNo = Boolean(filters.lotNo))" @keydown.enter.prevent="query"></label>
        <label class="op-field"><input v-model="checked.opNo" type="checkbox" :disabled="!dev" @change="setFilterEnabled('opNo')"> OP No <input v-model="filters.opNo" class="mes-input yellow" :disabled="!dev" @input="dev && (checked.opNo = Boolean(filters.opNo))" @keydown.enter.prevent="query"><button type="button" class="lookup-button" :disabled="!dev" @click="dev && (lookupField = lookupField === 'opNo' ? null : 'opNo')">...</button></label>
        <div class="time-field"><label><input v-model="holdTimeEnabled" type="checkbox" :disabled="!dev" @change="setHoldTimeEnabled"> Hold Time</label><span><label><input v-model="holdTime" type="radio" :value="0" :disabled="!dev" @change="dev && (holdTimeEnabled = true)"> Start</label><label><input v-model="holdTime" type="radio" :value="1" :disabled="!dev" @change="dev && (holdTimeEnabled = true)"> End</label></span></div>
        <label class="class-field"><input v-model="checked.mClassNo" type="checkbox" :disabled="!dev" @change="setFilterEnabled('mClassNo')"> MClass No <input v-model="filters.mClassNo" class="mes-input yellow" :disabled="!dev" @input="dev && (checked.mClassNo = Boolean(filters.mClassNo))" @keydown.enter.prevent="query"><button type="button" class="lookup-button" :disabled="!dev" @click="dev && (lookupField = lookupField === 'mClassNo' ? null : 'mClassNo')">...</button></label>
        <button type="button" class="mes-button query-button" :disabled="!dev" @click="query">Query</button>
      </div>
      <div v-if="dev && lookupField" class="lookup-options"><button v-for="item in lookupOptions" :key="item" type="button" @click="chooseLookup(item)">{{ item }}</button></div>
    </fieldset>
    <fieldset class="data-frame">
      <legend>Future Hold Data</legend>
      <div class="data-main">
        <div class="grid-wrap"><table><thead><tr><th>Lot No.</th><th>MONo</th><th>ProductNo</th><th>OP No.</th><th>Hold Time</th><th>MClassNo</th><th>Hold Description</th><th>Creator</th><th>Create Date</th></tr></thead><tbody><tr v-for="item in rows" :key="rowKey(item)" :class="{active: selectedKey === rowKey(item)}" tabindex="0" @click="dev && (selectedKey = rowKey(item))" @keydown.enter="dev && (selectedKey = rowKey(item))"><td>{{ item.lotNo }}</td><td>{{ state.lots.find(lot => lot.lotNo === item.lotNo)?.moNo || '—' }}</td><td>{{ state.lots.find(lot => lot.lotNo === item.lotNo)?.productNo || '—' }}</td><td>{{ item.opNo }}</td><td>{{ item.holdTime === 0 ? 'Start' : 'End' }}</td><td>{{ item.mClassNo }}</td><td>{{ item.holdDescription }}</td><td>{{ item.creator }}</td><td>{{ item.createdAt.slice(0,16).replace('T',' ') }}</td></tr><tr v-if="!rows.length"><td colspan="9" class="empty">{{ !dev ? unavailableText : searched ? '沒有符合條件的預先異常。' : '請按 Query。' }}</td></tr></tbody></table></div>
        <div class="actions"><button type="button" class="mes-button primary" :disabled="!dev || !selected" @click="release">Release</button><button type="button" class="mes-button" @click="emit('close')">Close (X)</button></div>
      </div>
    </fieldset>
  </section>
</template>

<style scoped>
.wp506-page{height:100%;min-height:0;box-sizing:border-box;display:flex;flex-direction:column;gap:7px;padding:9px;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif}
.dev-banner{padding:6px 9px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}
h1{margin:0;font-size:18px}h1 small{font-size:12px;font-weight:400;color:#777}.notice{margin:0;min-height:18px}.notice.error{padding:5px 8px;border:1px solid #d6aaa6;background:#fff0ef;color:#99251e}
fieldset{position:relative;min-width:0;margin:0;padding:7px 8px;border:1px solid #bbb;background:#fff}legend{padding:0 5px}
.query-fields{display:grid;grid-template-columns:300px 310px minmax(10px,1fr) 86px;grid-template-areas:"lot op . ." "time class . query";align-items:center;gap:7px 12px}
.query-fields>label{display:flex;align-items:center;gap:4px;white-space:nowrap}.lot-field{grid-area:lot}.op-field{grid-area:op}.class-field{grid-area:class}.time-field{grid-area:time;display:flex;align-items:center;gap:5px;white-space:nowrap}.time-field>span{display:flex;gap:9px;padding:4px 8px;border:1px solid #bbb}.query-button{grid-area:query}.query-fields .mes-input{width:125px;margin-left:auto}
.mes-input{height:26px;min-width:0;padding:3px 5px;box-sizing:border-box;border:1px solid #888;font:inherit}.yellow{background:#fff0b3!important}
.mes-button,.lookup-button{min-height:29px;padding:3px 10px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#f2f2f2);color:#333;font:inherit;cursor:pointer}.lookup-button{min-width:27px;padding:1px}.mes-button:disabled,.lookup-button:disabled{color:#999;cursor:default}.mes-button.primary{background:#f3a62a;border-color:#e99512;color:#fff}
.lookup-options{position:absolute;z-index:5;top:100%;left:315px;display:flex;gap:5px;padding:7px;border:1px solid #aaa;background:#fffbe9;box-shadow:0 3px 9px #0002}.lookup-options button{padding:3px 7px;border:1px solid #bbb;background:#fff;cursor:pointer}
.data-frame{flex:1;min-height:200px;display:flex;flex-direction:column}.data-main{display:flex;flex:1;min-height:0;gap:10px}.grid-wrap{flex:1;min-width:0;min-height:100px;overflow:auto;border:1px solid #aaa}.grid-wrap table{width:100%;min-width:920px;border-collapse:collapse;white-space:nowrap}.grid-wrap th{position:sticky;top:0;padding:5px 7px;border:1px solid #d4d4d4;background:#ead591;text-align:left;font-weight:500}.grid-wrap td{padding:4px 6px;border:1px solid #d4d4d4;background:#ffe89c}.grid-wrap tr.active td{background:#f4ca68}.grid-wrap tr{cursor:pointer}.empty{text-align:center!important;background:#fff!important;color:#777;height:33px}.actions{display:flex;flex-direction:column;justify-content:space-between;gap:8px}.actions .mes-button{width:86px}
@media(max-width:810px){.query-fields{grid-template-columns:minmax(0,1fr) minmax(0,1fr);grid-template-areas:"lot op" "time class" "query query"}.query-button{justify-self:end;width:86px}}
</style>
