<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

interface VariableQcItemRow {
  qcItemNo: string; qcItemName: string; chartType: number; specLineType: number
  componentCollect: number; lotIsValid: number; planNo: string; chartControl: number
  collectType: number; limitLotNum: number; sampleSize: number | null; unit: string
  qcDataType: number; creator: string; createDate: string; reviser: string; reviseDate: string
  issueState: number; dataStamp: number
}
interface MockStore { items: VariableQcItemRow[]; measurementCounts: Record<string, number> }
type ListPayload<T> = T[] | { items?: T[]; rows?: T[]; data?: T[] }

const props = withDefaults(defineProps<{ apiBaseUrl: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const appBasePath = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const mockKey = 'ymes-dev-mock:qc-variable-measurement-delete:v1'
const rows = ref<VariableQcItemRow[]>([])
const selectedNos = ref<string[]>([])
const activeNo = ref('')
const loading = ref(false)
const deleting = ref(false)
const confirmOpen = ref(false)
const errorMessage = ref('')
const notice = ref('')
const selectedRows = computed(() => rows.value.filter(row => selectedNos.value.includes(row.qcItemNo)))

function seedStore(): MockStore {
  const item = (qcItemNo: string, qcItemName: string, issueState: number, chartType: number, sampleSize: number, planNo: string): VariableQcItemRow => ({
    qcItemNo, qcItemName, chartType, specLineType: 0, componentCollect: 0, lotIsValid: 1,
    planNo, chartControl: 1, collectType: 0, limitLotNum: 0, sampleSize, unit: 'mm',
    qcDataType: 2, creator: 'DEV001', createDate: '2026-11-17 08:30:00', reviser: 'DEV001',
    reviseDate: '2026-11-17 09:10:00', issueState, dataStamp: 1,
  })
  return {
    items: [
      item('DEV-QCV-001', '軸徑尺寸量測項目（DEV）', 0, 0, 5, 'DEV-VAR-PLAN-01'),
      item('DEV-QCV-002', '焊點拉力量測項目（DEV）', -1, 1, 3, 'DEV-VAR-PLAN-02'),
      item('DEV-QCV-003', '平面度量測項目（DEV）', 0, 2, 8, 'DEV-VAR-PLAN-03'),
    ],
    measurementCounts: { 'DEV-QCV-001': 18, 'DEV-QCV-002': 0, 'DEV-QCV-003': 6 },
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
function normalizeRow(value: Record<string, unknown>): VariableQcItemRow {
  const text = (camel: string, upper: string): string => String(value[camel] ?? value[upper] ?? '')
  const number = (camel: string, upper: string, fallback = 0): number => Number(value[camel] ?? value[upper] ?? fallback)
  const sampleSize = value.sampleSize ?? value.SAMPLESIZE
  return {
    qcItemNo: text('qcItemNo', 'QCITEMNO'), qcItemName: text('qcItemName', 'QCITEMNAME'),
    chartType: number('chartType', 'CHARTTYPE'), specLineType: number('specLineType', 'SPECLINETYPE'),
    componentCollect: number('componentCollect', 'COMPONENTCOLLECT'), lotIsValid: number('lotIsValid', 'LOTISVALID'),
    planNo: text('planNo', 'PLANNO'), chartControl: number('chartControl', 'CHARTCONTROL'),
    collectType: number('collectType', 'COLLECTTYPE'), limitLotNum: number('limitLotNum', 'LIMITLOTNUM'),
    sampleSize: sampleSize == null ? null : Number(sampleSize), unit: text('unit', 'UNIT'),
    qcDataType: number('qcDataType', 'QCDATATYPE'), creator: text('creator', 'CREATOR'),
    createDate: text('createDate', 'CREATEDATE'), reviser: text('reviser', 'REVISER'),
    reviseDate: text('reviseDate', 'REVISEDATE'), issueState: number('issueState', 'ISSUESTATE'),
    dataStamp: number('dataStamp', 'DATASTAMP'),
  }
}
function valueLabel(value: number, labels: string[]): string { return labels[value] ?? String(value) }
function yn(value: number): string { return value === 1 ? 'Y' : value === 0 ? 'N' : '' }
function formatDate(value: string): string { return value ? value.replace('T', ' ').slice(0, 19) : '' }

async function loadRows(): Promise<void> {
  loading.value = true
  errorMessage.value = ''
  try {
    if (mock.value) {
      rows.value = readStore().items
        .filter(row => row.issueState === -1 || row.issueState === 0)
        .sort((left, right) => (left.issueState === -1 ? 0 : 1) - (right.issueState === -1 ? 0 : 1))
    } else {
      const [unused, pending] = await Promise.all([
        request<ListPayload<Record<string, unknown>>>('api/qc/variable-qc-items?issueState=-1'),
        request<ListPayload<Record<string, unknown>>>('api/qc/variable-qc-items?issueState=0'),
      ])
      rows.value = [...listOf(unused), ...listOf(pending)].map(normalizeRow)
    }
    selectedNos.value = []
    activeNo.value = ''
  } catch (error) {
    rows.value = []
    errorMessage.value = error instanceof Error ? error.message : '載入計量品管項目失敗。'
  } finally { loading.value = false }
}
function selectRow(row: VariableQcItemRow, event: MouseEvent): void {
  activeNo.value = row.qcItemNo
  if (event.ctrlKey || event.metaKey) {
    selectedNos.value = selectedNos.value.includes(row.qcItemNo)
      ? selectedNos.value.filter(no => no !== row.qcItemNo)
      : [...selectedNos.value, row.qcItemNo]
  } else if (!selectedNos.value.includes(row.qcItemNo)) selectedNos.value = [row.qcItemNo]
}
function toggleRow(row: VariableQcItemRow, checked: boolean): void {
  activeNo.value = row.qcItemNo
  selectedNos.value = checked ? [...new Set([...selectedNos.value, row.qcItemNo])] : selectedNos.value.filter(no => no !== row.qcItemNo)
}
function askDelete(): void {
  errorMessage.value = ''
  notice.value = ''
  if (!selectedRows.value.length) { errorMessage.value = '請先選取要刪除量測記錄的計量品管項目。'; return }
  confirmOpen.value = true
}
async function confirmDelete(): Promise<void> {
  const targets = [...selectedRows.value]
  if (!targets.length) { confirmOpen.value = false; return }
  confirmOpen.value = false
  deleting.value = true
  errorMessage.value = ''
  notice.value = ''
  const completed: string[] = []
  const failures: string[] = []
  for (const row of targets) {
    try {
      if (mock.value) {
        const store = readStore()
        if ((store.measurementCounts[row.qcItemNo] ?? 0) < 1) throw new Error('沒有量測記錄可刪除。')
        store.measurementCounts[row.qcItemNo] = 0
        saveStore(store)
      } else {
        await request('api/qc/variable-qc-items/' + encodeURIComponent(row.qcItemNo) + '/measurement-log-deletions', {
          method: 'POST', body: JSON.stringify({ issueState: row.issueState }),
        })
      }
      completed.push(row.qcItemNo)
    } catch (error) {
      failures.push(row.qcItemNo + '：' + (error instanceof Error ? error.message : '刪除失敗。'))
    }
  }
  await loadRows()
  if (completed.length) notice.value = '已清除 ' + completed.length + ' 筆計量品管項目的量測記錄；主檔資料仍保留。'
  if (failures.length) errorMessage.value = '有 ' + failures.length + ' 筆未完成。' +
    (completed.length ? '已完成：' + completed.join('、') + '。' : '') + failures.join('；')
  deleting.value = false
}
function closePage(): void { emit('close') }

onMounted(() => { void loadRows() })
</script>

<template>
  <main class="measurement-delete-page">
    <header class="page-heading">
      <div><div class="eyebrow">QC210 · QCITEM_VDELETE</div><h1>刪除計量型品管項目量測記錄</h1></div>
    </header>

    <section class="delete-workspace" aria-label="計量型品管項目量測記錄清單">
      <div class="grid-area">
        <div class="grid-toolbar">
          <span>資料總筆數 {{ rows.length }}</span><span>已選取 {{ selectedRows.length }} 筆</span>
          <span v-if="loading">載入中…</span><span v-else-if="deleting">刪除處理中…</span>
        </div>
        <div class="grid-scroll">
          <table class="win-grid">
            <thead><tr>
              <th class="select-column"></th><th>QCItemNo</th><th>QCItemName</th><th>ChartType</th><th>ChartType</th>
              <th>ComponentCollect</th><th>LotIsValid</th><th>PlanNo</th><th>ChartControl</th><th>CollectType</th>
              <th>LimitLotNum</th><th>SampleSize</th><th>Unit</th><th>QCDataType</th><th>Creator</th><th>CreateDate</th>
              <th>Reviser</th><th>ReviseDate</th>
            </tr></thead>
            <tbody>
              <tr v-for="row in rows" :key="row.qcItemNo" :class="{ active: row.qcItemNo === activeNo, selected: selectedNos.includes(row.qcItemNo) }" @click="selectRow(row, $event)">
                <td class="select-column"><input type="checkbox" :checked="selectedNos.includes(row.qcItemNo)" :aria-label="'選取 ' + row.qcItemNo" @click.stop @change="toggleRow(row, ($event.target as HTMLInputElement).checked)"></td>
                <td>{{ row.qcItemNo }}</td><td>{{ row.qcItemName }}</td>
                <td>{{ valueLabel(row.chartType, ['X-R', 'X-S', 'X-Rm']) }}</td>
                <td>{{ valueLabel(row.specLineType, ['Upper/Lower Control', 'Upper Control', 'Lower Control', 'Not Control']) }}</td>
                <td>{{ yn(row.componentCollect) }}</td><td>{{ yn(row.lotIsValid) }}</td><td>{{ row.planNo }}</td>
                <td>{{ yn(row.chartControl) }}</td><td>{{ valueLabel(row.collectType, ['Manual', 'Automatic']) }}</td>
                <td class="number-cell">{{ row.limitLotNum }}</td><td class="number-cell">{{ row.sampleSize ?? '' }}</td>
                <td>{{ row.unit }}</td><td>{{ valueLabel(row.qcDataType, ['Particle', 'InLine', 'OffLine']) }}</td>
                <td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td>
              </tr>
              <tr v-if="!rows.length"><td colspan="18" class="empty-cell">{{ loading ? '載入中…' : '沒有 IssueState 為未審核或不使用的計量品管項目' }}</td></tr>
            </tbody>
          </table>
        </div>
        <div v-if="errorMessage" class="feedback error" role="alert">{{ errorMessage }}</div>
        <div v-else-if="notice" class="feedback success" role="status">{{ notice }}</div>
      </div>
      <aside class="side-actions">
        <button class="win-button delete-button" type="button" :disabled="loading || deleting" @click="askDelete">刪除(D)</button>
      </aside>
    </section>
    <footer class="window-footer"><span>來源：IssueState -1、0</span><button class="win-button" type="button" @click="closePage">關閉(X)</button></footer>

    <div v-if="confirmOpen" class="modal-backdrop" role="presentation" @click.self="confirmOpen = false">
      <section class="confirm-dialog" role="dialog" aria-modal="true" aria-labelledby="confirm-title">
        <h2 id="confirm-title">確認刪除量測記錄</h2>
        <p>確定刪除選取的 {{ selectedRows.length }} 筆計量品管項目量測資料？</p>
        <p class="confirm-note">只清除量測記錄，不刪除計量品管項目主檔。</p>
        <div class="confirm-actions"><button class="win-button delete-button" type="button" @click="confirmDelete">確定刪除</button><button class="win-button" type="button" autofocus @click="confirmOpen = false">取消</button></div>
      </section>
    </div>
  </main>
</template>

<style scoped>
.measurement-delete-page{height:100%;min-height:520px;display:flex;flex-direction:column;gap:10px;padding:14px;background:#f7f5ee;color:#333;font-family:Arial,"Microsoft JhengHei",sans-serif;box-sizing:border-box}
.page-heading{display:flex;align-items:center;justify-content:space-between;gap:16px;min-height:46px}.page-heading h1{margin:2px 0 0;font-size:21px;font-weight:600}.eyebrow{color:#777;font-size:11px;letter-spacing:.04em}
.delete-workspace{display:grid;grid-template-columns:minmax(0,1fr) 88px;gap:10px;min-height:0;flex:1;border:1px solid #c9c9c9;background:#fff;padding:8px}
.grid-area{min-width:0;min-height:0;display:flex;flex-direction:column}.grid-toolbar{height:28px;flex:none;display:flex;align-items:center;gap:18px;padding:0 4px;color:#666;font-size:12px}.grid-scroll{min-height:0;flex:1;overflow:auto;border:1px solid #c7c7c7}
.win-grid{border-collapse:collapse;width:max-content;min-width:100%;font-size:12px;white-space:nowrap}.win-grid th{position:sticky;top:0;z-index:1;background:#e9a126;color:#fff;font-weight:600;text-align:center;border:1px solid #d2d2d2;padding:7px 8px}.win-grid td{height:28px;border:1px solid #d2d2d2;padding:2px 8px;background:#fff}.win-grid tbody tr{cursor:pointer}.win-grid tbody tr:hover td{background:#fff5cf}.win-grid tbody tr.selected td{background:#ffeb9b}.win-grid tbody tr.active td{outline:1px solid #d1a63f;outline-offset:-1px}.select-column{width:30px;text-align:center!important}.select-column input{margin:0}.number-cell{text-align:right}.empty-cell{text-align:center!important;color:#888;height:100px!important}
.side-actions{display:flex;flex-direction:column;align-items:stretch;gap:8px}.win-button{min-height:34px;padding:6px 12px;border:1px solid #c8c8c8;border-radius:3px;background:linear-gradient(#fff,#f2f2f2);color:#333;font-size:13px;cursor:pointer}.win-button:hover:not(:disabled){border-color:#aaa;background:#f7f7f7}.win-button:disabled{opacity:.55;cursor:default}.delete-button{background:#f4a128;border-color:#e6951d;color:#fff;font-weight:600}.delete-button:hover:not(:disabled){background:#e9951e}.window-footer{display:flex;justify-content:space-between;align-items:center;color:#777;font-size:12px}.feedback{flex:none;margin-top:6px;padding:8px 10px;border:1px solid;border-radius:3px;font-size:12px}.feedback.error{border-color:#e3b9b5;background:#fff3f2;color:#9a3028}.feedback.success{border-color:#c9ddbd;background:#f1f8ed;color:#406531}
.modal-backdrop{position:fixed;inset:0;z-index:80;display:grid;place-items:center;background:rgba(25,30,35,.35);padding:20px}.confirm-dialog{width:min(440px,100%);padding:20px;background:#fff;border:1px solid #aaa;box-shadow:0 10px 32px #0003}.confirm-dialog h2{margin:0 0 14px;font-size:18px}.confirm-dialog p{margin:8px 0;font-size:14px;line-height:1.5}.confirm-note{color:#777}.confirm-actions{display:flex;justify-content:flex-end;gap:8px;margin-top:20px}
@media(max-width:740px){.measurement-delete-page{padding:8px}.delete-workspace{grid-template-columns:1fr;grid-template-rows:minmax(320px,1fr) auto}.side-actions{flex-direction:row;align-items:center}.page-heading h1{font-size:18px}}
</style>
