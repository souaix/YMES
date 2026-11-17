<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

type IssueState = -1 | 0 | 1 | 2 | null
type EditMode = '' | 'add' | 'edit' | 'copy'

interface StageRow {
  stageNo: string
  stageName: string
  stageType: string
  sequence: number
  issueState: IssueState
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
  dataStamp: number | null
  mockApprovalHasDetail?: boolean
}

interface StageDraft {
  stageNo: string
  stageName: string
  stageType: string
  sequence: number
}

interface Envelope<T> {
  items?: T[]
  rows?: T[]
  data?: T[]
  detail?: string
  message?: string
  title?: string
}

const props = withDefaults(defineProps<{
  apiBaseUrl: string
  currentUserNo?: string
  developmentPreview?: boolean
}>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()

const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes.dev.ps102-stage-basis.v1'
const stateOptions: Array<{ value: IssueState; label: string }> = [
  { value: 0, label: '未核准' },
  { value: 1, label: '簽核中' },
  { value: 2, label: '已核准' },
  { value: -1, label: '不使用' },
]
const seedRows: StageRow[] = [
  { stageNo: 'DEV-STAGE-01', stageName: '開發前段', stageType: 'FAB', sequence: 10, issueState: 0, creator: 'DEV001', createDate: '2026-11-12T08:30:00', reviser: 'DEV001', reviseDate: '2026-11-12T08:30:00', dataStamp: 1, mockApprovalHasDetail: true },
  { stageNo: 'DEV-STAGE-02', stageName: '開發中段', stageType: 'ASSY', sequence: 20, issueState: 2, creator: 'DEV002', createDate: '2026-10-20T09:15:00', reviser: 'DEV002', reviseDate: '2026-11-01T13:40:00', dataStamp: 4 },
  { stageNo: 'DEV-STAGE-03', stageName: '開發測試段', stageType: 'TEST', sequence: 30, issueState: 1, creator: 'DEV003', createDate: '2026-11-14T10:10:00', reviser: 'DEV003', reviseDate: '2026-11-14T10:10:00', dataStamp: 2 },
  { stageNo: 'DEV-STAGE-04', stageName: '開發停用段', stageType: 'PKG', sequence: 40, issueState: -1, creator: 'DEV001', createDate: '2026-08-05T14:20:00', reviser: 'DEV002', reviseDate: '2026-10-08T16:40:00', dataStamp: 5 },
]

const rows = ref<StageRow[]>([])
const issueState = ref<IssueState>(0)
const searchText = ref('')
const appliedStageNo = ref('')
const activeStageNo = ref('')
const selectedStageNos = ref<string[]>([])
const editorOpen = ref(false)
const mode = ref<EditMode>('')
const sourceStageNo = ref('')
const draft = ref<StageDraft>(blankDraft())
const draftDirty = ref(false)
const busy = ref(false)
const error = ref('')
const notice = ref('')

function blankDraft(): StageDraft { return { stageNo: '', stageName: '', stageType: '', sequence: 1 } }
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function userNo(): string { return props.currentUserNo || (mock.value ? 'DEV001' : '') }
function formatDate(value: string): string {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value.replace('T', ' ').replaceAll('-', '/').slice(0, 19)
  const pad = (part: number) => String(part).padStart(2, '0')
  return `${date.getFullYear()}/${pad(date.getMonth() + 1)}/${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
}
function normalizeRow(row: StageRow): StageRow {
  return {
    ...row,
    stageNo: row.stageNo ?? '', stageName: row.stageName ?? '', stageType: row.stageType ?? '',
    sequence: Number(row.sequence ?? 0), issueState: row.issueState == null ? null : Number(row.issueState) as Exclude<IssueState, null>,
    creator: row.creator ?? '', createDate: row.createDate ?? '', reviser: row.reviser ?? '', reviseDate: row.reviseDate ?? '',
    dataStamp: row.dataStamp == null ? null : Number(row.dataStamp),
  }
}
function persistMock(): void { localStorage.setItem(storageKey, JSON.stringify(rows.value)) }
function loadMock(): void {
  try {
    const saved = localStorage.getItem(storageKey)
    rows.value = (saved ? JSON.parse(saved) as StageRow[] : clone(seedRows)).map(normalizeRow)
    if (!saved) persistMock()
  } catch {
    rows.value = clone(seedRows).map(normalizeRow)
    persistMock()
  }
}

async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  const response = await fetch(props.apiBaseUrl.replace(/\/+$/, '') + path, { credentials: 'include', ...init, headers })
  if (response.status === 401) {
    window.location.assign(import.meta.env.BASE_URL + 'login')
    throw new Error('登入逾期，請重新登入。')
  }
  if (!response.ok) {
    const body = await response.json().catch(() => null) as Envelope<unknown> | null
    throw new Error(body?.detail || body?.message || body?.title || `API 回應錯誤 (${response.status})。`)
  }
  return response.status === 204 ? undefined as T : await response.json() as T
}

const visibleRows = computed(() => rows.value.filter(row => row.issueState === issueState.value
  && (!appliedStageNo.value || row.stageNo.toLocaleLowerCase().includes(appliedStageNo.value.toLocaleLowerCase())))
  .sort((left, right) => left.stageNo.localeCompare(right.stageNo)))
const activeRow = computed(() => rows.value.find(row => row.stageNo === activeStageNo.value) ?? null)
const selectedRows = computed(() => rows.value.filter(row => selectedStageNos.value.includes(row.stageNo)))

async function load(): Promise<void> {
  busy.value = true
  error.value = ''
  appliedStageNo.value = searchText.value.trim()
  try {
    if (mock.value) loadMock()
    else {
      const query = new URLSearchParams()
      if (appliedStageNo.value) query.set('stageNo', appliedStageNo.value)
      const result = await api<StageRow[] | Envelope<StageRow>>(`/api/fab-prs-stages?${query}`)
      const received = Array.isArray(result) ? result : result.items ?? result.rows ?? result.data ?? []
      rows.value = received.map(normalizeRow)
    }
    if (!visibleRows.value.some(row => row.stageNo === activeStageNo.value)) activeStageNo.value = visibleRows.value[0]?.stageNo ?? ''
    selectedStageNos.value = []
    notice.value = `資料總筆數 ${visibleRows.value.length}`
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : '載入製造層別失敗。'
  } finally { busy.value = false }
}

watch(issueState, () => {
  selectedStageNos.value = []
  activeStageNo.value = visibleRows.value[0]?.stageNo ?? ''
})

function selectRow(row: StageRow): void { activeStageNo.value = row.stageNo; selectedStageNos.value = [row.stageNo] }
function toggleRow(row: StageRow): void {
  activeStageNo.value = row.stageNo
  selectedStageNos.value = selectedStageNos.value.includes(row.stageNo)
    ? selectedStageNos.value.filter(value => value !== row.stageNo) : [...selectedStageNos.value, row.stageNo]
}
function openEditor(next: Exclude<EditMode, ''>): void {
  error.value = ''
  const row = activeRow.value
  if (next !== 'add' && !row) { error.value = '請先選取製造層別。'; return }
  if (next === 'edit' && row?.issueState !== 0) { error.value = '只有未核准資料可修改。'; return }
  mode.value = next
  sourceStageNo.value = row?.stageNo ?? ''
  draft.value = next === 'add' ? blankDraft() : {
    stageNo: row?.stageNo ?? '', stageName: row?.stageName ?? '', stageType: row?.stageType ?? '', sequence: row?.sequence ?? 1,
  }
  draftDirty.value = false
  editorOpen.value = true
}
function validateDraft(): string {
  if (!draft.value.stageNo.trim()) return 'StageNo 為必填欄位。'
  if (draft.value.stageNo.trim().length > 50) return 'StageNo 不可超過 50 個字元。'
  if (!draft.value.stageName.trim()) return 'StageName 為必填欄位。'
  if (draft.value.stageName.trim().length > 50) return 'StageName 不可超過 50 個字元。'
  if (!draft.value.stageType.trim()) return 'StageType 為必填欄位。'
  if (draft.value.stageType.trim().length > 50) return 'StageType 不可超過 50 個字元。'
  if (!Number.isInteger(draft.value.sequence) || draft.value.sequence < 1 || draft.value.sequence > 9999) return 'Sequence 必須是 1 到 9999 的整數。'
  const duplicate = rows.value.some(row => row.sequence === draft.value.sequence
    && !(mode.value === 'edit' && row.stageNo === sourceStageNo.value))
  if (duplicate) return `Sequence ${draft.value.sequence} 已經使用。`
  return ''
}
function payload(): StageDraft {
  return { stageNo: draft.value.stageNo.trim().toLocaleUpperCase(), stageName: draft.value.stageName.trim(), stageType: draft.value.stageType.trim(), sequence: draft.value.sequence }
}
async function save(keepOpen: boolean): Promise<void> {
  error.value = ''
  const validation = validateDraft()
  if (validation) { error.value = validation; return }
  const body = payload()
  const row = activeRow.value
  busy.value = true
  try {
    if (mock.value) {
      const duplicateNo = rows.value.some(item => item.stageNo.toLocaleUpperCase() === body.stageNo
        && !(mode.value === 'edit' && item.stageNo === sourceStageNo.value))
      if (duplicateNo) throw new Error(`StageNo ${body.stageNo} 已存在。`)
      if (mode.value === 'edit' && row) {
        Object.assign(row, body, { reviser: userNo(), reviseDate: new Date().toISOString(), dataStamp: (row.dataStamp ?? 0) + 1 })
      } else {
        rows.value.push({ ...body, issueState: 0, creator: userNo(), createDate: new Date().toISOString(), reviser: '', reviseDate: '', dataStamp: 0,
          ...(mode.value === 'copy' && row?.mockApprovalHasDetail ? { mockApprovalHasDetail: true } : {}) })
      }
      persistMock()
    } else if (mode.value === 'edit') {
      if (!row || row.issueState !== 0) throw new Error('只有未核准資料可修改。')
      await api(`/api/fab-prs-stages/${encodeURIComponent(sourceStageNo.value)}`, {
        method: 'PUT', body: JSON.stringify({ ...body, dataStamp: row.dataStamp }),
      })
    } else {
      await api('/api/fab-prs-stages', { method: 'POST', body: JSON.stringify(body) })
    }
    const savedStageNo = body.stageNo
    await load()
    activeStageNo.value = savedStageNo
    if (!keepOpen) editorOpen.value = false
    notice.value = `製造層別 ${savedStageNo} 已${mode.value === 'edit' ? '修改' : '新增'}。`
    if (keepOpen) draftDirty.value = false
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : '儲存製造層別失敗。'
  } finally { busy.value = false }
}

async function deleteSelected(): Promise<void> {
  const targets = selectedRows.value
  if (!targets.length) { error.value = '請先選取製造層別。'; return }
  if (!window.confirm(`確定刪除／停用 ${targets.length} 筆選取資料？`)) return
  error.value = ''
  busy.value = true
  let changed = false
  const skipped: string[] = []
  try {
    for (const row of targets) {
      if (row.issueState === 1) { skipped.push(row.stageNo); continue }
      if (mock.value) {
        if (row.issueState === 2) { row.issueState = -1; row.reviser = userNo(); row.reviseDate = new Date().toISOString(); row.dataStamp = (row.dataStamp ?? 0) + 1 }
        else rows.value = rows.value.filter(item => item.stageNo !== row.stageNo)
        persistMock()
      } else {
        await api(`/api/fab-prs-stages/${encodeURIComponent(row.stageNo)}`, {
          method: 'DELETE', body: JSON.stringify({ dataStamp: row.dataStamp }),
        })
      }
      changed = true
    }
    notice.value = skipped.length ? `已處理資料；簽核中略過：${skipped.join('、')}` : `已處理 ${targets.length} 筆製造層別。`
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '刪除製造層別失敗。' }
  finally { if (changed) await load(); busy.value = false }
}

async function approveSelected(): Promise<void> {
  const targets = selectedRows.value
  if (!targets.length) { error.value = '請先選取製造層別。'; return }
  if (targets.some(row => row.issueState !== 0)) { error.value = '只有未核准資料可送出核准。'; return }
  if (!window.confirm(`確定送出 ${targets.length} 筆製造層別核准？`)) return
  error.value = ''
  busy.value = true
  let changed = false
  try {
    for (const row of targets) {
      if (mock.value) {
        row.issueState = row.mockApprovalHasDetail ? 1 : 2
        row.reviser = userNo()
        row.reviseDate = new Date().toISOString()
        row.dataStamp = (row.dataStamp ?? 0) + 1
        persistMock()
      } else {
        await api(`/api/fab-prs-stages/${encodeURIComponent(row.stageNo)}/approve`, {
          method: 'POST', body: JSON.stringify({ dataStamp: row.dataStamp }),
        })
      }
      changed = true
    }
    notice.value = '核准要求已處理；正式簽核狀態依 IssueManagement 回呼更新。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '核准製造層別失敗。' }
  finally { if (changed) await load(); busy.value = false }
}

onMounted(load)
</script>

<template>
  <main class="stage-basis-master">
    <header class="page-title">
      <div><h2>製造層別設定</h2><small>流程資料 / 流程設定 / STAGE BASIS / PS102</small></div>
      <b v-if="mock" class="dev-badge">開發假資料</b>
      <button type="button" class="close-top" @click="emit('close')">關閉</button>
    </header>
    <p v-if="mock" class="dev-note">此畫面只使用本機 localStorage 假資料，不連 MES Oracle 或 YMES 正式資料庫。</p>
    <section class="filters">
      <label v-for="state in stateOptions" :key="state.value" class="state-option">
        <input v-model="issueState" type="radio" :value="state.value">{{ state.label }}
      </label>
      <label class="search">StageNo <input v-model="searchText" maxlength="50" @keyup.enter="load"></label>
      <button type="button" class="primary" :disabled="busy" @click="load">查詢</button>
    </section>
    <p v-if="error" class="message error" role="alert">{{ error }}</p>
    <p v-else-if="notice" class="message" role="status">{{ notice }}</p>
    <section class="main-layout">
      <div class="grid-wrap">
        <table>
          <thead><tr><th class="select-cell"></th><th>StageNo</th><th>StageName</th><th>StageType</th><th>Sequence</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
          <tbody>
            <tr v-for="row in visibleRows" :key="row.stageNo" :class="{ selected: activeStageNo === row.stageNo }" @click="selectRow(row)">
              <td class="select-cell"><input type="checkbox" :checked="selectedStageNos.includes(row.stageNo)" @click.stop="toggleRow(row)"></td>
              <td>{{ row.stageNo }}</td><td>{{ row.stageName }}</td><td>{{ row.stageType }}</td><td class="number-cell">{{ row.sequence }}</td>
              <td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td>
            </tr>
            <tr v-if="!visibleRows.length"><td colspan="9" class="empty">{{ busy ? '載入中…' : '查無資料' }}</td></tr>
          </tbody>
        </table>
      </div>
      <aside class="actions">
        <button type="button" class="primary" :disabled="busy" @click="openEditor('add')">ADD</button>
        <button type="button" :disabled="busy || activeRow?.issueState !== 0" @click="openEditor('edit')">EDIT</button>
        <button type="button" :disabled="busy || !activeRow" @click="openEditor('copy')">Copy</button>
        <button type="button" :disabled="busy || !selectedStageNos.length" @click="approveSelected">Approve</button>
        <button type="button" :disabled="busy || !selectedStageNos.length" @click="deleteSelected">DEL</button>
        <button type="button" class="close-bottom" @click="emit('close')">Close</button>
      </aside>
    </section>
    <footer class="grid-footer"><span>資料總筆數 {{ visibleRows.length }}<template v-if="activeRow">　{{ stateOptions.find(state => state.value === activeRow.issueState)?.label }}　DataStamp {{ activeRow.dataStamp ?? 'NULL' }}</template></span><small>Print：WinForms 原按鈕隱藏</small></footer>

    <div v-if="editorOpen" class="overlay" @click.self="editorOpen = false">
      <section class="dialog definition-dialog">
        <header class="dialog-title"><h3>FABPRSStageBasisDef ({{ mode === 'add' ? 'ADD' : mode === 'edit' ? 'EDIT' : 'Copy' }})</h3><button type="button" class="x-button" @click="editorOpen = false">×</button></header>
        <fieldset class="stage-fields"><legend>FABPRSStageBasis</legend>
          <div class="field-row"><label for="stage-no">StageNo</label><input id="stage-no" v-model.trim="draft.stageNo" class="yellow" maxlength="50" :readonly="mode === 'edit'" @input="draftDirty = true" @blur="draft.stageNo = draft.stageNo.toLocaleUpperCase()"></div>
          <div class="field-row"><label for="stage-name">StageName</label><input id="stage-name" v-model.trim="draft.stageName" class="yellow" maxlength="50" @input="draftDirty = true"></div>
          <div class="field-row"><label for="stage-type">StageType</label><input id="stage-type" v-model.trim="draft.stageType" class="yellow" maxlength="50" @input="draftDirty = true"></div>
          <div class="field-row"><label for="stage-sequence">Sequence</label><input id="stage-sequence" v-model.number="draft.sequence" class="yellow sequence-input" type="number" min="1" max="9999" step="1" @input="draftDirty = true"></div>
        </fieldset>
        <div class="audit-grid"><span>Creator</span><input :value="mode === 'edit' ? activeRow?.creator : userNo()" disabled><span>CreateDate</span><input :value="formatDate(mode === 'edit' ? activeRow?.createDate ?? '' : new Date().toISOString())" disabled><span>Reviser</span><input :value="mode === 'edit' ? userNo() : '—'" disabled><span>ReviseDate</span><input :value="mode === 'edit' ? formatDate(new Date().toISOString()) : '—'" disabled></div>
        <p v-if="error" class="message error">{{ error }}</p>
        <footer class="dialog-actions"><button type="button" class="primary" :disabled="busy" @click="save(false)">確定</button><button type="button" :disabled="busy || !draftDirty" @click="save(true)">套用</button><button type="button" @click="editorOpen = false">關閉</button></footer>
      </section>
    </div>
  </main>
</template>

<style scoped>
.stage-basis-master{display:flex;flex-direction:column;gap:10px;padding:16px 18px 22px;color:#292820;min-width:0}.page-title,.filters,.dialog-title,.dialog-actions,.grid-footer{display:flex;align-items:center;gap:9px}.page-title{justify-content:space-between}.page-title h2{margin:0;font-size:20px}.page-title small{display:block;margin-top:4px;color:#777466}.dev-badge{padding:4px 10px;border:1px solid #e7c600;border-radius:12px;background:#fff4b5}.dev-note{margin:0;padding:8px 10px;border:1px solid #e7c600;background:#fff9d9;color:#685b20}.filters{min-height:42px;flex-wrap:wrap;padding:7px 10px;border:1px solid #e5e1d5;background:#fff}.state-option{display:flex;align-items:center;gap:4px;font-size:13px}.search{display:flex;align-items:center;gap:8px;margin-left:9px}.search input,.dialog input{min-height:30px;padding:5px 7px;border:1px solid #aaa69b;background:#fff8d8;font:inherit}.dialog input:read-only{background:#f4f2ec}button{min-height:31px;padding:6px 12px;border:1px solid #c9c5ba;border-radius:3px;background:#f8f7f2;color:inherit;font:inherit;cursor:pointer;white-space:nowrap}button:disabled{opacity:.45;cursor:not-allowed}.primary{border-color:#eda029;background:#f6a62b;color:#fff}.message{margin:0;padding:8px 10px;border-left:3px solid #d4ad00;background:#fff9dd}.message.error{border-color:#b74439;background:#fff0ed;color:#8d241c}.main-layout{display:grid;grid-template-columns:minmax(0,1fr) 88px;gap:9px;min-height:330px}.grid-wrap{overflow:auto;border:1px solid #d8d3c6;background:#fff}table{width:100%;min-width:1020px;border-collapse:collapse;font-size:12px}th{position:sticky;top:0;background:#e7c600;text-align:left;font-weight:600}th,td{padding:7px 8px;border:1px solid #d8d3c6;white-space:nowrap}tbody tr{cursor:pointer}tbody tr:hover{background:#fff8d8}tbody tr.selected{background:#ffedaa}.select-cell{width:30px;text-align:center}.number-cell{text-align:right}.empty{text-align:center;color:#777366;padding:20px}.actions{display:flex;flex-direction:column;gap:6px}.actions button{min-height:32px}.close-bottom{margin-top:auto}.grid-footer{min-height:25px;color:#777466;font-size:12px}.grid-footer small{margin-left:auto}.overlay{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:16px;background:#14130f66}.dialog{width:min(520px,96vw);max-height:94vh;overflow:auto;padding:12px 14px;background:#fff;border:1px solid #c9c5ba;box-shadow:0 12px 40px #0003}.dialog-title{justify-content:space-between;padding-bottom:8px;margin-bottom:8px;border-bottom:1px solid #e5e1d5}.dialog-title h3{margin:0;font-size:17px}.x-button{min-width:32px;padding:4px 8px;font-size:19px}.stage-fields{display:grid;grid-template-columns:104px minmax(0,1fr);gap:7px 8px;margin:0;padding:10px;border:1px solid #c9c5ba}.stage-fields>legend{padding:0 5px}.field-row{display:contents}.field-row>label{align-self:center;text-align:right;padding-right:4px}.field-row>input{width:100%;background:#fff0ad}.field-row>input.sequence-input{width:90px;text-align:right}.audit-grid{display:grid;grid-template-columns:72px 1fr 76px 1fr;gap:5px 7px;align-items:center;margin-top:9px;padding:8px;border:1px solid #c9c5ba;background:#f8f7f2;font-size:12px}.audit-grid input{min-width:0;background:#eee}.dialog-actions{justify-content:flex-end;margin-top:10px}.dialog-actions .primary{min-width:94px}@media(max-width:850px){.main-layout{grid-template-columns:1fr}.actions{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));grid-row:2}.close-bottom{margin-top:0}}@media(max-width:560px){.stage-basis-master{padding:12px}.search{margin-left:0}.page-title{align-items:flex-start}.audit-grid{grid-template-columns:72px 1fr}}
</style>
