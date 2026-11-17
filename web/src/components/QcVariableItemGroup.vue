<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type IssueState = 0 | 1 | 2 | -1
type Mode = 'add' | 'edit' | 'copy'
type PanelMode = 'add-items' | 'parameters' | 'operations' | 'script' | 'memo' | ''

interface GroupRow {
  groupNo: string; groupName: string; lotIsValid: number; componentCollect: number; planNo: string
  formName: string; webUrl: string; unionErf: number; issueState: IssueState; description: string
  creator: string; createDate: string; reviser: string; reviseDate: string; cursorMode: number
  qcClass: string; getFilePath: string; executionFile: string; dataStamp: number
}
interface DetailRow { groupNo: string; qcItemNo: string; qcItemName: string; showOrder: number; scriptIsValid: boolean; componentCollect: number; issueState: number }
interface QcItem { qcItemNo: string; qcItemName: string; componentCollect: number; issueState: number }
interface Plan { planNo: string; planName: string; planType: number; componentCollect: number; issueState: number }
interface Operation { opNo: string; opName: string; opOrder: number }
interface InterfaceOption { name: string; file: string }
interface MemoParameter { parameterNo: string; parameterName: string; checked: boolean }
interface ScriptRow { groupNo: string; qcItemNo: string; script: string; description: string }
interface Store {
  groups: GroupRow[]; details: DetailRow[]; qcItems: QcItem[]; plans: Plan[]; waferPlans: Plan[]
  operations: Operation[]; parameters: MemoParameter[]; scripts: ScriptRow[]; memos: Record<string, string[]>
  opBindings: Record<string, string[]>; groupParameters: Record<string, Array<{ parameterNo: string; parameterName: string; specType: number; value: string }>>
  issueFlowEnabled: boolean
}

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), {
  currentUserNo: '', developmentPreview: false,
})
const emit = defineEmits<{ close: [] }>()
const basePath = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mockMode = computed(() => import.meta.env.DEV && props.developmentPreview)
const storeKey = 'ymes-dev-mock:qc-variable-item-group:v1'

const rows = ref<GroupRow[]>([])
const details = ref<DetailRow[]>([])
const qcItems = ref<QcItem[]>([])
const plans = ref<Plan[]>([])
const waferPlans = ref<Plan[]>([])
const operations = ref<Operation[]>([])
const interfaceOptions = ref<InterfaceOption[]>([])
const qcClassOptions = ref<string[]>([])
const parameters = ref<MemoParameter[]>([])
const scripts = ref<ScriptRow[]>([])
const memos = ref<Record<string, string[]>>({})
const opBindings = ref<Record<string, string[]>>({})
const groupParameters = ref<Record<string, Array<{ parameterNo: string; parameterName: string; specType: number; value: string }>>>({})
const issueFlowEnabled = ref(false)
const unionPlan = ref(false)
const selectedGroupNo = ref('')
const selectedGroupNos = ref<string[]>([])
const selectedDetailNos = ref<string[]>([])
const issueStateFilter = ref<IssueState>(0)
const groupNoFilter = ref('')
const groupNoInput = ref('')
const loading = ref(false)
const saving = ref(false)
const message = ref('')
const error = ref('')
const editorOpen = ref(false)
const editorMode = ref<Mode>('add')
const panelMode = ref<PanelMode>('')
const pickerQuery = ref('')
const addItemSelection = ref<string[]>([])
const pendingDetails = ref<DetailRow[]>([])
const pendingSelection = ref<string[]>([])
const deleteConfirmOpen = ref(false)
const deleteTargets = ref<GroupRow[]>([])
const editor = ref<GroupRow>(blankGroup())
const scriptEditor = ref<ScriptRow>({ groupNo: '', qcItemNo: '', script: '', description: '' })
const parameterEditor = ref<{ rows: Array<{ parameterNo: string; parameterName: string; specType: number; value: string }> }>({ rows: [] })

const filteredRows = computed(() => rows.value
  .filter(row => row.issueState === issueStateFilter.value)
  .filter(row => !groupNoFilter.value || row.groupNo.toLowerCase().includes(groupNoFilter.value.trim().toLowerCase()))
  .slice().sort((a, b) => a.groupNo.localeCompare(b.groupNo)))
const activeGroup = computed(() => rows.value.find(row => row.groupNo === selectedGroupNo.value) ?? null)
const activeDetail = computed(() => details.value.find(row => selectedDetailNos.value.includes(row.qcItemNo)) ?? null)
const selectedGroups = computed(() => rows.value.filter(row => selectedGroupNos.value.includes(row.groupNo)))
const detailSelection = computed(() => details.value.filter(row => selectedDetailNos.value.includes(row.qcItemNo)))
const editorTitle = computed(() => ({ add: '計量品管項目群組定義（新增）', edit: '計量品管項目群組定義（修改）', copy: '計量品管項目群組定義（複製）' }[editorMode.value]))
const candidates = computed(() => {
  const query = pickerQuery.value.trim().toLowerCase()
  const existing = new Set(pendingDetails.value.map(row => row.qcItemNo))
  return qcItems.value.filter(item => item.issueState === 2 && item.componentCollect === (activeGroup.value?.componentCollect ?? 0) && !existing.has(item.qcItemNo))
    .filter(item => !query || `${item.qcItemNo} ${item.qcItemName}`.toLowerCase().includes(query))
})
const visiblePlans = computed(() => editor.value.componentCollect === 1 ? waferPlans.value : plans.value)
const modalTitle = computed(() => ({ 'add-items': '新增品管項目', parameters: 'Parameter｜品管參數', operations: 'Op｜作業站對應', script: 'Calculate Script｜計算腳本', memo: 'Memo｜備註欄位設定', '': '' }[panelMode.value]))
const issueStateOptions = [{ value: 0 as const, label: '未凍結' }, { value: 1 as const, label: '簽核中' }, { value: 2 as const, label: '已核准' }, { value: -1 as const, label: '未使用' }]

function blankGroup(): GroupRow {
  return { groupNo: '', groupName: '', lotIsValid: 0, componentCollect: 0, planNo: '', formName: '', webUrl: '', unionErf: 1,
    issueState: 0, description: '', creator: '', createDate: '', reviser: '', reviseDate: '', cursorMode: 0, qcClass: '', getFilePath: '', executionFile: '', dataStamp: 0 }
}
function auditDate(): string {
  const d = new Date()
  return `${d.getFullYear()}/${String(d.getMonth() + 1).padStart(2, '0')}/${String(d.getDate()).padStart(2, '0')} ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}:${String(d.getSeconds()).padStart(2, '0')}`
}
function seedStore(): Store {
  const audit = { creator: 'DEV001', createDate: '2026/11/17 08:30:00', reviser: 'DEV001', reviseDate: '2026/11/17 09:10:00', dataStamp: 1 }
  const qcItems: QcItem[] = [
    { qcItemNo: 'DEV-QCV-001', qcItemName: '尺寸量測值', componentCollect: 0, issueState: 2 },
    { qcItemNo: 'DEV-QCV-002', qcItemName: '表面粗糙度', componentCollect: 0, issueState: 2 },
    { qcItemNo: 'DEV-QCV-003', qcItemName: '膜厚量測值', componentCollect: 1, issueState: 2 },
    { qcItemNo: 'DEV-QCV-004', qcItemName: '待核准品管項目', componentCollect: 0, issueState: 0 },
    { qcItemNo: 'DEV-QCV-005', qcItemName: '外觀檢查數值', componentCollect: 0, issueState: 2 },
  ]
  const plans: Plan[] = [
    { planNo: 'DEV-AQL-01', planName: '一般抽樣計畫 A', planType: 1, componentCollect: 0, issueState: 2 },
    { planNo: 'DEV-AQL-02', planName: '一般抽樣計畫 B', planType: 2, componentCollect: 0, issueState: 2 },
  ]
  const waferPlans: Plan[] = [{ planNo: 'DEV-WAFER-01', planName: 'Wafer 抽樣計畫 A', planType: 3, componentCollect: 1, issueState: 2 }]
  const groups: GroupRow[] = [
    { ...blankGroup(), groupNo: 'QC-GRP-001', groupName: '外觀及尺寸檢驗', lotIsValid: 1, planNo: 'DEV-AQL-01', qcClass: 'CLASS-A', description: '包裝前量測項目', ...audit },
    { ...blankGroup(), groupNo: 'QC-GRP-002', groupName: 'Wafer 厚度量測', componentCollect: 1, lotIsValid: 1, planNo: 'DEV-WAFER-01', cursorMode: 1, issueState: 2, ...audit },
    { ...blankGroup(), groupNo: 'QC-GRP-003', groupName: '等待簽核群組', issueState: 1, ...audit },
    { ...blankGroup(), groupNo: 'QC-GRP-004', groupName: '停用群組示例', issueState: -1, ...audit },
  ]
  const details: DetailRow[] = [
    { groupNo: 'QC-GRP-001', qcItemNo: 'DEV-QCV-001', qcItemName: '尺寸量測值', showOrder: 0, scriptIsValid: true, componentCollect: 0, issueState: 2 },
    { groupNo: 'QC-GRP-001', qcItemNo: 'DEV-QCV-002', qcItemName: '表面粗糙度', showOrder: 1, scriptIsValid: false, componentCollect: 0, issueState: 2 },
    { groupNo: 'QC-GRP-001', qcItemNo: 'DEV-QCV-005', qcItemName: '外觀檢查數值', showOrder: 2, scriptIsValid: false, componentCollect: 0, issueState: 2 },
    { groupNo: 'QC-GRP-002', qcItemNo: 'DEV-QCV-003', qcItemName: '膜厚量測值', showOrder: 0, scriptIsValid: false, componentCollect: 1, issueState: 2 },
  ]
  return {
    groups, details, qcItems, plans, waferPlans,
    operations: [{ opNo: 'OP-ASSY-10', opName: '組裝量測', opOrder: 10 }, { opNo: 'OP-PACK-20', opName: '包裝檢查', opOrder: 20 }, { opNo: 'OP-WAFER-30', opName: '晶圓量測', opOrder: 30 }],
    parameters: [{ parameterNo: 'TEMP', parameterName: '環境溫度', checked: true }, { parameterNo: 'HUMIDITY', parameterName: '環境濕度', checked: false }, { parameterNo: 'MACHINE', parameterName: '量測機台', checked: false }, { parameterNo: 'OPERATOR', parameterName: '作業人員', checked: false }],
    scripts: [{ groupNo: 'QC-GRP-001', qcItemNo: 'DEV-QCV-001', script: 'VALUE >= LSL && VALUE <= USL', description: '量測值須介於規格上下限' }],
    memos: { 'QC-GRP-001': ['TEMP'] }, opBindings: { 'QC-GRP-002': ['OP-WAFER-30'] },
    groupParameters: { 'QC-GRP-001': [{ parameterNo: 'TEMP', parameterName: '環境溫度', specType: 0, value: '25' }] }, issueFlowEnabled: true,
  }
}
function readStore(): Store {
  try {
    const raw = window.localStorage.getItem(storeKey)
    if (raw) {
      const parsed = JSON.parse(raw) as Store
      if (Array.isArray(parsed.groups) && Array.isArray(parsed.details) && Array.isArray(parsed.qcItems)) return parsed
    }
  } catch { /* replace invalid dev fixture */ }
  const seeded = seedStore()
  writeStore(seeded)
  return seeded
}
function writeStore(value: Store): void { window.localStorage.setItem(storeKey, JSON.stringify(value)) }
function hydrateStore(value: Store): void {
  rows.value = value.groups; details.value = value.details; qcItems.value = value.qcItems; plans.value = value.plans
  waferPlans.value = value.waferPlans; operations.value = value.operations; parameters.value = value.parameters
  scripts.value = value.scripts; memos.value = value.memos; opBindings.value = value.opBindings; issueFlowEnabled.value = value.issueFlowEnabled
  groupParameters.value = value.groupParameters ?? {}
}
function persistMock(): void {
  if (!mockMode.value) return
  writeStore({ groups: rows.value, details: details.value, qcItems: qcItems.value, plans: plans.value, waferPlans: waferPlans.value,
    operations: operations.value, parameters: parameters.value, scripts: scripts.value, memos: memos.value, opBindings: opBindings.value,
    groupParameters: groupParameters.value, issueFlowEnabled: issueFlowEnabled.value })
}
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init, headers })
  if (response.status === 401) { window.location.assign(basePath + '/'); throw new Error('登入逾時，請重新登入。') }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; title?: string } | null
    throw new Error(problem?.detail || problem?.title || `API 回應失敗（${response.status}）。`)
  }
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}
function listOf<T>(payload: T[] | { items?: T[]; rows?: T[]; data?: T[] }): T[] {
  return Array.isArray(payload) ? payload : payload.items ?? payload.rows ?? payload.data ?? []
}
function normalizeGroup(value: Record<string, unknown>): GroupRow {
  const str = (key: string) => String(value[key] ?? '')
  const num = (key: string, fallback = 0) => Number(value[key] ?? fallback)
  return { groupNo: str('groupNo') || str('GROUPNO'), groupName: str('groupName') || str('GROUPNAME'), lotIsValid: num('lotIsValid', num('LOTISVALID')),
    componentCollect: num('componentCollect', num('COMPONENTCOLLECT')), planNo: str('planNo') || str('PLANNO'), formName: str('formName') || str('FORMNAME'),
    webUrl: str('webUrl') || str('WEBURL'), unionErf: num('unionErf', num('UNIONERF')), issueState: num('issueState', num('ISSUESTATE')) as IssueState,
    description: str('description') || str('DESCRIPTION'), creator: str('creator') || str('CREATOR'), createDate: str('createDate') || str('CREATEDATE'),
    reviser: str('reviser') || str('REVISER'), reviseDate: str('reviseDate') || str('REVISEDATE'), cursorMode: num('cursorMode', num('CURSORMODE')),
    qcClass: str('qcClass') || str('QCCLASS'), getFilePath: str('getFilePath') || str('GETFILEPATH'), executionFile: str('executionFile') || str('EXECUTIONFILE'),
    dataStamp: num('dataStamp', num('DATASTAMP')) }
}
function normalizeDetail(value: Record<string, unknown>): DetailRow {
  return { groupNo: String(value.groupNo ?? value.GROUPNO ?? ''), qcItemNo: String(value.qcItemNo ?? value.QCITEMNO ?? ''),
    qcItemName: String(value.qcItemName ?? value.QCITEMNAME ?? ''), showOrder: Number(value.showOrder ?? value.SHOWORDER ?? 0),
    scriptIsValid: Boolean(value.scriptIsValid ?? value.SCRIPTISVALID ?? false), componentCollect: Number(value.componentCollect ?? value.COMPONENTCOLLECT ?? 0),
    issueState: Number(value.issueState ?? value.ISSUESTATE ?? 2) }
}
async function loadGroups(): Promise<void> {
  loading.value = true; error.value = ''
  try {
    if (mockMode.value) { hydrateStore(readStore()) }
    else { rows.value = listOf(await request<GroupRow[] | { items?: GroupRow[] }>('api/qc/item-groups?functionNo=QCITEM_G')).map(item => normalizeGroup(item as unknown as Record<string, unknown>)) }
    if (!filteredRows.value.some(row => row.groupNo === selectedGroupNo.value)) {
      selectedGroupNo.value = filteredRows.value[0]?.groupNo ?? ''
      selectedGroupNos.value = selectedGroupNo.value ? [selectedGroupNo.value] : []
    }
    await loadDetails(selectedGroupNo.value)
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入計量品管群組失敗。' }
  finally { loading.value = false }
}
async function loadLookups(): Promise<void> {
  if (mockMode.value) {
    hydrateStore(readStore())
    interfaceOptions.value = [{ name: 'DEV_QC_MEASURE_FORM', file: 'DEV_SC_Var_MeasureData.dll' }, { name: 'DEV_QC_GROUP_FORM', file: 'DEV_QC_Group.dll' }]
    qcClassOptions.value = ['DEV-QC-CLASS-A', 'DEV-QC-CLASS-B']
    return
  }
  const [itemData, planData, waferData, opData, parameterData, interfaceData, qcClassData] = await Promise.all([
    request<QcItem[]>('api/qc/variable-qc-items?issueState=2'), request<Plan[]>('api/qc/sampling-plans?componentCollect=0&issueState=2'),
    request<Plan[]>('api/qc/sampling-plans?componentCollect=1&issueState=2'), request<Operation[]>('api/process/operations'),
    request<Array<{ parameterNo: string; parameterName: string }>>('api/system/mes-parameters?issueState=2&parameterClass=1'),
    request<InterfaceOption[]>('api/qc/item-groups/options/winspc-interfaces'), request<string[]>('api/qc/item-groups/options/qc-classes'),
  ])
  qcItems.value = listOf(itemData as QcItem[]).map(item => ({ ...item, issueState: Number(item.issueState) }))
  plans.value = listOf(planData as Plan[]); waferPlans.value = listOf(waferData as Plan[]); operations.value = listOf(opData as Operation[])
  parameters.value = listOf(parameterData as Array<{ parameterNo: string; parameterName: string }>).map(item => ({ ...item, checked: false }))
  interfaceOptions.value = listOf(interfaceData as InterfaceOption[])
  qcClassOptions.value = listOf(qcClassData as string[])
}
async function loadDetails(groupNo: string): Promise<void> {
  selectedGroupNo.value = groupNo
  selectedDetailNos.value = []
  if (!groupNo) { details.value = []; return }
  try {
    if (mockMode.value) {
      const fixture = readStore(); hydrateStore(fixture)
      details.value = fixture.details.filter(row => row.groupNo === groupNo).slice().sort((a, b) => a.showOrder - b.showOrder)
      return
    }
    const data = await request<DetailRow[] | { items?: DetailRow[] }>(`api/qc/item-groups/${encodeURIComponent(groupNo)}/details`)
    details.value = listOf(data).map(item => normalizeDetail(item as unknown as Record<string, unknown>)).sort((a, b) => a.showOrder - b.showOrder)
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入群組品管明細失敗。'; details.value = [] }
}
function selectGroup(row: GroupRow): void {
  selectedGroupNo.value = row.groupNo
  if (!selectedGroupNos.value.includes(row.groupNo)) selectedGroupNos.value = [row.groupNo]
  void loadDetails(row.groupNo)
}
function toggleGroupSelection(row: GroupRow): void {
  selectedGroupNos.value = selectedGroupNos.value.includes(row.groupNo)
    ? selectedGroupNos.value.filter(key => key !== row.groupNo) : [...selectedGroupNos.value, row.groupNo]
}
function selectStatus(value: IssueState): void {
  issueStateFilter.value = value
  selectedGroupNos.value = []
  selectedGroupNo.value = ''
  void loadGroups()
}
function searchGroups(): void { groupNoFilter.value = groupNoInput.value; void loadGroups() }
function setAllGroups(event: Event): void { selectedGroupNos.value = (event.target as HTMLInputElement).checked ? filteredRows.value.map(row => row.groupNo) : [] }
function setAllDetails(event: Event): void { selectedDetailNos.value = (event.target as HTMLInputElement).checked ? details.value.map(row => row.qcItemNo) : [] }
function openEditor(mode: Mode): void {
  error.value = ''; message.value = ''
  if (mode !== 'add' && !activeGroup.value) { error.value = '請先選取一筆群組資料。'; return }
  editorMode.value = mode
  if (mode === 'add') { editor.value = { ...blankGroup(), creator: props.currentUserNo || 'DEV001', createDate: auditDate() }; unionPlan.value = false }
  else {
    const source = activeGroup.value!
    editor.value = { ...source, groupNo: mode === 'copy' ? '' : source.groupNo, groupName: mode === 'copy' ? `${source.groupName} - 複本` : source.groupName,
      issueState: 0, creator: mode === 'copy' ? (props.currentUserNo || 'DEV001') : source.creator, createDate: mode === 'copy' ? auditDate() : source.createDate,
      reviser: mode === 'edit' ? (props.currentUserNo || 'DEV001') : '', reviseDate: mode === 'edit' ? auditDate() : '', dataStamp: source.dataStamp }
    unionPlan.value = !!source.planNo
  }
  editorOpen.value = true
}
async function saveEditor(closeAfter = true): Promise<void> {
  const value = editor.value
  if (!value.groupNo.trim()) { error.value = '群組編號為必填。'; return }
  if (!value.groupName.trim()) { error.value = '群組名稱為必填。'; return }
  if (value.lotIsValid !== 0 && value.lotIsValid !== 1) { error.value = '請選擇 Lot Is Valid。'; return }
  if (value.componentCollect !== 0 && value.componentCollect !== 1) { error.value = '請選擇 Component Collect。'; return }
  if (value.cursorMode !== 0 && value.cursorMode !== 1) { error.value = '請選擇 Cursor Mode。'; return }
  if (unionPlan.value && !value.planNo) { error.value = '已勾選 Union Plan，請選擇抽樣計畫。'; return }
  if (!unionPlan.value) value.planNo = ''
  if (value.planNo && !visiblePlans.value.some(plan => plan.planNo === value.planNo)) { error.value = '請從符合元件類型的已核准抽樣計畫中選擇。'; return }
  if (editorMode.value !== 'edit' && rows.value.some(row => row.groupNo.toLowerCase() === value.groupNo.trim().toLowerCase())) { error.value = '群組編號已存在。'; return }
  saving.value = true
  try {
    if (mockMode.value) {
      const saved = { ...value, groupNo: value.groupNo.trim(), groupName: value.groupName.trim(), dataStamp: value.dataStamp + 1 }
      if (editorMode.value === 'edit') rows.value = rows.value.map(row => row.groupNo === saved.groupNo ? saved : row)
      else {
        rows.value = [...rows.value, saved]
        if (editorMode.value === 'copy' && activeGroup.value) {
          const sourceNo = activeGroup.value.groupNo
          const copiedDetails = details.value.filter(row => row.groupNo === sourceNo).map((row, index) => ({ ...row, groupNo: saved.groupNo, showOrder: index }))
          details.value = [...details.value, ...copiedDetails]
        }
      }
      persistMock()
    } else if (editorMode.value === 'edit') {
      await request(`api/qc/item-groups/${encodeURIComponent(value.groupNo)}`, { method: 'PUT', body: JSON.stringify(value) })
    } else {
      const saved = await request<GroupRow>('api/qc/item-groups', { method: 'POST', body: JSON.stringify(value) })
      if (editorMode.value === 'copy' && activeGroup.value) await request(`api/qc/item-groups/${encodeURIComponent(activeGroup.value.groupNo)}/copy-details`, { method: 'POST', body: JSON.stringify({ targetGroupNo: value.groupNo }) })
      if (saved?.groupNo) value.groupNo = saved.groupNo
    }
    const key = value.groupNo
    if (closeAfter) editorOpen.value = false
    await loadGroups()
    selectedGroupNo.value = key
    selectedGroupNos.value = [key]
    await loadDetails(key)
    if (closeAfter) message.value = '群組資料已儲存。'
    else { editorMode.value = 'edit'; editor.value = { ...(rows.value.find(row => row.groupNo === key) ?? value) }; unionPlan.value = !!editor.value.planNo }
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '儲存群組失敗。' }
  finally { saving.value = false }
}
function openDelete(): void {
  const targets = selectedGroups.value.length ? selectedGroups.value : activeGroup.value ? [activeGroup.value] : []
  if (!targets.length) { error.value = '請先選取要刪除的群組。'; return }
  deleteTargets.value = targets
  deleteConfirmOpen.value = true
}
async function confirmDelete(): Promise<void> {
  const targets = [...deleteTargets.value]
  deleteConfirmOpen.value = false
  try {
    for (const row of targets) {
      if (row.issueState === 1) { error.value = `群組 ${row.groupNo} 正在簽核中，不允許刪除。`; continue }
      if (mockMode.value) {
        if (row.issueState === 2) rows.value = rows.value.map(item => item.groupNo === row.groupNo ? { ...item, issueState: -1, reviser: props.currentUserNo || 'DEV001', reviseDate: auditDate(), dataStamp: item.dataStamp + 1 } : item)
        else {
          rows.value = rows.value.filter(item => item.groupNo !== row.groupNo)
          details.value = details.value.filter(item => item.groupNo !== row.groupNo)
          scripts.value = scripts.value.filter(item => item.groupNo !== row.groupNo)
          delete opBindings.value[row.groupNo]
        }
      } else await request(`api/qc/item-groups/${encodeURIComponent(row.groupNo)}`, { method: 'DELETE' })
    }
    persistMock(); selectedGroupNos.value = []; await loadGroups(); message.value = '刪除/停用作業完成。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '刪除群組失敗。' }
}
async function approveSelected(): Promise<void> {
  const targets = selectedGroups.value.length ? selectedGroups.value : activeGroup.value ? [activeGroup.value] : []
  if (!targets.length) { error.value = '請先選取要核准的群組。'; return }
  if (!details.value.length) { error.value = '目前群組沒有品管項目明細，不能核准。'; return }
  try {
    for (const row of targets) {
      if (row.issueState !== 0) { error.value = `群組 ${row.groupNo} 目前不是未凍結狀態。`; continue }
      const nextState: IssueState = issueFlowEnabled.value ? 1 : 2
      if (mockMode.value) rows.value = rows.value.map(item => item.groupNo === row.groupNo ? { ...item, issueState: nextState, reviser: props.currentUserNo || 'DEV001', reviseDate: auditDate(), dataStamp: item.dataStamp + 1 } : item)
      else await request(`api/qc/item-groups/${encodeURIComponent(row.groupNo)}/approve`, { method: 'POST', body: JSON.stringify({ dataStamp: row.dataStamp }) })
    }
    persistMock(); await loadGroups(); message.value = '核准作業已送出。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '核准失敗。' }
}
function openAddItems(): void {
  if (!activeGroup.value) { error.value = '請先選取一筆群組資料。'; return }
  if (activeGroup.value.issueState !== 0) { error.value = '只有未凍結的群組可以新增品管項目。'; return }
  addItemSelection.value = []; pendingSelection.value = []; pendingDetails.value = details.value.map(row => ({ ...row })).sort((a, b) => a.showOrder - b.showOrder)
  pickerQuery.value = ''; panelMode.value = 'add-items'
}
function moveCandidatesRight(): void {
  const group = activeGroup.value
  if (!group) return
  const additions = candidates.value.filter(item => addItemSelection.value.includes(item.qcItemNo))
  pendingDetails.value = [...pendingDetails.value, ...additions.map(item => ({ groupNo: group.groupNo, qcItemNo: item.qcItemNo, qcItemName: item.qcItemName,
    showOrder: pendingDetails.value.length, scriptIsValid: scripts.value.some(script => script.groupNo === group.groupNo && script.qcItemNo === item.qcItemNo),
    componentCollect: item.componentCollect, issueState: item.issueState }))]
  addItemSelection.value = []
}
function movePendingLeft(): void {
  pendingDetails.value = pendingDetails.value.filter(row => !pendingSelection.value.includes(row.qcItemNo))
  pendingDetails.value.forEach((row, index) => { row.showOrder = index })
  pendingSelection.value = []
}
function movePendingItem(row: DetailRow, offset: -1 | 1): void {
  const ordered = pendingDetails.value.slice().sort((a, b) => a.showOrder - b.showOrder)
  const index = ordered.findIndex(item => item.qcItemNo === row.qcItemNo)
  const target = ordered[index + offset]
  if (!target) return
  const oldOrder = row.showOrder; row.showOrder = target.showOrder; target.showOrder = oldOrder
  pendingDetails.value = ordered.sort((a, b) => a.showOrder - b.showOrder).map((item, order) => ({ ...item, showOrder: order }))
}
function moveSelectedPending(offset: -1 | 1): void {
  const row = pendingDetails.value.find(item => item.qcItemNo === pendingSelection.value[0])
  if (row) movePendingItem(row, offset)
}
async function saveAddedItems(): Promise<void> {
  const group = activeGroup.value
  if (!group || group.issueState !== 0) return
  const newRows = pendingDetails.value.map((row, index) => ({ ...row, groupNo: group.groupNo, showOrder: index }))
  try {
    if (mockMode.value) {
      const retained = new Set(newRows.map(row => row.qcItemNo))
      details.value = [...details.value.filter(row => row.groupNo !== group.groupNo), ...newRows]
      scripts.value = scripts.value.filter(script => script.groupNo !== group.groupNo || retained.has(script.qcItemNo))
    }
    else await request(`api/qc/item-groups/${encodeURIComponent(group.groupNo)}/details`, { method: 'PUT', body: JSON.stringify({ items: newRows.map(row => ({ qcItemNo: row.qcItemNo, showOrder: row.showOrder })) }) })
    persistMock(); await loadDetails(group.groupNo); panelMode.value = ''; message.value = `已更新 ${newRows.length} 筆品管項目與顯示順序。`
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '新增品管項目失敗。' }
}
async function removeDetails(): Promise<void> {
  if (!activeGroup.value || !detailSelection.value.length) { error.value = '請先選取要移除的品管項目。'; return }
  if (activeGroup.value.issueState !== 0) { error.value = '只有未凍結的群組可以移除品管項目。'; return }
  try {
    const removed = [...detailSelection.value]
    if (mockMode.value) {
      details.value = details.value.filter(row => !removed.some(item => item.qcItemNo === row.qcItemNo))
      scripts.value = scripts.value.filter(script => !removed.some(item => item.qcItemNo === script.qcItemNo && script.groupNo === activeGroup.value!.groupNo))
      details.value.filter(row => row.groupNo === activeGroup.value!.groupNo).forEach((row, index) => { row.showOrder = index })
    } else for (const row of removed) await request(`api/qc/item-groups/${encodeURIComponent(activeGroup.value.groupNo)}/details/${encodeURIComponent(row.qcItemNo)}`, { method: 'DELETE' })
    selectedDetailNos.value = []; persistMock(); await loadDetails(activeGroup.value.groupNo); message.value = '選取的品管項目已移除。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '移除品管項目失敗。' }
}
function openParameters(): void {
  if (!activeGroup.value) { error.value = '請先選取一筆群組資料。'; return }
  const group = activeGroup.value
  const plan = [...plans.value, ...waferPlans.value].find(item => item.planNo === group.planNo && item.issueState === 2)
  parameterEditor.value = { rows: groupParameters.value[group.groupNo]?.map(row => ({ ...row })) ?? parameters.value.map(item => ({ parameterNo: item.parameterNo, parameterName: item.parameterName, specType: 0, value: '' })) }
  if (!mockMode.value) void request<Array<{ parameterNo: string; parameterName: string; specType: number; value: string }>>(`api/qc/item-groups/${encodeURIComponent(group.groupNo)}/parameters${plan ? `?planType=${plan.planType}` : ''}`).then(rows => { parameterEditor.value.rows = listOf(rows) }).catch(cause => { error.value = cause instanceof Error ? cause.message : '載入參數失敗。' })
  panelMode.value = 'parameters'
}
async function saveParameters(): Promise<void> {
  if (!activeGroup.value) return
  try {
    if (mockMode.value) groupParameters.value[activeGroup.value.groupNo] = parameterEditor.value.rows.map(row => ({ ...row }))
    else await request(`api/qc/item-groups/${encodeURIComponent(activeGroup.value.groupNo)}/parameters`, { method: 'PUT', body: JSON.stringify(parameterEditor.value.rows) })
    persistMock()
    panelMode.value = ''; message.value = '參數設定已儲存。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '儲存參數失敗。' }
}
function openOperations(): void {
  if (!activeGroup.value) { error.value = '請先選取一筆群組資料。'; return }
  if (activeGroup.value.issueState === 0 || activeGroup.value.issueState === 1) { error.value = '此狀態下不允許修改所屬作業站。'; return }
  if (!mockMode.value) void request<string[]>(`api/qc/item-groups/${encodeURIComponent(activeGroup.value.groupNo)}/operations`).then(result => { opBindings.value[activeGroup.value!.groupNo] = listOf(result) }).catch(cause => { error.value = cause instanceof Error ? cause.message : '載入作業站對應失敗。' })
  panelMode.value = 'operations'
}
function toggleOperation(opNo: string): void {
  if (!activeGroup.value) return
  const existing = opBindings.value[activeGroup.value.groupNo] ?? []
  opBindings.value[activeGroup.value.groupNo] = existing.includes(opNo) ? existing.filter(value => value !== opNo) : [...existing, opNo]
}
async function saveOperations(): Promise<void> {
  if (!activeGroup.value) return
  try {
    if (!mockMode.value) await request(`api/qc/item-groups/${encodeURIComponent(activeGroup.value.groupNo)}/operations`, { method: 'PUT', body: JSON.stringify({ qcType: 2, opNos: opBindings.value[activeGroup.value.groupNo] ?? [] }) })
    persistMock(); panelMode.value = ''; message.value = '作業站對應已儲存。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '儲存作業站對應失敗。' }
}
function openScript(): void {
  if (!activeGroup.value || !activeDetail.value) { error.value = '請先選取一筆品管項目明細。'; return }
  const old = scripts.value.find(item => item.groupNo === activeGroup.value!.groupNo && item.qcItemNo === activeDetail.value!.qcItemNo)
  scriptEditor.value = old ? { ...old } : { groupNo: activeGroup.value.groupNo, qcItemNo: activeDetail.value.qcItemNo, script: '', description: '' }
  panelMode.value = 'script'
}
async function saveScript(): Promise<void> {
  if (!activeGroup.value || !activeDetail.value) return
  try {
    if (mockMode.value) {
      scripts.value = scripts.value.filter(item => !(item.groupNo === scriptEditor.value.groupNo && item.qcItemNo === scriptEditor.value.qcItemNo))
      if (scriptEditor.value.script.trim()) scripts.value.push({ ...scriptEditor.value })
      details.value = details.value.map(item => item.groupNo === scriptEditor.value.groupNo && item.qcItemNo === scriptEditor.value.qcItemNo ? { ...item, scriptIsValid: !!scriptEditor.value.script.trim() } : item)
    } else await request(`api/qc/item-groups/${encodeURIComponent(scriptEditor.value.groupNo)}/details/${encodeURIComponent(scriptEditor.value.qcItemNo)}/script`, { method: 'PUT', body: JSON.stringify(scriptEditor.value) })
    persistMock(); await loadDetails(activeGroup.value.groupNo); panelMode.value = ''; message.value = '計算腳本已儲存。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '儲存計算腳本失敗。' }
}
function openMemo(): void {
  if (!activeGroup.value) { error.value = '請先選取一筆群組資料。'; return }
  const selected = new Set(memos.value[activeGroup.value.groupNo] ?? [])
  parameters.value = parameters.value.map(item => ({ ...item, checked: selected.has(item.parameterNo) }))
  if (!mockMode.value) void request<string[]>(`api/qc/item-groups/${encodeURIComponent(activeGroup.value.groupNo)}/memo-fields`).then(result => {
    const checked = new Set(listOf(result)); parameters.value = parameters.value.map(item => ({ ...item, checked: checked.has(item.parameterNo) }))
  }).catch(cause => { error.value = cause instanceof Error ? cause.message : '載入備註欄位失敗。' })
  panelMode.value = 'memo'
}
async function saveMemo(): Promise<void> {
  if (!activeGroup.value) return
  const selected = parameters.value.filter(item => item.checked).map(item => item.parameterNo)
  try {
    if (mockMode.value) memos.value[activeGroup.value.groupNo] = selected
    else await request(`api/qc/item-groups/${encodeURIComponent(activeGroup.value.groupNo)}/memo-fields`, { method: 'PUT', body: JSON.stringify({ qcItemType: 1, parameterNos: selected }) })
    persistMock(); panelMode.value = ''; message.value = '備註欄位設定已儲存。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '儲存備註欄位失敗。' }
}
function closePanel(): void { panelMode.value = ''; pickerQuery.value = '' }
function statusName(state: number): string { return ({ 0: '未凍結', 1: '簽核中', 2: '已核准', '-1': '未使用' } as Record<string, string>)[String(state)] ?? String(state) }
function yesNo(value: number): string { return value === 1 ? '是' : '否' }
function handleRowKey(event: KeyboardEvent, row: GroupRow): void { if (event.key === 'Enter' || event.key === ' ') selectGroup(row) }

onMounted(async () => {
  try { await loadLookups(); await loadGroups() }
  catch (cause) { error.value = cause instanceof Error ? cause.message : '載入資料失敗。' }
})
</script>

<template>
  <section class="qc-group-page">
    <header class="page-heading"><div><h1>計量品管項目群組</h1><p>QC212 · 計量品管項目_群組</p></div><button class="plain-button" @click="emit('close')">關閉</button></header>
    <section class="query-strip" aria-label="群組查詢條件">
      <label v-for="option in issueStateOptions" :key="option.value" class="state-option">
        <input type="radio" name="qc-group-state" :checked="issueStateFilter === option.value" @change="selectStatus(option.value)"><span>{{ option.label }}</span>
      </label>
      <label class="query-label" for="group-no-search">群組編號</label>
      <input id="group-no-search" v-model="groupNoInput" class="query-input" @keyup.enter="searchGroups">
      <span class="percent-mark">%</span><button class="plain-button search-button" @click="searchGroups">查詢</button>
      <span class="query-spacer"></span><span class="result-count">資料總筆數 {{ filteredRows.length }}</span>
    </section>
    <div v-if="error" class="alert error-alert" role="alert"><span>{{ error }}</span><button @click="error = ''">×</button></div>
    <div v-if="message" class="alert success-alert" role="status"><span>{{ message }}</span><button @click="message = ''">×</button></div>

    <section class="win-group master-group">
      <legend>計量品管項目群組資料</legend>
      <div class="master-layout">
        <div class="grid-frame master-grid-frame">
          <div class="grid-scroll">
            <table class="win-grid master-grid">
              <thead><tr>
                <th class="check-col"><input type="checkbox" aria-label="選取目前清單" :checked="filteredRows.length > 0 && filteredRows.every(row => selectedGroupNos.includes(row.groupNo))" @change="setAllGroups"></th>
                <th>Group No</th><th>Group Name</th><th>Lot Is Valid</th><th>Component Collect</th><th>Plan No</th><th>Form Name</th><th>Web URL</th><th>Union Report Create</th><th>Description</th><th>Creator</th><th>Create Date</th><th>Reviser</th><th>Revise Date</th><th>Cursor Mode</th><th>Get File Path</th>
              </tr></thead>
              <tbody>
                <tr v-for="row in filteredRows" :key="row.groupNo" :class="{ active: selectedGroupNo === row.groupNo }" tabindex="0" @click="selectGroup(row)" @keydown="handleRowKey($event, row)">
                  <td class="check-col" @click.stop><input type="checkbox" :checked="selectedGroupNos.includes(row.groupNo)" :aria-label="`選取 ${row.groupNo}`" @change="toggleGroupSelection(row)"></td>
                  <td>{{ row.groupNo }}</td><td>{{ row.groupName }}</td><td>{{ yesNo(row.lotIsValid) }}</td><td>{{ yesNo(row.componentCollect) }}</td><td>{{ row.planNo }}</td><td>{{ row.formName }}</td><td>{{ row.webUrl }}</td><td>{{ yesNo(row.unionErf) }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser }}</td><td>{{ row.reviseDate }}</td><td>{{ row.cursorMode === 0 ? 'Down' : 'Right' }}</td><td>{{ row.getFilePath }}</td>
                </tr>
                <tr v-if="!filteredRows.length"><td colspan="16" class="empty-row">{{ loading ? '載入中…' : '查無資料' }}</td></tr>
              </tbody>
            </table>
          </div>
        </div>
        <aside class="action-rail" aria-label="群組功能">
          <button class="win-button primary-button" @click="openEditor('add')">新增 (A)</button><button class="win-button" @click="openEditor('edit')">修改 (E)</button>
          <button class="win-button danger-button" @click="openDelete">刪除 (D)</button><button class="win-button" @click="openEditor('copy')">複製 (C)</button><button class="win-button" @click="approveSelected">核准</button>
          <button class="win-button" @click="openParameters">Parameter</button><button class="win-button" @click="openOperations">Op</button>
          <button class="win-button" @click="openAddItems">Add QC</button><button class="win-button" @click="openMemo">Memo</button>
        </aside>
      </div>
      <div class="master-remove"><button class="win-button danger-button" @click="removeDetails">移除 QC</button></div>
    </section>

    <section class="win-group detail-group">
      <legend>群組品管項目明細 <span v-if="activeGroup" class="group-caption">{{ activeGroup.groupNo }}｜{{ activeGroup.groupName }}</span></legend>
      <div class="detail-layout">
        <div class="grid-frame detail-grid-frame"><div class="grid-scroll">
          <table class="win-grid detail-grid"><thead><tr><th class="check-col"><input type="checkbox" aria-label="選取全部明細" :checked="details.length > 0 && selectedDetailNos.length === details.length" @change="setAllDetails"></th><th>QCItem No</th><th>QCItem Name</th><th>Show Order</th><th>Script Is Valid</th></tr></thead>
            <tbody><tr v-for="row in details" :key="row.qcItemNo" :class="{ selected: selectedDetailNos.includes(row.qcItemNo) }" @click="selectedDetailNos = [row.qcItemNo]">
              <td class="check-col" @click.stop><input v-model="selectedDetailNos" type="checkbox" :value="row.qcItemNo" :aria-label="`選取 ${row.qcItemNo}`"></td><td>{{ row.qcItemNo }}</td><td>{{ row.qcItemName }}</td><td class="number-cell">{{ row.showOrder }}</td><td>{{ row.scriptIsValid ? '是' : '否' }}</td>
            </tr><tr v-if="!details.length"><td colspan="5" class="empty-row">{{ activeGroup ? '此群組尚未設定品管項目' : '請先選取群組資料' }}</td></tr></tbody>
          </table>
        </div></div>
        <aside class="detail-actions"><button class="win-button" @click="openScript">Calculate Script</button></aside>
      </div>
      <footer class="detail-footer"><span>明細筆數 {{ details.length }}</span><span v-if="activeGroup">狀態：{{ statusName(activeGroup.issueState) }}</span></footer>
    </section>

    <div v-if="editorOpen" class="modal-backdrop" role="presentation" @click.self="editorOpen = false">
      <section class="dialog editor-dialog" role="dialog" aria-modal="true" :aria-label="editorTitle">
        <header class="dialog-heading"><h2>{{ editorTitle }}</h2><button class="icon-close" aria-label="關閉" @click="editorOpen = false">×</button></header>
        <div class="editor-form">
          <label class="form-row"><span>群組編號</span><input v-model.trim="editor.groupNo" class="yellow-input" maxlength="25" :readonly="editorMode === 'edit'"></label>
          <label class="form-row"><span>群組名稱</span><input v-model.trim="editor.groupName" class="yellow-input" maxlength="50"></label>
          <fieldset class="option-group"><legend>Lot Is Valid</legend><label><input v-model.number="editor.lotIsValid" type="radio" :value="1">是</label><label><input v-model.number="editor.lotIsValid" type="radio" :value="0">否</label></fieldset>
          <fieldset class="option-group"><legend>Component Collect</legend><label><input v-model.number="editor.componentCollect" type="radio" :value="1" @change="editor.planNo = ''; editor.cursorMode = 0">是</label><label><input v-model.number="editor.componentCollect" type="radio" :value="0" @change="editor.planNo = ''">否</label></fieldset>
          <label class="check-row"><input v-model="unionPlan" type="checkbox">連結抽樣計畫（Union Plan）</label>
          <label class="form-row"><span>抽樣計畫</span><select v-model="editor.planNo" class="yellow-input" :disabled="!unionPlan"><option value="">請選擇</option><option v-for="plan in visiblePlans.filter(item => item.issueState === 2)" :key="plan.planNo" :value="plan.planNo">{{ plan.planNo }}｜{{ plan.planName }}</option></select></label>
          <label class="check-row"><input v-model="editor.unionErf" type="checkbox" :true-value="1" :false-value="0">建立聯合異常報告（Union ERF）</label>
          <label class="form-row"><span>QC Class</span><input v-model="editor.qcClass" class="yellow-input" maxlength="30" list="qc-group-class-list"><datalist id="qc-group-class-list"><option v-for="item in qcClassOptions" :key="item" :value="item"></option></datalist></label>
          <label class="form-row"><span>WinSPC Form</span><select v-model="editor.formName" class="yellow-input" @change="editor.executionFile = interfaceOptions.find(item => item.name === editor.formName)?.file ?? ''"><option value="">（無）</option><option v-for="item in interfaceOptions" :key="`${item.name}|${item.file}`" :value="item.name">{{ item.name }}｜{{ item.file }}</option></select></label>
          <label class="form-row"><span>Web URL</span><input v-model="editor.webUrl" class="yellow-input" maxlength="100"></label>
          <fieldset class="option-group"><legend>Cursor Mode</legend><label><input v-model.number="editor.cursorMode" type="radio" :value="0" :disabled="editor.componentCollect === 1">Down</label><label><input v-model.number="editor.cursorMode" type="radio" :value="1" :disabled="editor.componentCollect === 1">Right</label></fieldset>
          <label class="form-row"><span>檔案路徑</span><input v-model="editor.getFilePath" class="yellow-input" maxlength="50"></label>
          <label class="form-row description-row"><span>說明</span><textarea v-model="editor.description" class="yellow-input" maxlength="255"></textarea></label>
          <div class="audit-panel"><label>建立人<input :value="editor.creator" readonly></label><label>建立日期<input :value="editor.createDate" readonly></label><label>修改人<input :value="editor.reviser" readonly></label><label>修改日期<input :value="editor.reviseDate" readonly></label></div>
        </div>
        <p v-if="error" class="dialog-error">{{ error }}</p>
        <footer class="dialog-actions"><button class="win-button" @click="editorOpen = false">關閉</button><button class="win-button" :disabled="saving" @click="saveEditor(false)">套用</button><button class="win-button primary-button" :disabled="saving" @click="saveEditor()">{{ saving ? '儲存中…' : '確定' }}</button></footer>
      </section>
    </div>

    <div v-if="panelMode" class="modal-backdrop" role="presentation" @click.self="closePanel">
      <section class="dialog child-dialog" role="dialog" aria-modal="true" :aria-label="modalTitle">
        <header class="dialog-heading"><div><h2>{{ modalTitle }}</h2><small v-if="activeGroup">{{ activeGroup.groupNo }}｜{{ activeGroup.groupName }}</small></div><button class="icon-close" aria-label="關閉" @click="closePanel">×</button></header>
        <template v-if="panelMode === 'add-items'">
          <div class="child-toolbar"><input v-model="pickerQuery" placeholder="搜尋品管項目編號或名稱"><span>僅列出已核准、元件類型相符的計量品管項目</span></div>
          <div class="dual-picker">
            <section class="picker-column"><h3>可選品管項目</h3><div class="child-list"><label v-for="item in candidates" :key="item.qcItemNo" class="child-list-row"><input v-model="addItemSelection" type="checkbox" :value="item.qcItemNo"><strong>{{ item.qcItemNo }}</strong><span>{{ item.qcItemName }}</span></label><div v-if="!candidates.length" class="empty-row">沒有可加入的品管項目</div></div></section>
            <div class="picker-moves"><button class="win-button" :disabled="!addItemSelection.length" @click="moveCandidatesRight">加入 →</button><button class="win-button" :disabled="!pendingSelection.length" @click="movePendingLeft">← 移除</button><button class="win-button" :disabled="!pendingSelection.length" @click="moveSelectedPending(-1)">上移</button><button class="win-button" :disabled="!pendingSelection.length" @click="moveSelectedPending(1)">下移</button></div>
            <section class="picker-column"><h3>已選品管項目（顯示順序）</h3><div class="child-list"><label v-for="row in pendingDetails.slice().sort((a, b) => a.showOrder - b.showOrder)" :key="row.qcItemNo" class="child-list-row"><input v-model="pendingSelection" type="checkbox" :value="row.qcItemNo"><strong>{{ row.showOrder + 1 }}. {{ row.qcItemNo }}</strong><span>{{ row.qcItemName }}</span></label><div v-if="!pendingDetails.length" class="empty-row">尚未選取項目</div></div></section>
          </div>
          <p class="dialog-hint">儲存時將完整替換此群組的明細與顯示順序，並保留每個品管項目已有的群組計算腳本。</p>
          <footer class="dialog-actions"><button class="win-button" @click="closePanel">取消</button><button class="win-button primary-button" @click="saveAddedItems">確定加入</button></footer>
        </template>
        <template v-else-if="panelMode === 'parameters'">
          <p class="dialog-hint">群組沿用共用 Parameter 編輯器；連結已核准抽樣計畫時會一併帶入 PlanType。</p>
          <div class="child-list parameter-list"><div v-for="(row, index) in parameterEditor.rows" :key="row.parameterNo" class="parameter-row"><strong>{{ row.parameterNo }}｜{{ row.parameterName }}</strong><select v-model.number="row.specType"><option :value="0">固定值</option><option :value="1">下限</option><option :value="2">上限</option></select><input v-model="row.value" class="yellow-input" placeholder="參數值"></div></div>
          <footer class="dialog-actions"><button class="win-button" @click="closePanel">取消</button><button class="win-button primary-button" @click="saveParameters">確定</button></footer>
        </template>
        <template v-else-if="panelMode === 'operations'">
          <p class="dialog-hint">QCType = 2。原 WinForms 不允許未凍結或簽核中的群組修改作業站。</p>
          <div class="child-list"><label v-for="op in operations.slice().sort((a, b) => a.opOrder - b.opOrder)" :key="op.opNo" class="child-list-row"><input type="checkbox" :checked="(opBindings[activeGroup?.groupNo ?? ''] ?? []).includes(op.opNo)" @change="toggleOperation(op.opNo)"><strong>{{ op.opNo }}</strong><span>{{ op.opName }}</span><small>順序 {{ op.opOrder }}</small></label></div>
          <footer class="dialog-actions"><button class="win-button" @click="closePanel">取消</button><button class="win-button primary-button" @click="saveOperations">確定</button></footer>
        </template>
        <template v-else-if="panelMode === 'script'">
          <label class="script-label">QC 項目<input :value="`${scriptEditor.qcItemNo}｜${activeDetail?.qcItemName ?? ''}`" readonly></label>
          <label class="script-label">Script<textarea v-model="scriptEditor.script" class="script-area" maxlength="255" placeholder="最多 255 字"></textarea></label>
          <label class="script-label">說明<textarea v-model="scriptEditor.description" class="yellow-input"></textarea></label>
          <footer class="dialog-actions"><button class="win-button" @click="closePanel">取消</button><button class="win-button primary-button" @click="saveScript">確定</button></footer>
        </template>
        <template v-else-if="panelMode === 'memo'">
          <div class="child-list memo-list"><label v-for="item in parameters.slice().sort((a, b) => a.parameterNo.localeCompare(b.parameterNo))" :key="item.parameterNo" class="child-list-row"><input v-model="item.checked" type="checkbox"><strong>{{ item.parameterNo }}</strong><span>{{ item.parameterName }}</span></label></div>
          <footer class="memo-actions"><button class="win-button" @click="parameters = parameters.map(item => ({ ...item, checked: true }))">全選</button><button class="win-button" @click="parameters = parameters.map(item => ({ ...item, checked: false }))">全不選</button><span></span><button class="win-button" @click="closePanel">取消</button><button class="win-button primary-button" @click="saveMemo">確定</button></footer>
        </template>
      </section>
    </div>
    <div v-if="deleteConfirmOpen" class="modal-backdrop" role="presentation" @click.self="deleteConfirmOpen = false"><section class="dialog confirm-dialog" role="dialog" aria-modal="true"><h2>確認刪除</h2><p>將對 {{ deleteTargets.length }} 筆群組執行刪除。已核准資料會改為未使用；未凍結或未使用資料會刪除主檔、明細及關聯設定。</p><footer class="dialog-actions"><button class="win-button" @click="deleteConfirmOpen = false">取消</button><button class="win-button danger-button" @click="confirmDelete">確定刪除</button></footer></section></div>
  </section>
</template>

<style scoped>
.qc-group-page{height:100%;min-height:560px;display:flex;flex-direction:column;gap:8px;padding:10px;background:#f8f6f0;color:#333;box-sizing:border-box;font-size:13px}.page-heading{display:flex;align-items:center;justify-content:space-between;min-height:38px}.page-heading h1{margin:0;font-size:21px;font-weight:600;color:#392f20}.page-heading p{margin:3px 0 0;color:#777;font-size:11px}.query-strip{min-height:40px;display:flex;align-items:center;gap:10px;padding:5px 9px;background:#fff;border:1px solid #d2d0ca;flex-wrap:wrap}.state-option{display:flex;align-items:center;gap:3px;white-space:nowrap}.state-option input,.option-group input,.check-row input{accent-color:#db9625}.query-label{margin-left:5px}.query-input{width:185px;height:27px;padding:3px 7px;border:1px solid #aaa;box-sizing:border-box}.percent-mark{margin-left:-6px}.query-spacer{flex:1}.result-count{color:#777;font-size:12px;white-space:nowrap}.win-group{position:relative;border:1px solid #c8c7c2;background:#fff;padding:18px 8px 8px;box-sizing:border-box;min-height:0}.win-group>legend{position:absolute;top:-9px;left:12px;padding:0 6px;background:#f8f6f0;color:#56514a;font-size:13px}.master-group{height:min(54vh,365px);min-height:235px}.master-layout{height:100%;display:grid;grid-template-columns:minmax(0,1fr) 92px;gap:8px}.grid-frame{min-width:0;min-height:0;border:1px solid #c9c9c9;background:#fff}.grid-scroll{width:100%;height:100%;overflow:auto}.win-grid{border-collapse:collapse;width:max-content;min-width:100%;font-size:12px;white-space:nowrap}.win-grid th{position:sticky;top:0;z-index:1;background:#e9a126;color:#fff;font-weight:600;text-align:center;border:1px solid #d2d2d2;padding:6px 8px}.win-grid td{height:25px;border:1px solid #d2d2d2;padding:2px 7px;background:#fff}.win-grid tbody tr{cursor:pointer}.win-grid tbody tr:hover td{background:#fff5cf}.win-grid tbody tr.active td,.win-grid tbody tr.selected td{background:#ffecaa}.check-col{width:25px;text-align:center!important}.check-col input{margin:0;accent-color:#da9525}.empty-row{text-align:center!important;color:#888;height:58px!important}.action-rail,.detail-actions{display:flex;flex-direction:column;gap:5px}.win-button,.plain-button{min-height:28px;padding:4px 9px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#f3f3f3);color:#333;font:inherit;white-space:nowrap;cursor:pointer}.win-button:hover,.plain-button:hover{background:#fff1bf;border-color:#d9a132}.win-button:disabled{opacity:.5;cursor:default}.primary-button{background:#f4a62a;border-color:#e69a20;color:#fff;font-weight:600}.primary-button:hover{background:#e99a20}.danger-button{color:#6b3b2a}.search-button{min-width:58px}.detail-group{flex:1;min-height:145px;display:flex;flex-direction:column}.group-caption{font-size:11px;color:#777;font-weight:400;margin-left:7px}.detail-layout{min-height:0;flex:1;display:grid;grid-template-columns:minmax(0,1fr) 134px;gap:8px}.detail-actions{display:grid;grid-template-columns:1fr 1fr;align-content:start}.detail-actions button:nth-child(3),.detail-actions button:nth-child(4){grid-column:1 / -1}.detail-footer{display:flex;justify-content:space-between;padding:5px 2px 0;color:#777;font-size:11px}.number-cell{text-align:right}.alert{display:flex;justify-content:space-between;align-items:center;padding:6px 10px;border:1px solid #ddd;font-size:12px}.alert button{border:0;background:transparent;font-size:17px;cursor:pointer}.error-alert{background:#fff0eb;border-color:#d7a89b;color:#8e3321}.success-alert{background:#f2f7ec;border-color:#c3d5ad;color:#4f6e35}.modal-backdrop{position:fixed;inset:0;z-index:100;display:grid;place-items:center;background:rgba(25,30,35,.38);padding:18px}.dialog{width:min(720px,100%);max-height:calc(100vh - 36px);overflow:auto;padding:12px;background:#fff;border:1px solid #aaa;box-shadow:0 10px 32px #0003;box-sizing:border-box}.editor-dialog{width:min(760px,100%)}.child-dialog{width:min(650px,100%)}.dialog-heading{display:flex;align-items:center;justify-content:space-between;gap:12px;border-bottom:1px solid #ddd;padding-bottom:8px}.dialog-heading h2,.confirm-dialog h2{margin:0;font-size:17px;font-weight:600}.dialog-heading small{display:block;margin-top:4px;color:#777}.icon-close{border:0;background:transparent;color:#555;font-size:24px;cursor:pointer}.editor-form{display:grid;grid-template-columns:1fr 1fr;gap:8px 15px;padding-top:11px}.form-row{display:grid;grid-template-columns:112px minmax(0,1fr);align-items:center;gap:6px}.form-row input,.form-row select,.script-label input,.script-label textarea,.parameter-row input,.parameter-row select,.memo-list input{min-width:0;min-height:28px;padding:4px 7px;border:1px solid #999;box-sizing:border-box;font:inherit}.yellow-input{background:#fff0b3}.form-row input:read-only{background:#eee;color:#666}.option-group{display:flex;align-items:center;gap:14px;min-height:35px;margin:0;padding:4px 9px;border:1px solid #bbb}.option-group legend{padding:0 4px;color:#666;font-size:11px}.option-group label,.check-row{display:flex;align-items:center;gap:4px}.check-row{grid-column:1 / -1;min-height:25px}.description-row textarea{height:62px;resize:vertical}.audit-panel{grid-column:1 / -1;display:grid;grid-template-columns:1fr 1fr;gap:7px;padding:8px;border:1px solid #ddd;background:#fafafa}.audit-panel label{display:grid;grid-template-columns:65px minmax(0,1fr);align-items:center;gap:5px;color:#666}.audit-panel input{width:100%;min-height:25px;padding:3px 6px;border:1px solid #ccc;background:#eee;color:#666;box-sizing:border-box}.dialog-actions{display:flex;justify-content:flex-end;gap:8px;margin-top:11px}.dialog-error{color:#a03020}.child-toolbar{display:flex;align-items:center;gap:10px;margin:11px 0;color:#777;font-size:11px}.child-toolbar input{height:29px;min-width:180px;flex:1;padding:4px 7px;border:1px solid #aaa}.child-list{max-height:50vh;overflow:auto;border:1px solid #ccc}.child-list-row{display:grid;grid-template-columns:24px minmax(120px,1fr) minmax(150px,2fr) auto;align-items:center;gap:8px;padding:8px 10px;border-bottom:1px solid #e2e2e2}.child-list-row:hover{background:#fff8de}.child-list-row input{accent-color:#da9525}.child-list-row small{color:#777}.dialog-hint{margin:9px 0 0;color:#777;font-size:11px;line-height:1.5}.parameter-list{padding:5px 8px}.parameter-row{display:grid;grid-template-columns:minmax(170px,1fr) 95px minmax(110px,150px);gap:8px;align-items:center;padding:5px 0;border-bottom:1px solid #eee}.parameter-row strong{font-size:12px;font-weight:500}.script-label{display:grid;grid-template-columns:92px minmax(0,1fr);align-items:start;gap:7px;margin-top:11px}.script-area{height:115px;resize:vertical;background:#fff8d6}.script-label textarea{min-height:60px;resize:vertical}.memo-list .child-list-row{grid-template-columns:24px minmax(120px,1fr) minmax(170px,2fr)}.memo-actions{display:flex;align-items:center;gap:7px;margin-top:11px}.memo-actions span{flex:1}.confirm-dialog{width:min(440px,100%)}.confirm-dialog p{line-height:1.6;color:#555}@media(max-width:900px){.master-group{height:46vh}.query-strip{gap:7px}.query-input{width:135px}.editor-form{grid-template-columns:1fr}.check-row,.audit-panel{grid-column:auto}.detail-layout{grid-template-columns:minmax(0,1fr) 112px}}@media(max-width:640px){.qc-group-page{min-height:600px;padding:7px}.master-group{height:40vh}.master-layout{grid-template-columns:minmax(0,1fr) 78px;gap:5px}.action-rail{gap:3px}.win-button{padding:3px;font-size:11px}.detail-layout{grid-template-columns:minmax(0,1fr)}.detail-actions{grid-template-columns:repeat(4,minmax(0,1fr))}.detail-actions button:nth-child(3),.detail-actions button:nth-child(4){grid-column:auto}.query-spacer{display:none}.parameter-row{grid-template-columns:1fr 80px}.parameter-row input{grid-column:1 / -1}}
.master-group{min-height:315px}
.master-layout{height:calc(100% - 33px)}
.master-remove{height:28px;display:flex;justify-content:flex-end;margin-top:5px}
.detail-layout{grid-template-columns:minmax(0,1fr) 115px}
.detail-actions{display:flex;align-items:stretch}
.child-dialog{width:min(780px,100%)}
.dual-picker{display:grid;grid-template-columns:minmax(0,1fr) 92px minmax(0,1fr);gap:8px;min-height:280px}
.picker-column{min-width:0;display:flex;flex-direction:column}
.picker-column h3{margin:2px 0 6px;font-size:12px;font-weight:600;color:#555}
.picker-column .child-list{flex:1;max-height:46vh}
.picker-moves{display:flex;flex-direction:column;justify-content:center;gap:7px}
.picker-column .child-list-row{grid-template-columns:24px minmax(0,1fr) minmax(0,1.3fr);gap:6px;padding:8px 7px;font-size:11px}
@media(max-width:900px){.master-group{height:auto;min-height:350px}.detail-layout{grid-template-columns:minmax(0,1fr) 100px}}
@media(max-width:640px){.master-group{height:auto;min-height:380px}.master-layout{height:calc(100% - 33px);grid-template-columns:minmax(0,1fr) 78px;gap:5px}.detail-layout{grid-template-columns:minmax(0,1fr)}.detail-actions{display:flex;flex-direction:row}.dual-picker{grid-template-columns:1fr}.picker-moves{flex-direction:row;flex-wrap:wrap}.picker-column .child-list{max-height:26vh}}
</style>
