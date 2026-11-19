<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'

type IssueState = -1 | 0 | 1 | 2
type EditorMode = 'add' | 'edit' | 'copy' | null
type RuleType = 0 | 1 | 2
interface Option { value: number; label: string }
interface BasisRow {
  ruleNo: string
  componentCollect: number
  issueState: IssueState | null
  creator: string | null
  createDate: string | null
  reviser: string | null
  reviseDate: string | null
  editableFields?: string[]
  version?: string
}
interface RuleDetail {
  ruleNo: string
  action: number
  reasonType: number
  reasonLevel: number
  rejectNum: number | null
  rate: string | null
  ruleType: RuleType | null
  changeAql: string | null
  creator?: string | null
  createDate?: string | null
  reviser?: string | null
  reviseDate?: string | null
}
interface RuleDraft {
  action: number
  reasonType: number
  reasonLevel: number
  rejectNum: string
  rate: string
  ruleType: RuleType | null
  changeAql: string
}
interface MockStore { bases: BasisRow[]; details: RuleDetail[] }
interface SaveResult { basis: BasisRow; details: RuleDetail[] }
type ListPayload<T> = T[] | { items?: T[] }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes-dev-mock:qc-attribute-rule-basis:v1'
const issueStates: { value: IssueState; label: string }[] = [
  { value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' },
]
const demoActions: Option[] = [
  { value: 0, label: 'Warning（DEMO）' }, { value: 1, label: 'Wait（DEMO）' }, { value: 2, label: 'Hold（DEMO）' }, { value: 3, label: 'Alert（DEMO）' },
]
const demoReasonTypes: Option[] = [{ value: 0, label: 'Scrap（DEMO）' }, { value: 1, label: 'Defect（DEMO）' }]
const ruleTypes: { value: RuleType; label: string }[] = [
  { value: 0, label: 'Number' }, { value: 1, label: 'Rate' }, { value: 2, label: 'AQL Reject Number' },
]
const demoAql = ['0.65', '1.0', '1.5', '2.5']
const seedBases: BasisRow[] = [
  { ruleNo: 'DEV-ATT-001', componentCollect: 0, issueState: 0, creator: 'DEV001', createDate: '2026-11-10T08:00:00+08:00', reviser: null, reviseDate: null },
  { ruleNo: 'DEV-ATT-002', componentCollect: 1, issueState: 1, creator: 'DEV001', createDate: '2026-11-10T09:00:00+08:00', reviser: null, reviseDate: null },
  { ruleNo: 'DEV-ATT-003', componentCollect: 0, issueState: 2, creator: 'DEV002', createDate: '2026-11-11T08:00:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-11T10:00:00+08:00' },
  { ruleNo: 'DEV-ATT-004', componentCollect: 1, issueState: -1, creator: 'DEV001', createDate: '2026-11-12T08:00:00+08:00', reviser: 'DEV001', reviseDate: '2026-11-12T10:00:00+08:00' },
]
const seedDetails: RuleDetail[] = [
  { ruleNo: 'DEV-ATT-001', action: 0, reasonType: 0, reasonLevel: -1, rejectNum: 1, rate: null, ruleType: 0, changeAql: null },
  { ruleNo: 'DEV-ATT-001', action: 2, reasonType: 1, reasonLevel: 2, rejectNum: 0, rate: '2.5', ruleType: 1, changeAql: null },
  { ruleNo: 'DEV-ATT-001', action: 3, reasonType: 0, reasonLevel: 1, rejectNum: 0, rate: null, ruleType: 2, changeAql: '1.0' },
  { ruleNo: 'DEV-ATT-002', action: 1, reasonType: 1, reasonLevel: 0, rejectNum: 2, rate: null, ruleType: 0, changeAql: null },
  { ruleNo: 'DEV-ATT-003', action: 2, reasonType: 0, reasonLevel: 3, rejectNum: 0, rate: '1.0', ruleType: 1, changeAql: null },
]

const bases = ref<BasisRow[]>([])
const detailRows = ref<RuleDetail[]>([])
const actionOptions = ref<Option[]>([])
const reasonTypeOptions = ref<Option[]>([])
const aqlOptions = ref<string[]>([])
const userDefinedAqlEnabled = ref(false)
const issueState = ref<IssueState>(0)
const searchInput = ref('')
const appliedSearch = ref('')
const selectedRuleNos = ref<string[]>([])
const activeRuleNo = ref('')
const loading = ref(false)
const loadingDetails = ref(false)
const loadedAggregateRuleNo = ref('')
const loadedAggregateVersion = ref<string | undefined>()
const busy = ref(false)
const errorMessage = ref('')
const notice = ref('')
const editorMode = ref<EditorMode>(null)
const sourceBasis = ref<BasisRow | null>(null)
const editor = ref({ ruleNo: '', componentCollect: 0 })
const editorDetails = ref<RuleDetail[]>([])
const editorError = ref('')
const editorNotice = ref('')
const editorDirty = ref(false)
const saving = ref(false)
const createDate = ref('')
const detailDraft = ref<RuleDraft>({ action: 0, reasonType: 0, reasonLevel: -1, rejectNum: '', rate: '', ruleType: 0, changeAql: '' })
const detailEditIndex = ref<number | null>(null)
const detailEditingEnabled = ref(false)
const selectedDetailKeys = ref<string[]>([])
let detailRequestSequence = 0

const visibleBases = computed(() => bases.value
  .filter(row => row.issueState === issueState.value)
  .filter(row => matchesSearch(row.ruleNo, appliedSearch.value))
  .sort((a, b) => a.ruleNo.localeCompare(b.ruleNo)))
const activeBasis = computed(() => bases.value.find(row => row.ruleNo === activeRuleNo.value) ?? null)
const canOpenAggregate = computed(() => !loading.value && !loadingDetails.value && !!activeBasis.value
  && loadedAggregateRuleNo.value === activeRuleNo.value
  && (mock.value || loadedAggregateVersion.value === activeBasis.value.version))
const selectedBases = computed(() => visibleBases.value.filter(row => selectedRuleNos.value.includes(row.ruleNo)))
const visibleDetails = computed(() => [...detailRows.value].sort((a, b) => a.action - b.action || a.reasonType - b.reasonType || a.reasonLevel - b.reasonLevel))
const selectedDetails = computed(() => editorDetails.value.filter(row => selectedDetailKeys.value.includes(detailKey(row))))
const editorTitle = computed(() => '計數判別法則基本資料（' + (editorMode.value === 'add' ? '新增' : editorMode.value === 'copy' ? '複製' : '修改') + '）')
const currentUser = computed(() => props.currentUserNo?.trim() || (mock.value ? 'DEV001' : ''))
const canSave = computed(() => editorMode.value !== null && !saving.value && (editorMode.value !== 'edit' || editableFields().length > 0))
const canEditDetails = computed(() => editorMode.value !== 'edit' || editableFields().includes('ruleDetails'))
const canEditComponentCollect = computed(() => editorMode.value !== 'edit' || editableFields().includes('componentCollect'))
const canDelete = computed(() => selectedBases.value.length > 0 && issueState.value !== 1 && selectedBases.value.every(row => row.issueState === 0 || row.issueState === 2 || row.issueState === -1))
const canApprove = computed(() => !loadingDetails.value && selectedBases.value.length > 0 && issueState.value === 0 && selectedBases.value.every(row => row.issueState === 0))

// API snapshots are JSON DTOs; JSON round trip also unwraps Vue proxies.
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function now(): string { return new Date().toISOString() }
function formatDate(value: string | null | undefined): string {
  if (!value) return ''
  const parsed = new Date(value)
  return Number.isNaN(parsed.getTime()) ? value : parsed.toLocaleString('zh-TW', { hour12: false })
}
function matchesSearch(value: string, pattern: string): boolean {
  if (!pattern) return true
  let escaped = ''
  for (const character of pattern) {
    if (character === '%') escaped += '.*'
    else if (character === '_') escaped += '.'
    else escaped += /[.*+?^{}()|[\]\\]/.test(character) || character === '$' ? '\\' + character : character
  }
  return new RegExp('^' + escaped + '$', 'i').test(value)
}
function listOf<T>(payload: ListPayload<T>): T[] { return Array.isArray(payload) ? payload : payload.items ?? [] }
function readStore(): MockStore {
  try {
    const stored = window.localStorage.getItem(storageKey)
    if (stored) {
      const parsed = JSON.parse(stored) as MockStore
      if (Array.isArray(parsed.bases) && Array.isArray(parsed.details)) return parsed
    }
  } catch { /* reset invalid preview data */ }
  const initial = { bases: clone(seedBases), details: clone(seedDetails) }
  writeStore(initial)
  return initial
}
function writeStore(store: MockStore): void { window.localStorage.setItem(storageKey, JSON.stringify(store)) }
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body && !(init.body instanceof FormData) && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init, headers })
  if (response.status === 401) {
    window.location.assign(appBase + '/')
    throw new Error('登入狀態已逾期，請重新登入。')
  }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; title?: string; message?: string } | null
    throw new Error(problem?.detail || problem?.message || problem?.title || 'API 呼叫失敗（' + response.status + '）。')
  }
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}
function actionLabel(value: number): string { return actionOptions.value.find(option => option.value === value)?.label ?? String(value) }
function reasonTypeLabel(value: number): string { return reasonTypeOptions.value.find(option => option.value === value)?.label ?? String(value) }
function ruleTypeLabel(value: number | null): string { return value === null ? '' : ruleTypes.find(option => option.value === value)?.label ?? String(value) }
function editableFields(): string[] {
  if (editorMode.value !== 'edit' || !sourceBasis.value) return ['componentCollect', 'ruleDetails']
  if (sourceBasis.value.issueState === 0) return ['componentCollect', 'ruleDetails']
  if (sourceBasis.value.issueState === 1) return []
  return sourceBasis.value.editableFields ?? []
}
function detailKey(row: Pick<RuleDetail, 'action' | 'reasonType' | 'reasonLevel'>): string { return row.action + '|' + row.reasonType + '|' + row.reasonLevel }
function focusRule(ruleNo = ''): void {
  const match = visibleBases.value.find(row => row.ruleNo === ruleNo)
  activeRuleNo.value = match?.ruleNo ?? visibleBases.value[0]?.ruleNo ?? ''
  selectedRuleNos.value = match ? [match.ruleNo] : []
}
async function loadDetails(ruleNo: string): Promise<void> {
  const sequence = ++detailRequestSequence
  loadedAggregateRuleNo.value = ''; loadedAggregateVersion.value = undefined
  if (!ruleNo) { detailRows.value = []; loadingDetails.value = false; return }
  if (mock.value) {
    detailRows.value = clone(readStore().details.filter(row => row.ruleNo === ruleNo))
    loadedAggregateRuleNo.value = ruleNo; loadingDetails.value = false
    return
  }
  detailRows.value = []
  loadingDetails.value = true
  try {
    const result = await request<SaveResult>('api/qc/attribute-rule-bases/' + encodeURIComponent(ruleNo))
    if (!result.basis || result.basis.ruleNo !== ruleNo || !Array.isArray(result.details)
      || !result.basis.version || !/^[0-9a-f]{64}$/i.test(result.basis.version)) throw new Error('主檔與明細回應不完整，請重新查詢後再修改。')
    if (sequence === detailRequestSequence && activeRuleNo.value === ruleNo) {
      detailRows.value = result.details
      const index = bases.value.findIndex(row => row.ruleNo === ruleNo)
      if (index >= 0) bases.value[index] = result.basis
      loadedAggregateRuleNo.value = ruleNo; loadedAggregateVersion.value = result.basis.version
    }
  } catch (error) {
    if (sequence === detailRequestSequence) errorMessage.value = error instanceof Error ? error.message : '載入判別法則明細失敗。'
  } finally { if (sequence === detailRequestSequence) loadingDetails.value = false }
}
async function selectBasis(row: BasisRow): Promise<void> {
  selectedRuleNos.value = [row.ruleNo]
  activeRuleNo.value = row.ruleNo
  errorMessage.value = ''
  await loadDetails(row.ruleNo)
}
async function refresh(focusKey = ''): Promise<void> {
  loadedAggregateRuleNo.value = ''; loadedAggregateVersion.value = undefined
  loading.value = true; errorMessage.value = ''
  try {
    if (mock.value) bases.value = clone(readStore().bases)
    else {
      const query = new URLSearchParams({ searchFieldName: 'RuleNo', searchFieldValue: appliedSearch.value })
      bases.value = listOf(await request<ListPayload<BasisRow>>('api/qc/attribute-rule-bases?' + query.toString()))
    }
    focusRule(focusKey || activeRuleNo.value)
    await loadDetails(activeRuleNo.value)
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '載入計數判別法則失敗。' }
  finally { loading.value = false }
}
async function loadOptions(): Promise<void> {
  if (mock.value) {
    actionOptions.value = clone(demoActions)
    reasonTypeOptions.value = clone(demoReasonTypes)
    userDefinedAqlEnabled.value = true
    aqlOptions.value = [...demoAql]
    return
  }
  const results = await Promise.allSettled([
    request<ListPayload<Option>>('api/qc/attribute-rule-bases/options/actions'),
    request<ListPayload<Option>>('api/qc/attribute-rule-bases/options/reason-types'),
    request<{ enabled: boolean; items?: string[] }>('api/qc/attribute-rule-bases/options/aql'),
  ])
  if (results[0].status === 'fulfilled') actionOptions.value = listOf(results[0].value)
  else errorMessage.value = results[0].reason instanceof Error ? results[0].reason.message : '載入 Action 選項失敗。'
  if (results[1].status === 'fulfilled') reasonTypeOptions.value = listOf(results[1].value)
  else errorMessage.value = results[1].reason instanceof Error ? results[1].reason.message : '載入 ReasonType 選項失敗。'
  if (results[2].status === 'fulfilled') {
    userDefinedAqlEnabled.value = results[2].value.enabled
    aqlOptions.value = results[2].value.items ?? []
  } else if (!errorMessage.value) errorMessage.value = results[2].reason instanceof Error ? results[2].reason.message : '載入 AQL 選項失敗。'
}
function query(): void { appliedSearch.value = searchInput.value.trim(); selectedRuleNos.value = []; void refresh() }
function setIssueState(value: IssueState): void {
  issueState.value = value; selectedRuleNos.value = []; focusRule()
  void loadDetails(activeRuleNo.value)
}
function toggleBasis(row: BasisRow): void {
  selectedRuleNos.value = selectedRuleNos.value.includes(row.ruleNo) ? selectedRuleNos.value.filter(key => key !== row.ruleNo) : [...selectedRuleNos.value, row.ruleNo]
  activeRuleNo.value = row.ruleNo
  errorMessage.value = ''
  void loadDetails(row.ruleNo)
}
function toggleDetail(row: RuleDetail): void {
  const key = detailKey(row)
  selectedDetailKeys.value = selectedDetailKeys.value.includes(key) ? selectedDetailKeys.value.filter(item => item !== key) : [...selectedDetailKeys.value, key]
}
function selectEditorDetail(row: RuleDetail): void { selectedDetailKeys.value = [detailKey(row)] }
function emptyDraft(): RuleDraft { return { action: actionOptions.value[0]?.value ?? 0, reasonType: reasonTypeOptions.value[0]?.value ?? 0, reasonLevel: -1, rejectNum: '', rate: '', ruleType: 0, changeAql: '' } }
function openAdd(): void {
  editorMode.value = 'add'; sourceBasis.value = null; editor.value = { ruleNo: '', componentCollect: 0 }; editorDetails.value = []
  createDate.value = now(); editorError.value = ''; editorNotice.value = ''; editorDirty.value = false; selectedDetailKeys.value = []; resetDetailDraft()
}
function openEdit(): void {
  if (!activeBasis.value || !canOpenAggregate.value) return
  editorMode.value = 'edit'; sourceBasis.value = clone(activeBasis.value)
  editor.value = { ruleNo: activeBasis.value.ruleNo, componentCollect: activeBasis.value.componentCollect }
  editorDetails.value = clone(detailRows.value); createDate.value = activeBasis.value.createDate ?? ''; editorError.value = ''; editorNotice.value = ''; editorDirty.value = false; selectedDetailKeys.value = []; resetDetailDraft()
}
function openCopy(): void {
  if (!activeBasis.value || !canOpenAggregate.value) return
  editorMode.value = 'copy'; sourceBasis.value = clone(activeBasis.value)
  editor.value = { ruleNo: activeBasis.value.ruleNo, componentCollect: activeBasis.value.componentCollect }
  editorDetails.value = clone(detailRows.value); createDate.value = now(); editorError.value = ''; editorNotice.value = '已複製主檔和明細，請輸入新的 RuleNo。'; editorDirty.value = false; selectedDetailKeys.value = []; resetDetailDraft()
}
async function closeEditor(): Promise<void> {
  if (saving.value) return
  if (editorDirty.value && await requestConfirmation('資料已變更，是否先保存？選擇取消將關閉並放棄尚未保存的變更。')) { await save(false); return }
  dismissEditor()
}
function dismissEditor(): void {
  editorMode.value = null; sourceBasis.value = null; editorError.value = ''; editorNotice.value = ''; selectedDetailKeys.value = []; detailEditIndex.value = null; editorDirty.value = false
}
function resetDetailDraft(): void { detailDraft.value = emptyDraft(); detailEditIndex.value = null; detailEditingEnabled.value = false }
function beginAddDetail(): void {
  if (!canEditDetails.value) return
  resetDetailDraft(); editorError.value = ''; editorNotice.value = ''
  detailEditingEnabled.value = true
}
function changeRuleType(): void {
  if (detailDraft.value.ruleType === 0) { detailDraft.value.rate = ''; detailDraft.value.changeAql = '' }
  else if (detailDraft.value.ruleType === 1) { detailDraft.value.rejectNum = ''; detailDraft.value.changeAql = '' }
  else { detailDraft.value.rejectNum = ''; detailDraft.value.rate = '' }
}
function validateDetail(): string {
  const value = detailDraft.value
  if (value.ruleType === null) return '請明確選擇 RuleType；來源空值不會自動當成 Number。'
  if (!actionOptions.value.some(option => option.value === value.action)) return '請選擇有效的 Action。'
  if (!reasonTypeOptions.value.some(option => option.value === value.reasonType)) return '請選擇有效的 ReasonType。'
  if (!Number.isInteger(value.reasonLevel) || value.reasonLevel < -1 || value.reasonLevel > 9) return 'ReasonLevel 必須是 -1 到 9 的整數。'
  if (value.ruleType === 0) {
    const rejectNum = Number(value.rejectNum)
    if (!value.rejectNum.trim() || !Number.isFinite(rejectNum) || rejectNum < 1 || !Number.isInteger(rejectNum)) return 'Number 規則的 RejectNum 必須是大於等於 1 的整數。'
    if (rejectNum > 9999999999) return 'RejectNum 超過來源 NUMBER(14,4) 可儲存範圍。'
  }
  if (value.ruleType === 1) {
    const rate = Number(value.rate)
    if (!value.rate.trim() || !Number.isFinite(rate) || rate <= 0) return 'Rate 規則必須輸入大於 0 的數值。'
    if (value.rate.length > 10) return 'Rate 不可超過 10 個字元。'
  }
  if (value.ruleType === 2 && !userDefinedAqlEnabled.value) return '目前未啟用自訂 AQL。'
  if (value.changeAql.length > 30) return 'ChangeAQL 不可超過 30 個字元。'
  return ''
}
function saveDetailDraft(): void {
  if (!detailEditingEnabled.value || !canEditDetails.value) return
  editorError.value = validateDetail()
  if (editorError.value) return
  const draft = detailDraft.value
  const row: RuleDetail = {
    ruleNo: editor.value.ruleNo.trim().toUpperCase(), action: draft.action, reasonType: draft.reasonType, reasonLevel: draft.reasonLevel,
    rejectNum: Number(draft.rejectNum) || 0, rate: draft.rate.trim() || null, ruleType: draft.ruleType, changeAql: draft.changeAql.trim() || null,
  }
  const existingIndex = detailEditIndex.value ?? editorDetails.value.findIndex(item => detailKey(item) === detailKey(row))
  if (existingIndex >= 0) editorDetails.value.splice(existingIndex, 1, row)
  else editorDetails.value.push(row)
  selectedDetailKeys.value = [detailKey(row)]; editorError.value = ''; editorNotice.value = '明細已更新於編輯清單；按確定或套用才會保存主檔。'; editorDirty.value = true; resetDetailDraft()
}
function editDetail(row: RuleDetail): void {
  if (!canEditDetails.value) return
  const index = editorDetails.value.findIndex(item => detailKey(item) === detailKey(row))
  if (index < 0) return
  detailEditIndex.value = index
  detailEditingEnabled.value = true
  detailDraft.value = { action: row.action, reasonType: row.reasonType, reasonLevel: row.reasonLevel, rejectNum: String(row.rejectNum ?? ''), rate: row.rate ?? '', ruleType: row.ruleType, changeAql: row.changeAql ?? '' }
  selectedDetailKeys.value = [detailKey(row)]; editorError.value = ''; editorNotice.value = '編輯明細時 Action、ReasonType、ReasonLevel 鍵值固定。'
}
function beginEditSelectedDetail(): void {
  const row = editorDetails.value.find(item => selectedDetailKeys.value.includes(detailKey(item)))
  if (row) editDetail(row)
}
async function removeEditorDetails(): Promise<void> {
  if (!canEditDetails.value) return
  if (!selectedDetails.value.length) { editorError.value = '請先選取要刪除的明細。'; return }
  if (!await requestConfirmation('確定移除選取的 ' + selectedDetails.value.length + ' 筆明細？')) return
  const removing = new Set(selectedDetailKeys.value)
  editorDetails.value = editorDetails.value.filter(row => !removing.has(detailKey(row)))
  selectedDetailKeys.value = []; editorError.value = ''; editorNotice.value = '明細已從編輯清單移除；按確定或套用才會保存。'; editorDirty.value = true; resetDetailDraft()
}
function validateBasis(): string {
  const ruleNo = editor.value.ruleNo.trim().toUpperCase()
  if (!ruleNo) return 'RuleNo 為必填。'
  if (ruleNo.length > 20) return 'RuleNo 不可超過 20 個字元。'
  if (detailEditingEnabled.value) return '請先完成或取消目前編輯中的明細。'
  if (editor.value.componentCollect !== 0 && editor.value.componentCollect !== 1) return 'ComponentCollect 只允許 Yes 或 No。'
  if (editorMode.value !== 'edit' && bases.value.some(row => row.ruleNo.toUpperCase() === ruleNo)) return 'RuleNo 已存在。'
  if (editorMode.value === 'edit' && sourceBasis.value?.issueState === 1) return '簽核中的資料不可修改。'
  if (editorMode.value === 'edit' && editableFields().length === 0) return '此資料狀態目前沒有可修改欄位。'
  return ''
}
function mutationQuery(row: BasisRow): URLSearchParams {
  if (!row.version || !/^[0-9a-f]{64}$/i.test(row.version)) throw new Error('法則版本資料遺失，請重新查詢後再操作。')
  return new URLSearchParams({ issueState: row.issueState === null ? '' : String(row.issueState), version: row.version })
}
async function save(keepOpen: boolean): Promise<void> {
  editorError.value = validateBasis()
  if (editorError.value || !editorMode.value) return
  const mode = editorMode.value
  const original = sourceBasis.value
  const ruleNo = editor.value.ruleNo.trim().toUpperCase()
  const payload = {
    ruleNo, componentCollect: editor.value.componentCollect,
    details: editorDetails.value.map(row => ({ action: row.action, reasonType: row.reasonType, reasonLevel: row.reasonLevel, rejectNum: row.rejectNum, rate: row.rate, ruleType: row.ruleType, changeAql: row.changeAql })),
  }
  saving.value = true
  try {
    let saved: SaveResult
    if (mock.value) {
      const store = readStore()
      if (mode === 'edit') {
        const current = store.bases.find(row => row.ruleNo === original?.ruleNo)
        if (!current) throw new Error('資料已不存在，請重新查詢。')
        if (canEditComponentCollect.value) current.componentCollect = payload.componentCollect
        if (canEditDetails.value) store.details = store.details.filter(row => row.ruleNo !== current.ruleNo).concat(payload.details.map(row => ({ ...row, ruleNo: current.ruleNo })))
        current.reviser = currentUser.value; current.reviseDate = now()
      } else {
        store.bases.push({ ruleNo, componentCollect: payload.componentCollect, issueState: 0, creator: currentUser.value, createDate: createDate.value, reviser: null, reviseDate: null })
        store.details.push(...payload.details.map(row => ({ ...row, ruleNo })))
      }
      writeStore(store)
      saved = { basis: clone(store.bases.find(row => row.ruleNo === ruleNo)!), details: clone(store.details.filter(row => row.ruleNo === ruleNo)) }
    } else if (mode === 'edit') {
      const query = mutationQuery(original!)
      saved = await request<SaveResult>('api/qc/attribute-rule-bases/' + encodeURIComponent(original!.ruleNo) + '?' + query.toString(), { method: 'PUT', body: JSON.stringify(payload) })
    } else saved = await request<SaveResult>('api/qc/attribute-rule-bases', { method: 'POST', body: JSON.stringify(payload) })
    // Hold the committed aggregate/version before any list reload can fail.
    sourceBasis.value = clone(saved.basis)
    editor.value = { ruleNo: saved.basis.ruleNo, componentCollect: saved.basis.componentCollect }
    editorDetails.value = clone(saved.details)
    createDate.value = saved.basis.createDate ?? ''
    editorMode.value = 'edit'
    editorDirty.value = false
    await refresh(saved.basis.ruleNo)
    if (keepOpen) editorNotice.value = '主檔與明細已套用。'
    else dismissEditor()
  } catch (error) { editorError.value = error instanceof Error ? error.message : '儲存計數判別法則失敗。' }
  finally { saving.value = false }
}
async function removeSelected(): Promise<void> {
  const targets = [...selectedBases.value]
  if (!targets.length) { errorMessage.value = '請先選取要刪除的主檔。'; return }
  if (!canDelete.value) { errorMessage.value = '簽核中的資料不可刪除。'; return }
  if (!await requestConfirmation('確定刪除選取的 ' + targets.length + ' 筆主檔及其全部明細？')) return
  busy.value = true; errorMessage.value = ''; notice.value = ''
  let completed = 0; let failureMessage = ''
  try {
    for (const row of targets) {
      if (mock.value) {
        const store = readStore(); store.bases = store.bases.filter(item => item.ruleNo !== row.ruleNo); store.details = store.details.filter(item => item.ruleNo !== row.ruleNo); writeStore(store)
      } else {
        const query = mutationQuery(row)
        await request('api/qc/attribute-rule-bases/' + encodeURIComponent(row.ruleNo) + '?' + query.toString(), { method: 'DELETE' })
      }
      completed++
    }
    notice.value = '已刪除 ' + completed + ' 筆主檔及明細。'
  } catch (error) { failureMessage = (error instanceof Error ? error.message : '刪除失敗。') + (completed ? '（前 ' + completed + ' 筆已完成）' : '') }
  finally { await refresh(); if (failureMessage) errorMessage.value = failureMessage; busy.value = false }
}
async function approveSelected(): Promise<void> {
  const targets = [...selectedBases.value]
  if (!targets.length) { errorMessage.value = '請先選取要核准的主檔。'; return }
  if (!canApprove.value) { errorMessage.value = '只有未簽核資料可送出核准。'; return }
  const store = mock.value ? readStore() : null
  for (const row of targets) {
    const details = mock.value ? store!.details.filter(item => item.ruleNo === row.ruleNo) : row.ruleNo === activeRuleNo.value ? detailRows.value : null
    if (details && details.length === 0) { errorMessage.value = 'RuleNo ' + row.ruleNo + ' 沒有明細，無法核准。'; return }
  }
  if (!await requestConfirmation('確定送出核准選取的 ' + targets.length + ' 筆主檔？')) return
  busy.value = true; errorMessage.value = ''; notice.value = ''
  let completed = 0; let failureMessage = ''
  try {
    for (const row of targets) {
      if (mock.value) {
        const current = store!.bases.find(item => item.ruleNo === row.ruleNo)!
        current.issueState = 2; writeStore(store!)
      } else await request('api/qc/attribute-rule-bases/' + encodeURIComponent(row.ruleNo) + '/approve?' + mutationQuery(row).toString(), { method: 'POST' })
      completed++
    }
    notice.value = mock.value ? '已核准 ' + completed + ' 筆 DEV 預覽資料。' : '已送出 ' + completed + ' 筆；實際狀態依 QCITEM_A RULE DATA 簽核流程更新。'
  } catch (error) { failureMessage = (error instanceof Error ? error.message : '核准失敗。') + (completed ? '（前 ' + completed + ' 筆已完成）' : '') }
  finally { await refresh(); if (failureMessage) errorMessage.value = failureMessage; busy.value = false }
}

onMounted(async () => {
  await loadOptions()
  const optionsError = errorMessage.value
  await refresh()
  if (optionsError) errorMessage.value = optionsError
})
</script>

<template>
  <main class="attribute-rule-page">
    <header class="page-heading"><h1>計數判別法則基本資料</h1><span v-if="mock" class="dev-badge">DEV 假資料</span></header>
    <section class="query-panel" aria-label="查詢條件">
      <div class="states" role="radiogroup" aria-label="簽核狀態"><label v-for="state in issueStates" :key="state.value" class="state-option"><input type="radio" name="att-rule-state" :value="state.value" :checked="issueState === state.value" @change="setIssueState(state.value)"><span>{{ state.label }}</span></label></div>
      <label for="att-rule-search">RuleNo</label><input id="att-rule-search" v-model="searchInput" class="search-input" maxlength="50" @keydown.enter="query"><span class="wildcard">%</span>
      <button type="button" class="button" :disabled="loading" @click="query">查詢</button>
    </section>
    <section class="master-panel" aria-label="判別法則主檔清單">
      <div class="grid-wrap"><table class="data-grid"><thead><tr><th class="select-col" aria-label="選取"></th><th>RuleNo</th><th>ComponentCollect</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead><tbody>
        <tr v-for="row in visibleBases" :key="row.ruleNo" :class="{ active: activeRuleNo === row.ruleNo }" @click="selectBasis(row)"><td class="select-col"><input type="checkbox" :checked="selectedRuleNos.includes(row.ruleNo)" :aria-label="'選取 ' + row.ruleNo" @click.stop @change="toggleBasis(row)"></td><td>{{ row.ruleNo }}</td><td>{{ row.componentCollect === 1 ? 'Yes' : 'No' }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td></tr>
        <tr v-if="!visibleBases.length"><td colspan="7" class="empty-cell">{{ loading ? '載入中…' : '查無資料' }}</td></tr>
      </tbody></table></div>
      <div class="action-column"><button type="button" class="button primary" :disabled="busy" @click="openAdd">新增(A)</button><button type="button" class="button" :disabled="busy || !canOpenAggregate" @click="openEdit">修改(E)</button><button type="button" class="button" :disabled="busy || !canDelete" @click="removeSelected">刪除(D)</button><button type="button" class="button" :disabled="busy || !canOpenAggregate" @click="openCopy">複製(C)</button><button type="button" class="button" :disabled="busy || !canApprove" @click="approveSelected">核准(Y)</button><button type="button" class="button close-button" @click="emit('close')">關閉(X)</button></div>
    </section>
    <footer class="list-footer">主檔資料總筆數 {{ visibleBases.length }}<span v-if="activeBasis">　目前明細 {{ visibleDetails.length }} 筆</span><span v-if="loadingDetails">　明細載入中…</span></footer>
    <section class="detail-panel" aria-label="所選主檔明細">
      <header><h2>判別法則明細<span v-if="activeBasis"> — {{ activeBasis.ruleNo }}</span></h2></header>
      <div class="detail-grid"><table class="data-grid"><thead><tr><th>Action</th><th>ReasonType</th><th>ReasonLevel</th><th>RejectNum</th><th>Rate</th><th>RuleType</th><th v-if="userDefinedAqlEnabled">ChangeAQL</th></tr></thead><tbody>
        <tr v-for="row in visibleDetails" :key="detailKey(row)"><td>{{ actionLabel(row.action) }}</td><td>{{ reasonTypeLabel(row.reasonType) }}</td><td>{{ row.reasonLevel }}</td><td>{{ row.rejectNum }}</td><td>{{ row.rate }}</td><td>{{ ruleTypeLabel(row.ruleType) }}</td><td v-if="userDefinedAqlEnabled">{{ row.changeAql }}</td></tr>
        <tr v-if="!visibleDetails.length"><td :colspan="userDefinedAqlEnabled ? 7 : 6" class="empty-cell">{{ loadingDetails ? '載入中…' : activeBasis ? '此主檔沒有明細' : '請選取主檔查看明細' }}</td></tr>
      </tbody></table></div>
    </section>
    <p v-if="errorMessage" class="message error" role="alert">{{ errorMessage }}</p><p v-if="notice" class="message notice" role="status">{{ notice }}</p>

    <div v-if="editorMode" class="backdrop" @click.self="closeEditor"><section class="dialog editor-dialog" role="dialog" aria-modal="true" :aria-label="editorTitle">
      <header class="titlebar"><h2>{{ editorTitle }}</h2><button class="x" type="button" aria-label="關閉" :disabled="saving" @click="closeEditor">×</button></header>
      <form class="editor-form" @submit.prevent="save(false)">
        <fieldset class="basis-fields" :disabled="saving"><legend>主檔資料</legend>
          <label class="field"><span>RuleNo</span><input v-model="editor.ruleNo" maxlength="20" :readonly="editorMode === 'edit'" required @input="editorDirty = true" @blur="editor.ruleNo = editor.ruleNo.toUpperCase()"></label>
          <div class="field component-collect"><span>ComponentCollect</span><label><input v-model.number="editor.componentCollect" type="radio" :value="1" :disabled="!canEditComponentCollect" @change="editorDirty = true"> Yes</label><label><input v-model.number="editor.componentCollect" type="radio" :value="0" :disabled="!canEditComponentCollect" @change="editorDirty = true"> No</label></div>
        </fieldset>
        <section class="detail-editor" :class="{ locked: !canEditDetails }"><header class="detail-editor-heading"><h3>Rule Detail</h3><span>{{ editorDetails.length }} 筆</span></header>
          <div class="editor-detail-grid"><table class="data-grid"><thead><tr><th class="select-col"></th><th>Action</th><th>ReasonType</th><th>ReasonLevel</th><th>RejectNum</th><th>Rate</th><th>RuleType</th><th v-if="userDefinedAqlEnabled">ChangeAQL</th></tr></thead><tbody>
            <tr v-for="row in editorDetails" :key="detailKey(row)" :class="{ active: selectedDetailKeys.includes(detailKey(row)) }" @click="selectEditorDetail(row)"><td class="select-col"><input type="checkbox" :checked="selectedDetailKeys.includes(detailKey(row))" :aria-label="'選取明細 ' + detailKey(row)" @click.stop @change="toggleDetail(row)"></td><td>{{ actionLabel(row.action) }}</td><td>{{ reasonTypeLabel(row.reasonType) }}</td><td>{{ row.reasonLevel }}</td><td>{{ row.rejectNum }}</td><td>{{ row.rate }}</td><td>{{ ruleTypeLabel(row.ruleType) }}</td><td v-if="userDefinedAqlEnabled">{{ row.changeAql }}</td></tr>
            <tr v-if="!editorDetails.length"><td :colspan="userDefinedAqlEnabled ? 8 : 7" class="empty-cell">尚未設定明細</td></tr>
          </tbody></table></div>
          <div class="detail-actions"><button type="button" class="button" :disabled="!canEditDetails || !actionOptions.length || !reasonTypeOptions.length" @click="beginAddDetail">新增明細</button><button type="button" class="button" :disabled="!canEditDetails || selectedDetails.length !== 1" @click="beginEditSelectedDetail">修改明細</button><button type="button" class="button" :disabled="!canEditDetails || !selectedDetails.length" @click="removeEditorDetails">刪除明細</button></div>
          <div class="rule-controls"><div class="field-grid">
            <label class="field"><span>Action</span><select v-model.number="detailDraft.action" :disabled="!canEditDetails || !detailEditingEnabled || detailEditIndex !== null"><option v-for="option in actionOptions" :key="option.value" :value="option.value">{{ option.label }}</option></select></label>
            <label class="field"><span>ReasonType</span><select v-model.number="detailDraft.reasonType" :disabled="!canEditDetails || !detailEditingEnabled || detailEditIndex !== null"><option v-for="option in reasonTypeOptions" :key="option.value" :value="option.value">{{ option.label }}</option></select></label>
            <label class="field"><span>ReasonLevel</span><input v-model.number="detailDraft.reasonLevel" type="number" min="-1" max="9" step="1" :readonly="!canEditDetails || !detailEditingEnabled || detailEditIndex !== null"></label>
            <label class="field"><span>RuleType</span><select v-model.number="detailDraft.ruleType" :disabled="!canEditDetails || !detailEditingEnabled" @change="changeRuleType"><option v-if="detailDraft.ruleType === null" :value="null">未設定</option><option v-for="option in ruleTypes" :key="option.value" :value="option.value">{{ option.label }}</option></select></label>
            <label v-if="detailDraft.ruleType === 0" class="field"><span>RejectNum</span><input v-model="detailDraft.rejectNum" inputmode="decimal" :disabled="!canEditDetails || !detailEditingEnabled" maxlength="15"></label>
            <label v-if="detailDraft.ruleType === 1" class="field"><span>Rate</span><input v-model="detailDraft.rate" inputmode="decimal" :disabled="!canEditDetails || !detailEditingEnabled" maxlength="10"></label>
            <label v-if="detailDraft.ruleType === 2 && userDefinedAqlEnabled" class="field"><span>ChangeAQL</span><select v-model="detailDraft.changeAql" :disabled="!canEditDetails || !detailEditingEnabled"><option value=""></option><option v-for="option in aqlOptions" :key="option" :value="option">{{ mock ? option + '（DEMO）' : option }}</option></select></label>
          </div><div class="detail-actions"><button type="button" class="button primary" :disabled="!canEditDetails || !detailEditingEnabled" @click="saveDetailDraft">完成</button><button v-if="detailEditingEnabled" type="button" class="button" :disabled="saving" @click="resetDetailDraft">取消</button></div></div>
        </section>
        <fieldset class="audit-fields" disabled><legend>建立／修改資訊</legend><label><span>建立人</span><input :value="editorMode === 'edit' ? sourceBasis?.creator ?? '' : currentUser" readonly></label><label><span>建立日期</span><input :value="formatDate(editorMode === 'edit' ? sourceBasis?.createDate : createDate)" readonly></label><label><span>修改人</span><input :value="editorMode === 'edit' ? sourceBasis?.reviser ?? '' : ''" readonly></label><label><span>修改日期</span><input :value="editorMode === 'edit' ? formatDate(sourceBasis?.reviseDate) : ''" readonly></label></fieldset>
        <p v-if="editorNotice" class="message notice" role="status">{{ editorNotice }}</p><p v-if="editorError" class="message error" role="alert">{{ editorError }}</p>
        <footer class="dialog-actions"><button type="submit" class="button primary" :disabled="!canSave">確定</button><button type="button" class="button" :disabled="saving" @click="closeEditor">關閉</button><button type="button" class="button" :disabled="!canSave" @click="save(true)">套用</button></footer>
      </form>
    </section></div>
    <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
  </main>
</template>

<style scoped>
.attribute-rule-page{--line:#c8c4bb;--text:#30302e;--muted:#77746e;--field:#fff0ad;--orange:#f7a32b;display:flex;flex-direction:column;gap:7px;height:100%;min-height:0;padding:10px;color:var(--text);background:#f7f5ef;font:14px/1.35 "Segoe UI","Microsoft JhengHei",sans-serif}
.page-heading{display:flex;align-items:center;justify-content:space-between;min-height:34px}.page-heading h1{margin:0;font-size:20px;font-weight:600}.dev-badge{padding:4px 9px;border:1px solid #d7a044;background:#fff1bd;color:#77551a;font-size:12px}
.query-panel{display:flex;flex-wrap:wrap;align-items:center;gap:9px;min-height:42px;padding:5px 9px;border:1px solid var(--line);background:#fff}.states{display:flex;flex-wrap:wrap;gap:11px}.state-option{display:inline-flex;align-items:center;gap:4px;white-space:nowrap;cursor:pointer}.state-option input{accent-color:#e18b17}.search-input{width:190px}.wildcard{margin-left:-5px}
input,textarea,select{box-sizing:border-box;min-height:29px;padding:4px 7px;border:1px solid #a9a69f;border-radius:2px;background:var(--field);color:var(--text);font:inherit}input:focus,textarea:focus,select:focus{border-color:#d18a21;outline:2px solid rgb(209 138 33 / 20%)}input[readonly],input:disabled,textarea[readonly],select:disabled{background:#ecebe7;color:#777}.button{min-height:30px;padding:4px 10px;border:1px solid #bdb9b0;border-radius:3px;background:linear-gradient(#fff,#f0efec);color:#343331;font:inherit;white-space:nowrap;cursor:pointer}.button:hover:not(:disabled){border-color:#d08a23;background:#fff6df}.button:disabled{opacity:.48;cursor:not-allowed}.button.primary{border-color:#df8612;background:var(--orange);color:#fff}
.master-panel{display:grid;grid-template-columns:minmax(0,1fr) 88px;gap:8px;flex:1.1;min-height:135px;padding:7px;border:1px solid var(--line);background:#fff}.grid-wrap,.detail-grid,.editor-detail-grid{min-width:0;min-height:0;overflow:auto;border:1px solid #bcb8af}.data-grid{width:100%;border-collapse:collapse;white-space:nowrap;font-size:13px}.data-grid th{position:sticky;top:0;z-index:1;padding:5px 7px;border:1px solid #c2bfb9;background:#eeeae0;text-align:left;font-weight:600}.data-grid td{height:27px;padding:3px 7px;border:1px solid #d1cec8;background:#fff}.data-grid tbody tr{cursor:pointer}.data-grid tbody tr:hover td{background:#fff7dd}.data-grid tbody tr.active td{background:#ffedb1}.data-grid input[type=checkbox]{min-height:auto;accent-color:#dc8e1c}.data-grid .select-col{width:28px;text-align:center}.empty-cell{text-align:center;color:var(--muted)}.action-column{display:flex;flex-direction:column;gap:7px}.action-column .button{width:100%}.close-button{margin-top:auto}.list-footer{min-height:18px;padding:0 2px;color:var(--muted);font-size:12px}
.detail-panel{display:flex;flex:0.9;flex-direction:column;gap:5px;min-height:125px;padding:7px;border:1px solid var(--line);background:#fff}.detail-panel>header,.detail-editor-heading{display:flex;align-items:center;justify-content:space-between}.detail-panel h2,.detail-editor-heading h3{margin:0;font-size:15px;font-weight:600}.detail-editor-heading span{color:var(--muted);font-size:12px}.detail-grid{flex:1}.detail-grid .data-grid{min-width:650px}
.message{margin:0;padding:7px 10px;border:1px solid}.error{border-color:#d99b86;background:#fff0e9;color:#873820}.notice{border-color:#c9bc91;background:#fff8dc;color:#67551d}.backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:16px;background:rgb(25 24 22 / 28%)}.dialog{display:flex;flex-direction:column;width:min(640px,100%);max-height:calc(100vh - 32px);overflow:auto;border:1px solid #a8a49c;background:#f8f7f3;box-shadow:0 8px 28px rgb(0 0 0 / 22%)}.titlebar{display:flex;align-items:center;justify-content:space-between;padding:9px 14px;border-bottom:1px solid var(--line);background:#f0eee8}.titlebar h2{margin:0;font-size:17px;font-weight:600}.x{border:0;background:transparent;font-size:24px;line-height:1;cursor:pointer}.editor-form{display:flex;flex-direction:column;gap:9px;padding:11px}.basis-fields{display:grid;grid-template-columns:130px minmax(0,1fr);gap:8px 10px;margin:0;padding:9px;border:1px solid var(--line)}.basis-fields legend,.audit-fields legend{padding:0 5px;color:#57544e}.field{display:grid;grid-template-columns:130px minmax(0,1fr);align-items:center;gap:7px}.field>span{text-align:right}.field input,.field select{width:100%}.component-collect{grid-template-columns:130px auto auto 1fr}.component-collect>label{display:flex;align-items:center;gap:4px}.component-collect input[type=radio]{min-height:auto;accent-color:#e18b17}
.detail-editor{display:flex;flex-direction:column;gap:7px;min-height:245px;padding:9px;border:1px solid var(--line);background:#fff}.detail-editor-heading span{color:var(--muted);font-size:12px}.editor-detail-grid{flex:1;max-height:230px}.editor-detail-grid .data-grid{min-width:620px}.rule-controls{display:flex;flex-direction:column;gap:7px;padding:8px;border:1px solid #d1cec8;background:#fbfaf7}.field-grid{display:grid;grid-template-columns:repeat(4,minmax(115px,1fr));gap:7px 10px}.field-grid .field{grid-template-columns:75px minmax(0,1fr)}.field-grid .field>span{text-align:right}.detail-actions{display:flex;flex-wrap:wrap;gap:7px}.audit-fields{display:grid;grid-template-columns:1fr 1fr;gap:7px 12px;margin:0;padding:8px;border:1px solid var(--line)}.audit-fields label{display:grid;grid-template-columns:70px minmax(0,1fr);align-items:center;gap:6px}.audit-fields input{width:100%;background:#ecebe7}.dialog-actions{display:flex;justify-content:flex-end;gap:8px;padding-top:3px}.dialog-actions .primary{min-width:92px}
@media(max-width:850px){.attribute-rule-page{padding:7px}.query-panel{align-items:flex-start}.states{width:100%}.master-panel{grid-template-columns:1fr;min-height:220px}.action-column{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));grid-row:2}.close-button{margin-top:0}.detail-panel{min-height:180px}.basis-fields{grid-template-columns:1fr}.field{grid-template-columns:110px minmax(0,1fr)}.component-collect{grid-template-columns:110px auto auto 1fr}.field-grid{grid-template-columns:repeat(2,minmax(130px,1fr))}.audit-fields{grid-template-columns:1fr}}
</style>
