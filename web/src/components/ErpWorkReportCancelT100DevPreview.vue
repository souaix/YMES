<script setup lang="ts">
import { computed, ref } from 'vue'

type SummaryRow = {
  workReportId: string
  dataStartTime: Date
  dataEndTime: Date
  erpFlag: number
  autoRunJobExists: boolean
}
type DetailRow = {
  workReportId: string
  itemNo: number
  equipmentNo: string
  moNo: string
  eqpResValue: number
  empResValue: number
  erpNo: string
  cancelFails: boolean
}

const emit = defineEmits<{ close: [] }>()
const now = new Date()
const startDefault = new Date(now)
startDefault.setDate(startDefault.getDate() - 1)
startDefault.setHours(0, 0, 0, 0)
const endDefault = new Date(now)
endDefault.setHours(0, 0, 0, 0)

const useMoNo = ref(false)
const moNo = ref('')
const useDateRange = ref(false)
const startTime = ref(toDateTimeLocal(startDefault))
const endTime = ref(toDateTimeLocal(endDefault))
const summaries = ref<SummaryRow[]>([
  { workReportId: 'WR-T100-001', dataStartTime: hoursAgo(20), dataEndTime: hoursAgo(19.7), erpFlag: 1, autoRunJobExists: true },
  { workReportId: 'WR-T100-002', dataStartTime: hoursAgo(56), dataEndTime: hoursAgo(55.7), erpFlag: -1, autoRunJobExists: false },
  { workReportId: 'WR-T100-003', dataStartTime: hoursAgo(5), dataEndTime: hoursAgo(4.7), erpFlag: 2, autoRunJobExists: true },
])
const details: DetailRow[] = [
  { workReportId: 'WR-T100-001', itemNo: 1, equipmentNo: 'EQP-LAM-02', moNo: 'MO-T100-001', eqpResValue: 0.6, empResValue: 1, erpNo: 'T100-WR-88021', cancelFails: false },
  { workReportId: 'WR-T100-001', itemNo: 2, equipmentNo: 'EQP-TEST-06', moNo: 'MO-T100-001', eqpResValue: 0.3, empResValue: 0.5, erpNo: 'T100-WR-88022', cancelFails: true },
  { workReportId: 'WR-T100-001', itemNo: 3, equipmentNo: 'EQP-LAM-02', moNo: 'MO-T100-002', eqpResValue: 0.2, empResValue: 0.25, erpNo: 'T100-WR-88022', cancelFails: true },
  { workReportId: 'WR-T100-002', itemNo: 1, equipmentNo: 'EQP-OVEN-01', moNo: 'MO-T100-003', eqpResValue: 1.2, empResValue: 1.5, erpNo: 'T100-WR-87914', cancelFails: false },
  { workReportId: 'WR-T100-003', itemNo: 1, equipmentNo: 'EQP-LAM-05', moNo: 'MO-T100-001', eqpResValue: 0.8, empResValue: 1, erpNo: 'T100-WR-88042', cancelFails: false },
]

const selectedWorkReportId = ref('')
const detailMoFilter = ref('')
const hasSearched = ref(false)
const loading = ref(false)
const error = ref('')
const actionMessage = ref('')

const visibleSummaries = computed(() => {
  if (!hasSearched.value) return []
  const moFilter = useMoNo.value ? moNo.value.trim().toUpperCase() : ''
  const from = useDateRange.value ? new Date(startTime.value) : null
  const to = useDateRange.value ? new Date(endTime.value) : null
  return summaries.value.filter(row => {
    const matchesMo = !moFilter || details.some(detail => detail.workReportId === row.workReportId && detail.moNo === moFilter)
    const matchesDate = !from || !to || (row.dataStartTime >= from && row.dataStartTime <= to)
    return matchesMo && matchesDate
  })
})
const selectedSummary = computed(() => summaries.value.find(row => row.workReportId === selectedWorkReportId.value) ?? null)
const visibleDetails = computed(() => {
  if (!selectedWorkReportId.value) return []
  const moFilter = detailMoFilter.value
  return details.filter(row => row.workReportId === selectedWorkReportId.value && (!moFilter || row.moNo === moFilter))
})

function hoursAgo(hours: number): Date { return new Date(Date.now() - hours * 60 * 60 * 1000) }
function toDateTimeLocal(value: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0')
  return value.getFullYear() + '-' + pad(value.getMonth() + 1) + '-' + pad(value.getDate()) + 'T' + pad(value.getHours()) + ':' + pad(value.getMinutes())
}
function formatDate(value: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0')
  return value.getFullYear() + '/' + pad(value.getMonth() + 1) + '/' + pad(value.getDate()) + ' ' + pad(value.getHours()) + ':' + pad(value.getMinutes()) + ':' + pad(value.getSeconds())
}
function formatNumber(value: number): string { return value.toLocaleString('zh-TW', { maximumFractionDigits: 4 }) }
function setMoNo(value: string): void {
  moNo.value = value
  useMoNo.value = value.trim() !== ''
}
function onMoInput(event: Event): void {
  if (event.target instanceof HTMLInputElement) setMoNo(event.target.value)
}
function toggleMoNo(): void {
  if (!useMoNo.value) moNo.value = ''
}
function selectSummary(row: SummaryRow): void {
  selectedWorkReportId.value = row.workReportId
  detailMoFilter.value = useMoNo.value ? moNo.value.trim().toUpperCase() : ''
  error.value = ''
  actionMessage.value = ''
}

async function query(): Promise<void> {
  error.value = ''
  actionMessage.value = ''
  if (!useMoNo.value && !useDateRange.value) {
    hasSearched.value = false
    selectedWorkReportId.value = ''
    error.value = '請至少勾選一項查詢條件：MO No 或 DataStartTime。'
    return
  }
  if (useMoNo.value && !moNo.value.trim()) {
    error.value = '已勾選 MO No，請輸入工單編號。'
    return
  }
  if (useDateRange.value) {
    if (!startTime.value || !endTime.value) { error.value = '請輸入 DataStartTime 起訖時間。'; return }
    if (new Date(endTime.value).getTime() - new Date(startTime.value).getTime() <= 0) {
      error.value = '起始時間必須早於結束時間。'
      return
    }
  }
  loading.value = true
  hasSearched.value = true
  selectedWorkReportId.value = ''
  await new Promise<void>(resolve => window.setTimeout(resolve, 120))
  loading.value = false
}

function cancelWorkReport(): void {
  const target = selectedSummary.value
  if (!target) { error.value = '請先選取一筆報工單。'; return }
  if (target.erpFlag !== 1) { error.value = '只有 ERPFlag = 1 的報工單可取消。'; return }
  const rows = visibleDetails.value
  const failedRows = rows.filter(row => row.cancelFails)
  const firstErpNo = [...new Set(details.filter(row => row.workReportId === target.workReportId).map(row => row.erpNo))][0] ?? ''
  target.erpFlag = -1
  error.value = ''
  actionMessage.value = failedRows.length
    ? 'DEV 模擬對目前明細逐筆呼叫 T100 取消；' + [...new Set(failedRows.map(row => row.erpNo))].join('、') + ' 失敗仍繼續 MES undo。Core 以假資料演示清除第一個分組 ERPNo：' + firstErpNo + '；來源 GROUP BY 沒有排序，實際所選單號不保證，請核對 ERP／MES 差異。'
    : 'DEV 模擬 T100 取消與 MES 更新完成；Core 以假資料演示清除第一個分組 ERPNo：' + firstErpNo + '（來源 GROUP BY 沒有排序）。未呼叫任何 ERP、API 或資料庫。'
}

function rerunWorkReport(): void {
  const target = selectedSummary.value
  if (!target) { error.value = '請先選取一筆報工單。'; return }
  if (target.erpFlag !== -1) { error.value = '只有 ERPFlag = -1 的報工單可重新彙整／重拋。'; return }
  target.erpFlag = 2
  error.value = ''
  actionMessage.value = target.autoRunJobExists
    ? 'DEV 模擬建立一筆 funCreateWOWorkReportData_EAI Standby 佇列，Summary ERPFlag 設為 2。'
    : 'DEV 模擬找不到 AutoRun Job；legacy Core 仍把 Summary ERPFlag 設為 2，但不新增佇列。'
}
</script>

<template>
  <main class="t100-page" aria-label="取消報工單 T100 DEV 預覽">
    <header class="page-heading">
      <div><span class="eyebrow">RPT / RT610</span><h1>取消報工單_T100</h1><p>依 MO 或報工彙整時間查詢；選列檢視 EAI 明細。</p></div>
      <span class="dev-badge">DEV 假資料</span>
    </header>

    <form class="query-panel" @submit.prevent="query">
      <label class="check-label"><input v-model="useMoNo" type="checkbox" @change="toggleMoNo"> MO No</label>
      <input class="mo-input" :value="moNo" maxlength="50" autocomplete="off" placeholder="MO-T100-001" @input="onMoInput" @blur="moNo = moNo.trim().toUpperCase()">
      <label class="check-label"><input v-model="useDateRange" type="checkbox"> DataStartTime</label>
      <label class="date-label">From <input v-model="startTime" type="datetime-local"></label>
      <label class="date-label">To <input v-model="endTime" type="datetime-local"></label>
      <button class="primary" type="submit" :disabled="loading">{{ loading ? '查詢中…' : 'Query' }}</button>
      <p v-if="error" class="error" role="alert">{{ error }}</p>
    </form>

    <div class="results">
      <div class="grid-stack">
        <section class="grid-panel summary-panel" aria-label="ERPWorkRPT Summary T100">
          <header><strong>ERPWorkRPT Summary</strong><span>{{ visibleSummaries.length }} rows</span></header>
          <div class="table-scroll"><table><thead><tr><th>ItemNo</th><th>DataStartTime</th><th>DataEndTime</th><th>ERPFlag</th></tr></thead>
            <tbody v-if="visibleSummaries.length"><tr v-for="(row, index) in visibleSummaries" :key="row.workReportId" :class="{ selected: selectedWorkReportId === row.workReportId }" @click="selectSummary(row)">
              <td class="numeric">{{ index + 1 }}</td><td>{{ formatDate(row.dataStartTime) }}</td><td>{{ formatDate(row.dataEndTime) }}</td><td><span class="flag">{{ row.erpFlag }}</span></td>
            </tr></tbody>
            <tbody v-else><tr><td colspan="4" class="empty-state">{{ loading ? '載入中…' : hasSearched ? '查無報工單。' : '請至少設定一項條件後查詢。' }}</td></tr></tbody>
          </table></div>
        </section>
        <section class="grid-panel detail-panel" aria-label="ERPWorkRPT Detail EAI">
          <header><strong>EAI Report Detail</strong><span>{{ visibleDetails.length }} rows</span></header>
          <div class="table-scroll"><table><thead><tr><th>ItemNo</th><th>EquipmentNo</th><th>MONo</th><th class="numeric">EQP_RESValue</th><th class="numeric">EMP_RESValue</th><th>ERPNo</th></tr></thead>
            <tbody v-if="visibleDetails.length"><tr v-for="row in visibleDetails" :key="row.itemNo">
              <td class="numeric">{{ row.itemNo }}</td><td>{{ row.equipmentNo }}</td><td>{{ row.moNo }}</td><td class="numeric">{{ formatNumber(row.eqpResValue) }}</td><td class="numeric">{{ formatNumber(row.empResValue) }}</td><td>{{ row.erpNo }}</td>
            </tr></tbody>
            <tbody v-else><tr><td colspan="6" class="empty-state">先選取一筆報工單。</td></tr></tbody>
          </table></div>
        </section>
      </div>
      <aside class="actions" aria-label="T100 報工單操作">
        <button type="button" @click="cancelWorkReport">Cancel WorkRPT</button>
        <button type="button" @click="rerunWorkReport">ReSum ReTurn</button>
        <span class="action-spacer"></span>
        <button class="close-button" type="button" @click="emit('close')">Close (X)</button>
      </aside>
    </div>
    <p v-if="actionMessage" class="action-message" role="status">{{ actionMessage }}</p>
    <footer class="page-footer">固定虛構資料；操作只改本機記憶體。此預覽不會呼叫 T100 ERP、MES API、Oracle 或 PostgreSQL。</footer>
  </main>
</template>

<style scoped>
.t100-page{box-sizing:border-box;display:flex;height:100%;min-height:0;flex-direction:column;gap:8px;overflow:hidden;padding:10px;background:#f3f2ed;color:#292820;font:inherit}.page-heading{display:flex;align-items:flex-start;justify-content:space-between;gap:12px}.eyebrow{color:#777366;font-size:9px;letter-spacing:.1em}.page-heading h1{margin:2px 0;font-size:17px}.page-heading p{margin:0;color:#777366;font-size:10px}.dev-badge{padding:4px 8px;border:1px solid #d6c995;border-radius:12px;background:#f4efdc;color:#705c17;font-size:10px;font-weight:700}.query-panel{display:flex;align-items:center;gap:8px;flex-wrap:wrap;padding:8px 10px;border:1px solid #d4d1c5;background:#fffefa}.check-label,.date-label{display:flex;align-items:center;gap:4px;font-size:10px;white-space:nowrap}.mo-input{box-sizing:border-box;width:150px;height:27px;padding:4px 7px;border:1px solid #c9c6bb;border-radius:2px;font:inherit;font-size:10px}.date-label input{height:27px;max-width:175px;padding:3px 5px;border:1px solid #c9c6bb;border-radius:2px;font:inherit;font-size:10px}.query-panel>button{min-height:27px;margin-left:auto;padding:4px 11px;border:1px solid #c9c6bb;border-radius:2px;background:#f3f2ed;color:#343329;font:inherit;font-size:10px;cursor:pointer}.query-panel>button.primary{border-color:#364d46;background:#364d46;color:#fff}.query-panel>button:disabled{opacity:.6;cursor:wait}.error{flex-basis:100%;margin:0;color:#a32121;font-size:10px}.results{display:grid;min-height:0;flex:1;grid-template-columns:minmax(0,1fr) 90px;gap:8px}.grid-stack{display:grid;min-height:0;grid-template-rows:minmax(0,1fr) minmax(0,1.2fr);gap:7px}.grid-panel{display:flex;min-height:0;flex-direction:column;overflow:hidden;border:1px solid #d4d1c5;background:#fffefa}.grid-panel>header{display:flex;align-items:center;justify-content:space-between;padding:5px 8px;border-bottom:1px solid #e6e4dc;font-size:10px}.grid-panel>header span{color:#777366;font-size:9px}.table-scroll{min-height:0;flex:1;overflow:auto}table{width:100%;border-collapse:collapse;font-size:10px;white-space:nowrap}th{position:sticky;top:0;z-index:1;background:#ecebe5;text-align:left}th,td{padding:4px 6px;border-bottom:1px solid #e6e4dc}.summary-panel tbody tr{cursor:pointer}tbody tr.selected{background:#e7eee9}tbody tr:hover{background:#f3f5ef}.numeric{text-align:right;font-variant-numeric:tabular-nums}.flag{display:inline-block;min-width:24px;padding:1px 5px;border:1px solid #dedbd0;background:#f8f7f2;text-align:center;font-variant-numeric:tabular-nums}.empty-state{padding:12px;text-align:center;color:#777366}.actions{display:flex;min-height:0;flex-direction:column;gap:6px;padding-top:28px}.actions button{min-height:34px;padding:4px 7px;border:1px solid #c9c6bb;border-radius:2px;background:#f8f7f2;color:#343329;font:inherit;font-size:9px;cursor:pointer}.action-spacer{flex:1}.actions .close-button{margin-bottom:4px}.action-message{margin:0;padding:6px 8px;border:1px solid #d6c995;background:#fbf7e8;color:#594c22;font-size:10px}.page-footer{color:#777366;font-size:9px}@media(max-width:820px){.query-panel{align-items:flex-start}.date-label{flex-wrap:wrap}.query-panel>button{margin-left:0}.results{grid-template-columns:minmax(0,1fr)}.grid-stack{grid-template-rows:repeat(2,minmax(150px,1fr))}.actions{flex-direction:row;padding-top:0}.actions .action-spacer{display:none}.actions .close-button{margin-left:auto}}
</style>
