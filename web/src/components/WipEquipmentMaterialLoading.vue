<script setup lang="ts">
import { computed, ref } from 'vue'

type LoadingMode = 'material' | 'component' | 'semi'
type Equipment = { equipmentNo: string; equipmentName: string; equipmentClass: string; issueState: number }
type WorkOrder = { moNo: string; productNo: string; state: number }
type Material = { materialNo: string; materialName: string; checkLotNo: boolean; requiresComponent: boolean; unitNo: string }
type SemiLot = { lotNo: string; productNo: string; quantity: number }
type MountedComponent = { id: string; componentNo: string; goodQty: number; scrapQty: number }
type MountedMaterial = {
  id: string; equipmentNo: string; materialNo: string; inputMaterialNo: string; materialLotNo: string
  checkLotNo: boolean; materialLevel: 0 | 1; putInPlaceType: 2 | 3 | 4; mono: string; seq: number
  inputQty: number; qty: number; unitNo: string; components: MountedComponent[]
}
type Snapshot = { version: 1; rows: MountedMaterial[]; stock: Record<string, number>; semiStock: Record<string, number> }
type Selection = { kind: 'group' | 'material' | 'component'; groupKey: string; stateId?: string; componentId?: string }
type Lookup = null | 'equipment' | 'mo' | 'unmount'

const props = defineProps<{ currentUserNo?: string; developmentPreview?: boolean; equipmentNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const storageKey = 'ymes.dev.wp301-eqp-material-loading.v1'

const equipments: Equipment[] = [
  { equipmentNo: 'DEV-EQP-01', equipmentName: '貼片機 A01', equipmentClass: '一般設備', issueState: 2 },
  { equipmentNo: 'DEV-EQP-02', equipmentName: '點膠機 B02', equipmentClass: '一般設備', issueState: 2 },
  { equipmentNo: 'DEV-EQP-VIRTUAL', equipmentName: '虛擬設備', equipmentClass: 'VIRTUAL', issueState: 2 },
  { equipmentNo: 'DEV-EQP-HOLD', equipmentName: '停用設備', equipmentClass: '一般設備', issueState: 1 },
]
const workOrders: WorkOrder[] = [
  { moNo: 'DEV-MO-202611-001', productNo: 'DEV-PRODUCT-01', state: 1 },
  { moNo: 'DEV-MO-202611-002', productNo: 'DEV-PRODUCT-02', state: 3 },
  { moNo: 'DEV-MO-202611-003', productNo: 'DEV-PRODUCT-03', state: 6 },
  { moNo: 'DEV-MO-CLOSED-01', productNo: 'DEV-PRODUCT-04', state: 7 },
]
const materials: Material[] = [
  { materialNo: 'DEV-MTL-SOLDER-01', materialName: '錫膏 SAC305', checkLotNo: true, requiresComponent: false, unitNo: 'G' },
  { materialNo: 'DEV-MTL-GREASE-02', materialName: '散熱膏', checkLotNo: false, requiresComponent: false, unitNo: 'G' },
  { materialNo: 'DEV-MTL-SUBSTRATE-03', materialName: '封裝基板', checkLotNo: true, requiresComponent: true, unitNo: 'PCS' },
]
const semiLots: SemiLot[] = [
  { lotNo: 'DEV-SEMI-LOT-001', productNo: 'DEV-PRODUCT-01', quantity: 24 },
  { lotNo: 'DEV-SEMI-LOT-002', productNo: 'DEV-PRODUCT-02', quantity: 12 },
]
const lotStock: Record<string, number> = {
  'DEV-MTL-SOLDER-01|DEV-SOLDER-LOT-01': 95,
  'DEV-MTL-SOLDER-01|DEV-SOLDER-LOT-02': 42,
  'DEV-MTL-GREASE-02|N/A': 200,
  'DEV-MTL-SUBSTRATE-03|DEV-SUB-LOT-01': 8,
}
const semiStock: Record<string, number> = { 'DEV-SEMI-LOT-001': 4, 'DEV-SEMI-LOT-002': 12 }

function seedRows(): MountedMaterial[] {
  return [
    {
      id: 'wp301-seed-1', equipmentNo: 'DEV-EQP-01', materialNo: 'DEV-MTL-SOLDER-01',
      inputMaterialNo: 'DEV-MTL-SOLDER-01', materialLotNo: 'DEV-SOLDER-LOT-01', checkLotNo: true,
      materialLevel: 0, putInPlaceType: 2, mono: 'N/A', seq: 1, inputQty: 5, qty: 5, unitNo: 'G', components: [],
    },
    {
      id: 'wp301-seed-2', equipmentNo: 'DEV-EQP-01', materialNo: 'DEV-MTL-SUBSTRATE-03',
      inputMaterialNo: 'DEV-MTL-SUBSTRATE-03', materialLotNo: 'DEV-SUB-LOT-01', checkLotNo: true,
      materialLevel: 0, putInPlaceType: 2, mono: 'N/A', seq: 1, inputQty: 2, qty: 2, unitNo: 'PCS',
      components: [
        { id: 'wp301-seed-c1', componentNo: 'DEV-COMP-0001', goodQty: 1, scrapQty: 0 },
        { id: 'wp301-seed-c2', componentNo: 'DEV-COMP-0002', goodQty: 1, scrapQty: 0 },
      ],
    },
    {
      id: 'wp301-seed-3', equipmentNo: 'DEV-EQP-01', materialNo: 'DEV-PRODUCT-01',
      inputMaterialNo: 'DEV-PRODUCT-01', materialLotNo: 'DEV-SEMI-LOT-001', checkLotNo: true,
      materialLevel: 1, putInPlaceType: 4, mono: 'N/A', seq: 1, inputQty: 20, qty: 20, unitNo: 'PCS', components: [],
    },
    {
      id: 'wp301-seed-4', equipmentNo: 'DEV-EQP-02', materialNo: 'DEV-MTL-GREASE-02',
      inputMaterialNo: 'DEV-MTL-GREASE-02', materialLotNo: 'N/A', checkLotNo: false,
      materialLevel: 0, putInPlaceType: 2, mono: 'N/A', seq: 1, inputQty: 10, qty: 10, unitNo: 'G', components: [],
    },
  ]
}

function seedSnapshot(): Snapshot {
  return { version: 1, rows: seedRows(), stock: { ...lotStock }, semiStock: { ...semiStock } }
}
function readSnapshot(): Snapshot {
  if (!dev.value) return seedSnapshot()
  try {
    const raw = window.localStorage.getItem(storageKey)
    if (!raw) return seedSnapshot()
    const value = JSON.parse(raw) as Snapshot
    if (value.version === 1 && Array.isArray(value.rows) && value.stock && value.semiStock) return value
  } catch { /* keep the deterministic DEV seed */ }
  return seedSnapshot()
}

const snapshot = ref<Snapshot>(readSnapshot())
const equipmentInput = ref(props.equipmentNo ?? '')
const currentEquipmentNo = ref('')
const appointWorkOrder = ref(false)
const workOrderInput = ref('N/A')
const currentWorkOrderNo = ref('')
const mode = ref<LoadingMode>('material')
const materialInput = ref('')
const materialLotInput = ref('N/A')
const quantityInput = ref('')
const enterMaterialLot = ref(false)
const componentMaterialInput = ref('')
const componentLotInput = ref('')
const componentNoInput = ref('')
const semiLotInput = ref('')
const semiProductInput = ref('')
const semiQuantityInput = ref('')
const selection = ref<Selection | null>(null)
const lookup = ref<Lookup>(null)
const actionError = ref('')
const actionMessage = ref('')

const allowedEquipments = computed(() => equipments.filter(item => item.issueState === 2 && item.equipmentClass !== 'VIRTUAL'))
const allowedWorkOrders = computed(() => workOrders.filter(item => [0, 1, 2, 3, 6].includes(item.state)))
const currentEquipment = computed(() => equipments.find(item => item.equipmentNo === currentEquipmentNo.value))
const mountedRows = computed(() => currentEquipmentNo.value
  ? snapshot.value.rows.filter(row => row.equipmentNo === currentEquipmentNo.value)
  : [])
const groups = computed(() => {
  const buckets = new Map<string, MountedMaterial[]>()
  for (const row of mountedRows.value) {
    const key = [row.materialNo, row.checkLotNo ? 1 : 0, row.materialLevel, row.putInPlaceType].join('|')
    if (!buckets.has(key)) buckets.set(key, [])
    buckets.get(key)!.push(row)
  }
  return [...buckets.entries()].map(([key, rows]) => ({
    key, materialNo: rows[0].materialNo, checkLotNo: rows[0].checkLotNo,
    materialLevel: rows[0].materialLevel, putInPlaceType: rows[0].putInPlaceType,
    rows: [...rows].sort((a, b) => a.seq - b.seq || a.inputMaterialNo.localeCompare(b.inputMaterialNo)),
  })).sort((a, b) => a.putInPlaceType - b.putInPlaceType || a.materialNo.localeCompare(b.materialNo))
})
const selectedMaterial = computed(() => selection.value?.stateId
  ? snapshot.value.rows.find(item => item.id === selection.value?.stateId) ?? null
  : null)
const selectedComponent = computed(() => {
  const row = selectedMaterial.value
  return row && selection.value?.componentId
    ? row.components.find(item => item.id === selection.value?.componentId) ?? null
    : null
})
const unmountLabel = computed(() => selectedComponent.value
  ? '元件 ' + selectedComponent.value.componentNo
  : selectedMaterial.value
    ? selectedMaterial.value.inputMaterialNo + ' / ' + selectedMaterial.value.materialLotNo
    : '')
const putInPlaceLabels: Record<number, string> = { 2: '在製品庫(原物料)', 3: '工單', 4: '在製品庫(半成品)' }
const putInPlaceName = (value: number) => putInPlaceLabels[value] ?? String(value)

function persist(): void {
  if (!dev.value) return
  window.localStorage.setItem(storageKey, JSON.stringify(snapshot.value))
}
function showError(message: string): void { actionError.value = message; actionMessage.value = '' }
function showMessage(message: string): void { actionMessage.value = message; actionError.value = '' }
function clearMessage(): void { actionMessage.value = ''; actionError.value = '' }
function resetDev(): void {
  snapshot.value = seedSnapshot()
  equipmentInput.value = ''
  currentEquipmentNo.value = ''
  selection.value = null
  persist()
  showMessage('已還原 WP301 DEV 假資料。')
}
function queryEquipment(): void {
  clearMessage()
  const equipmentNo = equipmentInput.value.trim().toUpperCase()
  if (!equipmentNo) { showError('請輸入設備編號。'); return }
  const equipment = allowedEquipments.value.find(item => item.equipmentNo.toUpperCase() === equipmentNo)
  if (!equipment) { showError('找不到已核准且非虛擬的設備。'); return }
  if (appointWorkOrder.value) {
    const moNo = workOrderInput.value.trim().toUpperCase()
    if (!moNo || !allowedWorkOrders.value.some(item => item.moNo.toUpperCase() === moNo)) {
      showError('指定工單必須存在，且狀態須為 0、1、2、3 或 6。')
      return
    }
    currentWorkOrderNo.value = moNo
  } else {
    currentWorkOrderNo.value = ''
    workOrderInput.value = 'N/A'
  }
  currentEquipmentNo.value = equipment.equipmentNo
  equipmentInput.value = equipment.equipmentNo
  selection.value = null
  clearMessage()
}
function toggleWorkOrder(): void {
  if (appointWorkOrder.value) {
    workOrderInput.value = ''
    currentWorkOrderNo.value = ''
  } else {
    workOrderInput.value = 'N/A'
    currentWorkOrderNo.value = ''
    if (currentEquipmentNo.value) selection.value = null
  }
}
function openLookup(target: 'equipment' | 'mo'): void {
  if (target === 'equipment') equipSearch.value = equipmentInput.value
  else moSearch.value = workOrderInput.value === 'N/A' ? '' : workOrderInput.value
  lookup.value = target
}
function chooseEquipment(item: Equipment): void {
  equipmentInput.value = item.equipmentNo
  lookup.value = null
  queryEquipment()
}
function chooseWorkOrder(item: WorkOrder): void {
  workOrderInput.value = item.moNo
  lookup.value = null
  if (currentEquipmentNo.value) queryEquipment()
}
function setMode(next: LoadingMode): void {
  mode.value = next
  enterMaterialLot.value = false
  materialLotInput.value = 'N/A'
  materialInput.value = ''
  quantityInput.value = ''
  componentMaterialInput.value = ''
  componentLotInput.value = ''
  componentNoInput.value = ''
  semiLotInput.value = ''
  semiProductInput.value = ''
  semiQuantityInput.value = ''
  clearMessage()
}
function setEnterMaterialLot(value: boolean): void {
  enterMaterialLot.value = value
  materialLotInput.value = value ? '' : 'N/A'
}
function onEnterMaterialLotToggle(event: Event): void {
  setEnterMaterialLot((event.target as HTMLInputElement).checked)
}
function focusField(id: string): void {
  window.document.getElementById(id)?.focus()
}
function stockKey(materialNo: string, lotNo: string): string { return materialNo + '|' + lotNo }
function findMountedMaterial(materialNo: string, inputMaterialNo: string, lotNo: string, mono: string): MountedMaterial | undefined {
  return snapshot.value.rows.find(item => item.equipmentNo === currentEquipmentNo.value && item.materialNo === materialNo &&
    item.inputMaterialNo === inputMaterialNo && item.materialLotNo === lotNo && item.mono === mono)
}
function resolveOriginalMaterial(inputMaterialNo: string): string {
  if (!appointWorkOrder.value || !currentWorkOrderNo.value) return '*'
  return inputMaterialNo.startsWith('DEV-MTL-') ? inputMaterialNo : '*'
}
function mount(input: { inputMaterialNo: string; lotNo: string; qty: number; material?: Material; semi?: SemiLot; componentNo?: string }): void {
  clearMessage()
  if (!dev.value) { showError('DEV 假資料模式停用。'); return }
  if (!currentEquipmentNo.value) { showError('請先選擇設備並載入設備上料現況。'); return }
  if (appointWorkOrder.value && !currentWorkOrderNo.value) { showError('請先指定工單。'); return }
  if (!input.inputMaterialNo || !Number.isFinite(input.qty) || input.qty <= 0) { showError('物料編號及上料數量必須有效且大於 0。'); return }
  if (input.material?.checkLotNo && (!input.lotNo || input.lotNo === 'N/A')) { showError('此物料要求輸入物料批號。'); return }
  if (mode.value === 'semi' && (!input.lotNo || input.lotNo === 'N/A')) { showError('半成品上料必須輸入半成品批號。'); return }
  if (mode.value === 'component' && !input.componentNo?.trim()) { showError('元件上料必須輸入元件編號。'); return }
  const putInPlaceType: 2 | 3 | 4 = appointWorkOrder.value ? 3 : mode.value === 'semi' ? 4 : 2
  const materialNo = resolveOriginalMaterial(input.inputMaterialNo)
  const mono = appointWorkOrder.value ? currentWorkOrderNo.value : 'N/A'
  const lotNo = input.lotNo || 'N/A'
  if (input.semi) {
    const available = snapshot.value.semiStock[input.semi.lotNo] ?? 0
    if (input.qty > available) { showError('半成品庫可用數量不足，目前可用 ' + available + ' ' + (input.material?.unitNo ?? 'PCS') + '。'); return }
  } else {
    const key = stockKey(input.inputMaterialNo, lotNo)
    const available = snapshot.value.stock[key] ?? 0
    if (input.qty > available) { showError('投料點可用數量不足，目前可用 ' + available + '。'); return }
  }
  if (mode.value === 'component' && input.material && !input.material.requiresComponent) {
    showError('此物料不允許以元件方式上料。')
    return
  }
  const row = findMountedMaterial(materialNo, input.inputMaterialNo, lotNo, mono)
  if (input.componentNo && row?.components.some(item => item.componentNo === input.componentNo?.trim().toUpperCase())) {
    showError('此元件已掛載在目前設備物料上。')
    return
  }
  if (row) {
    row.inputQty += input.qty
    row.qty += input.qty
    if (input.componentNo) row.components.push({ id: 'wp301-c-' + Date.now(), componentNo: input.componentNo.trim().toUpperCase(), goodQty: 1, scrapQty: 0 })
  } else {
    const sameGroup = snapshot.value.rows.filter(item => item.equipmentNo === currentEquipmentNo.value && item.materialNo === materialNo)
    snapshot.value.rows.push({
      id: 'wp301-' + Date.now() + '-' + Math.random().toString(36).slice(2, 7),
      equipmentNo: currentEquipmentNo.value, materialNo, inputMaterialNo: input.inputMaterialNo,
      materialLotNo: lotNo, checkLotNo: input.material?.checkLotNo ?? !!input.semi,
      materialLevel: input.semi ? 1 : 0, putInPlaceType, mono, seq: sameGroup.length + 1,
      inputQty: input.qty, qty: input.qty, unitNo: input.material?.unitNo ?? 'PCS',
      components: input.componentNo ? [{ id: 'wp301-c-' + Date.now(), componentNo: input.componentNo.trim().toUpperCase(), goodQty: 1, scrapQty: 0 }] : [],
    })
  }
  if (input.semi) snapshot.value.semiStock[input.semi.lotNo] = (snapshot.value.semiStock[input.semi.lotNo] ?? 0) - input.qty
  else {
    const key = stockKey(input.inputMaterialNo, lotNo)
    snapshot.value.stock[key] = (snapshot.value.stock[key] ?? 0) - input.qty
  }
  persist()
  selection.value = null
  showMessage('DEV 已模擬設備上料；目前資料只保存在瀏覽器。')
}
function mountMaterial(): void {
  const materialNo = materialInput.value.trim().toUpperCase()
  const item = materials.find(value => value.materialNo === materialNo)
  if (!item) { showError('物料主檔查無此物料。'); return }
  const lotNo = enterMaterialLot.value ? materialLotInput.value.trim().toUpperCase() : 'N/A'
  mount({ inputMaterialNo: item.materialNo, lotNo, qty: Number(quantityInput.value), material: item })
  if (!actionError.value) {
    materialInput.value = materialNo
    materialLotInput.value = enterMaterialLot.value ? '' : 'N/A'
    quantityInput.value = ''
  }
}
function mountComponent(): void {
  const materialNo = componentMaterialInput.value.trim().toUpperCase()
  const item = materials.find(value => value.materialNo === materialNo)
  if (!item) { showError('物料主檔查無此物料。'); return }
  mount({ inputMaterialNo: item.materialNo, lotNo: componentLotInput.value.trim().toUpperCase(), qty: 1, material: item, componentNo: componentNoInput.value })
  if (!actionError.value) componentNoInput.value = ''
}
function loadSemiAndMount(): void {
  const lotNo = semiLotInput.value.trim().toUpperCase()
  const item = semiLots.find(value => value.lotNo === lotNo)
  if (!item) { showError('找不到半成品批號，或該批不在可用庫存。'); return }
  semiProductInput.value = item.productNo
  semiQuantityInput.value = String(snapshot.value.semiStock[item.lotNo] ?? 0)
  mount({ inputMaterialNo: item.productNo, lotNo: item.lotNo, qty: Number(semiQuantityInput.value), semi: item })
}
function selectGroup(groupKey: string): void { selection.value = { kind: 'group', groupKey } }
function selectMaterial(groupKey: string, row: MountedMaterial): void { selection.value = { kind: 'material', groupKey, stateId: row.id } }
function selectComponent(groupKey: string, row: MountedMaterial, component: MountedComponent): void {
  selection.value = { kind: 'component', groupKey, stateId: row.id, componentId: component.id }
}
function openUnmount(): void {
  clearMessage()
  if (!selection.value || selection.value.kind === 'group' || !selectedMaterial.value) {
    showError('請選取一筆設備上料明細或元件。')
    return
  }
  lookup.value = 'unmount'
}
function confirmUnmount(): void {
  const row = selectedMaterial.value
  if (!row) { lookup.value = null; return }
  if (selection.value?.kind === 'component' && selection.value.componentId) {
    if (row.materialLevel === 1) snapshot.value.semiStock[row.materialLotNo] = (snapshot.value.semiStock[row.materialLotNo] ?? 0) + 1
    else {
      const key = stockKey(row.inputMaterialNo, row.materialLotNo)
      snapshot.value.stock[key] = (snapshot.value.stock[key] ?? 0) + 1
    }
    row.components = row.components.filter(item => item.id !== selection.value?.componentId)
    row.inputQty = Math.max(0, row.inputQty - 1)
    row.qty = Math.max(0, row.qty - 1)
    if (row.qty === 0) snapshot.value.rows = snapshot.value.rows.filter(item => item.id !== row.id)
  } else {
    if (row.materialLevel === 1) snapshot.value.semiStock[row.materialLotNo] = (snapshot.value.semiStock[row.materialLotNo] ?? 0) + row.qty
    else {
      const key = stockKey(row.inputMaterialNo, row.materialLotNo)
      snapshot.value.stock[key] = (snapshot.value.stock[key] ?? 0) + row.qty
    }
    snapshot.value.rows = snapshot.value.rows.filter(item => item.id !== row.id)
  }
  persist()
  selection.value = null
  lookup.value = null
  showMessage('DEV 已模擬下料；庫存與設備現況已同步更新。')
}
function changeSequence(direction: -1 | 1): void {
  clearMessage()
  if (selection.value?.kind !== 'material' || !selectedMaterial.value) { showError('請選取設備上料明細，再調整序號。'); return }
  const row = selectedMaterial.value
  const siblings = snapshot.value.rows.filter(item => item.equipmentNo === row.equipmentNo && item.materialNo === row.materialNo &&
    item.checkLotNo === row.checkLotNo && item.materialLevel === row.materialLevel && item.putInPlaceType === row.putInPlaceType)
    .sort((a, b) => a.seq - b.seq)
  const index = siblings.findIndex(item => item.id === row.id)
  const adjacent = siblings[index + direction]
  if (!adjacent) { showError(direction < 0 ? '已是最前面的序號。' : '已是最後面的序號。'); return }
  const currentSeq = row.seq
  row.seq = adjacent.seq
  adjacent.seq = currentSeq
  persist()
  showMessage('DEV 已交換相鄰上料序號。')
}

const equipSearch = ref('')
const moSearch = ref('')
const lookupEquipmentRows = computed(() => allowedEquipments.value.filter(item => (item.equipmentNo + item.equipmentName).toUpperCase().includes(equipSearch.value.toUpperCase())))
const lookupMORows = computed(() => allowedWorkOrders.value.filter(item => (item.moNo + item.productNo).toUpperCase().includes(moSearch.value.toUpperCase())))
</script>

<template>
  <section class="eqp-loading-page">
    <header class="page-heading">
      <div><span class="function-code">WP301 · EQP MOUNT MATERIAL</span><h1>設備上料</h1></div>
      <div class="heading-tools"><span v-if="dev" class="dev-badge">DEV 假資料</span><button v-if="dev" class="quiet-button" type="button" @click="resetDev">還原假資料</button></div>
    </header>

    <section class="equipment-box" aria-label="設備條件">
      <label for="wp301-equipment">設備編號</label>
      <input id="wp301-equipment" v-model="equipmentInput" class="kd-input" autocomplete="off" placeholder="輸入設備編號" @keydown.enter.prevent="queryEquipment">
      <button v-if="!props.equipmentNo" class="lookup-button" type="button" title="選擇設備" @click="openLookup('equipment')">…</button>
      <span class="equipment-caption">{{ currentEquipment ? currentEquipment.equipmentName + '／' + currentEquipment.equipmentClass : '按 Enter 載入設備上料現況' }}</span>
    </section>

    <fieldset class="loading-entry">
      <legend>批量上料區</legend>
      <div class="order-row">
        <label class="check-label"><input v-model="appointWorkOrder" type="checkbox" @change="toggleWorkOrder">指定工單</label>
        <input v-model="workOrderInput" class="kd-input order-input" :disabled="!appointWorkOrder" :placeholder="appointWorkOrder ? '工單編號' : 'N/A'" @keydown.enter.prevent="currentEquipmentNo ? queryEquipment() : openLookup('mo')">
        <button class="lookup-button" type="button" :disabled="!appointWorkOrder" title="選擇工單" @click="openLookup('mo')">…</button>
        <span class="current-order">{{ appointWorkOrder && currentWorkOrderNo ? '目前工單：' + currentWorkOrderNo : '未指定工單' }}</span>
      </div>
      <div class="mode-row">
        <span class="field-label">上料類型</span>
        <label><input type="radio" name="wp301-mode" :checked="mode === 'material'" @change="setMode('material')">物料</label>
        <label><input type="radio" name="wp301-mode" :checked="mode === 'component'" @change="setMode('component')">物料(元件)</label>
        <label><input type="radio" name="wp301-mode" :checked="mode === 'semi'" @change="setMode('semi')">半成品</label>
        <span class="entry-hint">掃描／輸入後按 Enter 上料</span>
      </div>

      <div v-if="mode === 'material'" class="entry-fields">
        <label for="wp301-material">物料編號</label>
        <input id="wp301-material" v-model="materialInput" class="kd-input" autocomplete="off" placeholder="DEV-MTL-SOLDER-01" @keydown.enter.prevent="focusField(enterMaterialLot ? 'wp301-lot' : 'wp301-qty')">
        <label for="wp301-lot">物料批號</label>
        <input id="wp301-lot" v-model="materialLotInput" class="kd-input" :disabled="!enterMaterialLot" autocomplete="off" placeholder="N/A" @keydown.enter.prevent="focusField('wp301-qty')">
        <label for="wp301-qty">上料數量</label>
        <input id="wp301-qty" v-model="quantityInput" class="kd-input numeric-input" inputmode="decimal" placeholder="0" @keydown.enter.prevent="mountMaterial">
        <label class="check-label doc-lot"><input :checked="enterMaterialLot" type="checkbox" @change="onEnterMaterialLotToggle">輸入料批</label>
      </div>

      <div v-else-if="mode === 'component'" class="entry-fields component-fields">
        <label for="wp301-comp-material">物料編號</label>
        <input id="wp301-comp-material" v-model="componentMaterialInput" class="kd-input" autocomplete="off" placeholder="基板／晶圓物料" @keydown.enter.prevent="focusField('wp301-comp-lot')">
        <label for="wp301-comp-lot">物料批號</label>
        <input id="wp301-comp-lot" v-model="componentLotInput" class="kd-input" autocomplete="off" placeholder="物料批號" @keydown.enter.prevent="focusField('wp301-component-no')">
        <label for="wp301-component-no">元件編號</label>
        <input id="wp301-component-no" v-model="componentNoInput" class="kd-input" autocomplete="off" placeholder="元件編號" @keydown.enter.prevent="mountComponent">
      </div>

      <div v-else class="entry-fields semi-fields">
        <label for="wp301-semi-lot">半成品批號</label>
        <input id="wp301-semi-lot" v-model="semiLotInput" class="kd-input" autocomplete="off" placeholder="DEV-SEMI-LOT-001" @keydown.enter.prevent="loadSemiAndMount">
        <label for="wp301-semi-product">產品編號</label>
        <input id="wp301-semi-product" v-model="semiProductInput" class="read-only-input" readonly>
        <label for="wp301-semi-qty">半成品數量</label>
        <input id="wp301-semi-qty" v-model="semiQuantityInput" class="read-only-input numeric-input" readonly>
      </div>
    </fieldset>

    <div class="workspace">
      <section class="material-grid-panel" aria-label="設備上料現況">
        <div class="grid-title"><span>設備上料現況</span><span v-if="currentEquipmentNo">設備：{{ currentEquipmentNo }}</span></div>
        <div class="table-wrap">
          <table class="material-grid">
            <thead>
              <tr>
                <th>物料編號</th><th>批號檢查</th><th>物料層級</th><th>投料點</th><th>序號</th>
                <th>輸入物料編號</th><th>物料批號</th><th>數量</th><th>工單</th>
                <th>元件編號</th><th>良品數</th><th>報廢數</th>
              </tr>
            </thead>
            <tbody v-if="groups.length">
              <template v-for="group in groups" :key="group.key">
                <tr class="group-row" :class="{ selected: selection?.kind === 'group' && selection.groupKey === group.key }" @click="selectGroup(group.key)">
                  <td>{{ group.materialNo }}</td><td>{{ group.checkLotNo ? '是' : '否' }}</td>
                  <td>{{ group.materialLevel === 1 ? '半成品' : '物料' }}</td><td>{{ putInPlaceName(group.putInPlaceType) }}</td>
                  <td colspan="8">{{ group.rows.length }} 筆上料明細</td>
                </tr>
                <template v-for="row in group.rows" :key="row.id">
                  <tr class="detail-row" :class="{ selected: selection?.kind === 'material' && selection.stateId === row.id }" @click="selectMaterial(group.key, row)">
                    <td></td><td></td><td></td><td></td><td class="numeric">{{ row.seq }}</td>
                    <td>{{ row.inputMaterialNo }}</td><td>{{ row.materialLotNo }}</td><td class="numeric">{{ row.qty }} {{ row.unitNo }}</td>
                    <td>{{ row.mono }}</td><td></td><td></td><td></td>
                  </tr>
                  <tr v-for="component in row.components" :key="component.id" class="component-row" :class="{ selected: selection?.kind === 'component' && selection.componentId === component.id }" @click="selectComponent(group.key, row, component)">
                    <td colspan="9"><span class="component-indent">↳ 元件</span></td><td>{{ component.componentNo }}</td>
                    <td class="numeric">{{ component.goodQty }}</td><td class="numeric">{{ component.scrapQty }}</td>
                  </tr>
                </template>
              </template>
            </tbody>
            <tbody v-else>
              <tr><td colspan="12" class="empty-state">{{ currentEquipmentNo ? '此設備目前沒有上料資料。' : '先選擇設備，載入設備上料現況。' }}</td></tr>
            </tbody>
          </table>
        </div>
        <div class="grid-footer"><span>上料群組 {{ groups.length }} 筆</span><span v-if="selection && selection.kind !== 'group'">已選取：{{ unmountLabel }}</span></div>
      </section>
      <aside class="action-column" aria-label="操作">
        <button type="button" @click="changeSequence(-1)">▲ 上移</button>
        <button type="button" @click="changeSequence(1)">▼ 下移</button>
        <span class="action-spacer"></span>
        <button type="button" @click="openUnmount">下料</button>
        <button class="close-button" type="button" @click="emit('close')">關閉</button>
      </aside>
    </div>

    <p v-if="actionError" class="feedback error" role="alert">{{ actionError }}</p>
    <p v-else-if="actionMessage" class="feedback success" role="status">{{ actionMessage }}</p>

    <div v-if="lookup === 'equipment'" class="modal-backdrop" @click.self="lookup = null">
      <section class="lookup-dialog" role="dialog" aria-modal="true" aria-labelledby="wp301-equipment-lookup-title">
        <header><h2 id="wp301-equipment-lookup-title">選擇設備</h2><button type="button" @click="lookup = null">×</button></header>
        <div class="lookup-filter"><label for="wp301-eqp-filter">設備編號／名稱</label><input id="wp301-eqp-filter" v-model="equipSearch" class="kd-input"></div>
        <div class="lookup-table-wrap"><table class="lookup-table"><thead><tr><th>設備編號</th><th>設備名稱</th><th>設備分類</th><th>狀態</th><th>操作</th></tr></thead>
          <tbody><tr v-for="item in lookupEquipmentRows" :key="item.equipmentNo" @dblclick="chooseEquipment(item)"><td>{{ item.equipmentNo }}</td><td>{{ item.equipmentName }}</td><td>{{ item.equipmentClass }}</td><td>已核准</td><td><button type="button" @click="chooseEquipment(item)">選取</button></td></tr>
            <tr v-if="!lookupEquipmentRows.length"><td colspan="5" class="empty-state">沒有符合的已核准設備。</td></tr></tbody></table></div>
        <footer><button type="button" @click="lookup = null">關閉</button></footer>
      </section>
    </div>

    <div v-if="lookup === 'mo'" class="modal-backdrop" @click.self="lookup = null">
      <section class="lookup-dialog" role="dialog" aria-modal="true" aria-labelledby="wp301-mo-lookup-title">
        <header><h2 id="wp301-mo-lookup-title">選擇工單</h2><button type="button" @click="lookup = null">×</button></header>
        <div class="lookup-filter"><label for="wp301-mo-filter">工單／產品編號</label><input id="wp301-mo-filter" v-model="moSearch" class="kd-input"></div>
        <div class="lookup-table-wrap"><table class="lookup-table"><thead><tr><th>工單編號</th><th>產品編號</th><th>工單狀態</th><th>操作</th></tr></thead>
          <tbody><tr v-for="item in lookupMORows" :key="item.moNo" @dblclick="chooseWorkOrder(item)"><td>{{ item.moNo }}</td><td>{{ item.productNo }}</td><td>{{ item.state }}</td><td><button type="button" @click="chooseWorkOrder(item)">選取</button></td></tr>
            <tr v-if="!lookupMORows.length"><td colspan="4" class="empty-state">沒有符合狀態條件的工單。</td></tr></tbody></table></div>
        <footer><button type="button" @click="lookup = null">關閉</button></footer>
      </section>
    </div>

    <div v-if="lookup === 'unmount'" class="modal-backdrop" @click.self="lookup = null">
      <section class="confirm-dialog" role="dialog" aria-modal="true" aria-labelledby="wp301-unmount-title">
        <header><h2 id="wp301-unmount-title">確認下料</h2><button type="button" @click="lookup = null">×</button></header>
        <p>要從設備 {{ currentEquipmentNo }} 下料「{{ unmountLabel }}」嗎？</p>
        <footer><button type="button" @click="lookup = null">取消</button><button class="danger-button" type="button" @click="confirmUnmount">確認下料</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.eqp-loading-page { min-height: 0; height: 100%; display: flex; flex-direction: column; gap: 8px; padding: 12px; color: #292929; background: #fff; overflow: auto; }
.page-heading { display: flex; align-items: center; justify-content: space-between; min-height: 38px; }
.page-heading h1 { display: inline; margin: 0 0 0 10px; font-size: 20px; font-weight: 600; }
.function-code { color: #6d6d6d; font-size: 11px; letter-spacing: .04em; }
.heading-tools { display: flex; align-items: center; gap: 8px; }
.dev-badge { border: 1px solid #b48a29; border-radius: 3px; padding: 4px 8px; color: #76580e; background: #fff3c7; font-size: 12px; }
.equipment-box { min-height: 46px; display: flex; align-items: center; gap: 8px; border: 1px solid #b9b9b9; padding: 7px 12px; }
.equipment-box > label, .entry-fields > label, .lookup-filter > label { min-width: 94px; text-align: right; }
.kd-input, .read-only-input { height: 30px; border: 1px solid #929292; border-radius: 0; padding: 4px 7px; color: #292929; background: #fff0ad; font: inherit; }
.kd-input:focus { outline: 2px solid #c99a30; outline-offset: 0; }
.kd-input:disabled, .read-only-input { color: #777; background: #ededed; }
.equipment-box .kd-input { width: 245px; }
.lookup-button { width: 30px; height: 30px; border: 1px solid #aaa; border-radius: 3px; color: #444; background: #f8f8f8; font-size: 18px; line-height: 1; }
.lookup-button:disabled { color: #999; background: #eee; }
.equipment-caption, .current-order, .entry-hint { color: #686868; font-size: 12px; }
.loading-entry { min-width: 0; margin: 0; border: 1px solid #aaa; padding: 4px 12px 9px; }
.loading-entry legend { padding: 0 5px; color: #444; font-weight: 600; }
.order-row, .mode-row, .entry-fields { min-height: 34px; display: flex; align-items: center; gap: 9px; }
.check-label, .mode-row label { display: inline-flex; align-items: center; gap: 5px; white-space: nowrap; }
.order-row .order-input { width: 225px; }
.mode-row { border-top: 1px solid #eee; }
.mode-row .field-label { width: 94px; text-align: right; }
.mode-row input, .check-label input { accent-color: #c28b1b; }
.entry-hint { margin-left: auto; }
.entry-fields { border-top: 1px solid #eee; padding-top: 5px; }
.entry-fields > input { width: min(17vw, 190px); min-width: 110px; }
.entry-fields > label { min-width: 92px; }
.entry-fields .numeric-input { text-align: right; }
.entry-fields .doc-lot { margin-left: auto; }
.component-fields, .semi-fields { justify-content: flex-start; }
.workspace { min-height: 250px; flex: 1; display: flex; gap: 9px; }
.material-grid-panel { min-width: 0; flex: 1; display: flex; flex-direction: column; border: 1px solid #aaa; }
.grid-title { display: flex; justify-content: space-between; padding: 7px 9px; color: #444; background: #f3f3f3; border-bottom: 1px solid #aaa; font-weight: 600; }
.table-wrap, .lookup-table-wrap { min-height: 0; flex: 1; overflow: auto; }
.material-grid, .lookup-table { width: 100%; border-collapse: collapse; font-size: 12px; }
.material-grid { min-width: 1120px; }
.material-grid th, .lookup-table th { position: sticky; top: 0; z-index: 1; padding: 7px 6px; border: 1px solid #c5c5c5; color: #303030; background: #e8e8e8; text-align: left; white-space: nowrap; }
.material-grid td, .lookup-table td { padding: 6px; border: 1px solid #d1d1d1; background: #fff0ad; white-space: nowrap; }
.material-grid .group-row td { background: #f1f1f1; font-weight: 600; }
.material-grid .detail-row td, .material-grid .component-row td { background: #fff8dd; }
.material-grid tr.selected td { outline: 1px solid #a77a16; outline-offset: -1px; background: #ffe18a; }
.material-grid tbody tr { cursor: pointer; }
.material-grid .numeric { text-align: right; }
.component-indent { padding-left: 12px; color: #646464; }
.empty-state { height: 68px; color: #777; text-align: center !important; background: #fafafa !important; }
.grid-footer { display: flex; justify-content: space-between; padding: 6px 9px; color: #666; border-top: 1px solid #c5c5c5; font-size: 12px; }
.action-column { width: 88px; flex: 0 0 88px; display: flex; flex-direction: column; gap: 7px; padding-top: 1px; }
.action-column button, .quiet-button, .lookup-dialog button, .confirm-dialog button { min-height: 32px; border: 1px solid #aaa; border-radius: 3px; color: #333; background: #f8f8f8; font: inherit; }
.action-spacer { flex: 1; }
.close-button { margin-top: 2px; }
.quiet-button { min-height: 28px; padding: 3px 9px; font-size: 12px; }
.feedback { flex: 0 0 auto; margin: 0; padding: 7px 10px; border: 1px solid; font-size: 13px; }
.feedback.error { border-color: #d5a2a2; color: #812c2c; background: #fff0f0; }
.feedback.success { border-color: #b5c9a8; color: #3e6631; background: #f1faeb; }
.modal-backdrop { position: fixed; inset: 0; z-index: 1000; display: grid; place-items: center; padding: 18px; background: rgb(0 0 0 / 34%); }
.lookup-dialog, .confirm-dialog { width: min(760px, 95vw); max-height: 82vh; display: flex; flex-direction: column; border: 1px solid #888; background: #fff; box-shadow: 0 8px 26px rgb(0 0 0 / 22%); }
.confirm-dialog { width: min(480px, 95vw); }
.lookup-dialog header, .confirm-dialog header { display: flex; align-items: center; justify-content: space-between; padding: 7px 12px; border-bottom: 1px solid #ccc; background: #f5f5f5; }
.lookup-dialog h2, .confirm-dialog h2 { margin: 0; font-size: 16px; }
.lookup-dialog header button, .confirm-dialog header button { min-height: 26px; width: 30px; font-size: 20px; }
.lookup-filter { display: flex; align-items: center; gap: 8px; padding: 9px 12px; }
.lookup-filter .kd-input { flex: 1; }
.lookup-table { min-width: 540px; }
.lookup-table tr { cursor: pointer; }
.lookup-table td:last-child { width: 70px; }
.lookup-table td button { min-height: 26px; padding: 2px 8px; }
.lookup-dialog footer, .confirm-dialog footer { display: flex; justify-content: flex-end; gap: 8px; padding: 9px 12px; border-top: 1px solid #ddd; }
.confirm-dialog p { padding: 8px 14px; line-height: 1.6; }
.danger-button { border-color: #b64a42 !important; color: #fff !important; background: #b64a42 !important; }
@media (max-width: 980px) {
  .eqp-loading-page { height: auto; min-height: 100%; }
  .entry-fields { flex-wrap: wrap; }
  .entry-fields > label { min-width: auto; }
  .entry-fields > input { width: 150px; }
  .workspace { min-height: 360px; }
  .equipment-caption, .current-order { overflow: hidden; text-overflow: ellipsis; }
}
</style>
