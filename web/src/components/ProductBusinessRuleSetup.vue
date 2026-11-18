<script setup lang="ts">
import { computed, ref } from 'vue'

type Row = Record<string, any>
type RuleNode = { id: string; nodeNo: string; kind: 'start' | 'rule' | 'end'; x: number; y: number; phaseNo?: number; phaseName?: string }
type RuleLink = { id: string; fromNode: string; toNode: string; linkName: string; lineStyle: string; phaseNo: number }
type OperationRule = { opNo: string; opName: string; processNo: string; processVersion: string; isNetFlow?: boolean; nodes: RuleNode[]; links: RuleLink[] }
type BusinessRuleState = { operations: OperationRule[] }

const props = withDefaults(defineProps<{
  product: Row
  processes: Row[]
  businessRules?: BusinessRuleState | null
  developmentPreview?: boolean
  canEdit?: boolean
}>(), { businessRules: null, developmentPreview: false, canEdit: false })
const emit = defineEmits<{ save: [value: BusinessRuleState]; close: [] }>()

const phaseCatalog = [
  { phaseNo: 1, phaseName: 'Before OP' }, { phaseNo: 2, phaseName: 'During OP' },
  { phaseNo: 3, phaseName: 'After OP' }, { phaseNo: 99, phaseName: 'All Phase' },
]
const ruleCatalog = [
  { ruleNo: 'DEV-BR-001', ruleName: '批號資料檢查' },
  { ruleNo: 'DEV-BR-002', ruleName: '元件條件檢查' },
  { ruleNo: 'DEV-BR-003', ruleName: '設備參數檢查' },
  { ruleNo: 'DEV-BR-004', ruleName: '完成條件檢查' },
]
const fallbackOps = [
  { processNo: 'DEV-PROC-01', processVersion: '01', opNo: 'DEV-OP-10', opName: '點膠作業' },
  { processNo: 'DEV-PROC-01', processVersion: '01', opNo: 'DEV-OP-20', opName: '組裝作業' },
  { processNo: 'DEV-PROC-02', processVersion: '01', opNo: 'DEV-OP-30', opName: '終檢作業' },
]
const mockOpDefaultTemplates: Record<string, string> = { 'DEV-OP-10': 'DEV-TEMPLATE-02', 'DEV-OP-20': 'DEV-TEMPLATE-01', 'DEV-OP-30': 'DEV-TEMPLATE-01' }
const mockTemplates: Record<string, BusinessRuleState> = {
  'DEV-TEMPLATE-01': { operations: [{ opNo: '', opName: '', processNo: '', processVersion: '', nodes: [
    { id: 'start', nodeNo: 'START', kind: 'start', x: 90, y: 160 },
    { id: 'rule-1', nodeNo: 'DEV-BR-001', kind: 'rule', x: 300, y: 160, phaseNo: 1, phaseName: 'Before OP' },
    { id: 'end-1', nodeNo: 'END', kind: 'end', x: 520, y: 160 },
  ], links: [
    { id: 'link-1', fromNode: 'start', toNode: 'rule-1', linkName: 'pass', lineStyle: 'polyline', phaseNo: 99 },
    { id: 'link-2', fromNode: 'rule-1', toNode: 'end-1', linkName: 'pass', lineStyle: 'polyline', phaseNo: 1 },
  ] }] },
  'DEV-TEMPLATE-02': { operations: [{ opNo: '', opName: '', processNo: '', processVersion: '', nodes: [
    { id: 'start', nodeNo: 'START', kind: 'start', x: 90, y: 160 },
    { id: 'rule-1', nodeNo: 'DEV-BR-001', kind: 'rule', x: 285, y: 160, phaseNo: 1, phaseName: 'Before OP' },
    { id: 'rule-2', nodeNo: 'DEV-BR-002', kind: 'rule', x: 500, y: 160, phaseNo: 2, phaseName: 'During OP' },
    { id: 'end-1', nodeNo: 'END', kind: 'end', x: 700, y: 160 },
  ], links: [
    { id: 'link-1', fromNode: 'start', toNode: 'rule-1', linkName: 'pass', lineStyle: 'VH', phaseNo: 99 },
    { id: 'link-2', fromNode: 'rule-1', toNode: 'rule-2', linkName: 'pass', lineStyle: 'HV', phaseNo: 1 },
    { id: 'link-3', fromNode: 'rule-2', toNode: 'end-1', linkName: 'pass', lineStyle: 'polyline', phaseNo: 2 },
  ] }] },
}

const clone = <T,>(value: T): T => JSON.parse(JSON.stringify(value)) as T
const readOnly = computed(() => !props.developmentPreview || !props.canEdit)
const editorReadOnly = computed(() => readOnly.value || (editorMode.value === 'edit' && draft.value.isNetFlow === true))
const state = ref<BusinessRuleState>(clone(props.businessRules ?? { operations: [] }))
const selectedOpNo = ref(state.value.operations[0]?.opNo ?? '')
const checkedOps = ref<string[]>([])
const editorOpen = ref(false)
const editorMode = ref<'add' | 'edit'>('add')
const draft = ref<OperationRule>({ opNo: '', opName: '', processNo: '', processVersion: '', nodes: [], links: [] })
const processNo = ref('')
const selectedRuleNo = ref('DEV-BR-001')
const selectedPhaseNo = ref<number | ''>(1)
const ruleAction = ref('pass')
const templateName = ref('')
const nodeDistance = ref(50)
const tool = ref('select')
const selectedNodeIds = ref<string[]>([])
const selectedLinkId = ref('')
const linkFromNodeId = ref('')
const nodeDrag = ref<{ id: string; pointerId: number; startClientX: number; startClientY: number; startX: number; startY: number } | null>(null)
const nodeDragMoved = ref(false)
const suppressNodeClick = ref(false)
const message = ref('')
const editorError = ref('')
const canvas = ref<SVGSVGElement | null>(null)
const operations = computed(() => state.value.operations)
const activeOperation = computed(() => operations.value.find(row => row.opNo === selectedOpNo.value) ?? null)
const activeNodes = computed(() => activeOperation.value?.nodes ?? [])
const activeLinks = computed(() => activeOperation.value?.links ?? [])
const editorOperationOptions = computed(() => {
  const selectedProcess = processNo.value
  const selectedVersion = draft.value.processVersion
  const process = props.processes.find(row => String(row.processNo ?? '') === selectedProcess && String(row.processVersion ?? '01') === selectedVersion)
  const candidates = fallbackOps.filter(row => row.processNo === selectedProcess && row.processVersion === selectedVersion && !state.value.operations.some(existing => existing.opNo === row.opNo))
  if (candidates.length) return candidates
  if (!process) return []
  const fallbackOpNo = `${selectedProcess}-OP`
  if (state.value.operations.some(existing => existing.opNo === fallbackOpNo)) return []
  return [{ processNo: selectedProcess, processVersion: selectedVersion, opNo: fallbackOpNo, opName: 'DEV 作業站' }]
})
const processOptions = computed(() => {
  const options = props.processes.map(row => ({ processNo: String(row.processNo ?? ''), processVersion: String(row.processVersion ?? '01'), psNo: String(row.psNo ?? '') })).filter(row => row.processNo)
  if (!options.length) return [{ processNo: 'DEV-PROC-01', processVersion: '01', psNo: 'DEV-PS-10' }]
  return options
})
const selectedNodes = computed(() => draft.value.nodes.filter(node => selectedNodeIds.value.includes(node.id)))
function makePhaseBoxes(nodes: RuleNode[]) {
  const phases = new Map<number, { phaseNo: number; name: string; nodes: RuleNode[] }>()
  for (const node of nodes.filter(row => row.kind === 'rule' && row.phaseNo !== undefined)) {
    const phaseNo = Number(node.phaseNo)
    const entry = phases.get(phaseNo) ?? { phaseNo, name: node.phaseName ?? `Phase ${phaseNo}`, nodes: [] }
    entry.nodes.push(node); phases.set(phaseNo, entry)
  }
  return [...phases.values()].map(phase => {
    const left = Math.min(...phase.nodes.map(node => node.x)) - 82
    const top = Math.min(...phase.nodes.map(node => node.y)) - 62
    const right = Math.max(...phase.nodes.map(node => node.x)) + 82
    const bottom = Math.max(...phase.nodes.map(node => node.y)) + 62
    return { ...phase, x: Math.max(2, left), y: Math.max(18, top), width: right - Math.max(2, left), height: bottom - Math.max(18, top) }
  })
}
const phaseBoxes = computed(() => makePhaseBoxes(draft.value.nodes))
const previewPhaseBoxes = computed(() => makePhaseBoxes(activeNodes.value))

function saveState(next: BusinessRuleState, text: string): void {
  state.value = clone(next); emit('save', clone(next)); message.value = text
}
function selectOperation(opNo: string, event?: MouseEvent): void {
  selectedOpNo.value = opNo
  if (event?.ctrlKey || event?.metaKey) {
    checkedOps.value = checkedOps.value.includes(opNo) ? checkedOps.value.filter(value => value !== opNo) : [...checkedOps.value, opNo]
  } else checkedOps.value = [opNo]
}
function startAdd(): void {
  if (readOnly.value) return
  editorMode.value = 'add'; editorOpen.value = true; editorError.value = ''; message.value = ''
  processNo.value = processOptions.value[0]?.processNo ?? ''
  draft.value = { opNo: '', opName: '', processNo: processNo.value, processVersion: processOptions.value[0]?.processVersion ?? '01', nodes: [], links: [] }
  selectedNodeIds.value = []; selectedLinkId.value = ''; linkFromNodeId.value = ''; tool.value = 'select'; templateName.value = ''; ruleAction.value = 'pass'
}
function startEdit(): void {
  if (readOnly.value || !activeOperation.value) return
  editorMode.value = 'edit'; editorOpen.value = true; editorError.value = ''; message.value = ''
  draft.value = clone(activeOperation.value); processNo.value = draft.value.processNo
  selectedNodeIds.value = []; selectedLinkId.value = ''; linkFromNodeId.value = ''; tool.value = 'select'; templateName.value = ''; ruleAction.value = 'pass'
}
function chooseProcess(): void {
  const process = processOptions.value.find(row => row.processNo === processNo.value)
  draft.value.processNo = processNo.value; draft.value.processVersion = process?.processVersion ?? '01'; draft.value.opNo = ''; draft.value.opName = ''
  draft.value.nodes = []; draft.value.links = []; templateName.value = ''
}
function chooseOperation(opNo: string): void {
  const selected = editorOperationOptions.value.find(row => row.opNo === opNo)
  draft.value.opNo = opNo; draft.value.opName = selected?.opName ?? 'DEV 作業站'
}
function addNode(kind: 'start' | 'rule', x: number, y: number): void {
  if (!draft.value.opNo) { editorError.value = '請先選擇 Product Process 與 OP。'; return }
  let nodeNo = kind === 'start' ? (draft.value.nodes.some(node => node.kind === 'start') ? 'END' : 'START') : selectedRuleNo.value
  if (kind === 'rule' && !nodeNo) { editorError.value = '請先選擇 Business Rule No。'; return }
  if (kind === 'rule' && draft.value.nodes.some(node => node.kind === 'rule' && node.nodeNo === nodeNo)) { editorError.value = `Rule Node「${nodeNo}」已存在；同一 OP 不可重複使用相同 Rule No。`; return }
  const nodeKind = nodeNo === 'START' ? 'start' : nodeNo === 'END' ? 'end' : 'rule'
  const id = `${nodeKind}-${Date.now()}-${Math.floor(Math.random() * 10000)}`
  draft.value.nodes.push({ id, nodeNo, kind: nodeKind, x: Math.max(4, Math.min(808, x)), y: Math.max(20, Math.min(354, y)) })
  selectedNodeIds.value = [id]; editorError.value = ''
}
function canvasClick(event: MouseEvent): void {
  const target = event.target as SVGElement
  if ((target !== event.currentTarget && !target.classList?.contains('canvas-bg')) || readOnly.value) return
  const bounds = canvas.value?.getBoundingClientRect()
  if (!bounds) return
  const x = (event.clientX - bounds.left) * 900 / bounds.width
  const y = (event.clientY - bounds.top) * 400 / bounds.height
  if (!editorReadOnly.value && tool.value === 'start-end') addNode('start', x, y)
  else if (!editorReadOnly.value && tool.value === 'rule-node') addNode('rule', x, y)
  else { selectedNodeIds.value = []; selectedLinkId.value = '' }
}
function selectOrLinkNode(event: MouseEvent, node: RuleNode): void {
  event.stopPropagation()
  if (suppressNodeClick.value) { suppressNodeClick.value = false; return }
  if (tool.value.startsWith('link-')) {
    if (!linkFromNodeId.value) { linkFromNodeId.value = node.id; editorError.value = `已選起點 ${node.nodeNo}，請再選連線終點。`; return }
    if (linkFromNodeId.value === node.id) { editorError.value = '連線起點與終點不可相同。'; linkFromNodeId.value = ''; return }
    if (!ruleAction.value.trim()) { editorError.value = 'Rule Action 必填。'; return }
    const from = draft.value.nodes.find(item => item.id === linkFromNodeId.value)
    if (!from || from.kind === 'end' || node.kind === 'start') { editorError.value = 'Start 不能作為連線終點，End 不能作為連線起點。'; linkFromNodeId.value = ''; return }
    const phaseNo = from.kind === 'start' ? 99 : Number(from.phaseNo ?? 0)
    if (from.kind === 'rule' && !phaseNo) { editorError.value = 'Rule Node 必須先 Set Phase 才能連線。'; linkFromNodeId.value = ''; return }
    const duplicate = draft.value.links.some(link => link.fromNode === from.id && link.toNode === node.id && link.linkName.toUpperCase() === ruleAction.value.trim().toUpperCase())
    if (duplicate) { editorError.value = '相同節點與 Rule Action 的連線已存在。'; linkFromNodeId.value = ''; return }
    draft.value.links.push({ id: `link-${Date.now()}-${Math.floor(Math.random() * 10000)}`, fromNode: from.id, toNode: node.id, linkName: ruleAction.value.trim().toLowerCase(), lineStyle: tool.value.replace('link-', ''), phaseNo })
    linkFromNodeId.value = ''; editorError.value = ''; return
  }
  selectedLinkId.value = ''
  if (event.ctrlKey || event.metaKey) selectedNodeIds.value = selectedNodeIds.value.includes(node.id) ? selectedNodeIds.value.filter(id => id !== node.id) : [...selectedNodeIds.value, node.id]
  else selectedNodeIds.value = [node.id]
}
function beginNodeDrag(event: PointerEvent, node: RuleNode): void {
  if (editorReadOnly.value || tool.value !== 'select') return
  event.stopPropagation()
  const group = event.currentTarget as SVGGElement
  group.setPointerCapture(event.pointerId)
  nodeDrag.value = { id: node.id, pointerId: event.pointerId, startClientX: event.clientX, startClientY: event.clientY, startX: node.x, startY: node.y }
  nodeDragMoved.value = false
  selectedNodeIds.value = [node.id]
  selectedLinkId.value = ''
}
function moveNodeDrag(event: PointerEvent): void {
  const drag = nodeDrag.value
  if (!drag || drag.pointerId !== event.pointerId) return
  const dx = event.clientX - drag.startClientX
  const dy = event.clientY - drag.startClientY
  if (!nodeDragMoved.value && Math.hypot(dx, dy) < 3) return
  nodeDragMoved.value = true
  const bounds = canvas.value?.getBoundingClientRect()
  const node = draft.value.nodes.find(item => item.id === drag.id)
  if (!bounds || !node) return
  node.x = Math.max(4, Math.min(808, drag.startX + dx * 900 / bounds.width))
  node.y = Math.max(20, Math.min(354, drag.startY + dy * 400 / bounds.height))
}
function endNodeDrag(event: PointerEvent): void {
  if (!nodeDrag.value || nodeDrag.value.pointerId !== event.pointerId) return
  suppressNodeClick.value = nodeDragMoved.value
  nodeDrag.value = null
  nodeDragMoved.value = false
}
function selectLink(event: MouseEvent, link: RuleLink): void { event.stopPropagation(); selectedNodeIds.value = []; selectedLinkId.value = link.id }
function pathFor(link: RuleLink, nodes: RuleNode[] = draft.value.nodes): string {
  const from = nodes.find(node => node.id === link.fromNode)
  const to = nodes.find(node => node.id === link.toNode)
  if (!from || !to) return ''
  const x1 = from.x + 44, y1 = from.y + 23, x2 = to.x + 44, y2 = to.y + 23
  if (link.lineStyle === 'VH') { const midY = Math.round((y1 + y2) / 2); return `M ${x1} ${y1} L ${x1} ${midY} L ${x2} ${midY} L ${x2} ${y2}` }
  if (link.lineStyle === 'HV') { const midX = Math.round((x1 + x2) / 2); return `M ${x1} ${y1} L ${midX} ${y1} L ${midX} ${y2} L ${x2} ${y2}` }
  if (link.lineStyle === 'VHV') { const a = y1 + (y2 - y1) / 3, b = y1 + (y2 - y1) * 2 / 3; return `M ${x1} ${y1} L ${x1} ${a} L ${x2} ${a} L ${x2} ${b} L ${x2} ${y2}` }
  if (link.lineStyle === 'HVH') { const a = x1 + (x2 - x1) / 3, b = x1 + (x2 - x1) * 2 / 3; return `M ${x1} ${y1} L ${a} ${y1} L ${a} ${y2} L ${b} ${y2} L ${x2} ${y2}` }
  const midX = Math.round((x1 + x2) / 2)
  return `M ${x1} ${y1} L ${midX} ${y1} L ${midX} ${y2} L ${x2} ${y2}`
}
function linkLabel(link: RuleLink, nodes: RuleNode[] = draft.value.nodes): { x: number; y: number } {
  const from = nodes.find(node => node.id === link.fromNode), to = nodes.find(node => node.id === link.toNode)
  return from && to ? { x: (from.x + to.x) / 2 + 44, y: (from.y + to.y) / 2 + 18 } : { x: 0, y: 0 }
}
function setPhase(): void {
  const phase = phaseCatalog.find(item => item.phaseNo === Number(selectedPhaseNo.value))
  const ruleNodes = selectedNodes.value.filter(node => node.kind === 'rule')
  if (!phase || !ruleNodes.length) { editorError.value = '請選擇 Phase，並至少選取一個 Rule Node。'; return }
  const ids = new Set(ruleNodes.map(node => node.id))
  for (const node of ruleNodes) { node.phaseNo = phase.phaseNo; node.phaseName = phase.phaseName }
  for (const link of draft.value.links) if (ids.has(link.fromNode)) link.phaseNo = phase.phaseNo
  editorError.value = `${ruleNodes.length} 個 Rule Node 已設定 ${phase.phaseName}。`
}
function unsetPhase(): void {
  const ruleNodes = selectedNodes.value.filter(node => node.kind === 'rule')
  if (!ruleNodes.length) { editorError.value = '請先選取要取消 Phase 的 Rule Node。'; return }
  const ids = new Set(ruleNodes.map(node => node.id))
  for (const node of draft.value.nodes) if (ids.has(node.id)) { delete node.phaseNo; delete node.phaseName }
  for (const link of draft.value.links) if (ids.has(link.fromNode)) link.phaseNo = 0
  editorError.value = `${ruleNodes.length} 個 Rule Node 已取消 Phase。`
}
function removeSelection(): void {
  if (!editorOpen.value || editorReadOnly.value) return
  const nodeIds = new Set(selectedNodeIds.value)
  const linkIds = new Set(selectedLinkId.value ? [selectedLinkId.value] : [])
  draft.value.nodes = draft.value.nodes.filter(node => !nodeIds.has(node.id))
  draft.value.links = draft.value.links.filter(link => !linkIds.has(link.id) && !nodeIds.has(link.fromNode) && !nodeIds.has(link.toNode))
  selectedNodeIds.value = []; selectedLinkId.value = ''; linkFromNodeId.value = ''; editorError.value = ''
}
function onDeleteKey(event: KeyboardEvent): void { if (event.key === 'Delete') { event.preventDefault(); removeSelection() } }
function arrangeAll(): void {
  const ordered = [...draft.value.nodes].sort((a, b) => (a.kind === 'start' ? -1 : a.kind === 'end' ? 1 : 0) - (b.kind === 'start' ? -1 : b.kind === 'end' ? 1 : 0) || a.nodeNo.localeCompare(b.nodeNo))
  ordered.forEach((node, index) => { node.x = 45 + index * Number(nodeDistance.value) * 2.2; node.y = 165 })
}
function alignNodes(axis: 'top' | 'left'): void {
  const targets = selectedNodes.value.length ? selectedNodes.value : draft.value.nodes
  if (!targets.length) return
  const value = axis === 'top' ? Math.min(...targets.map(node => node.y)) : Math.min(...targets.map(node => node.x))
  for (const node of targets) node[axis === 'top' ? 'y' : 'x'] = value
}
function loadTemplate(templateNo: string): void {
  const template = mockTemplates[templateNo]?.operations[0]
  if (!template) { templateName.value = ''; return }
  draft.value.nodes = clone(template.nodes); draft.value.links = clone(template.links); templateName.value = templateNo
  selectedNodeIds.value = []; selectedLinkId.value = ''; linkFromNodeId.value = ''; editorError.value = '已載入 DEV 假資料樣板；尚未儲存到 MES。'
}
function validateGraph(): boolean {
  if (!draft.value.processNo || !draft.value.processVersion || !draft.value.opNo) { editorError.value = 'Process No、Process Version 與 OP No 必填。'; return false }
  if (!draft.value.nodes.length) { editorError.value = '請建立至少一個圖形節點；要清除設定請使用主畫面的 DEL。'; return false }
  const startNodes = draft.value.nodes.filter(node => node.kind === 'start')
  const endNodes = draft.value.nodes.filter(node => node.kind === 'end')
  const ruleNodes = draft.value.nodes.filter(node => node.kind === 'rule')
  if (startNodes.length !== 1) { editorError.value = '每個 OP 必須且只能有一個 Start Node。'; return false }
  if (!endNodes.length) { editorError.value = '至少需要一個 End Node。'; return false }
  if (new Set(ruleNodes.map(node => node.nodeNo)).size !== ruleNodes.length) { editorError.value = '同一 OP 不可重複使用 Business Rule No。'; return false }
  if (draft.value.nodes.some(node => node.kind === 'rule' && !node.phaseNo)) { editorError.value = '所有 Rule Node 都必須歸入 Phase。'; return false }
  const nodeById = new Map(draft.value.nodes.map(node => [node.id, node]))
  if (!draft.value.links.every(link => nodeById.has(link.fromNode) && nodeById.has(link.toNode) && link.linkName.trim() && link.linkName.length <= 10 && link.phaseNo > 0)) { editorError.value = '連線端點、Rule Action（最多 10 字）或來源 Phase 不完整。'; return false }
  if (draft.value.links.some(link => nodeById.get(link.toNode)?.kind === 'start' || nodeById.get(link.fromNode)?.kind === 'end')) { editorError.value = 'Start 不可有輸入連線，End 不可有輸出連線。'; return false }
  const startId = startNodes[0].id
  const startLinks = draft.value.links.filter(link => link.fromNode === startId)
  if (startLinks.length !== 1 || startLinks[0].linkName.toUpperCase() !== 'PASS') { editorError.value = 'Start 只能有一條 PASS 輸出連線。'; return false }
  for (const node of ruleNodes) {
    const outgoing = draft.value.links.filter(link => link.fromNode === node.id)
    if (outgoing.filter(link => link.linkName.toUpperCase() === 'PASS').length !== 1) { editorError.value = `Rule Node「${node.nodeNo}」必須且只能有一條 PASS 輸出連線。`; return false }
    if (!draft.value.links.some(link => link.toNode === node.id)) { editorError.value = `Rule Node「${node.nodeNo}」至少需要一條輸入連線。`; return false }
  }
  if (draft.value.links.some(link => {
    const source = nodeById.get(link.fromNode)
    return source && link.phaseNo !== (source.kind === 'start' ? 99 : source.phaseNo)
  })) { editorError.value = '連線 Phase 必須與來源 Rule Node 的 Phase 相同；Start 使用 99。'; return false }
  if (draft.value.links.some(link => draft.value.links.filter(other => other.fromNode === link.fromNode && other.toNode === link.toNode && other.linkName.toUpperCase() === link.linkName.toUpperCase()).length > 1)) { editorError.value = 'FromNode、ToNode、Rule Action 不可重複。'; return false }
  editorError.value = ''; return true
}
function confirmEditor(): void {
  if (editorReadOnly.value || !validateGraph()) return
  if (editorMode.value === 'add' && state.value.operations.some(item => item.opNo === draft.value.opNo)) { editorError.value = '此 OP 已存在 Business Rule，請改用 Edit。'; return }
  const row: OperationRule = clone({ ...draft.value, processNo: draft.value.processNo, links: draft.value.links.map(link => ({ ...link, linkName: link.linkName.trim().toUpperCase() })) })
  const existing = state.value.operations.filter(item => item.opNo !== row.opNo)
  saveState({ operations: [...existing, row].sort((a, b) => a.opNo.localeCompare(b.opNo)) }, 'Business Rule DEV 假資料已儲存。')
  selectedOpNo.value = row.opNo; checkedOps.value = [row.opNo]; editorOpen.value = false
}
function deleteOperations(): void {
  if (readOnly.value) return
  const targets = checkedOps.value.length ? checkedOps.value : selectedOpNo.value ? [selectedOpNo.value] : []
  if (!targets.length) { message.value = '請先選取要刪除的 OP。'; return }
  if (!globalThis.confirm(`確定刪除選取的 ${targets.length} 筆 Product OP Business Rule？`)) return
  const next = state.value.operations.filter(row => !targets.includes(row.opNo))
  saveState({ operations: next }, `已從 DEV 假資料刪除 ${targets.length} 筆 OP Business Rule。`)
  selectedOpNo.value = next[0]?.opNo ?? ''; checkedOps.value = []
}
function copyDefault(): void {
  if (readOnly.value || !activeOperation.value) return
  if (!globalThis.confirm('確定以作業站的 Default Business Rule 覆蓋目前產品設定？')) return
  const selected = activeOperation.value
  const templateNo = mockOpDefaultTemplates[selected.opNo] ?? 'DEV-TEMPLATE-01'
  const template = clone(mockTemplates[templateNo].operations[0])
  const copied = { ...template, opNo: selected.opNo, opName: selected.opName, processNo: selected.processNo, processVersion: selected.processVersion }
  saveState({ operations: state.value.operations.map(row => row.opNo === selected.opNo ? copied : row) }, '已用 DEV 作業站 Default 假資料取代目前 Business Rule。')
}
function close(): void { emit('close') }
</script>

<template>
  <section class="business-rule-window">
    <header class="br-title"><span>Product OP Business Rule</span><button type="button" aria-label="關閉" @click="close">×</button></header>
    <div class="br-main">
      <section class="br-product-op">
        <fieldset class="br-group">
          <legend>Product OP</legend>
          <div class="br-op-grid">
            <table class="br-grid">
              <thead><tr><th>OPNo</th><th>OPName</th></tr></thead>
              <tbody>
                <tr v-for="row in operations" :key="row.opNo" :class="{ selected: selectedOpNo === row.opNo || checkedOps.includes(row.opNo) }" @click="selectOperation(row.opNo, $event)">
                  <td>{{ row.opNo }}</td><td>{{ row.opName }}</td>
                </tr>
                <tr v-if="!operations.length"><td colspan="2" class="empty">目前產品尚未設定 Business Rule。</td></tr>
              </tbody>
            </table>
            <div class="br-buttons">
              <button type="button" class="br-button primary" :disabled="readOnly" @click="startAdd">ADD (&amp;A)</button>
              <button type="button" class="br-button" :disabled="readOnly || !activeOperation" @click="startEdit">EDIT (&amp;E)</button>
              <button type="button" class="br-button" :disabled="readOnly || (!checkedOps.length && !activeOperation)" @click="deleteOperations">DEL (&amp;D)</button>
            </div>
          </div>
        </fieldset>
      </section>

      <section class="br-draw-view">
        <fieldset class="br-group">
          <legend>OP Business Rule</legend>
          <div v-if="activeOperation" class="br-preview-caption">{{ activeOperation.opNo }} — {{ activeOperation.opName }}　{{ activeOperation.processNo }} / {{ activeOperation.processVersion }}<span v-if="message">　{{ message }}</span></div>
          <div v-else class="br-preview-caption">選取 Product OP 後顯示商業規則流程圖。</div>
          <div class="br-preview-layout">
          <div class="br-preview-canvas">
            <svg v-if="activeOperation" viewBox="0 0 900 400" role="img" :aria-label="`${activeOperation.opNo} Business Rule`">
              <defs><marker id="br-preview-arrow" markerWidth="8" markerHeight="8" refX="6" refY="3" orient="auto"><path d="M0,0 L0,6 L7,3 z" fill="#68665f" /></marker></defs>
              <g v-for="phase in previewPhaseBoxes" :key="`preview-phase-${phase.phaseNo}`"><rect :x="phase.x" :y="phase.y" :width="phase.width" :height="phase.height" rx="8" class="phase-box"/><text :x="phase.x + 8" :y="phase.y + 17" class="phase-caption">{{ phase.name }}</text></g>
              <g v-for="link in activeOperation.links" :key="link.id"><path :d="pathFor(link, activeOperation.nodes)" class="br-link" marker-end="url(#br-preview-arrow)"/><text :x="linkLabel(link, activeOperation.nodes).x" :y="linkLabel(link, activeOperation.nodes).y" class="link-label">{{ link.linkName }}</text></g>
              <g v-for="node in activeOperation.nodes" :key="node.id" :transform="`translate(${node.x},${node.y})`">
                <ellipse v-if="node.kind !== 'rule'" cx="44" cy="23" rx="40" ry="21" :class="node.kind === 'start' ? 'start-node' : 'end-node'"/>
                <polygon v-else points="10,0 78,0 88,23 78,46 10,46 0,23" class="rule-node"/>
                <text x="44" y="28" class="node-text">{{ node.nodeNo }}</text>
              </g>
            </svg>
            <div v-else class="br-empty">No data to display.</div>
          </div>
          <div class="br-preview-actions">
            <button type="button" class="br-button" :disabled="readOnly || !activeOperation" @click="copyDefault">Default</button>
            <button type="button" class="br-button" @click="close">Close (&amp;X)</button>
          </div>
          </div>
        </fieldset>
      </section>
    </div>

    <div v-if="editorOpen" class="br-editor-overlay" @click.self="editorOpen = false">
      <section class="br-editor-window" @keydown="onDeleteKey" tabindex="0">
        <header class="br-editor-title"><span>{{ editorMode === 'add' ? 'Add Product OP Business Rule' : 'Edit Product OP Business Rule' }}</span><button type="button" aria-label="關閉" @click="editorOpen = false">×</button></header>
        <div class="br-editor-content">
          <fieldset class="br-editor-frame">
            <legend>Business Rule</legend>
            <div class="br-editor-fields">
              <label class="template-field">TemplateFlowBR<input :value="templateName" readonly><select :value="templateName" :disabled="editorReadOnly" @change="loadTemplate(($event.target as HTMLSelectElement).value)"><option value="">選擇 DEV 樣板</option><option value="DEV-TEMPLATE-01">DEV-TEMPLATE-01</option><option value="DEV-TEMPLATE-02">DEV-TEMPLATE-02</option></select></label>
              <label v-if="editorMode === 'add'">Process No<select v-model="processNo" :disabled="editorReadOnly" @change="chooseProcess"><option v-for="row in processOptions" :key="row.processNo + row.processVersion" :value="row.processNo">{{ row.processNo }} / {{ row.processVersion }} ({{ row.psNo }})</option></select></label>
              <label v-else>Product No<input :value="product.productNo" readonly></label>
              <label>Product Version<input :value="product.productVersion" readonly></label>
              <label>OP No<select v-if="editorMode === 'add'" :value="draft.opNo" :disabled="editorReadOnly || !processNo" @change="chooseOperation(($event.target as HTMLSelectElement).value)"><option value="">選擇 OP</option><option v-for="row in editorOperationOptions" :key="row.opNo" :value="row.opNo">{{ row.opNo }} — {{ row.opName }}</option></select><input v-else :value="draft.opNo" readonly></label>
              <label>Business Rule No<select v-model="selectedRuleNo" :disabled="editorReadOnly"><option v-for="row in ruleCatalog" :key="row.ruleNo" :value="row.ruleNo">{{ row.ruleNo }} — {{ row.ruleName }}</option></select></label>
              <label>Rule Action<input v-model.trim="ruleAction" maxlength="10" :readonly="editorReadOnly"></label>
              <label>Phase Name<select v-model.number="selectedPhaseNo" :disabled="editorReadOnly"><option v-for="phase in phaseCatalog" :key="phase.phaseNo" :value="phase.phaseNo">{{ phase.phaseNo }} — {{ phase.phaseName }}</option></select></label>
              <div class="br-phase-actions"><button type="button" class="br-button" :disabled="editorReadOnly" @click="setPhase">Set Phase</button><button type="button" class="br-button" :disabled="editorReadOnly" @click="unsetPhase">Un Set Phase</button></div>
              <label>Node Distance<select v-model.number="nodeDistance" :disabled="editorReadOnly"><option v-for="distance in [30,40,50,60,70,80]" :key="distance" :value="distance">{{ distance }}</option></select></label>
              <div class="br-arrange"><button type="button" class="br-button" :disabled="editorReadOnly" @click="arrangeAll">Arrange All</button><button type="button" class="br-button" :disabled="editorReadOnly" @click="alignNodes('top')">Top</button><button type="button" class="br-button" :disabled="editorReadOnly" @click="alignNodes('left')">Left</button></div>
            </div>
            <div class="br-toolbar">
              <button v-for="item in [{key:'select',label:'Select'},{key:'start-end',label:'Start/End'},{key:'rule-node',label:'Rule Node'},{key:'link-polyline',label:'General Link'},{key:'link-VH',label:'VH'},{key:'link-HV',label:'HV'},{key:'link-VHV',label:'VHV'},{key:'link-HVH',label:'HVH'}]" :key="item.key" type="button" class="br-tool" :class="{ active: tool === item.key }" :disabled="editorReadOnly" @click="tool = item.key; linkFromNodeId = ''">{{ item.label }}</button>
              <button type="button" class="br-tool delete-tool" :disabled="editorReadOnly || (!selectedNodeIds.length && !selectedLinkId)" @click="removeSelection">Delete Selected</button>
            </div>
            <div class="br-editor-canvas-wrap">
              <svg ref="canvas" tabindex="0" viewBox="0 0 900 400" class="br-editor-canvas" @click="canvasClick" @pointermove="moveNodeDrag" @pointerup="endNodeDrag" @pointercancel="endNodeDrag" @keydown.delete.prevent="removeSelection">
                <defs><marker id="br-editor-arrow" markerWidth="8" markerHeight="8" refX="6" refY="3" orient="auto"><path d="M0,0 L0,6 L7,3 z" fill="#68665f" /></marker></defs>
                <rect x="0" y="0" width="900" height="400" class="canvas-bg" />
                <g v-for="phase in phaseBoxes" :key="`edit-phase-${phase.phaseNo}`"><rect :x="phase.x" :y="phase.y" :width="phase.width" :height="phase.height" rx="8" class="phase-box"/><text :x="phase.x + 8" :y="phase.y + 17" class="phase-caption">{{ phase.name }}</text></g>
                <g v-for="link in draft.links" :key="link.id" @click="selectLink($event, link)"><path :d="pathFor(link)" :class="['br-link', { selected: selectedLinkId === link.id }]" marker-end="url(#br-editor-arrow)"/><text :x="linkLabel(link).x" :y="linkLabel(link).y" class="link-label">{{ link.linkName }}</text></g>
                <g v-for="node in draft.nodes" :key="node.id" :transform="`translate(${node.x},${node.y})`" class="flow-node" @pointerdown="beginNodeDrag($event, node)" @click="selectOrLinkNode($event, node)">
                  <ellipse v-if="node.kind !== 'rule'" cx="44" cy="23" rx="40" ry="21" :class="[node.kind === 'start' ? 'start-node' : 'end-node', { 'node-selected': selectedNodeIds.includes(node.id), 'link-source': linkFromNodeId === node.id }]"/>
                  <polygon v-else points="10,0 78,0 88,23 78,46 10,46 0,23" :class="['rule-node', { 'node-selected': selectedNodeIds.includes(node.id), 'link-source': linkFromNodeId === node.id }]"/>
                  <text x="44" y="28" class="node-text">{{ node.nodeNo }}</text>
                </g>
                <text v-if="!draft.nodes.length" x="450" y="190" class="canvas-hint">選取工具列後，點擊空白畫布新增節點；Rule Node 需先加入 Phase。</text>
              </svg>
            </div>
            <p class="br-editor-note">{{ editorError || (linkFromNodeId ? '選擇下一個節點完成連線。' : 'Ctrl+點選可多選節點；選取連線後可使用 Delete 移除。') }}</p>
          </fieldset>
        </div>
        <footer class="br-editor-footer"><button v-if="!(editorMode === 'edit' && draft.isNetFlow)" type="button" class="br-button primary" :disabled="editorReadOnly" @click="confirmEditor">Confirm</button><button type="button" class="br-button" @click="editorOpen = false">Close</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.business-rule-window{width:min(1008px,calc(100vw - 20px));height:min(591px,calc(100vh - 20px));display:flex;flex-direction:column;background:#fdfcf9;border:1px solid #aaa79d;box-shadow:0 8px 28px #0004;color:#27251e;font:13px Arial,sans-serif}.br-title,.br-editor-title{height:36px;flex:none;display:flex;align-items:center;justify-content:space-between;padding:0 12px;border-bottom:1px solid #d4d0c5;background:#f6f4ed;font-size:14px;font-weight:600}.br-title button,.br-editor-title button{border:0;background:transparent;font-size:21px;cursor:pointer}.br-main{flex:1;min-height:0;display:flex;flex-direction:column;gap:3px;padding:4px 7px}.br-group,.br-editor-frame{min-width:0;margin:0;padding:7px;border:1px solid #aaa79d}.br-group legend,.br-editor-frame legend{padding:0 5px;font-weight:600;color:#5c584d}.br-product-op{height:190px;flex:none}.br-product-op .br-group{height:100%}.br-op-grid{height:calc(100% - 8px);display:grid;grid-template-columns:minmax(0,1fr) 82px;gap:8px}.br-grid{width:100%;height:100%;border-collapse:collapse;font-size:12px;white-space:nowrap}.br-grid th{height:23px;padding:3px 6px;border:1px solid #c9c5b7;background:#f0b023;text-align:left;font-weight:600}.br-grid td{height:23px;padding:3px 6px;border:1px solid #d4d1c8;background:#fff}.br-grid tr.selected td{background:#ffe9a0}.br-grid tr:hover td{background:#fff1bd}.br-buttons{display:flex;flex-direction:column;gap:4px}.br-button{min-height:30px;padding:4px 8px;border:1px solid #b7b3a8;border-radius:3px;background:#f8f7f3;color:#292820;font:inherit;cursor:pointer}.br-button.primary{border-color:#da9f27;background:#f9a52a;color:white;font-weight:600}.br-button:disabled{opacity:.5;cursor:default}.br-draw-view{flex:1;min-height:0}.br-draw-view .br-group{height:100%;display:flex;flex-direction:column}.br-preview-caption{flex:none;height:22px;color:#5a564c;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.br-preview-layout{flex:1;min-height:0;display:grid;grid-template-columns:minmax(0,1fr) 82px;gap:8px}.br-preview-actions{display:flex;flex-direction:column;gap:4px}.br-preview-actions .br-button:last-child{margin-top:auto}.br-preview-canvas{min-width:0;min-height:0;overflow:auto;border:1px solid #c7c4bc;background-color:#fff;background-image:radial-gradient(#e7e5df 0.65px,transparent .65px);background-size:16px 16px}.br-preview-canvas svg{display:block;min-width:760px;width:100%;height:100%;min-height:310px}.br-empty{height:100%;display:grid;place-items:center;color:#777}.br-footer,.br-editor-footer{height:40px;flex:none;display:flex;justify-content:flex-end;align-items:center;gap:7px;padding:4px 9px;border-top:1px solid #d4d0c5;background:#faf9f5}.br-message{flex:1;min-width:0;overflow:hidden;color:#68645a;text-overflow:ellipsis;white-space:nowrap}.br-link{fill:none;stroke:#68665f;stroke-width:1.6}.br-link.selected{stroke:#dc8c16;stroke-width:3}.link-label{fill:#49463e;font-size:12px;text-anchor:middle;paint-order:stroke;stroke:#fff;stroke-width:4px;stroke-linejoin:round}.phase-box{fill:#fff9e9aa;stroke:#4b78a7;stroke-width:1.5;stroke-dasharray:6 4}.phase-caption{fill:#3c6082;font-size:12px;font-weight:600}.start-node{fill:#d07a34;stroke:#305f91;stroke-width:1.7}.end-node{fill:#e5b74d;stroke:#305f91;stroke-width:1.7}.rule-node{fill:#ffe994;stroke:#305f91;stroke-width:1.7}.node-text{fill:#28261f;font-size:11px;text-anchor:middle;pointer-events:none}.br-editor-overlay{position:fixed;inset:0;z-index:110;display:grid;place-items:center;background:#0006;padding:10px}.br-editor-window{width:min(860px,calc(100vw - 20px));height:min(650px,calc(100vh - 20px));display:flex;flex-direction:column;background:#fdfcf9;border:1px solid #99968d;box-shadow:0 12px 38px #0006;outline:none}.br-editor-content{flex:1;min-height:0;padding:5px 8px}.br-editor-frame{height:100%;display:flex;flex-direction:column}.br-editor-fields{display:grid;grid-template-columns:1fr 1fr 1fr;gap:5px 9px;align-items:end}.br-editor-fields label{display:flex;flex-direction:column;gap:2px;min-width:0;font-size:12px}.br-editor-fields input,.br-editor-fields select{min-width:0;height:24px;border:1px solid #9d998f;background:#fff1b9;padding:2px 5px;font:inherit}.br-editor-fields input[readonly]{background:#efeee9}.br-editor-fields .template-field{grid-column:span 2;display:grid;grid-template-columns:110px minmax(0,1fr) 154px;align-items:center}.br-editor-fields .template-field select{grid-column:3;grid-row:1}.br-phase-actions,.br-arrange{display:flex;gap:4px;align-items:flex-end}.br-phase-actions .br-button,.br-arrange .br-button{flex:1;min-width:0;font-size:11px;padding:3px}.br-toolbar{display:flex;flex-wrap:wrap;gap:3px;padding:5px 0 4px}.br-tool{height:29px;padding:3px 7px;border:1px solid #b9b5aa;background:#f7f6f1;font:11px Arial,sans-serif;cursor:pointer}.br-tool.active{background:#ffebac;border-color:#cf9620;box-shadow:inset 0 -2px #d49c25}.br-tool:disabled{opacity:.5;cursor:default}.delete-tool{margin-left:auto}.br-editor-canvas-wrap{flex:1;min-height:165px;overflow:auto;border:1px solid #bebbb2;background-color:#fff;background-image:radial-gradient(#e5e3dc .65px,transparent .65px);background-size:16px 16px}.br-editor-canvas{display:block;width:100%;min-width:760px;height:100%;min-height:270px;outline:none;cursor:crosshair}.canvas-bg{fill:transparent}.canvas-hint{fill:#858176;font-size:13px;text-anchor:middle;pointer-events:none}.flow-node{cursor:pointer}.node-selected{stroke:#e18813!important;stroke-width:3!important}.link-source{stroke:#b44028!important;stroke-width:3!important}.br-editor-note{height:20px;flex:none;margin:3px 0 0;color:#6c675a;font-size:11px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
@media(max-width:760px){.business-rule-window{height:calc(100vh - 12px);width:calc(100vw - 12px)}.br-product-op{height:210px}.br-op-grid{grid-template-columns:minmax(0,1fr) 72px}.br-preview-layout{grid-template-columns:minmax(0,1fr) 72px}.br-editor-window{height:calc(100vh - 12px);width:calc(100vw - 12px)}.br-editor-fields{grid-template-columns:1fr 1fr}.br-editor-fields .template-field{grid-column:1 / -1}.br-editor-fields .template-field{grid-template-columns:100px minmax(0,1fr) 130px}.br-arrange{grid-column:1 / -1}}
</style>
