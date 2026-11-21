<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

type InventoryKind = 'FGDINV' | 'SCRAPINV'
type ERPSource = 'T100' | 'E10ICD' | 'UNKNOWN'
type ResultMode = 'success' | 'erp-fail' | 'mes-fail'
type Line = { lotNo: string; moNo: string; productNo: string; productVersion: string; goodQty: number | null; scrapQty: number | null; lossQty: number | null; unitNo: string }
type Receipt = { id: string; invNo: string; erpNo: string; source: ERPSource; kind: InventoryKind; inventoryNo: string; inputDate: string; state: number; orgState: number; cancelUser: string; cancelDate: string; lines: Line[] }
type HistoryRow = { time: string; invNo: string; erpNo: string; kind: InventoryKind; requestType: string; erp: string; mes: string; state: string; note: string }

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const filter = reactive({ keyword: '' })
const receipts = ref<Receipt[]>(dev.value ? [
  { id: 't100-c1', invNo: 'DEV-T100-FGDIN-62001', erpNo: 'DEV-T100-ERP-82001', source: 'T100', kind: 'FGDINV', inventoryNo: 'DEV-FGD-01', inputDate: '2026-11-19 16:32', state: 2, orgState: 2, cancelUser: '', cancelDate: '', lines: [
    { lotNo: 'DEV-LOT-620-001', moNo: 'DEV-MO-620-01', productNo: 'DEV-PRD-620', productVersion: 'A1', goodQty: 120, scrapQty: null, lossQty: null, unitNo: 'PCS' },
    { lotNo: 'DEV-LOT-620-002', moNo: 'DEV-MO-620-01', productNo: 'DEV-PRD-620', productVersion: 'A1', goodQty: 48, scrapQty: null, lossQty: null, unitNo: 'PCS' },
  ] },
  { id: 't100-c2', invNo: 'DEV-T100-SCRIN-62002', erpNo: 'DEV-T100-SCR-82002', source: 'T100', kind: 'SCRAPINV', inventoryNo: 'DEV-SCR-01', inputDate: '2026-11-20 11:04', state: 2, orgState: 2, cancelUser: '', cancelDate: '', lines: [
    { lotNo: 'DEV-LOT-620-003', moNo: 'DEV-MO-620-02', productNo: 'DEV-PRD-621', productVersion: 'B2', goodQty: null, scrapQty: 3, lossQty: 1, unitNo: 'PCS' },
  ] },
  { id: 't100-c3', invNo: 'DEV-NO-ERP-62003', erpNo: '', source: 'T100', kind: 'FGDINV', inventoryNo: 'DEV-FGD-01', inputDate: '2026-11-20 15:26', state: 2, orgState: 2, cancelUser: '', cancelDate: '', lines: [
    { lotNo: 'DEV-LOT-620-004', moNo: 'DEV-MO-620-03', productNo: 'DEV-PRD-622', productVersion: 'A0', goodQty: 16, scrapQty: null, lossQty: null, unitNo: 'PCS' },
  ] },
  { id: 'foreign-c4', invNo: 'DEV-OTHERERP-62004', erpNo: 'DEV-ERP-OTHER-82004', source: 'E10ICD', kind: 'FGDINV', inventoryNo: 'DEV-FGD-01', inputDate: '2026-11-21 07:18', state: 2, orgState: 2, cancelUser: '', cancelDate: '', lines: [
    { lotNo: 'DEV-LOT-620-005', moNo: 'DEV-MO-620-04', productNo: 'DEV-PRD-623', productVersion: 'C1', goodQty: 8, scrapQty: null, lossQty: null, unitNo: 'PCS' },
  ] },
  { id: 'pending-c5', invNo: 'DEV-RECOVERY-62005', erpNo: 'DEV-T100-ERP-82005', source: 'T100', kind: 'FGDINV', inventoryNo: 'DEV-FGD-02', inputDate: '2026-11-21 08:02', state: -2, orgState: 1, cancelUser: '', cancelDate: '', lines: [
    { lotNo: 'DEV-LOT-620-006', moNo: 'DEV-MO-620-05', productNo: 'DEV-PRD-624', productVersion: 'A2', goodQty: 6, scrapQty: null, lossQty: null, unitNo: 'PCS' },
  ] },
] : [])
const queried = ref(false)
const selectedId = ref('')
const resultMode = ref<ResultMode>('success')
const error = ref('')
const info = ref('')
const working = ref(false)
const history = ref<HistoryRow[]>([])
const eligibleRows = computed(() => receipts.value.filter(row => row.state === 2 && (!filter.keyword.trim() || `${row.invNo} ${row.erpNo} ${row.lines.map(line => `${line.moNo} ${line.lotNo}`).join(' ')}`.toLowerCase().includes(filter.keyword.trim().toLowerCase()))))
const selected = computed(() => receipts.value.find(row => row.id === selectedId.value) ?? null)
const totalQty = computed(() => selected.value?.lines.reduce((sum, line) => sum + (line.goodQty ?? line.scrapQty ?? 0) + (line.lossQty ?? 0), 0) ?? 0)
const kindName = (kind: InventoryKind) => kind === 'FGDINV' ? '良品入庫' : '不良品入庫'
const requestType = (kind: InventoryKind) => kind === 'FGDINV' ? '0 · stockin.data.process' : '1 · wo.scrap.data.process'
const stateName = (state: number) => ({ 2: '已確認，可取消', 1: '取消處理中', '-2': 'ERP 已取消，MES 尚未完成', '-1': '已取消' } as Record<string, string>)[String(state)] ?? `State ${state}`
const fmt = (qty: number) => new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 6 }).format(qty)
const nowText = () => new Date().toISOString().slice(0, 19).replace('T', ' ')

function query(): void {
  error.value = ''
  info.value = ''
  selectedId.value = ''
  if (!dev.value) { error.value = '正式 T100／MES 取消 API 尚未接入；此頁不會讀取或送出資料。'; return }
  queried.value = true
  info.value = `已載入 ${eligibleRows.value.length} 張 MES State=2 的 DEV 入庫單。注意：原查詢沒有 IsERP 篩選，列表可能包含其他 ERP 來源。`
}
function choose(row: Receipt): void { selectedId.value = row.id; error.value = ''; info.value = '' }
function cancelReceipt(): void {
  error.value = ''
  info.value = ''
  if (!dev.value || working.value || !selected.value) return
  const row = selected.value
  if (row.state !== 2) { error.value = '只有 MES State=2 的入庫單可取消。'; return }
  if (!row.erpNo) { error.value = 'ERPNo 為空；來源程式禁止取消。'; return }
  if (row.source !== 'T100') { error.value = `來源 ERP 是 ${row.source}；原始 T100 清單沒有 ERP 篩選，但 DEV 會阻擋跨 ERP 取消。`; return }
  working.value = true
  const timestamp = nowText()
  const savedQty = totalQty.value
  const typeLabel = requestType(row.kind)
  row.orgState = 2
  row.state = 1
  if (resultMode.value === 'erp-fail') {
    row.orgState = 1
    row.state = 2
    history.value.unshift({ time: timestamp, invNo: row.invNo, erpNo: row.erpNo, kind: row.kind, requestType: typeLabel, erp: '失敗', mes: '未執行', state: '2：可再取消', note: '來源以 OrgState=1 還原 State=2' })
    selectedId.value = ''
    error.value = `T100 取消失敗（RequestType=${typeLabel}）；MES 未回復庫存，State 已還原 2。`
  } else if (resultMode.value === 'mes-fail') {
    row.orgState = 1
    row.state = -2
    history.value.unshift({ time: timestamp, invNo: row.invNo, erpNo: row.erpNo, kind: row.kind, requestType: typeLabel, erp: '成功', mes: '失敗', state: '-2：待恢復', note: 'ERP 已取消；MES 未完成，需對帳／恢復' })
    selectedId.value = ''
    info.value = `T100 ERP 已取消，但 MES 庫存回復失敗；單據保留 State=-2，不會再次顯示在取消清單。`
  } else {
    row.orgState = -2
    row.state = -1
    row.cancelUser = 'DEV-CURRENT'
    row.cancelDate = timestamp
    history.value.unshift({ time: timestamp, invNo: row.invNo, erpNo: row.erpNo, kind: row.kind, requestType: typeLabel, erp: '成功', mes: '成功', state: '-1：已取消', note: '庫存／Lot Qty／元件／動態 Bin 已模擬回復' })
    selectedId.value = ''
    info.value = `DEV 模擬 ${kindName(row.kind)}取消完成：${row.invNo} → T100 ${row.erpNo} → MES 庫存回復 ${fmt(savedQty)} PCS。`
  }
  working.value = false
}
</script>

<template>
  <section class="t100-cancel">
    <header class="head"><div><p class="eyebrow">WIP · ERP INTEGRATION · WP620</p><h1>取消入庫 T100 ERP</h1><p class="subtitle">FGD Stock In Cancel to T100</p></div><div class="head-actions"><span v-if="dev" class="dev-pill">DEV 假資料</span><button class="quiet" type="button" @click="emit('close')">關閉</button></div></header>
    <div v-if="!dev" class="unavailable">正式 T100／MES 取消服務尚未接入；此頁不會連線或送出資料。</div>
    <div v-else class="flow"><b>MES State=2 檢核</b><span>→</span><b>鎖定 State=1</b><span>→</span><b>T100 取消 ERP 單</b><span>→</span><b>MES 回復庫存</b><span>來源查詢未篩 ERP；DEV 阻擋非 T100 樣本</span></div>

    <section class="card query"><div class="query-fields"><label>入庫單／ERP單／MO／Lot<input v-model="filter.keyword" :disabled="!dev" type="search" placeholder="輸入部分編號"></label><button class="primary" type="button" :disabled="!dev" @click="query">查詢 State=2 單據</button><label>DEV 結果情境<select v-model="resultMode" :disabled="!dev"><option value="success">T100、MES 取消成功</option><option value="erp-fail">T100 取消失敗，還原 State=2</option><option value="mes-fail">ERP 成功、MES 回復失敗（State=-2）</option></select></label></div><p>原始 `LoadFGDInNoUnionSCRInNo(State=2)` 沒傳 `IsERP=true`；列表會混入有 ERP No 的其它來源單據。正式端需先確認 ERP source 才能呼叫 T100。</p></section>
    <div class="summary"><div><small>State=2 單據</small><strong>{{ queried ? eligibleRows.length : '—' }}</strong></div><div><small>目前選取</small><strong>{{ selected ? 1 : 0 }}</strong></div><div><small>種類／明細列</small><strong>{{ selected ? `${kindName(selected.kind)} / ${selected.lines.length}` : '—' }}</strong></div><div><small>取消數量</small><strong>{{ selected ? fmt(totalQty) : '0' }}</strong><small>PCS · 整張單據</small></div></div>
    <div v-if="error || info" class="notice" :class="{ bad: !!error }" role="status">{{ error || info }}</div>

    <section class="card"><div class="card-title"><div><h2>MES 已確認入庫單</h2><p>搜尋清單包含 ERPNo 空白與其它 ERP 來源；Confirm 會重查 State=2／ERPNo。</p></div></div><div class="table-scroll"><table><thead><tr><th>選取</th><th>MES 入庫單</th><th>ERP 來源</th><th>ERP 入庫單</th><th>種類</th><th>庫別</th><th>InputDate</th><th>State</th></tr></thead><tbody><tr v-for="row in (queried ? eligibleRows : [])" :key="row.id" :class="{ active: selectedId === row.id, foreign: row.source !== 'T100' }" @click="choose(row)"><td><button class="link" type="button" @click.stop="choose(row)">{{ selectedId === row.id ? '已選取' : '選取' }}</button></td><td class="serial">{{ row.invNo }}</td><td><span class="origin" :class="{ warning: row.source !== 'T100' }">{{ row.source }}</span></td><td class="lot">{{ row.erpNo || '—' }}</td><td>{{ kindName(row.kind) }}</td><td>{{ row.inventoryNo }}</td><td>{{ row.inputDate }}</td><td><span class="state">{{ stateName(row.state) }}</span></td></tr><tr v-if="queried && !eligibleRows.length"><td colspan="8" class="empty">找不到 State=2 入庫單</td></tr><tr v-if="!queried"><td colspan="8" class="empty">先查詢 MES State=2 的 DEV 入庫單</td></tr></tbody></table></div></section>

    <section v-if="selected" class="card details"><div class="card-title"><div><h2>{{ kindName(selected.kind) }}明細 · {{ selected.invNo }}</h2><p>{{ selected.erpNo || '無 ERP No' }} · {{ selected.inventoryNo }} · 整張入庫單取消</p></div><button class="danger" type="button" :disabled="!dev || working" @click="cancelReceipt">{{ working ? '取消中…' : '取消整張單據' }}</button></div><div class="table-scroll"><table><thead><tr><th>LotNo</th><th>MO</th><th>Product／Version</th><th class="num">GoodQty</th><th class="num">ScrapQty</th><th class="num">LossQty</th><th>Unit</th></tr></thead><tbody><tr v-for="line in selected.lines" :key="line.lotNo"><td class="lot">{{ line.lotNo }}</td><td>{{ line.moNo }}</td><td>{{ line.productNo }} · {{ line.productVersion }}</td><td class="num">{{ line.goodQty === null ? '—' : fmt(line.goodQty) }}</td><td class="num">{{ line.scrapQty === null ? '—' : fmt(line.scrapQty) }}</td><td class="num">{{ line.lossQty === null ? '—' : fmt(line.lossQty) }}</td><td>{{ line.unitNo }}</td></tr></tbody></table></div><div class="state-foot">FGDINV → T100 RequestType=0；SCRAPINV → RequestType=1。ERP code 必須為 0 後才呼叫 MES `ExeINVFGDINCancel`。</div></section>

    <section class="card history"><div class="card-title"><div><h2>DEV 取消紀錄</h2><p>State=-2 代表 T100 已取消但 MES 尚未完成，需對帳／恢復。</p></div></div><div class="table-scroll"><table><thead><tr><th>時間</th><th>MES 入庫單</th><th>ERP 單</th><th>種類／RequestType</th><th>ERP 取消</th><th>MES 回復</th><th>最後 State</th><th>處理</th></tr></thead><tbody><tr v-for="(item, index) in history" :key="item.time + item.invNo + index"><td>{{ item.time }}</td><td class="serial">{{ item.invNo }}</td><td class="lot">{{ item.erpNo }}</td><td>{{ kindName(item.kind) }} · {{ item.requestType }}</td><td>{{ item.erp }}</td><td>{{ item.mes }}</td><td>{{ item.state }}</td><td>{{ item.note }}</td></tr><tr v-if="!history.length"><td colspan="8" class="empty">尚無本頁操作</td></tr></tbody></table></div></section>
    <footer>`UpdINVInfo` 每次覆寫 `OrgState`；DEV 顯示舊狀態流程，正式 API 應保留最初狀態及取消操作紀錄。</footer>
  </section>
</template>

<style scoped>
.t100-cancel{--ink:#1a293b;--muted:#6d7c8e;--line:#dce4eb;--navy:#173b5d;color:var(--ink);height:100%;min-height:0;overflow:auto;padding:22px 26px 30px;background:#f7f9fb;font:13px/1.45 "Segoe UI","Microsoft JhengHei",sans-serif}.head{display:flex;align-items:center;justify-content:space-between;margin-bottom:16px}.eyebrow{margin:0;color:#73869a;font-size:10px;font-weight:750;letter-spacing:.13em}.head h1{margin:2px 0 0;font-size:23px}.subtitle{margin:0;color:var(--muted);font-size:12px}.head-actions{display:flex;align-items:center;gap:12px}.dev-pill{padding:5px 10px;border-radius:999px;background:#e8f4ed;color:#22724a;font-weight:700}.quiet,.link{border:1px solid var(--line);background:white;color:#334d66;border-radius:5px;padding:7px 12px;cursor:pointer}.primary,.danger{height:34px;padding:0 15px;border:1px solid var(--navy);border-radius:4px;background:var(--navy);color:white;font:inherit;font-weight:700;cursor:pointer}.danger{border-color:#973c34;background:#a8473e}.primary:disabled,.danger:disabled,.quiet:disabled,.link:disabled{opacity:.45;cursor:not-allowed}.flow{display:flex;align-items:center;gap:11px;padding:11px 15px;margin-bottom:14px;border:1px solid #ead3ca;border-radius:6px;background:#fff4ee;color:#7b5345}.flow b{color:#75412d}.flow span:last-child{margin-left:auto;color:#95614c;font-size:10px}.card{margin-bottom:13px;border:1px solid var(--line);border-radius:7px;background:white;box-shadow:0 2px 5px #10283a08}.query{padding:13px 15px}.query-fields{display:flex;align-items:end;gap:12px}.query-fields label,.summary label{display:flex;flex-direction:column;gap:5px;color:#5e7185;font-size:11px;font-weight:650}.query-fields input,.query-fields select{height:34px;min-width:210px;border:1px solid #cdd8e2;border-radius:4px;padding:0 9px;background:white;color:var(--ink);font:inherit}.query>p{margin:8px 0 0;color:#96634e;font-size:10px}.summary{display:flex;align-items:center;gap:0;margin:12px 0;background:white;border:1px solid var(--line);border-radius:7px;overflow:hidden}.summary>div{min-width:120px;padding:9px 14px;border-right:1px solid var(--line);display:grid;grid-template-columns:1fr auto;align-items:center;column-gap:8px}.summary small{color:var(--muted);font-size:10px}.summary strong{grid-row:span 2;color:#8b4b37;font-size:18px}.summary label{margin-left:auto;padding:8px 12px;flex-direction:row;align-items:center}.summary select{min-width:205px;height:30px}.notice{margin:0 0 12px;padding:9px 12px;border:1px solid #bddcc9;border-radius:5px;background:#eef8f1;color:#286440}.notice.bad{border-color:#ebc5c1;background:#fff3f1;color:#9e3831}.card-title{display:flex;align-items:center;justify-content:space-between;padding:12px 15px}.card-title h2{margin:0;font-size:14px}.card-title p{margin:2px 0 0;color:var(--muted);font-size:10px}.table-scroll{overflow:auto;border-top:1px solid #e6ebf0}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:11px}th{position:sticky;top:0;background:#f1f5f8;color:#607388;font-size:10px;font-weight:700;text-align:left}th,td{padding:8px 9px;border-bottom:1px solid #ebeff3}tbody tr{cursor:pointer}tbody tr:hover{background:#f8fafc}tbody tr.active{background:#fff3ed}tbody tr.foreign{background:#fffaf3}.num{text-align:right;font-variant-numeric:tabular-nums}.serial{color:#60768b;font-family:Consolas,monospace}.lot{color:#8b4b37;font-weight:650}.origin,.state{display:inline-block;padding:3px 6px;border-radius:4px;background:#e9f3ec;color:#286440;font-size:9px;font-weight:700}.origin.warning{background:#fff0df;color:#965618}.state{background:#e9f3ec;color:#286440}.empty{text-align:center;color:#8a98a7;padding:18px}.state-foot{padding:8px 12px;color:#728397;font-size:10px;background:#fafcfd}.history .card-title{padding-bottom:10px}footer{padding:2px 2px 8px;color:#8391a0;font-size:10px}.unavailable{padding:16px;border:1px solid #e8d2ac;border-radius:6px;background:#fff8ea;color:#7a5622}@media(max-width:1000px){.t100-cancel{padding:15px}.query-fields{flex-wrap:wrap}.summary{flex-wrap:wrap}.summary label{margin-left:0}.flow{align-items:flex-start;flex-wrap:wrap}.flow span:last-child{width:100%;margin:0}}
</style>
