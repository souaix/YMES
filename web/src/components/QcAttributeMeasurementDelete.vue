<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

interface QcItemRow {
  qcItemNo: string; qcItemName: string; lotIsValid: number; planNo: string; ruleNo: string; formName: string
  webUrl: string; errorReportName: string; noticeReportName: string; issueState: number; description: string
  creator: string; createDate: string; sampleSize: number | null; limitLotNum: number; componentCollect: number
  qcClass: string; mClassNo: string; isSampleSizeInteger: number; executionFile: string; dataStamp: number
  reviser: string; reviseDate: string
}
interface MockStore { items: QcItemRow[]; measurementCounts: Record<string, number> }
type ListPayload<T> = T[] | { items?: T[]; rows?: T[]; data?: T[] }

const props = withDefaults(defineProps<{ apiBaseUrl: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const appBasePath = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const mockKey = 'ymes-dev-mock:qc-attribute-measurement-delete:v1'
const rows = ref<QcItemRow[]>([])
const selectedNos = ref<string[]>([])
const activeNo = ref('')
const loading = ref(false)
const deleting = ref(false)
const confirmOpen = ref(false)
const errorMessage = ref('')
const notice = ref('')
const selectedRows = computed(() => rows.value.filter(row => selectedNos.value.includes(row.qcItemNo)))

function seedStore(): MockStore {
  const blank = (qcItemNo: string, qcItemName: string, issueState: number, formName = ''): QcItemRow => ({
    qcItemNo, qcItemName, lotIsValid: 1, planNo: 'DEV-PLAN-01', ruleNo: 'DEV-RULE-01', formName,
    webUrl: '', errorReportName: '', noticeReportName: '', issueState, description: 'DEV 假資料',
    creator: 'DEV001', createDate: '2026-11-17 08:30:00', sampleSize: 5, limitLotNum: 0,
    componentCollect: 0, qcClass: '', mClassNo: '', isSampleSizeInteger: 0, executionFile: '', dataStamp: 1,
    reviser: 'DEV001', reviseDate: '2026-11-17 08:30:00',
  })
  return {
    items: [
      blank('DEV-QCA-001', '外觀刮傷量測項目（DEV）', 0),
      blank('DEV-QCA-002', '尺寸抽樣項目（DEV）', -1),
      blank('DEV-QCA-TAPE', 'Tape 瑕疵量測項目（DEV）', 0, 'frmAtt_OnlineControlMeasureDataForTape'),
    ],
    measurementCounts: { 'DEV-QCA-001': 12, 'DEV-QCA-002': 0, 'DEV-QCA-TAPE': 4 },
  }
}
function readStore(): MockStore {
  try {
    const raw = window.localStorage.getItem(mockKey)
    if (raw) {
      const value = JSON.parse(raw) as MockStore
      if (Array.isArray(value.items) && value.measurementCounts) return value
    }
  } catch { /* initialize isolated DEV data */ }
  const value = seedStore()
  saveStore(value)
  return value
}
function saveStore(value: MockStore): void { window.localStorage.setItem(mockKey, JSON.stringify(value)) }
function listOf<T>(payload: ListPayload<T> | null | undefined): T[] {
  if (Array.isArray(payload)) return payload
  return payload?.items ?? payload?.rows ?? payload?.data ?? []
}
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init, headers })
  if (response.status === 401) { window.location.assign(appBasePath + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; title?: string; message?: string } | null
    throw new Error(problem?.detail || problem?.message || problem?.title || 'API 呼叫失敗（' + response.status + '）。')
  }
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}
function normalizeRow(value: Record<string, unknown>): QcItemRow {
  const text = (camel: string, upper: string): string => String(value[camel] ?? value[upper] ?? '')
  const number = (camel: string, upper: string, fallback = 0): number => Number(value[camel] ?? value[upper] ?? fallback)
  const sampleSize = value.sampleSize ?? value.SAMPLESIZE
  return {
    qcItemNo: text('qcItemNo', 'QCITEMNO'), qcItemName: text('qcItemName', 'QCITEMNAME'),
    lotIsValid: number('lotIsValid', 'LOTISVALID'), planNo: text('planNo', 'PLANNO'), ruleNo: text('ruleNo', 'RULENO'),
    formName: text('formName', 'FORMNAME'), webUrl: text('webUrl', 'WEBURL'),
    errorReportName: text('errorReportName', 'ERRORREPORTNAME'), noticeReportName: text('noticeReportName', 'NOTICEREPORTNAME'),
    issueState: number('issueState', 'ISSUESTATE'), description: text('description', 'DESCRIPTION'),
    creator: text('creator', 'CREATOR'), createDate: text('createDate', 'CREATEDATE'),
    sampleSize: sampleSize == null ? null : Number(sampleSize), limitLotNum: number('limitLotNum', 'LIMITLOTNUM'),
    componentCollect: number('componentCollect', 'COMPONENTCOLLECT'), qcClass: text('qcClass', 'QCCLASS'),
    mClassNo: text('mClassNo', 'MCLASSNO'), isSampleSizeInteger: number('isSampleSizeInteger', 'ISSAMPLESIZEINTEGER'),
    executionFile: text('executionFile', 'EXECUTIONFILE'), dataStamp: number('dataStamp', 'DATASTAMP'),
    reviser: text('reviser', 'REVISER'), reviseDate: text('reviseDate', 'REVISEDATE'),
  }
}
function stateLabel(value: number): string { return value === -1 ? '不使用' : value === 0 ? '未審核' : value === 1 ? '簽核中' : value === 2 ? '已審核' : String(value) }
function flagLabel(value: number): string { return value === 1 ? '是' : value === 0 ? '否' : '' }
function formatDate(value: string): string { return value ? value.replace('T', ' ').slice(0, 19) : '' }

async function loadRows(): Promise<void> {
  loading.value = true
  errorMessage.value = ''
  try {
    if (mock.value) {
      rows.value = readStore().items.filter(row => row.issueState === -1 || row.issueState === 0)
    } else {
      const [unused, pending] = await Promise.all([
        request<ListPayload<Record<string, unknown>>>('api/qc/attribute-qc-items?issueState=-1'),
        request<ListPayload<Record<string, unknown>>>('api/qc/attribute-qc-items?issueState=0'),
      ])
      rows.value = [...listOf(unused), ...listOf(pending)].map(normalizeRow)
    }
    selectedNos.value = []
    activeNo.value = ''
  } catch (error) {
    rows.value = []
    errorMessage.value = error instanceof Error ? error.message : '載入計數品管項目失敗。'
  } finally { loading.value = false }
}
function selectRow(row: QcItemRow, event: MouseEvent): void {
  activeNo.value = row.qcItemNo
  if (event.ctrlKey || event.metaKey) {
    selectedNos.value = selectedNos.value.includes(row.qcItemNo)
      ? selectedNos.value.filter(no => no !== row.qcItemNo)
      : [...selectedNos.value, row.qcItemNo]
  } else if (!selectedNos.value.includes(row.qcItemNo)) selectedNos.value = [row.qcItemNo]
}
function toggleRow(row: QcItemRow, checked: boolean): void {
  activeNo.value = row.qcItemNo
  selectedNos.value = checked ? [...new Set([...selectedNos.value, row.qcItemNo])] : selectedNos.value.filter(no => no !== row.qcItemNo)
}
function askDelete(): void {
  errorMessage.value = ''
  if (!selectedRows.value.length) { errorMessage.value = '請先選取要刪除量測記錄的品管項目。'; return }
  confirmOpen.value = true
}
async function confirmDelete(): Promise<void> {
  const targets = [...selectedRows.value]
  if (!targets.length) { confirmOpen.value = false; return }
  confirmOpen.value = false
  deleting.value = true
  errorMessage.value = ''
  let completed = 0
  try {
    for (const row of targets) {
      if (mock.value) {
        const store = readStore()
        if ((store.measurementCounts[row.qcItemNo] ?? 0) < 1) throw new Error('QC Item ' + row.qcItemNo + '：沒有量測記錄可刪除。')
        store.measurementCounts[row.qcItemNo] = 0
        saveStore(store)
      } else {
        await request('api/qc/attribute-qc-items/' + encodeURIComponent(row.qcItemNo) + '/measurement-log-deletions', {
          method: 'POST', body: JSON.stringify({ issueState: row.issueState }),
        })
      }
      completed++
    }
    notice.value = `已刪除 ${completed} 筆品管項目的量測記錄。品管項目主檔仍保留。`
    await loadRows()
  } catch (error) {
    errorMessage.value = (error instanceof Error ? error.message : '刪除量測記錄失敗。') + ` 已完成 ${completed} 筆。`
    if (completed) await loadRows()
  } finally { deleting.value = false }
}
function closePage(): void { emit('close') }

onMounted(() => { void loadRows() })
</script>

<template>
  <main class="measurement-delete-page">
    <header class="page-heading">
      <div><div class="eyebrow">QC204 · QCITEM_ADELETE</div><h1>刪除計數型品管項目量測記錄</h1></div>
    </header>

    <section class="delete-workspace" aria-label="計數型品管項目量測記錄清單">
      <div class="grid-area">
        <div class="grid-toolbar">
          <span>資料總筆數 {{ rows.length }}</span><span>已選取 {{ selectedRows.length }} 筆</span>
          <span v-if="loading">載入中…</span><span v-else-if="deleting">刪除處理中…</span>
        </div>
        <div class="grid-scroll">
          <table class="win-grid">
            <thead><tr>
              <th class="select-column"></th><th>品管項目編號</th><th>品管項目名稱</th><th>LotIsValid</th><th>PlanNo</th><th>RuleNo</th>
              <th>FormName</th><th>WebURL</th><th>ErrorReportName</th><th>NoticeReportName</th><th>Issue State</th><th>Description</th>
              <th>建立人</th><th>建立日期</th><th>SampleSize</th><th>LimitLotNum</th><th>ComponentCollect</th><th>QCClass</th>
              <th>MClassNo</th><th>IsSampleSizeInteger</th><th>ExecutionFile</th><th>DataStamp</th><th>修改人</th><th>修改日期</th>
            </tr></thead>
            <tbody>
              <tr v-for="row in rows" :key="row.qcItemNo" :class="{ active: row.qcItemNo === activeNo, selected: selectedNos.includes(row.qcItemNo) }" @click="selectRow(row, $event)">
                <td class="select-column"><input type="checkbox" :checked="selectedNos.includes(row.qcItemNo)" :aria-label="'選取 ' + row.qcItemNo" @click.stop @change="toggleRow(row, ($event.target as HTMLInputElement).checked)"></td>
                <td>{{ row.qcItemNo }}</td><td>{{ row.qcItemName }}</td><td>{{ flagLabel(row.lotIsValid) }}</td><td>{{ row.planNo }}</td><td>{{ row.ruleNo }}</td>
                <td>{{ row.formName }}</td><td>{{ row.webUrl }}</td><td>{{ row.errorReportName }}</td><td>{{ row.noticeReportName }}</td>
                <td>{{ stateLabel(row.issueState) }}</td><td>{{ row.description }}</td>
                <td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td>
                <td class="number-cell">{{ row.sampleSize ?? '' }}</td><td class="number-cell">{{ row.limitLotNum }}</td>
                <td>{{ flagLabel(row.componentCollect) }}</td><td>{{ row.qcClass }}</td><td>{{ row.mClassNo }}</td>
                <td>{{ flagLabel(row.isSampleSizeInteger) }}</td><td>{{ row.executionFile }}</td><td class="number-cell">{{ row.dataStamp }}</td>
                <td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td>
              </tr>
              <tr v-if="!rows.length"><td colspan="24" class="empty-cell">{{ loading ? '載入中…' : '沒有 IssueState 為未審核或不使用的計數品管項目' }}</td></tr>
            </tbody>
          </table>
        </div>
        <div v-if="errorMessage" class="feedback error" role="alert">{{ errorMessage }}</div>
        <div v-else-if="notice" class="feedback success" role="status">{{ notice }}</div>
      </div>
      <aside class="side-actions">
        <button class="win-button delete-button" type="button" :disabled="loading || deleting" @click="askDelete">刪除(D)</button>
        <span class="action-note">刪除選取項目的量測記錄</span>
      </aside>
    </section>
    <footer class="window-footer"><span>來源：IssueState -1、0</span><button class="win-button" type="button" @click="closePage">關閉(X)</button></footer>

    <div v-if="confirmOpen" class="modal-backdrop" role="presentation" @click.self="confirmOpen = false">
      <section class="confirm-dialog" role="dialog" aria-modal="true" aria-labelledby="confirm-title">
        <h2 id="confirm-title">確認刪除量測記錄</h2>
        <p>確定刪除選取的 {{ selectedRows.length }} 筆計數品管項目量測資料？</p>
        <p class="confirm-note">此操作會刪除量測記錄，品管項目主檔不會刪除。</p>
        <div class="confirm-actions"><button class="win-button delete-button" type="button" @click="confirmDelete">確定刪除</button><button class="win-button" type="button" autofocus @click="confirmOpen = false">取消</button></div>
      </section>
    </div>
  </main>
</template>

<style scoped>
.measurement-delete-page{height:100%;min-height:520px;display:flex;flex-direction:column;gap:10px;padding:14px;background:#f7f7f7;color:#333;font-family:Arial,"Microsoft JhengHei",sans-serif;box-sizing:border-box}
.page-heading{display:flex;align-items:center;justify-content:space-between;gap:16px;min-height:46px}.page-heading h1{margin:2px 0 0;font-size:21px;font-weight:600}.eyebrow{color:#777;font-size:11px;letter-spacing:.04em}
.delete-workspace{display:grid;grid-template-columns:minmax(0,1fr) 104px;gap:10px;min-height:0;flex:1;border:1px solid #c9c9c9;background:#fff;padding:8px}
.grid-area{min-width:0;min-height:0;display:flex;flex-direction:column}.grid-toolbar{height:28px;flex:none;display:flex;align-items:center;gap:18px;padding:0 4px;color:#666;font-size:12px}.grid-scroll{min-height:0;flex:1;overflow:auto;border:1px solid #c7c7c7}
.win-grid{border-collapse:collapse;width:max-content;min-width:100%;font-size:12px;white-space:nowrap}.win-grid th{position:sticky;top:0;z-index:1;background:#48a0e6;color:#fff;font-weight:600;text-align:center;border:1px solid #d2d2d2;padding:7px 8px}.win-grid td{height:28px;border:1px solid #d2d2d2;padding:2px 8px;background:#fff}.win-grid tbody tr{cursor:pointer}.win-grid tbody tr:hover td{background:#fff5cf}.win-grid tbody tr.selected td{background:#ffeb9b}.win-grid tbody tr.active td{outline:1px solid #d1a63f;outline-offset:-1px}.select-column{width:30px;text-align:center!important}.select-column input{margin:0}.number-cell{text-align:right}.empty-cell{text-align:center!important;color:#888;height:100px!important}
.side-actions{display:flex;flex-direction:column;align-items:stretch;gap:8px}.win-button{min-height:34px;padding:6px 12px;border:1px solid #c8c8c8;border-radius:3px;background:linear-gradient(#fff,#f2f2f2);color:#333;font-size:13px;cursor:pointer}.win-button:hover:not(:disabled){border-color:#aaa;background:#f7f7f7}.win-button:disabled{opacity:.55;cursor:default}.delete-button{background:#f4a128;border-color:#e6951d;color:#fff;font-weight:600}.delete-button:hover:not(:disabled){background:#e9951e}.action-note{color:#888;font-size:11px;line-height:1.45}.window-footer{display:flex;justify-content:space-between;align-items:center;color:#777;font-size:12px}.feedback{flex:none;margin-top:6px;padding:8px 10px;border:1px solid;border-radius:3px;font-size:12px}.feedback.error{border-color:#e3b9b5;background:#fff3f2;color:#9a3028}.feedback.success{border-color:#c9ddbd;background:#f1f8ed;color:#406531}
.modal-backdrop{position:fixed;inset:0;z-index:80;display:grid;place-items:center;background:rgba(25,30,35,.35);padding:20px}.confirm-dialog{width:min(440px,100%);padding:20px;background:#fff;border:1px solid #aaa;box-shadow:0 10px 32px #0003}.confirm-dialog h2{margin:0 0 14px;font-size:18px}.confirm-dialog p{margin:8px 0;font-size:14px;line-height:1.5}.confirm-note{color:#777}.confirm-actions{display:flex;justify-content:flex-end;gap:8px;margin-top:20px}
@media(max-width:740px){.measurement-delete-page{padding:8px}.delete-workspace{grid-template-columns:1fr;grid-template-rows:minmax(320px,1fr) auto}.side-actions{flex-direction:row;align-items:center}.action-note{flex:1}.page-heading h1{font-size:18px}}
</style>
