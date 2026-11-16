<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

interface ComputerAreaRow {
  computerNo: string
  factoryNo: string
  creator: string | null
  createDate: string | null
  reviser: string | null
  reviseDate: string | null
  dataStamp: number | null
  areas: string[]
}
interface FactoryOption { factoryNo: string; description: string }
interface AreaOption { areaNo: string; areaName: string; description?: string | null; issueState?: number; areaClass?: number; factoryNo?: string }
interface ComputerAreaForm { computerNo: string; factoryNo: string }
type FormMode = 'add' | 'edit' | 'copy' | null

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mockMode = computed(() => import.meta.env.DEV && props.developmentPreview)
const rowsKey = 'ymes-dev-mock:smd-computer-area:v1'
const filters = ref({ computerNo: '', factoryNo: '' })
const rows = ref<ComputerAreaRow[]>([])
const factories = ref<FactoryOption[]>([])
const areas = ref<AreaOption[]>([])
const selectedKeys = ref<string[]>([])
const activeKey = ref('')
const anchorKey = ref('')
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const formError = ref('')
const formMode = ref<FormMode>(null)
const form = ref<ComputerAreaForm>({ computerNo: '', factoryNo: '' })
const selectedAreaNos = ref<string[]>([])
const dataStamp = ref<number | null>(null)
const audit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const factoryPickerOpen = ref(false)
const factorySearch = ref('')
const activeRow = computed(() => rows.value.find(row => rowKey(row) === activeKey.value) ?? null)
const visibleFactories = computed(() => {
  const query = factorySearch.value.trim().toLocaleLowerCase()
  return factories.value.filter(item => !query || `${item.factoryNo} ${item.description}`.toLocaleLowerCase().includes(query))
})

const seedFactories: FactoryOption[] = [
  { factoryNo: 'DEV-FAB', description: '開發用工廠' },
  { factoryNo: 'DEV-FAB-2', description: '開發用第二工廠' },
]
const seedAreas: AreaOption[] = [
  { areaNo: 'DEV-AREA-01', areaName: '開發用組裝區', description: 'DEV 假資料', factoryNo: 'DEV-FAB', issueState: 2, areaClass: 0 },
  { areaNo: 'DEV-AREA-02', areaName: '開發用檢查區', description: 'DEV 假資料', factoryNo: 'DEV-FAB', issueState: 2, areaClass: 0 },
  { areaNo: 'DEV-AREA-03', areaName: '開發用包裝區', description: 'DEV 假資料', factoryNo: 'DEV-FAB', issueState: 2, areaClass: 0 },
  { areaNo: 'DEV-AREA-11', areaName: '第二工廠測試區', description: 'DEV 假資料', factoryNo: 'DEV-FAB-2', issueState: 2, areaClass: 0 },
]
const seedRows: ComputerAreaRow[] = [
  { computerNo: 'DEV-POST-01', factoryNo: 'DEV-FAB', creator: 'DEV001', createDate: '2026-11-10T08:00:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-11T09:20:00+08:00', dataStamp: 1, areas: ['DEV-AREA-01', 'DEV-AREA-02'] },
  { computerNo: 'DEV-POST-02', factoryNo: 'DEV-FAB', creator: 'DEV001', createDate: '2026-11-12T10:15:00+08:00', reviser: null, reviseDate: null, dataStamp: 0, areas: ['DEV-AREA-03'] },
]

function rowKey(row: Pick<ComputerAreaRow, 'computerNo' | 'factoryNo'>): string { return `${row.computerNo}\u0000${row.factoryNo}` }
function sortRows(value: ComputerAreaRow[]): ComputerAreaRow[] { return [...value].sort((a, b) => a.computerNo.localeCompare(b.computerNo) || a.factoryNo.localeCompare(b.factoryNo)) }
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
    if (response.status === 404) throw new Error('過帳電腦之區域 API 尚未建立；請依 todolist_homepc.md 完成 SMD SD106 API。')
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 回應錯誤 (${response.status})。`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function readRows(): ComputerAreaRow[] {
  try { const value = window.localStorage.getItem(rowsKey); return value ? JSON.parse(value) as ComputerAreaRow[] : structuredClone(seedRows) }
  catch { return structuredClone(seedRows) }
}
function saveRows(value: ComputerAreaRow[]): void { window.localStorage.setItem(rowsKey, JSON.stringify(value)) }
function formatDate(value: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat('zh-TW', { dateStyle: 'short', timeStyle: 'medium', hour12: false }).format(date)
}
function areaLabel(areaNo: string): string {
  const item = areas.value.find(row => row.areaNo === areaNo) ?? seedAreas.find(row => row.areaNo === areaNo)
  return item ? `${item.areaNo} - ${item.areaName}` : areaNo
}

async function loadRows(): Promise<void> {
  loading.value = true; errorMessage.value = ''; successMessage.value = ''
  try {
    if (mockMode.value) {
      rows.value = sortRows(readRows().filter(row => row.computerNo.toLocaleLowerCase().includes(filters.value.computerNo.trim().toLocaleLowerCase()) && row.factoryNo.toLocaleLowerCase().includes(filters.value.factoryNo.trim().toLocaleLowerCase())))
      if (!window.localStorage.getItem(rowsKey)) saveRows(readRows())
    } else {
      const query = new URLSearchParams()
      if (filters.value.computerNo.trim()) query.set('computerNo', filters.value.computerNo.trim())
      if (filters.value.factoryNo.trim()) query.set('factoryNo', filters.value.factoryNo.trim())
      rows.value = sortRows(await requestJson<ComputerAreaRow[]>(`api/smd/computer-areas?${query.toString()}`))
    }
    const available = new Set(rows.value.map(rowKey))
    selectedKeys.value = selectedKeys.value.filter(key => available.has(key))
    if (!available.has(activeKey.value)) { activeKey.value = ''; anchorKey.value = '' }
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '過帳電腦之區域資料載入失敗。' }
  finally { loading.value = false }
}

async function loadFactories(): Promise<void> {
  try {
    if (mockMode.value) factories.value = structuredClone(seedFactories)
    else {
      const options = await requestJson<{ factories: FactoryOption[] }>('api/smd/area-basis/options')
      factories.value = options.factories
    }
  } catch (error) { formError.value = error instanceof Error ? error.message : '已簽核工廠清單載入失敗。' }
}
async function loadApprovedAreas(factoryNo: string): Promise<void> {
  areas.value = []
  if (!factoryNo) { selectedAreaNos.value = []; return }
  try {
    if (mockMode.value) areas.value = structuredClone(seedAreas.filter(item => item.factoryNo === factoryNo && item.issueState === 2 && item.areaClass === 0))
    else {
      const query = new URLSearchParams({ factoryNo, issueState: '2', areaClass: '0' })
      areas.value = await requestJson<AreaOption[]>(`api/smd/areas?${query.toString()}`)
    }
    const available = new Set(areas.value.map(item => item.areaNo))
    selectedAreaNos.value = selectedAreaNos.value.filter(areaNo => available.has(areaNo))
  } catch (error) { selectedAreaNos.value = []; formError.value = error instanceof Error ? error.message : '已簽核區域清單載入失敗。' }
}

function selectRow(row: ComputerAreaRow, event: MouseEvent): void {
  const key = rowKey(row); const index = rows.value.findIndex(item => rowKey(item) === key)
  if (event.shiftKey && anchorKey.value) {
    const anchor = rows.value.findIndex(item => rowKey(item) === anchorKey.value)
    selectedKeys.value = anchor < 0 ? [key] : rows.value.slice(Math.min(anchor, index), Math.max(anchor, index) + 1).map(rowKey)
  } else if (event.ctrlKey || event.metaKey) {
    selectedKeys.value = selectedKeys.value.includes(key) ? selectedKeys.value.filter(item => item !== key) : [...selectedKeys.value, key]
    anchorKey.value = key
  } else { selectedKeys.value = [key]; anchorKey.value = key }
  activeKey.value = key
}
function toggleSelected(row: ComputerAreaRow): void {
  const key = rowKey(row)
  selectedKeys.value = selectedKeys.value.includes(key) ? selectedKeys.value.filter(item => item !== key) : [...selectedKeys.value, key]
  activeKey.value = key; anchorKey.value = key
}

async function openForm(mode: Exclude<FormMode, null>): Promise<void> {
  const source = activeRow.value
  if (mode !== 'add' && !source) { errorMessage.value = '請先選取一筆資料。'; return }
  formError.value = ''; errorMessage.value = ''; successMessage.value = ''; formMode.value = mode
  selectedAreaNos.value = mode === 'add' ? [] : [...(source?.areas ?? [])]
  form.value = mode === 'add' ? { computerNo: '', factoryNo: '' } : { computerNo: source?.computerNo ?? '', factoryNo: source?.factoryNo ?? '' }
  dataStamp.value = mode === 'edit' ? source?.dataStamp ?? 0 : null
  const now = new Date().toISOString()
  audit.value = mode === 'edit' && source
    ? { creator: source.creator ?? '', createDate: formatDate(source.createDate), reviser: source.reviser ?? '', reviseDate: formatDate(source.reviseDate) }
    : { creator: props.currentUserNo || 'DEV001', createDate: formatDate(now), reviser: '', reviseDate: '' }
  await loadFactories()
  await loadApprovedAreas(form.value.factoryNo)
  if (mode === 'add') factoryPickerOpen.value = true
}
function chooseFactory(factory: FactoryOption): void {
  form.value.factoryNo = factory.factoryNo; selectedAreaNos.value = []; factoryPickerOpen.value = false; factorySearch.value = ''; formError.value = ''
  void loadApprovedAreas(factory.factoryNo)
}
function toggleArea(areaNo: string): void {
  if (formMode.value === 'copy') return
  selectedAreaNos.value = selectedAreaNos.value.includes(areaNo) ? selectedAreaNos.value.filter(value => value !== areaNo) : [...selectedAreaNos.value, areaNo]
  formError.value = ''
}
function validateForm(): string {
  if (!form.value.computerNo.trim()) return 'ComputerNo 為必填。'
  if (form.value.computerNo.trim().length > 20) return 'ComputerNo 最多 20 個字元。'
  if (!form.value.factoryNo) return 'FactoryNo 為必填，請選擇已簽核工廠。'
  if (form.value.factoryNo.length > 20) return 'FactoryNo 最多 20 個字元。'
  if (selectedAreaNos.value.length === 0) return '至少要選取一個 AreaNo。'
  if (selectedAreaNos.value.some(value => value.length > 20)) return 'AreaNo 最多 20 個字元。'
  const currentKey = activeRow.value ? rowKey(activeRow.value) : ''
  if (formMode.value !== 'edit' && rows.value.some(row => rowKey(row) === rowKey(form.value))) return '相同 ComputerNo 與 FactoryNo 已存在。'
  if (formMode.value === 'edit' && rowKey(form.value) !== currentKey) return '修改時不可變更 ComputerNo 或 FactoryNo。'
  if (areas.value.length && selectedAreaNos.value.some(value => !areas.value.some(item => item.areaNo === value))) return 'AreaNo 必須屬於目前工廠下已簽核的非 Base Area。'
  return ''
}

async function saveForm(closeAfterSave: boolean): Promise<void> {
  if (!formMode.value || busy.value) return
  const validation = validateForm(); if (validation) { formError.value = validation; return }
  busy.value = true; formError.value = ''
  const mode = formMode.value; const now = new Date().toISOString()
  const body = { computerNo: form.value.computerNo.trim().toUpperCase(), factoryNo: form.value.factoryNo, areas: [...selectedAreaNos.value] }
  try {
    let saved: ComputerAreaRow
    if (mockMode.value) {
      const allRows = readRows(); const source = activeRow.value
      if (mode === 'add' && allRows.some(row => rowKey(row) === rowKey(body))) throw new Error('相同 ComputerNo 與 FactoryNo 已存在。')
      if (mode === 'copy') {
        if (!source) throw new Error('找不到要複製的來源資料。')
        if (allRows.some(row => rowKey(row) === rowKey(body))) throw new Error('相同 ComputerNo 與 FactoryNo 已存在。')
        saved = { ...body, areas: [...source.areas], creator: props.currentUserNo || 'DEV001', createDate: now, reviser: null, reviseDate: null, dataStamp: 0 }
      } else if (mode === 'edit') {
        const old = allRows.find(row => rowKey(row) === rowKey(body))
        if (!old) throw new Error('資料已不存在，請重新查詢。')
        if ((old.dataStamp ?? 0) !== (dataStamp.value ?? 0)) throw new Error('資料已被其他人修改，請重新查詢後再操作。')
        saved = { ...old, areas: body.areas, reviser: props.currentUserNo || 'DEV001', reviseDate: now, dataStamp: (old.dataStamp ?? 0) + 1 }
      } else saved = { ...body, creator: props.currentUserNo || 'DEV001', createDate: now, reviser: null, reviseDate: null, dataStamp: 0 }
      const nextRows = mode === 'edit' ? allRows.map(row => rowKey(row) === rowKey(saved) ? saved : row) : [...allRows, saved]
      saveRows(nextRows); rows.value = sortRows(nextRows.filter(row => row.computerNo.toLocaleLowerCase().includes(filters.value.computerNo.trim().toLocaleLowerCase()) && row.factoryNo.toLocaleLowerCase().includes(filters.value.factoryNo.trim().toLocaleLowerCase())))
    } else if (mode === 'edit') {
      const key = `${encodeURIComponent(body.computerNo)}/${encodeURIComponent(body.factoryNo)}`
      saved = await requestJson<ComputerAreaRow>(`api/smd/computer-areas/${key}?dataStamp=${encodeURIComponent(String(dataStamp.value ?? 0))}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ areas: body.areas }) })
      rows.value = sortRows(rows.value.map(row => rowKey(row) === rowKey(saved) ? saved : row))
    } else if (mode === 'copy') {
      const source = activeRow.value
      if (!source) throw new Error('找不到要複製的來源資料。')
      saved = await requestJson<ComputerAreaRow>('api/smd/computer-areas/copy', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ fromComputerNo: source.computerNo, fromFactoryNo: source.factoryNo, toComputerNo: body.computerNo }) })
      rows.value = sortRows([...rows.value, saved])
    } else {
      saved = await requestJson<ComputerAreaRow>('api/smd/computer-areas', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
      rows.value = sortRows([...rows.value, saved])
    }
    activeKey.value = rowKey(saved); selectedKeys.value = [activeKey.value]; successMessage.value = '過帳電腦之區域資料已儲存。'
    audit.value = { creator: saved.creator ?? audit.value.creator, createDate: formatDate(saved.createDate ?? now), reviser: saved.reviser ?? (mode === 'edit' ? props.currentUserNo || 'DEV001' : ''), reviseDate: formatDate(saved.reviseDate ?? (mode === 'edit' ? now : null)) }
    dataStamp.value = saved.dataStamp ?? (dataStamp.value ?? 0) + (mode === 'edit' ? 1 : 0)
    if (closeAfterSave) formMode.value = null
  } catch (error) { formError.value = error instanceof Error ? error.message : '過帳電腦之區域資料儲存失敗。' }
  finally { busy.value = false }
}

async function deleteSelected(): Promise<void> {
  const records = selectedKeys.value.map(key => rows.value.find(row => rowKey(row) === key)).filter((row): row is ComputerAreaRow => Boolean(row))
  if (!records.length || busy.value) return
  if (!window.confirm(`確定刪除選取的 ${records.length} 筆過帳電腦之區域資料？`)) return
  busy.value = true; errorMessage.value = ''; let completed = 0
  try {
    for (const row of records) {
      if (mockMode.value) {
        const next = readRows().filter(item => rowKey(item) !== rowKey(row)); saveRows(next); rows.value = sortRows(next.filter(item => item.computerNo.toLocaleLowerCase().includes(filters.value.computerNo.trim().toLocaleLowerCase()) && item.factoryNo.toLocaleLowerCase().includes(filters.value.factoryNo.trim().toLocaleLowerCase())))
      } else {
        const key = `${encodeURIComponent(row.computerNo)}/${encodeURIComponent(row.factoryNo)}`
        await requestJson<void>(`api/smd/computer-areas/${key}`, { method: 'DELETE' })
        rows.value = rows.value.filter(item => rowKey(item) !== rowKey(row))
      }
      completed += 1
    }
    selectedKeys.value = []; activeKey.value = ''; successMessage.value = `已刪除 ${completed} 筆資料。`
  } catch (error) { errorMessage.value = `${error instanceof Error ? error.message : '刪除失敗。'}（已完成 ${completed}/${records.length} 筆）`; if (completed) await loadRows() }
  finally { busy.value = false }
}

onMounted(() => { void loadRows() })
</script>

<template>
  <section class="computer-area-master">
    <header class="page-heading"><h1>過帳電腦之區域</h1></header>
    <form class="query-strip" @submit.prevent="loadRows">
      <label for="computer-area-query-computer">ComputerNo</label><input id="computer-area-query-computer" v-model.trim="filters.computerNo" maxlength="20" /> <span class="wildcard">%</span>
      <label for="computer-area-query-factory">FactoryNo</label><input id="computer-area-query-factory" v-model.trim="filters.factoryNo" maxlength="20" /> <span class="wildcard">%</span>
      <button class="btn" type="submit" :disabled="loading">查詢</button><span v-if="loading" class="muted">載入中…</span>
    </form>
    <div v-if="errorMessage" class="alert alert-danger" role="alert">{{ errorMessage }}</div>
    <div v-if="successMessage" class="alert alert-success" role="status">{{ successMessage }}</div>
    <div class="work-area">
      <div class="grid-panel">
        <div class="toolbar">
          <button class="btn btn-primary" type="button" :disabled="busy" @click="openForm('add')">新增(A)</button>
          <button class="btn" type="button" :disabled="!activeRow || busy" @click="openForm('edit')">修改(E)</button>
          <button class="btn" type="button" :disabled="!activeRow || busy" @click="openForm('copy')">複製(C)</button>
          <button class="btn" type="button" :disabled="selectedKeys.length === 0 || busy" @click="deleteSelected">刪除(D)</button>
        </div>
        <div class="table-wrap">
          <table class="data-grid">
            <thead><tr><th class="select-col"></th><th>ComputerNo</th><th>FactoryNo</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
            <tbody>
              <tr v-for="row in rows" :key="rowKey(row)" :class="{ active: activeKey === rowKey(row), selected: selectedKeys.includes(rowKey(row)) }" @click="selectRow(row, $event)">
                <td class="select-col"><input type="checkbox" :checked="selectedKeys.includes(rowKey(row))" :aria-label="`選取 ${row.computerNo}`" @click.stop="toggleSelected(row)" /></td>
                <td>{{ row.computerNo }}</td><td>{{ row.factoryNo }}</td><td>{{ row.creator ?? '' }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser ?? '' }}</td><td>{{ formatDate(row.reviseDate) }}</td>
              </tr>
              <tr v-if="!rows.length"><td colspan="7" class="empty-state">{{ loading ? '載入中…' : '沒有符合條件的資料' }}</td></tr>
            </tbody>
          </table>
        </div>
        <div class="record-count">資料總筆數 {{ rows.length }}</div>
      </div>
      <aside class="side-actions"><button class="btn close-button" type="button" @click="emit('close')">關閉</button></aside>
    </div>

    <div v-if="formMode" class="modal-backdrop" @click.self="formMode = null">
      <section class="definition-modal" role="dialog" aria-modal="true" aria-labelledby="computer-area-form-title">
        <header class="modal-heading"><h2 id="computer-area-form-title">過帳電腦之區域 — {{ formMode === 'add' ? '新增' : formMode === 'edit' ? '修改' : '複製' }}</h2><button class="icon-close" type="button" aria-label="關閉" @click="formMode = null">×</button></header>
        <div class="definition-body">
          <div class="basis-box">
            <div class="field-row"><label for="computer-area-no">ComputerNo</label><input id="computer-area-no" v-model.trim="form.computerNo" maxlength="20" :readonly="formMode === 'edit'" class="kd-input" @blur="form.computerNo = form.computerNo.toUpperCase()" /></div>
            <div class="field-row"><label for="computer-area-factory">FactoryNo</label><input id="computer-area-factory" :value="form.factoryNo" readonly class="kd-input readonly" /><button class="btn lookup-btn" type="button" :disabled="formMode !== 'add'" aria-label="選擇工廠" @click="factoryPickerOpen = true; factorySearch = ''">…</button></div>
            <div class="area-list-heading"><strong>AreaNo</strong><span>勾選此電腦可過帳的已簽核區域</span></div>
            <div class="area-list-wrap">
              <table class="data-grid area-grid"><thead><tr><th class="check-col">選取</th><th>AreaNo</th><th>AreaName</th><th>Description</th></tr></thead>
                <tbody><tr v-for="area in areas" :key="area.areaNo" :class="{ selected: selectedAreaNos.includes(area.areaNo), 'read-only-row': formMode === 'copy' }" @click="toggleArea(area.areaNo)"><td class="check-col"><input type="checkbox" :checked="selectedAreaNos.includes(area.areaNo)" :disabled="formMode === 'copy'" :aria-label="`選取 ${area.areaNo}`" @click.stop="toggleArea(area.areaNo)" /></td><td>{{ area.areaNo }}</td><td>{{ area.areaName }}</td><td>{{ area.description ?? '' }}</td></tr>
                  <tr v-if="!areas.length"><td colspan="4" class="empty-state">{{ form.factoryNo ? '此工廠沒有可選的已簽核區域' : '請先選擇工廠' }}</td></tr></tbody>
              </table>
            </div>
            <div v-if="selectedAreaNos.length" class="selected-summary">已選 {{ selectedAreaNos.length }} 個區域：{{ selectedAreaNos.map(areaLabel).join('、') }}</div>
          </div>
          <div v-if="formError" class="alert alert-danger form-alert" role="alert">{{ formError }}</div>
          <div class="audit-fields">
            <label>建立人<input :value="audit.creator" readonly /></label><label>建立日期<input :value="audit.createDate" readonly /></label>
            <label>修改人<input :value="audit.reviser" readonly /></label><label>修改日期<input :value="audit.reviseDate" readonly /></label>
          </div>
        </div>
        <footer class="modal-actions"><button class="btn btn-primary" type="button" :disabled="busy" @click="saveForm(true)">確定</button><button class="btn" type="button" :disabled="busy" @click="formMode = null">關閉</button><button class="btn" type="button" :disabled="busy" @click="saveForm(false)">套用</button></footer>
      </section>
    </div>

    <div v-if="factoryPickerOpen" class="modal-backdrop picker-layer" @click.self="factoryPickerOpen = false">
      <section class="picker-modal" role="dialog" aria-modal="true" aria-labelledby="factory-picker-title">
        <header class="modal-heading"><h2 id="factory-picker-title">選擇已簽核工廠</h2><button class="icon-close" type="button" aria-label="關閉" @click="factoryPickerOpen = false">×</button></header>
        <div class="picker-search"><input v-model.trim="factorySearch" autofocus placeholder="搜尋 FactoryNo / 名稱" /><button class="btn" type="button" @click="loadFactories">重新載入</button></div>
        <div class="picker-list"><button v-for="item in visibleFactories" :key="item.factoryNo" class="picker-row" type="button" @click="chooseFactory(item)"><strong>{{ item.factoryNo }}</strong><span>{{ item.description }}</span></button><p v-if="!visibleFactories.length" class="empty-state">沒有符合條件的已簽核工廠</p></div>
      </section>
    </div>
  </section>
</template>

<style scoped>
.computer-area-master{height:100%;min-height:0;display:flex;flex-direction:column;color:#292929;background:#fff}.page-heading{padding:4px 18px 12px;border-bottom:1px solid #ddd}.page-heading h1{margin:0;font-size:20px;font-weight:600}.query-strip{display:flex;align-items:center;gap:8px;flex-wrap:wrap;padding:9px 12px;border-bottom:1px solid #ddd}.query-strip label{white-space:nowrap}.query-strip input{width:180px;height:28px;border:1px solid #999;border-radius:0;padding:3px 6px}.wildcard{margin-left:-5px;color:#555}.btn{min-height:30px;padding:4px 12px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#f4f4f4);color:#222;white-space:nowrap}.btn:hover:not(:disabled){background:#fff0ad}.btn:disabled{opacity:.55}.btn-primary{background:#ff9d21;color:white;border-color:#ed901e}.work-area{display:grid;grid-template-columns:minmax(0,1fr) 90px;gap:10px;min-height:0;flex:1;padding:8px 10px 12px}.grid-panel{display:flex;flex-direction:column;min-width:0;border:1px solid #ccc;padding:6px}.toolbar{display:flex;gap:5px;align-items:center;padding-bottom:6px}.table-wrap,.area-list-wrap{min-height:0;overflow:auto;border:1px solid #c7c7c7;flex:1}.data-grid{width:100%;border-collapse:collapse;table-layout:auto;font-size:13px;white-space:nowrap}.data-grid th{position:sticky;top:0;z-index:1;background:#43a5e8;color:#fff;font-weight:500;text-align:left}.data-grid th,.data-grid td{height:28px;padding:3px 8px;border:1px solid #ccc}.data-grid tbody tr{cursor:pointer}.area-grid .read-only-row{cursor:default}.area-grid .read-only-row:hover{background:inherit}.data-grid tbody tr:nth-child(even){background:#fafafa}.data-grid tbody tr:hover,.data-grid tbody tr.selected{background:#fff0ad}.data-grid tbody tr.active{outline:1px solid #e9a52d;outline-offset:-2px}.select-col,.check-col{width:48px;text-align:center}.select-col input,.check-col input{accent-color:#e9a52d}.empty-state{padding:18px!important;text-align:center;color:#777;cursor:default}.record-count{padding:6px 2px 0;color:#666;font-size:12px}.side-actions{display:flex;align-items:flex-end;justify-content:flex-end}.close-button{width:100%}.muted{color:#777;font-size:12px}.alert{padding:8px 12px;margin:6px 10px;border:1px solid;border-radius:3px}.alert-danger{color:#87313b;background:#fae4e7;border-color:#edc6cb}.alert-success{color:#386044;background:#e7f3e9;border-color:#c9e5ce}.modal-backdrop{position:fixed;inset:0;z-index:40;display:grid;place-items:center;padding:16px;background:#0005}.definition-modal,.picker-modal{width:min(680px,96vw);max-height:min(760px,92vh);display:flex;flex-direction:column;background:#fff;border:1px solid #aaa;box-shadow:0 5px 24px #0004}.modal-heading{display:flex;align-items:center;justify-content:space-between;padding:10px 14px;border-bottom:1px solid #ccc}.modal-heading h2{margin:0;font-size:17px;font-weight:600}.icon-close{border:0;background:transparent;font-size:24px;line-height:1;color:#555}.definition-body{min-height:0;overflow:auto;padding:12px 14px}.basis-box{border:1px solid #bbb;padding:10px 12px}.field-row{display:grid;grid-template-columns:115px minmax(160px,1fr) 32px;align-items:center;gap:8px;min-height:38px}.field-row>label{text-align:right}.kd-input{height:28px;width:100%;border:1px solid #999;border-radius:0;background:#fff0ad;padding:3px 6px;color:#222}.kd-input:read-only{cursor:default}.kd-input.readonly{background:#eee}.lookup-btn{width:30px;padding:2px}.area-list-heading{display:flex;gap:12px;align-items:baseline;margin:10px 0 5px 115px}.area-list-heading span{color:#666;font-size:12px}.area-list-wrap{height:185px;max-height:35vh}.area-grid{font-size:12px}.area-grid th,.area-grid td{height:25px;padding:2px 6px}.selected-summary{padding:6px 0 0 115px;font-size:12px;color:#555;white-space:normal}.audit-fields{display:grid;grid-template-columns:1fr 1fr;gap:7px 12px;margin:10px 0 0;padding:10px 12px;border:1px solid #bbb}.audit-fields label{display:flex;align-items:center;gap:7px;white-space:nowrap}.audit-fields input{min-width:0;width:100%;height:25px;border:1px solid #bbb;background:#eee;padding:3px 5px}.form-alert{margin:9px 0 0}.modal-actions{display:flex;justify-content:flex-end;gap:8px;padding:10px 14px;border-top:1px solid #ddd}.picker-layer{z-index:50}.picker-modal{width:min(560px,96vw)}.picker-search{display:flex;gap:10px;align-items:center;padding:10px 14px;border-bottom:1px solid #ddd}.picker-search input{flex:1;height:29px;border:1px solid #aaa;padding:3px 7px}.picker-list{max-height:55vh;overflow:auto;padding:5px}.picker-row{width:100%;display:grid;grid-template-columns:minmax(130px,1fr) 2fr;gap:10px;text-align:left;padding:8px 10px;border:0;border-bottom:1px solid #eee;background:#fff;color:#333}.picker-row:hover{background:#fff0ad}@media(max-width:760px){.query-strip{align-items:flex-start}.work-area{grid-template-columns:minmax(0,1fr) 64px}.audit-fields{grid-template-columns:1fr}.field-row{grid-template-columns:95px minmax(100px,1fr) 32px}.area-list-heading,.selected-summary{margin-left:95px;padding-left:0}}
</style>
