<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'
import { apiErrorMessage } from '../utils/apiError'
type IssueState = 0 | 1 | 2 | -1
type EditorMode = 'add' | 'edit' | 'copy'
type RelationMode = 'parameters' | 'factors' | 'reasons' | 'operations' | 'samplePlanChanges' | 'simulation'
interface QcItem {
  qcItemNo: string; qcItemName: string | null; lotIsValid: number | null; componentCollect: number | null; planNo: string | null; ruleNo: string | null
  formName: string | null; description: string | null; limitLotNum: number | null; isSampleSizeInteger: number | null; qcClass: string | null; mClassNo: string | null
  issueState: number | null; dataStamp: number | null; creator: string | null; createDate: string | null; reviser: string | null; reviseDate: string | null; executionFile: string | null
}
interface ParameterRow { parameterNo: string; parameterValue: string | null }
interface FactorRow { factorNo: string; factorName?: string | null; parameterType?: number | null; metadataAvailable?: boolean; metadataUnavailableReason?: string | null; factorOnline: string | null; factorSpec: string | null; opNo: string | null; tableName: string | null; fieldName: string | null; factorShortRun: string | null }
interface FactorChoice { factorNo: string; factorName: string; parameterType: number; freeInput: boolean }
interface ReasonRow { reasonNo: string; reasonName: string; reasonType: string; reasonLevel: string; issueState: number }
interface ValueOption { value: string; label: string }
interface ReasonMapRow { qcItemNo: string; reasonNo: string; reasonName: string | null; creator: string | null; createDate: string | null; reviser: string | null; reviseDate: string | null }
interface SamplePlanChangeRow { oldSamplePlan: string; accLotQty: number; rejLotQty: number; newSamplePlan: string | null; factorNo: string; lotRange: number; aqlChkForSPlanChg: number | null; qcItemType: number | null }
interface OperationRow { opNo: string; opName: string | null; inline: number; executeOrder: number; qcItemType: number; inspTypeFai: boolean | null; inspTypeIpqc: boolean | null; inspTypePqc: boolean | null; reviser: string | null; reviseDate: string | null }
type RelationRow = ParameterRow | FactorRow | ReasonMapRow | SamplePlanChangeRow | OperationRow
interface Snapshot { qcItemNo: string; issueState: number | null; dataStamp: number | null; version: string; items: RelationRow[]; writable: boolean; writeUnavailableReason: string | null }
interface CopyPreview { sourceQcItemNo: string; issueState: number | null; dataStamp: number | null; sourceVersion: string; parametersDiffer: boolean; itemParameters: ParameterRow[]; planParameters: ParameterRow[]; parameterSources: string[] }
interface Availability { available: boolean; detail: string | null }
interface Options { samplingPlans: string[] | null; attributeRules: string[] | null; mClasses: string[] | null; interfaces: string[] | null; showQcClass: boolean; showLotValid: boolean; showComponentCollect: boolean; availability: Record<string, Availability> }
interface MockStore { items: QcItem[]; parameters: Record<string, ParameterRow[]>; factors: Record<string, FactorRow[]>; reasons: Record<string, ReasonMapRow[]>; samplePlanChanges: Record<string, SamplePlanChangeRow[]>; operations: Record<string, OperationRow[]> }
interface QcForm { qcItemNo: string; qcItemName: string; lotIsValid: number | null; componentCollect: number | null; planNo: string; ruleNo: string; formName: string; description: string; limitLotNum: string; isSampleSizeInteger: number | null; qcClass: string; mClassNo: string }
type ListPayload<T> = T[] | { items: T[] }
const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const apiRoot = 'api/qc/attribute-qc-items', mockKey = 'ymes-dev-mock:qc-attribute-item:v1'
const items = ref<QcItem[]>([]), selectedNos = ref<string[]>([]), activeNo = ref('')
const issueState = ref<IssueState>(0), searchText = ref(''), loading = ref(false), busy = ref(false), closing = ref(false)
const errorMessage = ref(''), notice = ref(''), editorOpen = ref(false), editorMode = ref<EditorMode>('add'), editorError = ref(''), editorNotice = ref('')
const editorOptionsLoaded = ref(false), editorOptionsLoading = ref(false), form = ref<QcForm>(blankForm()), editingOriginal = ref<QcItem | null>(null)
const editingDataStamp = ref<number | null>(null), editingIssueState = ref<number | null>(null), originalEditor = ref('')
const copyFlags = ref({ parameters: true, factors: true, reasons: true, samplePlanChanges: true }), copyPreview = ref<CopyPreview | null>(null), parameterSource = ref('')
const planOptions = ref<string[]>([]), ruleOptions = ref<string[]>([]), mClassOptions = ref<string[]>([]), interfaceOptions = ref<string[]>([])
const showQcClass = ref(false), showLotValid = ref(true), showComponentCollect = ref(true), lookupAvailability = ref<Record<string, Availability>>({})
const relationOpen = ref(false), relationMode = ref<RelationMode>('parameters'), relationTitle = ref(''), relationError = ref(''), relationNotice = ref('')
const relationRows = ref<RelationRow[]>([]), relationCandidates = ref<ReasonRow[]>([]), operationCandidates = ref<OperationRow[]>([])
const operationChoices = ref<OperationRow[]>([])
const operationCandidateNos = ref<string[]>([]), selectedOperationNos = ref<string[]>([]), reasonTypeOptions = ref<ValueOption[]>([]), relationSelectedNos = ref<string[]>([])
const relationSearch = ref(''), relationTypeFilter = ref(''), relationLevelFilter = ref(''), newParameterNo = ref(''), newFactorNo = ref('')
const relationOwner = ref<QcItem | null>(null), relationSnapshot = ref<Snapshot | null>(null), relationLoading = ref(false), relationReady = ref(false), originalRelation = ref('')
const factorChoices = ref<FactorChoice[]>([]), factorChoicesLoaded = ref(false), reasonTypesUnavailable = ref(''), simulationRows = ref<Array<Record<string, string | number>>>([])
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()
let listEpoch = 0, editorEpoch = 0, relationEpoch = 0
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function logicalKey(value: string): string { return value.replace(/^ +| +$/g, '').toLowerCase() }
function sameKey(left: string, right: string): boolean { return logicalKey(left) === logicalKey(right) }
function reasonSelected(no: string): boolean { return relationSelectedNos.value.some(selected => sameKey(selected, no)) }
function failure(value: unknown): string { return value instanceof Error ? value.message : '操作失敗，請重新查詢。' }
function blankForm(): QcForm { return { qcItemNo: '', qcItemName: '', lotIsValid: 0, componentCollect: 0, planNo: '', ruleNo: '', formName: '', description: '', limitLotNum: '20', isSampleSizeInteger: 0, qcClass: '', mClassNo: '' } }
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

function listOf<T>(payload: ListPayload<T>): T[] { const rows = Array.isArray(payload) ? payload : payload?.items; if (!Array.isArray(rows)) throw new Error('伺服器清單回應不完整。'); return rows }
function path(resource = '', no?: string, extra: Record<string, string> = {}): string { const query = new URLSearchParams(extra); if (no !== undefined) query.set('qcItemNo', no); return apiRoot + (resource ? '/' + resource : '') + (query.size ? '?' + query.toString() : '') }
async function request<T>(resource: string, init: RequestInit = {}): Promise<T> {
  if (!props.apiBaseUrl.trim()) throw new Error('目前環境尚未設定 API 位址。')
  const url = new URL(resource.replace(/^\/+/, ''), new URL(props.apiBaseUrl.replace(/\/+$/, '') + '/', window.location.origin))
  const headers = new Headers(init.headers); if (init.body) headers.set('Content-Type', 'application/json')
  const response = await fetch(url, { credentials: 'include', ...init, headers })
  if (!response.ok) { if (response.status === 401) location.assign(import.meta.env.BASE_URL); throw new Error(await apiErrorMessage(response)) }
  return response.status === 204 ? undefined as T : await response.json() as T
}
function normalizeItem(value: unknown): QcItem {
  if (!value || typeof value !== 'object') throw new Error('品管項目回應不完整。')
  const row = value as Record<string, unknown>
  const number = (key: string) => { if (!Object.hasOwn(row, key)) throw new Error('品管項目缺少版本或原始欄位。'); if (row[key] === null) return null; if (typeof row[key] !== 'number' || !Number.isFinite(row[key])) throw new Error('品管項目數值回應無效。'); return row[key] as number }
  const text = (key: string) => { if (!Object.hasOwn(row, key) || (row[key] !== null && typeof row[key] !== 'string')) throw new Error('品管項目文字回應不完整。'); return row[key] as string | null }
  if (typeof row.qcItemNo !== 'string' || !row.qcItemNo) throw new Error('品管項目沒有有效編號。')
  return { qcItemNo: row.qcItemNo, qcItemName: text('qcItemName'), lotIsValid: number('lotIsValid'), componentCollect: number('componentCollect'), planNo: text('planNo'), ruleNo: text('ruleNo'), formName: text('formName'), description: text('description'), limitLotNum: number('limitLotNum'), isSampleSizeInteger: number('isSampleSizeInteger'), qcClass: text('qcClass'), mClassNo: text('mClassNo'), issueState: number('issueState'), dataStamp: number('dataStamp'), creator: text('creator'), createDate: text('createDate'), reviser: text('reviser'), reviseDate: text('reviseDate'), executionFile: text('executionFile') }
}
function parseSnapshot(value: Snapshot, no: string): Snapshot { if (value.qcItemNo !== no || !Array.isArray(value.items) || !Object.hasOwn(value, 'dataStamp') || !Object.hasOwn(value, 'issueState') || !/^[0-9A-Fa-f]{64}$/.test(value.version) || typeof value.writable !== 'boolean') throw new Error('明細快照不完整，請重新開啟。'); return clone(value) }
function stateLabel(value: number | null): string { return value === null ? '未設定' : ({ 0: '未簽核', 1: '簽核中', 2: '已簽核', '-1': '不使用' } as Record<string, string>)[String(value)] ?? String(value) }
function valueLabel(value: number | null): string { return value === 1 ? '是' : value === 0 ? '否' : '' }
function sampleSizeModeLabel(value: number | null): string { return value === 0 ? '整數' : value === 1 ? '小數' : '' }
function displayDate(value?: string | null): string { if (!value) return ''; const date = new Date(value); return Number.isNaN(date.getTime()) ? value : new Intl.DateTimeFormat('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23' }).format(date) }
const visibleItems = computed(() => items.value.filter(item => item.issueState === issueState.value && item.qcItemNo.toLowerCase().includes(searchText.value.trim().toLowerCase())))
const activeItem = computed(() => visibleItems.value.find(item => item.qcItemNo === activeNo.value) ?? null)
const selectedItems = computed(() => visibleItems.value.filter(item => selectedNos.value.includes(item.qcItemNo)))
const targets = computed(() => selectedItems.value.length ? selectedItems.value : activeItem.value ? [activeItem.value] : [])
const blocked = computed(() => busy.value || loading.value || closing.value || editorOpen.value || relationOpen.value || confirmationMessage.value !== null)
const relationWritable = computed(() => relationReady.value && !relationLoading.value && (mock.value ? relationMode.value === 'operations' ? relationOwner.value?.issueState === 2 : relationOwner.value?.issueState === 0 : relationSnapshot.value?.writable === true))
const canEditOperations = computed(() => relationWritable.value && relationMode.value === 'operations')
const planChoiceList = computed(() => mock.value ? ['DEV-PLAN-01', 'DEV-PLAN-02'] : planOptions.value)
const ruleChoiceList = computed(() => mock.value ? ['DEV-AR-01', 'DEV-AR-02'] : ruleOptions.value)
const mClassChoiceList = computed(() => mock.value ? ['DEV-MC-01', 'DEV-MC-02'] : mClassOptions.value)
const interfaceChoiceList = computed(() => mock.value ? ['frmAttributeInspection', 'frmAppearanceInspection'] : interfaceOptions.value)
const currentEditorAudit = computed(() => ({ creator: editorMode.value === 'edit' ? editingOriginal.value?.creator ?? '' : '', createDate: editorMode.value === 'edit' ? displayDate(editingOriginal.value?.createDate) : '', reviser: editorMode.value === 'edit' ? editingOriginal.value?.reviser ?? '' : '', reviseDate: editorMode.value === 'edit' ? displayDate(editingOriginal.value?.reviseDate) : '' }))
const filteredCandidates = computed(() => relationCandidates.value.filter(row => (!relationSearch.value || `${row.reasonNo} ${row.reasonName}`.toLowerCase().includes(relationSearch.value.toLowerCase())) && (!relationTypeFilter.value || row.reasonType === relationTypeFilter.value) && (!relationLevelFilter.value || row.reasonLevel === relationLevelFilter.value) && !reasonSelected(row.reasonNo)))
function editorState(): string { return JSON.stringify({ form: form.value, copyFlags: editorMode.value === 'copy' ? copyFlags.value : null, parameterSource: editorMode.value === 'copy' ? parameterSource.value : null }) }
function relationState(): string { return JSON.stringify({ rows: relationRows.value, selected: relationSelectedNos.value }) }
function syncSelection(preferred = activeNo.value): void { const visible = new Set(visibleItems.value.map(x => x.qcItemNo)); selectedNos.value = selectedNos.value.filter(x => visible.has(x)); activeNo.value = visible.has(preferred) ? preferred : selectedNos.value.at(-1) ?? visibleItems.value[0]?.qcItemNo ?? ''; if (activeNo.value && !selectedNos.value.length) selectedNos.value = [activeNo.value]; if (selectedNos.value.length && !selectedNos.value.includes(activeNo.value)) activeNo.value = selectedNos.value.at(-1) ?? '' }
function upsert(row: QcItem): void { const index = items.value.findIndex(x => x.qcItemNo === row.qcItemNo); if (index < 0) items.value.push(clone(row)); else items.value[index] = clone(row) }
async function reloadItems(preferred = activeNo.value): Promise<void> { const epoch = ++listEpoch; const state = issueState.value; const search = searchText.value.trim(); const rows = mock.value ? readStore().items.filter(x => x.issueState === state) : listOf(await request<ListPayload<unknown>>(path('', undefined, { issueState: String(state), search }))).map(normalizeItem); if (epoch !== listEpoch || state !== issueState.value) return; items.value = rows; syncSelection(preferred) }
async function loadItems(): Promise<void> { if (blocked.value) return; loading.value = true; errorMessage.value = ''; notice.value = ''; try { await reloadItems() } catch (error) { errorMessage.value = failure(error) } finally { loading.value = false } }
function selectRow(row: QcItem, event: MouseEvent): void { if (blocked.value) return; if (event.ctrlKey || event.metaKey) toggleSelected(row, !selectedNos.value.includes(row.qcItemNo)); else { selectedNos.value = [row.qcItemNo]; activeNo.value = row.qcItemNo } }
function toggleSelected(row: QcItem, selected: boolean): void { if (blocked.value) return; selectedNos.value = selected ? [...new Set([...selectedNos.value, row.qcItemNo])] : selectedNos.value.filter(x => x !== row.qcItemNo); activeNo.value = selected ? row.qcItemNo : selectedNos.value.includes(activeNo.value) ? activeNo.value : selectedNos.value.at(-1) ?? '' }
function toForm(row: QcItem, copy = false): QcForm { return { qcItemNo: copy ? '' : row.qcItemNo, qcItemName: row.qcItemName ?? '', lotIsValid: row.lotIsValid, componentCollect: row.componentCollect, planNo: row.planNo ?? '', ruleNo: row.ruleNo ?? '', formName: row.formName ?? '', description: row.description ?? '', limitLotNum: row.limitLotNum === null ? '' : String(row.limitLotNum), isSampleSizeInteger: row.isSampleSizeInteger, qcClass: row.qcClass ?? '', mClassNo: row.mClassNo ?? '' } }
async function freshItem(no: string): Promise<QcItem> { const row = mock.value ? readStore().items.find(x => x.qcItemNo === no) : normalizeItem(await request(path('item', no))); if (!row) throw new Error('品管項目已不存在。'); return clone(row) }
async function openEditor(mode: EditorMode, item: QcItem | null = null): Promise<void> { if (blocked.value) return; busy.value = true; errorMessage.value = ''; try { const row = mode === 'add' ? null : await freshItem(item?.qcItemNo ?? ''); if (mode === 'edit' && row?.issueState !== 0) throw new Error('目前只開放修改未簽核資料。'); if (mode === 'copy' && (row?.issueState === null || row?.issueState === 1)) throw new Error('此狀態不能複製。'); editorMode.value = mode; editingOriginal.value = row; editingDataStamp.value = row?.dataStamp ?? null; editingIssueState.value = row?.issueState ?? null; form.value = row ? toForm(row, mode === 'copy') : blankForm(); copyFlags.value = { parameters: true, factors: true, reasons: true, samplePlanChanges: true }; copyPreview.value = null; parameterSource.value = ''; editorError.value = ''; editorNotice.value = ''; editorOptionsLoaded.value = mock.value; editorOpen.value = true; originalEditor.value = editorState(); await focusDialog('.editor-dialog'); await loadEditorOptions() } catch (error) { errorMessage.value = failure(error) } finally { busy.value = false; if (editorOpen.value) await focusDialog('.editor-dialog') } }
async function loadEditorOptions(): Promise<void> { const epoch = ++editorEpoch; editorOptionsLoading.value = true; editorOptionsLoaded.value = false; try { if (mock.value) { showQcClass.value = true; showLotValid.value = true; showComponentCollect.value = true; editorOptionsLoaded.value = true; return } const options = await request<Options>(path('options', undefined, { componentCollect: form.value.componentCollect === null ? '' : String(form.value.componentCollect) })); if (epoch !== editorEpoch || !editorOpen.value) return; planOptions.value = options.samplingPlans ?? []; ruleOptions.value = options.attributeRules ?? []; mClassOptions.value = options.mClasses ?? []; interfaceOptions.value = options.interfaces ?? []; lookupAvailability.value = options.availability; showQcClass.value = options.showQcClass; showLotValid.value = options.showLotValid; showComponentCollect.value = options.showComponentCollect; editorOptionsLoaded.value = options.availability.samplingPlans?.available === true; if (!editorOptionsLoaded.value) editorError.value = options.availability.samplingPlans?.detail || '抽樣計劃選項暫不可用。' } catch (error) { if (epoch === editorEpoch) editorError.value = failure(error) } finally { if (epoch === editorEpoch) editorOptionsLoading.value = false } }
watch(() => [form.value.planNo, form.value.componentCollect, copyFlags.value.parameters, copyFlags.value.factors, copyFlags.value.reasons, copyFlags.value.samplePlanChanges], (next, previous) => { copyPreview.value = null; parameterSource.value = ''; if (editorOpen.value && !busy.value && next[1] !== previous?.[1]) void loadEditorOptions() })
function validateForm(): string { if (!editorOptionsLoaded.value || editorOptionsLoading.value) return '欄位選項尚未載入完成。'; if (!form.value.qcItemNo.trim() || form.value.qcItemNo.length > 25 || !form.value.qcItemName.trim() || form.value.qcItemName.length > 500) return '請填寫有效的編號與名稱。'; if (!form.value.planNo.trim()) return '請選擇抽樣計劃。'; if (![0, 1].includes(form.value.lotIsValid as number) || ![0, 1].includes(form.value.componentCollect as number) || ![0, 1].includes(form.value.isSampleSizeInteger as number)) return '請明確選擇收集與抽樣選項。'; const limit = form.value.limitLotNum.trim() ? Number(form.value.limitLotNum) : NaN; if (!Number.isInteger(limit) || limit < 1 || limit > 99999) return 'Limit Lot Num 必須為1到99999的整數。'; if (showQcClass.value && !form.value.qcClass) return '請選擇QCClass。'; return '' }
function payload(): Record<string, unknown> { const old = editingOriginal.value; const optional = (key: 'ruleNo' | 'formName' | 'description' | 'qcClass' | 'mClassNo') => old && (old[key] ?? '') === form.value[key] ? old[key] : form.value[key]; return { qcItemNo: form.value.qcItemNo.trim(), qcItemName: form.value.qcItemName, lotIsValid: form.value.lotIsValid, componentCollect: form.value.componentCollect, planNo: form.value.planNo, ruleNo: optional('ruleNo'), formName: optional('formName'), description: optional('description'), limitLotNum: Number(form.value.limitLotNum), isSampleSizeInteger: form.value.isSampleSizeInteger, qcClass: optional('qcClass'), mClassNo: optional('mClassNo') } }
function copyPayload(): Record<string, unknown> { return { ...payload(), issueState: editingIssueState.value, dataStamp: editingDataStamp.value, copyParameters: copyFlags.value.parameters, copyFactors: copyFlags.value.factors, copyReasons: copyFlags.value.reasons, copySamplePlanChanges: copyFlags.value.samplePlanChanges } }
function adoptSaved(row: QcItem): void { issueState.value = 0; searchText.value = ''; upsert(row); activeNo.value = row.qcItemNo; selectedNos.value = [row.qcItemNo]; editingOriginal.value = clone(row); editingDataStamp.value = row.dataStamp; editingIssueState.value = row.issueState; editorMode.value = 'edit'; form.value = toForm(row); copyPreview.value = null; parameterSource.value = ''; originalEditor.value = editorState() }
async function saveItem(closeAfterSave = true): Promise<void> {
  if (busy.value || closing.value || confirmationMessage.value !== null || !editorOpen.value) return
  editorError.value = validateForm(); if (editorError.value) return
  busy.value = true; editorNotice.value = ''
  try {
    await focusDialog('.editor-dialog')
    let saved: QcItem
    if (mock.value) {
      const store = readStore(), old = editingOriginal.value, now = new Date().toISOString()
      if (editorMode.value === 'edit') {
        const index = store.items.findIndex(x => x.qcItemNo === old?.qcItemNo)
        if (index < 0 || store.items[index]?.dataStamp !== editingDataStamp.value) throw new Error('資料已變更，請重新開啟。')
        saved = { ...store.items[index]!, ...payload(), dataStamp: (editingDataStamp.value ?? 0) + 1, reviser: props.currentUserNo || 'DEV001', reviseDate: now } as QcItem
        store.items[index] = saved
      } else {
        if (store.items.some(x => x.qcItemNo.toUpperCase() === form.value.qcItemNo.trim().toUpperCase())) throw new Error('編號已存在。')
        saved = { ...demoItem(form.value.qcItemNo.trim(), form.value.qcItemName, 0, now), ...payload() } as QcItem
        store.items.push(saved)
        if (editorMode.value === 'copy' && old) for (const kind of ['parameters', 'factors', 'reasons', 'samplePlanChanges'] as const)
          if (copyFlags.value[kind]) (store[kind] as Record<string, RelationRow[]>)[saved.qcItemNo] = clone(store[kind][old.qcItemNo] ?? [])
      }
      saveStore(store)
    } else if (editorMode.value === 'copy') {
      const body = copyPayload()
      const preview = await request<CopyPreview>(path('copy-preview', editingOriginal.value!.qcItemNo), { method: 'POST', body: JSON.stringify(body) })
      if (copyPreview.value && copyPreview.value.sourceVersion !== preview.sourceVersion) {
        copyPreview.value = preview; parameterSource.value = ''
        throw new Error('複製來源或抽樣計劃已變更，請重新確認參數來源。')
      }
      copyPreview.value = preview
      if (copyFlags.value.parameters && preview.parametersDiffer && !['item', 'plan'].includes(parameterSource.value)) {
        editorNotice.value = '參數不同，請選擇下方的參數來源，再按儲存。'; return
      }
      saved = normalizeItem(await request(path('copy', editingOriginal.value!.qcItemNo), { method: 'POST', body: JSON.stringify({ ...body, sourceVersion: preview.sourceVersion, parameterSource: parameterSource.value || null }) }))
    } else saved = normalizeItem(await request(editorMode.value === 'edit' ? path('item', editingOriginal.value!.qcItemNo) : apiRoot, {
      method: editorMode.value === 'edit' ? 'PUT' : 'POST',
      body: JSON.stringify({ ...payload(), ...(editorMode.value === 'edit' ? { issueState: editingIssueState.value, dataStamp: editingDataStamp.value } : {}) }),
    }))
    adoptSaved(saved); editorNotice.value = '資料已儲存。'; notice.value = '計數品管項目已儲存。'
    if (closeAfterSave) { editorOpen.value = false; ++editorEpoch }
    try { await reloadItems(saved.qcItemNo) }
    catch (error) { const text = '已儲存；清單更新失敗，請重新查詢。' + failure(error); errorMessage.value = text; editorNotice.value = text }
  } catch (error) { editorError.value = failure(error) + ' 請重新查詢確認目前資料。' }
  finally { busy.value = false; if (editorOpen.value) await focusDialog('.editor-dialog') }
}

async function mutate(kind: 'approve' | 'delete'): Promise<void> {
  if (blocked.value || !targets.value.length) return
  const rows = clone(targets.value)
  if (rows.some(x => kind === 'approve' ? x.issueState !== 0 : ![-1, 0, 2].includes(x.issueState as number))) {
    errorMessage.value = '所選資料的狀態不允許此操作。'; return
  }
  busy.value = true; errorMessage.value = ''; notice.value = ''; let completed = 0
  try {
    if (!await requestConfirmation(`確定${kind === 'approve' ? '核准' : '刪除／停用'}所選 ${rows.length} 筆資料？`)) return
    for (const row of rows) {
      if (mock.value) {
        const store = readStore(), current = store.items.find(x => x.qcItemNo === row.qcItemNo)
        if (!current || current.dataStamp !== row.dataStamp) throw new Error('資料已變更。')
        if (kind === 'approve') {
          if (!store.factors[row.qcItemNo]?.length || !store.reasons[row.qcItemNo]?.length) throw new Error('缺少要因或原因設定。')
          current.issueState = 2; current.dataStamp = (current.dataStamp ?? 0) + 1; upsert(current)
        } else if (current.issueState === 2) {
          current.issueState = -1; current.dataStamp = (current.dataStamp ?? 0) + 1; upsert(current)
        } else {
          store.items = store.items.filter(x => x.qcItemNo !== row.qcItemNo)
          for (const resource of ['parameters', 'factors', 'reasons', 'operations', 'samplePlanChanges'] as const) delete store[resource][row.qcItemNo]
          items.value = items.value.filter(x => x.qcItemNo !== row.qcItemNo)
        }
        saveStore(store)
      } else if (kind === 'approve') upsert(normalizeItem(await request(path('approve', row.qcItemNo), { method: 'POST', body: JSON.stringify({ issueState: row.issueState, dataStamp: row.dataStamp }) })))
      else {
        await request(path('item', row.qcItemNo), { method: 'DELETE', body: JSON.stringify({ issueState: row.issueState, dataStamp: row.dataStamp }) })
        items.value = items.value.filter(x => x.qcItemNo !== row.qcItemNo)
      }
      completed++
    }
  } catch (error) { errorMessage.value = failure(error) }
  finally {
    if (completed || errorMessage.value) {
      notice.value = `已完成 ${completed}/${rows.length} 筆。`
      try { await reloadItems() } catch (error) { errorMessage.value += (errorMessage.value ? ' ' : '') + '清單更新失敗：' + failure(error) }
    }
    busy.value = false
  }
}
function approveSelected(): Promise<void> { return mutate('approve') }
function deleteSelected(): Promise<void> { return mutate('delete') }

async function openRelation(mode: RelationMode): Promise<void> {
  if (blocked.value || !activeItem.value) return
  relationOwner.value = clone(activeItem.value); relationMode.value = mode
  relationTitle.value = ({ parameters: 'Parameter', factors: 'Factor', reasons: 'Reason', operations: 'Op', samplePlanChanges: 'SamplePlanChange', simulation: 'Simulation' })[mode]
  relationOpen.value = true; relationReady.value = false; relationRows.value = []; relationSelectedNos.value = []; relationSnapshot.value = null
  relationCandidates.value = []; operationCandidates.value = []; operationChoices.value = []; factorChoices.value = []; reasonTypeOptions.value = []; factorChoicesLoaded.value = false
  reasonTypesUnavailable.value = ''; operationCandidateNos.value = []; selectedOperationNos.value = []; relationSearch.value = ''; relationTypeFilter.value = ''; relationLevelFilter.value = ''
  relationError.value = ''; relationNotice.value = ''; originalRelation.value = ''; simulationRows.value = []
  await focusDialog('.relation-dialog'); await loadRelation(mode); if (relationOpen.value) await focusDialog('.relation-dialog')
}

async function loadRelation(mode: RelationMode): Promise<void> {
  const owner = relationOwner.value; if (!owner) return
  const epoch = ++relationEpoch; relationLoading.value = true; relationReady.value = false; relationError.value = ''
  try {
    if (mode === 'simulation') {
      const result = mock.value ? [{ ParameterNo: 'SampleSize', ParameterValue: '32' }] : await request<Array<Record<string, string | number>>>(path('simulate', owner.qcItemNo), { method: 'POST', body: JSON.stringify({ planNo: owner.planNo }) })
      if (epoch === relationEpoch && relationOpen.value) simulationRows.value = result
      return
    }
    let parent = await freshItem(owner.qcItemNo)
    let snapshot: Snapshot
    if (mock.value) {
      const store = readStore(), rows = clone((store[mode] as Record<string, RelationRow[]>)[owner.qcItemNo] ?? [])
      snapshot = { qcItemNo: owner.qcItemNo, issueState: parent.issueState, dataStamp: parent.dataStamp, version: JSON.stringify({ parent, rows }), items: rows, writable: mode === 'operations' ? parent.issueState === 2 : parent.issueState === 0, writeUnavailableReason: null }
    } else {
      snapshot = parseSnapshot(await request<Snapshot>(path(mode, parent.qcItemNo)), parent.qcItemNo)
      if (snapshot.issueState !== parent.issueState || snapshot.dataStamp !== parent.dataStamp) parent = await freshItem(parent.qcItemNo)
      if (snapshot.issueState !== parent.issueState || snapshot.dataStamp !== parent.dataStamp) throw new Error('主檔正在更新，請重新開啟明細。')
    }
    if (epoch !== relationEpoch || !relationOpen.value) return
    relationOwner.value = parent; relationSnapshot.value = snapshot; relationRows.value = clone(snapshot.items)
    relationSelectedNos.value = mode === 'reasons' ? (relationRows.value as ReasonMapRow[]).map(x => x.reasonNo) : []
    relationReady.value = true; upsert(parent); originalRelation.value = relationState()
    if (mode === 'reasons') {
      if (mock.value) {
        relationCandidates.value = [{ reasonNo: 'DEV-RSN-01', reasonName: 'DEV 外觀破損', reasonType: '1', reasonLevel: '1', issueState: 2 }]
        reasonTypeOptions.value = [{ value: '1', label: 'DEV Defect' }]
      } else {
        try {
          const candidates = listOf(await request<ListPayload<Record<string, unknown>>>(path('options/reasons', undefined, { issueState: '2' })))
          if (epoch === relationEpoch) relationCandidates.value = candidates.map(x => ({ reasonNo: String(x.reasonNo), reasonName: String(x.reasonName), reasonType: String(x.reasonType), reasonLevel: String(x.reasonLevel), issueState: Number(x.issueState) }))
        } catch (error) { if (epoch === relationEpoch) relationNotice.value = failure(error) }
        try {
          const types = listOf(await request<ListPayload<ValueOption>>(path('options/reason-types')))
          if (epoch === relationEpoch) reasonTypeOptions.value = types
        } catch { if (epoch === relationEpoch) { reasonTypeOptions.value = []; reasonTypesUnavailable.value = '原因類別篩選暫不可用。' } }
      }
    } else if (mode === 'factors') {
      try {
        const choices = mock.value ? [{ factorNo: 'Appearance', factorName: 'DEV 外觀', parameterType: 2, freeInput: true }, { factorNo: 'Dimension', factorName: 'DEV 尺寸', parameterType: 2, freeInput: true }] : listOf(await request<ListPayload<FactorChoice>>(path('options/factors')))
        if (epoch === relationEpoch) { factorChoices.value = choices; factorChoicesLoaded.value = true }
      } catch (error) { if (epoch === relationEpoch) relationNotice.value = failure(error) }
    } else if (mode === 'operations') {
      try {
        const choices = mock.value ? [{ opNo: 'DEV-OP-PACK', opName: 'DEV 包裝', inline: 0, executeOrder: 0, qcItemType: 0, inspTypeFai: null, inspTypeIpqc: null, inspTypePqc: null, reviser: null, reviseDate: null }] : listOf(await request<ListPayload<OperationRow>>(path('options/operations', undefined, { qcType: '0', lotIsValid: String(parent.lotIsValid) })))
        if (epoch === relationEpoch) {
          operationChoices.value = clone(choices)
          operationCandidates.value = choices.filter(x => !(relationRows.value as OperationRow[]).some(row => sameKey(row.opNo, x.opNo)))
        }
      } catch (error) { if (epoch === relationEpoch) relationNotice.value = failure(error) }
    }
  } catch (error) {
    if (epoch === relationEpoch) { relationRows.value = []; relationSnapshot.value = null; relationReady.value = false; relationError.value = failure(error) }
  } finally { if (epoch === relationEpoch) relationLoading.value = false }
}

function moveReason(reasonNo: string, selected: boolean): void {
  if (!relationWritable.value || busy.value) return
  if (selected) {
    if (reasonSelected(reasonNo)) return
    const matches = relationCandidates.value.filter(x => sameKey(x.reasonNo, reasonNo))
    if (matches.length !== 1) { relationError.value = '原因選項含重複或失效編號，請重新查詢。'; return }
    const row = matches[0]!
    relationSelectedNos.value = [...relationSelectedNos.value, row.reasonNo]
    if (!(relationRows.value as ReasonMapRow[]).some(x => sameKey(x.reasonNo, row.reasonNo))) relationRows.value.push({ qcItemNo: relationOwner.value!.qcItemNo, reasonNo: row.reasonNo, reasonName: row.reasonName, creator: null, createDate: null, reviser: null, reviseDate: null })
  } else {
    relationSelectedNos.value = relationSelectedNos.value.filter(x => !sameKey(x, reasonNo))
    relationRows.value = (relationRows.value as ReasonMapRow[]).filter(x => !sameKey(x.reasonNo, reasonNo))
  }
}
function toggleOperationCandidate(no: string, checked: boolean): void { if (!canEditOperations.value || busy.value) return; operationCandidateNos.value = checked ? operationCandidateNos.value.some(x => sameKey(x, no)) ? operationCandidateNos.value : [...operationCandidateNos.value, no] : operationCandidateNos.value.filter(x => !sameKey(x, no)) }
function toggleSelectedOperation(no: string, checked: boolean): void { if (!canEditOperations.value || busy.value) return; selectedOperationNos.value = checked ? selectedOperationNos.value.some(x => sameKey(x, no)) ? selectedOperationNos.value : [...selectedOperationNos.value, no] : selectedOperationNos.value.filter(x => !sameKey(x, no)) }
function moveSelectedOperations(inline: 0 | 1): void {
  if (!canEditOperations.value || busy.value) return
  if (inline === 1 && relationOwner.value?.lotIsValid !== 1) { relationError.value = '只有LotIsValid=1可以加入InLine。'; return }
  const moving: OperationRow[] = []
  for (const no of operationCandidateNos.value) {
    if ((relationRows.value as OperationRow[]).some(row => sameKey(row.opNo, no)) || moving.some(row => sameKey(row.opNo, no))) continue
    const matches = operationCandidates.value.filter(row => sameKey(row.opNo, no))
    if (matches.length !== 1) { relationError.value = '作業站選項含重複或失效編號，請重新查詢。'; return }
    moving.push(matches[0]!)
  }
  relationRows.value.push(...moving.map(x => ({ ...x, inline, executeOrder: 0, qcItemType: 0, inspTypeFai: false, inspTypeIpqc: false, inspTypePqc: false })))
  operationCandidates.value = operationCandidates.value.filter(x => !moving.some(row => sameKey(row.opNo, x.opNo))); operationCandidateNos.value = []
}
function removeSelectedOperations(): void {
  if (!canEditOperations.value || busy.value) return
  const rows = relationRows.value as OperationRow[]
  const removed = rows.filter(row => selectedOperationNos.value.some(no => sameKey(row.opNo, no)))
  const remaining = rows.filter(row => !selectedOperationNos.value.some(no => sameKey(row.opNo, no)))
  // Restore only actual approved lookup choices. A removed legacy/orphan row
  // does not become an eligible new assignment merely by moving to the left.
  const available = operationChoices.value.filter(choice => removed.some(row => sameKey(row.opNo, choice.opNo)) && !remaining.some(row => sameKey(row.opNo, choice.opNo)) && !operationCandidates.value.some(row => sameKey(row.opNo, choice.opNo)))
  operationCandidates.value.push(...clone(available)); relationRows.value = remaining; selectedOperationNos.value = []
}
function addParameter(): void { if (!relationWritable.value || busy.value || !newParameterNo.value.trim()) return; (relationRows.value as ParameterRow[]).push({ parameterNo: newParameterNo.value.trim(), parameterValue: '' }); newParameterNo.value = '' }
function addFactor(): void {
  if (!relationWritable.value || busy.value || !factorChoicesLoaded.value) return
  const choice = factorChoices.value.find(x => x.factorNo === newFactorNo.value)
  if (!choice) { relationError.value = '請選擇已核准的要因。'; return }
  ;(relationRows.value as FactorRow[]).push({ factorNo: choice.factorNo, factorName: choice.factorName, parameterType: choice.parameterType, metadataAvailable: true, factorOnline: 'Y', factorSpec: 'Y', opNo: null, tableName: null, fieldName: null, factorShortRun: 'N' }); newFactorNo.value = ''
}
function addPlanChange(): void { if (!relationWritable.value || busy.value) return; (relationRows.value as SamplePlanChangeRow[]).push({ oldSamplePlan: relationOwner.value?.planNo ?? '', accLotQty: -1, rejLotQty: -1, newSamplePlan: null, factorNo: '*', lotRange: -1, aqlChkForSPlanChg: 0, qcItemType: 0 }) }
function removeRelationRow(index: number): void { if (relationWritable.value && !busy.value) relationRows.value.splice(index, 1) }

async function saveRelation(): Promise<void> {
  const owner = relationOwner.value, snapshot = relationSnapshot.value, kind = relationMode.value
  if (!owner || !snapshot || kind === 'simulation' || !relationWritable.value || busy.value || closing.value || confirmationMessage.value !== null) return
  busy.value = true; relationError.value = ''
  try {
    if (!await requestConfirmation('確定儲存這份設定？')) return
    await focusDialog('.relation-dialog')
    const rows = clone(kind === 'reasons' ? relationSelectedNos.value.map(reasonNo => ({ reasonNo })) : relationRows.value)
    let saved: Snapshot
    if (mock.value) {
      const store = readStore(), parent = store.items.find(x => x.qcItemNo === owner.qcItemNo)
      if (!parent || parent.dataStamp !== snapshot.dataStamp) throw new Error('主檔已變更。')
      ;(store[kind] as Record<string, unknown[]>)[owner.qcItemNo] = clone(relationRows.value)
      parent.dataStamp = (parent.dataStamp ?? 0) + 1; parent.reviser = props.currentUserNo || 'DEV001'; parent.reviseDate = new Date().toISOString(); saveStore(store)
      saved = { ...snapshot, dataStamp: parent.dataStamp, items: clone(relationRows.value), version: JSON.stringify({ parent, rows }) }
    } else saved = parseSnapshot(await request<Snapshot>(path(kind, owner.qcItemNo), { method: 'PUT', body: JSON.stringify({ issueState: snapshot.issueState, dataStamp: snapshot.dataStamp, version: snapshot.version, rows }) }), owner.qcItemNo)
    relationSnapshot.value = saved; relationRows.value = clone(saved.items)
    relationSelectedNos.value = kind === 'reasons' ? (saved.items as ReasonMapRow[]).map(x => x.reasonNo) : []
    relationOwner.value = { ...owner, issueState: saved.issueState, dataStamp: saved.dataStamp }
    const listRow = items.value.find(x => x.qcItemNo === owner.qcItemNo)
    if (listRow) { listRow.issueState = saved.issueState; listRow.dataStamp = saved.dataStamp }
    originalRelation.value = relationState(); relationNotice.value = '設定已儲存。'
    try {
      const fresh = await freshItem(owner.qcItemNo); upsert(fresh)
      if (fresh.dataStamp === saved.dataStamp && fresh.issueState === saved.issueState) relationOwner.value = fresh
      else { relationReady.value = false; relationNotice.value += ' 主檔已有其他變更，請重新開啟後再編輯。' }
      await reloadItems(owner.qcItemNo)
    } catch (error) { relationNotice.value += ' 主檔清單更新失敗，請重新查詢。' + failure(error) }
  } catch (error) { relationError.value = failure(error) + ' 請重新查詢確認目前資料。' }
  finally { busy.value = false; if (relationOpen.value) await focusDialog('.relation-dialog') }
}

async function tryClose(target: 'editor' | 'relation' | 'page'): Promise<void> {
  if (busy.value || closing.value || confirmationMessage.value !== null) return
  closing.value = true
  try {
    const dirty = editorOpen.value ? editorState() !== originalEditor.value : relationOpen.value && relationReady.value ? relationState() !== originalRelation.value : false
    if (dirty && !await requestConfirmation('目前資料尚未儲存，確定放棄變更？')) return
    if (target === 'editor' || target === 'page') { editorOpen.value = false; ++editorEpoch }
    if (target === 'relation' || target === 'page') { relationOpen.value = false; relationReady.value = false; ++relationEpoch }
    if (target === 'page') emit('close')
  } finally {
    closing.value = false
    if (editorOpen.value) await focusDialog('.editor-dialog')
    else if (relationOpen.value) await focusDialog('.relation-dialog')
  }
}
function closeEditor(): Promise<void> { return tryClose('editor') }
function closeRelation(): Promise<void> { return tryClose('relation') }
function closePage(): Promise<void> { return tryClose('page') }
const editorDialog = ref<HTMLElement | null>(null), relationDialog = ref<HTMLElement | null>(null)
async function focusDialog(selector: string): Promise<void> {
  await nextTick()
  const dialog = selector === '.editor-dialog' ? editorDialog.value : relationDialog.value
  if (!dialog) return
  const first = [...dialog.querySelectorAll<HTMLElement>('input:not(:disabled),select:not(:disabled),textarea:not(:disabled),button:not(:disabled)')].find(element => element.offsetParent !== null)
  ;(first ?? dialog).focus()
}
function trapTab(event: KeyboardEvent): void {
  const elements = [...(event.currentTarget as HTMLElement).querySelectorAll<HTMLElement>('input:not(:disabled),select:not(:disabled),textarea:not(:disabled),button:not(:disabled),[tabindex="0"]')].filter(x => x.offsetParent !== null)
  const first = elements[0], last = elements.at(-1)
  if (!first || !last) { event.preventDefault(); return }
  if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus() }
  else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus() }
}
onBeforeUnmount(() => { ++listEpoch; ++editorEpoch; ++relationEpoch })
onMounted(loadItems)
</script>


<template>
  <main class="attribute-qc-page">
    <div class="master-surface" :inert="busy || closing || editorOpen || relationOpen || confirmationMessage !== null">
    <header class="page-title"><h1>計數品管項目</h1><span v-if="mock" class="dev-badge">DEV 假資料</span></header>
    <section class="query-bar">
      <label v-for="state in [{value:0,label:'未簽核'},{value:1,label:'簽核中'},{value:2,label:'已簽核'},{value:-1,label:'不使用'}]" :key="state.value" class="state-option"><input v-model="issueState" type="radio" :disabled="blocked" :value="state.value" @change="loadItems"><span>{{ state.label }}</span></label>
      <label class="query-label" for="item-search">QCItemNo</label><input id="item-search" v-model="searchText" class="query-input" :disabled="blocked" @keyup.enter="loadItems"><button class="win-button query-button" type="button" :disabled="blocked" @click="loadItems">查詢</button>
    </section>
    <div class="main-area">
      <section class="grid-panel"><div class="grid-scroll"><table class="main-grid"><thead><tr><th class="check-col"></th><th>QCItemNo</th><th>QCItemName</th><th>LotIsValid</th><th>ComponentCollect</th><th>PlanNo</th><th>IsSampleSizeInteger</th><th>RuleNo</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead><tbody>
        <tr v-for="item in visibleItems" :key="item.qcItemNo" :class="{ selected: activeNo === item.qcItemNo }" @click="selectRow(item, $event)"><td class="check-col"><input type="checkbox" :checked="selectedNos.includes(item.qcItemNo)" :disabled="blocked" @click.stop @change="toggleSelected(item, ($event.target as HTMLInputElement).checked)"></td><td>{{ item.qcItemNo }}</td><td>{{ item.qcItemName }}</td><td>{{ valueLabel(item.lotIsValid) }}</td><td>{{ valueLabel(item.componentCollect) }}</td><td>{{ item.planNo }}</td><td>{{ sampleSizeModeLabel(item.isSampleSizeInteger) }}</td><td>{{ item.ruleNo }}</td><td>{{ item.creator }}</td><td>{{ displayDate(item.createDate) }}</td><td>{{ item.reviser }}</td><td>{{ displayDate(item.reviseDate) }}</td></tr>
        <tr v-if="!visibleItems.length"><td colspan="12" class="empty-row">{{ loading ? '載入中…' : '查無資料' }}</td></tr>
      </tbody></table></div><footer class="grid-footer">資料總筆數 {{ visibleItems.length }}　目前狀態：{{ stateLabel(issueState) }}</footer>
      </section>
      <aside class="side-actions">
        <button class="win-button" type="button" :disabled="blocked" @click="openEditor('add')">ADD (&amp;A)</button><button class="win-button" type="button" :disabled="blocked || activeItem?.issueState!==0" @click="openEditor('edit', activeItem)">EDIT (&amp;E)</button><button class="win-button" type="button" :disabled="blocked || !targets.length || !mock" :title="mock ? '' : '刪除與停用尚未開放'" @click="deleteSelected">DEL (&amp;D)</button><button class="win-button" type="button" :disabled="blocked || !activeItem || activeItem.issueState===null || activeItem.issueState===1" @click="openEditor('copy', activeItem)">Copy (&amp;C)</button><button class="win-button" type="button" :disabled="blocked || !targets.length || targets.some(row=>row.issueState!==0)" @click="approveSelected">Approve</button>
        <button class="win-button" type="button" :disabled="blocked || !activeItem" @click="openRelation('parameters')">Parameter</button><button class="win-button" type="button" :disabled="blocked || !activeItem" @click="openRelation('factors')">Factor</button><button class="win-button" type="button" :disabled="blocked || !activeItem" @click="openRelation('reasons')">Reason</button><button class="win-button" type="button" :disabled="blocked || !activeItem" @click="openRelation('simulation')">Simulation</button><button class="win-button" type="button" :disabled="blocked || !activeItem" @click="openRelation('operations')">Op</button><button class="win-button" type="button" :disabled="blocked || !activeItem" @click="openRelation('samplePlanChanges')">SamplePlanChange</button>
        <button class="win-button close-page" type="button" :disabled="busy || closing || confirmationMessage !== null" @click="closePage">Close (&amp;X)</button>
      </aside>
    </div>
    <p v-if="errorMessage" class="message error" role="alert">{{ errorMessage }}</p><p v-if="notice" class="message notice" role="status">{{ notice }}</p>
    </div>

    <div v-if="editorOpen" class="backdrop" @click.self="closeEditor"><section ref="editorDialog" class="dialog editor-dialog" role="dialog" aria-modal="true" aria-label="計數品管項目維護" tabindex="-1" :inert="confirmationMessage !== null" @keydown.esc.stop.prevent="closeEditor" @keydown.tab="trapTab">
      <header class="dialog-title"><h2>QCItem For Attributes Define ({{ editorMode.toUpperCase() }})</h2><span v-if="mock" class="dev-badge">DEV</span></header>
      <fieldset class="dialog-content" :disabled="busy || closing || editorOptionsLoading || confirmationMessage !== null">
      <div class="form-sections"><section class="form-group identity-group"><h3>QC Item</h3>
        <label class="field"><span>QCItemNo</span><input v-model="form.qcItemNo" :readonly="editorMode==='edit'" maxlength="25"></label>
        <label class="field"><span>QCItem Name</span><input v-model="form.qcItemName" maxlength="500"></label>
        <label class="field"><span>Plan No</span><div class="input-action"><select v-model="form.planNo"><option value="">請選擇</option><option v-if="form.planNo && !planChoiceList.includes(form.planNo)" :value="form.planNo">{{ form.planNo }}</option><option v-for="option in planChoiceList" :key="option">{{ option }}</option></select><button class="win-button small-button" type="button" @click="form.planNo=planChoiceList[0] || ''">...</button></div></label>
        <label class="field"><span>Rule No</span><div class="input-action"><select v-model="form.ruleNo"><option value="">—</option><option v-if="form.ruleNo && !ruleChoiceList.includes(form.ruleNo)" :value="form.ruleNo">{{ form.ruleNo }}</option><option v-for="option in ruleChoiceList" :key="option">{{ option }}</option></select><button class="win-button small-button" type="button" @click="form.ruleNo=ruleChoiceList[0] || ''">...</button></div></label>
        <label class="field"><span>Special Form Name</span><select v-model="form.formName"><option value="">—</option><option v-if="form.formName && !interfaceChoiceList.includes(form.formName)" :value="form.formName">{{ form.formName }}</option><option v-for="option in interfaceChoiceList" :key="option">{{ option }}</option></select><small v-if="!mock && lookupAvailability.interfaces?.available===false">介面選項暫不可用。</small></label>
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
      <fieldset v-if="editorMode==='copy' && copyFlags.parameters && copyPreview?.parametersDiffer" class="radio-field"><legend>參數來源</legend><label><input v-model="parameterSource" type="radio" value="item"> 來源品管項目 {{ editingOriginal?.qcItemNo }}</label><label><input v-model="parameterSource" type="radio" value="plan"> 目標抽樣計劃 {{ form.planNo }}</label></fieldset>
      </fieldset>
      <p v-if="editorError" class="message error">{{ editorError }}</p>
      <p v-if="editorNotice" class="message notice" role="status">{{ editorNotice }}</p>
      <div class="audit-row"><span>建立人 {{ currentEditorAudit.creator }}</span><span>建立日期 {{ currentEditorAudit.createDate }}</span><span>修改人 {{ currentEditorAudit.reviser }}</span><span>修改日期 {{ currentEditorAudit.reviseDate }}</span></div>
      <footer class="dialog-actions"><span class="hint">儲存後更新清單。</span><button class="win-button primary" type="button" :disabled="busy || closing || editorOptionsLoading || !editorOptionsLoaded" @click="saveItem(true)">確定</button><button class="win-button" type="button" :disabled="busy || closing" @click="closeEditor">關閉</button><button class="win-button" type="button" :disabled="busy || closing || editorOptionsLoading || !editorOptionsLoaded" @click="saveItem(false)">套用</button></footer>
    </section></div>

    <div v-if="relationOpen" class="backdrop" @click.self="closeRelation"><section ref="relationDialog" class="dialog relation-dialog" role="dialog" aria-modal="true" :aria-label="relationTitle" tabindex="-1" :inert="confirmationMessage !== null" @keydown.esc.stop.prevent="closeRelation" @keydown.tab="trapTab">
      <header class="dialog-title"><h2>{{ relationTitle }} — {{ relationOwner?.qcItemNo }}</h2><span>{{ relationOwner?.qcItemName }}</span></header>
      <p v-if="relationLoading" class="hint" role="status">載入中…</p><p v-else-if="relationSnapshot && !relationWritable" class="hint">{{ relationSnapshot.writeUnavailableReason || '目前唯讀。' }}</p>
      <fieldset class="dialog-content" :disabled="busy || closing || relationLoading || !relationWritable || confirmationMessage !== null">
      <template v-if="relationMode==='parameters'"><div class="inline-add"><input v-model="newParameterNo" placeholder="ParameterNo"><button class="win-button" type="button" @click="addParameter">新增參數</button></div><div class="relation-table-wrap"><table><thead><tr><th>ParameterNo</th><th>ParameterValue</th><th></th></tr></thead><tbody><tr v-for="(row,index) in relationRows as ParameterRow[]" :key="row.parameterNo"><td>{{ row.parameterNo }}</td><td><input v-model="row.parameterValue"></td><td><button class="link-button" type="button" @click="removeRelationRow(index)">移除</button></td></tr><tr v-if="!relationRows.length"><td colspan="3" class="empty-row">尚未設定參數</td></tr></tbody></table></div></template>
      <template v-else-if="relationMode==='factors'"><div class="inline-add"><select v-model="newFactorNo" :disabled="!factorChoicesLoaded"><option value="">請選擇已核准要因</option><option v-for="choice in factorChoices" :key="choice.factorNo" :value="choice.factorNo">{{ choice.factorNo }} — {{ choice.factorName }}</option></select><button class="win-button" type="button" :disabled="!factorChoicesLoaded || !newFactorNo" @click="addFactor">新增要因</button></div><p v-if="factorChoicesLoaded && !factorChoices.length" class="hint">尚無已核准的要因。</p><div class="relation-table-wrap"><table><thead><tr><th>FactorNo</th><th>Online</th><th>Spec</th><th>OPNo</th><th>TableName</th><th>FieldName</th><th>Short Run</th><th></th></tr></thead><tbody><tr v-for="(row,index) in relationRows as FactorRow[]" :key="row.factorNo"><td><input v-model="row.factorNo" readonly><small v-if="row.metadataAvailable===false">目錄資料暫不可用</small></td><td><select v-model="row.factorOnline"><option>Y</option><option>N</option></select></td><td><select v-model="row.factorSpec"><option>Y</option><option>N</option></select></td><td><input v-model="row.opNo"></td><td><input v-model="row.tableName"></td><td><input v-model="row.fieldName"></td><td><select v-model="row.factorShortRun"><option>N</option><option>Y</option></select></td><td><button class="link-button" type="button" @click="removeRelationRow(index)">移除</button></td></tr><tr v-if="!relationRows.length"><td colspan="8" class="empty-row">尚未設定要因</td></tr></tbody></table></div></template>
      <template v-else-if="relationMode==='reasons'"><div class="reason-filters"><input v-model="relationSearch" placeholder="ReasonNo / ReasonName"><select v-model="relationTypeFilter" :disabled="!mock && !!reasonTypesUnavailable"><option value="">全部 ReasonType</option><option v-for="option in reasonTypeOptions" :key="option.value" :value="option.value">{{ option.label }}</option></select><input v-model="relationLevelFilter" placeholder="ReasonLevel"></div><p v-if="reasonTypesUnavailable" class="hint">{{ reasonTypesUnavailable }}</p><div class="reason-dual"><section><h3>UnSelected</h3><div class="relation-table-wrap"><table><tbody><tr v-for="row in filteredCandidates" :key="row.reasonNo"><td><input type="checkbox" :checked="reasonSelected(row.reasonNo)" @change="moveReason(row.reasonNo, ($event.target as HTMLInputElement).checked)"></td><td>{{ row.reasonNo }}</td><td>{{ row.reasonName }}</td><td>{{ row.reasonLevel }}</td></tr></tbody></table></div></section><div class="move-count">{{ relationSelectedNos.length }} selected</div><section><h3>Selected</h3><div class="relation-table-wrap"><table><tbody><tr v-for="row in (relationRows as ReasonMapRow[]).filter(item=>reasonSelected(item.reasonNo))" :key="row.reasonNo"><td><input type="checkbox" checked @change="moveReason(row.reasonNo,false)"></td><td>{{ row.reasonNo }}</td><td>{{ row.reasonName }}</td></tr><tr v-if="!relationSelectedNos.length"><td class="empty-row">尚未設定原因</td></tr></tbody></table></div></section></div></template>
      <template v-else-if="relationMode==='samplePlanChanges'"><div class="inline-add"><button class="win-button" type="button" @click="addPlanChange">新增轉換條件</button></div><div class="relation-table-wrap"><table><thead><tr><th>Old Plan</th><th>Accept</th><th>Reject</th><th>New Plan</th><th>FactorNo</th><th>LotRange</th><th>AQL Check</th><th></th></tr></thead><tbody><tr v-for="(row,index) in relationRows as SamplePlanChangeRow[]" :key="`${row.oldSamplePlan}-${index}`"><td><input v-model="row.oldSamplePlan"></td><td><input v-model.number="row.accLotQty" type="number"></td><td><input v-model.number="row.rejLotQty" type="number"></td><td><input v-model="row.newSamplePlan"></td><td><input v-model="row.factorNo"></td><td><input v-model.number="row.lotRange" type="number"></td><td><select v-model.number="row.aqlChkForSPlanChg"><option :value="0">否</option><option :value="1">是</option></select></td><td><button class="link-button" type="button" @click="removeRelationRow(index)">移除</button></td></tr><tr v-if="!relationRows.length"><td colspan="8" class="empty-row">尚未設定抽樣轉換</td></tr></tbody></table></div></template>
      <template v-else-if="relationMode==='operations'"><div class="operation-dual"><section class="operation-list"><h3>未選作業站</h3><div class="relation-table-wrap"><table><thead><tr><th></th><th>OPNo</th><th>OPName</th></tr></thead><tbody><tr v-for="row in operationCandidates" :key="row.opNo"><td><input type="checkbox" :checked="operationCandidateNos.some(no=>sameKey(no,row.opNo))" @change="toggleOperationCandidate(row.opNo, ($event.target as HTMLInputElement).checked)"></td><td>{{ row.opNo }}</td><td>{{ row.opName }}</td></tr><tr v-if="!operationCandidates.length"><td colspan="3" class="empty-row">沒有可加入的作業站</td></tr></tbody></table></div></section><div class="operation-moves"><button class="win-button" type="button" :disabled="!canEditOperations || !operationCandidateNos.length || relationOwner?.lotIsValid!==1" @click="moveSelectedOperations(1)">加入 InLine →</button><button class="win-button" type="button" :disabled="!canEditOperations || !operationCandidateNos.length" @click="moveSelectedOperations(0)">加入 OffLine →</button><button class="win-button" type="button" :disabled="!canEditOperations || !selectedOperationNos.length" @click="removeSelectedOperations">← 移除</button></div><section class="operation-list assigned-list"><h3>已選作業站</h3><div class="relation-table-wrap"><table><thead><tr><th></th><th>Type</th><th>OPNo</th><th>OPName</th><th>ExecuteOrder</th><th>FAI</th><th>IPQC</th><th>PQC</th></tr></thead><tbody><tr v-for="row in relationRows as OperationRow[]" :key="row.opNo"><td><input type="checkbox" :checked="selectedOperationNos.some(no=>sameKey(no,row.opNo))" @change="toggleSelectedOperation(row.opNo, ($event.target as HTMLInputElement).checked)"></td><td>{{ row.inline===1 ? 'InLine' : 'OffLine' }}</td><td>{{ row.opNo }}</td><td>{{ row.opName }}</td><td><input v-model.number="row.executeOrder" type="number" :disabled="!canEditOperations"></td><td><input v-model="row.inspTypeFai" type="checkbox" :disabled="!canEditOperations || row.inline!==1"></td><td><input v-model="row.inspTypeIpqc" type="checkbox" :disabled="!canEditOperations || row.inline!==1"></td><td><input v-model="row.inspTypePqc" type="checkbox" :disabled="!canEditOperations || row.inline!==1"></td></tr><tr v-if="!relationRows.length"><td colspan="8" class="empty-row">尚未設定所屬作業站</td></tr></tbody></table></div></section></div><p v-if="!canEditOperations" class="hint">只有已簽核的品管項目可以修改作業站。</p><p v-else-if="relationOwner?.lotIsValid===0" class="hint">LotIsValid=0，無法加入 InLine 作業站。</p></template>
      <template v-else><div class="relation-table-wrap"><table><thead><tr><th>Parameter</th><th>Value</th></tr></thead><tbody><tr v-for="(row,index) in simulationRows" :key="index"><td>{{ row.ParameterNo ?? row.parameterNo }}</td><td>{{ row.ParameterValue ?? row.parameterValue }}</td></tr><tr v-if="!simulationRows.length"><td colspan="2" class="empty-row">沒有模擬結果</td></tr></tbody></table></div></template>
      </fieldset>
      <p v-if="relationError" class="message error" role="alert">{{ relationError }}</p><p v-if="relationNotice" class="message notice" role="status">{{ relationNotice }}</p>
      <footer class="dialog-actions"><span class="hint">{{ relationMode==='reasons' ? '只可加入已核准原因。' : relationMode==='operations' ? 'InLine 作業站至少選一種檢驗時機。' : '' }}</span><button v-if="relationMode!=='simulation'" class="win-button primary" type="button" :disabled="busy || closing || !relationWritable" @click="saveRelation">確定</button><button class="win-button" type="button" :disabled="busy || closing" @click="closeRelation">關閉</button></footer>
    </section></div>
    <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
  </main>
</template>

<style scoped>
.attribute-qc-page{display:flex;flex-direction:column;gap:6px;height:100%;min-height:0;background:#F7F5EE;color:var(--ink,#26251F);font:13px/1.35 "Segoe UI","Microsoft JhengHei",sans-serif}.page-title{display:flex;align-items:center;justify-content:space-between;padding:2px 10px}.page-title h1{margin:0;font-size:20px}.dev-badge{padding:3px 8px;border:1px solid #d7a044;background:#fff1bd;color:#76551a;font-size:11px}.query-bar{display:flex;align-items:center;gap:12px;min-height:38px;padding:4px 12px;border-block:1px solid #d4d0c8;background:#fff}.state-option{display:flex;align-items:center;gap:4px;white-space:nowrap}.state-option input,.radio-field input{accent-color:var(--yellow,#E7C600)}.query-label{margin-left:7px;white-space:nowrap}.query-input{width:min(240px,25vw);height:27px;border:1px solid #aaa69e;padding:3px 6px}.win-button{min-height:30px;padding:4px 9px;border:1px solid #bdb9b0;border-radius:3px;background:linear-gradient(#fff,#efeeeb);color:#343331;font:inherit;white-space:nowrap;cursor:pointer}.win-button:hover:not(:disabled){border-color:var(--yellow,#E7C600);background:#fff6df}.win-button:disabled{opacity:.48;cursor:not-allowed}.query-button{min-width:54px}.main-area{display:grid;grid-template-columns:minmax(0,1fr) 88px;gap:6px;flex:1;min-height:0;padding:0 6px}.grid-panel{display:flex;flex-direction:column;gap:4px;min-width:0;min-height:0;padding:5px;border:1px solid var(--line);background:#fff}.grid-scroll{flex:1;min-height:0;overflow:auto;border:1px solid #bbb8b1}.main-grid{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}.main-grid th{position:sticky;top:0;z-index:2;padding:6px;border:1px solid #c5c1b9;background:#eeeae0;text-align:left}.main-grid td{height:26px;padding:3px 6px;border:1px solid #d1cec8;background:#fff}.main-grid tbody tr{cursor:pointer}.main-grid tbody tr:hover td{background:#fff8e2}.main-grid tbody tr.selected td{background:#ffedb1}.check-col{width:26px;text-align:center!important}.check-col input{margin:0}.empty-row{padding:16px!important;text-align:center;color:var(--muted)}.grid-footer{height:18px;padding:0 3px;color:#777;font-size:11px}.side-actions{display:flex;flex-direction:column;gap:7px;overflow:auto}.side-actions .win-button{width:100%;min-height:32px;padding:4px 5px;font-size:11px}.close-page{margin-top:auto}.message{margin:0 8px;padding:7px 9px;border:1px solid}.error{border-color:#d99b86;background:#fff0e9;color:#873820}.notice{border-color:#d8c79a;background:#fff8dc;color:#65541d}.backdrop{position:fixed;inset:0;z-index:1200;display:flex;align-items:center;justify-content:center;padding:16px;background:#17171770}.dialog{display:flex;flex-direction:column;gap:8px;max-height:94vh;padding:12px;border:1px solid #a9a59e;background:#F7F5EE;box-shadow:0 9px 30px #0004}.editor-dialog{width:min(662px,96vw);min-height:min(527px,94vh)}.relation-dialog{width:min(940px,96vw);height:min(600px,92vh)}.dialog-title{display:flex;align-items:center;justify-content:space-between;min-height:28px;border-bottom:1px solid var(--line)}.dialog-title h2{margin:0;font-size:17px}.dialog-title>span{color:var(--muted);font-size:12px}.form-sections{display:grid;grid-template-columns:1.2fr .8fr;gap:8px;flex:1;min-height:0}.form-group{display:flex;flex-direction:column;gap:7px;padding:8px;border:1px solid var(--line);background:#fff}.form-group h3{margin:0 0 3px;font-size:13px}.field{display:grid;grid-template-columns:108px minmax(0,1fr);align-items:center;gap:6px}.field>span{text-align:right;color:#555}.field input,.field select,.field textarea,.inline-add input,.relation-table-wrap input,.relation-table-wrap select,.reason-filters input,.reason-filters select{box-sizing:border-box;min-width:0;width:100%;height:27px;padding:3px 6px;border:1px solid #aaa69e;background:#fff;color:#333;font:inherit}.field textarea{height:auto;resize:vertical}.field input:read-only{background:#f0efed}.wide-field{grid-column:1/-1;align-items:start}.wide-field>span{padding-top:5px}.input-action{display:flex;gap:4px}.input-action select{flex:1}.small-button{min-width:30px;padding-inline:6px}.radio-field{display:flex;align-items:center;gap:9px;margin:0;padding:5px 7px;border:1px solid #d2cfc8}.radio-field legend{padding:0 4px;color:#555}.radio-field label{display:flex;align-items:center;gap:3px;white-space:nowrap}.options-group .field{grid-template-columns:100px minmax(0,1fr)}.options-group .field small{grid-column:2;color:#8b6b31;font-size:10px}.copy-options{display:flex;flex-wrap:wrap;align-items:center;gap:12px;padding:7px;border:1px solid var(--line);background:#fff}.copy-options label{display:flex;align-items:center;gap:3px}.audit-row{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:2px 14px;padding:6px;border:1px solid #c9c5bc;background:#f4f2ed;color:#666;font-size:11px}.dialog-actions{display:flex;align-items:center;justify-content:flex-end;gap:6px;min-height:34px}.hint{flex:1;color:#69665e;font-size:10px}.primary{background:#3B3930;color:white}.relation-table-wrap{flex:1;min-height:0;overflow:auto;border:1px solid #bbb8b1;background:white}.relation-table-wrap table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}.relation-table-wrap th{position:sticky;top:0;padding:6px;border:1px solid #c6c2ba;background:#eeeae0;text-align:left}.relation-table-wrap td{padding:4px 6px;border:1px solid #d1cec8}.relation-table-wrap td input,.relation-table-wrap td select{min-width:70px;height:25px}.inline-add{display:flex;gap:6px;max-width:420px}.inline-add input{flex:1}.link-button{border:0;background:transparent;color:#8a5a13;text-decoration:underline;cursor:pointer}.reason-filters{display:grid;grid-template-columns:1fr 180px 110px;gap:6px}.reason-dual{display:grid;grid-template-columns:minmax(0,1fr) 96px minmax(0,1fr);gap:6px;flex:1;min-height:0}.reason-dual section{display:flex;flex-direction:column;min-width:0;min-height:0;padding:5px;border:1px solid var(--line);background:#fff}.reason-dual h3{margin:0 0 5px;text-align:center;font-size:13px}.reason-dual .relation-table-wrap{border:0}.reason-dual td:first-child{width:26px;text-align:center}.move-count{display:grid;place-items:center;color:#746f65;font-size:11px}@media(max-width:850px){.query-bar{flex-wrap:wrap}.main-area{grid-template-columns:minmax(0,1fr) 72px}.form-sections{grid-template-columns:1fr;overflow:auto}.editor-dialog{height:92vh}.options-group{display:grid;grid-template-columns:repeat(2,minmax(0,1fr))}.options-group h3{grid-column:1/-1}.field{grid-template-columns:100px minmax(0,1fr)}.relation-dialog{height:92vh}.relation-table-wrap{overflow:auto}}@media(max-width:580px){.reason-dual{grid-template-columns:minmax(0,1fr) 50px minmax(0,1fr)}.reason-filters{grid-template-columns:1fr 1fr}.reason-filters input{grid-column:1/-1}.dialog-actions{flex-wrap:wrap}.hint{flex-basis:100%}}
.query-input,.field input:not(:read-only),.field select,.field textarea,.inline-add input,.relation-table-wrap input:not([type="checkbox"]),.relation-table-wrap select,.reason-filters input,.reason-filters select{background:#fff1bf}.field input:read-only{background:#f0efed}.state-option input,.radio-field input,.check-col input,.reason-dual input[type="checkbox"]{accent-color:var(--yellow,#E7C600)}.audit-row{border-color:#c9c5bc}
.operation-dual{display:grid;grid-template-columns:minmax(0,1fr) 132px minmax(0,1.5fr);gap:7px;flex:1;min-height:0}.operation-list{display:flex;flex-direction:column;min-width:0;min-height:0;padding:5px;border:1px solid var(--line);background:#fff}.operation-list h3{margin:0 0 5px;text-align:center;font-size:13px}.operation-moves{display:flex;flex-direction:column;justify-content:center;gap:7px}.operation-moves .win-button{white-space:normal}.operation-list input[type="checkbox"]{accent-color:var(--yellow,#E7C600)}@media(max-width:850px){.operation-dual{grid-template-columns:1fr;overflow:auto}.operation-moves{flex-direction:row;flex-wrap:wrap}.operation-list{min-height:180px}}
.master-surface{display:flex;flex:1;flex-direction:column;gap:6px;min-height:0}.dialog-content{display:flex;flex:1;flex-direction:column;gap:8px;min-width:0;min-height:0;margin:0;padding:0;border:0}.dialog-content:disabled{opacity:.85}.inline-add select{min-width:0;flex:1;height:27px;border:1px solid var(--line,#E5E1D5);background:#fff;font:inherit}.primary{border-color:#3B3930;background:#3B3930;color:#fff}.primary:hover:not(:disabled){border-color:var(--yellow,#E7C600);background:#3B3930}.attribute-qc-page{background:#F7F5EE;color:var(--ink,#26251F);font-family:"Segoe UI","Microsoft JhengHei",sans-serif}.win-button:focus-visible,input:focus-visible,select:focus-visible,textarea:focus-visible{outline:2px solid var(--yellow,#E7C600);outline-offset:1px}
</style>
