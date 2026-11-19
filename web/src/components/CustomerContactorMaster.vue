<script setup lang="ts">
// Offline candidate. Relative imports target web/src/components after Root integration.
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'
import { apiErrorMessage } from '../utils/apiError'

type Mode = 'new' | 'edit' | 'copy' | ''
type OptionalField = 'telNo' | 'faxNo' | 'email' | 'title' | 'deptName' | 'description'
interface Customer { customerNo: string; customerName: string | null; customerSName: string | null; issueState: number | null }
interface Contactor {
  contactorName: string; customerNo: string; telNo: string | null; faxNo: string | null; email: string | null; title: string | null; deptName: string | null
  creator: string | null; createDate: string | null; description: string | null; dataStamp: number | null; reviser: string | null; reviseDate: string | null
}
interface Draft { contactorName: string; telNo: string; faxNo: string; email: string; title: string; deptName: string; description: string }
interface LegacyDevContactor {
  customerNo: string; contactorName: string; telNo: string | null; faxNo: string | null; email: string | null; title: string | null; deptName: string | null
  creator: string | null; createdAt: string | null; description: string | null; dataStamp: number | null; reviser: string | null; revisedAt: string | null
}
const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const apiRoot = 'api/ent/customer-contactors'
const contactorStorageKey = 'ymes-dev-mock:ent-customer-contactors:v1', customerStorageKey = 'ymes-dev-mock:ent-customer-basis:v1'
const seedCustomers: Customer[] = [
  { customerNo: 'DEV-C001', customerName: '開發用客戶甲股份有限公司', customerSName: '客戶甲', issueState: 0 },
  { customerNo: 'DEV-C002', customerName: '開發用客戶乙有限公司', customerSName: '客戶乙', issueState: 2 },
  { customerNo: 'DEV-C003', customerName: '開發用客戶丙企業', customerSName: '客戶丙', issueState: -1 },
]
const seedContactors: LegacyDevContactor[] = [
  { ...blankLegacyContactor(), customerNo: 'DEV-C001', contactorName: '林怡君', telNo: '02-0000-0011', faxNo: '02-0000-0012', email: 'dev.contact.a@example.test', title: '採購', deptName: '採購部', description: 'Development 假資料', creator: 'DEV001', createdAt: '2026-11-01T08:00:00+08:00', dataStamp: 1 },
  { ...blankLegacyContactor(), customerNo: 'DEV-C001', contactorName: '陳志明', telNo: '02-0000-0021', email: 'dev.contact.b@example.test', title: '工程師', deptName: '工程部', description: '', creator: 'DEV001', createdAt: '2026-11-02T08:00:00+08:00', dataStamp: 1 },
  { ...blankLegacyContactor(), customerNo: 'DEV-C002', contactorName: '佐藤花子', telNo: '+81-3-0000-0001', email: 'dev.contact.c@example.test', title: '採購經理', deptName: '採購部', description: 'Development 假資料', creator: 'DEV001', createdAt: '2026-11-03T08:00:00+08:00', dataStamp: 1 },
]


const customer = ref<Customer | null>(null), rows = ref<Contactor[]>([]), rowsOwner = ref<string | null>(null), rowsReady = ref(false)
const selectedKeys = ref<string[]>([]), activeKey = ref(''), loading = ref(false), busy = ref(false), saving = ref(false)
const error = ref(''), notice = ref(''), pickerOpen = ref(false), pickerLoading = ref(false), pickerReady = ref(false), pickerError = ref(''), pickerKeyword = ref(''), customers = ref<Customer[]>([])
const mode = ref<Mode>(''), sourceRow = ref<Contactor | null>(null), formCustomerNo = ref(''), draft = ref<Draft>(blankDraft()), draftOriginal = ref(''), formReady = ref(false), formError = ref(''), formNotice = ref('')
const pageElement = ref<HTMLElement | null>(null), formElement = ref<HTMLElement | null>(null), pickerElement = ref<HTMLElement | null>(null)
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()
let listEpoch = 0, formEpoch = 0, pickerEpoch = 0
let previousModalFocus: HTMLElement | null = null
const writing = computed(() => busy.value || saving.value)
const modalOpen = computed(() => !!mode.value || pickerOpen.value)
const blocked = computed(() => loading.value || writing.value || pickerLoading.value || modalOpen.value || confirmationMessage.value !== null)
const ownerReady = computed(() => rowsReady.value && !!customer.value && rowsOwner.value !== null && sameCustomer(customer.value.customerNo, rowsOwner.value))
const visibleRows = computed(() => ownerReady.value ? rows.value : [])
const singleSelected = computed(() => selectedKeys.value.length === 1 ? visibleRows.value.find(row => rowKey(row) === selectedKeys.value[0]) ?? null : null)
const selectedRows = computed(() => visibleRows.value.filter(row => selectedKeys.value.includes(rowKey(row))))
const allSelected = computed(() => !!visibleRows.value.length && selectedRows.value.length === visibleRows.value.length)
const visibleCustomers = computed(() => {
  const query = pickerKeyword.value.trim().toLowerCase()
  return customers.value.filter(row => !query || [row.customerNo, row.customerName ?? '', row.customerSName ?? ''].some(value => value.toLowerCase().includes(query)))
})
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function blankDraft(): Draft { return { contactorName: '', telNo: '', faxNo: '', email: '', title: '', deptName: '', description: '' } }
function blankLegacyContactor(): LegacyDevContactor { return { customerNo: '', contactorName: '', telNo: '', faxNo: '', email: '', title: '', deptName: '', creator: null, createdAt: null, description: '', dataStamp: 0, reviser: null, revisedAt: null } }
function fold(value: string): string { return value.replace(/^ +| +$/g, '').toLowerCase() }
function sameCustomer(a: string, b: string): boolean { return fold(a) === fold(b) }
function rowKey(row: Pick<Contactor, 'customerNo' | 'contactorName'>): string { return JSON.stringify([row.customerNo, row.contactorName]) }
function logicalKey(row: Pick<Contactor, 'customerNo' | 'contactorName'>): string { return JSON.stringify([fold(row.customerNo), fold(row.contactorName)]) }
function fail(value: unknown): string { return value instanceof Error ? value.message : '操作失敗，請重新查詢。' }
function stateName(value: number | null): string { return value === null ? '' : ({ 0: '未簽核', 1: '簽核中', 2: '已簽核', '-1': '不使用' } as Record<string, string>)[String(value)] ?? String(value) }
function path(resource = '', customerNo?: string, contactorName?: string): string {
  const query = new URLSearchParams()
  if (customerNo !== undefined) query.set('customerNo', customerNo)
  if (contactorName !== undefined) query.set('contactorName', contactorName)
  return apiRoot + (resource ? '/' + resource : '') + (query.size ? '?' + query.toString() : '')
}
class ApiFailure extends Error { status: number; constructor(status: number, message: string) { super(message); this.status = status } }
async function api<T>(resource: string, init: RequestInit = {}): Promise<T> {
  if (!props.apiBaseUrl.trim()) throw new Error('目前環境尚未設定 API 位址。')
  const url = new URL(resource.replace(/^\/+/, ''), new URL(props.apiBaseUrl.replace(/\/+$/, '') + '/', window.location.origin))
  const headers = new Headers(init.headers); if (init.body) headers.set('Content-Type', 'application/json')
  const response = await fetch(url, { credentials: 'include', ...init, headers })
  if (response.status === 401) { window.location.assign(import.meta.env.BASE_URL); throw new ApiFailure(401, '登入狀態已逾期。') }
  if (!response.ok) throw new ApiFailure(response.status, await apiErrorMessage(response))
  return response.status === 204 ? undefined as T : await response.json() as T
}
function object(value: unknown): Record<string, unknown> { if (value === null || typeof value !== 'object' || Array.isArray(value)) throw new Error('伺服器回應不完整。'); return value as Record<string, unknown> }
function text(row: Record<string, unknown>, key: string): string | null {
  if (!Object.hasOwn(row, key) || row[key] !== null && typeof row[key] !== 'string') throw new Error('伺服器文字欄位回應不完整。')
  return row[key] as string | null
}
function requiredText(row: Record<string, unknown>, key: string): string { const value = text(row, key); if (value === null) throw new Error('伺服器識別欄位回應不完整。'); return value }
function number(row: Record<string, unknown>, key: string): number | null {
  if (!Object.hasOwn(row, key) || row[key] !== null && (typeof row[key] !== 'number' || !Number.isSafeInteger(row[key]))) throw new Error('伺服器數值欄位回應不完整。')
  return row[key] as number | null
}
function listFrom(value: unknown): unknown[] { const list = object(value).items; if (!Array.isArray(list)) throw new Error('伺服器清單回應不完整。'); return list }
function normalizeCustomer(value: unknown): Customer { const row = object(value); return { customerNo: requiredText(row, 'customerNo'), customerName: text(row, 'customerName'), customerSName: text(row, 'customerSName'), issueState: number(row, 'issueState') } }
function normalizeContactor(value: unknown): Contactor {
  const row = object(value)
  return { contactorName: requiredText(row, 'contactorName'), customerNo: requiredText(row, 'customerNo'), telNo: text(row, 'telNo'), faxNo: text(row, 'faxNo'), email: text(row, 'email'),
    title: text(row, 'title'), deptName: text(row, 'deptName'), creator: text(row, 'creator'), createDate: text(row, 'createDate'), description: text(row, 'description'),
    dataStamp: number(row, 'dataStamp'), reviser: text(row, 'reviser'), reviseDate: text(row, 'reviseDate') }
}
function formatDate(value: string | null | undefined): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : new Intl.DateTimeFormat('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23' }).format(date)
}
// Cache aliases are confined to DEV. Production always uses the strict thirteen-field DTO.
function fromLegacyDev(value: unknown): Contactor {
  const row = object(value)
  return normalizeContactor({ ...row, createDate: Object.hasOwn(row, 'createDate') ? row.createDate : row.createdAt, reviseDate: Object.hasOwn(row, 'reviseDate') ? row.reviseDate : row.revisedAt })
}
function readMockRows(): Contactor[] {
  try { const raw = localStorage.getItem(contactorStorageKey); if (raw) { const data: unknown = JSON.parse(raw); if (Array.isArray(data)) return data.map(fromLegacyDev) } } catch { /* isolated historical DEV storage */ }
  return seedContactors.map(fromLegacyDev)
}
function saveMockRows(value: Contactor[]): void {
  localStorage.setItem(contactorStorageKey, JSON.stringify(value.map(row => { const { createDate, reviseDate, ...rest } = row; return { ...rest, createdAt: createDate, revisedAt: reviseDate } })))
}
function readMockCustomers(): Customer[] {
  try { const raw = localStorage.getItem(customerStorageKey); if (raw) { const data: unknown = JSON.parse(raw); if (Array.isArray(data)) return data.map(normalizeCustomer) } } catch { /* shared DEV Customer store */ }
  return clone(seedCustomers)
}
function clearRows(): void { ++listEpoch; loading.value = false; rowsReady.value = false; rowsOwner.value = null; rows.value = []; selectedKeys.value = []; activeKey.value = '' }
function upsert(row: Contactor): void { const i = rows.value.findIndex(value => rowKey(value) === rowKey(row)); if (i < 0) rows.value.push(clone(row)); else rows.value[i] = clone(row); rows.value.sort((a, b) => a.contactorName.localeCompare(b.contactorName)) }
async function reloadRows(customerNo: string, preferred = ''): Promise<void> {
  const epoch = ++listEpoch
  const result = mock.value ? readMockRows().filter(row => sameCustomer(row.customerNo, customerNo)) : listFrom(await api<unknown>(path('', customerNo))).map(normalizeContactor)
  if (epoch !== listEpoch || customer.value?.customerNo !== customerNo) return
  if (result.some(row => !sameCustomer(row.customerNo, customerNo)) || new Set(result.map(logicalKey)).size !== result.length) throw new Error('連絡人清單歸屬或識別值不一致。')
  rows.value = result.sort((a, b) => a.contactorName.localeCompare(b.contactorName)); rowsOwner.value = customerNo; rowsReady.value = true
  const keys = new Set(result.map(rowKey)); selectedKeys.value = preferred && keys.has(preferred) ? [preferred] : selectedKeys.value.filter(key => keys.has(key))
  activeKey.value = preferred && keys.has(preferred) ? preferred : selectedKeys.value.includes(activeKey.value) ? activeKey.value : selectedKeys.value[0] ?? ''
}
async function loadRows(): Promise<void> {
  if (blocked.value || !customer.value) return
  const no = customer.value.customerNo; loading.value = true; error.value = ''; notice.value = ''
  try { await reloadRows(no) } catch (cause) { error.value = fail(cause); rowsReady.value = false }
  finally { loading.value = false }
}
function selectRow(row: Contactor): void { if (blocked.value || !ownerReady.value) return; activeKey.value = rowKey(row); selectedKeys.value = [activeKey.value] }
function toggleRow(row: Contactor, checked: boolean): void {
  if (blocked.value || !ownerReady.value) return
  const key = rowKey(row); selectedKeys.value = checked ? [...new Set([...selectedKeys.value, key])] : selectedKeys.value.filter(value => value !== key)
  activeKey.value = checked ? key : selectedKeys.value.at(-1) ?? ''
}
function toggleAll(checked: boolean): void { if (blocked.value || !ownerReady.value) return; selectedKeys.value = checked ? visibleRows.value.map(rowKey) : []; activeKey.value = selectedKeys.value[0] ?? '' }
async function openCustomerPicker(): Promise<void> {
  if (blocked.value) return
  previousModalFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null
  const epoch = ++pickerEpoch; pickerOpen.value = true; pickerKeyword.value = ''; pickerError.value = ''; pickerReady.value = false; pickerLoading.value = true; customers.value = []
  try {
    const list = mock.value ? readMockCustomers() : listFrom(await api<unknown>(path('lookups/customers'))).map(normalizeCustomer)
    if (epoch !== pickerEpoch || !pickerOpen.value) return
    if (new Set(list.map(row => fold(row.customerNo))).size !== list.length) throw new Error('客戶清單含歧義識別值，請重新查詢。')
    customers.value = list; pickerReady.value = true
  } catch (cause) { if (epoch === pickerEpoch) pickerError.value = fail(cause) }
  finally { if (epoch === pickerEpoch) { pickerLoading.value = false; await restoreFocus() } }
}
async function closeCustomerPicker(): Promise<void> {
  if (writing.value || pickerLoading.value || confirmationMessage.value !== null) return
  pickerOpen.value = false; pickerReady.value = false; ++pickerEpoch; await restoreFocus()
}
async function chooseCustomer(row: Customer): Promise<void> {
  if (writing.value || pickerLoading.value || !pickerOpen.value || !pickerReady.value || confirmationMessage.value !== null) return
  clearRows(); customer.value = clone(row); error.value = ''; notice.value = ''
  await closeCustomerPicker(); await loadRows()
}
function draftFrom(row: Contactor): Draft { return { contactorName: row.contactorName, telNo: row.telNo ?? '', faxNo: row.faxNo ?? '', email: row.email ?? '', title: row.title ?? '', deptName: row.deptName ?? '', description: row.description ?? '' } }
async function readItem(row: Contactor): Promise<Contactor> {
  if (mock.value) {
    const matches = readMockRows().filter(value => logicalKey(value) === logicalKey(row))
    if (matches.length !== 1) throw new Error('連絡人已不存在或識別值有歧義。')
    return clone(matches[0]!)
  }
  const result = normalizeContactor(await api<unknown>(path('item', row.customerNo, row.contactorName)))
  if (logicalKey(result) !== logicalKey(row)) throw new Error('連絡人回應歸屬不一致。')
  return result
}
async function openForm(nextMode: Exclude<Mode, ''>): Promise<void> {
  if (blocked.value || !ownerReady.value || !customer.value || nextMode !== 'new' && !singleSelected.value) return
  previousModalFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null
  const epoch = ++formEpoch, owner = clone(customer.value), selected = singleSelected.value ? clone(singleSelected.value) : null
  busy.value = true; error.value = ''; notice.value = ''
  try {
    const row = nextMode === 'new' ? null : await readItem(selected!)
    if (epoch !== formEpoch) return
    if (row && !sameCustomer(row.customerNo, owner.customerNo)) throw new Error('連絡人回應歸屬不一致。')
    if (row) upsert(row)
    sourceRow.value = row; formCustomerNo.value = nextMode === 'edit' && row ? row.customerNo : owner.customerNo
    draft.value = row ? draftFrom(row) : blankDraft(); if (nextMode === 'copy') draft.value.contactorName = ''
    draftOriginal.value = JSON.stringify(draft.value); formError.value = ''; formNotice.value = ''; formReady.value = true; mode.value = nextMode
  } catch (cause) { error.value = fail(cause) }
  finally { busy.value = false; await restoreFocus() }
}
function validateDraft(): string {
  if (mode.value !== 'edit' && !draft.value.contactorName.trim() || draft.value.contactorName.length > 50) return '請填寫50字以內的連絡人姓名。'
  const limits: [OptionalField, number, string][] = [['telNo', 25, '電話'], ['faxNo', 25, '傳真'], ['email', 50, 'Email'], ['title', 25, '職稱'], ['deptName', 25, '部門'], ['description', 255, '說明']]
  for (const [key, limit, label] of limits) if (draft.value[key].length > limit) return label + '最多' + limit + '字元。'
  return ''
}
function optionalValue(key: OptionalField): string | null {
  const old = sourceRow.value
  if (old && draft.value[key] === (old[key] ?? '')) return old[key]
  // Preserve untouched source NULL/empty-string; submit changed text literally, including empty-string.
  return draft.value[key]
}
function payload(): Pick<Contactor, 'contactorName' | OptionalField> {
  return { contactorName: mode.value === 'edit' && sourceRow.value ? sourceRow.value.contactorName : draft.value.contactorName.replace(/^ +| +$/g, ''),
    telNo: optionalValue('telNo'), faxNo: optionalValue('faxNo'), email: optionalValue('email'), title: optionalValue('title'), deptName: optionalValue('deptName'), description: optionalValue('description') }
}
function requireMockParent(no: string): void {
  if (readMockCustomers().filter(row => sameCustomer(row.customerNo, no)).length !== 1) throw new Error('客戶不存在或識別值有歧義；本次未保存。')
}
function mockSave(owner: string, before: Contactor | null, value: ReturnType<typeof payload>, editing: boolean): Contactor {
  requireMockParent(owner)
  const all = readMockRows(), now = new Date().toISOString(), actor = props.currentUserNo || 'DEV001'
  if (editing) {
    const matches = all.filter(row => before && logicalKey(row) === logicalKey(before))
    if (matches.length !== 1 || matches[0]!.dataStamp !== before?.dataStamp) throw new Error('連絡人資料已變更，請重新查詢。')
    const row = matches[0]!
    if (row.dataStamp !== null && (!Number.isSafeInteger(row.dataStamp) || row.dataStamp >= 99999999999)) throw new Error('版本已達上限。')
    Object.assign(row, value); row.dataStamp = row.dataStamp === null ? 1 : row.dataStamp + 1; row.reviser = actor; row.reviseDate = now
    saveMockRows(all); return clone(row)
  }
  const identity = { customerNo: owner, contactorName: value.contactorName }
  if (all.some(row => logicalKey(row) === logicalKey(identity))) throw new Error('此客戶已存在同名連絡人。')
  const saved: Contactor = { ...identity, ...value, creator: actor, createDate: now, reviser: null, reviseDate: null, dataStamp: 0 }
  all.push(saved); saveMockRows(all); return clone(saved)
}
function adoptSaved(row: Contactor, old: Contactor | null, editing: boolean): void {
  if (!customer.value || !sameCustomer(row.customerNo, customer.value.customerNo) || editing && old && rowKey(row) !== rowKey(old)) throw new Error('保存回應歸屬或原始識別值不一致。')
  upsert(row); rowsOwner.value = customer.value.customerNo; rowsReady.value = true
  sourceRow.value = clone(row); formCustomerNo.value = row.customerNo; draft.value = draftFrom(row); draftOriginal.value = JSON.stringify(draft.value)
  activeKey.value = rowKey(row); selectedKeys.value = [activeKey.value]; mode.value = 'edit'; formReady.value = true
}
async function saveForm(close: boolean): Promise<void> {
  if (!mode.value || !formReady.value || !customer.value || writing.value || pickerOpen.value || confirmationMessage.value !== null) return
  formError.value = validateDraft(); if (formError.value) return
  const editing = mode.value === 'edit', old = sourceRow.value ? clone(sourceRow.value) : null, owner = formCustomerNo.value, customerNo = customer.value.customerNo, data = payload()
  saving.value = true; formNotice.value = ''; notice.value = ''; error.value = ''; let observed = false
  try {
    const result = mock.value ? mockSave(owner, old, data, editing) : await api<unknown>(editing ? path('item', owner, old!.contactorName) : path('', owner), { method: editing ? 'PUT' : 'POST', body: JSON.stringify(editing ? { ...data, dataStamp: old!.dataStamp } : data) })
    observed = true; const saved = normalizeContactor(result); adoptSaved(saved, old, editing)
    formNotice.value = '連絡人資料已儲存。'; notice.value = formNotice.value
    if (close) { mode.value = ''; formReady.value = false; ++formEpoch }
    try { await reloadRows(customerNo, rowKey(saved)) } catch (cause) { error.value = fail(cause); formNotice.value = '資料已儲存；清單更新失敗，已保留保存結果。'; notice.value = formNotice.value }
  } catch (cause) {
    formError.value = fail(cause)
    if (observed || !mock.value && (!(cause instanceof ApiFailure) || cause.status >= 500)) { formReady.value = false; formError.value += ' 保存結果無法確認，請關閉後重新查詢。' }
  } finally { saving.value = false; await restoreFocus() }
}
async function deleteSelected(): Promise<void> {
  if (blocked.value || !ownerReady.value || !customer.value || !selectedRows.value.length) return
  const selected = clone(selectedRows.value), owner = customer.value.customerNo, focus = document.activeElement instanceof HTMLElement ? document.activeElement : null
  let completed = 0; busy.value = true; error.value = ''; notice.value = ''
  try {
    if (!await requestConfirmation('確定刪除選取的' + selected.length + '筆連絡人資料？')) return
    for (const row of selected) {
      if (mock.value) {
        requireMockParent(row.customerNo)
        const all = readMockRows(), matches = all.filter(value => logicalKey(value) === logicalKey(row))
        if (matches.length !== 1 || matches[0]!.dataStamp !== row.dataStamp) throw new Error('連絡人資料已變更，請重新查詢。')
        saveMockRows(all.filter(value => rowKey(value) !== rowKey(matches[0]!)))
      } else await api<void>(path('item', row.customerNo, row.contactorName), { method: 'DELETE', body: JSON.stringify({ dataStamp: row.dataStamp }) })
      rows.value = rows.value.filter(value => rowKey(value) !== rowKey(row)); selectedKeys.value = selectedKeys.value.filter(key => key !== rowKey(row)); completed++
    }
    activeKey.value = selectedKeys.value[0] ?? ''; notice.value = '已刪除 ' + completed + ' 筆連絡人。'
  } catch (cause) {
    error.value = fail(cause) + ' 已確認完成 ' + completed + '/' + selected.length + ' 筆；請以重新查詢結果確認其餘資料。'; rowsReady.value = false
    try { await reloadRows(owner) } catch (reloadError) { error.value += ' ' + fail(reloadError) }
  } finally { busy.value = false; await restorePageFocus(focus) }
}
async function closeForm(): Promise<void> {
  if (writing.value || pickerOpen.value || confirmationMessage.value !== null) return
  if (JSON.stringify(draft.value) !== draftOriginal.value && !await requestConfirmation('尚有未儲存的修改，確定放棄？')) { await restoreFocus(); return }
  mode.value = ''; formReady.value = false; ++formEpoch; await restoreFocus()
}
function closePage(): void {
  if (blocked.value) return
  clearRows(); customer.value = null; error.value = ''; notice.value = ''; ++pickerEpoch; ++formEpoch
  emit('close') // Root may wire framework Home; without it, the native prototype's cleared-page result remains.
}
function currentModal(): HTMLElement | null { return pickerOpen.value ? pickerElement.value : mode.value ? formElement.value : null }
async function restorePageFocus(previous: HTMLElement | null): Promise<void> {
  await nextTick(); if (modalOpen.value || confirmationMessage.value !== null) return
  if (previous?.isConnected && !previous.matches(':disabled') && !previous.closest('[inert]')) previous.focus()
  else pageElement.value?.querySelector<HTMLElement>('.master-surface button:not(:disabled),.master-surface input:not(:disabled)')?.focus()
}
async function restoreFocus(): Promise<void> {
  await nextTick(); if (confirmationMessage.value !== null) return
  const modal = currentModal()
  if (modal && !writing.value && !pickerLoading.value) (modal.querySelector<HTMLElement>('input:not(:disabled):not([readonly]),textarea:not(:disabled),button:not(:disabled)') ?? modal).focus()
  else if (!modal) await restorePageFocus(previousModalFocus)
}
function keepModalFocus(event: KeyboardEvent): void {
  if (event.key !== 'Tab' || !modalOpen.value || confirmationMessage.value !== null) return
  if (writing.value || pickerLoading.value) { event.preventDefault(); return }
  if (!currentModal()?.contains(document.activeElement)) { event.preventDefault(); void restoreFocus() }
}
function trapTab(event: KeyboardEvent): void {
  const modal = currentModal(); if (!modal) return
  const controls = Array.from(modal.querySelectorAll<HTMLElement>('input:not(:disabled),textarea:not(:disabled),button:not(:disabled)')).filter(element => element.offsetParent !== null)
  const first = controls[0], last = controls.at(-1)
  if (!first || !last) { event.preventDefault(); modal.focus(); return }
  if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus() }
  else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus() }
}
onMounted(() => {
  window.addEventListener('keydown', keepModalFocus, true)
  if (mock.value && !localStorage.getItem(contactorStorageKey)) saveMockRows(seedContactors.map(fromLegacyDev))
})
onBeforeUnmount(() => { ++listEpoch; ++formEpoch; ++pickerEpoch; window.removeEventListener('keydown', keepModalFocus, true) })
</script>

<template>
  <section ref="pageElement" class="contactor-page" aria-label="客戶連絡人資料">
    <div class="master-surface" :inert="modalOpen || writing || confirmationMessage!==null">
      <fieldset class="query-strip" :disabled="blocked">
        <label class="query-field">客戶編號<input class="kd-input customer-input" :value="customer?.customerNo ?? ''" readonly aria-label="目前客戶編號"></label>
        <button class="kd-button" type="button" aria-label="選擇客戶" @click="openCustomerPicker">...</button>
        <span class="customer-name">{{ customer?.customerName }} {{ customer?.customerSName }}</span>
        <button class="kd-button" type="button" :disabled="!customer" @click="loadRows">重新查詢</button>
        <span v-if="mock" class="dev-badge">DEV</span>
      </fieldset>
      <fieldset class="win-group contact-panel"><legend>客戶連絡人資料</legend><div class="panel-layout">
        <div class="grid-scroll"><table class="kd-grid"><thead><tr>
          <th class="check-cell"><input type="checkbox" :checked="allSelected" :indeterminate="selectedRows.length>0 && !allSelected" :disabled="blocked || !ownerReady" aria-label="選取全部連絡人" @change="toggleAll(($event.target as HTMLInputElement).checked)"></th>
          <th>ContactorName</th><th>CustomerNo</th><th>TelNo</th><th>FaxNo</th><th>Email</th><th>Title</th><th>DeptName</th><th>Creator</th><th>CreateDate</th><th>Description</th><th>Reviser</th><th>ReviseDate</th>
        </tr></thead><tbody>
          <tr v-for="row in visibleRows" :key="rowKey(row)" :class="{active:activeKey===rowKey(row)}" :aria-selected="selectedKeys.includes(rowKey(row))" @click="selectRow(row)">
            <td class="check-cell" @click.stop><input type="checkbox" :checked="selectedKeys.includes(rowKey(row))" :disabled="blocked || !ownerReady" :aria-label="'選取 '+row.contactorName" @change="toggleRow(row,($event.target as HTMLInputElement).checked)"></td>
            <td>{{ row.contactorName }}</td><td>{{ row.customerNo }}</td><td>{{ row.telNo }}</td><td>{{ row.faxNo }}</td><td>{{ row.email }}</td><td>{{ row.title }}</td><td>{{ row.deptName }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.description }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td>
          </tr>
          <tr v-if="!visibleRows.length"><td colspan="13" class="empty-row">{{ !customer?'請先選擇客戶。':loading?'資料載入中…':!ownerReady?'清單尚未載入，請重新查詢。':'此客戶目前沒有連絡人資料。' }}</td></tr>
        </tbody></table></div>
        <aside class="vertical-actions">
          <button class="kd-button primary" type="button" :disabled="blocked || !ownerReady" @click="openForm('new')">新增 (A)</button>
          <button class="kd-button" type="button" :disabled="blocked || !ownerReady || !singleSelected" @click="openForm('edit')">修改 (E)</button>
          <button class="kd-button" type="button" :disabled="blocked || !ownerReady || !selectedRows.length" @click="deleteSelected">刪除 (D)</button>
          <button class="kd-button" type="button" :disabled="blocked || !ownerReady || !singleSelected" @click="openForm('copy')">複製 (C)</button>
        </aside>
      </div><div class="grid-status">資料總筆數 {{ visibleRows.length }}<span v-if="customer">　客戶狀態 {{ stateName(customer.issueState) }}</span></div></fieldset>
      <p v-if="error" class="message error-message" role="alert">{{ error }}</p><p v-if="notice" class="message notice-message" role="status">{{ notice }}</p>
      <div class="footer-actions"><button class="kd-button" type="button" :disabled="blocked" @click="closePage">關閉</button></div>
    </div>

    <div v-if="pickerOpen" class="dialog-backdrop" @click.self="closeCustomerPicker"><section ref="pickerElement" class="win-dialog picker-dialog" role="dialog" aria-modal="true" aria-label="選擇客戶資料" tabindex="-1" :inert="writing || pickerLoading || confirmationMessage!==null" @keydown.esc.stop.prevent="closeCustomerPicker" @keydown.tab="trapTab">
      <header class="dialog-title">選擇客戶資料</header>
      <fieldset class="picker-fields" :disabled="writing || pickerLoading || confirmationMessage!==null">
        <input v-model="pickerKeyword" class="kd-input" type="search" aria-label="客戶查找關鍵字" placeholder="客戶編號、名稱或簡稱" @keydown.enter.prevent>
        <div class="picker-scroll"><table class="kd-grid"><thead><tr><th>CustomerNo</th><th>CustomerName</th><th>CustomerSName</th><th>IssueState</th></tr></thead><tbody>
          <tr v-for="item in visibleCustomers" :key="item.customerNo"><td><button class="pick-button" type="button" :disabled="!pickerReady" @click="chooseCustomer(item)">{{ item.customerNo }}</button></td><td>{{ item.customerName }}</td><td>{{ item.customerSName }}</td><td>{{ stateName(item.issueState) }}</td></tr>
          <tr v-if="!visibleCustomers.length"><td colspan="4" class="empty-row">{{ pickerLoading?'載入中…':pickerError?'客戶資料未能載入':'沒有符合的客戶資料。' }}</td></tr>
        </tbody></table></div>
      </fieldset>
      <p v-if="pickerError" class="message error-message" role="alert">{{ pickerError }}</p>
      <footer class="dialog-actions"><button class="kd-button" type="button" :disabled="writing || pickerLoading" @click="closeCustomerPicker">關閉</button></footer>
    </section></div>

    <div v-if="mode" class="dialog-backdrop" @click.self="closeForm"><section ref="formElement" class="win-dialog header-dialog" role="dialog" aria-modal="true" aria-label="客戶連絡人編輯" tabindex="-1" :inert="writing || confirmationMessage!==null" @keydown.esc.stop.prevent="closeForm" @keydown.tab="trapTab">
      <header class="dialog-title">{{ mode==='new'?'新增':mode==='edit'?'修改':'複製' }}客戶連絡人資料</header>
      <form class="dialog-form" novalidate @submit.prevent="saveForm(true)">
        <fieldset class="form-group" :disabled="writing || !formReady || confirmationMessage!==null"><legend>客戶連絡人資料</legend><div class="form-grid">
          <label>客戶編號<input class="kd-input" :value="formCustomerNo" readonly aria-label="連絡人客戶編號"></label>
          <label>連絡人姓名<input v-model="draft.contactorName" class="kd-input" :readonly="mode==='edit'" maxlength="50" aria-label="連絡人姓名"></label>
          <label>電話<input v-model="draft.telNo" class="kd-input" maxlength="25" aria-label="連絡人電話"></label><label>傳真<input v-model="draft.faxNo" class="kd-input" maxlength="25" aria-label="連絡人傳真"></label>
          <label>職稱<input v-model="draft.title" class="kd-input" maxlength="25" aria-label="連絡人職稱"></label><label>部門<input v-model="draft.deptName" class="kd-input" maxlength="25" aria-label="連絡人部門"></label>
          <label class="wide-label">Email<input v-model="draft.email" class="kd-input" maxlength="50" aria-label="連絡人Email"></label>
          <label class="description-label">說明<textarea v-model="draft.description" class="kd-input" maxlength="255" rows="4" aria-label="連絡人說明"></textarea></label>
        </div></fieldset>
        <div class="audit-row"><span>建立人 {{ mode==='edit'?sourceRow?.creator:'' }}</span><span>建立日期 {{ mode==='edit'?formatDate(sourceRow?.createDate):'' }}</span><span>修改人 {{ mode==='edit'?sourceRow?.reviser:'' }}</span><span>修改日期 {{ mode==='edit'?formatDate(sourceRow?.reviseDate):'' }}</span></div>
        <p v-if="formError" class="message error-message" role="alert">{{ formError }}</p><p v-if="formNotice" class="message notice-message" role="status">{{ formNotice }}</p>
        <footer class="dialog-actions"><button class="kd-button" type="button" :disabled="writing" @click="closeForm">關閉</button><button class="kd-button" type="button" :disabled="writing || !formReady" @click="saveForm(false)">套用</button><button class="kd-button primary" type="submit" :disabled="writing || !formReady">確定</button></footer>
      </form>
    </section></div>
    <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
  </section>
</template>

<style scoped>
.contactor-page{box-sizing:border-box;display:flex;flex-direction:column;height:100%;min-height:0;padding:8px 10px;background:#F7F5EE;color:#26251F;font:13px/1.4 "Microsoft JhengHei",Arial,sans-serif}.master-surface{display:flex;flex-direction:column;gap:6px;flex:1;min-height:0}
.query-strip{display:flex;align-items:center;flex-wrap:wrap;gap:8px;min-height:40px;padding:5px 8px;border:1px solid #d4d0c7;background:#fff}.issue-filter{display:flex;align-items:center;gap:10px;padding:0;margin:0;border:0}.issue-filter legend{float:left;margin-right:10px}.issue-filter label,.query-field{display:inline-flex;align-items:center;gap:5px}.query-input{width:180px}.dev-badge{padding:2px 7px;border:1px solid #d8c373;background:#fff5ce;color:#76631b;font-size:11px}
.kd-input{box-sizing:border-box;width:100%;min-width:0;height:29px;padding:4px 7px;border:1px solid #aaa69e;background:#fff1bf;color:#333;font:inherit}.kd-input:focus{outline:2px solid var(--yellow,#E7C600);outline-offset:1px}.kd-input[readonly]{background:#efeeeb}.kd-button{min-height:30px;padding:4px 9px;border:1px solid #bdb9b0;border-radius:3px;background:linear-gradient(#fff,#efeeeb);color:#343331;font:inherit;white-space:nowrap;cursor:pointer}.kd-button:hover:not(:disabled){border-color:var(--yellow,#E7C600);background:#fff5d4}.kd-button:disabled{opacity:.48;cursor:default}.kd-button.primary{background:#3B3930;border-color:#3B3930;color:white}.kd-button.primary:hover:not(:disabled){background:#514d3e}
.win-group{display:flex;flex-direction:column;min-width:0;min-height:0;margin:0;padding:7px;border:1px solid #c8c3b8;background:#fff}.win-group legend{padding:0 5px;color:#59564e}.master-panel{flex:1.2}.detail-panel{flex:1}.panel-layout{display:flex;gap:8px;flex:1;min-height:0}.grid-scroll{flex:1;min-width:0;min-height:0;overflow:auto;border:1px solid #c5c1b9}.kd-grid{width:max-content;min-width:100%;border-collapse:collapse;background:#fff;font-size:12px}.kd-grid th{position:sticky;top:0;z-index:1;background:#EEEAE0;color:#3B3930;text-align:left}.kd-grid td,.kd-grid th{padding:4px 7px;border:1px solid #d4d0c7;white-space:nowrap}.kd-grid tbody tr{cursor:pointer}.kd-grid tbody tr:hover{background:#fff8e2}.kd-grid tr.active{background:#ffedb1}.check-cell{width:26px;text-align:center}.kd-grid input[type=checkbox],.issue-filter input{accent-color:var(--yellow,#E7C600)}.vertical-actions{display:flex;flex-direction:column;gap:6px;flex:0 0 88px}.grid-status{padding:3px 2px 0;color:#777366;font-size:11px}.empty-row{height:44px;text-align:center;color:#777366}
.message{margin:0;padding:6px 8px;border:1px solid}.error-message{border-color:#d99b86;background:#fff0e9;color:#873820}.notice-message{border-color:#d8c79a;background:#fff8dc;color:#65541d}.footer-actions{display:flex;justify-content:flex-end;padding-top:3px}
.dialog-backdrop{position:fixed;inset:0;z-index:1200;display:flex;align-items:center;justify-content:center;padding:14px;background:#17171770}.win-dialog{box-sizing:border-box;display:flex;flex-direction:column;gap:8px;max-height:94vh;padding:12px;border:1px solid #aaa69e;background:#F7F5EE;box-shadow:0 8px 25px #0004}.header-dialog{width:min(700px,96vw);overflow:auto}.dialog-title{padding-bottom:7px;border-bottom:1px solid #d4d0c7;font-size:16px;font-weight:600}.form-group{margin:0;padding:10px;border:1px solid #c9c4b9}.form-grid{display:grid;grid-template-columns:1fr 1fr;gap:10px 16px}.form-grid>label{display:flex;flex-direction:column;gap:4px}.lookup-control{display:flex;gap:5px}.lookup-control .kd-input{flex:1}.description-label{grid-column:1/-1}.description-label textarea{height:72px;resize:vertical}.audit-row{display:grid;grid-template-columns:1fr 1fr;gap:4px 10px;padding:7px;border:1px solid #d0cabf;background:#f1eee6;color:#777366;font-size:11px}.dialog-actions{display:flex;justify-content:flex-end;gap:7px}.picker-dialog{width:min(800px,96vw);min-height:180px}.picker-fields{display:flex;flex-direction:column;min-height:0;gap:8px;margin:0;padding:0;border:0}.picker-search{display:flex;gap:6px}.picker-search .kd-input{flex:1}.picker-scroll{overflow:auto;max-height:55vh;border:1px solid #c5c1b9}
@media(max-width:900px){.query-strip{gap:6px}.issue-filter{flex-wrap:wrap}.form-grid{grid-template-columns:1fr}.description-label{grid-column:auto}.vertical-actions{flex-basis:78px}.picker-search{flex-wrap:wrap}.picker-search .kd-input{flex-basis:100%}}
.dialog-form{display:flex;flex-direction:column;gap:8px}.detail-dialog{width:min(700px,96vw);overflow:auto}.query-strip{margin:0}.query-field select{width:155px}.flag-label{flex-direction:row!important;align-items:center;gap:6px!important;grid-column:1/-1}.flag-label input{accent-color:var(--yellow,#E7C600)}.flag-label span,.field-hint{color:#777366;font-size:11px}.field-hint{margin:8px 0 0}.lookup-backdrop{z-index:1300}.lookup-row{box-sizing:border-box;display:grid;grid-template-columns:minmax(140px,1fr) minmax(140px,1fr) minmax(80px,1fr);gap:10px;width:100%;padding:8px;border:0;border-bottom:1px solid #d4d0c7;background:#fff;color:#343331;text-align:left;font:inherit;cursor:pointer}.lookup-row:hover:not(:disabled){background:#fff5d4}.lookup-row:focus-visible{outline:2px solid var(--yellow,#E7C600);outline-offset:-2px}.lookup-row small{color:#777366}.lookup-row:disabled{opacity:.5}.dialog-title{overflow-wrap:anywhere}.grid-scroll{scrollbar-width:thin;scrollbar-color:#7775 transparent}
@media(max-width:900px){.lookup-row{grid-template-columns:1fr 1fr}.lookup-row small{grid-column:1/-1}.query-field{flex-wrap:wrap}}
.contact-panel{flex:1}.customer-input{width:190px}.customer-name{color:#777366;overflow-wrap:anywhere}.pick-button{border:0;background:transparent;color:#3B3930;text-align:left;font:inherit;cursor:pointer;padding:3px 1px;text-decoration:underline;text-underline-offset:3px}.pick-button:focus-visible{outline:2px solid var(--yellow,#E7C600);outline-offset:1px}.wide-label{grid-column:1/-1}
@media(max-width:900px){.customer-name{flex-basis:100%}.customer-input{width:min(220px,60vw)}.wide-label{grid-column:auto}}
</style>
