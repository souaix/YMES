<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type EditMode = 'add' | 'edit' | 'copy' | ''
type DataFlag = -1 | 0 | 1 | 2
type PickerKind = 'customer' | 'group' | 'element' | 'editorCustomer' | 'editorGroup' | 'addElements'
interface CustomerOption { customerNo: string; customerName: string; issueState: number }
interface GroupOption { opGroupNo: string; opGroupName: string; psNo: string; issueState: number }
interface ElementOption {
  elementNo: string; elementName: string; elementType: number; dataType: number; inputType: number
  transferType: number; valueType: number; validList: string; description: string; controlled: number
  defaultValue: string; elementSequence: number; allowNull: number; validValues: string[]; issueState: number
  elementTypeText?: string; dataTypeText?: string; transferTypeText?: string; valueTypeText?: string
  controlledText?: string; inputTypeText?: string; allowNullText?: string
}
interface AttributeRow {
  customerNo: string; opGroupNo: string; elementNo: string; elementName: string; defaultValue: string
  dataType: number; allowNull: number; sequence: number; byEquipmentType: boolean; dataStamp: number | null
  creator: string; createDate: string; reviser: string; reviseDate: string; inputType: number
  validValues: string[]; elementType: number; dataFlag: DataFlag
}
interface ApiEnvelope<T> { items?: T[]; rows?: T[]; data?: T[]; message?: string; title?: string; detail?: string }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes.dev.op-group-attrib.v1'

const seedCustomers: CustomerOption[] = [
  { customerNo: '*', customerName: '所有客戶共用', issueState: 2 },
  { customerNo: 'DEV-CUST01', customerName: 'DEV 晶圓測試客戶', issueState: 2 },
  { customerNo: 'DEV-CUST02', customerName: 'DEV 封裝客戶', issueState: 2 },
]
const seedGroups: GroupOption[] = [
  { opGroupNo: 'DEV-ECN-ASSY', opGroupName: 'DEV 組裝 ECN 群組', psNo: 'DEV-ASSY', issueState: 2 },
  { opGroupNo: 'DEV-ECN-QC', opGroupName: 'DEV 品檢 ECN 群組', psNo: 'DEV-QC', issueState: 2 },
  { opGroupNo: 'DEV-ECN-PACK', opGroupName: 'DEV 包裝 ECN 群組', psNo: 'DEV-PACK', issueState: 2 },
]
const seedElements: ElementOption[] = [
  { elementNo: 'DEV-PROD-001', elementName: 'DEV 產品版本', elementType: 5, dataType: 1, transferType: 0, valueType: 0, validList: '', description: 'DEV Key In 產品版本', controlled: 0, inputType: 0, defaultValue: '', elementSequence: 10, allowNull: 0, validValues: [], issueState: 2 },
  { elementNo: 'DEV-PROD-002', elementName: 'DEV 檢查結果', elementType: 5, dataType: 1, transferType: 0, valueType: 1, validList: 'PASS,FAIL,HOLD', description: 'DEV 選單檢查結果', controlled: 1, inputType: 1, defaultValue: 'PASS', elementSequence: 20, allowNull: 0, validValues: ['PASS', 'FAIL', 'HOLD'], issueState: 2 },
  { elementNo: 'DEV-PROD-003', elementName: 'DEV 溫度設定', elementType: 5, dataType: 0, transferType: 0, valueType: 2, validList: '', description: 'DEV 數值溫度', controlled: 2, inputType: 0, defaultValue: '25', elementSequence: 30, allowNull: 1, validValues: [], issueState: 2 },
  { elementNo: 'DEV-PROD-004', elementName: 'DEV 批次類別', elementType: 5, dataType: 1, transferType: 0, valueType: 1, validList: 'NORMAL,REWORK', description: 'DEV 批次類別選單', controlled: 0, inputType: 1, defaultValue: 'NORMAL', elementSequence: 40, allowNull: 1, validValues: ['NORMAL', 'REWORK'], issueState: 2 },
  { elementNo: 'DEV-COMMON-001', elementName: 'DEV 共用特性（不可加入）', elementType: 0, dataType: 1, transferType: 0, valueType: 0, validList: '', description: 'DEV 共用特性', controlled: 0, inputType: 0, defaultValue: '', elementSequence: 5, allowNull: 1, validValues: [], issueState: 2 },
]
const seedRows: AttributeRow[] = [
  { customerNo: '*', opGroupNo: 'DEV-ECN-ASSY', elementNo: 'DEV-PROD-001', elementName: 'DEV 產品版本', defaultValue: 'A01', dataType: 1, allowNull: 0, sequence: 10, byEquipmentType: false, dataStamp: 2, creator: 'DEV001', createDate: '2026-11-01T08:30:00', reviser: 'DEV002', reviseDate: '2026-11-05T09:15:00', inputType: 0, validValues: [], elementType: 5, dataFlag: -1 },
  { customerNo: '*', opGroupNo: 'DEV-ECN-ASSY', elementNo: 'DEV-PROD-002', elementName: 'DEV 檢查結果', defaultValue: 'PASS', dataType: 1, allowNull: 0, sequence: 20, byEquipmentType: true, dataStamp: 1, creator: 'DEV001', createDate: '2026-11-01T08:30:00', reviser: '', reviseDate: '', inputType: 1, validValues: ['PASS', 'FAIL', 'HOLD'], elementType: 5, dataFlag: -1 },
  { customerNo: 'DEV-CUST01', opGroupNo: 'DEV-ECN-QC', elementNo: 'DEV-PROD-003', elementName: 'DEV 溫度設定', defaultValue: '25', dataType: 0, allowNull: 1, sequence: 30, byEquipmentType: false, dataStamp: 0, creator: 'DEV001', createDate: '2026-11-02T10:00:00', reviser: '', reviseDate: '', inputType: 0, validValues: [], elementType: 5, dataFlag: -1 },
]

const rows = ref<AttributeRow[]>([])
const customers = ref<CustomerOption[]>([])
const groups = ref<GroupOption[]>([])
const elements = ref<ElementOption[]>([])
const filterCustomerNo = ref('')
const filterGroupNo = ref('')
const filterElementNo = ref('')
const filterCustomerEnabled = ref(false)
const filterGroupEnabled = ref(false)
const filterElementEnabled = ref(false)
const selectedKey = ref('')
const selectedKeys = ref<string[]>([])
const editMode = ref<EditMode>('')
const editorDraft = ref({ customerNo: '*', opGroupNo: '' })
const editorAudit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const editorRows = ref<AttributeRow[]>([])
const editorSelectedElements = ref<string[]>([])
const editorSourceKey = ref('')
const pickerOpen = ref(false)
const pickerKind = ref<PickerKind>('customer')
const pickerKeyword = ref('')
const pickerSelection = ref<string[]>([])
const loading = ref(false)
const saving = ref(false)
const busy = ref(false)
const abortLogRequired = ref(false)
const abortLogSettingReady = ref(false)
const editorLoading = ref(false)
const error = ref('')
const notice = ref('')

function rowKey(row: Pick<AttributeRow, 'customerNo' | 'opGroupNo' | 'elementNo'>): string {
  return [row.customerNo, row.opGroupNo, row.elementNo].join('\u001f')
}
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function nowText(): string { return new Date().toISOString() }
function userNo(): string { return props.currentUserNo || (mock.value ? 'DEV001' : '') }
function displayDate(value: string): string {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return new Intl.DateTimeFormat('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23' }).format(date).replaceAll('-', '/')
}
function dataTypeName(value: number): string { return ({ 0: 'Numeric', 1: 'String', 2: 'Percent', 3: 'DateTime', 4: 'Boolean' } as Record<number, string>)[value] ?? String(value) }
function allowNullName(value: number): string { return value === 0 ? 'Not Null' : 'Null' }
function elementTypeName(value: number): string { return ({ 0: 'Common', 1: 'Equipment', 2: 'Material', 3: 'OP', 4: 'Process', 5: 'Product', 6: 'OP Attrib', 7: 'QCItem Parameter', 8: 'Inventory', 9: 'Recipe', 10: 'ENG' } as Record<number, string>)[value] ?? String(value) }
function valueTypeName(value: number): string { return ({ 0: 'Not Limit', 1: 'Assign Valid Value', 2: 'Min/Max', 3: 'Reference MES Parameter' } as Record<number, string>)[value] ?? String(value) }
function controlledName(value: number): string { return ({ 0: 'Not Control', 1: 'Notice', 2: 'Check' } as Record<number, string>)[value] ?? String(value) }
function inputTypeName(value: number): string { return value === 0 ? 'Key In' : value === 1 ? 'Select List' : String(value) }
function parseOptionList(value: unknown): string[] {
  if (Array.isArray(value)) return value.map(String)
  if (typeof value !== 'string' || !value.trim()) return []
  return value.split(/[\r\n,;]/).map((item) => item.trim()).filter(Boolean)
}
function normalizeRow(value: Record<string, unknown>): AttributeRow {
  return {
    customerNo: String(value.customerNo ?? value.customerno ?? ''), opGroupNo: String(value.opGroupNo ?? value.opgroupno ?? ''),
    elementNo: String(value.elementNo ?? value.elementno ?? ''), elementName: String(value.elementName ?? value.elementname ?? ''),
    defaultValue: String(value.defaultValue ?? value.defaultvalue ?? ''), dataType: Number(value.dataType ?? value.datatype ?? 0),
    allowNull: Number(value.allowNull ?? value.allownull ?? 0), sequence: Number(value.sequence ?? 0),
    byEquipmentType: (() => { const flag = value.byEquipmentType ?? value.bySecondClass ?? value.bysecondclass ?? value.byequipment_type ?? false; return flag === true || Number(flag) === 1 })(),
    dataStamp: value.dataStamp == null && value.datastamp == null ? null : Number(value.dataStamp ?? value.datastamp),
    creator: String(value.creator ?? ''), createDate: String(value.createDate ?? value.createdate ?? ''),
    reviser: String(value.reviser ?? ''), reviseDate: String(value.reviseDate ?? value.revisedate ?? ''),
    inputType: Number(value.inputType ?? value.inputtype ?? 0), validValues: parseOptionList(value.validValues ?? value.validvalues),
    elementType: Number(value.elementType ?? value.elementtype ?? 5), dataFlag: -1,
  }
}
function listFrom<T>(result: T[] | ApiEnvelope<T>): T[] { return Array.isArray(result) ? result : result.items ?? result.rows ?? result.data ?? [] }
function apiPath(path: string): string { return props.apiBaseUrl.replace(/\/+$/, '') + path }
async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  const response = await fetch(apiPath(path), { credentials: 'include', ...init, headers })
  if (response.status === 401) { window.location.assign(import.meta.env.BASE_URL + 'login'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) {
    const body = await response.json().catch(() => null) as ApiEnvelope<unknown> | null
    if (response.status === 404) throw new Error('作業站參數群組 API 尚未建立，請依 todolist_homepc.md 完成資料表與 API。')
    throw new Error(body?.detail || body?.message || body?.title || 'API 回應錯誤 (' + response.status + ')。')
  }
  return response.status === 204 ? undefined as T : await response.json() as T
}
function persistMock(): void { localStorage.setItem(storageKey, JSON.stringify(rows.value)) }
function loadMock(): void {
  const saved = localStorage.getItem(storageKey)
  if (!saved) { rows.value = clone(seedRows); persistMock(); return }
  try { rows.value = (JSON.parse(saved) as unknown[]).map((row) => normalizeRow(row as Record<string, unknown>)) }
  catch { rows.value = clone(seedRows); persistMock() }
}
const visibleRows = computed(() => rows.value.filter((row) =>
  (!filterCustomerEnabled.value || !filterCustomerNo.value.trim() || row.customerNo.toUpperCase() === filterCustomerNo.value.trim().toUpperCase())
  && (!filterGroupEnabled.value || !filterGroupNo.value.trim() || row.opGroupNo.toUpperCase() === filterGroupNo.value.trim().toUpperCase())
  && (!filterElementEnabled.value || !filterElementNo.value.trim() || row.elementNo.toUpperCase() === filterElementNo.value.trim().toUpperCase()))
  .sort((a, b) => a.customerNo.localeCompare(b.customerNo) || a.opGroupNo.localeCompare(b.opGroupNo) || a.elementNo.localeCompare(b.elementNo)))
const selectedRow = computed(() => rows.value.find((row) => rowKey(row) === selectedKey.value) ?? null)
const editorVisibleRows = computed(() => editorRows.value.filter((row) => row.dataFlag !== 2).sort((a, b) => a.elementNo.localeCompare(b.elementNo)))
const pickerRows = computed(() => {
  const search = pickerKeyword.value.trim().toUpperCase()
  if (pickerKind.value === 'customer' || pickerKind.value === 'editorCustomer') {
    return customers.value.filter((row) => row.issueState === 2 && (!search || row.customerNo.toUpperCase().includes(search) || row.customerName.toUpperCase().includes(search)))
  }
  if (pickerKind.value === 'group' || pickerKind.value === 'editorGroup') {
    return groups.value.filter((row) => row.issueState === 2 && (!search || row.opGroupNo.toUpperCase().includes(search) || row.opGroupName.toUpperCase().includes(search) || row.psNo.toUpperCase().includes(search)))
  }
  const existing = new Set(editorRows.value.map((row) => row.elementNo))
  return elements.value.filter((row) => row.issueState === 2 && row.elementType !== 0 && !existing.has(row.elementNo)
    && (!search || row.elementNo.toUpperCase().includes(search) || row.elementName.toUpperCase().includes(search)))
})
const pickerIsMulti = computed(() => pickerKind.value === 'addElements')
const pickerIsElement = computed(() => pickerKind.value === 'element' || pickerKind.value === 'addElements')
const pickerTitle = computed(() => pickerIsMulti.value ? '選擇產品 Element' : (pickerKind.value === 'customer' || pickerKind.value === 'editorCustomer') ? '選擇客戶' : '選擇 ECN 作業站群組')
function pickerRowKey(row: CustomerOption | GroupOption | ElementOption): string {
  if ('customerNo' in row) return row.customerNo
  if ('opGroupNo' in row) return row.opGroupNo
  return row.elementNo
}
function pickerRowName(row: CustomerOption | GroupOption | ElementOption): string {
  if ('customerName' in row) return row.customerName
  if ('opGroupName' in row) return row.opGroupName
  return row.elementName
}

async function loadRows(): Promise<void> {
  loading.value = true; error.value = ''; notice.value = ''
  try {
    if (mock.value) loadMock()
    else {
      const query = new URLSearchParams()
      if (filterCustomerEnabled.value && filterCustomerNo.value.trim()) query.set('customerNo', filterCustomerNo.value.trim().toUpperCase())
      if (filterGroupEnabled.value && filterGroupNo.value.trim()) query.set('opGroupNo', filterGroupNo.value.trim().toUpperCase())
      if (filterElementEnabled.value && filterElementNo.value.trim()) query.set('elementNo', filterElementNo.value.trim().toUpperCase())
      const result = await api<AttributeRow[] | ApiEnvelope<AttributeRow>>('/api/operation-group-attributes?' + query.toString())
      rows.value = listFrom(result).map((row) => normalizeRow(row as unknown as Record<string, unknown>))
    }
    if (selectedKey.value && !visibleRows.value.some((row) => rowKey(row) === selectedKey.value)) selectedKey.value = ''
    selectedKeys.value = []
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入作業站參數群組失敗。' }
  finally { loading.value = false }
}
async function loadLookups(): Promise<void> {
  if (mock.value) { customers.value = clone(seedCustomers); groups.value = clone(seedGroups); elements.value = clone(seedElements); return }
  const [customerResult, groupResult, elementResult] = await Promise.all([
    api<CustomerOption[] | ApiEnvelope<CustomerOption>>('/api/customers?issueState=2'),
    api<GroupOption[] | ApiEnvelope<GroupOption>>('/api/ecn-operation-groups?issueState=2'),
    api<ElementOption[] | ApiEnvelope<ElementOption>>('/api/system-elements?issueState=2&elementType=5'),
  ])
  customers.value = listFrom(customerResult).map((row) => ({ customerNo: String((row as CustomerOption).customerNo ?? (row as unknown as Record<string, unknown>).customerno ?? ''), customerName: String((row as CustomerOption).customerName ?? (row as unknown as Record<string, unknown>).customername ?? ''), issueState: Number((row as CustomerOption).issueState ?? (row as unknown as Record<string, unknown>).issuestate ?? 0) }))
  if (!customers.value.some((row) => row.customerNo === '*')) customers.value.unshift({ customerNo: '*', customerName: '所有客戶共用', issueState: 2 })
  groups.value = listFrom(groupResult).map((row) => { const value = row as unknown as Record<string, unknown>; return { opGroupNo: String(value.opGroupNo ?? value.opgroupno ?? ''), opGroupName: String(value.opGroupName ?? value.opgroupname ?? ''), psNo: String(value.psNo ?? value.psno ?? ''), issueState: Number(value.issueState ?? value.issuestate ?? 0) } })
  elements.value = listFrom(elementResult).map((row) => { const value = row as unknown as Record<string, unknown>; return { elementNo: String(value.elementNo ?? value.elementno ?? ''), elementName: String(value.elementName ?? value.elementname ?? ''), elementType: Number(value.elementType ?? value.elementtype ?? 5), dataType: Number(value.dataType ?? value.datatype ?? 0), transferType: Number(value.transferType ?? value.transfertype ?? 0), valueType: Number(value.valueType ?? value.valuetype ?? 0), validList: String(value.validList ?? value.validlist ?? ''), description: String(value.description ?? ''), controlled: Number(value.controlled ?? 0), inputType: Number(value.inputType ?? value.inputtype ?? 0), defaultValue: String(value.defaultValue ?? value.defaultvalue ?? ''), elementSequence: Number(value.elementSequence ?? value.elementsequence ?? 0), allowNull: Number(value.allowNull ?? value.allownull ?? 0), validValues: parseOptionList(value.validValues ?? value.validlist), issueState: Number(value.issueState ?? value.issuestate ?? 0), elementTypeText: String(value.elementTypeText ?? value.elementtypetext ?? ''), dataTypeText: String(value.dataTypeText ?? value.datatypetext ?? ''), transferTypeText: String(value.transferTypeText ?? value.transfertypetext ?? ''), valueTypeText: String(value.valueTypeText ?? value.valuetypetext ?? ''), controlledText: String(value.controlledText ?? value.controlledtext ?? ''), inputTypeText: String(value.inputTypeText ?? value.inputtypetext ?? ''), allowNullText: String(value.allowNullText ?? value.allownulltext ?? '') } })
}
async function ensureLookups(): Promise<void> {
  if (customers.value.length && groups.value.length && elements.value.length) return
  try { await loadLookups() } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入查詢選項失敗。' }
}
async function loadElementValues(element: ElementOption): Promise<string[]> {
  if (mock.value) return clone(element.validValues)
  const result = await api<string[] | ApiEnvelope<string>>('/api/system-elements/' + encodeURIComponent(element.elementNo) + '/values')
  return listFrom(result).map((item) => typeof item === 'string' ? item : String((item as unknown as Record<string, unknown>).value ?? (item as unknown as Record<string, unknown>).elementValue ?? ''))
}
async function loadAbortLogSetting(): Promise<void> {
  if (mock.value) { abortLogRequired.value = false; abortLogSettingReady.value = true; return }
  const result = await api<unknown>('/api/system/parameters/InputAbortLog')
  const value = result && typeof result === 'object'
    ? (result as Record<string, unknown>).value ?? (result as Record<string, unknown>).parameterValue
    : result
  if (value === true || value === 1 || String(value).trim() === '1') abortLogRequired.value = true
  else if (value === false || value === 0 || String(value).trim() === '0') abortLogRequired.value = false
  else throw new Error('InputAbortLog 系統參數回應格式錯誤。')
  abortLogSettingReady.value = true
}
function elementOptionFromRow(row: AttributeRow): ElementOption {
  return { elementNo: row.elementNo, elementName: row.elementName, elementType: row.elementType, dataType: row.dataType, transferType: 0, valueType: 0, validList: '', description: '', controlled: 0, inputType: row.inputType, defaultValue: row.defaultValue, elementSequence: row.sequence, allowNull: row.allowNull, validValues: row.validValues, issueState: 2 }
}
function setFilterCheck(kind: 'customer' | 'group' | 'element'): void {
  if (kind === 'customer') filterCustomerEnabled.value = true
  if (kind === 'group') filterGroupEnabled.value = true
  if (kind === 'element') filterElementEnabled.value = true
}
function keyFilterEnter(kind: 'customer' | 'group' | 'element'): void { setFilterCheck(kind); void loadRows() }
async function openPicker(kind: PickerKind): Promise<void> {
  error.value = ''; pickerKind.value = kind; pickerKeyword.value = ''; pickerSelection.value = []; pickerOpen.value = true
  await ensureLookups()
}
function togglePickerValue(value: string, checked: boolean): void {
  pickerSelection.value = checked ? [...new Set([...pickerSelection.value, value])] : pickerSelection.value.filter((item) => item !== value)
}
async function commitPicker(): Promise<void> {
  const first = pickerSelection.value[0]
  if (pickerKind.value === 'customer' && first) { filterCustomerNo.value = first; filterCustomerEnabled.value = true }
  else if (pickerKind.value === 'group' && first) { filterGroupNo.value = first; filterGroupEnabled.value = true }
  else if (pickerKind.value === 'element' && first) { filterElementNo.value = first; filterElementEnabled.value = true }
  else if (pickerKind.value === 'editorCustomer' && first) editorDraft.value.customerNo = first
  else if (pickerKind.value === 'editorGroup' && first) editorDraft.value.opGroupNo = first
  else if (pickerKind.value === 'addElements' && pickerSelection.value.length) await addElements(pickerSelection.value)
  pickerOpen.value = false
}
async function addElements(elementNos: string[]): Promise<void> {
  for (const elementNo of elementNos) {
    const element = elements.value.find((item) => item.elementNo === elementNo)
    if (!element || editorRows.value.some((row) => row.elementNo === elementNo && row.dataFlag !== 2)) continue
    let validValues: string[] = []
    try { if (element.inputType === 1) validValues = await loadElementValues(element) }
    catch (cause) { error.value = cause instanceof Error ? cause.message : '載入 Element 有效值失敗。'; return }
    editorRows.value.push({
      customerNo: editorDraft.value.customerNo || '*', opGroupNo: editorDraft.value.opGroupNo, elementNo: element.elementNo,
      elementName: element.elementName, defaultValue: element.defaultValue, dataType: element.dataType, allowNull: element.allowNull,
      sequence: element.elementSequence, byEquipmentType: false, dataStamp: 0, creator: userNo(), createDate: nowText(),
      reviser: '', reviseDate: '', inputType: element.inputType, validValues, elementType: element.elementType, dataFlag: 0,
    })
  }
  pickerSelection.value = []
}
async function openEditor(mode: Exclude<EditMode, ''>): Promise<void> {
  error.value = ''; notice.value = ''
  if ((mode === 'edit' || mode === 'copy') && !selectedRow.value) return
  const selected = selectedRow.value
  if (mode === 'edit' && selected) {
    editorDraft.value = { customerNo: selected.customerNo, opGroupNo: selected.opGroupNo }
    editorSourceKey.value = selected.customerNo + '\u001f' + selected.opGroupNo
    editorAudit.value = { creator: selected.creator, createDate: selected.createDate, reviser: userNo(), reviseDate: nowText() }
  } else if (mode === 'copy' && selected) {
    editorDraft.value = { customerNo: selected.customerNo, opGroupNo: selected.opGroupNo }
    editorSourceKey.value = selected.customerNo + '\u001f' + selected.opGroupNo
    editorAudit.value = { creator: userNo(), createDate: nowText(), reviser: '', reviseDate: '' }
  } else {
    editorDraft.value = { customerNo: '*', opGroupNo: '' }; editorSourceKey.value = ''; editorRows.value = []
    editorAudit.value = { creator: userNo(), createDate: nowText(), reviser: '', reviseDate: '' }
  }
  editorSelectedElements.value = []
  editMode.value = mode
  editorLoading.value = true
  try {
    if (mode !== 'add' && selected) {
      if (mock.value) editorRows.value = clone(rows.value.filter((row) => row.customerNo === selected.customerNo && row.opGroupNo === selected.opGroupNo).map((row) => ({ ...row, dataFlag: -1 as DataFlag })))
      else {
        const query = new URLSearchParams({ customerNo: selected.customerNo, opGroupNo: selected.opGroupNo })
        const result = await api<AttributeRow[] | ApiEnvelope<AttributeRow>>('/api/operation-group-attributes/editor?' + query.toString())
        editorRows.value = listFrom(result).map((row) => ({ ...normalizeRow(row as unknown as Record<string, unknown>), dataFlag: -1 as DataFlag }))
      }
      for (const row of editorRows.value) {
        if (row.inputType !== 1) continue
        const option = elements.value.find((item) => item.elementNo === row.elementNo) ?? elementOptionFromRow(row)
        row.validValues = await loadElementValues(option)
      }
    }
    await ensureLookups()
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入參數明細失敗。' }
  finally { editorLoading.value = false }
}
function markChanged(row: AttributeRow): void { if (row.dataFlag === -1) row.dataFlag = 1 }
function removeEditorRows(): void {
  const selected = editorSelectedElements.value.length ? editorSelectedElements.value : []
  if (!selected.length) { error.value = '請先選取要移除的 Element。'; return }
  editorRows.value = editorRows.value.flatMap((row) => {
    if (!selected.includes(row.elementNo)) return [row]
    if (row.dataFlag === 0) return []
    return [{ ...row, dataFlag: 2 as DataFlag }]
  })
  editorSelectedElements.value = []; error.value = ''
}
function validateEditor(): string | null {
  const customerNo = editorDraft.value.customerNo.trim().toUpperCase()
  const opGroupNo = editorDraft.value.opGroupNo.trim().toUpperCase()
  editorDraft.value.customerNo = customerNo; editorDraft.value.opGroupNo = opGroupNo
  if (!customerNo) return 'CustomerNo 必須選擇；共用參數請使用 *。'
  if (customerNo.length > 20) return 'CustomerNo 最多 20 個字元。'
  if (!opGroupNo) return '請先選擇 ECN 作業站群組。'
  if (opGroupNo.length > 20) return 'OPGroupNo 最多 20 個字元。'
  if (editorRows.value.length === 0) return '請至少保留一筆 Element。'
  const duplicate = new Set<string>()
  for (const row of editorVisibleRows.value) {
    if (duplicate.has(row.elementNo)) return 'Element 不可重複：' + row.elementNo
    duplicate.add(row.elementNo)
    if (String(row.sequence ?? '').trim() === '' || !Number.isInteger(Number(row.sequence)) || Number(row.sequence) < 0 || Number(row.sequence) > 99) return row.elementNo + ' 的 Sequence 須為 0 至 99 的整數。'
    if (String(row.defaultValue ?? '').length > 255) return row.elementNo + ' 的 DefaultValue 最多 255 個字元。'
    if (row.inputType === 1 && row.defaultValue && row.validValues.length && !row.validValues.includes(row.defaultValue)) return row.elementNo + ' 的 DefaultValue 不在 Element 有效值清單內。'
  }
  return null
}
async function validateElementDefaults(): Promise<string | null> {
  for (const row of editorVisibleRows.value) {
    if (row.dataFlag === -1 || row.inputType !== 0 || !row.defaultValue) continue
    if (mock.value) {
      if (row.validValues.length && !row.validValues.includes(row.defaultValue)) return row.elementNo + ' 的 DefaultValue 不符合 Element 有效值。'
    } else {
      await api('/api/system-elements/' + encodeURIComponent(row.elementNo) + '/validate-value', { method: 'POST', body: JSON.stringify({ value: row.defaultValue }) })
    }
  }
  return null
}
function changedEntry(row: AttributeRow, customerNo: string, opGroupNo: string, flag: DataFlag): AttributeRow {
  return { ...row, customerNo, opGroupNo, sequence: Number(row.sequence), dataFlag: flag }
}
async function saveEditor(closeAfter: boolean): Promise<void> {
  const validation = validateEditor()
  if (validation) { error.value = validation; return }
  try {
    const valueError = await validateElementDefaults()
    if (valueError) { error.value = valueError; return }
  } catch (cause) { error.value = cause instanceof Error ? cause.message : 'Element 預設值驗證失敗。'; return }
  const customerNo = editorDraft.value.customerNo
  const opGroupNo = editorDraft.value.opGroupNo
  const targetKey = customerNo + '\u001f' + opGroupNo
  const copyToDifferentKey = editMode.value === 'copy' && targetKey !== editorSourceKey.value
  const entries = editorRows.value.map((row) => changedEntry(row, customerNo, opGroupNo, copyToDifferentKey ? (row.dataFlag === 2 ? 2 : 0) : row.dataFlag))
  saving.value = true; error.value = ''; notice.value = ''
  try {
    if (mock.value) {
      const now = nowText()
      const currentTarget = new Map(rows.value.filter((row) => row.customerNo === customerNo && row.opGroupNo === opGroupNo).map((row) => [rowKey(row), row]))
      const changes = new Map(rows.value.map((row) => [rowKey(row), row]))
      for (const entry of entries) {
        const key = rowKey(entry)
        if (entry.dataFlag === 2) { if (!copyToDifferentKey) changes.delete(key); continue }
        if (entry.dataFlag === 0 || (entry.dataFlag === -1 && !currentTarget.has(key))) {
          if (currentTarget.has(key)) continue
          changes.set(key, { ...entry, dataFlag: -1, dataStamp: 0, creator: userNo(), createDate: now, reviser: '', reviseDate: '' })
        } else if (entry.dataFlag === 1) {
          const existing = currentTarget.get(key)
          if (!existing) throw new Error('要修改的資料已不存在，請重新查詢。')
          if ((entry.dataStamp ?? 0) !== (existing.dataStamp ?? 0)) throw new Error('資料已被其他使用者修改，請重新載入後再操作。')
          changes.set(key, { ...entry, dataFlag: -1, dataStamp: (existing.dataStamp ?? 0) + 1, reviser: userNo(), reviseDate: now })
        }
      }
      rows.value = [...changes.values()].sort((a, b) => a.customerNo.localeCompare(b.customerNo) || a.opGroupNo.localeCompare(b.opGroupNo) || a.elementNo.localeCompare(b.elementNo))
      persistMock()
    } else {
      await api('/api/operation-group-attributes/batch-save', { method: 'POST', body: JSON.stringify({ customerNo, opGroupNo, entries: entries.map((row) => ({ elementNo: row.elementNo, elementName: row.elementName, defaultValue: row.defaultValue, dataType: row.dataType, allowNull: row.allowNull, sequence: row.sequence, bySecondClass: row.byEquipmentType ? 1 : 0, dataStamp: row.dataStamp, changeType: row.dataFlag })) }) })
    }
    selectedKey.value = rowKey({ customerNo, opGroupNo, elementNo: editorVisibleRows.value[0]?.elementNo ?? '' })
    await loadRows()
    notice.value = '作業站參數群組資料已儲存。'
    if (closeAfter) editMode.value = ''
    else {
      editorSourceKey.value = targetKey
      if (mock.value) editorRows.value = clone(rows.value.filter((row) => row.customerNo === customerNo && row.opGroupNo === opGroupNo).map((row) => ({ ...row, dataFlag: -1 as DataFlag })))
      else {
        const query = new URLSearchParams({ customerNo, opGroupNo })
        const result = await api<AttributeRow[] | ApiEnvelope<AttributeRow>>('/api/operation-group-attributes/editor?' + query.toString())
        editorRows.value = listFrom(result).map((row) => ({ ...normalizeRow(row as unknown as Record<string, unknown>), dataFlag: -1 as DataFlag }))
        for (const row of editorRows.value) {
          if (row.inputType === 1) {
            const option = elements.value.find((item) => item.elementNo === row.elementNo) ?? elementOptionFromRow(row)
            row.validValues = await loadElementValues(option)
          }
        }
      }
      if (editMode.value === 'add' || copyToDifferentKey) editMode.value = 'edit'
    }
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '儲存作業站參數群組失敗。' }
  finally { saving.value = false }
}
async function deleteRows(): Promise<void> {
  if (!abortLogSettingReady.value) {
    try { await loadAbortLogSetting() }
    catch (cause) { error.value = '無法確認 InputAbortLog 設定，已停止刪除：' + (cause instanceof Error ? cause.message : '系統參數讀取失敗。'); return }
  }
  const targets = selectedKeys.value.length ? [...selectedKeys.value] : selectedKey.value ? [selectedKey.value] : []
  if (!targets.length || !window.confirm('確定刪除選取的作業站參數群組資料？')) return
  let abortLog = ''
  if (abortLogRequired.value) {
    const value = window.prompt('請輸入作廢原因：')
    if (!value?.trim()) return
    abortLog = value.trim()
  }
  busy.value = true; error.value = ''; notice.value = ''
  try {
    if (mock.value) {
      const deleted = new Set(targets)
      rows.value = rows.value.filter((row) => !deleted.has(rowKey(row))); persistMock()
    } else {
      for (const key of targets) {
        const row = rows.value.find((item) => rowKey(item) === key)
        if (!row) continue
        const query = new URLSearchParams({ customerNo: row.customerNo, opGroupNo: row.opGroupNo, elementNo: row.elementNo })
        if (abortLog) query.set('abortLog', abortLog)
        await api('/api/operation-group-attributes?' + query.toString(), { method: 'DELETE' })
      }
    }
    selectedKeys.value = []; selectedKey.value = ''; await loadRows(); notice.value = '選取資料已刪除。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '刪除作業站參數群組失敗。' }
  finally { busy.value = false }
}
function selectMainRow(row: AttributeRow): void { selectedKey.value = rowKey(row) }
function toggleMainSelection(row: AttributeRow, checked: boolean): void {
  selectedKeys.value = checked ? [...new Set([...selectedKeys.value, rowKey(row)])] : selectedKeys.value.filter((key) => key !== rowKey(row))
}
function selectAllMainRows(checked: boolean): void { selectedKeys.value = checked ? visibleRows.value.map(rowKey) : [] }
function displayPickerName(kind: PickerKind, value: string): string {
  if (kind === 'customer' || kind === 'editorCustomer') return customers.value.find((row) => row.customerNo === value)?.customerName ?? ''
  return groups.value.find((row) => row.opGroupNo === value)?.opGroupName ?? ''
}
onMounted(() => {
  void loadRows()
  void ensureLookups()
  void loadAbortLogSetting().catch((cause) => { error.value = cause instanceof Error ? cause.message : '讀取 InputAbortLog 設定失敗。' })
})
</script>

<template>
  <div class="op-group-attrib-page">
    <div class="query-strip">
      <label class="query-item"><input v-model="filterCustomerEnabled" type="checkbox"><span>CustomerNo</span><input v-model="filterCustomerNo" class="kd-input query-input" maxlength="20" @blur="filterCustomerNo = filterCustomerNo.trim().toUpperCase()" @keydown.enter="keyFilterEnter('customer')"><button class="kd-button lookup-button" type="button" @click="openPicker('customer')">...</button></label>
      <label class="query-item"><input v-model="filterGroupEnabled" type="checkbox"><span>OPGroupNo</span><input v-model="filterGroupNo" class="kd-input query-input" maxlength="20" @blur="filterGroupNo = filterGroupNo.trim().toUpperCase()" @keydown.enter="keyFilterEnter('group')"><button class="kd-button lookup-button" type="button" @click="openPicker('group')">...</button></label>
      <label class="query-item"><input v-model="filterElementEnabled" type="checkbox"><span>ElementNo</span><input v-model="filterElementNo" class="kd-input query-input" maxlength="20" @blur="filterElementNo = filterElementNo.trim().toUpperCase()" @keydown.enter="keyFilterEnter('element')"><button class="kd-button lookup-button" type="button" @click="openPicker('element')">...</button></label>
      <button class="kd-button" type="button" :disabled="loading" @click="loadRows">查詢</button>
    </div>
    <p v-if="error && !editMode && !pickerOpen" class="message error-message" role="alert">{{ error }}</p>
    <p v-else-if="notice && !editMode" class="message notice-message" role="status">{{ notice }}</p>
    <fieldset class="win-group result-panel"><legend>OP Group Attribute</legend>
      <div class="panel-layout">
        <div class="grid-scroll"><table class="kd-grid">
          <thead><tr>
            <th class="check-cell"><input type="checkbox" aria-label="全選參數" :checked="visibleRows.length > 0 && visibleRows.every((row) => selectedKeys.includes(rowKey(row)))" @change="selectAllMainRows(($event.target as HTMLInputElement).checked)"></th>
            <th>CustomerNo</th><th>OPGroupNo</th><th>ElementNo</th><th>ElementName</th><th>DefaultValue</th><th>DataType</th><th>AllowNull</th><th>Sequence</th><th>BYEQUIPMENT_TYPE</th><th>Reviser</th><th>ReviseDate</th><th>Creator</th><th>CreateDate</th>
          </tr></thead><tbody>
            <tr v-for="row in visibleRows" :key="rowKey(row)" :class="{ active: selectedKey === rowKey(row) }" @click="selectMainRow(row)" @dblclick="openEditor('edit')">
              <td class="check-cell" @click.stop><input type="checkbox" :aria-label="'選取 ' + row.elementNo" :checked="selectedKeys.includes(rowKey(row))" @change="toggleMainSelection(row, ($event.target as HTMLInputElement).checked)"></td>
              <td>{{ row.customerNo }}</td><td>{{ row.opGroupNo }}</td><td>{{ row.elementNo }}</td><td>{{ row.elementName }}</td><td>{{ row.defaultValue }}</td><td>{{ dataTypeName(row.dataType) }}</td><td>{{ allowNullName(row.allowNull) }}</td><td class="number-cell">{{ row.sequence }}</td><td class="center-cell">{{ row.byEquipmentType ? '是' : '否' }}</td><td>{{ row.reviser }}</td><td>{{ displayDate(row.reviseDate) }}</td><td>{{ row.creator }}</td><td>{{ displayDate(row.createDate) }}</td>
            </tr>
            <tr v-if="!loading && visibleRows.length === 0"><td colspan="14" class="empty-row">沒有符合條件的作業站參數群組</td></tr>
          </tbody>
        </table></div>
        <aside class="vertical-actions">
          <button class="kd-button primary" type="button" :disabled="busy || loading" @click="openEditor('add')">新增 (A)</button>
          <button class="kd-button" type="button" :disabled="!selectedRow || busy" @click="openEditor('edit')">修改 (E)</button>
          <button class="kd-button" type="button" :disabled="(!selectedRow && !selectedKeys.length) || busy" @click="deleteRows">刪除 (D)</button>
          <button class="kd-button" type="button" :disabled="!selectedRow || busy" @click="openEditor('copy')">複製 (C)</button>
          <button class="kd-button" type="button" @click="emit('close')">關閉</button>
        </aside>
      </div>
      <div class="grid-status">{{ loading ? '載入中…' : '資料總筆數 ' + visibleRows.length }}<span v-if="selectedRow">　選取：{{ selectedRow.customerNo }} / {{ selectedRow.opGroupNo }} / {{ selectedRow.elementNo }}</span></div>
    </fieldset>

    <div v-if="editMode" class="dialog-backdrop" @click.self="editMode = ''">
      <section class="win-dialog editor-dialog" role="dialog" aria-modal="true" :aria-label="editMode === 'edit' ? '修改作業站參數群組' : editMode === 'copy' ? '複製作業站參數群組' : '新增作業站參數群組'">
        <header class="dialog-title">OP Group Attribute Define ({{ editMode.toUpperCase() }})</header>
        <div class="editor-identifiers">
          <label>OPGroupNo<div class="lookup-control"><input class="kd-input" :value="editorDraft.opGroupNo" readonly><button class="kd-button lookup-button" type="button" :disabled="editMode === 'edit'" @click="openPicker('editorGroup')">...</button></div></label>
          <label>CustomerNo<div class="lookup-control"><input class="kd-input" :value="editorDraft.customerNo" readonly><button class="kd-button lookup-button" type="button" :disabled="editMode === 'edit'" @click="openPicker('editorCustomer')">...</button></div></label>
          <span class="customer-name">{{ displayPickerName('editorCustomer', editorDraft.customerNo) }}</span>
          <span class="group-name">{{ displayPickerName('editorGroup', editorDraft.opGroupNo) }}</span>
        </div>
        <fieldset class="form-group"><legend>OP Group Attribute Define</legend>
          <div class="editor-grid-layout">
            <div class="grid-scroll editor-grid-scroll"><table class="kd-grid editor-grid">
              <thead><tr><th class="check-cell"><input type="checkbox" aria-label="全選 Element" :checked="editorVisibleRows.length > 0 && editorVisibleRows.every((row) => editorSelectedElements.includes(row.elementNo))" @change="editorSelectedElements = ($event.target as HTMLInputElement).checked ? editorVisibleRows.map((row) => row.elementNo) : []"></th><th>CustomerNo</th><th>ElementNo</th><th>ElementName</th><th>DefaultValue</th><th>DataType</th><th>AllowNull</th><th>Sequence</th><th>BYEQUIPMENT_TYPE</th></tr></thead>
              <tbody>
                <tr v-for="row in editorVisibleRows" :key="row.elementNo" :class="{ active: editorSelectedElements.includes(row.elementNo) }" @click="togglePickerValue(row.elementNo, !editorSelectedElements.includes(row.elementNo))">
                  <td class="check-cell" @click.stop><input type="checkbox" :checked="editorSelectedElements.includes(row.elementNo)" @change="togglePickerValue(row.elementNo, ($event.target as HTMLInputElement).checked)"></td>
                  <td>{{ editorDraft.customerNo }}</td><td>{{ row.elementNo }}</td><td>{{ row.elementName }}</td>
                  <td><select v-if="row.inputType === 1" v-model="row.defaultValue" class="kd-input cell-input" @change="markChanged(row)"><option value=""></option><option v-for="value in row.validValues" :key="value" :value="value">{{ value }}</option></select><input v-else v-model="row.defaultValue" class="kd-input cell-input" maxlength="255" @input="markChanged(row)"></td>
                  <td>{{ dataTypeName(row.dataType) }}</td><td><select v-model.number="row.allowNull" class="kd-input cell-input allow-input" @change="markChanged(row)"><option :value="0">Not Null</option><option :value="1">Null</option></select></td>
                  <td><input v-model.number="row.sequence" class="kd-input cell-input sequence-input" type="number" min="0" max="99" step="1" @input="markChanged(row)"></td>
                  <td class="center-cell"><input v-model="row.byEquipmentType" type="checkbox" @change="markChanged(row)"></td>
                </tr>
                <tr v-if="editorVisibleRows.length === 0"><td colspan="9" class="empty-row">請加入一個以上的產品 Element</td></tr>
              </tbody>
            </table></div>
            <aside class="vertical-actions"><button class="kd-button primary" type="button" :disabled="editorLoading || !editorDraft.opGroupNo" @click="openPicker('addElements')">新增 (A)</button><button class="kd-button" type="button" :disabled="editorLoading || !editorSelectedElements.length" @click="removeEditorRows">刪除 (D)</button></aside>
          </div>
          <div class="grid-status">資料總筆數 {{ editorVisibleRows.length }}　InputType=Key In 會依 Element 有效值檢查；Select List 使用有效值下拉選單。</div>
        </fieldset>
        <div class="audit-row"><label>建立人<input class="audit-input" :value="editorAudit.creator" readonly></label><label>建立日期<input class="audit-input" :value="displayDate(editorAudit.createDate)" readonly></label><label>修改人<input class="audit-input" :value="editorAudit.reviser" readonly></label><label>修改日期<input class="audit-input" :value="displayDate(editorAudit.reviseDate)" readonly></label></div>
        <p v-if="error" class="dialog-error" role="alert">{{ error }}</p>
        <footer class="dialog-actions"><button class="kd-button primary" type="button" :disabled="saving || editorLoading" @click="saveEditor(true)">確定</button><button class="kd-button" type="button" :disabled="saving" @click="editMode = ''">關閉</button><button class="kd-button" type="button" :disabled="saving || editorLoading" @click="saveEditor(false)">套用</button></footer>
      </section>
    </div>

    <div v-if="pickerOpen" class="dialog-backdrop picker-layer" @click.self="pickerOpen = false">
      <section class="win-dialog picker-dialog" :class="{ 'element-picker': pickerIsElement }" role="dialog" aria-modal="true" aria-label="選擇資料">
        <header class="dialog-title">{{ pickerTitle }}</header>
        <div class="picker-search"><input v-model="pickerKeyword" class="kd-input" placeholder="輸入代碼或名稱篩選"><template v-if="pickerIsMulti"><button class="kd-button" type="button" @click="pickerSelection = pickerRows.map(pickerRowKey)">全選</button><button class="kd-button" type="button" @click="pickerSelection = []">全不選</button></template></div>
        <div class="picker-scroll"><table class="kd-grid"><thead><tr><th v-if="pickerIsMulti" class="check-cell"></th><th>ElementNo</th><th>ElementName</th><template v-if="pickerIsElement"><th>Element 類別</th><th>資料型態</th><th>傳送型態</th><th>值型態</th><th>有效值清單</th><th>預設值</th><th>說明</th><th>管控方式</th><th>輸入方式</th><th>允許空值</th></template><th v-else-if="pickerRows.some((row) => 'psNo' in row)">PSNo</th></tr></thead><tbody>
          <tr v-for="row in pickerRows" :key="pickerRowKey(row)" @click="pickerIsMulti ? togglePickerValue(pickerRowKey(row), !pickerSelection.includes(pickerRowKey(row))) : (pickerSelection = [pickerRowKey(row)])">
            <td v-if="pickerIsMulti" class="check-cell"><input type="checkbox" :checked="pickerSelection.includes(pickerRowKey(row))" @click.stop @change="togglePickerValue(pickerRowKey(row), ($event.target as HTMLInputElement).checked)"></td>
            <td>{{ pickerRowKey(row) }}</td><td>{{ pickerRowName(row) }}</td><template v-if="pickerIsElement && 'elementType' in row"><td>{{ row.elementTypeText || elementTypeName(row.elementType) }}</td><td>{{ row.dataTypeText || dataTypeName(row.dataType) }}</td><td>{{ row.transferTypeText || row.transferType }}</td><td>{{ row.valueTypeText || valueTypeName(row.valueType) }}</td><td>{{ row.validList || row.validValues.join(', ') }}</td><td>{{ row.defaultValue }}</td><td>{{ row.description }}</td><td>{{ row.controlledText || controlledName(row.controlled) }}</td><td>{{ row.inputTypeText || inputTypeName(row.inputType) }}</td><td>{{ row.allowNullText || allowNullName(row.allowNull) }}</td></template><td v-else-if="'psNo' in row">{{ row.psNo }}</td>
          </tr>
          <tr v-if="pickerRows.length === 0"><td :colspan="pickerIsElement ? (pickerIsMulti ? 14 : 13) : (pickerKind === 'group' || pickerKind === 'editorGroup' ? 3 : 2)" class="empty-row">沒有符合條件的資料</td></tr>
        </tbody></table></div>
        <footer class="dialog-actions"><button class="kd-button primary" type="button" :disabled="!pickerSelection.length" @click="commitPicker">{{ pickerKind === 'addElements' ? '加入選取項目' : '確定' }}</button><button class="kd-button" type="button" @click="pickerOpen = false">關閉</button></footer>
      </section>
    </div>
  </div>
</template>

<style scoped>
.op-group-attrib-page{box-sizing:border-box;height:calc(100vh - 116px);min-height:500px;padding:8px 10px 9px;background:#fff;color:#303030;font:13px "Microsoft JhengHei",Arial,sans-serif;display:flex;flex-direction:column;gap:6px;overflow:auto}
.query-strip{min-height:44px;display:flex;align-items:center;gap:8px;flex-wrap:wrap;border:1px solid #d1d1d1;padding:4px 8px;background:#fff}.query-item{display:inline-flex;align-items:center;gap:6px;white-space:nowrap}.query-item>input[type=checkbox],.kd-grid input[type=checkbox]{accent-color:#d59a2c}.query-input{width:148px}
.kd-input{box-sizing:border-box;min-width:0;border:1px solid #999;background:#fff0b5;color:#333;padding:5px 7px;font:inherit;height:29px}.kd-input:focus{outline:1px solid #d0a243;box-shadow:0 0 0 1px #f5d88a}.kd-input[readonly],.kd-input:disabled{background:#eee;color:#777}.kd-button{min-height:30px;border:1px solid #b9b9b9;border-radius:3px;background:linear-gradient(#fff,#f1f1f1);color:#333;padding:4px 10px;font:inherit;white-space:nowrap;cursor:pointer}.kd-button:hover:not(:disabled){background:#fff1c5;border-color:#cba64b}.kd-button:disabled{opacity:.52;cursor:default}.kd-button.primary{background:#ffa62d;border-color:#ef9419;color:#fff;font-weight:600}.kd-button.primary:hover:not(:disabled){background:#f29419}.lookup-button{width:34px;padding:2px 4px}
.win-group{box-sizing:border-box;min-width:0;flex:1;margin:0;border:1px solid #bdbdbd;padding:8px}.win-group legend{padding:0 5px;font-size:14px;color:#4a4a4a}.panel-layout{display:flex;gap:8px;min-height:0;height:calc(100% - 25px)}.grid-scroll{flex:1;min-width:0;overflow:auto;border:1px solid #d0d0d0}.kd-grid{width:max-content;min-width:100%;border-collapse:collapse;background:#fff;font-size:13px}.kd-grid th{position:sticky;top:0;z-index:1;background:#4d9ed8;color:#fff;font-weight:500;text-align:left;white-space:nowrap}.kd-grid th,.kd-grid td{height:27px;padding:3px 7px;border:1px solid #d1d1d1;white-space:nowrap}.kd-grid tbody tr{cursor:pointer}.kd-grid tbody tr:hover{background:#fff4cf}.kd-grid tbody tr.active{background:#ffe79b}.check-cell{text-align:center;width:26px;padding:2px!important}.number-cell{text-align:right}.center-cell{text-align:center}.empty-row{text-align:center;color:#777;height:48px!important}.vertical-actions{width:86px;flex:0 0 86px;display:flex;flex-direction:column;gap:6px}.vertical-actions .kd-button{width:100%}.grid-status{min-height:20px;padding:4px 2px 0;color:#777;font-size:12px}.message{margin:0;padding:5px 8px;border:1px solid #d8d8d8}.error-message,.dialog-error{color:#8f2020;background:#fff0ee;border-color:#d5aaa5}.notice-message{color:#315d25;background:#f1f8e9;border-color:#bed4b3}
.dialog-backdrop{position:fixed;inset:0;z-index:1100;display:flex;align-items:center;justify-content:center;padding:14px;background:#0006}.win-dialog{box-sizing:border-box;background:#fff;border:1px solid #aaa;box-shadow:0 5px 18px #0004;padding:12px}.editor-dialog{width:min(1120px,calc(100vw - 28px));max-height:calc(100vh - 28px);overflow:auto}.dialog-title{font-size:16px;font-weight:600;padding:0 2px 10px;border-bottom:1px solid #ddd;margin-bottom:10px}.editor-identifiers{display:grid;grid-template-columns:minmax(250px,1fr) minmax(250px,1fr);gap:7px 18px;margin-bottom:9px}.editor-identifiers>label{display:flex;flex-direction:column;gap:4px}.lookup-control{display:flex;gap:5px}.lookup-control .kd-input{flex:1}.customer-name,.group-name{color:#777;font-size:12px}.form-group{margin:0;border:1px solid #c4c4c4;padding:8px}.form-group legend{padding:0 5px}.editor-grid-layout{display:flex;gap:8px;height:min(48vh,450px);min-height:220px}.editor-grid-scroll{height:100%}.editor-grid{min-width:950px}.cell-input{width:100%;height:27px;padding:3px 5px}.allow-input{min-width:90px}.sequence-input{width:70px}.audit-row{display:grid;grid-template-columns:1fr 1fr;gap:6px 10px;margin-top:9px;padding:7px;border:1px solid #ddd}.audit-row label{display:grid;grid-template-columns:58px 1fr;align-items:center;gap:6px}.audit-input{box-sizing:border-box;width:100%;height:26px;border:1px solid #ccc;background:#eee;color:#777;padding:3px 5px;font:inherit}.dialog-actions{display:flex;justify-content:flex-end;gap:7px;margin-top:10px}.dialog-actions .primary{min-width:90px}.dialog-error{padding:5px 7px;margin:8px 0 0;border:1px solid}.picker-layer{z-index:1110}.picker-dialog{width:min(760px,calc(100vw - 28px));max-height:min(680px,calc(100vh - 28px));display:flex;flex-direction:column}.picker-search{display:flex;gap:6px;margin-bottom:8px}.picker-search .kd-input{flex:1}.picker-scroll{overflow:auto;max-height:470px;border:1px solid #d0d0d0}.picker-scroll .kd-grid{width:100%}.picker-scroll tbody tr{cursor:pointer}.picker-scroll tbody tr:hover{background:#fff1c5}
.picker-dialog.element-picker{width:min(1180px,calc(100vw - 28px))}
@media(max-width:900px){.op-group-attrib-page{height:auto;min-height:100%}.query-item{flex-wrap:wrap}.panel-layout{height:calc(100% - 25px)}}
@media(max-width:640px){.panel-layout,.editor-grid-layout{height:auto;flex-direction:column}.result-panel{min-height:420px}.grid-scroll{min-height:270px}.vertical-actions{width:auto;flex:0 0 auto;flex-direction:row;flex-wrap:wrap}.vertical-actions .kd-button{width:auto;flex:1 1 75px}.editor-identifiers{grid-template-columns:1fr}.audit-row{grid-template-columns:1fr}.picker-search{flex-wrap:wrap}}
</style>
