<script setup lang="ts">
import { computed, ref } from 'vue'

const emit = defineEmits<{ close: [] }>()

type BasisMode = 'add' | 'edit' | 'copy'
type DetailMode = 'add' | 'edit' | 'copy'
type DialogMode = 'basis-add' | 'basis-edit' | 'basis-copy' | 'detail-add' | 'detail-edit' | 'detail-copy' | 'delete-basis' | 'delete-detail' | null
interface ReturnBasis { materialReturnNo: string; inventoryNo: string; vendorNo: string; state: number; description: string; creator: string; createDate: string; reviser: string; reviseDate: string; dataStamp: number }
interface ReturnComponent { componentId: string; componentNo: string; cassetteNo: string; slotNo: string; goodQty: number; scrapQty: number; unitNo: string }
interface ReturnDetail { key: string; materialReturnNo: string; materialNo: string; materialName: string; materialLotNo: string; locatorNo: string; unitNo: string; qty: number; creator: string; createDate: string; reviser: string; reviseDate: string; dataStamp: number; componentIds: string[] }
interface RawComponent extends ReturnComponent { inventoryNo: string; materialNo: string; materialLotNo: string }
interface RawLot { stockId: string; inventoryNo: string; vendorNo: string; materialNo: string; materialName: string; materialLotNo: string; locatorNo: string; unitNo: string; locQty: number }
interface DevState { bases: ReturnBasis[]; details: ReturnDetail[]; bookedComponentIds: string[]; nextSerial: number }
interface BasisDraft { materialReturnNo: string; inventoryNo: string; vendorNo: string; description: string }
interface DetailDraft { materialNo: string; stockId: string; qty: string }

const storageKey = 'ymes.dev.iv206-material-return.v1'
const inventories = [{ inventoryNo: 'RAW-A', name: '原料庫 A' }, { inventoryNo: 'RAW-B', name: '原料庫 B' }]
const vendors = [{ vendorNo: 'V-DEV-01', name: 'DEV 供應商甲' }, { vendorNo: 'V-DEV-02', name: 'DEV 供應商乙' }]
const rawLots: RawLot[] = [
  { stockId: 'raw-a-sensor', inventoryNo: 'RAW-A', vendorNo: 'V-DEV-01', materialNo: 'RM-SENSOR-05', materialName: '感測器模組 S', materialLotNo: 'SNS-2611-005', locatorNo: 'A-03-01', unitNo: 'EA', locQty: 4 },
  { stockId: 'raw-a-resin', inventoryNo: 'RAW-A', vendorNo: 'V-DEV-01', materialNo: 'RM-RESIN-01', materialName: '環氧樹脂 A', materialLotNo: 'RES-2611-001', locatorNo: 'A-01-01', unitNo: 'KG', locQty: 26.5 },
  { stockId: 'raw-b-copper', inventoryNo: 'RAW-B', vendorNo: 'V-DEV-02', materialNo: 'RM-COPPER-03', materialName: '銅箔材料 C', materialLotNo: 'CU-2611-020', locatorNo: 'B-01-02', unitNo: 'RL', locQty: 32 },
]
const rawComponents: RawComponent[] = [
  { componentId: 'sns-001', inventoryNo: 'RAW-A', materialNo: 'RM-SENSOR-05', materialLotNo: 'SNS-2611-005', componentNo: 'COMP-SNS-A', cassetteNo: 'CAS-01', slotNo: '01', goodQty: 1, scrapQty: 0, unitNo: 'EA' },
  { componentId: 'sns-002', inventoryNo: 'RAW-A', materialNo: 'RM-SENSOR-05', materialLotNo: 'SNS-2611-005', componentNo: 'COMP-SNS-B', cassetteNo: 'CAS-01', slotNo: '02', goodQty: 1, scrapQty: 0, unitNo: 'EA' },
  { componentId: 'sns-003', inventoryNo: 'RAW-A', materialNo: 'RM-SENSOR-05', materialLotNo: 'SNS-2611-005', componentNo: 'COMP-SNS-C', cassetteNo: 'CAS-02', slotNo: '01', goodQty: 1, scrapQty: 0, unitNo: 'EA' },
  { componentId: 'sns-004', inventoryNo: 'RAW-A', materialNo: 'RM-SENSOR-05', materialLotNo: 'SNS-2611-005', componentNo: 'COMP-SNS-D', cassetteNo: 'CAS-02', slotNo: '02', goodQty: 1, scrapQty: 0, unitNo: 'EA' },
]

function initialState(): DevState {
  return {
    bases: [
      { materialReturnNo: 'MR-DEV-26001', inventoryNo: 'RAW-A', vendorNo: 'V-DEV-01', state: 0, description: 'DEV 假資料：待確認退料草稿。', creator: 'DEV001', createDate: '2026-11-20 10:12:00', reviser: '', reviseDate: '', dataStamp: 0 },
      { materialReturnNo: 'MR-DEV-26002', inventoryNo: 'RAW-B', vendorNo: 'V-DEV-02', state: 0, description: 'DEV 假資料：供應商退料。', creator: 'DEV002', createDate: '2026-11-22 13:40:00', reviser: '', reviseDate: '', dataStamp: 0 },
      { materialReturnNo: 'MR-DEV-25998', inventoryNo: 'RAW-A', vendorNo: 'V-DEV-01', state: 2, description: '已確認歷史假資料，主畫面固定不載入。', creator: 'DEV003', createDate: '2026-11-18 11:20:00', reviser: '', reviseDate: '', dataStamp: 1 },
    ],
    details: [
      { key: 'mr-dev-26001-sensor', materialReturnNo: 'MR-DEV-26001', materialNo: 'RM-SENSOR-05', materialName: '感測器模組 S', materialLotNo: 'SNS-2611-005', locatorNo: 'A-03-01', unitNo: 'EA', qty: 2, creator: 'DEV001', createDate: '2026-11-20 10:20:00', reviser: '', reviseDate: '', dataStamp: 0, componentIds: ['sns-001', 'sns-002'] },
      { key: 'mr-dev-26001-resin', materialReturnNo: 'MR-DEV-26001', materialNo: 'RM-RESIN-01', materialName: '環氧樹脂 A', materialLotNo: 'RES-2611-001', locatorNo: 'A-01-01', unitNo: 'KG', qty: 1.25, creator: 'DEV001', createDate: '2026-11-20 10:25:00', reviser: '', reviseDate: '', dataStamp: 0, componentIds: [] },
    ],
    bookedComponentIds: ['sns-001', 'sns-002', 'sns-004'],
    nextSerial: 26003,
  }
}
function loadState(): DevState {
  try {
    const saved = localStorage.getItem(storageKey)
    if (saved) return JSON.parse(saved) as DevState
  } catch { /* use fixtures if browser storage is unavailable */ }
  return initialState()
}
function inventoryName(inventoryNo: string): string { return inventories.find((row) => row.inventoryNo === inventoryNo)?.name ?? inventoryNo }
function vendorName(vendorNo: string): string { return vendors.find((row) => row.vendorNo === vendorNo)?.name ?? vendorNo }
function formatQty(qty: number): string { return new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 6 }).format(qty) }
function nowText(): string { return new Date().toLocaleString('sv-SE', { hour12: false }).replace('T', ' ') }

const records = ref<DevState>(loadState())
const searchText = ref('')
const appliedSearch = ref('')
const selectedBasisNo = ref('MR-DEV-26001')
const selectedBasisNos = ref<string[]>([])
const selectedDetailKeys = ref<string[]>([])
const expandedDetailKeys = ref<string[]>([])
const dialogMode = ref<DialogMode>(null)
const notice = ref('DEV 假資料：退料單草稿與元件 BookingFlag 僅保存在本機，不執行正式 API 或庫存扣帳。')
const basisDraft = ref<BasisDraft>({ materialReturnNo: '', inventoryNo: '', vendorNo: '', description: '' })
const detailDraft = ref<DetailDraft>({ materialNo: '', stockId: '', qty: '' })
const componentSelection = ref<string[]>([])
const pendingDeleteBasisNos = ref<string[]>([])
const pendingDeleteDetailKeys = ref<string[]>([])
const editingDetailKey = ref('')

const visibleBases = computed(() => records.value.bases
  .filter((row) => row.state === 0 && (!appliedSearch.value || row.materialReturnNo.toLowerCase().includes(appliedSearch.value.toLowerCase())))
  .slice().sort((a, b) => a.materialReturnNo.localeCompare(b.materialReturnNo)))
const activeBasis = computed(() => visibleBases.value.find((row) => row.materialReturnNo === selectedBasisNo.value) ?? null)
const visibleDetails = computed(() => activeBasis.value
  ? records.value.details.filter((row) => row.materialReturnNo === activeBasis.value?.materialReturnNo)
    .slice().sort((a, b) => [a.materialNo, a.materialLotNo, a.locatorNo].join('|').localeCompare([b.materialNo, b.materialLotNo, b.locatorNo].join('|')))
  : [])
const activeDetail = computed(() => visibleDetails.value.find((row) => row.key === selectedDetailKeys.value[0]) ?? visibleDetails.value[0] ?? null)
const currentDetail = computed(() => records.value.details.find((row) => row.key === editingDetailKey.value) ?? null)
const materialOptions = computed(() => rawLots.filter((row) => row.inventoryNo === activeBasis.value?.inventoryNo && row.vendorNo === activeBasis.value?.vendorNo)
  .filter((row, index, all) => all.findIndex((item) => item.materialNo === row.materialNo) === index))
const lotOptions = computed(() => rawLots.filter((row) => row.inventoryNo === activeBasis.value?.inventoryNo && row.vendorNo === activeBasis.value?.vendorNo && row.materialNo === detailDraft.value.materialNo))
const selectedStock = computed(() => rawLots.find((row) => row.stockId === detailDraft.value.stockId) ?? null)
const availableComponents = computed(() => {
  if (!selectedStock.value) return []
  const priorIds = new Set(currentDetail.value?.componentIds ?? [])
  const booked = new Set(records.value.bookedComponentIds)
  return rawComponents.filter((row) => row.inventoryNo === selectedStock.value?.inventoryNo && row.materialNo === selectedStock.value?.materialNo && row.materialLotNo === selectedStock.value?.materialLotNo)
    .filter((row) => !booked.has(row.componentId) || priorIds.has(row.componentId))
})
const detailHasComponentData = computed(() => selectedStock.value ? rawComponents.some((row) => row.inventoryNo === selectedStock.value?.inventoryNo && row.materialNo === selectedStock.value?.materialNo && row.materialLotNo === selectedStock.value?.materialLotNo) : false)
const basisDialogTitle = computed(() => dialogMode.value === 'basis-edit' ? 'Edit MaterialReturn Basis' : dialogMode.value === 'basis-copy' ? 'Copy MaterialReturn Basis' : 'Add MaterialReturn Basis')
const detailDialogTitle = computed(() => dialogMode.value === 'detail-edit' ? 'Edit MaterialReturn Detail' : dialogMode.value === 'detail-copy' ? 'Copy MaterialReturn Detail' : 'Add MaterialReturn Detail')

function persist(message: string): void {
  try { localStorage.setItem(storageKey, JSON.stringify(records.value)) } catch { /* UI remains usable for this browser session */ }
  notice.value = message
}
function nextReturnNo(): string {
  const serial = records.value.nextSerial
  records.value.nextSerial += 1
  return `MR-DEV-${serial}`
}
function runQuery(): void {
  appliedSearch.value = searchText.value.trim()
  const first = visibleBases.value[0]
  selectedBasisNo.value = first?.materialReturnNo ?? ''
  selectedBasisNos.value = []
  selectedDetailKeys.value = []
}
function selectBasis(row: ReturnBasis): void {
  selectedBasisNo.value = row.materialReturnNo
  selectedDetailKeys.value = []
}
function toggleBasisSelection(no: string): void {
  selectedBasisNos.value = selectedBasisNos.value.includes(no) ? selectedBasisNos.value.filter((item) => item !== no) : [...selectedBasisNos.value, no]
}
function activateDetail(row: ReturnDetail): void { selectedDetailKeys.value = [row.key] }
function toggleDetailSelection(key: string): void {
  selectedDetailKeys.value = selectedDetailKeys.value.includes(key) ? selectedDetailKeys.value.filter((item) => item !== key) : [...selectedDetailKeys.value, key]
}
function toggleExpandedDetail(key: string): void {
  expandedDetailKeys.value = expandedDetailKeys.value.includes(key) ? expandedDetailKeys.value.filter((item) => item !== key) : [...expandedDetailKeys.value, key]
}
function componentRows(detail: ReturnDetail): RawComponent[] { return rawComponents.filter((row) => detail.componentIds.includes(row.componentId)) }

function openBasisDialog(mode: BasisMode): void {
  if (mode !== 'add' && !activeBasis.value) return
  const current = activeBasis.value
  basisDraft.value = {
    materialReturnNo: mode === 'edit' ? current?.materialReturnNo ?? '' : nextReturnNo(),
    inventoryNo: mode === 'add' ? '' : current?.inventoryNo ?? '',
    vendorNo: mode === 'add' ? '' : current?.vendorNo ?? '',
    description: mode === 'add' ? '' : current?.description ?? '',
  }
  dialogMode.value = mode === 'add' ? 'basis-add' : mode === 'edit' ? 'basis-edit' : 'basis-copy'
}
function changeBasisInventory(): void { basisDraft.value.vendorNo = '' }
function saveBasis(keepOpen = false): void {
  if (!basisDraft.value.materialReturnNo.trim() || !basisDraft.value.inventoryNo || !basisDraft.value.vendorNo) { notice.value = 'MaterialReturnNo、InventoryNo、VendorNo 為必填欄位。'; return }
  if (dialogMode.value === 'basis-edit') {
    records.value.bases = records.value.bases.map((row) => row.materialReturnNo === basisDraft.value.materialReturnNo
      ? { ...row, description: basisDraft.value.description, reviser: 'DEV001', reviseDate: nowText(), dataStamp: row.dataStamp + 1 }
      : row)
    selectedBasisNo.value = basisDraft.value.materialReturnNo
    persist('已更新 DEV 單頭說明；退料單號、庫房及供應商維持不變。')
  } else {
    const row: ReturnBasis = { ...basisDraft.value, state: 0, creator: 'DEV001', createDate: nowText(), reviser: '', reviseDate: '', dataStamp: 0 }
    records.value.bases = [...records.value.bases, row]
    selectedBasisNo.value = row.materialReturnNo
    appliedSearch.value = ''
    searchText.value = ''
    persist(dialogMode.value === 'basis-copy' ? '已複製 DEV 單頭；來源明細不會跟著複製。' : '已新增 DEV 未確認退料單頭。')
  }
  selectedBasisNos.value = []
  selectedDetailKeys.value = []
  if (!keepOpen) dialogMode.value = null
}
function requestDeleteBasis(): void {
  const selected = selectedBasisNos.value.length ? selectedBasisNos.value : activeBasis.value ? [activeBasis.value.materialReturnNo] : []
  pendingDeleteBasisNos.value = selected.filter((no) => records.value.bases.some((row) => row.materialReturnNo === no && row.state === 0))
  if (pendingDeleteBasisNos.value.length) dialogMode.value = 'delete-basis'
}
function confirmDeleteBasis(): void {
  const noSet = new Set(pendingDeleteBasisNos.value)
  const removed = records.value.details.filter((row) => noSet.has(row.materialReturnNo)).flatMap((row) => row.componentIds)
  records.value.bases = records.value.bases.filter((row) => !noSet.has(row.materialReturnNo))
  records.value.details = records.value.details.filter((row) => !noSet.has(row.materialReturnNo))
  records.value.bookedComponentIds = records.value.bookedComponentIds.filter((id) => !removed.includes(id))
  selectedBasisNos.value = []
  selectedBasisNo.value = visibleBases.value[0]?.materialReturnNo ?? ''
  selectedDetailKeys.value = []
  persist(`已在 DEV 假資料刪除 ${noSet.size} 張未確認退料單及其明細，並釋放模擬元件預約。`)
  dialogMode.value = null
}

function onMaterialChanged(): void { detailDraft.value.stockId = ''; detailDraft.value.qty = ''; componentSelection.value = [] }
function onStockChanged(): void {
  const stock = selectedStock.value
  componentSelection.value = []
  detailDraft.value.qty = stock ? (rawComponents.some((row) => row.inventoryNo === stock.inventoryNo && row.materialNo === stock.materialNo && row.materialLotNo === stock.materialLotNo) ? '0' : String(stock.locQty)) : ''
}
function openDetailDialog(mode: DetailMode): void {
  const basis = activeBasis.value
  if (!basis || basis.state !== 0) return
  const row = mode === 'add' ? null : activeDetail.value
  if (mode !== 'add' && !row) return
  editingDetailKey.value = mode === 'edit' ? row?.key ?? '' : ''
  detailDraft.value = {
    materialNo: row?.materialNo ?? '',
    stockId: row ? rawLots.find((lot) => lot.inventoryNo === basis.inventoryNo && lot.materialNo === row.materialNo && lot.materialLotNo === row.materialLotNo && lot.locatorNo === row.locatorNo)?.stockId ?? '' : '',
    qty: row ? (mode === 'copy' && row.componentIds.length ? '0' : String(row.qty)) : '',
  }
  componentSelection.value = mode === 'edit' ? [...(row?.componentIds ?? [])] : []
  dialogMode.value = mode === 'add' ? 'detail-add' : mode === 'edit' ? 'detail-edit' : 'detail-copy'
}
function toggleComponent(id: string): void {
  componentSelection.value = componentSelection.value.includes(id) ? componentSelection.value.filter((item) => item !== id) : [...componentSelection.value, id]
  detailDraft.value.qty = String(componentSelection.value.length)
}
function selectAllComponents(): void { componentSelection.value = availableComponents.value.map((row) => row.componentId); detailDraft.value.qty = String(componentSelection.value.length) }
function clearComponents(): void { componentSelection.value = []; detailDraft.value.qty = '0' }
function saveDetail(keepOpen = false): void {
  const basis = activeBasis.value
  const stock = selectedStock.value
  const qty = Number(detailDraft.value.qty)
  if (!basis || !stock || !detailDraft.value.materialNo) { notice.value = '請選擇供應商物料及同庫房批號／儲位。'; return }
  if (!Number.isFinite(qty) || qty <= 0) { notice.value = 'Qty 必須大於 0。'; return }
  if (detailHasComponentData.value && (componentSelection.value.length === 0 || !Number.isInteger(qty) || qty !== componentSelection.value.length)) { notice.value = '此批號含元件；至少選一個可退元件，且 Qty 必須等於已選元件數。'; return }
  const old = currentDetail.value
  const now = nowText()
  let row: ReturnDetail
  if (dialogMode.value === 'detail-edit' && old) {
    row = { ...old, qty, componentIds: [...componentSelection.value], reviser: 'DEV001', reviseDate: now, dataStamp: old.dataStamp + 1 }
    records.value.details = records.value.details.map((item) => item.key === old.key ? row : item)
  } else {
    row = { key: `dev-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`, materialReturnNo: basis.materialReturnNo, materialNo: stock.materialNo, materialName: stock.materialName, materialLotNo: stock.materialLotNo, locatorNo: stock.locatorNo, unitNo: stock.unitNo, qty, creator: 'DEV001', createDate: now, reviser: '', reviseDate: '', dataStamp: 0, componentIds: [...componentSelection.value] }
    records.value.details = [...records.value.details, row]
  }
  const previous = old?.componentIds ?? []
  const released = records.value.bookedComponentIds.filter((id) => !previous.includes(id))
  records.value.bookedComponentIds = [...new Set([...released, ...componentSelection.value])]
  selectedDetailKeys.value = [row.key]
  expandedDetailKeys.value = row.componentIds.length ? [...new Set([...expandedDetailKeys.value, row.key])] : expandedDetailKeys.value
  persist(dialogMode.value === 'detail-edit' ? '已更新 DEV 明細與本機元件預約。' : '已新增 DEV 退料明細與本機元件預約。')
  if (!keepOpen) dialogMode.value = null
}
function requestDeleteDetail(): void {
  const selected = selectedDetailKeys.value.length ? selectedDetailKeys.value : activeDetail.value ? [activeDetail.value.key] : []
  pendingDeleteDetailKeys.value = selected.filter((key) => records.value.details.some((row) => row.key === key && row.materialReturnNo === activeBasis.value?.materialReturnNo))
  if (pendingDeleteDetailKeys.value.length) dialogMode.value = 'delete-detail'
}
function confirmDeleteDetail(): void {
  const removed = records.value.details.filter((row) => pendingDeleteDetailKeys.value.includes(row.key)).flatMap((row) => row.componentIds)
  records.value.details = records.value.details.filter((row) => !pendingDeleteDetailKeys.value.includes(row.key))
  records.value.bookedComponentIds = records.value.bookedComponentIds.filter((id) => !removed.includes(id))
  selectedDetailKeys.value = []
  persist(`已在 DEV 假資料刪除 ${pendingDeleteDetailKeys.value.length} 筆明細並釋放模擬元件預約。`)
  dialogMode.value = null
}
function closeDialog(): void { dialogMode.value = null }

if (visibleBases.value.length) selectedBasisNo.value = visibleBases.value[0].materialReturnNo
</script>

<template>
  <main class="material-return-page" aria-label="開立退料單 DEV">
    <header class="page-heading">
      <div><p>INVENTORY · IV206</p><h1>Material Return <span>開立退料單</span></h1><small>維護未確認退料單；實際庫存扣帳由 IV201 退料確認處理</small></div>
      <button type="button" class="close-button" @click="emit('close')">Close (X)</button>
    </header>
    <p class="notice" role="status">{{ notice }}</p>

    <section class="query-panel" aria-label="退料單篩選">
      <label>MaterialReturnNo
        <span class="query-control"><input v-model="searchText" autocomplete="off" placeholder="搜尋未確認退料單" @keyup.enter="runQuery" /><button type="button" class="primary" @click="runQuery">Query</button></span>
      </label>
      <span class="query-hint">原 Form 只提供退料單號查詢，主檔固定篩選 State=0。</span>
    </section>

    <section class="panel basis-panel" aria-label="退料單主檔">
      <header class="panel-heading"><div><h2>MaterialReturn Basis</h2><span>{{ visibleBases.length }} 張未確認單</span></div>
        <div class="action-row"><button type="button" class="primary" @click="openBasisDialog('add')">ADD</button><button type="button" :disabled="!activeBasis" @click="openBasisDialog('edit')">EDIT</button><button type="button" :disabled="!activeBasis" @click="requestDeleteBasis">DEL</button><button type="button" :disabled="!activeBasis" @click="openBasisDialog('copy')">COPY</button></div>
      </header>
      <div class="table-wrap basis-table-wrap"><table><thead><tr><th class="select-col">選</th><th>MaterialReturnNo</th><th>InventoryNo</th><th>VendorNo</th><th>State</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
        <tbody><tr v-for="row in visibleBases" :key="row.materialReturnNo" :class="{ active: activeBasis?.materialReturnNo === row.materialReturnNo }" @click="selectBasis(row)">
          <td class="select-col"><input type="checkbox" :checked="selectedBasisNos.includes(row.materialReturnNo)" :aria-label="`選取 ${row.materialReturnNo}`" @click.stop @change="toggleBasisSelection(row.materialReturnNo)" /></td><td class="mono">{{ row.materialReturnNo }}</td><td>{{ row.inventoryNo }}</td><td>{{ row.vendorNo }}</td><td><span class="state-badge">UnConfirm</span></td><td class="description">{{ row.description || '—' }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser || '—' }}</td><td>{{ row.reviseDate || '—' }}</td>
        </tr><tr v-if="!visibleBases.length"><td colspan="10" class="empty">沒有符合單號條件的 State=0 退料單。</td></tr></tbody>
      </table></div>
      <footer class="grid-footer"><span>{{ activeBasis ? `${activeBasis.materialReturnNo} · ${inventoryName(activeBasis.inventoryNo)} · ${vendorName(activeBasis.vendorNo)}` : '尚未選取未確認單頭' }}</span><span>State=0 · localStorage DEV fixtures</span></footer>
    </section>

    <section class="panel detail-panel" aria-label="退料單明細">
      <header class="panel-heading"><div><h2>MaterialReturn Detail</h2><span>{{ activeBasis?.materialReturnNo ?? '請先選取單頭' }} · {{ visibleDetails.length }} 筆明細</span></div>
        <div class="action-row"><button type="button" class="primary" :disabled="!activeBasis" @click="openDetailDialog('add')">ADD</button><button type="button" :disabled="!activeDetail" @click="openDetailDialog('edit')">EDIT</button><button type="button" :disabled="!activeDetail" @click="requestDeleteDetail">DEL</button><button type="button" :disabled="!activeDetail" @click="openDetailDialog('copy')">COPY</button></div>
      </header>
      <div class="table-wrap detail-table-wrap"><table><thead><tr><th class="select-col">選</th><th>MaterialNo</th><th>MaterialLotNo</th><th>LocatorNo</th><th>UnitNo</th><th class="numeric">Qty</th><th>元件</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead><tbody>
        <template v-for="row in visibleDetails" :key="row.key"><tr :class="{ active: activeDetail?.key === row.key }" @click="activateDetail(row)">
          <td class="select-col"><input type="checkbox" :checked="selectedDetailKeys.includes(row.key)" :aria-label="`選取 ${row.materialNo} ${row.materialLotNo}`" @click.stop @change="toggleDetailSelection(row.key)" /></td><td class="mono">{{ row.materialNo }}<small>{{ row.materialName }}</small></td><td class="mono">{{ row.materialLotNo }}</td><td>{{ row.locatorNo }}</td><td>{{ row.unitNo }}</td><td class="numeric">{{ formatQty(row.qty) }}</td><td><button v-if="row.componentIds.length" type="button" class="text-button" @click.stop="toggleExpandedDetail(row.key)">{{ expandedDetailKeys.includes(row.key) ? '收合' : `查看 ${row.componentIds.length} 個` }}</button><span v-else>—</span></td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser || '—' }}</td><td>{{ row.reviseDate || '—' }}</td>
        </tr><tr v-if="row.componentIds.length && expandedDetailKeys.includes(row.key)" class="component-row"><td colspan="11"><strong>MaterialReturn Component</strong><span v-for="item in componentRows(row)" :key="item.componentId">{{ item.componentNo }} · Cassette {{ item.cassetteNo }} · Slot {{ item.slotNo }} · Good {{ item.goodQty }} / Scrap {{ item.scrapQty }}</span></td></tr></template>
        <tr v-if="activeBasis && !visibleDetails.length"><td colspan="11" class="empty">此 DEV 退料單尚無明細。</td></tr><tr v-else-if="!activeBasis"><td colspan="11" class="empty">選取未確認單頭後載入該單明細。</td></tr>
      </tbody></table></div>
      <footer class="grid-footer"><span>{{ visibleDetails.length }} 筆明細 · 依 MaterialNo／Lot／Locator 排序</span><span>只有 State=0 可維護</span></footer>
    </section>

    <div v-if="dialogMode === 'basis-add' || dialogMode === 'basis-edit' || dialogMode === 'basis-copy'" class="modal-backdrop" @click.self="closeDialog">
      <section class="dialog" role="dialog" aria-modal="true" aria-label="MaterialReturn Basis"><header class="dialog-heading"><div><p>IV206 · MATERIAL RETURN</p><h2>{{ basisDialogTitle }}</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="closeDialog">×</button></header>
        <div class="form-grid">
          <label>MaterialReturnNo <span class="serial-control"><input v-model="basisDraft.materialReturnNo" readonly /><button v-if="dialogMode !== 'basis-edit'" type="button" @click="basisDraft.materialReturnNo = nextReturnNo()">取得 DEV 假單號</button></span><small>DEV 模擬 `MATERIALRETURNNO` Serial Rule。</small></label>
          <label>InventoryNo <select v-model="basisDraft.inventoryNo" :disabled="dialogMode === 'basis-edit'" @change="changeBasisInventory"><option value="">請選庫房</option><option v-for="row in inventories" :key="row.inventoryNo" :value="row.inventoryNo">{{ row.inventoryNo }} · {{ row.name }}</option></select></label>
          <label>VendorNo <select v-model="basisDraft.vendorNo" :disabled="dialogMode === 'basis-edit'"><option value="">請選供應商</option><option v-for="row in vendors" :key="row.vendorNo" :value="row.vendorNo">{{ row.vendorNo }} · {{ row.name }}</option></select><small>Form 以供應商 lookup 建立退料對象。</small></label>
          <label class="span-two">Description <textarea v-model="basisDraft.description" rows="3" /></label>
        </div>
        <footer class="dialog-actions"><button type="button" @click="closeDialog">Close (X)</button><button type="button" @click="saveBasis(true)">Apply</button><button type="button" class="primary" @click="saveBasis()">Confirm</button></footer>
      </section>
    </div>

    <div v-if="dialogMode === 'detail-add' || dialogMode === 'detail-edit' || dialogMode === 'detail-copy'" class="modal-backdrop" @click.self="closeDialog">
      <section class="dialog detail-dialog" role="dialog" aria-modal="true" aria-label="MaterialReturn Detail"><header class="dialog-heading"><div><p>IV206 · MATERIAL RETURN DETAIL</p><h2>{{ detailDialogTitle }}</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="closeDialog">×</button></header>
        <p class="dialog-context">{{ activeBasis?.materialReturnNo }} · {{ activeBasis?.inventoryNo }} {{ inventoryName(activeBasis?.inventoryNo ?? '') }} · {{ activeBasis?.vendorNo }} {{ vendorName(activeBasis?.vendorNo ?? '') }}</p>
        <div class="form-grid">
          <label>MaterialNo <select v-model="detailDraft.materialNo" :disabled="dialogMode === 'detail-edit'" @change="onMaterialChanged"><option value="">選擇此供應商的物料</option><option v-for="row in materialOptions" :key="row.materialNo" :value="row.materialNo">{{ row.materialNo }} · {{ row.materialName }}</option></select></label>
          <label>MaterialLotNo / LocatorNo <select v-model="detailDraft.stockId" :disabled="dialogMode === 'detail-edit'" @change="onStockChanged"><option value="">選此庫房批號／儲位</option><option v-for="row in lotOptions" :key="row.stockId" :value="row.stockId">{{ row.materialLotNo }} · {{ row.locatorNo }} · LocQty {{ formatQty(row.locQty) }}</option></select></label>
          <label>UnitNo <input :value="selectedStock?.unitNo ?? currentDetail?.unitNo ?? ''" readonly /></label>
          <label>Qty <input v-model="detailDraft.qty" type="number" min="0" step="0.000001" /></label>
        </div>
        <div v-if="selectedStock" class="stock-card"><strong>{{ selectedStock.materialNo }} · {{ selectedStock.materialName }}</strong><span>Lot {{ selectedStock.materialLotNo }} · Locator {{ selectedStock.locatorNo }} · LocQty {{ formatQty(selectedStock.locQty) }} {{ selectedStock.unitNo }}</span><small>Draft 可輸入大於 LocQty 的數量；IV201 確認時必須重新驗庫存。本頁不扣帳。</small></div>
        <section v-if="selectedStock && detailHasComponentData" class="component-picker"><header><div><h3>Material Return Component</h3><span>{{ componentSelection.length }} 個已選；Qty 需等於所選元件數</span></div><div class="action-row"><button type="button" @click="selectAllComponents">全選</button><button type="button" @click="clearComponents">全不選</button></div></header>
          <div class="component-list"><label v-for="row in availableComponents" :key="row.componentId" class="component-option"><input type="checkbox" :checked="componentSelection.includes(row.componentId)" @change="toggleComponent(row.componentId)" /><span>{{ row.componentNo }}</span><small>Cassette {{ row.cassetteNo }} · Slot {{ row.slotNo }} · Good {{ row.goodQty }} / Scrap {{ row.scrapQty }} {{ row.unitNo }}</small></label><p v-if="!availableComponents.length" class="empty">此批號目前沒有可預約的 DEV 元件。</p></div>
        </section>
        <p class="rule-note">新增明細不會扣減 LocQty。含元件的批號需選元件並令 Qty 與數量相符；單據確認與庫存扣帳在 IV201 Source=3。</p>
        <footer class="dialog-actions"><button type="button" @click="closeDialog">Close (X)</button><button type="button" @click="saveDetail(true)">Apply</button><button type="button" class="primary" @click="saveDetail()">Confirm</button></footer>
      </section>
    </div>

    <div v-if="dialogMode === 'delete-basis' || dialogMode === 'delete-detail'" class="modal-backdrop" @click.self="closeDialog"><section class="dialog confirm-dialog" role="dialog" aria-modal="true" aria-label="確認刪除">
      <header class="dialog-heading"><div><p>IV206 · DEV 假資料</p><h2>確認刪除</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="closeDialog">×</button></header>
      <p v-if="dialogMode === 'delete-basis'">將刪除 {{ pendingDeleteBasisNos.length }} 張 State=0 單頭、其明細與元件假資料。</p><p v-else>將刪除 {{ pendingDeleteDetailKeys.length }} 筆選取明細及其 DEV 元件預約。</p>
      <footer class="dialog-actions"><button type="button" @click="closeDialog">Cancel</button><button type="button" class="danger" @click="dialogMode === 'delete-basis' ? confirmDeleteBasis() : confirmDeleteDetail()">Delete</button></footer>
    </section></div>
  </main>
</template>

<style scoped>
.material-return-page { display:flex; flex-direction:column; gap:10px; min-height:0; height:100%; overflow:auto; padding:12px; box-sizing:border-box; background:#f7f5ee; color:#26251f; font:inherit; }
.page-heading,.panel-heading,.dialog-heading,.component-picker header { display:flex; align-items:center; justify-content:space-between; gap:12px; }
.page-heading p,.dialog-heading p { margin:0 0 3px; color:#777366; font-size:11px; letter-spacing:.09em; }
.page-heading h1 { margin:0; font-size:22px; font-weight:650; }.page-heading h1 span { margin-left:8px; color:#777366; font-size:16px; font-weight:450; }.page-heading small { display:block; margin-top:4px; color:#777366; }
button,input,select,textarea { font:inherit; } button { cursor:pointer; } button:disabled { opacity:.45; cursor:not-allowed; }
.close-button,.action-row button,.dialog-actions button,.query-control button,.serial-control button,.component-picker button { min-height:29px; padding:4px 10px; border:1px solid #bdb9aa; border-radius:3px; background:#fffefa; color:#26251f; }
.action-row { display:flex; align-items:center; gap:6px; flex-wrap:wrap; }.action-row .primary,.primary { border-color:#3b3930; background:#3b3930; color:white; }.danger { border-color:#a84738!important; background:#a84738!important; color:white!important; }
.notice { margin:0; padding:7px 10px; border:1px solid #d9d3bf; border-radius:3px; background:#f1eddf; color:#625b46; }
.query-panel,.panel { border:1px solid #c9c5b9; border-radius:4px; background:#fffefa; }.query-panel { display:flex; align-items:end; gap:16px; padding:9px 12px; }.query-panel label,.form-grid label { display:flex; flex-direction:column; gap:4px; min-width:220px; color:#4d4a40; font-size:12px; }.query-control,.serial-control { display:flex; gap:6px; }.query-control input,.form-grid input,.form-grid select,.form-grid textarea,.serial-control input { min-height:29px; box-sizing:border-box; padding:4px 7px; border:1px solid #c9c5b9; border-radius:3px; background:#fff; color:#26251f; }.query-control input { min-width:270px; }.query-hint,.form-grid small { color:#777366; font-size:11px; }
.panel { display:flex; flex-direction:column; min-height:170px; overflow:hidden; }.panel-heading { padding:9px 11px; border-bottom:1px solid #d8d4ca; }.panel-heading h2 { margin:0; font-size:15px; }.panel-heading span { color:#777366; font-size:11px; }.panel-heading>div:first-child { display:flex; flex-direction:column; gap:2px; }
.table-wrap { overflow:auto; flex:1; } table { width:100%; border-collapse:collapse; font-size:12px; white-space:nowrap; } th,td { padding:6px 8px; border-bottom:1px solid #e6e3db; text-align:left; } th { position:sticky; top:0; z-index:1; background:#efede6; color:#5c594f; font-weight:600; } tbody tr { cursor:pointer; } tbody tr:hover { background:#f6f4ec; } tbody tr.active { background:#e8edf1; }.select-col { width:34px; text-align:center; }.mono { font-family:Consolas,'Courier New',monospace; }.description { min-width:180px; white-space:normal; }.numeric { text-align:right; font-variant-numeric:tabular-nums; }.state-badge { display:inline-block; padding:2px 6px; border-radius:10px; background:#f3ead1; color:#745d24; font-size:10px; }.empty { padding:20px; color:#777366; text-align:center; cursor:default; }.grid-footer { display:flex; justify-content:space-between; gap:10px; padding:6px 10px; border-top:1px solid #e6e3db; color:#777366; font-size:11px; }.detail-panel { flex:1; min-height:220px; }.detail-table-wrap small { display:block; color:#777366; font-size:10px; }.text-button { padding:0; border:0; background:transparent; color:#315b79; text-decoration:underline; }.component-row { background:#f7f5ee; cursor:default; }.component-row td { display:flex; align-items:center; gap:14px; flex-wrap:wrap; }.component-row strong { color:#625b46; }
.modal-backdrop { position:fixed; inset:0; z-index:1000; display:grid; place-items:center; overflow:auto; padding:18px; background:rgba(27,26,22,.42); }.dialog { width:min(690px,100%); max-height:min(90vh,920px); overflow:auto; padding:15px; border:1px solid #bdb9aa; border-radius:5px; background:#fffefa; box-shadow:0 15px 50px rgba(0,0,0,.24); }.detail-dialog { width:min(850px,100%); }.dialog-heading { margin-bottom:12px; }.dialog-heading h2 { margin:0; font-size:18px; }.icon-button { width:30px; height:30px; border:0; border-radius:3px; background:transparent; color:#555; font-size:23px; }.form-grid { display:grid; grid-template-columns:1fr 1fr; gap:12px; }.form-grid .span-two { grid-column:span 2; }.serial-control input { flex:1; min-width:0; }.dialog-actions { display:flex; justify-content:flex-end; gap:7px; margin-top:15px; padding-top:10px; border-top:1px solid #e6e3db; }.dialog-context { margin:0 0 12px; color:#625b46; font-size:12px; }.stock-card { display:flex; flex-direction:column; gap:4px; margin-top:12px; padding:9px; border:1px solid #d9d3bf; border-radius:3px; background:#f7f5ee; font-size:12px; }.stock-card span,.stock-card small { color:#6f6a5c; }.component-picker { margin-top:12px; border:1px solid #d8d4ca; border-radius:3px; }.component-picker header { padding:8px; border-bottom:1px solid #e6e3db; }.component-picker h3 { margin:0; font-size:13px; }.component-picker header span { color:#777366; font-size:11px; }.component-list { max-height:190px; overflow:auto; padding:5px 9px; }.component-option { display:grid; grid-template-columns:20px minmax(150px,1fr) 2fr; align-items:center; gap:8px; padding:6px 0; border-bottom:1px solid #efede6; font-size:12px; }.component-option small { color:#777366; }.component-option input { justify-self:start; }.rule-note { margin:10px 0 0; padding:8px; border-left:3px solid #b29957; background:#f7f5ee; color:#625b46; font-size:11px; }.confirm-dialog { width:min(470px,100%); }.confirm-dialog>p { color:#625b46; line-height:1.6; }
@media (max-width:800px) { .query-panel { align-items:stretch; flex-direction:column; gap:7px; }.query-panel label { min-width:0; }.query-control input { min-width:0; flex:1; }.page-heading { align-items:flex-start; }.page-heading h1 { font-size:18px; }.form-grid { grid-template-columns:1fr; }.form-grid .span-two { grid-column:auto; }.component-option { grid-template-columns:20px 1fr; }.component-option small { grid-column:2; } }
</style>
