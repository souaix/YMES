<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type IssueState = 0 | 1 | 2 | -1
type FormMode = 'add' | 'edit' | 'copy' | null
interface PlanTypeOption { value: number; label: string }
interface SamplingParameter { parameterNo: string; parameterValue: string | null; showFlag: number }
interface PlanRow {
  planNo: string
  planName: string
  planType: number
  functionName: string | null
  description: string | null
  creator: string | null
  createDate: string | null
  issueState: IssueState
  sQtyCanBBiggerThenPQty: number
  dataStamp: number | null
  reviser: string | null
  reviseDate: string | null
}
interface PlanForm {
  planNo: string
  planName: string
  planType: number
  functionName: string
  description: string
  allowSampleAbovePopulation: boolean
}
interface ParameterRow extends SamplingParameter { options: string[] }
interface SamplingResult { sampleQty?: number; acceptQty?: number; rejectQty?: number; kValue?: number; fValue?: number; message?: string }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const isMock = computed(() => import.meta.env.DEV && props.developmentPreview)
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const storageKey = 'ymes-dev-mock:qc-sampling-plans:v1'
const typeDefaults: PlanTypeOption[] = [
  { value: 0, label: 'Fixed Sample Size' }, { value: 1, label: 'Fixed Sample Rate' }, { value: 2, label: 'AQL' },
  { value: 3, label: 'User Define Function' }, { value: 4, label: 'No Definition' }, { value: 5, label: 'Sampling Table' },
  { value: 6, label: 'Sampling 1916' }, { value: 7, label: 'AQL' },
]
const seed: PlanRow[] = [
  { planNo: 'DEV-FIXED-01', planName: 'DEV Fixed Sampling', planType: 0, functionName: null, description: 'Development-only sample plan', creator: 'DEV001', createDate: '2026-11-10T08:00:00+08:00', issueState: 0, sQtyCanBBiggerThenPQty: 0, dataStamp: 0, reviser: null, reviseDate: null },
  { planNo: 'DEV-AQL-01', planName: 'DEV AQL Sampling', planType: 2, functionName: null, description: 'Development-only AQL plan', creator: 'DEV001', createDate: '2026-11-10T08:10:00+08:00', issueState: 2, sQtyCanBBiggerThenPQty: 0, dataStamp: 1, reviser: 'DEV002', reviseDate: '2026-11-11T09:00:00+08:00' },
  { planNo: 'DEV-TABLE-01', planName: 'DEV Sampling Table', planType: 5, functionName: null, description: '', creator: 'DEV002', createDate: '2026-11-11T10:00:00+08:00', issueState: -1, sQtyCanBBiggerThenPQty: 1, dataStamp: 2, reviser: 'DEV002', reviseDate: '2026-11-12T09:30:00+08:00' },
]
const seedParameters: Record<string, SamplingParameter[]> = {
  'DEV-FIXED-01': [{ parameterNo: 'PopulationQty', parameterValue: '', showFlag: 0 }, { parameterNo: 'SampleQty', parameterValue: '5', showFlag: 1 }],
  'DEV-AQL-01': [{ parameterNo: 'AcceptQty', parameterValue: '', showFlag: 0 }, { parameterNo: 'AQLType', parameterValue: 'General', showFlag: 1 }, { parameterNo: 'PopulationQty', parameterValue: '', showFlag: 0 }, { parameterNo: 'QCLevel', parameterValue: 'II', showFlag: 1 }, { parameterNo: 'RejectQty', parameterValue: '', showFlag: 0 }, { parameterNo: 'SampleQty', parameterValue: '', showFlag: 0 }, { parameterNo: 'AQL', parameterValue: '1.0', showFlag: 1 }],
  'DEV-TABLE-01': [{ parameterNo: 'SamplingTable', parameterValue: 'DEV-AQL-1.0', showFlag: 1 }, { parameterNo: 'PopulationQty', parameterValue: '', showFlag: 0 }, { parameterNo: 'SampleQty', parameterValue: '', showFlag: 0 }, { parameterNo: 'RejectQty', parameterValue: '', showFlag: 0 }],
}
const devOptions: Record<string, string[]> = {
  AQLType: ['General', 'Tightened', 'Reduced'], QCLevel: ['I', 'II', 'III', 'S-1', 'S-2', 'S-3', 'S-4'],
  SamplingTable: ['DEV-AQL-1.0', 'DEV-AQL-2.5'], SamplingType: ['0', '1'], VLevel: ['I', 'II', 'III'],
}
const stateOptions: { value: IssueState; label: string }[] = [
  { value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' },
]
const rows = ref<PlanRow[]>([])
const typeOptions = ref<PlanTypeOption[]>(typeDefaults)
const functionOptions = ref<string[]>([])
const currentState = ref<IssueState>(0)
const searchValue = ref('')
const appliedSearchValue = ref('')
const selectedKeys = ref<string[]>([])
const activeKey = ref('')
const loading = ref(false)
const saving = ref(false)
const error = ref('')
const notice = ref('')
const formMode = ref<FormMode>(null)
const source = ref<PlanRow | null>(null)
const form = ref<PlanForm>(emptyForm())
const parameterRows = ref<ParameterRow[]>([])
const formError = ref('')
const dirty = ref(false)
const createdBy = ref('')
const createdAt = ref('')
const revisedBy = ref('')
const revisedAt = ref('')
const simulationOpen = ref(false)
const simulationPopulation = ref('100')
const simulationBusy = ref(false)
const simulationError = ref('')
const simulationResult = ref<SamplingResult | null>(null)
const simulationPlan = ref<PlanRow | null>(null)

function emptyForm(): PlanForm { return { planNo: '', planName: '', planType: 0, functionName: '', description: '', allowSampleAbovePopulation: false } }
function clone<T>(value: T): T { return structuredClone(value) }
function nowIso(): string { return new Date().toISOString() }
function readStore(): { rows: PlanRow[]; parameters: Record<string, SamplingParameter[]> } {
  try {
    const raw = window.localStorage.getItem(storageKey)
    if (raw) {
      const parsed = JSON.parse(raw) as { rows?: PlanRow[]; parameters?: Record<string, SamplingParameter[]> }
      if (Array.isArray(parsed.rows) && parsed.parameters) return parsed as { rows: PlanRow[]; parameters: Record<string, SamplingParameter[]> }
    }
  } catch { /* reset development-only data */ }
  const data = { rows: clone(seed), parameters: clone(seedParameters) }
  writeStore(data)
  return data
}
function writeStore(data: { rows: PlanRow[]; parameters: Record<string, SamplingParameter[]> }): void { window.localStorage.setItem(storageKey, JSON.stringify(data)) }
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body && !(init.body instanceof FormData) && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init, headers })
  if (response.status === 401) { window.location.assign(appBase + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    if (response.status === 404) throw new Error('QC103 API 尚未建立；請依 todolist_homepc.md 完成資料表與 API。')
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 回應錯誤（${response.status}）。`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function keyOf(row: Pick<PlanRow, 'planNo'>): string { return row.planNo.toUpperCase() }
function issueLabel(value: IssueState): string { return stateOptions.find(option => option.value === value)?.label ?? String(value) }
function planTypeLabel(value: number): string { return typeOptions.value.find(option => option.value === value)?.label ?? String(value) }
function formatDate(value: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  const parts = new Intl.DateTimeFormat('zh-TW', { timeZone: 'Asia/Taipei', year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false }).formatToParts(date)
  const part = (type: string) => parts.find(item => item.type === type)?.value ?? ''
  return `${part('year')}/${part('month')}/${part('day')} ${part('hour')}:${part('minute')}:${part('second')}`
}
function parameterSpecs(planType: number): { parameterNo: string; showFlag: number }[] {
  const names: Record<number, string[]> = {
    0: ['PopulationQty', 'SampleQty'], 1: ['PopulationQty', 'SampleQty', 'SampleRate'],
    2: ['AcceptQty', 'AQLType', 'PopulationQty', 'QCLevel', 'RejectQty', 'SampleQty', 'AQL'],
    4: ['PopulationQty', 'SampleQty'], 5: ['SamplingTable', 'PopulationQty', 'SampleQty', 'RejectQty'],
    6: ['SamplingType', 'VLevel', 'PopulationQty', 'SampleQty', 'KValue', 'FValue', 'RejectQty'],
    7: ['AcceptQty', 'AQLType', 'PopulationQty', 'QCLevel', 'RejectQty', 'SampleQty', 'AQL'],
  }
  return (names[planType] ?? []).map(parameterNo => ({
    parameterNo,
    showFlag: planType === 0 ? Number(parameterNo === 'SampleQty')
      : planType === 1 ? Number(parameterNo === 'SampleRate')
        : planType === 2 || planType === 7 ? Number(['AQLType', 'QCLevel', 'AQL'].includes(parameterNo))
          : planType === 5 ? Number(parameterNo === 'SamplingTable') : 1,
  }))
}
const visibleRows = computed(() => {
  const query = appliedSearchValue.value.trim().toUpperCase()
  return rows.value.filter(row => row.issueState === currentState.value && (!query || row.planNo.toUpperCase().includes(query)))
    .slice().sort((a, b) => a.planNo.localeCompare(b.planNo))
})
const activeRow = computed(() => rows.value.find(row => keyOf(row) === activeKey.value) ?? null)
const selectedRows = computed(() => visibleRows.value.filter(row => selectedKeys.value.includes(keyOf(row))))
const formReadOnly = computed(() => formMode.value === 'edit' && source.value?.issueState === 1)
const dialogTitle = computed(() => `Sampling Plan Define (${formMode.value === 'add' ? 'ADD' : formMode.value === 'copy' ? 'Copy' : 'EDIT'})`)

async function load(): Promise<void> {
  if (isMock.value) { rows.value = clone(readStore().rows); return }
  const query = new URLSearchParams()
  if (appliedSearchValue.value) { query.set('searchFieldName', 'PlanNo'); query.set('searchFieldValue', appliedSearchValue.value) }
  rows.value = await request<PlanRow[]>('api/qc/sampling-plans' + (query.toString() ? '?' + query.toString() : ''))
}
function setActive(prefer = ''): void {
  const row = visibleRows.value.find(item => keyOf(item) === prefer) ?? visibleRows.value[0]
  activeKey.value = row ? keyOf(row) : ''
  selectedKeys.value = row ? [keyOf(row)] : []
}
async function refresh(prefer = ''): Promise<void> { await load(); setActive(prefer) }
async function query(): Promise<void> {
  appliedSearchValue.value = searchValue.value.trim()
  loading.value = true; error.value = ''; notice.value = ''
  try { await load(); setActive() }
  catch (reason) { error.value = reason instanceof Error ? reason.message : '查詢抽樣計劃失敗。' }
  finally { loading.value = false }
}
function changeState(value: IssueState): void { currentState.value = value; notice.value = ''; setActive() }
function selectRow(row: PlanRow): void { activeKey.value = keyOf(row); selectedKeys.value = [activeKey.value] }
function toggleRow(row: PlanRow): void {
  const key = keyOf(row)
  selectedKeys.value = selectedKeys.value.includes(key) ? selectedKeys.value.filter(item => item !== key) : [...selectedKeys.value, key]
  activeKey.value = key
}
function selectVisible(event: Event): void {
  selectedKeys.value = (event.target as HTMLInputElement).checked ? visibleRows.value.map(keyOf) : []
  activeKey.value = selectedKeys.value[0] ?? ''
}
function setTypeRows(planType: number, existing: SamplingParameter[] = []): void {
  const values = new Map(existing.map(item => [item.parameterNo, item.parameterValue ?? '']))
  parameterRows.value = parameterSpecs(planType).map(spec => ({ ...spec, parameterValue: values.get(spec.parameterNo) ?? '', options: devOptions[spec.parameterNo] ?? [] }))
  for (const item of existing) {
    if (!parameterRows.value.some(row => row.parameterNo === item.parameterNo)) parameterRows.value.push({ ...item, parameterValue: item.parameterValue ?? '', options: devOptions[item.parameterNo] ?? [] })
  }
  if (isMock.value) return
  for (const row of parameterRows.value) void loadParameterOptions(row.parameterNo)
}
async function loadParameterOptions(parameterNo: string): Promise<void> {
  if (!parameterNo || isMock.value) return
  try {
    const options = await request<string[]>('api/qc/sampling-plans/parameter-options/' + encodeURIComponent(parameterNo))
    const row = parameterRows.value.find(item => item.parameterNo === parameterNo)
    if (row) row.options = options
  } catch { /* free-text remains available if this parameter has no value list */ }
}
async function loadFunctions(): Promise<void> {
  if (isMock.value) { functionOptions.value = ['DEV_FUNSAMPLING_DEFAULT', 'DEV_FUNSAMPLING_AQL']; return }
  try { functionOptions.value = await request<string[]>('api/qc/sampling-plans/function-options') }
  catch (reason) { formError.value = reason instanceof Error ? reason.message : '載入抽樣函式失敗。' }
}
async function loadPlanTypes(): Promise<void> {
  if (isMock.value) return
  try { typeOptions.value = await request<PlanTypeOption[]>('api/qc/sampling-plans/plan-types') }
  catch { /* source-derived fallback values remain available */ }
}
async function openForm(mode: Exclude<FormMode, null>, row: PlanRow | null = null): Promise<void> {
  source.value = row ? clone(row) : null
  form.value = row ? {
    planNo: row.planNo, planName: mode === 'copy' ? row.planName + ' (Copy)' : row.planName,
    planType: row.planType, functionName: row.functionName ?? '', description: row.description ?? '', allowSampleAbovePopulation: row.sQtyCanBBiggerThenPQty === 1,
  } : emptyForm()
  createdBy.value = row?.creator ?? props.currentUserNo ?? 'DEV001'
  createdAt.value = row?.createDate ?? nowIso()
  revisedBy.value = row?.reviser ?? ''
  revisedAt.value = row?.reviseDate ?? ''
  formMode.value = mode; formError.value = ''; dirty.value = false; parameterRows.value = []
  if (row) {
    try {
      const params = isMock.value ? clone(readStore().parameters[row.planNo] ?? []) : await request<SamplingParameter[]>('api/qc/sampling-plans/' + encodeURIComponent(row.planNo) + '/parameters')
      setTypeRows(row.planType, params)
    } catch (reason) { formError.value = reason instanceof Error ? reason.message : '載入抽樣參數失敗。' }
  } else setTypeRows(form.value.planType)
  if (form.value.planType === 3) void loadFunctions()
}
function closeForm(): void { formMode.value = null; source.value = null; parameterRows.value = []; formError.value = ''; dirty.value = false }
function changePlanType(event: Event): void {
  const value = Number((event.target as HTMLSelectElement).value)
  form.value.planType = value
  if (value !== 3) form.value.functionName = ''
  setTypeRows(value)
  markDirty()
  if (value === 3) void loadFunctions()
}
function markDirty(): void { dirty.value = true; formError.value = '' }
function isPredefinedParameter(parameterNo: string): boolean { return parameterSpecs(form.value.planType).some(spec => spec.parameterNo === parameterNo) }
function addParameter(): void { parameterRows.value.push({ parameterNo: '', parameterValue: '', showFlag: 1, options: [] }); markDirty() }
function removeParameter(index: number): void { parameterRows.value.splice(index, 1); markDirty() }
function validate(): string {
  const planNo = form.value.planNo.trim().toUpperCase()
  if (!planNo) return 'PlanNo 為必填。'
  if (planNo.length > 25) return 'PlanNo 不可超過 25 個字元。'
  if (!form.value.planName.trim()) return 'PlanName 為必填。'
  if (form.value.planName.trim().length > 50) return 'PlanName 不可超過 50 個字元。'
  if (!typeOptions.value.some(item => item.value === form.value.planType)) return 'PlanType 必須選擇有效項目。'
  if (form.value.functionName.length > 50) return 'FunctionName 不可超過 50 個字元。'
  if (form.value.planType === 3 && !form.value.functionName.trim()) return 'User Define Function 必須選擇 FunctionName。'
  if (form.value.description.length > 255) return 'Description 不可超過 255 個字元。'
  if (formMode.value !== 'edit' && rows.value.some(row => row.planNo.toUpperCase() === planNo)) return '相同 PlanNo 已存在。'
  const parameterNames = parameterRows.value.map(row => row.parameterNo.trim().toUpperCase()).filter(Boolean)
  if (parameterNames.length !== parameterRows.value.length) return 'ParameterNo 不可空白。'
  if (new Set(parameterNames).size !== parameterNames.length) return 'ParameterNo 不可重複。'
  const missingParameters = parameterSpecs(form.value.planType).filter(spec => !parameterNames.includes(spec.parameterNo.toUpperCase()))
  if (missingParameters.length) return '缺少必要參數：' + missingParameters.map(item => item.parameterNo).join('、') + '。'
  for (const row of parameterRows.value) {
    if (row.parameterNo.length > 20) return 'ParameterNo 不可超過 20 個字元。'
    if (row.parameterValue && row.parameterValue.length > 255) return `${row.parameterNo} 不可超過 255 個字元。`
    if (row.showFlag === 1 && !row.parameterValue?.trim()) return `${row.parameterNo} 為必填。`
    if (row.parameterNo === 'SampleQty' && row.parameterValue && (!Number.isFinite(Number(row.parameterValue)) || Number(row.parameterValue) < 0)) return 'SampleQty 必須為非負數。'
    if (row.parameterNo === 'SampleRate' && row.parameterValue) {
      const rate = Number(row.parameterValue)
      if (!Number.isFinite(rate) || rate < 0 || rate > 1) return 'SampleRate 必須介於 0 到 1。'
    }
  }
  return ''
}
async function save(keepOpen: boolean): Promise<void> {
  formError.value = validate()
  if (formError.value || !formMode.value || formReadOnly.value) return
  const mode = formMode.value
  const original = source.value
  const record: PlanRow = {
    planNo: form.value.planNo.trim().toUpperCase(), planName: form.value.planName.trim(), planType: form.value.planType,
    functionName: form.value.functionName.trim() || null, description: form.value.description.trim() || null,
    creator: createdBy.value || null, createDate: createdAt.value || null, issueState: 0,
    sQtyCanBBiggerThenPQty: form.value.allowSampleAbovePopulation ? 1 : 0,
    dataStamp: original?.dataStamp ?? 0, reviser: revisedBy.value || null, reviseDate: revisedAt.value || null,
  }
  const parameters = parameterRows.value.map(row => ({ parameterNo: row.parameterNo.trim(), parameterValue: row.parameterValue?.trim() || null, showFlag: row.showFlag }))
  saving.value = true
  try {
    if (isMock.value) {
      const data = readStore()
      if (mode === 'edit') {
        const idx = data.rows.findIndex(row => original && keyOf(row) === keyOf(original))
        if (idx < 0) throw new Error('資料已不存在，請重新查詢。')
        const prior = data.rows[idx]
        data.rows[idx] = { ...record, creator: prior.creator, createDate: prior.createDate, issueState: prior.issueState, reviser: props.currentUserNo || 'DEV001', reviseDate: nowIso(), dataStamp: (prior.dataStamp ?? 0) + 1 }
      } else data.rows.push({ ...record, creator: props.currentUserNo || 'DEV001', createDate: nowIso(), issueState: 0, reviser: null, reviseDate: null, dataStamp: 0 })
      data.parameters[record.planNo] = clone(parameters)
      writeStore(data)
    } else if (mode === 'edit') {
      const query = new URLSearchParams()
      query.set('issueState', String(original!.issueState))
      query.set('dataStampIsNull', original!.dataStamp === null ? 'true' : 'false')
      if (original!.dataStamp !== null) query.set('dataStamp', String(original!.dataStamp))
      await request('api/qc/sampling-plans/' + encodeURIComponent(original!.planNo) + '?' + query.toString(), {
        method: 'PUT', body: JSON.stringify({ planName: record.planName, planType: record.planType, functionName: record.functionName, description: record.description, sQtyCanBBiggerThenPQty: record.sQtyCanBBiggerThenPQty, parameters }),
      })
    } else await request('api/qc/sampling-plans', { method: 'POST', body: JSON.stringify({ planNo: record.planNo, planName: record.planName, planType: record.planType, functionName: record.functionName, description: record.description, sQtyCanBBiggerThenPQty: record.sQtyCanBBiggerThenPQty, parameters }) })
    await refresh(keyOf(record))
    dirty.value = false
    if (keepOpen && mode === 'edit') {
      source.value = activeRow.value ? clone(activeRow.value) : null
      if (activeRow.value) { revisedBy.value = activeRow.value.reviser ?? ''; revisedAt.value = activeRow.value.reviseDate ?? '' }
      notice.value = '資料已套用。'
    } else if (keepOpen) notice.value = '資料已套用。'
    else closeForm()
  } catch (reason) { formError.value = reason instanceof Error ? reason.message : '儲存抽樣計劃失敗。' }
  finally { saving.value = false }
}
async function removeSelected(): Promise<void> {
  const items = [...selectedRows.value]
  if (!items.length) return
  if (items.some(row => row.issueState === 1)) { error.value = '簽核中的抽樣計劃不可刪除。'; return }
  if (!window.confirm(`確定刪除或停用選取的 ${items.length} 筆抽樣計劃？`)) return
  loading.value = true; error.value = ''
  try {
    for (const row of items) {
      if (isMock.value) {
        const data = readStore()
        if (row.issueState === 2) {
          const current = data.rows.find(item => keyOf(item) === keyOf(row))!
          current.issueState = -1; current.reviser = props.currentUserNo || 'DEV001'; current.reviseDate = nowIso(); current.dataStamp = (current.dataStamp ?? 0) + 1
        } else {
          data.rows = data.rows.filter(item => keyOf(item) !== keyOf(row))
          delete data.parameters[row.planNo]
        }
        writeStore(data)
      } else {
        const query = new URLSearchParams({ issueState: String(row.issueState), dataStampIsNull: row.dataStamp === null ? 'true' : 'false' })
        if (row.dataStamp !== null) query.set('dataStamp', String(row.dataStamp))
        await request('api/qc/sampling-plans/' + encodeURIComponent(row.planNo) + '?' + query.toString(), { method: 'DELETE' })
      }
    }
    await refresh(); notice.value = '操作完成。'
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '刪除抽樣計劃失敗。' }
  finally { loading.value = false }
}
async function approveSelected(): Promise<void> {
  const items = [...selectedRows.value]
  if (!items.length) return
  if (!window.confirm(`確定核准選取的 ${items.length} 筆抽樣計劃？`)) return
  loading.value = true; error.value = ''
  try {
    for (const row of items) {
      if (row.issueState !== 0) throw new Error('只有未簽核資料可以核准。')
      if (isMock.value) {
        const data = readStore(); const current = data.rows.find(item => keyOf(item) === keyOf(row))!
        current.issueState = 2; current.reviser = props.currentUserNo || 'DEV001'; current.reviseDate = nowIso(); current.dataStamp = (current.dataStamp ?? 0) + 1; writeStore(data)
      } else {
        const query = new URLSearchParams({ dataStampIsNull: row.dataStamp === null ? 'true' : 'false' })
        if (row.dataStamp !== null) query.set('dataStamp', String(row.dataStamp))
        await request('api/qc/sampling-plans/' + encodeURIComponent(row.planNo) + '/approve?' + query.toString(), { method: 'POST' })
      }
    }
    await refresh(); notice.value = '核准申請完成。'
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '核准抽樣計劃失敗。' }
  finally { loading.value = false }
}
function openSimulation(): void {
  if (!activeRow.value) return
  simulationPlan.value = clone(activeRow.value); simulationPopulation.value = '100'; simulationResult.value = null; simulationError.value = ''; simulationOpen.value = true
}
async function runSimulation(): Promise<void> {
  const population = Number(simulationPopulation.value)
  if (!Number.isFinite(population) || population < 0) { simulationError.value = 'PopulationQty 必須為非負數。'; return }
  simulationBusy.value = true; simulationError.value = ''; simulationResult.value = null
  try {
    if (isMock.value) {
      if (simulationPlan.value?.planType === 0) {
        const params = readStore().parameters[simulationPlan.value.planNo] ?? []
        let sampleQty = Number(params.find(item => item.parameterNo === 'SampleQty')?.parameterValue ?? 0)
        if (simulationPlan.value.sQtyCanBBiggerThenPQty === 0) sampleQty = Math.min(sampleQty, population)
        simulationResult.value = { sampleQty, message: 'DEV 固定抽樣示範；其他抽樣類型請使用正式 API 計算。' }
      } else simulationResult.value = { message: 'DEV 預覽只展示模擬輸入/輸出介面；此抽樣類型由正式 API 使用 MES 規則計算。' }
    } else simulationResult.value = await request<SamplingResult>('api/qc/sampling-plans/' + encodeURIComponent(simulationPlan.value!.planNo) + '/simulate', { method: 'POST', body: JSON.stringify({ populationQty: population }) })
  } catch (reason) { simulationError.value = reason instanceof Error ? reason.message : '執行抽樣模擬失敗。' }
  finally { simulationBusy.value = false }
}
function closePage(): void { emit('close') }
onMounted(async () => {
  loading.value = true
  try { await Promise.all([load(), loadPlanTypes()]); setActive() }
  catch (reason) { error.value = reason instanceof Error ? reason.message : '載入抽樣計劃失敗。' }
  finally { loading.value = false }
})
</script>

<template>
  <section class="sampling-plan-page" aria-label="抽樣計劃">
    <header class="page-title"><h1>抽樣計劃</h1><span v-if="isMock" class="preview-tag">開發假資料</span></header>
    <div class="query-bar">
      <div class="state-options"><label v-for="option in stateOptions" :key="option.value"><input type="radio" name="planState" :checked="currentState === option.value" @change="changeState(option.value)">{{ option.label }}</label></div>
      <label class="search"><span>PlanNo</span><input v-model="searchValue" maxlength="25" @keyup.enter="query"><span>%</span></label>
      <button type="button" @click="query">查詢</button>
    </div>
    <div v-if="error" class="message error" role="alert">{{ error }}</div><div v-if="notice" class="message notice" role="status">{{ notice }}</div>
    <div class="master-layout" :aria-busy="loading">
      <div class="grid-frame"><table class="plan-grid"><thead><tr>
        <th class="selector"><input type="checkbox" aria-label="選取全部" :checked="visibleRows.length > 0 && selectedRows.length === visibleRows.length" @change="selectVisible"></th>
        <th>PlanNo</th><th>PlanName</th><th>PlanType</th><th>FunctionName</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th>
      </tr></thead><tbody>
        <tr v-for="row in visibleRows" :key="row.planNo" :class="{ active: activeKey === keyOf(row), selected: selectedKeys.includes(keyOf(row)) }" @click="selectRow(row)" @dblclick="openForm('edit', row)">
          <td class="selector" @click.stop><input type="checkbox" :checked="selectedKeys.includes(keyOf(row))" :aria-label="`選取 ${row.planNo}`" @change="toggleRow(row)"></td>
          <td>{{ row.planNo }}</td><td>{{ row.planName }}</td><td>{{ planTypeLabel(row.planType) }}</td><td>{{ row.functionName ?? '' }}</td><td class="description-cell">{{ row.description ?? '' }}</td><td>{{ row.creator ?? '' }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser ?? '' }}</td><td>{{ formatDate(row.reviseDate) }}</td>
        </tr>
        <tr v-if="!visibleRows.length"><td colspan="10" class="empty">{{ loading ? '載入中…' : '沒有資料' }}</td></tr>
      </tbody></table></div>
      <aside class="actions"><button class="primary" type="button" @click="openForm('add')">新增(A)</button><button type="button" :disabled="!activeRow || currentState === 1" @click="activeRow && openForm('edit', activeRow)">修改(E)</button><button type="button" :disabled="!selectedRows.length" @click="removeSelected">刪除(D)</button><button type="button" :disabled="!activeRow || currentState === 1" @click="activeRow && openForm('copy', activeRow)">複製(C)</button><button type="button" :disabled="currentState !== 0 || !selectedRows.length" @click="approveSelected">核准(Y)</button><button type="button" :disabled="!activeRow" @click="openSimulation">Simulation</button><button class="close" type="button" @click="closePage">關閉(X)</button></aside>
    </div>
    <footer class="grid-footer">資料總筆數 {{ visibleRows.length }}</footer>

    <div v-if="formMode" class="backdrop" @click.self="closeForm">
      <section class="dialog definition-dialog" role="dialog" aria-modal="true" :aria-label="dialogTitle">
        <header class="dialog-title"><h2>{{ dialogTitle }}</h2><button type="button" class="x-button" aria-label="關閉" @click="closeForm">×</button></header>
        <form class="editor" @submit.prevent="save(false)">
          <fieldset><legend>Sampling Plan</legend>
            <label class="plan-no"><span>Plan No</span><input v-model="form.planNo" maxlength="25" :readonly="formMode === 'edit' || formReadOnly" class="kd-field" @blur="form.planNo = form.planNo.toUpperCase()" @input="markDirty"></label>
            <label><span>Plan Name</span><input v-model="form.planName" maxlength="50" :readonly="formReadOnly" class="kd-field" @input="markDirty"></label>
            <label><span>Plan Type</span><select :value="form.planType" :disabled="formReadOnly" class="kd-field" @change="changePlanType"><option v-for="option in typeOptions" :key="option.value" :value="option.value">{{ option.value }} - {{ option.label }}</option></select></label>
            <label v-if="form.planType === 3"><span>Function Name</span><select v-model="form.functionName" :disabled="formReadOnly" class="kd-field" @change="markDirty"><option value="">請選擇</option><option v-for="fn in functionOptions" :key="fn" :value="fn">{{ fn }}</option></select></label>
            <label class="description"><span>Description</span><textarea v-model="form.description" maxlength="255" :readonly="formReadOnly" class="kd-field" @input="markDirty"></textarea></label>
            <div class="sample-limit"><span>Sample Qty Can Be Bigger Than Population Qty</span><label><input v-model="form.allowSampleAbovePopulation" type="radio" :value="true" :disabled="formReadOnly" @change="markDirty"> Yes</label><label><input v-model="form.allowSampleAbovePopulation" type="radio" :value="false" :disabled="formReadOnly" @change="markDirty"> No</label></div>
          </fieldset>
          <fieldset class="parameter-box"><legend>Sampling Parameter</legend>
            <div class="parameter-actions"><button type="button" :disabled="formReadOnly" @click="addParameter">ADD(A)</button><button type="button" :disabled="formReadOnly || !parameterRows.length" @click="removeParameter(parameterRows.length - 1)">DEL(D)</button></div>
            <div class="parameter-scroll"><table class="parameter-grid"><thead><tr><th>ParameterNo</th><th>ParameterValue</th><th>必填</th><th></th></tr></thead><tbody>
              <tr v-for="(parameter, index) in parameterRows" :key="index"><td><input v-model="parameter.parameterNo" :readonly="isPredefinedParameter(parameter.parameterNo) || formReadOnly" maxlength="20" @input="markDirty"></td><td><select v-if="parameter.options.length" v-model="parameter.parameterValue" :disabled="formReadOnly" @change="markDirty"><option value="">（空白）</option><option v-for="value in parameter.options" :key="value" :value="value">{{ value }}</option></select><input v-else v-model="parameter.parameterValue" :readonly="formReadOnly" maxlength="255" @input="markDirty"></td><td>{{ parameter.showFlag === 1 ? '是' : '否' }}</td><td><button type="button" :disabled="formReadOnly" aria-label="刪除參數" @click="removeParameter(index)">×</button></td></tr>
              <tr v-if="!parameterRows.length"><td colspan="4" class="empty">此 PlanType 沒有預設參數</td></tr>
            </tbody></table></div>
          </fieldset>
          <fieldset class="audit-box"><legend>Basis Data</legend><label><span>Creator</span><input :value="createdBy" readonly></label><label><span>CreateDate</span><input :value="formatDate(createdAt)" readonly></label><label><span>Reviser</span><input :value="revisedBy" readonly></label><label><span>ReviseDate</span><input :value="formatDate(revisedAt)" readonly></label></fieldset>
          <p v-if="formReadOnly" class="form-error">此資料簽核中，表單唯讀。</p><p v-if="formError" class="form-error" role="alert">{{ formError }}</p>
          <div class="dialog-actions"><button class="primary" type="submit" :disabled="saving || formReadOnly">確定</button><button type="button" :disabled="saving" @click="closeForm">關閉</button><button type="button" :disabled="saving || !dirty || formReadOnly" @click="save(true)">套用</button></div>
        </form>
      </section>
    </div>

    <div v-if="simulationOpen" class="backdrop" @click.self="simulationOpen = false"><section class="dialog simulation-dialog" role="dialog" aria-modal="true" aria-label="Sampling Parameter Simulation"><header class="dialog-title"><h2>Sampling Parameter Simulation</h2><button class="x-button" type="button" aria-label="關閉" @click="simulationOpen = false">×</button></header><div class="simulation-body"><p><b>{{ simulationPlan?.planNo }}</b> · {{ planTypeLabel(simulationPlan?.planType ?? -1) }}</p><label><span>Population Qty</span><input v-model="simulationPopulation" type="number" min="0" class="kd-field"></label><p v-if="simulationError" class="form-error" role="alert">{{ simulationError }}</p><fieldset v-if="simulationResult"><legend>Result</legend><div v-if="simulationResult.sampleQty !== undefined">SampleQty：{{ simulationResult.sampleQty }}</div><div v-if="simulationResult.acceptQty !== undefined">AcceptQty：{{ simulationResult.acceptQty }}</div><div v-if="simulationResult.rejectQty !== undefined">RejectQty：{{ simulationResult.rejectQty }}</div><div v-if="simulationResult.kValue !== undefined">KValue：{{ simulationResult.kValue }}</div><div v-if="simulationResult.fValue !== undefined">FValue：{{ simulationResult.fValue }}</div><div v-if="simulationResult.message">{{ simulationResult.message }}</div></fieldset><div class="dialog-actions"><button class="primary" type="button" :disabled="simulationBusy" @click="runSimulation">Simulation</button><button type="button" @click="simulationOpen = false">關閉</button></div></div></section></div>
  </section>
</template>

<style scoped>
.sampling-plan-page{display:flex;flex-direction:column;gap:8px;min-width:0;height:100%;padding:12px;background:#fff;color:#282722;font-size:13px}.page-title{display:flex;align-items:center;gap:12px;min-height:34px;border-bottom:1px solid #dedbd4}.page-title h1{margin:0;font-size:20px;font-weight:600}.preview-tag{padding:3px 8px;border:1px solid #d8c57d;border-radius:3px;background:#fff4bf;color:#66551b;font-size:11px}.query-bar{display:flex;align-items:center;gap:12px;min-height:42px;padding:4px 10px;border:1px solid #d7d4cd;background:#faf9f6}.state-options{display:flex;align-items:center;gap:12px}.state-options label{display:flex;align-items:center;gap:4px;white-space:nowrap}.state-options input{accent-color:#666}.search{display:flex;align-items:center;gap:7px;margin-left:auto}.search input{width:180px;height:27px;border:1px solid #a9a59c;background:#fff;color:#333;padding:3px 6px}.query-bar>button,.actions button,.dialog-actions button,.parameter-actions button{min-height:31px;padding:4px 9px;border:1px solid #c5c1b8;border-radius:3px;background:#f8f7f3;color:#333;cursor:pointer}.query-bar>button:hover,.actions button:hover,.dialog-actions button:hover,.parameter-actions button:hover{background:#efede7}.message{padding:6px 9px;border:1px solid #d8d2c4}.message.error{background:#fff0ed;color:#902b1d}.message.notice{background:#f5f2e9;color:#595548}.master-layout{display:grid;grid-template-columns:minmax(0,1fr) 96px;gap:10px;min-height:240px;flex:1}.grid-frame{min-width:0;overflow:auto;border:1px solid #c9c6bf;background:#fff}.plan-grid,.parameter-grid{width:100%;border-collapse:collapse;white-space:nowrap}.plan-grid th,.plan-grid td,.parameter-grid th,.parameter-grid td{height:29px;padding:4px 7px;border:1px solid #d0cdc6;text-align:left}.plan-grid th,.parameter-grid th{position:sticky;top:0;z-index:1;background:#e8e6e0;color:#37352f;font-weight:600}.plan-grid tbody tr{cursor:pointer}.plan-grid tbody tr:hover{background:#fff8dc}.plan-grid tbody tr.active{outline:1px solid #a9975b;outline-offset:-1px}.plan-grid tbody tr.selected{background:#fff0b5}.plan-grid .selector{width:28px;padding:2px;text-align:center}.selector input{margin:0;accent-color:#776328}.description-cell{max-width:240px;overflow:hidden;text-overflow:ellipsis}.empty{height:52px;color:#777;text-align:center}.actions{display:flex;flex-direction:column;gap:7px}.actions button:disabled,.dialog-actions button:disabled,.parameter-actions button:disabled{opacity:.5;cursor:default}.actions .primary,.dialog-actions .primary{border-color:#da941b;background:#ffab2e;color:#fff;font-weight:600}.actions .primary:hover,.dialog-actions .primary:hover{background:#f29b20}.actions .close{margin-top:auto}.grid-footer{min-height:20px;color:#77746e}.backdrop{position:fixed;z-index:60;inset:0;display:flex;align-items:center;justify-content:center;padding:16px;background:rgb(28 27 25 / 42%)}.dialog{display:flex;flex-direction:column;max-height:95vh;overflow:auto;border:1px solid #b9b4aa;background:#f8f6f1;box-shadow:0 12px 36px rgb(0 0 0 / 24%)}.definition-dialog{width:min(540px,97vw)}.simulation-dialog{width:min(460px,97vw)}.dialog-title{position:sticky;top:0;z-index:2;display:flex;align-items:center;justify-content:space-between;padding:9px 14px;border-bottom:1px solid #d2cec5;background:#f2efe8}.dialog-title h2{margin:0;font-size:17px;font-weight:600}.x-button{border:0;background:transparent;font-size:23px;cursor:pointer}.editor,.simulation-body{display:flex;flex-direction:column;gap:8px;padding:10px}.editor fieldset,.simulation-body fieldset{min-width:0;border:1px solid #c9c4ba;padding:8px 10px}.editor legend,.simulation-body legend{padding:0 5px;color:#5a564f}.editor fieldset:first-child>label{display:grid;grid-template-columns:118px minmax(0,1fr);align-items:center;gap:8px;margin:4px 0}.editor fieldset:first-child>label>span,.simulation-body label>span{text-align:right}.plan-no{max-width:350px}.kd-field{width:100%;height:25px;padding:3px 6px;border:1px solid #99958c;border-radius:0;background:#fff0ad;color:#282722}.kd-field[readonly],.kd-field:disabled{background:#ecebe7;color:#777}.description textarea{height:54px;resize:vertical}.sample-limit{display:grid;grid-template-columns:minmax(0,1fr) auto auto;align-items:center;gap:12px;margin:6px 0 2px;padding-left:6px}.sample-limit label{display:flex;align-items:center;gap:3px;white-space:nowrap}.sample-limit input{accent-color:#666}.parameter-box{padding:6px!important}.parameter-actions{display:flex;gap:6px;margin:0 0 6px}.parameter-actions button{min-height:25px;padding:2px 8px}.parameter-scroll{max-height:140px;overflow:auto;border:1px solid #d0cdc6}.parameter-grid{font-size:12px}.parameter-grid th,.parameter-grid td{height:26px;padding:3px 5px}.parameter-grid td input,.parameter-grid td select{width:100%;height:23px;border:1px solid #aaa69e;background:#fff0ad;padding:2px 4px;color:#282722}.parameter-grid td:first-child input[readonly]{background:#ecebe7}.parameter-grid td:last-child button{border:0;background:transparent;font-size:18px;cursor:pointer}.audit-box{display:grid;grid-template-columns:1fr 1fr;gap:5px 9px}.audit-box label{display:grid;grid-template-columns:68px minmax(0,1fr);align-items:center;gap:4px}.audit-box label>span{text-align:right}.audit-box input{width:100%;height:23px;border:1px solid #c7c4bd;background:#ecebe7;color:#555;padding:2px 4px}.form-error{margin:0;color:#a12e20}.dialog-actions{position:sticky;bottom:0;display:flex;justify-content:flex-end;gap:8px;padding-top:4px}.dialog-actions button{min-width:72px}.simulation-body>label{display:grid;grid-template-columns:130px minmax(0,1fr);gap:8px;align-items:center}.simulation-body fieldset{line-height:1.8}
@media(max-width:820px){.sampling-plan-page{padding:8px}.query-bar{flex-wrap:wrap}.state-options{flex-wrap:wrap}.search{margin-left:0}.master-layout{grid-template-columns:1fr;grid-template-rows:minmax(220px,1fr) auto}.plan-grid{min-width:1050px}.actions{display:grid;grid-template-columns:repeat(4,minmax(0,1fr))}.actions .close{margin-top:0}.sample-limit{grid-template-columns:1fr auto auto}.audit-box{grid-template-columns:1fr}.editor fieldset:first-child>label{grid-template-columns:110px minmax(0,1fr)}}
</style>
