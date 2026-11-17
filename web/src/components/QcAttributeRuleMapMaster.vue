<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

type EditorMode = 'add' | 'edit' | 'copy'
type DetailMode = 'add' | 'edit' | null
interface FactorDef { factorNo: string; factorName: string; parameterType: number; options: string[] }
interface QcItem { qcItemNo: string; qcItemName: string; creator: string; createDate: string; ruleNo: string; componentCollect: number }
interface RuleOption { ruleNo: string; ruleName: string; componentCollect: number }
interface PlanOption { planNo: string; planName: string; planType: number }
interface BasisRow { qcAttSpSerial: string; qcItemNo: string; issueState: number; creator: string; createDate: string; factorValues: Record<string, string>; canEdit?: boolean }
interface DetailRow { retestLot: number; planNo: string; lotRuleNo: string; comRuleNo: string }
interface ParameterRow { retestLot: number; parameterNo: string; parameterValue: string }
interface RuleMapRecord { basis: BasisRow; details: DetailRow[]; parameters: ParameterRow[] }
interface SupportOptions {
  factors: FactorDef[]
  factorOptions: Record<string, string[]>
  rules: RuleOption[]
  plans: PlanOption[]
  waferPlans: PlanOption[]
  planParameters: Record<string, Array<{ parameterNo: string; parameterValue: string }>>
}
interface MockStore { items: QcItem[]; records: RuleMapRecord[]; support: SupportOptions }
interface ListPayload<T> { items?: T[]; rows?: T[]; data?: T[] }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const basePath = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const mockKey = 'ymes-dev-mock:qc-attribute-rule-map:v1'
const issueState = ref(0)
const qcItems = ref<QcItem[]>([])
const selectedItem = ref<QcItem | null>(null)
const factors = ref<FactorDef[]>([])
const support = ref<SupportOptions>(emptySupport())
const records = ref<BasisRow[]>([])
const activeSerial = ref('')
const selectedSerials = ref<string[]>([])
const details = ref<DetailRow[]>([])
const parameters = ref<ParameterRow[]>([])
const activeRetestLot = ref<number | null>(null)
const loading = ref(false)
const saving = ref(false)
const errorMessage = ref('')
const notice = ref('')
const lookupOpen = ref(false)
const lookupFilter = ref('')
const lookupSelectedItemNo = ref('')
const editorOpen = ref(false)
const editorMode = ref<EditorMode>('add')
const editorError = ref('')
const editorDirty = ref(false)
const draftFactorValues = ref<Record<string, string>>({})
const draftDetails = ref<DetailRow[]>([])
const draftParameters = ref<ParameterRow[]>([])
const selectedDraftRetestLot = ref<number | null>(null)
const detailMode = ref<DetailMode>(null)
const detailForm = ref<DetailRow>(blankDetail())
const detailParameters = ref<ParameterRow[]>([])
const detailSnapshot = ref<{ details: DetailRow[]; parameters: ParameterRow[] } | null>(null)
const parameterDialogOpen = ref(false)
const parameterDraft = ref<ParameterRow[]>([])
const parameterNoInput = ref('')
const parameterValueInput = ref('')

const issueStates = [
  { value: 0, label: '未審核' },
  { value: 1, label: '簽核中' },
  { value: 2, label: '已簽核' },
  { value: -1, label: '不使用' },
]
const visibleItems = computed(() => qcItems.value.filter(item => {
  const q = lookupFilter.value.trim().toLowerCase()
  return !q || item.qcItemNo.toLowerCase().includes(q) || item.qcItemName.toLowerCase().includes(q)
}))
const activeRecord = computed(() => records.value.find(row => row.qcAttSpSerial === activeSerial.value) ?? null)
const editorReadOnly = computed(() => editorMode.value === 'edit' && !!activeRecord.value && activeRecord.value.issueState !== 0 && activeRecord.value.canEdit !== true)
const detailRows = computed(() => details.value.map(row => ({ ...row, retestLotLabel: row.retestLot === 0 ? 'First' : String(row.retestLot) })))
const currentDraftDetails = computed(() => draftDetails.value.map(row => ({ ...row, retestLotLabel: row.retestLot === 0 ? 'First' : String(row.retestLot) })))
const detailFormPlan = computed(() => [...support.value.plans, ...support.value.waferPlans].find(plan => plan.planNo === detailForm.value.planNo) ?? null)
const detailParametersValid = computed(() => detailParameters.value.length > 0)
const editorTitle = computed(() => ({ add: '計數品管項目判別法則對應 (ADD)', edit: '計數品管項目判別法則對應 (EDIT)', copy: '計數品管項目判別法則對應 (COPY)' }[editorMode.value]))
const eligiblePlanOptions = computed(() => selectedItem.value?.componentCollect === 0 ? support.value.plans : support.value.waferPlans)
const eligibleLotRules = computed(() => support.value.rules.filter(row => row.componentCollect === 0))
const eligibleComponentRules = computed(() => support.value.rules.filter(row => row.componentCollect === 1))

function emptySupport(): SupportOptions { return { factors: [], factorOptions: {}, rules: [], plans: [], waferPlans: [], planParameters: {} } }
function blankDetail(): DetailRow { return { retestLot: 0, planNo: '', lotRuleNo: '', comRuleNo: '' } }
function demoStore(): MockStore {
  const items: QcItem[] = [
    { qcItemNo: 'DEV-QCA-001', qcItemName: '外觀缺陷計數（DEMO）', creator: 'DEV001', createDate: '2026/11/17 08:00:00', ruleNo: 'DEV-ITEM-RULE-01', componentCollect: 0 },
    { qcItemNo: 'DEV-QCA-002', qcItemName: '元件斷裂計數（DEMO）', creator: 'DEV001', createDate: '2026/11/16 13:40:00', ruleNo: 'DEV-ITEM-RULE-02', componentCollect: 1 },
  ]
  const support: SupportOptions = {
    factors: [
      { factorNo: 'MO', factorName: '工單', parameterType: 1, options: ['*', 'DEV-MO-1001', 'DEV-MO-1002'] },
      { factorNo: 'PRODUCT', factorName: '產品', parameterType: 1, options: ['*', 'DEV-PRD-A', 'DEV-PRD-B'] },
      { factorNo: 'OP', factorName: '作業站', parameterType: 1, options: ['*', 'DEV-OP-ASSY', 'DEV-OP-PACK'] },
    ],
    factorOptions: {},
    rules: [
      { ruleNo: 'DEV-LOT-01', ruleName: '一般批次判別（DEMO）', componentCollect: 0 },
      { ruleNo: 'DEV-LOT-AQL', ruleName: 'AQL 抽樣判別（DEMO）', componentCollect: 0 },
      { ruleNo: 'DEV-COMP-01', ruleName: '元件判別（DEMO）', componentCollect: 1 },
    ],
    plans: [
      { planNo: 'DEV-PLAN-100', planName: '一般抽樣計劃（DEMO）', planType: 0 },
      { planNo: 'DEV-PLAN-AQL', planName: 'AQL 抽樣計劃（DEMO）', planType: 2 },
    ],
    waferPlans: [{ planNo: 'DEV-WAFER-01', planName: '元件抽樣計劃（DEMO）', planType: 0 }],
    planParameters: {
      'DEV-PLAN-100': [{ parameterNo: 'SAMPLE_SIZE', parameterValue: '5' }, { parameterNo: 'ACCEPT_QTY', parameterValue: '0' }],
      'DEV-PLAN-AQL': [{ parameterNo: 'AQL_LEVEL', parameterValue: 'II' }, { parameterNo: 'AQL_VALUE', parameterValue: '1.0' }],
      'DEV-WAFER-01': [{ parameterNo: 'WAFER_SAMPLE_QTY', parameterValue: '10' }],
    },
  }
  const records: RuleMapRecord[] = [
    {
      basis: { qcAttSpSerial: 'SP00000141', qcItemNo: items[0].qcItemNo, issueState: 0, creator: 'DEV001', createDate: '2026/11/17 08:15:00', factorValues: { MO: 'DEV-MO-1001', PRODUCT: 'DEV-PRD-A', OP: 'DEV-OP-ASSY' } },
      details: [{ retestLot: 0, planNo: 'DEV-PLAN-100', lotRuleNo: 'DEV-LOT-01', comRuleNo: '' }, { retestLot: 1, planNo: 'DEV-PLAN-AQL', lotRuleNo: 'DEV-LOT-AQL', comRuleNo: '' }],
      parameters: [{ retestLot: 0, parameterNo: 'SAMPLE_SIZE', parameterValue: '5' }, { retestLot: 0, parameterNo: 'ACCEPT_QTY', parameterValue: '0' }, { retestLot: 1, parameterNo: 'AQL_LEVEL', parameterValue: 'II' }, { retestLot: 1, parameterNo: 'AQL_VALUE', parameterValue: '1.0' }],
    },
    {
      basis: { qcAttSpSerial: 'SP00000142', qcItemNo: items[0].qcItemNo, issueState: 2, creator: 'DEV002', createDate: '2026/11/15 10:10:00', factorValues: { MO: '*', PRODUCT: 'DEV-PRD-B', OP: 'DEV-OP-PACK' } },
      details: [{ retestLot: 0, planNo: 'DEV-PLAN-AQL', lotRuleNo: 'DEV-LOT-AQL', comRuleNo: '' }],
      parameters: [{ retestLot: 0, parameterNo: 'AQL_LEVEL', parameterValue: 'II' }, { retestLot: 0, parameterNo: 'AQL_VALUE', parameterValue: '1.0' }],
    },
    {
      basis: { qcAttSpSerial: 'SP00000143', qcItemNo: items[1].qcItemNo, issueState: 1, creator: 'DEV001', createDate: '2026/11/14 09:05:00', factorValues: { MO: 'DEV-MO-1002', PRODUCT: '*', OP: 'DEV-OP-ASSY' } },
      details: [{ retestLot: 0, planNo: 'DEV-WAFER-01', lotRuleNo: '', comRuleNo: 'DEV-COMP-01' }],
      parameters: [{ retestLot: 0, parameterNo: 'WAFER_SAMPLE_QTY', parameterValue: '10' }],
    },
  ]
  return { items, records, support }
}
function readStore(): MockStore {
  try {
    const raw = window.localStorage.getItem(mockKey)
    if (raw) {
      const parsed = JSON.parse(raw) as MockStore
      if (Array.isArray(parsed.items) && Array.isArray(parsed.records) && parsed.support) return parsed
    }
  } catch { /* initialize isolated DEV records */ }
  const value = demoStore()
  saveStore(value)
  return value
}
function saveStore(store: MockStore): void { window.localStorage.setItem(mockKey, JSON.stringify(store)) }
function listOf<T>(payload: T[] | ListPayload<T> | null | undefined): T[] {
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
  if (response.status === 401) { window.location.assign(basePath + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; title?: string; message?: string } | null
    throw new Error(problem?.detail || problem?.message || problem?.title || 'API 呼叫失敗（' + response.status + '）。')
  }
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}
function normalizeItem(row: Record<string, unknown>): QcItem {
  return {
    qcItemNo: String(row.qcItemNo ?? row.QCITEMNO ?? ''),
    qcItemName: String(row.qcItemName ?? row.QCITEMNAME ?? ''),
    creator: String(row.creator ?? row.CREATOR ?? ''),
    createDate: String(row.createDate ?? row.CREATEDATE ?? ''),
    ruleNo: String(row.ruleNo ?? row.RULENO ?? ''),
    componentCollect: Number(row.componentCollect ?? row.COMPONENTCOLLECT ?? -1),
  }
}
function normalizeFactor(row: Record<string, unknown>): FactorDef {
  return {
    factorNo: String(row.factorNo ?? row.FACTORNO ?? ''),
    factorName: String(row.factorName ?? row.FACTORNAME ?? row.parameterName ?? row.PARAMETERNAME ?? ''),
    parameterType: Number(row.parameterType ?? row.PARAMETERTYPE ?? 0),
    options: Array.isArray(row.options) ? row.options.map(String) : [],
  }
}
function normalizeBasis(row: Record<string, unknown>): BasisRow {
  const values = row.factorValues ?? row.FACTORVALUES
  const factorsFromColumns = Object.fromEntries(Object.entries(row).filter(([key]) => !['QCATTSP SERIAL','QCATTSPSERIAL','QCITEMNO','ISSUESTATE','CREATOR','CREATEDATE'].includes(key.toUpperCase())).map(([key, value]) => [key, String(value ?? '*')]))
  return {
    qcAttSpSerial: String(row.qcAttSpSerial ?? row.QCATTSPSERIAL ?? ''),
    qcItemNo: String(row.qcItemNo ?? row.QCITEMNO ?? ''),
    issueState: Number(row.issueState ?? row.ISSUESTATE ?? 0),
    creator: String(row.creator ?? row.CREATOR ?? ''),
    createDate: String(row.createDate ?? row.CREATEDATE ?? ''),
    factorValues: values && typeof values === 'object' ? Object.fromEntries(Object.entries(values as Record<string, unknown>).map(([key, value]) => [key, String(value ?? '*')])) : factorsFromColumns,
    canEdit: row.canEdit === true || row.CANEDIT === true,
  }
}
function normalizeDetail(row: Record<string, unknown>): DetailRow {
  return { retestLot: Number(row.retestLot ?? row.RETESTLOT ?? 0), planNo: String(row.planNo ?? row.PLANNO ?? ''), lotRuleNo: String(row.lotRuleNo ?? row.LOTRULENO ?? ''), comRuleNo: String(row.comRuleNo ?? row.COMRULENO ?? '') }
}
function normalizeParameter(row: Record<string, unknown>): ParameterRow {
  return { retestLot: Number(row.retestLot ?? row.RETESTLOT ?? 0), parameterNo: String(row.parameterNo ?? row.PARAMETERNO ?? ''), parameterValue: String(row.parameterValue ?? row.PARAMETERVALUE ?? '') }
}
function normalizeOptions(row: Record<string, unknown>): SupportOptions {
  const normalizePlans = (values: unknown): PlanOption[] => listOf(values as Array<Record<string, unknown>> | ListPayload<Record<string, unknown>>).map(plan => ({
    planNo: String(plan.planNo ?? plan.PLANNO ?? ''),
    planName: String(plan.planName ?? plan.PLANNAME ?? plan.planNo ?? plan.PLANNO ?? ''),
    planType: Number(plan.planType ?? plan.PLANTYPE ?? 0),
  }))
  const factorOptions = row.factorOptions && typeof row.factorOptions === 'object' ? row.factorOptions as Record<string, unknown> : {}
  const planParameters = row.planParameters && typeof row.planParameters === 'object' ? row.planParameters as Record<string, unknown> : {}
  return {
    factors: listOf((row.factors ?? []) as Array<Record<string, unknown>> | ListPayload<Record<string, unknown>>).map(normalizeFactor),
    factorOptions: Object.fromEntries(Object.entries(factorOptions).map(([key, values]) => [key, Array.isArray(values) ? values.map(String) : []])),
    rules: listOf((row.rules ?? row.attributeRules ?? []) as Array<Record<string, unknown>> | ListPayload<Record<string, unknown>>).map(rule => ({ ruleNo: String(rule.ruleNo ?? rule.RULENO ?? ''), ruleName: String(rule.ruleName ?? rule.RULENAME ?? ''), componentCollect: Number(rule.componentCollect ?? rule.COMPONENTCOLLECT ?? 0) })),
    plans: normalizePlans(row.plans),
    waferPlans: normalizePlans(row.waferPlans),
    planParameters: Object.fromEntries(Object.entries(planParameters).map(([planNo, value]) => [planNo, listOf(value as Array<Record<string, unknown>> | ListPayload<Record<string, unknown>>).map(normalizeParameter).map(parameter => ({ parameterNo: parameter.parameterNo, parameterValue: parameter.parameterValue }))])),
  }
}
function formatDate(value: string): string { return value ? value.replace('T', ' ').slice(0, 19) : '' }
function itemStatusLabel(value: number): string { return issueStates.find(row => row.value === value)?.label ?? String(value) }
function factorValue(row: BasisRow, factorNo: string): string { return row.factorValues[factorNo] ?? '*' }
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }

async function openItemLookup(): Promise<void> {
  errorMessage.value = ''
  lookupFilter.value = ''
  lookupSelectedItemNo.value = selectedItem.value?.qcItemNo ?? ''
  lookupOpen.value = true
  if (qcItems.value.length) return
  try {
    if (mock.value) qcItems.value = readStore().items
    else qcItems.value = listOf(await request<ListPayload<Record<string, unknown>>>('api/qc/attribute-qc-items?issueState=2')).map(normalizeItem)
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '載入計數品管項目失敗。'; lookupOpen.value = false }
}
async function chooseItem(item: QcItem): Promise<void> {
  selectedItem.value = item
  lookupOpen.value = false
  activeSerial.value = ''
  selectedSerials.value = []
  details.value = []
  parameters.value = []
  activeRetestLot.value = null
  loading.value = true
  errorMessage.value = ''
  notice.value = ''
  try {
    if (mock.value) {
      const store = readStore()
      factors.value = store.support.factors
      support.value = clone(store.support)
    } else {
      const encoded = encodeURIComponent(item.qcItemNo)
      const [factorPayload, optionPayload] = await Promise.all([
        request<ListPayload<Record<string, unknown>>>('api/qc/attribute-qc-items/' + encoded + '/factors?factorOnline=Y'),
        request<Record<string, unknown>>('api/qc/attribute-rule-maps/options?qcItemNo=' + encoded),
      ])
      const optionSupport = normalizeOptions(optionPayload)
      factors.value = listOf(factorPayload).map(normalizeFactor)
      support.value = {
        ...optionSupport,
        factors: (optionSupport.factors.length ? optionSupport.factors : factors.value).map(factor => ({ ...factor, options: optionSupport.factorOptions[factor.factorNo] ?? factor.options })),
      }
    }
    await loadRecords()
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '載入法則對應資料失敗。' }
  finally { loading.value = false }
}
async function loadRecords(): Promise<void> {
  if (!selectedItem.value) { records.value = []; return }
  const wasLoading = loading.value
  loading.value = true
  errorMessage.value = ''
  try {
    if (mock.value) {
      records.value = readStore().records.filter(row => row.basis.qcItemNo === selectedItem.value?.qcItemNo && row.basis.issueState === issueState.value).map(row => row.basis)
    } else {
      const params = new URLSearchParams({ qcItemNo: selectedItem.value.qcItemNo, issueState: String(issueState.value) })
      records.value = listOf(await request<ListPayload<Record<string, unknown>>>('api/qc/attribute-rule-maps?' + params.toString())).map(normalizeBasis)
    }
    const first = records.value[0]
    activeSerial.value = first?.qcAttSpSerial ?? ''
    selectedSerials.value = first ? [first.qcAttSpSerial] : []
    if (first) await loadRecordDetails(first.qcAttSpSerial)
    else { details.value = []; parameters.value = []; activeRetestLot.value = null }
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '載入判別法則對應清單失敗。' }
  finally { if (!wasLoading) loading.value = false }
}
async function loadRecordDetails(serial: string): Promise<void> {
  if (!serial) return
  try {
    if (mock.value) {
      const record = readStore().records.find(row => row.basis.qcAttSpSerial === serial)
      details.value = clone(record?.details ?? []).sort((a, b) => a.retestLot - b.retestLot)
      parameters.value = clone(record?.parameters ?? [])
    } else {
      const payload = await request<Record<string, unknown>>('api/qc/attribute-rule-maps/' + encodeURIComponent(serial))
      details.value = listOf((payload.details ?? []) as Array<Record<string, unknown>> | ListPayload<Record<string, unknown>>).map(normalizeDetail).sort((a, b) => a.retestLot - b.retestLot)
      parameters.value = listOf((payload.parameters ?? []) as Array<Record<string, unknown>> | ListPayload<Record<string, unknown>>).map(normalizeParameter)
    }
    activeRetestLot.value = details.value[0]?.retestLot ?? null
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '載入明細與參數失敗。'; details.value = []; parameters.value = [] }
}
function selectRecord(row: BasisRow, event: MouseEvent): void {
  activeSerial.value = row.qcAttSpSerial
  if (event.ctrlKey || event.metaKey) selectedSerials.value = selectedSerials.value.includes(row.qcAttSpSerial) ? selectedSerials.value.filter(value => value !== row.qcAttSpSerial) : [...selectedSerials.value, row.qcAttSpSerial]
  else if (!selectedSerials.value.includes(row.qcAttSpSerial)) selectedSerials.value = [row.qcAttSpSerial]
  void loadRecordDetails(row.qcAttSpSerial)
}
function toggleRecord(row: BasisRow, checked: boolean): void {
  selectedSerials.value = checked ? [...new Set([...selectedSerials.value, row.qcAttSpSerial])] : selectedSerials.value.filter(value => value !== row.qcAttSpSerial)
  activeSerial.value = row.qcAttSpSerial
  void loadRecordDetails(row.qcAttSpSerial)
}
function selectDetail(row: DetailRow): void { activeRetestLot.value = row.retestLot }
function parametersForRetestLot(retestLot: number): ParameterRow[] { return parameters.value.filter(row => row.retestLot === retestLot) }
function openEditor(mode: EditorMode): void {
  errorMessage.value = ''
  editorError.value = ''
  if (!selectedItem.value) { errorMessage.value = '請先查詢並選取一個已核准的計數品管項目。'; return }
  const source = mode === 'add' ? null : activeRecord.value
  if (mode !== 'add' && !source) { errorMessage.value = '請先選取一筆對應資料。'; return }
  if (mode !== 'edit' && !selectedItem.value.ruleNo) { errorMessage.value = '此計數品管項目未設定 RuleNo，無法新增或複製對應資料。'; return }
  if (!factors.value.length) { errorMessage.value = '此計數品管項目沒有 FactorOnline=Y 的要因。'; return }
  editorMode.value = mode
  draftFactorValues.value = source ? { ...source.factorValues } : Object.fromEntries(factors.value.map(factor => [factor.factorNo, '']))
  const record = source ? readRecordForEditor(source.qcAttSpSerial) : null
  draftDetails.value = record ? clone(record.details) : []
  draftParameters.value = record ? clone(record.parameters) : []
  selectedDraftRetestLot.value = draftDetails.value[0]?.retestLot ?? null
  detailMode.value = null
  detailSnapshot.value = null
  editorDirty.value = false
  editorOpen.value = true
}
function readRecordForEditor(serial: string): RuleMapRecord | null {
  if (mock.value) return readStore().records.find(row => row.basis.qcAttSpSerial === serial) ?? null
  return { basis: activeRecord.value ?? blankBasis(serial), details: clone(details.value), parameters: clone(parameters.value) }
}
function blankBasis(serial: string): BasisRow { return { qcAttSpSerial: serial, qcItemNo: selectedItem.value?.qcItemNo ?? '', issueState: issueState.value, creator: '', createDate: '', factorValues: {} } }
function markEditorDirty(): void { editorDirty.value = true }
function changeFactorValue(factorNo: string, value: string): void { draftFactorValues.value[factorNo] = value; markEditorDirty() }
function openDetailEditor(mode: Exclude<DetailMode, null>): void {
  editorError.value = ''
  detailSnapshot.value = { details: clone(draftDetails.value), parameters: clone(draftParameters.value) }
  detailMode.value = mode
  if (mode === 'edit') {
    const source = draftDetails.value.find(row => row.retestLot === selectedDraftRetestLot.value)
    if (!source) { detailMode.value = null; editorError.value = '請先選取一筆明細。'; return }
    detailForm.value = { ...source }
    detailParameters.value = clone(draftParameters.value.filter(row => row.retestLot === source.retestLot))
  } else {
    detailForm.value = blankDetail()
    detailParameters.value = []
  }
}
function cancelDetailEditor(): void {
  if (detailSnapshot.value) {
    draftDetails.value = detailSnapshot.value.details
    draftParameters.value = detailSnapshot.value.parameters
  }
  detailMode.value = null
  detailSnapshot.value = null
}
function setRetestType(isRetest: boolean): void {
  if (detailMode.value === 'edit') return
  detailForm.value.retestLot = isRetest ? Math.max(1, detailForm.value.retestLot || 1) : 0
}
function saveDetailEditor(): void {
  const row = detailForm.value
  if (!row.planNo.trim()) { editorError.value = 'PlanNo 必填。'; return }
  const plan = detailFormPlan.value
  if (selectedItem.value?.componentCollect === 0) {
    if (!row.lotRuleNo && plan?.planType !== 2) { editorError.value = '此抽樣計劃必須指定 LotRuleNo。'; return }
  } else if (!row.lotRuleNo && !row.comRuleNo) {
    editorError.value = 'LotRuleNo 與 ComRuleNo 至少需設定一項。'; return
  }
  if (!detailParametersValid.value) { editorError.value = '請先設定此 RetestLot 的抽樣參數。'; return }
  const exists = draftDetails.value.some(item => item.retestLot === row.retestLot && (detailMode.value !== 'edit' || item.retestLot !== selectedDraftRetestLot.value))
  if (exists) { editorError.value = '此 RetestLot 已存在。'; return }
  if (detailMode.value === 'edit') draftDetails.value = draftDetails.value.map(item => item.retestLot === selectedDraftRetestLot.value ? { ...row } : item)
  else draftDetails.value = [...draftDetails.value, { ...row }]
  draftDetails.value.sort((a, b) => a.retestLot - b.retestLot)
  draftParameters.value = [...draftParameters.value.filter(item => item.retestLot !== selectedDraftRetestLot.value), ...detailParameters.value.map(item => ({ ...item, retestLot: row.retestLot }))]
  selectedDraftRetestLot.value = row.retestLot
  editorDirty.value = true
  detailMode.value = null
  detailSnapshot.value = null
  editorError.value = ''
}
function deleteDraftDetail(): void {
  if (selectedDraftRetestLot.value === null) { editorError.value = '請先選取一筆明細。'; return }
  if (!window.confirm('確定刪除此 RetestLot 明細及其參數？')) return
  draftDetails.value = draftDetails.value.filter(row => row.retestLot !== selectedDraftRetestLot.value)
  draftParameters.value = draftParameters.value.filter(row => row.retestLot !== selectedDraftRetestLot.value)
  selectedDraftRetestLot.value = draftDetails.value[0]?.retestLot ?? null
  editorDirty.value = true
  editorError.value = ''
}
function openParameterDialog(): void {
  if (!detailForm.value.planNo.trim()) { editorError.value = '請先選取 PlanNo，再載入參數。'; return }
  parameterDraft.value = detailParameters.value.length ? clone(detailParameters.value) : clone((support.value.planParameters[detailForm.value.planNo] ?? []).map(row => ({ retestLot: detailForm.value.retestLot, ...row })))
  parameterNoInput.value = ''
  parameterValueInput.value = ''
  parameterDialogOpen.value = true
}
function addParameter(): void {
  const parameterNo = parameterNoInput.value.trim().toUpperCase()
  if (!parameterNo) { editorError.value = 'ParameterNo 必填。'; return }
  if (parameterDraft.value.some(row => row.parameterNo.toUpperCase() === parameterNo)) { editorError.value = 'ParameterNo 不可重複。'; return }
  parameterDraft.value.push({ retestLot: detailForm.value.retestLot, parameterNo, parameterValue: parameterValueInput.value })
  parameterNoInput.value = ''
  parameterValueInput.value = ''
  editorError.value = ''
}
function removeParameter(parameterNo: string): void { parameterDraft.value = parameterDraft.value.filter(row => row.parameterNo !== parameterNo) }
function applyParameterDraft(): void {
  if (!parameterDraft.value.length) { editorError.value = '至少需保留一筆抽樣參數。'; return }
  detailParameters.value = clone(parameterDraft.value).map(row => ({ ...row, retestLot: detailForm.value.retestLot }))
  parameterDialogOpen.value = false
  editorError.value = ''
}
function duplicateFactorCombination(): boolean {
  const normalized = JSON.stringify(Object.fromEntries(Object.entries(draftFactorValues.value).sort(([a], [b]) => a.localeCompare(b))))
  if (mock.value) return readStore().records.some(record => record.basis.qcItemNo === selectedItem.value?.qcItemNo && record.basis.qcAttSpSerial !== (editorMode.value === 'edit' ? activeSerial.value : '') && JSON.stringify(Object.fromEntries(Object.entries(record.basis.factorValues).sort(([a], [b]) => a.localeCompare(b)))) === normalized)
  return false
}
function validateEditor(): boolean {
  if (!selectedItem.value) { editorError.value = '請選取計數品管項目。'; return false }
  for (const factor of factors.value) {
    if (!String(draftFactorValues.value[factor.factorNo] ?? '').trim()) { editorError.value = '要因 ' + factor.factorNo + ' 必須設定值；若不限制請選 *。'; return false }
  }
  if (!draftDetails.value.length) { editorError.value = '至少需建立一筆判別法則明細。'; return false }
  for (const detail of draftDetails.value) {
    const plan = [...support.value.plans, ...support.value.waferPlans].find(row => row.planNo === detail.planNo)
    if (!detail.planNo) { editorError.value = '每筆 RetestLot 都必須設定 PlanNo。'; return false }
    if (selectedItem.value.componentCollect === 0 && plan?.planType !== 2 && !detail.lotRuleNo) { editorError.value = 'RetestLot ' + detail.retestLot + ' 必須設定 LotRuleNo。'; return false }
    if (selectedItem.value.componentCollect !== 0 && !detail.lotRuleNo && !detail.comRuleNo) { editorError.value = 'RetestLot ' + detail.retestLot + ' 必須設定 LotRuleNo 或 ComRuleNo。'; return false }
    if (!draftParameters.value.some(row => row.retestLot === detail.retestLot)) { editorError.value = 'RetestLot ' + detail.retestLot + ' 尚未設定抽樣參數。'; return false }
  }
  if (editorMode.value !== 'edit' && duplicateFactorCombination()) { editorError.value = '此 QCItemNo 與全部線上要因組合已存在。'; return false }
  return true
}
async function saveEditor(closeAfter: boolean): Promise<void> {
  editorError.value = ''
  if (!validateEditor()) return
  saving.value = true
  try {
    const body = editorMode.value === 'edit'
      ? { details: draftDetails.value, parameters: draftParameters.value }
      : { qcItemNo: selectedItem.value?.qcItemNo, factorValues: draftFactorValues.value, details: draftDetails.value, parameters: draftParameters.value }
    if (mock.value) {
      const store = readStore()
      if (editorMode.value === 'edit') {
        const index = store.records.findIndex(row => row.basis.qcAttSpSerial === activeSerial.value)
        if (index >= 0) store.records[index] = { basis: { ...store.records[index].basis, factorValues: { ...draftFactorValues.value } }, details: clone(draftDetails.value), parameters: clone(draftParameters.value) }
      } else {
        const maxSerial = store.records.reduce((max, row) => Math.max(max, Number(row.basis.qcAttSpSerial.replace(/^SP/, '')) || 0), 0)
        const serial = 'SP' + String(maxSerial + 1).padStart(8, '0')
        store.records.push({
          basis: { qcAttSpSerial: serial, qcItemNo: selectedItem.value!.qcItemNo, issueState: 0, creator: props.currentUserNo || 'DEV001', createDate: new Date().toLocaleString('sv-SE').replace('T', ' '), factorValues: { ...draftFactorValues.value } },
          details: clone(draftDetails.value), parameters: clone(draftParameters.value),
        })
      }
      saveStore(store)
    } else if (editorMode.value === 'edit') {
      await request('api/qc/attribute-rule-maps/' + encodeURIComponent(activeSerial.value), { method: 'PUT', body: JSON.stringify(body) })
    } else {
      await request('api/qc/attribute-rule-maps', { method: 'POST', body: JSON.stringify(body) })
    }
    notice.value = editorMode.value === 'edit' ? '對應資料已更新。' : '對應資料已新增。'
    editorDirty.value = false
    if (closeAfter) editorOpen.value = false
    await loadRecords()
  } catch (error) { editorError.value = error instanceof Error ? error.message : '儲存判別法則對應失敗。' }
  finally { saving.value = false }
}
async function closeEditor(): Promise<void> {
  if (editorDirty.value && window.confirm('資料已變更，是否先儲存？')) { await saveEditor(true); return }
  editorOpen.value = false
  detailMode.value = null
  parameterDialogOpen.value = false
}
async function deleteSelected(): Promise<void> {
  if (!selectedSerials.value.length) { errorMessage.value = '請先選取要刪除的資料。'; return }
  const deletable = records.value.filter(row => selectedSerials.value.includes(row.qcAttSpSerial) && [0, -1, 2].includes(row.issueState))
  if (!deletable.length) { errorMessage.value = '簽核中的資料不可刪除。'; return }
  if (!window.confirm('確定刪除選取的 ' + deletable.length + ' 筆資料？')) return
  saving.value = true
  let completed = 0
  try {
    for (const row of deletable) {
      if (mock.value) {
        const store = readStore()
        store.records = store.records.filter(record => record.basis.qcAttSpSerial !== row.qcAttSpSerial)
        saveStore(store)
      } else {
        await request('api/qc/attribute-rule-maps/' + encodeURIComponent(row.qcAttSpSerial) + '?issueState=' + row.issueState, { method: 'DELETE' })
      }
      completed++
    }
    notice.value = '已刪除 ' + completed + ' 筆資料。'
    await loadRecords()
  } catch (error) {
    errorMessage.value = (error instanceof Error ? error.message : '刪除失敗。') + ' 已完成 ' + completed + ' 筆。'
    if (completed) await loadRecords()
  } finally { saving.value = false }
}
async function approveSelected(): Promise<void> {
  if (!selectedSerials.value.length) { errorMessage.value = '請先選取要核准的資料。'; return }
  const selectedRows = records.value.filter(row => selectedSerials.value.includes(row.qcAttSpSerial))
  const approvable = selectedRows.filter(row => row.issueState === 0)
  const unusedCount = selectedRows.filter(row => row.issueState === -1).length
  if (!approvable.length) { errorMessage.value = unusedCount ? 'WinForms 簽核服務對「不使用」資料不變更狀態。' : '只有未審核資料可執行核准。'; return }
  if (!window.confirm('確定送出 ' + approvable.length + ' 筆核准？')) return
  saving.value = true
  let completed = 0
  try {
    for (const row of approvable) {
      if (mock.value) {
        const store = readStore()
        const record = store.records.find(item => item.basis.qcAttSpSerial === row.qcAttSpSerial)
        if (record) record.basis.issueState = 2
        saveStore(store)
      } else {
        await request('api/qc/attribute-rule-maps/' + encodeURIComponent(row.qcAttSpSerial) + '/approve?issueState=' + row.issueState, { method: 'POST' })
      }
      completed++
    }
    notice.value = '已送出 ' + completed + ' 筆核准。' + (unusedCount ? ' 不使用資料依 WinForms 規則未變更。' : '')
    await loadRecords()
  } catch (error) {
    errorMessage.value = (error instanceof Error ? error.message : '核准失敗。') + ' 已完成 ' + completed + ' 筆。'
    if (completed) await loadRecords()
  } finally { saving.value = false }
}
function closePage(): void { emit('close') }
function selectLookupItem(item: QcItem): void { lookupSelectedItemNo.value = item.qcItemNo }

watch(issueState, () => { if (selectedItem.value) void loadRecords() })
onMounted(() => { if (mock.value) { const store = readStore(); qcItems.value = store.items } })
</script>

<template>
  <main class="rule-map-page">
    <header class="page-heading">
      <div><div class="eyebrow">QC203 · QCITEM_A RULE MAP</div><h1>計數品管項目判別法則對應</h1></div>
      <button class="win-button close-button" type="button" @click="closePage">關閉</button>
    </header>

    <div class="query-strip">
      <div class="issue-states" role="radiogroup" aria-label="資料狀態">
        <label v-for="state in issueStates" :key="state.value"><input v-model="issueState" type="radio" :value="state.value"> {{ state.label }}</label>
      </div>
      <label class="query-label" for="qc-item-no">計數品管項目</label>
      <input id="qc-item-no" class="kd-input item-no" readonly :value="selectedItem?.qcItemNo || ''">
      <button class="lookup-button" type="button" aria-label="查詢計數品管項目" @click="openItemLookup">…</button>
      <input class="kd-input item-name" readonly :value="selectedItem?.qcItemName || ''">
      <button class="win-button" type="button" :disabled="!selectedItem || loading" @click="loadRecords">查詢</button>
    </div>

    <div v-if="errorMessage" class="message error-message" role="alert">{{ errorMessage }}</div>
    <div v-if="notice" class="message notice-message" role="status">{{ notice }}</div>

    <section class="basis-layout">
      <div class="grid-panel basis-panel">
        <div class="panel-heading"><strong>對應基本資料</strong><span>{{ selectedItem ? selectedItem.qcItemNo + ' · ' + itemStatusLabel(issueState) : '請先查詢計數品管項目' }}</span></div>
        <div class="grid-scroll">
          <table class="win-grid">
            <thead><tr>
              <th class="select-column">選取</th>
              <th v-for="factor in factors" :key="factor.factorNo">{{ factor.factorNo }}</th>
              <th>建立人</th><th>建立日期</th>
            </tr></thead>
            <tbody>
              <tr v-for="row in records" :key="row.qcAttSpSerial" :class="{ active: row.qcAttSpSerial === activeSerial }" @click="selectRecord(row, $event)">
                <td class="select-column"><input type="checkbox" :checked="selectedSerials.includes(row.qcAttSpSerial)" :aria-label="'選取 ' + row.qcAttSpSerial" @click.stop @change="toggleRecord(row, ($event.target as HTMLInputElement).checked)"></td>
                <td v-for="factor in factors" :key="factor.factorNo">{{ factorValue(row, factor.factorNo) }}</td>
                <td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td>
              </tr>
              <tr v-if="!records.length"><td :colspan="Math.max(3, factors.length + 3)" class="empty-cell">{{ selectedItem ? (loading ? '載入中…' : '查無資料') : '選取計數品管項目後顯示對應資料' }}</td></tr>
            </tbody>
          </table>
        </div>
        <div class="grid-footer">資料總筆數：{{ records.length }} <span v-if="activeRecord">　序號：{{ activeRecord.qcAttSpSerial }}</span></div>
      </div>
      <aside class="action-column" aria-label="資料操作">
        <button class="win-button primary-button" type="button" :disabled="!selectedItem || loading || saving" @click="openEditor('add')">新增(A)</button>
        <button class="win-button" type="button" :disabled="!activeRecord || saving" @click="openEditor('edit')">修改(E)</button>
        <button class="win-button" type="button" :disabled="!activeRecord || saving" @click="openEditor('copy')">複製(C)</button>
        <button class="win-button" type="button" :disabled="!selectedSerials.length || saving" @click="deleteSelected">刪除(D)</button>
        <button class="win-button" type="button" :disabled="!selectedSerials.length || saving" @click="approveSelected">核准</button>
        <span class="action-spacer"></span>
        <button class="win-button close-button" type="button" @click="closePage">關閉(X)</button>
      </aside>
    </section>

    <section class="grid-panel detail-panel">
      <div class="tab-strip"><span class="selected-tab">Detail</span><span v-if="activeRecord" class="tab-context">{{ activeRecord.qcAttSpSerial }}</span></div>
      <div class="grid-scroll detail-scroll">
        <table class="win-grid">
          <thead><tr><th>RetestLot</th><th>PlanNo</th><th>LotRuleNo</th><th>ComRuleNo</th><th>參數筆數</th></tr></thead>
          <tbody>
            <tr v-for="row in detailRows" :key="row.retestLot" :class="{ active: row.retestLot === activeRetestLot }" @click="selectDetail(row)">
              <td>{{ row.retestLotLabel }}</td><td>{{ row.planNo }}</td><td>{{ row.lotRuleNo }}</td><td>{{ row.comRuleNo }}</td><td>{{ parametersForRetestLot(row.retestLot).length }}</td>
            </tr>
            <tr v-if="!details.length"><td colspan="5" class="empty-cell">{{ activeRecord ? '此對應資料沒有明細' : '選取一筆對應資料以檢視明細' }}</td></tr>
          </tbody>
        </table>
      </div>
      <div v-if="activeRetestLot !== null" class="parameter-summary">
        <strong>RetestLot {{ activeRetestLot === 0 ? 'First' : activeRetestLot }} 參數</strong>
        <span v-for="row in parametersForRetestLot(activeRetestLot)" :key="row.parameterNo" class="parameter-chip">{{ row.parameterNo }} = {{ row.parameterValue }}</span>
      </div>
    </section>

    <div v-if="lookupOpen" class="modal-backdrop" @click.self="lookupOpen = false">
      <section class="modal-dialog lookup-dialog" role="dialog" aria-modal="true" aria-labelledby="lookup-title">
        <header class="modal-heading"><h2 id="lookup-title">已核准計數品管項目查詢</h2><button class="icon-close" type="button" @click="lookupOpen = false">×</button></header>
        <div class="lookup-filter"><label for="lookup-qc-filter">項目編號 / 名稱</label><input id="lookup-qc-filter" v-model="lookupFilter" class="kd-input" autofocus></div>
        <div class="grid-scroll lookup-grid"><table class="win-grid">
          <thead><tr><th>QCItemNo</th><th>QCItemName</th><th>RuleNo</th><th>ComponentCollect</th><th>建立人</th><th>建立日期</th></tr></thead>
          <tbody><tr v-for="item in visibleItems" :key="item.qcItemNo" :class="{ active: lookupSelectedItemNo === item.qcItemNo }" class="selectable-row" @click="selectLookupItem(item)" @dblclick="chooseItem(item)">
            <td>{{ item.qcItemNo }}</td><td>{{ item.qcItemName }}</td><td>{{ item.ruleNo }}</td><td>{{ item.componentCollect }}</td><td>{{ item.creator }}</td><td>{{ formatDate(item.createDate) }}</td>
          </tr><tr v-if="!visibleItems.length"><td colspan="6" class="empty-cell">查無已核准項目</td></tr></tbody>
        </table></div>
        <footer class="modal-footer"><span>雙擊資料列或選取資料後按確定</span><div><button class="win-button primary-button" type="button" :disabled="!lookupSelectedItemNo" @click="chooseItem(qcItems.find(item => item.qcItemNo === lookupSelectedItemNo)!)">確定</button><button class="win-button" type="button" @click="lookupOpen = false">關閉</button></div></footer>
      </section>
    </div>

    <div v-if="editorOpen" class="modal-backdrop editor-backdrop" @click.self="closeEditor">
      <section class="modal-dialog editor-dialog" role="dialog" aria-modal="true" aria-labelledby="editor-title">
        <header class="modal-heading"><div><h2 id="editor-title">{{ editorTitle }}</h2><span>FunctionKey QC203　·　{{ selectedItem?.qcItemNo }}</span></div><button class="icon-close" type="button" @click="closeEditor">×</button></header>
        <div v-if="editorError" class="message error-message editor-message" role="alert">{{ editorError }}</div>
        <div class="editor-meta">
          <label>QCItemNo<input class="kd-input" readonly :value="selectedItem?.qcItemNo"></label>
          <label>QCItemName<input class="kd-input" readonly :value="selectedItem?.qcItemName"></label>
          <label>RuleNo<input class="kd-input" readonly :value="selectedItem?.ruleNo"></label>
          <label>ComponentCollect<input class="kd-input" readonly :value="selectedItem?.componentCollect"></label>
        </div>
        <div class="editor-content">
          <section class="grid-panel factor-panel">
            <div class="panel-heading"><strong>線上要因</strong><span>{{ editorMode === 'edit' ? '修改時要因值唯讀' : '必須逐項設定；不限條件請使用 *' }}</span></div>
            <div class="grid-scroll factor-scroll"><table class="win-grid factor-grid">
              <thead><tr><th>FactorNo</th><th>FactorName</th><th>ParameterType</th><th>FactorValue</th></tr></thead>
              <tbody><tr v-for="factor in factors" :key="factor.factorNo">
                <td>{{ factor.factorNo }}</td><td>{{ factor.factorName }}</td><td>{{ factor.parameterType }}</td>
                <td v-if="editorMode === 'edit'">{{ draftFactorValues[factor.factorNo] }}</td>
                <td v-else-if="factor.parameterType === 2"><input class="kd-input inline-factor" :value="draftFactorValues[factor.factorNo]" @input="changeFactorValue(factor.factorNo, ($event.target as HTMLInputElement).value.toUpperCase())"></td>
                <td v-else><select class="kd-input inline-factor" :value="draftFactorValues[factor.factorNo]" @change="changeFactorValue(factor.factorNo, ($event.target as HTMLSelectElement).value)">
                  <option value="">請選擇</option><option v-for="value in [...new Set(['*', ...factor.options, ...(draftFactorValues[factor.factorNo] ? [draftFactorValues[factor.factorNo]] : [])])]" :key="value" :value="value">{{ value === '*' ? '＊ 不限' : value }}</option>
                </select></td>
              </tr><tr v-if="!factors.length"><td colspan="4" class="empty-cell">沒有 FactorOnline=Y 的要因</td></tr></tbody>
            </table></div>
          </section>
          <section class="grid-panel editor-detail-panel">
            <div class="panel-heading"><strong>判別法則明細</strong><span>RetestLot、PlanNo、LotRuleNo、ComRuleNo</span></div>
            <div class="grid-scroll draft-detail-scroll"><table class="win-grid">
              <thead><tr><th>RetestLot</th><th>PlanNo</th><th>LotRuleNo</th><th>ComRuleNo</th><th>參數</th></tr></thead>
              <tbody><tr v-for="row in currentDraftDetails" :key="row.retestLot" :class="{ active: row.retestLot === selectedDraftRetestLot }" @click="selectedDraftRetestLot = row.retestLot">
                <td>{{ row.retestLotLabel }}</td><td>{{ row.planNo }}</td><td>{{ row.lotRuleNo }}</td><td>{{ row.comRuleNo }}</td><td>{{ draftParameters.filter(parameter => parameter.retestLot === row.retestLot).length }}</td>
              </tr><tr v-if="!draftDetails.length"><td colspan="5" class="empty-cell">尚未新增明細</td></tr></tbody>
            </table></div>
            <div class="detail-action-row">
              <button class="win-button" type="button" :disabled="editorReadOnly" @click="openDetailEditor('add')">新增明細(A)</button>
              <button class="win-button" type="button" :disabled="editorReadOnly || selectedDraftRetestLot === null" @click="openDetailEditor('edit')">修改明細(E)</button>
              <button class="win-button" type="button" :disabled="editorReadOnly || selectedDraftRetestLot === null" @click="deleteDraftDetail">刪除明細(D)</button>
            </div>
            <div v-if="detailMode" class="detail-editor">
              <div class="detail-editor-heading">{{ detailMode === 'add' ? '新增判別法則明細' : '修改判別法則明細' }}</div>
              <div class="detail-form-grid">
                <fieldset class="retest-field"><legend>RetestLot</legend>
                  <label><input type="radio" :checked="detailForm.retestLot === 0" :disabled="detailMode === 'edit'" @change="setRetestType(false)"> First lot</label>
                  <label><input type="radio" :checked="detailForm.retestLot !== 0" :disabled="detailMode === 'edit'" @change="setRetestType(true)"> Retest lot</label>
                  <input v-if="detailForm.retestLot !== 0" v-model.number="detailForm.retestLot" class="kd-input retest-number" type="number" min="1" max="99" :disabled="detailMode === 'edit'" @input="editorDirty = true">
                </fieldset>
                <label>抽樣計劃
                  <select v-model="detailForm.planNo" class="kd-input" :disabled="editorReadOnly" @change="editorDirty = true">
                    <option value="">請選擇</option><option v-for="plan in eligiblePlanOptions" :key="plan.planNo" :value="plan.planNo">{{ plan.planNo }}　{{ plan.planName }}</option>
                  </select>
                </label>
                <label>LotRuleNo
                  <select v-model="detailForm.lotRuleNo" class="kd-input" :disabled="editorReadOnly" @change="editorDirty = true">
                    <option value="">（不設定）</option><option v-for="rule in eligibleLotRules" :key="rule.ruleNo" :value="rule.ruleNo">{{ rule.ruleNo }}　{{ rule.ruleName }}</option>
                  </select>
                </label>
                <label v-if="selectedItem?.componentCollect !== 0">ComRuleNo
                  <select v-model="detailForm.comRuleNo" class="kd-input" :disabled="editorReadOnly" @change="editorDirty = true">
                    <option value="">（不設定）</option><option v-for="rule in eligibleComponentRules" :key="rule.ruleNo" :value="rule.ruleNo">{{ rule.ruleNo }}　{{ rule.ruleName }}</option>
                  </select>
                </label>
              </div>
              <div class="detail-parameter-heading"><strong>抽樣參數</strong><button class="win-button" type="button" :disabled="editorReadOnly" @click="openParameterDialog">抽樣參數設定…</button></div>
              <div class="parameter-inline-list">
                <span v-for="parameter in detailParameters" :key="parameter.parameterNo" class="parameter-chip">{{ parameter.parameterNo }} = {{ parameter.parameterValue }}</span>
                <span v-if="!detailParameters.length" class="muted">尚未設定；每個 RetestLot 至少需要一筆</span>
              </div>
              <div class="detail-editor-actions"><button class="win-button primary-button" type="button" :disabled="editorReadOnly" @click="saveDetailEditor">完成</button><button class="win-button" type="button" @click="cancelDetailEditor">取消</button></div>
            </div>
          </section>
        </div>
        <footer class="modal-footer editor-footer"><span>{{ editorReadOnly ? '此狀態依 WinForms Issue Control 設為唯讀。' : '新增/複製會建立未審核資料；修改保留原狀態。' }}</span><div><button class="win-button primary-button" type="button" :disabled="saving || editorReadOnly" @click="saveEditor(true)">確定</button><button class="win-button" type="button" :disabled="saving" @click="closeEditor">關閉</button></div></footer>
      </section>
    </div>

    <div v-if="parameterDialogOpen" class="modal-backdrop nested-backdrop" @click.self="parameterDialogOpen = false">
      <section class="modal-dialog parameter-dialog" role="dialog" aria-modal="true" aria-labelledby="parameter-title">
        <header class="modal-heading"><div><h2 id="parameter-title">抽樣計劃參數設定</h2><span>PlanNo：{{ detailForm.planNo }}　·　RetestLot：{{ detailForm.retestLot === 0 ? 'First' : detailForm.retestLot }}</span></div><button class="icon-close" type="button" @click="parameterDialogOpen = false">×</button></header>
        <div class="parameter-toolbar"><button class="win-button" type="button" @click="parameterDraft = clone((support.planParameters[detailForm.planNo] ?? []).map(row => ({ retestLot: detailForm.retestLot, ...row })))">帶入抽樣計劃預設參數</button><span>參數由所選 PlanNo 載入，可修改或增刪。</span></div>
        <div class="parameter-add-row"><label>ParameterNo<input v-model="parameterNoInput" class="kd-input"></label><label>ParameterValue<input v-model="parameterValueInput" class="kd-input"></label><button class="win-button" type="button" @click="addParameter">新增參數</button></div>
        <div class="grid-scroll parameter-grid-scroll"><table class="win-grid">
          <thead><tr><th>ParameterNo</th><th>ParameterValue</th><th>操作</th></tr></thead>
          <tbody><tr v-for="row in parameterDraft" :key="row.parameterNo">
            <td>{{ row.parameterNo }}</td><td><input v-model="row.parameterValue" class="kd-input inline-factor"></td><td><button class="link-button" type="button" @click="removeParameter(row.parameterNo)">刪除</button></td>
          </tr><tr v-if="!parameterDraft.length"><td colspan="3" class="empty-cell">尚未載入參數</td></tr></tbody>
        </table></div>
        <div v-if="editorError" class="message error-message editor-message">{{ editorError }}</div>
        <footer class="modal-footer"><span>ParameterNo 在同一 RetestLot 不可重複。</span><div><button class="win-button primary-button" type="button" @click="applyParameterDraft">確定</button><button class="win-button" type="button" @click="parameterDialogOpen = false">取消</button></div></footer>
      </section>
    </div>
  </main>
</template>

<style scoped>
.rule-map-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:8px;padding:12px 14px 14px;background:#f8f6f0;color:#262626;font-family:Arial,"Microsoft JhengHei",sans-serif;font-size:13px}
.page-heading{display:flex;align-items:center;justify-content:space-between;min-height:42px}.eyebrow{font-size:11px;color:#777;letter-spacing:.04em}.page-heading h1{margin:2px 0 0;font-size:21px;font-weight:600}
.query-strip{display:flex;align-items:center;gap:8px;flex-wrap:wrap;padding:8px 10px;background:#fff;border:1px solid #c9c5bb}.issue-states{display:flex;align-items:center;gap:12px;margin-right:7px}.issue-states label{white-space:nowrap}.issue-states input{accent-color:#ff9f1c}.query-label{font-weight:600}.kd-input{height:30px;min-width:0;padding:4px 7px;border:1px solid #9d9d9d;border-radius:2px;background:#fff0b3;color:#262626;font:inherit}.kd-input:read-only{background:#eee}.item-no{width:160px}.item-name{width:min(300px,25vw);flex:1}
.win-button{min-height:30px;padding:4px 12px;border:1px solid #b9b9b9;border-radius:3px;background:linear-gradient(#fff,#f0f0f0);color:#222;font:inherit;white-space:nowrap;cursor:pointer}.win-button:hover:not(:disabled){border-color:#e6931a;background:#fff7e7}.win-button:disabled{color:#999;cursor:not-allowed;opacity:.7}.primary-button{background:#ffad36;border-color:#f1a02c;color:#fff;font-weight:600}.primary-button:hover:not(:disabled){background:#f39a1d}.close-button{color:#333}.lookup-button{height:30px;width:34px;border:1px solid #b8b8b8;border-radius:3px;background:#f8f8f8;font-size:20px;line-height:20px;cursor:pointer}
.message{padding:7px 10px;border:1px solid;border-radius:3px}.error-message{border-color:#d99a8e;background:#fff0ed;color:#8f2d1c}.notice-message{border-color:#c8d8bd;background:#f2f8ed;color:#466439}
.basis-layout{flex:1 1 48%;min-height:190px;display:grid;grid-template-columns:minmax(0,1fr) 104px;gap:8px}.grid-panel{min-width:0;min-height:0;display:flex;flex-direction:column;border:1px solid #c7c3ba;background:#fff}.basis-panel{padding:0 7px 4px}.panel-heading{display:flex;justify-content:space-between;align-items:center;gap:10px;min-height:32px;padding:5px 4px;color:#333}.panel-heading>span{color:#777;font-size:12px}.grid-scroll{min-width:0;overflow:auto}.basis-panel .grid-scroll{flex:1}
.win-grid{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}.win-grid th{position:sticky;top:0;z-index:1;padding:6px 8px;border:1px solid #fff;background:#3d9ee3;color:#fff;text-align:left;font-weight:600}.win-grid td{height:28px;padding:4px 8px;border:1px solid #c9c9c9;background:#fff}.win-grid tbody tr:nth-child(even) td{background:#fbfbfb}.win-grid tbody tr.active td{background:#ffe8a0}.win-grid tbody tr.selectable-row:hover td,.win-grid tbody tr:hover td{background:#fff0c7}.win-grid .empty-cell{height:62px;text-align:center;color:#777;background:#fff!important}.select-column{width:48px;text-align:center!important}.select-column input{accent-color:#ef9f23}
.grid-footer{min-height:24px;padding:5px 2px;color:#777;font-size:12px}.action-column{display:flex;flex-direction:column;gap:8px;padding:0 0 4px}.action-column .win-button{width:100%;min-height:34px}.action-spacer{flex:1}
.detail-panel{flex:0 1 34%;min-height:130px;padding:0 7px 6px}.tab-strip{display:flex;align-items:end;min-height:31px;border-bottom:1px solid #c5c0b6}.selected-tab{padding:6px 15px 5px;border:1px solid #c5c0b6;border-bottom:1px solid #fff;background:#fff}.tab-context{margin:0 0 5px 12px;color:#777}.detail-scroll{flex:1}.parameter-summary{display:flex;align-items:center;gap:7px;flex-wrap:wrap;padding:7px 2px 2px;border-top:1px solid #e1ded6}.parameter-chip{display:inline-block;padding:3px 7px;border:1px solid #ddd2b6;border-radius:2px;background:#fff7df;font-size:12px}.muted{color:#888}
.modal-backdrop{position:fixed;inset:0;z-index:3000;display:flex;align-items:center;justify-content:center;padding:20px;background:rgba(30,30,30,.38)}.modal-dialog{display:flex;flex-direction:column;max-height:92vh;border:1px solid #a8a49b;background:#f8f6f0;box-shadow:0 12px 36px rgba(0,0,0,.25)}.lookup-dialog{width:min(1000px,96vw);height:min(620px,90vh)}.modal-heading{display:flex;align-items:center;justify-content:space-between;gap:12px;padding:10px 14px;border-bottom:1px solid #c7c3ba;background:#f4f1e9}.modal-heading h2{margin:0;font-size:18px;font-weight:600}.modal-heading span{display:block;margin-top:4px;color:#777;font-size:12px}.icon-close{width:28px;height:28px;border:1px solid #c8c4bb;background:#fff;font-size:20px;cursor:pointer}.lookup-filter{display:flex;align-items:center;gap:10px;padding:10px 14px}.lookup-filter .kd-input{width:320px}.lookup-grid{flex:1;margin:0 14px}.modal-footer{display:flex;align-items:center;justify-content:space-between;gap:12px;padding:10px 14px;border-top:1px solid #c7c3ba;color:#777;font-size:12px}.modal-footer>div{display:flex;gap:8px}
.editor-backdrop{z-index:3010}.editor-dialog{width:min(1200px,97vw);height:min(880px,94vh)}.editor-message{margin:8px 12px 0}.editor-meta{display:grid;grid-template-columns:repeat(4,minmax(140px,1fr));gap:9px;padding:10px 12px;border-bottom:1px solid #d3cfc6}.editor-meta label,.parameter-add-row label{display:flex;flex-direction:column;gap:4px;color:#555;font-size:12px}.editor-meta .kd-input{width:100%}.editor-content{flex:1;min-height:0;display:grid;grid-template-columns:minmax(340px,.82fr) minmax(500px,1.18fr);gap:10px;padding:10px 12px}.factor-panel,.editor-detail-panel{overflow:hidden}.factor-scroll{flex:1}.factor-grid th:nth-child(1){width:110px}.factor-grid th:nth-child(3){width:100px}.factor-grid th:nth-child(4){width:220px}.inline-factor{width:100%;height:27px;padding:3px 5px}.draft-detail-scroll{height:150px;flex:none}.detail-action-row{display:flex;gap:6px;padding:7px 8px;border-top:1px solid #ddd}.detail-editor{flex:1;min-height:0;overflow:auto;margin:0 8px 8px;border:1px solid #d0cbc0;background:#faf9f5}.detail-editor-heading{padding:7px 9px;border-bottom:1px solid #d7d2c8;font-weight:600}.detail-form-grid{display:grid;grid-template-columns:1fr 1.2fr;gap:8px;padding:8px}.detail-form-grid>label{display:flex;flex-direction:column;gap:4px;color:#555;font-size:12px}.detail-form-grid .kd-input{width:100%}.retest-field{display:flex;align-items:center;gap:9px;grid-column:1 / -1;border:1px solid #d4d0c6}.retest-field legend{padding:0 4px;color:#555}.retest-number{width:78px}.detail-parameter-heading{display:flex;align-items:center;justify-content:space-between;padding:6px 8px;border-top:1px solid #ddd}.parameter-inline-list{display:flex;gap:6px;flex-wrap:wrap;min-height:34px;padding:4px 8px}.detail-editor-actions{display:flex;justify-content:flex-end;gap:7px;padding:7px 8px;border-top:1px solid #ddd}.editor-footer{min-height:50px}
.nested-backdrop{z-index:3020;background:rgba(30,30,30,.48)}.parameter-dialog{width:min(720px,95vw);height:min(570px,88vh)}.parameter-toolbar{display:flex;align-items:center;gap:10px;padding:9px 12px;color:#777;font-size:12px}.parameter-add-row{display:grid;grid-template-columns:1fr 1fr auto;align-items:end;gap:8px;padding:8px 12px;border-bottom:1px solid #d5d0c6}.parameter-add-row .kd-input{width:100%}.parameter-grid-scroll{flex:1;margin:0 12px}.link-button{border:0;background:transparent;color:#a45d00;text-decoration:underline;cursor:pointer}
@media(max-width:900px){.rule-map-page{padding:8px}.basis-layout{grid-template-columns:minmax(0,1fr) 88px}.editor-meta{grid-template-columns:repeat(2,minmax(130px,1fr))}.editor-content{grid-template-columns:1fr;overflow:auto}.factor-panel{min-height:190px}.editor-detail-panel{min-height:350px}.issue-states{gap:6px}.query-strip{align-items:flex-start}.query-label{align-self:center}}
@media(max-width:620px){.page-heading h1{font-size:17px}.issue-states{width:100%;justify-content:space-between}.item-no{width:calc(100% - 60px)}.item-name{width:100%;flex:none}.basis-layout{grid-template-columns:minmax(0,1fr) 76px;gap:4px}.action-column{gap:5px}.action-column .win-button{padding:4px 5px;font-size:11px}.editor-meta{grid-template-columns:1fr 1fr}.detail-form-grid{grid-template-columns:1fr}.retest-field{grid-column:auto}.parameter-add-row{grid-template-columns:1fr}.modal-backdrop{padding:8px}}
</style>
