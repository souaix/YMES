<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

type IssueState = -1 | 0 | 1 | 2
type EditMode = 'new' | 'edit' | 'copy' | null
type PickerKind = 'category' | 'type' | 'factory'

interface CarrierBasisRow {
  carrierNo: string
  carrierCategory: string
  carrierType: string
  description: string | null
  creator: string | null
  createDate: string | null
  issueState: IssueState
  factoryNo: string | null
  dataStamp: number | null
  reviser: string | null
  reviseDate: string | null
  getGroupNo: string | null
  getAnsNo: string | null
  maintenanceTime: string | null
  beGroupMainten: string | null
}
interface CarrierCategoryOption { carrierCategory: string; description?: string | null; issueState: number }
interface CarrierTypeOption { carrierType: string; carrierCategory: string; description?: string | null; issueState: number }
interface FactoryOption { factoryNo: string; description?: string | null; factoryName?: string | null; issueState: number }
interface PickerRow { value: string; description: string; detail: string }
interface CarrierForm { carrierNo: string; carrierCategory: string; carrierType: string; factoryNo: string; description: string }
interface AuditFields { creator: string; createDate: string; reviser: string; reviseDate: string }
interface DevWipCarrierState { carrierNo: string; carrierCategory: string; carrierType: string; carrierState: number; description: string | null; lotNo: string | null }
interface DevCarrierStore { rows: CarrierBasisRow[]; wipRows: DevWipCarrierState[] }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const usesDevelopmentMock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes-dev-mock:equipment-carrier-basis:v1'
const categoryStorageKey = 'ymes-dev-mock:equipment-carrier-categories:v1'
const typeStorageKey = 'ymes-dev-mock:equipment-carrier-types:v1'
const issueStates: { value: IssueState; label: string }[] = [
  { value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' },
]
const seedRows: CarrierBasisRow[] = [
  { carrierNo: 'DEV-CARRIER-001', carrierCategory: 'DEV-CARRIER-03', carrierType: 'DEV-TYPE-03', description: 'DEV 假資料：已核准分類與類別', creator: 'DEV001', createDate: '2026-11-01T08:00:00+08:00', issueState: 0, factoryNo: 'DEV-FAB-01', dataStamp: 0, reviser: null, reviseDate: null, getGroupNo: null, getAnsNo: null, maintenanceTime: null, beGroupMainten: null },
  { carrierNo: 'DEV-CARRIER-002', carrierCategory: 'DEV-CARRIER-03', carrierType: 'DEV-TYPE-03', description: 'DEV 假資料：等待簽核', creator: 'DEV001', createDate: '2026-11-02T08:00:00+08:00', issueState: 1, factoryNo: 'DEV-FAB-01', dataStamp: 1, reviser: 'DEV002', reviseDate: '2026-11-03T09:30:00+08:00', getGroupNo: null, getAnsNo: null, maintenanceTime: null, beGroupMainten: null },
  { carrierNo: 'DEV-CARRIER-003', carrierCategory: 'DEV-CARRIER-03', carrierType: 'DEV-TYPE-03', description: 'DEV 假資料：已核准', creator: 'DEV001', createDate: '2026-11-04T08:00:00+08:00', issueState: 2, factoryNo: 'DEV-FAB-02', dataStamp: 2, reviser: 'DEV002', reviseDate: '2026-11-05T09:30:00+08:00', getGroupNo: 'DEV-GROUP', getAnsNo: null, maintenanceTime: null, beGroupMainten: null },
  { carrierNo: 'DEV-CARRIER-004', carrierCategory: 'DEV-CARRIER-03', carrierType: 'DEV-TYPE-03', description: 'DEV 假資料：已停用', creator: 'DEV001', createDate: '2026-11-06T08:00:00+08:00', issueState: -1, factoryNo: 'DEV-FAB-02', dataStamp: 3, reviser: 'DEV002', reviseDate: '2026-11-07T09:30:00+08:00', getGroupNo: null, getAnsNo: null, maintenanceTime: null, beGroupMainten: null },
]
const seedCarrierTypes: CarrierTypeOption[] = [
  { carrierType: 'DEV-TYPE-03', carrierCategory: 'DEV-CARRIER-03', description: 'DEV 假資料：已核准載具類別', issueState: 2 },
]
const seedCategories: CarrierCategoryOption[] = [
  { carrierCategory: 'DEV-CARRIER-03', description: 'DEV 假資料：已核准載具分類', issueState: 2 },
]
const seedFactories: FactoryOption[] = [
  { factoryNo: 'DEV-FAB-01', factoryName: 'DEV 開發廠區 1', description: 'Development only', issueState: 2 },
  { factoryNo: 'DEV-FAB-02', factoryName: 'DEV 開發廠區 2', description: 'Development only', issueState: 2 },
]

const rows = ref<CarrierBasisRow[]>([])
const issueState = ref<IssueState>(0)
const queryText = ref('')
const appliedQuery = ref('')
const currentNo = ref('')
const selectedNos = ref<string[]>([])
const loading = ref(false)
const busy = ref(false)
const error = ref('')
const notice = ref('')
const mode = ref<EditMode>(null)
const sourceNo = ref('')
const form = ref<CarrierForm>(emptyForm())
const formDataStamp = ref(0)
const audit = ref<AuditFields>({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const formDirty = ref(false)
const pickerKind = ref<PickerKind | null>(null)
const pickerRows = ref<PickerRow[]>([])
const pickerSearch = ref('')
const pickerLoading = ref(false)
const pickerError = ref('')
const loadRequestId = ref(0)

const visibleRows = computed(() => rows.value
  .filter((row) => row.issueState === issueState.value)
  .filter((row) => !appliedQuery.value || row.carrierNo.toLocaleLowerCase().includes(appliedQuery.value.toLocaleLowerCase()))
  .slice().sort((a, b) => a.carrierNo.localeCompare(b.carrierNo)))
const current = computed(() => visibleRows.value.find((row) => row.carrierNo === currentNo.value) ?? null)
const filteredPickerRows = computed(() => pickerRows.value
  .filter((row) => !pickerSearch.value || (row.value + ' ' + row.description + ' ' + row.detail).toLocaleLowerCase().includes(pickerSearch.value.toLocaleLowerCase())))
const formTitle = computed(() => mode.value === 'edit' ? '載具基本資料定義(EDIT)' : mode.value === 'copy' ? '載具基本資料定義(COPY)' : '載具基本資料定義(ADD)')
const pickerTitle = computed(() => pickerKind.value === 'category' ? '選取已核准的載具分類' : pickerKind.value === 'type' ? '選取此分類的已核准載具類別' : '選取已核准的廠區')

function emptyForm(): CarrierForm { return { carrierNo: '', carrierCategory: '', carrierType: '', factoryNo: '', description: '' } }
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function userNo(): string { return props.currentUserNo?.trim() || (usesDevelopmentMock.value ? 'DEV001' : '') }
function seedWipRows(): DevWipCarrierState[] { return seedRows.map((row) => ({ carrierNo: row.carrierNo, carrierCategory: row.carrierCategory, carrierType: row.carrierType, carrierState: 0, description: row.description, lotNo: null })) }
function readDevStore(): DevCarrierStore {
  try {
    const raw = localStorage.getItem(storageKey)
    if (!raw) return { rows: clone(seedRows), wipRows: seedWipRows() }
    const parsed = JSON.parse(raw) as DevCarrierStore | CarrierBasisRow[]
    if (Array.isArray(parsed)) return { rows: parsed, wipRows: [] }
    return { rows: Array.isArray(parsed.rows) ? parsed.rows : [], wipRows: Array.isArray(parsed.wipRows) ? parsed.wipRows : [] }
  } catch { return { rows: clone(seedRows), wipRows: seedWipRows() } }
}
function writeDevStore(value: DevCarrierStore): void { localStorage.setItem(storageKey, JSON.stringify(value)) }
function readCategories(): CarrierCategoryOption[] {
  try {
    const raw = localStorage.getItem(categoryStorageKey)
    return (raw ? JSON.parse(raw) as CarrierCategoryOption[] : clone(seedCategories)).filter((item) => item.issueState === 2)
  } catch { return clone(seedCategories) }
}
function readCarrierTypes(): CarrierTypeOption[] {
  try {
    const raw = localStorage.getItem(typeStorageKey)
    return (raw ? JSON.parse(raw) as CarrierTypeOption[] : clone(seedCarrierTypes)).filter((item) => item.issueState === 2)
  } catch { return clone(seedCarrierTypes) }
}
function dateText(value?: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.valueOf()) ? value : date.toLocaleString('zh-TW')
}
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  const root = new URL(base.endsWith('/') ? base : base + '/', window.location.origin)
  return new URL(path.replace(/^\/+/, ''), root).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init })
  if (response.status === 401) {
    window.location.assign(appBase + '/')
    throw new Error('登入狀態已逾期，請重新登入。')
  }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    throw new Error(problem?.detail || problem?.message || problem?.title || '載具基本資料 API 回應錯誤 (' + response.status + ')。')
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function setQuery(): void { appliedQuery.value = queryText.value.trim(); selectedNos.value = []; void loadRows() }
function setIssueState(value: IssueState): void {
  issueState.value = value
  selectedNos.value = []
  if (!visibleRows.value.some((row) => row.carrierNo === currentNo.value)) currentNo.value = visibleRows.value[0]?.carrierNo ?? ''
}
function selectRow(no: string): void { currentNo.value = no }
function toggleSelected(no: string): void {
  selectedNos.value = selectedNos.value.includes(no) ? selectedNos.value.filter((value) => value !== no) : [...selectedNos.value, no]
  selectRow(no)
}
function markDirty(): void { formDirty.value = true }
function closeForm(): void { if (!busy.value) { mode.value = null; formDirty.value = false } }
function normalizeCarrierNo(): void { form.value.carrierNo = form.value.carrierNo.trim().toUpperCase() }
function validateForm(): string {
  normalizeCarrierNo()
  if (!form.value.carrierNo) return 'Carrier No 為必填。'
  if (form.value.carrierNo.length > 50) return 'Carrier No 不可超過 50 個字元。'
  if (!form.value.carrierCategory) return 'Carrier Category 為必填。'
  if (form.value.carrierCategory.length > 50) return 'Carrier Category 不可超過 50 個字元。'
  if (!form.value.carrierType) return 'Carrier Type 為必填。'
  if (form.value.carrierType.length > 50) return 'Carrier Type 不可超過 50 個字元。'
  if (!form.value.factoryNo) return 'Factory No 為必填。'
  if (form.value.factoryNo.length > 20) return 'Factory No 不可超過 20 個字元。'
  if (form.value.description.length > 255) return 'Description 不可超過 255 個字元。'
  return ''
}
function openForm(nextMode: Exclude<EditMode, null>): void {
  const row = current.value
  if (nextMode !== 'new' && !row) { error.value = '請先選取一筆載具基本資料。'; return }
  if (nextMode === 'edit' && row?.issueState !== 0) { error.value = '只有未簽核的載具基本資料可以修改。'; return }
  mode.value = nextMode
  error.value = ''
  notice.value = ''
  formDirty.value = false
  sourceNo.value = nextMode === 'edit' ? row?.carrierNo ?? '' : ''
  formDataStamp.value = row?.dataStamp ?? 0
  const now = new Date().toISOString()
  if (nextMode === 'new') {
    form.value = emptyForm()
    audit.value = { creator: userNo(), createDate: now, reviser: '', reviseDate: '' }
  } else if (row) {
    form.value = {
      carrierNo: row.carrierNo,
      carrierCategory: row.carrierCategory,
      carrierType: row.carrierType,
      factoryNo: row.factoryNo || '',
      description: row.description || '',
    }
    audit.value = nextMode === 'edit'
      ? { creator: row.creator || '', createDate: row.createDate || '', reviser: userNo(), reviseDate: now }
      : { creator: userNo(), createDate: now, reviser: '', reviseDate: '' }
  }
}
async function saveForm(keepOpen: boolean): Promise<void> {
  if (!mode.value || busy.value) return
  const problem = validateForm()
  if (problem) { error.value = problem; return }
  const saveMode = mode.value
  const carrierNo = form.value.carrierNo
  const payload = {
    carrierCategory: form.value.carrierCategory,
    carrierType: form.value.carrierType,
    factoryNo: form.value.factoryNo,
    description: form.value.description.trim() || null,
  }
  busy.value = true
  error.value = ''
  notice.value = ''
  try {
    let nextDataStamp = formDataStamp.value
    if (usesDevelopmentMock.value) {
      const store = readDevStore()
      if (saveMode === 'edit') {
        const index = store.rows.findIndex((row) => row.carrierNo === sourceNo.value)
        if (index < 0) throw new Error('載具基本資料已不存在，請重新載入。')
        if (store.rows[index].issueState !== 0) throw new Error('只有未簽核的載具基本資料可以修改。')
        if ((store.rows[index].dataStamp ?? 0) !== formDataStamp.value) throw new Error('資料已被其他使用者修改，請重新載入。')
        nextDataStamp = (store.rows[index].dataStamp ?? 0) + 1
        store.rows[index] = { ...store.rows[index], ...payload, dataStamp: nextDataStamp, reviser: userNo(), reviseDate: new Date().toISOString() }
        store.wipRows = store.wipRows.map((item) => item.carrierNo === sourceNo.value ? { ...item, carrierCategory: payload.carrierCategory, carrierType: payload.carrierType } : item)
      } else {
        if (store.rows.some((row) => row.carrierNo.toLocaleUpperCase() === carrierNo.toLocaleUpperCase())) throw new Error('Carrier No 已存在。')
        const created: CarrierBasisRow = {
          carrierNo, ...payload, creator: userNo(), createDate: new Date().toISOString(), issueState: 0,
          dataStamp: 0, reviser: null, reviseDate: null, getGroupNo: null, getAnsNo: null, maintenanceTime: null, beGroupMainten: null,
        }
        store.rows.push(created)
        store.wipRows.push({ carrierNo, carrierCategory: payload.carrierCategory, carrierType: payload.carrierType, carrierState: 0, description: payload.description, lotNo: null })
        nextDataStamp = 0
      }
      writeDevStore(store)
    } else {
      const body = saveMode === 'edit'
        ? { ...payload, issueState: 0, dataStamp: formDataStamp.value }
        : { carrierNo, ...payload }
      const result = await request<{ dataStamp?: number }>(
        saveMode === 'edit' ? 'api/equipment/carrier-basis/' + encodeURIComponent(sourceNo.value) : 'api/equipment/carrier-basis',
        { method: saveMode === 'edit' ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) },
      )
      if (result?.dataStamp !== undefined) nextDataStamp = result.dataStamp
      else if (saveMode === 'edit') nextDataStamp += 1
    }
    currentNo.value = carrierNo
    await loadRows()
    const saved = rows.value.find((row) => row.carrierNo === carrierNo)
    if (saved) audit.value = { creator: saved.creator || '', createDate: saved.createDate || '', reviser: saved.reviser || '', reviseDate: saved.reviseDate || '' }
    formDataStamp.value = nextDataStamp
    formDirty.value = false
    notice.value = saveMode === 'edit' ? '載具基本資料已修改。' : saveMode === 'copy' ? '載具基本資料已複製。' : '載具基本資料已新增。'
    if (!keepOpen) closeForm()
  } catch (e) { error.value = e instanceof Error ? e.message : '載具基本資料儲存失敗。' }
  finally { busy.value = false }
}
async function deleteSelected(): Promise<void> {
  if (busy.value) return
  const targetsByNo = new Set(selectedNos.value)
  if (current.value) targetsByNo.add(current.value.carrierNo)
  const targets = visibleRows.value.filter((row) => targetsByNo.has(row.carrierNo))
  if (!targets.length) { error.value = '請先選取要刪除的載具基本資料。'; return }
  if (!window.confirm('確定刪除／停用選取的 ' + targets.length + ' 筆載具基本資料？')) return
  busy.value = true
  error.value = ''
  notice.value = ''
  let changed = 0
  let failure = ''
  try {
    for (const row of targets) {
      if (![0, -1, 2].includes(row.issueState)) throw new Error('簽核中的載具基本資料不可刪除。')
      if (usesDevelopmentMock.value) {
        const store = readDevStore()
        const index = store.rows.findIndex((item) => item.carrierNo === row.carrierNo)
        if (index < 0) throw new Error('載具基本資料已不存在。')
        if ((store.rows[index].dataStamp ?? 0) !== (row.dataStamp ?? 0)) throw new Error('資料已被其他使用者修改。')
        if (row.issueState === 2) store.rows[index] = { ...store.rows[index], issueState: -1, dataStamp: (store.rows[index].dataStamp ?? 0) + 1, reviser: userNo(), reviseDate: new Date().toISOString() }
        else {
          store.rows.splice(index, 1)
          store.wipRows = store.wipRows.filter((item) => item.carrierNo !== row.carrierNo)
        }
        writeDevStore(store)
      } else {
        const query = new URLSearchParams({ issueState: String(row.issueState), dataStamp: String(row.dataStamp ?? 0) })
        await request<void>('api/equipment/carrier-basis/' + encodeURIComponent(row.carrierNo) + '?' + query.toString(), { method: 'DELETE' })
      }
      changed += 1
    }
    selectedNos.value = []
  } catch (e) { failure = e instanceof Error ? e.message : '載具基本資料刪除失敗。' }
  finally { busy.value = false }
  if (changed) await loadRows()
  if (failure) error.value = changed ? failure + ' 已完成 ' + changed + ' 筆。' : failure
  else if (changed) notice.value = '已完成 ' + changed + ' 筆刪除／停用。'
}
async function approveSelected(): Promise<void> {
  if (busy.value) return
  const targetsByNo = new Set(selectedNos.value)
  if (current.value) targetsByNo.add(current.value.carrierNo)
  const targets = visibleRows.value.filter((row) => targetsByNo.has(row.carrierNo))
  if (!targets.length) { error.value = '請先選取要核准的載具基本資料。'; return }
  if (!window.confirm('確定核准選取的 ' + targets.length + ' 筆載具基本資料？')) return
  busy.value = true
  error.value = ''
  notice.value = ''
  let changed = 0
  let failure = ''
  try {
    for (const row of targets) {
      if (row.issueState !== 0) throw new Error('只有未簽核的載具基本資料可以核准。')
      if (usesDevelopmentMock.value) {
        const store = readDevStore()
        const index = store.rows.findIndex((item) => item.carrierNo === row.carrierNo)
        if (index < 0) throw new Error('載具基本資料已不存在。')
        if ((store.rows[index].dataStamp ?? 0) !== (row.dataStamp ?? 0)) throw new Error('資料已被其他使用者修改。')
        store.rows[index] = { ...store.rows[index], issueState: 2, dataStamp: (store.rows[index].dataStamp ?? 0) + 1, reviser: userNo(), reviseDate: new Date().toISOString() }
        writeDevStore(store)
      } else {
        await request<void>('api/equipment/carrier-basis/' + encodeURIComponent(row.carrierNo) + '/approve', {
          method: 'POST', headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ issueState: row.issueState, dataStamp: row.dataStamp ?? 0 }),
        })
      }
      changed += 1
    }
    selectedNos.value = []
  } catch (e) { failure = e instanceof Error ? e.message : '載具基本資料核准失敗。' }
  finally { busy.value = false }
  if (changed) await loadRows()
  if (failure) error.value = changed ? failure + ' 已完成 ' + changed + ' 筆。' : failure
  else if (changed) notice.value = usesDevelopmentMock.value ? 'DEV 簡化核准已完成 ' + changed + ' 筆（直接轉為已簽核）。' : '已送出 ' + changed + ' 筆載具基本資料核准。'
}
async function loadRows(): Promise<void> {
  const requestId = ++loadRequestId.value
  loading.value = true
  error.value = ''
  notice.value = ''
  try {
    let data: CarrierBasisRow[]
    if (usesDevelopmentMock.value) data = readDevStore().rows
    else {
      const query = new URLSearchParams()
      if (appliedQuery.value) query.set('carrierNo', appliedQuery.value)
      data = await request<CarrierBasisRow[]>('api/equipment/carrier-basis?' + query.toString())
    }
    if (requestId !== loadRequestId.value) return
    rows.value = data.slice().sort((a, b) => a.carrierNo.localeCompare(b.carrierNo))
    selectedNos.value = selectedNos.value.filter((no) => visibleRows.value.some((row) => row.carrierNo === no))
    if (!visibleRows.value.some((row) => row.carrierNo === currentNo.value)) currentNo.value = visibleRows.value[0]?.carrierNo ?? ''
  } catch (e) {
    if (requestId !== loadRequestId.value) return
    rows.value = []
    currentNo.value = ''
    selectedNos.value = []
    error.value = e instanceof Error ? e.message : '載具基本資料載入失敗。'
  } finally { if (requestId === loadRequestId.value) loading.value = false }
}
function setPickerOptions(kind: PickerKind, data: CarrierCategoryOption[] | CarrierTypeOption[] | FactoryOption[]): void {
  if (kind === 'category') pickerRows.value = (data as CarrierCategoryOption[]).filter((item) => item.issueState === 2).map((item) => ({ value: item.carrierCategory, description: item.description || '', detail: '' }))
  else if (kind === 'type') pickerRows.value = (data as CarrierTypeOption[]).filter((item) => item.issueState === 2 && item.carrierCategory === form.value.carrierCategory).map((item) => ({ value: item.carrierType, description: item.description || '', detail: item.carrierCategory }))
  else pickerRows.value = (data as FactoryOption[]).filter((item) => item.issueState === 2).map((item) => ({ value: item.factoryNo, description: item.factoryName || item.description || '', detail: '' }))
}
async function openPicker(kind: PickerKind): Promise<void> {
  if (kind === 'type' && !form.value.carrierCategory) { error.value = '請先選取 Carrier Category。'; return }
  pickerKind.value = kind
  pickerRows.value = []
  pickerSearch.value = ''
  pickerError.value = ''
  pickerLoading.value = true
  try {
    if (usesDevelopmentMock.value) {
      if (kind === 'category') setPickerOptions(kind, readCategories())
      else if (kind === 'type') setPickerOptions(kind, readCarrierTypes())
      else setPickerOptions(kind, clone(seedFactories))
    } else if (kind === 'category') {
      setPickerOptions(kind, await request<CarrierCategoryOption[]>('api/equipment/carrier-categories?issueState=2'))
    } else if (kind === 'type') {
      const query = new URLSearchParams({ issueState: '2', carrierCategory: form.value.carrierCategory })
      setPickerOptions(kind, await request<CarrierTypeOption[]>('api/equipment/carrier-types?' + query.toString()))
    } else {
      setPickerOptions(kind, await request<FactoryOption[]>('api/master-data/factories?issueState=2'))
    }
    if (!pickerRows.value.length) pickerError.value = kind === 'category' ? '沒有可選取的已核准載具分類。' : kind === 'type' ? '此載具分類沒有可選取的已核准載具類別。' : '沒有可選取的已核准廠區。'
  } catch (e) { pickerRows.value = []; pickerError.value = e instanceof Error ? e.message : '選項載入失敗。' }
  finally { pickerLoading.value = false }
}
function choosePickerRow(row: PickerRow): void {
  if (pickerKind.value === 'category') {
    if (form.value.carrierCategory !== row.value) form.value.carrierType = ''
    form.value.carrierCategory = row.value
  } else if (pickerKind.value === 'type') form.value.carrierType = row.value
  else if (pickerKind.value === 'factory') form.value.factoryNo = row.value
  formDirty.value = true
  pickerKind.value = null
}
function closePage(): void { window.history.pushState({}, '', appBase || '/'); window.dispatchEvent(new PopStateEvent('popstate')) }

onMounted(() => { void loadRows() })
watch(() => [props.developmentPreview, props.apiBaseUrl], () => {
  closeForm()
  pickerKind.value = null
  selectedNos.value = []
  void loadRows()
})
</script>

<template>
  <section class="carrier-basis-page">
    <div v-if="usesDevelopmentMock" class="carrier-basis-dev-note">開發模式：目前資料為 DEV 假資料，儲存在此瀏覽器；新增會同步模擬載具主檔與在製載具狀態，核准流程採簡化狀態轉換。</div>
    <div class="carrier-basis-search">
      <div class="carrier-basis-states" role="radiogroup" aria-label="簽核狀態">
        <label v-for="state in issueStates" :key="state.value"><input type="radio" name="carrierBasisIssueState" :checked="issueState === state.value" @change="setIssueState(state.value)"> {{ state.label }}</label>
      </div>
      <label for="carrierBasisSearch">載具編號</label>
      <input id="carrierBasisSearch" v-model="queryText" type="text" @keydown.enter.prevent="setQuery">
      <span class="carrier-basis-like">%</span>
      <button type="button" class="carrier-basis-primary" :disabled="loading" @click="setQuery">查詢</button>
    </div>
    <p v-if="error && !mode" class="carrier-basis-message carrier-basis-error" role="alert">{{ error }}</p>
    <p v-else-if="notice && !mode" class="carrier-basis-message carrier-basis-success" role="status">{{ notice }}</p>
    <div class="carrier-basis-main">
      <div class="carrier-basis-grid-wrap">
        <table class="carrier-basis-grid">
          <thead><tr><th class="carrier-basis-check"></th><th>CarrierNo</th><th>CarrierCategory</th><th>CarrierType</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>FactoryNo</th><th>Reviser</th><th>ReviseDate</th><th>GetGroupNo</th><th>GetAnsNo</th><th>MaintenanceTime</th><th>BeGroupMainten</th></tr></thead>
          <tbody>
            <tr v-if="loading"><td colspan="14" class="carrier-basis-empty">載入中…</td></tr>
            <tr v-else-if="visibleRows.length === 0"><td colspan="14" class="carrier-basis-empty">沒有符合的載具基本資料。</td></tr>
            <tr v-for="row in visibleRows" :key="row.carrierNo" :class="{ 'carrier-basis-current': currentNo === row.carrierNo, 'carrier-basis-marked': selectedNos.includes(row.carrierNo) }" @click="selectRow(row.carrierNo)" @dblclick="selectRow(row.carrierNo); openForm('edit')">
              <td class="carrier-basis-check" @click.stop><input type="checkbox" :checked="selectedNos.includes(row.carrierNo)" :aria-label="'選取 ' + row.carrierNo" @change="toggleSelected(row.carrierNo)"></td>
              <td>{{ row.carrierNo }}</td><td>{{ row.carrierCategory }}</td><td>{{ row.carrierType }}</td><td>{{ row.description || '' }}</td><td>{{ row.creator || '' }}</td><td>{{ dateText(row.createDate) }}</td><td>{{ row.factoryNo || '' }}</td><td>{{ row.reviser || '' }}</td><td>{{ dateText(row.reviseDate) }}</td><td>{{ row.getGroupNo || '' }}</td><td>{{ row.getAnsNo || '' }}</td><td>{{ dateText(row.maintenanceTime) }}</td><td>{{ row.beGroupMainten || '' }}</td>
            </tr>
          </tbody>
        </table>
      </div>
      <aside class="carrier-basis-actions">
        <button type="button" class="carrier-basis-primary" :disabled="busy" @click="openForm('new')">新增(A)</button>
        <button type="button" :disabled="busy || current?.issueState !== 0" @click="openForm('edit')">修改(E)</button>
        <button type="button" :disabled="busy || (!selectedNos.length && !current)" @click="deleteSelected">刪除(D)</button>
        <button type="button" :disabled="busy || !current" @click="openForm('copy')">複製(C)</button>
        <button type="button" :disabled="busy || (!selectedNos.length && !current)" @click="approveSelected">核准</button>
        <button type="button" class="carrier-basis-close-page" :disabled="busy" @click="closePage">關閉</button>
      </aside>
    </div>

    <div v-if="mode" class="carrier-basis-overlay" @click.self="closeForm">
      <form class="carrier-basis-dialog" @submit.prevent="saveForm(false)">
        <header class="carrier-basis-dialog-title">{{ formTitle }}</header>
        <div class="carrier-basis-dialog-body">
          <fieldset class="carrier-basis-fields">
            <legend>Carrier Basis</legend>
            <label><span>Carrier No</span><input v-model="form.carrierNo" type="text" :readonly="mode === 'edit'" @input="markDirty" @blur="normalizeCarrierNo" @keydown.enter.prevent></label>
            <label class="carrier-basis-picker-field"><span>Carrier Category</span><input :value="form.carrierCategory" type="text" readonly><button type="button" aria-label="選取載具分類" :disabled="busy" @click="openPicker('category')">...</button></label>
            <label class="carrier-basis-picker-field"><span>Carrier Type</span><input :value="form.carrierType" type="text" readonly><button type="button" aria-label="選取載具類別" :disabled="busy" @click="openPicker('type')">...</button></label>
            <label class="carrier-basis-picker-field"><span>Factory No</span><input :value="form.factoryNo" type="text" readonly><button type="button" aria-label="選取廠區" :disabled="busy" @click="openPicker('factory')">...</button></label>
            <label class="carrier-basis-description"><span>Description</span><textarea v-model="form.description" rows="3" maxlength="255" @input="markDirty"></textarea></label>
          </fieldset>
          <fieldset class="carrier-basis-audit">
            <legend>建立／修改資料</legend>
            <label><span>建立人</span><input :value="audit.creator" readonly></label><label><span>建立日期</span><input :value="dateText(audit.createDate)" readonly></label>
            <label><span>修改人</span><input :value="audit.reviser" readonly></label><label><span>修改日期</span><input :value="dateText(audit.reviseDate)" readonly></label>
          </fieldset>
          <p v-if="error" class="carrier-basis-message carrier-basis-error" role="alert">{{ error }}</p>
        </div>
        <footer class="carrier-basis-dialog-footer">
          <button type="submit" class="carrier-basis-primary" :disabled="busy">確定</button><button type="button" :disabled="busy" @click="closeForm">關閉</button><button type="button" :disabled="busy || !formDirty" @click="saveForm(true)">套用</button>
        </footer>
      </form>
    </div>

    <div v-if="pickerKind" class="carrier-basis-overlay carrier-basis-picker-overlay" @click.self="pickerKind = null">
      <section class="carrier-basis-picker-dialog" role="dialog" aria-modal="true" :aria-label="pickerTitle">
        <header><strong>{{ pickerTitle }}</strong><button type="button" aria-label="關閉" @click="pickerKind = null">×</button></header>
        <label class="carrier-basis-picker-search"><span>查詢</span><input v-model="pickerSearch" type="text" @keydown.enter.prevent></label>
        <div class="carrier-basis-picker-list">
          <p v-if="pickerLoading" class="carrier-basis-empty">載入中…</p>
          <p v-else-if="pickerError" class="carrier-basis-error carrier-basis-picker-message">{{ pickerError }}</p>
          <p v-else-if="filteredPickerRows.length === 0" class="carrier-basis-empty">沒有符合的選項。</p>
          <table v-else><thead><tr><th>編號</th><th>說明</th><th>分類</th><th></th></tr></thead><tbody>
            <tr v-for="item in filteredPickerRows" :key="item.value" @dblclick="choosePickerRow(item)"><td>{{ item.value }}</td><td>{{ item.description }}</td><td>{{ item.detail }}</td><td><button type="button" @click="choosePickerRow(item)">選取</button></td></tr>
          </tbody></table>
        </div>
        <footer><button type="button" @click="pickerKind = null">關閉</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.carrier-basis-page{display:flex;height:100%;min-height:0;flex-direction:column;gap:6px;color:#333;font-size:13px}
.carrier-basis-dev-note{padding:7px 10px;border:1px solid #d8c577;background:#fff6c8;color:#66551b}
.carrier-basis-search{display:flex;min-height:38px;align-items:center;gap:7px;padding:5px 9px;border:1px solid #c9c9c9;background:#fafafa}
.carrier-basis-states{display:flex;align-items:center;gap:13px;margin-right:10px}
.carrier-basis-states label{display:flex;align-items:center;gap:3px;white-space:nowrap}
.carrier-basis-states input,.carrier-basis-check input{margin:0;accent-color:#ad831b}
.carrier-basis-search>input{width:min(270px,28vw);min-height:29px;padding:4px 7px;border:1px solid #a9a9a9;background:#fff0b3;color:#333;font:inherit}
.carrier-basis-like{margin-left:-7px;color:#555}
.carrier-basis-search button,.carrier-basis-actions button,.carrier-basis-dialog-footer button,.carrier-basis-picker-dialog button{min-height:30px;padding:4px 12px;border:1px solid #bdbdbd;border-radius:3px;background:#fafafa;color:#333;font:inherit;cursor:pointer}
.carrier-basis-search button:hover,.carrier-basis-actions button:hover,.carrier-basis-dialog-footer button:hover,.carrier-basis-picker-dialog button:hover{background:#fff2c3;border-color:#c8aa54}
.carrier-basis-search button:disabled,.carrier-basis-actions button:disabled,.carrier-basis-dialog-footer button:disabled{opacity:.55;cursor:not-allowed}
.carrier-basis-primary{background:#f5a623!important;border-color:#e49a24!important;color:#fff!important}
.carrier-basis-message{margin:0;padding:7px 10px;border:1px solid}
.carrier-basis-error{border-color:#dca7a2;background:#fff1ef;color:#942f24}
.carrier-basis-success{border-color:#c8d8ac;background:#f4f8eb;color:#496622}
.carrier-basis-main{display:grid;min-height:0;flex:1;grid-template-columns:minmax(0,1fr) 88px;gap:7px}
.carrier-basis-grid-wrap{min-height:0;overflow:auto;border:1px solid #bdbdbd;background:#fff}
.carrier-basis-grid{width:100%;min-width:1660px;border-collapse:collapse;white-space:nowrap}
.carrier-basis-grid th{position:sticky;top:0;z-index:1;padding:5px 7px;border:1px solid #d2c36d;background:#e7d978;color:#453d20;text-align:left;font-weight:600}
.carrier-basis-grid td{height:27px;padding:4px 7px;border:1px solid #d6d6d6}
.carrier-basis-grid tbody tr{cursor:pointer}.carrier-basis-grid tbody tr:nth-child(even){background:#fafafa}.carrier-basis-grid tbody tr:hover{background:#fff6d6}
.carrier-basis-grid tbody tr.carrier-basis-marked{background:#fff7dc}.carrier-basis-grid tbody tr.carrier-basis-current{background:#fff0b7}
.carrier-basis-check{width:28px;text-align:center}.carrier-basis-empty{height:45px!important;color:#777;text-align:center}
.carrier-basis-actions{display:flex;min-height:0;flex-direction:column;gap:6px;padding-top:0}.carrier-basis-actions button{width:100%;min-height:32px}.carrier-basis-close-page{margin-top:auto!important}
.carrier-basis-overlay{position:fixed;inset:0;z-index:80;display:grid;place-items:center;padding:16px;background:rgba(0,0,0,.35)}
.carrier-basis-dialog{display:flex;width:min(504px,96vw);height:min(394px,94vh);max-height:94vh;flex-direction:column;border:1px solid #999;background:#fdfdfd;box-shadow:0 5px 20px rgba(0,0,0,.24)}
.carrier-basis-dialog-title{display:flex;min-height:31px;align-items:center;padding:5px 11px;border-bottom:1px solid #ccc;background:#f4f1e8;color:#444}
.carrier-basis-dialog-body{display:flex;min-height:0;flex:1;flex-direction:column;gap:7px;overflow:auto;padding:7px 12px 0}
.carrier-basis-fields{display:flex;flex:none;flex-direction:column;gap:4px;margin:0;padding:7px 9px 9px;border:1px solid #bdbdbd}
.carrier-basis-fields legend,.carrier-basis-audit legend{padding:0 4px;color:#555}
.carrier-basis-fields>label{display:grid;min-height:23px;grid-template-columns:101px minmax(0,1fr);align-items:center;gap:6px}
.carrier-basis-fields>label>span{color:#555;text-align:right}
.carrier-basis-fields input,.carrier-basis-fields textarea{min-width:0;padding:3px 6px;border:1px solid #a9a9a9;background:#fff0b3;color:#333;font:inherit}
.carrier-basis-fields input{width:152px;min-height:22px}.carrier-basis-fields input[readonly]{background:#eee}
.carrier-basis-picker-field{grid-template-columns:101px 152px 24px!important}
.carrier-basis-picker-field button{width:24px;height:24px;padding:0}
.carrier-basis-description{align-items:start!important}.carrier-basis-fields textarea{width:min(353px,100%);min-height:64px;resize:vertical}
.carrier-basis-audit{display:grid;flex:none;grid-template-columns:1fr 1fr;gap:4px 8px;margin:0;padding:5px 8px 7px;border:1px solid #bdbdbd}
.carrier-basis-audit label{display:flex;min-width:0;align-items:center;gap:5px}.carrier-basis-audit label span{width:56px;flex:none;color:#555;text-align:right}
.carrier-basis-audit input{flex:1;min-width:0;min-height:25px;padding:3px 5px;border:1px solid #c5c5c5;background:#eee;color:#555;font:inherit}
.carrier-basis-dialog-footer{display:flex;min-height:48px;align-items:center;justify-content:flex-end;gap:7px;padding:7px 12px;border-top:1px solid #ccc}
.carrier-basis-picker-overlay{z-index:90}.carrier-basis-picker-dialog{display:flex;width:min(560px,94vw);height:min(430px,88vh);flex-direction:column;border:1px solid #999;background:#fff;box-shadow:0 5px 20px rgba(0,0,0,.24)}
.carrier-basis-picker-dialog>header{display:flex;min-height:38px;align-items:center;justify-content:space-between;padding:6px 12px;border-bottom:1px solid #ccc;background:#f4f1e8}
.carrier-basis-picker-dialog>header button{min-width:30px;padding:0;border:0;background:transparent;font-size:22px}
.carrier-basis-picker-search{display:flex;align-items:center;gap:8px;padding:9px 12px}.carrier-basis-picker-search input{min-height:29px;flex:1;padding:4px 7px;border:1px solid #aaa;background:#fff0b3;font:inherit}
.carrier-basis-picker-list{min-height:0;flex:1;overflow:auto;margin:0 12px;border:1px solid #bbb}.carrier-basis-picker-list table{width:100%;border-collapse:collapse}
.carrier-basis-picker-list th{position:sticky;top:0;padding:6px;border:1px solid #d2c36d;background:#e7d978;text-align:left}.carrier-basis-picker-list td{padding:6px;border:1px solid #ddd}
.carrier-basis-picker-list tbody tr:hover{background:#fff6d6}.carrier-basis-picker-message{margin:8px}
.carrier-basis-picker-dialog>footer{display:flex;justify-content:flex-end;padding:8px 12px}
@media(max-width:800px){.carrier-basis-search{flex-wrap:wrap}.carrier-basis-states{width:100%;flex-wrap:wrap}.carrier-basis-grid{min-width:1660px}.carrier-basis-dialog{width:96vw;height:94vh}.carrier-basis-audit{grid-template-columns:1fr}}
</style>
