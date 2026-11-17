<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type IssueState = 0 | 1 | 2 | -1
type EditorMode = 'add' | 'edit' | 'copy'
type RelationMode = 'parameters' | 'factors' | 'reasons' | 'operations' | 'samplePlanChanges' | 'simulation'
interface QcItem {
  qcItemNo: string; qcItemName: string; lotIsValid: number; componentCollect: number; planNo: string; ruleNo: string
  formName: string; description: string; limitLotNum: number; isSampleSizeInteger: number; qcClass: string; mClassNo: string
  issueState: IssueState; dataStamp: number; creator: string; createDate: string; reviser: string; reviseDate: string; executionFile: string
}
interface ParameterRow { parameterNo: string; parameterValue: string }
interface FactorRow { factorNo: string; factorOnline: string; factorSpec: string; opNo: string; tableName: string; fieldName: string; factorShortRun: string }
interface ReasonRow { reasonNo: string; reasonName: string; reasonType: string; reasonLevel: string; issueState: number }
interface ValueOption { value: string; label: string }
interface ReasonMapRow { qcItemNo: string; reasonNo: string; reasonName: string; creator: string; createDate: string; reviser: string; reviseDate: string }
interface SamplePlanChangeRow { oldSamplePlan: string; accLotQty: number; rejLotQty: number; newSamplePlan: string; factorNo: string; lotRange: number; aqlChkForSPlanChg: number; qcItemType: number }
interface OperationRow { opNo: string; opName: string; inline: number; executeOrder: number; qcItemType: number; inspTypeFai: boolean; inspTypeIpqc: boolean; inspTypePqc: boolean; reviser: string; reviseDate: string }
interface MockStore {
  items: QcItem[]; parameters: Record<string, ParameterRow[]>; factors: Record<string, FactorRow[]>
  reasons: Record<string, ReasonMapRow[]>; samplePlanChanges: Record<string, SamplePlanChangeRow[]>; operations: Record<string, OperationRow[]>
}
interface QcForm {
  qcItemNo: string; qcItemName: string; lotIsValid: number | null; componentCollect: number | null; planNo: string; ruleNo: string
  formName: string; description: string; limitLotNum: string; isSampleSizeInteger: number; qcClass: string; mClassNo: string
}
type ListPayload<T> = T[] | { items?: T[]; rows?: T[]; data?: T[] }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const basePath = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const apiRoot = 'api/qc/attribute-qc-items'
const mockKey = 'ymes-dev-mock:qc-attribute-item:v1'
const items = ref<QcItem[]>([])
const selectedNos = ref<string[]>([])
const activeNo = ref('')
const issueState = ref<IssueState>(0)
const searchText = ref('')
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const notice = ref('')
const editorOpen = ref(false)
const editorMode = ref<EditorMode>('add')
const editorError = ref('')
const editorOptionsLoaded = ref(false)
const form = ref<QcForm>(blankForm())
const editingDataStamp = ref(0)
const copyFlags = ref({ parameters: true, factors: true, reasons: true, samplePlanChanges: true })
const planOptions = ref<string[]>([])
const ruleOptions = ref<string[]>([])
const mClassOptions = ref<string[]>([])
const interfaceOptions = ref<string[]>([])
const showQcClass = ref(false)
const showLotValid = ref(true)
const showComponentCollect = ref(true)
const relationOpen = ref(false)
const relationMode = ref<RelationMode>('parameters')
const relationTitle = ref('')
const relationError = ref('')
const relationNotice = ref('')
const relationRows = ref<Array<ParameterRow | FactorRow | ReasonMapRow | SamplePlanChangeRow | OperationRow>>([])
const relationCandidates = ref<ReasonRow[]>([])
const operationCandidates = ref<OperationRow[]>([])
const operationCandidateNos = ref<string[]>([])
const selectedOperationNos = ref<string[]>([])
const reasonTypeOptions = ref<ValueOption[]>([])
const relationSelectedNos = ref<string[]>([])
const relationSearch = ref('')
const relationTypeFilter = ref('')
const relationLevelFilter = ref('')
const newParameterNo = ref('')
const newFactorNo = ref('')
const simulationRows = ref<Array<Record<string, string | number>>>([])

function blankForm(): QcForm {
  return { qcItemNo: '', qcItemName: '', lotIsValid: 0, componentCollect: 0, planNo: '', ruleNo: '', formName: '', description: '', limitLotNum: '20', isSampleSizeInteger: 0, qcClass: '', mClassNo: '' }
}
function demoItem(qcItemNo: string, qcItemName: string, issue: IssueState, date: string, options: Partial<QcItem> = {}): QcItem {
  return { qcItemNo, qcItemName, lotIsValid: 1, componentCollect: 0, planNo: 'DEV-PLAN-01', ruleNo: 'DEV-AR-01', formName: 'frmAttributeInspection', description: 'DEV 假資料', limitLotNum: 1, isSampleSizeInteger: 0, qcClass: 'SPC', mClassNo: '', issueState: issue, dataStamp: 0, creator: 'DEV001', createDate: date, reviser: '', reviseDate: '', executionFile: '', ...options }
}
function makeMockStore(): MockStore {
  return {
    items: [
      demoItem('DEV-QCA-001', '外觀抽樣（DEMO）', 0, '2026/11/17 08:00:00'),
      demoItem('DEV-QCA-002', '尺寸缺陷計數（DEMO）', 1, '2026/11/16 13:30:00', { componentCollect: 1, limitLotNum: 5 }),
      demoItem('DEV-QCA-003', '包裝檢查（DEMO）', 2, '2026/11/15 09:10:00', { reviser: 'DEV001', reviseDate: '2026/11/16 10:00:00' }),
      demoItem('DEV-QCA-004', '舊版抽樣項目（DEMO）', -1, '2026/11/01 09:10:00'),
    ],
    parameters: {
      'DEV-QCA-001': [{ parameterNo: 'SampleSize', parameterValue: '32' }, { parameterNo: 'AcRe', parameterValue: '0,1' }],
      'DEV-QCA-002': [{ parameterNo: 'SampleSize', parameterValue: '50' }],
    },
    factors: {
      'DEV-QCA-001': [{ factorNo: 'Appearance', factorOnline: 'Y', factorSpec: 'Y', opNo: 'OP-PACK', tableName: '', fieldName: '', factorShortRun: 'N' }],
      'DEV-QCA-002': [{ factorNo: 'Dimension', factorOnline: 'Y', factorSpec: 'Y', opNo: 'OP-MEASURE', tableName: '', fieldName: '', factorShortRun: 'N' }],
    },
    reasons: {
      'DEV-QCA-001': [{ qcItemNo: 'DEV-QCA-001', reasonNo: 'DEV-RSN-01', reasonName: '外觀破損（DEMO）', creator: 'DEV001', createDate: '2026/11/17 08:00:00', reviser: '', reviseDate: '' }],
      'DEV-QCA-002': [{ qcItemNo: 'DEV-QCA-002', reasonNo: 'DEV-RSN-02', reasonName: '尺寸異常（DEMO）', creator: 'DEV001', createDate: '2026/11/17 08:00:00', reviser: '', reviseDate: '' }],
    },
    samplePlanChanges: {},
    operations: { 'DEV-QCA-001': [{ opNo: 'OP-PACK', opName: '包裝', inline: 1, executeOrder: 0, qcItemType: 0, inspTypeFai: true, inspTypeIpqc: false, inspTypePqc: false, reviser: 'DEV001', reviseDate: '2026/11/17 08:00:00' }] },
  }
}
function readStore(): MockStore {
  try {
    const raw = window.localStorage.getItem(mockKey)
    if (raw) {
      const value = JSON.parse(raw) as MockStore
      if (Array.isArray(value.items)) return value
    }
  } catch { /* start with isolated DEV rows */ }
  const value = makeMockStore()
  saveStore(value)
  return value
}
function saveStore(value: MockStore): void { window.localStorage.setItem(mockKey, JSON.stringify(value)) }
function listOf<T>(payload: ListPayload<T>): T[] {
  if (Array.isArray(payload)) return payload
  return payload.items ?? payload.rows ?? payload.data ?? []
}
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body && !(init.body instanceof FormData) && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init, headers })
  if (response.status === 401) { window.location.assign(basePath + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; title?: string; message?: string } | null
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 呼叫失敗（${response.status}）。`)
  }
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}
function normalizeItem(value: Record<string, unknown>): QcItem {
  const state = Number(value.issueState ?? value.ISSUESTATE ?? 0)
  return {
    qcItemNo: String(value.qcItemNo ?? value.QCITEMNO ?? ''), qcItemName: String(value.qcItemName ?? value.QCITEMNAME ?? ''),
    lotIsValid: Number(value.lotIsValid ?? value.LOTISVALID ?? 0), componentCollect: Number(value.componentCollect ?? value.COMPONENTCOLLECT ?? 0),
    planNo: String(value.planNo ?? value.PLANNO ?? ''), ruleNo: String(value.ruleNo ?? value.RULENO ?? ''), formName: String(value.formName ?? value.FORMNAME ?? ''),
    description: String(value.description ?? value.DESCRIPTION ?? ''), limitLotNum: Number(value.limitLotNum ?? value.LIMITLOTNUM ?? 0),
    isSampleSizeInteger: Number(value.isSampleSizeInteger ?? value.ISSAMPLESIZEINTEGER ?? 0), qcClass: String(value.qcClass ?? value.QCCLASS ?? ''),
    mClassNo: String(value.mClassNo ?? value.MCLASSNO ?? ''), issueState: (state === -1 || state === 1 || state === 2 ? state : 0) as IssueState,
    dataStamp: Number(value.dataStamp ?? value.DATASTAMP ?? 0), creator: String(value.creator ?? value.CREATOR ?? ''), createDate: String(value.createDate ?? value.CREATEDATE ?? ''),
    reviser: String(value.reviser ?? value.REVISER ?? ''), reviseDate: String(value.reviseDate ?? value.REVISEDATE ?? ''), executionFile: String(value.executionFile ?? value.EXECUTIONFILE ?? ''),
  }
}
function stateLabel(value: number): string { return ({ 0: '未簽核', 1: '簽核中', 2: '已簽核', '-1': '不使用' } as Record<string, string>)[String(value)] ?? String(value) }
function valueLabel(value: number): string { return value === 1 ? '是' : value === 0 ? '否' : '' }
function sampleSizeModeLabel(value: number): string { return value === 0 ? '整數' : '小數' }
function displayDate(value: string): string { return value ? value.replace('T', ' ').slice(0, 19) : '' }
const visibleItems = computed(() => items.value.filter(item => item.qcItemNo.toLowerCase().includes(searchText.value.trim().toLowerCase())))
const activeItem = computed(() => items.value.find(item => item.qcItemNo === activeNo.value) ?? null)
const canEditOperations = computed(() => activeItem.value?.issueState === 2)
const selectedItems = computed(() => items.value.filter(item => selectedNos.value.includes(item.qcItemNo)))
const planChoiceList = computed(() => planOptions.value.length ? planOptions.value : mock.value ? ['DEV-PLAN-01', 'DEV-PLAN-02'] : [])
const ruleChoiceList = computed(() => ruleOptions.value.length ? ruleOptions.value : mock.value ? ['DEV-AR-01', 'DEV-AR-02'] : [])
const mClassChoiceList = computed(() => mClassOptions.value.length ? mClassOptions.value : mock.value ? ['DEV-MC-01', 'DEV-MC-02'] : [])
const interfaceChoiceList = computed(() => interfaceOptions.value.length ? interfaceOptions.value : mock.value ? ['frmAttributeInspection', 'frmAppearanceInspection'] : [])
const currentEditorAudit = computed(() => ({
  creator: editorMode.value === 'edit' ? (activeItem.value?.creator ?? '') : (props.currentUserNo || (mock.value ? 'DEV001' : '')),
  createDate: editorMode.value === 'edit' ? displayDate(activeItem.value?.createDate ?? '') : '儲存時建立',
  reviser: editorMode.value === 'edit' ? (props.currentUserNo || (mock.value ? 'DEV001' : '')) : '—',
  reviseDate: editorMode.value === 'edit' ? '儲存時更新' : '—',
}))
const filteredCandidates = computed(() => relationCandidates.value.filter(row => {
  const searchOk = !relationSearch.value || `${row.reasonNo} ${row.reasonName}`.toLowerCase().includes(relationSearch.value.toLowerCase())
  const typeOk = !relationTypeFilter.value || row.reasonType === relationTypeFilter.value
  const levelOk = !relationLevelFilter.value || row.reasonLevel === relationLevelFilter.value
  return searchOk && typeOk && levelOk && !relationSelectedNos.value.includes(row.reasonNo)
}))

async function loadItems(): Promise<void> {
  loading.value = true; errorMessage.value = ''
  try {
    if (mock.value) items.value = readStore().items.filter(row => row.issueState === issueState.value).sort((a, b) => a.qcItemNo.localeCompare(b.qcItemNo))
    else {
      const payload = await request<ListPayload<Record<string, unknown>>>(`${apiRoot}?issueState=${issueState.value}&search=${encodeURIComponent(searchText.value.trim())}`)
      items.value = listOf(payload).map(normalizeItem)
    }
    selectedNos.value = selectedNos.value.filter(no => items.value.some(row => row.qcItemNo === no))
    if (!items.value.some(row => row.qcItemNo === activeNo.value)) activeNo.value = items.value[0]?.qcItemNo ?? ''
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '載入計數品管項目失敗。' }
  finally { loading.value = false }
}
function selectRow(item: QcItem, event: MouseEvent): void {
  activeNo.value = item.qcItemNo
  if (event.ctrlKey || event.metaKey) selectedNos.value = selectedNos.value.includes(item.qcItemNo) ? selectedNos.value.filter(no => no !== item.qcItemNo) : [...selectedNos.value, item.qcItemNo]
  else if (!selectedNos.value.includes(item.qcItemNo)) selectedNos.value = [item.qcItemNo]
}
function toggleSelected(item: QcItem, checked: boolean): void {
  selectedNos.value = checked ? [...new Set([...selectedNos.value, item.qcItemNo])] : selectedNos.value.filter(no => no !== item.qcItemNo)
  activeNo.value = item.qcItemNo
}
function openEditor(mode: EditorMode, item: QcItem | null = null): void {
  errorMessage.value = ''; editorError.value = ''; editorMode.value = mode; editingDataStamp.value = item?.dataStamp ?? 0
  editorOptionsLoaded.value = mock.value
  showQcClass.value = mock.value
  showLotValid.value = mock.value
  showComponentCollect.value = mock.value
  form.value = item ? {
    qcItemNo: mode === 'copy' ? '' : item.qcItemNo, qcItemName: item.qcItemName, lotIsValid: item.lotIsValid, componentCollect: item.componentCollect,
    planNo: item.planNo, ruleNo: item.ruleNo, formName: item.formName, description: item.description, limitLotNum: String(item.limitLotNum),
    isSampleSizeInteger: item.isSampleSizeInteger, qcClass: item.qcClass, mClassNo: item.mClassNo,
  } : blankForm()
  copyFlags.value = { parameters: true, factors: true, reasons: true, samplePlanChanges: true }
  editorOpen.value = true
  if (!mock.value) void loadEditorOptions()
}
async function loadEditorOptions(): Promise<void> {
  try {
    const options = await request<{ samplingPlans?: string[]; attributeRules?: string[]; mClasses?: string[]; interfaces?: string[]; showQcClass?: boolean; showLotValid?: boolean; showComponentCollect?: boolean }>(`${apiRoot}/options`)
    planOptions.value = options.samplingPlans ?? []; ruleOptions.value = options.attributeRules ?? []; mClassOptions.value = options.mClasses ?? []; interfaceOptions.value = options.interfaces ?? []
    showQcClass.value = options.showQcClass ?? false
    showLotValid.value = options.showLotValid ?? true
    showComponentCollect.value = options.showComponentCollect ?? true
    editorOptionsLoaded.value = true
  } catch (error) { editorOptionsLoaded.value = false; editorError.value = error instanceof Error ? error.message : '載入選項失敗。' }
}
function closeEditor(): void { if (!busy.value) editorOpen.value = false }
function validateForm(): string {
  if (!editorOptionsLoaded.value) return '欄位選項尚未載入成功，請先確認 API 連線。'
  if (!form.value.qcItemNo.trim()) return 'QCItemNo 為必填。'
  if (form.value.qcItemNo.length > 25) return 'QCItemNo 長度不可超過 25。'
  if (!form.value.qcItemName.trim()) return 'QCItemName 為必填。'
  if (showLotValid.value && form.value.lotIsValid === null) return '請選擇 Lot Valid Check。'
  if (showComponentCollect.value && form.value.componentCollect === null) return '請選擇 Component Collect。'
  if (!form.value.planNo.trim()) return 'PlanNo 為必填。'
  if (showQcClass.value && !form.value.qcClass.trim()) return 'QCClass 為必填。'
  const lotLimit = Number(form.value.limitLotNum)
  if (!Number.isInteger(lotLimit) || lotLimit <= 0 || lotLimit > 99999) return 'Limit Lot Num 必須為 1 至 99999 的整數。'
  if (form.value.mClassNo && mClassOptions.value.length && !mClassOptions.value.includes(form.value.mClassNo)) return 'MClassNo 必須選擇有效主分類。'
  return ''
}
function buildPayload(): Record<string, unknown> {
  return {
    qcItemNo: form.value.qcItemNo.trim(), qcItemName: form.value.qcItemName.trim(), lotIsValid: form.value.lotIsValid ?? 0,
    componentCollect: form.value.componentCollect ?? 0, planNo: form.value.planNo.trim(), ruleNo: form.value.ruleNo.trim(), formName: form.value.formName,
    description: form.value.description, limitLotNum: Number(form.value.limitLotNum), isSampleSizeInteger: form.value.isSampleSizeInteger,
    qcClass: form.value.qcClass, mClassNo: form.value.mClassNo, dataStamp: editingDataStamp.value,
  }
}
async function saveItem(closeAfterSave = true): Promise<void> {
  editorError.value = validateForm(); if (editorError.value) return
  busy.value = true
  try {
    const payload = buildPayload()
    if (mock.value) {
      const store = readStore(); const now = new Date().toLocaleString('zh-TW', { hour12: false }).replaceAll('/', '/')
      if (editorMode.value === 'edit') {
        const index = store.items.findIndex(row => row.qcItemNo === form.value.qcItemNo)
        if (index < 0) throw new Error('資料已不存在，請重新查詢。')
        store.items[index] = { ...store.items[index], ...payload, qcItemNo: form.value.qcItemNo.trim(), limitLotNum: Number(form.value.limitLotNum), issueState: store.items[index].issueState, dataStamp: store.items[index].dataStamp + 1, reviser: props.currentUserNo || 'DEV001', reviseDate: now } as QcItem
      } else {
        if (store.items.some(row => row.qcItemNo.toUpperCase() === form.value.qcItemNo.trim().toUpperCase())) throw new Error('QCItemNo 已存在。')
        const newItem = { ...demoItem(form.value.qcItemNo.trim(), form.value.qcItemName.trim(), 0, now), ...payload, qcItemNo: form.value.qcItemNo.trim(), issueState: 0 as IssueState, dataStamp: 0, creator: props.currentUserNo || 'DEV001', createDate: now } as QcItem
        store.items.push(newItem)
        if (editorMode.value === 'copy' && activeItem.value) {
          const sourceNo = activeItem.value.qcItemNo
          if (copyFlags.value.parameters) store.parameters[newItem.qcItemNo] = structuredClone(store.parameters[sourceNo] ?? [])
          if (copyFlags.value.factors) store.factors[newItem.qcItemNo] = structuredClone(store.factors[sourceNo] ?? [])
          if (copyFlags.value.reasons) store.reasons[newItem.qcItemNo] = structuredClone(store.reasons[sourceNo] ?? []).map(row => ({ ...row, qcItemNo: newItem.qcItemNo }))
          if (copyFlags.value.samplePlanChanges) store.samplePlanChanges[newItem.qcItemNo] = structuredClone(store.samplePlanChanges[sourceNo] ?? [])
        }
      }
      saveStore(store)
    } else if (editorMode.value === 'edit') {
      await request(`${apiRoot}/${encodeURIComponent(form.value.qcItemNo)}`, { method: 'PUT', body: JSON.stringify(payload) })
    } else if (editorMode.value === 'copy') {
      await request(`${apiRoot}/${encodeURIComponent(activeItem.value?.qcItemNo ?? '')}/copy`, { method: 'POST', body: JSON.stringify({ ...payload, copyParameters: copyFlags.value.parameters, copyFactors: copyFlags.value.factors, copyReasons: copyFlags.value.reasons, copySamplePlanChanges: copyFlags.value.samplePlanChanges }) })
    } else {
      await request(apiRoot, { method: 'POST', body: JSON.stringify(payload) })
    }
    const savedNo = form.value.qcItemNo.trim(); if (closeAfterSave) editorOpen.value = false; activeNo.value = savedNo
    notice.value = mock.value ? 'DEV 假資料已更新。' : '計數品管項目已儲存。'
    await loadItems()
    if (!closeAfterSave) editingDataStamp.value = items.value.find(row => row.qcItemNo === savedNo)?.dataStamp ?? editingDataStamp.value
  } catch (error) { editorError.value = error instanceof Error ? error.message : '儲存失敗。' }
  finally { busy.value = false }
}
async function deleteSelected(): Promise<void> {
  if (!selectedItems.value.length) { errorMessage.value = '請先選取資料列。'; return }
  if (selectedItems.value.some(row => row.issueState === 1)) { errorMessage.value = '簽核中資料不可刪除。'; return }
  const isActive = selectedItems.value.some(row => row.issueState === 2)
  if (!window.confirm(isActive ? '確定停用所選已簽核項目，並刪除其他可刪資料？' : '確定刪除所選資料？')) return
  busy.value = true; errorMessage.value = ''
  try {
    for (const row of selectedItems.value) {
      if (mock.value) {
        const store = readStore()
        if (row.issueState === 2) {
          const index = store.items.findIndex(item => item.qcItemNo === row.qcItemNo)
          if (index >= 0) store.items[index] = { ...store.items[index], issueState: -1, dataStamp: row.dataStamp + 1 }
        } else {
          store.items = store.items.filter(item => item.qcItemNo !== row.qcItemNo)
          delete store.parameters[row.qcItemNo]; delete store.factors[row.qcItemNo]; delete store.reasons[row.qcItemNo]; delete store.samplePlanChanges[row.qcItemNo]
        }
        saveStore(store)
      } else {
        await request(`${apiRoot}/${encodeURIComponent(row.qcItemNo)}`, { method: 'DELETE', body: JSON.stringify({ issueState: row.issueState, dataStamp: row.dataStamp }) })
      }
    }
    selectedNos.value = []; notice.value = isActive ? '已停用已簽核項目。' : '資料已刪除。'; await loadItems()
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '刪除失敗；已完成筆數請重新查詢確認。' }
  finally { busy.value = false }
}
async function approveSelected(): Promise<void> {
  if (!selectedItems.value.length) { errorMessage.value = '請先選取資料列。'; return }
  if (selectedItems.value.some(row => row.issueState !== 0)) { errorMessage.value = '只有未簽核資料可以核准。'; return }
  if (!window.confirm(`確定核准 ${selectedItems.value.length} 筆計數品管項目？`)) return
  busy.value = true; errorMessage.value = ''
  try {
    for (const row of selectedItems.value) {
      let factorCount: number; let reasonCount: number
      if (mock.value) { const store = readStore(); factorCount = store.factors[row.qcItemNo]?.length ?? 0; reasonCount = store.reasons[row.qcItemNo]?.length ?? 0 }
      else {
        const [factors, reasons] = await Promise.all([
          request<ListPayload<Record<string, unknown>>>(`${apiRoot}/${encodeURIComponent(row.qcItemNo)}/factors`),
          request<ListPayload<Record<string, unknown>>>(`${apiRoot}/${encodeURIComponent(row.qcItemNo)}/reasons`),
        ])
        factorCount = listOf(factors).length; reasonCount = listOf(reasons).length
      }
      if (!factorCount || !reasonCount) throw new Error(`${row.qcItemNo} 缺少要因或原因設定，不能核准。`)
      if (mock.value) {
        const store = readStore(); const index = store.items.findIndex(item => item.qcItemNo === row.qcItemNo)
        if (index >= 0) store.items[index] = { ...store.items[index], issueState: 2, reviser: props.currentUserNo || 'DEV001', reviseDate: new Date().toLocaleString('zh-TW', { hour12: false }), dataStamp: row.dataStamp + 1 }
        saveStore(store)
      } else await request(`${apiRoot}/${encodeURIComponent(row.qcItemNo)}/approve`, { method: 'POST', body: JSON.stringify({ issueState: row.issueState, dataStamp: row.dataStamp }) })
    }
    selectedNos.value = []; notice.value = mock.value ? 'DEV 假資料已核准。' : '核准完成。'; await loadItems()
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '核准失敗；已完成筆數請重新查詢確認。'; await loadItems() }
  finally { busy.value = false }
}

function openRelation(mode: RelationMode): void {
  if (!activeItem.value) { errorMessage.value = '請先選取一筆 QCItem。'; return }
  if (mode === 'operations' && (activeItem.value.issueState === 0 || activeItem.value.issueState === 1)) { errorMessage.value = '未簽核或簽核中的計數品管項目，不允許修改所屬作業站。'; return }
  relationMode.value = mode; relationError.value = ''; relationNotice.value = ''; relationSearch.value = ''; relationTypeFilter.value = ''; relationLevelFilter.value = ''; relationSelectedNos.value = []; operationCandidateNos.value = []; selectedOperationNos.value = []
  relationTitle.value = ({ parameters: 'Parameter', factors: 'Factor', reasons: 'Reason', operations: 'Op', samplePlanChanges: 'SamplePlanChange', simulation: 'Simulation' } as Record<RelationMode, string>)[mode]
  relationOpen.value = true
  void loadRelation(mode)
}
async function loadRelation(mode: RelationMode): Promise<void> {
  relationError.value = ''
  try {
    if (mode === 'simulation') {
      if (mock.value) simulationRows.value = [{ ParameterNo: 'SampleSize', ParameterValue: '32' }, { ParameterNo: 'AcceptQty', ParameterValue: '0' }, { ParameterNo: 'RejectQty', ParameterValue: '1' }]
      else simulationRows.value = await request<Array<Record<string, string | number>>>(`${apiRoot}/${encodeURIComponent(activeItem.value!.qcItemNo)}/simulate`, { method: 'POST', body: JSON.stringify({ planNo: activeItem.value!.planNo }) })
      return
    }
    if (mock.value) {
      const store = readStore(); const no = activeItem.value!.qcItemNo
      relationRows.value = mode === 'parameters' ? structuredClone(store.parameters[no] ?? [])
        : mode === 'factors' ? structuredClone(store.factors[no] ?? [])
          : mode === 'reasons' ? structuredClone(store.reasons[no] ?? [])
            : mode === 'operations' ? structuredClone(store.operations[no] ?? [])
              : structuredClone(store.samplePlanChanges[no] ?? [])
      if (mode === 'reasons') relationSelectedNos.value = (relationRows.value as ReasonMapRow[]).map(row => row.reasonNo)
      if (mode === 'reasons') relationCandidates.value = [
        { reasonNo: 'DEV-RSN-01', reasonName: '外觀破損（DEMO）', reasonType: '1', reasonLevel: '1', issueState: 2 },
        { reasonNo: 'DEV-RSN-02', reasonName: '尺寸異常（DEMO）', reasonType: '1', reasonLevel: '2', issueState: 2 },
        { reasonNo: 'DEV-RSN-03', reasonName: '報廢（DEMO）', reasonType: '0', reasonLevel: '1', issueState: 2 },
      ]
      if (mode === 'reasons') reasonTypeOptions.value = [{ value: '0', label: 'Scrap' }, { value: '1', label: 'Defect' }, { value: '5', label: 'Adjust' }, { value: '6', label: 'EQP' }]
      if (mode === 'operations') {
        const choices = [{ opNo: 'OP-PACK', opName: '包裝' }, { opNo: 'OP-MEASURE', opName: '量測' }, { opNo: 'OP-ASSY', opName: '組裝' }, { opNo: 'OP-FINAL', opName: '最終檢查' }]
        operationCandidates.value = choices.map(row => normalizeOperation(row)).filter(row => !(relationRows.value as OperationRow[]).some(current => current.opNo === row.opNo))
      }
    } else if (mode === 'operations') {
      const no = encodeURIComponent(activeItem.value!.qcItemNo)
      const [assigned, candidates] = await Promise.all([
        request<ListPayload<Record<string, unknown>>>(`${apiRoot}/${no}/operations`),
        request<ListPayload<Record<string, unknown>>>(`${apiRoot}/options/operations?qcType=0&lotIsValid=${activeItem.value!.lotIsValid}`),
      ])
      relationRows.value = listOf(assigned).map(normalizeOperation)
      operationCandidates.value = listOf(candidates).map(normalizeOperation).filter(row => !(relationRows.value as OperationRow[]).some(current => current.opNo === row.opNo))
    } else {
      const path = `${apiRoot}/${encodeURIComponent(activeItem.value!.qcItemNo)}/${mode}`
      const payload = await request<ListPayload<Record<string, unknown>>>(path)
      relationRows.value = listOf(payload) as unknown as Array<ParameterRow | FactorRow | ReasonMapRow | SamplePlanChangeRow | OperationRow>
      if (mode === 'reasons') {
        relationSelectedNos.value = relationRows.value.map(row => String((row as ReasonMapRow).reasonNo))
        const candidates = await request<ListPayload<ReasonRow>>(`${apiRoot}/options/reasons?issueState=2`)
        relationCandidates.value = listOf(candidates)
        const types = await request<ListPayload<ValueOption>>(`${apiRoot}/options/reason-types`)
        reasonTypeOptions.value = listOf(types)
      }
    }
  } catch (error) { relationError.value = error instanceof Error ? error.message : `載入 ${relationTitle.value} 失敗。` }
}
function moveReason(reasonNo: string, selected: boolean): void {
  relationSelectedNos.value = selected ? [...new Set([...relationSelectedNos.value, reasonNo])] : relationSelectedNos.value.filter(value => value !== reasonNo)
  if (selected) {
    const reason = relationCandidates.value.find(row => row.reasonNo === reasonNo)
    if (reason && !(relationRows.value as ReasonMapRow[]).some(row => row.reasonNo === reasonNo)) (relationRows.value as ReasonMapRow[]).push({ qcItemNo: activeItem.value!.qcItemNo, reasonNo, reasonName: reason.reasonName, creator: '', createDate: '', reviser: '', reviseDate: '' })
  } else relationRows.value = (relationRows.value as ReasonMapRow[]).filter(row => row.reasonNo !== reasonNo)
}
function normalizeOperation(value: Record<string, unknown>): OperationRow {
  const flag = (key: string) => { const raw = value[key] ?? value[key.toUpperCase()]; return raw === true || raw === 1 || raw === '1' || raw === 'Y' }
  return {
    opNo: String(value.opNo ?? value.OPNO ?? ''), opName: String(value.opName ?? value.OPNAME ?? ''),
    inline: Number(value.inline ?? value.INLINE ?? 0), executeOrder: Number(value.executeOrder ?? value.EXECUTEORDER ?? 0),
    qcItemType: Number(value.qcItemType ?? value.QCITEMTYPE ?? 0), inspTypeFai: flag('inspTypeFai'), inspTypeIpqc: flag('inspTypeIpqc'), inspTypePqc: flag('inspTypePqc'),
    reviser: String(value.reviser ?? value.REVISER ?? ''), reviseDate: String(value.reviseDate ?? value.REVISEDATE ?? ''),
  }
}
function toggleOperationCandidate(opNo: string, selected: boolean): void {
  operationCandidateNos.value = selected ? [...new Set([...operationCandidateNos.value, opNo])] : operationCandidateNos.value.filter(value => value !== opNo)
}
function toggleSelectedOperation(opNo: string, selected: boolean): void {
  selectedOperationNos.value = selected ? [...new Set([...selectedOperationNos.value, opNo])] : selectedOperationNos.value.filter(value => value !== opNo)
}
function moveSelectedOperations(inline: 0 | 1): void {
  if (!canEditOperations.value) return
  if (inline === 1 && activeItem.value?.lotIsValid !== 1) { relationError.value = '只有 LotIsValid=1 的品管項目可以加入 InLine。'; return }
  const moving = operationCandidates.value.filter(row => operationCandidateNos.value.includes(row.opNo))
  relationRows.value.push(...moving.map(row => ({ ...row, inline, executeOrder: 0, qcItemType: 0, inspTypeFai: false, inspTypeIpqc: false, inspTypePqc: false })))
  operationCandidates.value = operationCandidates.value.filter(row => !operationCandidateNos.value.includes(row.opNo))
  operationCandidateNos.value = []; relationError.value = ''
}
function removeSelectedOperations(): void {
  if (!canEditOperations.value) return
  const rows = relationRows.value as OperationRow[]
  operationCandidates.value.push(...rows.filter(row => selectedOperationNos.value.includes(row.opNo)).map(row => ({ ...row, inline: 0 })))
  relationRows.value = rows.filter(row => !selectedOperationNos.value.includes(row.opNo))
  selectedOperationNos.value = []; relationError.value = ''
}
function addParameter(): void { if (!newParameterNo.value.trim()) return; (relationRows.value as ParameterRow[]).push({ parameterNo: newParameterNo.value.trim(), parameterValue: '' }); newParameterNo.value = '' }
function addFactor(): void { if (!newFactorNo.value.trim()) return; (relationRows.value as FactorRow[]).push({ factorNo: newFactorNo.value.trim(), factorOnline: 'Y', factorSpec: 'Y', opNo: '', tableName: '', fieldName: '', factorShortRun: 'N' }); newFactorNo.value = '' }
function removeRelationRow(index: number): void { relationRows.value.splice(index, 1) }
async function saveRelation(): Promise<void> {
  const item = activeItem.value; if (!item) return
  if (relationMode.value === 'operations') {
    const inlineWithoutTiming = (relationRows.value as OperationRow[]).find(row => row.inline === 1 && !row.inspTypeFai && !row.inspTypeIpqc && !row.inspTypePqc)
    if (inlineWithoutTiming) { relationError.value = `${inlineWithoutTiming.opNo} 為 InLine 作業站，至少需選一種檢驗時機。`; return }
  }
  relationError.value = ''; busy.value = true
  try {
    if (mock.value) {
      const store = readStore()
      if (relationMode.value === 'parameters') store.parameters[item.qcItemNo] = structuredClone(relationRows.value as ParameterRow[])
      else if (relationMode.value === 'factors') store.factors[item.qcItemNo] = structuredClone(relationRows.value as FactorRow[])
      else if (relationMode.value === 'reasons') store.reasons[item.qcItemNo] = structuredClone(relationRows.value as ReasonMapRow[])
      else if (relationMode.value === 'samplePlanChanges') store.samplePlanChanges[item.qcItemNo] = structuredClone(relationRows.value as SamplePlanChangeRow[])
      else if (relationMode.value === 'operations') store.operations[item.qcItemNo] = structuredClone(relationRows.value as OperationRow[])
      saveStore(store)
    } else {
      await request(`${apiRoot}/${encodeURIComponent(item.qcItemNo)}/${relationMode.value}`, { method: 'PUT', body: JSON.stringify({ rows: relationMode.value === 'reasons' ? relationSelectedNos.value.map(reasonNo => ({ reasonNo })) : relationRows.value }) })
    }
    relationNotice.value = mock.value ? 'DEV 假資料已保存。' : '關聯資料已保存。'
    await loadRelation(relationMode.value)
  } catch (error) { relationError.value = error instanceof Error ? error.message : '儲存關聯資料失敗。' }
  finally { busy.value = false }
}
function closeRelation(): void { if (!busy.value) relationOpen.value = false }
function closePage(): void { emit('close') }
onMounted(loadItems)
</script>

<template>
  <main class="attribute-qc-page">
    <header class="page-title"><h1>計數品管項目</h1><span v-if="mock" class="dev-badge">DEV 假資料</span></header>
    <section class="query-bar">
      <label v-for="state in [{value:0,label:'未簽核'},{value:1,label:'簽核中'},{value:2,label:'已簽核'},{value:-1,label:'不使用'}]" :key="state.value" class="state-option"><input v-model="issueState" type="radio" :value="state.value" @change="loadItems"><span>{{ state.label }}</span></label>
      <label class="query-label" for="item-search">QCItemNo</label><input id="item-search" v-model="searchText" class="query-input" @keyup.enter="loadItems"><button class="win-button query-button" type="button" @click="loadItems">查詢</button>
    </section>
    <div class="main-area">
      <section class="grid-panel"><div class="grid-scroll"><table class="main-grid"><thead><tr><th class="check-col"></th><th>QCItemNo</th><th>QCItemName</th><th>LotIsValid</th><th>ComponentCollect</th><th>PlanNo</th><th>IsSampleSizeInteger</th><th>RuleNo</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead><tbody>
        <tr v-for="item in visibleItems" :key="item.qcItemNo" :class="{ selected: activeNo === item.qcItemNo }" @click="selectRow(item, $event)"><td class="check-col"><input type="checkbox" :checked="selectedNos.includes(item.qcItemNo)" @click.stop @change="toggleSelected(item, ($event.target as HTMLInputElement).checked)"></td><td>{{ item.qcItemNo }}</td><td>{{ item.qcItemName }}</td><td>{{ valueLabel(item.lotIsValid) }}</td><td>{{ valueLabel(item.componentCollect) }}</td><td>{{ item.planNo }}</td><td>{{ sampleSizeModeLabel(item.isSampleSizeInteger) }}</td><td>{{ item.ruleNo }}</td><td>{{ item.creator }}</td><td>{{ displayDate(item.createDate) }}</td><td>{{ item.reviser }}</td><td>{{ displayDate(item.reviseDate) }}</td></tr>
        <tr v-if="!visibleItems.length"><td colspan="12" class="empty-row">{{ loading ? '載入中…' : '查無資料' }}</td></tr>
      </tbody></table></div><footer class="grid-footer">資料總筆數 {{ visibleItems.length }}　目前狀態：{{ stateLabel(issueState) }}</footer>
      </section>
      <aside class="side-actions">
        <button class="win-button" type="button" :disabled="busy" @click="openEditor('add')">ADD (&amp;A)</button><button class="win-button" type="button" :disabled="!activeItem || busy" @click="openEditor('edit', activeItem)">EDIT (&amp;E)</button><button class="win-button" type="button" :disabled="!selectedItems.length || busy" @click="deleteSelected">DEL (&amp;D)</button><button class="win-button" type="button" :disabled="!activeItem || busy" @click="openEditor('copy', activeItem)">Copy (&amp;C)</button><button class="win-button" type="button" :disabled="!selectedItems.length || busy" @click="approveSelected">Approve</button>
        <button class="win-button" type="button" :disabled="!activeItem" @click="openRelation('parameters')">Parameter</button><button class="win-button" type="button" :disabled="!activeItem" @click="openRelation('factors')">Factor</button><button class="win-button" type="button" :disabled="!activeItem" @click="openRelation('reasons')">Reason</button><button class="win-button" type="button" :disabled="!activeItem" @click="openRelation('simulation')">Simulation</button><button class="win-button" type="button" :disabled="!activeItem || activeItem.issueState===0 || activeItem.issueState===1" @click="openRelation('operations')">Op</button><button class="win-button" type="button" :disabled="!activeItem" @click="openRelation('samplePlanChanges')">SamplePlanChange</button>
        <button class="win-button close-page" type="button" @click="closePage">Close (&amp;X)</button>
      </aside>
    </div>
    <p v-if="errorMessage" class="message error" role="alert">{{ errorMessage }}</p><p v-if="notice" class="message notice" role="status">{{ notice }}</p>

    <div v-if="editorOpen" class="backdrop" @click.self="closeEditor"><section class="dialog editor-dialog" role="dialog" aria-modal="true" aria-label="計數品管項目維護">
      <header class="dialog-title"><h2>QCItem For Attributes Define ({{ editorMode.toUpperCase() }})</h2><span v-if="mock" class="dev-badge">DEV</span></header>
      <div class="form-sections"><section class="form-group identity-group"><h3>QC Item</h3>
        <label class="field"><span>QCItemNo</span><input v-model="form.qcItemNo" :readonly="editorMode==='edit'" maxlength="25"></label>
        <label class="field"><span>QCItem Name</span><input v-model="form.qcItemName" maxlength="500"></label>
        <label class="field"><span>Plan No</span><div class="input-action"><select v-model="form.planNo"><option value="">請選擇</option><option v-if="form.planNo && !planChoiceList.includes(form.planNo)" :value="form.planNo">{{ form.planNo }}</option><option v-for="option in planChoiceList" :key="option">{{ option }}</option></select><button class="win-button small-button" type="button" @click="form.planNo=planChoiceList[0] || ''">...</button></div></label>
        <label class="field"><span>Rule No</span><div class="input-action"><select v-model="form.ruleNo"><option value="">—</option><option v-if="form.ruleNo && !ruleChoiceList.includes(form.ruleNo)" :value="form.ruleNo">{{ form.ruleNo }}</option><option v-for="option in ruleChoiceList" :key="option">{{ option }}</option></select><button class="win-button small-button" type="button" @click="form.ruleNo=ruleChoiceList[0] || ''">...</button></div></label>
        <label class="field"><span>Special Form Name</span><select v-model="form.formName"><option value="">—</option><option v-if="form.formName && !interfaceChoiceList.includes(form.formName)" :value="form.formName">{{ form.formName }}</option><option v-for="option in interfaceChoiceList" :key="option">{{ option }}</option></select></label>
        <label class="field"><span>MClass No</span><div class="input-action"><select v-model="form.mClassNo"><option value="">—</option><option v-if="form.mClassNo && !mClassChoiceList.includes(form.mClassNo)" :value="form.mClassNo">{{ form.mClassNo }}</option><option v-for="option in mClassChoiceList" :key="option">{{ option }}</option></select><button class="win-button small-button" type="button" @click="form.mClassNo=mClassChoiceList[0] || ''">...</button></div></label>
        <label class="field wide-field"><span>Description</span><textarea v-model="form.description" rows="3" maxlength="255"></textarea></label>
      </section><section class="form-group options-group"><h3>Collection / Lot</h3>
        <fieldset v-if="showLotValid" class="radio-field"><legend>Lot Valid Check</legend><label><input v-model="form.lotIsValid" type="radio" :value="1"> Yes</label><label><input v-model="form.lotIsValid" type="radio" :value="0"> No</label></fieldset>
        <fieldset v-if="showComponentCollect" class="radio-field"><legend>Component Collect</legend><label><input v-model="form.componentCollect" type="radio" :value="1" :disabled="editorMode==='copy'"> Yes</label><label><input v-model="form.componentCollect" type="radio" :value="0" :disabled="editorMode==='copy'"> No</label></fieldset>
        <label class="field"><span>Limit Lot Num</span><input v-model="form.limitLotNum" inputmode="numeric"></label>
        <fieldset class="radio-field"><legend>Sample Size</legend><label><input v-model="form.isSampleSizeInteger" type="radio" :value="0"> Integer</label><label><input v-model="form.isSampleSizeInteger" type="radio" :value="1"> Decimal</label></fieldset>
        <label v-if="showQcClass" class="field"><span>QCClass</span><select v-model="form.qcClass"><option value="">—</option><option>SPC</option><option>IPQC</option></select></label>
      </section></div>
      <div v-if="editorMode==='copy'" class="copy-options"><strong>Copy Related Data</strong><label><input v-model="copyFlags.parameters" type="checkbox"> Parameter</label><label><input v-model="copyFlags.factors" type="checkbox"> Factor</label><label><input v-model="copyFlags.reasons" type="checkbox"> Reason</label><label><input v-model="copyFlags.samplePlanChanges" type="checkbox"> SamplePlanChange</label></div>
      <p v-if="editorError" class="message error">{{ editorError }}</p>
      <div class="audit-row"><span>建立人 {{ currentEditorAudit.creator }}</span><span>建立日期 {{ currentEditorAudit.createDate }}</span><span>修改人 {{ currentEditorAudit.reviser }}</span><span>修改日期 {{ currentEditorAudit.reviseDate }}</span></div>
      <footer class="dialog-actions"><span class="hint">新增/修改與關聯複製由 API 交易保存；DataStamp 用於避免覆蓋他人更新。</span><button class="win-button primary" type="button" :disabled="busy || !editorOptionsLoaded" @click="saveItem(true)">確定</button><button class="win-button" type="button" :disabled="busy" @click="closeEditor">關閉</button><button v-if="editorMode==='edit'" class="win-button" type="button" :disabled="busy || !editorOptionsLoaded" @click="saveItem(false)">套用</button></footer>
    </section></div>

    <div v-if="relationOpen" class="backdrop" @click.self="closeRelation"><section class="dialog relation-dialog" role="dialog" aria-modal="true" :aria-label="relationTitle">
      <header class="dialog-title"><h2>{{ relationTitle }} — {{ activeItem?.qcItemNo }}</h2><span>{{ activeItem?.qcItemName }}</span></header>
      <template v-if="relationMode==='parameters'"><div class="inline-add"><input v-model="newParameterNo" placeholder="ParameterNo"><button class="win-button" type="button" @click="addParameter">新增參數</button></div><div class="relation-table-wrap"><table><thead><tr><th>ParameterNo</th><th>ParameterValue</th><th></th></tr></thead><tbody><tr v-for="(row,index) in relationRows as ParameterRow[]" :key="row.parameterNo"><td>{{ row.parameterNo }}</td><td><input v-model="row.parameterValue"></td><td><button class="link-button" type="button" @click="removeRelationRow(index)">移除</button></td></tr><tr v-if="!relationRows.length"><td colspan="3" class="empty-row">尚未設定參數</td></tr></tbody></table></div></template>
      <template v-else-if="relationMode==='factors'"><div class="inline-add"><input v-model="newFactorNo" placeholder="FactorNo"><button class="win-button" type="button" @click="addFactor">新增要因</button></div><div class="relation-table-wrap"><table><thead><tr><th>FactorNo</th><th>Online</th><th>Spec</th><th>OPNo</th><th>TableName</th><th>FieldName</th><th>Short Run</th><th></th></tr></thead><tbody><tr v-for="(row,index) in relationRows as FactorRow[]" :key="row.factorNo"><td><input v-model="row.factorNo"></td><td><select v-model="row.factorOnline"><option>Y</option><option>N</option></select></td><td><select v-model="row.factorSpec"><option>Y</option><option>N</option></select></td><td><input v-model="row.opNo"></td><td><input v-model="row.tableName"></td><td><input v-model="row.fieldName"></td><td><select v-model="row.factorShortRun"><option>N</option><option>Y</option></select></td><td><button class="link-button" type="button" @click="removeRelationRow(index)">移除</button></td></tr><tr v-if="!relationRows.length"><td colspan="8" class="empty-row">尚未設定要因</td></tr></tbody></table></div></template>
      <template v-else-if="relationMode==='reasons'"><div class="reason-filters"><input v-model="relationSearch" placeholder="ReasonNo / ReasonName"><select v-model="relationTypeFilter"><option value="">全部 ReasonType</option><option v-for="option in reasonTypeOptions" :key="option.value" :value="option.value">{{ option.label }}</option></select><input v-model="relationLevelFilter" placeholder="ReasonLevel"></div><div class="reason-dual"><section><h3>UnSelected</h3><div class="relation-table-wrap"><table><tbody><tr v-for="row in filteredCandidates" :key="row.reasonNo"><td><input type="checkbox" :checked="relationSelectedNos.includes(row.reasonNo)" @change="moveReason(row.reasonNo, ($event.target as HTMLInputElement).checked)"></td><td>{{ row.reasonNo }}</td><td>{{ row.reasonName }}</td><td>{{ row.reasonLevel }}</td></tr></tbody></table></div></section><div class="move-count">{{ relationSelectedNos.length }} selected</div><section><h3>Selected</h3><div class="relation-table-wrap"><table><tbody><tr v-for="row in (relationRows as ReasonMapRow[]).filter(item=>relationSelectedNos.includes(item.reasonNo))" :key="row.reasonNo"><td><input type="checkbox" checked @change="moveReason(row.reasonNo,false)"></td><td>{{ row.reasonNo }}</td><td>{{ row.reasonName }}</td></tr><tr v-if="!relationSelectedNos.length"><td class="empty-row">尚未設定原因</td></tr></tbody></table></div></section></div></template>
      <template v-else-if="relationMode==='samplePlanChanges'"><div class="inline-add"><button class="win-button" type="button" @click="(relationRows as SamplePlanChangeRow[]).push({oldSamplePlan:activeItem?.planNo || '',accLotQty:-1,rejLotQty:-1,newSamplePlan:'',factorNo:'*',lotRange:-1,aqlChkForSPlanChg:0,qcItemType:0})">新增轉換條件</button></div><div class="relation-table-wrap"><table><thead><tr><th>Old Plan</th><th>Accept</th><th>Reject</th><th>New Plan</th><th>FactorNo</th><th>LotRange</th><th>AQL Check</th><th></th></tr></thead><tbody><tr v-for="(row,index) in relationRows as SamplePlanChangeRow[]" :key="`${row.oldSamplePlan}-${index}`"><td><input v-model="row.oldSamplePlan"></td><td><input v-model.number="row.accLotQty" type="number"></td><td><input v-model.number="row.rejLotQty" type="number"></td><td><input v-model="row.newSamplePlan"></td><td><input v-model="row.factorNo"></td><td><input v-model.number="row.lotRange" type="number"></td><td><select v-model.number="row.aqlChkForSPlanChg"><option :value="0">否</option><option :value="1">是</option></select></td><td><button class="link-button" type="button" @click="removeRelationRow(index)">移除</button></td></tr><tr v-if="!relationRows.length"><td colspan="8" class="empty-row">尚未設定抽樣轉換</td></tr></tbody></table></div></template>
      <template v-else-if="relationMode==='operations'"><div class="operation-dual"><section class="operation-list"><h3>未選作業站</h3><div class="relation-table-wrap"><table><thead><tr><th></th><th>OPNo</th><th>OPName</th></tr></thead><tbody><tr v-for="row in operationCandidates" :key="row.opNo"><td><input type="checkbox" :checked="operationCandidateNos.includes(row.opNo)" @change="toggleOperationCandidate(row.opNo, ($event.target as HTMLInputElement).checked)"></td><td>{{ row.opNo }}</td><td>{{ row.opName }}</td></tr><tr v-if="!operationCandidates.length"><td colspan="3" class="empty-row">沒有可加入的作業站</td></tr></tbody></table></div></section><div class="operation-moves"><button class="win-button" type="button" :disabled="!canEditOperations || !operationCandidateNos.length || activeItem?.lotIsValid!==1" @click="moveSelectedOperations(1)">加入 InLine →</button><button class="win-button" type="button" :disabled="!canEditOperations || !operationCandidateNos.length" @click="moveSelectedOperations(0)">加入 OffLine →</button><button class="win-button" type="button" :disabled="!canEditOperations || !selectedOperationNos.length" @click="removeSelectedOperations">← 移除</button></div><section class="operation-list assigned-list"><h3>已選作業站</h3><div class="relation-table-wrap"><table><thead><tr><th></th><th>Type</th><th>OPNo</th><th>OPName</th><th>ExecuteOrder</th><th>FAI</th><th>IPQC</th><th>PQC</th></tr></thead><tbody><tr v-for="row in relationRows as OperationRow[]" :key="row.opNo"><td><input type="checkbox" :checked="selectedOperationNos.includes(row.opNo)" @change="toggleSelectedOperation(row.opNo, ($event.target as HTMLInputElement).checked)"></td><td>{{ row.inline===1 ? 'InLine' : 'OffLine' }}</td><td>{{ row.opNo }}</td><td>{{ row.opName }}</td><td><input v-model.number="row.executeOrder" type="number" :disabled="!canEditOperations"></td><td><input v-model="row.inspTypeFai" type="checkbox" :disabled="!canEditOperations || row.inline!==1"></td><td><input v-model="row.inspTypeIpqc" type="checkbox" :disabled="!canEditOperations || row.inline!==1"></td><td><input v-model="row.inspTypePqc" type="checkbox" :disabled="!canEditOperations || row.inline!==1"></td></tr><tr v-if="!relationRows.length"><td colspan="8" class="empty-row">尚未設定所屬作業站</td></tr></tbody></table></div></section></div><p v-if="!canEditOperations" class="hint">只有已簽核的 QCItem 可以修改作業站；目前唯讀。</p><p v-else-if="activeItem?.lotIsValid===0" class="hint">LotIsValid=0，無法加入 InLine 作業站。</p></template>
      <template v-else><div class="relation-table-wrap"><table><thead><tr><th>Parameter</th><th>Value</th></tr></thead><tbody><tr v-for="(row,index) in simulationRows" :key="index"><td>{{ row.ParameterNo ?? row.parameterNo }}</td><td>{{ row.ParameterValue ?? row.parameterValue }}</td></tr><tr v-if="!simulationRows.length"><td colspan="2" class="empty-row">沒有模擬結果</td></tr></tbody></table></div></template>
      <p v-if="relationError" class="message error">{{ relationError }}</p><p v-if="relationNotice" class="message notice">{{ relationNotice }}</p>
      <footer class="dialog-actions"><span class="hint">{{ relationMode==='reasons' ? '只可對應已核准原因；確認時以交易替換整份對應。' : relationMode==='simulation' ? '模擬結果由 API 使用正式規則計算。' : relationMode==='operations' ? 'InLine 作業站至少要選擇一種檢驗時機；確認時保存整份所屬清單。' : 'DEV 為本地假資料；正式修改由 API 驗證及保存。' }}</span><button v-if="['parameters','factors','reasons','operations','samplePlanChanges'].includes(relationMode)" class="win-button primary" type="button" :disabled="busy || (relationMode==='operations' && !canEditOperations)" @click="saveRelation">確定</button><button class="win-button" type="button" :disabled="busy" @click="closeRelation">關閉</button></footer>
    </section></div>
  </main>
</template>

<style scoped>
.attribute-qc-page{--line:#c9c5bc;--text:#302f2c;--muted:#77746e;display:flex;flex-direction:column;gap:6px;height:100%;min-height:0;background:#f7f5ef;color:var(--text);font:13px/1.35 "Segoe UI","Microsoft JhengHei",sans-serif}.page-title{display:flex;align-items:center;justify-content:space-between;padding:2px 10px}.page-title h1{margin:0;font-size:20px}.dev-badge{padding:3px 8px;border:1px solid #d7a044;background:#fff1bd;color:#76551a;font-size:11px}.query-bar{display:flex;align-items:center;gap:12px;min-height:38px;padding:4px 12px;border-block:1px solid #d4d0c8;background:#fff}.state-option{display:flex;align-items:center;gap:4px;white-space:nowrap}.state-option input,.radio-field input{accent-color:#d89221}.query-label{margin-left:7px;white-space:nowrap}.query-input{width:min(240px,25vw);height:27px;border:1px solid #aaa69e;padding:3px 6px}.win-button{min-height:30px;padding:4px 9px;border:1px solid #bdb9b0;border-radius:3px;background:linear-gradient(#fff,#efeeeb);color:#343331;font:inherit;white-space:nowrap;cursor:pointer}.win-button:hover:not(:disabled){border-color:#d08a23;background:#fff6df}.win-button:disabled{opacity:.48;cursor:not-allowed}.query-button{min-width:54px}.main-area{display:grid;grid-template-columns:minmax(0,1fr) 88px;gap:6px;flex:1;min-height:0;padding:0 6px}.grid-panel{display:flex;flex-direction:column;gap:4px;min-width:0;min-height:0;padding:5px;border:1px solid var(--line);background:#fff}.grid-scroll{flex:1;min-height:0;overflow:auto;border:1px solid #bbb8b1}.main-grid{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}.main-grid th{position:sticky;top:0;z-index:2;padding:6px;border:1px solid #c5c1b9;background:#eeeae0;text-align:left}.main-grid td{height:26px;padding:3px 6px;border:1px solid #d1cec8;background:#fff}.main-grid tbody tr{cursor:pointer}.main-grid tbody tr:hover td{background:#fff8e2}.main-grid tbody tr.selected td{background:#ffedb1}.check-col{width:26px;text-align:center!important}.check-col input{margin:0}.empty-row{padding:16px!important;text-align:center;color:var(--muted)}.grid-footer{height:18px;padding:0 3px;color:#777;font-size:11px}.side-actions{display:flex;flex-direction:column;gap:7px;overflow:auto}.side-actions .win-button{width:100%;min-height:32px;padding:4px 5px;font-size:11px}.close-page{margin-top:auto}.message{margin:0 8px;padding:7px 9px;border:1px solid}.error{border-color:#d99b86;background:#fff0e9;color:#873820}.notice{border-color:#d8c79a;background:#fff8dc;color:#65541d}.backdrop{position:fixed;inset:0;z-index:1200;display:flex;align-items:center;justify-content:center;padding:16px;background:#17171770}.dialog{display:flex;flex-direction:column;gap:8px;max-height:94vh;padding:12px;border:1px solid #a9a59e;background:#f7f5ef;box-shadow:0 9px 30px #0004}.editor-dialog{width:min(662px,96vw);min-height:min(527px,94vh)}.relation-dialog{width:min(940px,96vw);height:min(600px,92vh)}.dialog-title{display:flex;align-items:center;justify-content:space-between;min-height:28px;border-bottom:1px solid var(--line)}.dialog-title h2{margin:0;font-size:17px}.dialog-title>span{color:var(--muted);font-size:12px}.form-sections{display:grid;grid-template-columns:1.2fr .8fr;gap:8px;flex:1;min-height:0}.form-group{display:flex;flex-direction:column;gap:7px;padding:8px;border:1px solid var(--line);background:#fff}.form-group h3{margin:0 0 3px;font-size:13px}.field{display:grid;grid-template-columns:108px minmax(0,1fr);align-items:center;gap:6px}.field>span{text-align:right;color:#555}.field input,.field select,.field textarea,.inline-add input,.relation-table-wrap input,.relation-table-wrap select,.reason-filters input,.reason-filters select{box-sizing:border-box;min-width:0;width:100%;height:27px;padding:3px 6px;border:1px solid #aaa69e;background:#fff;color:#333;font:inherit}.field textarea{height:auto;resize:vertical}.field input:read-only{background:#f0efed}.wide-field{grid-column:1/-1;align-items:start}.wide-field>span{padding-top:5px}.input-action{display:flex;gap:4px}.input-action select{flex:1}.small-button{min-width:30px;padding-inline:6px}.radio-field{display:flex;align-items:center;gap:9px;margin:0;padding:5px 7px;border:1px solid #d2cfc8}.radio-field legend{padding:0 4px;color:#555}.radio-field label{display:flex;align-items:center;gap:3px;white-space:nowrap}.options-group .field{grid-template-columns:100px minmax(0,1fr)}.options-group .field small{grid-column:2;color:#8b6b31;font-size:10px}.copy-options{display:flex;flex-wrap:wrap;align-items:center;gap:12px;padding:7px;border:1px solid var(--line);background:#fff}.copy-options label{display:flex;align-items:center;gap:3px}.audit-row{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:2px 14px;padding:6px;border:1px solid #c9c5bc;background:#f4f2ed;color:#666;font-size:11px}.dialog-actions{display:flex;align-items:center;justify-content:flex-end;gap:6px;min-height:34px}.hint{flex:1;color:#69665e;font-size:10px}.primary{background:#f5a72b;color:white}.relation-table-wrap{flex:1;min-height:0;overflow:auto;border:1px solid #bbb8b1;background:white}.relation-table-wrap table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}.relation-table-wrap th{position:sticky;top:0;padding:6px;border:1px solid #c6c2ba;background:#eeeae0;text-align:left}.relation-table-wrap td{padding:4px 6px;border:1px solid #d1cec8}.relation-table-wrap td input,.relation-table-wrap td select{min-width:70px;height:25px}.inline-add{display:flex;gap:6px;max-width:420px}.inline-add input{flex:1}.link-button{border:0;background:transparent;color:#8a5a13;text-decoration:underline;cursor:pointer}.reason-filters{display:grid;grid-template-columns:1fr 180px 110px;gap:6px}.reason-dual{display:grid;grid-template-columns:minmax(0,1fr) 96px minmax(0,1fr);gap:6px;flex:1;min-height:0}.reason-dual section{display:flex;flex-direction:column;min-width:0;min-height:0;padding:5px;border:1px solid var(--line);background:#fff}.reason-dual h3{margin:0 0 5px;text-align:center;font-size:13px}.reason-dual .relation-table-wrap{border:0}.reason-dual td:first-child{width:26px;text-align:center}.move-count{display:grid;place-items:center;color:#746f65;font-size:11px}@media(max-width:850px){.query-bar{flex-wrap:wrap}.main-area{grid-template-columns:minmax(0,1fr) 72px}.form-sections{grid-template-columns:1fr;overflow:auto}.editor-dialog{height:92vh}.options-group{display:grid;grid-template-columns:repeat(2,minmax(0,1fr))}.options-group h3{grid-column:1/-1}.field{grid-template-columns:100px minmax(0,1fr)}.relation-dialog{height:92vh}.relation-table-wrap{overflow:auto}}@media(max-width:580px){.reason-dual{grid-template-columns:minmax(0,1fr) 50px minmax(0,1fr)}.reason-filters{grid-template-columns:1fr 1fr}.reason-filters input{grid-column:1/-1}.dialog-actions{flex-wrap:wrap}.hint{flex-basis:100%}}
.query-input,.field input:not(:read-only),.field select,.field textarea,.inline-add input,.relation-table-wrap input:not([type="checkbox"]),.relation-table-wrap select,.reason-filters input,.reason-filters select{background:#fff1bf}.field input:read-only{background:#f0efed}.state-option input,.radio-field input,.check-col input,.reason-dual input[type="checkbox"]{accent-color:#d89221}.audit-row{border-color:#c9c5bc}
.operation-dual{display:grid;grid-template-columns:minmax(0,1fr) 132px minmax(0,1.5fr);gap:7px;flex:1;min-height:0}.operation-list{display:flex;flex-direction:column;min-width:0;min-height:0;padding:5px;border:1px solid var(--line);background:#fff}.operation-list h3{margin:0 0 5px;text-align:center;font-size:13px}.operation-moves{display:flex;flex-direction:column;justify-content:center;gap:7px}.operation-moves .win-button{white-space:normal}.operation-list input[type="checkbox"]{accent-color:#d89221}@media(max-width:850px){.operation-dual{grid-template-columns:1fr;overflow:auto}.operation-moves{flex-direction:row;flex-wrap:wrap}.operation-list{min-height:180px}}
</style>
