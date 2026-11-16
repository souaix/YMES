<script setup lang="ts">
import { computed, ref } from 'vue'

type EditorMode = 'add' | 'edit' | 'copy' | null
type ScriptField = 'vendorNoOrder' | 'materialNoOrder' | 'materialLotNoOrder' | 'columnsOrder' | 'dataOrder' | 'materialNoRowOrder' | 'materialLotNoRowOrder' | 'carrierBoxOrder' | 'locationRowOrder' | 'componentNoRowOrder' | 'laserRowOrder'
interface ScriptRow {
  mtlFolderPath: string
  vendorNo: string | null
  formatScript: string | null
  rawDataFormatScript?: string | null
  description: string | null
  creator: string
  createDate: string
  reviser: string | null
  reviseDate: string | null
  storeRawData: number
  sendMail: number
  masterSeparateSign: string | null
  detailCloumnSeparateSign: string | null
  withHeader: number | null
  dataStamp: number | null
}
interface DynamicParameter { id: string; testParameterNo: string; sequence: number }
interface EditorValue {
  mtlFolderPath: string; description: string; masterSeparateSign: string; detailCloumnSeparateSign: string
  withHeader: boolean; sendMail: number; vendorNoOrder: number; materialNoOrder: number; materialLotNoOrder: number
  columnsOrder: number; dataOrder: number; materialNoRowOrder: number; materialLotNoRowOrder: number
  carrierBoxOrder: number; locationRowOrder: number; componentNoRowOrder: number; laserRowOrder: number
  dynamicParameters: DynamicParameter[]; testFileName: string; testFileText: string
}

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const api = 'api/material-vendor-autoloader-scripts'
const storageKey = 'ymes-dev-mock:mtl-vendor-autoloader:v1'

const seedRows: ScriptRow[] = [
  {
    mtlFolderPath: 'D:\\MES\\AutoLoader\\DEV-VENDOR-A\\', vendorNo: 'DEV-VENDOR-A', description: 'CSV 來料檔案格式（DEV）',
    formatScript: 'VendorNo=>>1\nMaterialNo=>>2\nMaterialLotNo=>>3\nColumnsStart=>>1\nDataStart=>>2\nD_MaterialNo=>>1\nD_MaterialLotNo=>>2\nD_CassetteNo=>>3\nD_SlotNo=>>4\nD_ComponentNo=>>5\nD_LaserMark=>>6\nTEST-VOLTAGE=>>7',
    creator: 'DEV001', createDate: '2026-11-01T08:00:00+08:00', reviser: 'DEV001', reviseDate: '2026-11-03T10:30:00+08:00',
    storeRawData: 0, sendMail: 0, masterSeparateSign: ':', detailCloumnSeparateSign: ',', withHeader: 1, dataStamp: 2,
  },
  {
    mtlFolderPath: 'D:\\MES\\AutoLoader\\DEV-VENDOR-B\\', vendorNo: 'DEV-VENDOR-B', description: 'TXT 來料檔案格式（DEV）',
    formatScript: 'VendorNo=>>0\nMaterialNo=>>0\nMaterialLotNo=>>0\nColumnsStart=>>1\nDataStart=>>3\nD_MaterialNo=>>1\nD_MaterialLotNo=>>2\nD_CassetteNo=>>3\nD_SlotNo=>>4\nD_ComponentNo=>>0\nD_LaserMark=>>0',
    creator: 'DEV002', createDate: '2026-11-02T09:00:00+08:00', reviser: null, reviseDate: null,
    storeRawData: 0, sendMail: 1, masterSeparateSign: ':', detailCloumnSeparateSign: '|', withHeader: 1, dataStamp: 0,
  },
]
const sampleRecipes = [
  { testParameterNo: 'TEST-VOLTAGE', testParameterName: '測試電壓（DEV）' },
  { testParameterNo: 'TEST-CURRENT', testParameterName: '測試電流（DEV）' },
  { testParameterNo: 'TEST-RESULT', testParameterName: '測試結果（DEV）' },
]
const demoFolders = ['D:\\MES\\AutoLoader\\DEV-VENDOR-A\\', 'D:\\MES\\AutoLoader\\DEV-VENDOR-B\\', 'D:\\MES\\AutoLoader\\NEW-VENDOR\\']

const rows = ref<ScriptRow[]>([])
const filters = ref({ mtlFolderPath: '', vendorNo: '' })
const activeKey = ref('')
const selectedKeys = ref<string[]>([])
const queried = ref(false)
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const notice = ref('')
const editorMode = ref<EditorMode>(null)
const editor = ref<EditorValue>(blankEditor())
const audit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const sourceRow = ref<ScriptRow | null>(null)
const editorError = ref('')
const editorNotice = ref('')
const picker = ref<'folder' | 'vendor' | 'recipe' | null>(null)
const pickerQuery = ref('')
const pickerLoading = ref(false)
const recipeOptions = ref<typeof sampleRecipes>([])
const folderOptions = ref<string[]>([])
const vendorOptions = ref<Array<{ vendorNo: string; vendorName?: string | null }>>([])
const previewLines = ref<string[]>([])
const previewFile = ref<HTMLInputElement | null>(null)
const fieldLabels: Record<ScriptField, string> = {
  vendorNoOrder: 'VendorNo Order', materialNoOrder: 'MaterialNo Order', materialLotNoOrder: 'MaterialLotNo Order',
  columnsOrder: 'Columns Order', dataOrder: 'Data Order', materialNoRowOrder: 'MaterialsNo Row Order',
  materialLotNoRowOrder: 'MaterialLotNo Row Order', carrierBoxOrder: 'CarrierBox Order', locationRowOrder: 'Location Row Order',
  componentNoRowOrder: 'ComponentNo Row Order', laserRowOrder: 'Laser Row Order',
}
const headerFields: ScriptField[] = ['vendorNoOrder', 'materialNoOrder', 'materialLotNoOrder']
const bodyFields: ScriptField[] = ['materialNoRowOrder', 'materialLotNoRowOrder', 'carrierBoxOrder', 'locationRowOrder', 'componentNoRowOrder', 'laserRowOrder']
const visibleRows = computed(() => rows.value.slice().sort((a, b) => a.mtlFolderPath.localeCompare(b.mtlFolderPath)))
const editorTitle = computed(() => `來料 AutoLoader 格式檔設定（${editorMode.value === 'add' ? '新增' : editorMode.value === 'copy' ? '複製' : '修改'}）`)
const filteredFolders = computed(() => folderOptions.value.filter(folder => folder.toLowerCase().includes(pickerQuery.value.toLowerCase())))
const filteredVendors = computed(() => vendorOptions.value.filter(vendor => `${vendor.vendorNo} ${vendor.vendorName ?? ''}`.toLowerCase().includes(pickerQuery.value.toLowerCase())))

function blankEditor(): EditorValue {
  return {
    mtlFolderPath: '', description: '', masterSeparateSign: ':', detailCloumnSeparateSign: ',', withHeader: true, sendMail: 0,
    vendorNoOrder: 0, materialNoOrder: 0, materialLotNoOrder: 0, columnsOrder: 1, dataOrder: 2,
    materialNoRowOrder: 1, materialLotNoRowOrder: 2, carrierBoxOrder: 3, locationRowOrder: 4,
    componentNoRowOrder: 0, laserRowOrder: 0, dynamicParameters: [], testFileName: '', testFileText: '',
  }
}
function keyOf(row: ScriptRow): string { return row.mtlFolderPath }
function encode(value: string): string { return encodeURIComponent(value) }
function now(): string { return new Date().toISOString() }
function localRows(): ScriptRow[] {
  try {
    const saved = localStorage.getItem(storageKey)
    return saved ? JSON.parse(saved) as ScriptRow[] : structuredClone(seedRows)
  } catch { return structuredClone(seedRows) }
}
function persistMock(): void { localStorage.setItem(storageKey, JSON.stringify(rows.value)) }
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const base = props.apiBaseUrl.replace(/\/+$/, '')
  const response = await fetch(`${base}/${path.replace(/^\/+/, '')}`, {
    credentials: 'include', ...init,
    headers: { ...(init.body ? { 'Content-Type': 'application/json' } : {}), ...init.headers },
  })
  if (!response.ok) {
    const body = await response.json().catch(() => null) as { message?: string; title?: string } | null
    throw new Error(body?.message || body?.title || `API 請求失敗（${response.status}）`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function selectRow(row: ScriptRow): void {
  activeKey.value = keyOf(row)
  if (!selectedKeys.value.includes(activeKey.value)) selectedKeys.value = [activeKey.value]
  errorMessage.value = ''; notice.value = ''
}
function toggleRow(row: ScriptRow, checked: boolean): void {
  activeKey.value = keyOf(row)
  selectedKeys.value = checked ? [...new Set([...selectedKeys.value, keyOf(row)])] : selectedKeys.value.filter(key => key !== keyOf(row))
}
function toggleAll(checked: boolean): void { selectedKeys.value = checked ? visibleRows.value.map(keyOf) : [] }
function isSelected(row: ScriptRow): boolean { return selectedKeys.value.includes(keyOf(row)) }
function toggleAllFromEvent(event: Event): void { toggleAll((event.target as HTMLInputElement).checked) }
function toggleRowFromEvent(row: ScriptRow, event: Event): void { toggleRow(row, (event.target as HTMLInputElement).checked) }

async function loadRows(preserveKey = ''): Promise<void> {
  loading.value = true; errorMessage.value = ''; notice.value = ''
  try {
    if (mock.value) {
      const path = filters.value.mtlFolderPath.trim().toUpperCase()
      const vendor = filters.value.vendorNo.trim().toUpperCase()
      rows.value = localRows().filter(row => (!path || row.mtlFolderPath.toUpperCase() === path) && (!vendor || (row.vendorNo ?? '').toUpperCase() === vendor))
    } else {
      const query = new URLSearchParams()
      if (filters.value.mtlFolderPath.trim()) query.set('mtlFolderPath', filters.value.mtlFolderPath.trim())
      if (filters.value.vendorNo.trim()) query.set('vendorNo', filters.value.vendorNo.trim().toUpperCase())
      const queryString = query.toString()
      rows.value = await request<ScriptRow[]>(`${api}${queryString ? `?${queryString}` : ''}`)
    }
    const next = rows.value.find(row => keyOf(row) === preserveKey) ?? rows.value[0] ?? null
    activeKey.value = next ? keyOf(next) : ''
    selectedKeys.value = next ? [keyOf(next)] : []
    queried.value = true
  } catch (error) {
    rows.value = []; activeKey.value = ''; selectedKeys.value = []
    errorMessage.value = error instanceof Error ? error.message : 'AutoLoader 格式資料載入失敗。'
  } finally { loading.value = false }
}
function runQuery(): void { void loadRows() }
const activeRow = computed(() => rows.value.find(row => row.mtlFolderPath === activeKey.value) ?? null)

function parseFormatScript(script: string | null): Pick<EditorValue, ScriptField | 'dynamicParameters'> {
  const values = new Map<string, string>()
  for (const line of (script ?? '').split(/\r?\n/)) {
    const marker = line.indexOf('=>>')
    if (marker >= 0) values.set(line.slice(0, marker).trim(), line.slice(marker + 3).trim())
  }
  const int = (key: string, fallback: number): number => Number.isFinite(Number(values.get(key))) ? Number(values.get(key)) : fallback
  const known: Record<string, ScriptField> = {
    VendorNo: 'vendorNoOrder', MaterialNo: 'materialNoOrder', MaterialLotNo: 'materialLotNoOrder',
    ColumnsStart: 'columnsOrder', DataStart: 'dataOrder', D_MaterialNo: 'materialNoRowOrder',
    D_MaterialLotNo: 'materialLotNoRowOrder', D_CassetteNo: 'carrierBoxOrder', D_SlotNo: 'locationRowOrder',
    D_ComponentNo: 'componentNoRowOrder', D_LaserMark: 'laserRowOrder',
  }
  const parsed = {} as Pick<EditorValue, ScriptField | 'dynamicParameters'>
  for (const [key, field] of Object.entries(known)) Object.assign(parsed, { [field]: int(key, 0) })
  parsed.dynamicParameters = [...values.entries()].filter(([key]) => !known[key]).map(([testParameterNo, sequence], index) => ({ id: `${testParameterNo}-${index}`, testParameterNo, sequence: Number(sequence) || 0 }))
  return parsed
}
function fillEditor(row: ScriptRow): EditorValue {
  const parsed = parseFormatScript(row.formatScript)
  return {
    ...blankEditor(), mtlFolderPath: row.mtlFolderPath, description: row.description ?? '',
    masterSeparateSign: row.masterSeparateSign ?? ':', detailCloumnSeparateSign: row.detailCloumnSeparateSign ?? ',',
    withHeader: row.withHeader === 1, sendMail: row.sendMail,
    vendorNoOrder: parsed.vendorNoOrder, materialNoOrder: parsed.materialNoOrder, materialLotNoOrder: parsed.materialLotNoOrder,
    columnsOrder: parsed.columnsOrder, dataOrder: parsed.dataOrder, materialNoRowOrder: parsed.materialNoRowOrder,
    materialLotNoRowOrder: parsed.materialLotNoRowOrder, carrierBoxOrder: parsed.carrierBoxOrder, locationRowOrder: parsed.locationRowOrder,
    componentNoRowOrder: parsed.componentNoRowOrder, laserRowOrder: parsed.laserRowOrder, dynamicParameters: parsed.dynamicParameters,
  }
}
function openEditor(mode: Exclude<EditorMode, null>): void {
  const row = activeRow.value
  if ((mode === 'edit' || mode === 'copy') && !row) return
  sourceRow.value = mode === 'add' ? null : row
  editorMode.value = mode
  editorError.value = ''; editorNotice.value = ''; picker.value = null; previewLines.value = []
  editor.value = mode === 'add' || !row ? blankEditor() : fillEditor(row)
  if (mode === 'copy') editor.value.mtlFolderPath = ''
  audit.value = mode === 'edit' && row
    ? { creator: row.creator, createDate: row.createDate, reviser: props.currentUserNo || '', reviseDate: now() }
    : { creator: props.currentUserNo || 'DEV001', createDate: now(), reviser: '', reviseDate: '' }
}
function closeEditor(): void { editorMode.value = null; sourceRow.value = null; editorError.value = ''; editorNotice.value = ''; picker.value = null }
function addParameter(): void { editor.value.dynamicParameters.push({ id: `${Date.now()}-${Math.random()}`, testParameterNo: '', sequence: 0 }) }
function removeParameter(index: number): void { editor.value.dynamicParameters.splice(index, 1) }
function generatedScript(value = editor.value): string {
  const rules: Array<[string, number]> = [
    ['VendorNo', value.vendorNoOrder], ['MaterialNo', value.materialNoOrder], ['MaterialLotNo', value.materialLotNoOrder],
    ['ColumnsStart', value.columnsOrder], ['DataStart', value.dataOrder], ['D_MaterialNo', value.materialNoRowOrder],
    ['D_MaterialLotNo', value.materialLotNoRowOrder], ['D_CassetteNo', value.carrierBoxOrder], ['D_SlotNo', value.locationRowOrder],
    ['D_ComponentNo', value.componentNoRowOrder], ['D_LaserMark', value.laserRowOrder],
  ]
  const lines = rules.map(([key, order]) => `${key}=>>${order}`)
  for (const parameter of value.dynamicParameters) if (parameter.testParameterNo.trim()) lines.push(`${parameter.testParameterNo.trim()}=>>${parameter.sequence}`)
  return lines.join('\n')
}
function validateEditor(): boolean {
  const value = editor.value
  if (!value.mtlFolderPath.trim()) { editorError.value = '請輸入 MTL Folder Path。'; return false }
  if (value.mtlFolderPath.length > 400) { editorError.value = 'MTL Folder Path 不可超過 400 個字元。'; return false }
  if (value.description.length > 255) { editorError.value = 'Description 不可超過 255 個字元。'; return false }
  if (value.masterSeparateSign.length > 10 || value.detailCloumnSeparateSign.length > 10) { editorError.value = '分隔符號不可超過 10 個字元。'; return false }
  if (value.withHeader && !value.masterSeparateSign) { editorError.value = '有表頭時必須設定 Master Separate Sign。'; return false }
  if (!value.detailCloumnSeparateSign) { editorError.value = '請輸入 DetailCloumn Separate Sign。'; return false }
  if (!Number.isInteger(value.columnsOrder) || value.columnsOrder <= 0) { editorError.value = 'Columns Order 必須大於 0。'; return false }
  if (!Number.isInteger(value.dataOrder) || value.dataOrder <= value.columnsOrder) { editorError.value = 'Data Order 必須大於 Columns Order。'; return false }
  const orders: ScriptField[] = ['vendorNoOrder', 'materialNoOrder', 'materialLotNoOrder', 'materialNoRowOrder', 'materialLotNoRowOrder', 'carrierBoxOrder', 'locationRowOrder', 'componentNoRowOrder', 'laserRowOrder']
  if (orders.some(key => !Number.isInteger(value[key]) || value[key] < 0)) { editorError.value = '各欄位位置需為 0 或正整數。'; return false }
  if (value.dynamicParameters.some(item => !item.testParameterNo.trim() || !Number.isInteger(item.sequence) || item.sequence < 0)) { editorError.value = '動態測試參數名稱與順序不可空白。'; return false }
  if (value.dynamicParameters.some(item => item.testParameterNo.length > 50)) { editorError.value = '測試參數名稱不可超過 50 個字元。'; return false }
  editorError.value = ''
  return true
}
function payload(): Record<string, unknown> {
  return {
    mtlFolderPath: editor.value.mtlFolderPath.trim().toUpperCase(), vendorNo: editorMode.value === 'edit' ? sourceRow.value?.vendorNo ?? null : null,
    description: editor.value.description.trim() || null, formatScript: generatedScript(), rawDataFormatScript: sourceRow.value?.rawDataFormatScript ?? null,
    storeRawData: 0, sendMail: editor.value.sendMail, masterSeparateSign: editor.value.masterSeparateSign || null,
    detailCloumnSeparateSign: editor.value.detailCloumnSeparateSign, withHeader: editor.value.withHeader ? 1 : 0,
    dataStamp: sourceRow.value?.dataStamp ?? null,
  }
}
async function saveEditor(closeAfterSave: boolean): Promise<void> {
  if (!validateEditor()) return
  busy.value = true; editorError.value = ''; editorNotice.value = ''
  try {
    const body = payload()
    if (mock.value) {
      const stored = localRows()
      const key = String(body.mtlFolderPath)
      const current = stored.findIndex(row => row.mtlFolderPath === key)
      if (editorMode.value !== 'edit' && current >= 0) throw new Error('此 MTL Folder Path 已存在。')
      const stamp = sourceRow.value?.dataStamp ?? 0
      const row: ScriptRow = {
        ...body, mtlFolderPath: key, formatScript: String(body.formatScript), creator: sourceRow.value?.creator ?? audit.value.creator,
        createDate: sourceRow.value?.createDate ?? audit.value.createDate, reviser: editorMode.value === 'edit' ? (props.currentUserNo || 'DEV001') : null,
        reviseDate: editorMode.value === 'edit' ? now() : null, dataStamp: editorMode.value === 'edit' ? (stamp ?? 0) + 1 : 0,
      } as ScriptRow
      if (editorMode.value === 'edit') {
        if (current < 0) throw new Error('資料已不存在，請重新查詢。')
        stored[current] = row
      } else stored.push(row)
      rows.value = stored; persistMock()
    } else if (editorMode.value === 'edit' && sourceRow.value) {
      await request<void>(`${api}/${encode(sourceRow.value.mtlFolderPath)}`, { method: 'PUT', body: JSON.stringify(body) })
    } else {
      await request<void>(api, { method: 'POST', body: JSON.stringify(body) })
    }
    await loadRows(String(body.mtlFolderPath))
    notice.value = editorMode.value === 'edit' ? '格式設定已更新。' : '格式設定已新增。'
    if (closeAfterSave) closeEditor()
    else {
      editorNotice.value = '已套用變更；可繼續修改或關閉。'
      sourceRow.value = rows.value.find(row => row.mtlFolderPath === String(body.mtlFolderPath)) ?? sourceRow.value
      if (editorMode.value !== 'edit') editorMode.value = 'edit'
    }
  } catch (error) { editorError.value = error instanceof Error ? error.message : '儲存格式設定失敗。' }
  finally { busy.value = false }
}
async function deleteSelected(): Promise<void> {
  if (!selectedKeys.value.length && activeRow.value) selectedKeys.value = [activeKey.value]
  if (!selectedKeys.value.length || !window.confirm(`確定刪除選取的 ${selectedKeys.value.length} 筆 AutoLoader 格式設定？`)) return
  busy.value = true; errorMessage.value = ''; notice.value = ''
  try {
    if (mock.value) {
      const remove = new Set(selectedKeys.value)
      rows.value = localRows().filter(row => !remove.has(row.mtlFolderPath)); persistMock()
    } else {
      for (const key of selectedKeys.value) await request<void>(`${api}/${encode(key)}`, { method: 'DELETE' })
    }
    await loadRows()
    notice.value = '選取的 AutoLoader 格式設定已刪除。'
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '刪除格式設定失敗。' }
  finally { busy.value = false }
}
function parsePreview(text: string): string[] {
  return text.replace(/^\uFEFF/, '').split(/\r?\n/).filter(Boolean).slice(0, 40)
}
async function onFileSelected(event: Event): Promise<void> {
  const file = (event.target as HTMLInputElement).files?.[0]
  if (!file) return
  editor.value.testFileName = file.name; editor.value.testFileText = ''
  try {
    if (/\.(csv|txt)$/i.test(file.name)) {
      editor.value.testFileText = await file.text()
      previewLines.value = parsePreview(editor.value.testFileText)
      editorError.value = ''
    } else {
      previewLines.value = []
      editorError.value = '瀏覽器 DEV 預覽支援 CSV/TXT；Excel 檔案轉換需由正式 API 提供。'
    }
  } catch { editorError.value = '無法讀取選取的測試檔。' }
}
function previewRows(line: string): string[] { return line.split(editor.value.detailCloumnSeparateSign || ',').slice(0, 12) }
async function openPicker(kind: 'folder' | 'vendor' | 'recipe'): Promise<void> {
  picker.value = kind; pickerQuery.value = ''; pickerLoading.value = true
  try {
    if (kind === 'recipe') recipeOptions.value = mock.value ? structuredClone(sampleRecipes) : await request<typeof sampleRecipes>('api/test-recipes?recipeType=MTL_AUTOLOADER')
    else if (kind === 'folder') folderOptions.value = mock.value ? [...demoFolders] : await request<string[]>(`${api}/directories`)
    else vendorOptions.value = mock.value ? ['DEV-VENDOR-A', 'DEV-VENDOR-B', 'DEV-VENDOR-C'].map(vendorNo => ({ vendorNo, vendorName: 'DEV 供應商' })) : await request<typeof vendorOptions.value>('api/material-vendors')
  } catch (error) { editorError.value = error instanceof Error ? error.message : '測試配方載入失敗。'; recipeOptions.value = [] }
  finally { pickerLoading.value = false }
}
function chooseFolder(path: string): void { editor.value.mtlFolderPath = path; picker.value = null }
function chooseRecipe(recipe: { testParameterNo: string; testParameterName?: string }): void {
  if (!editor.value.dynamicParameters.some(item => item.testParameterNo === recipe.testParameterNo)) editor.value.dynamicParameters.push({ id: `${recipe.testParameterNo}-${Date.now()}`, testParameterNo: recipe.testParameterNo, sequence: editor.value.dynamicParameters.length + 1 })
  picker.value = null
}
function chooseVendor(vendorNo: string): void { filters.value.vendorNo = vendorNo; picker.value = null }
async function checkFormat(): Promise<void> {
  if (!validateEditor()) return
  editorNotice.value = ''; editorError.value = ''; busy.value = true
  try {
    const body = { formatScript: generatedScript(), withHeader: editor.value.withHeader, masterSeparateSign: editor.value.masterSeparateSign, detailCloumnSeparateSign: editor.value.detailCloumnSeparateSign, sampleLines: previewLines.value }
    if (mock.value) {
      editorNotice.value = previewLines.value.length ? `規則格式可產生，已讀取 ${previewLines.value.length} 行測試檔預覽（DEV 簡化檢查）。` : '規則格式可產生；尚未載入測試檔（DEV 簡化檢查）。'
    } else {
      const result = await request<{ valid: boolean; message?: string }>(`${api}/validate`, { method: 'POST', body: JSON.stringify(body) })
      if (!result.valid) throw new Error(result.message || '格式檢查未通過。')
      editorNotice.value = result.message || '格式檢查完成。'
    }
  } catch (error) { editorError.value = error instanceof Error ? error.message : '格式檢查失敗。' }
  finally { busy.value = false }
}
function closePage(): void { emit('close') }
</script>

<template>
  <main class="autoloader-page">
    <header class="page-header">
      <div><h1>來料 AutoLoader 格式檔設定</h1><p>MTL Folder Path、Vendor 與解析格式設定</p></div>
      <span v-if="mock" class="dev-tag">DEV 假資料</span>
    </header>

    <section class="filter-panel" aria-label="查詢條件">
      <label class="filter-item"><span>MTL Folder Path</span><input v-model.trim="filters.mtlFolderPath" maxlength="400" @keyup.enter="runQuery" @blur="filters.mtlFolderPath = filters.mtlFolderPath.toUpperCase()"></label>
      <label class="filter-item"><span>Vendor No</span><div class="input-with-button"><input v-model.trim="filters.vendorNo" maxlength="20" @keyup.enter="runQuery" @blur="filters.vendorNo = filters.vendorNo.toUpperCase()"><button type="button" class="lookup-button" aria-label="選擇供應商" @click="openPicker('vendor')">…</button></div></label>
      <button type="button" class="primary-button query-button" :disabled="loading || busy" @click="runQuery">{{ loading ? '查詢中…' : '查詢' }}</button>
    </section>

    <section class="list-panel">
      <div class="list-layout">
      <div class="table-wrap">
        <table class="data-grid">
          <thead><tr>
            <th class="check-column"><input type="checkbox" :checked="visibleRows.length > 0 && selectedKeys.length === visibleRows.length" aria-label="全選" @change="toggleAllFromEvent"></th>
            <th>MTL Folder Path</th><th>Description</th><th>Creator</th><th>Create Date</th><th>Reviser</th><th>Revise Date</th><th>Master Separate Sign</th><th>DetailCloumn Separate Sign</th><th>With Header</th>
          </tr></thead>
          <tbody v-if="queried && visibleRows.length">
            <tr v-for="row in visibleRows" :key="keyOf(row)" :class="{ active: activeKey === keyOf(row) }" @click="selectRow(row)" @dblclick="openEditor('edit')">
              <td class="check-column"><input type="checkbox" :checked="isSelected(row)" :aria-label="`選取 ${row.mtlFolderPath}`" @click.stop @change="toggleRowFromEvent(row, $event)"></td>
              <td class="path-cell">{{ row.mtlFolderPath }}</td><td>{{ row.description || '—' }}</td><td>{{ row.creator }}</td><td>{{ row.createDate ? new Date(row.createDate).toLocaleString() : '—' }}</td><td>{{ row.reviser || '—' }}</td><td>{{ row.reviseDate ? new Date(row.reviseDate).toLocaleString() : '—' }}</td><td>{{ row.masterSeparateSign || '—' }}</td><td>{{ row.detailCloumnSeparateSign || '—' }}</td><td>{{ row.withHeader === 1 ? '是' : '否' }}</td>
            </tr>
          </tbody>
          <tbody v-else><tr><td colspan="10" class="empty-state">{{ queried ? '查無資料' : '請輸入查詢條件後按「查詢」' }}</td></tr></tbody>
        </table>
      </div>
      <aside class="side-actions" aria-label="資料操作">
        <button type="button" class="primary-button" @click="openEditor('add')">新增(A)</button>
        <button type="button" :disabled="!activeRow || busy" @click="openEditor('edit')">修改(E)</button>
        <button type="button" :disabled="!activeRow || busy" @click="openEditor('copy')">複製(C)</button>
        <button type="button" :disabled="!selectedKeys.length || busy" @click="deleteSelected">刪除(D)</button>
        <button type="button" class="close-action" @click="closePage">關閉</button>
      </aside>
      </div>
      <footer class="list-footer">資料總筆數：{{ queried ? visibleRows.length : 0 }}<span v-if="selectedKeys.length">　已選取 {{ selectedKeys.length }} 筆</span></footer>
      <p v-if="errorMessage" class="alert error-alert" role="alert">{{ errorMessage }}</p>
      <p v-if="notice" class="alert success-alert" role="status">{{ notice }}</p>
    </section>

    <section class="script-preview-panel">
      <div class="section-heading"><h2>Format Script</h2><span>唯讀預覽；內容由欄位設定產生</span></div>
      <pre v-if="activeRow?.formatScript">{{ activeRow.formatScript }}</pre><p v-else class="preview-empty">選取資料列後顯示格式腳本</p>
    </section>

    <div v-if="editorMode" class="modal-backdrop" @click.self="closeEditor">
      <section class="editor-dialog" role="dialog" aria-modal="true" :aria-label="editorTitle">
        <header class="dialog-header"><h2>{{ editorTitle }}</h2><button type="button" class="icon-button" aria-label="關閉" @click="closeEditor">×</button></header>
        <div class="editor-scroll">
          <section class="form-section">
            <h3>格式檔基本資料設定</h3>
            <div class="form-grid">
              <label class="wide-field"><span>MTL Folder Path <b>*</b></span><div class="input-with-button"><input v-model.trim="editor.mtlFolderPath" maxlength="400" :readonly="editorMode === 'edit'" @blur="editor.mtlFolderPath = editor.mtlFolderPath.toUpperCase()"><button type="button" class="lookup-button" :disabled="editorMode === 'edit'" @click="openPicker('folder')">…</button></div></label>
              <label><span>Description</span><textarea v-model="editor.description" maxlength="255" rows="2"></textarea></label>
              <div class="form-block"><span class="block-label">Head Exists</span><label class="check-label"><input v-model="editor.withHeader" type="checkbox"> 有表頭</label></div>
              <label><span>Master Separate Sign</span><input v-model="editor.masterSeparateSign" maxlength="10" :disabled="!editor.withHeader"></label>
              <label><span>DetailCloumn Separate Sign <b>*</b></span><input v-model="editor.detailCloumnSeparateSign" maxlength="10"></label>
              <fieldset class="radio-field"><legend>Allow Send Mail</legend><label><input v-model.number="editor.sendMail" type="radio" :value="1"> 是</label><label><input v-model.number="editor.sendMail" type="radio" :value="0"> 否</label></fieldset>
              <div class="form-block"><span class="block-label">Store Raw Data</span><span class="muted-value">否（來源畫面固定傳入 0）</span></div>
              <div class="form-block audit-block"><span class="block-label">建立人／日期</span><span>{{ audit.creator || '—' }}　{{ audit.createDate ? new Date(audit.createDate).toLocaleString() : '—' }}</span><span class="block-label">修改人／日期</span><span>{{ audit.reviser || '—' }}　{{ audit.reviseDate ? new Date(audit.reviseDate).toLocaleString() : '—' }}</span></div>
            </div>
          </section>

          <section class="form-section">
            <div class="section-heading"><h3>測試檔與表頭／資料起始列</h3><div class="heading-actions"><input ref="previewFile" class="hidden-file" type="file" accept=".csv,.txt,.xls,.xlsx" @change="onFileSelected"><button type="button" @click="previewFile?.click()">選擇測試檔</button><button type="button" :disabled="!editor.testFileName" @click="previewLines = parsePreview(editor.testFileText)">View</button><button type="button" @click="checkFormat" :disabled="busy">格式檢查</button></div></div>
            <div class="two-column-layout">
              <div class="setting-card"><h4>Header</h4><label class="number-field"><span>Columns Order <b>*</b></span><input v-model.number="editor.columnsOrder" type="number" min="1"></label><div class="order-grid"><label v-for="key in headerFields" :key="key" class="number-field"><span>{{ fieldLabels[key] }}</span><input v-model.number="editor[key]" type="number" min="0"></label></div><p class="help-text">位置從 1 開始；0 代表此欄位不在表頭。</p></div>
              <div class="setting-card"><h4>Body</h4><label class="number-field"><span>Data Order <b>*</b></span><input v-model.number="editor.dataOrder" type="number" min="1"></label><div class="order-grid"><label v-for="key in bodyFields" :key="key" class="number-field"><span>{{ fieldLabels[key] }}</span><input v-model.number="editor[key]" type="number" min="0"></label></div><p class="help-text">Data Order 必須大於 Columns Order。</p></div>
            </div>
            <div v-if="editor.testFileName" class="file-status">測試檔：{{ editor.testFileName }}　（最多預覽 40 行）</div>
            <div v-if="previewLines.length" class="sample-preview"><div class="preview-caption">測試檔預覽</div><div v-for="(line, index) in previewLines.slice(0, 8)" :key="index" class="sample-line"><code>{{ String(index + 1).padStart(2, '0') }}</code><span>{{ line }}</span></div></div>
          </section>

          <section class="form-section">
            <div class="section-heading"><div><h3>Dynamic Test Parameter</h3><p class="section-subtitle">測試參數名稱與輸入檔欄位序號會寫入 FormatScript CLOB。</p></div><div class="heading-actions"><button type="button" @click="openPicker('recipe')">Test Item Setup</button><button type="button" @click="addParameter">新增參數</button></div></div>
            <div class="parameter-table-wrap"><table class="parameter-table"><thead><tr><th>TestParameterNo</th><th>Sequence</th><th></th></tr></thead><tbody><tr v-for="(item, index) in editor.dynamicParameters" :key="item.id"><td><input v-model.trim="item.testParameterNo" maxlength="50" aria-label="TestParameterNo"></td><td><input v-model.number="item.sequence" type="number" min="0" aria-label="Sequence"></td><td><button type="button" class="text-button danger-text" @click="removeParameter(index)">移除</button></td></tr><tr v-if="!editor.dynamicParameters.length"><td colspan="3" class="empty-state">尚未設定動態測試參數</td></tr></tbody></table></div>
            <div class="transform-note"><span>Columns Transform Setup</span><span>DEV 版先保留欄位順序映射；舊版進階轉換規則需由正式 API／轉換設定介面補齊。</span><button type="button" @click="editorNotice = '欄位轉換的進階公式與測試執行交由正式 API 解析；目前可編輯欄位序號與動態測試參數。'">設定說明</button></div>
          </section>

          <section class="form-section script-section"><div class="section-heading"><h3>Format Script 預覽</h3><span>套用前可確認實際寫入內容</span></div><pre>{{ generatedScript() }}</pre></section>
          <p v-if="editorError" class="alert error-alert" role="alert">{{ editorError }}</p>
          <p v-if="editorNotice" class="alert success-alert" role="status">{{ editorNotice }}</p>
        </div>
        <footer class="dialog-footer"><button type="button" :disabled="busy" @click="closeEditor">關閉</button><button type="button" :disabled="busy" @click="saveEditor(false)">套用</button><button type="button" class="primary-button" :disabled="busy" @click="saveEditor(true)">確定</button></footer>
      </section>
    </div>

    <div v-if="picker" class="modal-backdrop picker-backdrop" @click.self="picker = null">
      <section class="picker-dialog" role="dialog" aria-modal="true">
        <header class="dialog-header"><h2>{{ picker === 'folder' ? '選擇 MTL Folder Path' : picker === 'vendor' ? '選擇供應商' : '選擇測試配方' }}</h2><button type="button" class="icon-button" @click="picker = null">×</button></header>
        <div class="picker-content">
          <label v-if="picker !== 'recipe'" class="picker-search"><span>查詢</span><input v-model.trim="pickerQuery" :placeholder="picker === 'folder' ? '輸入路徑關鍵字' : '輸入 Vendor No'"></label>
          <p v-if="picker === 'folder'" class="help-text">正式環境應由伺服器回傳允許使用的監控目錄，不直接瀏覽伺服器檔案系統。</p>
          <p v-if="pickerLoading" class="empty-state">載入中…</p>
          <template v-else-if="picker === 'folder'">
            <button v-for="folder in filteredFolders" :key="folder" type="button" class="picker-row" @click="chooseFolder(folder)">{{ folder }}</button>
            <label class="manual-folder"><span>或手動輸入路徑</span><input v-model.trim="editor.mtlFolderPath" maxlength="400"></label>
          </template>
          <template v-else-if="picker === 'vendor'">
            <button v-for="vendor in filteredVendors" :key="vendor.vendorNo" type="button" class="picker-row" @click="chooseVendor(vendor.vendorNo)">{{ vendor.vendorNo }} <small>{{ vendor.vendorName || '' }}</small></button>
            <p v-if="!filteredVendors.length" class="empty-state">沒有符合的供應商</p>
            <p class="help-text">正式環境使用既有物料供應商查詢 API；選取後套用於主畫面的 Vendor No 篩選。</p>
          </template>
          <template v-else>
            <button v-for="recipe in recipeOptions" :key="recipe.testParameterNo" type="button" class="picker-row" @click="chooseRecipe(recipe)">{{ recipe.testParameterNo }} <small>{{ recipe.testParameterName || '' }}</small></button>
            <p v-if="!recipeOptions.length" class="empty-state">沒有可選的測試配方</p>
          </template>
        </div>
        <footer class="dialog-footer"><button type="button" @click="picker = null">關閉</button><button v-if="picker === 'folder'" type="button" class="primary-button" @click="chooseFolder(editor.mtlFolderPath)">使用輸入路徑</button></footer>
      </section>
    </div>
  </main>
</template>

<style scoped>
.autoloader-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:10px;padding:14px;background:#f7f6f2;color:#262522;font-size:13px}.page-header{display:flex;align-items:center;justify-content:space-between;border-bottom:1px solid #d9d6ce;padding:0 2px 9px}.page-header h1{margin:0;font-size:21px;font-weight:600}.page-header p{margin:4px 0 0;color:#79766f}.dev-tag{border:1px solid #d8aa3c;border-radius:4px;padding:4px 8px;background:#fff1c2;color:#745300;font-size:12px}.filter-panel,.list-panel,.script-preview-panel{border:1px solid #d2d0ca;background:#fff}.filter-panel{display:flex;align-items:end;gap:12px;padding:10px}.filter-item{display:grid;gap:4px;min-width:245px}.filter-item>span,.form-section label>span,.picker-search span{font-weight:600;color:#55524b}.autoloader-page input:not([type=checkbox]):not([type=radio]),.autoloader-page textarea{box-sizing:border-box;width:100%;min-height:31px;border:1px solid #aaa79f;border-radius:2px;padding:5px 7px;background:#fff0b4;color:#25241f;font:inherit}.autoloader-page input:focus,.autoloader-page textarea:focus{outline:2px solid #e3bf57;outline-offset:1px}.input-with-button{display:flex;gap:4px}.input-with-button input{flex:1;min-width:0}.autoloader-page button{border:1px solid #bcb9b2;border-radius:3px;background:linear-gradient(#fff,#f1f0ed);color:#292823;padding:6px 12px;font:inherit;cursor:pointer;white-space:nowrap}.autoloader-page button:hover:not(:disabled){background:#ece9e0}.autoloader-page button:disabled{opacity:.52;cursor:not-allowed}.autoloader-page .primary-button{border-color:#df9b28;background:#f8a72a;color:#fff;font-weight:600}.autoloader-page .primary-button:hover:not(:disabled){background:#e89518}.query-button{height:31px;min-width:74px}.lookup-button{width:32px;padding:3px!important;font-size:19px!important;line-height:20px}.list-panel{min-height:205px;display:flex;flex-direction:column}.list-toolbar{display:flex;justify-content:space-between;align-items:center;padding:6px;border-bottom:1px solid #ddd}.toolbar-actions{display:flex;gap:5px}.table-wrap{overflow:auto;flex:1;min-height:115px}.data-grid{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}.data-grid th{position:sticky;top:0;background:#54a8df;color:white;text-align:left;padding:7px 9px;border:1px solid #8ec5e6;font-weight:600}.data-grid td{padding:6px 9px;border:1px solid #d3d0c9}.data-grid tbody tr{cursor:pointer}.data-grid tbody tr:hover{background:#fff8df}.data-grid tbody tr.active{background:#ffe69a}.data-grid input[type=checkbox]{accent-color:#e8a32c}.check-column{width:30px;text-align:center}.path-cell{max-width:260px;overflow:hidden;text-overflow:ellipsis}.empty-state{text-align:center!important;color:#888!important;padding:18px!important;cursor:default!important}.list-footer{padding:6px 9px;color:#777;border-top:1px solid #ddd;font-size:12px}.script-preview-panel{min-height:95px;display:flex;flex-direction:column}.section-heading{display:flex;align-items:center;justify-content:space-between;gap:12px}.section-heading h2,.section-heading h3{margin:0}.script-preview-panel .section-heading{padding:8px 10px;border-bottom:1px solid #dedbd5}.script-preview-panel .section-heading h2{font-size:14px}.section-heading>span,.section-subtitle{font-size:12px;color:#777;margin:0}.script-preview-panel pre,.script-section pre{margin:0;padding:9px 11px;overflow:auto;background:#faf9f6;color:#39372f;font:12px/1.45 Consolas,monospace;white-space:pre-wrap}.preview-empty{margin:0;padding:13px;color:#929088}.alert{margin:7px 9px;padding:8px 10px;border-radius:3px}.error-alert{color:#8b2721;background:#fff0ef;border:1px solid #e7b0ab}.success-alert{color:#315a2b;background:#eff8eb;border:1px solid #c0d9b8}.modal-backdrop{position:fixed;inset:0;z-index:50;display:grid;place-items:center;padding:20px;background:#0006}.editor-dialog,.picker-dialog{display:flex;flex-direction:column;width:min(1180px,96vw);max-height:94vh;background:#f9f8f4;border:1px solid #aaa69d;box-shadow:0 12px 36px #0004}.dialog-header{display:flex;align-items:center;justify-content:space-between;padding:9px 14px;border-bottom:1px solid #d1cec6;background:#f5f3ed}.dialog-header h2{margin:0;font-size:18px}.icon-button{border:0!important;background:transparent!important;font-size:23px!important;padding:0 6px!important}.editor-scroll{overflow:auto;padding:12px 14px}.form-section{border:1px solid #cbc8c0;background:white;padding:12px;margin-bottom:11px}.form-section>h3,.form-section .section-heading h3{margin:0 0 10px;font-size:15px}.form-section .section-heading{margin-bottom:10px}.form-grid{display:grid;grid-template-columns:repeat(3,minmax(190px,1fr));gap:10px 14px;align-items:start}.form-grid>label,.wide-field{display:grid;gap:4px}.wide-field{grid-column:span 2}.form-grid b,.number-field b{color:#9a5d00}.form-grid textarea{resize:vertical;min-height:56px!important}.form-block{display:flex;flex-direction:column;gap:5px}.block-label{font-weight:600;color:#55524b}.check-label,.radio-field label{display:flex;align-items:center;gap:6px}.check-label input,.radio-field input{accent-color:#dca12e}.radio-field{display:flex;gap:16px;align-items:center;margin:0;border:1px solid #d4d1ca;padding:7px 10px}.radio-field legend{font-weight:600;color:#55524b;padding:0 4px}.muted-value{padding:6px 0;color:#777}.audit-block{grid-column:span 3;display:grid;grid-template-columns:130px 1fr 130px 1fr;align-items:center;background:#f6f5f2;padding:8px}.two-column-layout{display:grid;grid-template-columns:1fr 1fr;gap:12px}.setting-card{border:1px solid #d8d5ce;background:#fbfaf7;padding:10px}.setting-card h4{margin:0 0 8px;font-size:14px}.number-field{display:flex;align-items:center;justify-content:space-between;gap:8px;margin:6px 0}.number-field input{width:90px!important;min-height:29px!important}.order-grid{display:grid;grid-template-columns:1fr 1fr;gap:0 10px}.help-text{font-size:12px;color:#777;margin:7px 0}.heading-actions{display:flex;gap:5px;align-items:center}.hidden-file{display:none}.file-status{margin-top:8px;padding:6px 8px;background:#f7f3e6;color:#5c5542}.sample-preview{margin-top:8px;border:1px solid #d7d4cd;max-height:180px;overflow:auto}.preview-caption{padding:5px 8px;background:#eeeae0;font-weight:600}.sample-line{display:grid;grid-template-columns:36px 1fr;gap:8px;padding:4px 7px;border-top:1px solid #eee;font:12px/1.35 Consolas,monospace}.sample-line code{color:#8c826d}.sample-line span{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.parameter-table-wrap{border:1px solid #d4d1ca;overflow:auto}.parameter-table{width:100%;border-collapse:collapse}.parameter-table th{padding:6px 8px;text-align:left;color:white;background:#54a8df}.parameter-table td{padding:5px;border:1px solid #ddd}.parameter-table input{min-width:120px}.text-button{background:transparent!important;border:0!important}.danger-text{color:#a43b31!important}.transform-note{display:grid;grid-template-columns:190px 1fr auto;align-items:center;gap:10px;margin-top:10px;padding:8px;background:#f7f6f2;color:#67645d}.script-section pre{max-height:190px;border:1px solid #ddd}.dialog-footer{display:flex;justify-content:flex-end;gap:8px;padding:9px 14px;border-top:1px solid #d1cec6;background:#f5f3ed}.picker-dialog{width:min(560px,94vw)}.picker-content{padding:12px;overflow:auto;min-height:180px}.picker-search{display:grid;grid-template-columns:70px 1fr;align-items:center;gap:8px;margin-bottom:10px}.picker-row{display:block;width:100%;text-align:left;margin:4px 0;padding:8px 10px!important;background:#fff!important}.picker-row:hover{background:#fff2bf!important}.picker-row small{color:#777}.manual-folder{display:grid;gap:4px;margin-top:12px}.picker-backdrop{z-index:60}
@media(max-width:900px){.filter-panel{flex-wrap:wrap}.filter-item{flex:1;min-width:210px}.form-grid{grid-template-columns:1fr 1fr}.audit-block{grid-column:span 2;grid-template-columns:110px 1fr}.two-column-layout{grid-template-columns:1fr}.data-grid{min-width:1000px}}
.list-layout{display:flex;flex:1;min-height:155px}.side-actions{width:94px;display:flex;flex-direction:column;gap:7px;padding:8px;border-left:1px solid #ddd}.side-actions button{min-height:33px}.side-actions .close-action{margin-top:auto}
</style>
