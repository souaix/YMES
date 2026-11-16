<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

type IssueState = 0 | 1 | 2 | -1
type EditorMode = 'add' | 'edit' | 'copy' | null
interface GroupRow {
  groupNo: string
  groupName: string
  departmentNo: string | null
  description: string | null
  issueState: IssueState
  creator: string | null
  createDate: string | null
  reviser: string | null
  reviseDate: string | null
  dataStamp: number | null
}
interface UserRow { departmentNo: string; userNo: string; userName: string }
interface Members { primary: string[]; cc: string[] }
interface MockStore { rows: GroupRow[]; members: Record<string, Members> }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const useMock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes-dev-mock:qc-hold-disposition-group:v1'
const issueOptions: { value: IssueState; label: string }[] = [
  { value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' },
]
const seedRows: GroupRow[] = [
  { groupNo: 'DEV-QC-GROUP-01', groupName: 'DEV 品質處理一組', departmentNo: null, description: '一般主處理人員群組', issueState: 0, creator: 'DEV001', createDate: '2026-11-10T08:00:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
  { groupNo: 'DEV-QC-GROUP-02', groupName: 'DEV 品質處理二組', departmentNo: null, description: '簽核中範例', issueState: 1, creator: 'DEV001', createDate: '2026-11-10T08:30:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-10T09:00:00+08:00', dataStamp: 1 },
  { groupNo: 'DEV-QC-GROUP-03', groupName: 'DEV 夜班副知組', departmentNo: 'DEV-QA', description: '已簽核範例', issueState: 2, creator: 'DEV002', createDate: '2026-11-11T08:00:00+08:00', reviser: 'DEV001', reviseDate: '2026-11-11T09:00:00+08:00', dataStamp: 2 },
  { groupNo: 'DEV-QC-GROUP-04', groupName: 'DEV 停用群組', departmentNo: null, description: '不使用狀態範例', issueState: -1, creator: 'DEV001', createDate: '2026-11-12T08:00:00+08:00', reviser: 'DEV001', reviseDate: '2026-11-12T09:00:00+08:00', dataStamp: 3 },
]
const seedUsers: UserRow[] = [
  { departmentNo: 'DEV-QA', userNo: 'DEV001', userName: 'DEV 品保甲' },
  { departmentNo: 'DEV-QA', userNo: 'DEV002', userName: 'DEV 品保乙' },
  { departmentNo: 'DEV-MFG', userNo: 'DEV003', userName: 'DEV 製造甲' },
  { departmentNo: 'DEV-MFG', userNo: 'DEV004', userName: 'DEV 製造乙' },
  { departmentNo: 'DEV-ENG', userNo: 'DEV005', userName: 'DEV 工程甲' },
  { departmentNo: 'DEV-ENG', userNo: 'DEV006', userName: 'DEV 工程乙' },
]
const seedMembers: Record<string, Members> = {
  'DEV-QC-GROUP-01': { primary: ['DEV001', 'DEV003'], cc: ['DEV005'] },
  'DEV-QC-GROUP-02': { primary: ['DEV002'], cc: [] },
  'DEV-QC-GROUP-03': { primary: ['DEV001'], cc: ['DEV004', 'DEV006'] },
  'DEV-QC-GROUP-04': { primary: [], cc: [] },
}

const rows = ref<GroupRow[]>([])
const users = ref<UserRow[]>([])
const issueState = ref<IssueState>(0)
const searchText = ref('')
const selectedKeys = ref<string[]>([])
const activeGroupNo = ref('')
const primaryUserNos = ref<string[]>([])
const ccUserNos = ref<string[]>([])
const selectedAvailable = ref<string[]>([])
const selectedPrimary = ref<string[]>([])
const selectedCc = ref<string[]>([])
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const notice = ref('')
const editorMode = ref<EditorMode>(null)
const editorSource = ref<GroupRow | null>(null)
const editor = ref({ groupNo: '', groupName: '', description: '' })
const editorStartedAt = ref('')
const editorError = ref('')
const editorBusy = ref(false)

const visibleRows = computed(() => rows.value
  .filter(row => row.issueState === issueState.value)
  .filter(row => !searchText.value.trim() || row.groupNo.toLocaleLowerCase().includes(searchText.value.trim().toLocaleLowerCase())))
const selectedRows = computed(() => visibleRows.value.filter(row => selectedKeys.value.includes(row.groupNo)))
const activeRow = computed(() => rows.value.find(row => row.groupNo === activeGroupNo.value) ?? null)
const selectedUsers = computed(() => new Set([...primaryUserNos.value, ...ccUserNos.value]))
const availableUsers = computed(() => users.value.filter(user => !selectedUsers.value.has(user.userNo)))
const primaryMembers = computed(() => primaryUserNos.value.map(no => users.value.find(user => user.userNo === no) ?? { userNo: no, userName: '（使用者資料不存在）', departmentNo: '' }))
const ccMembers = computed(() => ccUserNos.value.map(no => users.value.find(user => user.userNo === no) ?? { userNo: no, userName: '（使用者資料不存在）', departmentNo: '' }))
const editorTitle = computed(() => `異常處理群組（${editorMode.value === 'add' ? '新增' : editorMode.value === 'copy' ? '複製' : '修改'}）`)
const editorLocked = computed(() => editorMode.value === 'edit' && !!editorSource.value && (editorSource.value.issueState === 1 || editorSource.value.issueState === -1))
const auditCreator = computed(() => editorMode.value === 'edit' ? editorSource.value?.creator ?? '' : props.currentUserNo || '')
const auditCreateDate = computed(() => editorMode.value === 'edit' ? editorSource.value?.createDate ?? '' : editorStartedAt.value)
const auditReviser = computed(() => editorMode.value === 'edit' ? editorSource.value?.reviser ?? '' : '')
const auditReviseDate = computed(() => editorMode.value === 'edit' ? editorSource.value?.reviseDate ?? '' : '')

function now(): string { return new Date().toISOString() }
function stateLabel(value: IssueState): string { return issueOptions.find(option => option.value === value)?.label ?? String(value) }
function stateClass(value: IssueState): string { return `state-${value < 0 ? `minus-${Math.abs(value)}` : value}` }
function dateText(value?: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat('zh-TW', { dateStyle: 'short', timeStyle: 'medium', hour12: false }).format(date)
}
function stampQuery(value: number | null): string { return value === null ? 'dataStamp=0&dataStampIsNull=true' : `dataStamp=${encodeURIComponent(String(value ?? 0))}` }
function readStore(): MockStore {
  try {
    const raw = window.localStorage.getItem(storageKey)
    if (!raw) return { rows: structuredClone(seedRows), members: structuredClone(seedMembers) }
    const saved = JSON.parse(raw) as Partial<MockStore>
    return { rows: saved.rows ?? structuredClone(seedRows), members: saved.members ?? structuredClone(seedMembers) }
  } catch { return { rows: structuredClone(seedRows), members: structuredClone(seedMembers) } }
}
function writeStore(store: MockStore): void { window.localStorage.setItem(storageKey, JSON.stringify(store)) }
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init, headers: { ...(init.body ? { 'Content-Type': 'application/json' } : {}), ...init.headers } })
  if (response.status === 401) { window.location.assign(appBase + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    if (response.status === 404) throw new Error('QC405 異常處理群組 API 尚未建立，請依 todolist_homepc.md 完成資料與 API。')
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 回應錯誤（${response.status}）。`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
async function loadRows(): Promise<void> {
  if (useMock.value) { rows.value = readStore().rows; return }
  const query = new URLSearchParams()
  if (searchText.value.trim()) { query.set('searchFieldName', 'GroupNo'); query.set('searchFieldValue', searchText.value.trim()) }
  const suffix = query.toString()
  rows.value = await request<GroupRow[]>(`api/qc/hold-disposition-groups${suffix ? `?${suffix}` : ''}`)
}
async function loadUsers(): Promise<void> {
  if (useMock.value) { users.value = structuredClone(seedUsers); return }
  users.value = await request<UserRow[]>('api/qc/hold-disposition-groups/options/users?issueState=2')
}
async function loadMembers(groupNo: string): Promise<void> {
  if (useMock.value) {
    const members = readStore().members[groupNo] ?? { primary: [], cc: [] }
    primaryUserNos.value = [...members.primary]
    ccUserNos.value = [...members.cc]
    return
  }
  const result = await request<Members>(`api/qc/hold-disposition-groups/${encodeURIComponent(groupNo)}/members`)
  primaryUserNos.value = result.primary ?? []
  ccUserNos.value = result.cc ?? []
}
async function query(): Promise<void> {
  loading.value = true; busy.value = true; errorMessage.value = ''; notice.value = ''; selectedKeys.value = []; activeGroupNo.value = ''; primaryUserNos.value = []; ccUserNos.value = []
  try { await loadRows() } catch (error) { errorMessage.value = error instanceof Error ? error.message : '查詢異常處理群組失敗。' }
  finally { loading.value = false; busy.value = false }
}
async function selectRow(row: GroupRow): Promise<void> {
  activeGroupNo.value = row.groupNo
  if (!selectedKeys.value.includes(row.groupNo)) selectedKeys.value = [row.groupNo]
  selectedAvailable.value = []; selectedPrimary.value = []; selectedCc.value = []
  busy.value = true; errorMessage.value = ''
  try { await loadMembers(row.groupNo) } catch (error) { errorMessage.value = error instanceof Error ? error.message : '載入群組成員失敗。' }
  finally { busy.value = false }
}
function toggleGroup(row: GroupRow, checked: boolean): void {
  if (checked) {
    selectedKeys.value = [...new Set([...selectedKeys.value, row.groupNo])]
    void selectRow(row)
    return
  }
  selectedKeys.value = selectedKeys.value.filter(key => key !== row.groupNo)
  if (activeGroupNo.value === row.groupNo) {
    activeGroupNo.value = ''; primaryUserNos.value = []; ccUserNos.value = []
    selectedAvailable.value = []; selectedPrimary.value = []; selectedCc.value = []
  }
}
watch(issueState, () => { selectedKeys.value = []; activeGroupNo.value = ''; primaryUserNos.value = []; ccUserNos.value = []; selectedAvailable.value = []; selectedPrimary.value = []; selectedCc.value = [] })

function openEditor(mode: Exclude<EditorMode, null>): void {
  errorMessage.value = ''; notice.value = ''; editorError.value = ''
  const source = mode === 'add' ? null : activeRow.value ? structuredClone(activeRow.value) : null
  if (mode !== 'add' && !source) { errorMessage.value = '請先選取一筆異常處理群組。'; return }
  editorSource.value = source
  editorStartedAt.value = mode === 'edit' ? '' : now()
  editor.value = source ? { groupNo: source.groupNo, groupName: source.groupName, description: source.description ?? '' } : { groupNo: '', groupName: '', description: '' }
  editorMode.value = mode
}
function closeEditor(): void { editorMode.value = null; editorSource.value = null; editorStartedAt.value = ''; editorError.value = '' }
function validateEditor(): string {
  const groupNo = editor.value.groupNo.trim()
  const groupName = editor.value.groupName.trim()
  if (!groupNo) return '群組編號為必填欄位。'
  if (!groupName) return '群組名稱為必填欄位。'
  if (groupNo.length > 20) return '群組編號最多 20 字。'
  if (groupName.length > 50) return '群組名稱最多 50 字。'
  if (editor.value.description.length > 255) return '說明最多 255 字。'
  if (editorLocked.value) return '此狀態的資料不可修改。'
  return ''
}
async function refreshRows(activeGroup = ''): Promise<void> {
  await loadRows()
  const found = rows.value.find(row => row.groupNo === activeGroup && row.issueState === issueState.value)
  activeGroupNo.value = found?.groupNo ?? ''
  selectedKeys.value = found ? [found.groupNo] : []
  if (!found) { primaryUserNos.value = []; ccUserNos.value = []; selectedAvailable.value = []; selectedPrimary.value = []; selectedCc.value = [] }
}
async function saveEditor(keepOpen: boolean): Promise<void> {
  editorError.value = validateEditor()
  if (editorError.value) return
  const mode = editorMode.value
  const source = editorSource.value
  if (!mode) return
  const groupNo = editor.value.groupNo.trim().toLocaleUpperCase()
  const groupName = editor.value.groupName.trim()
  const description = editor.value.description.trim() || null
  editorBusy.value = true; errorMessage.value = ''; notice.value = ''
  try {
    if (useMock.value) {
      const store = readStore()
      if (mode !== 'edit' && store.rows.some(row => row.groupNo === groupNo)) throw new Error(`群組編號 ${groupNo} 已存在。`)
      if (mode === 'edit' && source) {
        const index = store.rows.findIndex(row => row.groupNo === source.groupNo)
        if (index < 0) throw new Error('資料已不存在，請重新查詢。')
        store.rows[index] = { ...store.rows[index], groupName, description, reviser: props.currentUserNo || 'DEV001', reviseDate: now(), dataStamp: (store.rows[index].dataStamp ?? 0) + 1 }
      } else if (mode === 'copy' && source) {
        store.rows.push({ ...source, groupNo, groupName, description, issueState: 0, creator: props.currentUserNo || 'DEV001', createDate: now(), reviser: null, reviseDate: null, dataStamp: 0 })
        store.members[groupNo] = structuredClone(store.members[source.groupNo] ?? { primary: [], cc: [] })
      } else {
        store.rows.push({ groupNo, groupName, departmentNo: null, description, issueState: 0, creator: props.currentUserNo || 'DEV001', createDate: now(), reviser: null, reviseDate: null, dataStamp: 0 })
        store.members[groupNo] = { primary: [], cc: [] }
      }
      writeStore(store)
    } else if (mode === 'edit' && source) {
      await request(`api/qc/hold-disposition-groups/${encodeURIComponent(source.groupNo)}?${stampQuery(source.dataStamp)}`, { method: 'PUT', body: JSON.stringify({ groupName, description }) })
    } else if (mode === 'copy' && source) {
      await request('api/qc/hold-disposition-groups/copy', { method: 'POST', body: JSON.stringify({ sourceGroupNo: source.groupNo, groupNo, groupName, description }) })
    } else {
      await request('api/qc/hold-disposition-groups', { method: 'POST', body: JSON.stringify({ groupNo, groupName, description }) })
    }
    await refreshRows(groupNo)
    if (keepOpen) {
      if (mode === 'edit') editorSource.value = structuredClone(rows.value.find(row => row.groupNo === groupNo) ?? source!)
      editorStartedAt.value = mode === 'edit' ? '' : now()
      notice.value = mode === 'edit' ? '異常處理群組已套用。' : '異常處理群組已新增；可繼續輸入另一筆。'
    } else {
      closeEditor()
      notice.value = mode === 'edit' ? '異常處理群組已儲存。' : mode === 'copy' ? '異常處理群組及成員已複製。' : '異常處理群組已新增。'
    }
  } catch (error) { editorError.value = error instanceof Error ? error.message : '儲存異常處理群組失敗。' }
  finally { editorBusy.value = false }
}

async function deleteSelected(): Promise<void> {
  const targets = [...selectedRows.value]
  if (!targets.length) { errorMessage.value = '請先選取要刪除的資料。'; return }
  if (targets.some(row => row.issueState === 1)) { errorMessage.value = '簽核中的資料不可刪除。'; return }
  if (!window.confirm(`確定刪除選取的 ${targets.length} 筆異常處理群組？`)) return
  busy.value = true; errorMessage.value = ''; notice.value = ''
  let completed = 0
  try {
    for (const row of targets) {
      if (useMock.value) {
        const store = readStore()
        if (row.issueState === 2) {
          const index = store.rows.findIndex(item => item.groupNo === row.groupNo)
          if (index >= 0) store.rows[index] = { ...store.rows[index], issueState: -1, reviser: props.currentUserNo || 'DEV001', reviseDate: now(), dataStamp: (store.rows[index].dataStamp ?? 0) + 1 }
        } else {
          store.rows = store.rows.filter(item => item.groupNo !== row.groupNo)
          delete store.members[row.groupNo]
        }
        writeStore(store)
      } else {
        await request(`api/qc/hold-disposition-groups/${encodeURIComponent(row.groupNo)}?issueState=${row.issueState}&${stampQuery(row.dataStamp)}`, { method: 'DELETE' })
      }
      completed++
    }
    await refreshRows()
    if (activeGroupNo.value && !rows.value.some(row => row.groupNo === activeGroupNo.value)) activeGroupNo.value = ''
    notice.value = `已完成 ${completed} 筆刪除。`
  } catch (error) { errorMessage.value = `${error instanceof Error ? error.message : '刪除失敗。'}（已完成 ${completed}/${targets.length} 筆）` }
  finally { busy.value = false }
}
async function approveSelected(): Promise<void> {
  const targets = [...selectedRows.value]
  if (!targets.length) { errorMessage.value = '請先選取要核准的資料。'; return }
  if (targets.some(row => row.issueState !== 0)) { errorMessage.value = '只有未簽核資料可以送出核准。'; return }
  busy.value = true; errorMessage.value = ''; notice.value = ''
  let completed = 0
  try {
    for (const row of targets) {
      if (useMock.value) {
        const store = readStore(); const index = store.rows.findIndex(item => item.groupNo === row.groupNo)
        if (index >= 0) store.rows[index] = { ...store.rows[index], issueState: 2, reviser: props.currentUserNo || 'DEV001', reviseDate: now(), dataStamp: (store.rows[index].dataStamp ?? 0) + 1 }
        writeStore(store)
      } else {
        await request(`api/qc/hold-disposition-groups/${encodeURIComponent(row.groupNo)}/approve?issueState=0&${stampQuery(row.dataStamp)}`, { method: 'POST' })
      }
      completed++
    }
    await refreshRows()
    notice.value = `已送出 ${completed} 筆核准。DEV 預覽直接模擬核准完成；正式簽核依 Issue/Job 流程。`
  } catch (error) { errorMessage.value = `${error instanceof Error ? error.message : '核准失敗。'}（已完成 ${completed}/${targets.length} 筆）` }
  finally { busy.value = false }
}

function moveToPrimary(): void {
  primaryUserNos.value = [...new Set([...primaryUserNos.value, ...selectedAvailable.value])]
  selectedAvailable.value = []; selectedPrimary.value = []
}
function moveToCc(): void {
  ccUserNos.value = [...new Set([...ccUserNos.value, ...selectedAvailable.value])]
  selectedAvailable.value = []; selectedCc.value = []
}
function removePrimary(): void {
  primaryUserNos.value = primaryUserNos.value.filter(no => !selectedPrimary.value.includes(no))
  selectedPrimary.value = []
}
function removeCc(): void {
  ccUserNos.value = ccUserNos.value.filter(no => !selectedCc.value.includes(no))
  selectedCc.value = []
}
async function saveMembers(): Promise<void> {
  const group = activeRow.value
  if (!group) { errorMessage.value = '請先選取異常處理群組。'; return }
  busy.value = true; errorMessage.value = ''; notice.value = ''
  try {
    const payload = { primaryUserNos: [...primaryUserNos.value], ccUserNos: [...ccUserNos.value] }
    if (useMock.value) {
      const store = readStore()
      store.members[group.groupNo] = { primary: payload.primaryUserNos, cc: payload.ccUserNos }
      const index = store.rows.findIndex(row => row.groupNo === group.groupNo)
      if (index >= 0) store.rows[index] = { ...store.rows[index], reviser: props.currentUserNo || 'DEV001', reviseDate: now(), dataStamp: (store.rows[index].dataStamp ?? 0) + 1 }
      writeStore(store)
    } else {
      await request(`api/qc/hold-disposition-groups/${encodeURIComponent(group.groupNo)}/members?${stampQuery(group.dataStamp)}`, { method: 'PUT', body: JSON.stringify(payload) })
    }
    await refreshRows(group.groupNo)
    notice.value = '群組人員已儲存。'
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '儲存群組人員失敗。' }
  finally { busy.value = false }
}

onMounted(async () => {
  loading.value = true; errorMessage.value = ''
  try { await loadUsers(); await loadRows() } catch (error) { errorMessage.value = error instanceof Error ? error.message : '載入異常處理群組失敗。' }
  finally { loading.value = false }
})
</script>

<template>
  <main class="hold-group-page">
    <header class="page-title"><h1>異常處理群組</h1><button class="button" @click="emit('close')">關閉</button></header>
    <section class="filter-panel">
      <div class="state-options" role="radiogroup" aria-label="簽核狀態">
        <label v-for="option in issueOptions" :key="option.value"><input v-model="issueState" type="radio" :value="option.value" :disabled="busy">{{ option.label }}</label>
      </div>
      <label class="search-field"><span>群組編號</span><input v-model="searchText" type="search" maxlength="20" @keyup.enter="query"></label>
      <span class="percent-mark">%</span>
      <button class="button" :disabled="loading || busy" @click="query">查詢</button>
    </section>
    <p v-if="errorMessage" class="message error" role="alert">{{ errorMessage }}</p>
    <p v-if="notice" class="message success" role="status">{{ notice }}</p>

    <div class="split-layout">
      <section class="master-panel panel">
        <div class="grid-wrap master-grid">
          <table>
            <thead><tr><th class="check-col"></th><th>群組編號</th><th>群組名稱</th><th>說明</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th></tr></thead>
            <tbody>
              <tr v-for="row in visibleRows" :key="row.groupNo" :class="{ active: activeGroupNo === row.groupNo }" @click="selectRow(row)">
                <td class="check-col" @click.stop><input type="checkbox" :checked="selectedKeys.includes(row.groupNo)" :aria-label="`選取 ${row.groupNo}`" @change="toggleGroup(row, ($event.target as HTMLInputElement).checked)"></td>
                <td>{{ row.groupNo }}</td><td>{{ row.groupName }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ dateText(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ dateText(row.reviseDate) }}</td>
              </tr>
              <tr v-if="!visibleRows.length"><td class="empty" colspan="8">{{ loading ? '資料載入中…' : '沒有符合條件的資料' }}</td></tr>
            </tbody>
          </table>
        </div>
        <aside class="action-rail">
          <button class="button primary" :disabled="busy" @click="openEditor('add')">新增(A)</button>
          <button class="button" :disabled="busy || !activeRow || editorLocked" @click="openEditor('edit')">修改(E)</button>
          <button class="button" :disabled="busy || !activeRow" @click="openEditor('copy')">複製(C)</button>
          <button class="button" :disabled="busy || !selectedRows.length" @click="approveSelected">核准</button>
          <button class="button" :disabled="busy || !selectedRows.length" @click="deleteSelected">刪除(D)</button>
        </aside>
        <footer class="grid-footer">資料總筆數 {{ visibleRows.length }}<span v-if="activeRow">　目前群組：{{ activeRow.groupNo }}（{{ stateLabel(activeRow.issueState) }}）</span></footer>
      </section>

      <section class="member-panel panel">
        <header class="member-title"><h2>異常處理群組人員</h2><span v-if="activeRow">{{ activeRow.groupNo }}　{{ activeRow.groupName }}</span><span v-else>請先選取上方群組</span></header>
        <div class="member-layout">
          <section class="member-list">
            <h3>未選擇使用者</h3>
            <div class="grid-wrap"><table><thead><tr><th class="check-col"></th><th>部門編號</th><th>使用者編號</th><th>使用者名稱</th></tr></thead><tbody>
              <tr v-for="user in availableUsers" :key="user.userNo" :class="{ selected: selectedAvailable.includes(user.userNo) }" @click="selectedAvailable = selectedAvailable.includes(user.userNo) ? selectedAvailable.filter(no => no !== user.userNo) : [...selectedAvailable, user.userNo]">
                <td class="check-col"><input type="checkbox" :checked="selectedAvailable.includes(user.userNo)" @click.stop="selectedAvailable = selectedAvailable.includes(user.userNo) ? selectedAvailable.filter(no => no !== user.userNo) : [...selectedAvailable, user.userNo]"></td><td>{{ user.departmentNo }}</td><td>{{ user.userNo }}</td><td>{{ user.userName }}</td>
              </tr><tr v-if="!availableUsers.length"><td class="empty" colspan="4">{{ activeRow ? '所有已核准使用者皆已加入群組' : '選取群組後載入使用者' }}</td></tr>
            </tbody></table></div>
          </section>
          <div class="move-rail" aria-label="移動使用者">
            <button class="button arrow" title="加入主處理群組" :disabled="!activeRow || !selectedAvailable.length || busy" @click="moveToPrimary">→</button>
            <button class="button arrow" title="移除主處理群組" :disabled="!selectedPrimary.length || busy" @click="removePrimary">←</button>
            <button class="button arrow" title="加入副知群組" :disabled="!activeRow || !selectedAvailable.length || busy" @click="moveToCc">→ CC</button>
            <button class="button arrow" title="移除副知群組" :disabled="!selectedCc.length || busy" @click="removeCc">←</button>
          </div>
          <div class="assigned-lists">
            <section class="member-list assigned-primary"><h3>主處理群組</h3><div class="grid-wrap"><table><thead><tr><th class="check-col"></th><th>部門編號</th><th>使用者編號</th><th>使用者名稱</th></tr></thead><tbody>
              <tr v-for="user in primaryMembers" :key="user.userNo" :class="{ selected: selectedPrimary.includes(user.userNo) }" @click="selectedPrimary = selectedPrimary.includes(user.userNo) ? selectedPrimary.filter(no => no !== user.userNo) : [...selectedPrimary, user.userNo]">
                <td class="check-col"><input type="checkbox" :checked="selectedPrimary.includes(user.userNo)" @click.stop="selectedPrimary = selectedPrimary.includes(user.userNo) ? selectedPrimary.filter(no => no !== user.userNo) : [...selectedPrimary, user.userNo]"></td><td>{{ user.departmentNo }}</td><td>{{ user.userNo }}</td><td>{{ user.userName }}</td>
              </tr><tr v-if="!primaryMembers.length"><td class="empty" colspan="4">尚未設定主處理人員</td></tr>
            </tbody></table></div></section>
            <section class="member-list assigned-cc"><h3>副知群組（CC）</h3><div class="grid-wrap"><table><thead><tr><th class="check-col"></th><th>部門編號</th><th>使用者編號</th><th>使用者名稱</th></tr></thead><tbody>
              <tr v-for="user in ccMembers" :key="user.userNo" :class="{ selected: selectedCc.includes(user.userNo) }" @click="selectedCc = selectedCc.includes(user.userNo) ? selectedCc.filter(no => no !== user.userNo) : [...selectedCc, user.userNo]">
                <td class="check-col"><input type="checkbox" :checked="selectedCc.includes(user.userNo)" @click.stop="selectedCc = selectedCc.includes(user.userNo) ? selectedCc.filter(no => no !== user.userNo) : [...selectedCc, user.userNo]"></td><td>{{ user.departmentNo }}</td><td>{{ user.userNo }}</td><td>{{ user.userName }}</td>
              </tr><tr v-if="!ccMembers.length"><td class="empty" colspan="4">尚未設定副知人員</td></tr>
            </tbody></table></div></section>
          </div>
        </div>
        <footer class="member-footer"><span>主處理 {{ primaryUserNos.length }} 人　副知 {{ ccUserNos.length }} 人</span><button class="button primary" :disabled="!activeRow || busy" @click="saveMembers">確定</button></footer>
      </section>
    </div>

    <div v-if="editorMode" class="dialog-backdrop" @click.self="closeEditor">
      <section class="editor-dialog" role="dialog" aria-modal="true" :aria-label="editorTitle">
        <header class="dialog-titlebar"><h2>{{ editorTitle }}</h2><button class="icon-close" aria-label="關閉" @click="closeEditor">×</button></header>
        <form class="editor-form" @submit.prevent="saveEditor(false)">
          <fieldset class="edit-box"><legend>群組資料</legend>
            <label class="form-field"><span>群組編號 <b class="required">*</b></span><input v-model="editor.groupNo" :readonly="editorMode === 'edit'" maxlength="20" required :class="{ readonly: editorMode === 'edit' }" @blur="editor.groupNo = editor.groupNo.toLocaleUpperCase()"></label>
            <label class="form-field"><span>群組名稱 <b class="required">*</b></span><input v-model="editor.groupName" maxlength="50" required></label>
            <label class="form-field"><span>說明</span><textarea v-model="editor.description" maxlength="255" rows="3"></textarea></label>
          </fieldset>
          <fieldset class="audit-box"><legend>資料建立／修改資訊</legend>
            <label class="form-field"><span>建立人</span><input :value="auditCreator" readonly></label><label class="form-field"><span>建立日期</span><input :value="dateText(auditCreateDate)" readonly></label>
            <label class="form-field"><span>修改人</span><input :value="auditReviser" readonly></label><label class="form-field"><span>修改日期</span><input :value="dateText(auditReviseDate)" readonly></label>
          </fieldset>
          <p v-if="editorError" class="message error" role="alert">{{ editorError }}</p>
          <footer class="dialog-actions"><button type="button" class="button" :disabled="editorBusy" @click="saveEditor(true)">套用</button><button type="submit" class="button primary" :disabled="editorBusy">確定</button><button type="button" class="button" :disabled="editorBusy" @click="closeEditor">關閉</button></footer>
        </form>
      </section>
    </div>
  </main>
</template>

<style scoped>
.hold-group-page{--line:#c9c4ba;--header:#e7e4dc;--grid-head:#4da1df;--kd-yellow:#fff0b5;--orange:#f7a32b;display:flex;flex-direction:column;gap:8px;height:100%;min-height:0;padding:10px;color:#302e2a;background:#f7f5ef;font:13px/1.35 "Segoe UI","Microsoft JhengHei",sans-serif}.page-title{display:flex;align-items:center;justify-content:space-between;min-height:30px}.page-title h1{margin:0;font-size:19px;font-weight:600}.button{min-height:30px;padding:4px 12px;border:1px solid #bdb8ae;border-radius:3px;background:linear-gradient(#fff,#f1efe9);color:#302e2a;font:inherit;white-space:nowrap;cursor:pointer}.button:hover:not(:disabled){border-color:#8f897e;background:#ece9e1}.button:disabled{opacity:.48;cursor:not-allowed}.button.primary{border-color:#e49421;background:var(--orange);color:#fff;font-weight:600}.filter-panel{display:flex;align-items:center;gap:9px;min-height:42px;padding:5px 10px;border:1px solid var(--line);background:#fff}.state-options{display:flex;align-items:center;gap:12px}.state-options label{display:flex;align-items:center;gap:4px;white-space:nowrap}.state-options input{accent-color:#f5a128}.search-field{display:flex;align-items:center;gap:7px;margin-left:9px}.search-field input{width:190px}.filter-panel input[type=search],.form-field input,.form-field textarea{min-height:29px;border:1px solid #99948a;border-radius:0;background:var(--kd-yellow);padding:4px 7px;color:#302e2a;font:inherit}.percent-mark{margin-left:-7px}.split-layout{display:grid;grid-template-rows:minmax(180px,42%) minmax(250px,1fr);gap:7px;flex:1;min-height:0}.panel{min-height:0;border:1px solid var(--line);background:#fff}.master-panel{display:grid;grid-template-columns:minmax(0,1fr) 98px;grid-template-rows:minmax(0,1fr) 22px;gap:5px;padding:6px}.grid-wrap{min-height:0;overflow:auto;border:1px solid #b7b2a8;background:#fff}.master-grid{grid-column:1;grid-row:1}.grid-wrap table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}.grid-wrap th{position:sticky;top:0;z-index:1;padding:5px 7px;border-right:1px solid #d2d0ca;background:var(--grid-head);color:#fff;text-align:left;font-weight:600}.grid-wrap td{height:26px;padding:3px 7px;border:1px solid #d9d7d1}.grid-wrap tbody tr{cursor:pointer}.grid-wrap tbody tr:hover td{background:#fff6dc}.grid-wrap tbody tr.active td{background:#ffe9aa}.grid-wrap tbody tr.selected td{background:#fff0c7}.check-col{width:28px;min-width:28px;text-align:center!important}.check-col input{accent-color:#f5a128}.empty{height:38px!important;text-align:center;color:#777;cursor:default}.action-rail{grid-column:2;grid-row:1;display:flex;flex-direction:column;gap:5px}.action-rail .button{min-height:30px;padding-inline:4px}.grid-footer{grid-column:1/3;align-self:center;padding-left:2px;color:#777;font-size:12px}.member-panel{display:flex;flex-direction:column;padding:7px;gap:6px}.member-title{display:flex;align-items:center;justify-content:space-between;min-height:25px}.member-title h2{margin:0;font-size:15px;font-weight:600}.member-title span{color:#777;font-size:12px}.member-layout{display:grid;grid-template-columns:minmax(240px,1fr) 82px minmax(300px,1fr);gap:7px;flex:1;min-height:0}.member-list{display:flex;flex-direction:column;min-height:0;border:1px solid #d1cdc4}.member-list h3{flex:none;margin:0;padding:5px 8px;background:#e4f2f0;font-size:13px;font-weight:600}.member-list .grid-wrap{flex:1;border:0}.member-list .grid-wrap table{min-width:480px}.move-rail{display:flex;flex-direction:column;justify-content:center;gap:8px}.move-rail .arrow{min-height:34px;padding:3px 4px}.assigned-lists{display:grid;grid-template-rows:1fr 1fr;gap:7px;min-height:0}.member-footer{display:flex;align-items:center;justify-content:space-between;min-height:34px;color:#777}.dialog-backdrop{position:fixed;z-index:60;inset:0;display:flex;align-items:center;justify-content:center;padding:18px;background:rgb(28 27 25 / 42%)}.editor-dialog{display:flex;flex-direction:column;width:min(520px,96vw);max-height:90vh;overflow:auto;border:1px solid #b9b4aa;background:#f8f6f1;box-shadow:0 12px 36px rgb(0 0 0 / 24%)}.dialog-titlebar{position:sticky;top:0;z-index:2;display:flex;align-items:center;justify-content:space-between;padding:9px 14px;border-bottom:1px solid #d2cec5;background:#f2efe8}.dialog-titlebar h2{margin:0;font-size:16px;font-weight:600}.icon-close{width:30px;height:28px;border:0;background:transparent;color:#4c4943;font-size:23px;cursor:pointer}.editor-form{display:flex;flex-direction:column;gap:9px;padding:10px}.edit-box,.audit-box{display:grid;grid-template-columns:1fr 1fr;gap:8px 12px;margin:0;border:1px solid var(--line);padding:10px}.edit-box legend,.audit-box legend{padding:0 5px;color:#5a564f}.edit-box .form-field:nth-of-type(3){grid-column:1/3}.form-field{display:flex;flex-direction:column;gap:4px;min-width:0;color:#46433d}.form-field>span{font-size:12px}.form-field input,.form-field textarea{width:100%;box-sizing:border-box}.form-field textarea{resize:vertical;min-height:68px}.form-field input.readonly,.audit-box input{background:#ecebe7}.required{color:#a33a25}.dialog-actions{display:flex;justify-content:flex-end;gap:7px;padding-top:8px;border-top:1px solid #ddd9d1}.message{margin:0;padding:6px 9px;border:1px solid;font-size:12px}.message.error{border-color:#d5a39b;background:#fbe8e5;color:#8f2a1c}.message.success{border-color:#bad0ad;background:#edf5e8;color:#34552c}@media(max-width:1000px){.hold-group-page{height:auto;min-height:100%;overflow:visible}.split-layout{grid-template-rows:minmax(260px,auto) minmax(600px,auto)}.member-layout{grid-template-columns:minmax(220px,1fr) 68px minmax(260px,1fr)}.action-rail{width:90px}.state-options{gap:7px}.filter-panel{flex-wrap:wrap}}@media(max-width:760px){.member-layout{grid-template-columns:1fr;grid-template-rows:minmax(210px,1fr) 54px minmax(400px,1.5fr)}.move-rail{flex-direction:row}.assigned-lists{grid-template-rows:1fr 1fr}.master-panel{grid-template-columns:minmax(0,1fr);grid-template-rows:minmax(220px,1fr) auto 22px}.action-rail{grid-column:1;grid-row:2;flex-direction:row;flex-wrap:wrap}.grid-footer{grid-column:1}.state-options{flex-wrap:wrap}}
</style>
