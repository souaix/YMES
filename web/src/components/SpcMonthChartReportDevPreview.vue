<script setup lang="ts">
import { computed, ref } from 'vue'

type QcItem = { no: string; name: string; dataType: string; sampleSize: number }
type FactorDefinition = { factorNo: string; name: string; online: boolean; options: string[] }
type ReportCode = { reportNo: string; qcItemNos: string[]; mainQcItemNo: string; factors: Record<string, string> }
type Fixture = {
  id: string; qcItemNo: string; keyNode: string; chartTitle: string; chartUnit: string; sponsor: string
  target: number; us: number; ls: number; goal: number; sampleSize: number; factorValues: Record<string, string>
  erfQty: number; oos: number; ooc: number; avg: number; std: number
  cpkTwoMonthsAgo: number; runTwoMonthsAgo: number; cpkLastMonth: number; runLastMonth: number
  cpCurrent: number; cpkCurrent: number; runCurrent: number; dataCountCurrent: number
}
type AppliedQuery = {
  qcItemNos: string[]; fromDate: string; endDate: string; keyNode: string; combine: boolean; mainQcItemNo: string
  getLastMonth: boolean; factorValues: Record<string, string>
}
type NoticeKind = 'info' | 'success' | 'error'

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

const qcItems: QcItem[] = [
  { no: 'VAR-DEV-101', name: '軸心直徑', dataType: 'InLine', sampleSize: 25 },
  { no: 'VAR-DEV-202', name: '壓合高度', dataType: 'Particle', sampleSize: 25 },
  { no: 'VAR-DEV-305', name: '端面平面度', dataType: 'OffLine', sampleSize: 25 },
  { no: 'VAR-DEV-330', name: '鍍層厚度', dataType: 'InLine', sampleSize: 25 },
  { no: 'VAR-OLD-008', name: '舊型孔徑', dataType: 'InLine · IssueState -1', sampleSize: 25 },
]
const factorCatalog: Record<string, FactorDefinition[]> = {
  'VAR-DEV-101': [
    { factorNo: 'OP', name: '作業站', online: true, options: ['OP-20', 'OP-35', 'OP-50'] },
    { factorNo: 'EQUIPMENT', name: '設備', online: true, options: ['EQ-020', 'EQ-035'] },
  ],
  'VAR-DEV-202': [
    { factorNo: 'OP', name: '作業站', online: true, options: ['OP-20', 'OP-35'] },
    { factorNo: 'CUSTOMER', name: '客戶', online: false, options: ['CUS-DEV-01', 'CUS-DEV-02'] },
  ],
  'VAR-DEV-305': [
    { factorNo: 'PRODUCT', name: '產品', online: true, options: ['PRD-AX17', 'PRD-PH08'] },
    { factorNo: 'OP', name: '作業站', online: true, options: ['OP-10', 'OP-20'] },
  ],
  'VAR-DEV-330': [
    { factorNo: 'CUSTOMER', name: '客戶', online: false, options: ['CUS-DEV-01', 'CUS-DEV-03'] },
    { factorNo: 'LOT_TYPE', name: '批別', online: true, options: ['DEV-A', 'DEV-B'] },
  ],
  'VAR-OLD-008': [{ factorNo: 'OP', name: '作業站', online: true, options: ['OP-10', 'OP-20'] }],
}
const reportCodes: ReportCode[] = [
  { reportNo: 'MCR-VAR-001', qcItemNos: ['VAR-DEV-101', 'VAR-DEV-202'], mainQcItemNo: 'VAR-DEV-101', factors: { OP: 'OP-20', EQUIPMENT: 'EQ-020' } },
  { reportNo: 'MCR-VAR-002', qcItemNos: ['VAR-DEV-305'], mainQcItemNo: '', factors: { PRODUCT: 'PRD-AX17' } },
  { reportNo: 'MCR-VAR-003', qcItemNos: ['VAR-DEV-330'], mainQcItemNo: '', factors: { CUSTOMER: 'CUS-DEV-01' } },
]
const fixtures: Fixture[] = [
  { id: 'cap-101-op20', qcItemNo: 'VAR-DEV-101', keyNode: 'I', chartTitle: '軸心直徑趨勢', chartUnit: 'mm', sponsor: 'DEV-QA', target: 10, us: 10.2, ls: 9.8, goal: 1.33, sampleSize: 25, factorValues: { OP: 'OP-20', EQUIPMENT: 'EQ-020' }, erfQty: 1, oos: 2, ooc: 3, avg: 10.016, std: 0.041, cpkTwoMonthsAgo: 1.42, runTwoMonthsAgo: 34, cpkLastMonth: 1.51, runLastMonth: 31, cpCurrent: 1.71, cpkCurrent: 1.52, runCurrent: 18, dataCountCurrent: 450 },
  { id: 'cap-101-op35', qcItemNo: 'VAR-DEV-101', keyNode: 'I', chartTitle: '軸心直徑趨勢', chartUnit: 'mm', sponsor: 'DEV-QA', target: 10, us: 10.2, ls: 9.8, goal: 1.33, sampleSize: 25, factorValues: { OP: 'OP-35', EQUIPMENT: 'EQ-035' }, erfQty: 0, oos: 1, ooc: 2, avg: 10.009, std: 0.038, cpkTwoMonthsAgo: 1.36, runTwoMonthsAgo: 28, cpkLastMonth: 1.44, runLastMonth: 30, cpCurrent: 1.63, cpkCurrent: 1.39, runCurrent: 23, dataCountCurrent: 575 },
  { id: 'cap-202-op20', qcItemNo: 'VAR-DEV-202', keyNode: 'I', chartTitle: '壓合高度趨勢', chartUnit: 'mm', sponsor: 'DEV-QA', target: 2, us: 2.2, ls: 1.8, goal: 1.33, sampleSize: 25, factorValues: { OP: 'OP-20', CUSTOMER: 'CUS-DEV-01' }, erfQty: 2, oos: 3, ooc: 4, avg: 2.031, std: 0.052, cpkTwoMonthsAgo: 1.28, runTwoMonthsAgo: 30, cpkLastMonth: 1.34, runLastMonth: 27, cpCurrent: 1.48, cpkCurrent: 1.27, runCurrent: 22, dataCountCurrent: 550 },
  { id: 'cap-305-op10', qcItemNo: 'VAR-DEV-305', keyNode: 'C', chartTitle: '端面平面度趨勢', chartUnit: 'um', sponsor: 'DEV-QA', target: 0.025, us: 0.05, ls: 0, goal: 1.33, sampleSize: 25, factorValues: { OP: 'OP-10', PRODUCT: 'PRD-AX17' }, erfQty: 0, oos: 0, ooc: 1, avg: 0.019, std: 0.005, cpkTwoMonthsAgo: 1.40, runTwoMonthsAgo: 26, cpkLastMonth: 1.45, runLastMonth: 29, cpCurrent: 1.59, cpkCurrent: 1.43, runCurrent: 21, dataCountCurrent: 525 },
  { id: 'cap-330-cus01', qcItemNo: 'VAR-DEV-330', keyNode: 'N', chartTitle: '鍍層厚度趨勢', chartUnit: 'um', sponsor: 'DEV-QA', target: 8, us: 9, ls: 7, goal: 1.33, sampleSize: 25, factorValues: { CUSTOMER: 'CUS-DEV-01', LOT_TYPE: 'DEV-A' }, erfQty: 1, oos: 1, ooc: 1, avg: 8.04, std: 0.31, cpkTwoMonthsAgo: 1.22, runTwoMonthsAgo: 25, cpkLastMonth: 1.31, runLastMonth: 27, cpCurrent: 1.44, cpkCurrent: 1.34, runCurrent: 20, dataCountCurrent: 500 },
]

function toLocalDateInput(date: Date): string {
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}
const now = new Date()
const initialEnd = new Date(now)
const initialFrom = new Date(now)
initialFrom.setDate(initialFrom.getDate() + 1)
initialFrom.setMonth(initialFrom.getMonth() - 1)
const queryFromDate = ref(toLocalDateInput(initialFrom))
const queryEndDate = ref(toLocalDateInput(initialEnd))
const keyNode = ref('')
const getLastMonth = ref(true)
const reportNo = ref('')
const qcItemNos = ref<string[]>([])
const combineQcItems = ref(false)
const mainQcItemNo = ref('')
const factorValues = ref<Record<string, string>>({})
const appliedQuery = ref<AppliedQuery | null>(null)
const searched = ref(false)
const pickerMode = ref<'report' | 'qc' | null>(null)
const pickerSelection = ref<string[]>([])
const notice = ref('選取月報表編碼或 QC Item 後按 Query；此頁只使用 DEV 假資料。')
const noticeKind = ref<NoticeKind>('info')

const selectedItems = computed(() => qcItemNos.value.map((no) => qcItems.find((item) => item.no === no)).filter((item): item is QcItem => Boolean(item)))
const factorRows = computed(() => {
  const factors = new Map<string, FactorDefinition>()
  selectedItems.value.forEach((item) => factorCatalog[item.no]?.forEach((factor) => { if (!factors.has(factor.factorNo)) factors.set(factor.factorNo, factor) }))
  return [...factors.values()]
})
const resultRows = computed(() => {
  const applied = appliedQuery.value
  if (!applied) return []
  const matches = fixtures.filter((fixture) => {
    if (!applied.qcItemNos.includes(fixture.qcItemNo)) return false
    if (applied.keyNode && fixture.keyNode !== applied.keyNode) return false
    return Object.entries(applied.factorValues).every(([factorNo, raw]) => {
      const wanted = raw.split(',').map((value) => value.trim().toUpperCase()).filter(Boolean)
      return wanted.length === 0 || wanted.includes((fixture.factorValues[factorNo] ?? '').toUpperCase())
    })
  })
  const outputFixtures = applied.combine && applied.mainQcItemNo
    ? matches.filter((fixture) => fixture.qcItemNo === applied.mainQcItemNo)
    : matches
  const combinedItemNo = applied.qcItemNos.join(',')
  const combinedItemName = applied.qcItemNos.map(itemName).join(', ')
  return outputFixtures.map((fixture) => ({
    ...fixture,
    queryQcItemNo: applied.combine && applied.mainQcItemNo ? combinedItemNo : fixture.qcItemNo,
    queryQcItemName: applied.combine && applied.mainQcItemNo ? combinedItemName : itemName(fixture.qcItemNo),
  }))
})
const periodLabels = computed(() => {
  const applied = appliedQuery.value
  if (!applied) return []
  const from = new Date(`${applied.fromDate}T00:00:00`)
  const end = new Date(`${applied.endDate}T23:59:59`)
  const monthLabel = (date: Date) => `${date.getFullYear()}/${String(date.getMonth() + 1).padStart(2, '0')}`
  const firstPrevious = new Date(from.getFullYear(), from.getMonth() - 2, 1)
  const secondPrevious = new Date(from.getFullYear(), from.getMonth() - 1, 1)
  return [monthLabel(firstPrevious), monthLabel(secondPrevious), `${applied.fromDate} 至 ${toLocalDateInput(end)}`]
})

function qcLabel(qcItemNo: string): string {
  const item = qcItems.find((candidate) => candidate.no === qcItemNo)
  return item ? `${item.no} · ${item.name}` : qcItemNo
}
function setQcItemNos(next: string[]): void {
  const previous = factorValues.value
  qcItemNos.value = [...new Set(next)]
  factorValues.value = Object.fromEntries([...new Set(qcItemNos.value.flatMap((no) => factorCatalog[no]?.map((factor) => factor.factorNo) ?? []))].map((factorNo) => [factorNo, previous[factorNo] ?? '']))
  if (!qcItemNos.value.includes(mainQcItemNo.value)) mainQcItemNo.value = ''
  if (qcItemNos.value.length < 2) { combineQcItems.value = false; mainQcItemNo.value = '' }
}
function openReportPicker(): void { pickerMode.value = 'report'; pickerSelection.value = [] }
function openQcPicker(): void { pickerMode.value = 'qc'; pickerSelection.value = [...qcItemNos.value] }
function chooseReport(code: ReportCode): void {
  reportNo.value = code.reportNo
  setQcItemNos(code.qcItemNos)
  factorValues.value = { ...factorValues.value, ...code.factors }
  combineQcItems.value = Boolean(code.mainQcItemNo)
  mainQcItemNo.value = code.mainQcItemNo
  pickerMode.value = null
  notice.value = `${code.reportNo} 已載入；ReportNo 只帶入 QC Item、Main 與 Factor 條件。`
  noticeKind.value = 'info'
}
function applyQcPicker(): void {
  reportNo.value = ''
  setQcItemNos(pickerSelection.value)
  pickerMode.value = null
  notice.value = 'QC Item 已選取；月報表編碼欄位已清除。'
  noticeKind.value = 'info'
}
function togglePickerItem(qcItemNo: string, checked: boolean): void {
  pickerSelection.value = checked ? [...pickerSelection.value, qcItemNo] : pickerSelection.value.filter((item) => item !== qcItemNo)
}
function runQuery(): void {
  if (!qcItemNos.value.length) {
    notice.value = '請先選擇月報表編碼或 QC Item。'
    noticeKind.value = 'error'
    return
  }
  if (!queryFromDate.value || !queryEndDate.value || queryFromDate.value > queryEndDate.value) {
    notice.value = '請確認起日與迄日，起日不可晚於迄日。'
    noticeKind.value = 'error'
    return
  }
  if (combineQcItems.value && qcItemNos.value.length > 1 && !mainQcItemNo.value) {
    notice.value = '勾選 Combine QCItem 後，請指定 Main QC Item。'
    noticeKind.value = 'error'
    return
  }
  if (combineQcItems.value && new Set(selectedItems.value.map((item) => item.sampleSize)).size > 1) {
    notice.value = '所選 QC Item 的 SampleSize 不一致；來源 Service 會執行合併樣本數檢核。'
    noticeKind.value = 'error'
    return
  }
  appliedQuery.value = {
    qcItemNos: [...qcItemNos.value], fromDate: queryFromDate.value, endDate: queryEndDate.value, keyNode: keyNode.value,
    combine: combineQcItems.value, mainQcItemNo: mainQcItemNo.value, getLastMonth: getLastMonth.value,
    factorValues: { ...factorValues.value },
  }
  searched.value = true
  notice.value = `查詢完成：${resultRows.value.length} 筆管制圖月報列（DEV 假資料）。`
  noticeKind.value = resultRows.value.length ? 'success' : 'info'
}
function itemName(qcItemNo: string): string { return qcItems.find((item) => item.no === qcItemNo)?.name ?? qcItemNo }
function displayFactor(fixture: Fixture): string {
  return Object.entries(fixture.factorValues).map(([factorNo, value]) => `${factorNo}=${value}`).join(' · ')
}
function decimal(value: number): string { return value.toFixed(3) }
</script>

<template>
  <section v-if="props.developmentPreview" class="month-report-page" aria-label="SPC 管制圖月報表">
    <header class="page-head"><div><p class="eyebrow">SPC / SC303</p><h1>管制圖月報表</h1><p class="subtitle">依 QC Item、期間、Key Node 與 Factor 條件彙整計量管制圖能力。</p></div><button class="button" type="button" @click="emit('close')">Close (X)</button></header>
    <div class="notice" :class="noticeKind" role="status">{{ notice }}</div>

    <section class="query-panel panel">
      <div class="query-grid">
        <label class="field report-field"><span>Month Report Code</span><div class="input-action"><input :value="reportNo" readonly placeholder="選擇既有月報表編碼"><button class="button" type="button" @click="openReportPicker">...</button></div></label>
        <label class="field qc-field"><span>QCItem</span><div class="input-action"><input :value="qcItemNos.map((item) => item).join(', ')" readonly placeholder="選擇 Variable QC Item"><button class="button" type="button" @click="openQcPicker">...</button></div></label>
        <label class="field"><span>From Query Date</span><input v-model="queryFromDate" type="date"></label>
        <label class="field"><span>End Query Date</span><input v-model="queryEndDate" type="date"></label>
        <label class="field"><span>Key_Node</span><select v-model="keyNode"><option value="">全部</option><option value="I">I</option><option value="C">C</option><option value="N">N</option></select></label>
        <label class="check-field"><input v-model="combineQcItems" type="checkbox" :disabled="qcItemNos.length < 2"> CombineQCItem</label>
        <label v-if="combineQcItems && qcItemNos.length > 1" class="field"><span>Main QC Item</span><select v-model="mainQcItemNo"><option value="">選擇主要計算項目</option><option v-for="item in selectedItems" :key="item.no" :value="item.no">{{ item.no }} · {{ item.name }}</option></select></label>
        <label class="check-field"><input v-model="getLastMonth" type="checkbox"> Get Last Month（顯示前兩個月）</label>
      </div>
      <section class="factor-section"><div class="section-heading"><div><h2>Collect Factor</h2><p>依所選 QC Item 聯集 Factor；以逗號輸入多個值。</p></div><span v-if="factorRows.length" class="count-pill">{{ factorRows.length }} 個 Factor</span></div>
        <div v-if="factorRows.length" class="factor-grid"><label v-for="factor in factorRows" :key="factor.factorNo" class="factor-control"><span><strong>{{ factor.factorNo }}</strong><small>{{ factor.name }} · {{ factor.online ? 'Online' : 'Offline' }}</small></span><input v-model="factorValues[factor.factorNo]" :list="`month-factor-${factor.factorNo}`" placeholder="全部"><datalist :id="`month-factor-${factor.factorNo}`"><option v-for="option in factor.options" :key="option" :value="option"></option></datalist></label></div>
        <p v-else class="empty-hint">選取 QC Item 後載入收集要因。</p>
      </section>
      <footer class="query-footer"><span>預設起日為今日加一日再減一個月；Combine 結果以 Main 列示範，統計為固定假資料。</span><button class="button primary" type="button" @click="runQuery">Query</button></footer>
    </section>

    <section class="result-panel panel">
      <div class="result-heading"><div><p class="eyebrow">MONTHLY CHART REPORT</p><h2>月報結果</h2></div><div v-if="searched" class="result-meta"><span>{{ resultRows.length }} 筆</span></div></div>
      <div v-if="!searched" class="result-empty">設定查詢條件後按 Query 顯示月報結果。</div>
      <div v-else-if="resultRows.length === 0" class="result-empty">此條件沒有符合的 DEV 管制圖資料。</div>
      <div v-else class="result-table-scroll"><table class="result-table"><thead><tr>
        <th>QCItemNo</th><th>QCItemName</th><th>ChartFactors</th><th>CHART TITLE</th><th>DATA_UNIT</th><th>TARGET</th><th>U_SPEC</th><th>L_SPEC</th><th>RUN#</th><th>Data_CNT</th><th>#OOS</th><th>%OOS</th><th>#OOC</th><th>%OOC</th><th>AVG</th><th>STD</th><th>CP</th><th>CPK</th><th>ERFQty</th><th>CPK_GOAL</th><th>L2CPK</th><th>L2RUN#</th><th>LRUN#</th><th>LCPK</th><th>SPONSOR</th><th>Run&lt;20</th><th>Cpk&lt;Goal</th><th>R1&gt;25%</th><th>R2&gt;20%</th><th>Key_Node</th><th>Data_Type</th>
      </tr></thead><tbody><tr v-for="fixture in resultRows" :key="fixture.id">
        <td>{{ fixture.queryQcItemNo }}</td><td>{{ fixture.queryQcItemName }}</td><td>{{ displayFactor(fixture) }}</td><td>{{ fixture.chartTitle }}</td><td>{{ fixture.chartUnit }}</td><td>{{ decimal(fixture.target) }}</td><td>{{ decimal(fixture.us) }}</td><td>{{ decimal(fixture.ls) }}</td><td>{{ fixture.runCurrent }}</td><td>{{ fixture.dataCountCurrent }}</td><td>{{ fixture.oos }}</td><td>{{ decimal(fixture.oos / (fixture.runCurrent * fixture.sampleSize)) }}</td><td>{{ fixture.ooc }}</td><td>{{ decimal(fixture.ooc / (fixture.runCurrent * fixture.sampleSize)) }}</td><td>{{ decimal(fixture.avg) }}</td><td>{{ decimal(fixture.std) }}</td><td>{{ decimal(fixture.cpCurrent) }}</td><td>{{ decimal(fixture.cpkCurrent) }}</td><td>{{ fixture.erfQty }}</td><td>{{ decimal(fixture.goal) }}</td><td>{{ appliedQuery?.getLastMonth ? decimal(fixture.cpkTwoMonthsAgo) : '—' }}</td><td>{{ appliedQuery?.getLastMonth ? fixture.runTwoMonthsAgo : '—' }}</td><td>{{ appliedQuery?.getLastMonth ? fixture.runLastMonth : '—' }}</td><td>{{ appliedQuery?.getLastMonth ? decimal(fixture.cpkLastMonth) : '—' }}</td><td>{{ fixture.sponsor }}</td><td><span v-if="fixture.runCurrent < 20" class="warning-mark">*</span></td><td><span v-if="fixture.cpkCurrent < fixture.goal" class="warning-mark">#</span></td><td></td><td></td><td>{{ fixture.keyNode }}</td><td>{{ qcItems.find((item) => item.no === fixture.qcItemNo)?.dataType }}</td>
      </tr></tbody></table></div>
      <div v-if="searched && resultRows.length" class="period-note"><span v-if="appliedQuery?.getLastMonth">前兩月：{{ periodLabels[0] }}／{{ periodLabels[1] }}</span><span>本期：{{ periodLabels[2] }}</span><span>※ 表格數值均為 DEV 假資料；PP／PPK 舊畫面計算但 Grid 隱藏。</span></div>
    </section>

    <div v-if="pickerMode" class="overlay" role="presentation" @click.self="pickerMode = null">
      <section class="picker-dialog" role="dialog" aria-modal="true" :aria-labelledby="pickerMode === 'report' ? 'report-picker-title' : 'qc-picker-title'">
        <header class="dialog-head"><div><p class="eyebrow">SC303 · DEV lookup</p><h2 v-if="pickerMode === 'report'" id="report-picker-title">選擇月報表編碼</h2><h2 v-else id="qc-picker-title">選擇 Variable QC Item</h2></div><button class="icon-button" type="button" aria-label="關閉" @click="pickerMode = null">×</button></header>
        <div class="picker-body">
          <template v-if="pickerMode === 'report'"><button v-for="code in reportCodes" :key="code.reportNo" class="report-option" type="button" @click="chooseReport(code)"><strong>{{ code.reportNo }}</strong><span>{{ code.qcItemNos.map(qcLabel).join('、') }}</span><small>Factor：{{ Object.entries(code.factors).map(([factorNo, value]) => `${factorNo}=${value}`).join(' · ') || '—' }}</small></button></template>
          <template v-else><label v-for="item in qcItems" :key="item.no" class="qc-option"><input type="checkbox" :checked="pickerSelection.includes(item.no)" @change="togglePickerItem(item.no, ($event.target as HTMLInputElement).checked)"><span><strong>{{ item.no }} · {{ item.name }}</strong><small>{{ item.dataType }} · ChartControl=1 · SampleSize={{ item.sampleSize }}</small></span></label></template>
        </div>
        <footer v-if="pickerMode === 'qc'" class="dialog-actions"><span>{{ pickerSelection.length }} 個 QC Item</span><button class="button" type="button" @click="pickerMode = null">Cancel</button><button class="button primary" type="button" @click="applyQcPicker">確定</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.month-report-page{height:100%;min-height:0;box-sizing:border-box;padding:16px;display:flex;flex-direction:column;gap:11px;background:#f5f4ef;color:#25251f;font:inherit;overflow:auto}.page-head,.query-footer,.section-heading,.result-heading,.dialog-head,.dialog-actions{display:flex;align-items:center;justify-content:space-between;gap:12px}.page-head h1{margin:1px 0;font-size:23px}.eyebrow{margin:0;color:#7b765f;font-size:10px;font-weight:700;letter-spacing:.09em}.subtitle,.section-heading p{margin:3px 0 0;color:#777365;font-size:11px}.button{min-height:31px;padding:5px 12px;border:1px solid #aaa795;border-radius:4px;background:#fff;color:#292820;font:inherit;font-size:12px;cursor:pointer}.button:hover{border-color:#39382f;background:#efeee7}.button.primary{border-color:#31483d;background:#31483d;color:#fff}.notice{border:1px solid #d5d1bf;border-radius:4px;padding:7px 10px;background:#fbfaf5;color:#5b594b;font-size:11px}.notice.success{border-color:#b9d0bf;background:#f1f8f2;color:#315b3e}.notice.error{border-color:#dfb5aa;background:#fff5f2;color:#8d382b}.panel{border:1px solid #d4d1c5;border-radius:5px;background:#fff;min-width:0;overflow:hidden}.query-panel{padding:12px 14px}.query-grid{display:grid;grid-template-columns:minmax(220px,1.2fr) minmax(220px,1.4fr) repeat(2,minmax(145px,.8fr)) minmax(100px,.55fr);gap:10px 12px;align-items:end}.field{display:grid;gap:4px;font-size:10px;color:#686454}.field input,.field select,.input-action input,.factor-control input{box-sizing:border-box;width:100%;min-height:31px;border:1px solid #cbc8bb;border-radius:3px;padding:5px 7px;background:#fff;color:#292820;font:inherit;font-size:11px}.field input[readonly],.input-action input[readonly]{background:#f8f7f2}.input-action{display:flex;gap:4px}.input-action .button{min-width:36px}.check-field{display:flex;gap:7px;align-items:center;min-height:31px;font-size:11px;color:#504e43}.factor-section{margin-top:12px;border-top:1px solid #e7e4da;padding-top:10px}.section-heading h2,.result-heading h2{margin:0;font-size:13px}.count-pill,.result-meta span{border-radius:11px;background:#f0eee4;color:#696551;padding:4px 8px;font-size:10px;white-space:nowrap}.factor-grid{display:grid;grid-template-columns:repeat(3,minmax(190px,1fr));gap:7px;margin-top:8px}.factor-control{display:grid;grid-template-columns:minmax(95px,.65fr) minmax(100px,1fr);gap:7px;align-items:center;border:1px solid #e5e2d8;border-radius:4px;padding:7px}.factor-control>span{display:grid;gap:2px;font-size:10px}.factor-control small{font-size:9px;color:#817c6d}.empty-hint{margin:9px 0 2px;color:#969183;font-size:10px}.query-footer{margin-top:10px;padding-top:9px;border-top:1px solid #e7e4da}.query-footer>span{color:#898474;font-size:10px}.result-panel{display:flex;flex-direction:column;min-height:200px;flex:1}.result-heading{padding:11px 13px;border-bottom:1px solid #e5e2d8}.result-heading .eyebrow{margin-bottom:3px}.result-meta{display:flex;gap:6px}.result-empty{display:grid;place-items:center;flex:1;min-height:120px;color:#969183;font-size:12px}.result-table-scroll{overflow:auto;max-height:calc(100vh - 400px);min-height:130px}.result-table{width:max-content;min-width:100%;border-collapse:collapse;font-size:10px}.result-table th,.result-table td{padding:7px 8px;border-bottom:1px solid #eeece5;text-align:left;white-space:nowrap}.result-table th{position:sticky;top:0;z-index:1;background:#f8f7f2;color:#686454;font-weight:700}.result-table tbody tr:hover{background:#f4f6f0}.warning-mark{color:#a83d2d;font-weight:800}.period-note{display:flex;gap:16px;flex-wrap:wrap;padding:8px 12px;border-top:1px solid #e7e4da;color:#777365;font-size:9px}.overlay{position:fixed;inset:0;z-index:1000;background:#16171180;display:flex;align-items:center;justify-content:center;padding:20px}.picker-dialog{width:min(620px,100%);max-height:calc(100vh - 40px);overflow:auto;border:1px solid #d4d0c3;border-radius:7px;background:#fff;color:#25251f;box-shadow:0 18px 55px #0003}.dialog-head{padding:13px 16px;border-bottom:1px solid #e6e2d8}.dialog-head h2{margin:3px 0 0;font-size:17px}.icon-button{width:30px;height:30px;border:0;background:transparent;font-size:23px;color:#777;cursor:pointer}.picker-body{display:grid;gap:6px;padding:12px;max-height:55vh;overflow:auto}.report-option,.qc-option{width:100%;display:grid;grid-template-columns:auto 1fr;gap:3px 9px;align-items:start;border:1px solid #e5e2d8;border-radius:4px;padding:9px;background:#fff;text-align:left;font:inherit;font-size:11px;color:#333;cursor:pointer}.report-option:hover{background:#f4f6f0;border-color:#9ba997}.report-option strong{grid-column:1/-1;color:#315d47}.report-option span{grid-column:1/-1}.report-option small,.qc-option small{color:#827d6f;font-size:9px}.qc-option{grid-template-columns:16px 1fr;cursor:default}.qc-option>span{display:grid;gap:4px}.dialog-actions{justify-content:flex-end;padding:10px 13px;border-top:1px solid #e6e2d8}.dialog-actions>span{margin-right:auto;color:#777365;font-size:10px}@media(max-width:980px){.query-grid{grid-template-columns:repeat(3,minmax(150px,1fr))}.factor-grid{grid-template-columns:repeat(2,minmax(180px,1fr))}.result-table-scroll{max-height:55vh}}@media(max-width:640px){.month-report-page{height:auto;min-height:100%}.page-head{align-items:flex-start}.query-grid,.factor-grid{grid-template-columns:1fr}.query-footer{align-items:flex-start}.query-footer>span{line-height:1.5}.factor-control{grid-template-columns:1fr}.period-note{display:grid;gap:5px}}
</style>
