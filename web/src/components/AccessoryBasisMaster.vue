<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

type IssueState = 0 | 1 | 2 | -1
type EditMode = 'add' | 'edit' | 'copy' | null
interface AccessoryRow {
  accessoryNo: string
  accessoryType: string
  vendorNo: string
  accessoryVersion: string
  issueState: IssueState
  curVersion: number
  dataStamp: number
  [key: string]: string | number | null
}
interface AccessoryProperty {
  accessory: string
  accessoryVersion: string
  propertyNo: string
  defaultValue: string | null
  propertySequence: number | null
  description: string | null
  issueState: IssueState
  dataStamp: number
  creator: string | null
  createDate: string | null
  elementName?: string | null
  reviser?: string | null
  reviseDate?: string | null
}
interface ImportPreview {
  accessoryNo: string
  accessoryType: string
  accessoryCategory: string
  vendorNo: string
  modelNo: string
  assetNo: string
  description: string
  result: string
  message: string
}
const seedProperties: AccessoryProperty[] = [
  { accessory: 'ACC-DEV-001', accessoryVersion: '01', propertyNo: 'SIZE', elementName: '尺寸', defaultValue: '10', propertySequence: 1, description: '開發用尺寸屬性', issueState: 0, dataStamp: 0, creator: 'DEV001', createDate: '2026-11-10 09:10', reviser: null, reviseDate: null },
  { accessory: 'ACC-DEV-001', accessoryVersion: '01', propertyNo: 'MATERIAL', elementName: '材質', defaultValue: 'SUS304', propertySequence: 2, description: '治具材料', issueState: 2, dataStamp: 1, creator: 'DEV001', createDate: '2026-11-10 09:10', reviser: 'DEV002', reviseDate: '2026-11-11 10:00' },
]
const props = defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mock = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const storageKey = 'ymes-dev-mock:accessory-basis:ep203:v1'
const propertyKey = 'ymes-dev-mock:accessory-basis-properties:ep203:v1'
const statusOptions: { value: IssueState; label: string }[] = [
  { value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' },
]
const columns: { key: string; label: string }[] = [
  { key: 'accessoryNo', label: '配件編號' }, { key: 'accessoryType', label: '配件類別資料' },
  { key: 'vendorNo', label: '供應商編號' }, { key: 'modelNo', label: '型號' },
  { key: 'description', label: '說明' }, { key: 'creator', label: '建立人' },
  { key: 'createDate', label: '建立日期' }, { key: 'assetNo', label: '財產編號' },
  { key: 'accessoryCategory', label: '配件分類' }, { key: 'erpNo', label: 'ERP 編號' },
  { key: 'reviser', label: '修改人' }, { key: 'reviseDate', label: '修改日期' },
]
for (let i = 1; i <= 24; i++) columns.push({ key: 'xxPara' + i, label: 'XX_PARA' + i })
columns.push(
  { key: 'getAnsNo', label: '取得答案編號' }, { key: 'ansStatus', label: '答案狀態' },
  { key: 'accFilePath1', label: '配件檔案路徑 1' }, { key: 'accFilePath2', label: '配件檔案路徑 2' },
  { key: 'accFilePath3', label: '配件檔案路徑 3' }, { key: 'accessoryState', label: '配件狀態' },
  { key: 'getGroupNo', label: '取得群組編號' }, { key: 'maintenanceTime', label: '維護時間' },
  { key: 'beGroupMainten', label: '群組維護' },
)
function makeSeed(): AccessoryRow[] {
  const sample: AccessoryRow[] = [
    { accessoryNo: 'ACC-DEV-001', accessoryType: 'DEV-ACC-01', vendorNo: 'DEV-V001', accessoryVersion: '01', issueState: 0, curVersion: 0, dataStamp: 0, modelNo: 'JIG-A', description: '開發用定位治具', creator: 'DEV001', createDate: '2026-11-10 09:10', assetNo: 'ASSET-001', accessoryCategory: '治具', erpNo: '', reviser: '', reviseDate: '', accessoryState: 0, getGroupNo: '', maintenanceTime: '', beGroupMainten: 'N' },
    { accessoryNo: 'ACC-DEV-002', accessoryType: 'DEV-ACC-02', vendorNo: 'DEV-V002', accessoryVersion: '01', issueState: 2, curVersion: 1, dataStamp: 2, modelNo: 'M-200', description: '開發用耗材樣例', creator: 'DEV001', createDate: '2026-11-08 14:30', assetNo: '', accessoryCategory: '耗材', erpNo: 'ERP-200', reviser: 'DEV002', reviseDate: '2026-11-09 10:00', accessoryState: 0, getGroupNo: '', maintenanceTime: '', beGroupMainten: 'N' },
    { accessoryNo: 'ACC-DEV-003', accessoryType: 'DEV-ACC-01', vendorNo: 'DEV-V001', accessoryVersion: '01', issueState: 1, curVersion: 0, dataStamp: 1, modelNo: '', description: '開發用簽核中範例', creator: 'DEV001', createDate: '2026-11-07 11:20', assetNo: '', accessoryCategory: '治具', erpNo: '', reviser: 'DEV002', reviseDate: '2026-11-08 08:30', accessoryState: 0, getGroupNo: '', maintenanceTime: '', beGroupMainten: 'N' },
    { accessoryNo: 'ACC-DEV-004', accessoryType: 'DEV-ACC-02', vendorNo: 'DEV-V002', accessoryVersion: '01', issueState: -1, curVersion: 0, dataStamp: 1, modelNo: '', description: '開發用停用範例', creator: 'DEV001', createDate: '2026-11-06 16:00', assetNo: '', accessoryCategory: '耗材', erpNo: '', reviser: 'DEV002', reviseDate: '2026-11-07 10:00', accessoryState: 0, getGroupNo: '', maintenanceTime: '', beGroupMainten: 'N' },
  ]
  for (const row of sample) for (let i = 1; i <= 24; i++) row['xxPara' + i] = ''
  return sample
}
const rows = ref<AccessoryRow[]>([])
const properties = ref<AccessoryProperty[]>([])
const status = ref<IssueState>(0)
const searchText = ref('')
const appliedSearch = ref('')
const activeKey = ref('')
const selectedKeys = ref<string[]>([])
const loading = ref(false)
const busy = ref(false)
const error = ref('')
const notice = ref('')
const mode = ref<EditMode>(null)
const form = ref({ accessoryNo: '', accessoryVersion: '01', accessoryType: '', accessoryCategory: '', vendorNo: '', modelNo: '', assetNo: '', description: '' })
const audit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const lookupKind = ref<'type' | 'vendor' | 'property' | null>(null)
const typeOptions = ref<{ accessoryType: string; accessoryCategory: string }[]>([
  { accessoryType: 'DEV-ACC-01', accessoryCategory: '治具' }, { accessoryType: 'DEV-ACC-02', accessoryCategory: '耗材' },
])
const vendorOptions = ref<string[]>(['DEV-V001', 'DEV-V002', 'DEV-V003'])
const propertyOptions = ref<string[]>(['SIZE', 'MATERIAL', 'SPEC'])
const propertyOpen = ref(false)
const propertyMode = ref<EditMode>(null)
const propertyActive = ref('')
const propertyForm = ref({ propertyNo: '', defaultValue: '', propertySequence: '1', description: '' })
const importOpen = ref(false)
const importFileName = ref('')
const importPreviewToken = ref<string | null>(null)
const importRows = ref<ImportPreview[]>([])
function rowKey(row: AccessoryRow): string { return row.accessoryNo + '|' + row.accessoryVersion }
const visibleRows = computed(() => rows.value
  .filter(row => row.issueState === status.value)
  .filter(row => !appliedSearch.value || row.accessoryNo.toLowerCase().includes(appliedSearch.value.toLowerCase()))
  .sort((a, b) => a.accessoryType.localeCompare(b.accessoryType) || a.accessoryNo.localeCompare(b.accessoryNo)))
const current = computed(() => rows.value.find(row => rowKey(row) === activeKey.value) || null)
const targets = computed(() => {
  const checked = rows.value.filter(row => selectedKeys.value.includes(rowKey(row)))
  return checked.length ? checked : (current.value ? [current.value] : [])
})
const canEdit = computed(() => targets.value.length === 1 && targets.value[0].issueState === 0)
const canCopy = computed(() => !!current.value)
const canApprove = computed(() => targets.value.length > 0 && targets.value.every(row => row.issueState === 0))
const canDelete = computed(() => targets.value.length > 0 && targets.value.every(row => [0, 2, -1].includes(row.issueState)))
function freshDate(): string { return new Date().toLocaleString('zh-TW', { hour12: false }) }
function readLocal<T>(key: string, seed: T[]): T[] {
  try { const value = localStorage.getItem(key); return value ? JSON.parse(value) as T[] : structuredClone(seed) }
  catch { return structuredClone(seed) }
}
function writeLocal<T>(key: string, data: T[]): void { localStorage.setItem(key, JSON.stringify(data)) }
function apiPath(path: string): string {
  if (!props.apiBaseUrl.trim()) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(props.apiBaseUrl.replace(/\/+$/, '') + '/', location.origin)).toString()
}
async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiPath(path), { credentials: 'include', ...init })
  if (response.status === 401) { location.assign(appBase + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) {
    const body = await response.json().catch(() => null) as { detail?: string; title?: string; message?: string } | null
    if (response.status === 404) throw new Error('EP203 API 尚未建立，請依 todolist_homepc.md 完成資料表與 API。')
    throw new Error(body?.detail || body?.message || body?.title || 'API 回應錯誤 (' + response.status + ')。')
  }
  return response.status === 204 ? undefined as T : await response.json() as T
}
async function load(): Promise<void> {
  loading.value = true; error.value = ''
  try {
    if (mock.value) rows.value = readLocal(storageKey, makeSeed())
    else rows.value = await api<AccessoryRow[]>('api/equipment/accessory-basis?issueState=all&currentOnly=false')
    selectedKeys.value = selectedKeys.value.filter(key => rows.value.some(row => rowKey(row) === key))
    if (!visibleRows.value.some(row => rowKey(row) === activeKey.value)) activeKey.value = visibleRows.value[0] ? rowKey(visibleRows.value[0]) : ''
  } catch (e) { rows.value = []; error.value = e instanceof Error ? e.message : '配件基本資料載入失敗。' }
  finally { loading.value = false }
}
async function loadLookups(): Promise<void> {
  if (mock.value) {
    typeOptions.value = [{ accessoryType: 'DEV-ACC-01', accessoryCategory: '治具' }, { accessoryType: 'DEV-ACC-02', accessoryCategory: '耗材' }]
    vendorOptions.value = ['DEV-V001', 'DEV-V002', 'DEV-V003']
    propertyOptions.value = ['SIZE', 'MATERIAL', 'SPEC']
    return
  }
  typeOptions.value = []; vendorOptions.value = []; propertyOptions.value = []
  try {
    const result = await api<{ accessoryType: string; accessoryCategory: string }[]>('api/equipment/accessory-types?issueState=2')
    typeOptions.value = result.map(row => ({ accessoryType: row.accessoryType, accessoryCategory: row.accessoryCategory }))
  } catch { /* API lookup error is shown when the user opens the selector. */ }
  try {
    const result = await api<{ vendorNo: string }[]>('api/equipment/vendors?issueState=2')
    vendorOptions.value = result.map(row => row.vendorNo)
  } catch { /* API lookup error is shown when the user opens the selector. */ }
  try { const result = await api<{ propertyNo: string }[]>('api/equipment/accessory-properties/definitions?propertyType=1'); propertyOptions.value = result.map(row => row.propertyNo) } catch { /* Property definitions are supplied by the home-PC API. */ }
}
function query(): void { appliedSearch.value = searchText.value.trim(); selectedKeys.value = []; void load() }
function selectRow(row: AccessoryRow): void { activeKey.value = rowKey(row); error.value = '' }
function toggleRow(row: AccessoryRow): void {
  selectedKeys.value = selectedKeys.value.includes(rowKey(row))
    ? selectedKeys.value.filter(key => key !== rowKey(row)) : [...selectedKeys.value, rowKey(row)]
  activeKey.value = rowKey(row)
}
function openForm(next: Exclude<EditMode, null>): void {
  const source = current.value
  if (next !== 'add' && !source) { error.value = '請先選取一筆配件資料。'; return }
  if (next === 'edit' && source?.issueState !== 0) { error.value = '只有未簽核資料可以修改。'; return }
  mode.value = next
  form.value = next === 'edit' && source
    ? { accessoryNo: source.accessoryNo, accessoryVersion: source.accessoryVersion, accessoryType: source.accessoryType, accessoryCategory: String(source.accessoryCategory || ''), vendorNo: source.vendorNo, modelNo: String(source.modelNo || ''), assetNo: String(source.assetNo || ''), description: String(source.description || '') }
    : { accessoryNo: '', accessoryVersion: '01', accessoryType: next === 'copy' && source ? source.accessoryType : '', accessoryCategory: next === 'copy' && source ? String(source.accessoryCategory || '') : '', vendorNo: next === 'copy' && source ? source.vendorNo : '', modelNo: next === 'copy' && source ? String(source.modelNo || '') : '', assetNo: next === 'copy' && source ? String(source.assetNo || '') : '', description: next === 'copy' && source ? String(source.description || '') : '' }
  audit.value = next === 'edit' && source
    ? { creator: String(source.creator || ''), createDate: String(source.createDate || ''), reviser: props.currentUserNo || '', reviseDate: freshDate() }
    : { creator: props.currentUserNo || 'DEV001', createDate: freshDate(), reviser: '', reviseDate: '' }
  error.value = ''
}
function validateForm(): string {
  if (!form.value.accessoryNo.trim()) return '配件編號為必填。'
  if (!form.value.accessoryType.trim()) return '配件類別資料為必填。'
  if (!form.value.vendorNo.trim()) return '供應商編號為必填。'
  if (form.value.accessoryNo.length > 50 || form.value.accessoryType.length > 50 || form.value.vendorNo.length > 50) return '配件編號、類別或供應商編號超過 50 個字元。'
  if (form.value.accessoryVersion.length > 3) return '配件版本不可超過 3 個字元。'
  if (form.value.modelNo.length > 50 || form.value.assetNo.length > 50 || form.value.description.length > 255) return '型號或財產編號最多 50 字元，說明最多 255 字元。'
  const id = form.value.accessoryNo.trim().toUpperCase()
  if (mode.value !== 'edit' && rows.value.some(row => row.accessoryNo.toUpperCase() === id && row.accessoryVersion === form.value.accessoryVersion)) return '配件編號與版本已存在。'
  return ''
}
async function saveMain(): Promise<void> {
  if (!mode.value) return
  const problem = validateForm(); if (problem) { error.value = problem; return }
  busy.value = true; error.value = ''
  const action = mode.value; const source = current.value
  const id = form.value.accessoryNo.trim().toUpperCase(); const version = form.value.accessoryVersion.trim().toUpperCase()
  try {
    if (mock.value) {
      const all = readLocal(storageKey, makeSeed()); const stamp = new Date().toISOString()
      if (action === 'edit' && source) {
        const i = all.findIndex(row => row.accessoryNo === source.accessoryNo && row.accessoryVersion === source.accessoryVersion)
        if (i < 0) throw new Error('找不到要修改的資料。')
        all[i] = { ...all[i], accessoryType: form.value.accessoryType, accessoryCategory: form.value.accessoryCategory, vendorNo: form.value.vendorNo, modelNo: form.value.modelNo || null, assetNo: form.value.assetNo || null, description: form.value.description || null, dataStamp: all[i].dataStamp + 1, reviser: props.currentUserNo || 'DEV001', reviseDate: stamp }
      } else {
        const created: AccessoryRow = { accessoryNo: id, accessoryVersion: version, accessoryType: form.value.accessoryType, accessoryCategory: form.value.accessoryCategory, vendorNo: form.value.vendorNo, modelNo: form.value.modelNo || null, assetNo: form.value.assetNo || null, description: form.value.description || null, issueState: 0, curVersion: 0, dataStamp: 0, erpNo: '', creator: props.currentUserNo || 'DEV001', createDate: stamp, reviser: null, reviseDate: null }
        for (let i = 1; i <= 24; i++) created['xxPara' + i] = ''
        all.push(created)
        if (action === 'copy' && source) {
          const sourceProperties = readLocal(propertyKey, seedProperties).filter(row => row.accessory === source.accessoryNo && row.accessoryVersion === source.accessoryVersion && row.issueState === 2)
          const copy = sourceProperties.map(row => ({ ...row, accessory: id, accessoryVersion: version, issueState: 0 as IssueState, dataStamp: 0, creator: props.currentUserNo || 'DEV001', createDate: stamp, reviser: null, reviseDate: null }))
          writeLocal(propertyKey, [...readLocal(propertyKey, seedProperties).filter(row => row.accessory !== id), ...copy])
        }
      }
      writeLocal(storageKey, all); rows.value = all
    } else {
      const body = { accessoryNo: id, accessoryVersion: version, accessoryType: form.value.accessoryType, accessoryCategory: form.value.accessoryCategory || null, vendorNo: form.value.vendorNo, modelNo: form.value.modelNo || null, assetNo: form.value.assetNo || null, description: form.value.description || null }
      if (action === 'edit' && source) await api<void>('api/equipment/accessory-basis/' + encodeURIComponent(source.accessoryNo) + '/' + encodeURIComponent(source.accessoryVersion), { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ ...body, issueState: source.issueState, dataStamp: source.dataStamp }) })
      else await api<void>('api/equipment/accessory-basis', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
      if (action === 'copy' && source) await api<void>('api/equipment/accessory-basis/copy-properties', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ fromAccessoryNo: source.accessoryNo, fromAccessoryVersion: source.accessoryVersion, toAccessoryNo: id, toAccessoryVersion: version }) })
    }
    mode.value = null; activeKey.value = id + '|' + version; selectedKeys.value = []; await load(); notice.value = '配件基本資料已儲存。'
  } catch (e) { error.value = e instanceof Error ? e.message : '儲存配件基本資料失敗。' }
  finally { busy.value = false }
}
async function removeRows(): Promise<void> {
  const list = targets.value
  if (!canDelete.value || !list.length || !window.confirm('確定刪除選取的配件資料？')) return
  busy.value = true; error.value = ''
  try {
    if (mock.value) {
      const all = readLocal(storageKey, makeSeed()); const hardKeys = new Set(list.filter(row => row.issueState !== 2).map(row => row.accessoryNo + '|' + row.accessoryVersion))
      const inactiveKeys = new Set(list.filter(row => row.issueState === 2).map(row => row.accessoryNo + '|' + row.accessoryVersion))
      const stamp = new Date().toISOString()
      rows.value = all.filter(row => !hardKeys.has(row.accessoryNo + '|' + row.accessoryVersion)).map(row => inactiveKeys.has(row.accessoryNo + '|' + row.accessoryVersion) ? { ...row, issueState: -1 as IssueState, dataStamp: row.dataStamp + 1, reviser: props.currentUserNo || 'DEV001', reviseDate: stamp } : row)
      writeLocal(storageKey, rows.value)
      writeLocal(propertyKey, readLocal(propertyKey, seedProperties).filter(row => !hardKeys.has(row.accessory + '|' + row.accessoryVersion)))
    } else {
      for (const row of list) await api<void>('api/equipment/accessory-basis/delete', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ accessoryNo: row.accessoryNo, accessoryVersion: row.accessoryVersion, issueState: row.issueState, dataStamp: row.dataStamp }) })
    }
    selectedKeys.value = []; await load(); notice.value = '配件資料已刪除或停用。'
  } catch (e) { error.value = e instanceof Error ? e.message : '刪除失敗。' }
  finally { busy.value = false }
}
async function approveRows(): Promise<void> {
  const list = targets.value
  if (!canApprove.value || !list.length || !window.confirm('確定送出選取的配件資料核准？')) return
  busy.value = true; error.value = ''
  try {
    if (mock.value) {
      const ids = new Set(list.map(row => row.accessoryNo + '|' + row.accessoryVersion)); const stamp = new Date().toISOString()
      rows.value = readLocal(storageKey, makeSeed()).map(row => ids.has(row.accessoryNo + '|' + row.accessoryVersion) ? { ...row, issueState: 2 as IssueState, curVersion: 1, dataStamp: row.dataStamp + 1, reviser: props.currentUserNo || 'DEV001', reviseDate: stamp } : row)
      writeLocal(storageKey, rows.value)
    } else for (const row of list) await api<void>('api/equipment/accessory-basis/approve', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ accessoryNo: row.accessoryNo, accessoryVersion: row.accessoryVersion, issueState: row.issueState, dataStamp: row.dataStamp }) })
    selectedKeys.value = []; await load(); notice.value = '已送出核准；正式簽核狀態由 API/簽核流程決定。'
  } catch (e) { error.value = e instanceof Error ? e.message : '核准失敗。' }
  finally { busy.value = false }
}
async function openProperties(): Promise<void> {
  const selected = current.value
  if (!selected) { error.value = '請先選取一筆配件資料。'; return }
  propertyOpen.value = true; propertyActive.value = ''; error.value = ''
  try {
    properties.value = mock.value
      ? readLocal(propertyKey, seedProperties).filter(row => row.accessory === selected.accessoryNo && row.accessoryVersion === selected.accessoryVersion)
      : await api<AccessoryProperty[]>('api/equipment/accessory-basis/' + encodeURIComponent(selected.accessoryNo) + '/' + encodeURIComponent(selected.accessoryVersion) + '/properties')
  } catch (e) { properties.value = []; error.value = e instanceof Error ? e.message : '配件屬性載入失敗。' }
}
function openPropertyForm(next: Exclude<EditMode, null>): void {
  const row = properties.value.find(item => item.propertyNo === propertyActive.value)
  if (next !== 'add' && !row) { error.value = '請先選取一筆屬性。'; return }
  if (next === 'edit' && row?.issueState !== 0) { error.value = '只有未簽核的屬性可以修改。'; return }
  propertyMode.value = next
  propertyForm.value = row && next !== 'add' ? { propertyNo: next === 'edit' ? row.propertyNo : '', defaultValue: row.defaultValue || '', propertySequence: String(row.propertySequence ?? 1), description: row.description || '' } : { propertyNo: '', defaultValue: '', propertySequence: '1', description: '' }
}
async function saveProperty(): Promise<void> {
  const selectedAccessory = current.value
  if (!selectedAccessory || !propertyMode.value) return
  if (!propertyForm.value.propertyNo.trim()) { error.value = '屬性編號為必填。'; return }
  if (propertyForm.value.propertyNo.length > 20 || propertyForm.value.defaultValue.length > 255 || propertyForm.value.description.length > 255) { error.value = '欄位超過資料庫長度限制。'; return }
  if (Number(propertyForm.value.propertySequence) < 0 || Number(propertyForm.value.propertySequence) > 99) { error.value = '屬性順序需介於 0 至 99。'; return }
  if (propertyMode.value !== 'edit' && properties.value.some(row => row.propertyNo.toUpperCase() === propertyForm.value.propertyNo.trim().toUpperCase())) { error.value = '屬性編號已存在。'; return }
  const action = propertyMode.value; const propertyNo = propertyForm.value.propertyNo.trim().toUpperCase(); const original = properties.value.find(row => row.propertyNo === propertyActive.value)
  try {
    if (mock.value) {
      const all = readLocal(propertyKey, seedProperties); const stamp = new Date().toISOString()
      if (action === 'edit') {
        const i = all.findIndex(row => row.accessory === selectedAccessory.accessoryNo && row.accessoryVersion === selectedAccessory.accessoryVersion && row.propertyNo === propertyActive.value)
        if (i >= 0) all[i] = { ...all[i], defaultValue: propertyForm.value.defaultValue || null, propertySequence: Number(propertyForm.value.propertySequence), description: propertyForm.value.description || null, dataStamp: all[i].dataStamp + 1, reviser: props.currentUserNo || 'DEV001', reviseDate: stamp }
      } else all.push({ accessory: selectedAccessory.accessoryNo, accessoryVersion: selectedAccessory.accessoryVersion, propertyNo, defaultValue: propertyForm.value.defaultValue || null, propertySequence: Number(propertyForm.value.propertySequence), description: propertyForm.value.description || null, issueState: 0, dataStamp: 0, creator: props.currentUserNo || 'DEV001', createDate: stamp, reviser: null, reviseDate: null })
      writeLocal(propertyKey, all)
    } else {
      const url = 'api/equipment/accessory-basis/' + encodeURIComponent(selectedAccessory.accessoryNo) + '/' + encodeURIComponent(selectedAccessory.accessoryVersion) + '/properties'
      const body = { propertyNo, defaultValue: propertyForm.value.defaultValue || null, propertySequence: Number(propertyForm.value.propertySequence), description: propertyForm.value.description || null, issueState: original?.issueState ?? 0, dataStamp: original?.dataStamp ?? 0 }
      await api<void>(action === 'edit' ? url + '/' + encodeURIComponent(propertyActive.value) : url, { method: action === 'edit' ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
    }
    propertyMode.value = null; await openProperties(); notice.value = '屬性已儲存。'
  } catch (e) { error.value = e instanceof Error ? e.message : '屬性儲存失敗。' }
}
async function deleteProperty(): Promise<void> {
  const selectedAccessory = current.value; const row = properties.value.find(item => item.propertyNo === propertyActive.value)
  if (!selectedAccessory || !row) { error.value = '請先選取一筆屬性。'; return }
  if (![0, 2, -1].includes(row.issueState)) { error.value = '簽核中的屬性不可刪除。'; return }
  if (!window.confirm('確定刪除選取的屬性？')) return
  try {
    if (mock.value) writeLocal(propertyKey, readLocal(propertyKey, seedProperties).filter(item => !(item.accessory === row.accessory && item.accessoryVersion === row.accessoryVersion && item.propertyNo === row.propertyNo)))
    else await api<void>('api/equipment/accessory-basis/' + encodeURIComponent(row.accessory) + '/' + encodeURIComponent(row.accessoryVersion) + '/properties/' + encodeURIComponent(row.propertyNo), { method: 'DELETE', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ issueState: row.issueState, dataStamp: row.dataStamp }) })
    await openProperties(); notice.value = '屬性已刪除。'
  } catch (e) { error.value = e instanceof Error ? e.message : '刪除屬性失敗。' }
}
async function approveProperty(): Promise<void> {
  const row = properties.value.find(item => item.propertyNo === propertyActive.value)
  if (!row || row.issueState !== 0) { error.value = '請選取未簽核屬性。'; return }
  try {
    if (mock.value) writeLocal(propertyKey, readLocal(propertyKey, seedProperties).map(item => item.accessory === row.accessory && item.accessoryVersion === row.accessoryVersion && item.propertyNo === row.propertyNo ? { ...item, issueState: 2 as IssueState, dataStamp: item.dataStamp + 1, reviser: props.currentUserNo || 'DEV001', reviseDate: new Date().toISOString() } : item))
    else await api<void>('api/equipment/accessory-basis/' + encodeURIComponent(row.accessory) + '/' + encodeURIComponent(row.accessoryVersion) + '/properties/' + encodeURIComponent(row.propertyNo) + '/approve', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ issueState: row.issueState, dataStamp: row.dataStamp }) })
    await openProperties(); notice.value = '已送出屬性核准。'
  } catch (e) { error.value = e instanceof Error ? e.message : '核准屬性失敗。' }
}
function parseCsvLine(line: string): string[] {
  const values: string[] = []; let value = ''; let quoted = false
  for (let i = 0; i < line.length; i++) {
    const ch = line[i]
    if (ch === '"' && line[i + 1] === '"' && quoted) { value += '"'; i++ }
    else if (ch === '"') quoted = !quoted
    else if (ch === ',' && !quoted) { values.push(value); value = '' }
    else value += ch
  }
  values.push(value); return values
}
async function previewImport(event: Event): Promise<void> {
  const file = (event.target as HTMLInputElement).files?.[0]
  importRows.value = []; importFileName.value = file?.name || ''
  if (!file) return
  const extension = file.name.toLowerCase().split('.').pop()
  importPreviewToken.value = null
  if (extension === 'xlsx' || extension === 'xls') {
    if (mock.value) { error.value = 'WinForms 使用 Excel 第一工作表七欄；Development 假資料模式目前只預覽 CSV，正式 API 負責解析 Excel。'; return }
    try {
      const data = new FormData(); data.append('file', file)
      const result = await api<{ previewToken: string; rows: ImportPreview[] }>('api/equipment/accessory-basis/import/preview', { method: 'POST', body: data })
      importPreviewToken.value = result.previewToken; importRows.value = result.rows
    } catch (e) { error.value = e instanceof Error ? e.message : 'Excel 預覽失敗。' }
    return
  }
  if (extension !== 'csv') { error.value = '僅支援 .xls、.xlsx 或 Development CSV。'; return }
  const lines = (await file.text()).split(/\r?\n/).filter(line => line.trim())
  const header = parseCsvLine(lines.shift() || '').map(value => value.trim().toLowerCase())
  const names = ['accessoryno', 'accessorytype', 'accessorycategory', 'vendorno', 'modelno', 'assetno', 'description']
  const seen = new Set<string>()
  importRows.value = lines.map(line => {
    const values = parseCsvLine(line); const get = (name: string) => values[header.indexOf(name)]?.trim() || ''
    const row: ImportPreview = { accessoryNo: get(names[0]), accessoryType: get(names[1]), accessoryCategory: get(names[2]), vendorNo: get(names[3]), modelNo: get(names[4]), assetNo: get(names[5]), description: get(names[6]), result: '可匯入', message: '' }
    if (!row.accessoryNo || !row.accessoryType || !row.accessoryCategory || !row.vendorNo) { row.result = '失敗'; row.message = '配件編號、類別、分類及供應商必填。' }
    else if (rows.value.some(item => item.accessoryNo.toUpperCase() === row.accessoryNo.toUpperCase()) || seen.has(row.accessoryNo.toUpperCase())) { row.result = '失敗'; row.message = '配件編號已存在或重複。' }
    else if (!typeOptions.value.some(item => item.accessoryType === row.accessoryType)) { row.result = '失敗'; row.message = '配件類別不存在或尚未核准。' }
    else if (typeOptions.value.find(item => item.accessoryType === row.accessoryType)?.accessoryCategory !== row.accessoryCategory) { row.result = '失敗'; row.message = '配件分類與類別設定不符。' }
    else if (!vendorOptions.value.includes(row.vendorNo)) { row.result = '失敗'; row.message = '供應商不存在或尚未核准。' }
    seen.add(row.accessoryNo.toUpperCase())
    return row
  })
  if (!importRows.value.length) error.value = '檔案沒有資料列。'
}
async function submitImport(): Promise<void> {
  const valid = importRows.value.filter(row => row.result === '可匯入')
  if (!valid.length) { error.value = '沒有可匯入的資料。'; return }
  try {
    if (mock.value) {
      const all = readLocal(storageKey, makeSeed()); const stamp = new Date().toISOString()
      for (const row of valid) {
        const item: AccessoryRow = { accessoryNo: row.accessoryNo.toUpperCase(), accessoryVersion: '01', accessoryType: row.accessoryType, accessoryCategory: row.accessoryCategory, vendorNo: row.vendorNo, modelNo: row.modelNo || null, assetNo: row.assetNo || null, description: row.description || null, issueState: 0, curVersion: 0, dataStamp: 0, creator: props.currentUserNo || 'DEV001', createDate: stamp, reviser: null, reviseDate: null }
        for (let i = 1; i <= 24; i++) item['xxPara' + i] = ''
        all.push(item)
      }
      writeLocal(storageKey, all)
    } else {
      if (importPreviewToken.value) await api<void>('api/equipment/accessory-basis/import/commit', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ previewToken: importPreviewToken.value }) })
      else await api<void>('api/equipment/accessory-basis/import', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ rows: valid }) })
    }
    importOpen.value = false; await load(); notice.value = '配件資料匯入完成。'
  } catch (e) { error.value = e instanceof Error ? e.message : '匯入失敗。' }
}
async function importTemplate(): Promise<void> {
  if (mock.value) { error.value = '範本由系統功能 ACC BASIS 提供；請由正式 API 下載 Excel 範本。'; return }
  try {
    const response = await fetch(apiPath('api/system/template-files?functionNo=ACC%20BASIS'), { credentials: 'include' })
    if (!response.ok) throw new Error('匯入範本 API 尚未建立。')
    const blob = await response.blob(); const url = URL.createObjectURL(blob); const a = document.createElement('a'); a.href = url; a.download = 'ACC_BASIS.xlsx'; a.click(); URL.revokeObjectURL(url)
  } catch (e) { error.value = e instanceof Error ? e.message : '範本下載失敗。' }
}
function selectLookup(item: string | { accessoryType: string; accessoryCategory: string }): void { if (typeof item === 'string') { if (lookupKind.value === 'property') propertyForm.value.propertyNo = item; else form.value.vendorNo = item } else { form.value.accessoryType = item.accessoryType; form.value.accessoryCategory = item.accessoryCategory } lookupKind.value = null }
function setAllRows(event: Event): void { selectedKeys.value = (event.target as HTMLInputElement).checked ? visibleRows.value.map(row => row.accessoryNo) : [] }
function closePage(): void { window.history.pushState({}, '', appBase + '/'); window.dispatchEvent(new PopStateEvent('popstate')) }

watch(status, () => { selectedKeys.value = []; activeKey.value = ''; void load() })
watch(() => props.developmentPreview, () => { void load(); void loadLookups() })
onMounted(() => { void load(); void loadLookups() })
</script>

<template>
  <main class="accessory-page">
    <header class="page-title">
      <h1>配件基本資料</h1>
      <span v-if="mock" class="source-tag">Development 假資料</span>
    </header>
    <section class="query-bar">
      <div class="status-filter">
        <label v-for="item in statusOptions" :key="item.value">
          <input v-model="status" type="radio" :value="item.value"> {{ item.label }}
        </label>
      </div>
      <label for="accessory-query">配件編號</label>
      <input id="accessory-query" v-model="searchText" class="query-input" @keyup.enter="query">
      <span>%</span>
      <button class="win-btn" @click="query">查詢</button>
    </section>
    <p v-if="mock" class="mock-note">本機 Development 假資料，只存於瀏覽器 localStorage；正式區固定呼叫 API。</p>
    <p v-if="error" class="message error">{{ error }}</p>
    <p v-if="notice" class="message success">{{ notice }}</p>

    <section class="workspace">
      <div class="grid-panel">

      <div class="grid-scroll">
        <table class="accessory-grid">
          <thead>
            <tr>
              <th class="check-cell"><input type="checkbox" :checked="visibleRows.length > 0 && visibleRows.every(row => selectedKeys.includes(rowKey(row)))" aria-label="全選" @change="setAllRows($event)"></th>
              <th v-for="column in columns" :key="column.key">{{ column.label }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="row in visibleRows" :key="row.accessoryNo + row.accessoryVersion" :class="{ selected: activeKey === rowKey(row) }" @click="selectRow(row)" @dblclick="row.issueState === 0 && openForm('edit')">
              <td class="check-cell" @click.stop><input type="checkbox" :checked="selectedKeys.includes(rowKey(row))" @change="toggleRow(row)" :aria-label="'選取 ' + row.accessoryNo"></td>
              <td v-for="column in columns" :key="column.key">
                {{ row[column.key] }}
              </td>
            </tr>
            <tr v-if="!visibleRows.length"><td :colspan="columns.length + 1" class="empty">{{ loading ? '載入中…' : '查無資料' }}</td></tr>
          </tbody>
        </table>
      </div>
      <footer class="grid-footer">資料總筆數 <b>{{ visibleRows.length }}</b></footer>
      </div>
      <div class="actions">
        <button class="win-btn primary" @click="openForm('add')">新增(A)</button>
        <button class="win-btn" :disabled="!canEdit" @click="openForm('edit')">修改(E)</button>
        <button class="win-btn" :disabled="!canDelete || busy" @click="removeRows">刪除(D)</button>
        <button class="win-btn" :disabled="!canCopy" @click="openForm('copy')">複製(C)</button>
        <button class="win-btn" :disabled="!canApprove || busy" @click="approveRows">核准</button>
        <button class="win-btn" :disabled="!current" @click="openProperties">屬性</button>
        <button class="win-btn" @click="importOpen = true">匯入</button>
        <button class="win-btn close" @click="closePage">關閉</button>
      </div>
    </section>

    <div v-if="mode" class="backdrop" @click.self="mode = null">
      <section class="dialog main-dialog">
        <header class="dialog-head">
          <h2>配件基本資料定義（{{ mode === 'add' ? '新增' : mode === 'edit' ? '修改' : '複製' }}）</h2>
          <button class="x" @click="mode = null">×</button>
        </header>
        <fieldset class="group">
          <legend>配件主檔資料</legend>
          <div class="form-grid">
            <label class="form-row"><span>配件編號</span><input v-model.trim="form.accessoryNo" maxlength="50" class="kd-input" :readonly="mode === 'edit'"></label>
            <label class="form-row"><span>配件類別資料</span><div class="picker"><input :value="form.accessoryType" readonly class="kd-input"><button class="win-btn" @click="lookupKind = 'type'">…</button></div></label>
            <label class="form-row"><span>供應商編號</span><div class="picker"><input :value="form.vendorNo" readonly class="kd-input"><button class="win-btn" @click="lookupKind = 'vendor'">…</button></div></label>

            <label class="form-row"><span>型號</span><input v-model.trim="form.modelNo" maxlength="50" class="kd-input"></label>
            <label class="form-row"><span>財產編號</span><input v-model.trim="form.assetNo" maxlength="50" class="kd-input"></label>
            <label class="form-row description-row"><span>說明</span><textarea v-model="form.description" maxlength="255" class="kd-input description"></textarea></label>
          </div>
        </fieldset>
        <div class="audit">
          <label><span>建立人</span><input :value="audit.creator" readonly></label>
          <label><span>建立日期</span><input :value="audit.createDate" readonly></label>
          <label><span>修改人</span><input :value="audit.reviser" readonly></label>
          <label><span>修改日期</span><input :value="audit.reviseDate" readonly></label>
        </div>
        <footer class="dialog-foot">
          <button class="win-btn primary" :disabled="busy" @click="saveMain">確定</button>
          <button class="win-btn" @click="mode = null">關閉</button>
          <button class="win-btn" :disabled="busy" @click="saveMain">套用</button>
        </footer>
      </section>
    </div>

    <div v-if="lookupKind" class="backdrop nested" @click.self="lookupKind = null">
      <section class="dialog lookup-dialog">
        <header class="dialog-head"><h2>{{ lookupKind === 'type' ? '選擇配件類別' : lookupKind === 'vendor' ? '選擇供應商' : '選擇屬性編號' }}</h2><button class="x" @click="lookupKind = null">×</button></header>
        <div class="lookup-list">
          <div v-if="lookupKind === 'type'">
          <button v-for="item in typeOptions" :key="item.accessoryType" class="lookup-option" @click="selectLookup(item)">{{ item.accessoryType }}<span>{{ item.accessoryCategory }}</span></button>
        </div>
        <div v-else-if="lookupKind === 'vendor'">
          <button v-for="item in vendorOptions" :key="item" class="lookup-option" @click="selectLookup(item)">{{ item }}</button>
        </div>
        <div v-else>
          <button v-for="item in propertyOptions" :key="item" class="lookup-option" @click="selectLookup(item)">{{ item }}</button>
        </div>
          <p v-if="(lookupKind === 'type' ? typeOptions.length : lookupKind === 'vendor' ? vendorOptions.length : propertyOptions.length) === 0" class="empty">沒有可選資料</p>
        </div>
      </section>
    </div>

    <div v-if="propertyOpen" class="backdrop" @click.self="propertyOpen = false">
      <section class="dialog sub-dialog">
        <header class="dialog-head"><h2>配件屬性｜{{ current?.accessoryNo }} / {{ current?.accessoryVersion }}</h2><button class="x" @click="propertyOpen = false">×</button></header>
        <div class="actions">
          <button class="win-btn" @click="openPropertyForm('add')">新增</button>
          <button class="win-btn" :disabled="!properties.find(row => row.propertyNo === propertyActive && row.issueState === 0)" @click="openPropertyForm('edit')">修改</button>
          <button class="win-btn" :disabled="!propertyActive" @click="openPropertyForm('copy')">複製</button>
          <button class="win-btn" :disabled="!propertyActive" @click="deleteProperty">刪除</button>
          <button class="win-btn" :disabled="!properties.find(row => row.propertyNo === propertyActive && row.issueState === 0)" @click="approveProperty">核准</button>
        </div>
        <div class="grid-scroll property-scroll">
          <table class="accessory-grid">
            <thead><tr><th>屬性編號</th><th>元素名稱</th><th>預設值</th><th>順序</th><th>說明</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th></tr></thead>
            <tbody>
              <tr v-for="row in properties" :key="row.propertyNo" :class="{ selected: propertyActive === row.propertyNo }" @click="propertyActive = row.propertyNo">
                <td>{{ row.propertyNo }}</td><td>{{ row.elementName }}</td><td>{{ row.defaultValue }}</td><td>{{ row.propertySequence }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser }}</td><td>{{ row.reviseDate }}</td>
              </tr>
              <tr v-if="!properties.length"><td colspan="9" class="empty">查無屬性</td></tr>
            </tbody>
          </table>
        </div>
        <footer class="dialog-foot"><button class="win-btn" @click="propertyOpen = false">關閉</button></footer>
      </section>
    </div>
    <div v-if="propertyMode" class="backdrop nested" @click.self="propertyMode = null">
      <form class="dialog property-form" @submit.prevent="saveProperty">
        <header class="dialog-head"><h2>屬性定義（{{ propertyMode === 'edit' ? '修改' : propertyMode === 'copy' ? '複製' : '新增' }}）</h2><button type="button" class="x" @click="propertyMode = null">×</button></header>
        <label class="form-row"><span>屬性編號</span><div class="picker"><input :value="propertyForm.propertyNo" readonly class="kd-input"><button type="button" class="win-btn" :disabled="propertyMode === 'edit'" @click="lookupKind = 'property'">…</button></div></label>
        <label class="form-row"><span>預設值</span><input v-model="propertyForm.defaultValue" maxlength="255" class="kd-input"></label>
        <label class="form-row"><span>屬性順序</span><input v-model="propertyForm.propertySequence" type="number" min="0" max="99" class="kd-input"></label>
        <label class="form-row"><span>說明</span><textarea v-model="propertyForm.description" maxlength="255" class="kd-input description"></textarea></label>
        <footer class="dialog-foot"><button class="win-btn primary">確定</button><button type="button" class="win-btn" @click="propertyMode = null">關閉</button></footer>
      </form>
    </div>

    <div v-if="importOpen" class="backdrop" @click.self="importOpen = false">
      <section class="dialog import-dialog">
        <header class="dialog-head"><h2>配件基本資料匯入</h2><button class="x" @click="importOpen = false">×</button></header>
        <div class="import-file"><input type="file" accept=".xlsx,.xls,.csv" @change="previewImport"><button class="win-btn" @click="importTemplate">下載範本</button></div>
        <p class="file-name">{{ importFileName }}</p>
        <div class="grid-scroll property-scroll">
          <table class="accessory-grid">
            <thead><tr><th>配件編號</th><th>類別</th><th>分類</th><th>供應商</th><th>型號</th><th>財產編號</th><th>說明</th><th>結果</th><th>訊息</th></tr></thead>
            <tbody>
              <tr v-for="(row, i) in importRows" :key="i"><td>{{ row.accessoryNo }}</td><td>{{ row.accessoryType }}</td><td>{{ row.accessoryCategory }}</td><td>{{ row.vendorNo }}</td><td>{{ row.modelNo }}</td><td>{{ row.assetNo }}</td><td>{{ row.description }}</td><td>{{ row.result }}</td><td>{{ row.message }}</td></tr>
              <tr v-if="!importRows.length"><td colspan="9" class="empty">請選擇匯入檔案</td></tr>
            </tbody>
          </table>
        </div>
        <footer class="dialog-foot"><button class="win-btn primary" :disabled="!importRows.some(row => row.result === '可匯入')" @click="submitImport">匯入</button><button class="win-btn" @click="importOpen = false">關閉</button></footer>
      </section>
    </div>
  </main>
</template>

<style scoped>
.accessory-page{display:flex;flex-direction:column;gap:9px;min-height:100%;padding:10px;background:#fff;color:#343434;font:14px "Microsoft JhengHei",Arial,sans-serif}
.page-title{display:flex;align-items:center;gap:12px;padding:2px 8px 8px;border-bottom:1px solid #d7d2c7}.page-title h1{margin:0;color:#3c3529;font-size:21px;font-weight:600}.source-tag{padding:3px 8px;border:1px solid #dfc56c;border-radius:3px;background:#fff4c6;color:#755916;font-size:12px}
.query-bar{display:flex;align-items:center;gap:9px;min-height:42px;padding:6px 10px;border:1px solid #d6d2c9;background:#faf9f6}.status-filter{display:flex;align-items:center;gap:11px;margin-right:9px}.status-filter label{white-space:nowrap}.status-filter input{accent-color:#d98b22;margin-right:3px}.query-input{box-sizing:border-box;width:190px;height:29px;padding:4px 7px;border:1px solid #8f8b82;background:#fff0ae}
.mock-note{margin:0;padding:5px 9px;border-left:3px solid #d5a535;background:#fff8df;color:#6e5a25;font-size:12px}.message{margin:0;padding:7px 10px;border-radius:3px;font-size:13px}.error{background:#fff1ee;color:#972c1b}.success{background:#edf6e9;color:#426b34}
.workspace{display:grid;grid-template-columns:minmax(0,1fr) 104px;gap:10px;min-height:480px;flex:1}.grid-panel{display:flex;flex-direction:column;min-width:0;min-height:0;overflow:hidden;padding:6px;border:1px solid #c9c6bf;background:#fff}.actions{display:flex;flex-direction:column;gap:7px;padding-top:2px}.actions .win-btn{width:100%;min-height:34px}.win-btn{min-height:29px;padding:4px 11px;border:1px solid #bdb9b0;border-radius:3px;background:linear-gradient(#fff,#f0eee9);color:#35332e;font:14px "Microsoft JhengHei",Arial,sans-serif;white-space:nowrap;cursor:pointer}.win-btn:hover:not(:disabled){background:#fff0c1;border-color:#c39b42}.win-btn:disabled{opacity:.48;cursor:not-allowed}.win-btn.primary{border-color:#e9a33b;background:#f8a32d;color:#fff}.win-btn.close{margin-top:auto}
.grid-scroll{flex:1;min-height:0;width:100%;overflow:auto}.accessory-grid{width:max-content;min-width:100%;border-collapse:collapse;table-layout:auto}.accessory-grid th,.accessory-grid td{height:29px;padding:4px 8px;border:1px solid #c9c6bf;text-align:left;white-space:nowrap}.accessory-grid th{position:sticky;top:0;background:#f5cd64;color:#39342a;font-weight:600;z-index:1}.accessory-grid tbody tr{cursor:pointer}.accessory-grid tbody tr:nth-child(even){background:#fbfaf7}.accessory-grid tbody tr:hover{background:#fff4cf}.accessory-grid tbody tr.selected{background:#ffe59a}.check-cell{width:30px;text-align:center!important}.check-cell input{accent-color:#d98b22}.empty{text-align:center!important;color:#777;padding:18px!important}.grid-footer{padding:7px 2px 0;color:#666;font-size:12px}.grid-footer b{margin-left:10px;font-weight:400}
.backdrop{position:fixed;inset:0;z-index:3000;display:grid;place-items:center;padding:18px;background:rgba(35,33,29,.38)}.backdrop.nested{z-index:3010}.dialog{width:min(920px,97vw);max-height:94vh;overflow:auto;padding:12px;border:1px solid #aaa69d;border-radius:4px;background:#fff;box-shadow:0 8px 30px #0003}.dialog-head{display:flex;align-items:center;justify-content:space-between;padding:4px 2px 10px;border-bottom:1px solid #d7d2c7}.dialog-head h2{margin:0;color:#40392d;font-size:18px;font-weight:600}.x{width:29px;height:27px;border:1px solid #c5c0b6;border-radius:3px;background:#f5f3ef;color:#565149;font-size:20px;line-height:20px;cursor:pointer}.group{margin:13px 0 10px;padding:12px 15px;border:1px solid #bcb8af}.group legend{padding:0 6px;color:#554a35}.form-grid{display:grid;grid-template-columns:1fr 1fr;column-gap:20px}.form-row{display:grid;grid-template-columns:115px minmax(130px,1fr);align-items:center;gap:8px;margin:8px 0}.form-row>span:first-child{text-align:right}.kd-input{box-sizing:border-box;width:100%;min-height:29px;padding:4px 7px;border:1px solid #8f8b82;background:#fff0ae;color:#25231e;font:14px "Microsoft JhengHei",Arial,sans-serif}.kd-input.readonly,.kd-input:read-only{background:#efeee9;color:#666}.description-row{grid-column:1/-1}.description{min-height:65px;resize:vertical}.picker{display:grid;grid-template-columns:minmax(0,1fr) 38px;gap:6px}.audit{display:grid;grid-template-columns:1fr 1fr;gap:9px;padding:9px;border:1px solid #d2cfc7}.audit label{display:grid;grid-template-columns:85px minmax(0,1fr);align-items:center;gap:8px}.audit input{min-width:0;height:27px;padding:3px 6px;border:1px solid #aaa69d;background:#efeee9;color:#666}.dialog-foot{display:flex;justify-content:flex-end;gap:7px;padding-top:12px}.dialog-foot .win-btn{min-width:84px}.lookup-dialog{width:min(520px,94vw)}.lookup-list{max-height:55vh;overflow:auto;padding-top:8px}.lookup-option{display:flex;width:100%;justify-content:space-between;padding:10px 12px;border:0;border-bottom:1px solid #ddd;background:#fff;text-align:left;cursor:pointer}.lookup-option:hover{background:#fff0ae}.sub-dialog{width:min(1060px,98vw)}.property-scroll{max-height:58vh;border:1px solid #c9c6bf}.property-form{width:min(560px,96vw)}.import-dialog{width:min(1200px,98vw)}.import-file{display:flex;align-items:center;gap:10px;margin:12px 0 4px}.file-name{min-height:18px;color:#666;font-size:12px}
@media(max-width:820px){.workspace{grid-template-columns:1fr}.actions{order:-1;flex-direction:row;flex-wrap:wrap}.actions .win-btn{width:auto}.query-bar,.status-filter{flex-wrap:wrap}.form-grid,.audit{grid-template-columns:1fr}.description-row{grid-column:auto}.win-btn.close{margin-top:0;margin-left:auto}.form-row{grid-template-columns:105px minmax(120px,1fr)}}
</style>
