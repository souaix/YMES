<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

type SourceKind = 0 | 1 | 2
type ResultMode = 'success' | 'erp-warning' | 'erp-fail' | 'mes-fail'
type ScrapRow = {
  id: string; moNo: string; lotNo: string; lotSerial: string; opNo: string; productNo: string
  source: SourceKind; qty: number; unitNo: string; inventoryNo: string; inputDate: string
  boxNo: string; boxNoFlag: boolean; dateCode: string; dateCodeDefault: string; dateCodeFlag: boolean
  bookingFlag: number; selected: boolean; bins: Record<string, number>
}
type ErrorRow = { lotSerial: string; errorNo: string; reasonName: string; errorQty: number }
type ComponentRow = { lotSerial: string; componentNo: string; goodQty: number; scrapQty: number; unitNo: string }
type HistoryRow = { time: string; erpNo: string; moNo: string; source0: number; source1: number; source2: number; qty: number; erp: string; mes: string; compensation: string }

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const form = reactive({ moNo: dev.value ? 'DEV-MO-622-01' : '', inventoryNo: dev.value ? 'DEV-SCR-01' : '', locatorNo: '', docType: dev.value ? 'SCR-01' : '', referenceLot: '' })
const documentTypes = dev.value ? [{ value: 'SCR-01', label: 'SCR-01｜不良品入庫' }, { value: 'SCR-02', label: 'SCR-02｜報廢入庫' }] : []
const inventories = dev.value ? ['DEV-SCR-01', 'DEV-SCR-02'] : []
const locators = dev.value ? ['S-01', 'S-02', 'S-03'] : []
const rows = ref<ScrapRow[]>(dev.value ? [
  { id: 's0', moNo: 'DEV-MO-622-01', lotNo: 'DEV-LOT-622-001', lotSerial: 'DEV-SERIAL-622-001', opNo: 'DEV-OP-TEST', productNo: 'DEV-PRD-622', source: 0, qty: 18, unitNo: 'PCS', inventoryNo: 'DEV-WIP-SCR-01', inputDate: '2026-11-21 08:20', boxNo: '', boxNoFlag: false, dateCode: '', dateCodeDefault: '', dateCodeFlag: false, bookingFlag: 0, selected: false, bins: {} },
  { id: 's1', moNo: 'DEV-MO-622-01', lotNo: 'DEV-LOT-622-002', lotSerial: 'DEV-SERIAL-622-002', opNo: 'DEV-OP-TEST', productNo: 'DEV-PRD-622', source: 1, qty: 2, unitNo: 'PCS', inventoryNo: 'DEV-WIP-SCR-01', inputDate: '2026-11-21 09:15', boxNo: 'DEV-BOX-SCR-02', boxNoFlag: true, dateCode: '', dateCodeDefault: '2611', dateCodeFlag: true, bookingFlag: 0, selected: false, bins: {} },
  { id: 's2', moNo: 'DEV-MO-622-01', lotNo: 'DEV-LOT-622-003', lotSerial: 'DEV-SERIAL-622-003', opNo: 'DEV-OP-ASSY', productNo: 'DEV-PRD-622', source: 2, qty: 9, unitNo: 'PCS', inventoryNo: 'DEV-WIP-SCR-02', inputDate: '2026-11-21 10:05', boxNo: '', boxNoFlag: false, dateCode: '', dateCodeDefault: '', dateCodeFlag: false, bookingFlag: 0, selected: false, bins: { BIN01: 7, BIN02: 2, BIN99: 0 } },
  { id: 's3', moNo: 'DEV-MO-622-02', lotNo: 'DEV-LOT-622-004', lotSerial: 'DEV-SERIAL-622-004', opNo: 'DEV-OP-FT', productNo: 'DEV-PRD-623', source: 1, qty: 1, unitNo: 'PCS', inventoryNo: 'DEV-WIP-SCR-03', inputDate: '2026-11-20 16:40', boxNo: 'DEV-BOX-SCR-04', boxNoFlag: true, dateCode: '2611', dateCodeDefault: '', dateCodeFlag: true, bookingFlag: 0, selected: false, bins: {} },
] : [])
const errorRows: ErrorRow[] = dev.value ? [
  { lotSerial: 'DEV-SERIAL-622-001', errorNo: 'DEV-ERR-11', reasonName: '外觀刮傷', errorQty: 18 },
  { lotSerial: 'DEV-SERIAL-622-002', errorNo: 'DEV-ERR-22', reasonName: '元件電性不良', errorQty: 2 },
  { lotSerial: 'DEV-SERIAL-622-004', errorNo: 'DEV-ERR-28', reasonName: '測試失敗', errorQty: 1 },
] : []
const componentRows: ComponentRow[] = dev.value ? [
  { lotSerial: 'DEV-SERIAL-622-002', componentNo: 'DEV-COMP-622-01', goodQty: 1, scrapQty: 1, unitNo: 'PCS' },
  { lotSerial: 'DEV-SERIAL-622-002', componentNo: 'DEV-COMP-622-02', goodQty: 0, scrapQty: 1, unitNo: 'PCS' },
  { lotSerial: 'DEV-SERIAL-622-004', componentNo: 'DEV-COMP-622-03', goodQty: 0, scrapQty: 1, unitNo: 'PCS' },
] : []
const includeLotScrap = ref(false)
const currentSerial = ref('')
const resultMode = ref<ResultMode>('success')
const history = ref<HistoryRow[]>(dev.value ? [
  { time: '2026-11-21 07:35:22', erpNo: 'DEV-ERP-SCR-00102', moNo: 'DEV-MO-622-02', source0: 0, source1: 1, source2: 0, qty: 1, erp: '成功', mes: '成功', compensation: '—' },
] : [])
const info = ref('')
const error = ref('')
const working = ref(false)
const nextDoc = ref(103)
const sourceNames: Record<SourceKind, string> = { 0: 'Lot Scrap', 1: 'Component Scrap', 2: 'Fail Bin' }
const visibleRows = computed(() => rows.value.filter(row => row.moNo === form.moNo && row.bookingFlag === 0 && row.qty > 0 && (includeLotScrap.value || row.source !== 0)))
const selectedRows = computed(() => visibleRows.value.filter(row => row.selected))
const selectedQty = computed(() => selectedRows.value.reduce((sum, row) => sum + row.qty, 0))
const currentRow = computed(() => rows.value.find(row => row.lotSerial === currentSerial.value) ?? null)
const currentErrors = computed(() => errorRows.filter(row => row.lotSerial === currentSerial.value))
const currentComponents = computed(() => componentRows.filter(row => row.lotSerial === currentSerial.value))
const binColumns = computed(() => [...new Set(rows.value.flatMap(row => Object.keys(row.bins)))])
const formatQty = (value: number) => new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 6 }).format(value)
const nowText = () => new Date().toISOString().slice(0, 19).replace('T', ' ')

function allowDev(): boolean {
  if (dev.value) return true
  error.value = '不良品 ERP／MES 入庫服務尚未接入；正式環境不會讀取或送出資料。'
  return false
}
function clearMessage(): void { error.value = ''; info.value = '' }
function query(): void {
  clearMessage()
  if (!allowDev()) return
  if (!form.moNo) { error.value = '請選擇工單。'; return }
  currentSerial.value = ''
  info.value = `已載入 ${visibleRows.value.length} 筆可入庫的不良 Lot／Serial（DEV 假資料）。`
}
function selectAll(checked: boolean): void { visibleRows.value.forEach(row => { row.selected = checked }) }
function showDetail(row: ScrapRow): void { currentSerial.value = row.lotSerial; clearMessage() }
function sourceCount(source: SourceKind): number { return selectedRows.value.filter(row => row.source === source).length }
function findReferenceLot(): void {
  clearMessage()
  if (!allowDev()) return
  const target = form.referenceLot.trim().toUpperCase()
  if (!target) { error.value = '請輸入 Ref. Lot No。'; return }
  const match = rows.value.find(row => row.lotNo.toUpperCase() === target)
  if (!match) { error.value = '找不到此 Ref. Lot No 的 DEV 不良品資料。'; return }
  form.moNo = match.moNo
  if (match.source === 0) includeLotScrap.value = true
  query()
  currentSerial.value = match.lotSerial
  info.value = `已依 Ref. Lot No 載入工單 ${match.moNo} 與相關 LotSerial。`
}

function validate(): string {
  if (!form.docType) return '請選擇 ERP 不良品單別。'
  if (!form.moNo) return '請選擇工單。'
  if (!form.inventoryNo) return '請選擇不良品庫。'
  if (!selectedRows.value.length) return '請至少勾選一筆 Lot／Serial。'
  for (const row of selectedRows.value) {
    if (!(row.qty > 0)) return `${row.lotSerial} 沒有可入庫的不良數量。`
    if (row.boxNoFlag && !row.boxNo.trim()) return `${row.lotSerial} 必須輸入 BoxNo。`
    if (row.dateCodeFlag && !(row.dateCode.trim() || row.dateCodeDefault.trim())) return `${row.lotSerial} 必須輸入 DateCode 或使用預設值。`
  }
  for (const lotNo of new Set(selectedRows.value.map(row => row.lotNo))) {
    const sameLot = selectedRows.value.filter(row => row.lotNo === lotNo)
    if (new Set(sameLot.map(row => row.boxNo.trim())).size > 1) return `${lotNo} 的多筆 Serial 必須使用相同 BoxNo。`
    if (new Set(sameLot.map(row => row.dateCode.trim() || row.dateCodeDefault.trim())).size > 1) return `${lotNo} 的多筆 Serial 必須使用相同 DateCode。`
  }
  return ''
}

function confirmStockIn(): void {
  clearMessage()
  if (!allowDev() || working.value) return
  const validationError = validate()
  if (validationError) { error.value = validationError; return }
  working.value = true
  const picked = [...selectedRows.value]
  const qty = picked.reduce((sum, row) => sum + row.qty, 0)
  const timestamp = nowText()
  picked.forEach(row => { row.bookingFlag = 1 })
  const erpNo = 'DEV-ERP-SCR-' + String(nextDoc.value++).padStart(5, '0')
  const source0 = picked.filter(row => row.source === 0).length
  const source1 = picked.filter(row => row.source === 1).length
  const source2 = picked.filter(row => row.source === 2).length

  if (resultMode.value === 'erp-fail' || resultMode.value === 'erp-warning') {
    picked.forEach(row => { row.bookingFlag = 0 })
    const label = resultMode.value === 'erp-warning' ? 'W 警告（來源視為失敗）' : '失敗'
    history.value.unshift({ time: timestamp, erpNo: '—', moNo: form.moNo, source0, source1, source2, qty, erp: label, mes: '未執行', compensation: 'BookingFlag 已重設' })
    error.value = resultMode.value === 'erp-warning' ? 'E10ICD 回傳 W；不良品流程未將 W 視為成功，因此 MES 不執行並釋放預約。' : 'DEV 模擬 ERP 建單失敗；MES 未執行，已釋放批次預約。'
  } else if (resultMode.value === 'mes-fail') {
    picked.forEach(row => { row.bookingFlag = 0 })
    history.value.unshift({ time: timestamp, erpNo, moNo: form.moNo, source0, source1, source2, qty, erp: '成功', mes: '失敗', compensation: `已模擬 ERP 取消單；BookingFlag 已重設` })
    error.value = `DEV 模擬 MES 入庫失敗；已模擬 ERP_Stock_IN_Cancel（${erpNo}）並釋放批次預約。`
  } else {
    picked.forEach(row => { row.qty = 0; row.bookingFlag = 0; row.selected = false })
    history.value.unshift({ time: timestamp, erpNo, moNo: form.moNo, source0, source1, source2, qty, erp: '成功', mes: '成功', compensation: '—' })
    currentSerial.value = ''
    info.value = `DEV 模擬 E10ICD 不良品建單及 MES 入庫完成：${erpNo}，Lot Scrap ${source0}、Component Scrap ${source1}、Fail Bin ${source2}，共 ${formatQty(qty)} PCS。`
  }
  working.value = false
}
</script>

<template>
  <section class="scrap-e10">
    <header class="scrap-head"><div><p class="eyebrow">WIP · ERP INTEGRATION · WP622</p><h1>不良品入庫至 E10ICD ERP</h1><p class="subtitle">Scrap Stock In to E10ICD</p></div><div class="head-actions"><span v-if="dev" class="dev-pill">DEV 假資料</span><button class="quiet" type="button" @click="emit('close')">關閉</button></div></header>
    <div v-if="!dev" class="unavailable">正式 E10ICD 與 MES 不良入庫服務尚未接入；此頁不會連線或送出資料。</div>
    <div v-else class="flow"><b>ERP 不良單建單（RequestType=1）</b><span>→</span><b>MES 報廢庫入庫</b><span>MES 失敗會嘗試取消 ERP 單；本流程 ERP 回 W 不視為成功</span></div>

    <div class="toolbar">
      <label>工單 No.<select v-model="form.moNo" :disabled="!dev" @change="query"><option v-if="!dev" value="">正式來源未接入</option><template v-else><option value="DEV-MO-622-01">DEV-MO-622-01 · DEV-PRD-622</option><option value="DEV-MO-622-02">DEV-MO-622-02 · DEV-PRD-623</option></template></select></label>
      <label>ERP 單別<select v-model="form.docType" :disabled="!dev"><option v-for="item in documentTypes" :key="item.value" :value="item.value">{{ item.label }}</option></select></label>
      <label>不良品庫（InventoryType=3）<select v-model="form.inventoryNo" :disabled="!dev"><option v-for="item in inventories" :key="item" :value="item">{{ item }}</option></select></label>
      <label>儲位（可空白）<select v-model="form.locatorNo" :disabled="!dev"><option value="">N/A</option><option v-for="item in locators" :key="item" :value="item">{{ item }}</option></select></label>
      <label>Ref. Lot No<input v-model="form.referenceLot" type="text" :disabled="!dev" placeholder="輸入後按 Enter" @keydown.enter.prevent="findReferenceLot"></label>
      <button class="primary" type="button" :disabled="!dev" @click="query">查詢工單</button>
    </div>

    <div class="source-filter"><label class="toggle"><input v-model="includeLotScrap" type="checkbox" :disabled="!dev" @change="selectAll(false)"><span>納入一般 Lot Scrap（Source=0）</span></label><span>未勾選時只顯示 Component Scrap（1）與 Fail Bin（2）。查詢結果初始只列 BookingFlag=0。</span><button type="button" class="link" :disabled="!dev" @click="selectAll(true)">全選</button><button type="button" class="link" :disabled="!dev" @click="selectAll(false)">清除勾選</button></div>
    <div class="summary"><div><small>待入庫 Lot／Serial</small><strong>{{ visibleRows.length }}</strong></div><div><small>已勾選</small><strong>{{ selectedRows.length }}</strong></div><div><small>Lot Scrap / Component / Bin</small><strong>{{ sourceCount(0) }} / {{ sourceCount(1) }} / {{ sourceCount(2) }}</strong></div><div><small>入庫數量（整列）</small><strong>{{ formatQty(selectedQty) }}</strong><small>PCS</small></div><label>DEV 結果情境<select v-model="resultMode" :disabled="!dev"><option value="success">ERP、MES 成功</option><option value="erp-warning">ERP 回 W（非成功）</option><option value="erp-fail">ERP 建單失敗</option><option value="mes-fail">MES 失敗並取消 ERP 單</option></select></label></div>
    <div v-if="error || info" class="notice" :class="{ bad: !!error }" role="status">{{ error || info }}</div>

    <section class="card"><div class="card-title"><div><h2>待入庫不良 Lot／Serial</h2><p>Source 0=Lot Scrap Qty；1=Component 數量；2=Fail Bin 動態 Bin 合計。每列全量入庫，來源沒有部分數量欄。</p></div></div><div class="table-scroll"><table><thead><tr><th><input type="checkbox" :checked="!!visibleRows.length && selectedRows.length === visibleRows.length" :disabled="!dev" aria-label="全選" @change="selectAll(($event.target as HTMLInputElement).checked)"></th><th>Source</th><th>MONo</th><th>LotNo</th><th>LotSerial</th><th>OPNo</th><th class="num">Qty</th><th>UnitNo</th><th>InventoryNo</th><th>BoxNo</th><th>DateCode</th></tr></thead><tbody>
      <tr v-for="row in visibleRows" :key="row.id" :class="{ active: currentSerial === row.lotSerial, picked: row.selected }" @click="showDetail(row)"><td @click.stop><input v-model="row.selected" type="checkbox" :disabled="!dev" :aria-label="'選取 ' + row.lotSerial"></td><td><span class="source-chip" :class="'source-' + row.source">{{ row.source }} · {{ sourceNames[row.source] }}</span></td><td>{{ row.moNo }}</td><td class="lot">{{ row.lotNo }}</td><td class="serial">{{ row.lotSerial }}</td><td>{{ row.opNo }}</td><td class="num">{{ formatQty(row.qty) }}</td><td>{{ row.unitNo }}</td><td>{{ row.inventoryNo }}</td><td @click.stop><input v-model="row.boxNo" class="meta" type="text" :placeholder="row.boxNoFlag ? '必填' : '—'" :disabled="!dev || !row.boxNoFlag"></td><td @click.stop><input v-model="row.dateCode" class="meta" type="text" :placeholder="row.dateCodeDefault || (row.dateCodeFlag ? '必填' : '—')" :disabled="!dev || !row.dateCodeFlag"></td></tr>
      <tr v-if="!visibleRows.length"><td colspan="11" class="empty">此工單沒有符合來源條件的可入庫不良資料</td></tr>
    </tbody></table></div><div class="table-foot">已選 {{ selectedRows.length }} 列 · 數量 {{ formatQty(selectedQty) }} · 目的庫 {{ form.inventoryNo || '—' }} · 儲位 {{ form.locatorNo || 'N/A' }}</div></section>

    <div class="details">
      <section class="card"><div class="card-title"><div><h2>不良原因明細</h2><p>{{ currentSerial || '選取一筆 Lot／Serial 查看' }}</p></div></div><div v-if="currentErrors.length" class="detail-table"><div class="detail-header"><span>ErrorNo</span><span>ReasonName</span><span>ErrorQty</span></div><div v-for="item in currentErrors" :key="item.errorNo" class="detail-line"><span>{{ item.errorNo }}</span><span>{{ item.reasonName }}</span><span class="num">{{ formatQty(item.errorQty) }}</span></div></div><p v-else class="empty detail-empty">{{ currentSerial ? '沒有不良原因明細' : '尚未選取 Lot／Serial' }}</p></section>
      <section class="card"><div class="card-title"><div><h2>{{ currentRow?.source === 2 ? 'Fail Bin 明細' : 'Component Scrap 明細' }}</h2><p>{{ currentSerial || '選取一筆 Lot／Serial 查看' }}</p></div></div><div v-if="currentRow?.source === 2 && currentRow" class="detail-table"><div class="detail-header bin-detail"><span v-for="bin in binColumns" :key="bin">{{ bin }}</span></div><div class="detail-line bin-detail"><span v-for="bin in binColumns" :key="bin">{{ formatQty(currentRow.bins[bin] ?? 0) }}</span></div></div><div v-else-if="currentComponents.length" class="detail-table"><div class="detail-header component-detail"><span>ComponentNo</span><span>GoodQty</span><span>ScrapQty</span><span>UnitNo</span></div><div v-for="item in currentComponents" :key="item.componentNo" class="detail-line component-detail"><span>{{ item.componentNo }}</span><span class="num">{{ formatQty(item.goodQty) }}</span><span class="num">{{ formatQty(item.scrapQty) }}</span><span>{{ item.unitNo }}</span></div></div><p v-else class="empty detail-empty">{{ currentSerial ? '此 Serial 沒有 Component／Bin 明細' : '尚未選取 Lot／Serial' }}</p></section>
    </div>

    <section class="card history"><div class="card-title"><div><h2>DEV 處理紀錄</h2><p>本頁記憶體模擬，不是正式 ERP／MES 交易</p></div><button class="primary" type="button" :disabled="!dev || working" @click="confirmStockIn">{{ working ? '處理中…' : '確認不良入庫' }}</button></div><div class="table-scroll"><table><thead><tr><th>時間</th><th>ERP SCRInNo</th><th>工單</th><th>Source 0 / 1 / 2</th><th class="num">總數量</th><th>ERP 建單</th><th>MES 入庫</th><th>補償處理</th></tr></thead><tbody><tr v-for="(item, index) in history" :key="item.time + item.erpNo + index"><td>{{ item.time }}</td><td class="lot">{{ item.erpNo }}</td><td>{{ item.moNo }}</td><td>{{ item.source0 }} / {{ item.source1 }} / {{ item.source2 }}</td><td class="num">{{ formatQty(item.qty) }} PCS</td><td>{{ item.erp }}</td><td>{{ item.mes }}</td><td>{{ item.compensation }}</td></tr><tr v-if="!history.length"><td colspan="8" class="empty">尚無本頁操作</td></tr></tbody></table></div></section>
    <footer class="source-foot">Confirm：ERP `ERP_Stock_IN` RequestType=1 → MES `MOStockIn_Scrap_TP`；每列依整個 LotSerial 入庫。所有編號均為合成 DEV 假資料。</footer>
  </section>
</template>

<style scoped>
.scrap-e10{--ink:#1a293b;--muted:#6d7c8e;--line:#dce4eb;--navy:#173b5d;color:var(--ink);height:100%;min-height:0;overflow:auto;padding:22px 26px 30px;background:#f7f9fb;font:13px/1.45 "Segoe UI","Microsoft JhengHei",sans-serif}.scrap-head{display:flex;align-items:center;justify-content:space-between;margin-bottom:16px}.eyebrow{margin:0;color:#73869a;font-size:10px;font-weight:750;letter-spacing:.13em}.scrap-head h1{margin:2px 0 0;font-size:23px}.subtitle{margin:0;color:var(--muted);font-size:12px}.head-actions{display:flex;align-items:center;gap:12px}.dev-pill{padding:5px 10px;border-radius:999px;background:#e8f4ed;color:#22724a;font-weight:700}.quiet,.link{border:1px solid var(--line);background:white;color:#334d66;border-radius:5px;padding:7px 12px;cursor:pointer}.flow{display:flex;align-items:center;gap:12px;padding:11px 15px;margin-bottom:14px;border:1px solid #ead3ca;border-radius:6px;background:#fff4ee;color:#7b5345}.flow b{color:#75412d}.flow span:last-child{margin-left:auto;color:#856d63;font-size:11px}.toolbar{display:flex;align-items:end;gap:12px;padding:14px;background:white;border:1px solid var(--line);border-radius:7px}.toolbar label,.summary label{display:flex;flex-direction:column;gap:5px;color:#5e7185;font-size:11px;font-weight:650}.toolbar select,.summary select{min-width:145px;height:34px;border:1px solid #cdd8e2;border-radius:4px;padding:0 9px;background:white;color:var(--ink);font:inherit}.toolbar label:first-child select{min-width:235px}.toolbar label:nth-child(3) select{min-width:190px}.primary{height:34px;padding:0 15px;border:1px solid var(--navy);border-radius:4px;background:var(--navy);color:white;font:inherit;font-weight:700;cursor:pointer}.primary:disabled,.quiet:disabled,.link:disabled{opacity:.45;cursor:not-allowed}.source-filter{display:flex;align-items:center;gap:10px;padding:10px 13px;margin-top:12px;border:1px solid var(--line);border-radius:6px;background:white;color:var(--muted);font-size:10px}.toggle{display:flex;align-items:center;gap:7px;color:#405a70;font-weight:700}.source-filter .link{padding:4px 8px;font-size:10px}.source-filter span:nth-child(2){margin-right:auto}.summary{display:flex;align-items:center;gap:0;margin:12px 0;background:white;border:1px solid var(--line);border-radius:7px;overflow:hidden}.summary>div{min-width:112px;padding:9px 14px;border-right:1px solid var(--line);display:grid;grid-template-columns:1fr auto;align-items:center;column-gap:8px}.summary small{color:var(--muted);font-size:10px}.summary strong{grid-row:span 2;color:#8b4b37;font-size:18px}.summary label{margin-left:auto;padding:8px 12px;flex-direction:row;align-items:center}.summary select{min-width:180px;height:30px}.notice{margin:0 0 12px;padding:9px 12px;border:1px solid #bddcc9;border-radius:5px;background:#eef8f1;color:#286440}.notice.bad{border-color:#ebc5c1;background:#fff3f1;color:#9e3831}.card{margin-bottom:13px;border:1px solid var(--line);border-radius:7px;background:white;box-shadow:0 2px 5px #10283a08}.card-title{display:flex;align-items:center;justify-content:space-between;padding:12px 15px}.card-title h2{margin:0;font-size:14px}.card-title p{margin:2px 0 0;color:var(--muted);font-size:10px}.table-scroll{overflow:auto;border-top:1px solid #e6ebf0}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:11px}th{position:sticky;top:0;background:#f1f5f8;color:#607388;font-size:10px;font-weight:700;text-align:left}th,td{padding:8px 9px;border-bottom:1px solid #ebeff3}tbody tr{cursor:pointer}tbody tr:hover{background:#f8fafc}tbody tr.active{background:#fff3ed}tbody tr.picked{box-shadow:inset 3px 0 #b96743}.num{text-align:right;font-variant-numeric:tabular-nums}.lot{color:#8b4b37;font-weight:650}.serial{color:#60768b;font-family:Consolas,monospace}.source-chip{display:inline-block;padding:3px 6px;border-radius:3px;font-size:9px;font-weight:700}.source-0{background:#fff0df;color:#965618}.source-1{background:#ebf3fb;color:#315e87}.source-2{background:#f0edf9;color:#62518a}.meta{width:78px;padding:4px 5px;border:1px solid #d5dfe7;border-radius:3px;font:inherit}.empty{text-align:center;color:#8a98a7;padding:18px}.table-foot{padding:7px 12px;color:#728397;font-size:10px;background:#fafcfd}.details{display:grid;grid-template-columns:1fr 1.4fr;gap:13px}.detail-table{padding:0 14px 12px}.detail-table>div{display:grid;grid-template-columns:1fr 2fr 1fr;padding:6px 0;border-bottom:1px solid #eef1f4;font-size:10px}.detail-header{color:var(--muted);font-weight:700}.detail-line .num{text-align:right}.detail-table .component-detail{grid-template-columns:1.6fr 1fr 1fr .8fr}.detail-table .bin-detail{grid-template-columns:repeat(auto-fit,minmax(48px,1fr))}.detail-table .component-detail .num,.detail-table .bin-detail span:not(:first-child){text-align:right}.detail-empty{margin:0}.history .card-title{padding-bottom:10px}.source-foot{padding:2px 2px 8px;color:#8391a0;font-size:10px}.unavailable{padding:16px;border:1px solid #e8d2ac;border-radius:6px;background:#fff8ea;color:#7a5622}@media(max-width:1000px){.scrap-e10{padding:15px}.toolbar{flex-wrap:wrap}.summary{flex-wrap:wrap}.summary label{margin-left:0}.details{grid-template-columns:1fr}.flow{align-items:flex-start;flex-wrap:wrap}.flow span:last-child{width:100%;margin:0}.source-filter{flex-wrap:wrap}}
.toolbar input{min-width:145px;height:34px;border:1px solid #cdd8e2;border-radius:4px;padding:0 9px;background:white;color:var(--ink);font:inherit}.toolbar label:nth-child(5) input{width:160px}
</style>
