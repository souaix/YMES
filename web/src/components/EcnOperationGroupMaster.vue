<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type EditorMode = 'new' | 'edit' | 'copy' | ''
type IssueFilter = '0' | '1' | '2' | '-1'
interface GroupRow {
  opGroupNo: string; opGroupName: string; psNo: string; opGroupOrder: number; description: string
  issueState: number; creator: string; createDate: string; reviser: string; reviseDate: string; dataStamp: number | null
}
interface GroupOperation { opGroupNo: string; opNo: string; opName: string }
interface SegmentOption { psNo: string; psName: string }
interface OperationOption { opNo: string; opName: string; psNo: string; opType: string; opClass: number; issueState: number }
interface ApiEnvelope<T> { items?: T[]; rows?: T[]; data?: T[]; message?: string; title?: string }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes.dev.op-ecn-group-master.v1'
const seedSegments: SegmentOption[] = [
  { psNo: 'DEV-ASSY', psName: 'DEV 組裝區段' }, { psNo: 'DEV-QC', psName: 'DEV 品檢區段' }, { psNo: 'DEV-PACK', psName: 'DEV 包裝區段' },
]
const seedOperations: OperationOption[] = [
  { opNo: 'DEV-OP11', opName: 'DEV 上料', psNo: 'DEV-ASSY', opType: 'DEV-GEN', opClass: 0, issueState: 2 },
  { opNo: 'DEV-OP12', opName: 'DEV 組裝', psNo: 'DEV-ASSY', opType: 'DEV-GEN', opClass: 0, issueState: 2 },
  { opNo: 'DEV-OP13', opName: 'DEV 外觀檢查', psNo: 'DEV-ASSY', opType: 'DEV-QC', opClass: 1, issueState: 2 },
  { opNo: 'DEV-OP14', opName: 'DEV 包裝', psNo: 'DEV-PACK', opType: 'DEV-GEN', opClass: 0, issueState: 2 },
  { opNo: 'DEV-OP15', opName: 'DEV 抽驗', psNo: 'DEV-QC', opType: 'DEV-QC', opClass: 1, issueState: 2 },
  { opNo: 'DEV-OP16', opName: 'DEV 待核准站點（不可加入）', psNo: 'DEV-ASSY', opType: 'DEV-GEN', opClass: 0, issueState: 0 },
]
const seedGroups: GroupRow[] = [
  { opGroupNo: 'DEV-ECN-G01', opGroupName: 'DEV 組裝 ECN 群組', psNo: 'DEV-ASSY', opGroupOrder: 10, description: 'DEV 假資料：未簽核群組', issueState: 0, creator: 'DEV001', createDate: '2026-11-02T08:30:00', reviser: '', reviseDate: '', dataStamp: 0 },
  { opGroupNo: 'DEV-ECN-G02', opGroupName: 'DEV 品檢 ECN 群組', psNo: 'DEV-QC', opGroupOrder: 20, description: 'DEV 假資料：簽核中群組', issueState: 1, creator: 'DEV001', createDate: '2026-11-03T09:00:00', reviser: 'DEV002', reviseDate: '2026-11-03T09:20:00', dataStamp: 1 },
  { opGroupNo: 'DEV-ECN-G03', opGroupName: 'DEV 包裝 ECN 群組', psNo: 'DEV-PACK', opGroupOrder: 30, description: 'DEV 假資料：已核准群組', issueState: 2, creator: 'DEV001', createDate: '2026-11-04T10:00:00', reviser: 'DEV002', reviseDate: '2026-11-04T10:15:00', dataStamp: 1 },
  { opGroupNo: 'DEV-ECN-G04', opGroupName: 'DEV 停用 ECN 群組', psNo: 'DEV-ASSY', opGroupOrder: 40, description: 'DEV 假資料：不使用群組', issueState: -1, creator: 'DEV001', createDate: '2026-11-05T11:00:00', reviser: 'DEV002', reviseDate: '2026-11-05T11:10:00', dataStamp: 2 },
]
const seedDetails: GroupOperation[] = [
  { opGroupNo: 'DEV-ECN-G01', opNo: 'DEV-OP11', opName: 'DEV 上料' },
  { opGroupNo: 'DEV-ECN-G02', opNo: 'DEV-OP13', opName: 'DEV 外觀檢查' },
  { opGroupNo: 'DEV-ECN-G03', opNo: 'DEV-OP14', opName: 'DEV 包裝' },
  { opGroupNo: 'DEV-ECN-G04', opNo: 'DEV-OP12', opName: 'DEV 組裝' },
]

const rows = ref<GroupRow[]>([])
const details = ref<GroupOperation[]>([])
const allMockDetails = ref<GroupOperation[]>([])
const segments = ref<SegmentOption[]>([])
const operations = ref<OperationOption[]>([])
const activeGroupNo = ref('')
const activeOpNo = ref('')
const selectedGroupNos = ref<string[]>([])
const selectedOpNos = ref<string[]>([])
const issueFilter = ref<IssueFilter>('0')
const keyword = ref('')
const pickerKeyword = ref('')
const pickerSelection = ref<string[]>([])
const loading = ref(false)
const busy = ref(false)
const saving = ref(false)
const error = ref('')
const notice = ref('')
const headerMode = ref<EditorMode>('')
const headerDraft = ref(emptyHeaderDraft())
const headerStamp = ref<number | null>(0)
const headerAudit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const segmentPickerOpen = ref(false)
const operationPickerOpen = ref(false)
const activeGroup = computed(() => rows.value.find((row) => row.opGroupNo === activeGroupNo.value) ?? null)
const visibleGroups = computed(() => {
  const search = keyword.value.trim().toLocaleUpperCase()
  return rows.value.filter((row) => row.issueState === Number(issueFilter.value)
    && (!search || row.opGroupNo.toLocaleUpperCase().includes(search)))
    .sort((a, b) => a.opGroupOrder - b.opGroupOrder || a.opGroupNo.localeCompare(b.opGroupNo))
})
const visibleDetails = computed(() => details.value.filter((row) => row.opGroupNo === activeGroupNo.value))
const availableOperations = computed(() => {
  const search = pickerKeyword.value.trim().toLocaleUpperCase()
  const assigned = new Set((mock.value ? allMockDetails.value : details.value).map((row) => row.opNo))
  return operations.value.filter((row) => row.issueState === 2 && row.psNo === activeGroup.value?.psNo && !assigned.has(row.opNo)
    && (!search || row.opNo.toLocaleUpperCase().includes(search) || row.opName.toLocaleUpperCase().includes(search)
      || row.psNo.toLocaleUpperCase().includes(search) || row.opType.toLocaleUpperCase().includes(search)))
})

function emptyHeaderDraft() { return { opGroupNo: '', opGroupName: '', psNo: '', opGroupOrder: 0, description: '' } }
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function nowText(): string { return new Date().toISOString() }
function userNo(): string { return props.currentUserNo || (mock.value ? 'DEV001' : '') }
function displayDate(value: string): string {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return new Intl.DateTimeFormat('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23' }).format(date).replaceAll('-', '/')
}
function issueName(state: number): string { return ({ 0: '未簽核', 1: '簽核中', 2: '已簽核', '-1': '不使用' } as Record<string, string>)[String(state)] ?? String(state) }
function normalizeGroup(row: Record<string, unknown>): GroupRow {
  return {
    opGroupNo: String(row.opGroupNo ?? row.opgroupno ?? ''), opGroupName: String(row.opGroupName ?? row.opgroupname ?? ''),
    psNo: String(row.psNo ?? row.psno ?? ''), opGroupOrder: Number(row.opGroupOrder ?? row.opgrouporder ?? 0),
    description: String(row.description ?? ''), issueState: Number(row.issueState ?? row.issuestate ?? 0),
    creator: String(row.creator ?? ''), createDate: String(row.createDate ?? row.createdate ?? ''),
    reviser: String(row.reviser ?? ''), reviseDate: String(row.reviseDate ?? row.revisedate ?? ''),
    dataStamp: row.dataStamp == null && row.datastamp == null ? null : Number(row.dataStamp ?? row.datastamp),
  }
}
function normalizeDetail(row: Record<string, unknown>): GroupOperation {
  return { opGroupNo: String(row.opGroupNo ?? row.opgroupno ?? ''), opNo: String(row.opNo ?? row.opno ?? ''), opName: String(row.opName ?? row.opname ?? '') }
}
function normalizeSegment(row: Record<string, unknown>): SegmentOption {
  return { psNo: String(row.psNo ?? row.psno ?? ''), psName: String(row.psName ?? row.psname ?? row.description ?? '') }
}
function normalizeOperation(row: Record<string, unknown>): OperationOption {
  return {
    opNo: String(row.opNo ?? row.opno ?? ''), opName: String(row.opName ?? row.opname ?? ''),
    psNo: String(row.psNo ?? row.psno ?? ''), opType: String(row.opType ?? row.optype ?? ''),
    opClass: Number(row.opClass ?? row.opclass ?? 0), issueState: Number(row.issueState ?? row.issuestate ?? 0),
  }
}
function listFrom<T>(value: T[] | ApiEnvelope<T>): T[] {
  if (Array.isArray(value)) return value
  return value.items ?? value.rows ?? value.data ?? []
}
function apiPath(path: string): string {
  const base = props.apiBaseUrl.replace(/\/+$/, '')
  return base + path
}
async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiPath(path), { credentials: 'include', ...init })
  if (response.status === 401) { window.location.assign(import.meta.env.BASE_URL + 'login'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) {
    const body = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    if (response.status === 404) throw new Error('ECN 作業站群組 API 尚未建立，請依 todolist_homepc.md 完成資料表與 API。')
    throw new Error(body?.detail || body?.message || body?.title || 'API 回應錯誤 (' + response.status + ')。')
  }
  return response.status === 204 ? undefined as T : await response.json() as T
}
function persistMock(): void { localStorage.setItem(storageKey, JSON.stringify({ rows: rows.value, details: allMockDetails.value })) }
function loadMock(): void {
  const saved = localStorage.getItem(storageKey)
  if (!saved) { rows.value = clone(seedGroups); allMockDetails.value = clone(seedDetails); persistMock(); return }
  try {
    const parsed = JSON.parse(saved) as { rows?: GroupRow[]; details?: GroupOperation[] }
    rows.value = parsed.rows ?? clone(seedGroups); allMockDetails.value = parsed.details ?? clone(seedDetails)
  } catch { rows.value = clone(seedGroups); allMockDetails.value = clone(seedDetails); persistMock() }
}
async function loadRows(): Promise<void> {
  loading.value = true; error.value = ''
  try {
    if (mock.value) loadMock()
    else {
      const query = new URLSearchParams({ issueState: 'all', opGroupNo: keyword.value.trim() })
      const result = await api<GroupRow[] | ApiEnvelope<GroupRow>>('/api/ecn-operation-groups?' + query.toString())
      rows.value = listFrom(result).map((row) => normalizeGroup(row as unknown as Record<string, unknown>))
    }
    if (activeGroupNo.value && !rows.value.some((row) => row.opGroupNo === activeGroupNo.value && row.issueState === Number(issueFilter.value))) {
      activeGroupNo.value = ''; activeOpNo.value = ''; details.value = []
    }
    selectedGroupNos.value = []
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入 ECN 作業站群組失敗。' }
  finally { loading.value = false }
}
async function loadSegments(): Promise<void> {
  if (mock.value) { segments.value = clone(seedSegments); return }
  const result = await api<SegmentOption[] | ApiEnvelope<SegmentOption>>('/api/ecn-operation-groups/lookups/production-segments')
  segments.value = listFrom(result).map((row) => normalizeSegment(row as unknown as Record<string, unknown>))
}
async function loadDetails(groupNo: string): Promise<void> {
  if (!groupNo) { details.value = []; return }
  if (mock.value) { details.value = clone(allMockDetails.value.filter((row) => row.opGroupNo === groupNo)); return }
  const result = await api<GroupOperation[] | ApiEnvelope<GroupOperation>>('/api/ecn-operation-groups/' + encodeURIComponent(groupNo) + '/operations')
  details.value = listFrom(result).map((row) => normalizeDetail(row as unknown as Record<string, unknown>))
}
async function loadOperations(): Promise<void> {
  if (!activeGroup.value) return
  if (mock.value) { operations.value = clone(seedOperations); return }
  const query = new URLSearchParams({ psNo: activeGroup.value.psNo, issueState: '2', notInAnyGroup: 'true' })
  const result = await api<OperationOption[] | ApiEnvelope<OperationOption>>('/api/ecn-operation-groups/lookups/operations?' + query.toString())
  operations.value = listFrom(result).map((row) => normalizeOperation(row as unknown as Record<string, unknown>))
}
async function activateGroup(row: GroupRow): Promise<void> {
  activeGroupNo.value = row.opGroupNo; activeOpNo.value = ''; selectedOpNos.value = []; error.value = ''
  try { await loadDetails(row.opGroupNo) } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入 ECN 群組明細失敗。' }
}
function changeIssueFilter(value: IssueFilter): void {
  issueFilter.value = value
  if (activeGroup.value && activeGroup.value.issueState !== Number(value)) { activeGroupNo.value = ''; details.value = []; activeOpNo.value = '' }
  selectedGroupNos.value = []
}
function checkGroup(row: GroupRow, checked: boolean): void {
  selectedGroupNos.value = checked ? [...new Set([...selectedGroupNos.value, row.opGroupNo])] : selectedGroupNos.value.filter((value) => value !== row.opGroupNo)
}
function selectAllGroups(checked: boolean): void { selectedGroupNos.value = checked ? visibleGroups.value.map((row) => row.opGroupNo) : [] }
function checkOperation(row: GroupOperation, checked: boolean): void {
  selectedOpNos.value = checked ? [...new Set([...selectedOpNos.value, row.opNo])] : selectedOpNos.value.filter((value) => value !== row.opNo)
}
function selectAllOperations(checked: boolean): void { selectedOpNos.value = checked ? visibleDetails.value.map((row) => row.opNo) : [] }
function openHeader(mode: Exclude<EditorMode, ''>): void {
  error.value = ''; notice.value = ''
  if (mode !== 'new' && !activeGroup.value) return
  if (mode === 'edit' && activeGroup.value?.issueState !== 0) { error.value = '只有未簽核的 ECN 作業站群組可以修改。'; return }
  headerMode.value = mode
  const source = mode === 'new' ? null : activeGroup.value
  headerDraft.value = source ? { opGroupNo: mode === 'copy' ? '' : source.opGroupNo, opGroupName: source.opGroupName, psNo: source.psNo, opGroupOrder: source.opGroupOrder, description: source.description } : emptyHeaderDraft()
  headerStamp.value = source ? source.dataStamp : null
  headerAudit.value = source ? { creator: source.creator, createDate: source.createDate, reviser: source.reviser, reviseDate: source.reviseDate } : { creator: userNo(), createDate: nowText(), reviser: '', reviseDate: '' }
  void loadSegments().catch((cause) => { error.value = cause instanceof Error ? cause.message : '載入生產區段失敗。' })
}
function validateHeader(): string | null {
  const draft = headerDraft.value
  draft.opGroupNo = draft.opGroupNo.trim().toLocaleUpperCase()
  draft.opGroupName = draft.opGroupName.trim(); draft.psNo = draft.psNo.trim()
  if (!draft.opGroupNo) return '群組編號為必填欄位。'
  if (draft.opGroupNo.length > 20) return '群組編號最多 20 個字元。'
  if (!draft.opGroupName) return '群組名稱為必填欄位。'
  if (draft.opGroupName.length > 50) return '群組名稱最多 50 個字元。'
  if (!draft.psNo) return '生產區段為必填欄位。'
  if (draft.psNo.length > 50) return '生產區段最多 50 個字元。'
  if (!Number.isInteger(Number(draft.opGroupOrder)) || Number(draft.opGroupOrder) < 0 || Number(draft.opGroupOrder) > 999999) return '群組順序須為 0 至 999999 的整數。'
  if (draft.description.length > 255) return '說明最多 255 個字元。'
  if (headerMode.value !== 'edit' && rows.value.some((row) => row.opGroupNo.toLocaleUpperCase() === draft.opGroupNo)) return '群組編號已存在。'
  return null
}
async function saveHeader(closeAfter: boolean): Promise<void> {
  const validation = validateHeader()
  if (validation) { error.value = validation; return }
  saving.value = true; error.value = ''; notice.value = ''
  const draft = { ...headerDraft.value, opGroupOrder: Number(headerDraft.value.opGroupOrder), description: headerDraft.value.description.trim() }
  try {
    if (mock.value) {
      const now = nowText()
      if (headerMode.value === 'edit') rows.value = rows.value.map((row) => row.opGroupNo === activeGroupNo.value ? { ...row, ...draft, reviser: userNo(), reviseDate: now, dataStamp: (row.dataStamp ?? 0) + 1 } : row)
      else rows.value = [{ ...draft, issueState: 0, creator: userNo(), createDate: now, reviser: '', reviseDate: '', dataStamp: 0 }, ...rows.value]
      persistMock()
    } else if (headerMode.value === 'edit') {
      await api('/api/ecn-operation-groups/' + encodeURIComponent(activeGroupNo.value), { method: 'PUT', body: JSON.stringify({ ...draft, dataStamp: headerStamp.value }) })
    } else await api('/api/ecn-operation-groups', { method: 'POST', body: JSON.stringify(draft) })
    activeGroupNo.value = draft.opGroupNo
    await loadRows(); await loadDetails(draft.opGroupNo)
    const saved = rows.value.find((row) => row.opGroupNo === draft.opGroupNo)
    if (saved) {
      headerStamp.value = saved.dataStamp
      headerAudit.value = { creator: saved.creator, createDate: saved.createDate, reviser: saved.reviser, reviseDate: saved.reviseDate }
    }
    if (closeAfter) headerMode.value = ''
    else if (headerMode.value !== 'edit') headerDraft.value = emptyHeaderDraft()
    notice.value = 'ECN 作業站群組資料已儲存。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '儲存 ECN 作業站群組失敗。' }
  finally { saving.value = false }
}
async function deleteGroups(): Promise<void> {
  const targets = selectedGroupNos.value.length ? [...selectedGroupNos.value] : activeGroup.value ? [activeGroup.value.opGroupNo] : []
  if (!targets.length || !window.confirm('確定刪除選取的 ECN 作業站群組？已核准資料會改為不使用並保留明細。')) return
  busy.value = true; error.value = ''; notice.value = ''; let changed = false
  try {
    for (const groupNo of targets) {
      const row = rows.value.find((item) => item.opGroupNo === groupNo)
      if (!row) continue
      if (row.issueState === 1) throw new Error('簽核中的 ECN 作業站群組不可刪除。')
      if (mock.value) {
        if (row.issueState === 2) rows.value = rows.value.map((item) => item.opGroupNo === groupNo ? { ...item, issueState: -1, reviser: userNo(), reviseDate: nowText(), dataStamp: (item.dataStamp ?? 0) + 1 } : item)
        else { rows.value = rows.value.filter((item) => item.opGroupNo !== groupNo); allMockDetails.value = allMockDetails.value.filter((item) => item.opGroupNo !== groupNo); details.value = details.value.filter((item) => item.opGroupNo !== groupNo) }
        persistMock()
      } else await api('/api/ecn-operation-groups/' + encodeURIComponent(groupNo), { method: 'DELETE', body: JSON.stringify({ issueState: row.issueState, dataStamp: row.dataStamp }) })
      changed = true
    }
    notice.value = '已完成選取群組的刪除或停用。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '刪除 ECN 作業站群組失敗。' }
  finally {
    if (activeGroupNo.value && !rows.value.some((row) => row.opGroupNo === activeGroupNo.value && row.issueState === Number(issueFilter.value))) { activeGroupNo.value = ''; details.value = [] }
    selectedGroupNos.value = []; busy.value = false
    if (changed) await loadRows()
  }
}
async function approveGroups(): Promise<void> {
  const targets = selectedGroupNos.value.length ? [...selectedGroupNos.value] : activeGroup.value ? [activeGroup.value.opGroupNo] : []
  if (!targets.length || !window.confirm('確定送出選取的未簽核 ECN 作業站群組？')) return
  busy.value = true; error.value = ''; notice.value = ''; let changed = false
  try {
    for (const groupNo of targets) {
      const row = rows.value.find((item) => item.opGroupNo === groupNo)
      if (!row || row.issueState !== 0) throw new Error('只有未簽核的 ECN 作業站群組可以核准。')
      if (mock.value) { rows.value = rows.value.map((item) => item.opGroupNo === groupNo ? { ...item, issueState: 2, reviser: userNo(), reviseDate: nowText(), dataStamp: (item.dataStamp ?? 0) + 1 } : item); persistMock() }
      else await api('/api/ecn-operation-groups/' + encodeURIComponent(groupNo) + '/approve', { method: 'POST', body: JSON.stringify({ dataStamp: row.dataStamp }) })
      changed = true
    }
    notice.value = mock.value ? 'ECN 作業站群組已依 DEV 無簽核流程直接核准分支更新。' : '已送出 ECN 作業站群組核准，請依簽核狀態確認處理結果。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '核准 ECN 作業站群組失敗。' }
  finally { selectedGroupNos.value = []; busy.value = false; if (changed) await loadRows() }
}
async function openOperationPicker(): Promise<void> {
  if (!activeGroup.value) return
  error.value = ''; pickerKeyword.value = ''; pickerSelection.value = []
  try { await loadOperations(); operationPickerOpen.value = true }
  catch (cause) { error.value = cause instanceof Error ? cause.message : '載入可加入的作業站失敗。' }
}
function pickOperation(opNo: string, checked: boolean): void {
  pickerSelection.value = checked ? [...new Set([...pickerSelection.value, opNo])] : pickerSelection.value.filter((value) => value !== opNo)
}
async function addOperations(): Promise<void> {
  if (!activeGroup.value || !pickerSelection.value.length) { error.value = '請至少選擇一個作業站。'; return }
  busy.value = true; error.value = ''; let changed = false
  try {
    const groupNo = activeGroup.value.opGroupNo
    for (const opNo of pickerSelection.value) {
      const operation = operations.value.find((item) => item.opNo === opNo)
      if (!operation || operation.issueState !== 2 || operation.psNo !== activeGroup.value.psNo || (mock.value && allMockDetails.value.some((item) => item.opNo === opNo))) throw new Error('作業站必須已核准、與群組屬於相同生產區段，且尚未加入任何 ECN 群組。')
      if (mock.value) { const added = { opGroupNo: groupNo, opNo, opName: operation.opName }; allMockDetails.value.push(added); details.value.push(added); persistMock() }
      else await api('/api/ecn-operation-groups/' + encodeURIComponent(groupNo) + '/operations', { method: 'POST', body: JSON.stringify({ opNo }) })
      changed = true
    }
    operationPickerOpen.value = false; pickerSelection.value = []; selectedOpNos.value = []
    notice.value = '已加入 ' + (changed ? '選取的作業站。' : '0 筆作業站。')
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '加入 ECN 群組作業站失敗。' }
  finally {
    busy.value = false
    if (changed && !mock.value && activeGroupNo.value) {
      try { await loadDetails(activeGroupNo.value) } catch (cause) { error.value = cause instanceof Error ? cause.message : '重新載入 ECN 群組明細失敗。' }
    }
  }
}
async function deleteOperations(): Promise<void> {
  const targets = selectedOpNos.value.length ? [...selectedOpNos.value] : activeOpNo.value ? [activeOpNo.value] : []
  if (!activeGroup.value || !targets.length || !window.confirm('確定刪除選取的群組作業站明細？')) return
  busy.value = true; error.value = ''; let changed = false
  try {
    const groupNo = activeGroup.value.opGroupNo
    for (const opNo of targets) {
      if (mock.value) { allMockDetails.value = allMockDetails.value.filter((item) => !(item.opGroupNo === groupNo && item.opNo === opNo)); details.value = details.value.filter((item) => !(item.opGroupNo === groupNo && item.opNo === opNo)); persistMock() }
      else await api('/api/ecn-operation-groups/' + encodeURIComponent(groupNo) + '/operations/' + encodeURIComponent(opNo), { method: 'DELETE' })
      changed = true
    }
    selectedOpNos.value = []; activeOpNo.value = ''
    notice.value = '已刪除選取的群組作業站明細。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '刪除 ECN 群組作業站明細失敗。' }
  finally { busy.value = false; if (changed && !mock.value && activeGroupNo.value) await loadDetails(activeGroupNo.value) }
}
function chooseSegment(option: SegmentOption): void { headerDraft.value.psNo = option.psNo; segmentPickerOpen.value = false }
function operationClassName(value: number): string { return ({ 0: '一般作業站', 1: '品質管理', 2: '其他作業站' } as Record<number, string>)[value] ?? String(value) }
onMounted(() => { void loadRows() })
</script>

<template>
  <div class="ecn-group-page">
    <div class="query-strip">
      <fieldset class="issue-filter"><legend>ECN 作業站群組</legend>
        <label><input type="radio" name="ecn-issue-state" :checked="issueFilter === '0'" @change="changeIssueFilter('0')">未簽核</label>
        <label><input type="radio" name="ecn-issue-state" :checked="issueFilter === '1'" @change="changeIssueFilter('1')">簽核中</label>
        <label><input type="radio" name="ecn-issue-state" :checked="issueFilter === '2'" @change="changeIssueFilter('2')">已簽核</label>
        <label><input type="radio" name="ecn-issue-state" :checked="issueFilter === '-1'" @change="changeIssueFilter('-1')">不使用</label>
      </fieldset>
      <label class="query-field"><span>ECN 群組編號</span><input v-model="keyword" class="kd-input query-input" maxlength="20" @keydown.enter="loadRows"></label>
      <span class="query-percent">%</span><button class="kd-button" type="button" :disabled="loading" @click="loadRows">查詢</button>
    </div>
    <p v-if="error" class="message error-message" role="alert">{{ error }}</p>
    <p v-else-if="notice" class="message notice-message" role="status">{{ notice }}</p>
    <fieldset class="win-group master-panel"><legend>ECN OP Group Basis</legend>
      <div class="panel-layout">
        <div class="grid-scroll"><table class="kd-grid">
          <thead><tr>
            <th class="check-cell"><input type="checkbox" aria-label="全選群組" :checked="visibleGroups.length > 0 && visibleGroups.every((row) => selectedGroupNos.includes(row.opGroupNo))" @change="selectAllGroups(($event.target as HTMLInputElement).checked)"></th>
            <th>OPGroupNo</th><th>OPGroupName</th><th>PSNo</th><th>OPGroupOrder</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th>
          </tr></thead><tbody>
            <tr v-for="row in visibleGroups" :key="row.opGroupNo" :class="{ active: activeGroupNo === row.opGroupNo }" @click="activateGroup(row)" @dblclick="openHeader('edit')">
              <td class="check-cell" @click.stop><input type="checkbox" :checked="selectedGroupNos.includes(row.opGroupNo)" :aria-label="'選取 ' + row.opGroupNo" @change="checkGroup(row, ($event.target as HTMLInputElement).checked)"></td>
              <td>{{ row.opGroupNo }}</td><td>{{ row.opGroupName }}</td><td>{{ row.psNo }}</td><td class="number-cell">{{ row.opGroupOrder }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ displayDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ displayDate(row.reviseDate) }}</td>
            </tr>
            <tr v-if="!loading && visibleGroups.length === 0"><td colspan="10" class="empty-row">沒有符合條件的 ECN 作業站群組</td></tr>
          </tbody>
        </table></div>
        <aside class="vertical-actions">
          <button class="kd-button primary" type="button" :disabled="busy || loading" @click="openHeader('new')">新增 (A)</button>
          <button class="kd-button" type="button" :disabled="!activeGroup || activeGroup.issueState !== 0 || busy" @click="openHeader('edit')">修改 (E)</button>
          <button class="kd-button" type="button" :disabled="(!activeGroup && !selectedGroupNos.length) || busy" @click="deleteGroups">刪除 (D)</button>
          <button class="kd-button" type="button" :disabled="!activeGroup || busy" @click="openHeader('copy')">複製 (C)</button>
          <button class="kd-button" type="button" :disabled="(!activeGroup && !selectedGroupNos.length) || busy" @click="approveGroups">核准 (Y)</button>
        </aside>
      </div>
      <div class="grid-status">{{ loading ? '載入中…' : '資料總筆數 ' + visibleGroups.length }}<span v-if="activeGroup">　選取：{{ activeGroup.opGroupNo }}（{{ issueName(activeGroup.issueState) }}）</span></div>
    </fieldset>
    <fieldset class="win-group detail-panel"><legend>ECN OP Group Detail<span v-if="activeGroup"> — {{ activeGroup.opGroupNo }}</span></legend>
      <div class="panel-layout">
        <div class="grid-scroll"><table class="kd-grid detail-grid">
          <thead><tr><th class="check-cell"><input type="checkbox" aria-label="全選明細" :disabled="!visibleDetails.length" :checked="visibleDetails.length > 0 && visibleDetails.every((row) => selectedOpNos.includes(row.opNo))" @change="selectAllOperations(($event.target as HTMLInputElement).checked)"></th><th>OPNo</th><th>OPName</th></tr></thead>
          <tbody>
            <tr v-for="row in visibleDetails" :key="row.opNo" :class="{ active: activeOpNo === row.opNo }" @click="activeOpNo = row.opNo" @dblclick="activeOpNo = row.opNo">
              <td class="check-cell" @click.stop><input type="checkbox" :checked="selectedOpNos.includes(row.opNo)" :aria-label="'選取 ' + row.opNo" @change="checkOperation(row, ($event.target as HTMLInputElement).checked)"></td><td>{{ row.opNo }}</td><td>{{ row.opName }}</td>
            </tr>
            <tr v-if="activeGroup && !visibleDetails.length"><td colspan="3" class="empty-row">此 ECN 群組尚未設定作業站</td></tr>
            <tr v-if="!activeGroup"><td colspan="3" class="empty-row">請先選取上方 ECN 作業站群組</td></tr>
          </tbody>
        </table></div>
        <aside class="vertical-actions"><button class="kd-button primary" type="button" :disabled="!activeGroup || busy" @click="openOperationPicker">新增 (A)</button><button class="kd-button" type="button" :disabled="!activeGroup || (!activeOpNo && !selectedOpNos.length) || busy" @click="deleteOperations">刪除 (D)</button></aside>
      </div>
      <div class="grid-status">{{ activeGroup ? '資料總筆數 ' + visibleDetails.length : '未選取群組' }}</div>
    </fieldset>
    <div class="footer-actions"><span class="environment-note">DEV 預覽：瀏覽器 localStorage 假資料</span><button class="kd-button" type="button" @click="emit('close')">關閉</button></div>

    <div v-if="headerMode" class="dialog-backdrop" @click.self="headerMode = ''">
      <section class="win-dialog header-dialog" role="dialog" aria-modal="true" :aria-label="headerMode === 'edit' ? '修改 ECN 作業站群組' : headerMode === 'copy' ? '複製 ECN 作業站群組' : '新增 ECN 作業站群組'">
        <header class="dialog-title">{{ headerMode === 'edit' ? 'ECN OP Group Basis Define (EDIT)' : headerMode === 'copy' ? 'ECN OP Group Basis Define (COPY)' : 'ECN OP Group Basis Define (ADD)' }}</header>
        <fieldset class="form-group"><legend>ECN 作業站群組資料</legend>
          <div class="form-grid">
            <label>群組編號<input v-model="headerDraft.opGroupNo" class="kd-input" maxlength="20" :readonly="headerMode === 'edit'" @blur="headerDraft.opGroupNo = headerDraft.opGroupNo.toLocaleUpperCase()"></label>
            <label>群組名稱<input v-model="headerDraft.opGroupName" class="kd-input" maxlength="50"></label>
            <label>生產區段<div class="lookup-control"><input v-model="headerDraft.psNo" class="kd-input" maxlength="50" readonly><button class="kd-button lookup-button" type="button" @click="segmentPickerOpen = true">...</button></div></label>
            <label>群組順序<input v-model.number="headerDraft.opGroupOrder" class="kd-input number-input" type="number" min="0" max="999999" step="1"></label>
            <label class="description-label">說明<textarea v-model="headerDraft.description" class="kd-input" maxlength="255" rows="3"></textarea></label>
          </div>
          <div class="audit-row"><label>建立人<input class="audit-input" :value="headerAudit.creator" readonly></label><label>建立日期<input class="audit-input" :value="displayDate(headerAudit.createDate)" readonly></label><label>修改人<input class="audit-input" :value="headerAudit.reviser" readonly></label><label>修改日期<input class="audit-input" :value="displayDate(headerAudit.reviseDate)" readonly></label></div>
        </fieldset>
        <p v-if="error" class="dialog-error" role="alert">{{ error }}</p>
        <footer class="dialog-actions"><button class="kd-button primary" type="button" :disabled="saving" @click="saveHeader(true)">確定</button><button class="kd-button" type="button" :disabled="saving" @click="headerMode = ''">關閉</button><button class="kd-button" type="button" :disabled="saving" @click="saveHeader(false)">套用</button></footer>
      </section>
    </div>

    <div v-if="segmentPickerOpen" class="dialog-backdrop" @click.self="segmentPickerOpen = false"><section class="win-dialog picker-dialog" role="dialog" aria-modal="true" aria-label="選擇生產區段"><header class="dialog-title">選擇生產區段</header><div class="picker-scroll"><table class="kd-grid"><thead><tr><th>PSNo</th><th>PSName</th></tr></thead><tbody><tr v-for="row in segments" :key="row.psNo" @click="chooseSegment(row)"><td>{{ row.psNo }}</td><td>{{ row.psName }}</td></tr><tr v-if="!segments.length"><td colspan="2" class="empty-row">沒有可選擇的生產區段</td></tr></tbody></table></div><footer class="dialog-actions"><button class="kd-button" type="button" @click="segmentPickerOpen = false">關閉</button></footer></section></div>

    <div v-if="operationPickerOpen" class="dialog-backdrop" @click.self="operationPickerOpen = false"><section class="win-dialog picker-dialog operation-picker" role="dialog" aria-modal="true" aria-label="加入作業站"><header class="dialog-title">選擇可加入的作業站 — {{ activeGroup?.psNo }}</header><div class="picker-search"><input v-model="pickerKeyword" class="kd-input" placeholder="作業站編號／名稱／類別"><button class="kd-button" type="button" @click="pickerSelection = availableOperations.map((row) => row.opNo)">全選</button><button class="kd-button" type="button" @click="pickerSelection = []">全不選</button></div><div class="picker-scroll"><table class="kd-grid"><thead><tr><th class="check-cell"></th><th>OPNo</th><th>OPName</th><th>PSNo</th><th>OPType</th><th>OPClass</th></tr></thead><tbody><tr v-for="row in availableOperations" :key="row.opNo"><td class="check-cell"><input type="checkbox" :checked="pickerSelection.includes(row.opNo)" @change="pickOperation(row.opNo, ($event.target as HTMLInputElement).checked)"></td><td>{{ row.opNo }}</td><td>{{ row.opName }}</td><td>{{ row.psNo }}</td><td>{{ row.opType }}</td><td>{{ operationClassName(row.opClass) }}</td></tr><tr v-if="!availableOperations.length"><td colspan="6" class="empty-row">沒有可加入的已核准作業站</td></tr></tbody></table></div><footer class="dialog-actions"><button class="kd-button primary" type="button" :disabled="busy || !pickerSelection.length" @click="addOperations">加入選取項目</button><button class="kd-button" type="button" :disabled="busy" @click="operationPickerOpen = false">關閉</button></footer></section></div>
  </div>
</template>

<style scoped>
.ecn-group-page{box-sizing:border-box;height:calc(100vh - 116px);min-height:560px;padding:8px 10px 9px;background:#fff;color:#303030;font:13px "Microsoft JhengHei",Arial,sans-serif;display:flex;flex-direction:column;gap:6px;overflow:auto}
.query-strip{min-height:44px;display:flex;align-items:center;gap:8px;flex-wrap:wrap;border:1px solid #d1d1d1;padding:4px 8px;background:#fff}
.issue-filter{display:flex;align-items:center;gap:12px;margin:0 6px 0 0;border:0;padding:0}.issue-filter legend{float:left;margin-right:8px;color:#555}.issue-filter label{display:inline-flex;align-items:center;gap:3px;white-space:nowrap}.issue-filter input{accent-color:#d59a2c}
.query-field{display:inline-flex;align-items:center;gap:7px}.query-input{width:180px}.query-percent{color:#555}
.kd-input{box-sizing:border-box;min-width:0;border:1px solid #999;background:#fff0b5;color:#333;padding:5px 7px;font:inherit;height:29px}.kd-input:focus{outline:1px solid #d0a243;box-shadow:0 0 0 1px #f5d88a}.kd-input[readonly],.kd-input:disabled{background:#eee;color:#777}
.kd-button{min-height:30px;border:1px solid #b9b9b9;border-radius:3px;background:linear-gradient(#fff,#f1f1f1);color:#333;padding:4px 10px;font:inherit;white-space:nowrap;cursor:pointer}.kd-button:hover:not(:disabled){background:#fff1c5;border-color:#cba64b}.kd-button:disabled{opacity:.52;cursor:default}.kd-button.primary{background:#ffa62d;border-color:#ef9419;color:white;font-weight:600}.kd-button.primary:hover:not(:disabled){background:#f29419}
.win-group{box-sizing:border-box;min-width:0;margin:0;border:1px solid #bdbdbd;padding:8px 8px 6px}.win-group legend{padding:0 5px;font-size:14px;color:#4a4a4a}.master-panel{height:48%;min-height:260px;flex:1 1 48%}.detail-panel{height:38%;min-height:200px;flex:1 1 38%}.panel-layout{display:flex;gap:8px;min-height:0;height:calc(100% - 26px)}.grid-scroll{flex:1;min-width:0;overflow:auto;border:1px solid #d0d0d0}.kd-grid{width:max-content;min-width:100%;border-collapse:collapse;background:#fff;font-size:13px}.kd-grid th{position:sticky;top:0;z-index:1;background:#4d9ed8;color:#fff;font-weight:500;text-align:left;white-space:nowrap}.kd-grid th,.kd-grid td{height:27px;padding:3px 8px;border:1px solid #d1d1d1;white-space:nowrap}.kd-grid tbody tr{cursor:pointer}.kd-grid tbody tr:hover{background:#fff4cf}.kd-grid tbody tr.active{background:#ffe79b}.kd-grid .check-cell{text-align:center;width:27px;padding:2px}.kd-grid input[type=checkbox]{accent-color:#d59a2c}.number-cell{text-align:right}.empty-row{text-align:center;color:#777;height:48px!important}.vertical-actions{width:88px;flex:0 0 88px;display:flex;flex-direction:column;gap:6px}.vertical-actions .kd-button{width:100%}.grid-status{height:20px;padding:3px 2px 0;color:#777;font-size:12px}.detail-grid{min-width:480px}
.message{margin:0;padding:5px 8px;border:1px solid #d8d8d8}.error-message,.dialog-error{color:#8f2020;background:#fff0ee;border-color:#d5aaa5}.notice-message{color:#315d25;background:#f1f8e9;border-color:#bed4b3}
.footer-actions{display:flex;justify-content:flex-end;align-items:center;gap:8px;min-height:34px}.environment-note{margin-right:auto;color:#888;font-size:12px}
.dialog-backdrop{position:fixed;inset:0;z-index:1100;display:flex;align-items:center;justify-content:center;padding:14px;background:#0006}.win-dialog{box-sizing:border-box;background:#fff;border:1px solid #aaa;box-shadow:0 5px 18px #0004;padding:12px}.header-dialog{width:min(700px,calc(100vw - 28px))}.dialog-title{font-size:16px;font-weight:600;padding:0 2px 10px;border-bottom:1px solid #ddd;margin-bottom:10px}.form-group{margin:0;border:1px solid #c4c4c4;padding:10px}.form-group legend{padding:0 5px}.form-grid{display:grid;grid-template-columns:1fr 1fr;gap:10px 18px}.form-grid>label{display:flex;flex-direction:column;gap:4px}.lookup-control{display:flex;gap:5px}.lookup-control .kd-input{flex:1}.lookup-button{width:38px}.number-input{max-width:140px}.description-label{grid-column:1 / -1}.description-label textarea{height:72px;resize:vertical}.audit-row{display:grid;grid-template-columns:1fr 1fr;gap:6px 10px;margin-top:10px;padding:7px;border:1px solid #ddd}.audit-row label{display:grid;grid-template-columns:58px 1fr;align-items:center;gap:6px}.audit-input{box-sizing:border-box;width:100%;height:26px;border:1px solid #ccc;background:#eee;color:#777;padding:3px 5px;font:inherit}.dialog-actions{display:flex;justify-content:flex-end;gap:7px;margin-top:10px}.dialog-actions .primary{min-width:90px}.dialog-error{padding:5px 7px;margin:8px 0 0;border:1px solid}
.picker-dialog{width:min(760px,calc(100vw - 28px));max-height:min(680px,calc(100vh - 28px));display:flex;flex-direction:column}.picker-scroll{overflow:auto;max-height:430px;border:1px solid #d0d0d0}.picker-scroll .kd-grid{width:100%}.picker-scroll tbody tr{cursor:pointer}.picker-scroll tbody tr:hover{background:#fff1c5}.picker-search{display:flex;gap:6px;margin-bottom:8px}.picker-search .kd-input{flex:1}
@media(max-width:900px){.ecn-group-page{height:auto;min-height:100%}.issue-filter{width:100%;flex-wrap:wrap}.master-panel{min-height:300px}.detail-panel{min-height:230px}}
@media(max-width:620px){.panel-layout{height:auto;flex-direction:column}.master-panel,.detail-panel{height:auto;flex:auto;min-height:320px}.grid-scroll{min-height:220px}.vertical-actions{width:auto;flex:0 0 auto;flex-direction:row;flex-wrap:wrap}.vertical-actions .kd-button{width:auto;flex:1 1 75px}.form-grid{grid-template-columns:1fr}.description-label{grid-column:auto}.audit-row{grid-template-columns:1fr}.environment-note{display:none}.query-input{width:140px}}
</style>
