<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

type ResultMode = 'success' | 'erp-fail' | 'mes-fail-undo-ok' | 'mes-fail-undo-fail'
type Lot = {
  id: string
  lotNo: string
  orgLotNo: string
  moNo: string
  productNo: string
  productVersion: string
  erpNo: string
  unitNo: string
  sourceInventoryNo: string
  qty: number
  inInvNum: number
  inputDate: string
  packingLevel: number | null
  sysQty: number | null
  bookingFlag: number
  componentCount: number
  goodBinCount: number
  failQty: number
  resEmp: number
  stdEmp: number
  resEqp: number
  stdEqp: number
}
type HistoryRow = { time: string; mesNo: string; erpNo: string; lots: number; qty: number; erp: string; mes: string; undo: string; note: string }

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const form = reactive({
  sheetType: 'DEV-SFU · TipTop FGD Stock In',
  mesNo: 'DEV-FGD-609-0001',
  moNos: 'DEV-MO-609-01',
  referenceLotNo: '',
  destinationInventory: 'DEV-FGD-01',
  locatorNo: '',
  sourceInventory: 'DEV-WIP-01',
})
const sourceInventories = ['DEV-WIP-01', 'DEV-WIP-02']
const destinationInventories = ['DEV-FGD-01', 'DEV-FGD-02']
const locators = ['DEV-LOC-A01', 'DEV-LOC-A02', 'DEV-LOC-B01']
const candidates = ref<Lot[]>(dev.value ? [
  { id: 'tp-609-1', lotNo: 'DEV-LOT-609-001', orgLotNo: 'DEV-ORG-LOT-609-001', moNo: 'DEV-MO-609-01', productNo: 'DEV-PRD-609', productVersion: 'A1', erpNo: 'ERP-PRD-609', unitNo: 'PCS', sourceInventoryNo: 'DEV-WIP-01', qty: 120, inInvNum: 48, inputDate: '2026-11-20 14:05', packingLevel: null, sysQty: null, bookingFlag: 0, componentCount: 0, goodBinCount: 0, failQty: 0, resEmp: 2.5, stdEmp: 2, resEqp: 1.2, stdEqp: 1 },
  { id: 'tp-609-2', lotNo: 'DEV-LOT-609-002', orgLotNo: 'DEV-ORG-LOT-609-002', moNo: 'DEV-MO-609-01', productNo: 'DEV-PRD-609', productVersion: 'A1', erpNo: 'ERP-PRD-609', unitNo: 'PCS', sourceInventoryNo: 'DEV-WIP-01', qty: 80, inInvNum: 80, inputDate: '2026-11-20 15:20', packingLevel: null, sysQty: null, bookingFlag: 0, componentCount: 2, goodBinCount: 0, failQty: 1, resEmp: 1.4, stdEmp: 1.5, resEqp: 0.8, stdEqp: 1 },
  { id: 'tp-609-3', lotNo: 'DEV-LOT-609-003', orgLotNo: 'DEV-ORG-LOT-609-003', moNo: 'DEV-MO-609-02', productNo: 'DEV-PRD-610', productVersion: 'B2', erpNo: '', unitNo: 'PCS', sourceInventoryNo: 'DEV-WIP-02', qty: 60, inInvNum: 60, inputDate: '2026-11-21 08:40', packingLevel: 2, sysQty: 56, bookingFlag: 0, componentCount: 0, goodBinCount: 3, failQty: 0, resEmp: 0, stdEmp: 0, resEqp: 0, stdEqp: 0 },
  { id: 'tp-609-4', lotNo: 'DEV-LOT-609-004', orgLotNo: 'DEV-ORG-LOT-609-004', moNo: 'DEV-MO-609-02', productNo: 'DEV-PRD-610', productVersion: 'B2', erpNo: 'ERP-PRD-610', unitNo: 'PCS', sourceInventoryNo: 'DEV-WIP-02', qty: 35, inInvNum: 35, inputDate: '2026-11-21 09:12', packingLevel: null, sysQty: null, bookingFlag: 1, componentCount: 0, goodBinCount: 0, failQty: 0, resEmp: 0.5, stdEmp: 0.5, resEqp: 0.2, stdEqp: 0.25 },
] : [])
const queried = ref(false)
const availableMarked = ref<string[]>([])
const selectedIds = ref<string[]>([])
const selectedMarked = ref<string[]>([])
const scan = reactive({ action: 'select' as 'select' | 'unselect', lotNo: '' })
const resultMode = ref<ResultMode>('success')
const orgLotNoToErp = ref(false)
const processCost = ref('0')
const error = ref('')
const info = ref('')
const working = ref(false)
const history = ref<HistoryRow[]>([])
const resultNo = ref(1)
const matchesMO = (lot: Lot) => form.moNos.split(/[;,，、\s]+/).filter(Boolean).includes(lot.moNo)
const availableRows = computed(() => queried.value ? candidates.value.filter(row => row.qty > 0 && row.bookingFlag === 0 && matchesMO(row) && !selectedIds.value.includes(row.id)) : [])
const selectedRows = computed(() => selectedIds.value.map(id => candidates.value.find(row => row.id === id)).filter((row): row is Lot => !!row))
const selectedQty = computed(() => selectedRows.value.reduce((sum, row) => sum + Number(row.inInvNum || 0), 0))
const allInQty = (row: Lot) => Number(row.inInvNum) === Number(row.qty)
const erpQty = (row: Lot) => row.packingLevel !== null ? Number(row.sysQty ?? 0) : Number(row.inInvNum || 0)
const erpLot = (row: Lot) => orgLotNoToErp.value ? row.orgLotNo : row.lotNo
const fmt = (qty: number) => new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 6 }).format(qty)
const nowText = () => new Date().toLocaleString('sv-SE', { hour12: false })

function queryLots(): void {
  error.value = ''
  info.value = ''
  if (!dev.value) { error.value = '正式 TipTop／MES API 尚未接入；此頁不會讀取或送出資料。'; return }
  if (!form.moNos.trim()) { error.value = '請先輸入或選取至少一個 MO。'; return }
  queried.value = true
  availableMarked.value = []
  info.value = `${availableRows.value.length} 筆 DEV WIP 半成品庫存可選；只列所選 MO 中 Qty>0、BookingFlag=0 的 Lot。來源庫欄供掃描定位使用。`
}

function loadReferenceLot(): void {
  const lot = candidates.value.find(row => row.lotNo.toLowerCase() === form.referenceLotNo.trim().toLowerCase() || row.orgLotNo.toLowerCase() === form.referenceLotNo.trim().toLowerCase())
  if (!lot) { error.value = 'DEV 找不到此參考 Lot。'; return }
  form.moNos = lot.moNo
  form.sourceInventory = lot.sourceInventoryNo
  form.referenceLotNo = ''
  queryLots()
  info.value = `參考 Lot ${lot.lotNo} 已帶出 MO ${lot.moNo}，並載入來源庫 ${lot.sourceInventoryNo}。`
}

function toggleMarked(id: string, selected: string[]): string[] {
  return selected.includes(id) ? selected.filter(item => item !== id) : [...selected, id]
}

function addRows(rows: Lot[]): void {
  const ids = new Set(selectedIds.value)
  rows.forEach(row => ids.add(row.id))
  selectedIds.value = [...ids]
  availableMarked.value = []
  info.value = `已選取 ${selectedRows.value.length} 筆 Lot；可在右側調整各列 InInvNum。`
  error.value = ''
}

function removeRows(rows: Lot[]): void {
  const remove = new Set(rows.map(row => row.id))
  selectedIds.value = selectedIds.value.filter(id => !remove.has(id))
  selectedMarked.value = []
}

function selectAll(): void { addRows([...availableRows.value]) }
function clearSelected(): void { selectedIds.value = []; selectedMarked.value = [] }

function scanLot(): void {
  error.value = ''
  const value = scan.lotNo.trim().toLowerCase()
  if (!value) return
  const lot = candidates.value.find(row => row.lotNo.toLowerCase() === value || row.orgLotNo.toLowerCase() === value)
  if (!lot) { error.value = `DEV 找不到 Lot ${scan.lotNo}。`; return }
  if (scan.action === 'select') {
    if (lot.sourceInventoryNo !== form.sourceInventory || !matchesMO(lot) || lot.bookingFlag !== 0) { error.value = 'Lot 不符合目前來源庫／MO／BookingFlag 查詢條件。'; return }
    addRows([lot])
  } else {
    removeRows([lot])
    info.value = `已取消選取 ${lot.lotNo}。`
  }
  scan.lotNo = ''
}

function confirmStockIn(): void {
  error.value = ''
  info.value = ''
  if (!dev.value || working.value) return
  if (!form.sheetType || !form.moNos.trim() || !form.destinationInventory) { error.value = 'Sheet Type、MO 與目的庫皆為必填。'; return }
  if (!selectedRows.value.length) { error.value = '請至少選取一筆 Lot。'; return }
  for (const row of selectedRows.value) {
    const qty = Number(row.inInvNum)
    if (!Number.isFinite(qty) || qty <= 0 || qty > row.qty) { error.value = `${row.lotNo} 的 InInvNum 必須大於 0 且不得超過可入庫數量 ${fmt(row.qty)}。`; return }
    if (qty !== row.qty && (row.componentCount > 0 || row.goodBinCount > 0)) {
      error.value = `${row.lotNo} 有 Component 或 Good Bin 資料，舊 Form 不允許部分入庫。`
      return
    }
  }

  working.value = true
  const timestamp = nowText()
  const rows = [...selectedRows.value]
  const lotQty = rows.reduce((sum, row) => sum + Number(row.inInvNum), 0)
  const reserved = rows.filter(allInQty)
  reserved.forEach(row => { row.bookingFlag = 1 })

  if (resultMode.value === 'erp-fail') {
    reserved.forEach(row => { row.bookingFlag = 0 })
    history.value.unshift({ time: timestamp, mesNo: form.mesNo, erpNo: '—', lots: rows.length, qty: lotQty, erp: '非 0 code', mes: '未執行', undo: '不需要', note: 'ERP 建單失敗；finally 釋放整批 Lot Reservation' })
    error.value = 'TipTop CreateStockInData 回傳非 0 code；未呼叫 MES，整批 Lot Reservation 已模擬釋放。'
    working.value = false
    return
  }

  const erpNo = `DEV-TP-SFU-${String(resultNo.value++).padStart(5, '0')}`
  form.mesNo = erpNo
  if (resultMode.value === 'mes-fail-undo-ok' || resultMode.value === 'mes-fail-undo-fail') {
    reserved.forEach(row => { row.bookingFlag = 0 })
    const undoFailed = resultMode.value === 'mes-fail-undo-fail'
    history.value.unshift({ time: timestamp, mesNo: erpNo, erpNo, lots: rows.length, qty: lotQty, erp: '成功', mes: '失敗', undo: undoFailed ? '失敗／未知' : '成功', note: undoFailed ? 'ERP 可能仍有單；舊 Form finally 仍釋放 BookingFlag，需人工對帳' : 'ERP UndoCreateStockInData 成功；舊 Form finally 釋放 BookingFlag' })
    error.value = undoFailed
      ? 'MES 入庫失敗且 TipTop UndoCreateStockInData 未成功；來源碼仍釋放 BookingFlag，DEV 已留下 ERP/MES 對帳提醒。'
      : 'MES 入庫失敗；已模擬 TipTop UndoCreateStockInData 成功，MES 未扣庫存。'
    selectedIds.value = []
    working.value = false
    return
  }

  rows.forEach(row => {
    row.qty = Math.max(0, row.qty - Number(row.inInvNum))
    row.inInvNum = row.qty
    if (row.qty === 0) row.bookingFlag = 1
  })
  history.value.unshift({ time: timestamp, mesNo: erpNo, erpNo, lots: rows.length, qty: lotQty, erp: 'code=0 成功', mes: '成功', undo: '—', note: 'MES Core 已模擬建立入庫資料、扣 WIP、更新 FGD／Lot／Component／歷程' })
  info.value = `DEV 入庫完成：TipTop ${erpNo}，${rows.length} 筆 Lot，MES 入庫數量 ${fmt(lotQty)} ${rows[0]?.unitNo ?? ''}。${rows.some(row => row.packingLevel !== null) ? ' 包裝批 ERP 回傳數量依來源規則使用 SysQty。' : ''}${rows.some(row => row.goodBinCount > 0) ? ' 原始 MOStockIn_TP 未找到 Good Bin 更新語句，Bin 回存行為仍待來源／家用端確認。' : ''}`
  selectedIds.value = []
  selectedMarked.value = []
  working.value = false
}
</script>

<template>
  <section class="tp-stockin">
    <header class="head">
      <div><p class="eyebrow">WIP · ERP INTEGRATION · WP609</p><h1>入庫至 TipTop ERP</h1><p class="subtitle">FGD Stock In to TipTop</p></div>
      <div class="head-actions"><span v-if="dev" class="dev-pill">DEV 假資料</span><button class="quiet" type="button" @click="emit('close')">關閉</button></div>
    </header>
    <div v-if="!dev" class="unavailable">正式 TipTop／MES 入庫服務尚未接入；此頁不會讀取或送出資料。</div>
    <div v-else class="flow"><b>選擇工單與線邊 Lot</b><span>→</span><b>TipTop 建立 SFU</b><span>→</span><b>MES FGD 入庫</b><span>失敗時嘗試 UndoCreateStockInData</span></div>

    <section class="card form-card">
      <div class="form-grid">
        <label>TipTop Sheet Type<select v-model="form.sheetType" :disabled="!dev"><option>DEV-SFU · TipTop FGD Stock In</option><option>DEV-SFU-ALT · DEV 單別樣本</option></select></label>
        <label>MES／ERP Stock In No.<input v-model="form.mesNo" readonly></label>
        <label>MO No.（可輸入多筆，以逗號分隔）<div class="inline"><input v-model="form.moNos" :disabled="!dev" placeholder="輸入 DEV-MO-..."><button class="quiet" type="button" :disabled="!dev" @click="queryLots">查 MO</button></div></label>
        <label>Ref. Lot No.<div class="inline"><input v-model="form.referenceLotNo" :disabled="!dev" placeholder="輸入 Lot／OrgLot"><button class="quiet" type="button" :disabled="!dev" @click="loadReferenceLot">帶入</button></div></label>
        <label>目的 FGD Inventory<select v-model="form.destinationInventory" :disabled="!dev"><option v-for="item in destinationInventories" :key="item">{{ item }}</option></select></label>
        <label>目的 Locator（可空白）<select v-model="form.locatorNo" :disabled="!dev"><option value="">N/A · 不指定</option><option v-for="item in locators" :key="item">{{ item }}</option></select></label>
        <label>掃描來源 WIP Inventory<select v-model="form.sourceInventory" :disabled="!dev"><option v-for="item in sourceInventories" :key="item">{{ item }}</option></select></label>
        <label>條碼 Select／UnSelect<div class="inline scan"><select v-model="scan.action" :disabled="!dev"><option value="select">Select</option><option value="unselect">UnSelect</option></select><input v-model="scan.lotNo" :disabled="!dev" placeholder="掃描 Lot／OrgLot" @keydown.enter.prevent="scanLot"><button class="quiet" type="button" :disabled="!dev" @click="scanLot">執行</button></div></label>
      </div>
      <div class="query-actions"><button class="primary" type="button" :disabled="!dev || working" @click="queryLots">載入 WIP 候選</button><span>ERP `sfu01` 單號在建單成功後取回；目前皆為 DEV 合成欄位。</span></div>
    </section>

    <div class="summary"><div><small>可選 WIP Lot</small><strong>{{ queried ? availableRows.length : '—' }}</strong></div><div><small>已選 Lot</small><strong>{{ selectedRows.length }}</strong></div><div><small>MES 入庫數量</small><strong>{{ fmt(selectedQty) }}</strong><small>{{ selectedRows[0]?.unitNo ?? 'PCS' }}</small></div><div><small>ERP 傳送數量</small><strong>{{ fmt(selectedRows.reduce((sum, row) => sum + erpQty(row), 0)) }}</strong><small>包裝批使用 SysQty</small></div></div>
    <div v-if="error || info" class="notice" :class="{ bad: !!error }" role="status">{{ error || info }}</div>

    <section class="card table-card">
      <div class="card-title"><div><h2>WIP 半成品庫存候選</h2><p>Service 取 Qty、MO、Lot、產品／版本、來源庫、ERPNo、OrgLotNo、InputDate、PackingLevel、SysQty、BookingFlag 與工時；畫面只列 Qty&gt;0 且 BookingFlag=0。</p></div><div class="table-actions"><button class="quiet" type="button" :disabled="!dev || !availableRows.length" @click="selectAll">Select All</button><button class="primary" type="button" :disabled="!dev || !availableMarked.length" @click="addRows(availableRows.filter(row => availableMarked.includes(row.id)))">Select</button></div></div>
      <div class="table-scroll"><table><thead><tr><th></th><th>LotNo／OrgLotNo</th><th>MO</th><th>Product／Version</th><th>ERP Product</th><th>Source Inventory</th><th class="num">可入庫 Qty</th><th class="num input-col">InInvNum</th><th>Pack／SysQty</th><th>Component／Bin</th><th>Booking</th></tr></thead><tbody>
        <tr v-for="row in availableRows" :key="row.id" :class="{ marked: availableMarked.includes(row.id) }" @click="availableMarked = toggleMarked(row.id, availableMarked)"><td><input :checked="availableMarked.includes(row.id)" type="checkbox" @click.stop="availableMarked = toggleMarked(row.id, availableMarked)"></td><td><b>{{ row.lotNo }}</b><small>{{ row.orgLotNo }}</small></td><td>{{ row.moNo }}</td><td>{{ row.productNo }} · {{ row.productVersion }}</td><td>{{ row.erpNo || '使用 ProductNo' }}</td><td>{{ row.sourceInventoryNo }}</td><td class="num">{{ fmt(row.qty) }} {{ row.unitNo }}</td><td class="num" @click.stop><input v-model.number="row.inInvNum" class="qty-input" type="number" min="0" :max="row.qty" step="0.000001"></td><td>{{ row.packingLevel === null ? '一般 Lot' : `Level ${row.packingLevel} · ${fmt(row.sysQty ?? 0)}` }}</td><td><span :class="{ warning: row.componentCount + row.goodBinCount > 0 }">C{{ row.componentCount }}／B{{ row.goodBinCount }}</span></td><td><span class="flag">{{ row.bookingFlag }}</span></td></tr>
        <tr v-if="queried && !availableRows.length"><td colspan="11" class="empty">沒有符合 MO、來源庫、Qty 及 BookingFlag 條件的 DEV Lot</td></tr><tr v-if="!queried"><td colspan="11" class="empty">先選／輸入 MO 與來源庫，再載入候選</td></tr>
      </tbody></table></div>
      <p class="helper">部分入庫可用於沒有 Component／Good Bin 的 Lot；有 Component 或 Bin 明細時，舊 Form 會阻止部分入庫。Full Lot 才會設 BookingFlag=1。</p>
    </section>

    <section class="card table-card selected-card">
      <div class="card-title"><div><h2>Lot Selected · 待入庫明細</h2><p>TipTop ERP 數量遇包裝批使用 SysQty；MES `MOStockIn_TP` 使用 InInvNum。</p></div><div class="table-actions"><button class="quiet" type="button" :disabled="!dev || !selectedRows.length" @click="clearSelected">Clear</button><button class="quiet" type="button" :disabled="!dev || !selectedMarked.length" @click="removeRows(selectedRows.filter(row => selectedMarked.includes(row.id)))">UnSelect</button><button class="primary" type="button" :disabled="!dev || working || !selectedRows.length" @click="confirmStockIn">{{ working ? '入庫中…' : 'Confirm 入庫' }}</button></div></div>
      <div class="table-scroll"><table><thead><tr><th></th><th>Lot／MO</th><th>Product／Version</th><th>來源庫</th><th class="num">可入庫 Qty</th><th class="num input-col">InInvNum</th><th class="num">ERP Qty</th><th>ERP Lot／Product</th><th>Component／Bin</th><th>Resource (EMP／EQP)</th></tr></thead><tbody>
        <tr v-for="row in selectedRows" :key="row.id" :class="{ marked: selectedMarked.includes(row.id) }" @click="selectedMarked = toggleMarked(row.id, selectedMarked)"><td><input :checked="selectedMarked.includes(row.id)" type="checkbox" @click.stop="selectedMarked = toggleMarked(row.id, selectedMarked)"></td><td><b>{{ row.lotNo }}</b><small>{{ row.moNo }}</small></td><td>{{ row.productNo }} · {{ row.productVersion }}</td><td>{{ row.sourceInventoryNo }}</td><td class="num">{{ fmt(row.qty) }} {{ row.unitNo }}</td><td class="num" @click.stop><input v-model.number="row.inInvNum" class="qty-input" type="number" min="0" :max="row.qty" step="0.000001"></td><td class="num">{{ fmt(erpQty(row)) }}</td><td>{{ erpLot(row) }}<small>{{ row.erpNo || row.productNo }}</small></td><td>C{{ row.componentCount }}／B{{ row.goodBinCount }}<small v-if="row.goodBinCount" class="warning">Bin Core 流程待核</small></td><td>{{ processCost === '1' ? '不送成本工時' : `${fmt(row.resEmp)}／${fmt(row.resEqp)}` }}</td></tr>
        <tr v-if="!selectedRows.length"><td colspan="10" class="empty">從上方選取 WIP Lot，或用 Lot 條碼掃描</td></tr>
      </tbody></table></div>
      <p class="helper">目的庫：{{ form.destinationInventory }}　·　Locator：{{ form.locatorNo || 'N/A' }}　·　`OrgLotNoToERP`：{{ orgLotNoToErp ? '使用 OrgLotNo' : '使用 MES LotNo' }}　·　`TPERPProcessCost`：{{ processCost }}</p>
    </section>

    <details class="card dev-rules"><summary>DEV ERP 參數與結果情境</summary><div class="rule-grid"><label><input v-model="orgLotNoToErp" type="checkbox"> 模擬 `OrgLotNoToERP=1`</label><label>TPERPProcessCost<select v-model="processCost"><option value="0">0 · 傳工時／機時</option><option value="1">1 · 不傳成本工時</option></select></label><label>Confirm 結果<select v-model="resultMode"><option value="success">TipTop code=0，MES 成功</option><option value="erp-fail">TipTop code 非 0</option><option value="mes-fail-undo-ok">MES 失敗、ERP Undo 成功</option><option value="mes-fail-undo-fail">MES 失敗、ERP Undo 失敗</option></select></label></div><p>DEV 只呈現來源行為與補償風險；不呼叫 TipTop SOAP 或 EAI Service。</p></details>

    <section class="card history"><div class="card-title"><div><h2>DEV 入庫／補償歷程</h2><p>ERP 建單成功與 MES 本地入庫不是同一交易；undo 結果須可追查。</p></div></div><div class="table-scroll"><table><thead><tr><th>時間</th><th>MES FGD In No.</th><th>TipTop No.</th><th>Lot 數／MES Qty</th><th>ERP</th><th>MES</th><th>ERP Undo</th><th>狀態說明</th></tr></thead><tbody><tr v-for="(row, index) in history" :key="`${row.time}-${index}`"><td>{{ row.time }}</td><td class="mono">{{ row.mesNo }}</td><td class="mono">{{ row.erpNo }}</td><td>{{ row.lots }}／{{ fmt(row.qty) }}</td><td>{{ row.erp }}</td><td>{{ row.mes }}</td><td>{{ row.undo }}</td><td>{{ row.note }}</td></tr><tr v-if="!history.length"><td colspan="8" class="empty">尚無本頁操作</td></tr></tbody></table></div></section>
    <footer>公司端僅使用合成 DEV 資料。TipTop `CreateStockInData` code=0 後才呼叫 MES；MES 失敗會嘗試 `UndoCreateStockInData`。部分入庫 Lot 沒有 BookingFlag reservation，需由正式服務以交易鎖／版本檢查避免並發重送。</footer>
  </section>
</template>

<style scoped>
.tp-stockin{--ink:#1a293b;--muted:#6d7c8e;--line:#dce4eb;--navy:#173b5d;color:var(--ink);height:100%;min-height:0;overflow:auto;padding:22px 26px 30px;background:#f7f9fb;font:13px/1.45 "Segoe UI","Microsoft JhengHei",sans-serif}.head{display:flex;align-items:center;justify-content:space-between;margin-bottom:16px}.eyebrow{margin:0;color:#73869a;font-size:10px;font-weight:750;letter-spacing:.13em}.head h1{margin:2px 0 0;font-size:23px}.subtitle{margin:0;color:var(--muted);font-size:12px}.head-actions{display:flex;align-items:center;gap:12px}.dev-pill{padding:5px 10px;border-radius:999px;background:#e8f4ed;color:#22724a;font-weight:700}.quiet,.primary{height:32px;padding:0 12px;border:1px solid var(--line);border-radius:5px;background:white;color:#334d66;font:inherit;cursor:pointer}.primary{border-color:var(--navy);background:var(--navy);color:white;font-weight:700}.primary:disabled,.quiet:disabled{opacity:.45;cursor:not-allowed}.flow{display:flex;align-items:center;gap:11px;padding:11px 15px;margin-bottom:14px;border:1px solid #dce6ef;border-radius:6px;background:#f1f6fa;color:#536f86}.flow b{color:#244a68}.flow span:last-child{margin-left:auto;color:#8a6a43;font-size:10px}.card{margin-bottom:13px;border:1px solid var(--line);border-radius:7px;background:white;box-shadow:0 2px 5px #10283a08}.form-card{padding:14px 15px}.form-grid{display:grid;grid-template-columns:repeat(4,minmax(160px,1fr));gap:12px}.form-grid label,.rule-grid label{display:flex;flex-direction:column;gap:5px;color:#5e7185;font-size:11px;font-weight:650}.form-grid input,.form-grid select,.rule-grid select{min-width:0;height:33px;border:1px solid #cdd8e2;border-radius:4px;padding:0 9px;background:white;color:var(--ink);font:inherit}.form-grid input[readonly]{background:#f4f6f8;color:#687b8d}.inline{display:flex;gap:5px}.inline>*{min-width:0;flex:1}.inline button{flex:none}.scan select{max-width:92px}.scan input{flex:1}.query-actions{display:flex;align-items:center;gap:12px;margin-top:13px}.query-actions span,.helper{color:var(--muted);font-size:10px}.summary{display:flex;align-items:stretch;margin:12px 0;background:white;border:1px solid var(--line);border-radius:7px;overflow:hidden}.summary>div{min-width:142px;padding:9px 14px;border-right:1px solid var(--line);display:grid;grid-template-columns:1fr auto;align-items:center;column-gap:8px}.summary small{color:var(--muted);font-size:10px}.summary strong{grid-row:span 2;color:#285878;font-size:18px}.notice{margin:0 0 12px;padding:9px 12px;border:1px solid #bddcc9;border-radius:5px;background:#eef8f1;color:#286440}.notice.bad{border-color:#ebc5c1;background:#fff3f1;color:#9e3831}.card-title{display:flex;align-items:center;justify-content:space-between;gap:12px;padding:12px 15px}.card-title h2{margin:0;font-size:14px}.card-title p{margin:2px 0 0;color:var(--muted);font-size:10px}.table-actions{display:flex;gap:6px;flex:none}.table-scroll{overflow:auto;border-top:1px solid #e6ebf0}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:10px}th{position:sticky;top:0;background:#f1f5f8;color:#607388;font-size:9px;font-weight:700;text-align:left}th,td{padding:7px 8px;border-bottom:1px solid #ebeff3}tbody tr{cursor:pointer}tbody tr:hover{background:#f8fafc}tbody tr.marked{background:#fff5ed}td b{font-family:Consolas,monospace;color:#536e85}td small{display:block;color:#8b99a7;font-size:9px}.num{text-align:right;font-variant-numeric:tabular-nums}.qty-input{width:84px;height:27px;border:1px solid #bacbd9;border-radius:4px;padding:0 5px;text-align:right;font:inherit}.input-col{color:#9a5b36}.flag{display:inline-block;padding:2px 6px;border-radius:4px;background:#edf3f7;color:#607388;font-weight:700}.warning{color:#a75836;font-weight:650}.empty{text-align:center;color:#8a98a7;padding:18px}.helper{margin:0;padding:8px 12px;border-top:1px solid #ebeff3;background:#fafcfd}.selected-card{border-color:#cad9e4}.dev-rules{padding:10px 14px}.dev-rules summary{color:#34536d;font-size:11px;font-weight:700;cursor:pointer}.rule-grid{display:grid;grid-template-columns:repeat(3,minmax(160px,1fr));gap:14px;padding:12px 0 4px}.rule-grid label:first-child{flex-direction:row;align-items:center}.dev-rules p{margin:5px 0;color:var(--muted);font-size:10px}.history .card-title{padding-bottom:10px}.mono{font-family:Consolas,monospace;color:#647a8e}footer{padding:2px 2px 8px;color:#8391a0;font-size:10px}.unavailable{padding:16px;border:1px solid #e8d2ac;border-radius:6px;background:#fff8ea;color:#7a5622}@media(max-width:1100px){.tp-stockin{padding:15px}.form-grid{grid-template-columns:repeat(2,minmax(160px,1fr))}.summary{flex-wrap:wrap}.flow{align-items:flex-start;flex-wrap:wrap}.flow span:last-child{width:100%;margin:0}.rule-grid{grid-template-columns:1fr}}
</style>
