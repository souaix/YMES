<script setup lang="ts">
import { computed, ref } from 'vue'

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

type PickerMode = 'equipment' | 'state'
type StateLogFixture = {
  areaNo: string
  equipmentNo: string
  equipmentType: string
  equipmentState: string
  status: string
  startTime: string
  endTime: string | null
  reason: string
  chamberFlag: boolean
  belongEquipmentNo: string
}
type ReportRow = StateLogFixture & {
  visibleEquipmentNo: string
  chamberNo: string
  totalTimeMinutes: number
}

const localInput = (date: Date) => new Date(date.getTime() - date.getTimezoneOffset() * 60_000).toISOString().slice(0, 16)
const today = new Date()
today.setHours(0, 0, 0, 0)
const defaultStart = new Date(today)
defaultStart.setDate(defaultStart.getDate() - 1)
const dateAtOffset = (minutesBeforeToday: number) => new Date(today.getTime() - minutesBeforeToday * 60_000).toISOString()

const stateOptions = [
  { code: '10', name: '生產中', type: 'Run', utilization: 'Productive' },
  { code: '20', name: '待料', type: 'Idle', utilization: 'Non-productive' },
  { code: '30', name: '故障', type: 'Down', utilization: 'Non-productive' },
  { code: '40', name: '保養', type: 'PM', utilization: 'Non-productive' },
]
const equipmentOptions = [
  { code: 'ETCH-01', name: '蝕刻機一號', type: 'ETCH', area: 'FAB-A' },
  { code: 'ETCH-02', name: '蝕刻機二號', type: 'ETCH', area: 'FAB-A' },
  { code: 'CVD-01', name: '薄膜設備一號', type: 'CVD', area: 'FAB-B' },
  { code: 'CVD-01-CH1', name: '薄膜腔體一', type: 'CHAMBER', area: 'FAB-B' },
  { code: 'CVD-01-CH2', name: '薄膜腔體二', type: 'CHAMBER', area: 'FAB-B' },
]
const fixtures: StateLogFixture[] = [
  { areaNo: 'FAB-A', equipmentNo: 'ETCH-01', equipmentType: 'ETCH', equipmentState: '10', status: '生產中', startTime: dateAtOffset(1320), endTime: dateAtOffset(1110), reason: '批次生產', chamberFlag: false, belongEquipmentNo: '' },
  { areaNo: 'FAB-A', equipmentNo: 'ETCH-01', equipmentType: 'ETCH', equipmentState: '20', status: '待料', startTime: dateAtOffset(1110), endTime: dateAtOffset(1015), reason: '等待物料補充', chamberFlag: false, belongEquipmentNo: '' },
  { areaNo: 'FAB-A', equipmentNo: 'ETCH-01', equipmentType: 'ETCH', equipmentState: '10', status: '生產中', startTime: dateAtOffset(1015), endTime: dateAtOffset(760), reason: '批次生產', chamberFlag: false, belongEquipmentNo: '' },
  { areaNo: 'FAB-A', equipmentNo: 'ETCH-02', equipmentType: 'ETCH', equipmentState: '30', status: '故障', startTime: dateAtOffset(900), endTime: dateAtOffset(850), reason: '真空壓力異常', chamberFlag: false, belongEquipmentNo: '' },
  { areaNo: 'FAB-B', equipmentNo: 'CVD-01', equipmentType: 'CVD', equipmentState: '10', status: '生產中', startTime: dateAtOffset(1180), endTime: dateAtOffset(940), reason: '批次生產', chamberFlag: false, belongEquipmentNo: '' },
  { areaNo: 'FAB-B', equipmentNo: 'CVD-01-CH1', equipmentType: 'CVD', equipmentState: '10', status: '生產中', startTime: dateAtOffset(1160), endTime: dateAtOffset(960), reason: '腔體運轉', chamberFlag: true, belongEquipmentNo: 'CVD-01' },
  { areaNo: 'FAB-B', equipmentNo: 'CVD-01-CH2', equipmentType: 'CVD', equipmentState: '40', status: '保養', startTime: dateAtOffset(1000), endTime: dateAtOffset(890), reason: '定期清潔', chamberFlag: true, belongEquipmentNo: 'CVD-01' },
  { areaNo: 'FAB-B', equipmentNo: 'CVD-01-CH2', equipmentType: 'CVD', equipmentState: '10', status: '生產中', startTime: dateAtOffset(890), endTime: null, reason: '腔體運轉', chamberFlag: true, belongEquipmentNo: 'CVD-01' },
]

const startTime = ref(localInput(defaultStart))
const endTime = ref(localInput(today))
const equipmentFilterEnabled = ref(false)
const stateFilterEnabled = ref(false)
const selectedEquipment = ref<string[]>([])
const selectedStates = ref<string[]>([])
const chamberAvailable = true // DEV fixture: SYSParameter.ChamberManage = 1.
const showChambers = ref(false)
const activePicker = ref<PickerMode | null>(null)
const draftSelection = ref<string[]>([])
const errorMessage = ref('')
const reportRows = ref<ReportRow[]>([])
const hasQueried = ref(false)
const reportIsChamberMode = ref(false)

const pickerTitle = computed(() => activePicker.value === 'state' ? '選擇設備狀態' : '選擇設備')
const pickerOptions = computed(() => activePicker.value === 'state'
  ? stateOptions
  : equipmentOptions.filter((option) => showChambers.value || option.type !== 'CHAMBER'))
const selectedStateSummary = computed(() => stateOptions.filter((option) => selectedStates.value.includes(option.code)).map((option) => option.name).join('、'))
const selectedEquipmentSummary = computed(() => equipmentOptions.filter((option) => selectedEquipment.value.includes(option.code)).map((option) => option.name).join('、'))

function openPicker(mode: PickerMode) {
  activePicker.value = mode
  draftSelection.value = [...(mode === 'state' ? selectedStates.value : selectedEquipment.value)]
}

function toggleDraft(code: string) {
  draftSelection.value = draftSelection.value.includes(code)
    ? draftSelection.value.filter((item) => item !== code)
    : [...draftSelection.value, code]
}

function applyPicker() {
  if (activePicker.value === 'state') {
    selectedStates.value = [...draftSelection.value]
    stateFilterEnabled.value = selectedStates.value.length > 0
  } else if (activePicker.value === 'equipment') {
    selectedEquipment.value = [...draftSelection.value]
    equipmentFilterEnabled.value = selectedEquipment.value.length > 0
  }
  activePicker.value = null
}

function clearFilter(mode: PickerMode) {
  if (mode === 'state') selectedStates.value = []
  else selectedEquipment.value = []
}

function displayTime(value: string | null) {
  if (!value) return '—'
  return new Date(value).toLocaleString('zh-TW', { hour12: false })
}

function query() {
  errorMessage.value = ''
  const from = new Date(startTime.value)
  const to = new Date(endTime.value)
  if (!startTime.value || !endTime.value || Number.isNaN(from.getTime()) || Number.isNaN(to.getTime())) {
    errorMessage.value = '請輸入有效的起訖時間。'
    return
  }
  if (from >= to) {
    errorMessage.value = '結束時間必須晚於開始時間。'
    return
  }

  const allowedEquipment = new Set(equipmentFilterEnabled.value ? selectedEquipment.value : [])
  const allowedStates = new Set(stateFilterEnabled.value ? selectedStates.value : [])
  const chamberMode = showChambers.value && chamberAvailable
  reportRows.value = fixtures.flatMap((row) => {
    if (allowedEquipment.size > 0 && !allowedEquipment.has(row.equipmentNo)) return []
    if (allowedStates.size > 0 && !allowedStates.has(row.equipmentState)) return []
    const actualStart = new Date(row.startTime)
    const actualEnd = row.endTime ? new Date(row.endTime) : new Date()
    if (actualStart > to || actualEnd < from) return []
    const clippedStart = actualStart < from ? from : actualStart
    const clippedEnd = actualEnd > to ? to : actualEnd
    if (clippedEnd <= clippedStart) return []
    return [{
      ...row,
      startTime: clippedStart.toISOString(),
      endTime: clippedEnd.toISOString(),
      visibleEquipmentNo: chamberMode && row.chamberFlag ? row.belongEquipmentNo : row.equipmentNo,
      chamberNo: chamberMode && row.chamberFlag ? row.equipmentNo : '',
      totalTimeMinutes: Math.ceil((clippedEnd.getTime() - clippedStart.getTime()) / 60_000),
    }]
  }).sort((a, b) => a.areaNo.localeCompare(b.areaNo) || a.equipmentType.localeCompare(b.equipmentType) || a.equipmentNo.localeCompare(b.equipmentNo) || a.startTime.localeCompare(b.startTime))
  reportIsChamberMode.value = chamberMode
  hasQueried.value = true
}

function onEquipmentFilterChange() {
  if (!equipmentFilterEnabled.value) clearFilter('equipment')
}

function onStateFilterChange() {
  if (!stateFilterEnabled.value) clearFilter('state')
}
</script>

<template>
  <section v-if="developmentPreview" class="state-statistics" aria-label="設備狀態期間統計查詢 DEV 預覽">
    <header class="page-heading">
      <div>
        <p class="eyebrow">EMS / ACC EXECUTION · ES409</p>
        <h1>設備狀態期間統計查詢</h1>
        <p class="source-note">查詢區間內設備各狀態的起訖與分鐘數；DEV 假資料，不連正式服務。</p>
      </div>
      <span class="dev-badge">DEV MOCK</span>
    </header>

    <section class="query-panel" aria-label="查詢條件">
      <div class="filter-grid">
        <fieldset class="lookup-filter">
          <legend><label><input v-model="stateFilterEnabled" type="checkbox" @change="onStateFilterChange"> 設備狀態</label></legend>
          <div class="lookup-line">
            <input :value="selectedStates.join(', ') || '不限狀態'" type="text" readonly :disabled="!stateFilterEnabled" aria-label="已選設備狀態代碼">
            <button type="button" class="secondary-button" :disabled="!stateFilterEnabled" @click="openPicker('state')">選擇…</button>
          </div>
          <small>{{ selectedStateSummary || '未勾選時查詢全部狀態' }}</small>
        </fieldset>
        <fieldset class="lookup-filter">
          <legend><label><input v-model="equipmentFilterEnabled" type="checkbox" @change="onEquipmentFilterChange"> 設備編號</label></legend>
          <div class="lookup-line">
            <input :value="selectedEquipment.join(', ') || '不限設備'" type="text" readonly :disabled="!equipmentFilterEnabled" aria-label="已選設備編號">
            <button type="button" class="secondary-button" :disabled="!equipmentFilterEnabled" @click="openPicker('equipment')">選擇…</button>
          </div>
          <small>{{ selectedEquipmentSummary || '可多選設備；未勾選時查詢全部設備' }}</small>
        </fieldset>
        <label class="date-field">開始時間<input v-model="startTime" type="datetime-local" step="1"></label>
        <label class="date-field">結束時間<input v-model="endTime" type="datetime-local" step="1"></label>
        <label v-if="chamberAvailable" class="chamber-toggle"><input v-model="showChambers" type="checkbox"> Chamber 子設備</label>
      </div>
      <div class="query-actions">
        <span v-if="chamberAvailable" class="parameter-hint">DEV 模擬 `ChamberManage = 1`，可切換母設備／腔體顯示。</span>
        <span v-else class="parameter-hint">系統參數未啟用 Chamber 顯示。</span>
        <button type="button" class="primary-button" @click="query">查詢</button>
        <button type="button" class="secondary-button" @click="emit('close')">關閉</button>
      </div>
      <p v-if="errorMessage" class="error-message" role="alert">{{ errorMessage }}</p>
    </section>

    <section class="result-panel" aria-label="設備狀態統計結果">
      <div class="result-heading">
        <div><h2>查詢結果</h2><span>{{ hasQueried ? `共 ${reportRows.length} 筆` : '請設定條件後查詢' }}</span></div>
        <span v-if="hasQueried" class="result-range">{{ startTime.replace('T', ' ') }} ～ {{ endTime.replace('T', ' ') }}</span>
      </div>
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>AreaNo</th><th>EquipmentNo</th><th v-if="reportIsChamberMode">ChamberNo</th><th>Status</th>
              <th>StartTime</th><th class="number-cell">Totaltime_Min</th><th>Reason</th><th>EndTime</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(row, index) in reportRows" :key="`${row.equipmentNo}-${row.equipmentState}-${row.startTime}-${index}`">
              <td>{{ row.areaNo }}</td><td>{{ row.visibleEquipmentNo }}</td><td v-if="reportIsChamberMode">{{ row.chamberNo || '—' }}</td>
              <td><span class="state-pill" :class="`state-${row.equipmentState}`">{{ row.status }}</span></td>
              <td>{{ displayTime(row.startTime) }}</td><td class="number-cell">{{ row.totalTimeMinutes }}</td><td>{{ row.reason }}</td><td>{{ displayTime(row.endTime) }}</td>
            </tr>
            <tr v-if="hasQueried && reportRows.length === 0"><td :colspan="reportIsChamberMode ? 8 : 7" class="empty-state">此區間沒有符合條件的設備狀態記錄。</td></tr>
            <tr v-if="!hasQueried"><td :colspan="reportIsChamberMode ? 8 : 7" class="empty-state">尚未執行查詢。</td></tr>
          </tbody>
        </table>
      </div>
      <p class="table-footnote">分鐘數依原報表區間裁切後向上取整；EquipmentType 與 EquipmentState 為隱藏識別欄位。</p>
    </section>

    <div v-if="activePicker" class="dialog-backdrop" @click.self="activePicker = null">
      <section class="picker-dialog" role="dialog" aria-modal="true" :aria-label="pickerTitle">
        <header><h2>{{ pickerTitle }}</h2><button type="button" class="icon-button" aria-label="關閉" @click="activePicker = null">×</button></header>
        <div class="picker-options">
          <label v-for="option in pickerOptions" :key="option.code" class="picker-option">
            <input type="checkbox" :checked="draftSelection.includes(option.code)" @change="toggleDraft(option.code)">
            <span><strong>{{ option.code }}</strong><small>{{ option.name }}<template v-if="'type' in option"> · {{ option.type }}</template></small></span>
          </label>
        </div>
        <footer><button type="button" class="secondary-button" @click="draftSelection = []">清除</button><span></span><button type="button" class="secondary-button" @click="activePicker = null">取消</button><button type="button" class="primary-button" @click="applyPicker">套用</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.state-statistics { height: 100%; min-height: 0; display: flex; flex-direction: column; gap: 12px; padding: 14px; overflow: auto; box-sizing: border-box; color: #29281f; background: #f4f2ea; font: 13px/1.4 "Segoe UI", "Microsoft JhengHei", sans-serif; }
.page-heading,.result-heading,.query-actions,.lookup-line,.picker-dialog header,.picker-dialog footer { display: flex; align-items: center; }
.page-heading { justify-content: space-between; gap: 16px; }
.page-heading h1 { margin: 0; font-size: 20px; font-weight: 650; }
.page-heading p { margin: 3px 0 0; color: #6b685c; }
.page-heading .eyebrow { margin: 0 0 4px; color: #716b54; font-size: 11px; letter-spacing: .1em; }
.dev-badge { padding: 4px 8px; border: 1px solid #b2a477; border-radius: 3px; color: #6b5722; background: #f5edcf; font-size: 10px; font-weight: 700; white-space: nowrap; }
.query-panel,.result-panel { border: 1px solid #c9c5b8; background: #fbfaf6; box-shadow: 0 1px 2px #2724140c; }
.query-panel { padding: 12px; }
.filter-grid { display: grid; grid-template-columns: minmax(220px, 1fr) minmax(220px, 1fr) minmax(190px, .8fr) minmax(190px, .8fr) auto; align-items: end; gap: 10px; }
.lookup-filter { min-width: 0; margin: 0; padding: 0; border: 0; }
.lookup-filter legend { margin-bottom: 5px; font-weight: 600; }
.lookup-filter legend label,.chamber-toggle { display: flex; align-items: center; gap: 6px; }
.lookup-line { gap: 5px; }
input[type="text"],input[type="datetime-local"] { width: 100%; min-width: 0; height: 31px; box-sizing: border-box; padding: 4px 7px; border: 1px solid #b7b3a8; border-radius: 2px; color: #302f29; background: #fffefa; font: inherit; }
input:disabled { color: #858274; background: #f1f0ea; }
input[type="checkbox"] { accent-color: #6d7952; }
.lookup-filter small { display: block; min-height: 16px; margin-top: 3px; overflow: hidden; color: #777466; text-overflow: ellipsis; white-space: nowrap; }
.date-field { display: grid; gap: 4px; font-weight: 600; }
.chamber-toggle { align-self: center; padding-top: 18px; white-space: nowrap; }
.query-actions { justify-content: flex-end; gap: 7px; margin-top: 11px; }
.parameter-hint { flex: 1; color: #777466; font-size: 11px; }
.primary-button,.secondary-button,.icon-button { min-height: 30px; padding: 4px 10px; border: 1px solid #aaa596; border-radius: 2px; color: #343229; background: #f7f6f0; font: inherit; cursor: pointer; white-space: nowrap; }
.primary-button { border-color: #48543b; color: #fff; background: #59684a; }
.primary-button:hover { background: #4c5b3d; }
.secondary-button:hover,.icon-button:hover { background: #ebe9df; }
button:disabled { cursor: not-allowed; opacity: .5; }
button:focus-visible,input:focus-visible { outline: 2px solid #a48b27; outline-offset: 1px; }
.error-message { margin: 8px 0 0; color: #9c2f21; }
.result-panel { min-height: 170px; display: flex; flex-direction: column; }
.result-heading { justify-content: space-between; gap: 12px; padding: 9px 11px; border-bottom: 1px solid #d4d0c4; }
.result-heading div { display: flex; align-items: baseline; gap: 9px; }
.result-heading h2 { margin: 0; font-size: 14px; }
.result-heading span,.result-range { color: #777466; font-size: 11px; }
.table-wrap { overflow: auto; }
table { width: 100%; min-width: 900px; border-collapse: collapse; font-size: 12px; }
th,td { padding: 7px 9px; border-bottom: 1px solid #e2dfd5; text-align: left; white-space: nowrap; }
th { position: sticky; top: 0; z-index: 1; color: #555246; background: #eeece3; font-size: 11px; font-weight: 650; }
tbody tr:hover { background: #f4f2e8; }
.number-cell { text-align: right; font-variant-numeric: tabular-nums; }
.state-pill { display: inline-block; min-width: 46px; padding: 2px 6px; border-radius: 10px; color: #43513a; background: #e2eadb; text-align: center; }
.state-20 { color: #745f27; background: #f4e9c6; }.state-30 { color: #8b392d; background: #f5dfda; }.state-40 { color: #554d84; background: #e7e3f3; }
.empty-state { height: 80px; color: #777466; text-align: center; }
.table-footnote { margin: auto 10px 8px; color: #777466; font-size: 11px; }
.dialog-backdrop { position: fixed; inset: 0; z-index: 1200; display: grid; place-items: center; padding: 20px; background: #17170fb0; }
.picker-dialog { width: min(480px, 100%); max-height: min(580px, 90vh); display: flex; flex-direction: column; border: 1px solid #aaa596; border-radius: 4px; background: #fbfaf6; box-shadow: 0 16px 48px #0004; }
.picker-dialog header,.picker-dialog footer { justify-content: space-between; gap: 8px; padding: 10px 12px; }
.picker-dialog header { border-bottom: 1px solid #d4d0c4; }.picker-dialog header h2 { margin: 0; font-size: 15px; }
.picker-dialog footer { border-top: 1px solid #d4d0c4; }.picker-dialog footer span { flex: 1; }
.icon-button { min-width: 30px; padding: 0; font-size: 20px; line-height: 1; }
.picker-options { display: grid; gap: 1px; padding: 8px; overflow: auto; }
.picker-option { display: flex; align-items: center; gap: 9px; padding: 8px; border-radius: 2px; cursor: pointer; }.picker-option:hover { background: #eeece3; }
.picker-option span { display: grid; gap: 2px; }.picker-option small { color: #777466; }
@media (max-width: 950px) { .filter-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }.chamber-toggle { align-self: end; padding: 6px 0; }.query-actions { flex-wrap: wrap; }.parameter-hint { flex-basis: 100%; } }
@media (max-width: 560px) { .state-statistics { padding: 9px; }.filter-grid { grid-template-columns: 1fr; }.result-range { display: none; }.page-heading h1 { font-size: 17px; } }
</style>
