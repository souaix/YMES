<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type IssueState = 0 | 1 | 2 | -1
type EditorMode = 'add' | 'edit' | 'copy'
type ChildMode = 'parameters' | 'factors' | 'simulation' | 'operations' | 'samplePlanChanges' | 'memos'
interface VariableQcItem {
  qcItemNo: string; qcItemName: string; chartType: number; specLineType: number; componentCollect: number; lotIsValid: number
  planNo: string; chartControl: number; collectType: number; limitLotNum: number; sampleSize: number | null; unit: string
  qcDataType: number; minValue: number; maxValue: number; description: string; roundoffNum: number | null
  qcClass: string; mClassNo: string; calType: number; formName: string; errorReportName: string; noticeReportName: string
  webUrl: string; executionFile: string; issueState: IssueState; dataStamp: number
  creator: string; createDate: string; reviser: string; reviseDate: string
}
interface ParameterRow { parameterNo: string; parameterValue: string }
interface FactorRow { factorNo: string; factorOnline: string; factorSpec: string; opNo: string; tableName: string; fieldName: string; factorShortRun: string }
interface OperationRow { opNo: string; opName: string; inline: number; executeOrder: number; qcItemType: number; inspTypeFai: boolean; inspTypeIpqc: boolean; inspTypePqc: boolean }
interface SamplePlanChangeRow { oldSamplePlan: string; accLotQty: number; rejLotQty: number; newSamplePlan: string; factorNo: string; lotRange: number; aqlChkForSPlanChg: number }
interface MockStore { items: VariableQcItem[]; parameters: Record<string, ParameterRow[]>; factors: Record<string, FactorRow[]>; operations: Record<string, OperationRow[]>; samplePlanChanges: Record<string, SamplePlanChangeRow[]>; rules: Record<string, string[]>; memos: Record<string, string[]> }
interface EditorForm {
  qcItemNo: string; qcItemName: string; lotIsValid: number | null; componentCollect: number | null; calType: number
  planNo: string; formName: string; chartControl: number; chartType: number | null; specLineType: number | null
  collectType: number; qcDataType: number; minValue: string; maxValue: string; limitLotNum: string; sampleSize: string
  qcClass: string; unit: string; mClassNo: string; description: string; roundoffNum: string
}
interface CopyFlags { parameters: boolean; factors: boolean; rules: boolean; samplePlanChanges: boolean }
type Payload<T> = T[] | { items?: T[]; rows?: T[]; data?: T[] }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const apiRoot = 'api/qc/variable-qc-items'
const mockKey = 'ymes-dev-mock:qc-variable-item:theil-bu1:v1'
const currentUser = computed(() => props.currentUserNo || 'DEV001')
const items = ref<VariableQcItem[]>([])
const issueState = ref<IssueState>(0)
const searchText = ref('')
const selectedNos = ref<string[]>([])
const activeNo = ref('')
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const notice = ref('')
const editorOpen = ref(false)
const editorMode = ref<EditorMode>('add')
const editorError = ref('')
const editorForm = ref<EditorForm>(blankForm())
const editingStamp = ref(0)
const copyFlags = ref<CopyFlags>({ parameters: true, factors: true, rules: true, samplePlanChanges: true })
const copyParameterSource = ref<'qcItem' | 'samplingPlan'>('qcItem')
const copyParametersResolved = ref(false)
const copySourcePromptOpen = ref(false)
const copyQcParameters = ref<ParameterRow[]>([])
const copyPlanParameters = ref<ParameterRow[]>([])
const planOptions = ref(['PLAN-VAR-01', 'PLAN-VAR-02', 'PLAN-WAFER-01'])
const interfaceOptions = ref(['frmSPCVariableMeasureData', 'frmSPCVariableMeasureDataForComponent'])
const mClassOptions = ref(['MC-01', 'MC-02'])
const parameterOptions = ref(['THICKNESS', 'POSITION', 'SHIFT', 'LINE', 'VERSION', 'MATERIAL', 'EQUIPMENT', 'PROCESS', 'CUSTOMER'])
const devPlanParameters: Record<string, ParameterRow[]> = { 'PLAN-VAR-01': [{ parameterNo: 'THICKNESS', parameterValue: '2' }, { parameterNo: 'POSITION', parameterValue: '1' }], 'PLAN-VAR-02': [{ parameterNo: 'THICKNESS', parameterValue: '2' }, { parameterNo: 'POSITION', parameterValue: '3' }], 'PLAN-WAFER-01': [{ parameterNo: 'PARTICLE_SIZE', parameterValue: '0.5' }] }
const factorOptions = ref(['TEMPERATURE', 'PRESSURE', 'VISCOSITY', 'WEIGHT'])
const operationOptions = ref([{ opNo: 'OP-MEASURE', opName: '尺寸量測' }, { opNo: 'OP-PACK', opName: '包裝' }, { opNo: 'OP-FINAL', opName: '成品檢驗' }])
const showQcClass = ref(false)
const childOpen = ref(false)
const childMode = ref<ChildMode>('parameters')
const childRows = ref<Array<Record<string, string | number | boolean>>>([])
const childSelection = ref<string[]>([])
const childCandidate = ref('')
const childError = ref('')
const childNotice = ref('')
const simulationRows = ref<Array<Record<string, string | number>>>([])
const simulationMessage = ref('')
const childSearch = ref('')
const lookupOpen = ref(false)
const lookupMode = ref<'plan' | 'mclass'>('plan')

const statusOptions: Array<{ value: IssueState; label: string }> = [
  { value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' },
]
const chartTypes = ['X-R', 'X-S', 'X-Rm']
const specLineTypes = ['Upper/Lower Control', 'Upper Control', 'Lower Control', 'Not Control']
const qcDataTypes = ['Particle', 'InLine', 'OffLine']
const issueLabel = (value: number): string => statusOptions.find(option => option.value === value)?.label ?? String(value)
const chartLabel = (value: number): string => chartTypes[value] ?? ''
const specLabel = (value: number): string => specLineTypes[value] ?? ''
const yesNo = (value: number): string => value === 1 ? 'Yes' : value === 0 ? 'No' : ''
const collectLabel = (value: number): string => value === 1 ? 'Automatic' : 'Manual'
const dataTypeLabel = (value: number): string => qcDataTypes[value] ?? ''
const visibleItems = computed(() => items.value.filter(row => row.issueState === issueState.value)
  .filter(row => !searchText.value.trim() || row.qcItemNo.toLowerCase().includes(searchText.value.trim().toLowerCase()))
  .sort((left, right) => left.qcItemNo.localeCompare(right.qcItemNo)))
const activeItem = computed(() => items.value.find(row => row.qcItemNo === activeNo.value) ?? null)
const selectedRows = computed(() => visibleItems.value.filter(row => selectedNos.value.includes(row.qcItemNo)))
const childTitle = computed(() => ({
  parameters: 'Parameter', factors: 'Factor', simulation: 'Sampling Parameter Simulation',
  operations: 'OP', samplePlanChanges: 'SamplePlanChange', memos: 'QCItem Memo Setting',
}[childMode.value]))
const childEditable = computed(() => Boolean(activeItem.value && activeItem.value.issueState !== 1 && childMode.value !== 'simulation'))
const filteredParameterOptions = computed(() => parameterOptions.value.filter(item => item.toLowerCase().includes(childSearch.value.toLowerCase())))
const lookupItems = computed(() => (lookupMode.value === 'plan' ? planOptions.value : mClassOptions.value)
  .filter(value => value.toLowerCase().includes(childSearch.value.toLowerCase())))
const candidateOptions = computed(() => childMode.value === 'factors' ? factorOptions.value
  : childMode.value === 'operations' ? operationOptions.value.map(row => row.opNo) : parameterOptions.value)

function blankForm(): EditorForm {
  return { qcItemNo: '', qcItemName: '', lotIsValid: 0, componentCollect: 0, calType: 0, planNo: '', formName: '',
    chartControl: 0, chartType: 0, specLineType: 0, collectType: 0, qcDataType: 0, minValue: '-1000000',
    maxValue: '1000000', limitLotNum: '10', sampleSize: '', qcClass: '', unit: '', mClassNo: '', description: '', roundoffNum: '' }
}
function demoRow(no: string, name: string, state: IssueState, options: Partial<VariableQcItem> = {}): VariableQcItem {
  const date = state === 0 ? '2026/11/17 08:10:00' : '2026/11/16 15:20:00'
  return {
    qcItemNo: no, qcItemName: name, chartType: 0, specLineType: 0, componentCollect: 0, lotIsValid: 1,
    planNo: 'PLAN-VAR-01', chartControl: 1, collectType: 0, limitLotNum: 10, sampleSize: 5, unit: 'mm',
    qcDataType: 0, minValue: 0, maxValue: 100, description: 'DEV 假資料', roundoffNum: 2, qcClass: '', mClassNo: '',
    calType: 0, formName: 'frmSPCVariableMeasureData', errorReportName: '', noticeReportName: '', webUrl: '',
    executionFile: 'QC_VariableMeasure.dll', issueState: state, dataStamp: 0, creator: 'DEV001', createDate: date,
    reviser: state === 2 ? 'DEV001' : '', reviseDate: state === 2 ? date : '', ...options,
  }
}
function makeStore(): MockStore {
  return {
    items: [
      demoRow('DEV-QCV-001', '厚度量測（DEMO）', 0),
      demoRow('DEV-QCV-002', '晶圓顆粒尺寸（DEMO）', 1, { chartType: 1, componentCollect: 1, calType: 1, qcDataType: 1, planNo: 'PLAN-WAFER-01' }),
      demoRow('DEV-QCV-003', '外觀尺寸檢驗（DEMO）', 2, { chartType: 2, sampleSize: 1, qcDataType: 2, reviser: 'DEV001', reviseDate: '2026/11/17 09:00:00' }),
      demoRow('DEV-QCV-004', '舊版量測項目（DEMO）', -1, { chartControl: 0, description: '已停用的 DEV 假資料' }),
    ],
    parameters: { 'DEV-QCV-001': [{ parameterNo: 'THICKNESS', parameterValue: '2' }, { parameterNo: 'POSITION', parameterValue: '1' }], 'DEV-QCV-002': [{ parameterNo: 'PARTICLE_SIZE', parameterValue: '0.5' }] },
    factors: {
      'DEV-QCV-001': [{ factorNo: 'TEMPERATURE', factorOnline: 'Y', factorSpec: 'Y', opNo: 'OP-MEASURE', tableName: '', fieldName: '', factorShortRun: 'N' }],
      'DEV-QCV-002': [{ factorNo: 'PRESSURE', factorOnline: 'Y', factorSpec: 'Y', opNo: 'OP-MEASURE', tableName: '', fieldName: '', factorShortRun: 'N' }],
      'DEV-QCV-003': [{ factorNo: 'VISCOSITY', factorOnline: 'Y', factorSpec: 'Y', opNo: 'OP-FINAL', tableName: '', fieldName: '', factorShortRun: 'N' }],
    },
    operations: { 'DEV-QCV-003': [{ opNo: 'OP-FINAL', opName: '成品檢驗', inline: 1, executeOrder: 10, qcItemType: 1, inspTypeFai: true, inspTypeIpqc: true, inspTypePqc: true }] },
    samplePlanChanges: { 'DEV-QCV-003': [{ oldSamplePlan: 'PLAN-VAR-01', accLotQty: 5, rejLotQty: 0, newSamplePlan: 'PLAN-VAR-02', factorNo: 'VISCOSITY', lotRange: 1, aqlChkForSPlanChg: 1 }] },
    rules: { 'DEV-QCV-001': ['RULE-VAR-01'] },
    memos: { 'DEV-QCV-001': ['THICKNESS', 'SHIFT'], 'DEV-QCV-003': ['POSITION', 'VERSION'] },
  }
}
function readStore(): MockStore {
  try { const raw = localStorage.getItem(mockKey); if (raw) { const value = JSON.parse(raw) as MockStore; if (Array.isArray(value.items)) return value } }
  catch { /* reset isolated DEV data */ }
  const store = makeStore(); saveStore(store); return store
}
function saveStore(store: MockStore): void { localStorage.setItem(mockKey, JSON.stringify(store)) }
function toList<T>(payload: Payload<T>): T[] { return Array.isArray(payload) ? payload : payload.items ?? payload.rows ?? payload.data ?? [] }
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', headers: { Accept: 'application/json',
    ...(init.body ? { 'Content-Type': 'application/json' } : {}), ...init.headers }, ...init })
  if (!response.ok) throw new Error((await response.text()) || ('API 回應 ' + response.status))
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function setNotice(message: string): void { notice.value = message; errorMessage.value = '' }
function setError(error: unknown): void { errorMessage.value = error instanceof Error ? error.message : String(error); notice.value = '' }
function dateNow(): string { return new Date().toLocaleString('zh-TW', { hour12: false }) }
async function loadOptions(): Promise<void> {
  if (mock.value) return
  try {
    const options = await request<{ plans?: string[]; forms?: string[]; mClasses?: string[]; parameters?: string[]; factors?: string[]; operations?: Array<{ opNo: string; opName: string }>; qcClassRequired?: boolean }>(apiRoot + '/options')
    planOptions.value = options.plans ?? []
    interfaceOptions.value = options.forms ?? []
    mClassOptions.value = options.mClasses ?? []
    parameterOptions.value = options.parameters ?? []
    factorOptions.value = options.factors ?? []
    operationOptions.value = options.operations ?? []
    showQcClass.value = Boolean(options.qcClassRequired)
  } catch (error) { setError(error) }
}
async function loadItems(): Promise<void> {
  loading.value = true; errorMessage.value = ''
  try {
    if (mock.value) items.value = readStore().items
    else {
      const query = new URLSearchParams({ issueState: String(issueState.value) })
      if (searchText.value.trim()) query.set('qcItemNo', searchText.value.trim())
      items.value = toList(await request<Payload<VariableQcItem>>(apiRoot + '?' + query.toString()))
    }
    if (!items.value.some(row => row.qcItemNo === activeNo.value)) activeNo.value = ''
    selectedNos.value = selectedNos.value.filter(no => items.value.some(row => row.qcItemNo === no))
  } catch (error) { setError(error) } finally { loading.value = false }
}
function setStatus(value: IssueState): void { issueState.value = value; selectedNos.value = []; void loadItems() }
function toggleSelected(no: string, checked: boolean): void { selectedNos.value = checked ? [...new Set([...selectedNos.value, no])] : selectedNos.value.filter(item => item !== no) }
function allVisibleSelected(): boolean { return visibleItems.value.length > 0 && visibleItems.value.every(row => selectedNos.value.includes(row.qcItemNo)) }
function toggleAll(checked: boolean): void { selectedNos.value = checked ? visibleItems.value.map(row => row.qcItemNo) : [] }
onMounted(() => { void loadOptions(); void loadItems() })

function openEditor(mode: EditorMode): void {
  errorMessage.value = ''; editorError.value = ''; editorMode.value = mode
  const row = activeItem.value
  if (mode !== 'add' && !row) { setError('請先選取一筆計量品管項目。'); return }
  if (row && mode !== 'add') {
    editorForm.value = {
      qcItemNo: mode === 'copy' ? '' : row.qcItemNo, qcItemName: mode === 'copy' ? row.qcItemName + ' (Copy)' : row.qcItemName,
      lotIsValid: row.lotIsValid, componentCollect: row.componentCollect, calType: row.calType, planNo: row.planNo,
      formName: row.formName, chartControl: row.chartControl, chartType: row.chartType, specLineType: row.specLineType,
      collectType: row.collectType, qcDataType: row.qcDataType, minValue: String(row.minValue), maxValue: String(row.maxValue),
      limitLotNum: String(row.limitLotNum), sampleSize: row.sampleSize === null ? '' : String(row.sampleSize),
      qcClass: row.qcClass, unit: row.unit, mClassNo: row.mClassNo, description: row.description,
      roundoffNum: row.roundoffNum === null ? '' : String(row.roundoffNum),
    }
    editingStamp.value = row.dataStamp
  } else { editorForm.value = blankForm(); editingStamp.value = 0 }
  copyFlags.value = { parameters: true, factors: true, rules: true, samplePlanChanges: true }
  copyParameterSource.value = 'qcItem'
  copyParametersResolved.value = false
  copySourcePromptOpen.value = false
  editorOpen.value = true
}
function validateEditor(): string {
  const f = editorForm.value
  if (!f.qcItemNo.trim()) return 'QCItemNo 為必填。'
  if (f.qcItemNo.includes(',')) return 'QCItemNo 不可包含逗號。'
  if (!f.qcItemName.trim()) return 'QCItemName 為必填。'
  if (f.lotIsValid === null) return '請選擇 Lot Valid Check。'
  if (f.componentCollect === null) return '請選擇 Component Collect。'
  if (!f.planNo.trim()) return 'PlanNo 為必填。'
  if (showQcClass.value && !f.qcClass.trim()) return 'QCClass 為必填。'
  if (f.chartType === null) return 'ChartType 為必填。'
  if (f.specLineType === null) return 'SpecLineType 為必填。'
  if (!f.limitLotNum.trim() || !Number.isFinite(Number(f.limitLotNum))) return 'Limit Lot Num 必須為數值。'
  if (f.chartControl === 1 && Number(f.limitLotNum) <= 0) return '啟用 Chart Control 時，Limit Lot Num 必須大於 0。'
  if (!f.minValue.trim() || !f.maxValue.trim() || !Number.isFinite(Number(f.minValue)) || !Number.isFinite(Number(f.maxValue))) return 'Min/Max Reasonable Value 必須為數值。'
  if (Number(f.minValue) < -1000000 || Number(f.minValue) > 1000000 || Number(f.maxValue) < -1000000 || Number(f.maxValue) > 1000000) return 'Min/Max Reasonable Value 範圍為 -1,000,000 至 1,000,000。'
  if (Number(f.minValue) > Number(f.maxValue)) return 'Min Reasonable Value 不可大於 Max Reasonable Value。'
  if (f.sampleSize.trim() && (!Number.isFinite(Number(f.sampleSize)) || Number(f.sampleSize) < 0)) return 'Sample Size 必須為大於或等於 0 的數值。'
  if (f.sampleSize.trim() && !Number.isInteger(Number(f.sampleSize))) return 'Sample Size 必須為整數。'
  if (f.chartType === 2 && Number(f.sampleSize) !== 1) return 'ChartType 為 X-Rm 時，Sample Size 必須等於 1。'
  if (f.sampleSize.trim() && Number(f.sampleSize) === 1 && f.chartType !== 2) return 'Sample Size 為 1 時請選擇 X-Rm。'
  return ''
}
function makeItem(form: EditorForm, current?: VariableQcItem): VariableQcItem {
  const now = dateNow()
  return {
    qcItemNo: form.qcItemNo.trim(), qcItemName: form.qcItemName.trim(), chartType: Number(form.chartType),
    specLineType: Number(form.specLineType), componentCollect: Number(form.componentCollect), lotIsValid: Number(form.lotIsValid),
    planNo: form.planNo.trim(), chartControl: form.chartControl, collectType: form.collectType, limitLotNum: Number(form.limitLotNum),
    sampleSize: form.sampleSize.trim() ? Number(form.sampleSize) : null, unit: form.unit.trim(), qcDataType: form.qcDataType,
    minValue: Number(form.minValue), maxValue: Number(form.maxValue), description: form.description,
    roundoffNum: form.roundoffNum.trim() ? Number(form.roundoffNum) : null, qcClass: form.qcClass, mClassNo: form.mClassNo.trim(),
    calType: form.componentCollect === 1 ? form.calType : 0, formName: form.formName,
    errorReportName: current?.errorReportName ?? '', noticeReportName: current?.noticeReportName ?? '', webUrl: current?.webUrl ?? '',
    executionFile: interfaceOptions.value.includes(form.formName) ? form.formName : (current?.executionFile ?? ''),
    issueState: current?.issueState ?? 0, dataStamp: current ? current.dataStamp + 1 : 0, creator: current?.creator ?? currentUser.value,
    createDate: current?.createDate ?? now, reviser: current ? currentUser.value : '', reviseDate: current ? now : '',
  }
}
function cloneMockChildren(sourceNo: string, targetNo: string): void {
  const store = readStore()
  const parameterSourceRows = copyParameterSource.value === 'qcItem' ? (store.parameters[sourceNo] ?? []) : (devPlanParameters[editorForm.value.planNo] ?? [])
  store.parameters[targetNo] = structuredClone(copyFlags.value.parameters ? parameterSourceRows : (devPlanParameters[editorForm.value.planNo] ?? []))
  if (copyFlags.value.factors) store.factors[targetNo] = structuredClone(store.factors[sourceNo] ?? [])
  else store.factors[targetNo] = []
  if (copyFlags.value.rules) store.rules[targetNo] = structuredClone(store.rules[sourceNo] ?? [])
  else store.rules[targetNo] = []
  store.samplePlanChanges[targetNo] = copyFlags.value.samplePlanChanges ? structuredClone(store.samplePlanChanges[sourceNo] ?? []) : []
  store.operations[targetNo] = []
  store.memos[targetNo] = []
  saveStore(store)
}
function sameParameterRows(left: ParameterRow[], right: ParameterRow[]): boolean {
  const normalize = (rows: ParameterRow[]) => rows.map(row => [row.parameterNo.trim(), row.parameterValue.trim()]).sort((a, b) => a[0].localeCompare(b[0]))
  return JSON.stringify(normalize(left)) === JSON.stringify(normalize(right))
}
async function prepareCopyParameterSource(): Promise<boolean> {
  if (!activeItem.value) return true
  if (mock.value) {
    const store = readStore()
    copyQcParameters.value = structuredClone(store.parameters[activeItem.value.qcItemNo] ?? [])
    copyPlanParameters.value = structuredClone(devPlanParameters[editorForm.value.planNo] ?? [])
  } else {
    const preview = await request<{ qcItemParameters: ParameterRow[]; samplingPlanParameters: ParameterRow[] }>(apiRoot + '/copy-preview', {
      method: 'POST', body: JSON.stringify({ fromQcItemNo: activeItem.value.qcItemNo, planNo: editorForm.value.planNo }),
    })
    copyQcParameters.value = preview.qcItemParameters ?? []
    copyPlanParameters.value = preview.samplingPlanParameters ?? []
  }
  if (sameParameterRows(copyQcParameters.value, copyPlanParameters.value)) {
    copyParameterSource.value = 'qcItem'; copyParametersResolved.value = true; return true
  }
  copyParameterSource.value = 'qcItem'; copySourcePromptOpen.value = true; return false
}
function confirmCopyParameterSource(): void {
  copyParametersResolved.value = true; copySourcePromptOpen.value = false; void saveEditor(true)
}
function cancelCopyParameterSource(): void { copySourcePromptOpen.value = false }
async function saveEditor(closeAfter: boolean): Promise<void> {
  editorError.value = validateEditor()
  if (editorError.value) return
  if (editorMode.value === 'copy' && copyFlags.value.parameters && !copyParametersResolved.value) {
    try {
      if (!(await prepareCopyParameterSource())) return
    } catch (error) {
      editorError.value = error instanceof Error ? error.message : String(error)
      return
    }
  }
  busy.value = true
  try {
    const form = editorForm.value
    const current = editorMode.value === 'edit' ? activeItem.value ?? undefined : undefined
    const item = makeItem(form, current)
    if (mock.value) {
      const store = readStore()
      if (editorMode.value !== 'edit' && store.items.some(row => row.qcItemNo.toUpperCase() === item.qcItemNo.toUpperCase())) throw new Error('QCItemNo 已存在。')
      if (editorMode.value === 'edit') { store.items = store.items.map(row => row.qcItemNo === item.qcItemNo ? item : row); saveStore(store) }
      else {
        store.items.push(item); saveStore(store)
        if (editorMode.value === 'copy' && activeItem.value) cloneMockChildren(activeItem.value.qcItemNo, item.qcItemNo)
      }
    } else if (editorMode.value === 'edit') {
      await request(apiRoot + '/' + encodeURIComponent(item.qcItemNo), { method: 'PUT', body: JSON.stringify({ ...item, dataStamp: editingStamp.value }) })
    } else {
      await request(apiRoot, { method: 'POST', body: JSON.stringify({
        ...item, ...(editorMode.value === 'copy' && activeItem.value ? { fromQcItemNo: activeItem.value.qcItemNo, copyFlags: copyFlags.value, parameterSource: copyFlags.value.parameters ? copyParameterSource.value : 'samplingPlan' } : {}),
      }) })
    }
    issueState.value = 0; activeNo.value = item.qcItemNo
    if (closeAfter) editorOpen.value = false
    copyParametersResolved.value = false
    setNotice(editorMode.value === 'edit' ? '計量品管項目已修改。' : '計量品管項目已新增。')
    await loadItems()
  } catch (error) { editorError.value = error instanceof Error ? error.message : String(error) }
  finally { busy.value = false }
}
function rowsForAction(): VariableQcItem[] {
  const noSet = new Set(selectedNos.value)
  if (activeNo.value) noSet.add(activeNo.value)
  return visibleItems.value.filter(row => noSet.has(row.qcItemNo))
}
async function deleteSelected(): Promise<void> {
  const rows = rowsForAction()
  if (!rows.length) { setError('請選取欲刪除的資料。'); return }
  if (rows.some(row => ![0, -1, 2].includes(row.issueState))) { setError('簽核中的計量品管項目不可刪除。'); return }
  if (!window.confirm('確定刪除選取的計量品管項目？已簽核項目將改為「不使用」。')) return
  busy.value = true; let completed = 0
  try {
    for (const row of rows) {
      if (mock.value) {
        const store = readStore()
        if (row.issueState === 2) store.items = store.items.map(item => item.qcItemNo === row.qcItemNo ? { ...item, issueState: -1, dataStamp: item.dataStamp + 1 } : item)
        else store.items = store.items.filter(item => item.qcItemNo !== row.qcItemNo)
        saveStore(store)
      } else await request(apiRoot + '/' + encodeURIComponent(row.qcItemNo) + '?issueState=' + row.issueState + '&dataStamp=' + row.dataStamp, { method: 'DELETE' })
      completed += 1
    }
    selectedNos.value = []; activeNo.value = ''; setNotice('已處理 ' + completed + ' 筆計量品管項目。'); await loadItems()
  } catch (error) { setError('已完成 ' + completed + ' 筆；後續停止：' + (error instanceof Error ? error.message : String(error))); await loadItems() }
  finally { busy.value = false }
}
async function approveSelected(): Promise<void> {
  const rows = rowsForAction()
  if (!rows.length) { setError('請選取欲核准的資料。'); return }
  if (issueState.value !== 0 || rows.some(row => row.issueState !== 0)) { setError('只有未簽核的計量品管項目可以核准。'); return }
  if (!window.confirm('確定送出核准選取項目？')) return
  busy.value = true; let completed = 0
  try {
    for (const row of rows) {
      if (mock.value) {
        const store = readStore()
        if (!(store.factors[row.qcItemNo] ?? []).length) throw new Error(row.qcItemNo + ' 尚未設定 Factor，無法核准。')
        store.items = store.items.map(item => item.qcItemNo === row.qcItemNo ? { ...item, issueState: 2, reviser: currentUser.value, reviseDate: dateNow(), dataStamp: item.dataStamp + 1 } : item)
        saveStore(store)
      } else {
        const factors = toList(await request<Payload<FactorRow>>(apiRoot + '/' + encodeURIComponent(row.qcItemNo) + '/factors'))
        if (!factors.length) throw new Error(row.qcItemNo + ' 尚未設定 Factor，無法核准。')
        await request(apiRoot + '/' + encodeURIComponent(row.qcItemNo) + '/approve', { method: 'POST', body: JSON.stringify({ issueState: 0, dataStamp: row.dataStamp }) })
      }
      completed += 1
    }
    setNotice('已送出 ' + completed + ' 筆核准。'); await loadItems()
  } catch (error) { setError('已完成 ' + completed + ' 筆；後續停止：' + (error instanceof Error ? error.message : String(error))); await loadItems() }
  finally { busy.value = false }
}

async function loadChild(mode: ChildMode): Promise<void> {
  if (!activeItem.value) { setError('請先選取一筆計量品管項目。'); return }
  if (mode === 'operations' && [0, 1].includes(activeItem.value.issueState)) { setError('未簽核或簽核中的資料不可設定所屬作業站。'); return }
  childMode.value = mode; childError.value = ''; childNotice.value = ''; childSearch.value = ''; childCandidate.value = ''; simulationMessage.value = ''
  try {
    const no = encodeURIComponent(activeItem.value.qcItemNo)
    if (mode === 'simulation') {
      simulationRows.value = mock.value ? [] : toList(await request<Payload<Record<string, string | number>>>(apiRoot + '/' + no + '/simulation'))
      if (mock.value) simulationMessage.value = 'DEV 假資料模擬；不寫入量測記錄。'
    } else if (mode === 'memos') {
      if (!mock.value) childRows.value = toList(await request<Payload<Record<string, string | number | boolean>>>(apiRoot + '/' + no + '/memos'))
      else childRows.value = parameterOptions.value.map(parameterNo => ({ parameterNo, parameterName: parameterNo, selected: (readStore().memos[activeItem.value!.qcItemNo] ?? []).includes(parameterNo) }))
    } else {
      const endpoint = mode === 'samplePlanChanges' ? 'sample-plan-changes' : mode
      if (!mock.value) childRows.value = toList(await request<Payload<Record<string, string | number | boolean>>>(apiRoot + '/' + no + '/' + endpoint))
      else {
        const store = readStore()
        const source = mode === 'parameters' ? store.parameters[activeItem.value.qcItemNo]
          : mode === 'factors' ? store.factors[activeItem.value.qcItemNo]
            : mode === 'operations' ? store.operations[activeItem.value.qcItemNo]
              : store.samplePlanChanges[activeItem.value.qcItemNo]
        childRows.value = structuredClone(source ?? []) as unknown as Array<Record<string, string | number | boolean>>
      }
    }
    childSelection.value = []; childOpen.value = true
  } catch (error) { setError(error) }
}
function childKey(row: Record<string, string | number | boolean>): string {
  const key = row.parameterNo ?? row.factorNo ?? row.opNo
  return key === undefined ? String(row.oldSamplePlan ?? '') + ':' + String(row.newSamplePlan ?? '') + ':' + childRows.value.indexOf(row) : String(key)
}
function toggleChildSelection(key: string, checked: boolean): void {
  childSelection.value = checked ? [...new Set([...childSelection.value, key])] : childSelection.value.filter(item => item !== key)
}
function addChildRow(): void {
  if (childMode.value === 'samplePlanChanges') {
    childRows.value.push({ oldSamplePlan: '', accLotQty: 0, rejLotQty: 0, newSamplePlan: '', factorNo: '', lotRange: 0, aqlChkForSPlanChg: 0 })
    return
  }
  if (!childCandidate.value) return
  const keyName = childMode.value === 'parameters' ? 'parameterNo' : childMode.value === 'factors' ? 'factorNo' : 'opNo'
  if (childRows.value.some(row => String(row[keyName]) === childCandidate.value)) return
  if (childMode.value === 'parameters') childRows.value.push({ parameterNo: childCandidate.value, parameterValue: '' })
  else if (childMode.value === 'factors') childRows.value.push({ factorNo: childCandidate.value, factorOnline: 'Y', factorSpec: 'Y', opNo: '', tableName: '', fieldName: '', factorShortRun: 'N' })
  else if (childMode.value === 'operations') {
    const op = operationOptions.value.find(item => item.opNo === childCandidate.value)
    childRows.value.push({ opNo: childCandidate.value, opName: op?.opName ?? '', inline: activeItem.value?.lotIsValid === 1 ? 1 : 0,
      executeOrder: childRows.value.length + 1, qcItemType: 1, inspTypeFai: false, inspTypeIpqc: false, inspTypePqc: false })
  }
  childCandidate.value = ''
}
function removeChildRows(): void {
  childRows.value = childRows.value.filter(row => !childSelection.value.includes(childKey(row)))
  childSelection.value = []
}
async function saveChild(): Promise<void> {
  if (!activeItem.value) return
  childError.value = ''
  const no = encodeURIComponent(activeItem.value.qcItemNo)
  if (childMode.value === 'memos') {
    const checked = childRows.value.filter(row => Boolean(row.selected)).map(row => String(row.parameterNo))
    try {
      if (mock.value) {
        const store = readStore(); store.memos[activeItem.value.qcItemNo] = checked; saveStore(store)
      } else await request(apiRoot + '/' + no + '/memos', { method: 'PUT', body: JSON.stringify({ qcItemType: 0, parameterNos: checked }) })
      childNotice.value = 'Memo 欄位已儲存；DEV 畫面只保存設定，不產生量測資料。'
      childOpen.value = false
      return
    } catch (error) { childError.value = error instanceof Error ? error.message : String(error); return }
  }
  const endpoint = childMode.value === 'samplePlanChanges' ? 'sample-plan-changes' : childMode
  try {
    if (mock.value) {
      const store = readStore(); const key = activeItem.value.qcItemNo
      if (childMode.value === 'parameters') store.parameters[key] = structuredClone(childRows.value) as unknown as ParameterRow[]
      else if (childMode.value === 'factors') store.factors[key] = structuredClone(childRows.value) as unknown as FactorRow[]
      else if (childMode.value === 'operations') store.operations[key] = structuredClone(childRows.value) as unknown as OperationRow[]
      else if (childMode.value === 'samplePlanChanges') store.samplePlanChanges[key] = structuredClone(childRows.value) as unknown as SamplePlanChangeRow[]
      saveStore(store)
    } else await request(apiRoot + '/' + no + '/' + endpoint, { method: 'PUT', body: JSON.stringify({ items: childRows.value }) })
    childOpen.value = false; setNotice(childTitle.value + ' 已儲存。')
  } catch (error) { childError.value = error instanceof Error ? error.message : String(error) }
}
async function runSimulation(): Promise<void> {
  if (!activeItem.value) return
  childError.value = ''
  if (mock.value) {
    const item = activeItem.value
    simulationRows.value = Array.from({ length: Math.max(1, Math.min(Number(item.sampleSize || 5), 20)) }, (_, index) => {
      const low = Number(item.minValue); const high = Number(item.maxValue)
      const value = Number((low + Math.random() * (high - low)).toFixed(3))
      return { 樣本序號: index + 1, ParameterNo: parameterOptions.value[index % parameterOptions.value.length], 測量值: value, 判定: value >= low && value <= high ? '合格' : '超出範圍' }
    })
    simulationMessage.value = 'DEV 模擬完成；結果不會儲存至量測日誌。'
  } else {
    try {
      simulationRows.value = toList(await request<Payload<Record<string, string | number>>>(apiRoot + '/' + encodeURIComponent(activeItem.value.qcItemNo) + '/simulation', {
        method: 'POST', body: JSON.stringify({ planNo: activeItem.value.planNo }),
      }))
      simulationMessage.value = '模擬完成；結果不會寫入正式量測記錄。'
    } catch (error) { childError.value = error instanceof Error ? error.message : String(error) }
  }
}
function showLookup(mode: 'plan' | 'mclass'): void { lookupMode.value = mode; childSearch.value = ''; lookupOpen.value = true }
function chooseLookup(value: string): void {
  if (lookupMode.value === 'plan') editorForm.value.planNo = value
  else editorForm.value.mClassNo = value
  lookupOpen.value = false
}

</script>
<template>
  <section class="qcv-page">
    <header class="qcv-title"><h1>計量品管項目</h1><span v-if="mock" class="qcv-dev">DEV 假資料</span></header>
    <div class="qcv-query">
      <div class="qcv-status" role="radiogroup" aria-label="Issue State">
        <label v-for="option in statusOptions" :key="option.value"><input type="radio" name="qcv-status" :checked="issueState===option.value" @change="setStatus(option.value)"><span>{{ option.label }}</span></label>
      </div>
      <label class="qcv-search-label" for="qcv-search">QCItemNo</label>
      <input id="qcv-search" v-model="searchText" class="qcv-input qcv-search" @keyup.enter="loadItems">
      <button type="button" class="qcv-btn" @click="loadItems">查詢</button>
    </div>
    <div v-if="errorMessage" class="qcv-message qcv-error">{{ errorMessage }}</div>
    <div v-else-if="notice" class="qcv-message">{{ notice }}</div>
    <div class="qcv-main">
      <div class="qcv-grid-area">
        <div class="qcv-actionbar">
          <button type="button" class="qcv-btn qcv-primary" :disabled="busy" @click="openEditor('add')">ADD (&amp;A)</button>
          <button type="button" class="qcv-btn" :disabled="busy || !activeItem" @click="openEditor('edit')">EDIT (&amp;E)</button>
          <button type="button" class="qcv-btn" :disabled="busy || !activeItem" @click="openEditor('copy')">Copy</button>
          <button type="button" class="qcv-btn" :disabled="busy || issueState!==0 || !visibleItems.length" @click="approveSelected">Approve</button>
          <button type="button" class="qcv-btn" :disabled="busy || !visibleItems.length" @click="deleteSelected">DEL (&amp;D)</button>
          <span class="qcv-toolbar-gap"></span><span v-if="loading" class="qcv-loading">讀取中…</span>
        </div>
        <div class="qcv-grid-scroll">
          <table class="qcv-grid">
            <thead><tr>
              <th class="qcv-check"><input type="checkbox" :checked="allVisibleSelected()" @change="toggleAll(($event.target as HTMLInputElement).checked)"></th>
              <th>QCItemNo</th><th>QCItemName</th><th>ChartType</th><th>SpecLineType</th><th>ComponentCollect</th><th>LotIsValid</th><th>PlanNo</th><th>ChartControl</th><th>CollectType</th><th>LimitLotNum</th><th>SampleSize</th><th>Unit</th><th>QCDataType</th><th>MinValue</th><th>MaxValue</th><th>Description</th><th>Roundoff_Num</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th>
            </tr></thead>
            <tbody>
              <tr v-for="row in visibleItems" :key="row.qcItemNo" :class="{ selected: activeNo===row.qcItemNo }" @click="activeNo=row.qcItemNo">
                <td class="qcv-check" @click.stop><input type="checkbox" :checked="selectedNos.includes(row.qcItemNo)" @change="toggleSelected(row.qcItemNo, ($event.target as HTMLInputElement).checked)"></td>
                <td>{{ row.qcItemNo }}</td><td>{{ row.qcItemName }}</td><td>{{ chartLabel(row.chartType) }}</td><td>{{ specLabel(row.specLineType) }}</td><td>{{ yesNo(row.componentCollect) }}</td><td>{{ yesNo(row.lotIsValid) }}</td><td>{{ row.planNo }}</td><td>{{ yesNo(row.chartControl) }}</td><td>{{ collectLabel(row.collectType) }}</td><td class="num">{{ row.limitLotNum }}</td><td class="num">{{ row.sampleSize }}</td><td>{{ row.unit }}</td><td>{{ dataTypeLabel(row.qcDataType) }}</td><td class="num">{{ row.minValue }}</td><td class="num">{{ row.maxValue }}</td><td>{{ row.description }}</td><td class="num">{{ row.roundoffNum }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser }}</td><td>{{ row.reviseDate }}</td>
              </tr>
              <tr v-if="!visibleItems.length"><td colspan="22" class="qcv-empty">{{ loading ? '資料讀取中…' : '沒有符合條件的資料' }}</td></tr>
            </tbody>
          </table>
        </div>
        <footer class="qcv-grid-footer">資料總筆數 {{ visibleItems.length }}</footer>
      </div>
      <aside class="qcv-actions">
        <button type="button" class="qcv-btn" :disabled="!activeItem" @click="loadChild('parameters')">Parameter</button>
        <button type="button" class="qcv-btn" :disabled="!activeItem" @click="loadChild('factors')">Factor</button>
        <button type="button" class="qcv-btn" :disabled="!activeItem" @click="loadChild('simulation')">Simulation</button>
        <button type="button" class="qcv-btn" :disabled="!activeItem" @click="loadChild('operations')">OP</button>
        <button type="button" class="qcv-btn" :disabled="!activeItem" @click="loadChild('memos')">Memo</button>
        <span class="qcv-action-spacer"></span>
        <button type="button" class="qcv-btn" :disabled="!activeItem" @click="loadChild('samplePlanChanges')">SamplePlanChange</button>
        <button type="button" class="qcv-btn" @click="emit('close')">Close (&amp;X)</button>
      </aside>
    </div>

    <div v-if="editorOpen" class="qcv-overlay" @click.self="editorOpen=false">
      <section class="qcv-editor" role="dialog" aria-modal="true">
        <header class="qcv-dialog-title"><h2>QCItem For Variables Define ({{ editorMode.toUpperCase() }})</h2><button type="button" class="qcv-x" @click="editorOpen=false">×</button></header>
        <div class="qcv-editor-scroll">
          <fieldset class="qcv-fieldset qcv-basic"><legend>QCItem Define</legend>
            <div class="qcv-field-grid">
              <label class="qcv-field"><span>QCItem No</span><input v-model="editorForm.qcItemNo" class="qcv-input" :readonly="editorMode==='edit'" maxlength="25"></label>
              <label class="qcv-field"><span>QCItem Name</span><input v-model="editorForm.qcItemName" class="qcv-input" maxlength="500"></label>
              <div class="qcv-field"><span>Lot Valid Check</span><div class="qcv-options"><label><input v-model.number="editorForm.lotIsValid" type="radio" :value="1"> Yes</label><label><input v-model.number="editorForm.lotIsValid" type="radio" :value="0"> No</label></div></div>
              <div class="qcv-field"><span>Component Collect</span><div class="qcv-options"><label><input v-model.number="editorForm.componentCollect" type="radio" :value="1"> Yes</label><label><input v-model.number="editorForm.componentCollect" type="radio" :value="0" @change="editorForm.calType=0"> No</label></div></div>
              <div class="qcv-field"><span>Calculate Type</span><div class="qcv-options"><label><input v-model.number="editorForm.calType" type="radio" :value="0" :disabled="editorForm.componentCollect!==1"> By Lot</label><label><input v-model.number="editorForm.calType" type="radio" :value="1" :disabled="editorForm.componentCollect!==1"> By Component</label></div></div>
              <label class="qcv-field"><span>Plan No</span><div class="qcv-input-action"><input v-model="editorForm.planNo" class="qcv-input" readonly><button class="qcv-btn" type="button" @click="showLookup('plan')">…</button></div></label>
              <div class="qcv-field"><span>Chart Control</span><div class="qcv-options"><label><input v-model.number="editorForm.chartControl" type="radio" :value="1"> Yes</label><label><input v-model.number="editorForm.chartControl" type="radio" :value="0"> No</label></div></div>
              <label class="qcv-field"><span>Chart Type</span><select v-model.number="editorForm.chartType" class="qcv-input"><option :value="null">請選擇</option><option v-for="(item,index) in chartTypes" :key="item" :value="index">{{ item }}</option></select></label>
              <div class="qcv-field"><span>SpecLineType</span><div class="qcv-options qcv-options-wrap"><label v-for="(item,index) in specLineTypes" :key="item"><input v-model.number="editorForm.specLineType" type="radio" :value="index">{{ item }}</label></div></div>
              <div class="qcv-field"><span>Collect Type</span><div class="qcv-options"><label><input v-model.number="editorForm.collectType" type="radio" :value="0"> Manual</label><label><input v-model.number="editorForm.collectType" type="radio" :value="1"> Automatic</label></div></div>
              <label class="qcv-field"><span>QCDataType</span><select v-model.number="editorForm.qcDataType" class="qcv-input"><option v-for="(item,index) in qcDataTypes" :key="item" :value="index">{{ item }}</option></select></label>
              <label class="qcv-field"><span>Min Reasonable Value</span><input v-model="editorForm.minValue" class="qcv-input" type="number" step="any"></label>
              <label class="qcv-field"><span>Max Reasonable Value</span><input v-model="editorForm.maxValue" class="qcv-input" type="number" step="any"></label>
              <label class="qcv-field"><span>Limit Lot Num</span><input v-model="editorForm.limitLotNum" class="qcv-input" type="number"></label>
              <label class="qcv-field"><span>Sample Size</span><input v-model="editorForm.sampleSize" class="qcv-input" type="number"></label>
              <label v-if="showQcClass" class="qcv-field"><span>QC Class</span><input v-model="editorForm.qcClass" class="qcv-input"></label>
              <label class="qcv-field"><span>Unit</span><input v-model="editorForm.unit" class="qcv-input"></label>
              <label class="qcv-field"><span>MClass No</span><div class="qcv-input-action"><input v-model="editorForm.mClassNo" class="qcv-input" readonly><button class="qcv-btn" type="button" @click="showLookup('mclass')">…</button></div></label>
              <label class="qcv-field"><span>Special Form Name</span><select v-model="editorForm.formName" class="qcv-input"><option value="">(Default)</option><option v-for="item in interfaceOptions" :key="item" :value="item">{{ item }}</option></select></label>
              <label class="qcv-field"><span>Roundoff_Num</span><input v-model="editorForm.roundoffNum" class="qcv-input" type="number" min="0"></label>
              <label class="qcv-field qcv-description"><span>Description</span><textarea v-model="editorForm.description" class="qcv-input" rows="3"></textarea></label>
            </div>
          </fieldset>
          <fieldset v-if="editorMode==='copy'" class="qcv-fieldset"><legend>Copy</legend><div class="qcv-options qcv-copy-options">
            <label><input v-model="copyFlags.parameters" type="checkbox"> Parameter</label><label><input v-model="copyFlags.factors" type="checkbox"> Factor</label>
            <label><input v-model="copyFlags.rules" type="checkbox"> Rule</label><label><input v-model="copyFlags.samplePlanChanges" type="checkbox"> SamplePlanChange</label>
          </div></fieldset>
          <fieldset class="qcv-audit"><legend>建立/修改資訊</legend><label>建立人<input :value="editorMode==='edit' ? activeItem?.creator : currentUser" readonly></label><label>建立日期<input :value="editorMode==='edit' ? activeItem?.createDate : dateNow()" readonly></label><label>修改人<input :value="editorMode==='edit' ? currentUser : ''" readonly></label><label>修改日期<input :value="editorMode==='edit' ? dateNow() : ''" readonly></label></fieldset>
        </div>
        <p v-if="editorError" class="qcv-dialog-error">{{ editorError }}</p>
        <footer class="qcv-dialog-actions"><button type="button" class="qcv-btn qcv-primary" :disabled="busy" @click="saveEditor(true)">確定</button><button type="button" class="qcv-btn" @click="editorOpen=false">關閉</button><button v-if="editorMode==='edit'" type="button" class="qcv-btn" :disabled="busy" @click="saveEditor(false)">套用</button></footer>
      </section>
    </div>

    <div v-if="copySourcePromptOpen" class="qcv-overlay qcv-copy-overlay" @click.self="cancelCopyParameterSource">
      <section class="qcv-copy-selector" role="dialog" aria-modal="true">
        <header class="qcv-dialog-title"><h2>QCItem Parameter Copy Selector</h2><button type="button" class="qcv-x" @click="cancelCopyParameterSource">×</button></header>
        <fieldset class="qcv-copy-choice"><legend>COPYOPTION</legend><label><input v-model="copyParameterSource" type="radio" value="qcItem"> From QCItem</label><label><input v-model="copyParameterSource" type="radio" value="samplingPlan"> From SamplingPlan</label></fieldset>
        <div class="qcv-copy-grids">
          <section><h3>QCItem Parameter</h3><table class="qcv-grid"><thead><tr><th>ParameterNo</th><th>ParameterValue</th></tr></thead><tbody><tr v-for="row in copyQcParameters" :key="row.parameterNo"><td>{{ row.parameterNo }}</td><td>{{ row.parameterValue }}</td></tr><tr v-if="!copyQcParameters.length"><td colspan="2" class="qcv-empty">沒有參數</td></tr></tbody></table></section>
          <section><h3>Sampling Plan Parameter</h3><table class="qcv-grid"><thead><tr><th>ParameterNo</th><th>ParameterValue</th></tr></thead><tbody><tr v-for="row in copyPlanParameters" :key="row.parameterNo"><td>{{ row.parameterNo }}</td><td>{{ row.parameterValue }}</td></tr><tr v-if="!copyPlanParameters.length"><td colspan="2" class="qcv-empty">沒有參數</td></tr></tbody></table></section>
        </div>
        <footer class="qcv-dialog-actions"><button type="button" class="qcv-btn qcv-primary" @click="confirmCopyParameterSource">Confirm</button><button type="button" class="qcv-btn" @click="cancelCopyParameterSource">Close</button></footer>
      </section>
    </div>
    <div v-if="childOpen" class="qcv-overlay" @click.self="childOpen=false">
      <section class="qcv-child-dialog" role="dialog" aria-modal="true">
        <header class="qcv-dialog-title"><div><h2>{{ childTitle }}</h2><p>{{ activeItem?.qcItemNo }}　{{ activeItem?.qcItemName }}</p></div><button type="button" class="qcv-x" @click="childOpen=false">×</button></header>
        <template v-if="childMode==='simulation'">
          <p class="qcv-child-hint">{{ simulationMessage || '依 QCItem 與 PlanNo 模擬量測參數。' }}</p>
          <div class="qcv-child-grid"><table class="qcv-grid"><thead><tr><th>樣本序號</th><th>ParameterNo</th><th>測量值</th><th>判定</th></tr></thead><tbody><tr v-for="(row,index) in simulationRows" :key="index"><td>{{ row['樣本序號'] }}</td><td>{{ row.ParameterNo }}</td><td>{{ row['測量值'] }}</td><td>{{ row['判定'] }}</td></tr><tr v-if="!simulationRows.length"><td colspan="4" class="qcv-empty">尚未執行模擬</td></tr></tbody></table></div>
          <p v-if="childError" class="qcv-dialog-error">{{ childError }}</p><footer class="qcv-dialog-actions"><button class="qcv-btn qcv-primary" type="button" @click="runSimulation">Simulation</button><button class="qcv-btn" type="button" @click="childOpen=false">關閉</button></footer>
        </template>
        <template v-else-if="childMode==='memos'">
          <div class="qcv-memo-toolbar"><input v-model="childSearch" class="qcv-input" placeholder="搜尋 ParameterNo"><button type="button" class="qcv-btn" @click="childRows.forEach(row=>row.selected=true)">全選</button><button type="button" class="qcv-btn" @click="childRows.forEach(row=>row.selected=false)">全不選</button><button type="button" class="qcv-btn" :disabled="!childSelection.length" @click="childRows.forEach(row=>{if(childSelection.includes(childKey(row)))row.selected=true})">多選</button></div>
          <div class="qcv-child-grid"><table class="qcv-grid"><thead><tr><th class="qcv-check">選取列</th><th class="qcv-check">Memo</th><th>ParameterNo</th><th>ParameterName</th></tr></thead><tbody>
            <tr v-for="row in childRows.filter(item=>String(item.parameterNo).toLowerCase().includes(childSearch.toLowerCase()))" :key="String(row.parameterNo)">
              <td class="qcv-check"><input type="checkbox" :checked="childSelection.includes(childKey(row))" @change="toggleChildSelection(childKey(row), ($event.target as HTMLInputElement).checked)"></td>
              <td class="qcv-check"><input v-model="row.selected" type="checkbox"></td><td>{{ row.parameterNo }}</td><td>{{ row.parameterName || row.parameterNo }}</td>
            </tr>
          </tbody></table></div>
          <p class="qcv-child-hint">來源為已核准 ParameterClass=1 的 MES Parameter。儲存會整批取代此 QCItem 的 Memo 欄位設定。</p>
          <p v-if="childError" class="qcv-dialog-error">{{ childError }}</p><footer class="qcv-dialog-actions"><button class="qcv-btn qcv-primary" type="button" :disabled="!childEditable" @click="saveChild">確定</button><button class="qcv-btn" type="button" @click="childOpen=false">取消</button></footer>
        </template>
        <template v-else>
          <div class="qcv-child-toolbar">
            <select v-if="childMode==='parameters' || childMode==='factors' || childMode==='operations'" v-model="childCandidate" class="qcv-input"><option value="">選擇要加入的項目</option><option v-for="item in candidateOptions" :key="item" :value="item">{{ item }}</option></select>
            <button class="qcv-btn" type="button" :disabled="!childEditable || (childMode!=='samplePlanChanges' && !childCandidate)" @click="addChildRow">新增</button>
            <button class="qcv-btn" type="button" :disabled="!childEditable || !childSelection.length" @click="removeChildRows">刪除選取</button>
            <span class="qcv-child-hint">QCItemType = 1</span>
          </div>
          <div class="qcv-child-grid"><table class="qcv-grid">
            <thead v-if="childMode==='parameters'"><tr><th class="qcv-check"></th><th>ParameterNo</th><th>ParameterValue</th></tr></thead>
            <thead v-else-if="childMode==='factors'"><tr><th class="qcv-check"></th><th>FactorNo</th><th>FactorOnline</th><th>FactorSpec</th><th>OPNo</th><th>TableName</th><th>FieldName</th><th>FactorShortRun</th></tr></thead>
            <thead v-else-if="childMode==='operations'"><tr><th class="qcv-check"></th><th>OPNo</th><th>OPName</th><th>Type</th><th>ExecuteOrder</th><th>FAI</th><th>IPQC</th><th>PQC</th></tr></thead>
            <thead v-else><tr><th class="qcv-check"></th><th>Old Sample Plan</th><th>Acc Lot Qty</th><th>Rej Lot Qty</th><th>New Sample Plan</th><th>FactorNo</th><th>Lot Range</th><th>AQL Check</th></tr></thead>
            <tbody>
              <tr v-for="row in childRows" :key="childKey(row)">
                <td class="qcv-check"><input type="checkbox" :checked="childSelection.includes(childKey(row))" @change="toggleChildSelection(childKey(row), ($event.target as HTMLInputElement).checked)"></td>
                <template v-if="childMode==='parameters'"><td>{{ row.parameterNo }}</td><td><input v-model="row.parameterValue" class="qcv-input" :disabled="!childEditable"></td></template>
                <template v-else-if="childMode==='factors'"><td>{{ row.factorNo }}</td><td><select v-model="row.factorOnline" class="qcv-input" :disabled="!childEditable"><option>Y</option><option>N</option></select></td><td><select v-model="row.factorSpec" class="qcv-input" :disabled="!childEditable"><option>Y</option><option>N</option></select></td><td><input v-model="row.opNo" class="qcv-input" :disabled="!childEditable"></td><td><input v-model="row.tableName" class="qcv-input" :disabled="!childEditable"></td><td><input v-model="row.fieldName" class="qcv-input" :disabled="!childEditable"></td><td><select v-model="row.factorShortRun" class="qcv-input" :disabled="!childEditable"><option>N</option><option>Y</option></select></td></template>
                <template v-else-if="childMode==='operations'"><td>{{ row.opNo }}</td><td>{{ row.opName }}</td><td><select v-model.number="row.inline" class="qcv-input" :disabled="!childEditable || activeItem?.lotIsValid===0"><option :value="1">InLine</option><option :value="0">OffLine</option></select></td><td><input v-model.number="row.executeOrder" class="qcv-input" type="number" :disabled="!childEditable"></td><td><input v-model="row.inspTypeFai" type="checkbox" :disabled="!childEditable || row.inline!==1"></td><td><input v-model="row.inspTypeIpqc" type="checkbox" :disabled="!childEditable || row.inline!==1"></td><td><input v-model="row.inspTypePqc" type="checkbox" :disabled="!childEditable || row.inline!==1"></td></template>
                <template v-else><td><input v-model="row.oldSamplePlan" class="qcv-input" :disabled="!childEditable"></td><td><input v-model.number="row.accLotQty" class="qcv-input" type="number" :disabled="!childEditable"></td><td><input v-model.number="row.rejLotQty" class="qcv-input" type="number" :disabled="!childEditable"></td><td><input v-model="row.newSamplePlan" class="qcv-input" :disabled="!childEditable"></td><td><input v-model="row.factorNo" class="qcv-input" :disabled="!childEditable"></td><td><input v-model.number="row.lotRange" class="qcv-input" type="number" :disabled="!childEditable"></td><td><select v-model.number="row.aqlChkForSPlanChg" class="qcv-input" :disabled="!childEditable"><option :value="0">No</option><option :value="1">Yes</option></select></td></template>
              </tr><tr v-if="!childRows.length"><td colspan="8" class="qcv-empty">沒有設定資料</td></tr>
            </tbody>
          </table></div>
          <p v-if="childError" class="qcv-dialog-error">{{ childError }}</p><footer class="qcv-dialog-actions"><button type="button" class="qcv-btn qcv-primary" :disabled="!childEditable" @click="saveChild">確定</button><button type="button" class="qcv-btn" @click="childOpen=false">關閉</button></footer>
        </template>
      </section>
    </div>

    <div v-if="lookupOpen" class="qcv-overlay qcv-lookup-overlay" @click.self="lookupOpen=false">
      <section class="qcv-lookup" role="dialog" aria-modal="true"><header class="qcv-dialog-title"><h2>{{ lookupMode==='plan' ? 'Sampling Plan Lookup' : 'MClass Lookup' }}</h2><button type="button" class="qcv-x" @click="lookupOpen=false">×</button></header>
        <input v-model="childSearch" class="qcv-input" placeholder="搜尋代碼"><button v-for="item in lookupItems" :key="item" class="qcv-lookup-row" type="button" @click="chooseLookup(item)">{{ item }}</button>
        <footer class="qcv-dialog-actions"><button type="button" class="qcv-btn" @click="lookupOpen=false">關閉</button></footer>
      </section>
    </div>
  </section>
</template>
<style scoped>
.qcv-page{height:100%;min-height:0;display:flex;flex-direction:column;background:#f7f6f1;color:#292929;font:13px "Segoe UI",Arial,sans-serif}
.qcv-title{display:flex;align-items:center;gap:12px;padding:10px 16px;border-bottom:1px solid #d7d5cf;background:#faf9f6;flex:none}.qcv-title h1{font-size:20px;font-weight:600;margin:0}
.qcv-dev{font-size:11px;color:#765600;background:#fff0b3;border:1px solid #e8c85e;border-radius:3px;padding:3px 7px}
.qcv-query{min-height:42px;display:flex;align-items:center;gap:9px;padding:5px 12px;border-bottom:1px solid #d2d0ca;background:#fff;flex:none}
.qcv-status{display:flex;align-items:center;gap:14px;margin-right:8px}.qcv-status label,.qcv-options label,.qcv-copy-options label{display:inline-flex;align-items:center;gap:4px;white-space:nowrap}
.qcv-status input,.qcv-options input,.qcv-copy-options input,.qcv-grid input[type=checkbox]{accent-color:#e79320}.qcv-search-label{white-space:nowrap}
.qcv-input{border:1px solid #9d9d9d;background:#fff0ad;color:#252525;border-radius:2px;min-height:28px;padding:4px 7px;font:inherit;box-sizing:border-box}.qcv-input:focus{outline:1px solid #bd8a33}
.qcv-search{width:190px;background:white}.qcv-btn{min-height:30px;padding:4px 10px;border:1px solid #bdbab4;border-radius:3px;background:linear-gradient(#fff,#f1f0ed);color:#252525;font:inherit;white-space:nowrap;cursor:pointer}
.qcv-btn:hover:not(:disabled){background:#fff5d6;border-color:#c68a2b}.qcv-btn:disabled{opacity:.52;cursor:default}.qcv-primary{background:#ffae32;border-color:#e6991e;color:white;font-weight:600}
.qcv-message{padding:6px 12px;color:#47633a;background:#f0f6eb;border-bottom:1px solid #d2d0ca}.qcv-error,.qcv-dialog-error{color:#9c2e19;background:#fff1ec}
.qcv-main{display:flex;gap:8px;padding:7px 8px 10px;flex:1;min-height:0}.qcv-grid-area{flex:1;min-width:0;display:flex;flex-direction:column;border:1px solid #c8c5be;background:#fff}
.qcv-actionbar{display:flex;align-items:center;gap:5px;padding:5px;border-bottom:1px solid #d5d2ca;min-height:34px}.qcv-toolbar-gap{flex:1}.qcv-loading{color:#74694d}
.qcv-grid-scroll{overflow:auto;flex:1;min-height:0}.qcv-grid{border-collapse:collapse;min-width:100%;width:max-content;background:#fff}
.qcv-grid th,.qcv-grid td{border:1px solid #c8c8c8;padding:4px 7px;white-space:nowrap;height:25px;text-align:left}.qcv-grid th{background:#e9e7e2;color:#303030;font-weight:600;position:sticky;top:0;z-index:1}
.qcv-grid td{max-width:250px;overflow:hidden;text-overflow:ellipsis}.qcv-grid tbody tr:hover{background:#fff8df}.qcv-grid tbody tr.selected{background:#ffe6a3}
.qcv-grid .qcv-check{width:36px;text-align:center;padding:3px}.qcv-grid .num{text-align:right}.qcv-empty{text-align:center!important;color:#888;height:55px!important}
.qcv-grid-footer{padding:5px 8px;color:#666;border-top:1px solid #d5d2ca;background:#faf9f6}.qcv-actions{width:140px;flex:none;display:flex;flex-direction:column;gap:6px}
.qcv-actions .qcv-btn{height:34px}.qcv-action-spacer{flex:1}
.qcv-overlay{position:fixed;inset:0;z-index:5000;background:#0006;display:flex;align-items:center;justify-content:center;padding:18px}
.qcv-editor,.qcv-child-dialog,.qcv-lookup{background:#f7f6f1;border:1px solid #9d9a93;box-shadow:0 8px 28px #0004;display:flex;flex-direction:column;max-height:94vh}
.qcv-editor{width:min(710px,96vw);height:min(743px,94vh)}.qcv-child-dialog{width:min(1000px,96vw);height:min(650px,92vh)}
.qcv-dialog-title{display:flex;align-items:center;justify-content:space-between;padding:9px 14px;border-bottom:1px solid #cbc8c0;background:#fbfaf7;flex:none}
.qcv-dialog-title h2{font-size:16px;font-weight:600;margin:0}.qcv-dialog-title p{margin:4px 0 0;color:#666}.qcv-x{border:0;background:transparent;font-size:24px;color:#555;cursor:pointer}
.qcv-editor-scroll{overflow:auto;padding:8px 12px;flex:1}.qcv-fieldset{border:1px solid #bcb9b1;margin:4px 0 10px;padding:10px}.qcv-fieldset legend{padding:0 5px;color:#45423c}
.qcv-basic{min-height:500px}.qcv-field-grid{display:grid;grid-template-columns:1fr 1fr;gap:8px 12px}.qcv-field{display:flex;align-items:center;gap:8px;min-width:0}
.qcv-field>span{width:130px;text-align:right;flex:none;color:#333}.qcv-field>.qcv-input,.qcv-field select.qcv-input{flex:1;min-width:0}
.qcv-input-action{display:flex;flex:1;min-width:0;gap:4px}.qcv-input-action .qcv-input{flex:1;min-width:0}
.qcv-options{display:flex;align-items:center;gap:12px;min-height:28px;flex-wrap:wrap}.qcv-options-wrap{max-width:260px}
.qcv-description{grid-column:1/-1;align-items:flex-start}.qcv-description textarea{flex:1;resize:vertical}.qcv-copy-options{padding:5px 8px}
.qcv-audit{display:grid;grid-template-columns:1fr 1fr;gap:8px;border:1px solid #bcb9b1;padding:10px}.qcv-audit legend{padding:0 5px}
.qcv-audit label{display:grid;grid-template-columns:80px 1fr;align-items:center;gap:6px}.qcv-audit input{min-width:0;background:#ededeb;border:1px solid #c6c4be;padding:5px}
.qcv-dialog-actions{display:flex;justify-content:flex-end;gap:8px;padding:9px 12px;border-top:1px solid #cbc8c0;background:#faf9f6;flex:none}
.qcv-dialog-error{margin:0;padding:7px 12px}.qcv-child-toolbar,.qcv-memo-toolbar{display:flex;gap:6px;align-items:center;padding:8px 10px;border-bottom:1px solid #d4d1ca;flex:none}
.qcv-child-toolbar select{min-width:220px}.qcv-child-grid{flex:1;min-height:0;overflow:auto;background:#fff;margin:8px;border:1px solid #c8c5be}
.qcv-child-grid .qcv-grid{width:100%}.qcv-child-grid .qcv-grid td .qcv-input{width:100%;min-width:80px;min-height:25px}
.qcv-child-hint{padding:6px 12px;margin:0;color:#665d4b}.qcv-lookup-overlay{z-index:5100}.qcv-copy-overlay{z-index:5200}.qcv-copy-selector{width:min(556px,96vw);height:min(462px,90vh);background:#f7f6f1;border:1px solid #9d9a93;box-shadow:0 8px 28px #0004;display:flex;flex-direction:column}.qcv-copy-choice{display:flex;gap:24px;margin:10px 12px 0;padding:8px;border:1px solid #bcb9b1}.qcv-copy-choice label{display:flex;align-items:center;gap:5px}.qcv-copy-choice input{accent-color:#e79320}.qcv-copy-grids{display:grid;grid-template-columns:1fr 1fr;gap:8px;padding:8px 12px;flex:1;min-height:0}.qcv-copy-grids section{min-width:0;overflow:auto;border:1px solid #ccc;background:#fff}.qcv-copy-grids h3{font-size:13px;font-weight:600;margin:0;padding:6px 8px;background:#e9e7e2;border-bottom:1px solid #ccc}.qcv-copy-grids .qcv-grid{width:100%;min-width:0}.qcv-lookup{width:min(440px,95vw);padding-bottom:5px}
.qcv-lookup>.qcv-input{margin:10px}.qcv-lookup-row{margin:0 10px;border:0;border-bottom:1px solid #ddd;background:#fff;text-align:left;padding:8px;cursor:pointer}.qcv-lookup-row:hover{background:#fff0ad}
@media(max-width:1050px){.qcv-main{flex-direction:column}.qcv-actions{width:auto;flex-direction:row;flex-wrap:wrap}.qcv-action-spacer{display:none}.qcv-actions .qcv-btn{flex:1}.qcv-query{flex-wrap:wrap}.qcv-status{width:100%}.qcv-field-grid{grid-template-columns:1fr}.qcv-field>span{width:145px}.qcv-basic{min-height:unset}}
</style>
