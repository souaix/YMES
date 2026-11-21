<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type Workorder = {
  moNo: string
  createDate: string
  planFinishDate: string
  roNo: string
  moTypeNo: number
  moTypeDescription: string
  productNo: string
  description: string
}
type LotRow = {
  lotNo: string
  moNo: string
  status: number
  productNo: string
  productName: string
  productVersion: string
  psNo: string
  processNo: string
  opNo: string
  opName: string
  curQty: number
  curUnitNo: string
  sapPcsQty: number
  sapArgQty: number
  sapPrdQty: number
  inspectionNo: string
  cimLotNo: string
  preProductNo: string
}
type ReportRow = LotRow & { inspectionNo: string; cimLotNo: string; preProductNo: string }

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)

const sampleOrders: Workorder[] = [
  { moNo: 'DEV-MO-261121-01', createDate: '2026/11/21 08:10:00', planFinishDate: '2026/11/26', roNo: 'DEV-RO-2611-08', moTypeNo: 1, moTypeDescription: 'DEV 量產工單', productNo: 'DEV-PROD-GLASS-01', description: 'DEV 光學玻璃基板量產工令' },
  { moNo: 'DEV-MO-261120-02', createDate: '2026/11/20 15:40:00', planFinishDate: '2026/11/25', roNo: 'DEV-RO-2611-06', moTypeNo: 2, moTypeDescription: 'DEV 試作工單', productNo: 'DEV-PROD-GLASS-02', description: 'DEV 精密玻璃基板試作工令' },
]
const sampleRows: LotRow[] = [
  { lotNo: 'DEV-LOT-WO-001', moNo: 'DEV-MO-261121-01', status: 1, productNo: 'DEV-PROD-GLASS-01', productName: 'DEV 光學玻璃基板', productVersion: 'A', psNo: 'DEV-PS-CUT', processNo: 'DEV-PROC-GLASS', opNo: 'DEV-OP-CUT-10', opName: '切割 10', curQty: 120, curUnitNo: 'PCS', sapPcsQty: 12, sapArgQty: 4, sapPrdQty: 120, inspectionNo: 'DEV-INSP-001', cimLotNo: 'DEV-CIM-001', preProductNo: '' },
  { lotNo: 'DEV-LOT-WO-002', moNo: 'DEV-MO-261121-01', status: 4, productNo: 'DEV-PROD-GLASS-01', productName: 'DEV 光學玻璃基板', productVersion: 'A', psNo: 'DEV-PS-GRIND', processNo: 'DEV-PROC-GLASS', opNo: 'DEV-OP-GRIND-20', opName: '研磨 20', curQty: 96, curUnitNo: 'PCS', sapPcsQty: 10, sapArgQty: 3, sapPrdQty: 96, inspectionNo: 'DEV-INSP-002', cimLotNo: 'DEV-CIM-002', preProductNo: '' },
  { lotNo: 'DEV-LOT-WO-003', moNo: 'DEV-MO-261121-01', status: 0, productNo: 'DEV-PROD-GLASS-01', productName: 'DEV 光學玻璃基板', productVersion: 'A', psNo: 'DEV-PS-CLEAN', processNo: 'DEV-PROC-GLASS', opNo: 'DEV-OP-CLEAN-30', opName: '清洗 30', curQty: 84, curUnitNo: 'PCS', sapPcsQty: 8, sapArgQty: 2, sapPrdQty: 84, inspectionNo: '', cimLotNo: 'DEV-CIM-003', preProductNo: '' },
  { lotNo: 'DEV-LOT-WO-004', moNo: 'DEV-MO-261120-02', status: 1, productNo: 'DEV-PROD-GLASS-02', productName: 'DEV 精密玻璃基板', productVersion: 'B', psNo: 'DEV-PS-CUT', processNo: 'DEV-PROC-GLASS-B', opNo: 'DEV-OP-CUT-10', opName: '切割 10', curQty: 48, curUnitNo: 'PCS', sapPcsQty: 6, sapArgQty: 2, sapPrdQty: 48, inspectionNo: 'DEV-INSP-004', cimLotNo: 'DEV-CIM-004', preProductNo: '' },
  { lotNo: 'DEV-LOT-WO-005', moNo: 'DEV-MO-261120-02', status: 1, productNo: 'DEV-PROD-GLASS-02', productName: 'DEV 精密玻璃基板', productVersion: 'B', psNo: 'DEV-PS-EDGE', processNo: 'DEV-PROC-GLASS-B', opNo: 'DEV-OP-EDGE-20', opName: '磨邊 20', curQty: 40, curUnitNo: 'PCS', sapPcsQty: 5, sapArgQty: 2, sapPrdQty: 40, inspectionNo: '', cimLotNo: '', preProductNo: '' },
]

const filters = ref({ useMoNo: false, moNo: '', useCreateDate: false, createDateFrom: '', createDateEnd: '' })
const rows = ref<LotRow[]>([])
const selectedLotNos = ref<string[]>([])
const queried = ref(false)
const errorText = ref('')
const statusText = ref('Ready')
const reportOpen = ref(false)
const reportRows = ref<ReportRow[]>([])
const reportOrder = ref<Workorder | null>(null)
const printMessage = ref('')

const selectedRows = computed(() => rows.value.filter(row => selectedLotNos.value.includes(row.lotNo)))
const totalPcsQty = computed(() => reportRows.value.reduce((sum, row) => sum + row.sapPcsQty, 0))
const totalArgQty = computed(() => reportRows.value.reduce((sum, row) => sum + row.sapArgQty, 0))
const totalPrdQty = computed(() => reportRows.value.reduce((sum, row) => sum + row.sapPrdQty, 0))

function dateTimeInput(date: Date): string {
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
}
function parseDate(value: string): number { return value ? new Date(value.replaceAll('/', '-')).getTime() : Number.NaN }
function initializeDateRange(): void {
  const now = new Date()
  const start = new Date(now)
  if (now.getHours() < 7 || (now.getHours() === 7 && now.getMinutes() < 30)) start.setDate(start.getDate() - 1)
  start.setHours(7, 30, 0, 0)
  filters.value.createDateFrom = dateTimeInput(start)
  filters.value.createDateEnd = dateTimeInput(now)
}
function query(): void {
  errorText.value = ''
  reportOpen.value = false
  if (!filters.value.useMoNo && !filters.value.useCreateDate) {
    errorText.value = '查詢至少勾選 MO No 或 MO Create Date。'
    return
  }
  const from = filters.value.useCreateDate ? parseDate(filters.value.createDateFrom) : Number.NaN
  const end = filters.value.useCreateDate ? parseDate(filters.value.createDateEnd) : Number.NaN
  if (filters.value.useCreateDate && (!Number.isFinite(from) || !Number.isFinite(end))) {
    errorText.value = 'MO Create Date 起訖時間不可空白。'
    return
  }
  queried.value = true
  selectedLotNos.value = []
  if (!dev.value) {
    rows.value = []
    statusText.value = 'Production workorder print data is not connected.'
    return
  }
  const moNo = filters.value.moNo.trim().toUpperCase()
  rows.value = sampleRows.filter(row => {
    const order = sampleOrders.find(item => item.moNo === row.moNo)
    return (!filters.value.useMoNo || (moNo !== '' && row.moNo.toUpperCase() === moNo)) &&
      (!filters.value.useCreateDate || (order !== undefined && parseDate(order.createDate.replace(' ', 'T')) >= from && parseDate(order.createDate.replace(' ', 'T')) <= end))
  })
  statusText.value = `Rows: ${rows.value.length}`
}
function selectAll(): void { selectedLotNos.value = rows.value.map(row => row.lotNo) }
function unselectAll(): void { selectedLotNos.value = [] }
function openReport(): void {
  errorText.value = ''
  printMessage.value = ''
  if (selectedRows.value.length === 0) { errorText.value = '請先勾選要列印的批號。'; return }
  const moNos = new Set(selectedRows.value.map(row => row.moNo))
  if (moNos.size !== 1) { errorText.value = '勾選的批號含兩張以上工單，請只選同一張工單。'; return }
  const moNo = selectedRows.value[0].moNo
  reportOrder.value = sampleOrders.find(order => order.moNo === moNo) || null
  reportRows.value = selectedRows.value.map(row => ({ ...row }))
  reportOpen.value = true
}
function simulatePrint(): void {
  printMessage.value = 'DEV 已模擬產生工令單預覽，沒有送出實體列印。'
}
function formatNumber(value: number): string { return value.toLocaleString('zh-TW', { maximumFractionDigits: 2 }) }
function formatDate(value: string): string { return value ? value.replaceAll('-', '/') : '' }

onMounted(initializeDateRange)
</script>

<template>
  <section class="workorder-print-page" aria-label="工令單列印">
    <header class="page-title">
      <div>
        <h1>工令單列印</h1>
        <p>WORKORDER PRINT · 選取同一張工單的生產批，產生 PF 生產工令單預覽</p>
      </div>
      <button class="mes-button" @click="emit('close')">Close (&amp;X)</button>
    </header>

    <div class="query-box">
      <h2>Query</h2>
      <div class="query-grid">
        <label class="check-field"><input v-model="filters.useMoNo" type="checkbox"> MO No</label>
        <input v-model="filters.moNo" class="text-input" aria-label="MO No" placeholder="輸入工單號" @keyup.enter="query">
        <label class="check-field"><input v-model="filters.useCreateDate" type="checkbox"> MO Create Date</label>
        <label class="date-field">From <input v-model="filters.createDateFrom" type="datetime-local" step="1"></label>
        <label class="date-field">To <input v-model="filters.createDateEnd" type="datetime-local" step="1"></label>
        <button class="mes-button primary" @click="query">Query</button>
      </div>
      <div class="query-footnote">MO 清單查找按鈕在原始 Form 中為隱藏；本 DEV 畫面直接輸入 MO No。日期預設為今日 07:30（07:30 前則為前一日）至現在，需勾選後才套用。</div>
    </div>

    <div v-if="errorText" class="message error" role="alert">{{ errorText }}</div>
    <div v-if="queried && !dev" class="message info" role="status">正式工令單查詢尚未連接。DEV 假資料只在開發預覽提供。</div>

    <div class="toolbar">
      <div class="toolbar-left">
        <button class="mes-button" :disabled="rows.length === 0" @click="selectAll">Select All</button>
        <button class="mes-button" :disabled="rows.length === 0" @click="unselectAll">UnSelect All</button>
        <span>已選 {{ selectedLotNos.length }} / {{ rows.length }} 筆</span>
      </div>
      <button class="mes-button primary" :disabled="!dev || rows.length === 0" @click="openReport">Print</button>
    </div>

    <div class="grid-wrap">
      <table class="lot-grid">
        <thead><tr>
          <th class="check-col">Select</th><th>LOTNO</th><th>MONO</th><th>Status</th><th>ProductNo</th><th>ProductName</th><th>Version</th><th>PSNO</th><th>ProcessNo</th><th>OPNO</th><th>OPName</th><th>CURQTY</th><th>CURUNITNO</th><th>SAP_PCSQTY</th><th>SAP_ARGQTY</th><th>SAP_PRDQTY</th>
        </tr></thead>
        <tbody>
          <tr v-for="row in rows" :key="row.lotNo" :class="{ selected: selectedLotNos.includes(row.lotNo) }">
            <td class="check-col"><input v-model="selectedLotNos" type="checkbox" :value="row.lotNo" :aria-label="`選取 ${row.lotNo}`"></td>
            <td>{{ row.lotNo }}</td><td>{{ row.moNo }}</td><td>{{ row.status }}</td><td>{{ row.productNo }}</td><td>{{ row.productName }}</td><td>{{ row.productVersion }}</td><td>{{ row.psNo }}</td><td>{{ row.processNo }}</td><td>{{ row.opNo }}</td><td>{{ row.opName }}</td>
            <td class="number">{{ formatNumber(row.curQty) }}</td><td>{{ row.curUnitNo }}</td><td class="number">{{ formatNumber(row.sapPcsQty) }}</td><td class="number">{{ formatNumber(row.sapArgQty) }}</td><td class="number">{{ formatNumber(row.sapPrdQty) }}</td>
          </tr>
          <tr v-if="rows.length === 0"><td colspan="16" class="empty">{{ queried ? 'No matching workorder lots.' : '請設定查詢條件後按 Query。' }}</td></tr>
        </tbody>
      </table>
    </div>
    <footer class="statusbar"><span>{{ statusText }}</span><span>DEV Mock · Read only</span></footer>

    <div v-if="reportOpen && reportOrder" class="overlay" role="dialog" aria-modal="true" aria-label="工令單預覽">
      <section class="report-dialog">
        <header class="dialog-header"><strong>PF 生產工令單預覽</strong><button class="mes-button" @click="reportOpen = false">Close (&amp;X)</button></header>
        <div class="paper">
          <div class="paper-title">PF 生產工令單 <small>Production Dispatch Sheet</small></div>
          <div class="report-fields">
            <span><b>開工日</b>{{ formatDate(reportOrder.createDate.slice(0, 10)) }}</span>
            <span><b>完工日</b>{{ formatDate(reportOrder.planFinishDate) }}</span>
            <span><b>TH NO</b>{{ reportOrder.roNo }}</span>
            <span><b>TH PN</b>{{ reportOrder.productNo }}</span>
            <span><b>工令單號</b>{{ reportOrder.moNo }}</span>
            <span><b>下線類別</b>{{ filters.moNo.trim() ? reportOrder.moTypeDescription : '' }}</span>
            <span><b>備註</b>{{ reportOrder.description }}</span>
            <span><b>CIM_NO/備註</b>{{ reportRows[0]?.cimLotNo || '' }}</span>
          </div>
          <div class="table-scroll">
            <table class="report-grid">
              <thead><tr><th>批號 / LOT NO.</th><th>大片 / PANELQTY</th><th>排片 / LAYOUTQTY</th><th>數量 / PIECESQTY</th><th>前工程檢驗單號</th><th>前工程 P/N</th><th>CIM_NO/備註</th></tr></thead>
              <tbody><tr v-for="row in reportRows" :key="row.lotNo"><td>{{ row.lotNo }}</td><td class="number">{{ formatNumber(row.sapPcsQty) }}</td><td class="number">{{ formatNumber(row.sapArgQty) }}</td><td class="number">{{ formatNumber(row.sapPrdQty) }}</td><td>{{ row.inspectionNo }}</td><td>{{ row.preProductNo }}</td><td>{{ row.cimLotNo }}</td></tr></tbody>
              <tfoot><tr><th>共 {{ reportRows.length }} 批</th><th class="number">{{ formatNumber(totalPcsQty) }}</th><th class="number">{{ formatNumber(totalArgQty) }}</th><th class="number">{{ formatNumber(totalPrdQty) }}</th><th colspan="3"></th></tr></tfoot>
            </table>
          </div>
          <div class="signatures"><span>線別 / MFG Line：________________</span><span>基板組別 / SUB Group：________________</span><span>派令單號 / Dispatch No.：________________</span></div>
          <p class="preview-note">DEV 示意報表：前工程 P/N 欄在目前 Form 來源中未被查詢／填入，故留白。</p>
        </div>
        <footer class="dialog-footer"><span>{{ printMessage }}</span><div><button class="mes-button" @click="reportOpen = false">Close</button><button class="mes-button primary" @click="simulatePrint">Print (DEV 模擬)</button></div></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.workorder-print-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:10px;padding:12px;box-sizing:border-box;color:#30363b;background:#f3f5f6;font:13px Arial,"Microsoft JhengHei",sans-serif}.page-title{display:flex;align-items:center;justify-content:space-between;gap:12px}.page-title h1{margin:0 0 4px;font-size:20px;font-weight:600}.page-title p{margin:0;color:#69747c;font-size:12px}.mes-button{height:30px;padding:4px 12px;border:1px solid #9ca6ac;border-radius:2px;background:#f7f8f9;color:#30363b;cursor:pointer;white-space:nowrap}.mes-button:hover:not(:disabled){background:#e7edf1}.mes-button:disabled{opacity:.48;cursor:not-allowed}.mes-button.primary{border-color:#35789c;background:#397fa2;color:#fff}.mes-button.primary:hover:not(:disabled){background:#2f6d8b}.query-box{padding:10px;border:1px solid #aeb8bd;background:#fff}.query-box h2{margin:0 0 9px;font-size:14px;font-weight:600}.query-grid{display:grid;grid-template-columns:auto minmax(150px,1.3fr) auto minmax(180px,1fr) minmax(180px,1fr) auto;align-items:center;gap:8px}.check-field{display:flex;align-items:center;gap:5px;white-space:nowrap}.check-field input,.check-col input{accent-color:#367d9e}.text-input,.date-field input{height:30px;padding:4px 7px;border:1px solid #b9c2c7;background:#fff;box-sizing:border-box;color:#30363b}.date-field{display:flex;align-items:center;gap:7px;white-space:nowrap}.date-field input{min-width:0;width:100%}.query-footnote{margin-top:8px;color:#717d84;font-size:11px}.message{padding:7px 10px;border:1px solid}.message.error{border-color:#d38c8c;background:#fff0f0;color:#a33333}.message.info{border-color:#9bbac7;background:#edf7fb;color:#315d6d}.toolbar{display:flex;align-items:center;justify-content:space-between;gap:10px}.toolbar-left{display:flex;align-items:center;gap:7px}.toolbar-left span{margin-left:6px;color:#66747c}.grid-wrap{flex:1;min-height:150px;overflow:auto;border:1px solid #aeb8bd;background:#fff}.lot-grid,.report-grid{width:100%;border-collapse:collapse;white-space:nowrap}.lot-grid th,.report-grid th{position:sticky;top:0;z-index:1;padding:6px 8px;border:1px solid #b8c2c7;background:#dfeaf0;text-align:left;font-weight:600}.lot-grid td,.report-grid td{height:28px;padding:3px 8px;border:1px solid #cbd2d6;background:#fff}.lot-grid tr.selected td{background:#fff0c5}.lot-grid tbody tr:hover td{background:#f2f8fb}.check-col{width:52px;text-align:center}.number{text-align:right}.empty{text-align:center!important;color:#778188;background:#fff!important;height:48px!important}.statusbar{height:24px;flex:0 0 24px;display:flex;align-items:center;justify-content:space-between;padding:0 5px;border-top:1px solid #c7cdd1;color:#68747b}.overlay{position:fixed;inset:0;z-index:1600;display:grid;place-items:center;padding:18px;background:#0006}.report-dialog{width:min(1150px,96vw);height:min(820px,94vh);display:flex;flex-direction:column;gap:9px;padding:12px;background:#f6f7f8;border:1px solid #888;box-shadow:0 8px 26px #0004;box-sizing:border-box}.dialog-header,.dialog-footer{display:flex;align-items:center;justify-content:space-between;gap:10px}.paper{flex:1;overflow:auto;padding:26px;background:white;border:1px solid #aeb4b8;color:#222}.paper-title{margin:0 0 19px;text-align:center;font-size:24px;font-weight:700;letter-spacing:3px}.paper-title small{display:block;margin-top:5px;font-size:12px;font-weight:400;letter-spacing:1px}.report-fields{display:grid;grid-template-columns:1fr 1fr;gap:9px 24px;padding-bottom:15px;border-bottom:1px solid #8c969b}.report-fields span{display:flex;gap:8px}.report-fields b{width:110px;flex:0 0 110px}.table-scroll{overflow:auto}.report-grid{margin-top:15px}.report-grid th{position:static}.report-grid td{height:34px}.report-grid tfoot th{background:#edf1f3}.signatures{display:grid;grid-template-columns:repeat(3,1fr);gap:15px;margin-top:28px;padding-top:12px;border-top:1px solid #aaa}.preview-note{margin:18px 0 0;padding:7px;background:#fff8dd;color:#755d1d}.dialog-footer>div{display:flex;gap:7px}.dialog-footer>span{color:#477043}@media(max-width:1100px){.query-grid{grid-template-columns:auto minmax(140px,1fr) auto minmax(150px,1fr);}.query-grid .mes-button{grid-column:4;justify-self:end}.date-field{min-width:0}.date-field input{width:170px}}@media(max-width:680px){.page-title{align-items:flex-start}.query-grid{grid-template-columns:auto minmax(0,1fr)}.query-grid .check-field{grid-column:1}.query-grid .date-field{grid-column:1 / -1}.query-grid .mes-button{grid-column:2}.toolbar-left{flex-wrap:wrap}.report-fields,.signatures{grid-template-columns:1fr}.paper{padding:14px}}
</style>
