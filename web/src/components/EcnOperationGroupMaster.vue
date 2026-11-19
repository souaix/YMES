<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'
import { apiErrorMessage } from '../utils/apiError'

type EditorMode = 'new' | 'edit' | 'copy' | ''
type IssueFilter = 0 | 1 | 2 | -1
interface GroupRow {
  opGroupNo: string; opGroupName: string; psNo: string | null; opGroupOrder: number | null; description: string | null
  issueState: number; creator: string | null; createDate: string | null; dataStamp: number | null; reviser: string | null; reviseDate: string | null
}
interface GroupOperation { opGroupNo: string; opNo: string; opName: string | null }
interface SegmentOption { psNo: string; psName: string | null; issueState: number | null }
interface OperationOption { opNo: string; opName: string; psNo: string | null; opType: string | null; opClass: number | null; issueState: number }
interface OperationsResult { group: Record<string, unknown>; items: Record<string, unknown>[] }
interface OperationSaved { group: Record<string, unknown>; item: Record<string, unknown> }
interface HeaderDraft { opGroupNo: string; opGroupName: string; psNo: string; opGroupOrder: string; description: string }
interface MockStore { rows: GroupRow[]; details: GroupOperation[] }
const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const apiRoot = 'api/ecn-operation-groups', storageKey = 'ymes.dev.op-ecn-group-master.v2'
const rows = ref<GroupRow[]>([]), details = ref<GroupOperation[]>([]), detailOwner = ref<GroupRow | null>(null)
const activeGroupNo = ref(''), activeOpNo = ref(''), selectedGroupNos = ref<string[]>([]), selectedOpNos = ref<string[]>([])
const issueFilter = ref<IssueFilter>(0), keyword = ref(''), error = ref(''), notice = ref('')
const loading = ref(false), detailLoading = ref(false), detailsReady = ref(false), busy = ref(false), headerSaving = ref(false), lookupLoading = ref(false)
const headerMode = ref<EditorMode>(''), headerDraft = ref<HeaderDraft>(blankDraft()), headerSource = ref<GroupRow | null>(null)
const headerOriginal = ref(''), headerError = ref(''), headerNotice = ref(''), headerReady = ref(false)
const segmentPickerOpen = ref(false), operationPickerOpen = ref(false), segments = ref<SegmentOption[]>([]), operations = ref<OperationOption[]>([])
const memberOwner = ref<GroupRow | null>(null), pickerKeyword = ref(''), pickerSelection = ref<string[]>([]), pickerError = ref(''), pickerNotice = ref(''), pickerReady = ref(false)
const headerElement = ref<HTMLElement | null>(null), segmentElement = ref<HTMLElement | null>(null), operationElement = ref<HTMLElement | null>(null)
const pageElement = ref<HTMLElement | null>(null)
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()
let listSequence = 0, detailSequence = 0, lookupSequence = 0
let previousModalFocus: HTMLElement | null = null, previousSegmentFocus: HTMLElement | null = null
const writing = computed(() => busy.value || headerSaving.value)
const modalOpen = computed(() => !!headerMode.value || segmentPickerOpen.value || operationPickerOpen.value)
const blocked = computed(() => loading.value || detailLoading.value || writing.value || lookupLoading.value || modalOpen.value || confirmationMessage.value !== null)
const visibleGroups = computed(() => rows.value.filter(row => row.issueState === issueFilter.value && row.opGroupNo.toLowerCase().includes(keyword.value.trim().toLowerCase()))
  .sort((a, b) => (a.opGroupOrder === b.opGroupOrder ? 0 : a.opGroupOrder === null ? 1 : b.opGroupOrder === null ? -1 : a.opGroupOrder - b.opGroupOrder) || a.opGroupNo.localeCompare(b.opGroupNo)))
const activeGroup = computed(() => visibleGroups.value.find(row => row.opGroupNo === activeGroupNo.value) ?? null)
const targets = computed(() => visibleGroups.value.filter(row => selectedGroupNos.value.includes(row.opGroupNo)).length
  ? visibleGroups.value.filter(row => selectedGroupNos.value.includes(row.opGroupNo)) : activeGroup.value ? [activeGroup.value] : [])
const visibleDetails = computed(() => activeGroup.value && detailOwner.value?.opGroupNo === activeGroup.value.opGroupNo ? details.value : [])
const availableOperations = computed(() => {
  const search = pickerKeyword.value.toLowerCase().trim()
  return operations.value.filter(row => row.issueState === 2 && row.psNo === memberOwner.value?.psNo
    && (!search || [row.opNo, row.opName, row.psNo ?? '', row.opType ?? '', operationClassName(row.opClass)].some(value => value.toLowerCase().includes(search))))
})
const headerAudit = computed(() => headerMode.value === 'edit' ? headerSource.value : null)
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function blankDraft(): HeaderDraft { return { opGroupNo: '', opGroupName: '', psNo: '', opGroupOrder: '0', description: '' } }
function fail(value: unknown): string { return value instanceof Error ? value.message : '操作失敗，請重新查詢。' }
function path(resource = '', group?: string, op?: string, extra: Record<string, string> = {}): string {
  const query = new URLSearchParams(extra)
  if (group !== undefined) query.set('opGroupNo', group)
  if (op !== undefined) query.set('opNo', op)
  return apiRoot + (resource ? '/' + resource : '') + (query.size ? '?' + query.toString() : '')
}
async function api<T>(resource: string, init: RequestInit = {}): Promise<T> {
  if (!props.apiBaseUrl.trim()) throw new Error('目前環境尚未設定 API 位址。')
  const url = new URL(resource.replace(/^\/+/, ''), new URL(props.apiBaseUrl.replace(/\/+$/, '') + '/', window.location.origin))
  const headers = new Headers(init.headers); if (init.body) headers.set('Content-Type', 'application/json')
  const response = await fetch(url, { credentials: 'include', ...init, headers })
  if (response.status === 401) { window.location.assign(import.meta.env.BASE_URL); throw new Error('登入狀態已逾期。') }
  if (!response.ok) throw new Error(await apiErrorMessage(response))
  return response.status === 204 ? undefined as T : await response.json() as T
}
function listFrom<T>(value: T[] | { items: T[] }): T[] {
  const list = Array.isArray(value) ? value : value?.items
  if (!Array.isArray(list)) throw new Error('伺服器清單回應不完整。')
  return list
}
function text(row: Record<string, unknown>, key: string): string | null {
  if (!Object.hasOwn(row, key) || (row[key] !== null && typeof row[key] !== 'string')) throw new Error('伺服器文字欄位回應不完整。')
  return row[key] as string | null
}
function number(row: Record<string, unknown>, key: string): number | null {
  if (!Object.hasOwn(row, key) || (row[key] !== null && (typeof row[key] !== 'number' || !Number.isFinite(row[key])))) throw new Error('伺服器版本或數值欄位回應不完整。')
  return row[key] as number | null
}
function normalizeGroup(row: Record<string, unknown>): GroupRow {
  const no = text(row, 'opGroupNo'), name = text(row, 'opGroupName'), state = number(row, 'issueState')
  if (!no || name === null || state === null) throw new Error('ECN 群組回應不完整。')
  return { opGroupNo: no, opGroupName: name, psNo: text(row, 'psNo'), opGroupOrder: number(row, 'opGroupOrder'), description: text(row, 'description'),
    issueState: state, creator: text(row, 'creator'), createDate: text(row, 'createDate'), dataStamp: number(row, 'dataStamp'), reviser: text(row, 'reviser'), reviseDate: text(row, 'reviseDate') }
}
function normalizeDetail(row: Record<string, unknown>): GroupOperation {
  const group = text(row, 'opGroupNo'), op = text(row, 'opNo')
  if (!group || !op) throw new Error('ECN 作業站明細回應不完整。')
  return { opGroupNo: group, opNo: op, opName: text(row, 'opName') }
}
function displayDate(value?: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : new Intl.DateTimeFormat('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23' }).format(date)
}
function issueName(value: number): string { return ({ 0: '未簽核', 1: '簽核中', 2: '已簽核', '-1': '不使用' } as Record<string, string>)[String(value)] ?? String(value) }
function operationClassName(value: number | null): string { return value === null ? '' : ({ 0: '一般作業站', 1: '品質管理', 2: '其他作業站' } as Record<string, string>)[String(value)] ?? String(value) }
const demoSegments: SegmentOption[] = [{ psNo: 'DEV-ASSY', psName: 'DEV 組裝區段', issueState: 2 }, { psNo: 'DEV-QC', psName: 'DEV 品檢區段', issueState: 2 }]
const demoOperations: OperationOption[] = [{ opNo: 'DEV-OP11', opName: 'DEV 上料', psNo: 'DEV-ASSY', opType: 'DEV-GEN', opClass: 0, issueState: 2 },
  { opNo: 'DEV-OP12', opName: 'DEV 組裝', psNo: 'DEV-ASSY', opType: 'DEV-GEN', opClass: 0, issueState: 2 }, { opNo: 'DEV-QC11', opName: 'DEV 檢查', psNo: 'DEV-QC', opType: 'DEV-QC', opClass: 1, issueState: 2 }]
function readMock(): MockStore {
  try { const value = JSON.parse(localStorage.getItem(storageKey) ?? 'null') as MockStore | null; if (value && Array.isArray(value.rows) && Array.isArray(value.details)) return value } catch { /* isolated DEV store */ }
  const groups: GroupRow[] = [0, 1, 2, -1].map((state, index) => ({ opGroupNo: 'DEV-ECN-0' + index, opGroupName: 'DEV ECN 群組 ' + index,
    psNo: index === 2 ? 'DEV-QC' : 'DEV-ASSY', opGroupOrder: index, description: 'DEV 假資料', issueState: state,
    creator: 'DEV001', createDate: '2026-11-18T08:00:00Z', dataStamp: 0, reviser: null, reviseDate: null }))
  const value = { rows: groups, details: [{ opGroupNo: groups[0]!.opGroupNo, opNo: 'DEV-OP11', opName: 'DEV 上料' }] }; saveMock(value); return value
}
function saveMock(value: MockStore): void { localStorage.setItem(storageKey, JSON.stringify(value)) }
function actor(): string { return props.currentUserNo || 'DEV001' }
function bumpMock(row: GroupRow): void { if (row.dataStamp === 99999999999) throw new Error('版本已達上限。'); row.dataStamp = (row.dataStamp ?? 0) + 1; row.reviser = actor(); row.reviseDate = new Date().toISOString() }
function upsert(row: GroupRow): void { const index = rows.value.findIndex(value => value.opGroupNo === row.opGroupNo); if (index < 0) rows.value.push(clone(row)); else rows.value[index] = clone(row) }
function clearDetails(): void { ++detailSequence; detailLoading.value = false; details.value = []; detailOwner.value = null; detailsReady.value = false; activeOpNo.value = ''; selectedOpNos.value = [] }
function adoptMemberGroup(row: GroupRow): void {
  upsert(row)
  if (activeGroupNo.value === row.opGroupNo && row.issueState !== issueFilter.value && [0,1,2,-1].includes(row.issueState)) {
    issueFilter.value = row.issueState as IssueFilter
    notice.value = '群組目前狀態為' + issueName(row.issueState) + '。'
  }
}
function syncSelection(preferred: string): void {
  const visible = new Set(visibleGroups.value.map(row => row.opGroupNo))
  selectedGroupNos.value = selectedGroupNos.value.filter(no => visible.has(no))
  activeGroupNo.value = visible.has(preferred) ? preferred : selectedGroupNos.value[0] ?? visibleGroups.value[0]?.opGroupNo ?? ''
  if (activeGroupNo.value && !selectedGroupNos.value.length) selectedGroupNos.value = [activeGroupNo.value]
  if (!activeGroupNo.value || detailOwner.value?.opGroupNo !== activeGroupNo.value) clearDetails()
}
async function reloadRows(preferred = activeGroupNo.value): Promise<void> {
  const sequence = ++listSequence, search = keyword.value.trim()
  const result = mock.value ? clone(readMock().rows) : listFrom(await api<{ items: Record<string, unknown>[] }>(path('', undefined, undefined, { issueState: 'all', opGroupNo: search }))).map(normalizeGroup)
  if (sequence !== listSequence || search !== keyword.value.trim()) return
  rows.value = result; syncSelection(preferred)
}
async function readDetailSnapshot(no: string): Promise<{ group: GroupRow; items: GroupOperation[] }> {
  if (mock.value) { const store = readMock(), group = store.rows.find(row => row.opGroupNo === no); if (!group) throw new Error('群組已不存在。'); return { group: clone(group), items: clone(store.details.filter(row => row.opGroupNo === no)) } }
  const result = await api<OperationsResult>(path('operations', no)), group = normalizeGroup(result.group)
  if (group.opGroupNo !== no || !Array.isArray(result.items)) throw new Error('群組明細回應不完整。')
  const detail = result.items.map(normalizeDetail)
  if (detail.some(row => row.opGroupNo !== no)) throw new Error('群組明細歸屬不一致。')
  return { group, items: detail }
}
async function reloadDetails(no: string): Promise<void> {
  const sequence = ++detailSequence; detailLoading.value = true; detailsReady.value = false; details.value = []; detailOwner.value = null; selectedOpNos.value = []; activeOpNo.value = ''
  try {
    const result = await readDetailSnapshot(no)
    if (sequence !== detailSequence || activeGroupNo.value !== no) return
    adoptMemberGroup(result.group); detailOwner.value = result.group; details.value = result.items; detailsReady.value = true
  } finally { if (sequence === detailSequence) detailLoading.value = false }
}
async function loadRows(): Promise<void> {
  if (blocked.value) return
  loading.value = true; error.value = ''; notice.value = ''
  try { await reloadRows(); if (activeGroupNo.value) await reloadDetails(activeGroupNo.value) } catch (cause) { error.value = fail(cause) }
  finally { loading.value = false }
}
async function activateGroup(row: GroupRow): Promise<void> {
  if (blocked.value) return
  activeGroupNo.value = row.opGroupNo; selectedGroupNos.value = [row.opGroupNo]; error.value = ''
  try { await reloadDetails(row.opGroupNo) } catch (cause) { error.value = fail(cause) }
}
function changeIssueFilter(value: IssueFilter): void { if (blocked.value) return; issueFilter.value = value; selectedGroupNos.value = []; syncSelection(''); if (activeGroupNo.value) void refreshDetails() }
async function refreshDetails(): Promise<void> { if (blocked.value || !activeGroupNo.value) return; error.value = ''; try { await reloadDetails(activeGroupNo.value) } catch (cause) { error.value = fail(cause) } }
function checkGroup(row: GroupRow, checked: boolean): void {
  if (blocked.value) return
  const visible = new Set(visibleGroups.value.map(value => value.opGroupNo))
  selectedGroupNos.value = (checked ? [...new Set([...selectedGroupNos.value, row.opGroupNo])] : selectedGroupNos.value.filter(no => no !== row.opGroupNo)).filter(no => visible.has(no))
  const preferred = checked ? row.opGroupNo : activeGroupNo.value
  const next = selectedGroupNos.value.includes(preferred) ? preferred : selectedGroupNos.value.at(-1) ?? ''
  if (next === activeGroupNo.value && (next === '' || detailsReady.value)) return
  activeGroupNo.value = next; clearDetails()
  if (next) void reloadDetails(next).catch(cause => { error.value = fail(cause) })
}
watch(keyword, () => {
  const visible = new Set(visibleGroups.value.map(row => row.opGroupNo))
  selectedGroupNos.value = selectedGroupNos.value.filter(no => visible.has(no))
  if (activeGroupNo.value && !visible.has(activeGroupNo.value)) { activeGroupNo.value = ''; clearDetails() }
})
function checkOperation(row: GroupOperation, checked: boolean): void { if (blocked.value) return; selectedOpNos.value = checked ? [...new Set([...selectedOpNos.value, row.opNo])] : selectedOpNos.value.filter(no => no !== row.opNo); activeOpNo.value = checked ? row.opNo : selectedOpNos.value.at(-1) ?? '' }
function activateOperation(row: GroupOperation): void {
  if (blocked.value || !detailsReady.value) return
  activeOpNo.value = row.opNo; selectedOpNos.value = [row.opNo]
}
function draftFrom(row: GroupRow, copy = false): HeaderDraft { return { opGroupNo: copy ? '' : row.opGroupNo, opGroupName: row.opGroupName, psNo: row.psNo ?? '', opGroupOrder: row.opGroupOrder === null ? '' : String(row.opGroupOrder), description: row.description ?? '' } }
async function freshGroup(no: string): Promise<GroupRow> {
  if (!mock.value) return normalizeGroup(await api<Record<string, unknown>>(path('item', no)))
  const row = readMock().rows.find(value => value.opGroupNo === no); if (!row) throw new Error('群組已不存在。'); return clone(row)
}
async function openHeader(mode: Exclude<EditorMode, ''>): Promise<void> {
  if (blocked.value || (mode !== 'new' && !activeGroup.value)) return
  previousModalFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null
  busy.value = true; error.value = ''
  try {
    const source = mode === 'new' ? null : await freshGroup(activeGroup.value!.opGroupNo)
    if (mode === 'edit' && source?.issueState !== 0) throw new Error('只有未簽核群組可以修改。')
    headerMode.value = mode; headerSource.value = source; headerDraft.value = source ? draftFrom(source, mode === 'copy') : blankDraft()
    headerOriginal.value = JSON.stringify(headerDraft.value); headerError.value = ''; headerNotice.value = ''; headerReady.value = true
  } catch (cause) { error.value = fail(cause) } finally { busy.value = false; await restoreFocus() }
}
function validateHeader(): string {
  const draft = headerDraft.value, old = headerSource.value
  const unchangedPs = headerMode.value === 'edit' && old !== null && draft.psNo === (old.psNo ?? '')
  if (!draft.opGroupNo.trim() || draft.opGroupNo.trim().length > 20 || !draft.opGroupName.trim() || draft.opGroupName.length > 50) return '請填寫有效群組編號與名稱。'
  if ((!draft.psNo.trim() && !unchangedPs) || draft.psNo.length > 50) return '請選擇生產區段。'
  if (!draft.opGroupOrder.trim()) { if (!(headerMode.value === 'edit' && old?.opGroupOrder === null)) return '請填寫群組順序。' }
  else if (!Number.isInteger(Number(draft.opGroupOrder)) || Number(draft.opGroupOrder) < 0 || Number(draft.opGroupOrder) > 999999) return '群組順序須為0到999999整數。'
  if (draft.description.length > 255) return '說明最多255字元。'
  return ''
}
async function saveHeader(closeAfter: boolean): Promise<void> {
  if (!headerMode.value || !headerReady.value || writing.value || lookupLoading.value || segmentPickerOpen.value || confirmationMessage.value !== null) return
  headerError.value = validateHeader(); if (headerError.value) return
  const mode = headerMode.value, old = headerSource.value, draft = headerDraft.value
  const payload = { opGroupNo: draft.opGroupNo.trim(), opGroupName: draft.opGroupName, psNo: old && (old.psNo ?? '') === draft.psNo ? old.psNo : draft.psNo.trim(),
    opGroupOrder: draft.opGroupOrder.trim() ? Number(draft.opGroupOrder) : null, description: old && (old.description ?? '') === draft.description ? old.description : draft.description }
  headerSaving.value = true; headerNotice.value = ''
  try {
    let saved: GroupRow
    if (mock.value) {
      const store = readMock()
      if (mode === 'edit') {
        const current = store.rows.find(row => row.opGroupNo === old?.opGroupNo)
        if (!current || current.issueState !== 0 || current.dataStamp !== old?.dataStamp) throw new Error('群組已變更。')
        if (payload.psNo !== current.psNo && store.details.filter(row => row.opGroupNo === current.opGroupNo).some(row => demoOperations.find(op => op.opNo === row.opNo)?.psNo !== payload.psNo)) throw new Error('既有成員不屬於新生產區段。')
        Object.assign(current, payload); bumpMock(current); saved = clone(current)
      } else {
        const no = payload.opGroupNo.toUpperCase()
        if (store.rows.some(row => row.opGroupNo.toUpperCase() === no)) throw new Error('群組編號已存在。')
        saved = { ...payload, opGroupNo: no, issueState: 0, dataStamp: 0, creator: actor(), createDate: new Date().toISOString(), reviser: null, reviseDate: null }
        store.rows.push(saved)
      }
      saveMock(store)
    } else saved = normalizeGroup(await api<Record<string, unknown>>(mode === 'edit' ? path('item', old!.opGroupNo) : apiRoot,
      { method: mode === 'edit' ? 'PUT' : 'POST', body: JSON.stringify({ ...payload, ...(mode === 'edit' ? { dataStamp: old!.dataStamp } : {}) }) }))
    // Adopt the committed server DTO and CAS BEFORE fallible list/detail GETs.
    upsert(saved); issueFilter.value = 0; keyword.value = ''; activeGroupNo.value = saved.opGroupNo; selectedGroupNos.value = [saved.opGroupNo]
    headerSource.value = clone(saved); headerMode.value = 'edit'; headerDraft.value = draftFrom(saved); headerOriginal.value = JSON.stringify(headerDraft.value)
    headerNotice.value = '資料已儲存。'; notice.value = 'ECN 群組已儲存。'; clearDetails()
    if (closeAfter) headerMode.value = ''
    try { await reloadRows(saved.opGroupNo); if (activeGroupNo.value === saved.opGroupNo) await reloadDetails(saved.opGroupNo) }
    catch (cause) { headerNotice.value = '已儲存；清單更新失敗，請重新查詢。' + fail(cause); error.value = headerNotice.value }
  } catch (cause) { headerError.value = fail(cause) + ' 請重新查詢確認資料。' }
  finally { headerSaving.value = false; await restoreFocus() }
}
async function openSegmentPicker(): Promise<void> {
  if (!headerMode.value || writing.value || lookupLoading.value || confirmationMessage.value !== null) return
  previousSegmentFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null
  const sequence = ++lookupSequence; lookupLoading.value = true; headerError.value = ''
  try {
    const result = mock.value ? clone(demoSegments) : listFrom(await api<{ items: SegmentOption[] }>(path('lookups/production-segments')))
    if (sequence !== lookupSequence || !headerMode.value) return
    segments.value = result; segmentPickerOpen.value = true
  } catch (cause) { headerError.value = fail(cause) } finally { lookupLoading.value = false; await restoreFocus() }
}
async function closeSegmentPicker(): Promise<void> { if (writing.value || lookupLoading.value || confirmationMessage.value !== null) return; segmentPickerOpen.value = false; await nextTick(); previousSegmentFocus?.focus() }
function chooseSegment(row: SegmentOption): void { if (writing.value || lookupLoading.value) return; headerDraft.value.psNo = row.psNo; void closeSegmentPicker() }
async function mutateGroups(kind: 'delete' | 'approve'): Promise<void> {
  if (blocked.value || !targets.value.length) return
  const sourceFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null
  const selected = clone(targets.value)
  if (selected.some(row => kind === 'approve' ? row.issueState !== 0 : ![0, 2, -1].includes(row.issueState))) { error.value = '所選群組狀態不允許此操作。'; return }
  busy.value = true; error.value = ''; notice.value = ''; let completed = 0
  try {
    if (!await requestConfirmation(kind === 'approve' ? '確定核准選取的' + selected.length + '筆群組？' : '確定刪除選取的' + selected.length + '筆群組？已核准資料會停用並保留明細。')) return
    for (const row of selected) {
      if (mock.value) {
        const store = readMock(), current = store.rows.find(value => value.opGroupNo === row.opGroupNo)
        if (!current || current.dataStamp !== row.dataStamp) throw new Error('群組已變更。')
        if (kind === 'approve' || current.issueState === 2) { bumpMock(current); current.issueState = kind === 'approve' ? 2 : -1; upsert(current) }
        else { store.rows = store.rows.filter(value => value.opGroupNo !== row.opGroupNo); store.details = store.details.filter(value => value.opGroupNo !== row.opGroupNo); rows.value = rows.value.filter(value => value.opGroupNo !== row.opGroupNo) }
        saveMock(store)
      } else if (kind === 'approve') upsert(normalizeGroup(await api<Record<string, unknown>>(path('approve', row.opGroupNo), { method: 'POST', body: JSON.stringify({ dataStamp: row.dataStamp }) })))
      else {
        const result = await api<{ deleted: boolean; item: Record<string, unknown> | null }>(path('item', row.opGroupNo), { method: 'DELETE', body: JSON.stringify({ dataStamp: row.dataStamp }) })
        if (result.deleted) rows.value = rows.value.filter(value => value.opGroupNo !== row.opGroupNo)
        else if (result.item) upsert(normalizeGroup(result.item)); else throw new Error('刪除回應不完整，請重新查詢。')
      }
      if (activeGroupNo.value === row.opGroupNo) clearDetails()
      completed++
    }
  } catch (cause) { error.value = fail(cause) + ' 請重新查詢確認資料。' }
  finally {
    if (completed || error.value) {
      notice.value = '已完成 ' + completed + '/' + selected.length + ' 筆。'
      try { await reloadRows(); if (activeGroupNo.value) await reloadDetails(activeGroupNo.value) } catch (cause) { error.value += ' 清單更新失敗：' + fail(cause) }
    }
    busy.value = false
    await restorePageFocus(sourceFocus)
  }
}
async function preparePicker(): Promise<void> {
  const no = memberOwner.value?.opGroupNo; if (!no) return
  const sequence = ++lookupSequence; lookupLoading.value = true; pickerReady.value = false; pickerError.value = ''
  try {
    const snapshot = await readDetailSnapshot(no)
    if (snapshot.group.psNo === null) throw new Error('群組未設定生產區段，無法新增作業站。')
    const choices = mock.value ? demoOperations.filter(op => op.issueState === 2 && op.psNo === snapshot.group.psNo && !readMock().details.some(row => row.opNo === op.opNo))
      : listFrom(await api<{ items: OperationOption[] }>(path('lookups/operations', undefined, undefined, { psNo: snapshot.group.psNo, issueState: '2', notInAnyGroup: 'true' })))
    if (sequence !== lookupSequence || !operationPickerOpen.value || memberOwner.value?.opGroupNo !== no) return
    memberOwner.value = snapshot.group; adoptMemberGroup(snapshot.group); detailOwner.value = snapshot.group; details.value = snapshot.items; detailsReady.value = true
    operations.value = choices; pickerSelection.value = pickerSelection.value.filter(op => choices.some(row => row.opNo === op)); pickerReady.value = true
  } catch (cause) { pickerError.value = fail(cause) } finally { if (sequence === lookupSequence) lookupLoading.value = false; await restoreFocus() }
}
async function openOperationPicker(): Promise<void> {
  if (blocked.value || !activeGroup.value || !detailsReady.value || !detailOwner.value) return
  previousModalFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null
  memberOwner.value = clone(detailOwner.value); operationPickerOpen.value = true; pickerKeyword.value = ''; pickerSelection.value = []; pickerNotice.value = ''
  await preparePicker()
}
function pickOperation(no: string, checked: boolean): void { if (writing.value || lookupLoading.value || !pickerReady.value) return; pickerSelection.value = checked ? [...new Set([...pickerSelection.value, no])] : pickerSelection.value.filter(value => value !== no) }
async function addOperations(): Promise<void> {
  if (!operationPickerOpen.value || !pickerReady.value || writing.value || lookupLoading.value || !memberOwner.value || !pickerSelection.value.length || confirmationMessage.value !== null) return
  const selected = [...pickerSelection.value], no = memberOwner.value.opGroupNo
  busy.value = true; pickerError.value = ''; pickerNotice.value = ''; let completed = 0
  try {
    for (const opNo of selected) {
      let group: GroupRow, item: GroupOperation
      if (mock.value) {
        const store = readMock(), current = store.rows.find(row => row.opGroupNo === no), op = demoOperations.find(row => row.opNo === opNo)
        if (!current || current.dataStamp !== memberOwner.value!.dataStamp) throw new Error('群組已變更。')
        if (!op || op.issueState !== 2 || op.psNo !== current.psNo || store.details.some(row => row.opNo === opNo)) throw new Error('作業站須已核准、PS完全相同且尚未分組。')
        bumpMock(current); item = { opGroupNo: no, opNo, opName: op.opName }; store.details.push(item); group = clone(current); saveMock(store)
      } else { const result = await api<OperationSaved>(path('operations', no), { method: 'POST', body: JSON.stringify({ opNo, dataStamp: memberOwner.value!.dataStamp }) }); group = normalizeGroup(result.group); item = normalizeDetail(result.item) }
      if (group.opGroupNo !== no || item.opGroupNo !== no) throw new Error('保存回應歸屬不一致。')
      memberOwner.value = group; adoptMemberGroup(group); detailOwner.value = group; details.value = [...details.value.filter(row => row.opNo !== item.opNo), item]
      pickerSelection.value = pickerSelection.value.filter(value => value !== opNo); operations.value = operations.value.filter(row => row.opNo !== opNo); completed++
    }
    operationPickerOpen.value = false; notice.value = '已加入 ' + completed + ' 筆作業站。'
  } catch (cause) { pickerError.value = fail(cause) + ' 請先重新載入，確認後再操作。'; pickerReady.value = false }
  finally { pickerNotice.value = '已完成 ' + completed + '/' + selected.length + ' 筆。'; busy.value = false; await restoreFocus() }
}
async function deleteOperations(): Promise<void> {
  if (blocked.value || !activeGroup.value || !detailsReady.value || !detailOwner.value || detailOwner.value.opGroupNo !== activeGroup.value.opGroupNo) return
  const sourceFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null
  const selected = visibleDetails.value.filter(row => selectedOpNos.value.length ? selectedOpNos.value.includes(row.opNo) : row.opNo === activeOpNo.value)
  if (!selected.length) return
  const no = detailOwner.value.opGroupNo; let parent = clone(detailOwner.value), completed = 0
  busy.value = true; error.value = ''; notice.value = ''
  try {
    if (!await requestConfirmation('確定刪除選取的' + selected.length + '筆作業站明細？')) return
    for (const row of selected) {
      if (mock.value) {
        const store = readMock(), current = store.rows.find(value => value.opGroupNo === no)
        if (!current || current.dataStamp !== parent.dataStamp) throw new Error('群組已變更。')
        if (!store.details.some(value => value.opGroupNo === no && value.opNo === row.opNo)) throw new Error('明細已不存在。')
        bumpMock(current); store.details = store.details.filter(value => !(value.opGroupNo === no && value.opNo === row.opNo)); parent = clone(current); saveMock(store)
      } else parent = normalizeGroup((await api<{ group: Record<string, unknown> }>(path('operations', no, row.opNo), { method: 'DELETE', body: JSON.stringify({ dataStamp: parent.dataStamp }) })).group)
      if (parent.opGroupNo !== no) throw new Error('保存回應歸屬不一致。')
      adoptMemberGroup(parent); detailOwner.value = parent; details.value = details.value.filter(value => value.opNo !== row.opNo); selectedOpNos.value = selectedOpNos.value.filter(value => value !== row.opNo); completed++
    }
    activeOpNo.value = ''; notice.value = '已刪除 ' + completed + ' 筆明細。'
  } catch (cause) { error.value = fail(cause) + ' 已完成 ' + completed + '/' + selected.length + ' 筆；請重新載入確認。'; detailsReady.value = false }
  finally { busy.value = false; await restorePageFocus(sourceFocus) }
}
async function closeHeader(): Promise<void> {
  if (writing.value || lookupLoading.value || segmentPickerOpen.value || confirmationMessage.value !== null) return
  if (JSON.stringify(headerDraft.value) !== headerOriginal.value && !await requestConfirmation('尚有未儲存的修改，確定放棄？')) { await restoreFocus(); return }
  headerMode.value = ''; headerReady.value = false; ++lookupSequence; await restoreFocus()
}
async function closeOperationPicker(): Promise<void> {
  if (writing.value || lookupLoading.value || confirmationMessage.value !== null) return
  if (pickerSelection.value.length && !await requestConfirmation('尚有未加入的選取項目，確定關閉？')) { await restoreFocus(); return }
  operationPickerOpen.value = false; pickerSelection.value = []; ++lookupSequence; await restoreFocus()
}
function closePage(): void { if (!blocked.value) emit('close') }
function currentModal(): HTMLElement | null { return segmentPickerOpen.value ? segmentElement.value : operationPickerOpen.value ? operationElement.value : headerMode.value ? headerElement.value : null }
async function restorePageFocus(previous: HTMLElement | null): Promise<void> {
  await nextTick()
  if (currentModal() || confirmationMessage.value !== null) return
  if (previous?.isConnected && !previous.matches(':disabled') && !previous.closest('[inert]')) previous.focus()
  else pageElement.value?.querySelector<HTMLElement>('.master-surface input:not(:disabled),.master-surface button:not(:disabled)')?.focus()
}
async function restoreFocus(): Promise<void> {
  await nextTick()
  if (confirmationMessage.value !== null) return
  const modal = currentModal()
  if (modal && !writing.value && !lookupLoading.value) (modal.querySelector<HTMLElement>('input:not(:disabled):not([readonly]),select:not(:disabled),textarea:not(:disabled),button:not(:disabled)') ?? modal).focus()
  else if (!modal) await restorePageFocus(previousModalFocus)
}
function keepModalFocus(event: KeyboardEvent): void {
  if (event.key !== 'Tab' || confirmationMessage.value !== null || !modalOpen.value) return
  if (writing.value || lookupLoading.value) { event.preventDefault(); return }
  if (!currentModal()?.contains(document.activeElement)) { event.preventDefault(); void restoreFocus() }
}
function trapTab(event: KeyboardEvent): void {
  const modal = currentModal(); if (!modal) return
  const controls = Array.from(modal.querySelectorAll<HTMLElement>('input:not(:disabled),select:not(:disabled),textarea:not(:disabled),button:not(:disabled)')).filter(element => element.offsetParent !== null)
  const first = controls[0], last = controls.at(-1)
  if (!first || !last) { event.preventDefault(); modal.focus(); return }
  if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus() }
  else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus() }
}
onMounted(() => { window.addEventListener('keydown', keepModalFocus, true); void loadRows() })
onBeforeUnmount(() => { ++listSequence; ++detailSequence; ++lookupSequence; window.removeEventListener('keydown', keepModalFocus, true) })
</script>

<template>
  <main ref="pageElement" class="ecn-group-page">
    <div class="master-surface" :inert="writing || lookupLoading || modalOpen || confirmationMessage !== null">
      <div class="query-strip">
        <fieldset class="issue-filter"><legend>ECN 作業站群組</legend>
          <label v-for="state in ([0,1,2,-1] as IssueFilter[])" :key="state"><input type="radio" :checked="issueFilter===state" :disabled="blocked" @change="changeIssueFilter(state)">{{ issueName(state) }}</label>
        </fieldset>
        <label class="query-field">群組編號<input v-model="keyword" class="kd-input query-input" maxlength="20" :disabled="blocked" @keydown.enter="loadRows"></label>
        <button class="kd-button" type="button" :disabled="blocked" @click="loadRows">查詢</button><span v-if="mock" class="dev-badge">DEV 假資料</span>
      </div>
      <fieldset class="win-group master-panel"><legend>ECN OP Group Basis</legend><div class="panel-layout">
        <div class="grid-scroll"><table class="kd-grid"><thead><tr><th></th><th>OPGroupNo</th><th>OPGroupName</th><th>PSNo</th><th>OPGroupOrder</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead><tbody>
          <tr v-for="row in visibleGroups" :key="row.opGroupNo" :class="{active:activeGroupNo===row.opGroupNo}" @click="activateGroup(row)">
            <td class="check-cell" @click.stop><input type="checkbox" :checked="selectedGroupNos.includes(row.opGroupNo)" :disabled="blocked" :aria-label="'選取 '+row.opGroupNo" @change="checkGroup(row,($event.target as HTMLInputElement).checked)"></td>
            <td>{{ row.opGroupNo }}</td><td>{{ row.opGroupName }}</td><td>{{ row.psNo }}</td><td>{{ row.opGroupOrder }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ displayDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ displayDate(row.reviseDate) }}</td>
          </tr><tr v-if="!visibleGroups.length"><td colspan="10" class="empty-row">{{ loading?'載入中…':'沒有符合條件的群組' }}</td></tr>
        </tbody></table></div>
        <aside class="vertical-actions"><button class="kd-button primary" type="button" :disabled="blocked" @click="openHeader('new')">新增 (A)</button><button class="kd-button" type="button" :disabled="blocked || activeGroup?.issueState!==0" @click="openHeader('edit')">修改 (E)</button><button class="kd-button" type="button" :disabled="blocked || !targets.length" @click="mutateGroups('delete')">刪除 (D)</button><button class="kd-button" type="button" :disabled="blocked || !activeGroup" @click="openHeader('copy')">複製 (C)</button><button class="kd-button" type="button" :disabled="blocked || !targets.length || targets.some(row=>row.issueState!==0)" @click="mutateGroups('approve')">核准 (Y)</button></aside>
      </div><div class="grid-status">資料總筆數 {{ visibleGroups.length }}</div></fieldset>
      <fieldset class="win-group detail-panel"><legend>ECN OP Group Detail<span v-if="activeGroup"> — {{ activeGroup.opGroupNo }}</span></legend><div class="panel-layout">
        <div class="grid-scroll"><table class="kd-grid detail-grid"><thead><tr><th></th><th>OPNo</th><th>OPName</th></tr></thead><tbody>
          <tr v-for="row in visibleDetails" :key="row.opNo" :class="{active:activeOpNo===row.opNo}" @click="activateOperation(row)">
            <td class="check-cell" @click.stop><input type="checkbox" :checked="selectedOpNos.includes(row.opNo)" :disabled="blocked || !detailsReady" :aria-label="'選取 '+row.opNo" @change="checkOperation(row,($event.target as HTMLInputElement).checked)"></td><td>{{ row.opNo }}</td><td>{{ row.opName }}</td>
          </tr><tr v-if="!visibleDetails.length"><td colspan="3" class="empty-row">{{ detailLoading?'載入中…':activeGroup?'此群組尚未設定作業站':'請先選取上方群組' }}</td></tr>
        </tbody></table></div>
        <aside class="vertical-actions"><button class="kd-button primary" type="button" :disabled="blocked || !activeGroup || !detailsReady" @click="openOperationPicker">新增 (A)</button><button class="kd-button" type="button" :disabled="blocked || !activeGroup || !detailsReady || (!activeOpNo && !selectedOpNos.length)" @click="deleteOperations">刪除 (D)</button><button class="kd-button" type="button" :disabled="blocked || !activeGroup" @click="refreshDetails">重新載入</button></aside>
      </div><div class="grid-status">資料總筆數 {{ visibleDetails.length }}</div></fieldset>
      <p v-if="error" class="message error-message" role="alert">{{ error }}</p><p v-if="notice" class="message notice-message" role="status">{{ notice }}</p>
      <div class="footer-actions"><button class="kd-button" type="button" :disabled="blocked" @click="closePage">關閉</button></div>
    </div>

    <div v-if="headerMode" class="dialog-backdrop" @click.self="closeHeader"><section ref="headerElement" class="win-dialog header-dialog" role="dialog" aria-modal="true" aria-label="ECN 群組主檔" tabindex="-1" :inert="writing || lookupLoading || segmentPickerOpen || confirmationMessage!==null" @keydown.esc.stop.prevent="closeHeader" @keydown.tab="trapTab">
      <header class="dialog-title">ECN OP Group Basis Define（{{ headerMode.toUpperCase() }}）</header>
      <fieldset class="form-group" :disabled="writing || lookupLoading || confirmationMessage!==null"><legend>ECN 作業站群組資料</legend><div class="form-grid">
        <label>群組編號<input v-model="headerDraft.opGroupNo" class="kd-input" maxlength="20" :readonly="headerMode==='edit'" @blur="headerMode!=='edit' && (headerDraft.opGroupNo=headerDraft.opGroupNo.toUpperCase())"></label>
        <label>群組名稱<input v-model="headerDraft.opGroupName" class="kd-input" maxlength="50"></label>
        <label>生產區段<div class="lookup-control"><input v-model="headerDraft.psNo" class="kd-input" readonly><button class="kd-button" type="button" @click="openSegmentPicker">...</button></div></label>
        <label>群組順序<input :value="headerDraft.opGroupOrder" class="kd-input" type="number" min="0" max="999999" step="1" @input="headerDraft.opGroupOrder=($event.target as HTMLInputElement).value"></label>
        <label class="description-label">說明<textarea v-model="headerDraft.description" class="kd-input" maxlength="255" rows="3"></textarea></label>
      </div></fieldset>
      <div class="audit-row"><span>建立人 {{ headerAudit?.creator }}</span><span>建立日期 {{ displayDate(headerAudit?.createDate) }}</span><span>修改人 {{ headerAudit?.reviser }}</span><span>修改日期 {{ displayDate(headerAudit?.reviseDate) }}</span></div>
      <p v-if="headerError" class="message error-message" role="alert">{{ headerError }}</p><p v-if="headerNotice" class="message notice-message">{{ headerNotice }}</p>
      <footer class="dialog-actions"><button class="kd-button primary" type="button" :disabled="writing || lookupLoading || !headerReady" @click="saveHeader(true)">確定</button><button class="kd-button" type="button" :disabled="writing || lookupLoading" @click="closeHeader">關閉</button><button class="kd-button" type="button" :disabled="writing || lookupLoading || !headerReady" @click="saveHeader(false)">套用</button></footer>
    </section></div>
    <div v-if="segmentPickerOpen" class="dialog-backdrop" @click.self="closeSegmentPicker"><section ref="segmentElement" class="win-dialog picker-dialog" role="dialog" aria-modal="true" aria-label="選擇生產區段" tabindex="-1" :inert="writing || lookupLoading || confirmationMessage!==null" @keydown.esc.stop.prevent="closeSegmentPicker" @keydown.tab="trapTab">
      <header class="dialog-title">選擇生產區段</header><div class="picker-scroll"><table class="kd-grid"><thead><tr><th>PSNo</th><th>PSName</th></tr></thead><tbody><tr v-for="row in segments" :key="row.psNo" @click="chooseSegment(row)"><td>{{ row.psNo }}</td><td>{{ row.psName }}</td></tr><tr v-if="!segments.length"><td colspan="2" class="empty-row">沒有可選擇的生產區段</td></tr></tbody></table></div><footer class="dialog-actions"><button class="kd-button" type="button" @click="closeSegmentPicker">關閉</button></footer>
    </section></div>
    <div v-if="operationPickerOpen" class="dialog-backdrop" @click.self="closeOperationPicker"><section ref="operationElement" class="win-dialog picker-dialog" role="dialog" aria-modal="true" aria-label="加入 ECN 作業站" tabindex="-1" :inert="writing || lookupLoading || confirmationMessage!==null" @keydown.esc.stop.prevent="closeOperationPicker" @keydown.tab="trapTab">
      <header class="dialog-title">選擇可加入的作業站 — {{ memberOwner?.psNo }}</header><fieldset class="picker-fields" :disabled="writing || lookupLoading || confirmationMessage!==null">
      <div class="picker-search"><input v-model="pickerKeyword" class="kd-input" placeholder="作業站編號／名稱／類別"><button class="kd-button" type="button" :disabled="!pickerReady" @click="pickerSelection=availableOperations.map(row=>row.opNo)">全選</button><button class="kd-button" type="button" @click="pickerSelection=[]">全不選</button><button class="kd-button" type="button" @click="preparePicker">重新載入</button></div>
      <div class="picker-scroll"><table class="kd-grid"><thead><tr><th></th><th>OPNo</th><th>OPName</th><th>PSNo</th><th>OPType</th><th>OPClass</th></tr></thead><tbody><tr v-for="row in availableOperations" :key="row.opNo"><td><input type="checkbox" :checked="pickerSelection.includes(row.opNo)" :disabled="!pickerReady" @change="pickOperation(row.opNo,($event.target as HTMLInputElement).checked)"></td><td>{{ row.opNo }}</td><td>{{ row.opName }}</td><td>{{ row.psNo }}</td><td>{{ row.opType }}</td><td>{{ operationClassName(row.opClass) }}</td></tr><tr v-if="!availableOperations.length"><td colspan="6" class="empty-row">{{ lookupLoading?'載入中…':'沒有可加入的已核准作業站' }}</td></tr></tbody></table></div></fieldset>
      <p v-if="pickerError" class="message error-message" role="alert">{{ pickerError }}</p><p v-if="pickerNotice" class="message notice-message">{{ pickerNotice }}</p>
      <footer class="dialog-actions"><button class="kd-button primary" type="button" :disabled="writing || lookupLoading || !pickerReady || !pickerSelection.length" @click="addOperations">加入選取項目</button><button class="kd-button" type="button" :disabled="writing || lookupLoading" @click="closeOperationPicker">關閉</button></footer>
    </section></div>
  </main>
  <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
</template>

<style scoped>
.ecn-group-page{box-sizing:border-box;display:flex;flex-direction:column;height:100%;min-height:0;padding:8px 10px;background:#F7F5EE;color:#26251F;font:13px/1.4 "Microsoft JhengHei",Arial,sans-serif}.master-surface{display:flex;flex-direction:column;gap:6px;flex:1;min-height:0}
.query-strip{display:flex;align-items:center;flex-wrap:wrap;gap:8px;min-height:40px;padding:5px 8px;border:1px solid #d4d0c7;background:#fff}.issue-filter{display:flex;align-items:center;gap:10px;padding:0;margin:0;border:0}.issue-filter legend{float:left;margin-right:10px}.issue-filter label,.query-field{display:inline-flex;align-items:center;gap:5px}.query-input{width:180px}.dev-badge{padding:2px 7px;border:1px solid #d8c373;background:#fff5ce;color:#76631b;font-size:11px}
.kd-input{box-sizing:border-box;width:100%;min-width:0;height:29px;padding:4px 7px;border:1px solid #aaa69e;background:#fff1bf;color:#333;font:inherit}.kd-input:focus{outline:2px solid var(--yellow,#E7C600);outline-offset:1px}.kd-input[readonly]{background:#efeeeb}.kd-button{min-height:30px;padding:4px 9px;border:1px solid #bdb9b0;border-radius:3px;background:linear-gradient(#fff,#efeeeb);color:#343331;font:inherit;white-space:nowrap;cursor:pointer}.kd-button:hover:not(:disabled){border-color:var(--yellow,#E7C600);background:#fff5d4}.kd-button:disabled{opacity:.48;cursor:default}.kd-button.primary{background:#3B3930;border-color:#3B3930;color:white}.kd-button.primary:hover:not(:disabled){background:#514d3e}
.win-group{display:flex;flex-direction:column;min-width:0;min-height:0;margin:0;padding:7px;border:1px solid #c8c3b8;background:#fff}.win-group legend{padding:0 5px;color:#59564e}.master-panel{flex:1.2}.detail-panel{flex:1}.panel-layout{display:flex;gap:8px;flex:1;min-height:0}.grid-scroll{flex:1;min-width:0;min-height:0;overflow:auto;border:1px solid #c5c1b9}.kd-grid{width:max-content;min-width:100%;border-collapse:collapse;background:#fff;font-size:12px}.kd-grid th{position:sticky;top:0;z-index:1;background:#EEEAE0;color:#3B3930;text-align:left}.kd-grid td,.kd-grid th{padding:4px 7px;border:1px solid #d4d0c7;white-space:nowrap}.kd-grid tbody tr{cursor:pointer}.kd-grid tbody tr:hover{background:#fff8e2}.kd-grid tr.active{background:#ffedb1}.check-cell{width:26px;text-align:center}.kd-grid input[type=checkbox],.issue-filter input{accent-color:var(--yellow,#E7C600)}.vertical-actions{display:flex;flex-direction:column;gap:6px;flex:0 0 88px}.grid-status{padding:3px 2px 0;color:#777366;font-size:11px}.empty-row{height:44px;text-align:center;color:#777366}
.message{margin:0;padding:6px 8px;border:1px solid}.error-message{border-color:#d99b86;background:#fff0e9;color:#873820}.notice-message{border-color:#d8c79a;background:#fff8dc;color:#65541d}.footer-actions{display:flex;justify-content:flex-end;padding-top:3px}
.dialog-backdrop{position:fixed;inset:0;z-index:1200;display:flex;align-items:center;justify-content:center;padding:14px;background:#17171770}.win-dialog{box-sizing:border-box;display:flex;flex-direction:column;gap:8px;max-height:94vh;padding:12px;border:1px solid #aaa69e;background:#F7F5EE;box-shadow:0 8px 25px #0004}.header-dialog{width:min(700px,96vw);overflow:auto}.dialog-title{padding-bottom:7px;border-bottom:1px solid #d4d0c7;font-size:16px;font-weight:600}.form-group{margin:0;padding:10px;border:1px solid #c9c4b9}.form-grid{display:grid;grid-template-columns:1fr 1fr;gap:10px 16px}.form-grid>label{display:flex;flex-direction:column;gap:4px}.lookup-control{display:flex;gap:5px}.lookup-control .kd-input{flex:1}.description-label{grid-column:1/-1}.description-label textarea{height:72px;resize:vertical}.audit-row{display:grid;grid-template-columns:1fr 1fr;gap:4px 10px;padding:7px;border:1px solid #d0cabf;background:#f1eee6;color:#777366;font-size:11px}.dialog-actions{display:flex;justify-content:flex-end;gap:7px}.picker-dialog{width:min(800px,96vw);min-height:180px}.picker-fields{display:flex;flex-direction:column;min-height:0;gap:8px;margin:0;padding:0;border:0}.picker-search{display:flex;gap:6px}.picker-search .kd-input{flex:1}.picker-scroll{overflow:auto;max-height:55vh;border:1px solid #c5c1b9}
@media(max-width:900px){.query-strip{gap:6px}.issue-filter{flex-wrap:wrap}.form-grid{grid-template-columns:1fr}.description-label{grid-column:auto}.vertical-actions{flex-basis:78px}.picker-search{flex-wrap:wrap}.picker-search .kd-input{flex-basis:100%}}
</style>
