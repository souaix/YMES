<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

type LookupKind = 'equipmentType' | 'equipmentNo' | 'userNo'
type StateLogRow = {
  equipmentType: string
  equipmentNo: string
  equipmentState: string
  stateName: string
  stateColor: string
  eqpSerialNo: string
  startTime: string
  endTime: string
  userNo: string
  lotNo: string
  opNo: string
  lotSerial: string
  description: string
}

const localInput = (date: Date) => new Date(date.getTime() - date.getTimezoneOffset() * 60_000).toISOString().slice(0, 19)
const dayAt = (offset: number) => {
  const date = new Date()
  date.setHours(0, 0, 0, 0)
  date.setDate(date.getDate() + offset)
  return date
}
const atTime = (dayOffset: number, hour: number, minute: number, second = 0) => {
  const date = dayAt(dayOffset)
  date.setHours(hour, minute, second, 0)
  return localInput(date)
}
const dateAt = (date: Date, hour: number, minute: number, second = 0) => {
  const value = new Date(date)
  value.setHours(hour, minute, second, 0)
  return localInput(value)
}
const sevenDaysAgo = dayAt(-7)
const today = dayAt(0)
const initialEquipmentNo = (new URLSearchParams(window.location.search).get('strEquipmentNo') ?? '').trim().toUpperCase()

const stateOptions = [
  { code: '10', name: '生產中', color: '#5d8d64' },
  { code: '20', name: '待料', color: '#c49b37' },
  { code: '30', name: '故障', color: '#b94f43' },
  { code: '40', name: '保養', color: '#7467a8' },
]
const equipmentTypeOptions = [
  { code: 'ETCH', name: '蝕刻設備' },
  { code: 'CVD', name: '薄膜設備' },
  { code: 'TESTER', name: '測試設備' },
  { code: 'CLEAN', name: '清洗設備' },
]
const equipmentOptions = [
  { code: 'ETCH-01', name: '蝕刻機一號', type: 'ETCH' },
  { code: 'ETCH-02', name: '蝕刻機二號', type: 'ETCH' },
  { code: 'CVD-01', name: '薄膜設備一號', type: 'CVD' },
  { code: 'TEST-03', name: '測試機三號', type: 'TESTER' },
  { code: 'CLEAN-02', name: '清洗機二號', type: 'CLEAN' },
]
const userOptions = [
  { code: 'OP1008', name: '陳怡君' },
  { code: 'OP2041', name: '林志明' },
  { code: 'OP3092', name: '黃雅婷' },
  { code: 'OP4106', name: '王建宏' },
]
const stateByCode = new Map(stateOptions.map((option) => [option.code, option]))
const fixtureRows: StateLogRow[] = [
  { equipmentType: 'ETCH', equipmentNo: 'ETCH-01', equipmentState: '10', stateName: '生產中', stateColor: '#5d8d64', eqpSerialNo: 'EQPS-20261123-005', startTime: atTime(0, 7, 50), endTime: atTime(0, 12, 0), userNo: 'OP1008', lotNo: 'LOT-ETCH-018', opNo: 'ETCH120', lotSerial: 'LS-120518', description: '今日生產狀態' },
  { equipmentType: 'CLEAN', equipmentNo: 'CLEAN-02', equipmentState: '40', stateName: '保養', stateColor: '#7467a8', eqpSerialNo: 'EQPS-20261116-003', startTime: atTime(-7, 9, 20), endTime: atTime(0, 8, 20), userNo: 'OP4106', lotNo: 'LOT-CLEAN-026', opNo: 'CLN220', lotSerial: 'LS-220826', description: '季度清洗及腔體校正' },
  { equipmentType: 'CVD', equipmentNo: 'CVD-01', equipmentState: '30', stateName: '故障', stateColor: '#b94f43', eqpSerialNo: 'EQPS-20261116-002', startTime: atTime(-7, 10, 45), endTime: atTime(0, 11, 15), userNo: 'OP3092', lotNo: 'LOT-CVD-041', opNo: 'CVD310', lotSerial: 'LS-310441', description: '真空壓力偏低，待工程確認' },
  { equipmentType: 'ETCH', equipmentNo: 'ETCH-01', equipmentState: '10', stateName: '生產中', stateColor: '#5d8d64', eqpSerialNo: 'EQPS-20261116-001', startTime: atTime(-7, 8, 10), endTime: atTime(0, 7, 50), userNo: 'OP1008', lotNo: 'LOT-ETCH-018', opNo: 'ETCH120', lotSerial: 'LS-120518', description: '批次生產，狀態延續至今日' },
  { equipmentType: 'TESTER', equipmentNo: 'TEST-03', equipmentState: '20', stateName: '待料', stateColor: '#c49b37', eqpSerialNo: 'EQPS-20261116-004', startTime: atTime(-7, 13, 5), endTime: atTime(0, 14, 5), userNo: 'OP2041', lotNo: 'LOT-TEST-077', opNo: 'FT420', lotSerial: 'LS-420977', description: '等待批次移入及治具確認' },
]

const equipmentTypeEnabled = ref(false)
const equipmentNoEnabled = ref(initialEquipmentNo !== '')
const userNoEnabled = ref(false)
const equipmentStateEnabled = ref(false)
const equipmentType = ref('')
const equipmentNo = ref(initialEquipmentNo)
const userNo = ref('')
const equipmentState = ref('')
const startFromEnabled = ref(true)
const startEndEnabled = ref(true)
const endFromEnabled = ref(true)
const endEndEnabled = ref(true)
const initialStartDay = initialEquipmentNo ? today : sevenDaysAgo
const startFrom = ref(dateAt(initialStartDay, 0, 0))
const startEnd = ref(dateAt(initialStartDay, 23, 59, 59))
const endFrom = ref(dateAt(today, 0, 0))
const endEnd = ref(dateAt(today, 23, 59, 59))
const rows = ref<StateLogRow[]>([])
const hasQueried = ref(false)
const showGantt = ref(false)
const lookupKind = ref<LookupKind | null>(null)
const lookupDraft = ref('')

const lookupTitle = computed(() => lookupKind.value === 'equipmentType'
  ? '選擇設備類別'
  : lookupKind.value === 'equipmentNo' ? '選擇設備編號' : '選擇使用者')
const lookupOptions = computed(() => lookupKind.value === 'equipmentType'
  ? equipmentTypeOptions
  : lookupKind.value === 'equipmentNo' ? equipmentOptions : userOptions)
const ganttRange = computed(() => {
  if (rows.value.length === 0) return { min: 0, max: 1, span: 1 }
  const min = Math.min(...rows.value.map((row) => new Date(row.startTime).getTime()))
  const max = Math.max(...rows.value.map((row) => new Date(row.endTime).getTime()))
  return { min, max, span: Math.max(max - min, 60_000) }
})
const ganttTicks = computed(() => {
  const { min, max } = ganttRange.value
  return [new Date(min), new Date((min + max) / 2), new Date(max)]
})

function updateTextFilter(kind: LookupKind, value: string) {
  if (kind === 'equipmentType') {
    equipmentType.value = value.toUpperCase()
    equipmentTypeEnabled.value = equipmentType.value !== ''
  } else if (kind === 'equipmentNo') {
    equipmentNo.value = value.toUpperCase()
    equipmentNoEnabled.value = equipmentNo.value !== ''
  } else {
    userNo.value = value.toUpperCase()
    userNoEnabled.value = userNo.value !== ''
  }
}

function toggleTextFilter(kind: LookupKind, enabled: boolean) {
  if (kind === 'equipmentType') {
    equipmentTypeEnabled.value = enabled
    if (!enabled) equipmentType.value = ''
  } else if (kind === 'equipmentNo') {
    equipmentNoEnabled.value = enabled
    if (!enabled) equipmentNo.value = ''
  } else {
    userNoEnabled.value = enabled
    if (!enabled) userNo.value = ''
  }
}

function openLookup(kind: LookupKind) {
  lookupKind.value = kind
  lookupDraft.value = kind === 'equipmentType' ? equipmentType.value : kind === 'equipmentNo' ? equipmentNo.value : userNo.value
}

function applyLookup() {
  if (!lookupKind.value) return
  updateTextFilter(lookupKind.value, lookupDraft.value)
  lookupKind.value = null
}

function toggleStateFilter(enabled: boolean) {
  equipmentStateEnabled.value = enabled
  if (!enabled) equipmentState.value = ''
}

function withinBounds(value: string, lowerEnabled: boolean, lower: string, upperEnabled: boolean, upper: string) {
  const time = new Date(value).getTime()
  if (lowerEnabled && time < new Date(lower).getTime()) return false
  if (upperEnabled && time > new Date(upper).getTime()) return false
  return true
}

function query() {
  const result = fixtureRows.filter((row) => {
    if (equipmentTypeEnabled.value && equipmentType.value && row.equipmentType !== equipmentType.value) return false
    if (equipmentNoEnabled.value && equipmentNo.value && row.equipmentNo !== equipmentNo.value) return false
    if (userNoEnabled.value && userNo.value && row.userNo !== userNo.value) return false
    if (equipmentStateEnabled.value && equipmentState.value && row.equipmentState !== equipmentState.value) return false
    if (!withinBounds(row.startTime, startFromEnabled.value, startFrom.value, startEndEnabled.value, startEnd.value)) return false
    if (!withinBounds(row.endTime, endFromEnabled.value, endFrom.value, endEndEnabled.value, endEnd.value)) return false
    return true
  })
  rows.value = [...result].sort((left, right) => left.equipmentType.localeCompare(right.equipmentType)
    || left.equipmentNo.localeCompare(right.equipmentNo)
    || left.eqpSerialNo.localeCompare(right.eqpSerialNo))
  hasQueried.value = true
}

function displayTime(value: string) {
  return new Date(value).toLocaleString('zh-TW', { hour12: false })
}

function barStyle(row: StateLogRow) {
  const start = new Date(row.startTime).getTime()
  const end = new Date(row.endTime).getTime()
  const left = ((start - ganttRange.value.min) / ganttRange.value.span) * 100
  const width = Math.max(((end - start) / ganttRange.value.span) * 100, 1.2)
  return { left: `${left}%`, width: `${width}%`, backgroundColor: stateByCode.get(row.equipmentState)?.color ?? row.stateColor }
}

onMounted(() => {
  // WinForms accepts an EquipmentNo from the equipment monitor and runs Query immediately.
  // Keep the same optional deep-link parameter for a direct browser entry.
  if (initialEquipmentNo) query()
})
</script>

<template>
  <section class="equipment-state-log" aria-label="設備狀態記錄查詢 DEV 預覽">
    <header class="page-heading">
      <div>
        <p class="eyebrow">EMS / ACC EXECUTION · ES402</p>
        <h1>設備狀態記錄查詢</h1>
        <p class="source-note">依設備、狀態、使用者及起訖時間篩選設備歷程；資料僅為 DEV 假資料。</p>
      </div>
      <span class="dev-badge">DEV MOCK</span>
    </header>

    <section class="query-panel" aria-label="查詢條件">
      <div class="filter-grid text-filters">
        <fieldset class="lookup-filter">
          <legend><label><input :checked="equipmentTypeEnabled" type="checkbox" @change="toggleTextFilter('equipmentType', ($event.target as HTMLInputElement).checked)"> 設備類別</label></legend>
          <div class="lookup-line">
            <input :value="equipmentType" type="text" :disabled="!equipmentTypeEnabled" aria-label="設備類別" @input="updateTextFilter('equipmentType', ($event.target as HTMLInputElement).value)">
            <button type="button" class="secondary-button" @click="openLookup('equipmentType')">…</button>
          </div>
        </fieldset>
        <fieldset class="lookup-filter">
          <legend><label><input :checked="equipmentNoEnabled" type="checkbox" @change="toggleTextFilter('equipmentNo', ($event.target as HTMLInputElement).checked)"> 設備編號</label></legend>
          <div class="lookup-line">
            <input :value="equipmentNo" type="text" :disabled="!equipmentNoEnabled" aria-label="設備編號" @input="updateTextFilter('equipmentNo', ($event.target as HTMLInputElement).value)">
            <button type="button" class="secondary-button" @click="openLookup('equipmentNo')">…</button>
          </div>
        </fieldset>
        <fieldset class="lookup-filter">
          <legend><label><input :checked="userNoEnabled" type="checkbox" @change="toggleTextFilter('userNo', ($event.target as HTMLInputElement).checked)"> 使用者</label></legend>
          <div class="lookup-line">
            <input :value="userNo" type="text" :disabled="!userNoEnabled" aria-label="使用者代碼" @input="updateTextFilter('userNo', ($event.target as HTMLInputElement).value)">
            <button type="button" class="secondary-button" @click="openLookup('userNo')">…</button>
          </div>
        </fieldset>
        <fieldset class="lookup-filter">
          <legend><label><input :checked="equipmentStateEnabled" type="checkbox" @change="toggleStateFilter(($event.target as HTMLInputElement).checked)"> 設備狀態</label></legend>
          <select :value="equipmentState" :disabled="!equipmentStateEnabled" aria-label="設備狀態" @change="equipmentState = ($event.target as HTMLSelectElement).value">
            <option value="">不限狀態</option>
            <option v-for="option in stateOptions" :key="option.code" :value="option.code">{{ option.name }}（{{ option.code }}）</option>
          </select>
        </fieldset>
      </div>

      <div class="date-grid">
        <fieldset class="date-range">
          <legend>Start Time</legend>
          <label><input v-model="startFromEnabled" type="checkbox"> From Date <input v-model="startFrom" type="datetime-local" step="1" :disabled="!startFromEnabled"></label>
          <label><input v-model="startEndEnabled" type="checkbox"> End Date <input v-model="startEnd" type="datetime-local" step="1" :disabled="!startEndEnabled"></label>
        </fieldset>
        <fieldset class="date-range">
          <legend>End Time</legend>
          <label><input v-model="endFromEnabled" type="checkbox"> From Date <input v-model="endFrom" type="datetime-local" step="1" :disabled="!endFromEnabled"></label>
          <label><input v-model="endEndEnabled" type="checkbox"> End Date <input v-model="endEnd" type="datetime-local" step="1" :disabled="!endEndEnabled"></label>
        </fieldset>
      </div>
      <div class="query-actions">
        <span>日期條件各自為可選的包含式下限／上限。</span>
        <button type="button" class="primary-button" @click="query">Query</button>
        <button type="button" class="secondary-button" @click="emit('close')">Close (X)</button>
      </div>
    </section>

    <section class="result-panel" aria-label="設備狀態記錄結果">
      <div class="result-heading">
        <div><h2>Equipment State Log</h2><span>{{ hasQueried ? `共 ${rows.length} 筆` : '尚未執行查詢' }}</span></div>
        <button type="button" class="secondary-button" :disabled="!hasQueried || rows.length === 0" @click="showGantt = true">Gantt Chart</button>
      </div>
      <div class="table-wrap">
        <table>
          <thead>
            <tr><th>Equipment Type</th><th>Equipment No</th><th>Equipment State</th><th>Serial No</th><th>Start Time</th><th>End Time</th><th>User No</th><th>Description</th></tr>
          </thead>
          <tbody>
            <tr v-for="row in rows" :key="row.eqpSerialNo">
              <td>{{ row.equipmentType }}</td><td>{{ row.equipmentNo }}</td><td><span class="state-pill" :style="{ '--state-color': row.stateColor }">{{ row.stateName }}</span></td>
              <td>{{ row.eqpSerialNo }}</td><td>{{ displayTime(row.startTime) }}</td><td>{{ displayTime(row.endTime) }}</td><td>{{ row.userNo }}</td><td class="description-cell">{{ row.description }}</td>
            </tr>
            <tr v-if="!hasQueried"><td colspan="8" class="empty-state">設定條件後按 Query。</td></tr>
            <tr v-else-if="rows.length === 0"><td colspan="8" class="empty-state">沒有符合條件的設備狀態記錄。</td></tr>
          </tbody>
        </table>
      </div>
      <p class="table-footnote">LotNo／OPNo 是 Service 回傳的隱藏欄位；EquipmentState 與 LotSerial 供識別及查詢關聯。</p>
    </section>

    <div v-if="lookupKind" class="dialog-backdrop" @click.self="lookupKind = null">
      <section class="lookup-dialog" role="dialog" aria-modal="true" :aria-label="lookupTitle">
        <header><h2>{{ lookupTitle }}</h2><button type="button" class="icon-button" aria-label="關閉" @click="lookupKind = null">×</button></header>
        <div class="lookup-options">
          <label v-for="option in lookupOptions" :key="option.code" class="lookup-option">
            <input v-model="lookupDraft" type="radio" :value="option.code">
            <span><strong>{{ option.code }}</strong><small>{{ option.name }}<template v-if="'type' in option"> · {{ option.type }}</template></small></span>
          </label>
        </div>
        <footer><button type="button" class="secondary-button" @click="lookupDraft = ''">清除</button><span></span><button type="button" class="secondary-button" @click="lookupKind = null">取消</button><button type="button" class="primary-button" @click="applyLookup">套用</button></footer>
      </section>
    </div>

    <div v-if="showGantt" class="dialog-backdrop" @click.self="showGantt = false">
      <section class="gantt-dialog" role="dialog" aria-modal="true" aria-label="設備狀態 Gantt Chart">
        <header><div><p class="eyebrow">ES402 / 記錄視覺化</p><h2>Equipment State Log · Gantt Chart</h2></div><button type="button" class="icon-button" aria-label="關閉圖表" @click="showGantt = false">×</button></header>
        <div class="gantt-axis"><span v-for="(tick, index) in ganttTicks" :key="index" :style="{ left: `${index * 50}%` }">{{ displayTime(tick.toISOString()) }}</span></div>
        <div class="gantt-rows">
          <div v-for="row in rows" :key="row.eqpSerialNo" class="gantt-row">
            <div class="gantt-label"><strong>{{ row.equipmentNo }}</strong><small>{{ row.stateName }}</small></div>
            <div class="gantt-track"><div class="gantt-bar" :style="barStyle(row)" :title="`${row.equipmentNo} · ${row.stateName} · ${displayTime(row.startTime)} ～ ${displayTime(row.endTime)}`"><span>{{ row.stateName }}</span></div></div>
          </div>
        </div>
        <div class="state-legend"><span v-for="option in stateOptions" :key="option.code"><i :style="{ backgroundColor: option.color }"></i>{{ option.name }}</span></div>
        <footer><span>每列代表一筆設備狀態歷程；狀態色依設備狀態基礎設定示意。</span><button type="button" class="secondary-button" @click="showGantt = false">Close (X)</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.equipment-state-log { height: 100%; min-height: 0; display: flex; flex-direction: column; gap: 12px; padding: 14px; overflow: auto; box-sizing: border-box; color: #29281f; background: #f4f2ea; font: 13px/1.4 "Segoe UI", "Microsoft JhengHei", sans-serif; }
.page-heading,.query-actions,.lookup-line,.result-heading,.lookup-dialog header,.lookup-dialog footer,.gantt-dialog header,.gantt-dialog footer { display: flex; align-items: center; }
.page-heading { justify-content: space-between; gap: 16px; }.page-heading h1 { margin: 0; font-size: 20px; font-weight: 650; }.page-heading p { margin: 3px 0 0; color: #6b685c; }.page-heading .eyebrow,.gantt-dialog .eyebrow { margin: 0 0 4px; color: #716b54; font-size: 11px; letter-spacing: .1em; }
.dev-badge { padding: 4px 8px; border: 1px solid #b2a477; border-radius: 3px; color: #6b5722; background: #f5edcf; font-size: 10px; font-weight: 700; white-space: nowrap; }
.query-panel,.result-panel { border: 1px solid #c9c5b8; background: #fbfaf6; box-shadow: 0 1px 2px #2724140c; }.query-panel { padding: 12px; }
.filter-grid { display: grid; grid-template-columns: repeat(4, minmax(170px, 1fr)); gap: 10px; }.lookup-filter,.date-range { min-width: 0; margin: 0; padding: 0; border: 0; }.lookup-filter legend,.date-range legend { margin-bottom: 5px; font-weight: 600; }.lookup-filter legend label { display: flex; align-items: center; gap: 6px; }
.lookup-line { gap: 5px; } input[type="text"],input[type="datetime-local"],select { width: 100%; min-width: 0; height: 31px; box-sizing: border-box; padding: 4px 7px; border: 1px solid #b7b3a8; border-radius: 2px; color: #302f29; background: #fffefa; font: inherit; } input:disabled,select:disabled { color: #858274; background: #f1f0ea; } input[type="checkbox"],input[type="radio"] { accent-color: #6d7952; }
.date-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 18px; margin-top: 11px; padding-top: 10px; border-top: 1px solid #dedbd0; }.date-range label { display: grid; grid-template-columns: 18px 76px minmax(220px, 1fr); align-items: center; gap: 6px; margin: 5px 0; }.date-range input[type="checkbox"] { margin: 0; }
.query-actions { justify-content: flex-end; gap: 7px; margin-top: 11px; }.query-actions > span { flex: 1; color: #777466; font-size: 11px; }.primary-button,.secondary-button,.icon-button { min-height: 30px; padding: 4px 10px; border: 1px solid #aaa596; border-radius: 2px; color: #343229; background: #f7f6f0; font: inherit; cursor: pointer; white-space: nowrap; }.primary-button { border-color: #48543b; color: #fff; background: #59684a; }.primary-button:hover { background: #4c5b3d; }.secondary-button:hover,.icon-button:hover { background: #ebe9df; }.secondary-button:disabled { cursor: not-allowed; opacity: .5; }button:focus-visible,input:focus-visible,select:focus-visible { outline: 2px solid #a48b27; outline-offset: 1px; }
.result-panel { min-height: 170px; display: flex; flex-direction: column; }.result-heading { justify-content: space-between; gap: 12px; padding: 9px 11px; border-bottom: 1px solid #d4d0c4; }.result-heading div { display: flex; align-items: baseline; gap: 9px; }.result-heading h2 { margin: 0; font-size: 14px; }.result-heading span { color: #777466; font-size: 11px; }
.table-wrap { overflow: auto; } table { width: 100%; min-width: 1060px; border-collapse: collapse; font-size: 12px; } th,td { padding: 7px 9px; border-bottom: 1px solid #e2dfd5; text-align: left; white-space: nowrap; } th { position: sticky; top: 0; z-index: 1; color: #555246; background: #eeece3; font-size: 11px; font-weight: 650; } tbody tr:hover { background: #f4f2e8; }.description-cell { max-width: 270px; overflow: hidden; text-overflow: ellipsis; }.state-pill { display: inline-block; min-width: 48px; padding: 2px 6px; border-left: 4px solid var(--state-color); background: #f0eee5; }.empty-state { height: 80px; color: #777466; text-align: center; }.table-footnote { margin: auto 10px 8px; color: #777466; font-size: 11px; }
.dialog-backdrop { position: fixed; inset: 0; z-index: 1200; display: grid; place-items: center; padding: 20px; background: #17170fb0; }.lookup-dialog,.gantt-dialog { width: min(520px, 100%); max-height: min(650px, 90vh); display: flex; flex-direction: column; border: 1px solid #aaa596; border-radius: 4px; background: #fbfaf6; box-shadow: 0 16px 48px #0004; }.lookup-dialog header,.lookup-dialog footer,.gantt-dialog header,.gantt-dialog footer { justify-content: space-between; gap: 8px; padding: 10px 12px; }.lookup-dialog header,.gantt-dialog header { border-bottom: 1px solid #d4d0c4; }.lookup-dialog header h2,.gantt-dialog header h2 { margin: 0; font-size: 15px; }.lookup-dialog footer,.gantt-dialog footer { border-top: 1px solid #d4d0c4; }.lookup-dialog footer span { flex: 1; }.icon-button { min-width: 30px; padding: 0; font-size: 20px; line-height: 1; }
.lookup-options { display: grid; gap: 1px; padding: 8px; overflow: auto; }.lookup-option { display: flex; align-items: center; gap: 9px; padding: 8px; border-radius: 2px; cursor: pointer; }.lookup-option:hover { background: #eeece3; }.lookup-option span { display: grid; gap: 2px; }.lookup-option small { color: #777466; }
.gantt-dialog { width: min(980px, 96vw); }.gantt-dialog header { align-items: flex-start; }.gantt-dialog h2 { margin: 0; }.gantt-axis { position: relative; height: 34px; margin: 12px 18px 0 190px; border-bottom: 1px solid #c9c5b8; color: #777466; font-size: 10px; }.gantt-axis span { position: absolute; bottom: 4px; transform: translateX(-50%); white-space: nowrap; }.gantt-rows { display: grid; gap: 8px; max-height: 420px; padding: 8px 18px; overflow: auto; }.gantt-row { display: grid; grid-template-columns: 170px minmax(400px, 1fr); align-items: center; gap: 12px; }.gantt-label { display: grid; gap: 2px; }.gantt-label small { color: #777466; }.gantt-track { position: relative; height: 28px; background: repeating-linear-gradient(to right, #eeece3 0, #eeece3 calc(25% - 1px), #d8d4c8 calc(25% - 1px), #d8d4c8 25%); }.gantt-bar { position: absolute; top: 4px; height: 20px; min-width: 6px; display: flex; align-items: center; overflow: hidden; border-radius: 2px; color: white; font-size: 10px; }.gantt-bar span { padding: 0 6px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }.state-legend { display: flex; flex-wrap: wrap; gap: 14px; padding: 4px 18px 12px; color: #656254; font-size: 11px; }.state-legend span { display: flex; align-items: center; gap: 5px; }.state-legend i { width: 10px; height: 10px; border-radius: 50%; }.gantt-dialog footer span { flex: 1; color: #777466; font-size: 11px; }
@media (max-width: 1000px) { .filter-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }.date-grid { grid-template-columns: 1fr; }.date-range label { grid-template-columns: 18px 76px minmax(150px, 1fr); } }
@media (max-width: 620px) { .equipment-state-log { padding: 9px; }.filter-grid { grid-template-columns: 1fr; }.page-heading h1 { font-size: 17px; }.query-actions { align-items: stretch; flex-wrap: wrap; }.query-actions > span { flex-basis: 100%; }.date-range label { grid-template-columns: 18px 68px minmax(0, 1fr); font-size: 11px; }.gantt-axis { margin-left: 120px; }.gantt-row { grid-template-columns: 105px minmax(280px, 1fr); gap: 8px; }.gantt-rows { overflow: auto; }.gantt-dialog footer { align-items: flex-start; flex-wrap: wrap; } }
</style>
