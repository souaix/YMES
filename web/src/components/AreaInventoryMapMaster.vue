<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

interface InventoryMapRow {
  areaNo: string
  inventoryNo: string
  baseAreaNo: string
  defaultInv: number
  creator: string | null
  createDate: string | null
  reviser: string | null
  reviseDate: string | null
  dataStamp: number | null
}
interface AreaOption { areaNo: string; areaName: string; issueState: number; areaClass: number }
interface RelationOption { objectNo: string; objectText: string; objectType: number; containAreaNo?: string; objectAreaType?: number }
interface InventoryForm { areaNo: string; inventoryNo: string; baseAreaNo: string; defaultInv: number }
type FormMode = 'add' | 'edit' | 'copy' | null
type PickerMode = 'base' | 'area' | 'inventory' | null

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mockMode = computed(() => import.meta.env.DEV && props.developmentPreview)
const rowsKey = 'ymes-dev-mock:smd-area-inventory:v1'
const baseAreas = ref<AreaOption[]>([])
const selectedBaseAreaNo = ref('')
const relationOptions = ref<RelationOption[]>([])
const rows = ref<InventoryMapRow[]>([])
const selectedKeys = ref<string[]>([])
const activeKey = ref('')
const anchorKey = ref('')
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const formError = ref('')
const formMode = ref<FormMode>(null)
const form = ref<InventoryForm>(blankForm())
const formDataStamp = ref<number | null>(null)
const audit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const pickerMode = ref<PickerMode>(null)
const pickerSearch = ref('')

const seedBaseAreas: AreaOption[] = [
  { areaNo: 'DEV-BASE', areaName: '開發用基底區域', issueState: 2, areaClass: 1 },
  { areaNo: 'DEV-BASE-2', areaName: '開發用第二基底區域', issueState: 2, areaClass: 1 },
]
const seedRelations: RelationOption[] = [
  { objectNo: 'DEV-OP-01', objectText: '開發用作業區', objectType: 0, containAreaNo: 'DEV-BASE', objectAreaType: 0 },
  { objectNo: 'DEV-LINE', objectText: '開發用生產線', objectType: 0, containAreaNo: 'DEV-BASE', objectAreaType: 1 },
  { objectNo: 'DEV-INV-01', objectText: '開發用庫房一', objectType: 1, containAreaNo: 'DEV-BASE' },
  { objectNo: 'DEV-INV-02', objectText: '開發用庫房二', objectType: 1, containAreaNo: 'DEV-BASE' },
]
const seedRows: InventoryMapRow[] = [
  { areaNo: 'DEV-OP-01', inventoryNo: 'DEV-INV-01', baseAreaNo: 'DEV-BASE', defaultInv: 1, creator: 'DEV001', createDate: '2026-11-10T08:00:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-11T09:00:00+08:00', dataStamp: 2 },
  { areaNo: 'DEV-OP-01', inventoryNo: 'DEV-INV-02', baseAreaNo: 'DEV-BASE', defaultInv: 0, creator: 'DEV001', createDate: '2026-11-10T08:05:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
  { areaNo: 'DEV-LINE', inventoryNo: 'DEV-INV-01', baseAreaNo: 'DEV-BASE', defaultInv: 0, creator: 'DEV001', createDate: '2026-11-12T08:00:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
]

function blankForm(): InventoryForm { return { areaNo: '', inventoryNo: '', baseAreaNo: '', defaultInv: 0 } }
function rowKey(row: Pick<InventoryMapRow, 'areaNo' | 'inventoryNo'>): string { return `${row.areaNo}\u0000${row.inventoryNo}` }
function objectOptions(type: 0 | 1): RelationOption[] {
  return relationOptions.value.filter(option => option.objectType === type && (!option.containAreaNo || option.containAreaNo === selectedBaseAreaNo.value))
}
const activeRow = computed(() => rows.value.find(row => rowKey(row) === activeKey.value) ?? null)
const allSelected = computed(() => rows.value.length > 0 && rows.value.every(row => selectedKeys.value.includes(rowKey(row))))
const visiblePickerRows = computed(() => {
  const query = pickerSearch.value.trim().toLocaleLowerCase()
  if (pickerMode.value === 'base') return baseAreas.value.filter(item => !query || `${item.areaNo} ${item.areaName}`.toLocaleLowerCase().includes(query))
  const options = objectOptions(pickerMode.value === 'area' ? 0 : 1)
  return options.filter(item => !query || `${item.objectNo} ${item.objectText}`.toLocaleLowerCase().includes(query))
})

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
    if (response.status === 404) throw new Error('區域庫房 API 尚未建立；請依 todolist_homepc.md 完成 SMD SD105 API。')
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 回應錯誤 (${response.status})。`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function readRows(): InventoryMapRow[] {
  try { const value = window.localStorage.getItem(rowsKey); return value ? JSON.parse(value) as InventoryMapRow[] : structuredClone(seedRows) }
  catch { return structuredClone(seedRows) }
}
function saveRows(value: InventoryMapRow[]): void { window.localStorage.setItem(rowsKey, JSON.stringify(value)) }
function formatDate(value: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat('zh-TW', { dateStyle: 'short', timeStyle: 'medium', hour12: false }).format(date)
}
function selectedBaseAreaName(areaNo: string): string {
  return baseAreas.value.find(item => item.areaNo === areaNo)?.areaName ?? ''
}

async function loadBaseAreas(): Promise<void> {
  try {
    if (mockMode.value) baseAreas.value = structuredClone(seedBaseAreas)
    else {
      const options = await requestJson<{ baseAreas: AreaOption[] }>('api/smd/area-basis/options')
      baseAreas.value = options.baseAreas.filter(row => row.issueState === 2 && row.areaClass === 1)
    }
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '基底區域清單載入失敗。' }
}

async function loadRows(): Promise<void> {
  errorMessage.value = ''; successMessage.value = ''
  if (!selectedBaseAreaNo.value) { rows.value = []; selectedKeys.value = []; activeKey.value = ''; relationOptions.value = []; return }
  rows.value = []; selectedKeys.value = []; activeKey.value = ''; relationOptions.value = []
  loading.value = true
  try {
    if (mockMode.value) {
      rows.value = readRows().filter(row => row.baseAreaNo === selectedBaseAreaNo.value)
      relationOptions.value = structuredClone(seedRelations).filter(item => !item.containAreaNo || item.containAreaNo === selectedBaseAreaNo.value)
      if (!window.localStorage.getItem(rowsKey)) saveRows(readRows())
    } else {
      const baseNo = encodeURIComponent(selectedBaseAreaNo.value)
      const [list, layout] = await Promise.all([
        requestJson<InventoryMapRow[]>(`api/smd/area-inventories?baseAreaNo=${baseNo}`),
        requestJson<{ relations: RelationOption[] }>(`api/smd/areas/${baseNo}/layout`),
      ])
      rows.value = list
      relationOptions.value = layout.relations.filter(item => item.objectType === 0 || item.objectType === 1)
    }
    const keys = new Set(rows.value.map(rowKey))
    selectedKeys.value = selectedKeys.value.filter(key => keys.has(key))
    if (!keys.has(activeKey.value)) { activeKey.value = rows.value[0] ? rowKey(rows.value[0]) : ''; selectedKeys.value = activeKey.value ? [activeKey.value] : [] }
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '區域庫房資料載入失敗。' }
  finally { loading.value = false }
}

function chooseBaseArea(area: AreaOption): void {
  selectedBaseAreaNo.value = area.areaNo
  pickerMode.value = null; pickerSearch.value = ''
  void loadRows()
}
function chooseObject(option: RelationOption): void {
  if (pickerMode.value === 'area') form.value.areaNo = option.objectNo
  if (pickerMode.value === 'inventory') form.value.inventoryNo = option.objectNo
  pickerMode.value = null; pickerSearch.value = ''; formError.value = ''
}
function selectRow(row: InventoryMapRow, event: MouseEvent): void {
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
function toggleAll(checked: boolean): void { selectedKeys.value = checked ? rows.value.map(rowKey) : [] }

async function openForm(next: Exclude<FormMode, null>): Promise<void> {
  const source = activeRow.value
  if (!selectedBaseAreaNo.value) { errorMessage.value = '請先選擇基底區域。'; return }
  if (next !== 'add' && !source) { errorMessage.value = '請先選取一筆區域庫房資料。'; return }
  formError.value = ''; errorMessage.value = ''; successMessage.value = ''; formMode.value = next
  if (next === 'add') { form.value = { ...blankForm(), baseAreaNo: selectedBaseAreaNo.value }; formDataStamp.value = null }
  else if (source) {
    form.value = { areaNo: source.areaNo, inventoryNo: source.inventoryNo, baseAreaNo: source.baseAreaNo, defaultInv: source.defaultInv }
    formDataStamp.value = next === 'edit' ? source.dataStamp ?? 0 : null
  }
  const now = new Date().toISOString()
  audit.value = next === 'edit' && source
    ? { creator: source.creator ?? '', createDate: formatDate(source.createDate), reviser: source.reviser ?? '', reviseDate: formatDate(source.reviseDate) }
    : { creator: props.currentUserNo || 'DEV001', createDate: formatDate(now), reviser: '', reviseDate: '' }
}

function validateForm(): string {
  if (!form.value.baseAreaNo) return 'Base Area No 為必填。'
  if (!form.value.areaNo) return 'Area No 為必填，請從基底區域配置選取。'
  if (!form.value.inventoryNo) return 'Inventory No 為必填，請從基底區域配置選取。'
  if (![0, 1].includes(form.value.defaultInv)) return 'Default 僅能為 No 或 Yes。'
  const duplicate = rows.value.some(row => row.areaNo === form.value.areaNo && row.inventoryNo === form.value.inventoryNo && !(formMode.value === 'edit' && rowKey(row) === activeKey.value))
  if (duplicate) return '此區域與庫房的對應已存在。'
  if (form.value.defaultInv === 1 && rows.value.some(row => row.areaNo === form.value.areaNo && row.defaultInv === 1 && !(formMode.value === 'edit' && rowKey(row) === activeKey.value))) return '同一區域已有預設庫房，請先取消原預設。'
  if (!objectOptions(0).some(option => option.objectNo === form.value.areaNo)) return 'Area No 必須屬於目前基底區域配置。'
  if (!objectOptions(1).some(option => option.objectNo === form.value.inventoryNo)) return 'Inventory No 必須屬於目前基底區域配置。'
  return ''
}

async function saveForm(closeAfterSave: boolean): Promise<void> {
  if (!formMode.value || busy.value) return
  const validation = validateForm(); if (validation) { formError.value = validation; return }
  busy.value = true; formError.value = ''
  const now = new Date().toISOString(); const wasEditing = formMode.value === 'edit'
  try {
    const body = { ...form.value, ...(wasEditing ? { dataStamp: formDataStamp.value } : {}) }
    let saved: InventoryMapRow
    if (mockMode.value) {
      const allRows = readRows(); const old = allRows.find(row => rowKey(row) === rowKey(body))
      if (!wasEditing && old) throw new Error('此區域與庫房的對應已存在。')
      saved = { ...body, creator: wasEditing ? old?.creator ?? null : props.currentUserNo || 'DEV001', createDate: wasEditing ? old?.createDate ?? null : now, reviser: wasEditing ? props.currentUserNo || 'DEV001' : null, reviseDate: wasEditing ? now : null, dataStamp: wasEditing ? (old?.dataStamp ?? 0) + 1 : 0 }
      const nextRows = wasEditing ? allRows.map(row => rowKey(row) === rowKey(saved) ? saved : row) : [...allRows, saved]
      saveRows(nextRows); rows.value = nextRows.filter(row => row.baseAreaNo === selectedBaseAreaNo.value)
    } else if (wasEditing) {
      saved = await requestJson<InventoryMapRow>(`api/smd/area-inventories/${encodeURIComponent(body.areaNo)}/${encodeURIComponent(body.inventoryNo)}?dataStamp=${encodeURIComponent(String(formDataStamp.value ?? 0))}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
      rows.value = rows.value.map(row => rowKey(row) === rowKey(saved) ? saved : row)
    } else {
      saved = await requestJson<InventoryMapRow>('api/smd/area-inventories', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
      rows.value = [...rows.value, saved]
    }
    activeKey.value = rowKey(saved); selectedKeys.value = [activeKey.value]; successMessage.value = '區域庫房資料已儲存。'
    audit.value = { creator: saved.creator ?? audit.value.creator, createDate: formatDate(saved.createDate ?? now), reviser: saved.reviser ?? (wasEditing ? props.currentUserNo || 'DEV001' : ''), reviseDate: formatDate(saved.reviseDate ?? (wasEditing ? now : null)) }
    if (closeAfterSave) formMode.value = null
    else if (!wasEditing) { formMode.value = 'edit'; formDataStamp.value = saved.dataStamp ?? 0 }
  } catch (error) { formError.value = error instanceof Error ? error.message : '區域庫房資料儲存失敗。' }
  finally { busy.value = false }
}

async function deleteSelected(): Promise<void> {
  const records = selectedKeys.value.map(key => rows.value.find(row => rowKey(row) === key)).filter((row): row is InventoryMapRow => Boolean(row))
  if (!records.length || busy.value) return
  if (!window.confirm(`確定刪除選取的 ${records.length} 筆區域庫房對應？`)) return
  busy.value = true; errorMessage.value = ''; let completed = 0
  try {
    for (const row of records) {
      if (mockMode.value) {
        const allRows = readRows().filter(item => rowKey(item) !== rowKey(row)); saveRows(allRows); rows.value = allRows.filter(item => item.baseAreaNo === selectedBaseAreaNo.value)
      } else {
        await requestJson<void>(`api/smd/area-inventories/${encodeURIComponent(row.areaNo)}/${encodeURIComponent(row.inventoryNo)}?baseAreaNo=${encodeURIComponent(row.baseAreaNo)}&dataStamp=${encodeURIComponent(String(row.dataStamp ?? 0))}`, { method: 'DELETE' })
        rows.value = rows.value.filter(item => rowKey(item) !== rowKey(row))
      }
      completed += 1
    }
    selectedKeys.value = []; activeKey.value = rows.value[0] ? rowKey(rows.value[0]) : ''; successMessage.value = `已刪除 ${completed} 筆區域庫房對應。`
  } catch (error) { errorMessage.value = `${error instanceof Error ? error.message : '區域庫房刪除失敗。'}（已完成 ${completed}/${records.length} 筆）`; if (completed) await loadRows() }
  finally { busy.value = false }
}

onMounted(() => { void loadBaseAreas() })
</script>

<template>
  <section class="area-inventory-master">
    <header class="page-heading"><h1>區域庫房基本設定</h1></header>
    <div class="query-strip"><label for="base-area">Base Area No.</label><input id="base-area" :value="selectedBaseAreaNo" readonly /><button class="btn lookup-btn" type="button" aria-label="選擇基底區域" @click="pickerMode = 'base'; pickerSearch = ''">…</button><button class="btn" type="button" :disabled="!selectedBaseAreaNo || loading" @click="loadRows">查詢</button><span class="base-area-name">{{ selectedBaseAreaName(selectedBaseAreaNo) }}</span><span v-if="loading" class="muted">載入中…</span></div>
    <div class="work-area">
      <div class="grid-panel"><div class="toolbar"><button class="btn btn-primary" type="button" :disabled="!selectedBaseAreaNo" @click="openForm('add')">新增(A)</button><button class="btn" type="button" :disabled="!activeRow" @click="openForm('edit')">修改(E)</button><button class="btn" type="button" :disabled="!activeRow" @click="openForm('copy')">複製(C)</button><button class="btn" type="button" :disabled="!selectedKeys.length || busy" @click="deleteSelected">刪除(D)</button></div>
        <div class="table-scroll"><table class="data-grid"><thead><tr><th class="row-selector"><input type="checkbox" :checked="allSelected" aria-label="全選" @change="toggleAll(($event.target as HTMLInputElement).checked)" /></th><th>Area No.</th><th>Inventory No.</th><th>Base Area No.</th><th>Default</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead><tbody>
          <tr v-for="row in rows" :key="rowKey(row)" :class="{ active: rowKey(row) === activeKey, selected: selectedKeys.includes(rowKey(row)) }" @click="selectRow(row, $event)" @dblclick="openForm('edit')"><td class="row-selector">{{ rowKey(row) === activeKey ? '▶' : '' }}</td><td>{{ row.areaNo }}</td><td>{{ row.inventoryNo }}</td><td>{{ row.baseAreaNo }}</td><td>{{ row.defaultInv === 1 ? 'Yes' : 'No' }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td></tr>
          <tr v-if="!loading && !rows.length"><td class="empty" colspan="9">{{ selectedBaseAreaNo ? '沒有區域庫房對應資料' : '請先選擇 Base Area No.' }}</td></tr>
        </tbody></table></div><footer class="grid-footer">資料總筆數 {{ rows.length }}</footer></div>
      <aside class="side-actions"><button class="btn close-btn" type="button" @click="emit('close')">關閉(X)</button></aside>
    </div>
    <p v-if="errorMessage" class="notice error" role="alert">{{ errorMessage }}</p><p v-if="successMessage" class="notice success" role="status">{{ successMessage }}</p><p v-if="mockMode" class="preview-note">Development 假資料預覽；資料只保存在此瀏覽器。</p>

    <div v-if="formMode" class="modal-backdrop" @click.self="formMode = null"><section class="editor-modal" role="dialog" aria-modal="true" aria-label="區域庫房定義"><header class="modal-heading"><h2>Area inventory define（{{ formMode === 'edit' ? '修改' : formMode === 'copy' ? '複製' : '新增' }}）</h2><button class="icon-close" type="button" aria-label="關閉" @click="formMode = null">×</button></header>
      <fieldset class="editor-fields"><legend>Area inventory define</legend><div class="field-row"><label>Base Area No.</label><input class="kd-input" :value="form.baseAreaNo" readonly /></div>
        <div class="field-row"><label for="map-area-no">Area No.</label><input id="map-area-no" class="kd-input" :value="form.areaNo" readonly /><button class="btn lookup-btn" type="button" :disabled="formMode === 'edit'" @click="pickerMode = 'area'; pickerSearch = ''">…</button></div>
        <div class="field-row"><label for="map-inventory-no">Inventory No.</label><input id="map-inventory-no" class="kd-input" :value="form.inventoryNo" readonly /><button class="btn lookup-btn" type="button" :disabled="formMode === 'edit'" @click="pickerMode = 'inventory'; pickerSearch = ''">…</button></div>
        <div class="field-row"><label>Default</label><div class="radio-options"><label><input v-model.number="form.defaultInv" type="radio" :value="0" /> No</label><label><input v-model.number="form.defaultInv" type="radio" :value="1" /> Yes</label></div><span /></div>
      </fieldset>
      <fieldset class="audit-fields"><legend>建立／修改資訊</legend><label>Creator<input :value="audit.creator" readonly /></label><label>CreateDate<input :value="audit.createDate" readonly /></label><label>Reviser<input :value="audit.reviser" readonly /></label><label>ReviseDate<input :value="audit.reviseDate" readonly /></label></fieldset>
      <p v-if="formError" class="notice error">{{ formError }}</p><footer class="modal-actions"><button class="btn btn-primary" :disabled="busy" type="button" @click="saveForm(true)">確定</button><button class="btn" type="button" @click="formMode = null">關閉</button><button class="btn" :disabled="busy" type="button" @click="saveForm(false)">套用</button></footer>
    </section></div>

    <div v-if="pickerMode" class="modal-backdrop picker-backdrop" @click.self="pickerMode = null"><section class="picker-modal" role="dialog" aria-modal="true" aria-label="資料選擇"><header class="modal-heading"><h2>{{ pickerMode === 'base' ? '選擇 Base Area' : pickerMode === 'area' ? '選擇 Area' : '選擇 Inventory' }}</h2><button class="icon-close" type="button" aria-label="關閉" @click="pickerMode = null">×</button></header><div class="picker-search"><label>查詢</label><input v-model="pickerSearch" autofocus /></div><div class="picker-list"><button v-for="item in visiblePickerRows" :key="pickerMode === 'base' ? (item as AreaOption).areaNo : (item as RelationOption).objectNo" type="button" class="picker-row" @click="pickerMode === 'base' ? chooseBaseArea(item as AreaOption) : chooseObject(item as RelationOption)"><span>{{ pickerMode === 'base' ? (item as AreaOption).areaNo : (item as RelationOption).objectNo }}</span><span>{{ pickerMode === 'base' ? (item as AreaOption).areaName : (item as RelationOption).objectText }}</span></button><p v-if="!visiblePickerRows.length" class="empty">目前沒有可選項目</p></div><footer class="modal-actions"><button class="btn" type="button" @click="pickerMode = null">關閉</button></footer></section></div>
  </section>
</template>

<style scoped>
.area-inventory-master{height:100%;min-height:0;display:flex;flex-direction:column;background:#fff;color:#333;font-size:14px}.page-heading{padding:8px 14px;border-bottom:1px solid #d5d5d5;background:#f5f3ed}.page-heading h1{margin:0;font-size:20px;font-weight:500}.query-strip{min-height:48px;display:flex;align-items:center;gap:8px;padding:6px 14px;border-bottom:1px solid #d4d4d4}.query-strip>label{font-weight:500}.query-strip>input{height:29px;width:180px;border:1px solid #aaa;padding:3px 7px;background:#fff0ad}.lookup-btn{min-width:30px;padding:3px 8px}.base-area-name{color:#666}.muted,.preview-note{color:#777}.work-area{display:flex;flex:1;min-height:0;gap:10px;padding:7px}.grid-panel{display:flex;flex:1;min-width:0;min-height:0;flex-direction:column;border:1px solid #c7c7c7}.toolbar{display:flex;flex-wrap:wrap;gap:5px;padding:6px;border-bottom:1px solid #ccc}.btn{min-height:30px;padding:4px 12px;border:1px solid #b8b8b8;border-radius:3px;background:linear-gradient(#fff,#f1f1f1);color:#333;white-space:nowrap}.btn:hover:not(:disabled){border-color:#888;background:#eee}.btn:disabled{opacity:.52}.btn-primary{border-color:#ed9c21;background:#ffa928;color:#fff}.table-scroll{flex:1;min-height:0;overflow:auto}.data-grid{width:100%;border-collapse:collapse;white-space:nowrap}.data-grid th{position:sticky;top:0;z-index:1;background:#48a3df;color:#fff;font-weight:500;text-align:left}.data-grid th,.data-grid td{height:29px;padding:4px 7px;border:1px solid #d2d2d2}.data-grid tbody tr{cursor:pointer}.data-grid tbody tr:hover{background:#fff4ca}.data-grid tbody tr.active{background:#ffe699}.data-grid tbody tr.selected:not(.active){background:#fff1c3}.row-selector{width:28px;text-align:center}.row-selector input{accent-color:#f1a021}.empty{text-align:center;color:#777;padding:18px!important}.grid-footer{min-height:25px;padding:4px 8px;color:#666;border-top:1px solid #ccc}.side-actions{width:88px;display:flex;flex-direction:column;align-items:stretch;padding-top:42px}.close-btn{margin-top:auto}.notice{margin:4px 12px;padding:7px 10px;border-radius:3px}.error{color:#9b1c1c;background:#fff1f0;border:1px solid #e6b3ae}.success{color:#32633a;background:#f0f8ef;border:1px solid #b9d6b7}.preview-note{margin:2px 14px 7px;font-size:12px}.modal-backdrop{position:fixed;inset:0;z-index:40;display:flex;align-items:center;justify-content:center;padding:20px;background:rgba(0,0,0,.28)}.editor-modal,.picker-modal{width:min(560px,96vw);max-height:94vh;overflow:auto;border:1px solid #aaa;border-radius:4px;background:#fff;box-shadow:0 5px 24px #0004}.modal-heading{display:flex;align-items:center;justify-content:space-between;padding:10px 14px;border-bottom:1px solid #ccc;background:#f5f3ed}.modal-heading h2{margin:0;font-size:17px;font-weight:500}.icon-close{border:0;background:transparent;font-size:25px;line-height:1;color:#555}.editor-fields{margin:12px 14px 7px;padding:10px 12px 12px;border:1px solid #bbb}.editor-fields legend,.audit-fields legend{padding:0 5px;color:#555}.field-row{display:grid;grid-template-columns:110px minmax(120px,1fr) 32px;align-items:center;gap:8px;min-height:38px}.field-row>label{text-align:right}.kd-input{height:28px;width:100%;border:1px solid #999;border-radius:0;background:#fff0ad;padding:3px 6px;color:#222}.field-row .kd-input:read-only{cursor:default}.radio-options{display:flex;gap:24px;align-items:center;height:30px}.radio-options input{accent-color:#f1a021}.audit-fields{display:grid;grid-template-columns:1fr 1fr;gap:7px 12px;margin:8px 14px;padding:10px 12px;border:1px solid #bbb}.audit-fields label{display:flex;align-items:center;gap:7px;white-space:nowrap}.audit-fields input{min-width:0;width:100%;height:25px;border:1px solid #bbb;background:#eee;padding:3px 5px}.modal-actions{display:flex;justify-content:flex-end;gap:8px;padding:10px 14px}.picker-backdrop{z-index:50}.picker-modal{width:min(620px,96vw)}.picker-search{display:flex;gap:10px;align-items:center;padding:10px 14px;border-bottom:1px solid #ddd}.picker-search input{flex:1;height:29px;border:1px solid #aaa;padding:3px 7px}.picker-list{max-height:55vh;overflow:auto;padding:5px}.picker-row{width:100%;display:grid;grid-template-columns:minmax(150px,1fr) 2fr;gap:10px;text-align:left;padding:8px 10px;border:0;border-bottom:1px solid #eee;background:#fff;color:#333}.picker-row:hover{background:#fff0ad}@media(max-width:700px){.query-strip{flex-wrap:wrap}.audit-fields{grid-template-columns:1fr}.side-actions{width:70px}}
</style>
