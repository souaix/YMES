<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'

type TextFilterKey = 'equipmentType' | 'equipmentNo' | 'userNo' | 'accessoryType' | 'accessoryNo'
type DateFilterKey = 'startFrom' | 'startEnd' | 'endFrom' | 'endEnd'
type AccessoryLog = {
  accSerialNo: string
  equipmentType: string
  equipmentNo: string
  accessoryType: string
  accessoryNo: string
  accessoryVersion: string
  startTime: string
  endTime: string
  userNo: string
}

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()

const filters = reactive({
  equipmentType: { enabled: false, value: '' },
  equipmentNo: { enabled: false, value: '' },
  userNo: { enabled: false, value: '' },
  accessoryType: { enabled: false, value: '' },
  accessoryNo: { enabled: false, value: '' },
  startFrom: { enabled: false, value: '' },
  startEnd: { enabled: false, value: '' },
  endFrom: { enabled: false, value: '' },
  endEnd: { enabled: false, value: '' },
})

const fixtureRows: AccessoryLog[] = [
  { accSerialNo: 'DEV-ACC-00051', equipmentType: 'FT TESTER', equipmentNo: 'TST-01', accessoryType: 'PROBE', accessoryNo: 'PRB-FT-008', accessoryVersion: '01', startTime: '2026-11-18T08:10', endTime: '2026-11-19T14:35', userNo: 'U11019' },
  { accSerialNo: 'DEV-ACC-00052', equipmentType: 'FT TESTER', equipmentNo: 'TST-01', accessoryType: 'SOCKET', accessoryNo: 'SCK-FT-121', accessoryVersion: '02', startTime: '2026-11-19T15:05', endTime: '2026-11-20T09:20', userNo: 'U20418' },
  { accSerialNo: 'DEV-ACC-00053', equipmentType: 'FT TESTER', equipmentNo: 'TST-02', accessoryType: 'PROBE', accessoryNo: 'PRB-FT-011', accessoryVersion: '01', startTime: '2026-11-20T09:40', endTime: '2026-11-21T10:15', userNo: 'U11019' },
  { accSerialNo: 'DEV-ACC-00054', equipmentType: 'FT MOUNTER', equipmentNo: 'MNT-01', accessoryType: 'PICKUP', accessoryNo: 'PKU-MT-022', accessoryVersion: '03', startTime: '2026-11-20T11:25', endTime: '2026-11-21T07:55', userNo: 'U30682' },
  { accSerialNo: 'DEV-ACC-00055', equipmentType: 'FT MOUNTER', equipmentNo: 'MNT-02', accessoryType: 'NOZZLE', accessoryNo: 'NZL-MT-034', accessoryVersion: '01', startTime: '2026-11-21T08:25', endTime: '2026-11-22T12:10', userNo: 'U20418' },
  { accSerialNo: 'DEV-ACC-00056', equipmentType: 'FT TESTER', equipmentNo: 'TST-02', accessoryType: 'SOCKET', accessoryNo: 'SCK-FT-128', accessoryVersion: '01', startTime: '2026-11-22T13:00', endTime: '2026-11-23T08:45', userNo: 'U11019' },
  { accSerialNo: 'DEV-ACC-00057', equipmentType: 'OVEN', equipmentNo: 'OVN-01', accessoryType: 'THERMOCOUPLE', accessoryNo: 'THM-OV-016', accessoryVersion: '02', startTime: '2026-11-22T14:30', endTime: '2026-11-23T09:15', userNo: 'U30682' },
  { accSerialNo: 'DEV-ACC-00058', equipmentType: 'FT MOUNTER', equipmentNo: 'MNT-01', accessoryType: 'PICKUP', accessoryNo: 'PKU-MT-019', accessoryVersion: '02', startTime: '2026-11-23T09:30', endTime: '2026-11-23T17:05', userNo: 'U11019' },
]

const lookupData = {
  equipmentType: ['FT TESTER', 'FT MOUNTER', 'OVEN'],
  equipmentNo: ['MNT-01', 'MNT-02', 'OVN-01', 'TST-01', 'TST-02'],
  userNo: ['U11019', 'U20418', 'U30682'],
  accessoryType: ['NOZZLE', 'PICKUP', 'PROBE', 'SOCKET', 'THERMOCOUPLE'],
}
const resultRows = ref<AccessoryLog[]>([])
const hasQueried = ref(false)
const pickerField = ref<TextFilterKey | null>(null)
const selectedOption = ref('')
const statusMessage = ref('請輸入查詢條件後按 Query；未指定條件時會查詢所有 DEV 歷程。')

const pickerOptions = computed(() => {
  const field = pickerField.value
  if (!field) return []
  if (field === 'accessoryNo') {
    const type = filters.accessoryType.value.trim().toUpperCase()
    const options = fixtureRows
      .filter((row) => !type || row.accessoryType === type)
      .map((row) => row.accessoryNo)
    return [...new Set(options)].sort()
  }
  return [...lookupData[field]]
})

const enabledFilterCount = computed(() =>
  ([
    filters.equipmentType,
    filters.equipmentNo,
    filters.userNo,
    filters.accessoryType,
    filters.accessoryNo,
    filters.startFrom,
    filters.startEnd,
    filters.endFrom,
    filters.endEnd,
  ] as const).filter((filter) => filter.enabled && filter.value).length,
)

function setTextFilterFromInput(key: TextFilterKey) {
  filters[key].enabled = filters[key].value.trim().length > 0
}

function normalizeTextFilter(key: TextFilterKey) {
  filters[key].value = filters[key].value.trim().toUpperCase()
  filters[key].enabled = filters[key].value.length > 0
}

function clearTextFilterIfDisabled(key: TextFilterKey) {
  if (!filters[key].enabled) filters[key].value = ''
}

function clearDateFilterIfDisabled(key: DateFilterKey) {
  if (!filters[key].enabled) filters[key].value = ''
}

function openPicker(key: TextFilterKey) {
  pickerField.value = key
  selectedOption.value = ''
}

function closePicker() {
  pickerField.value = null
  selectedOption.value = ''
}

function applyPickerValue() {
  const field = pickerField.value
  const option = selectedOption.value
  if (!field || !option) return
  filters[field].value = option
  filters[field].enabled = true
  closePicker()
}

function valueIfEnabled(key: TextFilterKey) {
  const filter = filters[key]
  return filter.enabled ? filter.value.trim().toUpperCase() : ''
}

function queryHistory() {
  const equipmentType = valueIfEnabled('equipmentType')
  const equipmentNo = valueIfEnabled('equipmentNo')
  const userNo = valueIfEnabled('userNo')
  const accessoryType = valueIfEnabled('accessoryType')
  const accessoryNo = valueIfEnabled('accessoryNo')
  const startFrom = filters.startFrom.enabled ? filters.startFrom.value : ''
  const startEnd = filters.startEnd.enabled ? filters.startEnd.value : ''
  const endFrom = filters.endFrom.enabled ? filters.endFrom.value : ''
  const endEnd = filters.endEnd.enabled ? filters.endEnd.value : ''

  resultRows.value = fixtureRows
    .filter((row) => (!equipmentType || row.equipmentType === equipmentType)
      && (!equipmentNo || row.equipmentNo === equipmentNo)
      && (!userNo || row.userNo === userNo)
      && (!accessoryType || row.accessoryType === accessoryType)
      && (!accessoryNo || row.accessoryNo === accessoryNo)
      && (!startFrom || row.startTime >= startFrom)
      && (!startEnd || row.startTime <= startEnd)
      && (!endFrom || row.endTime >= endFrom)
      && (!endEnd || row.endTime <= endEnd))
    .sort((a, b) => a.startTime.localeCompare(b.startTime)
      || a.equipmentType.localeCompare(b.equipmentType)
      || a.equipmentNo.localeCompare(b.equipmentNo)
      || a.accessoryType.localeCompare(b.accessoryType)
      || a.accessoryNo.localeCompare(b.accessoryNo))

  hasQueried.value = true
  statusMessage.value = resultRows.value.length
    ? `查詢完成，找到 ${resultRows.value.length} 筆 DEV 歷程。`
    : '查詢完成，沒有符合條件的 DEV 歷程。'
}

function formatDateTime(value: string) {
  return value ? `${value.replace('T', ' ')}:00` : '—'
}

onMounted(() => {
  const initialAccessoryNo = new URLSearchParams(window.location.search).get('AccessoryNo')
  if (!initialAccessoryNo) return
  filters.accessoryNo.value = initialAccessoryNo.trim().toUpperCase()
  filters.accessoryNo.enabled = true
  queryHistory()
})
</script>

<template>
  <section class="equipment-acc-log" aria-label="設備配件歷史資料查詢 DEV 預覽" :data-development-preview="props.developmentPreview ? 'true' : 'false'">
    <header class="page-heading">
      <div>
        <p class="eyebrow">EMS EXECUTION · ES408</p>
        <h1>設備配件歷史資料查詢</h1>
        <p class="subheading">依設備、配件、人員與起訖時間查找掛載歷程。</p>
      </div>
      <span class="dev-badge">DEV 假資料</span>
    </header>

    <section class="filter-panel" aria-label="查詢條件">
      <div class="text-filter-grid">
        <div class="filter-field">
          <label class="filter-toggle"><input v-model="filters.equipmentType.enabled" type="checkbox" @change="clearTextFilterIfDisabled('equipmentType')"><span>Equipment Type</span></label>
          <div class="input-with-picker"><input v-model="filters.equipmentType.value" type="text" autocomplete="off" @input="setTextFilterFromInput('equipmentType')" @blur="normalizeTextFilter('equipmentType')"><button type="button" aria-label="選擇設備類別" @click="openPicker('equipmentType')">⌕</button></div>
        </div>
        <div class="filter-field">
          <label class="filter-toggle"><input v-model="filters.equipmentNo.enabled" type="checkbox" @change="clearTextFilterIfDisabled('equipmentNo')"><span>Equipment No</span></label>
          <div class="input-with-picker"><input v-model="filters.equipmentNo.value" type="text" autocomplete="off" @input="setTextFilterFromInput('equipmentNo')" @blur="normalizeTextFilter('equipmentNo')"><button type="button" aria-label="選擇設備編號" @click="openPicker('equipmentNo')">⌕</button></div>
        </div>
        <div class="filter-field">
          <label class="filter-toggle"><input v-model="filters.userNo.enabled" type="checkbox" @change="clearTextFilterIfDisabled('userNo')"><span>User No</span></label>
          <div class="input-with-picker"><input v-model="filters.userNo.value" type="text" autocomplete="off" @input="setTextFilterFromInput('userNo')" @blur="normalizeTextFilter('userNo')"><button type="button" aria-label="選擇使用者編號" @click="openPicker('userNo')">⌕</button></div>
        </div>
        <div class="filter-field">
          <label class="filter-toggle"><input v-model="filters.accessoryType.enabled" type="checkbox" @change="clearTextFilterIfDisabled('accessoryType')"><span>Accessory Type</span></label>
          <div class="input-with-picker"><input v-model="filters.accessoryType.value" type="text" autocomplete="off" @input="setTextFilterFromInput('accessoryType')" @blur="normalizeTextFilter('accessoryType')"><button type="button" aria-label="選擇配件類別" @click="openPicker('accessoryType')">⌕</button></div>
        </div>
        <div class="filter-field">
          <label class="filter-toggle"><input v-model="filters.accessoryNo.enabled" type="checkbox" @change="clearTextFilterIfDisabled('accessoryNo')"><span>Accessory No</span></label>
          <div class="input-with-picker"><input v-model="filters.accessoryNo.value" type="text" autocomplete="off" @input="setTextFilterFromInput('accessoryNo')" @blur="normalizeTextFilter('accessoryNo')"><button type="button" aria-label="選擇配件編號" @click="openPicker('accessoryNo')">⌕</button></div>
        </div>
      </div>

      <div class="date-filter-grid">
        <div class="date-group">
          <h2>Start Time</h2>
          <label class="date-filter"><input v-model="filters.startFrom.enabled" type="checkbox" @change="clearDateFilterIfDisabled('startFrom')"><span>From</span><input v-model="filters.startFrom.value" type="datetime-local" :disabled="!filters.startFrom.enabled"></label>
          <label class="date-filter"><input v-model="filters.startEnd.enabled" type="checkbox" @change="clearDateFilterIfDisabled('startEnd')"><span>To</span><input v-model="filters.startEnd.value" type="datetime-local" :disabled="!filters.startEnd.enabled"></label>
        </div>
        <div class="date-group">
          <h2>End Time</h2>
          <label class="date-filter"><input v-model="filters.endFrom.enabled" type="checkbox" @change="clearDateFilterIfDisabled('endFrom')"><span>From</span><input v-model="filters.endFrom.value" type="datetime-local" :disabled="!filters.endFrom.enabled"></label>
          <label class="date-filter"><input v-model="filters.endEnd.enabled" type="checkbox" @change="clearDateFilterIfDisabled('endEnd')"><span>To</span><input v-model="filters.endEnd.value" type="datetime-local" :disabled="!filters.endEnd.enabled"></label>
        </div>
        <div class="query-action">
          <span>{{ enabledFilterCount }} 個有效條件</span>
          <button type="button" class="query-button" @click="queryHistory">Query</button>
        </div>
      </div>
      <p class="filter-note">文字條件採完整相符；時間起訖採含邊界比較。Lookup 清單均為 DEV 假資料。</p>
    </section>

    <section class="results-panel" aria-label="設備配件歷程結果">
      <div class="results-heading">
        <div><h2>Equipment Accessory State Log</h2><p>{{ hasQueried ? statusMessage : '尚未查詢' }}</p></div>
        <span v-if="hasQueried" class="result-count">{{ resultRows.length }} 筆</span>
      </div>
      <div class="table-wrap">
        <table>
          <thead><tr><th>Equipment Type</th><th>Equipment No</th><th>Accessory Type</th><th>Accessory No</th><th>Start Time</th><th>End Time</th><th>User No</th></tr></thead>
          <tbody>
            <tr v-for="row in resultRows" :key="row.accSerialNo">
              <td>{{ row.equipmentType }}</td><td class="mono">{{ row.equipmentNo }}</td><td>{{ row.accessoryType }}</td><td class="mono">{{ row.accessoryNo }}</td><td>{{ formatDateTime(row.startTime) }}</td><td>{{ formatDateTime(row.endTime) }}</td><td class="mono">{{ row.userNo }}</td>
            </tr>
            <tr v-if="hasQueried && resultRows.length === 0"><td colspan="7" class="empty-state">沒有符合條件的歷程</td></tr>
            <tr v-if="!hasQueried"><td colspan="7" class="empty-state">設定條件後按 Query 載入歷程</td></tr>
          </tbody>
        </table>
      </div>
    </section>

    <footer class="page-footer">
      <p role="status" aria-live="polite">{{ statusMessage }}</p>
      <button type="button" class="close-button" @click="emit('close')">Close (X)</button>
    </footer>

    <div v-if="pickerField" class="modal-backdrop" @click.self="closePicker">
      <section class="picker-dialog" role="dialog" aria-modal="true" :aria-label="`選擇 ${pickerField}`">
        <header><div><p class="eyebrow">DEV LOOKUP</p><h2>選擇 {{ pickerField }}</h2></div><button type="button" aria-label="關閉選擇視窗" @click="closePicker">×</button></header>
        <p v-if="pickerField === 'accessoryNo' && !filters.accessoryType.value" class="picker-note">尚未輸入配件類別，清單列出所有假資料配件編號。</p>
        <div class="picker-options">
          <label v-for="option in pickerOptions" :key="option" class="picker-option"><input v-model="selectedOption" type="radio" name="lookup-option" :value="option"><span>{{ option }}</span></label>
          <p v-if="pickerOptions.length === 0" class="empty-state">沒有可選項目</p>
        </div>
        <footer><button type="button" class="close-button secondary" @click="closePicker">Cancel</button><button type="button" class="query-button" :disabled="!selectedOption" @click="applyPickerValue">Select</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.equipment-acc-log { display: flex; flex-direction: column; gap: 14px; min-height: 0; height: 100%; box-sizing: border-box; padding: 20px; overflow: auto; background: #f5f4ee; color: #2b302e; font: inherit; }
.page-heading { display: flex; align-items: flex-start; justify-content: space-between; gap: 16px; }
.eyebrow { margin: 0 0 5px; color: #6d786f; font-size: 10px; font-weight: 700; letter-spacing: .12em; }
h1 { margin: 0; color: #202722; font-size: 22px; font-weight: 650; }
.subheading { margin: 5px 0 0; color: #788079; font-size: 12px; }
.dev-badge { flex: 0 0 auto; padding: 5px 9px; border: 1px solid #c9dfd0; border-radius: 999px; background: #e8f3ec; color: #367148; font-size: 10px; font-weight: 700; }
.filter-panel, .results-panel { border: 1px solid #dfe3dd; border-radius: 9px; background: #fff; box-shadow: 0 4px 13px #23372b0a; }
.filter-panel { padding: 14px; }
.text-filter-grid { display: grid; grid-template-columns: repeat(5, minmax(120px, 1fr)); gap: 10px; }
.filter-field { min-width: 0; }
.filter-toggle { display: flex; align-items: center; gap: 6px; margin-bottom: 5px; color: #515b54; font-size: 10px; font-weight: 700; }
.filter-toggle input, .date-filter > input[type="checkbox"] { accent-color: #47704f; }
.filter-toggle input { width: 13px; height: 13px; margin: 0; }
.input-with-picker { display: flex; height: 31px; }
.input-with-picker input { width: 100%; min-width: 0; padding: 0 8px; border: 1px solid #d5dbd5; border-right: 0; border-radius: 5px 0 0 5px; color: #303832; font: inherit; font-size: 11px; }
.input-with-picker button { width: 31px; flex: 0 0 31px; border: 1px solid #d5dbd5; border-radius: 0 5px 5px 0; background: #f7f9f6; color: #55745d; font-size: 17px; cursor: pointer; }
.input-with-picker input:focus-visible, .date-filter input:focus-visible, .query-button:focus-visible, .close-button:focus-visible { outline: 2px solid #5e8c6c; outline-offset: 2px; }
.date-filter-grid { display: grid; grid-template-columns: minmax(0, 1fr) minmax(0, 1fr) 96px; gap: 14px; margin-top: 13px; padding-top: 12px; border-top: 1px solid #edf0eb; }
.date-group h2 { margin: 0 0 7px; color: #59635c; font-size: 10px; font-weight: 700; }
.date-filter { display: grid; grid-template-columns: 14px 33px minmax(0, 1fr); align-items: center; gap: 7px; margin: 5px 0; color: #737d75; font-size: 10px; }
.date-filter > input[type="checkbox"] { width: 13px; height: 13px; margin: 0; }
.date-filter > input[type="datetime-local"] { width: 100%; min-width: 0; height: 27px; padding: 0 6px; border: 1px solid #d5dbd5; border-radius: 4px; color: #344039; font: inherit; font-size: 10px; }
.date-filter > input:disabled { background: #f5f6f4; color: #a4aaa4; }
.query-action { display: flex; flex-direction: column; justify-content: end; gap: 7px; color: #909790; font-size: 10px; text-align: right; }
.query-button, .close-button { min-height: 32px; padding: 0 14px; border: 1px solid #3f6749; border-radius: 5px; background: #426e4d; color: #fff; font: inherit; font-size: 11px; font-weight: 700; cursor: pointer; }
.query-button:disabled { opacity: .45; cursor: default; }
.query-button:not(:disabled):hover { background: #365e40; }
.filter-note { margin: 9px 0 0; color: #909790; font-size: 10px; }
.results-panel { display: flex; flex-direction: column; flex: 1 1 200px; min-height: 190px; overflow: hidden; }
.results-heading { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 11px 14px; border-bottom: 1px solid #e9ede8; }
.results-heading h2 { margin: 0; color: #313c34; font-size: 12px; }
.results-heading p { margin: 3px 0 0; color: #889088; font-size: 10px; }
.result-count { padding: 4px 8px; border-radius: 999px; background: #eff3ef; color: #59715f; font-size: 10px; font-weight: 700; }
.table-wrap { flex: 1; overflow: auto; }
table { width: 100%; min-width: 850px; border-collapse: collapse; font-size: 10px; }
th { position: sticky; top: 0; z-index: 1; padding: 9px 10px; background: #f4f6f3; color: #627067; font-size: 9px; font-weight: 700; text-align: left; white-space: nowrap; }
td { padding: 8px 10px; border-top: 1px solid #edf0ed; color: #424b45; white-space: nowrap; }
tbody tr:hover td { background: #f8faf7; }
.mono { color: #344b3a; font-family: ui-monospace, SFMono-Regular, Consolas, monospace; }
.empty-state { padding: 28px 12px; color: #919891; text-align: center; }
.page-footer { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.page-footer p { margin: 0; color: #748077; font-size: 10px; }
.close-button { border-color: #cfd6cf; background: #fff; color: #4a564d; }
.close-button:hover { background: #f4f6f3; }
.close-button.secondary { min-width: 75px; }
.modal-backdrop { position: fixed; inset: 0; z-index: 1000; display: grid; place-items: center; padding: 20px; background: #14231970; }
.picker-dialog { display: flex; flex-direction: column; width: min(420px, 100%); max-height: min(540px, 85vh); overflow: hidden; border: 1px solid #d9e0d9; border-radius: 10px; background: #fff; box-shadow: 0 18px 50px #0d1b1260; }
.picker-dialog > header { display: flex; justify-content: space-between; align-items: center; padding: 15px 17px 11px; border-bottom: 1px solid #edf0eb; }
.picker-dialog h2 { margin: 0; color: #2c3730; font-size: 15px; text-transform: capitalize; }
.picker-dialog > header > button { width: 30px; height: 30px; border: 0; border-radius: 5px; background: transparent; color: #6a746c; font-size: 22px; cursor: pointer; }
.picker-note { margin: 9px 16px 0; color: #8b918b; font-size: 10px; }
.picker-options { min-height: 80px; padding: 8px 14px; overflow: auto; }
.picker-option { display: flex; align-items: center; gap: 9px; min-height: 35px; padding: 0 8px; border-radius: 5px; color: #37433a; font-size: 11px; cursor: pointer; }
.picker-option:hover { background: #f3f6f2; }
.picker-option input { accent-color: #47704f; }
.picker-dialog > footer { display: flex; justify-content: flex-end; gap: 8px; padding: 12px 15px; border-top: 1px solid #edf0eb; }
@media (max-width: 850px) {
  .equipment-acc-log { padding: 14px; }
  .text-filter-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .date-filter-grid { grid-template-columns: 1fr; }
  .query-action { flex-direction: row; align-items: center; justify-content: space-between; text-align: left; }
}
@media (max-width: 520px) { .text-filter-grid { grid-template-columns: 1fr; } }
</style>
