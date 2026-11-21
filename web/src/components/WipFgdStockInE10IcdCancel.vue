<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

type InventoryKind = 'FGDINV' | 'SCRAPINV'
type ResultMode = 'success' | 'erp-fail' | 'mes-fail'
type GoodLine = { lotNo: string; productNo: string; qty: number; unitNo: string; fromInventoryNo: string; bins: Record<string, number>; components: { componentNo: string; goodQty: number; scrapQty: number; unitNo: string }[] }
type ScrapLine = { lotNo: string; lotSerial: string; source: 0 | 1 | 2; qty: number; unitNo: string; fromInventoryNo: string; reason: string; bins: Record<string, number>; components: { componentNo: string; goodQty: number; scrapQty: number; unitNo: string }[] }
type Receipt = { id: string; invNo: string; erpNo: string; kind: InventoryKind; inventoryNo: string; moNo: string; productNo: string; productVersion: string; inputDate: string; state: number; orgState: number; cancelUser: string; cancelDate: string; lines: GoodLine[] | ScrapLine[] }
type HistoryRow = { time: string; invNo: string; erpNo: string; kind: InventoryKind; erp: string; mes: string; state: string; note: string }

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const search = reactive({ keyword: '', kind: 'ALL' })
const resultMode = ref<ResultMode>('success')
const queried = ref(false)
const selectedId = ref('')
const error = ref('')
const info = ref('')
const working = ref(false)
const receipts = ref<Receipt[]>(dev.value ? [
  { id: 'r1', invNo: 'DEV-FGDIN-26001', erpNo: 'DEV-ERP-FGD-90101', kind: 'FGDINV', inventoryNo: 'DEV-FGD-01', moNo: 'DEV-MO-623-01', productNo: 'DEV-PRD-623', productVersion: 'A1', inputDate: '2026-11-20 15:18', state: 2, orgState: 2, cancelUser: '', cancelDate: '', lines: [
    { lotNo: 'DEV-LOT-623-001', productNo: 'DEV-PRD-623', qty: 24, unitNo: 'PCS', fromInventoryNo: 'DEV-WIP-01', bins: { BIN01: 18, BIN02: 6 }, components: [{ componentNo: 'DEV-COMP-623-01', goodQty: 2, scrapQty: 0, unitNo: 'PCS' }] },
    { lotNo: 'DEV-LOT-623-002', productNo: 'DEV-PRD-623', qty: 12, unitNo: 'PCS', fromInventoryNo: 'DEV-WIP-01', bins: { BIN01: 8, BIN02: 4 }, components: [] },
  ] },
  { id: 'r2', invNo: 'DEV-SCRIN-26002', erpNo: 'DEV-ERP-SCR-90202', kind: 'SCRAPINV', inventoryNo: 'DEV-SCR-01', moNo: 'DEV-MO-623-02', productNo: 'DEV-PRD-624', productVersion: 'B2', inputDate: '2026-11-21 08:42', state: 2, orgState: 2, cancelUser: '', cancelDate: '', lines: [
    { lotNo: 'DEV-LOT-623-003', lotSerial: 'DEV-SERIAL-623-003', source: 1, qty: 2, unitNo: 'PCS', fromInventoryNo: 'DEV-WIP-SCR-01', reason: '元件電性不良', bins: {}, components: [{ componentNo: 'DEV-COMP-623-02', goodQty: 0, scrapQty: 2, unitNo: 'PCS' }] },
    { lotNo: 'DEV-LOT-623-004', lotSerial: 'DEV-SERIAL-623-004', source: 2, qty: 5, unitNo: 'PCS', fromInventoryNo: 'DEV-WIP-SCR-01', reason: '測試站 Fail Bin', bins: { BIN01: 3, BIN02: 2, BIN99: 0 }, components: [] },
  ] },
  { id: 'r3', invNo: 'DEV-FGDIN-26003', erpNo: '', kind: 'FGDINV', inventoryNo: 'DEV-FGD-01', moNo: 'DEV-MO-623-03', productNo: 'DEV-PRD-625', productVersion: 'A0', inputDate: '2026-11-21 09:12', state: 2, orgState: 2, cancelUser: '', cancelDate: '', lines: [{ lotNo: 'DEV-LOT-623-005', productNo: 'DEV-PRD-625', qty: 8, unitNo: 'PCS', fromInventoryNo: 'DEV-WIP-02', bins: {}, components: [] }] },
] : [])
const history = ref<HistoryRow[]>([])
const eligibleRows = computed(() => receipts.value.filter(row => row.state === 2 && !!row.erpNo && (search.kind === 'ALL' || row.kind === search.kind) && (!search.keyword.trim() || `${row.invNo} ${row.erpNo} ${row.moNo} ${row.productNo}`.toLowerCase().includes(search.keyword.trim().toLowerCase()))))
const selected = computed(() => receipts.value.find(row => row.id === selectedId.value) ?? null)
const selectedQty = computed(() => selected.value?.lines.reduce((total, line) => total + line.qty, 0) ?? 0)
const selectedLines = computed(() => selected.value?.lines ?? [])
const binColumns = computed(() => [...new Set(selectedLines.value.flatMap(line => Object.keys(line.bins)))])
const fmt = (value: number) => new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 6 }).format(value)
const kindName = (kind: InventoryKind) => kind === 'FGDINV' ? '良品入庫' : '不良品入庫'
const stateName = (state: number) => ({ 2: '已確認，可取消', 1: '取消處理中（來源還原 OrgState 有覆寫）', '-2': 'ERP 已取消，MES 尚未完成', '-1': '已取消' } as Record<string, string>)[String(state)] ?? `State ${state}`
const nowText = () => new Date().toISOString().slice(0, 19).replace('T', ' ')

function query(): void {
  error.value = ''
  info.value = ''
  selectedId.value = ''
  if (!dev.value) { error.value = '正式取消入庫 API 尚未接入；此頁不會讀取或送出資料。'; return }
  queried.value = true
  info.value = `已載入 ${eligibleRows.value.length} 張狀態為已確認且有 ERP 單號的 DEV 入庫單。無 ERP 單號的單據不在可取消清單。`
}

function choose(row: Receipt): void {
  selectedId.value = row.id
  error.value = ''
  info.value = ''
}

function cancelReceipt(): void {
  error.value = ''
  info.value = ''
  if (!dev.value || working.value || !selected.value) return
  const row = selected.value
  if (row.state !== 2 || !row.erpNo) { error.value = '來源檢核失敗：只有 MES State=2 且存在 ERP 單號的入庫單可取消。'; return }
  working.value = true
  const timestamp = nowText()
  const cancelQty = selectedQty.value
  const oldState = row.state
  row.orgState = oldState
  row.state = 1
  const requestType = row.kind === 'FGDINV' ? '0（stockin.data.process）' : '1（wo.scrap.data.process）'

  if (resultMode.value === 'erp-fail') {
    row.orgState = 1
    row.state = 2
    history.value.unshift({ time: timestamp, invNo: row.invNo, erpNo: row.erpNo, kind: row.kind, erp: '失敗', mes: '未執行', state: '2：可再取消', note: '來源程式將 OrgState 更新為 1 後還原 State=2' })
    selectedId.value = ''
    error.value = `DEV 模擬 ERP 取消失敗（RequestType=${requestType}）；MES 未回復庫存，單據 State 已還原為 2。`
  } else if (resultMode.value === 'mes-fail') {
    row.orgState = 1
    row.state = -2
    history.value.unshift({ time: timestamp, invNo: row.invNo, erpNo: row.erpNo, kind: row.kind, erp: '成功', mes: '失敗', state: '-2：待恢復', note: 'ERP 已取消；需家用端核對 ERP/MES 後人工恢復' })
    selectedId.value = ''
    info.value = `ERP 已取消（RequestType=${requestType}），但 DEV 模擬 MES 庫存回復失敗；單據保留 State=-2，不會再出現在可取消清單。`
  } else {
    row.orgState = -2
    row.state = -1
    row.cancelUser = 'DEV-CURRENT'
    row.cancelDate = timestamp
    history.value.unshift({ time: timestamp, invNo: row.invNo, erpNo: row.erpNo, kind: row.kind, erp: '成功', mes: '成功', state: '-1：已取消', note: '庫存／Lot 數量／動態 Bin 已模擬回復' })
    selectedId.value = ''
    info.value = `DEV 模擬 ${kindName(row.kind)}取消完成：${row.invNo} → ERP ${row.erpNo} → MES 庫存回復；共 ${fmt(cancelQty)} PCS。`
  }
  working.value = false
}
</script>

<template>
  <section class="cancel-e10">
    <header class="head"><div><p class="eyebrow">WIP · ERP INTEGRATION · WP623</p><h1>取消入庫 E10ICD ERP</h1><p class="subtitle">FGD Stock In Cancel to E10ICD</p></div><div class="head-actions"><span v-if="dev" class="dev-pill">DEV 假資料</span><button class="quiet" type="button" @click="emit('close')">關閉</button></div></header>
    <div v-if="!dev" class="unavailable">正式 E10ICD／MES 取消服務尚未接入；此頁不會連線或送出資料。</div>
    <div v-else class="flow"><b>MES State=2 + ERP No 檢核</b><span>→</span><b>鎖定 State=1</b><span>→</span><b>E10ICD 取消</b><span>→</span><b>MES 庫存回復</b></div>

    <section class="card query-card"><div class="query-fields"><label>入庫單／ERP單／工單／品號<input v-model="search.keyword" :disabled="!dev" type="search" placeholder="輸入部分編號"></label><label>入庫種類<select v-model="search.kind" :disabled="!dev"><option value="ALL">全部</option><option value="FGDINV">良品入庫</option><option value="SCRAPINV">不良品入庫</option></select></label><button class="primary" type="button" :disabled="!dev" @click="query">查詢可取消單據</button></div><p>只列已確認（State=2）且有 ERP 單號的良品／不良品入庫；不支援單筆 Lot／Serial 部分取消。</p></section>

    <div class="summary"><div><small>可取消單據</small><strong>{{ queried ? eligibleRows.length : '—' }}</strong></div><div><small>目前選取</small><strong>{{ selected ? 1 : 0 }}</strong></div><div><small>入庫種類／明細數</small><strong>{{ selected ? `${kindName(selected.kind)} / ${selected.lines.length}` : '—' }}</strong></div><div><small>回復數量</small><strong>{{ selected ? fmt(selectedQty) : '0' }}</strong><small>PCS · 依整張入庫單</small></div><label>DEV 結果情境<select v-model="resultMode" :disabled="!dev"><option value="success">ERP、MES 取消成功</option><option value="erp-fail">ERP 取消失敗，還原可取消狀態</option><option value="mes-fail">ERP 成功、MES 回復失敗（State=-2）</option></select></label></div>
    <div v-if="error || info" class="notice" :class="{ bad: !!error }" role="status">{{ error || info }}</div>

    <section class="card"><div class="card-title"><div><h2>已確認 ERP 入庫單</h2><p>來源查詢 `LoadFGDInNoUnionSCRInNo(State=2, IsERP=true)`；良品、不良品使用不同 ERP RequestType。</p></div></div><div class="table-scroll"><table><thead><tr><th>選取</th><th>種類</th><th>MES 入庫單</th><th>ERP 入庫單</th><th>工單</th><th>品號／版本</th><th>目的庫</th><th>入庫時間</th><th>State</th></tr></thead><tbody><tr v-for="row in (queried ? eligibleRows : [])" :key="row.id" :class="{ active: selectedId === row.id }" @click="choose(row)"><td><button class="link" type="button" @click.stop="choose(row)">{{ selectedId === row.id ? '已選取' : '選取' }}</button></td><td><span class="kind" :class="row.kind.toLowerCase()">{{ kindName(row.kind) }}</span></td><td class="serial">{{ row.invNo }}</td><td class="lot">{{ row.erpNo }}</td><td>{{ row.moNo }}</td><td>{{ row.productNo }} · {{ row.productVersion }}</td><td>{{ row.inventoryNo }}</td><td>{{ row.inputDate }}</td><td><span class="state">{{ stateName(row.state) }}</span></td></tr><tr v-if="queried && !eligibleRows.length"><td colspan="9" class="empty">找不到符合條件且可取消的入庫單</td></tr><tr v-if="!queried"><td colspan="9" class="empty">先查詢已確認且有 ERP 單號的 DEV 入庫單</td></tr></tbody></table></div></section>

    <section v-if="selected" class="card details-card"><div class="card-title"><div><h2>{{ selected.kind === 'FGDINV' ? '良品入庫明細' : '不良品入庫明細' }} · {{ selected.invNo }}</h2><p>ERP No {{ selected.erpNo }} · MO {{ selected.moNo }} · {{ selected.inventoryNo }} · 單據整張取消</p></div><button class="danger" type="button" :disabled="!dev || working" @click="cancelReceipt">{{ working ? '取消處理中…' : '取消整張入庫單' }}</button></div><div class="table-scroll"><table v-if="selected.kind === 'FGDINV'"><thead><tr><th>LotNo</th><th>品號</th><th class="num">Qty</th><th>Unit</th><th>來源庫</th><th>Component</th><th>動態 Bin</th></tr></thead><tbody><tr v-for="line in selectedLines as GoodLine[]" :key="line.lotNo"><td class="lot">{{ line.lotNo }}</td><td>{{ line.productNo }}</td><td class="num">{{ fmt(line.qty) }}</td><td>{{ line.unitNo }}</td><td>{{ line.fromInventoryNo }}</td><td>{{ line.components.map(item => `${item.componentNo} (${fmt(item.goodQty)}/${fmt(item.scrapQty)})`).join('、') || '—' }}</td><td>{{ Object.entries(line.bins).map(([bin, qty]) => `${bin}:${fmt(qty)}`).join(' · ') || '—' }}</td></tr></tbody></table><table v-else><thead><tr><th>Source</th><th>LotNo</th><th>LotSerial</th><th>不良原因</th><th class="num">Qty</th><th>Unit</th><th>來源庫</th><th>Component／Bin</th></tr></thead><tbody><tr v-for="line in selectedLines as ScrapLine[]" :key="line.lotSerial"><td><span class="kind scrapinv">{{ line.source }} · {{ ['Lot Scrap', 'Component Scrap', 'Fail Bin'][line.source] }}</span></td><td class="lot">{{ line.lotNo }}</td><td class="serial">{{ line.lotSerial }}</td><td>{{ line.reason }}</td><td class="num">{{ fmt(line.qty) }}</td><td>{{ line.unitNo }}</td><td>{{ line.fromInventoryNo }}</td><td>{{ line.source === 1 ? line.components.map(item => `${item.componentNo} (${fmt(item.scrapQty)})`).join('、') : line.source === 2 ? Object.entries(line.bins).map(([bin, qty]) => `${bin}:${fmt(qty)}`).join(' · ') : '—' }}</td></tr></tbody></table></div><div class="state-foot">取消結果會將 FGD／SCRAP Inventory 數量減回、還原線邊倉／Component／Fail Bin、Lot CurQty／SysQty 與動態 Bin，並寫入取消人員及日期。DEV 不會更新真實資料。</div></section>

    <section class="card history"><div class="card-title"><div><h2>DEV 取消紀錄</h2><p>只記憶體模擬；State=-2 代表 ERP 已取消但 MES 尚未完成，需追查後恢復。</p></div></div><div class="table-scroll"><table><thead><tr><th>時間</th><th>MES 入庫單</th><th>ERP 單</th><th>種類</th><th>ERP 取消</th><th>MES 回復</th><th>最後 State</th><th>處理記錄</th></tr></thead><tbody><tr v-for="(item, index) in history" :key="item.time + item.invNo + index"><td>{{ item.time }}</td><td class="serial">{{ item.invNo }}</td><td class="lot">{{ item.erpNo }}</td><td>{{ kindName(item.kind) }}</td><td>{{ item.erp }}</td><td>{{ item.mes }}</td><td>{{ item.state }}</td><td>{{ item.note }}</td></tr><tr v-if="!history.length"><td colspan="8" class="empty">尚無本頁操作</td></tr></tbody></table></div></section>
    <footer>ERP RequestType：良品 `0` → `stockin.data.process`；不良品 `1` → `wo.scrap.data.process`。正式 ERP code 必須為 `0` 才視為成功。</footer>
  </section>
</template>

<style scoped>
.cancel-e10{--ink:#1a293b;--muted:#6d7c8e;--line:#dce4eb;--navy:#173b5d;color:var(--ink);height:100%;min-height:0;overflow:auto;padding:22px 26px 30px;background:#f7f9fb;font:13px/1.45 "Segoe UI","Microsoft JhengHei",sans-serif}.head{display:flex;align-items:center;justify-content:space-between;margin-bottom:16px}.eyebrow{margin:0;color:#73869a;font-size:10px;font-weight:750;letter-spacing:.13em}.head h1{margin:2px 0 0;font-size:23px}.subtitle{margin:0;color:var(--muted);font-size:12px}.head-actions{display:flex;align-items:center;gap:12px}.dev-pill{padding:5px 10px;border-radius:999px;background:#e8f4ed;color:#22724a;font-weight:700}.quiet,.link{border:1px solid var(--line);background:white;color:#334d66;border-radius:5px;padding:7px 12px;cursor:pointer}.primary,.danger{height:34px;padding:0 15px;border:1px solid var(--navy);border-radius:4px;background:var(--navy);color:white;font:inherit;font-weight:700;cursor:pointer}.danger{border-color:#973c34;background:#a8473e}.primary:disabled,.danger:disabled,.quiet:disabled,.link:disabled{opacity:.45;cursor:not-allowed}.flow{display:flex;align-items:center;gap:12px;padding:11px 15px;margin-bottom:14px;border:1px solid #ead3ca;border-radius:6px;background:#fff4ee;color:#7b5345}.flow b{color:#75412d}.query-card{padding:13px 15px}.query-card p{margin:8px 0 0;color:var(--muted);font-size:10px}.query-fields{display:flex;align-items:end;gap:12px}.query-fields label,.summary label{display:flex;flex-direction:column;gap:5px;color:#5e7185;font-size:11px;font-weight:650}.query-fields input,.query-fields select,.summary select{height:34px;min-width:160px;border:1px solid #cdd8e2;border-radius:4px;padding:0 9px;background:white;color:var(--ink);font:inherit}.query-fields input{width:290px}.summary{display:flex;align-items:center;gap:0;margin:12px 0;background:white;border:1px solid var(--line);border-radius:7px;overflow:hidden}.summary>div{min-width:118px;padding:9px 14px;border-right:1px solid var(--line);display:grid;grid-template-columns:1fr auto;align-items:center;column-gap:8px}.summary small{color:var(--muted);font-size:10px}.summary strong{grid-row:span 2;color:#8b4b37;font-size:17px}.summary label{margin-left:auto;padding:8px 12px;flex-direction:row;align-items:center}.summary select{min-width:205px;height:30px}.notice{margin:0 0 12px;padding:9px 12px;border:1px solid #bddcc9;border-radius:5px;background:#eef8f1;color:#286440}.notice.bad{border-color:#ebc5c1;background:#fff3f1;color:#9e3831}.card{margin-bottom:13px;border:1px solid var(--line);border-radius:7px;background:white;box-shadow:0 2px 5px #10283a08}.card-title{display:flex;align-items:center;justify-content:space-between;padding:12px 15px}.card-title h2{margin:0;font-size:14px}.card-title p{margin:2px 0 0;color:var(--muted);font-size:10px}.table-scroll{overflow:auto;border-top:1px solid #e6ebf0}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:11px}th{position:sticky;top:0;background:#f1f5f8;color:#607388;font-size:10px;font-weight:700;text-align:left}th,td{padding:8px 9px;border-bottom:1px solid #ebeff3}tbody tr{cursor:pointer}tbody tr:hover{background:#f8fafc}tbody tr.active{background:#fff3ed}.num{text-align:right;font-variant-numeric:tabular-nums}.lot{color:#8b4b37;font-weight:650}.serial{color:#60768b;font-family:Consolas,monospace}.kind,.state{display:inline-block;padding:3px 6px;border-radius:4px;background:#ebf3fb;color:#315e87;font-size:9px;font-weight:700}.kind.scrapinv{background:#fff0df;color:#965618}.state{background:#e9f3ec;color:#286440}.empty{text-align:center;color:#8a98a7;padding:18px}.state-foot{padding:8px 12px;color:#728397;font-size:10px;background:#fafcfd}.history .card-title{padding-bottom:10px}footer{padding:2px 2px 8px;color:#8391a0;font-size:10px}.unavailable{padding:16px;border:1px solid #e8d2ac;border-radius:6px;background:#fff8ea;color:#7a5622}@media(max-width:1000px){.cancel-e10{padding:15px}.query-fields{flex-wrap:wrap}.summary{flex-wrap:wrap}.summary label{margin-left:0}.flow{align-items:flex-start;flex-wrap:wrap}}
</style>
