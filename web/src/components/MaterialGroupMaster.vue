<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

interface MaterialGroup {
  groupNo: string
  materialType: string
  description: string | null
  creator: string | null
  createDate: string | null
  dataStamp: number | null
  reviser: string | null
  reviseDate: string | null
}
interface Material { materialNo: string; materialType: string; materialName?: string | null; issueState: number }
interface MaterialType { materialType: string; materialTypeName?: string | null; issueState?: number }
interface GroupStore { groups: MaterialGroup[]; members: Record<string, string[]> }
type EditorMode = 'add' | 'edit' | 'copy' | null

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const groupApi = 'api/material-groups'
const typeApi = 'api/material-types'
const materialApi = 'api/materials'
const groupStorageKey = 'ymes-dev-mock:mtl-material-group:v1'
const materialStorageKey = 'ymes:dev:master-data:materials:v1'
const typeStorageKey = 'ymes-dev-mock:mtl-material-type:v1'

const seedGroups: MaterialGroup[] = [
  { groupNo: 'DEV-GROUP-CHEM-01', materialType: 'CHEM', description: '開發用清洗液群組', creator: 'DEV001', createDate: '2026-11-01T08:00:00+08:00', dataStamp: 1, reviser: 'DEV001', reviseDate: '2026-11-02T09:00:00+08:00' },
  { groupNo: 'DEV-GROUP-PACK-01', materialType: 'PACK', description: '開發用包裝物料群組', creator: 'DEV001', createDate: '2026-11-03T08:00:00+08:00', dataStamp: 0, reviser: null, reviseDate: null },
]
const seedMaterials: Material[] = [
  { materialNo: 'MAT-GUM-001', materialType: 'GUM', materialName: '固晶膠 A（DEV）', issueState: 0 },
  { materialNo: 'MAT-CHEM-002', materialType: 'CHEM', materialName: '清洗液 B（DEV）', issueState: 2 },
  { materialNo: 'MAT-PACK-003', materialType: 'PACK', materialName: '包裝盒 C（DEV）', issueState: 1 },
  { materialNo: 'MAT-WIP-004', materialType: 'CHEM', materialName: '研發試料 D（DEV）', issueState: -1 },
]
const seedStore: GroupStore = { groups: structuredClone(seedGroups), members: { 'DEV-GROUP-CHEM-01': ['MAT-CHEM-002'], 'DEV-GROUP-PACK-01': [] } }

const groups = ref<MaterialGroup[]>([])
const materials = ref<Material[]>([])
const types = ref<MaterialType[]>([])
const assigned = ref<string[]>([])
const available = ref<string[]>([])
const activeGroupNo = ref('')
const checkedGroupNos = ref<string[]>([])
const groupQuery = ref('')
const availableSearch = ref('')
const assignedSearch = ref('')
const availableSelection = ref<string[]>([])
const assignedSelection = ref<string[]>([])
const loading = ref(false)
const detailLoading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const notice = ref('')

const editorMode = ref<EditorMode>(null)
const editorSource = ref<MaterialGroup | null>(null)
const editor = ref({ groupNo: '', materialType: '', description: '' })
const audit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const editorError = ref('')
const initialEditorSnapshot = ref('')
const typePickerOpen = ref(false)
const typeQuery = ref('')

const visibleGroups = computed(() => groups.value.slice().sort((a, b) => a.groupNo.localeCompare(b.groupNo)))
const activeGroup = computed(() => groups.value.find(row => row.groupNo === activeGroupNo.value) ?? null)
const availableTypes = computed(() => types.value.filter(row => !typeQuery.value.trim() || row.materialType.toLocaleLowerCase().includes(typeQuery.value.trim().toLocaleLowerCase()) || String(row.materialTypeName ?? '').toLocaleLowerCase().includes(typeQuery.value.trim().toLocaleLowerCase())).slice().sort((a, b) => a.materialType.localeCompare(b.materialType)))
const visibleAvailable = computed(() => available.value.filter(no => no.toLocaleLowerCase().includes(availableSearch.value.trim().toLocaleLowerCase())))
const visibleAssigned = computed(() => assigned.value.filter(no => no.toLocaleLowerCase().includes(assignedSearch.value.trim().toLocaleLowerCase())))
const editorTitle = computed(() => `物料群組資料（${editorMode.value === 'add' ? '新增' : editorMode.value === 'copy' ? '複製' : '修改'}）`)
const hasEditorChanges = computed(() => Boolean(editorMode.value) && JSON.stringify(editor.value) !== initialEditorSnapshot.value)

function freshStore(): GroupStore { return structuredClone(seedStore) }
function readStore(): GroupStore {
  try {
    const raw = window.localStorage.getItem(groupStorageKey)
    if (!raw) return freshStore()
    const parsed = JSON.parse(raw) as Partial<GroupStore>
    return { groups: parsed.groups ?? structuredClone(seedGroups), members: parsed.members ?? structuredClone(seedStore.members) }
  } catch { return freshStore() }
}
function writeStore(value: GroupStore): void { window.localStorage.setItem(groupStorageKey, JSON.stringify(value)) }
function readMaterials(): Material[] {
  try {
    const raw = window.localStorage.getItem(materialStorageKey)
    if (raw) return JSON.parse(raw) as Material[]
  } catch { /* use isolated development fixtures when browser storage is unavailable */ }
  try { window.localStorage.setItem(materialStorageKey, JSON.stringify(seedMaterials)) } catch { /* this page remains usable for the current session */ }
  return structuredClone(seedMaterials)
}
function readTypes(): MaterialType[] {
  try {
    const raw = window.localStorage.getItem(typeStorageKey)
    if (raw) {
      const parsed = JSON.parse(raw) as { types?: MaterialType[] }
      return parsed.types ?? []
    }
  } catch { /* material types can still be inferred from the local demo materials */ }
  return []
}
function now(): string { return new Date().toISOString() }
function enc(value: string): string { return encodeURIComponent(value) }
function dateText(value: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat('zh-TW', { dateStyle: 'short', timeStyle: 'medium', hour12: false }).format(date)
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
    if (response.status === 404) throw new Error('物料群組 API 尚未建立；請依 todolist_homepc.md 完成 MTL ML104。')
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 回應錯誤 (${response.status})。`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function stampQuery(value: number | null): string { return value === null ? 'dataStamp=0&dataStampIsNull=true' : `dataStamp=${enc(String(value))}` }

async function loadTypes(): Promise<void> {
  if (mock.value) {
    materials.value = readMaterials()
    const storeTypes = readTypes()
    const fromMaterials = [...new Set(materials.value.map(row => row.materialType).filter(Boolean))].map(materialType => ({ materialType }))
    types.value = [...new Map([...storeTypes, ...fromMaterials].map(row => [row.materialType.toUpperCase(), row])).values()]
    return
  }
  types.value = await request<MaterialType[]>(typeApi)
}
async function loadDetails(group: MaterialGroup | null): Promise<void> {
  assigned.value = []; available.value = []; availableSelection.value = []; assignedSelection.value = []
  availableSearch.value = ''; assignedSearch.value = ''
  if (!group) return
  detailLoading.value = true
  try {
    if (mock.value) {
      const store = readStore()
      assigned.value = (store.members[group.groupNo] ?? []).slice().sort((a, b) => a.localeCompare(b))
      materials.value = readMaterials()
      available.value = materials.value.filter(row => row.issueState === 2 && row.materialType.toUpperCase() === group.materialType.toUpperCase() && !assigned.value.includes(row.materialNo)).map(row => row.materialNo).sort((a, b) => a.localeCompare(b))
    } else {
      const [members, candidates] = await Promise.all([
        request<string[]>(`${groupApi}/${enc(group.groupNo)}/materials`),
        request<Material[]>(`${materialApi}?${new URLSearchParams({ issueState: '2', materialType: group.materialType })}`),
      ])
      assigned.value = members.slice().sort((a, b) => a.localeCompare(b))
      available.value = candidates.map(row => row.materialNo).filter(no => !assigned.value.includes(no)).sort((a, b) => a.localeCompare(b))
    }
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '物料群組明細載入失敗。' }
  finally { detailLoading.value = false }
}
async function loadGroups(preserve = ''): Promise<void> {
  loading.value = true; errorMessage.value = ''; notice.value = ''
  try {
    if (mock.value) {
      const query = groupQuery.value.trim().toLocaleLowerCase()
      groups.value = readStore().groups.filter(row => !query || row.groupNo.toLocaleLowerCase().includes(query)).sort((a, b) => a.groupNo.localeCompare(b.groupNo))
    } else {
      const query = new URLSearchParams()
      if (groupQuery.value.trim()) query.set('groupNo', groupQuery.value.trim())
      const queryText = query.toString()
      groups.value = await request<MaterialGroup[]>(groupApi + (queryText ? `?${queryText}` : ''))
    }
    const next = groups.value.find(row => row.groupNo === preserve) ?? groups.value[0] ?? null
    activeGroupNo.value = next?.groupNo ?? ''
    checkedGroupNos.value = []
    await loadDetails(next)
  } catch (error) {
    groups.value = []; activeGroupNo.value = ''; assigned.value = []; available.value = []
    errorMessage.value = error instanceof Error ? error.message : '物料群組載入失敗。'
  } finally { loading.value = false }
}
async function selectGroup(row: MaterialGroup): Promise<void> {
  activeGroupNo.value = row.groupNo
  if (!checkedGroupNos.value.includes(row.groupNo)) checkedGroupNos.value = [row.groupNo]
  errorMessage.value = ''; notice.value = ''
  await loadDetails(row)
}
function toggleGroup(row: MaterialGroup, checked: boolean): void {
  activeGroupNo.value = row.groupNo
  checkedGroupNos.value = checked ? [...new Set([...checkedGroupNos.value, row.groupNo])] : checkedGroupNos.value.filter(key => key !== row.groupNo)
  void loadDetails(row)
}
function moveItems(source: 'available' | 'assigned'): void {
  if (source === 'available') {
    const moving = new Set(availableSelection.value)
    assigned.value = [...new Set([...assigned.value, ...available.value.filter(no => moving.has(no))])].sort((a, b) => a.localeCompare(b))
    available.value = available.value.filter(no => !moving.has(no))
    availableSelection.value = []
  } else {
    const moving = new Set(assignedSelection.value)
    available.value = [...new Set([...available.value, ...assigned.value.filter(no => moving.has(no))])].sort((a, b) => a.localeCompare(b))
    assigned.value = assigned.value.filter(no => !moving.has(no))
    assignedSelection.value = []
  }
}
function moveByExactSearch(source: 'available' | 'assigned', event: KeyboardEvent): void {
  if (event.key !== 'Enter') return
  const search = (source === 'available' ? availableSearch.value : assignedSearch.value).trim().toUpperCase()
  const list = source === 'available' ? available.value : assigned.value
  if (!search || !list.includes(search)) return
  if (source === 'available') availableSelection.value = [search]
  else assignedSelection.value = [search]
  moveItems(source)
  if (source === 'available') availableSearch.value = ''
  else assignedSearch.value = ''
}
function openEditor(mode: Exclude<EditorMode, null>): void {
  const row = activeGroup.value
  if ((mode === 'edit' || mode === 'copy') && !row) return
  editorMode.value = mode; editorSource.value = mode === 'add' ? null : row; editorError.value = ''; typePickerOpen.value = false
  editor.value = mode === 'add' || !row
    ? { groupNo: '', materialType: '', description: '' }
    : { groupNo: row.groupNo, materialType: row.materialType, description: row.description ?? '' }
  initialEditorSnapshot.value = JSON.stringify(editor.value)
  audit.value = mode === 'edit' && row
    ? { creator: row.creator ?? '', createDate: row.createDate ?? '', reviser: props.currentUserNo || '', reviseDate: now() }
    : { creator: props.currentUserNo || 'DEV001', createDate: now(), reviser: '', reviseDate: '' }
}
function closeEditor(): void { editorMode.value = null; editorSource.value = null; editorError.value = ''; typePickerOpen.value = false; initialEditorSnapshot.value = '' }
async function saveGroup(closeAfter: boolean): Promise<void> {
  if (!editorMode.value || busy.value) return
  editor.value.groupNo = editor.value.groupNo.trim().toUpperCase()
  editor.value.materialType = editor.value.materialType.trim().toUpperCase()
  editor.value.description = editor.value.description.trim()
  editorError.value = ''
  if (!editor.value.groupNo) { editorError.value = '請輸入物料群組編號。'; return }
  if (editor.value.groupNo.length > 50) { editorError.value = '物料群組編號不可超過 50 個字元。'; return }
  if (!editor.value.materialType) { editorError.value = '請選擇物料類別。'; return }
  if (editor.value.materialType.length > 50) { editorError.value = '物料類別不可超過 50 個字元。'; return }
  if (editor.value.description.length > 255) { editorError.value = '說明不可超過 255 個字元。'; return }
  busy.value = true
  try {
    const existing = editorSource.value
    const mode = editorMode.value
    let savedNo = editor.value.groupNo
    if (mock.value) {
      const store = readStore()
      if (mode !== 'edit' && store.groups.some(row => row.groupNo.toUpperCase() === savedNo)) throw new Error('物料群組編號已存在。')
      if (mode === 'edit' && existing && existing.groupNo !== savedNo) throw new Error('修改時不可變更物料群組編號。')
      const timestamp = now()
      if (mode === 'edit' && existing) {
        store.groups = store.groups.map(row => row.groupNo === existing.groupNo ? { ...row, description: editor.value.description || null, reviser: props.currentUserNo || 'DEV001', reviseDate: timestamp, dataStamp: Number(row.dataStamp ?? 0) + 1 } : row)
      } else {
        const row: MaterialGroup = { groupNo: savedNo, materialType: editor.value.materialType, description: editor.value.description || null, creator: props.currentUserNo || 'DEV001', createDate: timestamp, dataStamp: 0, reviser: null, reviseDate: null }
        store.groups.push(row)
        store.members[savedNo] = []
        audit.value = { creator: row.creator ?? '', createDate: row.createDate ?? '', reviser: '', reviseDate: '' }
      }
      writeStore(store)
    } else if (mode === 'edit' && existing) {
      const query = stampQuery(existing.dataStamp)
      await request<void>(`${groupApi}/${enc(existing.groupNo)}?${query}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ description: editor.value.description || null }) })
      savedNo = existing.groupNo
    } else {
      const created = await request<MaterialGroup>(groupApi, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ groupNo: savedNo, materialType: editor.value.materialType, description: editor.value.description || null }) })
      savedNo = created.groupNo
    }
    await loadGroups(savedNo)
    editor.value.groupNo = savedNo
    notice.value = mode === 'edit' ? '物料群組資料已更新。' : mode === 'copy' ? '已複製物料群組主檔；明細不會複製。' : '物料群組資料已新增。'
    const savedRow = groups.value.find(row => row.groupNo === savedNo)
    if (savedRow && mode !== 'edit') audit.value = { creator: savedRow.creator ?? '', createDate: savedRow.createDate ?? '', reviser: savedRow.reviser ?? '', reviseDate: savedRow.reviseDate ?? '' }
    if (mode === 'edit' && editorSource.value) {
      editorSource.value = savedRow ?? editorSource.value
      audit.value = { creator: editorSource.value.creator ?? '', createDate: editorSource.value.createDate ?? '', reviser: editorSource.value.reviser ?? '', reviseDate: editorSource.value.reviseDate ?? '' }
    }
    initialEditorSnapshot.value = JSON.stringify(editor.value)
    if (closeAfter) closeEditor()
  } catch (error) { editorError.value = error instanceof Error ? error.message : '物料群組儲存失敗。' }
  finally { busy.value = false }
}
async function deleteGroups(): Promise<void> {
  const targets = checkedGroupNos.value.length ? checkedGroupNos.value : activeGroupNo.value ? [activeGroupNo.value] : []
  const rows = targets.map(key => groups.value.find(row => row.groupNo === key)).filter((row): row is MaterialGroup => Boolean(row))
  if (!rows.length || busy.value) return
  if (!window.confirm(`確定刪除選取的 ${rows.length} 筆物料群組資料？其群組成員也會一併刪除。`)) return
  busy.value = true; errorMessage.value = ''; notice.value = ''
  let completed = 0
  try {
    for (const row of rows) {
      if (mock.value) {
        const store = readStore()
        store.groups = store.groups.filter(item => item.groupNo !== row.groupNo)
        delete store.members[row.groupNo]
        writeStore(store)
      } else await request<void>(`${groupApi}/${enc(row.groupNo)}?${stampQuery(row.dataStamp)}`, { method: 'DELETE' })
      completed++
    }
    await loadGroups()
    notice.value = `已刪除 ${completed} 筆物料群組資料。`
  } catch (error) {
    await loadGroups()
    errorMessage.value = `${error instanceof Error ? error.message : '物料群組刪除失敗。'} 已完成 ${completed}/${rows.length} 筆。`
  } finally { busy.value = false }
}
async function saveMembers(): Promise<void> {
  const row = activeGroup.value
  if (!row || busy.value) return
  busy.value = true; errorMessage.value = ''; notice.value = ''
  try {
    if (mock.value) {
      const store = readStore()
      store.members[row.groupNo] = assigned.value.slice().sort((a, b) => a.localeCompare(b))
      writeStore(store)
    } else await request<void>(`${groupApi}/${enc(row.groupNo)}/materials`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ materialNos: assigned.value }) })
    notice.value = '物料群組成員設定成功。'
    await loadDetails(row)
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '物料群組成員儲存失敗。' }
  finally { busy.value = false }
}
function onTypePicked(row: MaterialType): void { editor.value.materialType = row.materialType; typePickerOpen.value = false; typeQuery.value = '' }

async function initialLoad(): Promise<void> {
  if (mock.value) {
    const store = readStore()
    if (!window.localStorage.getItem(groupStorageKey)) writeStore(store)
    materials.value = readMaterials()
    types.value = []
  }
  try { await loadTypes() } catch (error) { errorMessage.value = error instanceof Error ? error.message : '物料類別選項載入失敗。' }
  await loadGroups()
}
onMounted(() => { void initialLoad() })
</script>

<template>
  <section class="material-group-page">
    <header class="page-title"><h1>物料群組資料</h1></header>
    <div class="search-strip">
      <label class="query-field"><span>物料群組編號</span><input v-model="groupQuery" class="kd-input" maxlength="50" @keyup.enter="loadGroups()"></label>
      <button class="win-btn" :disabled="loading" @click="loadGroups()">{{ loading ? '查詢中' : '查詢' }}</button>
    </div>
    <p v-if="errorMessage" class="message error">{{ errorMessage }}</p>
    <p v-if="notice" class="message notice">{{ notice }}</p>

    <main class="master-area">
      <div class="grid-panel">
        <div class="grid-scroll">
          <table>
            <thead><tr><th class="check"></th><th>物料群組編號</th><th>物料類別</th><th>說明</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th></tr></thead>
            <tbody>
              <tr v-for="row in visibleGroups" :key="row.groupNo" :class="{ active: activeGroupNo === row.groupNo, selected: checkedGroupNos.includes(row.groupNo) }" @click="selectGroup(row)" @dblclick="openEditor('edit')">
                <td class="check"><input type="checkbox" :checked="checkedGroupNos.includes(row.groupNo)" @click.stop="activeGroupNo = row.groupNo" @change="toggleGroup(row, ($event.target as HTMLInputElement).checked)"></td>
                <td>{{ row.groupNo }}</td><td>{{ row.materialType }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ dateText(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ dateText(row.reviseDate) }}</td>
              </tr>
              <tr v-if="!loading && !visibleGroups.length"><td colspan="8" class="empty">沒有符合條件的物料群組資料</td></tr>
            </tbody>
          </table>
        </div>
        <aside class="actions">
          <button class="win-btn primary" :disabled="busy" @click="openEditor('add')">新增(A)</button>
          <button class="win-btn" :disabled="busy || !activeGroup" @click="openEditor('edit')">修改(E)</button>
          <button class="win-btn danger" :disabled="busy || !activeGroup" @click="deleteGroups">刪除(D)</button>
          <button class="win-btn" :disabled="busy || !activeGroup" @click="openEditor('copy')">複製(C)</button>
          <button class="win-btn close-action" :disabled="busy" @click="emit('close')">關閉</button>
        </aside>
      </div>
      <section class="membership-panel">
        <div class="membership-heading"><strong>群組物料編號</strong><span v-if="activeGroup">群組：{{ activeGroup.groupNo }}　物料類別：{{ activeGroup.materialType }}</span></div>
        <div class="lists-body" :class="{ waiting: !activeGroup || detailLoading }">
          <div class="list-column">
            <label class="list-title" for="available-search">UnSelectData</label>
            <input id="available-search" v-model="availableSearch" class="kd-input" placeholder="輸入物料編號，Enter 精確移入" :disabled="!activeGroup || detailLoading" @keydown="moveByExactSearch('available', $event)">
            <div class="item-list" @keydown.ctrl.a.prevent="availableSelection = visibleAvailable.slice()">
              <label v-for="no in visibleAvailable" :key="no" class="item-option" @dblclick.prevent="availableSelection = [no]; moveItems('available')"><input v-model="availableSelection" type="checkbox" :value="no">{{ no }}</label>
              <p v-if="!detailLoading && !visibleAvailable.length" class="list-empty">{{ activeGroup ? '沒有可選的已核准物料' : '請先選取物料群組' }}</p>
            </div>
          </div>
          <div class="move-actions"><button class="win-btn" title="將選取物料移入群組" :disabled="!availableSelection.length || busy" @click="moveItems('available')">▶</button><button class="win-btn" title="將選取物料移出群組" :disabled="!assignedSelection.length || busy" @click="moveItems('assigned')">◀</button></div>
          <div class="list-column">
            <label class="list-title" for="assigned-search">SelectData</label>
            <input id="assigned-search" v-model="assignedSearch" class="kd-input" placeholder="輸入物料編號，Enter 精確移出" :disabled="!activeGroup || detailLoading" @keydown="moveByExactSearch('assigned', $event)">
            <div class="item-list">
              <label v-for="no in visibleAssigned" :key="no" class="item-option" @dblclick.prevent="assignedSelection = [no]; moveItems('assigned')"><input v-model="assignedSelection" type="checkbox" :value="no">{{ no }}</label>
              <p v-if="!detailLoading && !visibleAssigned.length" class="list-empty">{{ activeGroup ? '目前沒有群組成員' : '請先選取物料群組' }}</p>
            </div>
          </div>
        </div>
        <footer class="membership-footer"><span>{{ detailLoading ? '載入明細中…' : `可選 ${available.length} 筆　已選 ${assigned.length} 筆` }}</span><button class="win-btn primary" :disabled="!activeGroup || busy || detailLoading" @click="saveMembers">確定</button></footer>
      </section>
    </main>
    <footer class="status-line"><span>資料總筆數：{{ visibleGroups.length }}</span><span>目前選取：{{ activeGroup?.groupNo ?? '—' }}</span></footer>

    <div v-if="editorMode" class="backdrop" @click.self="closeEditor">
      <form class="editor" @submit.prevent="saveGroup(true)">
        <header class="dialog-title"><h2>{{ editorTitle }}</h2><button type="button" class="x-button" @click="closeEditor">×</button></header>
        <fieldset class="fields">
          <legend>物料群組主檔資料</legend>
          <div class="identity-row">
            <label><span>物料群組編號</span><input v-model="editor.groupNo" class="kd-input" maxlength="50" :readonly="editorMode === 'edit'" @blur="editor.groupNo = editor.groupNo.trim().toUpperCase()"></label>
            <label><span>物料類別</span><div class="lookup-field"><input :value="editor.materialType" class="kd-input" readonly><button type="button" class="win-btn lookup-button" :disabled="editorMode === 'edit'" @click="typePickerOpen = true">…</button></div></label>
          </div>
          <label class="description-field"><span>說明</span><textarea v-model="editor.description" class="kd-input" maxlength="255"></textarea></label>
        </fieldset>
        <fieldset class="audit">
          <legend>建立 / 修改資訊</legend>
          <label>建立人<input :value="audit.creator" readonly></label><label>建立日期<input :value="dateText(audit.createDate)" readonly></label>
          <label>修改人<input :value="audit.reviser" readonly></label><label>修改日期<input :value="dateText(audit.reviseDate)" readonly></label>
        </fieldset>
        <p v-if="editorError" class="message error">{{ editorError }}</p>
        <footer class="dialog-actions"><button class="win-btn primary" :disabled="busy">確定</button><button type="button" class="win-btn" :disabled="busy" @click="closeEditor">關閉</button><button type="button" class="win-btn" :disabled="busy || !hasEditorChanges" @click="saveGroup(false)">套用</button></footer>
      </form>
    </div>
    <div v-if="typePickerOpen" class="backdrop picker-backdrop" @click.self="typePickerOpen = false">
      <section class="editor type-picker"><header class="dialog-title"><h2>選擇物料類別</h2><button class="x-button" @click="typePickerOpen = false">×</button></header>
        <label class="picker-search"><span>物料類別</span><input v-model="typeQuery" class="kd-input" autofocus></label>
        <div class="type-grid"><button v-for="row in availableTypes" :key="row.materialType" type="button" @click="onTypePicked(row)"><span>{{ row.materialType }}</span><span>{{ row.materialTypeName ?? '' }}</span></button><p v-if="!availableTypes.length" class="empty">沒有可選的物料類別</p></div>
        <footer class="dialog-actions"><button type="button" class="win-btn" @click="typePickerOpen = false">關閉</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.material-group-page{height:100%;min-height:0;display:flex;flex-direction:column;background:#fff;color:#292929;font:14px "Microsoft JhengHei",Arial,sans-serif}.page-title{height:50px;flex:none;display:flex;align-items:center;padding:0 24px;border-bottom:1px solid #ddd8ce;background:#f7f5ef}.page-title h1{margin:0;font-size:20px;font-weight:500}.search-strip{min-height:42px;flex:none;display:flex;align-items:center;gap:10px;padding:5px 12px;border-bottom:1px solid #ccc}.query-field{display:flex;align-items:center;gap:7px}.kd-input{box-sizing:border-box;min-width:0;height:27px;border:1px solid #999;border-radius:0;padding:3px 6px;background:#ffffc0;color:#222;font:inherit}.kd-input:read-only,.kd-input:disabled{background:#eee}.win-btn{min-height:29px;padding:3px 10px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#f1efeb);font:inherit;white-space:nowrap;cursor:pointer}.win-btn:hover:not(:disabled){border-color:#e39a37;background:#fff8e8}.win-btn:disabled{opacity:.5;cursor:default}.win-btn.primary{border-color:#e69520;background:#f7a52a;color:#fff;font-weight:600}.win-btn.danger{color:#8b3030}.message{margin:4px 12px;padding:6px 9px;border:1px solid #ddd;background:#fff9e7}.message.error{color:#8d2222;background:#fff3f1}.message.notice{color:#4d6128;background:#f5f8eb}.master-area{flex:1;min-height:0;display:flex;flex-direction:column;padding:8px 12px 4px;gap:8px}.grid-panel{height:42%;min-height:170px;display:grid;grid-template-columns:minmax(0,1fr) 92px;gap:8px}.grid-scroll{min-width:0;min-height:0;overflow:auto;border:1px solid #aaa;background:#fff}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}th,td{height:24px;padding:3px 6px;border:1px solid #c9c9c9;text-align:left}th{position:sticky;top:0;z-index:1;background:#e7e5df;font-weight:500}tbody tr{cursor:pointer}tbody tr:hover{background:#fff5d8}tbody tr.active{background:#ffefbf;outline:1px solid #d5a13d;outline-offset:-1px}tbody tr.selected:not(.active){background:#f8f3e6}.check{width:25px;text-align:center;padding:2px}.check input,.item-option input{accent-color:#e4a02f}.empty{text-align:center;color:#777;padding:18px}.actions{display:flex;flex-direction:column;gap:6px}.close-action{margin-top:auto}.membership-panel{flex:1;min-height:190px;display:flex;flex-direction:column;border:1px solid #aaa;padding:7px 8px 6px}.membership-heading{display:flex;align-items:center;justify-content:space-between;padding:0 2px 5px;border-bottom:1px solid #ddd;color:#333}.membership-heading span{font-size:12px;color:#666}.lists-body{flex:1;min-height:0;display:grid;grid-template-columns:minmax(0,1fr) 58px minmax(0,1fr);gap:9px;padding-top:6px}.lists-body.waiting{opacity:.78}.list-column{min-height:0;display:flex;flex-direction:column;gap:5px}.list-title{color:#555;font-size:12px}.list-column>.kd-input{background:#fff}.item-list{flex:1;min-height:0;overflow:auto;border:1px solid #aaa;background:white;padding:3px}.item-option{display:flex;align-items:center;gap:6px;min-height:23px;padding:1px 4px;cursor:pointer;font:12px "Microsoft JhengHei",Arial,sans-serif}.item-option:hover{background:#fff4d2}.list-empty{margin:12px 4px;color:#888;text-align:center;font-size:12px}.move-actions{display:flex;flex-direction:column;justify-content:center;gap:8px}.move-actions button{padding:4px 6px}.membership-footer{height:33px;flex:none;display:flex;align-items:center;justify-content:space-between;border-top:1px solid #ddd;margin-top:5px;padding-top:5px;color:#777;font-size:12px}.status-line{height:30px;flex:none;display:flex;align-items:center;justify-content:space-between;padding:0 12px;border-top:1px solid #ccc;color:#666;font-size:12px}.backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:10px;background:#0005}.editor{box-sizing:border-box;width:min(523px,96vw);min-height:276px;display:flex;flex-direction:column;padding:0 10px 7px;border:1px solid #999;background:#f9f8f4;box-shadow:0 8px 28px #0004}.dialog-title{height:36px;flex:none;display:flex;align-items:center;justify-content:space-between;margin:0 -10px 5px;padding:0 10px;border-bottom:1px solid #ccc;background:#f1efe9}.dialog-title h2{margin:0;font-size:15px;font-weight:500}.x-button{border:0;background:transparent;color:#555;font-size:24px;line-height:1;cursor:pointer}.fields{display:flex;flex-direction:column;gap:8px;margin:6px 0;padding:7px;border:1px solid #aaa}.fields legend,.audit legend{padding:0 5px;color:#555}.fields label{display:grid;grid-template-columns:125px minmax(0,1fr);align-items:center;gap:7px}.fields label>span{text-align:right}.lookup-field{display:flex;gap:5px}.lookup-field .kd-input{flex:1}.lookup-button{width:30px;padding:0}.description-field{align-items:start!important}.description-field textarea{height:62px;resize:vertical}.audit{display:grid;grid-template-columns:1fr 1fr;gap:5px 10px;margin:0;padding:5px 7px;border:1px solid #aaa}.audit label{display:grid;grid-template-columns:58px minmax(0,1fr);align-items:center;gap:5px;font-size:11px;white-space:nowrap}.audit input{box-sizing:border-box;width:100%;min-width:0;height:22px;border:1px solid #aaa;background:#eee;padding:2px 5px;font:inherit}.dialog-actions{display:flex;justify-content:flex-end;gap:7px;padding:7px 0 1px}.picker-backdrop{z-index:1002}.type-picker{height:min(390px,90vh);min-height:0}.picker-search{display:flex;align-items:center;gap:8px;margin:5px 2px 8px}.picker-search span{white-space:nowrap}.picker-search input{flex:1}.type-grid{flex:1;min-height:0;overflow:auto;border:1px solid #aaa;background:#fff}.type-grid button{display:grid;grid-template-columns:150px 1fr;width:100%;padding:6px 8px;border:0;border-bottom:1px solid #ddd;background:white;text-align:left;font:inherit;cursor:pointer}.type-grid button:hover{background:#fff1c7}.type-grid p{margin:0}@media(max-width:760px){.grid-panel{grid-template-columns:minmax(0,1fr) 78px}.actions{gap:4px}.actions .win-btn{padding:3px 5px;font-size:12px}.membership-heading{align-items:flex-start;gap:4px;flex-direction:column}.lists-body{grid-template-columns:minmax(0,1fr) 42px minmax(0,1fr);gap:5px}.item-option{font-size:11px}.query-field{flex:1;min-width:260px}.master-area{padding:6px}}
.identity-row{display:grid;grid-template-columns:1fr 1fr;gap:10px}.identity-row label{grid-template-columns:76px minmax(0,1fr)}.description-field{width:70%}@media(max-width:760px){.identity-row{grid-template-columns:1fr}.description-field{width:100%}}
</style>
