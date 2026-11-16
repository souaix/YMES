<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

type IssueState = -1 | 0 | 1 | 2
type EditorMode = 'add' | 'edit' | 'copy' | null
type LookupKind = 'mClass' | 'sClass' | 'group' | 'ocap' | null
type SearchField = 'HoldItemNo' | 'MClassNo' | 'SClassNo'
interface HoldItem {
  holdItemNo: string
  mClassNo: string
  sClassNo: string
  holdItemName: string
  description: string | null
  issueState: IssueState
  createErf: number
  holdDispGroup: string | null
  ocapNo: string | null
  ocapVersion: string | null
  holdSystem: number
  creator: string | null
  createDate: string | null
  reviser: string | null
  reviseDate: string | null
  dataStamp: number | null
}
interface ClassOption { mClassNo: string; description?: string | null }
interface SubClassOption { mClassNo: string; sClassNo: string; description?: string | null }
interface GroupOption { groupNo: string; groupName: string }
interface OcapOption { ocapNo: string; ocapVersion: string; description?: string | null }
interface LookupSet {
  mClasses: ClassOption[]
  sClasses: SubClassOption[]
  holdDispGroups: GroupOption[]
  ocaps: OcapOption[]
  holdSystems: { value: number; label: string }[]
}
interface ImportItem {
  holdItemNo: string; mClassNo: string; sClassNo: string; holdItemName: string
  createErf: number; holdSystem: number; ocapNo: string; holdDispGroup: string
  description: string
}
interface ImportResult { holdItemNo: string; holdItemName: string; result: 'success' | 'fail'; message: string }
interface MockStore { rows: HoldItem[]; lookups: LookupSet }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const useMock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes-dev-mock:qc-hold-item-basis:v1'
const issueStates: { value: IssueState; label: string }[] = [
  { value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' },
]
const searchFields: { value: SearchField; label: string }[] = [
  { value: 'HoldItemNo', label: '異常原因編號' }, { value: 'MClassNo', label: '主分類編號' }, { value: 'SClassNo', label: '次分類編號' },
]
const seedLookups: LookupSet = {
  mClasses: [{ mClassNo: 'DEV-MC-01', description: 'DEV 品質主分類一' }, { mClassNo: 'DEV-MC-02', description: 'DEV 品質主分類二' }],
  sClasses: [
    { mClassNo: 'DEV-MC-01', sClassNo: 'DEV-SC-01', description: 'DEV 外觀' },
    { mClassNo: 'DEV-MC-01', sClassNo: 'DEV-SC-02', description: 'DEV 尺寸' },
    { mClassNo: 'DEV-MC-02', sClassNo: 'DEV-SC-03', description: 'DEV 製程' },
  ],
  holdDispGroups: [{ groupNo: 'DEV-QC-GROUP-01', groupName: 'DEV 品質處理一組' }, { groupNo: 'DEV-QC-GROUP-02', groupName: 'DEV 品質處理二組' }],
  ocaps: [{ ocapNo: 'DEV-OCAP-01', ocapVersion: '01', description: 'DEV 外觀異常處理' }, { ocapNo: 'DEV-OCAP-02', ocapVersion: '02', description: 'DEV 製程異常處理' }],
  holdSystems: [{ value: 0, label: 'MES' }, { value: 1, label: 'EAP_Exception' }, { value: 2, label: 'EDA_Exception' }],
}
const seedRows: HoldItem[] = [
  { holdItemNo: 'DEV-HOLD-001', mClassNo: 'DEV-MC-01', sClassNo: 'DEV-SC-01', holdItemName: 'DEV 外觀刮傷', description: '隔離批次並通知品保確認。', issueState: 0, createErf: 1, holdDispGroup: 'DEV-QC-GROUP-01', ocapNo: 'DEV-OCAP-01', ocapVersion: '*', holdSystem: 0, creator: 'DEV001', createDate: '2026-11-10T08:00:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
  { holdItemNo: 'DEV-HOLD-002', mClassNo: 'DEV-MC-01', sClassNo: 'DEV-SC-02', holdItemName: 'DEV 尺寸超規', description: '量測結果超出允收範圍。', issueState: 1, createErf: 1, holdDispGroup: null, ocapNo: null, ocapVersion: null, holdSystem: 1, creator: 'DEV001', createDate: '2026-11-10T08:20:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-10T09:00:00+08:00', dataStamp: 1 },
  { holdItemNo: 'DEV-HOLD-003', mClassNo: 'DEV-MC-02', sClassNo: 'DEV-SC-03', holdItemName: 'DEV 製程參數異常', description: '停線並通知工程單位。', issueState: 2, createErf: 0, holdDispGroup: 'DEV-QC-GROUP-02', ocapNo: 'DEV-OCAP-02', ocapVersion: '*', holdSystem: 2, creator: 'DEV002', createDate: '2026-11-11T08:00:00+08:00', reviser: 'DEV001', reviseDate: '2026-11-11T09:00:00+08:00', dataStamp: 2 },
  { holdItemNo: 'DEV-HOLD-004', mClassNo: 'DEV-MC-02', sClassNo: 'DEV-SC-03', holdItemName: 'DEV 待停用異常', description: 'DEV 狀態範例。', issueState: -1, createErf: 1, holdDispGroup: null, ocapNo: null, ocapVersion: null, holdSystem: 0, creator: 'DEV001', createDate: '2026-11-12T08:00:00+08:00', reviser: 'DEV001', reviseDate: '2026-11-12T09:00:00+08:00', dataStamp: 3 },
]

const rows = ref<HoldItem[]>([])
const lookups = ref<LookupSet>(structuredClone(seedLookups))
const issueState = ref<IssueState>(0)
const searchField = ref<SearchField>('HoldItemNo')
const searchValue = ref('')
const appliedSearchField = ref<SearchField>('HoldItemNo')
const appliedSearchValue = ref('')
const selectedIds = ref<string[]>([])
const activeId = ref('')
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const notice = ref('')
const editorMode = ref<EditorMode>(null)
const sourceRow = ref<HoldItem | null>(null)
const editor = ref({
  holdItemNo: '', holdItemName: '', mClassNo: '', sClassNo: '', createErf: 1,
  holdSystem: 0, sendMail: false, holdDispGroup: '', ocapNo: '', description: '',
})
const editorError = ref('')
const editorNotice = ref('')
const editorBusy = ref(false)
const editorStartedAt = ref('')
const lookupKind = ref<LookupKind>(null)
const lookupSearch = ref('')
const lookupSelection = ref('')
const importOpen = ref(false)
const importFile = ref<File | null>(null)
const importItems = ref<ImportItem[]>([])
const importResults = ref<ImportResult[]>([])
const importing = ref(false)
const importError = ref('')
const importInput = ref<HTMLInputElement | null>(null)

const visibleRows = computed(() => rows.value
  .filter(row => row.issueState === issueState.value)
  .filter(row => !useMock.value || !appliedSearchValue.value.trim() ||
    String(row[searchKey(appliedSearchField.value)] ?? '').toLocaleLowerCase().includes(appliedSearchValue.value.trim().toLocaleLowerCase())))
const selectedRows = computed(() => visibleRows.value.filter(row => selectedIds.value.includes(row.holdItemNo)))
const activeRow = computed(() => rows.value.find(row => row.holdItemNo === activeId.value) ?? null)
const editorTitle = computed(() => '異常原因基本資料（' + (editorMode.value === 'add' ? '新增' : editorMode.value === 'copy' ? '複製' : '修改') + '）')
const editorReadOnly = computed(() => editorMode.value === 'edit' && sourceRow.value?.issueState === 1)
const lookupTitle = computed(() => lookupKind.value === 'mClass' ? '選擇主分類' : lookupKind.value === 'sClass' ? '選擇次分類' : lookupKind.value === 'group' ? '選擇異常處理群組' : '選擇 OCAP')
const filteredLookupRows = computed(() => {
  const term = lookupSearch.value.trim().toLocaleLowerCase()
  if (lookupKind.value === 'mClass') return lookups.value.mClasses.filter(x => !term || (x.mClassNo + ' ' + (x.description ?? '')).toLocaleLowerCase().includes(term))
  if (lookupKind.value === 'sClass') return lookups.value.sClasses.filter(x => x.mClassNo === editor.value.mClassNo && (!term || (x.sClassNo + ' ' + (x.description ?? '')).toLocaleLowerCase().includes(term)))
  if (lookupKind.value === 'group') return lookups.value.holdDispGroups.filter(x => !term || (x.groupNo + ' ' + x.groupName).toLocaleLowerCase().includes(term))
  if (lookupKind.value === 'ocap') return lookups.value.ocaps.filter(x => !term || (x.ocapNo + ' ' + x.ocapVersion + ' ' + (x.description ?? '')).toLocaleLowerCase().includes(term))
  return []
})

function searchKey(field: SearchField): 'holdItemNo' | 'mClassNo' | 'sClassNo' {
  return field === 'HoldItemNo' ? 'holdItemNo' : field === 'MClassNo' ? 'mClassNo' : 'sClassNo'
}
function now(): string { return new Date().toISOString() }
function clone<T>(value: T): T { return structuredClone(value) }
function readStore(): MockStore {
  try {
    const value = window.localStorage.getItem(storageKey)
    if (value) {
      const parsed = JSON.parse(value) as Partial<MockStore>
      if (Array.isArray(parsed.rows) && parsed.lookups) return { rows: parsed.rows, lookups: parsed.lookups }
    }
  } catch { /* reset malformed development-only data */ }
  const initial = { rows: clone(seedRows), lookups: clone(seedLookups) }
  writeStore(initial)
  return initial
}
function writeStore(store: MockStore): void { window.localStorage.setItem(storageKey, JSON.stringify(store)) }
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body && !(init.body instanceof FormData) && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init, headers })
  if (response.status === 401) { window.location.assign(appBase + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    if (response.status === 404) throw new Error('QC403 異常原因基本資料 API 尚未建立；請依 todolist_homepc.md 完成 schema 與 API。')
    throw new Error(problem?.detail || problem?.message || problem?.title || 'API 回應錯誤（' + response.status + '）。')
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
async function loadLookups(): Promise<void> {
  if (useMock.value) { lookups.value = clone(readStore().lookups); return }
  lookups.value = await request<LookupSet>('api/qc/hold-item-basis/lookups')
}
async function loadRows(): Promise<void> {
  if (useMock.value) { rows.value = clone(readStore().rows); return }
  const query = new URLSearchParams()
  if (appliedSearchValue.value.trim()) {
    query.set('searchFieldName', appliedSearchField.value)
    query.set('searchFieldValue', appliedSearchValue.value.trim())
  }
  const suffix = query.toString()
  rows.value = await request<HoldItem[]>('api/qc/hold-item-basis' + (suffix ? '?' + suffix : ''))
}
function setActive(preferred = ''): void {
  const found = visibleRows.value.find(row => row.holdItemNo === preferred) ?? visibleRows.value[0]
  activeId.value = found?.holdItemNo ?? ''
  selectedIds.value = found ? [found.holdItemNo] : []
}
async function query(): Promise<void> {
  appliedSearchField.value = searchField.value
  appliedSearchValue.value = searchValue.value
  loading.value = true; errorMessage.value = ''; notice.value = ''
  try { await loadRows(); setActive() }
  catch (error) { errorMessage.value = error instanceof Error ? error.message : '查詢異常原因失敗。' }
  finally { loading.value = false }
}
async function refreshRows(preferred = ''): Promise<void> { await loadRows(); setActive(preferred) }
function selectRow(row: HoldItem): void { activeId.value = row.holdItemNo; selectedIds.value = [row.holdItemNo] }
function toggleSelection(row: HoldItem): void {
  activeId.value = row.holdItemNo
  selectedIds.value = selectedIds.value.includes(row.holdItemNo)
    ? selectedIds.value.filter(key => key !== row.holdItemNo) : [...selectedIds.value, row.holdItemNo]
}
function setAllSelected(event: Event): void {
  const checked = (event.target as HTMLInputElement).checked
  selectedIds.value = checked ? visibleRows.value.map(row => row.holdItemNo) : []
}
function openEditor(mode: Exclude<EditorMode, null>, row: HoldItem | null = null): void {
  errorMessage.value = ''; notice.value = ''; editorError.value = ''; editorNotice.value = ''
  sourceRow.value = row ? clone(row) : null
  if (row) {
    editor.value = {
      holdItemNo: mode === 'copy' ? '' : row.holdItemNo, holdItemName: row.holdItemName,
      mClassNo: row.mClassNo, sClassNo: row.sClassNo, createErf: row.createErf,
      holdSystem: row.holdSystem ?? 0, sendMail: Boolean(row.holdDispGroup),
      holdDispGroup: row.holdDispGroup ?? '', ocapNo: row.ocapNo ?? '', description: row.description ?? '',
    }
  } else {
    editor.value = { holdItemNo: '', holdItemName: '', mClassNo: '', sClassNo: '', createErf: 1, holdSystem: 0, sendMail: false, holdDispGroup: '', ocapNo: '', description: '' }
  }
  editorStartedAt.value = mode === 'edit' ? (row?.createDate ?? '') : now()
  editorMode.value = mode
}
function closeEditor(): void { editorMode.value = null; sourceRow.value = null; editorError.value = ''; editorNotice.value = '' }
function openLookup(kind: Exclude<LookupKind, null>): void {
  if (kind === 'sClass' && !editor.value.mClassNo) { editorError.value = '請先選擇主分類。'; return }
  lookupKind.value = kind; lookupSearch.value = ''; lookupSelection.value = ''
}
function closeLookup(): void { lookupKind.value = null; lookupSearch.value = ''; lookupSelection.value = '' }
function selectLookup(row: ClassOption | SubClassOption | GroupOption | OcapOption): void {
  lookupSelection.value = 'mClassNo' in row ? row.mClassNo + (('sClassNo' in row) ? ' / ' + row.sClassNo : '') : 'groupNo' in row ? row.groupNo : row.ocapNo
}
function confirmLookup(): void {
  const row = filteredLookupRows.value.find(item => {
    const value = 'mClassNo' in item ? item.mClassNo + (('sClassNo' in item) ? ' / ' + item.sClassNo : '') : 'groupNo' in item ? item.groupNo : item.ocapNo
    return value === lookupSelection.value
  })
  if (!row) return
  if (lookupKind.value === 'mClass' && 'mClassNo' in row) { editor.value.mClassNo = row.mClassNo; editor.value.sClassNo = '' }
  else if (lookupKind.value === 'sClass' && 'sClassNo' in row) editor.value.sClassNo = row.sClassNo
  else if (lookupKind.value === 'group' && 'groupNo' in row) editor.value.holdDispGroup = row.groupNo
  else if (lookupKind.value === 'ocap' && 'ocapNo' in row) editor.value.ocapNo = row.ocapNo
  closeLookup()
}
function onHoldDispToggle(): void {
  editor.value.holdDispGroup = ''
}
function validateEditor(): string {
  const value = editor.value
  value.holdItemNo = value.holdItemNo.trim().toLocaleUpperCase()
  if (!value.holdItemNo) return '異常原因編號為必填欄位。'
  if (!value.holdItemName.trim()) return '異常原因名稱為必填欄位。'
  if (!value.mClassNo) return '主分類編號為必填欄位。'
  if (!value.sClassNo) return '次分類編號為必填欄位。'
  if (value.sendMail && !value.holdDispGroup) return '勾選寄送通知時，必須選擇異常處理群組。'
  if (value.ocapNo && !lookups.value.ocaps.some(x => x.ocapNo === value.ocapNo)) return 'OCAP 編號不存在或尚未簽核。'
  if (value.holdItemNo.length > 50 || value.holdItemName.length > 100 || value.mClassNo.length > 30 || value.sClassNo.length > 30) return '欄位長度超過資料表限制。'
  if (value.description.length > 255 || value.holdDispGroup.length > 50 || value.ocapNo.length > 50) return '欄位長度超過資料表限制。'
  if (!lookups.value.holdSystems.some(x => x.value === Number(value.holdSystem))) return 'HoldSystem 選項無效。'
  if (editorReadOnly.value) return '簽核中的資料不可修改。'
  return ''
}
function toPayload(): Omit<HoldItem, 'creator' | 'createDate' | 'reviser' | 'reviseDate' | 'issueState' | 'dataStamp'> {
  return {
    holdItemNo: editor.value.holdItemNo.trim().toLocaleUpperCase(), holdItemName: editor.value.holdItemName.trim(),
    mClassNo: editor.value.mClassNo, sClassNo: editor.value.sClassNo, description: editor.value.description.trim() || null,
    createErf: Number(editor.value.createErf), holdDispGroup: editor.value.sendMail ? editor.value.holdDispGroup : null,
    ocapNo: editor.value.ocapNo || null, ocapVersion: editor.value.ocapNo ? '*' : null, holdSystem: Number(editor.value.holdSystem),
  }
}
async function saveEditor(keepOpen: boolean): Promise<void> {
  editorError.value = validateEditor(); editorNotice.value = ''
  if (editorError.value) return
  const mode = editorMode.value
  const original = sourceRow.value
  if (!mode) return
  const payload = toPayload()
  editorBusy.value = true; errorMessage.value = ''; notice.value = ''
  try {
    if (useMock.value) {
      const store = readStore()
      if (mode !== 'edit' && store.rows.some(row => row.holdItemNo.toLocaleUpperCase() === payload.holdItemNo)) throw new Error('異常原因編號已存在。')
      if (mode === 'edit' && original) {
        const index = store.rows.findIndex(row => row.holdItemNo === original.holdItemNo)
        if (index < 0) throw new Error('資料已不存在，請重新查詢。')
        store.rows[index] = { ...store.rows[index], holdItemName: payload.holdItemName, description: payload.description, createErf: payload.createErf, holdDispGroup: payload.holdDispGroup, ocapNo: payload.ocapNo, ocapVersion: payload.ocapVersion, holdSystem: payload.holdSystem, reviser: props.currentUserNo || 'DEV001', reviseDate: now(), dataStamp: (store.rows[index].dataStamp ?? 0) + 1 }
      } else {
        store.rows.push({ ...payload, issueState: 0, creator: props.currentUserNo || 'DEV001', createDate: now(), reviser: null, reviseDate: null, dataStamp: 0 })
      }
      writeStore(store)
    } else if (mode === 'edit' && original) {
      await request('api/qc/hold-item-basis/' + encodeURIComponent(original.holdItemNo) + '?issueState=' + original.issueState + '&dataStamp=' + (original.dataStamp ?? ''), { method: 'PUT', body: JSON.stringify(payload) })
    } else {
      await request('api/qc/hold-item-basis', { method: 'POST', body: JSON.stringify(payload) })
    }
    await refreshRows(payload.holdItemNo)
    if (keepOpen) {
      sourceRow.value = mode === 'edit' ? clone(rows.value.find(row => row.holdItemNo === payload.holdItemNo) ?? original!) : null
      if (mode !== 'edit') editor.value = { holdItemNo: '', holdItemName: '', mClassNo: '', sClassNo: '', createErf: 1, holdSystem: 0, sendMail: false, holdDispGroup: '', ocapNo: '', description: '' }
      editorStartedAt.value = mode === 'edit' ? (sourceRow.value?.createDate ?? '') : now()
      editorNotice.value = mode === 'edit' ? '資料已套用。' : '資料已新增，可繼續輸入。'
    } else {
      closeEditor()
      notice.value = mode === 'edit' ? '異常原因資料已修改。' : mode === 'copy' ? '異常原因資料已複製。' : '異常原因資料已新增。'
    }
  } catch (error) { editorError.value = error instanceof Error ? error.message : '儲存異常原因資料失敗。' }
  finally { editorBusy.value = false }
}
async function deleteSelected(): Promise<void> {
  const targets = [...selectedRows.value]
  if (!targets.length) { errorMessage.value = '請先選取要刪除的資料。'; return }
  if (targets.some(row => row.issueState === 1)) { errorMessage.value = '簽核中的資料不可刪除。'; return }
  if (!window.confirm('確定刪除選取的 ' + targets.length + ' 筆異常原因？已簽核資料會改為不使用。')) return
  busy.value = true; errorMessage.value = ''; notice.value = ''; let completed = 0
  try {
    for (const row of targets) {
      if (useMock.value) {
        const store = readStore()
        if (row.issueState === 2) {
          const index = store.rows.findIndex(item => item.holdItemNo === row.holdItemNo)
          if (index >= 0) store.rows[index] = { ...store.rows[index], issueState: -1, reviser: props.currentUserNo || 'DEV001', reviseDate: now(), dataStamp: (store.rows[index].dataStamp ?? 0) + 1 }
        } else store.rows = store.rows.filter(item => item.holdItemNo !== row.holdItemNo)
        writeStore(store)
      } else {
        await request('api/qc/hold-item-basis/' + encodeURIComponent(row.holdItemNo) + '?issueState=' + row.issueState + '&dataStamp=' + (row.dataStamp ?? ''), { method: 'DELETE' })
      }
      completed++
    }
    await refreshRows(); notice.value = '已處理 ' + completed + ' 筆異常原因資料。'
  } catch (error) { errorMessage.value = (error instanceof Error ? error.message : '刪除異常原因失敗。') + '（已完成 ' + completed + ' 筆）'; await refreshRows() }
  finally { busy.value = false }
}
async function approveSelected(): Promise<void> {
  const targets = [...selectedRows.value]
  if (!targets.length) { errorMessage.value = '請先選取要核准的資料。'; return }
  if (targets.some(row => row.issueState !== 0)) { errorMessage.value = '只有未簽核資料可以送出核准。'; return }
  if (!window.confirm('確定核准選取的 ' + targets.length + ' 筆異常原因？')) return
  busy.value = true; errorMessage.value = ''; notice.value = ''; let completed = 0
  try {
    for (const row of targets) {
      if (useMock.value) {
        const store = readStore(); const index = store.rows.findIndex(item => item.holdItemNo === row.holdItemNo)
        if (index >= 0) store.rows[index] = { ...store.rows[index], issueState: 2, reviser: props.currentUserNo || 'DEV001', reviseDate: now(), dataStamp: (store.rows[index].dataStamp ?? 0) + 1 }
        writeStore(store)
      } else {
        await request('api/qc/hold-item-basis/' + encodeURIComponent(row.holdItemNo) + '/approve', { method: 'POST', body: JSON.stringify({ issueState: row.issueState, dataStamp: row.dataStamp, creator: props.currentUserNo || '' }) })
      }
      completed++
    }
    await refreshRows(); notice.value = '已送出 ' + completed + ' 筆異常原因核准。'
  } catch (error) { errorMessage.value = (error instanceof Error ? error.message : '核准異常原因失敗。') + '（已完成 ' + completed + ' 筆）'; await refreshRows() }
  finally { busy.value = false }
}
function openImport(): void { importOpen.value = true; importFile.value = null; importItems.value = []; importResults.value = []; importError.value = '' }
function closeImport(): void { importOpen.value = false; importFile.value = null; importItems.value = []; importResults.value = []; importError.value = '' }
function downloadTemplate(): void {
  const content = '\uFEFFHOLDITEMNO,MCLASSNO,SCLASSNO,HOLDITEMNAME,CREATEERF,HOLDSYSTEM,OCAPNO,HOLDDISPGROUP,DESCRIPTION\r\n'
  const url = URL.createObjectURL(new Blob([content], { type: 'text/csv;charset=utf-8' }))
  const anchor = document.createElement('a'); anchor.href = url; anchor.download = 'HoldItemTemplate.csv'; anchor.click(); URL.revokeObjectURL(url)
}
function parseCsv(text: string): string[][] {
  const result: string[][] = []; let row: string[] = []; let cell = ''; let quoted = false
  for (let i = 0; i < text.length; i++) {
    const char = text[i]
    if (quoted && char === '"' && text[i + 1] === '"') { cell += '"'; i++ }
    else if (char === '"') quoted = !quoted
    else if (!quoted && char === ',') { row.push(cell); cell = '' }
    else if (!quoted && (char === '\n' || char === '\r')) {
      if (char === '\r' && text[i + 1] === '\n') i++
      row.push(cell); cell = ''
      if (row.some(value => value.trim())) result.push(row)
      row = []
    } else cell += char
  }
  row.push(cell); if (row.some(value => value.trim())) result.push(row)
  return result
}
async function chooseImportFile(event: Event): Promise<void> {
  importError.value = ''; importResults.value = []; importItems.value = []
  const file = (event.target as HTMLInputElement).files?.[0] ?? null
  importFile.value = file
  if (!file) return
  if (useMock.value && !file.name.toLocaleLowerCase().endsWith('.csv')) {
    importFile.value = null; importError.value = '公司端假資料預覽目前支援 CSV；正式 API 依 WinForms 支援 .xls / .xlsx。'; return
  }
  if (!useMock.value) return
  try {
    const parsed = parseCsv(await file.text())
    if (parsed.length < 2) throw new Error('檔案沒有可匯入資料。')
    const items: ImportItem[] = parsed.slice(1).map(row => {
      const systemText = (row[5] ?? '').trim()
      const systemMap: Record<string, number> = { MES: 0, EAP_Exception: 1, EDA_Exception: 2 }
      return {
        holdItemNo: (row[0] ?? '').trim().toLocaleUpperCase(), mClassNo: (row[1] ?? '').trim().toLocaleUpperCase(),
        sClassNo: (row[2] ?? '').trim().toLocaleUpperCase(), holdItemName: (row[3] ?? '').trim().toLocaleUpperCase(),
        createErf: (row[4] ?? '').trim() === '' ? Number.NaN : Number((row[4] ?? '').trim()),
        holdSystem: systemText === '' ? 0 : (systemMap[systemText] ?? -1),
        ocapNo: (row[6] ?? '').trim().toLocaleUpperCase(), holdDispGroup: (row[7] ?? '').trim().toLocaleUpperCase(),
        description: (row[8] ?? '').trim(),
      }
    })
    importItems.value = items
  } catch (error) { importError.value = error instanceof Error ? error.message : '讀取匯入檔案失敗。'; importFile.value = null }
}
function validateImportItem(item: ImportItem, existing: HoldItem[], seen: Set<string>): string {
  if (!item.holdItemNo || !item.mClassNo || !item.sClassNo || !item.holdItemName || !Number.isInteger(item.createErf)) return '必填欄位不足或 CreateERF 格式錯誤。'
  if (item.holdItemNo.length > 50 || item.mClassNo.length > 30 || item.sClassNo.length > 30 || item.holdItemName.length > 100) return '欄位長度超過資料表限制。'
  if (item.createErf < -9 || item.createErf > 9) return 'CreateERF 超出 NUMBER(1,0) 範圍。'
  if (![0, 1, 2].includes(item.holdSystem)) return 'HoldSystem 僅接受 MES、EAP_Exception 或 EDA_Exception。'
  if (existing.some(row => row.holdItemNo.toLocaleUpperCase() === item.holdItemNo) || seen.has(item.holdItemNo)) return '異常原因編號已存在。'
  if (!lookups.value.mClasses.some(row => row.mClassNo === item.mClassNo)) return '主分類不存在或尚未簽核。'
  if (!lookups.value.sClasses.some(row => row.sClassNo === item.sClassNo)) return '次分類不存在或尚未簽核。'
  if (item.holdDispGroup && !lookups.value.holdDispGroups.some(row => row.groupNo === item.holdDispGroup)) return '異常處理群組不存在。'
  if (item.ocapNo && !lookups.value.ocaps.some(row => row.ocapNo === item.ocapNo)) return 'OCAP 不存在或尚未簽核。'
  return ''
}
async function startImport(): Promise<void> {
  if (!importFile.value) { importError.value = '請先選擇匯入檔案。'; return }
  importing.value = true; importError.value = ''; importResults.value = []
  try {
    if (!useMock.value) {
      const data = new FormData(); data.append('file', importFile.value)
      const response = await request<{ results: ImportResult[] }>('api/qc/hold-item-basis/import', { method: 'POST', body: data })
      importResults.value = response.results
      if (response.results.some(item => item.result === 'success')) { await refreshRows(); notice.value = '異常原因匯入完成。' }
      return
    }
    const store = readStore(); const seen = new Set<string>(); const results: ImportResult[] = []
    for (const item of importItems.value) {
      const error = validateImportItem(item, store.rows, seen)
      if (error) { results.push({ holdItemNo: item.holdItemNo, holdItemName: item.holdItemName, result: 'fail', message: error }); continue }
      seen.add(item.holdItemNo)
      store.rows.push({
        holdItemNo: item.holdItemNo, mClassNo: item.mClassNo, sClassNo: item.sClassNo, holdItemName: item.holdItemName,
        description: item.description || null, issueState: 0, createErf: item.createErf, holdDispGroup: item.holdDispGroup || null,
        ocapNo: item.ocapNo || null, ocapVersion: item.ocapNo ? '*' : null, holdSystem: item.holdSystem,
        creator: props.currentUserNo || 'DEV001', createDate: now(), reviser: null, reviseDate: null, dataStamp: 0,
      })
      results.push({ holdItemNo: item.holdItemNo, holdItemName: item.holdItemName, result: 'success', message: '匯入成功' })
    }
    writeStore(store); importResults.value = results
    if (results.some(item => item.result === 'success')) { await refreshRows(); notice.value = '異常原因匯入完成。' }
  } catch (error) { importError.value = error instanceof Error ? error.message : '匯入異常原因失敗。' }
  finally { importing.value = false }
}
function formatDate(value: string | null): string { return value ? new Date(value).toLocaleString('zh-TW', { hour12: false }) : '' }
function holdSystemLabel(value: number): string { return lookups.value.holdSystems.find(x => x.value === Number(value))?.label ?? String(value) }
function closePage(): void { emit('close') }

watch(issueState, () => setActive())
onMounted(() => {
  void (async () => {
    loading.value = true
    try { await loadLookups(); await loadRows(); setActive() }
    catch (error) { errorMessage.value = error instanceof Error ? error.message : '載入異常原因資料失敗。' }
    finally { loading.value = false }
  })()
})
</script>

<template>
  <main class="hold-item-page">
    <header class="page-heading">
      <div><h1>異常原因基本資料</h1><p>QC403 · HOLDITEM BASIS</p></div>
      <span v-if="useMock" class="dev-badge">DEV 假資料</span>
    </header>
    <form class="query-panel" @submit.prevent="query">
      <div class="state-options">
        <label v-for="state in issueStates" :key="state.value" class="state-option">
          <input v-model="issueState" type="radio" :value="state.value" :disabled="loading || busy">
          <span>{{ state.label }}</span>
        </label>
      </div>
      <label class="search-field"><span>查詢欄位</span>
        <select v-model="searchField" :disabled="loading || busy">
          <option v-for="field in searchFields" :key="field.value" :value="field.value">{{ field.label }}</option>
        </select>
      </label>
      <input v-model="searchValue" class="search-input" aria-label="查詢文字" :disabled="loading || busy" @keyup.enter="query">
      <button class="button query-button" type="submit" :disabled="loading || busy">{{ loading ? '查詢中' : '查詢' }}</button>
    </form>
    <p v-if="errorMessage" class="message error-message" role="alert">{{ errorMessage }}</p>
    <p v-if="notice" class="message notice-message" role="status">{{ notice }}</p>
    <section class="grid-panel">
      <div class="table-scroll">
        <table class="data-grid">
          <thead><tr>
            <th class="check-column"><input type="checkbox" aria-label="全選目前資料" :checked="visibleRows.length > 0 && selectedRows.length === visibleRows.length" @change="setAllSelected"></th>
            <th>HoldItemNo</th><th>MClassNo</th><th>SClassNo</th><th>HoldItemName</th><th>CreateERF</th><th>OCAP No</th><th>HoldSystem</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th>
          </tr></thead>
          <tbody>
            <tr v-for="row in visibleRows" :key="row.holdItemNo" :class="{ 'active-row': activeId === row.holdItemNo }" @click="selectRow(row)" @dblclick="openEditor('edit', row)">
              <td class="check-column" @click.stop><input type="checkbox" :aria-label="'選取 ' + row.holdItemNo" :checked="selectedIds.includes(row.holdItemNo)" @change="toggleSelection(row)"></td>
              <td class="key-cell">{{ row.holdItemNo }}</td><td>{{ row.mClassNo }}</td><td>{{ row.sClassNo }}</td><td>{{ row.holdItemName }}</td>
              <td>{{ Number(row.createErf) === 1 ? 'Create' : 'Not Create' }}</td><td>{{ row.ocapNo || '' }}</td><td>{{ holdSystemLabel(row.holdSystem) }}</td>
              <td class="multiline-cell">{{ row.description || '' }}</td><td>{{ row.creator || '' }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser || '' }}</td><td>{{ formatDate(row.reviseDate) }}</td>
            </tr>
            <tr v-if="!visibleRows.length" class="empty-row"><td colspan="13">{{ loading ? '資料載入中…' : '查無資料' }}</td></tr>
          </tbody>
        </table>
      </div>
      <aside class="action-rail">
        <button class="button primary" type="button" :disabled="busy || loading" @click="openEditor('add')">新增(A)</button>
        <button class="button" type="button" :disabled="busy || loading || !activeRow" @click="activeRow && openEditor('edit', activeRow)">修改(E)</button>
        <button class="button" type="button" :disabled="busy || loading || !activeRow" @click="activeRow && openEditor('copy', activeRow)">複製(C)</button>
        <button class="button" type="button" :disabled="busy || loading || !selectedRows.length" @click="approveSelected">核准</button>
        <button class="button" type="button" :disabled="busy || loading || !selectedRows.length" @click="deleteSelected">刪除(D)</button>
        <button class="button" type="button" :disabled="busy || loading" @click="openImport">匯入</button>
        <button class="button close-button" type="button" @click="closePage">關閉</button>
      </aside>
      <footer class="grid-footer"><span>資料總筆數：{{ visibleRows.length }}</span><span v-if="busy">資料處理中…</span></footer>
    </section>

    <div v-if="editorMode" class="dialog-backdrop" @click.self="closeEditor">
      <section class="editor-dialog" role="dialog" aria-modal="true" :aria-label="editorTitle">
        <header class="dialog-titlebar"><h2>{{ editorTitle }}</h2><button class="icon-close" type="button" aria-label="關閉" @click="closeEditor">×</button></header>
        <form class="editor-form" @submit.prevent="saveEditor(false)">
          <fieldset class="identity-panel"><legend>異常原因基本資料</legend>
            <label class="wide-field"><span>異常原因編號<span class="required">*</span></span><input v-model="editor.holdItemNo" maxlength="50" :readonly="editorMode === 'edit' || editorReadOnly" :disabled="editorReadOnly" class="kd-field"></label>
          </fieldset>
          <div class="editor-columns">
            <fieldset class="detail-panel"><legend>基本資料</legend>
              <label class="wide-field"><span>異常原因名稱<span class="required">*</span></span><input v-model="editor.holdItemName" maxlength="100" :readonly="editorReadOnly" class="kd-field"></label>
              <label class="wide-field"><span>主分類編號<span class="required">*</span></span><span class="picker-control"><input v-model="editor.mClassNo" readonly class="kd-field"><button class="button picker-button" type="button" :disabled="editorReadOnly" @click="openLookup('mClass')">…</button></span></label>
              <label class="wide-field"><span>次分類編號<span class="required">*</span></span><span class="picker-control"><input v-model="editor.sClassNo" readonly class="kd-field"><button class="button picker-button" type="button" :disabled="editorReadOnly || !editor.mClassNo" @click="openLookup('sClass')">…</button></span></label>
              <label class="check-field"><input v-model="editor.createErf" type="checkbox" :true-value="1" :false-value="0" :disabled="editorReadOnly"><span>Create ERF Process</span></label>
              <label class="wide-field"><span>OCAPNo</span><span class="picker-control"><input v-model="editor.ocapNo" readonly class="kd-field"><button class="button picker-button" type="button" :disabled="editorReadOnly" @click="openLookup('ocap')">…</button></span></label>
              <label class="check-field"><input v-model="editor.sendMail" type="checkbox" :disabled="editorReadOnly" @change="onHoldDispToggle"><span>Send Mail to HoldDispGroup</span></label>
            </fieldset>
            <fieldset class="detail-panel">
              <legend>通知與分類</legend>
              <label class="wide-field"><span>HoldSystem<span class="required">*</span></span>
                <select v-model.number="editor.holdSystem" :disabled="editorReadOnly" class="kd-field">
                  <option v-for="item in lookups.holdSystems" :key="item.value" :value="item.value">{{ item.label }}</option>
                </select>
              </label>
              <label class="wide-field"><span>HoldDispGroup</span><span class="picker-control"><input v-model="editor.holdDispGroup" readonly class="kd-field" :disabled="!editor.sendMail || editorReadOnly"><button class="button picker-button" type="button" :disabled="!editor.sendMail || editorReadOnly" @click="openLookup('group')">…</button></span></label>
              <label class="wide-field description-field"><span>Description</span><textarea v-model="editor.description" maxlength="255" :readonly="editorReadOnly" class="kd-field"></textarea></label>
            </fieldset>
          </div>
          <fieldset class="audit-panel"><legend>建立/修改資訊</legend>
            <label class="form-field"><span>建立人</span><input :value="editorMode === 'edit' ? (sourceRow?.creator || '') : (props.currentUserNo || 'DEV001')" readonly></label>
            <label class="form-field"><span>建立日期</span><input :value="formatDate(editorStartedAt)" readonly></label>
            <label class="form-field"><span>修改人</span><input :value="editorMode === 'edit' ? (sourceRow?.reviser || '') : ''" readonly></label>
            <label class="form-field"><span>修改日期</span><input :value="editorMode === 'edit' ? formatDate(sourceRow?.reviseDate ?? null) : ''" readonly></label>
          </fieldset>
          <p v-if="editorError" class="message error-message" role="alert">{{ editorError }}</p>
          <p v-if="editorNotice" class="message notice-message" role="status">{{ editorNotice }}</p>
        </form>
        <footer class="dialog-actions">
          <button class="button primary" type="button" :disabled="editorBusy || editorReadOnly" @click="saveEditor(false)">確定</button>
          <button class="button" type="button" @click="closeEditor">關閉</button>
          <button class="button" type="button" :disabled="editorBusy || editorReadOnly" @click="saveEditor(true)">套用</button>
        </footer>
      </section>
    </div>

    <div v-if="lookupKind" class="dialog-backdrop lookup-backdrop" @click.self="closeLookup">
      <section class="picker-dialog" role="dialog" aria-modal="true" :aria-label="lookupTitle">
        <header class="dialog-titlebar"><h2>{{ lookupTitle }}</h2><button class="icon-close" type="button" aria-label="關閉" @click="closeLookup">×</button></header>
        <div class="picker-search"><label for="qc403-lookup-search">查詢</label><input id="qc403-lookup-search" v-model="lookupSearch" class="kd-field"><span v-if="lookupKind === 'sClass'">限主分類 {{ editor.mClassNo }}</span></div>
        <div class="table-scroll picker-table"><table class="data-grid">
          <thead><tr><th>編號</th><th>名稱/說明</th></tr></thead>
          <tbody>
            <tr v-for="row in filteredLookupRows" :key="'sClassNo' in row ? row.mClassNo + '|' + row.sClassNo : 'mClassNo' in row ? row.mClassNo : 'groupNo' in row ? row.groupNo : row.ocapNo" :class="{ 'active-row': lookupSelection === ('mClassNo' in row ? row.mClassNo + (('sClassNo' in row) ? ' / ' + row.sClassNo : '') : 'groupNo' in row ? row.groupNo : row.ocapNo) }" @click="selectLookup(row)" @dblclick="confirmLookup">
              <td>{{ 'mClassNo' in row ? row.mClassNo + (('sClassNo' in row) ? ' / ' + row.sClassNo : '') : 'groupNo' in row ? row.groupNo : row.ocapNo }}</td>
              <td>{{ 'groupName' in row ? row.groupName : 'description' in row ? row.description : '' }}</td>
            </tr>
            <tr v-if="!filteredLookupRows.length" class="empty-row"><td colspan="2">查無可選資料</td></tr>
          </tbody>
        </table></div>
        <footer class="dialog-actions"><button class="button primary" type="button" :disabled="!lookupSelection" @click="confirmLookup">選擇</button><button class="button" type="button" @click="closeLookup">關閉</button></footer>
      </section>
    </div>

    <div v-if="importOpen" class="dialog-backdrop" @click.self="closeImport">
      <section class="import-dialog" role="dialog" aria-modal="true" aria-label="異常原因資料匯入">
        <header class="dialog-titlebar"><h2>異常原因資料匯入</h2><button class="icon-close" type="button" aria-label="關閉" @click="closeImport">×</button></header>
        <div class="import-toolbar">
          <input ref="importInput" class="file-input" type="file" accept=".csv,.xls,.xlsx" @change="chooseImportFile">
          <button class="button" type="button" @click="importInput?.click()">選擇檔案</button>
          <span class="file-name">{{ importFile?.name || '尚未選擇檔案' }}</span>
          <button class="button" type="button" @click="downloadTemplate">下載範本</button>
        </div>
        <p v-if="useMock" class="import-hint">DEV 模式以 CSV 假資料預覽；正式 API 接收 WinForms 的 .xls / .xlsx，讀取第一個工作表。</p>
        <p v-if="importError" class="message error-message" role="alert">{{ importError }}</p>
        <div class="table-scroll import-table"><table class="data-grid">
          <thead><tr><th>HoldItemNo</th><th>HoldItemName</th><th>結果</th><th>Message</th></tr></thead>
          <tbody>
            <tr v-for="(item, index) in importResults" :key="index"><td>{{ item.holdItemNo }}</td><td>{{ item.holdItemName }}</td><td :class="item.result === 'success' ? 'success-text' : 'failure-text'">{{ item.result === 'success' ? '成功' : '失敗' }}</td><td>{{ item.message }}</td></tr>
            <tr v-if="!importResults.length && importItems.length"><td colspan="4">已讀取 {{ importItems.length }} 筆；按「匯入」開始逐筆檢查。</td></tr>
            <tr v-if="!importResults.length && !importItems.length"><td colspan="4">選擇範本檔後顯示每筆異常原因的匯入結果。</td></tr>
          </tbody>
        </table></div>
        <footer class="dialog-actions">
          <button class="button primary" type="button" :disabled="importing || !importFile" @click="startImport">{{ importing ? '匯入中…' : '匯入' }}</button>
          <button class="button" type="button" :disabled="importing" @click="closeImport">關閉</button>
        </footer>
      </section>
    </div>
  </main>
</template>

<style scoped>
.hold-item-page{--line:#c8c4bb;--text:#30302e;--muted:#77746e;--field:#fff0ad;--orange:#f7a32b;display:flex;flex-direction:column;gap:8px;height:100%;min-height:0;padding:12px;color:var(--text);background:#f7f5ef;font:14px/1.35 "Segoe UI","Microsoft JhengHei",sans-serif}
.page-heading{display:flex;align-items:center;justify-content:space-between;min-height:42px}.page-heading h1{margin:0;font-size:20px;font-weight:600}.page-heading p{margin:2px 0 0;color:var(--muted);font-size:12px}.dev-badge{padding:4px 9px;border:1px solid #d7a044;border-radius:3px;background:#fff1bd;color:#77551a;font-size:12px}
.query-panel{display:flex;flex-wrap:wrap;align-items:center;gap:10px;min-height:46px;padding:6px 10px;border:1px solid var(--line);background:#fff}.state-options{display:flex;flex-wrap:wrap;align-items:center;gap:12px}.state-option,.search-field{display:inline-flex;align-items:center;gap:5px;white-space:nowrap}.state-option{cursor:pointer}.state-option input{accent-color:#e18b17}.search-field select{min-width:130px}.search-input{width:190px}
input,select,textarea{box-sizing:border-box;min-height:30px;padding:5px 7px;border:1px solid #a9a69f;border-radius:2px;background:var(--field);color:var(--text);font:inherit}input:focus,select:focus,textarea:focus{border-color:#d18a21;outline:2px solid rgb(209 138 33 / 20%)}input[readonly],input:disabled,select:disabled,textarea[readonly]{background:#ecebe7;color:#777}textarea{resize:vertical}
.button{min-height:30px;padding:4px 11px;border:1px solid #bdb9b0;border-radius:3px;background:linear-gradient(#fff,#f0efec);color:#343331;font:inherit;white-space:nowrap;cursor:pointer}.button:hover:not(:disabled){border-color:#d08a23;background:#fff6df}.button:disabled{opacity:.48;cursor:not-allowed}.button.primary{border-color:#df8612;background:var(--orange);color:#fff}.button.primary:hover:not(:disabled){background:#df8612}.query-button{min-width:58px}
.message{flex:0 0 auto;margin:0;padding:7px 10px;border:1px solid;border-radius:2px}.error-message{border-color:#d99b86;background:#fff0e9;color:#873820}.notice-message{border-color:#c9bc91;background:#fff8dc;color:#67551d}
.grid-panel{display:grid;grid-template-columns:minmax(0,1fr) 88px;grid-template-rows:minmax(0,1fr) auto;gap:7px 9px;flex:1 1 auto;min-height:220px;padding:6px;border:1px solid var(--line);background:#fff}.table-scroll{min-width:0;min-height:0;overflow:auto;border:1px solid #c9c6bf}.data-grid{width:100%;border-collapse:separate;border-spacing:0;font-size:13px;white-space:nowrap}.data-grid th{position:sticky;top:0;z-index:1;padding:6px 8px;border-right:1px solid #e1ded8;border-bottom:1px solid #c9c6bf;background:#f5cd64;color:#39342a;text-align:left;font-weight:600}.data-grid td{height:27px;padding:3px 7px;border-right:1px solid #e1ded8;border-bottom:1px solid #e1ded8}.data-grid tbody tr{cursor:pointer}.data-grid tbody tr:hover{background:#fff8e3}.data-grid tbody tr.active-row{background:#ffedaa}.data-grid input[type=checkbox]{width:15px;min-height:15px;height:15px;margin:0;accent-color:#d98717;vertical-align:middle}.check-column{width:28px;min-width:28px;text-align:center!important}.key-cell{font-weight:600}.multiline-cell{min-width:170px;max-width:300px;white-space:pre-wrap;overflow-wrap:anywhere}.empty-row{height:48px!important;color:#777;text-align:center;cursor:default}
.action-rail{display:flex;flex-direction:column;gap:7px}.action-rail .button{min-height:32px}.close-button{margin-top:auto}.grid-footer{grid-column:1/-1;display:flex;gap:14px;min-height:20px;padding:3px 2px 0;color:var(--muted);font-size:12px}
.dialog-backdrop{position:fixed;z-index:50;inset:0;display:flex;align-items:center;justify-content:center;padding:18px;background:rgb(28 27 25 / 42%)}.editor-dialog,.picker-dialog,.import-dialog{display:flex;flex-direction:column;width:min(760px,97vw);max-height:min(94vh,760px);overflow:auto;border:1px solid #b9b4aa;background:#f8f6f1;box-shadow:0 12px 36px rgb(0 0 0 / 24%)}.dialog-titlebar{position:sticky;top:0;z-index:2;display:flex;align-items:center;justify-content:space-between;padding:10px 15px;border-bottom:1px solid #d2cec5;background:#f2efe8}.dialog-titlebar h2{margin:0;font-size:17px;font-weight:600}.icon-close{width:30px;height:28px;border:0;background:transparent;color:#4c4943;font-size:23px;cursor:pointer}
.editor-form{display:flex;flex-direction:column;gap:9px;padding:12px}.identity-panel,.detail-panel,.audit-panel{min-width:0;border:1px solid #c9c4ba;padding:9px}.identity-panel legend,.detail-panel legend,.audit-panel legend{padding:0 5px;color:#5a564f}.wide-field,.form-field{display:flex;flex-direction:column;gap:5px;min-width:0;color:#46433d}.wide-field>span:first-child,.form-field>span{font-size:13px}.identity-panel .wide-field{max-width:440px}.kd-field{background:var(--field)}.editor-columns{display:grid;grid-template-columns:1fr 1fr;gap:10px}.detail-panel{display:flex;flex-direction:column;gap:9px}.picker-control{display:flex;gap:6px}.picker-control input{flex:1;min-width:0}.picker-button{width:38px}.check-field{display:flex;align-items:center;gap:7px;min-height:30px}.check-field input{width:16px;min-height:16px;height:16px;accent-color:#d98717}.description-field textarea{min-height:95px}.audit-panel{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:8px 12px}.audit-panel .form-field input{width:100%;background:#ecebe7}.required{padding-left:3px;color:#a33a25}.dialog-actions{display:flex;justify-content:flex-end;gap:8px;padding:7px 12px 12px;border-top:1px solid #ddd9d1}.editor-dialog>.dialog-actions{position:sticky;bottom:0;background:#f8f6f1}.picker-dialog{width:min(620px,96vw)}.picker-search{display:flex;align-items:center;gap:10px;padding:10px 14px}.picker-search input{flex:1}.picker-table{min-height:190px;max-height:50vh;margin:0 14px 12px}
.import-dialog{width:min(900px,97vw);height:min(650px,90vh)}.import-toolbar{display:flex;align-items:center;gap:8px;padding:10px 14px}.file-input{position:absolute;width:1px;height:1px;overflow:hidden;opacity:0}.file-name{flex:1;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.import-hint{margin:0;padding:0 14px 9px;color:#68645c;font-size:12px}.import-table{flex:1;margin:0 14px 12px}.success-text{color:#28643d}.failure-text{color:#a33a25}
@media(max-width:900px){.hold-item-page{height:auto;min-height:100%}.grid-panel{grid-template-columns:1fr;grid-template-rows:minmax(340px,1fr) auto auto}.action-rail{flex-direction:row;flex-wrap:wrap}.close-button{margin:0 0 0 auto}.grid-footer{grid-column:1}}
@media(max-width:620px){.query-panel{align-items:flex-start}.state-options{width:100%}.editor-columns,.audit-panel{grid-template-columns:1fr}.grid-footer{flex-direction:column;gap:2px}}
</style>
