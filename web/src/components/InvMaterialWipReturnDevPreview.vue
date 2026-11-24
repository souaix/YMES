<script setup lang="ts">
import { computed, ref } from 'vue'

const emit = defineEmits<{ close: [] }>()

type FromPlaceType = 2 | 3 | 5 | 6 | 7
type BasisMode = 'add' | 'edit' | 'copy'
type DetailMode = 'add' | 'edit' | 'copy'
type DialogMode = `basis-${BasisMode}` | `detail-${DetailMode}` | 'delete-basis' | 'delete-detail' | null

interface Basis {
  materialReturnNo: string
  fromPlaceType: FromPlaceType
  fromInventoryNo: string
  inventoryNo: string
  state: number
  description: string
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
  dataStamp: number
}
interface SourcePlace {
  fromPlaceType: FromPlaceType
  fromPlaceNo: string
  name: string
  inventoryNo: string
}
interface SourceLot {
  key: string
  fromPlaceType: FromPlaceType
  fromPlaceNo: string
  materialNo: string
  materialLotNo: string
  locatorNo: string
  unitNo: string
  qty: number
  materialName: string
}
interface WipComponent {
  componentId: string
  sourceLotKey: string
  componentNo: string
  componentLotNo: string
  laserMark: string
  cassetteNo: string
  slotNo: string
  goodQty: number
  scrapQty: number
  unitNo: string
  releaseState: number
}
interface Detail {
  key: string
  materialReturnNo: string
  materialNo: string
  materialLotNo: string
  locatorNo: string
  toLocatorNo: string
  unitNo: string
  qty: number
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
  dataStamp: number
  componentIds: string[]
}
interface DevState {
  bases: Basis[]
  details: Detail[]
  bookedComponentIds: string[]
  nextSerial: number
  nextDetailKey: number
}
interface BasisDraft {
  materialReturnNo: string
  fromPlaceType: FromPlaceType
  fromInventoryNo: string
  inventoryNo: string
  description: string
}
interface DetailDraft {
  sourceLotKey: string
  materialNo: string
  materialLotNo: string
  locatorNo: string
  toLocatorNo: string
  unitNo: string
  qty: string
}

const storageKey = 'ymes.dev.iv302-material-wip-return.v1'
const fromPlaceLabels: Record<FromPlaceType, string> = {
  2: 'WIP Inventory',
  3: 'MO',
  5: 'WIP Semi',
  6: 'MO Semi',
  7: 'MO Material + Semi',
}
const fromPlaceTypes: FromPlaceType[] = [2, 3, 5, 6, 7]
const sourcePlaces: SourcePlace[] = [
  { fromPlaceType: 2, fromPlaceNo: 'WIP-RAW-01', name: '在製原料庫 A', inventoryNo: 'RAW-A' },
  { fromPlaceType: 2, fromPlaceNo: 'WIP-RAW-02', name: '在製原料庫 B', inventoryNo: 'RAW-B' },
  { fromPlaceType: 3, fromPlaceNo: 'MO-DEV-101', name: 'DEV 工單 101', inventoryNo: 'RAW-A' },
  { fromPlaceType: 5, fromPlaceNo: 'WIP-SEMI-01', name: '在製半成品庫 A', inventoryNo: 'SEMI-A' },
  { fromPlaceType: 6, fromPlaceNo: 'MO-DEV-201', name: 'DEV 半成品工單 201', inventoryNo: 'SEMI-A' },
  { fromPlaceType: 7, fromPlaceNo: 'MO-DEV-301', name: 'DEV 混合物料工單 301', inventoryNo: 'N/A' },
]
const targetInventories = [
  { inventoryNo: 'RAW-A', name: '原料庫 A', inventoryType: 0 },
  { inventoryNo: 'RAW-B', name: '原料庫 B', inventoryType: 0 },
  { inventoryNo: 'SEMI-A', name: '半成品庫 A', inventoryType: 2 },
]
const sourceLots: SourceLot[] = [
  { key: 'lot-wip-raw-sensor', fromPlaceType: 2, fromPlaceNo: 'WIP-RAW-01', materialNo: 'RM-SENSOR-05', materialLotNo: 'WIP-SNS-2611-01', locatorNo: 'WR-A-01', unitNo: 'EA', qty: 4, materialName: '感測器模組 S' },
  { key: 'lot-wip-raw-resin', fromPlaceType: 2, fromPlaceNo: 'WIP-RAW-01', materialNo: 'RM-RESIN-01', materialLotNo: 'WIP-RES-2611-03', locatorNo: 'WR-A-02', unitNo: 'KG', qty: 18.5, materialName: '環氧樹脂 A' },
  { key: 'lot-wip-raw-copper', fromPlaceType: 2, fromPlaceNo: 'WIP-RAW-02', materialNo: 'RM-COPPER-03', materialLotNo: 'WIP-CU-2611-02', locatorNo: 'WR-B-01', unitNo: 'RL', qty: 6, materialName: '銅箔材料 C' },
  { key: 'lot-mo-raw-chip', fromPlaceType: 3, fromPlaceNo: 'MO-DEV-101', materialNo: 'RM-CHIP-11', materialLotNo: 'MO101-CHIP-01', locatorNo: 'MO-A-01', unitNo: 'EA', qty: 3, materialName: '控制晶片 11' },
  { key: 'lot-wip-semi-product', fromPlaceType: 5, fromPlaceNo: 'WIP-SEMI-01', materialNo: 'PRD-CTRL-02', materialLotNo: 'SEMI-2611-08', locatorNo: 'WS-A-02', unitNo: 'EA', qty: 3, materialName: '控制板半成品' },
  { key: 'lot-mo-semi-product', fromPlaceType: 6, fromPlaceNo: 'MO-DEV-201', materialNo: 'PRD-PANEL-07', materialLotNo: 'MO201-PNL-03', locatorNo: 'MS-A-01', unitNo: 'EA', qty: 5, materialName: '面板半成品' },
  { key: 'lot-mo-mix-sensor', fromPlaceType: 7, fromPlaceNo: 'MO-DEV-301', materialNo: 'RM-SENSOR-05', materialLotNo: 'MO301-SNS-01', locatorNo: 'MO-MIX-01', unitNo: 'EA', qty: 2, materialName: '混合工單感測器' },
]
const sourceComponents: WipComponent[] = [
  { componentId: 'iv302-wip-sns-a', sourceLotKey: 'lot-wip-raw-sensor', componentNo: 'WIP-CMP-SNS-A', componentLotNo: 'CLOT-SNS-A', laserMark: 'LM-SNS-A', cassetteNo: 'CAS-W01', slotNo: '01', goodQty: 1, scrapQty: 0, unitNo: 'EA', releaseState: 0 },
  { componentId: 'iv302-wip-sns-b', sourceLotKey: 'lot-wip-raw-sensor', componentNo: 'WIP-CMP-SNS-B', componentLotNo: 'CLOT-SNS-B', laserMark: 'LM-SNS-B', cassetteNo: 'CAS-W01', slotNo: '02', goodQty: 1, scrapQty: 0, unitNo: 'EA', releaseState: 0 },
  { componentId: 'iv302-wip-sns-c', sourceLotKey: 'lot-wip-raw-sensor', componentNo: 'WIP-CMP-SNS-C', componentLotNo: 'CLOT-SNS-C', laserMark: 'LM-SNS-C', cassetteNo: 'CAS-W02', slotNo: '01', goodQty: 1, scrapQty: 0, unitNo: 'EA', releaseState: 0 },
  { componentId: 'iv302-mo-chip-a', sourceLotKey: 'lot-mo-raw-chip', componentNo: 'MO101-CMP-001', componentLotNo: 'CLOT-MO-001', laserMark: 'LM-MO-001', cassetteNo: 'CAS-M01', slotNo: '01', goodQty: 1, scrapQty: 0, unitNo: 'EA', releaseState: 0 },
  { componentId: 'iv302-mo-chip-b', sourceLotKey: 'lot-mo-raw-chip', componentNo: 'MO101-CMP-002', componentLotNo: 'CLOT-MO-002', laserMark: 'LM-MO-002', cassetteNo: 'CAS-M01', slotNo: '02', goodQty: 1, scrapQty: 0, unitNo: 'EA', releaseState: 1 },
  { componentId: 'iv302-semi-cmp-a', sourceLotKey: 'lot-wip-semi-product', componentNo: 'SEMI-CMP-001', componentLotNo: 'CLOT-PNL-01', laserMark: 'LM-PNL-01', cassetteNo: 'CAS-S01', slotNo: '01', goodQty: 1, scrapQty: 0, unitNo: 'EA', releaseState: 0 },
  { componentId: 'iv302-semi-cmp-b', sourceLotKey: 'lot-wip-semi-product', componentNo: 'SEMI-CMP-002', componentLotNo: 'CLOT-PNL-02', laserMark: 'LM-PNL-02', cassetteNo: 'CAS-S01', slotNo: '02', goodQty: 1, scrapQty: 0, unitNo: 'EA', releaseState: 0 },
  { componentId: 'iv302-mix-cmp-a', sourceLotKey: 'lot-mo-mix-sensor', componentNo: 'MIX-CMP-001', componentLotNo: 'CLOT-MIX-01', laserMark: 'LM-MIX-01', cassetteNo: 'CAS-X01', slotNo: '01', goodQty: 1, scrapQty: 0, unitNo: 'EA', releaseState: 0 },
  { componentId: 'iv302-mix-cmp-b', sourceLotKey: 'lot-mo-mix-sensor', componentNo: 'MIX-CMP-002', componentLotNo: 'CLOT-MIX-02', laserMark: 'LM-MIX-02', cassetteNo: 'CAS-X01', slotNo: '02', goodQty: 1, scrapQty: 0, unitNo: 'EA', releaseState: 0 },
]
const sourceLocators: Record<string, string[]> = {
  'WIP-RAW-01': ['WR-A-01', 'WR-A-02'],
  'WIP-RAW-02': ['WR-B-01'],
  'MO-DEV-101': ['MO-A-01'],
  'WIP-SEMI-01': ['WS-A-01', 'WS-A-02'],
  'MO-DEV-201': ['MS-A-01'],
  'MO-DEV-301': ['MO-MIX-01'],
}
const targetLocators: Record<string, string[]> = {
  'RAW-A': ['RAW-A-01', 'RAW-A-02'],
  'RAW-B': ['RAW-B-01'],
  'SEMI-A': ['SEMI-A-01', 'SEMI-A-02'],
  'N/A': ['N/A'],
}

function initialState(): DevState {
  return {
    bases: [
      { materialReturnNo: 'MWR-DEV-0001', fromPlaceType: 2, fromInventoryNo: 'WIP-RAW-01', inventoryNo: 'RAW-A', state: 0, description: 'DEV 假資料：在製原料退回原料庫。', creator: 'DEV001', createDate: '2026-11-21 09:25:00', reviser: '', reviseDate: '', dataStamp: 0 },
      { materialReturnNo: 'MWR-DEV-0002', fromPlaceType: 3, fromInventoryNo: 'MO-DEV-101', inventoryNo: 'RAW-A', state: 0, description: 'DEV 假資料：工單原料退料。', creator: 'DEV002', createDate: '2026-11-21 11:10:00', reviser: 'DEV003', reviseDate: '2026-11-22 08:30:00', dataStamp: 1 },
      { materialReturnNo: 'MWR-DEV-0003', fromPlaceType: 5, fromInventoryNo: 'WIP-SEMI-01', inventoryNo: 'SEMI-A', state: 0, description: 'DEV 假資料：在製半成品退料。', creator: 'DEV001', createDate: '2026-11-22 14:05:00', reviser: '', reviseDate: '', dataStamp: 0 },
      { materialReturnNo: 'MWR-DEV-0004', fromPlaceType: 6, fromInventoryNo: 'MO-DEV-201', inventoryNo: 'SEMI-A', state: 0, description: 'DEV 假資料：工單半成品退料。', creator: 'DEV004', createDate: '2026-11-23 10:40:00', reviser: '', reviseDate: '', dataStamp: 0 },
      { materialReturnNo: 'MWR-DEV-0005', fromPlaceType: 7, fromInventoryNo: 'MO-DEV-301', inventoryNo: 'N/A', state: 0, description: 'DEV 假資料：工單混合物料／半成品退料。', creator: 'DEV004', createDate: '2026-11-23 16:20:00', reviser: '', reviseDate: '', dataStamp: 0 },
    ],
    details: [
      { key: 'iv302-d-1', materialReturnNo: 'MWR-DEV-0001', materialNo: 'RM-SENSOR-05', materialLotNo: 'WIP-SNS-2611-01', locatorNo: 'WR-A-01', toLocatorNo: 'RAW-A-01', unitNo: 'EA', qty: 2, creator: 'DEV001', createDate: '2026-11-21 09:32:00', reviser: '', reviseDate: '', dataStamp: 0, componentIds: ['iv302-wip-sns-a', 'iv302-wip-sns-b'] },
      { key: 'iv302-d-2', materialReturnNo: 'MWR-DEV-0001', materialNo: 'RM-RESIN-01', materialLotNo: 'WIP-RES-2611-03', locatorNo: 'WR-A-02', toLocatorNo: 'RAW-A-02', unitNo: 'KG', qty: 5.5, creator: 'DEV001', createDate: '2026-11-21 09:40:00', reviser: '', reviseDate: '', dataStamp: 0, componentIds: [] },
      { key: 'iv302-d-3', materialReturnNo: 'MWR-DEV-0002', materialNo: 'RM-CHIP-11', materialLotNo: 'MO101-CHIP-01', locatorNo: 'MO-A-01', toLocatorNo: 'RAW-A-01', unitNo: 'EA', qty: 1, creator: 'DEV002', createDate: '2026-11-21 11:22:00', reviser: '', reviseDate: '', dataStamp: 0, componentIds: ['iv302-mo-chip-a'] },
      { key: 'iv302-d-4', materialReturnNo: 'MWR-DEV-0003', materialNo: 'PRD-CTRL-02', materialLotNo: 'SEMI-2611-08', locatorNo: 'WS-A-02', toLocatorNo: 'SEMI-A-01', unitNo: 'EA', qty: 1, creator: 'DEV001', createDate: '2026-11-22 14:15:00', reviser: '', reviseDate: '', dataStamp: 0, componentIds: ['iv302-semi-cmp-a'] },
      { key: 'iv302-d-5', materialReturnNo: 'MWR-DEV-0004', materialNo: 'PRD-PANEL-07', materialLotNo: 'MO201-PNL-03', locatorNo: 'MS-A-01', toLocatorNo: 'SEMI-A-02', unitNo: 'EA', qty: 2, creator: 'DEV004', createDate: '2026-11-23 10:52:00', reviser: '', reviseDate: '', dataStamp: 0, componentIds: [] },
      { key: 'iv302-d-6', materialReturnNo: 'MWR-DEV-0005', materialNo: 'RM-SENSOR-05', materialLotNo: 'MO301-SNS-01', locatorNo: 'MO-MIX-01', toLocatorNo: 'N/A', unitNo: 'EA', qty: 1, creator: 'DEV004', createDate: '2026-11-23 16:31:00', reviser: '', reviseDate: '', dataStamp: 0, componentIds: ['iv302-mix-cmp-a'] },
    ],
    bookedComponentIds: ['iv302-wip-sns-a', 'iv302-wip-sns-b', 'iv302-mo-chip-a', 'iv302-semi-cmp-a', 'iv302-mix-cmp-a'],
    nextSerial: 6,
    nextDetailKey: 7,
  }
}

function readState(): DevState {
  try {
    const saved = localStorage.getItem(storageKey)
    if (saved) {
      const value = JSON.parse(saved) as DevState
      if (Array.isArray(value.bases) && Array.isArray(value.details) && Array.isArray(value.bookedComponentIds)) return value
    }
  } catch { /* Keep the DEV fixture usable when browser storage is unavailable. */ }
  return initialState()
}

const records = ref<DevState>(readState())
const searchText = ref('')
const appliedSearch = ref('')
const activeBasisNo = ref('MWR-DEV-0001')
const selectedBasisNos = ref<string[]>([])
const selectedDetailKeys = ref<string[]>([])
const expandedDetailKeys = ref<string[]>([])
const dialogMode = ref<DialogMode>(null)
const notice = ref('公司端 DEV 假資料；操作只存在此瀏覽器，不呼叫正式 API。')
const basisDraft = ref<BasisDraft>({ materialReturnNo: '', fromPlaceType: 2, fromInventoryNo: '', inventoryNo: '', description: '' })
const detailDraft = ref<DetailDraft>({ sourceLotKey: '', materialNo: '', materialLotNo: '', locatorNo: '', toLocatorNo: '', unitNo: '', qty: '' })
const componentSelection = ref<string[]>([])
const editingDetailKey = ref('')

const visibleBases = computed(() => records.value.bases
  .filter((row) => row.state === 0)
  .filter((row) => !appliedSearch.value || row.materialReturnNo.toLowerCase().includes(appliedSearch.value.toLowerCase()))
  .sort((a, b) => a.materialReturnNo.localeCompare(b.materialReturnNo)))
const activeBasis = computed(() => visibleBases.value.find((row) => row.materialReturnNo === activeBasisNo.value) ?? visibleBases.value[0] ?? null)
const visibleDetails = computed(() => records.value.details
  .filter((row) => row.materialReturnNo === activeBasis.value?.materialReturnNo)
  .sort((a, b) => [a.materialNo, a.materialLotNo, a.locatorNo].join('|').localeCompare([b.materialNo, b.materialLotNo, b.locatorNo].join('|'))))
const activeDetail = computed(() => visibleDetails.value.find((row) => row.key === selectedDetailKeys.value[0]) ?? visibleDetails.value[0] ?? null)
const currentDetail = computed(() => records.value.details.find((row) => row.key === editingDetailKey.value) ?? null)
const basisSourceOptions = computed(() => sourcePlaces.filter((row) => row.fromPlaceType === basisDraft.value.fromPlaceType))
const basisTargetOptions = computed(() => targetInventories.filter((row) => row.inventoryType === (basisDraft.value.fromPlaceType === 5 || basisDraft.value.fromPlaceType === 6 ? 2 : 0)))
const availableSourceLots = computed(() => sourceLots.filter((row) => row.fromPlaceType === activeBasis.value?.fromPlaceType && row.fromPlaceNo === activeBasis.value?.fromInventoryNo))
const selectedSourceLot = computed(() => sourceLots.find((row) => row.key === detailDraft.value.sourceLotKey) ?? null)
const allSourceComponents = computed(() => sourceComponents.filter((row) => row.sourceLotKey === detailDraft.value.sourceLotKey))
const availableComponents = computed(() => {
  const booked = new Set(records.value.bookedComponentIds)
  const oldIds = new Set(currentDetail.value?.componentIds ?? [])
  return allSourceComponents.value.filter((row) => !booked.has(row.componentId) || oldIds.has(row.componentId))
})
const componentRowsInDetail = (detail: Detail) => sourceComponents.filter((row) => detail.componentIds.includes(row.componentId))
const hasComponentData = computed(() => allSourceComponents.value.length > 0)
const isBasisEdit = computed(() => dialogMode.value === 'basis-edit')
const isDetailEdit = computed(() => dialogMode.value === 'detail-edit')
const basisDialogTitle = computed(() => dialogMode.value === 'basis-edit' ? 'Edit MaterialReturn Basis' : dialogMode.value === 'basis-copy' ? 'Copy MaterialReturn Basis' : 'Add MaterialReturn Basis')
const detailDialogTitle = computed(() => dialogMode.value === 'detail-edit' ? 'Edit MaterialReturn Detail' : dialogMode.value === 'detail-copy' ? 'Copy MaterialReturn Detail' : 'Add MaterialReturn Detail')
const selectedBasisCount = computed(() => selectedBasisNos.value.length)

function persist(message: string): void {
  try { localStorage.setItem(storageKey, JSON.stringify(records.value)) } catch { /* Keep the current browser session usable. */ }
  notice.value = message
}
function serialText(): string { return `MWR-DEV-${String(records.value.nextSerial).padStart(4, '0')}` }
function nowText(): string { return new Date().toLocaleString('sv-SE', { hour12: false }).replace('T', ' ') }
function formatQty(value: number): string { return new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 8 }).format(value) }
function runQuery(): void { appliedSearch.value = searchText.value.trim(); notice.value = `已查詢 State=0 草稿：${visibleBases.value.length} 筆。` }
function clearQuery(): void { searchText.value = ''; appliedSearch.value = ''; notice.value = '已清除單號條件；畫面仍只列未確認草稿。' }
function activateBasis(row: Basis): void { activeBasisNo.value = row.materialReturnNo; selectedDetailKeys.value = [] }
function toggleBasisSelection(no: string): void { selectedBasisNos.value = selectedBasisNos.value.includes(no) ? selectedBasisNos.value.filter((item) => item !== no) : [...selectedBasisNos.value, no] }
function toggleDetailSelection(key: string): void { selectedDetailKeys.value = selectedDetailKeys.value.includes(key) ? selectedDetailKeys.value.filter((item) => item !== key) : [...selectedDetailKeys.value, key] }
function toggleDetailExpand(key: string): void { expandedDetailKeys.value = expandedDetailKeys.value.includes(key) ? expandedDetailKeys.value.filter((item) => item !== key) : [...expandedDetailKeys.value, key] }

function openBasisDialog(mode: BasisMode): void {
  if (mode !== 'add' && !activeBasis.value) return
  const row = activeBasis.value
  const newNo = mode === 'edit' ? row?.materialReturnNo ?? '' : serialText()
  basisDraft.value = {
    materialReturnNo: newNo,
    fromPlaceType: row?.fromPlaceType ?? 2,
    fromInventoryNo: mode === 'add' ? '' : row?.fromInventoryNo ?? '',
    inventoryNo: mode === 'add' ? '' : row?.inventoryNo ?? '',
    description: mode === 'add' ? '' : row?.description ?? '',
  }
  dialogMode.value = `basis-${mode}`
}
function changeBasisType(): void {
  basisDraft.value.fromInventoryNo = ''
  basisDraft.value.inventoryNo = basisDraft.value.fromPlaceType === 7 ? 'N/A' : ''
}
function changeBasisSource(): void {
  const place = basisSourceOptions.value.find((row) => row.fromPlaceNo === basisDraft.value.fromInventoryNo)
  if (!place) return
  if ([3, 6, 7].includes(place.fromPlaceType)) basisDraft.value.inventoryNo = place.inventoryNo
}
function saveBasis(keepOpen = false): void {
  const draft = basisDraft.value
  if (!draft.materialReturnNo.trim() || !draft.fromInventoryNo || !draft.inventoryNo) { notice.value = '請選擇來源類型、來源庫房／工單及退入庫房。'; return }
  if (dialogMode.value === 'basis-edit') {
    records.value.bases = records.value.bases.map((row) => row.materialReturnNo === draft.materialReturnNo
      ? { ...row, description: draft.description, reviser: 'DEV001', reviseDate: nowText(), dataStamp: row.dataStamp + 1 }
      : row)
    activeBasisNo.value = draft.materialReturnNo
    persist('已更新 DEV 單頭說明；來源類型、來源及目標庫房維持不變。')
  } else {
    const row: Basis = { ...draft, state: 0, creator: 'DEV001', createDate: nowText(), reviser: '', reviseDate: '', dataStamp: 0 }
    records.value.bases = [...records.value.bases, row]
    records.value.nextSerial += 1
    activeBasisNo.value = row.materialReturnNo
    appliedSearch.value = ''
    searchText.value = ''
    persist(dialogMode.value === 'basis-copy' ? '已複製 DEV 單頭；來源明細不隨單頭複製。' : '已新增 DEV 未確認退料單頭。')
  }
  selectedBasisNos.value = []
  selectedDetailKeys.value = []
  if (!keepOpen) {
    dialogMode.value = null
  } else if (dialogMode.value === 'basis-add' || dialogMode.value === 'basis-copy') {
    basisDraft.value = { materialReturnNo: serialText(), fromPlaceType: 2, fromInventoryNo: '', inventoryNo: '', description: '' }
    dialogMode.value = 'basis-add'
  }
}
function requestDeleteBasis(): void {
  const requested = selectedBasisNos.value.length ? selectedBasisNos.value : activeBasis.value ? [activeBasis.value.materialReturnNo] : []
  selectedBasisNos.value = requested.filter((no) => records.value.bases.some((row) => row.materialReturnNo === no && row.state === 0))
  if (selectedBasisNos.value.length) dialogMode.value = 'delete-basis'
}
function confirmDeleteBasis(): void {
  const removedNos = new Set(selectedBasisNos.value)
  const removedDetails = records.value.details.filter((row) => removedNos.has(row.materialReturnNo))
  const releasedIds = new Set(removedDetails.flatMap((row) => row.componentIds))
  records.value.bases = records.value.bases.filter((row) => !removedNos.has(row.materialReturnNo))
  records.value.details = records.value.details.filter((row) => !removedNos.has(row.materialReturnNo))
  records.value.bookedComponentIds = records.value.bookedComponentIds.filter((id) => !releasedIds.has(id))
  activeBasisNo.value = ''
  selectedBasisNos.value = []
  selectedDetailKeys.value = []
  persist(`已刪除 ${removedNos.size} 張 DEV 未確認單頭與明細，並釋放元件假預約。`)
  dialogMode.value = null
}

function openDetailDialog(mode: DetailMode): void {
  const basis = activeBasis.value
  if (!basis || basis.state !== 0) return
  const row = mode === 'add' ? null : activeDetail.value
  if (mode !== 'add' && !row) return
  editingDetailKey.value = mode === 'edit' ? row?.key ?? '' : ''
  const lot = row ? sourceLots.find((item) => item.fromPlaceType === basis.fromPlaceType && item.fromPlaceNo === basis.fromInventoryNo && item.materialNo === row.materialNo && item.materialLotNo === row.materialLotNo) : null
  detailDraft.value = {
    sourceLotKey: lot?.key ?? '',
    materialNo: row?.materialNo ?? '',
    materialLotNo: row?.materialLotNo ?? '',
    locatorNo: row?.locatorNo ?? '',
    toLocatorNo: row?.toLocatorNo ?? '',
    unitNo: row?.unitNo ?? '',
    qty: row ? (mode === 'copy' && row.componentIds.length ? '0' : String(row.qty)) : '',
  }
  componentSelection.value = mode === 'edit' ? [...(row?.componentIds ?? [])] : []
  dialogMode.value = `detail-${mode}`
}
function chooseSourceLot(key: string): void {
  const lot = sourceLots.find((row) => row.key === key)
  if (!lot) return
  detailDraft.value.sourceLotKey = lot.key
  detailDraft.value.materialNo = lot.materialNo
  detailDraft.value.materialLotNo = lot.materialLotNo
  detailDraft.value.locatorNo = lot.locatorNo || 'N/A'
  detailDraft.value.unitNo = lot.unitNo
  detailDraft.value.toLocatorNo = targetLocators[activeBasis.value?.inventoryNo ?? 'N/A']?.[0] ?? 'N/A'
  componentSelection.value = []
  detailDraft.value.qty = sourceComponents.some((row) => row.sourceLotKey === lot.key) ? '0' : String(lot.qty)
}
function toggleComponent(id: string): void {
  componentSelection.value = componentSelection.value.includes(id) ? componentSelection.value.filter((item) => item !== id) : [...componentSelection.value, id]
  detailDraft.value.qty = String(componentSelection.value.length)
}
function selectAllComponents(): void { componentSelection.value = availableComponents.value.map((row) => row.componentId); detailDraft.value.qty = String(componentSelection.value.length) }
function clearComponents(): void { componentSelection.value = []; detailDraft.value.qty = '0' }
function saveDetail(keepOpen = false): void {
  const basis = activeBasis.value
  const lot = selectedSourceLot.value
  const qty = Number(detailDraft.value.qty)
  if (!basis || !lot || !detailDraft.value.materialNo || !detailDraft.value.materialLotNo || !detailDraft.value.locatorNo || !detailDraft.value.toLocatorNo) { notice.value = '請選擇來源物料批號、來源儲位及退入儲位。'; return }
  if (!Number.isFinite(qty) || qty <= 0) { notice.value = 'Qty 必須大於 0。'; return }
  if (qty > lot.qty) { notice.value = `Qty 不可大於此來源批號的 DEV 可用量 ${formatQty(lot.qty)} ${lot.unitNo}。`; return }
  if (basis.fromPlaceType === 7 && allSourceComponents.value.length === 0) { notice.value = '來源類型 MO Material + Semi 必須有可選元件。'; return }
  if (hasComponentData.value && (componentSelection.value.length === 0 || !Number.isInteger(qty) || qty !== componentSelection.value.length)) { notice.value = '此來源批號含元件；至少選一個未預約元件，且 Qty 必須等於已選元件數。'; return }
  const released = availableComponents.value.find((row) => componentSelection.value.includes(row.componentId) && basis.fromPlaceType === 3 && row.releaseState === 1)
  if (released) { notice.value = `元件 ${released.componentNo} 已建立並釋出批號，不能退回。`; return }
  const duplicate = records.value.details.find((row) => row.materialReturnNo === basis.materialReturnNo && row.materialNo === detailDraft.value.materialNo && row.materialLotNo === detailDraft.value.materialLotNo && row.locatorNo === detailDraft.value.locatorNo && row.key !== editingDetailKey.value)
  if (duplicate) { notice.value = '相同 MaterialNo、MaterialLotNo、LocatorNo 已存在於此單。'; return }

  const old = currentDetail.value
  const now = nowText()
  const row: Detail = {
    key: old?.key ?? `iv302-d-${records.value.nextDetailKey}`,
    materialReturnNo: basis.materialReturnNo,
    materialNo: detailDraft.value.materialNo,
    materialLotNo: detailDraft.value.materialLotNo,
    locatorNo: detailDraft.value.locatorNo,
    toLocatorNo: detailDraft.value.toLocatorNo,
    unitNo: detailDraft.value.unitNo || lot.unitNo,
    qty,
    creator: old?.creator ?? 'DEV001',
    createDate: old?.createDate ?? now,
    reviser: old ? 'DEV001' : '',
    reviseDate: old ? now : '',
    dataStamp: old ? old.dataStamp + 1 : 0,
    componentIds: [...componentSelection.value],
  }
  if (old) {
    const releasedIds = new Set(old.componentIds)
    records.value.bookedComponentIds = records.value.bookedComponentIds.filter((id) => !releasedIds.has(id))
  } else {
    records.value.nextDetailKey += 1
  }
  records.value.bookedComponentIds = [...new Set([...records.value.bookedComponentIds, ...row.componentIds])]
  records.value.details = old
    ? records.value.details.map((item) => item.key === old.key ? row : item)
    : [...records.value.details, row]
  selectedDetailKeys.value = [row.key]
  persist(dialogMode.value === 'detail-edit' ? '已更新 DEV 退料明細與元件預約。' : '已新增 DEV 退料明細與元件假預約。')
  if (!keepOpen) {
    dialogMode.value = null
  } else if (dialogMode.value === 'detail-add' || dialogMode.value === 'detail-copy') {
    editingDetailKey.value = ''
    detailDraft.value = { sourceLotKey: '', materialNo: '', materialLotNo: '', locatorNo: '', toLocatorNo: '', unitNo: '', qty: '' }
    componentSelection.value = []
    dialogMode.value = 'detail-add'
  }
}
function requestDeleteDetails(): void {
  const requested = selectedDetailKeys.value.length ? selectedDetailKeys.value : activeDetail.value ? [activeDetail.value.key] : []
  selectedDetailKeys.value = requested.filter((key) => visibleDetails.value.some((row) => row.key === key))
  if (selectedDetailKeys.value.length) dialogMode.value = 'delete-detail'
}
function confirmDeleteDetails(): void {
  const keySet = new Set(selectedDetailKeys.value)
  const released = new Set(records.value.details.filter((row) => keySet.has(row.key)).flatMap((row) => row.componentIds))
  records.value.details = records.value.details.filter((row) => !keySet.has(row.key))
  records.value.bookedComponentIds = records.value.bookedComponentIds.filter((id) => !released.has(id))
  selectedDetailKeys.value = []
  persist(`已刪除 ${keySet.size} 筆 DEV 明細並釋放其元件假預約。`)
  dialogMode.value = null
}
</script>

<template>
  <main class="iv302-page" aria-label="開立退料單(WIP)">
    <header class="iv302-header">
      <div>
        <div class="iv302-kicker">INVENTORY · IV302</div>
        <h1>WIP MATERIAL RETURN <span>開立退料單(WIP)</span></h1>
      </div>
      <div class="iv302-header-actions">
        <span class="iv302-state-tag">只顯示未確認草稿</span>
        <button type="button" class="iv302-button" @click="emit('close')">Close (X)</button>
      </div>
    </header>

    <section class="iv302-search" aria-label="退料單查詢">
      <label for="iv302-search-no">Material Return No</label>
      <input id="iv302-search-no" v-model="searchText" type="search" placeholder="輸入退料單號" @keydown.enter="runQuery">
      <button type="button" class="iv302-button iv302-button-primary" @click="runQuery">Query</button>
      <button type="button" class="iv302-button" @click="clearQuery">Clear</button>
      <span class="iv302-search-hint">WinForms 查詢固定帶入 State = 0</span>
    </section>

    <section class="iv302-panel iv302-basis-panel">
      <div class="iv302-panel-heading">
        <div><h2>Material Return Basis</h2><span>{{ visibleBases.length }} 筆草稿</span></div>
        <div class="iv302-toolbar">
          <button type="button" class="iv302-button iv302-button-primary" @click="openBasisDialog('add')">＋ Add</button>
          <button type="button" class="iv302-button" :disabled="!activeBasis" @click="openBasisDialog('copy')">Copy</button>
          <button type="button" class="iv302-button" :disabled="!activeBasis" @click="openBasisDialog('edit')">Edit</button>
          <button type="button" class="iv302-button iv302-button-danger" :disabled="!activeBasis" @click="requestDeleteBasis">Delete<span v-if="selectedBasisCount"> ({{ selectedBasisCount }})</span></button>
        </div>
      </div>
      <div class="iv302-table-wrap">
        <table class="iv302-table iv302-basis-table">
          <thead><tr><th class="iv302-check-col"><span class="sr-only">選取</span></th><th>MaterialReturnNo</th><th>FromPlaceType</th><th>FromInventoryNo</th><th>InventoryNo</th><th>State</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
          <tbody>
            <tr v-for="row in visibleBases" :key="row.materialReturnNo" :class="{ 'is-active': activeBasis?.materialReturnNo === row.materialReturnNo }" @click="activateBasis(row)">
              <td class="iv302-check-col" @click.stop><input :checked="selectedBasisNos.includes(row.materialReturnNo)" type="checkbox" :aria-label="`選取 ${row.materialReturnNo}`" @change="toggleBasisSelection(row.materialReturnNo)"></td>
              <td class="iv302-primary-cell">{{ row.materialReturnNo }}</td><td>{{ fromPlaceLabels[row.fromPlaceType] }} <small>({{ row.fromPlaceType }})</small></td><td>{{ row.fromInventoryNo }}</td><td>{{ row.inventoryNo }}</td><td><span class="iv302-unconfirm">UnConfirm</span></td><td class="iv302-description-cell">{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser || '—' }}</td><td>{{ row.reviseDate || '—' }}</td>
            </tr>
            <tr v-if="visibleBases.length === 0"><td colspan="11" class="iv302-empty">沒有符合條件的未確認退料單。</td></tr>
          </tbody>
        </table>
      </div>
    </section>

    <section class="iv302-panel iv302-detail-panel">
      <div class="iv302-panel-heading">
        <div><h2>Material Return Detail</h2><span>{{ activeBasis?.materialReturnNo ?? '請先選取單頭' }} · {{ visibleDetails.length }} 筆明細</span></div>
        <div class="iv302-toolbar">
          <button type="button" class="iv302-button iv302-button-primary" :disabled="!activeBasis" @click="openDetailDialog('add')">＋ Add</button>
          <button type="button" class="iv302-button" :disabled="!activeDetail" @click="openDetailDialog('copy')">Copy</button>
          <button type="button" class="iv302-button" :disabled="!activeDetail" @click="openDetailDialog('edit')">Edit</button>
          <button type="button" class="iv302-button iv302-button-danger" :disabled="!activeDetail" @click="requestDeleteDetails">Delete<span v-if="selectedDetailKeys.length"> ({{ selectedDetailKeys.length }})</span></button>
        </div>
      </div>
      <div class="iv302-table-wrap iv302-detail-wrap">
        <table class="iv302-table iv302-detail-table">
          <thead><tr><th class="iv302-check-col"><span class="sr-only">選取</span></th><th>MaterialNo</th><th>MaterialLotNo</th><th>LocatorNo</th><th>ToLocatorNo</th><th>UnitNo</th><th class="iv302-number">Qty</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
          <tbody>
            <template v-for="row in visibleDetails" :key="row.key">
              <tr :class="{ 'is-active': activeDetail?.key === row.key }" @click="selectedDetailKeys = [row.key]">
                <td class="iv302-check-col" @click.stop><input :checked="selectedDetailKeys.includes(row.key)" type="checkbox" :aria-label="`選取 ${row.materialNo} ${row.materialLotNo}`" @change="toggleDetailSelection(row.key)"></td>
                <td><button type="button" class="iv302-expand" :aria-expanded="expandedDetailKeys.includes(row.key)" @click.stop="toggleDetailExpand(row.key)">{{ expandedDetailKeys.includes(row.key) ? '▾' : '▸' }}</button>{{ row.materialNo }}</td><td>{{ row.materialLotNo }}</td><td>{{ row.locatorNo }}</td><td>{{ row.toLocatorNo }}</td><td>{{ row.unitNo }}</td><td class="iv302-number">{{ formatQty(row.qty) }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser || '—' }}</td><td>{{ row.reviseDate || '—' }}</td>
              </tr>
              <tr v-if="expandedDetailKeys.includes(row.key)" class="iv302-child-row"><td colspan="11">
                <div class="iv302-child-heading">Material Return Component · {{ componentRowsInDetail(row).length }} 筆</div>
                <div v-if="componentRowsInDetail(row).length" class="iv302-table-wrap"><table class="iv302-table iv302-component-table"><thead><tr><th>ComponentNo</th><th>ComponentLotNo</th><th>LaserMark</th><th>CassetteNo</th><th>SlotNo</th><th class="iv302-number">GoodQty</th><th class="iv302-number">ScrapQty</th><th>UnitNo</th></tr></thead><tbody><tr v-for="component in componentRowsInDetail(row)" :key="component.componentId"><td>{{ component.componentNo }}</td><td>{{ component.componentLotNo }}</td><td>{{ component.laserMark }}</td><td>{{ component.cassetteNo }}</td><td>{{ component.slotNo }}</td><td class="iv302-number">{{ formatQty(component.goodQty) }}</td><td class="iv302-number">{{ formatQty(component.scrapQty) }}</td><td>{{ component.unitNo }}</td></tr></tbody></table></div>
                <p v-else class="iv302-no-components">此明細沒有元件資料。</p>
              </td></tr>
            </template>
            <tr v-if="visibleDetails.length === 0"><td colspan="11" class="iv302-empty">此單頭目前沒有退料明細。</td></tr>
          </tbody>
        </table>
      </div>
    </section>

    <footer class="iv302-footer"><span role="status" aria-live="polite">{{ notice }}</span><span>DEV preview · local fixtures only</span></footer>

    <div v-if="dialogMode?.startsWith('basis-')" class="iv302-backdrop" @click.self="dialogMode = null">
      <section class="iv302-dialog" role="dialog" aria-modal="true" aria-labelledby="iv302-basis-title">
        <header class="iv302-dialog-header"><div><span class="iv302-kicker">IV302 · BASIS</span><h2 id="iv302-basis-title">{{ basisDialogTitle }}</h2></div><button type="button" class="iv302-icon-button" aria-label="關閉" @click="dialogMode = null">×</button></header>
        <div class="iv302-dialog-body">
          <label class="iv302-field"><span>MaterialReturnNo <b>*</b></span><input v-model="basisDraft.materialReturnNo" :readonly="isBasisEdit" maxlength="40"></label>
          <label class="iv302-field"><span>FromPlaceType <b>*</b></span><select v-model.number="basisDraft.fromPlaceType" :disabled="isBasisEdit" @change="changeBasisType"><option v-for="type in fromPlaceTypes" :key="type" :value="type">{{ fromPlaceLabels[type] }} ({{ type }})</option></select></label>
          <label class="iv302-field"><span>FromInventoryNo / MO No <b>*</b></span><select v-model="basisDraft.fromInventoryNo" :disabled="isBasisEdit" @change="changeBasisSource"><option value="">選擇來源</option><option v-for="place in basisSourceOptions" :key="place.fromPlaceNo" :value="place.fromPlaceNo">{{ place.fromPlaceNo }} · {{ place.name }}</option></select></label>
          <label class="iv302-field"><span>InventoryNo (退入庫房) <b>*</b></span><select v-if="[2, 5].includes(basisDraft.fromPlaceType)" v-model="basisDraft.inventoryNo" :disabled="isBasisEdit"><option value="">選擇目標庫房</option><option v-for="inventory in basisTargetOptions" :key="inventory.inventoryNo" :value="inventory.inventoryNo">{{ inventory.inventoryNo }} · {{ inventory.name }}</option></select><input v-else v-model="basisDraft.inventoryNo" readonly><small v-if="[3, 6, 7].includes(basisDraft.fromPlaceType)">依所選工單的 DEV 來源假資料帶入。</small></label>
          <label class="iv302-field iv302-field-wide"><span>Description</span><textarea v-model="basisDraft.description" rows="3" maxlength="240"></textarea></label>
          <div class="iv302-info">{{ isBasisEdit ? '原始 Edit 僅允許修改 Description，其他來源欄位鎖定。' : 'Copy 會建立新單頭，不會複製來源明細。單號由 DEV 假序號模擬。' }}</div>
        </div>
        <footer class="iv302-dialog-footer"><button type="button" class="iv302-button" @click="dialogMode = null">Cancel</button><button type="button" class="iv302-button" @click="saveBasis(true)">Apply</button><button type="button" class="iv302-button iv302-button-primary" @click="saveBasis(false)">Confirm</button></footer>
      </section>
    </div>

    <div v-if="dialogMode?.startsWith('detail-')" class="iv302-backdrop" @click.self="dialogMode = null">
      <section class="iv302-dialog iv302-detail-dialog" role="dialog" aria-modal="true" aria-labelledby="iv302-detail-title">
        <header class="iv302-dialog-header"><div><span class="iv302-kicker">IV302 · DETAIL</span><h2 id="iv302-detail-title">{{ detailDialogTitle }}</h2></div><button type="button" class="iv302-icon-button" aria-label="關閉" @click="dialogMode = null">×</button></header>
        <div class="iv302-dialog-body">
          <div class="iv302-context"><b>{{ activeBasis?.materialReturnNo }}</b><span>{{ activeBasis ? fromPlaceLabels[activeBasis.fromPlaceType] : '' }}</span><span>{{ activeBasis?.fromInventoryNo }} → {{ activeBasis?.inventoryNo }}</span></div>
          <label class="iv302-field iv302-field-wide"><span>來源 Material / Lot <b>*</b></span><select :value="detailDraft.sourceLotKey" :disabled="isDetailEdit" @change="chooseSourceLot(($event.target as HTMLSelectElement).value)"><option value="">選擇來源批號</option><option v-for="lot in availableSourceLots" :key="lot.key" :value="lot.key">{{ lot.materialNo }} · {{ lot.materialLotNo }} · 可用 {{ formatQty(lot.qty) }} {{ lot.unitNo }}</option></select></label>
          <label class="iv302-field"><span>MaterialNo <b>*</b></span><input v-model="detailDraft.materialNo" readonly></label>
          <label class="iv302-field"><span>MaterialLotNo <b>*</b></span><input v-model="detailDraft.materialLotNo" readonly></label>
          <label class="iv302-field"><span>LocatorNo (來源) <b>*</b></span><select v-model="detailDraft.locatorNo" :disabled="isDetailEdit"><option v-for="locator in sourceLocators[activeBasis?.fromInventoryNo ?? ''] ?? ['N/A']" :key="locator" :value="locator">{{ locator }}</option></select></label>
          <label class="iv302-field"><span>ToLocatorNo (退入) <b>*</b></span><select v-model="detailDraft.toLocatorNo"><option v-for="locator in targetLocators[activeBasis?.inventoryNo ?? 'N/A'] ?? ['N/A']" :key="locator" :value="locator">{{ locator }}</option></select></label>
          <label class="iv302-field"><span>UnitNo</span><input v-model="detailDraft.unitNo" readonly></label>
          <label class="iv302-field"><span>Qty <b>*</b></span><input v-model="detailDraft.qty" type="number" min="0" step="0.00000001"><small v-if="selectedSourceLot">來源可用量 {{ formatQty(selectedSourceLot.qty) }} {{ selectedSourceLot.unitNo }}</small></label>
          <section v-if="activeBasis && [2, 3, 5, 7].includes(activeBasis.fromPlaceType) && selectedSourceLot" class="iv302-components iv302-field-wide">
            <div class="iv302-components-heading"><div><b>Material Return Component Data</b><small>{{ availableComponents.length }} 個尚未被其他草稿預約的元件</small></div><div><button type="button" class="iv302-button iv302-button-small" :disabled="availableComponents.length === 0" @click="selectAllComponents">Select All</button><button type="button" class="iv302-button iv302-button-small" @click="clearComponents">UnSelect All</button></div></div>
            <div v-if="availableComponents.length" class="iv302-table-wrap iv302-component-pick-wrap"><table class="iv302-table iv302-component-table"><thead><tr><th>選取</th><th>ComponentNo</th><th>ComponentLotNo</th><th>CassetteNo</th><th>SlotNo</th><th>GoodQty</th><th>ScrapQty</th><th>狀態</th></tr></thead><tbody><tr v-for="component in availableComponents" :key="component.componentId"><td><input :checked="componentSelection.includes(component.componentId)" type="checkbox" :aria-label="`選取 ${component.componentNo}`" @change="toggleComponent(component.componentId)"></td><td>{{ component.componentNo }}</td><td>{{ component.componentLotNo }}</td><td>{{ component.cassetteNo }}</td><td>{{ component.slotNo }}</td><td>{{ component.goodQty }}</td><td>{{ component.scrapQty }}</td><td>{{ component.releaseState === 1 ? '已釋出工單批號' : '可退' }}</td></tr></tbody></table></div>
            <p v-else class="iv302-no-components">{{ activeBasis.fromPlaceType === 7 ? '此來源類型必須選擇元件，目前 DEV 假資料沒有可用元件。' : hasComponentData ? '元件皆已被其他 DEV 草稿預約。' : '此批號沒有元件資料，可輸入部分退料數量。' }}</p>
            <p class="iv302-info">原始邏輯：有元件資料時至少選一個，Qty 必須等於已選元件數；MO 元件若已釋出工單批號則不可退回。</p>
          </section>
          <div class="iv302-info iv302-field-wide">DEV 只模擬來源批號、儲位、數量與元件 BookingFlag；不扣正式庫存。確認流程由 IV304 負責。</div>
        </div>
        <footer class="iv302-dialog-footer"><button type="button" class="iv302-button" @click="dialogMode = null">Cancel</button><button type="button" class="iv302-button" @click="saveDetail(true)">Apply</button><button type="button" class="iv302-button iv302-button-primary" @click="saveDetail(false)">Confirm</button></footer>
      </section>
    </div>

    <div v-if="dialogMode === 'delete-basis' || dialogMode === 'delete-detail'" class="iv302-backdrop" @click.self="dialogMode = null">
      <section class="iv302-dialog iv302-confirm-dialog" role="dialog" aria-modal="true" aria-labelledby="iv302-delete-title">
        <header class="iv302-dialog-header"><div><span class="iv302-kicker">DELETE DRAFT</span><h2 id="iv302-delete-title">確認刪除</h2></div><button type="button" class="iv302-icon-button" aria-label="關閉" @click="dialogMode = null">×</button></header>
        <p v-if="dialogMode === 'delete-basis'">將刪除 {{ selectedBasisNos.length }} 張 State=0 單頭與所屬明細，並釋放假元件預約。</p>
        <p v-else>將刪除 {{ selectedDetailKeys.length }} 筆 State=0 明細並釋放假元件預約。</p>
        <footer class="iv302-dialog-footer"><button type="button" class="iv302-button" @click="dialogMode = null">Cancel</button><button type="button" class="iv302-button iv302-button-danger" @click="dialogMode === 'delete-basis' ? confirmDeleteBasis() : confirmDeleteDetails()">Delete</button></footer>
      </section>
    </div>
  </main>
</template>

<style scoped>
.iv302-page { display: flex; flex-direction: column; gap: 12px; height: 100%; min-height: 0; overflow: auto; padding: 14px; box-sizing: border-box; color: #26251f; background: #f4f2e9; font: 13px/1.4 "Segoe UI", "Microsoft JhengHei", sans-serif; }
.iv302-header,.iv302-panel-heading,.iv302-toolbar,.iv302-header-actions,.iv302-search,.iv302-dialog-header,.iv302-dialog-footer,.iv302-components-heading { display: flex; align-items: center; }
.iv302-header,.iv302-panel-heading,.iv302-dialog-header { justify-content: space-between; gap: 12px; }
.iv302-header { padding: 2px 2px 4px; }
.iv302-kicker { color: #8b8060; font-size: 10px; font-weight: 800; letter-spacing: .16em; }
.iv302-header h1 { margin: 3px 0 0; font-size: 20px; letter-spacing: .04em; }
.iv302-header h1 span { margin-left: 10px; color: #777366; font-size: 14px; font-weight: 500; letter-spacing: 0; }
.iv302-header-actions,.iv302-toolbar { gap: 6px; flex-wrap: wrap; }
.iv302-state-tag,.iv302-unconfirm { display: inline-flex; align-items: center; padding: 3px 7px; border: 1px solid #d8c58f; border-radius: 99px; background: #fff5d6; color: #70571c; font-size: 11px; font-weight: 700; white-space: nowrap; }
.iv302-button { min-height: 28px; padding: 4px 10px; border: 1px solid #aaa797; border-radius: 3px; background: #fbfaf6; color: #302f29; font: inherit; cursor: pointer; white-space: nowrap; }
.iv302-button:hover:not(:disabled) { border-color: #4b483e; background: #efede3; }
.iv302-button:disabled { opacity: .45; cursor: not-allowed; }
.iv302-button-primary { border-color: #466b63; background: #466b63; color: white; }
.iv302-button-primary:hover:not(:disabled) { background: #36564f; color: white; }
.iv302-button-danger { border-color: #c9a49b; color: #8b352b; }
.iv302-button-small { min-height: 24px; padding: 2px 7px; font-size: 11px; }
.iv302-search { gap: 7px; min-height: 42px; padding: 7px 10px; border: 1px solid #d5d1c2; background: #fbfaf6; }
.iv302-search label { font-weight: 700; white-space: nowrap; }
.iv302-search input { width: min(320px, 45vw); min-height: 27px; padding: 3px 7px; border: 1px solid #c8c4b8; border-radius: 2px; font: inherit; }
.iv302-search-hint { margin-left: auto; color: #777366; font-size: 11px; }
.iv302-panel { display: flex; flex-direction: column; min-height: 150px; border: 1px solid #c9c5b8; background: #fbfaf6; box-shadow: 0 2px 7px rgb(47 43 29 / 5%); }
.iv302-basis-panel { flex: 0 0 42%; min-height: 230px; }
.iv302-detail-panel { flex: 1 0 310px; min-height: 270px; }
.iv302-panel-heading { min-height: 46px; padding: 5px 9px; border-bottom: 1px solid #d7d3c7; background: linear-gradient(#f5f3eb, #eeece2); }
.iv302-panel-heading > div:first-child { display: flex; align-items: baseline; gap: 10px; }
.iv302-panel-heading h2 { margin: 0; font-size: 14px; }
.iv302-panel-heading span { color: #777366; font-size: 11px; }
.iv302-table-wrap { width: 100%; min-height: 0; overflow: auto; }
.iv302-table { width: 100%; border-collapse: collapse; white-space: nowrap; font-size: 12px; }
.iv302-table th { position: sticky; top: 0; z-index: 1; padding: 6px 8px; border-right: 1px solid #e1ded4; border-bottom: 1px solid #c9c5b8; background: #e8e5da; color: #514e42; font-size: 11px; font-weight: 700; text-align: left; }
.iv302-table td { padding: 6px 8px; border-right: 1px solid #efede7; border-bottom: 1px solid #e6e3d9; }
.iv302-table tbody tr:hover { background: #f5f3e9; }
.iv302-table tbody tr.is-active { background: #eaf0eb; box-shadow: inset 3px 0 #547d72; }
.iv302-table small { color: #898575; font-size: 10px; }
.iv302-check-col { width: 28px; min-width: 28px; text-align: center !important; }
.iv302-check-col input,.iv302-component-table input { accent-color: #52746a; }
.iv302-primary-cell { color: #375c52; font-weight: 700; }
.iv302-description-cell { max-width: 260px; overflow: hidden; text-overflow: ellipsis; }
.iv302-number { text-align: right !important; font-variant-numeric: tabular-nums; }
.iv302-empty { padding: 22px !important; color: #888475; text-align: center; }
.iv302-expand,.iv302-icon-button { border: 0; background: transparent; color: #536d62; cursor: pointer; font: inherit; }
.iv302-expand { width: 20px; padding: 0; font-size: 15px; }
.iv302-child-row td { padding: 8px 20px; background: #f8f7f1; }
.iv302-child-heading { margin-bottom: 5px; color: #5f5a4c; font-size: 11px; font-weight: 800; }
.iv302-component-table th { position: static; background: #efede3; }
.iv302-component-table td { padding: 5px 7px; }
.iv302-no-components { margin: 7px 0; color: #777366; }
.iv302-footer { display: flex; justify-content: space-between; gap: 12px; color: #777366; font-size: 11px; }
.iv302-backdrop { position: fixed; inset: 0; z-index: 1000; display: grid; place-items: center; padding: 18px; background: rgb(26 26 22 / 48%); }
.iv302-dialog { display: flex; flex-direction: column; width: min(660px, 96vw); max-height: min(92vh, 850px); overflow: auto; border: 1px solid #a9a495; border-radius: 4px; background: #fbfaf6; box-shadow: 0 16px 48px rgb(0 0 0 / 24%); }
.iv302-detail-dialog { width: min(920px, 96vw); }
.iv302-confirm-dialog { width: min(440px, 96vw); }
.iv302-dialog-header { padding: 11px 14px; border-bottom: 1px solid #d7d3c7; background: #efede3; }
.iv302-dialog-header h2 { margin: 2px 0 0; font-size: 16px; }
.iv302-icon-button { font-size: 23px; line-height: 1; }
.iv302-dialog-body { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px 12px; padding: 14px; }
.iv302-field { display: flex; flex-direction: column; gap: 4px; min-width: 0; }
.iv302-field > span { color: #595649; font-size: 11px; font-weight: 700; }
.iv302-field b { color: #9a4438; }
.iv302-field input,.iv302-field select,.iv302-field textarea { min-height: 30px; padding: 5px 7px; border: 1px solid #c8c4b8; border-radius: 2px; box-sizing: border-box; background: white; color: #27261f; font: inherit; }
.iv302-field input[readonly],.iv302-field select:disabled { background: #efeee8; color: #666356; }
.iv302-field small { color: #807c6f; font-size: 10px; }
.iv302-field-wide { grid-column: 1 / -1; }
.iv302-context { display: flex; gap: 12px; align-items: center; grid-column: 1 / -1; padding: 8px 10px; border: 1px solid #e0dccf; background: #f3f1e8; color: #5b584d; }
.iv302-components { padding: 8px; border: 1px solid #d5d1c4; background: #f8f7f1; }
.iv302-components-heading { justify-content: space-between; gap: 8px; margin-bottom: 7px; }
.iv302-components-heading > div:first-child { display: flex; flex-direction: column; }
.iv302-components-heading small { color: #777366; font-size: 10px; }
.iv302-components-heading > div:last-child { display: flex; gap: 5px; }
.iv302-component-pick-wrap { max-height: 180px; border: 1px solid #dfdccf; background: white; }
.iv302-info { padding: 7px 9px; border-left: 3px solid #b59b55; background: #f6f0dc; color: #625b45; font-size: 11px; }
.iv302-dialog-footer { justify-content: flex-end; gap: 7px; padding: 10px 14px; border-top: 1px solid #d7d3c7; background: #f3f1e9; }
.iv302-confirm-dialog > p { margin: 0; padding: 18px 16px; color: #575448; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0, 0, 0, 0); white-space: nowrap; border: 0; }
@media (max-width: 760px) {
  .iv302-page { padding: 8px; }
  .iv302-header { align-items: flex-start; flex-direction: column; }
  .iv302-search { flex-wrap: wrap; }
  .iv302-search-hint { width: 100%; margin-left: 0; }
  .iv302-panel-heading { align-items: flex-start; flex-direction: column; }
  .iv302-basis-panel { flex-basis: 300px; }
  .iv302-dialog-body { grid-template-columns: 1fr; }
  .iv302-field-wide,.iv302-context { grid-column: auto; }
  .iv302-footer { flex-direction: column; }
}
</style>
