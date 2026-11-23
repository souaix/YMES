<script setup lang="ts">
import { computed, ref } from 'vue'

type FactorOption = { value: string; name: string }
type FactorDefinition = { factorNo: string; name: string; online: boolean; parameterType: 0 | 1 | 2; options: FactorOption[] }
type ChartFixture = {
  id: string; qcItemNo: string; qcItemName: string; dataType: string; keyNode: string; title: string; unit: string
  lastDataDaysAgo: number
  sponsor: string; target: number; us: number; ls: number; goal: number; sampleSize: number
  factors: Record<string, string>; erfQty: number; oos: number; ooc: number; avg: number; std: number
  cpkTwoMonthsAgo: number; runTwoMonthsAgo: number; cpkLastMonth: number; runLastMonth: number
  cpCurrent: number; cpkCurrent: number; runCurrent: number; dataCountCurrent: number
}
type NoticeKind = 'info' | 'success' | 'error'

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

const factors: FactorDefinition[] = [
  { factorNo: 'OP', name: '作業站', online: true, parameterType: 0, options: [{ value: 'OP-10', name: '加工一站' }, { value: 'OP-20', name: '精加工二站' }, { value: 'OP-35', name: '研磨三站' }, { value: 'OP-50', name: '包裝站' }] },
  { factorNo: 'EQUIPMENT', name: '設備', online: true, parameterType: 1, options: [{ value: 'EQ-020', name: '車床 020' }, { value: 'EQ-035', name: '研磨機 035' }, { value: 'EQ-050', name: '量測台 050' }] },
  { factorNo: 'PRODUCT', name: '產品', online: true, parameterType: 0, options: [{ value: 'PRD-AX17', name: '軸心 A17' }, { value: 'PRD-PH08', name: '壓合件 H08' }, { value: 'PRD-CB22', name: '連接件 B22' }] },
  { factorNo: 'CUSTOMER', name: '客戶', online: false, parameterType: 1, options: [{ value: 'CUS-DEV-01', name: 'DEV 客戶甲' }, { value: 'CUS-DEV-02', name: 'DEV 客戶乙' }, { value: 'CUS-DEV-03', name: 'DEV 客戶丙' }] },
  { factorNo: 'LOT_TYPE', name: '批別', online: true, parameterType: 2, options: [] },
]
const fixtures: ChartFixture[] = [
  { id: 'axis-op20', qcItemNo: 'VAR-DEV-101', qcItemName: '軸心直徑', dataType: 'InLine', keyNode: 'I', title: '軸心直徑趨勢', unit: 'mm', lastDataDaysAgo: 5, sponsor: 'DEV-QA', target: 10, us: 10.2, ls: 9.8, goal: 1.33, sampleSize: 25, factors: { OP: 'OP-20', EQUIPMENT: 'EQ-020', PRODUCT: 'PRD-AX17', CUSTOMER: 'CUS-DEV-01', LOT_TYPE: 'DEV-A' }, erfQty: 1, oos: 2, ooc: 3, avg: 10.016, std: 0.041, cpkTwoMonthsAgo: 1.42, runTwoMonthsAgo: 34, cpkLastMonth: 1.51, runLastMonth: 31, cpCurrent: 1.71, cpkCurrent: 1.52, runCurrent: 18, dataCountCurrent: 450 },
  { id: 'axis-op35', qcItemNo: 'VAR-DEV-101', qcItemName: '軸心直徑', dataType: 'InLine', keyNode: 'I', title: '軸心直徑趨勢', unit: 'mm', lastDataDaysAgo: 8, sponsor: 'DEV-QA', target: 10, us: 10.2, ls: 9.8, goal: 1.33, sampleSize: 25, factors: { OP: 'OP-35', EQUIPMENT: 'EQ-035', PRODUCT: 'PRD-AX17', CUSTOMER: 'CUS-DEV-02', LOT_TYPE: 'DEV-B' }, erfQty: 0, oos: 1, ooc: 2, avg: 10.009, std: 0.038, cpkTwoMonthsAgo: 1.36, runTwoMonthsAgo: 28, cpkLastMonth: 1.44, runLastMonth: 30, cpCurrent: 1.63, cpkCurrent: 1.39, runCurrent: 23, dataCountCurrent: 575 },
  { id: 'press-op20', qcItemNo: 'VAR-DEV-202', qcItemName: '壓合高度', dataType: 'Particle', keyNode: 'I', title: '壓合高度趨勢', unit: 'mm', lastDataDaysAgo: 12, sponsor: 'DEV-QA', target: 2, us: 2.2, ls: 1.8, goal: 1.33, sampleSize: 25, factors: { OP: 'OP-20', EQUIPMENT: 'EQ-020', PRODUCT: 'PRD-PH08', CUSTOMER: 'CUS-DEV-01', LOT_TYPE: 'DEV-A' }, erfQty: 2, oos: 3, ooc: 4, avg: 2.031, std: 0.052, cpkTwoMonthsAgo: 1.8, runTwoMonthsAgo: 30, cpkLastMonth: 1.9, runLastMonth: 27, cpCurrent: 1.2, cpkCurrent: 1.27, runCurrent: 22, dataCountCurrent: 550 },
  { id: 'flat-op10', qcItemNo: 'VAR-DEV-305', qcItemName: '端面平面度', dataType: 'OffLine', keyNode: 'C', title: '端面平面度趨勢', unit: 'um', lastDataDaysAgo: 18, sponsor: 'DEV-QA', target: 0.025, us: 0.05, ls: 0, goal: 1.33, sampleSize: 25, factors: { OP: 'OP-10', EQUIPMENT: 'EQ-050', PRODUCT: 'PRD-AX17', CUSTOMER: 'CUS-DEV-03', LOT_TYPE: 'DEV-A' }, erfQty: 0, oos: 0, ooc: 1, avg: 0.019, std: 0.005, cpkTwoMonthsAgo: 1.4, runTwoMonthsAgo: 26, cpkLastMonth: 1.45, runLastMonth: 29, cpCurrent: 1.59, cpkCurrent: 1.43, runCurrent: 21, dataCountCurrent: 525 },
  { id: 'coat-cus01', qcItemNo: 'VAR-DEV-330', qcItemName: '鍍層厚度', dataType: 'InLine', keyNode: 'N', title: '鍍層厚度趨勢', unit: 'um', lastDataDaysAgo: 2, sponsor: 'DEV-QA', target: 8, us: 9, ls: 7, goal: 1.33, sampleSize: 25, factors: { OP: 'OP-50', EQUIPMENT: 'EQ-050', PRODUCT: 'PRD-CB22', CUSTOMER: 'CUS-DEV-01', LOT_TYPE: 'DEV-C' }, erfQty: 1, oos: 1, ooc: 1, avg: 8.04, std: 0.31, cpkTwoMonthsAgo: 1.22, runTwoMonthsAgo: 25, cpkLastMonth: 1.31, runLastMonth: 27, cpCurrent: 1.44, cpkCurrent: 1.34, runCurrent: 20, dataCountCurrent: 500 },
]

function localDateInput(date: Date): string {
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}
const now = new Date()
const initialFrom = new Date(now)
initialFrom.setDate(initialFrom.getDate() + 1)
initialFrom.setMonth(initialFrom.getMonth() - 1)
const fromDate = ref(localDateInput(initialFrom))
const endDate = ref(localDateInput(now))
const keyNode = ref('')
const getLastMonth = ref(true)
const factorValues = ref<Record<string, string>>(Object.fromEntries(factors.map((factor) => [factor.factorNo, ''])))
const factorNames = ref<Record<string, string>>(Object.fromEntries(factors.map((factor) => [factor.factorNo, ''])))
const applied = ref<{ fromDate: string; endDate: string; keyNode: string; getLastMonth: boolean; factorValues: Record<string, string> } | null>(null)
const searched = ref(false)
const pickerFactorNo = ref('')
const pickerSelection = ref<string[]>([])
const notice = ref('設定至少一個 Collect Factor 後按 Query；此頁只使用 DEV 假資料。')
const noticeKind = ref<NoticeKind>('info')

const pickerFactor = computed(() => factors.find((factor) => factor.factorNo === pickerFactorNo.value) ?? null)
const resultRows = computed(() => {
  if (!applied.value) return []
  const filters = Object.entries(applied.value.factorValues)
    .map(([factorNo, raw]) => [factorNo, raw.split(',').map((value) => value.trim().toUpperCase()).filter(Boolean)] as const)
    .filter(([, values]) => values.length > 0)
  return fixtures.filter((fixture) => {
    const queryStart = new Date(`${applied.value?.fromDate}T00:00:00`)
    const queryEnd = new Date(`${applied.value?.endDate}T23:59:59`)
    const latestData = new Date(now)
    latestData.setDate(latestData.getDate() - fixture.lastDataDaysAgo)
    latestData.setHours(12, 0, 0, 0)
    if (latestData < queryStart || latestData > queryEnd) return false
    if (applied.value?.keyNode && fixture.keyNode !== applied.value.keyNode) return false
    if (!filters.every(([factorNo, values]) => values.includes((fixture.factors[factorNo] ?? '').toUpperCase()))) return false
    return true
  }).map((fixture) => ({
    ...fixture,
    r2Flag: (fixture.cpkTwoMonthsAgo - fixture.cpkCurrent) / Math.abs(fixture.cpkTwoMonthsAgo) > 0.2 ? '@' : '',
    r1Flag: (fixture.cpkLastMonth - fixture.cpkCurrent) / Math.abs(fixture.cpkLastMonth) > 0.25 ? '!' : '',
  }))
})
const periods = computed(() => {
  if (!applied.value) return []
  const from = new Date(`${applied.value.fromDate}T00:00:00`)
  const end = new Date(`${applied.value.endDate}T23:59:59`)
  const label = (date: Date) => `${date.getFullYear()}/${String(date.getMonth() + 1).padStart(2, '0')}`
  return [label(new Date(from.getFullYear(), from.getMonth() - 2, 1)), label(new Date(from.getFullYear(), from.getMonth() - 1, 1)), `${applied.value.fromDate} 至 ${localDateInput(end)}`]
})

function openFactorPicker(factor: FactorDefinition): void {
  pickerFactorNo.value = factor.factorNo
  pickerSelection.value = (factorValues.value[factor.factorNo] ?? '').split(',').map((value) => value.trim()).filter(Boolean)
}
function togglePickerValue(value: string, checked: boolean): void {
  pickerSelection.value = checked ? [...new Set([...pickerSelection.value, value])] : pickerSelection.value.filter((item) => item !== value)
}
function applyFactorPicker(): void {
  if (pickerFactor.value) {
    const factorNo = pickerFactor.value.factorNo
    factorValues.value[factorNo] = pickerSelection.value.join(',')
    factorNames.value[factorNo] = pickerSelection.value.map((value) => pickerFactor.value?.options.find((option) => option.value === value)?.name ?? '').filter(Boolean).join(',')
  }
  pickerFactorNo.value = ''
}
function updateFreeText(factorNo: string, value: string): void {
  factorValues.value[factorNo] = value.toUpperCase()
  factorNames.value[factorNo] = ''
}
function clearFactors(): void {
  factorValues.value = Object.fromEntries(factors.map((factor) => [factor.factorNo, '']))
  factorNames.value = Object.fromEntries(factors.map((factor) => [factor.factorNo, '']))
  notice.value = 'Collect Factor 條件已清除；已顯示的查詢結果保留。'
  noticeKind.value = 'info'
}
function runQuery(): void {
  if (!fromDate.value || !endDate.value || fromDate.value > endDate.value) {
    notice.value = '請確認起日與迄日，起日不可晚於迄日。'
    noticeKind.value = 'error'
    return
  }
  if (!Object.values(factorValues.value).some((value) => value.trim() !== '')) {
    notice.value = '請至少輸入或選取一個 Collect Factor 值。'
    noticeKind.value = 'error'
    return
  }
  applied.value = { fromDate: fromDate.value, endDate: endDate.value, keyNode: keyNode.value, getLastMonth: getLastMonth.value, factorValues: { ...factorValues.value } }
  searched.value = true
  notice.value = `查詢完成：${resultRows.value.length} 張控制圖（DEV 假資料；依要因值篩選全部合格控制圖）。`
  noticeKind.value = resultRows.value.length > 0 ? 'success' : 'info'
}
function factorText(chart: ChartFixture): string {
  return factors.filter((factor) => factor.online)
    .map((factor) => chart.factors[factor.factorNo] ?? '')
    .filter((value) => value.trim() !== '' && value !== '*')
    .join(',')
}
function decimal(value: number): string { return value.toFixed(3) }
</script>

<template>
  <section v-if="props.developmentPreview" class="factor-report-page" aria-label="SPC 管制圖月報表_以要因查詢">
    <header class="page-head">
      <div><p class="eyebrow">SPC / SC312</p><h1>管制圖月報表_以要因查詢</h1><p class="subtitle">依日期、Key Node 和 Collect Factor 查詢全部符合條件的計量管制圖。</p></div>
      <button class="button" type="button" @click="emit('close')">Close (X)</button>
    </header>
    <div class="notice" :class="noticeKind" role="status">{{ notice }}</div>

    <section class="query-panel panel">
      <div class="query-grid">
        <label class="field"><span>From Query Date</span><input v-model="fromDate" type="date"></label>
        <label class="field"><span>End Query Date</span><input v-model="endDate" type="date"></label>
        <label class="field"><span>Key_Node</span><select v-model="keyNode"><option value="">全部</option><option value="I">I</option><option value="C">C</option><option value="N">N</option></select></label>
        <label class="check-field"><input v-model="getLastMonth" type="checkbox"> Get Last Month（顯示前兩個月）</label>
      </div>
      <section class="factor-section">
        <div class="section-heading"><div><h2>Collect Factor</h2><p>至少設定一個值；同一要因可選多值，多個要因會一起套用。</p></div><span class="count-pill">{{ factors.length }} 個 DEV Factor</span></div>
        <div class="factor-table-scroll"><table class="factor-table"><thead><tr><th>FactorNo</th><th>FactorName</th><th>Value</th><th>Name</th><th>選取</th></tr></thead><tbody>
          <tr v-for="factor in factors" :key="factor.factorNo"><td><strong>{{ factor.factorNo }}</strong></td><td>{{ factor.name }}</td><td><input v-if="factor.parameterType === 2" :value="factorValues[factor.factorNo]" placeholder="可輸入多值，以逗號分隔" @input="updateFreeText(factor.factorNo, ($event.target as HTMLInputElement).value)"><input v-else :value="factorValues[factor.factorNo]" readonly placeholder="全部"></td><td>{{ factorNames[factor.factorNo] }}</td><td><button v-if="factor.parameterType !== 2" class="button select-button" type="button" @click="openFactorPicker(factor)">...</button><span v-else class="muted">直接輸入</span></td></tr>
        </tbody></table></div>
      </section>
      <footer class="query-footer"><span>來源的 QC Item、Month Report Code 與 QCDataType 群組在 Designer 中隱藏；本頁依其實際可見條件呈現。</span><div class="actions"><button class="button" type="button" @click="clearFactors">Clear</button><button class="button primary" type="button" @click="runQuery">Query</button></div></footer>
    </section>

    <section class="result-panel panel">
      <div class="result-heading"><div><p class="eyebrow">MONTHLY CHART REPORT BY FACTOR</p><h2>月報結果</h2></div><span v-if="searched" class="count-pill">{{ resultRows.length }} 張控制圖</span></div>
      <div v-if="!searched" class="result-empty">設定查詢條件後按 Query 顯示月報結果。</div>
      <div v-else-if="resultRows.length === 0" class="result-empty">此條件沒有符合的 DEV 管制圖資料。</div>
      <div v-else class="result-table-scroll"><table class="result-table"><thead><tr>
        <th>QCItemNo</th><th>QCItemName</th><th>ChartFactors</th><th>CHART TITLE</th><th>DATA_UNIT</th><th>TARGET</th><th>U_SPEC</th><th>L_SPEC</th><th>RUN#</th><th>Data_CNT</th><th>#OOS</th><th>%OOS</th><th>#OOC</th><th>%OOC</th><th>AVG</th><th>STD</th><th>CP</th><th>CPK</th><th>ERFQty</th><th>CPK_GOAL</th><th>L2CPK</th><th>L2RUN#</th><th>LRUN#</th><th>LCPK</th><th>SPONSOR</th><th>Run&lt;20</th><th>Cpk&lt;Goal</th><th>R1&gt;25%</th><th>R2&gt;20%</th><th>Key_Node</th><th>Data_Type</th>
      </tr></thead><tbody><tr v-for="chart in resultRows" :key="chart.id">
        <td>{{ chart.qcItemNo }}</td><td>{{ chart.qcItemName }}</td><td>{{ factorText(chart) }}</td><td>{{ chart.title }}</td><td>{{ chart.unit }}</td><td>{{ decimal(chart.target) }}</td><td>{{ decimal(chart.us) }}</td><td>{{ decimal(chart.ls) }}</td><td>{{ chart.runCurrent }}</td><td>{{ chart.dataCountCurrent }}</td><td>{{ chart.oos }}</td><td>{{ decimal(chart.oos / (chart.runCurrent * chart.sampleSize)) }}</td><td>{{ chart.ooc }}</td><td>{{ decimal(chart.ooc / (chart.runCurrent * chart.sampleSize)) }}</td><td>{{ decimal(chart.avg) }}</td><td>{{ decimal(chart.std) }}</td><td>{{ decimal(chart.cpCurrent) }}</td><td>{{ decimal(chart.cpkCurrent) }}</td><td>{{ chart.erfQty }}</td><td>{{ decimal(chart.goal) }}</td><td>{{ applied?.getLastMonth ? decimal(chart.cpkTwoMonthsAgo) : '—' }}</td><td>{{ applied?.getLastMonth ? chart.runTwoMonthsAgo : '—' }}</td><td>{{ applied?.getLastMonth ? chart.runLastMonth : '—' }}</td><td>{{ applied?.getLastMonth ? decimal(chart.cpkLastMonth) : '—' }}</td><td>{{ chart.sponsor }}</td><td><span v-if="chart.runCurrent < 20" class="warning-mark">*</span></td><td><span v-if="chart.cpkCurrent < chart.goal" class="warning-mark">#</span></td><td>{{ chart.r1Flag }}</td><td>{{ chart.r2Flag }}</td><td>{{ chart.keyNode }}</td><td>{{ chart.dataType }}</td>
      </tr></tbody></table></div>
      <div v-if="searched && resultRows.length" class="period-note"><span v-if="applied?.getLastMonth">前兩月：{{ periods[0] }}／{{ periods[1] }}</span><span>本期：{{ periods[2] }}</span><span>※ 表格、能力指標與查詢資料皆為 DEV 假資料，不是舊 Core 即時計算。</span></div>
    </section>

    <div v-if="pickerFactor" class="overlay" role="presentation" @click.self="pickerFactorNo = ''">
      <section class="picker-dialog" role="dialog" aria-modal="true" :aria-labelledby="`picker-title-${pickerFactor.factorNo}`">
        <header class="dialog-head"><div><p class="eyebrow">SC312 · DEV lookup</p><h2 :id="`picker-title-${pickerFactor.factorNo}`">選取 {{ pickerFactor.name }}（{{ pickerFactor.factorNo }}）</h2></div><button class="icon-button" type="button" aria-label="關閉" @click="pickerFactorNo = ''">×</button></header>
        <div class="picker-body"><label v-for="option in pickerFactor.options" :key="option.value" class="option-row"><input type="checkbox" :checked="pickerSelection.includes(option.value)" @change="togglePickerValue(option.value, ($event.target as HTMLInputElement).checked)"><span><strong>{{ option.value }}</strong><small>{{ option.name }}</small></span></label></div>
        <footer class="dialog-actions"><span>{{ pickerSelection.length }} 個值</span><button class="button" type="button" @click="pickerFactorNo = ''">Cancel</button><button class="button primary" type="button" @click="applyFactorPicker">確定</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.factor-report-page{height:100%;min-height:0;box-sizing:border-box;padding:16px;display:flex;flex-direction:column;gap:11px;background:#f5f4ef;color:#25251f;font:inherit;overflow:auto}.page-head,.query-footer,.section-heading,.result-heading,.dialog-head,.dialog-actions{display:flex;align-items:center;justify-content:space-between;gap:12px}.page-head h1{margin:1px 0;font-size:22px}.eyebrow{margin:0;color:#7b765f;font-size:10px;font-weight:700;letter-spacing:.09em}.subtitle,.section-heading p{margin:3px 0 0;color:#777365;font-size:11px}.button{min-height:31px;padding:5px 12px;border:1px solid #aaa795;border-radius:4px;background:#fff;color:#292820;font:inherit;font-size:12px;cursor:pointer}.button:hover{border-color:#39382f;background:#efeee7}.button.primary{border-color:#31483d;background:#31483d;color:#fff}.notice{border:1px solid #d5d1bf;border-radius:4px;padding:7px 10px;background:#fbfaf5;color:#5b594b;font-size:11px}.notice.success{border-color:#b9d0bf;background:#f1f8f2;color:#315b3e}.notice.error{border-color:#dfb5aa;background:#fff5f2;color:#8d382b}.panel{border:1px solid #d4d1c5;border-radius:5px;background:#fff;min-width:0;overflow:hidden}.query-panel{padding:12px 14px}.query-grid{display:grid;grid-template-columns:repeat(3,minmax(150px,1fr)) auto;gap:10px 12px;align-items:end}.field{display:grid;gap:4px;font-size:10px;color:#686454}.field input,.field select,.factor-table input{box-sizing:border-box;width:100%;min-height:31px;border:1px solid #cbc8bb;border-radius:3px;padding:5px 7px;background:#fff;color:#292820;font:inherit;font-size:11px}.field input[readonly],.factor-table input[readonly]{background:#f8f7f2}.check-field{display:flex;gap:7px;align-items:center;min-height:31px;font-size:11px;color:#504e43}.factor-section{margin-top:12px;border-top:1px solid #e7e4da;padding-top:10px}.section-heading h2,.result-heading h2{margin:0;font-size:13px}.count-pill{border-radius:11px;background:#f0eee4;color:#696551;padding:4px 8px;font-size:10px;white-space:nowrap}.factor-table-scroll,.result-table-scroll{overflow:auto}.factor-table{width:100%;border-collapse:collapse;font-size:10px}.factor-table th,.factor-table td{padding:6px 8px;border-bottom:1px solid #eeece5;text-align:left;white-space:nowrap}.factor-table th{background:#f8f7f2;color:#686454}.factor-table input{min-width:190px}.select-button{min-width:38px}.query-footer{margin-top:10px;padding-top:9px;border-top:1px solid #e7e4da}.query-footer>span{color:#898474;font-size:10px}.actions{display:flex;gap:6px}.result-panel{display:flex;flex-direction:column;min-height:200px;flex:1}.result-heading{padding:11px 13px;border-bottom:1px solid #e5e2d8}.result-heading .eyebrow{margin-bottom:3px}.result-empty{display:grid;place-items:center;flex:1;min-height:120px;color:#969183;font-size:12px}.result-table-scroll{max-height:calc(100vh - 410px);min-height:130px}.result-table{width:max-content;min-width:100%;border-collapse:collapse;font-size:10px}.result-table th,.result-table td{padding:7px 8px;border-bottom:1px solid #eeece5;text-align:left;white-space:nowrap}.result-table th{position:sticky;top:0;z-index:1;background:#f8f7f2;color:#686454;font-weight:700}.result-table tbody tr:hover{background:#f4f6f0}.warning-mark{color:#a83d2d;font-weight:800}.period-note{display:flex;gap:16px;flex-wrap:wrap;padding:8px 12px;border-top:1px solid #e7e4da;color:#777365;font-size:9px}.overlay{position:fixed;inset:0;z-index:1000;background:#16171180;display:flex;align-items:center;justify-content:center;padding:20px}.picker-dialog{width:min(520px,100%);max-height:calc(100vh - 40px);overflow:auto;border:1px solid #d4d0c3;border-radius:7px;background:#fff;color:#25251f;box-shadow:0 18px 55px #0003}.dialog-head{padding:13px 16px;border-bottom:1px solid #e6e2d8}.dialog-head h2{margin:3px 0 0;font-size:16px}.icon-button{width:30px;height:30px;border:0;background:transparent;font-size:23px;color:#777;cursor:pointer}.picker-body{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:6px;padding:12px;max-height:55vh;overflow:auto}.option-row{display:flex;gap:8px;align-items:center;border:1px solid #e5e2d8;border-radius:4px;padding:8px;font-size:11px}.option-row span{display:grid;gap:2px}.option-row small{color:#827d6f;font-size:9px}.dialog-actions{justify-content:flex-end;padding:10px 13px;border-top:1px solid #e6e2d8}.dialog-actions>span{margin-right:auto;color:#777365;font-size:10px}@media(max-width:980px){.query-grid{grid-template-columns:repeat(2,minmax(150px,1fr))}.factor-table-scroll{overflow-x:auto}.result-table-scroll{max-height:55vh}}@media(max-width:640px){.factor-report-page{height:auto;min-height:100%}.page-head{align-items:flex-start}.query-grid{grid-template-columns:1fr}.query-footer{align-items:flex-start;flex-direction:column}.actions{width:100%;justify-content:flex-end}.picker-body{grid-template-columns:1fr}}
</style>
