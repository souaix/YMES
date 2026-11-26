<script setup lang="ts">
import { computed, ref } from 'vue'

interface RecipeTypeOption {
  key: string
  psNo: string
  testRecipeType: string
  recipeNos: string[]
}

interface MapPoint {
  x: number
  y: number
  value: number
}

interface ReportRecord {
  id: string
  psNo: string
  logGroupSerial: string
  testRecipeType: string
  lotNo: string
  opNo: string
  opName: string
  equipmentNo: string
  componentNo: string
  testDate: string
  importDate: string
  productNo: string
  productVersion: string
  maps: Record<string, MapPoint[]>
  checkFlag: boolean
}

type PickerName = 'ps' | 'testRecipeType' | 'equipment' | null
type StatisticName = 'MaxValue' | 'MinValue' | 'MeanValue' | 'Sigma'

interface MapCard {
  recipeNo: string
  points: Array<MapPoint & { viewX: number; viewY: number }>
  statistics: Array<{ name: string; label: string; value: number }>
}

interface ChartReport {
  row: ReportRecord
  maps: MapCard[]
}

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

const productionSegments = ['PS-WP-01', 'PS-WP-02', 'PS-WP-03']
const recipeTypes: RecipeTypeOption[] = [
  { key: 'PS-WP-01|WAFER_DC', psNo: 'PS-WP-01', testRecipeType: 'WAFER_DC', recipeNos: ['VTH_CHECK', 'IDSAT', 'IGSS'] },
  { key: 'PS-WP-01|WAFER_LEAK', psNo: 'PS-WP-01', testRecipeType: 'WAFER_LEAK', recipeNos: ['GATE_LEAK', 'JUNCTION_LEAK'] },
  { key: 'PS-WP-02|WAFER_DC', psNo: 'PS-WP-02', testRecipeType: 'WAFER_DC', recipeNos: ['VTH_CHECK', 'IDSAT'] },
  { key: 'PS-WP-02|WAFER_LEAK', psNo: 'PS-WP-02', testRecipeType: 'WAFER_LEAK', recipeNos: ['GATE_LEAK', 'JUNCTION_LEAK'] },
  { key: 'PS-WP-03|WAFER_DC', psNo: 'PS-WP-03', testRecipeType: 'WAFER_DC', recipeNos: ['VTH_CHECK', 'IDSAT', 'IGSS'] },
]

const waferCoordinates = Array.from({ length: 7 }, (_, row) => row - 3)
  .flatMap((y) => Array.from({ length: 7 }, (_, column) => column - 3)
    .filter((x) => (x * x) + (y * y) <= 9)
    .map((x) => ({ x, y })))

function makeMapValues(testRecipeType: string, center: number, seed: number) {
  const option = recipeTypes.find((item) => item.testRecipeType === testRecipeType)
  const recipeNos = option?.recipeNos ?? []
  return Object.fromEntries(recipeNos.map((recipeNo, recipeIndex) => {
    const recipeCenter = center + (recipeIndex * (testRecipeType === 'WAFER_DC' ? 0.22 : 0.035))
    const points = waferCoordinates.map(({ x, y }, index) => {
      const wave = Math.sin((x + seed) * 1.7) * 0.018 + Math.cos((y - seed) * 1.3) * 0.012
      const edgeDrift = (x * 0.0013) + (y * -0.0008)
      const value = recipeCenter + wave + edgeDrift + ((index % 5) - 2) * 0.0007
      return { x, y, value: Number(value.toFixed(5)) }
    })
    return [recipeNo, points]
  })) as Record<string, MapPoint[]>
}

const rawRecords: ReportRecord[] = [
  { id: 'rtb03-001', psNo: 'PS-WP-01', logGroupSerial: 'LG-WP-261120-001', testRecipeType: 'WAFER_DC', lotNo: 'W26112001', opNo: 'WP20', opName: 'Wafer Probe', equipmentNo: 'WPROBE-03', componentNo: 'WFR-001', testDate: '2026-11-20T08:12:41', importDate: '2026-11-20T08:16:03', productNo: 'DEV-IC-240', productVersion: 'A1', maps: makeMapValues('WAFER_DC', 1.024, 1), checkFlag: false },
  { id: 'rtb03-002', psNo: 'PS-WP-01', logGroupSerial: 'LG-WP-261120-002', testRecipeType: 'WAFER_DC', lotNo: 'W26112001', opNo: 'WP20', opName: 'Wafer Probe', equipmentNo: 'WPROBE-03', componentNo: 'WFR-002', testDate: '2026-11-20T08:29:18', importDate: '2026-11-20T08:32:51', productNo: 'DEV-IC-240', productVersion: 'A1', maps: makeMapValues('WAFER_DC', 1.019, 2), checkFlag: false },
  { id: 'rtb03-003', psNo: 'PS-WP-01', logGroupSerial: 'LG-WP-261120-003', testRecipeType: 'WAFER_DC', lotNo: 'W26112001', opNo: 'WP20', opName: 'Wafer Probe', equipmentNo: 'WPROBE-05', componentNo: 'WFR-003', testDate: '2026-11-20T09:02:27', importDate: '2026-11-20T09:06:42', productNo: 'DEV-IC-240', productVersion: 'A1', maps: makeMapValues('WAFER_DC', 1.028, 3), checkFlag: false },
  { id: 'rtb03-004', psNo: 'PS-WP-01', logGroupSerial: 'LG-WP-261121-004', testRecipeType: 'WAFER_LEAK', lotNo: 'W26112104', opNo: 'WP21', opName: 'Leakage Probe', equipmentNo: 'WPROBE-09', componentNo: 'WFR-004', testDate: '2026-11-21T11:40:33', importDate: '2026-11-21T11:43:56', productNo: 'DEV-IC-180', productVersion: 'B2', maps: makeMapValues('WAFER_LEAK', 0.084, 4), checkFlag: false },
  { id: 'rtb03-005', psNo: 'PS-WP-02', logGroupSerial: 'LG-WP-261121-005', testRecipeType: 'WAFER_DC', lotNo: 'W26112104', opNo: 'WP20', opName: 'Wafer Probe', equipmentNo: 'WPROBE-05', componentNo: 'WFR-005', testDate: '2026-11-21T12:03:15', importDate: '2026-11-21T12:06:28', productNo: 'DEV-IC-240', productVersion: 'A1', maps: makeMapValues('WAFER_DC', 1.021, 5), checkFlag: false },
  { id: 'rtb03-006', psNo: 'PS-WP-02', logGroupSerial: 'LG-WP-261122-006', testRecipeType: 'WAFER_LEAK', lotNo: 'W26112208', opNo: 'WP21', opName: 'Leakage Probe', equipmentNo: 'WPROBE-09', componentNo: 'WFR-006', testDate: '2026-11-22T07:23:52', importDate: '2026-11-22T07:29:31', productNo: 'DEV-IC-180', productVersion: 'B2', maps: makeMapValues('WAFER_LEAK', 0.091, 6), checkFlag: false },
  { id: 'rtb03-007', psNo: 'PS-WP-03', logGroupSerial: 'LG-WP-261123-007', testRecipeType: 'WAFER_DC', lotNo: 'W26112312', opNo: 'WP20', opName: 'Wafer Probe', equipmentNo: 'WPROBE-03', componentNo: 'WFR-007', testDate: '2026-11-23T15:08:05', importDate: '2026-11-23T15:12:18', productNo: 'DEV-IC-240', productVersion: 'A1', maps: makeMapValues('WAFER_DC', 1.031, 7), checkFlag: false },
  { id: 'rtb03-008', psNo: 'PS-WP-03', logGroupSerial: 'LG-WP-261124-008', testRecipeType: 'WAFER_DC', lotNo: 'W26112415', opNo: 'WP20', opName: 'Wafer Probe', equipmentNo: 'WPROBE-05', componentNo: 'WFR-008', testDate: '2026-11-24T13:16:39', importDate: '2026-11-24T13:21:07', productNo: 'DEV-IC-240', productVersion: 'A1', maps: makeMapValues('WAFER_DC', 1.017, 8), checkFlag: false },
]

const equipmentOptions = ['WPROBE-03', 'WPROBE-05', 'WPROBE-09']
const statisticOptions: Array<{ name: StatisticName; label: string }> = [
  { name: 'MaxValue', label: 'Max Value' },
  { name: 'MinValue', label: 'Min Value' },
  { name: 'Sigma', label: 'Sigma' },
  { name: 'MeanValue', label: 'Mean Value' },
]

const psNo = ref('')
const selectedTypeKeys = ref<string[]>([])
const equipmentNo = ref('')
const componentNo = ref('')
const lotNo = ref('')
const includeEquipment = ref(false)
const includeComponent = ref(false)
const includeLot = ref(false)
const includeTestDate = ref(false)
const fromTestDate = ref('2026-11-20T00:00:00')
const toTestDate = ref('2026-11-26T23:59:59')
const selectedStatistics = ref<StatisticName[]>([])
const resultRows = ref<ReportRecord[]>([])
const selectedIds = ref<string[]>([])
const hasQueried = ref(false)
const picker = ref<PickerName>(null)
const draftPsNo = ref('')
const draftTypeKeys = ref<string[]>([])
const draftEquipmentNo = ref('')
const chartOpen = ref(false)
const notice = ref('選取 PSNo 與一個以上 TestRecipeType 後按 Query。')
const noticeError = ref(false)

const availableTypes = computed(() => recipeTypes.filter((item) => item.psNo === psNo.value))
const typeSummary = computed(() => selectedTypeKeys.value
  .map((key) => recipeTypes.find((item) => item.key === key)?.testRecipeType)
  .filter((value): value is string => Boolean(value))
  .join('; '))
const selectedRows = computed(() => resultRows.value.filter((row) => selectedIds.value.includes(row.id)))
const allRowsSelected = computed(() => resultRows.value.length > 0 && selectedRows.value.length === resultRows.value.length)

const chartReports = computed<ChartReport[]>(() => selectedRows.value.map((row) => {
  const recipeType = recipeTypes.find((item) => item.psNo === psNo.value && item.testRecipeType === row.testRecipeType)
  const maps = (recipeType?.recipeNos ?? []).map((recipeNo) => {
    const points = row.maps[recipeNo] ?? []
    const maxRadius = Math.max(...points.map((point) => Math.sqrt((point.x * point.x) + (point.y * point.y))), 1) * 1.1
    const positioned = points.map((point) => ({
      ...point,
      viewX: 130 + (point.x / maxRadius) * 98,
      viewY: 130 - (point.y / maxRadius) * 98,
    }))
    const values = points.map((point) => point.value)
    const mean = values.reduce((total, value) => total + value, 0) / (values.length || 1)
    const sigma = Math.sqrt(values.reduce((total, value) => total + ((value - mean) ** 2), 0) / (values.length || 1))
    const stats: Record<StatisticName, number> = {
      MaxValue: Math.max(...values),
      MinValue: Math.min(...values),
      MeanValue: mean,
      Sigma: sigma,
    }
    return {
      recipeNo,
      points: positioned,
      statistics: statisticOptions
        .filter((item) => selectedStatistics.value.includes(item.name))
        .map((item) => ({ ...item, value: Number(stats[item.name].toFixed(6)) })),
    }
  })
  return { row, maps }
}))

function openPicker(name: Exclude<PickerName, null>) {
  if (name === 'testRecipeType' && !psNo.value) {
    notice.value = '請先選擇 PSNo。'
    noticeError.value = true
    return
  }
  if (name === 'ps') draftPsNo.value = psNo.value
  if (name === 'testRecipeType') draftTypeKeys.value = [...selectedTypeKeys.value]
  if (name === 'equipment') draftEquipmentNo.value = equipmentNo.value
  picker.value = name
}

function confirmPicker() {
  let resetQuery = false
  if (picker.value === 'ps') {
    if (psNo.value !== draftPsNo.value) {
      psNo.value = draftPsNo.value
      selectedTypeKeys.value = []
      resetQuery = true
    }
  } else if (picker.value === 'testRecipeType') {
    selectedTypeKeys.value = [...draftTypeKeys.value]
    resetQuery = true
  } else if (picker.value === 'equipment') {
    equipmentNo.value = draftEquipmentNo.value
    includeEquipment.value = Boolean(draftEquipmentNo.value)
  }
  picker.value = null
  if (resetQuery) {
    resultRows.value = []
    selectedIds.value = []
    hasQueried.value = false
  }
}

function toggleType(key: string) {
  draftTypeKeys.value = draftTypeKeys.value.includes(key)
    ? draftTypeKeys.value.filter((value) => value !== key)
    : [...draftTypeKeys.value, key]
}

function toggleAllRows() {
  selectedIds.value = allRowsSelected.value ? [] : resultRows.value.map((row) => row.id)
}

function query() {
  if (!psNo.value) {
    notice.value = 'PSNo 為必要條件。'
    noticeError.value = true
    return
  }
  const selectedTypes = availableTypes.value.filter((item) => selectedTypeKeys.value.includes(item.key)).map((item) => item.testRecipeType)
  if (selectedTypes.length === 0) {
    notice.value = '至少選擇一個 TestRecipeType。'
    noticeError.value = true
    return
  }
  if (includeTestDate.value && (!fromTestDate.value || !toTestDate.value || fromTestDate.value > toTestDate.value)) {
    notice.value = 'TestDate 起訖時間不完整，或起始時間晚於結束時間。'
    noticeError.value = true
    return
  }
  resultRows.value = rawRecords.filter((row) => row.psNo === psNo.value && selectedTypes.includes(row.testRecipeType)
    && (!includeEquipment.value || !equipmentNo.value.trim() || row.equipmentNo === equipmentNo.value.trim())
    && (!includeComponent.value || !componentNo.value.trim() || row.componentNo === componentNo.value.trim())
    && (!includeLot.value || !lotNo.value.trim() || row.lotNo === lotNo.value.trim())
    && (!includeTestDate.value || (row.testDate >= fromTestDate.value && row.testDate <= toTestDate.value)))
    .map((row) => ({ ...row, checkFlag: false }))
  selectedIds.value = []
  hasQueried.value = true
  notice.value = resultRows.value.length
    ? `DEV 假資料：找到 ${resultRows.value.length} 筆含座標的 Wafer 測試記錄。`
    : 'DEV 假資料：沒有符合條件的 Wafer Map 記錄。'
  noticeError.value = false
}

function openChart() {
  if (selectedRows.value.length === 0) {
    notice.value = '請先勾選至少一筆測試記錄。'
    noticeError.value = true
    return
  }
  chartOpen.value = true
}

function onEquipmentChange() {
  if (!includeEquipment.value) equipmentNo.value = ''
}

function onComponentChange() {
  if (!includeComponent.value) componentNo.value = ''
}

function onLotChange() {
  if (!includeLot.value) lotNo.value = ''
}

function displayDate(value: string) {
  return value.replace('T', ' ')
}

function xmlText(value: string) {
  return value.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;').replaceAll("'", '&apos;')
}

function saveGraph() {
  const tileWidth = 270
  const tileHeight = 310
  const columns = 3
  const gap = 12
  const width = 48 + (columns * tileWidth) + ((columns - 1) * gap)
  let y = 64
  const groups: string[] = []
  for (const report of chartReports.value) {
    groups.push(`<text x="24" y="${y}" font-family="Arial,sans-serif" font-size="16" font-weight="700">TestRecipeType: ${xmlText(report.row.testRecipeType)} · WaferId: ${xmlText(report.row.componentNo)} · TestDate: ${xmlText(displayDate(report.row.testDate))}</text>`)
    y += 12
    report.maps.forEach((map, index) => {
      const x = 24 + (index % columns) * (tileWidth + gap)
      const rowY = y + Math.floor(index / columns) * (tileHeight + gap)
      const statsText = map.statistics.map((item, statIndex) => `<text x="${x + 8}" y="${rowY + 284 + statIndex * 15}" font-family="Arial,sans-serif" font-size="11">${xmlText(item.label)}: ${item.value.toFixed(6)}</text>`).join('')
      const points = map.points.map((point) => `<g><circle cx="${x + 135 + (point.viewX - 130)}" cy="${rowY + 150 + (point.viewY - 130)}" r="2.2" fill="#d72f35"/><text x="${x + 135 + (point.viewX - 130)}" y="${rowY + 154 + (point.viewY - 130)}" text-anchor="middle" font-family="Arial,sans-serif" font-size="8" fill="#25251f">${point.value.toFixed(4)}<title>X ${point.x}, Y ${point.y}: ${point.value}</title></text></g>`).join('')
      groups.push(`<g><rect x="${x}" y="${rowY}" width="${tileWidth}" height="${tileHeight}" rx="8" fill="#fff" stroke="#d7d1c2"/><text x="${x + 135}" y="${rowY + 24}" text-anchor="middle" font-family="Arial,sans-serif" font-size="13" font-weight="700">${xmlText(map.recipeNo)}</text><circle cx="${x + 135}" cy="${rowY + 150}" r="102" fill="#fff" stroke="#3d3b34" stroke-width="1.4"/>${points}${statsText}</g>`)
    })
    y += Math.ceil(report.maps.length / columns) * (tileHeight + gap) + 24
  }
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${y + 24}" viewBox="0 0 ${width} ${y + 24}"><rect width="100%" height="100%" fill="#f7f5ee"/><text x="${width / 2}" y="34" text-anchor="middle" font-family="Arial,sans-serif" font-size="22" font-weight="700">Test Report</text><path d="M24 46 H${width - 24}" stroke="#777" stroke-width="2"/>${groups.join('')}</svg>`
  const url = URL.createObjectURL(new Blob([svg], { type: 'image/svg+xml;charset=utf-8' }))
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = 'wafer-map-test-report.svg'
  anchor.click()
  window.setTimeout(() => URL.revokeObjectURL(url), 1000)
}
</script>

<template>
  <section class="wafer-map-page" aria-label="Wafer Map 電性測試圖表 DEV 預覽">
    <header class="page-heading">
      <div><p class="eyebrow">RTB03 · READ ONLY REPORT</p><h1>Wafer Map 電性測試圖表</h1></div>
      <span class="dev-badge">DEV 假資料</span>
    </header>

    <section class="filter-panel" aria-label="查詢條件">
      <div class="filter-grid">
        <label class="filter-field"><span>PSNo</span><div class="picker-field"><input :value="psNo" readonly placeholder="請選擇製程" @click="openPicker('ps')"><button type="button" aria-label="選擇 PSNo" @click="openPicker('ps')">…</button></div></label>
        <label class="filter-field"><span>TestRecipeType</span><div class="picker-field"><input :value="typeSummary" readonly placeholder="請先選 PSNo，再選測試類別" @click="openPicker('testRecipeType')"><button type="button" aria-label="選擇 TestRecipeType" @click="openPicker('testRecipeType')">…</button></div></label>
        <label class="filter-field"><span class="check-caption"><input v-model="includeEquipment" type="checkbox" @change="onEquipmentChange"> EquipmentNo</span><div class="picker-field"><input v-model="equipmentNo" placeholder="設備編號" @input="includeEquipment = Boolean(equipmentNo.trim())"><button type="button" aria-label="選擇 EquipmentNo" @click="openPicker('equipment')">…</button></div></label>
        <label class="filter-field"><span class="check-caption"><input v-model="includeComponent" type="checkbox" @change="onComponentChange"> ComponentNo</span><input v-model="componentNo" type="text" placeholder="元件編號（完全相同）" @input="includeComponent = Boolean(componentNo.trim())"></label>
        <label class="filter-field"><span class="check-caption"><input v-model="includeLot" type="checkbox" @change="onLotChange"> LotNo</span><input v-model="lotNo" type="text" placeholder="批號（完全相同）" @input="includeLot = Boolean(lotNo.trim())"></label>
        <label class="filter-field date-field"><span class="check-caption"><input v-model="includeTestDate" type="checkbox"> TestDate</span><div><input v-model="fromTestDate" type="datetime-local" step="1"><span>To</span><input v-model="toTestDate" type="datetime-local" step="1"></div></label>
      </div>
      <fieldset class="stat-options"><legend>Test Data Statistics View</legend><label v-for="stat in statisticOptions" :key="stat.name"><input v-model="selectedStatistics" type="checkbox" :value="stat.name">{{ stat.label }}</label></fieldset>
      <div class="filter-actions"><p :class="['notice', noticeError ? 'notice-error' : '']" role="status">{{ notice }}</p><div><button class="primary-button" type="button" @click="query">Query</button><button type="button" @click="emit('close')">Close</button></div></div>
    </section>

    <section class="results-panel" aria-label="Wafer 測試記錄">
      <div class="panel-heading"><div><h2>Wafer Test Records</h2><span v-if="selectedRows.length" class="selected-count">已選 {{ selectedRows.length }} 筆</span></div><button type="button" :disabled="selectedRows.length === 0" @click="openChart">Enable Chart</button></div>
      <p v-if="!hasQueried" class="empty-state">設定 PSNo 與 TestRecipeType 後執行查詢。</p>
      <p v-else-if="resultRows.length === 0" class="empty-state">沒有符合條件的 Wafer Map 記錄。</p>
      <div v-else class="table-scroll"><table><thead><tr><th><input type="checkbox" :checked="allRowsSelected" aria-label="全選" @change="toggleAllRows"></th><th>TestRecipeType</th><th>LotNo</th><th>OPNo</th><th>OPName</th><th>EquipmentNo</th><th>ComponentNo</th><th>TestDate</th><th>ImportDate</th><th>LogGroupSerial</th></tr></thead><tbody>
        <tr v-for="row in resultRows" :key="row.id" :class="{ selected: selectedIds.includes(row.id) }"><td><input v-model="selectedIds" type="checkbox" :value="row.id" :aria-label="`選取 ${row.logGroupSerial}`"></td><td>{{ row.testRecipeType }}</td><td>{{ row.lotNo }}</td><td>{{ row.opNo }}</td><td>{{ row.opName }}</td><td>{{ row.equipmentNo }}</td><td>{{ row.componentNo }}</td><td>{{ displayDate(row.testDate) }}</td><td>{{ displayDate(row.importDate) }}</td><td>{{ row.logGroupSerial }}</td></tr>
      </tbody></table></div>
    </section>

    <div v-if="picker" class="modal-backdrop" @click.self="picker = null">
      <section class="picker-modal" role="dialog" aria-modal="true" :aria-label="picker === 'ps' ? '選擇 PSNo' : picker === 'testRecipeType' ? '選擇 TestRecipeType' : '選擇 EquipmentNo'">
        <header><h2>{{ picker === 'ps' ? '選擇 PSNo' : picker === 'testRecipeType' ? '選擇 TestRecipeType' : '選擇 EquipmentNo' }}</h2><button type="button" aria-label="關閉" @click="picker = null">×</button></header>
        <div v-if="picker === 'ps'" class="choice-list"><label v-for="item in productionSegments" :key="item"><input v-model="draftPsNo" type="radio" name="rtb03-psno" :value="item">{{ item }}</label></div>
        <div v-else-if="picker === 'testRecipeType'" class="choice-list"><label v-for="item in availableTypes" :key="item.key"><input type="checkbox" :checked="draftTypeKeys.includes(item.key)" @change="toggleType(item.key)"><span><strong>{{ item.testRecipeType }}</strong><small>TestRecipeNo: {{ item.recipeNos.join(', ') }}</small></span></label><p v-if="availableTypes.length === 0">此 PSNo 沒有 DEV 測試類別。</p></div>
        <div v-else class="choice-list"><label><input v-model="draftEquipmentNo" type="radio" name="rtb03-equipment" value="">全部設備</label><label v-for="item in equipmentOptions" :key="item"><input v-model="draftEquipmentNo" type="radio" name="rtb03-equipment" :value="item">{{ item }}</label></div>
        <footer><button type="button" @click="picker = null">Cancel</button><button class="primary-button" type="button" @click="confirmPicker">Apply</button></footer>
      </section>
    </div>

    <div v-if="chartOpen" class="chart-backdrop" @click.self="chartOpen = false">
      <section class="chart-modal" role="dialog" aria-modal="true" aria-label="Wafer Map Test Report">
        <header class="chart-heading"><div><p class="eyebrow">WAFER MAP · DEV</p><h2>Test Report</h2><p>{{ chartReports.length }} 筆 Wafer 記錄；每個 TestRecipeNo 依 X/Y 座標顯示電性量測值。</p></div><div><button type="button" @click="saveGraph">Save Graph (SVG)</button><button type="button" @click="chartOpen = false">Close</button></div></header>
        <div class="report-list"><article v-for="report in chartReports" :key="report.row.id" class="report-card">
          <header><h3>TestRecipeType: {{ report.row.testRecipeType }}</h3><p>WaferId: {{ report.row.componentNo }} · LotNo: {{ report.row.lotNo }} · {{ report.row.opNo }} {{ report.row.opName }} · {{ report.row.equipmentNo }} · {{ displayDate(report.row.testDate) }}</p></header>
          <div class="map-grid"><section v-for="map in report.maps" :key="map.recipeNo" class="map-card"><h4>{{ map.recipeNo }}</h4><svg class="wafer-svg" viewBox="0 0 260 260" role="img" :aria-label="`${report.row.componentNo} ${map.recipeNo} wafer map`"><circle cx="130" cy="130" r="102" class="wafer-outline"/><g v-for="point in map.points" :key="`${point.x}:${point.y}`"><circle :cx="point.viewX" :cy="point.viewY" r="2.1" class="measurement-dot"/><text :x="point.viewX" :y="point.viewY + 3" text-anchor="middle" class="measurement-value">{{ point.value.toFixed(4) }}<title>X {{ point.x }}, Y {{ point.y }} · {{ point.value }}</title></text></g></svg><div v-if="map.statistics.length" class="map-statistics"><span v-for="stat in map.statistics" :key="stat.name">{{ stat.label }}: {{ stat.value.toFixed(6) }}</span></div></section></div>
        </article></div>
      </section>
    </div>
  </section>
</template>

<style scoped>
.wafer-map-page { box-sizing: border-box; min-height: 0; height: 100%; overflow: auto; padding: 18px; background: #f7f5ee; color: #26251f; font: 13px/1.45 Arial, "Microsoft JhengHei", sans-serif; }
.page-heading,.panel-heading,.chart-heading,.report-card>header,.filter-actions,.picker-modal>header,.picker-modal>footer { display: flex; align-items: center; justify-content: space-between; gap: 14px; }
.page-heading { margin: 0 0 16px; }
.page-heading h1,.chart-heading h2 { margin: 2px 0; font-size: 23px; }
.eyebrow { margin: 0; color: #827d6d; font-size: 10px; font-weight: 700; letter-spacing: .09em; }
.dev-badge { border: 1px solid #d6a95b; border-radius: 999px; padding: 5px 11px; background: #fff2d6; color: #805718; font-size: 11px; font-weight: 700; }
.filter-panel,.results-panel { border: 1px solid #d8d4c8; border-radius: 8px; background: #fff; box-shadow: 0 3px 12px #28230d0b; }
.filter-panel { padding: 14px; }
.filter-grid { display: grid; grid-template-columns: repeat(3,minmax(205px,1fr)); gap: 12px 16px; }
.filter-field { display: grid; gap: 5px; min-width: 0; color: #555146; font-size: 11px; font-weight: 700; }
.filter-field>input,.picker-field input,.date-field input { box-sizing: border-box; width: 100%; min-width: 0; height: 32px; border: 1px solid #d1ccbd; border-radius: 4px; padding: 5px 8px; background: #fffdf8; color: #292820; font: inherit; font-weight: 400; }
.picker-field { display: flex; }
.picker-field input { border-radius: 4px 0 0 4px; cursor: pointer; }
.picker-field button { width: 36px; border: 1px solid #d1ccbd; border-left: 0; border-radius: 0 4px 4px 0; background: #f2efe5; color: #454239; font-size: 17px; cursor: pointer; }
.check-caption { display: flex; align-items: center; gap: 6px; }
.date-field>div { display: flex; align-items: center; gap: 7px; }
.date-field>div span { color: #777; font-weight: 400; }
.date-field input { font-size: 11px; }
.stat-options { display: flex; flex-wrap: wrap; gap: 14px; margin: 13px 0 0; border: 1px solid #e2ded3; border-radius: 5px; padding: 8px 11px 10px; }
.stat-options legend { padding: 0 5px; color: #5d594d; font-size: 11px; font-weight: 700; }
.stat-options label { display: inline-flex; align-items: center; gap: 5px; color: #49463d; }
.filter-actions { margin-top: 12px; }
.filter-actions>div,.chart-heading>div:last-child { display: flex; gap: 8px; }
.notice { margin: 0; color: #777366; }
.notice-error { color: #b42318; }
button { min-height: 31px; border: 1px solid #c9c4b6; border-radius: 4px; padding: 5px 11px; background: #fff; color: #343229; font: inherit; cursor: pointer; }
button:hover { background: #f5f2e9; }
button:disabled { opacity: .5; cursor: not-allowed; }
.primary-button { border-color: #6b633e; background: #6b633e; color: #fff; }
.primary-button:hover { background: #554e31; }
.results-panel { margin-top: 14px; overflow: hidden; }
.panel-heading { padding: 10px 14px; border-bottom: 1px solid #e5e1d6; }
.panel-heading>div { display: flex; align-items: center; gap: 10px; }
.panel-heading h2 { margin: 0; font-size: 15px; }
.selected-count { color: #827d6d; font-size: 11px; }
.empty-state { margin: 0; padding: 28px 14px; color: #847f72; text-align: center; }
.table-scroll { max-height: 360px; overflow: auto; }
table { width: 100%; border-collapse: collapse; white-space: nowrap; font-size: 11px; }
th,td { border-bottom: 1px solid #eae7df; padding: 7px 9px; text-align: left; }
th { position: sticky; top: 0; background: #f3f0e7; color: #615d50; font-weight: 700; }
tbody tr:hover,tbody tr.selected { background: #f7f2e4; }
.modal-backdrop,.chart-backdrop { position: fixed; z-index: 1200; inset: 0; display: grid; place-items: center; background: #17171080; padding: 20px; }
.picker-modal,.chart-modal { width: min(540px,96vw); max-height: min(88vh,760px); overflow: hidden; border: 1px solid #d8d4c8; border-radius: 9px; background: #fff; box-shadow: 0 18px 55px #0004; }
.picker-modal>header,.picker-modal>footer { padding: 11px 14px; border-bottom: 1px solid #e7e3da; }
.picker-modal>header h2 { margin: 0; font-size: 16px; }
.picker-modal>header button { min-width: 30px; font-size: 19px; }
.choice-list { max-height: 58vh; overflow: auto; padding: 8px 14px; }
.choice-list label { display: flex; align-items: center; gap: 9px; padding: 9px 2px; border-bottom: 1px solid #eeeae2; cursor: pointer; }
.choice-list label span { display: grid; gap: 2px; }
.choice-list small { color: #777366; font-size: 10px; font-weight: 400; }
.choice-list p { color: #777366; }
.picker-modal>footer { justify-content: flex-end; border-top: 1px solid #e7e3da; border-bottom: 0; }
.chart-backdrop { padding: 24px; }
.chart-modal { display: flex; flex-direction: column; width: min(1180px,96vw); max-height: 92vh; }
.chart-heading { padding: 15px 18px; border-bottom: 1px solid #e4dfd4; }
.chart-heading p:last-child { margin: 5px 0 0; color: #777366; }
.report-list { overflow: auto; padding: 14px 18px 22px; background: #f7f5ee; }
.report-card { margin-bottom: 16px; border: 1px solid #ded9cd; border-radius: 8px; padding: 12px; background: white; }
.report-card>header { align-items: flex-start; flex-direction: column; gap: 2px; margin-bottom: 8px; }
.report-card h3,.map-card h4 { margin: 0; }
.report-card>header p { margin: 0; color: #777366; font-size: 11px; }
.map-grid { display: grid; grid-template-columns: repeat(auto-fit,minmax(238px,1fr)); gap: 10px; }
.map-card { min-width: 0; border: 1px solid #e4e0d7; border-radius: 7px; padding: 9px; background: #fffefa; text-align: center; }
.map-card h4 { color: #49463c; font-size: 12px; }
.wafer-svg { display: block; width: min(100%,260px); margin: 2px auto; }
.wafer-outline { fill: #fff; stroke: #38372f; stroke-width: 1.3; }
.measurement-dot { fill: #d72f35; }
.measurement-value { fill: #25251f; font: 8px Arial,sans-serif; }
.map-statistics { display: flex; flex-wrap: wrap; justify-content: center; gap: 3px 10px; min-height: 18px; color: #4c483d; font-size: 10px; }
@media(max-width:850px) { .filter-grid { grid-template-columns: repeat(2,minmax(175px,1fr)); } .chart-heading { align-items: flex-start; flex-direction: column; } }
@media(max-width:560px) { .wafer-map-page { padding: 10px; } .filter-grid { grid-template-columns: 1fr; } .filter-actions { align-items: flex-start; flex-direction: column; } .date-field>div { flex-wrap: wrap; } .date-field input { flex: 1 1 150px; } }
</style>
