<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, reactive, ref } from 'vue'

interface StateDefinition { key: string; name: string; color: string }
interface StatePercent { equipmentState: number; percent: number; seconds: number }
interface ApiState { equipmentState: number; stateName: string; stateColorHex: string }
interface ApiRow {
  equipmentNo: string; equipmentType: string; equipmentName: string | null; areaNo: string | null
  belongEquipmentNo: string | null; isChamber: boolean; moves: number; lotQtyEqp: number
  hasStateData: boolean; lossPercentage: number; statePercentages: StatePercent[]; children: ApiRow[]
}
interface EquipmentRow extends ApiRow { areaName: string; states: Record<string, number> }
interface AreaOption { areaNo: string; areaName: string }
interface EquipmentOption { equipmentNo: string; equipmentType: string; equipmentName: string }
interface ApiResponse { startTime: string; endTime: string; chamberManage: boolean; states: ApiState[]; items: ApiRow[] }

const props = defineProps<{ apiBaseUrl: string }>()
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const now = new Date()
const endDate = new Date(now.getFullYear(), now.getMonth(), now.getDate())
const startDate = new Date(endDate)
startDate.setDate(startDate.getDate() - 1)
const areaChecked = ref(false)
const equipmentChecked = ref(false)
const areaNo = ref('')
const areaName = ref('')
const equipmentNo = ref('')
const startTime = ref(toLocalInput(startDate))
const endTime = ref(toLocalInput(endDate))
const chamberManage = ref(false)
const hasSearched = ref(false)
const loading = ref(false)
const resultRows = ref<EquipmentRow[]>([])
const selectedNo = ref('')
const expanded = reactive(new Set<string>())
const modalKind = ref<'area' | 'equipment' | null>(null)
const modalSearch = ref('')
const areaOptions = ref<AreaOption[]>([])
const equipmentOptions = ref<EquipmentOption[]>([])
const stagedEquipment = reactive(new Set<string>())
const lookupLoading = ref(false)
const lookupError = ref('')
const error = ref('')
const stateWarning = ref('')
const states = ref<StateDefinition[]>([])
const areaController = ref<AbortController | null>(null)
const equipmentController = ref<AbortController | null>(null)
const reportController = ref<AbortController | null>(null)

function toLocalInput(date: Date): string {
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}
function apiUrl(path: string, query: URLSearchParams): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  const url = new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin))
  query.forEach((value, key) => url.searchParams.set(key, value))
  return url.toString()
}
async function requestJson<T>(path: string, query = new URLSearchParams(), signal?: AbortSignal): Promise<T> {
  const response = await fetch(apiUrl(path, query), { credentials: 'include', headers: { Accept: 'application/json' }, cache: 'no-store', signal })
  if (response.status === 401) { window.location.assign(appBase + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  const payload = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
  if (!response.ok) throw new Error(payload?.detail || payload?.message || payload?.title || `API 回應錯誤 (${response.status})。`)
  return payload as unknown as T
}
function rowsFrom<T>(payload: unknown): T[] {
  if (Array.isArray(payload)) return payload as T[]
  if (typeof payload === 'object' && payload !== null && 'items' in payload && Array.isArray(payload.items)) return payload.items as T[]
  return []
}
function colorFrom(value: string | null | undefined): string { return value && /^#?[0-9a-f]{6}$/i.test(value) ? (value.startsWith('#') ? value : `#${value}`) : '#858173' }
function mapStates(rows: ApiState[]): StateDefinition[] {
  return rows.map(row => ({ key: String(row.equipmentState), name: row.stateName, color: colorFrom(row.stateColorHex) }))
}
function mapRow(row: ApiRow): EquipmentRow {
  const percentages: Record<string, number> = {}
  for (const item of row.statePercentages ?? []) percentages[String(item.equipmentState)] = Number(item.percent) || 0
  return {
    ...row,
    areaName: areaOptions.value.find(area => area.areaNo === row.areaNo)?.areaName ?? row.areaNo ?? '未分類區域',
    states: percentages,
    children: (row.children ?? []).map(mapRow),
  }
}
const modalTitle = computed(() => modalKind.value === 'area' ? '選擇 Area No' : '選擇 Equipment No')
const visibleAreas = computed(() => {
  const search = modalSearch.value.trim().toUpperCase()
  return areaOptions.value.filter(item => !search || `${item.areaNo} ${item.areaName}`.toUpperCase().includes(search))
})
const modalEquipment = computed(() => {
  const search = modalSearch.value.trim().toUpperCase()
  return equipmentOptions.value.filter(item => !search || `${item.equipmentNo} ${item.equipmentType} ${item.equipmentName}`.toUpperCase().includes(search))
})
const selectedRow = computed(() => {
  for (const parent of resultRows.value) {
    if (parent.equipmentNo === selectedNo.value) return parent
    const child = parent.children.find(item => item.equipmentNo === selectedNo.value)
    if (child) return child
  }
  return null
})
const distribution = computed(() => {
  if (!selectedRow.value) return []
  const values = states.value.map(state => ({ name: state.name, value: selectedRow.value?.states[state.key] ?? 0, color: state.color }))
  if (selectedRow.value.lossPercentage > 0.005) values.push({ name: 'Loss', value: selectedRow.value.lossPercentage, color: '#34332e' })
  return values.filter(item => item.value > 0)
})
const donutStyle = computed(() => {
  if (!distribution.value.length) return { background: '#e8e6dd' }
  let edge = 0
  const slices = distribution.value.map(item => {
    const nextEdge = edge + item.value
    const slice = `${item.color} ${edge.toFixed(2)}% ${nextEdge.toFixed(2)}%`
    edge = nextEdge
    return slice
  })
  if (edge < 100) slices.push(`#e8e6dd ${edge}% 100%`)
  return { background: `conic-gradient(${slices.join(', ')})` }
})
const classifiedPercent = computed(() => selectedRow.value ? Math.max(0, 100 - selectedRow.value.lossPercentage) : 0)
const totalMoves = computed(() => resultRows.value.reduce((sum, row) => sum + Number(row.moves || 0), 0))
function formatNumber(value: number): string { return Number(value || 0).toLocaleString('zh-TW', { maximumFractionDigits: 2 }) }
function formatTime(value: string): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.valueOf()) ? value : date.toLocaleString('zh-TW', { hour12: false })
}
function setAreaChecked(value: boolean): void { areaChecked.value = value; if (!value) { areaNo.value = ''; areaName.value = '' } }
function setEquipmentChecked(value: boolean): void { equipmentChecked.value = value; if (!value) equipmentNo.value = '' }
function setEquipmentText(value: string): void { equipmentNo.value = value.toUpperCase(); equipmentChecked.value = Boolean(equipmentNo.value.trim()) }
function openAreaPicker(): void { modalKind.value = 'area'; modalSearch.value = ''; areaOptions.value = []; lookupError.value = '' }
function openEquipmentPicker(): void {
  stagedEquipment.clear()
  equipmentNo.value.split(',').map(value => value.trim()).filter(Boolean).forEach(value => stagedEquipment.add(value))
  modalKind.value = 'equipment'; modalSearch.value = ''; equipmentOptions.value = []; lookupError.value = ''
}
function closePicker(): void {
  areaController.value?.abort(); equipmentController.value?.abort(); areaController.value = null; equipmentController.value = null
  modalKind.value = null; lookupLoading.value = false
}
async function searchPicker(): Promise<void> {
  lookupError.value = ''
  if (modalKind.value === 'area') {
    const search = modalSearch.value.trim()
    if (search.length < 2) { lookupError.value = '請輸入至少 2 個字元搜尋區域。'; areaOptions.value = []; return }
    areaController.value?.abort()
    const controller = new AbortController(); areaController.value = controller; lookupLoading.value = true
    try {
      const query = new URLSearchParams({ areaNo: search, issueState: '2' })
      const rows = rowsFrom<AreaOption>(await requestJson<unknown>('api/smd/areas', query, controller.signal))
      if (areaController.value === controller) areaOptions.value = rows.slice(0, 200)
    } catch (cause) {
      if (!controller.signal.aborted) { lookupError.value = cause instanceof Error ? cause.message : '區域 lookup 失敗。'; areaOptions.value = [] }
    } finally { if (areaController.value === controller) { areaController.value = null; lookupLoading.value = false } }
    return
  }
  if (modalKind.value !== 'equipment') return
  const search = modalSearch.value.trim()
  if (search.length < 3) { lookupError.value = '請輸入至少 3 個字元搜尋設備。'; equipmentOptions.value = []; return }
  equipmentController.value?.abort()
  const controller = new AbortController(); equipmentController.value = controller; lookupLoading.value = true
  try {
    const query = new URLSearchParams({ keyword: search, issueState: '2' })
    const rows = rowsFrom<EquipmentOption>(await requestJson<unknown>('api/equipment', query, controller.signal))
    if (equipmentController.value === controller) equipmentOptions.value = rows.slice(0, 200)
  } catch (cause) {
    if (!controller.signal.aborted) { lookupError.value = cause instanceof Error ? cause.message : '設備 lookup 失敗。'; equipmentOptions.value = [] }
  } finally { if (equipmentController.value === controller) { equipmentController.value = null; lookupLoading.value = false } }
}
function chooseArea(area: AreaOption): void { areaNo.value = area.areaNo; areaName.value = area.areaName; areaChecked.value = true; closePicker() }
function toggleStagedEquipment(value: string, checked: boolean): void { checked ? stagedEquipment.add(value) : stagedEquipment.delete(value) }
function applyEquipmentPicker(): void {
  equipmentNo.value = Array.from(stagedEquipment).join(',')
  equipmentChecked.value = Boolean(equipmentNo.value)
  closePicker()
}
function toggleExpanded(row: EquipmentRow): void { expanded.has(row.equipmentNo) ? expanded.delete(row.equipmentNo) : expanded.add(row.equipmentNo) }
function selectRow(row: EquipmentRow): void { selectedNo.value = row.equipmentNo }
async function loadStates(): Promise<void> {
  try {
    const rows = rowsFrom<ApiState>(await requestJson<unknown>('api/equipment/states'))
    states.value = mapStates(rows)
  } catch (cause) { stateWarning.value = cause instanceof Error ? cause.message : '設備狀態 lookup 失敗；查詢回應仍會提供狀態欄位。' }
}
async function query(): Promise<void> {
  error.value = ''
  if (!startTime.value || !endTime.value) { error.value = 'StartTime 與 EndTime 為必填。'; return }
  const start = new Date(startTime.value).getTime(); const end = new Date(endTime.value).getTime()
  if (!Number.isFinite(start) || !Number.isFinite(end) || start >= end) { error.value = '請確認起訖時間，StartTime 必須早於 EndTime。'; return }
  if (end > Date.now()) { error.value = 'EndTime 不可晚於目前時間。'; return }
  if (end - start > 31 * 24 * 60 * 60 * 1000) { error.value = '查詢區間不可超過 31 天。'; return }
  if (areaChecked.value && !areaNo.value.trim()) { error.value = '已勾選 Area No，請選擇區域。'; return }
  const equipmentList = [...new Set(equipmentNo.value.split(',').map(value => value.trim().toUpperCase()).filter(Boolean))]
  if (equipmentChecked.value && !equipmentList.length) { error.value = '已勾選 Equipment No，請輸入或選取設備。'; return }
  if (equipmentList.length > 100) { error.value = 'Equipment No 最多可選 100 台。'; return }
  reportController.value?.abort()
  const controller = new AbortController(); reportController.value = controller; loading.value = true; hasSearched.value = true
  resultRows.value = []; selectedNo.value = ''; expanded.clear()
  try {
    const query = new URLSearchParams({ startTime: startTime.value, endTime: endTime.value, chamberManage: String(chamberManage.value) })
    if (areaChecked.value) query.set('areaNos', areaNo.value.trim())
    if (equipmentChecked.value) query.set('equipmentNos', equipmentList.join(','))
    const response = await requestJson<ApiResponse>('api/reports/equipment-daily-utilization', query, controller.signal)
    if (reportController.value !== controller) return
    if (response.states?.length) states.value = mapStates(response.states)
    resultRows.value = (response.items ?? []).map(mapRow)
  } catch (cause) {
    if (!controller.signal.aborted) { error.value = cause instanceof Error ? cause.message : '設備稼動資料查詢失敗。'; resultRows.value = [] }
  } finally { if (reportController.value === controller) { reportController.value = null; loading.value = false } }
}
onMounted(() => { void loadStates() })
onBeforeUnmount(() => { reportController.value?.abort(); areaController.value?.abort(); equipmentController.value?.abort() })
</script>

<template>
  <main class="eqp-report" aria-label="設備稼動查詢">
    <header class="page-heading"><div><div class="eyebrow">RPT / RT410</div><h1>設備稼動查詢</h1><p>依設備狀態歷程統計區間占比，並彙總生產數量。</p></div><span class="live-badge">API 唯讀</span></header>
    <form class="query-panel" @submit.prevent="query">
      <div class="filter-grid">
        <div class="filter-field"><label><input type="checkbox" :checked="areaChecked" @change="setAreaChecked(($event.target as HTMLInputElement).checked)"> Area No</label><div class="picker-input"><input :value="areaName ? `${areaNo} · ${areaName}` : areaNo" readonly placeholder="選擇區域"><button type="button" aria-label="選擇區域" @click="openAreaPicker">…</button></div></div>
        <div class="filter-field equipment-filter"><label><input type="checkbox" :checked="equipmentChecked" @change="setEquipmentChecked(($event.target as HTMLInputElement).checked)"> Equipment No</label><div class="picker-input"><input :value="equipmentNo" placeholder="多個設備編號以逗號分隔" @input="setEquipmentText(($event.target as HTMLInputElement).value)" @blur="setEquipmentText(equipmentNo.trim())"><button type="button" aria-label="選擇設備" @click="openEquipmentPicker">…</button></div></div>
        <label class="date-field"><span>StartTime</span><input v-model="startTime" type="datetime-local" required></label>
        <label class="date-field"><span>EndTime</span><input v-model="endTime" type="datetime-local" required></label>
      </div>
      <div class="filter-actions"><label class="live-setting"><input v-model="chamberManage" type="checkbox"> ChamberManage（包含 Chamber 明細）</label><button type="submit" class="query-button" :disabled="loading">{{ loading ? '查詢中…' : 'Query' }}</button><button type="button" class="close-button" @click="emit('close')">Close (X)</button></div>
      <p v-if="error" class="error-message" role="alert">{{ error }}</p>
      <p v-if="stateWarning" class="warning-message" role="status">狀態 lookup 暫時不可用：{{ stateWarning }}</p>
    </form>
    <section class="legend-panel" aria-label="設備狀態圖例"><div v-for="state in states" :key="state.key" class="legend-item"><span :style="{ background: state.color }"></span>{{ state.name }}</div><div class="legend-item"><span class="loss-swatch"></span>Loss</div><span class="range-label">{{ formatTime(startTime) }} 至 {{ formatTime(endTime) }}</span></section>
    <section class="results-layout">
      <div class="table-panel"><div class="table-toolbar"><strong>設備稼動結果</strong><span>{{ loading ? '載入中…' : `${resultRows.length} 台設備 · Moves ${formatNumber(totalMoves)}` }}</span></div>
        <div class="table-scroll"><table><thead><tr><th class="equipment-col">Equipment No</th><th class="moves-col">Moves</th><th v-if="chamberManage" class="moves-col">LotQty_EQP</th><th v-for="state in states" :key="state.key" class="state-col">{{ state.name }} %</th></tr></thead>
          <tbody v-if="resultRows.length"><template v-for="row in resultRows" :key="row.equipmentNo"><tr :class="{ selected: selectedNo === row.equipmentNo }" @click="selectRow(row)"><td class="equipment-cell"><button v-if="chamberManage && row.children.length" type="button" class="expand-button" :aria-label="expanded.has(row.equipmentNo) ? '收合子設備' : '展開子設備'" @click.stop="toggleExpanded(row)">{{ expanded.has(row.equipmentNo) ? '−' : '+' }}</button><span>{{ row.equipmentNo }}</span></td><td class="numeric">{{ formatNumber(row.moves) }}</td><td v-if="chamberManage" class="numeric">{{ formatNumber(row.lotQtyEqp) }}</td><td v-for="state in states" :key="state.key" class="numeric state-value">{{ (row.states[state.key] ?? 0).toFixed(2) }}%</td></tr>
            <tr v-for="child in (chamberManage && expanded.has(row.equipmentNo) ? row.children : [])" :key="child.equipmentNo" :class="['child-row', { selected: selectedNo === child.equipmentNo }]" @click="selectRow(child)"><td class="equipment-cell"><span class="child-marker">↳</span>{{ child.equipmentNo }}</td><td class="numeric">{{ formatNumber(child.moves) }}</td><td v-if="chamberManage" class="numeric">{{ formatNumber(child.lotQtyEqp) }}</td><td v-for="state in states" :key="state.key" class="numeric state-value">{{ (child.states[state.key] ?? 0).toFixed(2) }}%</td></tr>
          </template></tbody><tbody v-else><tr><td :colspan="2 + states.length + (chamberManage ? 1 : 0)" class="empty-state">{{ error ? '查詢失敗' : loading ? '載入中…' : hasSearched ? '查無符合設備。' : '設定條件後按 Query。' }}</td></tr></tbody>
        </table></div><footer class="table-footer">狀態占比以區間內設備狀態秒數計算，未涵蓋時段由 Loss 顯示於明細。設備數超過 API 上限時請縮小條件。</footer>
      </div>
      <aside class="detail-panel" aria-label="設備狀態明細"><template v-if="selectedRow"><div class="detail-heading"><div><span class="eyebrow">EQUIPMENT STATE</span><h2>{{ selectedRow.equipmentNo }}</h2><p>{{ selectedRow.areaName }} · {{ selectedRow.equipmentType }}</p></div><span class="moves-pill">Moves {{ formatNumber(selectedRow.moves) }}</span></div>
          <div v-if="selectedRow.hasStateData && distribution.length" class="chart-area"><div class="donut" :style="donutStyle"><div class="donut-hole"><strong>{{ classifiedPercent.toFixed(1) }}%</strong><small>已分類</small></div></div><div class="chart-legend"><div v-for="part in distribution" :key="part.name"><span :style="{ background: part.color }"></span><b>{{ part.name }}</b><strong>{{ part.value.toFixed(2) }}%</strong></div></div></div><div v-else class="no-data-chart"><strong>NoData</strong><span>此設備於選取區間內沒有狀態資料。</span></div><div class="detail-note">狀態名稱與色彩來自設備狀態主檔；區間未覆蓋比例依 API 回傳的 Loss 顯示。</div>
        </template><div v-else class="detail-empty"><span class="empty-icon">◷</span><strong>選取設備查看狀態明細</strong><p>查詢後點選結果列，即可查看所選區間的狀態占比。</p></div></aside>
    </section>
    <div v-if="modalKind" class="modal-backdrop" @click.self="closePicker"><section class="picker-modal" role="dialog" aria-modal="true" :aria-label="modalTitle"><header><div><span class="eyebrow">MASTER LOOKUP</span><h2>{{ modalTitle }}</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="closePicker">×</button></header>
        <form class="picker-search" @submit.prevent="searchPicker"><input v-model="modalSearch" class="modal-search" type="search" :placeholder="modalKind === 'area' ? '搜尋區域編號或名稱（至少 2 字）' : '搜尋設備編號或名稱（至少 3 字）'"><button type="submit" :disabled="lookupLoading">{{ lookupLoading ? '搜尋中…' : '搜尋' }}</button></form>
        <p v-if="lookupError" class="lookup-error" role="alert">{{ lookupError }}</p><div class="picker-list"><template v-if="modalKind === 'area'"><button v-for="area in visibleAreas" :key="area.areaNo" type="button" class="lookup-option" @click="chooseArea(area)"><strong>{{ area.areaNo }}</strong><span>{{ area.areaName }}</span></button><p v-if="!lookupLoading && !lookupError && !visibleAreas.length" class="empty-picker">輸入區域條件後搜尋。</p></template>
          <template v-else><label v-for="item in modalEquipment" :key="item.equipmentNo" class="lookup-option equipment-option"><input type="checkbox" :checked="stagedEquipment.has(item.equipmentNo)" @change="toggleStagedEquipment(item.equipmentNo, ($event.target as HTMLInputElement).checked)"><strong>{{ item.equipmentNo }}</strong><span>{{ item.equipmentName }} · {{ item.equipmentType }}</span></label><p v-if="!lookupLoading && !lookupError && !modalEquipment.length" class="empty-picker">輸入設備條件後搜尋。</p></template></div>
        <footer v-if="modalKind === 'equipment'"><span>已選 {{ stagedEquipment.size }} 台</span><button type="button" class="query-button" @click="applyEquipmentPicker">套用選取</button></footer>
      </section></div>
  </main>
</template>

<style scoped>
.eqp-report{box-sizing:border-box;display:flex;flex-direction:column;gap:9px;min-height:0;height:100%;padding:12px;overflow:auto;background:#f3f2ed;color:#292820;font:inherit}.page-heading{display:flex;justify-content:space-between;gap:12px}.eyebrow{color:#777366;font-size:9px;letter-spacing:.1em}.page-heading h1{margin:2px 0;font-size:18px}.page-heading p{margin:0;color:#777366;font-size:10px}.live-badge{height:max-content;padding:4px 8px;border:1px solid #b9d0bf;border-radius:12px;background:#e5f0e7;color:#285c43;font-size:10px;font-weight:700}.query-panel,.legend-panel,.table-panel,.detail-panel{border:1px solid #d4d1c5;background:#fffefa;box-shadow:0 1px 2px #26251f0a}.query-panel{padding:10px}.filter-grid{display:grid;grid-template-columns:minmax(220px,1fr) minmax(300px,1.4fr) minmax(190px,.9fr) minmax(190px,.9fr);gap:9px 11px;align-items:end}.filter-field>label,.date-field>span{display:flex;align-items:center;gap:5px;margin-bottom:4px;color:#565347;font-size:10px;font-weight:700}.filter-field input[type=checkbox],.live-setting input{accent-color:#3c6a54}.picker-input{display:flex;min-width:0}.picker-input input,.date-field input,.modal-search{box-sizing:border-box;width:100%;height:29px;padding:4px 7px;border:1px solid #cbc8bc;background:#fff;color:#292820;font:inherit}.picker-input button{width:31px;border:1px solid #cbc8bc;border-left:0;background:#eeeae0;font:inherit}.filter-actions{display:flex;align-items:center;gap:7px;margin-top:10px}.live-setting{display:flex;align-items:center;gap:5px;margin-right:auto;color:#706c5e;font-size:10px}.query-button,.close-button{min-height:29px;padding:4px 13px;border:1px solid #3c6a54;border-radius:3px;background:#3c6a54;color:#fff;font:inherit;font-weight:700;cursor:pointer}.close-button{border-color:#777366;background:#fffefa;color:#4e4a40}.query-button:disabled{opacity:.6;cursor:wait}.error-message,.warning-message{margin:8px 0 0;font-size:10px}.error-message{color:#b03939}.warning-message{color:#8b6b18}.legend-panel{display:flex;align-items:center;flex-wrap:wrap;gap:8px 13px;padding:8px 10px}.legend-item{display:flex;align-items:center;gap:5px;color:#5b584d;font-size:10px}.legend-item>span,.chart-legend span{width:11px;height:11px;border-radius:2px}.loss-swatch{background:#34332e}.range-label{margin-left:auto;color:#777366;font-size:10px}.results-layout{display:grid;grid-template-columns:minmax(0,1.8fr) minmax(290px,.9fr);gap:10px;min-height:320px}.table-panel,.detail-panel{min-width:0}.table-toolbar{display:flex;justify-content:space-between;align-items:center;padding:9px 10px;border-bottom:1px solid #e0ddd2}.table-toolbar strong{font-size:12px}.table-toolbar span{color:#777366;font-size:10px}.table-scroll{overflow:auto;max-height:calc(100vh - 345px)}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:10px}th{position:sticky;top:0;z-index:1;padding:7px 6px;background:#eeece4;color:#514e43;text-align:right;font-weight:700}th:first-child{text-align:left}td{padding:7px 6px;border-top:1px solid #eeece6;color:#3b3932}tbody tr{cursor:pointer}tbody tr:hover{background:#f7f5ed}tbody tr.selected{background:#e8efe8}.equipment-col{min-width:144px}.moves-col{min-width:72px}.state-col{min-width:70px}.numeric{text-align:right;font-variant-numeric:tabular-nums}.equipment-cell{font-weight:700}.expand-button{width:19px;height:19px;margin-right:4px;border:1px solid #d1cec3;border-radius:2px;background:#faf9f5;color:#454238;cursor:pointer}.child-row{background:#faf9f5}.child-row .equipment-cell{padding-left:24px;font-weight:500}.child-marker{margin-right:5px;color:#989384}.empty-state{height:120px;text-align:center;color:#827d6d}.table-footer{padding:7px 9px;border-top:1px solid #e4e1d7;color:#777366;font-size:9px}.detail-panel{padding:12px}.detail-heading{display:flex;justify-content:space-between;gap:8px}.detail-heading h2{margin:3px 0;font-size:15px}.detail-heading p{margin:0;color:#777366;font-size:10px}.moves-pill{height:max-content;padding:4px 7px;border-radius:12px;background:#eeeae0;color:#555144;font-size:9px}.chart-area{display:flex;align-items:center;justify-content:center;gap:14px;padding:18px 0}.donut{display:grid;flex:0 0 142px;width:142px;height:142px;place-items:center;border-radius:50%}.donut-hole{display:flex;width:88px;height:88px;flex-direction:column;align-items:center;justify-content:center;border-radius:50%;background:#fffefa}.donut-hole strong{font-size:17px}.donut-hole small{color:#777366;font-size:9px}.chart-legend{display:grid;gap:5px;min-width:106px;max-height:210px;overflow:auto}.chart-legend>div{display:grid;grid-template-columns:12px 1fr auto;align-items:center;gap:5px;font-size:9px}.chart-legend strong{font-variant-numeric:tabular-nums}.no-data-chart{display:flex;min-height:195px;flex-direction:column;align-items:center;justify-content:center;gap:5px;color:#777366}.no-data-chart strong{font-size:16px;color:#827d6d}.detail-note{padding-top:8px;border-top:1px solid #e7e4da;color:#777366;font-size:9px}.detail-empty{display:flex;height:100%;min-height:230px;flex-direction:column;align-items:center;justify-content:center;text-align:center;color:#777366}.detail-empty strong{margin-top:8px;color:#565347}.detail-empty p{max-width:210px;margin:5px 0;font-size:10px}.empty-icon{font-size:28px;color:#a49f8d}.modal-backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:20px;background:#201f1bcc}.picker-modal{display:flex;width:min(510px,100%);max-height:min(620px,90vh);flex-direction:column;border:1px solid #c9c5b8;background:#fffefa;box-shadow:0 16px 55px #0005}.picker-modal>header,.picker-modal>footer{display:flex;align-items:center;justify-content:space-between;padding:12px;border-bottom:1px solid #e1ded4}.picker-modal>header h2{margin:3px 0 0;font-size:15px}.picker-modal>footer{border-top:1px solid #e1ded4;border-bottom:0;color:#777366}.icon-button{width:28px;height:28px;border:0;background:transparent;font-size:22px;cursor:pointer}.picker-search{display:flex;gap:6px;margin:10px 12px}.picker-search .modal-search{flex:1;min-width:0;margin:0}.picker-search button{min-width:70px;border:1px solid #cbc8bc;background:#f1efe8;font:inherit}.picker-list{overflow:auto;min-height:100px;padding:0 12px}.lookup-option{display:flex;width:100%;min-height:36px;align-items:center;gap:9px;padding:6px 8px;border:0;border-bottom:1px solid #efede6;background:#fffefa;color:#514e43;text-align:left;font:inherit;cursor:pointer}.lookup-option:hover{background:#f3f1e9}.lookup-option strong{min-width:116px;color:#343229}.lookup-option span{color:#777366}.equipment-option{box-sizing:border-box}.equipment-option input{accent-color:#3c6a54}.empty-picker{padding:25px;text-align:center;color:#827d6d}.lookup-error{margin:0 12px;color:#b03939;font-size:10px}@media(max-width:1100px){.filter-grid{grid-template-columns:repeat(2,minmax(200px,1fr))}}@media(max-width:760px){.filter-grid{grid-template-columns:1fr}.filter-actions{flex-wrap:wrap}.live-setting{width:100%;margin-right:0}.results-layout{grid-template-columns:1fr}.table-scroll{max-height:55vh}.chart-area{flex-wrap:wrap}.range-label{margin-left:0}}
</style>
