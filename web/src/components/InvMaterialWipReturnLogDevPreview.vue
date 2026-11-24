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
}
interface Filters {
  materialReturnNo: string
  inventoryNo: string
  fromInventoryNo: string
  fromMONo: string
  state: number | ''
  fromPlaceType: FromPlaceType | ''
  fromCreateDate: string
  endCreateDate: string
  fromReturnDate: string
  endReturnDate: string
  useState: boolean
  useMaterialReturnNo: boolean
  useInventoryNo: boolean
  useFromInventoryNo: boolean
  useFromPlaceType: boolean
  useFromMONo: boolean
  useFromCreateDate: boolean
  useEndCreateDate: boolean
  useFromReturnDate: boolean
  useEndReturnDate: boolean
}

const stateLabels: Record<number, string> = { 0: 'UnConfirm', 1: 'Handling', 2: 'Confirm', 3: 'Failed' }
const fromPlaceLabels: Record<FromPlaceType, string> = { 2: 'WIP INV', 3: 'MO', 5: 'SEMI', 6: 'MO SEMI', 7: 'MO Material + Semi' }
const fromPlaceTypes: FromPlaceType[] = [2, 3, 5, 6, 7]
const rawInventoryOptions = [
  { inventoryNo: 'RAW-A', name: '原料庫 A' },
  { inventoryNo: 'RAW-B', name: '原料庫 B' },
]
const wipInventoryOptions = [
  { inventoryNo: 'WIP-RAW-01', name: '在製原料庫 A' },
  { inventoryNo: 'WIP-RAW-02', name: '在製原料庫 B' },
  { inventoryNo: 'WIP-SEMI-01', name: '在製半成品庫 A' },
]
const basisRows: Basis[] = [
  { materialReturnNo: 'MWR-DEV-0001', fromPlaceType: 2, fromInventoryNo: 'WIP-RAW-01', inventoryNo: 'RAW-A', state: 2, description: 'DEV：在製原料已退回。', creator: 'DEV001', createDate: '2026-11-18 09:25:00', returnDate: '2026-11-19 15:12:00' },
  { materialReturnNo: 'MWR-DEV-0002', fromPlaceType: 3, fromInventoryNo: 'MO-DEV-101', inventoryNo: 'RAW-A', state: 1, description: 'DEV：工單原料退料處理中。', creator: 'DEV002', createDate: '2026-11-20 11:10:00', returnDate: '' },
  { materialReturnNo: 'MWR-DEV-0003', fromPlaceType: 5, fromInventoryNo: 'WIP-SEMI-01', inventoryNo: 'SEMI-A', state: 0, description: 'DEV：半成品退料草稿。', creator: 'DEV001', createDate: '2026-11-22 14:05:00', returnDate: '' },
  { materialReturnNo: 'MWR-DEV-0004', fromPlaceType: 6, fromInventoryNo: 'MO-DEV-201', inventoryNo: 'SEMI-A', state: 3, description: 'DEV：工單半成品退料失敗。', creator: 'DEV004', createDate: '2026-11-23 10:40:00', returnDate: '' },
  { materialReturnNo: 'MWR-DEV-0005', fromPlaceType: 7, fromInventoryNo: 'MO-DEV-301', inventoryNo: 'N/A', state: 2, description: 'DEV：混合工單已退料。', creator: 'DEV004', createDate: '2026-11-23 16:20:00', returnDate: '2026-11-24 08:15:00' },
]
const detailRows: Detail[] = [
  { key: 'wiplog-d-01', materialReturnNo: 'MWR-DEV-0001', materialNo: 'RM-SENSOR-05', materialLotNo: 'WIP-SNS-2611-01', locatorNo: 'WR-A-01', unitNo: 'EA', qty: 2, componentIds: ['wiplog-c-01', 'wiplog-c-02'] },
  { key: 'wiplog-d-02', materialReturnNo: 'MWR-DEV-0001', materialNo: 'RM-SENSOR-05', materialLotNo: 'WIP-SNS-2611-01', locatorNo: 'WR-A-02', unitNo: 'EA', qty: 1, componentIds: ['wiplog-c-03'] },
  { key: 'wiplog-d-03', materialReturnNo: 'MWR-DEV-0001', materialNo: 'RM-RESIN-01', materialLotNo: 'WIP-RES-2611-03', locatorNo: 'WR-A-02', unitNo: 'KG', qty: 5.5, componentIds: [] },
  { key: 'wiplog-d-04', materialReturnNo: 'MWR-DEV-0002', materialNo: 'RM-CHIP-11', materialLotNo: 'MO101-CHIP-01', locatorNo: 'MO-A-01', unitNo: 'EA', qty: 1, componentIds: ['wiplog-c-04'] },
  { key: 'wiplog-d-05', materialReturnNo: 'MWR-DEV-0003', materialNo: 'PRD-CTRL-02', materialLotNo: 'SEMI-2611-08', locatorNo: 'WS-A-02', unitNo: 'EA', qty: 1, componentIds: ['wiplog-c-05'] },
  { key: 'wiplog-d-06', materialReturnNo: 'MWR-DEV-0004', materialNo: 'PRD-PANEL-07', materialLotNo: 'MO201-PNL-03', locatorNo: 'MS-A-01', unitNo: 'EA', qty: 2, componentIds: [] },
  { key: 'wiplog-d-07', materialReturnNo: 'MWR-DEV-0005', materialNo: 'RM-SENSOR-05', materialLotNo: 'MO301-SNS-01', locatorNo: 'MO-MIX-01', unitNo: 'EA', qty: 1, componentIds: ['wiplog-c-06'] },
]
const componentRows: Component[] = [
  { componentId: 'wiplog-c-01', materialLotNo: 'WIP-SNS-2611-01', componentNo: 'WIP-CMP-SNS-A', goodQty: 1, scrapQty: 0 },
  { componentId: 'wiplog-c-02', materialLotNo: 'WIP-SNS-2611-01', componentNo: 'WIP-CMP-SNS-B', goodQty: 1, scrapQty: 0 },
  { componentId: 'wiplog-c-03', materialLotNo: 'WIP-SNS-2611-01', componentNo: 'WIP-CMP-SNS-C', goodQty: 1, scrapQty: 0 },
  { componentId: 'wiplog-c-04', materialLotNo: 'MO101-CHIP-01', componentNo: 'MO101-CMP-001', goodQty: 1, scrapQty: 0 },
  { componentId: 'wiplog-c-05', materialLotNo: 'SEMI-2611-08', componentNo: 'SEMI-CMP-001', goodQty: 1, scrapQty: 0 },
  { componentId: 'wiplog-c-06', materialLotNo: 'MO301-SNS-01', componentNo: 'MIX-CMP-001', goodQty: 1, scrapQty: 0 },
]

function dateText(date: Date): string { return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}` }
function dateOffset(days: number): string { const date = new Date(); date.setDate(date.getDate() + days); return dateText(date) }
function emptyFilters(): Filters {
  return {
    materialReturnNo: '', inventoryNo: '', fromInventoryNo: '', fromMONo: '', state: '', fromPlaceType: '',
    fromCreateDate: dateOffset(-14), endCreateDate: dateOffset(0), fromReturnDate: dateOffset(-14), endReturnDate: dateOffset(0),
    useState: false, useMaterialReturnNo: false, useInventoryNo: false, useFromInventoryNo: false, useFromPlaceType: false, useFromMONo: false, useFromCreateDate: false, useEndCreateDate: false, useFromReturnDate: false, useEndReturnDate: false,
  }
}

const filters = ref<Filters>(emptyFilters())
const appliedFilters = ref<Filters>(emptyFilters())
const hasQueried = ref(false)
const activeBasisNo = ref('')
const expandedDetailKeys = ref<string[]>([])
const notice = ref('請設定條件並按 Query；開啟畫面時不會自動查詢。')
const visibleBases = computed(() => {
  if (!hasQueried.value) return []
  const value = appliedFilters.value
  return basisRows.filter((row) => {
    if (value.materialReturnNo && !row.materialReturnNo.toLowerCase().includes(value.materialReturnNo.toLowerCase())) return false
    if (value.inventoryNo && row.inventoryNo !== value.inventoryNo) return false
    const sourceNo = value.useFromMONo && value.fromMONo ? value.fromMONo : value.fromInventoryNo
    if (sourceNo && row.fromInventoryNo !== sourceNo) return false
    if (value.useState && value.state !== '' && row.state !== value.state) return false
    if (value.useFromPlaceType && value.fromPlaceType !== '' && row.fromPlaceType !== value.fromPlaceType) return false
    if (value.useFromCreateDate && row.createDate.slice(0, 10) < value.fromCreateDate) return false
    if (value.useEndCreateDate && row.createDate.slice(0, 10) > value.endCreateDate) return false
    if (value.useFromReturnDate && (!row.returnDate || row.returnDate.slice(0, 10) < value.fromReturnDate)) return false
    if (value.useEndReturnDate && (!row.returnDate || row.returnDate.slice(0, 10) > value.endReturnDate)) return false
    return true
  })
})
const activeBasis = computed(() => visibleBases.value.find((row) => row.materialReturnNo === activeBasisNo.value) ?? visibleBases.value[0] ?? null)
const visibleDetails = computed(() => detailRows.filter((row) => row.materialReturnNo === activeBasis.value?.materialReturnNo))

function runQuery(): void {
  appliedFilters.value = { ...filters.value }
  hasQueried.value = true
  activeBasisNo.value = ''
  const first = visibleBases.value[0]
  activeBasisNo.value = first?.materialReturnNo ?? ''
  expandTicketComponents(activeBasisNo.value)
  notice.value = `查詢完成：${visibleBases.value.length} 筆結果；DEV 假資料不呼叫正式 API。`
}
function clearFilters(): void {
  filters.value = emptyFilters()
  appliedFilters.value = emptyFilters()
  hasQueried.value = false
  activeBasisNo.value = ''
  expandedDetailKeys.value = []
  notice.value = '已清除條件；請按 Query 載入結果。'
}
function setSourceMode(mode: 'wip' | 'mo', checked: boolean): void {
  if (mode === 'wip') {
    filters.value.useFromInventoryNo = checked
    if (checked) {
      filters.value.useFromMONo = false
      filters.value.fromMONo = ''
    }
  } else {
    filters.value.useFromMONo = checked
    if (checked) {
      filters.value.useFromInventoryNo = false
      filters.value.fromInventoryNo = ''
    }
  }
}
function setMaterialReturnNo(value: string): void { filters.value.materialReturnNo = value; filters.value.useMaterialReturnNo = !!value }
function setInventoryNo(value: string): void { filters.value.inventoryNo = value; filters.value.useInventoryNo = !!value }
function setFromInventoryNo(value: string): void {
  filters.value.fromInventoryNo = value
  setSourceMode('wip', !!value)
}
function setFromMONo(value: string): void {
  filters.value.fromMONo = value
  setSourceMode('mo', !!value)
}
function setFromPlaceType(value: string): void {
  filters.value.fromPlaceType = value === '' ? '' : Number(value) as FromPlaceType
  filters.value.useFromPlaceType = value !== ''
}
function setState(value: string): void {
  filters.value.state = value === '' ? '' : Number(value)
  filters.value.useState = value !== ''
}
function expandTicketComponents(materialReturnNo: string): void {
  expandedDetailKeys.value = detailRows.filter((row) => row.materialReturnNo === materialReturnNo && row.componentIds.length > 0).map((row) => row.key)
}
function selectBasis(row: Basis): void { activeBasisNo.value = row.materialReturnNo; expandTicketComponents(row.materialReturnNo) }
function toggleExpand(key: string): void { expandedDetailKeys.value = expandedDetailKeys.value.includes(key) ? expandedDetailKeys.value.filter((item) => item !== key) : [...expandedDetailKeys.value, key] }
function componentsFor(detail: Detail): Component[] { return componentRows.filter((component) => detail.componentIds.includes(component.componentId)) }
function formatQty(value: number): string { return new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 8 }).format(value) }
function displayFromPlaceType(value: FromPlaceType): string { return fromPlaceLabels[value] }
</script>

<template>
  <main class="iv303-page" aria-label="退料單記錄(WIP)">
    <header class="iv303-header">
      <div><div class="iv303-kicker">INVENTORY · IV303</div><h1>WIP MATERIAL RETURN LOG <span>退料單記錄(WIP)</span></h1></div>
      <div class="iv303-header-actions"><span class="iv303-readonly-tag">唯讀查詢</span><button type="button" class="iv303-button" @click="emit('close')">Close (X)</button></div>
    </header>

    <section class="iv303-query-panel" aria-label="退料單查詢條件">
      <div class="iv303-query-title"><h2>Query</h2><span>ReturnDate 來自 Basis.ReturnDate，WinForms 顯示為 ConfirmDate</span></div>
      <div class="iv303-query-grid">
        <label class="iv303-filter"><span><input v-model="filters.useMaterialReturnNo" type="checkbox"> Return No</span><input :value="filters.materialReturnNo" type="search" placeholder="包含查詢" @input="setMaterialReturnNo(($event.target as HTMLInputElement).value)"></label>
        <label class="iv303-filter"><span><input v-model="filters.useInventoryNo" type="checkbox"> Inventory No</span><select :value="filters.inventoryNo" @change="setInventoryNo(($event.target as HTMLSelectElement).value)"><option value="">全部</option><option v-for="item in rawInventoryOptions" :key="item.inventoryNo" :value="item.inventoryNo">{{ item.inventoryNo }} · {{ item.name }}</option></select></label>
        <label class="iv303-filter"><span><input v-model="filters.useFromPlaceType" type="checkbox"> From Place Type</span><select :value="filters.fromPlaceType" @change="setFromPlaceType(($event.target as HTMLSelectElement).value)"><option value="">全部</option><option v-for="type in fromPlaceTypes" :key="type" :value="type">{{ fromPlaceLabels[type] }} ({{ type }})</option></select></label>
        <label class="iv303-filter"><span><input v-model="filters.useState" type="checkbox"> State</span><select :value="filters.state" @change="setState(($event.target as HTMLSelectElement).value)"><option value="">全部</option><option v-for="state in [0, 1, 2, 3]" :key="state" :value="state">{{ stateLabels[state] }} ({{ state }})</option></select></label>
        <label class="iv303-filter"><span><input v-model="filters.useFromInventoryNo" type="checkbox" @change="setSourceMode('wip', ($event.target as HTMLInputElement).checked)"> From Return Inventory No</span><select :value="filters.fromInventoryNo" :disabled="filters.useFromMONo" @change="setFromInventoryNo(($event.target as HTMLSelectElement).value)"><option value="">全部</option><option v-for="item in wipInventoryOptions" :key="item.inventoryNo" :value="item.inventoryNo">{{ item.inventoryNo }} · {{ item.name }}</option></select></label>
        <label class="iv303-filter"><span><input v-model="filters.useFromMONo" type="checkbox" @change="setSourceMode('mo', ($event.target as HTMLInputElement).checked)"> From Return MO No</span><input :value="filters.fromMONo" :disabled="!filters.useFromMONo" type="search" placeholder="輸入工單號" @input="setFromMONo(($event.target as HTMLInputElement).value)"></label>
        <label class="iv303-filter"><span><input v-model="filters.useFromCreateDate" type="checkbox"> From Create Date</span><input v-model="filters.fromCreateDate" type="date" @change="filters.useFromCreateDate = true"></label>
        <label class="iv303-filter"><span><input v-model="filters.useEndCreateDate" type="checkbox"> End Create Date</span><input v-model="filters.endCreateDate" type="date" @change="filters.useEndCreateDate = true"></label>
        <label class="iv303-filter"><span><input v-model="filters.useFromReturnDate" type="checkbox"> From MaterialReturn Date</span><input v-model="filters.fromReturnDate" type="date"></label>
        <label class="iv303-filter"><span><input v-model="filters.useEndReturnDate" type="checkbox"> End MaterialReturn Date</span><input v-model="filters.endReturnDate" type="date"></label>
      </div>
      <div class="iv303-query-actions"><button type="button" class="iv303-button iv303-button-primary" @click="runQuery">Query</button><button type="button" class="iv303-button" @click="clearFilters">Clear</button><span>預設日期近 14 天至今天；條件核取方塊預設未勾選。</span></div>
    </section>

    <section class="iv303-panel iv303-basis-panel">
      <div class="iv303-panel-heading"><div><h2>Material Return Basis</h2><span>{{ hasQueried ? `${visibleBases.length} 筆結果` : '尚未查詢' }}</span></div></div>
      <div class="iv303-table-wrap"><table class="iv303-table iv303-basis-table">
        <thead><tr><th>MaterialReturnNo</th><th>FromPlaceType</th><th>FromInventoryNo</th><th>InventoryNo</th><th>State</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>ConfirmDate</th></tr></thead>
        <tbody>
          <tr v-for="row in visibleBases" :key="row.materialReturnNo" :class="{ 'is-active': activeBasis?.materialReturnNo === row.materialReturnNo }" @click="selectBasis(row)">
            <td class="iv303-primary-cell">{{ row.materialReturnNo }}</td><td>{{ displayFromPlaceType(row.fromPlaceType) }}</td><td>{{ row.fromInventoryNo }}</td><td>{{ row.inventoryNo }}</td><td><span class="iv303-state" :class="`state-${row.state}`">{{ stateLabels[row.state] }}</span></td><td class="iv303-description">{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.returnDate || '—' }}</td>
          </tr>
          <tr v-if="hasQueried && visibleBases.length === 0"><td colspan="9" class="iv303-empty">沒有符合條件的 WIP 退料單。</td></tr>
          <tr v-if="!hasQueried"><td colspan="9" class="iv303-empty">請輸入查詢條件後按 Query；未查詢時不載入資料。</td></tr>
        </tbody>
      </table></div>
    </section>

    <section class="iv303-panel iv303-detail-panel">
      <div class="iv303-panel-heading"><div><h2>Material Return Detail</h2><span>{{ activeBasis?.materialReturnNo ?? '選取查詢結果後載入' }} · {{ visibleDetails.length }} 筆明細</span></div></div>
      <div class="iv303-table-wrap"><table class="iv303-table iv303-detail-table">
        <thead><tr><th>MaterialNo</th><th>MaterialLotNo</th><th>LocatorNo</th><th>UnitNo</th><th class="iv303-number">Qty</th></tr></thead>
        <tbody>
          <template v-for="row in visibleDetails" :key="row.key">
            <tr><td><button type="button" class="iv303-expand" :aria-expanded="expandedDetailKeys.includes(row.key)" @click="toggleExpand(row.key)">{{ expandedDetailKeys.includes(row.key) ? '▾' : '▸' }}</button>{{ row.materialNo }}</td><td>{{ row.materialLotNo }}</td><td>{{ row.locatorNo }}</td><td>{{ row.unitNo }}</td><td class="iv303-number">{{ formatQty(row.qty) }}</td></tr>
            <tr v-if="expandedDetailKeys.includes(row.key)" class="iv303-child-row"><td colspan="5"><div class="iv303-child-heading">Material Return Component · {{ componentsFor(row).length }} 筆</div><table v-if="componentsFor(row).length" class="iv303-table iv303-component-table"><thead><tr><th>MaterialLotNo</th><th>ComponentNo</th><th class="iv303-number">ComponentGoodQty</th><th class="iv303-number">ComponentScrapQty</th></tr></thead><tbody><tr v-for="component in componentsFor(row)" :key="component.componentId"><td>{{ component.materialLotNo }}</td><td>{{ component.componentNo }}</td><td class="iv303-number">{{ formatQty(component.goodQty) }}</td><td class="iv303-number">{{ formatQty(component.scrapQty) }}</td></tr></tbody></table><p v-else class="iv303-no-components">此明細沒有元件子列。</p></td></tr>
          </template>
          <tr v-if="hasQueried && activeBasis && visibleDetails.length === 0"><td colspan="5" class="iv303-empty">此單頭沒有明細。</td></tr>
          <tr v-if="!activeBasis"><td colspan="5" class="iv303-empty">{{ hasQueried ? '尚未選取有效單頭。' : '請先執行查詢。' }}</td></tr>
        </tbody>
      </table></div>
    </section>

    <footer class="iv303-footer"><span role="status" aria-live="polite">{{ notice }}</span><span>DEV preview · read only</span></footer>
  </main>
</template>

<style scoped>
.iv303-page { display: flex; flex-direction: column; gap: 11px; height: 100%; min-height: 0; overflow: auto; padding: 13px; box-sizing: border-box; color: #26251f; background: #f4f2e9; font: 13px/1.4 "Segoe UI", "Microsoft JhengHei", sans-serif; }
.iv303-header,.iv303-header-actions,.iv303-query-title,.iv303-query-actions,.iv303-panel-heading { display: flex; align-items: center; }
.iv303-header,.iv303-query-title,.iv303-panel-heading { justify-content: space-between; gap: 12px; }
.iv303-kicker { color: #8b8060; font-size: 10px; font-weight: 800; letter-spacing: .16em; }
.iv303-header h1 { margin: 3px 0 0; font-size: 20px; letter-spacing: .04em; }
.iv303-header h1 span { margin-left: 10px; color: #777366; font-size: 14px; font-weight: 500; letter-spacing: 0; }
.iv303-header-actions { gap: 8px; }
.iv303-readonly-tag { padding: 3px 8px; border: 1px solid #aac2b4; border-radius: 99px; background: #e5efe8; color: #386152; font-size: 11px; font-weight: 700; }
.iv303-button { min-height: 28px; padding: 4px 10px; border: 1px solid #aaa797; border-radius: 3px; background: #fbfaf6; color: #302f29; font: inherit; cursor: pointer; white-space: nowrap; }
.iv303-button:hover { border-color: #4b483e; background: #efede3; }
.iv303-button-primary { border-color: #466b63; background: #466b63; color: white; }
.iv303-button-primary:hover { background: #36564f; color: white; }
.iv303-query-panel,.iv303-panel { border: 1px solid #c9c5b8; background: #fbfaf6; box-shadow: 0 2px 7px rgb(47 43 29 / 5%); }
.iv303-query-panel { padding: 8px 10px 9px; }
.iv303-query-title { min-height: 26px; padding-bottom: 6px; border-bottom: 1px solid #dedace; }
.iv303-query-title h2,.iv303-panel-heading h2 { margin: 0; font-size: 14px; }
.iv303-query-title span,.iv303-panel-heading span { color: #777366; font-size: 11px; }
.iv303-query-grid { display: grid; grid-template-columns: repeat(4, minmax(180px, 1fr)); gap: 8px 10px; padding: 9px 0; }
.iv303-filter { display: flex; flex-direction: column; gap: 4px; min-width: 0; }
.iv303-filter > span { display: flex; align-items: center; gap: 5px; color: #595649; font-size: 11px; font-weight: 700; }
.iv303-filter input[type="checkbox"] { accent-color: #52746a; }
.iv303-filter > input:not([type="checkbox"]),.iv303-filter > select { min-height: 29px; padding: 4px 7px; border: 1px solid #c8c4b8; border-radius: 2px; background: white; color: #27261f; font: inherit; }
.iv303-filter > input:disabled,.iv303-filter > select:disabled { background: #efeee8; color: #777366; }
.iv303-query-actions { gap: 6px; padding-top: 7px; border-top: 1px solid #e3e0d6; }
.iv303-query-actions span { margin-left: auto; color: #777366; font-size: 11px; }
.iv303-panel { display: flex; flex-direction: column; min-height: 130px; }
.iv303-basis-panel { flex: 0 0 36%; min-height: 205px; }
.iv303-detail-panel { flex: 1 0 250px; min-height: 220px; }
.iv303-panel-heading { min-height: 38px; padding: 4px 9px; border-bottom: 1px solid #d7d3c7; background: linear-gradient(#f5f3eb, #eeece2); }
.iv303-panel-heading > div:first-child { display: flex; align-items: baseline; gap: 10px; }
.iv303-table-wrap { width: 100%; min-height: 0; overflow: auto; }
.iv303-table { width: 100%; border-collapse: collapse; white-space: nowrap; font-size: 12px; }
.iv303-table th { position: sticky; top: 0; z-index: 1; padding: 6px 8px; border-right: 1px solid #e1ded4; border-bottom: 1px solid #c9c5b8; background: #e8e5da; color: #514e42; font-size: 11px; font-weight: 700; text-align: left; }
.iv303-table td { padding: 6px 8px; border-right: 1px solid #efede7; border-bottom: 1px solid #e6e3d9; }
.iv303-table tbody tr:hover { background: #f5f3e9; }
.iv303-table tbody tr.is-active { background: #eaf0eb; box-shadow: inset 3px 0 #547d72; }
.iv303-primary-cell { color: #375c52; font-weight: 700; }
.iv303-description { max-width: 260px; overflow: hidden; text-overflow: ellipsis; }
.iv303-state { display: inline-block; padding: 2px 7px; border: 1px solid #ddd6c4; border-radius: 99px; background: #f4f1e6; color: #605c4e; font-size: 10px; }
.iv303-state.state-0 { border-color: #d8c58f; background: #fff5d6; color: #70571c; }
.iv303-state.state-1 { border-color: #b6c6d8; background: #edf3fa; color: #365b7c; }
.iv303-state.state-2 { border-color: #aac2b4; background: #e5efe8; color: #386152; }
.iv303-state.state-3 { border-color: #d7b3aa; background: #faece8; color: #8b352b; }
.iv303-number { text-align: right !important; font-variant-numeric: tabular-nums; }
.iv303-empty { padding: 19px !important; color: #888475; text-align: center; }
.iv303-expand { width: 20px; padding: 0; border: 0; background: transparent; color: #536d62; cursor: pointer; font: inherit; font-size: 15px; }
.iv303-child-row td { padding: 8px 20px; background: #f8f7f1; }
.iv303-child-heading { margin-bottom: 5px; color: #5f5a4c; font-size: 11px; font-weight: 800; }
.iv303-component-table th { position: static; background: #efede3; }
.iv303-component-table td { padding: 5px 7px; }
.iv303-no-components { margin: 7px 0; color: #777366; }
.iv303-footer { display: flex; justify-content: space-between; gap: 12px; color: #777366; font-size: 11px; }
@media (max-width: 950px) { .iv303-query-grid { grid-template-columns: repeat(2, minmax(180px, 1fr)); } }
@media (max-width: 600px) { .iv303-page { padding: 8px; } .iv303-header,.iv303-query-title,.iv303-panel-heading { align-items: flex-start; flex-direction: column; } .iv303-query-grid { grid-template-columns: 1fr; } .iv303-query-actions,.iv303-footer { align-items: flex-start; flex-direction: column; } .iv303-query-actions span { margin-left: 0; } .iv303-basis-panel { flex-basis: 290px; } }
</style>
