<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type NodeKind = 'kpi' | 'description'
type KpiRule = { operator: string; value: string; color: string }
type TreeNode = { id: string; kind: NodeKind; x: number; y: number; label: string; refNo?: string; text?: string; textColor?: string; backColor?: string; borderColor?: string; transparent?: boolean }
type TreeLink = { id: string; from: string; to: string; style: string }
type Kpi = {
  no: string; name: string; description: string; targetType: string; targetValue: string
  low: string; high: string; actualScript: string; reportId: string; chartId: string; marketDb: string
  marketTable: string; marketColumn: string; limitState: boolean; rules: KpiRule[]
}
type Basis = {
  id: number; no: string; name: string; description: string; state: 0 | 1
  creator: string; created: string; reviser: string; revised: string
  nodes: TreeNode[]; links: TreeLink[]; kpis: Kpi[]
}

const emit = defineEmits<{ close: [] }>()
const clone = <T,>(value: T): T => JSON.parse(JSON.stringify(value)) as T
const stamp = '2026/11/28'
const sample: Basis[] = [
  { id: 1, no: 'KPI-001', name: '工廠營運指標', description: '日常營運概況', state: 1, creator: 'DEMO', created: '2026/01/12', reviser: 'DEMO', revised: '2026/10/03', nodes: [{ id: 'n1', kind: 'kpi', x: 80, y: 72, label: '良率', refNo: 'YIELD' }, { id: 'n2', kind: 'description', x: 330, y: 80, label: '當日目標' }], links: [{ id: 'l1', from: 'n1', to: 'n2', style: 'Straight' }], kpis: [{ no: 'YIELD', name: '良率', description: '合格數 / 投入數', targetType: 'Percentage', targetValue: '98', low: '95', high: '100', actualScript: 'SELECT ...', reportId: 'RPT-YIELD', chartId: 'CHART-YIELD', marketDb: 'MES', marketTable: 'TBLWIPLOTLOG', marketColumn: 'GOODQTY', limitState: true, rules: [{ operator: '<', value: '95', color: '#d9534f' }] }] },
  { id: 2, no: 'KPI-002', name: '品質指標', description: '品質部門檢視', state: 0, creator: 'DEMO', created: '2025/12/18', reviser: 'DEMO', revised: '2026/03/10', nodes: [{ id: 'n3', kind: 'kpi', x: 120, y: 100, label: '不良率', refNo: 'DEFECT' }], links: [], kpis: [{ no: 'DEFECT', name: '不良率', description: '', targetType: 'Percentage', targetValue: '1', low: '', high: '2', actualScript: '', reportId: '', chartId: '', marketDb: '', marketTable: '', marketColumn: '', limitState: false, rules: [] }] },
  { id: 3, no: 'KPI-003', name: '設備效率', description: '', state: 1, creator: 'DEMO', created: '2026/06/01', reviser: 'DEMO', revised: '2026/08/21', nodes: [], links: [], kpis: [] },
]
const rows = ref<Basis[]>(clone(sample))
const queryNo = ref('')
const queryName = ref('')
const selectedIds = ref<number[]>([1])
const message = ref('DEV 假資料；所有變更僅保存在此頁面記憶體。')
const filteredRows = computed(() => rows.value.filter(row => row.no.toLowerCase().includes(queryNo.value.trim().toLowerCase()) && row.name.toLowerCase().includes(queryName.value.trim().toLowerCase())).sort((a, b) => a.no.localeCompare(b.no)))
const selectedRows = computed(() => selectedIds.value.map(id => rows.value.find(row => row.id === id)).filter((row): row is Basis => !!row))
const current = computed(() => selectedRows.value[0] ?? null)
const editable = computed(() => current.value?.state === 1)
const nextId = ref(4)

const basisDialog = ref(false)
const basisMode = ref<'Add' | 'Edit' | 'Copy'>('Add')
const basisForm = ref({ id: 0, no: '', name: '', description: '' })
function openBasis(mode: 'Add' | 'Edit' | 'Copy') {
  const source = current.value
  if (mode !== 'Add' && !source) return
  if (mode === 'Edit' && source?.state !== 1) return
  basisMode.value = mode
  basisForm.value = mode === 'Add' ? { id: 0, no: '', name: '', description: '' } : { id: mode === 'Copy' ? 0 : source!.id, no: mode === 'Copy' ? `${source!.no}-COPY` : source!.no, name: mode === 'Copy' ? `${source!.name} (Copy)` : source!.name, description: source!.description }
  basisDialog.value = true
}
function saveBasis() {
  const form = basisForm.value
  if (!form.no.trim() || !form.name.trim()) { message.value = 'KPITreeNo 與 KPITreeName 為必填。'; return }
  if (form.no.length > 50 || form.name.length > 50) { message.value = 'KPITreeNo 與 KPITreeName 最長 50 字。'; return }
  if (rows.value.some(row => row.no.toLowerCase() === form.no.trim().toLowerCase() && row.id !== form.id)) { message.value = 'KPITreeNo 已存在。'; return }
  if (basisMode.value === 'Edit') {
    const row = rows.value.find(item => item.id === form.id)
    if (!row || row.state !== 1) return
    Object.assign(row, { no: form.no.trim(), name: form.name.trim(), description: form.description, reviser: 'DEV USER', revised: stamp })
    selectedIds.value = [row.id]
  } else {
    const source = current.value
    const row: Basis = basisMode.value === 'Copy' && source ? { ...clone(source), id: nextId.value++, no: form.no.trim(), name: form.name.trim(), description: form.description, creator: 'DEV USER', created: stamp, reviser: 'DEV USER', revised: stamp } : { id: nextId.value++, no: form.no.trim(), name: form.name.trim(), description: form.description, state: 1, creator: 'DEV USER', created: stamp, reviser: 'DEV USER', revised: stamp, nodes: [], links: [], kpis: [] }
    rows.value.push(row)
    selectedIds.value = [row.id]
  }
  basisDialog.value = false
  message.value = `${basisMode.value} 已在本頁假資料中完成。`
}

function toggleSelect(id: number, checked: boolean) {
  selectedIds.value = checked ? [...new Set([...selectedIds.value, id])] : selectedIds.value.filter(value => value !== id)
}
function deleteSelected() {
  if (!selectedRows.value.length) return
  let removed = 0
  const skippedFrozen: string[] = []
  for (const row of [...selectedRows.value]) {
    if (row.state !== 1) { skippedFrozen.push(row.no); continue }
    rows.value = rows.value.filter(item => item.id !== row.id)
    selectedIds.value = selectedIds.value.filter(id => id !== row.id)
    removed++
  }
  if (removed || skippedFrozen.length) message.value = `已在 DEV 假資料刪除 ${removed} 筆${skippedFrozen.length ? `；凍結資料未刪除：${skippedFrozen.join(', ')}` : ''}。`
}
function changeState(state: 0 | 1) {
  const row = current.value
  if (!row || (state === 0 ? row.state !== 1 : row.state !== 0)) return
  row.state = state
  row.reviser = 'DEV USER'
  row.revised = stamp
  message.value = state === 0 ? '已模擬 Freeze（0=凍結）。' : '已模擬 Unfreeze（1=可編輯）。'
}

const activeDialog = ref<'draw' | 'preview' | null>(null)
const draft = ref<Basis | null>(null)
const selectedNodeId = ref('')
const selectedLinkId = ref('')
const linkStyle = ref('Straight')
const zoom = ref(100)
const kpiDialog = ref(false)
const descDialog = ref(false)
const editingNodeId = ref('')
const kpiForm = ref<Kpi>(emptyKpi())
const descForm = ref({ label: '', text: '', textColor: '#303030', backColor: '#fff5d6', borderColor: '#756b55', transparent: false })

function emptyKpi(): Kpi { return { no: '', name: '', description: '', targetType: 'Number', targetValue: '', low: '', high: '', actualScript: '', reportId: '', chartId: '', marketDb: '', marketTable: '', marketColumn: '', limitState: false, rules: [] } }
function openDraw(preview = false) {
  if (!current.value) return
  if (!preview && current.value.state !== 1) { message.value = '凍結資料只能預覽，不能編輯指標樹。'; return }
  draft.value = clone(current.value)
  activeDialog.value = preview ? 'preview' : 'draw'
  selectedNodeId.value = ''
  selectedLinkId.value = ''
  zoom.value = 100
}
function addNode(kind: NodeKind) {
  if (!draft.value || activeDialog.value !== 'draw') return
  const index = draft.value.nodes.length + 1
  const id = `n-${Date.now()}-${index}`
  draft.value.nodes.push({ id, kind, x: 34 + ((index - 1) % 4) * 215, y: 30 + Math.floor((index - 1) / 4) * 150, label: kind === 'kpi' ? `KPI ${index}` : `說明 ${index}` })
  selectedNodeId.value = id
  if (kind === 'kpi') { kpiForm.value = emptyKpi(); editingNodeId.value = id; kpiDialog.value = true }
  else { descForm.value = { label: `TEXT${index}`, text: '', textColor: '#303030', backColor: '#fff5d6', borderColor: '#756b55', transparent: false }; editingNodeId.value = id; descDialog.value = true }
}
function beginEditNode(node: TreeNode) {
  if (activeDialog.value !== 'draw' || !draft.value) return
  selectedNodeId.value = node.id
  if (node.kind === 'kpi') {
    const item = draft.value.kpis.find(kpi => kpi.no === node.refNo)
    kpiForm.value = item ? clone(item) : emptyKpi()
    editingNodeId.value = node.id
    kpiDialog.value = true
  } else {
    descForm.value = { label: node.label, text: node.text ?? node.label, textColor: node.textColor ?? '#303030', backColor: node.backColor ?? '#fff5d6', borderColor: node.borderColor ?? '#756b55', transparent: node.transparent ?? false }
    editingNodeId.value = node.id
    descDialog.value = true
  }
}
function saveKpi() {
  const item = kpiForm.value
  if (!item.no.trim() || !item.name.trim() || !item.description.trim()) { message.value = 'KPI No、名稱與說明皆為必填。'; return }
  const currentNode = draft.value?.nodes.find(node => node.id === editingNodeId.value)
  if (draft.value?.kpis.some(kpi => kpi.no.toUpperCase() === item.no.trim().toUpperCase() && kpi.no !== currentNode?.refNo)) { message.value = 'KPINo 必須唯一。'; return }
  const target = item.targetValue === '' ? null : Number(item.targetValue)
  const low = item.low === '' ? null : Number(item.low)
  const high = item.high === '' ? null : Number(item.high)
  if ([target, low, high].some(value => value !== null && !Number.isFinite(value))) { message.value = '目標值與上下限需為數值。'; return }
  if ((low === null) !== (high === null) || (low !== null && high !== null && high < low)) { message.value = '範圍上下限必須成對輸入，且上限不可小於下限。'; return }
  if (target === null && low === null) { message.value = '至少輸入目標值或完整範圍。'; return }
  if (target !== null && low !== null && high !== null && (target < low || target > high)) { message.value = '目標值必須位於設定範圍內。'; return }
  if (item.targetType === 'Percentage' && [target, low, high].some(value => value !== null && value < 0)) { message.value = '百分比目標不接受負值。'; return }
  if (!draft.value) return
  const node = draft.value.nodes.find(value => value.id === editingNodeId.value)
  const oldNo = node?.refNo
  if (node) { node.refNo = item.no.trim(); node.label = item.name.trim() }
  const oldIndex = draft.value.kpis.findIndex(value => value.no === oldNo)
  const normalized = { ...clone(item), no: item.no.trim(), name: item.name.trim() }
  if (oldIndex >= 0) draft.value.kpis.splice(oldIndex, 1, normalized)
  else draft.value.kpis.push(normalized)
  kpiDialog.value = false
  message.value = 'KPI 設定已保存在未儲存的 DEV 畫布草稿。'
}
function saveDescription() {
  const node = draft.value?.nodes.find(value => value.id === editingNodeId.value)
  if (!node) return
  if (!descForm.value.label.trim() || !descForm.value.text.trim()) { message.value = '說明控制項的 No 與文字為必填。'; return }
  node.label = descForm.value.label.trim()
  node.text = descForm.value.text
  node.textColor = descForm.value.textColor
  node.backColor = descForm.value.backColor
  node.borderColor = descForm.value.borderColor
  node.transparent = descForm.value.transparent
  descDialog.value = false
}
function addRule() { kpiForm.value.rules.push({ operator: '<', value: '', color: '#d9534f' }) }
function removeRule(index: number) { kpiForm.value.rules.splice(index, 1) }
const awaitingLink = ref(false)
function startLink() { awaitingLink.value = true; message.value = 'Ctrl 點選目標節點建立連線。' }
function pickNode(event: MouseEvent, node: TreeNode) {
  if (awaitingLink.value && event.ctrlKey && draft.value) {
    if (selectedNodeId.value !== node.id) draft.value.links.push({ id: `l-${Date.now()}`, from: selectedNodeId.value, to: node.id, style: linkStyle.value })
    awaitingLink.value = false
    return
  }
  selectedLinkId.value = ''
  selectedNodeId.value = node.id
}
const drag = ref<{ id: string; x: number; y: number; left: number; top: number } | null>(null)
function startDrag(event: PointerEvent, node: TreeNode) {
  if (activeDialog.value !== 'draw' || (event.target as HTMLElement).closest('button')) return
  const board = (event.currentTarget as HTMLElement).parentElement
  if (!board) return
  drag.value = { id: node.id, x: event.clientX, y: event.clientY, left: node.x, top: node.y }
  ;(event.currentTarget as HTMLElement).setPointerCapture(event.pointerId)
  window.addEventListener('pointermove', moveDrag)
  window.addEventListener('pointerup', endDrag, { once: true })
  function moveDrag(move: PointerEvent) {
    if (!drag.value || !draft.value) return
    const target = draft.value.nodes.find(value => value.id === drag.value?.id)
    if (target) { target.x = Math.max(0, drag.value.left + (move.clientX - drag.value.x) * 100 / zoom.value); target.y = Math.max(0, drag.value.top + (move.clientY - drag.value.y) * 100 / zoom.value) }
  }
  function endDrag() { drag.value = null; window.removeEventListener('pointermove', moveDrag) }
}
function removeSelectedNode() {
  if (!draft.value || activeDialog.value !== 'draw') return
  if (selectedLinkId.value) {
    draft.value.links = draft.value.links.filter(link => link.id !== selectedLinkId.value)
    selectedLinkId.value = ''
    return
  }
  if (!selectedNodeId.value) return
  const node = draft.value.nodes.find(value => value.id === selectedNodeId.value)
  if (node?.kind === 'kpi') draft.value.kpis = draft.value.kpis.filter(kpi => kpi.no !== node.refNo)
  draft.value.nodes = draft.value.nodes.filter(value => value.id !== selectedNodeId.value)
  draft.value.links = draft.value.links.filter(link => link.from !== selectedNodeId.value && link.to !== selectedNodeId.value)
  selectedNodeId.value = ''
}
function onCanvasKey(event: KeyboardEvent) { if (event.key === 'Delete') { event.preventDefault(); removeSelectedNode() } }
function saveTree() {
  if (!draft.value || activeDialog.value !== 'draw') return
  if (draft.value.nodes.some(node => node.kind === 'kpi' && (!node.refNo || !draft.value?.kpis.some(kpi => kpi.no === node.refNo)))) { message.value = '請完成所有 KPI Control 定義後再儲存。'; return }
  if (draft.value.nodes.some(node => node.kind === 'description' && !node.text?.trim())) { message.value = '請完成所有 Descript Control 內容後再儲存。'; return }
  const row = rows.value.find(value => value.id === draft.value?.id)
  if (!row || row.state !== 1) return
  row.nodes = clone(draft.value.nodes)
  row.links = clone(draft.value.links)
  row.kpis = clone(draft.value.kpis)
  row.reviser = 'DEV USER'
  row.revised = stamp
  activeDialog.value = null
  message.value = '樹狀圖與明細已存入本頁假資料；正式服務需以單一交易保存。'
}
function closeDraw() { activeDialog.value = null; draft.value = null; awaitingLink.value = false }
function nodeStyle(node: TreeNode) { return { left: `${node.x}px`, top: `${node.y}px`, color: node.textColor, background: node.transparent ? 'transparent' : node.backColor, borderColor: node.borderColor } }
function nodeById(id: string) { return draft.value?.nodes.find(node => node.id === id) }
function kpiForNode(node: TreeNode) { return draft.value?.kpis.find(kpi => kpi.no === node.refNo) }
function linkPath(link: TreeLink) {
  const from = nodeById(link.from), to = nodeById(link.to)
  if (!from || !to) return ''
  const sx = from.x + 70, sy = from.y + 35, ex = to.x + 70, ey = to.y + 35, mx = (sx + ex) / 2, my = (sy + ey) / 2
  if (link.style === 'VH') return `M ${sx} ${sy} L ${sx} ${my} L ${ex} ${my} L ${ex} ${ey}`
  if (link.style === 'HV') return `M ${sx} ${sy} L ${mx} ${sy} L ${mx} ${ey} L ${ex} ${ey}`
  if (link.style === 'VHV') return `M ${sx} ${sy} L ${sx} ${my} L ${mx} ${my} L ${mx} ${ey} L ${ex} ${ey}`
  if (link.style === 'HVH') return `M ${sx} ${sy} L ${mx} ${sy} L ${mx} ${my} L ${ex} ${my} L ${ex} ${ey}`
  return `M ${sx} ${sy} L ${ex} ${ey}`
}
watch(activeDialog, value => { if (value !== 'draw') awaitingLink.value = false })
</script>

<template>
  <section class="kpi-page" aria-label="指標樹基本資料設定 DEV 預覽">
    <header class="page-head"><div><h1>指標樹基本資料設定 <small>RT904</small></h1><p>DEV 假資料互動預覽</p></div><button class="close" @click="emit('close')">Close (X)</button></header>
    <div class="search"><label>KPITreeNo <input v-model="queryNo" maxlength="50" @keyup.enter="message='查詢完成（DEV 假資料）。'" /></label><label>KPITreeName <input v-model="queryName" maxlength="50" @keyup.enter="message='查詢完成（DEV 假資料）。'" /></label><button @click="message='查詢完成（DEV 假資料）。'">Query</button></div>
    <nav class="toolbar" aria-label="指標樹操作"><button @click="openBasis('Add')">Add</button><button :disabled="!editable" @click="openBasis('Edit')">Edit</button><button :disabled="!current" @click="openBasis('Copy')">Copy</button><button :disabled="!selectedRows.length" @click="deleteSelected">Delete</button><span class="separator"></span><button :disabled="!current || current.state !== 1" @click="changeState(0)">Freeze</button><button :disabled="!current || current.state !== 0" @click="changeState(1)">Unfreeze</button><span class="separator"></span><button :disabled="!current" @click="openDraw(true)">Preview</button><button :disabled="!editable" @click="openDraw(false)">Draw KPI Tree</button></nav>
    <p class="notice" role="status">{{ message }}</p>
    <div class="grid-wrap"><table><thead><tr><th class="check"></th><th>KPITreeNo</th><th>KPITreeName</th><th>Description</th><th>FreezeState</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead><tbody><tr v-for="row in filteredRows" :key="row.id" :class="{ chosen: selectedIds.includes(row.id) }" @click="selectedIds=[row.id]"><td><input type="checkbox" :checked="selectedIds.includes(row.id)" :aria-label="`選取 ${row.no}`" @click.stop="toggleSelect(row.id, !selectedIds.includes(row.id))" /></td><td>{{ row.no }}</td><td>{{ row.name }}</td><td>{{ row.description }}</td><td><span :class="['state', row.state ? 'open' : 'frozen']">{{ row.state ? '1 UnFreeze' : '0 Freeze' }}</span></td><td>{{ row.creator }}</td><td>{{ row.created }}</td><td>{{ row.reviser }}</td><td>{{ row.revised }}</td></tr><tr v-if="!filteredRows.length"><td colspan="9" class="empty">沒有符合資料</td></tr></tbody></table></div>
    <div class="preview-area"><h2>指標樹預覽 <span v-if="current">{{ current.no }} — {{ current.name }}</span></h2><div v-if="current?.nodes.length" class="mini-canvas"><div v-for="node in current.nodes" :key="node.id" class="mini-node" :class="node.kind" :style="{left:`${node.x/2}px`,top:`${node.y/2}px`}">{{ node.label }}</div></div><div v-else class="empty">選取一筆資料查看已儲存的節點。</div></div>

    <div v-if="basisDialog" class="veil" @click.self="basisDialog=false"><section class="dialog small"><header><h2>{{ basisMode }} 指標樹</h2><button @click="basisDialog=false">×</button></header><label>KPITreeNo <input v-model="basisForm.no" maxlength="50" /></label><label>KPITreeName <input v-model="basisForm.name" maxlength="50" /></label><label>Description <textarea v-model="basisForm.description" rows="3"></textarea></label><footer><button @click="basisDialog=false">Cancel</button><button class="primary" @click="saveBasis">Save</button></footer></section></div>

    <div v-if="activeDialog" class="veil editor-veil"><section class="dialog editor"><header><div><h2>{{ activeDialog === 'draw' ? '指標樹編輯' : '指標樹預覽' }} — {{ draft?.no }} {{ draft?.name }}</h2><small>{{ activeDialog === 'draw' ? '拖曳節點；Ctrl 點選目標節點建立連線。選取連線或節點後按 Delete 移除。' : '唯讀預覽' }}</small></div><button @click="closeDraw">×</button></header><div v-if="activeDialog==='draw'" class="graph-tools"><button @click="addNode('kpi')">KPI Control</button><button @click="addNode('description')">Descript Control</button><button :disabled="!selectedNodeId" @click="startLink">連線</button><select v-model="linkStyle"><option>Straight</option><option>VH</option><option>HV</option><option>VHV</option><option>HVH</option></select><span class="spacer"></span><button @click="zoom=Math.max(25,zoom-25)">−</button><span>{{ zoom }}%</span><button @click="zoom=Math.min(300,zoom+25)">+</button><button @click="zoom=100">Reset</button><button class="primary" @click="saveTree">Save</button><button @click="closeDraw">Cancel</button></div><div v-else class="graph-tools"><span>Zoom</span><button @click="zoom=Math.max(25,zoom-25)">−</button><span>{{ zoom }}%</span><button @click="zoom=Math.min(300,zoom+25)">+</button><button @click="zoom=100">Reset</button><button @click="closeDraw">Close</button></div><div class="canvas-viewport" tabindex="0" @keydown="onCanvasKey"><div class="canvas" :style="{ transform:`scale(${zoom/100})`, width:`${100/zoom*100}%`, height:`${100/zoom*100}%` }"><svg class="links"><path v-for="link in draft?.links" :key="link.id" :d="linkPath(link)" fill="none" :stroke="selectedLinkId===link.id ? '#c35335' : '#56657b'" stroke-width="2" marker-end="url(#arrow)" @click.stop="selectedLinkId=link.id;selectedNodeId=''" /><defs><marker id="arrow" markerWidth="10" markerHeight="10" refX="7" refY="3" orient="auto"><path d="M0,0 L0,6 L8,3 z" fill="#56657b" /></marker></defs></svg><div v-for="node in draft?.nodes" :key="node.id" class="graph-node" :class="[node.kind,{ selected:selectedNodeId===node.id }]" :style="nodeStyle(node)" @pointerdown="startDrag($event,node)" @click.stop="pickNode($event,node)" @dblclick.stop="beginEditNode(node)"><template v-if="node.kind==='kpi'"><span>{{ node.label }}</span><small>{{ kpiForNode(node)?.description || 'KPI 說明' }}</small><small>Target: {{ kpiForNode(node)?.targetValue || '—' }} {{ kpiForNode(node)?.targetType || '' }}</small></template><template v-else><span>{{ node.label }}</span><small>{{ node.text || node.label }}</small></template><button v-if="activeDialog==='draw'" class="node-edit" title="編輯" @click.stop="beginEditNode(node)">⋯</button></div></div></div><footer class="editor-foot">{{ activeDialog==='draw' ? '未保存的修改只存在於 DEV 畫布草稿。' : '正式預覽需由家用端 API 載入。' }}</footer></section></div>

    <div v-if="kpiDialog" class="veil top-veil"><section class="dialog kpi-dialog"><header><h2>KPI 定義</h2><button @click="kpiDialog=false">×</button></header><div class="form-grid"><label>KPI No * <input v-model="kpiForm.no" /></label><label>KPI Name * <input v-model="kpiForm.name" /></label><label class="wide">KPI Description * <textarea v-model="kpiForm.description" rows="2"></textarea></label><label>Target Type <select v-model="kpiForm.targetType"><option>Number</option><option>Percentage</option></select></label><label>Target Value <input v-model="kpiForm.targetValue" inputmode="decimal" /></label><label>Range Low <input v-model="kpiForm.low" inputmode="decimal" /></label><label>Range Up <input v-model="kpiForm.high" inputmode="decimal" /></label><label class="wide">ActualValueScript（僅展示，DEV 不執行）<textarea v-model="kpiForm.actualScript" rows="2"></textarea></label><label>Report ID <select v-model="kpiForm.reportId"><option value="">-- 請選擇 --</option><option>RPT-YIELD</option><option>RPT-DEFECT</option><option>RPT-OEE</option></select></label><label>Chart ID <select v-model="kpiForm.chartId"><option value="">-- 請選擇 --</option><option>CHART-YIELD</option><option>CHART-OEE</option></select></label><label>ReportMarket DB <select v-model="kpiForm.marketDb"><option value="">--</option><option>MES</option><option>ERP</option></select></label><label>Market Table <input v-model="kpiForm.marketTable" placeholder="假資料選項" /></label><label>Market Column <input v-model="kpiForm.marketColumn" placeholder="假資料選項" /></label><div class="wide fake-test"><button @click="message='Script Test 僅為預覽：不執行 ActualValueScript，也不連資料庫。'">Test Script（預覽）</button><span>正式端不可直接執行未授權公式。</span></div><div class="wide rule-head"><label><input v-model="kpiForm.limitState" type="checkbox" />啟用容忍值規則</label><button :disabled="!kpiForm.limitState" @click="addRule">新增規則</button></div><div v-for="(rule,index) in kpiForm.rules" :key="index" class="wide rule-row"><select v-model="rule.operator"><option>&lt;</option><option>&lt;=</option><option>=</option><option>&gt;=</option><option>&gt;</option></select><input v-model="rule.value" placeholder="數值" /><input v-model="rule.color" type="color" /><button @click="removeRule(index)">刪除</button></div></div><footer><button @click="kpiDialog=false">Cancel</button><button class="primary" @click="saveKpi">Save</button></footer></section></div>

    <div v-if="descDialog" class="veil top-veil"><section class="dialog small"><header><h2>說明控制項</h2><button @click="descDialog=false">×</button></header><label>KPI No * <input v-model="descForm.label" maxlength="50" @blur="descForm.label=descForm.label.toUpperCase()" /></label><label>Description * <textarea v-model="descForm.text" rows="4"></textarea></label><div class="colors"><label>文字 <input v-model="descForm.textColor" type="color" /></label><label>背景 <input v-model="descForm.backColor" type="color" /></label><label>外框 <input v-model="descForm.borderColor" type="color" /></label><label><input v-model="descForm.transparent" type="checkbox" />透明背景</label></div><footer><button @click="descDialog=false">Cancel</button><button class="primary" @click="saveDescription">Save</button></footer></section></div>
  </section>
</template>

<style scoped>
.kpi-page{height:100%;min-height:420px;display:flex;flex-direction:column;gap:8px;padding:12px;box-sizing:border-box;background:#f7f5ee;color:#292820;font:13px/1.35 "Segoe UI",Arial,sans-serif}.page-head,.page-head>div,.toolbar,.search,.dialog header,.dialog footer,.graph-tools,.editor-foot{display:flex;align-items:center;gap:8px}.page-head{justify-content:space-between;border-bottom:1px solid #cec9b9;padding-bottom:7px}.page-head h1{margin:0;font-size:18px}.page-head h1 small{font-size:12px;color:#777}.page-head p{margin:0;color:#777}.close,.dialog header>button{margin-left:auto}.search label,.dialog label{display:flex;align-items:center;gap:6px}.search input,.dialog input:not([type=checkbox]):not([type=color]),.dialog textarea,.dialog select{min-width:120px;padding:5px 7px;border:1px solid #aaa594;border-radius:2px;background:white;color:#292820;font:inherit}.search button,.toolbar button,.dialog button,.graph-tools button{min-height:27px;padding:3px 9px;border:1px solid #777365;border-radius:3px;background:#fff;color:#302f29;font:inherit;cursor:pointer}.toolbar{padding:5px 0}.toolbar button:disabled,.graph-tools button:disabled{opacity:.45;cursor:not-allowed}.separator{height:20px;border-left:1px solid #aaa;margin:0 3px}.notice{margin:0;padding:5px 8px;background:#eee9d9;color:#5b584b}.grid-wrap{flex:1;min-height:130px;overflow:auto;border:1px solid #aaa594;background:#fff}table{width:100%;border-collapse:collapse;white-space:nowrap}th,td{padding:5px 7px;border:1px solid #d2cebf;text-align:left}th{position:sticky;top:0;background:#e9e5d7;font-weight:600}tr.chosen{background:#e7eef7}.check{width:25px}.state{padding:2px 5px;border-radius:3px}.state.open{background:#e0eedc;color:#37652f}.state.frozen{background:#eee1da;color:#824739}.empty{text-align:center;color:#888;padding:16px}.preview-area{height:130px;flex:none;border:1px solid #aaa594;background:#fff;position:relative;overflow:auto}.preview-area h2{position:sticky;top:0;margin:0;padding:5px 8px;background:#e9e5d7;font-size:13px}.preview-area h2 span{margin-left:10px;color:#777;font-weight:400}.mini-canvas{position:relative;min-height:90px}.mini-node{position:absolute;min-width:90px;padding:6px 10px;border:1px solid #627087;border-radius:3px;background:#eef4fc;box-shadow:1px 2px 3px #0002}.mini-node.description{background:#fff5d6;border-color:#9d8963}.veil{position:fixed;inset:0;z-index:40;display:grid;place-items:center;background:#17171366;padding:16px;box-sizing:border-box}.top-veil{z-index:60}.dialog{display:flex;flex-direction:column;max-height:96%;background:#f7f5ee;border:1px solid #555145;box-shadow:0 8px 28px #0005;box-sizing:border-box}.dialog header{padding:9px 12px;border-bottom:1px solid #c8c2af}.dialog h2{margin:0;font-size:16px}.dialog header small{display:block;color:#777;margin-top:3px}.dialog footer{justify-content:flex-end;padding:8px 12px;border-top:1px solid #c8c2af}.dialog .primary{background:#37465e;color:#fff;border-color:#37465e}.small{width:min(480px,100%);padding:0 12px 8px}.small>label{display:grid;grid-template-columns:130px 1fr;margin:10px 0}.small>label input,.small>label textarea{width:100%;box-sizing:border-box}.editor{width:min(1200px,100%);height:min(780px,100%)}.graph-tools{padding:7px;border-bottom:1px solid #c8c2af}.graph-tools .spacer{flex:1}.canvas-viewport{flex:1;overflow:auto;background-color:#fff;background-image:radial-gradient(#d5d0c2 1px,transparent 1px);background-size:18px 18px;outline:none}.canvas{position:relative;min-width:900px;min-height:500px;transform-origin:top left}.links{position:absolute;inset:0;width:100%;height:100%;overflow:visible;pointer-events:none}.graph-node{position:absolute;width:140px;min-height:50px;display:flex;align-items:center;justify-content:space-between;padding:7px 9px;box-sizing:border-box;border:1px solid #53647d;border-radius:3px;background:#eaf1fb;box-shadow:1px 2px 4px #0002;cursor:move;touch-action:none}.graph-node.description{background:#fff5d6;border-color:#9d8963}.graph-node.selected{outline:2px solid #4278bc}.node-edit{margin-left:auto;padding:0 4px!important;min-height:20px!important}.editor-foot{padding:5px 10px;color:#777}.kpi-dialog{width:min(760px,100%);padding:0 12px 8px;overflow:auto}.form-grid{display:grid;grid-template-columns:1fr 1fr;gap:9px 14px;padding:12px 2px;overflow:auto}.form-grid>label{display:grid;grid-template-columns:140px 1fr}.form-grid .wide{grid-column:1/-1}.form-grid input,.form-grid textarea,.form-grid select{width:100%;box-sizing:border-box}.fake-test{display:flex;align-items:center;gap:10px;color:#8a4d2f}.rule-head{display:flex;justify-content:space-between;align-items:center;padding-top:8px;border-top:1px solid #c8c2af}.rule-head label{display:flex;align-items:center}.rule-row{display:grid;grid-template-columns:1fr 1fr 60px auto;gap:6px}.colors{display:grid;grid-template-columns:1fr 1fr;gap:8px;padding:8px}.colors input[type=color]{width:34px;height:26px}.colors label{justify-content:space-between}.colors label:last-child{justify-content:flex-start}@media(max-width:740px){.search{flex-wrap:wrap}.toolbar{flex-wrap:wrap}.form-grid{grid-template-columns:1fr}.form-grid .wide{grid-column:auto}.form-grid>label{grid-template-columns:115px 1fr}}
</style>

<style scoped>
.links path{pointer-events:stroke;cursor:pointer}
.graph-node{width:150px;min-height:68px;display:flex;flex-direction:column;align-items:flex-start;justify-content:center;gap:2px}
.graph-node small{font-size:10px;color:#65635b}
.node-edit{position:absolute;right:4px;top:3px}
</style>
