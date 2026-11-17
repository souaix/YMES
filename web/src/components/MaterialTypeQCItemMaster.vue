<script setup lang="ts">
import { computed, ref } from 'vue'

type ItemKind = 0 | 1 | 2
interface MaterialType { materialType: string; description?: string | null; creator?: string | null; createDate?: string | null }
interface QcItem { qcItemNo: string; qcItemName: string; qcItemType: ItemKind; reviser?: string | null; reviseDate?: string | null }
interface CatalogItem { qcItemNo: string; qcItemName: string }
interface Bundle { attributes: CatalogItem[]; variables: CatalogItem[]; groups: CatalogItem[]; assigned: QcItem[] }
interface MockStore { assignments: Record<string, QcItem[]> }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const useMock = computed(() => import.meta.env.DEV && props.developmentPreview)
const rootApi = 'api/material-types'
const assignmentStorageKey = 'ymes-dev-mock:mtl-type-qc-item:v1'
const materialTypeStorageKey = 'ymes-dev-mock:mtl-material-type:v1'
const kindLabels: Record<ItemKind, string> = { 0: 'Attributes', 1: 'Variables', 2: 'Group' }
const seedTypes: MaterialType[] = [
  { materialType: 'DEV-RESIN', description: '一般物料類別假資料', creator: 'DEV001', createDate: '2026-11-02T08:00:00+08:00' },
  { materialType: 'DEV-GUM-ADHESIVE', description: '固晶膠時效設定示例', creator: 'DEV001', createDate: '2026-11-03T08:00:00+08:00' },
  { materialType: 'DEV-PACK', description: '包裝物料', creator: 'DEV001', createDate: '2026-11-05T08:00:00+08:00' },
]
const catalogs: Record<ItemKind, CatalogItem[]> = {
  0: [
    { qcItemNo: 'DEV-ATTR-001', qcItemName: '物料外觀（DEV）' },
    { qcItemNo: 'DEV-ATTR-002', qcItemName: '包裝檢查（DEV）' },
    { qcItemNo: 'DEV-ATTR-003', qcItemName: '標籤內容（DEV）' },
  ],
  1: [
    { qcItemNo: 'DEV-VAR-001', qcItemName: '物料尺寸（DEV）' },
    { qcItemNo: 'DEV-VAR-002', qcItemName: '黏度（DEV）' },
    { qcItemNo: 'DEV-VAR-003', qcItemName: '含水率（DEV）' },
  ],
  2: [
    { qcItemNo: 'DEV-GROUP-001', qcItemName: '進料檢驗組（DEV）' },
    { qcItemNo: 'DEV-GROUP-002', qcItemName: '包裝檢驗組（DEV）' },
  ],
}
const seedAssignments: Record<string, QcItem[]> = {
  'DEV-RESIN': [
    { qcItemNo: 'DEV-ATTR-001', qcItemName: '物料外觀（DEV）', qcItemType: 0, reviser: 'DEV001', reviseDate: '2026-11-02T08:10:00+08:00' },
    { qcItemNo: 'DEV-VAR-001', qcItemName: '物料尺寸（DEV）', qcItemType: 1, reviser: 'DEV001', reviseDate: '2026-11-02T08:10:00+08:00' },
  ],
  'DEV-GUM-ADHESIVE': [{ qcItemNo: 'DEV-GROUP-001', qcItemName: '進料檢驗組（DEV）', qcItemType: 2, reviser: 'DEV001', reviseDate: '2026-11-03T08:10:00+08:00' }],
  'DEV-PACK': [],
}
const kinds: ItemKind[] = [0, 1, 2]

const types = ref<MaterialType[]>([])
const queryText = ref('')
const selectedTypeNo = ref('')
const loadedTypeNo = ref('')
const catalogsByKind = ref<Record<ItemKind, CatalogItem[]>>({ 0: [], 1: [], 2: [] })
const assignedItems = ref<QcItem[]>([])
const availableSelection = ref<Record<ItemKind, string[]>>({ 0: [], 1: [], 2: [] })
const assignedSelection = ref<string[]>([])
const loading = ref(false)
const saving = ref(false)
const notice = ref('')
const error = ref('')
const activeType = computed(() => types.value.find(row => row.materialType === selectedTypeNo.value) ?? null)
const selectedCodes = computed(() => new Set(assignedItems.value.map(row => row.qcItemNo)))
const assignedByKind = computed(() => ({
  0: assignedItems.value.filter(row => row.qcItemType === 0).sort((a, b) => a.qcItemNo.localeCompare(b.qcItemNo)),
  1: assignedItems.value.filter(row => row.qcItemType === 1).sort((a, b) => a.qcItemNo.localeCompare(b.qcItemNo)),
  2: assignedItems.value.filter(row => row.qcItemType === 2).sort((a, b) => a.qcItemNo.localeCompare(b.qcItemNo)),
}))
const availableRows = computed(() => ({
  0: catalogsByKind.value[0].filter(row => !selectedCodes.value.has(row.qcItemNo)),
  1: catalogsByKind.value[1].filter(row => !selectedCodes.value.has(row.qcItemNo)),
  2: catalogsByKind.value[2].filter(row => !selectedCodes.value.has(row.qcItemNo)),
}))

function enc(value: string): string { return encodeURIComponent(value) }
function now(): string { return new Date().toISOString() }
function dateText(value?: string | null): string {
  if (!value) return ''
  const parsed = new Date(value)
  return Number.isNaN(parsed.valueOf()) ? value : new Intl.DateTimeFormat('zh-TW', { dateStyle: 'short', timeStyle: 'medium', hour12: false }).format(parsed)
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
    if (response.status === 404) throw new Error('物料類別 QC Item API 尚未建立；請依 todolist_homepc.md 完成 MTL ML105。')
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 回應錯誤 (${response.status})。`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function readMockAssignments(): MockStore {
  try {
    const raw = window.localStorage.getItem(assignmentStorageKey)
    if (!raw) return { assignments: structuredClone(seedAssignments) }
    const parsed = JSON.parse(raw) as Partial<MockStore>
    return { assignments: parsed.assignments ?? structuredClone(seedAssignments) }
  } catch { return { assignments: structuredClone(seedAssignments) } }
}
function writeMockAssignments(store: MockStore): void { window.localStorage.setItem(assignmentStorageKey, JSON.stringify(store)) }
function loadMockTypes(prefix: string): MaterialType[] {
  try {
    const raw = window.localStorage.getItem(materialTypeStorageKey)
    const parsed = raw ? JSON.parse(raw) as { types?: MaterialType[] } : null
    return (parsed?.types ?? seedTypes).filter(row => row.materialType.toLocaleUpperCase().startsWith(prefix.toLocaleUpperCase())).sort((a, b) => a.materialType.localeCompare(b.materialType))
  } catch { return seedTypes.filter(row => row.materialType.toLocaleUpperCase().startsWith(prefix.toLocaleUpperCase())) }
}

async function queryTypes(): Promise<void> {
  loading.value = true; error.value = ''; notice.value = ''; selectedTypeNo.value = ''; assignedItems.value = []
  loadedTypeNo.value = ''
  try {
    const prefix = queryText.value.trim()
    types.value = useMock.value
      ? loadMockTypes(prefix)
      : await request<MaterialType[]>(`${rootApi}?materialType=${enc(prefix)}`)
    if (!types.value.length) { catalogsByKind.value = { 0: [], 1: [], 2: [] }; return }
    await selectType(types.value[0].materialType)
  } catch (reason) { types.value = []; error.value = reason instanceof Error ? reason.message : '物料類別查詢失敗。' }
  finally { loading.value = false }
}

async function selectType(materialType: string): Promise<void> {
  if (selectedTypeNo.value === materialType && loadedTypeNo.value === materialType) return
  selectedTypeNo.value = materialType; loadedTypeNo.value = ''; error.value = ''; notice.value = ''
  availableSelection.value = { 0: [], 1: [], 2: [] }; assignedSelection.value = []
  try {
    let bundle: Bundle
    if (useMock.value) {
      const store = readMockAssignments()
      bundle = { attributes: catalogs[0], variables: catalogs[1], groups: catalogs[2], assigned: structuredClone(store.assignments[materialType] ?? []) }
    } else bundle = await request<Bundle>(`${rootApi}/${enc(materialType)}/qc-items`)
    catalogsByKind.value = { 0: bundle.attributes, 1: bundle.variables, 2: bundle.groups }
    assignedItems.value = bundle.assigned.map(row => ({ ...row }))
    loadedTypeNo.value = materialType
  } catch (reason) { catalogsByKind.value = { 0: [], 1: [], 2: [] }; assignedItems.value = []; error.value = reason instanceof Error ? reason.message : 'QC Item 載入失敗。' }
}

function toggleAvailable(kind: ItemKind, code: string, checked: boolean): void {
  const current = availableSelection.value[kind]
  availableSelection.value[kind] = checked ? [...current, code] : current.filter(item => item !== code)
}
function addItems(kind: ItemKind): void {
  const selected = new Set(availableSelection.value[kind])
  const entries = catalogsByKind.value[kind].filter(row => selected.has(row.qcItemNo) && !selectedCodes.value.has(row.qcItemNo))
  assignedItems.value = [...assignedItems.value, ...entries.map(row => ({ ...row, qcItemType: kind }))]
  availableSelection.value[kind] = []; error.value = ''
}
function removeItems(): void {
  const selected = new Set(assignedSelection.value)
  assignedItems.value = assignedItems.value.filter(row => !selected.has(row.qcItemNo))
  assignedSelection.value = []; error.value = ''
}
function toggleAssigned(code: string, checked: boolean): void {
  assignedSelection.value = checked ? [...assignedSelection.value, code] : assignedSelection.value.filter(item => item !== code)
}
function updateKind(row: QcItem, kind: ItemKind): void {
  if (row.qcItemType !== 0) row.qcItemType = kind
}

async function save(): Promise<void> {
  const materialType = activeType.value?.materialType
  if (!materialType || loadedTypeNo.value !== materialType || saving.value) return
  if (!window.confirm(`確定儲存「${materialType}」的 QC Item？此設定會套用至此類別的所有物料。`)) return
  saving.value = true; error.value = ''; notice.value = ''
  const items = assignedItems.value.map(row => ({ qcItemNo: row.qcItemNo, qcItemType: row.qcItemType }))
  try {
    if (useMock.value) {
      const store = readMockAssignments()
      store.assignments[materialType] = assignedItems.value.map(row => ({ ...row, reviser: props.currentUserNo || 'DEV001', reviseDate: now() }))
      writeMockAssignments(store)
      notice.value = `DEV 假資料已儲存，共 ${items.length} 個 QC Item。`
    } else {
      await request(`${rootApi}/${enc(materialType)}/qc-items`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ items }) })
      const bundle = await request<Bundle>(`${rootApi}/${enc(materialType)}/qc-items`)
      assignedItems.value = bundle.assigned
      notice.value = 'QC Item 已儲存，並已更新此類別下的物料。'
    }
  } catch (reason) { error.value = reason instanceof Error ? reason.message : 'QC Item 儲存失敗。' }
  finally { saving.value = false }
}
</script>

<template>
  <section class="mtl-type-qc-page">
    <header class="page-title"><h1>物料類別品管項目設定</h1></header>
    <fieldset class="type-search">
      <legend>MaterialType</legend>
      <label for="type-prefix">物料類別</label>
      <input id="type-prefix" v-model="queryText" class="kd-input" maxlength="50" placeholder="輸入類別編號前綴" @keydown.enter.prevent="queryTypes">
      <button class="win-btn" type="button" :disabled="loading" @click="queryTypes">查詢</button>
      <span class="search-hint">可輸入前綴；留白查詢全部</span>
    </fieldset>

    <div class="type-grid-wrap">
      <table class="type-grid">
        <thead><tr><th>物料類別</th><th>說明</th><th>建立人</th><th>建立日期</th></tr></thead>
        <tbody>
          <tr v-for="row in types" :key="row.materialType" :class="{ selected: row.materialType === selectedTypeNo }" @click="selectType(row.materialType)">
            <td>{{ row.materialType }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ dateText(row.createDate) }}</td>
          </tr>
          <tr v-if="!types.length"><td colspan="4" class="empty">{{ loading ? '查詢中…' : '請輸入條件並按「查詢」載入物料類別。' }}</td></tr>
        </tbody>
      </table>
    </div>

    <div class="workspace">
      <div class="catalogs">
        <section v-for="kind in kinds" :key="kind" class="catalog-panel">
          <header><h2>MTL QCItem for {{ kindLabels[kind] }}</h2><span>{{ availableRows[kind].length }} 筆</span></header>
          <div class="catalog-scroll">
            <table><thead><tr><th class="check-col"></th><th>QCItemNo</th><th>QCItemName</th></tr></thead>
              <tbody>
                <tr v-for="row in availableRows[kind]" :key="row.qcItemNo" @dblclick="toggleAvailable(kind,row.qcItemNo,true);addItems(kind)">
                  <td class="check-col"><input type="checkbox" :checked="availableSelection[kind].includes(row.qcItemNo)" :aria-label="`選取 ${row.qcItemNo}`" @change="toggleAvailable(kind,row.qcItemNo,($event.target as HTMLInputElement).checked)"></td>
                  <td>{{ row.qcItemNo }}</td><td>{{ row.qcItemName }}</td>
                </tr>
                <tr v-if="!availableRows[kind].length"><td colspan="3" class="empty">{{ activeType ? '沒有可加入的項目' : '請先查詢並選取物料類別' }}</td></tr>
              </tbody>
            </table>
          </div>
          <button class="win-btn add-button" type="button" :disabled="!availableSelection[kind].length" @click="addItems(kind)">加入 →</button>
        </section>
      </div>

      <section class="assigned-panel">
        <header><h2>MTLQCITEM<span v-if="activeType">：{{ activeType.materialType }}</span></h2><button class="win-btn remove-button" type="button" :disabled="!assignedSelection.length" @click="removeItems">← 移除</button><span>{{ assignedItems.length }} 筆</span></header>
        <div class="assigned-scroll">
          <table><thead><tr><th class="check-col"></th><th>QCItemNo</th><th>QCItemName</th><th>QCItemType</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
            <tbody>
              <template v-for="kind in kinds" :key="kind">
                <tr v-if="assignedByKind[kind].length" class="group-row"><td colspan="6">{{ kindLabels[kind] }}（{{ assignedByKind[kind].length }}）</td></tr>
                <tr v-for="row in assignedByKind[kind]" :key="row.qcItemNo" @dblclick="toggleAssigned(row.qcItemNo,true);removeItems()">
                  <td class="check-col"><input type="checkbox" :checked="assignedSelection.includes(row.qcItemNo)" :aria-label="`選取 ${row.qcItemNo}`" @change="toggleAssigned(row.qcItemNo,($event.target as HTMLInputElement).checked)"></td>
                  <td>{{ row.qcItemNo }}</td><td>{{ row.qcItemName }}</td>
                  <td><select :value="row.qcItemType" :disabled="row.qcItemType === 0" @click.stop @change="updateKind(row,Number(($event.target as HTMLSelectElement).value) as ItemKind)"><option v-for="kind in kinds" :key="kind" :value="kind">{{ kindLabels[kind] }}</option></select></td>
                  <td>{{ row.reviser }}</td><td>{{ dateText(row.reviseDate) }}</td>
                </tr>
              </template>
              <tr v-if="!assignedItems.length"><td colspan="6" class="empty">{{ activeType ? '尚未設定 QC Item' : '請先選取物料類別' }}</td></tr>
            </tbody>
          </table>
        </div>
      </section>
    </div>

    <p v-if="error" class="feedback error" role="alert">{{ error }}</p><p v-if="notice" class="feedback success" role="status">{{ notice }}</p>
    <footer class="page-footer"><span>{{ activeType ? `目前物料類別：${activeType.materialType}` : '請先查詢並選取物料類別' }}</span><div><button class="win-btn primary" type="button" :disabled="!activeType || loadedTypeNo !== selectedTypeNo || saving" @click="save">確定</button><button class="win-btn" type="button" @click="emit('close')">取消</button></div></footer>
  </section>
</template>

<style scoped>
.type-search{height:43px;margin:5px 10px 4px}.type-grid-wrap{height:96px;margin-bottom:6px}.remove-button{min-height:22px;padding:1px 8px}.workspace{grid-template-columns:minmax(0,1fr) minmax(0,1fr)}.transfer-actions{display:none}.group-row td{background:#efeee8;font-weight:600}.assigned-panel select{position:relative;z-index:2}@media(max-width:900px){.workspace{grid-template-columns:minmax(0,1fr) minmax(0,1fr)}}@media(max-width:680px){.type-search{height:auto;flex-wrap:wrap;padding:7px}.workspace{grid-template-columns:minmax(0,1fr);grid-template-rows:minmax(260px,1fr) minmax(220px,1fr)}.catalogs,.assigned-panel{grid-column:1}.assigned-panel{grid-row:2}}
</style>
