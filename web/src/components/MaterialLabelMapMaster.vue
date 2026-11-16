<script setup lang="ts">
import { computed, ref } from 'vue'

type EditorMode = 'add' | 'edit' | 'copy' | null
type PickerKind = 'materialType' | 'material' | 'label'
interface LabelMap {
  materialType: string; materialNo: string; labelSource: number; labelType: string; labelNo: string
  materialTypeName?: string | null; materialName?: string | null; creator: string | null; createDate: string | null
  reviser: string | null; reviseDate: string | null; dataStamp: number | null
}
interface MaterialType { materialType: string; materialTypeName?: string | null }
interface Material { materialNo: string; materialType: string; materialName?: string | null; issueState: number }
interface LabelTemplate { labelNo: string; labelType: string; labelSource: number; formatFileName?: string; description?: string | null; printerNo?: string | null }
interface EditorValue { materialType: string; materialNo: string; labelType: string; labelNo: string }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const mapApi = 'api/material-label-maps'
const typeApi = 'api/material-types'
const materialApi = 'api/materials'
const labelApi = 'api/product-labels'
const storageKey = 'ymes-dev-mock:mtl-label-map:v1'

const seedTypes: MaterialType[] = [
  { materialType: 'CHEM', materialTypeName: '化學品（DEV）' },
  { materialType: 'PACK', materialTypeName: '包裝材料（DEV）' },
  { materialType: 'WIP', materialTypeName: '在製品（DEV）' },
]
const seedMaterials: Material[] = [
  { materialNo: 'DEV-CHEM-001', materialType: 'CHEM', materialName: '清洗液（DEV）', issueState: 2 },
  { materialNo: 'DEV-CHEM-002', materialType: 'CHEM', materialName: '稀釋劑（DEV）', issueState: 2 },
  { materialNo: 'DEV-PACK-001', materialType: 'PACK', materialName: '外箱（DEV）', issueState: 2 },
  { materialNo: 'DEV-WIP-001', materialType: 'WIP', materialName: '半成品（DEV）', issueState: 2 },
]
const seedLabels: LabelTemplate[] = [
  { labelNo: 'DEV-CHEM-LOT', labelType: 'LOT', labelSource: 5, formatFileName: 'chem-lot.btw', description: '物料批號標籤（DEV）', printerNo: 'DEV-PRN-01' },
  { labelNo: 'DEV-PACK-BOX', labelType: 'BOX', labelSource: 5, formatFileName: 'pack-box.btw', description: '包裝箱標籤（DEV）', printerNo: 'DEV-PRN-02' },
  { labelNo: 'DEV-OTHER-SOURCE', labelType: 'LOT', labelSource: 1, formatFileName: 'other.btw', description: '非物料來源，不應出現在選擇器' },
]
const seedMaps: LabelMap[] = [
  { materialType: 'CHEM', materialNo: 'DEV-CHEM-001', labelSource: 5, labelType: 'LOT', labelNo: 'DEV-CHEM-LOT', materialTypeName: '化學品（DEV）', materialName: '清洗液（DEV）', creator: 'DEV001', createDate: '2026-11-01T08:00:00+08:00', reviser: 'DEV001', reviseDate: '2026-11-02T09:00:00+08:00', dataStamp: 1 },
  { materialType: 'PACK', materialNo: 'DEV-PACK-001', labelSource: 5, labelType: 'BOX', labelNo: 'DEV-PACK-BOX', materialTypeName: '包裝材料（DEV）', materialName: '外箱（DEV）', creator: 'DEV001', createDate: '2026-11-03T08:00:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
]

const rows = ref<LabelMap[]>([])
const types = ref<MaterialType[]>([])
const materials = ref<Material[]>([])
const labels = ref<LabelTemplate[]>([])
const filters = ref({ materialType: '', materialNo: '', labelNo: '' })
const filterEnabled = ref({ materialType: false, materialNo: false, labelNo: false })
const activeKey = ref('')
const selectedKeys = ref<string[]>([])
const hasQueried = ref(false)
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const notice = ref('')

const editorMode = ref<EditorMode>(null)
const sourceRow = ref<LabelMap | null>(null)
const editor = ref<EditorValue>({ materialType: '', materialNo: '', labelType: '', labelNo: '' })
const audit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const editorError = ref('')
const editorSnapshot = ref('')
const pickerKind = ref<PickerKind | null>(null)
const pickerRows = ref<Array<MaterialType | Material | LabelTemplate>>([])
const pickerQuery = ref('')
const pickerLoading = ref(false)
const pickerContext = ref<'editor' | 'search'>('editor')

const visibleRows = computed(() => rows.value.slice().sort((a, b) => a.materialType.localeCompare(b.materialType) || a.materialNo.localeCompare(b.materialNo) || a.labelType.localeCompare(b.labelType)))
const activeRow = computed(() => rows.value.find(row => rowKey(row) === activeKey.value) ?? null)
const editorTitle = computed(() => `物料標籤對應設定（${editorMode.value === 'add' ? '新增' : editorMode.value === 'copy' ? '複製' : '修改'}）`)
const hasChanges = computed(() => Boolean(editorMode.value) && JSON.stringify(editor.value) !== editorSnapshot.value)
const filteredPickerRows = computed(() => {
  const query = pickerQuery.value.trim().toLocaleLowerCase()
  if (!query) return pickerRows.value
  return pickerRows.value.filter(row => Object.values(row).some(value => String(value ?? '').toLocaleLowerCase().includes(query)))
})

function rowKey(row: Pick<LabelMap, 'materialType' | 'materialNo' | 'labelType'> & Partial<Pick<LabelMap, 'labelSource'>>): string { return `${row.materialType}\u0000${row.materialNo}\u0000${row.labelSource ?? 5}\u0000${row.labelType}` }
function readStore(): LabelMap[] {
  try {
    const raw = window.localStorage.getItem(storageKey)
    return raw ? JSON.parse(raw) as LabelMap[] : structuredClone(seedMaps)
  } catch { return structuredClone(seedMaps) }
}
function writeStore(value: LabelMap[]): void { window.localStorage.setItem(storageKey, JSON.stringify(value)) }
function now(): string { return new Date().toISOString() }
const statusTime = ref(now())
function enc(value: string): string { return encodeURIComponent(value) }
function dateText(value: string | null | undefined): string {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.valueOf())) return value
  const pad = (part: number) => String(part).padStart(2, '0')
  return `${date.getFullYear()}/${pad(date.getMonth() + 1)}/${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
}
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init })
  if (response.status === 401) { window.location.assign(appBase + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    if (response.status === 404) throw new Error('物料標籤對應 API 尚未建立；請依 todolist_homepc.md 完成 MTL ML107。')
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 回應錯誤 (${response.status})。`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function stampQuery(value: number | null): string { return value === null ? 'dataStamp=0&dataStampIsNull=true' : `dataStamp=${enc(String(value))}` }
function pickerPath(kind: PickerKind): string {
  if (kind === 'materialType') return `${typeApi}?issueState=2`
  if (kind === 'material') return `${materialApi}?${new URLSearchParams({ issueState: '2' })}`
  return `${labelApi}?labelSource=5`
}

async function loadRows(preserve = ''): Promise<void> {
  loading.value = true; errorMessage.value = ''; notice.value = ''
  try {
    if (mock.value) {
      if (!window.localStorage.getItem(storageKey)) writeStore(structuredClone(seedMaps))
      const query = { materialType: filters.value.materialType.trim().toLocaleLowerCase(), materialNo: filters.value.materialNo.trim().toLocaleLowerCase(), labelNo: filters.value.labelNo.trim().toLocaleLowerCase() }
      rows.value = readStore().filter(row => (!filterEnabled.value.materialType || row.materialType.toLocaleLowerCase().includes(query.materialType)) && (!filterEnabled.value.materialNo || row.materialNo.toLocaleLowerCase().includes(query.materialNo)) && (!filterEnabled.value.labelNo || row.labelNo.toLocaleLowerCase().includes(query.labelNo)))
    } else {
      const params = new URLSearchParams()
      if (filterEnabled.value.materialType && filters.value.materialType.trim()) params.set('materialType', filters.value.materialType.trim())
      if (filterEnabled.value.materialNo && filters.value.materialNo.trim()) params.set('materialNo', filters.value.materialNo.trim())
      if (filterEnabled.value.labelNo && filters.value.labelNo.trim()) params.set('labelNo', filters.value.labelNo.trim())
      const query = params.toString()
      rows.value = await request<LabelMap[]>(mapApi + (query ? `?${query}` : ''))
    }
    const next = rows.value.find(row => rowKey(row) === preserve) ?? rows.value[0] ?? null
    activeKey.value = next ? rowKey(next) : ''
    selectedKeys.value = []
    hasQueried.value = true
  } catch (error) {
    rows.value = []; activeKey.value = ''; selectedKeys.value = []
    errorMessage.value = error instanceof Error ? error.message : '物料標籤對應資料載入失敗。'
  } finally { loading.value = false }
}
function runQuery(): void { void loadRows() }
function selectRow(row: LabelMap): void { activeKey.value = rowKey(row); if (!selectedKeys.value.includes(activeKey.value)) selectedKeys.value = [activeKey.value]; errorMessage.value = ''; notice.value = '' }
function toggleRow(row: LabelMap, checked: boolean): void {
  const key = rowKey(row); activeKey.value = key
  selectedKeys.value = checked ? [...new Set([...selectedKeys.value, key])] : selectedKeys.value.filter(item => item !== key)
}
function updateFilter(key: keyof typeof filters.value): void {
  if (key !== 'labelNo') filters.value[key] = filters.value[key].toUpperCase()
  filterEnabled.value[key] = Boolean(filters.value[key].trim())
}

function openEditor(mode: Exclude<EditorMode, null>): void {
  const row = activeRow.value
  if ((mode === 'edit' || mode === 'copy') && !row) return
  editorMode.value = mode; sourceRow.value = mode === 'add' ? null : row; editorError.value = ''; pickerKind.value = null
  editor.value = mode === 'add' || !row ? { materialType: '', materialNo: '', labelType: '', labelNo: '' } : { materialType: row.materialType, materialNo: row.materialNo, labelType: row.labelType, labelNo: row.labelNo }
  editorSnapshot.value = JSON.stringify(editor.value)
  audit.value = mode === 'edit' && row
    ? { creator: row.creator ?? '', createDate: row.createDate ?? '', reviser: props.currentUserNo || '', reviseDate: now() }
    : { creator: props.currentUserNo || 'DEV001', createDate: now(), reviser: '', reviseDate: '' }
}
function closeEditor(): void { editorMode.value = null; sourceRow.value = null; editorError.value = ''; pickerKind.value = null; editorSnapshot.value = '' }
function mapPath(row: LabelMap): string { return `${mapApi}/${enc(row.materialType)}/${enc(row.materialNo)}/${enc(row.labelType)}` }
function pickedLabel(row: LabelTemplate): void {
  if (editorMode.value !== 'edit') editor.value.labelType = row.labelType
  editor.value.labelNo = row.labelNo
  pickerKind.value = null; pickerQuery.value = ''
}
function pickedMaterialType(row: MaterialType): void {
  editor.value.materialType = row.materialType
  editor.value.materialNo = ''
  pickerKind.value = null; pickerQuery.value = ''
}
function pickedMaterial(row: Material): void {
  editor.value.materialNo = row.materialNo
  pickerKind.value = null; pickerQuery.value = ''
}

async function openPicker(kind: PickerKind): Promise<void> {
  pickerContext.value = 'editor'
  if (kind === 'material' && !editor.value.materialType) { editorError.value = '請先選擇物料類別。'; return }
  pickerKind.value = kind; pickerQuery.value = ''; pickerLoading.value = true; pickerRows.value = []; editorError.value = ''
  try {
    if (mock.value) {
      if (kind === 'materialType') pickerRows.value = structuredClone(seedTypes)
      else if (kind === 'material') {
        materials.value = structuredClone(seedMaterials)
        const items = materials.value.filter(row => row.issueState === 2 && row.materialType.toUpperCase() === editor.value.materialType.toUpperCase())
        pickerRows.value = items.length ? [...items, { materialNo: '*', materialType: editor.value.materialType, materialName: '', issueState: 2 }] : []
      } else {
        labels.value = structuredClone(seedLabels).filter(row => row.labelSource === 5)
        pickerRows.value = editorMode.value === 'edit' ? labels.value.filter(row => row.labelType === editor.value.labelType) : labels.value
      }
    } else {
      if (kind === 'material') {
        const query = new URLSearchParams({ issueState: '2', materialType: editor.value.materialType })
        const items = await request<Material[]>(`${materialApi}?${query}`)
        materials.value = items
        pickerRows.value = items.length ? [...items, { materialNo: '*', materialType: editor.value.materialType, materialName: '', issueState: 2 }] : []
      } else if (kind === 'label') {
        const items = await request<LabelTemplate[]>(pickerPath(kind))
        labels.value = items.filter(row => row.labelSource === 5)
        pickerRows.value = editorMode.value === 'edit' ? labels.value.filter(row => row.labelType === editor.value.labelType) : labels.value
      } else {
        types.value = await request<MaterialType[]>(pickerPath(kind))
        pickerRows.value = types.value
      }
    }
  } catch (error) { pickerKind.value = null; editorError.value = error instanceof Error ? error.message : '查詢選擇清單失敗。' }
  finally { pickerLoading.value = false }
}
async function openSearchPicker(kind: PickerKind): Promise<void> {
  pickerContext.value = 'search'; pickerKind.value = kind; pickerQuery.value = ''; pickerLoading.value = true; pickerRows.value = []
  try {
    if (mock.value) {
      pickerRows.value = kind === 'materialType' ? structuredClone(seedTypes) : kind === 'material' ? structuredClone(seedMaterials.filter(row => row.issueState === 2)) : structuredClone(seedLabels.filter(row => row.labelSource === 5))
    } else {
      const result = await request<Array<MaterialType | Material | LabelTemplate>>(pickerPath(kind))
      pickerRows.value = kind === 'label' ? (result as LabelTemplate[]).filter(row => row.labelSource === 5) : result
    }
  } catch (error) { pickerKind.value = null; errorMessage.value = error instanceof Error ? error.message : '查詢選擇清單失敗。' }
  finally { pickerLoading.value = false }
}
function choosePickerRow(row: MaterialType | Material | LabelTemplate): void {
  if (pickerContext.value === 'search') {
    if (pickerKind.value === 'materialType') { filters.value.materialType = (row as MaterialType).materialType; filterEnabled.value.materialType = true }
    else if (pickerKind.value === 'material') { filters.value.materialNo = (row as Material).materialNo; filterEnabled.value.materialNo = true }
    else if (pickerKind.value === 'label') { filters.value.labelNo = (row as LabelTemplate).labelNo; filterEnabled.value.labelNo = true }
    pickerKind.value = null
    return
  }
  if (pickerKind.value === 'materialType') pickedMaterialType(row as MaterialType)
  else if (pickerKind.value === 'material') pickedMaterial(row as Material)
  else if (pickerKind.value === 'label') pickedLabel(row as LabelTemplate)
}

async function save(closeAfter: boolean): Promise<void> {
  if (!editorMode.value || busy.value) return
  editor.value = { materialType: editor.value.materialType.trim().toUpperCase(), materialNo: editor.value.materialNo.trim().toUpperCase(), labelType: editor.value.labelType.trim().toUpperCase(), labelNo: editor.value.labelNo.trim() }
  editorError.value = ''
  if (!editor.value.materialType) { editorError.value = '請選擇物料類別。'; return }
  if (!editor.value.materialNo) { editorError.value = '請選擇物料編號。'; return }
  if (!editor.value.labelType) { editorError.value = '請選擇標籤類別。'; return }
  if (!editor.value.labelNo) { editorError.value = '請輸入或選擇標籤編號。'; return }
  if (editor.value.materialType.length > 50 || editor.value.materialNo.length > 50 || editor.value.labelType.length > 50 || editor.value.labelNo.length > 50) { editorError.value = '欄位長度超過資料庫限制。'; return }
  busy.value = true
  try {
    const mode = editorMode.value; const source = sourceRow.value
    let saved: LabelMap
    if (mock.value) {
      const store = readStore()
      if (mode === 'edit' && source) {
        const key = rowKey(source)
        const existing = store.find(row => rowKey(row) === key)
        if (!existing || existing.dataStamp !== source.dataStamp) throw new Error('資料已被其他使用者修改，請重新載入後再試。')
        const updated: LabelMap = { ...existing, labelNo: editor.value.labelNo, reviser: props.currentUserNo || 'DEV001', reviseDate: now(), dataStamp: Number(existing.dataStamp ?? 0) + 1 }
        store[store.findIndex(row => rowKey(row) === key)] = updated; saved = updated
      } else {
        const key = rowKey({ ...editor.value, labelType: editor.value.labelType })
        if (store.some(row => rowKey(row) === key)) throw new Error('相同物料類別、物料編號及標籤類別的對應資料已存在。')
        const material = seedMaterials.find(row => row.materialNo === editor.value.materialNo)
        const materialType = seedTypes.find(row => row.materialType === editor.value.materialType)
        saved = { ...editor.value, labelSource: 5, materialTypeName: materialType?.materialTypeName ?? editor.value.materialType, materialName: material?.materialName ?? (editor.value.materialNo === '*' ? '' : null), creator: props.currentUserNo || 'DEV001', createDate: now(), reviser: null, reviseDate: null, dataStamp: 0 }
        store.push(saved)
      }
      writeStore(store)
    } else if (mode === 'edit' && source) {
      await request<void>(`${mapPath(source)}?${stampQuery(source.dataStamp)}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ labelNo: editor.value.labelNo }) })
      saved = { ...source, labelNo: editor.value.labelNo, reviser: props.currentUserNo || '', reviseDate: now(), dataStamp: Number(source.dataStamp ?? 0) + 1 }
    } else {
      saved = await request<LabelMap>(mapApi, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ ...editor.value, labelSource: 5 }) })
    }
    await loadRows(rowKey(saved))
    editor.value = { materialType: saved.materialType, materialNo: saved.materialNo, labelType: saved.labelType, labelNo: saved.labelNo }
    audit.value = { creator: saved.creator ?? '', createDate: saved.createDate ?? '', reviser: saved.reviser ?? '', reviseDate: saved.reviseDate ?? '' }
    editorSnapshot.value = JSON.stringify(editor.value)
    notice.value = mode === 'edit' ? '物料標籤對應資料已更新。' : mode === 'copy' ? '物料標籤對應資料已複製。' : '物料標籤對應資料已新增。'
    if (mode === 'edit') sourceRow.value = saved
    if (closeAfter) closeEditor()
  } catch (error) { editorError.value = error instanceof Error ? error.message : '物料標籤對應資料儲存失敗。' }
  finally { busy.value = false }
}
async function deleteSelected(): Promise<void> {
  const keys = selectedKeys.value.length ? selectedKeys.value : activeKey.value ? [activeKey.value] : []
  const targets = keys.map(key => rows.value.find(row => rowKey(row) === key)).filter((row): row is LabelMap => Boolean(row))
  if (!targets.length || busy.value) return
  if (!window.confirm(`確定刪除選取的 ${targets.length} 筆物料標籤對應資料？`)) return
  busy.value = true; errorMessage.value = ''; notice.value = ''; let completed = 0
  try {
    for (const row of targets) {
      if (mock.value) {
        const store = readStore().filter(item => rowKey(item) !== rowKey(row))
        if (store.length === readStore().length) throw new Error(`資料不存在：${row.materialNo} / ${row.labelType}`)
        writeStore(store)
      } else await request<void>(mapPath(row), { method: 'DELETE' })
      completed++
    }
    await loadRows(); notice.value = `已刪除 ${completed} 筆物料標籤對應資料。`
  } catch (error) {
    await loadRows()
    errorMessage.value = `${error instanceof Error ? error.message : '刪除失敗。'} 已完成 ${completed}/${targets.length} 筆。`
  } finally { busy.value = false }
}
function onFilterEnter(): void { runQuery() }
</script>

<template>
  <section class="label-map-page">
    <header class="page-title"><h1>物料標籤對應設定</h1></header>
    <div class="search-strip" @keydown.enter.prevent="onFilterEnter">
      <label class="search-field"><input type="checkbox" :checked="filterEnabled.materialType" @change="filterEnabled.materialType = ($event.target as HTMLInputElement).checked"><span>物料類別</span><input v-model="filters.materialType" class="kd-input" @input="updateFilter('materialType')"><button type="button" class="win-btn lookup-button" title="選擇物料類別" @click="openSearchPicker('materialType')">…</button></label>
      <label class="search-field"><input type="checkbox" :checked="filterEnabled.materialNo" @change="filterEnabled.materialNo = ($event.target as HTMLInputElement).checked"><span>物料編號</span><input v-model="filters.materialNo" class="kd-input" @input="updateFilter('materialNo')"><button type="button" class="win-btn lookup-button" title="選擇物料編號" @click="openSearchPicker('material')">…</button></label>
      <label class="search-field"><input type="checkbox" :checked="filterEnabled.labelNo" @change="filterEnabled.labelNo = ($event.target as HTMLInputElement).checked"><span>標籤編號</span><input v-model="filters.labelNo" class="kd-input" @input="updateFilter('labelNo')"><button type="button" class="win-btn lookup-button" title="選擇物料標籤" @click="openSearchPicker('label')">…</button></label>
      <button class="win-btn query-button" :disabled="loading" @click="runQuery">查詢</button>
    </div>
    <p v-if="errorMessage" class="message error" role="alert">{{ errorMessage }}</p>
    <p v-if="notice" class="message notice" role="status">{{ notice }}</p>
    <main class="master-area">
      <div class="grid-panel">
        <div class="grid-scroll"><table>
          <thead><tr><th class="check"></th><th>物料類別</th><th>物料編號</th><th>物料類別名稱</th><th>物料名稱</th><th>標籤類別</th><th>標籤編號</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th></tr></thead>
          <tbody>
            <tr v-for="row in visibleRows" :key="rowKey(row)" :class="{ active: activeKey === rowKey(row), selected: selectedKeys.includes(rowKey(row)) }" @click="selectRow(row)" @dblclick="openEditor('edit')">
              <td class="check"><input type="checkbox" :checked="selectedKeys.includes(rowKey(row))" @click.stop="activeKey = rowKey(row)" @change="toggleRow(row, ($event.target as HTMLInputElement).checked)"></td>
              <td>{{ row.materialType }}</td><td>{{ row.materialNo }}</td><td>{{ row.materialTypeName }}</td><td>{{ row.materialName }}</td><td>{{ row.labelType }}</td><td>{{ row.labelNo }}</td><td>{{ row.creator }}</td><td>{{ dateText(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ dateText(row.reviseDate) }}</td>
            </tr>
            <tr v-if="!loading && !visibleRows.length"><td colspan="11" class="empty">{{ hasQueried ? '沒有符合條件的物料標籤對應資料' : '請輸入查詢條件後按「查詢」載入資料' }}</td></tr>
          </tbody>
        </table><div v-if="loading" class="loading-cover">資料載入中…</div></div>
        <aside class="actions">
          <button class="win-btn primary" :disabled="busy" @click="openEditor('add')">新增(A)</button>
          <button class="win-btn" :disabled="busy || !activeRow" @click="openEditor('edit')">修改(E)</button>
          <button class="win-btn" :disabled="busy || !(selectedKeys.length || activeRow)" @click="deleteSelected">刪除(D)</button>
          <button class="win-btn" :disabled="busy || !activeRow" @click="openEditor('copy')">複製(C)</button>
          <button class="win-btn close-action" :disabled="busy" @click="emit('close')">關閉</button>
        </aside>
      </div>
    </main>
    <footer class="status-line"><span>UserNo: {{ props.currentUserNo || 'DEV001' }}</span><span>{{ dateText(statusTime) }}</span></footer>

    <div v-if="editorMode" class="backdrop" @click.self="closeEditor">
      <form class="editor" @submit.prevent="save(true)">
        <header class="dialog-title"><h2>{{ editorTitle }}</h2><button type="button" class="x-button" @click="closeEditor">×</button></header>
        <fieldset class="fields"><legend>物料標籤對應資料</legend>
          <label><span>物料類別</span><div class="lookup-field"><input :value="editor.materialType" class="kd-input" readonly><button type="button" class="win-btn lookup-button" :disabled="editorMode === 'edit'" @click="openPicker('materialType')">…</button></div></label>
          <label><span>物料類別名稱</span><input :value="types.find(row => row.materialType === editor.materialType)?.materialTypeName ?? sourceRow?.materialTypeName ?? ''" readonly></label>
          <label><span>物料編號</span><div class="lookup-field"><input :value="editor.materialNo" class="kd-input" readonly><button type="button" class="win-btn lookup-button" :disabled="editorMode === 'edit' || !editor.materialType" @click="openPicker('material')">…</button></div></label>
          <label><span>物料名稱</span><input :value="materials.find(row => row.materialNo === editor.materialNo)?.materialName ?? sourceRow?.materialName ?? ''" readonly></label>
          <label><span>標籤類別</span><input :value="editor.labelType" class="kd-input" readonly></label>
          <label><span>標籤編號</span><div class="lookup-field"><input v-model="editor.labelNo" class="kd-input" maxlength="50"><button type="button" class="win-btn lookup-button" @click="openPicker('label')">…</button></div></label>
        </fieldset>
        <fieldset class="audit"><legend>建立 / 修改資訊</legend><label>建立人<input :value="audit.creator" readonly></label><label>建立日期<input :value="dateText(audit.createDate)" readonly></label><label>修改人<input :value="audit.reviser" readonly></label><label>修改日期<input :value="dateText(audit.reviseDate)" readonly></label></fieldset>
        <p v-if="editorError" class="message error" role="alert">{{ editorError }}</p>
        <footer class="dialog-actions"><button class="win-btn primary" :disabled="busy">確定</button><button type="button" class="win-btn" :disabled="busy" @click="closeEditor">關閉</button><button type="button" class="win-btn" :disabled="busy || !hasChanges" @click="save(false)">套用</button></footer>
      </form>
    </div>
    <div v-if="pickerKind" class="backdrop picker-backdrop" @click.self="pickerKind = null">
      <section class="editor picker-dialog"><header class="dialog-title"><h2>{{ pickerKind === 'materialType' ? '選擇物料類別' : pickerKind === 'material' ? '選擇物料編號' : '選擇物料標籤' }}</h2><button class="x-button" @click="pickerKind = null">×</button></header>
        <label class="picker-search"><span>快速查詢</span><input v-model="pickerQuery" class="kd-input" autofocus></label>
        <div class="picker-list"><table><thead><tr><th>{{ pickerKind === 'materialType' ? '物料類別' : pickerKind === 'material' ? '物料編號' : '標籤編號' }}</th><th>{{ pickerKind === 'materialType' ? '物料類別名稱' : pickerKind === 'material' ? '物料名稱' : '標籤類別' }}</th><th v-if="pickerKind === 'label'">格式檔名</th><th v-if="pickerKind === 'label'">說明</th></tr></thead><tbody>
          <tr v-for="row in filteredPickerRows" :key="pickerKind === 'materialType' ? (row as MaterialType).materialType : pickerKind === 'material' ? (row as Material).materialNo : (row as LabelTemplate).labelNo" @dblclick="choosePickerRow(row)" @click="choosePickerRow(row)">
            <template v-if="pickerKind === 'materialType'"><td>{{ (row as MaterialType).materialType }}</td><td>{{ (row as MaterialType).materialTypeName }}</td></template>
            <template v-else-if="pickerKind === 'material'"><td>{{ (row as Material).materialNo }}</td><td>{{ (row as Material).materialName }}</td></template>
            <template v-else><td>{{ (row as LabelTemplate).labelNo }}</td><td>{{ (row as LabelTemplate).labelType }}</td><td>{{ (row as LabelTemplate).formatFileName }}</td><td>{{ (row as LabelTemplate).description }}</td></template>
          </tr>
          <tr v-if="!pickerLoading && !filteredPickerRows.length"><td :colspan="pickerKind === 'label' ? 4 : 2" class="empty">沒有可選資料</td></tr>
        </tbody></table><div v-if="pickerLoading" class="loading-cover">選擇清單載入中…</div></div>
        <footer class="dialog-actions"><button type="button" class="win-btn" @click="pickerKind = null">關閉</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.label-map-page{height:100%;min-height:0;display:flex;flex-direction:column;background:#fff;color:#292929;font:14px "Microsoft JhengHei",Arial,sans-serif}.page-title{height:50px;flex:none;display:flex;align-items:center;padding:0 24px;border-bottom:1px solid #ddd8ce;background:#f7f5ef}.page-title h1{margin:0;font-size:20px;font-weight:500}.search-strip{min-height:46px;flex:none;display:flex;align-items:center;flex-wrap:wrap;gap:8px;padding:5px 10px;border-bottom:1px solid #ccc}.search-field{display:flex;align-items:center;gap:5px;white-space:nowrap}.search-field span{min-width:60px}.kd-input{box-sizing:border-box;min-width:0;height:27px;border:1px solid #999;border-radius:0;padding:3px 6px;background:#ffffc0;color:#222;font:inherit}.kd-input:read-only,.kd-input:disabled{background:#eee}.search-field>.kd-input{width:145px}.win-btn{min-height:29px;padding:3px 9px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#f1efeb);font:inherit;white-space:nowrap;cursor:pointer}.win-btn:hover:not(:disabled){border-color:#e39a37;background:#fff8e8}.win-btn:disabled{opacity:.5;cursor:default}.win-btn.primary{border-color:#e69520;background:#f7a52a;color:#fff;font-weight:600}.lookup-button{width:29px;padding:0}.query-button{min-width:52px}.message{margin:4px 10px;padding:6px 9px;border:1px solid #ddd;background:#fff9e7}.message.error{color:#8d2222;background:#fff3f1}.message.notice{color:#4d6128;background:#f5f8eb}.master-area{flex:1;min-height:0;display:flex;flex-direction:column;padding:7px 10px 4px}.grid-panel{flex:1;min-height:120px;display:grid;grid-template-columns:minmax(0,1fr) 90px;gap:8px}.grid-scroll{position:relative;min-width:0;min-height:0;overflow:auto;border:1px solid #aaa;background:#fff}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}th,td{height:24px;padding:3px 6px;border:1px solid #c9c9c9;text-align:left}th{position:sticky;top:0;z-index:1;background:#e7e5df;font-weight:500}tbody tr{cursor:pointer}tbody tr:hover{background:#fff5d8}tbody tr.active{background:#ffefbf;outline:1px solid #d5a13d;outline-offset:-1px}tbody tr.selected:not(.active){background:#f8f3e6}.check{width:25px;text-align:center;padding:2px}.check input{accent-color:#e4a02f}.empty{text-align:center;color:#777;padding:18px}.actions{display:flex;flex-direction:column;gap:6px}.close-action{margin-top:auto}.status-line{height:30px;flex:none;display:flex;align-items:center;justify-content:space-between;padding:0 12px;border-top:1px solid #ccc;color:#666;font-size:12px}.loading-cover{position:absolute;inset:28px 0 0;display:grid;place-items:center;background:#ffffffb8;color:#555}.backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:10px;background:#0005}.editor{box-sizing:border-box;width:min(560px,96vw);display:flex;flex-direction:column;padding:0 10px 7px;border:1px solid #999;background:#f9f8f4;box-shadow:0 8px 28px #0004}.dialog-title{height:36px;flex:none;display:flex;align-items:center;justify-content:space-between;margin:0 -10px 5px;padding:0 10px;border-bottom:1px solid #ccc;background:#f1efe9}.dialog-title h2{margin:0;font-size:15px;font-weight:500}.x-button{border:0;background:transparent;color:#555;font-size:24px;line-height:1;cursor:pointer}.fields{display:flex;flex-direction:column;gap:6px;margin:6px 0;padding:7px;border:1px solid #aaa}.fields legend,.audit legend{padding:0 5px;color:#555}.fields>label{display:grid;grid-template-columns:128px minmax(0,1fr);align-items:center;gap:7px}.fields>label>span{text-align:right}.fields>label>input{box-sizing:border-box;width:100%;height:27px;border:1px solid #aaa;background:#eee;padding:3px 6px;font:inherit}.fields .kd-input{background:#ffffc0}.fields .kd-input:read-only{background:#ffffc0}.lookup-field{display:flex;gap:5px}.lookup-field .kd-input{flex:1}.audit{display:grid;grid-template-columns:1fr 1fr;gap:5px 10px;margin:0;padding:5px 7px;border:1px solid #aaa}.audit label{display:grid;grid-template-columns:58px minmax(0,1fr);align-items:center;gap:5px;font-size:11px;white-space:nowrap}.audit input{box-sizing:border-box;width:100%;min-width:0;height:22px;border:1px solid #aaa;background:#eee;padding:2px 5px;font:inherit}.dialog-actions{display:flex;justify-content:flex-end;gap:7px;padding:7px 0 1px}.picker-backdrop{z-index:1002}.picker-dialog{height:min(490px,90vh);min-height:260px}.picker-search{display:flex;align-items:center;gap:8px;margin:5px 2px 8px}.picker-search span{white-space:nowrap}.picker-search input{flex:1}.picker-list{position:relative;flex:1;min-height:0;overflow:auto;border:1px solid #aaa;background:#fff}.picker-list table{width:100%}.picker-list tbody tr:hover{background:#fff1c7}@media(max-width:850px){.search-strip{align-items:stretch}.search-field{flex:1 1 280px}.search-field>.kd-input{flex:1}.grid-panel{grid-template-columns:minmax(0,1fr) 76px}.actions{gap:4px}.actions .win-btn{padding:3px 5px;font-size:12px}.master-area{padding:5px}.fields>label{grid-template-columns:110px minmax(0,1fr)}}
</style>
