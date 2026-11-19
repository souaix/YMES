<script setup lang="ts">
import { computed, ref, toRaw } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import ManufacturingOrderMaterials from './ManufacturingOrderMaterials.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'

type IssueState = -1 | 0 | 1 | 2
type MoSource = 0 | 1 | 2 | 3
interface Process { psNo: string; processNo: string; processVersion: string; opNo: string; haveComponent: boolean }
interface Component { componentNo: string; goodQty: number; unitNo: string }
interface Property { propertyNo: string; propertyValue: string }
interface WorkOrder {
  moNo: string; moSource: MoSource; roNo: string; itemNo: number; returnNo: string; customerNo: string
  productNo: string; productVersion: string; engNo: string; engVersion: string; moQty: number; moUnitNo: string
  moTypeNo: number; priority: number; factoryNo: string; customerLotNo: string; planFinishDate: string
  description: string; belongToMoNo: string; issueState: IssueState; moState: number; dataStamp: number
  creator: string; createDate: string; reviser: string; reviseDate: string
  processes: Process[]; components: Component[]; properties: Property[]
  fromErp?: boolean; hasLots?: boolean; hasIssuedMaterial?: boolean
  unreleaseLotQty?: number; erpMoLineNo?: string
  productType?: string; planStartDate?: string; actualStartDate?: string; actualFinishDate?: string
}

const props = defineProps<{ currentUserNo?: string; developmentPreview?: boolean; pageMode?: 'delete' | 'query' | 'start-report' | 'finish-report' | 'materials' }>()
const emit = defineEmits<{ close: [] }>()
const isDev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const deletionOnly = computed(() => props.pageMode === 'delete')
const startReport = computed(() => props.pageMode === 'start-report')
const finishReport = computed(() => props.pageMode === 'finish-report')
const report = computed(() => startReport.value || finishReport.value)
const queryOnly = computed(() => props.pageMode === 'query' || report.value)
const reportAction = computed(() => finishReport.value ? '完工' : '開工')
const pageTitle = computed(() => {
  if (report.value) return '工單達交率（準時' + reportAction.value + '）'
  if (queryOnly.value) return '工單查詢'
  return deletionOnly.value ? '工單刪除' : '工單基本資料'
})
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()
const storageKey = 'ymes-dev-oe201-work-orders-v1'
const sources: { value: MoSource; label: string }[] = [
  { value: 0, label: '訂單' }, { value: 1, label: '退貨' }, { value: 2, label: '無來源' }, { value: 3, label: '參考訂單' },
]
const states: { value: IssueState; label: string }[] = [
  { value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' },
]
const products = ['DEV-PRD-1001', 'DEV-PRD-1002']
const productTypes: Record<string, string> = { 'DEV-PRD-1001': 'DEV-IC', 'DEV-PRD-1002': 'DEV-WAFER' }
const moStateLabels: Record<number, string> = { 0: '未確認', 1: '簽核中', 2: '已確認', 3: '已發放', 6: '在製品退回', 99: '結案' }
const defaultProcess = (): Process => ({ psNo: '', processNo: '', processVersion: '01', opNo: '', haveComponent: false })
const operator = () => props.currentUserNo || 'DEV001'
const now = () => new Date().toISOString()

function blank(): WorkOrder {
  return {
    moNo: '', moSource: 2, roNo: 'N/A', itemNo: -1, returnNo: 'N/A', customerNo: '', productNo: '',
    productVersion: '01', engNo: 'N/A', engVersion: 'N/A', moQty: 1, moUnitNo: 'PCS', moTypeNo: 1, priority: 0,
    factoryNo: 'DEV-FAB', customerLotNo: '', planFinishDate: '', description: '', belongToMoNo: 'N/A',
    issueState: 0, moState: 0, dataStamp: 0, creator: operator(), createDate: now(), reviser: '',
    reviseDate: '', processes: [], components: [], properties: [],
  }
}

function readLocal(): WorkOrder[] {
  if (!isDev.value) return []
  try {
    const saved = localStorage.getItem(storageKey)
    if (saved) {
      const rows = JSON.parse(saved) as unknown
      if (Array.isArray(rows)) return rows as WorkOrder[]
    }
  } catch {
    // A fresh preview can use the example orders when local storage is unreadable.
  }
  const seed = { ...blank(), productNo: 'DEV-PRD-1001', moQty: 25, processes: [
    { ...defaultProcess(), psNo: 'DEV-PS01', processNo: 'DEV-PROC01', opNo: 'DEV-OP01' },
  ], planStartDate: '2026-11-12', planFinishDate: '2026-11-15' }
  return [
    { ...seed, moNo: 'DEV-MO-001' },
    { ...seed, moNo: 'DEV-MO-002', issueState: 1, moState: 1 },
    { ...seed, moNo: 'DEV-MO-003', issueState: 2, moState: 2, fromErp: true, actualStartDate: '2026-11-12' },
    { ...seed, moNo: 'DEV-MO-004', issueState: -1 },
    { ...seed, moNo: 'DEV-MO-005', hasLots: true },
    { ...seed, moNo: 'DEV-MO-006', issueState: 2, moState: 99, unreleaseLotQty: 0, actualStartDate: '2026-11-13', actualFinishDate: '2026-11-15' },
    { ...seed, moNo: 'DEV-MO-007', issueState: 2, moState: 99, actualStartDate: '2026-11-11', actualFinishDate: '2026-11-14' },
    { ...seed, moNo: 'DEV-MO-008', issueState: 2, moState: 99, actualStartDate: '2026-11-12', actualFinishDate: '2026-11-16' },
  ]
}

const rows = ref(readLocal())
const currentState = ref<IssueState>(0)
const search = ref('')
const appliedSearch = ref('')
const activeNo = ref('')
const error = ref('')
const notice = ref('')
const editError = ref('')
const editor = ref<WorkOrder>()
const mode = ref<'add' | 'edit' | 'copy' | 'property'>('add')
const section = ref<'basis' | 'process' | 'components' | 'property'>('basis')
const materials = ref<WorkOrder>()
const selectedNos = ref<string[]>([])
const queryFilters = ref({
  moNo: '', roNo: '', itemNo: '', customerNo: '', productNo: '', productVersion: '',
  engNo: '', engVersion: '', moTypeNo: '', sources: [] as number[], states: [] as number[],
  dateFrom: '', dateTo: '', productTypes: [] as string[],
})
const appliedFilters = ref<typeof queryFilters.value>()
const queryStates = computed(() => Object.entries(moStateLabels)
  .map(([value, label]) => ({ value: Number(value), label }))
  .filter(state => queryOnly.value || state.value <= 3))
const visible = computed(() => rows.value.filter(row => {
  if (!props.pageMode) return row.issueState === currentState.value && row.moNo.toUpperCase().includes(appliedSearch.value)
  const query = appliedFilters.value
  if (!query) return false
  if (report.value) {
    const planDate = (finishReport.value ? row.planFinishDate : row.planStartDate)?.slice(0, 10) ?? ''
    if (query.dateFrom && planDate < query.dateFrom) return false
    if (query.dateTo && (!planDate || planDate > query.dateTo)) return false
    const type = row.productType ?? productTypes[row.productNo] ?? ''
    if (query.productTypes.length && !query.productTypes.includes(type)) return false
  }
  const fields = ['roNo', 'itemNo', 'customerNo', 'productNo', 'productVersion', 'engNo', 'engVersion', 'moTypeNo'] as const
  return row.moNo.toUpperCase().startsWith(query.moNo) &&
    fields.every(key => !query[key] || String(row[key]).toUpperCase() === query[key]) &&
    (!query.sources.length || query.sources.includes(row.moSource)) &&
    (query.states.length ? query.states.includes(row.moState) : queryOnly.value || row.moState >= 0 && row.moState <= 3)
}).sort((a, b) => a.moNo.localeCompare(b.moNo)))
const active = computed(() => visible.value.find(row => row.moNo === activeNo.value) ?? visible.value[0])

function dayDifference(actual?: string, planned?: string): number | null {
  if (!actual || !planned) return null
  const days = (Date.parse(actual) - Date.parse(planned)) / 86_400_000
  return Number.isFinite(days) ? days : null
}

function leadTime(row: WorkOrder): number | null {
  const days = dayDifference(row.actualFinishDate, row.actualStartDate)
  return days === null ? null : days + 1
}

const deliverySummary = computed(() => {
  const counts = { early: 0, onTime: 0, late: 0, missing: 0 }
  let leadDays = 0
  let leadRows = 0
  for (const row of visible.value) {
    const days = finishReport.value
      ? dayDifference(row.actualFinishDate, row.planFinishDate)
      : dayDifference(row.actualStartDate, row.planStartDate)
    if (days === null) counts.missing++
    else if (days < 0) counts.early++
    else if (days > 0) counts.late++
    else counts.onTime++
    const lead = leadTime(row)
    if (lead !== null) { leadDays += lead; leadRows++ }
  }
  return { ...counts, averageLeadTime: leadRows ? leadDays / leadRows : 0 }
})

function displayRate(count: number): string {
  return visible.value.length ? (100 * count / visible.value.length).toFixed(1) + '%' : '0%'
}

function displayDays(days: number | null): string {
  if (days === null) return '—'
  return String(Number(days.toFixed(2)))
}

function applyQueryFilters(): void {
  const query = structuredClone(toRaw(queryFilters.value))
  if (query.dateFrom && query.dateTo && query.dateFrom > query.dateTo) {
    error.value = '起日不可晚於迄日。'
    return
  }
  const fields = ['moNo', 'roNo', 'itemNo', 'customerNo', 'productNo', 'productVersion', 'engNo', 'engVersion', 'moTypeNo'] as const
  for (const key of fields) query[key] = query[key].trim().toUpperCase()
  appliedFilters.value = query
  selectedNos.value = []
  notice.value = ''
  error.value = ''
}

function toggleDeletionSelection(event: Event): void {
  selectedNos.value = (event.target as HTMLInputElement).checked ? visible.value.map(row => row.moNo) : []
}

async function deleteSelected(): Promise<void> {
  if (!deletionOnly.value || !isDev.value) return
  const selection = visible.value.filter(row => selectedNos.value.includes(row.moNo))
  if (!selection.length || !await requestConfirmation('確定處理 ' + selection.length + ' 筆工單的刪除或停用？')) return
  let next = rows.value.slice()
  let finished = 0
  error.value = ''
  notice.value = ''
  for (const row of selection) {
    if (row.issueState === 1 || row.moState >= 3) {
      error.value = row.moNo + ' 的目前狀態不可執行刪除。'
      break
    }
    const suspend = row.fromErp || row.issueState === 2
    if (!suspend && (row.hasLots || row.hasIssuedMaterial)) {
      error.value = row.moNo + (row.hasLots ? ' 已建立生產批。' : ' 仍有已發料記錄。')
      break
    }
    next = suspend ? next.map(item => item === row ? {
      ...item, issueState: -1, dataStamp: item.dataStamp + 1, reviser: operator(), reviseDate: now(),
    } : item) : next.filter(item => item !== row)
    finished++
  }
  if (finished > 0 && !saveLocal(next)) { error.value = '無法儲存本機工單。'; return }
  notice.value = '已完成 ' + finished + ' 筆。'
  selectedNos.value = selection.slice(finished).map(row => row.moNo)
}

function saveLocal(next: WorkOrder[]): boolean {
  if (!isDev.value) return false
  try {
    localStorage.setItem(storageKey, JSON.stringify(next))
    rows.value = next
    return true
  } catch {
    return false
  }
}

function open(nextMode: typeof mode.value): void {
  const row = active.value
  if (nextMode !== 'add' && !row) return
  if (nextMode === 'edit' && row?.issueState !== 0) return
  if (nextMode === 'copy' && row) {
    editor.value = {
      ...structuredClone(toRaw(row)), moNo: '', issueState: 0, moState: 0, dataStamp: 0,
      creator: operator(), createDate: now(), reviser: '', reviseDate: '', belongToMoNo: 'N/A',
      fromErp: false, hasLots: false, hasIssuedMaterial: false,
      unreleaseLotQty: undefined, erpMoLineNo: undefined, actualStartDate: undefined, actualFinishDate: undefined,
    }
  } else {
    editor.value = nextMode === 'add' || !row ? blank() : structuredClone(toRaw(row))
  }
  mode.value = nextMode
  section.value = nextMode === 'property' ? 'property' : 'basis'
  editError.value = ''
}

function save(close: boolean): void {
  if (!editor.value || mode.value === 'property') return
  const row = structuredClone(toRaw(editor.value))
  row.moNo = row.moNo.trim().toUpperCase()
  if (!row.moNo || !row.productNo || !row.factoryNo || !row.moUnitNo) {
    editError.value = '工單、產品、廠別及工單單位必填。'; return
  }
  if (!Number.isFinite(row.moQty) || row.moQty <= 0 || row.moQty > 99_999_999.9999 ||
    !Number.isInteger(row.priority) || !Number.isInteger(row.moTypeNo) ||
    row.priority < 0 || row.priority > 99 || row.moTypeNo < 0 || row.moTypeNo > 99) {
    editError.value = '請確認工單量、類別和優先序。'; return
  }
  if (row.moSource === 0 || row.moSource === 3) {
    row.roNo = row.roNo.trim().toUpperCase()
    if (!row.roNo || row.roNo === 'N/A' || !Number.isInteger(row.itemNo) || row.itemNo < 0 || row.itemNo > 99) {
      editError.value = '訂單來源需要訂單編號與有效項次。'; return
    }
  } else {
    row.roNo = 'N/A'
    row.itemNo = -1
  }
  if (row.moSource === 1 && (!row.returnNo.trim() || row.returnNo === 'N/A')) {
    editError.value = '退貨來源需要退貨編號。'; return
  }
  if (row.moSource !== 1) row.returnNo = 'N/A'
  if (row.processes.some(process => !process.psNo.trim() || !process.processNo.trim())) {
    editError.value = '每筆流程需要生產區段及流程編號。'; return
  }
  const componentNos = row.components.map(component => component.componentNo.trim().toUpperCase())
  const propertyNos = row.properties.map(property => property.propertyNo.trim().toUpperCase())
  if (componentNos.some(no => !no) || new Set(componentNos).size !== componentNos.length ||
    propertyNos.some(no => !no) || new Set(propertyNos).size !== propertyNos.length) {
    editError.value = '元件及屬性編號不可空白或重複。'; return
  }
  if (row.components.some(component =>
    !Number.isFinite(component.goodQty) || component.goodQty < 0 || component.goodQty > 99_999_999.9999 || !component.unitNo.trim(),
  )) {
    editError.value = '請輸入有效的元件良品量及單位。'; return
  }
  row.components.forEach((component, index) => { component.componentNo = componentNos[index]! })
  row.properties.forEach((property, index) => { property.propertyNo = propertyNos[index]! })
  const existing = rows.value.find(item => item.moNo === row.moNo)
  if (mode.value === 'edit' ? existing?.issueState !== 0 || existing.dataStamp !== row.dataStamp : !!existing) {
    editError.value = mode.value === 'edit' ? '工單狀態已變更。' : '工單編號已存在。'; return
  }
  if (existing) { row.dataStamp++; row.reviser = operator(); row.reviseDate = now() }
  if (!saveLocal(existing ? rows.value.map(item => item === existing ? row : item) : [...rows.value, row])) {
    editError.value = '無法儲存本機工單。'; return
  }
  currentState.value = 0
  search.value = ''
  appliedSearch.value = ''
  activeNo.value = row.moNo
  editor.value = close ? undefined : structuredClone(row)
  mode.value = 'edit'
  editError.value = ''
}

async function changeState(action: 'delete' | 'approve' | 'release'): Promise<void> {
  const row = active.value
  if (!row) return
  error.value = ''
  const isApproval = action === 'approve'
  const isRelease = action === 'release'
  if (isApproval && row.issueState !== 0) return
  if (isRelease && (row.issueState !== 2 || row.moState !== 2)) return
  if (action === 'delete' && (row.issueState === 1 || row.moState >= 3)) return
  const hardDelete = action === 'delete' && row.issueState !== 2 && !row.fromErp
  if (hardDelete && (row.hasLots || row.hasIssuedMaterial)) {
    error.value = row.hasLots ? '此工單已建立生產批。' : '此工單仍有已發料記錄。'
    return
  }
  if (action !== 'delete') {
    if (!row.processes.length) { error.value = '工單至少需要一個流程。'; return }
    if (row.processes.some(process => process.haveComponent) && !row.components.length) {
      error.value = '需要元件的流程尚未指定元件。'; return
    }
    if (row.components.length > row.moQty) { error.value = '元件數量不可超過工單量。'; return }
  }
  const label = isApproval ? '核准' : isRelease ? '發放' : hardDelete ? '刪除' : '停用'
  if (!await requestConfirmation('確定' + label + '工單 ' + row.moNo + '？')) return
  const next = rows.value.flatMap(item => {
    if (item !== row) return [item]
    if (hardDelete) return []
    return [{
      ...row, issueState: isApproval || isRelease ? 2 as const : -1 as const,
      moState: isApproval ? 2 : isRelease ? 3 : row.moState,
      dataStamp: row.dataStamp + 1, reviser: operator(), reviseDate: now(),
    }]
  })
  if (!saveLocal(next)) error.value = '無法儲存本機工單。'
}

function apply(event: MouseEvent): void {
  if ((event.currentTarget as HTMLButtonElement).form?.reportValidity()) save(false)
}
</script>

<template>
  <ManufacturingOrderMaterials v-if="pageMode === 'materials'" class="mo-page" :orders="rows" :development-preview="isDev" @close="emit('close')" />
  <section v-else class="mo-page">
    <h2>{{ pageTitle }}</h2>
    <template v-if="isDev">
      <form v-if="pageMode" class="filter-form" @submit.prevent="applyQueryFilters">
        <div class="toolbar">
          <label>工單編號 <input v-model="queryFilters.moNo" maxlength="50"></label>
          <label>訂單編號 <input v-model="queryFilters.roNo" maxlength="25"></label>
          <label>訂單項次 <input v-model="queryFilters.itemNo" type="text" inputmode="numeric" maxlength="2"></label>
          <label>客戶編號 <input v-model="queryFilters.customerNo" maxlength="50"></label>
          <label>產品 <input v-model="queryFilters.productNo" maxlength="50"></label>
          <label>產品版本 <input v-model="queryFilters.productVersion" maxlength="5"></label>
          <label>工程編號 <input v-model="queryFilters.engNo" maxlength="30"></label>
          <label>工程版本 <input v-model="queryFilters.engVersion" maxlength="3"></label>
          <label>工單類別 <input v-model="queryFilters.moTypeNo" type="text" inputmode="numeric" maxlength="2"></label>
        </div>
        <div class="toolbar">
          <template v-if="report">
            <label>計畫{{ reportAction }}起日 <input v-model="queryFilters.dateFrom" type="date"></label>
            <label>計畫{{ reportAction }}迄日 <input v-model="queryFilters.dateTo" type="date"></label>
            <label>產品類別 <select v-model="queryFilters.productTypes" multiple><option v-for="type in Object.values(productTypes)" :key="type">{{ type }}</option></select></label>
          </template>
          <fieldset class="toolbar"><legend>來源</legend>
            <label v-for="source in sources" :key="source.value"><input v-model="queryFilters.sources" :value="source.value" type="checkbox"> {{ source.label }}</label>
          </fieldset>
          <fieldset class="toolbar"><legend>工單狀態</legend>
            <label v-for="state in queryStates" :key="state.value"><input v-model="queryFilters.states" :value="state.value" type="checkbox"> {{ state.label }}</label>
          </fieldset>
          <button type="submit">查詢</button>
        </div>
      </form>
      <form v-else class="toolbar" @submit.prevent="appliedSearch = search.trim().toUpperCase(); activeNo = ''; error = ''">
        <label>工單編號 <input v-model="search" maxlength="50"></label><button type="submit">查詢</button>
        <button v-for="state in states" :key="state.value" type="button" :aria-pressed="currentState === state.value" @click="currentState = state.value; activeNo = ''; error = ''">{{ state.label }}</button>
      </form>
      <p v-if="error" role="alert">{{ error }}</p>
      <p v-if="notice" role="status">{{ notice }}</p>
      <dl v-if="report && appliedFilters" class="summary" :aria-label="reportAction + '統計'">
        <div><dt>工單總數</dt><dd>{{ visible.length }}</dd></div>
        <div><dt>準時</dt><dd>{{ deliverySummary.onTime }} 筆（{{ displayRate(deliverySummary.onTime) }}）</dd></div>
        <div><dt>提前</dt><dd>{{ deliverySummary.early }} 筆（{{ displayRate(deliverySummary.early) }}）</dd></div>
        <div><dt>延遲</dt><dd>{{ deliverySummary.late }} 筆（{{ displayRate(deliverySummary.late) }}）</dd></div>
        <div><dt>缺少計畫或實際日期</dt><dd>{{ deliverySummary.missing }}</dd></div>
        <div v-if="finishReport"><dt>平均生產天數</dt><dd>{{ displayDays(deliverySummary.averageLeadTime) }}</dd></div>
      </dl>
      <div class="workspace">
        <div class="grid"><table>
          <thead><tr>
            <th>
              <input v-if="deletionOnly" type="checkbox" aria-label="選取查詢結果" :disabled="!visible.length" :checked="visible.length > 0 && selectedNos.length === visible.length" @change="toggleDeletionSelection">
              <template v-else>選取</template>
            </th>
            <th>工單編號</th><th>來源</th><th>訂單</th><th>廠別</th><th>客戶</th><th>產品</th><th>版本</th><th>工單量</th><th>單位</th><th>工單狀態</th><th v-if="pageMode">簽核狀態</th><th>計畫完工</th><th>說明</th>
            <template v-if="queryOnly">
              <th>訂單項次</th><th>未開批量</th><th>工程編號</th><th>工程版本</th><th>工單類別</th><th>優先序</th><th>父工單</th><th>客戶批號</th><th>ERP 行號</th>
            </template>
            <template v-if="report"><th>計畫開工</th><th>實際開工</th><th>開工差異天數</th><th>實際完工</th><th>完工差異天數</th></template>
            <th v-if="finishReport">生產天數</th>
          </tr></thead>
          <tbody>
            <tr v-for="row in visible" :key="row.moNo" :class="{ selected: deletionOnly ? selectedNos.includes(row.moNo) : active?.moNo === row.moNo }" @click="activeNo = row.moNo">
              <td>
                <input v-if="deletionOnly" v-model="selectedNos" type="checkbox" :value="row.moNo" :aria-label="'選取 ' + row.moNo">
                <input v-else type="radio" name="selected-work-order" :checked="active?.moNo === row.moNo" :aria-label="'選取 ' + row.moNo" @change="activeNo = row.moNo">
              </td>
              <td>{{ row.moNo }}</td><td>{{ sources.find(item => item.value === row.moSource)?.label }}</td><td>{{ row.roNo }}</td><td>{{ row.factoryNo }}</td><td>{{ row.customerNo }}</td><td>{{ row.productNo }}</td><td>{{ row.productVersion }}</td><td>{{ row.moQty }}</td><td>{{ row.moUnitNo }}</td><td>{{ moStateLabels[row.moState] }}</td>
              <td v-if="pageMode">{{ states.find(item => item.value === row.issueState)?.label }}</td>
              <td>{{ row.planFinishDate }}</td><td>{{ row.description }}</td>
              <template v-if="queryOnly">
                <td>{{ row.itemNo }}</td><td>{{ row.unreleaseLotQty ?? row.moQty }}</td><td>{{ row.engNo }}</td><td>{{ row.engVersion }}</td>
                <td>{{ row.moTypeNo }}</td><td>{{ row.priority }}</td><td>{{ row.belongToMoNo }}</td><td>{{ row.customerLotNo }}</td><td>{{ row.erpMoLineNo }}</td>
              </template>
              <template v-if="report">
                <td>{{ row.planStartDate }}</td><td>{{ row.actualStartDate }}</td><td>{{ displayDays(dayDifference(row.actualStartDate, row.planStartDate)) }}</td>
                <td>{{ row.actualFinishDate }}</td><td>{{ displayDays(dayDifference(row.actualFinishDate, row.planFinishDate)) }}</td>
              </template>
              <td v-if="finishReport">{{ displayDays(leadTime(row)) }}</td>
            </tr>
            <tr v-if="!visible.length"><td :colspan="finishReport ? 29 : startReport ? 28 : queryOnly ? 23 : deletionOnly ? 14 : 13">{{ pageMode && !appliedFilters ? '請輸入查詢條件或查詢全部' : '沒有符合條件的工單' }}</td></tr>
          </tbody>
        </table></div>
        <div v-if="deletionOnly" class="actions">
          <button type="button" :disabled="!selectedNos.length" @click="deleteSelected">刪除</button>
        </div>
        <div v-else-if="queryOnly" class="actions">
          <button type="button" :disabled="!active" @click="open('property')">屬性</button>
        </div>
        <div v-else class="actions">
          <button type="button" @click="open('add')">新增</button>
          <button type="button" :disabled="active?.issueState !== 0" @click="open('edit')">修改</button>
          <button type="button" :disabled="!active || active.issueState === 1 || active.moState >= 3" @click="changeState('delete')">刪除</button>
          <button type="button" :disabled="!active" @click="open('copy')">複製</button>
          <button type="button" :disabled="active?.issueState !== 0" @click="changeState('approve')">核准</button>
          <button type="button" :disabled="!active" @click="materials = active">用料清單</button>
          <button type="button" :disabled="active?.issueState !== 2 || active?.moState !== 2" @click="changeState('release')">發放</button>
          <button type="button" :disabled="!active" @click="open('property')">屬性</button>
        </div>
      </div>
      <p>資料總筆數 {{ visible.length }}</p>
    </template>
    <p v-else role="status">此功能尚未接入正式服務。</p>
    <button type="button" @click="emit('close')">關閉</button>

    <div v-if="editor" class="overlay">
      <form class="dialog" role="dialog" aria-modal="true" aria-label="工單定義" @submit.prevent="save(true)">
        <h3>{{ mode === 'property' ? '工單屬性' : '工單定義' }} {{ editor.moNo }}</h3>
        <nav v-if="mode !== 'property'" class="toolbar" aria-label="工單設定">
          <button type="button" :aria-pressed="section === 'basis'" @click="section = 'basis'">基本資料</button>
          <button type="button" :aria-pressed="section === 'process'" @click="section = 'process'">流程</button>
          <button type="button" :aria-pressed="section === 'components'" @click="section = 'components'">元件</button>
          <button type="button" :aria-pressed="section === 'property'" @click="section = 'property'">屬性</button>
        </nav>
        <div v-show="section === 'basis'" class="fields">
          <label>工單編號 <input v-model="editor.moNo" maxlength="50" :readonly="mode === 'edit'"></label>
          <label>來源 <select v-model.number="editor.moSource"><option v-for="source in sources" :key="source.value" :value="source.value">{{ source.label }}</option></select></label>
          <template v-if="editor.moSource === 0 || editor.moSource === 3">
            <label>訂單編號 <input v-model="editor.roNo" maxlength="25"></label>
            <label>訂單項次 <input v-model.number="editor.itemNo" type="number" min="0" max="99"></label>
          </template>
          <label v-if="editor.moSource === 1">退貨編號 <input v-model="editor.returnNo" maxlength="20"></label>
          <label>廠別 <input v-model="editor.factoryNo" maxlength="20"></label>
          <label>客戶編號 <input v-model="editor.customerNo" maxlength="50"></label>
          <label>產品 <select v-model="editor.productNo"><option value="">請選擇</option><option v-for="product in products" :key="product">{{ product }}</option></select></label>
          <label>產品版本 <input v-model="editor.productVersion" maxlength="5"></label>
          <label>工程編號 <input v-model="editor.engNo" maxlength="30"></label>
          <label>工程版本 <input v-model="editor.engVersion" maxlength="3"></label>
          <label>工單量 <input v-model.number="editor.moQty" type="number" min="0.0001" max="99999999.9999" step="0.0001"></label>
          <label>工單單位 <select v-model="editor.moUnitNo"><option>PCS</option><option>SET</option></select></label>
          <label>工單類別 <input v-model.number="editor.moTypeNo" type="number" min="0" max="99"></label>
          <label>優先序 <input v-model.number="editor.priority" type="number" min="0" max="99"></label>
          <label>客戶批號 <input v-model="editor.customerLotNo" maxlength="50"></label>
          <label>計畫開工日 <input v-model="editor.planStartDate" type="date"></label>
          <label>計畫完工日 <input v-model="editor.planFinishDate" type="date"></label>
          <label class="wide">說明 <textarea v-model="editor.description" maxlength="1000"></textarea></label>
        </div>
        <fieldset v-if="section === 'process'">
          <legend>流程（依加入次序）</legend>
          <div v-for="(process, index) in editor.processes" :key="index" class="child-row">
            <label>生產區段 <input v-model="process.psNo" maxlength="50"></label><label>流程 <input v-model="process.processNo" maxlength="30"></label>
            <label>版本 <input v-model="process.processVersion" maxlength="3"></label><label>作業站 <input v-model="process.opNo" maxlength="50"></label>
            <label><input v-model="process.haveComponent" type="checkbox"> 需要元件</label><button type="button" @click="editor.processes.splice(index, 1)">移除</button>
          </div>
          <button type="button" :disabled="editor.processes.length >= 99" @click="editor.processes.push(defaultProcess())">新增流程</button>
        </fieldset>
        <fieldset v-if="section === 'components'">
          <legend>元件</legend>
          <div v-for="(component, index) in editor.components" :key="index" class="child-row">
            <label>元件編號 <input v-model="component.componentNo" maxlength="50"></label><label>良品量 <input v-model.number="component.goodQty" type="number" min="0" max="99999999.9999" step="0.0001"></label>
            <label>單位 <input v-model="component.unitNo" maxlength="30"></label><button type="button" @click="editor.components.splice(index, 1)">移除</button>
          </div>
          <button type="button" @click="editor.components.push({ componentNo: '', goodQty: 1, unitNo: editor.moUnitNo })">新增元件</button>
        </fieldset>
        <fieldset v-if="section === 'property'">
          <legend>屬性</legend>
          <p v-if="!editor.properties.length && mode === 'property'">沒有屬性</p>
          <div v-for="(property, index) in editor.properties" :key="index" class="child-row">
            <label>屬性編號 <input v-model="property.propertyNo" maxlength="20" :readonly="mode === 'property'"></label>
            <label>屬性值 <input v-model="property.propertyValue" maxlength="255" :readonly="mode === 'property'"></label>
            <button v-if="mode !== 'property'" type="button" @click="editor.properties.splice(index, 1)">移除</button>
          </div>
          <button v-if="mode !== 'property'" type="button" @click="editor.properties.push({ propertyNo: '', propertyValue: '' })">新增屬性</button>
        </fieldset>
        <p v-if="editError" role="alert">{{ editError }}</p>
        <footer><button v-if="mode !== 'property'" type="submit">確認</button><button v-if="mode !== 'property'" type="button" @click="apply">套用</button><button type="button" @click="editor = undefined">關閉</button></footer>
      </form>
    </div>
    <div v-if="materials" class="overlay">
      <ManufacturingOrderMaterials class="dialog materials-dialog" role="dialog" aria-modal="true" aria-label="工單用料清單"
        :orders="rows" :initial-mo-no="materials.moNo" :development-preview="isDev" @close="materials = undefined" />
    </div>
    <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
  </section>
</template>

<style scoped>
.mo-page { display: grid; align-content: start; gap: 12px; padding: 16px; color: #253244; }
h2, h3, p { margin: 0; }
.toolbar, .child-row, footer { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; }
.filter-form { display: grid; gap: 8px; }
.summary { display: flex; gap: 12px; flex-wrap: wrap; margin: 0; }
.summary div { min-width: 110px; border: 1px solid #bcc7d7; border-radius: 4px; padding: 8px 12px; }
.summary dd { margin: 4px 0 0; font-weight: 600; }
.child-row { margin-bottom: 10px; }
.workspace { display: grid; grid-template-columns: minmax(0, 1fr) auto; gap: 12px; }
.actions { display: flex; flex-direction: column; gap: 8px; }
.grid { overflow: auto; border: 1px solid #bcc7d7; background: white; max-height: 54vh; }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
th, td { padding: 7px 9px; border-bottom: 1px solid #dce4ed; white-space: nowrap; text-align: left; }
th { background: #eef3f9; }
.selected td, button[aria-pressed="true"] { background: #d5e7fa; }
button, input, select, textarea { font: inherit; }
button { width: max-content; min-height: 30px; padding: 3px 10px; border: 1px solid #b8c5d6; border-radius: 4px; background: #f7f9fc; color: inherit; }
button:disabled { opacity: .55; }
.actions button { width: 100%; }
.overlay { position: fixed; inset: 0; z-index: 150; display: grid; place-items: center; background: #21334b55; padding: 18px; }
.dialog { width: min(760px, 100%); max-height: 90vh; overflow: auto; display: grid; gap: 14px; background: white; padding: 18px; border: 1px solid #8fa4bf; border-radius: 6px; }
.materials-dialog { width: min(1050px, 100%); }
.fields { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px; }
.fields label { display: grid; gap: 3px; }
.wide { grid-column: 1 / -1; }
.fields :is(input, select, textarea) { width: 100%; }
footer { justify-content: flex-end; }
[role="alert"] { color: #9d2727; }
</style>
