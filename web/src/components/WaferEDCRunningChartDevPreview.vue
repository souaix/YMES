<script setup lang="ts">
import { computed, ref } from 'vue'

interface RecipeOption {
  key: string
  psNo: string
  testRecipeType: string
  testRecipeNo: string
}

interface ReportRecord {
  id: string
  logGroupSerial: string
  componentNo: string
  productNo: string
  lotNo: string
  opNo: string
  equipmentNo: string
  testRecipeType: string
  testRecipeNo: string
  testRecipe: string
  testDate: string
  importDate: string
  measurements: number[]
  checkFlag: boolean
}

type PickerName = 'ps' | 'recipe' | 'equipment' | null

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

const productionSegments = ['PS-WP-01', 'PS-WP-02', 'PS-WP-03']
const recipes: RecipeOption[] = [
  { key: 'PS-WP-01|WAFER_DC|VTH_CHECK', psNo: 'PS-WP-01', testRecipeType: 'WAFER_DC', testRecipeNo: 'VTH_CHECK' },
  { key: 'PS-WP-01|WAFER_DC|IDSAT', psNo: 'PS-WP-01', testRecipeType: 'WAFER_DC', testRecipeNo: 'IDSAT' },
  { key: 'PS-WP-02|WAFER_LEAK|GATE_LEAK', psNo: 'PS-WP-02', testRecipeType: 'WAFER_LEAK', testRecipeNo: 'GATE_LEAK' },
  { key: 'PS-WP-02|WAFER_LEAK|JUNCTION_LEAK', psNo: 'PS-WP-02', testRecipeType: 'WAFER_LEAK', testRecipeNo: 'JUNCTION_LEAK' },
  { key: 'PS-WP-03|WAFER_DC|VTH_CHECK', psNo: 'PS-WP-03', testRecipeType: 'WAFER_DC', testRecipeNo: 'VTH_CHECK' },
]
const offsets = [-0.023, -0.017, -0.015, -0.014, -0.012, -0.010, -0.008, -0.007, -0.005, -0.004, -0.002, -0.001, 0.001, 0.002, 0.003, 0.005, 0.006, 0.008, 0.010, 0.011, 0.013, 0.014, 0.016, 0.019]
function sampleValues(center: number, trend: number) {
  return offsets.map((offset, index) => Number((center + offset + trend * index / 23 + ((index % 4) - 1.5) * 0.0006).toFixed(4)))
}

const records: ReportRecord[] = [
  { id: 'rtb01-001', logGroupSerial: 'LG-WP-261120-001', componentNo: 'WFR-001-DIE-01', productNo: 'DEV-IC-240', lotNo: 'W26112001', opNo: 'WP20', equipmentNo: 'WPROBE-03', testRecipeType: 'WAFER_DC', testRecipeNo: 'VTH_CHECK', testRecipe: 'VTH_CHECK', testDate: '2026-11-20T08:12:41', importDate: '2026-11-20T08:16:03', measurements: sampleValues(1.024, 0.006), checkFlag: false },
  { id: 'rtb01-002', logGroupSerial: 'LG-WP-261120-002', componentNo: 'WFR-001-DIE-02', productNo: 'DEV-IC-240', lotNo: 'W26112001', opNo: 'WP20', equipmentNo: 'WPROBE-03', testRecipeType: 'WAFER_DC', testRecipeNo: 'VTH_CHECK', testRecipe: 'VTH_CHECK', testDate: '2026-11-20T08:19:08', importDate: '2026-11-20T08:21:10', measurements: sampleValues(1.027, -0.008), checkFlag: false },
  { id: 'rtb01-003', logGroupSerial: 'LG-WP-261120-003', componentNo: 'WFR-001-DIE-03', productNo: 'DEV-IC-240', lotNo: 'W26112001', opNo: 'WP20', equipmentNo: 'WPROBE-05', testRecipeType: 'WAFER_DC', testRecipeNo: 'IDSAT', testRecipe: 'IDSAT', testDate: '2026-11-20T09:02:27', importDate: '2026-11-20T09:06:42', measurements: sampleValues(122.5, 0.35), checkFlag: false },
  { id: 'rtb01-004', logGroupSerial: 'LG-WP-261121-007', componentNo: 'WFR-004-DIE-18', productNo: 'DEV-IC-240', lotNo: 'W26112104', opNo: 'WP20', equipmentNo: 'WPROBE-03', testRecipeType: 'WAFER_DC', testRecipeNo: 'VTH_CHECK', testRecipe: 'VTH_CHECK', testDate: '2026-11-21T11:40:33', importDate: '2026-11-21T11:43:56', measurements: sampleValues(1.019, 0.011), checkFlag: false },
  { id: 'rtb01-005', logGroupSerial: 'LG-WP-261121-008', componentNo: 'WFR-004-DIE-19', productNo: 'DEV-IC-240', lotNo: 'W26112104', opNo: 'WP20', equipmentNo: 'WPROBE-05', testRecipeType: 'WAFER_DC', testRecipeNo: 'VTH_CHECK', testRecipe: 'VTH_CHECK', testDate: '2026-11-21T11:52:06', importDate: '2026-11-21T11:54:15', measurements: sampleValues(1.022, -0.004), checkFlag: false },
  { id: 'rtb01-006', logGroupSerial: 'LG-WP-261122-010', componentNo: 'WFR-008-DIE-07', productNo: 'DEV-IC-180', lotNo: 'W26112208', opNo: 'WP21', equipmentNo: 'WPROBE-09', testRecipeType: 'WAFER_LEAK', testRecipeNo: 'GATE_LEAK', testRecipe: 'GATE_LEAK', testDate: '2026-11-22T07:23:52', importDate: '2026-11-22T07:29:31', measurements: sampleValues(0.084, 0.012), checkFlag: false },
  { id: 'rtb01-007', logGroupSerial: 'LG-WP-261122-011', componentNo: 'WFR-008-DIE-08', productNo: 'DEV-IC-180', lotNo: 'W26112208', opNo: 'WP21', equipmentNo: 'WPROBE-09', testRecipeType: 'WAFER_LEAK', testRecipeNo: 'JUNCTION_LEAK', testRecipe: 'JUNCTION_LEAK', testDate: '2026-11-22T07:41:19', importDate: '2026-11-22T07:44:57', measurements: sampleValues(0.142, -0.009), checkFlag: false },
  { id: 'rtb01-008', logGroupSerial: 'LG-WP-261123-018', componentNo: 'WFR-012-DIE-21', productNo: 'DEV-IC-240', lotNo: 'W26112312', opNo: 'WP20', equipmentNo: 'WPROBE-03', testRecipeType: 'WAFER_DC', testRecipeNo: 'VTH_CHECK', testRecipe: 'VTH_CHECK', testDate: '2026-11-23T15:08:05', importDate: '2026-11-23T15:12:18', measurements: sampleValues(1.031, 0.004), checkFlag: false },
]

const equipmentOptions = ['WPROBE-03', 'WPROBE-05', 'WPROBE-09']
const psNo = ref('')
const recipeKeys = ref<string[]>([])
const equipmentNo = ref('')
const componentNo = ref('')
const lotNo = ref('')
const includeEquipment = ref(false)
const includeComponent = ref(false)
const includeLot = ref(false)
const includeStartDate = ref(false)
const includeEndDate = ref(false)
const startDate = ref('2026-11-01T00:00:00')
const endDate = ref('2026-11-26T23:59:59')
const queryResults = ref<ReportRecord[]>([])
const queried = ref(false)
const notice = ref('選取 PSNo 與一個以上 TestRecipe 後按 Query。')
const noticeError = ref(false)
const checkedIds = ref<string[]>([])
const picker = ref<PickerName>(null)
const draftPsNo = ref('')
const draftRecipeKeys = ref<string[]>([])
const draftEquipmentNo = ref('')
const chartsOpen = ref(false)

const availableRecipes = computed(() => recipes.filter((item) => item.psNo === psNo.value))
const recipeSummary = computed(() => recipeKeys.value.map((key) => recipes.find((item) => item.key === key)).filter((item): item is RecipeOption => Boolean(item)).map((item) => `${item.testRecipeType} / ${item.testRecipeNo}`).join('; '))
const checkedRows = computed(() => queryResults.value.filter((row) => checkedIds.value.includes(row.id)))
const allChecked = computed(() => queryResults.value.length > 0 && checkedRows.value.length === queryResults.value.length)
const chartSeries = computed(() => checkedRows.value.map((row) => {
  const values = row.measurements
  const min = Math.min(...values)
  const max = Math.max(...values)
  const mean = values.reduce((sum, value) => sum + value, 0) / (values.length || 1)
  const variance = values.reduce((sum, value) => sum + (value - mean) ** 2, 0) / (values.length || 1)
  const sd = Math.sqrt(variance)
  const low = min - (max === min ? 1 : max - min) * 0.12
  const high = max + (max === min ? 1 : max - min) * 0.12
  const plot = { width: 900, height: 270, left: 62, right: 24, top: 20, bottom: 44 }
  const plotWidth = plot.width - plot.left - plot.right
  const plotHeight = plot.height - plot.top - plot.bottom
  const points = values.map((value, index) => ({
    sequence: index + 1,
    value,
    x: plot.left + (values.length <= 1 ? 0 : index / (values.length - 1)) * plotWidth,
    y: plot.top + (high - value) / (high - low) * plotHeight,
  }))
  return { row, points, path: points.map((point, index) => `${index === 0 ? 'M' : 'L'} ${point.x.toFixed(1)} ${point.y.toFixed(1)}`).join(' '), stats: { count: values.length, mean, sd, min, max }, plot, yTop: plot.top, yBottom: plot.top + plotHeight }
}))

function openPicker(name: Exclude<PickerName, null>) {
  if (name !== 'ps' && !psNo.value) {
    notice.value = '請先選擇 PSNo。'
    noticeError.value = true
    return
  }
  if (name === 'ps') draftPsNo.value = psNo.value
  if (name === 'recipe') draftRecipeKeys.value = [...recipeKeys.value]
  if (name === 'equipment') draftEquipmentNo.value = equipmentNo.value
  picker.value = name
}

function confirmPicker() {
  let resetQuery = false
  if (picker.value === 'ps') {
    psNo.value = draftPsNo.value
    recipeKeys.value = []
    resetQuery = true
  }
  if (picker.value === 'recipe') {
    recipeKeys.value = [...draftRecipeKeys.value]
    resetQuery = true
  }
  if (picker.value === 'equipment') {
    equipmentNo.value = draftEquipmentNo.value
    includeEquipment.value = Boolean(draftEquipmentNo.value)
  }
  picker.value = null
  if (resetQuery) {
    queryResults.value = []
    checkedIds.value = []
    queried.value = false
  }
}

function toggleRecipe(key: string) {
  draftRecipeKeys.value = draftRecipeKeys.value.includes(key) ? draftRecipeKeys.value.filter((value) => value !== key) : [...draftRecipeKeys.value, key]
}

function toggleAllRows() {
  checkedIds.value = allChecked.value ? [] : queryResults.value.map((row) => row.id)
}

function query() {
  if (!psNo.value) {
    queried.value = false
    queryResults.value = []
    notice.value = 'PSNo 為必要條件。'
    noticeError.value = true
    return
  }
  if (recipeKeys.value.length === 0) {
    queried.value = false
    queryResults.value = []
    notice.value = '至少選擇一個 TestRecipe。'
    noticeError.value = true
    return
  }
  if ((includeStartDate.value && !startDate.value) || (includeEndDate.value && !endDate.value) || (includeStartDate.value && includeEndDate.value && startDate.value > endDate.value)) {
    notice.value = '日期端點不完整，或起日大於迄日。'
    noticeError.value = true
    return
  }
  const chosen = new Set(recipeKeys.value)
  queryResults.value = records.filter((row) => chosen.has(`${psNo.value}|${row.testRecipeType}|${row.testRecipeNo}`)
    && (!includeEquipment.value || !equipmentNo.value || row.equipmentNo === equipmentNo.value)
    && (!includeComponent.value || !componentNo.value.trim() || row.componentNo.toLowerCase().includes(componentNo.value.trim().toLowerCase()))
    && (!includeLot.value || !lotNo.value.trim() || row.lotNo.toLowerCase().includes(lotNo.value.trim().toLowerCase()))
    && (!includeStartDate.value || row.testDate >= startDate.value)
    && (!includeEndDate.value || row.testDate <= endDate.value))
    .map((row) => ({ ...row, checkFlag: false }))
  checkedIds.value = []
  queried.value = true
  notice.value = queryResults.value.length ? `DEV 假資料：找到 ${queryResults.value.length} 筆記錄；勾選一筆以上可一次繪製多張運行圖。` : 'DEV 假資料：沒有符合條件的記錄。'
  noticeError.value = false
}

function openCharts() {
  if (checkedRows.value.length === 0) {
    notice.value = '請先勾選至少一筆測試記錄。'
    noticeError.value = true
    return
  }
  chartsOpen.value = true
}

function toggleEquipmentFilter() {
  if (!includeEquipment.value) equipmentNo.value = ''
}

function toggleComponentFilter() {
  if (!includeComponent.value) componentNo.value = ''
}

function toggleLotFilter() {
  if (!includeLot.value) lotNo.value = ''
}

function saveGraph(id: string, recipe: string) {
  const svg = document.getElementById(`rtb01-chart-${id}`)
  if (!svg) return
  const clone = svg.cloneNode(true) as SVGSVGElement
  clone.setAttribute('xmlns', 'http://www.w3.org/2000/svg')
  const data = new XMLSerializer().serializeToString(clone)
  const url = URL.createObjectURL(new Blob([data], { type: 'image/svg+xml;charset=utf-8' }))
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = `${recipe}-running-chart.svg`
  anchor.click()
  URL.revokeObjectURL(url)
}

function displayDate(value: string) {
  return value.replace('T', ' ')
}
</script>

<template>
  <section class="wafer-running" aria-label="Wafer 電性數據運行圖 DEV 預覽">
    <header class="page-heading"><div><p class="eyebrow">RTB01 · READ ONLY REPORT</p><h1>Wafer 電性數據運行圖</h1></div><span class="dev-badge">DEV 假資料</span></header>
    <section class="filter-panel" aria-label="查詢條件">
      <div class="filter-grid">
        <label class="filter-field"><span>PSNo</span><div class="picker-field"><input :value="psNo" readonly placeholder="請選擇製程" @click="openPicker('ps')"><button type="button" @click="openPicker('ps')">…</button></div></label>
        <label class="filter-field recipe-field"><span>TestRecipe</span><div class="picker-field"><input :value="recipeSummary" readonly placeholder="請先選 PSNo，再選測試項目" @click="openPicker('recipe')"><button type="button" @click="openPicker('recipe')">…</button></div></label>
        <label class="filter-field"><span class="check-caption"><input v-model="includeEquipment" type="checkbox" @change="toggleEquipmentFilter"> EquipmentNo</span><div class="picker-field"><input :value="equipmentNo" readonly placeholder="全部主設備" @click="openPicker('equipment')"><button type="button" @click="openPicker('equipment')">…</button></div></label>
        <label class="filter-field"><span class="check-caption"><input v-model="includeComponent" type="checkbox" @change="toggleComponentFilter"> ComponentNo</span><input v-model="componentNo" type="text" placeholder="輸入元件編號片段" @input="includeComponent = Boolean(componentNo.trim())"></label>
        <label class="filter-field"><span class="check-caption"><input v-model="includeLot" type="checkbox" @change="toggleLotFilter"> LotNo</span><input v-model="lotNo" type="text" placeholder="輸入批號片段" @input="includeLot = Boolean(lotNo.trim())"></label>
        <label class="filter-field"><span class="check-caption"><input v-model="includeStartDate" type="checkbox"> StartTestTime</span><input v-model="startDate" type="datetime-local" step="1"></label>
        <label class="filter-field"><span class="check-caption"><input v-model="includeEndDate" type="checkbox"> EndTestTime</span><input v-model="endDate" type="datetime-local" step="1"></label>
      </div>
      <div class="filter-actions"><p :class="['notice', noticeError ? 'notice-error' : '']" role="status">{{ notice }}</p><div><button class="primary-button" type="button" @click="query">Query</button><button type="button" @click="emit('close')">Close</button></div></div>
    </section>

    <section class="results-panel" aria-label="EDC Raw Data 記錄">
      <div class="panel-heading"><div><h2>EDC Raw Data Records</h2><span v-if="checkedRows.length" class="selected-count">已選 {{ checkedRows.length }} 筆</span></div><button type="button" :disabled="checkedRows.length === 0" @click="openCharts">Enable Chart</button></div>
      <p v-if="!queried" class="empty-state">設定 PSNo 與 TestRecipe 後執行查詢。</p>
      <p v-else-if="queryResults.length === 0" class="empty-state">沒有符合條件的記錄。</p>
      <div v-else class="table-scroll"><table><thead><tr><th><input type="checkbox" :checked="allChecked" aria-label="全選" @change="toggleAllRows"></th><th>LogGroupSerial</th><th>ImportDate</th><th>ComponentNo</th><th>ProductNo</th><th>LotNo</th><th>OPNo</th><th>EquipmentNo</th><th>TestRecipeType</th><th>TestRecipeNo</th><th>TestDate</th></tr></thead><tbody>
        <tr v-for="row in queryResults" :key="row.id" :class="{ selected: checkedIds.includes(row.id) }"><td><input v-model="checkedIds" type="checkbox" :value="row.id" :aria-label="`選取 ${row.logGroupSerial}`"></td><td>{{ row.logGroupSerial }}</td><td>{{ displayDate(row.importDate) }}</td><td>{{ row.componentNo }}</td><td>{{ row.productNo }}</td><td>{{ row.lotNo }}</td><td>{{ row.opNo }}</td><td>{{ row.equipmentNo }}</td><td>{{ row.testRecipeType }}</td><td>{{ row.testRecipeNo }}</td><td>{{ displayDate(row.testDate) }}</td></tr>
      </tbody></table></div>
    </section>

    <div v-if="picker" class="modal-backdrop" @click.self="picker = null">
      <section class="picker-modal" role="dialog" aria-modal="true" :aria-label="picker === 'ps' ? '選擇 PSNo' : picker === 'recipe' ? '選擇 TestRecipe' : '選擇 EquipmentNo'">
        <header><h2>{{ picker === 'ps' ? '選擇 PSNo' : picker === 'recipe' ? '選擇 TestRecipe' : '選擇主設備' }}</h2><button type="button" aria-label="關閉" @click="picker = null">×</button></header>
        <div v-if="picker === 'ps'" class="choice-list"><label v-for="item in productionSegments" :key="item"><input v-model="draftPsNo" type="radio" name="psno" :value="item">{{ item }}</label></div>
        <div v-else-if="picker === 'recipe'" class="choice-list"><label v-for="item in availableRecipes" :key="item.key"><input type="checkbox" :checked="draftRecipeKeys.includes(item.key)" @change="toggleRecipe(item.key)"><span><strong>{{ item.testRecipeType }} / {{ item.testRecipeNo }}</strong></span></label><p v-if="availableRecipes.length === 0">此 PSNo 沒有 DEV 測試項目。</p></div>
        <div v-else class="choice-list"><label><input v-model="draftEquipmentNo" type="radio" name="equipment" value="">全部主設備</label><label v-for="item in equipmentOptions" :key="item"><input v-model="draftEquipmentNo" type="radio" name="equipment" :value="item">{{ item }}</label></div>
        <footer><button type="button" @click="picker = null">Cancel</button><button class="primary-button" type="button" @click="confirmPicker">Apply</button></footer>
      </section>
    </div>

    <div v-if="chartsOpen" class="chart-backdrop" @click.self="chartsOpen = false">
      <section class="chart-modal" role="dialog" aria-modal="true" aria-label="Wafer 電性運行圖">
        <header class="chart-heading"><div><p class="eyebrow">RUNNING CHART · DEV</p><h2>Selected EDC Samples</h2><p>{{ chartSeries.length }} 筆匯入記錄；每筆的 Measure Data 依 Sequence 作圖。</p></div><button type="button" @click="chartsOpen = false">Close</button></header>
        <div class="chart-list"><article v-for="series in chartSeries" :key="series.row.id" class="chart-card">
          <header><div><h3>{{ series.row.opNo }} · {{ series.row.testRecipeType }} / {{ series.row.testRecipeNo }}</h3><p>{{ series.row.lotNo }} · {{ series.row.componentNo }} · {{ series.row.equipmentNo }} · {{ displayDate(series.row.testDate) }}</p></div><button type="button" @click="saveGraph(series.row.id, series.row.testRecipeNo)">Save Graph</button></header>
          <svg :id="`rtb01-chart-${series.row.id}`" class="running-svg" viewBox="0 0 900 270" role="img" :aria-label="`${series.row.testRecipeNo} sequence running chart`">
            <line x1="62" :y1="series.yTop" x2="62" :y2="series.yBottom" class="axis-line"/><line x1="62" :y1="series.yBottom" x2="876" :y2="series.yBottom" class="axis-line"/>
            <line v-for="tick in [0, 0.25, 0.5, 0.75, 1]" :key="tick" x1="62" :y1="series.yTop + tick * (series.yBottom - series.yTop)" x2="876" :y2="series.yTop + tick * (series.yBottom - series.yTop)" class="grid-line"/>
            <path :d="series.path" class="series-line"/>
            <circle v-for="point in series.points" :key="point.sequence" :cx="point.x" :cy="point.y" r="3.2" class="series-point"><title>#{{ point.sequence }} · {{ point.value.toFixed(4) }}</title></circle>
            <text x="57" :y="series.yTop + 4" text-anchor="end" class="axis-label">{{ series.stats.max.toFixed(4) }}</text><text x="57" :y="series.yBottom" text-anchor="end" class="axis-label">{{ series.stats.min.toFixed(4) }}</text>
            <text x="470" y="260" text-anchor="middle" class="axis-title">Sequence</text><text x="14" y="130" text-anchor="middle" class="axis-title" transform="rotate(-90 14 130)">Measure Data</text>
            <text x="62" y="246" class="axis-label">1</text><text x="876" y="246" text-anchor="end" class="axis-label">{{ series.stats.count }}</text>
          </svg>
          <footer><span>DataPoint: {{ series.stats.count }}</span><span>Mean: {{ series.stats.mean.toFixed(4) }}</span><span>SDEV: {{ series.stats.sd.toFixed(4) }}</span><span>Min: {{ series.stats.min.toFixed(4) }}</span><span>Max: {{ series.stats.max.toFixed(4) }}</span></footer>
          <details><summary>量測點明細</summary><div class="table-scroll"><table><thead><tr><th>Sequence</th><th>Measure Data</th><th>TestRecipe</th><th>TestDate</th></tr></thead><tbody><tr v-for="point in series.points" :key="point.sequence"><td>{{ point.sequence }}</td><td>{{ point.value.toFixed(4) }}</td><td>{{ series.row.testRecipeNo }}</td><td>{{ displayDate(series.row.testDate) }}</td></tr></tbody></table></div></details>
        </article></div>
      </section>
    </div>
  </section>
</template>

<style scoped>
.wafer-running { box-sizing: border-box; min-height: 0; height: 100%; overflow: auto; padding: 12px; background: #f7f5ee; color: var(--ink, #26251f); font: inherit; }
.page-heading,.panel-heading,.chart-heading,.chart-card > header { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.page-heading { margin-bottom: 12px; }
.page-heading h1,.chart-heading h2 { margin: 1px 0 0; font-size: 20px; }
.eyebrow { margin: 0; color: #737063; font-size: 11px; font-weight: 700; letter-spacing: .08em; }
.dev-badge { padding: 4px 8px; border: 1px solid #aa8142; border-radius: 12px; background: #f8edda; color: #73501e; font-size: 11px; font-weight: 700; }
.filter-panel,.results-panel { margin-bottom: 12px; padding: 12px; border: 1px solid #d2cec2; border-radius: 4px; background: #fffefa; }
.filter-grid { display: grid; grid-template-columns: repeat(3, minmax(200px, 1fr)); gap: 10px 14px; }
.filter-field { display: flex; min-width: 0; flex-direction: column; gap: 4px; color: #555246; font-size: 12px; }
.filter-field > span { font-weight: 700; }
.filter-field input:not([type=checkbox]) { box-sizing: border-box; width: 100%; min-height: 30px; padding: 4px 7px; border: 1px solid #bbb7ac; border-radius: 3px; background: white; color: inherit; font: inherit; }
.picker-field { display: flex; gap: 4px; }
.picker-field input { min-width: 0; }
.picker-field button { min-width: 32px; border: 1px solid #8e897c; border-radius: 3px; background: #f4f1e9; color: inherit; font-size: 16px; }
.check-caption { display: flex; align-items: center; gap: 5px; }
.filter-actions { display: flex; justify-content: space-between; align-items: center; gap: 12px; margin-top: 12px; padding-top: 10px; border-top: 1px solid #e5e1d8; }
.notice { margin: 0; color: #4e6b48; font-size: 12px; }
.notice-error { color: #a33b32; }
button { min-height: 29px; padding: 4px 10px; border: 1px solid #8e897c; border-radius: 3px; background: #f4f1e9; color: #302f29; font: inherit; cursor: pointer; }
button:disabled { opacity: .48; cursor: default; }
.filter-actions button + button { margin-left: 6px; }
.primary-button,.panel-heading > button { border-color: #3d493b; background: #3d493b; color: white; }
.panel-heading h2,.chart-card h3 { margin: 0; font-size: 14px; }
.selected-count { display: block; margin-top: 3px; color: #61734f; font-size: 11px; }
.empty-state { margin: 16px 0 4px; color: #797568; font-size: 13px; }
.table-scroll { max-width: 100%; overflow: auto; margin-top: 10px; }
table { width: 100%; border-collapse: collapse; font-size: 11px; white-space: nowrap; }
th,td { padding: 6px 8px; border: 1px solid #ddd9d0; text-align: left; }
th { position: sticky; top: 0; z-index: 1; background: #efede5; color: #514e44; font-size: 10px; }
tbody tr.selected { background: #e7eedf; }
.modal-backdrop,.chart-backdrop { position: fixed; inset: 0; z-index: 1000; display: grid; place-items: center; padding: 20px; background: rgb(20 20 18 / 48%); }
.picker-modal { box-sizing: border-box; width: min(440px, 100%); max-height: 82vh; display: flex; flex-direction: column; border-radius: 5px; background: #fffefa; box-shadow: 0 12px 40px rgb(0 0 0 / 24%); }
.picker-modal > header,.picker-modal > footer { display: flex; align-items: center; justify-content: space-between; gap: 8px; padding: 12px; border-bottom: 1px solid #e0dcd2; }
.picker-modal > header h2 { margin: 0; font-size: 15px; }
.picker-modal > header button { border: 0; background: transparent; font-size: 22px; }
.picker-modal > footer { justify-content: flex-end; border-top: 1px solid #e0dcd2; border-bottom: 0; }
.choice-list { display: grid; gap: 3px; overflow: auto; padding: 12px; }
.choice-list label { display: flex; align-items: center; gap: 8px; padding: 8px; border-radius: 3px; background: #f8f6f0; cursor: pointer; }
.choice-list span { display: grid; gap: 3px; }
.choice-list p { color: #777366; font-size: 12px; }
.chart-backdrop { z-index: 1010; place-items: stretch; padding: 2vh 2vw; }
.chart-modal { box-sizing: border-box; display: flex; min-width: 0; max-height: 96vh; flex-direction: column; overflow: hidden; border-radius: 5px; background: #f7f5ee; box-shadow: 0 12px 40px rgb(0 0 0 / 30%); }
.chart-heading { padding: 14px 16px; border-bottom: 1px solid #d2cec2; background: #fffefa; }
.chart-heading p:last-child,.chart-card > header p { margin: 5px 0 0; color: #6d695d; font-size: 11px; }
.chart-list { display: grid; gap: 10px; overflow: auto; padding: 12px 16px 18px; }
.chart-card { padding: 10px; border: 1px solid #d2cec2; border-radius: 4px; background: #fffefa; }
.chart-card > header { align-items: flex-start; }
.running-svg { display: block; width: 100%; min-height: 165px; margin-top: 8px; background: #fff; }
.axis-line { stroke: #68665d; stroke-width: 1; }
.grid-line { stroke: #e8e5dc; stroke-width: 1; }
.axis-label,.axis-title { fill: #68665d; font-size: 10px; }
.axis-title { font-size: 11px; }
.series-line { fill: none; stroke: #557c9d; stroke-width: 2; stroke-linejoin: round; stroke-linecap: round; }
.series-point { fill: #557c9d; stroke: white; stroke-width: 1; }
.chart-card > footer { display: flex; flex-wrap: wrap; gap: 6px 18px; padding: 8px 0; border-bottom: 1px solid #e5e1d8; color: #5f5b50; font-size: 11px; font-variant-numeric: tabular-nums; }
.chart-card details { padding-top: 8px; color: #5f5b50; font-size: 11px; }
.chart-card summary { cursor: pointer; }
.chart-card details .table-scroll { max-height: 220px; }
@media (max-width: 900px) { .filter-grid { grid-template-columns: repeat(2, minmax(180px, 1fr)); } .chart-heading { align-items: flex-start; flex-direction: column; } }
@media (max-width: 580px) { .filter-grid { grid-template-columns: 1fr; } .filter-actions { align-items: flex-start; flex-direction: column; } }
</style>
