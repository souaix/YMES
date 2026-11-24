<script setup lang="ts">
import { computed, ref } from 'vue'

const emit = defineEmits<{ close: [] }>()

type FromPlaceType = 2 | 3 | 5 | 6 | 7
interface Basis {
  materialReturnNo: string
  fromPlaceType: FromPlaceType
  fromInventoryNo: string
  inventoryNo: string
  state: number
  description: string
  creator: string
  createDate: string
  returnDate: string
}
interface Detail {
  key: string
  materialReturnNo: string
  materialNo: string
  materialLotNo: string
  locatorNo: string
  unitNo: string
  qty: number
  componentIds: string[]
}
interface Component {
  componentId: string
  materialLotNo: string
  componentNo: string
  goodQty: number
  scrapQty: number
  returnInventoryNo: string
}
interface Posting {
  key: string
  kind: 'material' | 'component'
  source: string
  destination: string
  materialNo: string
  materialLotNo: string
  locatorNo: string
  unitNo: string
  qty: number
  componentNo: string
  goodQty: number
  scrapQty: number
}
interface Filters {
  materialReturnNo: string
  inventoryNo: string
  fromInventoryNo: string
  fromMONo: string
  fromPlaceType: FromPlaceType | ''
  fromCreateDate: string
  endCreateDate: string
  useMaterialReturnNo: boolean
  useInventoryNo: boolean
  useFromInventoryNo: boolean
  useFromMONo: boolean
  useFromPlaceType: boolean
  useFromCreateDate: boolean
  useEndCreateDate: boolean
}
interface DevState {
  bases: Basis[]
  lastConfirmation: { materialReturnNo: string; totalQty: number; detailCount: number; componentCount: number; confirmDate: string; postings: Posting[] } | null
}

const storageKey = 'ymes.dev.iv304-wip-return-confirm.v1'
const stateLabels: Record<number, string> = { 0: 'UnConfirm', 1: 'Handling', 2: 'Confirm', 3: 'Failed' }
const fromPlaceLabels: Record<FromPlaceType, string> = { 2: 'WIP Inventory', 3: 'MO', 5: 'WIP Semi', 6: 'MO Semi', 7: 'MO Material + Semi' }
const fromPlaceTypes: FromPlaceType[] = [2, 3, 5, 6, 7]
const targetInventoryOptions = [
  { inventoryNo: 'RAW-A', name: '原料庫 A' },
  { inventoryNo: 'RAW-B', name: '原料庫 B' },
]
const wipInventoryOptions = [
  { inventoryNo: 'WIP-RAW-01', name: '在製原料庫 A' },
  { inventoryNo: 'WIP-RAW-02', name: '在製原料庫 B' },
  { inventoryNo: 'WIP-SEMI-01', name: '在製半成品庫 A' },
]
const initialBases: Basis[] = [
  { materialReturnNo: 'MWR-DEV-0001', fromPlaceType: 2, fromInventoryNo: 'WIP-RAW-01', inventoryNo: 'RAW-A', state: 0, description: 'DEV：在製原料退回原料庫。', creator: 'DEV001', createDate: '2026-11-21 09:25:00', returnDate: '' },
  { materialReturnNo: 'MWR-DEV-0002', fromPlaceType: 3, fromInventoryNo: 'MO-DEV-101', inventoryNo: 'RAW-A', state: 0, description: 'DEV：工單原料退料，含元件。', creator: 'DEV002', createDate: '2026-11-21 11:10:00', returnDate: '' },
  { materialReturnNo: 'MWR-DEV-0003', fromPlaceType: 5, fromInventoryNo: 'WIP-SEMI-01', inventoryNo: 'SEMI-A', state: 0, description: 'DEV：在製半成品退料。', creator: 'DEV001', createDate: '2026-11-22 14:05:00', returnDate: '' },
  { materialReturnNo: 'MWR-DEV-0004', fromPlaceType: 6, fromInventoryNo: 'MO-DEV-201', inventoryNo: 'SEMI-A', state: 0, description: 'DEV：工單半成品退料。', creator: 'DEV004', createDate: '2026-11-23 10:40:00', returnDate: '' },
  { materialReturnNo: 'MWR-DEV-0005', fromPlaceType: 7, fromInventoryNo: 'MO-DEV-301', inventoryNo: 'N/A', state: 0, description: 'DEV：工單混合物料／半成品退料。', creator: 'DEV004', createDate: '2026-11-23 16:20:00', returnDate: '' },
]
const detailRows: Detail[] = [
  { key: 'iv304-d-01', materialReturnNo: 'MWR-DEV-0001', materialNo: 'RM-SENSOR-05', materialLotNo: 'WIP-SNS-2611-01', locatorNo: 'WR-A-01', unitNo: 'EA', qty: 2, componentIds: ['iv304-c-01', 'iv304-c-02'] },
  { key: 'iv304-d-02', materialReturnNo: 'MWR-DEV-0001', materialNo: 'RM-RESIN-01', materialLotNo: 'WIP-RES-2611-03', locatorNo: 'WR-A-02', unitNo: 'KG', qty: 5.5, componentIds: [] },
  { key: 'iv304-d-03', materialReturnNo: 'MWR-DEV-0002', materialNo: 'RM-CHIP-11', materialLotNo: 'MO101-CHIP-01', locatorNo: 'MO-A-01', unitNo: 'EA', qty: 1, componentIds: ['iv304-c-03'] },
  { key: 'iv304-d-04', materialReturnNo: 'MWR-DEV-0003', materialNo: 'PRD-CTRL-02', materialLotNo: 'SEMI-2611-08', locatorNo: 'WS-A-02', unitNo: 'EA', qty: 1, componentIds: ['iv304-c-04'] },
  { key: 'iv304-d-05', materialReturnNo: 'MWR-DEV-0004', materialNo: 'PRD-PANEL-07', materialLotNo: 'MO201-PNL-03', locatorNo: 'MS-A-01', unitNo: 'EA', qty: 2, componentIds: [] },
  { key: 'iv304-d-06', materialReturnNo: 'MWR-DEV-0005', materialNo: 'RM-SENSOR-05', materialLotNo: 'MO301-SNS-01', locatorNo: 'MO-MIX-01', unitNo: 'EA', qty: 1, componentIds: ['iv304-c-05'] },
]
const componentRows: Component[] = [
  { componentId: 'iv304-c-01', materialLotNo: 'WIP-SNS-2611-01', componentNo: 'WIP-CMP-SNS-A', goodQty: 1, scrapQty: 0, returnInventoryNo: 'RAW-A' },
  { componentId: 'iv304-c-02', materialLotNo: 'WIP-SNS-2611-01', componentNo: 'WIP-CMP-SNS-B', goodQty: 1, scrapQty: 0, returnInventoryNo: 'RAW-A' },
  { componentId: 'iv304-c-03', materialLotNo: 'MO101-CHIP-01', componentNo: 'MO101-CMP-001', goodQty: 1, scrapQty: 0, returnInventoryNo: 'RAW-A' },
  { componentId: 'iv304-c-04', materialLotNo: 'SEMI-2611-08', componentNo: 'SEMI-CMP-001', goodQty: 1, scrapQty: 0, returnInventoryNo: 'SEMI-A' },
  { componentId: 'iv304-c-05', materialLotNo: 'MO301-SNS-01', componentNo: 'MIX-CMP-001', goodQty: 1, scrapQty: 0, returnInventoryNo: 'RAW-A' },
]

function dateText(date: Date): string { return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}` }
function dateOffset(days: number): string { const date = new Date(); date.setDate(date.getDate() + days); return dateText(date) }
function emptyFilters(): Filters {
  return {
    materialReturnNo: '', inventoryNo: '', fromInventoryNo: '', fromMONo: '', fromPlaceType: '',
    fromCreateDate: dateOffset(-14), endCreateDate: dateOffset(0),
    useMaterialReturnNo: false, useInventoryNo: false, useFromInventoryNo: false, useFromMONo: false,
    useFromPlaceType: false, useFromCreateDate: false, useEndCreateDate: false,
  }
}
function initialState(): DevState { return { bases: initialBases.map((row) => ({ ...row })), lastConfirmation: null } }
function readState(): DevState {
  try {
    const saved = localStorage.getItem(storageKey)
    if (saved) {
      const value = JSON.parse(saved) as DevState
      if (Array.isArray(value.bases)) {
        const lastConfirmation = value.lastConfirmation
          ? { ...value.lastConfirmation, postings: Array.isArray(value.lastConfirmation.postings) ? value.lastConfirmation.postings : [] }
          : null
        return { bases: value.bases, lastConfirmation }
      }
    }
  } catch { /* Keep the DEV fixtures usable if browser storage is unavailable. */ }
  return initialState()
}

const records = ref<DevState>(readState())
const filters = ref<Filters>(emptyFilters())
const appliedFilters = ref<Filters>(emptyFilters())
const hasQueried = ref(false)
const activeBasisNo = ref('')
const expandedDetailKeys = ref<string[]>([])
const confirmationOpen = ref(false)
const notice = ref('畫面開啟時不會自動查詢；此頁僅使用 DEV 假資料。')
const visibleBases = computed(() => {
  if (!hasQueried.value) return []
  const value = appliedFilters.value
  return records.value.bases.filter((row) => {
    if (row.state !== 0) return false
    if (value.useMaterialReturnNo && value.materialReturnNo && !row.materialReturnNo.toLowerCase().includes(value.materialReturnNo.toLowerCase())) return false
    if (value.useInventoryNo && value.inventoryNo && row.inventoryNo !== value.inventoryNo) return false
    const fromNo = value.useFromMONo && value.fromMONo ? value.fromMONo : value.useFromInventoryNo ? value.fromInventoryNo : ''
    if (fromNo && row.fromInventoryNo !== fromNo) return false
    if (value.useFromPlaceType && value.fromPlaceType !== '' && row.fromPlaceType !== value.fromPlaceType) return false
    if (value.useFromCreateDate && row.createDate.slice(0, 10) < value.fromCreateDate) return false
    if (value.useEndCreateDate && row.createDate.slice(0, 10) > value.endCreateDate) return false
    return true
  })
})
const activeBasis = computed(() => visibleBases.value.find((row) => row.materialReturnNo === activeBasisNo.value) ?? null)
const visibleDetails = computed(() => detailRows.filter((row) => row.materialReturnNo === activeBasis.value?.materialReturnNo))
const totalQty = computed(() => visibleDetails.value.reduce((sum, row) => sum + row.qty, 0))
const totalComponents = computed(() => visibleDetails.value.reduce((sum, row) => sum + row.componentIds.length, 0))

function persist(message: string): void {
  try { localStorage.setItem(storageKey, JSON.stringify(records.value)) } catch { /* Keep the current preview usable for this browser session. */ }
  notice.value = message
}
function runQuery(): void {
  appliedFilters.value = { ...filters.value }
  hasQueried.value = true
  const first = visibleBases.value[0]
  activeBasisNo.value = first?.materialReturnNo ?? ''
  expandTicket(first?.materialReturnNo ?? '')
  notice.value = `查詢完成：State=UnConfirm，${visibleBases.value.length} 筆。DEV 假資料不呼叫正式 API。`
}
function clearFilters(): void {
  filters.value = emptyFilters()
  appliedFilters.value = emptyFilters()
  hasQueried.value = false
  activeBasisNo.value = ''
  expandedDetailKeys.value = []
  notice.value = '已清除條件；請按 Query 載入結果。'
}
function resetDemo(): void {
  records.value = initialState()
  filters.value = emptyFilters()
  appliedFilters.value = emptyFilters()
  hasQueried.value = false
  activeBasisNo.value = ''
  expandedDetailKeys.value = []
  confirmationOpen.value = false
  persist('已重設 DEV 假資料。按 Query 後會重新列出未確認單據。')
}
function setSourceMode(mode: 'wip' | 'mo', checked: boolean): void {
  if (mode === 'wip') {
    filters.value.useFromInventoryNo = checked
    if (checked) { filters.value.useFromMONo = false; filters.value.fromMONo = '' }
  } else {
    filters.value.useFromMONo = checked
    if (checked) { filters.value.useFromInventoryNo = false; filters.value.fromInventoryNo = '' }
  }
}
function setMaterialReturnNo(value: string): void { filters.value.materialReturnNo = value; filters.value.useMaterialReturnNo = !!value }
function setInventoryNo(value: string): void { filters.value.inventoryNo = value; filters.value.useInventoryNo = !!value }
function setFromInventoryNo(value: string): void { filters.value.fromInventoryNo = value; setSourceMode('wip', !!value) }
function setFromMONo(value: string): void { filters.value.fromMONo = value; setSourceMode('mo', !!value) }
function setFromPlaceType(value: string): void { filters.value.fromPlaceType = value === '' ? '' : Number(value) as FromPlaceType; filters.value.useFromPlaceType = value !== '' }
function selectBasis(row: Basis): void { activeBasisNo.value = row.materialReturnNo; expandTicket(row.materialReturnNo) }
function expandTicket(no: string): void { expandedDetailKeys.value = detailRows.filter((row) => row.materialReturnNo === no && row.componentIds.length > 0).map((row) => row.key) }
function toggleExpand(key: string): void { expandedDetailKeys.value = expandedDetailKeys.value.includes(key) ? expandedDetailKeys.value.filter((item) => item !== key) : [...expandedDetailKeys.value, key] }
function componentsFor(detail: Detail): Component[] { return componentRows.filter((row) => detail.componentIds.includes(row.componentId)) }
function formatQty(value: number): string { return new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 8 }).format(value) }
function displayFromPlaceType(value: FromPlaceType): string { return fromPlaceLabels[value] }
function formatNow(): string { return new Date().toLocaleString('sv-SE', { hour12: false }).replace('T', ' ') }
function openConfirmation(): void {
  if (!activeBasis.value) { notice.value = '請先查詢並選取單據。'; return }
  if (activeBasis.value.state !== 0) { notice.value = '只有 UnConfirm 單據可以執行退料確認。'; return }
  confirmationOpen.value = true
}
function confirmReturn(): void {
  const basis = activeBasis.value
  if (!basis || basis.state !== 0) { confirmationOpen.value = false; notice.value = '單據狀態已改變，請重新查詢。'; return }
  const confirmDate = formatNow()
  const postings = visibleDetails.value.flatMap((detail): Posting[] => {
    const components = componentsFor(detail)
    const componentTargets = [...new Set(components.map((component) => component.returnInventoryNo))]
    const materialPosting: Posting = {
      key: `${detail.key}:material`, kind: 'material', source: `${fromPlaceLabels[basis.fromPlaceType]} · ${basis.fromInventoryNo}`,
      destination: basis.fromPlaceType === 7 ? componentTargets.join(', ') || '依元件目標' : basis.inventoryNo,
      materialNo: detail.materialNo, materialLotNo: detail.materialLotNo, locatorNo: detail.locatorNo, unitNo: detail.unitNo,
      qty: detail.qty, componentNo: '', goodQty: 0, scrapQty: 0,
    }
    const componentPostings = components.map((component): Posting => ({
      key: `${detail.key}:${component.componentId}`, kind: 'component', source: `${fromPlaceLabels[basis.fromPlaceType]} · ${basis.fromInventoryNo}`,
      destination: component.returnInventoryNo, materialNo: detail.materialNo, materialLotNo: detail.materialLotNo,
      locatorNo: detail.locatorNo, unitNo: 'component', qty: 0, componentNo: component.componentNo,
      goodQty: component.goodQty, scrapQty: component.scrapQty,
    }))
    return [materialPosting, ...componentPostings]
  })
  const summary = { materialReturnNo: basis.materialReturnNo, totalQty: totalQty.value, detailCount: visibleDetails.value.length, componentCount: totalComponents.value, confirmDate, postings }
  records.value.bases = records.value.bases.map((row) => row.materialReturnNo === basis.materialReturnNo ? { ...row, state: 2, returnDate: confirmDate } : row)
  records.value.lastConfirmation = summary
  confirmationOpen.value = false
  runQuery()
  notice.value = `DEV 已模擬確認 ${summary.materialReturnNo}：${formatQty(summary.totalQty)} 件量、${summary.componentCount} 個元件過帳；單據已離開 UnConfirm 清單。`
  try { localStorage.setItem(storageKey, JSON.stringify(records.value)) } catch { /* Keep the current preview usable for this browser session. */ }
}
</script>

<template>
  <main class="iv304-page" aria-label="退料單確認(WIP)">
    <header class="iv304-header">
      <div><div class="iv304-kicker">INVENTORY · IV304</div><h1>WIP RETURN CONFIRM <span>退料單確認(WIP)</span></h1></div>
      <div class="iv304-header-actions"><span class="iv304-state iv304-state-0">只列 UnConfirm</span><button type="button" class="iv304-button" @click="emit('close')">Close (X)</button></div>
    </header>

    <section class="iv304-query-panel" aria-label="退料單查詢條件">
      <div class="iv304-query-title"><h2>Query</h2><span>確認使用 Source=10；正式庫存不在此 DEV 頁修改</span></div>
      <div class="iv304-query-grid">
        <label class="iv304-filter"><span><input v-model="filters.useMaterialReturnNo" type="checkbox"> Return No</span><input :value="filters.materialReturnNo" type="search" placeholder="包含查詢" @input="setMaterialReturnNo(($event.target as HTMLInputElement).value)"></label>
        <label class="iv304-filter"><span><input v-model="filters.useInventoryNo" type="checkbox"> Inventory No</span><select :value="filters.inventoryNo" @change="setInventoryNo(($event.target as HTMLSelectElement).value)"><option value="">全部</option><option v-for="item in targetInventoryOptions" :key="item.inventoryNo" :value="item.inventoryNo">{{ item.inventoryNo }} · {{ item.name }}</option></select></label>
        <label class="iv304-filter"><span><input v-model="filters.useFromPlaceType" type="checkbox"> From Place Type</span><select :value="filters.fromPlaceType" @change="setFromPlaceType(($event.target as HTMLSelectElement).value)"><option value="">全部</option><option v-for="type in fromPlaceTypes" :key="type" :value="type">{{ fromPlaceLabels[type] }} ({{ type }})</option></select></label>
        <label class="iv304-filter iv304-state-filter"><span><input type="checkbox" checked disabled> State</span><input value="UnConfirm (0)" disabled></label>
        <label class="iv304-filter"><span><input v-model="filters.useFromInventoryNo" type="checkbox" @change="setSourceMode('wip', ($event.target as HTMLInputElement).checked)"> From Return Inventory No</span><select :value="filters.fromInventoryNo" :disabled="filters.useFromMONo" @change="setFromInventoryNo(($event.target as HTMLSelectElement).value)"><option value="">全部</option><option v-for="item in wipInventoryOptions" :key="item.inventoryNo" :value="item.inventoryNo">{{ item.inventoryNo }} · {{ item.name }}</option></select></label>
        <label class="iv304-filter"><span><input v-model="filters.useFromMONo" type="checkbox" @change="setSourceMode('mo', ($event.target as HTMLInputElement).checked)"> From Return MO No</span><input :value="filters.fromMONo" :disabled="!filters.useFromMONo" type="search" placeholder="輸入工單號" @input="setFromMONo(($event.target as HTMLInputElement).value)"></label>
        <label class="iv304-filter"><span><input v-model="filters.useFromCreateDate" type="checkbox"> From Create Date</span><input v-model="filters.fromCreateDate" type="date" @change="filters.useFromCreateDate = true"></label>
        <label class="iv304-filter"><span><input v-model="filters.useEndCreateDate" type="checkbox"> End Create Date</span><input v-model="filters.endCreateDate" type="date" @change="filters.useEndCreateDate = true"></label>
      </div>
      <div class="iv304-query-actions"><button type="button" class="iv304-button iv304-button-primary" @click="runQuery">Query</button><button type="button" class="iv304-button" @click="clearFilters">Clear</button><button type="button" class="iv304-button" @click="resetDemo">Reset DEV</button><span>預設日期近 14 天至今天；起迄日核取方塊預設未勾選。</span></div>
    </section>

    <section class="iv304-panel iv304-basis-panel">
      <div class="iv304-panel-heading"><div><h2>Material Return Basis</h2><span>{{ hasQueried ? `${visibleBases.length} 筆未確認單據` : '尚未查詢' }}</span></div><button type="button" class="iv304-button iv304-button-confirm" :disabled="!activeBasis" @click="openConfirmation">WIP Return</button></div>
      <div class="iv304-table-wrap"><table class="iv304-table iv304-basis-table">
        <thead><tr><th>MaterialReturnNo</th><th>FromPlaceType</th><th>FromInventoryNo</th><th>InventoryNo</th><th>State</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>ConfirmDate</th></tr></thead>
        <tbody>
          <tr v-for="row in visibleBases" :key="row.materialReturnNo" :class="{ 'is-active': activeBasis?.materialReturnNo === row.materialReturnNo }" @click="selectBasis(row)">
            <td class="iv304-primary-cell">{{ row.materialReturnNo }}</td><td>{{ displayFromPlaceType(row.fromPlaceType) }}</td><td>{{ row.fromInventoryNo }}</td><td>{{ row.inventoryNo }}</td><td><span class="iv304-state iv304-state-0">{{ stateLabels[row.state] }}</span></td><td class="iv304-description">{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.returnDate || '—' }}</td>
          </tr>
          <tr v-if="hasQueried && visibleBases.length === 0"><td colspan="9" class="iv304-empty">沒有符合條件的 UnConfirm WIP 退料單。</td></tr>
          <tr v-if="!hasQueried"><td colspan="9" class="iv304-empty">按 Query 查詢；開啟畫面時不自動載入。</td></tr>
        </tbody>
      </table></div>
    </section>

    <section class="iv304-panel iv304-detail-panel">
      <div class="iv304-panel-heading"><div><h2>Material Return Detail</h2><span>{{ activeBasis?.materialReturnNo ?? '選取未確認單據後載入' }} · {{ visibleDetails.length }} 筆明細</span></div><span v-if="activeBasis" class="iv304-detail-total">合計 {{ formatQty(totalQty) }} · {{ totalComponents }} 個元件</span></div>
      <div class="iv304-table-wrap"><table class="iv304-table iv304-detail-table">
        <thead><tr><th></th><th>MaterialNo</th><th>MaterialLotNo</th><th>LocatorNo</th><th>UnitNo</th><th class="iv304-number">Qty</th></tr></thead>
        <tbody>
          <template v-for="row in visibleDetails" :key="row.key">
            <tr><td><button type="button" class="iv304-expand" :aria-expanded="expandedDetailKeys.includes(row.key)" @click="toggleExpand(row.key)">{{ expandedDetailKeys.includes(row.key) ? '▾' : '▸' }}</button></td><td>{{ row.materialNo }}</td><td>{{ row.materialLotNo }}</td><td>{{ row.locatorNo }}</td><td>{{ row.unitNo }}</td><td class="iv304-number">{{ formatQty(row.qty) }}</td></tr>
            <tr v-if="expandedDetailKeys.includes(row.key)" class="iv304-child-row"><td colspan="6"><div class="iv304-child-heading">Material Return Component · {{ componentsFor(row).length }} 筆</div><table v-if="componentsFor(row).length" class="iv304-table iv304-component-table"><thead><tr><th>MaterialLotNo</th><th>ComponentNo</th><th>ReturnInventoryNo</th><th class="iv304-number">ComponentGoodQty</th><th class="iv304-number">ComponentScrapQty</th></tr></thead><tbody><tr v-for="component in componentsFor(row)" :key="component.componentId"><td>{{ component.materialLotNo }}</td><td>{{ component.componentNo }}</td><td>{{ component.returnInventoryNo }}</td><td class="iv304-number">{{ formatQty(component.goodQty) }}</td><td class="iv304-number">{{ formatQty(component.scrapQty) }}</td></tr></tbody></table><p v-else class="iv304-no-components">此明細沒有元件子列。</p></td></tr>
          </template>
          <tr v-if="hasQueried && activeBasis && visibleDetails.length === 0"><td colspan="6" class="iv304-empty">此單頭沒有明細。</td></tr>
          <tr v-if="!activeBasis"><td colspan="6" class="iv304-empty">{{ hasQueried ? '尚未選取有效單頭。' : '請先執行查詢。' }}</td></tr>
        </tbody>
      </table></div>
    </section>

    <section v-if="records.lastConfirmation" class="iv304-last-confirm" role="status">
      <div class="iv304-last-heading"><strong>最近的 DEV 確認：{{ records.lastConfirmation.materialReturnNo }}</strong><span>{{ records.lastConfirmation.confirmDate }} · {{ records.lastConfirmation.detailCount }} 筆明細 · {{ formatQty(records.lastConfirmation.totalQty) }} 總量 · {{ records.lastConfirmation.componentCount }} 個元件</span><small>下表是本機模擬過帳列；正式庫存／工單資料沒有異動。</small></div>
      <div class="iv304-table-wrap"><table class="iv304-table iv304-posting-table"><thead><tr><th>類型</th><th>來源</th><th>MaterialNo</th><th>MaterialLotNo</th><th>LocatorNo</th><th>ComponentNo</th><th>目標庫房</th><th class="iv304-number">Qty／Good</th><th class="iv304-number">Scrap</th></tr></thead><tbody><tr v-for="posting in records.lastConfirmation.postings" :key="posting.key"><td>{{ posting.kind === 'material' ? '物料' : '元件' }}</td><td>{{ posting.source }}</td><td>{{ posting.materialNo }}</td><td>{{ posting.materialLotNo }}</td><td>{{ posting.locatorNo }}</td><td>{{ posting.componentNo || '—' }}</td><td>{{ posting.destination }}</td><td class="iv304-number">{{ posting.kind === 'material' ? `${formatQty(posting.qty)} ${posting.unitNo}` : formatQty(posting.goodQty) }}</td><td class="iv304-number">{{ posting.kind === 'component' ? formatQty(posting.scrapQty) : '—' }}</td></tr><tr v-if="records.lastConfirmation.postings.length === 0"><td colspan="9" class="iv304-empty">沒有明細過帳列。</td></tr></tbody></table></div>
    </section>
    <footer class="iv304-footer"><span role="status" aria-live="polite">{{ notice }}</span><span>DEV preview · localStorage only</span></footer>

    <div v-if="confirmationOpen && activeBasis" class="iv304-modal-backdrop" @click.self="confirmationOpen = false">
      <section class="iv304-dialog" role="dialog" aria-modal="true" aria-labelledby="iv304-confirm-title">
        <header class="iv304-dialog-header"><div><span class="iv304-kicker">IV304 · SOURCE 10</span><h2 id="iv304-confirm-title">WIP Return Confirm</h2></div><button type="button" class="iv304-icon-button" aria-label="關閉" @click="confirmationOpen = false">×</button></header>
        <p>確認退料單 <strong>{{ activeBasis.materialReturnNo }}</strong>？DEV 會把本機單據狀態改為 Confirm，並記錄模擬過帳摘要。</p>
        <dl class="iv304-confirm-summary"><div><dt>來源</dt><dd>{{ displayFromPlaceType(activeBasis.fromPlaceType) }} · {{ activeBasis.fromInventoryNo }}</dd></div><div><dt>退入庫房</dt><dd>{{ activeBasis.inventoryNo }}</dd></div><div><dt>明細／數量</dt><dd>{{ visibleDetails.length }} 筆 · {{ formatQty(totalQty) }}</dd></div><div><dt>元件</dt><dd>{{ totalComponents }} 個</dd></div></dl>
        <p class="iv304-confirm-note">原 WinForms 只處理目前作用中的單據；完成後重新查詢 State=0，因此成功單據會從清單消失。DEV 不連正式 API／資料庫。</p>
        <footer class="iv304-dialog-actions"><button type="button" class="iv304-button" @click="confirmationOpen = false">Cancel</button><button type="button" class="iv304-button iv304-button-primary" @click="confirmReturn">Confirm WIP Return</button></footer>
      </section>
    </div>
  </main>
</template>

<style scoped>
.iv304-page { display:flex; flex-direction:column; gap:11px; height:100%; min-height:0; overflow:auto; padding:13px; box-sizing:border-box; color:#26251f; background:#f4f2e9; font:13px/1.4 "Segoe UI","Microsoft JhengHei",sans-serif; }
.iv304-header,.iv304-header-actions,.iv304-query-title,.iv304-query-actions,.iv304-panel-heading { display:flex; align-items:center; }
.iv304-header,.iv304-query-title,.iv304-panel-heading { justify-content:space-between; gap:12px; }
.iv304-kicker { color:#8b8060; font-size:10px; font-weight:800; letter-spacing:.16em; }
.iv304-header h1 { margin:3px 0 0; font-size:20px; letter-spacing:.04em; }
.iv304-header h1 span { margin-left:10px; color:#777366; font-size:14px; font-weight:500; letter-spacing:0; }
.iv304-header-actions { gap:8px; }
.iv304-query-panel,.iv304-panel { border:1px solid #d7d3c7; background:#fbfaf6; box-shadow:0 1px 2px #26251f0a; }
.iv304-query-panel { padding:9px; }
.iv304-query-title { margin-bottom:8px; }
.iv304-query-title h2,.iv304-panel-heading h2 { margin:0; font-size:13px; }
.iv304-query-title span,.iv304-panel-heading span { color:#777366; font-size:11px; }
.iv304-query-grid { display:grid; grid-template-columns:repeat(4,minmax(155px,1fr)); gap:7px 10px; }
.iv304-filter { display:flex; flex-direction:column; gap:4px; min-width:0; color:#5f5a4c; font-size:11px; }
.iv304-filter > span { display:flex; align-items:center; gap:5px; }
.iv304-filter input:not([type=checkbox]),.iv304-filter select { min-height:26px; width:100%; box-sizing:border-box; border:1px solid #c9c5b8; border-radius:2px; padding:3px 6px; background:#fffefa; color:#26251f; font:inherit; }
.iv304-filter > input:disabled,.iv304-filter > select:disabled { background:#efeee8; color:#777366; }
.iv304-filter input[type=checkbox] { margin:0; accent-color:#537a6a; }
.iv304-query-actions { gap:6px; padding-top:7px; border-top:1px solid #e3e0d6; }
.iv304-query-actions span { margin-left:auto; color:#777366; font-size:11px; }
.iv304-button { min-height:27px; padding:4px 10px; border:1px solid #c9c5b8; border-radius:3px; background:#fffefa; color:#3b3930; font:inherit; cursor:pointer; }
.iv304-button:disabled { opacity:.5; cursor:not-allowed; }
.iv304-button-primary { border-color:#3b6658; background:#3b6658; color:#fff; }
.iv304-button-confirm { border-color:#9f7028; background:#f2e5c9; color:#684515; font-weight:700; }
.iv304-panel { display:flex; flex-direction:column; min-height:130px; }
.iv304-basis-panel { flex:0 0 36%; min-height:205px; }
.iv304-detail-panel { flex:1 0 250px; min-height:220px; }
.iv304-panel-heading { min-height:38px; padding:4px 9px; border-bottom:1px solid #d7d3c7; background:linear-gradient(#f5f3eb,#eeece2); }
.iv304-panel-heading > div:first-child { display:flex; align-items:baseline; gap:10px; }
.iv304-detail-total { margin-left:auto; color:#4f6f60 !important; font-weight:700; }
.iv304-table-wrap { width:100%; min-height:0; overflow:auto; }
.iv304-table { width:100%; border-collapse:collapse; white-space:nowrap; font-size:12px; }
.iv304-table th { position:sticky; top:0; z-index:1; padding:6px 8px; border-right:1px solid #e1ded4; border-bottom:1px solid #c9c5b8; background:#e8e5da; color:#514e42; font-size:11px; font-weight:700; text-align:left; }
.iv304-table td { padding:6px 8px; border-right:1px solid #efede7; border-bottom:1px solid #e6e3d9; }
.iv304-basis-table tbody tr { cursor:pointer; }
.iv304-table tbody tr:hover { background:#f5f3e9; }
.iv304-table tbody tr.is-active { background:#eaf0eb; box-shadow:inset 3px 0 #547d72; }
.iv304-primary-cell { color:#375c52; font-weight:700; }
.iv304-description { max-width:260px; overflow:hidden; text-overflow:ellipsis; }
.iv304-state { display:inline-block; padding:2px 7px; border:1px solid #ddd6c4; border-radius:99px; background:#f4f1e6; color:#605c4e; font-size:10px; }
.iv304-state-0 { border-color:#d8c58f; background:#fff5d6; color:#70571c; }
.iv304-number { text-align:right !important; font-variant-numeric:tabular-nums; }
.iv304-empty { padding:19px !important; color:#888475; text-align:center; }
.iv304-expand { width:20px; padding:0; border:0; background:transparent; color:#536d62; cursor:pointer; font:inherit; font-size:15px; }
.iv304-child-row td { padding:8px 20px; background:#f8f7f1; }
.iv304-child-heading { margin-bottom:5px; color:#5f5a4c; font-size:11px; font-weight:800; }
.iv304-component-table th { position:static; background:#efede3; }
.iv304-component-table td { padding:5px 7px; }
.iv304-no-components { margin:7px 0; color:#777366; }
.iv304-last-confirm { display:flex; flex-direction:column; gap:7px; padding:8px 10px; border:1px solid #b9cec1; background:#edf4ef; color:#365b4b; }
.iv304-last-heading { display:flex; flex-wrap:wrap; align-items:baseline; gap:6px 14px; }
.iv304-last-confirm small { color:#718275; }
.iv304-posting-table { background:#fbfcf8; }
.iv304-footer { display:flex; justify-content:space-between; gap:12px; color:#777366; font-size:11px; }
.iv304-modal-backdrop { position:fixed; inset:0; z-index:1000; display:grid; place-items:center; padding:20px; background:#1c211dcc; }
.iv304-dialog { width:min(520px,100%); box-sizing:border-box; padding:16px; border:1px solid #bcb6a7; border-radius:4px; background:#fbfaf6; box-shadow:0 16px 45px #15171355; }
.iv304-dialog-header { display:flex; justify-content:space-between; align-items:flex-start; margin-bottom:12px; }
.iv304-dialog-header h2 { margin:4px 0 0; font-size:18px; }
.iv304-icon-button { width:28px; height:28px; border:1px solid #d7d3c7; border-radius:3px; background:#fffefa; color:#5f5a4c; font-size:20px; cursor:pointer; }
.iv304-dialog p { margin:8px 0 12px; color:#514e42; }
.iv304-confirm-summary { display:grid; grid-template-columns:1fr 1fr; gap:8px; margin:0; padding:10px; background:#f2f0e8; }
.iv304-confirm-summary div { min-width:0; }
.iv304-confirm-summary dt { color:#777366; font-size:10px; }
.iv304-confirm-summary dd { margin:2px 0 0; overflow-wrap:anywhere; font-weight:700; }
.iv304-confirm-note { color:#777366 !important; font-size:11px; }
.iv304-dialog-actions { display:flex; justify-content:flex-end; gap:7px; margin-top:14px; }
@media (max-width:950px) { .iv304-query-grid { grid-template-columns:repeat(2,minmax(180px,1fr)); } }
@media (max-width:600px) { .iv304-page { padding:8px; } .iv304-header,.iv304-query-title,.iv304-panel-heading { align-items:flex-start; flex-direction:column; } .iv304-query-grid { grid-template-columns:1fr; } .iv304-query-actions,.iv304-footer { align-items:flex-start; flex-direction:column; } .iv304-query-actions span { margin-left:0; } .iv304-basis-panel { flex-basis:290px; } .iv304-confirm-summary { grid-template-columns:1fr; } }
</style>
