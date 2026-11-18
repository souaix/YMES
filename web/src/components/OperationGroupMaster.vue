<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type EditorMode = 'new' | 'edit' | 'copy' | ''
type RoleFlag = 0 | 1 | 2
interface GroupRow {
  opGroupNo: string; opGroupName: string; psNo: string; opGroupOrder: number; description: string
  creator: string; createDate: string; reviser: string; reviseDate: string; dataStamp: number | null
}
interface MemberRow {
  opGroupNo: string; opNo: string; opName: string; opRoleFlag: RoleFlag; weightRate: number
  creator: string; createDate: string; reviser: string; reviseDate: string; dataStamp: number | null
}
interface SegmentOption { psNo: string; psName: string }
interface OperationOption { opNo: string; opName: string; psNo: string; issueState: number }
interface ApiEnvelope<T> { items?: T[]; rows?: T[]; data?: T[]; message?: string; title?: string }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes.dev.op-group-master.v1'
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
const mockMembers = ref<MemberRow[]>([])
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
const busy = ref(false)
const error = ref('')
const notice = ref('')
const headerMode = ref<EditorMode>('')
const memberMode = ref<EditorMode>('')
const headerDraft = ref(emptyHeaderDraft())
const memberDraft = ref(emptyMemberDraft())
const headerAudit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const memberAudit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const headerStamp = ref<number | null>(0)
const memberStamp = ref<number | null>(0)
const segmentPickerOpen = ref(false)
const operationPickerOpen = ref(false)
const headerSaving = ref(false)
const memberSaving = ref(false)

const activeGroup = computed(() => rows.value.find((row) => row.opGroupNo === activeGroupNo.value) ?? null)
const activeMember = computed(() => members.value.find((row) => row.opNo === activeMemberKey.value) ?? null)
const visibleGroups = computed(() => {
  const text = keyword.value.trim().toLocaleUpperCase()
  if (!text) return rows.value
  return rows.value.filter((row) => String(row[searchField.value] ?? '').toLocaleUpperCase().includes(text))
})
const visibleOperations = computed(() => {
  const text = pickerKeyword.value.trim().toLocaleUpperCase()
  return operations.value.filter((row) => row.issueState === 2
    && (!activeGroup.value?.psNo || activeGroup.value.psNo === '*' || row.psNo === activeGroup.value.psNo)
    && (!text || row.opNo.toLocaleUpperCase().includes(text) || row.opName.toLocaleUpperCase().includes(text)))
})
const headerDialogOpen = computed(() => headerMode.value !== '')
const memberDialogOpen = computed(() => memberMode.value !== '')
const roleNames: Record<RoleFlag, string> = { 0: '一般作業站', 1: '進入點', 2: '離開點' }

function emptyHeaderDraft() { return { opGroupNo: '', opGroupName: '', psNo: '', opGroupOrder: 0, description: '' } }
function emptyMemberDraft() { return { opNo: '', opRoleFlag: null as RoleFlag | null, weightRate: 0 } }
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function nowText(): string { return new Date().toISOString() }
function userNo(): string { return props.currentUserNo || (mock.value ? 'DEV001' : '') }
function displayDate(value: string): string {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return new Intl.DateTimeFormat('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23' }).format(date).replaceAll('-', '/')
}
function roleName(value: number): string { return roleNames[value as RoleFlag] ?? String(value) }
function normalizeHeader(row: Record<string, unknown>): GroupRow {
  return {
    opGroupNo: String(row.opGroupNo ?? row.opgroupno ?? ''), opGroupName: String(row.opGroupName ?? row.opgroupname ?? ''),
    psNo: String(row.psNo ?? row.psno ?? ''), opGroupOrder: Number(row.opGroupOrder ?? row.opgrouporder ?? 0),
    description: String(row.description ?? ''), creator: String(row.creator ?? ''), createDate: String(row.createDate ?? row.createdate ?? ''),
    reviser: String(row.reviser ?? ''), reviseDate: String(row.reviseDate ?? row.revisedate ?? ''),
    dataStamp: row.dataStamp == null && row.datastamp == null ? null : Number(row.dataStamp ?? row.datastamp),
  }
}
function normalizeMember(row: Record<string, unknown>): MemberRow {
  return {
    opGroupNo: String(row.opGroupNo ?? row.opgroupno ?? ''), opNo: String(row.opNo ?? row.opno ?? ''),
    opName: String(row.opName ?? row.opname ?? ''), opRoleFlag: Number(row.opRoleFlag ?? row.oproleflag ?? 0) as RoleFlag,
    weightRate: Number(row.weightRate ?? row.weightrate ?? 0), creator: String(row.creator ?? ''),
    createDate: String(row.createDate ?? row.createdate ?? ''), reviser: String(row.reviser ?? ''),
    reviseDate: String(row.reviseDate ?? row.revisedate ?? ''),
    dataStamp: row.dataStamp == null && row.datastamp == null ? null : Number(row.dataStamp ?? row.datastamp),
  }
}
function apiUrl(path: string): string {
  if (!props.apiBaseUrl.trim()) throw new Error('目前尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(props.apiBaseUrl.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init, headers: { 'Content-Type': 'application/json', ...init.headers } })
  const raw = await response.text()
  let body: unknown = null
  try { body = raw ? JSON.parse(raw) : null } catch { body = raw }
  if (!response.ok) {
    const envelope = body as ApiEnvelope<unknown> | null
    throw new Error(envelope && typeof envelope === 'object' ? envelope.message ?? envelope.title ?? 'API 回應 ' + response.status : String(body || 'API 回應 ' + response.status))
  }
  return body as T
}
function responseRows<T>(result: T[] | ApiEnvelope<T>): T[] { return Array.isArray(result) ? result : result.items ?? result.rows ?? result.data ?? [] }
function persistMock(): void { localStorage.setItem(storageKey, JSON.stringify({ groups: rows.value, members: mockMembers.value })) }
function loadMock(): void {
  try {
    const saved = localStorage.getItem(storageKey)
    if (!saved) {
      rows.value = clone(seedGroups)
      mockMembers.value = clone(seedMembers)
      persistMock()
      return
    }
    const parsed = JSON.parse(saved) as { groups?: GroupRow[]; members?: MemberRow[] }
    rows.value = Array.isArray(parsed.groups) ? parsed.groups : clone(seedGroups)
    mockMembers.value = Array.isArray(parsed.members) ? parsed.members : clone(seedMembers)
  } catch {
    rows.value = clone(seedGroups)
    mockMembers.value = clone(seedMembers)
  }
}
async function loadRows(): Promise<void> {
  loading.value = true; error.value = ''; notice.value = ''; selectedGroupNos.value = []
  try {
    if (mock.value) loadMock()
    else {
      const query = new URLSearchParams()
      if (keyword.value.trim()) query.set(searchField.value, keyword.value.trim())
      const queryString = query.toString()
      const result = await api<GroupRow[] | ApiEnvelope<GroupRow>>('/api/operation-groups' + (queryString ? '?' + queryString : ''))
      rows.value = responseRows(result).map((row) => normalizeHeader(row as unknown as Record<string, unknown>))
    }
    if (!visibleGroups.value.some((row) => row.opGroupNo === activeGroupNo.value)) activeGroupNo.value = visibleGroups.value[0]?.opGroupNo ?? ''
    if (activeGroupNo.value) await loadMembers(activeGroupNo.value)
    else { members.value = []; activeMemberKey.value = ''; selectedMemberNos.value = [] }
    notice.value = '主檔 ' + visibleGroups.value.length + ' 筆'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入作業站群組資料失敗。' }
  finally { loading.value = false }
}
async function loadMembers(opGroupNo: string): Promise<void> {
  activeGroupNo.value = opGroupNo; selectedMemberNos.value = []; activeMemberKey.value = ''
  if (!opGroupNo) { members.value = []; return }
  if (mock.value) {
    members.value = clone(mockMembers.value.filter((row) => row.opGroupNo === opGroupNo))
    return
  }
  const result = await api<MemberRow[] | ApiEnvelope<MemberRow>>('/api/operation-groups/' + encodeURIComponent(opGroupNo) + '/members')
  members.value = responseRows(result).map((row) => normalizeMember(row as unknown as Record<string, unknown>))
}
function activateGroup(row: GroupRow): void {
  if (activeGroupNo.value === row.opGroupNo) return
  void loadMembers(row.opGroupNo).catch((cause) => { error.value = cause instanceof Error ? cause.message : '載入群組作業站明細失敗。' })
}
async function loadSegments(): Promise<void> {
  if (mock.value) { segments.value = clone(seedSegments); return }
  const result = await api<SegmentOption[] | ApiEnvelope<SegmentOption>>('/api/operation-groups/lookups/production-segments')
  segments.value = responseRows(result).map((row) => {
    const item = row as unknown as Record<string, unknown>
    return { psNo: String(item.psNo ?? item.psno ?? ''), psName: String(item.psName ?? item.psname ?? '') }
  })
}
async function loadOperations(): Promise<void> {
  if (mock.value) { operations.value = clone(seedOperations); return }
  const query = new URLSearchParams({ issueState: '2' })
  if (activeGroup.value?.psNo && activeGroup.value.psNo !== '*') query.set('psNo', activeGroup.value.psNo)
  const result = await api<OperationOption[] | ApiEnvelope<OperationOption>>('/api/operation-groups/lookups/operations?' + query.toString())
  operations.value = responseRows(result).map((row) => {
    const item = row as unknown as Record<string, unknown>
    return { opNo: String(item.opNo ?? item.opno ?? ''), opName: String(item.opName ?? item.opname ?? ''), psNo: String(item.psNo ?? item.psno ?? ''), issueState: Number(item.issueState ?? item.issuestate ?? 0) }
  })
}
function checkedGroup(opGroupNo: string, checked: boolean): void {
  selectedGroupNos.value = checked ? [...new Set([...selectedGroupNos.value, opGroupNo])] : selectedGroupNos.value.filter((value) => value !== opGroupNo)
}
function checkedMember(opNo: string, checked: boolean): void {
  selectedMemberNos.value = checked ? [...new Set([...selectedMemberNos.value, opNo])] : selectedMemberNos.value.filter((value) => value !== opNo)
}
function selectAllGroups(checked: boolean): void { selectedGroupNos.value = checked ? visibleGroups.value.map((row) => row.opGroupNo) : [] }
function selectAllMembers(checked: boolean): void { selectedMemberNos.value = checked ? members.value.map((row) => row.opNo) : [] }
async function openHeader(mode: Exclude<EditorMode, ''>): Promise<void> {
  error.value = ''
  try { await loadSegments() } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入生產區段選項失敗。'; return }
  const source = mode === 'new' ? null : activeGroup.value
  headerMode.value = mode
  headerDraft.value = source
    ? { opGroupNo: mode === 'copy' ? '' : source.opGroupNo, opGroupName: source.opGroupName, psNo: source.psNo, opGroupOrder: source.opGroupOrder, description: source.description }
    : emptyHeaderDraft()
  headerStamp.value = source?.dataStamp ?? 0
  headerAudit.value = { creator: mode === 'edit' ? source?.creator ?? '' : userNo(), createDate: mode === 'edit' ? source?.createDate ?? '' : nowText(), reviser: mode === 'edit' ? userNo() : '', reviseDate: mode === 'edit' ? nowText() : '' }
}
async function saveHeader(closeAfter: boolean): Promise<void> {
  const draft = headerDraft.value
  const opGroupNo = draft.opGroupNo.trim().toLocaleUpperCase()
  const opGroupName = draft.opGroupName.trim()
  const psNo = draft.psNo.trim()
  if (!opGroupNo || !opGroupName || !psNo) { error.value = '作業站群組編號、名稱及生產區段為必填欄位。'; return }
  if (opGroupNo.length > 20 || opGroupName.length > 50 || draft.description.length > 255) { error.value = '欄位長度超過資料庫限制。'; return }
  if (draft.opGroupOrder < 0 || draft.opGroupOrder > 999999 || !Number.isInteger(Number(draft.opGroupOrder))) { error.value = '群組順序須為 0 至 999999 的整數。'; return }
  if (headerMode.value !== 'edit' && rows.value.some((row) => row.opGroupNo.toLocaleUpperCase() === opGroupNo)) { error.value = '作業站群組編號已存在。'; return }
  headerSaving.value = true; error.value = ''
  try {
    const body = { opGroupNo, opGroupName, psNo, opGroupOrder: Number(draft.opGroupOrder), description: draft.description }
    if (mock.value) {
      if (headerMode.value === 'edit') rows.value = rows.value.map((row) => row.opGroupNo === activeGroupNo.value ? { ...row, ...body, reviser: userNo(), reviseDate: nowText(), dataStamp: (row.dataStamp ?? 0) + 1 } : row)
      else rows.value = [{ ...body, creator: userNo(), createDate: nowText(), reviser: '', reviseDate: '', dataStamp: 0 }, ...rows.value]
      persistMock()
    } else if (headerMode.value === 'edit') {
      await api('/api/operation-groups/' + encodeURIComponent(activeGroupNo.value), { method: 'PUT', body: JSON.stringify({ ...body, dataStamp: headerStamp.value }) })
    } else await api('/api/operation-groups', { method: 'POST', body: JSON.stringify(body) })
    activeGroupNo.value = opGroupNo
    await loadRows()
    if (closeAfter) headerMode.value = ''
    else if (headerMode.value !== 'edit') headerDraft.value = emptyHeaderDraft()
    notice.value = '作業站群組資料已儲存。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '儲存作業站群組資料失敗。' }
  finally { headerSaving.value = false }
}
async function deleteGroups(): Promise<void> {
  const targets = selectedGroupNos.value.length ? selectedGroupNos.value : activeGroupNo.value ? [activeGroupNo.value] : []
  if (!targets.length || !window.confirm('確定刪除選取的作業站群組？刪除群組時會一併刪除其所屬作業站明細。')) return
  busy.value = true; error.value = ''
  try {
    if (mock.value) {
      rows.value = rows.value.filter((row) => !targets.includes(row.opGroupNo))
      mockMembers.value = mockMembers.value.filter((row) => !targets.includes(row.opGroupNo))
      persistMock()
    } else for (const opGroupNo of targets) await api('/api/operation-groups/' + encodeURIComponent(opGroupNo), { method: 'DELETE' })
    if (targets.includes(activeGroupNo.value)) activeGroupNo.value = ''
    selectedGroupNos.value = []
    await loadRows()
    notice.value = '已刪除 ' + targets.length + ' 筆作業站群組。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '刪除作業站群組失敗。' }
  finally { busy.value = false }
}
async function openMember(mode: 'new' | 'edit'): Promise<void> {
  if (!activeGroup.value || (mode === 'edit' && !activeMember.value)) return
  error.value = ''
  try { await loadOperations() } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入已核准作業站失敗。'; return }
  memberMode.value = mode
  const source = mode === 'edit' ? activeMember.value : null
  memberDraft.value = source ? { opNo: source.opNo, opRoleFlag: source.opRoleFlag, weightRate: source.weightRate } : emptyMemberDraft()
  memberStamp.value = source?.dataStamp ?? 0
  memberAudit.value = { creator: mode === 'edit' ? source?.creator ?? '' : userNo(), createDate: mode === 'edit' ? source?.createDate ?? '' : nowText(), reviser: mode === 'edit' ? userNo() : '', reviseDate: mode === 'edit' ? nowText() : '' }
}
async function saveMember(closeAfter: boolean): Promise<void> {
  if (!activeGroup.value) return
  const draft = memberDraft.value
  const opNo = draft.opNo.trim().toLocaleUpperCase()
  const weightRate = Number(draft.weightRate)
  if (!opNo) { error.value = '作業站編號為必填欄位。'; return }
  if (draft.opRoleFlag === null) { error.value = '請選擇作業站角色。'; return }
  if (!Number.isFinite(weightRate) || weightRate < 0 || weightRate > 100 || Math.round(weightRate * 100) !== weightRate * 100) { error.value = '權重須為 0 至 100，最多兩位小數。'; return }
  const operation = operations.value.find((row) => row.opNo === opNo)
  if (memberMode.value === 'new' && members.value.some((row) => row.opNo === opNo)) { error.value = '此作業站已加入目前群組。'; return }
  if (memberMode.value === 'new' && (!operation || (activeGroup.value.psNo !== '*' && operation.psNo !== activeGroup.value.psNo))) { error.value = '只能選擇目前生產區段中已核准的作業站。'; return }
  memberSaving.value = true; error.value = ''
  try {
    const body = { opGroupNo: activeGroup.value.opGroupNo, opNo, opRoleFlag: draft.opRoleFlag, weightRate }
    if (mock.value) {
      if (memberMode.value === 'edit') mockMembers.value = mockMembers.value.map((row) => row.opGroupNo === body.opGroupNo && row.opNo === activeMemberKey.value ? { ...row, opRoleFlag: draft.opRoleFlag as RoleFlag, weightRate, reviser: userNo(), reviseDate: nowText(), dataStamp: (row.dataStamp ?? 0) + 1 } : row)
      else mockMembers.value.push({ ...body, opRoleFlag: draft.opRoleFlag as RoleFlag, opName: operation?.opName ?? opNo, creator: userNo(), createDate: nowText(), reviser: '', reviseDate: '', dataStamp: 0 })
      persistMock()
    } else if (memberMode.value === 'edit') {
      await api('/api/operation-groups/' + encodeURIComponent(activeGroup.value.opGroupNo) + '/members/' + encodeURIComponent(opNo), { method: 'PUT', body: JSON.stringify({ opRoleFlag: draft.opRoleFlag, weightRate, dataStamp: memberStamp.value }) })
    } else await api('/api/operation-groups/' + encodeURIComponent(activeGroup.value.opGroupNo) + '/members', { method: 'POST', body: JSON.stringify(body) })
    const groupNo = activeGroup.value.opGroupNo
    activeMemberKey.value = opNo
    await loadMembers(groupNo)
    activeMemberKey.value = opNo
    if (closeAfter) memberMode.value = ''
    notice.value = '群組作業站明細已儲存。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '儲存群組作業站明細失敗。' }
  finally { memberSaving.value = false }
}
async function deleteMembers(): Promise<void> {
  const targets = selectedMemberNos.value.length ? selectedMemberNos.value : activeMemberKey.value ? [activeMemberKey.value] : []
  if (!targets.length || !activeGroup.value || !window.confirm('確定刪除選取的群組作業站明細？')) return
  busy.value = true; error.value = ''
  try {
    const groupNo = activeGroup.value.opGroupNo
    if (mock.value) { mockMembers.value = mockMembers.value.filter((row) => !(row.opGroupNo === groupNo && targets.includes(row.opNo))); persistMock() }
    else for (const opNo of targets) await api('/api/operation-groups/' + encodeURIComponent(groupNo) + '/members/' + encodeURIComponent(opNo), { method: 'DELETE' })
    selectedMemberNos.value = []; activeMemberKey.value = ''
    await loadMembers(groupNo)
    notice.value = '已刪除 ' + targets.length + ' 筆群組作業站明細。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '刪除群組作業站明細失敗。' }
  finally { busy.value = false }
}
function checkGroup(): void {
  if (!activeGroup.value || !activeMember.value) { window.alert('請先選取作業站群組及其中一筆作業站明細。'); return }
  const errors: string[] = []
  if (members.value.filter((row) => row.opRoleFlag === 1).length > 1) errors.push('同一群組的進入點最多只能有一筆。')
  if (members.value.filter((row) => row.opRoleFlag === 2).length > 1) errors.push('同一群組的離開點最多只能有一筆。')
  const total = members.value.reduce((sum, row) => sum + Math.round(Number(row.weightRate) * 100), 0) / 100
  if (Math.abs(total - 100) > 0.000001) errors.push('權重總和須等於 100%；目前合計 ' + total + '%。')
  window.alert(errors.length ? errors.join('\n') : '檢核成功。')
}
function chooseSegment(option: SegmentOption): void { headerDraft.value.psNo = option.psNo; segmentPickerOpen.value = false }
function chooseOperation(option: OperationOption): void { memberDraft.value.opNo = option.opNo; operationPickerOpen.value = false; pickerKeyword.value = '' }
function openOperationPicker(): void { pickerKeyword.value = ''; operationPickerOpen.value = true }
onMounted(() => { void loadRows() })
</script>

<template>
  <div class="operation-group-page">
    <div class="query-strip">
      <label class="query-field"><span>查詢欄位</span><select v-model="searchField"><option value="opGroupNo">作業站群組編號</option><option value="psNo">生產區段</option></select></label>
      <input v-model="keyword" class="kd-input query-input" maxlength="50" @keydown.enter="loadRows">
      <span class="query-percent">%</span><button class="kd-button" type="button" :disabled="loading" @click="loadRows">查詢</button>
      <span class="query-hint">查詢欄位：作業站群組編號／生產區段</span>
    </div>
    <fieldset class="win-group master-panel">
      <legend>OP Group Basis</legend>
      <div class="panel-layout">
        <div class="grid-scroll"><table class="kd-grid group-grid">
          <thead><tr>
            <th class="check-cell"><input type="checkbox" :checked="visibleGroups.length > 0 && visibleGroups.every((row) => selectedGroupNos.includes(row.opGroupNo))" aria-label="全選群組" @change="selectAllGroups(($event.target as HTMLInputElement).checked)"></th>
            <th>作業站群組編號</th><th>作業站群組名稱</th><th>生產區段</th><th>群組順序</th><th>說明</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th>
          </tr></thead>
          <tbody>
            <tr v-for="row in visibleGroups" :key="row.opGroupNo" :class="{ active: activeGroupNo === row.opGroupNo }" @click="activateGroup(row)" @dblclick="openHeader('edit')">
              <td class="check-cell" @click.stop><input type="checkbox" :checked="selectedGroupNos.includes(row.opGroupNo)" :aria-label="'選取 ' + row.opGroupNo" @change="checkedGroup(row.opGroupNo, ($event.target as HTMLInputElement).checked)"></td>
              <td>{{ row.opGroupNo }}</td><td>{{ row.opGroupName }}</td><td>{{ row.psNo }}</td><td class="number-cell">{{ row.opGroupOrder }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ displayDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ displayDate(row.reviseDate) }}</td>
            </tr>
            <tr v-if="!loading && visibleGroups.length === 0"><td colspan="10" class="empty-row">沒有符合條件的作業站群組資料</td></tr>
          </tbody>
        </table></div>
        <aside class="vertical-actions">
          <button class="kd-button primary" type="button" :disabled="busy || loading" @click="openHeader('new')">新增 (A)</button>
          <button class="kd-button" type="button" :disabled="!activeGroup || busy" @click="openHeader('edit')">修改 (E)</button>
          <button class="kd-button" type="button" :disabled="!activeGroup || busy" @click="deleteGroups">刪除 (D)</button>
          <button class="kd-button" type="button" :disabled="!activeGroup || busy" @click="openHeader('copy')">複製 (C)</button>
          <button class="kd-button" type="button" :disabled="!activeGroup || !activeMember || busy" @click="checkGroup">檢核</button>
        </aside>
      </div>
      <div class="grid-status">{{ loading ? '載入中…' : '資料總筆數 ' + visibleGroups.length }}<span v-if="activeGroup">　目前群組：{{ activeGroup.opGroupNo }}</span></div>
    </fieldset>
    <fieldset class="win-group detail-panel">
      <legend>OP Group Detail<span v-if="activeGroup"> — {{ activeGroup.opGroupNo }}</span></legend>
      <div class="panel-layout">
        <div class="grid-scroll"><table class="kd-grid detail-grid">
          <thead><tr>
            <th class="check-cell"><input type="checkbox" :checked="members.length > 0 && members.every((row) => selectedMemberNos.includes(row.opNo))" aria-label="全選明細" @change="selectAllMembers(($event.target as HTMLInputElement).checked)"></th>
            <th>作業站群組編號</th><th>作業站編號</th><th>作業站角色</th><th>權重 (%)</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th>
          </tr></thead>
          <tbody>
            <tr v-for="row in members" :key="row.opNo" :class="{ active: activeMemberKey === row.opNo }" @click="activeMemberKey = row.opNo" @dblclick="openMember('edit')">
              <td class="check-cell" @click.stop><input type="checkbox" :checked="selectedMemberNos.includes(row.opNo)" :aria-label="'選取 ' + row.opNo" @change="checkedMember(row.opNo, ($event.target as HTMLInputElement).checked)"></td>
              <td>{{ row.opGroupNo }}</td><td>{{ row.opNo }}</td><td>{{ roleName(row.opRoleFlag) }}</td><td class="number-cell">{{ Number(row.weightRate).toFixed(2).replace(/\.?0+$/, '') }}</td><td>{{ row.creator }}</td><td>{{ displayDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ displayDate(row.reviseDate) }}</td>
            </tr>
            <tr v-if="!members.length"><td colspan="9" class="empty-row">{{ activeGroup ? '此群組尚無作業站明細' : '請先選取作業站群組' }}</td></tr>
          </tbody>
        </table></div>
        <aside class="vertical-actions detail-actions">
          <button class="kd-button primary" type="button" :disabled="!activeGroup || busy" @click="openMember('new')">新增</button>
          <button class="kd-button" type="button" :disabled="!activeMember || busy" @click="openMember('edit')">修改</button>
          <button class="kd-button" type="button" :disabled="!activeMember || busy" @click="deleteMembers">刪除</button>
        </aside>
      </div>
      <div class="grid-status">明細筆數 {{ members.length }}<span v-if="members.length">　權重合計 {{ members.reduce((sum, row) => sum + Number(row.weightRate), 0).toFixed(2).replace(/\.?0+$/, '') }}%</span></div>
    </fieldset>
    <div class="page-footer">
      <span v-if="notice" class="notice">{{ notice }}</span><span v-if="error" class="error-message" role="alert">{{ error }}</span>
      <button class="kd-button" type="button" @click="emit('close')">關閉 (X)</button>
    </div>
    <div v-if="headerDialogOpen" class="dialog-backdrop" @click.self="headerMode = ''">
      <section class="win-dialog header-dialog" role="dialog" aria-modal="true">
        <h2>OP Group Basis Define ({{ headerMode === 'new' ? 'ADD' : headerMode === 'edit' ? 'EDIT' : 'Copy' }})</h2>
        <fieldset class="win-group form-group"><legend>OP Group Data</legend>
          <div class="header-form-grid">
            <label>作業站群組編號<input v-model="headerDraft.opGroupNo" class="kd-input yellow-field" maxlength="20" :readonly="headerMode === 'edit'" @blur="headerDraft.opGroupNo = headerDraft.opGroupNo.toLocaleUpperCase()"></label>
            <label>作業站群組名稱<input v-model="headerDraft.opGroupName" class="kd-input yellow-field" maxlength="50"></label>
            <label>生產區段<span class="picker-field"><input :value="headerDraft.psNo" class="kd-input" readonly><button class="kd-button picker-button" type="button" @click="segmentPickerOpen = true">...</button></span></label>
            <label>群組順序<input v-model.number="headerDraft.opGroupOrder" class="kd-input number-input" type="number" min="0" max="999999" step="1"></label>
            <label class="description-field">說明<textarea v-model="headerDraft.description" class="kd-input" maxlength="255"></textarea></label>
          </div>
        </fieldset>
        <fieldset class="win-group audit-group"><legend>基本資料</legend>
          <div><span>建立人</span><input :value="headerAudit.creator" readonly><span>建立日期</span><input :value="displayDate(headerAudit.createDate)" readonly></div>
          <div><span>修改人</span><input :value="headerAudit.reviser" readonly><span>修改日期</span><input :value="displayDate(headerAudit.reviseDate)" readonly></div>
        </fieldset>
        <div class="dialog-actions"><button class="kd-button primary" type="button" :disabled="headerSaving" @click="saveHeader(true)">確定</button><button class="kd-button" type="button" :disabled="headerSaving" @click="headerMode = ''">關閉</button><button class="kd-button" type="button" :disabled="headerSaving" @click="saveHeader(false)">套用</button></div>
        <div v-if="error" class="dialog-error">{{ error }}</div>
      </section>
    </div>
    <div v-if="memberDialogOpen" class="dialog-backdrop" @click.self="memberMode = ''">
      <section class="win-dialog member-dialog" role="dialog" aria-modal="true">
        <h2>OP Group Detail Define ({{ memberMode === 'new' ? 'ADD' : 'EDIT' }})</h2>
        <fieldset class="win-group form-group"><legend>OP Group Detail</legend>
          <label>作業站群組編號<input :value="activeGroup?.opGroupNo" class="kd-input" readonly></label>
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
        <div class="dialog-actions"><button class="kd-button primary" type="button" :disabled="memberSaving" @click="saveMember(true)">確定</button><button class="kd-button" type="button" :disabled="memberSaving" @click="memberMode = ''">關閉</button><button class="kd-button" type="button" :disabled="memberSaving" @click="saveMember(false)">套用</button></div>
        <div v-if="error" class="dialog-error">{{ error }}</div>
      </section>
    </div>
    <div v-if="segmentPickerOpen" class="dialog-backdrop picker-backdrop" @click.self="segmentPickerOpen = false">
      <section class="win-dialog picker-dialog" role="dialog" aria-modal="true"><h2>選擇生產區段</h2>
        <table class="kd-grid"><thead><tr><th>區段編號</th><th>區段名稱</th></tr></thead>
          <tbody><tr v-for="option in segments" :key="option.psNo" @click="chooseSegment(option)"><td>{{ option.psNo }}</td><td>{{ option.psName }}</td></tr></tbody>
        </table><div class="dialog-actions"><button class="kd-button" type="button" @click="segmentPickerOpen = false">關閉</button></div>
      </section>
    </div>
    <div v-if="operationPickerOpen" class="dialog-backdrop picker-backdrop" @click.self="operationPickerOpen = false">
      <section class="win-dialog picker-dialog" role="dialog" aria-modal="true"><h2>選擇已核准作業站</h2>
        <div class="picker-search"><input v-model="pickerKeyword" class="kd-input" placeholder="作業站編號／名稱"><button class="kd-button" type="button">查詢</button></div>
        <table class="kd-grid"><thead><tr><th>作業站編號</th><th>作業站名稱</th><th>生產區段</th><th>狀態</th></tr></thead>
          <tbody><tr v-for="option in visibleOperations" :key="option.opNo" @click="chooseOperation(option)"><td>{{ option.opNo }}</td><td>{{ option.opName }}</td><td>{{ option.psNo }}</td><td>已核准</td></tr></tbody>
        </table><div v-if="!visibleOperations.length" class="empty-row">目前生產區段沒有可選的已核准作業站</div>
        <div class="dialog-actions"><button class="kd-button" type="button" @click="operationPickerOpen = false">關閉</button></div>
      </section>
    </div>
  </div>
</template>

<style scoped>
.operation-group-page{box-sizing:border-box;min-height:570px;height:calc(100vh - 116px);padding:8px 10px 10px;color:#303030;background:#fff;font:13px "Microsoft JhengHei",Arial,sans-serif;display:flex;flex-direction:column;gap:6px}
.query-strip{min-height:39px;display:flex;align-items:center;gap:8px;padding:4px 10px;border:1px solid #c7c7c7;background:#fafafa;flex:0 0 auto}
.query-field{display:flex;align-items:center;gap:7px;white-space:nowrap}.query-field select{width:172px;height:27px;border:1px solid #aaa;background:#fff;color:#333;padding:2px 5px}
.kd-input{box-sizing:border-box;height:28px;border:1px solid #999;border-radius:0;padding:4px 7px;background:#fff;color:#333;font:inherit}.kd-input:focus{outline:1px solid #d79a31;outline-offset:0}.yellow-field{background:#fff0ae}.query-input{width:190px}.query-percent{margin-left:-5px}.query-hint{color:#777;margin-left:5px}
.kd-button{min-width:66px;height:30px;border:1px solid #aaa;border-radius:3px;padding:3px 10px;background:linear-gradient(#fff,#ededed);color:#333;font:inherit;white-space:nowrap;cursor:pointer}.kd-button:hover:not(:disabled){background:#fff4cf;border-color:#d3a344}.kd-button:disabled{color:#999;cursor:default}.kd-button.primary{background:#ffae32;border-color:#e69a26;color:#fff;font-weight:600}.kd-button.primary:hover:not(:disabled){background:#f49b1e}
.win-group{box-sizing:border-box;min-width:0;border:1px solid #b9b9b9;padding:5px 9px 6px;background:#fff}.win-group>legend{padding:0 6px;color:#555}.master-panel{height:255px;flex:0 0 255px}.detail-panel{height:232px;flex:1 1 232px;min-height:180px}
.panel-layout{display:flex;gap:10px;height:calc(100% - 20px);min-height:0}.grid-scroll{flex:1 1 auto;min-width:0;overflow:auto;border:1px solid #bdbdbd}.kd-grid{border-collapse:collapse;width:max-content;min-width:100%;color:#333;font:inherit;background:#fff}.kd-grid th,.kd-grid td{height:27px;box-sizing:border-box;border:1px solid #c9c9c9;padding:4px 7px;text-align:left;white-space:nowrap}.kd-grid th{position:sticky;top:0;z-index:1;background:#e6e6e6;font-weight:600}.kd-grid tbody tr{cursor:pointer}.kd-grid tbody tr:hover{background:#fff8df}.kd-grid tbody tr.active{background:#ffe79a}.kd-grid .check-cell{width:29px;min-width:29px;text-align:center;padding:2px}.kd-grid .check-cell input{margin:0;accent-color:#d39829}.group-grid{min-width:1260px}.detail-grid{min-width:1120px}.number-cell{text-align:right!important}.empty-row{height:44px!important;text-align:center!important;color:#777;cursor:default!important}
.vertical-actions{flex:0 0 88px;display:flex;flex-direction:column;gap:5px}.vertical-actions .kd-button{width:88px;min-width:0;height:32px;padding:2px 3px}.grid-status{height:18px;color:#777;font-size:12px;padding:1px 2px 0}.page-footer{min-height:36px;display:flex;align-items:center;justify-content:flex-end;gap:10px;flex:0 0 auto}.notice{margin-right:auto;color:#666}.error-message{margin-right:auto;color:#b42318}
.dialog-backdrop{position:fixed;inset:0;z-index:2000;display:flex;align-items:center;justify-content:center;padding:18px;background:rgb(0 0 0 / 32%)}.win-dialog{box-sizing:border-box;width:min(566px,calc(100vw - 28px));max-height:calc(100vh - 30px);overflow:auto;padding:12px;border:1px solid #969696;background:#f8f8f8;box-shadow:0 6px 24px rgb(0 0 0 / 22%)}.win-dialog h2{margin:0 0 9px;color:#444;font-size:16px;font-weight:500}.form-group{padding:10px}.header-form-grid{display:grid;grid-template-columns:1fr 1fr;gap:12px 10px;padding:8px 4px 5px}.header-form-grid label,.member-dialog .form-group>label{display:flex;flex-direction:column;gap:4px}.header-form-grid input,.member-dialog .form-group>label>input{width:100%}.picker-field{display:flex;gap:4px}.picker-field>input{flex:1;min-width:0}.picker-button{min-width:30px;width:30px;padding:1px}.number-input{width:110px!important;text-align:right}.description-field{grid-column:1/-1}.description-field textarea{box-sizing:border-box;width:100%;min-height:118px;resize:vertical}
.audit-group{margin-top:8px;padding:7px 9px 9px}.audit-group>div{display:grid;grid-template-columns:54px minmax(80px,1fr) 54px minmax(100px,1fr);align-items:center;gap:5px;margin:3px 0}.audit-group input{box-sizing:border-box;width:100%;height:25px;border:1px solid #ccc;padding:3px 5px;background:#eee;color:#666;font:inherit}.dialog-actions{display:flex;justify-content:flex-end;gap:8px;margin-top:10px}.dialog-actions .primary{min-width:94px}.dialog-error{margin-top:7px;color:#b42318}.member-dialog{width:min(484px,calc(100vw - 28px))}.member-dialog .form-group{display:grid;grid-template-columns:1fr;gap:11px;padding:10px 12px}.role-group{display:flex;flex-wrap:wrap;gap:12px;border:1px solid #bbb;padding:7px 10px 9px}.role-group legend{padding:0 5px}.role-group label{display:inline-flex;align-items:center;gap:4px;white-space:nowrap}.role-group input{accent-color:#d39829}.weight-field{display:flex;align-items:center;gap:7px}
.picker-dialog{width:min(650px,calc(100vw - 28px))}.picker-dialog .kd-grid{width:100%}.picker-dialog .kd-grid tbody tr{cursor:pointer}.picker-dialog .kd-grid tbody tr:hover{background:#ffe79a}.picker-search{display:flex;gap:7px;margin:7px 0}.picker-search .kd-input{flex:1}
@media(max-width:920px){.operation-group-page{height:auto;min-height:100%;overflow:auto}.query-strip{flex-wrap:wrap}.query-hint{display:none}.master-panel{height:255px}.detail-panel{min-height:232px}}
@media(max-width:620px){.panel-layout{flex-direction:column}.vertical-actions{flex-direction:row;flex-wrap:wrap;flex-basis:auto}.vertical-actions .kd-button{flex:1 1 70px}.master-panel,.detail-panel{height:330px;flex-basis:330px}.header-form-grid{grid-template-columns:1fr}.description-field{grid-column:auto}.audit-group>div{grid-template-columns:48px minmax(55px,1fr)}.audit-group>div span:nth-of-type(2),.audit-group>div input:last-child{display:none}}
</style>
