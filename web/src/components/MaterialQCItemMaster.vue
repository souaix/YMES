<script setup lang="ts">
import { computed, ref } from 'vue'

type ItemKind = 0 | 1 | 2
type PickerMode = 'material-type' | 'material' | null
interface MaterialRow { materialNo: string; materialType: string; materialName: string }
interface QcItem { qcItemNo: string; qcItemName: string; qcItemType: ItemKind; reviser?: string | null; reviseDate?: string | null }
interface Bundle { available: QcItem[]; assigned: QcItem[] }
interface MaterialTypeRow { materialType: string; materialTypeName?: string | null; description?: string | null }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }> ()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const useMock = computed(() => import.meta.env.DEV && props.developmentPreview)
const mockMaterialStorage = 'ymes:dev:master-data:materials:v1'
const mockTypeStorage = 'ymes-dev-mock:mtl-material-type:v1'
const typeQcStorage = (materialNo: string) => `ymes:dev:material-qc-items:${encodeURIComponent(materialNo)}:v1`
const kindLabels: Record<ItemKind, string> = { 0: 'Attributes', 1: 'Variables', 2: 'Group' }
const kinds: ItemKind[] = [0, 1, 2]
const seedMaterials: MaterialRow[] = [
  { materialNo: 'MAT-GUM-001', materialType: 'GUM', materialName: '固晶膠 A（DEV）' },
  { materialNo: 'MAT-CHEM-002', materialType: 'CHEM', materialName: '清洗液 B（DEV）' },
  { materialNo: 'MAT-PACK-003', materialType: 'PACK', materialName: '包裝盒 C（DEV）' },
  { materialNo: 'MAT-WIP-004', materialType: 'CHEM', materialName: '研發試料 D（DEV）' },
]
const qcCatalog: QcItem[] = [
  { qcItemNo: 'DEV-ATTR-001', qcItemName: '物料外觀（DEV）', qcItemType: 0 },
  { qcItemNo: 'DEV-ATTR-002', qcItemName: '包裝檢查（DEV）', qcItemType: 0 },
  { qcItemNo: 'DEV-ATTR-003', qcItemName: '標籤內容（DEV）', qcItemType: 0 },
  { qcItemNo: 'DEV-VAR-001', qcItemName: '物料尺寸（DEV）', qcItemType: 1 },
  { qcItemNo: 'DEV-VAR-002', qcItemName: '黏度（DEV）', qcItemType: 1 },
  { qcItemNo: 'DEV-VAR-003', qcItemName: '含水率（DEV）', qcItemType: 1 },
  { qcItemNo: 'DEV-GROUP-001', qcItemName: '進料檢驗組（DEV）', qcItemType: 2 },
  { qcItemNo: 'DEV-GROUP-002', qcItemName: '包裝檢驗組（DEV）', qcItemType: 2 },
]
const seedAssigned: Record<string, QcItem[]> = {
  'MAT-GUM-001': [
    { qcItemNo: 'DEV-ATTR-001', qcItemName: '物料外觀（DEV）', qcItemType: 0, reviser: 'DEV001', reviseDate: '2026-11-02T08:10:00+08:00' },
    { qcItemNo: 'DEV-VAR-001', qcItemName: '物料尺寸（DEV）', qcItemType: 1, reviser: 'DEV001', reviseDate: '2026-11-02T08:10:00+08:00' },
  ],
  'MAT-CHEM-002': [],
  'MAT-PACK-003': [{ qcItemNo: 'DEV-GROUP-001', qcItemName: '進料檢驗組（DEV）', qcItemType: 2, reviser: 'DEV001', reviseDate: '2026-11-03T08:10:00+08:00' }],
  'MAT-WIP-004': [],
}

const materialType = ref('')
const materialNoPrefix = ref('')
const rows = ref<MaterialRow[]>([])
const selectedMaterialNo = ref('')
const availableItems = ref<QcItem[]>([])
const assignedItems = ref<QcItem[]>([])
const availableSelection = ref<Record<ItemKind, string[]>>({ 0: [], 1: [], 2: [] })
const assignedSelection = ref<string[]>([])
const typeOptions = ref<MaterialTypeRow[]>([])
const materialOptions = ref<MaterialRow[]>([])
const pickerMode = ref<PickerMode>(null)
const pickerQuery = ref('')
const loading = ref(false)
const detailLoading = ref(false)
const loadedMaterialNo = ref('')
let selectionRequestId = 0
const saving = ref(false)
const error = ref('')
const notice = ref('')
const activeMaterial = computed(() => rows.value.find(row => row.materialNo === selectedMaterialNo.value) ?? null)
const availableCodes = computed(() => new Set(assignedItems.value.map(row => row.qcItemNo)))
const availableByKind = computed(() => ({
  0: availableItems.value.filter(row => row.qcItemType === 0 && !availableCodes.value.has(row.qcItemNo)).sort((a, b) => a.qcItemNo.localeCompare(b.qcItemNo)),
  1: availableItems.value.filter(row => row.qcItemType === 1 && !availableCodes.value.has(row.qcItemNo)).sort((a, b) => a.qcItemNo.localeCompare(b.qcItemNo)),
  2: availableItems.value.filter(row => row.qcItemType === 2 && !availableCodes.value.has(row.qcItemNo)).sort((a, b) => a.qcItemNo.localeCompare(b.qcItemNo)),
}))
const assignedByKind = computed(() => ({
  0: assignedItems.value.filter(row => row.qcItemType === 0).sort((a, b) => a.qcItemNo.localeCompare(b.qcItemNo)),
  1: assignedItems.value.filter(row => row.qcItemType === 1).sort((a, b) => a.qcItemNo.localeCompare(b.qcItemNo)),
  2: assignedItems.value.filter(row => row.qcItemType === 2).sort((a, b) => a.qcItemNo.localeCompare(b.qcItemNo)),
}))
const filteredTypeOptions = computed(() => typeOptions.value.filter(row => !pickerQuery.value || row.materialType.toLocaleLowerCase().includes(pickerQuery.value.toLocaleLowerCase())))
const filteredMaterialOptions = computed(() => materialOptions.value.filter(row => !pickerQuery.value || [row.materialNo, row.materialName, row.materialType].some(value => value.toLocaleLowerCase().includes(pickerQuery.value.toLocaleLowerCase()))))

function enc(value: string): string { return encodeURIComponent(value) }
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
    if (response.status === 404) throw new Error('物料 QC Item API 尚未建立；請依 todolist_homepc.md 完成 MTL ML106。')
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 回應錯誤 (${response.status})。`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function readAssigned(materialNo: string): QcItem[] {
  try { const raw = window.localStorage.getItem(typeQcStorage(materialNo)); return raw ? JSON.parse(raw) as QcItem[] : structuredClone(seedAssigned[materialNo] ?? []) }
  catch { return structuredClone(seedAssigned[materialNo] ?? []) }
}
function mockMaterials(): MaterialRow[] {
  try { const raw = window.localStorage.getItem(mockMaterialStorage); const parsed = raw ? JSON.parse(raw) as Array<Record<string, unknown>> : null; return parsed ? parsed.map(row => ({ materialNo: String(row.materialNo ?? ''), materialType: String(row.materialType ?? ''), materialName: String(row.materialName ?? '') })) : seedMaterials }
  catch { return seedMaterials }
}
function mockTypes(): MaterialTypeRow[] {
  const options: MaterialTypeRow[] = []
  try { const raw = window.localStorage.getItem(mockTypeStorage); const parsed = raw ? JSON.parse(raw) as { types?: MaterialTypeRow[] } : null; options.push(...(parsed?.types ?? [])) } catch { /* include types present on DEV materials below */ }
  const byNo = new Map<string, MaterialTypeRow>()
  for (const row of options) byNo.set(row.materialType, row)
  for (const row of mockMaterials()) if (!byNo.has(row.materialType)) byNo.set(row.materialType, { materialType: row.materialType })
  return [...byNo.values()]
}

async function loadMaterials(): Promise<MaterialRow[]> {
  if (useMock.value) return mockMaterials().filter(row => (!materialType.value.trim() || row.materialType.toLocaleLowerCase() === materialType.value.trim().toLocaleLowerCase()) && (!materialNoPrefix.value.trim() || row.materialNo.toLocaleLowerCase().startsWith(materialNoPrefix.value.trim().toLocaleLowerCase())))
  const params = new URLSearchParams()
  if (materialType.value.trim()) params.set('materialType', materialType.value.trim())
  if (materialNoPrefix.value.trim()) params.set('materialNoPrefix', materialNoPrefix.value.trim())
  return await request<MaterialRow[]>(`api/materials?${params}`)
}
async function queryMaterials(): Promise<void> {
  error.value = ''; notice.value = ''; selectedMaterialNo.value = ''; loadedMaterialNo.value = ''; selectionRequestId++; rows.value = []; assignedItems.value = []; availableItems.value = []
  if (!materialType.value.trim() && !materialNoPrefix.value.trim()) { error.value = '請輸入物料類別或物料編號條件。'; return }
  loading.value = true
  try { rows.value = await loadMaterials(); if (rows.value.length) await selectMaterial(rows.value[0].materialNo) }
  catch (reason) { rows.value = []; error.value = reason instanceof Error ? reason.message : '物料查詢失敗。' }
  finally { loading.value = false }
}
async function selectMaterial(materialNo: string): Promise<void> {
  if (selectedMaterialNo.value === materialNo && loadedMaterialNo.value === materialNo) return
  selectedMaterialNo.value = materialNo; loadedMaterialNo.value = ''; error.value = ''; notice.value = ''; availableItems.value = []; assignedItems.value = []; availableSelection.value = { 0: [], 1: [], 2: [] }; assignedSelection.value = []
  const requestId = ++selectionRequestId
  detailLoading.value = true
  try {
    let bundle: Bundle
    if (useMock.value) bundle = { available: structuredClone(qcCatalog), assigned: readAssigned(materialNo) }
    else bundle = await request<Bundle>(`api/materials/${enc(materialNo)}/qc-item-library`)
    if (requestId !== selectionRequestId) return
    availableItems.value = bundle.available.map(row => ({ ...row }))
    assignedItems.value = bundle.assigned.map(row => ({ ...row }))
    loadedMaterialNo.value = materialNo
  } catch (reason) { if (requestId === selectionRequestId) { availableItems.value = []; assignedItems.value = []; error.value = reason instanceof Error ? reason.message : 'QC Item 載入失敗。' } }
  finally { if (requestId === selectionRequestId) detailLoading.value = false }
}
function toggleAvailable(kind: ItemKind, code: string, checked: boolean): void { const current = availableSelection.value[kind]; availableSelection.value[kind] = checked ? [...current, code] : current.filter(item => item !== code) }
function addItems(kind: ItemKind): void {
  const selected = new Set(availableSelection.value[kind]); const added = availableByKind.value[kind].filter(row => selected.has(row.qcItemNo))
  assignedItems.value = [...assignedItems.value, ...added.map(row => ({ qcItemNo: row.qcItemNo, qcItemName: row.qcItemName, qcItemType: kind }))]
  availableSelection.value[kind] = []; error.value = ''
}
function toggleAssigned(code: string, checked: boolean): void { assignedSelection.value = checked ? [...assignedSelection.value, code] : assignedSelection.value.filter(item => item !== code) }
function removeItems(): void { const selected = new Set(assignedSelection.value); assignedItems.value = assignedItems.value.filter(row => !selected.has(row.qcItemNo)); assignedSelection.value = []; error.value = '' }
function dateText(value?: string | null): string { if (!value) return ''; const date = new Date(value); return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat('zh-TW', { dateStyle: 'short', timeStyle: 'medium', hour12: false }).format(date) }

async function save(): Promise<void> {
  const materialNo = activeMaterial.value?.materialNo
  if (!materialNo || loadedMaterialNo.value !== materialNo || saving.value) return
  saving.value = true; error.value = ''; notice.value = ''
  const items = assignedItems.value.map(row => ({ qcItemNo: row.qcItemNo, qcItemType: row.qcItemType }))
  try {
    if (useMock.value) {
      const revised = assignedItems.value.map(row => ({ ...row, reviser: props.currentUserNo || 'DEV001', reviseDate: new Date().toISOString() }))
      window.localStorage.setItem(typeQcStorage(materialNo), JSON.stringify(revised)); assignedItems.value = revised
      notice.value = 'DEV 假資料已儲存。'
    } else {
      await request(`api/materials/${enc(materialNo)}/qc-items`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ items }) })
      const bundle = await request<Bundle>(`api/materials/${enc(materialNo)}/qc-item-library`)
      availableItems.value = bundle.available; assignedItems.value = bundle.assigned; notice.value = 'QC Item 已儲存。'
    }
  } catch (reason) { error.value = reason instanceof Error ? reason.message : 'QC Item 儲存失敗。' }
  finally { saving.value = false }
}

async function openPicker(mode: Exclude<PickerMode, null>): Promise<void> {
  pickerMode.value = mode; pickerQuery.value = ''; error.value = ''
  if (mode === 'material-type') {
    try { typeOptions.value = useMock.value ? mockTypes() : await request<MaterialTypeRow[]>('api/material-types') }
    catch (reason) { typeOptions.value = []; error.value = reason instanceof Error ? reason.message : '物料類別查詢失敗。' }
  } else {
    try { materialOptions.value = await loadMaterials() }
    catch (reason) { materialOptions.value = []; error.value = reason instanceof Error ? reason.message : '物料清單查詢失敗。' }
  }
}
function chooseType(row: MaterialTypeRow): void { materialType.value = row.materialType; pickerMode.value = null }
function chooseMaterial(row: MaterialRow): void { materialNoPrefix.value = row.materialNo; pickerMode.value = null }
</script>

<template>
  <section class="material-qc-page">
    <header class="page-title"><h1>物料品管項目設定</h1></header>
    <fieldset class="query-panel"><legend>Query</legend>
      <label for="mtl-type">MaterialType</label><input id="mtl-type" v-model="materialType" class="kd-input" maxlength="50" @keydown.enter.prevent="queryMaterials"><button class="win-btn lookup" type="button" aria-label="選擇物料類別" @click="openPicker('material-type')">…</button>
      <label for="mtl-no">MaterialNo</label><input id="mtl-no" v-model="materialNoPrefix" class="kd-input" maxlength="50" @keydown.enter.prevent="queryMaterials"><span class="percent">%</span><button class="win-btn lookup" type="button" aria-label="選擇物料" @click="openPicker('material')">…</button>
      <button class="win-btn query-button" type="button" :disabled="loading" @click="queryMaterials">Query</button>
    </fieldset>

    <section class="materials-panel"><header><h2>Material</h2><span>{{ rows.length }} 筆</span></header><div class="material-table-wrap"><table><thead><tr><th>MaterialType</th><th>MaterialNo</th><th>MaterialName</th></tr></thead><tbody>
      <tr v-for="row in rows" :key="row.materialNo" :class="{ selected: row.materialNo === selectedMaterialNo }" @click="selectMaterial(row.materialNo)"><td>{{ row.materialType }}</td><td>{{ row.materialNo }}</td><td>{{ row.materialName }}</td></tr>
      <tr v-if="!rows.length"><td colspan="3" class="empty">{{ loading ? '查詢中…' : '輸入 MaterialType 或 MaterialNo 後按 Query' }}</td></tr>
    </tbody></table></div></section>

    <div class="qc-workspace">
      <div class="catalogs">
        <section v-for="kind in kinds" :key="kind" class="catalog-panel"><header><h2>MTL QCItem for {{ kindLabels[kind] }}</h2><span>{{ availableByKind[kind].length }} 筆</span></header><div class="catalog-scroll"><table><thead><tr><th class="check-col"></th><th>QCItemNo</th><th>QCItemName</th></tr></thead><tbody>
          <tr v-for="row in availableByKind[kind]" :key="row.qcItemNo" @dblclick="toggleAvailable(kind,row.qcItemNo,true);addItems(kind)"><td class="check-col"><input type="checkbox" :checked="availableSelection[kind].includes(row.qcItemNo)" @change="toggleAvailable(kind,row.qcItemNo,($event.target as HTMLInputElement).checked)"></td><td>{{ row.qcItemNo }}</td><td>{{ row.qcItemName }}</td></tr>
          <tr v-if="!availableByKind[kind].length"><td colspan="3" class="empty">{{ activeMaterial ? (detailLoading ? '載入中…' : '沒有可加入的項目') : '先查詢並選取物料' }}</td></tr>
        </tbody></table></div><button class="win-btn add-button" type="button" :disabled="!availableSelection[kind].length" @click="addItems(kind)">→</button></section>
      </div>
      <section class="assigned-panel"><header><h2>MTLQCITEM<span v-if="activeMaterial">：{{ activeMaterial.materialNo }}</span></h2><button class="win-btn remove-button" type="button" :disabled="!assignedSelection.length" @click="removeItems">←</button><span>{{ assignedItems.length }} 筆</span></header><div class="assigned-scroll"><table><thead><tr><th class="check-col"></th><th>QCItemNo</th><th>QCItemName</th><th>QCItemType</th><th>Reviser</th><th>ReviseDate</th></tr></thead><tbody>
        <template v-for="kind in kinds" :key="kind"><tr v-if="assignedByKind[kind].length" class="group-row"><td colspan="6">{{ kindLabels[kind] }}（{{ assignedByKind[kind].length }}）</td></tr><tr v-for="row in assignedByKind[kind]" :key="row.qcItemNo" @dblclick="toggleAssigned(row.qcItemNo,true);removeItems()"><td class="check-col"><input type="checkbox" :checked="assignedSelection.includes(row.qcItemNo)" @change="toggleAssigned(row.qcItemNo,($event.target as HTMLInputElement).checked)"></td><td>{{ row.qcItemNo }}</td><td>{{ row.qcItemName }}</td><td>{{ kindLabels[row.qcItemType] }}</td><td>{{ row.reviser }}</td><td>{{ dateText(row.reviseDate) }}</td></tr></template>
        <tr v-if="!assignedItems.length"><td colspan="6" class="empty">{{ activeMaterial ? (detailLoading ? '載入中…' : '尚未設定 QC Item') : '先選取物料' }}</td></tr>
      </tbody></table></div></section>
    </div>
    <p v-if="error" class="feedback error" role="alert">{{ error }}</p><p v-if="notice" class="feedback success" role="status">{{ notice }}</p>
    <footer class="page-footer"><span>{{ activeMaterial ? `${activeMaterial.materialNo} — ${activeMaterial.materialName}` : '請先查詢並選取物料' }}</span><div><button class="win-btn primary" type="button" :disabled="!activeMaterial || loadedMaterialNo !== activeMaterial.materialNo || saving" @click="save">Confirm</button><button class="win-btn" type="button" @click="emit('close')">Close</button></div></footer>

    <div v-if="pickerMode" class="picker-backdrop" @click.self="pickerMode=null"><section class="picker-window"><header><h2>{{ pickerMode === 'material-type' ? '選擇 MaterialType' : '選擇 MaterialNo' }}</h2><button class="close-x" type="button" @click="pickerMode=null">×</button></header><input v-model="pickerQuery" class="kd-input picker-search" :placeholder="pickerMode === 'material-type' ? '搜尋類別編號/名稱' : '搜尋物料編號/名稱/類別'"><div class="picker-grid"><table><thead><tr v-if="pickerMode === 'material-type'"><th>MaterialType</th><th>名稱/說明</th></tr><tr v-else><th>MaterialType</th><th>MaterialNo</th><th>MaterialName</th></tr></thead><tbody>
      <template v-if="pickerMode === 'material-type'"><tr v-for="row in filteredTypeOptions" :key="row.materialType" @dblclick="chooseType(row)" @click="chooseType(row)"><td>{{ row.materialType }}</td><td>{{ row.materialTypeName || row.description }}</td></tr></template><template v-else><tr v-for="row in filteredMaterialOptions" :key="row.materialNo" @dblclick="chooseMaterial(row)" @click="chooseMaterial(row)"><td>{{ row.materialType }}</td><td>{{ row.materialNo }}</td><td>{{ row.materialName }}</td></tr></template>
      <tr v-if="pickerMode === 'material-type' && !filteredTypeOptions.length"><td colspan="2" class="empty">無符合項目</td></tr><tr v-if="pickerMode === 'material' && !filteredMaterialOptions.length"><td colspan="3" class="empty">無符合項目</td></tr>
    </tbody></table></div><footer><button class="win-btn" type="button" @click="pickerMode=null">關閉</button></footer></section></div>
  </section>
</template>

<style scoped>
.material-qc-page{height:100%;min-height:0;display:flex;flex-direction:column;background:#fff;color:#292929;font:13px "Microsoft JhengHei",Arial,sans-serif}.page-title{height:46px;flex:none;display:flex;align-items:center;padding:0 22px;border-bottom:1px solid #ddd8ce;background:#f7f5ef}.page-title h1{margin:0;font-size:19px;font-weight:500}.query-panel{height:76px;flex:none;display:grid;grid-template-columns:95px minmax(120px,185px) 18px 32px minmax(120px,158px) 1fr 80px;grid-template-rows:1fr 1fr;align-items:center;gap:2px 6px;margin:6px 10px;padding:4px 9px;border:1px solid #aaa}.query-panel legend{padding:0 5px;color:#666}.query-panel label{white-space:nowrap}.kd-input{box-sizing:border-box;min-width:0;width:100%;height:25px;border:1px solid #999;border-radius:0;padding:3px 6px;background:#ffffc0;color:#222;font:inherit}.lookup{height:25px;min-height:25px;padding:1px 5px}.query-panel label[for="mtl-type"]{grid-column:1;grid-row:1}.query-panel #mtl-type{grid-column:2;grid-row:1}.query-panel .lookup:first-of-type{grid-column:4;grid-row:1}.query-panel label[for="mtl-no"]{grid-column:1;grid-row:2}.query-panel #mtl-no{grid-column:2;grid-row:2}.percent{grid-column:3;grid-row:2;text-align:center}.query-panel .lookup:nth-of-type(2){grid-column:4;grid-row:2}.query-button{grid-column:7;grid-row:1/3;height:31px}.materials-panel{height:125px;flex:none;display:flex;flex-direction:column;margin:0 10px 7px;border:1px solid #aaa;min-height:0}.materials-panel>header,.catalog-panel>header,.assigned-panel>header{height:25px;flex:none;display:flex;align-items:center;justify-content:space-between;padding:0 8px;border-bottom:1px solid #c9c9c9;background:#f2f0e9}.materials-panel h2,.catalog-panel h2,.assigned-panel h2{margin:0;font-size:12px;font-weight:500}.materials-panel header span,.catalog-panel header span,.assigned-panel header>span{font-size:11px;color:#777}.material-table-wrap,.catalog-scroll,.assigned-scroll{flex:1;min-height:0;overflow:auto}.material-qc-page table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}.material-qc-page th,.material-qc-page td{height:22px;padding:3px 6px;border:1px solid #c9c9c9;text-align:left}.material-qc-page th{position:sticky;top:0;z-index:1;background:#e7e5df;font-weight:500}.material-table-wrap tbody tr,.catalog-scroll tbody tr{cursor:pointer}.material-table-wrap tbody tr:hover,.catalog-scroll tbody tr:hover{background:#fff5d8}.material-table-wrap tbody tr.selected{background:#ffefbf}.qc-workspace{display:grid;grid-template-columns:minmax(0,1fr) minmax(0,1fr);gap:8px;flex:1;min-height:0;margin:0 10px 7px}.catalogs{display:grid;grid-template-rows:repeat(3,minmax(0,1fr));gap:5px;min-height:0}.catalog-panel,.assigned-panel{display:flex;flex-direction:column;min-height:0;border:1px solid #aaa;background:white}.catalog-scroll table,.assigned-scroll table{width:100%}.check-col{width:24px;text-align:center!important}.check-col input{accent-color:#e4a02f}.add-button{align-self:flex-end;min-height:22px;padding:0 12px;margin:2px 5px}.remove-button{min-height:22px;padding:0 10px}.group-row td{background:#efeee8;font-weight:600}.empty{text-align:center;color:#777;padding:8px!important}.feedback{flex:none;margin:0 10px 4px;padding:4px 8px;border:1px solid #ddd;font-size:12px}.feedback.error{color:#8d2222;background:#fff3f1}.feedback.success{color:#356b35;background:#f1faee}.page-footer{height:39px;flex:none;display:flex;align-items:center;justify-content:space-between;padding:0 12px;border-top:1px solid #ccc;color:#666;font-size:12px}.page-footer>div{display:flex;gap:8px}.page-footer .win-btn,.win-btn{min-height:28px;padding:3px 10px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#f1efeb);font:inherit;white-space:nowrap;cursor:pointer}.win-btn:hover:not(:disabled){border-color:#e39a37;background:#fff8e8}.win-btn:disabled{opacity:.5;cursor:default}.win-btn.primary{border-color:#e69520;background:#f7a52a;color:white;font-weight:600}.picker-backdrop{position:fixed;inset:0;z-index:1100;display:grid;place-items:center;padding:12px;background:#0005}.picker-window{display:flex;flex-direction:column;width:min(620px,95vw);height:min(440px,88vh);padding:0 10px 8px;border:1px solid #999;background:#f9f8f4;box-shadow:0 8px 28px #0004}.picker-window>header{height:38px;flex:none;display:flex;align-items:center;justify-content:space-between;margin:0 -10px 7px;padding:0 10px;border-bottom:1px solid #ccc;background:#f1efe9}.picker-window h2{margin:0;font-size:15px;font-weight:500}.close-x{border:0;background:transparent;font-size:24px;color:#555;cursor:pointer}.picker-search{width:100%;margin-bottom:7px}.picker-grid{flex:1;min-height:0;overflow:auto;border:1px solid #aaa;background:white}.picker-grid tbody tr{cursor:pointer}.picker-grid tbody tr:hover{background:#fff0bf}.picker-window footer{display:flex;justify-content:flex-end;padding-top:7px}@media(max-width:760px){.query-panel{height:auto;grid-template-columns:90px minmax(90px,1fr) 18px 30px 80px;grid-template-rows:repeat(2,32px)}.query-panel label[for="mtl-type"]{grid-column:1;grid-row:1}.query-panel #mtl-type{grid-column:2;grid-row:1}.query-panel .lookup:first-of-type{grid-column:4;grid-row:1}.query-panel label[for="mtl-no"]{grid-column:1;grid-row:2}.query-panel #mtl-no{grid-column:2;grid-row:2}.percent{grid-column:3;grid-row:2}.query-panel .lookup:nth-of-type(2){grid-column:4;grid-row:2}.query-button{grid-column:5;grid-row:1/3}.qc-workspace{grid-template-columns:minmax(0,1fr);grid-template-rows:minmax(240px,1fr) minmax(240px,1fr)}.assigned-panel{grid-row:2}.assigned-scroll table{min-width:650px}}
</style>
