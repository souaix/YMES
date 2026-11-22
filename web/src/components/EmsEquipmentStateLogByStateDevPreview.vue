<script setup lang="ts">
import { computed, ref, watch } from 'vue'

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)

interface StateDefinition { code: number; name: string; color: string }
interface StateLog { equipmentType: string; equipmentNo: string; equipmentState: number; userNo: string; startTime: string; endTime: string }
interface DailyRow { equipmentState: number; startTime: string; totalTime: number }

const stateDefinitions: StateDefinition[] = [
  { code: 0, name: '待機', color: '#78b98c' },
  { code: 1, name: '生產', color: '#4a9d68' },
  { code: 2, name: '故障', color: '#dd6b65' },
  { code: 3, name: '換線', color: '#4f91c6' },
  { code: 4, name: '保養', color: '#d5a640' },
  { code: 5, name: '待料', color: '#c98254' },
  { code: 6, name: '工程確認', color: '#8b70b5' },
  { code: 7, name: '點檢', color: '#4ea9a4' },
  { code: 8, name: '暖機', color: '#7587bd' },
  { code: 9, name: '待作業員', color: '#b67c9c' },
  { code: 10, name: '校驗', color: '#9a9a65' },
  { code: 11, name: '工程停機', color: '#777777' },
]

function localDateTime(value: Date): string {
  const pad = (number: number) => String(number).padStart(2, '0')
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}:${pad(value.getSeconds())}`
}

function dayOffset(days: number, hour: number, minute = 0): Date {
  const value = new Date()
  value.setDate(value.getDate() + days)
  value.setHours(hour, minute, 0, 0)
  return value
}

function makeFixtureLogs(): StateLog[] {
  const rows: StateLog[] = []
  const dayOffsets = [-14, -10, -7, -6, -3, -1, 0]
  stateDefinitions.forEach((state, stateIndex) => {
    dayOffsets.forEach((offset, dayIndex) => {
      const start = dayOffset(offset, 6 + (stateIndex % 9), (stateIndex * 7) % 60)
      const durationMinutes = 35 + ((stateIndex * 29 + dayIndex * 17) % 380)
      const end = new Date(start.getTime() + durationMinutes * 60_000)
      rows.push({
        equipmentType: stateIndex % 3 === 0 ? 'DEV-BONDER' : stateIndex % 3 === 1 ? 'DEV-TESTER' : 'DEV-OVEN',
        equipmentNo: `DEV-EQP-${String(101 + ((stateIndex + dayIndex) % 6)).padStart(3, '0')}`,
        equipmentState: state.code,
        userNo: `DEV-OP-${String(1 + ((stateIndex * 2 + dayIndex) % 5)).padStart(2, '0')}`,
        startTime: localDateTime(start),
        endTime: localDateTime(end),
      })

      if (offset === -7 || offset === -1) {
        const secondStart = dayOffset(offset, 16 + (stateIndex % 4), 15)
        const secondEnd = new Date(secondStart.getTime() + (25 + stateIndex * 9) * 60_000)
        rows.push({
          equipmentType: stateIndex % 2 === 0 ? 'DEV-TESTER' : 'DEV-BONDER',
          equipmentNo: `DEV-EQP-${String(201 + ((stateIndex + dayIndex) % 4)).padStart(3, '0')}`,
          equipmentState: state.code,
          userNo: `DEV-OP-${String(2 + ((stateIndex + dayIndex) % 4)).padStart(2, '0')}`,
          startTime: localDateTime(secondStart),
          endTime: localDateTime(secondEnd),
        })
      }
    })
  })
  return rows
}

const mockLogs = ref<StateLog[]>(dev.value ? makeFixtureLogs() : [])
const equipmentTypes = computed(() => [...new Set(mockLogs.value.map((row) => row.equipmentType))].sort())
const equipmentNumbers = computed(() => [...new Set(mockLogs.value.map((row) => row.equipmentNo))].sort())
const users = computed(() => [...new Set(mockLogs.value.map((row) => row.userNo))].sort())

const equipmentType = ref('')
const equipmentTypeChecked = ref(false)
const equipmentNo = ref('')
const equipmentNoChecked = ref(false)
const userNo = ref('')
const userNoChecked = ref(false)
const equipmentState = ref<number | null>(null)
watch(equipmentType, (value) => { equipmentTypeChecked.value = value.trim() !== '' })
watch(equipmentTypeChecked, (checked) => { if (!checked) equipmentType.value = '' })
watch(equipmentNo, (value) => { equipmentNoChecked.value = value.trim() !== '' })
watch(equipmentNoChecked, (checked) => { if (!checked) equipmentNo.value = '' })
watch(userNo, (value) => { userNoChecked.value = value.trim() !== '' })
watch(userNoChecked, (checked) => { if (!checked) userNo.value = '' })

const now = new Date()
const sevenDaysAgo = new Date(now)
sevenDaysAgo.setDate(sevenDaysAgo.getDate() - 7)
const startFrom = ref(localDateTime(new Date(sevenDaysAgo.getFullYear(), sevenDaysAgo.getMonth(), sevenDaysAgo.getDate(), 0, 0, 0)))
const startTo = ref(localDateTime(new Date(sevenDaysAgo.getFullYear(), sevenDaysAgo.getMonth(), sevenDaysAgo.getDate(), 23, 59, 59)))
const startFromChecked = ref(true)
const startToChecked = ref(true)

const lookupMode = ref<'equipmentType' | 'equipmentNo' | 'userNo' | null>(null)
const lookupSearch = ref('')
const lookupOptions = computed(() => {
  const options = lookupMode.value === 'equipmentType'
    ? equipmentTypes.value
    : lookupMode.value === 'equipmentNo'
      ? equipmentNumbers.value
      : lookupMode.value === 'userNo'
        ? users.value
        : []
  return options.filter((option) => !lookupSearch.value || option.toLowerCase().includes(lookupSearch.value.trim().toLowerCase()))
})

const queryError = ref('')
const reportVisible = ref(false)
const reportMode = ref<'table' | 'bar'>('table')
const reportRows = ref<DailyRow[]>([])
const matchedLogCount = ref(0)
const reportStateCode = ref<number | null>(null)
const reportState = computed(() => stateDefinitions.find((state) => state.code === reportStateCode.value) ?? null)
const totalHours = computed(() => reportRows.value.reduce((sum, row) => sum + row.totalTime, 0))
const largestHours = computed(() => Math.max(...reportRows.value.map((row) => row.totalTime), 0))

const filteredFixtureRows = computed(() => mockLogs.value.filter((row) => {
  if (equipmentState.value !== null && row.equipmentState !== equipmentState.value) return false
  if (equipmentTypeChecked.value && equipmentType.value && row.equipmentType.toLowerCase() !== equipmentType.value.trim().toLowerCase()) return false
  if (equipmentNoChecked.value && equipmentNo.value && row.equipmentNo.toLowerCase() !== equipmentNo.value.trim().toLowerCase()) return false
  if (userNoChecked.value && userNo.value && row.userNo.toLowerCase() !== userNo.value.trim().toLowerCase()) return false

  const from = startFromChecked.value ? startFrom.value : ''
  const to = startToChecked.value ? startTo.value : ''
  const timestamp = new Date(row.startTime).getTime()
  if (from && timestamp < new Date(from).getTime()) return false
  if (to && timestamp > new Date(to).getTime()) return false
  return true
}))

function aggregateByStartDay(rows: StateLog[], stateCode: number): DailyRow[] {
  const totals = new Map<string, number>()
  for (const row of rows) {
    const durationHours = (new Date(row.endTime).getTime() - new Date(row.startTime).getTime()) / 3_600_000
    const dailyRoundedHours = Number(durationHours.toFixed(2))
    if (dailyRoundedHours === 0) continue
    const dateKey = row.startTime.slice(0, 10)
    totals.set(dateKey, Number(((totals.get(dateKey) ?? 0) + dailyRoundedHours).toFixed(2)))
  }
  return [...totals.entries()]
    .sort(([dateA], [dateB]) => dateA.localeCompare(dateB))
    .map(([date, totalTime]) => ({ equipmentState: stateCode, startTime: date, totalTime }))
}

function runReport(mode: 'table' | 'bar'): void {
  if (equipmentState.value === null) {
    queryError.value = '請先選擇設備狀態。'
    reportVisible.value = false
    return
  }
  const stateCode = equipmentState.value
  queryError.value = ''
  const matched = filteredFixtureRows.value
  matchedLogCount.value = matched.length
  reportStateCode.value = stateCode
  reportRows.value = aggregateByStartDay(matched, stateCode)
  reportMode.value = mode
  reportVisible.value = true
}

function openLookup(mode: 'equipmentType' | 'equipmentNo' | 'userNo'): void {
  lookupSearch.value = ''
  lookupMode.value = mode
}

function chooseLookup(option: string): void {
  if (lookupMode.value === 'equipmentType') equipmentType.value = option
  if (lookupMode.value === 'equipmentNo') equipmentNo.value = option
  if (lookupMode.value === 'userNo') userNo.value = option
  lookupMode.value = null
}

function closeLookup(): void { lookupMode.value = null }
</script>

<template>
  <section v-if="dev" class="equipment-state-log-by-state" aria-label="設備歷史資料查詢_狀態 DEV">
    <header class="page-header">
      <div><div class="eyebrow">EMS · ES403 · DEV fixtures</div><h1>設備歷史資料查詢_狀態 <span>STATE PRODUCTION TIME</span></h1></div>
      <button type="button" class="close-icon" aria-label="關閉" @click="emit('close')">×</button>
    </header>

    <div class="fixture-banner"><strong>DEV 假資料</strong><span>必選設備狀態；表格與趨勢圖只使用本頁虛構設備歷程。</span></div>

    <section class="criteria-panel" aria-label="查詢條件">
      <div class="section-heading"><h2>Query Conditions</h2><span>設備狀態必選；設備類別、設備編號與人員可選</span></div>
      <div class="criteria-grid">
        <div class="condition-row">
          <label for="equipment-state">Equipment State <span class="required">*</span></label>
          <select id="equipment-state" v-model="equipmentState">
            <option :value="null">請選擇狀態</option>
            <option v-for="state in stateDefinitions" :key="state.code" :value="state.code">{{ state.name }} ({{ state.code }})</option>
          </select>
        </div>
        <div class="condition-row">
          <label class="check-label"><input v-model="equipmentTypeChecked" type="checkbox" />Equipment Type</label>
          <div class="lookup-input"><input v-model="equipmentType" type="text" placeholder="DEV-TESTER" /><button type="button" aria-label="查找設備類別" @click="openLookup('equipmentType')">…</button></div>
        </div>
        <div class="condition-row">
          <label class="check-label"><input v-model="equipmentNoChecked" type="checkbox" />Equipment No</label>
          <div class="lookup-input"><input v-model="equipmentNo" type="text" placeholder="DEV-EQP-101" @blur="equipmentNo = equipmentNo.toUpperCase()" /><button type="button" aria-label="查找設備編號" @click="openLookup('equipmentNo')">…</button></div>
        </div>
        <div class="condition-row">
          <label class="check-label"><input v-model="userNoChecked" type="checkbox" />User No</label>
          <div class="lookup-input"><input v-model="userNo" type="text" placeholder="DEV-OP-01" @blur="userNo = userNo.toUpperCase()" /><button type="button" aria-label="查找人員" @click="openLookup('userNo')">…</button></div>
        </div>
      </div>

      <div class="date-range">
        <strong>Status Start Time</strong>
        <label><input v-model="startFromChecked" type="checkbox" />From</label><input v-model="startFrom" type="datetime-local" step="1" :disabled="!startFromChecked" />
        <label><input v-model="startToChecked" type="checkbox" />To</label><input v-model="startTo" type="datetime-local" step="1" :disabled="!startToChecked" />
      </div>

      <div class="query-actions">
        <button type="button" class="primary-button" @click="runReport('table')">Query</button>
        <button type="button" class="secondary-button" @click="runReport('bar')">Trend Chart</button>
        <button type="button" class="plain-button" @click="emit('close')">Close (X)</button>
      </div>
      <p class="criteria-note">來源可見條件只有 Status Start Time；From／To 各自勾選，範圍包含起訖時間。結果依歷程開始日彙總總時數。</p>
      <p v-if="queryError" class="validation-message" role="alert">{{ queryError }}</p>
    </section>

    <section v-if="reportVisible" class="report-panel" aria-live="polite">
      <header class="report-header">
        <div><div class="eyebrow">{{ reportMode === 'table' ? 'State Production Time · Data' : 'Equipment State Total Time of Daily' }}</div><h2>{{ reportState?.name }} ({{ reportState?.code }}) · 每日狀態時數</h2></div>
        <div class="report-summary"><strong>{{ reportRows.length }}</strong><span>日</span><strong>{{ totalHours.toFixed(2) }}</strong><span>小時</span></div>
      </header>
      <div v-if="reportRows.length === 0" class="empty-report">查無符合條件的狀態歷程。</div>

      <div v-else-if="reportMode === 'table'" class="table-scroll">
        <table>
          <thead><tr><th>EquipmentState</th><th>StartTime</th><th>TotalTime (hr)</th></tr></thead>
          <tbody><tr v-for="row in reportRows" :key="row.startTime"><td>{{ row.equipmentState }}</td><td>{{ row.startTime }}</td><td>{{ row.totalTime.toFixed(2) }}</td></tr></tbody>
        </table>
      </div>

      <div v-else class="bar-chart" role="img" aria-label="每日設備狀態時數趨勢長條圖">
        <div v-for="row in reportRows" :key="row.startTime" class="bar-row">
          <div class="bar-label"><span>{{ row.startTime }}</span><small>{{ row.totalTime.toFixed(2) }} hr</small></div>
          <div class="bar-track"><div class="bar-fill" :style="{ width: `${largestHours ? Math.max(row.totalTime, 0) / largestHours * 100 : 0}%`, backgroundColor: reportState?.color ?? '#777777' }"><span>{{ row.totalTime.toFixed(2) }}</span></div></div>
        </div>
      </div>
      <footer>符合條件 {{ matchedLogCount }} 筆原始歷程；Core 將非零時數按 StartTime 日曆日累加並排序。</footer>
    </section>

    <div v-if="lookupMode" class="lookup-backdrop" @click.self="closeLookup">
      <section class="lookup-dialog" role="dialog" aria-modal="true" :aria-label="`查找${lookupMode === 'equipmentType' ? '設備類別' : lookupMode === 'equipmentNo' ? '設備編號' : '人員'}`">
        <header><div><strong>{{ lookupMode === 'equipmentType' ? 'Equipment Type' : lookupMode === 'equipmentNo' ? 'Equipment No' : 'User No' }}</strong><span>固定 DEV 查找列</span></div><button type="button" aria-label="關閉查找視窗" @click="closeLookup">×</button></header>
        <input v-model="lookupSearch" class="lookup-search" type="search" placeholder="搜尋" />
        <div class="lookup-options"><button v-for="option in lookupOptions" :key="option" type="button" @click="chooseLookup(option)">{{ option }}<span>選取</span></button><p v-if="!lookupOptions.length" class="empty-row">沒有符合的假資料。</p></div>
        <footer><button type="button" class="secondary-button" @click="closeLookup">Cancel</button></footer>
      </section>
    </div>
  </section>
  <section v-else class="empty-state" aria-label="DEV 假資料未啟用">DEV 預覽未啟用。</section>
</template>

<style scoped>
.equipment-state-log-by-state { height: 100%; min-height: 0; overflow: auto; padding: 16px; box-sizing: border-box; background: #f5f4ef; color: #26251f; font: inherit; }
.page-header,.report-header,.section-heading,.query-actions,.lookup-dialog header,.lookup-dialog footer { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.page-header,.fixture-banner,.criteria-panel,.report-panel { max-width: 1120px; margin: 0 auto 12px; }
.eyebrow,.section-heading span,.lookup-dialog header span { color: #777366; font-size: 12px; }
h1 { margin: 3px 0 0; font-size: 22px; } h1 span { margin-left: 8px; color: #777366; font-size: 13px; font-weight: 500; }
.close-icon,.lookup-dialog header button { width: 32px; height: 32px; border: 1px solid #c9c6b9; border-radius: 4px; background: white; color: #333; font-size: 21px; cursor: pointer; }
.fixture-banner,.criteria-panel,.report-panel { border: 1px solid #d9d6ca; border-radius: 6px; background: white; }
.fixture-banner { display: flex; align-items: center; gap: 10px; padding: 9px 12px; background: #fff9df; color: #655612; font-size: 13px; }
.fixture-banner strong { padding: 2px 7px; border-radius: 10px; background: #efdb87; font-size: 11px; }
.criteria-panel { padding: 14px; }
.section-heading { justify-content: flex-start; margin-bottom: 10px; } .section-heading h2,.report-header h2 { margin: 0; font-size: 15px; }
.criteria-grid { display: grid; grid-template-columns: repeat(2, minmax(260px, 1fr)); gap: 8px 20px; }
.condition-row { display: grid; grid-template-columns: 155px minmax(0, 1fr); gap: 8px; align-items: center; }
.condition-row > label:first-child:not(.check-label) { color: #4b4940; font-size: 13px; }
.required { color: #b13a32; font-weight: 700; }
.check-label { display: flex; align-items: center; gap: 7px; color: #4b4940; font-size: 13px; }
.lookup-input { display: flex; min-width: 0; gap: 5px; }
.lookup-input input,.lookup-search,.date-range input[type="datetime-local"],.condition-row select { min-width: 0; min-height: 31px; box-sizing: border-box; border: 1px solid #c9c6b9; border-radius: 3px; padding: 5px 8px; background: #fbfaf6; color: #292821; font: inherit; }
.lookup-input input { flex: 1; }
.lookup-input button { width: 34px; border: 1px solid #aaa696; border-radius: 3px; background: #f4f2e9; font: inherit; cursor: pointer; }
.condition-row select { width: 100%; }
.date-range label { display: flex; align-items: center; gap: 6px; color: #4b4940; font-size: 12px; }
.date-range { display: grid; grid-template-columns: 150px 72px minmax(180px, 1fr) 52px minmax(180px, 1fr); align-items: center; gap: 8px; padding: 9px 10px; background: #fbfaf6; }
.date-range strong { font-size: 13px; }
.date-range input:disabled { color: #999; background: #eee; }
.query-actions { justify-content: flex-end; flex-wrap: wrap; margin-top: 14px; }
.primary-button,.secondary-button,.plain-button { min-height: 31px; padding: 5px 13px; border: 1px solid #3b3930; border-radius: 3px; font: inherit; cursor: pointer; }
.primary-button { background: #3b3930; color: white; }.secondary-button { background: white; color: #333; }.plain-button { border-color: #c9c6b9; background: #f7f5ee; color: #333; }
.criteria-note,.validation-message { margin: 9px 0 0; color: #777366; font-size: 11px; }
.validation-message { color: #a12e28; font-size: 12px; }
.report-panel { padding: 14px; }
.report-header { margin-bottom: 10px; }
.report-header h2 { margin-top: 3px; }
.report-summary { display: flex; align-items: baseline; gap: 6px; color: #777366; font-size: 12px; }
.report-summary strong { margin-left: 8px; color: #292821; font-size: 18px; }
.table-scroll { overflow: auto; }
table { width: 100%; border-collapse: collapse; font-size: 12px; white-space: nowrap; }
th,td { padding: 8px 10px; border-bottom: 1px solid #e7e4dc; text-align: left; }
th { background: #f2f0e8; color: #565349; font-weight: 600; }
.empty-report,.empty-row { padding: 18px; color: #777366; text-align: center; }
.bar-chart { display: grid; gap: 9px; max-height: 470px; overflow: auto; padding: 12px 2px; }
.bar-row { display: grid; grid-template-columns: 190px minmax(0, 1fr); align-items: center; gap: 12px; }
.bar-label { display: flex; justify-content: space-between; gap: 8px; font-size: 12px; } .bar-label small { color: #777366; white-space: nowrap; }
.bar-track { height: 22px; overflow: hidden; border-radius: 3px; background: #f0eee8; }
.bar-fill { height: 100%; min-width: 2px; border-radius: 3px; color: white; font-size: 11px; text-align: right; }
.bar-fill span { display: block; padding: 3px 6px; text-shadow: 0 1px 2px #222; }
.report-panel footer { margin-top: 10px; color: #777366; font-size: 11px; }
.lookup-backdrop { position: fixed; inset: 0; z-index: 40; display: grid; place-items: center; padding: 16px; background: rgb(25 24 20 / 40%); }
.lookup-dialog { width: min(460px, 100%); max-height: 85vh; display: flex; flex-direction: column; gap: 12px; padding: 14px; border-radius: 7px; background: white; box-shadow: 0 16px 48px rgb(0 0 0 / 24%); }
.lookup-dialog header div { display: grid; gap: 3px; }.lookup-search { width: 100%; background: white; }
.lookup-options { overflow: auto; display: grid; gap: 5px; }
.lookup-options button { display: flex; justify-content: space-between; padding: 9px; border: 1px solid #ddd9ce; border-radius: 4px; background: #fbfaf6; text-align: left; font: inherit; cursor: pointer; }
.lookup-options button:hover { background: #fff9df; }.lookup-options span { color: #777366; font-size: 11px; }.lookup-dialog footer { justify-content: flex-end; }
.empty-state { padding: 16px; color: #777366; }
button:focus-visible,input:focus-visible,select:focus-visible { outline: 2px solid #d1b400; outline-offset: 2px; }
@media (max-width: 760px) {
  .equipment-state-log-by-state { padding: 10px; } h1 span { display: block; margin: 2px 0; }
  .criteria-grid { grid-template-columns: 1fr; }.condition-row { grid-template-columns: 130px minmax(0, 1fr); }
  .date-range { grid-template-columns: 1fr 1fr; }.date-range strong { grid-column: 1 / -1; }
  .bar-row { grid-template-columns: 1fr; gap: 4px; }
}
</style>
