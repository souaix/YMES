<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'

interface BreakdownRepairRow {
  breakdownSerialNo: string
  equipmentNo: string
  state: 0 | 1 | 2
  breakdownStartTime: string
  breakdownEndTime: string
  breakdownDescription: string
  breakdownCreator: string
  breakdownCreateDate: string
  repairSerialNo: string
  repairStartTime: string
  repairEndTime: string
  repairUserNo: string
  repairDescription: string
}

interface DateRange { enabled: boolean; from: string; to: string }

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

const chamberManage = false
const chamberEquipmentNos = new Set(['CNC-02-CH1', 'PRESS-04-CH1'])
const equipmentBasis = ['CNC-02', 'CNC-03', 'CNC-02-CH1', 'PUMP-07', 'PUMP-08', 'PRESS-04', 'PRESS-04-CH1', 'ROBOT-01']
const equipmentOptions = equipmentBasis.filter((equipmentNo) => chamberManage || !chamberEquipmentNos.has(equipmentNo))
const rows: BreakdownRepairRow[] = [
  { breakdownSerialNo: 'BD-2611-0101', equipmentNo: 'CNC-02', state: 0, breakdownStartTime: '2026-11-22 08:42:00', breakdownEndTime: '', breakdownDescription: 'Spindle over-temperature alarm', breakdownCreator: 'CHEN01', breakdownCreateDate: '2026-11-22 08:50:14', repairSerialNo: '', repairStartTime: '', repairEndTime: '', repairUserNo: '', repairDescription: '' },
  { breakdownSerialNo: 'BD-2611-0102', equipmentNo: 'PUMP-07', state: 1, breakdownStartTime: '2026-11-20 14:10:00', breakdownEndTime: '', breakdownDescription: 'Coolant pressure below threshold', breakdownCreator: 'LIN02', breakdownCreateDate: '2026-11-20 14:13:29', repairSerialNo: 'RP-2611-0068', repairStartTime: '2026-11-20 14:35:00', repairEndTime: '', repairUserNo: 'WANG04', repairDescription: '' },
  { breakdownSerialNo: 'BD-2611-0098', equipmentNo: 'PRESS-04', state: 2, breakdownStartTime: '2026-11-18 07:30:00', breakdownEndTime: '2026-11-18 07:42:00', breakdownDescription: 'Safety door interlock open', breakdownCreator: 'WU05', breakdownCreateDate: '2026-11-18 07:34:02', repairSerialNo: 'RP-2611-0061', repairStartTime: '2026-11-18 07:48:00', repairEndTime: '2026-11-18 08:55:00', repairUserNo: 'WANG04', repairDescription: 'Adjusted interlock alignment and verified cycle.' },
  { breakdownSerialNo: 'BD-2611-0095', equipmentNo: 'CNC-03', state: 2, breakdownStartTime: '2026-11-16 16:05:00', breakdownEndTime: '2026-11-16 16:16:00', breakdownDescription: 'Tool magazine positioning error', breakdownCreator: 'CHEN01', breakdownCreateDate: '2026-11-16 16:09:33', repairSerialNo: 'RP-2611-0057', repairStartTime: '2026-11-16 16:24:00', repairEndTime: '2026-11-16 17:12:00', repairUserNo: 'LIU03', repairDescription: 'Reset magazine home sensor and completed test run.' },
  { breakdownSerialNo: 'BD-2611-0091', equipmentNo: 'PUMP-08', state: 2, breakdownStartTime: '2026-11-13 10:18:00', breakdownEndTime: '2026-11-13 10:30:00', breakdownDescription: 'Pump vibration exceeded limit', breakdownCreator: 'LIN02', breakdownCreateDate: '2026-11-13 10:21:40', repairSerialNo: 'RP-2611-0052', repairStartTime: '2026-11-13 10:47:00', repairEndTime: '2026-11-13 12:06:00', repairUserNo: 'WANG04', repairDescription: 'Replaced worn coupling insert.' },
  { breakdownSerialNo: 'BD-2611-0091', equipmentNo: 'PUMP-08', state: 2, breakdownStartTime: '2026-11-13 10:18:00', breakdownEndTime: '2026-11-13 10:30:00', breakdownDescription: 'Pump vibration exceeded limit', breakdownCreator: 'LIN02', breakdownCreateDate: '2026-11-13 10:21:40', repairSerialNo: 'RP-2611-0053', repairStartTime: '2026-11-13 12:25:00', repairEndTime: '2026-11-13 12:54:00', repairUserNo: 'LIU03', repairDescription: 'Follow-up alignment check after restart.' },
  { breakdownSerialNo: 'BD-2611-0084', equipmentNo: 'ROBOT-01', state: 2, breakdownStartTime: '2026-11-09 09:02:00', breakdownEndTime: '2026-11-09 09:19:00', breakdownDescription: 'Axis 4 position deviation', breakdownCreator: 'WU05', breakdownCreateDate: '2026-11-09 09:07:11', repairSerialNo: 'RP-2611-0044', repairStartTime: '2026-11-09 09:31:00', repairEndTime: '2026-11-09 10:28:00', repairUserNo: 'WANG04', repairDescription: 'Re-homed axis and validated reference position.' },
  { breakdownSerialNo: 'BD-2611-0079', equipmentNo: 'CNC-02', state: 2, breakdownStartTime: '2026-11-05 11:40:00', breakdownEndTime: '2026-11-05 11:52:00', breakdownDescription: 'Coolant flow sensor intermittent', breakdownCreator: 'CHEN01', breakdownCreateDate: '2026-11-05 11:45:20', repairSerialNo: 'RP-2611-0038', repairStartTime: '2026-11-05 12:05:00', repairEndTime: '2026-11-05 13:20:00', repairUserNo: 'LIU03', repairDescription: 'Cleaned sensor port and replaced connector.' },
  { breakdownSerialNo: 'BD-2610-0067', equipmentNo: 'PRESS-04-CH1', state: 1, breakdownStartTime: '2026-10-28 13:10:00', breakdownEndTime: '', breakdownDescription: 'Chamber pressure switch alert', breakdownCreator: 'WU05', breakdownCreateDate: '2026-10-28 13:15:03', repairSerialNo: 'RP-2610-0031', repairStartTime: '2026-10-28 13:46:00', repairEndTime: '', repairUserNo: 'WANG04', repairDescription: '' },
  { breakdownSerialNo: 'BD-2610-0058', equipmentNo: 'CNC-03', state: 2, breakdownStartTime: '2026-10-17 08:20:00', breakdownEndTime: '2026-10-17 08:32:00', breakdownDescription: 'Hydraulic unit low level warning', breakdownCreator: 'LIN02', breakdownCreateDate: '2026-10-17 08:25:51', repairSerialNo: 'RP-2610-0023', repairStartTime: '2026-10-17 08:45:00', repairEndTime: '2026-10-17 09:56:00', repairUserNo: 'LIU03', repairDescription: 'Topped up hydraulic oil and inspected for leaks.' },
  { breakdownSerialNo: 'BD-2609-0034', equipmentNo: 'PUMP-07', state: 0, breakdownStartTime: '2026-09-12 15:28:00', breakdownEndTime: '', breakdownDescription: 'Motor overload trip', breakdownCreator: 'CHEN01', breakdownCreateDate: '2026-09-12 15:33:14', repairSerialNo: '', repairStartTime: '', repairEndTime: '', repairUserNo: '', repairDescription: '' },
]

function toLocalInput(value: Date) {
  const local = new Date(value.getTime() - value.getTimezoneOffset() * 60_000)
  return local.toISOString().slice(0, 16)
}

function getDefaultRange(): DateRange {
  const to = new Date()
  const from = new Date(to)
  from.setMonth(from.getMonth() - 3)
  return { enabled: false, from: toLocalInput(from), to: toLocalInput(to) }
}

const dates = reactive({ breakdown: getDefaultRange(), repairStart: getDefaultRange(), repairEnd: getDefaultRange() })
const equipmentFilterEnabled = ref(false)
const selectedEquipment = ref<string[]>([])
const selectedStates = ref<number[]>([])
const resultRows = ref<BreakdownRepairRow[]>([])
const queried = ref(false)
const notice = ref('設定條件後按 Query。')
const noticeIsError = ref(false)
const resultCount = computed(() => queried.value ? `符合 ${resultRows.value.length} 筆記錄` : '尚未查詢')

watch(equipmentFilterEnabled, (enabled) => { if (!enabled) selectedEquipment.value = [] })

function matchesRange(value: string, range: DateRange) {
  if (!range.enabled) return true
  if (!value) return false
  const dateTime = value.replace(' ', 'T').slice(0, 16)
  return dateTime >= range.from && dateTime <= range.to
}

function queryRepairs() {
  noticeIsError.value = false
  const activeRanges = [dates.breakdown, dates.repairStart, dates.repairEnd].filter((range) => range.enabled)
  if (activeRanges.some((range) => !range.from || !range.to || range.from >= range.to)) {
    notice.value = '已啟用的日期區間必須同時填寫起訖時間，且起始時間要早於結束時間。'
    noticeIsError.value = true
    queried.value = false
    resultRows.value = []
    return
  }

  resultRows.value = rows.filter((row) => {
    if (!equipmentFilterEnabled.value || selectedEquipment.value.length === 0) {
      // No equipment selection means no equipment restriction.
    } else if (!selectedEquipment.value.includes(row.equipmentNo)) return false
    if (selectedStates.value.length > 0 && !selectedStates.value.includes(row.state)) return false
    if (!matchesRange(row.breakdownStartTime, dates.breakdown)) return false
    if (!matchesRange(row.repairStartTime, dates.repairStart)) return false
    if (!matchesRange(row.repairEndTime, dates.repairEnd)) return false
    return true
  })
  queried.value = true
  notice.value = `查詢完成：${resultRows.value.length} 筆故障／維修記錄。`
}

function stateName(value: BreakdownRepairRow['state']) {
  return value === 0 ? 'UnConfirm' : value === 1 ? 'Waiting' : 'CLOSED'
}

function formatDateTime(value: string) { return value ? value.replaceAll('-', '/') : '—' }
</script>

<template>
  <section class="pm-breakdown-repair" aria-label="故障維修登錄查詢 DEV 預覽">
    <header class="page-header">
      <div><p>BREAKDOWN REPAIR QUERY / PM507</p><h1>故障維修登錄查詢</h1></div>
      <div class="header-actions"><span class="dev-tag">DEV 假資料</span><button type="button" class="secondary" @click="emit('close')">Close (&amp;X)</button></div>
    </header>

    <section class="criteria-panel" aria-label="查詢條件">
      <fieldset class="equipment-panel">
        <legend>Equipment</legend>
        <label class="equipment-toggle"><input v-model="equipmentFilterEnabled" type="checkbox">EquipmentNo</label>
        <select v-model="selectedEquipment" multiple size="5" :disabled="!equipmentFilterEnabled" aria-label="Equipment No multi-select">
          <option v-for="equipmentNo in equipmentOptions" :key="equipmentNo" :value="equipmentNo">{{ equipmentNo }}</option>
        </select>
        <span class="field-hint">可多選；取消勾選或未選設備代表不限制。</span>
      </fieldset>

      <fieldset class="state-panel">
        <legend>State</legend>
        <label><input v-model="selectedStates" type="checkbox" :value="0">UnConfirm</label>
        <label><input v-model="selectedStates" type="checkbox" :value="1">Waiting</label>
        <label><input v-model="selectedStates" type="checkbox" :value="2">CLOSED</label>
        <span class="field-hint">未勾選狀態時查詢全部狀態。</span>
      </fieldset>

      <div class="date-ranges">
        <fieldset class="date-range">
          <legend>BreakDownStartTime</legend>
          <label class="range-toggle"><input v-model="dates.breakdown.enabled" type="checkbox">套用日期區間</label>
          <label><span>From</span><input v-model="dates.breakdown.from" type="datetime-local" step="1" :disabled="!dates.breakdown.enabled"></label>
          <label><span>To</span><input v-model="dates.breakdown.to" type="datetime-local" step="1" :disabled="!dates.breakdown.enabled"></label>
        </fieldset>
        <fieldset class="date-range">
          <legend>RepairStartTime</legend>
          <label class="range-toggle"><input v-model="dates.repairStart.enabled" type="checkbox">套用日期區間</label>
          <label><span>From</span><input v-model="dates.repairStart.from" type="datetime-local" step="1" :disabled="!dates.repairStart.enabled"></label>
          <label><span>To</span><input v-model="dates.repairStart.to" type="datetime-local" step="1" :disabled="!dates.repairStart.enabled"></label>
        </fieldset>
        <fieldset class="date-range">
          <legend>RepairEndDateTime</legend>
          <label class="range-toggle"><input v-model="dates.repairEnd.enabled" type="checkbox">套用日期區間</label>
          <label><span>From</span><input v-model="dates.repairEnd.from" type="datetime-local" step="1" :disabled="!dates.repairEnd.enabled"></label>
          <label><span>To</span><input v-model="dates.repairEnd.to" type="datetime-local" step="1" :disabled="!dates.repairEnd.enabled"></label>
        </fieldset>
      </div>

      <div class="query-actions"><button type="button" class="primary" @click="queryRepairs">Query</button></div>
    </section>

    <section class="results-panel" aria-label="故障維修查詢結果">
      <div class="result-heading"><h2>Breakdown / Repair Records</h2><span>{{ resultCount }}</span></div>
      <p class="notice" :class="{ error: noticeIsError }" role="status">{{ notice }}</p>
      <div v-if="!queried" class="empty-state">按 Query 後顯示設備故障與維修記錄。</div>
      <div v-else-if="resultRows.length === 0" class="empty-state">沒有符合條件的記錄。</div>
      <div v-else class="table-scroll">
        <table>
          <thead><tr>
            <th>BreakDownSerialNo</th><th>EquipmentNo</th><th>State</th><th>BreakDownStartTime</th><th>BreakDownEndTime</th>
            <th>BreakDownDescription</th><th>BreakDownCreator</th><th>BreakDownCreateDate</th><th>RepairSerialNo</th>
            <th>RepairStartTime</th><th>RepairEndTime</th><th>RepairUserNo</th><th>RepairDescription</th>
          </tr></thead>
          <tbody>
            <tr v-for="(row, index) in resultRows" :key="`${row.breakdownSerialNo}-${row.repairSerialNo || 'open'}-${index}`">
              <td>{{ row.breakdownSerialNo }}</td><td>{{ row.equipmentNo }}</td><td>{{ stateName(row.state) }}</td>
              <td>{{ formatDateTime(row.breakdownStartTime) }}</td><td>{{ formatDateTime(row.breakdownEndTime) }}</td>
              <td class="description">{{ row.breakdownDescription }}</td><td>{{ row.breakdownCreator }}</td><td>{{ formatDateTime(row.breakdownCreateDate) }}</td>
              <td>{{ row.repairSerialNo || '—' }}</td><td>{{ formatDateTime(row.repairStartTime) }}</td><td>{{ formatDateTime(row.repairEndTime) }}</td>
              <td>{{ row.repairUserNo || '—' }}</td><td class="description">{{ row.repairDescription || '—' }}</td>
            </tr>
          </tbody>
        </table>
      </div>
      <p class="formula-note">CLOSED 資料依 RepairLog 關聯 BreakDownLog；同一故障若有多筆維修記錄，結果可能多列。DEV 範例資料為本機假資料。</p>
    </section>
  </section>
</template>

<style scoped>
.pm-breakdown-repair { display: flex; flex-direction: column; gap: 10px; min-height: 0; padding: 10px 12px 14px; box-sizing: border-box; overflow: auto; background: #f7f5ee; color: #26251f; font: inherit; }
.page-header { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.page-header p { margin: 0 0 2px; color: #777366; font-size: 11px; letter-spacing: .08em; }
.page-header h1 { margin: 0; font-size: 21px; font-weight: 650; }
.header-actions { display: flex; align-items: center; gap: 8px; }
.dev-tag { padding: 3px 7px; border: 1px solid #ba9850; border-radius: 3px; color: #71551b; background: #f8edce; font-size: 11px; }
button, input, select { font: inherit; }
button { cursor: pointer; }
.secondary, .primary { min-height: 28px; padding: 4px 11px; border: 1px solid #3b3930; border-radius: 3px; background: #fff; color: #3b3930; }
.primary { min-width: 88px; min-height: 34px; background: #3b3930; color: #fff; }
.criteria-panel { display: grid; grid-template-columns: minmax(195px, .9fr) minmax(160px, .7fr) minmax(580px, 2.8fr) auto; gap: 8px; align-items: stretch; }
fieldset { min-width: 0; margin: 0; padding: 6px 9px 8px; border: 1px solid #c9c5b8; border-radius: 3px; }
legend { padding: 0 5px; color: #514d42; font-size: 12px; font-weight: 650; }
.equipment-panel { display: flex; flex-direction: column; gap: 5px; }
.equipment-toggle, .state-panel label, .range-toggle { display: inline-flex; align-items: center; gap: 5px; font-size: 12px; }
.equipment-panel select { min-width: 0; width: 100%; height: 105px; padding: 3px; border: 1px solid #c9c5b8; border-radius: 2px; background: #fff; color: #26251f; }
.equipment-panel select:disabled { background: #eeeae0; color: #938f83; }
.field-hint { color: #777366; font-size: 10px; line-height: 1.35; }
.state-panel { display: flex; flex-direction: column; align-items: flex-start; gap: 8px; }
.state-panel .field-hint { margin-top: auto; }
.date-ranges { display: grid; grid-template-columns: repeat(3, minmax(190px, 1fr)); gap: 7px; }
.date-range { display: flex; flex-direction: column; gap: 5px; padding: 6px 8px 8px; }
.date-range > label:not(.range-toggle) { display: grid; grid-template-columns: 34px minmax(0, 1fr); gap: 5px; align-items: center; color: #686457; font-size: 11px; }
.date-range input[type="datetime-local"] { min-width: 0; width: 100%; height: 26px; padding: 2px 4px; border: 1px solid #c9c5b8; border-radius: 2px; background: #fff; color: #26251f; font-size: 11px; }
.date-range input:disabled { color: #938f83; background: #eeeae0; }
.range-toggle { color: #777366; }
.query-actions { display: flex; align-items: flex-end; justify-content: flex-end; }
.results-panel { display: flex; min-height: 175px; flex-direction: column; padding: 8px 9px 7px; border: 1px solid #c9c5b8; border-radius: 3px; background: #fbfaf6; }
.result-heading { display: flex; align-items: baseline; justify-content: space-between; gap: 8px; }
.result-heading h2 { margin: 0; font-size: 15px; }
.result-heading span, .notice, .formula-note { color: #777366; font-size: 11px; }
.notice { margin: 4px 0 7px; }
.notice.error { color: #a1352d; }
.empty-state { display: grid; min-height: 96px; place-items: center; color: #777366; font-size: 13px; }
.table-scroll { overflow: auto; border: 1px solid #d4d0c5; background: white; }
table { width: 100%; min-width: 1670px; border-collapse: collapse; font-size: 12px; }
th, td { padding: 6px 7px; border-right: 1px solid #e1ded5; border-bottom: 1px solid #e1ded5; text-align: left; white-space: nowrap; }
th { position: sticky; top: 0; z-index: 1; background: #efede5; color: #403d34; font-weight: 650; }
tbody tr:nth-child(even) { background: #faf9f5; }
td.description { min-width: 185px; max-width: 275px; white-space: normal; }
.formula-note { margin: 7px 0 0; }
@media (max-width: 1280px) { .criteria-panel { grid-template-columns: minmax(180px, 1fr) minmax(150px, .8fr) auto; } .date-ranges { grid-column: 1 / -1; grid-row: 2; } .query-actions { grid-column: 3; grid-row: 1; } }
@media (max-width: 760px) { .criteria-panel { grid-template-columns: 1fr 1fr; } .date-ranges { grid-template-columns: 1fr; grid-column: 1 / -1; grid-row: auto; } .query-actions { grid-column: 1 / -1; grid-row: auto; justify-content: flex-start; } }
@media (max-width: 520px) { .criteria-panel { grid-template-columns: 1fr; } .page-header { align-items: flex-start; } }
</style>
