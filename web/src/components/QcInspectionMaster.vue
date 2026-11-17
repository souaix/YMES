<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type IssueState = 0 | 1 | 2 | -1
type Mode = 'add' | 'edit' | 'copy'
type ItemType = 0 | 1 | 2 | 3
interface InspectionRow {
  opNo: string; equipmentNo: string; eqpDataType: 0 | 1; inspType: number; inspWeek: number
  inspStartTime: string; periodMin: number | null; equipmentState: number | null; lotCount: number | null
  issueState: IssueState; mailGroupNo: string; ccMailGroupNo: string; creator: string; createDate: string
  reviser: string; reviseDate: string; dataStamp: number
}
interface InspectionItem { qcItemNo: string; qcItemName: string; qcItemType: ItemType; lotIsValid: number }
interface DetailRow { qcItemNo: string; qcItemName: string; qcItemType: ItemType; lotIsValid: number }
interface Options {
  operations: string[]; equipment: string[]; equipmentTypes: string[]; equipmentStates: { value: number; label: string }[]
  inspectionWeeks: { value: number; label: string }[]; mailGroups: string[]; qcItems: InspectionItem[]
}
interface Store { rows: InspectionRow[]; details: Record<string, DetailRow[]>; options: Options }
interface Editor {
  opNo: string; equipmentNo: string; equipmentType: string; eqpDataType: 0 | 1; inspType: number
  inspWeek: number; inspStartTime: string; periodMin: string; equipmentState: number | ''; lotCount: string
  mailGroupNo: string; ccMailGroupNo: string
}

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes-dev-mock:qc-inspection-master:v1'
const statusOptions: { value: IssueState; label: string }[] = [
  { value: 0, label: '未凍結' }, { value: 1, label: '簽核中' }, { value: 2, label: '已核准' }, { value: -1, label: '未使用' },
]
const inspectionTypes = [
  { value: 0, label: '首件檢查' }, { value: 1, label: '人員檢查' }, { value: 2, label: '班次檢查' },
  { value: 3, label: '配件檢查' }, { value: 4, label: '每日檢查' }, { value: 5, label: '定期檢查' },
  { value: 6, label: '設備狀態檢查' }, { value: 7, label: '連續批量檢查' }, { value: 8, label: '每週檢查' },
]
const itemTypes: { value: ItemType; label: string }[] = [
  { value: 0, label: '計數品管項目' }, { value: 1, label: '計量品管項目' },
  { value: 2, label: '計量品管項目群組' }, { value: 3, label: '計數順序群組' },
]
const timeOptions = Array.from({ length: 48 }, (_, i) => `${String(Math.floor(i / 2)).padStart(2, '0')}:${i % 2 ? '30' : '00'}`)
const demoOptions: Options = {
  operations: ['DEV-OP-01', 'DEV-OP-02', 'DEV-OP-03'],
  equipment: ['DEV-EQP-01', 'DEV-EQP-02', 'DEV-EQP-03'],
  equipmentTypes: ['DEV-EQP-TYPE-A', 'DEV-EQP-TYPE-B'],
  equipmentStates: [{ value: 0, label: '待機' }, { value: 1, label: '生產' }, { value: 2, label: '維修' }],
  inspectionWeeks: Array.from({ length: 7 }, (_, value) => ({ value, label: `週別 ${value}` })),
  mailGroups: ['DEV-QC-MAIL', 'DEV-ENG-MAIL'],
  qcItems: [
    { qcItemNo: 'DEV-A-001', qcItemName: '外觀缺點數', qcItemType: 0, lotIsValid: 1 },
    { qcItemNo: 'DEV-A-002', qcItemName: '尺寸超差數', qcItemType: 0, lotIsValid: 1 },
    { qcItemNo: 'DEV-V-001', qcItemName: '厚度', qcItemType: 1, lotIsValid: 1 },
    { qcItemNo: 'DEV-V-002', qcItemName: '重量', qcItemType: 1, lotIsValid: 1 },
    { qcItemNo: 'DEV-G-001', qcItemName: '外觀檢查群組', qcItemType: 2, lotIsValid: 1 },
    { qcItemNo: 'DEV-AG-001', qcItemName: '計數順序群組', qcItemType: 3, lotIsValid: 1 },
  ],
}
const demoRows: InspectionRow[] = [
  { opNo: 'DEV-OP-01', equipmentNo: 'DEV-EQP-01', eqpDataType: 0, inspType: 0, inspWeek: -1, inspStartTime: 'N/A', periodMin: null, equipmentState: null, lotCount: null, issueState: 0, mailGroupNo: 'DEV-QC-MAIL', ccMailGroupNo: '', creator: 'DEV001', createDate: '2026-11-17 08:30', reviser: '', reviseDate: '', dataStamp: 1 },
  { opNo: '*', equipmentNo: 'DEV-EQP-TYPE-A', eqpDataType: 1, inspType: 4, inspWeek: -1, inspStartTime: '08:00', periodMin: null, equipmentState: null, lotCount: null, issueState: 2, mailGroupNo: 'DEV-QC-MAIL', ccMailGroupNo: 'DEV-ENG-MAIL', creator: 'DEV001', createDate: '2026-11-16 12:10', reviser: 'DEV002', reviseDate: '2026-11-16 13:10', dataStamp: 2 },
  { opNo: 'DEV-OP-02', equipmentNo: 'DEV-EQP-02', eqpDataType: 0, inspType: 5, inspWeek: -1, inspStartTime: 'N/A', periodMin: 120, equipmentState: null, lotCount: null, issueState: 1, mailGroupNo: '', ccMailGroupNo: '', creator: 'DEV001', createDate: '2026-11-15 16:00', reviser: 'DEV001', reviseDate: '2026-11-17 07:00', dataStamp: 3 },
  { opNo: 'DEV-OP-03', equipmentNo: 'DEV-EQP-03', eqpDataType: 0, inspType: 8, inspWeek: 1, inspStartTime: '09:00', periodMin: null, equipmentState: null, lotCount: null, issueState: -1, mailGroupNo: '', ccMailGroupNo: '', creator: 'DEV002', createDate: '2026-11-12 09:00', reviser: 'DEV002', reviseDate: '2026-11-14 09:00', dataStamp: 4 },
]

const rows = ref<InspectionRow[]>([])
const details = ref<DetailRow[]>([])
const options = ref<Options>(structuredClone(demoOptions))
const issueStateFilter = ref<IssueState>(0)
const opSearch = ref(''); const equipmentSearch = ref(''); const inspTypeSearch = ref('')
const selectedKey = ref(''); const selectedKeys = ref<string[]>([])
const loading = ref(false); const busy = ref(false); const error = ref(''); const notice = ref('')
const mode = ref<Mode>('add'); const formOpen = ref(false); const deleteOpen = ref(false)
const editingOriginal = ref<InspectionRow | null>(null)
const editor = ref<Editor>(blankEditor())
const draftDetails = ref<DetailRow[]>([])
const selectedItemType = ref<ItemType>(0)
const selectedAvailable = ref<string[]>([]); const selectedIncluded = ref<string[]>([])
const detailForSelection = computed(() => details.value)
const filteredRows = computed(() => rows.value.filter(row => row.issueState === issueStateFilter.value
  && (!opSearch.value.trim() || row.opNo.toUpperCase().includes(opSearch.value.trim().toUpperCase()))
  && (!equipmentSearch.value.trim() || row.equipmentNo.toUpperCase().includes(equipmentSearch.value.trim().toUpperCase()))
  && (inspTypeSearch.value === '' || row.inspType === Number(inspTypeSearch.value))))
const selectedRows = computed(() => filteredRows.value.filter(row => selectedKeys.value.includes(rowKey(row))))
const selectedRow = computed(() => rows.value.find(row => rowKey(row) === selectedKey.value) ?? null)
const availableItems = computed(() => options.value.qcItems.filter(item => item.qcItemType === selectedItemType.value
  && item.lotIsValid !== 0 && !draftDetails.value.some(detail => detail.qcItemNo === item.qcItemNo)))
const timeIsUsed = computed(() => editor.value.inspType === 4 || editor.value.inspType === 8)
const eqpScopeLabel = (row: InspectionRow) => row.eqpDataType === 0 ? '設備' : '設備類型'
const stateLabel = (value: IssueState) => statusOptions.find(item => item.value === value)?.label ?? String(value)
const typeLabel = (value: number) => inspectionTypes.find(item => item.value === value)?.label ?? String(value)
const itemTypeLabel = (value: number) => itemTypes.find(item => item.value === value)?.label ?? String(value)

function blankEditor(): Editor {
  return { opNo: '', equipmentNo: '', equipmentType: '', eqpDataType: 0, inspType: 0, inspWeek: 0, inspStartTime: '08:00', periodMin: '', equipmentState: '', lotCount: '', mailGroupNo: '', ccMailGroupNo: '' }
}
function rowKey(row: InspectionRow): string { return [row.opNo, row.equipmentNo, row.inspType, row.inspWeek, row.inspStartTime].join('\u001f') }
function detailKey(row: Pick<InspectionRow, 'opNo' | 'equipmentNo' | 'inspType'>): string { return [row.opNo, row.equipmentNo, row.inspType].join('\u001f') }
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function readStore(): Store {
  const saved = window.localStorage.getItem(storageKey)
  if (saved) {
    try { return JSON.parse(saved) as Store } catch { /* reset invalid DEV cache */ }
  }
  return { rows: clone(demoRows), details: { [detailKey(demoRows[0])]: [{ ...demoOptions.qcItems[0] }, { ...demoOptions.qcItems[2] }] }, options: clone(demoOptions) }
}
function writeStore(store: Store): void { window.localStorage.setItem(storageKey, JSON.stringify(store)) }
function normalizeRow(source: Record<string, unknown>): InspectionRow {
  return {
    opNo: String(source.opNo ?? source.OPNO ?? ''), equipmentNo: String(source.equipmentNo ?? source.EQUIPMENTNO ?? ''),
    eqpDataType: Number(source.eqpDataType ?? source.EQPDATATYPE ?? 0) === 1 ? 1 : 0,
    inspType: Number(source.inspType ?? source.INSPTYPE ?? 0), inspWeek: Number(source.inspWeek ?? source.INSPWEEK ?? -1),
    inspStartTime: String(source.inspStartTime ?? source.INSPSTARTTIME ?? 'N/A'),
    periodMin: source.periodMin == null && source.PERIODMIN == null ? null : Number(source.periodMin ?? source.PERIODMIN),
    equipmentState: source.equipmentState == null && source.EQUIPMENTSTATE == null ? null : Number(source.equipmentState ?? source.EQUIPMENTSTATE),
    lotCount: source.lotCount == null && source.LOTCOUNT == null ? null : Number(source.lotCount ?? source.LOTCOUNT),
    issueState: Number(source.issueState ?? source.ISSUESTATE ?? 0) as IssueState,
    mailGroupNo: String(source.mailGroupNo ?? source.MAILGROUPNO ?? ''), ccMailGroupNo: String(source.ccMailGroupNo ?? source.CCMAILGROUPNO ?? ''),
    creator: String(source.creator ?? source.CREATOR ?? ''), createDate: String(source.createDate ?? source.CREATEDATE ?? ''),
    reviser: String(source.reviser ?? source.REVISER ?? ''), reviseDate: String(source.reviseDate ?? source.REVISEDATE ?? ''),
    dataStamp: Number(source.dataStamp ?? source.DATASTAMP ?? 0),
  }
}
async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const base = props.apiBaseUrl.trim().replace(/\/+$/, '')
  const response = await fetch(`${base}/${path.replace(/^\/+/, '')}`, { credentials: 'include', ...init, headers: { Accept: 'application/json', ...(init?.body ? { 'Content-Type': 'application/json' } : {}), ...init?.headers } })
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; title?: string; message?: string } | null
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 呼叫失敗（${response.status}）。`)
  }
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}
function normalizeDetails(payload: unknown): DetailRow[] {
  const list = Array.isArray(payload) ? payload : (payload as { items?: unknown[] } | null)?.items ?? []
  return list.map(value => {
    const item = value as Record<string, unknown>
    return { qcItemNo: String(item.qcItemNo ?? item.QCITEMNO ?? ''), qcItemName: String(item.qcItemName ?? item.QCITEMNAME ?? ''), qcItemType: Number(item.qcItemType ?? item.QCITEMTYPE ?? 0) as ItemType, lotIsValid: Number(item.lotIsValid ?? item.LOTISVALID ?? 1) }
  })
}
async function loadOptions(): Promise<void> {
  if (mock.value) { options.value = readStore().options; return }
  const result = await request<Options>('api/qc/inspections/options')
  options.value = result
}
async function loadRows(): Promise<void> {
  loading.value = true; error.value = ''; notice.value = ''
  try {
    if (mock.value) rows.value = readStore().rows.map(normalizeRow)
    else {
      const query = new URLSearchParams({ issueState: String(issueStateFilter.value), opNo: opSearch.value.trim(), equipmentNo: equipmentSearch.value.trim(), inspType: inspTypeSearch.value })
      const payload = await request<InspectionRow[] | { items?: Record<string, unknown>[] }>(`api/qc/inspections?${query}`)
      const list = Array.isArray(payload) ? payload : payload.items ?? []
      rows.value = list.map(normalizeRow)
    }
    selectedKeys.value = selectedKeys.value.filter(key => filteredRows.value.some(row => rowKey(row) === key))
    if (!filteredRows.value.some(row => rowKey(row) === selectedKey.value)) selectedKey.value = filteredRows.value[0] ? rowKey(filteredRows.value[0]) : ''
    if (selectedRow.value) await loadDetails(selectedRow.value)
    else details.value = []
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入設備品檢資料失敗。' }
  finally { loading.value = false }
}
async function loadDetails(row: InspectionRow): Promise<void> {
  try {
    if (mock.value) details.value = readStore().details[detailKey(row)] ?? []
    else details.value = normalizeDetails(await request(`api/qc/inspections/${encodeURIComponent(row.opNo)}/${encodeURIComponent(row.equipmentNo)}/${row.inspType}/details`))
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入品管項目明細失敗。'; details.value = [] }
}
function selectRow(row: InspectionRow): void { selectedKey.value = rowKey(row); selectedKeys.value = [selectedKey.value]; void loadDetails(row) }
function toggleRow(row: InspectionRow): void {
  const key = rowKey(row)
  selectedKeys.value = selectedKeys.value.includes(key) ? selectedKeys.value.filter(item => item !== key) : [...selectedKeys.value, key]
  selectedKey.value = key
  void loadDetails(row)
}
function clearSearch(): void { opSearch.value = ''; equipmentSearch.value = ''; inspTypeSearch.value = ''; void loadRows() }
function changeIssueState(): void { selectedKeys.value = []; selectedKey.value = ''; void loadRows() }
function openEditor(nextMode: Mode): void {
  error.value = ''; notice.value = ''; mode.value = nextMode; draftDetails.value = []; editingOriginal.value = null
  if (nextMode === 'add' || !selectedRow.value) editor.value = blankEditor()
  else {
    const row = selectedRow.value; editingOriginal.value = clone(row)
    editor.value = { opNo: row.opNo, equipmentNo: row.eqpDataType === 0 ? row.equipmentNo : '', equipmentType: row.eqpDataType === 1 ? row.equipmentNo : '', eqpDataType: row.eqpDataType, inspType: row.inspType, inspWeek: row.inspWeek < 0 ? 0 : row.inspWeek, inspStartTime: row.inspStartTime === 'N/A' ? '08:00' : row.inspStartTime, periodMin: row.periodMin == null ? '' : String(row.periodMin), equipmentState: row.equipmentState ?? '', lotCount: row.lotCount == null ? '' : String(row.lotCount), mailGroupNo: row.mailGroupNo, ccMailGroupNo: row.ccMailGroupNo }
    draftDetails.value = clone(mock.value ? readStore().details[detailKey(row)] ?? details.value : details.value)
    if (nextMode === 'copy') { editor.value.opNo = ''; editor.value.equipmentNo = ''; editor.value.equipmentType = '' }
  }
  selectedItemType.value = 0; selectedAvailable.value = []; selectedIncluded.value = []; formOpen.value = true
}
function setEquipmentScope(value: 0 | 1): void {
  if (mode.value === 'edit') return
  editor.value.eqpDataType = value; editor.value.equipmentNo = ''; editor.value.equipmentType = ''
}
function addAvailableItems(): void {
  const chosen = new Set(selectedAvailable.value)
  draftDetails.value.push(...availableItems.value.filter(item => chosen.has(item.qcItemNo)).map(item => ({ ...item })))
  selectedAvailable.value = []
}
function removeIncludedItems(): void {
  const chosen = new Set(selectedIncluded.value)
  draftDetails.value = draftDetails.value.filter(item => !chosen.has(item.qcItemNo)); selectedIncluded.value = []
}
function itemListForType(type: ItemType): DetailRow[] { return draftDetails.value.filter(item => item.qcItemType === type) }
function normalizedEditor(): Record<string, unknown> {
  const equipmentNo = editor.value.eqpDataType === 0 ? editor.value.equipmentNo.trim().toUpperCase() : editor.value.equipmentType.trim().toUpperCase()
  return {
    opNo: editor.value.opNo.trim().toUpperCase(), equipmentNo, eqpDataType: editor.value.eqpDataType,
    inspType: editor.value.inspType, inspWeek: editor.value.inspType === 8 ? editor.value.inspWeek : -1,
    inspStartTime: timeIsUsed.value ? editor.value.inspStartTime : 'N/A',
    periodMin: editor.value.inspType === 5 ? Number(editor.value.periodMin) : null,
    equipmentState: editor.value.inspType === 6 ? Number(editor.value.equipmentState) : null,
    lotCount: editor.value.inspType === 7 ? Number(editor.value.lotCount) : null,
    mailGroupNo: editor.value.mailGroupNo.trim().toUpperCase(), ccMailGroupNo: editor.value.ccMailGroupNo.trim().toUpperCase(),
    details: draftDetails.value.map(({ qcItemNo, qcItemType }) => ({ qcItemNo, qcItemType })),
  }
}
function validateEditor(): string {
  if (!editor.value.opNo.trim()) return 'OP No 必填。'
  if (editor.value.opNo.trim() !== '*' && !options.value.operations.includes(editor.value.opNo.trim().toUpperCase())) return '請選擇有效且已核准的作業站；全作業站請輸入 *。'
  const scope = editor.value.eqpDataType === 0 ? editor.value.equipmentNo.trim() : editor.value.equipmentType.trim()
  if (!scope) return editor.value.eqpDataType === 0 ? '設備編號必填。' : '設備類型必填。'
  const allowedScope = editor.value.eqpDataType === 0 ? options.value.equipment : options.value.equipmentTypes
  if (!allowedScope.includes(scope.toUpperCase())) return '設備或設備類型必須由查找清單選取。'
  if (!inspectionTypes.some(type => type.value === editor.value.inspType)) return '請選擇一種檢查類型。'
  if (editor.value.inspType === 4 && !timeOptions.includes(editor.value.inspStartTime)) return '請選擇每日檢查時間。'
  if (editor.value.inspType === 5) {
    const value = Number(editor.value.periodMin)
    if (!Number.isInteger(value) || value < 1 || value > 99999) return '定期檢查分鐘需為 1–99999 的整數。'
  }
  if (editor.value.inspType === 6 && (editor.value.equipmentState === '' || !options.value.equipmentStates.some(item => item.value === Number(editor.value.equipmentState)))) return '請選擇有效設備狀態。'
  if (editor.value.inspType === 7) {
    const value = Number(editor.value.lotCount)
    if (!Number.isInteger(value) || value < 0 || value > 999) return '連續批量需為 0–999 的整數。'
  }
  if (editor.value.inspType === 8 && (!options.value.inspectionWeeks.some(item => item.value === editor.value.inspWeek) || !timeOptions.includes(editor.value.inspStartTime))) return '請選擇有效週別與每週檢查時間。'
  if (!draftDetails.value.length) return '至少需選擇一個品管項目。'
  if (draftDetails.value.some(item => item.lotIsValid === 0)) return '品管項目 LotIsValid=0，不可加入設備品檢設定。'
  if (editor.value.ccMailGroupNo.trim() && !editor.value.mailGroupNo.trim()) return '設定副本通知群組時，必須先選擇通知群組。'
  if (editor.value.mailGroupNo && !options.value.mailGroups.includes(editor.value.mailGroupNo)) return '請從通知群組清單選擇主通知群組。'
  if (editor.value.ccMailGroupNo && !options.value.mailGroups.includes(editor.value.ccMailGroupNo)) return '請從通知群組清單選擇副本通知群組。'
  return ''
}
async function saveEditor(): Promise<void> {
  error.value = ''; notice.value = ''
  const validation = validateEditor(); if (validation) { error.value = validation; return }
  busy.value = true
  try {
    const payload = normalizedEditor()
    if (mock.value) {
      const store = readStore(); const now = new Date().toLocaleString('sv-SE').replace('T', ' ')
      const original = editingOriginal.value
      const next = normalizeRow({ ...payload, issueState: 0, creator: mode.value === 'edit' ? original?.creator : props.currentUserNo || 'DEV001', createDate: mode.value === 'edit' ? original?.createDate : now, reviser: mode.value === 'edit' ? props.currentUserNo || 'DEV001' : '', reviseDate: mode.value === 'edit' ? now : '', dataStamp: (original?.dataStamp ?? 0) + 1 })
      if (mode.value === 'edit' && original) store.rows = store.rows.filter(row => rowKey(normalizeRow(row as unknown as Record<string, unknown>)) !== rowKey(original))
      const duplicate = store.rows.some(row => rowKey(normalizeRow(row as unknown as Record<string, unknown>)) === rowKey(next))
      if (duplicate) throw new Error('相同 OP、設備範圍、檢查類型、週別及時間的設定已存在。')
      store.rows.push(next); store.details[detailKey(next)] = clone(draftDetails.value); writeStore(store)
      selectedKey.value = rowKey(next); selectedKeys.value = [rowKey(next)]
    } else if (mode.value === 'add' || mode.value === 'copy') {
      const result = await request<InspectionRow>('api/qc/inspections', { method: 'POST', body: JSON.stringify(payload) })
      selectedKey.value = rowKey(normalizeRow(result)); selectedKeys.value = [selectedKey.value]
    } else if (editingOriginal.value) {
      await request(`api/qc/inspections/${encodeURIComponent(editingOriginal.value.opNo)}/${encodeURIComponent(editingOriginal.value.equipmentNo)}/${editingOriginal.value.inspType}/${editingOriginal.value.inspWeek}/${encodeURIComponent(editingOriginal.value.inspStartTime)}`, { method: 'PUT', body: JSON.stringify({ ...payload, dataStamp: editingOriginal.value.dataStamp }) })
    }
    formOpen.value = false; await loadRows(); notice.value = mode.value === 'edit' ? '設備品檢設定已修改。' : '設備品檢設定已新增。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '儲存設備品檢設定失敗。' }
  finally { busy.value = false }
}
async function deleteSelected(): Promise<void> {
  if (!selectedRows.value.length) return
  error.value = ''; notice.value = ''; busy.value = true
  try {
    const targets = [...selectedRows.value]
    for (const row of targets) {
      if (row.issueState === 1) throw new Error(`${row.opNo}/${row.equipmentNo} 正在簽核中，不可刪除。`)
      if (mock.value) {
        const store = readStore()
        if (row.issueState === 2) store.rows = store.rows.map(item => rowKey(normalizeRow(item as unknown as Record<string, unknown>)) === rowKey(row) ? { ...item, issueState: -1, reviser: props.currentUserNo || 'DEV001', reviseDate: new Date().toLocaleString('sv-SE').replace('T', ' '), dataStamp: item.dataStamp + 1 } : item)
        else { store.rows = store.rows.filter(item => rowKey(normalizeRow(item as unknown as Record<string, unknown>)) !== rowKey(row)); delete store.details[detailKey(row)] }
        writeStore(store)
      } else await request(`api/qc/inspections/${encodeURIComponent(row.opNo)}/${encodeURIComponent(row.equipmentNo)}/${row.inspType}/${row.inspWeek}/${encodeURIComponent(row.inspStartTime)}`, { method: 'DELETE', headers: { 'If-Match': String(row.dataStamp) } })
    }
    deleteOpen.value = false; selectedKeys.value = []; await loadRows(); notice.value = `${targets.length} 筆設備品檢設定已處理。`
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '刪除設備品檢設定失敗。' }
  finally { busy.value = false }
}
async function approveSelected(): Promise<void> {
  const pending = selectedRows.value.filter(row => row.issueState === 0)
  if (!pending.length) { error.value = '請選擇未凍結的資料。'; return }
  error.value = ''; notice.value = ''; busy.value = true
  try {
    for (const row of pending) {
      if (mock.value) {
        const store = readStore(); const target = store.rows.find(item => rowKey(normalizeRow(item as unknown as Record<string, unknown>)) === rowKey(row))
        if (target) { target.issueState = 2; target.reviser = props.currentUserNo || 'DEV001'; target.reviseDate = new Date().toLocaleString('sv-SE').replace('T', ' '); target.dataStamp += 1 }
        writeStore(store)
      } else await request(`api/qc/inspections/${encodeURIComponent(row.opNo)}/${encodeURIComponent(row.equipmentNo)}/${row.inspType}/${row.inspWeek}/${encodeURIComponent(row.inspStartTime)}/approval`, { method: 'POST', body: JSON.stringify({ dataStamp: row.dataStamp }) })
    }
    selectedKeys.value = []; await loadRows(); notice.value = `${pending.length} 筆資料已送出核准。`
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '核准設備品檢設定失敗。' }
  finally { busy.value = false }
}

onMounted(async () => {
  loading.value = true
  try { await loadOptions() } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入查找資料失敗。' }
  await loadRows()
})
</script>

<template>
  <main class="qci-page">
    <header class="qci-title"><div><h1>設備品檢管理設定</h1><small>QC217 · QC INSPECTION</small></div><button class="qci-btn" @click="emit('close')">關閉</button></header>
    <fieldset class="qci-status"><legend>資料狀態</legend><label v-for="item in statusOptions" :key="item.value"><input v-model.number="issueStateFilter" type="radio" :value="item.value" @change="changeIssueState">{{ item.label }}</label></fieldset>
    <form class="qci-query" @submit.prevent="loadRows">
      <label>作業站編號 <input v-model="opSearch" maxlength="20"></label><label>設備編號／類型 <input v-model="equipmentSearch" maxlength="50"></label>
      <label>檢查類型 <select v-model="inspTypeSearch"><option value="">全部</option><option v-for="item in inspectionTypes" :key="item.value" :value="String(item.value)">{{ item.value }} {{ item.label }}</option></select></label>
      <button class="qci-btn primary" :disabled="loading">查詢</button><button type="button" class="qci-btn" @click="clearSearch">清除</button>
      <span class="qci-mode" v-if="mock">DEV 假資料</span>
    </form>
    <section class="qci-main">
      <div class="qci-grids">
        <section class="qci-box qci-master-box"><h2>設備品檢設定</h2><div class="qci-grid-scroll">
          <table class="qci-grid"><thead><tr><th class="select-cell"></th><th>OP No</th><th>範圍</th><th>設備編號／類型</th><th>檢查類型</th><th>週別</th><th>開始時間</th><th>週期(分)</th><th>設備狀態</th><th>連續批量</th><th>通知群組</th><th>副本群組</th><th>狀態</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th></tr></thead>
            <tbody><tr v-for="row in filteredRows" :key="rowKey(row)" :class="{ active: selectedKey === rowKey(row) }" @click="selectRow(row)">
              <td class="select-cell"><input type="checkbox" :checked="selectedKeys.includes(rowKey(row))" @click.stop="toggleRow(row)"></td><td>{{ row.opNo }}</td><td>{{ eqpScopeLabel(row) }}</td><td>{{ row.equipmentNo }}</td><td>{{ row.inspType }} {{ typeLabel(row.inspType) }}</td><td>{{ row.inspWeek < 0 ? '' : options.inspectionWeeks.find(item => item.value === row.inspWeek)?.label ?? row.inspWeek }}</td><td>{{ row.inspStartTime === 'N/A' ? '' : row.inspStartTime }}</td><td>{{ row.periodMin ?? '' }}</td><td>{{ options.equipmentStates.find(item => item.value === row.equipmentState)?.label ?? row.equipmentState ?? '' }}</td><td>{{ row.lotCount ?? '' }}</td><td>{{ row.mailGroupNo }}</td><td>{{ row.ccMailGroupNo }}</td><td><span class="qci-state" :class="`state-${row.issueState}`">{{ stateLabel(row.issueState) }}</span></td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser }}</td><td>{{ row.reviseDate }}</td>
            </tr><tr v-if="!filteredRows.length"><td colspan="17" class="empty">{{ loading ? '載入中…' : '查無資料' }}</td></tr></tbody>
          </table>
        </div><footer>資料總筆數　{{ filteredRows.length }}<span v-if="selectedKeys.length">　已選取 {{ selectedKeys.length }} 筆</span></footer></section>
        <section class="qci-box qci-detail-box"><h2>品管項目明細 <small v-if="selectedRow">{{ selectedRow.opNo }} / {{ selectedRow.equipmentNo }} / {{ typeLabel(selectedRow.inspType) }}</small></h2><div class="qci-grid-scroll"><table class="qci-grid"><thead><tr><th>品管項目編號</th><th>品管項目名稱</th><th>品管項目類型</th></tr></thead><tbody><tr v-for="item in detailForSelection" :key="item.qcItemNo"><td>{{ item.qcItemNo }}</td><td>{{ item.qcItemName }}</td><td>{{ item.qcItemType }} {{ itemTypeLabel(item.qcItemType) }}</td></tr><tr v-if="!detailForSelection.length"><td colspan="3" class="empty">請選取一筆設備品檢設定</td></tr></tbody></table></div></section>
      </div>
      <aside class="qci-actions"><button class="qci-btn primary" :disabled="busy" @click="openEditor('add')">新增(A)</button><button class="qci-btn" :disabled="busy || selectedKeys.length !== 1 || selectedRow?.issueState !== 0" @click="openEditor('edit')">修改(E)</button><button class="qci-btn" :disabled="busy || selectedKeys.length !== 1" @click="openEditor('copy')">複製(C)</button><button class="qci-btn" :disabled="busy || !selectedRows.some(row => row.issueState === 0)" @click="approveSelected">核准</button><button class="qci-btn danger" :disabled="busy || !selectedRows.length || selectedRows.some(row => row.issueState === 1)" @click="deleteOpen = true">刪除(D)</button><span class="qci-action-space"></span><button class="qci-btn" @click="emit('close')">關閉</button></aside>
    </section>
    <p v-if="error" class="qci-message error">{{ error }}</p><p v-if="notice" class="qci-message success">{{ notice }}</p>

    <div v-if="formOpen" class="qci-shade" @click.self="formOpen = false"><section class="qci-dialog" role="dialog" aria-modal="true">
      <header><div><h2>設備品檢管理設定 ({{ mode.toUpperCase() }})</h2><small>QC Inspection Define</small></div><button type="button" class="qci-x" @click="formOpen = false">×</button></header>
      <div class="qci-form-scroll"><div class="qci-form-grid">
        <label class="field">作業站編號<input v-model="editor.opNo" list="qci-operations" maxlength="20" :readonly="mode === 'edit'" placeholder="輸入或選擇作業站；* 代表全部"><datalist id="qci-operations"><option v-for="op in options.operations" :key="op" :value="op"></option></datalist></label>
        <fieldset class="scope-field"><legend>設備範圍（擇一）</legend><label><input type="radio" :checked="editor.eqpDataType === 0" :disabled="mode === 'edit'" @change="setEquipmentScope(0)">設備編號</label><label><input type="radio" :checked="editor.eqpDataType === 1" :disabled="mode === 'edit'" @change="setEquipmentScope(1)">設備類型</label><input v-if="editor.eqpDataType === 0" v-model="editor.equipmentNo" list="qci-equipments" :readonly="mode === 'edit'" maxlength="50"><input v-else v-model="editor.equipmentType" list="qci-equipment-types" :readonly="mode === 'edit'" maxlength="50"><datalist id="qci-equipments"><option v-for="item in options.equipment" :key="item" :value="item"></option></datalist><datalist id="qci-equipment-types"><option v-for="item in options.equipmentTypes" :key="item" :value="item"></option></datalist></fieldset>
        <label class="field">通知群組<select v-model="editor.mailGroupNo"><option value="">（不通知）</option><option v-for="group in options.mailGroups" :key="group" :value="group">{{ group }}</option></select></label><label class="field">副本通知群組<select v-model="editor.ccMailGroupNo"><option value="">（無）</option><option v-for="group in options.mailGroups" :key="group" :value="group">{{ group }}</option></select></label>
        <fieldset class="schedule-field"><legend>檢查類型及排程條件</legend><div class="inspection-options"><label v-for="item in inspectionTypes" :key="item.value"><input v-model.number="editor.inspType" type="radio" name="inspection-type" :value="item.value" :disabled="mode === 'edit'">{{ item.value }} {{ item.label }}</label></div>
          <div class="schedule-controls"><label v-if="editor.inspType === 4" class="field">每日開始時間<select v-model="editor.inspStartTime"><option v-for="time in timeOptions" :key="time" :value="time">{{ time }}</option></select></label>
            <label v-if="editor.inspType === 5" class="field">檢查週期（分鐘）<input v-model="editor.periodMin" inputmode="numeric" type="number" min="1" max="99999"></label>
            <label v-if="editor.inspType === 6" class="field">設備狀態<select v-model.number="editor.equipmentState"><option value="">請選擇</option><option v-for="state in options.equipmentStates" :key="state.value" :value="state.value">{{ state.value }} {{ state.label }}</option></select></label>
            <label v-if="editor.inspType === 7" class="field">連續批量數<input v-model="editor.lotCount" inputmode="numeric" type="number" min="0" max="999"></label>
            <template v-if="editor.inspType === 8"><label class="field">每週週別<select v-model.number="editor.inspWeek"><option v-for="week in options.inspectionWeeks" :key="week.value" :value="week.value">{{ week.label }}</option></select></label><label class="field">每週開始時間<select v-model="editor.inspStartTime"><option v-for="time in timeOptions" :key="time" :value="time">{{ time }}</option></select></label></template>
            <p v-if="![4,5,6,7,8].includes(editor.inspType)" class="schedule-note">此檢查類型不需額外排程值。</p>
          </div>
        </fieldset>
        <fieldset class="items-field"><legend>檢查品管項目</legend><div class="item-type-tabs"><button v-for="type in itemTypes" :key="type.value" type="button" class="qci-btn" :class="{ selected: selectedItemType === type.value }" @click="selectedItemType = type.value; selectedAvailable = []; selectedIncluded = []">{{ type.label }}</button></div>
          <div class="item-picker"><section><h3>可選項目</h3><div class="item-list"><label v-for="item in availableItems" :key="item.qcItemNo"><input v-model="selectedAvailable" type="checkbox" :value="item.qcItemNo"><span>{{ item.qcItemNo }}　{{ item.qcItemName }}</span></label><p v-if="!availableItems.length" class="empty">沒有可加入項目（排除 LotIsValid=0）</p></div></section><div class="item-move"><button type="button" class="qci-btn" :disabled="!selectedAvailable.length" @click="addAvailableItems">加入 →</button><button type="button" class="qci-btn" :disabled="!selectedIncluded.length" @click="removeIncludedItems">← 移除</button></div><section><h3>已選項目 <small>{{ draftDetails.length }} 筆</small></h3><div class="item-list"><label v-for="item in itemListForType(selectedItemType)" :key="item.qcItemNo"><input v-model="selectedIncluded" type="checkbox" :value="item.qcItemNo"><span>{{ item.qcItemNo }}　{{ item.qcItemName }}</span></label><p v-if="!itemListForType(selectedItemType).length" class="empty">尚未加入</p></div></section></div>
          <p class="qci-hint">各類別分開挑選；至少一個項目。來源 WinForms 要求所有選定項目 LotIsValid 不為 0。</p>
        </fieldset>
        <section class="audit"><label>建立人<input :value="editingOriginal?.creator || (mode === 'edit' ? '' : currentUserNo || 'DEV001')" readonly></label><label>建立日期<input :value="editingOriginal?.createDate || '儲存時由系統寫入'" readonly></label><label>修改人<input :value="mode === 'edit' ? currentUserNo || 'DEV001' : ''" readonly></label><label>修改日期<input :value="mode === 'edit' ? '儲存時由系統寫入' : ''" readonly></label></section>
      </div></div>
      <footer><button class="qci-btn primary" :disabled="busy" @click="saveEditor">確定</button><button class="qci-btn" :disabled="busy" @click="formOpen = false">關閉</button></footer>
    </section></div>
    <div v-if="deleteOpen" class="qci-shade" @click.self="deleteOpen = false"><section class="qci-dialog qci-confirm"><header><h2>確認刪除</h2><button class="qci-x" @click="deleteOpen = false">×</button></header><p>選取 {{ selectedRows.length }} 筆設定。未凍結／未使用資料會硬刪；已核准資料改為未使用。來源程式會依 OP、設備及檢查類型一併刪除品管項目明細。</p><footer><button class="qci-btn danger" :disabled="busy" @click="deleteSelected">確定刪除</button><button class="qci-btn" @click="deleteOpen = false">取消</button></footer></section></div>
  </main>
</template>

<style scoped>
.qci-page{height:100%;min-height:560px;display:flex;flex-direction:column;gap:6px;padding:8px;color:#333;background:#fff;box-sizing:border-box;font:13px Arial,"Microsoft JhengHei",sans-serif;overflow:auto}.qci-title{display:flex;align-items:center;justify-content:space-between;border-bottom:1px solid #d6d1c7;padding:0 4px 7px}.qci-title h1{margin:0 0 2px;font-size:20px;font-weight:500}.qci-title small,.qci-box h2 small{color:#7b776e;font-size:11px}.qci-btn{min-height:30px;padding:4px 11px;border:1px solid #bbb7ad;border-radius:3px;background:linear-gradient(#fff,#f4f2ed);color:#333;cursor:pointer;white-space:nowrap}.qci-btn:hover:not(:disabled){background:#fff1c4;border-color:#c3a55c}.qci-btn:disabled{opacity:.48;cursor:not-allowed}.qci-btn.primary{border-color:#efa12c;background:#f7a52f;color:white;font-weight:600}.qci-btn.danger{color:#8e3028}.qci-status{display:flex;align-items:center;gap:18px;margin:0;padding:5px 10px;border:1px solid #d3d0c8}.qci-status legend,.scope-field legend,.schedule-field legend,.items-field legend{padding:0 5px;color:#666}.qci-status label{display:flex;align-items:center;gap:4px}.qci-status input,.qci-grid input[type=checkbox],.scope-field input[type=radio],.inspection-options input,.item-list input{accent-color:#dda12e}.qci-query{display:flex;align-items:center;gap:8px;flex-wrap:wrap;padding:6px 8px;border:1px solid #d3d0c8;background:#f9f8f5}.qci-query label{display:flex;align-items:center;gap:5px}.qci-query input,.qci-query select,.qci-form-grid input:not([type=radio]):not([type=checkbox]),.qci-form-grid select{height:27px;box-sizing:border-box;border:1px solid #aaa69d;border-radius:2px;background:#ffefb0;padding:3px 6px;color:#333;font:inherit}.qci-query input{width:150px}.qci-query select{min-width:140px}.qci-mode{margin-left:auto;color:#85631c;font-size:11px}.qci-main{display:grid;grid-template-columns:minmax(0,1fr) 104px;gap:7px;flex:1;min-height:0}.qci-grids{display:flex;flex-direction:column;gap:7px;min-height:0}.qci-box{display:flex;flex-direction:column;min-height:0;border:1px solid #c8c5bd;padding:5px}.qci-master-box{flex:1.65}.qci-detail-box{flex:.85;min-height:135px}.qci-box h2{display:flex;align-items:center;gap:12px;margin:0 0 5px;font-size:13px;font-weight:600}.qci-grid-scroll{flex:1;min-height:0;overflow:auto;border:1px solid #ccc}.qci-grid{border-collapse:collapse;min-width:100%;font-size:12px;white-space:nowrap}.qci-grid th{position:sticky;top:0;z-index:1;background:#4b9fe8;color:white;font-weight:500;text-align:center}.qci-grid th,.qci-grid td{border:1px solid #d4d0c8;padding:4px 6px}.qci-grid td{background:#fff8dd}.qci-grid tbody tr{cursor:pointer}.qci-grid tbody tr:hover td{background:#ffedac}.qci-grid tbody tr.active td{background:#ffe39a}.qci-grid .select-cell{width:25px;text-align:center}.qci-grid .empty{height:34px;text-align:center;color:#888;background:#faf9f6}.qci-box footer{padding:4px 2px 0;color:#777;font-size:11px}.qci-state{display:inline-block;padding:2px 5px;border-radius:2px}.state-0{background:#fff0b9}.state-1{background:#e4e2dc}.state-2{background:#dbe9d4}.state--1{background:#f2d8d5}.qci-actions{display:flex;flex-direction:column;gap:5px;overflow:auto}.qci-actions .qci-btn{width:100%;min-height:33px}.qci-action-space{flex:1}.qci-message{margin:0;padding:6px 9px;border:1px solid}.qci-message.error{color:#8e3028;background:#fff1ef;border-color:#e3b6af}.qci-message.success{color:#315f2b;background:#f0f7ed;border-color:#bdd1b4}.qci-shade{position:fixed;inset:0;z-index:40;display:flex;align-items:center;justify-content:center;padding:12px;background:#0006}.qci-dialog{display:flex;flex-direction:column;width:min(1120px,98vw);max-height:96vh;border:1px solid #aaa69d;border-top:3px solid #dda12e;background:#f8f7f3;box-shadow:0 8px 25px #0004}.qci-dialog>header{display:flex;align-items:center;justify-content:space-between;padding:10px 14px;border-bottom:1px solid #d2cfc7;background:#f1efe9}.qci-dialog>header h2{margin:0;font-size:17px;font-weight:500}.qci-dialog>header small{color:#777}.qci-x{border:0;background:transparent;font-size:24px;color:#555;cursor:pointer}.qci-form-scroll{overflow:auto;padding:10px 14px}.qci-form-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:9px}.field{display:flex;flex-direction:column;gap:4px}.qci-form-grid .field input,.qci-form-grid .field select{width:100%}.scope-field,.schedule-field,.items-field{min-width:0;margin:0;padding:8px;border:1px solid #c8c5bd}.scope-field{display:grid;grid-template-columns:repeat(2,max-content) minmax(120px,1fr);align-items:center;gap:6px}.scope-field legend{grid-column:1/-1}.scope-field>label{display:flex;align-items:center;gap:3px}.scope-field>input{grid-column:1/-1;width:100%}.schedule-field,.items-field,.audit{grid-column:1/-1}.inspection-options{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:6px;padding:3px 0 8px}.inspection-options label{display:flex;align-items:center;gap:5px;min-height:24px}.schedule-controls{display:flex;align-items:end;gap:12px;flex-wrap:wrap;padding:7px;border-top:1px solid #dedbd4}.schedule-controls .field{min-width:170px}.schedule-note{margin:0;color:#777}.item-type-tabs{display:flex;gap:5px;flex-wrap:wrap;margin:2px 0 7px}.item-type-tabs .selected{background:#ffe39a;border-color:#c3a55c}.item-picker{display:grid;grid-template-columns:minmax(0,1fr) 100px minmax(0,1fr);gap:8px;min-height:200px}.item-picker section{display:flex;flex-direction:column;min-height:180px;border:1px solid #c8c5bd;background:#fff}.item-picker h3{display:flex;justify-content:space-between;margin:0;padding:6px 8px;border-bottom:1px solid #ccc;background:#f0eee8;font-size:12px;font-weight:500}.item-list{flex:1;overflow:auto}.item-list label{display:flex;gap:6px;align-items:center;padding:5px 7px;border-bottom:1px solid #eee}.item-list label:hover{background:#fff2c7}.item-list span{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.item-move{display:flex;flex-direction:column;justify-content:center;gap:8px}.item-move .qci-btn{white-space:normal}.empty{padding:12px;text-align:center;color:#888}.qci-hint{margin:5px 0 0;color:#777;font-size:11px}.audit{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:8px;padding:9px;border:1px solid #b9b5ab;background:#f2f0e9}.audit label{display:flex;flex-direction:column;gap:4px;color:#666;font-size:11px}.audit input{width:100%;background:#ecebe7!important}.qci-dialog>footer{display:flex;justify-content:flex-end;gap:8px;padding:9px 14px;border-top:1px solid #d2cfc7}.qci-confirm{width:min(480px,95vw)}.qci-confirm p{padding:0 14px;line-height:1.65;color:#555}
@media(max-width:900px){.qci-main{grid-template-columns:minmax(0,1fr) 80px}.qci-actions .qci-btn{padding:4px;font-size:11px}.qci-form-grid{grid-template-columns:1fr}.qci-form-grid>*{grid-column:1}.inspection-options{grid-template-columns:repeat(2,minmax(0,1fr))}.audit{grid-template-columns:repeat(2,minmax(0,1fr))}}
@media(max-width:620px){.qci-page{padding:5px}.qci-query label{width:100%;justify-content:space-between}.qci-query input,.qci-query select{flex:1}.qci-main{grid-template-columns:minmax(0,1fr) 65px}.qci-status{gap:8px;flex-wrap:wrap}.item-picker{grid-template-columns:1fr}.item-move{flex-direction:row}.inspection-options{grid-template-columns:1fr 1fr}.audit{grid-template-columns:1fr 1fr}}
</style>
