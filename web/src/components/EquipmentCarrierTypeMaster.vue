<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from 'vue'

type EditMode = 'new' | 'edit' | 'copy' | null
type IssueState = -1 | 0 | 1 | 2
interface CarrierType {
  carrierType: string
  carrierCategory: string
  useLayer: number
  carrierLayer: number | null
  carrierCapacity: number | null
  description: string | null
  issueState: IssueState
  startPosition: number | null
  dataStamp: number
  creator: string | null
  createDate: string | null
  reviser: string | null
  reviseDate: string | null
}
interface CarrierCategory { carrierCategory: string; description?: string | null; issueState: number }
interface TypeForm {
  carrierType: string
  carrierCategory: string
  useLayer: number | null
  carrierLayer: string
  carrierCapacity: string
  startPosition: string
  description: string
}
interface AuditFields { creator: string; createDate: string; reviser: string; reviseDate: string }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const usesDevelopmentMock = computed(() => import.meta.env.DEV && props.developmentPreview)
const typeStorageKey = 'ymes-dev-mock:equipment-carrier-types:v1'
const categoryStorageKey = 'ymes-dev-mock:equipment-carrier-categories:v1'
const issueStates: { value: IssueState; label: string }[] = [
  { value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' },
]
const seedRows: CarrierType[] = [
  { carrierType: 'DEV-TYPE-01', carrierCategory: 'DEV-CARRIER-03', useLayer: 1, carrierLayer: 2, carrierCapacity: 20, description: 'DEV 假資料：雙層載具', issueState: 0, startPosition: 1, dataStamp: 0, creator: 'DEV001', createDate: '2026-11-02T08:00:00+08:00', reviser: null, reviseDate: null },
  { carrierType: 'DEV-TYPE-02', carrierCategory: 'DEV-CARRIER-03', useLayer: 0, carrierLayer: 1, carrierCapacity: 1, description: 'DEV 假資料：不使用層數', issueState: 1, startPosition: 1, dataStamp: 1, creator: 'DEV001', createDate: '2026-11-03T08:00:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-04T09:00:00+08:00' },
  { carrierType: 'DEV-TYPE-03', carrierCategory: 'DEV-CARRIER-03', useLayer: 1, carrierLayer: 4, carrierCapacity: 40, description: 'DEV 假資料：已簽核載具類別', issueState: 2, startPosition: 2, dataStamp: 2, creator: 'DEV001', createDate: '2026-11-05T08:00:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-06T09:00:00+08:00' },
  { carrierType: 'DEV-TYPE-04', carrierCategory: 'DEV-CARRIER-03', useLayer: 1, carrierLayer: 1, carrierCapacity: 10, description: 'DEV 假資料：停用載具類別', issueState: -1, startPosition: 1, dataStamp: 3, creator: 'DEV001', createDate: '2026-11-07T08:00:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-08T09:00:00+08:00' },
]
const seedCategories: CarrierCategory[] = [{ carrierCategory: 'DEV-CARRIER-03', description: 'DEV 假資料：已簽核載具分類', issueState: 2 }]

const rows = ref<CarrierType[]>([])
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
const form = ref<TypeForm>(emptyForm())
const formDataStamp = ref(0)
const audit = ref<AuditFields>({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const formDirty = ref(false)
const categoryOptions = ref<CarrierCategory[]>([])
const categoryDialogOpen = ref(false)
const categoryLoading = ref(false)
const categorySearch = ref('')
const categoryError = ref('')
const typeInput = ref<HTMLInputElement | null>(null)
const categoryButton = ref<HTMLButtonElement | null>(null)
const loadRequestId = ref(0)

const visibleRows = computed(() => rows.value
  .filter((row) => row.issueState === issueState.value)
  .filter((row) => !appliedQuery.value || row.carrierType.toLocaleLowerCase().includes(appliedQuery.value.toLocaleLowerCase()))
  .slice().sort((a, b) => a.carrierType.localeCompare(b.carrierType)))
const current = computed(() => visibleRows.value.find((row) => row.carrierType === currentNo.value) ?? null)
const filteredCategoryOptions = computed(() => categoryOptions.value
  .filter((row) => row.issueState === 2)
  .filter((row) => !categorySearch.value || row.carrierCategory.toLocaleLowerCase().includes(categorySearch.value.toLocaleLowerCase())))
const formTitle = computed(() => mode.value === 'edit' ? '載具類別資料定義(EDIT)' : mode.value === 'copy' ? '載具類別資料定義(COPY)' : '載具類別資料定義(ADD)')

function emptyForm(): TypeForm {
  return { carrierType: '', carrierCategory: '', useLayer: null, carrierLayer: '', carrierCapacity: '', startPosition: '1', description: '' }
}
function userNo(): string { return props.currentUserNo?.trim() || (usesDevelopmentMock.value ? 'DEV001' : '') }
function cloneRows<T>(value: T[]): T[] { return JSON.parse(JSON.stringify(value)) as T[] }
function readRows(): CarrierType[] {
  try { const raw = localStorage.getItem(typeStorageKey); return raw ? JSON.parse(raw) as CarrierType[] : cloneRows(seedRows) }
  catch { return cloneRows(seedRows) }
}
function writeRows(value: CarrierType[]): void { localStorage.setItem(typeStorageKey, JSON.stringify(value)) }
function readCategories(): CarrierCategory[] {
  try {
    const raw = localStorage.getItem(categoryStorageKey)
    return (raw ? JSON.parse(raw) as CarrierCategory[] : cloneRows(seedCategories)).filter((row) => row.issueState === 2)
  } catch { return cloneRows(seedCategories) }
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
    throw new Error(problem?.detail || problem?.message || problem?.title || '載具類別 API 回應錯誤 (' + response.status + ')。')
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function setQuery(): void { appliedQuery.value = queryText.value.trim(); selectedNos.value = []; void loadRows() }
function setIssueState(value: IssueState): void {
  issueState.value = value
  selectedNos.value = []
  if (!visibleRows.value.some((row) => row.carrierType === currentNo.value)) currentNo.value = visibleRows.value[0]?.carrierType ?? ''
}
function selectRow(no: string): void { currentNo.value = no }
function toggleSelected(no: string): void {
  selectedNos.value = selectedNos.value.includes(no) ? selectedNos.value.filter((value) => value !== no) : [...selectedNos.value, no]
  selectRow(no)
}
function markDirty(): void { formDirty.value = true }
function normalizeType(): void { form.value.carrierType = form.value.carrierType.trim().toUpperCase() }
function setUseLayer(value: 0 | 1): void {
  form.value.useLayer = value
  if (value === 0) {
    form.value.carrierLayer = '1'
    form.value.carrierCapacity = '1'
    form.value.startPosition = '1'
  }
  markDirty()
}
function focusCategory(): void { categoryButton.value?.focus() }
function closeForm(): void { if (busy.value) return; mode.value = null; formDirty.value = false; categoryDialogOpen.value = false }

async function openForm(nextMode: Exclude<EditMode, null>): Promise<void> {
  const row = current.value
  if (nextMode !== 'new' && !row) { error.value = '請先選取一筆載具類別資料。'; return }
  if (nextMode === 'edit' && row?.issueState !== 0) { error.value = '只有未簽核的載具類別可以修改。'; return }
  mode.value = nextMode
  error.value = ''
  notice.value = ''
  formDirty.value = false
  sourceNo.value = nextMode === 'edit' ? row?.carrierType ?? '' : ''
  formDataStamp.value = row?.dataStamp ?? 0
  const now = new Date().toISOString()
  if (nextMode === 'new') {
    form.value = emptyForm()
    audit.value = { creator: userNo(), createDate: now, reviser: '', reviseDate: '' }
  } else if (row) {
    form.value = {
      carrierType: row.carrierType,
      carrierCategory: row.carrierCategory,
      useLayer: row.useLayer,
      carrierLayer: String(row.useLayer === 0 ? 1 : row.carrierLayer ?? ''),
      carrierCapacity: String(row.useLayer === 0 ? 1 : row.carrierCapacity ?? ''),
      startPosition: String(row.useLayer === 0 ? 1 : row.startPosition ?? 1),
      description: row.description || '',
    }
    audit.value = nextMode === 'edit'
      ? { creator: row.creator || '', createDate: row.createDate || '', reviser: userNo(), reviseDate: now }
      : { creator: userNo(), createDate: now, reviser: '', reviseDate: '' }
  }
  await nextTick()
  if (nextMode !== 'edit') typeInput.value?.focus()
}

function validateForm(): string {
  normalizeType()
  if (!form.value.carrierType) return 'Carrier Type 為必填。'
  if (form.value.carrierType.length > 50) return 'Carrier Type 不可超過 50 個字元。'
  if (!form.value.carrierCategory) return 'Carrier Category 為必填。'
  if (form.value.carrierCategory.length > 50) return 'Carrier Category 不可超過 50 個字元。'
  if (form.value.description.length > 255) return 'Description 不可超過資料庫欄位長度 255。'
  const startPosition = Number(form.value.startPosition)
  if (!form.value.startPosition.trim() || !Number.isInteger(startPosition) || startPosition <= 0) return 'Start Position 必須是正整數。'
  if (form.value.useLayer !== 0 && form.value.useLayer !== 1) return '請選取 Use Layer。'
  if (form.value.useLayer === 1) {
    const layer = Number(form.value.carrierLayer)
    const capacity = Number(form.value.carrierCapacity)
    if (!form.value.carrierLayer.trim() || !Number.isInteger(layer) || layer <= 0 || layer > 999) return 'Carrier Layer 必須是 1 到 999 的整數。'
    if (!form.value.carrierCapacity.trim() || !Number.isInteger(capacity) || capacity <= 0 || capacity > 999) return 'Carrier Capacity 必須是 1 到 999 的整數。'
  }
  if (startPosition > 999) return 'Start Position 不可超過 999。'
  if (!categoryOptions.value.some((item) => item.issueState === 2 && item.carrierCategory === form.value.carrierCategory)) return '請選取已核准的 Carrier Category。'
  return ''
}

async function saveForm(keepOpen: boolean): Promise<void> {
  if (!mode.value || busy.value) return
  const problem = validateForm()
  if (problem) { error.value = problem; return }
  const saveMode = mode.value
  const type = form.value.carrierType
  const description = form.value.description.trim() || null
  const useLayer = form.value.useLayer as 0 | 1
  const carrierLayer = useLayer === 0 ? 1 : Number(form.value.carrierLayer)
  const carrierCapacity = useLayer === 0 ? 1 : Number(form.value.carrierCapacity)
  const startPosition = useLayer === 0 ? 1 : Number(form.value.startPosition)
  const now = new Date().toISOString()
  if (saveMode === 'edit') audit.value = { ...audit.value, reviser: userNo(), reviseDate: now }
  busy.value = true
  error.value = ''
  notice.value = ''
  try {
    let nextDataStamp = formDataStamp.value
    if (usesDevelopmentMock.value) {
      const allRows = readRows()
      if (saveMode === 'edit') {
        const index = allRows.findIndex((item) => item.carrierType === sourceNo.value)
        if (index < 0) throw new Error('載具類別資料已不存在，請重新載入。')
        if (allRows[index].issueState !== 0) throw new Error('只有未簽核的載具類別可以修改。')
        if (allRows[index].dataStamp !== formDataStamp.value) throw new Error('資料已被其他使用者修改，請重新載入。')
        nextDataStamp = allRows[index].dataStamp + 1
        allRows[index] = { ...allRows[index], carrierCategory: form.value.carrierCategory, useLayer, carrierLayer, carrierCapacity, startPosition, description, dataStamp: nextDataStamp, reviser: audit.value.reviser, reviseDate: audit.value.reviseDate }
      } else {
        if (allRows.some((item) => item.carrierType.toLocaleUpperCase() === type)) throw new Error('Carrier Type 已存在。')
        allRows.push({ carrierType: type, carrierCategory: form.value.carrierCategory, useLayer, carrierLayer, carrierCapacity, startPosition, description, issueState: 0, dataStamp: 0, creator: audit.value.creator, createDate: audit.value.createDate, reviser: null, reviseDate: null })
        nextDataStamp = 0
      }
      writeRows(allRows)
    } else {
      const body = saveMode === 'edit'
        ? { carrierCategory: form.value.carrierCategory, useLayer, carrierLayer, carrierCapacity, startPosition, description, issueState: 0, dataStamp: formDataStamp.value }
        : { carrierType: type, carrierCategory: form.value.carrierCategory, useLayer, carrierLayer, carrierCapacity, startPosition, description }
      const result = await request<{ dataStamp?: number }>(saveMode === 'edit' ? 'api/equipment/carrier-types/' + encodeURIComponent(sourceNo.value) : 'api/equipment/carrier-types', {
        method: saveMode === 'edit' ? 'PUT' : 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      })
      if (result?.dataStamp !== undefined) nextDataStamp = result.dataStamp
      else if (saveMode === 'edit') nextDataStamp += 1
    }
    currentNo.value = type
    await loadRows()
    const saved = rows.value.find((item) => item.carrierType === type)
    if (saved) audit.value = { creator: saved.creator || '', createDate: saved.createDate || '', reviser: saved.reviser || '', reviseDate: saved.reviseDate || '' }
    formDataStamp.value = nextDataStamp
    formDirty.value = false
    notice.value = saveMode === 'edit' ? '載具類別資料已修改。' : saveMode === 'copy' ? '載具類別資料已複製。' : '載具類別資料已新增。'
    if (!keepOpen) closeForm()
  } catch (e) { error.value = e instanceof Error ? e.message : '載具類別資料儲存失敗。' }
  finally { busy.value = false }
}

async function deleteSelected(): Promise<void> {
  if (busy.value) return
  const targetsByNo = new Set(selectedNos.value)
  if (current.value) targetsByNo.add(current.value.carrierType)
  const targets = visibleRows.value.filter((row) => targetsByNo.has(row.carrierType))
  if (!targets.length) { error.value = '請先選取要刪除的載具類別資料。'; return }
  if (!window.confirm('確定刪除／停用選取的 ' + targets.length + ' 筆載具類別資料？')) return
  busy.value = true; error.value = ''; notice.value = ''
  let changed = 0
  let failure = ''
  try {
    for (const row of targets) {
      if (![0, 2, -1].includes(row.issueState)) throw new Error('簽核中的載具類別不可刪除。')
      if (usesDevelopmentMock.value) {
        const allRows = readRows()
        const index = allRows.findIndex((item) => item.carrierType === row.carrierType)
        if (index < 0) throw new Error('載具類別資料已不存在。')
        if (allRows[index].dataStamp !== row.dataStamp) throw new Error('資料已被其他使用者修改。')
        if (row.issueState === 2) allRows[index] = { ...allRows[index], issueState: -1, dataStamp: allRows[index].dataStamp + 1, reviser: userNo(), reviseDate: new Date().toISOString() }
        else allRows.splice(index, 1)
        writeRows(allRows)
      } else {
        const query = new URLSearchParams({ issueState: String(row.issueState), dataStamp: String(row.dataStamp) })
        await request<void>('api/equipment/carrier-types/' + encodeURIComponent(row.carrierType) + '?' + query.toString(), { method: 'DELETE' })
      }
      changed += 1
    }
    selectedNos.value = []
  } catch (e) { failure = e instanceof Error ? e.message : '載具類別資料刪除失敗。' }
  finally { busy.value = false }
  if (changed) await loadRows()
  if (failure) error.value = changed ? failure + ' 已完成 ' + changed + ' 筆。' : failure
  else if (changed) notice.value = '已完成 ' + changed + ' 筆刪除／停用。'
}

async function approveSelected(): Promise<void> {
  if (busy.value) return
  const targetsByNo = new Set(selectedNos.value)
  if (current.value) targetsByNo.add(current.value.carrierType)
  const targets = visibleRows.value.filter((row) => targetsByNo.has(row.carrierType))
  if (!targets.length) { error.value = '請先選取要核准的載具類別資料。'; return }
  if (!window.confirm('確定核准選取的 ' + targets.length + ' 筆載具類別資料？')) return
  busy.value = true; error.value = ''; notice.value = ''
  let changed = 0
  let failure = ''
  try {
    for (const row of targets) {
      if (row.issueState !== 0) throw new Error('只有未簽核的載具類別可以核准。')
      if (usesDevelopmentMock.value) {
        const allRows = readRows()
        const index = allRows.findIndex((item) => item.carrierType === row.carrierType)
        if (index < 0) throw new Error('載具類別資料已不存在。')
        if (allRows[index].dataStamp !== row.dataStamp) throw new Error('資料已被其他使用者修改。')
        allRows[index] = { ...allRows[index], issueState: 2, dataStamp: allRows[index].dataStamp + 1, reviser: userNo(), reviseDate: new Date().toISOString() }
        writeRows(allRows)
      } else {
        await request<void>('api/equipment/carrier-types/' + encodeURIComponent(row.carrierType) + '/approve', {
          method: 'POST', headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ issueState: row.issueState, dataStamp: row.dataStamp }),
        })
      }
      changed += 1
    }
    selectedNos.value = []
  } catch (e) { failure = e instanceof Error ? e.message : '載具類別核准失敗。' }
  finally { busy.value = false }
  if (changed) await loadRows()
  if (failure) error.value = changed ? failure + ' 已完成 ' + changed + ' 筆。' : failure
  else if (changed) notice.value = usesDevelopmentMock.value ? 'DEV 簡化核准已完成 ' + changed + ' 筆（直接轉為已簽核）。' : '已核准 ' + changed + ' 筆載具類別資料。'
}

async function loadRows(): Promise<void> {
  const requestId = ++loadRequestId.value
  loading.value = true; error.value = ''; notice.value = ''
  try {
    let data: CarrierType[]
    if (usesDevelopmentMock.value) data = readRows()
    else {
      const query = new URLSearchParams()
      if (appliedQuery.value) query.set('carrierType', appliedQuery.value)
      data = await request<CarrierType[]>('api/equipment/carrier-types?' + query.toString())
    }
    if (requestId !== loadRequestId.value) return
    rows.value = data.slice().sort((a, b) => a.carrierType.localeCompare(b.carrierType))
    selectedNos.value = selectedNos.value.filter((no) => visibleRows.value.some((row) => row.carrierType === no))
    if (!visibleRows.value.some((row) => row.carrierType === currentNo.value)) currentNo.value = visibleRows.value[0]?.carrierType ?? ''
  } catch (e) {
    if (requestId !== loadRequestId.value) return
    rows.value = []; currentNo.value = ''; selectedNos.value = []
    error.value = e instanceof Error ? e.message : '載具類別資料載入失敗。'
  } finally { if (requestId === loadRequestId.value) loading.value = false }
}

async function openCategoryPicker(): Promise<void> {
  categoryDialogOpen.value = true
  categorySearch.value = ''
  categoryError.value = ''
  categoryLoading.value = true
  try {
    if (usesDevelopmentMock.value) categoryOptions.value = readCategories()
    else categoryOptions.value = await request<CarrierCategory[]>('api/equipment/carrier-categories?issueState=2')
    if (!categoryOptions.value.length) categoryError.value = '沒有可選取的已核准載具分類。'
  } catch (e) { categoryOptions.value = []; categoryError.value = e instanceof Error ? e.message : '載具分類資料載入失敗。' }
  finally { categoryLoading.value = false }
}
function chooseCategory(category: CarrierCategory): void {
  form.value.carrierCategory = category.carrierCategory
  formDirty.value = true
  categoryDialogOpen.value = false
}
function closePage(): void { window.history.pushState({}, '', appBase || '/'); window.dispatchEvent(new PopStateEvent('popstate')) }

onMounted(() => { void loadRows() })
watch(() => [props.developmentPreview, props.apiBaseUrl], () => {
  closeForm(); selectedNos.value = []; categoryOptions.value = []
  void loadRows()
})
</script>

<template>
  <section class="carrier-type-page">
    <div v-if="usesDevelopmentMock" class="carrier-type-dev-note">開發模式：目前資料為 DEV 假資料，儲存在此瀏覽器；核准流程已簡化，不會寫入 MES 或正式資料庫。</div>
    <div class="carrier-type-search">
      <div class="carrier-type-states" role="radiogroup" aria-label="簽核狀態">
        <label v-for="state in issueStates" :key="state.value"><input type="radio" name="carrierTypeIssueState" :checked="issueState===state.value" @change="setIssueState(state.value)"> {{ state.label }}</label>
      </div>
      <label for="carrierTypeSearch">載具類別編號</label>
      <input id="carrierTypeSearch" v-model="queryText" type="text" @keydown.enter.prevent="setQuery">
      <span class="carrier-type-like">%</span>
      <button type="button" class="carrier-type-primary" :disabled="loading" @click="setQuery">查詢</button>
    </div>
    <p v-if="error && !mode" class="carrier-type-message carrier-type-error" role="alert">{{ error }}</p>
    <p v-else-if="notice && !mode" class="carrier-type-message carrier-type-success" role="status">{{ notice }}</p>
    <div class="carrier-type-main">
      <div class="carrier-type-grid-wrap">
        <table class="carrier-type-grid">
          <thead><tr><th class="carrier-type-check"></th><th>載具類別</th><th>載具分類</th><th>Use Layer</th><th>Carrier Layer</th><th>Carrier Capacity</th><th>Start Position</th><th>Description</th><th>Creator</th><th>Create Date</th><th>Reviser</th><th>Revise Date</th></tr></thead>
          <tbody>
            <tr v-if="loading"><td colspan="12" class="carrier-type-empty">載入中…</td></tr>
            <tr v-else-if="visibleRows.length===0"><td colspan="12" class="carrier-type-empty">沒有符合的載具類別資料。</td></tr>
            <tr v-for="row in visibleRows" :key="row.carrierType" :class="{ 'carrier-type-current': currentNo===row.carrierType, 'carrier-type-marked': selectedNos.includes(row.carrierType) }" @click="selectRow(row.carrierType)" @dblclick="selectRow(row.carrierType); openForm('edit')">
              <td class="carrier-type-check" @click.stop><input type="checkbox" :checked="selectedNos.includes(row.carrierType)" :aria-label="'選取 ' + row.carrierType" @change="toggleSelected(row.carrierType)"></td>
              <td>{{ row.carrierType }}</td><td>{{ row.carrierCategory }}</td><td>{{ row.useLayer===1 ? '使用' : '不使用' }}</td><td class="carrier-type-number">{{ row.carrierLayer }}</td><td class="carrier-type-number">{{ row.carrierCapacity }}</td><td class="carrier-type-number">{{ row.startPosition }}</td><td>{{ row.description || '' }}</td><td>{{ row.creator || '' }}</td><td>{{ dateText(row.createDate) }}</td><td>{{ row.reviser || '' }}</td><td>{{ dateText(row.reviseDate) }}</td>
            </tr>
          </tbody>
        </table>
      </div>
      <aside class="carrier-type-actions">
        <button type="button" class="carrier-type-primary" :disabled="busy" @click="openForm('new')">新增(A)</button>
        <button type="button" :disabled="busy || current?.issueState!==0" @click="openForm('edit')">修改(E)</button>
        <button type="button" :disabled="busy || (!selectedNos.length && !current)" @click="deleteSelected">刪除(D)</button>
        <button type="button" :disabled="busy || !current" @click="openForm('copy')">複製(C)</button>
        <button type="button" :disabled="busy || (!selectedNos.length && !current)" @click="approveSelected">核准</button>
        <button type="button" class="carrier-type-close-page" :disabled="busy" @click="closePage">關閉</button>
      </aside>
    </div>

    <div v-if="mode" class="carrier-type-overlay" @click.self="closeForm">
      <form class="carrier-type-dialog" @submit.prevent="saveForm(false)">
        <div class="carrier-type-dialog-body">
          <fieldset class="carrier-type-fields">
            <legend>Carrier Type</legend>
            <label><span>Carrier Type</span><input ref="typeInput" v-model="form.carrierType" type="text" :readonly="mode==='edit'" @input="markDirty" @blur="normalizeType" @keydown.enter.prevent="focusCategory"></label>
            <label class="carrier-type-category"><span>Carrier Category</span><input :value="form.carrierCategory" type="text" readonly @click="openCategoryPicker"><button ref="categoryButton" type="button" aria-label="選取載具分類" :disabled="busy" @click="openCategoryPicker">…</button></label>
            <div class="carrier-type-use"><span>Use Layer</span><label><input type="radio" name="carrierTypeUseLayer" :checked="form.useLayer===0" @change="setUseLayer(0)"> Not Use</label><label><input type="radio" name="carrierTypeUseLayer" :checked="form.useLayer===1" @change="setUseLayer(1)"> Use</label></div>
            <label><span>Carrier Layer</span><input v-model="form.carrierLayer" type="text" inputmode="numeric" :disabled="form.useLayer!==1" @input="markDirty" @keydown.enter.prevent></label>
            <label><span>Carrier Capacity</span><input v-model="form.carrierCapacity" type="text" inputmode="numeric" :disabled="form.useLayer!==1" @input="markDirty" @keydown.enter.prevent></label>
            <label><span>Start Position</span><input v-model="form.startPosition" type="text" inputmode="numeric" :disabled="form.useLayer===0" @input="markDirty" @keydown.enter.prevent></label>
            <label class="carrier-type-description"><span>Description</span><textarea v-model="form.description" rows="3" @input="markDirty"></textarea></label>
          </fieldset>
          <fieldset class="carrier-type-audit">
            <legend>建立／修改資料</legend>
            <label><span>建立人</span><input :value="audit.creator" readonly></label><label><span>建立日期</span><input :value="dateText(audit.createDate)" readonly></label>
            <label><span>修改人</span><input :value="audit.reviser" readonly></label><label><span>修改日期</span><input :value="dateText(audit.reviseDate)" readonly></label>
          </fieldset>
          <p v-if="error" class="carrier-type-message carrier-type-error" role="alert">{{ error }}</p>
        </div>
        <footer class="carrier-type-dialog-footer">
          <button type="submit" class="carrier-type-primary" :disabled="busy">確定</button><button type="button" :disabled="busy" @click="closeForm">關閉</button><button type="button" :disabled="busy || !formDirty" @click="saveForm(true)">套用</button>
        </footer>
      </form>
    </div>

    <div v-if="categoryDialogOpen" class="carrier-type-overlay carrier-type-category-overlay" @click.self="categoryDialogOpen=false">
      <section class="carrier-type-category-dialog" role="dialog" aria-modal="true" aria-label="選取已核准的載具分類">
        <header><strong>選取載具分類</strong><button type="button" aria-label="關閉" @click="categoryDialogOpen=false">×</button></header>
        <label class="carrier-type-category-search"><span>載具分類</span><input v-model="categorySearch" type="text" @keydown.enter.prevent></label>
        <div class="carrier-type-category-list">
          <p v-if="categoryLoading" class="carrier-type-empty">載入中…</p><p v-else-if="categoryError" class="carrier-type-error">{{ categoryError }}</p><p v-else-if="filteredCategoryOptions.length===0" class="carrier-type-empty">沒有符合的已核准載具分類。</p>
          <table v-else><thead><tr><th>載具分類</th><th>Description</th></tr></thead><tbody><tr v-for="item in filteredCategoryOptions" :key="item.carrierCategory" @dblclick="chooseCategory(item)"><td>{{ item.carrierCategory }}</td><td>{{ item.description || '' }}</td><td><button type="button" @click="chooseCategory(item)">選取</button></td></tr></tbody></table>
        </div>
        <footer><button type="button" @click="categoryDialogOpen=false">關閉</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.carrier-type-page{display:flex;height:100%;min-height:0;flex-direction:column;gap:6px;color:#333;font-size:13px}
.carrier-type-dev-note{padding:7px 10px;border:1px solid #d8c577;background:#fff6c8;color:#66551b}
.carrier-type-search{display:flex;min-height:38px;align-items:center;gap:7px;padding:5px 9px;border:1px solid #c9c9c9;background:#fafafa}
.carrier-type-states{display:flex;align-items:center;gap:13px;margin-right:10px}
.carrier-type-states label{display:flex;align-items:center;gap:3px;white-space:nowrap}
.carrier-type-states input,.carrier-type-check input,.carrier-type-use input{margin:0;accent-color:#ad831b}
.carrier-type-search>input{width:min(270px,28vw);min-height:29px;padding:4px 7px;border:1px solid #a9a9a9;background:#fff0b3;color:#333;font:inherit}
.carrier-type-like{margin-left:-7px;color:#555}
.carrier-type-search button,.carrier-type-actions button,.carrier-type-dialog-footer button,.carrier-type-category-dialog button{min-height:30px;padding:4px 12px;border:1px solid #bdbdbd;border-radius:3px;background:#fafafa;color:#333;font:inherit;cursor:pointer}
.carrier-type-search button:hover,.carrier-type-actions button:hover,.carrier-type-dialog-footer button:hover,.carrier-type-category-dialog button:hover{background:#fff2c3;border-color:#c8aa54}
.carrier-type-search button:disabled,.carrier-type-actions button:disabled,.carrier-type-dialog-footer button:disabled{opacity:.55;cursor:not-allowed}
.carrier-type-primary{background:#f5a623!important;border-color:#e49a24!important;color:#fff!important}
.carrier-type-message{margin:0;padding:7px 10px;border:1px solid}
.carrier-type-error{border-color:#dca7a2;background:#fff1ef;color:#942f24}
.carrier-type-success{border-color:#c8d8ac;background:#f4f8eb;color:#496622}
.carrier-type-main{display:grid;min-height:0;flex:1;grid-template-columns:minmax(0,1fr) 88px;gap:7px}
.carrier-type-grid-wrap{min-height:0;overflow:auto;border:1px solid #bdbdbd;background:#fff}
.carrier-type-grid{width:100%;border-collapse:collapse;white-space:nowrap}
.carrier-type-grid th{position:sticky;top:0;z-index:1;padding:5px 7px;border:1px solid #d2c36d;background:#e7d978;color:#453d20;text-align:left;font-weight:600}
.carrier-type-grid td{height:27px;padding:4px 7px;border:1px solid #d6d6d6}
.carrier-type-grid tbody tr{cursor:pointer}.carrier-type-grid tbody tr:nth-child(even){background:#fafafa}.carrier-type-grid tbody tr:hover{background:#fff6d6}
.carrier-type-grid tbody tr.carrier-type-marked{background:#fff7dc}.carrier-type-grid tbody tr.carrier-type-current{background:#fff0b7}
.carrier-type-check{width:28px;text-align:center}.carrier-type-number{text-align:right}
.carrier-type-empty{height:45px!important;color:#777;text-align:center}
.carrier-type-actions{display:flex;min-height:0;flex-direction:column;gap:6px;padding-top:0}.carrier-type-actions button{width:100%;min-height:32px}.carrier-type-close-page{margin-top:auto!important}
.carrier-type-overlay{position:fixed;inset:0;z-index:80;display:grid;place-items:center;padding:16px;background:rgba(0,0,0,.35)}
.carrier-type-dialog{display:flex;width:min(503px,96vw);height:min(410px,94vh);max-height:94vh;flex-direction:column;border:1px solid #999;background:#fdfdfd;box-shadow:0 5px 20px rgba(0,0,0,.24)}
.carrier-type-dialog-body{display:flex;min-height:0;flex:1;flex-direction:column;gap:7px;overflow:auto;padding:10px 12px 0}
.carrier-type-fields{display:flex;flex:none;flex-direction:column;gap:4px;margin:0;padding:7px 9px 9px;border:1px solid #bdbdbd}
.carrier-type-fields legend,.carrier-type-audit legend{padding:0 4px;color:#555}
.carrier-type-fields>label{display:grid;min-height:23px;grid-template-columns:101px minmax(0,1fr);align-items:center;gap:6px}
.carrier-type-fields>label>span,.carrier-type-use>span{color:#555;text-align:right}
.carrier-type-fields input,.carrier-type-fields textarea{min-width:0;padding:3px 6px;border:1px solid #a9a9a9;background:#fff0b3;color:#333;font:inherit}
.carrier-type-fields input{width:152px;min-height:22px}.carrier-type-fields input[readonly]{background:#eee}.carrier-type-fields input:disabled{background:#eee;color:#666}
.carrier-type-fields .carrier-type-category{grid-template-columns:101px 152px 24px;gap:6px}
.carrier-type-category input{width:152px}.carrier-type-category button{width:24px;height:24px;padding:0}
.carrier-type-use{display:flex;min-height:23px;align-items:center;gap:9px;padding-left:0}
.carrier-type-use>span{width:101px;flex:none}.carrier-type-use label{display:flex;align-items:center;gap:4px;min-width:70px}
.carrier-type-description{align-items:start!important}.carrier-type-fields textarea{width:min(352px,100%);min-height:59px;resize:vertical}
.carrier-type-audit{display:grid;flex:none;grid-template-columns:1fr 1fr;gap:4px 8px;margin:0;padding:5px 8px 7px;border:1px solid #bdbdbd}
.carrier-type-audit label{display:flex;min-width:0;align-items:center;gap:5px}.carrier-type-audit label span{width:56px;flex:none;color:#555;text-align:right}
.carrier-type-audit input{flex:1;min-width:0;min-height:25px;padding:3px 5px;border:1px solid #c5c5c5;background:#eee;color:#555;font:inherit}
.carrier-type-dialog-footer{display:flex;min-height:48px;align-items:center;justify-content:flex-end;gap:7px;padding:7px 12px;border-top:1px solid #ccc}
.carrier-type-category-overlay{z-index:90}.carrier-type-category-dialog{display:flex;width:min(560px,94vw);height:min(430px,88vh);flex-direction:column;border:1px solid #999;background:#fff;box-shadow:0 5px 20px rgba(0,0,0,.24)}
.carrier-type-category-dialog>header{display:flex;min-height:38px;align-items:center;justify-content:space-between;padding:6px 12px;border-bottom:1px solid #ccc;background:#f4f1e8}
.carrier-type-category-dialog>header button{min-width:30px;padding:0;border:0;background:transparent;font-size:22px}
.carrier-type-category-search{display:flex;align-items:center;gap:8px;padding:9px 12px}.carrier-type-category-search input{min-height:29px;flex:1;padding:4px 7px;border:1px solid #aaa;background:#fff0b3;font:inherit}
.carrier-type-category-list{min-height:0;flex:1;overflow:auto;margin:0 12px;border:1px solid #bbb}.carrier-type-category-list table{width:100%;border-collapse:collapse}.carrier-type-category-list th{position:sticky;top:0;padding:6px;border:1px solid #d2c36d;background:#e7d978;text-align:left}.carrier-type-category-list td{padding:6px;border:1px solid #ddd}.carrier-type-category-list tbody tr:hover{background:#fff6d6}
.carrier-type-category-dialog>footer{display:flex;justify-content:flex-end;padding:8px 12px}
@media(max-width:800px){.carrier-type-search{flex-wrap:wrap}.carrier-type-states{width:100%;flex-wrap:wrap}.carrier-type-grid{min-width:1450px}.carrier-type-dialog{width:96vw;height:94vh}.carrier-type-audit{grid-template-columns:1fr}}
</style>
