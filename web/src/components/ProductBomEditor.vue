<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type Row = Record<string, any>
type TreeLine = { id: string; label: string; kind: 'root' | 'material' | 'product' | 'group'; depth: number; directIndex?: number; node?: Row }
type BomDraft = {
  nodeType: 0 | 1 | null
  nodeNo: string
  nodeVersion: string
  versionMode: 'current' | 'specified'
  opNo: string
  stdQty: number | string
  unitNo: string
  putInPlace: string
  decreaseRatePct: number
  specified: number
  description: string
  creator: string
  createDate: string
}

const props = defineProps<{
  product: Row
  bom: Row[]
  materialGroups: Row[]
  products: Row[]
  relations: Record<string, { bom?: Row[]; materialGroups?: Row[] }>
  developmentPreview: boolean
  enterpriseNo?: string
}>()
const emit = defineEmits<{
  close: []
  save: [bom: Row[], materialGroups: Row[]]
}>()

const materials = [
  { materialNo: 'DEV-MTL-001', unitNo: 'EA', putInPlace: 'WIP INV', checkLotNo: false, description: 'DEV 標準物料' },
  { materialNo: 'DEV-MTL-002', unitNo: 'EA', putInPlace: 'MO', checkLotNo: true, description: 'DEV 批號管制物料' },
  { materialNo: 'DEV-MTL-SUB-01', unitNo: 'EA', putInPlace: 'WIP INV', checkLotNo: false, description: 'DEV 子階材料' },
  { materialNo: 'DEV-MTL-COMP-01', unitNo: 'EA', putInPlace: 'WIP INV', checkLotNo: false, description: 'DEV 組件材料' },
  { materialNo: 'DEV-MTL-ALT-01', unitNo: 'EA', putInPlace: 'WIP INV', checkLotNo: false, description: 'DEV 替代材料 A' },
  { materialNo: 'DEV-MTL-ALT-02', unitNo: 'KG', putInPlace: 'WIP INV', checkLotNo: false, description: 'DEV 替代材料 B' },
]
const workingBom = ref<Row[]>([])
const workingGroups = ref<Row[]>([])
const mode = ref<'idle' | 'add' | 'edit' | 'view'>('idle')
const selectedId = ref('')
const selectedIndex = ref(-1)
const draft = ref<BomDraft>(blankDraft())
const error = ref('')
const lookupField = ref('')
const context = ref<{ open: boolean; x: number; y: number; line: TreeLine | null }>({ open: false, x: 0, y: 0, line: null })
const substituteOpen = ref(false)
const substituteSelection = ref<string[]>([])

function blankDraft(): BomDraft {
  return { nodeType: null, nodeNo: '', nodeVersion: '', versionMode: 'current', opNo: '', stdQty: 1, unitNo: '', putInPlace: '', decreaseRatePct: 0, specified: -1, description: '', creator: 'DEV001', createDate: nowText() }
}
function nowText(): string {
  const now = new Date()
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${now.getFullYear()}/${pad(now.getMonth() + 1)}/${pad(now.getDate())} ${pad(now.getHours())}:${pad(now.getMinutes())}:${pad(now.getSeconds())}`
}
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function keyOfProduct(productNo: string, version: string): string {
  if (version === '*') {
    const current = props.products.find(row => String(row.productNo) === productNo && Number(row.curVersion) === 1)
    if (current) return `${productNo}|${current.productVersion}`
  }
  return `${productNo}|${version}`
}
function lineKey(index: number, row: Row): string { return `${index}|${String(row.nodeNo)}|${String(row.opNo)}` }
function percent(value: unknown): string {
  const number = Number(value ?? 0) * 100
  return (Number.isFinite(number) ? number.toFixed(8).replace(/0+$/, '').replace(/\.$/, '') : '0') + '%'
}
function nodeLabel(row: Row): string {
  const isProduct = Number(row.nodeType) === 1
  const version = isProduct ? `,${String(row.nodeVersion || '*')}` : ''
  const specified = Number(row.specified) === 1 ? 'Specified material' : 'Not specified material'
  const description = row.description ? ` ${row.description}` : ''
  return `${row.nodeNo}${version} (${row.opNo}:${row.stdQty}/${row.unitNo}, DecreaseRate:${percent(row.decreaseRate)}, ${specified})${description}`
}
function appendChildren(productNo: string, productVersion: string, depth: number, ancestors: Set<string>, out: TreeLine[], path: string): void {
  if (depth > 5) return
  const key = keyOfProduct(productNo, productVersion)
  const relation = props.relations[key]
  const sortedBom = [...(relation?.bom ?? [])].sort((left, right) => Number(right.nodeType) - Number(left.nodeType))
  for (const [index, node] of sortedBom.entries()) {
    const isProduct = Number(node.nodeType) === 1
    const id = `${path}/nested-${index}-${node.nodeNo}-${node.opNo}`
    out.push({ id, label: nodeLabel(node), kind: isProduct ? 'product' : 'material', depth, node })
    if (!isProduct) {
      const groups = (relation?.materialGroups ?? []).filter(group => group.nodeNo === node.nodeNo && group.opNo === node.opNo)
      groups.forEach((group, groupIndex) => out.push({ id: `${id}/group-${groupIndex}`, label: String(group.materialNo), kind: 'group', depth: depth + 1, node: group }))
    }
    if (isProduct) {
      const childKey = keyOfProduct(String(node.nodeNo), String(node.nodeVersion || '*'))
      if (!ancestors.has(childKey)) {
        const next = new Set(ancestors)
        next.add(childKey)
        appendChildren(String(node.nodeNo), String(node.nodeVersion || '*'), depth + 1, next, out, id)
      }
    }
  }
}
const readOnly = computed(() => !props.developmentPreview)
const editorDisabled = computed(() => readOnly.value || !['add', 'edit'].includes(mode.value))
const treeLines = computed<TreeLine[]>(() => {
  const root: TreeLine = { id: 'root', label: `${props.product.productNo}:${props.product.productVersion}`, kind: 'root', depth: 0 }
  const lines: TreeLine[] = [root]
  const ancestors = new Set([`${props.product.productNo}|${props.product.productVersion}`])
  const sortedBom = [...workingBom.value].sort((left, right) => Number(right.nodeType) - Number(left.nodeType))
  sortedBom.forEach(node => {
    const index = workingBom.value.indexOf(node)
    const isProduct = Number(node.nodeType) === 1
    const id = lineKey(index, node)
    lines.push({ id, label: nodeLabel(node), kind: isProduct ? 'product' : 'material', depth: 1, directIndex: index, node })
    if (!isProduct) {
      workingGroups.value.filter(group => group.nodeNo === node.nodeNo && group.opNo === node.opNo)
        .forEach((group, groupIndex) => lines.push({ id: `${id}/group-${groupIndex}`, label: String(group.materialNo), kind: 'group', depth: 2, node: group }))
    } else {
      const childKey = keyOfProduct(String(node.nodeNo), String(node.nodeVersion || '*'))
      if (!ancestors.has(childKey)) appendChildren(String(node.nodeNo), String(node.nodeVersion || '*'), 2, new Set([...ancestors, childKey]), lines, id)
    }
  })
  return lines
})
const approvedProducts = computed(() => props.products.filter(row => Number(row.issueState) === 2))
const productVersions = computed(() => approvedProducts.value.filter(row => row.productNo === draft.value.nodeNo))
const lookupOptions = computed(() => lookupField.value === 'ProductNo' ? approvedProducts.value : materials)
const operationOptions = computed(() => [...new Set(['DEV-OP-10', 'DEV-OP-20', 'DEV-OP-30', 'DEV-OP-40', 'DEV-OP-50', ...workingBom.value.map(row => String(row.opNo)), 'LOTCREATE'])])
const currentMaterial = computed(() => materials.find(row => row.materialNo === draft.value.nodeNo))
const canSubstitute = computed(() => Boolean(context.value.line?.directIndex !== undefined && Number(context.value.line.node?.nodeType) === 0 && Number(context.value.line.node?.specified) === 0))

watch(() => `${props.product.productNo}|${props.product.productVersion}`, () => {
  const bom = props.bom
  const groups = props.materialGroups
  workingBom.value = clone(bom ?? [])
  workingGroups.value = clone(groups ?? [])
  selectedId.value = ''
  selectedIndex.value = -1
  mode.value = 'idle'
}, { immediate: true })

function selectLine(line: TreeLine): void {
  selectedId.value = line.id
  selectedIndex.value = line.directIndex ?? -1
}
function openContext(event: MouseEvent, line: TreeLine): void {
  event.preventDefault()
  selectLine(line)
  context.value = { open: true, x: Math.min(event.clientX, window.innerWidth - 190), y: Math.min(event.clientY, window.innerHeight - 170), line }
}
function startAdd(): void {
  if (readOnly.value) return
  error.value = ''
  draft.value = blankDraft()
  mode.value = 'add'
  context.value.open = false
}
function startEdit(viewOnly = false, targetIndex = context.value.line?.directIndex ?? selectedIndex.value): void {
  const index = targetIndex
  if (index === undefined || (readOnly.value && !viewOnly)) return
  const row = workingBom.value[index]
  draft.value = {
    nodeType: Number(row.nodeType) === 1 ? 1 : 0,
    nodeNo: String(row.nodeNo ?? ''),
    nodeVersion: String(row.nodeVersion ?? 'N/A'),
    versionMode: String(row.nodeVersion) === '*' ? 'current' : 'specified',
    opNo: String(row.opNo ?? ''),
    stdQty: row.stdQty ?? 0,
    unitNo: String(row.unitNo ?? ''),
    putInPlace: Number(row.putInPlaceType) === 3 ? 'MO' : 'WIP INV',
    decreaseRatePct: Number(row.decreaseRate ?? 0) * 100,
    specified: Number(row.specified ?? 0),
    description: String(row.description ?? ''),
    creator: String(row.creator ?? 'DEV001'),
    createDate: String(row.createDate ?? ''),
  }
  selectedIndex.value = index
  mode.value = viewOnly ? 'view' : 'edit'
  context.value.open = false
  error.value = ''
}
function clearEditor(): void { mode.value = 'idle'; draft.value = blankDraft(); error.value = '' }
function changeNodeType(type: 0 | 1): void {
  if (readOnly.value || mode.value !== 'add') return
  draft.value.nodeType = type
  draft.value.nodeNo = ''
  draft.value.nodeVersion = type === 1 ? '' : 'N/A'
  draft.value.versionMode = 'current'
  draft.value.unitNo = ''
  draft.value.putInPlace = type === 1 ? 'MO' : 'WIP INV'
}
function chooseLookup(item: Row): void {
  if (lookupField.value === 'ProductNo') {
    const current = approvedProducts.value.find(row => row.productNo === item.productNo && Number(row.curVersion) === 1) ?? item
    draft.value.nodeNo = String(item.productNo)
    draft.value.nodeVersion = draft.value.versionMode === 'current' ? '*' : String(item.productVersion)
    draft.value.unitNo = String(item.unitNo ?? current.unitNo ?? '')
  } else {
    draft.value.nodeNo = String(item.materialNo)
    draft.value.nodeVersion = 'N/A'
    draft.value.unitNo = String(item.unitNo)
    draft.value.putInPlace = String(item.putInPlace)
  }
  lookupField.value = ''
}
function validateDraft(): string {
  if (draft.value.nodeType === null) return '請選擇 Product 或 Material。'
  if (draft.value.nodeType === 1 && !draft.value.nodeNo) return '請選擇 ProductNo。'
  if (draft.value.nodeType === 0 && !draft.value.nodeNo) return '請選擇 MaterialNo。'
  if (draft.value.nodeType === 1 && !draft.value.nodeVersion) return '請選擇 ProductVersion。'
  if (!draft.value.opNo) return '請選擇 Input OPNo。'
  if (!draft.value.putInPlace) return '請選擇 Put In Place Type。'
  if (String(draft.value.stdQty).trim() === '') return '請輸入 Standard Use Qty。'
  const qty = Number(draft.value.stdQty)
  if (!Number.isFinite(qty) || qty < 0) return 'Standard Use Qty 必須是大於或等於 0 的數值。'
  if (String(draft.value.decreaseRatePct).trim() === '') return '請輸入 Decrease Rate。'
  const rate = Number(draft.value.decreaseRatePct)
  if (!Number.isFinite(rate) || rate < 0 || rate > 99) return 'Decrease Rate 必須介於 0 至 99。'
  if (![0, 1].includes(Number(draft.value.specified))) return '請選擇 Specified。'
  if (draft.value.opNo.toUpperCase() === 'LOTCREATE') {
    if (draft.value.putInPlace !== 'MO') return '選擇 LOTCREATE 時，Put In Place Type 必須是 MO。'
    if (draft.value.nodeType === 1) return 'Product 節點不可使用 LOTCREATE。'
    if (currentMaterial.value?.checkLotNo && String(props.enterpriseNo ?? '').toUpperCase() !== 'TS') return '批號管制物料不可使用 LOTCREATE。'
  }
  return ''
}
function makeBomRow(): Row {
  return {
    nodeNo: draft.value.nodeNo,
    nodeType: draft.value.nodeType ?? 0,
    nodeVersion: draft.value.nodeType === 0 ? 'N/A' : draft.value.versionMode === 'current' ? '*' : draft.value.nodeVersion,
    opNo: draft.value.opNo,
    stdQty: Number(draft.value.stdQty),
    unitNo: draft.value.unitNo,
    putInPlaceType: draft.value.putInPlace === 'MO' ? 3 : draft.value.nodeType === 1 ? 4 : 2,
    decreaseRate: Number(draft.value.decreaseRatePct) / 100,
    specified: Number(draft.value.specified),
    description: draft.value.description,
    creator: draft.value.creator || 'DEV001',
    createDate: draft.value.createDate || new Date().toISOString(),
  }
}
function commitRelation(): void { emit('save', clone(workingBom.value), clone(workingGroups.value)) }
function saveEditor(closeAfter: boolean): void {
  error.value = validateDraft()
  if (error.value || readOnly.value || !['add', 'edit'].includes(mode.value)) return
  const row = makeBomRow()
  if (mode.value === 'add') {
    if (workingBom.value.some(item => item.nodeNo === row.nodeNo && item.opNo === row.opNo)) { error.value = '相同 NodeNo 與 Input OPNo 已存在。'; return }
    workingBom.value = [...workingBom.value, row]
    selectedIndex.value = workingBom.value.length - 1
    selectedId.value = lineKey(selectedIndex.value, row)
    commitRelation()
    if (closeAfter) clearEditor()
    else { draft.value = blankDraft(); mode.value = 'add' }
  } else {
    const original = workingBom.value[selectedIndex.value]
    if (original && (original.nodeNo !== row.nodeNo || original.opNo !== row.opNo) && workingBom.value.some((item, index) => index !== selectedIndex.value && item.nodeNo === row.nodeNo && item.opNo === row.opNo)) { error.value = '相同 NodeNo 與 Input OPNo 已存在。'; return }
    workingBom.value = workingBom.value.map((item, index) => index === selectedIndex.value ? row : item)
    commitRelation()
    if (closeAfter) clearEditor()
  }
}
function deleteNode(): void {
  const index = context.value.line?.directIndex
  if (index === undefined || readOnly.value) return
  const item = workingBom.value[index]
  if (!window.confirm(`確定刪除 BOM 節點 ${item.nodeNo} / ${item.opNo}？`)) return
  workingBom.value.splice(index, 1)
  workingGroups.value = workingGroups.value.filter(group => !(group.nodeNo === item.nodeNo && group.opNo === item.opNo))
  selectedIndex.value = -1
  selectedId.value = ''
  context.value.open = false
  commitRelation()
}
function openSubstitutes(): void {
  const item = context.value.line?.node
  if (!item || !canSubstitute.value) return
  substituteSelection.value = workingGroups.value.filter(group => group.nodeNo === item.nodeNo && group.opNo === item.opNo).map(group => String(group.materialNo))
  substituteOpen.value = true
  context.value.open = false
}
function saveSubstitutes(): void {
  const item = workingBom.value[selectedIndex.value]
  if (!item) { substituteOpen.value = false; return }
  workingGroups.value = workingGroups.value.filter(group => !(group.nodeNo === item.nodeNo && group.opNo === item.opNo))
    .concat(substituteSelection.value.map(materialNo => ({ nodeNo: item.nodeNo, opNo: item.opNo, materialNo })))
  substituteOpen.value = false
  commitRelation()
}
</script>

<template>
  <section class="bom-window" role="dialog" aria-modal="true" aria-label="Product BOM">
    <section class="bom-tree-panel">
      <fieldset class="bom-tree-fieldset"><legend>Product BOM</legend>
        <div class="bom-tree" role="tree" @click="context.open=false">
          <button v-for="line in treeLines" :key="line.id" type="button" role="treeitem" class="bom-tree-line" :class="[`kind-${line.kind}`, { selected: selectedId === line.id }]" :style="{ paddingLeft: `${8 + line.depth * 17}px` }" @click.stop="selectLine(line)" @dblclick.stop="line.directIndex !== undefined && startEdit(false,line.directIndex)" @contextmenu="openContext($event,line)">
            <span class="tree-glyph">{{ line.kind === 'root' || line.kind === 'product' ? '⊞' : line.kind === 'group' ? '├' : '└' }}</span>{{ line.label }}
          </button>
          <div v-if="!workingBom.length" class="bom-tree-empty">目前產品版本沒有 BOM 明細。</div>
        </div>
        <button class="prd-button tree-refresh" type="button" @click="selectedId=''; selectedIndex=-1">Refresh</button>
      </fieldset>
    </section>

    <section class="bom-node-panel">
      <fieldset class="bom-node-fieldset"><legend>Node</legend>
        <fieldset class="bom-node-define"><legend>Node Define</legend>
          <div class="node-type-row"><label><input v-model.number="draft.nodeType" :disabled="readOnly || mode !== 'add'" type="radio" :value="1" @change="changeNodeType(1)" />Product</label><label><input v-model.number="draft.nodeType" :disabled="readOnly || mode !== 'add'" type="radio" :value="0" @change="changeNodeType(0)" />Material</label></div>
          <div class="node-fields">
            <template v-if="draft.nodeType === 1">
              <label class="bom-field"><span>Product No</span><div class="bom-control"><input readonly :value="draft.nodeNo" /><button class="prd-button" type="button" :disabled="editorDisabled || mode !== 'add'" @click="lookupField='ProductNo'">...</button></div></label>
              <fieldset class="bom-version"><legend>Product Version</legend><label><input v-model="draft.versionMode" type="radio" value="current" :disabled="editorDisabled || mode !== 'add'" @change="draft.nodeVersion='*'" />Current</label><label><input v-model="draft.versionMode" type="radio" value="specified" :disabled="editorDisabled || mode !== 'add'" @change="draft.nodeVersion=''" />Specified</label><select v-if="draft.versionMode === 'specified'" v-model="draft.nodeVersion" :disabled="editorDisabled || mode !== 'add'"><option value="">選擇版本</option><option v-for="row in productVersions" :key="row.productVersion" :value="row.productVersion">{{ row.productVersion }}</option></select></fieldset>
            </template>
            <label v-else-if="draft.nodeType === 0" class="bom-field"><span>Material No</span><div class="bom-control"><input readonly :value="draft.nodeNo" /><button class="prd-button" type="button" :disabled="editorDisabled || mode !== 'add'" @click="lookupField='MaterialNo'">...</button></div></label>
            <label class="bom-field"><span>Input OPNo</span><select v-model="draft.opNo" :disabled="editorDisabled"><option value="">請選擇</option><option v-for="op in operationOptions" :key="op">{{ op }}</option></select></label>
            <label class="bom-field"><span>Standard Use Qty</span><input v-model="draft.stdQty" :disabled="editorDisabled" type="number" min="0" step="0.000001" /></label>
            <label class="bom-field"><span>Unit No</span><input readonly :value="draft.unitNo" /></label>
            <label class="bom-field"><span>Decrease Rate</span><div class="percent-control"><input v-model.number="draft.decreaseRatePct" :disabled="editorDisabled" type="number" min="0" max="99" step="0.01" /><span>%</span></div></label>
            <label class="bom-field"><span>Put In Place Type</span><select v-model="draft.putInPlace" :disabled="editorDisabled"><option value="WIP INV">WIP INV</option><option value="MO">MO</option></select></label>
            <label class="bom-field"><span>Specified</span><select v-model.number="draft.specified" :disabled="editorDisabled"><option :value="0">No</option><option :value="1">Yes</option></select></label>
            <label class="bom-field bom-description"><span>Description</span><textarea v-model="draft.description" :disabled="editorDisabled" rows="3" /></label>
            <label class="bom-field"><span>Creator</span><input readonly :value="draft.creator" /></label>
            <label class="bom-field"><span>CreateDate</span><input readonly :value="draft.createDate" /></label>
          </div>
          <div v-if="error" class="bom-error">{{ error }}</div>
          <div class="bom-edit-buttons"><button class="prd-button" type="button" :disabled="readOnly || !['add','edit'].includes(mode)" @click="saveEditor(true)">Confirm</button><button class="prd-button" type="button" :disabled="readOnly || !['add','edit'].includes(mode)" @click="saveEditor(false)">Apply</button><button class="prd-button" type="button" :disabled="mode==='idle'" @click="clearEditor">Cancel</button></div>
        </fieldset>
        <button class="prd-button bom-close" type="button" @click="emit('close')">Close</button>
      </fieldset>
    </section>

    <div v-if="context.open" class="bom-context" :style="{ left: `${context.x}px`, top: `${context.y}px` }" @click.stop>
      <template v-if="context.line?.kind === 'root'"><button type="button" :disabled="readOnly" @click="startAdd">Add</button></template>
      <template v-else-if="context.line?.directIndex !== undefined"><button type="button" :disabled="readOnly" @click="startEdit(false)">Edit</button><button type="button" @click="startEdit(true)">View</button><button type="button" :disabled="readOnly" @click="deleteNode">Del</button><button v-if="canSubstitute" type="button" :disabled="readOnly" @click="openSubstitutes">Set Substitute MTL</button></template>
      <span v-else class="menu-disabled">此階層僅供檢視</span>
    </div>

    <div v-if="lookupField" class="bom-sub-overlay" @click.self="lookupField=''"><section class="bom-pick-dialog"><header><strong>選擇 {{ lookupField }}</strong><button type="button" @click="lookupField=''">×</button></header><div class="bom-pick-list"><button v-for="item in lookupOptions" :key="lookupField === 'ProductNo' ? `${item.productNo}|${item.productVersion}` : item.materialNo" type="button" @click="chooseLookup(item)">{{ lookupField === 'ProductNo' ? `${item.productNo} / ${item.productVersion} / ${item.productName}` : `${item.materialNo} / ${item.unitNo} / ${item.description}` }}</button><p v-if="!lookupOptions.length">沒有已核准的可選資料。</p></div></section></div>

    <div v-if="substituteOpen" class="bom-sub-overlay" @click.self="substituteOpen=false"><section class="bom-pick-dialog"><header><strong>Set Substitute MTL</strong><button type="button" @click="substituteOpen=false">×</button></header><p class="substitute-note">未指定用料可以維護替代料群組。</p><label v-for="item in materials.filter(row => row.materialNo !== context.line?.node?.nodeNo)" :key="item.materialNo" class="substitute-choice"><input v-model="substituteSelection" type="checkbox" :value="item.materialNo" />{{ item.materialNo }}　{{ item.description }}</label><footer><button class="prd-button" type="button" @click="substituteOpen=false">Cancel</button><button class="prd-button confirm" type="button" @click="saveSubstitutes">OK</button></footer></section></div>
  </section>
</template>

<style scoped>
.bom-window{position:relative;box-sizing:border-box;width:min(719px,calc(100vw - 20px));height:min(516px,calc(100vh - 20px));display:grid;grid-template-columns:minmax(0,304fr) minmax(0,413fr);gap:2px;background:#fdfcf9;border:1px solid #99968d;box-shadow:0 8px 30px #0005;color:#28271f;font:12px "Segoe UI","Microsoft JhengHei",sans-serif;overflow:hidden}.bom-tree-panel,.bom-node-panel{min-width:0;min-height:0}.bom-tree-fieldset,.bom-node-fieldset{box-sizing:border-box;position:relative;height:100%;margin:0;padding:6px;border:1px solid #aaa79f;min-width:0}.bom-tree-fieldset legend,.bom-node-fieldset legend,.bom-node-define legend{padding:0 4px;color:#514d40}.bom-tree{position:absolute;inset:9px 6px 47px;overflow:auto;border:1px solid #d2cfc6;background:#fff;padding:3px 0}.bom-tree-line{display:block;width:max-content;min-width:100%;max-width:none;height:20px;padding-top:1px;padding-bottom:1px;border:0;background:transparent;text-align:left;white-space:nowrap;font:11px "Segoe UI","Microsoft JhengHei",sans-serif;cursor:default}.bom-tree-line:hover{background:#fff2c6}.bom-tree-line.selected{background:#ffe69a}.tree-glyph{display:inline-block;width:17px;color:#777}.kind-material{color:darkred}.kind-product{color:blue}.kind-group{color:green}.kind-root{color:#27251f;font-weight:600}.bom-tree-empty{padding:14px;color:#777}.tree-refresh{position:absolute;left:8px;bottom:6px;width:80px;height:32px}.bom-node-fieldset{padding:6px}.bom-node-define{box-sizing:border-box;position:absolute;inset:9px 8px 42px;margin:0;padding:7px;border:1px solid #aaa79f;min-width:0}.bom-node-define legend{font-weight:600}.node-type-row{position:absolute;left:18px;top:15px;display:flex;gap:14px}.node-type-row label,.bom-version label{display:flex;align-items:center;gap:3px}.node-type-row input,.bom-version input,.substitute-choice input{accent-color:#b4831d}.node-fields{position:absolute;left:5px;right:5px;top:42px;bottom:66px;overflow:auto;display:flex;flex-direction:column;gap:6px}.bom-field{min-height:25px;display:grid;grid-template-columns:100px minmax(0,1fr);align-items:center;gap:7px}.bom-field>span{text-align:right}.bom-field input,.bom-field select,.bom-field textarea,.bom-version select{box-sizing:border-box;width:100%;height:24px;min-width:0;border:1px solid #99968d;background:#fff2bd;padding:2px 5px;color:#28271f;font:inherit}.bom-field input:read-only{background:#efeee9}.bom-control{display:flex;gap:4px}.bom-control input{flex:1}.bom-control button{width:32px;min-height:24px;height:24px;padding:0}.bom-version{position:relative;display:flex;align-items:center;gap:8px;margin:0 0 0 107px;padding:5px 6px 4px;height:37px;border:1px solid #aaa79f}.bom-version legend{font-size:11px}.bom-version select{width:72px}.percent-control{display:flex;align-items:center;gap:4px}.percent-control input{width:90px}.bom-description{align-items:start}.bom-description textarea{height:48px;resize:vertical}.bom-edit-buttons{position:absolute;right:9px;bottom:8px;display:flex;gap:6px}.bom-edit-buttons button{width:78px;height:31px}.bom-close{position:absolute;right:8px;bottom:5px;width:80px;height:31px}.bom-error{position:absolute;left:7px;right:7px;bottom:42px;padding:4px 7px;border:1px solid #d29a8e;background:#fff1ed;color:#7c281d}.bom-context{position:fixed;z-index:92;min-width:165px;padding:3px;background:#fff;border:1px solid #89867e;box-shadow:0 3px 10px #0003}.bom-context button{display:block;width:100%;padding:5px 10px;border:0;background:#fff;text-align:left;font:inherit;cursor:pointer}.bom-context button:hover:not(:disabled){background:#fff0c0}.bom-context button:disabled{color:#999}.menu-disabled{display:block;padding:6px 9px;color:#888}.bom-sub-overlay{position:fixed;inset:0;z-index:101;display:grid;place-items:center;background:#0005;padding:14px}.bom-pick-dialog{width:min(460px,calc(100vw - 24px));max-height:min(450px,calc(100vh - 24px));overflow:auto;background:#fdfcf9;border:1px solid #99968d;box-shadow:0 8px 25px #0005}.bom-pick-dialog header{display:flex;justify-content:space-between;align-items:center;padding:8px 10px;background:#f5f2e9;border-bottom:1px solid #d6d2c7}.bom-pick-dialog header button{border:0;background:transparent;font-size:20px;cursor:pointer}.bom-pick-list{display:flex;flex-direction:column;padding:7px;gap:4px}.bom-pick-list button{padding:7px;text-align:left;border:1px solid #c8c4b9;background:#fff;font:inherit;cursor:pointer}.bom-pick-list button:hover{background:#fff1bd}.bom-pick-list p,.substitute-note{padding:8px 10px;color:#666}.substitute-choice{display:flex;align-items:center;gap:7px;padding:6px 12px;border-bottom:1px solid #eee}.bom-pick-dialog footer{display:flex;justify-content:flex-end;gap:6px;padding:9px;border-top:1px solid #d7d3c7}.bom-readonly{position:absolute;z-index:2;left:8px;right:8px;bottom:5px;margin:0;padding:4px 8px;background:#fff8dc;border:1px solid #e4d7a3;color:#5f5427}
@media(max-width:700px){.bom-window{grid-template-columns: minmax(0,1fr) minmax(0,1.15fr)}.bom-field{grid-template-columns:83px minmax(0,1fr);gap:4px}.bom-version{margin-left:88px}.bom-field>span{font-size:11px}}
.prd-button{min-height:28px;padding:4px 8px;border:1px solid #aaa79c;border-radius:3px;background:linear-gradient(#fff,#f3f1eb);color:#25241e;font:inherit;white-space:nowrap;cursor:pointer}.prd-button:hover:not(:disabled){border-color:#c49a35;background:#fff5d6}.prd-button:disabled{opacity:.5;cursor:not-allowed}.prd-button.confirm{background:#f7a92c;border-color:#e1971f;color:#fff;font-weight:600}
</style>
