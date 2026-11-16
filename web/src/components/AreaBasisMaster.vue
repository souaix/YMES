<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

interface AreaRow {
  areaNo: string; areaName: string; areaType: number; areaClass: number; baseAreaNo: string; factoryNo: string; departmentNo: string
  description: string; creator: string | null; createDate: string | null; issueState: number; areaCompose: number
  reviser: string | null; reviseDate: string | null; dataStamp: number | null; backgroundImage?: string | null
}
interface AreaForm {
  areaNo: string; areaName: string; areaType: number; areaClass: number; baseAreaNo: string; factoryNo: string
  departmentNo: string; description: string; areaCompose: number
}
interface LayoutNode {
  containAreaNo: string; objectNo: string; objectType: number; objectText: string; objectX: number; objectY: number
  objectLength: number; objectWidth: number; objectColor: number; objectAreaType: number; objectSequence: number
  borderColor: number; borderStyle: number; textColor: number; equipmentGroup?: string
}
interface LayoutOption { objectNo: string; objectType: number; objectText: string; objectAreaType: number; equipmentGroup?: string }
type FormMode = 'add' | 'edit' | 'copy' | null
type AreaClassMode = 'base' | 'functional' | 'line' | 'other'

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mockMode = computed(() => import.meta.env.DEV && props.developmentPreview)
const rowsKey = 'ymes-dev-mock:smd-area-basis:v1'
const layoutsKey = 'ymes-dev-mock:smd-area-layout:v1'
const issueStates = [{ value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' }]
const areaTypes = [{ value: 0, label: '作業區' }, { value: 1, label: '生產線' }, { value: 2, label: '其他' }]
const areaComposes = [{ value: 0, label: '功能型（並聯）' }, { value: 1, label: '線型（串聯）' }]
const classModes: { key: AreaClassMode; label: string; areaClass: number; areaType: number; areaCompose: number }[] = [
  { key: 'base', label: '基底區域', areaClass: 1, areaType: 2, areaCompose: 0 },
  { key: 'functional', label: '離散型功能區', areaClass: 0, areaType: 0, areaCompose: 0 },
  { key: 'line', label: '連續性生產線', areaClass: 0, areaType: 1, areaCompose: 1 },
  { key: 'other', label: '其他區域', areaClass: 0, areaType: 2, areaCompose: 0 },
]
const seedRows: AreaRow[] = [
  { areaNo: 'DEV-BASE', areaName: '開發用基底區域', areaType: 2, areaClass: 1, baseAreaNo: '', factoryNo: 'DEV-FAB', departmentNo: 'DEV-DEPT', description: 'Development 預覽用基底區域', creator: 'DEV001', createDate: '2026-11-10T08:00:00+08:00', issueState: 0, areaCompose: 0, reviser: null, reviseDate: null, dataStamp: 1 },
  { areaNo: 'DEV-OP-01', areaName: '開發用作業區', areaType: 0, areaClass: 0, baseAreaNo: 'DEV-BASE', factoryNo: 'DEV-FAB', departmentNo: 'DEV-DEPT', description: '虛擬作業區', creator: 'DEV001', createDate: '2026-11-11T08:00:00+08:00', issueState: 2, areaCompose: 0, reviser: 'DEV002', reviseDate: '2026-11-12T09:00:00+08:00', dataStamp: 2 },
  { areaNo: 'DEV-LINE', areaName: '開發用生產線', areaType: 1, areaClass: 0, baseAreaNo: 'DEV-BASE', factoryNo: 'DEV-FAB', departmentNo: 'DEV-DEPT', description: '虛擬連續生產線', creator: 'DEV001', createDate: '2026-11-12T08:00:00+08:00', issueState: 1, areaCompose: 1, reviser: 'DEV002', reviseDate: '2026-11-13T09:00:00+08:00', dataStamp: 1 },
  { areaNo: 'DEV-OTHER', areaName: '開發用其他區域', areaType: 2, areaClass: 0, baseAreaNo: 'DEV-BASE', factoryNo: 'DEV-FAB', departmentNo: 'DEV-DEPT', description: '', creator: 'DEV001', createDate: '2026-11-13T08:00:00+08:00', issueState: -1, areaCompose: 0, reviser: null, reviseDate: null, dataStamp: 3 },
]
const seedLayoutOptions: LayoutOption[] = [
  { objectNo: 'DEV-OP-01', objectType: 0, objectText: '開發用作業區', objectAreaType: 0 },
  { objectNo: 'DEV-LINE', objectType: 0, objectText: '開發用生產線', objectAreaType: 1 },
  { objectNo: 'DEV-INV-01', objectType: 1, objectText: '開發用庫房', objectAreaType: 0 },
  { objectNo: 'DEV-EQP-01', objectType: 2, objectText: '開發用設備 01', objectAreaType: 0 },
  { objectNo: 'DEV-EQPTYPE-01', objectType: 3, objectText: '開發用設備類別', objectAreaType: 0 },
  { objectNo: 'DEV-EQG-01', objectType: 4, objectText: '開發用設備群組', objectAreaType: 0, equipmentGroup: 'DEV-EQG-01' },
]
const seedLayout: LayoutNode[] = [
  { containAreaNo: 'DEV-BASE', objectNo: 'DEV-OP-01', objectType: 0, objectText: '開發用作業區', objectX: 80, objectY: 65, objectLength: 75, objectWidth: 170, objectColor: 4294951175, objectAreaType: 0, objectSequence: 1, borderColor: 0, borderStyle: 0, textColor: 0 },
  { containAreaNo: 'DEV-BASE', objectNo: 'DEV-LINE', objectType: 0, objectText: '開發用生產線', objectX: 330, objectY: 65, objectLength: 75, objectWidth: 190, objectColor: 4294951175, objectAreaType: 1, objectSequence: 2, borderColor: 0, borderStyle: 0, textColor: 0 },
  { containAreaNo: 'DEV-OP-01', objectNo: 'DEV-EQP-01', objectType: 2, objectText: '開發用設備 01', objectX: 105, objectY: 150, objectLength: 48, objectWidth: 105, objectColor: 4289003519, objectAreaType: 0, objectSequence: 1, borderColor: 0, borderStyle: 0, textColor: 0 },
]

const rows = ref<AreaRow[]>([])
const filterState = ref(0)
const searchText = ref('')
const selectedNos = ref<string[]>([])
const activeNo = ref('')
const selectionAnchorNo = ref('')
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const formError = ref('')
const formMode = ref<FormMode>(null)
const form = ref<AreaForm>(blankForm())
const mode = ref<AreaClassMode>('functional')
const audit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const formDataStamp = ref<number | null>(null)
const factories = ref<{ factoryNo: string; factoryName: string }[]>([])
const departments = ref<{ departmentNo: string; departmentName: string }[]>([])
const baseAreas = ref<AreaRow[]>([])
const layoutOpen = ref(false)
const layoutBusy = ref(false)
const layoutError = ref('')
const layoutSuccess = ref('')
const layoutBase = ref<AreaRow | null>(null)
const layoutNodes = ref<LayoutNode[]>([])
const layoutOptions = ref<LayoutOption[]>([])
const selectedLayoutNodeKey = ref('')
const newLayoutObjectKey = ref('')
const backgroundImageDataUrl = ref<string | null>(null)
const layoutDirty = ref(false)
const draggingKey = ref('')

function blankForm(): AreaForm { return { areaNo: '', areaName: '', areaType: 0, areaClass: 0, baseAreaNo: '', factoryNo: '', departmentNo: '', description: '', areaCompose: 0 } }
const visibleRows = computed(() => rows.value.filter(row => row.issueState === filterState.value)
  .filter(row => !searchText.value.trim() || row.areaNo.toLocaleLowerCase().includes(searchText.value.trim().toLocaleLowerCase()))
  .sort((a, b) => a.areaNo.localeCompare(b.areaNo)))
const activeRow = computed(() => rows.value.find(row => row.areaNo === activeNo.value) ?? null)
const allVisibleSelected = computed(() => visibleRows.value.length > 0 && visibleRows.value.every(row => selectedNos.value.includes(row.areaNo)))
const selectedLayoutNode = computed(() => layoutNodes.value.find(node => layoutNodeKey(node) === selectedLayoutNodeKey.value) ?? null)
const availableLayoutOptions = computed(() => layoutOptions.value.filter(option => !layoutNodes.value.some(node => node.objectNo === option.objectNo && node.objectType === option.objectType)))
const optionGroups = computed(() => [
  { label: '區域', options: availableLayoutOptions.value.filter(x => x.objectType === 0) },
  { label: '庫房', options: availableLayoutOptions.value.filter(x => x.objectType === 1) },
  { label: '設備', options: availableLayoutOptions.value.filter(x => x.objectType === 2) },
  { label: '設備類別', options: availableLayoutOptions.value.filter(x => x.objectType === 3) },
  { label: '設備群組', options: availableLayoutOptions.value.filter(x => x.objectType === 4) },
])

function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function requestJson<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init })
  if (response.status === 401) { window.location.assign(appBase + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    if (response.status === 404) throw new Error('區域基本資料 API 尚未建立；請依 todolist_homepc.md 完成 SMD SD104 API。')
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 回應錯誤 (${response.status})。`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function readRows(): AreaRow[] {
  try { const value = window.localStorage.getItem(rowsKey); return value ? JSON.parse(value) as AreaRow[] : structuredClone(seedRows) }
  catch { return structuredClone(seedRows) }
}
function saveRows(value: AreaRow[]): void { window.localStorage.setItem(rowsKey, JSON.stringify(value)) }
function readLayouts(): Record<string, { nodes: LayoutNode[]; backgroundImageDataUrl: string | null }> {
  try { const value = window.localStorage.getItem(layoutsKey); return value ? JSON.parse(value) as Record<string, { nodes: LayoutNode[]; backgroundImageDataUrl: string | null }> : {} }
  catch { return {} }
}
function saveLayouts(value: Record<string, { nodes: LayoutNode[]; backgroundImageDataUrl: string | null }>): void { window.localStorage.setItem(layoutsKey, JSON.stringify(value)) }
function formatDate(value: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat('zh-TW', { dateStyle: 'short', timeStyle: 'medium', hour12: false }).format(date)
}
function stateLabel(value: number): string { return issueStates.find(item => item.value === value)?.label ?? '未知' }
function areaTypeLabel(value: number): string { return areaTypes.find(item => item.value === value)?.label ?? String(value) }
function areaClassLabel(value: number): string { return value === 1 ? '基底區域' : '一般區域' }
function composeLabel(value: number): string { return areaComposes.find(item => item.value === value)?.label ?? String(value) }
function areaPath(no: string): string { return `api/smd/areas/${encodeURIComponent(no)}` }
function layoutNodeKey(node: LayoutNode): string { return `${node.objectType}/${node.objectNo}` }
function objectTypeLabel(value: number): string { return ({ 0: '區域', 1: '庫房', 2: '設備', 3: '設備類別', 4: '設備群組' } as Record<number, string>)[value] ?? `類型 ${value}` }
function colorCss(value: number): string { return `#${(Number(value) >>> 0 & 0xffffff).toString(16).padStart(6, '0')}` }

async function loadOptions(): Promise<void> {
  if (mockMode.value) {
    factories.value = [{ factoryNo: 'DEV-FAB', factoryName: 'Development 工廠' }]
    departments.value = [{ departmentNo: 'DEV-DEPT', departmentName: 'Development 部門' }]
    baseAreas.value = readRows().filter(row => row.areaClass === 1 && row.issueState === 2)
    return
  }
  try {
    const options = await requestJson<{ factories: typeof factories.value; departments: typeof departments.value; baseAreas: AreaRow[] }>('api/smd/area-basis/options')
    factories.value = options.factories
    departments.value = options.departments
    baseAreas.value = options.baseAreas
  } catch (error) { formError.value = error instanceof Error ? error.message : '區域選項載入失敗。' }
}

async function loadRows(): Promise<void> {
  loading.value = true; errorMessage.value = ''; successMessage.value = ''
  try {
    if (mockMode.value) {
      rows.value = readRows()
      if (!window.localStorage.getItem(rowsKey)) saveRows(rows.value)
    } else {
      const query = new URLSearchParams({ issueState: String(filterState.value) })
      if (searchText.value.trim()) query.set('areaNo', searchText.value.trim())
      rows.value = await requestJson<AreaRow[]>(`api/smd/areas?${query.toString()}`)
    }
    const visibleNos = new Set(visibleRows.value.map(row => row.areaNo))
    selectedNos.value = selectedNos.value.filter(no => visibleNos.has(no))
    if (!visibleNos.has(activeNo.value)) { activeNo.value = visibleRows.value[0]?.areaNo ?? ''; selectedNos.value = activeNo.value ? [activeNo.value] : [] }
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '區域資料載入失敗。' }
  finally { loading.value = false }
}

function selectRow(row: AreaRow, event: MouseEvent): void {
  const index = visibleRows.value.findIndex(item => item.areaNo === row.areaNo)
  if (event.shiftKey && selectionAnchorNo.value) {
    const anchor = visibleRows.value.findIndex(item => item.areaNo === selectionAnchorNo.value)
    selectedNos.value = anchor < 0 ? [row.areaNo] : visibleRows.value.slice(Math.min(anchor, index), Math.max(anchor, index) + 1).map(item => item.areaNo)
  } else if (event.ctrlKey || event.metaKey) {
    selectedNos.value = selectedNos.value.includes(row.areaNo) ? selectedNos.value.filter(no => no !== row.areaNo) : [...selectedNos.value, row.areaNo]
    selectionAnchorNo.value = row.areaNo
  } else { selectedNos.value = [row.areaNo]; selectionAnchorNo.value = row.areaNo }
  activeNo.value = row.areaNo
}
function toggleAll(checked: boolean): void {
  if (checked) selectedNos.value = [...new Set([...selectedNos.value, ...visibleRows.value.map(row => row.areaNo)])]
  else selectedNos.value = selectedNos.value.filter(no => !visibleRows.value.some(row => row.areaNo === no))
}
function selectedMode(row: AreaRow): AreaClassMode {
  return classModes.find(item => item.areaClass === row.areaClass && item.areaType === row.areaType && item.areaCompose === row.areaCompose)?.key ?? (row.areaClass === 1 ? 'base' : 'other')
}
function setMode(next: AreaClassMode): void {
  const preset = classModes.find(item => item.key === next)
  if (!preset) return
  const previous = mode.value
  mode.value = next
  form.value.areaClass = preset.areaClass
  form.value.areaType = preset.areaType
  form.value.areaCompose = preset.areaCompose
  if (next === 'base') { form.value.baseAreaNo = ''; form.value.factoryNo = '' }
  else if (previous === 'base') { form.value.baseAreaNo = ''; form.value.factoryNo = '' }
  else if (form.value.baseAreaNo) inheritFactory()
  formError.value = ''
}
function inheritFactory(): void {
  form.value.factoryNo = baseAreas.value.find(row => row.areaNo === form.value.baseAreaNo)?.factoryNo ?? ''
}

async function openForm(next: Exclude<FormMode, null>): Promise<void> {
  const source = activeRow.value
  if (next !== 'add' && !source) { errorMessage.value = '請先選取一筆區域資料。'; return }
  if (next === 'edit' && source?.issueState !== 0) { errorMessage.value = '只有未簽核區域可以修改。'; return }
  formError.value = ''; errorMessage.value = ''; successMessage.value = ''
  formMode.value = next
  await loadOptions()
  if (next === 'add') { form.value = blankForm(); mode.value = 'functional'; formDataStamp.value = null }
  else if (source) {
    form.value = { areaNo: source.areaNo, areaName: source.areaName, areaType: source.areaType, areaClass: source.areaClass, baseAreaNo: source.baseAreaNo ?? '', factoryNo: source.factoryNo, departmentNo: source.departmentNo ?? '', description: source.description ?? '', areaCompose: source.areaCompose }
    mode.value = selectedMode(source)
    formDataStamp.value = next === 'edit' ? source.dataStamp ?? 0 : null
    if (next === 'copy') form.value.areaNo = ''
  }
  const now = new Date().toISOString()
  audit.value = next === 'edit' && source
    ? { creator: source.creator ?? '', createDate: formatDate(source.createDate), reviser: props.currentUserNo || source.reviser || '', reviseDate: formatDate(new Date().toISOString()) }
    : { creator: props.currentUserNo || 'DEV001', createDate: formatDate(now), reviser: '', reviseDate: '' }
}

function validateForm(): string {
  form.value.areaNo = form.value.areaNo.trim().toLocaleUpperCase()
  form.value.areaName = form.value.areaName.trim()
  if (!form.value.areaNo) return '區域編號為必填。'
  if (form.value.areaNo.length > 20) return '區域編號最多 20 個字元。'
  if (!form.value.areaName) return '區域名稱為必填。'
  if (form.value.areaName.length > 50) return '區域名稱最多 50 個字元。'
  if (mode.value === 'base' && !form.value.factoryNo) return '基底區域必須選擇工廠。'
  if (mode.value !== 'base' && !form.value.baseAreaNo) return '一般區域必須選擇基底區域。'
  if (!form.value.departmentNo) return '部門為必填。'
  if (!form.value.factoryNo) return '工廠為必填；一般區域請先選擇所屬基底區域。'
  if (form.value.description.length > 255) return '說明最多 255 個字元。'
  if (![0, 1, 2].includes(form.value.areaType) || ![0, 1].includes(form.value.areaClass) || ![0, 1].includes(form.value.areaCompose)) return '區域類型、分類或配置代碼不正確。'
  return ''
}

async function saveForm(closeAfterSave: boolean): Promise<void> {
  if (!formMode.value || busy.value) return
  const validation = validateForm(); if (validation) { formError.value = validation; return }
  busy.value = true; formError.value = ''
  const now = new Date().toISOString()
  try {
    const body = { ...form.value, ...(formMode.value === 'edit' ? { dataStamp: formDataStamp.value } : {}) }
    let saved: AreaRow
    if (mockMode.value) {
      const current = readRows()
      if (formMode.value !== 'edit' && current.some(row => row.areaNo.toLocaleUpperCase() === body.areaNo.toLocaleUpperCase())) throw new Error('區域編號已存在。')
      const old = current.find(row => row.areaNo === body.areaNo)
      saved = { ...body, issueState: formMode.value === 'edit' ? old?.issueState ?? 0 : 0, creator: formMode.value === 'edit' ? old?.creator ?? null : props.currentUserNo || 'DEV001', createDate: formMode.value === 'edit' ? old?.createDate ?? null : now, reviser: formMode.value === 'edit' ? props.currentUserNo || 'DEV001' : null, reviseDate: formMode.value === 'edit' ? now : null, dataStamp: formMode.value === 'edit' ? (old?.dataStamp ?? 0) + 1 : 0 }
      const next = formMode.value === 'edit' ? current.map(row => row.areaNo === body.areaNo ? saved : row) : [...current, saved]
      saveRows(next); rows.value = next
    } else if (formMode.value === 'edit') {
      saved = await requestJson<AreaRow>(`${areaPath(body.areaNo)}?dataStamp=${encodeURIComponent(String(formDataStamp.value ?? 0))}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
      rows.value = rows.value.map(row => row.areaNo === saved.areaNo ? saved : row)
    } else {
      saved = await requestJson<AreaRow>('api/smd/areas', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
      rows.value = [...rows.value, saved].sort((a, b) => a.areaNo.localeCompare(b.areaNo))
    }
    activeNo.value = saved.areaNo; selectedNos.value = [saved.areaNo]; successMessage.value = '區域資料已儲存。'
    if (formMode.value === 'edit') {
      formDataStamp.value = saved.dataStamp ?? (formDataStamp.value ?? 0) + 1
      audit.value.reviser = saved.reviser ?? (props.currentUserNo || 'DEV001')
      audit.value.reviseDate = formatDate(saved.reviseDate ?? now)
    } else {
      audit.value.creator = saved.creator ?? (props.currentUserNo || 'DEV001')
      audit.value.createDate = formatDate(saved.createDate ?? now)
    }
    if (closeAfterSave) formMode.value = null
  } catch (error) { formError.value = error instanceof Error ? error.message : '區域資料儲存失敗。' }
  finally { busy.value = false }
}

async function approveSelected(): Promise<void> {
  const records = selectedNos.value.map(no => rows.value.find(row => row.areaNo === no)).filter((row): row is AreaRow => Boolean(row))
  if (!records.length) { errorMessage.value = '請選取要核准的未簽核區域。'; return }
  if (records.some(row => row.issueState !== 0)) { errorMessage.value = '核准只能選取未簽核區域。'; return }
  if (!window.confirm(`確定送出 ${records.length} 筆區域核准？`)) return
  busy.value = true; errorMessage.value = ''; let completed = 0
  try {
    for (const row of records) {
      if (mockMode.value) {
        const local = readRows()
        const current = local.find(item => item.areaNo === row.areaNo)
        if (!current || current.issueState !== 0) throw new Error(`${row.areaNo} 狀態已改變。`)
        const nextState = { ...current, issueState: 2, reviser: props.currentUserNo || 'DEV001', reviseDate: new Date().toISOString(), dataStamp: (current.dataStamp ?? 0) + 1 }
        saveRows(local.map(item => item.areaNo === row.areaNo ? nextState : item)); rows.value = local.map(item => item.areaNo === row.areaNo ? nextState : item)
      } else {
        await requestJson<AreaRow>(`${areaPath(row.areaNo)}/approve?dataStamp=${encodeURIComponent(String(row.dataStamp ?? 0))}`, { method: 'POST' })
        rows.value = rows.value.map(item => item.areaNo === row.areaNo ? { ...item, issueState: 2, dataStamp: (item.dataStamp ?? 0) + 1 } : item)
      }
      completed += 1
    }
    successMessage.value = `已完成 ${completed} 筆核准。`; await loadRows()
  } catch (error) { errorMessage.value = `${error instanceof Error ? error.message : '區域核准失敗。'}（已完成 ${completed}/${records.length} 筆）`; if (completed) await loadRows() }
  finally { busy.value = false }
}

async function deleteSelected(): Promise<void> {
  const records = selectedNos.value.map(no => rows.value.find(row => row.areaNo === no)).filter((row): row is AreaRow => Boolean(row))
  if (!records.length || busy.value) return
  if (records.some(row => row.issueState === 1)) { errorMessage.value = '簽核中的區域不可刪除。'; return }
  if (!window.confirm(`確定刪除或停用選取的 ${records.length} 筆區域？`)) return
  busy.value = true; errorMessage.value = ''; let completed = 0
  try {
    for (const row of records) {
      if (mockMode.value) {
        const local = readRows()
        const hasChildren = Object.values(readLayouts()).some(layout => layout.nodes.some(node => node.containAreaNo === row.areaNo))
        if (hasChildren) throw new Error(`${row.areaNo} 仍有區域配置關聯，無法刪除。`)
        const next = row.issueState === 2 ? local.map(item => item.areaNo === row.areaNo ? { ...item, issueState: -1, reviser: props.currentUserNo || 'DEV001', reviseDate: new Date().toISOString(), dataStamp: (item.dataStamp ?? 0) + 1 } : item) : local.filter(item => item.areaNo !== row.areaNo)
        saveRows(next); rows.value = next
      } else {
        await requestJson<void>(`${areaPath(row.areaNo)}?issueState=${row.issueState}&dataStamp=${encodeURIComponent(String(row.dataStamp ?? 0))}`, { method: 'DELETE' })
        rows.value = row.issueState === 2 ? rows.value.map(item => item.areaNo === row.areaNo ? { ...item, issueState: -1, dataStamp: (item.dataStamp ?? 0) + 1 } : item) : rows.value.filter(item => item.areaNo !== row.areaNo)
      }
      completed += 1
    }
    selectedNos.value = []; if (!rows.value.some(row => row.areaNo === activeNo.value)) activeNo.value = visibleRows.value[0]?.areaNo ?? ''
    successMessage.value = `已處理 ${completed} 筆區域資料。`
  } catch (error) { errorMessage.value = `${error instanceof Error ? error.message : '區域刪除失敗。'}（已完成 ${completed}/${records.length} 筆）`; if (completed) await loadRows() }
  finally { busy.value = false }
}

async function openLayout(): Promise<void> {
  if (!activeRow.value || activeRow.value.areaClass !== 1) return
  layoutBase.value = activeRow.value; layoutOpen.value = true; layoutBusy.value = true; layoutError.value = ''; layoutSuccess.value = ''; layoutDirty.value = false
  try {
    if (mockMode.value) {
      layoutOptions.value = structuredClone(seedLayoutOptions)
      const stored = readLayouts()[activeRow.value.areaNo]
      layoutNodes.value = structuredClone(stored?.nodes ?? (activeRow.value.areaNo === 'DEV-BASE' ? seedLayout : []))
      backgroundImageDataUrl.value = stored?.backgroundImageDataUrl ?? null
      if (!newLayoutObjectKey.value) { const first = availableLayoutOptions.value[0]; if (first) newLayoutObjectKey.value = `${first.objectType}/${first.objectNo}` }
    } else {
      const [layout, options] = await Promise.all([
        requestJson<{ relations: LayoutNode[]; backgroundImageDataUrl?: string | null }>(`${areaPath(activeRow.value.areaNo)}/layout`),
        requestJson<LayoutOption[]>(`${areaPath(activeRow.value.areaNo)}/layout-options`),
      ])
      layoutNodes.value = layout.relations; backgroundImageDataUrl.value = layout.backgroundImageDataUrl ?? null; layoutOptions.value = options
    }
    selectedLayoutNodeKey.value = layoutNodes.value[0] ? layoutNodeKey(layoutNodes.value[0]) : ''
  } catch (error) { layoutError.value = error instanceof Error ? error.message : '區域配置載入失敗。' }
  finally { layoutBusy.value = false }
}

function addLayoutNode(): void {
  const [typeText, ...noParts] = newLayoutObjectKey.value.split('/')
  const option = layoutOptions.value.find(item => item.objectType === Number(typeText) && item.objectNo === noParts.join('/'))
  if (!option || !layoutBase.value) { layoutError.value = '請先選擇要加入配置的項目。'; return }
  const node: LayoutNode = { containAreaNo: layoutBase.value.areaNo, objectNo: option.objectNo, objectType: option.objectType, objectText: option.objectText, objectX: 35 + layoutNodes.value.length * 18, objectY: 35 + layoutNodes.value.length * 18, objectLength: 64, objectWidth: 150, objectColor: 4294951175, objectAreaType: option.objectAreaType, objectSequence: layoutNodes.value.length + 1, borderColor: 0, borderStyle: 0, textColor: 0, equipmentGroup: option.equipmentGroup }
  layoutNodes.value = [...layoutNodes.value, node]; selectedLayoutNodeKey.value = layoutNodeKey(node); layoutDirty.value = true; layoutError.value = ''
}
function removeLayoutNode(node: LayoutNode): void {
  layoutNodes.value = layoutNodes.value.filter(item => layoutNodeKey(item) !== layoutNodeKey(node))
  selectedLayoutNodeKey.value = layoutNodes.value[0] ? layoutNodeKey(layoutNodes.value[0]) : ''; layoutDirty.value = true
}
function startNodeDrag(event: DragEvent, node: LayoutNode): void { draggingKey.value = layoutNodeKey(node); event.dataTransfer?.setData('text/plain', draggingKey.value) }
function dropNode(event: DragEvent): void {
  if (!draggingKey.value) return
  const node = layoutNodes.value.find(item => layoutNodeKey(item) === draggingKey.value)
  const canvas = event.currentTarget as HTMLElement
  if (!node || !canvas) return
  const rect = canvas.getBoundingClientRect()
  node.objectX = Math.max(0, Math.round(event.clientX - rect.left + canvas.scrollLeft - node.objectWidth / 2))
  node.objectY = Math.max(0, Math.round(event.clientY - rect.top + canvas.scrollTop - node.objectLength / 2))
  selectedLayoutNodeKey.value = layoutNodeKey(node); layoutDirty.value = true; draggingKey.value = ''
}
function handleBackgroundFile(event: Event): void {
  const file = (event.target as HTMLInputElement).files?.[0]
  if (!file) return
  if (file.size > 10 * 1024 * 1024) { layoutError.value = '背景圖片不可超過 10 MB。'; return }
  const reader = new FileReader()
  reader.onload = () => { backgroundImageDataUrl.value = String(reader.result); layoutDirty.value = true; layoutError.value = '' }
  reader.onerror = () => { layoutError.value = '背景圖片讀取失敗。' }
  reader.readAsDataURL(file)
}
async function saveLayout(): Promise<void> {
  if (!layoutBase.value || layoutBusy.value) return
  layoutBusy.value = true; layoutError.value = ''; layoutSuccess.value = ''
  try {
    let updatedBase: AreaRow
    if (mockMode.value) {
      const saved = readLayouts(); saved[layoutBase.value.areaNo] = { nodes: structuredClone(layoutNodes.value), backgroundImageDataUrl: backgroundImageDataUrl.value }; saveLayouts(saved)
      updatedBase = { ...layoutBase.value, dataStamp: (layoutBase.value.dataStamp ?? 0) + 1 }
      const savedRows = readRows().map(row => row.areaNo === updatedBase.areaNo ? updatedBase : row)
      saveRows(savedRows); rows.value = savedRows
    } else {
      const request = { dataStamp: layoutBase.value.dataStamp ?? 0, relations: layoutNodes.value, backgroundImageDataUrl: backgroundImageDataUrl.value }
      updatedBase = await requestJson<AreaRow>(`${areaPath(layoutBase.value.areaNo)}/layout?dataStamp=${encodeURIComponent(String(layoutBase.value.dataStamp ?? 0))}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(request) })
      rows.value = rows.value.map(row => row.areaNo === updatedBase.areaNo ? updatedBase : row)
    }
    layoutBase.value = updatedBase
    layoutDirty.value = false; layoutSuccess.value = '區域配置已儲存。'
  } catch (error) { layoutError.value = error instanceof Error ? error.message : '區域配置儲存失敗。' }
  finally { layoutBusy.value = false }
}
function closeLayout(): void {
  if (layoutDirty.value && !window.confirm('配置尚未儲存，確定關閉？')) return
  layoutOpen.value = false; layoutBase.value = null
}

onMounted(() => { void loadRows(); void loadOptions() })
</script>

<template>
  <section class="area-master">
    <header class="page-heading"><h1>區域基本資料</h1></header>
    <form class="query-strip" @submit.prevent="loadRows">
      <div class="state-filters"><label v-for="state in issueStates" :key="state.value"><input v-model.number="filterState" type="radio" :value="state.value" @change="loadRows" /> {{ state.label }}</label></div>
      <label for="area-query">區域編號</label><input id="area-query" v-model="searchText" maxlength="20" @input="searchText = searchText.toLocaleUpperCase()" /><span class="query-percent">%</span>
      <button class="btn" type="submit" :disabled="loading">查詢</button><span v-if="loading" class="muted">載入中…</span>
    </form>
    <div class="work-area">
      <div class="grid-panel">
        <div class="toolbar">
          <button class="btn btn-primary" type="button" @click="openForm('add')">新增(A)</button>
          <button class="btn" type="button" :disabled="activeRow?.issueState !== 0" @click="openForm('edit')">修改(E)</button>
          <button class="btn" type="button" :disabled="!selectedNos.length || busy" @click="deleteSelected">刪除(D)</button>
          <button class="btn" type="button" :disabled="!activeRow" @click="openForm('copy')">複製(C)</button>
          <button class="btn" type="button" :disabled="!selectedNos.length || busy" @click="approveSelected">核准</button>
          <button class="btn" type="button" :disabled="activeRow?.areaClass !== 1" @click="openLayout">區域配置</button>
        </div>
        <div class="table-scroll"><table class="data-grid">
          <thead><tr><th class="row-selector"><input type="checkbox" :checked="allVisibleSelected" aria-label="全選" @change="toggleAll(($event.target as HTMLInputElement).checked)" /></th><th>區域編號</th><th>區域名稱</th><th>區域類型</th><th>區域分類</th><th>區域配置</th><th>工廠</th><th>部門</th><th>基底區域</th><th>說明</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th><th>狀態</th></tr></thead>
          <tbody><tr v-for="row in visibleRows" :key="row.areaNo" :class="{ active: row.areaNo === activeNo, selected: selectedNos.includes(row.areaNo) }" @click="selectRow(row, $event)">
            <td class="row-selector">{{ row.areaNo === activeNo ? '▶' : '' }}</td><td>{{ row.areaNo }}</td><td>{{ row.areaName }}</td><td>{{ areaTypeLabel(row.areaType) }}</td><td>{{ areaClassLabel(row.areaClass) }}</td><td>{{ composeLabel(row.areaCompose) }}</td><td>{{ row.factoryNo }}</td><td>{{ row.departmentNo }}</td><td>{{ row.baseAreaNo }}</td><td class="description-cell">{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td><td>{{ stateLabel(row.issueState) }}</td>
          </tr><tr v-if="!loading && !visibleRows.length"><td class="empty" colspan="15">沒有符合條件的資料</td></tr></tbody>
        </table></div>
        <footer class="grid-footer">資料總筆數 {{ visibleRows.length }}</footer>
      </div>
      <aside class="side-actions"><button class="btn close-btn" type="button" @click="emit('close')">關閉(X)</button></aside>
    </div>
    <p v-if="errorMessage" class="notice error" role="alert">{{ errorMessage }}</p><p v-if="successMessage" class="notice success" role="status">{{ successMessage }}</p>
    <p v-if="mockMode" class="preview-note">Development 假資料預覽；不會寫入 MES 或 Production 資料庫。</p>

    <div v-if="formMode" class="modal-backdrop" @click.self="formMode = null">
      <section class="editor-modal" role="dialog" aria-modal="true" aria-label="區域基本資料">
        <header class="modal-heading"><h2>區域基本資料（{{ formMode === 'edit' ? '修改' : formMode === 'copy' ? '複製' : '新增' }}）</h2><button class="icon-close" type="button" aria-label="關閉" @click="formMode = null">×</button></header>
        <fieldset class="editor-fields"><legend>區域基本資料</legend>
          <div class="field-row"><label for="area-no">區域編號</label><input id="area-no" v-model="form.areaNo" maxlength="20" class="kd-input" :disabled="formMode === 'edit'" @blur="form.areaNo = form.areaNo.toLocaleUpperCase()" /></div>
          <div class="field-row"><label for="area-name">區域名稱</label><input id="area-name" v-model="form.areaName" maxlength="50" class="kd-input wide-input" /></div>
          <div class="field-row"><label for="area-dept">部門</label><select id="area-dept" v-model="form.departmentNo" class="kd-input"><option value="">請選擇</option><option v-for="item in departments" :key="item.departmentNo" :value="item.departmentNo">{{ item.departmentNo }} - {{ item.departmentName }}</option></select><button class="btn lookup-btn" type="button" @click="loadOptions">…</button></div>
          <div v-if="mode !== 'base'" class="field-row"><label for="area-base">基底區域</label><select id="area-base" v-model="form.baseAreaNo" class="kd-input" @change="inheritFactory"><option value="">請選擇</option><option v-for="item in baseAreas" :key="item.areaNo" :value="item.areaNo">{{ item.areaNo }} - {{ item.areaName }}</option></select><span class="field-unit">自動帶入工廠</span></div>
          <div v-if="mode === 'base'" class="field-row"><label for="area-factory">工廠</label><select id="area-factory" v-model="form.factoryNo" class="kd-input"><option value="">請選擇</option><option v-for="item in factories" :key="item.factoryNo" :value="item.factoryNo">{{ item.factoryNo }} - {{ item.factoryName }}</option></select></div>
          <div class="classification-group"><span class="field-label">區域分類／類型</span><div class="classification-options"><label v-for="item in classModes" :key="item.key"><input type="radio" name="area-kind" :checked="mode === item.key" @change="setMode(item.key)" /> {{ item.label }}</label></div></div>
          <div class="field-row description-row"><label for="area-description">說明</label><textarea id="area-description" v-model="form.description" maxlength="255" rows="3"></textarea></div>
        </fieldset>
        <fieldset class="audit-fields"><legend>建立／修改資訊</legend><label>建立人<input :value="audit.creator" readonly /></label><label>建立日期<input :value="audit.createDate" readonly /></label><label>修改人<input :value="audit.reviser" readonly /></label><label>修改日期<input :value="audit.reviseDate" readonly /></label></fieldset>
        <p v-if="formError" class="notice error">{{ formError }}</p><footer class="modal-actions"><button class="btn btn-primary" :disabled="busy" type="button" @click="saveForm(true)">確定</button><button class="btn" type="button" @click="formMode = null">關閉</button><button class="btn" :disabled="busy" type="button" @click="saveForm(false)">套用</button></footer>
      </section>
    </div>

    <div v-if="layoutOpen && layoutBase" class="modal-backdrop layout-backdrop" @click.self="closeLayout">
      <section class="layout-modal" role="dialog" aria-modal="true" aria-label="區域配置">
        <header class="modal-heading"><div><h2>區域配置</h2><span>{{ layoutBase.areaNo }} - {{ layoutBase.areaName }}</span></div><button class="icon-close" type="button" aria-label="關閉" @click="closeLayout">×</button></header>
        <div class="layout-toolbar"><label class="btn file-button">設定背景<input type="file" accept="image/png,image/jpeg,image/bmp" @change="handleBackgroundFile" /></label><button class="btn" type="button" :disabled="!backgroundImageDataUrl" @click="backgroundImageDataUrl = null; layoutDirty = true">清除背景</button><select v-model="newLayoutObjectKey"><optgroup v-for="group in optionGroups" :key="group.label" :label="group.label"><option v-for="item in group.options" :key="`${item.objectType}/${item.objectNo}`" :value="`${item.objectType}/${item.objectNo}`">{{ item.objectNo }} - {{ item.objectText }}</option></optgroup></select><button class="btn" type="button" :disabled="!newLayoutObjectKey" @click="addLayoutNode">新增節點</button><button class="btn" type="button" :disabled="!selectedLayoutNode" @click="selectedLayoutNode && removeLayoutNode(selectedLayoutNode)">刪除節點</button><span class="muted">拖曳節點調整位置</span></div>
        <p v-if="layoutError" class="notice error">{{ layoutError }}</p><p v-if="layoutSuccess" class="notice success">{{ layoutSuccess }}</p><p v-if="layoutBusy" class="muted">配置載入／儲存中…</p>
        <div class="layout-body">
          <div class="layout-canvas" @dragover.prevent @drop.prevent="dropNode" :style="backgroundImageDataUrl ? { backgroundImage: `url(${backgroundImageDataUrl})` } : {}">
            <div v-for="node in layoutNodes" :key="layoutNodeKey(node)" class="layout-node" :class="{ 'node-active': selectedLayoutNodeKey === layoutNodeKey(node) }" :style="{ left: `${node.objectX}px`, top: `${node.objectY}px`, width: `${node.objectWidth}px`, height: `${node.objectLength}px`, backgroundColor: colorCss(node.objectColor) }" draggable="true" @click="selectedLayoutNodeKey = layoutNodeKey(node)" @dragstart="startNodeDrag($event, node)"><strong>{{ node.objectText }}</strong><small>{{ objectTypeLabel(node.objectType) }} · {{ node.objectNo }}</small></div>
          </div>
          <aside class="layout-properties"><h3>節點屬性</h3><template v-if="selectedLayoutNode"><label>物件<input :value="`${selectedLayoutNode.objectNo} / ${objectTypeLabel(selectedLayoutNode.objectType)}`" readonly /></label><label>包含於<select v-model="selectedLayoutNode.containAreaNo" @change="layoutDirty = true"><option :value="layoutBase.areaNo">{{ layoutBase.areaNo }}（基底區域）</option><option v-for="row in rows.filter(x => x.baseAreaNo === layoutBase?.areaNo)" :key="row.areaNo" :value="row.areaNo">{{ row.areaNo }} - {{ row.areaName }}</option></select></label><div class="position-grid"><label>X<input v-model.number="selectedLayoutNode.objectX" type="number" min="0" @input="layoutDirty = true" /></label><label>Y<input v-model.number="selectedLayoutNode.objectY" type="number" min="0" @input="layoutDirty = true" /></label><label>寬<input v-model.number="selectedLayoutNode.objectWidth" type="number" min="20" @input="layoutDirty = true" /></label><label>高<input v-model.number="selectedLayoutNode.objectLength" type="number" min="20" @input="layoutDirty = true" /></label></div><label>背景顏色<input :value="colorCss(selectedLayoutNode.objectColor)" type="color" @input="selectedLayoutNode.objectColor = Number.parseInt(`ff${($event.target as HTMLInputElement).value.slice(1)}`, 16); layoutDirty = true" /></label><label>順序<input v-model.number="selectedLayoutNode.objectSequence" type="number" min="0" @input="layoutDirty = true" /></label></template><p v-else class="muted">選取畫布上的節點以編輯屬性。</p></aside>
        </div>
        <footer class="modal-actions"><button class="btn btn-primary" type="button" :disabled="layoutBusy" @click="saveLayout">儲存</button><button class="btn" type="button" @click="closeLayout">關閉</button></footer>
        <p class="preview-note">Web 版以拖曳畫布節點保存位置、尺寸、物件關係與背景；WinForms 的 AddFlow 群組選取、連續線巢狀推導在 API 規格列為待實作規則。</p>
      </section>
    </div>
  </section>
</template>

<style scoped>
.area-master{height:100%;min-height:0;display:flex;flex-direction:column;background:#fff;color:#333;font-size:14px}.page-heading{padding:8px 14px;border-bottom:1px solid #d5d5d5;background:#f5f3ed}.page-heading h1{margin:0;font-size:20px;font-weight:500}.query-strip{min-height:48px;display:flex;align-items:center;gap:8px;padding:6px 14px;border-bottom:1px solid #d4d4d4}.query-strip>label{font-weight:500}.query-strip input:not([type=radio]){height:29px;width:170px;border:1px solid #aaa;padding:3px 7px}.state-filters{display:flex;gap:10px;margin-right:15px}.state-filters input,.classification-options input{accent-color:#f1a021}.query-percent{margin-left:-4px}.work-area{display:flex;flex:1;min-height:0;gap:10px;padding:7px}.grid-panel{display:flex;flex:1;min-width:0;min-height:0;flex-direction:column;border:1px solid #c7c7c7}.toolbar{display:flex;flex-wrap:wrap;gap:5px;padding:6px;border-bottom:1px solid #ccc}.btn{min-height:30px;padding:4px 12px;border:1px solid #b8b8b8;border-radius:3px;background:linear-gradient(#fff,#f1f1f1);color:#333;white-space:nowrap}.btn:hover:not(:disabled){border-color:#888;background:#eee}.btn:disabled{opacity:.52}.btn-primary{border-color:#ed9c21;background:#ffa928;color:#fff}.table-scroll{flex:1;min-height:0;overflow:auto}.data-grid{width:100%;border-collapse:collapse;white-space:nowrap}.data-grid th{position:sticky;top:0;z-index:1;background:#48a3df;color:#fff;font-weight:500;text-align:left}.data-grid th,.data-grid td{height:29px;padding:4px 7px;border:1px solid #d2d2d2}.data-grid tbody tr{cursor:pointer}.data-grid tbody tr:hover{background:#fff4ca}.data-grid tbody tr.active{background:#ffe699}.data-grid tbody tr.selected:not(.active){background:#fff1c3}.row-selector{width:28px;text-align:center;color:#333}.row-selector input{accent-color:#f1a021}.description-cell{max-width:220px;overflow:hidden;text-overflow:ellipsis}.empty{text-align:center;color:#777;padding:18px!important}.grid-footer{min-height:25px;padding:4px 8px;color:#666;border-top:1px solid #ccc}.side-actions{width:88px;display:flex;flex-direction:column;align-items:stretch;gap:7px;padding-top:42px}.close-btn{margin-top:auto}.notice{margin:4px 12px;padding:7px 10px;border-radius:3px}.error{color:#9b1c1c;background:#fff1f0;border:1px solid #e6b3ae}.success{color:#32633a;background:#f0f8ef;border:1px solid #b9d6b7}.muted,.preview-note{color:#777}.preview-note{margin:2px 14px 7px;font-size:12px}.modal-backdrop{position:fixed;inset:0;z-index:40;display:flex;align-items:center;justify-content:center;padding:20px;background:rgba(0,0,0,.28)}.editor-modal,.layout-modal{width:min(650px,96vw);max-height:94vh;overflow:auto;border:1px solid #aaa;border-radius:4px;background:#fff;box-shadow:0 5px 24px #0004}.modal-heading{display:flex;align-items:center;justify-content:space-between;padding:10px 14px;border-bottom:1px solid #ccc;background:#f5f3ed}.modal-heading h2{margin:0;font-size:17px;font-weight:500}.modal-heading span{font-size:12px;color:#666}.icon-close{border:0;background:transparent;font-size:25px;line-height:1;color:#555}.editor-fields{margin:12px 14px 7px;padding:10px 12px 12px;border:1px solid #bbb}.editor-fields legend,.audit-fields legend{padding:0 5px;color:#555}.field-row{display:grid;grid-template-columns:95px minmax(120px,1fr) 32px;align-items:center;gap:8px;min-height:34px}.field-row>label,.field-label{text-align:right}.kd-input{height:27px;width:100%;border:1px solid #999;border-radius:0;background:#fff0ad;padding:3px 6px;color:#222}.kd-input:disabled{background:#eee;color:#777}.wide-input{grid-column:2/4}.lookup-btn{grid-column:3}.field-unit{font-size:11px;color:#777}.classification-group{display:grid;grid-template-columns:95px 1fr;gap:8px;padding:5px 0}.classification-group .field-label{text-align:right}.classification-options{display:grid;grid-template-columns:1fr 1fr;gap:5px 10px}.description-row{align-items:start;padding-top:5px}.description-row textarea{grid-column:2/4;min-height:65px;resize:vertical;border:1px solid #999;padding:5px}.audit-fields{display:grid;grid-template-columns:1fr 1fr;gap:7px 12px;margin:8px 14px;padding:10px 12px;border:1px solid #bbb}.audit-fields label{display:flex;align-items:center;gap:7px;white-space:nowrap}.audit-fields input{min-width:0;width:100%;height:25px;border:1px solid #bbb;background:#eee;padding:3px 5px}.modal-actions{display:flex;justify-content:flex-end;gap:8px;padding:10px 14px}.layout-backdrop{z-index:45}.layout-modal{width:min(1200px,98vw);height:min(850px,95vh);display:flex;flex-direction:column}.layout-toolbar{display:flex;align-items:center;flex-wrap:wrap;gap:7px;padding:7px 10px;border-bottom:1px solid #ddd}.layout-toolbar select{min-width:220px;max-width:330px;height:30px}.file-button input{display:none}.layout-body{display:flex;flex:1;min-height:0;padding:8px;gap:8px}.layout-canvas{position:relative;flex:1;min-width:0;min-height:0;overflow:auto;border:1px solid #999;background-color:#fff;background-size:auto;background-repeat:no-repeat;background-position:top left;background-image:linear-gradient(0deg,transparent 24px,#eeeeee 25px),linear-gradient(90deg,transparent 24px,#eeeeee 25px);background-size:25px 25px}.layout-node{position:absolute;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:3px;border:1px solid #555;padding:4px;text-align:center;cursor:move;user-select:none;box-shadow:1px 2px 3px #0003}.layout-node small{font-size:10px}.layout-node.node-active{outline:3px solid #f2a11b}.layout-properties{width:225px;flex:0 0 225px;overflow:auto;border:1px solid #ccc;padding:8px}.layout-properties h3{margin:0 0 8px;font-size:15px}.layout-properties>label,.position-grid label{display:flex;flex-direction:column;gap:3px;margin:7px 0}.layout-properties input,.layout-properties select{width:100%;height:27px;border:1px solid #aaa;padding:3px 5px}.layout-properties input[type=color]{padding:1px}.position-grid{display:grid;grid-template-columns:1fr 1fr;gap:0 8px}.position-grid label{margin:2px 0}.layout-modal>.preview-note{margin-top:0}.modal-backdrop .notice{margin:4px 10px}@media(max-width:800px){.state-filters{flex-wrap:wrap}.query-strip{flex-wrap:wrap}.audit-fields{grid-template-columns:1fr}.layout-body{flex-direction:column}.layout-properties{width:auto;flex-basis:auto}.side-actions{width:72px}}
</style>
