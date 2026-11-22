<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type ResultScenario = 'success' | 'failure' | 'unknown'
type SearchScenario = 'success' | 'failure'
type PendingItem = {
  key: string
  wono: string
  productNo: string
  lotNo: string
  qty: number
  goodBad: string
  hsDate: string
  dueDate: string
  idbsNo: string
  werks: string
  grsNo: string
  aufnr: string
  mandt: string
}

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const fixtureItems: PendingItem[] = [
  { key: 'U001', wono: 'DEV-WO-26001', productNo: 'DEV-MTL-001', lotNo: 'DEVLOT-U001', qty: 240, goodBad: 'GOOD', hsDate: '2026-11-21', dueDate: '2026-11-22', idbsNo: '000010', werks: 'DEV1', grsNo: '0001', aufnr: 'DEV-ORD-26001', mandt: '800' },
  { key: 'U002', wono: 'DEV-WO-26002', productNo: 'DEV-MTL-008', lotNo: 'DEVLOT-U002', qty: 80, goodBad: 'GOOD', hsDate: '2026-11-21', dueDate: '2026-11-23', idbsNo: '000020', werks: 'DEV1', grsNo: '', aufnr: 'DEV-ORD-26002', mandt: '800' },
  { key: 'U003', wono: 'DEV-WO-26002', productNo: 'DEV-MTL-011', lotNo: 'DEVLOT-U003', qty: 12, goodBad: 'BAD', hsDate: '2026-11-22', dueDate: '2026-11-24', idbsNo: '000030', werks: 'DEV1', grsNo: '0002', aufnr: 'DEV-ORD-26002', mandt: '800' },
  { key: 'U004', wono: 'DEV-WO-26003', productNo: 'DEV-MTL-004', lotNo: 'DEVLOT-U004', qty: 60, goodBad: 'GOOD', hsDate: '2026-11-22', dueDate: '2026-11-25', idbsNo: '', werks: 'DEV1', grsNo: '', aufnr: 'DEV-ORD-26003', mandt: '800' },
]
const remotePending = ref<PendingItem[]>(fixtureItems.map(item => ({ ...item })))
const rows = ref<PendingItem[]>([])
const selectedKey = ref('')
const busy = ref(false)
const loading = ref(false)
const scenario = ref<ResultScenario>('success')
const searchScenario = ref<SearchScenario>('success')
const errorMessage = ref('')
const infoMessage = ref('')
const requestUnknown = ref(false)
const lastUpdated = ref('')
const operationLog = ref<{ time: string; lotNo: string; idbsNo: string; result: string; detail: string }[]>([])
const selected = computed(() => rows.value.find(item => item.key === selectedKey.value) ?? null)
const canOutbound = computed(() => !!selected.value && !busy.value && !requestUnknown.value)
const outboundFieldsValid = computed(() => !!selected.value?.lotNo.trim() && !!selected.value?.idbsNo.trim())
const resolvedGrsNo = computed(() => selected.value?.grsNo.trim() || '0001')

function timestamp(): string { return new Date().toLocaleString('zh-TW', { hour12: false }) }

function searchData(): void {
  errorMessage.value = ''
  infoMessage.value = ''
  loading.value = true
  if (searchScenario.value === 'failure') {
    errorMessage.value = '模擬 Link Error：GetPendingList 失敗；保留目前 Grid 供檢視。'
    loading.value = false
    return
  }
  if (requestUnknown.value) {
    requestUnknown.value = false
    selectedKey.value = ''
    infoMessage.value = '已重新查詢 pending list；上一筆 outbound 結果已由清單狀態對帳。'
  }
  rows.value = remotePending.value.map(item => ({ ...item }))
  if (selectedKey.value && !rows.value.some(item => item.key === selectedKey.value)) selectedKey.value = ''
  lastUpdated.value = timestamp()
  loading.value = false
  if (!infoMessage.value) infoMessage.value = `已載入 ${rows.value.length} 筆待處理資料（DEV）。`
}

function requestSearch(): void {
  if (!dev.value || busy.value) return
  searchData()
}

function requestOutbound(): void {
  errorMessage.value = ''
  infoMessage.value = ''
  if (!selected.value) {
    errorMessage.value = 'Please select one record.'
    return
  }
  if (!selected.value.lotNo.trim() || !selected.value.idbsNo.trim()) {
    errorMessage.value = 'LOTNO / IDBSNO is empty.'
    return
  }
  runOutboundRequest()
}

function runOutboundRequest(): void {
  const item = selected.value
  if (!item || busy.value) return
  busy.value = true
  errorMessage.value = ''
  infoMessage.value = ''
  const time = timestamp()
  const grsNo = item.grsNo.trim() || '0001'

  if (scenario.value === 'failure') {
    const detail = 'API 回傳 Success=false；本筆仍保留在 pending 清單。'
    operationLog.value.unshift({ time, lotNo: item.lotNo, idbsNo: item.idbsNo, result: '失敗', detail })
    errorMessage.value = '模擬 outbound 失敗：' + detail
    busy.value = false
    return
  }

  if (scenario.value === 'unknown') {
    remotePending.value = remotePending.value.filter(row => row.key !== item.key)
    requestUnknown.value = true
    operationLog.value.unshift({ time, lotNo: item.lotNo, idbsNo: item.idbsNo, result: '結果未知', detail: `請求已送出（GRSNO=${grsNo}），回應逾時；待 Search 重新查詢對帳。` })
    errorMessage.value = '回應逾時，SAP 端可能已處理。已停用重送，請先 Search 重新讀取 pending list。'
    busy.value = false
    return
  }

  remotePending.value = remotePending.value.filter(row => row.key !== item.key)
  operationLog.value.unshift({ time, lotNo: item.lotNo, idbsNo: item.idbsNo, result: 'DEV 模擬完成', detail: `Outbound 成功；AUFNR=${item.aufnr}、MATNR=${item.productNo}、GRSNO=${grsNo}。` })
  selectedKey.value = ''
  busy.value = false
  searchData()
  if (!errorMessage.value) infoMessage.value = 'DEV outbound 模擬成功；已重新載入 pending list。'
}

onMounted(() => {
  if (dev.value) searchData()
})
</script>

<template>
  <section class="uninventory-page">
    <header class="page-header"><div><p class="eyebrow">WIP · ERP INVENTORY REVERSAL · DEV</p><h1>取消入庫作業</h1><p class="subtitle">查詢待退料清單，選取單筆執行 outbound</p></div><div class="header-actions"><span class="dev-pill">DEV 假資料</span><button type="button" class="quiet" @click="emit('close')">關閉</button></div></header>
    <div v-if="!dev" class="unavailable-note" role="status">正式 SAP outbound 服務尚未接入；此頁不會讀取或改寫資料。</div>
    <template v-else>
      <div class="source-banner"><span><b>服務</b> WipUnInventory/GetPendingList · Outbound</span><span><b>SAP runtime</b> DEV fixture values only</span><span><b>資料</b> {{ rows.length }} 筆待處理</span></div>
      <div class="dev-controls"><label>DEV Search 情境<select v-model="searchScenario"><option value="success">GetPendingList 成功</option><option value="failure">GetPendingList Link Error</option></select></label><label>DEV outbound 情境<select v-model="scenario"><option value="success">Success</option><option value="failure">Success=false</option><option value="unknown">Timeout／結果未知</option></select></label><span class="control-note">Search 在 Form 開啟時自動執行；此頁完全使用本地假資料。</span><span class="updated">更新：{{ lastUpdated || '—' }}</span></div>
      <div v-if="errorMessage || infoMessage" class="notice" :class="{ error: !!errorMessage }" role="status"><span>{{ errorMessage || infoMessage }}</span><button type="button" aria-label="清除訊息" @click="errorMessage = ''; infoMessage = ''">×</button></div>
      <div v-if="requestUnknown" class="reconcile-banner"><strong>結果未知，已暫停重送</strong><span>先 Search 讀取 pending list，依服務端清單狀態對帳。</span><button type="button" class="quiet" :disabled="loading" @click="requestSearch">Search 對帳</button></div>
      <section class="panel list-panel"><div class="panel-heading"><div><h2>Pending outbound</h2><p>單選資料後按 outbound；資料欄位依 API 回傳後由 Form 改名與隱藏欄位。</p></div><div class="panel-actions"><span class="count">{{ rows.length }} 筆</span><button type="button" class="quiet" :disabled="loading || busy" @click="requestSearch">{{ loading ? 'Search 中…' : 'Search' }}</button><button type="button" class="primary" :disabled="!canOutbound" @click="requestOutbound">outbound</button></div></div>
        <div class="table-scroll"><table><thead><tr><th>WONO</th><th>PRODUCTNO</th><th>LOTNO</th><th>QTY</th><th>GOOD/BAD</th><th>HSDATE</th><th>DUEDATE</th><th>IDBSNO</th><th>WERKS</th><th>GRSNO</th><th>AUFNR</th><th>MANDT</th></tr></thead><tbody><tr v-for="item in rows" :key="item.key" :class="{ selected: selectedKey === item.key, invalid: !item.lotNo || !item.idbsNo }" @click="selectedKey = item.key"><td>{{ item.wono }}</td><td class="mono">{{ item.productNo }}</td><td class="mono">{{ item.lotNo || '—' }}</td><td class="numeric">{{ item.qty.toLocaleString('zh-TW') }}</td><td><span class="quality" :class="item.goodBad.toLowerCase()">{{ item.goodBad }}</span></td><td>{{ item.hsDate }}</td><td>{{ item.dueDate }}</td><td class="mono">{{ item.idbsNo || '—' }}</td><td>{{ item.werks }}</td><td>{{ item.grsNo || '(default 0001)' }}</td><td class="mono">{{ item.aufnr }}</td><td>{{ item.mandt }}</td></tr><tr v-if="!rows.length"><td colspan="12" class="empty">目前沒有待處理資料</td></tr></tbody></table></div>
        <div class="list-footer"><span>{{ selected ? `目前選取 ${selected.lotNo}` : '尚未選取資料' }}</span><span>缺 LOTNO／IDBSNO 的資料不能 outbound</span></div>
      </section>
      <div class="detail-grid"><section class="panel detail-panel"><div class="panel-heading"><div><h2>Outbound request</h2><p>來源請求欄位與 SAP runtime 分開呈現。</p></div><span class="request-state" :class="{ ready: canOutbound && outboundFieldsValid }">{{ requestUnknown ? 'RECONCILE' : !selected ? 'SELECT ONE' : outboundFieldsValid ? 'READY' : 'CHECK FIELDS' }}</span></div><dl v-if="selected"><dt>LotNo</dt><dd>{{ selected.lotNo || '—' }}</dd><dt>Aufnr</dt><dd>{{ selected.aufnr }}</dd><dt>Matnr</dt><dd>{{ selected.productNo }}</dd><dt>Idbsno</dt><dd>{{ selected.idbsNo || '—' }}</dd><dt>Grsno</dt><dd>{{ resolvedGrsNo }} <small v-if="!selected.grsNo">· Form fallback</small></dd><dt>SAP_WERKS / SAP_MANDT</dt><dd>{{ selected.werks }} / {{ selected.mandt }}</dd></dl><p v-else class="empty detail-empty">選一筆清單資料檢視 request。</p></section>
        <section class="panel history-panel"><div class="panel-heading"><div><h2>DEV 操作紀錄</h2><p>僅保留目前頁面內的模擬結果。</p></div></div><div class="table-scroll"><table><thead><tr><th>時間</th><th>LOTNO</th><th>IDBSNO</th><th>結果</th><th>說明</th></tr></thead><tbody><tr v-for="(item, index) in operationLog" :key="item.time + index"><td>{{ item.time }}</td><td class="mono">{{ item.lotNo }}</td><td class="mono">{{ item.idbsNo }}</td><td><span class="result-pill" :class="{ failed: item.result === '失敗', unknown: item.result === '結果未知' }">{{ item.result }}</span></td><td>{{ item.detail }}</td></tr><tr v-if="!operationLog.length"><td colspan="5" class="empty">尚無操作紀錄</td></tr></tbody></table></div></section></div>
      <footer>DEV 只模擬 pending list 與單筆 outbound 請求；不呼叫 SAP、Web API、MES Core、資料庫或 ERP。</footer>
    </template>
  </section>
</template>

<style scoped>
.uninventory-page{--ink:#1d2d3c;--muted:#718496;--line:#dce4eb;height:100%;min-height:0;overflow:auto;padding:20px 24px 28px;box-sizing:border-box;background:#f6f8fa;color:var(--ink);font:12px/1.45 "Segoe UI","Microsoft JhengHei",sans-serif}.page-header{display:flex;align-items:center;justify-content:space-between;gap:14px;margin-bottom:14px}.eyebrow{margin:0;color:#76899b;font-size:9px;font-weight:800;letter-spacing:.12em}.page-header h1{margin:2px 0 0;font-size:22px}.subtitle{margin:0;color:var(--muted);font-size:11px}.header-actions,.panel-actions{display:flex;align-items:center;gap:8px}.dev-pill,.count,.request-state,.result-pill{display:inline-flex;padding:4px 8px;border-radius:999px;background:#e6f3ec;color:#28744e;font-size:9px;font-weight:750}.quiet,.primary{min-height:31px;padding:0 11px;border:1px solid #ccd7e0;border-radius:5px;background:white;color:#38526a;font:inherit;cursor:pointer}.primary{border-color:#244e72;background:#244e72;color:white;font-weight:750}.quiet:disabled,.primary:disabled{cursor:not-allowed;opacity:.45}.source-banner{display:flex;gap:16px;flex-wrap:wrap;padding:8px 11px;margin-bottom:10px;border:1px solid #dbe4ec;border-radius:5px;background:#edf3f8;color:#667c90;font-size:9px}.source-banner b{margin-right:4px;color:#38546e}.dev-controls{display:flex;align-items:end;gap:12px;flex-wrap:wrap;padding:8px 10px;margin-bottom:10px;border:1px solid var(--line);border-radius:5px;background:white}.dev-controls label{display:flex;flex-direction:column;gap:3px;color:#6d8193;font-size:9px}.dev-controls select{height:28px;min-width:180px;padding:0 6px;border:1px solid #ccd8e1;border-radius:4px;background:white;color:#3c556b;font:inherit}.control-note,.updated{color:#7d8c99;font-size:9px}.updated{margin-left:auto}.notice{display:flex;align-items:center;justify-content:space-between;gap:8px;padding:8px 10px;margin-bottom:10px;border:1px solid #c4dfcd;border-radius:5px;background:#eff8f1;color:#286543;font-size:10px}.notice.error{border-color:#e7c2bd;background:#fff3f1;color:#963e37}.notice button{border:0;background:transparent;color:inherit;font-size:16px;cursor:pointer}.reconcile-banner{display:flex;align-items:center;gap:10px;padding:10px 12px;margin-bottom:10px;border:1px solid #ecd4a8;border-radius:5px;background:#fff8e9;color:#805d25;font-size:10px}.reconcile-banner strong{font-size:11px}.reconcile-banner span{flex:1}.panel{min-width:0;border:1px solid var(--line);border-radius:6px;background:white;box-shadow:0 2px 5px #142b3a08;overflow:hidden}.panel-heading{display:flex;align-items:center;justify-content:space-between;gap:12px;padding:10px 12px}.panel-heading h2{margin:0;font-size:13px}.panel-heading p{margin:2px 0 0;color:var(--muted);font-size:9px}.panel-actions{flex:none}.table-scroll{overflow:auto;border-top:1px solid #e7edf2}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:9px}th{position:sticky;top:0;background:#f1f5f8;color:#667b8d;font-size:8px;font-weight:800;text-align:left}th,td{padding:7px 8px;border-bottom:1px solid #edf1f4}tbody tr{cursor:pointer}tbody tr.selected{background:#eaf4fb}tbody tr.invalid{color:#9b7659}.mono{font-family:Consolas,"Cascadia Mono",monospace;color:#526d84}.numeric{text-align:right;font-variant-numeric:tabular-nums}.quality{padding:2px 5px;border-radius:3px;background:#e7f3eb;color:#34734d;font-size:8px;font-weight:750}.quality.bad{background:#fff0ec;color:#a24a3e}.empty{text-align:center;color:#8694a1;padding:17px}.list-footer{display:flex;justify-content:space-between;gap:10px;padding:8px 11px;color:#7b8996;font-size:9px}.detail-grid{display:grid;grid-template-columns:minmax(260px,.62fr) minmax(450px,1.38fr);gap:10px;margin-top:10px}.request-state{background:#f0f3f5;color:#788794}.request-state.ready{background:#e5f3eb;color:#28744e}.detail-panel dl{display:grid;grid-template-columns:minmax(125px,.7fr) minmax(150px,1.3fr);gap:7px 10px;padding:0 13px 13px;margin:0;font-size:10px}.detail-panel dt{color:#81909d}.detail-panel dd{margin:0;color:#3d586f;font-weight:650}.detail-panel dd small{color:#8795a1;font-weight:400}.detail-empty{margin:0}.result-pill{border-radius:4px;background:#e8f2eb;color:#377451}.result-pill.failed{background:#fff0ed;color:#a24b41}.result-pill.unknown{background:#fff4db;color:#8b6727}footer{padding:9px 2px;color:#84929e;font-size:9px}.unavailable-note{padding:12px;border:1px solid #e6d0aa;border-radius:6px;background:#fff8ea;color:#795923;font-size:11px}@media(max-width:1000px){.uninventory-page{padding:13px}.detail-grid{grid-template-columns:1fr}.panel-heading{align-items:flex-start;flex-direction:column}.panel-actions{flex-wrap:wrap}.list-footer{flex-direction:column}}@media(max-width:560px){.page-header{align-items:flex-start}.source-banner{gap:8px}.dev-controls{align-items:stretch;flex-direction:column}.updated{margin-left:0}.reconcile-banner{align-items:flex-start;flex-direction:column}}
</style>
