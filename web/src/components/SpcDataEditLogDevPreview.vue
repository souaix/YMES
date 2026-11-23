<script setup lang="ts">
import { computed, ref } from 'vue'

type QcType = 'attribute' | 'variable'
interface QcItem { no: string; name: string; issueState: 2 | -1 }
interface EditLog {
  id: string; type: QcType; userId: string; recordDate: string; spcSerial: string; qcItemNo: string; qcItemName: string
  lotSerial: string; originalValue: string; newValue: string; description: string; logGroupSerial: string
}
interface QuerySnapshot { type: QcType; qcItemNo: string; editor: string; fromDate: string; endDate: string; fromEnabled: boolean; endEnabled: boolean }

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const qcItems: Record<QcType, QcItem[]> = {
  variable: [
    { no: 'VAR-DEV-101', name: '軸心直徑', issueState: 2 },
    { no: 'VAR-DEV-102', name: '壓合高度', issueState: 2 },
    { no: 'VAR-DEV-109', name: '舊型端面寬度', issueState: -1 },
  ],
  attribute: [
    { no: 'ATT-DEV-201', name: '外觀抽驗', issueState: 2 },
    { no: 'ATT-DEV-202', name: '端子缺陷抽驗', issueState: 2 },
    { no: 'ATT-DEV-209', name: '停用包裝檢查', issueState: -1 },
  ],
}
const logs = ref<EditLog[]>([
  { id: 'att-1', type: 'attribute', userId: 'DEV-QC-01', recordDate: '2026-11-23 08:40:12', spcSerial: 'DEV-SPC-A-8101', qcItemNo: 'ATT-DEV-201', qcItemName: '外觀抽驗', lotSerial: 'DEV-LOT-S-0101', originalValue: 'GoodQty=74; DefectQty=4', newValue: 'GoodQty=75; DefectQty=3', description: '複核缺陷數量', logGroupSerial: 'DEV-EDIT-GRP-01' },
  { id: 'var-1', type: 'variable', userId: 'DEV-QC-02', recordDate: '2026-11-22 16:15:08', spcSerial: 'DEV-SPC-V-5201', qcItemNo: 'VAR-DEV-101', qcItemName: '軸心直徑', lotSerial: 'DEV-LOT-S-0201', originalValue: 'SerialNo=4; TestValue=10.35', newValue: 'SerialNo=4; TestValue=10.15', description: '量測值輸入修正', logGroupSerial: 'DEV-EDIT-GRP-02' },
  { id: 'att-2', type: 'attribute', userId: 'DEV-QC-03', recordDate: '2026-11-20 11:06:40', spcSerial: 'DEV-SPC-A-8105', qcItemNo: 'ATT-DEV-202', qcItemName: '端子缺陷抽驗', lotSerial: 'DEV-LOT-S-0310', originalValue: 'ErrorNo=D-009; ErrorQty=2', newValue: 'ErrorNo=D-006; ErrorQty=2', description: '異常原因更正', logGroupSerial: 'DEV-EDIT-GRP-03' },
  { id: 'var-2', type: 'variable', userId: 'DEV-QC-01', recordDate: '2026-11-18 09:25:01', spcSerial: 'DEV-SPC-V-5199', qcItemNo: 'VAR-DEV-102', qcItemName: '壓合高度', lotSerial: 'DEV-LOT-S-0178', originalValue: 'SerialNo=2; TestValue=2.14', newValue: 'SerialNo=2; TestValue=2.04', description: '誤植小數位修正', logGroupSerial: 'DEV-EDIT-GRP-04' },
  { id: 'att-3', type: 'attribute', userId: 'DEV-QC-02', recordDate: '2026-11-11 14:02:19', spcSerial: 'DEV-SPC-A-8021', qcItemNo: 'ATT-DEV-201', qcItemName: '外觀抽驗', lotSerial: 'DEV-LOT-S-0087', originalValue: 'ScrapQty=1; GoodQty=59', newValue: 'ScrapQty=0; GoodQty=60', description: '重新檢視抽樣結果', logGroupSerial: 'DEV-EDIT-GRP-05' },
  { id: 'var-3', type: 'variable', userId: 'DEV-QC-04', recordDate: '2026-10-30 17:41:55', spcSerial: 'DEV-SPC-V-5109', qcItemNo: 'VAR-DEV-101', qcItemName: '軸心直徑', lotSerial: 'DEV-LOT-S-0032', originalValue: '—', newValue: '—', description: '批次量測資料刪除', logGroupSerial: 'DEV-EDIT-GRP-06' },
  { id: 'var-4', type: 'variable', userId: 'DEV-QC-02', recordDate: '2026-09-28 10:13:42', spcSerial: 'DEV-SPC-V-4911', qcItemNo: 'VAR-DEV-109', qcItemName: '舊型端面寬度', lotSerial: 'DEV-LOT-S-0009', originalValue: 'SerialNo=1; TestValue=6.03', newValue: 'SerialNo=1; TestValue=6.01', description: '歷史資料校正', logGroupSerial: '' },
  { id: 'att-4', type: 'attribute', userId: 'DEV-QC-03', recordDate: '2026-09-25 07:55:09', spcSerial: 'DEV-SPC-A-7880', qcItemNo: 'ATT-DEV-209', qcItemName: '停用包裝檢查', lotSerial: 'DEV-LOT-S-0002', originalValue: 'ErrorQty=3', newValue: 'ErrorQty=1', description: '舊批資料校正', logGroupSerial: '' },
])

const queryType = ref<QcType | ''>('')
const qcItemNo = ref('')
const editor = ref('')
const fromEnabled = ref(false)
const endEnabled = ref(false)
const fromDate = ref('2026-09-24T00:00')
const endDate = ref('2026-11-23T23:59')
const searched = ref(false)
const appliedQuery = ref<QuerySnapshot | null>(null)
const selectedLog = ref<EditLog | null>(null)
const notice = ref('選擇 QC Type 與查詢條件後按 Query；此頁只使用 DEV 假資料。')
const noticeKind = ref<'info' | 'success' | 'error'>('info')

const itemOptions = computed(() => queryType.value ? qcItems[queryType.value] : [])
const editorOptions = computed(() => queryType.value ? [...new Set(logs.value.filter((log) => log.type === queryType.value).map((log) => log.userId))].sort() : [])
const filteredLogs = computed(() => {
  const query = appliedQuery.value
  if (!query) return []
  return logs.value.filter((log) => {
    if (log.type !== query.type) return false
    if (query.qcItemNo && log.qcItemNo !== query.qcItemNo) return false
    if (query.editor && log.userId !== query.editor) return false
    if (query.fromEnabled && log.recordDate.replace(' ', 'T') < query.fromDate) return false
    if (query.endEnabled && log.recordDate.replace(' ', 'T') > query.endDate) return false
    return true
  }).sort((left, right) => left.spcSerial.localeCompare(right.spcSerial) || left.recordDate.localeCompare(right.recordDate))
})

function setNotice(message: string, kind: 'info' | 'success' | 'error' = 'info'): void { notice.value = message; noticeKind.value = kind }
function setType(type: QcType): void {
  if (queryType.value === type) return
  queryType.value = type
  qcItemNo.value = ''
  editor.value = ''
  searched.value = false
  appliedQuery.value = null
  selectedLog.value = null
  setNotice('已切換 QC Type，請調整條件後按 Query。')
}
function runQuery(): void {
  if (!queryType.value) return setNotice('請選擇 Attribute 或 Variable。', 'error')
  if (fromEnabled.value && endEnabled.value && fromDate.value > endDate.value) return setNotice('起始日期不能晚於結束日期。', 'error')
  appliedQuery.value = {
    type: queryType.value,
    qcItemNo: qcItemNo.value,
    editor: editor.value,
    fromDate: fromDate.value,
    endDate: endDate.value,
    fromEnabled: fromEnabled.value,
    endEnabled: endEnabled.value,
  }
  searched.value = true
  selectedLog.value = null
  setNotice(`查詢完成：${filteredLogs.value.length} 筆 DEV 修改記錄。`, 'success')
}
function formatDate(value: string): string { return value }
</script>

<template>
  <section v-if="props.developmentPreview" class="edit-log-page" aria-label="SPC 資料修改記錄">
    <header class="page-head"><div><p class="eyebrow">SPC / SC203</p><h1>資料修改記錄</h1><p class="subtitle">查詢計量或計數 SPC 原始資料的編修歷程；結果唯讀。</p></div><button class="button" @click="emit('close')">Close (X)</button></header>

    <section class="query-panel panel">
      <div class="type-tabs"><strong>QC Type</strong><button :class="{ active: queryType === 'attribute' }" @click="setType('attribute')">Attribute</button><button :class="{ active: queryType === 'variable' }" @click="setType('variable')">Variable</button><span>DEV 假資料 · 不呼叫 Service 或資料庫</span></div>
      <div class="query-fields">
        <label>QC Item<select v-model="qcItemNo" :disabled="!queryType"><option value="">全部 QC Item</option><option v-for="item in itemOptions" :key="item.no" :value="item.no">{{ item.no }} — {{ item.name }}（{{ item.issueState === 2 ? 'Issue' : 'Stop' }}）</option></select></label>
        <label>Editor<select v-model="editor" :disabled="!queryType"><option value="">全部 Editor</option><option v-for="userId in editorOptions" :key="userId" :value="userId">{{ userId }}</option></select></label>
        <div class="date-filter"><label><input v-model="fromEnabled" type="checkbox"> From Date</label><input v-model="fromDate" type="datetime-local" :disabled="!fromEnabled"></div>
        <div class="date-filter"><label><input v-model="endEnabled" type="checkbox"> End Date</label><input v-model="endDate" type="datetime-local" :disabled="!endEnabled"></div>
      </div>
      <footer class="query-actions"><span>未指定 QC Item、Editor 或日期時，DEV 會列出所選類型的全部假資料。</span><button class="button primary" @click="runQuery">Query</button></footer>
    </section>

    <section v-if="searched" class="results-panel panel">
      <header class="results-head"><div><h2>{{ appliedQuery?.type === 'attribute' ? 'Attribute' : 'Variable' }} 修改歷程</h2><span>{{ filteredLogs.length }} 筆記錄</span></div></header>
      <div class="table-scroll"><table class="history-table"><thead><tr><th>Editor</th><th>RecordDate</th><th>SPCSerial</th><th>QCItemNo</th><th>LotSerial</th><th>OriginalValue</th><th>NewValue</th><th>Description</th><th>LogGroupSerial</th></tr></thead><tbody>
        <tr v-for="log in filteredLogs" :key="log.id" class="data-row" @click="selectedLog = log"><td>{{ log.userId }}</td><td>{{ formatDate(log.recordDate) }}</td><td>{{ log.spcSerial }}</td><td>{{ log.qcItemNo }} — {{ log.qcItemName }}</td><td>{{ log.lotSerial || '—' }}</td><td>{{ log.originalValue || '—' }}</td><td>{{ log.newValue || '—' }}</td><td>{{ log.description || '—' }}</td><td>{{ log.logGroupSerial || '—' }}</td></tr>
        <tr v-if="!filteredLogs.length"><td colspan="9" class="empty">沒有符合條件的 DEV 修改記錄</td></tr>
      </tbody></table></div>
      <footer class="grid-foot">依 SPCSerial、RecordDate 排序；點選一列檢視完整修改前後值。</footer>
    </section>
    <p class="notice" :class="noticeKind" role="status">{{ notice }}</p>

    <div v-if="selectedLog" class="modal-backdrop" @click.self="selectedLog = null"><section class="record-dialog" role="dialog" aria-modal="true" aria-label="SPC 修改記錄明細">
      <header class="dialog-head"><div><p class="eyebrow">{{ selectedLog.type === 'attribute' ? 'ATTRIBUTE EDIT LOG' : 'VARIABLE EDIT LOG' }}</p><h2>{{ selectedLog.spcSerial }}</h2></div><button class="button" @click="selectedLog = null">Close</button></header>
      <dl class="record-basis"><div><dt>Editor / RecordDate</dt><dd>{{ selectedLog.userId }} / {{ selectedLog.recordDate }}</dd></div><div><dt>QC Item</dt><dd>{{ selectedLog.qcItemNo }} — {{ selectedLog.qcItemName }}</dd></div><div><dt>LotSerial</dt><dd>{{ selectedLog.lotSerial || '—' }}</dd></div><div><dt>LogGroupSerial</dt><dd>{{ selectedLog.logGroupSerial || '—' }}</dd></div><div><dt>Description</dt><dd>{{ selectedLog.description || '—' }}</dd></div></dl>
      <section class="value-diff"><div><h3>OriginalValue</h3><pre>{{ selectedLog.originalValue || '—' }}</pre></div><div><h3>NewValue</h3><pre>{{ selectedLog.newValue || '—' }}</pre></div></section>
      <p class="readonly-note">SC203 是唯讀歷程查詢；此 DEV 明細不提供修改或刪除。</p>
    </section></div>
  </section>
  <section v-else class="pending">DEV 預覽已關閉。<button class="button" @click="emit('close')">Close</button></section>
</template>

<style scoped>
.edit-log-page{height:100%;min-height:0;overflow:auto;box-sizing:border-box;padding:14px;background:#f4f3ef;color:#292821;font:13px/1.45 "Segoe UI",Arial,sans-serif}.page-head{display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:12px}.eyebrow{margin:0;color:#9a6b00;font-size:11px;font-weight:700;letter-spacing:.08em}.page-head h1{margin:2px 0;font-size:22px}.subtitle{margin:0;color:#777366}.button{min-height:30px;padding:5px 11px;border:1px solid #aaa79a;border-radius:3px;background:#fff;color:#292821;font:inherit;cursor:pointer}.button:hover{background:#f9edbd;border-color:#c8a300}.button.primary{background:#e5c400;border-color:#c6a800;font-weight:700}.panel{margin-bottom:10px;border:1px solid #d4d1c7;border-radius:4px;background:#fff;overflow:hidden}.query-panel{padding:10px}.type-tabs{display:flex;align-items:center;gap:6px;margin-bottom:11px}.type-tabs strong{margin-right:4px;color:#625f55}.type-tabs button{padding:6px 12px;border:1px solid #d4d1c7;background:#faf9f6;color:#555246;font:inherit;cursor:pointer}.type-tabs button.active{background:#fff0ad;border-color:#c6a800;color:#292821;font-weight:700}.type-tabs span{margin-left:auto;color:#777366;font-size:11px}.query-fields{display:grid;grid-template-columns:1fr 1fr 1fr 1fr;gap:9px;align-items:end}.query-fields>label,.date-filter{display:flex;flex-direction:column;gap:5px;color:#625f55;font-size:11px}.query-fields select,.query-fields input[type=datetime-local]{min-height:32px;box-sizing:border-box;border:1px solid #c8c5bb;border-radius:3px;padding:5px 7px;background:#fff;color:#292821;font:inherit}.date-filter>label{display:flex;align-items:center;gap:5px}.query-fields input:disabled,.query-fields select:disabled{background:#efeee9;color:#999}.query-actions{display:flex;justify-content:space-between;align-items:center;gap:8px;margin-top:11px;padding-top:9px;border-top:1px solid #e9e6dc}.query-actions span{color:#777366;font-size:10px}.results-head{display:flex;justify-content:space-between;align-items:center;padding:8px 10px;border-bottom:1px solid #e4e1d8;background:#faf9f6}.results-head h2{margin:0;font-size:14px}.results-head span{color:#777366;font-size:11px}.table-scroll{max-height:560px;overflow:auto}.history-table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}.history-table th,.history-table td{padding:7px 8px;border-bottom:1px solid #eceae4;text-align:left;vertical-align:top}.history-table th{position:sticky;top:0;background:#f3f1ea;color:#5e5b50;font-weight:600}.history-table td:nth-child(6),.history-table td:nth-child(7){max-width:330px;white-space:normal;overflow-wrap:anywhere}.data-row{cursor:pointer}.data-row:hover{background:#fff4c2}.empty{text-align:center!important;color:#888!important;padding:18px!important}.grid-foot{padding:7px 10px;border-top:1px solid #eceae4;color:#777366;font-size:10px}.notice{position:sticky;bottom:0;margin:0;padding:8px 10px;border:1px solid #e3d9a8;border-radius:3px;background:#fff8d8;color:#63551b}.notice.success{border-color:#acd0ae;background:#eff8ef;color:#346238}.notice.error{border-color:#e0b2aa;background:#fff0ed;color:#922f25}.modal-backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:15px;background:#20201866}.record-dialog{width:min(900px,97vw);max-height:92vh;overflow:auto;padding:13px;border:1px solid #c5c1b4;border-radius:5px;background:#fff;box-shadow:0 16px 48px #0003}.dialog-head{display:flex;justify-content:space-between;align-items:center;padding-bottom:8px;border-bottom:1px solid #e6e3da}.dialog-head h2{margin:2px 0;font-size:18px}.record-basis{display:grid;grid-template-columns:1fr 1fr;gap:0 15px;margin:8px 0}.record-basis div{display:grid;grid-template-columns:135px minmax(0,1fr);gap:8px;padding:6px 2px;border-bottom:1px solid #f0eee8}.record-basis dt{color:#777366;font-size:10px}.record-basis dd{margin:0;overflow-wrap:anywhere}.value-diff{display:grid;grid-template-columns:1fr 1fr;gap:10px;margin-top:10px}.value-diff>div{min-width:0;padding:9px;border:1px solid #e4e1d8;border-radius:3px;background:#faf9f6}.value-diff h3{margin:0 0 7px;font-size:12px}.value-diff pre{margin:0;white-space:pre-wrap;overflow-wrap:anywhere;font:12px/1.5 Consolas,monospace}.readonly-note{margin:9px 0 0;padding:7px;background:#f7f5ea;color:#777366;font-size:10px}.pending{padding:18px;color:#777366}.pending .button{margin-left:10px}
@media(max-width:1050px){.query-fields{grid-template-columns:1fr 1fr}.type-tabs{flex-wrap:wrap}.type-tabs span{width:100%;margin-left:0}.record-basis,.value-diff{grid-template-columns:1fr}}
</style>
