<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)

interface ReasonRow { reasonNo: string; reasonName: string }
interface LogRow {
  serialNo: string
  accessoryType: string
  accessoryNo: string
  accessoryVersion: string
  stateName: string
  startTime: number
  endTime: number
  userNo: string
  description: string
  applyTime: number | null
  accumulateQty: number
  timeCycleStart: number | null
  reasons: ReasonRow[]
}
interface SearchCriteria {
  useAccessoryType: boolean
  accessoryType: string
  useAccessoryNo: boolean
  accessoryNo: string
  useUserNo: boolean
  userNo: string
  useStartTime: boolean
  startFrom: string
  startTo: string
}
type PickerKind = 'accessoryType' | 'accessoryNo' | 'userNo' | null

function at(daysAgo: number, hour: number, minute = 0): number {
  const date = new Date()
  date.setDate(date.getDate() - daysAgo)
  date.setHours(hour, minute, 0, 0)
  return date.getTime()
}

function toLocalInput(value: Date): string {
  const pad = (number: number) => String(number).padStart(2, '0')
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}:${pad(value.getSeconds())}`
}

function displayTime(value: number | null): string {
  return value === null ? '—' : new Intl.DateTimeFormat('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false }).format(value)
}

const today = new Date()
const dayStart = new Date(today)
dayStart.setHours(0, 0, 0, 0)
const dayEnd = new Date(today)
dayEnd.setHours(23, 59, 59, 0)

// 假歷程、假使用者及假原因只在本元件記憶體中，用來呈現查詢與主從明細。
const logRows: LogRow[] = [
  { serialNo: 'DEV-ACC-LOG-001', accessoryType: 'PROBE', accessoryNo: 'PC-PRB-01-001', accessoryVersion: 'A01', stateName: '使用中', startTime: at(0, 8, 10), endTime: at(0, 9, 42), userNo: '11019', description: '設備上機測試', applyTime: at(0, 8, 8), accumulateQty: 128.5, timeCycleStart: at(0, 8, 10), reasons: [{ reasonNo: 'R-001', reasonName: '生產使用' }] },
  { serialNo: 'DEV-ACC-LOG-002', accessoryType: 'PROBE', accessoryNo: 'PC-PRB-01-001', accessoryVersion: 'A01', stateName: '維修中', startTime: at(0, 9, 42), endTime: at(0, 10, 5), userNo: '11019', description: '針痕清潔後檢查', applyTime: null, accumulateQty: 128.5, timeCycleStart: at(0, 8, 10), reasons: [{ reasonNo: 'R-014', reasonName: '例行清潔' }, { reasonNo: 'R-022', reasonName: '接觸異常檢查' }] },
  { serialNo: 'DEV-ACC-LOG-003', accessoryType: 'PROBE', accessoryNo: 'PC-PRB-02-004', accessoryVersion: 'A02', stateName: '庫存', startTime: at(0, 7, 35), endTime: at(0, 8, 0), userNo: '10428', description: '歸還配件庫', applyTime: at(0, 7, 35), accumulateQty: 32, timeCycleStart: null, reasons: [] },
  { serialNo: 'DEV-ACC-LOG-004', accessoryType: 'MASK', accessoryNo: 'RET-ETCH-01-003', accessoryVersion: 'B01', stateName: '使用中', startTime: at(0, 6, 55), endTime: at(0, 11, 20), userNo: '10806', description: '蝕刻批次使用', applyTime: at(0, 6, 52), accumulateQty: 84.25, timeCycleStart: at(0, 6, 55), reasons: [{ reasonNo: 'R-031', reasonName: '製程領用' }] },
  { serialNo: 'DEV-ACC-LOG-005', accessoryType: 'MASK', accessoryNo: 'RET-ETCH-01-003', accessoryVersion: 'B01', stateName: '維修中', startTime: at(0, 11, 20), endTime: at(0, 11, 48), userNo: '10806', description: '使用後檢查', applyTime: null, accumulateQty: 84.25, timeCycleStart: at(0, 6, 55), reasons: [{ reasonNo: 'R-014', reasonName: '例行檢查' }] },
  { serialNo: 'DEV-ACC-LOG-006', accessoryType: 'MOLD', accessoryNo: 'JIG-MOL-01-009', accessoryVersion: 'A03', stateName: '待驗收', startTime: at(1, 16, 20), endTime: at(1, 17, 5), userNo: '10211', description: '保養後待驗收', applyTime: null, accumulateQty: 18, timeCycleStart: null, reasons: [{ reasonNo: 'R-042', reasonName: '保養完成' }] },
  { serialNo: 'DEV-ACC-LOG-007', accessoryType: 'PROBE', accessoryNo: 'PC-PRB-03-002', accessoryVersion: 'A01', stateName: '使用中', startTime: at(1, 13, 5), endTime: at(1, 15, 30), userNo: '11019', description: '批次抽測', applyTime: at(1, 13, 0), accumulateQty: 246, timeCycleStart: at(1, 13, 5), reasons: [{ reasonNo: 'R-001', reasonName: '生產使用' }] },
  { serialNo: 'DEV-ACC-LOG-008', accessoryType: 'SEAL', accessoryNo: 'SL-OVN-02-005', accessoryVersion: 'C02', stateName: '報廢', startTime: at(2, 14, 45), endTime: at(2, 15, 10), userNo: '10428', description: '壽命到期', applyTime: null, accumulateQty: 500, timeCycleStart: at(0, 14, 45), reasons: [{ reasonNo: 'R-058', reasonName: '壽命到期' }, { reasonNo: 'R-060', reasonName: '工程確認報廢' }] },
  { serialNo: 'DEV-ACC-LOG-009', accessoryType: 'MOLD', accessoryNo: 'JIG-MOL-02-001', accessoryVersion: 'A01', stateName: '使用中', startTime: at(2, 9, 30), endTime: at(2, 12, 15), userNo: '10806', description: '成型機生產使用', applyTime: at(2, 9, 28), accumulateQty: 72, timeCycleStart: at(2, 9, 30), reasons: [{ reasonNo: 'R-001', reasonName: '生產使用' }] },
  { serialNo: 'DEV-ACC-LOG-010', accessoryType: 'MASK', accessoryNo: 'RET-ETCH-02-001', accessoryVersion: 'A02', stateName: '庫存', startTime: at(3, 17, 15), endTime: at(3, 18, 0), userNo: '10211', description: '完成清潔後歸還', applyTime: at(3, 17, 15), accumulateQty: 195.75, timeCycleStart: null, reasons: [{ reasonNo: 'R-014', reasonName: '清潔完成' }] },
  { serialNo: 'DEV-ACC-LOG-011', accessoryType: 'PROBE', accessoryNo: 'PC-PRB-02-004', accessoryVersion: 'A02', stateName: '維修中', startTime: at(4, 10, 5), endTime: at(4, 10, 50), userNo: '10428', description: '探針卡接觸檢查', applyTime: null, accumulateQty: 31.5, timeCycleStart: null, reasons: [{ reasonNo: 'R-022', reasonName: '接觸異常檢查' }] },
  { serialNo: 'DEV-ACC-LOG-012', accessoryType: 'SEAL', accessoryNo: 'SL-OVN-02-005', accessoryVersion: 'C02', stateName: '使用中', startTime: at(6, 7, 40), endTime: at(6, 11, 25), userNo: '11019', description: '烘烤製程使用', applyTime: at(6, 7, 38), accumulateQty: 493, timeCycleStart: at(6, 7, 40), reasons: [{ reasonNo: 'R-047', reasonName: '製程備品領用' }] },
  { serialNo: 'DEV-ACC-LOG-013', accessoryType: 'MOLD', accessoryNo: 'JIG-MOL-01-009', accessoryVersion: 'A03', stateName: '庫存', startTime: at(8, 15, 0), endTime: at(8, 15, 30), userNo: '10211', description: '入庫點收', applyTime: null, accumulateQty: 18, timeCycleStart: null, reasons: [] },
  { serialNo: 'DEV-ACC-LOG-014', accessoryType: 'MASK', accessoryNo: 'RET-ETCH-01-003', accessoryVersion: 'B01', stateName: '使用中', startTime: at(9, 8, 35), endTime: at(9, 13, 25), userNo: '10806', description: '蝕刻製程使用', applyTime: at(9, 8, 32), accumulateQty: 76, timeCycleStart: at(9, 8, 35), reasons: [{ reasonNo: 'R-031', reasonName: '製程領用' }] },
]

const typeOptions = [...new Set(logRows.map((row) => row.accessoryType))].sort().map((code) => ({ code, name: `${code} 配件` }))
const criteria = reactive<SearchCriteria>({
  useAccessoryType: false,
  accessoryType: '',
  useAccessoryNo: false,
  accessoryNo: '',
  useUserNo: false,
  userNo: '',
  useStartTime: false,
  startFrom: toLocalInput(dayStart),
  startTo: toLocalInput(dayEnd),
})
const accessoryOptions = computed(() => [...new Set(logRows
  .filter((row) => !criteria.accessoryType || row.accessoryType === criteria.accessoryType)
  .map((row) => row.accessoryNo))].sort().map((code) => ({ code, name: code })))
const userOptions = [...new Set(logRows.map((row) => row.userNo))].sort().map((code) => ({ code, name: `${code}（DEV 使用者）` }))
const submittedCriteria = ref<SearchCriteria | null>(null)
const selectedSerialNo = ref('')
const pickerKind = ref<PickerKind>(null)
const message = ref('選擇條件後按 Query。資料皆為 DEV 假歷程。')

const pickerOptions = computed(() => {
  if (pickerKind.value === 'accessoryType') return typeOptions
  if (pickerKind.value === 'accessoryNo') return accessoryOptions.value
  if (pickerKind.value === 'userNo') return userOptions
  return []
})
const pickerTitle = computed(() => ({ accessoryType: '選擇配件類型', accessoryNo: '選擇配件編號', userNo: '選擇使用者' }[pickerKind.value ?? 'accessoryType']))
const visibleLogs = computed(() => {
  const search = submittedCriteria.value
  if (!search) return []
  const from = search.useStartTime && search.startFrom ? new Date(search.startFrom).getTime() : null
  const to = search.useStartTime && search.startTo ? new Date(search.startTo).getTime() : null
  return logRows
    .filter((row) => (!search.useAccessoryType || !search.accessoryType || row.accessoryType === search.accessoryType)
      && (!search.useAccessoryNo || !search.accessoryNo || row.accessoryNo === search.accessoryNo)
      && (!search.useUserNo || !search.userNo || row.userNo === search.userNo)
      && (from === null || row.startTime >= from)
      && (to === null || row.startTime <= to))
    .sort((left, right) => left.accessoryType.localeCompare(right.accessoryType)
      || left.accessoryNo.localeCompare(right.accessoryNo)
      || left.startTime - right.startTime)
})
const selectedLog = computed(() => visibleLogs.value.find((row) => row.serialNo === selectedSerialNo.value) ?? null)
const selectedReasons = computed(() => selectedLog.value?.reasons ?? [])

function toggleFilter(key: 'AccessoryType' | 'AccessoryNo' | 'UserNo') {
  if (key === 'AccessoryType' && !criteria.useAccessoryType) criteria.accessoryType = ''
  if (key === 'AccessoryNo' && !criteria.useAccessoryNo) criteria.accessoryNo = ''
  if (key === 'UserNo' && !criteria.useUserNo) criteria.userNo = ''
}

function openPicker(kind: Exclude<PickerKind, null>) {
  pickerKind.value = kind
}

function chooseOption(value: string) {
  if (pickerKind.value === 'accessoryType') {
    criteria.accessoryType = value
    criteria.useAccessoryType = true
  } else if (pickerKind.value === 'accessoryNo') {
    criteria.accessoryNo = value
    criteria.useAccessoryNo = true
  } else if (pickerKind.value === 'userNo') {
    criteria.userNo = value
    criteria.useUserNo = true
  }
  pickerKind.value = null
}

function runQuery() {
  submittedCriteria.value = { ...criteria }
  const rows = visibleLogs.value
  selectedSerialNo.value = rows[0]?.serialNo ?? ''
  message.value = rows.length ? `查詢完成，共 ${rows.length} 筆配件狀態歷程。` : '沒有符合條件的假資料。'
}

function formatQuantity(value: number): string {
  return new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 4 }).format(value)
}
</script>

<template>
  <section v-if="dev" class="accessory-log-page" aria-label="配件歷史資料查詢 DEV">
    <header class="page-heading">
      <div class="title-line"><h1>配件歷史資料查詢</h1><span class="dev-badge">DEV 假資料</span></div>
      <p>依配件、操作人員或歷程起始時間查詢；選取主表列可檢視對應原因。</p>
    </header>

    <section class="filters" aria-label="查詢條件">
      <div class="filter-field">
        <label class="filter-check"><input v-model="criteria.useAccessoryType" type="checkbox" @change="toggleFilter('AccessoryType')" />Accessory Type</label>
        <div class="picker-input"><input v-model="criteria.accessoryType" type="text" maxlength="50" aria-label="Accessory Type" @blur="criteria.accessoryType = criteria.accessoryType.toUpperCase()" /><button type="button" aria-label="選擇配件類型" @click="openPicker('accessoryType')">…</button></div>
      </div>
      <div class="filter-field">
        <label class="filter-check"><input v-model="criteria.useAccessoryNo" type="checkbox" @change="toggleFilter('AccessoryNo')" />Accessory No</label>
        <div class="picker-input"><input v-model="criteria.accessoryNo" type="text" maxlength="50" aria-label="Accessory No" @blur="criteria.accessoryNo = criteria.accessoryNo.toUpperCase()" /><button type="button" aria-label="選擇配件編號" @click="openPicker('accessoryNo')">…</button></div>
      </div>
      <div class="filter-field">
        <label class="filter-check"><input v-model="criteria.useUserNo" type="checkbox" @change="toggleFilter('UserNo')" />User No</label>
        <div class="picker-input"><input v-model="criteria.userNo" type="text" maxlength="10" aria-label="User No" @blur="criteria.userNo = criteria.userNo.toUpperCase()" /><button type="button" aria-label="選擇使用者" @click="openPicker('userNo')">…</button></div>
      </div>
      <div class="filter-field time-filter">
        <label class="filter-check"><input v-model="criteria.useStartTime" type="checkbox" />Start Time</label>
        <div class="date-range"><label>From <input v-model="criteria.startFrom" type="datetime-local" step="1" /></label><label>To <input v-model="criteria.startTo" type="datetime-local" step="1" /></label></div>
      </div>
      <button type="button" class="query-button" @click="runQuery">Query</button>
    </section>

    <section class="results-section" aria-label="配件狀態歷程">
      <div class="section-heading"><h2>Accessory State Log</h2><span>{{ visibleLogs.length }} 筆</span></div>
      <div class="table-wrap">
        <table class="history-table">
          <thead><tr><th>Accessory Type</th><th>Accessory No</th><th>State Name</th><th>Start Time</th><th>End Time</th><th>User No</th><th>Description</th><th>Apply Time</th><th>Accumulate Qty</th><th>Time Cycle Start</th></tr></thead>
          <tbody>
            <tr v-for="row in visibleLogs" :key="row.serialNo" :class="{ selected: row.serialNo === selectedSerialNo }" tabindex="0" @click="selectedSerialNo = row.serialNo" @keydown.enter="selectedSerialNo = row.serialNo">
              <td>{{ row.accessoryType }}</td><td>{{ row.accessoryNo }}</td><td><span class="state-label">{{ row.stateName }}</span></td><td>{{ displayTime(row.startTime) }}</td><td>{{ displayTime(row.endTime) }}</td><td>{{ row.userNo }}</td><td class="description-cell">{{ row.description }}</td><td>{{ displayTime(row.applyTime) }}</td><td class="number-cell">{{ formatQuantity(row.accumulateQty) }}</td><td>{{ displayTime(row.timeCycleStart) }}</td>
            </tr>
            <tr v-if="submittedCriteria && visibleLogs.length === 0"><td class="empty-cell" colspan="10">沒有符合條件的假資料</td></tr>
            <tr v-if="!submittedCriteria"><td class="empty-cell" colspan="10">請設定條件並按 Query 載入 DEV 假歷程。</td></tr>
          </tbody>
        </table>
      </div>
    </section>

    <section class="reasons-section" aria-label="配件狀態原因">
      <div class="section-heading"><h2>Accessory Reasons</h2><span>{{ selectedLog ? `${selectedLog.accessoryNo} · ${selectedLog.stateName}` : '請先選取歷程' }}</span></div>
      <div class="reasons-table-wrap">
        <table class="reasons-table"><thead><tr><th>Reason No</th><th>Reason Name</th></tr></thead>
          <tbody>
            <tr v-for="reason in selectedReasons" :key="`${reason.reasonNo}-${reason.reasonName}`"><td>{{ reason.reasonNo }}</td><td>{{ reason.reasonName }}</td></tr>
            <tr v-if="selectedLog && selectedReasons.length === 0"><td colspan="2" class="empty-cell">此歷程沒有附加原因。</td></tr>
            <tr v-if="!selectedLog"><td colspan="2" class="empty-cell">查詢後選取一筆配件歷程以載入原因明細。</td></tr>
          </tbody>
        </table>
      </div>
    </section>

    <footer class="page-footer"><p role="status">{{ message }}</p><button type="button" class="close-button" @click="emit('close')">Close (X)</button></footer>

    <div v-if="pickerKind" class="modal-backdrop" @click.self="pickerKind = null">
      <section class="picker-dialog" role="dialog" aria-modal="true" :aria-label="pickerTitle">
        <header><div><h2>{{ pickerTitle }}</h2><p>來源操作為 WinForms 共用查詢選擇器；此處使用虛構清單。</p></div><button class="close-picker" type="button" aria-label="關閉" @click="pickerKind = null">×</button></header>
        <div class="picker-options"><button v-for="option in pickerOptions" :key="option.code" type="button" @click="chooseOption(option.code)"><strong>{{ option.code }}</strong><span>{{ option.name }}</span></button></div>
      </section>
    </div>
  </section>
  <section v-else class="not-available" role="status">DEV 預覽只在開發環境提供。</section>
</template>

<style scoped>
.accessory-log-page { display: flex; flex-direction: column; gap: 9px; height: 100%; min-height: 0; padding: 12px 14px; box-sizing: border-box; overflow: auto; background: #f7f5ee; color: #26251f; font: inherit; }
.page-heading { flex: none; }
.title-line { display: flex; align-items: center; gap: 9px; }
h1 { margin: 0; font-size: 19px; }
.page-heading p { margin: 4px 0 0; color: #777366; font-size: 12px; }
.dev-badge { padding: 3px 8px; border-radius: 12px; background: #fff0bd; color: #6d5713; font-size: 10px; font-weight: 700; }
.filters { display: grid; grid-template-columns: minmax(180px, 1fr) minmax(190px, 1fr) minmax(160px, .9fr) minmax(390px, 2fr) auto; align-items: end; gap: 9px; padding: 9px; border: 1px solid #d8d3c5; border-radius: 5px; background: #fffdf7; }
.filter-field { display: grid; gap: 4px; min-width: 0; }
.filter-check { display: flex; align-items: center; gap: 6px; color: #555247; font-size: 11px; font-weight: 600; }
.picker-input { display: flex; min-width: 0; }
.picker-input input, .date-range input { height: 26px; min-width: 0; padding: 3px 6px; border: 1px solid #c8c2b4; border-radius: 3px; background: white; color: #26251f; font: inherit; font-size: 11px; }
.picker-input input { flex: 1; width: 100%; }
.picker-input button { width: 29px; margin-left: 4px; border: 1px solid #b9b4a6; border-radius: 3px; background: #f8f6ef; color: #37352f; }
.date-range { display: flex; align-items: center; gap: 6px; }
.date-range label { display: flex; align-items: center; gap: 5px; color: #686457; font-size: 10px; white-space: nowrap; }
.date-range input { width: 160px; }
.query-button, .close-button { min-height: 29px; padding: 4px 12px; border: 1px solid #3b3930; border-radius: 4px; background: #3b3930; color: white; font: inherit; font-size: 12px; cursor: pointer; }
.results-section, .reasons-section { display: flex; flex-direction: column; min-height: 0; border: 1px solid #d8d3c5; border-radius: 5px; background: #fffdf7; }
.results-section { flex: 1 1 auto; }
.reasons-section { flex: 0 0 145px; }
.section-heading { display: flex; flex: none; align-items: center; justify-content: space-between; gap: 12px; padding: 6px 9px; border-bottom: 1px solid #e7e3d8; background: #ece9df; }
.section-heading h2 { margin: 0; color: #555247; font-size: 11px; font-weight: 700; }
.section-heading span { color: #777366; font-size: 10px; }
.table-wrap, .reasons-table-wrap { min-height: 0; overflow: auto; }
.history-table, .reasons-table { width: 100%; border-collapse: collapse; font-size: 11px; white-space: nowrap; }
thead { position: sticky; top: 0; z-index: 1; background: #f5f3eb; color: #625f55; text-align: left; }
th, td { padding: 6px 8px; border-bottom: 1px solid #eeeae0; }
th { font-size: 10px; font-weight: 700; }
.history-table tbody tr { cursor: pointer; }
.history-table tbody tr:hover { background: #f8f6ef; }
.history-table tbody tr.selected { background: #fff2c6; }
.history-table tbody tr:focus-visible { outline: 2px solid #d8b622; outline-offset: -2px; }
.state-label { display: inline-block; padding: 2px 6px; border-radius: 9px; background: #e9efe9; color: #3d684c; }
.description-cell { max-width: 180px; overflow: hidden; text-overflow: ellipsis; }
.number-cell { text-align: right; font-variant-numeric: tabular-nums; }
.empty-cell { padding: 14px 8px; color: #8a867a; text-align: center; }
.reasons-table { max-width: 540px; }
.page-footer { display: flex; flex: none; align-items: center; justify-content: space-between; min-height: 29px; gap: 10px; }
.page-footer p { margin: 0; color: #686457; font-size: 11px; }
.close-button { min-width: 78px; min-height: 26px; }
button:focus-visible, input:focus-visible { outline: 2px solid #d8b622; outline-offset: 2px; }
.modal-backdrop { position: fixed; inset: 0; z-index: 20; display: grid; place-items: center; padding: 18px; background: #18171277; }
.picker-dialog { width: min(480px, 100%); max-height: min(520px, 90vh); display: flex; flex-direction: column; padding: 14px; border: 1px solid #c9c3b4; border-radius: 7px; background: #fffdf7; box-shadow: 0 16px 48px #0003; }
.picker-dialog header { display: flex; justify-content: space-between; gap: 14px; margin-bottom: 10px; }
.picker-dialog h2 { margin: 0; font-size: 16px; }
.picker-dialog p { margin: 4px 0 0; color: #777366; font-size: 11px; }
.close-picker { width: 28px; height: 28px; border: 0; border-radius: 4px; background: transparent; color: #625f55; font-size: 21px; cursor: pointer; }
.picker-options { display: grid; gap: 5px; overflow: auto; }
.picker-options button { display: flex; align-items: center; gap: 12px; min-height: 35px; padding: 5px 8px; border: 1px solid #e3dfd4; border-radius: 4px; background: white; color: #36342e; text-align: left; font: inherit; cursor: pointer; }
.picker-options button:hover { border-color: #88742e; background: #fff9df; }
.picker-options strong { min-width: 130px; font-size: 11px; }
.not-available { padding: 12px; background: #f7f5ee; color: #777366; }
@media (max-width: 900px) { .filters { grid-template-columns: repeat(2, minmax(0, 1fr)); } .time-filter { grid-column: 1 / -1; } .query-button { justify-self: end; } .results-section { min-height: 260px; } .reasons-section { min-height: 130px; } }
@media (max-width: 540px) { .filters { grid-template-columns: minmax(0, 1fr); } .time-filter { grid-column: auto; } .date-range { align-items: flex-start; flex-direction: column; } .date-range label { width: 100%; justify-content: space-between; } .date-range input { width: min(230px, 65vw); } .query-button { justify-self: stretch; } }
</style>
