<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'

type EditorMode = 'new' | 'edit' | 'copy' | ''
type RoleFlag = 0 | 1 | 2
interface GroupRow {
  opGroupNo: string; opGroupName: string; psNo: string | null; opGroupOrder: number | null; description: string | null; erpNo?: string | null
  creator: string | null; createDate: string | null; reviser: string | null; reviseDate: string | null; dataStamp: number | null
}
interface MemberRow {
  opGroupNo: string; opNo: string; opName: string | null; opRoleFlag: RoleFlag; weightRate: number
  creator: string | null; createDate: string | null; reviser: string | null; reviseDate: string | null; dataStamp: number | null
}
interface SegmentOption { psNo: string; psName: string | null }
interface OperationOption { opNo: string; opName: string; psNo: string | null; issueState: number }
interface ApiEnvelope<T> { items: T[] }
interface MembersResult { group: Record<string, unknown>; items: Record<string, unknown>[] }
interface MemberSavedResult { group: Record<string, unknown>; item: Record<string, unknown> }
interface MockStore { groups: GroupRow[]; members: MemberRow[] }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes.dev.op-group-master.v1'
const basePath = import.meta.env.BASE_URL.replace(/\/+$/, '')
const maxStamp = 99999999999
const seedSegments: SegmentOption[] = [
  { psNo: 'DEV-ASSY', psName: 'DEV 組裝區段' }, { psNo: 'DEV-QC', psName: 'DEV 品檢區段' }, { psNo: 'DEV-PACK', psName: 'DEV 包裝區段' },
]
const seedOperations: OperationOption[] = [
  { opNo: 'DEV-OP10', opName: 'DEV 上料', psNo: 'DEV-ASSY', issueState: 2 },
  { opNo: 'DEV-OP20', opName: 'DEV 組裝', psNo: 'DEV-ASSY', issueState: 2 },
  { opNo: 'DEV-OP30', opName: 'DEV 外觀檢查', psNo: 'DEV-ASSY', issueState: 2 },
  { opNo: 'DEV-QC10', opName: 'DEV 品質檢驗', psNo: 'DEV-QC', issueState: 2 },
  { opNo: 'DEV-QC20', opName: 'DEV 抽驗', psNo: 'DEV-QC', issueState: 2 },
  { opNo: 'DEV-PK10', opName: 'DEV 裝箱', psNo: 'DEV-PACK', issueState: 2 },
  { opNo: 'DEV-DRAFT', opName: 'DEV 未核准站點（不可選）', psNo: 'DEV-ASSY', issueState: 0 },
]
const seedGroups: GroupRow[] = [
  { opGroupNo: 'DEV-ASSY-G1', opGroupName: 'DEV 組裝作業群組', psNo: 'DEV-ASSY', opGroupOrder: 10, description: 'DEV 假資料：標準組裝群組', creator: 'DEV001', createDate: '2026-11-01T08:30:00', reviser: 'DEV002', reviseDate: '2026-11-07T10:15:00', dataStamp: 2 },
  { opGroupNo: 'DEV-QC-G1', opGroupName: 'DEV 品檢群組', psNo: 'DEV-QC', opGroupOrder: 20, description: 'DEV 假資料：品檢作業群組', creator: 'DEV001', createDate: '2026-11-02T09:00:00', reviser: '', reviseDate: '', dataStamp: 0 },
  { opGroupNo: 'DEV-PACK-G1', opGroupName: 'DEV 包裝群組', psNo: 'DEV-PACK', opGroupOrder: 30, description: 'DEV 假資料：包裝群組', creator: 'DEV001', createDate: '2026-11-03T10:00:00', reviser: '', reviseDate: '', dataStamp: 0 },
]
const seedMembers: MemberRow[] = [
  { opGroupNo: 'DEV-ASSY-G1', opNo: 'DEV-OP10', opName: 'DEV 上料', opRoleFlag: 1, weightRate: 40, creator: 'DEV001', createDate: '2026-11-01T08:35:00', reviser: '', reviseDate: '', dataStamp: 0 },
  { opGroupNo: 'DEV-ASSY-G1', opNo: 'DEV-OP20', opName: 'DEV 組裝', opRoleFlag: 0, weightRate: 35, creator: 'DEV001', createDate: '2026-11-01T08:36:00', reviser: 'DEV002', reviseDate: '2026-11-08T11:20:00', dataStamp: 1 },
  { opGroupNo: 'DEV-ASSY-G1', opNo: 'DEV-OP30', opName: 'DEV 外觀檢查', opRoleFlag: 2, weightRate: 25, creator: 'DEV001', createDate: '2026-11-01T08:37:00', reviser: '', reviseDate: '', dataStamp: 0 },
  { opGroupNo: 'DEV-QC-G1', opNo: 'DEV-QC10', opName: 'DEV 品質檢驗', opRoleFlag: 1, weightRate: 60, creator: 'DEV001', createDate: '2026-11-02T09:10:00', reviser: '', reviseDate: '', dataStamp: 0 },
  { opGroupNo: 'DEV-QC-G1', opNo: 'DEV-QC20', opName: 'DEV 抽驗', opRoleFlag: 2, weightRate: 30, creator: 'DEV001', createDate: '2026-11-02T09:11:00', reviser: '', reviseDate: '', dataStamp: 0 },
]

const rows = ref<GroupRow[]>([])
const members = ref<MemberRow[]>([])
const segments = ref<SegmentOption[]>([])
const operations = ref<OperationOption[]>([])
const activeGroupNo = ref('')
const activeMemberKey = ref('')
const selectedGroupNos = ref<string[]>([])
const selectedMemberNos = ref<string[]>([])
const searchField = ref<'opGroupNo' | 'psNo'>('opGroupNo')
const keyword = ref('')
const pickerKeyword = ref('')
const loading = ref(false)
const detailLoading = ref(false)
const detailsReady = ref(false)
const busy = ref(false)
const lookupLoading = ref(false)
const error = ref('')
const notice = ref('')
const headerMode = ref<EditorMode>('')
const memberMode = ref<EditorMode>('')
const headerDraft = ref(emptyHeaderDraft())
const memberDraft = ref(emptyMemberDraft())
const headerSource = ref<GroupRow | null>(null)
const memberSource = ref<MemberRow | null>(null)
const memberGroupNo = ref('')
const headerStamp = ref<number | null>(0)
const memberStamp = ref<number | null>(0)
const headerSnapshot = ref('')
const memberSnapshot = ref('')
const segmentPickerOpen = ref(false)
const operationPickerOpen = ref(false)
const headerSaving = ref(false)
const memberSaving = ref(false)
const pageElement = ref<HTMLElement | null>(null)
const headerElement = ref<HTMLElement | null>(null)
const memberElement = ref<HTMLElement | null>(null)
const segmentElement = ref<HTMLElement | null>(null)
const operationElement = ref<HTMLElement | null>(null)
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()
const headerDialogOpen = computed(() => headerMode.value !== '')
const memberDialogOpen = computed(() => memberMode.value !== '')
const writing = computed(() => headerSaving.value || memberSaving.value || busy.value)
const blocked = computed(() => loading.value || detailLoading.value || lookupLoading.value || writing.value || headerDialogOpen.value || memberDialogOpen.value || confirmationMessage.value !== null)
let membersSequence = 0
let previousHeaderFocus: HTMLElement | null = null
let previousMemberFocus: HTMLElement | null = null

const visibleGroups = computed(() => rows.value.filter(matchesGroup))
const activeGroup = computed(() => visibleGroups.value.find(row => row.opGroupNo === activeGroupNo.value) ?? null)
const activeMember = computed(() => members.value.find(row => row.opNo === activeMemberKey.value) ?? null)
const groupDeleteTargets = computed(() => visibleGroups.value.filter(row => selectedGroupNos.value.includes(row.opGroupNo)))
const memberDeleteTargets = computed(() => activeGroup.value && detailsReady.value ? members.value.filter(row => selectedMemberNos.value.includes(row.opNo)) : [])
const groupDeleteCount = computed(() => groupDeleteTargets.value.length)
const memberDeleteCount = computed(() => memberDeleteTargets.value.length)
const visibleOperations = computed(() => {
  const text = pickerKeyword.value.trim().toUpperCase()
  const existing = new Set(members.value.filter(row => opKey(row.opGroupNo) === opKey(memberGroupNo.value)).map(row => opKey(row.opNo)))
  return operations.value.filter(row => row.issueState === 2 && !existing.has(opKey(row.opNo)) && (!text || row.opNo.toUpperCase().includes(text) || row.opName.toUpperCase().includes(text)))
})
const roleNames: Record<RoleFlag, string> = { 0: '一般作業站', 1: '進入點', 2: '離開點' }
const headerAudit = computed(() => headerSource.value ?? { creator: '', createDate: '', reviser: '', reviseDate: '' })
const memberAudit = computed(() => memberSource.value ?? { creator: '', createDate: '', reviser: '', reviseDate: '' })

function emptyHeaderDraft() { return { opGroupNo: '', opGroupName: '', psNo: '' as string | null, opGroupOrder: 0 as number | null, description: '' } }
function emptyMemberDraft() { return { opNo: '', opRoleFlag: null as RoleFlag | null, weightRate: 0 as number | string } }
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function userNo(): string { return props.currentUserNo || (mock.value ? 'DEV001' : '') }
function nowText(): string { return new Date().toISOString() }
function displayDate(value: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString('zh-TW', { hour12: false })
}
function roleName(value: number): string { return roleNames[value as RoleFlag] ?? String(value) }
function opKey(value: string): string { return value.replace(/^ +| +$/g, '').toLowerCase() }
function matchesGroup(row: GroupRow): boolean {
  const text = keyword.value.trim().toUpperCase()
  return !text || String(row[searchField.value] ?? '').toUpperCase().includes(text)
}
function nullableText(value: unknown): string | null { return value == null ? null : String(value) }
function normalizeHeader(row: Record<string, unknown>): GroupRow {
  return { opGroupNo: String(row.opGroupNo ?? ''), opGroupName: String(row.opGroupName ?? ''), psNo: nullableText(row.psNo),
    opGroupOrder: row.opGroupOrder == null ? null : Number(row.opGroupOrder), description: nullableText(row.description), erpNo: nullableText(row.erpNo),
    creator: nullableText(row.creator), createDate: nullableText(row.createDate), reviser: nullableText(row.reviser), reviseDate: nullableText(row.reviseDate),
    dataStamp: row.dataStamp == null ? null : Number(row.dataStamp) }
}
function normalizeMember(row: Record<string, unknown>): MemberRow {
  return { opGroupNo: String(row.opGroupNo ?? ''), opNo: String(row.opNo ?? ''), opName: nullableText(row.opName), opRoleFlag: Number(row.opRoleFlag) as RoleFlag,
    weightRate: Number(row.weightRate), creator: nullableText(row.creator), createDate: nullableText(row.createDate), reviser: nullableText(row.reviser), reviseDate: nullableText(row.reviseDate),
    dataStamp: row.dataStamp == null ? null : Number(row.dataStamp) }
}
function apiUrl(path: string): string {
  if (!props.apiBaseUrl.trim()) throw new Error('目前尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(props.apiBaseUrl.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body) headers.set('Content-Type', 'application/json')
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init, headers })
  if (response.status === 401) { window.location.assign(basePath + '/'); throw new Error('登入已逾期，請重新登入。') }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; title?: string } | null
    throw new Error(problem?.detail || problem?.title || 'API 呼叫失敗（' + response.status + '）。')
  }
  return response.status === 204 ? undefined as T : response.json() as Promise<T>
}
function responseRows<T>(result: ApiEnvelope<T>): T[] {
  if (!Array.isArray(result.items)) throw new Error('回傳清單格式不正確。')
  return result.items
}
function groupPath(no: string): string { return 'api/operation-groups/item?' + new URLSearchParams({ opGroupNo: no }) }
function memberPath(group: string, op?: string): string {
  const query = new URLSearchParams({ opGroupNo: group })
  if (op !== undefined) query.set('opNo', op)
  return 'api/operation-groups/members?' + query
}
function readMock(): MockStore {
  try {
    const saved = JSON.parse(localStorage.getItem(storageKey) || 'null') as MockStore | null
    if (saved && Array.isArray(saved.groups) && Array.isArray(saved.members)) return saved
  } catch { /* initialize only isolated Development records */ }
  const initial = { groups: clone(seedGroups), members: clone(seedMembers) }
  persistMock(initial)
  return initial
}
function persistMock(store: MockStore): void { localStorage.setItem(storageKey, JSON.stringify(store)) }
function upsertGroup(row: GroupRow): void {
  rows.value = rows.value.some(current => current.opGroupNo === row.opGroupNo)
    ? rows.value.map(current => current.opGroupNo === row.opGroupNo ? row : current) : [...rows.value, row]
}
function clearMembers(): void {
  membersSequence++
  members.value = []; activeMemberKey.value = ''; selectedMemberNos.value = []; detailsReady.value = false; detailLoading.value = false
}
function clearActiveGroup(): void {
  activeGroupNo.value = ''; selectedGroupNos.value = []; clearMembers()
}
async function loadRows(): Promise<void> {
  if (blocked.value) return
  loading.value = true; error.value = ''; notice.value = ''; selectedGroupNos.value = []; clearMembers()
  try {
    if (mock.value) rows.value = readMock().groups
    else {
      const query = new URLSearchParams()
      if (keyword.value.trim()) query.set(searchField.value, keyword.value.trim())
      rows.value = responseRows(await api<ApiEnvelope<Record<string, unknown>>>('api/operation-groups?' + query)).map(normalizeHeader)
    }
    const selected = visibleGroups.value.find(row => row.opGroupNo === activeGroupNo.value) ?? visibleGroups.value[0]
    activeGroupNo.value = selected?.opGroupNo ?? ''
    selectedGroupNos.value = selected ? [selected.opGroupNo] : []
    if (selected) await loadMembers(selected.opGroupNo)
    notice.value = '主檔 ' + visibleGroups.value.length + ' 筆'
  } catch (cause) { error.value = message(cause, '載入作業站群組失敗。') }
  finally { loading.value = false }
}
async function loadMembers(no: string): Promise<void> {
  const sequence = ++membersSequence
  activeGroupNo.value = no; detailLoading.value = true; detailsReady.value = false
  members.value = []; activeMemberKey.value = ''; selectedMemberNos.value = []
  try {
    let group: GroupRow
    let detail: MemberRow[]
    if (mock.value) {
      const store = readMock()
      const found = store.groups.find(row => row.opGroupNo === no)
      if (!found) throw new Error('群組已不存在，請重新查詢。')
      group = found; detail = store.members.filter(row => row.opGroupNo === no)
    } else {
      const result = await api<MembersResult>(memberPath(no))
      group = normalizeHeader(result.group); detail = result.items.map(normalizeMember)
    }
    if (sequence !== membersSequence || activeGroupNo.value !== no) return
    upsertGroup(group)
    if (!matchesGroup(group)) { clearActiveGroup(); notice.value = '群組已不符合目前查詢條件，請重新查詢。'; return }
    activeGroupNo.value = group.opGroupNo; members.value = detail
    activeMemberKey.value = detail[0]?.opNo ?? ''; selectedMemberNos.value = detail[0] ? [detail[0].opNo] : []; detailsReady.value = true
  } finally { if (sequence === membersSequence) detailLoading.value = false }
}
function applyGroupSelection(nos: string[], preferred: string): void {
  const visible = new Set(visibleGroups.value.map(row => row.opGroupNo))
  selectedGroupNos.value = [...new Set(nos.filter(no => visible.has(no)))]
  const active = selectedGroupNos.value.includes(preferred) ? preferred : selectedGroupNos.value.at(-1) ?? ''
  if (!active) { activeGroupNo.value = ''; clearMembers(); return }
  if (active === activeGroupNo.value && detailsReady.value) return
  error.value = ''
  void loadMembers(active).catch(cause => { error.value = message(cause, '載入群組成員失敗。') })
}
function activateGroup(row: GroupRow, event: MouseEvent): void {
  if (blocked.value) return
  if (event.ctrlKey || event.metaKey) checkedGroup(row.opGroupNo, !selectedGroupNos.value.includes(row.opGroupNo))
  else applyGroupSelection([row.opGroupNo], row.opGroupNo)
}
function checkedGroup(no: string, checked: boolean): void {
  if (blocked.value) return
  const selected = checked ? [...selectedGroupNos.value, no] : selectedGroupNos.value.filter(value => value !== no)
  applyGroupSelection(selected, checked ? no : activeGroupNo.value)
}
function checkedMember(no: string, checked: boolean): void {
  if (blocked.value) return
  selectedMemberNos.value = checked ? [...new Set([...selectedMemberNos.value, no])] : selectedMemberNos.value.filter(value => value !== no)
  activeMemberKey.value = checked ? no : selectedMemberNos.value.includes(activeMemberKey.value) ? activeMemberKey.value : selectedMemberNos.value.at(-1) ?? ''
}
function selectAllGroups(checked: boolean): void { if (!blocked.value) applyGroupSelection(checked ? visibleGroups.value.map(row => row.opGroupNo) : [], activeGroupNo.value) }
function selectAllMembers(checked: boolean): void {
  if (blocked.value) return
  selectedMemberNos.value = checked ? members.value.map(row => row.opNo) : []
  activeMemberKey.value = selectedMemberNos.value.includes(activeMemberKey.value) ? activeMemberKey.value : selectedMemberNos.value.at(-1) ?? ''
}
function activateMember(row: MemberRow, event: MouseEvent): void {
  if (blocked.value) return
  if (event.ctrlKey || event.metaKey) checkedMember(row.opNo, !selectedMemberNos.value.includes(row.opNo))
  else { selectedMemberNos.value = [row.opNo]; activeMemberKey.value = row.opNo }
}
async function openHeader(mode: Exclude<EditorMode, ''>): Promise<void> {
  if (blocked.value) return
  const source = mode === 'new' ? null : activeGroup.value
  if (mode !== 'new' && !source) return
  previousHeaderFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null
  error.value = ''; headerMode.value = mode
  headerSource.value = mode === 'edit' && source ? clone(source) : null
  headerDraft.value = source ? { opGroupNo: mode === 'copy' ? '' : source.opGroupNo, opGroupName: source.opGroupName, psNo: source.psNo,
    opGroupOrder: mode === 'copy' ? source.opGroupOrder ?? 0 : source.opGroupOrder, description: source.description ?? '' } : emptyHeaderDraft()
  headerStamp.value = source ? source.dataStamp : 0
  headerSnapshot.value = JSON.stringify(headerDraft.value)
  await restoreModalFocus()
}
async function openMember(mode: 'new' | 'edit'): Promise<void> {
  if (blocked.value || !detailsReady.value || !activeGroup.value || (mode === 'edit' && !activeMember.value)) return
  previousMemberFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null
  error.value = ''; memberMode.value = mode; memberGroupNo.value = activeGroup.value.opGroupNo
  const source = mode === 'edit' ? activeMember.value : null
  memberSource.value = source ? clone(source) : null
  memberDraft.value = source ? { opNo: source.opNo, opRoleFlag: source.opRoleFlag, weightRate: source.weightRate } : emptyMemberDraft()
  memberStamp.value = source ? source.dataStamp : 0
  memberSnapshot.value = JSON.stringify(memberDraft.value)
  await restoreModalFocus()
}
async function openSegmentPicker(): Promise<void> {
  if (!headerDialogOpen.value || writing.value || lookupLoading.value || confirmationMessage.value !== null) return
  lookupLoading.value = true; error.value = ''
  try {
    segments.value = mock.value ? [{ psNo: '*', psName: '所有生產區段' }, ...clone(seedSegments)]
      : responseRows(await api<ApiEnvelope<SegmentOption>>('api/operation-groups/lookups/production-segments'))
    segmentPickerOpen.value = true
  } catch (cause) { error.value = message(cause, '載入生產區段失敗。') }
  finally { lookupLoading.value = false; await restoreModalFocus() }
}
async function openOperationPicker(): Promise<void> {
  if (!memberDialogOpen.value || memberMode.value === 'edit' || writing.value || lookupLoading.value || confirmationMessage.value !== null) return
  lookupLoading.value = true; error.value = ''; pickerKeyword.value = ''
  try {
    const group = rows.value.find(row => row.opGroupNo === memberGroupNo.value)
    if (!group) throw new Error('群組已不存在，請重新查詢。')
    operations.value = mock.value ? clone(seedOperations).filter(op => op.issueState === 2 && (group.psNo === '*' || op.psNo === group.psNo))
      : responseRows(await api<ApiEnvelope<OperationOption>>('api/operation-groups/lookups/operations?' + new URLSearchParams({ psNo: group.psNo ?? '', issueState: '2' })))
    operationPickerOpen.value = true
  } catch (cause) { error.value = message(cause, '載入已核准作業站失敗。') }
  finally { lookupLoading.value = false; await restoreModalFocus() }
}
async function reloadOperationOptions(): Promise<void> {
  const search = pickerKeyword.value
  await openOperationPicker()
  pickerKeyword.value = search
}
async function chooseSegment(option: SegmentOption): Promise<void> {
  if (lookupLoading.value || writing.value || confirmationMessage.value !== null) return
  headerDraft.value.psNo = option.psNo; segmentPickerOpen.value = false; await restoreModalFocus()
}
async function chooseOperation(option: OperationOption): Promise<void> {
  if (lookupLoading.value || writing.value || confirmationMessage.value !== null) return
  if (members.value.some(row => opKey(row.opGroupNo) === opKey(memberGroupNo.value) && opKey(row.opNo) === opKey(option.opNo))) {
    error.value = '此作業站已加入群組。'; return
  }
  if (option.issueState !== 2) { error.value = '只能選擇已核准的作業站。'; return }
  memberDraft.value.opNo = option.opNo; operationPickerOpen.value = false; await restoreModalFocus()
}
async function closeSegmentPicker(): Promise<void> {
  if (lookupLoading.value || writing.value || confirmationMessage.value !== null) return
  segmentPickerOpen.value = false; await restoreModalFocus()
}
async function closeOperationPicker(): Promise<void> {
  if (lookupLoading.value || writing.value || confirmationMessage.value !== null) return
  operationPickerOpen.value = false; await restoreModalFocus()
}
function ensureNextStamp(value: number | null): number { if (value === maxStamp) throw new Error('版本已達上限，資料未變更。'); return (value ?? 0) + 1 }
function bumpMockGroup(row: GroupRow): GroupRow { return { ...row, dataStamp: ensureNextStamp(row.dataStamp), reviser: userNo(), reviseDate: nowText() } }

async function saveHeader(closeAfter: boolean): Promise<void> {
  if (!headerDialogOpen.value || writing.value || lookupLoading.value || segmentPickerOpen.value || confirmationMessage.value !== null) return
  const draft = headerDraft.value
  const no = headerMode.value === 'edit' ? headerSource.value!.opGroupNo : draft.opGroupNo.trim().toUpperCase()
  const preservingRawPs = headerMode.value === 'edit' && headerSource.value !== null && draft.psNo === headerSource.value.psNo
  const preservingNullOrder = headerMode.value === 'edit' && headerSource.value?.opGroupOrder === null && draft.opGroupOrder === null
  if (!no || !draft.opGroupName.trim() || (!preservingRawPs && !draft.psNo?.trim())) { error.value = '群組編號、名稱及生產區段為必填。'; return }
  if (no.length > 20 || draft.opGroupName.length > 50 || (draft.psNo?.length ?? 0) > 50 || draft.description.length > 255) { error.value = '欄位長度超過允許範圍。'; return }
  if (!preservingNullOrder && (draft.opGroupOrder === null || !Number.isInteger(draft.opGroupOrder) || draft.opGroupOrder < 0 || draft.opGroupOrder > 999999)) { error.value = '群組順序須為 0–999999 整數。'; return }
  headerSaving.value = true; error.value = ''
  try {
    const body = { opGroupNo: no, opGroupName: draft.opGroupName, psNo: draft.psNo, opGroupOrder: draft.opGroupOrder, description: draft.description }
    let saved: GroupRow
    if (mock.value) {
      const store = readMock()
      if (headerMode.value === 'edit') {
        const old = store.groups.find(row => row.opGroupNo === no)
        if (!old || old.dataStamp !== headerStamp.value) throw new Error('資料版本已變更，請重新查詢。')
        if (old.psNo !== body.psNo && body.psNo !== '*' && store.members.filter(row => row.opGroupNo === no).some(member => seedOperations.find(op => op.opNo === member.opNo)?.psNo !== body.psNo)) throw new Error('既有成員不屬於新的生產區段。')
        saved = { ...old, ...body, dataStamp: ensureNextStamp(old.dataStamp), reviser: userNo(), reviseDate: nowText() }
        store.groups = store.groups.map(row => row.opGroupNo === no ? saved : row)
      } else {
        if (store.groups.some(row => row.opGroupNo.trim().toUpperCase() === no)) throw new Error('群組編號已存在。')
        saved = { ...body, description: body.description || null, erpNo: null, creator: userNo(), createDate: nowText(), reviser: null, reviseDate: null, dataStamp: 0 }
        store.groups.push(saved)
      }
      persistMock(store)
    } else saved = normalizeHeader(await api<Record<string, unknown>>(headerMode.value === 'edit' ? groupPath(no) : 'api/operation-groups',
      { method: headerMode.value === 'edit' ? 'PUT' : 'POST', body: JSON.stringify(headerMode.value === 'edit' ? { ...body, dataStamp: headerStamp.value } : body) }))
    const previous = activeGroupNo.value
    upsertGroup(saved); headerSource.value = clone(saved); headerStamp.value = saved.dataStamp
    headerDraft.value = { opGroupNo: saved.opGroupNo, opGroupName: saved.opGroupName, psNo: saved.psNo, opGroupOrder: saved.opGroupOrder, description: saved.description ?? '' }
    headerMode.value = 'edit'; headerSnapshot.value = JSON.stringify(headerDraft.value)
    if (matchesGroup(saved)) {
      activeGroupNo.value = saved.opGroupNo; selectedGroupNos.value = [saved.opGroupNo]
      if (previous !== saved.opGroupNo) { clearMembers(); detailsReady.value = true }
      notice.value = '作業站群組資料已儲存。'
    } else {
      clearActiveGroup()
      notice.value = '資料已儲存；不符合目前查詢條件，請重新查詢。'
    }
    if (closeAfter) headerMode.value = ''
  } catch (cause) { error.value = message(cause, '儲存群組失敗。') }
  finally { headerSaving.value = false; await restoreModalFocus(previousHeaderFocus) }
}
async function saveMember(closeAfter: boolean): Promise<void> {
  if (!memberDialogOpen.value || writing.value || lookupLoading.value || operationPickerOpen.value || confirmationMessage.value !== null) return
  const draft = memberDraft.value
  const no = memberMode.value === 'edit' ? memberSource.value!.opNo : draft.opNo
  const text = String(draft.weightRate).trim()
  const weight = Number(text)
  if (!no || draft.opRoleFlag === null) { error.value = '請選擇作業站及角色。'; return }
  if (!/^\d+(\.\d{1,2})?$/.test(text) || !Number.isFinite(weight) || weight < 0 || weight > 100) { error.value = '權重須為 0–100，最多兩位小數。'; return }
  if (memberMode.value === 'new' && members.value.some(row => opKey(row.opGroupNo) === opKey(memberGroupNo.value) && opKey(row.opNo) === opKey(no))) { error.value = '此作業站已加入群組。'; return }
  memberSaving.value = true; error.value = ''
  try {
    let saved: MemberRow
    let group: GroupRow
    const body = { opRoleFlag: draft.opRoleFlag, weightRate: weight }
    if (mock.value) {
      const store = readMock()
      const parent = store.groups.find(row => row.opGroupNo === memberGroupNo.value)
      if (!parent) throw new Error('群組已不存在。')
      group = bumpMockGroup(parent)
      const operation = seedOperations.find(row => row.opNo === no)
      if (memberMode.value === 'edit') {
        const old = store.members.find(row => row.opGroupNo === memberGroupNo.value && row.opNo === no)
        if (!old || old.dataStamp !== memberStamp.value) throw new Error('成員版本已變更，請重新查詢。')
        saved = { ...old, ...body, dataStamp: ensureNextStamp(old.dataStamp), reviser: userNo(), reviseDate: nowText() }
        store.members = store.members.map(row => row.opGroupNo === memberGroupNo.value && row.opNo === no ? saved : row)
      } else {
        if (!operation || operation.issueState !== 2 || (parent.psNo !== '*' && parent.psNo !== operation.psNo)) throw new Error('只能加入此區段已核准作業站。')
        if (store.members.some(row => row.opGroupNo === memberGroupNo.value && row.opNo === no)) throw new Error('此作業站已加入群組。')
        saved = { opGroupNo: memberGroupNo.value, opNo: no, opName: operation.opName, ...body, creator: userNo(), createDate: nowText(), reviser: null, reviseDate: null, dataStamp: 0 }
        store.members.push(saved)
      }
      store.groups = store.groups.map(row => row.opGroupNo === group.opGroupNo ? group : row); persistMock(store)
    } else {
      const result = await api<MemberSavedResult>(memberPath(memberGroupNo.value, memberMode.value === 'edit' ? no : undefined),
        { method: memberMode.value === 'edit' ? 'PUT' : 'POST', body: JSON.stringify(memberMode.value === 'edit' ? { ...body, dataStamp: memberStamp.value } : { ...body, opNo: no }) })
      saved = normalizeMember(result.item); group = normalizeHeader(result.group)
    }
    upsertGroup(group)
    if (matchesGroup(group)) {
      members.value = [...members.value.filter(row => row.opNo !== saved.opNo), saved]
      activeMemberKey.value = saved.opNo; selectedMemberNos.value = [saved.opNo]
      notice.value = '群組作業站成員已儲存。'
    } else {
      clearActiveGroup()
      notice.value = '成員已儲存；群組已不符合目前查詢條件，請重新查詢。'
    }
    memberSource.value = clone(saved); memberStamp.value = saved.dataStamp
    memberDraft.value = { opNo: saved.opNo, opRoleFlag: saved.opRoleFlag, weightRate: saved.weightRate }
    memberMode.value = 'edit'; memberSnapshot.value = JSON.stringify(memberDraft.value)
    if (closeAfter) memberMode.value = ''
  } catch (cause) { error.value = message(cause, '儲存成員失敗。') }
  finally { memberSaving.value = false; await restoreModalFocus(previousMemberFocus) }
}
async function deleteGroups(): Promise<void> {
  if (blocked.value) return
  const targets = [...groupDeleteTargets.value]
  if (!targets.length || !await requestConfirmation('確定刪除 ' + targets.length + ' 筆群組及其全部成員？')) return
  busy.value = true; error.value = ''; let completed = 0
  try {
    for (const target of targets) {
      if (mock.value) {
        const store = readMock(); const old = store.groups.find(row => row.opGroupNo === target.opGroupNo)
        if (!old || old.dataStamp !== target.dataStamp) throw new Error('群組版本已變更，請重新查詢。')
        store.groups = store.groups.filter(row => row.opGroupNo !== target.opGroupNo)
        store.members = store.members.filter(row => row.opGroupNo !== target.opGroupNo); persistMock(store)
      } else await api(groupPath(target.opGroupNo), { method: 'DELETE', body: JSON.stringify({ dataStamp: target.dataStamp }) })
      completed++; rows.value = rows.value.filter(row => row.opGroupNo !== target.opGroupNo)
      selectedGroupNos.value = selectedGroupNos.value.filter(no => no !== target.opGroupNo)
      if (activeGroupNo.value === target.opGroupNo) { activeGroupNo.value = ''; clearMembers() }
    }
  } catch (cause) { error.value = message(cause, '刪除群組失敗。') }
  finally { notice.value = '已刪除 ' + completed + '／' + targets.length + ' 筆群組。'; busy.value = false }
}
async function deleteMembers(): Promise<void> {
  if (blocked.value || !activeGroup.value || !detailsReady.value) return
  const groupNo = activeGroup.value.opGroupNo
  const targets = [...memberDeleteTargets.value]
  if (!targets.length || !await requestConfirmation('確定刪除 ' + targets.length + ' 筆群組作業站成員？')) return
  busy.value = true; error.value = ''; let completed = 0; let leftFilter = false
  try {
    for (const target of targets) {
      let group: GroupRow
      if (mock.value) {
        const store = readMock(); const old = store.members.find(row => row.opGroupNo === groupNo && row.opNo === target.opNo)
        const parent = store.groups.find(row => row.opGroupNo === groupNo)
        if (!old || old.dataStamp !== target.dataStamp || !parent) throw new Error('成員版本已變更，請重新查詢。')
        group = bumpMockGroup(parent)
        store.members = store.members.filter(row => !(row.opGroupNo === groupNo && row.opNo === target.opNo))
        store.groups = store.groups.map(row => row.opGroupNo === groupNo ? group : row); persistMock(store)
      } else {
        const result = await api<{ group: Record<string, unknown> }>(memberPath(groupNo, target.opNo), { method: 'DELETE', body: JSON.stringify({ dataStamp: target.dataStamp }) })
        group = normalizeHeader(result.group)
      }
      completed++; upsertGroup(group)
      if (!matchesGroup(group)) { clearActiveGroup(); leftFilter = true }
      else {
        members.value = members.value.filter(row => row.opNo !== target.opNo)
        selectedMemberNos.value = selectedMemberNos.value.filter(no => no !== target.opNo)
        if (activeMemberKey.value === target.opNo) activeMemberKey.value = selectedMemberNos.value[0] ?? ''
      }
    }
  } catch (cause) { error.value = message(cause, '刪除成員失敗。') }
  finally { notice.value = '已刪除 ' + completed + '／' + targets.length + ' 筆成員。' + (leftFilter ? ' 群組已不符合目前查詢條件，請重新查詢。' : ''); busy.value = false }
}
function checkGroup(): void {
  if (blocked.value || !activeGroup.value || !activeMember.value || !detailsReady.value) return
  const problems: string[] = []
  if (members.value.filter(row => row.opRoleFlag === 1).length > 1) problems.push('進入點最多一筆')
  if (members.value.filter(row => row.opRoleFlag === 2).length > 1) problems.push('離開點最多一筆')
  const total = members.value.reduce((sum, row) => sum + Math.round(row.weightRate * 100), 0)
  if (total !== 10000) problems.push('權重合計為 ' + (total / 100) + '%，應為 100%')
  error.value = problems.join('；'); notice.value = problems.length ? '' : '檢核成功。'
}
async function closeHeader(): Promise<void> {
  if (writing.value || lookupLoading.value || confirmationMessage.value !== null || segmentPickerOpen.value) return
  if (JSON.stringify(headerDraft.value) !== headerSnapshot.value && !await requestConfirmation('尚有未儲存的修改，確定放棄？')) { await restoreModalFocus(); return }
  headerMode.value = ''
  await restoreModalFocus(previousHeaderFocus)
}
async function closeMember(): Promise<void> {
  if (writing.value || lookupLoading.value || confirmationMessage.value !== null || operationPickerOpen.value) return
  if (JSON.stringify(memberDraft.value) !== memberSnapshot.value && !await requestConfirmation('尚有未儲存的修改，確定放棄？')) { await restoreModalFocus(); return }
  memberMode.value = ''
  await restoreModalFocus(previousMemberFocus)
}
function closePage(): void { if (!blocked.value) emit('close') }
function currentDialog(): HTMLElement | null {
  return operationPickerOpen.value ? operationElement.value : segmentPickerOpen.value ? segmentElement.value
    : memberDialogOpen.value ? memberElement.value : headerDialogOpen.value ? headerElement.value : null
}
function canFocus(element: HTMLElement): boolean { return element.isConnected && element.getClientRects().length > 0 && !element.matches(':disabled') && !element.closest('[inert]') }
function focusFirst(element: HTMLElement | null): void {
  if (!element || !canFocus(element)) return
  const first = Array.from(element.querySelectorAll<HTMLElement>('input:not([readonly]):not([disabled]),select:not([disabled]),textarea:not([disabled]),button:not([disabled]),[tabindex="0"]')).find(canFocus)
  ;(first ?? element).focus()
}
async function restoreModalFocus(previous: HTMLElement | null = null): Promise<void> {
  await nextTick()
  if (confirmationMessage.value !== null) return
  const dialog = currentDialog()
  if (dialog) { if (!writing.value && !lookupLoading.value) focusFirst(dialog); return }
  if (previous && pageElement.value?.contains(previous) && canFocus(previous)) previous.focus()
  else focusFirst(pageElement.value)
}
function keepModalFocus(event: KeyboardEvent): void {
  if (event.key !== 'Tab' || confirmationMessage.value !== null) return
  if (writing.value || lookupLoading.value) { event.preventDefault(); return }
  if (!headerDialogOpen.value && !memberDialogOpen.value) return
  const dialog = currentDialog()
  if (!dialog?.contains(document.activeElement)) { event.preventDefault(); void restoreModalFocus() }
}
function trapTab(event: KeyboardEvent, element: HTMLElement | null): void {
  if (writing.value || lookupLoading.value || confirmationMessage.value !== null) { event.preventDefault(); return }
  const controls = Array.from(element?.querySelectorAll<HTMLElement>('button:not([disabled]),input:not([disabled]),select:not([disabled]),textarea:not([disabled]),[tabindex="0"]') ?? []).filter(canFocus)
  if (!controls.length) { event.preventDefault(); element?.focus(); return }
  const edge = event.shiftKey ? controls[0] : controls[controls.length - 1]
  if (document.activeElement === edge) { event.preventDefault(); (event.shiftKey ? controls[controls.length - 1] : controls[0])?.focus() }
}
function message(cause: unknown, fallback: string): string { return cause instanceof Error ? cause.message : fallback }
watch([keyword, searchField], () => {
  applyGroupSelection(selectedGroupNos.value, activeGroupNo.value)
})
onMounted(() => { document.addEventListener('keydown', keepModalFocus, true); void loadRows() })
onBeforeUnmount(() => { document.removeEventListener('keydown', keepModalFocus, true); membersSequence++ })
</script>

<template>
  <div ref="pageElement" class="operation-group-page" tabindex="-1" :inert="confirmationMessage !== null">
    <div class="page-work" :inert="headerDialogOpen || memberDialogOpen || segmentPickerOpen || operationPickerOpen">
    <div class="query-strip">
      <label class="query-field"><span>查詢欄位</span><select v-model="searchField" :disabled="blocked"><option value="opGroupNo">作業站群組編號</option><option value="psNo">生產區段</option></select></label>
      <input v-model="keyword" :disabled="blocked" class="kd-input query-input" maxlength="50" @keydown.enter="loadRows">
      <button class="kd-button" type="button" :disabled="blocked" @click="loadRows">查詢</button>
      <span v-if="mock" class="dev-badge">DEV 假資料</span>
      <span class="query-hint">查詢欄位：作業站群組編號／生產區段</span>
    </div>
    <fieldset class="win-group master-panel">
      <legend>OP Group Basis</legend>
      <div class="panel-layout">
        <div class="grid-scroll"><table class="kd-grid group-grid">
          <thead><tr>
            <th class="check-cell"><input type="checkbox" :disabled="blocked" :checked="visibleGroups.length > 0 && visibleGroups.every((row) => selectedGroupNos.includes(row.opGroupNo))" aria-label="全選群組" @change="selectAllGroups(($event.target as HTMLInputElement).checked)"></th>
            <th>作業站群組編號</th><th>作業站群組名稱</th><th>生產區段</th><th>群組順序</th><th>說明</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th>
          </tr></thead>
          <tbody>
            <tr v-for="row in visibleGroups" :key="row.opGroupNo" :class="{ active: activeGroupNo === row.opGroupNo }" @click="activateGroup(row, $event)" @dblclick="openHeader('edit')">
              <td class="check-cell" @click.stop><input type="checkbox" :disabled="blocked" :checked="selectedGroupNos.includes(row.opGroupNo)" :aria-label="'選取 ' + row.opGroupNo" @change="checkedGroup(row.opGroupNo, ($event.target as HTMLInputElement).checked)"></td>
              <td>{{ row.opGroupNo }}</td><td>{{ row.opGroupName }}</td><td>{{ row.psNo }}</td><td class="number-cell">{{ row.opGroupOrder }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ displayDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ displayDate(row.reviseDate) }}</td>
            </tr>
            <tr v-if="!loading && visibleGroups.length === 0"><td colspan="10" class="empty-row">沒有符合條件的作業站群組資料</td></tr>
          </tbody>
        </table></div>
        <aside class="vertical-actions">
          <button class="kd-button primary" type="button" :disabled="blocked" @click="openHeader('new')">新增 (A)</button>
          <button class="kd-button" type="button" :disabled="!activeGroup || blocked" @click="openHeader('edit')">修改 (E)</button>
          <button class="kd-button" type="button" :disabled="groupDeleteCount === 0 || blocked" @click="deleteGroups">刪除 (D)</button>
          <button class="kd-button" type="button" :disabled="!activeGroup || blocked" @click="openHeader('copy')">複製 (C)</button>
          <button class="kd-button" type="button" :disabled="!activeGroup || !activeMember || blocked || !detailsReady" @click="checkGroup">檢核</button>
        </aside>
      </div>
      <div class="grid-status">{{ loading ? '載入中…' : '資料總筆數 ' + visibleGroups.length }}<span v-if="activeGroup">　目前群組：{{ activeGroup.opGroupNo }}</span></div>
    </fieldset>
    <fieldset class="win-group detail-panel">
      <legend>OP Group Detail<span v-if="activeGroup"> — {{ activeGroup.opGroupNo }}</span></legend>
      <div class="panel-layout">
        <div class="grid-scroll"><table class="kd-grid detail-grid">
          <thead><tr>
            <th class="check-cell"><input type="checkbox" :disabled="blocked || !detailsReady" :checked="members.length > 0 && members.every((row) => selectedMemberNos.includes(row.opNo))" aria-label="全選明細" @change="selectAllMembers(($event.target as HTMLInputElement).checked)"></th>
            <th>作業站群組編號</th><th>作業站編號</th><th>作業站角色</th><th>權重 (%)</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th>
          </tr></thead>
          <tbody>
            <tr v-for="row in members" :key="row.opNo" :class="{ active: activeMemberKey === row.opNo }" @click="activateMember(row, $event)" @dblclick="openMember('edit')">
              <td class="check-cell" @click.stop><input type="checkbox" :disabled="blocked || !detailsReady" :checked="selectedMemberNos.includes(row.opNo)" :aria-label="'選取 ' + row.opNo" @change="checkedMember(row.opNo, ($event.target as HTMLInputElement).checked)"></td>
              <td>{{ row.opGroupNo }}</td><td>{{ row.opNo }}</td><td>{{ roleName(row.opRoleFlag) }}</td><td class="number-cell">{{ Number(row.weightRate).toFixed(2).replace(/\.?0+$/, '') }}</td><td>{{ row.creator }}</td><td>{{ displayDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ displayDate(row.reviseDate) }}</td>
            </tr>
            <tr v-if="!members.length"><td colspan="9" class="empty-row">{{ activeGroup ? '此群組尚無作業站明細' : '請先選取作業站群組' }}</td></tr>
          </tbody>
        </table></div>
        <aside class="vertical-actions detail-actions">
          <button class="kd-button primary" type="button" :disabled="!activeGroup || blocked || !detailsReady" @click="openMember('new')">新增</button>
          <button class="kd-button" type="button" :disabled="!activeMember || blocked || !detailsReady" @click="openMember('edit')">修改</button>
          <button class="kd-button" type="button" :disabled="memberDeleteCount === 0 || blocked || !detailsReady" @click="deleteMembers">刪除</button>
        </aside>
      </div>
      <div class="grid-status">{{ detailLoading ? '載入成員中…' : '明細筆數 ' + members.length }}<span v-if="members.length">　權重合計 {{ members.reduce((sum, row) => sum + Number(row.weightRate), 0).toFixed(2).replace(/\.?0+$/, '') }}%</span></div>
    </fieldset>
    <div class="page-footer">
      <span v-if="notice" class="notice">{{ notice }}</span><span v-if="error" class="error-message" role="alert">{{ error }}</span>
      <button class="kd-button" type="button" :disabled="blocked" @click="closePage">關閉 (X)</button>
    </div>
    </div>
    <div v-if="headerDialogOpen" class="dialog-backdrop" @click.self="closeHeader">
      <section ref="headerElement" class="win-dialog header-dialog" role="dialog" aria-modal="true" aria-label="作業站群組定義" tabindex="-1" :inert="headerSaving || segmentPickerOpen || lookupLoading" @keydown.esc.stop.prevent="closeHeader" @keydown.tab="trapTab($event, headerElement)">
        <h2>OP Group Basis Define ({{ headerMode === 'new' ? 'ADD' : headerMode === 'edit' ? 'EDIT' : 'Copy' }})</h2>
        <fieldset class="win-group form-group"><legend>OP Group Data</legend>
          <div class="header-form-grid">
            <label>作業站群組編號<input v-model="headerDraft.opGroupNo" class="kd-input yellow-field" maxlength="20" :readonly="headerMode === 'edit'" @blur="headerMode !== 'edit' && (headerDraft.opGroupNo = headerDraft.opGroupNo.toLocaleUpperCase())"></label>
            <label>作業站群組名稱<input v-model="headerDraft.opGroupName" class="kd-input yellow-field" maxlength="50"></label>
            <label>生產區段<span class="picker-field"><input :value="headerDraft.psNo" class="kd-input" readonly><button class="kd-button picker-button" type="button" :disabled="headerSaving || lookupLoading" @click="openSegmentPicker">...</button></span></label>
            <label>群組順序<input v-model.number="headerDraft.opGroupOrder" class="kd-input number-input" type="number" min="0" max="999999" step="1"></label>
            <label class="description-field">說明<textarea v-model="headerDraft.description" class="kd-input" maxlength="255"></textarea></label>
          </div>
        </fieldset>
        <fieldset class="win-group audit-group"><legend>基本資料</legend>
          <div><span>建立人</span><input :value="headerAudit.creator" readonly><span>建立日期</span><input :value="displayDate(headerAudit.createDate)" readonly></div>
          <div><span>修改人</span><input :value="headerAudit.reviser" readonly><span>修改日期</span><input :value="displayDate(headerAudit.reviseDate)" readonly></div>
        </fieldset>
        <div class="dialog-actions"><button class="kd-button primary" type="button" :disabled="headerSaving || lookupLoading" @click="saveHeader(true)">確定</button><button class="kd-button" type="button" :disabled="headerSaving || lookupLoading" @click="closeHeader">關閉</button><button class="kd-button" type="button" :disabled="headerSaving || lookupLoading" @click="saveHeader(false)">套用</button></div>
        <div v-if="error" class="dialog-error">{{ error }}</div>
      </section>
    </div>
    <div v-if="memberDialogOpen" class="dialog-backdrop" @click.self="closeMember">
      <section ref="memberElement" class="win-dialog member-dialog" role="dialog" aria-modal="true" aria-label="群組作業站成員定義" tabindex="-1" :inert="memberSaving || operationPickerOpen || lookupLoading" @keydown.esc.stop.prevent="closeMember" @keydown.tab="trapTab($event, memberElement)">
        <h2>OP Group Detail Define ({{ memberMode === 'new' ? 'ADD' : 'EDIT' }})</h2>
        <fieldset class="win-group form-group"><legend>OP Group Detail</legend>
          <label>作業站群組編號<input :value="memberGroupNo" class="kd-input" readonly></label>
          <label>作業站編號<span class="picker-field"><input v-model="memberDraft.opNo" class="kd-input" readonly><button class="kd-button picker-button" type="button" :disabled="memberMode === 'edit'" @click="openOperationPicker">...</button></span></label>
          <fieldset class="role-group"><legend>作業站角色</legend>
            <label><input v-model.number="memberDraft.opRoleFlag" type="radio" :value="0"> 一般作業站</label>
            <label><input v-model.number="memberDraft.opRoleFlag" type="radio" :value="1"> 進入點</label>
            <label><input v-model.number="memberDraft.opRoleFlag" type="radio" :value="2"> 離開點</label>
          </fieldset>
          <label>權重<span class="weight-field"><input v-model.number="memberDraft.weightRate" class="kd-input yellow-field number-input" type="number" min="0" max="100" step="0.01"><span>%</span></span></label>
        </fieldset>
        <fieldset class="win-group audit-group"><legend>基本資料</legend>
          <div><span>建立人</span><input :value="memberAudit.creator" readonly><span>建立日期</span><input :value="displayDate(memberAudit.createDate)" readonly></div>
          <div><span>修改人</span><input :value="memberAudit.reviser" readonly><span>修改日期</span><input :value="displayDate(memberAudit.reviseDate)" readonly></div>
        </fieldset>
        <div class="dialog-actions"><button class="kd-button primary" type="button" :disabled="memberSaving || lookupLoading" @click="saveMember(true)">確定</button><button class="kd-button" type="button" :disabled="memberSaving || lookupLoading" @click="closeMember">關閉</button><button class="kd-button" type="button" :disabled="memberSaving || lookupLoading" @click="saveMember(false)">套用</button></div>
        <div v-if="error" class="dialog-error">{{ error }}</div>
      </section>
    </div>
    <div v-if="segmentPickerOpen" class="dialog-backdrop picker-backdrop" @click.self="closeSegmentPicker">
      <section ref="segmentElement" class="win-dialog picker-dialog" role="dialog" aria-modal="true" aria-label="選擇生產區段" tabindex="-1" @keydown.esc.stop.prevent="closeSegmentPicker" @keydown.tab="trapTab($event, segmentElement)"><h2>選擇生產區段</h2>
        <table class="kd-grid"><thead><tr><th>區段編號</th><th>區段名稱</th></tr></thead>
          <tbody><tr v-for="option in segments" :key="option.psNo" tabindex="0" @click="chooseSegment(option)" @keydown.enter.prevent="chooseSegment(option)"><td>{{ option.psNo }}</td><td>{{ option.psName }}</td></tr></tbody>
        </table><div class="dialog-actions"><button class="kd-button" type="button" :disabled="lookupLoading || writing" @click="closeSegmentPicker">關閉</button></div>
      </section>
    </div>
    <div v-if="operationPickerOpen" class="dialog-backdrop picker-backdrop" @click.self="closeOperationPicker">
      <section ref="operationElement" class="win-dialog picker-dialog" role="dialog" aria-modal="true" aria-label="選擇已核准作業站" tabindex="-1" :inert="lookupLoading" @keydown.esc.stop.prevent="closeOperationPicker" @keydown.tab="trapTab($event, operationElement)"><h2>選擇已核准作業站</h2>
        <div class="picker-search"><input v-model="pickerKeyword" class="kd-input" placeholder="作業站編號／名稱"><button class="kd-button" type="button" :disabled="lookupLoading" @click="reloadOperationOptions">查詢</button></div>
        <table class="kd-grid"><thead><tr><th>作業站編號</th><th>作業站名稱</th><th>生產區段</th><th>狀態</th></tr></thead>
          <tbody><tr v-for="option in visibleOperations" :key="option.opNo" tabindex="0" @click="chooseOperation(option)" @keydown.enter.prevent="chooseOperation(option)"><td>{{ option.opNo }}</td><td>{{ option.opName }}</td><td>{{ option.psNo }}</td><td>已核准</td></tr></tbody>
        </table><div v-if="!visibleOperations.length" class="empty-row">目前生產區段沒有可選的已核准作業站</div>
        <div class="dialog-actions"><button class="kd-button" type="button" :disabled="lookupLoading || writing" @click="closeOperationPicker">關閉</button></div>
      </section>
    </div>
  </div>
  <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
</template>

<style scoped>
.operation-group-page{box-sizing:border-box;min-height:570px;height:calc(100vh - 116px);padding:8px 10px 10px;color:var(--ink,#26251F);background:#F7F5EE;font:13px "Segoe UI","Microsoft JhengHei",Arial,sans-serif;display:flex;flex-direction:column;gap:6px}
.page-work{display:contents}.picker-backdrop{z-index:1300}.dev-badge{padding:3px 7px;background:#FFF9D8;color:var(--ink,#26251F);border:1px solid var(--line,#E5E1D5)}.query-strip{min-height:39px;display:flex;align-items:center;gap:8px;padding:4px 10px;border:1px solid #c7c7c7;background:#fafafa;flex:0 0 auto}
.query-field{display:flex;align-items:center;gap:7px;white-space:nowrap}.query-field select{width:172px;height:27px;border:1px solid #aaa;background:#fff;color:#333;padding:2px 5px}
.kd-input{box-sizing:border-box;height:28px;border:1px solid #999;border-radius:0;padding:4px 7px;background:#fff;color:#333;font:inherit}.kd-input:focus{outline:1px solid var(--yellow,#E7C600);outline-offset:0}.yellow-field{background:#FFF9D8}.query-input{width:190px}.query-percent{margin-left:-5px}.query-hint{color:#777;margin-left:5px}
.kd-button{min-width:66px;height:30px;border:1px solid #aaa;border-radius:3px;padding:3px 10px;background:linear-gradient(#fff,#ededed);color:#333;font:inherit;white-space:nowrap;cursor:pointer}.kd-button:hover:not(:disabled){background:#FFF9D8;border-color:var(--yellow,#E7C600)}.kd-button:disabled{color:#999;cursor:default}.kd-button.primary{background:#3B3930;border-color:#3B3930;color:#fff;font-weight:600}.kd-button.primary:hover:not(:disabled){background:#26251F}
.win-group{box-sizing:border-box;min-width:0;border:1px solid #b9b9b9;padding:5px 9px 6px;background:#fff}.win-group>legend{padding:0 6px;color:#555}.master-panel{height:255px;flex:0 0 255px}.detail-panel{height:232px;flex:1 1 232px;min-height:180px}
.panel-layout{display:flex;gap:10px;height:calc(100% - 20px);min-height:0}.grid-scroll{flex:1 1 auto;min-width:0;overflow:auto;border:1px solid #bdbdbd}.kd-grid{border-collapse:collapse;width:max-content;min-width:100%;color:#333;font:inherit;background:#fff}.kd-grid th,.kd-grid td{height:27px;box-sizing:border-box;border:1px solid #c9c9c9;padding:4px 7px;text-align:left;white-space:nowrap}.kd-grid th{position:sticky;top:0;z-index:1;background:#e6e6e6;font-weight:600}.kd-grid tbody tr{cursor:pointer}.kd-grid tbody tr:hover{background:#FFFBEB}.kd-grid tbody tr.active{background:#FFF2B2}.kd-grid .check-cell{width:29px;min-width:29px;text-align:center;padding:2px}.kd-grid .check-cell input{margin:0;accent-color:var(--yellow,#E7C600)}.group-grid{min-width:1260px}.detail-grid{min-width:1120px}.number-cell{text-align:right!important}.empty-row{height:44px!important;text-align:center!important;color:#777;cursor:default!important}
.vertical-actions{flex:0 0 88px;display:flex;flex-direction:column;gap:5px}.vertical-actions .kd-button{width:88px;min-width:0;height:32px;padding:2px 3px}.grid-status{height:18px;color:#777;font-size:12px;padding:1px 2px 0}.page-footer{min-height:36px;display:flex;align-items:center;justify-content:flex-end;gap:10px;flex:0 0 auto}.notice{margin-right:auto;color:#666}.error-message{margin-right:auto;color:#b42318}
.dialog-backdrop{position:fixed;inset:0;z-index:1200;display:flex;align-items:center;justify-content:center;padding:18px;background:rgb(0 0 0 / 32%)}.win-dialog{box-sizing:border-box;width:min(566px,calc(100vw - 28px));max-height:calc(100vh - 30px);overflow:auto;padding:12px;border:1px solid #969696;background:#F7F5EE;box-shadow:0 6px 24px rgb(0 0 0 / 22%)}.win-dialog h2{margin:0 0 9px;color:#444;font-size:16px;font-weight:500}.form-group{padding:10px}.header-form-grid{display:grid;grid-template-columns:1fr 1fr;gap:12px 10px;padding:8px 4px 5px}.header-form-grid label,.member-dialog .form-group>label{display:flex;flex-direction:column;gap:4px}.header-form-grid input,.member-dialog .form-group>label>input{width:100%}.picker-field{display:flex;gap:4px}.picker-field>input{flex:1;min-width:0}.picker-button{min-width:30px;width:30px;padding:1px}.number-input{width:110px!important;text-align:right}.description-field{grid-column:1/-1}.description-field textarea{box-sizing:border-box;width:100%;min-height:118px;resize:vertical}
.audit-group{margin-top:8px;padding:7px 9px 9px}.audit-group>div{display:grid;grid-template-columns:54px minmax(80px,1fr) 54px minmax(100px,1fr);align-items:center;gap:5px;margin:3px 0}.audit-group input{box-sizing:border-box;width:100%;height:25px;border:1px solid #ccc;padding:3px 5px;background:#eee;color:#666;font:inherit}.dialog-actions{display:flex;justify-content:flex-end;gap:8px;margin-top:10px}.dialog-actions .primary{min-width:94px}.dialog-error{margin-top:7px;color:#b42318}.member-dialog{width:min(484px,calc(100vw - 28px))}.member-dialog .form-group{display:grid;grid-template-columns:1fr;gap:11px;padding:10px 12px}.role-group{display:flex;flex-wrap:wrap;gap:12px;border:1px solid #bbb;padding:7px 10px 9px}.role-group legend{padding:0 5px}.role-group label{display:inline-flex;align-items:center;gap:4px;white-space:nowrap}.role-group input{accent-color:var(--yellow,#E7C600)}.weight-field{display:flex;align-items:center;gap:7px}
.picker-dialog{width:min(650px,calc(100vw - 28px))}.picker-dialog .kd-grid{width:100%}.picker-dialog .kd-grid tbody tr{cursor:pointer}.picker-dialog .kd-grid tbody tr:hover{background:#FFF2B2}.picker-search{display:flex;gap:7px;margin:7px 0}.picker-search .kd-input{flex:1}
@media(max-width:920px){.operation-group-page{height:auto;min-height:100%;overflow:auto}.query-strip{flex-wrap:wrap}.query-hint{display:none}.master-panel{height:255px}.detail-panel{min-height:232px}}
@media(max-width:620px){.panel-layout{flex-direction:column}.vertical-actions{flex-direction:row;flex-wrap:wrap;flex-basis:auto}.vertical-actions .kd-button{flex:1 1 70px}.master-panel,.detail-panel{height:330px;flex-basis:330px}.header-form-grid{grid-template-columns:1fr}.description-field{grid-column:auto}.audit-group>div{grid-template-columns:48px minmax(55px,1fr)}.audit-group>div span:nth-of-type(2),.audit-group>div input:last-child{display:none}}
</style>
