<script setup lang="ts">
import { computed, ref } from 'vue'

type Tolerance = { operator: number; up: number; low?: number; color: string }
type KpiValue = { no: string; name: string; description: string; targetType: 'Number' | 'Percentage'; target: number; low?: number; high?: number; actual: number | null; script: string; reportId: string; chartId: string; tolerance?: Tolerance }
type DrawNode = { id: string; kind: 'kpi' | 'description'; refNo: string; label: string; x: number; y: number; text?: string }
type DrawLink = { from: string; to: string; style: string }
type Basis = { no: string; name: string; description: string; creator: string; created: string; freezeState: 0 | 1; nodes: DrawNode[]; links: DrawLink[]; kpis: KpiValue[] }

const emit = defineEmits<{ close: [] }>()
const rows = ref<Basis[]>([
  { no: 'KPI-001', name: '工廠營運指標', description: '日常營運概況', creator: 'DEMO', created: '2026/01/12 09:30:00', freezeState: 0, nodes: [{ id: 'a', kind: 'kpi', refNo: 'YIELD', label: '良率', x: 80, y: 70 }, { id: 'b', kind: 'description', refNo: '', label: '目標說明', x: 350, y: 92, text: '日目標 98%\n資料為測試示意' }, { id: 'c', kind: 'kpi', refNo: 'OEE', label: 'OEE', x: 80, y: 240 }], links: [{ from: 'a', to: 'b', style: 'VH' }, { from: 'a', to: 'c', style: 'Straight' }], kpis: [{ no: 'YIELD', name: '良率', description: '合格數 / 投入數', targetType: 'Percentage', target: 98, low: 95, high: 100, actual: 96.8, script: 'SELECT GOODQTY / INPUTQTY * 100 FROM ...', reportId: 'RPT-YIELD', chartId: 'CHART-YIELD', tolerance: { operator: 1, up: 97, color: '#f1c6a6' } }, { no: 'OEE', name: '整體設備效率', description: 'Availability × Performance × Quality', targetType: 'Percentage', target: 85, low: 80, high: 100, actual: 82.4, script: 'SELECT OEE_VALUE FROM ...', reportId: 'RPT-OEE', chartId: 'CHART-OEE', tolerance: { operator: 1, up: 80, color: '#f1c6a6' } }] },
  { no: 'KPI-002', name: '品質指標', description: '品質部門檢視', creator: 'DEMO', created: '2025/12/18 14:10:00', freezeState: 0, nodes: [{ id: 'd', kind: 'kpi', refNo: 'DEFECT', label: '不良率', x: 130, y: 120 }], links: [], kpis: [{ no: 'DEFECT', name: '不良率', description: '不良數 / 投入數', targetType: 'Percentage', target: 1, low: 0, high: 2, actual: 1.4, script: 'SELECT DEFECTQTY / INPUTQTY * 100 FROM ...', reportId: 'RPT-DEFECT', chartId: '', tolerance: { operator: 0, up: 2, color: '#f1c6a6' } }] },
  { no: 'KPI-003', name: '設備效率', description: '測試中的未凍結版本不出現在 RT905 清單', creator: 'DEMO', created: '2026/06/01 08:00:00', freezeState: 1, nodes: [], links: [], kpis: [] },
])
const queryNo = ref('')
const queryName = ref('')
const resultRows = ref<Basis[]>([])
const selectedNo = ref('')
const message = ref('Query 後只顯示 FreezeState=0 的已凍結指標樹。假實績、公式與歷程皆為 DEV 示意。')
const visibleRows = computed(() => resultRows.value)
const selected = computed(() => rows.value.find(row => row.no === selectedNo.value) ?? null)
const activeView = ref(false)
const zoom = ref(100)
const detailKpi = ref<KpiValue | null>(null)
const savedLogs = ref<Array<{ treeNo: string; kpiNo: string; value: number | null }>>([])

function query() {
  const no = queryNo.value.trim().toLowerCase()
  const name = queryName.value.trim().toLowerCase()
  resultRows.value = rows.value.filter(row => row.freezeState === 0 && (!no || row.no.toLowerCase().includes(no)) && (!name || row.name.toLowerCase().includes(name))).sort((a, b) => a.no.localeCompare(b.no))
  selectedNo.value = resultRows.value[0]?.no ?? ''
  message.value = `查詢完成：${resultRows.value.length} 筆；依舊版行為只顯示 FreezeState=0。`
}
function openTree(row = selected.value) {
  if (!row || row.freezeState !== 0) return
  selectedNo.value = row.no
  activeView.value = true
  zoom.value = 100
  detailKpi.value = null
  message.value = `開啟 ${row.no}；實績值只使用 DEV fixtures，ActualValueScript 未執行。`
}
function kpiFor(node: DrawNode) { return selected.value?.kpis.find(kpi => kpi.no === node.refNo) }
function matches(kpi: KpiValue) {
  const rule = kpi.tolerance
  if (!rule || kpi.actual === null) return false
  const actual = kpi.actual
  if (rule.operator === 0) return actual > rule.up
  if (rule.operator === 1) return actual < rule.up
  if (rule.operator === 2) return actual >= rule.up
  if (rule.operator === 3) return actual <= rule.up
  if (rule.operator === 4) return actual === rule.up
  if (rule.operator === 5) return actual >= (rule.low ?? -Infinity) && actual <= rule.up
  return false
}
function nodeStyle(node: DrawNode) {
  const kpi = kpiFor(node)
  const alarm = kpi ? matches(kpi) : false
  return { left: `${node.x}px`, top: `${node.y}px`, background: alarm ? (kpi?.tolerance?.color ?? '#f2d4c0') : undefined }
}
function nodeById(id: string) { return selected.value?.nodes.find(node => node.id === id) }
function path(link: DrawLink) {
  const from = nodeById(link.from), to = nodeById(link.to)
  if (!from || !to) return ''
  const sx = from.x + 85, sy = from.y + 45, ex = to.x + 85, ey = to.y + 45, mx = (sx + ex) / 2, my = (sy + ey) / 2
  if (link.style === 'VH') return `M ${sx} ${sy} L ${sx} ${my} L ${ex} ${my} L ${ex} ${ey}`
  if (link.style === 'HV') return `M ${sx} ${sy} L ${mx} ${sy} L ${mx} ${ey} L ${ex} ${ey}`
  if (link.style === 'VHV') return `M ${sx} ${sy} L ${sx} ${my} L ${mx} ${my} L ${mx} ${ey} L ${ex} ${ey}`
  if (link.style === 'HVH') return `M ${sx} ${sy} L ${mx} ${sy} L ${mx} ${my} L ${ex} ${my} L ${ex} ${ey}`
  return `M ${sx} ${sy} L ${ex} ${ey}`
}
function openKpi(node: DrawNode) { if (node.kind === 'kpi') detailKpi.value = kpiFor(node) ?? null }
function saveLog() {
  if (!selected.value) return
  savedLogs.value.push(...selected.value.kpis.map(kpi => ({ treeNo: selected.value!.no, kpiNo: kpi.no, value: kpi.actual })))
  message.value = `已在 DEV 記憶體模擬寫入 ${selected.value.kpis.length} 筆 KPI 歷程；沒有呼叫 wsRPT.AddEKBKPILog。`
}
function exportCsv() {
  if (!selected.value) return
  const headers = ['KPITreeNo','KPITreeName','KPINo','KPIName','TargetValue','TargetValueUp','TargetValueLow','ActualValue','ActualValueScript','Creator','CreateDate']
  const csv = [headers, ...selected.value.kpis.map(kpi => [selected.value!.no, selected.value!.name, kpi.no, kpi.name, kpi.target, kpi.high ?? '', kpi.low ?? '', kpi.actual ?? '', kpi.script, selected.value!.creator, selected.value!.created])].map(values => values.map(value => `"${String(value).replaceAll('"','""')}"`).join(',')).join('\r\n')
  const url = URL.createObjectURL(new Blob(['\ufeff', csv], { type: 'text/csv;charset=utf-8' }))
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = `${selected.value.no}-DEV-KPI.csv`
  anchor.click()
  URL.revokeObjectURL(url)
  message.value = '已匯出 DEV CSV；腳本欄為原樣展示，沒有執行。'
}
query()
</script>

<template>
  <section class="query-page" aria-label="指標樹查詢 DEV 預覽">
    <header class="head"><div><h1>指標樹查詢 <small>RT905</small></h1><p>DEV 假資料；ActualValueScript 不執行</p></div><button @click="emit('close')">Close (X)</button></header>
    <div class="search"><label>KPI Tree No <span>%</span><input v-model="queryNo" @keyup.enter="query" /></label><label>KPI Tree Name <span>%</span><input v-model="queryName" @keyup.enter="query" /></label><button @click="query">Query</button></div>
    <p class="notice" role="status">{{ message }}</p>
    <div class="result-layout"><div class="table-wrap"><table><thead><tr><th>KPI Tree No</th><th>KPI Tree Name</th><th>Description</th><th>Creator</th><th>Create Date</th></tr></thead><tbody><tr v-for="row in visibleRows" :key="row.no" :class="{selected:selectedNo===row.no}" @click="selectedNo=row.no" @dblclick="openTree(row)"><td>{{ row.no }}</td><td>{{ row.name }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ row.created }}</td></tr><tr v-if="!visibleRows.length"><td colspan="5" class="empty">請輸入條件後 Query；空白可查全部已凍結指標樹。</td></tr></tbody></table></div><aside><button :disabled="!selected" @click="openTree()">Tree Query</button><p>只查 FreezeState=0</p></aside></div>
    <div v-if="activeView && selected" class="veil"><section class="viewer"><header><div><h2>{{ selected.no }} — {{ selected.name }}</h2><p>唯讀樹圖　|　DEV 假實績與容忍值示意；正式公式需由 Report Engine 執行。</p></div><button @click="activeView=false">×</button></header><nav class="tools"><button @click="zoom=Math.max(25,zoom-25)">−</button><span>{{ zoom }}%</span><button @click="zoom=Math.min(300,zoom+25)">+</button><button @click="zoom=100">Reset</button><span class="spacer"></span><button @click="exportCsv">Export CSV</button><button @click="saveLog">Save KPI Log</button><button @click="activeView=false">Close</button></nav><div class="canvas-scroll"><div class="canvas" :style="{transform:`scale(${zoom/100})`,width:`${10000/zoom}%`,height:`${8000/zoom}px`}"><svg class="links"><path v-for="(link,index) in selected.links" :key="index" :d="path(link)" fill="none" stroke="#667286" stroke-width="2" marker-end="url(#kpi-arrow)" /><defs><marker id="kpi-arrow" markerWidth="10" markerHeight="10" refX="7" refY="3" orient="auto"><path d="M0,0 L0,6 L8,3 z" fill="#667286" /></marker></defs></svg><button v-for="node in selected.nodes" :key="node.id" class="node" :class="node.kind" :style="nodeStyle(node)" :title="node.kind==='kpi'?'雙擊開啟 KPI 詳情（DEV）':''" @dblclick="openKpi(node)"><template v-if="node.kind==='kpi'"><b>{{ kpiFor(node)?.name || node.label }}</b><span>{{ kpiFor(node)?.description || 'KPI 說明' }}</span><small>Target {{ kpiFor(node)?.target }}{{ kpiFor(node)?.targetType==='Percentage'?'%':'' }}</small><strong>Actual {{ kpiFor(node)?.actual ?? 'ERROR' }}{{ kpiFor(node)?.targetType==='Percentage'?'%':'' }}</strong></template><template v-else><b>{{ node.label }}</b><span>{{ node.text }}</span></template></button></div></div><footer>{{ savedLogs.length }} 筆 DEV 歷程已暫存在此頁記憶體。Tree Query 每次開啟會重新計算假資料顯示。</footer></section></div>
    <div v-if="detailKpi" class="veil detail-veil"><section class="detail"><header><h2>{{ detailKpi.name }} ({{ detailKpi.no }})</h2><button @click="detailKpi=null">×</button></header><dl><dt>Description</dt><dd>{{ detailKpi.description }}</dd><dt>Target</dt><dd>{{ detailKpi.target }} {{ detailKpi.targetType }}</dd><dt>Range</dt><dd>{{ detailKpi.low ?? '—' }} to {{ detailKpi.high ?? '—' }}</dd><dt>DEV Actual</dt><dd>{{ detailKpi.actual ?? 'Script preview unavailable' }}</dd><dt>Report / Chart</dt><dd>{{ detailKpi.reportId || '—' }} / {{ detailKpi.chartId || '—' }}</dd><dt>ActualValueScript</dt><dd><code>{{ detailKpi.script || '—' }}</code><small>僅展示，沒有執行</small></dd><dt>Tolerance rule code</dt><dd>{{ detailKpi.tolerance?.operator ?? '—' }} (ValueList 顯示文字待查)</dd></dl><p>實際雙擊會開啟 RT_Script；DEV 只展示其傳入參數，不呼叫 Script／Report API。</p><button @click="detailKpi=null">Close</button></section></div>
  </section>
</template>

<style scoped>
.query-page{height:100%;min-height:400px;display:flex;flex-direction:column;gap:8px;padding:12px;box-sizing:border-box;background:#f7f5ee;color:#292820;font:13px/1.4 "Segoe UI",Arial,sans-serif}.head,.head>div,.search,.head button,.viewer header,.viewer header>div,.tools,.detail header{display:flex;align-items:center;gap:8px}.head{justify-content:space-between;border-bottom:1px solid #cbc5b4;padding-bottom:7px}.head h1{margin:0;font-size:18px}.head h1 small{color:#777;font-size:12px}.head p{margin:0;color:#777}.head>button,.search button,.tools button,.detail button{min-height:27px;padding:3px 10px;border:1px solid #777365;border-radius:3px;background:#fff;color:#302f29;font:inherit;cursor:pointer}.search label{display:flex;align-items:center;gap:5px}.search input{width:220px;padding:5px;border:1px solid #aaa594;font:inherit}.search span{color:#777}.notice{margin:0;padding:5px 8px;background:#eee9d9;color:#59574b}.result-layout{display:flex;min-height:150px;flex:1;gap:8px}.table-wrap{flex:1;overflow:auto;border:1px solid #aaa594;background:#fff}table{width:100%;border-collapse:collapse;white-space:nowrap}th,td{padding:6px 8px;border:1px solid #d2cebf;text-align:left}th{position:sticky;top:0;background:#e9e5d7}tr.selected{background:#e7eef7}tr{cursor:pointer}.result-layout aside{width:120px;display:flex;flex-direction:column;gap:8px}.result-layout aside button{padding:7px;border:1px solid #777365;background:#39485e;color:white;cursor:pointer}.result-layout aside button:disabled{opacity:.5}.result-layout aside p{margin:0;color:#777;font-size:11px}.empty{text-align:center;color:#888;padding:20px}.veil{position:fixed;inset:0;z-index:50;display:grid;place-items:center;padding:16px;box-sizing:border-box;background:#17171377}.viewer,.detail{display:flex;flex-direction:column;max-height:96%;background:#f7f5ee;border:1px solid #555145;box-shadow:0 8px 28px #0005;box-sizing:border-box}.viewer{width:min(1200px,100%);height:min(780px,100%)}.viewer header,.detail header{justify-content:space-between;padding:8px 12px;border-bottom:1px solid #c8c2af}.viewer header h2,.detail h2{margin:0;font-size:16px}.viewer header p{margin:4px 0 0;color:#8b4e33;font-size:11px}.viewer header button,.detail header button{margin-left:auto;border:0;background:transparent;font-size:21px;cursor:pointer}.tools{padding:7px;border-bottom:1px solid #c8c2af}.tools .spacer{flex:1}.tools span{min-width:35px;text-align:center}.canvas-scroll{flex:1;overflow:auto;background-color:#fff;background-image:radial-gradient(#d5d0c2 1px,transparent 1px);background-size:18px 18px}.canvas{position:relative;min-width:900px;min-height:500px;transform-origin:top left}.links{position:absolute;inset:0;width:100%;height:100%;overflow:visible;pointer-events:none}.node{position:absolute;width:170px;min-height:90px;display:flex;flex-direction:column;align-items:flex-start;gap:3px;padding:7px 9px;border:1px solid #53647d;border-radius:3px;background:#eaf1fb;box-shadow:1px 2px 4px #0002;text-align:left;font:inherit;cursor:pointer}.node strong{color:#263c61}.node small{color:#555}.node.description{background:#fff5d6;border-color:#9d8963}.viewer footer{padding:6px 10px;color:#777;border-top:1px solid #ddd}.detail-veil{z-index:60}.detail{width:min(560px,100%);padding-bottom:10px}.detail dl{display:grid;grid-template-columns:150px 1fr;gap:8px;margin:12px 16px}.detail dt{font-weight:600}.detail dd{margin:0;overflow-wrap:anywhere}.detail dd code{display:block;max-height:130px;overflow:auto;white-space:pre-wrap}.detail dd small{display:block;color:#9c563d}.detail>p{margin:0 16px 10px;color:#8b4e33}.detail>button{align-self:flex-end;margin:0 12px}@media(max-width:700px){.search{flex-wrap:wrap}.search input{width:150px}.result-layout{flex-direction:column}.result-layout aside{width:auto;flex-direction:row}.detail dl{grid-template-columns:115px 1fr}}
</style>
