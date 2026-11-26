<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

type StateKey = 'production' | 'idle' | 'waitMachine' | 'waitExamine' | 'maintain' | 'repair' | 'breakdown'
interface EquipmentRow {
  equipmentNo: string
  areaNo: string
  areaName: string
  equipmentType: string
  belongEquipmentNo: string
  moves: number
  states: Record<StateKey, number>
  children: EquipmentRow[]
}
interface EquipmentOption { equipmentNo: string; areaNo: string; areaName: string; equipmentType: string; isChild: boolean }

const emit = defineEmits<{ close: [] }>()
const states: { key: StateKey; name: string; color: string }[] = [
  { key: 'production', name: '生產', color: '#4f9c68' },
  { key: 'idle', name: '閒置', color: '#98a1a8' },
  { key: 'waitMachine', name: '待機', color: '#58a9c4' },
  { key: 'waitExamine', name: '待檢', color: '#e4b84b' },
  { key: 'maintain', name: '保養', color: '#8b75bd' },
  { key: 'repair', name: '維修', color: '#e18a48' },
  { key: 'breakdown', name: '故障', color: '#c85d5d' },
]

const sampleRows: EquipmentRow[] = [
  { equipmentNo: 'EQ-ASM-001', areaNo: 'AREA-A', areaName: '一廠組裝區', equipmentType: 'Bonder', belongEquipmentNo: '', moves: 1240, states: { production: 76.4, idle: 7.2, waitMachine: 4.1, waitExamine: 2.3, maintain: 3.0, repair: 1.0, breakdown: 2.0 }, children: [
    { equipmentNo: 'EQ-ASM-001-01', areaNo: 'AREA-A', areaName: '一廠組裝區', equipmentType: 'Bonder Chamber', belongEquipmentNo: 'EQ-ASM-001', moves: 510, states: { production: 81.2, idle: 5.2, waitMachine: 3.8, waitExamine: 2.4, maintain: 2.1, repair: 1.3, breakdown: 1.5 }, children: [] },
    { equipmentNo: 'EQ-ASM-001-02', areaNo: 'AREA-A', areaName: '一廠組裝區', equipmentType: 'Bonder Chamber', belongEquipmentNo: 'EQ-ASM-001', moves: 730, states: { production: 71.8, idle: 9.2, waitMachine: 4.4, waitExamine: 2.2, maintain: 3.8, repair: 0.7, breakdown: 2.7 }, children: [] },
  ] },
  { equipmentNo: 'EQ-ASM-002', areaNo: 'AREA-A', areaName: '一廠組裝區', equipmentType: 'Die Attach', belongEquipmentNo: '', moves: 980, states: { production: 68.2, idle: 11.8, waitMachine: 5.4, waitExamine: 3.1, maintain: 5.2, repair: 2.8, breakdown: 1.7 }, children: [
    { equipmentNo: 'EQ-ASM-002-01', areaNo: 'AREA-A', areaName: '一廠組裝區', equipmentType: 'Die Attach Chamber', belongEquipmentNo: 'EQ-ASM-002', moves: 980, states: { production: 68.2, idle: 11.8, waitMachine: 5.4, waitExamine: 3.1, maintain: 5.2, repair: 2.8, breakdown: 1.7 }, children: [] },
  ] },
  { equipmentNo: 'EQ-TST-001', areaNo: 'AREA-B', areaName: '二廠測試區', equipmentType: 'Handler', belongEquipmentNo: '', moves: 765, states: { production: 88.1, idle: 3.6, waitMachine: 2.4, waitExamine: 1.7, maintain: 1.8, repair: 0.6, breakdown: 0.5 }, children: [] },
  { equipmentNo: 'EQ-TST-002', areaNo: 'AREA-B', areaName: '二廠測試區', equipmentType: 'Tester', belongEquipmentNo: '', moves: 412, states: { production: 52.0, idle: 13.0, waitMachine: 8.2, waitExamine: 5.8, maintain: 4.0, repair: 6.0, breakdown: 3.0 }, children: [] },
  { equipmentNo: 'EQ-PKG-001', areaNo: 'AREA-C', areaName: '三廠包裝區', equipmentType: 'Packer', belongEquipmentNo: '', moves: 0, states: { production: 0, idle: 0, waitMachine: 0, waitExamine: 0, maintain: 0, repair: 0, breakdown: 0 }, children: [] },
]

const equipmentCatalog: EquipmentOption[] = sampleRows.flatMap((row) => [row, ...row.children]).map((row) => ({ equipmentNo: row.equipmentNo, areaNo: row.areaNo, areaName: row.areaName, equipmentType: row.equipmentType, isChild: Boolean(row.belongEquipmentNo) }))
const now = new Date()
const endDate = new Date(now.getFullYear(), now.getMonth(), now.getDate())
const startDate = new Date(endDate)
startDate.setDate(startDate.getDate() - 1)
function toLocalInput(date: Date) {
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T00:00`
}

const areaChecked = ref(false)
const equipmentChecked = ref(false)
const areaNo = ref('')
const equipmentNo = ref('')
const startTime = ref(toLocalInput(startDate))
const endTime = ref(toLocalInput(endDate))
const chamberManage = ref(false)
const hasSearched = ref(false)
const resultRows = ref<EquipmentRow[]>([])
const selectedNo = ref('')
const expanded = reactive(new Set<string>())
const modalKind = ref<'area' | 'equipment' | null>(null)
const modalSearch = ref('')
const stagedEquipment = reactive(new Set<string>())
const error = ref('')

const areas = computed(() => Array.from(new Map(sampleRows.map((row) => [row.areaNo, { areaNo: row.areaNo, areaName: row.areaName }])).values()))
const modalEquipment = computed(() => {
  const search = modalSearch.value.trim().toUpperCase()
  return equipmentCatalog.filter((item) => (chamberManage.value || !item.isChild) && (!areaNo.value || item.areaNo === areaNo.value) && (!search || `${item.equipmentNo} ${item.equipmentType} ${item.areaNo}`.toUpperCase().includes(search)))
})
const selectedRow = computed(() => {
  for (const parent of resultRows.value) {
    if (parent.equipmentNo === selectedNo.value) return parent
    const child = parent.children.find((item) => item.equipmentNo === selectedNo.value)
    if (child) return child
  }
  return null
})
const distribution = computed(() => {
  if (!selectedRow.value) return []
  const values = states.map((state) => ({ name: state.name, value: selectedRow.value?.states[state.key] ?? 0, color: state.color }))
  const loss = Math.max(0, 100 - values.reduce((sum, item) => sum + item.value, 0))
  if (loss > 0.005) values.push({ name: 'Loss', value: loss, color: '#34332e' })
  return values.filter((item) => item.value > 0)
})
const donutStyle = computed(() => {
  if (!distribution.value.length) return { background: '#e8e6dd' }
  let edge = 0
  const slices = distribution.value.map((item) => {
    const nextEdge = edge + item.value
    const slice = `${item.color} ${edge.toFixed(2)}% ${nextEdge.toFixed(2)}%`
    edge = nextEdge
    return slice
  })
  if (edge < 100) slices.push(`#e8e6dd ${edge}% 100%`)
  return { background: `conic-gradient(${slices.join(', ')})` }
})
const totalMoves = computed(() => resultRows.value.reduce((sum, row) => sum + row.moves, 0))

function setAreaChecked(value: boolean) {
  areaChecked.value = value
  if (!value) areaNo.value = ''
}
function setEquipmentChecked(value: boolean) {
  equipmentChecked.value = value
  if (!value) equipmentNo.value = ''
}
function setEquipmentText(value: string) {
  equipmentNo.value = value.toUpperCase()
  equipmentChecked.value = Boolean(equipmentNo.value.trim())
}
function openAreaPicker() { modalKind.value = 'area'; modalSearch.value = '' }
function chooseArea(area: { areaNo: string }) {
  areaNo.value = area.areaNo
  areaChecked.value = true
  modalKind.value = null
}
function openEquipmentPicker() {
  stagedEquipment.clear()
  equipmentNo.value.split(',').map((value) => value.trim()).filter(Boolean).forEach((value) => stagedEquipment.add(value))
  modalKind.value = 'equipment'
  modalSearch.value = ''
}
function toggleStagedEquipment(value: string, checked: boolean) {
  checked ? stagedEquipment.add(value) : stagedEquipment.delete(value)
}
function applyEquipmentPicker() {
  equipmentNo.value = Array.from(stagedEquipment).join(',')
  equipmentChecked.value = Boolean(equipmentNo.value)
  modalKind.value = null
}
function toggleExpanded(row: EquipmentRow) {
  expanded.has(row.equipmentNo) ? expanded.delete(row.equipmentNo) : expanded.add(row.equipmentNo)
}
function selectRow(row: EquipmentRow) { selectedNo.value = row.equipmentNo }
function query() {
  error.value = ''
  if (!startTime.value || !endTime.value || new Date(startTime.value) >= new Date(endTime.value)) {
    error.value = '請確認起訖時間，StartTime 必須早於 EndTime。'
    return
  }
  if (areaChecked.value && !areaNo.value.trim()) {
    error.value = '已勾選 Area No，請選擇區域。'
    return
  }
  if (equipmentChecked.value && !equipmentNo.value.trim()) {
    error.value = '已勾選 Equipment No，請輸入或選取設備。'
    return
  }
  const equipmentSet = new Set(equipmentNo.value.split(',').map((value) => value.trim().toUpperCase()).filter(Boolean))
  resultRows.value = sampleRows.filter((row) => (!areaChecked.value || row.areaNo === areaNo.value) && (!equipmentChecked.value || equipmentSet.has(row.equipmentNo.toUpperCase()))).sort((a, b) => a.equipmentNo.localeCompare(b.equipmentNo))
  selectedNo.value = ''
  expanded.clear()
  hasSearched.value = true
}
function displayTime(value: string) {
  const date = new Date(value)
  return date.toLocaleString('zh-TW', { hour12: false })
}
</script>

<template>
  <main class="eqp-report" aria-label="設備稼動查詢 DEV">
    <header class="page-heading">
      <div><div class="eyebrow">RPT / RT410</div><h1>設備稼動查詢</h1><p>依設備狀態歷程統計區間占比，並彙總生產數量。</p></div>
      <span class="dev-badge">DEV 假資料</span>
    </header>

    <section class="query-panel" aria-label="查詢條件">
      <div class="filter-grid">
        <div class="filter-field"><label><input type="checkbox" :checked="areaChecked" @change="setAreaChecked(($event.target as HTMLInputElement).checked)"> Area No</label><div class="picker-input"><input :value="areaNo" readonly placeholder="選擇區域"><button type="button" aria-label="選擇區域" @click="openAreaPicker">…</button></div></div>
        <div class="filter-field equipment-filter"><label><input type="checkbox" :checked="equipmentChecked" @change="setEquipmentChecked(($event.target as HTMLInputElement).checked)"> Equipment No</label><div class="picker-input"><input :value="equipmentNo" placeholder="輸入多個設備編號，以逗號分隔" @input="setEquipmentText(($event.target as HTMLInputElement).value)" @blur="setEquipmentText(equipmentNo.trim())"><button type="button" aria-label="選擇設備" @click="openEquipmentPicker">…</button></div></div>
        <label class="date-field"><span>StartTime</span><input v-model="startTime" type="datetime-local"></label>
        <label class="date-field"><span>EndTime</span><input v-model="endTime" type="datetime-local"></label>
      </div>
      <div class="filter-actions">
        <label class="dev-setting"><input v-model="chamberManage" type="checkbox"> DEV 模擬 ChamberManage 子設備資料</label>
        <button type="button" class="query-button" @click="query">Query</button>
        <button type="button" class="close-button" @click="emit('close')">Close (X)</button>
      </div>
      <p v-if="error" class="error-message" role="alert">{{ error }}</p>
    </section>

    <section class="legend-panel" aria-label="設備狀態圖例">
      <div v-for="state in states" :key="state.key" class="legend-item"><span :style="{ background: state.color }"></span>{{ state.name }}</div>
      <div class="legend-item"><span class="loss-swatch"></span>Loss</div>
      <span class="range-label">{{ displayTime(startTime) }} 至 {{ displayTime(endTime) }}</span>
    </section>

    <section class="results-layout">
      <div class="table-panel">
        <div class="table-toolbar"><strong>設備稼動結果</strong><span>{{ resultRows.length }} 台設備 · Moves {{ totalMoves.toLocaleString() }}</span></div>
        <div class="table-scroll">
          <table>
            <thead><tr><th class="equipment-col">Equipment No</th><th class="moves-col">Moves</th><th v-if="chamberManage" class="moves-col">LotQty_EQP</th><th v-for="state in states" :key="state.key" class="state-col">{{ state.name }} %</th></tr></thead>
            <tbody v-if="resultRows.length">
              <template v-for="row in resultRows" :key="row.equipmentNo">
                <tr :class="{ selected: selectedNo === row.equipmentNo }" @click="selectRow(row)">
                  <td class="equipment-cell"><button v-if="chamberManage && row.children.length" type="button" class="expand-button" :aria-label="expanded.has(row.equipmentNo) ? '收合子設備' : '展開子設備'" @click.stop="toggleExpanded(row)">{{ expanded.has(row.equipmentNo) ? '−' : '+' }}</button><span>{{ row.equipmentNo }}</span></td>
                  <td class="numeric">{{ row.moves.toLocaleString() }}</td><td v-if="chamberManage" class="numeric">{{ row.moves.toLocaleString() }}</td>
                  <td v-for="state in states" :key="state.key" class="numeric state-value">{{ row.states[state.key].toFixed(2) }}%</td>
                </tr>
                <tr v-for="child in (chamberManage && expanded.has(row.equipmentNo) ? row.children : [])" :key="child.equipmentNo" :class="['child-row', { selected: selectedNo === child.equipmentNo }]" @click="selectRow(child)">
                  <td class="equipment-cell"><span class="child-marker">↳</span>{{ child.equipmentNo }}</td><td class="numeric">{{ child.moves.toLocaleString() }}</td><td v-if="chamberManage" class="numeric">{{ child.moves.toLocaleString() }}</td>
                  <td v-for="state in states" :key="state.key" class="numeric state-value">{{ child.states[state.key].toFixed(2) }}%</td>
                </tr>
              </template>
            </tbody>
            <tbody v-else><tr><td :colspan="2 + states.length + (chamberManage ? 1 : 0)" class="empty-state">{{ hasSearched ? '查無符合設備。' : '設定條件後按 Query。' }}</td></tr></tbody>
          </table>
        </div>
        <footer class="table-footer">狀態占比由期間內設備狀態秒數 ÷ 查詢區間秒數計算；未涵蓋時段以 Loss 顯示於明細。</footer>
      </div>

      <aside class="detail-panel" aria-label="設備狀態明細">
        <template v-if="selectedRow">
          <div class="detail-heading"><div><span class="eyebrow">EQUIPMENT STATE</span><h2>{{ selectedRow.equipmentNo }}</h2><p>{{ selectedRow.areaName }} · {{ selectedRow.equipmentType }}</p></div><span class="moves-pill">Moves {{ selectedRow.moves.toLocaleString() }}</span></div>
          <div v-if="distribution.length" class="chart-area"><div class="donut" :style="donutStyle"><div class="donut-hole"><strong>{{ (selectedRow.states.production || 0).toFixed(1) }}%</strong><small>生產占比</small></div></div><div class="chart-legend"><div v-for="part in distribution" :key="part.name"><span :style="{ background: part.color }"></span><b>{{ part.name }}</b><strong>{{ part.value.toFixed(2) }}%</strong></div></div></div>
          <div v-else class="no-data-chart"><strong>NoData</strong><span>此設備於選取區間內沒有狀態資料。</span></div>
          <div class="detail-note">點選其他設備可切換狀態圓餅明細。狀態色彩與欄位依設備狀態主檔動態定義。</div>
        </template>
        <div v-else class="detail-empty"><span class="empty-icon">◷</span><strong>選取設備查看狀態明細</strong><p>查詢後點選結果列，即可查看所選區間的狀態占比。</p></div>
      </aside>
    </section>

    <div v-if="modalKind" class="modal-backdrop" @click.self="modalKind = null">
      <section class="picker-modal" role="dialog" aria-modal="true" :aria-label="modalKind === 'area' ? '選擇區域' : '選擇設備'">
        <header><div><span class="eyebrow">MASTER LOOKUP</span><h2>{{ modalKind === 'area' ? '選擇 Area No' : '選擇 Equipment No' }}</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="modalKind = null">×</button></header>
        <input v-model="modalSearch" class="modal-search" type="search" :placeholder="modalKind === 'area' ? '搜尋區域編號或名稱' : '搜尋設備編號、類別或區域'">
        <div class="picker-list">
          <button v-if="modalKind === 'area'" v-for="area in areas.filter((item) => `${item.areaNo} ${item.areaName}`.toUpperCase().includes(modalSearch.trim().toUpperCase()))" :key="area.areaNo" type="button" class="lookup-option" @click="chooseArea(area)"><strong>{{ area.areaNo }}</strong><span>{{ area.areaName }}</span></button>
          <label v-else v-for="item in modalEquipment" :key="item.equipmentNo" class="lookup-option equipment-option"><input type="checkbox" :checked="stagedEquipment.has(item.equipmentNo)" @change="toggleStagedEquipment(item.equipmentNo, ($event.target as HTMLInputElement).checked)"><strong>{{ item.equipmentNo }}</strong><span>{{ item.equipmentType }} · {{ item.areaName }}</span></label>
          <p v-if="modalKind === 'area' && !areas.some((item) => `${item.areaNo} ${item.areaName}`.toUpperCase().includes(modalSearch.trim().toUpperCase()))" class="empty-picker">沒有符合的區域。</p>
          <p v-if="modalKind === 'equipment' && !modalEquipment.length" class="empty-picker">沒有符合的設備。</p>
        </div>
        <footer v-if="modalKind === 'equipment'"><span>已選 {{ stagedEquipment.size }} 台</span><button type="button" class="query-button" @click="applyEquipmentPicker">套用選取</button></footer>
      </section>
    </div>
  </main>
</template>

<style scoped>
.eqp-report{box-sizing:border-box;height:100%;min-height:0;overflow:auto;padding:16px;background:#f4f2eb;color:#282720;font:12px/1.45 Arial,"Microsoft JhengHei",sans-serif}.page-heading{display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:12px}.eyebrow{display:block;color:#827d6d;font-size:9px;font-weight:700;letter-spacing:.12em}.page-heading h1{margin:3px 0;font-size:20px}.page-heading p{margin:0;color:#777366}.dev-badge{padding:4px 8px;border:1px solid #d8c98d;border-radius:12px;background:#f8edc5;color:#715c15;font-size:10px;font-weight:700}.query-panel,.legend-panel,.table-panel,.detail-panel{border:1px solid #d4d1c5;background:#fffefa;box-shadow:0 1px 2px #26251f0a}.query-panel{padding:12px}.filter-grid{display:grid;grid-template-columns:minmax(250px,1fr) minmax(340px,1.4fr) minmax(215px,.9fr) minmax(215px,.9fr);gap:9px 12px;align-items:end}.filter-field>label,.date-field>span{display:flex;align-items:center;gap:5px;margin-bottom:4px;color:#565347;font-size:10px;font-weight:700}.filter-field input[type=checkbox],.dev-setting input{accent-color:#3c6a54}.picker-input{display:flex;min-width:0}.picker-input input,.date-field input,.modal-search{box-sizing:border-box;width:100%;height:29px;padding:4px 7px;border:1px solid #cbc8bc;background:#fff;color:#292820;font:inherit}.picker-input button{width:31px;border:1px solid #cbc8bc;border-left:0;background:#eeeae0;font:inherit}.filter-actions{display:flex;align-items:center;gap:7px;margin-top:10px}.dev-setting{display:flex;align-items:center;gap:5px;margin-right:auto;color:#706c5e;font-size:10px}.query-button,.close-button{min-height:29px;padding:4px 13px;border:1px solid #3c6a54;border-radius:3px;background:#3c6a54;color:#fff;font:inherit;font-weight:700;cursor:pointer}.close-button{border-color:#777366;background:#fffefa;color:#4e4a40}.error-message{margin:9px 0 0;color:#b03939}.legend-panel{display:flex;align-items:center;flex-wrap:wrap;gap:8px 13px;margin:9px 0;padding:8px 10px}.legend-item{display:flex;align-items:center;gap:5px;color:#5b584d;font-size:10px}.legend-item>span,.chart-legend span{width:11px;height:11px;border-radius:2px}.loss-swatch{background:#34332e}.range-label{margin-left:auto;color:#777366;font-size:10px}.results-layout{display:grid;grid-template-columns:minmax(0,1.8fr) minmax(290px,.9fr);gap:10px;min-height:330px}.table-panel,.detail-panel{min-width:0}.table-toolbar{display:flex;justify-content:space-between;align-items:center;padding:9px 10px;border-bottom:1px solid #e0ddd2}.table-toolbar strong{font-size:12px}.table-toolbar span{color:#777366;font-size:10px}.table-scroll{overflow:auto;max-height:calc(100vh - 345px)}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:10px}th{position:sticky;top:0;z-index:1;padding:7px 6px;background:#eeece4;color:#514e43;text-align:right;font-weight:700}th:first-child{text-align:left}td{padding:7px 6px;border-top:1px solid #eeece6;color:#3b3932}tbody tr{cursor:pointer}tbody tr:hover{background:#f7f5ed}tbody tr.selected{background:#e8efe8}.equipment-col{min-width:144px}.moves-col{min-width:72px}.state-col{min-width:70px}.numeric{text-align:right;font-variant-numeric:tabular-nums}.state-value{color:#5b584d}.equipment-cell{font-weight:700}.expand-button{width:19px;height:19px;margin-right:4px;border:1px solid #d1cec3;border-radius:2px;background:#faf9f5;color:#454238;cursor:pointer}.child-row{background:#faf9f5}.child-row .equipment-cell{padding-left:24px;font-weight:500}.child-marker{margin-right:5px;color:#989384}.empty-state{height:120px;text-align:center;color:#827d6d}.table-footer{padding:7px 9px;border-top:1px solid #e4e1d7;color:#777366;font-size:9px}.detail-panel{padding:12px}.detail-heading{display:flex;justify-content:space-between;gap:8px}.detail-heading h2{margin:3px 0;font-size:15px}.detail-heading p{margin:0;color:#777366;font-size:10px}.moves-pill{height:max-content;padding:4px 7px;border-radius:12px;background:#eeeae0;color:#555144;font-size:9px}.chart-area{display:flex;align-items:center;justify-content:center;gap:16px;padding:19px 0}.donut{display:grid;flex:0 0 142px;width:142px;height:142px;place-items:center;border-radius:50%}.donut-hole{display:flex;width:88px;height:88px;flex-direction:column;align-items:center;justify-content:center;border-radius:50%;background:#fffefa}.donut-hole strong{font-size:18px}.donut-hole small{color:#777366;font-size:9px}.chart-legend{display:grid;gap:5px;min-width:106px}.chart-legend>div{display:grid;grid-template-columns:12px 1fr auto;align-items:center;gap:5px;font-size:9px}.chart-legend strong{font-variant-numeric:tabular-nums}.no-data-chart{display:flex;min-height:195px;flex-direction:column;align-items:center;justify-content:center;gap:5px;color:#777366}.no-data-chart strong{font-size:16px;color:#827d6d}.detail-note{padding-top:8px;border-top:1px solid #e7e4da;color:#777366;font-size:9px}.detail-empty{display:flex;height:100%;min-height:230px;flex-direction:column;align-items:center;justify-content:center;text-align:center;color:#777366}.detail-empty strong{margin-top:8px;color:#565347}.detail-empty p{max-width:210px;margin:5px 0;font-size:10px}.empty-icon{font-size:28px;color:#a49f8d}.modal-backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:20px;background:#201f1bcc}.picker-modal{display:flex;width:min(510px,100%);max-height:min(620px,90vh);flex-direction:column;border:1px solid #c9c5b8;background:#fffefa;box-shadow:0 16px 55px #0005}.picker-modal>header,.picker-modal>footer{display:flex;align-items:center;justify-content:space-between;padding:12px;border-bottom:1px solid #e1ded4}.picker-modal>header h2{margin:3px 0 0;font-size:15px}.picker-modal>footer{border-top:1px solid #e1ded4;border-bottom:0;color:#777366}.icon-button{width:28px;height:28px;border:0;background:transparent;font-size:22px;cursor:pointer}.modal-search{margin:10px 12px;width:calc(100% - 24px)}.picker-list{overflow:auto;min-height:100px;padding:0 12px}.lookup-option{display:flex;width:100%;min-height:36px;align-items:center;gap:9px;padding:6px 8px;border:0;border-bottom:1px solid #efede6;background:#fffefa;color:#514e43;text-align:left;font:inherit;cursor:pointer}.lookup-option:hover{background:#f3f1e9}.lookup-option strong{min-width:116px;color:#343229}.lookup-option span{color:#777366}.equipment-option{box-sizing:border-box}.equipment-option input{accent-color:#3c6a54}.empty-picker{padding:25px;text-align:center;color:#827d6d}
@media(max-width:1050px){.filter-grid{grid-template-columns:repeat(2,minmax(220px,1fr))}.results-layout{grid-template-columns:1fr}.detail-panel{min-height:250px}.table-scroll{max-height:420px}}
@media(max-width:620px){.eqp-report{padding:9px}.filter-grid{grid-template-columns:1fr}.filter-actions{flex-wrap:wrap}.dev-setting{width:100%;margin:0 0 4px}.range-label{width:100%;margin:0}.chart-area{flex-wrap:wrap}.page-heading h1{font-size:17px}}
</style>
