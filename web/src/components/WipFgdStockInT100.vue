<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

type ResultMode = 'success' | 'erp-warning' | 'erp-fail' | 'mes-fail' | 'mes-fail-cancel-fail'
type LotRow = { id: string; moNo: string; lotNo: string; orgLotNo: string; baseLotNo: string; productNo: string; productVersion: string; qty: number; unitNo: string; sourceInventoryNo: string; inputDate: string; bookingFlag: number; components: string[]; selected: boolean }
type HistoryRow = { time: string; fgdinNo: string; moNo: string; lots: number; qty: number; erp: string; mes: string; compensation: string; booking: string }

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const form = reactive({ docType: dev.value ? 'DEV-T100-FG-01' : '', moNo: dev.value ? 'DEV-MO-616-01' : '', destInventoryNo: dev.value ? 'DEV-FGD-616' : '', locatorNo: '', sourceInventoryNo: dev.value ? 'DEV-WIP-616' : '', referenceLot: '', scanLot: '', scanMode: 'select' as 'select' | 'remove' })
const docTypes = dev.value ? [{ value: 'DEV-T100-FG-01', label: 'DEV-T100-FG-01 · 成品入庫' }, { value: 'DEV-T100-FG-02', label: 'DEV-T100-FG-02 · 製程完工' }] : []
const moOptions = dev.value ? ['DEV-MO-616-01', 'DEV-MO-616-02'] : []
const destinationInventories = dev.value ? ['DEV-FGD-616', 'DEV-FGD-617'] : []
const sourceInventories = dev.value ? ['DEV-WIP-616', 'DEV-WIP-617'] : []
const locators = dev.value ? ['A-01-01', 'A-01-02'] : []
const allLots = ref<LotRow[]>(dev.value ? [
  { id: 't1', moNo: 'DEV-MO-616-01', lotNo: 'DEV-LOT-616-001', orgLotNo: 'DEV-ORG-LOT-616-001', baseLotNo: 'DEV-BASE-616-001', productNo: 'DEV-PRD-616', productVersion: 'A1', qty: 120, unitNo: 'PCS', sourceInventoryNo: 'DEV-WIP-616', inputDate: '2026-11-20 08:10', bookingFlag: 0, components: ['DEV-COMP-616-A'], selected: false },
  { id: 't2', moNo: 'DEV-MO-616-01', lotNo: 'DEV-LOT-616-002', orgLotNo: 'DEV-ORG-LOT-616-002', baseLotNo: 'DEV-BASE-616-002', productNo: 'DEV-PRD-616', productVersion: 'A1', qty: 96, unitNo: 'PCS', sourceInventoryNo: 'DEV-WIP-616', inputDate: '2026-11-20 09:42', bookingFlag: 0, components: [], selected: false },
  { id: 't3', moNo: 'DEV-MO-616-01', lotNo: 'DEV-LOT-616-003', orgLotNo: 'DEV-ORG-LOT-616-003', baseLotNo: 'DEV-BASE-616-003', productNo: 'DEV-PRD-616', productVersion: 'A1', qty: 44, unitNo: 'PCS', sourceInventoryNo: 'DEV-WIP-617', inputDate: '2026-11-20 10:25', bookingFlag: 0, components: ['DEV-COMP-616-B', 'DEV-COMP-616-C'], selected: false },
  { id: 't4', moNo: 'DEV-MO-616-02', lotNo: 'DEV-LOT-616-004', orgLotNo: 'DEV-ORG-LOT-616-004', baseLotNo: 'DEV-BASE-616-004', productNo: 'DEV-PRD-617', productVersion: 'B3', qty: 80, unitNo: 'PCS', sourceInventoryNo: 'DEV-WIP-617', inputDate: '2026-11-21 07:52', bookingFlag: 0, components: [], selected: false },
  { id: 't5', moNo: 'DEV-MO-616-01', lotNo: 'DEV-LOT-616-005', orgLotNo: 'DEV-ORG-LOT-616-005', baseLotNo: 'DEV-BASE-616-005', productNo: 'DEV-PRD-616', productVersion: 'A1', qty: 15, unitNo: 'PCS', sourceInventoryNo: 'DEV-WIP-616', inputDate: '2026-11-21 08:35', bookingFlag: 1, components: [], selected: false },
] : [])
const history = ref<HistoryRow[]>([])
const resultMode = ref<ResultMode>('success')
const leftChecked = ref<string[]>([])
const rightChecked = ref<string[]>([])
const queried = ref(false)
const working = ref(false)
const error = ref('')
const info = ref('')
const nextDocNo = ref(1001)
const visibleLots = computed(() => allLots.value.filter(row => row.moNo === form.moNo && row.bookingFlag === 0 && row.qty > 0))
const availableLots = computed(() => visibleLots.value.filter(row => !row.selected))
const selectedLots = computed(() => visibleLots.value.filter(row => row.selected))
const selectedQty = computed(() => selectedLots.value.reduce((sum, row) => sum + row.qty, 0))
const formatQty = (qty: number) => new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 6 }).format(qty)
const nowText = () => new Date().toISOString().slice(0, 19).replace('T', ' ')

function clearMessages(): void { error.value = ''; info.value = '' }
function allowDev(): boolean {
  if (dev.value) return true
  error.value = '正式 T100／MES 入庫整合尚未接入；此頁不會讀取或送出資料。'
  return false
}
function queryMo(): void {
  clearMessages()
  if (!allowDev()) return
  queried.value = true
  leftChecked.value = []
  rightChecked.value = []
  allLots.value.forEach(row => { if (row.moNo !== form.moNo) row.selected = false })
  info.value = `已載入 ${availableLots.value.length} 筆 BookingFlag=0 的線邊在製 Lot；已預約或已入庫資料不列出。`
}
function findReferenceLot(): void {
  clearMessages()
  if (!allowDev()) return
  const value = form.referenceLot.trim().toUpperCase()
  const row = allLots.value.find(item => item.lotNo.toUpperCase() === value || item.orgLotNo.toUpperCase() === value)
  if (!value) { error.value = '請輸入 Ref. Lot No。'; return }
  if (!row) { error.value = '找不到此 Ref. Lot No 的 DEV Lot。'; return }
  form.moNo = row.moNo
  form.sourceInventoryNo = row.sourceInventoryNo
  queryMo()
  info.value = `已依參考 Lot 載入工單 ${row.moNo} 與來源庫 ${row.sourceInventoryNo}。`
}
function moveSelected(direction: 'right' | 'left'): void {
  clearMessages()
  if (!allowDev()) return
  const keys = direction === 'right' ? leftChecked.value : rightChecked.value
  const rows = direction === 'right' ? availableLots.value : selectedLots.value
  rows.filter(row => keys.includes(row.id)).forEach(row => { row.selected = direction === 'right' })
  leftChecked.value = []
  rightChecked.value = []
}
function moveAll(direction: 'right' | 'left'): void {
  clearMessages()
  if (!allowDev()) return
  ;(direction === 'right' ? availableLots.value : selectedLots.value).forEach(row => { row.selected = direction === 'right' })
  leftChecked.value = []
  rightChecked.value = []
}
function scanLot(): void {
  clearMessages()
  if (!allowDev()) return
  const value = form.scanLot.trim().toUpperCase()
  const row = visibleLots.value.find(item => item.sourceInventoryNo === form.sourceInventoryNo && (item.lotNo.toUpperCase() === value || item.orgLotNo.toUpperCase() === value))
  if (!value) { error.value = '請輸入 Lot No。'; return }
  if (!row) { error.value = '此 Lot 不在目前工單／來源庫的可用清單。'; return }
  row.selected = form.scanMode === 'select'
  form.scanLot = ''
  info.value = form.scanMode === 'select' ? `${row.lotNo} 已加入入庫清單。` : `${row.lotNo} 已移回可選清單。`
}
function confirmStockIn(): void {
  clearMessages()
  if (!allowDev() || working.value) return
  if (!form.docType) { error.value = '請選擇 T100 入庫單別。'; return }
  if (!form.moNo) { error.value = '請選擇工單。'; return }
  if (!form.destInventoryNo) { error.value = '請選擇成品庫。'; return }
  if (!selectedLots.value.length) { error.value = '請至少選擇一個 Lot。'; return }
  working.value = true
  const picked = [...selectedLots.value]
  const totalQty = picked.reduce((sum, row) => sum + row.qty, 0)
  const timestamp = nowText()
  picked.forEach(row => { row.bookingFlag = 1 })
  const fgdinNo = 'DEV-T100-FGD-' + String(nextDocNo.value++).padStart(5, '0')

  if (resultMode.value === 'erp-fail') {
    picked.forEach(row => { row.bookingFlag = 0; row.selected = false })
    history.value.unshift({ time: timestamp, fgdinNo: '—', moNo: form.moNo, lots: picked.length, qty: totalQty, erp: '失敗', mes: '未執行', compensation: '未建立 ERP 單', booking: 'BookingFlag 已重設' })
    error.value = 'DEV 模擬 T100 ERP 建單失敗；MES 未執行，已釋放 Lot 預約並刷新清單。'
  } else if (resultMode.value === 'mes-fail' || resultMode.value === 'mes-fail-cancel-fail') {
    picked.forEach(row => { row.bookingFlag = 0; row.selected = false })
    const cancelFailed = resultMode.value === 'mes-fail-cancel-fail'
    history.value.unshift({ time: timestamp, fgdinNo, moNo: form.moNo, lots: picked.length, qty: totalQty, erp: cancelFailed ? '成功（取消失敗）' : '成功', mes: '失敗', compensation: cancelFailed ? 'ERP 取消失敗／需人工對帳' : '已模擬 T100 ERP 取消單', booking: 'BookingFlag 已重設' })
    error.value = cancelFailed ? `MES 入庫失敗；T100 ERP 取消 ${fgdinNo} 也失敗，ERP/MES 結果需人工對帳。來源流程仍會釋放預約。` : `MES 入庫失敗；已模擬 T100 ERP 取消單 ${fgdinNo} 並釋放 Lot 預約。`
  } else {
    picked.forEach(row => { row.qty = 0; row.bookingFlag = 0; row.selected = false })
    const warning = resultMode.value === 'erp-warning'
    history.value.unshift({ time: timestamp, fgdinNo, moNo: form.moNo, lots: picked.length, qty: totalQty, erp: warning ? 'W 警告（續行）' : '成功', mes: '成功', compensation: '—', booking: '來源 Lot 已入成品庫' })
    info.value = `DEV 模擬 T100 ERP${warning ? ' 回 W 警告（依來源仍視為成功並續行）' : ' 建單成功'}及 MES 入庫完成：${fgdinNo}，${picked.length} 個 Lot，共 ${formatQty(totalQty)} PCS。`
  }
  working.value = false
}
</script>

<template>
  <section class="t100-stockin">
    <header class="head"><div><p class="eyebrow">WIP · ERP INTEGRATION · WP616</p><h1>入庫至 T100 ERP</h1><p class="subtitle">FGD Stock In to T100</p></div><div class="head-actions"><span v-if="dev" class="dev-pill">DEV 假資料</span><button class="quiet" type="button" @click="emit('close')">關閉</button></div></header>
    <div v-if="!dev" class="unavailable">正式 T100／MES 入庫整合尚未接入；此頁不會連線或送出資料。</div>
    <div v-else class="flow"><b>選擇來源 WIP Lot</b><span>→</span><b>建立 T100 ERP 單</b><span>→</span><b>MES FGD 入庫</b><span>MES 失敗會嘗試取消 ERP 單；ERP W 會警告後續行；Form 的 auto=N 未由目前整合路徑送出</span></div>

    <section class="card form-card"><div class="form-grid"><label>ERP Sheet Type<select v-model="form.docType" :disabled="!dev"><option value="">請選擇</option><option v-for="item in docTypes" :key="item.value" :value="item.value">{{ item.label }}</option></select></label><label>MO No<select v-model="form.moNo" :disabled="!dev" @change="queryMo"><option v-for="item in moOptions" :key="item" :value="item">{{ item }}</option></select></label><label>目的成品庫（InventoryType=1）<select v-model="form.destInventoryNo" :disabled="!dev"><option value="">請選擇</option><option v-for="item in destinationInventories" :key="item" :value="item">{{ item }}</option></select></label><label>目的儲位（可空白）<select v-model="form.locatorNo" :disabled="!dev"><option value="">N/A</option><option v-for="item in locators" :key="item" :value="item">{{ item }}</option></select></label><label>來源線邊庫（InventoryType=6）<select v-model="form.sourceInventoryNo" :disabled="!dev" @change="queryMo"><option v-for="item in sourceInventories" :key="item" :value="item">{{ item }}</option></select></label><label>Ref. Lot No<input v-model="form.referenceLot" type="text" :disabled="!dev" placeholder="Lot／OrgLot，按 Enter" @keydown.enter.prevent="findReferenceLot"></label><button class="primary query" type="button" :disabled="!dev" @click="queryMo">載入工單 Lot</button></div><div class="scan-row"><label>掃描 Lot No<input v-model="form.scanLot" type="text" :disabled="!dev" placeholder="輸入後按 Enter" @keydown.enter.prevent="scanLot"></label><label class="radio"><input v-model="form.scanMode" type="radio" value="select" :disabled="!dev">Select</label><label class="radio"><input v-model="form.scanMode" type="radio" value="remove" :disabled="!dev">UnSelect</label><button class="quiet" type="button" :disabled="!dev" @click="scanLot">執行掃描</button><span>掃描結果依目前工單與來源庫加入／移出完整 Lot；不可改部分 Qty。</span></div></section>

    <div class="summary"><div><small>可選 Lot</small><strong>{{ queried ? availableLots.length : '—' }}</strong></div><div><small>已選 Lot</small><strong>{{ selectedLots.length }}</strong></div><div><small>入庫總 Qty</small><strong>{{ formatQty(selectedQty) }}</strong><small>PCS · 整批入庫</small></div><div><small>掃描來源庫／目的庫</small><strong class="small-value">{{ form.sourceInventoryNo || '—' }} → {{ form.destInventoryNo || '—' }}</strong></div><label>DEV 結果情境<select v-model="resultMode" :disabled="!dev"><option value="success">ERP、MES 成功</option><option value="erp-warning">ERP code=W（warning 續行）</option><option value="erp-fail">ERP 建單失敗</option><option value="mes-fail">MES 失敗，ERP 取消成功</option><option value="mes-fail-cancel-fail">MES 失敗，ERP 取消失敗</option></select></label></div>
    <div v-if="error || info" class="notice" :class="{ bad: !!error }" role="status">{{ error || info }}</div>

    <div class="transfer"><section class="card grid-card"><div class="card-title"><div><h2>WIP Inventory</h2><p>{{ form.moNo }} · 所有線邊庫來源 · BookingFlag=0</p></div><button class="quiet" type="button" :disabled="!dev || !availableLots.length" @click="moveAll('right')">全選 →</button></div><div class="table-scroll"><table><thead><tr><th></th><th>LotNo</th><th>來源庫</th><th>ProductNo／Version</th><th class="num">Qty</th><th>Unit</th><th>BaseLotNo</th><th>InputDate</th></tr></thead><tbody><tr v-for="row in (queried ? availableLots : [])" :key="row.id" @click="leftChecked = leftChecked.includes(row.id) ? leftChecked.filter(id => id !== row.id) : [...leftChecked, row.id]" :class="{ checked: leftChecked.includes(row.id) }"><td><input v-model="leftChecked" type="checkbox" :value="row.id" :disabled="!dev" @click.stop></td><td class="lot">{{ row.lotNo }}</td><td>{{ row.sourceInventoryNo }}</td><td>{{ row.productNo }} · {{ row.productVersion }}</td><td class="num">{{ formatQty(row.qty) }}</td><td>{{ row.unitNo }}</td><td>{{ row.baseLotNo }}</td><td>{{ row.inputDate }}</td></tr><tr v-if="queried && !availableLots.length"><td colspan="8" class="empty">目前工單沒有可選 Lot</td></tr><tr v-if="!queried"><td colspan="8" class="empty">請載入工單 Lot</td></tr></tbody></table></div></section><div class="move-controls"><button type="button" class="move" :disabled="!dev || !leftChecked.length" @click="moveSelected('right')">加入 →</button><button type="button" class="move" :disabled="!dev || !rightChecked.length" @click="moveSelected('left')">← 移回</button><button type="button" class="link" :disabled="!dev || !selectedLots.length" @click="moveAll('left')">清除已選</button></div><section class="card grid-card"><div class="card-title"><div><h2>Lot Selected</h2><p>整筆數量 · {{ selectedLots.length }} Lot / {{ formatQty(selectedQty) }} PCS</p></div><button class="quiet" type="button" :disabled="!dev || !availableLots.length" @click="moveAll('right')">Select All</button></div><div class="table-scroll"><table><thead><tr><th></th><th>LotNo</th><th>來源庫</th><th>ProductNo／Version</th><th class="num">Qty</th><th>Unit</th><th>元件摘要</th></tr></thead><tbody><tr v-for="row in (queried ? selectedLots : [])" :key="row.id" @click="rightChecked = rightChecked.includes(row.id) ? rightChecked.filter(id => id !== row.id) : [...rightChecked, row.id]" :class="{ checked: rightChecked.includes(row.id) }"><td><input v-model="rightChecked" type="checkbox" :value="row.id" :disabled="!dev" @click.stop></td><td class="lot">{{ row.lotNo }}</td><td>{{ row.sourceInventoryNo }}</td><td>{{ row.productNo }} · {{ row.productVersion }}</td><td class="num">{{ formatQty(row.qty) }}</td><td>{{ row.unitNo }}</td><td>{{ row.components.join('、') || '—' }}</td></tr><tr v-if="queried && !selectedLots.length"><td colspan="7" class="empty">尚未選取 Lot</td></tr></tbody></table></div></section></div>

    <section class="card history"><div class="card-title"><div><h2>DEV 入庫處理紀錄</h2><p>本頁記憶體假資料，不是真正 T100／MES 交易</p></div><button class="primary" type="button" :disabled="!dev || working" @click="confirmStockIn">{{ working ? '處理中…' : 'Confirm 入庫' }}</button></div><div class="table-scroll"><table><thead><tr><th>時間</th><th>FGDInNo</th><th>MO</th><th>Lot／數量</th><th>ERP</th><th>MES</th><th>補償</th><th>BookingFlag</th></tr></thead><tbody><tr v-for="(item, index) in history" :key="item.time + item.fgdinNo + index"><td>{{ item.time }}</td><td class="lot">{{ item.fgdinNo }}</td><td>{{ item.moNo }}</td><td>{{ item.lots }} / {{ formatQty(item.qty) }} PCS</td><td>{{ item.erp }}</td><td>{{ item.mes }}</td><td>{{ item.compensation }}</td><td>{{ item.booking }}</td></tr><tr v-if="!history.length"><td colspan="8" class="empty">尚無本頁操作</td></tr></tbody></table></div></section>
    <footer>ERP `stockin.data.create` 建單後，整筆 Lot／數量交 MES `MOStockIn_TP`。目前整合路徑未傳 Form 區域變數 auto=N；ERP 確認／過帳預設待家用端查證。`OrgLotNoToERP` 可決定送原始 LotNo 或目前 LotNo。</footer>
  </section>
</template>

<style scoped>
.t100-stockin{--ink:#1a293b;--muted:#6d7c8e;--line:#dce4eb;--navy:#173b5d;color:var(--ink);height:100%;min-height:0;overflow:auto;padding:22px 26px 30px;background:#f7f9fb;font:13px/1.45 "Segoe UI","Microsoft JhengHei",sans-serif}.head{display:flex;align-items:center;justify-content:space-between;margin-bottom:16px}.eyebrow{margin:0;color:#73869a;font-size:10px;font-weight:750;letter-spacing:.13em}.head h1{margin:2px 0 0;font-size:23px}.subtitle{margin:0;color:var(--muted);font-size:12px}.head-actions{display:flex;align-items:center;gap:12px}.dev-pill{padding:5px 10px;border-radius:999px;background:#e8f4ed;color:#22724a;font-weight:700}.quiet,.link{border:1px solid var(--line);background:white;color:#334d66;border-radius:5px;padding:7px 12px;cursor:pointer}.primary{height:34px;padding:0 15px;border:1px solid var(--navy);border-radius:4px;background:var(--navy);color:white;font:inherit;font-weight:700;cursor:pointer}.primary:disabled,.quiet:disabled,.link:disabled,.move:disabled{opacity:.45;cursor:not-allowed}.flow{display:flex;align-items:center;gap:12px;padding:11px 15px;margin-bottom:14px;border:1px solid #cddce9;border-radius:6px;background:#f1f7fc;color:#45627d}.flow b{color:#294d6f}.flow span:last-child{margin-left:auto;color:#71859a;font-size:10px}.card{margin-bottom:13px;border:1px solid var(--line);border-radius:7px;background:white;box-shadow:0 2px 5px #10283a08}.form-card{padding:14px}.form-grid{display:grid;grid-template-columns:1.3fr 1fr 1.25fr .9fr 1.2fr 1.2fr auto;gap:10px;align-items:end}.form-grid label,.scan-row>label:not(.radio),.summary label{display:flex;flex-direction:column;gap:5px;color:#5e7185;font-size:10px;font-weight:650}.form-grid select,.form-grid input,.scan-row input,.summary select{height:33px;min-width:0;border:1px solid #cdd8e2;border-radius:4px;padding:0 8px;background:white;color:var(--ink);font:inherit}.query{white-space:nowrap}.scan-row{display:flex;align-items:end;gap:10px;margin-top:12px;padding-top:11px;border-top:1px solid #e8edf1}.scan-row>label:first-child{width:220px}.radio{display:flex;align-items:center;gap:5px;height:32px;color:#50667d;font-size:10px}.radio input{height:auto}.scan-row span{margin:0 0 8px auto;color:var(--muted);font-size:10px}.summary{display:flex;align-items:center;gap:0;margin:12px 0;background:white;border:1px solid var(--line);border-radius:7px;overflow:hidden}.summary>div{min-width:108px;padding:9px 12px;border-right:1px solid var(--line);display:grid;grid-template-columns:1fr auto;align-items:center;column-gap:8px}.summary small{color:var(--muted);font-size:10px}.summary strong{grid-row:span 2;color:#27658a;font-size:18px}.summary strong.small-value{font-size:11px}.summary label{margin-left:auto;padding:8px 12px;flex-direction:row;align-items:center}.summary select{min-width:200px;height:30px}.notice{margin:0 0 12px;padding:9px 12px;border:1px solid #bddcc9;border-radius:5px;background:#eef8f1;color:#286440}.notice.bad{border-color:#ebc5c1;background:#fff3f1;color:#9e3831}.transfer{display:grid;grid-template-columns:minmax(0,1fr) 90px minmax(0,1fr);gap:10px;align-items:center}.grid-card{min-width:0}.card-title{display:flex;align-items:center;justify-content:space-between;padding:11px 13px}.card-title h2{margin:0;font-size:13px}.card-title p{margin:2px 0 0;color:var(--muted);font-size:10px}.table-scroll{overflow:auto;border-top:1px solid #e6ebf0;max-height:350px}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:10px}th{position:sticky;top:0;background:#f1f5f8;color:#607388;font-size:9px;font-weight:700;text-align:left}th,td{padding:7px 8px;border-bottom:1px solid #ebeff3}tbody tr{cursor:pointer}tbody tr:hover{background:#f8fafc}tbody tr.checked{background:#fff5e9}.num{text-align:right;font-variant-numeric:tabular-nums}.lot{color:#8b4b37;font-weight:650}.empty{text-align:center;color:#8a98a7;padding:17px}.move-controls{display:flex;flex-direction:column;align-items:center;gap:8px}.move{min-width:74px;padding:7px 8px;border:1px solid #cdd8e2;border-radius:4px;background:white;color:#334d66;cursor:pointer}.move-controls .link{padding:5px;font-size:9px}.history .card-title{padding-bottom:10px}.history .primary{margin-left:auto}.history .card-title{gap:12px}footer{padding:2px 2px 8px;color:#8391a0;font-size:10px}.unavailable{padding:16px;border:1px solid #e8d2ac;border-radius:6px;background:#fff8ea;color:#7a5622}@media(max-width:1250px){.t100-stockin{padding:15px}.form-grid{grid-template-columns:repeat(3,minmax(150px,1fr))}.transfer{grid-template-columns:1fr}.move-controls{flex-direction:row;justify-content:center}.summary{flex-wrap:wrap}.summary label{margin-left:0}.flow{align-items:flex-start;flex-wrap:wrap}.flow span:last-child{width:100%;margin:0}.scan-row{flex-wrap:wrap}.scan-row span{width:100%;margin:0}}
</style>
