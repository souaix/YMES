<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import LocatorBasisMaster from './LocatorBasisMaster.vue'

interface InventoryProperty {
  propertyNo: string
  defaultValue: string
  propertySequence: number
  description: string
  issueState: number
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
  dataStamp: number
}
interface InventoryRow {
  inventoryNo: string
  inventoryName: string
  inventoryType: number
  inventoryClass: number
  factoryNo: string
  engineerGroupNo: string
  description: string
  creator: string
  createDate: string
  issueState: number
  reviser: string
  reviseDate: string
  dataStamp: number
  properties: InventoryProperty[]
}
interface InventoryDraft {
  inventoryNo: string
  inventoryName: string
  inventoryType: number
  inventoryClass: number
  factoryNo: string
  engineerGroupNo: string
  description: string
}
interface PropertyDefinition { propertyNo: string; defaultValue: string; propertySequence: number; description: string }
interface PropertyDraft { propertyNo: string; defaultValue: string; propertySequence: string; description: string }
type FormMode = 'add' | 'edit' | 'copy' | null

const props = withDefaults(defineProps<{ developmentPreview: boolean; currentUserNo?: string }>(), { currentUserNo: '' })
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.inv.inventory-basis.v1'
const locatorStorageKey = 'ymes-dev-mock:smd-locator-basis:v1'
const inventoryTypes = [
  { value: 0, label: 'RAW 原物料倉' }, { value: 1, label: 'FGD 成品倉' }, { value: 2, label: 'SEMI 半成品倉' },
  { value: 3, label: 'SCRAP 報廢倉' }, { value: 4, label: 'FAIL 不良品倉' }, { value: 6, label: 'WIP 在製品倉' },
  { value: 7, label: 'DUMMY 虛擬倉' }, { value: 9, label: 'TOOL 模治具倉' },
]
const issueStates = [
  { value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' },
]
const factories = [
  { factoryNo: 'F01', factoryName: '一廠' }, { factoryNo: 'F02', factoryName: '二廠' }, { factoryNo: 'F03', factoryName: '測試廠' },
]
const engineerGroups = [
  { groupNo: 'ENG-EQ-01', groupName: '設備工程一組' }, { groupNo: 'ENG-EQ-02', groupName: '設備工程二組' },
  { groupNo: 'ENG-FAC-01', groupName: '廠務工程組' }, { groupNo: 'ENG-PE-01', groupName: '製程工程組' },
]
const propertyDefinitions: PropertyDefinition[] = [
  { propertyNo: 'OWNER_DEPT', defaultValue: '工程一課', propertySequence: 10, description: '管理部門' },
  { propertyNo: 'STORAGE_RULE', defaultValue: 'FIFO', propertySequence: 20, description: '庫存管理規則' },
  { propertyNo: 'TEMP_RANGE', defaultValue: '常溫', propertySequence: 30, description: '保存條件' },
  { propertyNo: 'ERP_LOCATION', defaultValue: 'LOC-01', propertySequence: 40, description: 'ERP 儲位對應' },
]
const seedRows: InventoryRow[] = [
  { inventoryNo: 'INV-DEV-RAW', inventoryName: '一廠原物料倉', inventoryType: 0, inventoryClass: 1, factoryNo: 'F01', engineerGroupNo: 'ENG-EQ-01', description: 'DEV 假資料：原料收發與儲位管理。', creator: 'DEV001', createDate: '2026-10-12 08:15', issueState: 0, reviser: 'DEV002', reviseDate: '2026-10-16 10:20', dataStamp: 2, properties: [
    { propertyNo: 'OWNER_DEPT', defaultValue: '工程一課', propertySequence: 10, description: '管理部門', issueState: 2, creator: 'DEV001', createDate: '2026-10-12 08:20', reviser: '', reviseDate: '', dataStamp: 1 },
    { propertyNo: 'STORAGE_RULE', defaultValue: 'FIFO', propertySequence: 20, description: '庫存管理規則', issueState: 0, creator: 'DEV001', createDate: '2026-10-12 08:22', reviser: '', reviseDate: '', dataStamp: 1 },
  ] },
  { inventoryNo: 'INV-DEV-WIP', inventoryName: '在製品線邊倉', inventoryType: 6, inventoryClass: 1, factoryNo: 'F01', engineerGroupNo: 'ENG-EQ-02', description: 'DEV 假資料：在製品線邊庫房。', creator: 'DEV001', createDate: '2026-10-14 09:00', issueState: 1, reviser: '', reviseDate: '', dataStamp: 1, properties: [] },
  { inventoryNo: 'INV-DEV-FGD', inventoryName: '成品待出貨倉', inventoryType: 1, inventoryClass: 0, factoryNo: 'F02', engineerGroupNo: 'ENG-FAC-01', description: 'DEV 假資料：成品入庫。', creator: 'DEV003', createDate: '2026-10-20 13:40', issueState: 2, reviser: 'DEV003', reviseDate: '2026-10-21 09:10', dataStamp: 3, properties: [
    { propertyNo: 'STORAGE_RULE', defaultValue: 'FIFO', propertySequence: 20, description: '先進先出', issueState: 2, creator: 'DEV003', createDate: '2026-10-20 13:50', reviser: 'DEV003', reviseDate: '2026-10-21 09:10', dataStamp: 2 },
  ] },
  { inventoryNo: 'INV-DEV-SCR', inventoryName: '不良品隔離倉', inventoryType: 3, inventoryClass: 1, factoryNo: 'F03', engineerGroupNo: '', description: 'DEV 假資料：停用庫房示例。', creator: 'DEV002', createDate: '2026-10-25 16:20', issueState: -1, reviser: 'DEV002', reviseDate: '2026-11-01 11:30', dataStamp: 2, properties: [] },
]

function readRows(): InventoryRow[] {
  try {
    const raw = window.localStorage.getItem(storageKey)
    if (raw) {
      const value = JSON.parse(raw) as InventoryRow[]
      if (Array.isArray(value) && value.every((row) => row && typeof row.inventoryNo === 'string' && Array.isArray(row.properties))) return value
    }
  } catch { /* use fixtures when local storage is unavailable */ }
  return structuredClone(seedRows)
}

const rows = ref<InventoryRow[]>(readRows())
const issueState = ref(0)
const queryInventoryNo = ref('')
const appliedQuery = ref('')
const selectedNo = ref(rows.value[0]?.inventoryNo ?? '')
const selectedNos = ref<string[]>([])
const notice = ref('DEV 假資料：不會呼叫 MES 或正式 API。')
const currentUser = computed(() => props.currentUserNo.trim() || 'DEVUSER')
const formMode = ref<FormMode>(null)
const formError = ref('')
const draft = ref<InventoryDraft>(blankInventoryDraft())
const propertyDialogOpen = ref(false)
const propertyIssueState = ref(0)
const selectedPropertyNos = ref<string[]>([])
const activePropertyNo = ref('')
const propertyMode = ref<FormMode>(null)
const propertyError = ref('')
const propertyDefinitionNo = ref('')
const propertyDraft = ref<PropertyDraft>({ propertyNo: '', defaultValue: '', propertySequence: '', description: '' })
const locatorDialogOpen = ref(false)

function blankInventoryDraft(): InventoryDraft { return { inventoryNo: '', inventoryName: '', inventoryType: 0, inventoryClass: 1, factoryNo: '', engineerGroupNo: '', description: '' } }
const visibleRows = computed(() => {
  const query = appliedQuery.value.trim().toLocaleUpperCase()
  return rows.value.filter((row) => row.issueState === issueState.value && (!query || row.inventoryNo.toLocaleUpperCase().includes(query)))
    .sort((a, b) => a.inventoryNo.localeCompare(b.inventoryNo))
})
const activeInventory = computed(() => visibleRows.value.find((row) => row.inventoryNo === selectedNo.value) ?? null)
const selectedRows = computed(() => visibleRows.value.filter((row) => selectedNos.value.includes(row.inventoryNo)))
const actionRows = computed(() => {
  const selected = [...selectedRows.value]
  const active = visibleRows.value.find((row) => row.inventoryNo === selectedNo.value)
  if (active && !selected.some((row) => row.inventoryNo === active.inventoryNo)) selected.push(active)
  return selected
})
const canEdit = computed(() => activeInventory.value?.issueState === 0)
const visibleProperties = computed(() => (activeInventory.value?.properties ?? []).filter((property) => property.issueState === propertyIssueState.value).sort((a, b) => a.propertySequence - b.propertySequence || a.propertyNo.localeCompare(b.propertyNo)))
const activeProperty = computed(() => visibleProperties.value.find((property) => property.propertyNo === activePropertyNo.value) ?? null)
const actionProperties = computed(() => {
  const selected = visibleProperties.value.filter((property) => selectedPropertyNos.value.includes(property.propertyNo))
  const active = visibleProperties.value.find((property) => property.propertyNo === activePropertyNo.value)
  if (active && !selected.some((property) => property.propertyNo === active.propertyNo)) selected.push(active)
  return selected
})

function runQuery() { appliedQuery.value = queryInventoryNo.value.trim() }

watch(visibleRows, (visible) => {
  if (!visible.some((row) => row.inventoryNo === selectedNo.value)) selectedNo.value = visible[0]?.inventoryNo ?? ''
  selectedNos.value = selectedNos.value.filter((no) => visible.some((row) => row.inventoryNo === no))
})

function selectRow(row: InventoryRow) { selectedNo.value = row.inventoryNo }
function toggleRow(row: InventoryRow) {
  selectedNo.value = row.inventoryNo
  selectedNos.value = selectedNos.value.includes(row.inventoryNo) ? selectedNos.value.filter((no) => no !== row.inventoryNo) : [...selectedNos.value, row.inventoryNo]
}
function openMainForm(mode: Exclude<FormMode, null>) {
  if (mode !== 'add' && !activeInventory.value) return
  if (mode === 'edit' && !canEdit.value) { notice.value = '只有未簽核庫房可以修改。'; return }
  formError.value = ''
  formMode.value = mode
  const source = activeInventory.value
  draft.value = mode === 'add' || !source ? blankInventoryDraft() : {
    inventoryNo: mode === 'copy' ? `${source.inventoryNo}-COPY` : source.inventoryNo,
    inventoryName: mode === 'copy' ? `${source.inventoryName} 複製` : source.inventoryName,
    inventoryType: source.inventoryType,
    inventoryClass: source.inventoryClass,
    factoryNo: source.factoryNo,
    engineerGroupNo: source.engineerGroupNo,
    description: source.description,
  }
}
function saveMainForm(closeAfterSave: boolean) {
  const inventoryNo = draft.value.inventoryNo.trim().toLocaleUpperCase()
  const inventoryName = draft.value.inventoryName.trim()
  if (!inventoryNo || !inventoryName || !draft.value.factoryNo) { formError.value = '庫房編號、庫房名稱及廠區為必填。'; return }
  const duplicate = rows.value.some((row) => row.inventoryNo.toLocaleUpperCase() === inventoryNo && !(formMode.value === 'edit' && row.inventoryNo === selectedNo.value))
  if (duplicate) { formError.value = '庫房編號已存在。'; return }
  const now = new Date().toLocaleString('zh-TW', { hour12: false })
  if (formMode.value === 'edit' && activeInventory.value) {
    Object.assign(activeInventory.value, {
      inventoryName, inventoryType: Number(draft.value.inventoryType), inventoryClass: Number(draft.value.inventoryClass),
      factoryNo: draft.value.factoryNo, engineerGroupNo: draft.value.engineerGroupNo, description: draft.value.description,
      reviser: currentUser.value, reviseDate: now, dataStamp: activeInventory.value.dataStamp + 1,
    })
    notice.value = `已模擬修改 ${activeInventory.value.inventoryNo}。`
  } else {
    const created: InventoryRow = {
      inventoryNo, inventoryName, inventoryType: Number(draft.value.inventoryType), inventoryClass: Number(draft.value.inventoryClass),
      factoryNo: draft.value.factoryNo, engineerGroupNo: draft.value.engineerGroupNo, description: draft.value.description,
      creator: currentUser.value, createDate: now, issueState: 0, reviser: '', reviseDate: '', dataStamp: 1, properties: [],
    }
    rows.value = [...rows.value, created].sort((a, b) => a.inventoryNo.localeCompare(b.inventoryNo))
    selectedNo.value = created.inventoryNo
    selectedNos.value = [created.inventoryNo]
    notice.value = formMode.value === 'copy' ? '已模擬複製庫房主檔；舊 WinForms Copy 只建立主檔欄位。' : `已模擬新增 ${created.inventoryNo}，狀態為未簽核。`
  }
  if (closeAfterSave) formMode.value = null
}
function removeLocators(inventoryNo: string) {
  try {
    const raw = window.localStorage.getItem(locatorStorageKey)
    if (!raw) return
    const locators = JSON.parse(raw) as Array<{ inventoryNo?: string }>
    window.localStorage.setItem(locatorStorageKey, JSON.stringify(locators.filter((row) => row.inventoryNo !== inventoryNo)))
  } catch { /* linked fake rows may be unavailable */ }
}
function deleteSelected() {
  if (!actionRows.value.length) { notice.value = '請先選取庫房。'; return }
  if (!window.confirm(`確定刪除選取的 ${actionRows.value.length} 筆庫房資料？此操作也會移除其 DEV Property 與 Locator 假資料。`)) return
  const targets = [...actionRows.value]
  const hardDelete = targets.filter((row) => row.issueState === 0 || row.issueState === -1)
  const softDelete = targets.filter((row) => row.issueState === 2)
  const blocked = targets.filter((row) => row.issueState === 1)
  const now = new Date().toLocaleString('zh-TW', { hour12: false })
  for (const row of hardDelete) removeLocators(row.inventoryNo)
  for (const row of softDelete) Object.assign(row, { issueState: -1, reviser: currentUser.value, reviseDate: now, dataStamp: row.dataStamp + 1 })
  rows.value = rows.value.filter((row) => !hardDelete.some((target) => target.inventoryNo === row.inventoryNo))
  selectedNos.value = selectedNos.value.filter((no) => !hardDelete.some((row) => row.inventoryNo === no))
  const removed = hardDelete.length + softDelete.length
  notice.value = blocked.length ? `已模擬處理 ${removed} 筆（未簽核／不使用實體刪除，已簽核改為不使用）；${blocked.length} 筆簽核中資料被拒絕。舊 Form 多選逐筆執行，可能部分成功。` : `已模擬處理 ${removed} 筆庫房（未簽核／不使用實體刪除，已簽核改為不使用）。`
}
function approveSelected() {
  const targets = actionRows.value.filter((row) => row.issueState === 0)
  if (!targets.length) { notice.value = '只有未簽核庫房可以核准。'; return }
  if (!window.confirm(`確定送出 ${targets.length} 筆庫房核准？DEV 會模擬完成簽核。`)) return
  const now = new Date().toLocaleString('zh-TW', { hour12: false })
  for (const row of targets) Object.assign(row, { issueState: 2, reviser: currentUser.value, reviseDate: now, dataStamp: row.dataStamp + 1 })
  notice.value = `已模擬 ${targets.length} 筆核准；正式流程須依 IssueManagement 設定處理。`
}

function openProperties() {
  if (!activeInventory.value) { notice.value = '請先選擇庫房。'; return }
  propertyDialogOpen.value = true
  propertyIssueState.value = 0
  selectedPropertyNos.value = []
  activePropertyNo.value = ''
  propertyMode.value = null
}
function selectProperty(property: InventoryProperty) { activePropertyNo.value = property.propertyNo }
function toggleProperty(property: InventoryProperty) {
  activePropertyNo.value = property.propertyNo
  selectedPropertyNos.value = selectedPropertyNos.value.includes(property.propertyNo)
    ? selectedPropertyNos.value.filter((no) => no !== property.propertyNo)
    : [...selectedPropertyNos.value, property.propertyNo]
}
function openPropertyForm(mode: Exclude<FormMode, null>) {
  if (mode !== 'add' && !activeProperty.value) return
  if ((mode === 'edit' || mode === 'copy') && activeProperty.value?.issueState !== 0) { notice.value = 'Property 只有未簽核資料可編輯或複製。'; return }
  propertyError.value = ''
  propertyDefinitionNo.value = ''
  propertyMode.value = mode
  const source = activeProperty.value
  propertyDraft.value = mode === 'add' || !source ? { propertyNo: '', defaultValue: '', propertySequence: '', description: '' } : {
    propertyNo: mode === 'copy' ? '' : source.propertyNo,
    defaultValue: source.defaultValue,
    propertySequence: String(source.propertySequence),
    description: source.description,
  }
}
function choosePropertyDefinition() {
  const definition = propertyDefinitions.find((item) => item.propertyNo === propertyDefinitionNo.value)
  if (!definition) return
  propertyDraft.value = { propertyNo: definition.propertyNo, defaultValue: definition.defaultValue, propertySequence: String(definition.propertySequence), description: definition.description }
}
function saveProperty(closeAfterSave: boolean) {
  if (!activeInventory.value) return
  const propertyNo = propertyDraft.value.propertyNo.trim().toLocaleUpperCase()
  if (!propertyNo) { propertyError.value = 'PropertyNo 為必填，請從 PropertyType 8 的定義清單選取。'; return }
  const duplicate = activeInventory.value.properties.some((row) => row.propertyNo.toLocaleUpperCase() === propertyNo && !(propertyMode.value === 'edit' && row.propertyNo === activePropertyNo.value))
  if (duplicate) { propertyError.value = '此庫房已設定相同 PropertyNo。'; return }
  const now = new Date().toLocaleString('zh-TW', { hour12: false })
  if (propertyMode.value === 'edit' && activeProperty.value) {
    Object.assign(activeProperty.value, {
      defaultValue: propertyDraft.value.defaultValue, propertySequence: Number(propertyDraft.value.propertySequence || 0),
      description: propertyDraft.value.description, reviser: currentUser.value, reviseDate: now, dataStamp: activeProperty.value.dataStamp + 1,
    })
    notice.value = `已模擬修改 ${propertyNo}。`
  } else {
    const created: InventoryProperty = {
      propertyNo, defaultValue: propertyDraft.value.defaultValue, propertySequence: Number(propertyDraft.value.propertySequence || 0),
      description: propertyDraft.value.description, issueState: 0, creator: currentUser.value, createDate: now, reviser: '', reviseDate: '', dataStamp: 1,
    }
    activeInventory.value.properties = [...activeInventory.value.properties, created]
    activePropertyNo.value = propertyNo
    selectedPropertyNos.value = [propertyNo]
    notice.value = `已模擬新增 ${propertyNo} Property。`
  }
  if (closeAfterSave) propertyMode.value = null
}
function deleteProperties() {
  if (!activeInventory.value || !actionProperties.value.length) { notice.value = '請先選取 Property。'; return }
  if (!window.confirm(`確定刪除選取的 ${actionProperties.value.length} 筆 Property？`)) return
  const targets = [...actionProperties.value]
  const hardDelete = targets.filter((row) => row.issueState === 0 || row.issueState === -1)
  const softDelete = targets.filter((row) => row.issueState === 2)
  const now = new Date().toLocaleString('zh-TW', { hour12: false })
  for (const row of softDelete) Object.assign(row, { issueState: -1, reviser: currentUser.value, reviseDate: now, dataStamp: row.dataStamp + 1 })
  const allowed = [...hardDelete, ...softDelete]
  const blocked = targets.length - allowed.length
  activeInventory.value.properties = activeInventory.value.properties.filter((row) => !hardDelete.includes(row))
  selectedPropertyNos.value = []
  activePropertyNo.value = ''
  notice.value = blocked ? `已模擬處理 ${allowed.length} 筆 Property（已簽核改為不使用）；簽核中資料不可刪除。` : `已模擬處理 ${allowed.length} 筆 Property（已簽核改為不使用）。`
}
function approveProperties() {
  if (!activeInventory.value || propertyIssueState.value !== 0) { notice.value = '只有未簽核 Property 可以核准。'; return }
  const targets = actionProperties.value.filter((row) => row.issueState === 0)
  if (!targets.length) { notice.value = '請選取未簽核 Property。'; return }
  if (!window.confirm(`確定核准 ${targets.length} 筆 Property？DEV 會模擬完成簽核。`)) return
  const now = new Date().toLocaleString('zh-TW', { hour12: false })
  for (const row of targets) Object.assign(row, { issueState: 2, reviser: currentUser.value, reviseDate: now, dataStamp: row.dataStamp + 1 })
  notice.value = `已模擬核准 ${targets.length} 筆 Property。`
}
function closePropertyDialog() { propertyDialogOpen.value = false; propertyMode.value = null }

watch(rows, (value) => {
  try { window.localStorage.setItem(storageKey, JSON.stringify(value)) } catch { notice.value = '瀏覽器未允許 localStorage，資料只保留在目前頁面。' }
}, { deep: true })
</script>

<template>
  <main class="inv-basis" aria-label="庫房基本資料 DEV">
    <header class="page-heading"><div><p>INV · IV102</p><h1>庫房基本資料</h1></div><button type="button" class="close-icon" aria-label="Close" @click="emit('close')">×</button></header>
    <div class="query-strip">
      <fieldset class="state-filter"><legend>Issue State</legend><label v-for="state in issueStates" :key="state.value"><input v-model="issueState" type="radio" :value="state.value" />{{ state.label }}</label></fieldset>
      <label class="query-label">InventoryNo <input v-model="queryInventoryNo" type="search" placeholder="輸入庫房編號" @keydown.enter.prevent="runQuery" /></label>
      <button type="button" @click="runQuery">Query</button>
      <span class="preview-note">Development Preview · 假資料</span>
    </div>
    <div v-if="notice" class="notice" role="status">{{ notice }}</div>
    <div class="work-area">
      <section class="grid-panel" aria-label="庫房清單">
        <div class="table-scroll"><table><thead><tr><th class="selector"></th><th>InventoryNo</th><th>InventoryName</th><th>InventoryType</th><th>InventoryClass</th><th>FactoryNo</th><th>EngineerGroupNo</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead><tbody>
          <tr v-for="row in visibleRows" :key="row.inventoryNo" :class="{ active: row.inventoryNo === selectedNo, checked: selectedNos.includes(row.inventoryNo) }" @click="selectRow(row)" @dblclick="row.issueState === 0 && openMainForm('edit')">
            <td class="selector"><input type="checkbox" :checked="selectedNos.includes(row.inventoryNo)" :aria-label="`選取 ${row.inventoryNo}`" @click.stop="toggleRow(row)" /></td>
            <td class="mono">{{ row.inventoryNo }}</td><td>{{ row.inventoryName }}</td><td>{{ inventoryTypes.find((item) => item.value === row.inventoryType)?.label ?? row.inventoryType }}</td><td>{{ row.inventoryClass === 0 ? 'System' : 'User' }}</td><td>{{ row.factoryNo }}</td><td>{{ row.engineerGroupNo }}</td><td class="description">{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser }}</td><td>{{ row.reviseDate }}</td>
          </tr>
          <tr v-if="!visibleRows.length"><td class="empty" colspan="12">沒有符合條件的庫房資料</td></tr>
        </tbody></table></div>
        <footer class="grid-footer">{{ visibleRows.length }} 筆 · DataStamp {{ activeInventory?.dataStamp ?? '—' }} · 狀態 {{ issueStates.find((state) => state.value === issueState)?.label }}</footer>
      </section>
      <aside class="side-actions">
        <button type="button" class="primary" @click="openMainForm('add')">ADD (A)</button>
        <button type="button" :disabled="!canEdit" @click="openMainForm('edit')">EDIT (E)</button>
        <button type="button" class="danger" :disabled="!actionRows.length" @click="deleteSelected">DEL (D)</button>
        <button type="button" :disabled="!activeInventory" @click="openMainForm('copy')">Copy (C)</button>
        <button type="button" :disabled="issueState !== 0 || !actionRows.length" @click="approveSelected">Approve (Y)</button>
        <hr />
        <button type="button" :disabled="!activeInventory" @click="openProperties">Property</button>
        <button type="button" :disabled="!activeInventory" @click="locatorDialogOpen = true">Locator</button>
        <button type="button" class="close-button" @click="emit('close')">Close (X)</button>
      </aside>
    </div>

    <div v-if="formMode" class="modal-backdrop" @click.self="formMode = null">
      <form class="editor" @submit.prevent="saveMainForm(true)">
        <header><h2>Inventory Data · {{ formMode === 'add' ? 'ADD' : formMode === 'edit' ? 'EDIT' : 'COPY' }}</h2><button type="button" aria-label="關閉" @click="formMode = null">×</button></header>
        <fieldset><legend>Inventory Data</legend>
          <label>InventoryNo <input v-model="draft.inventoryNo" :readonly="formMode === 'edit'" @blur="draft.inventoryNo = draft.inventoryNo.toLocaleUpperCase()" /></label>
          <label>InventoryName <input v-model="draft.inventoryName" /></label>
          <label>InventoryType <select v-model.number="draft.inventoryType"><option v-for="type in inventoryTypes" :key="type.value" :value="type.value">{{ type.label }}</option></select></label>
          <label>InventoryClass <select v-model.number="draft.inventoryClass"><option :value="0">System Define</option><option :value="1">User Define</option></select></label>
          <label>FactoryNo <select v-model="draft.factoryNo"><option value="">請選擇廠區</option><option v-for="factory in factories" :key="factory.factoryNo" :value="factory.factoryNo">{{ factory.factoryNo }} · {{ factory.factoryName }}</option></select></label>
          <label>EngineerGroupNo <select v-model="draft.engineerGroupNo"><option value="">（未指定）</option><option v-for="group in engineerGroups" :key="group.groupNo" :value="group.groupNo">{{ group.groupNo }} · {{ group.groupName }}</option></select></label>
          <label class="description-field">Description <textarea v-model="draft.description" rows="3"></textarea></label>
        </fieldset>
        <p v-if="formMode !== 'add' && activeInventory" class="audit">Creator {{ activeInventory.creator }} · {{ activeInventory.createDate }} <template v-if="activeInventory.reviser"> · Reviser {{ activeInventory.reviser }} · {{ activeInventory.reviseDate }}</template></p>
        <p v-if="formError" class="form-error" role="alert">{{ formError }}</p>
        <footer><button type="button" @click="formMode = null">Close</button><span></span><button type="button" @click="saveMainForm(false)">Apply</button><button class="primary" type="submit">Confirm</button></footer>
      </form>
    </div>

    <div v-if="propertyDialogOpen && activeInventory" class="modal-backdrop property-backdrop" @click.self="closePropertyDialog">
      <section class="property-window" role="dialog" aria-modal="true" :aria-label="`${activeInventory.inventoryNo} Property`">
        <header class="property-heading"><div><p>InventoryNo · {{ activeInventory.inventoryNo }}</p><h2>Inventory Property</h2></div><button type="button" aria-label="關閉" @click="closePropertyDialog">×</button></header>
        <div class="property-toolbar"><fieldset class="state-filter compact"><legend>Issue State</legend><label v-for="state in issueStates" :key="state.value"><input v-model="propertyIssueState" type="radio" :value="state.value" />{{ state.label }}</label></fieldset><span></span><button class="primary" type="button" @click="openPropertyForm('add')">ADD</button><button type="button" :disabled="activeProperty?.issueState !== 0" @click="openPropertyForm('edit')">EDIT</button><button type="button" :disabled="!activeProperty" @click="openPropertyForm('copy')">Copy</button><button type="button" class="danger" :disabled="!actionProperties.length" @click="deleteProperties">DEL</button><button type="button" :disabled="propertyIssueState !== 0 || !actionProperties.length" @click="approveProperties">Approve</button></div>
        <div class="property-table-wrap"><table class="property-table"><thead><tr><th class="selector"></th><th>InventoryNo</th><th>PropertyNo</th><th>DefaultValue</th><th>PropertySequence</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead><tbody>
          <tr v-for="property in visibleProperties" :key="property.propertyNo" :class="{ active: activePropertyNo === property.propertyNo }" @click="selectProperty(property)"><td class="selector"><input type="checkbox" :checked="selectedPropertyNos.includes(property.propertyNo)" @click.stop="toggleProperty(property)" /></td><td>{{ activeInventory.inventoryNo }}</td><td>{{ property.propertyNo }}</td><td>{{ property.defaultValue }}</td><td class="number">{{ property.propertySequence }}</td><td>{{ property.description }}</td><td>{{ property.creator }}</td><td>{{ property.createDate }}</td><td>{{ property.reviser }}</td><td>{{ property.reviseDate }}</td></tr>
          <tr v-if="!visibleProperties.length"><td colspan="10" class="empty">沒有符合狀態的 Property</td></tr>
        </tbody></table></div>
        <footer class="property-footer"><span>{{ visibleProperties.length }} 筆</span><span>{{ notice }}</span><button type="button" @click="closePropertyDialog">Close (X)</button></footer>
      </section>
    </div>

    <div v-if="propertyMode" class="modal-backdrop definition-backdrop" @click.self="propertyMode = null">
      <form class="property-definition" @submit.prevent="saveProperty(true)">
        <header><h3>INV Property Data · {{ propertyMode.toUpperCase() }}</h3><button type="button" aria-label="關閉" @click="propertyMode = null">×</button></header>
        <label>InventoryNo <input :value="activeInventory?.inventoryNo" readonly /></label>
        <label>PropertyNo <div class="property-picker"><select v-if="propertyMode !== 'edit'" v-model="propertyDefinitionNo" @change="choosePropertyDefinition"><option value="">選擇 PropertyType 8 項目</option><option v-for="item in propertyDefinitions" :key="item.propertyNo" :value="item.propertyNo">{{ item.propertyNo }} · {{ item.description }}</option></select><input v-else :value="propertyDraft.propertyNo" readonly /></div></label>
        <label>DefaultValue <input v-model="propertyDraft.defaultValue" /></label>
        <label>PropertySequence <input v-model="propertyDraft.propertySequence" type="number" /></label>
        <label>Description <textarea v-model="propertyDraft.description" rows="3"></textarea></label>
        <p v-if="propertyError" class="form-error" role="alert">{{ propertyError }}</p>
        <footer><button type="button" @click="propertyMode = null">Close</button><span></span><button type="button" @click="saveProperty(false)">Apply</button><button class="primary" type="submit">Confirm</button></footer>
      </form>
    </div>

    <div v-if="locatorDialogOpen && activeInventory" class="locator-backdrop">
      <section class="locator-window" role="dialog" aria-modal="true" :aria-label="`${activeInventory.inventoryNo} Locator`">
        <LocatorBasisMaster :api-base-url="''" :current-user-no="currentUser" :development-preview="true" :initial-inventory-no="activeInventory.inventoryNo" @close="locatorDialogOpen = false" />
      </section>
    </div>
  </main>
</template>

<style scoped>
.inv-basis{display:flex;flex-direction:column;width:100%;height:100%;min-height:0;gap:8px;padding:10px;box-sizing:border-box;background:#f4f2eb;color:#292820;font:13px/1.35 "Segoe UI","Microsoft JhengHei",sans-serif}.page-heading{display:flex;align-items:center;justify-content:space-between;padding:0 2px 6px;border-bottom:1px solid #cbc8bb}.page-heading p,.property-heading p{margin:0;color:#777366;font-size:11px;letter-spacing:.06em}.page-heading h1{margin:0;font-size:20px;font-weight:600}.close-icon,.property-heading>button{width:29px;height:27px;border:1px solid #888477;background:#e7e4d9;color:#35332d;font-size:19px}.query-strip,.property-toolbar{display:flex;align-items:center;gap:7px;min-height:38px;flex-wrap:wrap}.state-filter{display:flex;align-items:center;gap:8px;margin:0;padding:3px 8px;border:1px solid #c8c5b9;background:#eeece4}.state-filter legend{padding:0 4px;color:#6d695e}.state-filter label{display:flex;align-items:center;gap:3px;white-space:nowrap}.state-filter input{margin:0;accent-color:#56704f}.query-label{display:flex;align-items:center;gap:6px}.query-label input,.editor input,.editor select,.editor textarea,.property-definition input,.property-definition select,.property-definition textarea{box-sizing:border-box;min-height:27px;border:1px solid #999587;padding:4px 7px;color:#292820;background:#fffef8;font:inherit}.query-label input{width:190px}.preview-note{margin-left:auto;color:#777366;font-size:11px}.inv-basis button{min-height:28px;padding:4px 9px;border:1px solid #8c887b;border-radius:2px;background:#fbfaf5;color:#292820;font:inherit;cursor:pointer;white-space:nowrap}.inv-basis button:hover:not(:disabled){background:#e7e4d9}.inv-basis button:disabled{opacity:.5;cursor:not-allowed}.inv-basis button.primary{border-color:#48634d;background:#506c54;color:white}.inv-basis button.danger{color:#8d302a}.notice{min-height:20px;color:#625f55;font-size:11px}.work-area{display:flex;flex:1;min-height:0;gap:8px}.grid-panel{display:flex;flex:1;min-width:0;min-height:0;flex-direction:column;border:1px solid #c6c2b6;background:#fffef9}.table-scroll,.property-table-wrap{flex:1;min-height:0;overflow:auto}.inv-basis table{width:max-content;min-width:100%;border-collapse:collapse;white-space:nowrap;font-size:11px}.inv-basis th,.inv-basis td{height:27px;padding:4px 6px;border-right:1px solid #e0ded6;border-bottom:1px solid #e3e1da;text-align:left}.inv-basis th{position:sticky;top:0;z-index:1;background:#e8e6dd;color:#514f46;font-weight:600}.inv-basis tbody tr{cursor:pointer}.inv-basis tbody tr:hover{background:#f5f2e5}.inv-basis tbody tr.active{background:#e4ecde}.inv-basis tbody tr.checked:not(.active){background:#f5efd9}.inv-basis .selector{width:28px;text-align:center}.inv-basis .selector input{margin:0}.mono{font-family:Consolas,"Courier New",monospace}.description{max-width:190px;overflow:hidden;text-overflow:ellipsis}.grid-footer,.property-footer{display:flex;justify-content:space-between;gap:12px;align-items:center;min-height:25px;padding:3px 8px;border-top:1px solid #d6d3c8;color:#6e6a60;font-size:11px}.side-actions{width:104px;display:flex;flex:none;flex-direction:column;gap:6px}.side-actions button{width:100%}.side-actions hr{width:100%;margin:3px 0;border:0;border-top:1px solid #c7c3b7}.side-actions .close-button{margin-top:auto}.empty{text-align:center;color:#817d72;padding:18px!important}.modal-backdrop,.locator-backdrop{position:fixed;inset:0;z-index:25;display:flex;align-items:center;justify-content:center;padding:16px;background:rgb(25 24 20 / 42%)}.editor,.property-definition{width:min(570px,96vw);max-height:94vh;display:flex;flex-direction:column;gap:9px;overflow:auto;padding:12px;border:1px solid #898578;background:#f8f6ef;box-shadow:0 8px 24px #0004}.editor>header,.property-definition>header{display:flex;align-items:center;justify-content:space-between;padding-bottom:7px;border-bottom:1px solid #cfccbf}.editor h2,.property-definition h3{margin:0;font-size:16px;font-weight:600}.editor>header button,.property-definition>header button{width:28px;padding:0;font-size:19px}.editor fieldset{display:grid;grid-template-columns:1fr 1fr;gap:8px 12px;margin:0;padding:10px;border:1px solid #c8c4b7}.editor legend{padding:0 5px}.editor fieldset>label,.property-definition>label{display:flex;flex-direction:column;gap:3px}.editor fieldset>label input,.editor fieldset>label select,.property-definition>label input,.property-definition>label select,.property-definition>label textarea{width:100%}.editor .description-field{grid-column:1/-1}.editor textarea,.property-definition textarea{resize:vertical}.audit{color:#777366;font-size:11px}.form-error{margin:0;color:#9a2924}.editor footer,.property-definition footer{display:flex;align-items:center;gap:7px;padding-top:8px;border-top:1px solid #cfccbf}.editor footer span,.property-definition footer span,.property-toolbar>span{flex:1}.property-backdrop{z-index:27}.property-window{display:flex;flex-direction:column;width:min(1200px,97vw);height:min(560px,92vh);min-height:340px;border:1px solid #8e8a7d;background:#faf9f4;box-shadow:0 8px 28px #0005}.property-heading{display:flex;align-items:center;justify-content:space-between;padding:8px 10px;border-bottom:1px solid #d5d1c5;background:#eae8df}.property-heading h2{margin:0;font-size:16px}.property-toolbar{padding:7px;border-bottom:1px solid #d8d5cb}.property-toolbar .compact{gap:5px}.property-table{font-size:11px!important}.property-table .number{text-align:right}.property-footer>span:nth-child(2){flex:1}.definition-backdrop{z-index:30}.property-definition{width:min(490px,96vw)}.property-picker{width:100%}.locator-backdrop{z-index:28;padding:2vh 2vw}.locator-window{width:min(1280px,96vw);height:94vh;overflow:hidden;border:1px solid #aaa;background:white;box-shadow:0 8px 28px #0005}.locator-window :deep(.locator-master){width:100%;height:100%}@media(max-width:900px){.state-filter{flex-wrap:wrap}.work-area{gap:5px}.side-actions{width:88px}.preview-note{margin-left:0}.editor fieldset{grid-template-columns:1fr}.property-window{height:90vh}.property-toolbar{overflow:auto;flex-wrap:nowrap}.property-window table{font-size:10px}.property-footer{flex-wrap:wrap}}@media(max-width:560px){.query-strip{align-items:flex-start}.state-filter{width:100%}.work-area{flex-direction:column}.grid-panel{min-height:330px}.side-actions{width:100%;flex-direction:row;flex-wrap:wrap}.side-actions button{width:auto}.side-actions .close-button{margin:0}.editor fieldset{grid-template-columns:1fr}.property-window{width:100vw;height:100vh;max-height:none}.locator-window{width:100vw;height:100vh}}
</style>
