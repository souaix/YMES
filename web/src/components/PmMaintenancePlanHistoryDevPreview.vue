<script setup lang="ts">
import { computed, ref } from 'vue'

interface HistoryRow {
  pmEqpSopNo: string
  equipmentNo: string
  pmSerialNo: number
  frequencyNo: string
  pmEstDateTime: string
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
  pmWoNo: string
  pmWoItemNo: number | string
  woCreateDate: string
  woReleaseDate: string
  woFinishedDate: string
  executeState: number
  logCreator: string
  logCreateDate: string
  frequencyName: string
  pmSopNo: string
  pmSopName: string
}
type FilterKey = 'equipment' | 'eqpSop' | 'frequency' | 'sop'
type Direction = 'right' | 'left'
interface FilterGroup { key: FilterKey; label: string }
interface HistoryState { version: number; rows: HistoryRow[] }

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.pm.maintenance-plan-log.v1'
const fallbackRows: HistoryRow[] = [
  { pmEqpSopNo: 'EQPSOP2500018', equipmentNo: 'CNC-02', pmSerialNo: 1, frequencyNo: 'PM-MONTHLY-15', pmEstDateTime: '2026-10-15 00:00:00', creator: 'PMUSER', createDate: '2026-01-08 09:30:00', reviser: 'PMUSER', reviseDate: '2026-01-10 14:12:00', pmWoNo: 'EW26010042', pmWoItemNo: 1, woCreateDate: '2026-10-15 08:06:00', woReleaseDate: '2026-10-15 08:25:00', woFinishedDate: '2026-10-15 11:54:00', executeState: 4, logCreator: 'PMUSER', logCreateDate: '2026-10-15 11:54:18', frequencyName: '月中設備校驗', pmSopNo: 'SOP-CAL-02', pmSopName: '感測器校正程序' },
  { pmEqpSopNo: 'EQPSOP2500021', equipmentNo: 'PUMP-07', pmSerialNo: 3, frequencyNo: 'PM-WEEKLY-01', pmEstDateTime: '2026-11-23 00:00:00', creator: 'DEV02', createDate: '2026-02-10 11:15:00', reviser: 'PMLEAD', reviseDate: '2026-08-04 10:20:00', pmWoNo: 'EW26110017', pmWoItemNo: 1, woCreateDate: '2026-11-23 07:42:00', woReleaseDate: '2026-11-23 08:05:00', woFinishedDate: '', executeState: 3, logCreator: 'PMLEAD', logCreateDate: '2026-11-23 08:05:41', frequencyName: '每週保養', pmSopNo: 'SOP-LUBE-03', pmSopName: '主軸潤滑保養程序' },
  { pmEqpSopNo: 'EQPSOP2500018', equipmentNo: 'CNC-02', pmSerialNo: 2, frequencyNo: 'PM-MONTHLY-15', pmEstDateTime: '2026-09-15 00:00:00', creator: 'PMUSER', createDate: '2026-01-08 09:30:00', reviser: 'PMUSER', reviseDate: '2026-03-04 15:20:00', pmWoNo: 'EW26090055', pmWoItemNo: 1, woCreateDate: '2026-09-15 08:00:00', woReleaseDate: '2026-09-15 08:18:00', woFinishedDate: '', executeState: 2, logCreator: 'PMUSER', logCreateDate: '2026-09-15 08:00:12', frequencyName: '月中設備校驗', pmSopNo: 'SOP-CAL-02', pmSopName: '感測器校正程序' },
  { pmEqpSopNo: 'EQPSOP2500021', equipmentNo: 'PUMP-07', pmSerialNo: 2, frequencyNo: 'PM-WEEKLY-01', pmEstDateTime: '2026-11-16 00:00:00', creator: 'DEV02', createDate: '2026-02-10 11:15:00', reviser: '', reviseDate: '', pmWoNo: '', pmWoItemNo: 0, woCreateDate: '', woReleaseDate: '', woFinishedDate: '', executeState: 0, logCreator: 'DEV02', logCreateDate: '2026-11-14 16:22:08', frequencyName: '每週保養', pmSopNo: 'SOP-LUBE-03', pmSopName: '主軸潤滑保養程序' },
  { pmEqpSopNo: 'EQPSOP2500036', equipmentNo: 'PRESS-04', pmSerialNo: 5, frequencyNo: 'PM-QUARTER-01', pmEstDateTime: '2026-08-01 00:00:00', creator: 'PMUSER', createDate: '2026-03-02 13:40:00', reviser: 'PMLEAD', reviseDate: '2026-07-20 09:10:00', pmWoNo: '', pmWoItemNo: 0, woCreateDate: '', woReleaseDate: '', woFinishedDate: '', executeState: 5, logCreator: 'PMLEAD', logCreateDate: '2026-07-20 09:11:31', frequencyName: '每季檢查', pmSopNo: 'SOP-PRESS-01', pmSopName: '沖床安全檢查程序' },
  { pmEqpSopNo: 'EQPSOP2500036', equipmentNo: 'PRESS-04-CH1', pmSerialNo: 1, frequencyNo: 'PM-QUARTER-01', pmEstDateTime: '2026-08-01 00:00:00', creator: 'PMUSER', createDate: '2026-03-02 13:40:00', reviser: 'PMLEAD', reviseDate: '2026-07-20 09:10:00', pmWoNo: '', pmWoItemNo: 0, woCreateDate: '', woReleaseDate: '', woFinishedDate: '', executeState: 5, logCreator: 'PMLEAD', logCreateDate: '2026-07-20 09:11:31', frequencyName: '每季檢查', pmSopNo: 'SOP-PRESS-01', pmSopName: '沖床安全檢查程序' },
]
const statusOptions = [
  { value: 0, label: 'Adjust To Cancel' }, { value: 2, label: 'WO Created' }, { value: 3, label: 'WO Released' },
  { value: 4, label: 'WO Finished' }, { value: 5, label: 'Deleted' },
]
const groups: FilterGroup[] = [
  { key: 'equipment', label: 'Equipment No' }, { key: 'eqpSop', label: 'EQP SOP No' },
  { key: 'frequency', label: 'Frequency No' }, { key: 'sop', label: 'SOP No' },
]
const chamberManage = false
const allRows = ref(readRows())
const poolSelection = ref<Record<FilterKey, string[]>>({ equipment: [], eqpSop: [], frequency: [], sop: [] })
const selectedLists = ref<Record<FilterKey, string[]>>({ equipment: [], eqpSop: [], frequency: [], sop: [] })
const enabledStates = ref<number[]>([])
const fromEnabled = ref(false)
const toEnabled = ref(false)
const fromDate = ref('2026-11-01')
const toDate = ref('2026-11-30')
const rows = ref<HistoryRow[]>([])
const queried = ref(false)
const notice = ref('設定條件後按 Query。')
const filteredSource = computed(() => allRows.value.filter((row) => chamberManage || !/-CH\d+$/i.test(row.equipmentNo)))
const choices = computed<Record<FilterKey, string[]>>(() => ({
  equipment: distinct(filteredSource.value.map((row) => row.equipmentNo)),
  eqpSop: distinct(filteredSource.value.map((row) => row.pmEqpSopNo)),
  frequency: distinct(filteredSource.value.map((row) => row.frequencyNo)),
  sop: distinct(filteredSource.value.map((row) => row.pmSopNo)),
}))

function readRows(): HistoryRow[] {
  try {
    const saved = JSON.parse(window.localStorage.getItem(storageKey) ?? 'null') as HistoryState | null
    if (saved?.version === 1 && Array.isArray(saved.rows)) return saved.rows
    window.localStorage.setItem(storageKey, JSON.stringify({ version: 1, rows: fallbackRows } satisfies HistoryState))
  } catch { /* Keep the read-only fixture available when browser storage is blocked. */ }
  return fallbackRows
}
function distinct(values: string[]) { return [...new Set(values.filter(Boolean))].sort((a, b) => a.localeCompare(b)) }
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
function matches(key: FilterKey, row: HistoryRow) {
  const selected = selectedLists.value[key]
  if (!selected.length) return true
  const value = key === 'equipment' ? row.equipmentNo : key === 'eqpSop' ? row.pmEqpSopNo : key === 'frequency' ? row.frequencyNo : row.pmSopNo
  return selected.includes(value)
}
function queryHistory() {
  if (fromEnabled.value && !fromDate.value) return void (notice.value = '請設定預估保養日期起日。')
  if (toEnabled.value && !toDate.value) return void (notice.value = '請設定預估保養日期迄日。')
  if (fromEnabled.value && toEnabled.value && fromDate.value > toDate.value) return void (notice.value = '日期起日不可晚於迄日。')
  rows.value = filteredSource.value.filter((row) => matches('equipment', row) && matches('eqpSop', row) && matches('frequency', row) && matches('sop', row)
    && (!fromEnabled.value || row.pmEstDateTime.slice(0, 10) >= fromDate.value)
    && (!toEnabled.value || row.pmEstDateTime.slice(0, 10) <= toDate.value)
    && (!enabledStates.value.length || enabledStates.value.includes(row.executeState)))
    .sort((a, b) => a.createDate.localeCompare(b.createDate))
  queried.value = true
  notice.value = `查詢完成：${rows.value.length} 筆；排序依 CreateDate 升冪。`
}
function stateLabel(value: number) { return statusOptions.find((item) => item.value === value)?.label ?? `State ${value}` }
function dateLabel(value: string) { return value ? value.replace('T', ' ').slice(0, 19) : '—' }
</script>

<template>
  <section class="pm-plan-history" aria-label="保養計劃歷史資料查詢 DEV 預覽">
    <header class="page-header">
      <div><p>PM PLAN HISTORY / PM503</p><h1>保養計劃歷史資料查詢</h1></div>
      <div class="header-actions"><span class="dev-tag">DEV 假資料</span><button type="button" class="secondary" @click="emit('close')">Close (X)</button></div>
    </header>

    <section class="criteria-panel" aria-label="查詢條件">
      <div class="list-filters">
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
      </div>
      <div class="criteria-bottom">
        <div class="date-filter">
          <span class="filter-title">Estimated PM Date</span>
          <label class="date-choice"><input v-model="fromEnabled" type="checkbox"> From <input v-model="fromDate" type="date" :disabled="!fromEnabled"></label>
          <label class="date-choice"><input v-model="toEnabled" type="checkbox"> To <input v-model="toDate" type="date" :disabled="!toEnabled"></label>
        </div>
        <fieldset class="state-filter"><legend>Executive State</legend>
          <label v-for="state in statusOptions" :key="state.value"><input v-model="enabledStates" type="checkbox" :value="state.value"> {{ state.label }}</label>
        </fieldset>
        <button type="button" @click="queryHistory">Query</button>
      </div>
    </section>

    <section class="results-panel">
      <header class="results-header"><div><h2>保養計畫歷史</h2><p>{{ notice }}</p></div><span>{{ queried ? `${rows.length} rows` : '尚未查詢' }}</span></header>
      <div class="table-wrap"><table>
        <thead><tr><th>PM EQP SOP No</th><th>Equipment No</th><th>PM Serial No</th><th>Frequency No</th><th>PM EST Date</th><th>Creator</th><th>Create Date</th><th>Reviser</th><th>Revise Date</th><th>PM WO No</th><th>WO Item</th><th>WO Create Date</th><th>WO Release Date</th><th>WO Finished Date</th><th>Execute State</th><th>Log Creator</th><th>Log Create Date</th><th>Frequency Name</th><th>SOP Name</th></tr></thead>
        <tbody>
          <tr v-for="(row, index) in rows" :key="`${row.pmEqpSopNo}-${row.pmSerialNo}-${row.logCreateDate}-${index}`">
            <td>{{ row.pmEqpSopNo }}</td><td>{{ row.equipmentNo }}</td><td>{{ row.pmSerialNo }}</td><td>{{ row.frequencyNo }}</td><td>{{ dateLabel(row.pmEstDateTime) }}</td><td>{{ row.creator || '—' }}</td><td>{{ dateLabel(row.createDate) }}</td><td>{{ row.reviser || '—' }}</td><td>{{ dateLabel(row.reviseDate) }}</td><td>{{ row.pmWoNo || '—' }}</td><td>{{ row.pmWoItemNo || '—' }}</td><td>{{ dateLabel(row.woCreateDate) }}</td><td>{{ dateLabel(row.woReleaseDate) }}</td><td>{{ dateLabel(row.woFinishedDate) }}</td><td><span class="state-tag" :class="`state-${row.executeState}`">{{ stateLabel(row.executeState) }}</span></td><td>{{ row.logCreator || '—' }}</td><td>{{ dateLabel(row.logCreateDate) }}</td><td>{{ row.frequencyName }}</td><td>{{ row.pmSopName }}</td>
          </tr>
          <tr v-if="rows.length === 0"><td colspan="19" class="empty">{{ queried ? '沒有符合條件的歷史資料。' : '設定條件後按 Query。' }}</td></tr>
        </tbody>
      </table></div>
    </section>
    <footer class="page-footer"><span>DEV localStorage：{{ storageKey }}</span><span>查詢僅模擬 PM503 篩選，不呼叫正式 API。</span></footer>
  </section>
</template>

<style scoped>
.pm-plan-history { height:100%; min-height:0; display:flex; flex-direction:column; gap:9px; padding:12px; box-sizing:border-box; background:#f7f5ee; color:#26251f; font:12px/1.42 "Segoe UI","Microsoft JhengHei",sans-serif; }
.page-header,.header-actions,.results-header,.page-footer { display:flex; align-items:center; }.page-header,.results-header { justify-content:space-between; gap:10px; }.page-header { border-bottom:1px solid #d6d2c6; padding-bottom:8px; }.page-header p { margin:0; color:#777366; font-size:10px; letter-spacing:.06em; }h1 { margin:2px 0 0; font-size:20px; font-weight:600; }.header-actions { gap:8px; }.dev-tag { padding:3px 8px; border-radius:3px; background:#dce9de; color:#28533a; font-size:10px; }
button { min-height:28px; padding:4px 10px; border:1px solid #454237; border-radius:3px; background:#454237; color:#fff; font:inherit; cursor:pointer; }button.secondary { border-color:#bab6a8; background:#fffef9; color:#333126; }
.criteria-panel { display:flex; flex-direction:column; gap:9px; padding:9px; border:1px solid #d0ccbf; background:#eeece4; }.list-filters { display:grid; grid-template-columns:repeat(4,minmax(150px,1fr)); gap:9px; }.dual-filter { min-width:0; }.filter-title { display:block; margin-bottom:4px; color:#514f45; font-weight:600; }.list-pair { display:grid; grid-template-columns:minmax(0,1fr) 28px minmax(0,1fr); gap:4px; height:76px; }.list-pair select { width:100%; min-width:0; padding:4px; border:1px solid #bbb7a9; background:#fffef9; color:#26251f; font:inherit; }.list-arrows { display:flex; flex-direction:column; justify-content:center; gap:5px; }.list-arrows button { min-height:25px; padding:2px; font-size:16px; }.criteria-bottom { display:flex; align-items:flex-end; flex-wrap:wrap; gap:14px; }.date-filter { display:flex; align-items:center; flex-wrap:wrap; gap:7px; }.date-choice { display:flex; align-items:center; gap:4px; color:#514f45; }.date-choice input[type=date] { min-height:27px; min-width:0; padding:3px 5px; border:1px solid #bbb7a9; background:#fffef9; color:#26251f; font:inherit; }.date-choice input:disabled { opacity:.5; }.state-filter { display:flex; align-items:center; flex-wrap:wrap; gap:9px; min-width:0; margin:0; padding:3px 8px 6px; border:1px solid #c8c4b7; }.state-filter legend { padding:0 3px; color:#514f45; font-weight:600; }.state-filter label { white-space:nowrap; color:#514f45; }
.results-panel { flex:1; min-height:0; display:flex; flex-direction:column; gap:8px; padding:9px; border:1px solid #d0ccbf; background:#fffef9; }.results-header h2 { margin:0; font-size:16px; }.results-header p { margin:2px 0 0; color:#777366; font-size:10px; }.table-wrap { min-height:0; overflow:auto; border:1px solid #e0ddd4; }table { width:max-content; min-width:100%; border-collapse:collapse; white-space:nowrap; }th,td { padding:6px 8px; border-bottom:1px solid #e7e4db; text-align:left; }th { position:sticky; top:0; z-index:1; background:#e9e6dc; color:#555246; font-weight:600; }tbody tr:nth-child(even) { background:#f8f7f1; }.empty { padding:22px; text-align:center; color:#777366; }.state-tag { display:inline-block; padding:2px 6px; border-radius:3px; background:#e4e1d8; color:#514f45; }.state-2 { background:#e4ebf7; color:#32547a; }.state-3 { background:#f8edcf; color:#775b16; }.state-4 { background:#dce9de; color:#28533a; }.state-5 { background:#f1dddd; color:#8c3535; }.page-footer { justify-content:space-between; gap:8px; color:#8a877c; font-size:10px; }
@media(max-width:900px) { .list-filters { grid-template-columns:repeat(2,minmax(0,1fr)); }.criteria-bottom { align-items:flex-start; }.state-filter { flex:1 1 100%; }.page-footer { align-items:flex-start; flex-direction:column; } }
@media(max-width:560px) { .pm-plan-history { padding:7px; }.list-filters { grid-template-columns:1fr; }.date-filter { align-items:flex-start; flex-direction:column; }.date-choice { flex-wrap:wrap; } }
</style>
