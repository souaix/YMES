<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type IssueState = 0 | 1 | 2 | -1
type EditorMode = 'add' | 'edit' | 'copy'
interface GroupRow {
  groupNo: string; groupName: string; lotIsValid: number; description: string; issueState: IssueState
  creator: string; createDate: string; reviser: string; reviseDate: string; dataStamp: number
}
interface QcItem { qcItemNo: string; qcItemName: string; issueState: number }
interface DetailRow { groupNo: string; qcItemNo: string; qcItemName: string; showOrder: number }
interface Operation { opNo: string; opName: string; isInline: boolean; inspTypeFai: boolean; inspTypeIpqc: boolean; inspTypePqc: boolean; executeOrder: number }
interface Store { groups: GroupRow[]; details: DetailRow[]; qcItems: QcItem[]; operations: Operation[]; bindings: Record<string, Operation[]>; issueFlowEnabled: boolean }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const basePath = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mockMode = computed(() => import.meta.env.DEV && props.developmentPreview)
const storeKey = 'ymes-dev-mock:qc-attribute-item-group:v1'
const statusOptions: Array<{ label: string; value: IssueState }> = [
  { label: '未簽核', value: 0 }, { label: '簽核中', value: 1 }, { label: '已核准', value: 2 }, { label: '不使用', value: -1 },
]
const groups = ref<GroupRow[]>([])
const details = ref<DetailRow[]>([])
const qcItems = ref<QcItem[]>([])
const operations = ref<Operation[]>([])
const issueStateFilter = ref<IssueState>(0)
const groupNoInput = ref('')
const appliedGroupNo = ref('')
const selectedGroupNo = ref('')
const selectedGroupNos = ref<string[]>([])
const selectedDetailNo = ref('')
const loading = ref(false)
const message = ref('')
const error = ref('')
const editorOpen = ref(false)
const editorMode = ref<EditorMode>('add')
const editor = ref<GroupRow>(blankGroup())
const detailDialogOpen = ref(false)
const availableItems = ref<QcItem[]>([])
const draftDetails = ref<DetailRow[]>([])
const selectedAvailableNos = ref<string[]>([])
const selectedDraftNos = ref<string[]>([])
const itemSearch = ref('')
const opDialogOpen = ref(false)
const draftOperations = ref<Operation[]>([])
const opDialogReadOnly = ref(false)
const confirmDeleteOpen = ref(false)
const busy = ref(false)

const filteredGroups = computed(() => groups.value
  .filter(row => row.issueState === issueStateFilter.value)
  .filter(row => !appliedGroupNo.value || row.groupNo.toLowerCase().includes(appliedGroupNo.value.toLowerCase()))
  .slice().sort((a, b) => a.groupNo.localeCompare(b.groupNo)))
const activeGroup = computed(() => groups.value.find(row => row.groupNo === selectedGroupNo.value) ?? null)
const selectedGroups = computed(() => groups.value.filter(row => selectedGroupNos.value.includes(row.groupNo)))
const filteredAvailableItems = computed(() => availableItems.value.filter(row => !itemSearch.value || (row.qcItemNo + ' ' + row.qcItemName).toLowerCase().includes(itemSearch.value.toLowerCase())))
const editorTitle = computed(() => ({ add: '計數品管項目群組定義（新增）', edit: '計數品管項目群組定義（修改）', copy: '計數品管項目群組定義（複製）' }[editorMode.value]))

function blankGroup(): GroupRow {
  return { groupNo: '', groupName: '', lotIsValid: 0, description: '', issueState: 0, creator: '', createDate: '', reviser: '', reviseDate: '', dataStamp: 0 }
}
function seedStore(): Store {
  const audit = { creator: 'DEV001', createDate: '2026-11-03 09:30:00', reviser: 'DEV002', reviseDate: '2026-11-05 14:20:00', dataStamp: 1 }
  const groups: GroupRow[] = [
    { ...blankGroup(), groupNo: 'ATT-GRP-001', groupName: '外觀缺陷分類', lotIsValid: 1, description: '包裝前外觀檢查項目', ...audit },
    { ...blankGroup(), groupNo: 'ATT-GRP-002', groupName: '標籤與包裝確認', lotIsValid: 0, description: '依批次判斷的包裝檢查', issueState: 2, ...audit },
    { ...blankGroup(), groupNo: 'ATT-GRP-003', groupName: '待簽核測試群組', lotIsValid: 1, description: 'DEV 假資料：簽核流程中', issueState: 1, ...audit },
    { ...blankGroup(), groupNo: 'ATT-GRP-004', groupName: '停用群組', lotIsValid: 0, description: 'DEV 假資料：不使用', issueState: -1, ...audit },
  ]
  const qcItems: QcItem[] = [
    { qcItemNo: 'ATT-001', qcItemName: '外觀刮傷', issueState: 2 }, { qcItemNo: 'ATT-002', qcItemName: '表面污染', issueState: 2 },
    { qcItemNo: 'ATT-003', qcItemName: '標籤內容正確', issueState: 2 }, { qcItemNo: 'ATT-004', qcItemName: '包裝完整', issueState: 2 },
    { qcItemNo: 'ATT-005', qcItemName: '待核准項目（不提供選取）', issueState: 1 },
  ]
  const details: DetailRow[] = [
    { groupNo: 'ATT-GRP-001', qcItemNo: 'ATT-001', qcItemName: '外觀刮傷', showOrder: 0 },
    { groupNo: 'ATT-GRP-001', qcItemNo: 'ATT-002', qcItemName: '表面污染', showOrder: 1 },
    { groupNo: 'ATT-GRP-002', qcItemNo: 'ATT-003', qcItemName: '標籤內容正確', showOrder: 0 },
    { groupNo: 'ATT-GRP-002', qcItemNo: 'ATT-004', qcItemName: '包裝完整', showOrder: 1 },
  ]
  return { groups, details, qcItems, operations: [
    { opNo: 'OP-INSP-10', opName: '外觀檢驗', isInline: false, inspTypeFai: false, inspTypeIpqc: false, inspTypePqc: false, executeOrder: 10 },
    { opNo: 'OP-PACK-20', opName: '包裝檢查', isInline: false, inspTypeFai: false, inspTypeIpqc: false, inspTypePqc: false, executeOrder: 20 },
    { opNo: 'OP-FINAL-30', opName: '最終檢驗', isInline: false, inspTypeFai: false, inspTypeIpqc: false, inspTypePqc: false, executeOrder: 30 },
  ], bindings: { 'ATT-GRP-002': [{ opNo: 'OP-PACK-20', opName: '包裝檢查', isInline: false, inspTypeFai: false, inspTypeIpqc: false, inspTypePqc: false, executeOrder: 20 }] }, issueFlowEnabled: true }
}
function readStore(): Store {
  try {
    const parsed = JSON.parse(localStorage.getItem(storeKey) || 'null') as Store | null
    if (parsed && Array.isArray(parsed.groups) && Array.isArray(parsed.details)) return parsed
  } catch { /* reset corrupt DEV fixture */ }
  const initial = seedStore(); localStorage.setItem(storeKey, JSON.stringify(initial)); return initial
}
function saveStore(store: Store): void { localStorage.setItem(storeKey, JSON.stringify(store)) }
function hydrate(store: Store): void {
  groups.value = store.groups.map(row => ({ ...row })); details.value = store.details.map(row => ({ ...row }))
  qcItems.value = store.qcItems.map(row => ({ ...row })); operations.value = store.operations.map(row => ({ ...row }))
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
    throw new Error(problem?.detail || problem?.title || 'API 回應失敗（' + response.status + '）。')
  }
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}
function listOf<T>(payload: T[] | { items?: T[]; rows?: T[]; data?: T[] }): T[] {
  return Array.isArray(payload) ? payload : payload.items ?? payload.rows ?? payload.data ?? []
}
function normalizeGroup(value: Record<string, unknown>): GroupRow {
  const read = (lower: string, upper: string) => value[lower] ?? value[upper]
  return {
    groupNo: String(read('groupNo', 'GROUPNO') ?? ''), groupName: String(read('groupName', 'GROUPNAME') ?? ''),
    lotIsValid: Number(read('lotIsValid', 'LOTISVALID') ?? 0), description: String(read('description', 'DESCRIPTION') ?? ''),
    issueState: Number(read('issueState', 'ISSUESTATE') ?? 0) as IssueState,
    creator: String(read('creator', 'CREATOR') ?? ''), createDate: String(read('createDate', 'CREATEDATE') ?? ''),
    reviser: String(read('reviser', 'REVISER') ?? ''), reviseDate: String(read('reviseDate', 'REVISEDATE') ?? ''),
    dataStamp: Number(read('dataStamp', 'DATASTAMP') ?? 0),
  }
}
function normalizeItem(value: Record<string, unknown>): QcItem {
  return { qcItemNo: String(value.qcItemNo ?? value.QCITEMNO ?? ''), qcItemName: String(value.qcItemName ?? value.QCITEMNAME ?? ''), issueState: Number(value.issueState ?? value.ISSUESTATE ?? 2) }
}
function normalizeDetail(value: Record<string, unknown>): DetailRow {
  return { groupNo: String(value.groupNo ?? value.GROUPNO ?? ''), qcItemNo: String(value.qcItemNo ?? value.QCITEMNO ?? ''), qcItemName: String(value.qcItemName ?? value.QCITEMNAME ?? ''), showOrder: Number(value.showOrder ?? value.SHOWORDER ?? 0) }
}
async function loadGroups(): Promise<void> {
  loading.value = true; error.value = ''
  try {
    if (mockMode.value) hydrate(readStore())
    else groups.value = listOf(await request<GroupRow[] | { items?: GroupRow[] }>('api/qc/attribute-item-groups?functionNo=QCITEM_G_A_ORDER')).map(row => normalizeGroup(row as unknown as Record<string, unknown>))
    if (!filteredGroups.value.some(row => row.groupNo === selectedGroupNo.value)) {
      selectedGroupNo.value = filteredGroups.value[0]?.groupNo ?? ''; selectedGroupNos.value = selectedGroupNo.value ? [selectedGroupNo.value] : []
    }
    await loadDetails(selectedGroupNo.value)
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入計數品管群組失敗。' }
  finally { loading.value = false }
}
async function loadLookups(): Promise<void> {
  if (mockMode.value) { hydrate(readStore()); return }
  const [itemData, operationData] = await Promise.all([
    request<QcItem[] | { items?: QcItem[] }>('api/qc/attribute-qc-items?issueState=2'),
    request<Operation[] | { items?: Operation[] }>('api/process/operations?issueState=2'),
  ])
  qcItems.value = listOf(itemData).map(row => normalizeItem(row as unknown as Record<string, unknown>))
  operations.value = listOf(operationData).map(value => {
    const row = value as unknown as Record<string, unknown>
    return { opNo: String(row.opNo ?? row.OPNO ?? ''), opName: String(row.opName ?? row.OPNAME ?? ''), isInline: false, inspTypeFai: false, inspTypeIpqc: false, inspTypePqc: false, executeOrder: Number(row.opOrder ?? row.OPORDER ?? 0) }
  })
}
async function loadDetails(groupNo: string): Promise<void> {
  selectedGroupNo.value = groupNo; selectedDetailNo.value = ''
  if (!groupNo) { details.value = []; return }
  try {
    if (mockMode.value) details.value = readStore().details.filter(row => row.groupNo === groupNo).slice().sort((a, b) => a.showOrder - b.showOrder)
    else details.value = listOf(await request<DetailRow[] | { items?: DetailRow[] }>('api/qc/attribute-item-groups/' + encodeURIComponent(groupNo) + '/details'))
      .map(row => normalizeDetail(row as unknown as Record<string, unknown>)).sort((a, b) => a.showOrder - b.showOrder)
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入群組明細失敗。'; details.value = [] }
}
function selectGroup(row: GroupRow): void {
  selectedGroupNo.value = row.groupNo
  if (!selectedGroupNos.value.includes(row.groupNo)) selectedGroupNos.value = [row.groupNo]
  void loadDetails(row.groupNo)
}
function toggleGroup(row: GroupRow): void {
  selectedGroupNos.value = selectedGroupNos.value.includes(row.groupNo) ? selectedGroupNos.value.filter(no => no !== row.groupNo) : [...selectedGroupNos.value, row.groupNo]
  selectedGroupNo.value = row.groupNo; void loadDetails(row.groupNo)
}
function selectAllGroups(event: Event): void {
  selectedGroupNos.value = (event.target as HTMLInputElement).checked ? filteredGroups.value.map(row => row.groupNo) : []
}
function searchGroups(): void {
  appliedGroupNo.value = groupNoInput.value.trim().toUpperCase()
  selectedGroupNo.value = filteredGroups.value[0]?.groupNo ?? ''; selectedGroupNos.value = selectedGroupNo.value ? [selectedGroupNo.value] : []
  void loadDetails(selectedGroupNo.value)
}
function selectStatus(value: IssueState): void {
  issueStateFilter.value = value; selectedGroupNo.value = filteredGroups.value[0]?.groupNo ?? ''
  selectedGroupNos.value = selectedGroupNo.value ? [selectedGroupNo.value] : []; void loadDetails(selectedGroupNo.value)
}
function yesNo(value: number): string { return value === 1 ? '是' : '否' }
function openEditor(mode: EditorMode): void {
  if (mode !== 'add' && !activeGroup.value) { error.value = '請先選取一筆群組資料。'; return }
  if (mode === 'edit' && activeGroup.value?.issueState !== 0) { error.value = '只有未簽核群組可以修改。'; return }
  editorMode.value = mode
  editor.value = mode === 'add' ? { ...blankGroup(), creator: props.currentUserNo || 'DEVUSER', createDate: new Date().toLocaleString('sv-SE') } : { ...activeGroup.value!, ...(mode === 'copy' ? { groupNo: '', creator: props.currentUserNo || 'DEVUSER', createDate: new Date().toLocaleString('sv-SE'), reviser: '', reviseDate: '', dataStamp: 0, issueState: 0 as IssueState } : {}) }
  editorOpen.value = true; error.value = ''
}
async function saveGroup(keepOpen = false): Promise<void> {
  const value = { ...editor.value, groupNo: editor.value.groupNo.trim().toUpperCase(), groupName: editor.value.groupName.trim(), description: editor.value.description.trim() }
  if (!value.groupNo || !value.groupName) { error.value = '群組編號與群組名稱為必填。'; return }
  if (value.groupNo.length > 25 || value.groupName.length > 50 || value.description.length > 255) { error.value = '欄位長度超過資料表限制。'; return }
  if (editorMode.value !== 'edit' && groups.value.some(row => row.groupNo.toUpperCase() === value.groupNo)) { error.value = '群組編號已存在。'; return }
  busy.value = true; error.value = ''
  try {
    if (mockMode.value) {
      const store = readStore()
      if (editorMode.value === 'edit') {
        const i = store.groups.findIndex(row => row.groupNo === value.groupNo)
        if (i < 0 || store.groups[i].issueState !== 0) throw new Error('群組狀態已變更，請重新查詢。')
        store.groups[i] = { ...value, issueState: 0, reviser: props.currentUserNo || 'DEVUSER', reviseDate: new Date().toLocaleString('sv-SE'), dataStamp: store.groups[i].dataStamp + 1 }
      } else store.groups.push({ ...value, issueState: 0 })
      saveStore(store); hydrate(store)
    } else if (editorMode.value === 'edit') await request('api/qc/attribute-item-groups/' + encodeURIComponent(value.groupNo), { method: 'PUT', body: JSON.stringify({ groupName: value.groupName, lotIsValid: value.lotIsValid, description: value.description, dataStamp: value.dataStamp }) })
    else await request('api/qc/attribute-item-groups', { method: 'POST', body: JSON.stringify({ groupNo: value.groupNo, groupName: value.groupName, lotIsValid: value.lotIsValid, description: value.description }) })
    message.value = editorMode.value === 'edit' ? '群組資料已修改。' : editorMode.value === 'copy' ? '群組已複製；依 WinForms 僅複製基本資料。' : '群組資料已新增。'
    if (issueStateFilter.value === 0) { appliedGroupNo.value = ''; groupNoInput.value = ''; await loadGroups(); await loadDetails(value.groupNo) }
    if (keepOpen) {
      editorMode.value = 'edit'
      editor.value = { ...(groups.value.find(row => row.groupNo === value.groupNo) ?? value) }
    } else editorOpen.value = false
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '儲存群組失敗。' }
  finally { busy.value = false }
}
function requestDelete(): void {
  if (!selectedGroups.value.length) { error.value = '請先選取要刪除的群組。'; return }
  if (selectedGroups.value.some(row => row.issueState === 1)) { error.value = '簽核中的群組不可刪除。'; return }
  confirmDeleteOpen.value = true
}
async function deleteSelected(): Promise<void> {
  busy.value = true; error.value = ''
  try {
    const targets = [...selectedGroups.value]
    for (const row of targets) {
      if (mockMode.value) {
        const store = readStore()
        if (row.issueState === 2) {
          const target = store.groups.find(item => item.groupNo === row.groupNo)
          if (target) { target.issueState = -1; target.reviser = props.currentUserNo || 'DEVUSER'; target.reviseDate = new Date().toLocaleString('sv-SE'); target.dataStamp += 1 }
        } else {
          store.groups = store.groups.filter(item => item.groupNo !== row.groupNo); store.details = store.details.filter(item => item.groupNo !== row.groupNo)
        }
        saveStore(store); hydrate(store)
      } else await request('api/qc/attribute-item-groups/' + encodeURIComponent(row.groupNo), { method: 'DELETE', body: JSON.stringify({ issueState: row.issueState, dataStamp: row.dataStamp }) })
    }
    confirmDeleteOpen.value = false; message.value = '刪除/停用作業完成。'; selectedGroupNos.value = []; await loadGroups()
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '刪除群組失敗。' }
  finally { busy.value = false }
}
async function approveSelected(): Promise<void> {
  if (!selectedGroups.value.length) { error.value = '請先選取未簽核群組。'; return }
  if (selectedGroups.value.some(row => row.issueState !== 0)) { error.value = '核准只接受未簽核群組。'; return }
  if (selectedGroupNos.value.includes(selectedGroupNo.value) && !details.value.length) { error.value = '目前群組至少要設定一個品管項目才能核准。'; return }
  if (!window.confirm('確定核准所選群組？')) return
  busy.value = true; error.value = ''
  try {
    for (const row of selectedGroups.value) {
      if (mockMode.value) {
        const store = readStore(); const target = store.groups.find(item => item.groupNo === row.groupNo)
        if (target) { target.issueState = store.issueFlowEnabled ? 1 : 2; target.reviser = props.currentUserNo || 'DEVUSER'; target.reviseDate = new Date().toLocaleString('sv-SE'); target.dataStamp += 1 }
        saveStore(store); hydrate(store)
      } else await request('api/qc/attribute-item-groups/' + encodeURIComponent(row.groupNo) + '/approval', { method: 'POST', body: JSON.stringify({ dataStamp: row.dataStamp }) })
    }
    message.value = '核准作業已送出。'; await loadGroups()
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '群組核准失敗。' }
  finally { busy.value = false }
}
function openDetailEditor(): void {
  if (!activeGroup.value) { error.value = '請先選取群組資料。'; return }
  if (activeGroup.value.issueState !== 0) { error.value = '只有未簽核群組可以設定品管項目。'; return }
  draftDetails.value = details.value.map(row => ({ ...row })); selectedAvailableNos.value = []; selectedDraftNos.value = []; itemSearch.value = ''
  availableItems.value = qcItems.value.filter(item => item.issueState === 2 && !details.value.some(detail => detail.qcItemNo === item.qcItemNo))
  detailDialogOpen.value = true; error.value = ''
}
function addItems(): void {
  const selected = new Set(selectedAvailableNos.value)
  for (const item of availableItems.value.filter(row => selected.has(row.qcItemNo))) {
    if (!draftDetails.value.some(row => row.qcItemNo === item.qcItemNo)) draftDetails.value.push({ groupNo: selectedGroupNo.value, qcItemNo: item.qcItemNo, qcItemName: item.qcItemName, showOrder: draftDetails.value.length })
  }
  availableItems.value = availableItems.value.filter(row => !selected.has(row.qcItemNo)); selectedAvailableNos.value = []
  draftDetails.value.forEach((row, index) => { row.showOrder = index })
}
function removeItems(): void {
  const selected = new Set(selectedDraftNos.value)
  const removed = draftDetails.value.filter(row => selected.has(row.qcItemNo)).map(row => qcItems.value.find(item => item.qcItemNo === row.qcItemNo)).filter((row): row is QcItem => Boolean(row))
  draftDetails.value = draftDetails.value.filter(row => !selected.has(row.qcItemNo)); availableItems.value.push(...removed); selectedDraftNos.value = []
  draftDetails.value.forEach((row, index) => { row.showOrder = index })
}
function moveDraft(delta: number): void {
  if (selectedDraftNos.value.length !== 1) return
  const from = draftDetails.value.findIndex(row => row.qcItemNo === selectedDraftNos.value[0]); const to = from + delta
  if (from < 0 || to < 0 || to >= draftDetails.value.length) return
  ;[draftDetails.value[from], draftDetails.value[to]] = [draftDetails.value[to], draftDetails.value[from]]
  draftDetails.value.forEach((row, index) => { row.showOrder = index })
}
async function saveDetails(): Promise<void> {
  busy.value = true; error.value = ''
  try {
    if (mockMode.value) {
      const store = readStore(); store.details = store.details.filter(row => row.groupNo !== selectedGroupNo.value)
      store.details.push(...draftDetails.value.map((row, index) => ({ ...row, showOrder: index }))); saveStore(store); hydrate(store)
    } else await request('api/qc/attribute-item-groups/' + encodeURIComponent(selectedGroupNo.value) + '/details', { method: 'PUT', body: JSON.stringify({ items: draftDetails.value.map((row, index) => ({ qcItemNo: row.qcItemNo, showOrder: index })) }) })
    detailDialogOpen.value = false; message.value = '群組品管項目已更新。'; await loadDetails(selectedGroupNo.value)
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '儲存群組明細失敗。' }
  finally { busy.value = false }
}
async function openOperations(): Promise<void> {
  if (!activeGroup.value) { error.value = '請先選取群組資料。'; return }
  error.value = ''
  opDialogReadOnly.value = activeGroup.value.issueState !== 2
  try {
    if (mockMode.value) draftOperations.value = (readStore().bindings[selectedGroupNo.value] ?? []).map(row => ({ ...row }))
    else draftOperations.value = listOf(await request<Operation[] | { items?: Operation[] }>('api/qc/item-operations?qcItemType=3&qcItemNo=' + encodeURIComponent(selectedGroupNo.value))).map(value => {
      const row = value as unknown as Record<string, unknown>
      const inline = row.isInline ?? row.inline ?? row.INLINE
      return { opNo: String(row.opNo ?? row.OPNO ?? ''), opName: String(row.opName ?? row.OPNAME ?? ''), isInline: inline === true || Number(inline) === 1,
        inspTypeFai: row.inspTypeFai === true || Number(row.inspTypeFai ?? row.INSPTYPE_FAI) === 1,
        inspTypeIpqc: row.inspTypeIpqc === true || Number(row.inspTypeIpqc ?? row.INSPTYPE_IPQC) === 1,
        inspTypePqc: row.inspTypePqc === true || Number(row.inspTypePqc ?? row.INSPTYPE_PQC) === 1,
        executeOrder: Number(row.executeOrder ?? row.EXECUTEORDER ?? 0) }
    })
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入作業站對應失敗。'; return }
  opDialogOpen.value = true
}
function toggleOperation(operation: Operation, checked: boolean): void {
  if (opDialogReadOnly.value) return
  const current = draftOperations.value.filter(row => row.opNo !== operation.opNo)
  if (checked) current.push({ ...operation, isInline: false, inspTypeFai: false, inspTypeIpqc: false, inspTypePqc: false })
  draftOperations.value = current
}
async function saveOperations(): Promise<void> {
  if (!activeGroup.value || opDialogReadOnly.value) { opDialogOpen.value = false; return }
  if (activeGroup.value.lotIsValid === 0 && draftOperations.value.some(row => row.isInline)) { error.value = 'Lot Valid Check=否時不可指定 InLine 作業站。'; return }
  if (draftOperations.value.some(row => row.isInline && !row.inspTypeFai && !row.inspTypeIpqc && !row.inspTypePqc)) { error.value = '每個 InLine 作業站至少要勾選一個檢驗時機。'; return }
  busy.value = true
  try {
    if (mockMode.value) { const store = readStore(); store.bindings[selectedGroupNo.value] = draftOperations.value.map(row => ({ ...row })); saveStore(store) }
    else await request('api/qc/item-operations?qcItemType=3&qcItemNo=' + encodeURIComponent(selectedGroupNo.value), { method: 'PUT', body: JSON.stringify({ operations: draftOperations.value.map(row => ({ opNo: row.opNo, inline: row.isInline, inspTypeFai: row.inspTypeFai, inspTypeIpqc: row.inspTypeIpqc, inspTypePqc: row.inspTypePqc })) }) })
    opDialogOpen.value = false; message.value = '作業站對應已儲存。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '儲存作業站對應失敗。' }
  finally { busy.value = false }
}
function selectItemKey(event: KeyboardEvent, row: GroupRow): void { if (event.key === 'Enter' || event.key === ' ') selectGroup(row) }
onMounted(async () => { try { await loadLookups(); await loadGroups() } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入資料失敗。' } })
</script>

<template>
  <section class="qc-attribute-group-page">
    <header class="page-heading"><div><h1>計數品管項目群組</h1><p>QC213 · 計數品管項目_群組</p></div><button class="plain-button" @click="emit('close')">關閉</button></header>
    <section class="query-strip" aria-label="群組查詢條件">
      <label v-for="option in statusOptions" :key="option.value" class="state-option"><input type="radio" name="qc-attribute-group-state" :checked="issueStateFilter === option.value" @change="selectStatus(option.value)"><span>{{ option.label }}</span></label>
      <label class="query-label" for="attribute-group-no-search">群組編號</label><input id="attribute-group-no-search" v-model="groupNoInput" class="query-input" @keyup.enter="searchGroups">
      <span class="percent-mark">%</span><button class="plain-button search-button" @click="searchGroups">查詢</button><span class="query-spacer"></span><span class="result-count">資料總筆數 {{ filteredGroups.length }}</span>
    </section>
    <div v-if="error" class="alert error-alert" role="alert"><span>{{ error }}</span><button @click="error = ''">×</button></div>
    <div v-if="message" class="alert success-alert" role="status"><span>{{ message }}</span><button @click="message = ''">×</button></div>

    <section class="win-group master-group"><legend>計數品管項目群組資料</legend>
      <div class="master-layout"><div class="grid-frame"><div class="grid-scroll"><table class="win-grid master-grid">
        <thead><tr><th class="check-col"><input type="checkbox" aria-label="選取目前清單" :checked="filteredGroups.length > 0 && filteredGroups.every(row => selectedGroupNos.includes(row.groupNo))" @change="selectAllGroups"></th><th>Group No</th><th>Group Name</th><th>Lot Valid Check</th><th>Description</th><th>Creator</th><th>Create Date</th><th>Reviser</th><th>Revise Date</th></tr></thead>
        <tbody><tr v-for="row in filteredGroups" :key="row.groupNo" :class="{ active: selectedGroupNo === row.groupNo }" tabindex="0" @click="selectGroup(row)" @keydown="selectItemKey($event, row)">
          <td class="check-col" @click.stop><input type="checkbox" :checked="selectedGroupNos.includes(row.groupNo)" :aria-label="'選取 '+row.groupNo" @change="toggleGroup(row)"></td><td>{{ row.groupNo }}</td><td>{{ row.groupName }}</td><td>{{ yesNo(row.lotIsValid) }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser }}</td><td>{{ row.reviseDate }}</td>
        </tr><tr v-if="!filteredGroups.length"><td colspan="9" class="empty-row">{{ loading ? '載入中…' : '查無資料' }}</td></tr></tbody>
      </table></div></div>
      <aside class="action-rail" aria-label="群組功能">
        <button class="win-button primary-button" @click="openEditor('add')">新增 (A)</button><button class="win-button" :disabled="activeGroup?.issueState !== 0" @click="openEditor('edit')">修改 (E)</button>
        <button class="win-button danger-button" :disabled="!selectedGroups.length" @click="requestDelete">刪除 (D)</button><button class="win-button" :disabled="!activeGroup" @click="openEditor('copy')">複製 (C)</button>
        <button class="win-button" :disabled="!selectedGroups.length" @click="approveSelected">核准</button><button class="win-button" :disabled="!activeGroup" @click="openOperations">Op</button>
      </aside></div><footer class="grid-footer">資料總筆數 {{ filteredGroups.length }}</footer>
    </section>

    <section class="win-group detail-group"><legend>群組品管項目</legend>
      <div class="detail-toolbar"><span>群組：{{ activeGroup?.groupNo || '—' }}　{{ activeGroup?.groupName || '' }}</span><button class="win-button primary-button" :disabled="activeGroup?.issueState !== 0" @click="openDetailEditor">Add QC</button></div>
      <div class="grid-frame detail-grid-frame"><div class="grid-scroll"><table class="win-grid"><thead><tr><th>Group No</th><th>QCItem No</th><th>QCItem Name</th><th>Show Order</th></tr></thead>
        <tbody><tr v-for="row in details" :key="row.qcItemNo" :class="{ active: selectedDetailNo === row.qcItemNo }" @click="selectedDetailNo = row.qcItemNo"><td>{{ row.groupNo }}</td><td>{{ row.qcItemNo }}</td><td>{{ row.qcItemName }}</td><td>{{ row.showOrder }}</td></tr><tr v-if="!details.length"><td colspan="4" class="empty-row">尚未設定品管項目</td></tr></tbody>
      </table></div></div><footer class="grid-footer">資料總筆數 {{ details.length }}</footer>
    </section>

    <div v-if="editorOpen" class="modal-backdrop" @click.self="editorOpen = false"><section class="dialog editor-dialog" role="dialog" aria-modal="true" :aria-label="editorTitle">
      <header class="dialog-heading"><div><h2>{{ editorTitle }}</h2><small>QC Item Group Define · QC213</small></div><button class="icon-close" aria-label="關閉" @click="editorOpen = false">×</button></header>
      <div class="editor-form"><label class="form-row"><span>Group No</span><input v-model="editor.groupNo" class="yellow-input" maxlength="25" :readonly="editorMode === 'edit'" @blur="editor.groupNo = editor.groupNo.toUpperCase()"></label>
        <label class="form-row"><span>Group Name</span><input v-model="editor.groupName" class="yellow-input" maxlength="50"></label>
        <fieldset class="option-group"><legend>Lot Valid Check</legend><label><input v-model.number="editor.lotIsValid" type="radio" :value="0">No</label><label><input v-model.number="editor.lotIsValid" type="radio" :value="1">Yes</label></fieldset>
        <label class="form-row description-row"><span>Description</span><textarea v-model="editor.description" maxlength="255"></textarea></label>
        <div class="audit-panel"><label>建立人<input :value="editor.creator" readonly></label><label>建立日期<input :value="editor.createDate" readonly></label><label>修改人<input :value="editor.reviser || '—'" readonly></label><label>修改日期<input :value="editor.reviseDate || '—'" readonly></label></div>
      </div><p v-if="error" class="dialog-error">{{ error }}</p><footer class="dialog-actions"><button class="win-button primary-button" :disabled="busy" @click="saveGroup()">確定</button><button class="win-button" @click="editorOpen = false">關閉</button><button v-if="editorMode !== 'copy'" class="win-button" :disabled="busy" @click="saveGroup(true)">套用</button></footer>
    </section></div>

    <div v-if="detailDialogOpen" class="modal-backdrop" @click.self="detailDialogOpen = false"><section class="dialog picker-dialog" role="dialog" aria-modal="true" aria-label="選擇計數品管項目">
      <header class="dialog-heading"><div><h2>Add QC · {{ activeGroup?.groupNo }}</h2><small>只提供已核准的計數品管項目；雙擊列可移動</small></div><button class="icon-close" aria-label="關閉" @click="detailDialogOpen = false">×</button></header>
      <div class="picker-columns"><section class="picker-column"><h3>可選品管項目</h3><input v-model="itemSearch" class="query-input picker-search" placeholder="QCItem No / Name"><div class="picker-list">
        <button v-for="item in filteredAvailableItems" :key="item.qcItemNo" class="picker-row" :class="{ picked: selectedAvailableNos.includes(item.qcItemNo) }" @click="selectedAvailableNos = selectedAvailableNos.includes(item.qcItemNo) ? selectedAvailableNos.filter(no => no !== item.qcItemNo) : [...selectedAvailableNos, item.qcItemNo]" @dblclick="selectedAvailableNos = [item.qcItemNo]; addItems()"><span>{{ item.qcItemNo }}</span><span>{{ item.qcItemName }}</span></button><p v-if="!filteredAvailableItems.length" class="empty-row">沒有可新增項目</p>
      </div></section><div class="move-controls"><button class="win-button" :disabled="!selectedAvailableNos.length" @click="addItems">加入 →</button><button class="win-button" :disabled="!selectedDraftNos.length" @click="removeItems">← 移除</button></div>
        <section class="picker-column"><h3>群組品管項目</h3><div class="picker-list"><button v-for="(item, index) in draftDetails" :key="item.qcItemNo" class="picker-row selected-row" :class="{ picked: selectedDraftNos.includes(item.qcItemNo) }" @click="selectedDraftNos = selectedDraftNos.includes(item.qcItemNo) ? selectedDraftNos.filter(no => no !== item.qcItemNo) : [item.qcItemNo]" @dblclick="selectedDraftNos = [item.qcItemNo]; removeItems()"><span>{{ index + 1 }}.</span><span>{{ item.qcItemNo }}</span><span>{{ item.qcItemName }}</span></button><p v-if="!draftDetails.length" class="empty-row">尚未選取</p></div><div class="order-controls"><button class="win-button" :disabled="selectedDraftNos.length !== 1" @click="moveDraft(-1)">上移</button><button class="win-button" :disabled="selectedDraftNos.length !== 1" @click="moveDraft(1)">下移</button></div></section>
      </div><p v-if="error" class="dialog-error">{{ error }}</p><footer class="dialog-actions"><button class="win-button primary-button" :disabled="busy" @click="saveDetails">確定</button><button class="win-button" @click="detailDialogOpen = false">關閉</button></footer>
    </section></div>

    <div v-if="opDialogOpen" class="modal-backdrop" @click.self="opDialogOpen = false"><section class="dialog op-dialog" role="dialog" aria-modal="true" aria-label="作業站對應">
      <header class="dialog-heading"><div><h2>QC Item Operation · {{ activeGroup?.groupNo }}</h2><small>QCType=3 · {{ opDialogReadOnly ? '唯讀：只有已核准項目可修改' : '已核准群組可設定作業站' }}</small></div><button class="icon-close" aria-label="關閉" @click="opDialogOpen = false">×</button></header>
      <div class="grid-frame"><div class="grid-scroll"><table class="win-grid"><thead><tr><th>設定</th><th>作業站編號</th><th>作業站名稱</th><th>ExecuteStatus</th><th>FAI</th><th>IPQC</th><th>PQC</th></tr></thead><tbody>
        <tr v-for="op in operations" :key="op.opNo"><td><input type="checkbox" :checked="draftOperations.some(row => row.opNo === op.opNo)" :disabled="opDialogReadOnly" @change="toggleOperation(op, ($event.target as HTMLInputElement).checked)"></td><td>{{ op.opNo }}</td><td>{{ op.opName }}</td>
          <td><select :value="draftOperations.find(row => row.opNo === op.opNo)?.isInline ? 1 : 0" :disabled="opDialogReadOnly || !draftOperations.some(row => row.opNo === op.opNo) || activeGroup?.lotIsValid !== 1" @change="draftOperations = draftOperations.map(row => row.opNo === op.opNo ? { ...row, isInline: Number(($event.target as HTMLSelectElement).value) === 1 } : row)"><option value="1">InLine</option><option value="0">OffLine</option></select></td>
          <td><input type="checkbox" :checked="draftOperations.find(row => row.opNo === op.opNo)?.inspTypeFai || false" :disabled="opDialogReadOnly || !draftOperations.find(row => row.opNo === op.opNo)?.isInline" @change="draftOperations = draftOperations.map(row => row.opNo === op.opNo ? { ...row, inspTypeFai: ($event.target as HTMLInputElement).checked } : row)"></td>
          <td><input type="checkbox" :checked="draftOperations.find(row => row.opNo === op.opNo)?.inspTypeIpqc || false" :disabled="opDialogReadOnly || !draftOperations.find(row => row.opNo === op.opNo)?.isInline" @change="draftOperations = draftOperations.map(row => row.opNo === op.opNo ? { ...row, inspTypeIpqc: ($event.target as HTMLInputElement).checked } : row)"></td>
          <td><input type="checkbox" :checked="draftOperations.find(row => row.opNo === op.opNo)?.inspTypePqc || false" :disabled="opDialogReadOnly || !draftOperations.find(row => row.opNo === op.opNo)?.isInline" @change="draftOperations = draftOperations.map(row => row.opNo === op.opNo ? { ...row, inspTypePqc: ($event.target as HTMLInputElement).checked } : row)"></td>
        </tr><tr v-if="!operations.length"><td colspan="7" class="empty-row">沒有作業站資料</td></tr></tbody></table></div></div>
      <p v-if="activeGroup?.lotIsValid === 0" class="dialog-hint">Lot Valid Check=No，不可指定 InLine 作業站。</p><p v-if="error" class="dialog-error">{{ error }}</p>
      <footer class="dialog-actions"><button class="win-button primary-button" :disabled="busy || opDialogReadOnly" @click="saveOperations">確定</button><button class="win-button" @click="opDialogOpen = false">關閉</button></footer>
    </section></div>

    <div v-if="confirmDeleteOpen" class="modal-backdrop" @click.self="confirmDeleteOpen = false"><section class="dialog confirm-dialog" role="alertdialog" aria-modal="true"><h2>刪除群組</h2><p>未簽核/不使用資料將刪除；已核准資料將改為「不使用」。確定繼續？</p><footer class="dialog-actions"><button class="win-button danger-button" :disabled="busy" @click="deleteSelected">確定刪除</button><button class="win-button" @click="confirmDeleteOpen = false">取消</button></footer></section></div>
  </section>
</template>

<style scoped>
.qc-attribute-group-page{height:100%;min-height:540px;padding:10px 12px;box-sizing:border-box;display:flex;flex-direction:column;gap:8px;color:#333;font:13px "Segoe UI",Arial,sans-serif;background:#fff}
.page-heading{display:flex;align-items:center;justify-content:space-between}.page-heading h1{font-size:20px;font-weight:500;margin:0 0 2px}.page-heading p{font-size:11px;color:#777;margin:0}
.plain-button,.win-button{min-height:29px;padding:4px 11px;border:1px solid #bcbcbc;border-radius:3px;background:linear-gradient(#fff,#f5f5f5);color:#333;cursor:pointer;font:inherit;white-space:nowrap}.plain-button:hover,.win-button:hover:not(:disabled){background:#fff4d1;border-color:#d1a64d}.win-button:disabled{opacity:.48;cursor:not-allowed}.primary-button{background:#f7a52b;border-color:#df9126;color:#fff}.primary-button:hover:not(:disabled){background:#e99823}.danger-button{color:#9b3525}
.query-strip{min-height:38px;display:flex;align-items:center;gap:10px;padding:4px 8px;border:1px solid #d1d1d1;background:#fafafa;flex-wrap:wrap}.state-option{display:flex;align-items:center;gap:3px;white-space:nowrap}.state-option input{accent-color:#d99424}.query-label{margin-left:8px}.query-input{width:180px;min-height:27px;padding:3px 6px;border:1px solid #999;box-sizing:border-box;font:inherit}.percent-mark{margin-left:-7px}.search-button{min-width:55px}.query-spacer{flex:1}.result-count,.grid-footer{color:#777;font-size:11px}
.alert{display:flex;justify-content:space-between;align-items:center;padding:6px 10px;border:1px solid #ddd;font-size:12px}.alert button{border:0;background:transparent;font-size:17px;cursor:pointer}.error-alert{background:#fff0eb;border-color:#d7a89b;color:#8e3321}.success-alert{background:#f2f7ec;border-color:#c3d5ad;color:#4f6e35}
.win-group{position:relative;border:1px solid #bbb;padding:13px 8px 6px;min-width:0}.win-group>legend{padding:0 5px;color:#555;font-size:13px}.master-group{height:48%;min-height:250px;display:flex;flex-direction:column}.master-layout{display:grid;grid-template-columns:minmax(0,1fr) 94px;gap:8px;flex:1;min-height:0}.grid-frame{min-width:0;min-height:0;border:1px solid #b9b9b9;background:#fff}.grid-scroll{width:100%;height:100%;overflow:auto}.win-grid{border-collapse:collapse;width:100%;font-size:12px;white-space:nowrap}.win-grid th{position:sticky;top:0;z-index:1;background:#44a7e7;color:#fff;font-weight:500;text-align:left;padding:5px 8px;border:1px solid #d7e0e5}.win-grid td{height:25px;padding:2px 8px;border:1px solid #ccc;max-width:320px;overflow:hidden;text-overflow:ellipsis}.win-grid tr:nth-child(even){background:#fffaf0}.win-grid tbody tr{cursor:pointer}.win-grid tbody tr:hover{background:#fff3c5}.win-grid tbody tr.active{background:#ffe79a}.check-col{width:25px;text-align:center;padding:2px!important}.check-col input{accent-color:#d99424}
.action-rail{display:flex;flex-direction:column;gap:5px}.action-rail .win-button{min-height:31px;padding:3px 4px}.grid-footer{padding:5px 3px 0}
.detail-group{flex:1;min-height:150px;display:flex;flex-direction:column}.detail-toolbar{display:flex;align-items:center;gap:8px;margin:0 0 5px}.detail-toolbar span{flex:1;color:#555}.detail-grid-frame{flex:1}.empty-row{text-align:center!important;color:#888;padding:12px!important}
.modal-backdrop{position:fixed;inset:0;z-index:100;display:grid;place-items:center;background:rgba(25,30,35,.38);padding:18px}.dialog{width:min(650px,100%);max-height:calc(100vh - 36px);overflow:auto;padding:12px;background:#fff;border:1px solid #aaa;box-shadow:0 10px 32px #0003;box-sizing:border-box}.editor-dialog{width:min(540px,100%)}.picker-dialog{width:min(900px,100%)}.op-dialog{width:min(760px,100%)}.dialog-heading{display:flex;align-items:center;justify-content:space-between;gap:12px;border-bottom:1px solid #ddd;padding-bottom:8px}.dialog-heading h2{margin:0;font-size:17px;font-weight:600}.dialog-heading small{display:block;margin-top:4px;color:#777}.icon-close{border:0;background:transparent;color:#555;font-size:24px;cursor:pointer}
.editor-form{display:grid;grid-template-columns:1fr 1fr;gap:9px 14px;padding-top:12px}.form-row{display:grid;grid-template-columns:105px minmax(0,1fr);align-items:center;gap:6px}.form-row input,.form-row textarea{min-width:0;min-height:29px;padding:4px 7px;border:1px solid #999;box-sizing:border-box;font:inherit}.yellow-input{background:#fff0b3}.form-row input:read-only{background:#eee;color:#666}.option-group{grid-column:1/-1;display:flex;align-items:center;gap:14px;min-height:35px;margin:0;padding:4px 9px;border:1px solid #bbb}.option-group legend{padding:0 4px;color:#666;font-size:11px}.option-group label{display:flex;align-items:center;gap:4px}.option-group input{accent-color:#d99424}.description-row{grid-column:1/-1;align-items:start}.description-row textarea{height:66px;resize:vertical}.audit-panel{grid-column:1/-1;display:grid;grid-template-columns:1fr 1fr;gap:7px;padding:8px;border:1px solid #ddd;background:#fafafa}.audit-panel label{display:grid;grid-template-columns:58px minmax(0,1fr);align-items:center;gap:5px;color:#666}.audit-panel input{width:100%;min-height:25px;padding:3px 6px;border:1px solid #ccc;background:#eee;color:#666;box-sizing:border-box}.dialog-actions{display:flex;justify-content:flex-end;gap:8px;margin-top:11px}.dialog-error{color:#a03020}.confirm-dialog{width:min(440px,100%)}.confirm-dialog p{line-height:1.6;color:#555}
.picker-columns{display:grid;grid-template-columns:minmax(0,1fr) 88px minmax(0,1fr);gap:9px;margin-top:10px;min-height:300px}.picker-column{display:flex;flex-direction:column;min-width:0}.picker-column h3{margin:0 0 6px;font-size:13px;font-weight:500}.picker-search{width:100%;margin-bottom:6px}.picker-list{flex:1;max-height:48vh;overflow:auto;border:1px solid #ccc}.picker-row{display:grid;grid-template-columns:minmax(100px,.8fr) minmax(0,1.4fr);gap:8px;width:100%;padding:7px 8px;border:0;border-bottom:1px solid #eee;background:#fff;text-align:left;color:#333;font:inherit;cursor:pointer}.selected-row{grid-template-columns:33px minmax(100px,.8fr) minmax(0,1.4fr)}.picker-row:hover{background:#fff8de}.picker-row.picked{background:#ffe79a}.move-controls,.order-controls{display:flex;flex-direction:column;justify-content:center;gap:7px}.order-controls{flex-direction:row;margin-top:7px}.order-controls .win-button{flex:1}
.dialog-hint{margin:8px 0 0;color:#777;font-size:11px}.order-input{width:80px}
@media(max-width:850px){.qc-attribute-group-page{min-height:620px}.master-group{height:43%}.master-layout{grid-template-columns:minmax(0,1fr) 78px;gap:5px}.win-button{padding:3px 6px;font-size:11px}.picker-columns{grid-template-columns:minmax(0,1fr) 70px minmax(0,1fr)}}
@media(max-width:620px){.qc-attribute-group-page{padding:7px}.master-group{height:38%}.action-rail{gap:3px}.query-strip{gap:6px}.query-input{width:130px}.query-spacer{display:none}.editor-form{grid-template-columns:1fr}.option-group,.description-row,.audit-panel{grid-column:auto}.audit-panel{grid-template-columns:1fr}.picker-columns{grid-template-columns:1fr}.move-controls{flex-direction:row}.picker-list{max-height:190px}.win-grid{font-size:11px}}
</style>
