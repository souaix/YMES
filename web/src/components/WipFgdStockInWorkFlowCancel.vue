<script setup lang="ts">
import { computed, ref } from 'vue'

type ErpSource = 'WorkFlow' | 'TipTop' | 'T100' | 'E10ICD'
type ErpOutcome = 'success' | 'fail' | 'timeout'
type MesOutcome = 'success' | 'fail'
type DetailRow = {
  kind: 'FGDINV' | 'SCRAPINV'
  moNo: string
  lotNo: string
  productNo: string
  productVersion: string
  goodQty: number
  scrapQty: number
  lossQty: number
  unitNo: string
}
type StockInDoc = {
  mesNo: string
  erpNo: string
  source: ErpSource
  inventoryNo: string
  createDate: string
  state: number
  orgState: number
  lines: DetailRow[]
}
type HistoryRow = { at: string; mesNo: string; erpNo: string; source: string; erpResult: string; mesResult: string; state: string; note: string }

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const docs = ref<StockInDoc[]>(dev.value ? [
  {
    mesNo: 'DEV-FGD-619-0001', erpNo: 'WF01-000184', source: 'WorkFlow', inventoryNo: 'DEV-FGD-01', createDate: '2026-11-22 08:42', state: 2, orgState: 0,
    lines: [
      { kind: 'FGDINV', moNo: 'DEV-MO-619-01', lotNo: 'DEV-LOT-619-001', productNo: 'DEV-PRD-619', productVersion: 'A1', goodQty: 48, scrapQty: 0, lossQty: 0, unitNo: 'PCS' },
      { kind: 'SCRAPINV', moNo: 'DEV-MO-619-01', lotNo: 'DEV-LOT-619-002', productNo: 'DEV-PRD-619', productVersion: 'A1', goodQty: 0, scrapQty: 2, lossQty: 1, unitNo: 'PCS' },
    ],
  },
  {
    mesNo: 'DEV-FGD-619-0002', erpNo: 'TP-000932', source: 'TipTop', inventoryNo: 'DEV-FGD-01', createDate: '2026-11-21 16:07', state: 2, orgState: 0,
    lines: [{ kind: 'FGDINV', moNo: 'DEV-MO-619-02', lotNo: 'DEV-LOT-619-003', productNo: 'DEV-PRD-620', productVersion: 'B2', goodQty: 36, scrapQty: 0, lossQty: 0, unitNo: 'PCS' }],
  },
  {
    mesNo: 'DEV-FGD-619-0003', erpNo: 'T100-001145', source: 'T100', inventoryNo: 'DEV-FGD-02', createDate: '2026-11-21 14:20', state: 2, orgState: 0,
    lines: [{ kind: 'FGDINV', moNo: 'DEV-MO-619-03', lotNo: 'DEV-LOT-619-004', productNo: 'DEV-PRD-621', productVersion: 'C1', goodQty: 24, scrapQty: 0, lossQty: 0, unitNo: 'PCS' }],
  },
  {
    mesNo: 'DEV-FGD-619-0004', erpNo: 'E10-000573', source: 'E10ICD', inventoryNo: 'DEV-FGD-03', createDate: '2026-11-20 11:35', state: 2, orgState: 0,
    lines: [{ kind: 'SCRAPINV', moNo: 'DEV-MO-619-04', lotNo: 'DEV-LOT-619-005', productNo: 'DEV-PRD-622', productVersion: 'A2', goodQty: 0, scrapQty: 5, lossQty: 0, unitNo: 'PCS' }],
  },
  {
    mesNo: 'DEV-FGD-619-0005', erpNo: 'WF01-000185', source: 'WorkFlow', inventoryNo: 'DEV-FGD-01', createDate: '2026-11-22 09:10', state: 1, orgState: 2,
    lines: [{ kind: 'FGDINV', moNo: 'DEV-MO-619-05', lotNo: 'DEV-LOT-619-006', productNo: 'DEV-PRD-623', productVersion: 'A1', goodQty: 12, scrapQty: 0, lossQty: 0, unitNo: 'PCS' }],
  },
] : [])
const queryText = ref('')
const queried = ref(false)
const selectedNo = ref('')
const erpOutcome = ref<ErpOutcome>('success')
const mesOutcome = ref<MesOutcome>('success')
const history = ref<HistoryRow[]>([])
const error = ref('')
const info = ref('')
const busy = ref(false)
const visibleDocs = computed(() => docs.value.filter(doc => doc.state === 2 && !!doc.erpNo && (!queryText.value || doc.mesNo.toLowerCase().includes(queryText.value.toLowerCase()))))
const selectedDoc = computed(() => docs.value.find(doc => doc.mesNo === selectedNo.value))
const selectedLines = computed(() => selectedDoc.value?.lines ?? [])
const isWorkflow = computed(() => selectedDoc.value?.source === 'WorkFlow')
const lineTotal = computed(() => selectedLines.value.reduce((sum, row) => sum + row.goodQty + row.scrapQty + row.lossQty, 0))

const fmt = (value: number) => new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 4 }).format(value)
const nowText = () => new Date().toLocaleString('zh-TW', { hour12: false })

function queryDocs(): void {
  error.value = ''
  info.value = ''
  if (!dev.value) { error.value = '正式 MES／WorkFlow API 尚未接入；此頁不會讀取或送出資料。'; return }
  queried.value = true
  selectedNo.value = ''
  info.value = `已載入 ${visibleDocs.value.length} 張 State=2 且 ERPNo 有值的 DEV 單據。`
}

function selectDoc(doc: StockInDoc): void {
  selectedNo.value = doc.mesNo
  error.value = ''
  info.value = doc.state === 2 ? '已載入此單的 FGD／SCRIN 明細；取消前會再次檢查狀態、ERPNo 與來源。' : `目前單據 State=${doc.state}，無法從可取消清單執行。`
}

function cancelStockIn(): void {
  error.value = ''
  info.value = ''
  const doc = selectedDoc.value
  if (!dev.value || busy.value) return
  if (!doc) { error.value = '請先查詢並選取一張入庫單。'; return }
  if (doc.state !== 2 || !doc.erpNo) { error.value = '僅 State=2 且 ERPNo 有值的單據可取消。'; return }
  if (!isWorkflow.value) { error.value = `此單 ERP 來源為 ${doc.source}；原查詢會列出所有 ERPNo 非空的 ERP 單據，但本功能只送 WorkFlow Undo。已阻擋跨 ERP 取消。`; return }
  const erpParts = doc.erpNo.split('-')
  if (erpParts.length < 2 || !erpParts[0] || !erpParts[1]) { error.value = 'ERPNo 格式不含「單別-單號」，無法組 WorkFlow Undo 請求。'; return }

  busy.value = true
  const at = nowText()
  doc.orgState = 2
  doc.state = 1

  if (erpOutcome.value !== 'success') {
    doc.orgState = 1
    doc.state = 2
    const isTimeout = erpOutcome.value === 'timeout'
    history.value.unshift({ at, mesNo: doc.mesNo, erpNo: doc.erpNo, source: doc.source, erpResult: isTimeout ? '逾時／例外' : '明確失敗', mesResult: '未執行', state: '2', note: isTimeout ? '舊 Form 預設 result=fail，例外後會還原 State=2；ERP 實際是否已 Undo 仍未知' : 'ERP Undo 失敗，Form 將 State 還原為 2' })
    error.value = isTimeout ? 'WorkFlow Undo 呼叫逾時。舊 Form 會把例外當 fail 並還原 MES State=2，ERP 端結果可能未知；正式端需對帳後才可重試。' : 'WorkFlow Undo 明確失敗；DEV 模擬還原 MES State=2。'
    busy.value = false
    return
  }

  doc.orgState = 1
  doc.state = -2
  if (mesOutcome.value === 'fail') {
    history.value.unshift({ at, mesNo: doc.mesNo, erpNo: doc.erpNo, source: doc.source, erpResult: 'Undo 成功', mesResult: 'MES 回復失敗', state: '-2', note: 'MES 取消未完成；原流程停在 State=-2，須查核部分 SQL 與 ERP 狀態' })
    error.value = 'WorkFlow Undo 已成功，但 MES 取消失敗；單據保留 State=-2，需先對帳再人工恢復。'
    busy.value = false
    return
  }

  // Core sets both FGD/SCRIN basis State=3; the Form then overwrites them to the final State=-1.
  doc.state = 3
  doc.orgState = -2
  doc.state = -1
  history.value.unshift({ at, mesNo: doc.mesNo, erpNo: doc.erpNo, source: doc.source, erpResult: 'Undo 成功', mesResult: 'MES 回復成功', state: '-1', note: 'Core 回復庫存並設 State=3；Form 接著更新為最終取消 State=-1' })
  info.value = `WorkFlow ERP Undo 與 MES 回復完成：${doc.mesNo} 已取消，良品／不良品明細均已模擬回復。`
  busy.value = false
}
</script>

<template>
  <section class="wf-cancel">
    <header class="hero"><div><p class="eyebrow">WIP · ERP INTEGRATION · WP619</p><h1>取消入庫 WorkFlow ERP</h1><p class="subtitle">WorkFlow Undo → MES FGD／SCRIN 庫存回復</p></div><div class="hero-actions"><span class="dev-pill">DEV 假資料</span><button class="quiet" type="button" @click="emit('close')">關閉</button></div></header>
    <div v-if="!dev" class="unavailable">正式 MES／WorkFlow API 尚未接入；此頁不會讀取或送出資料。</div>
    <template v-else>
      <div class="flow"><b>查詢 State=2／有 ERPNo</b><span>→</span><b>WorkFlow Undo</b><span>→</span><b>MES 回復 FGD／SCRIN</b><span>→</span><b>State=-1</b></div>
      <div class="notice source-warning"><strong>來源守門缺口：</strong>WinForms 查詢只要求 ERPNo 非空，沒有確認 ERP 來源；本頁會顯示其他 ERP 單據供辨識，但只允許 WorkFlow 來源執行取消。</div>
      <div v-if="error || info" class="notice" :class="{ bad: !!error }" role="status">{{ error || info }}</div>

      <section class="card search"><div class="card-heading"><div><h2>選取已確認的 ERP 入庫單</h2><p>來源 WinForms：按「…」載入 State=2、IsERP=True；至少須有入庫單號與 ERPNo。</p></div><span class="setting">State=2 · ERPNo ≠ 空白</span></div><div class="search-controls"><label>MES 入庫單號片段<input v-model="queryText" type="text" placeholder="可留白查全部"></label><button class="primary" type="button" @click="queryDocs">查詢可取消單</button><label>ERP Undo 結果<select v-model="erpOutcome"><option value="success">成功</option><option value="fail">明確失敗</option><option value="timeout">逾時／例外（結果未知）</option></select></label><label>MES 回復結果<select v-model="mesOutcome"><option value="success">成功</option><option value="fail">失敗（保留 State=-2）</option></select></label></div></section>

      <section class="card list"><div class="card-heading"><div><h2>可取消候選單</h2><p>{{ queried ? `${visibleDocs.length} 張符合條件` : '按查詢後顯示 DEV 候選單' }}</p></div><span class="filter-note">清單混有不同 ERP 來源 · 先核對 ERPNo</span></div><div class="table-wrap"><table><thead><tr><th>選取</th><th>MES 入庫單</th><th>ERPNo</th><th>DEV ERP 來源</th><th>成品庫</th><th>建立時間</th><th>明細類型</th><th>State</th></tr></thead><tbody><tr v-for="doc in (queried ? visibleDocs : [])" :key="doc.mesNo" :class="{ selected: selectedNo === doc.mesNo, foreign: doc.source !== 'WorkFlow' }" @click="selectDoc(doc)"><td><input type="radio" name="wf-cancel-doc" :checked="selectedNo === doc.mesNo" @click.stop="selectDoc(doc)"></td><td class="serial">{{ doc.mesNo }}</td><td>{{ doc.erpNo }}</td><td><span class="source-tag" :class="{ workflow: doc.source === 'WorkFlow' }">{{ doc.source }}</span></td><td>{{ doc.inventoryNo }}</td><td>{{ doc.createDate }}</td><td>{{ [...new Set(doc.lines.map(line => line.kind))].join(' / ') }}</td><td>{{ doc.state }}</td></tr><tr v-if="queried && !visibleDocs.length"><td colspan="8" class="empty">沒有符合條件的 DEV 入庫單；State≠2 或 ERPNo 空白的單據不會出現在原查詢清單。</td></tr><tr v-if="!queried"><td colspan="8" class="empty">尚未查詢</td></tr></tbody></table></div></section>

      <section class="card detail"><div class="card-heading"><div><h2>入庫明細</h2><p>{{ selectedDoc ? `${selectedDoc.mesNo} · ${selectedLines.length} 列 · Good／Scrap／Loss 合計 ${fmt(lineTotal)}` : '選取一張候選單後載入 FGD 與 SCRIN 明細' }}</p></div><span v-if="selectedDoc" class="setting">{{ selectedDoc.source }} · {{ selectedDoc.erpNo }}</span></div><div class="table-wrap"><table><thead><tr><th>INVType</th><th>MO</th><th>Lot</th><th>產品／版次</th><th class="num">GoodQty</th><th class="num">ScrapQty</th><th class="num">LossQty</th><th>Unit</th></tr></thead><tbody><tr v-for="(line, index) in selectedLines" :key="line.lotNo + index"><td>{{ line.kind }}</td><td>{{ line.moNo }}</td><td class="serial">{{ line.lotNo }}</td><td>{{ line.productNo }} · {{ line.productVersion }}</td><td class="num">{{ fmt(line.goodQty) }}</td><td class="num">{{ fmt(line.scrapQty) }}</td><td class="num">{{ fmt(line.lossQty) }}</td><td>{{ line.unitNo }}</td></tr><tr v-if="!selectedLines.length"><td colspan="8" class="empty">沒有明細</td></tr></tbody></table></div><div v-if="selectedDoc && !isWorkflow" class="blocked">這張單的 DEV ERP 來源是 {{ selectedDoc.source }}。原始 ERPNo 欄位不保證能識別來源，因此取消按鈕會阻擋。</div><div class="actions"><button class="danger" type="button" :disabled="busy || !selectedDoc" @click="cancelStockIn">{{ busy ? '處理中…' : '取消 WorkFlow 入庫' }}</button></div></section>

      <section class="card history"><div class="card-heading"><div><h2>模擬處理紀錄</h2><p>State 2=已確認；1=ERP Undo 處理中；-2=MES 取消處理中／待恢復；3=Core 取消；-1=最終取消。</p></div></div><div class="table-wrap"><table><thead><tr><th>時間</th><th>MES 入庫單</th><th>ERPNo</th><th>來源</th><th>ERP Undo</th><th>MES 回復</th><th>最終 State</th><th>處理說明</th></tr></thead><tbody><tr v-for="(item, i) in history" :key="item.at + item.mesNo + i"><td>{{ item.at }}</td><td class="serial">{{ item.mesNo }}</td><td>{{ item.erpNo }}</td><td>{{ item.source }}</td><td>{{ item.erpResult }}</td><td>{{ item.mesResult }}</td><td>{{ item.state }}</td><td>{{ item.note }}</td></tr><tr v-if="!history.length"><td colspan="8" class="empty">尚無操作紀錄</td></tr></tbody></table></div></section>
      <footer>此頁只改 DEV 假資料。舊 Core 的多表回復 SQL 沒有有效交易包覆；ERP 與 MES 狀態須由正式服務補上冪等、對帳及可恢復處理。</footer>
    </template>
  </section>
</template>

<style scoped>
.wf-cancel{--ink:#1b2a3c;--muted:#708094;--line:#dce4eb;color:var(--ink);height:100%;min-height:0;overflow:auto;padding:22px 26px 30px;background:#f7f9fb;font:13px/1.45 "Segoe UI","Microsoft JhengHei",sans-serif}.hero{display:flex;align-items:center;justify-content:space-between;margin-bottom:15px}.eyebrow{margin:0;color:#73869a;font-size:10px;font-weight:750;letter-spacing:.13em}.hero h1{margin:2px 0 0;font-size:23px}.subtitle{margin:0;color:var(--muted);font-size:12px}.hero-actions{display:flex;align-items:center;gap:9px}.dev-pill,.setting,.source-tag{display:inline-block;padding:4px 8px;border-radius:999px;background:#e8f4ed;color:#22724a;font-size:10px;font-weight:700}.setting{background:#edf2f8;color:#385a78}.source-tag{border-radius:4px;background:#fff1dc;color:#8a5a1f}.source-tag.workflow{background:#e8f4ed;color:#22724a}.quiet,.primary,.danger{height:34px;padding:0 12px;border:1px solid var(--line);border-radius:5px;background:white;color:#334d66;cursor:pointer;font:inherit}.primary,.danger{border-color:#173b5d;background:#173b5d;color:white;font-weight:700}.danger{border-color:#973c34;background:#a8473e}.primary:disabled,.danger:disabled{opacity:.45;cursor:not-allowed}.flow{display:flex;align-items:center;gap:10px;padding:10px 14px;margin-bottom:13px;border:1px solid #ccdae7;border-radius:6px;background:#eff5fa;color:#48637c;font-size:11px}.flow b{color:#234663}.card{margin-bottom:13px;border:1px solid var(--line);border-radius:7px;background:white;box-shadow:0 2px 5px #10283a08}.search{padding:13px 15px}.card-heading{display:flex;align-items:center;justify-content:space-between;padding:11px 14px}.search .card-heading{padding:0 0 10px}.card-heading h2{margin:0;font-size:14px}.card-heading p{margin:2px 0 0;color:var(--muted);font-size:10px}.search-controls{display:grid;grid-template-columns:1.6fr auto 1fr 1fr;align-items:end;gap:10px}.search-controls label{display:flex;flex-direction:column;gap:5px;color:#5e7185;font-size:10px;font-weight:650}.search-controls input,.search-controls select{height:33px;min-width:0;border:1px solid #cdd8e2;border-radius:4px;padding:0 8px;background:white;color:var(--ink);font:inherit}.notice{margin:0 0 12px;padding:9px 12px;border:1px solid #bddcc9;border-radius:5px;background:#eef8f1;color:#286440}.notice.bad{border-color:#ebc5c1;background:#fff3f1;color:#9e3831}.notice.source-warning{border-color:#efdaa6;background:#fff8e7;color:#785a20}.list,.detail,.history{overflow:hidden}.filter-note{color:#8a5a1f;font-size:10px}.table-wrap{overflow:auto;border-top:1px solid #e6ebf0}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:10px}th{position:sticky;top:0;background:#f1f5f8;color:#607388;font-size:9px;font-weight:700;text-align:left}th,td{padding:7px 8px;border-bottom:1px solid #ebeff3}tbody tr{cursor:pointer}tbody tr:hover{background:#f8fafc}tbody tr.selected{background:#edf6fb}tbody tr.foreign{background:#fffaf1}.serial{color:#60768b;font-family:Consolas,monospace}.num{text-align:right;font-variant-numeric:tabular-nums}input[type=radio]{accent-color:#1c628c}.empty{text-align:center;color:#8a98a7;padding:17px}.blocked{padding:9px 13px;border-top:1px solid #efdaa6;background:#fff8e7;color:#785a20;font-size:11px}.actions{display:flex;justify-content:flex-end;padding:9px 12px}.history .card-heading{padding-bottom:9px}footer{padding:2px 2px 8px;color:#8391a0;font-size:10px}.unavailable{padding:16px;border:1px solid #e8d2ac;border-radius:6px;background:#fff8ea;color:#7a5622}@media(max-width:1000px){.wf-cancel{padding:14px}.search-controls{grid-template-columns:repeat(2,minmax(0,1fr))}.flow{align-items:flex-start;flex-wrap:wrap}}
</style>
