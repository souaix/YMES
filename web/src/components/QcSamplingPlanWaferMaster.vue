<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type IssueState = 0 | 1 | 2 | -1
type FormMode = 'add' | 'edit' | 'copy' | 'view' | null
interface PlanRow {
  planNo: string
  sampleType: number
  sampleRate: number
  everyNum: number
  samplePieceNum: number
  samplePointNum: number
  creator: string | null
  createDate: string | null
  issueState: IssueState
  functionName: string | null
  aqlType: string | null
  aql: string | null
  qcLevel: string | null
  rejectQty: number | null
  dataStamp: number | null
  reviser: string | null
  reviseDate: string | null
}
interface RangeRow { lowerLimit: number; upperLimit: number; sampleNum: number; _selected?: boolean }
interface AreaRow { area: string; areaType: number; areaNo: string | null; _selected?: boolean }
interface PlanForm {
  planNo: string; sampleType: number; sampleRate: string; everyNum: string; samplePieceNum: string
  samplePointNum: string; rejectQty: string; aqlType: string; qcLevel: string; aql: string
}
interface Store { rows: PlanRow[]; ranges: Record<string, RangeRow[]>; areas: Record<string, AreaRow[]> }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const isMock = computed(() => import.meta.env.DEV && props.developmentPreview)
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const storageKey = 'ymes-dev-mock:qc-sampling-plan-wafer:v1'
const sampleTypes = [
  { value: 0, label: 'by Rate' }, { value: 1, label: 'by Wafer Count' }, { value: 2, label: 'by Fixed Piece' },
  { value: 3, label: 'by Range' }, { value: 5, label: 'by 105E AQL' }, { value: 6, label: 'by GB2828 AQL' },
]
const stateOptions: { value: IssueState; label: string }[] = [
  { value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' },
]
const seed: Store = {
  rows: [
    { planNo: 'WAFER-RATE-01', sampleType: 0, sampleRate: 10, everyNum: 0, samplePieceNum: 0, samplePointNum: 5, creator: 'DEV001', createDate: '2026-11-10T08:00:00+08:00', issueState: 0, functionName: null, aqlType: null, aql: null, qcLevel: null, rejectQty: 1, dataStamp: 0, reviser: null, reviseDate: null },
    { planNo: 'WAFER-COUNT-01', sampleType: 1, sampleRate: 0, everyNum: 10, samplePieceNum: 2, samplePointNum: 5, creator: 'DEV001', createDate: '2026-11-10T08:10:00+08:00', issueState: 0, functionName: null, aqlType: null, aql: null, qcLevel: null, rejectQty: 1, dataStamp: 0, reviser: 'DEV002', reviseDate: '2026-11-12T14:20:00+08:00' },
    { planNo: 'WAFER-RANGE-01', sampleType: 3, sampleRate: 0, everyNum: 0, samplePieceNum: 0, samplePointNum: 3, creator: 'DEV002', createDate: '2026-11-11T09:20:00+08:00', issueState: 2, functionName: null, aqlType: null, aql: null, qcLevel: null, rejectQty: null, dataStamp: 2, reviser: 'DEV003', reviseDate: '2026-11-13T11:15:00+08:00' },
    { planNo: 'WAFER-105E-01', sampleType: 5, sampleRate: 0, everyNum: 0, samplePieceNum: 0, samplePointNum: 5, creator: 'DEV002', createDate: '2026-11-11T10:00:00+08:00', issueState: 1, functionName: 'FUNSA105E', aqlType: '105E-NORMAL', aql: '1.0', qcLevel: 'II', rejectQty: 1, dataStamp: 1, reviser: 'DEV004', reviseDate: '2026-11-13T15:30:00+08:00' },
    { planNo: 'WAFER-GB2828-01', sampleType: 6, sampleRate: 0, everyNum: 0, samplePieceNum: 0, samplePointNum: 5, creator: 'DEV003', createDate: '2026-11-12T07:45:00+08:00', issueState: -1, functionName: 'FUNSAGB2828', aqlType: 'GB2828-NORMAL', aql: '1.0', qcLevel: 'II', rejectQty: 1, dataStamp: null, reviser: 'DEV003', reviseDate: '2026-11-14T10:00:00+08:00' },
  ],
  ranges: { 'WAFER-RANGE-01': [{ lowerLimit: 1, upperLimit: 10, sampleNum: 2 }, { lowerLimit: 11, upperLimit: 50, sampleNum: 5 }] },
  areas: {
    'WAFER-RATE-01': [{ area: 'CENTER', areaType: 0, areaNo: '0' }, { area: 'EDGE', areaType: 1, areaNo: '2' }],
    'WAFER-COUNT-01': [{ area: 'CENTER', areaType: 0, areaNo: '0' }],
    'WAFER-RANGE-01': [{ area: 'ALL', areaType: 0, areaNo: '0' }],
  },
}

const rows = ref<PlanRow[]>([])
const state = ref<IssueState>(0)
const searchValue = ref('')
const appliedSearchValue = ref('')
const activeKey = ref('')
const selectedKeys = ref<string[]>([])
const loading = ref(false)
const saving = ref(false)
const error = ref('')
const notice = ref('')
const formMode = ref<FormMode>(null)
const source = ref<PlanRow | null>(null)
const formError = ref('')
const dirty = ref(false)
const form = ref<PlanForm>(emptyForm())
const ranges = ref<RangeRow[]>([])
const areas = ref<AreaRow[]>([])
const rangeDraft = ref({ lowerLimit: '', upperLimit: '', sampleNum: '' })
const areaDraft = ref({ area: '', areaNo: '', enhance: false })
const createdBy = ref('')
const createdAt = ref('')
const revisedBy = ref('')
const revisedAt = ref('')
const aqlOptions = ref<{ aqlTypes: string[]; qcLevels: string[]; aqlValues: string[] }>({ aqlTypes: [], qcLevels: [], aqlValues: [] })

function emptyForm(): PlanForm { return { planNo: '', sampleType: 0, sampleRate: '', everyNum: '', samplePieceNum: '', samplePointNum: '5', rejectQty: '', aqlType: '', qcLevel: '', aql: '' } }
function keyOf(row: Pick<PlanRow, 'planNo'>): string { return row.planNo.toUpperCase() }
function clone<T>(value: T): T { return structuredClone(value) }
function nowIso(): string { return new Date().toISOString() }
function typeLabel(value: number): string { return sampleTypes.find(item => item.value === value)?.label ?? `SampleType ${value}` }
function readStore(): Store {
  try {
    const raw = window.localStorage.getItem(storageKey)
    if (raw) {
      const parsed = JSON.parse(raw) as Partial<Store>
      if (Array.isArray(parsed.rows) && parsed.ranges && parsed.areas) return parsed as Store
    }
  } catch { /* reset only development sample data */ }
  const data = clone(seed); writeStore(data); return data
}
function writeStore(data: Store): void { window.localStorage.setItem(storageKey, JSON.stringify(data)) }
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
    if (response.status === 404) throw new Error('QC104 API 尚未建立；請依 todolist_homepc.md 完成資料表與 API。')
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 回應錯誤（${response.status}）。`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
const visibleRows = computed(() => {
  const query = appliedSearchValue.value.trim().toUpperCase()
  return rows.value.filter(row => row.issueState === state.value && (!query || row.planNo.toUpperCase().includes(query)))
    .slice().sort((a, b) => a.planNo.localeCompare(b.planNo))
})
const activeRow = computed(() => rows.value.find(row => keyOf(row) === activeKey.value) ?? null)
const selectedRows = computed(() => visibleRows.value.filter(row => selectedKeys.value.includes(keyOf(row))))
const isReadOnly = computed(() => formMode.value === 'view')
const dialogTitle = computed(() => `Wafer Sample Plan (${formMode.value === 'add' ? 'ADD' : formMode.value === 'edit' ? 'EDIT' : formMode.value === 'copy' ? 'Copy' : 'View'})`)
const isAql = computed(() => form.value.sampleType === 5 || form.value.sampleType === 6)
const visibleAqlTypes = computed(() => {
  const prefix = form.value.sampleType === 5 ? '105E' : 'GB2828'
  const values = aqlOptions.value.aqlTypes.filter(item => item.toUpperCase().startsWith(prefix))
  return form.value.aqlType && !values.includes(form.value.aqlType) ? [form.value.aqlType, ...values] : values
})
const visibleQcLevels = computed(() => form.value.qcLevel && !aqlOptions.value.qcLevels.includes(form.value.qcLevel) ? [form.value.qcLevel, ...aqlOptions.value.qcLevels] : aqlOptions.value.qcLevels)
const visibleAqlValues = computed(() => form.value.aql && !aqlOptions.value.aqlValues.includes(form.value.aql) ? [form.value.aql, ...aqlOptions.value.aqlValues] : aqlOptions.value.aqlValues)

async function load(): Promise<void> {
  if (isMock.value) { rows.value = clone(readStore().rows); return }
  const query = new URLSearchParams()
  if (appliedSearchValue.value) { query.set('searchFieldName', 'PlanNo'); query.set('searchFieldValue', appliedSearchValue.value) }
  rows.value = await request<PlanRow[]>('api/qc/sampling-plans/wafer' + (query.toString() ? '?' + query.toString() : ''))
}
function setActive(prefer = ''): void {
  const row = visibleRows.value.find(item => keyOf(item) === prefer) ?? visibleRows.value[0]
  activeKey.value = row ? keyOf(row) : ''; selectedKeys.value = row ? [keyOf(row)] : []
}
async function refresh(prefer = ''): Promise<void> { await load(); setActive(prefer) }
async function query(): Promise<void> {
  appliedSearchValue.value = searchValue.value.trim(); loading.value = true; error.value = ''; notice.value = ''
  try { await load(); setActive() } catch (reason) { error.value = reason instanceof Error ? reason.message : '查詢 Wafer 抽樣計劃失敗。' }
  finally { loading.value = false }
}
function changeState(value: IssueState): void { state.value = value; notice.value = ''; setActive() }
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
function areaTypeLabel(value: number): string { return value === 1 ? 'Enhance' : 'General' }
function formatDate(value: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return new Intl.DateTimeFormat('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false }).format(date).replaceAll('-', '/')
}
async function loadAqlOptions(): Promise<void> {
  if (isMock.value) {
    aqlOptions.value = { aqlTypes: ['105E-NORMAL', '105E-TIGHTENED', 'GB2828-NORMAL', 'GB2828-TIGHTENED'], qcLevels: ['S-1', 'S-2', 'S-3', 'I', 'II', 'III'], aqlValues: ['0.010', '0.025', '0.040', '0.065', '0.10', '0.15', '0.25', '0.40', '0.65', '1.0', '1.5', '2.5', '4.0'] }
    return
  }
  try { aqlOptions.value = await request('api/qc/sampling-plans/wafer/aql-options') }
  catch (reason) { formError.value = reason instanceof Error ? reason.message : '載入 AQL 選項失敗。' }
}
function onSampleTypeChange(): void {
  form.value.sampleRate = ''
  form.value.everyNum = ''
  form.value.samplePieceNum = ''
  form.value.aqlType = ''
  form.value.qcLevel = ''
  form.value.aql = ''
  rangeDraft.value = { lowerLimit: '', upperLimit: '', sampleNum: '' }
  markDirty()
  if (isAql.value) void loadAqlOptions()
}
async function openForm(mode: Exclude<FormMode, null>, row: PlanRow | null = null): Promise<void> {
  source.value = row ? clone(row) : null
  form.value = row ? {
    planNo: mode === 'copy' ? '' : row.planNo, sampleType: row.sampleType, sampleRate: String(row.sampleRate ?? 0),
    everyNum: String(row.everyNum ?? 0), samplePieceNum: String(row.samplePieceNum ?? 0), samplePointNum: String(row.samplePointNum ?? 0),
    rejectQty: row.rejectQty == null ? '' : String(row.rejectQty), aqlType: row.aqlType ?? '', qcLevel: row.qcLevel ?? '', aql: row.aql ?? '',
  } : emptyForm()
  createdBy.value = mode === 'add' || mode === 'copy' ? (props.currentUserNo || 'DEV001') : (row?.creator ?? '')
  createdAt.value = mode === 'add' || mode === 'copy' ? nowIso() : (row?.createDate ?? '')
  revisedBy.value = mode === 'edit' ? (props.currentUserNo || 'DEV001') : (row?.reviser ?? '')
  revisedAt.value = mode === 'edit' ? nowIso() : (row?.reviseDate ?? '')
  ranges.value = []; areas.value = []; rangeDraft.value = { lowerLimit: '', upperLimit: '', sampleNum: '' }; areaDraft.value = { area: '', areaNo: '', enhance: false }
  formMode.value = mode; formError.value = ''; dirty.value = false
  if (isAql.value) await loadAqlOptions()
  if (row) {
    try {
      if (isMock.value) {
        const data = readStore(); ranges.value = clone(data.ranges[row.planNo] ?? []); areas.value = clone(data.areas[row.planNo] ?? [])
      } else {
        const details = await request<{ ranges: RangeRow[]; areas: AreaRow[] }>('api/qc/sampling-plans/wafer/' + encodeURIComponent(row.planNo) + '/details')
        ranges.value = details.ranges ?? []; areas.value = details.areas ?? []
      }
    } catch (reason) { formError.value = reason instanceof Error ? reason.message : '載入區間/區域資料失敗。' }
  }
}
function closeForm(): void {
  if (dirty.value && !window.confirm('資料尚未套用，確定關閉？')) return
  formMode.value = null; source.value = null; formError.value = ''; dirty.value = false; ranges.value = []; areas.value = []
}
function markDirty(): void { dirty.value = true; formError.value = '' }
function numberValue(value: string): number | null {
  const text = value.trim()
  if (!/^-?\d+$/.test(text)) return null
  const number = Number(text)
  return Number.isSafeInteger(number) ? number : null
}
function validate(): string {
  const planNo = form.value.planNo.trim()
  if (!planNo) return 'PlanNo 為必填。'
  if (planNo.length > 25) return 'PlanNo 不可超過 25 個字元。'
  if (formMode.value !== 'edit' && rows.value.some(row => row.planNo.toUpperCase() === planNo.toUpperCase())) return '相同 PlanNo 已存在。'
  if (!sampleTypes.some(type => type.value === form.value.sampleType)) return 'SampleType 無效。'
  if (form.value.sampleType === 0) {
    const rate = numberValue(form.value.sampleRate)
    if (rate === null || rate < 1 || rate > 100) return 'SampleRate 必須是 1 到 100 的整數百分比。'
  } else if (form.value.sampleType === 1) {
    const every = numberValue(form.value.everyNum); const piece = numberValue(form.value.samplePieceNum)
    if (every === null || every <= 0 || every > 9999) return 'EveryNum 必須是正整數。'
    if (piece === null || piece <= 0 || piece > every || piece > 9999) return 'SamplePieceNum 必須大於 0 且不可大於 EveryNum。'
  } else if (form.value.sampleType === 2) {
    const piece = numberValue(form.value.samplePieceNum)
    if (piece === null || piece <= 0 || piece > 9999) return 'Fixed Piece 必須是正整數。'
  } else if (form.value.sampleType === 3 && ranges.value.length === 0) return 'by Range 至少要設定一筆區間。'
  else if (isAql.value) {
    if (!form.value.aqlType) return 'AQLType 為必填。'
    if (!form.value.qcLevel) return 'QCLevel 為必填。'
    if (!form.value.aql) return 'AQL 為必填。'
  }
  const points = numberValue(form.value.samplePointNum)
  if (points === null || points <= 0 || points > 999999) return 'Point Per Piece 必須是正整數。'
  if (form.value.rejectQty.trim()) {
    const reject = numberValue(form.value.rejectQty)
    if (reject === null || reject < 0 || reject > 9999) return 'Reject Qty 必須是 0 到 9999 的整數。'
  }
  if (areas.value.some(area => !area.area.trim() || area.area.length > 20 || (area.areaNo?.length ?? 0) > 50)) return 'Sample Area 或 AreaNo 長度無效。'
  return ''
}
function addRange(): void {
  form.value.sampleType = 3
  const lower = numberValue(rangeDraft.value.lowerLimit); const upper = numberValue(rangeDraft.value.upperLimit); const sample = numberValue(rangeDraft.value.sampleNum)
  if (lower === null || lower <= 0 || lower > 2147483647) { formError.value = 'LowerLimit 必須是 1 到 2147483647 的整數。'; return }
  if (upper === null || upper <= 0 || upper > 2147483647) { formError.value = 'UpperLimit 必須是 1 到 2147483647 的整數。'; return }
  if (sample === null || sample <= 0 || sample > upper || sample > 2147483647) { formError.value = 'SampleNum 必須大於 0、不可大於 UpperLimit，且須為 Int32 整數。'; return }
  if (lower > upper) { formError.value = 'LowerLimit 不可大於 UpperLimit。'; return }
  ranges.value.push({ lowerLimit: lower, upperLimit: upper, sampleNum: sample }); rangeDraft.value = { lowerLimit: '', upperLimit: '', sampleNum: '' }; markDirty()
}
function removeRanges(): void { ranges.value = ranges.value.filter(row => !row._selected); markDirty() }
function addArea(): void {
  const area = areaDraft.value.area.trim(); const areaNo = areaDraft.value.areaNo.trim() || '0'
  if (!area) { formError.value = 'Sample Area 為必填。'; return }
  if (areas.value.some(item => item.area.toUpperCase() === area.toUpperCase())) { formError.value = 'Sample Area 不可重複。'; return }
  if (area.length > 20 || areaNo.length > 50) { formError.value = 'Sample Area 或 AreaNo 超出欄位長度。'; return }
  areas.value.push({ area, areaType: areaDraft.value.enhance ? 1 : 0, areaNo }); areaDraft.value = { area: '', areaNo: '', enhance: false }; markDirty()
}
function removeAreas(): void { areas.value = areas.value.filter(row => !row._selected); markDirty() }
async function save(keepOpen: boolean): Promise<void> {
  formError.value = validate()
  if (formError.value || !formMode.value || formMode.value === 'view') return
  const mode = formMode.value; const original = source.value
  const record: PlanRow = {
    planNo: form.value.planNo.trim().toUpperCase(), sampleType: form.value.sampleType,
    sampleRate: form.value.sampleType === 0 ? Number(form.value.sampleRate) : 0,
    everyNum: form.value.sampleType === 1 ? Number(form.value.everyNum) : 0,
    samplePieceNum: form.value.sampleType === 1 || form.value.sampleType === 2 ? Number(form.value.samplePieceNum) : 0,
    samplePointNum: Number(form.value.samplePointNum), creator: createdBy.value || null, createDate: createdAt.value || null,
    issueState: mode === 'edit' && original ? original.issueState : 0, functionName: null,
    aqlType: isAql.value ? form.value.aqlType : null, aql: isAql.value ? form.value.aql : null,
    qcLevel: isAql.value ? form.value.qcLevel : null, rejectQty: form.value.rejectQty.trim() ? Number(form.value.rejectQty) : null,
    dataStamp: original?.dataStamp ?? 0, reviser: revisedBy.value || null, reviseDate: revisedAt.value || null,
  }
  const payload = {
    sampleType: record.sampleType, sampleRate: record.sampleRate, everyNum: record.everyNum, samplePieceNum: record.samplePieceNum,
    samplePointNum: record.samplePointNum, rejectQty: record.rejectQty, aqlType: record.aqlType, aql: record.aql, qcLevel: record.qcLevel,
    ranges: record.sampleType === 3 ? ranges.value.map(({ lowerLimit, upperLimit, sampleNum }) => ({ lowerLimit, upperLimit, sampleNum })) : [],
    areas: areas.value.map(({ area, areaType, areaNo }) => ({ area, areaType, areaNo })),
  }
  saving.value = true
  try {
    if (isMock.value) {
      const data = readStore()
      if (mode === 'edit' && original) {
        const index = data.rows.findIndex(row => keyOf(row) === keyOf(original)); if (index < 0) throw new Error('資料已不存在，請重新查詢。')
        const prior = data.rows[index]
        data.rows[index] = { ...record, creator: prior.creator, createDate: prior.createDate, issueState: prior.issueState, reviser: props.currentUserNo || 'DEV001', reviseDate: nowIso(), dataStamp: (prior.dataStamp ?? 0) + 1 }
      } else data.rows.push({ ...record, creator: props.currentUserNo || 'DEV001', createDate: nowIso(), issueState: 0, reviser: null, reviseDate: null, dataStamp: 0 })
      data.ranges[record.planNo] = clone(payload.ranges); data.areas[record.planNo] = clone(payload.areas); writeStore(data)
    } else if (mode === 'edit' && original) {
      const query = new URLSearchParams({ issueState: String(original.issueState), dataStampIsNull: original.dataStamp === null ? 'true' : 'false' })
      if (original.dataStamp !== null) query.set('dataStamp', String(original.dataStamp))
      await request('api/qc/sampling-plans/wafer/' + encodeURIComponent(original.planNo) + '?' + query.toString(), { method: 'PUT', body: JSON.stringify(payload) })
    } else await request('api/qc/sampling-plans/wafer', { method: 'POST', body: JSON.stringify({ planNo: record.planNo, ...payload }) })
    await refresh(keyOf(record)); dirty.value = false
    if (keepOpen) {
      formMode.value = 'edit'; source.value = activeRow.value ? clone(activeRow.value) : null
      if (activeRow.value) { revisedBy.value = activeRow.value.reviser ?? ''; revisedAt.value = activeRow.value.reviseDate ?? '' }
      notice.value = '資料已套用。'
    } else closeForm()
  } catch (reason) { formError.value = reason instanceof Error ? reason.message : '儲存 Wafer 抽樣計劃失敗。' }
  finally { saving.value = false }
}
async function removeSelected(): Promise<void> {
  const items = [...selectedRows.value]; if (!items.length) return
  if (items.some(row => row.issueState === 1)) { error.value = '簽核中的資料不可刪除。'; return }
  if (!window.confirm(`確定刪除或停用選取的 ${items.length} 筆 Wafer 抽樣計劃？`)) return
  loading.value = true; error.value = ''
  try {
    for (const row of items) {
      if (isMock.value) {
        const data = readStore()
        if (row.issueState === 2) { const current = data.rows.find(item => keyOf(item) === keyOf(row))!; current.issueState = -1; current.reviser = props.currentUserNo || 'DEV001'; current.reviseDate = nowIso(); current.dataStamp = (current.dataStamp ?? 0) + 1 }
        else { data.rows = data.rows.filter(item => keyOf(item) !== keyOf(row)); delete data.ranges[row.planNo]; delete data.areas[row.planNo] }
        writeStore(data)
      } else {
        const query = new URLSearchParams({ issueState: String(row.issueState), dataStampIsNull: row.dataStamp === null ? 'true' : 'false' })
        if (row.dataStamp !== null) query.set('dataStamp', String(row.dataStamp))
        await request('api/qc/sampling-plans/wafer/' + encodeURIComponent(row.planNo) + '?' + query.toString(), { method: 'DELETE' })
      }
    }
    await refresh(); notice.value = '操作完成。'
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '刪除 Wafer 抽樣計劃失敗。' }
  finally { loading.value = false }
}
async function approveSelected(): Promise<void> {
  const items = [...selectedRows.value]; if (!items.length) return
  if (!window.confirm(`確定核准選取的 ${items.length} 筆 Wafer 抽樣計劃？`)) return
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
        await request('api/qc/sampling-plans/wafer/' + encodeURIComponent(row.planNo) + '/approve?' + query.toString(), { method: 'POST' })
      }
    }
    await refresh(); notice.value = '核准申請完成。'
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '核准 Wafer 抽樣計劃失敗。' }
  finally { loading.value = false }
}
function closePage(): void { emit('close') }
onMounted(async () => { loading.value = true; try { await load(); setActive() } catch (reason) { error.value = reason instanceof Error ? reason.message : '載入 Wafer 抽樣計劃失敗。' } finally { loading.value = false } })
</script>

<template>
  <section class="wafer-plan-page">
    <header class="page-title"><h1>Wafer Sampling Plan</h1><span v-if="isMock" class="preview-tag">DEV 假資料</span></header>
    <div class="search-bar">
      <label v-for="option in stateOptions" :key="option.value"><input type="radio" :checked="state === option.value" @change="changeState(option.value)">{{ option.label }}</label>
      <label class="search-field"><span>Plan No</span><input v-model="searchValue" class="kd-field" @keydown.enter="query"></label>
      <span class="percent">%</span><button type="button" @click="query">查詢</button>
    </div>
    <p v-if="error" class="message error" role="alert">{{ error }}</p><p v-else-if="notice" class="message notice">{{ notice }}</p>
    <div class="master-layout">
      <div class="grid-frame">
        <table class="plan-grid"><thead><tr>
          <th class="selector"><input type="checkbox" :checked="visibleRows.length > 0 && selectedRows.length === visibleRows.length" aria-label="選取全部" @change="selectVisible"></th>
          <th>Plan No</th><th>SampleType</th><th>Sample Rate</th><th>Every Num</th><th>Sample Piece Num</th><th>Sample Point Num</th><th>Creator</th><th>Create Date</th><th>Reviser</th><th>Revise Date</th>
        </tr></thead><tbody>
          <tr v-for="row in visibleRows" :key="keyOf(row)" :class="{ active: activeKey === keyOf(row), selected: selectedKeys.includes(keyOf(row)) }" @click="selectRow(row)" @dblclick="openForm(row.issueState === 0 ? 'edit' : 'view', row)">
            <td class="selector" @click.stop><input type="checkbox" :checked="selectedKeys.includes(keyOf(row))" :aria-label="`選取 ${row.planNo}`" @change="toggleRow(row)"></td>
            <td>{{ row.planNo }}</td><td>{{ typeLabel(row.sampleType) }}</td><td class="number">{{ row.sampleRate || '' }}</td><td class="number">{{ row.everyNum || '' }}</td><td class="number">{{ row.samplePieceNum || '' }}</td><td class="number">{{ row.samplePointNum || '' }}</td>
            <td>{{ row.creator ?? '' }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser ?? '' }}</td><td>{{ formatDate(row.reviseDate) }}</td>
          </tr>
          <tr v-if="!visibleRows.length"><td class="empty" colspan="11">{{ loading ? '載入中…' : '沒有資料' }}</td></tr>
        </tbody></table>
      </div>
      <aside class="actions" aria-label="資料操作">
        <button class="primary" type="button" @click="openForm('add')">ADD (&amp;A)</button>
        <button type="button" :disabled="!activeRow || activeRow.issueState !== 0" @click="activeRow && openForm('edit', activeRow)">EDIT (&amp;E)</button>
        <button type="button" :disabled="!selectedRows.length" @click="removeSelected">DEL (&amp;D)</button>
        <button type="button" :disabled="!activeRow" @click="activeRow && openForm('copy', activeRow)">Copy (&amp;C)</button>
        <button type="button" :disabled="!selectedRows.length || state !== 0" @click="approveSelected">Approve (&amp;Y)</button>
        <button type="button" :disabled="!activeRow" @click="activeRow && openForm('view', activeRow)">View (&amp;V)</button>
        <button class="close" type="button" @click="closePage">Close (&amp;X)</button>
      </aside>
    </div>
    <footer class="grid-footer">資料總筆數：{{ visibleRows.length }}</footer>

    <div v-if="formMode" class="backdrop" @click.self="closeForm">
      <section class="dialog" role="dialog" aria-modal="true" :aria-label="dialogTitle">
        <header class="dialog-title"><h2>{{ dialogTitle }}</h2><button type="button" class="x-button" aria-label="關閉" @click="closeForm">×</button></header>
        <form class="dialog-body" @submit.prevent="save(false)">
          <fieldset class="sampling-plan-group"><legend>Sampling Plan</legend>
            <label class="field-row"><span>Plan No</span><input v-model="form.planNo" class="kd-field" maxlength="25" :readonly="isReadOnly || formMode === 'edit'" @blur="form.planNo = form.planNo.toUpperCase()" @input="markDirty"></label>
            <div class="sample-types"><span class="field-label">Sample Type</span><div class="radio-list">
              <label v-for="option in sampleTypes" :key="option.value"><input v-model.number="form.sampleType" type="radio" :value="option.value" :disabled="isReadOnly" @change="onSampleTypeChange">{{ option.label }}</label>
            </div></div>
            <div class="type-fields">
              <label v-if="form.sampleType === 0" class="field-row"><span>Sample Rate</span><input v-model="form.sampleRate" class="kd-field number" inputmode="numeric" :readonly="isReadOnly" @input="markDirty"><small>%</small></label>
              <template v-else-if="form.sampleType === 1"><label class="field-row"><span>Every</span><input v-model="form.everyNum" class="kd-field number" inputmode="numeric" :readonly="isReadOnly" @input="markDirty"><small>Wafer</small></label><label class="field-row"><span>Draw Out</span><input v-model="form.samplePieceNum" class="kd-field number" inputmode="numeric" :readonly="isReadOnly" @input="markDirty"><small>Piece</small></label></template>
              <label v-else-if="form.sampleType === 2" class="field-row"><span>Draw Out</span><input v-model="form.samplePieceNum" class="kd-field number" inputmode="numeric" :readonly="isReadOnly" @input="markDirty"><small>Piece</small></label>
              <div v-else-if="form.sampleType === 3" class="range-panel">
                <fieldset class="subgroup"><legend>by Range</legend>
                  <div class="range-entry"><label><span>Lower Limit</span><input v-model="rangeDraft.lowerLimit" class="kd-field number" inputmode="numeric" :disabled="isReadOnly"></label><label><span>Upper Limit</span><input v-model="rangeDraft.upperLimit" class="kd-field number" inputmode="numeric" :disabled="isReadOnly"></label><label><span>Sample Num</span><input v-model="rangeDraft.sampleNum" class="kd-field number" inputmode="numeric" :disabled="isReadOnly"></label><button type="button" :disabled="isReadOnly" @click="addRange">Add</button><button type="button" :disabled="isReadOnly" @click="removeRanges">Del</button></div>
                  <div class="subgrid-wrap"><table class="subgrid"><thead><tr><th></th><th>LowerLimit</th><th>UpperLimit</th><th>SampleNum</th></tr></thead><tbody><tr v-for="(row, index) in ranges" :key="`${row.lowerLimit}-${row.upperLimit}-${index}`"><td><input v-model="row._selected" type="checkbox" :disabled="isReadOnly"></td><td class="number">{{ row.lowerLimit }}</td><td class="number">{{ row.upperLimit }}</td><td class="number">{{ row.sampleNum }}</td></tr><tr v-if="!ranges.length"><td colspan="4" class="empty">沒有區間資料</td></tr></tbody></table></div>
                </fieldset>
              </div>
              <div v-else-if="isAql" class="aql-fields">
                <label class="field-row"><span>AQL Type</span><select v-model="form.aqlType" class="kd-field" :disabled="isReadOnly" @change="markDirty"><option value="">請選擇</option><option v-for="value in visibleAqlTypes" :key="value">{{ value }}</option></select></label>
                <label class="field-row"><span>QC Level</span><select v-model="form.qcLevel" class="kd-field" :disabled="isReadOnly" @change="markDirty"><option value="">請選擇</option><option v-for="value in visibleQcLevels" :key="value">{{ value }}</option></select></label>
                <label class="field-row"><span>AQL</span><select v-model="form.aql" class="kd-field" :disabled="isReadOnly" @change="markDirty"><option value="">請選擇</option><option v-for="value in visibleAqlValues" :key="value">{{ value }}</option></select></label>
              </div>
              <p v-else-if="form.sampleType === 4" class="legacy-note">此 Plan 使用 WinForms Service 保留的 User Define 類型；原始定義窗沒有對應選項。</p>
            </div>
          </fieldset>
          <fieldset class="draw-group"><legend>Sample Draw Out</legend>
            <label class="point-toggle"><input type="radio" checked disabled> Fixed</label>
            <label class="field-row"><span>Point Per Piece</span><input v-model="form.samplePointNum" class="kd-field number" inputmode="numeric" :readonly="isReadOnly" @input="markDirty"></label>
            <label class="field-row"><span>Reject Qty</span><input v-model="form.rejectQty" class="kd-field number" inputmode="numeric" :readonly="isReadOnly" @input="markDirty"></label>
          </fieldset>
          <fieldset class="area-group"><legend>Sample Area</legend>
            <div class="area-entry"><label><span>Sample Area</span><input v-model="areaDraft.area" class="kd-field" maxlength="20" :disabled="isReadOnly"></label><label><span>AreaNo</span><input v-model="areaDraft.areaNo" class="kd-field" maxlength="50" :disabled="isReadOnly" placeholder="0"></label><label class="enhance"><input v-model="areaDraft.enhance" type="checkbox" :disabled="isReadOnly">Enhance</label><button type="button" :disabled="isReadOnly" @click="addArea">Add</button><button type="button" :disabled="isReadOnly" @click="removeAreas">Del</button></div>
            <div class="subgrid-wrap area-grid-wrap"><table class="subgrid"><thead><tr><th></th><th>Area</th><th>AreaType</th><th>AreaNo</th></tr></thead><tbody><tr v-for="(row, index) in areas" :key="`${row.area}-${index}`"><td><input v-model="row._selected" type="checkbox" :disabled="isReadOnly"></td><td>{{ row.area }}</td><td>{{ areaTypeLabel(row.areaType) }}</td><td>{{ row.areaNo ?? '' }}</td></tr><tr v-if="!areas.length"><td colspan="4" class="empty">沒有 Sample Area</td></tr></tbody></table></div>
          </fieldset>
          <fieldset class="audit-group"><legend>Basis Data</legend><label><span>Creator</span><input :value="createdBy" readonly></label><label><span>Create Date</span><input :value="formatDate(createdAt)" readonly></label><label><span>Reviser</span><input :value="revisedBy" readonly></label><label><span>Revise Date</span><input :value="formatDate(revisedAt)" readonly></label></fieldset>
          <p v-if="formError" class="form-error" role="alert">{{ formError }}</p>
          <div class="dialog-actions"><button v-if="!isReadOnly" class="primary" type="submit" :disabled="saving">Confirm</button><button type="button" @click="closeForm">Close</button><button v-if="!isReadOnly" type="button" :disabled="saving || !dirty" @click="save(true)">Apply</button></div>
        </form>
      </section>
    </div>
  </section>
</template>

<style scoped>
.wafer-plan-page{display:flex;flex-direction:column;gap:8px;min-width:0;height:100%;padding:12px;background:#fff;color:#282722;font-size:13px}.page-title{display:flex;align-items:center;gap:12px;min-height:34px;border-bottom:1px solid #dedbd4}.page-title h1{margin:0;font-size:20px;font-weight:600}.preview-tag{padding:3px 8px;border:1px solid #d8c57d;border-radius:3px;background:#fff4bf;color:#66551b;font-size:11px}.search-bar{display:flex;align-items:center;gap:12px;min-height:40px;padding:4px 10px;border:1px solid #d7d4cd;background:#faf9f6}.search-bar>label:not(.search-field){display:flex;align-items:center;gap:4px;white-space:nowrap}.search-bar input[type=radio],.point-toggle input,.radio-list input,.enhance input{accent-color:#666}.search-field{display:flex;align-items:center;gap:8px;margin-left:8px}.kd-field{height:25px;padding:3px 6px;border:1px solid #99958c;border-radius:0;background:#fff0ad;color:#282722}.search-field .kd-field{width:180px;background:#fff}.search-bar .percent{margin-left:-8px}.search-bar button,.range-entry button,.area-entry button,.actions button,.dialog-actions button{min-height:30px;padding:4px 9px;border:1px solid #c5c1b8;border-radius:3px;background:#f8f7f3;color:#333;cursor:pointer}.search-bar button:hover,.actions button:hover,.dialog-actions button:hover,.range-entry button:hover,.area-entry button:hover{background:#efede7}.message{margin:0;padding:6px 9px;border:1px solid #d8d2c4}.message.error{background:#fff0ed;color:#902b1d}.message.notice{background:#f5f2e9;color:#595548}.master-layout{display:grid;grid-template-columns:minmax(0,1fr) 88px;gap:10px;min-height:250px;flex:1}.grid-frame{min-width:0;overflow:auto;border:1px solid #c9c6bf;background:#fff}.plan-grid{width:100%;border-collapse:collapse;white-space:nowrap}.plan-grid th,.plan-grid td{height:29px;padding:4px 7px;border:1px solid #d0cdc6;text-align:left}.plan-grid th{position:sticky;top:0;z-index:1;background:#e8e6e0;color:#37352f;font-weight:600}.plan-grid tbody tr{cursor:pointer}.plan-grid tbody tr:hover{background:#fff8dc}.plan-grid tbody tr.active{outline:1px solid #a9975b;outline-offset:-1px}.plan-grid tbody tr.selected{background:#fff0b5}.selector{width:28px;padding:2px!important;text-align:center!important}.selector input{margin:0;accent-color:#776328}.number{text-align:right;font-variant-numeric:tabular-nums}.empty{height:40px;color:#777;text-align:center}.actions{display:flex;flex-direction:column;gap:7px}.actions button:disabled,.dialog-actions button:disabled,.range-entry button:disabled,.area-entry button:disabled{opacity:.5;cursor:default}.actions .primary,.dialog-actions .primary{border-color:#da941b;background:#ffab2e;color:#fff;font-weight:600}.actions .primary:hover,.dialog-actions .primary:hover{background:#f29b20}.actions .close{margin-top:auto}.grid-footer{min-height:20px;color:#77746e;font-variant-numeric:tabular-nums}.backdrop{position:fixed;z-index:60;inset:0;display:flex;align-items:center;justify-content:center;padding:14px;background:rgb(28 27 25 / 42%)}.dialog{display:flex;flex-direction:column;width:min(632px,98vw);max-height:96vh;overflow:auto;border:1px solid #b9b4aa;background:#f8f6f1;box-shadow:0 12px 36px rgb(0 0 0 / 24%)}.dialog-title{position:sticky;top:0;z-index:2;display:flex;align-items:center;justify-content:space-between;padding:9px 14px;border-bottom:1px solid #d2cec5;background:#f2efe8}.dialog-title h2{margin:0;font-size:17px;font-weight:600}.x-button{border:0;background:transparent;font-size:23px;cursor:pointer}.dialog-body{display:flex;flex-direction:column;gap:8px;padding:9px}.dialog-body fieldset{min-width:0;border:1px solid #c9c4ba;padding:8px 10px}.dialog-body legend{padding:0 5px;color:#5a564f}.sampling-plan-group{display:grid;grid-template-columns:145px minmax(0,1fr);gap:7px 12px}.field-row{display:grid;grid-template-columns:104px minmax(90px,190px) auto;align-items:center;gap:8px;min-height:26px}.field-row>span{text-align:right}.field-row small{color:#5f5b53}.field-row .kd-field{width:100%}.field-row .kd-field[readonly]{background:#efede7}.field-row .kd-field:disabled{background:#ecebe7;color:#777}.sample-types{display:contents}.field-label{text-align:right;align-self:start;padding-top:6px}.radio-list{display:grid;grid-template-columns:1fr 1fr;gap:4px 10px}.radio-list label{display:flex;align-items:center;gap:4px;white-space:nowrap}.type-fields{grid-column:2;display:flex;flex-direction:column;gap:6px;min-width:0}.type-fields>.field-row{grid-template-columns:104px minmax(90px,190px) auto}.range-panel,.aql-fields{width:100%}.subgroup{padding:5px 8px!important}.subgroup legend{font-size:12px}.range-entry,.area-entry{display:flex;align-items:end;flex-wrap:wrap;gap:6px;margin-bottom:7px}.range-entry label,.area-entry label:not(.enhance){display:flex;flex-direction:column;gap:3px}.range-entry label span,.area-entry label span{font-size:11px;color:#58554e}.range-entry .kd-field{width:88px}.range-entry button,.area-entry button{min-height:25px;padding:2px 8px}.subgrid-wrap{max-height:135px;overflow:auto;border:1px solid #c9c6bf}.subgrid{width:100%;border-collapse:collapse;white-space:nowrap}.subgrid th,.subgrid td{height:25px;padding:3px 6px;border:1px solid #d0cdc6;text-align:left}.subgrid th{position:sticky;top:0;background:#e8e6e0}.subgrid td:first-child{width:24px;text-align:center}.subgrid input{accent-color:#776328}.subgrid .empty{height:35px}.area-entry .kd-field{width:130px}.area-entry label:nth-child(2) .kd-field{width:90px}.area-entry .enhance{display:flex;align-items:center;gap:4px;min-height:26px;white-space:nowrap}.area-grid-wrap{max-height:105px}.draw-group{display:grid;grid-template-columns:145px 1fr 1fr;gap:8px 12px;align-items:center}.draw-group legend{grid-column:1/-1}.point-toggle{grid-column:1;display:flex;align-items:center;gap:5px;justify-self:end}.draw-group .field-row{grid-template-columns:104px minmax(80px,130px)}.audit-group{display:grid;grid-template-columns:1fr 1fr;gap:6px 9px}.audit-group label{display:grid;grid-template-columns:78px minmax(0,1fr);align-items:center;gap:5px}.audit-group label>span{text-align:right}.audit-group input{width:100%;height:24px;padding:2px 5px;border:1px solid #c7c4bd;background:#ecebe7;color:#555}.legacy-note{margin:4px 0;color:#756331}.form-error{margin:0;color:#a12e20}.dialog-actions{position:sticky;bottom:0;display:flex;justify-content:flex-end;gap:8px;padding-top:6px;background:#f8f6f1}.dialog-actions button{min-width:76px}.grid-footer{font-variant-numeric:tabular-nums}
@media(max-width:760px){.wafer-plan-page{padding:8px}.search-bar{flex-wrap:wrap}.master-layout{grid-template-columns:1fr;grid-template-rows:minmax(220px,1fr) auto}.actions{display:grid;grid-template-columns:repeat(4,minmax(0,1fr))}.actions .close{margin-top:0}.plan-grid{min-width:1080px}.sampling-plan-group{grid-template-columns:1fr}.sample-types{display:grid;grid-template-columns:104px 1fr;gap:8px}.type-fields{grid-column:2}.draw-group{grid-template-columns:1fr}.point-toggle{grid-column:1;justify-self:start}.draw-group .field-row{grid-template-columns:104px minmax(90px,1fr)}.audit-group{grid-template-columns:1fr}.area-entry{align-items:start}}
</style>
