<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

interface LocatorRow {
  inventoryNo: string
  locatorNo: string
  locatorName: string | null
  capacity: number | string | null
  description: string | null
  creator: string | null
  createDate: string | null
  issueState: number
  erpNo: string | null
  reviser: string | null
  reviseDate: string | null
  dataStamp: number | string | null
}
interface InventoryOption { inventoryNo: string }
interface LocatorForm { inventoryNo: string; locatorNo: string; locatorName: string; capacity: string; description: string }
type FormMode = 'add' | 'edit' | 'copy' | null

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mockMode = computed(() => import.meta.env.DEV && props.developmentPreview)
const rowsKey = 'ymes-dev-mock:smd-locator-basis:v1'
const issueStateFilter = ref(0)
const queryInventoryNo = ref('')
const appliedInventoryNo = ref('')
const allRows = ref<LocatorRow[]>([])
const inventoryOptions = ref<InventoryOption[]>([])
const selectedKeys = ref<string[]>([])
const activeKey = ref('')
const anchorKey = ref('')
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const formError = ref('')
const formMode = ref<FormMode>(null)
const form = ref<LocatorForm>({ inventoryNo: '', locatorNo: '', locatorName: '', capacity: '', description: '' })
const formBaseline = ref<LocatorForm | null>(null)
const formDirty = computed(() => !!formBaseline.value && JSON.stringify(form.value) !== JSON.stringify(formBaseline.value))
const formDataStamp = ref<number>(0)
const audit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const inventoryPickerOpen = ref(false)
const inventoryPickerQuery = ref('')
const activeRow = computed(() => visibleRows.value.find(row => rowKey(row) === activeKey.value) ?? null)
const visibleRows = computed(() => allRows.value
  .filter(row => Number(row.issueState) === issueStateFilter.value)
  .filter(row => !appliedInventoryNo.value || row.inventoryNo === appliedInventoryNo.value)
  .sort((a, b) => a.inventoryNo.localeCompare(b.inventoryNo) || a.locatorNo.localeCompare(b.locatorNo)))
const selectedRows = computed(() => visibleRows.value.filter(row => selectedKeys.value.includes(rowKey(row))))
const filteredInventoryOptions = computed(() => inventoryOptions.value
  .filter(item => !inventoryPickerQuery.value.trim() || item.inventoryNo.toLocaleLowerCase().includes(inventoryPickerQuery.value.trim().toLocaleLowerCase()))
  .sort((a, b) => a.inventoryNo.localeCompare(b.inventoryNo)))
const canPickInventory = computed(() => formMode.value === 'add' && !appliedInventoryNo.value)
const issueStates = [
  { value: 0, label: '未簽核' },
  { value: 1, label: '簽核中' },
  { value: 2, label: '已簽核' },
  { value: -1, label: '不使用' },
]

const seedRows: LocatorRow[] = [
  { inventoryNo: 'INV-DEV-RAW', locatorNo: 'A-01', locatorName: '原料儲位 A01', capacity: 120, description: 'DEV 假資料：未簽核', creator: 'DEV001', createDate: '2026-11-10T08:00:00+08:00', issueState: 0, erpNo: null, reviser: 'DEV002', reviseDate: '2026-11-11T09:20:00+08:00', dataStamp: 2 },
  { inventoryNo: 'INV-DEV-RAW', locatorNo: 'A-02', locatorName: '原料儲位 A02', capacity: 80.5, description: 'DEV 假資料：簽核中', creator: 'DEV001', createDate: '2026-11-12T10:15:00+08:00', issueState: 1, erpNo: null, reviser: null, reviseDate: null, dataStamp: 1 },
  { inventoryNo: 'INV-DEV-FG', locatorNo: 'FG-01', locatorName: '成品儲位 01', capacity: 300, description: 'DEV 假資料：已簽核', creator: 'DEV001', createDate: '2026-11-13T10:15:00+08:00', issueState: 2, erpNo: null, reviser: 'DEV002', reviseDate: '2026-11-14T09:00:00+08:00', dataStamp: 3 },
  { inventoryNo: 'INV-DEV-RAW', locatorNo: 'OLD-01', locatorName: '停用儲位', capacity: 0, description: 'DEV 假資料：不使用', creator: 'DEV001', createDate: '2026-11-14T10:15:00+08:00', issueState: -1, erpNo: null, reviser: 'DEV002', reviseDate: '2026-11-15T09:00:00+08:00', dataStamp: 1 },
]
const seedInventories: InventoryOption[] = [{ inventoryNo: 'INV-DEV-FG' }, { inventoryNo: 'INV-DEV-RAW' }]

function rowKey(row: Pick<LocatorRow, 'inventoryNo' | 'locatorNo'>): string { return JSON.stringify([row.inventoryNo, row.locatorNo]) }
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
    if (response.status === 404) throw new Error('儲位基本資料 API 尚未建立；請依家用端待辦實作後再使用 Production。')
    throw new Error(problem?.detail || problem?.message || problem?.title || '儲位基本資料 API 呼叫失敗。')
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function readRows(): LocatorRow[] {
  try { const value = window.localStorage.getItem(rowsKey); return value ? JSON.parse(value) as LocatorRow[] : structuredClone(seedRows) }
  catch { return structuredClone(seedRows) }
}
function saveRows(value: LocatorRow[]): void { window.localStorage.setItem(rowsKey, JSON.stringify(value)) }
function formatDate(value: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return new Intl.DateTimeFormat('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false }).format(date).replaceAll('-', '/')
}
function formatCapacity(value: LocatorRow['capacity']): string {
  if (value === null || value === undefined || value === '') return ''
  const number = Number(value)
  return Number.isFinite(number) ? number.toFixed(4).replace(/\.?0+$/, '') : String(value)
}
function applyIssueFilter(value: number): void {
  issueStateFilter.value = value
  selectedKeys.value = []
  activeKey.value = ''
  anchorKey.value = ''
  errorMessage.value = ''
  successMessage.value = ''
}
async function loadRows(): Promise<void> {
  appliedInventoryNo.value = queryInventoryNo.value.trim()
  loading.value = true
  errorMessage.value = ''
  try {
    if (mockMode.value) {
      allRows.value = readRows()
      if (!window.localStorage.getItem(rowsKey)) saveRows(allRows.value)
    } else {
      const query = new URLSearchParams()
      if (appliedInventoryNo.value) query.set('inventoryNo', appliedInventoryNo.value)
      const encodedQuery = query.toString()
      const suffix = encodedQuery ? '?' + encodedQuery : ''
      const result = await requestJson<LocatorRow[] | { items?: LocatorRow[] }>('api/smd/locator-basis' + suffix)
      allRows.value = Array.isArray(result) ? result : result.items ?? []
    }
    if (activeKey.value && !allRows.value.some(row => rowKey(row) === activeKey.value)) {
      activeKey.value = ''
      selectedKeys.value = []
      anchorKey.value = ''
    }
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '儲位資料載入失敗。' }
  finally { loading.value = false }
}
async function loadInventoryOptions(): Promise<void> {
  try {
    if (mockMode.value) inventoryOptions.value = structuredClone(seedInventories)
    else {
      const result = await requestJson<{ approvedInventories?: InventoryOption[] }>('api/smd/locator-basis/options')
      inventoryOptions.value = result.approvedInventories ?? []
    }
  } catch (error) {
    inventoryOptions.value = []
    errorMessage.value = error instanceof Error ? error.message : '已核准庫存選項載入失敗。'
  }
}
function selectRow(row: LocatorRow, event: MouseEvent): void {
  const key = rowKey(row)
  const visible = visibleRows.value
  if (event.shiftKey && anchorKey.value) {
    const start = visible.findIndex(item => rowKey(item) === anchorKey.value)
    const end = visible.findIndex(item => rowKey(item) === key)
    if (start >= 0 && end >= 0) selectedKeys.value = visible.slice(Math.min(start, end), Math.max(start, end) + 1).map(rowKey)
  } else if (event.ctrlKey || event.metaKey) {
    selectedKeys.value = selectedKeys.value.includes(key) ? selectedKeys.value.filter(item => item !== key) : [...selectedKeys.value, key]
    anchorKey.value = key
  } else {
    selectedKeys.value = [key]
    anchorKey.value = key
  }
  activeKey.value = key
  errorMessage.value = ''
  successMessage.value = ''
}
function openForm(mode: Exclude<FormMode, null>): void {
  formError.value = ''
  errorMessage.value = ''
  successMessage.value = ''
  const source = activeRow.value
  if ((mode === 'edit' || mode === 'copy') && !source) { errorMessage.value = '請先選取一筆儲位資料。'; return }
  if (mode === 'edit' && source?.issueState !== 0) { errorMessage.value = '只有未簽核資料可以修改。'; return }
  if (mode === 'add') {
    const inventoryNo = appliedInventoryNo.value
    form.value = { inventoryNo, locatorNo: '', locatorName: '', capacity: '', description: '' }
    formDataStamp.value = 0
    audit.value = { creator: props.currentUserNo || 'DEV001', createDate: formatDate(new Date().toISOString()), reviser: '', reviseDate: '' }
  } else if (source) {
    form.value = {
      inventoryNo: source.inventoryNo,
      locatorNo: source.locatorNo,
      locatorName: source.locatorName ?? '',
      capacity: source.capacity === null || source.capacity === undefined ? '' : String(source.capacity),
      description: source.description ?? '',
    }
    formDataStamp.value = Number(source.dataStamp ?? 0)
    audit.value = mode === 'copy'
      ? { creator: props.currentUserNo || 'DEV001', createDate: formatDate(new Date().toISOString()), reviser: '', reviseDate: '' }
      : { creator: source.creator ?? '', createDate: formatDate(source.createDate), reviser: source.reviser ?? '', reviseDate: formatDate(source.reviseDate) }
  }
  formMode.value = mode
  formBaseline.value = structuredClone(form.value)
}
function closeForm(): void {
  if (busy.value) return
  if (formDirty.value && !window.confirm('資料尚未儲存，確定關閉？')) return
  formMode.value = null
  formBaseline.value = null
  inventoryPickerOpen.value = false
}
function openInventoryPicker(): void {
  if (!canPickInventory.value) return
  inventoryPickerQuery.value = ''
  inventoryPickerOpen.value = true
}
function chooseInventory(item: InventoryOption): void {
  form.value = { ...form.value, inventoryNo: item.inventoryNo }
  inventoryPickerOpen.value = false
}
function validateForm(): string {
  const value = form.value
  if (!value.inventoryNo.trim()) return '請輸入或選擇庫存編號。'
  if (!value.locatorNo.trim()) return '請輸入儲位編號。'
  if (value.inventoryNo.length > 20) return '庫存編號不可超過 20 個字元。'
  if (value.locatorNo.length > 20) return '儲位編號不可超過 20 個字元。'
  if (value.locatorName.length > 50) return '儲位名稱不可超過 50 個字元。'
  if (value.description.length > 255) return '說明不可超過 255 個字元。'
  if (value.capacity.trim()) {
    if (!/^\d+(?:\.\d{1,4})?$/.test(value.capacity.trim())) return '容量必須是大於或等於 0 且最多 4 位小數的數值。'
    if (Number(value.capacity) > 99999999.9999) return '容量超出資料庫 NUMBER(12,4) 範圍。'
  }
  if (formMode.value !== 'edit' && allRows.value.some(row => row.inventoryNo === value.inventoryNo.trim() && row.locatorNo === value.locatorNo.trim())) return '同一庫存下的儲位編號已存在。'
  return ''
}
async function saveForm(closeAfterSave: boolean): Promise<void> {
  if (!formMode.value || busy.value) return
  const validation = validateForm()
  if (validation) { formError.value = validation; return }
  const mode = formMode.value
  const original = activeRow.value
  const body: Record<string, unknown> = { inventoryNo: form.value.inventoryNo.trim(), locatorNo: form.value.locatorNo.trim() }
  if (form.value.locatorName !== '') body.locatorName = form.value.locatorName
  if (form.value.capacity.trim() !== '') body.capacity = Number(form.value.capacity)
  if (form.value.description !== '') body.description = form.value.description
  busy.value = true
  formError.value = ''
  errorMessage.value = ''
  const now = new Date().toISOString()
  try {
    let saved: LocatorRow
    if (mockMode.value) {
      const list = readRows()
      if (mode === 'edit') {
        if (!original) throw new Error('資料已不存在，請重新查詢。')
        const current = list.find(row => rowKey(row) === rowKey(original))
        if (!current) throw new Error('資料已不存在，請重新查詢。')
        if (Number(current.dataStamp ?? 0) !== formDataStamp.value) throw new Error('資料已被其他人修改，請重新查詢後再操作。')
        saved = {
          ...current,
          locatorName: form.value.locatorName === '' ? current.locatorName : form.value.locatorName,
          capacity: form.value.capacity.trim() === '' ? current.capacity : Number(form.value.capacity),
          description: form.value.description === '' ? current.description : form.value.description,
          reviser: props.currentUserNo || 'DEV001',
          reviseDate: now,
          dataStamp: Number(current.dataStamp ?? 0) + 1,
        }
        saveRows(list.map(row => rowKey(row) === rowKey(current) ? saved : row))
      } else {
        if (list.some(row => row.inventoryNo === body.inventoryNo && row.locatorNo === body.locatorNo)) throw new Error('同一庫存下的儲位編號已存在。')
        saved = {
          inventoryNo: String(body.inventoryNo),
          locatorNo: String(body.locatorNo),
          locatorName: (body.locatorName as string | undefined) ?? null,
          capacity: (body.capacity as number | undefined) ?? 0,
          description: (body.description as string | undefined) ?? null,
          creator: props.currentUserNo || 'DEV001',
          createDate: now,
          issueState: 0,
          erpNo: null,
          reviser: null,
          reviseDate: null,
          dataStamp: 0,
        }
        saveRows([...list, saved])
      }
    } else if (mode === 'edit' && original) {
      const query = new URLSearchParams({ inventoryNo: original.inventoryNo, locatorNo: original.locatorNo, issueState: String(original.issueState), dataStamp: String(formDataStamp.value) })
      saved = await requestJson<LocatorRow>('api/smd/locator-basis/item?' + query.toString(), { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
    } else {
      saved = await requestJson<LocatorRow>('api/smd/locator-basis', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
    }
    await loadRows()
    activeKey.value = rowKey(saved)
    selectedKeys.value = [activeKey.value]
    anchorKey.value = activeKey.value
    form.value = { inventoryNo: saved.inventoryNo, locatorNo: saved.locatorNo, locatorName: saved.locatorName ?? '', capacity: saved.capacity === null ? '' : String(saved.capacity ?? ''), description: saved.description ?? '' }
    formDataStamp.value = Number(saved.dataStamp ?? 0)
    audit.value = { creator: saved.creator ?? '', createDate: formatDate(saved.createDate), reviser: saved.reviser ?? '', reviseDate: formatDate(saved.reviseDate) }
    formMode.value = 'edit'
    formBaseline.value = structuredClone(form.value)
    if (closeAfterSave) formMode.value = null
    else successMessage.value = '資料已儲存。'
  } catch (error) { formError.value = error instanceof Error ? error.message : '儲位資料儲存失敗。' }
  finally { busy.value = false }
}
async function deleteSelected(): Promise<void> {
  const targets = [...selectedRows.value]
  if (!targets.length || busy.value) { if (!targets.length) errorMessage.value = '請先選取要刪除的資料。'; return }
  if (!window.confirm('確定刪除選取的 ' + targets.length + ' 筆儲位資料？')) return
  busy.value = true
  errorMessage.value = ''
  successMessage.value = ''
  let completed = 0
  try {
    for (const row of targets) {
      if (![0, 2, -1].includes(Number(row.issueState))) throw new Error(row.inventoryNo + ' / ' + row.locatorNo + ' 正在簽核中，不允許刪除；已停止後續資料。')
      if (mockMode.value) {
        const next = readRows()
        if (row.issueState === 2) saveRows(next.map(item => rowKey(item) === rowKey(row) ? { ...item, issueState: -1, reviser: props.currentUserNo || 'DEV001', reviseDate: new Date().toISOString(), dataStamp: Number(item.dataStamp ?? 0) + 1 } : item))
        else saveRows(next.filter(item => rowKey(item) !== rowKey(row)))
      } else {
        const query = new URLSearchParams({ inventoryNo: row.inventoryNo, locatorNo: row.locatorNo, issueState: String(row.issueState), dataStamp: String(row.dataStamp ?? 0) })
        await requestJson<void>('api/smd/locator-basis/item?' + query.toString(), { method: 'DELETE' })
      }
      completed++
    }
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '儲位資料刪除失敗。' }
  finally {
    busy.value = false
    if (completed) {
      selectedKeys.value = []
      activeKey.value = ''
      anchorKey.value = ''
      await loadRows()
      if (!errorMessage.value) successMessage.value = '已完成 ' + completed + ' 筆刪除或停用。'
    }
  }
}
async function approveSelected(): Promise<void> {
  const targets = [...selectedRows.value]
  if (!targets.length || busy.value) { if (!targets.length) errorMessage.value = '請先選取要核准的資料。'; return }
  if (issueStateFilter.value !== 0 || targets.some(row => row.issueState !== 0)) { errorMessage.value = '只有未簽核資料可以核准。'; return }
  if (!window.confirm('確定核准選取的 ' + targets.length + ' 筆儲位資料？')) return
  busy.value = true
  errorMessage.value = ''
  successMessage.value = ''
  let completed = 0
  try {
    for (const row of targets) {
      if (mockMode.value) {
        const next = readRows().map(item => rowKey(item) === rowKey(row) ? { ...item, issueState: 2, reviser: props.currentUserNo || 'DEV001', reviseDate: new Date().toISOString(), dataStamp: Number(item.dataStamp ?? 0) + 1 } : item)
        saveRows(next)
      } else {
        const query = new URLSearchParams({ inventoryNo: row.inventoryNo, locatorNo: row.locatorNo, issueState: '0', dataStamp: String(row.dataStamp ?? 0) })
        await requestJson<LocatorRow>('api/smd/locator-basis/item/approve?' + query.toString(), { method: 'POST' })
      }
      completed++
    }
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '儲位資料核准失敗。' }
  finally {
    busy.value = false
    if (completed) {
      selectedKeys.value = []
      activeKey.value = ''
      anchorKey.value = ''
      await loadRows()
      if (!errorMessage.value) successMessage.value = '已送出 ' + completed + ' 筆核准。DEV 假資料直接轉為已簽核。'
    }
  }
}

onMounted(() => { void loadRows(); void loadInventoryOptions() })
</script>

<template>
  <section class="locator-master">
    <header class="page-heading"><h1>儲位基本資料</h1></header>
    <div class="query-strip">
      <fieldset class="issue-filter"><legend>狀態</legend><label v-for="state in issueStates" :key="state.value"><input type="radio" name="locator-issue-state" :checked="issueStateFilter === state.value" @change="applyIssueFilter(state.value)" />{{ state.label }}</label></fieldset>
      <label for="locator-query">庫存編號</label><input id="locator-query" v-model="queryInventoryNo" maxlength="20" @keyup.enter="loadRows" /><span class="wildcard">%</span><button class="btn" type="button" :disabled="loading" @click="loadRows">查詢</button><span v-if="loading" class="muted">載入中…</span>
    </div>
    <div v-if="mockMode" class="preview-note">Development Preview：使用本機 localStorage 假資料，不連接正式 API。</div>
    <div v-if="errorMessage" class="notice error" role="alert">{{ errorMessage }}</div>
    <div v-if="successMessage" class="notice success" role="status">{{ successMessage }}</div>
    <div class="work-area">
      <div class="grid-panel">
        <div class="table-scroll"><table class="data-grid"><thead><tr><th class="row-selector"></th><th class="inventory-no">庫存編號</th><th class="locator-no">儲位編號</th><th class="locator-name">儲位名稱</th><th class="capacity">容量</th><th class="description">說明</th><th class="audit-user">建立人</th><th class="audit-date">建立日期</th><th class="audit-user">修改人</th><th class="audit-date">修改日期</th></tr></thead><tbody>
          <tr v-for="row in visibleRows" :key="rowKey(row)" :class="{ active: rowKey(row) === activeKey, selected: selectedKeys.includes(rowKey(row)) }" @click="selectRow(row, $event)" @dblclick="row.issueState === 0 && openForm('edit')"><td class="row-selector">{{ rowKey(row) === activeKey ? '▶' : '' }}</td><td>{{ row.inventoryNo }}</td><td>{{ row.locatorNo }}</td><td>{{ row.locatorName ?? '' }}</td><td class="capacity-cell">{{ formatCapacity(row.capacity) }}</td><td class="description-cell">{{ row.description ?? '' }}</td><td>{{ row.creator ?? '' }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser ?? '' }}</td><td>{{ formatDate(row.reviseDate) }}</td></tr>
          <tr v-if="!visibleRows.length"><td class="empty" colspan="10">{{ loading ? '載入中…' : '沒有符合條件的資料' }}</td></tr>
        </tbody></table></div><footer class="grid-footer">資料總筆數 {{ visibleRows.length }}</footer>
      </div>
      <aside class="side-actions">
        <button class="btn btn-primary" type="button" :disabled="busy" @click="openForm('add')">新增(A)</button>
        <button class="btn" type="button" :disabled="busy || activeRow?.issueState !== 0" @click="openForm('edit')">修改(E)</button>
        <button class="btn" type="button" :disabled="busy || !selectedRows.length" @click="deleteSelected">刪除(D)</button>
        <button class="btn" type="button" :disabled="busy || !activeRow" @click="openForm('copy')">複製(C)</button>
        <button class="btn" type="button" :disabled="busy || issueStateFilter !== 0 || !selectedRows.length" @click="approveSelected">核准(Y)</button>
        <button class="btn close-btn" type="button" :disabled="busy" @click="emit('close')">關閉(X)</button>
      </aside>
    </div>

    <div v-if="formMode" class="modal-backdrop" @click.self="closeForm">
      <form class="editor-modal" @submit.prevent="saveForm(true)">
        <header class="modal-heading"><h2>儲位資料定義（{{ formMode === 'add' ? '新增' : formMode === 'edit' ? '修改' : '複製' }}）</h2><button class="icon-close" type="button" aria-label="關閉" @click="closeForm">×</button></header>
        <fieldset class="editor-fields"><legend>儲位資料</legend>
          <div class="field-row"><label for="locator-inventory">庫存編號</label><div class="picker-field"><input id="locator-inventory" v-model="form.inventoryNo" class="kd-input" readonly /><button class="btn picker-button" type="button" :disabled="!canPickInventory" @click="openInventoryPicker">…</button></div></div>
          <div class="field-row"><label for="locator-no">儲位編號</label><input id="locator-no" v-model="form.locatorNo" class="kd-input" maxlength="20" :readonly="formMode === 'edit'" /></div>
          <div class="field-row"><label for="locator-name">儲位名稱</label><input id="locator-name" v-model="form.locatorName" class="plain-input" maxlength="50" /></div>
          <div class="field-row"><label for="locator-capacity">容量</label><input id="locator-capacity" v-model="form.capacity" class="plain-input numeric-input" inputmode="decimal" /></div>
          <div class="field-row description-row"><label for="locator-description">說明</label><textarea id="locator-description" v-model="form.description" class="plain-input description-input" maxlength="255"></textarea></div>
        </fieldset>
        <fieldset class="audit-fields"><legend>稽核資料</legend><label>建立人<input :value="audit.creator" readonly /></label><label>建立日期<input :value="audit.createDate" readonly /></label><label>修改人<input :value="audit.reviser" readonly /></label><label>修改日期<input :value="audit.reviseDate" readonly /></label></fieldset>
        <div v-if="formError" class="notice error form-error" role="alert">{{ formError }}</div>
        <div class="modal-actions"><button class="btn btn-primary" type="submit" :disabled="busy">確定</button><button class="btn" type="button" :disabled="busy" @click="closeForm">關閉</button><button class="btn" type="button" :disabled="busy || !formDirty" @click="saveForm(false)">套用</button></div>
      </form>
      <div v-if="inventoryPickerOpen" class="picker-backdrop" @click.self="inventoryPickerOpen = false">
        <section class="inventory-picker"><header class="modal-heading"><h2>選擇已核准庫存</h2><button class="icon-close" type="button" @click="inventoryPickerOpen = false">×</button></header><label class="picker-search">庫存編號 <input v-model="inventoryPickerQuery" autofocus /></label><div class="picker-list"><button v-for="item in filteredInventoryOptions" :key="item.inventoryNo" type="button" @click="chooseInventory(item)">{{ item.inventoryNo }}</button><p v-if="!filteredInventoryOptions.length">沒有已核准的庫存。</p></div></section>
      </div>
    </div>
  </section>
</template>

<style scoped>
.locator-master{display:flex;flex-direction:column;width:100%;height:100%;min-height:0;background:#fff;color:#333;font:14px Arial,"Microsoft JhengHei",sans-serif}.page-heading{height:54px;flex:none;padding:9px 42px;background:#f5f3ed;border-bottom:1px solid #ddd}.page-heading h1{margin:0;font-size:21px;font-weight:500}.query-strip{display:flex;align-items:center;gap:7px;min-height:52px;padding:5px 10px;border-bottom:1px solid #ccc;white-space:nowrap}.query-strip>input{width:190px;height:28px;border:1px solid #999;padding:3px 6px;font:inherit}.wildcard{margin-left:-4px}.issue-filter{display:flex;gap:11px;align-items:center;margin:0 8px 0 0;padding:4px 8px;border:1px solid #ccc}.issue-filter legend{padding:0 4px;color:#666}.issue-filter label{display:flex;align-items:center;gap:3px}.issue-filter input{margin:0;accent-color:#ffa928}.muted{color:#777}.btn{min-height:29px;padding:4px 10px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#f1f1f1);color:#333;white-space:nowrap}.btn:hover:not(:disabled){border-color:#888;background:#eee}.btn:disabled{opacity:.52}.btn-primary{border-color:#ed9c21;background:#ffa928;color:#fff}.work-area{display:flex;flex:1;min-height:0;gap:10px;padding:7px}.grid-panel{display:flex;flex:1;min-width:0;min-height:0;flex-direction:column;border:1px solid #c7c7c7}.table-scroll{flex:1;min-height:0;overflow:auto}.data-grid{width:max-content;min-width:100%;border-collapse:collapse;table-layout:fixed;white-space:nowrap}.data-grid th{position:sticky;top:0;z-index:1;background:#48a3df;color:#fff;font-weight:500;text-align:left}.data-grid th,.data-grid td{height:29px;padding:4px 7px;border:1px solid #d2d2d2}.data-grid tbody tr{cursor:pointer}.data-grid tbody tr:hover{background:#fff4ca}.data-grid tbody tr.active{background:#ffe699}.data-grid tbody tr.selected:not(.active){background:#fff1c3}.row-selector{width:26px;text-align:center}.inventory-no{width:120px}.locator-no{width:100px}.locator-name{width:150px}.capacity{width:100px}.capacity-cell{text-align:right}.description{width:174px}.description-cell{max-width:174px;white-space:pre-wrap;overflow-wrap:anywhere}.audit-user{width:80px}.audit-date{width:150px}.empty{text-align:center;color:#777;padding:18px!important}.grid-footer{min-height:25px;padding:4px 8px;color:#666;border-top:1px solid #ccc}.side-actions{width:88px;display:flex;flex-direction:column;gap:6px;padding-top:0}.side-actions .btn{width:100%}.close-btn{margin-top:auto}.notice{margin:4px 12px;padding:7px 10px;border-radius:3px}.error{color:#9b1c1c;background:#fff1f0;border:1px solid #e6b3ae}.success{color:#32633a;background:#f0f8ef;border:1px solid #b9d6b7}.preview-note{margin:2px 14px 7px;font-size:12px;color:#666}.modal-backdrop,.picker-backdrop{position:fixed;inset:0;z-index:40;display:flex;align-items:center;justify-content:center;padding:20px;background:rgba(0,0,0,.28)}.editor-modal{box-sizing:border-box;width:min(508px,96vw);height:min(345px,94vh);display:flex;flex-direction:column;overflow:auto;border:1px solid #aaa;border-radius:4px;background:#fff;box-shadow:0 5px 24px #0004}.modal-heading{display:flex;align-items:center;justify-content:space-between;padding:5px 12px;border-bottom:1px solid #ccc;background:#f5f3ed}.modal-heading h2{margin:0;font-size:16px;font-weight:500}.icon-close{border:0;background:transparent;font-size:25px;line-height:1;color:#555}.editor-fields{box-sizing:border-box;flex:none;height:201px;margin:7px 10px 5px;padding:7px 9px 6px;border:1px solid #bbb}.editor-fields legend,.audit-fields legend{padding:0 5px;color:#555}.field-row{display:grid;grid-template-columns:88px minmax(0,1fr);align-items:center;gap:7px;min-height:29px}.field-row>label{text-align:right}.kd-input,.plain-input{box-sizing:border-box;width:100%;height:25px;border:1px solid #999;border-radius:0;padding:3px 6px;color:#222;font:inherit}.kd-input{background:#ffffc0}.kd-input:read-only{background:#eee}.plain-input{background:#fff}.picker-field{display:flex;gap:5px}.picker-field .kd-input{max-width:184px}.picker-button{min-width:28px;padding:2px 5px}.numeric-input{text-align:right}.description-row{align-items:start;padding-top:2px}.description-row>label{padding-top:5px}.description-input{height:54px;resize:none}.audit-fields{box-sizing:border-box;flex:none;display:grid;grid-template-columns:1fr 1fr;gap:5px 9px;height:62px;margin:0 10px;padding:5px 8px;border:1px solid #bbb}.audit-fields label{display:grid;grid-template-columns:58px minmax(0,1fr);align-items:center;gap:4px;white-space:nowrap;font-size:11px}.audit-fields input{box-sizing:border-box;min-width:0;width:100%;height:22px;border:1px solid #bbb;background:#eee;padding:2px 4px}.form-error{margin:2px 10px}.modal-actions{display:flex;justify-content:flex-end;gap:7px;margin-top:auto;padding:5px 10px 7px}.picker-backdrop{z-index:50}.inventory-picker{width:min(360px,92vw);max-height:70vh;display:flex;flex-direction:column;border:1px solid #aaa;background:#fff;box-shadow:0 5px 24px #0004}.picker-search{display:flex;align-items:center;gap:8px;padding:10px}.picker-search input{flex:1;min-width:0;height:28px}.picker-list{overflow:auto;padding:0 10px 10px}.picker-list button{display:block;width:100%;padding:7px;border:0;border-bottom:1px solid #ddd;background:white;text-align:left}.picker-list button:hover{background:#fff0ad}.picker-list p{color:#777}@media(max-width:850px){.query-strip{flex-wrap:wrap}.issue-filter{gap:5px;margin-right:0}.work-area{gap:6px;padding:5px}.side-actions{width:76px}.audit-fields{grid-template-columns:1fr}.editor-modal{height:auto;max-height:94vh}.editor-fields{height:auto}.modal-actions{margin-top:auto}}
</style>