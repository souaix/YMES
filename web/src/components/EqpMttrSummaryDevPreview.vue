<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

type FilterKey = 'areaNo' | 'equipmentType' | 'equipmentNo'
interface EquipmentSeed { areaNo: string; areaName: string; equipmentType: string; equipmentNo: string; belongEquipmentNo: string; chamberFlag: boolean; breakdownShares: number[] }
interface SummaryRow { areaNo: string; equipmentType: string; equipmentNo: string; belongEquipmentNo: string; chamberFlag: boolean; breakdownTimes: number | null; mttr: number | null; mtbf: number | null; availability: number | null }
interface Option { value: string; label: string }

const emit = defineEmits<{ close: [] }>()
const chamberManageEnabled = true // DEV 的 TBLSYSPARAMETER.ChamberManage 假設定值
const rowsSeed: EquipmentSeed[] = [
  { areaNo: 'AREA-A', areaName: '一廠組裝區', equipmentType: 'Bonder', equipmentNo: 'EQ-ASM-001', belongEquipmentNo: '', chamberFlag: false, breakdownShares: [.08, .045] },
  { areaNo: 'AREA-A', areaName: '一廠組裝區', equipmentType: 'Die Attach', equipmentNo: 'EQ-ASM-002', belongEquipmentNo: '', chamberFlag: false, breakdownShares: [.035] },
  { areaNo: 'AREA-B', areaName: '二廠測試區', equipmentType: 'Handler', equipmentNo: 'EQ-TST-001', belongEquipmentNo: '', chamberFlag: false, breakdownShares: [] },
  { areaNo: 'AREA-B', areaName: '二廠測試區', equipmentType: 'Tester', equipmentNo: 'EQ-TST-002', belongEquipmentNo: '', chamberFlag: false, breakdownShares: [.12, .025, .018] },
  { areaNo: 'AREA-C', areaName: '三廠包裝區', equipmentType: 'Packer', equipmentNo: 'EQ-PKG-001', belongEquipmentNo: '', chamberFlag: false, breakdownShares: [.055] },
  { areaNo: 'AREA-A', areaName: '一廠組裝區', equipmentType: 'Bonder', equipmentNo: 'CH-ASM-001-01', belongEquipmentNo: 'EQ-ASM-001', chamberFlag: true, breakdownShares: [.06] },
  { areaNo: 'AREA-A', areaName: '一廠組裝區', equipmentType: 'Bonder', equipmentNo: 'CH-ASM-001-02', belongEquipmentNo: 'EQ-ASM-001', chamberFlag: true, breakdownShares: [.09, .02] },
  { areaNo: 'AREA-B', areaName: '二廠測試區', equipmentType: 'Tester', equipmentNo: 'CH-TST-002-01', belongEquipmentNo: 'EQ-TST-002', chamberFlag: true, breakdownShares: [.03] },
]

const today = new Date()
const todayText = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`
const startDate = ref(todayText)
const endDate = ref(todayText)
const filters = reactive<Record<FilterKey, string>>({ areaNo: '', equipmentType: '', equipmentNo: '' })
const enabled = reactive<Record<FilterKey, boolean>>({ areaNo: false, equipmentType: false, equipmentNo: false })
const showChamber = ref(false)
const resultRows = ref<SummaryRow[]>([])
const queriedRange = ref('')
const queryError = ref('')
const notice = ref('')
const showInfo = ref(false)
const pickerOpen = ref(false)
const pickerKey = ref<FilterKey>('areaNo')
const pickerSearch = ref('')
const stagedValues = reactive(new Set<string>())

function selectedValues(key: FilterKey) { return new Set(filters[key].split(',').map((value) => value.trim().toUpperCase()).filter(Boolean)) }
const areas: Option[] = Array.from(new Map(rowsSeed.filter((row) => !row.chamberFlag).map((row) => [row.areaNo, { value: row.areaNo, label: `${row.areaNo} · ${row.areaName}` }])).values())
const types: Option[] = Array.from(new Set(rowsSeed.filter((row) => !row.chamberFlag).map((row) => row.equipmentType))).map((value) => ({ value, label: value }))
const pickerOptions = computed(() => {
  const term = pickerSearch.value.trim().toUpperCase()
  const equipmentTypes = selectedValues('equipmentType')
  const source: Option[] = pickerKey.value === 'areaNo'
    ? areas
    : pickerKey.value === 'equipmentType'
      ? types
      : rowsSeed.filter((row) => !row.chamberFlag && (!equipmentTypes.size || equipmentTypes.has(row.equipmentType.toUpperCase()))).map((row) => ({ value: row.equipmentNo, label: `${row.equipmentNo} · ${row.equipmentType}` }))
  return source.filter((option) => !term || option.label.toUpperCase().includes(term))
})
const pickerTitle = computed(() => ({ areaNo: '選擇 Area No', equipmentType: '選擇 Equipment Type', equipmentNo: '選擇 Equipment No' }[pickerKey.value]))
const activeEquipmentCount = computed(() => resultRows.value.length)

function setFilterEnabled(key: FilterKey, checked: boolean) {
  enabled[key] = checked
  if (!checked) filters[key] = ''
  clearResults()
}
function setEquipmentText(value: string) {
  filters.equipmentNo = value.toUpperCase()
  enabled.equipmentNo = Boolean(value.trim())
  clearResults()
}
function openPicker(key: FilterKey) {
  pickerKey.value = key
  pickerSearch.value = ''
  stagedValues.clear()
  selectedValues(key).forEach((value) => stagedValues.add(value))
  pickerOpen.value = true
}
function toggleStaged(value: string, checked: boolean) { checked ? stagedValues.add(value.toUpperCase()) : stagedValues.delete(value.toUpperCase()) }
function applyPicker() {
  filters[pickerKey.value] = Array.from(stagedValues).join(',')
  enabled[pickerKey.value] = stagedValues.size > 0
  pickerOpen.value = false
  clearResults()
}
function clearResults() { resultRows.value = []; queriedRange.value = ''; queryError.value = ''; notice.value = '' }
function parseDate(text: string) { return new Date(`${text}T00:00:00`) }
function matchesFilter(key: FilterKey, value: string) {
  if (!enabled[key] || !filters[key].trim()) return true
  return selectedValues(key).has(value.toUpperCase())
}
function calculateRow(seed: EquipmentSeed, windowHours: number): SummaryRow {
  const breakdownHours = seed.breakdownShares.reduce((sum, share) => sum + windowHours * share, 0)
  const count = seed.breakdownShares.length
  const nonBreakdownHours = Math.max(0, windowHours - breakdownHours)
  return {
    areaNo: seed.areaNo,
    equipmentType: seed.equipmentType,
    equipmentNo: seed.equipmentNo,
    belongEquipmentNo: seed.belongEquipmentNo,
    chamberFlag: seed.chamberFlag,
    breakdownTimes: count,
    mttr: count ? breakdownHours / count : 0,
    mtbf: count ? nonBreakdownHours / count : nonBreakdownHours,
    availability: windowHours > 0 ? nonBreakdownHours / windowHours : null,
  }
}
function runQuery() {
  clearResults()
  const start = parseDate(startDate.value)
  const endDay = parseDate(endDate.value)
  const now = new Date()
  if (!startDate.value || !endDate.value || Number.isNaN(start.getTime()) || Number.isNaN(endDay.getTime())) { queryError.value = '請選擇有效的開始與結束日期。'; return }
  if (start.getTime() > now.getTime() || endDay.getTime() > now.getTime()) { queryError.value = '開始日期與結束日期不可晚於今天。'; return }
  if (start.getTime() > endDay.getTime()) { queryError.value = '開始日期不可晚於結束日期。'; return }
  const endExclusive = new Date(endDay)
  endExclusive.setDate(endExclusive.getDate() + 1)
  const effectiveEnd = endExclusive.getTime() > now.getTime() ? now : endExclusive
  const windowHours = (effectiveEnd.getTime() - start.getTime()) / 3_600_000
  if (windowHours <= 0) { queryError.value = '查詢區間沒有可計算的時間。'; return }

  resultRows.value = rowsSeed
    .filter((seed) => (showChamber.value || !seed.chamberFlag)
      && matchesFilter('areaNo', seed.areaNo)
      && matchesFilter('equipmentType', seed.equipmentType)
      && matchesFilter('equipmentNo', seed.equipmentNo))
    .map((seed) => calculateRow(seed, windowHours))
    .sort((left, right) => left.areaNo.localeCompare(right.areaNo) || left.equipmentType.localeCompare(right.equipmentType) || (left.belongEquipmentNo || left.equipmentNo).localeCompare(right.belongEquipmentNo || right.equipmentNo) || Number(left.chamberFlag) - Number(right.chamberFlag))
  queriedRange.value = `${startDate.value} 00:00 ～ ${endExclusive.toLocaleDateString('sv-SE')} 00:00${effectiveEnd.getTime() < endExclusive.getTime() ? `（計算至現在 ${now.toLocaleTimeString('zh-TW', { hour: '2-digit', minute: '2-digit', hour12: false })}）` : ''}`
  notice.value = resultRows.value.length ? `查詢完成：${resultRows.value.length} 筆設備。` : '查無符合條件的設備。'
}
function formatHours(value: number | null) { return value === null ? '—' : value.toFixed(2) }
function formatAvailability(value: number | null) { return value === null ? '—' : `${(value * 100).toFixed(2)}%` }
</script>

<template>
  <section class="mttr" aria-label="設備 MTTR 統計表 DEV 預覽">
    <header class="titlebar">
      <div><p class="eyebrow">RPT / 在製品 / RT412</p><h1>設備 MTTR 統計表</h1></div>
      <div class="title-actions"><button class="subtle" type="button" @click="showInfo = true">? 說明</button><button class="close" type="button" @click="emit('close')">關閉 ×</button></div>
    </header>

    <section class="filters" aria-label="查詢條件">
      <div class="filter-grid">
        <div class="pick-row">
          <label class="check-label"><input type="checkbox" :checked="enabled.areaNo" @change="setFilterEnabled('areaNo', ($event.target as HTMLInputElement).checked)"> Area No</label>
          <input class="readonly" :value="filters.areaNo" placeholder="未指定" readonly>
          <button class="picker-button" type="button" @click="openPicker('areaNo')">...</button>
        </div>
        <div class="pick-row">
          <label class="check-label"><input type="checkbox" :checked="enabled.equipmentType" @change="setFilterEnabled('equipmentType', ($event.target as HTMLInputElement).checked)"> Equipment Type</label>
          <input class="readonly" :value="filters.equipmentType" placeholder="未指定" readonly>
          <button class="picker-button" type="button" @click="openPicker('equipmentType')">...</button>
        </div>
        <div class="pick-row">
          <label class="check-label"><input type="checkbox" :checked="enabled.equipmentNo" @change="setFilterEnabled('equipmentNo', ($event.target as HTMLInputElement).checked)"> Equipment No</label>
          <input :value="filters.equipmentNo" placeholder="可輸入逗號分隔的設備編號" @input="setEquipmentText(($event.target as HTMLInputElement).value)">
          <button class="picker-button" type="button" @click="openPicker('equipmentNo')">...</button>
        </div>
        <div class="date-row"><label for="start-date">開始日期</label><input id="start-date" v-model="startDate" type="date" @change="clearResults"></div>
        <div class="date-row"><label for="end-date">結束日期</label><input id="end-date" v-model="endDate" type="date" @change="clearResults"></div>
        <label v-if="chamberManageEnabled" class="chamber-check"><input v-model="showChamber" type="checkbox" @change="clearResults"> 顯示 Chamber</label>
        <button class="query" type="button" @click="runQuery">查詢</button>
      </div>
      <p class="source-note">DEV 使用固定合成資料；ChamberManage 假設定為啟用。日期以整日查詢，今天的資料計算至目前時間。</p>
    </section>

    <div v-if="queryError" class="feedback error" role="alert">{{ queryError }}</div>
    <div v-else-if="notice" class="feedback" role="status">{{ notice }}</div>
    <div v-if="queriedRange" class="range-note">查詢期間：{{ queriedRange }} <span>｜{{ activeEquipmentCount }} 筆</span></div>

    <div class="grid-wrap">
      <table>
        <thead><tr>
          <th>Area No</th><th>Equipment Type</th><th>Equipment No</th><th v-if="showChamber">Chamber Type</th><th v-if="showChamber">Chamber No</th>
          <th class="numeric">Breakdown Times</th><th class="numeric">MTTR（小時）</th><th class="numeric">MTBF（小時）</th><th class="numeric">Availability</th>
        </tr></thead>
        <tbody v-if="resultRows.length">
          <tr v-for="row in resultRows" :key="row.equipmentNo">
            <td>{{ row.areaNo }}</td><td>{{ row.equipmentType }}</td><td>{{ row.chamberFlag ? row.belongEquipmentNo : row.equipmentNo }}</td>
            <td v-if="showChamber">{{ row.chamberFlag ? row.equipmentType : '—' }}</td><td v-if="showChamber">{{ row.chamberFlag ? row.equipmentNo : '—' }}</td>
            <td class="numeric">{{ row.breakdownTimes ?? '—' }}</td><td class="numeric">{{ formatHours(row.mttr) }}</td><td class="numeric">{{ formatHours(row.mtbf) }}</td><td class="numeric">{{ formatAvailability(row.availability) }}</td>
          </tr>
        </tbody>
        <tbody v-else><tr><td class="empty" :colspan="showChamber ? 9 : 7">尚未查詢</td></tr></tbody>
      </table>
    </div>

    <footer class="statusbar"><span>設備 MTTR 統計表 · DEV 假資料</span><span>月結 Save 按鈕依來源 Designer 預設隱藏</span></footer>

    <div v-if="pickerOpen" class="overlay" @click.self="pickerOpen = false">
      <section class="dialog picker" role="dialog" aria-modal="true" :aria-label="pickerTitle">
        <header><h2>{{ pickerTitle }}</h2><button type="button" aria-label="關閉" @click="pickerOpen = false">×</button></header>
        <input v-model="pickerSearch" class="search" type="search" placeholder="搜尋...">
        <div class="option-list"><label v-for="option in pickerOptions" :key="option.value"><input type="checkbox" :checked="stagedValues.has(option.value.toUpperCase())" @change="toggleStaged(option.value, ($event.target as HTMLInputElement).checked)"><span>{{ option.label }}</span></label><p v-if="!pickerOptions.length" class="empty-options">沒有符合項目</p></div>
        <footer><span>已選 {{ stagedValues.size }} 項</span><div><button class="subtle" type="button" @click="stagedValues.clear()">清除</button><button class="query" type="button" @click="applyPicker">套用</button></div></footer>
      </section>
    </div>

    <div v-if="showInfo" class="overlay" @click.self="showInfo = false">
      <section class="dialog info" role="dialog" aria-modal="true" aria-label="MTTR 統計說明">
        <header><h2>統計說明</h2><button type="button" aria-label="關閉" @click="showInfo = false">×</button></header>
        <ul><li><strong>Breakdown Times</strong>：查詢區間內故障狀態歷程筆數。</li><li><strong>MTTR</strong>：故障狀態時數總和 ÷ 故障次數。</li><li><strong>MTBF</strong>：非故障狀態時數總和 ÷ 故障次數；無故障時依來源公式顯示非故障總時數。</li><li><strong>Availability</strong>：非故障時數 ÷（非故障時數 + 故障時數）。</li><li>此頁只用 DEV 合成資料。正式的月結儲存呼叫已在來源程式中，但按鈕預設隱藏，本頁不寫入資料。</li></ul>
        <button class="query" type="button" @click="showInfo = false">關閉</button>
      </section>
    </div>
  </section>
</template>

<style scoped>
.mttr{box-sizing:border-box;min-height:0;height:100%;display:flex;flex-direction:column;gap:10px;padding:12px;background:#f7f5ee;color:#26251f;font:13px/1.4 "Segoe UI",Arial,sans-serif;overflow:auto}.titlebar{display:flex;justify-content:space-between;align-items:center;gap:12px;border-bottom:1px solid #d4d0c4;padding:0 0 9px}.eyebrow{margin:0 0 2px;color:#817b6b;font-size:11px;letter-spacing:.08em}.titlebar h1{margin:0;font-size:19px;font-weight:600}.title-actions{display:flex;gap:6px}.title-actions button,.picker-button,.query,.subtle,.close{min-height:28px;border:1px solid #aaa596;border-radius:3px;padding:4px 10px;background:#fff;color:#343229;font:inherit;cursor:pointer}.title-actions .close,.query{background:#3b3930;border-color:#3b3930;color:white}.filters{border:1px solid #d0ccbf;background:#efede5;padding:10px}.filter-grid{display:grid;grid-template-columns:minmax(340px,1fr) minmax(300px,1fr) minmax(340px,1fr);gap:8px 12px;align-items:center}.pick-row{display:grid;grid-template-columns:132px minmax(90px,1fr) 32px;gap:5px;align-items:center}.check-label,.chamber-check{display:flex;align-items:center;gap:6px;white-space:nowrap}.check-label input,.chamber-check input{accent-color:#526d63}.pick-row input,.date-row input,.search{box-sizing:border-box;min-width:0;height:29px;border:1px solid #c5c0b2;border-radius:2px;padding:4px 7px;background:#fff;color:#2d2c26;font:inherit}.pick-row input.readonly{background:#f8f7f2;color:#625f54}.picker-button{min-width:30px;padding:2px}.date-row{display:flex;align-items:center;gap:8px}.date-row label{width:132px;text-align:right}.date-row input{width:150px}.chamber-check{grid-column:2}.query{justify-self:end;min-width:74px}.source-note{margin:8px 0 0;color:#777365;font-size:11px}.feedback{padding:7px 9px;border-left:3px solid #697f68;background:#e8eee5;color:#435745}.feedback.error{border-color:#b65045;background:#f7e9e5;color:#8c342b}.range-note{font-weight:600;color:#4b493f}.range-note span{font-weight:400;color:#777365}.grid-wrap{min-height:170px;flex:1;overflow:auto;border:1px solid #c8c3b5;background:white}table{width:100%;border-collapse:collapse;white-space:nowrap}th,td{border-right:1px solid #d9d5c9;border-bottom:1px solid #ddd9ce;padding:7px 9px;text-align:left}th{position:sticky;top:0;z-index:1;background:#e7e4da;color:#444137;font-weight:600}tbody tr:nth-child(even){background:#f8f7f2}td.numeric,th.numeric{text-align:right}.empty{text-align:center!important;color:#827d70;padding:26px}.statusbar{display:flex;justify-content:space-between;border-top:1px solid #d4d0c4;padding-top:6px;color:#777365;font-size:11px}.overlay{position:fixed;inset:0;z-index:40;display:grid;place-items:center;padding:18px;background:#16150f66}.dialog{box-sizing:border-box;width:min(460px,100%);max-height:min(80vh,640px);display:flex;flex-direction:column;gap:10px;padding:14px;border:1px solid #9b9688;border-radius:4px;background:#f8f7f2;box-shadow:0 8px 30px #0003}.dialog header,.dialog footer{display:flex;justify-content:space-between;align-items:center;gap:10px}.dialog h2{margin:0;font-size:16px;font-weight:600}.dialog header button{width:27px;height:27px;border:1px solid #c4bfb1;background:white;border-radius:3px;font-size:18px;cursor:pointer}.search{width:100%}.option-list{min-height:100px;overflow:auto;border:1px solid #d2cec1;background:white}.option-list label{display:flex;gap:8px;align-items:center;padding:7px 9px;border-bottom:1px solid #eeeae0;cursor:pointer}.option-list label:hover{background:#f1efe7}.option-list input{accent-color:#526d63}.empty-options{padding:12px;color:#777365}.dialog footer{border-top:1px solid #dedace;padding-top:9px;color:#777365;font-size:12px}.dialog footer div{display:flex;gap:7px}.info{align-items:flex-start}.info ul{margin:0;padding-left:20px;line-height:1.65}.info li+li{margin-top:5px}.info>.query{align-self:flex-end}@media(max-width:1050px){.filter-grid{grid-template-columns:1fr 1fr}.pick-row{grid-template-columns:125px minmax(80px,1fr) 32px}.chamber-check{grid-column:1}.query{grid-column:2}}@media(max-width:680px){.titlebar{align-items:flex-start;flex-direction:column}.filter-grid{grid-template-columns:1fr}.pick-row{grid-template-columns:118px minmax(50px,1fr) 32px}.date-row label{width:118px}.chamber-check,.query{grid-column:1}.query{justify-self:start}.statusbar{align-items:flex-start;flex-direction:column}}
</style>
