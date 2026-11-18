<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'

type IssueState = -1 | 0 | 1 | 2
type EditMode = '' | 'add' | 'edit' | 'copy'

interface ProductionSegmentRow {
  psNo: string
  psName: string
  psOrder: number
  description: string
  issueState: IssueState
  haveComponent: 0 | 1
  haveLevel: 0 | 1
  serialTypeNoLot: string
  serialTypeNoComp: string
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
  dataStamp: number | null
  mockApprovalHasDetail?: boolean
}

interface SegmentDraft {
  psNo: string
  psName: string
  psOrder: number
  description: string
  haveComponent: 0 | 1
  haveLevel: 0 | 1
  serialTypeNoLot: string
  serialTypeNoComp: string
}

interface SerialRuleOption {
  serialTypeNo: string
  description?: string
  enable?: number
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
  enterpriseNo?: string
}>(), { currentUserNo: '', developmentPreview: false, enterpriseNo: '' })
const emit = defineEmits<{ close: [] }>()
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()

const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const forceNoFlags = computed(() => props.enterpriseNo.trim().toLocaleUpperCase() === 'WAHHONG')
const rowsStorageKey = 'ymes.dev.ps101-production-segment.v1'
const stateOptions: Array<{ value: IssueState; label: string }> = [
  { value: 0, label: '未核准' },
  { value: 1, label: '簽核中' },
  { value: 2, label: '已核准' },
  { value: -1, label: '不使用' },
]
const seedRows: ProductionSegmentRow[] = [
  { psNo: 'DEV-PS-01', psName: 'DEV 前段製程', psOrder: 1, description: '開發用假資料：有 Component、沒有 Level。', issueState: 0, haveComponent: 1, haveLevel: 0, serialTypeNoLot: 'DEV-LOT-A', serialTypeNoComp: '', creator: 'DEV001', createDate: '2026-11-12T08:30:00', reviser: 'DEV001', reviseDate: '2026-11-12T08:30:00', dataStamp: 1, mockApprovalHasDetail: true },
  { psNo: 'DEV-PS-02', psName: 'DEV 中段製程', psOrder: 2, description: '開發用假資料：有 Level、沒有 Component。', issueState: 2, haveComponent: 0, haveLevel: 1, serialTypeNoLot: '', serialTypeNoComp: '', creator: 'DEV002', createDate: '2026-10-20T09:15:00', reviser: 'DEV002', reviseDate: '2026-11-01T13:40:00', dataStamp: 4 },
  { psNo: 'DEV-PS-03', psName: 'DEV 待簽核區段', psOrder: 3, description: '此列用於檢視簽核中狀態。', issueState: 1, haveComponent: 0, haveLevel: 0, serialTypeNoLot: 'DEV-LOT-B', serialTypeNoComp: '', creator: 'DEV003', createDate: '2026-11-14T10:10:00', reviser: 'DEV003', reviseDate: '2026-11-14T10:10:00', dataStamp: 2 },
  { psNo: 'DEV-PS-04', psName: 'DEV 停用區段', psOrder: 4, description: '此列用於檢視不使用狀態。', issueState: -1, haveComponent: 0, haveLevel: 0, serialTypeNoLot: '', serialTypeNoComp: '', creator: 'DEV001', createDate: '2026-08-05T14:20:00', reviser: 'DEV002', reviseDate: '2026-10-08T16:40:00', dataStamp: 5 },
]
const seedSerialRules: SerialRuleOption[] = [
  { serialTypeNo: 'DEV-LOT-A', description: 'DEV 批號規則 A', enable: 1 },
  { serialTypeNo: 'DEV-LOT-B', description: 'DEV 批號規則 B', enable: 1 },
  { serialTypeNo: 'DEV-LOT-OFF', description: 'DEV 停用規則範例', enable: 0 },
]

const rows = ref<ProductionSegmentRow[]>([])
const issueState = ref<IssueState>(0)
const searchText = ref('')
const activePSNo = ref('')
const selectedPSNos = ref<string[]>([])
const editorOpen = ref(false)
const mode = ref<EditMode>('')
const sourcePSNo = ref('')
const editorSource = ref<ProductionSegmentRow | null>(null)
const draft = ref<SegmentDraft>(blankDraft())
const draftDirty = ref(false)
const serialPickerOpen = ref(false)
const serialSearch = ref('')
const serialOptions = ref<SerialRuleOption[]>([])
const busy = ref(false)
const error = ref('')
const notice = ref('')

function blankDraft(): SegmentDraft {
  return { psNo: '', psName: '', psOrder: 1, description: '', haveComponent: 0, haveLevel: 0, serialTypeNoLot: '', serialTypeNoComp: '' }
}
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function currentUserNo(): string { return props.currentUserNo || (mock.value ? 'DEV001' : '') }
function formatDate(value: string): string {
  if (!value) return ''
  const parsed = new Date(value)
  if (Number.isNaN(parsed.getTime())) return value.replace('T', ' ').replaceAll('-', '/').slice(0, 19)
  const pad = (part: number) => String(part).padStart(2, '0')
  return `${parsed.getFullYear()}/${pad(parsed.getMonth() + 1)}/${pad(parsed.getDate())} ${pad(parsed.getHours())}:${pad(parsed.getMinutes())}:${pad(parsed.getSeconds())}`
}
function issueLabel(value: IssueState): string { return stateOptions.find(item => item.value === value)?.label ?? String(value) }
function serialLabel(value: number): string { return value === 1 ? 'Yes' : 'No' }
function normalizeRow(row: ProductionSegmentRow): ProductionSegmentRow {
  return {
    ...row,
    psNo: row.psNo ?? '', psName: row.psName ?? '', psOrder: Number(row.psOrder ?? 0),
    description: row.description ?? '', issueState: Number(row.issueState ?? 0) as IssueState,
    haveComponent: Number(row.haveComponent ?? 0) as 0 | 1, haveLevel: Number(row.haveLevel ?? 0) as 0 | 1,
    serialTypeNoLot: row.serialTypeNoLot ?? '', serialTypeNoComp: row.serialTypeNoComp ?? '',
    creator: row.creator ?? '', createDate: row.createDate ?? '', reviser: row.reviser ?? '', reviseDate: row.reviseDate ?? '',
    dataStamp: row.dataStamp == null ? null : Number(row.dataStamp),
  }
}
function persistMock(): void { localStorage.setItem(rowsStorageKey, JSON.stringify(rows.value)) }
function loadMock(): void {
  try {
    const saved = localStorage.getItem(rowsStorageKey)
    rows.value = (saved ? JSON.parse(saved) as ProductionSegmentRow[] : clone(seedRows)).map(normalizeRow)
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
  && (!searchText.value.trim() || row.psNo.toLocaleLowerCase().includes(searchText.value.trim().toLocaleLowerCase()))))
const activeRow = computed(() => visibleRows.value.find(row => row.psNo === activePSNo.value) ?? null)
const selectedRows = computed(() => visibleRows.value.filter(row => selectedPSNos.value.includes(row.psNo)))
const visibleSerialOptions = computed(() => serialOptions.value.filter(row => !serialSearch.value.trim()
  || row.serialTypeNo.toLocaleLowerCase().includes(serialSearch.value.trim().toLocaleLowerCase())
  || (row.description ?? '').toLocaleLowerCase().includes(serialSearch.value.trim().toLocaleLowerCase())))

watch(issueState, () => {
  selectedPSNos.value = []
  activePSNo.value = visibleRows.value[0]?.psNo ?? ''
})

async function load(): Promise<void> {
  busy.value = true
  error.value = ''
  try {
    if (mock.value) loadMock()
    else {
      const query = new URLSearchParams()
      if (searchText.value.trim()) query.set('psNo', searchText.value.trim())
      const result = await api<ProductionSegmentRow[] | Envelope<ProductionSegmentRow>>(`/api/production-segments?${query}`)
      const received = Array.isArray(result) ? result : result.items ?? result.rows ?? result.data ?? []
      rows.value = received.map(normalizeRow)
    }
    if (!visibleRows.value.some(row => row.psNo === activePSNo.value)) activePSNo.value = visibleRows.value[0]?.psNo ?? ''
    selectedPSNos.value = []
    notice.value = `資料總筆數 ${visibleRows.value.length}`
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : '載入生產區段失敗。'
  } finally { busy.value = false }
}
function selectRow(row: ProductionSegmentRow): void { activePSNo.value = row.psNo; selectedPSNos.value = [row.psNo] }
function toggleRow(row: ProductionSegmentRow): void {
  activePSNo.value = row.psNo
  selectedPSNos.value = selectedPSNos.value.includes(row.psNo)
    ? selectedPSNos.value.filter(value => value !== row.psNo) : [...selectedPSNos.value, row.psNo]
}
function openEditor(next: Exclude<EditMode, ''>): void {
  error.value = ''
  const row = activeRow.value
  if (next !== 'add' && !row) { error.value = '請先選取生產區段。'; return }
  if (next === 'edit' && row?.issueState !== 0) { error.value = '只有未核准資料可修改。'; return }
  mode.value = next
  sourcePSNo.value = row?.psNo ?? ''
  editorSource.value = next === 'add' ? null : row
  if (next === 'add') draft.value = blankDraft()
  else draft.value = {
    psNo: row?.psNo ?? '', psName: row?.psName ?? '', psOrder: row?.psOrder ?? 1,
    description: row?.description ?? '', haveComponent: row?.haveComponent ?? 0,
    haveLevel: row?.haveLevel ?? 0, serialTypeNoLot: row?.serialTypeNoLot ?? '',
    serialTypeNoComp: row?.serialTypeNoComp ?? '',
  }
  if (forceNoFlags.value) { draft.value.haveComponent = 0; draft.value.haveLevel = 0 }
  draftDirty.value = false
  editorOpen.value = true
}
function validateDraft(): string {
  if (!draft.value.psNo.trim()) return 'PS. No 為必填欄位。'
  if (draft.value.psNo.trim().length > 50) return 'PS. No 不可超過 50 個字元。'
  if (!draft.value.psName.trim()) return 'PS. Name 為必填欄位。'
  if (draft.value.psName.trim().length > 50) return 'PS. Name 不可超過 50 個字元。'
  if (!Number.isInteger(draft.value.psOrder) || draft.value.psOrder < 1 || draft.value.psOrder > 99) return 'PS. Order 必須介於 1 到 99。'
  if (draft.value.description.length > 255) return 'Description 不可超過 255 個字元。'
  if (draft.value.haveComponent === 1 && draft.value.haveLevel === 1) return 'Have Component 與 Have Level 只能擇一。'
  return ''
}
function draftPayload(): SegmentDraft {
  return {
    psNo: draft.value.psNo.trim().toLocaleUpperCase(), psName: draft.value.psName.trim(),
    psOrder: draft.value.psOrder, description: draft.value.description,
    haveComponent: forceNoFlags.value ? 0 : draft.value.haveComponent,
    haveLevel: forceNoFlags.value ? 0 : draft.value.haveLevel,
    serialTypeNoLot: draft.value.serialTypeNoLot, serialTypeNoComp: draft.value.serialTypeNoComp,
  }
}
async function save(keepOpen: boolean): Promise<void> {
  error.value = ''
  const validation = validateDraft()
  if (validation) { error.value = validation; return }
  const payload = draftPayload()
  const row = editorSource.value
  const wasEdit = mode.value === 'edit'
  busy.value = true
  try {
    let savedRow: ProductionSegmentRow
    if (mock.value) {
      const duplicate = rows.value.some(item => item.psNo.toLocaleUpperCase() === payload.psNo
        && !(mode.value === 'edit' && item.psNo === sourcePSNo.value))
      if (duplicate) throw new Error(`PS. No ${payload.psNo} 已存在。`)
      if (mode.value === 'edit' && row) {
        Object.assign(row, payload, { reviser: currentUserNo(), reviseDate: new Date().toISOString(), dataStamp: (row.dataStamp ?? 0) + 1 })
        savedRow = normalizeRow(clone(row))
      } else {
        savedRow = { ...payload, issueState: 0, dataStamp: 0, creator: currentUserNo(), createDate: new Date().toISOString(), reviser: '', reviseDate: '', ...(mode.value === 'copy' && row?.mockApprovalHasDetail ? { mockApprovalHasDetail: true } : {}) }
        rows.value.push(savedRow)
      }
      persistMock()
    } else if (mode.value === 'edit') {
      if (!row || row.issueState !== 0) throw new Error('只有未核准資料可修改。')
      savedRow = normalizeRow(await api<ProductionSegmentRow>(`/api/production-segments/${encodeURIComponent(sourcePSNo.value)}`, {
        method: 'PUT', body: JSON.stringify({ ...payload, dataStamp: row.dataStamp }),
      }))
    } else {
      savedRow = normalizeRow(await api<ProductionSegmentRow>('/api/production-segments', { method: 'POST', body: JSON.stringify(payload) }))
    }
    const savedPSNo = savedRow.psNo
    if (!wasEdit) { issueState.value = 0; searchText.value = '' }
    await load()
    activePSNo.value = savedPSNo
    if (!keepOpen) editorOpen.value = false
    else {
      // Apply on Add/Copy continues editing the saved row, using the server's
      // actual stamp and audit rather than submitting the same key again.
      mode.value = 'edit'
      sourcePSNo.value = savedPSNo
      editorSource.value = rows.value.find(item => item.psNo === savedPSNo) ?? savedRow
      const saved = editorSource.value
      draft.value = { psNo: saved.psNo, psName: saved.psName, psOrder: saved.psOrder, description: saved.description,
        haveComponent: saved.haveComponent, haveLevel: saved.haveLevel, serialTypeNoLot: saved.serialTypeNoLot, serialTypeNoComp: saved.serialTypeNoComp }
      draftDirty.value = false
    }
    notice.value = `生產區段 ${savedPSNo} 已${keepOpen ? '套用' : wasEdit ? '修改' : '新增'}。`
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : '儲存生產區段失敗。'
  } finally { busy.value = false }
}
async function loadSerialRules(): Promise<void> {
  error.value = ''
  try {
    if (mock.value) serialOptions.value = clone(seedSerialRules)
    else {
      const result = await api<SerialRuleOption[] | Envelope<SerialRuleOption>>('/api/system/serial-rule-basis?serialType=LOTNO')
      serialOptions.value = (Array.isArray(result) ? result : result.items ?? result.rows ?? result.data ?? [])
        .map(row => ({ ...row, serialTypeNo: row.serialTypeNo ?? '' }))
    }
    serialSearch.value = ''
    serialPickerOpen.value = true
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入批號序號規則失敗。' }
}
function chooseSerialRule(option: SerialRuleOption): void {
  draft.value.serialTypeNoLot = option.serialTypeNo
  draftDirty.value = true
  serialPickerOpen.value = false
}
async function deleteSelected(): Promise<void> {
  const targets = selectedRows.value
  if (!targets.length) { error.value = '請先選取生產區段。'; return }
  if (!(await requestConfirmation(`確定刪除／停用 ${targets.length} 筆選取資料？`))) return
  error.value = ''
  busy.value = true
  let changed = false
  try {
    for (const row of targets) {
      if (row.issueState === 1) throw new Error(`${row.psNo} 正在簽核中，不可刪除。`)
      if (mock.value) {
        if (row.issueState === 2) { row.issueState = -1; row.reviser = currentUserNo(); row.reviseDate = new Date().toISOString(); row.dataStamp = (row.dataStamp ?? 0) + 1 }
        else rows.value = rows.value.filter(item => item.psNo !== row.psNo)
        persistMock()
      } else {
        await api(`/api/production-segments/${encodeURIComponent(row.psNo)}`, {
          method: 'DELETE', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ dataStamp: row.dataStamp }),
        })
      }
      changed = true
    }
    notice.value = `已處理 ${targets.length} 筆生產區段。`
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '刪除生產區段失敗。' }
  finally { if (changed) await reloadKeepingFeedback(); busy.value = false }
}
async function approveSelected(): Promise<void> {
  const targets = selectedRows.value
  if (!targets.length) { error.value = '請先選取生產區段。'; return }
  if (targets.some(row => row.issueState !== 0)) { error.value = '只有未核准資料可送出核准。'; return }
  if (!(await requestConfirmation(`確定送出 ${targets.length} 筆生產區段核准？`))) return
  error.value = ''
  busy.value = true
  let changed = false
  try {
    for (const row of targets) {
      if (mock.value) {
        row.issueState = row.mockApprovalHasDetail ? 1 : 2
        row.reviser = currentUserNo()
        row.reviseDate = new Date().toISOString()
        row.dataStamp = (row.dataStamp ?? 0) + 1
        persistMock()
      } else {
        await api(`/api/production-segments/${encodeURIComponent(row.psNo)}/approve`, {
          method: 'POST', body: JSON.stringify({ dataStamp: row.dataStamp }),
        })
      }
      changed = true
    }
    notice.value = '選取的生產區段已完成核准處理。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '核准生產區段失敗。' }
  finally { if (changed) await reloadKeepingFeedback(); busy.value = false }
}

async function reloadKeepingFeedback(): Promise<void> {
  const failure = error.value, message = notice.value
  await load()
  if (failure) error.value = failure
  if (message) notice.value = message
}

onMounted(load)
</script>

<template>
  <main class="production-segment-master">
    <header class="page-title">
      <div><h2>生產區段資料</h2><small>流程資料 / 流程設定 / PS BASIS / PS101</small></div>
      <b v-if="mock" class="dev-badge">開發假資料</b>
      <button type="button" class="close-top" @click="emit('close')">關閉</button>
    </header>
    <p v-if="mock" class="dev-note">此畫面只使用本機 localStorage 假資料，不連 MES Oracle 或 YMES 正式資料庫。</p>
    <section class="filters">
      <label v-for="state in stateOptions" :key="state.value" class="state-option">
        <input v-model="issueState" type="radio" :value="state.value">{{ state.label }}
      </label>
      <label class="search">PS. No <input v-model="searchText" maxlength="50" @keyup.enter="load"></label>
      <button type="button" class="primary" :disabled="busy" @click="load">查詢</button>
    </section>
    <p v-if="error" class="message error" role="alert">{{ error }}</p>
    <p v-else-if="notice" class="message" role="status">{{ notice }}</p>
    <section class="main-layout">
      <div class="grid-wrap">
        <table>
          <thead><tr><th class="select-cell"></th><th>PS. No</th><th>PS. Name</th><th>PS. Order</th><th>HaveComponent</th><th>HaveLevel</th><th>Description</th><th>SerialTypeNo Lot</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
          <tbody>
            <tr v-for="row in visibleRows" :key="row.psNo" :class="{ selected: activePSNo === row.psNo }" @click="selectRow(row)">
              <td class="select-cell"><input type="checkbox" :aria-label="'選取 ' + row.psNo" :checked="selectedPSNos.includes(row.psNo)" @click.stop="toggleRow(row)"></td>
              <td>{{ row.psNo }}</td><td>{{ row.psName }}</td><td class="number-cell">{{ row.psOrder }}</td>
              <td>{{ serialLabel(row.haveComponent) }}</td><td>{{ serialLabel(row.haveLevel) }}</td><td class="description-cell">{{ row.description }}</td>
              <td>{{ row.serialTypeNoLot }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td>
            </tr>
            <tr v-if="!visibleRows.length"><td colspan="12" class="empty">{{ busy ? '載入中…' : '查無資料' }}</td></tr>
          </tbody>
        </table>
      </div>
      <aside class="actions">
        <button type="button" class="primary" :disabled="busy" @click="openEditor('add')">ADD (A)</button>
        <button type="button" :disabled="busy || activeRow?.issueState !== 0" @click="openEditor('edit')">EDIT (E)</button>
        <button type="button" :disabled="busy || !activeRow" @click="openEditor('copy')">Copy (C)</button>
        <button type="button" :disabled="busy || !selectedPSNos.length" @click="approveSelected">Approve (Y)</button>
        <button type="button" :disabled="busy || !selectedPSNos.length" @click="deleteSelected">DEL (D)</button>
        <button type="button" class="close-bottom" @click="emit('close')">Close (X)</button>
      </aside>
    </section>
    <footer class="grid-footer"><span>資料總筆數 {{ visibleRows.length }}<template v-if="activeRow">　{{ issueLabel(activeRow.issueState) }}</template></span></footer>

    <div v-if="editorOpen" class="overlay" @click.self="editorOpen = false">
      <section class="dialog definition-dialog" role="dialog" aria-modal="true" aria-label="生產區段定義">
        <header class="dialog-title"><h3>Production Segment Define ({{ mode === 'add' ? 'ADD' : mode === 'edit' ? 'EDIT' : 'Copy' }})</h3><button type="button" class="x-button" @click="editorOpen = false">×</button></header>
        <fieldset class="segment-fields"><legend>Production Segment Data</legend>
          <div class="field-row"><label for="ps-no">PS. No</label><input id="ps-no" v-model.trim="draft.psNo" class="yellow" maxlength="50" :readonly="mode === 'edit'" @input="draftDirty = true" @blur="draft.psNo = draft.psNo.toLocaleUpperCase()"></div>
          <div class="field-row"><label for="ps-name">PS. Name</label><input id="ps-name" v-model.trim="draft.psName" class="yellow" maxlength="50" @input="draftDirty = true"></div>
          <div class="field-row"><label for="ps-order">PS. Order</label><input id="ps-order" v-model.number="draft.psOrder" type="number" min="1" max="99" step="1" @input="draftDirty = true"></div>
          <div class="field-row"><label>Have Component</label><fieldset class="choice-box"><label><input v-model.number="draft.haveComponent" type="radio" :value="0" :disabled="forceNoFlags" @change="draftDirty = true">No</label><label><input v-model.number="draft.haveComponent" type="radio" :value="1" :disabled="forceNoFlags" @change="draftDirty = true">Yes</label></fieldset></div>
          <div class="field-row"><label>Have Level</label><fieldset class="choice-box"><label><input v-model.number="draft.haveLevel" type="radio" :value="0" :disabled="forceNoFlags" @change="draftDirty = true">No</label><label><input v-model.number="draft.haveLevel" type="radio" :value="1" :disabled="forceNoFlags" @change="draftDirty = true">Yes</label></fieldset></div>
          <div class="field-row description-row"><label for="ps-description">Description</label><textarea id="ps-description" v-model="draft.description" maxlength="255" @input="draftDirty = true"></textarea></div>
          <fieldset class="serial-rule"><legend>Serial Rule Config</legend><div class="serial-row"><label for="lot-serial">SerialTypeNo_Lot</label><input id="lot-serial" :value="draft.serialTypeNoLot" readonly><button type="button" :disabled="busy" @click="loadSerialRules">...</button></div></fieldset>
        </fieldset>
        <div class="audit-grid"><span>Creator</span><input :value="mode === 'edit' ? editorSource?.creator : currentUserNo()" disabled><span>CreateDate</span><input :value="formatDate(mode === 'edit' ? editorSource?.createDate ?? '' : new Date().toISOString())" disabled><span>Reviser</span><input :value="mode === 'edit' ? editorSource?.reviser : '—'" disabled><span>ReviseDate</span><input :value="mode === 'edit' ? formatDate(editorSource?.reviseDate ?? '') : '—'" disabled></div>
        <p v-if="forceNoFlags" class="enterprise-note">WAHHONG 規則：Have Component 與 Have Level 固定為 No。</p>
        <p v-if="error" class="message error">{{ error }}</p>
        <footer class="dialog-actions"><button type="button" class="primary" :disabled="busy" @click="save(false)">確定</button><button type="button" :disabled="busy || !draftDirty" @click="save(true)">套用</button><button type="button" @click="editorOpen = false">關閉</button></footer>
      </section>
    </div>

    <div v-if="serialPickerOpen" class="overlay picker-overlay" @click.self="serialPickerOpen = false">
      <section class="dialog serial-dialog" role="dialog" aria-modal="true" aria-label="LOTNO 序號規則"><header class="dialog-title"><h3>LOTNO Serial Rule</h3><button type="button" class="x-button" @click="serialPickerOpen = false">×</button></header>
        <label class="serial-search">SerialTypeNo <input v-model="serialSearch" maxlength="50" @keyup.enter="visibleSerialOptions.length && chooseSerialRule(visibleSerialOptions[0])"></label>
        <div class="grid-wrap serial-grid"><table><thead><tr><th>SerialTypeNo</th></tr></thead><tbody>
          <tr v-for="option in visibleSerialOptions" :key="option.serialTypeNo" @dblclick="chooseSerialRule(option)" @click="chooseSerialRule(option)"><td>{{ option.serialTypeNo }}</td></tr>
          <tr v-if="!visibleSerialOptions.length"><td class="empty">查無序號規則</td></tr>
        </tbody></table></div>
        <footer class="dialog-actions"><button type="button" @click="draft.serialTypeNoLot = ''; draftDirty = true; serialPickerOpen = false">清除</button><button type="button" @click="serialPickerOpen = false">關閉</button></footer>
      </section>
    </div>
  </main>
  <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
</template>

<style scoped>
.production-segment-master{display:flex;flex-direction:column;gap:10px;padding:16px 18px 22px;color:#292820;min-width:0}.page-title,.filters,.dialog-title,.dialog-actions,.grid-footer{display:flex;align-items:center;gap:9px}.page-title{justify-content:space-between}.page-title h2{margin:0;font-size:20px}.page-title small,.dialog-title small{display:block;margin-top:4px;color:#777466}.dev-badge{padding:4px 10px;border:1px solid #e7c600;border-radius:12px;background:#fff4b5}.dev-note,.enterprise-note{margin:0;padding:8px 10px;border:1px solid #e7c600;background:#fff9d9;color:#685b20}.filters{min-height:42px;flex-wrap:wrap;padding:7px 10px;border:1px solid #e5e1d5;background:#fff}.state-option{display:flex;align-items:center;gap:4px;font-size:13px}.search{display:flex;align-items:center;gap:8px;margin-left:9px}.search input,.dialog input,.dialog textarea{min-height:30px;padding:5px 7px;border:1px solid #aaa69b;background:#fff8d8;font:inherit}.dialog input:read-only{background:#f4f2ec}button{min-height:31px;padding:6px 12px;border:1px solid #c9c5ba;border-radius:3px;background:#f8f7f2;color:inherit;font:inherit;cursor:pointer;white-space:nowrap}button:disabled{opacity:.45;cursor:not-allowed}.primary{border-color:#eda029;background:#f6a62b;color:#fff}.message{margin:0;padding:8px 10px;border-left:3px solid #d4ad00;background:#fff9dd}.message.error{border-color:#b74439;background:#fff0ed;color:#8d241c}.main-layout{display:grid;grid-template-columns:minmax(0,1fr) 88px;gap:9px;min-height:330px}.grid-wrap{overflow:auto;border:1px solid #d8d3c6;background:#fff}table{width:100%;min-width:1320px;border-collapse:collapse;font-size:12px}th{position:sticky;top:0;background:#e7c600;text-align:left;font-weight:600}th,td{padding:7px 8px;border:1px solid #d8d3c6;white-space:nowrap}tbody tr{cursor:pointer}tbody tr:hover{background:#fff8d8}tbody tr.selected{background:#ffedaa}.select-cell{width:30px;text-align:center}.number-cell{text-align:right}.description-cell{min-width:210px;max-width:330px;white-space:normal}.empty{text-align:center;color:#777366;padding:20px}.actions{display:flex;flex-direction:column;gap:6px}.actions button{min-height:32px}.close-bottom{margin-top:auto}.grid-footer{min-height:25px;color:#777466;font-size:12px}.grid-footer small{margin-left:auto}.overlay{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:16px;background:#14130f66}.dialog{width:min(1000px,98vw);max-height:94vh;overflow:auto;padding:12px 14px;background:#fff;border:1px solid #c9c5ba;box-shadow:0 12px 40px #0003}.dialog-title{justify-content:space-between;padding-bottom:8px;margin-bottom:8px;border-bottom:1px solid #e5e1d5}.dialog-title h3{margin:0;font-size:17px}.x-button{min-width:32px;padding:4px 8px;font-size:19px}.segment-fields{display:grid;grid-template-columns:120px minmax(0,1fr) 120px 150px;grid-auto-rows:minmax(30px,auto);gap:0 0;margin:0;padding:8px 10px 10px;border:1px solid #c9c5ba}.segment-fields>legend{padding:0 5px}.field-row{display:contents}.field-row>label{align-self:center;padding:4px 8px 4px 2px;text-align:right}.field-row>input,.field-row>textarea{grid-column:2/5;justify-self:start;width:min(100%,400px)}.field-row input.yellow{background:#fff0ad}.field-row>input[type=number]{width:82px}.choice-box{grid-column:2/5;display:flex;align-items:center;gap:10px;width:124px;min-height:29px;margin:0;padding:2px 7px;border:1px solid #bdb9af}.choice-box label{display:flex;align-items:center;gap:3px}.choice-box input{min-height:auto;padding:0}.description-row{min-height:168px}.description-row>label{align-self:start;padding-top:8px}.description-row>textarea{align-self:stretch;justify-self:stretch;width:auto;min-height:152px;resize:vertical;background:#fff}.serial-rule{grid-column:1/5;min-height:90px;margin:5px 0 0;padding:8px;border:1px solid #c9c5ba}.serial-rule legend{padding:0 5px}.serial-row{display:grid;grid-template-columns:120px minmax(0,1fr) 30px;align-items:center;gap:8px;padding:8px 3px}.serial-row label{text-align:right}.serial-row button{width:30px;padding:2px}.audit-grid{display:grid;grid-template-columns:80px 1fr 84px 1fr;gap:5px 8px;align-items:center;margin-top:9px;padding:8px;border:1px solid #c9c5ba;background:#f8f7f2;font-size:12px}.audit-grid input{min-width:0;background:#eee}.dialog-actions{justify-content:flex-end;margin-top:10px}.dialog-actions .primary{min-width:94px}.serial-dialog{width:min(760px,96vw)}.serial-search{display:grid;grid-template-columns:120px minmax(0,1fr);align-items:center;gap:8px;padding:6px 2px}.serial-grid{max-height:380px}.serial-grid table{min-width:500px}.picker-overlay{z-index:1010}@media(max-width:850px){.main-layout{grid-template-columns:1fr}.actions{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));grid-row:2}.close-bottom{margin-top:0}.segment-fields{grid-template-columns:120px minmax(0,1fr)}.field-row>input,.field-row>textarea,.choice-box{grid-column:2}.serial-rule{grid-column:1/3}.audit-grid{grid-template-columns:80px 1fr}}@media(max-width:560px){.production-segment-master{padding:12px}.search{margin-left:0}.page-title{align-items:flex-start}.field-row{display:grid;grid-column:1/-1;grid-template-columns:120px minmax(0,1fr);gap:6px;padding:4px 0}.field-row>label{text-align:left}.field-row>input,.field-row>textarea,.choice-box{grid-column:2;width:100%}.serial-rule{grid-column:1/-1}.serial-row{grid-template-columns:1fr}.serial-row label{text-align:left}.serial-row button{width:100%}.audit-grid{grid-template-columns:1fr 1fr}}
</style>
