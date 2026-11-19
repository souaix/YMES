<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'
import { apiErrorMessage } from '../utils/apiError'

type Mode = 'new' | 'edit' | 'copy' | ''
type LookupKind = 'customer' | 'segment' | 'operation' | ''
interface MasterRow {
  customerNo: string; customerOPNo: string; customerOPName: string; psNo: string; customerOPSeq: number
  description: string | null; creator: string | null; createDate: string | null; reviser: string | null; reviseDate: string | null; dataStamp: number | null
}
interface DetailRow {
  customerNo: string; customerOPNo: string; psNo: string; opNo: string; custOPProcessCode: string; custStepName: string
  transactionFlag: number; creator: string | null; createDate: string | null; reviser: string | null; reviseDate: string | null; dataStamp: number | null
}
interface LookupOption { value: string; label: string | null; description: string | null; psNo?: string | null; issueState?: number | null }
interface MasterDraft { customerNo: string; customerOPNo: string; customerOPName: string; psNo: string; customerOPSeq: string; description: string }
interface DetailDraft { opNo: string; custOPProcessCode: string; custStepName: string; transactionFlag: number }
interface Snapshot { parent: MasterRow; items: DetailRow[] }
interface MockStore { masters: MasterRow[]; details: DetailRow[] }
const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const apiRoot = 'api/customer-op-mappings', storageKey = 'ymes.dev.opb03.customer-op-mapping.v1'
const seedMasters: MasterRow[] = [
  { customerNo: 'DEV-CUSTOMER-01', customerOPNo: 'DEV-IN-01', customerOPName: 'DEV 客戶入料站', psNo: 'DEV-ASSY', customerOPSeq: 10, description: 'DEV 假資料：客戶入料站點', creator: 'DEV001', createDate: '2026-11-10T09:00:00', reviser: 'DEV002', reviseDate: '2026-11-15T14:20:00', dataStamp: 1 },
  { customerNo: 'DEV-CUSTOMER-01', customerOPNo: 'DEV-OUT-01', customerOPName: 'DEV 客戶出貨站', psNo: 'DEV-PACK', customerOPSeq: 20, description: 'DEV 假資料：客戶出貨站點', creator: 'DEV001', createDate: '2026-11-11T10:30:00', reviser: '', reviseDate: '', dataStamp: 0 },
  { customerNo: 'DEV-CUSTOMER-02', customerOPNo: 'DEV-QC-01', customerOPName: 'DEV 客戶檢驗站', psNo: 'DEV-QC', customerOPSeq: 30, description: 'DEV 假資料：檢驗站點', creator: 'DEV001', createDate: '2026-11-12T11:00:00', reviser: '', reviseDate: '', dataStamp: 0 },
]
const seedDetails: DetailRow[] = [
  { customerNo: 'DEV-CUSTOMER-01', customerOPNo: 'DEV-IN-01', psNo: 'DEV-ASSY', opNo: 'DEV-OP10', custOPProcessCode: 'DEV-RECEIVE', custStepName: 'DEV 客戶收料', transactionFlag: 1, creator: 'DEV001', createDate: '2026-11-10T09:10:00', reviser: '', reviseDate: '', dataStamp: 0 },
  { customerNo: 'DEV-CUSTOMER-01', customerOPNo: 'DEV-IN-01', psNo: 'DEV-ASSY', opNo: 'DEV-OP20', custOPProcessCode: 'DEV-INSPECT', custStepName: 'DEV 外觀確認', transactionFlag: 0, creator: 'DEV001', createDate: '2026-11-10T09:12:00', reviser: 'DEV002', reviseDate: '2026-11-15T14:21:00', dataStamp: 1 },
  { customerNo: 'DEV-CUSTOMER-01', customerOPNo: 'DEV-OUT-01', psNo: 'DEV-PACK', opNo: 'DEV-PK10', custOPProcessCode: 'DEV-SHIP', custStepName: 'DEV 客戶出貨', transactionFlag: 1, creator: 'DEV001', createDate: '2026-11-11T10:40:00', reviser: '', reviseDate: '', dataStamp: 0 },
]
const seedCustomers: LookupOption[] = [
  { value: 'DEV-CUSTOMER-01', label: 'DEV-CUSTOMER-01', description: 'DEV 客戶甲', issueState: 2 },
  { value: 'DEV-CUSTOMER-02', label: 'DEV-CUSTOMER-02', description: 'DEV 客戶乙', issueState: 2 },
]
const seedSegments: LookupOption[] = [
  { value: 'DEV-ASSY', label: 'DEV-ASSY', description: 'DEV 組裝區段', issueState: 2 },
  { value: 'DEV-PACK', label: 'DEV-PACK', description: 'DEV 包裝區段', issueState: 2 },
  { value: 'DEV-QC', label: 'DEV-QC', description: 'DEV 品檢區段', issueState: 2 },
]
const seedOperations: LookupOption[] = [
  { value: 'DEV-OP10', label: 'DEV 上料', description: 'DEV-OP10', psNo: 'DEV-ASSY', issueState: 2 },
  { value: 'DEV-OP20', label: 'DEV 外觀確認', description: 'DEV-OP20', psNo: 'DEV-ASSY', issueState: 2 },
  { value: 'DEV-PK10', label: 'DEV 裝箱出貨', description: 'DEV-PK10', psNo: 'DEV-PACK', issueState: 2 },
  { value: 'DEV-QC10', label: 'DEV 品質檢驗', description: 'DEV-QC10', psNo: 'DEV-QC', issueState: 2 },
  { value: 'DEV-DRAFT', label: 'DEV 未核准站點（不可選）', description: 'DEV-DRAFT', psNo: 'DEV-ASSY', issueState: 0 },
]


const masters = ref<MasterRow[]>([]), details = ref<DetailRow[]>([]), detailOwner = ref<MasterRow | null>(null)
const activeMasterKey = ref(''), activeDetailKey = ref(''), selectedMasterKeys = ref<string[]>([]), selectedDetailKeys = ref<string[]>([])
const searchField = ref<'customerNo' | 'customerOPNo'>('customerNo'), keyword = ref(''), error = ref(''), notice = ref('')
const loading = ref(false), listReady = ref(false), detailLoading = ref(false), detailsReady = ref(false), busy = ref(false), saving = ref(false)
const masterMode = ref<Mode>(''), masterDraft = ref<MasterDraft>(emptyMaster()), masterSource = ref<MasterRow | null>(null), masterSnapshot = ref<Snapshot | null>(null)
const masterOriginal = ref(''), masterError = ref(''), masterReady = ref(false)
const detailMode = ref<'new' | 'edit' | ''>(''), detailDraft = ref<DetailDraft>(emptyDetail()), detailSource = ref<DetailRow | null>(null), detailParent = ref<MasterRow | null>(null)
const detailOriginal = ref(''), detailError = ref(''), detailNotice = ref(''), detailReady = ref(false)
const lookupKind = ref<LookupKind>(''), lookupKeyword = ref(''), lookupOptions = ref<LookupOption[]>([]), lookupLoading = ref(false), lookupReady = ref(false), lookupError = ref('')
const pageElement = ref<HTMLElement | null>(null), masterElement = ref<HTMLElement | null>(null), detailElement = ref<HTMLElement | null>(null), lookupElement = ref<HTMLElement | null>(null)
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()
let listEpoch = 0, detailEpoch = 0, formEpoch = 0, lookupEpoch = 0
let previousModalFocus: HTMLElement | null = null, previousLookupFocus: HTMLElement | null = null
const writing = computed(() => busy.value || saving.value)
const modalOpen = computed(() => !!masterMode.value || !!detailMode.value || !!lookupKind.value)
const blocked = computed(() => loading.value || detailLoading.value || writing.value || lookupLoading.value || modalOpen.value || confirmationMessage.value !== null)
const visibleMasters = computed(() => masters.value.filter(row => row[searchField.value].toLowerCase().includes(keyword.value.trim().toLowerCase()))
  .sort((a, b) => a.customerNo.localeCompare(b.customerNo) || a.customerOPSeq - b.customerOPSeq || a.customerOPNo.localeCompare(b.customerOPNo)))
const activeMaster = computed(() => visibleMasters.value.find(row => masterKey(row) === activeMasterKey.value) ?? null)
const visibleDetails = computed(() => activeMaster.value && detailOwner.value && sameMaster(activeMaster.value, detailOwner.value) ? details.value : [])
const ownerReady = computed(() => detailsReady.value && !!activeMaster.value && !!detailOwner.value && sameMaster(activeMaster.value, detailOwner.value) && activeMaster.value.dataStamp === detailOwner.value.dataStamp)
const activeDetail = computed(() => visibleDetails.value.find(row => detailKey(row) === activeDetailKey.value) ?? null)
const masterTargets = computed(() => visibleMasters.value.filter(row => selectedMasterKeys.value.length ? selectedMasterKeys.value.includes(masterKey(row)) : masterKey(row) === activeMasterKey.value))
const detailTargets = computed(() => visibleDetails.value.filter(row => selectedDetailKeys.value.length ? selectedDetailKeys.value.includes(detailKey(row)) : detailKey(row) === activeDetailKey.value))
const masterHasDetails = computed(() => masterMode.value === 'edit' && !!masterSnapshot.value?.items.length)
const lookupTitle = computed(() => lookupKind.value === 'customer' ? '選擇客戶' : lookupKind.value === 'segment' ? '選擇生產區段' : '選擇作業站')
const visibleLookupOptions = computed(() => {
  const query = lookupKeyword.value.trim().toLowerCase()
  return lookupOptions.value.filter(row => row.issueState === 2 && (lookupKind.value !== 'operation' || row.psNo === detailParent.value?.psNo)
    && (!query || [row.value, row.label ?? '', row.description ?? '', row.psNo ?? ''].some(value => value.toLowerCase().includes(query))))
})
function emptyMaster(): MasterDraft { return { customerNo: '', customerOPNo: '', customerOPName: '', psNo: '', customerOPSeq: '0', description: '' } }
function emptyDetail(): DetailDraft { return { opNo: '', custOPProcessCode: '', custStepName: '', transactionFlag: 0 } }
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function masterKey(row: Pick<MasterRow, 'customerNo' | 'customerOPNo'>): string { return JSON.stringify([row.customerNo, row.customerOPNo]) }
function detailKey(row: Pick<DetailRow, 'customerNo' | 'customerOPNo' | 'opNo'>): string { return JSON.stringify([row.customerNo, row.customerOPNo, row.opNo]) }
function sameMaster(a: Pick<MasterRow, 'customerNo' | 'customerOPNo'>, b: Pick<MasterRow, 'customerNo' | 'customerOPNo'>): boolean { return masterKey(a) === masterKey(b) }
function fail(value: unknown): string { return value instanceof Error ? value.message : '操作失敗，請重新查詢。' }
function path(resource = '', parent?: Pick<MasterRow, 'customerNo' | 'customerOPNo'>, opNo?: string, extra: Record<string, string> = {}): string {
  const query = new URLSearchParams(extra)
  if (parent) { query.set('customerNo', parent.customerNo); query.set('customerOPNo', parent.customerOPNo) }
  if (opNo !== undefined) query.set('opNo', opNo)
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
function object(value: unknown): Record<string, unknown> {
  if (value === null || typeof value !== 'object' || Array.isArray(value)) throw new Error('伺服器回應不完整。')
  return value as Record<string, unknown>
}
function text(row: Record<string, unknown>, key: string): string | null {
  if (!Object.hasOwn(row, key) || row[key] !== null && typeof row[key] !== 'string') throw new Error('伺服器文字欄位回應不完整。')
  return row[key] as string | null
}
function requiredText(row: Record<string, unknown>, key: string): string { const value = text(row, key); if (value === null) throw new Error('伺服器識別欄位回應不完整。'); return value }
function number(row: Record<string, unknown>, key: string): number | null {
  if (!Object.hasOwn(row, key) || row[key] !== null && (typeof row[key] !== 'number' || !Number.isSafeInteger(row[key]))) throw new Error('伺服器數值欄位回應不完整。')
  return row[key] as number | null
}
function requiredNumber(row: Record<string, unknown>, key: string): number { const value = number(row, key); if (value === null) throw new Error('伺服器必要數值回應不完整。'); return value }
function listFrom(value: unknown): unknown[] { const rows = object(value).items; if (!Array.isArray(rows)) throw new Error('伺服器清單回應不完整。'); return rows }
function normalizeMaster(value: unknown): MasterRow {
  const row = object(value)
  return { customerNo: requiredText(row, 'customerNo'), customerOPNo: requiredText(row, 'customerOPNo'), customerOPName: requiredText(row, 'customerOPName'),
    psNo: requiredText(row, 'psNo'), customerOPSeq: requiredNumber(row, 'customerOPSeq'), description: text(row, 'description'), creator: text(row, 'creator'),
    createDate: text(row, 'createDate'), reviser: text(row, 'reviser'), reviseDate: text(row, 'reviseDate'), dataStamp: number(row, 'dataStamp') }
}
function normalizeDetail(value: unknown): DetailRow {
  const row = object(value)
  return { customerNo: requiredText(row, 'customerNo'), customerOPNo: requiredText(row, 'customerOPNo'), psNo: requiredText(row, 'psNo'), opNo: requiredText(row, 'opNo'),
    custOPProcessCode: requiredText(row, 'custOPProcessCode'), custStepName: requiredText(row, 'custStepName'), transactionFlag: requiredNumber(row, 'transactionFlag'),
    creator: text(row, 'creator'), createDate: text(row, 'createDate'), reviser: text(row, 'reviser'), reviseDate: text(row, 'reviseDate'), dataStamp: number(row, 'dataStamp') }
}
function displayDate(value: string | null | undefined): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : new Intl.DateTimeFormat('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23' }).format(date)
}
function saveMock(store: MockStore): void { localStorage.setItem(storageKey, JSON.stringify(store)) }
function readMock(): MockStore {
  try { const value = JSON.parse(localStorage.getItem(storageKey) ?? 'null') as MockStore | null; if (value && Array.isArray(value.masters) && Array.isArray(value.details)) return value } catch { /* preserve isolated DEV storage */ }
  const value = { masters: clone(seedMasters), details: clone(seedDetails) }; saveMock(value); return value
}
function bumpMock(row: MasterRow | DetailRow): void {
  if (row.dataStamp !== null && (!Number.isSafeInteger(row.dataStamp) || row.dataStamp >= 99999999999)) throw new Error('版本已達上限。')
  row.dataStamp = row.dataStamp === null ? 1 : row.dataStamp + 1; row.reviser = props.currentUserNo || 'DEV001'; row.reviseDate = new Date().toISOString()
}
function mockActor(): { creator: string; createDate: string; reviser: null; reviseDate: null; dataStamp: number } { return { creator: props.currentUserNo || 'DEV001', createDate: new Date().toISOString(), reviser: null, reviseDate: null, dataStamp: 0 } }
function upsertMaster(row: MasterRow): void { const i = masters.value.findIndex(value => sameMaster(value, row)); if (i < 0) masters.value.push(clone(row)); else masters.value[i] = clone(row) }
function clearDetails(): void { ++detailEpoch; detailLoading.value = false; detailsReady.value = false; detailOwner.value = null; details.value = []; activeDetailKey.value = ''; selectedDetailKeys.value = [] }
function adoptSnapshot(snapshot: Snapshot): void {
  upsertMaster(snapshot.parent); detailOwner.value = clone(snapshot.parent); details.value = clone(snapshot.items); detailsReady.value = true
  const keys = new Set(snapshot.items.map(detailKey)); selectedDetailKeys.value = selectedDetailKeys.value.filter(key => keys.has(key))
  if (!keys.has(activeDetailKey.value)) activeDetailKey.value = selectedDetailKeys.value[0] ?? ''
}
async function readSnapshot(parent: Pick<MasterRow, 'customerNo' | 'customerOPNo'>): Promise<Snapshot> {
  if (mock.value) { const store = readMock(), current = store.masters.find(row => sameMaster(row, parent)); if (!current) throw new Error('主檔已不存在。'); return { parent: clone(current), items: clone(store.details.filter(row => sameMaster(row, current))) } }
  const result = object(await api<unknown>(path('details', parent))), current = normalizeMaster(result.parent)
  if (!sameMaster(current, parent) || !Array.isArray(result.items)) throw new Error('主從回應歸屬不一致。')
  const rows = result.items.map(normalizeDetail)
  if (rows.some(row => !sameMaster(row, current)) || new Set(rows.map(detailKey)).size !== rows.length) throw new Error('明細回應歸屬或識別值不一致。')
  return { parent: current, items: rows }
}
async function readMaster(parent: MasterRow): Promise<MasterRow> {
  if (mock.value) { const row = readMock().masters.find(value => sameMaster(value, parent)); if (!row) throw new Error('主檔已不存在。'); return clone(row) }
  const row = normalizeMaster(await api<unknown>(path('item', parent))); if (!sameMaster(row, parent)) throw new Error('主檔回應歸屬不一致。'); return row
}
function synchronizeSelection(preferred: string): void {
  const visible = new Set(visibleMasters.value.map(masterKey)); selectedMasterKeys.value = selectedMasterKeys.value.filter(key => visible.has(key))
  const first = visibleMasters.value[0]
  activeMasterKey.value = visible.has(preferred) ? preferred : selectedMasterKeys.value[0] ?? (first ? masterKey(first) : '')
  if (activeMasterKey.value && !selectedMasterKeys.value.length) selectedMasterKeys.value = [activeMasterKey.value]
  if (!detailOwner.value || masterKey(detailOwner.value) !== activeMasterKey.value) clearDetails()
}
async function reloadRows(preferred = activeMasterKey.value): Promise<void> {
  const epoch = ++listEpoch, search = keyword.value.trim(), field = searchField.value
  const rows = mock.value ? clone(readMock().masters) : listFrom(await api<unknown>(path('', undefined, undefined, search ? { [field]: search } : {}))).map(normalizeMaster)
  if (epoch !== listEpoch || search !== keyword.value.trim() || field !== searchField.value) return
  if (new Set(rows.map(masterKey)).size !== rows.length) throw new Error('主檔回應含重複識別值。')
  masters.value = rows; listReady.value = true; synchronizeSelection(preferred)
}
async function reloadDetails(parent: MasterRow): Promise<void> {
  const epoch = ++detailEpoch, key = masterKey(parent); detailLoading.value = true; detailsReady.value = false; detailOwner.value = null; details.value = []; selectedDetailKeys.value = []; activeDetailKey.value = ''
  try { const result = await readSnapshot(parent); if (epoch !== detailEpoch || activeMasterKey.value !== key) return; adoptSnapshot(result) }
  finally { if (epoch === detailEpoch) detailLoading.value = false }
}
async function loadRows(): Promise<void> {
  if (blocked.value) return
  loading.value = true; error.value = ''; notice.value = ''; clearDetails()
  try { await reloadRows(); if (activeMaster.value) await reloadDetails(activeMaster.value) }
  catch (cause) { error.value = fail(cause); listReady.value = false }
  finally { loading.value = false }
}
async function activateMaster(row: MasterRow): Promise<void> {
  if (blocked.value) return
  activeMasterKey.value = masterKey(row); selectedMasterKeys.value = [activeMasterKey.value]; error.value = ''
  try { await reloadDetails(row) } catch (cause) { error.value = fail(cause) }
}
function checkMaster(row: MasterRow, checked: boolean): void {
  if (blocked.value) return
  const key = masterKey(row), visible = new Set(visibleMasters.value.map(masterKey))
  selectedMasterKeys.value = (checked ? [...new Set([...selectedMasterKeys.value, key])] : selectedMasterKeys.value.filter(value => value !== key)).filter(value => visible.has(value))
  const preferred = checked ? key : activeMasterKey.value, next = selectedMasterKeys.value.includes(preferred) ? preferred : selectedMasterKeys.value.at(-1) ?? ''
  if (next === activeMasterKey.value && (next === '' || ownerReady.value)) return
  activeMasterKey.value = next; clearDetails()
  const parent = visibleMasters.value.find(value => masterKey(value) === next)
  if (parent) void reloadDetails(parent).catch(cause => { error.value = fail(cause) })
}
watch([keyword, searchField], () => {
  const visible = new Set(visibleMasters.value.map(masterKey)); selectedMasterKeys.value = selectedMasterKeys.value.filter(key => visible.has(key))
  if (activeMasterKey.value && !visible.has(activeMasterKey.value)) {
    activeMasterKey.value = selectedMasterKeys.value[0] ?? ''; clearDetails()
    const parent = activeMaster.value
    if (parent && !writing.value && !loading.value && !modalOpen.value) void reloadDetails(parent).catch(cause => { error.value = fail(cause) })
  }
})
function activateDetail(row: DetailRow): void { if (blocked.value || !ownerReady.value) return; activeDetailKey.value = detailKey(row); selectedDetailKeys.value = [activeDetailKey.value] }
function checkDetail(row: DetailRow, checked: boolean): void { if (blocked.value || !ownerReady.value) return; const key = detailKey(row); selectedDetailKeys.value = checked ? [...new Set([...selectedDetailKeys.value, key])] : selectedDetailKeys.value.filter(value => value !== key); activeDetailKey.value = checked ? key : selectedDetailKeys.value.at(-1) ?? '' }
async function refreshDetails(): Promise<void> { if (blocked.value || !activeMaster.value) return; error.value = ''; try { await reloadDetails(activeMaster.value) } catch (cause) { error.value = fail(cause) } }
function masterFrom(row: MasterRow): MasterDraft { return { customerNo: row.customerNo, customerOPNo: row.customerOPNo, customerOPName: row.customerOPName, psNo: row.psNo, customerOPSeq: String(row.customerOPSeq), description: row.description ?? '' } }
function detailFrom(row: DetailRow): DetailDraft { return { opNo: row.opNo, custOPProcessCode: row.custOPProcessCode, custStepName: row.custStepName, transactionFlag: row.transactionFlag } }
async function openMaster(mode: Exclude<Mode, ''>): Promise<void> {
  if (blocked.value || mode !== 'new' && (!activeMaster.value || !listReady.value)) return
  previousModalFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null; busy.value = true; error.value = ''; notice.value = ''
  const epoch = ++formEpoch, source = activeMaster.value ? clone(activeMaster.value) : null
  try {
    let row: MasterRow | null = null, snapshot: Snapshot | null = null
    if (mode === 'edit' && source) { snapshot = await readSnapshot(source); row = snapshot.parent; adoptSnapshot(snapshot) }
    else if (mode === 'copy' && source) { row = await readMaster(source); upsertMaster(row) }
    if (epoch !== formEpoch) return
    masterSource.value = row; masterSnapshot.value = snapshot; masterDraft.value = row ? masterFrom(row) : emptyMaster(); masterOriginal.value = JSON.stringify(masterDraft.value)
    masterError.value = ''; masterReady.value = true; masterMode.value = mode
  } catch (cause) { error.value = fail(cause) }
  finally { busy.value = false; await restoreFocus() }
}
function validateMaster(): string {
  const draft = masterDraft.value, old = masterSource.value, editing = masterMode.value === 'edit'
  if ((!editing && !draft.customerNo.trim()) || draft.customerNo.length > 50 || (!editing && !draft.customerOPNo.trim()) || draft.customerOPNo.length > 20) return '請選擇客戶並填寫有效客戶站點編號。'
  if (!draft.customerOPName.trim() || draft.customerOPName.length > 50) return '請填寫50字以內的站點名稱。'
  if ((!draft.psNo.trim() && !(editing && old?.psNo === draft.psNo)) || draft.psNo.length > 50) return '請選擇生產區段。'
  if (!draft.customerOPSeq.trim() || !Number.isInteger(Number(draft.customerOPSeq)) || Number(draft.customerOPSeq) < 0 || Number(draft.customerOPSeq) > 999) return '站點順序須為0到999整數。'
  return draft.description.length > 255 ? '說明最多255字元。' : ''
}
function masterPayload(): Omit<MasterRow, 'creator' | 'createDate' | 'reviser' | 'reviseDate' | 'dataStamp'> {
  const draft = masterDraft.value, old = masterSource.value, editing = masterMode.value === 'edit'
  return { customerNo: editing && old ? old.customerNo : draft.customerNo, customerOPNo: editing && old ? old.customerOPNo : draft.customerOPNo.trim().toUpperCase(),
    customerOPName: draft.customerOPName, customerOPSeq: Number(draft.customerOPSeq), psNo: editing && old?.psNo === draft.psNo ? old.psNo : draft.psNo,
    description: old && draft.description === (old.description ?? '') ? old.description : draft.description }
}
function mockSaveMaster(payload: ReturnType<typeof masterPayload>, source: MasterRow | null, mode: Mode): MasterRow {
  const store = readMock()
  if (mode === 'edit') {
    const row = store.masters.find(value => source && sameMaster(value, source)); if (!row || row.dataStamp !== source?.dataStamp) throw new Error('主檔已變更，請重新查詢。')
    if (payload.psNo !== row.psNo && store.details.some(value => sameMaster(value, row))) throw new Error('已有明細，生產區段無法變更。')
    if (payload.psNo !== row.psNo && !seedSegments.some(value => value.value === payload.psNo && value.issueState === 2)) throw new Error('請選擇已核准的生產區段。')
    bumpMock(row); Object.assign(row, payload); saveMock(store); return clone(row)
  }
  if (!seedCustomers.some(row => row.value === payload.customerNo && row.issueState === 2) || !seedSegments.some(row => row.value === payload.psNo && row.issueState === 2)) throw new Error('請選擇已核准的客戶與生產區段。')
  if (store.masters.some(row => sameMaster(row, payload))) throw new Error('客戶站點編號已存在。')
  const row: MasterRow = { ...payload, ...mockActor() }; store.masters.push(row); saveMock(store); return clone(row)
}
async function saveMaster(): Promise<void> {
  if (!masterMode.value || !masterReady.value || writing.value || lookupKind.value || confirmationMessage.value !== null) return
  masterError.value = validateMaster(); if (masterError.value) return
  const mode = masterMode.value, source = masterSource.value ? clone(masterSource.value) : null, payload = masterPayload(), priorItems = clone(masterSnapshot.value?.items ?? [])
  saving.value = true; notice.value = ''; let observed = false
  try {
    const result = mock.value ? mockSaveMaster(payload, source, mode) : await api<unknown>(mode === 'edit' ? path('item', source!) : path(), { method: mode === 'edit' ? 'PUT' : 'POST', body: JSON.stringify(mode === 'edit' ? { ...payload, dataStamp: source!.dataStamp } : payload) })
    observed = true; const saved = normalizeMaster(result)
    if (mode === 'edit' && source && !sameMaster(saved, source)) throw new Error('保存回應歸屬不一致。')
    upsertMaster(saved)
    if (!saved[searchField.value].toLowerCase().includes(keyword.value.trim().toLowerCase())) keyword.value = ''
    activeMasterKey.value = masterKey(saved); selectedMasterKeys.value = [activeMasterKey.value]
    adoptSnapshot({ parent: saved, items: mode === 'edit' ? priorItems : [] }); listReady.value = true
    masterSource.value = saved; masterReady.value = false; masterMode.value = ''; ++formEpoch; notice.value = '資料已儲存。'
    try { await reloadRows(masterKey(saved)) } catch (cause) { error.value = fail(cause); notice.value = '資料已儲存；清單更新失敗，已保留保存結果，請重新查詢。' }
  } catch (cause) {
    masterError.value = fail(cause)
    if (observed || !mock.value && (!(cause instanceof ApiFailure) || cause.status >= 500)) { masterReady.value = false; masterError.value += ' 保存結果無法確認，請關閉後重新查詢。' }
  } finally { saving.value = false; await restoreFocus() }
}
async function openDetail(mode: 'new' | 'edit'): Promise<void> {
  if (blocked.value || !ownerReady.value || !activeMaster.value || mode === 'edit' && !activeDetail.value) return
  previousModalFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null
  const parent = clone(activeMaster.value), selected = activeDetail.value ? detailKey(activeDetail.value) : '', epoch = ++formEpoch
  busy.value = true; error.value = ''; notice.value = ''
  try {
    const snapshot = await readSnapshot(parent); if (epoch !== formEpoch) return
    adoptSnapshot(snapshot); const row = mode === 'edit' ? snapshot.items.find(value => detailKey(value) === selected) : null
    if (mode === 'edit' && !row) throw new Error('明細已變更，請重新選取。')
    detailParent.value = clone(snapshot.parent); detailSource.value = row ? clone(row) : null; detailDraft.value = row ? detailFrom(row) : emptyDetail()
    detailOriginal.value = JSON.stringify(detailDraft.value); detailError.value = ''; detailNotice.value = ''; detailReady.value = true; detailMode.value = mode
  } catch (cause) { error.value = fail(cause) }
  finally { busy.value = false; await restoreFocus() }
}
function uppercaseDetailOp(): void { if (detailMode.value === 'new' || detailDraft.value.opNo !== detailSource.value?.opNo) detailDraft.value.opNo = detailDraft.value.opNo.toUpperCase() }
function validateDetail(): string {
  const draft = detailDraft.value, sameOp = detailMode.value === 'edit' && detailSource.value?.opNo === draft.opNo
  if ((!sameOp && !draft.opNo.trim()) || draft.opNo.length > 20) return '請選擇有效作業站。'
  if (!draft.custOPProcessCode.trim() || draft.custOPProcessCode.length > 20 || !draft.custStepName.trim() || draft.custStepName.length > 20) return '流程代碼及步驟名稱為必填，最多20字元。'
  if (![0, 1].includes(draft.transactionFlag) && !(detailMode.value === 'edit' && detailSource.value?.transactionFlag === draft.transactionFlag)) return '請選擇是否傳送交易資料。'
  return ''
}
function detailPayload(): DetailDraft {
  const draft = detailDraft.value, old = detailSource.value
  return { ...draft, opNo: detailMode.value === 'edit' && old?.opNo === draft.opNo ? old.opNo : draft.opNo.trim().toUpperCase() }
}
function mockSaveDetail(parent: MasterRow, source: DetailRow | null, payload: DetailDraft, mode: 'new' | 'edit'): { parent: MasterRow; item: DetailRow } {
  const store = readMock(), current = store.masters.find(row => sameMaster(row, parent))
  if (!current || current.dataStamp !== parent.dataStamp) throw new Error('主檔已變更，請重新查詢。')
  const row = mode === 'edit' ? store.details.find(value => source && detailKey(value) === detailKey(source)) : null
  if (mode === 'edit' && (!row || row.dataStamp !== source?.dataStamp)) throw new Error('明細已變更，請重新查詢。')
  if (mode === 'new' || payload.opNo !== row?.opNo) {
    if (!seedOperations.some(op => op.value === payload.opNo && op.issueState === 2 && op.psNo === current.psNo)) throw new Error('作業站須已核准且屬於相同生產區段。')
    if (store.details.some(value => sameMaster(value, parent) && value.opNo === payload.opNo && value !== row)) throw new Error('此客戶站點已有該作業站。')
  }
  bumpMock(current)
  let item: DetailRow
  if (row) { bumpMock(row); Object.assign(row, payload); item = row }
  else { item = { customerNo: current.customerNo, customerOPNo: current.customerOPNo, psNo: current.psNo, ...payload, ...mockActor() }; store.details.push(item) }
  saveMock(store); return { parent: clone(current), item: clone(item) }
}
function adoptDetailSaved(parent: MasterRow, item: DetailRow, old: DetailRow | null): void {
  if (!sameMaster(parent, item) || !detailParent.value || !sameMaster(parent, detailParent.value)) throw new Error('保存回應歸屬不一致。')
  upsertMaster(parent); detailOwner.value = clone(parent)
  if (old) details.value = details.value.filter(row => detailKey(row) !== detailKey(old))
  const index = details.value.findIndex(row => detailKey(row) === detailKey(item)); if (index < 0) details.value.push(clone(item)); else details.value[index] = clone(item)
  detailsReady.value = true; activeDetailKey.value = detailKey(item); selectedDetailKeys.value = [activeDetailKey.value]
  detailParent.value = clone(parent); detailSource.value = clone(item); detailDraft.value = detailFrom(item); detailOriginal.value = JSON.stringify(detailDraft.value)
  detailMode.value = 'edit'; detailReady.value = true; listReady.value = true
}
async function saveDetail(close: boolean): Promise<void> {
  if (!detailMode.value || !detailReady.value || !detailParent.value || writing.value || lookupKind.value || confirmationMessage.value !== null) return
  detailError.value = validateDetail(); if (detailError.value) return
  const mode = detailMode.value, parent = clone(detailParent.value), source = detailSource.value ? clone(detailSource.value) : null, payload = detailPayload()
  saving.value = true; detailNotice.value = ''; notice.value = ''; let observed = false
  try {
    const raw = mock.value ? mockSaveDetail(parent, source, payload, mode) : await api<unknown>(mode === 'edit' ? path('details/item', parent, source!.opNo) : path('details', parent), { method: mode === 'edit' ? 'PUT' : 'POST', body: JSON.stringify({ ...payload, parentDataStamp: parent.dataStamp, ...(mode === 'edit' ? { dataStamp: source!.dataStamp } : {}) }) })
    observed = true; const result = object(raw), savedParent = normalizeMaster(result.parent), item = normalizeDetail(result.item)
    adoptDetailSaved(savedParent, item, source); detailNotice.value = '資料已儲存。'; notice.value = '明細已儲存。'
    if (close) { detailMode.value = ''; detailReady.value = false; ++formEpoch }
    try { await reloadRows(masterKey(savedParent)) } catch (cause) { error.value = fail(cause); detailNotice.value = '資料已儲存；清單更新失敗，已保留保存結果。'; notice.value = detailNotice.value }
  } catch (cause) {
    detailError.value = fail(cause)
    if (observed || !mock.value && (!(cause instanceof ApiFailure) || cause.status >= 500)) { detailReady.value = false; detailError.value += ' 保存結果無法確認，請關閉後重新查詢。' }
  } finally { saving.value = false; await restoreFocus() }
}
async function deleteMasters(): Promise<void> {
  if (blocked.value || !listReady.value || !masterTargets.value.length) return
  const targets = clone(masterTargets.value), focus = document.activeElement instanceof HTMLElement ? document.activeElement : null
  let completed = 0; busy.value = true; error.value = ''; notice.value = ''
  try {
    if (!await requestConfirmation('確定刪除選取的' + targets.length + '筆客戶站點及其全部明細？')) return
    for (const row of targets) {
      if (mock.value) { const store = readMock(), current = store.masters.find(value => sameMaster(value, row)); if (!current || current.dataStamp !== row.dataStamp) throw new Error('主檔已變更，請重新查詢。'); store.details = store.details.filter(value => !sameMaster(value, row)); store.masters = store.masters.filter(value => !sameMaster(value, row)); saveMock(store) }
      else await api<void>(path('item', row), { method: 'DELETE', body: JSON.stringify({ dataStamp: row.dataStamp }) })
      masters.value = masters.value.filter(value => !sameMaster(value, row)); selectedMasterKeys.value = selectedMasterKeys.value.filter(key => key !== masterKey(row))
      if (activeMasterKey.value === masterKey(row)) { activeMasterKey.value = ''; clearDetails() }
      completed++
    }
    notice.value = '已刪除 ' + completed + ' 筆。'
  } catch (cause) { error.value = fail(cause) + ' 已完成 ' + completed + '/' + targets.length + ' 筆；請重新查詢確認。'; detailsReady.value = false; listReady.value = false }
  finally { busy.value = false; await restorePageFocus(focus) }
}
async function deleteDetails(): Promise<void> {
  if (blocked.value || !ownerReady.value || !detailOwner.value || !detailTargets.value.length) return
  const targets = clone(detailTargets.value), focus = document.activeElement instanceof HTMLElement ? document.activeElement : null
  let parent = clone(detailOwner.value), completed = 0; busy.value = true; error.value = ''; notice.value = ''
  try {
    if (!await requestConfirmation('確定刪除選取的' + targets.length + '筆作業站明細？')) return
    for (const row of targets) {
      let saved: MasterRow
      if (mock.value) { const store = readMock(), current = store.masters.find(value => sameMaster(value, parent)), item = store.details.find(value => detailKey(value) === detailKey(row)); if (!current || current.dataStamp !== parent.dataStamp || !item || item.dataStamp !== row.dataStamp) throw new Error('主檔或明細已變更，請重新查詢。'); bumpMock(current); store.details = store.details.filter(value => detailKey(value) !== detailKey(row)); saved = clone(current); saveMock(store) }
      else saved = normalizeMaster(object(await api<unknown>(path('details/item', parent, row.opNo), { method: 'DELETE', body: JSON.stringify({ dataStamp: row.dataStamp, parentDataStamp: parent.dataStamp }) })).parent)
      if (!sameMaster(saved, parent)) throw new Error('刪除回應歸屬不一致。')
      parent = saved; upsertMaster(saved); detailOwner.value = clone(saved); details.value = details.value.filter(value => detailKey(value) !== detailKey(row)); selectedDetailKeys.value = selectedDetailKeys.value.filter(key => key !== detailKey(row)); completed++
    }
    activeDetailKey.value = selectedDetailKeys.value[0] ?? ''; notice.value = '已刪除 ' + completed + ' 筆明細。'
  } catch (cause) { error.value = fail(cause) + ' 已完成 ' + completed + '/' + targets.length + ' 筆；請重新載入確認。'; detailsReady.value = false }
  finally { busy.value = false; await restorePageFocus(focus) }
}
async function openLookup(kind: Exclude<LookupKind, ''>): Promise<void> {
  if (writing.value || lookupLoading.value || lookupKind.value || confirmationMessage.value !== null) return
  if (kind === 'operation' ? !detailMode.value || !detailReady.value || !detailParent.value : !masterMode.value || !masterReady.value) return
  if (kind === 'customer' && masterMode.value === 'edit' || kind === 'segment' && masterHasDetails.value) return
  previousLookupFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null
  const epoch = ++lookupEpoch, ownerEpoch = formEpoch, parent = detailParent.value ? clone(detailParent.value) : null
  lookupKind.value = kind; lookupKeyword.value = ''; lookupOptions.value = []; lookupReady.value = false; lookupError.value = ''; lookupLoading.value = true
  try {
    let options: LookupOption[]
    if (mock.value) options = clone(kind === 'customer' ? seedCustomers : kind === 'segment' ? seedSegments : seedOperations)
    else {
      const resource = kind === 'customer' ? 'lookups/customers' : kind === 'segment' ? 'lookups/production-segments' : 'lookups/operations'
      options = listFrom(await api<unknown>(path(resource, undefined, undefined, kind === 'operation' && parent ? { psNo: parent.psNo } : {}))).map(value => {
        const row = object(value), state = number(row, 'issueState')
        if (kind === 'customer') return { value: requiredText(row, 'customerNo'), label: text(row, 'customerName'), description: null, issueState: state }
        if (kind === 'segment') return { value: requiredText(row, 'psNo'), label: text(row, 'psName'), description: null, issueState: state }
        return { value: requiredText(row, 'opNo'), label: text(row, 'opName'), description: null, psNo: text(row, 'psNo'), issueState: state }
      })
    }
    if (epoch !== lookupEpoch || ownerEpoch !== formEpoch || lookupKind.value !== kind) return
    if (new Set(options.map(row => row.value)).size !== options.length) throw new Error('查找回應含重複識別值。')
    lookupOptions.value = options; lookupReady.value = true
  } catch (cause) { if (epoch === lookupEpoch) lookupError.value = fail(cause) }
  finally { if (epoch === lookupEpoch) { lookupLoading.value = false; await restoreFocus() } }
}
function chooseLookup(row: LookupOption): void {
  if (!lookupReady.value || writing.value || lookupLoading.value || row.issueState !== 2 || confirmationMessage.value !== null) return
  if (lookupKind.value === 'customer' && masterMode.value !== 'edit') masterDraft.value.customerNo = row.value
  else if (lookupKind.value === 'segment' && !masterHasDetails.value) masterDraft.value.psNo = row.value
  else if (lookupKind.value === 'operation' && detailParent.value && row.psNo === detailParent.value.psNo) detailDraft.value.opNo = row.value
  else return
  void closeLookup()
}
async function closeLookup(): Promise<void> {
  if (writing.value || lookupLoading.value || confirmationMessage.value !== null) return
  lookupKind.value = ''; lookupReady.value = false; ++lookupEpoch; await nextTick()
  if (previousLookupFocus?.isConnected && !previousLookupFocus.matches(':disabled') && !previousLookupFocus.closest('[inert]')) previousLookupFocus.focus()
  else await restoreFocus()
}
async function closeMaster(): Promise<void> {
  if (writing.value || lookupKind.value || confirmationMessage.value !== null) return
  if (JSON.stringify(masterDraft.value) !== masterOriginal.value && !await requestConfirmation('尚有未儲存的修改，確定放棄？')) { await restoreFocus(); return }
  masterMode.value = ''; masterReady.value = false; ++formEpoch; await restoreFocus()
}
async function closeDetail(): Promise<void> {
  if (writing.value || lookupKind.value || confirmationMessage.value !== null) return
  if (JSON.stringify(detailDraft.value) !== detailOriginal.value && !await requestConfirmation('尚有未儲存的修改，確定放棄？')) { await restoreFocus(); return }
  detailMode.value = ''; detailReady.value = false; ++formEpoch; await restoreFocus()
}
function closePage(): void { if (!blocked.value) emit('close') }
function currentModal(): HTMLElement | null { return lookupKind.value ? lookupElement.value : detailMode.value ? detailElement.value : masterMode.value ? masterElement.value : null }
async function restorePageFocus(previous: HTMLElement | null): Promise<void> {
  await nextTick(); if (modalOpen.value || confirmationMessage.value !== null) return
  if (previous?.isConnected && !previous.matches(':disabled') && !previous.closest('[inert]')) previous.focus()
  else pageElement.value?.querySelector<HTMLElement>('.master-surface input:not(:disabled),.master-surface button:not(:disabled)')?.focus()
}
async function restoreFocus(): Promise<void> {
  await nextTick(); if (confirmationMessage.value !== null) return
  const modal = currentModal()
  if (modal && !writing.value && !lookupLoading.value) (modal.querySelector<HTMLElement>('input:not(:disabled):not([readonly]),select:not(:disabled),textarea:not(:disabled),button:not(:disabled)') ?? modal).focus()
  else if (!modal) await restorePageFocus(previousModalFocus)
}
function keepModalFocus(event: KeyboardEvent): void {
  if (event.key !== 'Tab' || !modalOpen.value || confirmationMessage.value !== null) return
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
onBeforeUnmount(() => { ++listEpoch; ++detailEpoch; ++formEpoch; ++lookupEpoch; window.removeEventListener('keydown', keepModalFocus, true) })
</script>

<template>
  <section ref="pageElement" class="customer-op-mapping-page" aria-label="客戶站點對應設定">
    <div class="master-surface" :inert="modalOpen || writing || confirmationMessage !== null">
      <fieldset class="query-strip" :disabled="blocked">
        <label class="query-field">查詢欄位<select v-model="searchField" class="kd-input"><option value="customerNo">CustomerNo</option><option value="customerOPNo">CustomerOPNo</option></select></label>
        <label class="query-field">關鍵字<input v-model="keyword" class="kd-input query-input" aria-label="查詢關鍵字" @keydown.enter.prevent="loadRows"></label>
        <button class="kd-button" type="button" @click="loadRows">查詢</button>
        <span v-if="mock" class="dev-badge">DEV</span>
      </fieldset>
      <fieldset class="win-group master-panel"><legend>Customer OP Mapping Basis</legend><div class="panel-layout">
        <div class="grid-scroll"><table class="kd-grid"><thead><tr><th></th><th>CustomerNo</th><th>CustomerOPNo</th><th>CustomerOPName</th><th>PSNo</th><th>CustomerOPSeq</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead><tbody>
          <tr v-for="row in visibleMasters" :key="masterKey(row)" :class="{active:activeMasterKey===masterKey(row)}" @click="activateMaster(row)">
            <td class="check-cell" @click.stop><input type="checkbox" :checked="selectedMasterKeys.includes(masterKey(row))" :disabled="blocked" :aria-label="'選取 '+row.customerNo+' '+row.customerOPNo" @change="checkMaster(row,($event.target as HTMLInputElement).checked)"></td>
            <td>{{ row.customerNo }}</td><td>{{ row.customerOPNo }}</td><td>{{ row.customerOPName }}</td><td>{{ row.psNo }}</td><td>{{ row.customerOPSeq }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ displayDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ displayDate(row.reviseDate) }}</td>
          </tr><tr v-if="!visibleMasters.length"><td colspan="11" class="empty-row">{{ loading?'載入中…':listReady?'沒有符合條件的資料':'清單尚未載入，請重新查詢' }}</td></tr>
        </tbody></table></div>
        <aside class="vertical-actions">
          <button class="kd-button primary" type="button" :disabled="blocked" @click="openMaster('new')">新增 (A)</button>
          <button class="kd-button" type="button" :disabled="blocked || !listReady || !activeMaster" @click="openMaster('edit')">修改 (E)</button>
          <button class="kd-button" type="button" :disabled="blocked || !listReady || !masterTargets.length" @click="deleteMasters">刪除 (D)</button>
          <button class="kd-button" type="button" :disabled="blocked || !listReady || !activeMaster" @click="openMaster('copy')">複製 (C)</button>
        </aside>
      </div><div class="grid-status">資料總筆數 {{ visibleMasters.length }}</div></fieldset>
      <fieldset class="win-group detail-panel"><legend>Customer OP Mapping Detail<span v-if="activeMaster"> — {{ activeMaster.customerNo }} / {{ activeMaster.customerOPNo }}</span></legend><div class="panel-layout">
        <div class="grid-scroll"><table class="kd-grid"><thead><tr><th></th><th>PSNo</th><th>OPNo</th><th>CustOPProcessCode</th><th>CustStepName</th><th>TransactionFlag</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead><tbody>
          <tr v-for="row in visibleDetails" :key="detailKey(row)" :class="{active:activeDetailKey===detailKey(row)}" @click="activateDetail(row)">
            <td class="check-cell" @click.stop><input type="checkbox" :checked="selectedDetailKeys.includes(detailKey(row))" :disabled="blocked || !ownerReady" :aria-label="'選取明細 '+row.opNo" @change="checkDetail(row,($event.target as HTMLInputElement).checked)"></td>
            <td>{{ row.psNo }}</td><td>{{ row.opNo }}</td><td>{{ row.custOPProcessCode }}</td><td>{{ row.custStepName }}</td><td>{{ row.transactionFlag===1?'Yes':row.transactionFlag===0?'No':row.transactionFlag }}</td><td>{{ row.creator }}</td><td>{{ displayDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ displayDate(row.reviseDate) }}</td>
          </tr><tr v-if="!visibleDetails.length"><td colspan="10" class="empty-row">{{ detailLoading?'載入中…':!activeMaster?'請先選取上方資料':!ownerReady?'明細尚未載入，請重新載入':'此客戶站點尚未設定明細' }}</td></tr>
        </tbody></table></div>
        <aside class="vertical-actions">
          <button class="kd-button primary" type="button" :disabled="blocked || !ownerReady" @click="openDetail('new')">新增 (A)</button>
          <button class="kd-button" type="button" :disabled="blocked || !ownerReady || !activeDetail" @click="openDetail('edit')">修改 (E)</button>
          <button class="kd-button" type="button" :disabled="blocked || !ownerReady || !detailTargets.length" @click="deleteDetails">刪除 (D)</button>
          <button class="kd-button" type="button" :disabled="blocked || !activeMaster" @click="refreshDetails">重新載入</button>
        </aside>
      </div><div class="grid-status">資料總筆數 {{ visibleDetails.length }}</div></fieldset>
      <p v-if="error" class="message error-message" role="alert">{{ error }}</p><p v-if="notice" class="message notice-message" role="status">{{ notice }}</p>
      <div class="footer-actions"><button class="kd-button" type="button" :disabled="blocked" @click="closePage">關閉</button></div>
    </div>

    <div v-if="masterMode" class="dialog-backdrop" @click.self="closeMaster"><section ref="masterElement" class="win-dialog header-dialog" role="dialog" aria-modal="true" aria-label="客戶站點主檔" tabindex="-1" :inert="writing || !!lookupKind || confirmationMessage!==null" @keydown.esc.stop.prevent="closeMaster" @keydown.tab="trapTab">
      <header class="dialog-title">Customer OP Mapping Define（{{ masterMode==='new'?'ADD':masterMode==='edit'?'EDIT':'Copy' }}）</header>
      <form class="dialog-form" novalidate @submit.prevent="saveMaster">
        <fieldset class="form-group" :disabled="writing || !!lookupKind || !masterReady || confirmationMessage!==null"><legend>Customer OP Mapping Basis</legend><div class="form-grid">
          <label>Customer No<div class="lookup-control"><input v-model="masterDraft.customerNo" class="kd-input" maxlength="50" readonly aria-label="客戶編號"><button class="kd-button" type="button" :disabled="masterMode==='edit'" aria-label="查找客戶" @click="openLookup('customer')">...</button></div></label>
          <label>Customer OPNo<input v-model="masterDraft.customerOPNo" class="kd-input" maxlength="20" :readonly="masterMode==='edit'" aria-label="客戶站點編號" @blur="masterMode!=='edit' && (masterDraft.customerOPNo=masterDraft.customerOPNo.toUpperCase())"></label>
          <label>Customer OP Name<input v-model="masterDraft.customerOPName" class="kd-input" maxlength="50" aria-label="客戶站點名稱"></label>
          <label>PS No<div class="lookup-control"><input v-model="masterDraft.psNo" class="kd-input" maxlength="50" readonly aria-label="主檔生產區段"><button class="kd-button" type="button" :disabled="masterHasDetails" aria-label="查找生產區段" @click="openLookup('segment')">...</button></div></label>
          <label>Customer OP Seq<input :value="masterDraft.customerOPSeq" class="kd-input" type="number" min="0" max="999" step="1" aria-label="站點順序" @input="masterDraft.customerOPSeq=($event.target as HTMLInputElement).value"></label>
          <label class="description-label">Description<textarea v-model="masterDraft.description" class="kd-input" maxlength="255" rows="3" aria-label="說明"></textarea></label>
        </div><p v-if="masterHasDetails" class="field-hint">已有明細，生產區段無法變更。</p></fieldset>
        <div class="audit-row"><span>建立人 {{ masterMode==='edit'?masterSource?.creator:'' }}</span><span>建立日期 {{ masterMode==='edit'?displayDate(masterSource?.createDate):'' }}</span><span>修改人 {{ masterMode==='edit'?masterSource?.reviser:'' }}</span><span>修改日期 {{ masterMode==='edit'?displayDate(masterSource?.reviseDate):'' }}</span></div>
        <p v-if="masterError" class="message error-message" role="alert">{{ masterError }}</p>
        <footer class="dialog-actions"><button class="kd-button primary" type="submit" :disabled="writing || !!lookupKind || !masterReady">確定</button><button class="kd-button" type="button" :disabled="writing || !!lookupKind" @click="closeMaster">關閉</button><button class="kd-button primary" type="button" :disabled="writing || !!lookupKind || !masterReady" @click="saveMaster">套用</button></footer>
      </form>
    </section></div>

    <div v-if="detailMode" class="dialog-backdrop" @click.self="closeDetail"><section ref="detailElement" class="win-dialog detail-dialog" role="dialog" aria-modal="true" aria-label="客戶站點明細" tabindex="-1" :inert="writing || !!lookupKind || confirmationMessage!==null" @keydown.esc.stop.prevent="closeDetail" @keydown.tab="trapTab">
      <header class="dialog-title">Customer OP Mapping Detail Define（{{ detailMode==='new'?'ADD':'EDIT' }}）</header>
      <form class="dialog-form" novalidate @submit.prevent="saveDetail(true)">
        <fieldset class="form-group" :disabled="writing || !!lookupKind || !detailReady || confirmationMessage!==null"><legend>Customer OP Mapping Detail</legend><div class="form-grid">
          <label>Customer No<input :value="detailParent?.customerNo" class="kd-input" readonly></label><label>Customer OPNo<input :value="detailParent?.customerOPNo" class="kd-input" readonly></label>
          <label>PS No<input :value="detailSource?.psNo ?? detailParent?.psNo" class="kd-input" readonly></label>
          <label>OPNo<div class="lookup-control"><input v-model="detailDraft.opNo" class="kd-input" maxlength="20" aria-label="作業站編號" @blur="uppercaseDetailOp"><button class="kd-button" type="button" aria-label="查找作業站" @click="openLookup('operation')">...</button></div></label>
          <label>CustOPProcessCode<input v-model="detailDraft.custOPProcessCode" class="kd-input" maxlength="20" aria-label="流程代碼"></label><label>CustStepName<input v-model="detailDraft.custStepName" class="kd-input" maxlength="20" aria-label="步驟名稱"></label>
          <label class="flag-label"><input type="checkbox" :checked="detailDraft.transactionFlag===1" :indeterminate="![0,1].includes(detailDraft.transactionFlag)" aria-label="傳送交易資料" @change="detailDraft.transactionFlag=($event.target as HTMLInputElement).checked?1:0"> SendTranSactionData<span v-if="![0,1].includes(detailDraft.transactionFlag)">原始值：{{ detailDraft.transactionFlag }}</span></label>
        </div></fieldset>
        <div class="audit-row"><span>建立人 {{ detailSource?.creator }}</span><span>建立日期 {{ displayDate(detailSource?.createDate) }}</span><span>修改人 {{ detailSource?.reviser }}</span><span>修改日期 {{ displayDate(detailSource?.reviseDate) }}</span></div>
        <p v-if="detailError" class="message error-message" role="alert">{{ detailError }}</p><p v-if="detailNotice" class="message notice-message" role="status">{{ detailNotice }}</p>
        <footer class="dialog-actions"><button class="kd-button primary" type="submit" :disabled="writing || !!lookupKind || !detailReady">確定</button><button class="kd-button" type="button" :disabled="writing || !!lookupKind" @click="closeDetail">關閉</button><button class="kd-button primary" type="button" :disabled="writing || !!lookupKind || !detailReady" @click="saveDetail(false)">套用</button></footer>
      </form>
    </section></div>

    <div v-if="lookupKind" class="dialog-backdrop lookup-backdrop" @click.self="closeLookup"><section ref="lookupElement" class="win-dialog picker-dialog" role="dialog" aria-modal="true" :aria-label="lookupTitle" tabindex="-1" :inert="writing || lookupLoading || confirmationMessage!==null" @keydown.esc.stop.prevent="closeLookup" @keydown.tab="trapTab">
      <header class="dialog-title">{{ lookupTitle }}</header>
      <fieldset class="picker-fields" :disabled="writing || lookupLoading || confirmationMessage!==null">
        <div class="picker-search"><input v-model="lookupKeyword" class="kd-input" aria-label="查找關鍵字" placeholder="編號或名稱" @keydown.enter.prevent></div>
        <div class="picker-scroll"><button v-for="option in visibleLookupOptions" :key="option.value" class="lookup-row" type="button" :disabled="!lookupReady" @click="chooseLookup(option)"><strong>{{ option.value }}</strong><span>{{ option.label }}</span><small>{{ option.psNo ?? option.description }}</small></button><p v-if="!visibleLookupOptions.length" class="empty-row">{{ lookupLoading?'載入中…':lookupError?'查找資料未能載入':'沒有可選資料' }}</p></div>
      </fieldset>
      <p v-if="lookupError" class="message error-message" role="alert">{{ lookupError }}</p>
      <footer class="dialog-actions"><button class="kd-button" type="button" :disabled="writing || lookupLoading" @click="closeLookup">關閉</button></footer>
    </section></div>
    <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
  </section>
</template>

<style scoped>
.customer-op-mapping-page{box-sizing:border-box;display:flex;flex-direction:column;height:100%;min-height:0;padding:8px 10px;background:#F7F5EE;color:#26251F;font:13px/1.4 "Microsoft JhengHei",Arial,sans-serif}.master-surface{display:flex;flex-direction:column;gap:6px;flex:1;min-height:0}
.query-strip{display:flex;align-items:center;flex-wrap:wrap;gap:8px;min-height:40px;padding:5px 8px;border:1px solid #d4d0c7;background:#fff}.issue-filter{display:flex;align-items:center;gap:10px;padding:0;margin:0;border:0}.issue-filter legend{float:left;margin-right:10px}.issue-filter label,.query-field{display:inline-flex;align-items:center;gap:5px}.query-input{width:180px}.dev-badge{padding:2px 7px;border:1px solid #d8c373;background:#fff5ce;color:#76631b;font-size:11px}
.kd-input{box-sizing:border-box;width:100%;min-width:0;height:29px;padding:4px 7px;border:1px solid #aaa69e;background:#fff1bf;color:#333;font:inherit}.kd-input:focus{outline:2px solid var(--yellow,#E7C600);outline-offset:1px}.kd-input[readonly]{background:#efeeeb}.kd-button{min-height:30px;padding:4px 9px;border:1px solid #bdb9b0;border-radius:3px;background:linear-gradient(#fff,#efeeeb);color:#343331;font:inherit;white-space:nowrap;cursor:pointer}.kd-button:hover:not(:disabled){border-color:var(--yellow,#E7C600);background:#fff5d4}.kd-button:disabled{opacity:.48;cursor:default}.kd-button.primary{background:#3B3930;border-color:#3B3930;color:white}.kd-button.primary:hover:not(:disabled){background:#514d3e}
.win-group{display:flex;flex-direction:column;min-width:0;min-height:0;margin:0;padding:7px;border:1px solid #c8c3b8;background:#fff}.win-group legend{padding:0 5px;color:#59564e}.master-panel{flex:1.2}.detail-panel{flex:1}.panel-layout{display:flex;gap:8px;flex:1;min-height:0}.grid-scroll{flex:1;min-width:0;min-height:0;overflow:auto;border:1px solid #c5c1b9}.kd-grid{width:max-content;min-width:100%;border-collapse:collapse;background:#fff;font-size:12px}.kd-grid th{position:sticky;top:0;z-index:1;background:#EEEAE0;color:#3B3930;text-align:left}.kd-grid td,.kd-grid th{padding:4px 7px;border:1px solid #d4d0c7;white-space:nowrap}.kd-grid tbody tr{cursor:pointer}.kd-grid tbody tr:hover{background:#fff8e2}.kd-grid tr.active{background:#ffedb1}.check-cell{width:26px;text-align:center}.kd-grid input[type=checkbox],.issue-filter input{accent-color:var(--yellow,#E7C600)}.vertical-actions{display:flex;flex-direction:column;gap:6px;flex:0 0 88px}.grid-status{padding:3px 2px 0;color:#777366;font-size:11px}.empty-row{height:44px;text-align:center;color:#777366}
.message{margin:0;padding:6px 8px;border:1px solid}.error-message{border-color:#d99b86;background:#fff0e9;color:#873820}.notice-message{border-color:#d8c79a;background:#fff8dc;color:#65541d}.footer-actions{display:flex;justify-content:flex-end;padding-top:3px}
.dialog-backdrop{position:fixed;inset:0;z-index:1200;display:flex;align-items:center;justify-content:center;padding:14px;background:#17171770}.win-dialog{box-sizing:border-box;display:flex;flex-direction:column;gap:8px;max-height:94vh;padding:12px;border:1px solid #aaa69e;background:#F7F5EE;box-shadow:0 8px 25px #0004}.header-dialog{width:min(700px,96vw);overflow:auto}.dialog-title{padding-bottom:7px;border-bottom:1px solid #d4d0c7;font-size:16px;font-weight:600}.form-group{margin:0;padding:10px;border:1px solid #c9c4b9}.form-grid{display:grid;grid-template-columns:1fr 1fr;gap:10px 16px}.form-grid>label{display:flex;flex-direction:column;gap:4px}.lookup-control{display:flex;gap:5px}.lookup-control .kd-input{flex:1}.description-label{grid-column:1/-1}.description-label textarea{height:72px;resize:vertical}.audit-row{display:grid;grid-template-columns:1fr 1fr;gap:4px 10px;padding:7px;border:1px solid #d0cabf;background:#f1eee6;color:#777366;font-size:11px}.dialog-actions{display:flex;justify-content:flex-end;gap:7px}.picker-dialog{width:min(800px,96vw);min-height:180px}.picker-fields{display:flex;flex-direction:column;min-height:0;gap:8px;margin:0;padding:0;border:0}.picker-search{display:flex;gap:6px}.picker-search .kd-input{flex:1}.picker-scroll{overflow:auto;max-height:55vh;border:1px solid #c5c1b9}
@media(max-width:900px){.query-strip{gap:6px}.issue-filter{flex-wrap:wrap}.form-grid{grid-template-columns:1fr}.description-label{grid-column:auto}.vertical-actions{flex-basis:78px}.picker-search{flex-wrap:wrap}.picker-search .kd-input{flex-basis:100%}}
.dialog-form{display:flex;flex-direction:column;gap:8px}.detail-dialog{width:min(700px,96vw);overflow:auto}.query-strip{margin:0}.query-field select{width:155px}.flag-label{flex-direction:row!important;align-items:center;gap:6px!important;grid-column:1/-1}.flag-label input{accent-color:var(--yellow,#E7C600)}.flag-label span,.field-hint{color:#777366;font-size:11px}.field-hint{margin:8px 0 0}.lookup-backdrop{z-index:1300}.lookup-row{box-sizing:border-box;display:grid;grid-template-columns:minmax(140px,1fr) minmax(140px,1fr) minmax(80px,1fr);gap:10px;width:100%;padding:8px;border:0;border-bottom:1px solid #d4d0c7;background:#fff;color:#343331;text-align:left;font:inherit;cursor:pointer}.lookup-row:hover:not(:disabled){background:#fff5d4}.lookup-row:focus-visible{outline:2px solid var(--yellow,#E7C600);outline-offset:-2px}.lookup-row small{color:#777366}.lookup-row:disabled{opacity:.5}.dialog-title{overflow-wrap:anywhere}.grid-scroll{scrollbar-width:thin;scrollbar-color:#7775 transparent}
@media(max-width:900px){.lookup-row{grid-template-columns:1fr 1fr}.lookup-row small{grid-column:1/-1}.query-field{flex-wrap:wrap}}
</style>
