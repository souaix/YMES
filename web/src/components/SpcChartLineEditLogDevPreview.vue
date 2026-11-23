<script setup lang="ts">
import { computed, ref } from 'vue'

interface QcItem { no: string; name: string; issueState: 2 | -1 }
interface LookupItem { no: string; name: string; issueState: 2 | -1 }
interface ChartSnapshot {
  factorSerial: string
  spcId: 0 | 1 | 2 | 3 | 4
  creator: string
  startTime: string
  qcItemNo: string
  qcItemName: string
  productNo: string
  productName: string
  customerNo: string
  customerName: string
  materialNo: string
  materialName: string
  vendorNo: string
  vendorName: string
  factorValues: Record<string, string>
  us: string | null
  cs: string | null
  ls: string | null
  ucl: string | null
  cl: string | null
  lcl: string | null
  memo: string
}
interface HistoryRow {
  id: string
  factorSerial: string
  changeType: string
  userId: string
  recordDate: string
  productName: string
  customerName: string
  materialName: string
  vendorName: string
  factorValues: Record<string, string>
  qcItemNo: string
  qcItemName: string
  controlChart: string
  values: Partial<Record<'US' | 'CS' | 'LS' | 'UCL' | 'CL' | 'LCL' | 'MEMO', string>>
}
interface QueryState {
  qcItemNo: string
  productNo: string
  customerNo: string
  materialNo: string
  vendorNo: string
  fromDate: string
  endDate: string
  fromEnabled: boolean
  endEnabled: boolean
}

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const qcItems: QcItem[] = [
  { no: 'VAR-DEV-101', name: '軸心直徑', issueState: 2 },
  { no: 'VAR-DEV-102', name: '壓合高度', issueState: 2 },
  { no: 'VAR-DEV-109', name: '舊型端面寬度', issueState: -1 },
]
const products: LookupItem[] = [
  { no: 'PRD-AX17', name: '精密傳動軸 AX17', issueState: 2 },
  { no: 'PRD-PH08', name: '定位座 PH08', issueState: 2 },
  { no: 'PRD-OLD3', name: '舊型連接器', issueState: -1 },
]
const customers: LookupItem[] = [
  { no: 'CUS-DEV-01', name: '啟明電子', issueState: 2 },
  { no: 'CUS-DEV-02', name: '宏遠工業', issueState: 2 },
  { no: 'CUS-OLD-01', name: '舊客戶樣本', issueState: -1 },
]
const materials: LookupItem[] = [
  { no: 'MTL-SS31', name: '不鏽鋼棒材 SS31', issueState: 2 },
  { no: 'MTL-AL08', name: '鋁合金料 AL08', issueState: 2 },
  { no: 'MTL-OLD2', name: '停用材料 OLD2', issueState: -1 },
]
const vendors: LookupItem[] = [
  { no: 'VEN-DEV-01', name: '大川材料', issueState: 2 },
  { no: 'VEN-DEV-02', name: '明新金屬', issueState: 2 },
  { no: 'VEN-OLD-01', name: '停用供應商', issueState: -1 },
]
const onlineFactorFields = ['MO', 'OP', 'EQUIPMENT', 'BR']
const snapshots: ChartSnapshot[] = [
  { factorSerial: 'DEV-CHART-001', spcId: 1, creator: 'DEV-QC-01', startTime: '2026-09-25T09:10:00', qcItemNo: 'VAR-DEV-101', qcItemName: '軸心直徑', productNo: 'PRD-AX17', productName: '精密傳動軸 AX17', customerNo: 'CUS-DEV-01', customerName: '啟明電子', materialNo: 'MTL-SS31', materialName: '不鏽鋼棒材 SS31', vendorNo: 'VEN-DEV-01', vendorName: '大川材料', factorValues: { MO: 'MO-2618', OP: 'OP-20', EQUIPMENT: 'EQP-020', BR: 'BR-A' }, us: '10.20', cs: '10.00', ls: '9.80', ucl: '1.15', cl: '1.00', lcl: '0.85', memo: '初始管制圖設定' },
  { factorSerial: 'DEV-CHART-001', spcId: 1, creator: 'DEV-QC-02', startTime: '2026-10-18T13:42:00', qcItemNo: 'VAR-DEV-101', qcItemName: '軸心直徑', productNo: 'PRD-AX17', productName: '精密傳動軸 AX17', customerNo: 'CUS-DEV-01', customerName: '啟明電子', materialNo: 'MTL-SS31', materialName: '不鏽鋼棒材 SS31', vendorNo: 'VEN-DEV-01', vendorName: '大川材料', factorValues: { MO: 'MO-2618', OP: 'OP-20', EQUIPMENT: 'EQP-020', BR: 'BR-A' }, us: '10.25', cs: '10.00', ls: '9.80', ucl: '1.15', cl: '1.00', lcl: '0.85', memo: '依新圖面調整上規格' },
  { factorSerial: 'DEV-CHART-001', spcId: 1, creator: 'DEV-QC-01', startTime: '2026-11-20T15:30:00', qcItemNo: 'VAR-DEV-101', qcItemName: '軸心直徑', productNo: 'PRD-AX17', productName: '精密傳動軸 AX17', customerNo: 'CUS-DEV-01', customerName: '啟明電子', materialNo: 'MTL-SS31', materialName: '不鏽鋼棒材 SS31', vendorNo: 'VEN-DEV-01', vendorName: '大川材料', factorValues: { MO: 'MO-2618', OP: 'OP-20', EQUIPMENT: 'EQP-020', BR: 'BR-A' }, us: '10.25', cs: '10.00', ls: '9.80', ucl: '1.18', cl: '1.00', lcl: '0.82', memo: '依最新製程數據調整管制界線' },
  { factorSerial: 'DEV-CHART-002', spcId: 2, creator: 'DEV-QC-03', startTime: '2026-10-03T08:25:00', qcItemNo: 'VAR-DEV-102', qcItemName: '壓合高度', productNo: 'PRD-PH08', productName: '定位座 PH08', customerNo: 'CUS-DEV-02', customerName: '宏遠工業', materialNo: 'MTL-AL08', materialName: '鋁合金料 AL08', vendorNo: 'VEN-DEV-02', vendorName: '明新金屬', factorValues: { MO: 'MO-2616', OP: 'OP-35', EQUIPMENT: 'EQP-035', BR: 'BR-C' }, us: '2.20', cs: '2.00', ls: '1.80', ucl: '0.24', cl: '0.12', lcl: '0.00', memo: '新增壓合高度 R Chart' },
  { factorSerial: 'DEV-CHART-002', spcId: 2, creator: 'DEV-QC-03', startTime: '2026-11-16T10:05:00', qcItemNo: 'VAR-DEV-102', qcItemName: '壓合高度', productNo: 'PRD-PH08', productName: '定位座 PH08', customerNo: 'CUS-DEV-02', customerName: '宏遠工業', materialNo: 'MTL-AL08', materialName: '鋁合金料 AL08', vendorNo: 'VEN-DEV-02', vendorName: '明新金屬', factorValues: { MO: 'MO-2616', OP: 'OP-35', EQUIPMENT: 'EQP-035', BR: 'BR-C' }, us: '2.20', cs: '2.00', ls: '1.80', ucl: '0.26', cl: '0.13', lcl: '0.00', memo: '重新計算 R Chart 界線' },
  { factorSerial: 'DEV-CHART-003', spcId: 0, creator: 'DEV-QC-04', startTime: '2026-08-20T16:55:00', qcItemNo: 'VAR-DEV-109', qcItemName: '舊型端面寬度', productNo: 'PRD-OLD3', productName: '舊型連接器', customerNo: 'CUS-OLD-01', customerName: '舊客戶樣本', materialNo: 'MTL-OLD2', materialName: '停用材料 OLD2', vendorNo: 'VEN-OLD-01', vendorName: '停用供應商', factorValues: { MO: 'MO-2608', OP: 'OP-05', EQUIPMENT: 'EQP-005', BR: 'BR-Z' }, us: '6.00', cs: '5.80', ls: '5.60', ucl: '0.30', cl: '0.15', lcl: '0.00', memo: '歷史趨勢圖設定' },
]

const query = ref<QueryState>({
  qcItemNo: '', productNo: '', customerNo: '', materialNo: '', vendorNo: '',
  fromDate: '2026-09-24T00:00:00', endDate: '2026-11-23T23:59:59', fromEnabled: false, endEnabled: false,
})
const appliedQuery = ref<QueryState | null>(null)
const searched = ref(false)
const notice = ref('選擇查詢條件後按 Query；此頁只使用 DEV 假資料。')
const noticeKind = ref<'info' | 'success' | 'error'>('info')

const historyRows = computed(() => reconstructHistory(snapshots))
const filteredRows = computed(() => {
  const applied = appliedQuery.value
  if (!applied) return []
  return historyRows.value.filter((row) => {
    const raw = snapshots.find((snapshot) => snapshot.factorSerial === row.factorSerial && snapshot.startTime === row.recordDate)
    if (!raw) return false
    if (applied.qcItemNo && raw.qcItemNo !== applied.qcItemNo) return false
    if (applied.productNo && raw.productNo !== applied.productNo) return false
    if (applied.customerNo && raw.customerNo !== applied.customerNo) return false
    if (applied.materialNo && raw.materialNo !== applied.materialNo) return false
    if (applied.vendorNo && raw.vendorNo !== applied.vendorNo) return false
    // WinForms sends no dates to Service/Core; it strictly filters the reconstructed rows afterwards.
    if (applied.fromEnabled && row.recordDate <= applied.fromDate) return false
    if (applied.endEnabled && row.recordDate >= applied.endDate) return false
    return true
  }).sort((left, right) => left.factorSerial.localeCompare(right.factorSerial) || left.recordDate.localeCompare(right.recordDate) || left.id.localeCompare(right.id))
})

function reconstructHistory(rows: ChartSnapshot[]): HistoryRow[] {
  const ordered = [...rows].sort((left, right) => left.factorSerial.localeCompare(right.factorSerial) || left.startTime.localeCompare(right.startTime))
  const output: HistoryRow[] = []
  const previousByFactor = new Map<string, ChartSnapshot>()
  for (const current of ordered) {
    const hasAnyLimit = [current.us, current.cs, current.ls, current.ucl, current.cl, current.lcl].some((value) => value !== null)
    if (!hasAnyLimit) continue
    const previous = previousByFactor.get(current.factorSerial)
    if (!previous) {
      output.push(toHistoryRow(current, '新增', current))
      previousByFactor.set(current.factorSerial, current)
      continue
    }

    const specChanges = changedValues(current, previous, ['US', 'CS', 'LS'])
    if (Object.keys(specChanges).length) output.push(toHistoryRow(current, '規格修改', current, specChanges))
    const lineChanges = changedValues(current, previous, ['UCL', 'CL', 'LCL'])
    if (Object.keys(lineChanges).length) output.push(toHistoryRow(current, '管制線修改', current, lineChanges))
    previousByFactor.set(current.factorSerial, current)
  }
  return output
}

function changedValues(current: ChartSnapshot, previous: ChartSnapshot, fields: Array<'US' | 'CS' | 'LS' | 'UCL' | 'CL' | 'LCL'>): Partial<Record<'US' | 'CS' | 'LS' | 'UCL' | 'CL' | 'LCL' | 'MEMO', string>> {
  const values: Partial<Record<'US' | 'CS' | 'LS' | 'UCL' | 'CL' | 'LCL' | 'MEMO', string>> = {}
  for (const field of fields) {
    const key = field.toLowerCase() as 'us' | 'cs' | 'ls' | 'ucl' | 'cl' | 'lcl'
    const currentValue = current[key] ?? ''
    const previousValue = previous[key] ?? ''
    if (currentValue !== previousValue) values[field] = currentValue || '（清除）'
  }
  if (Object.keys(values).length) values.MEMO = current.memo
  return values
}

function toHistoryRow(snapshot: ChartSnapshot, changeType: string, source: ChartSnapshot, values?: HistoryRow['values']): HistoryRow {
  return {
    id: `${snapshot.factorSerial}-${snapshot.startTime}-${changeType}`,
    factorSerial: snapshot.factorSerial,
    changeType,
    userId: snapshot.creator,
    recordDate: snapshot.startTime,
    productName: snapshot.productName,
    customerName: snapshot.customerName,
    materialName: snapshot.materialName,
    vendorName: snapshot.vendorName,
    factorValues: { ...snapshot.factorValues },
    qcItemNo: snapshot.qcItemNo,
    qcItemName: snapshot.qcItemName,
    controlChart: chartName(snapshot.spcId),
    values: values ?? { US: source.us ?? '', CS: source.cs ?? '', LS: source.ls ?? '', UCL: source.ucl ?? '', CL: source.cl ?? '', LCL: source.lcl ?? '', MEMO: source.memo },
  }
}

function chartName(spcId: ChartSnapshot['spcId']): string {
  return ({ 0: 'Trend Chart', 1: 'Xbar Chart', 2: 'R Chart', 3: 'S Chart', 4: 'Rm Chart' })[spcId]
}
function itemLabel(item: LookupItem | QcItem): string { return `${item.no} — ${item.name}（${item.issueState === 2 ? 'Issue' : 'Stop'}）` }
function clearFilter(key: 'qcItemNo' | 'productNo' | 'customerNo' | 'materialNo' | 'vendorNo'): void { query.value[key] = '' }
function runQuery(): void {
  appliedQuery.value = { ...query.value }
  searched.value = true
  notice.value = `查詢完成：${filteredRows.value.length} 筆 DEV 管制線歷程。日期條件依舊程式在歷程重建後套用。`
  noticeKind.value = 'success'
}
function displayValue(value: string | undefined): string { return value || '—' }
function formatDate(value: string): string { return value.replace('T', ' ') }
</script>

<template>
  <section v-if="props.developmentPreview" class="line-edit-page" aria-label="SPC 管制線修改紀錄">
    <header class="page-head">
      <div><p class="eyebrow">SPC / SC215</p><h1>管制線修改紀錄</h1><p class="subtitle">從管制圖設定歷程重建新增、規格修改與管制線修改；結果唯讀。</p></div>
      <button class="button" type="button" @click="emit('close')">Close (X)</button>
    </header>

    <section class="query-panel panel">
      <div class="query-caption"><strong>Query</strong><span class="type-chip">QC Type: Variable</span><span class="source-note">原 Form 隱藏 QC Type 選項，啟動預設 Variable</span><span class="dev-badge">DEV 假資料</span></div>
      <div class="query-grid">
        <div class="lookup-field"><label><input type="checkbox" :checked="!!query.qcItemNo" @change="clearFilter('qcItemNo')"> QCItem</label><select v-model="query.qcItemNo"><option value="">全部 QC Item</option><option v-for="item in qcItems" :key="item.no" :value="item.no">{{ itemLabel(item) }}</option></select></div>
        <div class="lookup-field"><label><input type="checkbox" :checked="!!query.productNo" @change="clearFilter('productNo')"> Product Name</label><select v-model="query.productNo"><option value="">全部 Product</option><option v-for="item in products" :key="item.no" :value="item.no">{{ itemLabel(item) }}</option></select></div>
        <div class="lookup-field"><label><input type="checkbox" :checked="!!query.materialNo" @change="clearFilter('materialNo')"> Material Name</label><select v-model="query.materialNo"><option value="">全部 Material</option><option v-for="item in materials" :key="item.no" :value="item.no">{{ itemLabel(item) }}</option></select></div>
        <div class="lookup-field"><label><input type="checkbox" :checked="!!query.vendorNo" @change="clearFilter('vendorNo')"> Vendor Name</label><select v-model="query.vendorNo"><option value="">全部 Vendor</option><option v-for="item in vendors" :key="item.no" :value="item.no">{{ itemLabel(item) }}</option></select></div>
        <div class="lookup-field"><label><input type="checkbox" :checked="!!query.customerNo" @change="clearFilter('customerNo')"> Customer Name</label><select v-model="query.customerNo"><option value="">全部 Customer</option><option v-for="item in customers" :key="item.no" :value="item.no">{{ itemLabel(item) }}</option></select></div>
        <div class="date-field"><label><input v-model="query.fromEnabled" type="checkbox"> From Query Date</label><input v-model="query.fromDate" type="datetime-local" step="1" :disabled="!query.fromEnabled"></div>
        <div class="date-field"><label><input v-model="query.endEnabled" type="checkbox"> To</label><input v-model="query.endDate" type="datetime-local" step="1" :disabled="!query.endEnabled"></div>
      </div>
      <footer class="query-actions"><span>日期預設為最近兩個月，兩端未勾選；查詢不會修改管制圖設定。</span><button class="button primary" type="button" @click="runQuery">Query</button></footer>
    </section>

    <section v-if="searched" class="results-panel panel">
      <header class="results-head"><div><h2>Control Chart Line History</h2><span>{{ filteredRows.length }} 筆記錄</span></div><span>唯讀</span></header>
      <div class="table-scroll"><table class="history-table"><thead><tr>
        <th>Data Type</th><th>UserID</th><th>RecordDate</th><th>Product</th><th>Customer</th><th>Material</th><th>Vendor</th>
        <th v-for="factor in onlineFactorFields" :key="factor">{{ factor }}</th>
        <th>QCItemNo</th><th>QCItemName</th><th>Control Chart</th><th>US</th><th>CS</th><th>LS</th><th>UCL</th><th>CL</th><th>LCL</th><th>Memo</th>
      </tr></thead><tbody>
        <tr v-for="row in filteredRows" :key="row.id" :class="['data-row', row.changeType === '新增' ? 'new-row' : row.changeType === '規格修改' ? 'spec-row' : 'line-row']">
          <td><span class="change-pill">{{ row.changeType }}</span></td><td>{{ row.userId }}</td><td>{{ formatDate(row.recordDate) }}</td><td>{{ row.productName }}</td><td>{{ row.customerName }}</td><td>{{ row.materialName }}</td><td>{{ row.vendorName }}</td>
          <td v-for="factor in onlineFactorFields" :key="factor">{{ displayValue(row.factorValues[factor]) }}</td>
          <td>{{ row.qcItemNo }}</td><td>{{ row.qcItemName }}</td><td>{{ row.controlChart }}</td>
          <td>{{ displayValue(row.values.US) }}</td><td>{{ displayValue(row.values.CS) }}</td><td>{{ displayValue(row.values.LS) }}</td><td>{{ displayValue(row.values.UCL) }}</td><td>{{ displayValue(row.values.CL) }}</td><td>{{ displayValue(row.values.LCL) }}</td><td class="memo-cell">{{ displayValue(row.values.MEMO) }}</td>
        </tr>
        <tr v-if="filteredRows.length === 0"><td :colspan="17 + onlineFactorFields.length" class="empty">沒有符合條件的 DEV 管制線歷程</td></tr>
      </tbody></table></div>
      <footer class="grid-foot">FactorSerial 在原 Grid 中隱藏；查詢由管制圖 Basis 與 DetailLog 歷程重建。此處 Factor 值、資料與分類皆為 DEV 假資料。</footer>
    </section>
    <p class="notice" :class="noticeKind" role="status">{{ notice }}</p>
  </section>
  <section v-else class="pending">DEV 預覽已關閉。<button class="button" type="button" @click="emit('close')">Close</button></section>
</template>

<style scoped>
.line-edit-page{height:100%;min-height:0;overflow:auto;box-sizing:border-box;padding:14px;background:#f4f3ef;color:#292821;font:13px/1.45 "Segoe UI",Arial,sans-serif}.page-head{display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:12px}.eyebrow{margin:0;color:#9a6b00;font-size:11px;font-weight:700;letter-spacing:.08em}.page-head h1{margin:2px 0;font-size:22px}.subtitle{margin:0;color:#777366}.button{min-height:30px;padding:5px 11px;border:1px solid #aaa79a;border-radius:3px;background:#fff;color:#292821;font:inherit;cursor:pointer}.button:hover{background:#f9edbd;border-color:#c8a300}.button.primary{background:#e5c400;border-color:#c6a800;font-weight:700}.panel{margin-bottom:10px;border:1px solid #d4d1c7;border-radius:4px;background:#fff;overflow:hidden}.query-panel{padding:10px}.query-caption{display:flex;align-items:center;gap:10px;margin-bottom:10px}.query-caption>strong{color:#625f55}.type-chip{padding:2px 8px;border:1px solid #d7d1b7;border-radius:10px;background:#f7f5ea;font-size:11px}.source-note{color:#777366;font-size:10px}.dev-badge{margin-left:auto;color:#856b12;font-size:10px}.query-grid{display:grid;grid-template-columns:repeat(5,minmax(150px,1fr));gap:9px 12px;align-items:end}.lookup-field,.date-field{display:flex;min-width:0;flex-direction:column;gap:5px;color:#625f55;font-size:11px}.lookup-field>label,.date-field>label{display:flex;align-items:center;gap:5px;min-height:18px}.lookup-field select,.date-field input[type=datetime-local]{width:100%;min-height:31px;box-sizing:border-box;border:1px solid #c8c5bb;border-radius:3px;padding:4px 6px;background:#fff;color:#292821;font:inherit}.date-field input:disabled{background:#efeee9;color:#999}.query-actions{display:flex;justify-content:space-between;align-items:center;gap:8px;margin-top:11px;padding-top:9px;border-top:1px solid #e9e6dc}.query-actions span{color:#777366;font-size:10px}.results-head{display:flex;justify-content:space-between;align-items:center;padding:8px 10px;border-bottom:1px solid #e4e1d8;background:#faf9f6}.results-head div{display:flex;align-items:center;gap:10px}.results-head h2{margin:0;font-size:14px}.results-head span{color:#777366;font-size:11px}.table-scroll{max-height:560px;overflow:auto}.history-table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:11px}.history-table th,.history-table td{padding:7px 8px;border-bottom:1px solid #eceae4;text-align:left;vertical-align:top}.history-table th{position:sticky;top:0;background:#f3f1ea;color:#5e5b50;font-weight:600}.history-table td{max-width:180px;overflow:hidden;text-overflow:ellipsis}.history-table .memo-cell{min-width:180px;white-space:normal}.data-row{background:#fff}.new-row{background:#f5fbf4}.spec-row{background:#fffdf1}.line-row{background:#fff8f5}.change-pill{display:inline-block;padding:2px 6px;border:1px solid #ddd6bd;border-radius:3px;background:#fff;font-weight:600}.empty{text-align:center!important;color:#888!important;padding:18px!important}.grid-foot{padding:7px 10px;border-top:1px solid #eceae4;color:#777366;font-size:10px}.notice{position:sticky;bottom:0;margin:0;padding:8px 10px;border:1px solid #e3d9a8;border-radius:3px;background:#fff8d8;color:#63551b}.notice.success{border-color:#acd0ae;background:#eff8ef;color:#346238}.notice.error{border-color:#e0b2aa;background:#fff0ed;color:#922f25}.pending{padding:18px;color:#777366}.pending .button{margin-left:10px}
@media(max-width:1100px){.query-grid{grid-template-columns:repeat(2,minmax(180px,1fr))}.query-caption{flex-wrap:wrap}.dev-badge{margin-left:0}}
</style>
