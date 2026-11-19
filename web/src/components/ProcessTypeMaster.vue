<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'

type IssueState = -1 | 0 | 1 | 2
type EditMode = '' | 'add' | 'edit' | 'copy'

interface ProcessTypeRow {
  processType: string
  description: string | null
  issueState: IssueState | null
  dataStamp: number | null
  creator: string | null
  createDate: string | null
  reviser: string | null
  reviseDate: string | null
}

interface ProcessPropertyRow {
  processNo: string
  processVersion: string
  propertyNo: string
  defaultValue: string
  propertySequence: number
  description: string
  issueState: IssueState
  dataStamp: number | null
}

interface Envelope<T> {
  items?: T[]
  rows?: T[]
  data?: T[]
  detail?: string
  message?: string
  title?: string
}

const props = withDefaults(defineProps<{
  apiBaseUrl: string
  currentUserNo?: string
  developmentPreview?: boolean
}>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()

const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const listStorageKey = 'ymes.dev.ps103-process-type.v1'
const propertyStorageKey = 'ymes.dev.ps103-process-type-properties.v1'
const states: Array<{ value: IssueState; label: string }> = [
  { value: 0, label: '未核准' },
  { value: 1, label: '簽核中' },
  { value: 2, label: '已核准' },
  { value: -1, label: '不使用' },
]
const propertyChoices = ['DEV-PROC-TEMP', 'DEV-PROC-ROUTE', 'DEV-PROC-REMARK']

const seedRows: ProcessTypeRow[] = [
  { processType: 'DEV-STANDARD', description: 'DEV 標準製程類別', issueState: 0, dataStamp: 2, creator: 'DEV001', createDate: '2026-11-15T09:00:00', reviser: 'DEV001', reviseDate: '2026-11-17T15:20:00' },
  { processType: 'DEV-OUTSOURCE', description: 'DEV 外包製程類別', issueState: 2, dataStamp: 4, creator: 'DEV002', createDate: '2026-10-22T14:30:00', reviser: 'DEV002', reviseDate: '2026-11-01T10:10:00' },
  { processType: 'DEV-REVIEW', description: 'DEV 待簽核樣本', issueState: 1, dataStamp: 1, creator: 'DEV003', createDate: '2026-11-17T11:45:00', reviser: 'DEV003', reviseDate: '2026-11-17T11:45:00' },
  { processType: 'DEV-OLD', description: 'DEV 不使用樣本', issueState: -1, dataStamp: 5, creator: 'DEV001', createDate: '2026-08-03T08:20:00', reviser: 'DEV002', reviseDate: '2026-10-08T16:40:00' },
]
const seedProperties: ProcessPropertyRow[] = [
  { processNo: 'DEV-STANDARD', processVersion: '-1', propertyNo: 'DEV-PROC-TEMP', defaultValue: '25', propertySequence: 1, description: 'DEV 溫度條件', issueState: 0, dataStamp: 1 },
  { processNo: 'DEV-OUTSOURCE', processVersion: '-1', propertyNo: 'DEV-PROC-ROUTE', defaultValue: 'OUT', propertySequence: 1, description: 'DEV 外包路徑', issueState: 2, dataStamp: 2 },
]

const rows = ref<ProcessTypeRow[]>([])
const propertyRows = ref<ProcessPropertyRow[]>([])
const issueState = ref<IssueState>(0)
const searchText = ref('')
const activeType = ref('')
const selectedTypes = ref<string[]>([])
const editorOpen = ref(false)
const mode = ref<EditMode>('')
const sourceType = ref('')
const editorSource = ref<ProcessTypeRow | null>(null)
const editorCreateDate = ref('')
const typeInput = ref<HTMLInputElement | null>(null)
const descriptionInput = ref<HTMLTextAreaElement | null>(null)
const page = ref<HTMLElement | null>(null)
const editor = ref<HTMLElement | null>(null)
const propertyWindow = ref<HTMLElement | null>(null)
let previousModalFocus: HTMLElement | null = null
const draft = ref({ processType: '', description: '' })
const draftDirty = ref(false)
const propertyWindowOpen = ref(false)
const propertyState = ref<IssueState>(0)
const selectedPropertyKey = ref('')
const selectedPropertyKeys = ref<string[]>([])
const propertyDraft = ref<ProcessPropertyRow | null>(null)
const busy = ref(false)
const error = ref('')
const notice = ref('')
const modalOpen = computed(() => editorOpen.value || propertyWindowOpen.value)

const visibleRows = computed(() => rows.value.filter((row) =>
  row.issueState === issueState.value
  && (!searchText.value.trim() || row.processType.toLocaleLowerCase().includes(searchText.value.trim().toLocaleLowerCase()))))
const activeRow = computed(() => visibleRows.value.find((row) => row.processType === activeType.value) ?? null)
const selectedRows = computed(() => visibleRows.value.filter((row) => selectedTypes.value.includes(row.processType)))
const visibleProperties = computed(() => !mock.value ? [] : propertyRows.value.filter((row) =>
  row.processNo === activeType.value && row.issueState === propertyState.value))
const selectedProperties = computed(() => propertyRows.value.filter((row) => selectedPropertyKeys.value.includes(propertyKey(row))))
const draftAudit = computed(() => editorSource.value?.processType.toUpperCase() === draft.value.processType.trim().toUpperCase() ? editorSource.value : null)

watch(visibleRows, visible => {
  const visibleKeys = new Set(visible.map(row => row.processType))
  selectedTypes.value = selectedTypes.value.filter(key => visibleKeys.has(key))
  if (!visibleKeys.has(activeType.value)) activeType.value = ''
})

function copy<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function userNo(): string { return props.currentUserNo || (mock.value ? 'DEV001' : '') }
function formatDate(value: string | null | undefined): string {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value.replace('T', ' ').slice(0, 19).replaceAll('-', '/')
  const pad = (part: number) => String(part).padStart(2, '0')
  return `${date.getFullYear()}/${pad(date.getMonth() + 1)}/${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
}
function issueLabel(value: IssueState | null): string {
  if (value === null) return ''
  return states.find((item) => item.value === value)?.label ?? String(value)
}
function persistMock(): void {
  localStorage.setItem(listStorageKey, JSON.stringify(rows.value))
  localStorage.setItem(propertyStorageKey, JSON.stringify(propertyRows.value))
}
function loadMock(): void {
  try {
    const savedRows = localStorage.getItem(listStorageKey)
    const savedProperties = localStorage.getItem(propertyStorageKey)
    rows.value = savedRows ? JSON.parse(savedRows) as ProcessTypeRow[] : copy(seedRows)
    propertyRows.value = savedProperties ? JSON.parse(savedProperties) as ProcessPropertyRow[] : copy(seedProperties)
    if (!savedRows || !savedProperties) persistMock()
  } catch {
    rows.value = copy(seedRows)
    propertyRows.value = copy(seedProperties)
    persistMock()
  }
}

async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  const response = await fetch(props.apiBaseUrl.replace(/\/+$/, '') + path, {
    credentials: 'include', ...init, headers,
  })
  if (response.status === 401) {
    window.location.assign(import.meta.env.BASE_URL + 'login')
    throw new Error('登入逾期，請重新登入。')
  }
  if (!response.ok) {
    const raw = await response.text()
    let message = raw.trim()
    try {
      const body: unknown = JSON.parse(raw)
      if (body && typeof body === 'object') {
        const values = body as Record<string, unknown>
        const detail = [values.detail, values.message, values.title].find(value => typeof value === 'string' && value.trim())
        if (typeof detail === 'string') message = detail
      }
    } catch { /* Preserve non-JSON service messages. */ }
    throw new Error(message || `API 回應錯誤 (${response.status})。`)
  }
  return response.status === 204 ? undefined as T : await response.json() as T
}

async function load(): Promise<void> {
  busy.value = true
  error.value = ''
  try {
    if (mock.value) loadMock()
    else {
      const query = new URLSearchParams({ issueState: String(issueState.value), processType: searchText.value.trim() })
      const result = await api<ProcessTypeRow[] | Envelope<ProcessTypeRow>>(`/api/process-types?${query}`)
      rows.value = Array.isArray(result) ? result : result.items ?? result.rows ?? result.data ?? []
    }
    if (!visibleRows.value.some((row) => row.processType === activeType.value)) activeType.value = visibleRows.value[0]?.processType ?? ''
    selectedTypes.value = []
    notice.value = `資料筆數 ${visibleRows.value.length}`
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : '載入流程類別失敗。'
  } finally {
    busy.value = false
  }
}

function queryRows(): void { if (!busy.value && !modalOpen.value) void load() }
function closePage(): void { if (!busy.value && !modalOpen.value) emit('close') }

function selectRow(row: ProcessTypeRow): void {
  if (busy.value || modalOpen.value || !visibleRows.value.some(item => item.processType === row.processType)) return
  activeType.value = row.processType
  selectedTypes.value = [row.processType]
}
function toggleRow(row: ProcessTypeRow): void {
  if (busy.value || modalOpen.value || !visibleRows.value.some(item => item.processType === row.processType)) return
  selectedTypes.value = selectedTypes.value.includes(row.processType)
    ? selectedTypes.value.filter((value) => value !== row.processType)
    : [...selectedTypes.value, row.processType]
  activeType.value = row.processType
}
function openEditor(next: Exclude<EditMode, ''>): void {
  if (busy.value || modalOpen.value || confirmationMessage.value !== null) return
  const row = activeRow.value
  error.value = ''
  if (next !== 'add' && !row) { error.value = '請先選取流程類別。'; return }
  if (next === 'edit' && row?.issueState !== 0) { error.value = '只有未核准資料可修改。'; return }
  previousModalFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null
  mode.value = next
  sourceType.value = row?.processType ?? ''
  editorSource.value = next === 'edit' && row ? copy(row) : null
  editorCreateDate.value = next === 'edit' ? row?.createDate ?? '' : new Date().toISOString()
  draftDirty.value = false
  draft.value = next === 'add'
    ? { processType: '', description: '' }
    : { processType: row?.processType ?? '', description: row?.description ?? '' }
  editorOpen.value = true
  void restoreModalFocus()
}
async function closeEditor(): Promise<void> {
  if (busy.value || confirmationMessage.value !== null) return
  editorOpen.value = false
  await restoreModalFocus()
}

function canFocus(element: HTMLElement): boolean {
  return element.isConnected && element.getClientRects().length > 0 && !element.matches(':disabled') && !element.closest('[inert]')
}
function modalControls(container: HTMLElement): HTMLElement[] {
  return Array.from(container.querySelectorAll<HTMLElement>('button:not(:disabled),input:not(:disabled),textarea:not(:disabled),select:not(:disabled),[tabindex="0"]')).filter(canFocus)
}
async function restoreModalFocus(): Promise<void> {
  await nextTick()
  if (confirmationMessage.value !== null || busy.value) return
  const container = propertyWindowOpen.value ? propertyWindow.value : editorOpen.value ? editor.value : null
  if (container) {
    const preferred = editorOpen.value ? mode.value === 'edit' ? descriptionInput.value : typeInput.value : null
    ;(preferred && canFocus(preferred) ? preferred : modalControls(container)[0] ?? container).focus()
  } else if (previousModalFocus && page.value?.contains(previousModalFocus) && canFocus(previousModalFocus)) previousModalFocus.focus()
  else {
    const first = Array.from(page.value?.querySelectorAll<HTMLElement>('input:not(:disabled),button:not(:disabled),[tabindex="0"]') ?? []).find(canFocus)
    ;(first ?? page.value)?.focus()
  }
}
function keepModalFocus(event: KeyboardEvent): void {
  if (event.key !== 'Tab' || !modalOpen.value || confirmationMessage.value !== null) return
  if (busy.value) { event.preventDefault(); return }
  const container = propertyWindowOpen.value ? propertyWindow.value : editor.value
  if (!container) { event.preventDefault(); return }
  const controls = modalControls(container)
  const first = controls[0]; const last = controls[controls.length - 1]
  if (!first || !last) { event.preventDefault(); container.focus(); return }
  const active = document.activeElement
  if (!container.contains(active) || active === container) { event.preventDefault(); (event.shiftKey ? last : first).focus() }
  else if (event.shiftKey && active === first) { event.preventDefault(); last.focus() }
  else if (!event.shiftKey && active === last) { event.preventDefault(); first.focus() }
}
async function closeProperties(): Promise<void> {
  if (busy.value || confirmationMessage.value !== null) return
  propertyWindowOpen.value = false
  await restoreModalFocus()
}

function validateDraft(): string {
  const processType = draft.value.processType.trim().toUpperCase()
  if (!processType) return 'Process Type 為必填欄位。'
  if (processType.length > 20) return 'Process Type 最多 20 個字元。'
  if (draft.value.description.length > 255) return 'Description 最多 255 個字元。'
  const duplicate = rows.value.some((row) => row.processType.toUpperCase() === processType
    && !(mode.value === 'edit' && row.processType === sourceType.value))
  if (duplicate) return 'Process Type 已存在。'
  return ''
}

async function save(keepOpen: boolean): Promise<void> {
  if (busy.value) return
  const validation = validateDraft()
  if (validation) { error.value = validation; return }
  if (mode.value === 'edit' && !draftDirty.value) {
    if (!keepOpen) editorOpen.value = false
    notice.value = '資料未變更。'
    await restoreModalFocus()
    return
  }
  const processType = draft.value.processType.trim().toUpperCase()
  const description = draft.value.description
  const original = editorSource.value
  busy.value = true
  error.value = ''
  try {
    let saved: ProcessTypeRow
    if (!mock.value) {
      if (mode.value === 'edit') {
        if (!original) throw new Error('編輯版本資料遺失，請關閉後重新查詢。')
        const body = JSON.stringify({ processType, description, dataStamp: original.dataStamp })
        saved = await api<ProcessTypeRow>(`/api/process-types/${encodeURIComponent(original.processType)}`, { method: 'PUT', body })
      } else {
        saved = await api<ProcessTypeRow>('/api/process-types', { method: 'POST', body: JSON.stringify({ processType, description }) })
      }
    } else if (mode.value === 'edit' && original) {
      const current = rows.value.find(row => row.processType === original.processType)
      if (!current || current.dataStamp !== original.dataStamp || current.issueState !== 0) throw new Error('資料已變更，請重新查詢。')
      Object.assign(current, { description, dataStamp: (current.dataStamp ?? 0) + 1, reviser: userNo(), reviseDate: new Date().toISOString() })
      saved = copy(current)
      persistMock()
    } else {
      const now = new Date().toISOString()
      saved = { processType, description, issueState: 0, dataStamp: 0, creator: userNo(), createDate: now, reviser: null, reviseDate: null }
      rows.value.push(saved)
      persistMock()
    }
    // Retain the real committed version before reloading a filtered list.
    editorSource.value = copy(saved)
    sourceType.value = saved.processType
    draft.value = { processType: saved.processType, description: saved.description ?? '' }
    editorCreateDate.value = saved.createDate ?? ''
    activeType.value = saved.processType
    if (saved.issueState !== null) issueState.value = saved.issueState
    await load()
    if (keepOpen && (mode.value === 'add' || mode.value === 'copy')) {
      draftDirty.value = false
      notice.value = '資料已套用；表單保持開啟，Process Type 欄位可繼續輸入。'
    } else if (keepOpen && mode.value === 'edit') {
      draftDirty.value = false
      notice.value = '資料已套用；表單保持開啟。'
    } else {
      editorOpen.value = false
      notice.value = mock.value ? '開發假資料已更新。' : '已儲存。'
    }
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : '儲存流程類別失敗。'
  } finally {
    busy.value = false
    await restoreModalFocus()
  }
}

async function deleteSelected(): Promise<void> {
  if (busy.value || modalOpen.value || confirmationMessage.value !== null) return
  if (!selectedRows.value.length && activeRow.value) selectedTypes.value = [activeRow.value.processType]
  const targets = selectedRows.value.map(row => copy(row))
  if (!targets.length) { error.value = '請先選取流程類別。'; return }
  if (targets.some((row) => row.issueState === 1)) { error.value = '簽核中的資料不可刪除。'; return }
  if (!await requestConfirmation(`確定刪除／停用 ${targets.length} 筆流程類別？`)) return
  if (targets.some(row => !visibleRows.value.some(item => item.processType === row.processType))) {
    error.value = '目前顯示資料已變更，請重新選取可見的流程類別。'; return
  }
  busy.value = true; error.value = ''
  let completed = 0; let failure = ''
  try {
    for (const row of targets) {
      if (!mock.value) {
        await api(`/api/process-types/${encodeURIComponent(row.processType)}`, {
          method: 'DELETE', body: JSON.stringify({ issueState: row.issueState, dataStamp: row.dataStamp }),
        })
      } else {
        const current = rows.value.find(item => item.processType === row.processType)
        if (!current || current.dataStamp !== row.dataStamp || current.issueState !== row.issueState) throw new Error('資料已變更，請重新查詢。')
        if (current.issueState === 2) {
          current.issueState = -1; current.dataStamp = (current.dataStamp ?? 0) + 1
          current.reviser = userNo(); current.reviseDate = new Date().toISOString()
        } else {
          rows.value = rows.value.filter((item) => item.processType !== row.processType)
          propertyRows.value = propertyRows.value.filter((item) => item.processNo !== row.processType)
        }
      }
      completed++
    }
  } catch (cause) {
    failure = (cause instanceof Error ? cause.message : '刪除流程類別失敗。') + (completed ? `（前 ${completed} 筆已完成）` : '')
  } finally {
    if (mock.value) persistMock()
    await load()
    notice.value = `已刪除／停用 ${completed} 筆流程類別。`
    if (failure) error.value = failure
    busy.value = false
  }
}

async function approveSelected(): Promise<void> {
  if (busy.value || modalOpen.value || confirmationMessage.value !== null) return
  const targets = (selectedRows.value.length ? selectedRows.value : activeRow.value ? [activeRow.value] : []).map(row => copy(row))
  if (!targets.length || targets.some((row) => row.issueState !== 0)) { error.value = '請選取未核准的流程類別。'; return }
  if (!await requestConfirmation(`送出 ${targets.length} 筆流程類別核准？`)) return
  if (targets.some(row => !visibleRows.value.some(item => item.processType === row.processType))) {
    error.value = '目前顯示資料已變更，請重新選取可見的流程類別。'; return
  }
  busy.value = true; error.value = ''
  let completed = 0; let failure = ''
  try {
    for (const row of targets) {
      if (!mock.value) {
        await api(`/api/process-types/${encodeURIComponent(row.processType)}/approve`, {
          method: 'POST', body: JSON.stringify({ dataStamp: row.dataStamp }),
        })
      } else {
        const current = rows.value.find(item => item.processType === row.processType)
        if (!current || current.dataStamp !== row.dataStamp || current.issueState !== 0) throw new Error('資料已變更，請重新查詢。')
        current.issueState = 2; current.dataStamp = (current.dataStamp ?? 0) + 1
        current.reviser = userNo(); current.reviseDate = new Date().toISOString()
      }
      completed++
    }
  } catch (cause) {
    failure = (cause instanceof Error ? cause.message : '核准流程類別失敗。') + (completed ? `（前 ${completed} 筆已完成）` : '')
  } finally {
    if (mock.value) persistMock()
    await load()
    notice.value = `已完成 ${completed} 筆流程類別核准送出。`
    if (failure) error.value = failure
    busy.value = false
  }
}

async function openProperties(): Promise<void> {
  if (busy.value || editorOpen.value || confirmationMessage.value !== null) return
  if (!activeRow.value) { error.value = '請先選取流程類別。'; return }
  if (!propertyWindowOpen.value) {
    previousModalFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null
    propertyState.value = 0
  }
  propertyWindowOpen.value = true
  propertyDraft.value = null
  selectedPropertyKey.value = ''
  selectedPropertyKeys.value = []
  // No shared ProcessProperty/Element provider exists yet. Do not present DEV
  // choices or pretend an absent service is an empty production data set.
  if (!mock.value) { propertyRows.value = []; error.value = ''; await restoreModalFocus(); return }
  try {
    if (mock.value) loadMock()
    else {
      const query = new URLSearchParams({ processNo: activeRow.value.processType, issueState: String(propertyState.value) })
      const result = await api<ProcessPropertyRow[] | Envelope<ProcessPropertyRow>>(`/api/process-properties?${query}`)
      const received = Array.isArray(result) ? result : result.items ?? result.rows ?? result.data ?? []
      propertyRows.value = [...propertyRows.value.filter((row) => row.processNo !== activeRow.value?.processType), ...received]
    }
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : '載入流程屬性失敗。'
  } finally {
    await restoreModalFocus()
  }
}

function propertyKey(row: ProcessPropertyRow): string {
  return `${row.processNo}\u001f${row.processVersion}\u001f${row.propertyNo}`
}
function selectProperty(row: ProcessPropertyRow): void {
  selectedPropertyKey.value = propertyKey(row)
  selectedPropertyKeys.value = [selectedPropertyKey.value]
  propertyDraft.value = null
}
function toggleProperty(row: ProcessPropertyRow): void {
  const key = propertyKey(row)
  selectedPropertyKey.value = key
  selectedPropertyKeys.value = selectedPropertyKeys.value.includes(key)
    ? selectedPropertyKeys.value.filter((value) => value !== key)
    : [...selectedPropertyKeys.value, key]
  propertyDraft.value = null
}
function editProperty(row?: ProcessPropertyRow): void {
  if (!mock.value) return
  if (row) selectedPropertyKey.value = propertyKey(row)
  propertyDraft.value = row ? copy(row) : {
    processNo: activeType.value, processVersion: '-1', propertyNo: '', defaultValue: '',
    propertySequence: 0, description: '', issueState: 0, dataStamp: 0,
  }
}
function editSelectedProperty(): void {
  const row = selectedProperties.value.find((item) => propertyKey(item) === selectedPropertyKey.value)
  if (row) editProperty(row)
}
async function saveProperty(): Promise<void> {
  if (!mock.value) return
  const value = propertyDraft.value
  if (!value || !value.propertyNo.trim()) { error.value = 'Property No 為必填欄位。'; return }
  try {
    if (!mock.value) {
      const body = JSON.stringify(value)
      if (selectedPropertyKey.value) await api('/api/process-properties', { method: 'PUT', body })
      else await api('/api/process-properties', { method: 'POST', body })
      await openProperties()
    } else {
      const old = propertyRows.value.find((row) => propertyKey(row) === selectedPropertyKey.value)
      if (old) Object.assign(old, value, { dataStamp: (old.dataStamp ?? 0) + 1 })
      else propertyRows.value.push(copy(value))
      persistMock()
    }
    propertyDraft.value = null
    selectedPropertyKey.value = ''
    notice.value = '流程屬性已更新。'
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : '儲存流程屬性失敗。'
  }
}
async function deleteProperty(row: ProcessPropertyRow, confirmDelete = true): Promise<void> {
  if (!mock.value) return
  if (confirmDelete && !await requestConfirmation(`確定刪除 Property ${row.propertyNo}？`)) return
  if (row.issueState === 1) { error.value = '簽核中的屬性不可刪除。'; return }
  try {
    if (!mock.value) await api('/api/process-properties', { method: 'DELETE', body: JSON.stringify({ processNo: row.processNo, processVersion: row.processVersion, propertyNo: row.propertyNo, issueState: row.issueState, dataStamp: row.dataStamp }) })
    else if (row.issueState === 2) { row.issueState = -1; row.dataStamp = (row.dataStamp ?? 0) + 1 }
    else propertyRows.value = propertyRows.value.filter((item) => propertyKey(item) !== propertyKey(row))
    if (mock.value) persistMock()
    propertyDraft.value = null
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : '刪除流程屬性失敗。'
  }
}
async function deleteSelectedProperties(): Promise<void> {
  if (!mock.value) return
  const targets = selectedProperties.value
  if (!targets.length) { error.value = '請先選取 Property。'; return }
  if (targets.some((row) => row.issueState === 1)) { error.value = '簽核中的屬性不可刪除。'; return }
  if (!await requestConfirmation(`確定刪除／停用 ${targets.length} 筆 Property？`)) return
  for (const row of targets) await deleteProperty(row, false)
  selectedPropertyKeys.value = []
  selectedPropertyKey.value = ''
}
async function approveSelectedProperties(): Promise<void> {
  if (!mock.value) return
  const targets = selectedProperties.value
  if (!targets.length || targets.some((row) => row.issueState !== 0)) { error.value = '請選取未核准的 Property。'; return }
  if (!await requestConfirmation(`送出 ${targets.length} 筆 Property 核准？`)) return
  try {
    for (const row of targets) {
      if (!mock.value) {
        await api('/api/process-properties/approve', { method: 'POST', body: JSON.stringify({ processNo: row.processNo, processVersion: row.processVersion, propertyNo: row.propertyNo, dataStamp: row.dataStamp }) })
      } else {
        row.issueState = 2
        row.dataStamp = (row.dataStamp ?? 0) + 1
      }
    }
    if (mock.value) persistMock()
    await openProperties()
    notice.value = mock.value ? 'DEV Property 核准流程已模擬完成。' : 'Property 已送交共用簽核流程。'
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : '核准 Property 失敗。'
  }
}
function deleteSelectedProperty(): void {
  void deleteSelectedProperties()
}

onMounted(() => { document.addEventListener('keydown', keepModalFocus, true); void load() })
onBeforeUnmount(() => { document.removeEventListener('keydown', keepModalFocus, true) })
</script>

<template>
  <main ref="page" class="process-type-master" tabindex="-1">
    <header class="page-title" :inert="modalOpen || confirmationMessage !== null">
      <div><h2>流程類別資料</h2><small>流程資料 / 流程設定 / PRS TYPE / PS103</small></div>
      <b v-if="mock" class="dev-badge">開發假資料</b>
      <button type="button" :disabled="busy || modalOpen" @click="closePage">關閉</button>
    </header>
    <p v-if="mock" class="dev-note">資料只存在這台電腦的 localStorage，不會寫入正式資料庫。</p>
    <section class="filters" :inert="modalOpen || confirmationMessage !== null">
      <label v-for="state in states" :key="state.value" class="state-option">
        <input v-model="issueState" type="radio" :value="state.value" :disabled="busy || modalOpen" @change="queryRows">{{ state.label }}
      </label>
      <label class="search">Process Type <input v-model="searchText" maxlength="20" :disabled="busy || modalOpen" @keyup.enter="queryRows"></label>
      <button type="button" class="primary" :disabled="busy || modalOpen" @click="queryRows">查詢</button>
    </section>
    <p v-if="error" class="message error" role="alert">{{ error }}</p>
    <p v-else-if="notice" class="message" role="status">{{ notice }}</p>
    <section class="main-layout" :inert="modalOpen || confirmationMessage !== null">
      <div class="grid-wrap">
        <table>
          <thead><tr><th class="select-cell"></th><th>Process Type</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
          <tbody>
            <tr v-for="row in visibleRows" :key="row.processType" :class="{ selected: activeType === row.processType }" @click="selectRow(row)">
              <td class="select-cell"><input type="checkbox" :checked="selectedTypes.includes(row.processType)" :disabled="busy || modalOpen" @click.stop="toggleRow(row)"></td>
              <td>{{ row.processType }}</td><td class="description">{{ row.description }}</td><td>{{ row.creator }}</td>
              <td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td>
            </tr>
            <tr v-if="!visibleRows.length"><td colspan="7" class="empty">{{ busy ? '載入中…' : '查無資料' }}</td></tr>
          </tbody>
        </table>
      </div>
      <aside class="actions">
        <button type="button" class="primary" :disabled="busy || modalOpen" @click="openEditor('add')">ADD (A)</button>
        <button type="button" :disabled="busy || modalOpen || activeRow?.issueState !== 0" @click="openEditor('edit')">EDIT (E)</button>
        <button type="button" :disabled="busy || modalOpen || !activeRow" @click="openEditor('copy')">Copy (C)</button>
        <button type="button" :disabled="busy || modalOpen" @click="approveSelected">Approve (Y)</button>
        <button type="button" :disabled="busy || modalOpen" @click="deleteSelected">DEL (D)</button>
        <button type="button" :disabled="busy || modalOpen || !activeRow" @click="openProperties">Property (S)</button>
      </aside>
    </section>
    <footer class="grid-footer"><span>資料總筆數 {{ visibleRows.length }}<template v-if="activeRow">　{{ issueLabel(activeRow.issueState) }}</template></span></footer>

    <div v-if="editorOpen" class="overlay" @click.self="closeEditor">
      <section ref="editor" class="dialog" role="dialog" aria-modal="true" aria-label="Process Type Define" tabindex="-1" :inert="confirmationMessage !== null">
        <header><h3>Process Type Define ({{ mode === 'add' ? 'ADD' : mode === 'edit' ? 'EDIT' : 'Copy' }})</h3><button type="button" :disabled="busy" @click="closeEditor">×</button></header>
        <fieldset><legend>Process Type Data</legend>
          <label>Process Type<input ref="typeInput" v-model.trim="draft.processType" maxlength="20" :disabled="busy" :readonly="mode === 'edit'" @input="draftDirty = true" @blur="draft.processType = draft.processType.toUpperCase()"></label>
          <label>Description<textarea ref="descriptionInput" v-model="draft.description" maxlength="255" rows="3" :disabled="busy" @input="draftDirty = true"></textarea></label>
        </fieldset>
        <div class="audit-grid"><span>Creator</span><input :value="draftAudit?.creator ?? userNo()" disabled><span>CreateDate</span><input :value="formatDate(draftAudit?.createDate ?? editorCreateDate)" disabled><span>Reviser</span><input :value="draftAudit?.reviser ?? ''" disabled><span>ReviseDate</span><input :value="formatDate(draftAudit?.reviseDate)" disabled></div>
        <p v-if="error" class="message error">{{ error }}</p>
        <footer><button type="button" class="primary" :disabled="busy" @click="save(false)">確定</button><button type="button" :disabled="busy" @click="save(true)">套用</button><button type="button" :disabled="busy" @click="closeEditor">關閉</button></footer>
      </section>
    </div>

    <div v-if="propertyWindowOpen" class="overlay" @click.self="closeProperties">
      <section ref="propertyWindow" class="dialog property-dialog" role="dialog" aria-modal="true" aria-label="Process Property" tabindex="-1" :inert="confirmationMessage !== null">
        <header><div><h3>Process Property</h3><small>ProcessNo = {{ activeType }}</small></div><button type="button" :disabled="busy" @click="closeProperties">×</button></header>
        <p v-if="!mock" class="message error" role="alert">流程屬性維護尚未開放；主檔功能可正常使用。</p>
        <div class="property-states"><label v-for="state in states" :key="state.value"><input v-model="propertyState" type="radio" :value="state.value" :disabled="!mock" @change="openProperties">{{ state.label }}</label></div>
        <div class="grid-wrap property-grid"><table><thead><tr><th class="select-cell"></th><th>ProcessVersion</th><th>PropertyNo</th><th>DefaultValue</th><th>Sequence</th><th>Description</th></tr></thead><tbody>
          <tr v-for="row in visibleProperties" :key="propertyKey(row)" :class="{ selected: selectedPropertyKeys.includes(propertyKey(row)) }" @click="selectProperty(row)"><td><input type="checkbox" :checked="selectedPropertyKeys.includes(propertyKey(row))" @click.stop="toggleProperty(row)"></td><td>{{ row.processVersion === '-1' ? '' : row.processVersion }}</td><td>{{ row.propertyNo }}</td><td>{{ row.defaultValue }}</td><td>{{ row.propertySequence }}</td><td>{{ row.description }}</td></tr>
          <tr v-if="!visibleProperties.length"><td colspan="6" class="empty">{{ mock ? '查無資料' : '流程屬性服務尚未提供' }}</td></tr>
        </tbody></table></div>
        <div class="property-actions"><button type="button" :disabled="!mock" @click="editProperty()">ADD</button><button type="button" :disabled="!mock || !selectedPropertyKey" @click="editSelectedProperty">EDIT</button><button type="button" :disabled="!mock || !propertyDraft" @click="saveProperty">套用屬性</button><button type="button" :disabled="!mock || !selectedPropertyKeys.length" @click="deleteSelectedProperty">DEL</button><button type="button" :disabled="!mock || !selectedPropertyKeys.length" @click="approveSelectedProperties">Approve</button></div>
        <div v-if="propertyDraft && mock" class="property-form">
          <label>ProcessVersion<input :value="propertyDraft.processVersion === '-1' ? '' : propertyDraft.processVersion" readonly></label>
          <label>PropertyNo<select v-model="propertyDraft.propertyNo"><option value="">請選擇</option><option v-for="value in propertyChoices" :key="value">{{ value }}</option></select></label>
          <label>DefaultValue<input v-model="propertyDraft.defaultValue"></label>
          <label>Sequence<input v-model.number="propertyDraft.propertySequence" type="number" min="0" max="99"></label>
          <label class="wide">Description<input v-model="propertyDraft.description" maxlength="255"></label>
          <p class="dev-note wide">PropertyNo 正式選項取自核准的 System Element；此處只列 DEV 假值。</p>
        </div>
        <footer><button type="button" class="primary" :disabled="busy" @click="closeProperties">關閉</button></footer>
      </section>
    </div>
    <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
  </main>
</template>

<style scoped>
.process-type-master{display:flex;flex-direction:column;gap:10px;padding:16px 18px 22px;color:#292820;min-width:0}.page-title,.filters,.dialog header,.dialog footer,.grid-footer{display:flex;align-items:center;gap:9px}.page-title{justify-content:space-between}.page-title h2{margin:0;font-size:20px}.page-title small,.dialog header small{display:block;margin-top:4px;color:#777466}.dev-badge{padding:4px 10px;border:1px solid #e7c600;border-radius:12px;background:#fff4b5}.dev-note{margin:0;padding:8px 10px;border:1px solid #e7c600;background:#fff9d9;color:#685b20}.filters{min-height:42px;flex-wrap:wrap;padding:7px 10px;border:1px solid #e5e1d5;background:#fff}.state-option,.property-states label{display:flex;align-items:center;gap:4px;font-size:13px}.search{display:flex;align-items:center;gap:8px;margin-left:9px}.search input,.dialog input,.dialog textarea,.dialog select{min-height:30px;padding:5px 7px;border:1px solid #aaa69b;background:#fff8d8;font:inherit}button{min-height:31px;padding:6px 12px;border:1px solid #c9c5ba;border-radius:3px;background:#f8f7f2;color:inherit;font:inherit;cursor:pointer;white-space:nowrap}button:disabled{opacity:.45;cursor:not-allowed}.primary{border-color:#eda029;background:#f6a62b;color:white}.message{margin:0;padding:8px 10px;border-left:3px solid #d4ad00;background:#fff9dd}.message.error{border-color:#b74439;background:#fff0ed;color:#8d241c}.main-layout{display:grid;grid-template-columns:minmax(0,1fr) 88px;gap:9px;min-height:330px}.grid-wrap{overflow:auto;border:1px solid #d8d3c6;background:white}table{width:100%;min-width:760px;border-collapse:collapse;font-size:12px}th{position:sticky;top:0;background:#e7c600;text-align:left;font-weight:600}th,td{padding:7px 8px;border:1px solid #d8d3c6;white-space:nowrap}tbody tr{cursor:pointer}tbody tr:hover{background:#fff8d8}tbody tr.selected{background:#ffedaa}.select-cell{width:30px;text-align:center}.description{min-width:200px;white-space:normal}.empty{text-align:center;color:#777366;padding:20px}.actions{display:flex;flex-direction:column;gap:6px}.actions button{min-height:32px}.grid-footer{min-height:25px;color:#777466;font-size:12px}.grid-footer small{margin-left:auto}.overlay{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:16px;background:#14130f66}.dialog{width:min(496px,100%);max-height:92vh;overflow:auto;padding:14px 16px;background:white;border:1px solid #c9c5ba;box-shadow:0 12px 40px #0003}.dialog header{justify-content:space-between;padding-bottom:9px;margin-bottom:10px;border-bottom:1px solid #e5e1d5}.dialog h3{margin:0;font-size:17px}.dialog fieldset{margin:0;padding:10px 12px 14px;border:1px solid #c9c5ba}.dialog legend{padding:0 5px}.dialog fieldset label{display:grid;grid-template-columns:92px 1fr;align-items:start;gap:8px;margin:7px 0;font-size:13px}.dialog textarea{resize:vertical}.audit-grid{display:grid;grid-template-columns:80px 1fr 84px 1fr;gap:5px 8px;align-items:center;margin-top:9px;padding:8px;border:1px solid #c9c5ba;background:#f8f7f2;font-size:12px}.audit-grid input{min-width:0;background:#eee}.dialog footer{justify-content:flex-end;margin-top:12px}.property-dialog{width:min(820px,100%)}.property-states{display:flex;gap:12px;flex-wrap:wrap;margin:8px 0}.property-grid{max-height:240px}.property-grid table{min-width:680px}.property-actions{display:flex;gap:6px;margin:8px 0}.property-form{display:grid;grid-template-columns:1fr 1fr;gap:8px;padding:10px;border:1px solid #d8d3c6}.property-form label{display:flex;flex-direction:column;gap:4px;font-size:12px}.property-form .wide{grid-column:1/-1}.property-form .dev-note{font-size:12px}@media(max-width:850px){.main-layout{grid-template-columns:1fr}.actions{display:grid;grid-template-columns:repeat(3,minmax(0,1fr))}.audit-grid{grid-template-columns:80px 1fr}.property-form{grid-template-columns:1fr}.property-form .wide{grid-column:auto}}@media(max-width:560px){.process-type-master{padding:12px}.search{margin-left:0}.page-title{align-items:flex-start}.property-states{gap:7px}}
</style>
