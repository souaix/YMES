<script setup lang="ts">
import { computed, ref, watch } from 'vue'

interface TestRecord {
  psNo: string
  lotNo: string
  componentNo: string
  testRecipeType: string
  logGroupSerial: string
  opNo: string
  opName: string
  equipmentNo: string
  testDate: string
  dataType: 'now' | 'history'
  values: Record<string, string | number>
}

type PickerName = 'ps' | 'recipe' | null

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

const productionSegments = ['PS-FT-01', 'PS-FT-02', 'PS-CP-01']
const recipeTypesBySegment: Record<string, string[]> = {
  'PS-FT-01': ['FT_DC', 'FT_LEAK'],
  'PS-FT-02': ['FT_DC', 'FT_AC'],
  'PS-CP-01': ['CP_WAFER'],
}
const records: TestRecord[] = [
  { psNo: 'PS-FT-01', lotNo: 'LOT26112001', componentNo: 'C-001-A', testRecipeType: 'FT_DC', logGroupSerial: 'LG-261120-0001', opNo: 'FT01', opName: 'Final Test', equipmentNo: 'FT-12', testDate: '2026-11-20T08:31:20', dataType: 'now', values: { VDD_5V: 5.012, IDD_UA: 12.4, LEAK_NA: 0.13 } },
  { psNo: 'PS-FT-01', lotNo: 'LOT26112001', componentNo: 'C-001-B', testRecipeType: 'FT_DC', logGroupSerial: 'LG-261120-0002', opNo: 'FT01', opName: 'Final Test', equipmentNo: 'FT-12', testDate: '2026-11-20T09:02:14', dataType: 'now', values: { VDD_5V: 5.008, IDD_UA: 12.8, LEAK_NA: 0.15 } },
  { psNo: 'PS-FT-01', lotNo: 'LOT26112002', componentNo: 'C-002-A', testRecipeType: 'FT_LEAK', logGroupSerial: 'LG-261120-0003', opNo: 'FT01', opName: 'Final Test', equipmentNo: 'FT-14', testDate: '2026-11-20T10:11:04', dataType: 'now', values: { PIN_LEAK_NA: 0.22, IO_LEAK_NA: 0.31 } },
  { psNo: 'PS-FT-01', lotNo: 'LOT26112002', componentNo: 'C-002-A', testRecipeType: 'FT_DC', logGroupSerial: 'LG-261120-0004', opNo: 'FT01', opName: 'Final Test', equipmentNo: 'FT-14', testDate: '2026-11-20T10:12:09', dataType: 'now', values: { VDD_5V: 4.996, IDD_UA: 13.1, LEAK_NA: 0.17 } },
  { psNo: 'PS-FT-02', lotNo: 'LOT26112104', componentNo: 'C-104-A', testRecipeType: 'FT_AC', logGroupSerial: 'LG-261121-0011', opNo: 'FT02', opName: 'AC Test', equipmentNo: 'FT-21', testDate: '2026-11-21T14:16:35', dataType: 'now', values: { FREQ_MHZ: 400, JITTER_PS: 16.8 } },
  { psNo: 'PS-FT-02', lotNo: 'LOT26112104', componentNo: 'C-104-B', testRecipeType: 'FT_DC', logGroupSerial: 'LG-261121-0012', opNo: 'FT02', opName: 'AC Test', equipmentNo: 'FT-21', testDate: '2026-11-21T14:22:50', dataType: 'now', values: { VDD_5V: 5.021, IDD_UA: 11.9, LEAK_NA: 0.12 } },
  { psNo: 'PS-CP-01', lotNo: 'LOT26112208', componentNo: 'WAFER-08-DIE-221', testRecipeType: 'CP_WAFER', logGroupSerial: 'LG-261122-0021', opNo: 'CP01', opName: 'Wafer Probe', equipmentNo: 'CP-03', testDate: '2026-11-22T07:40:12', dataType: 'now', values: { BIN1_QTY: 918, BIN2_QTY: 12, YIELD_PCT: 98.71 } },
  { psNo: 'PS-FT-01', lotNo: 'LOT26110509', componentNo: 'C-509-A', testRecipeType: 'FT_DC', logGroupSerial: 'LG-261105-0089', opNo: 'FT01', opName: 'Final Test', equipmentNo: 'FT-12', testDate: '2026-11-05T16:52:44', dataType: 'history', values: { VDD_5V: 4.991, IDD_UA: 13.6, LEAK_NA: 0.20 } },
  { psNo: 'PS-FT-01', lotNo: 'LOT26110509', componentNo: 'C-509-B', testRecipeType: 'FT_LEAK', logGroupSerial: 'LG-261105-0090', opNo: 'FT01', opName: 'Final Test', equipmentNo: 'FT-12', testDate: '2026-11-05T17:03:31', dataType: 'history', values: { PIN_LEAK_NA: 0.28, IO_LEAK_NA: 0.42 } },
  { psNo: 'PS-CP-01', lotNo: 'LOT26110107', componentNo: 'WAFER-07-DIE-114', testRecipeType: 'CP_WAFER', logGroupSerial: 'LG-261101-0204', opNo: 'CP01', opName: 'Wafer Probe', equipmentNo: 'CP-02', testDate: '2026-11-01T05:12:00', dataType: 'history', values: { BIN1_QTY: 901, BIN2_QTY: 19, YIELD_PCT: 97.92 } },
]

const psNo = ref('')
const lotNo = ref('')
const componentNo = ref('')
const recipeType = ref('')
const includeLot = ref(false)
const includeComponent = ref(false)
const includeRecipe = ref(false)
const includeDate = ref(false)
const testDateFrom = ref('2026-11-01T00:00:00')
const testDateTo = ref('2026-11-26T23:59:59')
const dataType = ref<'now' | 'history'>('now')
const groupBy = ref<'recipe' | 'component'>('recipe')
const picker = ref<PickerName>(null)
const pickedSegments = ref<string[]>([])
const pickedRecipes = ref<string[]>([])
const result = ref<TestRecord[]>([])
const queried = ref(false)
const notice = ref('輸入 PSNo 後按 Query；可用「...」選擇多個條件。')
const noticeError = ref(false)
const expanded = ref<string[]>([])

const selectedSegments = computed(() => splitValues(psNo.value))
const availableRecipeTypes = computed(() => [...new Set(selectedSegments.value.flatMap((segment) => recipeTypesBySegment[segment] ?? []))].sort())
const resultGroups = computed(() => {
  const buckets = new Map<string, TestRecord[]>()
  for (const row of result.value) {
    const key = groupBy.value === 'recipe' ? row.testRecipeType : `${row.lotNo}\u0000${row.componentNo}`
    buckets.set(key, [...(buckets.get(key) ?? []), row])
  }
  return [...buckets.entries()].map(([key, rows]) => ({
    key,
    label: groupBy.value === 'recipe' ? rows[0].testRecipeType : rows[0].componentNo,
    lotNo: groupBy.value === 'component' ? rows[0].lotNo : '',
    rows: [...rows].sort((a, b) => a.testDate.localeCompare(b.testDate)),
  })).sort((a, b) => a.label.localeCompare(b.label))
})
const measurementColumns = computed(() => [...new Set(result.value.flatMap((row) => Object.keys(row.values)))].sort())
const shownDate = computed(() => includeDate.value ? `${testDateFrom.value.replace('T', ' ')} ～ ${testDateTo.value.replace('T', ' ')}` : '未指定')

watch(selectedSegments, (segments) => {
  const valid = new Set(segments.flatMap((segment) => recipeTypesBySegment[segment] ?? []))
  pickedRecipes.value = pickedRecipes.value.filter((type) => valid.has(type))
  if (recipeType.value) recipeType.value = splitValues(recipeType.value).filter((type) => valid.has(type)).join(',')
}, { deep: true })

function splitValues(value: string) {
  return [...new Set(value.split(',').map((item) => item.trim()).filter(Boolean))]
}

function openPicker(name: Exclude<PickerName, null>) {
  if (name === 'recipe' && selectedSegments.value.length === 0) {
    notice.value = '請先輸入或選取 PSNo，再載入 TestRecipeType。'
    noticeError.value = true
    return
  }
  if (name === 'ps') pickedSegments.value = [...selectedSegments.value]
  else pickedRecipes.value = splitValues(recipeType.value).filter((type) => availableRecipeTypes.value.includes(type))
  picker.value = name
}

function confirmPicker() {
  if (picker.value === 'ps') psNo.value = pickedSegments.value.join(',')
  if (picker.value === 'recipe') {
    recipeType.value = pickedRecipes.value.join(',')
    includeRecipe.value = pickedRecipes.value.length > 0
  }
  picker.value = null
}

function togglePicked(list: string[], item: string) {
  const index = list.indexOf(item)
  if (index >= 0) list.splice(index, 1)
  else list.push(item)
}

function query() {
  const segments = selectedSegments.value
  if (segments.length === 0) {
    result.value = []
    queried.value = false
    notice.value = 'PSNo 為必填條件。'
    noticeError.value = true
    return
  }
  if (includeDate.value && (!testDateFrom.value || !testDateTo.value || testDateFrom.value > testDateTo.value)) {
    notice.value = 'TestDate 起訖時間不完整或起日大於迄日。'
    noticeError.value = true
    return
  }
  const lots = includeLot.value ? splitValues(lotNo.value) : []
  const components = includeComponent.value ? splitValues(componentNo.value) : []
  const recipes = includeRecipe.value ? splitValues(recipeType.value) : []
  result.value = records.filter((row) => segments.includes(row.psNo)
    && row.dataType === dataType.value
    && (!lots.length || lots.includes(row.lotNo))
    && (!components.length || components.includes(row.componentNo))
    && (!recipes.length || recipes.includes(row.testRecipeType))
    && (!includeDate.value || (row.testDate >= testDateFrom.value && row.testDate <= testDateTo.value)))
  queried.value = true
  expanded.value = []
  notice.value = result.value.length ? `DEV 假資料：符合 ${result.value.length} 筆測試資料，依 ${groupBy.value === 'recipe' ? 'TestRecipeType' : 'ComponentNo'} 彙整。` : 'DEV 假資料：沒有符合條件的測試資料。'
  noticeError.value = false
}

function toggleExpanded(key: string) {
  expanded.value = expanded.value.includes(key) ? expanded.value.filter((value) => value !== key) : [...expanded.value, key]
}

function displayDate(value: string) {
  return value.replace('T', ' ')
}
</script>

<template>
  <section class="electrical-report" aria-label="電性測試資料查詢 DEV 預覽">
    <header class="report-heading">
      <div><p class="eyebrow">RTS01 · READ ONLY REPORT</p><h1>Electrical Test Data Query</h1></div>
      <span class="dev-badge">DEV 假資料</span>
    </header>

    <section class="filter-panel" aria-label="查詢條件">
      <div class="filter-topline">
        <fieldset><legend>DataType</legend>
          <label><input v-model="dataType" type="radio" value="now"> NowData</label>
          <label><input v-model="dataType" type="radio" value="history"> HistoryLog</label>
        </fieldset>
        <fieldset><legend>DataGroupBy</legend>
          <label><input v-model="groupBy" type="radio" value="recipe"> TestRecipeType</label>
          <label><input v-model="groupBy" type="radio" value="component"> Component</label>
        </fieldset>
      </div>
      <div class="filter-grid">
        <label class="filter-label" for="rts-psno">PSNo <span class="required">*</span></label>
        <div class="input-with-action"><input id="rts-psno" v-model="psNo" autocomplete="off" placeholder="例如 PS-FT-01，多筆以逗號分隔"><button type="button" @click="openPicker('ps')">...</button></div>

        <label class="filter-label" for="rts-lot"><input v-model="includeLot" type="checkbox"> LotNo</label>
        <input id="rts-lot" v-model="lotNo" :disabled="!includeLot" placeholder="可輸入多筆，以逗號分隔">

        <label class="filter-label" for="rts-component"><input v-model="includeComponent" type="checkbox"> ComponentNo</label>
        <input id="rts-component" v-model="componentNo" :disabled="!includeComponent" placeholder="可輸入多筆，以逗號分隔">

        <label class="filter-label" for="rts-recipe"><input v-model="includeRecipe" type="checkbox"> TestRecipeType</label>
        <div class="input-with-action"><input id="rts-recipe" v-model="recipeType" :disabled="!includeRecipe" placeholder="選擇 PSNo 對應類別"><button type="button" :disabled="!includeRecipe" @click="openPicker('recipe')">...</button></div>

        <label class="filter-label" for="rts-from"><input v-model="includeDate" type="checkbox"> TestDate</label>
        <div class="date-range"><input id="rts-from" v-model="testDateFrom" type="datetime-local" step="1" :disabled="!includeDate"><span>To</span><input v-model="testDateTo" type="datetime-local" step="1" :disabled="!includeDate"></div>
      </div>
      <div class="filter-actions"><span class="condition-summary">日期條件：{{ shownDate }}</span><button class="primary" type="button" @click="query">Query</button><button type="button" @click="emit('close')">Close (X)</button></div>
    </section>

    <p class="notice" :class="{ error: noticeError }" role="status">{{ notice }}</p>

    <section class="results-panel" aria-label="查詢結果">
      <header><div><h2>Test Data</h2><span>{{ queried ? `${resultGroups.length} 個彙整列 / ${result.length} 筆明細` : '尚未查詢' }}</span></div><small>唯讀 · 動態 TestRecipe 欄位</small></header>
      <div v-if="!queried" class="empty-state">設定查詢條件後按 Query。</div>
      <div v-else-if="resultGroups.length === 0" class="empty-state">沒有符合條件的測試資料。</div>
      <div v-else class="table-scroll">
        <table class="summary-table">
          <thead><tr><th class="expand-col"></th><th v-if="groupBy === 'component'">LotNo</th><th>{{ groupBy === 'recipe' ? 'TestRecipeType' : 'ComponentNo' }}</th><th>測試筆數</th></tr></thead>
          <tbody>
            <template v-for="group in resultGroups" :key="group.key">
              <tr class="summary-row">
                <td class="expand-col"><button type="button" :aria-expanded="expanded.includes(group.key)" @click="toggleExpanded(group.key)">{{ expanded.includes(group.key) ? '−' : '+' }}</button></td>
                <td v-if="groupBy === 'component'">{{ group.lotNo }}</td><td>{{ group.label }}</td><td>{{ group.rows.length }}</td>
              </tr>
              <tr v-if="expanded.includes(group.key)" class="detail-row"><td :colspan="groupBy === 'component' ? 4 : 3">
                <div class="detail-scroll"><table class="detail-table"><thead><tr><th>TestDate</th><th>LotNo</th><th>ComponentNo</th><th>TestRecipeType</th><th>OPNo / OPName</th><th>EquipmentNo</th><th>LogGroupSerial</th><th v-for="column in measurementColumns" :key="column">{{ column }}</th></tr></thead>
                  <tbody><tr v-for="row in group.rows" :key="row.logGroupSerial"><td>{{ displayDate(row.testDate) }}</td><td>{{ row.lotNo }}</td><td>{{ row.componentNo }}</td><td>{{ row.testRecipeType }}</td><td>{{ row.opNo }} / {{ row.opName }}</td><td>{{ row.equipmentNo }}</td><td>{{ row.logGroupSerial }}</td><td v-for="column in measurementColumns" :key="column" class="numeric">{{ row.values[column] ?? '—' }}</td></tr></tbody>
                </table></div>
              </td></tr>
            </template>
          </tbody>
        </table>
      </div>
    </section>

    <div v-if="picker" class="picker-backdrop" role="presentation" @click.self="picker = null">
      <section class="picker-dialog" role="dialog" aria-modal="true" :aria-label="picker === 'ps' ? '選擇 PSNo' : '選擇 TestRecipeType'">
        <header><h2>{{ picker === 'ps' ? 'Select Production Segment' : 'Select TestRecipeType' }}</h2><button type="button" aria-label="關閉" @click="picker = null">×</button></header>
        <p v-if="picker === 'recipe'" class="picker-hint">只列出目前 PSNo 對應的 DEV 核准測試類別。</p>
        <div class="picker-list"><label v-for="item in picker === 'ps' ? productionSegments : availableRecipeTypes" :key="item"><input type="checkbox" :checked="(picker === 'ps' ? pickedSegments : pickedRecipes).includes(item)" @change="togglePicked(picker === 'ps' ? pickedSegments : pickedRecipes, item)">{{ item }}</label></div>
        <footer><button type="button" @click="picker = null">Cancel</button><button class="primary" type="button" @click="confirmPicker">OK</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.electrical-report { box-sizing: border-box; height: 100%; min-height: 0; display: flex; flex-direction: column; gap: 10px; padding: 12px; overflow: auto; background: #f7f5ee; color: #26251f; font: 13px/1.35 Arial, sans-serif; }
.report-heading { display: flex; align-items: center; justify-content: space-between; min-height: 42px; }
.report-heading h1 { margin: 0; font-size: 19px; font-weight: 600; }
.eyebrow { margin: 0 0 3px; color: #777366; font-size: 10px; letter-spacing: .1em; }
.dev-badge { border: 1px solid #b18a26; border-radius: 3px; padding: 4px 8px; background: #fff7dd; color: #6b5114; font-weight: 600; }
.filter-panel, .results-panel { border: 1px solid #c9c5b7; background: #fffefa; }
.filter-topline { display: flex; gap: 20px; padding: 8px 10px 4px; border-bottom: 1px solid #e1ded4; }
fieldset { display: flex; align-items: center; gap: 14px; margin: 0; padding: 2px 8px 6px; border: 0; }
legend { float: left; margin-right: 9px; color: #666257; font-weight: 600; }
label { display: inline-flex; align-items: center; gap: 5px; }
input[type="radio"], input[type="checkbox"] { accent-color: #544f40; }
.filter-grid { display: grid; grid-template-columns: 130px minmax(180px, 1fr) 130px minmax(180px, 1fr); gap: 8px 10px; align-items: center; padding: 10px; }
.filter-label { color: #3d3a31; }
.required { color: #a33024; }
input:not([type="checkbox"]):not([type="radio"]) { box-sizing: border-box; width: 100%; min-width: 0; min-height: 27px; padding: 3px 6px; border: 1px solid #bdb9ad; border-radius: 2px; background: #fff; color: #29271f; font: inherit; }
input:disabled { background: #f0efe9 !important; color: #89867c !important; }
.input-with-action { display: flex; gap: 4px; }
button { min-height: 27px; padding: 3px 9px; border: 1px solid #aaa698; border-radius: 2px; background: #f7f5ef; color: #2c2a24; font: inherit; cursor: pointer; }
button:hover:not(:disabled) { background: #ebe7d9; }
button:disabled { cursor: not-allowed; opacity: .55; }
.primary { border-color: #514c3d; background: #514c3d; color: #fff; }
.primary:hover:not(:disabled) { background: #39362d; }
.date-range { display: flex; align-items: center; gap: 6px; }
.date-range input { flex: 1; }
.filter-actions { display: flex; justify-content: flex-end; align-items: center; gap: 7px; padding: 0 10px 9px; }
.condition-summary { margin-right: auto; color: #777366; font-size: 12px; }
.notice { margin: 0; padding: 7px 9px; border-left: 3px solid #6c7c51; background: #eff3e9; color: #39462d; }
.notice.error { border-color: #a33024; background: #fbefed; color: #8c261c; }
.results-panel { display: flex; flex-direction: column; min-height: 200px; flex: 1; }
.results-panel > header { display: flex; justify-content: space-between; align-items: center; padding: 8px 10px; border-bottom: 1px solid #dedbd0; }
.results-panel > header div { display: flex; align-items: baseline; gap: 12px; }
.results-panel h2 { margin: 0; font-size: 15px; }
.results-panel > header span, .results-panel small { color: #777366; }
.empty-state { display: grid; min-height: 150px; place-items: center; color: #827e73; }
.table-scroll, .detail-scroll { width: 100%; overflow: auto; }
table { width: 100%; border-collapse: collapse; }
th, td { border-right: 1px solid #e6e2d8; border-bottom: 1px solid #e6e2d8; padding: 6px 8px; text-align: left; white-space: nowrap; }
th { position: sticky; top: 0; z-index: 1; background: #eeece4; color: #3d3a31; font-weight: 600; }
.summary-table tbody tr:nth-child(even) { background: #faf9f5; }
.expand-col { width: 36px; text-align: center; }
.expand-col button { min-width: 23px; min-height: 22px; padding: 0; font-weight: 700; }
.detail-row > td { padding: 0; background: #f5f3ec; }
.detail-table { min-width: 1100px; background: #fffefa; font-size: 12px; }
.detail-table th { position: static; background: #e6e2d7; }
.numeric { text-align: right; font-variant-numeric: tabular-nums; }
.picker-backdrop { position: fixed; inset: 0; z-index: 50; display: grid; place-items: center; padding: 18px; background: #17160f77; }
.picker-dialog { display: flex; flex-direction: column; width: min(440px, 96vw); max-height: 80vh; border: 1px solid #807b6c; background: #fffefa; box-shadow: 0 14px 40px #17160f44; }
.picker-dialog > header, .picker-dialog > footer { display: flex; align-items: center; justify-content: space-between; padding: 9px 11px; border-bottom: 1px solid #dedbd0; }
.picker-dialog h2 { margin: 0; font-size: 15px; }
.picker-dialog > header button { min-width: 27px; font-size: 18px; }
.picker-hint { margin: 0; padding: 8px 11px; color: #777366; }
.picker-list { display: grid; gap: 2px; overflow: auto; padding: 10px 12px; }
.picker-list label { min-height: 28px; }
.picker-dialog > footer { justify-content: flex-end; gap: 7px; border-top: 1px solid #dedbd0; border-bottom: 0; }
@media (max-width: 800px) { .filter-grid { grid-template-columns: 115px minmax(150px, 1fr); } .filter-topline { flex-wrap: wrap; gap: 5px; } fieldset { flex-wrap: wrap; gap: 7px; } .date-range { flex-wrap: wrap; } }
</style>
