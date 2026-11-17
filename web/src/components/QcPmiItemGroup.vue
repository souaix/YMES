<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type IssueState = 0 | 1 | 2 | -1
type EditMode = 'add' | 'edit' | 'copy'
interface GroupRow {
  groupNo: string; groupName: string; lotIsValid: number; componentCollect: number; planNo: string
  formName: string; webUrl: string; unionErf: number; issueState: IssueState; description: string
  creator: string; createDate: string; reviser: string; reviseDate: string; cursorMode: number
  qcClass: string; getFilePath: string; errorReportName: string; noticeReportName: string; dataStamp: number
}
interface DetailRow { groupNo: string; qcItemNo: string; qcItemName: string; showOrder: number; deductionItem: number }
interface QcItem { qcItemNo: string; qcItemName: string; issueState: number; componentCollect: number }
interface SamplingPlan { planNo: string; planName: string; planType: number; componentCollect: number; issueState: number }
interface Factor { factorNo: string; factorName: string; parameterType: number; options: string[] }
interface DeductionRow {
  deductionSerial: string; groupNo: string; normalTimes: number; deductionTimes: number; curPass: number; curStatus: number
  factorValues: Record<string, string>; creator: string; createDate: string; reviser: string; reviseDate: string; dataStamp: number
}
interface OpRow { opNo: string; opName: string; inline: 0 | 1; qcItemType: number; inspTypeFai: boolean; inspTypeIpqc: boolean; inspTypePqc: boolean }
interface Store { groups: GroupRow[]; details: DetailRow[]; deductions: DeductionRow[]; qcItems: QcItem[]; factors: Factor[]; issueFlowEnabled: boolean; operationBindings?: Record<string, OpRow[]> }
type Panel = '' | 'details' | 'deduction' | 'parameters' | 'operations'

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const basePath = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mockMode = computed(() => import.meta.env.DEV && props.developmentPreview)
const storeKey = 'ymes-dev-mock:qc-pmi-item-group:v1'
const statusOptions: { value: IssueState; label: string }[] = [
  { value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' },
]
const groups = ref<GroupRow[]>([]); const details = ref<DetailRow[]>([]); const deductions = ref<DeductionRow[]>([])
const qcItems = ref<QcItem[]>([]); const factors = ref<Factor[]>([]); const plans = ref<SamplingPlan[]>([])
const selectedGroupNo = ref(''); const selectedGroupNos = ref<string[]>([]); const selectedDetailNos = ref<string[]>([]); const selectedDeductionSerial = ref('')
const issueStateFilter = ref<IssueState>(0); const groupNoInput = ref(''); const groupNoFilter = ref('')
const loading = ref(false); const busy = ref(false); const error = ref(''); const message = ref('')
const panel = ref<Panel>(''); const mode = ref<EditMode>('add'); const formOpen = ref(false); const confirmDelete = ref(false)
const unionPlan = ref(false)
const editableFields = ref<string[]>([])
const editReadOnly = computed(() => mode.value === 'edit' && editor.value.issueState === 1)
const canSaveGroup = computed(() => mode.value !== 'edit' || editor.value.issueState === 0 || (!editReadOnly.value && editableFields.value.length > 0))
const editor = ref<GroupRow>(blankGroup()); const deductionEditor = ref<DeductionRow>(blankDeduction());
const draftDetails = ref<DetailRow[]>([]); const availableItems = ref<QcItem[]>([]); const selectedAvailable = ref<string[]>([]); const selectedDraft = ref<string[]>([]); const itemSearch = ref('')
const parameterRows = ref<Array<{ parameterNo: string; parameterName: string; value: string }>>([])
const opCandidates = ref<Array<{ opNo: string; opName: string }>>([])
const operationRows = ref<OpRow[]>([])
const selectedCandidateNos = ref<string[]>([])
const selectedOperationNos = ref<string[]>([])
const availableOperations = computed(() => opCandidates.value.filter(row => !operationRows.value.some(item => item.opNo === row.opNo)))
const assignedOperations = computed(() => operationRows.value.slice().sort((a, b) => a.inline - b.inline || a.opNo.localeCompare(b.opNo)))

const filteredGroups = computed(() => groups.value.filter(row => row.issueState === issueStateFilter.value)
  .filter(row => !groupNoFilter.value || row.groupNo.toLowerCase().includes(groupNoFilter.value.trim().toLowerCase()))
  .slice().sort((a, b) => a.groupNo.localeCompare(b.groupNo)))
const activeGroup = computed(() => groups.value.find(row => row.groupNo === selectedGroupNo.value) ?? null)
const selectedGroups = computed(() => groups.value.filter(row => selectedGroupNos.value.includes(row.groupNo)))
const activeDetail = computed(() => details.value.find(row => selectedDetailNos.value.includes(row.qcItemNo)) ?? null)
const activeDeduction = computed(() => deductions.value.find(row => row.deductionSerial === selectedDeductionSerial.value) ?? null)
const visibleItems = computed(() => availableItems.value.filter(row => !itemSearch.value || `${row.qcItemNo} ${row.qcItemName}`.toLowerCase().includes(itemSearch.value.trim().toLowerCase())))
const planOptions = computed(() => plans.value.filter(row => row.issueState === 2 && row.componentCollect === editor.value.componentCollect))
const editorTitle = computed(() => ({ add: '計量品管項目群組定義（新增）', edit: '計量品管項目群組定義（修改）', copy: '計量品管項目群組定義（複製）' }[mode.value]))
const deductionTitle = computed(() => activeDeduction.value ? '扣除基準（修改）' : '扣除基準（新增）')

function blankGroup(): GroupRow { return { groupNo: '', groupName: '', lotIsValid: 0, componentCollect: 0, planNo: '', formName: '', webUrl: '', unionErf: 1, issueState: 0, description: '', creator: '', createDate: '', reviser: '', reviseDate: '', cursorMode: 0, qcClass: '', getFilePath: '', errorReportName: '', noticeReportName: '', dataStamp: 0 } }
function blankDeduction(): DeductionRow { return { deductionSerial: '', groupNo: '', normalTimes: 1, deductionTimes: 1, curPass: 0, curStatus: 0, factorValues: {}, creator: '', createDate: '', reviser: '', reviseDate: '', dataStamp: 0 } }
function nowText(): string { return new Date().toLocaleString('sv-SE').replace('T', ' ') }
function seedStore(): Store {
  const audit = { creator: 'DEV001', createDate: '2026-11-17 08:30:00', reviser: 'DEV001', reviseDate: '2026-11-17 09:10:00', dataStamp: 1 }
  const qs: QcItem[] = [
    { qcItemNo: 'PMI-001', qcItemName: '外觀裂痕計量', issueState: 2, componentCollect: 0 },
    { qcItemNo: 'PMI-002', qcItemName: '尺寸偏差計量', issueState: 2, componentCollect: 0 },
    { qcItemNo: 'PMI-003', qcItemName: '晶圓膜厚量測', issueState: 2, componentCollect: 1 },
    { qcItemNo: 'PMI-004', qcItemName: '未核准項目示例', issueState: 0, componentCollect: 0 },
  ]
  const gs = [
    { ...blankGroup(), groupNo: 'PMI-GRP-001', groupName: '外觀計量扣除群組', lotIsValid: 1, planNo: 'DEV-AQL-01', unionErf: 1, issueState: 2 as IssueState, description: 'DEV 假資料：一般量測', ...audit },
    { ...blankGroup(), groupNo: 'PMI-GRP-002', groupName: '尺寸抽驗扣除群組', lotIsValid: 0, issueState: 0 as IssueState, description: 'DEV 假資料：待編輯', ...audit },
    { ...blankGroup(), groupNo: 'PMI-GRP-003', groupName: '簽核中群組', issueState: 1 as IssueState, ...audit },
    { ...blankGroup(), groupNo: 'PMI-GRP-004', groupName: '不使用群組', issueState: -1 as IssueState, ...audit },
  ]
  const ds: DetailRow[] = [
    { groupNo: 'PMI-GRP-001', qcItemNo: 'PMI-001', qcItemName: '外觀裂痕計量', showOrder: 0, deductionItem: 0 },
    { groupNo: 'PMI-GRP-001', qcItemNo: 'PMI-002', qcItemName: '尺寸偏差計量', showOrder: 1, deductionItem: 1 },
    { groupNo: 'PMI-GRP-002', qcItemNo: 'PMI-002', qcItemName: '尺寸偏差計量', showOrder: 0, deductionItem: 0 },
  ]
  const fs: Factor[] = [
    { factorNo: 'MO', factorName: '工單', parameterType: 1, options: ['MO-DEV-1001', 'MO-DEV-1002'] },
    { factorNo: 'PRODUCT', factorName: '產品', parameterType: 1, options: ['PRD-DEV-A', 'PRD-DEV-B'] },
    { factorNo: 'OP', factorName: '作業站', parameterType: 1, options: ['OP-ASSY-10', 'OP-PACK-20'] },
  ]
  const dsx: DeductionRow[] = [{ ...blankDeduction(), deductionSerial: 'DEV-DED-001', groupNo: 'PMI-GRP-001', normalTimes: 5, deductionTimes: 1, curPass: 2, curStatus: 1, factorValues: { MO: 'MO-DEV-1001', PRODUCT: '*' }, ...audit }]
  return { groups: gs, details: ds, deductions: dsx, qcItems: qs, factors: fs, issueFlowEnabled: true,
    operationBindings: { 'PMI-GRP-001': [{ opNo: 'OP-ASSY-10', opName: '組裝量測', inline: 1, qcItemType: 4, inspTypeFai: true, inspTypeIpqc: false, inspTypePqc: false }] } }
}
function readStore(): Store { try { const raw = localStorage.getItem(storeKey); if (raw) return JSON.parse(raw) as Store } catch { /* reset invalid DEV fixture */ } const value = seedStore(); writeStore(value); return value }
function writeStore(value: Store): void { localStorage.setItem(storeKey, JSON.stringify(value)) }
function hydrate(value: Store): void { groups.value = value.groups; details.value = value.details; deductions.value = value.deductions; qcItems.value = value.qcItems; factors.value = value.factors }
function persist(): void { if (mockMode.value) writeStore({ ...readStore(), groups: groups.value, details: details.value, deductions: deductions.value, qcItems: qcItems.value, factors: factors.value }) }
function apiUrl(path: string): string { const base = props.apiBaseUrl.trim(); if (!base) throw new Error('尚未設定 API 位址。'); return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString() }
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers); if (init.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init, headers })
  if (response.status === 401) { window.location.assign(basePath + '/'); throw new Error('登入逾時，請重新登入。') }
  if (!response.ok) { const problem = await response.json().catch(() => null) as { detail?: string; title?: string } | null; throw new Error(problem?.detail || problem?.title || `API 回應失敗（${response.status}）。`) }
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}
function listOf<T>(value: T[] | { items?: T[]; rows?: T[]; data?: T[] }): T[] { return Array.isArray(value) ? value : value.items ?? value.rows ?? value.data ?? [] }
function normalizeGroup(value: Record<string, unknown>): GroupRow {
  const get = (name: string) => value[name] ?? value[name.toUpperCase()]
  return { ...blankGroup(), groupNo: String(get('groupNo') ?? ''), groupName: String(get('groupName') ?? ''), lotIsValid: Number(get('lotIsValid') ?? 0), componentCollect: Number(get('componentCollect') ?? 0), planNo: String(get('planNo') ?? ''), formName: String(get('formName') ?? ''), webUrl: String(get('webUrl') ?? ''), unionErf: Number(get('unionErf') ?? 0), issueState: Number(get('issueState') ?? 0) as IssueState, description: String(get('description') ?? ''), creator: String(get('creator') ?? ''), createDate: String(get('createDate') ?? ''), reviser: String(get('reviser') ?? ''), reviseDate: String(get('reviseDate') ?? ''), cursorMode: Number(get('cursorMode') ?? 0), qcClass: String(get('qcClass') ?? ''), getFilePath: String(get('getFilePath') ?? ''), errorReportName: String(get('errorReportName') ?? ''), noticeReportName: String(get('noticeReportName') ?? ''), dataStamp: Number(get('dataStamp') ?? 0) }
}
function normalizeDetail(value: Record<string, unknown>): DetailRow { return { groupNo: String(value.groupNo ?? value.GROUPNO ?? ''), qcItemNo: String(value.qcItemNo ?? value.QCITEMNO ?? ''), qcItemName: String(value.qcItemName ?? value.QCITEMNAME ?? ''), showOrder: Number(value.showOrder ?? value.SHOWORDER ?? 0), deductionItem: Number(value.deductionItem ?? value.DEDUCTIONITEM ?? 0) } }
function normalizeDeduction(value: Record<string, unknown>): DeductionRow { return { ...blankDeduction(), deductionSerial: String(value.deductionSerial ?? value.DEDUCTIONSERIAL ?? ''), groupNo: String(value.groupNo ?? value.GROUPNO ?? ''), normalTimes: Number(value.normalTimes ?? value.NORMALTIMES ?? 0), deductionTimes: Number(value.deductionTimes ?? value.DEDUCTIONTIMES ?? 0), curPass: Number(value.curPass ?? value.CURPASS ?? 0), curStatus: Number(value.curStatus ?? value.CURSTATUS ?? 0), factorValues: (value.factorValues ?? value.FACTORVALUES ?? {}) as Record<string, string>, creator: String(value.creator ?? value.CREATOR ?? ''), createDate: String(value.createDate ?? value.CREATEDATE ?? ''), reviser: String(value.reviser ?? value.REVISER ?? ''), reviseDate: String(value.reviseDate ?? value.REVISEDATE ?? ''), dataStamp: Number(value.dataStamp ?? value.DATASTAMP ?? 0) } }
async function loadGroups(): Promise<void> {
  loading.value = true; error.value = ''
  try {
    if (mockMode.value) hydrate(readStore())
    else groups.value = listOf(await request<GroupRow[] | { items?: GroupRow[] }>(`api/qc/pmi-item-groups?issueState=${issueStateFilter.value}&groupNo=${encodeURIComponent(groupNoFilter.value)}`)).map(row => normalizeGroup(row as unknown as Record<string, unknown>))
    if (!filteredGroups.value.some(row => row.groupNo === selectedGroupNo.value)) { selectedGroupNo.value = filteredGroups.value[0]?.groupNo ?? ''; selectedGroupNos.value = selectedGroupNo.value ? [selectedGroupNo.value] : [] }
    await loadChildren(selectedGroupNo.value)
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入 PMI 群組失敗。' }
  finally { loading.value = false }
}
async function loadLookups(): Promise<void> {
  if (mockMode.value) { hydrate(readStore()); plans.value = [{ planNo: 'DEV-AQL-01', planName: '一般抽樣計畫 A', planType: 1, componentCollect: 0, issueState: 2 }, { planNo: 'DEV-WAFER-01', planName: 'Wafer 抽樣計畫 A', planType: 3, componentCollect: 1, issueState: 2 }]; return }
  const [itemData, factorData, standardPlanData, waferPlanData] = await Promise.all([
    request<QcItem[] | { items?: QcItem[] }>('api/qc/variable-qc-items?issueState=2'),
    request<Factor[] | { items?: Factor[] }>('api/qc/pmi-item-groups/options/factors?issueState=2'),
    request<SamplingPlan[] | { items?: SamplingPlan[] }>('api/qc/sampling-plans?componentCollect=0&issueState=2'),
    request<SamplingPlan[] | { items?: SamplingPlan[] }>('api/qc/sampling-plans?componentCollect=1&issueState=2'),
  ])
  qcItems.value = listOf(itemData).map(row => { const item = row as unknown as Record<string, unknown>; return { qcItemNo: String(item.qcItemNo ?? item.QCITEMNO ?? ''), qcItemName: String(item.qcItemName ?? item.QCITEMNAME ?? ''), issueState: Number(item.issueState ?? item.ISSUESTATE ?? 2), componentCollect: Number(item.componentCollect ?? item.COMPONENTCOLLECT ?? 0) } })
  factors.value = listOf(factorData).map(row => { const item = row as unknown as Record<string, unknown>; return { factorNo: String(item.factorNo ?? item.factorno ?? ''), factorName: String(item.factorName ?? item.factorname ?? item.factorNo ?? ''), parameterType: Number(item.parameterType ?? item.parametertype ?? 0), options: (item.options ?? []) as string[] } })
  plans.value = [...listOf(standardPlanData), ...listOf(waferPlanData)]
}
async function loadChildren(groupNo: string): Promise<void> {
  selectedGroupNo.value = groupNo; selectedDetailNos.value = []; selectedDeductionSerial.value = ''
  if (!groupNo) { details.value = []; deductions.value = []; return }
  try {
    if (mockMode.value) { const store = readStore(); details.value = store.details.filter(row => row.groupNo === groupNo).slice().sort((a, b) => a.showOrder - b.showOrder); deductions.value = store.deductions.filter(row => row.groupNo === groupNo); return }
    const [detailData, deductionData] = await Promise.all([
      request<DetailRow[] | { items?: DetailRow[] }>(`api/qc/pmi-item-groups/${encodeURIComponent(groupNo)}/details`),
      request<DeductionRow[] | { items?: DeductionRow[] }>(`api/qc/pmi-item-groups/${encodeURIComponent(groupNo)}/deductions`),
    ])
    details.value = listOf(detailData).map(row => normalizeDetail(row as unknown as Record<string, unknown>)).sort((a, b) => a.showOrder - b.showOrder)
    deductions.value = listOf(deductionData).map(row => normalizeDeduction(row as unknown as Record<string, unknown>))
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入群組明細失敗。'; details.value = []; deductions.value = [] }
}
function selectGroup(row: GroupRow): void { selectedGroupNo.value = row.groupNo; if (!selectedGroupNos.value.includes(row.groupNo)) selectedGroupNos.value = [row.groupNo]; void loadChildren(row.groupNo) }
function toggleGroup(row: GroupRow): void { selectedGroupNos.value = selectedGroupNos.value.includes(row.groupNo) ? selectedGroupNos.value.filter(no => no !== row.groupNo) : [...selectedGroupNos.value, row.groupNo] }
function selectAll(event: Event): void { selectedGroupNos.value = (event.target as HTMLInputElement).checked ? filteredGroups.value.map(row => row.groupNo) : [] }
function search(): void { groupNoFilter.value = groupNoInput.value.trim(); void loadGroups() }
function changeState(value: IssueState): void { issueStateFilter.value = value; selectedGroupNo.value = ''; selectedGroupNos.value = []; void loadGroups() }
function canEditField(field: string): boolean {
  if (mode.value === 'add' || mode.value === 'copy') return true
  if (field === 'groupNo' || editReadOnly.value) return false
  if (editor.value.issueState === 0) return true
  if (field === 'planNo' || field === 'unionPlan') return editableFields.value.includes('planNo') || editableFields.value.includes('unionPlan')
  return editableFields.value.includes(field)
}
async function openGroupForm(nextMode: EditMode): Promise<void> {
  error.value = ''; message.value = ''
  const source = activeGroup.value
  if (nextMode !== 'add' && !source) { error.value = '請先選取一筆群組資料。'; return }
  mode.value = nextMode
  if (nextMode === 'add') editor.value = { ...blankGroup(), creator: props.currentUserNo || 'DEV001', createDate: nowText() }
  else editor.value = { ...source!, groupNo: nextMode === 'copy' ? '' : source!.groupNo, groupName: nextMode === 'copy' ? `${source!.groupName} - 複本` : source!.groupName, issueState: nextMode === 'copy' ? 0 : source!.issueState, creator: nextMode === 'copy' ? (props.currentUserNo || 'DEV001') : source!.creator, createDate: nextMode === 'copy' ? nowText() : source!.createDate, reviser: nextMode === 'edit' ? props.currentUserNo : '', reviseDate: nextMode === 'edit' ? nowText() : '' }
  unionPlan.value = !!editor.value.planNo
  editableFields.value = []
  if (nextMode === 'edit' && editor.value.issueState !== 0 && editor.value.issueState !== 1) {
    try {
      if (mockMode.value) editableFields.value = ['groupName', 'lotIsValid', 'planNo', 'unionErf', 'description', 'getFilePath']
      else editableFields.value = listOf(await request<string[] | { items?: string[] }>(`api/qc/pmi-item-groups/${encodeURIComponent(editor.value.groupNo)}/edit-policy`))
    } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入舊系統欄位修改權限失敗。'; return }
  }
  formOpen.value = true
}
function toggleUnionPlan(): void { if (!unionPlan.value) editor.value.planNo = '' }
async function saveGroup(apply = false): Promise<void> {
  const value = editor.value
  if (!canSaveGroup.value) { error.value = '此簽核狀態或 Issue Control 設定不允許修改群組欄位。'; return }
  if (!value.groupNo.trim() || !value.groupName.trim()) { error.value = '群組編號與群組名稱為必填。'; return }
  if (![0, 1].includes(value.lotIsValid)) { error.value = '請選擇 Lot Is Valid。'; return }
  if (unionPlan.value && !value.planNo.trim()) { error.value = '已勾選連結抽樣計畫，請輸入抽樣計畫編號。'; return }
  if (value.groupNo.length > 25 || value.groupName.length > 50 || value.description.length > 255 || value.getFilePath.length > 50) { error.value = '欄位超過來源資料表長度限制。'; return }
  busy.value = true
  try {
    if (mockMode.value) {
      const store = readStore()
      if (mode.value === 'edit') { const index = store.groups.findIndex(row => row.groupNo === value.groupNo); if (index < 0) throw new Error('群組資料已不存在。'); store.groups[index] = { ...value, reviser: props.currentUserNo || 'DEV001', reviseDate: nowText(), dataStamp: store.groups[index].dataStamp + 1 } }
      else { if (store.groups.some(row => row.groupNo.toUpperCase() === value.groupNo.toUpperCase())) throw new Error('群組編號已存在。'); store.groups.push({ ...value, issueState: 0 }); }
      hydrate(store); writeStore(store)
    } else if (mode.value === 'edit') await request(`api/qc/pmi-item-groups/${encodeURIComponent(value.groupNo)}`, { method: 'PUT', body: JSON.stringify(value) })
    else await request('api/qc/pmi-item-groups', { method: 'POST', body: JSON.stringify(value) })
    const key = value.groupNo; if (!apply) formOpen.value = false
    await loadGroups(); selectedGroupNo.value = key; selectedGroupNos.value = [key]; await loadChildren(key)
    message.value = mode.value === 'copy' ? '已建立複製的群組主檔；WinForms 複製功能不複製明細。' : '群組資料已儲存。'
    if (apply) { mode.value = 'edit'; editor.value = { ...(activeGroup.value ?? value) } }
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '儲存群組失敗。' }
  finally { busy.value = false }
}
async function deleteGroups(): Promise<void> {
  const targets = selectedGroups.value.length ? selectedGroups.value : activeGroup.value ? [activeGroup.value] : []
  if (!targets.length) { error.value = '請先選取要刪除的群組。'; return }
  const blocked: string[] = []
  busy.value = true; confirmDelete.value = false
  try {
    for (const row of targets) {
      if (row.issueState === 1) { blocked.push(row.groupNo); continue }
      if (mockMode.value) {
        if (row.issueState === 2) groups.value = groups.value.map(item => item.groupNo === row.groupNo ? { ...item, issueState: -1, reviser: props.currentUserNo || 'DEV001', reviseDate: nowText(), dataStamp: item.dataStamp + 1 } : item)
        else { groups.value = groups.value.filter(item => item.groupNo !== row.groupNo); details.value = details.value.filter(item => item.groupNo !== row.groupNo) }
      } else await request(`api/qc/pmi-item-groups/${encodeURIComponent(row.groupNo)}`, { method: 'DELETE', body: JSON.stringify({ issueState: row.issueState, dataStamp: row.dataStamp }) })
    }
    persist(); await loadGroups(); message.value = '刪除作業完成。'; if (blocked.length) error.value = `以下群組正簽核中，不允許刪除：${blocked.join('、')}`
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '刪除群組失敗。' }
  finally { busy.value = false }
}
async function approveGroups(): Promise<void> {
  const targets = selectedGroups.value.length ? selectedGroups.value : activeGroup.value ? [activeGroup.value] : []
  if (!targets.length) { error.value = '請先選取要核准的群組。'; return }
  if (!details.value.length) { error.value = '目前群組尚未設定品管項目，不能核准。'; return }
  busy.value = true
  try {
    for (const row of targets) {
      if (row.issueState !== 0) continue
      if (mockMode.value) { const state: IssueState = readStore().issueFlowEnabled ? 1 : 2; groups.value = groups.value.map(item => item.groupNo === row.groupNo ? { ...item, issueState: state, reviser: props.currentUserNo || 'DEV001', reviseDate: nowText(), dataStamp: item.dataStamp + 1 } : item) }
      else await request(`api/qc/pmi-item-groups/${encodeURIComponent(row.groupNo)}/approval`, { method: 'POST', body: JSON.stringify({ dataStamp: row.dataStamp }) })
    }
    persist(); await loadGroups(); message.value = '簽核已送出。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '簽核失敗。' }
  finally { busy.value = false }
}
function openDetails(): void {
  if (!activeGroup.value) { error.value = '請先選取群組資料。'; return }
  if (activeGroup.value.issueState !== 0) { error.value = '只有未簽核群組可以維護品管項目。'; return }
  draftDetails.value = details.value.map(row => ({ ...row })); selectedDraft.value = []; selectedAvailable.value = []; itemSearch.value = ''
  availableItems.value = qcItems.value.filter(row => row.issueState === 2 && row.componentCollect === activeGroup.value!.componentCollect && !details.value.some(item => item.qcItemNo === row.qcItemNo)); panel.value = 'details'
}
function addDetails(): void { for (const item of availableItems.value.filter(row => selectedAvailable.value.includes(row.qcItemNo))) draftDetails.value.push({ groupNo: activeGroup.value!.groupNo, qcItemNo: item.qcItemNo, qcItemName: item.qcItemName, showOrder: draftDetails.value.length, deductionItem: 0 }); const selected = new Set(selectedAvailable.value); availableItems.value = availableItems.value.filter(item => !selected.has(item.qcItemNo)); selectedAvailable.value = []; }
function removeDraftDetails(): void { const gone = new Set(selectedDraft.value); for (const row of draftDetails.value.filter(item => gone.has(item.qcItemNo))) { const item = qcItems.value.find(q => q.qcItemNo === row.qcItemNo); if (item) availableItems.value.push(item) } draftDetails.value = draftDetails.value.filter(row => !gone.has(row.qcItemNo)); draftDetails.value.forEach((row, index) => row.showOrder = index); selectedDraft.value = [] }
function moveDraft(delta: -1 | 1): void { if (selectedDraft.value.length !== 1) return; const from = draftDetails.value.findIndex(row => row.qcItemNo === selectedDraft.value[0]); const to = from + delta; if (to < 0 || to >= draftDetails.value.length) return; [draftDetails.value[from], draftDetails.value[to]] = [draftDetails.value[to], draftDetails.value[from]]; draftDetails.value.forEach((row, index) => row.showOrder = index) }
async function saveDetails(): Promise<void> {
  if (draftDetails.value.length && draftDetails.value.every(row => row.deductionItem === 1)) { error.value = '至少要保留一個非減項品管項目。'; return }
  busy.value = true
  try {
    const body = { items: draftDetails.value.map((row, index) => ({ qcItemNo: row.qcItemNo, showOrder: index, deductionItem: row.deductionItem })) }
    if (mockMode.value) { details.value = [...details.value.filter(row => row.groupNo !== activeGroup.value!.groupNo), ...draftDetails.value.map((row, index) => ({ ...row, showOrder: index }))] }
    else await request(`api/qc/pmi-item-groups/${encodeURIComponent(activeGroup.value!.groupNo)}/details`, { method: 'PUT', body: JSON.stringify(body) })
    persist(); panel.value = ''; await loadChildren(activeGroup.value!.groupNo); message.value = '群組品管項目與排序已儲存。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '儲存品管明細失敗。' }
  finally { busy.value = false }
}
function openDeduction(nextMode: 'add' | 'edit' | 'copy'): void {
  if (!activeGroup.value) { error.value = '請先選取群組資料。'; return }
  if (activeGroup.value.issueState !== 2) { error.value = '扣除基準只允許維護已核准群組。'; return }
  const source = activeDeduction.value
  if (nextMode !== 'add' && !source) { error.value = '請先選取一筆扣除基準。'; return }
  deductionEditor.value = nextMode === 'add' ? { ...blankDeduction(), groupNo: activeGroup.value.groupNo, creator: props.currentUserNo || 'DEV001', createDate: nowText(), factorValues: Object.fromEntries(factors.value.map(f => [f.factorNo, '*'])) }
    : { ...source!, deductionSerial: nextMode === 'copy' ? '' : source!.deductionSerial, groupNo: activeGroup.value.groupNo, creator: nextMode === 'copy' ? (props.currentUserNo || 'DEV001') : source!.creator, createDate: nextMode === 'copy' ? nowText() : source!.createDate, reviser: nextMode === 'edit' ? props.currentUserNo : '', reviseDate: nextMode === 'edit' ? nowText() : '', dataStamp: nextMode === 'copy' ? 0 : source!.dataStamp, factorValues: { ...source!.factorValues } }
  mode.value = nextMode; panel.value = 'deduction'
}
async function saveDeduction(): Promise<void> {
  const value = deductionEditor.value
  if (!Number.isInteger(value.normalTimes) || value.normalTimes <= 0 || value.normalTimes > 99 || !Number.isInteger(value.deductionTimes) || value.deductionTimes <= 0 || value.deductionTimes > 99 || !Number.isInteger(value.curPass) || value.curPass < 0 || value.curPass > 999) { error.value = 'NormalTimes、DeductionTimes 須為 1–99，CurPass 須為 0–999。'; return }
  for (const factor of factors.value) {
    const factorValue = String(value.factorValues[factor.factorNo] ?? '').trim()
    if (!factorValue) { error.value = `請輸入要因「${factor.factorName}」的值。`; return }
    if (factorValue.length > 61) { error.value = `要因「${factor.factorName}」最多 61 個字元。`; return }
  }
  busy.value = true
  try {
    const body = { ...value, factorValues: value.factorValues }
    if (mockMode.value) {
      const rows = deductions.value.slice()
      if (mode.value === 'edit') { const index = rows.findIndex(row => row.deductionSerial === value.deductionSerial); rows[index] = { ...value, reviser: props.currentUserNo || 'DEV001', reviseDate: nowText(), dataStamp: rows[index].dataStamp + 1 } }
      else { const newSerial = `DEV-DED-${String(Date.now()).slice(-6)}`; rows.push({ ...value, deductionSerial: newSerial }); selectedDeductionSerial.value = newSerial }
      deductions.value = rows
    } else if (mode.value === 'edit') await request(`api/qc/pmi-item-groups/${encodeURIComponent(value.groupNo)}/deductions/${encodeURIComponent(value.deductionSerial)}`, { method: 'PUT', body: JSON.stringify(body) })
    else await request(`api/qc/pmi-item-groups/${encodeURIComponent(value.groupNo)}/deductions`, { method: 'POST', body: JSON.stringify(body) })
    persist(); panel.value = ''; await loadChildren(value.groupNo); message.value = '扣除基準已儲存。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '儲存扣除基準失敗。' }
  finally { busy.value = false }
}
async function deleteDeduction(): Promise<void> { if (!activeDeduction.value) { error.value = '請先選取扣除基準。'; return } if (!window.confirm(`確定刪除扣除序號 ${activeDeduction.value.deductionSerial}？`)) return; try { if (mockMode.value) deductions.value = deductions.value.filter(row => row.deductionSerial !== selectedDeductionSerial.value); else await request(`api/qc/pmi-item-groups/${encodeURIComponent(activeGroup.value!.groupNo)}/deductions/${encodeURIComponent(selectedDeductionSerial.value)}`, { method: 'DELETE' }); persist(); await loadChildren(activeGroup.value!.groupNo); message.value = '扣除基準已刪除。' } catch (cause) { error.value = cause instanceof Error ? cause.message : '刪除扣除基準失敗。' } }
async function openParameters(): Promise<void> { if (!activeGroup.value) { error.value = '請先選取群組資料。'; return } try { parameterRows.value = mockMode.value ? [{ parameterNo: 'TEMP', parameterName: '環境溫度', value: '25' }, { parameterNo: 'HUMIDITY', parameterName: '環境濕度', value: '' }] : listOf(await request<Array<{ parameterNo: string; parameterName: string; value: string }> | { items?: { parameterNo: string; parameterName: string; value: string }[] }>(`api/qc/item-parameters?qcItemType=4&qcItemNo=${encodeURIComponent(activeGroup.value.groupNo)}`)); panel.value = 'parameters' } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入 Parameter 失敗。' } }
async function saveParameters(): Promise<void> { try { if (!mockMode.value) await request(`api/qc/item-parameters?qcItemType=4&qcItemNo=${encodeURIComponent(activeGroup.value!.groupNo)}`, { method: 'PUT', body: JSON.stringify(parameterRows.value) }); panel.value = ''; message.value = 'Parameter 已儲存。' } catch (cause) { error.value = cause instanceof Error ? cause.message : '儲存 Parameter 失敗。' } }
function normalizeOp(value: Record<string, unknown>): OpRow {
  return { opNo: String(value.opNo ?? value.OPNO ?? ''), opName: String(value.opName ?? value.OPNAME ?? ''), inline: Number(value.inline ?? value.INLINE ?? 0) === 1 ? 1 : 0,
    qcItemType: Number(value.qcItemType ?? value.QCITEMTYPE ?? 4), inspTypeFai: Boolean(Number(value.inspTypeFai ?? value.INSPTYPE_FAI ?? 0)),
    inspTypeIpqc: Boolean(Number(value.inspTypeIpqc ?? value.INSPTYPE_IPQC ?? 0)), inspTypePqc: Boolean(Number(value.inspTypePqc ?? value.INSPTYPE_PQC ?? 0)) }
}
function assignOperations(inline: 0 | 1): void {
  for (const opNo of selectedCandidateNos.value) {
    const candidate = availableOperations.value.find(row => row.opNo === opNo)
    if (candidate) operationRows.value.push({ ...candidate, inline, qcItemType: 4, inspTypeFai: false, inspTypeIpqc: false, inspTypePqc: false })
  }
  selectedCandidateNos.value = []
}
function removeAssignedOperations(): void {
  operationRows.value = operationRows.value.filter(row => !selectedOperationNos.value.includes(row.opNo))
  selectedOperationNos.value = []
}
async function openOperations(): Promise<void> {
  if (!activeGroup.value) { error.value = '請先選取群組資料。'; return }
  if (activeGroup.value.issueState !== 2) { error.value = '未簽核或簽核中的群組不可設定作業站。'; return }
  try {
    selectedCandidateNos.value = []; selectedOperationNos.value = []
    if (mockMode.value) {
      opCandidates.value = [{ opNo: 'OP-ASSY-10', opName: '組裝量測' }, { opNo: 'OP-PACK-20', opName: '包裝檢查' }, { opNo: 'OP-FINAL-30', opName: '成品檢驗' }]
      operationRows.value = structuredClone(readStore().operationBindings?.[activeGroup.value.groupNo] ?? [])
    } else {
      const [candidateData, bindingData] = await Promise.all([
        request<Array<{ opNo: string; opName: string }> | { items?: { opNo: string; opName: string }[] }>('api/process/operations?issueState=2'),
        request<Array<Record<string, unknown>> | { items?: Record<string, unknown>[] }>(`api/qc/item-operations?qcType=4&qcItemNo=${encodeURIComponent(activeGroup.value.groupNo)}`),
      ])
      opCandidates.value = listOf(candidateData).map(row => { const item = row as unknown as Record<string, unknown>; return { opNo: String(item.opNo ?? item.OPNO ?? ''), opName: String(item.opName ?? item.OPNAME ?? '') } }).filter(row => row.opNo)
      operationRows.value = listOf(bindingData).map(row => normalizeOp(row as Record<string, unknown>))
    }
    panel.value = 'operations'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入 Op 失敗。' }
}
async function saveOperations(): Promise<void> {
  if (operationRows.value.some(row => row.inline === 1 && !row.inspTypeFai && !row.inspTypeIpqc && !row.inspTypePqc)) { error.value = '每個 InLine 作業站至少要選一種檢驗時機（FAI／IPQC／PQC）。'; return }
  try {
    if (mockMode.value) { const store = readStore(); store.operationBindings ??= {}; store.operationBindings[activeGroup.value!.groupNo] = structuredClone(operationRows.value); writeStore(store) }
    else await request(`api/qc/item-operations?qcType=4&qcItemNo=${encodeURIComponent(activeGroup.value!.groupNo)}`, { method: 'PUT', body: JSON.stringify({ operations: operationRows.value }) })
    panel.value = ''; message.value = '作業站對應已儲存。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '儲存 Op 失敗。' }
}
function yesNo(value: number): string { return value === 1 ? '是' : '否' }
onMounted(async () => { try { await loadLookups(); await loadGroups() } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入資料失敗。' } })
</script>

<template>
  <section class="pmi-page">
    <header class="pmi-heading"><div><h1>計量品管項目群組_減項</h1><small>QC216 · QCITEM_G_PMI</small></div><button class="pmi-btn" @click="emit('close')">關閉</button></header>
    <div class="pmi-query">
      <label v-for="option in statusOptions" :key="option.value"><input type="radio" name="pmi-state" :checked="issueStateFilter === option.value" @change="changeState(option.value)">{{ option.label }}</label>
      <span>群組編號</span><input v-model="groupNoInput" class="pmi-search" @keyup.enter="search"><span>%</span><button class="pmi-btn" @click="search">查詢</button><span class="pmi-count">資料總筆數 {{ filteredGroups.length }}</span>
    </div>
    <div v-if="error" class="pmi-alert error">{{ error }}<button @click="error = ''">×</button></div><div v-if="message" class="pmi-alert success">{{ message }}<button @click="message = ''">×</button></div>
    <fieldset class="pmi-box pmi-master"><legend>計量品管項目群組資料</legend><div class="pmi-master-layout">
      <div class="pmi-grid-wrap"><table class="pmi-grid"><thead><tr><th class="check"><input type="checkbox" :checked="filteredGroups.length > 0 && filteredGroups.every(row => selectedGroupNos.includes(row.groupNo))" @change="selectAll"></th><th>群組編號</th><th>群組名稱</th><th>LotIsValid</th><th>PlanNo</th><th>UnionERF</th><th>說明</th><th>建立人</th><th>建立日期</th><th>CursorMode</th><th>GetFilePath</th><th>修改人</th><th>修改日期</th></tr></thead><tbody>
        <tr v-for="row in filteredGroups" :key="row.groupNo" :class="{ active: row.groupNo === selectedGroupNo }" @click="selectGroup(row)"><td class="check" @click.stop><input type="checkbox" :checked="selectedGroupNos.includes(row.groupNo)" @change="toggleGroup(row)"></td><td>{{ row.groupNo }}</td><td>{{ row.groupName }}</td><td>{{ yesNo(row.lotIsValid) }}</td><td>{{ row.planNo }}</td><td>{{ yesNo(row.unionErf) }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.cursorMode }}</td><td>{{ row.getFilePath }}</td><td>{{ row.reviser }}</td><td>{{ row.reviseDate }}</td></tr>
        <tr v-if="!filteredGroups.length"><td colspan="13" class="empty">{{ loading ? '載入中…' : '查無資料' }}</td></tr>
      </tbody></table></div><aside class="pmi-rail"><button class="pmi-btn primary" @click="openGroupForm('add')">ADD (A)</button><button class="pmi-btn" :disabled="!activeGroup" @click="openGroupForm('edit')">EDIT (E)</button><button class="pmi-btn" :disabled="!selectedGroups.length && !activeGroup" @click="confirmDelete = true">DEL (D)</button><button class="pmi-btn" :disabled="!activeGroup" @click="openGroupForm('copy')">Copy (C)</button><button class="pmi-btn" :disabled="!selectedGroups.length && !activeGroup" @click="approveGroups">Approve</button><button class="pmi-btn" :disabled="!activeGroup" @click="openParameters">Parameter</button><button class="pmi-btn" :disabled="!activeGroup || activeGroup.issueState === 0 || activeGroup.issueState === 1" @click="openOperations">Op</button><button class="pmi-btn" :disabled="activeGroup?.issueState !== 0" @click="openDetails">Add QC</button></aside>
      </div><footer class="pmi-footer">資料總筆數 {{ filteredGroups.length }}</footer>
    </fieldset>
    <div class="pmi-lower">
      <fieldset class="pmi-box pmi-detail-box"><legend>群組品管項目　{{ activeGroup?.groupNo || '' }} {{ activeGroup?.groupName || '' }}</legend>
        <div class="pmi-grid-wrap"><table class="pmi-grid"><thead><tr><th>品管項目編號</th><th>品管項目名稱</th><th>工序順序</th><th>減項</th></tr></thead><tbody><tr v-for="row in details" :key="row.qcItemNo" :class="{ active: selectedDetailNos.includes(row.qcItemNo) }" @click="selectedDetailNos = selectedDetailNos.includes(row.qcItemNo) ? selectedDetailNos.filter(no => no !== row.qcItemNo) : [row.qcItemNo]"><td>{{ row.qcItemNo }}</td><td>{{ row.qcItemName }}</td><td>{{ row.showOrder }}</td><td>{{ yesNo(row.deductionItem) }}</td></tr><tr v-if="!details.length"><td colspan="4" class="empty">尚未設定品管項目</td></tr></tbody></table></div><footer class="pmi-footer">資料總筆數 {{ details.length }}</footer>
      </fieldset>
      <fieldset class="pmi-box pmi-deduction-box"><legend>扣除基準</legend><div class="pmi-deduction-layout">
        <div class="pmi-deduction-table"><div class="pmi-grid-wrap"><table class="pmi-grid"><thead><tr><th>扣除序號</th><th v-for="factor in factors" :key="factor.factorNo">{{ factor.factorName }}</th><th>正常次數</th><th>扣除次數</th><th>目前通過數</th><th>狀態</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th></tr></thead><tbody><tr v-for="row in deductions" :key="row.deductionSerial" :class="{ active: selectedDeductionSerial === row.deductionSerial }" @click="selectedDeductionSerial = row.deductionSerial"><td>{{ row.deductionSerial }}</td><td v-for="factor in factors" :key="factor.factorNo">{{ row.factorValues[factor.factorNo] ?? '*' }}</td><td>{{ row.normalTimes }}</td><td>{{ row.deductionTimes }}</td><td>{{ row.curPass }}</td><td>{{ ['一般', '扣除', '扣除重測'][row.curStatus] ?? row.curStatus }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser }}</td><td>{{ row.reviseDate }}</td></tr><tr v-if="!deductions.length"><td :colspan="9 + factors.length" class="empty">尚未設定扣除基準</td></tr></tbody></table></div><footer class="pmi-footer">資料總筆數 {{ deductions.length }}</footer></div>
        <aside class="pmi-rail"><button class="pmi-btn primary" :disabled="activeGroup?.issueState !== 2" @click="openDeduction('add')">ADD</button><button class="pmi-btn" :disabled="!activeDeduction || activeGroup?.issueState !== 2" @click="openDeduction('edit')">EDIT</button><button class="pmi-btn" :disabled="!activeDeduction || activeGroup?.issueState !== 2" @click="deleteDeduction">DEL</button><button class="pmi-btn" :disabled="!activeDeduction || activeGroup?.issueState !== 2" @click="openDeduction('copy')">Copy</button><button class="pmi-btn" @click="emit('close')">Close</button></aside>
      </div>
      </fieldset>
    </div>

    <div v-if="formOpen" class="pmi-backdrop" @click.self="formOpen = false"><section class="pmi-dialog" role="dialog" aria-modal="true"><header><div><h2>{{ editorTitle }}</h2><small>QC216 · GroupNo {{ editor.groupNo || '新增' }}</small></div><button class="x" @click="formOpen = false">×</button></header><div class="pmi-form">
      <label>群組編號<input v-model="editor.groupNo" maxlength="25" :readonly="mode === 'edit'" class="yellow" @blur="editor.groupNo = editor.groupNo.toUpperCase()"></label><label>群組名稱<input v-model="editor.groupName" maxlength="50" class="yellow" :readonly="!canEditField('groupName')"></label>
      <fieldset class="pmi-radio"><legend>Lot Is Valid</legend><label><input v-model.number="editor.lotIsValid" type="radio" :value="0" :disabled="!canEditField('lotIsValid')">否</label><label><input v-model.number="editor.lotIsValid" type="radio" :value="1" :disabled="!canEditField('lotIsValid')">是</label></fieldset>
      <label class="wide check-label"><input v-model="unionPlan" type="checkbox" :disabled="!canEditField('unionPlan')" @change="toggleUnionPlan">Union Plan No</label><label>抽樣計畫編號<select v-model="editor.planNo" class="yellow" :disabled="!unionPlan || !canEditField('planNo')"><option value="">請選擇</option><option v-for="plan in planOptions" :key="plan.planNo" :value="plan.planNo">{{ plan.planNo }}　{{ plan.planName }}</option></select></label><label class="check-label"><input v-model.number="editor.unionErf" type="checkbox" :true-value="1" :false-value="0" :disabled="!canEditField('unionErf')">Union Report Create</label>
      <label>Get File Path<input v-model="editor.getFilePath" maxlength="50" :readonly="!canEditField('getFilePath')"></label><label class="wide">說明<textarea v-model="editor.description" maxlength="255" rows="3" :readonly="!canEditField('description')"></textarea></label>
      <div class="audit"><label>建立人<input :value="editor.creator" readonly></label><label>建立日期<input :value="editor.createDate" readonly></label><label>修改人<input :value="editor.reviser" readonly></label><label>修改日期<input :value="editor.reviseDate" readonly></label></div>
    </div><p v-if="editReadOnly" class="hint">簽核中資料唯讀。</p><p v-if="mode === 'edit' && editor.issueState !== 0 && !editReadOnly && editableFields.length === 0" class="hint">目前 Issue Control 未開放可修改欄位。</p><p v-if="error" class="modal-error">{{ error }}</p><footer><button class="pmi-btn primary" :disabled="busy || !canSaveGroup" @click="saveGroup()">確定</button><button class="pmi-btn" @click="formOpen = false">關閉</button><button v-if="mode !== 'copy'" class="pmi-btn" :disabled="busy || !canSaveGroup" @click="saveGroup(true)">套用</button></footer></section></div>

    <div v-if="panel === 'details'" class="pmi-backdrop" @click.self="panel = ''"><section class="pmi-dialog wide-dialog" role="dialog" aria-modal="true"><header><div><h2>群組品管項目設定</h2><small>僅列出同 ComponentCollect 且已核准的計量品管項目</small></div><button class="x" @click="panel = ''">×</button></header><input v-model="itemSearch" class="pmi-search full" placeholder="品管項目編號／名稱"><div class="picker"><div><h3>可選項目</h3><button v-for="item in visibleItems" :key="item.qcItemNo" class="pick-row" :class="{ chosen: selectedAvailable.includes(item.qcItemNo) }" @click="selectedAvailable = selectedAvailable.includes(item.qcItemNo) ? selectedAvailable.filter(no => no !== item.qcItemNo) : [...selectedAvailable, item.qcItemNo]"><span>{{ item.qcItemNo }}</span><span>{{ item.qcItemName }}</span></button></div><div class="move"><button class="pmi-btn" @click="addDetails">加入 →</button><button class="pmi-btn" @click="removeDraftDetails">← 移除</button></div><div><h3>群組明細（勾選減項）</h3><div v-for="(row, index) in draftDetails" :key="row.qcItemNo" class="detail-draft"><button class="pick-row" :class="{ chosen: selectedDraft.includes(row.qcItemNo) }" @click="selectedDraft = selectedDraft.includes(row.qcItemNo) ? selectedDraft.filter(no => no !== row.qcItemNo) : [row.qcItemNo]"><span>{{ index + 1 }}. {{ row.qcItemNo }}</span><span>{{ row.qcItemName }}</span></button><label><input v-model.number="row.deductionItem" type="checkbox" :true-value="1" :false-value="0">減項</label></div><div class="order"><button class="pmi-btn" :disabled="selectedDraft.length !== 1" @click="moveDraft(-1)">上移</button><button class="pmi-btn" :disabled="selectedDraft.length !== 1" @click="moveDraft(1)">下移</button></div></div></div><p class="hint">至少保留一項非減項品管項目。新增、移除與順序變更以整組明細儲存。</p><p v-if="error" class="modal-error">{{ error }}</p><footer><button class="pmi-btn primary" :disabled="busy" @click="saveDetails">確定</button><button class="pmi-btn" @click="panel = ''">關閉</button></footer></section></div>

    <div v-if="panel === 'deduction'" class="pmi-backdrop" @click.self="panel = ''"><section class="pmi-dialog wide-dialog" role="dialog" aria-modal="true"><header><div><h2>{{ mode === 'copy' ? '扣除基準（複製）' : deductionTitle }}</h2><small>{{ activeGroup?.groupNo }} · {{ activeGroup?.groupName }}</small></div><button class="x" @click="panel = ''">×</button></header><div class="deduction-form"><label>NormalTimes<input v-model.number="deductionEditor.normalTimes" type="number" min="1"></label><label>DeductionTimes<input v-model.number="deductionEditor.deductionTimes" type="number" min="1"></label><label>CurPass<input v-model.number="deductionEditor.curPass" type="number" min="0"></label><fieldset><legend>CurStatus</legend><label v-for="(name, index) in ['一般', '扣除', '扣除重測']" :key="name"><input v-model.number="deductionEditor.curStatus" type="radio" :value="index">{{ name }}</label></fieldset><div class="factor-fields"><label v-for="factor in factors" :key="factor.factorNo">{{ factor.factorName }}<select v-if="factor.parameterType !== 2" v-model="deductionEditor.factorValues[factor.factorNo]"><option value="*">*（不限）</option><option v-for="option in factor.options" :key="option" :value="option">{{ option }}</option></select><input v-else v-model="deductionEditor.factorValues[factor.factorNo]" placeholder="* 代表不限"></label></div></div><p class="hint">每個目前啟用的要因皆須指定值；ParameterType=2 可輸入文字，其他類型須從來源清單選取；* 代表不限。儲存時由 API 檢查要因組合是否重複。</p><p v-if="error" class="modal-error">{{ error }}</p><footer><button class="pmi-btn primary" :disabled="busy" @click="saveDeduction">確定</button><button class="pmi-btn" @click="panel = ''">關閉</button></footer></section></div>

    <div v-if="panel === 'parameters'" class="pmi-backdrop" @click.self="panel = ''"><section class="pmi-dialog" role="dialog" aria-modal="true"><header><h2>Parameter｜{{ activeGroup?.groupNo }}</h2><button class="x" @click="panel = ''">×</button></header><table class="pmi-grid"><thead><tr><th>參數編號</th><th>參數名稱</th><th>值</th></tr></thead><tbody><tr v-for="row in parameterRows" :key="row.parameterNo"><td>{{ row.parameterNo }}</td><td>{{ row.parameterName }}</td><td><input v-model="row.value"></td></tr></tbody></table><footer><button class="pmi-btn primary" @click="saveParameters">確定</button><button class="pmi-btn" @click="panel = ''">關閉</button></footer></section></div>
    <div v-if="panel === 'operations'" class="pmi-backdrop" @click.self="panel = ''"><section class="pmi-dialog op-dialog" role="dialog" aria-modal="true"><header><div><h2>QC_ItemOp｜{{ activeGroup?.groupNo }} {{ activeGroup?.groupName }}</h2><small>QCType=4；只有已核准群組可編輯。InLine 僅適用 LotIsValid=是。</small></div><button class="x" @click="panel = ''">×</button></header><div class="op-transfer"><div class="op-list"><h3>可選作業站</h3><div class="op-list-body"><label v-for="row in availableOperations" :key="row.opNo"><input v-model="selectedCandidateNos" type="checkbox" :value="row.opNo"><b>{{ row.opNo }}</b><span>{{ row.opName }}</span></label><p v-if="!availableOperations.length" class="empty">沒有可加入的作業站</p></div></div><div class="op-transfer-buttons"><button class="pmi-btn" :disabled="!selectedCandidateNos.length || activeGroup?.lotIsValid !== 1" title="LotIsValid=1 才可加入 InLine" @click="assignOperations(1)">加入 InLine →</button><button class="pmi-btn" :disabled="!selectedCandidateNos.length" @click="assignOperations(0)">加入 OffLine →</button><button class="pmi-btn" :disabled="!selectedOperationNos.length" @click="removeAssignedOperations">← 移除</button></div><div class="op-list"><h3>已選作業站</h3><div class="op-list-body"><table class="pmi-grid"><thead><tr><th></th><th>ExecuteStatus</th><th>OpNo</th><th>OpName</th><th>INSPTYPE_FAI</th><th>INSPTYPE_IPQC</th><th>INSPTYPE_PQC</th></tr></thead><tbody><tr v-for="row in assignedOperations" :key="row.opNo"><td><input v-model="selectedOperationNos" type="checkbox" :value="row.opNo"></td><td>{{ row.inline === 1 ? 'InLine' : 'OffLine' }}</td><td>{{ row.opNo }}</td><td>{{ row.opName }}</td><td><input v-model="row.inspTypeFai" type="checkbox" :disabled="row.inline !== 1"></td><td><input v-model="row.inspTypeIpqc" type="checkbox" :disabled="row.inline !== 1"></td><td><input v-model="row.inspTypePqc" type="checkbox" :disabled="row.inline !== 1"></td></tr><tr v-if="!assignedOperations.length"><td colspan="7" class="empty">尚未設定作業站</td></tr></tbody></table></div></div></div><p class="hint">每個 InLine 作業站至少選擇一種 FAI、IPQC 或 PQC 檢驗時機；OffLine 不設定檢驗時機。</p><p v-if="error" class="modal-error">{{ error }}</p><footer><button class="pmi-btn primary" @click="saveOperations">確定</button><button class="pmi-btn" @click="panel = ''">關閉</button></footer></section></div>
    <div v-if="confirmDelete" class="pmi-backdrop"><section class="pmi-dialog confirm"><h2>刪除群組</h2><p>未簽核／不使用資料會硬刪；已簽核資料改為「不使用」；簽核中資料不允許刪除。硬刪前會檢查在線使用狀況，確定繼續？</p><footer><button class="pmi-btn primary" :disabled="busy" @click="deleteGroups">確定</button><button class="pmi-btn" @click="confirmDelete = false">取消</button></footer></section></div>
  </section>
</template>

<style scoped>
.pmi-page{height:100%;min-height:620px;padding:10px 12px;display:flex;flex-direction:column;gap:7px;background:#fff;color:#333;font:13px "Segoe UI","Microsoft JhengHei",sans-serif}.pmi-heading,.pmi-toolbar{display:flex;align-items:center;gap:6px}.pmi-heading{justify-content:space-between}.pmi-heading h1{margin:0;font-size:20px;font-weight:500}.pmi-heading small,.pmi-footer{color:#777;font-size:11px}.pmi-query{display:flex;align-items:center;gap:9px;min-height:38px;padding:4px 8px;border:1px solid #d0d0d0;flex-wrap:wrap}.pmi-query label{display:flex;align-items:center;gap:3px}.pmi-query input[type=radio],input[type=checkbox],input[type=radio]{accent-color:#d99522}.pmi-search{width:185px;min-height:27px;padding:3px 6px;border:1px solid #999}.pmi-count{margin-left:auto;color:#777;font-size:11px}.pmi-btn{min-height:29px;padding:4px 10px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#f3f3f3);color:#333;font:inherit;white-space:nowrap;cursor:pointer}.pmi-btn:hover:not(:disabled){background:#fff4d1;border-color:#d2a44c}.pmi-btn:disabled{opacity:.48;cursor:not-allowed}.pmi-btn.primary{background:#f6a329;border-color:#dc9027;color:white}.pmi-box{position:relative;min-width:0;margin:0;padding:12px 7px 5px;border:1px solid #bcbcbc;display:flex;flex-direction:column}.pmi-box legend{padding:0 5px;color:#555}.pmi-master{height:48%;min-height:255px}.pmi-toolbar{min-height:32px;flex-wrap:wrap;margin-bottom:4px}.pmi-toolbar-space{flex:1}.pmi-grid-wrap{min-height:0;flex:1;overflow:auto;border:1px solid #bdbdbd}.pmi-grid{width:100%;border-collapse:collapse;font-size:12px;white-space:nowrap}.pmi-grid th{position:sticky;top:0;z-index:1;padding:5px 7px;border:1px solid #d6dfe4;background:#45a6e5;color:#fff;text-align:left;font-weight:500}.pmi-grid td{height:25px;padding:2px 7px;border:1px solid #ccc;max-width:280px;overflow:hidden;text-overflow:ellipsis}.pmi-grid tbody tr{cursor:pointer}.pmi-grid tbody tr:nth-child(even){background:#fffaf0}.pmi-grid tbody tr:hover{background:#fff2c3}.pmi-grid tbody tr.active{background:#ffe79a}.pmi-grid .check{width:26px;text-align:center;padding:2px}.pmi-footer{padding:4px 2px 0}.empty{text-align:center!important;color:#888;padding:12px!important}.pmi-lower{display:grid;grid-template-columns:minmax(0,1fr) minmax(0,1.15fr);gap:8px;flex:1;min-height:0}.pmi-lower>.pmi-box{min-height:160px}.pmi-alert{display:flex;justify-content:space-between;padding:6px 9px;border:1px solid #ddd}.pmi-alert button{border:0;background:transparent;font-size:18px}.pmi-alert.error{background:#fff0eb;color:#8e3321}.pmi-alert.success{background:#f1f7eb;color:#4f6e35}.pmi-backdrop{position:fixed;inset:0;z-index:100;display:grid;place-items:center;padding:18px;background:#1d242b66}.pmi-dialog{width:min(650px,100%);max-height:calc(100vh - 36px);overflow:auto;padding:12px;border:1px solid #aaa;background:white;box-shadow:0 10px 32px #0003}.pmi-dialog.wide-dialog{width:min(920px,100%)}.pmi-dialog header{display:flex;align-items:center;justify-content:space-between;gap:10px;padding-bottom:8px;border-bottom:1px solid #ddd}.pmi-dialog h2{margin:0;font-size:17px;font-weight:600}.pmi-dialog header small{display:block;margin-top:3px;color:#777}.pmi-dialog .x{border:0;background:transparent;font-size:23px;color:#555}.pmi-form{display:grid;grid-template-columns:1fr 1fr;gap:9px 14px;padding:12px 2px}.pmi-form>label{display:grid;grid-template-columns:125px minmax(0,1fr);align-items:center;gap:6px}.pmi-form input,.pmi-form select,.pmi-form textarea,.deduction-form input,.deduction-form select{min-width:0;min-height:28px;padding:3px 6px;border:1px solid #999;font:inherit}.pmi-form .yellow{background:#fff0b3}.pmi-form .wide{grid-column:1/-1}.pmi-form .wide textarea{grid-column:2;resize:vertical}.pmi-form .check-label{display:flex;gap:7px}.pmi-form .check-label input{min-height:auto}.pmi-form small{grid-column:2;color:#777}.audit{grid-column:1/-1;display:grid;grid-template-columns:1fr 1fr;gap:7px;padding:8px;border:1px solid #ddd;background:#fafafa}.audit label{display:grid;grid-template-columns:62px 1fr;gap:5px;align-items:center;color:#666}.audit input{background:#eee;color:#666}.pmi-dialog footer{display:flex;justify-content:flex-end;gap:7px;margin-top:10px}.modal-error{color:#a03220}.picker{display:grid;grid-template-columns:minmax(0,1fr) 88px minmax(0,1fr);gap:9px;margin-top:10px}.picker h3{margin:0 0 6px;font-size:13px;font-weight:500}.picker>div:first-child,.picker>div:last-child{min-width:0;max-height:52vh;overflow:auto;border:1px solid #ccc}.pick-row{display:grid;grid-template-columns:minmax(110px,.8fr) minmax(0,1.4fr);gap:7px;width:100%;padding:7px;border:0;border-bottom:1px solid #eee;background:#fff;text-align:left;font:inherit;color:#333}.pick-row.chosen{background:#ffe79a}.move{display:flex;flex-direction:column;justify-content:center;gap:7px}.detail-draft{display:grid;grid-template-columns:minmax(0,1fr) 66px;align-items:center;border-bottom:1px solid #eee}.detail-draft label{white-space:nowrap}.detail-draft input{accent-color:#d99522}.order{display:flex;gap:6px;padding:7px}.hint{color:#777;font-size:11px}.deduction-form{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:10px;padding:12px 2px}.deduction-form>label,.factor-fields>label{display:grid;gap:4px}.deduction-form fieldset{grid-column:1/-1;display:flex;gap:14px;border:1px solid #bbb}.deduction-form fieldset label{display:flex;align-items:center;gap:4px}.factor-fields{grid-column:1/-1;display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:9px}.full{width:100%;margin-top:10px}.confirm{width:min(420px,100%)}.confirm p{line-height:1.6;color:#555}
@media(max-width:900px){.pmi-lower{grid-template-columns:1fr;grid-template-rows:1fr 1fr}.pmi-master{height:45%}.pmi-page{min-height:780px}.pmi-form{grid-template-columns:1fr}.pmi-form .wide,.audit{grid-column:auto}.factor-fields{grid-template-columns:1fr 1fr}}
@media(max-width:600px){.pmi-query{gap:5px}.pmi-count{margin-left:0}.pmi-search{width:125px}.picker{grid-template-columns:1fr}.move{flex-direction:row}.deduction-form,.factor-fields{grid-template-columns:1fr}.pmi-page{padding:6px}.pmi-lower{min-height:420px}}
.pmi-page{overflow:auto}.pmi-master{flex:0 0 43%;height:auto;min-height:245px}.pmi-master-layout{display:grid;grid-template-columns:minmax(0,1fr) 96px;gap:7px;flex:1;min-height:0}.pmi-rail{display:flex;flex-direction:column;gap:4px;overflow:auto}.pmi-rail .pmi-btn{min-height:29px;padding:3px 4px}.pmi-lower{display:flex;flex:1;flex-direction:column;min-height:280px;gap:6px}.pmi-detail-box{flex:0 0 34%;min-height:115px}.pmi-deduction-box{flex:1;min-height:150px}.pmi-deduction-layout{display:grid;grid-template-columns:minmax(0,1fr) 88px;gap:7px;flex:1;min-height:0}.pmi-deduction-table{display:flex;flex-direction:column;min-width:0;min-height:0;gap:2px}.pmi-deduction-table .pmi-grid-wrap{flex:1}.pmi-radio{grid-column:1/-1;display:flex;align-items:center;gap:12px;margin:0;padding:5px 9px;border:1px solid #bbb}.pmi-radio legend{padding:0 4px;color:#666;font-size:11px}.pmi-radio label{display:flex;align-items:center;gap:4px}.pmi-radio input{accent-color:#d99522}
@media(max-width:900px){.pmi-master{flex-basis:43%}.pmi-master-layout{grid-template-columns:minmax(0,1fr) 82px}.pmi-rail .pmi-btn{font-size:11px}.pmi-lower{grid-template-columns:none;grid-template-rows:none}}
@media(max-width:600px){.pmi-master-layout{grid-template-columns:minmax(0,1fr) 72px;gap:4px}.pmi-rail .pmi-btn{font-size:10px;padding:2px}.pmi-deduction-layout{grid-template-columns:minmax(0,1fr) 68px}.pmi-detail-box{flex-basis:30%}}
.op-dialog{width:min(1180px,98vw)}.op-dialog>header{align-items:flex-start}.op-dialog header small{color:#777}.op-transfer{display:grid;grid-template-columns:minmax(200px,.85fr) 145px minmax(0,1.6fr);gap:8px;min-height:360px;margin-top:10px}.op-list{display:flex;flex-direction:column;min-width:0;min-height:0;border:1px solid #c8c5bd}.op-list h3{margin:0;padding:6px 8px;border-bottom:1px solid #ccc;background:#f3f0e8;font-size:13px;font-weight:500}.op-list-body{min-height:0;overflow:auto;flex:1}.op-list-body>label{display:grid;grid-template-columns:20px minmax(90px,.8fr) minmax(0,1fr);align-items:center;gap:5px;padding:6px 7px;border-bottom:1px solid #eee}.op-transfer-buttons{display:flex;flex-direction:column;justify-content:center;gap:8px}.op-transfer-buttons .pmi-btn{white-space:normal}.op-list-body .pmi-grid th{position:static}.op-list-body .pmi-grid td{text-align:center}.op-list-body .pmi-grid td:nth-child(2),.op-list-body .pmi-grid td:nth-child(3),.op-list-body .pmi-grid td:nth-child(4){text-align:left}.op-dialog input[type=checkbox]{accent-color:#d99522}.op-dialog .hint{margin-bottom:0}
@media(max-width:900px){.op-transfer{grid-template-columns:1fr;min-height:0}.op-list{min-height:190px}.op-transfer-buttons{display:grid;grid-template-columns:repeat(3,minmax(0,1fr))}}
@media(max-width:600px){.op-transfer-buttons{grid-template-columns:1fr}.op-list-body .pmi-grid{font-size:10px}.op-list-body .pmi-grid th,.op-list-body .pmi-grid td{padding:4px}}
</style>
