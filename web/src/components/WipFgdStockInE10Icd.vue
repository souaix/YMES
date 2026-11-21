<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

type Outcome = 'success' | 'warning' | 'erp-fail' | 'mes-fail'
type LotRow = {
  id: string; moNo: string; lotNo: string; productNo: string; productVersion: string; qty: number
  inQty: number; unitNo: string; inventoryNo: string; baseLotNo: string; inputDate: string
  boxNo: string; boxNoFlag: boolean; dateCode: string; dateCodeDefault: string; dateCodeFlag: boolean
  bookingFlag: number; selected: boolean; bins: Record<string, number>
}
type Component = { lotNo: string; componentNo: string; goodQty: number; scrapQty: number; unitNo: string }
type HistoryRow = {
  time: string; erpNo: string; moNo: string; lots: number; qty: number
  erp: string; mes: string; rollback: string
}

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const form = reactive({ moNo: dev.value ? 'DEV-MO-621-01' : '', inventoryNo: dev.value ? 'DEV-FGD-01' : '', locatorNo: dev.value ? 'A-01' : '', docType: dev.value ? 'FGD-01' : '' })
const docTypes = dev.value ? [{ value: 'FGD-01', label: 'FGD-01｜成品入庫' }, { value: 'FGD-02', label: 'FGD-02｜成品完工入庫' }] : []
const inventories = dev.value ? ['DEV-FGD-01', 'DEV-FGD-02'] : []
const locators = dev.value ? ['A-01', 'A-02', 'B-01', 'B-02'] : []
const outcome = ref<Outcome>('success')
const lots = ref<LotRow[]>(dev.value ? [
  { id: 'r1', moNo: 'DEV-MO-621-01', lotNo: 'DEV-LOT-621-001', productNo: 'DEV-PRD-621', productVersion: 'A01', qty: 480, inQty: 480, unitNo: 'PCS', inventoryNo: 'DEV-WIP-01', baseLotNo: 'DEV-BASE-621-01', inputDate: '2026-11-21 08:10', boxNo: 'DEV-BOX-01', boxNoFlag: true, dateCode: '2611', dateCodeDefault: '', dateCodeFlag: true, bookingFlag: 0, selected: false, bins: { BIN01: 450, BIN02: 30 } },
  { id: 'r2', moNo: 'DEV-MO-621-01', lotNo: 'DEV-LOT-621-002', productNo: 'DEV-PRD-621', productVersion: 'A01', qty: 360, inQty: 360, unitNo: 'PCS', inventoryNo: 'DEV-WIP-01', baseLotNo: 'DEV-BASE-621-01', inputDate: '2026-11-21 09:25', boxNo: 'DEV-BOX-02', boxNoFlag: true, dateCode: '', dateCodeDefault: '2611', dateCodeFlag: true, bookingFlag: 0, selected: false, bins: { BIN01: 360, BIN02: 0 } },
  { id: 'r3', moNo: 'DEV-MO-621-01', lotNo: 'DEV-LOT-621-003', productNo: 'DEV-PRD-621', productVersion: 'A01', qty: 1250, inQty: 1250, unitNo: 'PCS', inventoryNo: 'DEV-WIP-02', baseLotNo: 'DEV-BASE-621-02', inputDate: '2026-11-21 10:40', boxNo: '', boxNoFlag: false, dateCode: '', dateCodeDefault: '', dateCodeFlag: false, bookingFlag: 0, selected: false, bins: { BIN01: 1100, BIN02: 150 } },
  { id: 'r4', moNo: 'DEV-MO-621-02', lotNo: 'DEV-LOT-621-004', productNo: 'DEV-PRD-622', productVersion: 'B02', qty: 720, inQty: 720, unitNo: 'PCS', inventoryNo: 'DEV-WIP-03', baseLotNo: 'DEV-BASE-622-01', inputDate: '2026-11-20 16:05', boxNo: '', boxNoFlag: false, dateCode: '', dateCodeDefault: '', dateCodeFlag: false, bookingFlag: 0, selected: false, bins: { BIN01: 720, BIN02: 0 } },
] : [])
const componentRows: Component[] = dev.value ? [
  { lotNo: 'DEV-LOT-621-001', componentNo: 'DEV-COMP-621-01', goodQty: 470, scrapQty: 10, unitNo: 'PCS' },
  { lotNo: 'DEV-LOT-621-001', componentNo: 'DEV-COMP-621-02', goodQty: 466, scrapQty: 14, unitNo: 'PCS' },
  { lotNo: 'DEV-LOT-621-003', componentNo: 'DEV-COMP-621-03', goodQty: 1250, scrapQty: 0, unitNo: 'PCS' },
] : []
const visibleLots = computed(() => lots.value.filter(row => row.moNo === form.moNo && row.qty > 0 && row.bookingFlag === 0))
const selectedLots = computed(() => visibleLots.value.filter(row => row.selected))
const currentLot = ref('')
const currentComponents = computed(() => componentRows.filter(item => item.lotNo === currentLot.value))
const currentBinRow = computed(() => lots.value.find(item => item.lotNo === currentLot.value))
const history = ref<HistoryRow[]>(dev.value ? [
  { time: '2026-11-21 07:52:10', erpNo: 'DEV-ERP-FGD-00318', moNo: 'DEV-MO-621-02', lots: 1, qty: 720, erp: '成功', mes: '成功', rollback: '—' },
] : [])
const message = ref('')
const error = ref('')
const loading = ref(false)
const nextNumber = ref(319)

const selectedQuantity = computed(() => selectedLots.value.reduce((sum, row) => sum + (Number(row.inQty) || 0), 0))
const formatQty = (value: number) => new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 6 }).format(value)
const nowText = () => new Date().toISOString().slice(0, 19).replace('T', ' ')

function requireDev(): boolean {
  if (dev.value) return true
  error.value = '此功能目前僅開放 Vue DEV 假資料預覽，尚未接正式 ERP／MES 服務。'
  return false
}
function clearMessage(): void { error.value = ''; message.value = '' }
function query(): void {
  clearMessage()
  if (!requireDev()) return
  if (!form.moNo.trim()) { error.value = '請選擇工單。'; return }
  currentLot.value = ''
  message.value = '已載入 ' + visibleLots.value.length + ' 筆可入庫 WIP Lot（DEV 假資料）。'
}
function selectAll(checked: boolean): void { visibleLots.value.forEach(row => { row.selected = checked }) }
function selectRow(row: LotRow): void { currentLot.value = row.lotNo; clearMessage() }

function validate(): string {
  if (!form.docType) return '請選擇 ERP 入庫單別。'
  if (!form.moNo.trim()) return '請選擇工單。'
  if (!form.inventoryNo) return '請選擇成品庫。'
  if (!selectedLots.value.length) return '請至少勾選一筆生產批。'
  for (const row of selectedLots.value) {
    const amount = Number(row.inQty)
    if (!Number.isFinite(amount) || amount <= 0 || amount > row.qty) return `${row.lotNo} 的 InInvNum 必須大於 0 且不可超過可入庫量 ${formatQty(row.qty)}。`
    if (row.boxNoFlag && !row.boxNo.trim()) return `${row.lotNo} 必須輸入 BoxNo。`
    if (row.dateCodeFlag && !(row.dateCode.trim() || row.dateCodeDefault.trim())) return `${row.lotNo} 必須輸入 DateCode 或使用預設值。`
  }
  for (const lotNo of new Set(selectedLots.value.map(row => row.lotNo))) {
    const sameLot = selectedLots.value.filter(row => row.lotNo === lotNo)
    const effectiveDateCodes = sameLot.map(row => row.dateCode.trim() || row.dateCodeDefault.trim())
    if (new Set(effectiveDateCodes).size > 1) return `${lotNo} 的多筆入庫列必須使用相同 DateCode。`
    if (new Set(sameLot.map(row => row.boxNo.trim())).size > 1) return `${lotNo} 的多筆入庫列必須使用相同 BoxNo。`
  }
  return ''
}

function confirmStockIn(): void {
  clearMessage()
  if (!requireDev() || loading.value) return
  const validationError = validate()
  if (validationError) { error.value = validationError; return }
  loading.value = true
  const selected = [...selectedLots.value]
  const amount = selected.reduce((sum, row) => sum + Number(row.inQty), 0)
  const stamp = nowText()
  selected.forEach(row => { row.bookingFlag = 1 })
  const erpNo = 'DEV-ERP-FGD-' + String(nextNumber.value++).padStart(5, '0')

  if (outcome.value === 'erp-fail') {
    selected.forEach(row => { row.bookingFlag = 0 })
    history.value.unshift({ time: stamp, erpNo: '—', moNo: form.moNo, lots: selected.length, qty: amount, erp: '失敗', mes: '未執行', rollback: 'BookingFlag 已重設' })
    error.value = 'DEV 模擬 ERP 建單失敗；MES 未執行，已釋放批次預約。'
  } else if (outcome.value === 'mes-fail') {
    selected.forEach(row => { row.bookingFlag = 0 })
    history.value.unshift({ time: stamp, erpNo, moNo: form.moNo, lots: selected.length, qty: amount, erp: '成功', mes: '失敗', rollback: '已模擬 ERP 取消單；BookingFlag 已重設' })
    error.value = `DEV 模擬 MES 入庫失敗；已模擬呼叫 ERP_Stock_IN_Cancel（${erpNo}），並釋放批次預約。`
  } else {
    selected.forEach(row => {
      row.qty = Math.max(0, row.qty - Number(row.inQty))
      row.inQty = row.qty
      row.selected = false
      row.bookingFlag = 0
    })
    history.value.unshift({ time: stamp, erpNo, moNo: form.moNo, lots: selected.length, qty: amount, erp: outcome.value === 'warning' ? '警告（視為成功）' : '成功', mes: '成功', rollback: '—' })
    currentLot.value = ''
    message.value = `DEV 模擬完成 ERP 建單與 MES 成品入庫：${erpNo}，${selected.length} 批／${formatQty(amount)} PCS。`
    if (outcome.value === 'warning') message.value += ' ERP 回傳 W 警告；來源仍視為可繼續 MES 入庫。'
  }
  loading.value = false
}
</script>

<template>
  <section class="e10-stockin">
    <header class="e10-head">
      <div><p class="eyebrow">WIP · ERP INTEGRATION · WP621</p><h1>入庫至 E10ICD ERP</h1><p class="subtitle">FGD Stock In to E10ICD</p></div>
      <div class="head-actions"><span v-if="dev" class="dev-pill">DEV 假資料</span><button class="quiet-button" type="button" @click="emit('close')">關閉</button></div>
    </header>
    <div v-if="!dev" class="unavailable">正式 E10ICD 與 MES 入庫服務尚未接入；此頁不會連線或送出資料。</div>
    <div v-else class="flow-banner"><b>ERP 建立入庫單</b><span>→</span><b>MES 扣除 WIP、建立成品庫存</b><span>· ERP 回傳 W 警告仍會繼續 MES；MES 失敗時模擬取消 ERP 單</span></div>

    <div class="e10-toolbar">
      <label>工單 No.<select v-model="form.moNo" :disabled="!dev" @change="query"><option v-if="!dev" value="">正式來源未接入</option><template v-else><option value="DEV-MO-621-01">DEV-MO-621-01 · DEV-PRD-621</option><option value="DEV-MO-621-02">DEV-MO-621-02 · DEV-PRD-622</option></template></select></label>
      <label>ERP 單別<select v-model="form.docType" :disabled="!dev"><option v-for="item in docTypes" :key="item.value" :value="item.value">{{ item.label }}</option></select></label>
      <label>成品庫<select v-model="form.inventoryNo" :disabled="!dev"><option v-for="item in inventories" :key="item" :value="item">{{ item }}</option></select></label>
      <label>儲位（可空白）<select v-model="form.locatorNo" :disabled="!dev"><option value="">N/A</option><option v-for="item in locators" :key="item" :value="item">{{ item }}</option></select></label>
      <button class="primary-button" type="button" :disabled="!dev" @click="query">查詢工單批次</button>
    </div>

    <div class="e10-summary">
      <div><small>可入庫批次</small><strong>{{ visibleLots.length }}</strong></div><div><small>已選批次</small><strong>{{ selectedLots.length }}</strong></div><div><small>本次入庫量</small><strong>{{ formatQty(selectedQuantity) }}</strong><small>PCS</small></div><div><small>自動確認／過帳</small><strong>N</strong></div>
      <label class="scenario">DEV 結果情境<select v-model="outcome" :disabled="!dev"><option value="success">ERP、MES 成功</option><option value="warning">ERP W 警告後成功</option><option value="erp-fail">ERP 建單失敗</option><option value="mes-fail">MES 失敗並取消 ERP 單</option></select></label>
    </div>

    <div v-if="error || message" class="notice" :class="{ error: !!error }" role="status">{{ error || message }}</div>

    <section class="grid-card">
      <div class="section-title"><div><h2>待入庫 WIP 批次</h2><p>來源欄位依 MES `LoadWIPInventory_SEMIbyMO_TP`；`InInvNum` 可調整且不得超過 Qty。</p></div><div class="grid-actions"><button type="button" class="text-button" :disabled="!dev" @click="selectAll(true)">全選</button><button type="button" class="text-button" :disabled="!dev" @click="selectAll(false)">全取消</button></div></div>
      <div class="table-wrap"><table><thead><tr><th><input type="checkbox" :checked="!!visibleLots.length && selectedLots.length === visibleLots.length" aria-label="全選批次" :disabled="!dev" @change="selectAll(($event.target as HTMLInputElement).checked)"></th><th>MONo</th><th>LotNo</th><th>ProductNo</th><th class="num">Qty</th><th class="num">InInvNum</th><th>UnitNo</th><th>InventoryNo</th><th>BoxNo</th><th>DateCode</th></tr></thead><tbody>
        <tr v-for="row in visibleLots" :key="row.id" :class="{ active: currentLot === row.lotNo, chosen: row.selected }" @click="selectRow(row)"><td @click.stop><input v-model="row.selected" type="checkbox" :disabled="!dev" :aria-label="'選取 ' + row.lotNo"></td><td>{{ row.moNo }}</td><td class="lot-cell">{{ row.lotNo }}</td><td>{{ row.productNo }}</td><td class="num">{{ formatQty(row.qty) }}</td><td class="num" @click.stop><input v-model.number="row.inQty" class="qty-input" type="number" min="0" :max="row.qty" step="any" :disabled="!dev"></td><td>{{ row.unitNo }}</td><td>{{ row.inventoryNo }}</td><td @click.stop><input v-model="row.boxNo" class="meta-input" type="text" :placeholder="row.boxNoFlag ? '必填' : '—'" :disabled="!dev || !row.boxNoFlag"></td><td @click.stop><input v-model="row.dateCode" class="meta-input" type="text" :placeholder="row.dateCodeDefault || (row.dateCodeFlag ? '必填' : '—')" :disabled="!dev || !row.dateCodeFlag"></td></tr>
        <tr v-if="!visibleLots.length"><td colspan="10" class="empty">{{ form.moNo ? '此工單沒有可入庫批次' : '請選擇工單' }}</td></tr>
      </tbody></table></div>
      <div class="table-foot">選取 {{ selectedLots.length }} 筆 · 本次數量 {{ formatQty(selectedQuantity) }} · 儲位 {{ form.locatorNo || 'N/A' }}</div>
    </section>

    <div class="detail-row">
      <section class="detail-card"><div class="section-title"><div><h2>Bin 明細</h2><p>{{ currentLot || '選取上方 Lot 查看' }}</p></div></div><div v-if="currentBinRow" class="bin-list"><div v-for="(qty, bin) in currentBinRow.bins" :key="bin"><span>{{ bin }}</span><b>{{ formatQty(qty) }}</b></div></div><p v-else class="empty detail-empty">尚未選取 Lot</p></section>
      <section class="detail-card"><div class="section-title"><div><h2>Component 明細</h2><p>{{ currentLot || '選取上方 Lot 查看' }}</p></div></div><div v-if="currentComponents.length" class="component-list"><div class="component-head"><span>ComponentNo</span><span>GoodQty</span><span>ScrapQty</span><span>UnitNo</span></div><div v-for="item in currentComponents" :key="item.componentNo" class="component-line"><span>{{ item.componentNo }}</span><span>{{ formatQty(item.goodQty) }}</span><span>{{ formatQty(item.scrapQty) }}</span><span>{{ item.unitNo }}</span></div></div><p v-else class="empty detail-empty">{{ currentLot ? '此 Lot 沒有 Component 明細' : '尚未選取 Lot' }}</p></section>
    </div>

    <section class="history-card"><div class="section-title"><div><h2>本頁操作紀錄</h2><p>DEV 本機記憶體模擬，不是正式 ERP／MES 交易紀錄</p></div><button class="primary-button" type="button" :disabled="!dev || loading" @click="confirmStockIn">{{ loading ? '處理中…' : '確認入庫' }}</button></div>
      <div class="table-wrap"><table><thead><tr><th>時間</th><th>ERP 入庫單號</th><th>工單</th><th class="num">Lot／數量</th><th>ERP 建單</th><th>MES 入庫</th><th>ERP 取消／預約回復</th></tr></thead><tbody><tr v-for="(item, index) in history" :key="item.time + item.erpNo + index"><td>{{ item.time }}</td><td class="lot-cell">{{ item.erpNo }}</td><td>{{ item.moNo }}</td><td class="num">{{ item.lots }} Lot／{{ formatQty(item.qty) }} PCS</td><td>{{ item.erp }}</td><td>{{ item.mes }}</td><td>{{ item.rollback }}</td></tr><tr v-if="!history.length"><td colspan="7" class="empty">尚無本頁操作</td></tr></tbody></table></div>
    </section>
    <footer class="source-foot">來源按鈕：Confirm／Close · `auto=N` · ERP `ERP_Stock_IN`（RequestType=0）→ MES `MOStockIn_E10`；本頁所有編號均為合成 DEV 假資料。</footer>
  </section>
</template>

<style scoped>
.e10-stockin{--ink:#18283d;--muted:#6c7b8f;--line:#dce4ec;--navy:#173b5d;--blue:#236f9e;--pale:#f4f7fa;color:var(--ink);height:100%;min-height:0;overflow:auto;padding:22px 26px 30px;background:#f7f9fb;font:13px/1.45 "Segoe UI", "Microsoft JhengHei", sans-serif}
.e10-head{display:flex;align-items:center;justify-content:space-between;margin-bottom:16px}.eyebrow{margin:0;color:#73869a;font-size:10px;font-weight:750;letter-spacing:.13em}.e10-head h1{margin:2px 0 0;font-size:23px;letter-spacing:.01em}.subtitle{margin:0;color:var(--muted);font-size:12px}.head-actions{display:flex;align-items:center;gap:12px}.dev-pill{padding:5px 10px;border-radius:999px;background:#e8f4ed;color:#22724a;font-weight:700}.quiet-button,.text-button{border:1px solid var(--line);background:white;color:#334d66;border-radius:5px;padding:7px 12px;cursor:pointer}.flow-banner{display:flex;align-items:center;gap:12px;padding:11px 15px;margin-bottom:14px;border:1px solid #cadce9;border-radius:6px;background:#edf5fa;color:#315a79}.flow-banner b{color:#214a69}.flow-banner span:last-child{margin-left:auto;color:#657d91;font-size:11px}.e10-toolbar{display:flex;align-items:end;gap:12px;padding:14px;background:white;border:1px solid var(--line);border-radius:7px;box-shadow:0 2px 5px #10283a0a}.e10-toolbar label,.scenario{display:flex;flex-direction:column;gap:5px;color:#5e7185;font-size:11px;font-weight:650}.e10-toolbar select,.scenario select{min-width:158px;height:34px;border:1px solid #cdd8e2;border-radius:4px;padding:0 9px;background:white;color:var(--ink);font:inherit}.e10-toolbar label:first-child select{min-width:245px}.primary-button{height:34px;padding:0 15px;border:1px solid var(--navy);border-radius:4px;background:var(--navy);color:white;font:inherit;font-weight:700;cursor:pointer}.primary-button:disabled,.quiet-button:disabled,.text-button:disabled{opacity:.45;cursor:not-allowed}.e10-summary{display:flex;align-items:center;gap:0;margin:13px 0;background:white;border:1px solid var(--line);border-radius:7px;overflow:hidden}.e10-summary>div{min-width:112px;padding:9px 15px;border-right:1px solid var(--line);display:grid;grid-template-columns:1fr auto;align-items:center;column-gap:8px}.e10-summary small{color:var(--muted);font-size:10px}.e10-summary strong{grid-row:span 2;color:#244f71;font-size:19px}.e10-summary .scenario{margin-left:auto;padding:8px 13px;flex-direction:row;align-items:center}.scenario select{min-width:175px;height:30px}.notice{margin:0 0 12px;padding:9px 12px;border:1px solid #bddcc9;border-radius:5px;background:#eef8f1;color:#286440}.notice.error{border-color:#ebc5c1;background:#fff3f1;color:#9e3831}.grid-card,.detail-card,.history-card{margin-bottom:13px;border:1px solid var(--line);border-radius:7px;background:#fff;box-shadow:0 2px 5px #10283a08}.section-title{display:flex;align-items:center;justify-content:space-between;padding:12px 15px}.section-title h2{margin:0;font-size:14px}.section-title p{margin:2px 0 0;color:var(--muted);font-size:10px}.grid-actions{display:flex;gap:6px}.text-button{padding:5px 9px;font-size:11px}.table-wrap{overflow:auto;border-top:1px solid #e6ebf0}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:11px}th{position:sticky;top:0;background:#f1f5f8;color:#607388;font-size:10px;font-weight:700;text-align:left}th,td{padding:8px 9px;border-bottom:1px solid #ebeff3}tbody tr{cursor:pointer}tbody tr:hover{background:#f7fafc}tbody tr.active{background:#eef6fb}tbody tr.chosen{box-shadow:inset 3px 0 #367ca6}.num{text-align:right;font-variant-numeric:tabular-nums}.lot-cell{color:#225e88;font-weight:650}.qty-input{width:76px;padding:4px 5px;border:1px solid #ccd8e2;border-radius:3px;text-align:right;font:inherit}.meta-input{width:78px;padding:4px 5px;border:1px solid #d5dfe7;border-radius:3px;font:inherit}.empty{text-align:center;color:#8a98a7;padding:18px}.table-foot{padding:7px 12px;color:#728397;font-size:10px;background:#fafcfd}.detail-row{display:grid;grid-template-columns:1fr 1.6fr;gap:13px}.detail-card{min-height:130px}.bin-list{display:flex;gap:8px;padding:0 14px 14px}.bin-list div{display:flex;flex-direction:column;min-width:90px;padding:8px 10px;border:1px solid #e2e9ef;border-radius:5px;background:#f8fafb}.bin-list span{color:var(--muted);font-size:10px}.bin-list b{margin-top:4px}.component-list{padding:0 14px 12px}.component-list>div{display:grid;grid-template-columns:1.6fr 1fr 1fr .7fr;padding:6px 0;border-bottom:1px solid #eef1f4;font-size:10px}.component-head{color:var(--muted);font-weight:700}.component-line span:nth-child(n+2){text-align:right}.detail-empty{margin:0}.history-card .section-title{padding-bottom:10px}.source-foot{padding:2px 2px 8px;color:#8391a0;font-size:10px}.unavailable{padding:16px;border:1px solid #e8d2ac;border-radius:6px;background:#fff8ea;color:#7a5622}@media(max-width:1000px){.e10-stockin{padding:15px}.e10-toolbar{flex-wrap:wrap}.e10-summary{flex-wrap:wrap}.e10-summary .scenario{margin-left:0}.detail-row{grid-template-columns:1fr}.flow-banner{align-items:flex-start;flex-wrap:wrap}.flow-banner span:last-child{width:100%;margin:0}}
</style>
