<script setup lang="ts">
import { computed, ref } from 'vue'

type Row = Record<string, any>
type ParameterData = {
  ops: Row[]
  attributes: Row[]
  rules: Row[]
  equipments: Row[]
}
type TreeNode = {
  id: string
  parentId: string
  label: string
  kind: string
  depth: number
  hasChildren: boolean
  data?: Row
  attribType?: number
  attribSource?: number
  serialNo?: string
}
type MenuItem = { key: string; label: string }

const props = withDefaults(defineProps<{
  product: Row
  processes: Row[]
  parameters?: ParameterData | null
  developmentPreview?: boolean
  canEdit?: boolean
}>(), { parameters: null, developmentPreview: false, canEdit: false })
const emit = defineEmits<{ save: [parameters: ParameterData]; close: [] }>()
const window = globalThis.window

const sourceNames = ['Key In', 'Calculate', 'System', 'Setup']
const phaseOptions = [
  { value: 1, label: '1 - Before OP' }, { value: 2, label: '2 - During OP' },
  { value: 3, label: '3 - After OP' }, { value: 99, label: '99 - All' },
]
const mockOpCatalog = [
  { psNo: 'DEV-PS-10', opNo: 'DEV-OP-10', opName: '點膠作業', opOrder: 10, haveComponent: 1, inProductProcess: true },
  { psNo: 'DEV-PS-10', opNo: 'DEV-OP-20', opName: '組裝作業', opOrder: 20, haveComponent: 1, inProductProcess: true },
  { psNo: 'DEV-PS-20', opNo: 'DEV-OP-30', opName: '終檢作業', opOrder: 10, haveComponent: 0, inProductProcess: false },
]
const mockElementCatalog = ['DEV-LOT-QTY', 'DEV-LOT-TEMP', 'DEV-COMP-LOT', 'DEV-COMP-SIZE']
const mockRuleCatalog = ['DEV-RULE-LOT-001', 'DEV-RULE-COMP-001', 'DEV-RULE-CP-001']
const mockEquipmentCatalog = ['DEV-EQP-01', 'DEV-EQP-02', 'DEV-EQP-03']
const mockRecipeCatalog = ['DEV-RECIPE-GROUP-A', 'DEV-RECIPE-GROUP-B']
const mockRecipeDetails: Record<string, Row[]> = {
  'DEV-RECIPE-GROUP-A': [{ recipeNo: 'DEV-RECIPE-001', recipeValue: 'DEV-SET-01' }, { recipeNo: 'DEV-RECIPE-002', recipeValue: 'DEV-SET-02' }],
  'DEV-RECIPE-GROUP-B': [{ recipeNo: 'DEV-RECIPE-003', recipeValue: 'DEV-SET-03' }],
}
const currentUser = 'DEV001'
const readOnly = computed(() => !props.developmentPreview || !props.canEdit)
const scope = ref<'process' | 'all'>('process')
const blankData = (): ParameterData => ({ ops: [], attributes: [], rules: [], equipments: [] })
const clone = <T,>(value: T): T => JSON.parse(JSON.stringify(value)) as T
const inputData = clone(props.parameters ?? blankData())
const data = ref<ParameterData>({
  ops: Array.isArray(inputData.ops) ? inputData.ops : [],
  attributes: Array.isArray(inputData.attributes) ? inputData.attributes : [],
  rules: Array.isArray(inputData.rules) ? inputData.rules : [],
  equipments: Array.isArray(inputData.equipments) ? inputData.equipments : [],
})
const selectedId = ref('product')
const collapsed = ref<Set<string>>(new Set())
const activeTab = ref('op')
const editorKind = ref('')
const editorMode = ref<'none' | 'add' | 'edit' | 'view'>('none')
const editorReadOnly = computed(() => readOnly.value || editorMode.value === 'view')
const draft = ref<Row>({})
const message = ref('')
const context = ref({ visible: false, x: 0, y: 0, nodeId: '' })

function productPs(): Row[] {
  return props.processes.map((row, index) => ({
    psNo: String(row.psNo ?? ''), psOrder: Number(row.psOrder ?? (index + 1) * 10),
    haveComponent: Number(row.haveComponent ?? 0), processNo: row.processNo ?? '',
  })).filter(row => row.psNo)
}
const shownPs = computed(() => {
  const actual = productPs()
  if (scope.value === 'process' || !props.developmentPreview) return actual
  const union = new Map<string, Row>(actual.map(row => [row.psNo, row]))
  for (const op of mockOpCatalog) {
    if (!union.has(op.psNo)) union.set(op.psNo, { psNo: op.psNo, psOrder: op.psNo === 'DEV-PS-20' ? 20 : 10, haveComponent: op.haveComponent, processNo: 'DEV-ALL-PS' })
  }
  return [...union.values()].sort((a, b) => Number(a.psOrder) - Number(b.psOrder) || String(a.psNo).localeCompare(String(b.psNo)))
})
const routeOpNos = computed(() => {
  if (!props.developmentPreview) return new Set<string>()
  const set = new Set(mockOpCatalog.filter(item => item.inProductProcess).map(item => item.opNo))
  return set
})
const allNodes = computed<TreeNode[]>(() => {
  const nodes: TreeNode[] = [{ id: 'product', parentId: '', label: props.product.productVersion && props.product.productVersion !== '-1' ? `${props.product.productNo}:${props.product.productVersion}` : String(props.product.productNo ?? ''), kind: 'product', depth: 0, hasChildren: true }]
  const add = (node: TreeNode) => nodes.push(node)
  for (const ps of shownPs.value) {
    const psId = `ps:${ps.psNo}`
    add({ id: psId, parentId: 'product', label: String(ps.psNo), kind: 'ps', depth: 1, hasChildren: true, data: ps })
    const ops = data.value.ops.filter(row => String(row.psNo) === String(ps.psNo))
      .filter(row => scope.value === 'all' || !routeOpNos.value.size || routeOpNos.value.has(String(row.opNo)))
      .sort((a, b) => Number(a.opOrder ?? 0) - Number(b.opOrder ?? 0) || String(a.opNo).localeCompare(String(b.opNo)))
    for (const op of ops) {
      const opId = `op:${op.serialNo}`
      const condition = Boolean(op.nodeId || op.opScript)
      const marker = condition ? ` (Condition${op.nodeId ? `, NodeId:${op.nodeId}` : ''}${op.nodeId && op.opScript ? ' & Script' : op.opScript ? ', Script' : ''})` : ''
      add({ id: opId, parentId: psId, label: `${op.opNo}${marker}`, kind: 'op', depth: 2, hasChildren: true, data: op, serialNo: String(op.serialNo) })
      const lotId = `${opId}:lot`
      add({ id: lotId, parentId: opId, label: 'Lot Attrib', kind: 'attribGroup', depth: 3, hasChildren: true, attribType: 0, serialNo: String(op.serialNo) })
      sourceNames.forEach((source, sourceIndex) => {
        const sourceId = `${lotId}:source:${sourceIndex}`
        const attribs = data.value.attributes.filter(item => String(item.serialNo) === String(op.serialNo) && Number(item.attribType) === 0 && Number(item.attribSource) === sourceIndex)
          .sort((a, b) => Number(a.attribPhase) - Number(b.attribPhase) || Number(a.attribSequence) - Number(b.attribSequence))
        add({ id: sourceId, parentId: lotId, label: source, kind: 'attribSource', depth: 4, hasChildren: attribs.length > 0, attribType: 0, attribSource: sourceIndex, serialNo: String(op.serialNo) })
        for (const item of attribs) {
          const phase = phaseOptions.find(option => option.value === Number(item.attribPhase))?.label ?? String(item.attribPhase)
          const suffix = Number(item.attribSource) >= 2 ? `: ${item.attribValue ?? ''}` : ''
          add({ id: `attrib:${op.serialNo}:0:${item.attribNo}`, parentId: sourceId, label: `${item.attribNo}${suffix} (${phase})`, kind: 'attrib', depth: 5, hasChildren: false, data: item, attribType: 0, attribSource: sourceIndex, serialNo: String(op.serialNo) })
        }
      })
      if (Number(ps.haveComponent) === 1) {
        const componentId = `${opId}:component`
        add({ id: componentId, parentId: opId, label: 'Component Attrib', kind: 'attribGroup', depth: 3, hasChildren: true, attribType: 1, serialNo: String(op.serialNo) })
        sourceNames.slice(0, 3).forEach((source, sourceIndex) => {
          const sourceId = `${componentId}:source:${sourceIndex}`
          const attribs = data.value.attributes.filter(item => String(item.serialNo) === String(op.serialNo) && Number(item.attribType) === 1 && Number(item.attribSource) === sourceIndex)
            .sort((a, b) => Number(a.attribPhase) - Number(b.attribPhase) || Number(a.attribSequence) - Number(b.attribSequence))
          add({ id: sourceId, parentId: componentId, label: source, kind: 'attribSource', depth: 4, hasChildren: attribs.length > 0, attribType: 1, attribSource: sourceIndex, serialNo: String(op.serialNo) })
          for (const item of attribs) {
            const phase = phaseOptions.find(option => option.value === Number(item.attribPhase))?.label ?? String(item.attribPhase)
            const suffix = Number(item.attribSource) === 2 ? `: ${item.attribValue ?? ''}` : ''
            add({ id: `attrib:${op.serialNo}:1:${item.attribNo}`, parentId: sourceId, label: `${item.attribNo}${suffix} (${phase})`, kind: 'attrib', depth: 5, hasChildren: false, data: item, attribType: 1, attribSource: sourceIndex, serialNo: String(op.serialNo) })
          }
        })
      }
      const ruleId = `${opId}:rules`
      const rules = data.value.rules.filter(item => String(item.serialNo) === String(op.serialNo))
        .sort((a, b) => Number(a.rulePhase) - Number(b.rulePhase) || Number(a.ruleSequence) - Number(b.ruleSequence))
      add({ id: ruleId, parentId: opId, label: 'Rule', kind: 'ruleGroup', depth: 3, hasChildren: rules.length > 0, serialNo: String(op.serialNo) })
      for (const item of rules) {
        const phase = phaseOptions.find(option => option.value === Number(item.rulePhase))?.label ?? String(item.rulePhase)
        add({ id: `rule:${op.serialNo}:${item.ruleNo}`, parentId: ruleId, label: `${item.ruleNo} (${Number(item.ruleType) === 0 ? 'Lot' : 'Component'} / ${phase})`, kind: 'rule', depth: 4, hasChildren: false, data: item, serialNo: String(op.serialNo) })
      }
      const eqpId = `${opId}:equipment`
      const equipments = data.value.equipments.filter(item => String(item.serialNo) === String(op.serialNo)).sort((a, b) => String(a.equipmentNo).localeCompare(String(b.equipmentNo)))
      add({ id: eqpId, parentId: opId, label: 'Equipment', kind: 'equipmentGroup', depth: 3, hasChildren: equipments.length > 0, serialNo: String(op.serialNo) })
      for (const item of equipments) {
        const equipmentId = `equipment:${op.serialNo}:${item.equipmentNo}`
        const recipeDetails = mockRecipeDetails[String(item.recipeGroup ?? '')] ?? []
        add({ id: equipmentId, parentId: eqpId, label: `${item.equipmentNo} (${item.recipeGroup ?? ''})`, kind: 'equipment', depth: 4, hasChildren: recipeDetails.length > 0, data: item, serialNo: String(op.serialNo) })
        recipeDetails.forEach(detail => add({ id: `${equipmentId}:recipe:${detail.recipeNo}`, parentId: equipmentId, label: `${detail.recipeNo}:${detail.recipeValue}`, kind: 'recipeDetail', depth: 5, hasChildren: false, data: { ...detail, equipmentNo: item.equipmentNo, recipeGroup: item.recipeGroup }, serialNo: String(op.serialNo) }))
      }
    }
  }
  return nodes
})
const visibleNodes = computed(() => {
  const hidden = new Set<string>()
  const result: TreeNode[] = []
  for (const node of allNodes.value) {
    if (node.parentId && hidden.has(node.parentId)) { hidden.add(node.id); continue }
    result.push(node)
    if (collapsed.value.has(node.id)) hidden.add(node.id)
  }
  return result
})
const selectedNode = computed(() => allNodes.value.find(node => node.id === selectedId.value) ?? allNodes.value[0])
const tabs = [
  { key: 'op', label: 'OP' }, { key: 'attrib', label: 'Attrib' }, { key: 'rule', label: 'Rule' },
  { key: 'equipment', label: 'Equipment' }, { key: 'condition', label: 'Condition OP' },
]
const opCandidates = computed(() => {
  const psNo = String(draft.value.psNo ?? '')
  return mockOpCatalog.filter(op => (!psNo || op.psNo === psNo)
    && (scope.value === 'all' || op.inProductProcess)
    && !data.value.ops.some(existing => existing.psNo === op.psNo && existing.opNo === op.opNo && !existing.nodeId && !existing.opScript))
})
const equipmentCandidates = computed(() => mockEquipmentCatalog.filter(equipmentNo => !data.value.equipments.some(item => item.serialNo === draft.value.serialNo && item.equipmentNo === equipmentNo)))
const menuItems = computed<MenuItem[]>(() => {
  const node = allNodes.value.find(item => item.id === context.value.nodeId)
  if (!node) return []
  if (node.kind === 'product') return []
  if (node.kind === 'ps') return readOnly.value ? [] : [{ key: 'add-op', label: 'Add OP' }, { key: 'add-condition', label: 'Add Condition OP' }]
  if (node.kind === 'op') {
    const conditional = Boolean(node.data?.nodeId || node.data?.opScript)
    if (readOnly.value) return [{ key: 'view', label: conditional ? 'View Condition OP' : 'View OP' }]
    if (conditional) return [{ key: 'edit', label: 'Edit Condition OP' }, { key: 'delete', label: 'Delete Condition OP' }, { key: 'view', label: 'View Condition OP' }]
    return [{ key: 'delete', label: 'Delete Default OP' }]
  }
  if (node.kind === 'attribGroup' || node.kind === 'attribSource') return readOnly.value ? [] : [{ key: 'add-attrib', label: 'Add Attrib' }]
  if (node.kind === 'ruleGroup') return readOnly.value ? [] : [{ key: 'add-rule', label: 'Add Rule' }]
  if (node.kind === 'equipmentGroup') return readOnly.value ? [] : [{ key: 'add-equipment', label: 'Add Equipment / Recipe' }]
  if (node.kind === 'recipeDetail') return [{ key: 'view', label: 'View Recipe Detail' }]
  if (node.kind === 'equipment') return [
    ...(readOnly.value ? [] : [{ key: 'add-equipment', label: 'Add Equipment / Recipe' }, { key: 'edit', label: 'Edit' }, { key: 'delete', label: 'Delete' }]),
  ]
  if (node.kind === 'attrib' || node.kind === 'rule') return [
    ...(readOnly.value ? [] : [{ key: 'edit', label: 'Edit' }, { key: 'delete', label: 'Delete' }]), { key: 'view', label: 'View' },
  ]
  return []
})
const isEditorActive = computed(() => editorMode.value !== 'none')
const editorTitle = computed(() => ({ op: 'OP', attrib: 'Attrib', rule: 'Rule', equipment: 'Equipment / Recipe', condition: 'Condition OP' } as Record<string, string>)[editorKind.value] ?? '')

function selectNode(node: TreeNode): void { selectedId.value = node.id; context.value.visible = false }
function toggleNode(node: TreeNode): void {
  if (!node.hasChildren) return
  const next = new Set(collapsed.value)
  if (next.has(node.id)) next.delete(node.id); else next.add(node.id)
  collapsed.value = next
}
function openContext(event: MouseEvent, node: TreeNode): void {
  selectNode(node)
  const x = Math.min(event.clientX, window.innerWidth - 230)
  const y = Math.min(event.clientY, window.innerHeight - 290)
  context.value = { visible: true, x: Math.max(4, x), y: Math.max(4, y), nodeId: node.id }
  if (!menuItems.value.length) context.value.visible = false
}
function startEditor(action: string): void {
  const node = allNodes.value.find(item => item.id === context.value.nodeId)
  if (!node) return
  context.value.visible = false
  if (action === 'delete') { deleteNode(node); return }
  if (action === 'view' && !node.data && node.kind !== 'op') return
  const mode = action.startsWith('add-') ? 'add' : action === 'view' ? 'view' : action === 'edit' ? 'edit' : 'none'
  if (mode !== 'view' && readOnly.value) { message.value = '目前為檢視模式，只有 Development 假資料可修改。'; return }
  editorMode.value = mode
  if (action === 'add-op') {
    editorKind.value = 'op'; activeTab.value = 'op'
    const psNo = node.kind === 'ps' ? String(node.data?.psNo ?? '') : ''
    const defaults = mockOpCatalog.filter(item => (!psNo || item.psNo === psNo)
      && (scope.value === 'all' || item.inProductProcess)
      && !data.value.ops.some(existing => existing.psNo === item.psNo && existing.opNo === item.opNo && !existing.nodeId && !existing.opScript))
      .map(item => `${item.psNo}|${item.opNo}`)
    draft.value = { psNo, selectedOps: defaults }
    return
  }
  if (action === 'add-condition') {
    editorKind.value = 'condition'; activeTab.value = 'condition'
    draft.value = { psNo: String(node.data?.psNo ?? ''), opNo: '', nodeId: '', opScript: '' }
    return
  }
  if (action === 'add-attrib') {
    editorKind.value = 'attrib'; activeTab.value = 'attrib'
    draft.value = { serialNo: node.serialNo, attribType: node.attribType ?? 0, attribSource: node.attribSource ?? 0, attribNo: '', attribValue: '', attribSequence: 0, attribPhase: 1, attribScript: '', saveToLotPropertyNo: '', qcItemNo: '' }
    return
  }
  if (action === 'add-lot-attrib' || action === 'add-component-attrib') {
    editorKind.value = 'attrib'; activeTab.value = 'attrib'
    draft.value = { serialNo: node.serialNo, attribType: action === 'add-lot-attrib' ? 0 : 1, attribSource: 0, attribNo: '', attribValue: '', attribSequence: 0, attribPhase: 1, attribScript: '', saveToLotPropertyNo: '', qcItemNo: '' }
    return
  }
  if (action === 'add-rule') {
    editorKind.value = 'rule'; activeTab.value = 'rule'
    draft.value = { serialNo: node.serialNo, ruleNo: '', ruleSequence: 0, rulePhase: 1, ruleScript: '', ruleType: 0, ruleClass: 0 }
    return
  }
  if (action === 'add-equipment') {
    editorKind.value = 'equipment'; activeTab.value = 'equipment'
    draft.value = { serialNo: node.serialNo, psNo: node.data?.psNo ?? '', opNo: node.data?.opNo ?? '', equipmentNos: [], equipmentNo: '', recipeGroup: '', recipeDetails: [] }
    return
  }
  if (action === 'edit' || action === 'view') {
    if (node.kind === 'attrib') { editorKind.value = 'attrib'; activeTab.value = 'attrib' }
    else if (node.kind === 'rule') { editorKind.value = 'rule'; activeTab.value = 'rule' }
    else if (node.kind === 'equipment' || node.kind === 'recipeDetail') { editorKind.value = 'equipment'; activeTab.value = 'equipment' }
    else if (node.kind === 'op') { editorKind.value = 'condition'; activeTab.value = 'condition' }
    draft.value = clone(node.data ?? {})
    if (editorKind.value === 'equipment') {
      if (node.kind === 'recipeDetail') draft.value.recipeDetails = [clone(node.data)]
      else draft.value.recipeDetails = clone(mockRecipeDetails[String(draft.value.recipeGroup ?? '')] ?? [])
    }
  }
}
function setCandidate(candidate: Row, event: Event): void {
  const checked = (event.target as HTMLInputElement).checked
  const key = `${candidate.psNo}|${candidate.opNo}`
  const current = new Set<string>(draft.value.selectedOps ?? [])
  if (checked) current.add(key); else current.delete(key)
  draft.value.selectedOps = [...current]
}
function setEquipmentCandidate(equipmentNo: string, event: Event): void {
  const checked = (event.target as HTMLInputElement).checked
  const selected = new Set<string>(draft.value.equipmentNos ?? [])
  if (checked) selected.add(equipmentNo); else selected.delete(equipmentNo)
  draft.value.equipmentNos = [...selected]
}
function validateDraft(): boolean {
  if (editorKind.value === 'op' && !(draft.value.selectedOps?.length)) { message.value = '請至少勾選一個 OP。'; return false }
  if (editorKind.value === 'attrib') {
    if (!draft.value.attribNo) { message.value = '請選擇 AttribNo。'; return false }
    if (!draft.value.attribPhase) { message.value = '請選擇 Attrib Phase。'; return false }
    if ([0, 1].includes(Number(draft.value.attribSource)) && Number(draft.value.attribPhase) === 99) { message.value = 'Key In / Calculate 不可設定 All Phase。'; return false }
    if (Number(draft.value.attribSource) === 1 && !draft.value.attribScript) { message.value = 'Calculate Attrib 必須設定 Script。'; return false }
    if (Number(draft.value.attribSource) === 3 && !draft.value.attribValue) { message.value = 'Setup Attrib 必須設定預設值。'; return false }
  }
  if (editorKind.value === 'rule' && (!draft.value.ruleNo || !draft.value.rulePhase || !draft.value.ruleScript)) { message.value = 'RuleNo、RulePhase 與 RuleScript 為必填。'; return false }
  if (editorKind.value === 'equipment' && editorMode.value === 'add' && !(draft.value.equipmentNos?.length)) { message.value = '至少勾選一項 Equipment。'; return false }
  if (editorKind.value === 'equipment' && editorMode.value !== 'add' && !draft.value.equipmentNo) { message.value = 'EquipmentNo 必填。'; return false }
  if (editorKind.value === 'condition' && (!draft.value.opNo || (!draft.value.nodeId && !draft.value.opScript))) { message.value = 'OPNo 必填，且 NodeID 或 OP Script 至少設定一項。'; return false }
  message.value = ''
  return true
}
function nextSerial(): string { return `DEV-PRDOP-${Date.now()}-${Math.floor(Math.random() * 10000)}` }
function applyEditor(closeAfter: boolean): void {
  if (!isEditorActive.value || editorReadOnly.value || !validateDraft()) return
  if (editorKind.value === 'op' && editorMode.value === 'add') {
    for (const key of draft.value.selectedOps as string[]) {
      const [psNo, opNo] = key.split('|')
      const source = mockOpCatalog.find(item => item.psNo === psNo && item.opNo === opNo)
      if (!source) continue
      data.value.ops.push({ serialNo: nextSerial(), psNo, opNo, opName: source.opName, opOrder: source.opOrder, haveComponent: source.haveComponent, nodeId: '', opScript: '', dataStamp: 0, creator: currentUser })
    }
  } else if (editorKind.value === 'condition' && editorMode.value === 'add') {
    const source = mockOpCatalog.find(item => item.psNo === draft.value.psNo && item.opNo === draft.value.opNo)
    data.value.ops.push({ ...source, ...clone(draft.value), serialNo: nextSerial(), opOrder: Number(source?.opOrder ?? 0) + 1, haveComponent: Number(source?.haveComponent ?? 0), dataStamp: 0, creator: currentUser })
  } else if (editorKind.value === 'attrib') {
    const row = { ...clone(draft.value), serialNo: String(draft.value.serialNo), attribType: Number(draft.value.attribType), attribSource: Number(draft.value.attribSource), attribSequence: Number(draft.value.attribSequence ?? 0), attribPhase: Number(draft.value.attribPhase), reviser: currentUser, reviseDate: new Date().toISOString() }
    if (editorMode.value === 'add') data.value.attributes.push(row)
    else data.value.attributes = data.value.attributes.map(item => item.serialNo === row.serialNo && item.attribType === row.attribType && item.attribNo === row.attribNo ? row : item)
  } else if (editorKind.value === 'rule') {
    const row = { ...clone(draft.value), ruleSequence: Number(draft.value.ruleSequence ?? 0), rulePhase: Number(draft.value.rulePhase), ruleType: Number(draft.value.ruleType), reviser: currentUser, reviseDate: new Date().toISOString() }
    if (editorMode.value === 'add') data.value.rules.push(row)
    else data.value.rules = data.value.rules.map(item => item.serialNo === row.serialNo && item.ruleNo === row.ruleNo ? row : item)
  } else if (editorKind.value === 'equipment') {
    if (editorMode.value === 'add') {
      for (const equipmentNo of draft.value.equipmentNos as string[]) data.value.equipments.push({ serialNo: draft.value.serialNo, equipmentNo, recipeGroup: draft.value.recipeGroup, reviser: currentUser, reviseDate: new Date().toISOString() })
    } else {
      const row = { ...clone(draft.value), reviser: currentUser, reviseDate: new Date().toISOString() }
      delete row.recipeDetails
      data.value.equipments = data.value.equipments.map(item => item.serialNo === row.serialNo && item.equipmentNo === row.equipmentNo ? row : item)
    }
  } else if (editorKind.value === 'condition' && editorMode.value === 'edit') {
    data.value.ops = data.value.ops.map(item => item.serialNo === draft.value.serialNo ? { ...item, nodeId: draft.value.nodeId, opScript: draft.value.opScript, reviser: currentUser, reviseDate: new Date().toISOString() } : item)
  }
  emit('save', clone(data.value))
  message.value = 'Development 假資料已套用並儲存。'
  if (closeAfter) {
    editorMode.value = 'none'; editorKind.value = ''; draft.value = {}
    emit('close')
  } else if (editorMode.value === 'add') {
    if (editorKind.value === 'op') draft.value = { psNo: draft.value.psNo, selectedOps: [] }
    else if (editorKind.value === 'attrib') draft.value = { ...draft.value, attribNo: '', attribValue: '', attribScript: '', attribSequence: Number(draft.value.attribSequence ?? 0) + 1 }
    else if (editorKind.value === 'rule') draft.value = { ...draft.value, ruleNo: '', ruleScript: '', ruleSequence: Number(draft.value.ruleSequence ?? 0) + 1 }
    else if (editorKind.value === 'equipment') draft.value = { ...draft.value, equipmentNos: [], recipeGroup: '', recipeDetails: [] }
    else { editorMode.value = 'none'; editorKind.value = ''; draft.value = {} }
  }
}
function deleteNode(node: TreeNode): void {
  if (readOnly.value) return
  if (!window.confirm(`確定刪除「${node.label}」？`)) return
  if (node.kind === 'attrib') data.value.attributes = data.value.attributes.filter(item => item.serialNo !== node.serialNo || item.attribType !== node.attribType || item.attribNo !== node.data?.attribNo)
  else if (node.kind === 'rule') data.value.rules = data.value.rules.filter(item => item.serialNo !== node.serialNo || item.ruleNo !== node.data?.ruleNo)
  else if (node.kind === 'equipment') data.value.equipments = data.value.equipments.filter(item => item.serialNo !== node.serialNo || item.equipmentNo !== node.data?.equipmentNo)
  else if (node.kind === 'op') {
    const serialNo = node.serialNo
    data.value.ops = data.value.ops.filter(item => item.serialNo !== serialNo)
    data.value.attributes = data.value.attributes.filter(item => item.serialNo !== serialNo)
    data.value.rules = data.value.rules.filter(item => item.serialNo !== serialNo)
    data.value.equipments = data.value.equipments.filter(item => item.serialNo !== serialNo)
  }
  emit('save', clone(data.value)); selectedId.value = node.parentId || 'product'; message.value = 'Development 假資料已刪除。'
}
function cancelEditor(): void { editorKind.value = ''; editorMode.value = 'none'; draft.value = {}; message.value = '' }
function refreshTree(): void { collapsed.value = new Set(); selectedId.value = 'product'; message.value = '已依目前產品流程重新整理樹狀資料。' }
function close(): void { emit('close') }
</script>

<template>
  <section class="parameter-window" @click="context.visible = false">
    <header class="parameter-title"><span>Parameter Setup</span><button type="button" aria-label="Close" @click.stop="close">×</button></header>
    <div class="parameter-body">
      <section class="parameter-left">
        <div class="parameter-scope">
          <strong>Default OP</strong>
          <label><input v-model="scope" type="radio" value="process" @change="refreshTree"> Product Process OP</label>
          <label><input v-model="scope" type="radio" value="all" @change="refreshTree"> All PS OP</label>
        </div>
        <div class="parameter-tree" role="tree" aria-label="Product process parameter tree">
          <button v-for="node in visibleNodes" :key="node.id" class="parameter-tree-row" :class="[{ selected: selectedId === node.id }, `kind-${node.kind}`]" :style="{ paddingLeft: `${8 + node.depth * 15}px` }" type="button" role="treeitem" :aria-expanded="node.hasChildren ? !collapsed.has(node.id) : undefined" @click.stop="selectNode(node)" @dblclick.stop="toggleNode(node)" @contextmenu.prevent.stop="openContext($event, node)">
            <span class="tree-toggle" @click.stop="toggleNode(node)">{{ node.hasChildren ? (collapsed.has(node.id) ? '▸' : '▾') : '·' }}</span><span>{{ node.label }}</span>
          </button>
          <div v-if="!processes.length" class="parameter-empty-tree">此產品尚未設定流程，請先完成 Product Process。</div>
        </div>
        <footer class="parameter-left-footer"><span v-if="developmentPreview" class="parameter-dev-tag">DEV 假資料</span><button class="parameter-button" type="button" @click="refreshTree">Refresh</button></footer>
      </section>
      <section class="parameter-right">
        <div class="parameter-editor-frame" :class="{ disabled: !isEditorActive }">
          <nav class="parameter-tabs" aria-label="Parameter editors">
            <button v-for="tab in tabs" :key="tab.key" type="button" :disabled="isEditorActive && tab.key !== editorKind" :class="{ active: activeTab === tab.key }" @click="activeTab = tab.key">{{ tab.label }}</button>
          </nav>
          <div class="parameter-editor-body">
            <div v-if="!isEditorActive" class="parameter-instruction">選取左側節點，按滑鼠右鍵執行新增、修改、刪除或檢視。</div>
            <template v-else>
              <div class="parameter-editor-heading"><strong>{{ editorMode === 'add' ? 'Add' : editorMode === 'edit' ? 'Edit' : 'View' }} {{ editorTitle }}</strong><span>{{ readOnly || editorMode === 'view' ? '唯讀' : 'DEV 模式' }}</span></div>
              <div v-if="editorKind === 'op'" class="parameter-editor-scroll">
                <div class="parameter-radio-row"><label><input v-model="scope" type="radio" value="process"> Product Process OP</label><label><input v-model="scope" type="radio" value="all"> All PS OP</label></div>
                <table class="parameter-grid"><thead><tr><th>選取</th><th>OPNo</th><th>OPName</th><th>PSNo</th></tr></thead><tbody>
                  <tr v-for="candidate in opCandidates" :key="candidate.psNo + candidate.opNo"><td><input type="checkbox" :checked="draft.selectedOps?.includes(`${candidate.psNo}|${candidate.opNo}`)" :disabled="editorReadOnly" @change="setCandidate(candidate, $event)"></td><td>{{ candidate.opNo }}</td><td>{{ candidate.opName }}</td><td>{{ candidate.psNo }}</td></tr>
                  <tr v-if="!opCandidates.length"><td colspan="4" class="empty">沒有可加入的作業站。</td></tr>
                </tbody></table>
              </div>
              <div v-else-if="editorKind === 'attrib'" class="parameter-editor-scroll parameter-form-grid">
                <label class="wide">Attrib Type<select v-model.number="draft.attribType" :disabled="editorReadOnly || editorMode === 'edit'"><option :value="0">Lot Attrib</option><option :value="1">Component Attrib</option></select></label>
                <label>Attrib Source<select v-model.number="draft.attribSource" :disabled="editorReadOnly || editorMode === 'edit'"><option v-for="(source,index) in sourceNames.slice(0, Number(draft.attribType) === 1 ? 3 : 4)" :key="source" :value="index">{{ source }}</option></select></label>
                <label>Attrib Phase<select v-model.number="draft.attribPhase" :disabled="editorReadOnly"><option v-for="phase in phaseOptions" :key="phase.value" :value="phase.value">{{ phase.label }}</option></select></label>
                <label>Attrib No<div class="parameter-input-lookup"><input v-model.trim="draft.attribNo" :readonly="editorReadOnly || editorMode === 'edit'" list="parameter-attrib-options"><button type="button" :disabled="editorReadOnly || editorMode === 'edit'" @click="draft.attribNo = mockElementCatalog[0]">...</button></div><datalist id="parameter-attrib-options"><option v-for="item in mockElementCatalog" :key="item" :value="item" /></datalist></label>
                <label>Sequence<input v-model.number="draft.attribSequence" type="number" min="0" :readonly="editorReadOnly"></label>
                <label v-if="[2,3].includes(Number(draft.attribSource))">Attrib Value<input v-model="draft.attribValue" :readonly="editorReadOnly"></label>
                <label v-if="Number(draft.attribSource) === 1" class="wide">Calculate Script<textarea v-model="draft.attribScript" rows="4" :readonly="editorReadOnly" /></label>
                <label v-if="Number(draft.attribType) === 0">Save To Lot Property<input v-model="draft.saveToLotPropertyNo" :readonly="editorReadOnly"></label>
                <label v-if="Number(draft.attribType) === 0">Save To QC Item<input v-model="draft.qcItemNo" :readonly="editorReadOnly"></label>
                <p class="parameter-form-note wide">Component Attrib 不提供 Setup 與 QC Item；Key In / Calculate 不允許 All Phase。</p>
              </div>
              <div v-else-if="editorKind === 'rule'" class="parameter-editor-scroll parameter-form-grid">
                <label>Rule No<input v-model.trim="draft.ruleNo" :readonly="editorReadOnly || editorMode === 'edit'" list="parameter-rule-options"><datalist id="parameter-rule-options"><option v-for="item in mockRuleCatalog" :key="item" :value="item" /></datalist></label>
                <label>Sequence<input v-model.number="draft.ruleSequence" type="number" min="0" :readonly="editorReadOnly"></label>
                <label>Rule Phase<select v-model.number="draft.rulePhase" :disabled="editorReadOnly"><option v-for="phase in phaseOptions" :key="phase.value" :value="phase.value">{{ phase.label }}</option></select></label>
                <label>Rule Type<select v-model.number="draft.ruleType" :disabled="editorReadOnly || editorMode === 'edit'"><option :value="0">Lot Rule</option><option :value="1">Component Rule</option></select></label>
                <label>Rule Class<select v-model.number="draft.ruleClass" :disabled="editorReadOnly || editorMode === 'edit'"><option :value="0">Normal</option><option :value="1">Component</option><option :value="2">Bin</option></select></label>
                <label class="wide">Rule Script<textarea v-model="draft.ruleScript" rows="6" :readonly="editorReadOnly" /></label>
                <p class="parameter-form-note wide">CP/FT 專用 Rule Class 的條件、郵件通知與外部套件由 WinForms 專用服務處理；本 DEV 頁先呈現一般規則欄位。</p>
              </div>
              <div v-else-if="editorKind === 'equipment'" class="parameter-editor-scroll parameter-form-grid">
                <div v-if="editorMode === 'add'" class="wide"><span class="recipe-caption">Equipment（可複選）</span><table class="parameter-grid"><thead><tr><th>選取</th><th>EquipmentNo</th></tr></thead><tbody><tr v-for="item in equipmentCandidates" :key="item"><td><input type="checkbox" :checked="draft.equipmentNos?.includes(item)" :disabled="editorReadOnly" @change="setEquipmentCandidate(item, $event)"></td><td>{{ item }}</td></tr><tr v-if="!equipmentCandidates.length"><td colspan="2" class="empty">沒有可加入的設備。</td></tr></tbody></table></div>
                <label v-else>Equipment No<input v-model.trim="draft.equipmentNo" readonly></label>
                <label>Recipe Group<select v-model="draft.recipeGroup" :disabled="editorReadOnly" @change="draft.recipeDetails = clone(mockRecipeDetails[String(draft.recipeGroup)] ?? [])"><option value="">（未設定）</option><option v-for="item in mockRecipeCatalog" :key="item" :value="item">{{ item }}</option></select></label>
                <div class="wide"><span class="recipe-caption">Recipe Detail（從 Recipe Group 主檔唯讀載入）</span><table class="parameter-grid"><thead><tr><th>RecipeNo</th><th>RecipeValue</th></tr></thead><tbody><tr v-for="recipe in draft.recipeDetails ?? []" :key="recipe.recipeNo"><td>{{ recipe.recipeNo }}</td><td>{{ recipe.recipeValue }}</td></tr><tr v-if="!(draft.recipeDetails?.length)"><td colspan="2" class="empty">無 Recipe Detail</td></tr></tbody></table></div>
                <p class="parameter-form-note wide">產品資料只保存 SerialNo、EquipmentNo、RecipeGroup。Recipe Detail 來自設備 Recipe Group 主檔，不在本畫面新增或修改。</p>
              </div>
              <div v-else-if="editorKind === 'condition'" class="parameter-editor-scroll parameter-form-grid">
                <label>OP No<select v-model="draft.opNo" :disabled="editorReadOnly || editorMode !== 'add'"><option value="">選擇 OP</option><option v-for="item in mockOpCatalog.filter(row => row.psNo === draft.psNo)" :key="item.psNo + item.opNo" :value="item.opNo">{{ item.opNo }} — {{ item.opName }}</option></select></label>
                <label>Node ID<input v-model.trim="draft.nodeId" :readonly="editorReadOnly"></label>
                <label class="wide">OP Script<textarea v-model="draft.opScript" rows="10" :readonly="editorReadOnly || editorMode === 'edit'" /></label>
                <p class="parameter-form-note wide">新增條件 OP 會以相同 PS/OP 建立一筆產品 OP 設定；至少設定 Node ID 或 Script。</p>
              </div>
            </template>
          </div>
          <footer class="parameter-editor-actions">
            <button class="parameter-button confirm" type="button" :disabled="!isEditorActive || editorReadOnly" @click="applyEditor(true)">Confirm</button>
            <button class="parameter-button" type="button" :disabled="!isEditorActive" @click="cancelEditor">Cancel</button>
            <button class="parameter-button" type="button" :disabled="!isEditorActive || editorReadOnly" @click="applyEditor(false)">Apply</button>
          </footer>
        </div>
        <footer class="parameter-right-footer"><span class="parameter-message" role="status">{{ message }}</span><button class="parameter-button" type="button" @click="close">Close</button></footer>
      </section>
    </div>
    <div v-if="context.visible" class="parameter-context" :style="{ left: `${context.x}px`, top: `${context.y}px` }" @click.stop>
      <button v-for="item in menuItems" :key="item.key" type="button" :disabled="editorReadOnly && item.key !== 'view'" @click="startEditor(item.key)">{{ item.label }}</button>
    </div>
  </section>
</template>

<style scoped>
.parameter-window{position:relative;width:min(947px,calc(100vw - 24px));height:min(591px,calc(100vh - 24px));min-height:420px;display:flex;flex-direction:column;background:#f7f6f2;border:1px solid #96938a;box-shadow:0 8px 30px #0005;color:#28271f;font:12px "Segoe UI","Microsoft JhengHei",sans-serif}
.parameter-title{height:27px;flex:none;display:flex;align-items:center;justify-content:space-between;padding:0 8px;background:#efeee9;border-bottom:1px solid #c5c2b8;font-size:13px}.parameter-title button{width:29px;height:23px;border:0;background:transparent;font-size:19px;cursor:pointer}
.parameter-body{display:grid;grid-template-columns:359px minmax(0,1fr);gap:0;flex:1;min-height:0}.parameter-left{display:flex;flex-direction:column;min-width:0;border-right:1px solid #c1beb5;background:#f2f1ed}.parameter-scope{height:50px;flex:none;display:grid;grid-template-columns:73px 1fr;align-content:center;gap:4px 5px;padding:2px 8px}.parameter-scope strong{grid-row:1 / span 2;align-self:center}.parameter-scope label,.parameter-radio-row label{display:flex;align-items:center;gap:4px;white-space:nowrap}.parameter-scope input,.parameter-radio-row input{accent-color:#b4831d}.parameter-tree{flex:1;min-height:0;overflow:auto;margin:0 7px;border:1px solid #aaa79f;background:#fff;padding:4px 0}.parameter-tree-row{width:100%;height:22px;display:flex;align-items:center;gap:2px;border:0;background:transparent;text-align:left;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;padding-top:0;padding-bottom:0;color:#292820;font:inherit;cursor:default}.parameter-tree-row:hover{background:#fff4cf}.parameter-tree-row.selected{background:#ffe8a1}.parameter-tree-row.kind-attribGroup{color:#ab7013;font-weight:600}.parameter-tree-row.kind-ruleGroup{color:#4f6f91;font-weight:600}.parameter-tree-row.kind-equipmentGroup{color:#55734d;font-weight:600}.tree-toggle{width:13px;flex:none;color:#68645b;text-align:center}.parameter-empty-tree{padding:12px;color:#776f5e}.parameter-left-footer,.parameter-right-footer{height:35px;flex:none;display:flex;align-items:center;justify-content:space-between;padding:3px 8px}.parameter-dev-tag{color:#725b17;font-size:11px}.parameter-button{min-width:68px;height:27px;padding:3px 9px;border:1px solid #aaa79c;border-radius:3px;background:linear-gradient(#fff,#f0efe9);color:#292820;font:inherit;white-space:nowrap;cursor:pointer}.parameter-button:hover:not(:disabled){border-color:#bf962f;background:#fff1c7}.parameter-button:disabled{opacity:.5;cursor:not-allowed}.parameter-button.confirm{background:#f5aa30;border-color:#de941e;color:#fff;font-weight:600}
.parameter-right{min-width:0;display:flex;flex-direction:column;padding:8px 8px 0}.parameter-editor-frame{flex:1;min-height:0;display:flex;flex-direction:column;border:1px solid #a9a69d;background:#fdfcf9}.parameter-editor-frame.disabled{background:#f3f2ee}.parameter-tabs{height:27px;flex:none;display:flex;border-bottom:1px solid #bebbb1;background:#eeede8}.parameter-tabs button{min-width:77px;padding:4px 10px;border:0;border-right:1px solid #cbc8bf;background:transparent;color:#36342e;font:inherit;cursor:pointer}.parameter-tabs button.active{background:#fff0c4;border-bottom:2px solid #d7a32b;font-weight:600}.parameter-editor-body{flex:1;min-height:0;overflow:auto;padding:10px}.parameter-instruction{height:100%;display:grid;place-items:center;color:#777267;text-align:center}.parameter-editor-heading{height:25px;display:flex;justify-content:space-between;align-items:center;margin-bottom:8px;border-bottom:1px solid #ddd9ce}.parameter-editor-heading span{color:#7a7568}.parameter-editor-scroll{min-height:0;overflow:auto}.parameter-radio-row{display:flex;gap:18px;padding:2px 3px 8px}.parameter-grid{width:100%;border-collapse:collapse;white-space:nowrap}.parameter-grid th{height:23px;padding:3px 6px;border:1px solid #bcb8aa;background:#efb526;text-align:left}.parameter-grid td{height:24px;padding:3px 6px;border:1px solid #d0cdc4;background:#fff}.parameter-grid tbody tr:nth-child(even) td{background:#faf9f5}.parameter-grid td:first-child{text-align:center}.parameter-grid .empty{padding:14px;text-align:center;color:#777}.parameter-form-grid{display:grid;grid-template-columns:1fr 1fr;align-content:start;gap:9px 12px}.parameter-form-grid label{min-width:0;display:flex;flex-direction:column;gap:3px}.parameter-form-grid label.wide,.parameter-form-grid .wide{grid-column:1/-1}.parameter-form-grid input,.parameter-form-grid select,.parameter-form-grid textarea{width:100%;min-width:0;min-height:25px;padding:3px 6px;border:1px solid #99968d;background:#fff1bd;color:#292820;font:inherit}.parameter-form-grid input:read-only,.parameter-form-grid textarea:read-only{background:#eeede8}.parameter-form-grid textarea{resize:vertical}.parameter-input-lookup{display:flex;gap:4px}.parameter-input-lookup input{flex:1}.parameter-input-lookup button{width:34px;border:1px solid #aaa79c;background:#f2f0ea;cursor:pointer}.parameter-input-lookup button:disabled{opacity:.5}.parameter-form-note{margin:0;padding:6px 8px;border:1px solid #e2d5a6;background:#fff9e1;color:#5e542f;line-height:1.5}.parameter-editor-actions{height:42px;flex:none;display:flex;justify-content:flex-end;align-items:center;gap:6px;padding:5px 8px;border-top:1px solid #d0cdc3}.parameter-right-footer{padding-right:0}.parameter-message{flex:1;overflow:hidden;color:#655f4d;white-space:nowrap;text-overflow:ellipsis}.parameter-context{position:fixed;z-index:120;min-width:170px;padding:4px;border:1px solid #9f9b90;background:#fff;box-shadow:0 4px 14px #0003}.parameter-context button{width:100%;height:27px;padding:4px 8px;border:0;background:#fff;text-align:left;font:inherit;cursor:pointer}.parameter-context button:hover:not(:disabled){background:#ffedba}.parameter-context button:disabled{color:#aaa;cursor:not-allowed}
.recipe-caption{display:block;margin-bottom:4px}
@media(max-width:760px){.parameter-window{height:min(760px,calc(100vh - 16px));min-height:0}.parameter-body{grid-template-columns:minmax(185px,38%) minmax(0,1fr)}.parameter-scope{grid-template-columns:1fr;height:auto;min-height:64px}.parameter-scope strong{grid-row:auto}.parameter-tabs{overflow-x:auto}.parameter-tabs button{min-width:68px;padding:4px 6px}.parameter-form-grid{grid-template-columns:1fr}.parameter-form-grid label.wide,.parameter-form-grid .wide{grid-column:auto}}
</style>
