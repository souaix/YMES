<script setup lang="ts">
import { computed, ref } from 'vue'

type ProductInType = 0 | 1
type ProductInState = -1 | 0 | 1 | 2 | 3
type TypeFilter = 'all' | 'fgd' | 'semi'
interface Inventory { inventoryNo: string; inventoryName: string; inventoryType: 1 | 2 | 6 }
interface BasisRow {
  productInNo: string; productInType: ProductInType; inventoryNo: string; fromInventoryNo: string
  state: ProductInState; inputDate: string; description: string; creator: string; createDate: string
  reviser: string; reviseDate: string; toERP: number | null; erpNo: string; cancelUser: string; cancelDate: string
}
interface ComponentRow { componentNo: string; goodQty: number; scrapQty: number; unitNo: string }
interface DetailRow {
  productInNo: string; productInType: ProductInType; productNo: string; productName: string
  lotNo: string; qty: number; unitNo: string; locatorNo: string; components: ComponentRow[]
}
interface Criteria {
  productInNo: string; inventoryNo: string; fromInventoryNo: string; state: string
  useCreateDate: boolean; fromCreateDate: string; endCreateDate: string
  useInputDate: boolean; fromInputDate: string; endInputDate: string
}

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: true })
const emit = defineEmits<{ close: [] }>()
const previewLabel = computed(() => props.developmentPreview ? 'DEV 假資料預覽' : 'DEV 模擬')
const inventories: Inventory[] = [
  { inventoryNo: 'FGD-DEV-01', inventoryName: '成品庫 A', inventoryType: 1 },
  { inventoryNo: 'FGD-DEV-02', inventoryName: '成品庫 B', inventoryType: 1 },
  { inventoryNo: 'SEMI-DEV-01', inventoryName: '半成品庫 A', inventoryType: 6 },
  { inventoryNo: 'SEMI-DEV-02', inventoryName: '半成品庫 B', inventoryType: 6 },
  { inventoryNo: 'SEMI-DEV-03', inventoryName: '半成品庫 C', inventoryType: 6 },
  { inventoryNo: 'MAT-DEV-01', inventoryName: '材料庫 A', inventoryType: 2 },
]
const stateLabels: Record<ProductInState, string> = { '-1': 'VOID', 0: 'UnConfirm', 1: 'Handling', 2: 'Confirm', 3: 'Fail' }
const fixtureBasis: BasisRow[] = [
  { productInNo: 'FI-DEV-260001', productInType: 0, inventoryNo: 'FGD-DEV-01', fromInventoryNo: 'SEMI-DEV-01', state: 2, inputDate: '2026-11-18 10:35:12', description: 'DEV 已確認成品入庫；含 Component 與 ERP 回寫。', creator: 'DEV001', createDate: '2026-11-18 09:12:00', reviser: 'DEV001', reviseDate: '2026-11-18 10:35:12', toERP: 1, erpNo: 'ERP-DEV-6801', cancelUser: '', cancelDate: '' },
  { productInNo: 'FI-DEV-260002', productInType: 0, inventoryNo: 'FGD-DEV-01', fromInventoryNo: 'SEMI-DEV-01', state: 0, inputDate: '', description: 'DEV 未確認草稿。', creator: 'DEV002', createDate: '2026-11-20 14:36:00', reviser: '', reviseDate: '', toERP: 0, erpNo: '', cancelUser: '', cancelDate: '' },
  { productInNo: 'FI-DEV-260003', productInType: 0, inventoryNo: 'FGD-DEV-02', fromInventoryNo: 'SEMI-DEV-02', state: 3, inputDate: '2026-11-21 16:22:40', description: 'DEV 過帳失敗，保留輸入日期與錯誤狀態。', creator: 'DEV003', createDate: '2026-11-21 15:58:00', reviser: 'DEV003', reviseDate: '2026-11-21 16:22:40', toERP: 0, erpNo: '', cancelUser: '', cancelDate: '' },
  { productInNo: 'FI-DEV-260004', productInType: 0, inventoryNo: 'FGD-DEV-01', fromInventoryNo: '', state: -1, inputDate: '2026-11-19 08:02:00', description: 'DEV 已作廢的外購成品單。', creator: 'DEV001', createDate: '2026-11-19 07:44:00', reviser: 'DEV001', reviseDate: '2026-11-19 08:02:00', toERP: 0, erpNo: '', cancelUser: 'DEV001', cancelDate: '2026-11-19 08:02:00' },
  { productInNo: 'SI-DEV-260001', productInType: 1, inventoryNo: 'SEMI-DEV-02', fromInventoryNo: 'SEMI-DEV-01', state: 2, inputDate: '2026-11-22 13:05:00', description: 'DEV 已確認半成品轉庫。', creator: 'DEV002', createDate: '2026-11-22 12:41:00', reviser: 'DEV002', reviseDate: '2026-11-22 13:05:00', toERP: null, erpNo: '', cancelUser: '', cancelDate: '' },
  { productInNo: 'SI-DEV-260002', productInType: 1, inventoryNo: 'SEMI-DEV-03', fromInventoryNo: 'SEMI-DEV-02', state: 1, inputDate: '', description: 'DEV 正在處理中的半成品單。', creator: 'DEV004', createDate: '2026-11-23 11:10:00', reviser: '', reviseDate: '', toERP: null, erpNo: '', cancelUser: '', cancelDate: '' },
  { productInNo: 'FI-DEV-EXT-009', productInType: 0, inventoryNo: 'FGD-DEV-02', fromInventoryNo: '', state: 2, inputDate: '2026-11-17 17:20:00', description: 'DEV Source=0 外購來源；此 Form 不提供 Source 篩選。', creator: 'DEV003', createDate: '2026-11-17 16:58:00', reviser: '', reviseDate: '', toERP: 0, erpNo: '', cancelUser: '', cancelDate: '' },
]
const fixtureDetail: DetailRow[] = [
  { productInNo: 'FI-DEV-260001', productInType: 0, productNo: 'PRD-CONTROL-10', productName: '控制板總成', lotNo: 'LOT-FGD-001', qty: 4, unitNo: 'EA', locatorNo: 'FGD-A-01', components: [{ componentNo: 'CMP-PCB-01', goodQty: 4, scrapQty: 0, unitNo: 'EA' }, { componentNo: 'CMP-CONN-02', goodQty: 3, scrapQty: 1, unitNo: 'EA' }] },
  { productInNo: 'FI-DEV-260001', productInType: 0, productNo: 'PRD-CONTROL-10', productName: '控制板總成', lotNo: 'LOT-FGD-002', qty: 2, unitNo: 'EA', locatorNo: 'FGD-A-02', components: [] },
  { productInNo: 'FI-DEV-260002', productInType: 0, productNo: 'PRD-PANEL-07', productName: '顯示面板', lotNo: 'LOT-SEMI-310', qty: 6, unitNo: 'EA', locatorNo: 'FGD-A-03', components: [] },
  { productInNo: 'FI-DEV-260003', productInType: 0, productNo: 'PRD-VALVE-06', productName: '閥件模組', lotNo: 'LOT-SEMI-502', qty: 3, unitNo: 'EA', locatorNo: 'FGD-B-01', components: [{ componentNo: 'CMP-VALVE-01', goodQty: 2, scrapQty: 1, unitNo: 'EA' }] },
  { productInNo: 'FI-DEV-260004', productInType: 0, productNo: 'PRD-MOTOR-03', productName: '驅動馬達', lotNo: 'LOT-EXT-001', qty: 10, unitNo: 'EA', locatorNo: 'FGD-A-08', components: [] },
  { productInNo: 'SI-DEV-260001', productInType: 1, productNo: 'PRD-SENSOR-04', productName: '感測器模組', lotNo: 'LOT-SEMI-210', qty: 12, unitNo: 'EA', locatorNo: 'S-B-01', components: [{ componentNo: 'CMP-SENSOR-01', goodQty: 10, scrapQty: 2, unitNo: 'EA' }] },
  { productInNo: 'SI-DEV-260002', productInType: 1, productNo: 'PRD-PANEL-07', productName: '顯示面板', lotNo: 'LOT-SEMI-411', qty: 5, unitNo: 'EA', locatorNo: 'S-C-01', components: [] },
  { productInNo: 'FI-DEV-EXT-009', productInType: 0, productNo: 'PRD-MOTOR-03', productName: '驅動馬達', lotNo: 'LOT-EXT-009', qty: 3, unitNo: 'EA', locatorNo: 'FGD-B-04', components: [{ componentNo: 'CMP-MOTOR-02', goodQty: 3, scrapQty: 0, unitNo: 'EA' }] },
]
const criteria = ref<Criteria>(blankCriteria())
const appliedCriteria = ref<Criteria | null>(null)
const typeFilter = ref<TypeFilter>('all')
const activeKey = ref('')
const activeDetailKey = ref('')
const notice = ref('DEV 假資料：唯讀查詢，不呼叫 MES 或正式 API。設定條件後按 Query。')

const queriedBases = computed(() => appliedCriteria.value ? filterBasis(appliedCriteria.value) : [])
const visibleBases = computed(() => queriedBases.value.filter((row) => typeFilter.value === 'all' || row.productInType === (typeFilter.value === 'fgd' ? 0 : 1)))
const activeBasis = computed(() => visibleBases.value.find((row) => basisKey(row) === activeKey.value) ?? visibleBases.value[0] ?? null)
const activeDetails = computed(() => activeBasis.value ? fixtureDetail.filter((row) => row.productInNo === activeBasis.value?.productInNo && row.productInType === activeBasis.value.productInType) : [])
const activeDetail = computed(() => activeDetails.value.find((row) => detailKey(row) === activeDetailKey.value) ?? activeDetails.value[0] ?? null)
const detailTotal = computed(() => activeDetails.value.reduce((sum, row) => sum + row.qty, 0))
const resultTitle = computed(() => typeFilter.value === 'all' ? '全部類型' : typeFilter.value === 'fgd' ? 'FGD INV' : 'SEMI INV')

function dateInputValue(value: Date): string { return `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, '0')}-${String(value.getDate()).padStart(2, '0')}` }
function blankCriteria(): Criteria {
  const endDate = new Date()
  const startDate = new Date(endDate)
  startDate.setFullYear(startDate.getFullYear() - 1)
  const start = dateInputValue(startDate)
  const end = dateInputValue(endDate)
  return { productInNo: '', inventoryNo: '', fromInventoryNo: '', state: '', useCreateDate: false, fromCreateDate: start, endCreateDate: end, useInputDate: false, fromInputDate: start, endInputDate: end }
}
function stateLabel(state: ProductInState): string { return stateLabels[state] }
function typeLabel(type: ProductInType): string { return type === 0 ? 'FGD' : 'SEMI' }
function inventoryName(inventoryNo: string): string { return inventories.find((row) => row.inventoryNo === inventoryNo)?.inventoryName ?? (inventoryNo || '—') }
function formatQty(value: number): string { return new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 6 }).format(value) }
function basisKey(row: BasisRow): string { return `${row.productInType}|${row.productInNo}` }
function detailKey(row: DetailRow): string { return `${row.productInNo}|${row.lotNo}|${row.locatorNo}` }
function dateMatches(value: string, from: string, end: string): boolean {
  if (!from && !end) return true
  if (!value) return false
  const day = value.slice(0, 10)
  return (!from || day >= from) && (!end || day <= end)
}
function filterBasis(query: Criteria): BasisRow[] {
  return fixtureBasis.filter((row) => {
    if (query.productInNo && row.productInNo !== query.productInNo.trim()) return false
    if (query.inventoryNo && row.inventoryNo !== query.inventoryNo) return false
    if (query.fromInventoryNo && row.fromInventoryNo !== query.fromInventoryNo) return false
    if (query.state !== '' && String(row.state) !== query.state) return false
    if (query.useCreateDate && !dateMatches(row.createDate, query.fromCreateDate, query.endCreateDate)) return false
    if (query.useInputDate && !dateMatches(row.inputDate, query.fromInputDate, query.endInputDate)) return false
    return true
  }).sort((a, b) => a.productInNo.localeCompare(b.productInNo))
}
function runQuery(): void {
  const snapshot = { ...criteria.value }
  appliedCriteria.value = snapshot
  const rows = filterBasis(snapshot).filter((row) => typeFilter.value === 'all' || row.productInType === (typeFilter.value === 'fgd' ? 0 : 1))
  activeKey.value = rows[0] ? basisKey(rows[0]) : ''
  activeDetailKey.value = ''
  notice.value = `DEV 假資料查詢完成：${rows.length} 張單據；依單號排序。`
}
function setTypeFilter(value: TypeFilter): void {
  typeFilter.value = value
  activeKey.value = visibleBases.value[0] ? basisKey(visibleBases.value[0]) : ''
  activeDetailKey.value = ''
}
function resetQuery(): void {
  criteria.value = blankCriteria()
  appliedCriteria.value = null
  typeFilter.value = 'all'
  activeKey.value = ''
  activeDetailKey.value = ''
  notice.value = 'DEV 假資料：唯讀查詢，不呼叫 MES 或正式 API。設定條件後按 Query。'
}
</script>

<template>
  <main class="product-in-log" aria-label="成品入庫單記錄 DEV 預覽">
    <header class="page-head"><div><p class="eyebrow">INV / IV503 · {{ previewLabel }}</p><h1>成品入庫單記錄</h1><p class="sub">FGD／SEMI 入庫單唯讀查詢 · 不修改庫存</p></div><button type="button" class="close" title="Close (X)" @click="emit('close')">×</button></header>
    <p class="notice" role="status">{{ notice }}</p>
    <section class="query-panel" aria-label="成品入庫單查詢條件">
      <div class="query-grid">
        <label>ProductIn No<input v-model.trim="criteria.productInNo" list="product-in-log-numbers" autocomplete="off" placeholder="精確單號" @keydown.enter.prevent="runQuery" /><datalist id="product-in-log-numbers"><option v-for="row in fixtureBasis" :key="row.productInNo" :value="row.productInNo" /></datalist></label>
        <label>Inventory No<select v-model="criteria.inventoryNo"><option value="">全部 FGD／SEMI 庫房</option><option v-for="row in inventories.filter((item) => item.inventoryType !== 6)" :key="row.inventoryNo" :value="row.inventoryNo">{{ row.inventoryNo }} · {{ row.inventoryName }}</option></select></label>
        <label>From Inventory No<select v-model="criteria.fromInventoryNo"><option value="">全部 WIP 半成品庫</option><option v-for="row in inventories.filter((item) => item.inventoryType === 6)" :key="row.inventoryNo" :value="row.inventoryNo">{{ row.inventoryNo }} · {{ row.inventoryName }}</option></select></label>
        <label>State<select v-model="criteria.state"><option value="">全部狀態</option><option value="0">UnConfirm (0)</option><option value="1">Handling (1)</option><option value="2">Confirm (2)</option><option value="3">Fail (3)</option><option value="-1">VOID (-1)</option></select></label>
        <fieldset class="date-filter"><legend><label><input v-model="criteria.useCreateDate" type="checkbox" /> CreateDate</label></legend><label>From<input v-model="criteria.fromCreateDate" type="date" :disabled="!criteria.useCreateDate" /></label><label>To<input v-model="criteria.endCreateDate" type="date" :disabled="!criteria.useCreateDate" /></label></fieldset>
        <fieldset class="date-filter"><legend><label><input v-model="criteria.useInputDate" type="checkbox" /> ConfirmDate / InputDate</label></legend><label>From<input v-model="criteria.fromInputDate" type="date" :disabled="!criteria.useInputDate" /></label><label>To<input v-model="criteria.endInputDate" type="date" :disabled="!criteria.useInputDate" /></label></fieldset>
      </div>
      <footer class="query-actions"><span>查詢涵蓋所有 Source；日期區間含結束日。狀態 VOID 以 -1 查詢。</span><div><button type="button" @click="resetQuery">Clear</button><button type="button" class="primary" @click="runQuery">Query</button></div></footer>
    </section>
    <section class="type-bar" aria-label="產品入庫類型篩選"><span>ProductIn Type</span><button type="button" :class="{ active: typeFilter === 'all' }" @click="setTypeFilter('all')">All</button><button type="button" :class="{ active: typeFilter === 'fgd' }" @click="setTypeFilter('fgd')">FGD INV</button><button type="button" :class="{ active: typeFilter === 'semi' }" @click="setTypeFilter('semi')">SEMI INV</button><span class="type-hint">{{ resultTitle }} · {{ visibleBases.length }} 張</span></section>
    <section class="panel basis-panel" aria-label="入庫單記錄主檔">
      <header class="panel-title"><div><h2>ProductIn Basis</h2><span>{{ activeBasis ? `${activeBasis.productInNo} · ${inventoryName(activeBasis.inventoryNo)}` : '尚未選取單據' }}</span></div><small>{{ appliedCriteria ? `${visibleBases.length} 筆查詢結果` : '按 Query 載入結果' }}</small></header>
      <div class="table-wrap"><table><thead><tr><th>Type</th><th>ProductInNo</th><th>InventoryNo</th><th>State</th><th>ConfirmDate</th><th>Description</th><th>FromInventoryNo</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th><th v-if="typeFilter === 'fgd'">ToERP</th><th v-if="typeFilter === 'fgd'">ERPNo</th><th v-if="typeFilter === 'fgd'">CANCELUSER</th><th v-if="typeFilter === 'fgd'">CANCELDATE</th></tr></thead>
        <tbody><tr v-for="row in visibleBases" :key="basisKey(row)" :class="{ active: activeBasis && basisKey(activeBasis) === basisKey(row) }" @click="activeKey = basisKey(row); activeDetailKey = ''"><td>{{ typeLabel(row.productInType) }}</td><td class="mono">{{ row.productInNo }}</td><td>{{ row.inventoryNo }}</td><td><span class="state-badge" :class="`state-${row.state}`">{{ stateLabel(row.state) }}</span></td><td>{{ row.inputDate || '—' }}</td><td class="description">{{ row.description || '—' }}</td><td>{{ row.fromInventoryNo || '—' }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser || '—' }}</td><td>{{ row.reviseDate || '—' }}</td><td v-if="typeFilter === 'fgd'">{{ row.toERP === null ? '—' : row.toERP ? 'Yes' : 'No' }}</td><td v-if="typeFilter === 'fgd'">{{ row.erpNo || '—' }}</td><td v-if="typeFilter === 'fgd'">{{ row.cancelUser || '—' }}</td><td v-if="typeFilter === 'fgd'">{{ row.cancelDate || '—' }}</td></tr>
          <tr v-if="!appliedCriteria"><td :colspan="typeFilter === 'fgd' ? 15 : 11" class="empty">設定條件後按 Query；未查詢時不載入主檔結果。</td></tr><tr v-else-if="!visibleBases.length"><td :colspan="typeFilter === 'fgd' ? 15 : 11" class="empty">沒有符合條件的入庫單。</td></tr></tbody>
      </table></div>
      <footer class="grid-footer"><span>{{ visibleBases.length }} 張單據</span><span>唯讀查詢 · 不提供新增、修改或刪除</span></footer>
    </section>
    <section class="panel detail-panel" aria-label="入庫單 Lot 與 Component 明細">
      <header class="panel-title"><div><h2>ProductIn Detail</h2><span>{{ activeBasis?.productInNo ?? '請先查詢並選取單據' }}</span></div><small>{{ activeDetails.length }} 筆 Lot · 合計 {{ formatQty(detailTotal) }}</small></header>
      <div class="table-wrap"><table><thead><tr><th>ProductNo</th><th>ProductName</th><th>LotNo</th><th>Qty</th><th>UnitNo</th><th>LocatorNo</th></tr></thead><tbody>
        <tr v-for="row in activeDetails" :key="detailKey(row)" :class="{ active: activeDetail && detailKey(activeDetail) === detailKey(row) }" @click="activeDetailKey = detailKey(row)"><td>{{ row.productNo }}</td><td>{{ row.productName }}</td><td class="mono">{{ row.lotNo }}</td><td class="numeric">{{ formatQty(row.qty) }}</td><td>{{ row.unitNo }}</td><td>{{ row.locatorNo }}</td></tr>
        <tr v-if="activeBasis && !activeDetails.length"><td colspan="6" class="empty">此入庫單沒有明細。</td></tr><tr v-else-if="!activeBasis"><td colspan="6" class="empty">查詢並選取單頭後載入 Detail。</td></tr>
      </tbody></table></div>
      <div class="component-block"><header><strong>Component</strong><span>{{ activeDetail?.lotNo ?? '選取 Lot 查看 Component' }}</span></header><div class="table-wrap"><table><thead><tr><th>ComponentNo</th><th>GoodQty</th><th>ScrapQty</th><th>UnitNo</th></tr></thead><tbody>
        <tr v-for="row in activeDetail?.components ?? []" :key="row.componentNo"><td class="mono">{{ row.componentNo }}</td><td class="numeric">{{ formatQty(row.goodQty) }}</td><td class="numeric">{{ formatQty(row.scrapQty) }}</td><td>{{ row.unitNo }}</td></tr><tr v-if="activeDetail && !activeDetail.components.length"><td colspan="4" class="empty">此 Lot 沒有 Component 明細。</td></tr><tr v-else-if="!activeDetail"><td colspan="4" class="empty">尚未選取 Lot。</td></tr>
      </tbody></table></div></div>
      <footer class="grid-footer"><span>Component relation 依入庫單號及 LotNo 展開</span><span>DEV fixtures · local only</span></footer>
    </section>
  </main>
</template>

<style scoped>
.product-in-log { height: 100%; min-height: 0; display: flex; flex-direction: column; gap: 10px; padding: 14px; box-sizing: border-box; overflow: auto; background: #f3f1e9; color: #26251f; font: inherit; }
.page-head, .panel-title, .query-actions, .query-actions > div, .type-bar, .component-block > header { display: flex; align-items: center; gap: 8px; }.page-head, .panel-title, .query-actions { justify-content: space-between; }.page-head h1, .panel-title h2 { margin: 0; font-weight: 600; }.page-head h1 { font-size: 20px; }.eyebrow { margin: 0 0 3px; color: #77715f; font-size: 10px; letter-spacing: .12em; }.sub, .panel-title span { margin: 4px 0 0; color: #6e6b60; font-size: 12px; }.close { border: 0; background: transparent; color: #625f54; font-size: 22px; cursor: pointer; }.notice { min-height: 16px; margin: 0; color: #365b43; font-size: 12px; }
.query-panel, .panel { border: 1px solid #cecabd; background: #fbfaf6; }.query-panel { padding: 9px 10px; }.query-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 8px 10px; }.query-grid > label, .date-filter > label { display: flex; min-width: 0; flex-direction: column; gap: 4px; color: #555246; font-size: 11px; }.query-grid input:not([type=checkbox]), .query-grid select { width: 100%; min-width: 0; min-height: 27px; padding: 3px 7px; border: 1px solid #c7c3b7; border-radius: 3px; background: #fffefa; color: #26251f; font: inherit; }.date-filter { display: flex; min-width: 0; align-items: end; gap: 8px; margin: 0; padding: 4px 7px 6px; border: 1px solid #d5d1c5; }.date-filter legend { padding: 0 4px; color: #555246; font-size: 11px; }.date-filter legend label { display: flex; align-items: center; gap: 4px; }.date-filter > label { flex: 1; }.date-filter input[type=date] { width: 100%; min-width: 0; }.date-filter input:disabled { background: #edeae0; }.query-actions { flex-wrap: wrap; margin-top: 8px; padding-top: 8px; border-top: 1px solid #e0ddd3; color: #716d60; font-size: 11px; }.query-actions > span { min-width: 0; flex: 1 1 24rem; }.query-actions > div { flex: 0 0 auto; margin-left: auto; }.query-actions button, .type-bar button { min-height: 27px; padding: 4px 10px; border: 1px solid #aaa698; border-radius: 3px; background: #fffefa; color: #343229; font: inherit; cursor: pointer; }.query-actions .primary { border-color: #35342d; background: #35342d; color: white; }
.type-bar { flex-wrap: wrap; }.type-bar > span:first-child { color: #555246; font-size: 12px; }.type-bar button.active { border-color: #35342d; background: #35342d; color: white; }.type-hint { margin-left: auto; color: #706d62; font-size: 11px; }.basis-panel { flex: 1 1 42%; }.detail-panel { flex: 1 1 40%; }.panel-title { min-height: 34px; padding: 5px 9px; border-bottom: 1px solid #dedbce; }.panel-title h2 { font-size: 14px; }.panel-title small { color: #706d62; }.table-wrap { overflow: auto; max-width: 100%; }table { width: 100%; border-collapse: collapse; white-space: nowrap; font-size: 11px; }th, td { padding: 6px 8px; border: 1px solid #e1ded3; text-align: left; }th { position: sticky; top: 0; z-index: 1; background: #efede4; color: #555246; font-weight: 600; }tbody tr { cursor: pointer; }tbody tr:hover { background: #f2f0e8; }tbody tr.active { background: #e8ecdf; }.mono { font-family: Consolas, monospace; }.numeric { text-align: right; }.description { max-width: 300px; overflow: hidden; text-overflow: ellipsis; }.empty { padding: 16px; text-align: center; color: #858173; }.state-badge { display: inline-block; padding: 2px 6px; border: 1px solid #d0cbbd; border-radius: 10px; color: #625e51; }.state-2 { border-color: #b8ccb2; background: #edf5e9; color: #365b43; }.state-3 { border-color: #dfb8b3; background: #fbefed; color: #913c35; }.state--1 { border-color: #d7c4c0; background: #eee7e4; color: #6f4b43; }.state-0 { color: #6e5928; }.grid-footer { display: flex; justify-content: space-between; gap: 8px; padding: 5px 9px; border-top: 1px solid #dedbce; color: #777366; font-size: 10px; }.component-block { border-top: 1px solid #dedbce; }.component-block > header { padding: 7px 9px 2px; font-size: 11px; }.component-block > header span { color: #777366; }
@media (max-width: 850px) { .query-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }.query-actions { align-items: start; flex-direction: column; }.query-actions > span { flex-basis: auto; }.query-actions > div { margin-left: 0; }.type-hint { margin-left: 0; } }
</style>
