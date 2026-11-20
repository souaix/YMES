<script setup lang="ts">
// Offline candidate; relative imports target web/src/components after Root integration.
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'
import { apiErrorMessage } from '../utils/apiError'

type Mode = 'new' | 'edit' | ''
type IssueFilter = 0 | 1 | 2 | -1
type OptionalField = 'contactorName' | 'telNo' | 'faxNo' | 'email' | 'address' | 'bankNo'
interface Broker {
  brokerNo: string; contactorName: string | null; telNo: string | null; faxNo: string | null
  email: string | null; address: string | null; bankNo: string | null; issueState: number | null
  creator: string | null; createDate: string | null; dataStamp: number | null; reviser: string | null; reviseDate: string | null
}
interface Draft { brokerNo: string; contactorName: string; telNo: string; faxNo: string; email: string; address: string; bankNo: string }
type BrokerWrite = Pick<Broker, 'brokerNo' | OptionalField>
type DeleteResult = { status: 204 } | { status: 200; item: Broker }
const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const apiRoot = 'api/ent/broker-basis', storageKey = 'ymes-dev-mock:ent-broker-basis:v1'
const seedRows: Broker[] = [
  { brokerNo: 'DEV-BROKER-01', contactorName: '開發用報關行甲', telNo: '02-0000-0101', faxNo: '02-0000-0102', email: 'broker.a@example.test', address: '台北市開發路 1 號', bankNo: 'DEV-BANK-01', issueState: 0, creator: 'DEV001', createDate: '2026-11-01T08:00:00+08:00', dataStamp: 0, reviser: null, reviseDate: null },
  { brokerNo: 'DEV-BROKER-02', contactorName: '開發用報關行乙', telNo: '02-0000-0201', faxNo: '', email: 'broker.b@example.test', address: '新北市範例路 2 號', bankNo: '', issueState: 2, creator: 'DEV001', createDate: '2026-11-02T08:00:00+08:00', dataStamp: 2, reviser: 'DEV002', reviseDate: '2026-11-03T09:00:00+08:00' },
  { brokerNo: 'DEV-BROKER-03', contactorName: '開發用報關行丙', telNo: '', faxNo: '', email: '', address: '', bankNo: '', issueState: 1, creator: 'DEV001', createDate: '2026-11-04T08:00:00+08:00', dataStamp: 1, reviser: 'DEV002', reviseDate: '2026-11-05T09:00:00+08:00' },
  { brokerNo: 'DEV-BROKER-04', contactorName: '開發用報關行丁', telNo: '', faxNo: '', email: '', address: '', bankNo: '', issueState: -1, creator: 'DEV001', createDate: '2026-11-06T08:00:00+08:00', dataStamp: 2, reviser: 'DEV002', reviseDate: '2026-11-07T09:00:00+08:00' },
]
const rows = ref<Broker[]>([]), rowsReady = ref(false), filterState = ref<IssueFilter>(0), keyword = ref(''), appliedKeyword = ref('')
const selectedKeys = ref<string[]>([]), activeKey = ref(''), loading = ref(false), busy = ref(false), saving = ref(false)
const error = ref(''), notice = ref(''), mode = ref<Mode>(''), sourceRow = ref<Broker | null>(null), draft = ref<Draft>(blankDraft())
const draftOriginal = ref(''), formReady = ref(false), formError = ref(''), formNotice = ref('')
const pageElement = ref<HTMLElement | null>(null), formElement = ref<HTMLElement | null>(null)
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()
let listEpoch = 0, formEpoch = 0, operationEpoch = 0, disposed = false
let previousModalFocus: HTMLElement | null = null
const writing = computed(() => busy.value || saving.value)
const modalOpen = computed(() => mode.value !== '')
const blocked = computed(() => loading.value || writing.value || modalOpen.value || confirmationMessage.value !== null)
const visibleRows = computed(() => rows.value.filter(row => row.issueState === filterState.value))
const selectedRows = computed(() => visibleRows.value.filter(row => selectedKeys.value.includes(row.brokerNo)))
const singleSelected = computed(() => selectedRows.value.length === 1 && selectedKeys.value.length === 1 ? selectedRows.value[0]! : null)
const allSelected = computed(() => visibleRows.value.length > 0 && selectedRows.value.length === visibleRows.value.length)
const canEdit = computed(() => rowsReady.value && singleSelected.value?.issueState === 0)

function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function blankDraft(): Draft { return { brokerNo: '', contactorName: '', telNo: '', faxNo: '', email: '', address: '', bankNo: '' } }
function trimAscii(value: string): string { return value.replace(/^ +| +$/g, '') }
function fold(value: string): string { return trimAscii(value).toLowerCase() }
function fail(value: unknown): string { return value instanceof Error ? value.message : '操作失敗，請重新查詢。' }
function stateName(value: number | null): string { return value === null ? '' : ({ 0: '未簽核', 1: '簽核中', 2: '已簽核', '-1': '不使用' } as Record<string, string>)[String(value)] ?? String(value) }
function path(resource = '', brokerNo?: string): string {
  const query = new URLSearchParams()
  if (brokerNo !== undefined) query.set('brokerNo', brokerNo)
  return apiRoot + (resource ? '/' + resource : '') + (query.size ? '?' + query.toString() : '')
}
class ApiFailure extends Error { status: number; constructor(status: number, message: string) { super(message); this.status = status } }
async function request(resource: string, init: RequestInit = {}): Promise<Response> {
  if (!props.apiBaseUrl.trim()) throw new Error('目前環境尚未設定 API 位址。')
  const url = new URL(resource.replace(/^\/+/, ''), new URL(props.apiBaseUrl.replace(/\/+$/, '') + '/', window.location.origin))
  const headers = new Headers(init.headers); if (init.body) headers.set('Content-Type', 'application/json')
  const response = await fetch(url, { credentials: 'include', ...init, headers })
  if (response.status === 401) { window.location.assign(import.meta.env.BASE_URL); throw new ApiFailure(401, '登入狀態已逾期。') }
  if (!response.ok) throw new ApiFailure(response.status, await apiErrorMessage(response))
  return response
}
function object(value: unknown): Record<string, unknown> { if (value === null || typeof value !== 'object' || Array.isArray(value)) throw new Error('伺服器回應不完整。'); return value as Record<string, unknown> }
function text(row: Record<string, unknown>, key: string): string | null {
  if (!Object.hasOwn(row, key) || row[key] !== null && typeof row[key] !== 'string') throw new Error('伺服器文字欄位回應不完整。')
  return row[key] as string | null
}
function integer(row: Record<string, unknown>, key: string): number | null {
  if (!Object.hasOwn(row, key) || row[key] !== null && (typeof row[key] !== 'number' || !Number.isSafeInteger(row[key]))) throw new Error('伺服器數值欄位回應不完整。')
  return row[key] as number | null
}
function dateValue(row: Record<string, unknown>, key: string, formal: boolean): string | null {
  const value = text(row, key)
  if (value !== null && (Number.isNaN(Date.parse(value)) || formal && !/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?Z$/.test(value))) throw new Error('伺服器日期欄位回應不完整。')
  return value
}
function normalizeBroker(value: unknown, formal = true): Broker {
  const row = object(value), brokerNo = text(row, 'brokerNo'), dataStamp = integer(row, 'dataStamp')
  if (brokerNo === null || dataStamp !== null && Math.abs(dataStamp) > 99999999999) throw new Error('伺服器識別值或版本回應不完整。')
  return { brokerNo, contactorName: text(row, 'contactorName'), telNo: text(row, 'telNo'), faxNo: text(row, 'faxNo'), email: text(row, 'email'), address: text(row, 'address'), bankNo: text(row, 'bankNo'),
    issueState: integer(row, 'issueState'), creator: text(row, 'creator'), createDate: dateValue(row, 'createDate', formal), dataStamp, reviser: text(row, 'reviser'), reviseDate: dateValue(row, 'reviseDate', formal) }
}
async function responseDto(response: Response): Promise<Broker> {
  if (response.status !== 200) throw new Error('伺服器未回傳完整的儲存結果。')
  return normalizeBroker(await response.json())
}
function formatDate(value: string | null | undefined): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : new Intl.DateTimeFormat('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23' }).format(date)
}
function readMockRows(): Broker[] {
  try { const raw = localStorage.getItem(storageKey); if (raw) { const value: unknown = JSON.parse(raw); if (Array.isArray(value)) return value.map(row => normalizeBroker(row, false)) } } catch { /* Historical DEV data never enters formal mode. */ }
  return clone(seedRows)
}
function saveMockRows(value: Broker[]): void { localStorage.setItem(storageKey, JSON.stringify(value)) }
function pruneSelection(preferred = ''): void {
  const keys = new Set(visibleRows.value.map(row => row.brokerNo))
  selectedKeys.value = preferred && keys.has(preferred) ? [preferred] : selectedKeys.value.filter(key => keys.has(key))
  activeKey.value = selectedKeys.value.includes(activeKey.value) ? activeKey.value : selectedKeys.value[0] ?? ''
}
function upsert(row: Broker): void {
  const index = rows.value.findIndex(value => value.brokerNo === row.brokerNo)
  if (index < 0) rows.value.push(clone(row)); else rows.value[index] = clone(row)
  pruneSelection()
}
async function reloadRows(term: string, preferred = ''): Promise<void> {
  const epoch = ++listEpoch
  let result: Broker[]
  if (mock.value) result = readMockRows().filter(row => !term || row.brokerNo.toLowerCase().includes(term.toLowerCase())).sort((a, b) => a.brokerNo.localeCompare(b.brokerNo))
  else {
    const response = await request(path('', term || undefined))
    if (response.status !== 200) throw new Error('伺服器清單回應不完整。')
    const value = object(await response.json()).items
    if (!Array.isArray(value)) throw new Error('伺服器清單回應不完整。')
    result = value.map(value => normalizeBroker(value))
  }
  if (disposed || epoch !== listEpoch) return
  if (new Set(result.map(row => row.brokerNo)).size !== result.length) throw new Error('報關行清單含重複識別值。')
  // Formal ordering is the server's documented C-collation order, not browser locale order.
  rows.value = result; appliedKeyword.value = term; rowsReady.value = true; pruneSelection(preferred)
}
async function loadRows(): Promise<void> {
  if (blocked.value || disposed) return
  loading.value = true; rowsReady.value = false; rows.value = []; selectedKeys.value = []; activeKey.value = ''; error.value = ''; notice.value = ''
  try { await reloadRows(trimAscii(keyword.value)) } catch (cause) { if (!disposed) error.value = fail(cause) }
  finally { loading.value = false }
}
function setFilter(value: IssueFilter): void { if (blocked.value) return; filterState.value = value; selectedKeys.value = []; activeKey.value = ''; error.value = ''; notice.value = '' }
function selectRow(row: Broker): void { if (blocked.value || !rowsReady.value) return; activeKey.value = row.brokerNo; selectedKeys.value = [row.brokerNo] }
function toggleRow(row: Broker, checked: boolean): void {
  if (blocked.value || !rowsReady.value) return
  selectedKeys.value = checked ? [...new Set([...selectedKeys.value, row.brokerNo])] : selectedKeys.value.filter(key => key !== row.brokerNo)
  activeKey.value = checked ? row.brokerNo : selectedKeys.value.at(-1) ?? ''
}
function toggleAll(checked: boolean): void { if (blocked.value || !rowsReady.value) return; selectedKeys.value = checked ? visibleRows.value.map(row => row.brokerNo) : []; activeKey.value = selectedKeys.value[0] ?? '' }
function draftFrom(row: Broker): Draft { return { brokerNo: row.brokerNo, contactorName: row.contactorName ?? '', telNo: row.telNo ?? '', faxNo: row.faxNo ?? '', email: row.email ?? '', address: row.address ?? '', bankNo: row.bankNo ?? '' } }
async function readItem(row: Broker): Promise<Broker> {
  if (mock.value) {
    const matches = readMockRows().filter(value => fold(value.brokerNo) === fold(row.brokerNo))
    if (matches.length !== 1 || matches[0]!.brokerNo !== row.brokerNo) throw new Error('報關行不存在或識別值有歧義。')
    return clone(matches[0]!)
  }
  const value = await responseDto(await request(path('item', row.brokerNo)))
  if (value.brokerNo !== row.brokerNo) throw new Error('報關行回應的原始識別值不一致。')
  return value
}
async function openForm(nextMode: Exclude<Mode, ''>): Promise<void> {
  if (blocked.value || disposed || nextMode === 'edit' && !canEdit.value) return
  previousModalFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null
  const epoch = ++formEpoch, selected = singleSelected.value ? clone(singleSelected.value) : null
  busy.value = true; error.value = ''; notice.value = ''
  try {
    const row = nextMode === 'new' ? null : await readItem(selected!)
    if (disposed || epoch !== formEpoch) return
    if (row) { upsert(row); if (row.issueState !== 0) throw new Error('報關行目前不是未簽核狀態，請重新查詢。') }
    sourceRow.value = row; draft.value = row ? draftFrom(row) : blankDraft(); draftOriginal.value = JSON.stringify(draft.value)
    mode.value = nextMode; formReady.value = true; formError.value = ''; formNotice.value = ''
  } catch (cause) { if (!disposed && epoch === formEpoch) error.value = fail(cause) }
  finally { busy.value = false; await restoreFocus() }
}
function normalizeNewBrokerNo(): void { if (mode.value === 'new' && formReady.value && !writing.value) draft.value.brokerNo = draft.value.brokerNo.toUpperCase() }
function validateDraft(): string {
  if (mode.value === 'new' && !trimAscii(draft.value.brokerNo)) return '請輸入 Broker No。'
  const limits: [keyof Draft, number, string][] = [['brokerNo', 50, 'Broker No'], ['contactorName', 50, 'Contactor'], ['telNo', 25, 'TEL'], ['faxNo', 25, 'Fax'], ['email', 255, 'Email'], ['address', 500, 'Address'], ['bankNo', 20, 'Bank No']]
  for (const [key, limit, label] of limits) if (draft.value[key].length > limit) return label + '最多' + limit + '字元。'
  if (/[\u0000-\u0008\u000B\u000C\u000E-\u001F\u007F-\u009F]/.test(draft.value.address)) return 'Address 含有不支援的控制字元。'
  return ''
}
function optionalValue(key: OptionalField): string | null { const old = sourceRow.value; return old && draft.value[key] === (old[key] ?? '') ? old[key] : draft.value[key] }
function payload(): BrokerWrite {
  return { brokerNo: mode.value === 'edit' && sourceRow.value ? sourceRow.value.brokerNo : trimAscii(draft.value.brokerNo), contactorName: optionalValue('contactorName'), telNo: optionalValue('telNo'), faxNo: optionalValue('faxNo'), email: optionalValue('email'), address: optionalValue('address'), bankNo: optionalValue('bankNo') }
}
function nextMockStamp(value: number | null): number { if (value !== null && value >= 99999999999) throw new Error('版本已達上限。'); return value === null ? 1 : value + 1 }
function mockSave(old: Broker | null, value: BrokerWrite, editing: boolean): Broker {
  const all = readMockRows(), now = new Date().toISOString(), actor = props.currentUserNo || 'DEV001'
  if (editing) {
    const matches = all.filter(row => fold(row.brokerNo) === fold(old!.brokerNo)), row = matches[0]
    if (matches.length !== 1 || !row || row.brokerNo !== old!.brokerNo || row.dataStamp !== old!.dataStamp || row.issueState !== 0) throw new Error('報關行資料已變更，請重新查詢。')
    Object.assign(row, value); row.dataStamp = nextMockStamp(row.dataStamp); row.reviser = actor; row.reviseDate = now
    saveMockRows(all); return clone(row)
  }
  const brokerNo = trimAscii(value.brokerNo).toUpperCase()
  if (all.some(row => fold(row.brokerNo) === fold(brokerNo))) throw new Error('Broker No 已存在。')
  const saved: Broker = { ...value, brokerNo, issueState: 0, creator: actor, createDate: now, dataStamp: 0, reviser: null, reviseDate: null }
  all.push(saved); saveMockRows(all); return clone(saved)
}
function adoptSaved(row: Broker, old: Broker | null, editing: boolean): void {
  if (editing && old && row.brokerNo !== old.brokerNo || row.issueState !== 0) throw new Error('儲存回應的原始識別值或狀態不一致。')
  upsert(row); sourceRow.value = clone(row); draft.value = draftFrom(row); draftOriginal.value = JSON.stringify(draft.value)
  mode.value = 'edit'; formReady.value = true; pruneSelection(row.brokerNo)
}
async function saveForm(close: boolean): Promise<void> {
  if (!mode.value || !formReady.value || writing.value || confirmationMessage.value !== null || disposed) return
  formError.value = validateDraft(); if (formError.value) return
  const epoch = formEpoch, editing = mode.value === 'edit', old = sourceRow.value ? clone(sourceRow.value) : null, value = payload(), term = appliedKeyword.value
  if (editing && !old) { formReady.value = false; formError.value = '原始資料未載入，請關閉後重新查詢。'; return }
  saving.value = true; formNotice.value = ''; notice.value = ''; error.value = ''; let observed = false
  try {
    let saved: Broker
    if (mock.value) saved = mockSave(old, value, editing)
    else {
      const response = await request(editing ? path('item', old!.brokerNo) : path(), { method: editing ? 'PUT' : 'POST', body: JSON.stringify(editing ? { ...value, dataStamp: old!.dataStamp } : value) })
      observed = true; saved = await responseDto(response)
    }
    if (disposed || epoch !== formEpoch) return
    observed = true; adoptSaved(saved, old, editing)
    formNotice.value = '報關行資料已儲存。'; notice.value = formNotice.value
    if (close) { mode.value = ''; formReady.value = false; ++formEpoch }
    try { await reloadRows(term, saved.brokerNo) } catch (cause) { if (!disposed) { error.value = fail(cause); formNotice.value = '資料已儲存；清單更新失敗，已保留儲存結果，請重新查詢。'; notice.value = formNotice.value } }
  } catch (cause) {
    if (!disposed && epoch === formEpoch) {
      formError.value = fail(cause)
      if (observed || !mock.value && (!(cause instanceof ApiFailure) || cause.status >= 500)) { formReady.value = false; formError.value += ' 儲存結果無法確認，請關閉後重新查詢。' }
    }
  } finally { saving.value = false; await restoreFocus() }
}
function mockTransition(row: Broker, action: 'delete' | 'approve'): DeleteResult | Broker {
  const all = readMockRows(), matches = all.filter(value => fold(value.brokerNo) === fold(row.brokerNo)), current = matches[0]
  if (matches.length !== 1 || !current || current.brokerNo !== row.brokerNo || current.dataStamp !== row.dataStamp || current.issueState !== row.issueState) throw new Error('報關行資料已變更，請重新查詢。')
  if (action === 'delete' && (current.issueState === 0 || current.issueState === -1)) { saveMockRows(all.filter(value => value.brokerNo !== current.brokerNo)); return { status: 204 } }
  if (action === 'approve' ? current.issueState !== 0 : current.issueState !== 2) throw new Error('報關行目前狀態不允許此操作。')
  current.dataStamp = nextMockStamp(current.dataStamp); current.issueState = action === 'approve' ? 2 : -1; current.reviser = props.currentUserNo || 'DEV001'; current.reviseDate = new Date().toISOString()
  saveMockRows(all); return action === 'approve' ? clone(current) : { status: 200, item: clone(current) }
}
async function removeBroker(row: Broker): Promise<DeleteResult> {
  if (mock.value) return mockTransition(row, 'delete') as DeleteResult
  const response = await request(path('item', row.brokerNo), { method: 'DELETE', body: JSON.stringify({ expectedIssueState: row.issueState, dataStamp: row.dataStamp }) })
  if (response.status === 204) return { status: 204 }
  return { status: 200, item: await responseDto(response) }
}
async function approveBroker(row: Broker): Promise<Broker> {
  if (mock.value) return mockTransition(row, 'approve') as Broker
  return responseDto(await request(path('approve', row.brokerNo), { method: 'POST', body: JSON.stringify({ expectedIssueState: row.issueState, dataStamp: row.dataStamp }) }))
}
async function runBatch(action: 'delete' | 'approve'): Promise<void> {
  if (blocked.value || !rowsReady.value || !selectedRows.value.length || disposed) return
  const targets = clone(selectedRows.value), term = appliedKeyword.value, epoch = ++operationEpoch
  const previous = document.activeElement instanceof HTMLElement ? document.activeElement : null
  busy.value = true; error.value = ''; notice.value = ''; let completed = 0, retired = 0, removed = 0
  try {
    if (!await requestConfirmation(action === 'approve' ? '確定核准選取的 ' + targets.length + ' 筆報關行資料？' : '確定刪除或停用選取的 ' + targets.length + ' 筆報關行資料？')) return
    if (disposed || epoch !== operationEpoch) return
    for (const row of targets) {
      if (action === 'delete') {
        const result = await removeBroker(row)
        if (disposed || epoch !== operationEpoch) return
        if (result.status === 204) { rows.value = rows.value.filter(value => value.brokerNo !== row.brokerNo); removed++ }
        else {
          if (result.item.brokerNo !== row.brokerNo || result.item.issueState !== -1) throw new Error('停用回應的原始識別值或狀態不一致。')
          upsert(result.item); retired++
        }
      } else {
        const saved = await approveBroker(row)
        if (disposed || epoch !== operationEpoch) return
        if (saved.brokerNo !== row.brokerNo || saved.issueState !== 2) throw new Error('核准回應的原始識別值或狀態不一致。')
        upsert(saved)
      }
      selectedKeys.value = selectedKeys.value.filter(key => key !== row.brokerNo); pruneSelection(); completed++
    }
    notice.value = action === 'approve' ? '已核准 ' + completed + ' 筆報關行資料。' + (mock.value ? '（DEV 本機模擬）' : '') : '已處理 ' + completed + ' 筆報關行資料：刪除 ' + removed + ' 筆、停用 ' + retired + ' 筆。'
    try { await reloadRows(term) } catch (cause) { if (!disposed) { rowsReady.value = false; error.value = fail(cause) + ' 操作已完成；清單更新失敗，請重新查詢。' } }
  } catch (cause) {
    if (disposed || epoch !== operationEpoch) return
    rowsReady.value = false; error.value = fail(cause) + ' 已確認完成 ' + completed + '/' + targets.length + ' 筆；請以重新查詢結果確認其餘資料。'
    try { await reloadRows(term) } catch (reloadError) { if (!disposed) error.value += ' ' + fail(reloadError) }
  } finally { busy.value = false; await restorePageFocus(previous) }
}
async function closeForm(): Promise<void> {
  if (writing.value || confirmationMessage.value !== null || disposed) return
  if (JSON.stringify(draft.value) !== draftOriginal.value && !await requestConfirmation('尚有未儲存的修改，確定放棄？')) { await restoreFocus(); return }
  if (disposed) return
  mode.value = ''; formReady.value = false; formError.value = ''; formNotice.value = ''; sourceRow.value = null; ++formEpoch; await restoreFocus()
}
function closePage(): void {
  if (blocked.value || disposed) return
  ++listEpoch; ++formEpoch; ++operationEpoch; rows.value = []; rowsReady.value = false; selectedKeys.value = []; activeKey.value = ''; error.value = ''; notice.value = ''; emit('close')
}
async function restorePageFocus(previous: HTMLElement | null): Promise<void> {
  await nextTick(); if (disposed || modalOpen.value || confirmationMessage.value !== null) return
  if (previous?.isConnected && !previous.matches(':disabled') && !previous.closest('[inert]')) previous.focus()
  else pageElement.value?.querySelector<HTMLElement>('.master-surface button:not(:disabled),.master-surface input:not(:disabled)')?.focus()
}
async function restoreFocus(): Promise<void> {
  await nextTick(); if (disposed || confirmationMessage.value !== null) return
  if (mode.value && !writing.value) {
    const selector = mode.value === 'new' ? '[data-focus="new"]' : '[data-focus="edit"]'
    const preferred = formElement.value?.querySelector<HTMLElement>(selector)
    const target = preferred && !preferred.matches(':disabled') ? preferred : formElement.value?.querySelector<HTMLElement>('input:not(:disabled):not([readonly]),textarea:not(:disabled),button:not(:disabled)')
    ;(target ?? formElement.value)?.focus()
  } else if (!mode.value) await restorePageFocus(previousModalFocus)
}
function keepModalFocus(event: KeyboardEvent): void {
  if (event.key !== 'Tab' || !mode.value || confirmationMessage.value !== null) return
  if (writing.value) { event.preventDefault(); return }
  if (!formElement.value?.contains(document.activeElement)) { event.preventDefault(); void restoreFocus() }
}
function trapTab(event: KeyboardEvent): void {
  if (confirmationMessage.value !== null) return
  if (writing.value) { event.preventDefault(); return }
  const modal = formElement.value; if (!modal) return
  const controls = Array.from(modal.querySelectorAll<HTMLElement>('input:not(:disabled),textarea:not(:disabled),button:not(:disabled)')).filter(element => element.offsetParent !== null)
  const first = controls[0], last = controls.at(-1)
  if (!first || !last) { event.preventDefault(); modal.focus(); return }
  if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus() }
  else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus() }
}
onMounted(() => {
  window.addEventListener('keydown', keepModalFocus, true)
  if (mock.value && !localStorage.getItem(storageKey)) saveMockRows(seedRows)
  void loadRows()
})
onBeforeUnmount(() => { disposed = true; ++listEpoch; ++formEpoch; ++operationEpoch; window.removeEventListener('keydown', keepModalFocus, true) })
</script>

<template>
  <section ref="pageElement" class="broker-page" aria-label="報關行基本資料">
    <div class="master-surface" :inert="modalOpen || writing || confirmationMessage!==null">
      <fieldset class="query-strip" :disabled="blocked">
        <div class="issue-filter" role="radiogroup" aria-label="簽核狀態">
          <label><input type="radio" name="broker-issue-state" :checked="filterState===0" @change="setFilter(0)">未簽核</label>
          <label><input type="radio" name="broker-issue-state" :checked="filterState===1" @change="setFilter(1)">簽核中</label>
          <label><input type="radio" name="broker-issue-state" :checked="filterState===2" @change="setFilter(2)">已簽核</label>
          <label><input type="radio" name="broker-issue-state" :checked="filterState===-1" @change="setFilter(-1)">不使用</label>
        </div>
        <label class="query-field">報關行編號<input v-model="keyword" class="kd-input query-input" maxlength="50" aria-label="報關行編號查詢" @keydown.enter.prevent="loadRows"></label>
        <button class="kd-button" type="button" @click="loadRows">查詢</button>
        <span v-if="mock" class="dev-badge">DEV 開發假資料</span>
      </fieldset>
      <p v-if="error" class="message error-message" role="alert">{{ error }}</p>
      <p v-if="notice" class="message notice-message" role="status">{{ notice }}</p>
      <fieldset class="win-group broker-panel"><legend>報關行資料</legend>
        <div class="panel-layout">
          <div class="grid-scroll"><table class="kd-grid"><thead><tr>
            <th class="check-cell"><input type="checkbox" aria-label="選取所有報關行" :checked="allSelected" :indeterminate="selectedRows.length>0 && !allSelected" :disabled="blocked || !rowsReady || !visibleRows.length" @change="toggleAll(($event.target as HTMLInputElement).checked)"></th>
            <th>報關行編號</th><th>連絡人</th><th>電話</th><th>傳真</th><th>Email</th><th>地址</th><th>銀行編號</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th>
          </tr></thead><tbody>
            <tr v-for="row in visibleRows" :key="row.brokerNo" :tabindex="blocked || !rowsReady ? -1 : 0" :aria-selected="selectedKeys.includes(row.brokerNo)" :class="{active:selectedKeys.includes(row.brokerNo)}" @click="selectRow(row)" @keydown.enter.prevent="selectRow(row)">
              <td class="check-cell" @click.stop><input type="checkbox" :checked="selectedKeys.includes(row.brokerNo)" :disabled="blocked || !rowsReady" :aria-label="'選取報關行 '+row.brokerNo" @change="toggleRow(row,($event.target as HTMLInputElement).checked)"></td>
              <td>{{ row.brokerNo }}</td><td>{{ row.contactorName }}</td><td>{{ row.telNo }}</td><td>{{ row.faxNo }}</td><td>{{ row.email }}</td><td>{{ row.address }}</td><td>{{ row.bankNo }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td>
            </tr>
            <tr v-if="!visibleRows.length"><td colspan="12" class="empty-row">{{ loading?'資料載入中…':error && !rowsReady?'報關行資料未能載入。':'沒有符合的報關行資料。' }}</td></tr>
          </tbody></table></div>
          <div class="vertical-actions">
            <button class="kd-button primary" type="button" :disabled="blocked" @click="openForm('new')">新增(A)</button>
            <button class="kd-button" type="button" :disabled="blocked || !canEdit" @click="openForm('edit')">修改(E)</button>
            <button class="kd-button" type="button" :disabled="blocked || !rowsReady || !selectedRows.length" @click="runBatch('delete')">刪除(D)</button>
            <button class="kd-button" type="button" :disabled="blocked || !rowsReady || !selectedRows.length" @click="runBatch('approve')">核准</button>
            <button class="kd-button close-button" type="button" :disabled="blocked" @click="closePage">關閉</button>
          </div>
        </div>
        <div class="grid-status">資料總筆數 {{ visibleRows.length }}</div>
      </fieldset>
    </div>
    <div v-if="mode" class="dialog-backdrop" @click.self="closeForm"><section ref="formElement" class="win-dialog" role="dialog" aria-modal="true" aria-label="報關行編輯" tabindex="-1" :inert="writing || confirmationMessage!==null" @keydown.esc.stop.prevent="closeForm" @keydown.tab="trapTab">
      <header class="dialog-title">{{ mode==='new'?'新增':'修改' }}報關行基本資料</header>
      <form class="dialog-form" novalidate @submit.prevent="saveForm(true)">
        <fieldset class="form-group" :disabled="writing || !formReady || confirmationMessage!==null"><legend>報關行資料</legend><div class="form-grid">
          <label>Broker No<input v-model="draft.brokerNo" class="kd-input" :readonly="mode==='edit'" maxlength="50" aria-label="Broker No" data-focus="new" @blur="normalizeNewBrokerNo"></label>
          <label>Contactor<input v-model="draft.contactorName" class="kd-input" maxlength="50" aria-label="Contactor" data-focus="edit"></label>
          <label>TEL<input v-model="draft.telNo" class="kd-input" maxlength="25" aria-label="TEL"></label>
          <label>Fax<input v-model="draft.faxNo" class="kd-input" maxlength="25" aria-label="Fax"></label>
          <label>Email<input v-model="draft.email" class="kd-input" maxlength="255" aria-label="Email"></label>
          <label>Bank No<input v-model="draft.bankNo" class="kd-input" maxlength="20" aria-label="Bank No"></label>
          <label class="wide-label">Address<textarea v-model="draft.address" class="kd-input" maxlength="500" rows="3" aria-label="Address"></textarea></label>
        </div></fieldset>
        <div class="audit-row">
          <label>建立人<input class="kd-input" :value="mode==='edit' ? sourceRow?.creator ?? '' : ''" readonly aria-label="報關行建立人"></label>
          <label>建立日期<input class="kd-input" :value="mode==='edit' ? formatDate(sourceRow?.createDate) : ''" readonly aria-label="報關行建立日期"></label>
          <label>修改人<input class="kd-input" :value="mode==='edit' ? sourceRow?.reviser ?? '' : ''" readonly aria-label="報關行修改人"></label>
          <label>修改日期<input class="kd-input" :value="mode==='edit' ? formatDate(sourceRow?.reviseDate) : ''" readonly aria-label="報關行修改日期"></label>
        </div>
        <p v-if="formError" class="message error-message" role="alert">{{ formError }}</p>
        <p v-if="formNotice" class="message notice-message" role="status">{{ formNotice }}</p>
        <footer class="dialog-actions">
          <button class="kd-button" type="button" :disabled="writing || confirmationMessage!==null" @click="closeForm">關閉</button>
          <button class="kd-button" type="button" :disabled="writing || !formReady || confirmationMessage!==null" @click="saveForm(false)">套用</button>
          <button class="kd-button primary" type="submit" :disabled="writing || !formReady || confirmationMessage!==null">確定</button>
        </footer>
      </form>
    </section></div>
    <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
  </section>
</template>

<style scoped>
.broker-page{box-sizing:border-box;display:flex;flex-direction:column;height:100%;min-height:0;padding:8px 10px;background:#F7F5EE;color:#26251F;font:13px/1.4 "Segoe UI","Microsoft JhengHei",sans-serif}.master-surface{display:flex;flex-direction:column;gap:6px;flex:1;min-height:0}
.query-strip{display:flex;align-items:center;flex-wrap:wrap;gap:8px;min-height:40px;margin:0;padding:5px 8px;border:1px solid #d4d0c7;background:#fff}.issue-filter{display:flex;align-items:center;flex-wrap:wrap;gap:10px;margin-right:8px}.issue-filter label,.query-field{display:inline-flex;align-items:center;gap:5px}.query-input{width:180px}.dev-badge{padding:2px 7px;border:1px solid #d8c373;background:#fff5ce;color:#76631b;font-size:11px}
.kd-input{box-sizing:border-box;width:100%;min-width:0;height:29px;padding:4px 7px;border:1px solid #aaa69e;background:#fff1bf;color:#333;font:inherit}.kd-input:focus{outline:2px solid var(--yellow,#E7C600);outline-offset:1px}.kd-input[readonly]{background:#efeeeb}.kd-button{min-height:30px;padding:4px 9px;border:1px solid #bdb9b0;border-radius:3px;background:linear-gradient(#fff,#efeeeb);color:#343331;font:inherit;white-space:nowrap;cursor:pointer}.kd-button:hover:not(:disabled){border-color:var(--yellow,#E7C600);background:#fff5d4}.kd-button:disabled{opacity:.48;cursor:default}.kd-button.primary{background:#3B3930;border-color:#3B3930;color:#fff}.kd-button.primary:hover:not(:disabled){background:#514d3e}
.win-group{display:flex;flex-direction:column;min-width:0;min-height:0;margin:0;padding:7px;border:1px solid #c8c3b8;background:#fff}.win-group legend{padding:0 5px;color:#59564e}.broker-panel{flex:1}.panel-layout{display:flex;gap:8px;flex:1;min-height:0}.grid-scroll{flex:1;min-width:0;min-height:0;overflow:auto;border:1px solid #c5c1b9;scrollbar-width:thin;scrollbar-color:#7775 transparent}.kd-grid{width:max-content;min-width:100%;border-collapse:collapse;background:#fff;font-size:12px}.kd-grid th{position:sticky;top:0;z-index:1;background:#EEEAE0;color:#3B3930;text-align:left}.kd-grid td,.kd-grid th{padding:4px 7px;border:1px solid #d4d0c7;white-space:nowrap}.kd-grid td{max-width:320px;overflow:hidden;text-overflow:ellipsis}.kd-grid tbody tr{cursor:pointer}.kd-grid tbody tr:hover{background:#fff8e2}.kd-grid tr.active{background:#ffedb1}.kd-grid tbody tr:focus-visible{outline:2px solid var(--yellow,#E7C600);outline-offset:-2px}.check-cell{width:26px;text-align:center}.kd-grid input[type=checkbox],.issue-filter input{accent-color:var(--yellow,#E7C600)}.vertical-actions{display:flex;flex-direction:column;gap:6px;flex:0 0 88px}.close-button{margin-top:auto}.grid-status{padding:3px 2px 0;color:#777366;font-size:11px}.empty-row{height:44px;text-align:center;color:#777366}
.message{margin:0;padding:6px 8px;border:1px solid;overflow-wrap:anywhere}.error-message{border-color:#d99b86;background:#fff0e9;color:#873820}.notice-message{border-color:#d8c79a;background:#fff8dc;color:#65541d}
.dialog-backdrop{position:fixed;inset:0;z-index:1200;display:flex;align-items:center;justify-content:center;padding:14px;background:#17171770}.win-dialog{box-sizing:border-box;display:flex;flex-direction:column;gap:8px;width:min(650px,96vw);max-height:94vh;overflow:auto;padding:12px;border:1px solid #aaa69e;background:#F7F5EE;box-shadow:0 8px 25px #0004}.dialog-title{padding-bottom:7px;border-bottom:1px solid #d4d0c7;font-size:16px;font-weight:600}.dialog-form{display:flex;flex-direction:column;gap:8px}.form-group{margin:0;padding:10px;border:1px solid #c9c4b9}.form-grid{display:grid;grid-template-columns:1fr 1fr;gap:10px 16px}.form-grid>label,.audit-row>label{display:flex;flex-direction:column;gap:4px}.wide-label{grid-column:1/-1}.wide-label textarea{height:72px;resize:vertical}.audit-row{display:grid;grid-template-columns:1fr 1fr;gap:7px 10px;padding:7px;border:1px solid #d0cabf;background:#f1eee6;color:#777366;font-size:11px}.audit-row .kd-input{font-size:11px}.dialog-actions{display:flex;justify-content:flex-end;gap:7px}
@media(max-width:900px){.query-strip{gap:6px}.query-field{flex-wrap:wrap}.form-grid{grid-template-columns:1fr}.wide-label{grid-column:auto}.vertical-actions{flex-basis:78px}}
@media(max-width:500px){.issue-filter{gap:7px}.query-input{width:min(180px,55vw)}.audit-row{grid-template-columns:1fr}.win-dialog{padding:9px}.broker-page{padding:6px}.vertical-actions{flex-basis:74px}}
</style>
