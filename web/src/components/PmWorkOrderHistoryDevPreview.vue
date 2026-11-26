<script setup lang="ts">
import { computed, ref } from 'vue'

interface WorkOrderDetail {
  woItemNo: number
  equipmentNo: string
  pmSopNo: string
  pmEstDateTime: string
  releaseDate: string
  finishedDate: string
  executor: string
  executeDesc: string
}
interface WorkOrderHistory {
  woNo: string
  woType: 0 | 1
  description: string
  creator: string
  createDate: string
  details: WorkOrderDetail[]
}
type WorkOrderType = 'all' | 'pm' | 'request'
type Direction = 'right' | 'left'
interface HistoryState { version: number; workOrders: WorkOrderHistory[] }

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.pm.work-order-history.v1'
const fallbackWorkOrders: WorkOrderHistory[] = [
  { woNo: 'EW26110017', woType: 0, description: 'CNC-02 November preventive maintenance', creator: 'PMUSER', createDate: '2026-11-15 08:02:16', details: [
    { woItemNo: 1, equipmentNo: 'CNC-02', pmSopNo: 'SOP-CAL-02', pmEstDateTime: '2026-11-15 00:00:00', releaseDate: '2026-11-15 08:25:00', finishedDate: '2026-11-15 11:54:00', executor: 'ENG01', executeDesc: 'Sensor calibration completed.' },
    { woItemNo: 2, equipmentNo: 'CNC-02-CH1', pmSopNo: 'SOP-CAL-02', pmEstDateTime: '2026-11-15 00:00:00', releaseDate: '2026-11-15 08:25:00', finishedDate: '2026-11-15 11:50:00', executor: 'ENG01', executeDesc: 'Chamber check completed.' },
  ] },
  { woNo: 'EW26090055', woType: 0, description: 'PUMP-07 weekly lubrication', creator: 'DEV02', createDate: '2026-09-14 16:41:02', details: [
    { woItemNo: 1, equipmentNo: 'PUMP-07', pmSopNo: 'SOP-LUBE-03', pmEstDateTime: '2026-09-15 00:00:00', releaseDate: '2026-09-15 07:58:00', finishedDate: '2026-09-15 09:32:00', executor: 'ENG03', executeDesc: 'Lubrication and vibration inspection completed.' },
  ] },
  { woNo: 'RW26100008', woType: 1, description: 'PRESS-04 abnormal noise inspection', creator: 'OPERATOR7', createDate: '2026-10-18 10:05:48', details: [
    { woItemNo: 1, equipmentNo: 'PRESS-04', pmSopNo: 'SOP-PRESS-01', pmEstDateTime: '2026-10-18 10:05:00', releaseDate: '2026-10-18 10:20:00', finishedDate: '2026-10-18 12:44:00', executor: 'ENG02', executeDesc: 'Tightened the coupling and verified operation.' },
  ] },
  { woNo: 'EW26100042', woType: 0, description: 'Monthly line equipment inspection', creator: 'PMUSER', createDate: '2026-10-28 09:00:00', details: [
    { woItemNo: 1, equipmentNo: 'CNC-02', pmSopNo: 'SOP-CAL-02', pmEstDateTime: '2026-11-15 00:00:00', releaseDate: '2026-11-15 08:10:00', finishedDate: '', executor: 'ENG01', executeDesc: 'Awaiting replacement sensor.' },
    { woItemNo: 2, equipmentNo: 'PUMP-07', pmSopNo: 'SOP-LUBE-03', pmEstDateTime: '2026-11-16 00:00:00', releaseDate: '2026-11-16 08:15:00', finishedDate: '', executor: 'ENG03', executeDesc: 'Maintenance started.' },
  ] },
  { woNo: 'RW26090017', woType: 1, description: 'Chamber pressure alarm investigation', creator: 'OPERATOR2', createDate: '2026-09-12 13:22:51', details: [
    { woItemNo: 1, equipmentNo: 'CNC-02-CH1', pmSopNo: 'SOP-CAL-02', pmEstDateTime: '2026-09-12 13:20:00', releaseDate: '2026-09-12 13:30:00', finishedDate: '2026-09-12 15:12:00', executor: 'ENG01', executeDesc: 'Alarm sensor reset.' },
  ] },
  { woNo: 'EW26080031', woType: 0, description: 'Quarterly press safety check', creator: 'PMLEAD', createDate: '2026-08-01 07:30:00', details: [
    { woItemNo: 1, equipmentNo: 'PRESS-04', pmSopNo: 'SOP-PRESS-01', pmEstDateTime: '2026-08-01 00:00:00', releaseDate: '2026-08-01 07:55:00', finishedDate: '2026-08-01 10:06:00', executor: 'ENG02', executeDesc: 'Safety interlock and emergency stop verified.' },
  ] },
]
const chamberManage = false
const sourceRows = ref(readWorkOrders())
const selectedPool = ref<string[]>([])
const selectedEquipment = ref<string[]>([])
const selectedType = ref<WorkOrderType>('all')
const estFromEnabled = ref(false)
const estToEnabled = ref(false)
const estFrom = ref('2026-11-01')
const estTo = ref('2026-11-30')
const finishFromEnabled = ref(false)
const finishToEnabled = ref(false)
const finishFrom = ref('2026-11-01')
const finishTo = ref('2026-11-30')
const queryRows = ref<WorkOrderHistory[]>([])
const activeWoNo = ref('')
const queried = ref(false)
const notice = ref('設定條件後按 Query。')
const equipmentOptions = computed(() => [...new Set(sourceRows.value.flatMap((row) => row.details.map((detail) => detail.equipmentNo)).filter((equipmentNo) => chamberManage || !isChamber(equipmentNo)))].sort((a, b) => a.localeCompare(b)))
const detailRows = computed(() => {
  const selected = queryRows.value.find((row) => row.woNo === activeWoNo.value)
  if (!selected) return []
  return selected.details.filter((detail) => chamberManage || equipmentOptions.value.includes(detail.equipmentNo))
})
const activeWorkOrder = computed(() => queryRows.value.find((row) => row.woNo === activeWoNo.value) ?? null)

function readWorkOrders(): WorkOrderHistory[] {
  try {
    const saved = JSON.parse(window.localStorage.getItem(storageKey) ?? 'null') as HistoryState | null
    if (saved?.version === 1 && Array.isArray(saved.workOrders)) return saved.workOrders
    window.localStorage.setItem(storageKey, JSON.stringify({ version: 1, workOrders: fallbackWorkOrders } satisfies HistoryState))
  } catch { /* Keep the readonly fixture available when browser storage is blocked. */ }
  return fallbackWorkOrders
}
function isChamber(equipmentNo: string) { return /-CH\d+$/i.test(equipmentNo) }
function transfer(direction: Direction) {
  const source = direction === 'right' ? selectedPool.value : selectedEquipment.value
  const target = direction === 'right' ? selectedEquipment.value : selectedPool.value
  const moving = new Set(source)
  const combined = [...target, ...source].filter((item, index, all) => all.indexOf(item) === index).sort((a, b) => a.localeCompare(b))
  if (direction === 'right') selectedEquipment.value = combined
  else selectedPool.value = combined
  if (direction === 'right') selectedPool.value = selectedPool.value.filter((item) => !moving.has(item))
  else selectedEquipment.value = selectedEquipment.value.filter((item) => !moving.has(item))
  source.splice(0)
}
function dateMatches(value: string, fromEnabled: boolean, from: string, toEnabled: boolean, to: string) {
  if ((fromEnabled || toEnabled) && !value) return false
  if (fromEnabled && value.slice(0, 10) < from) return false
  if (toEnabled && value.slice(0, 10) > to) return false
  return true
}
function detailMatchesQuery(detail: WorkOrderDetail) {
  return (!selectedEquipment.value.length || selectedEquipment.value.includes(detail.equipmentNo))
    && dateMatches(detail.pmEstDateTime, estFromEnabled.value, estFrom.value, estToEnabled.value, estTo.value)
    && dateMatches(detail.finishedDate, finishFromEnabled.value, finishFrom.value, finishToEnabled.value, finishTo.value)
}
function hasDetailCriteria() {
  return selectedEquipment.value.length > 0 || estFromEnabled.value || estToEnabled.value || finishFromEnabled.value || finishToEnabled.value
}
function queryHistory() {
  queryRows.value = sourceRows.value.filter((row) => (selectedType.value === 'all' || (selectedType.value === 'pm' ? row.woType === 0 : row.woType === 1))
    && (!hasDetailCriteria() || row.details.some(detailMatchesQuery)))
    .sort((a, b) => a.createDate.localeCompare(b.createDate))
  queried.value = true
  activeWoNo.value = ''
  notice.value = `查詢完成：${queryRows.value.length} 張工單。點選主檔查看明細；排序依 CreateDate 升冪。`
}
function selectWorkOrder(woNo: string) { activeWoNo.value = woNo }
function woTypeLabel(value: number) { return value === 0 ? 'PM Work Order' : 'Required Work Order' }
function dateLabel(value: string) { return value ? value.replace('T', ' ').slice(0, 19) : '—' }
</script>

<template>
  <section class="pm-wo-history" aria-label="保養工單歷史資料查詢 DEV 預覽">
    <header class="page-header">
      <div><p>PMWO HISTORY / PM504</p><h1>保養工單歷史資料查詢</h1></div>
      <div class="header-actions"><span class="dev-tag">DEV 假資料</span><button type="button" class="secondary" @click="emit('close')">Close (X)</button></div>
    </header>

    <section class="criteria-panel" aria-label="查詢條件">
      <div class="equipment-filter">
        <label class="filter-title">Equipment No</label>
        <div class="list-pair">
          <select v-model="selectedPool" multiple aria-label="未選設備清單" @dblclick="transfer('right')">
            <option v-for="equipmentNo in equipmentOptions.filter((value) => !selectedEquipment.includes(value))" :key="equipmentNo" :value="equipmentNo">{{ equipmentNo }}</option>
          </select>
          <div class="list-arrows"><button type="button" class="secondary" aria-label="加入設備篩選" @click="transfer('right')">›</button><button type="button" class="secondary" aria-label="移除設備篩選" @click="transfer('left')">‹</button></div>
          <select v-model="selectedEquipment" multiple aria-label="已選設備清單" @dblclick="transfer('left')">
            <option v-for="equipmentNo in selectedEquipment" :key="equipmentNo" :value="equipmentNo">{{ equipmentNo }}</option>
          </select>
        </div>
      </div>
      <div class="date-filters">
        <div class="date-filter"><span class="filter-title">Estimated PM Date</span><label><input v-model="estFromEnabled" type="checkbox"> From <input v-model="estFrom" type="date" :disabled="!estFromEnabled"></label><label><input v-model="estToEnabled" type="checkbox"> To <input v-model="estTo" type="date" :disabled="!estToEnabled"></label></div>
        <div class="date-filter"><span class="filter-title">Finished Date</span><label><input v-model="finishFromEnabled" type="checkbox"> From <input v-model="finishFrom" type="date" :disabled="!finishFromEnabled"></label><label><input v-model="finishToEnabled" type="checkbox"> To <input v-model="finishTo" type="date" :disabled="!finishToEnabled"></label></div>
        <fieldset class="type-filter"><legend>WO Type</legend>
          <label><input v-model="selectedType" type="radio" name="pm-wo-type" value="all"> All</label>
          <label><input v-model="selectedType" type="radio" name="pm-wo-type" value="pm"> PM Order</label>
          <label><input v-model="selectedType" type="radio" name="pm-wo-type" value="request"> REQ Order</label>
        </fieldset>
        <button type="button" @click="queryHistory">Query</button>
      </div>
    </section>

    <section class="results-panel">
      <div class="grid-panel master-panel">
        <header class="results-header"><div><h2>PM Work Order History</h2><p>{{ notice }}</p></div><span>{{ queried ? `${queryRows.length} rows` : '尚未查詢' }}</span></header>
        <div class="table-wrap"><table>
          <thead><tr><th>WO No</th><th>WO Type</th><th>Description</th><th>Creator</th><th>Create Date</th></tr></thead>
          <tbody>
            <tr v-for="row in queryRows" :key="row.woNo" :class="{ active: row.woNo === activeWoNo }" @click="selectWorkOrder(row.woNo)"><td>{{ row.woNo }}</td><td>{{ woTypeLabel(row.woType) }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ dateLabel(row.createDate) }}</td></tr>
            <tr v-if="queryRows.length === 0"><td colspan="5" class="empty">{{ queried ? '沒有符合條件的工單。' : '設定條件後按 Query。' }}</td></tr>
          </tbody>
        </table></div>
      </div>
      <div class="grid-panel detail-panel">
        <header class="results-header"><div><h2>工單明細</h2><p>{{ activeWorkOrder ? `${activeWorkOrder.woNo}｜${woTypeLabel(activeWorkOrder.woType)}` : '選取主檔工單載入明細。' }}</p></div><span>{{ detailRows.length }} items</span></header>
        <div class="table-wrap"><table>
          <thead><tr><th>Item No</th><th>Equipment No</th><th>SOP No</th><th>Estimated PM Date</th><th>Release Date</th><th>Finished Date</th><th>Executor</th><th>Executed Description</th></tr></thead>
          <tbody>
            <tr v-for="detail in detailRows" :key="`${activeWoNo}-${detail.woItemNo}`"><td>{{ detail.woItemNo }}</td><td>{{ detail.equipmentNo }}</td><td>{{ detail.pmSopNo }}</td><td>{{ dateLabel(detail.pmEstDateTime) }}</td><td>{{ dateLabel(detail.releaseDate) }}</td><td>{{ dateLabel(detail.finishedDate) }}</td><td>{{ detail.executor }}</td><td>{{ detail.executeDesc }}</td></tr>
            <tr v-if="detailRows.length === 0"><td colspan="8" class="empty">{{ activeWorkOrder ? '此工單沒有可顯示的明細。' : '選取工單後顯示明細。' }}</td></tr>
          </tbody>
        </table></div>
      </div>
    </section>
    <footer class="page-footer"><span>DEV localStorage：{{ storageKey }}</span><span>主檔依明細條件篩選；點選工單讀取明細。未呼叫正式 API。</span></footer>
  </section>
</template>

<style scoped>
.pm-wo-history { height:100%; min-height:0; display:flex; flex-direction:column; gap:9px; padding:12px; box-sizing:border-box; background:#f7f5ee; color:#26251f; font:12px/1.42 "Segoe UI","Microsoft JhengHei",sans-serif; }
.page-header,.header-actions,.results-header,.page-footer { display:flex; align-items:center; }.page-header,.results-header { justify-content:space-between; gap:10px; }.page-header { border-bottom:1px solid #d6d2c6; padding-bottom:8px; }.page-header p { margin:0; color:#777366; font-size:10px; letter-spacing:.06em; }h1 { margin:2px 0 0; font-size:20px; font-weight:600; }.header-actions { gap:8px; }.dev-tag { padding:3px 8px; border-radius:3px; background:#dce9de; color:#28533a; font-size:10px; }
button { min-height:28px; padding:4px 10px; border:1px solid #454237; border-radius:3px; background:#454237; color:#fff; font:inherit; cursor:pointer; }button.secondary { border-color:#bab6a8; background:#fffef9; color:#333126; }
.criteria-panel { display:grid; grid-template-columns:minmax(240px,.8fr) minmax(500px,2fr); gap:12px; padding:9px; border:1px solid #d0ccbf; background:#eeece4; }.filter-title { display:block; margin-bottom:4px; color:#514f45; font-weight:600; }.list-pair { display:grid; grid-template-columns:minmax(0,1fr) 28px minmax(0,1fr); gap:4px; height:100px; }.list-pair select { width:100%; min-width:0; padding:4px; border:1px solid #bbb7a9; background:#fffef9; color:#26251f; font:inherit; }.list-arrows { display:flex; flex-direction:column; justify-content:center; gap:5px; }.list-arrows button { min-height:25px; padding:2px; font-size:16px; }.date-filters { display:flex; align-items:flex-end; flex-wrap:wrap; gap:9px 14px; }.date-filter { display:grid; grid-template-columns:auto auto auto; align-items:center; gap:5px 9px; }.date-filter .filter-title { grid-column:1/-1; margin:0; }.date-filter label { display:flex; align-items:center; gap:4px; white-space:nowrap; color:#514f45; }.date-filter input[type=date] { min-height:27px; min-width:0; padding:3px 5px; border:1px solid #bbb7a9; background:#fffef9; color:#26251f; font:inherit; }.date-filter input:disabled { opacity:.5; }.type-filter { display:flex; align-items:center; flex-wrap:wrap; gap:8px; min-width:0; margin:0; padding:3px 8px 6px; border:1px solid #c8c4b7; }.type-filter legend { padding:0 3px; color:#514f45; font-weight:600; }.type-filter label { white-space:nowrap; color:#514f45; }
.results-panel { flex:1; min-height:0; display:grid; grid-template-rows:minmax(130px,1.25fr) minmax(125px,1fr); gap:9px; }.grid-panel { min-height:0; display:flex; flex-direction:column; gap:7px; padding:8px; border:1px solid #d0ccbf; background:#fffef9; }.results-header h2 { margin:0; font-size:15px; }.results-header p { margin:2px 0 0; color:#777366; font-size:10px; }.table-wrap { min-height:0; overflow:auto; border:1px solid #e0ddd4; }table { width:100%; border-collapse:collapse; white-space:nowrap; }th,td { padding:5px 8px; border-bottom:1px solid #e7e4db; text-align:left; }th { position:sticky; top:0; z-index:1; background:#e9e6dc; color:#555246; font-weight:600; }tbody tr:nth-child(even) { background:#f8f7f1; }tbody tr.active { background:#e2ebf2; }.master-panel tbody tr { cursor:pointer; }.empty { padding:18px; text-align:center; color:#777366; }.page-footer { justify-content:space-between; gap:8px; color:#8a877c; font-size:10px; }
@media(max-width:950px) { .criteria-panel { grid-template-columns:1fr; }.equipment-filter { max-width:440px; }.results-panel { min-height:370px; }.page-footer { align-items:flex-start; flex-direction:column; } }
@media(max-width:560px) { .pm-wo-history { padding:7px; }.date-filters { align-items:flex-start; flex-direction:column; }.date-filter { grid-template-columns:1fr; }.date-filter .filter-title { grid-column:auto; }.results-panel { grid-template-rows:minmax(180px,1fr) minmax(180px,1fr); } }
</style>
