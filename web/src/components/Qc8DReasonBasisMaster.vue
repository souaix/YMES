<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

type IssueState = -1 | 0 | 1 | 2
type EditorMode = 'subtype-add' | 'subtype-edit' | 'subtype-copy' | 'reason-add' | 'reason-edit' | 'reason-copy' | null
type ReasonType = 1 | 2 | 3 | 4 | 5

interface AuditFields {
  creator: string | null
  createDate: string | null
  reviser: string | null
  reviseDate: string | null
}

interface ReasonSubtype extends AuditFields {
  reasonType: number
  reasonSubType: string
  reasonSubTypeName: string | null
  description: string | null
  issueState: IssueState
  dataStamp: number | null
}

interface Reason extends AuditFields {
  reasonNo: string
  reasonName: string
  reasonType: string
  reasonSubType: string
  reasonLevel: number
  description: string | null
  dataStamp: number | null
}

interface MockStore {
  subtypes: ReasonSubtype[]
  reasons: Reason[]
}

interface AuditEditor {
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
}

interface SubtypeEditor extends AuditEditor {
  reasonType: number
  reasonSubType: string
  reasonSubTypeName: string
  description: string
  dataStamp: number | null
}

interface ReasonEditor extends AuditEditor {
  reasonNo: string
  reasonName: string
  reasonType: string
  reasonSubType: string
  reasonLevel: number
  description: string
  dataStamp: number | null
}

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const useMock = computed(() => import.meta.env.DEV && props.developmentPreview)
const mockStorageKey = 'ymes-dev-mock:qc-8d-reason-basis:v1'
const issueStates: { value: IssueState; label: string }[] = [
  { value: 0, label: '未簽核' },
  { value: 1, label: '簽核中' },
  { value: 2, label: '已簽核' },
  { value: -1, label: '不使用' },
]
const reasonTypes: { value: ReasonType; label: string }[] = [
  { value: 1, label: '人員' },
  { value: 2, label: '設備' },
  { value: 3, label: '物料' },
  { value: 4, label: '方法' },
  { value: 5, label: '環境' },
]
const seedStore: MockStore = {
  subtypes: [
    { reasonType: 2, reasonSubType: 'DEV-EQP-CAUSE', reasonSubTypeName: '設備原因（DEV）', description: '8D 設備原因測試資料', issueState: 0, creator: 'DEV001', createDate: '2026-11-02T08:00:00+08:00', reviser: 'DEV001', reviseDate: '2026-11-03T10:00:00+08:00', dataStamp: 2 },
    { reasonType: 3, reasonSubType: 'DEV-MTL-CAUSE', reasonSubTypeName: '物料原因（DEV）', description: '8D 物料原因測試資料', issueState: 1, creator: 'DEV001', createDate: '2026-11-04T08:00:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
    { reasonType: 4, reasonSubType: 'DEV-PROC-CAUSE', reasonSubTypeName: '製程原因（DEV）', description: '8D 製程原因測試資料', issueState: 2, creator: 'DEV001', createDate: '2026-11-05T08:00:00+08:00', reviser: 'DEV001', reviseDate: '2026-11-05T09:30:00+08:00', dataStamp: 1 },
    { reasonType: 1, reasonSubType: 'DEV-OLD-CAUSE', reasonSubTypeName: '舊原因（DEV）', description: '8D 不使用狀態範例', issueState: -1, creator: 'DEV001', createDate: '2026-11-06T08:00:00+08:00', reviser: 'DEV001', reviseDate: '2026-11-06T09:00:00+08:00', dataStamp: 1 },
  ],
  reasons: [
    { reasonNo: 'DEV-8D-EQ-01', reasonName: '感測器訊號異常（DEV）', reasonType: '2', reasonSubType: 'DEV-EQP-CAUSE', reasonLevel: 1, description: '原因明細假資料', creator: 'DEV001', createDate: '2026-11-02T08:30:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
    { reasonNo: 'DEV-8D-EQ-02', reasonName: '設備參數設定錯誤（DEV）', reasonType: '2', reasonSubType: 'DEV-EQP-CAUSE', reasonLevel: 2, description: '原因明細假資料', creator: 'DEV001', createDate: '2026-11-02T08:35:00+08:00', reviser: 'DEV001', reviseDate: '2026-11-03T08:35:00+08:00', dataStamp: 1 },
    { reasonNo: 'DEV-8D-MTL-01', reasonName: '來料尺寸偏差（DEV）', reasonType: '3', reasonSubType: 'DEV-MTL-CAUSE', reasonLevel: 1, description: '原因明細假資料', creator: 'DEV001', createDate: '2026-11-04T08:30:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
    { reasonNo: 'DEV-8D-PR-01', reasonName: '作業條件未依規範（DEV）', reasonType: '4', reasonSubType: 'DEV-PROC-CAUSE', reasonLevel: 1, description: '原因明細假資料', creator: 'DEV001', createDate: '2026-11-05T08:30:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
  ],
}

const allSubtypes = ref<ReasonSubtype[]>([])
const reasons = ref<Reason[]>([])
const issueState = ref<IssueState>(0)
const searchText = ref('')
const selectedSubtypeNo = ref('')
const selectedSubtypeNos = ref<string[]>([])
const selectedReasonNos = ref<string[]>([])
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const notice = ref('')
const editorMode = ref<EditorMode>(null)
const subtypeEditor = ref<SubtypeEditor>(blankSubtypeEditor())
const reasonEditor = ref<ReasonEditor>(blankReasonEditor())
const editorError = ref('')
const editorNotice = ref('')
const editorBusy = ref(false)
const subtypeSource = ref<ReasonSubtype | null>(null)
const reasonSource = ref<Reason | null>(null)

const visibleSubtypes = computed(() => allSubtypes.value
  .filter(row => row.issueState === issueState.value)
  .filter(row => !searchText.value.trim() || row.reasonSubType.toLocaleLowerCase().includes(searchText.value.trim().toLocaleLowerCase()))
  .slice()
  .sort((left, right) => left.reasonSubType.localeCompare(right.reasonSubType)))
const activeSubtype = computed(() => allSubtypes.value.find(row => row.reasonSubType === selectedSubtypeNo.value) ?? null)
const visibleReasons = computed(() => reasons.value.filter(row => row.reasonSubType === selectedSubtypeNo.value))
const selectedSubtypes = computed(() => allSubtypes.value.filter(row => selectedSubtypeNos.value.includes(row.reasonSubType)))
const editorTitle = computed(() => {
  if (!editorMode.value) return ''
  const operation = editorMode.value.endsWith('add') ? '新增' : editorMode.value.endsWith('copy') ? '複製' : '修改'
  return editorMode.value.startsWith('subtype-') ? '8D 原因子類別（' + operation + '）' : '8D 原因明細（' + operation + '）'
})
const editorAudit = computed(() => editorMode.value?.startsWith('subtype-') ? subtypeEditor.value : reasonEditor.value)
const detailEditable = computed(() => activeSubtype.value?.issueState === 0)
const canDeleteReasons = computed(() => detailEditable.value && (issueState.value === 0 || issueState.value === -1))

function blankAudit(): AuditEditor {
  return { creator: props.currentUserNo || 'DEV001', createDate: now(), reviser: '', reviseDate: '' }
}
function blankSubtypeEditor(): SubtypeEditor {
  return { ...blankAudit(), reasonType: 1, reasonSubType: '', reasonSubTypeName: '', description: '', dataStamp: null }
}
function blankReasonEditor(): ReasonEditor {
  return { ...blankAudit(), reasonNo: '', reasonName: '', reasonType: '', reasonSubType: '', reasonLevel: 0, description: '', dataStamp: null }
}
function now(): string { return new Date().toISOString() }
function dateText(value?: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat('zh-TW', { dateStyle: 'short', timeStyle: 'medium', hour12: false }).format(date)
}
function statusText(value: number): string { return issueStates.find(item => item.value === value)?.label ?? String(value) }
function statusClass(value: number): string { return 'issue-state-' + String(value).replace('-', 'minus-') }
function typeText(value: number | string): string {
  const option = reasonTypes.find(item => String(item.value) === String(value))
  return option ? String(value) + ' ' + option.label : String(value)
}
function enc(value: string): string { return encodeURIComponent(value) }
function stampQuery(value: number | null): string {
  return value === null ? 'dataStamp=0&dataStampIsNull=true' : 'dataStamp=' + enc(String(value))
}
function readMock(): MockStore {
  try {
    const stored = window.localStorage.getItem(mockStorageKey)
    if (!stored) return structuredClone(seedStore)
    const parsed = JSON.parse(stored) as Partial<MockStore>
    return { subtypes: parsed.subtypes ?? structuredClone(seedStore.subtypes), reasons: parsed.reasons ?? structuredClone(seedStore.reasons) }
  } catch {
    return structuredClone(seedStore)
  }
}
function writeMock(store: MockStore): void { window.localStorage.setItem(mockStorageKey, JSON.stringify(store)) }
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), {
    credentials: 'include',
    ...init,
    headers: { ...(init.body ? { 'Content-Type': 'application/json' } : {}), ...init.headers },
  })
  if (response.status === 401) {
    window.location.assign(appBase + '/')
    throw new Error('登入狀態已逾期，請重新登入。')
  }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    if (response.status === 404) throw new Error('8D 原因基本資料 API 尚未建立；請依 todolist_homepc.md 完成 QC111。')
    throw new Error(problem?.detail || problem?.message || problem?.title || 'API 回應錯誤（' + response.status + '）。')
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}

async function loadSubtypeRows(): Promise<void> {
  if (useMock.value) {
    allSubtypes.value = readMock().subtypes
    return
  }
  const query = new URLSearchParams({ search: searchText.value.trim() })
  allSubtypes.value = await request<ReasonSubtype[]>('api/qc/8d-reason-subtypes?' + query.toString())
}
async function loadReasons(reasonSubType: string): Promise<void> {
  selectedReasonNos.value = []
  if (!reasonSubType) {
    reasons.value = []
    return
  }
  if (useMock.value) {
    reasons.value = readMock().reasons.filter(row => row.reasonSubType === reasonSubType)
    return
  }
  const query = new URLSearchParams({ reasonSubType })
  reasons.value = await request<Reason[]>('api/qc/8d-reasons?' + query.toString())
}
async function query(): Promise<void> {
  loading.value = true
  busy.value = true
  errorMessage.value = ''
  notice.value = ''
  selectedSubtypeNo.value = ''
  selectedSubtypeNos.value = []
  reasons.value = []
  try {
    await loadSubtypeRows()
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '查詢 8D 原因子類別失敗。'
  } finally {
    loading.value = false
    busy.value = false
  }
}
async function selectSubtype(row: ReasonSubtype): Promise<void> {
  selectedSubtypeNo.value = row.reasonSubType
  selectedSubtypeNos.value = [row.reasonSubType]
  selectedReasonNos.value = []
  loading.value = true
  errorMessage.value = ''
  try {
    await loadReasons(row.reasonSubType)
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '載入原因明細失敗。'
  } finally {
    loading.value = false
  }
}
function toggleSubtype(row: ReasonSubtype): void {
  const selected = selectedSubtypeNos.value.includes(row.reasonSubType)
  selectedSubtypeNos.value = selected
    ? selectedSubtypeNos.value.filter(item => item !== row.reasonSubType)
    : [...selectedSubtypeNos.value, row.reasonSubType]
  selectedSubtypeNo.value = row.reasonSubType
  selectedReasonNos.value = []
  loading.value = true
  errorMessage.value = ''
  void loadReasons(row.reasonSubType)
    .catch(error => { errorMessage.value = error instanceof Error ? error.message : '載入原因明細失敗。' })
    .finally(() => { loading.value = false })
}
function toggleReason(row: Reason): void {
  selectedReasonNos.value = selectedReasonNos.value.includes(row.reasonNo)
    ? selectedReasonNos.value.filter(item => item !== row.reasonNo)
    : [...selectedReasonNos.value, row.reasonNo]
}
function selectedRow(row: ReasonSubtype): boolean { return selectedSubtypeNo.value === row.reasonSubType }
function subtypeChecked(row: ReasonSubtype): boolean { return selectedSubtypeNos.value.includes(row.reasonSubType) }
function reasonChecked(row: Reason): boolean { return selectedReasonNos.value.includes(row.reasonNo) }
watch(issueState, () => {
  selectedSubtypeNo.value = ''
  selectedSubtypeNos.value = []
  selectedReasonNos.value = []
  reasons.value = []
})

function beginSubtype(mode: 'add' | 'edit' | 'copy'): void {
  errorMessage.value = ''
  editorError.value = ''
  editorNotice.value = ''
  subtypeSource.value = mode === 'add' ? null : selectedSubtypes.value.length === 1 ? structuredClone(selectedSubtypes.value[0]) : activeSubtype.value ? structuredClone(activeSubtype.value) : null
  if (mode !== 'add' && !subtypeSource.value) {
    errorMessage.value = '請先選取一筆原因子類別。'
    return
  }
  if (mode === 'add') subtypeEditor.value = blankSubtypeEditor()
  else if (subtypeSource.value) {
    subtypeEditor.value = {
      ...subtypeSource.value,
      creator: subtypeSource.value.creator || '',
      createDate: subtypeSource.value.createDate || '',
      reviser: subtypeSource.value.reviser || '',
      reviseDate: subtypeSource.value.reviseDate || '',
      reasonSubTypeName: subtypeSource.value.reasonSubTypeName || '',
      description: subtypeSource.value.description || '',
    }
    if (mode === 'copy') subtypeEditor.value = { ...subtypeEditor.value, ...blankAudit(), reasonSubType: '', dataStamp: null }
  }
  editorMode.value = mode === 'add' ? 'subtype-add' : mode === 'edit' ? 'subtype-edit' : 'subtype-copy'
}
function beginReason(mode: 'add' | 'edit' | 'copy'): void {
  errorMessage.value = ''
  editorError.value = ''
  editorNotice.value = ''
  if (!activeSubtype.value || activeSubtype.value.issueState !== 0) {
    errorMessage.value = '原因明細僅能在未簽核的子類別下維護。'
    return
  }
  reasonSource.value = mode === 'add' ? null : visibleReasons.value.find(row => selectedReasonNos.value.includes(row.reasonNo)) ? structuredClone(visibleReasons.value.find(row => selectedReasonNos.value.includes(row.reasonNo))!) : null
  if (mode !== 'add' && (selectedReasonNos.value.length !== 1 || !reasonSource.value)) {
    errorMessage.value = '請選取一筆原因明細。'
    return
  }
  if (mode === 'add') {
    reasonEditor.value = { ...blankReasonEditor(), reasonType: String(activeSubtype.value.reasonType), reasonSubType: activeSubtype.value.reasonSubType }
  } else if (reasonSource.value) {
    reasonEditor.value = {
      ...reasonSource.value,
      creator: reasonSource.value.creator || '',
      createDate: reasonSource.value.createDate || '',
      reviser: reasonSource.value.reviser || '',
      reviseDate: reasonSource.value.reviseDate || '',
      description: reasonSource.value.description || '',
    }
    if (mode === 'copy') reasonEditor.value = { ...reasonEditor.value, ...blankAudit(), reasonNo: '', dataStamp: null }
  }
  editorMode.value = mode === 'add' ? 'reason-add' : mode === 'edit' ? 'reason-edit' : 'reason-copy'
}
function closeEditor(): void {
  editorMode.value = null
  subtypeSource.value = null
  reasonSource.value = null
  editorError.value = ''
  editorNotice.value = ''
}
function validateSubtype(): string {
  const row = subtypeEditor.value
  if (!row.reasonSubType.trim()) return '原因子類別為必填欄位。'
  if (!row.reasonSubTypeName.trim()) return '原因子類別名稱為必填欄位。'
  if (!reasonTypes.some(option => option.value === Number(row.reasonType))) return '原因類別必須為 1 至 5。'
  if (row.reasonSubType.trim().length > 50) return '原因子類別最多 50 字。'
  if (row.reasonSubTypeName.trim().length > 100) return '原因子類別名稱最多 100 字。'
  if (row.description.length > 255) return '說明最多 255 字。'
  return ''
}
function validateReason(): string {
  const row = reasonEditor.value
  if (!row.reasonNo.trim()) return '原因編號為必填欄位。'
  if (!row.reasonName.trim()) return '原因名稱為必填欄位。'
  if (!reasonTypes.some(option => String(option.value) === row.reasonType)) return '原因類別必須為 1 至 5。'
  if (String(row.reasonLevel).trim() === '' || !Number.isInteger(Number(row.reasonLevel)) || Number(row.reasonLevel) < 0 || Number(row.reasonLevel) > 9) return '原因層級必須為 0 至 9 的整數。'
  if (row.reasonNo.trim().length > 55) return '原因編號最多 55 字。'
  if (row.reasonName.trim().length > 100) return '原因名稱最多 100 字。'
  if (row.description.length > 255) return '說明最多 255 字。'
  return ''
}
async function refreshSelection(reasonSubType: string): Promise<void> {
  await loadSubtypeRows()
  const row = allSubtypes.value.find(item => item.reasonSubType === reasonSubType)
  if (row && row.issueState === issueState.value) {
    selectedSubtypeNo.value = row.reasonSubType
    selectedSubtypeNos.value = [row.reasonSubType]
    await loadReasons(row.reasonSubType)
  } else {
    selectedSubtypeNo.value = ''
    selectedSubtypeNos.value = []
    reasons.value = []
  }
}
async function saveSubtype(keepOpen: boolean): Promise<void> {
  editorError.value = validateSubtype()
  editorNotice.value = ''
  if (editorError.value) return
  const mode = editorMode.value
  const original = subtypeSource.value
  const key = subtypeEditor.value.reasonSubType.trim().toLocaleUpperCase()
  const payload = {
    reasonType: Number(subtypeEditor.value.reasonType),
    reasonSubType: key,
    reasonSubTypeName: subtypeEditor.value.reasonSubTypeName.trim().toLocaleUpperCase(),
    description: subtypeEditor.value.description.trim() || null,
  }
  editorBusy.value = true
  try {
    if (useMock.value) {
      const store = readMock()
      if (mode === 'subtype-edit' && original) {
        const index = store.subtypes.findIndex(row => row.reasonSubType === original.reasonSubType)
        if (index < 0) throw new Error('資料已不存在，請重新查詢。')
        store.subtypes[index] = { ...store.subtypes[index], ...payload, reviser: props.currentUserNo || 'DEV001', reviseDate: now(), dataStamp: (store.subtypes[index].dataStamp ?? 0) + 1 }
      } else {
        if (store.subtypes.some(row => row.reasonSubType === key)) throw new Error('原因子類別已存在。')
        store.subtypes.push({ ...payload, issueState: 0, creator: props.currentUserNo || 'DEV001', createDate: now(), reviser: null, reviseDate: null, dataStamp: 0 })
        if (keepOpen) editorMode.value = 'subtype-edit'
      }
      writeMock(store)
    } else if (mode === 'subtype-edit' && original) {
      await request('api/qc/8d-reason-subtypes/' + enc(original.reasonSubType) + '?' + stampQuery(original.dataStamp), { method: 'PUT', body: JSON.stringify(payload) })
    } else {
      await request('api/qc/8d-reason-subtypes', { method: 'POST', body: JSON.stringify(payload) })
      if (keepOpen) editorMode.value = 'subtype-edit'
    }
    await refreshSelection(key)
    const saved = allSubtypes.value.find(row => row.reasonSubType === key)
    if (saved) {
      subtypeSource.value = structuredClone(saved)
      subtypeEditor.value = { ...saved, reasonSubTypeName: saved.reasonSubTypeName || '', description: saved.description || '', creator: saved.creator || '', createDate: saved.createDate || '', reviser: saved.reviser || '', reviseDate: saved.reviseDate || '' }
    }
    notice.value = '原因子類別已儲存。'
    if (keepOpen) editorNotice.value = '儲存完成。'
    else closeEditor()
  } catch (error) {
    editorError.value = error instanceof Error ? error.message : '儲存原因子類別失敗。'
  } finally {
    editorBusy.value = false
  }
}
async function saveReason(keepOpen: boolean): Promise<void> {
  editorError.value = validateReason()
  editorNotice.value = ''
  if (editorError.value) return
  const mode = editorMode.value
  const original = reasonSource.value
  const reasonNo = reasonEditor.value.reasonNo.trim().toLocaleUpperCase()
  const payload = {
    reasonNo,
    reasonName: reasonEditor.value.reasonName.trim(),
    reasonType: String(reasonEditor.value.reasonType),
    reasonSubType: reasonEditor.value.reasonSubType,
    reasonLevel: Number(reasonEditor.value.reasonLevel),
    description: reasonEditor.value.description.trim() || null,
  }
  editorBusy.value = true
  try {
    if (useMock.value) {
      const store = readMock()
      if (mode === 'reason-edit' && original) {
        const index = store.reasons.findIndex(row => row.reasonNo === original.reasonNo)
        if (index < 0) throw new Error('資料已不存在，請重新查詢。')
        store.reasons[index] = { ...store.reasons[index], ...payload, reviser: props.currentUserNo || 'DEV001', reviseDate: now(), dataStamp: (store.reasons[index].dataStamp ?? 0) + 1 }
      } else {
        if (store.reasons.some(row => row.reasonNo === reasonNo)) throw new Error('原因編號已存在。')
        store.reasons.push({ ...payload, creator: props.currentUserNo || 'DEV001', createDate: now(), reviser: null, reviseDate: null, dataStamp: 0 })
        if (keepOpen) editorMode.value = 'reason-edit'
      }
      writeMock(store)
    } else if (mode === 'reason-edit' && original) {
      await request('api/qc/8d-reasons/' + enc(original.reasonNo) + '?' + stampQuery(original.dataStamp), { method: 'PUT', body: JSON.stringify(payload) })
    } else {
      await request('api/qc/8d-reasons', { method: 'POST', body: JSON.stringify(payload) })
      if (keepOpen) editorMode.value = 'reason-edit'
    }
    await loadReasons(payload.reasonSubType)
    const saved = reasons.value.find(row => row.reasonNo === reasonNo)
    if (saved) {
      reasonSource.value = structuredClone(saved)
      reasonEditor.value = { ...saved, description: saved.description || '', creator: saved.creator || '', createDate: saved.createDate || '', reviser: saved.reviser || '', reviseDate: saved.reviseDate || '' }
    }
    selectedReasonNos.value = [reasonNo]
    notice.value = '原因明細已儲存。'
    if (keepOpen) editorNotice.value = '儲存完成。'
    else closeEditor()
  } catch (error) {
    editorError.value = error instanceof Error ? error.message : '儲存原因明細失敗。'
  } finally {
    editorBusy.value = false
  }
}
async function saveEditor(keepOpen: boolean): Promise<void> {
  if (editorMode.value?.startsWith('subtype-')) await saveSubtype(keepOpen)
  else if (editorMode.value?.startsWith('reason-')) await saveReason(keepOpen)
}
async function deleteSubtypes(): Promise<void> {
  const rows = selectedSubtypes.value
  if (!rows.length) { errorMessage.value = '請選取要刪除的原因子類別。'; return }
  if (rows.some(row => row.issueState === 1)) { errorMessage.value = '簽核中的原因子類別不可刪除。'; return }
  if (!window.confirm('確定刪除選取的 ' + rows.length + ' 筆原因子類別？已簽核資料會標為不使用；未簽核/不使用資料會實體刪除。')) return
  busy.value = true
  errorMessage.value = ''
  notice.value = ''
  let completed = 0
  try {
    if (useMock.value) {
      const store = readMock()
      for (const row of rows) {
        if (row.issueState === 2) {
          const index = store.subtypes.findIndex(item => item.reasonSubType === row.reasonSubType)
          if (index >= 0) store.subtypes[index] = { ...store.subtypes[index], issueState: -1, reviser: props.currentUserNo || 'DEV001', reviseDate: now(), dataStamp: (store.subtypes[index].dataStamp ?? 0) + 1 }
        } else {
          store.subtypes = store.subtypes.filter(item => item.reasonSubType !== row.reasonSubType)
        }
        completed++
      }
      writeMock(store)
    } else {
      for (const row of rows) {
        await request('api/qc/8d-reason-subtypes/' + enc(row.reasonSubType) + '?issueState=' + row.issueState + '&' + stampQuery(row.dataStamp), { method: 'DELETE' })
        completed++
      }
    }
    await query()
    notice.value = '已完成 ' + completed + ' 筆原因子類別刪除/停用。原因明細依 WinForms/Core 行為不隨子類別刪除。'
  } catch (error) {
    await query()
    errorMessage.value = (error instanceof Error ? error.message : '刪除失敗。') + (completed ? '；先前已完成 ' + completed + ' 筆，舊系統為逐筆執行。' : '')
  } finally {
    busy.value = false
  }
}
async function approveSubtypes(): Promise<void> {
  const rows = selectedSubtypes.value
  if (!rows.length) { errorMessage.value = '請選取要核准的原因子類別。'; return }
  if (rows.some(row => row.issueState !== 0)) { errorMessage.value = '只有未簽核的原因子類別可以送出核准。'; return }
  if (!window.confirm('確定送出 ' + rows.length + ' 筆原因子類別核准？')) return
  busy.value = true
  errorMessage.value = ''
  notice.value = ''
  let completed = 0
  try {
    if (useMock.value) {
      const store = readMock()
      for (const row of rows) {
        const index = store.subtypes.findIndex(item => item.reasonSubType === row.reasonSubType)
        if (index >= 0) store.subtypes[index] = { ...store.subtypes[index], issueState: 2, reviser: props.currentUserNo || 'DEV001', reviseDate: now(), dataStamp: (store.subtypes[index].dataStamp ?? 0) + 1 }
        completed++
      }
      writeMock(store)
    } else {
      for (const row of rows) {
        await request('api/qc/8d-reason-subtypes/' + enc(row.reasonSubType) + '/approve', {
          method: 'POST',
          body: JSON.stringify({ issueState: row.issueState, dataStamp: row.dataStamp, creator: props.currentUserNo || '' }),
        })
        completed++
      }
    }
    await query()
    notice.value = '已送出 ' + completed + ' 筆核准；無簽核工作流程時直接轉為已簽核，有簽核流程時由 API 建立簽核並回傳簽核中。'
  } catch (error) {
    await query()
    errorMessage.value = (error instanceof Error ? error.message : '核准失敗。') + (completed ? '；先前已完成 ' + completed + ' 筆。' : '')
  } finally {
    busy.value = false
  }
}
async function deleteReasons(): Promise<void> {
  if (!canDeleteReasons.value) { errorMessage.value = '只有未簽核子類別下的原因明細可以刪除。'; return }
  const rows = visibleReasons.value.filter(row => selectedReasonNos.value.includes(row.reasonNo))
  if (!rows.length) { errorMessage.value = '請選取要刪除的原因明細。'; return }
  if (!window.confirm('確定刪除選取的 ' + rows.length + ' 筆原因明細？')) return
  busy.value = true
  errorMessage.value = ''
  notice.value = ''
  let completed = 0
  try {
    if (useMock.value) {
      const store = readMock()
      store.reasons = store.reasons.filter(row => !selectedReasonNos.value.includes(row.reasonNo))
      completed = rows.length
      writeMock(store)
      reasons.value = store.reasons.filter(row => row.reasonSubType === selectedSubtypeNo.value)
    } else {
      for (const row of rows) {
        await request('api/qc/8d-reasons/' + enc(row.reasonNo), { method: 'DELETE' })
        completed++
      }
      await loadReasons(selectedSubtypeNo.value)
    }
    selectedReasonNos.value = []
    notice.value = '已刪除 ' + completed + ' 筆原因明細。'
  } catch (error) {
    await loadReasons(selectedSubtypeNo.value)
    errorMessage.value = (error instanceof Error ? error.message : '刪除原因明細失敗。') + (completed ? '；先前已完成 ' + completed + ' 筆，舊系統為逐筆執行。' : '')
  } finally {
    busy.value = false
  }
}

onMounted(() => { void query() })
</script>

<template>
  <main class="qc8d-page">
    <header class="page-heading">
      <div>
        <h1>8D 原因基本資料</h1>
        <p>原因子類別與原因明細</p>
      </div>
      <span v-if="useMock" class="dev-badge">開發假資料</span>
    </header>

    <section class="query-panel" aria-label="查詢條件">
      <div class="state-options" role="radiogroup" aria-label="簽核狀態">
        <label v-for="state in issueStates" :key="state.value" class="state-option">
          <input v-model="issueState" type="radio" :value="state.value">
          <span>{{ state.label }}</span>
        </label>
      </div>
      <label class="search-field">
        <span>原因子類別</span>
        <input v-model.trim="searchText" type="search" maxlength="50" @keydown.enter.prevent="query">
      </label>
      <button type="button" class="button query-button" :disabled="busy" @click="query">查詢</button>
    </section>

    <p v-if="errorMessage" class="message error-message" role="alert">{{ errorMessage }}</p>
    <p v-if="notice" class="message notice-message" role="status">{{ notice }}</p>

    <section class="grid-group subtype-group">
      <div class="group-caption">8D ReasonSubType</div>
      <div class="grid-layout">
        <div class="table-scroll">
          <table class="data-grid subtype-grid">
            <thead>
              <tr>
                <th class="check-column"></th>
                <th>Reason Type</th>
                <th>Reason SubType</th>
                <th>Reason SubType Name</th>
                <th>Description</th>
                <th>Creator</th>
                <th>Create Date</th>
                <th>Reviser</th>
                <th>Revise Date</th>
                <th>狀態</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="row in visibleSubtypes"
                :key="row.reasonSubType"
                :class="{ 'active-row': selectedRow(row) }"
                @click="selectSubtype(row)"
              >
                <td class="check-column"><input type="checkbox" :checked="subtypeChecked(row)" aria-label="選取原因子類別" @click.stop="toggleSubtype(row)"></td>
                <td>{{ typeText(row.reasonType) }}</td>
                <td>{{ row.reasonSubType }}</td>
                <td>{{ row.reasonSubTypeName }}</td>
                <td class="description-cell">{{ row.description }}</td>
                <td>{{ row.creator }}</td>
                <td>{{ dateText(row.createDate) }}</td>
                <td>{{ row.reviser }}</td>
                <td>{{ dateText(row.reviseDate) }}</td>
                <td><span class="status-pill" :class="statusClass(row.issueState)">{{ statusText(row.issueState) }}</span></td>
              </tr>
              <tr v-if="!loading && visibleSubtypes.length === 0">
                <td colspan="10" class="empty-row">查無原因子類別資料</td>
              </tr>
              <tr v-if="loading">
                <td colspan="10" class="empty-row">資料載入中…</td>
              </tr>
            </tbody>
          </table>
        </div>
        <aside class="action-rail" aria-label="原因子類別功能">
          <button type="button" class="button primary" :disabled="busy" @click="beginSubtype('add')">新增(A)</button>
          <button type="button" class="button" :disabled="busy || selectedSubtypes.length !== 1 || selectedSubtypes[0]?.issueState !== 0" @click="beginSubtype('edit')">修改(E)</button>
          <button type="button" class="button" :disabled="busy || selectedSubtypes.length !== 1" @click="beginSubtype('copy')">複製(C)</button>
          <button type="button" class="button" :disabled="busy || !selectedSubtypes.length || selectedSubtypes.some(row => row.issueState === 1)" @click="deleteSubtypes">刪除(D)</button>
          <button type="button" class="button" :disabled="busy || !selectedSubtypes.length || selectedSubtypes.some(row => row.issueState !== 0)" @click="approveSubtypes">核准</button>
        </aside>
      </div>
      <footer class="grid-footer">資料總筆數：{{ visibleSubtypes.length }}</footer>
    </section>

    <section class="grid-group reason-group">
      <div class="group-caption">8D ReasonBasis{{ activeSubtype ? ' — ' + activeSubtype.reasonSubType : '' }}</div>
      <div class="grid-layout">
        <div class="table-scroll">
          <table class="data-grid reason-grid">
            <thead>
              <tr>
                <th class="check-column"></th>
                <th>Reason No</th>
                <th>Reason Name</th>
                <th>Reason Level</th>
                <th>Reason Type</th>
                <th>Description</th>
                <th>Creator</th>
                <th>Create Date</th>
                <th>Reviser</th>
                <th>Revise Date</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="row in visibleReasons"
                :key="row.reasonNo"
                :class="{ 'active-row': selectedReasonNos.includes(row.reasonNo) }"
                @click="toggleReason(row)"
              >
                <td class="check-column"><input type="checkbox" :checked="reasonChecked(row)" aria-label="選取原因明細" @click.stop="toggleReason(row)"></td>
                <td>{{ row.reasonNo }}</td>
                <td>{{ row.reasonName }}</td>
                <td>{{ row.reasonLevel }}</td>
                <td>{{ typeText(row.reasonType) }}</td>
                <td class="description-cell">{{ row.description }}</td>
                <td>{{ row.creator }}</td>
                <td>{{ dateText(row.createDate) }}</td>
                <td>{{ row.reviser }}</td>
                <td>{{ dateText(row.reviseDate) }}</td>
              </tr>
              <tr v-if="!loading && !selectedSubtypeNo">
                <td colspan="10" class="empty-row">請先選取上方原因子類別</td>
              </tr>
              <tr v-else-if="!loading && visibleReasons.length === 0">
                <td colspan="10" class="empty-row">此原因子類別尚無明細</td>
              </tr>
              <tr v-if="loading && selectedSubtypeNo">
                <td colspan="10" class="empty-row">資料載入中…</td>
              </tr>
            </tbody>
          </table>
        </div>
        <aside class="action-rail" aria-label="原因明細功能">
          <button type="button" class="button primary" :disabled="busy || !detailEditable" @click="beginReason('add')">新增(A)</button>
          <button type="button" class="button" :disabled="busy || !detailEditable || selectedReasonNos.length !== 1" @click="beginReason('edit')">修改(E)</button>
          <button type="button" class="button" :disabled="busy || !detailEditable || selectedReasonNos.length !== 1" @click="beginReason('copy')">複製(C)</button>
          <button type="button" class="button" :disabled="busy || !canDeleteReasons || !selectedReasonNos.length" @click="deleteReasons">刪除(D)</button>
          <button type="button" class="button close-button" @click="emit('close')">關閉</button>
        </aside>
      </div>
      <footer class="grid-footer">資料總筆數：{{ visibleReasons.length }}<span v-if="activeSubtype && !detailEditable" class="readonly-hint">已簽核/簽核中/不使用的子類別不可維護原因明細</span></footer>
    </section>

    <div v-if="editorMode" class="dialog-backdrop" role="presentation" @click.self="closeEditor">
      <section class="editor-dialog" role="dialog" aria-modal="true" :aria-label="editorTitle">
        <header class="dialog-titlebar">
          <h2>{{ editorTitle }}</h2>
          <button type="button" class="icon-close" aria-label="關閉" @click="closeEditor">×</button>
        </header>
        <form class="editor-form" @submit.prevent="saveEditor(false)">
          <div v-if="editorMode.startsWith('subtype-')" class="form-grid">
            <label class="form-field"><span>原因子類別<span class="required">*</span></span><input v-model.trim="subtypeEditor.reasonSubType" maxlength="50" :readonly="editorMode === 'subtype-edit'" @blur="subtypeEditor.reasonSubType = subtypeEditor.reasonSubType.toLocaleUpperCase()"></label>
            <label class="form-field"><span>原因類別<span class="required">*</span></span><select v-model.number="subtypeEditor.reasonType"><option v-for="item in reasonTypes" :key="item.value" :value="item.value">{{ item.value }} — {{ item.label }}</option></select></label>
            <label class="form-field"><span>原因子類別名稱<span class="required">*</span></span><input v-model.trim="subtypeEditor.reasonSubTypeName" maxlength="100" @blur="subtypeEditor.reasonSubTypeName = subtypeEditor.reasonSubTypeName.toLocaleUpperCase()"></label>
            <label class="form-field wide-field"><span>說明</span><textarea v-model="subtypeEditor.description" maxlength="255" rows="4"></textarea></label>
          </div>
          <div v-else class="form-grid">
            <label class="form-field"><span>原因編號<span class="required">*</span></span><input v-model.trim="reasonEditor.reasonNo" maxlength="55" :readonly="editorMode === 'reason-edit'" @blur="reasonEditor.reasonNo = reasonEditor.reasonNo.toLocaleUpperCase()"></label>
            <label class="form-field"><span>原因名稱<span class="required">*</span></span><input v-model.trim="reasonEditor.reasonName" maxlength="100"></label>
            <label class="form-field"><span>原因類別<span class="required">*</span></span><select v-model="reasonEditor.reasonType" disabled><option v-for="item in reasonTypes" :key="item.value" :value="String(item.value)">{{ item.value }} — {{ item.label }}</option></select></label>
            <label class="form-field"><span>原因子類別</span><input v-model="reasonEditor.reasonSubType" readonly></label>
            <label class="form-field"><span>原因層級<span class="required">*</span>（0–9）</span><input v-model.number="reasonEditor.reasonLevel" type="number" min="0" max="9" step="1"></label>
            <label class="form-field wide-field"><span>說明</span><textarea v-model="reasonEditor.description" maxlength="255" rows="4"></textarea></label>
          </div>
          <fieldset class="audit-panel">
            <legend>建立/修改資訊</legend>
            <label class="form-field"><span>建立人</span><input :value="editorAudit.creator" readonly></label>
            <label class="form-field"><span>建立日期</span><input :value="dateText(editorAudit.createDate)" readonly></label>
            <label class="form-field"><span>修改人</span><input :value="editorAudit.reviser" readonly></label>
            <label class="form-field"><span>修改日期</span><input :value="dateText(editorAudit.reviseDate)" readonly></label>
          </fieldset>
          <p v-if="editorError" class="message error-message" role="alert">{{ editorError }}</p>
          <p v-if="editorNotice" class="message notice-message" role="status">{{ editorNotice }}</p>
          <footer class="dialog-actions">
            <button type="submit" class="button primary" :disabled="editorBusy">確定</button>
            <button type="button" class="button" :disabled="editorBusy" @click="closeEditor">關閉</button>
            <button type="button" class="button" :disabled="editorBusy" @click="saveEditor(true)">套用</button>
          </footer>
        </form>
      </section>
    </div>
  </main>
</template>

<style scoped>
.qc8d-page {
  --kd-line: #c8c4bb;
  --kd-text: #30302e;
  --kd-muted: #77746e;
  --kd-field: #fff0ad;
  --kd-button: #f7a32b;
  --kd-button-dark: #df8612;
  display: flex;
  flex-direction: column;
  gap: 8px;
  height: 100%;
  min-height: 0;
  padding: 12px;
  color: var(--kd-text);
  background: #f7f5ef;
  font: 14px/1.35 "Segoe UI", "Microsoft JhengHei", sans-serif;
}
.page-heading { display: flex; align-items: center; justify-content: space-between; min-height: 42px; }
.page-heading h1 { margin: 0; font-size: 20px; font-weight: 600; }
.page-heading p { margin: 2px 0 0; color: var(--kd-muted); font-size: 12px; }
.dev-badge { padding: 4px 9px; border: 1px solid #d7a044; border-radius: 3px; background: #fff1bd; color: #77551a; font-size: 12px; }
.query-panel { display: flex; flex-wrap: wrap; align-items: center; gap: 12px; min-height: 46px; padding: 6px 10px; border: 1px solid var(--kd-line); background: #fff; }
.state-options { display: flex; flex-wrap: wrap; align-items: center; gap: 12px; }
.state-option { display: inline-flex; align-items: center; gap: 4px; white-space: nowrap; cursor: pointer; }
.state-option input { accent-color: #e18b17; }
.search-field { display: inline-flex; align-items: center; gap: 8px; }
.search-field span { white-space: nowrap; }
.search-field input { width: 190px; }
input, select, textarea { box-sizing: border-box; min-height: 30px; padding: 5px 7px; border: 1px solid #a9a69f; border-radius: 2px; background: var(--kd-field); color: var(--kd-text); font: inherit; }
input:focus, select:focus, textarea:focus { border-color: #d18a21; outline: 2px solid rgb(209 138 33 / 20%); }
input[readonly], input:disabled, select:disabled { background: #ecebe7; color: #777; }
textarea { resize: vertical; }
.button { min-height: 30px; padding: 4px 11px; border: 1px solid #bdb9b0; border-radius: 3px; background: linear-gradient(#fff, #f0efec); color: #343331; font: inherit; white-space: nowrap; cursor: pointer; }
.button:hover:not(:disabled) { border-color: #d08a23; background: #fff6df; }
.button:disabled { opacity: .48; cursor: not-allowed; }
.button.primary { border-color: var(--kd-button-dark); background: var(--kd-button); color: #fff; }
.button.primary:hover:not(:disabled) { background: var(--kd-button-dark); }
.query-button { min-width: 58px; }
.message { flex: 0 0 auto; margin: 0; padding: 7px 10px; border: 1px solid; border-radius: 2px; }
.error-message { border-color: #d99b86; background: #fff0e9; color: #873820; }
.notice-message { border-color: #c9bc91; background: #fff8dc; color: #67551d; }
.grid-group { display: flex; flex-direction: column; min-height: 0; padding: 6px; border: 1px solid var(--kd-line); background: #fff; }
.subtype-group { flex: 0 0 41%; min-height: 220px; }
.reason-group { flex: 1 1 auto; min-height: 210px; }
.group-caption { margin: -1px 0 6px; padding: 0 3px; color: #59554c; font-weight: 600; }
.grid-layout { display: flex; flex: 1 1 auto; min-height: 0; gap: 8px; }
.table-scroll { flex: 1 1 auto; min-width: 0; overflow: auto; border: 1px solid #c5c1b9; }
.data-grid { width: 100%; border-collapse: separate; border-spacing: 0; font-size: 13px; white-space: nowrap; }
.data-grid th { position: sticky; top: 0; z-index: 1; padding: 6px 7px; border-right: 1px solid #d4d0c8; border-bottom: 1px solid #bcb8af; background: #eee9de; color: #3f3c36; text-align: left; font-weight: 600; }
.data-grid td { height: 29px; padding: 4px 7px; border-right: 1px solid #e1ded8; border-bottom: 1px solid #e1ded8; }
.data-grid tbody tr { cursor: pointer; }
.data-grid tbody tr:hover { background: #fff8e3; }
.data-grid tbody tr.active-row { background: #ffedaa; }
.data-grid input[type="checkbox"] { width: 15px; min-height: 15px; height: 15px; margin: 0; accent-color: #d98717; vertical-align: middle; }
.check-column { width: 28px; min-width: 28px; text-align: center !important; }
.description-cell { max-width: 260px; overflow: hidden; text-overflow: ellipsis; }
.empty-row { height: 48px !important; color: #777; text-align: center; cursor: default; }
.action-rail { display: flex; flex: 0 0 88px; flex-direction: column; gap: 7px; }
.action-rail .button { min-height: 32px; }
.action-rail .close-button { margin-top: auto; }
.grid-footer { display: flex; align-items: center; gap: 14px; min-height: 20px; padding: 3px 2px 0; color: var(--kd-muted); font-size: 12px; }
.readonly-hint { color: #865b1b; }
.status-pill { display: inline-block; min-width: 48px; padding: 2px 6px; border: 1px solid #d5c8a3; border-radius: 10px; background: #fff6d9; text-align: center; font-size: 11px; }
.issue-state-0 { color: #795b16; }
.issue-state-1 { border-color: #d9bb79; background: #fff0c4; color: #775315; }
.issue-state-2 { border-color: #a9c6a1; background: #eaf5e7; color: #315c2b; }
.issue-state-minus-1 { border-color: #c9c5bd; background: #efeeeb; color: #65615a; }
.dialog-backdrop { position: fixed; z-index: 50; inset: 0; display: flex; align-items: center; justify-content: center; padding: 18px; background: rgb(28 27 25 / 42%); }
.editor-dialog { display: flex; flex-direction: column; width: min(780px, 96vw); max-height: min(90vh, 760px); overflow: auto; border: 1px solid #b9b4aa; background: #f8f6f1; box-shadow: 0 12px 36px rgb(0 0 0 / 24%); }
.dialog-titlebar { position: sticky; top: 0; z-index: 2; display: flex; align-items: center; justify-content: space-between; padding: 10px 15px; border-bottom: 1px solid #d2cec5; background: #f2efe8; }
.dialog-titlebar h2 { margin: 0; font-size: 17px; font-weight: 600; }
.icon-close { width: 30px; height: 28px; border: 0; background: transparent; color: #4c4943; font-size: 23px; cursor: pointer; }
.editor-form { display: flex; flex-direction: column; gap: 14px; padding: 16px; }
.form-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px 16px; }
.form-field { display: flex; flex-direction: column; gap: 5px; min-width: 0; color: #46433d; }
.form-field > span { font-size: 13px; }
.form-field input, .form-field select, .form-field textarea { width: 100%; }
.wide-field { grid-column: 1 / -1; }
.required { padding-left: 3px; color: #a33a25; }
.audit-panel { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 9px 15px; padding: 11px; border: 1px solid #c9c4ba; }
.audit-panel legend { padding: 0 5px; color: #5a564f; }
.dialog-actions { display: flex; justify-content: flex-end; gap: 8px; padding-top: 7px; border-top: 1px solid #ddd9d1; }
@media (max-width: 900px) {
  .qc8d-page { height: auto; min-height: 100%; }
  .subtype-group { flex-basis: 390px; }
  .reason-group { flex-basis: 390px; }
  .grid-layout { flex-direction: column; }
  .action-rail { flex-basis: auto; flex-direction: row; flex-wrap: wrap; }
  .action-rail .close-button { margin: 0 0 0 auto; }
}
@media (max-width: 600px) {
  .query-panel { align-items: flex-start; }
  .state-options { width: 100%; }
  .form-grid, .audit-panel { grid-template-columns: 1fr; }
  .wide-field { grid-column: auto; }
}
</style>
