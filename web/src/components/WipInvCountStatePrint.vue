<script setup lang="ts">
import { computed, ref } from 'vue'

type DataType = 'WIP' | 'SEMIWIP' | 'FGD' | 'SEMI' | 'RAW' | 'MTLWIP'
type ColumnKey = 'countSerial' | 'countDate' | 'lotNo' | 'opNo' | 'curQty' | 'curUnitNo' | 'moNo' | 'productNo' | 'productVersion' | 'inventoryNo' | 'locatorNo' | 'materialNo' | 'materialLotNo'
type CountRow = {
  id: string
  countSerial: string
  countDate: string
  countState: 0 | 1
  dataType: DataType
  lotNo: string
  opNo: string
  curQty: number
  curUnitNo: string
  moNo: string
  productNo: string
  productVersion: string
  inventoryNo: string
  locatorNo: string
  materialNo: string
  materialLotNo: string
}
type ReportKind = 'general' | 'material' | 'product'

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const types: { value: DataType; label: string; group: string }[] = [
  { value: 'WIP', label: 'WIP 生產批', group: '生產批盤點' },
  { value: 'SEMIWIP', label: 'SEMIWIP 線邊半成品', group: '生產批盤點' },
  { value: 'FGD', label: 'FGD 成品庫', group: '產品庫盤點' },
  { value: 'SEMI', label: 'SEMI 半成品庫', group: '產品庫盤點' },
  { value: 'RAW', label: 'RAW 原物料庫', group: '原物料盤點' },
  { value: 'MTLWIP', label: 'MTLWIP 線邊原物料', group: '原物料盤點' },
]
const fields: Record<DataType, ColumnKey[]> = {
  WIP: ['countSerial', 'countDate', 'lotNo', 'opNo', 'curQty', 'curUnitNo', 'moNo', 'productNo', 'productVersion', 'inventoryNo'],
  SEMIWIP: ['countSerial', 'countDate', 'lotNo', 'curQty', 'curUnitNo', 'moNo', 'productNo', 'productVersion', 'inventoryNo'],
  FGD: ['countSerial', 'countDate', 'lotNo', 'curQty', 'curUnitNo', 'productNo', 'inventoryNo', 'locatorNo'],
  SEMI: ['countSerial', 'countDate', 'lotNo', 'curQty', 'curUnitNo', 'productNo', 'inventoryNo', 'locatorNo'],
  RAW: ['countSerial', 'countDate', 'materialNo', 'materialLotNo', 'curQty', 'curUnitNo', 'inventoryNo', 'locatorNo'],
  MTLWIP: ['countSerial', 'countDate', 'materialNo', 'materialLotNo', 'curQty', 'curUnitNo', 'inventoryNo', 'locatorNo'],
}
const labels: Record<ColumnKey, string> = {
  countSerial: 'CountSerial', countDate: 'CountDate', lotNo: 'LotNo', opNo: 'OPNo', curQty: 'CurQty', curUnitNo: 'CurUnitNo',
  moNo: 'MONo', productNo: 'ProductNo', productVersion: 'ProductVersion', inventoryNo: 'InventoryNo', locatorNo: 'LocatorNo',
  materialNo: 'MaterialNo', materialLotNo: 'MaterialLotNo',
}
const rows = ref<CountRow[]>([
  { id: 'wip-1', countSerial: 'DEV-IC-608-001', countDate: '2026-11-20 18:00', countState: 0, dataType: 'WIP', lotNo: 'DEV-LOT-608-001', opNo: 'DEV-OP-120', curQty: 100, curUnitNo: 'PCS', moNo: 'DEV-MO-608-01', productNo: 'DEV-PRD-608', productVersion: 'A1', inventoryNo: 'DEV-WIP-01', locatorNo: '', materialNo: '', materialLotNo: '' },
  { id: 'wip-2', countSerial: 'DEV-IC-608-002', countDate: '2026-11-21 18:00', countState: 0, dataType: 'WIP', lotNo: 'DEV-LOT-608-002', opNo: 'DEV-OP-240', curQty: 64, curUnitNo: 'PCS', moNo: 'DEV-MO-608-02', productNo: 'DEV-PRD-609', productVersion: 'B1', inventoryNo: 'DEV-WIP-01', locatorNo: '', materialNo: '', materialLotNo: '' },
  { id: 'semiwip-1', countSerial: 'DEV-IC-608-001', countDate: '2026-11-20 18:00', countState: 0, dataType: 'SEMIWIP', lotNo: 'DEV-LOT-608-003', opNo: 'DEV-OP-240', curQty: 35, curUnitNo: 'BOX', moNo: 'DEV-MO-608-02', productNo: 'DEV-PRD-609', productVersion: 'B1', inventoryNo: 'DEV-WIP-SEMI-01', locatorNo: '', materialNo: '', materialLotNo: '' },
  { id: 'fgd-1', countSerial: 'DEV-IC-608-001', countDate: '2026-11-20 18:00', countState: 0, dataType: 'FGD', lotNo: 'DEV-LOT-608-004', opNo: '', curQty: 40, curUnitNo: 'PCS', moNo: 'DEV-MO-608-03', productNo: 'DEV-PRD-610', productVersion: 'A1', inventoryNo: 'DEV-FGD-01', locatorNo: 'DEV-LOC-A01', materialNo: '', materialLotNo: '' },
  { id: 'fgd-2', countSerial: 'DEV-IC-608-002', countDate: '2026-11-21 18:00', countState: 0, dataType: 'FGD', lotNo: 'DEV-LOT-608-005', opNo: '', curQty: 18, curUnitNo: 'PCS', moNo: 'DEV-MO-608-03', productNo: 'DEV-PRD-610', productVersion: 'A1', inventoryNo: 'DEV-FGD-01', locatorNo: 'DEV-LOC-A02', materialNo: '', materialLotNo: '' },
  { id: 'semi-1', countSerial: 'DEV-IC-608-001', countDate: '2026-11-20 18:00', countState: 0, dataType: 'SEMI', lotNo: 'DEV-LOT-608-006', opNo: '', curQty: 22, curUnitNo: 'PCS', moNo: 'DEV-MO-608-04', productNo: 'DEV-PRD-611', productVersion: 'C2', inventoryNo: 'DEV-SEMI-01', locatorNo: 'DEV-LOC-S01', materialNo: '', materialLotNo: '' },
  { id: 'raw-1', countSerial: 'DEV-IC-608-001', countDate: '2026-11-20 18:00', countState: 0, dataType: 'RAW', lotNo: '', opNo: '', curQty: 280, curUnitNo: 'KG', moNo: '', productNo: '', productVersion: '', inventoryNo: 'DEV-RAW-01', locatorNo: 'DEV-LOC-R01', materialNo: 'DEV-MTL-608', materialLotNo: 'DEV-MTLLOT-001' },
  { id: 'raw-2', countSerial: 'DEV-IC-608-002', countDate: '2026-11-21 18:00', countState: 0, dataType: 'RAW', lotNo: '', opNo: '', curQty: 195, curUnitNo: 'KG', moNo: '', productNo: '', productVersion: '', inventoryNo: 'DEV-RAW-01', locatorNo: 'DEV-LOC-R02', materialNo: 'DEV-MTL-608', materialLotNo: 'DEV-MTLLOT-002' },
  { id: 'mtlwip-1', countSerial: 'DEV-IC-608-001', countDate: '2026-11-20 18:00', countState: 0, dataType: 'MTLWIP', lotNo: '', opNo: '', curQty: 72, curUnitNo: 'PCS', moNo: '', productNo: '', productVersion: '', inventoryNo: 'DEV-WIP-RAW-01', locatorNo: '', materialNo: 'DEV-MTL-609', materialLotNo: 'DEV-MTLLOT-003' },
])
const dataType = ref<DataType>('WIP')
const barcode = ref(true)
const queried = ref(false)
const showPreview = ref(false)
const printed = ref(false)
const error = ref('')
const info = ref('')
const typeInfo = computed(() => types.find(item => item.value === dataType.value)!)
const isLotType = computed(() => dataType.value === 'WIP' || dataType.value === 'SEMIWIP')
const sourceRows = computed(() => queried.value ? rows.value.filter(row => row.dataType === dataType.value && row.countState === 0) : [])
const displayRows = computed(() => {
  const sortKeys: (keyof CountRow)[] = isLotType.value ? ['opNo', 'productNo'] : dataType.value === 'RAW' || dataType.value === 'MTLWIP' ? ['inventoryNo', 'materialNo'] : ['inventoryNo', 'productNo']
  return [...sourceRows.value].sort((left, right) => {
    for (const key of sortKeys) {
      const compare = String(left[key]).localeCompare(String(right[key]), 'zh-TW')
      if (compare) return compare
    }
    return left.countSerial.localeCompare(right.countSerial)
  })
})
const reportKind = computed<ReportKind>(() => isLotType.value ? 'general' : dataType.value === 'RAW' || dataType.value === 'MTLWIP' ? 'material' : 'product')
const reportClassName = computed(() => {
  const base = reportKind.value === 'general' ? 'arInvCount_General' : reportKind.value === 'material' ? 'arInvCount_MTL' : 'arInvCount_PRD'
  if (barcode.value) return base
  return reportKind.value === 'general' ? `${base}_NoBarCode` : `${base}_NoBarcode`
})
const reportGroupName = computed(() => isLotType.value ? dataType.value : '')
const reportLabel = computed(() => reportKind.value === 'general' ? '生產批盤點清冊' : reportKind.value === 'material' ? '原物料盤點清冊' : '產品庫盤點清冊')

function query(): void {
  error.value = ''
  info.value = ''
  showPreview.value = false
  printed.value = false
  if (!dev.value) { error.value = '正式盤點列印服務尚未接入；此頁不會讀取或列印資料。'; return }
  queried.value = true
  info.value = `已載入 State=0 的 ${dataType.value} 盤點資料，共 ${displayRows.value.length} 筆（DEV）。`
}

function changeType(value: DataType): void {
  dataType.value = value
  queried.value = false
  showPreview.value = false
  printed.value = false
  error.value = ''
  info.value = ''
}

function openPrintPreview(): void {
  error.value = ''
  if (!queried.value || !displayRows.value.length) { error.value = '請先查詢有資料的盤點類型，才能預覽列印。'; return }
  showPreview.value = true
  printed.value = false
}

function simulatePrint(): void {
  printed.value = true
  info.value = 'DEV 列印已模擬；沒有產生條碼檔、PDF 或送到系統印表機。'
}

function displayValue(row: CountRow, key: ColumnKey): string {
  return key === 'curQty' ? new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 4 }).format(row.curQty) : String(row[key] || '—')
}
</script>

<template>
  <section class="count-print">
    <header class="hero"><div><p class="eyebrow">WIP · INVENTORY CONTROL · WP607</p><h1>列印盤點清冊</h1><p class="subtitle">依盤點庫存類型查詢已關帳明細，選擇含條碼或不含條碼版型</p></div><div class="hero-actions"><span class="dev-pill">DEV 假資料</span><button class="quiet" type="button" @click="emit('close')">關閉</button></div></header>
    <div v-if="!dev" class="unavailable">正式盤點列印服務尚未接入；此頁不會讀取或列印資料。</div>
    <template v-else>
      <div class="flow"><b>選盤點類型</b><span>→</span><b>Query State=0 明細</b><span>→</span><b>選條碼版型</b><span>→</span><b>預覽清冊</b></div>
      <div v-if="error || info" class="notice" :class="{ bad: !!error }" role="status">{{ error || info }}</div>
      <section class="card filters"><div class="card-heading"><div><h2>盤點庫存類型</h2><p>列出該 DataType 全部 State=0 已關帳盤點批次，依 WinForms 規則排序。</p></div><span class="setting">初始類型 WIP · Barcode 預設勾選</span></div><div class="type-options"><label v-for="item in types" :key="item.value" :class="{ active: dataType === item.value }"><input type="radio" name="inv-count-print-type" :value="item.value" :checked="dataType === item.value" @change="changeType(item.value)"><span>{{ item.label }}</span><small>{{ item.group }}</small></label><button class="primary" type="button" @click="query">Query</button></div></section>
      <div class="summary"><div><small>DataType</small><strong>{{ dataType }}</strong><small>{{ typeInfo.label }}</small></div><div><small>盤點明細</small><strong>{{ displayRows.length }}</strong></div><div><small>CountSerial</small><strong>{{ new Set(displayRows.map(row => row.countSerial)).size }}</strong></div><div class="summary-spacer"></div><label class="barcode-option"><input v-model="barcode" type="checkbox"> Barcode</label><button class="primary" type="button" :disabled="!displayRows.length" @click="openPrintPreview">Print</button></div>
      <section v-if="showPreview" class="card preview"><div class="card-heading"><div><h2>{{ reportLabel }}</h2><p>ActiveReport：{{ reportClassName }}<span v-if="reportGroupName"> · PrintGroupName={{ reportGroupName }}</span> · {{ displayRows.length }} 筆</p></div><div class="preview-actions"><button class="quiet" type="button" @click="showPreview = false">關閉預覽</button><button class="primary" type="button" @click="simulatePrint">{{ printed ? '已模擬列印' : '模擬列印' }}</button></div></div><p class="preview-note">版型與排序依 WinForms 選擇；DEV 不產出真正的 ActiveReport 文件、條碼影像或印表機工作。</p><div class="report-sheet"><div class="report-title"><strong>{{ reportLabel }}</strong><span>盤點類型：{{ dataType }}<template v-if="reportGroupName">（{{ reportGroupName }}）</template></span><span>盤點日期：{{ displayRows[0]?.countDate }}</span></div><div class="report-table-wrap"><table><thead><tr><th v-for="column in fields[dataType]" :key="column">{{ labels[column] }}</th></tr></thead><tbody><tr v-for="row in displayRows" :key="row.id"><td v-for="column in fields[dataType]" :key="column">{{ displayValue(row, column) }}</td></tr></tbody></table></div></div><p v-if="printed" class="print-note">已完成 DEV 列印模擬，沒有呼叫作業系統印表機。</p></section>
      <section class="card grid-card"><div class="card-heading"><div><h2>{{ typeInfo.label }}盤點資料</h2><p>{{ queried ? '來源：LoadInvCountStateWithDetail* · State=0 · 已依類型排序' : '按 Query 載入 DEV 盤點明細' }}</p></div><span class="filter-note">{{ displayRows.length }} 筆</span></div><div class="table-wrap"><table><thead><tr><th v-for="column in fields[dataType]" :key="column">{{ labels[column] }}</th></tr></thead><tbody><tr v-for="row in displayRows" :key="row.id"><td v-for="column in fields[dataType]" :key="column" :class="{ serial: column === 'countSerial' || column === 'lotNo' || column === 'materialLotNo', num: column === 'curQty' }">{{ displayValue(row, column) }}</td></tr><tr v-if="!displayRows.length"><td :colspan="fields[dataType].length" class="empty">{{ queried ? '此 DataType 沒有 State=0 DEV 盤點資料' : '尚未查詢' }}</td></tr></tbody></table></div></section>
      <footer>DEV 只模擬查詢、報表版型選擇與預覽；不呼叫 MES Service、資料庫、ActiveReports 執行環境或本機印表機。</footer>
    </template>
  </section>
</template>

<style scoped>
.count-print{--ink:#1b2a3c;--muted:#708094;--line:#dce4eb;color:var(--ink);height:100%;min-height:0;overflow:auto;padding:22px 26px 30px;background:#f7f9fb;font:13px/1.45 "Segoe UI","Microsoft JhengHei",sans-serif}.hero{display:flex;align-items:center;justify-content:space-between;margin-bottom:15px}.eyebrow{margin:0;color:#73869a;font-size:10px;font-weight:750;letter-spacing:.13em}.hero h1{margin:2px 0 0;font-size:23px}.subtitle{margin:0;color:var(--muted);font-size:12px}.hero-actions,.preview-actions{display:flex;align-items:center;gap:9px}.dev-pill,.setting{display:inline-block;padding:4px 8px;border-radius:999px;background:#e8f4ed;color:#22724a;font-size:10px;font-weight:700}.setting{background:#edf2f8;color:#385a78}.quiet,.primary{height:34px;padding:0 12px;border:1px solid var(--line);border-radius:5px;background:white;color:#334d66;cursor:pointer;font:inherit}.primary{border-color:#173b5d;background:#173b5d;color:white;font-weight:700}.primary:disabled{opacity:.45;cursor:not-allowed}.flow{display:flex;align-items:center;gap:10px;padding:10px 14px;margin-bottom:13px;border:1px solid #ccdae7;border-radius:6px;background:#eff5fa;color:#48637c;font-size:11px}.flow b{color:#234663}.card{margin-bottom:13px;border:1px solid var(--line);border-radius:7px;background:white;box-shadow:0 2px 5px #10283a08}.filters{padding:13px 15px}.card-heading{display:flex;align-items:center;justify-content:space-between;padding:11px 14px}.filters .card-heading{padding:0 0 10px}.card-heading h2{margin:0;font-size:14px}.card-heading p{margin:2px 0 0;color:var(--muted);font-size:10px}.type-options{display:flex;align-items:center;gap:8px;flex-wrap:wrap}.type-options label{display:grid;grid-template-columns:18px auto;grid-template-rows:auto auto;align-items:center;column-gap:5px;padding:7px 10px;border:1px solid var(--line);border-radius:5px;background:white;color:#465d73;font-size:11px;cursor:pointer}.type-options label.active{border-color:#6695b4;background:#f0f7fb}.type-options input{grid-row:span 2;accent-color:#1d6289}.type-options small{grid-column:2;color:#8997a5;font-size:9px}.type-options .primary{margin-left:auto}.notice{margin:0 0 12px;padding:9px 12px;border:1px solid #bddcc9;border-radius:5px;background:#eef8f1;color:#286440}.notice.bad{border-color:#ebc5c1;background:#fff3f1;color:#9e3831}.summary{display:flex;align-items:center;gap:0;margin:12px 0;background:white;border:1px solid var(--line);border-radius:7px;overflow:hidden}.summary>div{min-width:110px;padding:8px 11px;border-right:1px solid var(--line);display:grid;grid-template-columns:1fr auto;align-items:center;column-gap:7px}.summary small{color:var(--muted);font-size:9px}.summary strong{grid-row:span 2;color:#244f71;font-size:16px}.summary .summary-spacer{flex:1;border:0}.barcode-option{display:flex;align-items:center;gap:5px;padding:0 10px;color:#536d86;font-size:11px}.barcode-option input{accent-color:#1d6289}.summary>.primary{margin:6px}.grid-card{overflow:hidden}.filter-note{color:#73869a;font-size:10px}.table-wrap{overflow:auto;border-top:1px solid #e6ebf0}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:10px}th{position:sticky;top:0;background:#f1f5f8;color:#607388;font-size:9px;font-weight:700;text-align:left}th,td{padding:7px 8px;border-bottom:1px solid #ebeff3}.serial{color:#60768b;font-family:Consolas,monospace}.num{text-align:right;font-variant-numeric:tabular-nums}.empty{text-align:center;color:#8a98a7;padding:17px}.preview{overflow:hidden}.preview-note{margin:0 14px 10px;color:#78899a;font-size:10px}.report-sheet{margin:0 14px 12px;padding:18px;border:1px solid #d9e1e8;background:white;box-shadow:0 2px 8px #112b3d12}.report-title{display:flex;justify-content:space-between;gap:12px;padding-bottom:12px;border-bottom:2px solid #344e65;color:#405a70;font-size:10px}.report-title strong{font-size:13px;color:#263d53}.report-table-wrap{overflow:auto}.report-table-wrap table{margin-top:8px;white-space:nowrap}.print-note{margin:0 14px 10px;color:#22724a;font-size:10px}footer{padding:2px 2px 8px;color:#8391a0;font-size:10px}.unavailable{padding:16px;border:1px solid #e8d2ac;border-radius:6px;background:#fff8ea;color:#7a5622}@media(max-width:1100px){.count-print{padding:14px}.flow{align-items:flex-start;flex-wrap:wrap}.summary{flex-wrap:wrap}.summary .summary-spacer{display:none}.report-title{flex-wrap:wrap}}
</style>
