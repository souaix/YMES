<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

type ConsumeLog = {
  equipmentNo: string; consumeNo: string; consumeSeq: string; creator: string; createDate: string
  accQty: number; accPeriod: number
}
type Equipment = { equipmentNo: string; equipmentType: string; description: string; issueState: number }
type ConsumeBasis = { equipmentType: string; consumeNo: string }
type SharedSnapshot = { version: number; statuses?: unknown[]; logs?: ConsumeLog[] }
type PickerMode = 'equipment' | 'consume' | null

const props = defineProps<{ currentUserNo?: string; developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const sharedStorageKey = 'ymes.dev.wp303-consume-status.v1'
const productionUnavailable = '設備耗品歷程服務尚未提供，暫無法查詢或操作。'

const equipment: Equipment[] = [
  { equipmentNo: 'DEV-EQP-ETCH-01', equipmentType: 'DEV-ETCH', description: 'DEV 蝕刻設備 01', issueState: 2 },
  { equipmentNo: 'DEV-EQP-ETCH-02', equipmentType: 'DEV-ETCH', description: 'DEV 蝕刻設備 02', issueState: 2 },
  { equipmentNo: 'DEV-EQP-CMP-01', equipmentType: 'DEV-CMP', description: 'DEV CMP 設備 01', issueState: 2 },
]
const consumeBasis: ConsumeBasis[] = [
  { equipmentType: 'DEV-ETCH', consumeNo: 'DEV-ETCH-KIT' },
  { equipmentType: 'DEV-ETCH', consumeNo: 'DEV-ETCH-FILTER' },
  { equipmentType: 'DEV-CMP', consumeNo: 'DEV-CMP-PAD' },
]
const fixtureLogs: ConsumeLog[] = [
  { equipmentNo: 'DEV-EQP-ETCH-01', consumeNo: 'DEV-ETCH-KIT', consumeSeq: 'DEV-KIT-2026-000', creator: 'DEVUSER', createDate: '2026/11/01 08:15:00', accQty: 486, accPeriod: 702 },
  { equipmentNo: 'DEV-EQP-CMP-01', consumeNo: 'DEV-CMP-PAD', consumeSeq: 'DEV-PAD-2026-001', creator: 'DEVUSER', createDate: '2026/10/29 17:00:00', accQty: 975, accPeriod: 342.5 },
]

function readLogs(): ConsumeLog[] {
  if (!dev.value) return []
  try {
    const raw = window.localStorage.getItem(sharedStorageKey)
    if (raw) {
      const parsed = JSON.parse(raw) as SharedSnapshot
      if (parsed.version === 1 && Array.isArray(parsed.logs)) return parsed.logs.map(item => ({ ...item }))
    }
  } catch { /* fall back to fixed DEV history */ }
  return fixtureLogs.map(item => ({ ...item }))
}

function localDateTimeValue(date: Date): string {
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
}

const allLogs = ref<ConsumeLog[]>(readLogs())
const results = ref<ConsumeLog[]>([])
const hasQueried = ref(false)
const queryError = ref('')
const pickerMode = ref<PickerMode>(null)
const pickerText = ref('')
const query = reactive({
  equipmentChecked: false,
  equipmentNo: '',
  consumeChecked: false,
  consumeNo: '',
  fromChecked: false,
  from: localDateTimeValue(new Date()),
  toChecked: false,
  to: localDateTimeValue(new Date()),
})

const pickerEquipment = computed(() => (dev.value ? equipment : [])
  .filter(item => item.issueState === 2)
  .filter(item => !pickerText.value || (item.equipmentNo + ' ' + item.equipmentType + ' ' + item.description).toLowerCase().includes(pickerText.value.toLowerCase()))
  .sort((a, b) => a.equipmentType.localeCompare(b.equipmentType) || a.equipmentNo.localeCompare(b.equipmentNo)))
const pickerConsume = computed(() => (dev.value ? consumeBasis : [])
  .filter(item => !pickerText.value || (item.consumeNo + ' ' + item.equipmentType).toLowerCase().includes(pickerText.value.toLowerCase()))
  .sort((a, b) => a.consumeNo.localeCompare(b.consumeNo) || a.equipmentType.localeCompare(b.equipmentType)))
const pickerTitle = computed(() => pickerMode.value === 'equipment' ? '選擇設備 (IssueState = 2)' : '選擇耗品 (ConsumeNo)')

function openPicker(mode: Exclude<PickerMode, null>): void {
  if (!dev.value) return
  pickerMode.value = mode
  pickerText.value = ''
}
function chooseEquipment(item: Equipment): void {
  if (!dev.value) return
  query.equipmentNo = item.equipmentNo
  query.equipmentChecked = true
  pickerMode.value = null
}
function chooseConsume(item: ConsumeBasis): void {
  if (!dev.value) return
  query.consumeNo = item.consumeNo
  query.consumeChecked = true
  pickerMode.value = null
}
function toggleEquipment(): void {
  if (!dev.value) return
  if (!query.equipmentChecked) query.equipmentNo = ''
}
function toggleConsume(): void {
  if (!dev.value) return
  if (!query.consumeChecked) query.consumeNo = ''
}
function normalizeEquipment(): void { if (!dev.value) return; query.equipmentNo = query.equipmentNo.trim().toUpperCase() }
function runQuery(): void {
  if (!dev.value) { results.value = []; hasQueried.value = false; return }
  const equipmentNo = query.equipmentChecked ? query.equipmentNo.trim().toUpperCase() : ''
  const consumeNo = query.consumeChecked ? query.consumeNo.trim() : ''
  const from = query.fromChecked && query.from ? new Date(query.from).getTime() : null
  const to = query.toChecked && query.to ? new Date(query.to).getTime() : null
  queryError.value = ''
  if (!equipmentNo && !consumeNo && (from === null || to === null)) {
    results.value = []
    hasQueried.value = false
    queryError.value = '請指定 Equipment No、Consume No，或同時指定 From Date 與 End Date。'
    return
  }
  if (from !== null && to !== null && from > to) {
    results.value = []
    hasQueried.value = false
    queryError.value = 'From Date 不可晚於 End Date。'
    return
  }
  results.value = allLogs.value.filter(item => {
    if (equipmentNo && item.equipmentNo.toUpperCase() !== equipmentNo) return false
    if (consumeNo && item.consumeNo !== consumeNo) return false
    const time = new Date(item.createDate).getTime()
    if (from !== null && Number.isFinite(from) && time < from) return false
    if (to !== null && Number.isFinite(to) && time > to) return false
    return true
  })
  hasQueried.value = true
}
</script>

<template>
  <section class="consume-log-page" aria-label="設備耗品歷程">
    <p v-if="!dev" role="status">{{ productionUnavailable }}</p>
    <fieldset class="query-box" :disabled="!dev">
      <legend>Query</legend>
      <div class="filter-row">
        <label class="check-filter"><input v-model="query.equipmentChecked" type="checkbox" @change="toggleEquipment">Equipment No</label>
        <input v-model="query.equipmentNo" class="query-input equipment-input" aria-label="Equipment No" @blur="normalizeEquipment" @keydown.enter.prevent="runQuery">
        <button class="lookup-button" type="button" aria-label="選擇設備" :disabled="!dev" @click="openPicker('equipment')">...</button>
        <label class="check-filter consume-check"><input v-model="query.consumeChecked" type="checkbox" @change="toggleConsume">Consume No</label>
        <input v-model="query.consumeNo" class="query-input consume-input" aria-label="Consume No" @keydown.enter.prevent="runQuery">
        <button class="lookup-button" type="button" aria-label="選擇耗品" :disabled="!dev" @click="openPicker('consume')">...</button>
      </div>
      <div class="filter-row date-row">
        <label class="check-filter date-filter"><input v-model="query.fromChecked" type="checkbox">From Date</label>
        <input v-model="query.from" type="datetime-local" step="1" class="query-input date-input" aria-label="From Date">
        <label class="check-filter end-filter"><input v-model="query.toChecked" type="checkbox">End Date</label>
        <input v-model="query.to" type="datetime-local" step="1" class="query-input date-input" aria-label="End Date">
        <button class="query-button primary-button" type="button" :disabled="!dev" @click="runQuery">Query</button>
      </div>
    </fieldset>
    <p v-if="queryError" class="query-error" role="alert">{{ queryError }}</p>

    <section class="grid-panel" aria-label="耗品更換紀錄清單">
      <div class="table-wrap"><table class="log-grid">
        <thead><tr><th>EquipmentNo</th><th>ConsumeNo</th><th>ConsumeSeq</th><th class="numeric">AccQty</th><th class="numeric">AccPeriod</th><th>Creator</th><th>CreateDate</th></tr></thead>
        <tbody>
          <tr v-for="(row, index) in results" :key="row.equipmentNo + '\u0000' + row.consumeNo + '\u0000' + row.consumeSeq + '\u0000' + index">
            <td>{{ row.equipmentNo }}</td><td>{{ row.consumeNo }}</td><td>{{ row.consumeSeq }}</td><td class="numeric">{{ row.accQty }}</td><td class="numeric">{{ Number(row.accPeriod).toFixed(2) }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td>
          </tr>
          <tr v-if="hasQueried && results.length === 0"><td colspan="7" class="empty-state">查無符合條件的耗品歷程</td></tr>
          <tr v-if="!hasQueried"><td colspan="7" class="empty-state">{{ dev ? '設定查詢條件後按 Query' : productionUnavailable }}</td></tr>
        </tbody>
      </table></div>
      <div class="grid-footer"><span>{{ hasQueried ? '資料總筆數 ' + results.length : '尚未查詢' }}</span><span class="hint">{{ dev ? 'DEV 假資料；與 WP303 重置歷程共用' : '服務尚未提供' }}</span></div>
    </section>

    <div class="footer-actions"><button type="button" @click="emit('close')">Close (&amp;X)</button></div>

    <div v-if="dev && pickerMode" class="modal-backdrop" role="presentation" @click.self="pickerMode = null">
      <section class="picker-dialog" role="dialog" aria-modal="true" aria-labelledby="wp304-picker-title">
        <h2 id="wp304-picker-title">{{ pickerTitle }}</h2>
        <label class="picker-search">搜尋<input v-model="pickerText" class="query-input" autocomplete="off"></label>
        <div class="picker-list">
          <template v-if="pickerMode === 'equipment'">
            <button v-for="item in pickerEquipment" :key="item.equipmentNo" type="button" @click="chooseEquipment(item)"><strong>{{ item.equipmentNo }}</strong><span>{{ item.equipmentType }}　{{ item.description }}</span></button>
            <p v-if="pickerEquipment.length === 0" class="empty-state">沒有符合條件的啟用設備</p>
          </template>
          <template v-else>
            <button v-for="item in pickerConsume" :key="item.equipmentType + '\u0000' + item.consumeNo" type="button" @click="chooseConsume(item)"><strong>{{ item.consumeNo }}</strong><span>{{ item.equipmentType }}</span></button>
            <p v-if="pickerConsume.length === 0" class="empty-state">沒有符合條件的耗品</p>
          </template>
        </div>
        <div class="dialog-actions"><button type="button" @click="pickerMode = null">Close</button></div>
      </section>
    </div>
  </section>
</template>

<style scoped>
.consume-log-page { height: 100%; min-height: 0; display: flex; flex-direction: column; gap: 6px; padding: 12px; color: #292929; background: #fff; overflow: auto; }
.query-box { flex: 0 0 auto; min-height: 81px; margin: 0; border: 1px solid #aaa; padding: 5px 10px 7px; }
.query-error { flex: 0 0 auto; margin: 0; color: #a33; font-size: 13px; }
.query-box legend { padding: 0 5px; color: #444; font-weight: 600; }
.filter-row { display: flex; align-items: center; gap: 6px; min-height: 30px; }
.check-filter { min-width: 116px; display: flex; align-items: center; gap: 5px; white-space: nowrap; }
.check-filter input { accent-color: #72590d; }
.consume-check { min-width: 100px; margin-left: 12px; }
.query-input { height: 27px; min-width: 0; border: 1px solid #929292; border-radius: 0; padding: 3px 6px; color: #292929; background: #fff; font: inherit; }
.query-input:focus { outline: 2px solid #c99a30; }
.equipment-input, .consume-input { width: 120px; }
.lookup-button { width: 25px; min-height: 26px; padding: 1px 3px; }
button { min-height: 30px; border: 1px solid #aaa; border-radius: 3px; padding: 4px 9px; color: #333; background: #f8f8f8; font: inherit; white-space: nowrap; cursor: pointer; }
.primary-button { border-color: #d58b16; color: #fff; background: #f59a18; }
.date-row { margin-top: 3px; }
.date-filter { min-width: 116px; padding-left: 17px; }
.end-filter { min-width: 100px; margin-left: 12px; }
.date-input { width: 176px; }
.query-button { min-width: 80px; margin-left: auto; }
.grid-panel { flex: 1 1 auto; min-height: 0; display: flex; flex-direction: column; border: 1px solid #aaa; }
.table-wrap { min-height: 0; flex: 1 1 auto; overflow: auto; }
.log-grid { width: 100%; border-collapse: collapse; font-size: 12px; }
.log-grid th { position: sticky; top: 0; z-index: 1; padding: 7px 6px; border: 1px solid #c5c5c5; color: #303030; background: #e8e8e8; text-align: left; white-space: nowrap; }
.log-grid td { padding: 6px; border: 1px solid #d1d1d1; background: #fff0ad; white-space: nowrap; }
.numeric { text-align: right; }
.empty-state { padding: 14px !important; color: #777; text-align: center; background: #fafafa !important; }
.grid-footer { display: flex; justify-content: space-between; gap: 8px; padding: 4px 7px; border-top: 1px solid #bbb; color: #666; font-size: 11px; }
.hint { margin-left: auto; }
.footer-actions { display: flex; justify-content: flex-end; }
.footer-actions button { min-width: 80px; }
.modal-backdrop { position: fixed; inset: 0; z-index: 1000; display: grid; place-items: center; padding: 16px; background: rgb(0 0 0 / 34%); }
.picker-dialog { width: min(560px, 100%); max-height: min(86vh, 680px); overflow: auto; border: 1px solid #aaa; border-radius: 4px; padding: 14px; background: #fff; box-shadow: 0 8px 30px rgb(0 0 0 / 20%); }
.picker-dialog h2 { margin: 0 0 12px; font-size: 17px; }
.picker-search { display: grid; grid-template-columns: 70px minmax(0, 1fr); align-items: center; gap: 8px; margin-bottom: 9px; }
.picker-search .query-input { width: 100%; }
.picker-list { max-height: 55vh; overflow: auto; display: flex; flex-direction: column; gap: 5px; }
.picker-list button { display: flex; justify-content: space-between; gap: 10px; text-align: left; }
.picker-list span { color: #666; }
.dialog-actions { display: flex; justify-content: flex-end; margin-top: 11px; }
@media (max-width: 860px) {
  .consume-log-page { height: auto; min-height: 100%; }
  .query-box { padding-bottom: 10px; }
  .filter-row { flex-wrap: wrap; margin: 4px 0; }
  .consume-check, .end-filter { margin-left: 0; }
  .date-row { gap: 7px; }
  .date-filter { padding-left: 0; }
  .date-input { width: 190px; }
  .query-button { margin-left: 0; }
}
</style>
