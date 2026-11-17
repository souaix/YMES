<script setup lang="ts">
import { computed, ref } from 'vue'

type EditorMode = 'add' | 'edit' | 'copy' | null
type TabKey = 'basic' | 'summary' | 'raw' | 'nonnumeric' | 'recipe'
interface TestRecipe { testRecipeNo: string; calculation: string; formula: string; fileField: string }
interface NonNumericField { fieldName: string; dataType: number }
interface CoordInfo { fieldIndex: number; coordAxisType: number }
interface DynamicParameter { testParameterNo: string; columnOrder: number }
interface RawValue { testParameterNo: string; rowOrder: number }
interface EquipmentOption { equipmentNo: string; equipmentType: string; equipmentName: string; hasMatchingFormat?: boolean }
interface ScriptRow {
  equipmentNo: string; fileNameFormat: string; equipmentType: string; equipmentName: string; engineerGroupNo: string
  equipmentClass: string; vendorNo: string; modelNo: string; assetNo: string; capacity: number; loadPort: number
  autoFlag: number | null; eaController: string; eqpRecipe: number | null; maxTime: number | null; fixEqpTime: number
  varEqpTime: number; countEqpUnitQty: number; qcListNo: string; description: string; creator: string; createDate: string
  reviser: string | null; reviseDate: string | null; masterSeparateSign: string; detailCloumnSeparateSign: string
  storeRawData: number; multiWafer: number; batchFormat: number; haveCoordInfo: number; withHeader: number | null
  sendMail: number; isCoordinateY: number
  fileType: string; formatScript: string; rawDataFormatScript: string; dataStamp: number | null
  testRecipes: TestRecipe[]; unNumericFields: NonNumericField[]; coordInfo: CoordInfo[]
  dynamicParameters: DynamicParameter[]; rawValues: RawValue[]
}
interface EditorValue {
  equipmentNo: string; fileNameFormat: string; description: string; allowSaveRowData: boolean; sendMail: boolean
  masterSeparateSign: string; detailCloumnSeparateSign: string; fileType: string; withHeader: boolean
  equipmentNoOrder: number; programNoOrder: number; componentNoOrder: number; testDateOrder: number
  columnsOrder: number; dataOrder: number; lotNoRowOrder: number; componentNoRowOrder: number
  haveCoordInfo: boolean; xCoordFieldIndex: number; yCoordFieldIndex: number; isCoordinateY: boolean
  dynamicParameters: DynamicParameter[]; rawValues: RawValue[]; testRecipes: TestRecipe[]
  unNumericFields: NonNumericField[]; testFileName: string; testFileText: string
}

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const endpoint = 'api/eqp/auto-loader-scripts'
const storageKey = 'ymes-dev-mock:eqp-auto-loader:v1'
const seedRows: ScriptRow[] = [
  {
    equipmentNo: 'EQ-DEV-01', fileNameFormat: '*.csv', equipmentType: 'TESTER', equipmentName: '測試機台（DEV）', engineerGroupNo: 'ENG1-RW-01',
    equipmentClass: '一般設備', vendorNo: 'DEV-VENDOR', modelNo: 'T-100', assetNo: 'ASSET-001', capacity: -1, loadPort: 2, autoFlag: 1,
    eaController: 'DEV-EAP', eqpRecipe: 1, maxTime: 120, fixEqpTime: 0, varEqpTime: 0, countEqpUnitQty: 1, qcListNo: 'QC-DEV',
    description: 'AutoLoader 測試格式（DEV 假資料）', creator: 'DEV001', createDate: '2026-11-01T08:00:00+08:00', reviser: 'DEV001', reviseDate: '2026-11-03T10:30:00+08:00',
    masterSeparateSign: ':', detailCloumnSeparateSign: ',', storeRawData: 0, sendMail: 0, isCoordinateY: 0, multiWafer: 0, batchFormat: 0, haveCoordInfo: 0, withHeader: 1,
    fileType: '單片', formatScript: 'WaferId=>>2\nTestDate=>>4\nEquipmentNo=>>1\nProgramNo=>>3\nColumnsStart=>>1\nDataStart=>>2\nVOLTAGE=>>5',
    rawDataFormatScript: 'VOLTAGE=>>5', dataStamp: 2, testRecipes: [{ testRecipeNo: 'TR-DEV-01', calculation: '', formula: '', fileField: 'VOLTAGE' }],
    unNumericFields: [], coordInfo: [], dynamicParameters: [{ testParameterNo: 'VOLTAGE', columnOrder: 5 }], rawValues: [{ testParameterNo: 'VOLTAGE', rowOrder: 5 }],
  },
  {
    equipmentNo: 'EQ-DEV-02', fileNameFormat: 'REPORT_*.txt', equipmentType: 'PROBER', equipmentName: '探針台（DEV）', engineerGroupNo: 'ENG1-RW-03',
    equipmentClass: '一般設備', vendorNo: 'DEV-VENDOR', modelNo: 'P-200', assetNo: '', capacity: -1, loadPort: 1, autoFlag: 0,
    eaController: '', eqpRecipe: 0, maxTime: null, fixEqpTime: 0, varEqpTime: 0, countEqpUnitQty: 1, qcListNo: '',
    description: '多晶圓格式範例（DEV）', creator: 'DEV001', createDate: '2026-11-02T08:00:00+08:00', reviser: null, reviseDate: null,
    masterSeparateSign: ':', detailCloumnSeparateSign: '|', storeRawData: 1, sendMail: 1, isCoordinateY: 0, multiWafer: 1, batchFormat: 0, haveCoordInfo: 1, withHeader: 1,
    fileType: '多片', formatScript: 'WaferId=>>2\nTestDate=>>4\nEquipmentNo=>>1\nProgramNo=>>3\nColumnsStart=>>1\nDataStart=>>2\nD_ComponentNo=>>6\nX=>>7\nY=>>8',
    rawDataFormatScript: '', dataStamp: 0, testRecipes: [], unNumericFields: [], coordInfo: [{ fieldIndex: 7, coordAxisType: 0 }, { fieldIndex: 8, coordAxisType: 1 }], dynamicParameters: [], rawValues: [],
  },
]
const demoEquipmentOptions = [
  { equipmentNo: 'EQ-DEV-01', equipmentType: 'TESTER', equipmentName: '測試機台（DEV）' },
  { equipmentNo: 'EQ-DEV-02', equipmentType: 'PROBER', equipmentName: '探針台（DEV）' },
  { equipmentNo: 'EQ-DEV-03', equipmentType: 'TESTER', equipmentName: '測試機台二（DEV）' },
  { equipmentNo: 'EQ-DEV-04', equipmentType: 'ASSEMBLY', equipmentName: '組裝設備（DEV）' },
]
const equipmentOptions = ref<EquipmentOption[]>([])
const rows = ref<ScriptRow[]>([])
const filters = ref({ equipmentType: '', equipmentNo: '' })
const queried = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const notice = ref('')
const selectedKeys = ref<string[]>([])
const activeKey = ref('')
const editorMode = ref<EditorMode>(null)
const editor = ref<EditorValue>(blankEditor())
const editorError = ref('')
const editorNotice = ref('')
const activeTab = ref<TabKey>('basic')
const audit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const sourceRow = ref<ScriptRow | null>(null)
const defaultDialog = ref(false)
const defaultPrefix = ref('')
const defaultSelected = ref<string[]>([])
const defaultError = ref('')
const equipmentPickerOpen = ref(false)
const equipmentPickerQuery = ref('')
const equipmentPickerRows = ref<EquipmentOption[]>([])
const testFileInput = ref<HTMLInputElement | null>(null)
const tabs: { key: TabKey; label: string }[] = [
  { key: 'basic', label: '基本資料 / 檔案格式' }, { key: 'summary', label: 'Summary EQC' },
  { key: 'raw', label: 'Original Value' }, { key: 'nonnumeric', label: 'TestFile UnNumeric' }, { key: 'recipe', label: 'Test Recipe Define' },
]
const headerOrderFields: { key: 'equipmentNoOrder' | 'programNoOrder' | 'componentNoOrder' | 'testDateOrder' | 'columnsOrder' | 'dataOrder'; label: string }[] = [
  { key: 'equipmentNoOrder', label: '設備編號' }, { key: 'programNoOrder', label: '程式編號' }, { key: 'componentNoOrder', label: '晶圓 / Component 編號' },
  { key: 'testDateOrder', label: '測試日期' }, { key: 'columnsOrder', label: '欄位起始列' }, { key: 'dataOrder', label: '資料起始列' },
]
const visibleRows = computed(() => rows.value.slice().sort((a, b) => a.equipmentNo.localeCompare(b.equipmentNo) || a.fileNameFormat.localeCompare(b.fileNameFormat)))
const activeRow = computed(() => rows.value.find(row => keyOf(row) === activeKey.value) ?? null)
const allSelected = computed(() => visibleRows.value.length > 0 && visibleRows.value.every(row => selectedKeys.value.includes(keyOf(row))))
const filteredTargets = computed(() => {
  const currentRows = mock.value ? readMock() : rows.value
  return equipmentOptions.value.filter(item => item.equipmentNo.toUpperCase().startsWith(defaultPrefix.value.trim().toUpperCase()) && item.equipmentNo !== sourceRow.value?.equipmentNo && item.hasMatchingFormat !== true && !currentRows.some(row => row.equipmentNo === item.equipmentNo && row.fileNameFormat === sourceRow.value?.fileNameFormat))
})
const filteredEquipmentPickerRows = computed(() => equipmentPickerRows.value.filter(item => `${item.equipmentNo} ${item.equipmentType} ${item.equipmentName}`.toLowerCase().includes(equipmentPickerQuery.value.trim().toLowerCase())))
const editorTitle = computed(() => `設備 AutoLoader 格式檔設定（${editorMode.value === 'add' ? '新增' : editorMode.value === 'copy' ? '複製' : '修改'}）`)

function blankEditor(): EditorValue {
  return { equipmentNo: '', fileNameFormat: '', description: '', allowSaveRowData: false, sendMail: false, masterSeparateSign: ':', detailCloumnSeparateSign: ',',
    fileType: 'single', withHeader: true, equipmentNoOrder: 1, programNoOrder: 3, componentNoOrder: 2, testDateOrder: 4, columnsOrder: 1, dataOrder: 2,
    lotNoRowOrder: 0, componentNoRowOrder: 0, haveCoordInfo: false, xCoordFieldIndex: 0, yCoordFieldIndex: 0, isCoordinateY: false,
    dynamicParameters: [], rawValues: [], testRecipes: [], unNumericFields: [], testFileName: '', testFileText: '' }
}
function keyOf(row: Pick<ScriptRow, 'equipmentNo' | 'fileNameFormat'>): string { return `${row.equipmentNo}\u0000${row.fileNameFormat}` }
function now(): string { return new Date().toISOString() }
function readMock(): ScriptRow[] {
  try { const saved = localStorage.getItem(storageKey); return saved ? JSON.parse(saved) as ScriptRow[] : structuredClone(seedRows) }
  catch { return structuredClone(seedRows) }
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const base = props.apiBaseUrl.replace(/\/+$/, '')
  const response = await fetch(`${base}/${path.replace(/^\/+/, '')}`, { credentials: 'include', ...init, headers: { ...(init.body ? { 'Content-Type': 'application/json' } : {}), ...init.headers } })
  if (!response.ok) { const body = await response.json().catch(() => null) as { message?: string; title?: string } | null; throw new Error(body?.message || body?.title || `API 請求失敗（${response.status}）`) }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function pick(row: ScriptRow): void { activeKey.value = keyOf(row) }
function toggleRow(row: ScriptRow): void { pick(row); const key = keyOf(row); selectedKeys.value = selectedKeys.value.includes(key) ? selectedKeys.value.filter(item => item !== key) : [...selectedKeys.value, key] }
function toggleAll(): void { selectedKeys.value = allSelected.value ? [] : visibleRows.value.map(keyOf) }
async function query(): Promise<void> {
  errorMessage.value = ''; notice.value = ''; busy.value = true
  try {
    if (mock.value) rows.value = readMock().filter(row => (!filters.value.equipmentNo || row.equipmentNo.toUpperCase() === filters.value.equipmentNo.trim().toUpperCase()) && (!filters.value.equipmentType || row.equipmentType.toUpperCase() === filters.value.equipmentType.trim().toUpperCase()))
    else rows.value = await request<ScriptRow[]>(`${endpoint}?${new URLSearchParams({ equipmentNo: filters.value.equipmentNo.trim(), equipmentType: filters.value.equipmentType.trim() })}`)
    selectedKeys.value = []; activeKey.value = ''; queried.value = true
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '查詢失敗' }
  finally { busy.value = false }
}
function parseEditor(row: ScriptRow): EditorValue {
  const map = new Map<string, number>()
  for (const match of row.formatScript.matchAll(/^([^=\r\n]+)=>>(-?\d+)$/gm)) map.set(match[1], Number(match[2]))
  const fileType = row.multiWafer === -1 ? 'batch' : row.batchFormat === 1 ? 'production' : row.multiWafer === 1 ? 'multi' : 'single'
  return { equipmentNo: row.equipmentNo, fileNameFormat: row.fileNameFormat, description: row.description, allowSaveRowData: row.storeRawData === 1,
    masterSeparateSign: row.masterSeparateSign, detailCloumnSeparateSign: row.detailCloumnSeparateSign, fileType, withHeader: row.withHeader === 1,
    equipmentNoOrder: map.get('EquipmentNo') ?? 0, programNoOrder: map.get('ProgramNo') ?? 0, componentNoOrder: map.get('WaferId') ?? 0, testDateOrder: map.get('TestDate') ?? 0,
    columnsOrder: map.get('ColumnsStart') ?? 0, dataOrder: map.get('DataStart') ?? 0, lotNoRowOrder: map.get('D_LotNo') ?? 0, componentNoRowOrder: map.get('D_ComponentNo') ?? 0,
    haveCoordInfo: row.haveCoordInfo === 1, xCoordFieldIndex: row.coordInfo.find(item => item.coordAxisType === 0)?.fieldIndex ?? 0,
    yCoordFieldIndex: row.coordInfo.find(item => item.coordAxisType === 1)?.fieldIndex ?? 0, isCoordinateY: row.isCoordinateY === 1, sendMail: row.sendMail === 1,
    dynamicParameters: structuredClone(row.dynamicParameters), rawValues: structuredClone(row.rawValues), testRecipes: structuredClone(row.testRecipes),
    unNumericFields: structuredClone(row.unNumericFields), testFileName: '', testFileText: '' }
}
function begin(mode: Exclude<EditorMode, null>): void {
  errorMessage.value = ''; editorError.value = ''; editorNotice.value = ''; activeTab.value = 'basic';
  sourceRow.value = activeRow.value ? structuredClone(activeRow.value) : null
  if (mode === 'add') { editor.value = blankEditor(); audit.value = { creator: props.currentUserNo || 'DEV001', createDate: now(), reviser: '', reviseDate: '' } }
  else if (sourceRow.value) {
    editor.value = parseEditor(sourceRow.value)
    audit.value = { creator: sourceRow.value.creator, createDate: sourceRow.value.createDate, reviser: sourceRow.value.reviser || '', reviseDate: sourceRow.value.reviseDate || '' }
    if (mode === 'copy') { editor.value.equipmentNo = ''; editor.value.fileNameFormat = ''; audit.value = { creator: props.currentUserNo || 'DEV001', createDate: now(), reviser: '', reviseDate: '' } }
  } else { errorMessage.value = '請先選取一筆設備格式檔'; return }
  editorMode.value = mode
}
function closeEditor(): void { editorMode.value = null; sourceRow.value = null }
async function openEquipmentPicker(): Promise<void> {
  equipmentPickerQuery.value = ''; errorMessage.value = ''
  try {
    equipmentPickerRows.value = mock.value ? structuredClone(demoEquipmentOptions) : await request<EquipmentOption[]>('api/eqp/auto-loader-scripts/options/equipment')
    equipmentOptions.value = structuredClone(equipmentPickerRows.value); equipmentPickerOpen.value = true
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '載入設備清單失敗' }
}
function chooseEquipment(equipmentNo: string): void { editor.value.equipmentNo = equipmentNo; equipmentPickerOpen.value = false }
function makeScript(): { formatScript: string; rawDataFormatScript: string; multiWafer: number; batchFormat: number; fileType: string; coordInfo: CoordInfo[] } {
  const format: string[] = [
    `WaferId=>>${editor.value.componentNoOrder}`, `TestDate=>>${editor.value.testDateOrder}`, `EquipmentNo=>>${editor.value.equipmentNoOrder}`,
    `ProgramNo=>>${editor.value.programNoOrder}`, `ColumnsStart=>>${editor.value.columnsOrder}`, `DataStart=>>${editor.value.dataOrder}`,
  ]
  if (editor.value.fileType === 'multi') format.push(`D_ComponentNo=>>${editor.value.componentNoRowOrder}`)
  if (editor.value.fileType === 'production') format.push('D_ComponentNo=>>0', `D_LotNo=>>${editor.value.lotNoRowOrder}`)
  for (const item of editor.value.dynamicParameters.filter(item => item.testParameterNo.trim())) format.push(`${item.testParameterNo.trim()}=>>${item.columnOrder}`)
  for (const recipe of editor.value.testRecipes.filter(item => item.testRecipeNo.trim())) format.push(`${recipe.testRecipeNo.trim()}=>>${editor.value.fileType === 'multi' ? recipe.fileField : `${recipe.testRecipeNo.trim()}:${recipe.formula}`}`)
  const raw = editor.value.rawValues.filter(item => item.testParameterNo.trim()).map(item => `${item.testParameterNo.trim()}=>>${item.rowOrder}`).join('\n')
  const multiWafer = editor.value.fileType === 'multi' ? 1 : editor.value.fileType === 'batch' ? -1 : 0
  const batchFormat = editor.value.fileType === 'production' ? 1 : 0
  const coordInfo: CoordInfo[] = editor.value.haveCoordInfo ? [{ fieldIndex: editor.value.xCoordFieldIndex, coordAxisType: 0 }, { fieldIndex: editor.value.yCoordFieldIndex, coordAxisType: 1 }] : []
  if (editor.value.haveCoordInfo) format.push(`X=>>${editor.value.xCoordFieldIndex}`, `Y=>>${editor.value.yCoordFieldIndex}`)
  return { formatScript: format.join('\n'), rawDataFormatScript: raw, multiWafer, batchFormat, fileType: editor.value.fileType, coordInfo }
}
function validate(): string {
  if (!editor.value.equipmentNo.trim()) return '設備編號為必填欄位。'
  if (!editor.value.fileNameFormat.trim()) return '檔名格式為必填欄位。'
  if (editor.value.equipmentNo.length > 50 || editor.value.fileNameFormat.length > 100) return '設備編號最多 50 字，檔名格式最多 100 字。'
  if (editor.value.description.length > 255) return '說明最多 255 字。'
  if (editor.value.withHeader && [editor.value.equipmentNoOrder, editor.value.programNoOrder, editor.value.componentNoOrder, editor.value.testDateOrder].some(value => value <= 0)) return '啟用表頭時，EquipmentNo、ProgramNo、ComponentNo、TestDate 欄位順序都必須大於 0。'
  if (editor.value.haveCoordInfo && (editor.value.xCoordFieldIndex <= 0 || editor.value.yCoordFieldIndex <= 0)) return '啟用座標資訊時，X/Y 座標欄位順序必須大於 0。'
  if (editor.value.allowSaveRowData && !editor.value.rawValues.some(item => item.testParameterNo.trim())) return '啟用儲存原始值時，至少要設定一筆 Original Value。'
  if ([...editor.value.dynamicParameters.map(item => item.testParameterNo), ...editor.value.testRecipes.map(item => item.testRecipeNo)].some(value => value.trim().length > 50)) return '測試參數編號最多 50 字。'
  return ''
}
function buildRow(existing?: ScriptRow): ScriptRow {
  const script = makeScript(); const stamp = existing?.dataStamp ?? 0
  const fileTypeLabel = script.fileType === 'multi' ? '多片' : script.fileType === 'production' ? '設備生產資料' : script.fileType === 'batch' ? '單批次' : '單片'
  return { equipmentNo: editor.value.equipmentNo.trim().toUpperCase(), fileNameFormat: editor.value.fileNameFormat.trim(), equipmentType: existing?.equipmentType || '', equipmentName: existing?.equipmentName || '',
    engineerGroupNo: existing?.engineerGroupNo || '', equipmentClass: existing?.equipmentClass || '', vendorNo: existing?.vendorNo || '', modelNo: existing?.modelNo || '', assetNo: existing?.assetNo || '',
    capacity: existing?.capacity ?? -1, loadPort: existing?.loadPort ?? 0, autoFlag: existing?.autoFlag ?? null, eaController: existing?.eaController || '', eqpRecipe: existing?.eqpRecipe ?? null,
    maxTime: existing?.maxTime ?? null, fixEqpTime: existing?.fixEqpTime ?? 0, varEqpTime: existing?.varEqpTime ?? 0, countEqpUnitQty: existing?.countEqpUnitQty ?? 1, qcListNo: existing?.qcListNo || '',
    description: editor.value.description.trim(), creator: existing?.creator || props.currentUserNo || 'DEV001', createDate: existing?.createDate || now(), reviser: existing ? props.currentUserNo || 'DEV001' : null,
    reviseDate: existing ? now() : null, masterSeparateSign: editor.value.masterSeparateSign, detailCloumnSeparateSign: editor.value.detailCloumnSeparateSign,
    storeRawData: editor.value.allowSaveRowData ? 1 : 0, sendMail: editor.value.sendMail ? 1 : 0, isCoordinateY: editor.value.isCoordinateY ? 1 : 0, multiWafer: script.multiWafer, batchFormat: script.batchFormat,
    haveCoordInfo: editor.value.haveCoordInfo ? 1 : 0, withHeader: editor.value.withHeader ? 1 : 0, fileType: fileTypeLabel,
    formatScript: script.formatScript, rawDataFormatScript: script.rawDataFormatScript, dataStamp: stamp + 1, testRecipes: structuredClone(editor.value.testRecipes),
    unNumericFields: structuredClone(editor.value.unNumericFields), coordInfo: script.coordInfo, dynamicParameters: structuredClone(editor.value.dynamicParameters), rawValues: structuredClone(editor.value.rawValues) }
}
function toApiPayload(row: ScriptRow): Record<string, unknown> {
  return {
    equipmentNo: row.equipmentNo, fileNameFormat: row.fileNameFormat, description: row.description,
    storeRawData: row.storeRawData, sendMail: row.sendMail, masterSeparateSign: row.masterSeparateSign,
    detailCloumnSeparateSign: row.detailCloumnSeparateSign, multiWafer: row.multiWafer, batchFormat: row.batchFormat,
    haveCoordInfo: row.haveCoordInfo, withHeader: row.withHeader, isCoordinateY: row.isCoordinateY,
    formatScript: row.formatScript, rawDataFormatScript: row.rawDataFormatScript,
    testRecipes: row.testRecipes.map(({ testRecipeNo, calculation, formula, fileField }) => ({ testRecipeNo, calculation, formula, fileField })),
    unNumericFields: row.unNumericFields.map(({ fieldName, dataType }) => ({ fieldName, dataType })),
    coordInfo: row.coordInfo.map(({ fieldIndex, coordAxisType }) => ({ fieldIndex, coordAxisType })),
  }
}
async function save(): Promise<void> {
  editorError.value = validate(); editorNotice.value = ''; if (editorError.value) return
  const payload = buildRow(editorMode.value === 'edit' ? sourceRow.value || undefined : undefined)
  busy.value = true
  try {
    if (mock.value) {
      const all = readMock(); const oldKey = sourceRow.value ? keyOf(sourceRow.value) : ''
      if (editorMode.value === 'edit') { const index = all.findIndex(row => keyOf(row) === oldKey); if (index < 0) throw new Error('資料已不存在，請重新查詢。'); all[index] = payload }
      else { if (all.some(row => keyOf(row) === keyOf(payload))) throw new Error('設備編號與檔名格式已存在。'); all.push(payload) }
      rows.value = all.filter(row => (!filters.value.equipmentNo || row.equipmentNo.toLowerCase().includes(filters.value.equipmentNo.trim().toLowerCase())) && (!filters.value.equipmentType || row.equipmentType.toLowerCase() === filters.value.equipmentType.trim().toLowerCase()))
      localStorage.setItem(storageKey, JSON.stringify(all))
    } else if (editorMode.value === 'edit' && sourceRow.value) {
      await request(`${endpoint}/${encodeURIComponent(sourceRow.value.equipmentNo)}/${encodeURIComponent(sourceRow.value.fileNameFormat)}?dataStamp=${sourceRow.value.dataStamp ?? 0}`, { method: 'PUT', body: JSON.stringify(toApiPayload(payload)) }); await query()
    } else { await request(endpoint, { method: 'POST', body: JSON.stringify(toApiPayload(payload)) }); await query() }
    activeKey.value = keyOf(payload); selectedKeys.value = [activeKey.value]; notice.value = '儲存完成。'; closeEditor()
  } catch (error) { editorError.value = error instanceof Error ? error.message : '儲存失敗' }
  finally { busy.value = false }
}
async function removeSelected(): Promise<void> {
  const selected = visibleRows.value.filter(row => selectedKeys.value.includes(keyOf(row)))
  if (!selected.length) { errorMessage.value = '請先勾選要刪除的資料。'; return }
  if (!window.confirm(`確定刪除選取的 ${selected.length} 筆設備 AutoLoader 格式檔？`)) return
  busy.value = true; errorMessage.value = ''; let deleted = 0
  try {
    for (const row of selected) {
      if (mock.value) { const all = readMock().filter(item => keyOf(item) !== keyOf(row)); localStorage.setItem(storageKey, JSON.stringify(all)); rows.value = rows.value.filter(item => keyOf(item) !== keyOf(row)) }
      else await request(`${endpoint}/${encodeURIComponent(row.equipmentNo)}/${encodeURIComponent(row.fileNameFormat)}?dataStamp=${row.dataStamp ?? 0}`, { method: 'DELETE' })
      deleted++
    }
    if (!mock.value) await query(); else selectedKeys.value = []
    notice.value = `已刪除 ${deleted} 筆資料。`; activeKey.value = ''
  } catch (error) { errorMessage.value = `${error instanceof Error ? error.message : '刪除失敗'}（已完成 ${deleted}/${selected.length} 筆）` }
  finally { busy.value = false }
}
async function openDefault(): Promise<void> {
  if (!activeRow.value) { errorMessage.value = '請先選取一筆來源設備格式檔。'; return }
  sourceRow.value = structuredClone(activeRow.value); defaultPrefix.value = ''; defaultSelected.value = []; defaultError.value = ''
  try {
    if (mock.value) equipmentOptions.value = structuredClone(demoEquipmentOptions).map(item => ({ ...item, hasMatchingFormat: readMock().some(row => row.equipmentNo === item.equipmentNo && row.fileNameFormat === sourceRow.value?.fileNameFormat) }))
    else equipmentOptions.value = await request<EquipmentOption[]>(`${endpoint}/options/equipment?${new URLSearchParams({ fileNameFormat: sourceRow.value.fileNameFormat })}`)
    defaultDialog.value = true
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '載入設備清單失敗' }
}
function toggleDefaultAll(): void { defaultSelected.value = defaultSelected.value.length === filteredTargets.value.length ? [] : filteredTargets.value.map(item => item.equipmentNo) }
async function applyDefault(): Promise<void> {
  if (!sourceRow.value || !defaultSelected.value.length) { defaultError.value = '請選擇要套用的設備。'; return }
  busy.value = true; errorMessage.value = ''; const src = sourceRow.value; let count = 0
  try {
    if (mock.value) {
      const all = readMock()
      for (const equipmentNo of defaultSelected.value) {
        if (all.some(row => row.equipmentNo === equipmentNo && row.fileNameFormat === src.fileNameFormat)) continue
        const target = equipmentOptions.value.find(item => item.equipmentNo === equipmentNo); if (!target) continue
        const clone = structuredClone(src); clone.equipmentNo = equipmentNo; clone.equipmentType = target.equipmentType; clone.equipmentName = target.equipmentName
        clone.creator = props.currentUserNo || 'DEV001'; clone.createDate = now(); clone.reviser = null; clone.reviseDate = null; clone.dataStamp = 0
        all.push(clone); count++
      }
      localStorage.setItem(storageKey, JSON.stringify(all)); rows.value = all.filter(row => (!filters.value.equipmentNo || row.equipmentNo.toLowerCase().includes(filters.value.equipmentNo.trim().toLowerCase())) && (!filters.value.equipmentType || row.equipmentType.toLowerCase() === filters.value.equipmentType.trim().toLowerCase()))
    } else {
      const result = await request<{ createdEquipmentNos?: string[]; existingEquipmentNos?: string[]; invalidEquipmentNos?: string[] }>(`${endpoint}/${encodeURIComponent(src.equipmentNo)}/${encodeURIComponent(src.fileNameFormat)}/default?dataStamp=${src.dataStamp ?? 0}`, { method: 'POST', body: JSON.stringify({ equipmentNos: defaultSelected.value }) })
      count = result.createdEquipmentNos?.length ?? defaultSelected.value.length
      await query()
      const skipped = (result.existingEquipmentNos?.length || 0) + (result.invalidEquipmentNos?.length || 0)
      defaultDialog.value = false
      notice.value = `已套用至 ${count} 台設備。${skipped ? `另有 ${skipped} 台因已存在或不符合條件而略過。` : ''}`
      return
    }
    defaultDialog.value = false; notice.value = `已套用至 ${count} 台設備。`
  } catch (error) { defaultError.value = `${error instanceof Error ? error.message : '預設套用失敗'}（已完成 ${count} 台）` }
  finally { busy.value = false }
}
async function loadTestFile(event: Event): Promise<void> {
  const file = (event.target as HTMLInputElement).files?.[0]; if (!file) return
  editor.value.testFileName = file.name
  try { editor.value.testFileText = (await file.text()).split(/\r?\n/).slice(0, 80).join('\n'); editorNotice.value = '只在瀏覽器顯示前 80 行預覽；不會上傳檔案。' }
  catch { editorError.value = '無法讀取測試檔案。' }
}
function addParameter(): void { editor.value.dynamicParameters.push({ testParameterNo: '', columnOrder: 1 }) }
function addRawValue(): void { editor.value.rawValues.push({ testParameterNo: '', rowOrder: 1 }) }
function addRecipe(): void { editor.value.testRecipes.push({ testRecipeNo: '', calculation: '', formula: '', fileField: '' }) }
function addUnNumeric(): void { editor.value.unNumericFields.push({ fieldName: '', dataType: 0 }) }
</script>

<template>
  <main class="autoloader-page">
    <header class="page-header"><div><h1>設備 AutoLoader 格式檔設定</h1><p>依設備編號及檔名格式維護設備測試檔解析規則。</p></div><span v-if="mock" class="dev-tag">DEV 假資料</span></header>
    <section class="filter-panel">
      <label class="filter-item"><span>設備類別</span><input v-model.trim="filters.equipmentType" maxlength="50" @blur="filters.equipmentType = filters.equipmentType.toUpperCase()" @keyup.enter="query"></label>
      <label class="filter-item"><span>設備編號</span><input v-model.trim="filters.equipmentNo" maxlength="50" @blur="filters.equipmentNo = filters.equipmentNo.toUpperCase()" @keyup.enter="query"></label>
      <button class="query-button" type="button" :disabled="busy" @click="query">{{ busy ? '處理中…' : '查詢' }}</button>
    </section>
    <section class="list-panel">
      <div class="list-toolbar"><div class="toolbar-actions"><button class="primary-button" type="button" :disabled="busy" @click="begin('add')">新增(A)</button><button type="button" :disabled="busy || !activeRow" @click="begin('edit')">修改(E)</button><button type="button" :disabled="busy || !activeRow" @click="begin('copy')">複製(C)</button><button type="button" :disabled="busy || !selectedKeys.length" @click="removeSelected">刪除(D)</button><button type="button" :disabled="busy || !activeRow" @click="openDefault">預設</button><button type="button" @click="emit('close')">關閉</button></div><span>{{ queried ? `資料總筆數 ${visibleRows.length}` : '請輸入查詢條件後按「查詢」' }}</span></div>
      <div v-if="errorMessage" class="alert error-alert">{{ errorMessage }}</div><div v-if="notice" class="alert success-alert">{{ notice }}</div>
      <div class="table-wrap"><table class="data-grid"><thead><tr><th class="check-column"><input type="checkbox" :checked="allSelected" @change="toggleAll"></th><th v-for="col in ['設備編號','檔名格式','設備類別','設備名稱','管理群組','設備分類','供應商編號','設備型號','財產編號','批量限制','批數限制','批量單位','自動設備','EAP Controller','設備配方','最大時間','固定機時','變動機時','機時單位批量','點檢表編號','說明','建立人','建立日期','修改人','修改日期','主檔分隔符','明細分隔符','儲存原始值','多晶圓','批次格式','座標資訊','表頭','檔案類型']" :key="col">{{ col }}</th></tr></thead>
        <tbody v-if="queried"><tr v-for="row in visibleRows" :key="keyOf(row)" :class="{ active: activeKey === keyOf(row) }" @click="pick(row)"><td class="check-column" @click.stop><input type="checkbox" :checked="selectedKeys.includes(keyOf(row))" @change="toggleRow(row)"></td><td v-for="(value,valueIndex) in [row.equipmentNo,row.fileNameFormat,row.equipmentType,row.equipmentName,row.engineerGroupNo,row.equipmentClass,row.vendorNo,row.modelNo,row.assetNo,row.capacity,row.loadPort,row.countEqpUnitQty,row.autoFlag === null ? '' : row.autoFlag === 1 ? '是' : '否',row.eaController,row.eqpRecipe === null ? '' : row.eqpRecipe,row.maxTime,row.fixEqpTime,row.varEqpTime,row.countEqpUnitQty,row.qcListNo,row.description,row.creator,row.createDate,row.reviser,row.reviseDate,row.masterSeparateSign,row.detailCloumnSeparateSign,row.storeRawData === 1 ? '是' : '否',row.multiWafer,row.batchFormat,row.haveCoordInfo === 1 ? '是' : '否',row.withHeader === 1 ? '是' : '否',row.fileType]" :key="`${keyOf(row)}-${valueIndex}`">{{ value ?? '' }}</td></tr><tr v-if="!visibleRows.length"><td colspan="34" class="empty-state">查無資料</td></tr></tbody><tbody v-else><tr><td colspan="34" class="empty-state">尚未查詢</td></tr></tbody></table></div>
    </section>
    <section class="script-grid"><article class="script-preview-panel"><h2>格式檔內容</h2><pre v-if="activeRow">{{ activeRow.formatScript || '（空白）' }}</pre><p v-else>選取資料列後顯示。</p></article><article class="script-preview-panel"><h2>原始資料格式檔</h2><pre v-if="activeRow">{{ activeRow.rawDataFormatScript || '（空白）' }}</pre><p v-else>選取資料列後顯示。</p></article></section>

    <div v-if="editorMode" class="modal-backdrop" @click.self="closeEditor"><section class="editor-dialog" role="dialog" aria-modal="true"><header class="dialog-header"><h2>{{ editorTitle }}</h2><button class="icon-button" type="button" aria-label="關閉" @click="closeEditor">×</button></header>
      <nav class="tabs"><button v-for="tab in tabs" :key="tab.key" type="button" :class="{ selected: activeTab === tab.key }" @click="activeTab = tab.key">{{ tab.label }}</button></nav>
      <div class="editor-scroll">
        <div v-if="editorError" class="alert error-alert">{{ editorError }}</div><div v-if="editorNotice" class="alert success-alert">{{ editorNotice }}</div>
        <template v-if="activeTab === 'basic'"><section class="form-section"><h3>設備 AutoLoader 主檔資料</h3><div class="form-grid">
          <label><span>設備編號 <b>*</b></span><div class="input-with-button"><input v-model.trim="editor.equipmentNo" maxlength="50" :readonly="editorMode === 'edit'"><button type="button" class="lookup-button" title="選擇設備" @click="openEquipmentPicker">…</button></div></label>
          <label><span>檔名格式 <b>*</b></span><input v-model.trim="editor.fileNameFormat" maxlength="100" placeholder="例如 *.csv"></label>
          <label class="wide-field"><span>說明</span><textarea v-model="editor.description" maxlength="255" rows="2"></textarea></label>
          <fieldset class="radio-field"><legend>檔案類型</legend><label><input v-model="editor.fileType" type="radio" value="single">單片</label><label><input v-model="editor.fileType" type="radio" value="multi">多片</label><label><input v-model="editor.fileType" type="radio" value="production">設備生產資料</label><label><input v-model="editor.fileType" type="radio" value="batch">單批次</label></fieldset>
          <label class="check-label"><input v-model="editor.sendMail" type="checkbox">允許發送郵件</label><label class="check-label"><input v-model="editor.allowSaveRowData" type="checkbox">儲存原始資料</label>
          <label><span>主檔分隔符號</span><input v-model="editor.masterSeparateSign" maxlength="10"></label><label><span>明細欄位分隔符號</span><input v-model="editor.detailCloumnSeparateSign" maxlength="10"></label>
        </div></section>
        <div class="two-column-layout"><section class="form-section"><h3>表頭欄位順序</h3><label class="check-label"><input v-model="editor.withHeader" type="checkbox">檔案包含表頭</label><div class="order-grid"><label v-for="item in headerOrderFields" :key="item.key"><span>{{ item.label }}</span><input v-model.number="editor[item.key]" type="number" min="0"></label></div></section>
          <section class="form-section"><h3>明細欄位與座標</h3><div class="order-grid"><label v-if="editor.fileType === 'production'"><span>Lot No 列順序</span><input v-model.number="editor.lotNoRowOrder" type="number" min="0"></label><label v-if="editor.fileType === 'multi' || editor.fileType === 'production'"><span>Component No 列順序</span><input v-model.number="editor.componentNoRowOrder" type="number" min="0"></label></div><label class="check-label"><input v-model="editor.haveCoordInfo" type="checkbox">包含座標資訊</label><div v-if="editor.haveCoordInfo" class="order-grid"><label><span>X 座標欄位順序</span><input v-model.number="editor.xCoordFieldIndex" type="number" min="1"></label><label><span>Y 座標欄位順序</span><input v-model.number="editor.yCoordFieldIndex" type="number" min="1"></label><label class="check-label"><input v-model="editor.isCoordinateY" type="checkbox">IC Ingot 座標以 Y 軸判斷</label></div><p class="help-text">IC Ingot 例外欄位保留；正式選項依設備類別規則驗證。</p></section></div>
          <section class="form-section"><div class="section-heading"><h3>測試檔案預覽</h3><button type="button" @click="testFileInput?.click()">選擇測試檔</button><input ref="testFileInput" class="hidden-file" type="file" accept=".txt,.csv,.dat" @change="loadTestFile"></div><p v-if="editor.testFileName" class="file-status">{{ editor.testFileName }} — 僅本機預覽，不會上傳。</p><pre v-if="editor.testFileText" class="file-preview">{{ editor.testFileText }}</pre><p v-else class="help-text">上傳或檢視來源測試檔並預覽 Header / Body；瀏覽器版本先提供文字預覽。</p></section></template>
        <template v-else-if="activeTab === 'summary'"><section class="form-section"><div class="section-heading"><div><h3>Summary EQC / 動態測試參數</h3><p class="help-text">參數需存在於核准的 Test Recipe；公式與測試檔欄位轉換規則由 API 重新驗證。</p></div><button type="button" @click="addParameter">新增參數</button></div><div class="parameter-table-wrap"><table class="parameter-table"><thead><tr><th>Test Parameter No</th><th>欄位順序</th><th>操作</th></tr></thead><tbody><tr v-for="(item,index) in editor.dynamicParameters" :key="index"><td><input v-model.trim="item.testParameterNo" maxlength="50"></td><td><input v-model.number="item.columnOrder" type="number" min="0"></td><td><button type="button" class="text-button danger-text" @click="editor.dynamicParameters.splice(index,1)">移除</button></td></tr><tr v-if="!editor.dynamicParameters.length"><td colspan="3" class="empty-state">尚未設定</td></tr></tbody></table></div></section></template>
        <template v-else-if="activeTab === 'raw'"><section class="form-section"><div class="section-heading"><div><h3>Original Value 原始值對照</h3><p class="help-text">啟用儲存原始資料時至少需設定一個原始值欄位。</p></div><button type="button" @click="addRawValue">新增原始值</button></div><div class="parameter-table-wrap"><table class="parameter-table"><thead><tr><th>Test Parameter No</th><th>資料列順序</th><th>操作</th></tr></thead><tbody><tr v-for="(item,index) in editor.rawValues" :key="index"><td><input v-model.trim="item.testParameterNo" maxlength="50"></td><td><input v-model.number="item.rowOrder" type="number" min="0"></td><td><button type="button" class="text-button danger-text" @click="editor.rawValues.splice(index,1)">移除</button></td></tr><tr v-if="!editor.rawValues.length"><td colspan="3" class="empty-state">尚未設定</td></tr></tbody></table></div></section></template>
        <template v-else-if="activeTab === 'nonnumeric'"><section class="form-section"><div class="section-heading"><div><h3>TestFile UnNumeric 欄位</h3><p class="help-text">設定非數值欄位型別；正式允許值須由 API 使用來源 ValueList 驗證。</p></div><button type="button" @click="addUnNumeric">新增欄位</button></div><div class="parameter-table-wrap"><table class="parameter-table"><thead><tr><th>欄位名稱</th><th>資料型別代碼</th><th>操作</th></tr></thead><tbody><tr v-for="(item,index) in editor.unNumericFields" :key="index"><td><input v-model.trim="item.fieldName" maxlength="50"></td><td><select v-model.number="item.dataType"><option :value="0">0</option><option :value="1">1</option></select></td><td><button type="button" class="text-button danger-text" @click="editor.unNumericFields.splice(index,1)">移除</button></td></tr><tr v-if="!editor.unNumericFields.length"><td colspan="3" class="empty-state">尚未設定</td></tr></tbody></table></div></section></template>
        <template v-else><section class="form-section"><div class="section-heading"><div><h3>Test Recipe Define 測試配方定義</h3><p class="help-text">Recipe/Formula 與檔案欄位規則須經後端依設備類別及測試配方核准狀態驗證。</p></div><button type="button" @click="addRecipe">新增配方</button></div><div class="parameter-table-wrap"><table class="parameter-table"><thead><tr><th>Test Recipe No</th><th>Calculation</th><th>Formula</th><th>File Field</th><th>操作</th></tr></thead><tbody><tr v-for="(item,index) in editor.testRecipes" :key="index"><td><input v-model.trim="item.testRecipeNo" maxlength="50"></td><td><input v-model.trim="item.calculation" maxlength="100"></td><td><input v-model.trim="item.formula" maxlength="50"></td><td><input v-model.trim="item.fileField" maxlength="50"></td><td><button type="button" class="text-button danger-text" @click="editor.testRecipes.splice(index,1)">移除</button></td></tr><tr v-if="!editor.testRecipes.length"><td colspan="5" class="empty-state">尚未設定</td></tr></tbody></table></div></section></template>
        <section class="audit-block"><span>建立人</span><input :value="audit.creator" readonly><span>建立日期</span><input :value="audit.createDate" readonly><span>修改人</span><input :value="audit.reviser" readonly><span>修改日期</span><input :value="audit.reviseDate" readonly></section>
      </div><footer class="dialog-footer"><button class="primary-button" type="button" :disabled="busy" @click="save">確定</button><button type="button" @click="closeEditor">關閉</button></footer>
    </section></div>

    <div v-if="defaultDialog" class="modal-backdrop" @click.self="defaultDialog = false"><section class="picker-dialog" role="dialog" aria-modal="true"><header class="dialog-header"><h2>套用設備 AutoLoader 格式檔</h2><button class="icon-button" type="button" @click="defaultDialog = false">×</button></header><div class="picker-content"><p>來源設備：{{ sourceRow?.equipmentNo }} / {{ sourceRow?.fileNameFormat }}</p><label class="filter-item"><span>設備編號前綴</span><input v-model.trim="defaultPrefix" maxlength="50" placeholder="輸入設備編號前綴"></label><label class="check-label"><input type="checkbox" :checked="defaultSelected.length === filteredTargets.length && filteredTargets.length > 0" @change="toggleDefaultAll">全選</label><div class="target-list"><label v-for="target in filteredTargets" :key="target.equipmentNo" class="target-row"><input v-model="defaultSelected" type="checkbox" :value="target.equipmentNo"><span>{{ target.equipmentNo }} — {{ target.equipmentType }} — {{ target.equipmentName }}</span></label><p v-if="!filteredTargets.length" class="empty-state">沒有符合條件且尚未設定相同檔名格式的設備。</p></div><div v-if="defaultError" class="alert error-alert">{{ defaultError }}</div><p class="help-text">只將解析設定複製到所選設備；既有相同設備與檔名格式不會覆蓋。</p></div><footer class="dialog-footer"><button class="primary-button" type="button" :disabled="busy || !defaultSelected.length" @click="applyDefault">套用</button><button type="button" @click="defaultDialog = false">取消</button></footer></section></div>
    <div v-if="equipmentPickerOpen" class="modal-backdrop picker-backdrop" @click.self="equipmentPickerOpen = false"><section class="picker-dialog" role="dialog" aria-modal="true"><header class="dialog-header"><h2>選擇設備</h2><button class="icon-button" type="button" @click="equipmentPickerOpen = false">×</button></header><div class="picker-content"><label class="filter-item"><span>設備編號 / 類別</span><input v-model.trim="equipmentPickerQuery" maxlength="100" placeholder="輸入編號、類別或名稱"></label><div class="target-list"><button v-for="item in filteredEquipmentPickerRows" :key="item.equipmentNo" type="button" class="picker-row" @click="chooseEquipment(item.equipmentNo)">{{ item.equipmentNo }} — {{ item.equipmentType }} — {{ item.equipmentName }}</button><p v-if="!filteredEquipmentPickerRows.length" class="empty-state">沒有符合設備。</p></div></div><footer class="dialog-footer"><button type="button" @click="equipmentPickerOpen = false">關閉</button></footer></section></div>
  </main>
</template>

<style scoped>
.autoloader-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:9px;padding:12px;background:#f7f6f2;color:#292823;font-size:13px}.page-header{display:flex;align-items:center;justify-content:space-between;border-bottom:1px solid #d9d6ce;padding:0 2px 8px}.page-header h1{margin:0;font-size:20px;font-weight:600}.page-header p{margin:4px 0 0;color:#79766f}.dev-tag{border:1px solid #d8aa3c;border-radius:4px;padding:4px 8px;background:#fff1c2;color:#745300;font-size:12px}.filter-panel,.list-panel,.script-preview-panel{border:1px solid #d2d0ca;background:#fff}.filter-panel{display:flex;align-items:end;gap:12px;padding:9px}.filter-item{display:grid;gap:4px;min-width:230px}.filter-item>span,.form-section label>span,.audit-block span{font-weight:600;color:#55524b}.autoloader-page input:not([type=checkbox]):not([type=radio]),.autoloader-page textarea,.autoloader-page select{box-sizing:border-box;min-height:30px;border:1px solid #aaa79f;border-radius:2px;padding:5px 7px;background:#fff0b4;color:#25241f;font:inherit}.filter-item input{width:100%}.autoloader-page input:focus,.autoloader-page textarea:focus,.autoloader-page select:focus{outline:2px solid #e3bf57;outline-offset:1px}.autoloader-page button{border:1px solid #bcb9b2;border-radius:3px;background:linear-gradient(#fff,#f1f0ed);color:#292823;padding:6px 11px;font:inherit;cursor:pointer;white-space:nowrap}.autoloader-page button:hover:not(:disabled){background:#ece9e0}.autoloader-page button:disabled{opacity:.52;cursor:not-allowed}.autoloader-page .primary-button{border-color:#df9b28;background:#f8a72a;color:#fff;font-weight:600}.query-button{height:30px;min-width:72px}.list-panel{min-height:240px;display:flex;flex-direction:column;flex:1}.list-toolbar{display:flex;justify-content:space-between;align-items:center;gap:8px;padding:6px;border-bottom:1px solid #ddd}.toolbar-actions{display:flex;gap:5px}.list-toolbar>span{color:#777;white-space:nowrap}.table-wrap{overflow:auto;flex:1;min-height:160px}.data-grid{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}.data-grid th{position:sticky;top:0;background:#54a8df;color:white;text-align:left;padding:7px 8px;border:1px solid #8ec5e6;font-weight:600}.data-grid td{padding:6px 8px;border:1px solid #d3d0c9;max-width:210px;overflow:hidden;text-overflow:ellipsis}.data-grid tbody tr{cursor:pointer}.data-grid tbody tr:hover{background:#fff8df}.data-grid tbody tr.active{background:#ffe69a}.check-column{width:30px;text-align:center}.check-column input,.check-label input,.radio-field input,.target-row input{accent-color:#dfa52d}.empty-state{text-align:center!important;color:#888!important;padding:18px!important;cursor:default!important}.script-grid{display:grid;grid-template-columns:1fr 1fr;gap:9px}.script-preview-panel{min-height:110px;display:flex;flex-direction:column}.script-preview-panel h2{font-size:14px;margin:0;padding:7px 10px;border-bottom:1px solid #dedbd5}.script-preview-panel pre{margin:0;padding:8px 10px;overflow:auto;max-height:150px;background:#faf9f6;color:#39372f;font:12px/1.45 Consolas,monospace;white-space:pre-wrap}.script-preview-panel p{margin:0;padding:12px;color:#888}.alert{margin:7px;padding:8px 10px;border-radius:3px}.error-alert{color:#8b2721;background:#fff0ef;border:1px solid #e7b0ab}.success-alert{color:#315a2b;background:#eff8eb;border:1px solid #c0d9b8}.modal-backdrop{position:fixed;inset:0;z-index:50;display:grid;place-items:center;padding:18px;background:#0006}.editor-dialog,.picker-dialog{display:flex;flex-direction:column;width:min(1160px,97vw);max-height:95vh;background:#f9f8f4;border:1px solid #aaa69d;box-shadow:0 12px 36px #0004}.dialog-header{display:flex;align-items:center;justify-content:space-between;padding:8px 13px;border-bottom:1px solid #d1cec6;background:#f5f3ed}.dialog-header h2{margin:0;font-size:18px}.icon-button{border:0!important;background:transparent!important;font-size:23px!important;padding:0 6px!important}.tabs{display:flex;gap:3px;padding:6px 10px 0;border-bottom:1px solid #d1cec6;background:#f1efe9;overflow:auto}.tabs button{border-radius:3px 3px 0 0}.tabs button.selected{background:#fff0b4;border-bottom-color:#d5a32d}.editor-scroll{overflow:auto;padding:10px 13px}.form-section{border:1px solid #cbc8c0;background:white;padding:10px;margin-bottom:9px}.form-section>h3,.form-section .section-heading h3{margin:0 0 8px;font-size:15px}.form-grid{display:grid;grid-template-columns:repeat(3,minmax(190px,1fr));gap:9px 13px;align-items:start}.form-grid>label,.wide-field{display:grid;gap:4px}.wide-field{grid-column:span 2}.form-grid b{color:#9a5d00}.form-grid textarea{resize:vertical}.input-with-button{display:flex;gap:4px}.input-with-button input{flex:1;min-width:0}.lookup-button{width:32px;padding:2px!important;font-size:18px!important}.radio-field{grid-column:span 3;display:flex;gap:14px;align-items:center;margin:0;border:1px solid #d4d1ca;padding:7px 10px}.radio-field legend{font-weight:600;color:#55524b;padding:0 4px}.radio-field label,.check-label{display:flex;align-items:center;gap:6px}.check-label{padding:5px 0}.two-column-layout{display:grid;grid-template-columns:1fr 1fr;gap:9px}.order-grid{display:grid;grid-template-columns:1fr 1fr;gap:8px 12px;margin-top:8px}.order-grid label{display:grid;gap:4px}.audit-block{display:grid;grid-template-columns:80px 1fr 80px 1fr 80px 1fr 80px 1fr;align-items:center;gap:5px;padding:8px;background:#f3f2ef;border:1px solid #ddd}.audit-block input{width:100%;min-width:0;background:#eee!important}.section-heading{display:flex;align-items:center;justify-content:space-between;gap:10px;margin-bottom:8px}.section-heading h3{margin:0}.help-text{font-size:12px;color:#777;margin:6px 0}.parameter-table-wrap{border:1px solid #d4d1ca;overflow:auto}.parameter-table{width:100%;border-collapse:collapse}.parameter-table th{padding:6px 8px;text-align:left;color:white;background:#54a8df}.parameter-table td{padding:5px;border:1px solid #ddd}.parameter-table input,.parameter-table select{width:100%;min-width:90px}.text-button{background:transparent!important;border:0!important}.danger-text{color:#a43b31!important}.hidden-file{display:none}.file-status{padding:6px 8px;background:#f7f3e6;color:#5c5542}.file-preview{max-height:180px;overflow:auto;margin:0;padding:8px;background:#faf9f6;font:12px/1.4 Consolas,monospace;white-space:pre-wrap}.dialog-footer{display:flex;justify-content:flex-end;gap:8px;padding:8px 13px;border-top:1px solid #d1cec6;background:#f5f3ed}.picker-dialog{width:min(620px,95vw)}.picker-content{padding:12px;overflow:auto;min-height:180px}.target-list{max-height:45vh;overflow:auto;border:1px solid #d8d5ce;margin-top:7px;background:white}.target-row{display:flex;align-items:center;gap:8px;padding:8px;border-bottom:1px solid #eee}.target-row:hover{background:#fff7d9}
.picker-row{display:block;width:100%;text-align:left;padding:8px 10px!important;border:0!important;border-bottom:1px solid #eee!important;background:#fff!important}.picker-row:hover{background:#fff2bf!important}.picker-backdrop{z-index:60}
@media(max-width:900px){.filter-panel{flex-wrap:wrap}.filter-item{flex:1;min-width:200px}.script-grid,.two-column-layout{grid-template-columns:1fr}.form-grid{grid-template-columns:1fr 1fr}.form-grid .radio-field{grid-column:span 2}.audit-block{grid-template-columns:80px 1fr 80px 1fr}.data-grid{min-width:1900px}}
</style>
