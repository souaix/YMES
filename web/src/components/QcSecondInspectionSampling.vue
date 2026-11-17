<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

interface SamplingRow {
  qcItemNo: string
  qcItemName: string
  qcItemType: number
  opNo: string
  opName: string
  oldSamplePlan: string
  newSamplePlan: string
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
  dataStamp: number
  componentCollect: number
}
interface OperationOption { opNo: string; opName: string; opOrder: number }
interface VariableQcItemOption { qcItemNo: string; qcItemName: string; opNo: string; componentCollect: number; issueState: number }
interface SamplingPlanOption { planNo: string; planName: string; componentCollect: number }
interface MockStore {
  rows: SamplingRow[]
  operations: OperationOption[]
  qcItems: VariableQcItemOption[]
  plans: SamplingPlanOption[]
  waferPlans: SamplingPlanOption[]
}
type Payload<T> = T[] | { items?: T[]; rows?: T[]; data?: T[] }
type EditorMode = 'add' | 'edit' | 'copy'
type PickerMode = 'filter-operation' | 'filter-qc-item' | 'editor-operation' | 'editor-qc-item' | 'old-plan' | 'new-plan'

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), {
  currentUserNo: '',
  developmentPreview: false,
})
const emit = defineEmits<{ close: [] }>()
const appBasePath = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const mockKey = 'ymes-dev-mock:qc-second-inspection-sampling:v1'
const rows = ref<SamplingRow[]>([])
const operations = ref<OperationOption[]>([])
const qcItems = ref<VariableQcItemOption[]>([])
const plans = ref<SamplingPlanOption[]>([])
const waferPlans = ref<SamplingPlanOption[]>([])
const filterOpNo = ref('')
const filterOpName = ref('')
const filterQcItemNo = ref('')
const filterQcItemName = ref('')
const loading = ref(false)
const saving = ref(false)
const deleting = ref(false)
const notice = ref('')
const errorMessage = ref('')
const activeKey = ref('')
const selectedKeys = ref<string[]>([])
const deleteConfirmOpen = ref(false)
const editorOpen = ref(false)
const editorMode = ref<EditorMode>('add')
const pickerOpen = ref(false)
const pickerMode = ref<PickerMode>('filter-operation')
const pickerQuery = ref('')

interface EditorState {
  qcItemNo: string; qcItemName: string; qcItemType: number; opNo: string; opName: string
  oldSamplePlan: string; newSamplePlan: string; creator: string; createDate: string
  reviser: string; reviseDate: string; dataStamp: number; componentCollect: number | null
}
const editor = ref<EditorState>(blankEditor())
const activeRow = computed(() => rows.value.find(row => rowKey(row) === activeKey.value) ?? null)
const selectedRows = computed(() => rows.value.filter(row => selectedKeys.value.includes(rowKey(row))))
const deleteRows = computed(() => selectedRows.value.length ? selectedRows.value : activeRow.value ? [activeRow.value] : [])
const editorTitle = computed(() => ({
  add: 'SamplePlan For 2nd Inspection Define（新增）',
  edit: 'SamplePlan For 2nd Inspection Define（修改）',
  copy: 'SamplePlan For 2nd Inspection Define（複製）',
}[editorMode.value]))
const pickerTitle = computed(() => ({
  'filter-operation': '選擇作業站',
  'filter-qc-item': '選擇計量品管項目',
  'editor-operation': '選擇作業站',
  'editor-qc-item': '選擇已核准計量品管項目',
  'old-plan': editor.value.componentCollect ? '選擇 Wafer 抽樣計畫' : '選擇抽樣計畫',
  'new-plan': editor.value.componentCollect ? '選擇 Wafer 抽樣計畫' : '選擇抽樣計畫',
}[pickerMode.value]))
const pickerItems = computed(() => {
  const query = pickerQuery.value.trim().toLowerCase()
  if (pickerMode.value.includes('operation')) {
    return operations.value
      .filter(item => !query || (item.opNo + ' ' + item.opName).toLowerCase().includes(query))
      .map(item => ({ key: item.opNo, code: item.opNo, name: item.opName, detail: '順序 ' + item.opOrder }))
  }
  if (pickerMode.value.includes('qc-item')) {
    const opNo = pickerMode.value === 'editor-qc-item' ? editor.value.opNo :
      pickerMode.value === 'filter-qc-item' ? filterOpNo.value : ''
    return qcItems.value
      .filter(item => item.issueState === 2 && (!opNo || item.opNo === opNo))
      .filter(item => !query || (item.qcItemNo + ' ' + item.qcItemName + ' ' + item.opNo).toLowerCase().includes(query))
      .map(item => ({ key: item.qcItemNo + '|' + item.opNo, code: item.qcItemNo, name: item.qcItemName, detail: item.opNo }))
  }
  const source = editor.value.componentCollect ? waferPlans.value : plans.value
  return source
    .filter(item => !query || (item.planNo + ' ' + item.planName).toLowerCase().includes(query))
    .map(item => ({ key: item.planNo, code: item.planNo, name: item.planName, detail: item.componentCollect ? 'Wafer' : '一般' }))
})

function blankEditor(): EditorState {
  return {
    qcItemNo: '', qcItemName: '', qcItemType: 1, opNo: '', opName: '', oldSamplePlan: '', newSamplePlan: '',
    creator: '', createDate: '', reviser: '', reviseDate: '', dataStamp: 0, componentCollect: null,
  }
}
function rowKey(row: Pick<SamplingRow, 'qcItemNo' | 'qcItemType' | 'opNo' | 'oldSamplePlan'>): string {
  return [row.qcItemNo, row.qcItemType, row.opNo, row.oldSamplePlan].join('|')
}
function nowText(): string {
  const date = new Date()
  return date.getFullYear() + '/' + String(date.getMonth() + 1).padStart(2, '0') + '/' +
    String(date.getDate()).padStart(2, '0') + ' ' + String(date.getHours()).padStart(2, '0') + ':' +
    String(date.getMinutes()).padStart(2, '0') + ':' + String(date.getSeconds()).padStart(2, '0')
}
function seedStore(): MockStore {
  const operations: OperationOption[] = [
    { opNo: 'OP-ASSY-10', opName: '組裝檢查', opOrder: 10 },
    { opNo: 'OP-PACK-20', opName: '包裝檢查', opOrder: 20 },
    { opNo: 'OP-WAFER-30', opName: '晶圓外觀檢查', opOrder: 30 },
  ]
  const qcItems: VariableQcItemOption[] = [
    { qcItemNo: 'DEV-QCV-211-01', qcItemName: '組裝尺寸抽驗（DEV）', opNo: 'OP-ASSY-10', componentCollect: 0, issueState: 2 },
    { qcItemNo: 'DEV-QCV-211-02', qcItemName: '包裝重量抽驗（DEV）', opNo: 'OP-PACK-20', componentCollect: 0, issueState: 2 },
    { qcItemNo: 'DEV-QCV-211-03', qcItemName: '晶圓缺陷複驗（DEV）', opNo: 'OP-WAFER-30', componentCollect: 1, issueState: 2 },
  ]
  const plans: SamplingPlanOption[] = [
    { planNo: 'DEV-AQL-01', planName: '一般抽樣計畫 A', componentCollect: 0 },
    { planNo: 'DEV-AQL-02', planName: '一般抽樣計畫 B', componentCollect: 0 },
    { planNo: 'DEV-AQL-03', planName: '一般加嚴抽樣', componentCollect: 0 },
  ]
  const waferPlans: SamplingPlanOption[] = [
    { planNo: 'DEV-WAFER-01', planName: 'Wafer 抽樣計畫 A', componentCollect: 1 },
    { planNo: 'DEV-WAFER-02', planName: 'Wafer 抽樣計畫 B', componentCollect: 1 },
  ]
  const audit = { creator: 'DEV001', createDate: '2026/11/17 08:30:00', reviser: 'DEV001', reviseDate: '2026/11/17 09:10:00', dataStamp: 0 }
  const rows: SamplingRow[] = [
    { qcItemNo: qcItems[0].qcItemNo, qcItemName: qcItems[0].qcItemName, qcItemType: 1, opNo: qcItems[0].opNo, opName: operations[0].opName, oldSamplePlan: 'DEV-AQL-01', newSamplePlan: 'DEV-AQL-02', componentCollect: 0, ...audit },
    { qcItemNo: qcItems[1].qcItemNo, qcItemName: qcItems[1].qcItemName, qcItemType: 1, opNo: qcItems[1].opNo, opName: operations[1].opName, oldSamplePlan: 'DEV-AQL-02', newSamplePlan: 'DEV-AQL-03', componentCollect: 0, ...audit },
    { qcItemNo: qcItems[2].qcItemNo, qcItemName: qcItems[2].qcItemName, qcItemType: 1, opNo: qcItems[2].opNo, opName: operations[2].opName, oldSamplePlan: 'DEV-WAFER-01', newSamplePlan: 'DEV-WAFER-02', componentCollect: 1, ...audit },
  ]
  return { rows, operations, qcItems, plans, waferPlans }
}
function readStore(): MockStore {
  try {
    const raw = window.localStorage.getItem(mockKey)
    if (raw) {
      const value = JSON.parse(raw) as MockStore
      if (Array.isArray(value.rows) && Array.isArray(value.operations) && Array.isArray(value.qcItems) &&
        Array.isArray(value.plans) && Array.isArray(value.waferPlans)) return value
    }
  } catch { /* initialize isolated DEV data */ }
  const value = seedStore()
  saveStore(value)
  return value
}
function saveStore(value: MockStore): void { window.localStorage.setItem(mockKey, JSON.stringify(value)) }
function listOf<T>(payload: Payload<T> | null | undefined): T[] {
  if (Array.isArray(payload)) return payload
  return payload?.items ?? payload?.rows ?? payload?.data ?? []
}
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init, headers })
  if (response.status === 401) { window.location.assign(appBasePath + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; title?: string; message?: string } | null
    throw new Error(problem?.detail || problem?.message || problem?.title || 'API 呼叫失敗（' + response.status + '）。')
  }
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}
function normalizeRow(value: Record<string, unknown>): SamplingRow {
  const text = (camel: string, upper: string): string => String(value[camel] ?? value[upper] ?? '')
  const number = (camel: string, upper: string, fallback = 0): number => Number(value[camel] ?? value[upper] ?? fallback)
  return {
    qcItemNo: text('qcItemNo', 'QCITEMNO'), qcItemName: text('qcItemName', 'QCITEMNAME'),
    qcItemType: number('qcItemType', 'QCITEMTYPE', 1), opNo: text('opNo', 'OPNO'), opName: text('opName', 'OPNAME'),
    oldSamplePlan: text('oldSamplePlan', 'OLDSAMPLEPLAN'), newSamplePlan: text('newSamplePlan', 'NEWSAMPLEPLAN'),
    creator: text('creator', 'CREATOR'), createDate: text('createDate', 'CREATEDATE'),
    reviser: text('reviser', 'REVISER'), reviseDate: text('reviseDate', 'REVISEDATE'),
    dataStamp: number('dataStamp', 'DATASTAMP'), componentCollect: number('componentCollect', 'COMPONENTCOLLECT'),
  }
}
function normalizeOperation(value: Record<string, unknown>): OperationOption {
  return { opNo: String(value.opNo ?? value.OPNO ?? ''), opName: String(value.opName ?? value.OPNAME ?? ''), opOrder: Number(value.opOrder ?? value.OPORDER ?? 0) }
}
function normalizeQcItem(value: Record<string, unknown>): VariableQcItemOption {
  return {
    qcItemNo: String(value.qcItemNo ?? value.QCITEMNO ?? ''), qcItemName: String(value.qcItemName ?? value.QCITEMNAME ?? ''),
    opNo: String(value.opNo ?? value.OPNO ?? ''), componentCollect: Number(value.componentCollect ?? value.COMPONENTCOLLECT ?? 0),
    issueState: Number(value.issueState ?? value.ISSUESTATE ?? 2),
  }
}
function normalizePlan(value: Record<string, unknown>, componentCollect: number): SamplingPlanOption {
  return {
    planNo: String(value.planNo ?? value.PLANNO ?? ''), planName: String(value.planName ?? value.PLANNAME ?? ''),
    componentCollect: Number(value.componentCollect ?? value.COMPONENTCOLLECT ?? componentCollect),
  }
}
async function loadLookups(): Promise<void> {
  if (mock.value) {
    const store = readStore()
    operations.value = store.operations.slice().sort((left, right) => left.opOrder - right.opOrder)
    qcItems.value = store.qcItems
    plans.value = store.plans
    waferPlans.value = store.waferPlans
    return
  }
  const [operationPayload, qcPayload, planPayload, waferPayload] = await Promise.all([
    request<Payload<Record<string, unknown>>>('api/qc/second-inspection-sampling/options/operations'),
    request<Payload<Record<string, unknown>>>('api/qc/second-inspection-sampling/options/variable-qc-items?issueState=2'),
    request<Payload<Record<string, unknown>>>('api/qc/second-inspection-sampling/options/sampling-plans?componentCollect=0'),
    request<Payload<Record<string, unknown>>>('api/qc/second-inspection-sampling/options/sampling-plans?componentCollect=1'),
  ])
  operations.value = listOf(operationPayload).map(normalizeOperation).sort((left, right) => left.opOrder - right.opOrder)
  qcItems.value = listOf(qcPayload).map(normalizeQcItem)
  plans.value = listOf(planPayload).map(item => normalizePlan(item, 0))
  waferPlans.value = listOf(waferPayload).map(item => normalizePlan(item, 1))
}
async function loadRows(): Promise<void> {
  loading.value = true
  errorMessage.value = ''
  notice.value = ''
  try {
    if (mock.value) {
      const store = readStore()
      rows.value = store.rows.filter(row =>
        (!filterOpNo.value || row.opNo === filterOpNo.value) &&
        (!filterQcItemNo.value || row.qcItemNo === filterQcItemNo.value))
    } else {
      const query = new URLSearchParams()
      if (filterOpNo.value) query.set('opNo', filterOpNo.value)
      if (filterQcItemNo.value) query.set('qcItemNo', filterQcItemNo.value)
      const payload = await request<Payload<Record<string, unknown>>>('api/qc/second-inspection-sampling?' + query.toString())
      rows.value = listOf(payload).map(normalizeRow)
    }
    selectedKeys.value = []
    activeKey.value = ''
  } catch (error) {
    rows.value = []
    errorMessage.value = error instanceof Error ? error.message : '載入二次檢驗抽樣計畫失敗。'
  } finally { loading.value = false }
}
function selectRow(row: SamplingRow, event: MouseEvent): void {
  const key = rowKey(row)
  activeKey.value = key
  if (event.ctrlKey || event.metaKey) {
    selectedKeys.value = selectedKeys.value.includes(key) ? selectedKeys.value.filter(item => item !== key) : [...selectedKeys.value, key]
  } else {
    selectedKeys.value = [key]
  }
}
function toggleRow(row: SamplingRow, checked: boolean): void {
  const key = rowKey(row)
  activeKey.value = key
  selectedKeys.value = checked ? [...new Set([...selectedKeys.value, key])] : selectedKeys.value.filter(item => item !== key)
}
function openPicker(mode: PickerMode): void {
  errorMessage.value = ''
  notice.value = ''
  pickerMode.value = mode
  pickerQuery.value = ''
  pickerOpen.value = true
}
async function ensureLookups(): Promise<void> {
  errorMessage.value = ''
  try {
    await loadLookups()
    errorMessage.value = ''
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '載入查找資料失敗。'
    pickerOpen.value = false
  }
}
async function choosePicker(item: { key: string; code: string; name: string }): Promise<void> {
  if (pickerMode.value === 'filter-operation') { filterOpNo.value = item.code; filterOpName.value = item.name }
  else if (pickerMode.value === 'filter-qc-item') { filterQcItemNo.value = item.code; filterQcItemName.value = item.name }
  else if (pickerMode.value === 'editor-operation') {
    const operation = operations.value.find(option => option.opNo === item.code)
    editor.value.opNo = item.code
    editor.value.opName = item.name
    editor.value.qcItemNo = ''
    editor.value.qcItemName = ''
    editor.value.oldSamplePlan = ''
    editor.value.newSamplePlan = ''
    editor.value.componentCollect = null
    if (operation) await ensureLookups()
  } else if (pickerMode.value === 'editor-qc-item') {
    const option = qcItems.value.find(value => value.qcItemNo === item.code && value.opNo === editor.value.opNo)
    editor.value.qcItemNo = item.code
    editor.value.qcItemName = item.name
    editor.value.oldSamplePlan = ''
    editor.value.newSamplePlan = ''
    editor.value.componentCollect = option?.componentCollect ?? 0
  } else if (pickerMode.value === 'old-plan') editor.value.oldSamplePlan = item.code
  else editor.value.newSamplePlan = item.code
  pickerOpen.value = false
}
async function openFilterOperationPicker(): Promise<void> { await ensureLookups(); if (!errorMessage.value) openPicker('filter-operation') }
async function openFilterQcPicker(): Promise<void> { await ensureLookups(); if (!errorMessage.value) openPicker('filter-qc-item') }
async function openEditorOperationPicker(): Promise<void> { await ensureLookups(); if (!errorMessage.value) openPicker('editor-operation') }
async function openEditorQcPicker(): Promise<void> { await ensureLookups(); if (!errorMessage.value) openPicker('editor-qc-item') }
async function openPlanPicker(mode: 'old-plan' | 'new-plan'): Promise<void> { await ensureLookups(); if (!errorMessage.value) openPicker(mode) }
function openAdd(): void {
  editorMode.value = 'add'
  editor.value = { ...blankEditor(), creator: props.currentUserNo || 'DEV001', createDate: nowText() }
  editorOpen.value = true
}
async function openEdit(): Promise<void> {
  if (!activeRow.value) { errorMessage.value = '請先選取一筆資料。'; return }
  await ensureLookups()
  const row = activeRow.value
  editorMode.value = 'edit'
  editor.value = { ...row, opName: row.opName || operations.value.find(item => item.opNo === row.opNo)?.opName || '', componentCollect: 0, reviser: props.currentUserNo || 'DEV001', reviseDate: nowText() }
  editorOpen.value = true
}
async function openCopy(): Promise<void> {
  if (!activeRow.value) { errorMessage.value = '請先選取一筆資料。'; return }
  await ensureLookups()
  const row = activeRow.value
  editorMode.value = 'copy'
  editor.value = {
    ...row, opName: row.opName || operations.value.find(item => item.opNo === row.opNo)?.opName || '', componentCollect: 0,
    creator: props.currentUserNo || 'DEV001', createDate: nowText(),
    reviser: '', reviseDate: '', dataStamp: 0,
  }
  editorOpen.value = true
}
function formatDate(value: string): string {
  return value ? value.replace('T', ' ').replace(/\.\d+/, '').slice(0, 19) : ''
}
function compositePath(row: Pick<SamplingRow, 'qcItemNo' | 'qcItemType' | 'opNo' | 'oldSamplePlan'>): string {
  return [row.qcItemType, row.qcItemNo, row.opNo, row.oldSamplePlan].map(encodeURIComponent).join('/')
}
async function saveEditor(closeAfterSave: boolean): Promise<void> {
  if (!editor.value.opNo.trim()) { errorMessage.value = '作業站不可空白。'; return }
  if (!editor.value.qcItemNo.trim()) { errorMessage.value = '品管項目不可空白。'; return }
  if (!editor.value.oldSamplePlan.trim()) { errorMessage.value = '原抽樣計畫不可空白。'; return }
  if (!editor.value.newSamplePlan.trim()) { errorMessage.value = '變更後抽樣計畫不可空白。'; return }
  saving.value = true
  errorMessage.value = ''
  notice.value = ''
  try {
    const isEdit = editorMode.value === 'edit'
    const candidate: SamplingRow = {
      ...editor.value,
      opName: editor.value.opName || operations.value.find(item => item.opNo === editor.value.opNo)?.opName || '',
      qcItemType: 1,
      creator: editor.value.creator || props.currentUserNo || 'DEV001',
      createDate: editor.value.createDate || nowText(),
      reviser: isEdit ? (props.currentUserNo || 'DEV001') : '',
      reviseDate: isEdit ? nowText() : '',
      componentCollect: editor.value.componentCollect ? 1 : 0,
    }
    if (mock.value) {
      const store = readStore()
      const existingIndex = store.rows.findIndex(row => rowKey(row) === rowKey(candidate))
      if (isEdit) {
        if (existingIndex < 0) throw new Error('資料已不存在，請重新查詢。')
        if (store.rows[existingIndex].dataStamp !== editor.value.dataStamp) throw new Error('資料已被其他使用者修改，請重新查詢。')
        store.rows[existingIndex] = { ...store.rows[existingIndex], newSamplePlan: candidate.newSamplePlan, reviser: candidate.reviser, reviseDate: candidate.reviseDate, dataStamp: candidate.dataStamp + 1 }
      } else {
        if (existingIndex >= 0) throw new Error('相同作業站、品管項目及原抽樣計畫的資料已存在。')
        store.rows.push(candidate)
      }
      saveStore(store)
    } else if (isEdit) {
      await request('api/qc/second-inspection-sampling/' + compositePath(candidate), {
        method: 'PUT', body: JSON.stringify({ newSamplePlan: candidate.newSamplePlan, dataStamp: editor.value.dataStamp }),
      })
    } else {
      await request('api/qc/second-inspection-sampling', {
        method: 'POST',
        body: JSON.stringify({
          qcItemNo: candidate.qcItemNo, qcItemType: 1, opNo: candidate.opNo,
          oldSamplePlan: candidate.oldSamplePlan, newSamplePlan: candidate.newSamplePlan,
        }),
      })
    }
    await loadRows()
    activeKey.value = rowKey(candidate)
    if (isEdit && !closeAfterSave) editor.value.dataStamp += 1
    editorOpen.value = !closeAfterSave
    notice.value = isEdit ? '資料已修改。' : '資料已新增。'
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '儲存失敗。'
  } finally { saving.value = false }
}
async function confirmDelete(): Promise<void> {
  const targets = deleteRows.value.slice()
  if (!targets.length) return
  deleting.value = true
  deleteConfirmOpen.value = false
  errorMessage.value = ''
  const failures: string[] = []
  let successes = 0
  for (const row of targets) {
    try {
      if (mock.value) {
        const store = readStore()
        const index = store.rows.findIndex(item => rowKey(item) === rowKey(row))
        if (index < 0) throw new Error('資料已不存在。')
        store.rows.splice(index, 1)
        saveStore(store)
      } else {
        await request('api/qc/second-inspection-sampling/' + compositePath(row), { method: 'DELETE' })
      }
      successes += 1
    } catch (error) {
      failures.push(row.qcItemNo + ' / ' + row.oldSamplePlan + '：' + (error instanceof Error ? error.message : '刪除失敗'))
    }
  }
  deleting.value = false
  await loadRows()
  if (failures.length) {
    errorMessage.value = successes + ' 筆刪除成功，' + failures.length + ' 筆失敗：' + failures.join('；')
  } else {
    notice.value = '已刪除 ' + successes + ' 筆資料。'
  }
}
function closePage(): void { emit('close') }
onMounted(() => { void loadLookups().catch(() => undefined) })
</script>

<template>
  <main class="second-inspection-page">
    <header class="page-heading">
      <div><span class="eyebrow">QC211 · SAMPLINGFOR2NDINSP</span><h1>二次檢驗抽樣計畫</h1></div>
    </header>

    <section class="filter-bar" aria-label="查詢條件">
      <div class="filter-field">
        <label for="filter-op">作業站</label>
        <input id="filter-op" :value="filterOpNo" readonly aria-label="作業站編號">
        <input :value="filterOpName" readonly aria-label="作業站名稱">
        <button class="lookup-button" type="button" aria-label="查找作業站" @click="openFilterOperationPicker">...</button>
      </div>
      <div class="filter-field">
        <label for="filter-qc-item">品管項目</label>
        <input id="filter-qc-item" :value="filterQcItemNo" readonly aria-label="品管項目編號">
        <input :value="filterQcItemName" readonly aria-label="品管項目名稱">
        <button class="lookup-button" type="button" aria-label="查找品管項目" @click="openFilterQcPicker">...</button>
      </div>
      <button class="win-button query-button" type="button" :disabled="loading" @click="loadRows">查詢</button>
    </section>

    <section class="list-workspace">
      <div class="grid-area">
        <div class="grid-toolbar">
          <span>資料總筆數 {{ rows.length }}</span>
          <span v-if="selectedRows.length">已選取 {{ selectedRows.length }} 筆</span>
          <span v-if="loading">載入中…</span><span v-else-if="deleting">刪除處理中…</span>
        </div>
        <div class="grid-scroll">
          <table class="win-grid">
            <thead><tr>
              <th class="select-column"></th><th>作業站編號</th><th>品管項目編號</th><th>品管項目名稱</th>
              <th>原抽樣計畫</th><th>變更後抽樣計畫</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th>
            </tr></thead>
            <tbody>
              <tr v-for="row in rows" :key="rowKey(row)" :class="{ active: rowKey(row) === activeKey, selected: selectedKeys.includes(rowKey(row)) }" @click="selectRow(row, $event)">
                <td class="select-column"><input type="checkbox" :checked="selectedKeys.includes(rowKey(row))" :aria-label="'選取 ' + row.qcItemNo" @click.stop @change="toggleRow(row, ($event.target as HTMLInputElement).checked)"></td>
                <td>{{ row.opNo }}</td><td>{{ row.qcItemNo }}</td><td>{{ row.qcItemName }}</td>
                <td>{{ row.oldSamplePlan }}</td><td>{{ row.newSamplePlan }}</td><td>{{ row.creator }}</td>
                <td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td>
              </tr>
              <tr v-if="!rows.length"><td colspan="10" class="empty-cell">{{ loading ? '載入中…' : '目前沒有資料；選擇查詢條件後按「查詢」。' }}</td></tr>
            </tbody>
          </table>
        </div>
        <div v-if="errorMessage" class="feedback error" role="alert">{{ errorMessage }}</div>
        <div v-else-if="notice" class="feedback success" role="status">{{ notice }}</div>
      </div>
      <aside class="side-actions">
        <button class="win-button primary-button" type="button" :disabled="loading || saving || deleting" @click="openAdd">新增(A)</button>
        <button class="win-button" type="button" :disabled="loading || saving || deleting || !activeRow" @click="openEdit">修改(E)</button>
        <button class="win-button" type="button" :disabled="loading || saving || deleting || !deleteRows.length" @click="deleteConfirmOpen = true">刪除(D)</button>
        <button class="win-button" type="button" :disabled="loading || saving || deleting || !activeRow" @click="openCopy">複製(C)</button>
      </aside>
    </section>
    <footer class="window-footer"><span>QCItemType：計量型（1）</span><button class="win-button" type="button" @click="closePage">關閉(X)</button></footer>

    <div v-if="editorOpen" class="modal-backdrop" role="presentation" @click.self="editorOpen = false">
      <section class="editor-dialog" role="dialog" aria-modal="true" :aria-label="editorTitle">
        <header class="dialog-heading"><h2>{{ editorTitle }}</h2><button type="button" class="icon-close" aria-label="關閉" @click="editorOpen = false">×</button></header>
        <div class="editor-fields">
          <div class="editor-field">
            <label>作業站</label><input class="data-input" :value="editor.opNo" readonly>
            <input class="name-input" :value="editor.opName" readonly>
            <button class="lookup-button" type="button" :disabled="editorMode === 'edit'" @click="openEditorOperationPicker">...</button>
          </div>
          <div class="editor-field">
            <label>品管項目</label><input class="data-input" :value="editor.qcItemNo" readonly>
            <input class="name-input" :value="editor.qcItemName" readonly>
            <button class="lookup-button" type="button" :disabled="editorMode === 'edit' || !editor.opNo" @click="openEditorQcPicker">...</button>
          </div>
          <fieldset class="plan-group" :disabled="editorMode === 'add' && !editor.qcItemNo">
            <legend>抽樣計畫</legend>
            <div class="editor-field plan-field">
              <label>原抽樣計畫</label><input class="data-input yellow-input" :value="editor.oldSamplePlan" readonly>
              <button class="lookup-button" type="button" :disabled="editorMode === 'edit' || editor.componentCollect === null" @click="openPlanPicker('old-plan')">...</button>
            </div>
            <div class="editor-field plan-field">
              <label>變更後抽樣計畫</label><input class="data-input yellow-input" :value="editor.newSamplePlan" readonly>
              <button class="lookup-button" type="button" :disabled="editor.componentCollect === null" @click="openPlanPicker('new-plan')">...</button>
            </div>
            <small>{{ editor.componentCollect === null ? '請先選擇作業站及計量品管項目' : editor.componentCollect ? '查找 Wafer 抽樣計畫' : '查找一般抽樣計畫' }}</small>
          </fieldset>
          <section class="audit-panel" aria-label="建立與修改資訊">
            <div><label>建立人</label><input :value="editor.creator" readonly></div>
            <div><label>建立日期</label><input :value="editor.createDate" readonly></div>
            <div><label>修改人</label><input :value="editor.reviser" readonly></div>
            <div><label>修改日期</label><input :value="editor.reviseDate" readonly></div>
          </section>
        </div>
        <p v-if="errorMessage" class="feedback error" role="alert">{{ errorMessage }}</p>
        <div class="dialog-actions">
          <button class="win-button primary-button" type="button" :disabled="saving" @click="saveEditor(true)">{{ saving ? '儲存中…' : '確定' }}</button>
          <button class="win-button" type="button" :disabled="saving" @click="editorOpen = false">關閉</button>
          <button class="win-button" type="button" :disabled="saving" @click="saveEditor(false)">套用</button>
        </div>
      </section>
    </div>

    <div v-if="pickerOpen" class="modal-backdrop picker-backdrop" role="presentation" @click.self="pickerOpen = false">
      <section class="picker-dialog" role="dialog" aria-modal="true" :aria-label="pickerTitle">
        <header class="dialog-heading"><h2>{{ pickerTitle }}</h2><button type="button" class="icon-close" aria-label="關閉" @click="pickerOpen = false">×</button></header>
        <label class="picker-search">查詢 <input v-model="pickerQuery" autofocus></label>
        <div class="picker-list">
          <button v-for="item in pickerItems" :key="item.key" type="button" class="picker-row" @click="choosePicker(item)">
            <strong>{{ item.code }}</strong><span>{{ item.name }}</span><small>{{ item.detail }}</small>
          </button>
          <p v-if="!pickerItems.length" class="empty-picker">沒有符合的查找資料。</p>
        </div>
        <footer class="dialog-actions"><button class="win-button" type="button" @click="pickerOpen = false">關閉</button></footer>
      </section>
    </div>

    <div v-if="deleteConfirmOpen" class="modal-backdrop" role="presentation" @click.self="deleteConfirmOpen = false">
      <section class="confirm-dialog" role="dialog" aria-modal="true" aria-labelledby="delete-title">
        <h2 id="delete-title">確認刪除</h2>
        <p>確定刪除選取的 {{ deleteRows.length }} 筆二次檢驗抽樣計畫？</p>
        <p class="confirm-note">刪除依品管項目、作業站與原抽樣計畫定位。</p>
        <div class="dialog-actions"><button class="win-button primary-button" type="button" @click="confirmDelete">確定</button><button class="win-button" type="button" @click="deleteConfirmOpen = false">取消</button></div>
      </section>
    </div>
  </main>
</template>

<style scoped>
.second-inspection-page{height:100%;min-height:520px;display:flex;flex-direction:column;gap:9px;padding:14px;background:#f7f5ee;color:#333;font-family:Arial,"Microsoft JhengHei",sans-serif;box-sizing:border-box}
.page-heading{display:flex;align-items:center;justify-content:space-between;gap:16px;min-height:40px}.page-heading h1{margin:3px 0 0;font-size:21px;font-weight:600}.eyebrow{color:#777;font-size:11px;letter-spacing:.04em}
.filter-bar{display:grid;grid-template-columns:minmax(0,1fr) 80px;grid-template-rows:auto auto;align-items:center;gap:6px 10px;padding:7px 8px;border:1px solid #ccc;background:#fff}.filter-field{display:flex;align-items:center;gap:5px}.filter-field label,.editor-field label{width:72px;text-align:right;font-size:13px}.filter-field input,.editor-field input,.audit-panel input{height:28px;box-sizing:border-box;border:1px solid #999;padding:4px 7px;background:#fff;font-size:13px;color:#333}.filter-field input:first-of-type{width:140px}.filter-field input:nth-of-type(2){width:155px}.filter-field input[readonly],.editor-field input[readonly]{background:#f5f5f5}.filter-field:nth-child(2){grid-column:1;grid-row:2}.query-button{grid-column:2;grid-row:1;align-self:start}.lookup-button{width:34px;height:29px;border:1px solid #c8c8c8;border-radius:3px;background:linear-gradient(#fff,#f2f2f2);color:#333;cursor:pointer}.lookup-button:disabled{opacity:.5;cursor:default}
.list-workspace{display:grid;grid-template-columns:minmax(0,1fr) 88px;gap:9px;min-height:0;flex:1;border:1px solid #c9c9c9;background:#fff;padding:7px}.grid-area{display:flex;flex-direction:column;min-width:0;min-height:0}.grid-toolbar{height:27px;flex:none;display:flex;align-items:center;gap:18px;padding:0 4px;color:#666;font-size:12px}.grid-scroll{min-height:0;flex:1;overflow:auto;border:1px solid #c7c7c7}.win-grid{border-collapse:collapse;width:max-content;min-width:100%;font-size:12px;white-space:nowrap}.win-grid th{position:sticky;top:0;z-index:1;background:#e9a126;color:#fff;font-weight:600;text-align:center;border:1px solid #d2d2d2;padding:7px 9px}.win-grid td{height:28px;border:1px solid #d2d2d2;padding:2px 8px;background:#fff}.win-grid tbody tr{cursor:pointer}.win-grid tbody tr:hover td{background:#fff5cf}.win-grid tbody tr.selected td{background:#ffeb9b}.win-grid tbody tr.active td{outline:1px solid #d1a63f;outline-offset:-1px}.select-column{width:28px;text-align:center!important}.select-column input{margin:0}.empty-cell{text-align:center!important;color:#888;height:100px!important}
.side-actions{display:flex;flex-direction:column;align-items:stretch;gap:8px}.win-button{min-height:33px;padding:5px 11px;border:1px solid #c8c8c8;border-radius:3px;background:linear-gradient(#fff,#f2f2f2);color:#333;font-size:13px;cursor:pointer;white-space:nowrap}.win-button:hover:not(:disabled){border-color:#aaa;background:#f7f7f7}.win-button:disabled{opacity:.55;cursor:default}.primary-button{background:#f4a128;border-color:#e6951d;color:#fff;font-weight:600}.primary-button:hover:not(:disabled){background:#e9951e}.window-footer{display:flex;justify-content:space-between;align-items:center;color:#777;font-size:12px}
.feedback{margin:6px 0 0;padding:8px 10px;border:1px solid;border-radius:3px;font-size:12px;line-height:1.45;overflow-wrap:anywhere}.feedback.error{border-color:#e3b9b5;background:#fff3f2;color:#9a3028}.feedback.success{border-color:#c9ddbd;background:#f1f8ed;color:#406531}
.modal-backdrop{position:fixed;inset:0;z-index:80;display:grid;place-items:center;background:rgba(25,30,35,.35);padding:20px}.editor-dialog,.picker-dialog,.confirm-dialog{width:min(534px,100%);max-height:calc(100vh - 40px);overflow:auto;padding:12px;background:#fff;border:1px solid #aaa;box-shadow:0 10px 32px #0003;box-sizing:border-box}.dialog-heading{display:flex;align-items:center;justify-content:space-between;gap:12px;border-bottom:1px solid #ddd;padding-bottom:9px}.dialog-heading h2,.confirm-dialog h2{margin:0;font-size:18px;font-weight:600}.icon-close{border:0;background:transparent;color:#555;font-size:24px;cursor:pointer}.editor-fields{display:flex;flex-direction:column;gap:10px;padding-top:13px}.editor-field{display:grid;grid-template-columns:80px 184px minmax(80px,1fr) 24px;align-items:center;gap:6px}.editor-field label{width:auto}.editor-field .name-input{min-width:0}.editor-field .data-input{background:#fff5ca}.editor-field .lookup-button{width:24px}.plan-group{margin:0;padding:9px 11px 8px;border:1px solid #bbb}.plan-group legend{padding:0 5px;color:#555;font-size:13px}.plan-field{grid-template-columns:130px minmax(130px,1fr) 24px}.plan-field label{text-align:right}.plan-field .lookup-button{width:24px}.editor-field .yellow-input{background:#fff0b3}.plan-group small{display:block;margin:6px 0 0 137px;color:#777;font-size:11px}.audit-panel{display:grid;grid-template-columns:1fr 1fr;gap:7px 12px;padding:8px;border:1px solid #ddd;background:#fafafa}.audit-panel div{display:grid;grid-template-columns:70px minmax(0,1fr);align-items:center;gap:5px}.audit-panel label{font-size:12px;text-align:right;color:#666}.audit-panel input{width:100%;background:#f0f0f0;color:#666}.dialog-actions{display:flex;justify-content:flex-end;gap:8px;margin-top:12px}.picker-dialog{width:min(600px,100%)}.picker-search{display:flex;align-items:center;gap:8px;margin:12px 0;font-size:13px}.picker-search input{height:30px;flex:1;border:1px solid #aaa;padding:5px 8px}.picker-list{max-height:50vh;overflow:auto;border:1px solid #ccc}.picker-row{display:grid;grid-template-columns:minmax(120px,1fr) minmax(150px,2fr) minmax(80px,1fr);gap:10px;width:100%;padding:9px 10px;border:0;border-bottom:1px solid #e2e2e2;background:#fff;color:#333;text-align:left;cursor:pointer}.picker-row:hover{background:#fff0b3}.picker-row small{color:#777}.empty-picker{padding:20px;text-align:center;color:#888}.confirm-dialog{width:min(440px,100%)}.confirm-dialog p{margin:10px 0;font-size:14px;line-height:1.5}.confirm-note{color:#777}
@media(max-width:850px){.filter-bar{align-items:flex-start}.filter-field{width:100%}.filter-field input:nth-of-type(2){flex:1}.query-button{margin-left:auto}.list-workspace{grid-template-columns:1fr;grid-template-rows:minmax(300px,1fr) auto}.side-actions{flex-direction:row;flex-wrap:wrap}.page-heading h1{font-size:18px}.editor-field{grid-template-columns:88px minmax(90px,1fr) minmax(90px,1fr) 34px}.plan-field{grid-template-columns:110px minmax(100px,1fr) 34px}.plan-group small{margin-left:116px}}
@media(max-width:560px){.second-inspection-page{padding:8px}.editor-field{grid-template-columns:88px minmax(0,1fr) 34px}.editor-field .name-input{grid-column:2}.editor-field .lookup-button{grid-column:3;grid-row:1 / span 2}.plan-field{grid-template-columns:100px minmax(0,1fr) 34px}.audit-panel{grid-template-columns:1fr}.audit-panel div{grid-template-columns:72px minmax(0,1fr)}.plan-group small{margin-left:0}.filter-field label{width:65px}.filter-field input:first-of-type{width:110px}}
</style>
