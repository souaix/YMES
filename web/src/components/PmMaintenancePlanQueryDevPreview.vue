<script setup lang="ts">
import { computed, ref, watch } from 'vue'

interface PlanRow {
  serialNo: string
  pmeqpSopNo: string
  pmSerialNo: number
  equipmentNo: string
  frequencyNo: string
  frequencyName: string
  sopNo: string
  sopName: string
  estimatedDate: string
  executeState: number
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
}
interface PlanState { version: number; plans?: Array<Record<string, unknown>>; records?: Array<Record<string, unknown>> }
type FilterKey = 'equipment' | 'frequency' | 'creator'
type Direction = 'right' | 'left'
interface FilterGroup { key: FilterKey; label: string }

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.pm.eqp-type-sop-basis.v1'
const fallbackRows: PlanRow[] = [
  { serialNo: 'DEV-PLAN-0001', pmeqpSopNo: 'EQPSOP2600001', pmSerialNo: 1, equipmentNo: 'CNC-02', frequencyNo: 'PM-MONTHLY-15', frequencyName: '月中設備校驗', sopNo: 'SOP-CAL-02', sopName: '感測器校正程序', estimatedDate: '2026-11-15', executeState: 1, creator: 'PMUSER', createDate: '2026-01-08 09:30', reviser: 'PMUSER', reviseDate: '2026-03-04 15:20' },
  { serialNo: 'DEV-PLAN-0002', pmeqpSopNo: 'EQPSOP2600001', pmSerialNo: 2, equipmentNo: 'CNC-02', frequencyNo: 'PM-MONTHLY-15', frequencyName: '月中設備校驗', sopNo: 'SOP-CAL-02', sopName: '感測器校正程序', estimatedDate: '2026-12-15', executeState: 0, creator: 'PMUSER', createDate: '2026-01-08 09:30', reviser: 'PMUSER', reviseDate: '2026-03-04 15:20' },
  { serialNo: 'DEV-PLAN-0003', pmeqpSopNo: 'EQPSOP2600002', pmSerialNo: 1, equipmentNo: 'PUMP-07', frequencyNo: 'PM-WEEKLY-01', frequencyName: '每週保養', sopNo: 'SOP-LUBE-03', sopName: '主軸潤滑保養程序', estimatedDate: '2026-11-30', executeState: 0, creator: 'DEV02', createDate: '2026-02-10 11:15', reviser: '', reviseDate: '' },
  { serialNo: 'DEV-PLAN-0004', pmeqpSopNo: 'EQPSOP2600002', pmSerialNo: 2, equipmentNo: 'CNC-02-CH1', frequencyNo: 'PM-WEEKLY-01', frequencyName: '每週保養', sopNo: 'SOP-LUBE-03', sopName: '主軸潤滑保養程序', estimatedDate: '2026-11-22', executeState: 0, creator: 'DEV02', createDate: '2026-02-10 11:15', reviser: '', reviseDate: '' },
]
const groups: FilterGroup[] = [
  { key: 'equipment', label: 'Equipment No' },
  { key: 'frequency', label: 'Frequency No' },
  { key: 'creator', label: 'Creator' },
]
const poolSelection = ref<Record<FilterKey, string[]>>({ equipment: [], frequency: [], creator: [] })
const selectedLists = ref<Record<FilterKey, string[]>>({ equipment: [], frequency: [], creator: [] })
const fromEnabled = ref(false)
const toEnabled = ref(false)
const fromDate = ref('2026-11-01')
const toDate = ref('2026-12-31')
const queryRows = ref<PlanRow[]>([])
const queried = ref(false)
const notice = ref('')
const calendarOpen = ref(false)
const calendarMonth = ref('2026-11')

function readSavedRows(): PlanRow[] {
  try {
    const parsed = JSON.parse(window.localStorage.getItem(storageKey) ?? 'null') as PlanState | null
    if (!Array.isArray(parsed?.plans)) return fallbackRows
    const records = Array.isArray(parsed.records) ? parsed.records : []
    const recordByNo = new Map(records.map((row) => [String(row.pmeqpSopNo ?? ''), row]))
    return parsed.plans.map((raw) => {
      const parent = recordByNo.get(String(raw.pmeqpSopNo ?? '')) ?? {}
      const estimatedDate = String(raw.estimatedDate ?? raw.pmEstDateTime ?? '').slice(0, 10)
      return {
        serialNo: String(raw.serialNo ?? ''), pmeqpSopNo: String(raw.pmeqpSopNo ?? ''), pmSerialNo: Number(raw.pmSerialNo ?? 0),
        equipmentNo: String(raw.equipmentNo ?? ''), frequencyNo: String(raw.frequencyNo ?? ''), frequencyName: String(raw.frequencyName ?? parent.frequencyName ?? ''),
        sopNo: String(raw.sopNo ?? parent.sopNo ?? ''), sopName: String(raw.sopName ?? parent.sopName ?? ''), estimatedDate,
        executeState: Number(raw.executeState ?? 0), creator: String(raw.creator ?? ''), createDate: String(raw.createDate ?? ''),
        reviser: String(raw.reviser ?? ''), reviseDate: String(raw.reviseDate ?? ''),
      }
    }).filter((row) => row.serialNo && row.equipmentNo && row.frequencyNo && row.estimatedDate)
  } catch {
    return fallbackRows
  }
}

const rows = ref(readSavedRows())
const frequencyNames = computed(() => {
  const result: Record<string, string> = {}
  for (const row of rows.value) if (row.frequencyNo && row.frequencyName) result[row.frequencyNo] = row.frequencyName
  try {
    const saved = JSON.parse(window.localStorage.getItem('ymes.dev.pm.frequency-basis.v1') ?? 'null') as { records?: Array<Record<string, unknown>> } | null
    for (const row of saved?.records ?? []) {
      const no = String(row.frequencyNo ?? '')
      if (no && row.frequencyName) result[no] = String(row.frequencyName)
    }
  } catch { /* Keep plan labels when the DEV frequency list is unavailable. */ }
  return result
})
const sopNames = computed(() => {
  const result: Record<string, string> = {}
  for (const row of rows.value) if (row.sopNo && row.sopName) result[row.sopNo] = row.sopName
  try {
    const saved = JSON.parse(window.localStorage.getItem('ymes.dev.pm.sop-basis.v1') ?? 'null') as { records?: Array<Record<string, unknown>> } | null
    for (const row of saved?.records ?? []) {
      const no = String(row.sopNo ?? '')
      if (no && row.sopName) result[no] = String(row.sopName)
    }
  } catch { /* Keep plan labels when the DEV SOP list is unavailable. */ }
  return result
})
const choices = computed<Record<FilterKey, string[]>>(() => ({
  equipment: [...new Set(rows.value.map((row) => row.equipmentNo).filter((value) => value && !/-CH\d+$/i.test(value)))].sort(),
  frequency: [...new Set(rows.value.map((row) => row.frequencyNo).filter(Boolean))].sort(),
  creator: [...new Set(rows.value.map((row) => row.creator).filter(Boolean))].sort(),
}))
const displayedRows = computed(() => queryRows.value.filter((row) => !/-CH\d+$/i.test(row.equipmentNo)))
const calendarRows = computed(() => queryRows.value.filter((row) => row.estimatedDate.slice(0, 7) === calendarMonth.value))
const monthLabel = computed(() => calendarMonth.value.replace('-', '/'))
const selectedText = computed(() => {
  const describe = (key: FilterKey) => selectedLists.value[key].length ? selectedLists.value[key].join(', ') : '全部'
  return `設備 ${describe('equipment')}｜頻率 ${describe('frequency')}｜建立者 ${describe('creator')}`
})

watch(choices, (value) => {
  for (const group of groups) {
    const existing = new Set(value[group.key])
    poolSelection.value[group.key] = value[group.key].filter((item) => !selectedLists.value[group.key].includes(item))
    selectedLists.value[group.key] = selectedLists.value[group.key].filter((item) => existing.has(item))
  }
}, { immediate: true })

function transfer(key: FilterKey, direction: Direction) {
  const source = direction === 'right' ? poolSelection.value[key] : selectedLists.value[key]
  const target = direction === 'right' ? selectedLists.value[key] : poolSelection.value[key]
  const moving = new Set(source)
  const combined = [...target, ...source].filter((item, index, all) => all.indexOf(item) === index)
  if (direction === 'right') selectedLists.value[key] = combined
  else poolSelection.value[key] = combined
  if (direction === 'right') poolSelection.value[key] = poolSelection.value[key].filter((item) => !moving.has(item))
  else selectedLists.value[key] = selectedLists.value[key].filter((item) => !moving.has(item))
  source.splice(0)
}
function matches(key: FilterKey, row: PlanRow) {
  const selected = selectedLists.value[key]
  if (!selected.length) return true
  const value = key === 'equipment' ? row.equipmentNo : key === 'frequency' ? row.frequencyNo : row.creator
  return selected.includes(value)
}
function queryPlans() {
  if (fromEnabled.value && !fromDate.value) return void (notice.value = '請輸入預估保養日期起日。')
  if (toEnabled.value && !toDate.value) return void (notice.value = '請輸入預估保養日期迄日。')
  if (fromEnabled.value && toEnabled.value && fromDate.value > toDate.value) return void (notice.value = '日期起日不可晚於迄日。')
  queryRows.value = rows.value.filter((row) => matches('equipment', row) && matches('frequency', row) && matches('creator', row)
    && (!fromEnabled.value || row.estimatedDate >= fromDate.value) && (!toEnabled.value || row.estimatedDate <= toDate.value))
    .sort((a, b) => a.estimatedDate.localeCompare(b.estimatedDate) || a.equipmentNo.localeCompare(b.equipmentNo) || a.pmSerialNo - b.pmSerialNo)
  queried.value = true
  notice.value = `查詢完成：原始 ${queryRows.value.length} 筆，畫面顯示 ${displayedRows.value.length} 筆。`
}
function showCalendar() {
  const first = queryRows.value[0]?.estimatedDate ?? '2026-11-01'
  calendarMonth.value = first.slice(0, 7)
  calendarOpen.value = true
}
function shiftCalendarMonth(offset: number) {
  const [year, month] = calendarMonth.value.split('-').map(Number)
  const next = new Date(year, month - 1 + offset, 1)
  calendarMonth.value = `${next.getFullYear()}-${String(next.getMonth() + 1).padStart(2, '0')}`
}
function dateLabel(value: string) { return value ? value.replaceAll('-', '/') : '—' }
function frequencyLabel(row: PlanRow) { return frequencyNames.value[row.frequencyNo] || row.frequencyNo }
function sopLabel(row: PlanRow) { return sopNames.value[row.sopNo] || row.sopName || row.sopNo }

queryPlans()
</script>

<template>
  <section class="pm-plan-query" aria-label="設備保養計畫查詢 DEV 預覽">
    <header class="page-header">
      <div><p>PM MAINTENANCE QUERY / PM501</p><h1>設備保養計畫查詢</h1></div>
      <div class="header-actions"><span class="dev-tag">DEV 假資料</span><button type="button" class="secondary" @click="emit('close')">關閉</button></div>
    </header>

    <section class="filter-panel" aria-label="查詢條件">
      <div v-for="group in groups" :key="group.key" class="dual-filter">
        <label class="filter-title">{{ group.label }}</label>
        <div class="list-pair">
          <select v-model="poolSelection[group.key]" multiple :aria-label="`${group.label}未選清單`" @dblclick="transfer(group.key, 'right')">
            <option v-for="item in choices[group.key].filter((value) => !selectedLists[group.key].includes(value))" :key="item" :value="item">{{ item }}</option>
          </select>
          <div class="list-arrows"><button type="button" class="secondary" :aria-label="`加入${group.label}篩選`" @click="transfer(group.key, 'right')">›</button><button type="button" class="secondary" :aria-label="`移除${group.label}篩選`" @click="transfer(group.key, 'left')">‹</button></div>
          <select v-model="selectedLists[group.key]" multiple :aria-label="`${group.label}已選清單`" @dblclick="transfer(group.key, 'left')">
            <option v-for="item in selectedLists[group.key]" :key="item" :value="item">{{ item }}</option>
          </select>
        </div>
      </div>
      <div class="date-filter">
        <label class="filter-title">預估保養日期</label>
        <label class="date-choice"><input v-model="fromEnabled" type="checkbox"> From <input v-model="fromDate" type="date" :disabled="!fromEnabled"></label>
        <label class="date-choice"><input v-model="toEnabled" type="checkbox"> To <input v-model="toDate" type="date" :disabled="!toEnabled"></label>
        <button type="button" @click="queryPlans">查詢</button>
      </div>
    </section>

    <section class="results-panel">
      <header class="results-header"><div><h2>保養計畫</h2><p>{{ selectedText }}</p></div><button type="button" :disabled="!queried || queryRows.length === 0" @click="showCalendar">PM Calendar</button></header>
      <div class="table-wrap"><table>
        <thead><tr><th>Equipment No</th><th>Frequency</th><th>Frequency Name</th><th>SOP</th><th>SOP Name</th><th>EST Date</th><th>Creator</th><th>Reviser</th><th>Execute State</th></tr></thead>
        <tbody>
          <tr v-for="row in displayedRows" :key="row.serialNo"><td>{{ row.equipmentNo }}</td><td>{{ row.frequencyNo }}</td><td>{{ frequencyLabel(row) }}</td><td>{{ row.sopNo }}</td><td>{{ sopLabel(row) }}</td><td>{{ dateLabel(row.estimatedDate) }}</td><td>{{ row.creator || '—' }}</td><td>{{ row.reviser || '—' }}</td><td>{{ row.executeState }}</td></tr>
          <tr v-if="displayedRows.length === 0"><td colspan="9" class="empty">{{ queried ? '沒有符合條件的保養計畫。' : '設定條件後按查詢。' }}</td></tr>
        </tbody>
      </table></div>
      <p v-if="notice" class="notice" role="status">{{ notice }} <span>子設備是否顯示依本 DEV fixture 模擬 ChamberManage=0；日曆預覽接收 Core 原始結果。</span></p>
    </section>
    <footer class="page-footer"><span>DEV localStorage：{{ storageKey }}</span><span>查詢結果依 PMESTDATETIME 升冪排序；不呼叫正式 API。</span></footer>

    <div v-if="calendarOpen" class="modal-backdrop" @click.self="calendarOpen = false">
      <section class="calendar-dialog" role="dialog" aria-modal="true" aria-labelledby="calendar-title">
        <header><div><p>PM_PlanCalendar / PM502</p><h2 id="calendar-title">保養日曆預覽 — {{ monthLabel }}</h2></div><div><button type="button" class="secondary" @click="shiftCalendarMonth(-1)">‹</button> <button type="button" class="secondary" @click="shiftCalendarMonth(1)">›</button> <button type="button" class="secondary" aria-label="關閉日曆" @click="calendarOpen = false">×</button></div></header>
        <p class="calendar-note">WinForms 將原始 MaintenancePlan DataTable 複製給外部 PM_PlanCalendar；以下用 DEV 月份清單模擬開啟結果，包含結果集內子設備列。</p>
        <div class="table-wrap"><table><thead><tr><th>預估日期</th><th>設備</th><th>頻率</th><th>SOP</th><th>PM Serial</th><th>建立者</th></tr></thead><tbody><tr v-for="row in calendarRows" :key="row.serialNo"><td>{{ dateLabel(row.estimatedDate) }}</td><td>{{ row.equipmentNo }}</td><td>{{ frequencyLabel(row) }}</td><td>{{ sopLabel(row) }}</td><td>{{ row.pmSerialNo }}</td><td>{{ row.creator }}</td></tr><tr v-if="calendarRows.length === 0"><td colspan="6" class="empty">本月沒有計畫。</td></tr></tbody></table></div>
        <footer><span>{{ calendarRows.length }} 筆</span><button type="button" class="secondary" @click="calendarOpen = false">關閉</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.pm-plan-query { height:100%; min-height:0; display:flex; flex-direction:column; gap:9px; padding:12px; box-sizing:border-box; background:#f7f5ee; color:#26251f; font:12px/1.42 "Segoe UI","Microsoft JhengHei",sans-serif; }
.page-header,.header-actions,.results-header,.page-footer,.calendar-dialog header,.calendar-dialog footer { display:flex; align-items:center; }.page-header,.results-header,.calendar-dialog header,.calendar-dialog footer { justify-content:space-between; gap:10px; }.page-header { border-bottom:1px solid #d6d2c6; padding-bottom:8px; }.page-header p,.calendar-dialog header p { margin:0; color:#777366; font-size:10px; letter-spacing:.06em; }h1 { margin:2px 0 0; font-size:20px; font-weight:600; }.header-actions { gap:8px; }.dev-tag { padding:3px 8px; border-radius:3px; background:#dce9de; color:#28533a; font-size:10px; }
button { min-height:28px; padding:4px 10px; border:1px solid #454237; border-radius:3px; background:#454237; color:#fff; font:inherit; cursor:pointer; }button:disabled { opacity:.42; cursor:default; }button.secondary { border-color:#bab6a8; background:#fffef9; color:#333126; }
.filter-panel { display:grid; grid-template-columns:repeat(3,minmax(160px,1fr)) minmax(250px,1.2fr); gap:9px; padding:9px; border:1px solid #d0ccbf; background:#eeece4; }.dual-filter { min-width:0; }.filter-title { display:block; margin-bottom:4px; color:#514f45; font-weight:600; }.list-pair { display:grid; grid-template-columns:minmax(0,1fr) 28px minmax(0,1fr); gap:4px; height:96px; }.list-pair select { width:100%; min-width:0; padding:4px; border:1px solid #bbb7a9; background:#fffef9; color:#26251f; font:inherit; }.list-arrows { display:flex; flex-direction:column; justify-content:center; gap:5px; }.list-arrows button { min-height:27px; padding:2px; font-size:16px; }.date-filter { display:flex; flex-direction:column; gap:5px; align-items:flex-start; }.date-choice { display:flex; align-items:center; gap:5px; color:#514f45; }.date-choice input[type=date] { min-height:27px; min-width:0; padding:3px 5px; border:1px solid #bbb7a9; background:#fffef9; color:#26251f; font:inherit; }.date-choice input:disabled { opacity:.5; }
.results-panel { flex:1; min-height:0; display:flex; flex-direction:column; gap:8px; padding:9px; border:1px solid #d0ccbf; background:#fffef9; }.results-header h2 { margin:0; font-size:16px; }.results-header p { margin:2px 0 0; color:#777366; font-size:10px; }.table-wrap { min-height:0; overflow:auto; border:1px solid #e0ddd4; }table { width:100%; border-collapse:collapse; white-space:nowrap; }th,td { padding:6px 8px; border-bottom:1px solid #e7e4db; text-align:left; }th { position:sticky; top:0; z-index:1; background:#e9e6dc; color:#555246; font-weight:600; }tbody tr:nth-child(even) { background:#f8f7f1; }.empty { padding:20px; text-align:center; color:#777366; }.notice { margin:0; padding:6px 8px; background:#eeece4; color:#454237; }.notice span { margin-left:10px; color:#777366; }.page-footer { justify-content:space-between; gap:8px; color:#8a877c; font-size:10px; }
.modal-backdrop { position:fixed; inset:0; z-index:120; display:grid; place-items:center; padding:16px; background:#28271f80; }.calendar-dialog { width:min(900px,100%); max-height:calc(100vh - 32px); display:flex; flex-direction:column; gap:9px; padding:14px; border:1px solid #b8b4a7; border-radius:5px; background:#f7f5ee; box-shadow:0 18px 60px #0004; }.calendar-dialog h2 { margin:2px 0 0; font-size:17px; }.calendar-dialog .table-wrap { flex:1; }.calendar-note { margin:0; padding:7px 9px; background:#eeece4; color:#777366; }.calendar-dialog footer { border-top:1px solid #d6d2c6; padding-top:8px; }
@media(max-width:900px) { .filter-panel { grid-template-columns:repeat(2,minmax(0,1fr)); }.date-filter { grid-column:1/-1; flex-direction:row; flex-wrap:wrap; align-items:center; }.results-panel { min-height:260px; }.page-footer { align-items:flex-start; flex-direction:column; } }
@media(max-width:560px) { .pm-plan-query { padding:7px; }.filter-panel { grid-template-columns:1fr; }.date-filter { grid-column:auto; flex-direction:column; align-items:flex-start; }.date-choice { flex-wrap:wrap; }.date-choice input[type=date] { flex:1; }.notice span { display:block; margin:3px 0 0; }.calendar-dialog { padding:8px; } }
</style>
