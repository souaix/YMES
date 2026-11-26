<script setup lang="ts">
import { computed, ref } from 'vue'

interface RecipeOption {
  key: string
  psNo: string
  testRecipeType: string
  testRecipeNo: string
  itemName: string
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
  testRecipe: string
  testDate: string
  importDate: string
  opName: string
  fileName: string
  measurements: number[]
}

interface MeasurementRow {
  sequence: number
  value: number
  excluded: boolean
}

type PickerName = 'ps' | 'recipe' | 'equipment' | null

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

const productionSegments = ['PS-WP-01', 'PS-WP-02', 'PS-WP-03']
const recipes: RecipeOption[] = [
  { key: 'PS-WP-01|WAFER_DC|VTH_CHECK', psNo: 'PS-WP-01', testRecipeType: 'WAFER_DC', testRecipeNo: 'VTH_CHECK', itemName: 'Threshold voltage' },
  { key: 'PS-WP-01|WAFER_DC|IDSAT', psNo: 'PS-WP-01', testRecipeType: 'WAFER_DC', testRecipeNo: 'IDSAT', itemName: 'Saturation current' },
  { key: 'PS-WP-02|WAFER_LEAK|GATE_LEAK', psNo: 'PS-WP-02', testRecipeType: 'WAFER_LEAK', testRecipeNo: 'GATE_LEAK', itemName: 'Gate leakage' },
  { key: 'PS-WP-02|WAFER_LEAK|JUNCTION_LEAK', psNo: 'PS-WP-02', testRecipeType: 'WAFER_LEAK', testRecipeNo: 'JUNCTION_LEAK', itemName: 'Junction leakage' },
  { key: 'PS-WP-03|WAFER_DC|VTH_CHECK', psNo: 'PS-WP-03', testRecipeType: 'WAFER_DC', testRecipeNo: 'VTH_CHECK', itemName: 'Threshold voltage' },
]

const offsets = [-0.021, -0.018, -0.016, -0.014, -0.012, -0.011, -0.009, -0.008, -0.006, -0.005, -0.004, -0.003, -0.002, -0.001, 0, 0, 0.001, 0.002, 0.003, 0.004, 0.005, 0.006, 0.007, 0.009, 0.011, 0.012, 0.014, 0.016, 0.018, 0.024]
function sampleValues(center: number, shift: number) {
  return offsets.map((offset, index) => Number((center + offset + shift + ((index % 5) - 2) * 0.0004).toFixed(4)))
}

const records: ReportRecord[] = [
  { id: 'rtb02-001', logGroupSerial: 'LG-WP-261120-001', componentNo: 'WFR-001-DIE-01', productNo: 'DEV-IC-240', lotNo: 'W26112001', opNo: 'WP20', equipmentNo: 'WPROBE-03', testRecipeType: 'WAFER_DC', testRecipe: 'VTH_CHECK', testDate: '2026-11-20T08:12:41', importDate: '2026-11-20T08:16:03', opName: 'Wafer Probe', fileName: 'W26112001_VTH_0812.csv', measurements: sampleValues(1.024, 0) },
  { id: 'rtb02-002', logGroupSerial: 'LG-WP-261120-002', componentNo: 'WFR-001-DIE-02', productNo: 'DEV-IC-240', lotNo: 'W26112001', opNo: 'WP20', equipmentNo: 'WPROBE-03', testRecipeType: 'WAFER_DC', testRecipe: 'VTH_CHECK', testDate: '2026-11-20T08:19:08', importDate: '2026-11-20T08:21:10', opName: 'Wafer Probe', fileName: 'W26112001_VTH_0819.csv', measurements: sampleValues(1.027, 0.001) },
  { id: 'rtb02-003', logGroupSerial: 'LG-WP-261120-003', componentNo: 'WFR-001-DIE-03', productNo: 'DEV-IC-240', lotNo: 'W26112001', opNo: 'WP20', equipmentNo: 'WPROBE-05', testRecipeType: 'WAFER_DC', testRecipe: 'IDSAT', testDate: '2026-11-20T09:02:27', importDate: '2026-11-20T09:06:42', opName: 'Wafer Probe', fileName: 'W26112001_IDS_0902.csv', measurements: sampleValues(122.5, 1.9) },
  { id: 'rtb02-004', logGroupSerial: 'LG-WP-261121-007', componentNo: 'WFR-004-DIE-18', productNo: 'DEV-IC-240', lotNo: 'W26112104', opNo: 'WP20', equipmentNo: 'WPROBE-03', testRecipeType: 'WAFER_DC', testRecipe: 'VTH_CHECK', testDate: '2026-11-21T11:40:33', importDate: '2026-11-21T11:43:56', opName: 'Wafer Probe', fileName: 'W26112104_VTH_1140.csv', measurements: sampleValues(1.019, -0.001) },
  { id: 'rtb02-005', logGroupSerial: 'LG-WP-261121-008', componentNo: 'WFR-004-DIE-19', productNo: 'DEV-IC-240', lotNo: 'W26112104', opNo: 'WP20', equipmentNo: 'WPROBE-05', testRecipeType: 'WAFER_DC', testRecipe: 'VTH_CHECK', testDate: '2026-11-21T11:52:06', importDate: '2026-11-21T11:54:15', opName: 'Wafer Probe', fileName: 'W26112104_VTH_1152.csv', measurements: sampleValues(1.022, 0.0005) },
  { id: 'rtb02-006', logGroupSerial: 'LG-WP-261122-010', componentNo: 'WFR-008-DIE-07', productNo: 'DEV-IC-180', lotNo: 'W26112208', opNo: 'WP21', equipmentNo: 'WPROBE-09', testRecipeType: 'WAFER_LEAK', testRecipe: 'GATE_LEAK', testDate: '2026-11-22T07:23:52', importDate: '2026-11-22T07:29:31', opName: 'Wafer Probe 2', fileName: 'W26112208_GLEAK_0723.csv', measurements: sampleValues(0.084, 0.003) },
  { id: 'rtb02-007', logGroupSerial: 'LG-WP-261122-011', componentNo: 'WFR-008-DIE-08', productNo: 'DEV-IC-180', lotNo: 'W26112208', opNo: 'WP21', equipmentNo: 'WPROBE-09', testRecipeType: 'WAFER_LEAK', testRecipe: 'JUNCTION_LEAK', testDate: '2026-11-22T07:41:19', importDate: '2026-11-22T07:44:57', opName: 'Wafer Probe 2', fileName: 'W26112208_JLEAK_0741.csv', measurements: sampleValues(0.142, 0.004) },
  { id: 'rtb02-008', logGroupSerial: 'LG-WP-261123-018', componentNo: 'WFR-012-DIE-21', productNo: 'DEV-IC-240', lotNo: 'W26112312', opNo: 'WP20', equipmentNo: 'WPROBE-03', testRecipeType: 'WAFER_DC', testRecipe: 'VTH_CHECK', testDate: '2026-11-23T15:08:05', importDate: '2026-11-23T15:12:18', opName: 'Wafer Probe', fileName: 'W26112312_VTH_1508.csv', measurements: sampleValues(1.031, 0.002) },
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
const selectedId = ref('')
const picker = ref<PickerName>(null)
const draftPsNo = ref('')
const draftRecipeKeys = ref<string[]>([])
const draftEquipmentNo = ref('')
const analysisOpen = ref(false)
const analysisRecord = ref<ReportRecord | null>(null)
const measurementRows = ref<MeasurementRow[]>([])
const groupCount = ref(8)
const sigmaTimes = ref(3)
const statChoices = [
  { key: 'count', label: 'Data Qty' }, { key: 'mean', label: 'Mean' }, { key: 'sd', label: 'Std Dev' },
  { key: 'max', label: 'Max' }, { key: 'min', label: 'Min' }, { key: 'range', label: 'Data Range' },
  { key: 'skew', label: 'Skew' }, { key: 'kurtosis', label: 'Kurtosis' },
]
const visibleStats = ref(['count', 'mean', 'sd', 'max', 'min'])

const availableRecipes = computed(() => recipes.filter((item) => item.psNo === psNo.value))
const recipeSummary = computed(() => recipeKeys.value.map((key) => recipes.find((item) => item.key === key)).filter((item): item is RecipeOption => Boolean(item)).map((item) => `${item.testRecipeType} / ${item.testRecipeNo}`).join('; '))
const selectedRecord = computed(() => queryResults.value.find((row) => row.id === selectedId.value) ?? null)
const allValues = computed(() => measurementRows.value.map((row) => row.value))
const activeValues = computed(() => measurementRows.value.filter((row) => !row.excluded).map((row) => row.value))
const statistics = computed(() => {
  const values = activeValues.value
  if (values.length === 0) return { count: 0, mean: 0, sd: 0, max: 0, min: 0, range: 0, skew: 0, kurtosis: 0 }
  const mean = values.reduce((sum, value) => sum + value, 0) / values.length
  const variance = values.reduce((sum, value) => sum + (value - mean) ** 2, 0) / values.length
  const sd = Math.sqrt(variance)
  const third = values.reduce((sum, value) => sum + (value - mean) ** 3, 0) / values.length
  const fourth = values.reduce((sum, value) => sum + (value - mean) ** 4, 0) / values.length
  const min = Math.min(...values)
  const max = Math.max(...values)
  return { count: values.length, mean, sd, max, min, range: max - min, skew: sd ? third / sd ** 3 : 0, kurtosis: variance ? fourth / variance ** 2 - 3 : 0 }
})
const statisticCards = computed(() => statChoices.filter((item) => visibleStats.value.includes(item.key)).map((item) => ({ ...item, value: item.key === 'count' ? statistics.value.count.toString() : statistics.value[item.key as keyof typeof statistics.value].toFixed(4) })))
const bins = computed(() => {
  if (allValues.value.length === 0) return []
  const min = Math.min(...allValues.value)
  const max = Math.max(...allValues.value)
  const count = Math.max(8, Math.min(32, Math.floor(Number(groupCount.value) || 8)))
  const step = max === min ? 1 : (max - min) / Math.max(1, count - 3)
  const start = min - step * 1.5
  return Array.from({ length: count }, (_, index) => {
    const from = start + index * step
    const to = from + step
    const frequency = activeValues.value.filter((value) => value >= from && (index === count - 1 ? value <= to : value < to)).length
    return { index, from, to, median: (from + to) / 2, frequency }
  })
})
const chartBars = computed(() => {
  const width = 820
  const height = 280
  const left = 56
  const right = 20
  const top = 18
  const bottom = 52
  const plotWidth = width - left - right
  const plotHeight = height - top - bottom
  const maxFrequency = Math.max(1, ...bins.value.map((bin) => bin.frequency))
  const barWidth = bins.value.length ? plotWidth / bins.value.length : plotWidth
  return bins.value.map((bin, index) => {
    const barHeight = bin.frequency / maxFrequency * plotHeight
    return { ...bin, x: left + index * barWidth + 2, y: top + plotHeight - barHeight, width: Math.max(1, barWidth - 4), height: barHeight, labelY: height - 19 }
  })
})
const normalCurvePath = computed(() => {
  if (bins.value.length === 0 || statistics.value.sd === 0 || statistics.value.count < 2) return ''
  const width = 820
  const height = 280
  const left = 56
  const right = 20
  const top = 18
  const bottom = 52
  const plotWidth = width - left - right
  const plotHeight = height - top - bottom
  const from = bins.value[0].from
  const to = bins.value[bins.value.length - 1].to
  const maxFrequency = Math.max(1, ...bins.value.map((bin) => bin.frequency))
  const binWidth = bins.value[0].to - bins.value[0].from
  const curveFrom = Math.max(from, statistics.value.mean - Math.abs(sigmaTimes.value) * statistics.value.sd)
  const curveTo = Math.min(to, statistics.value.mean + Math.abs(sigmaTimes.value) * statistics.value.sd)
  if (curveTo <= curveFrom) return ''
  return Array.from({ length: 51 }, (_, index) => {
    const value = curveFrom + (curveTo - curveFrom) * index / 50
    const z = (value - statistics.value.mean) / statistics.value.sd
    const expected = statistics.value.count * binWidth / (statistics.value.sd * Math.sqrt(2 * Math.PI)) * Math.exp(-0.5 * z * z)
    const x = left + (value - from) / (to - from || 1) * plotWidth
    const y = top + plotHeight - expected / maxFrequency * plotHeight
    return `${index === 0 ? 'M' : 'L'} ${x.toFixed(1)} ${y.toFixed(1)}`
  }).join(' ')
})

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
  if (picker.value === 'ps') {
    psNo.value = draftPsNo.value
    recipeKeys.value = []
  }
  if (picker.value === 'recipe') recipeKeys.value = [...draftRecipeKeys.value]
  if (picker.value === 'equipment') {
    equipmentNo.value = draftEquipmentNo.value
    includeEquipment.value = Boolean(draftEquipmentNo.value)
  }
  picker.value = null
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

function toggleRecipe(key: string) {
  draftRecipeKeys.value = draftRecipeKeys.value.includes(key) ? draftRecipeKeys.value.filter((value) => value !== key) : [...draftRecipeKeys.value, key]
}

function query() {
  if (!psNo.value) {
    queryResults.value = []
    queried.value = false
    notice.value = 'PSNo 為必要條件。'
    noticeError.value = true
    return
  }
  if (recipeKeys.value.length === 0) {
    queryResults.value = []
    queried.value = false
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
  queryResults.value = records.filter((row) => chosen.has(`${psNo.value}|${row.testRecipeType}|${row.testRecipe}`)
    && (!includeEquipment.value || !equipmentNo.value || row.equipmentNo === equipmentNo.value)
    && (!includeComponent.value || !componentNo.value.trim() || row.componentNo.toLowerCase().includes(componentNo.value.trim().toLowerCase()))
    && (!includeLot.value || !lotNo.value.trim() || row.lotNo.toLowerCase().includes(lotNo.value.trim().toLowerCase()))
    && (!includeStartDate.value || row.testDate >= startDate.value)
    && (!includeEndDate.value || row.testDate <= endDate.value))
  queried.value = true
  selectedId.value = ''
  notice.value = queryResults.value.length ? `DEV 假資料：找到 ${queryResults.value.length} 筆 EDC 匯入記錄。選取一筆後按 Enable Chart。` : 'DEV 假資料：沒有符合條件的 EDC 記錄。'
  noticeError.value = false
}

function openAnalysis(row: ReportRecord) {
  analysisRecord.value = row
  measurementRows.value = row.measurements.map((value, index) => ({ sequence: index + 1, value, excluded: false }))
  analysisOpen.value = true
}

function toggleStat(key: string) {
  visibleStats.value = visibleStats.value.includes(key) ? visibleStats.value.filter((value) => value !== key) : [...visibleStats.value, key]
}

function saveGraph() {
  const svg = document.getElementById('rtb02-histogram')
  if (!svg) return
  const clone = svg.cloneNode(true) as SVGSVGElement
  clone.setAttribute('xmlns', 'http://www.w3.org/2000/svg')
  const content = new XMLSerializer().serializeToString(clone)
  const url = URL.createObjectURL(new Blob([content], { type: 'image/svg+xml;charset=utf-8' }))
  const link = document.createElement('a')
  link.href = url
  link.download = `${analysisRecord.value?.testRecipe ?? 'wafer-edc'}-normality.svg`
  link.click()
  URL.revokeObjectURL(url)
}

function displayDate(value: string) {
  return value.replace('T', ' ')
}
</script>

<template>
  <section class="wafer-normality" aria-label="Wafer 電性數據常態分析 DEV 預覽">
    <header class="page-heading">
      <div><p class="eyebrow">RTB02 · READ ONLY REPORT</p><h1>Wafer 電性數據常態分析圖</h1></div>
      <span class="dev-badge">DEV 假資料</span>
    </header>

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

    <section class="results-panel" aria-label="EDC 匯入記錄">
      <div class="panel-heading"><h2>EDC Raw Data Records</h2><button type="button" :disabled="!selectedRecord" @click="selectedRecord && openAnalysis(selectedRecord)">Enable Chart</button></div>
      <p v-if="!queried" class="empty-state">設定 PSNo 與 TestRecipe 後執行查詢。</p>
      <p v-else-if="queryResults.length === 0" class="empty-state">沒有符合條件的記錄。</p>
      <div v-else class="table-scroll"><table><thead><tr><th>LogGroupSerial</th><th>ComponentNo</th><th>ProductNo</th><th>LotNo</th><th>OPNo</th><th>EquipmentNo</th><th>TestRecipeType</th><th>TestRecipe</th><th>TestDate</th><th>ImportDate</th><th>OPName</th><th>FileName</th></tr></thead><tbody>
        <tr v-for="row in queryResults" :key="row.id" :class="{ selected: selectedId === row.id }" tabindex="0" @click="selectedId = row.id" @keydown.enter="selectedId = row.id"><td>{{ row.logGroupSerial }}</td><td>{{ row.componentNo }}</td><td>{{ row.productNo }}</td><td>{{ row.lotNo }}</td><td>{{ row.opNo }}</td><td>{{ row.equipmentNo }}</td><td>{{ row.testRecipeType }}</td><td>{{ row.testRecipe }}</td><td>{{ displayDate(row.testDate) }}</td><td>{{ displayDate(row.importDate) }}</td><td>{{ row.opName }}</td><td>{{ row.fileName }}</td></tr>
      </tbody></table></div>
    </section>

    <div v-if="picker" class="modal-backdrop" @click.self="picker = null">
      <section class="picker-modal" role="dialog" aria-modal="true" :aria-label="picker === 'ps' ? '選擇 PSNo' : picker === 'recipe' ? '選擇 TestRecipe' : '選擇 EquipmentNo'">
        <header><h2>{{ picker === 'ps' ? '選擇 PSNo' : picker === 'recipe' ? '選擇 TestRecipe' : '選擇主設備' }}</h2><button type="button" aria-label="關閉" @click="picker = null">×</button></header>
        <div v-if="picker === 'ps'" class="choice-list"><label v-for="item in productionSegments" :key="item"><input v-model="draftPsNo" type="radio" name="psno" :value="item">{{ item }}</label></div>
        <div v-else-if="picker === 'recipe'" class="choice-list"><label v-for="item in availableRecipes" :key="item.key"><input type="checkbox" :checked="draftRecipeKeys.includes(item.key)" @change="toggleRecipe(item.key)"><span><strong>{{ item.testRecipeType }} / {{ item.testRecipeNo }}</strong><small>{{ item.itemName }}</small></span></label><p v-if="availableRecipes.length === 0">此 PSNo 沒有 DEV 測試項目。</p></div>
        <div v-else class="choice-list"><label><input v-model="draftEquipmentNo" type="radio" name="equipment" value="">全部主設備</label><label v-for="item in equipmentOptions" :key="item"><input v-model="draftEquipmentNo" type="radio" name="equipment" :value="item">{{ item }}</label></div>
        <footer><button type="button" @click="picker = null">Cancel</button><button class="primary-button" type="button" @click="confirmPicker">Apply</button></footer>
      </section>
    </div>

    <div v-if="analysisOpen && analysisRecord" class="analysis-backdrop" @click.self="analysisOpen = false">
      <section class="analysis-modal" role="dialog" aria-modal="true" aria-label="常態分析圖與原始量測值">
        <header class="analysis-heading"><div><p class="eyebrow">NORMALITY ANALYSIS · DEV</p><h2>{{ analysisRecord.testRecipeType }} / {{ analysisRecord.testRecipe }}</h2><p>{{ analysisRecord.lotNo }} · {{ analysisRecord.componentNo }} · {{ analysisRecord.equipmentNo }} · {{ displayDate(analysisRecord.testDate) }}</p></div><div class="analysis-actions"><label>Group count <input v-model.number="groupCount" type="number" min="8" max="32"></label><label>Sigma <input v-model.number="sigmaTimes" type="number" min="0.1" step="0.1"></label><button type="button" @click="saveGraph">Save Graph</button><button type="button" @click="analysisOpen = false">Close</button></div></header>
        <div class="analysis-content">
          <section class="chart-card"><div class="chart-title"><h3>Measurement Histogram</h3><span>{{ statistics.count }} / {{ measurementRows.length }} included</span></div>
            <svg id="rtb02-histogram" viewBox="0 0 820 280" role="img" aria-label="量測值直方圖">
              <line x1="56" y1="18" x2="56" y2="228" class="axis-line"/><line x1="56" y1="228" x2="800" y2="228" class="axis-line"/>
              <line v-for="tick in [0, 0.25, 0.5, 0.75, 1]" :key="tick" x1="56" :y1="228 - tick * 210" x2="800" :y2="228 - tick * 210" class="grid-line"/>
              <rect v-for="bar in chartBars" :key="bar.index" :x="bar.x" :y="bar.y" :width="bar.width" :height="bar.height" rx="2" class="histogram-bar"><title>{{ bar.from.toFixed(4) }} – {{ bar.to.toFixed(4) }}: {{ bar.frequency }}</title></rect>
              <path v-if="normalCurvePath" :d="normalCurvePath" class="normal-curve" stroke-width="2" />
              <text v-for="bar in chartBars" :key="`label-${bar.index}`" :x="bar.x + bar.width / 2" :y="bar.labelY" text-anchor="middle" class="axis-label">{{ bar.median.toFixed(3) }}</text>
              <text x="430" y="270" text-anchor="middle" class="axis-title">Measure Data</text><text x="15" y="125" text-anchor="middle" class="axis-title" transform="rotate(-90 15 125)">Frequency</text>
            </svg>
            <p class="chart-note">DEV 假量測值即時計算；鈴形曲線為樣本常態參考。US／LS／Target 未由此來源載入，能力指標與常態檢定 P-Value 不顯示。</p>
          </section>
          <section class="stats-row" aria-label="統計摘要"><article v-for="item in statisticCards" :key="item.key"><span>{{ item.label }}</span><strong>{{ item.value }}</strong></article></section>
          <details class="stat-settings"><summary>統計欄位</summary><div><label v-for="item in statChoices" :key="item.key"><input type="checkbox" :checked="visibleStats.includes(item.key)" @change="toggleStat(item.key)">{{ item.label }}</label></div></details>
          <section class="frequency-card"><h3>Frequency Groups</h3><div class="table-scroll"><table><thead><tr><th>FromValue</th><th>ToValue</th><th>Median</th><th>Number Of Times</th></tr></thead><tbody><tr v-for="bin in bins" :key="bin.index"><td>{{ bin.from.toFixed(4) }}</td><td>{{ bin.to.toFixed(4) }}</td><td>{{ bin.median.toFixed(4) }}</td><td>{{ bin.frequency }}</td></tr></tbody></table></div></section>
          <section class="measurement-card"><div class="chart-title"><h3>Test Data Detail</h3><span>取消 Excluded 可即時重新計算圖表與摘要</span></div><div class="table-scroll detail-scroll"><table><thead><tr><th>Sequence</th><th>Measure Data</th><th>Excluded</th><th>TestRecipe</th><th>TestDate</th><th>EquipmentNo</th></tr></thead><tbody><tr v-for="row in measurementRows" :key="row.sequence" :class="{ excluded: row.excluded }"><td>{{ row.sequence }}</td><td>{{ row.value.toFixed(4) }}</td><td><input v-model="row.excluded" type="checkbox" :aria-label="`排除第 ${row.sequence} 筆`"></td><td>{{ analysisRecord.testRecipe }}</td><td>{{ displayDate(analysisRecord.testDate) }}</td><td>{{ analysisRecord.equipmentNo }}</td></tr></tbody></table></div></section>
        </div>
      </section>
    </div>
  </section>
</template>

<style scoped>
.wafer-normality { box-sizing: border-box; min-height: 0; height: 100%; overflow: auto; padding: 12px; background: #f7f5ee; color: var(--ink, #26251f); font: inherit; }
.page-heading,.panel-heading,.analysis-heading,.chart-title { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.page-heading { margin-bottom: 12px; }
.page-heading h1,.analysis-heading h2 { margin: 1px 0 0; font-size: 20px; }
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
.primary-button,.panel-heading button { border-color: #3d493b; background: #3d493b; color: white; }
.panel-heading h2,.chart-card h3,.frequency-card h3,.measurement-card h3 { margin: 0; font-size: 14px; }
.empty-state { margin: 16px 0 4px; color: #797568; font-size: 13px; }
.table-scroll { max-width: 100%; overflow: auto; margin-top: 10px; }
table { width: 100%; border-collapse: collapse; font-size: 11px; white-space: nowrap; }
th,td { padding: 6px 8px; border: 1px solid #ddd9d0; text-align: left; }
th { position: sticky; top: 0; z-index: 1; background: #efede5; color: #514e44; font-size: 10px; }
tbody tr { cursor: pointer; }
tbody tr:hover { background: #f5f3ec; }
tbody tr.selected { background: #e7eedf; }
.modal-backdrop,.analysis-backdrop { position: fixed; inset: 0; z-index: 1000; display: grid; place-items: center; padding: 20px; background: rgb(20 20 18 / 48%); }
.picker-modal { box-sizing: border-box; width: min(440px, 100%); max-height: 82vh; display: flex; flex-direction: column; border-radius: 5px; background: #fffefa; box-shadow: 0 12px 40px rgb(0 0 0 / 24%); }
.picker-modal > header,.picker-modal > footer { display: flex; align-items: center; justify-content: space-between; gap: 8px; padding: 12px; border-bottom: 1px solid #e0dcd2; }
.picker-modal > header h2 { margin: 0; font-size: 15px; }
.picker-modal > header button { border: 0; background: transparent; font-size: 22px; }
.picker-modal > footer { justify-content: flex-end; border-top: 1px solid #e0dcd2; border-bottom: 0; }
.choice-list { display: grid; gap: 3px; overflow: auto; padding: 12px; }
.choice-list label { display: flex; align-items: center; gap: 8px; padding: 8px; border-radius: 3px; background: #f8f6f0; cursor: pointer; }
.choice-list label span { display: grid; gap: 3px; }
.choice-list small { color: #777366; }
.choice-list p { color: #777366; font-size: 12px; }
.analysis-backdrop { z-index: 1010; place-items: stretch; padding: 2vh 2vw; }
.analysis-modal { box-sizing: border-box; display: flex; min-width: 0; max-height: 96vh; flex-direction: column; overflow: hidden; border-radius: 5px; background: #f7f5ee; box-shadow: 0 12px 40px rgb(0 0 0 / 30%); }
.analysis-heading { align-items: flex-start; padding: 14px 16px; border-bottom: 1px solid #d2cec2; background: #fffefa; }
.analysis-heading > div:first-child { min-width: 220px; }
.analysis-heading p:last-child { margin: 5px 0 0; color: #6d695d; font-size: 11px; }
.analysis-actions { display: flex; flex-wrap: wrap; justify-content: flex-end; align-items: center; gap: 6px; }
.analysis-actions label { display: flex; align-items: center; gap: 5px; color: #5e5a4f; font-size: 11px; }
.analysis-actions input { box-sizing: border-box; width: 58px; min-height: 28px; padding: 3px; border: 1px solid #bbb7ac; border-radius: 3px; font: inherit; }
.analysis-content { overflow: auto; padding: 12px 16px 18px; }
.chart-card,.frequency-card,.measurement-card { padding: 10px; border: 1px solid #d2cec2; border-radius: 4px; background: #fffefa; }
.chart-title { align-items: baseline; }
.chart-title span { color: #777366; font-size: 11px; }
.chart-card svg { display: block; width: 100%; min-height: 170px; margin-top: 8px; background: #fff; }
.axis-line { stroke: #68665d; stroke-width: 1; }
.grid-line { stroke: #e8e5dc; stroke-width: 1; }
.axis-label,.axis-title { fill: #68665d; font-size: 10px; }
.axis-title { font-size: 11px; }
.histogram-bar { fill: #8299af; stroke: #536d84; stroke-width: 1; }
.normal-curve { fill: none; stroke: #a7473d; stroke-linecap: round; stroke-linejoin: round; }
.chart-note { margin: 6px 0 0; color: #777366; font-size: 11px; }
.stats-row { display: grid; grid-template-columns: repeat(5, minmax(90px, 1fr)); gap: 8px; margin: 10px 0; }
.stats-row article { display: grid; gap: 4px; padding: 9px 10px; border: 1px solid #d2cec2; border-radius: 4px; background: #fffefa; }
.stats-row span { color: #777366; font-size: 10px; }
.stats-row strong { font-size: 15px; font-variant-numeric: tabular-nums; }
.stat-settings { margin: 8px 0 10px; color: #5e5a4f; font-size: 11px; }
.stat-settings summary { cursor: pointer; }
.stat-settings > div { display: flex; flex-wrap: wrap; gap: 8px 14px; padding: 9px 0; }
.frequency-card { margin-bottom: 10px; }
.frequency-card .table-scroll { max-height: 160px; }
.measurement-card .detail-scroll { max-height: 230px; }
.measurement-card tbody tr { cursor: default; }
.measurement-card tbody tr.excluded { color: #969185; background: #f5f2ec; text-decoration: line-through; }
.measurement-card input { width: 15px; height: 15px; }
@media (max-width: 900px) { .filter-grid { grid-template-columns: repeat(2, minmax(180px, 1fr)); } .analysis-heading { flex-direction: column; } .analysis-actions { justify-content: flex-start; } }
@media (max-width: 580px) { .filter-grid { grid-template-columns: 1fr; } .filter-actions { align-items: flex-start; flex-direction: column; } .stats-row { grid-template-columns: repeat(2, minmax(90px, 1fr)); } }
</style>
