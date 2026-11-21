<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

type AllowErpMode = '0' | '1' | '2'
type SubmitMode = 'success' | 'erp-fail' | 'erp-timeout' | 'mes-fail'
type RetryMode = 'success' | 'fail' | 'timeout'
type Mo = { moNo: string; state: number; moTypeNo: string; factoryNo: string; qty: number; fgdInQty: number; scrapInQty: number; materialIssuedQty: number; materialStdQty: number }
type StockRow = {
  id: string; moNo: string; moTypeNo: string; factoryNo: string; productNo: string; productVersion: string
  erpNo: string; erpUnitNo: string; lotNo: string; orgLotNo: string; baseLotNo: string; lotSerial: string
  sourceInventory: string; unitNo: string; goodQty: number; scrapQty: number; lossQty: number
  bookingFlag: number; available: boolean; status: string
}
type ReThrowTicket = { id: string; mesNo: string; sheetType: string; createdAt: string; rowIds: string[]; state: number; requestXml: string }
type HistoryRow = { at: string; mesNo: string; erpNo: string; mode: string; outcome: string; state: string; note: string }

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const systemMode = ref<AllowErpMode>('2')
const showLossData = ref(true)
const form = reactive({ moInput: '', sheetType: 'WF01:工單完工入庫', fgdInventory: 'DEV-FGD-01', locator: 'DEV-LOC-A01', factoryNo: '', mesNo: '' })
const modes: { value: AllowErpMode; label: string }[] = [
  { value: '0', label: '0 · 僅 MES 入庫' },
  { value: '1', label: '1 · MES + WorkFlow（略過數量檢查）' },
  { value: '2', label: '2 · MES + WorkFlow（含數量檢查）' },
]
const moList: Mo[] = [
  { moNo: 'DEV-MO-614-01', state: 3, moTypeNo: 'A1', factoryNo: 'DEV-F01', qty: 240, fgdInQty: 64, scrapInQty: 6, materialIssuedQty: 500, materialStdQty: 2 },
  { moNo: 'DEV-MO-614-02', state: 6, moTypeNo: 'B2', factoryNo: 'DEV-F02', qty: 160, fgdInQty: 45, scrapInQty: 5, materialIssuedQty: 320, materialStdQty: 1.5 },
  { moNo: 'DEV-MO-614-03', state: 99, moTypeNo: 'C1', factoryNo: 'DEV-F01', qty: 80, fgdInQty: 80, scrapInQty: 0, materialIssuedQty: 200, materialStdQty: 1 },
]
const rows = ref<StockRow[]>(dev.value ? [
  { id: 'wf-r1', moNo: 'DEV-MO-614-01', moTypeNo: 'A1', factoryNo: 'DEV-F01', productNo: 'DEV-PRD-614', productVersion: 'A1', erpNo: 'WF-PRD-614', erpUnitNo: 'PCS', lotNo: 'DEV-LOT-614-001', orgLotNo: 'DEV-ORG-614-001', baseLotNo: 'DEV-BASE-614-001', lotSerial: '', sourceInventory: 'DEV-WIP-01', unitNo: 'PCS', goodQty: 72, scrapQty: 0, lossQty: 0, bookingFlag: 0, available: true, status: '可選' },
  { id: 'wf-r2', moNo: 'DEV-MO-614-01', moTypeNo: 'A1', factoryNo: 'DEV-F01', productNo: 'DEV-PRD-614', productVersion: 'A1', erpNo: 'WF-PRD-614', erpUnitNo: 'PCS', lotNo: 'DEV-LOT-614-002', orgLotNo: 'DEV-ORG-614-002', baseLotNo: 'DEV-BASE-614-002', lotSerial: 'DEV-SCR-614-002', sourceInventory: 'DEV-WIP-01', unitNo: 'PCS', goodQty: 18, scrapQty: 3, lossQty: 1, bookingFlag: 0, available: true, status: '可選' },
  { id: 'wf-r3', moNo: 'DEV-MO-614-01', moTypeNo: 'A1', factoryNo: 'DEV-F01', productNo: 'DEV-PRD-614', productVersion: 'A1', erpNo: 'WF-PRD-614', erpUnitNo: 'PCS', lotNo: 'DEV-LOT-614-003', orgLotNo: 'DEV-ORG-614-003', baseLotNo: 'DEV-BASE-614-003', lotSerial: 'DEV-FAIL-614-003', sourceInventory: 'DEV-WIP-02', unitNo: 'PCS', goodQty: 0, scrapQty: 0, lossQty: 4, bookingFlag: 0, available: true, status: '可選' },
  { id: 'wf-r4', moNo: 'DEV-MO-614-02', moTypeNo: 'B2', factoryNo: 'DEV-F02', productNo: 'DEV-PRD-615', productVersion: 'B2', erpNo: 'WF-PRD-615', erpUnitNo: 'BOX', lotNo: 'DEV-LOT-614-004', orgLotNo: 'DEV-ORG-614-004', baseLotNo: 'DEV-BASE-614-004', lotSerial: '', sourceInventory: 'DEV-WIP-02', unitNo: 'BOX', goodQty: 24, scrapQty: 2, lossQty: 0, bookingFlag: 0, available: true, status: '可選' },
  { id: 'wf-r5', moNo: 'DEV-MO-614-02', moTypeNo: 'B2', factoryNo: 'DEV-F02', productNo: 'DEV-PRD-615', productVersion: 'B2', erpNo: 'WF-PRD-615', erpUnitNo: 'BOX', lotNo: 'DEV-LOT-614-005', orgLotNo: 'DEV-ORG-614-005', baseLotNo: 'DEV-BASE-614-005', lotSerial: '', sourceInventory: 'DEV-WIP-02', unitNo: 'BOX', goodQty: 36, scrapQty: 0, lossQty: 0, bookingFlag: 1, available: true, status: '已預約' },
] : [])
const queriedMoNos = ref<string[]>([])
const selectedIds = ref<string[]>([])
const submitMode = ref<SubmitMode>('success')
const retryMode = ref<RetryMode>('success')
const retryFilter = reactive({ sheetType: '', mesNo: '' })
const retrySelectedIds = ref<string[]>([])
const tickets = ref<ReThrowTicket[]>(dev.value ? [{ id: 'wf-ticket-1', mesNo: 'DEV-FGD-614-0007', sheetType: 'WF01', createdAt: '2026-11-22 09:18', rowIds: ['wf-r6'], state: 1, requestXml: '<DEV RequestXML：MOCTF／MOCTG 入庫請求資料>' }] : [])
const ticketRows = ref<StockRow[]>(dev.value ? [{ id: 'wf-r6', moNo: 'DEV-MO-614-02', moTypeNo: 'B2', factoryNo: 'DEV-F02', productNo: 'DEV-PRD-615', productVersion: 'B2', erpNo: 'WF-PRD-615', erpUnitNo: 'BOX', lotNo: 'DEV-LOT-614-006', orgLotNo: 'DEV-ORG-614-006', baseLotNo: 'DEV-BASE-614-006', lotSerial: '', sourceInventory: 'DEV-WIP-02', unitNo: 'BOX', goodQty: 12, scrapQty: 0, lossQty: 0, bookingFlag: 1, available: false, status: 'ERP 等待重拋' }] : [])
const history = ref<HistoryRow[]>([])
const showRetry = ref(false)
const queried = ref(false)
const error = ref('')
const info = ref('')
const busy = ref(false)
const ticketNo = ref(8)
const erpNo = ref(142)
const moInputOptions = computed(() => moList.map(mo => mo.moNo))
const eligibleRows = computed(() => rows.value.filter(row => queriedMoNos.value.includes(row.moNo) && row.available && row.bookingFlag === 0 && (showLossData.value || row.goodQty > 0 || row.scrapQty > 0)))
const displayedRows = computed(() => eligibleRows.value)
const selectedRows = computed(() => rows.value.filter(row => selectedIds.value.includes(row.id) && row.available))
const selectedTotal = computed(() => selectedRows.value.reduce((sum, row) => sum + row.goodQty + row.scrapQty, 0))
const quantityCheck = computed(() => {
  if (systemMode.value !== '2' || !selectedRows.value.length) return { ok: true, messages: [] as string[] }
  const messages: string[] = []
  const byMo = new Map<string, StockRow[]>()
  selectedRows.value.forEach(row => byMo.set(row.moNo, [...(byMo.get(row.moNo) ?? []), row]))
  for (const [moNo, selected] of byMo) {
    const mo = moList.find(item => item.moNo === moNo)
    if (!mo) { messages.push(`${moNo}：MO 資料不存在`); continue }
    const incoming = selected.reduce((sum, row) => sum + row.goodQty + row.scrapQty, 0)
    const outputAfter = mo.fgdInQty + mo.scrapInQty + incoming
    const remaining = mo.qty - mo.fgdInQty - mo.scrapInQty
    if (incoming > remaining) messages.push(`${moNo}：入庫 ${fmt(incoming)} 超過工單剩餘 ${fmt(remaining)}`)
    const requiredMaterial = mo.materialStdQty * outputAfter
    if (mo.materialIssuedQty < requiredMaterial) messages.push(`${moNo}：ERP 淨發料 ${fmt(mo.materialIssuedQty)} 少於需領 ${fmt(requiredMaterial)}`)
  }
  return { ok: messages.length === 0, messages }
})
const visibleTickets = computed(() => tickets.value.filter(ticket => (!retryFilter.sheetType || ticket.sheetType === retryFilter.sheetType) && (!retryFilter.mesNo || ticket.mesNo.toLowerCase().includes(retryFilter.mesNo.toLowerCase()))))
const selectedTickets = computed(() => visibleTickets.value.filter(ticket => retrySelectedIds.value.includes(ticket.id)))
const modeLabel = computed(() => modes.find(mode => mode.value === systemMode.value)?.label ?? '')
const fmt = (qty: number) => new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 4 }).format(qty)
const nowText = () => new Date().toLocaleString('zh-TW', { hour12: false })

function queryMo(): void {
  error.value = ''
  info.value = ''
  if (!dev.value) { error.value = '正式 WorkFlow／MES 服務尚未接入；此頁不會讀取或送出資料。'; return }
  const mo = moList.find(item => item.moNo === form.moInput)
  if (!mo) { error.value = '請選擇一張已 Release 的 DEV 工單。'; return }
  if (![3, 6].includes(mo.state)) { error.value = `工單狀態 ${mo.state} 不在來源查詢允許的 3／6。`; return }
  form.factoryNo = mo.factoryNo
  if (!queriedMoNos.value.includes(mo.moNo)) queriedMoNos.value.push(mo.moNo)
  queried.value = true
  info.value = `${mo.moNo} 已載入；已勾選批次會保留，畫面可累積多張 MO 的批次。`
}

function syncErpMode(): void {
  if (systemMode.value === '0') form.sheetType = 'MES'
  else if (form.sheetType === 'MES') form.sheetType = 'WF01:工單完工入庫'
}

function toggleRow(row: StockRow): void {
  if (!row.available || row.bookingFlag !== 0) return
  selectedIds.value = selectedIds.value.includes(row.id) ? selectedIds.value.filter(id => id !== row.id) : [...selectedIds.value, row.id]
  error.value = ''
}

function selectCurrent(): void {
  selectedIds.value = [...new Set([...selectedIds.value, ...displayedRows.value.map(row => row.id)])]
}

function clearSelection(): void { selectedIds.value = [] }

function submit(): void {
  error.value = ''
  info.value = ''
  if (!dev.value || busy.value) return
  if (!form.sheetType || !form.moInput || !form.fgdInventory || !form.locator || !selectedRows.value.length) {
    error.value = '請填寫單別、MO、成品庫、儲位並選取至少一筆批次。'
    return
  }
  if (new Set(selectedRows.value.map(row => row.factoryNo)).size > 1) {
    error.value = '目前選取批次含不同工廠；此 Form 以單一 FactoryNo 組 ERP 表頭，請分開入庫。'
    return
  }
  form.factoryNo = selectedRows.value[0].factoryNo
  if (selectedRows.value.some(row => row.goodQty === 0 && row.scrapQty === 0)) { error.value = '選取列只有 LossQty；Form 可顯示此列，但 Core 的入庫分支以 GoodQty／ScrapQty 判斷，請先核實原始處理規則。'; return }
  if (!quantityCheck.value.ok) { error.value = `ALLOWINV2ERP=2 數量檢查未通過：${quantityCheck.value.messages.join('；')}`; return }
  busy.value = true
  const stamp = nowText()
  const picked = [...selectedRows.value]
  const mesNo = `DEV-FGD-614-${String(ticketNo.value++).padStart(4, '0')}`
  form.mesNo = mesNo
  const mode = modeLabel.value
  if (submitMode.value === 'erp-timeout' && systemMode.value !== '0') {
    picked.forEach(row => { row.available = false; row.status = 'State=1 · 等待重拋' })
    const newTicket: ReThrowTicket = { id: `wf-ticket-${ticketNo.value}`, mesNo, sheetType: form.sheetType.split(':')[0], createdAt: stamp, rowIds: picked.map(row => row.id), state: 1, requestXml: '<DEV RequestXML：本次 MOCTF／MOCTG payload>' }
    tickets.value.unshift(newTicket)
    ticketRows.value.push(...picked)
    history.value.unshift({ at: stamp, mesNo, erpNo: '等待 ERP 確認', mode, outcome: '逾時／未知結果', state: '1', note: 'MES 入庫已建立；RequestXML 存入重拋票券，不可盲目重送' })
    info.value = `WorkFlow 回應逾時；MES 單 ${mesNo} 保留 State=1，已建立 DEV 重拋票券。`
  } else if (submitMode.value === 'erp-fail' && systemMode.value !== '0') {
    picked.forEach(row => { row.available = true; row.status = 'ERP 失敗 · MES 取消已回復' })
    selectedIds.value = []
    history.value.unshift({ at: stamp, mesNo, erpNo: '—', mode, outcome: 'WorkFlow 失敗', state: '3', note: '呼叫 MOStockIn_WF_Cancel_ForStockIn 撤回 MES 庫存' })
    info.value = `WorkFlow 建單失敗；DEV 模擬 MES 取消入庫，State=3，來源庫存重新可選。`
  } else if (submitMode.value === 'mes-fail') {
    picked.forEach(row => { row.available = true; row.status = 'MES 失敗 · 候選保留' })
    history.value.unshift({ at: stamp, mesNo, erpNo: '—', mode, outcome: 'MES 失敗', state: '未過帳', note: 'Form 另呼叫 ERP Undo helper；此分支的補償先後順序需家用端核實' })
    error.value = 'MES 入庫失敗，DEV 保留原來源批次。WinForms 的錯誤分支另呼叫 WorkFlow Undo，需核對其外部狀態與重試語意。'
  } else {
    picked.forEach(row => { row.available = false; row.status = systemMode.value === '0' ? 'MES 完成' : 'MES／ERP 完成' })
    const erpDoc = systemMode.value === '0' ? '' : `${form.sheetType.split(':')[0]}-${String(erpNo.value++).padStart(6, '0')}`
    history.value.unshift({ at: stamp, mesNo, erpNo: erpDoc || '（MES only）', mode, outcome: '成功', state: '2', note: systemMode.value === '0' ? '僅 MES 入庫，ERPNo 留空' : 'ERP 單號前綴 SheetType，MES State=2' })
    info.value = systemMode.value === '0' ? `MES 入庫完成 ${mesNo}；ALLOWINV2ERP=0，沒有送 WorkFlow。` : `WorkFlow 與 MES 入庫完成 ${mesNo}；ERPNo 已綁定並更新 State=2。`
    selectedIds.value = []
  }
  busy.value = false
}

function toggleTicket(ticket: ReThrowTicket): void {
  retrySelectedIds.value = retrySelectedIds.value.includes(ticket.id) ? retrySelectedIds.value.filter(id => id !== ticket.id) : [...retrySelectedIds.value, ticket.id]
}

function retryTickets(): void {
  error.value = ''
  info.value = ''
  if (!dev.value || busy.value || !selectedTickets.value.length) return
  busy.value = true
  for (const ticket of selectedTickets.value) {
    const stamp = nowText()
    if (retryMode.value === 'timeout') {
      history.value.unshift({ at: stamp, mesNo: ticket.mesNo, erpNo: '—', mode: '重拋', outcome: '仍逾時', state: '1', note: '票券保留，等待查明 WorkFlow 結果' })
      continue
    }
    tickets.value = tickets.value.filter(item => item.id !== ticket.id)
    retrySelectedIds.value = retrySelectedIds.value.filter(id => id !== ticket.id)
    const ticketLotRows = ticketRows.value.filter(row => ticket.rowIds.includes(row.id))
    if (retryMode.value === 'fail') {
      ticketLotRows.forEach(row => {
        row.available = true
        row.bookingFlag = 0
        row.status = '重拋失敗 · MES 已取消'
        if (!rows.value.some(candidate => candidate.id === row.id)) rows.value.push(row)
      })
      history.value.unshift({ at: stamp, mesNo: ticket.mesNo, erpNo: '—', mode: '重拋', outcome: 'WorkFlow 失敗', state: '3', note: '重拋後撤回 MES 入庫並刪除票券' })
    } else {
      const docNo = `${ticket.sheetType}-${String(erpNo.value++).padStart(6, '0')}`
      history.value.unshift({ at: stamp, mesNo: ticket.mesNo, erpNo: docNo, mode: '重拋', outcome: '成功', state: '2', note: 'ERP 單號回寫、刪除票券、OrgState=1' })
      ticketLotRows.forEach(row => { row.status = 'MES／ERP 完成' })
    }
  }
  busy.value = false
  if (retryMode.value === 'timeout') info.value = '重拋仍逾時；票券留在 State=1，稍後查詢或人工對帳。'
  else info.value = retryMode.value === 'success' ? '重拋成功：票券已移除，ERPNo 綁回 MES 單據。' : '重拋失敗：DEV 模擬撤回 MES 入庫並移除票券。'
}
</script>

<template>
  <section class="wf-stockin">
    <header class="hero"><div><p class="eyebrow">WIP · ERP INTEGRATION · WP614</p><h1>入庫至 WorkFlow ERP</h1><p class="subtitle">WIP SEMI／SCRAP → FGD Stock In</p></div><div class="hero-actions"><span class="dev-pill">DEV 假資料</span><button class="quiet" type="button" @click="showRetry = !showRetry">{{ showRetry ? '返回入庫' : `ERP 逾時重拋 (${tickets.length})` }}</button><button class="quiet" type="button" @click="emit('close')">關閉</button></div></header>
    <div v-if="!dev" class="unavailable">正式 WorkFlow／MES API 尚未接入；此頁不會讀取或送出資料。</div>
    <template v-else>
      <div class="flow"><b>選取 MO／WIP 庫存</b><span>→</span><b>MES 建單並扣庫</b><span>→</span><b>WorkFlow XMLAdapter</b><span>→</span><b>綁 ERP 單號或保留 State=1 重拋</b></div>
      <div v-if="error || info" class="notice" :class="{ bad: !!error }" role="status">{{ error || info }}</div>

      <template v-if="!showRetry">
        <section class="card setup"><div class="card-heading"><div><h2>入庫條件</h2><p>來源工單限 Release／Partial Release；可累積多張工單的批次。</p></div><span class="setting">ALLOWINV2ERP {{ systemMode }}</span></div>
          <div class="fields">
            <label>ERP Sheet Type<select v-model="form.sheetType" :disabled="systemMode === '0'"><option v-if="systemMode === '0'" value="MES">MES</option><option>WF01:工單完工入庫</option><option>WF02:工單完工入庫（DEV）</option></select></label>
            <label>工單 MO<select v-model="form.moInput"><option value="">選擇已 Release 工單</option><option v-for="mo in moInputOptions" :key="mo">{{ mo }}</option></select></label>
            <button class="primary query" type="button" @click="queryMo">載入工單庫存</button>
            <label>成品庫<input v-model="form.fgdInventory" type="text"></label>
            <label>儲位<input v-model="form.locator" type="text"></label>
            <label>工廠<input v-model="form.factoryNo" readonly type="text"></label>
          </div>
          <div class="dev-settings"><label>系統參數情境<select v-model="systemMode" @change="syncErpMode"><option v-for="mode in modes" :key="mode.value" :value="mode.value">{{ mode.label }}</option></select></label><label class="check"><input v-model="showLossData" type="checkbox"> ShowLossData_FGDStockIN_WF=1</label><label>提交結果<select v-model="submitMode"><option value="success">MES／WorkFlow 成功</option><option value="erp-fail">ERP 失敗，撤回 MES（State=3）</option><option value="erp-timeout">ERP 逾時，建立 State=1 重拋票</option><option value="mes-fail">MES 失敗／ERP Undo 分支</option></select></label></div>
          <p v-if="systemMode === '2' && quantityCheck.messages.length" class="check-warning">ALLOWINV2ERP=2 檢查：{{ quantityCheck.messages.join('；') }}</p>
          <p v-else-if="systemMode === '2' && selectedRows.length" class="check-ok">MO 預計產量與 ERP 淨發料檢查通過。</p>
          <p v-if="systemMode === '2'" class="check-warning">來源程式只在 ALLOWINV2ERP=1 時切出 SheetType 代碼；模式 2 可能把「代碼:名稱」送進 ERP TF001。DEV 以代碼模擬有效 request，正式端需修正並回歸重拋查詢。</p>
          <p class="caption">Source uses `ReferenceType=1` and `ReferenceCase=1`. ALLOWINV2ERP=2 first runs MO output/material issue checks. InInvNum is not editable: WF posts each selected Lot’s GoodQty + ScrapQty; LossQty is display/filter only and is not sent in MOCTG quantity.</p>
        </section>
        <div class="summary"><div><small>查詢 MO</small><strong>{{ queriedMoNos.length }}</strong></div><div><small>可選批次</small><strong>{{ displayedRows.length }}</strong></div><div><small>已選批次</small><strong>{{ selectedRows.length }}</strong></div><div><small>Good + Scrap</small><strong>{{ fmt(selectedTotal) }}</strong><small>依各筆 MES Unit</small></div><div class="summary-spacer"></div><button class="quiet" type="button" :disabled="!displayedRows.length" @click="selectCurrent">全選目前清單</button><button class="quiet" type="button" :disabled="!selectedIds.length" @click="clearSelection">清除勾選</button><button class="danger" type="button" :disabled="busy || !selectedRows.length" @click="submit">{{ busy ? '處理中…' : `建立 MES 入庫（${selectedRows.length}）` }}</button></div>
        <section class="card grid-card"><div class="card-heading"><div><h2>WIP SEMI／SCRAP 庫存候選</h2><p>{{ queriedMoNos.length ? queriedMoNos.join(' · ') : '載入一張工單後顯示；已選列會保留以便多 MO 合併' }}</p></div><span class="filter-note">BookingFlag=0 · {{ showLossData ? '含 Loss-only 列' : '隱藏 Loss-only 列' }}</span></div><div class="table-wrap"><table><thead><tr><th>勾選</th><th>MO</th><th>Lot／Serial</th><th>產品／版次</th><th>ERP 品號</th><th>來源庫</th><th class="num">GoodQty</th><th class="num">ScrapQty</th><th class="num">LossQty</th><th>MES／ERP Unit</th><th>狀態</th></tr></thead><tbody><tr v-for="row in displayedRows" :key="row.id" :class="{ selected: selectedIds.includes(row.id), restored: row.status.includes('失敗') }" @click="toggleRow(row)"><td><input type="checkbox" :checked="selectedIds.includes(row.id)" @click.stop="toggleRow(row)"></td><td>{{ row.moNo }}</td><td class="serial">{{ row.lotNo }}<small v-if="row.lotSerial"> · {{ row.lotSerial }}</small></td><td>{{ row.productNo }} · {{ row.productVersion }}</td><td>{{ row.erpNo || row.productNo }}</td><td>{{ row.sourceInventory }}</td><td class="num">{{ fmt(row.goodQty) }}</td><td class="num">{{ fmt(row.scrapQty) }}</td><td class="num">{{ fmt(row.lossQty) }}</td><td>{{ row.unitNo }} / {{ row.erpUnitNo || row.unitNo }}</td><td><span class="status">{{ row.status }}</span></td></tr><tr v-if="!displayedRows.length"><td colspan="11" class="empty">尚無可入庫 DEV 資料，請載入工單；ERP timeout 的 State=1 項目須走重拋。</td></tr></tbody></table></div></section>
        <section class="card history"><div class="card-heading"><div><h2>本頁模擬處理紀錄</h2><p>State 0=MES 建單／待 ERP；1=ERP 待重拋；2=完成；3=ERP 失敗後撤回。</p></div></div><div class="table-wrap"><table><thead><tr><th>時間</th><th>MES 單號</th><th>ERP 單號</th><th>ALLOWINV2ERP</th><th>結果</th><th>State</th><th>處理</th></tr></thead><tbody><tr v-for="(item, i) in history" :key="item.at + item.mesNo + i"><td>{{ item.at }}</td><td class="serial">{{ item.mesNo }}</td><td>{{ item.erpNo }}</td><td>{{ item.mode }}</td><td>{{ item.outcome }}</td><td>{{ item.state }}</td><td>{{ item.note }}</td></tr><tr v-if="!history.length"><td colspan="7" class="empty">尚無操作紀錄</td></tr></tbody></table></div></section>
      </template>

      <template v-else>
        <section class="card setup"><div class="card-heading"><div><h2>WorkFlow ERP 逾時重拋</h2><p>WinForms 對選取票券重送原始 RequestXML；ERP 成功後綁回單號，失敗則撤回 MES 入庫。</p></div></div><div class="fields retry-fields"><label>Sheet Type<input v-model="retryFilter.sheetType" placeholder="全部單別"></label><label>MES 入庫單<input v-model="retryFilter.mesNo" placeholder="輸入單號片段"></label><label>DEV 重拋結果<select v-model="retryMode"><option value="success">WorkFlow 成功</option><option value="fail">WorkFlow 失敗，撤回 MES</option><option value="timeout">仍逾時，保留票券</option></select></label><button class="primary" type="button" :disabled="busy || !selectedTickets.length" @click="retryTickets">{{ busy ? '重拋中…' : `重拋選取 (${selectedTickets.length})` }}</button></div></section>
        <section class="card grid-card"><div class="card-heading"><div><h2>待重拋票券</h2><p>來源 `wsINV.LoadERPReThrowTicket`；MES State=1、MESTicketType=0。</p></div></div><div class="table-wrap"><table><thead><tr><th>選取</th><th>MES 入庫單</th><th>單別</th><th>建立時間</th><th>ERP 暫存 XML</th><th>明細批次數</th><th>State</th></tr></thead><tbody><tr v-for="ticket in visibleTickets" :key="ticket.id" :class="{ selected: retrySelectedIds.includes(ticket.id) }" @click="toggleTicket(ticket)"><td><input type="checkbox" :checked="retrySelectedIds.includes(ticket.id)" @click.stop="toggleTicket(ticket)"></td><td class="serial">{{ ticket.mesNo }}</td><td>{{ ticket.sheetType }}</td><td>{{ ticket.createdAt }}</td><td><code>{{ ticket.requestXml }}</code></td><td>{{ ticket.rowIds.length }}</td><td><span class="status pending">{{ ticket.state }} · 待 ERP</span></td></tr><tr v-if="!visibleTickets.length"><td colspan="7" class="empty">沒有符合條件的 DEV 重拋票券</td></tr></tbody></table></div><div v-for="ticket in selectedTickets" :key="ticket.id + '-detail'" class="ticket-detail"><strong>{{ ticket.mesNo }} · 入庫明細</strong><div v-for="row in ticketRows.filter(item => ticket.rowIds.includes(item.id))" :key="row.id">{{ row.moNo }} / {{ row.lotNo }} · {{ row.goodQty }} Good / {{ row.scrapQty }} Scrap / {{ row.lossQty }} Loss · {{ row.unitNo }}</div></div></section>
      </template>
      <footer>舊 Core 以 `CollectionSQL` 集中多表 SQL，但交易／commit 範例已註解。此頁只模擬 DEV 狀態，不會呼叫 WorkFlow SOAP、EAI Service 或資料庫。</footer>
    </template>
  </section>
</template>

<style scoped>
.wf-stockin{--ink:#1b2a3c;--muted:#708094;--line:#dce4eb;--navy:#173b5d;color:var(--ink);height:100%;min-height:0;overflow:auto;padding:22px 26px 30px;background:#f7f9fb;font:13px/1.45 "Segoe UI","Microsoft JhengHei",sans-serif}.hero{display:flex;align-items:center;justify-content:space-between;margin-bottom:15px}.eyebrow{margin:0;color:#73869a;font-size:10px;font-weight:750;letter-spacing:.13em}.hero h1{margin:2px 0 0;font-size:23px}.subtitle{margin:0;color:var(--muted);font-size:12px}.hero-actions{display:flex;align-items:center;gap:9px}.dev-pill,.setting,.status{display:inline-block;padding:4px 8px;border-radius:999px;background:#e8f4ed;color:#22724a;font-size:10px;font-weight:700}.setting{background:#edf2f8;color:#385a78}.quiet,.primary,.danger{height:34px;padding:0 12px;border:1px solid var(--line);border-radius:5px;background:white;color:#334d66;cursor:pointer;font:inherit}.primary,.danger{border-color:#173b5d;background:#173b5d;color:white;font-weight:700}.danger{border-color:#973c34;background:#a8473e}.primary:disabled,.danger:disabled,.quiet:disabled{opacity:.45;cursor:not-allowed}.flow{display:flex;align-items:center;gap:10px;padding:10px 14px;margin-bottom:13px;border:1px solid #ccdae7;border-radius:6px;background:#eff5fa;color:#48637c;font-size:11px}.flow b{color:#234663}.card{margin-bottom:13px;border:1px solid var(--line);border-radius:7px;background:white;box-shadow:0 2px 5px #10283a08}.setup{padding:13px 15px}.card-heading{display:flex;align-items:center;justify-content:space-between;padding:11px 14px}.setup .card-heading{padding:0 0 10px}.card-heading h2{margin:0;font-size:14px}.card-heading p{margin:2px 0 0;color:var(--muted);font-size:10px}.fields{display:grid;grid-template-columns:1.1fr 1.4fr auto 1fr 1fr .8fr;align-items:end;gap:10px}.fields label,.dev-settings label{display:flex;flex-direction:column;gap:5px;color:#5e7185;font-size:10px;font-weight:650}.fields input,.fields select,.dev-settings select{height:33px;min-width:0;border:1px solid #cdd8e2;border-radius:4px;padding:0 8px;background:white;color:var(--ink);font:inherit}.fields input[readonly]{background:#f3f6f8;color:#66798e}.query{white-space:nowrap}.dev-settings{display:flex;align-items:end;gap:16px;padding:10px 0 0;margin-top:10px;border-top:1px solid #edf0f3}.dev-settings label{min-width:225px}.dev-settings label.check{min-width:210px;flex-direction:row;align-items:center;height:32px}.dev-settings label.check input{accent-color:#24658c}.caption{margin:9px 0 0;color:#758496;font-size:10px}.summary{display:flex;align-items:center;gap:0;margin:12px 0;background:white;border:1px solid var(--line);border-radius:7px;overflow:hidden}.summary>div{min-width:105px;padding:8px 12px;border-right:1px solid var(--line);display:grid;grid-template-columns:1fr auto;align-items:center;column-gap:7px}.summary small{color:var(--muted);font-size:9px}.summary strong{grid-row:span 2;color:#244f71;font-size:16px}.summary .summary-spacer{flex:1;border:0}.summary .quiet,.summary .danger{margin:6px 7px}.grid-card{overflow:hidden}.filter-note{color:#73869a;font-size:10px}.table-wrap{overflow:auto;border-top:1px solid #e6ebf0}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:10px}th{position:sticky;top:0;background:#f1f5f8;color:#607388;font-size:9px;font-weight:700;text-align:left}th,td{padding:7px 8px;border-bottom:1px solid #ebeff3}tbody tr{cursor:pointer}tbody tr:hover{background:#f8fafc}tbody tr.selected{background:#edf6fb}tbody tr.restored{background:#fff9ee}input[type=checkbox]{accent-color:#1c628c}.num{text-align:right;font-variant-numeric:tabular-nums}.serial{color:#60768b;font-family:Consolas,monospace}.serial small{color:#8997a5}.status{border-radius:4px;background:#edf2f8;color:#536d86;font-size:9px}.status.pending{background:#fff1dc;color:#8a5a1f}.empty{text-align:center;color:#8a98a7;padding:17px}.history .card-heading{padding-bottom:9px}.history table{font-size:10px}.notice{margin:0 0 12px;padding:9px 12px;border:1px solid #bddcc9;border-radius:5px;background:#eef8f1;color:#286440}.notice.bad{border-color:#ebc5c1;background:#fff3f1;color:#9e3831}.retry-fields{grid-template-columns:1fr 1.5fr 1.4fr auto}.ticket-detail{padding:9px 13px;border-top:1px solid #e6ebf0;background:#fbfcfd;color:#617286;font-size:10px}.ticket-detail strong{display:block;margin-bottom:4px;color:#2b4864}.ticket-detail div{padding:2px 0}.ticket-detail code{font-size:9px}.unavailable{padding:16px;border:1px solid #e8d2ac;border-radius:6px;background:#fff8ea;color:#7a5622}footer{padding:2px 2px 8px;color:#8391a0;font-size:10px}@media(max-width:1100px){.wf-stockin{padding:14px}.fields{grid-template-columns:repeat(3,minmax(0,1fr))}.query{white-space:normal}.summary{flex-wrap:wrap}.summary .summary-spacer{display:none}.dev-settings{flex-wrap:wrap}.flow{align-items:flex-start;flex-wrap:wrap}}
.check-warning{margin:9px 0 0;padding:7px 9px;border:1px solid #f0d0a8;border-radius:4px;background:#fff8eb;color:#8b6128;font-size:10px}.check-ok{margin:9px 0 0;padding:7px 9px;border:1px solid #c4ddcc;border-radius:4px;background:#f0f8f2;color:#2f7047;font-size:10px}
</style>
