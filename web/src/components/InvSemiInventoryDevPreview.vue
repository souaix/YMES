<script setup lang="ts">
import { computed, ref } from 'vue'

interface InventoryRow { inventoryNo: string; productNo: string; productName: string; locatorNo: string; lotNo: string; locQty: number; unitNo: string; belongToType: number; belongToNo: string; inputDate: string; semiInNo: string }
interface ComponentRow { componentNo: string; goodQty: number; scrapQty: number; unitNo: string }
interface BinRow { lotNo: string; bin1: number; bin2: number; bin3: number; bin4: number; bin5: number; bin6: number; bin7: number; bin8: number }
type Workflow = 'SEMI In' | 'SEMI Out' | ''

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: true })
const emit = defineEmits<{ close: [] }>()
const previewLabel = computed(() => props.developmentPreview ? 'DEV 假資料預覽' : 'DEV 模擬')
const inventoryOptions = ['SEMI-DEV-01', 'SEMI-DEV-02']
const inventoryRows: InventoryRow[] = [
  { inventoryNo: 'SEMI-DEV-01', productNo: 'PROD-DEV-701', productName: 'Semi Device Alpha', locatorNo: 'SEMI-L-01', lotNo: 'LOT-DEV-101', locQty: 85.25, unitNo: 'PCS', belongToType: 0, belongToNo: 'MO-DEV-2201', inputDate: '2026-11-24 08:25:00', semiInNo: 'SIN-DEV-0101' },
  { inventoryNo: 'SEMI-DEV-01', productNo: 'PROD-DEV-702', productName: 'Semi Device Beta', locatorNo: 'SEMI-L-02', lotNo: 'LOT-DEV-102', locQty: 32, unitNo: 'PCS', belongToType: 1, belongToNo: 'WO-DEV-2202', inputDate: '2026-11-23 14:10:00', semiInNo: 'SIN-DEV-0100' },
  { inventoryNo: 'SEMI-DEV-01', productNo: 'PROD-DEV-703', productName: 'Semi Device Gamma', locatorNo: 'SEMI-L-03', lotNo: 'LOT-DEV-103', locQty: 18.5, unitNo: 'PCS', belongToType: 0, belongToNo: 'MO-DEV-2203', inputDate: '2026-11-22 09:45:00', semiInNo: 'SIN-DEV-0098' },
  { inventoryNo: 'SEMI-DEV-02', productNo: 'PROD-DEV-701', productName: 'Semi Device Alpha', locatorNo: 'SEMI-L-05', lotNo: 'LOT-DEV-104', locQty: 46, unitNo: 'PCS', belongToType: 0, belongToNo: 'MO-DEV-2210', inputDate: '2026-11-21 16:05:00', semiInNo: 'SIN-DEV-0095' },
]
const components: Record<string, ComponentRow[]> = {
  'SEMI-DEV-01|LOT-DEV-101|SEMI-L-01': [
    { componentNo: 'COMP-DEV-101-A', goodQty: 55, scrapQty: 1, unitNo: 'PCS' },
    { componentNo: 'COMP-DEV-101-B', goodQty: 30.25, scrapQty: 0, unitNo: 'PCS' },
  ],
  'SEMI-DEV-01|LOT-DEV-103|SEMI-L-03': [],
}
const goodBins: Record<string, BinRow[]> = {
  'LOT-DEV-103': [{ lotNo: 'LOT-DEV-103', bin1: 10.5, bin2: 4, bin3: 2, bin4: 1, bin5: 0, bin6: 0, bin7: 0, bin8: 1 }],
  'LOT-DEV-104': [{ lotNo: 'LOT-DEV-104', bin1: 20, bin2: 12, bin3: 8, bin4: 6, bin5: 0, bin6: 0, bin7: 0, bin8: 0 }],
}
const binBasis = [{ binNo: 'Bin1', binOrder: 1 }, { binNo: 'Bin2', binOrder: 2 }, { binNo: 'Bin3', binOrder: 3 }, { binNo: 'Bin4', binOrder: 4 }, { binNo: 'Bin5', binOrder: 5 }, { binNo: 'Bin6', binOrder: 6 }, { binNo: 'Bin7', binOrder: 7 }, { binNo: 'Bin8', binOrder: 8 }]
const inventoryNo = ref('')
const appliedInventoryNo = ref('')
const selectedKey = ref('')
const workflow = ref<Workflow>('')
const lookupOpen = ref(false)
const notice = ref('DEV 假資料：選取半成品庫房或輸入 InventoryNo 後按 Enter 查詢。')
const visibleRows = computed(() => appliedInventoryNo.value ? inventoryRows.filter((row) => row.inventoryNo === appliedInventoryNo.value && row.locQty > 0) : [])
const selectedRow = computed(() => visibleRows.value.find((row) => rowKey(row) === selectedKey.value) ?? null)
const selectedComponents = computed(() => selectedRow.value ? components[rowKey(selectedRow.value)] ?? [] : [])
const selectedBins = computed(() => selectedRow.value && selectedComponents.value.length === 0 ? goodBins[selectedRow.value.lotNo] ?? [] : [])
const detailMode = computed(() => selectedComponents.value.length ? 'component' : selectedBins.value.length ? 'bin' : 'empty')
const visibleBins = computed(() => selectedRow.value ? binBasis.slice().sort((a, b) => a.binOrder - b.binOrder).filter((bin) => Object.prototype.hasOwnProperty.call(selectedBins.value[0] ?? {}, bin.binNo.toLowerCase())) : [])

function rowKey(row: InventoryRow): string { return `${row.inventoryNo}|${row.lotNo}|${row.locatorNo}` }
function queryInventory(value: string): void {
  const normalized = value.trim()
  inventoryNo.value = normalized
  appliedInventoryNo.value = normalized
  const first = visibleRows.value[0]
  selectedKey.value = first ? rowKey(first) : ''
  notice.value = visibleRows.value.length ? `DEV 查詢完成：${normalized} 共 ${visibleRows.value.length} 筆 LocQty > 0 庫存。` : `DEV 查詢完成：${normalized || '(空)'} 沒有符合的半成品庫存。`
}
function selectRow(row: InventoryRow): void { selectedKey.value = rowKey(row); notice.value = `已選取 ${row.lotNo}／${row.locatorNo}；明細按元件庫存優先，否則查 GoodBin。` }
function chooseInventory(value: string): void { lookupOpen.value = false; queryInventory(value) }
function binValue(row: BinRow, binNo: string): number { return row[binNo.toLowerCase() as keyof BinRow] as number }
function openWorkflow(kind: Exclude<Workflow, ''>): void { workflow.value = kind }
function closeWorkflow(): void { workflow.value = ''; notice.value = '本頁 DEV 假資料維持不變；入庫／出庫由獨立確認功能處理。' }
</script>

<template>
  <main class="semi-inventory" aria-label="半成品庫庫房資料">
    <header class="page-head">
      <div><p class="eyebrow">INV · IV401</p><h1>半成品庫庫房資料</h1><p class="subtitle">SEMI Inventory · 庫存批次、儲位、元件及 Bin 數量</p></div>
      <div class="head-actions"><span class="dev-tag">{{ previewLabel }}</span><button type="button" class="close" @click="emit('close')">Close (X)</button></div>
    </header>

    <section class="query-card">
      <div class="query-title"><div><h2>Inventory No</h2><p>只列 IssueState=2、InventoryType=2 的半成品庫房</p></div><span class="source-note">輸入 InventoryNo 後 Enter 查詢，或使用 Lookup 選擇</span></div>
      <div class="query-row"><label class="field"><span>Inventory No</span><input v-model="inventoryNo" placeholder="輸入完整 InventoryNo" @keyup.enter="queryInventory(inventoryNo)"></label><button type="button" class="secondary lookup-button" @click="lookupOpen = true">....</button></div>
      <p class="notice" role="status">{{ notice }}</p>
    </section>

    <div class="action-row"><div><button type="button" class="secondary" @click="openWorkflow('SEMI In')">SEMI In</button><button type="button" class="secondary" @click="openWorkflow('SEMI Out')">SEMI Out</button></div><span>舊 Form 由獨立確認視窗執行入庫／出庫；本頁只查詢現況。</span></div>

    <div class="data-layout">
      <section class="inventory-card">
        <div class="section-head"><div><h2>SEMI Inventory</h2><p>{{ appliedInventoryNo ? `${visibleRows.length} 筆 LocQty > 0` : '尚未查詢' }}</p></div><span class="read-only">唯讀</span></div>
        <div class="table-wrap"><table><thead><tr><th>Product In No</th><th>Product In Name</th><th>LocatorNo</th><th>LotNo</th><th class="numeric">LocQty</th><th>UnitNo</th><th>BelongToType</th><th>BelongToNo</th><th>InputDate</th><th>SEMIInNo</th></tr></thead><tbody>
          <tr v-for="row in visibleRows" :key="rowKey(row)" :class="{ selected: rowKey(row) === selectedKey }" @click="selectRow(row)"><td class="code">{{ row.productNo }}</td><td>{{ row.productName }}</td><td class="code">{{ row.locatorNo }}</td><td class="code">{{ row.lotNo }}</td><td class="numeric">{{ row.locQty }}</td><td>{{ row.unitNo }}</td><td>{{ row.belongToType === 0 ? 'MO' : '' }}</td><td class="code">{{ row.belongToNo }}</td><td>{{ row.inputDate }}</td><td class="code">{{ row.semiInNo }}</td></tr>
          <tr v-if="!visibleRows.length"><td colspan="10" class="empty">{{ appliedInventoryNo ? '沒有符合的半成品庫存' : '選取 InventoryNo 後載入庫存' }}</td></tr>
        </tbody></table></div>
      </section>

      <section class="detail-card">
        <div class="section-head"><div><h2>{{ detailMode === 'component' ? 'SEMI Inventory Component' : detailMode === 'bin' ? 'GoodBin State' : 'Inventory Detail' }}</h2><p>{{ selectedRow ? `${selectedRow.lotNo} · ${selectedRow.locatorNo}` : '選取一筆庫存列' }}</p></div><span class="source-pill">{{ detailMode === 'component' ? 'Component 優先' : detailMode === 'bin' ? '動態 Bin 欄' : '無明細' }}</span></div>
        <div v-if="selectedRow && detailMode === 'component'" class="table-wrap"><table><thead><tr><th>ComponentNo</th><th class="numeric">GoodQty</th><th class="numeric">ScrapQty</th><th>UnitNo</th></tr></thead><tbody><tr v-for="item in selectedComponents" :key="item.componentNo"><td class="code">{{ item.componentNo }}</td><td class="numeric">{{ item.goodQty }}</td><td class="numeric">{{ item.scrapQty }}</td><td>{{ item.unitNo }}</td></tr></tbody></table></div>
        <div v-else-if="selectedRow && detailMode === 'bin'" class="table-wrap"><table><thead><tr><th>LotNo</th><th v-for="bin in visibleBins" :key="bin.binNo" class="numeric">{{ bin.binNo }}</th></tr></thead><tbody><tr v-for="item in selectedBins" :key="item.lotNo"><td class="code">{{ item.lotNo }}</td><td v-for="bin in visibleBins" :key="bin.binNo" class="numeric">{{ binValue(item, bin.binNo) }}</td></tr></tbody></table></div>
        <div v-else class="detail-empty">{{ selectedRow ? '此 Lot 沒有 Component 或 GoodBin 明細。' : '選取左側半成品庫存列後顯示明細。' }}</div>
        <p class="detail-note">Component 查詢帶 InventoryNo + LotNo + LocatorNo；只有 Component 結果為空才按 LotNo 查 GoodBin。Bin 欄依 SYS Bin 的 BinOrder 過濾。</p>
      </section>
    </div>
    <footer class="page-footer"><span>只顯示 LocQty &gt; 0；DEV 資料僅存在頁面記憶體。</span><span>SEMI Inventory · INV Type 2</span></footer>

    <div v-if="lookupOpen" class="modal-backdrop" role="presentation" @click.self="lookupOpen = false"><section class="workflow-modal" role="dialog" aria-modal="true" aria-label="Inventory Lookup"><p class="eyebrow">SEMI INVENTORY</p><h2>Inventory Lookup</h2><p>SEMI Inventory Basis · IssueState=2</p><button v-for="value in inventoryOptions" :key="value" type="button" class="lookup-option" @click="chooseInventory(value)">{{ value }}</button><button type="button" class="secondary" @click="lookupOpen = false">Cancel</button></section></div>
    <div v-if="workflow" class="modal-backdrop" role="presentation" @click.self="closeWorkflow"><section class="workflow-modal" role="dialog" aria-modal="true" :aria-label="workflow"><p class="eyebrow">SEMI INVENTORY</p><h2>{{ workflow }}</h2><p>舊 WinForms 會開啟獨立的 {{ workflow === 'SEMI In' ? '半成品入庫確認（IV404）' : '半成品出庫確認（IV403）' }} 視窗。這兩個交易模組由各自 Function 維護，本頁 DEV 不變更庫存。</p><button type="button" class="primary" @click="closeWorkflow">返回查詢</button></section></div>
  </main>
</template>

<style scoped>
.semi-inventory { height: 100%; min-height: 0; overflow: auto; box-sizing: border-box; padding: 18px 22px 22px; background: #f1f2ef; color: #2f352f; font: 13px/1.45 "Segoe UI", "Microsoft JhengHei", sans-serif; }
.page-head,.head-actions,.query-title,.query-row,.action-row,.section-head,.page-footer { display: flex; align-items: center; justify-content: space-between; gap: 13px; }
.page-head { max-width: 1440px; margin: 0 auto 15px; }
.page-head h1 { margin: 0; color: #29372c; font-size: 22px; line-height: 1.25; }
.eyebrow { margin: 0 0 3px; color: #587e63; font-size: 11px; font-weight: 700; letter-spacing: .12em; }
.subtitle,.query-title p,.section-head p { margin: 3px 0 0; color: #767c73; font-size: 11px; }
.head-actions { flex-shrink: 0; }
.dev-tag,.read-only,.source-pill { display: inline-block; border: 1px solid #c0d5c4; border-radius: 999px; padding: 4px 9px; color: #3c6948; background: #edf5ed; font-size: 10px; font-weight: 700; }
.close,.secondary,.primary { min-height: 32px; border: 1px solid #cdd3cc; border-radius: 5px; padding: 5px 12px; background: white; color: #3d483f; font: inherit; cursor: pointer; }
.primary { border-color: #477c55; background: #477c55; color: white; font-weight: 700; }
.query-card,.inventory-card,.detail-card { max-width: 1440px; margin: 0 auto 14px; border: 1px solid #d9ded7; border-radius: 8px; background: white; box-shadow: 0 2px 9px #252e260a; }
.query-card { padding: 14px 16px 11px; }
.query-title { margin-bottom: 12px; }
.query-title h2,.section-head h2,.workflow-modal h2 { margin: 0; color: #344538; font-size: 14px; }
.source-note,.action-row > span,.detail-note { color: #7b8178; font-size: 10px; }
.query-row { justify-content: flex-start; }
.field { display: grid; gap: 4px; min-width: 230px; color: #596257; font-size: 11px; font-weight: 650; }
.field select,.field input { min-height: 33px; box-sizing: border-box; border: 1px solid #d1d8d0; border-radius: 4px; padding: 5px 8px; background: white; color: #303830; font: inherit; font-size: 12px; }
.or { color: #888e86; font-size: 10px; }
.lookup-button { align-self: end; min-width: 40px; padding-inline: 8px; }
.notice { margin: 10px 0 0; border-left: 3px solid #77a47d; padding: 5px 9px; color: #5f6d60; background: #f4f8f3; font-size: 10px; }
.action-row { max-width: 1440px; margin: 0 auto 10px; }
.action-row > div { display: flex; gap: 7px; }
.data-layout { display: grid; grid-template-columns: minmax(610px,1.25fr) minmax(380px,.75fr); gap: 14px; max-width: 1440px; margin: 0 auto 14px; }
.data-layout > section { min-width: 0; margin-bottom: 0; overflow: hidden; }
.section-head { padding: 12px 14px 10px; border-bottom: 1px solid #e8ece7; }
.table-wrap { overflow: auto; }
table { width: 100%; border-collapse: collapse; white-space: nowrap; font-size: 10px; }
th,td { border-bottom: 1px solid #e9ede8; padding: 8px 9px; text-align: left; }
th { position: sticky; top: 0; background: #f4f7f3; color: #596759; font-weight: 700; }
tbody tr { cursor: pointer; }
tbody tr:hover { background: #f8faf7; }
tbody tr.selected { background: #eaf3e9; }
.code { color: #45684d; font-family: Consolas, "Courier New", monospace; }
.numeric { text-align: right; }
.empty { height: 90px; color: #899087; text-align: center; vertical-align: middle; }
.detail-empty { display: grid; min-height: 140px; place-items: center; color: #899087; font-size: 11px; }
.detail-note { margin: 0; border-top: 1px solid #edf0eb; padding: 8px 13px; }
.page-footer { max-width: 1440px; margin: 0 auto; color: #81877e; font-size: 9px; }
.modal-backdrop { position: fixed; inset: 0; z-index: 50; display: grid; place-items: center; padding: 16px; background: #18221a88; }
.workflow-modal { width: min(460px,100%); box-sizing: border-box; border: 1px solid #d9ded7; border-radius: 9px; padding: 20px; background: white; box-shadow: 0 16px 48px #16201635; }
.workflow-modal h2 { margin: 0 0 10px; font-size: 18px; }
.workflow-modal p:not(.eyebrow) { margin: 0 0 16px; color: #687168; font-size: 12px; }
.lookup-option { display: block; width: 100%; margin: 7px 0; border: 1px solid #d1d8d0; border-radius: 5px; padding: 8px 10px; background: #f8faf7; color: #3d483f; text-align: left; font: inherit; cursor: pointer; }
@media (max-width: 1120px) { .data-layout { grid-template-columns: 1fr; } }
@media (max-width: 680px) { .semi-inventory { padding: 12px; } .page-head { align-items: flex-start; } .head-actions { align-items: flex-end; flex-direction: column; gap: 6px; } .query-title,.query-row,.action-row,.section-head,.page-footer { align-items: flex-start; flex-direction: column; } .field { width: 100%; } .query-row > .primary { width: 100%; } }
</style>
