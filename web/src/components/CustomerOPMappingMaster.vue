<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type Mode = 'new' | 'edit' | 'copy' | ''
type LookupKind = 'customer' | 'segment' | 'operation' | ''
interface MasterRow {
  customerNo: string; customerOPNo: string; customerOPName: string; psNo: string; customerOPSeq: number
  description: string; creator: string; createDate: string; reviser: string; reviseDate: string; dataStamp: number | null
}
interface DetailRow {
  customerNo: string; customerOPNo: string; psNo: string; opNo: string; custOPProcessCode: string; custStepName: string
  transactionFlag: number; creator: string; createDate: string; reviser: string; reviseDate: string; dataStamp: number | null
}
interface LookupOption { value: string; label: string; description: string; psNo?: string; issueState?: number }
interface ApiEnvelope<T> { items?: T[]; rows?: T[]; data?: T[]; message?: string; title?: string }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes.dev.opb03.customer-op-mapping.v1'

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

const masters = ref<MasterRow[]>([])
const allDetails = ref<DetailRow[]>([])
const details = ref<DetailRow[]>([])
const activeMasterKey = ref('')
const activeDetailKey = ref('')
const selectedMasterKeys = ref<string[]>([])
const selectedDetailKeys = ref<string[]>([])
const searchField = ref<'customerNo' | 'customerOPNo'>('customerNo')
const keyword = ref('')
const appliedKeyword = ref('')
const loading = ref(false)
const busy = ref(false)
const error = ref('')
const notice = ref('')
const masterMode = ref<Mode>('')
const detailMode = ref<Mode>('')
const originalMaster = ref<MasterRow | null>(null)
const originalDetail = ref<DetailRow | null>(null)
const masterDraft = ref(emptyMaster())
const detailDraft = ref(emptyDetail())
const masterAudit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const detailAudit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const lookupKind = ref<LookupKind>('')
const lookupKeyword = ref('')
const lookupOptions = ref<LookupOption[]>([])
const lookupBusy = ref(false)
const formError = ref('')

function emptyMaster() { return { customerNo: '', customerOPNo: '', customerOPName: '', psNo: '', customerOPSeq: 0, description: '' } }
function emptyDetail() { return { opNo: '', custOPProcessCode: '', custStepName: '', transactionFlag: 0 } }
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function nowText(): string { return new Date().toISOString() }
function currentUser(): string { return props.currentUserNo || (mock.value ? 'DEV001' : '') }
function masterKey(row: Pick<MasterRow, 'customerNo' | 'customerOPNo'>): string { return JSON.stringify([row.customerNo, row.customerOPNo]) }
function detailKey(row: Pick<DetailRow, 'customerNo' | 'customerOPNo' | 'opNo'>): string { return JSON.stringify([row.customerNo, row.customerOPNo, row.opNo]) }
function displayDate(value: string): string {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return new Intl.DateTimeFormat('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23' }).format(date).replaceAll('-', '/')
}
function normalizeMaster(source: Record<string, unknown>): MasterRow {
  return {
    customerNo: String(source.customerNo ?? source.customerno ?? ''), customerOPNo: String(source.customerOPNo ?? source.customeropno ?? ''),
    customerOPName: String(source.customerOPName ?? source.customeropname ?? ''), psNo: String(source.psNo ?? source.psno ?? ''),
    customerOPSeq: Number(source.customerOPSeq ?? source.customeropseq ?? 0), description: String(source.description ?? ''),
    creator: String(source.creator ?? ''), createDate: String(source.createDate ?? source.createdate ?? ''),
    reviser: String(source.reviser ?? ''), reviseDate: String(source.reviseDate ?? source.revisedate ?? ''),
    dataStamp: source.dataStamp == null && source.datastamp == null ? null : Number(source.dataStamp ?? source.datastamp),
  }
}
function normalizeDetail(source: Record<string, unknown>): DetailRow {
  return {
    customerNo: String(source.customerNo ?? source.customerno ?? ''), customerOPNo: String(source.customerOPNo ?? source.customeropno ?? ''),
    psNo: String(source.psNo ?? source.psno ?? ''), opNo: String(source.opNo ?? source.opno ?? ''),
    custOPProcessCode: String(source.custOPProcessCode ?? source.custopprocesscode ?? ''), custStepName: String(source.custStepName ?? source.custstepname ?? ''),
    transactionFlag: Number(source.transactionFlag ?? source.transactionflag ?? 0), creator: String(source.creator ?? ''),
    createDate: String(source.createDate ?? source.createdate ?? ''), reviser: String(source.reviser ?? ''),
    reviseDate: String(source.reviseDate ?? source.revisedate ?? ''),
    dataStamp: source.dataStamp == null && source.datastamp == null ? null : Number(source.dataStamp ?? source.datastamp),
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
function persistMock(): void { localStorage.setItem(storageKey, JSON.stringify({ masters: masters.value, details: allDetails.value })) }
function loadMock(): void {
  try {
    const saved = localStorage.getItem(storageKey)
    if (!saved) { masters.value = clone(seedMasters); allDetails.value = clone(seedDetails); persistMock(); return }
    const parsed = JSON.parse(saved) as { masters?: MasterRow[]; details?: DetailRow[] }
    masters.value = Array.isArray(parsed.masters) ? parsed.masters : clone(seedMasters)
    allDetails.value = Array.isArray(parsed.details) ? parsed.details : clone(seedDetails)
  } catch { masters.value = clone(seedMasters); allDetails.value = clone(seedDetails) }
}
const visibleMasters = computed(() => {
  const text = appliedKeyword.value.trim().toLocaleUpperCase()
  return !text ? masters.value : masters.value.filter((row) => String(row[searchField.value] ?? '').toLocaleUpperCase().includes(text))
})
const activeMaster = computed(() => masters.value.find((row) => masterKey(row) === activeMasterKey.value) ?? null)
const activeDetail = computed(() => details.value.find((row) => detailKey(row) === activeDetailKey.value) ?? null)
const visibleLookupOptions = computed(() => {
  const query = lookupKeyword.value.trim().toLocaleUpperCase()
  const options = lookupOptions.value.filter((item) => (item.issueState == null || item.issueState === 2)
    && (lookupKind.value !== 'operation' || !activeMaster.value?.psNo || item.psNo === activeMaster.value.psNo))
  return query ? options.filter((item) => (item.value + ' ' + item.label + ' ' + item.description).toLocaleUpperCase().includes(query)) : options
})
const masterModeLabel = computed(() => masterMode.value === 'new' ? 'ADD' : masterMode.value === 'edit' ? 'EDIT' : 'Copy')
const detailModeLabel = computed(() => detailMode.value === 'new' ? 'ADD' : 'EDIT')
const lookupTitle = computed(() => lookupKind.value === 'customer' ? 'Customer No' : lookupKind.value === 'segment' ? 'PS No' : 'OPNo')

async function loadDetails(customerNo: string, customerOPNo: string): Promise<void> {
  activeMasterKey.value = JSON.stringify([customerNo, customerOPNo])
  activeDetailKey.value = ''; selectedDetailKeys.value = []; error.value = ''
  if (mock.value) {
    details.value = clone(allDetails.value.filter((row) => row.customerNo === customerNo && row.customerOPNo === customerOPNo))
    return
  }
  try {
    const path = '/api/customer-op-mappings/' + encodeURIComponent(customerNo) + '/' + encodeURIComponent(customerOPNo) + '/details'
    details.value = responseRows(await api<DetailRow[] | ApiEnvelope<DetailRow>>(path)).map((row) => normalizeDetail(row as unknown as Record<string, unknown>))
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入客戶站點明細失敗。'; details.value = [] }
}
async function loadMasters(): Promise<void> {
  loading.value = true; error.value = ''; notice.value = ''; selectedMasterKeys.value = []; selectedDetailKeys.value = []
  try {
    if (mock.value) {
      loadMock()
      appliedKeyword.value = keyword.value
    } else {
      const query = new URLSearchParams()
      if (keyword.value.trim()) query.set(searchField.value, keyword.value.trim())
      const suffix = query.toString()
      const result = await api<MasterRow[] | ApiEnvelope<MasterRow>>('/api/customer-op-mappings' + (suffix ? '?' + suffix : ''))
      masters.value = responseRows(result).map((row) => normalizeMaster(row as unknown as Record<string, unknown>))
      appliedKeyword.value = keyword.value
    }
    const next = visibleMasters.value.some((row) => masterKey(row) === activeMasterKey.value) ? activeMaster.value : visibleMasters.value[0] ?? null
    if (next) await loadDetails(next.customerNo, next.customerOPNo)
    else { activeMasterKey.value = ''; details.value = []; activeDetailKey.value = '' }
    notice.value = '資料總筆數 ' + visibleMasters.value.length
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入客戶站點對應資料失敗。' }
  finally { loading.value = false }
}
function setMasterChecked(row: MasterRow, checked: boolean): void {
  const key = masterKey(row)
  selectedMasterKeys.value = checked ? Array.from(new Set([...selectedMasterKeys.value, key])) : selectedMasterKeys.value.filter((item) => item !== key)
  if (checked) void loadDetails(row.customerNo, row.customerOPNo)
}
function setDetailChecked(row: DetailRow, checked: boolean): void {
  const key = detailKey(row)
  selectedDetailKeys.value = checked ? Array.from(new Set([...selectedDetailKeys.value, key])) : selectedDetailKeys.value.filter((item) => item !== key)
  if (checked) activeDetailKey.value = key
}
function openMaster(mode: Exclude<Mode, ''>, row?: MasterRow): void {
  formError.value = ''; originalMaster.value = row ? clone(row) : null
  if (row) {
    masterDraft.value = { customerNo: row.customerNo, customerOPNo: row.customerOPNo, customerOPName: row.customerOPName, psNo: row.psNo, customerOPSeq: row.customerOPSeq, description: row.description }
    masterAudit.value = { creator: mode === 'edit' ? row.creator : currentUser(), createDate: mode === 'edit' ? row.createDate : nowText(), reviser: mode === 'edit' ? currentUser() : '', reviseDate: mode === 'edit' ? nowText() : '' }
  } else { masterDraft.value = emptyMaster(); masterAudit.value = { creator: currentUser(), createDate: nowText(), reviser: '', reviseDate: '' } }
  masterMode.value = mode
}
function closeMaster(): void { masterMode.value = ''; originalMaster.value = null; formError.value = '' }
async function saveMaster(): Promise<void> {
  const draft = masterDraft.value
  draft.customerNo = draft.customerNo.trim().toUpperCase(); draft.customerOPNo = draft.customerOPNo.trim().toUpperCase()
  draft.customerOPName = draft.customerOPName.trim(); draft.psNo = draft.psNo.trim().toUpperCase(); draft.description = draft.description.trim()
  if (!draft.customerNo || !draft.customerOPNo || !draft.customerOPName || !draft.psNo) { formError.value = 'Customer No、Customer OPNo、Customer OP Name 與 PS No 必填。'; return }
  if (!Number.isInteger(draft.customerOPSeq) || draft.customerOPSeq < 0 || draft.customerOPSeq > 999999) { formError.value = 'OP Group Order 必須是 0–999999 的整數。'; return }
  busy.value = true; formError.value = ''; error.value = ''
  try {
    const original = originalMaster.value
    if (mock.value) {
      const duplicate = masters.value.some((row) => row.customerNo === draft.customerNo && row.customerOPNo === draft.customerOPNo && (!original || masterKey(row) !== masterKey(original)))
      if (duplicate) throw new Error('Customer No 與 Customer OPNo 已存在。')
      if (masterMode.value === 'edit' && original) {
        const index = masters.value.findIndex((row) => masterKey(row) === masterKey(original))
        const updated: MasterRow = { ...original, ...draft, reviser: currentUser(), reviseDate: nowText(), dataStamp: original.dataStamp == null ? null : original.dataStamp + 1 }
        if (index >= 0) masters.value.splice(index, 1, updated)
      } else {
        masters.value.unshift({ ...draft, creator: currentUser(), createDate: nowText(), reviser: '', reviseDate: '', dataStamp: 0 })
      }
      persistMock()
    } else {
      const body = { ...draft, dataStamp: original?.dataStamp }
      if (masterMode.value === 'edit' && original) {
        const path = '/api/customer-op-mappings/' + encodeURIComponent(original.customerNo) + '/' + encodeURIComponent(original.customerOPNo)
        await api(path, { method: 'PUT', body: JSON.stringify(body) })
      } else await api('/api/customer-op-mappings', { method: 'POST', body: JSON.stringify(draft) })
    }
    const key = JSON.stringify([draft.customerNo, draft.customerOPNo]); closeMaster(); keyword.value = ''; appliedKeyword.value = ''
    await loadMasters(); if (masters.value.some((row) => masterKey(row) === key)) await loadDetails(draft.customerNo, draft.customerOPNo)
  } catch (cause) { formError.value = cause instanceof Error ? cause.message : '儲存主檔失敗。' }
  finally { busy.value = false }
}
function masterDeleteTargets(): MasterRow[] {
  const keys = selectedMasterKeys.value.length ? selectedMasterKeys.value : activeMaster.value ? [activeMasterKey.value] : []
  return masters.value.filter((row) => keys.includes(masterKey(row)))
}
async function deleteMasters(): Promise<void> {
  const targets = masterDeleteTargets()
  if (!targets.length) return
  if (!window.confirm('確定刪除選取的 ' + targets.length + ' 筆客戶站點資料及其所有明細？')) return
  busy.value = true; error.value = ''; let deleted = false
  try {
    for (const row of targets) {
      if (mock.value) {
        masters.value = masters.value.filter((item) => masterKey(item) !== masterKey(row))
        allDetails.value = allDetails.value.filter((item) => item.customerNo !== row.customerNo || item.customerOPNo !== row.customerOPNo)
        persistMock()
      } else {
        const path = '/api/customer-op-mappings/' + encodeURIComponent(row.customerNo) + '/' + encodeURIComponent(row.customerOPNo)
        await api(path, { method: 'DELETE' })
      }
      deleted = true
    }
    if (deleted) await loadMasters()
  } catch (cause) {
    const message = cause instanceof Error ? cause.message : '刪除主檔失敗。'
    if (deleted) await loadMasters()
    error.value = message
  }
  finally { busy.value = false }
}
function openDetail(mode: 'new' | 'edit', row?: DetailRow): void {
  if (!activeMaster.value) return
  formError.value = ''; originalDetail.value = row ? clone(row) : null
  detailDraft.value = row ? { opNo: row.opNo, custOPProcessCode: row.custOPProcessCode, custStepName: row.custStepName, transactionFlag: row.transactionFlag } : emptyDetail()
  detailAudit.value = row
    ? { creator: row.creator, createDate: row.createDate, reviser: currentUser(), reviseDate: nowText() }
    : { creator: currentUser(), createDate: nowText(), reviser: '', reviseDate: '' }
  detailMode.value = mode
}
function closeDetail(): void { detailMode.value = ''; originalDetail.value = null; formError.value = '' }
async function saveDetail(closeAfter: boolean): Promise<void> {
  if (!activeMaster.value) return
  const group = activeMaster.value
  const draft = detailDraft.value
  draft.opNo = draft.opNo.trim().toUpperCase(); draft.custOPProcessCode = draft.custOPProcessCode.trim(); draft.custStepName = draft.custStepName.trim()
  if (!draft.opNo || !draft.custOPProcessCode || !draft.custStepName) { formError.value = 'OPNo、CustOPProcessCode 與 CustStepName 必填。'; return }
  if (draft.transactionFlag !== 0 && draft.transactionFlag !== 1) { formError.value = 'SendTranSactionData 只能是 0 或 1。'; return }
  busy.value = true; formError.value = ''; error.value = ''
  try {
    const original = originalDetail.value
    const candidateKey = JSON.stringify([group.customerNo, group.customerOPNo, draft.opNo])
    const duplicate = details.value.some((row) => detailKey(row) === candidateKey && (!original || detailKey(row) !== detailKey(original)))
    if (duplicate) throw new Error('相同 Customer No、Customer OPNo 與 OPNo 的明細已存在。')
    if (mock.value) {
      if (detailMode.value === 'edit' && original) {
        const index = allDetails.value.findIndex((row) => detailKey(row) === detailKey(original))
        const updated: DetailRow = { ...original, ...draft, opNo: draft.opNo, reviser: currentUser(), reviseDate: nowText(), dataStamp: original.dataStamp == null ? null : original.dataStamp + 1 }
        if (index >= 0) allDetails.value.splice(index, 1, updated)
      } else allDetails.value.push({ ...group, ...draft, creator: currentUser(), createDate: nowText(), reviser: '', reviseDate: '', dataStamp: 0 })
      persistMock()
    } else {
      const payload = {
        customerNo: group.customerNo, customerOPNo: group.customerOPNo, psNo: group.psNo,
        opNo: draft.opNo, custOPProcessCode: draft.custOPProcessCode, custStepName: draft.custStepName,
        transactionFlag: draft.transactionFlag, dataStamp: original?.dataStamp,
      }
      if (detailMode.value === 'edit' && original) {
        const path = '/api/customer-op-mappings/' + encodeURIComponent(group.customerNo) + '/' + encodeURIComponent(group.customerOPNo) + '/details/' + encodeURIComponent(original.opNo)
        await api(path, { method: 'PUT', body: JSON.stringify({ ...payload, opNo: draft.opNo }) })
      } else {
        const path = '/api/customer-op-mappings/' + encodeURIComponent(group.customerNo) + '/' + encodeURIComponent(group.customerOPNo) + '/details'
        await api(path, { method: 'POST', body: JSON.stringify(payload) })
      }
    }
    const nextOpNo = draft.opNo
    if (closeAfter) closeDetail()
    await loadDetails(group.customerNo, group.customerOPNo)
    const saved = details.value.find((row) => row.opNo === nextOpNo)
    if (saved) {
      activeDetailKey.value = detailKey(saved)
      if (!closeAfter) {
        originalDetail.value = clone(saved)
        detailDraft.value = { opNo: saved.opNo, custOPProcessCode: saved.custOPProcessCode, custStepName: saved.custStepName, transactionFlag: saved.transactionFlag }
        detailAudit.value = { creator: saved.creator, createDate: saved.createDate, reviser: saved.reviser, reviseDate: saved.reviseDate }
      }
    }
  } catch (cause) { formError.value = cause instanceof Error ? cause.message : '儲存明細失敗。' }
  finally { busy.value = false }
}
function detailDeleteTargets(): DetailRow[] {
  const keys = selectedDetailKeys.value.length ? selectedDetailKeys.value : activeDetail.value ? [activeDetailKey.value] : []
  return details.value.filter((row) => keys.includes(detailKey(row)))
}
async function deleteDetails(): Promise<void> {
  const targets = detailDeleteTargets()
  if (!targets.length || !activeMaster.value) return
  if (!window.confirm('確定刪除選取的 ' + targets.length + ' 筆明細？')) return
  busy.value = true; error.value = ''; let deleted = false
  try {
    for (const row of targets) {
      if (mock.value) {
        allDetails.value = allDetails.value.filter((item) => detailKey(item) !== detailKey(row))
        persistMock()
      } else {
        const path = '/api/customer-op-mappings/' + encodeURIComponent(row.customerNo) + '/' + encodeURIComponent(row.customerOPNo) + '/details/' + encodeURIComponent(row.opNo)
        await api(path, { method: 'DELETE' })
      }
      deleted = true
    }
    if (deleted && activeMaster.value) await loadDetails(activeMaster.value.customerNo, activeMaster.value.customerOPNo)
  } catch (cause) {
    const message = cause instanceof Error ? cause.message : '刪除明細失敗。'
    const parent = activeMaster.value
    if (deleted && parent) await loadDetails(parent.customerNo, parent.customerOPNo)
    error.value = message
  }
  finally { busy.value = false }
}
async function openLookup(kind: Exclude<LookupKind, ''>): Promise<void> {
  lookupKind.value = kind; lookupKeyword.value = ''; lookupOptions.value = []; lookupBusy.value = true
  try {
    if (mock.value) lookupOptions.value = clone(kind === 'customer' ? seedCustomers : kind === 'segment' ? seedSegments : seedOperations)
    else {
      const params = new URLSearchParams()
      params.set('issueState', '2')
      if (kind === 'operation' && activeMaster.value) params.set('psNo', activeMaster.value.psNo)
      const resource = kind === 'customer' ? 'customers' : kind === 'segment' ? 'production-segments' : 'operations'
      const path = '/api/customer-op-mappings/lookups/' + resource + '?' + params.toString()
      const result = await api<Record<string, unknown>[] | ApiEnvelope<Record<string, unknown>>>(path)
      lookupOptions.value = responseRows(result).map((row) => ({
        value: String(row.customerNo ?? row.customerno ?? row.psNo ?? row.psno ?? row.opNo ?? row.opno ?? ''),
        label: String(row.customerName ?? row.customername ?? row.psName ?? row.psname ?? row.opName ?? row.opname ?? row.name ?? ''),
        description: String(row.description ?? ''), psNo: row.psNo == null && row.psno == null ? undefined : String(row.psNo ?? row.psno),
        issueState: row.issueState == null && row.issuestate == null ? undefined : Number(row.issueState ?? row.issuestate),
      }))
    }
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入選擇資料失敗。' }
  finally { lookupBusy.value = false }
}
function chooseLookup(option: LookupOption): void {
  if (lookupKind.value === 'customer') masterDraft.value.customerNo = option.value.toUpperCase()
  else if (lookupKind.value === 'segment') masterDraft.value.psNo = option.value.toUpperCase()
  else if (lookupKind.value === 'operation') detailDraft.value.opNo = option.value.toUpperCase()
  lookupKind.value = ''
}
function closeLookup(): void { lookupKind.value = '' }
onMounted(() => { void loadMasters() })
</script>

<template>
  <section class="customer-op-mapping-page">
    <header class="page-heading"><h1>客戶站點對應設定</h1><span v-if="mock" class="dev-badge">DEV 假資料</span></header>
    <div class="query-bar">
      <label for="customer-op-search-field">Customer OP Mapping</label>
      <select id="customer-op-search-field" v-model="searchField">
        <option value="customerNo">Customer No</option><option value="customerOPNo">Customer OPNo</option>
      </select>
      <input v-model="keyword" maxlength="50" aria-label="查詢內容" @keydown.enter.prevent="loadMasters">
      <span>%</span><button class="mes-button" :disabled="loading || busy" @click="loadMasters">查詢</button>
      <span v-if="loading" class="subtle">載入中…</span>
    </div>

    <section class="data-panel master-panel">
      <h2>Customer OP Mapping Basis</h2>
      <div class="panel-content">
        <div class="grid-scroll master-grid-scroll">
          <table class="mes-grid">
            <thead><tr><th class="row-selector"></th><th>CustomerNo</th><th>CustomerOPNo</th><th>CustomerOPName</th><th>PSNo</th><th>CustomerOPSeq</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
            <tbody>
              <tr v-for="row in visibleMasters" :key="masterKey(row)" :class="{ active: masterKey(row) === activeMasterKey }" @click="loadDetails(row.customerNo, row.customerOPNo)" @dblclick="openMaster('edit', row)">
                <td class="row-selector"><input type="checkbox" :checked="selectedMasterKeys.includes(masterKey(row))" :aria-label="'選取 ' + row.customerNo + ' ' + row.customerOPNo" @click.stop @change="setMasterChecked(row, ($event.target as HTMLInputElement).checked)"></td>
                <td>{{ row.customerNo }}</td><td>{{ row.customerOPNo }}</td><td>{{ row.customerOPName }}</td><td>{{ row.psNo }}</td><td class="numeric">{{ row.customerOPSeq }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ displayDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ displayDate(row.reviseDate) }}</td>
              </tr>
              <tr v-if="!visibleMasters.length"><td colspan="11" class="empty-cell">{{ loading ? '載入中…' : '沒有符合的資料' }}</td></tr>
            </tbody>
          </table>
        </div>
        <aside class="action-stack">
          <button class="mes-button primary" :disabled="busy" @click="openMaster('new')">ADD (&amp;A)</button>
          <button class="mes-button" :disabled="busy || !activeMaster" @click="activeMaster && openMaster('edit', activeMaster)">EDIT (&amp;E)</button>
          <button class="mes-button" :disabled="busy || !masterDeleteTargets().length" @click="deleteMasters">DEL (&amp;D)</button>
          <button class="mes-button" :disabled="busy || !activeMaster" @click="activeMaster && openMaster('copy', activeMaster)">Copy (&amp;C)</button>
        </aside>
      </div>
      <footer class="panel-footer"><span>資料總筆數 {{ visibleMasters.length }}</span><span>{{ notice }}</span></footer>
    </section>

    <section class="data-panel detail-panel">
      <h2>Customer OP Mapping Detail <span v-if="activeMaster" class="parent-key">{{ activeMaster.customerNo }} / {{ activeMaster.customerOPNo }}</span></h2>
      <div class="panel-content">
        <div class="grid-scroll detail-grid-scroll">
          <table class="mes-grid">
            <thead><tr><th class="row-selector"></th><th>PSNo</th><th>OPNo</th><th>CustOPProcessCode</th><th>CustStepName</th><th>TransactionFlag</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
            <tbody>
              <tr v-for="row in details" :key="detailKey(row)" :class="{ active: detailKey(row) === activeDetailKey }" @click="activeDetailKey = detailKey(row)" @dblclick="openDetail('edit', row)">
                <td class="row-selector"><input type="checkbox" :checked="selectedDetailKeys.includes(detailKey(row))" :aria-label="'選取 ' + row.opNo" @click.stop @change="setDetailChecked(row, ($event.target as HTMLInputElement).checked)"></td>
                <td>{{ row.psNo }}</td><td>{{ row.opNo }}</td><td>{{ row.custOPProcessCode }}</td><td>{{ row.custStepName }}</td><td>{{ row.transactionFlag === 1 ? 'Yes' : 'No' }}</td><td>{{ row.creator }}</td><td>{{ displayDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ displayDate(row.reviseDate) }}</td>
              </tr>
              <tr v-if="!details.length"><td colspan="10" class="empty-cell">{{ activeMaster ? '沒有明細資料' : '請先選取主檔資料' }}</td></tr>
            </tbody>
          </table>
        </div>
        <aside class="action-stack">
          <button class="mes-button primary" :disabled="busy || !activeMaster" @click="openDetail('new')">ADD</button>
          <button class="mes-button" :disabled="busy || !activeDetail" @click="activeDetail && openDetail('edit', activeDetail)">EDIT</button>
          <button class="mes-button" :disabled="busy || !detailDeleteTargets().length" @click="deleteDetails">DEL</button>
        </aside>
      </div>
      <footer class="panel-footer"><span>資料總筆數 {{ details.length }}</span><span v-if="error" class="error-text">{{ error }}</span></footer>
    </section>
    <div class="page-bottom"><span v-if="error && details.length">{{ error }}</span><button class="mes-button close-button" @click="emit('close')">Close (&amp;X)</button></div>

    <div v-if="masterMode" class="modal-backdrop" @click.self="closeMaster">
      <section class="modal-card master-dialog" role="dialog" aria-modal="true" aria-labelledby="master-dialog-title">
        <header class="dialog-header"><h2 id="master-dialog-title">Customer OP Mapping Define ({{ masterModeLabel }})</h2><button class="dialog-x" aria-label="關閉" @click="closeMaster">×</button></header>
        <form class="dialog-form" @submit.prevent="saveMaster">
          <fieldset class="form-group">
            <legend>Customer OP Mapping Basis</legend>
            <div class="form-grid">
              <label for="map-customer-no">Customer No</label>
              <div class="lookup-control"><input id="map-customer-no" v-model="masterDraft.customerNo" maxlength="50" readonly required><button type="button" class="lookup-button" :disabled="masterMode === 'edit'" @click="openLookup('customer')">...</button></div>
              <label for="map-customer-op-no">Customer OPNo</label>
              <input id="map-customer-op-no" v-model="masterDraft.customerOPNo" maxlength="20" :readonly="masterMode === 'edit'" required @blur="masterDraft.customerOPNo = masterDraft.customerOPNo.toUpperCase()">
              <label for="map-customer-op-name">Customer OP Name</label><input id="map-customer-op-name" v-model="masterDraft.customerOPName" maxlength="50" required>
              <label for="map-ps-no">PS No</label>
              <div class="lookup-control"><input id="map-ps-no" v-model="masterDraft.psNo" maxlength="50" readonly required><button type="button" class="lookup-button" :disabled="masterMode === 'edit' && details.length > 0" @click="openLookup('segment')">...</button></div>
              <label for="map-seq">OP Group Order</label><input id="map-seq" v-model.number="masterDraft.customerOPSeq" type="number" min="0" max="999999" step="1" required>
              <label for="map-description">Description</label><textarea id="map-description" v-model="masterDraft.description" maxlength="255" rows="3"></textarea>
            </div>
            <p v-if="masterMode === 'edit' && details.length" class="field-hint">已有明細，WinForms 會鎖定 PS No。</p>
          </fieldset>
          <div class="audit-grid">
            <label>Creator</label><input :value="masterAudit.creator" readonly>
            <label>CreateDate</label><input :value="displayDate(masterAudit.createDate)" readonly>
            <label>Reviser</label><input :value="masterAudit.reviser" readonly>
            <label>ReviseDate</label><input :value="displayDate(masterAudit.reviseDate)" readonly>
          </div>
          <p v-if="formError" class="form-error">{{ formError }}</p>
          <footer class="dialog-actions"><button type="submit" class="mes-button primary" :disabled="busy">確定</button><button type="button" class="mes-button" @click="closeMaster">關閉</button><button type="button" class="mes-button" :disabled="busy" @click="saveMaster">套用</button></footer>
        </form>
      </section>
    </div>

    <div v-if="detailMode" class="modal-backdrop" @click.self="closeDetail">
      <section class="modal-card detail-dialog" role="dialog" aria-modal="true" aria-labelledby="detail-dialog-title">
        <header class="dialog-header"><h2 id="detail-dialog-title">Customer OP Mapping Detail Define ({{ detailModeLabel }})</h2><button class="dialog-x" aria-label="關閉" @click="closeDetail">×</button></header>
        <form class="dialog-form" @submit.prevent="saveDetail(true)">
          <fieldset class="form-group detail-fields">
            <legend>Customer OP Mapping Detail</legend>
            <div class="form-grid detail-form-grid">
              <label>Customer No</label><input :value="activeMaster?.customerNo" readonly>
              <label>Customer OPNo</label><input :value="activeMaster?.customerOPNo" readonly>
              <label>PS No</label><input :value="activeMaster?.psNo" readonly>
              <label for="map-op-no">OPNo</label><div class="lookup-control"><input id="map-op-no" v-model="detailDraft.opNo" maxlength="20" required @blur="detailDraft.opNo = detailDraft.opNo.toUpperCase()"><button type="button" class="lookup-button" @click="openLookup('operation')">...</button></div>
              <label for="map-process-code">CustOPProcessCode</label><input id="map-process-code" v-model="detailDraft.custOPProcessCode" maxlength="20" required>
              <label for="map-step-name">CustStepName</label><input id="map-step-name" v-model="detailDraft.custStepName" maxlength="20" required>
              <span class="field-spacer"></span><label class="checkbox-label"><input v-model="detailDraft.transactionFlag" type="checkbox" :true-value="1" :false-value="0"> SendTranSactionData</label>
            </div>
          </fieldset>
          <div class="audit-grid">
            <label>Creator</label><input :value="detailAudit.creator" readonly>
            <label>CreateDate</label><input :value="displayDate(detailAudit.createDate)" readonly>
            <label>Reviser</label><input :value="detailAudit.reviser" readonly>
            <label>ReviseDate</label><input :value="displayDate(detailAudit.reviseDate)" readonly>
          </div>
          <p v-if="formError" class="form-error">{{ formError }}</p>
          <footer class="dialog-actions"><button type="submit" class="mes-button primary" :disabled="busy">確定</button><button type="button" class="mes-button" @click="closeDetail">關閉</button><button type="button" class="mes-button" :disabled="busy" @click="saveDetail(false)">套用</button></footer>
        </form>
      </section>
    </div>

    <div v-if="lookupKind" class="modal-backdrop lookup-backdrop" @click.self="closeLookup">
      <section class="modal-card lookup-dialog" role="dialog" aria-modal="true" aria-labelledby="lookup-dialog-title">
        <header class="dialog-header"><h2 id="lookup-dialog-title">選擇 {{ lookupTitle }}</h2><button class="dialog-x" aria-label="關閉" @click="closeLookup">×</button></header>
        <div class="lookup-search"><input v-model="lookupKeyword" :placeholder="'搜尋 ' + lookupTitle" @keydown.enter.prevent><button class="mes-button" @click="lookupKeyword = lookupKeyword.trim()">查詢</button></div>
        <div class="lookup-list"><button v-for="option in visibleLookupOptions" :key="option.value" class="lookup-row" @click="chooseLookup(option)"><strong>{{ option.value }}</strong><span>{{ option.label }}</span><small>{{ option.description }}</small></button><p v-if="!visibleLookupOptions.length" class="empty-cell">{{ lookupBusy ? '載入中…' : '沒有可選資料' }}</p></div>
        <footer class="dialog-actions"><button class="mes-button" @click="closeLookup">關閉</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.customer-op-mapping-page{box-sizing:border-box;height:100%;min-height:560px;padding:0 10px 8px;display:flex;flex-direction:column;gap:7px;background:#fff;color:#3d3d3d;font-size:13px}
.page-heading{height:52px;flex:none;display:flex;align-items:center;gap:12px;border-bottom:1px solid #e2ded6}.page-heading h1{margin:0 0 0 12px;font-size:20px;font-weight:600;color:#39352e}
.dev-badge{padding:3px 8px;border:1px solid #dfc783;border-radius:3px;background:#fff5d6;color:#76560b;font-size:11px}
.query-bar{min-height:40px;flex:none;display:flex;align-items:center;gap:8px;padding:5px 10px;border:1px solid #d5d5d5;background:#fff}.query-bar label{font-weight:600}.query-bar select,.query-bar input{height:28px;border:1px solid #aaa;background:#fff2ba;padding:3px 7px;color:#333}.query-bar select{width:155px}.query-bar input{width:min(230px,30vw)}.subtle{color:#777}
.data-panel{min-height:0;border:1px solid #bdbdbd;background:#fff;padding:7px;display:flex;flex-direction:column}.master-panel{flex:1.18}.detail-panel{flex:0.9}.data-panel h2{font-size:14px;font-weight:600;margin:0 0 6px;color:#554b3e}.panel-content{display:flex;min-height:0;flex:1;gap:8px}.grid-scroll{min-width:0;flex:1;overflow:auto;border:1px solid #b8b8b8}.master-grid-scroll{min-height:150px}.detail-grid-scroll{min-height:120px}.mes-grid{border-collapse:collapse;min-width:100%;width:max-content;font-size:12px;white-space:nowrap}.mes-grid th{position:sticky;top:0;z-index:1;height:27px;padding:4px 8px;border:1px solid #d0d0d0;background:#4ba1d9;color:#fff;font-weight:600;text-align:left}.mes-grid td{height:27px;padding:3px 8px;border:1px solid #c9c9c9;background:#fff;color:#363636}.mes-grid tbody tr:nth-child(even) td{background:#fafafa}.mes-grid tbody tr:hover td{background:#fff3cf}.mes-grid tbody tr.active td{background:#ffe39a}.mes-grid .row-selector{width:27px;min-width:27px;text-align:center;padding:2px}.row-selector input{margin:0;accent-color:#ef9b23}.numeric{text-align:right}.empty-cell{text-align:center!important;color:#858585!important;padding:22px!important}.action-stack{width:80px;flex:none;display:flex;flex-direction:column;gap:6px}.mes-button{min-height:30px;padding:4px 10px;border:1px solid #c8c8c8;border-radius:3px;background:linear-gradient(#fff,#f3f3f3);color:#333;font-size:12px;white-space:nowrap;cursor:pointer}.mes-button:hover:not(:disabled){border-color:#e2a849;background:#fff5dd}.mes-button:disabled{opacity:.55;cursor:not-allowed}.mes-button.primary{border-color:#e99a27;background:#ff9f24;color:#fff;font-weight:600}.mes-button.primary:hover:not(:disabled){background:#ed8e12}.panel-footer{min-height:20px;flex:none;display:flex;align-items:end;justify-content:space-between;padding-top:4px;color:#787878;font-size:11px}.parent-key{margin-left:12px;color:#81745f;font-size:12px;font-weight:400}.page-bottom{min-height:34px;flex:none;display:flex;align-items:center;justify-content:space-between}.close-button{width:80px}.error-text,.page-bottom>span{color:#b42318}.modal-backdrop{position:fixed;inset:0;z-index:80;display:flex;align-items:center;justify-content:center;padding:16px;background:rgba(35,35,35,.34)}.modal-card{box-sizing:border-box;max-height:calc(100vh - 32px);overflow:auto;border:1px solid #aaa;border-radius:4px;background:#fdfdfd;box-shadow:0 12px 36px rgba(0,0,0,.24)}.master-dialog{width:min(616px,100%)}.detail-dialog{width:min(484px,100%)}.dialog-header{height:39px;display:flex;align-items:center;justify-content:space-between;padding:0 12px;border-bottom:1px solid #d5d5d5;background:#f5f2eb}.dialog-header h2{margin:0;font-size:14px;font-weight:600}.dialog-x{width:28px;height:28px;border:0;background:transparent;color:#555;font-size:22px;cursor:pointer}.dialog-form{padding:8px 12px 10px}.form-group{margin:0;padding:9px 10px 11px;border:1px solid #bbb}.form-group legend{padding:0 5px;color:#555;font-size:13px}.form-grid{display:grid;grid-template-columns:102px minmax(0,1fr) 102px minmax(0,1fr);align-items:center;gap:8px 7px}.form-grid>label{text-align:right}.form-grid input,.form-grid textarea,.lookup-control input,.audit-grid input,.lookup-search input{box-sizing:border-box;width:100%;min-width:0;height:27px;padding:4px 6px;border:1px solid #999;border-radius:0;background:#fff2ba;color:#333;font:inherit}.form-grid input:read-only,.audit-grid input{background:#ededed;color:#666}.form-grid textarea{height:auto;resize:vertical;grid-column:2 / 5}.lookup-control{display:flex;min-width:0;gap:4px}.lookup-control input{flex:1}.lookup-button{width:28px;height:27px;flex:none;border:1px solid #c5c5c5;border-radius:3px;background:#f6f6f6;cursor:pointer}.lookup-button:disabled{opacity:.45;cursor:not-allowed}.field-hint{margin:6px 0 0 109px;color:#7a6a4e;font-size:11px}.audit-grid{display:grid;grid-template-columns:74px minmax(0,1fr) 74px minmax(0,1fr);align-items:center;gap:6px 7px;margin-top:8px;padding:7px;border:1px solid #d2d2d2;background:#fafafa}.audit-grid label{text-align:right;color:#666}.audit-grid input{height:25px}.dialog-actions{display:flex;justify-content:flex-end;gap:7px;margin-top:10px}.form-error{margin:7px 0 0;color:#b42318}.detail-fields{padding-bottom:12px}.detail-form-grid{grid-template-columns:116px minmax(0,1fr)}.detail-form-grid>label{text-align:right}.detail-form-grid .field-spacer{grid-column:1}.checkbox-label{display:flex;align-items:center;gap:6px;justify-self:start}.checkbox-label input{width:auto;height:auto;accent-color:#e99a27}.lookup-backdrop{z-index:90}.lookup-dialog{width:min(560px,100%)}.lookup-search{display:flex;gap:8px;padding:10px}.lookup-search input{flex:1}.lookup-list{max-height:min(55vh,440px);overflow:auto;margin:0 10px;border:1px solid #ccc}.lookup-row{width:100%;min-height:37px;display:grid;grid-template-columns:170px minmax(100px,1fr) minmax(120px,1fr);align-items:center;gap:8px;padding:5px 8px;border:0;border-bottom:1px solid #e0e0e0;background:#fff;text-align:left;color:#333;cursor:pointer}.lookup-row:hover{background:#fff0c1}.lookup-row small{color:#777}.lookup-dialog>.dialog-actions{padding:0 10px 10px}
@media(max-width:900px){.customer-op-mapping-page{min-height:760px}.query-bar{flex-wrap:wrap}.panel-content{flex-direction:column}.action-stack{width:auto;flex-direction:row;flex-wrap:wrap}.action-stack .mes-button{min-width:76px}.master-panel,.detail-panel{flex:1}.form-grid{grid-template-columns:100px minmax(0,1fr)}.form-grid textarea{grid-column:2}.form-grid>label{text-align:right}.audit-grid{grid-template-columns:70px minmax(0,1fr)}.lookup-row{grid-template-columns:minmax(100px,1fr) minmax(100px,1fr)}}
</style>
