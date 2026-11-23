<script setup lang="ts">
import { computed, ref } from 'vue'

type FilterKey = 'qcItemNo' | 'productNo' | 'customerNo' | 'materialNo' | 'vendorNo'
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
interface HistoryRow {
  id: string
  factorSerial: string
  spcId: number
  changeType: string
  userId: string
  recordDate: string
  productName: string
  customerName: string
  materialName: string
  vendorName: string
  productNo: string
  customerNo: string
  materialNo: string
  vendorNo: string
  factorValues: Record<string, string>
  qcItemNo: string
  qcItemName: string
  controlChart: string
  values: Record<string, string>
}
interface SearchResponse {
  totalCount: number
  page: number
  pageSize: number
  pageCount: number
  factorFields: string[]
  rows: HistoryRow[]
}

const props = defineProps<{ apiBaseUrl: string }>()
const emit = defineEmits<{ close: [] }>()
const now = new Date()
const dateEnd = new Date(now)
dateEnd.setHours(23, 59, 59, 0)
const dateStart = new Date(now)
dateStart.setDate(dateStart.getDate() - 60)
dateStart.setHours(0, 0, 0, 0)
const localDateTime = (date: Date): string => {
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
}
const query = ref<QueryState>({
  qcItemNo: '', productNo: '', customerNo: '', materialNo: '', vendorNo: '',
  fromDate: localDateTime(dateStart), endDate: localDateTime(dateEnd), fromEnabled: false, endEnabled: false,
})
const appliedQuery = ref<QueryState | null>(null)
const rows = ref<HistoryRow[]>([])
const factorFields = ref<string[]>([])
const page = ref(1)
const pageSize = 50
const pageCount = ref(0)
const totalCount = ref(0)
const searched = ref(false)
const loading = ref(false)
const error = ref('')
const notice = ref('輸入查詢條件後按 Query；結果只讀取 YMES 管制圖與歷程資料。')
const noticeKind = ref<'info' | 'success' | 'error'>('info')
const startItem = computed(() => totalCount.value === 0 ? 0 : (page.value - 1) * pageSize + 1)
const endItem = computed(() => Math.min(page.value * pageSize, totalCount.value))

function clearFilter(key: FilterKey): void { query.value[key] = '' }

function buildUrl(targetPage: number, criteria: QueryState): URL {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  const root = new URL(base, window.location.origin)
  if (root.protocol !== 'https:' && root.hostname !== 'localhost' && root.hostname !== '127.0.0.1')
    throw new Error('API 連線必須使用 HTTPS。')
  root.pathname = `${root.pathname.replace(/\/+$/, '')}/api/spc/chart-line-edit-log`
  root.search = ''
  const params = new URLSearchParams()
  for (const key of ['qcItemNo', 'productNo', 'customerNo', 'materialNo', 'vendorNo'] as const) {
    const value = criteria[key].trim()
    if (value) params.set(key, value)
  }
  if (criteria.fromEnabled) params.set('fromDate', criteria.fromDate)
  if (criteria.endEnabled) params.set('endDate', criteria.endDate)
  params.set('page', String(targetPage))
  params.set('pageSize', String(pageSize))
  root.search = params.toString()
  return root
}

async function runQuery(targetPage = 1): Promise<void> {
  error.value = ''
  noticeKind.value = 'info'
  const criteria = targetPage === 1 ? { ...query.value } : { ...(appliedQuery.value ?? query.value) }
  if (targetPage === 1) {
    if ((criteria.fromEnabled && !criteria.fromDate) || (criteria.endEnabled && !criteria.endDate)) {
      error.value = '已啟用的日期條件需要輸入日期。'
      notice.value = error.value
      noticeKind.value = 'error'
      return
    }
    if (criteria.fromEnabled && criteria.endEnabled && criteria.fromDate >= criteria.endDate) {
      error.value = 'From Date 必須早於 To Date。'
      notice.value = error.value
      noticeKind.value = 'error'
      return
    }
  }
  loading.value = true
  try {
    const response = await fetch(buildUrl(targetPage, criteria), { credentials: 'include', headers: { Accept: 'application/json' } })
    const body = await response.json().catch(() => ({})) as Partial<SearchResponse> & { detail?: string; title?: string; message?: string }
    if (!response.ok) throw new Error(body.detail || body.message || body.title || `查詢失敗（HTTP ${response.status}）。`)
    const result = body as SearchResponse
    rows.value = result.rows ?? []
    factorFields.value = result.factorFields ?? []
    page.value = result.page
    pageCount.value = result.pageCount
    totalCount.value = result.totalCount
    if (targetPage === 1) appliedQuery.value = criteria
    searched.value = true
    notice.value = `查詢完成：${result.totalCount.toLocaleString()} 筆管制線歷程。日期依舊系統規則在歷程重建後排除端點。`
    noticeKind.value = 'success'
  } catch (cause) {
    rows.value = []
    error.value = cause instanceof Error ? cause.message : '查詢失敗，請稍後再試。'
    notice.value = error.value
    noticeKind.value = 'error'
  } finally {
    loading.value = false
  }
}

function formatDate(value: string): string { return value.replace('T', ' ').replace(/\.\d+$/, '') }
function displayValue(value: string | undefined): string { return value || '—' }
function displayMaster(name: string, code: string): string { return !code || code === '*' ? '—' : name || code }
function itemCountText(): string { return `${startItem.value.toLocaleString()}–${endItem.value.toLocaleString()} / ${totalCount.value.toLocaleString()}` }
</script>

<template>
  <section class="line-edit-page" aria-label="SPC 管制線修改紀錄">
    <header class="page-head">
      <div><p class="eyebrow">SPC / SC215</p><h1>管制線修改紀錄</h1><p class="subtitle">從完整管制圖歷程重建新增、規格修改與管制線修改；結果唯讀。</p></div>
      <button class="button" type="button" @click="emit('close')">關閉</button>
    </header>

    <section class="query-panel panel">
      <div class="query-caption"><strong>查詢條件</strong><span class="type-chip">QC Type：Variable</span></div>
      <div class="query-grid">
        <div class="lookup-field"><label>QC Item No.</label><div class="input-wrap"><input v-model="query.qcItemNo" maxlength="25" autocomplete="off" placeholder="全部 QC Item"><button v-if="query.qcItemNo" type="button" aria-label="清除 QC Item" @click="clearFilter('qcItemNo')">×</button></div></div>
        <div class="lookup-field"><label>Product No.</label><div class="input-wrap"><input v-model="query.productNo" maxlength="61" autocomplete="off" placeholder="全部產品"><button v-if="query.productNo" type="button" aria-label="清除產品" @click="clearFilter('productNo')">×</button></div></div>
        <div class="lookup-field"><label>Material No.</label><div class="input-wrap"><input v-model="query.materialNo" maxlength="61" autocomplete="off" placeholder="全部物料"><button v-if="query.materialNo" type="button" aria-label="清除物料" @click="clearFilter('materialNo')">×</button></div></div>
        <div class="lookup-field"><label>Vendor No.</label><div class="input-wrap"><input v-model="query.vendorNo" maxlength="61" autocomplete="off" placeholder="全部供應商"><button v-if="query.vendorNo" type="button" aria-label="清除供應商" @click="clearFilter('vendorNo')">×</button></div></div>
        <div class="lookup-field"><label>Customer No.</label><div class="input-wrap"><input v-model="query.customerNo" maxlength="61" autocomplete="off" placeholder="全部客戶"><button v-if="query.customerNo" type="button" aria-label="清除客戶" @click="clearFilter('customerNo')">×</button></div></div>
        <div class="date-field"><label><input v-model="query.fromEnabled" type="checkbox"> From Query Date</label><input v-model="query.fromDate" type="datetime-local" step="1" :disabled="!query.fromEnabled"></div>
        <div class="date-field"><label><input v-model="query.endEnabled" type="checkbox"> To</label><input v-model="query.endDate" type="datetime-local" step="1" :disabled="!query.endEnabled"></div>
      </div>
      <footer class="query-actions"><span>日期預設最近兩個月但不啟用；日期端點依來源程式排除。</span><button class="button primary" type="button" :disabled="loading" @click="runQuery(1)">{{ loading ? '查詢中…' : 'Query' }}</button></footer>
    </section>

    <section v-if="searched" class="results-panel panel">
      <header class="results-head"><div><h2>Control Chart Line History</h2><span>{{ totalCount.toLocaleString() }} 筆記錄</span></div><span>唯讀</span></header>
      <div class="table-scroll"><table class="history-table"><thead><tr>
        <th>Data Type</th><th>UserID</th><th>RecordDate</th><th>Product</th><th>Customer</th><th>Material</th><th>Vendor</th>
        <th v-for="factor in factorFields" :key="factor">{{ factor }}</th>
        <th>QCItemNo</th><th>QCItemName</th><th>Control Chart</th><th>US</th><th>CS</th><th>LS</th><th>UCL</th><th>CL</th><th>LCL</th><th>Memo</th>
      </tr></thead><tbody>
        <tr v-for="row in rows" :key="row.id" :class="['data-row', row.changeType === '新增' ? 'new-row' : row.changeType === '規格修改' ? 'spec-row' : 'line-row']">
          <td><span class="change-pill">{{ row.changeType }}</span></td><td>{{ row.userId }}</td><td>{{ formatDate(row.recordDate) }}</td>
          <td>{{ displayMaster(row.productName, row.productNo) }}</td><td>{{ displayMaster(row.customerName, row.customerNo) }}</td><td>{{ displayMaster(row.materialName, row.materialNo) }}</td><td>{{ displayMaster(row.vendorName, row.vendorNo) }}</td>
          <td v-for="factor in factorFields" :key="factor">{{ displayValue(row.factorValues[factor]) }}</td>
          <td>{{ row.qcItemNo }}</td><td>{{ row.qcItemName }}</td><td>{{ row.controlChart }}</td>
          <td>{{ displayValue(row.values.US) }}</td><td>{{ displayValue(row.values.CS) }}</td><td>{{ displayValue(row.values.LS) }}</td><td>{{ displayValue(row.values.UCL) }}</td><td>{{ displayValue(row.values.CL) }}</td><td>{{ displayValue(row.values.LCL) }}</td><td class="memo-cell">{{ displayValue(row.values.MEMO) }}</td>
        </tr>
        <tr v-if="rows.length === 0"><td :colspan="17 + factorFields.length" class="empty">沒有符合條件的管制線歷程</td></tr>
      </tbody></table></div>
      <footer class="grid-foot"><span>FactorSerial 在原 Grid 中隱藏；完整歷程重建後才套用日期與分頁。</span><div v-if="pageCount > 1" class="pager"><span>{{ itemCountText() }}</span><button class="button" type="button" :disabled="loading || page <= 1" @click="runQuery(page - 1)">上一頁</button><span>{{ page }} / {{ pageCount }}</span><button class="button" type="button" :disabled="loading || page >= pageCount" @click="runQuery(page + 1)">下一頁</button></div></footer>
    </section>
    <p class="notice" :class="noticeKind" role="status">{{ notice }}</p>
  </section>
</template>

<style scoped>
.line-edit-page{height:100%;min-height:0;overflow:auto;box-sizing:border-box;padding:14px;background:#f4f3ef;color:#292821;font:13px/1.45 "Segoe UI",Arial,sans-serif}.page-head{display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:12px}.eyebrow{margin:0;color:#9a6b00;font-size:11px;font-weight:700;letter-spacing:.08em}.page-head h1{margin:2px 0;font-size:22px}.subtitle{margin:0;color:#777366}.button{min-height:30px;padding:5px 11px;border:1px solid #aaa79a;border-radius:3px;background:#fff;color:#292821;font:inherit;cursor:pointer}.button:hover{background:#f9edbd;border-color:#c8a300}.button:disabled{opacity:.55;cursor:wait}.button.primary{background:#e5c400;border-color:#c6a800;font-weight:700}.panel{margin-bottom:10px;border:1px solid #d4d1c7;border-radius:4px;background:#fff;overflow:hidden}.query-panel{padding:10px}.query-caption{display:flex;align-items:center;gap:10px;margin-bottom:10px}.query-caption>strong{color:#625f55}.type-chip{padding:2px 8px;border:1px solid #d7d1b7;border-radius:10px;background:#f7f5ea;font-size:11px}.query-grid{display:grid;grid-template-columns:repeat(5,minmax(150px,1fr));gap:9px 12px;align-items:end}.lookup-field,.date-field{display:flex;min-width:0;flex-direction:column;gap:5px;color:#625f55;font-size:11px}.lookup-field>label,.date-field>label{display:flex;align-items:center;gap:5px;min-height:18px}.lookup-field input,.date-field input[type=datetime-local]{width:100%;min-height:31px;box-sizing:border-box;border:1px solid #c8c5bb;border-radius:3px;padding:4px 6px;background:#fff;color:#292821;font:inherit}.input-wrap{position:relative}.input-wrap input{padding-right:28px}.input-wrap button{position:absolute;right:4px;top:4px;width:24px;height:24px;border:0;background:transparent;color:#777;font-size:18px;cursor:pointer}.date-field input:disabled{background:#efeee9;color:#999}.query-actions{display:flex;justify-content:space-between;align-items:center;gap:8px;margin-top:11px;padding-top:9px;border-top:1px solid #e9e6dc}.query-actions span{color:#777366;font-size:10px}.results-head{display:flex;justify-content:space-between;align-items:center;padding:8px 10px;border-bottom:1px solid #e4e1d8;background:#faf9f6}.results-head div{display:flex;align-items:center;gap:10px}.results-head h2{margin:0;font-size:14px}.results-head span{color:#777366;font-size:11px}.table-scroll{max-height:560px;overflow:auto}.history-table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:11px}.history-table th,.history-table td{padding:7px 8px;border-bottom:1px solid #eceae4;text-align:left;vertical-align:top}.history-table th{position:sticky;top:0;background:#f3f1ea;color:#5e5b50;font-weight:600}.history-table td{max-width:180px;overflow:hidden;text-overflow:ellipsis}.history-table .memo-cell{min-width:180px;white-space:normal}.data-row{background:#fff}.new-row{background:#f5fbf4}.spec-row{background:#fffdf1}.line-row{background:#fff8f5}.change-pill{display:inline-block;padding:2px 6px;border:1px solid #ddd6bd;border-radius:3px;background:#fff;font-weight:600}.empty{text-align:center!important;color:#888!important;padding:18px!important}.grid-foot{display:flex;justify-content:space-between;align-items:center;gap:12px;padding:7px 10px;border-top:1px solid #eceae4;color:#777366;font-size:10px}.pager{display:flex;align-items:center;gap:7px;white-space:nowrap}.notice{position:sticky;bottom:0;margin:0;padding:8px 10px;border:1px solid #e3d9a8;border-radius:3px;background:#fff8d8;color:#63551b}.notice.success{border-color:#acd0ae;background:#eff8ef;color:#346238}.notice.error{border-color:#e0b2aa;background:#fff0ed;color:#922f25}@media(max-width:1100px){.query-grid{grid-template-columns:repeat(2,minmax(180px,1fr))}.query-caption{flex-wrap:wrap}.grid-foot{align-items:flex-start;flex-direction:column}}@media(max-width:600px){.line-edit-page{padding:9px}.query-grid{grid-template-columns:minmax(0,1fr)}.page-head h1{font-size:19px}.pager{flex-wrap:wrap}}
</style>
