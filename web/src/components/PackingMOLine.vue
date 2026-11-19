<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

type LevelNo = 1 | 2 | 3 | 4 | 5 | 6
type LevelRule = { level: LevelNo; name: string; enabled: boolean; maxQty: number | null; maxCount: number | null; labelTypes: string[] }
type Lot = { lotNo: string; productNo: string; productVersion: string; moNo: string; opNo: string; brNo: string; status: number; qty: number; availableQty: number; unitNo: string; componentCount: number; packed: boolean; splitFrom: string; inventoryNo?: string; fgdIn?: boolean }
type PackContent = { id: string; itemNo: string; itemType: 'LOT' | 'PACK'; itemLevel: number; packQty: number; unitNo: string }
type PackBox = { packingNo: string; level: LevelNo; isClosed: boolean; isMerged: boolean; dismantled: boolean; parentPackingNo: string; contents: PackContent[]; qty: number; createdAt: string }
type Accessory = { barcode: string; name: string }
type PrintJob = { id: string; packingNo: string; packingLevel: number; labelType: string; labelName: string; printerName: string; path: string; result: string; printCount: number }
type Snapshot = { version: 1; computerName: string; pdLineNo: string; moNo: string; productNo: string; productVersion: string; packingRuleNo: string; packingRuleVersion: string; levels: LevelRule[]; lots: Lot[]; boxes: PackBox[]; accessories: Accessory[]; printers: Record<string, string>; printJobs: PrintJob[]; nextPackageSeq: number; nextRowSeq: number; logs: string[] }

const props = defineProps<{ currentUserNo?: string; developmentPreview?: boolean; inventoryMode?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const inventoryMode = computed(() => props.inventoryMode === true)
const sampleMoNo = inventoryMode.value ? 'MO-26-00629' : 'MO-26-00601'
const sampleProductNo = inventoryMode.value ? 'PRD-0062901' : 'PRD-0060101'
const storageKey = inventoryMode.value ? 'ymes.dev.wp629-packing-moline-inv.v1' : 'ymes.dev.wp601-packing-moline.v1'
const printerChoices = ['DEV-LABEL-01', 'DEV-LABEL-02', 'DEV-ZEBRA-03']

function seedSnapshot(): Snapshot {
  const levels: LevelRule[] = [
    { level: 1, name: '內盒', enabled: true, maxQty: 12, maxCount: 4, labelTypes: ['L1_T1', 'L1_T2'] },
    { level: 2, name: '外箱', enabled: true, maxQty: 60, maxCount: 5, labelTypes: ['L2_T1'] },
    { level: 3, name: '棧板', enabled: false, maxQty: 300, maxCount: 5, labelTypes: ['L3_T1'] },
    { level: 4, name: 'L4', enabled: false, maxQty: null, maxCount: null, labelTypes: [] },
    { level: 5, name: 'L5', enabled: false, maxQty: null, maxCount: null, labelTypes: [] },
    { level: 6, name: 'L6', enabled: false, maxQty: null, maxCount: null, labelTypes: [] },
  ]
  if (inventoryMode.value) {
    const lots: Lot[] = [
      { lotNo: 'LOT-629-0001', productNo: sampleProductNo, productVersion: 'A01', moNo: sampleMoNo, opNo: 'WIP-INV', brNo: 'L_CO_PACKING_MOLINE_INV', status: 11, qty: 4, availableQty: 0, unitNo: 'PCS', componentCount: 0, packed: true, splitFrom: '', inventoryNo: 'WIP-INV-629-01' },
      { lotNo: 'LOT-629-0002', productNo: sampleProductNo, productVersion: 'A01', moNo: sampleMoNo, opNo: 'WIP-INV', brNo: 'L_CO_PACKING_MOLINE_INV', status: 11, qty: 4, availableQty: 0, unitNo: 'PCS', componentCount: 0, packed: true, splitFrom: '', inventoryNo: 'WIP-INV-629-01' },
      { lotNo: 'LOT-629-0003', productNo: sampleProductNo, productVersion: 'A01', moNo: sampleMoNo, opNo: 'WIP-INV', brNo: 'L_CO_PACKING_MOLINE_INV', status: 11, qty: 6, availableQty: 6, unitNo: 'PCS', componentCount: 0, packed: false, splitFrom: '', inventoryNo: 'WIP-INV-629-01' },
      { lotNo: 'LOT-629-0004', productNo: sampleProductNo, productVersion: 'A01', moNo: sampleMoNo, opNo: 'WIP-INV', brNo: 'L_CO_PACKING_MOLINE_INV', status: 11, qty: 8, availableQty: 8, unitNo: 'PCS', componentCount: 0, packed: false, splitFrom: '', inventoryNo: 'WIP-INV-629-01' },
      { lotNo: 'LOT-629-0005', productNo: sampleProductNo, productVersion: 'A01', moNo: sampleMoNo, opNo: 'WIP-INV', brNo: 'L_CO_PACKING_MOLINE_INV', status: 11, qty: 5, availableQty: 5, unitNo: 'PCS', componentCount: 0, packed: false, splitFrom: '', inventoryNo: 'WIP-INV-629-02' },
      { lotNo: 'LOT-629-0006', productNo: sampleProductNo, productVersion: 'A01', moNo: sampleMoNo, opNo: 'WIP-INV', brNo: 'L_CO_PACKING_MOLINE_INV', status: 11, qty: 3, availableQty: 3, unitNo: 'PCS', componentCount: 0, packed: false, splitFrom: '', inventoryNo: 'WIP-INV-629-01', fgdIn: true },
      { lotNo: 'LOT-629-0099', productNo: sampleProductNo, productVersion: 'A01', moNo: sampleMoNo, opNo: 'OP-PACK', brNo: 'L_CO_PACKING_MOLINE', status: 1, qty: 5, availableQty: 5, unitNo: 'PCS', componentCount: 0, packed: false, splitFrom: '', inventoryNo: '' },
    ]
    const boxes: PackBox[] = [
      { packingNo: 'PK-L1-629001', level: 1, isClosed: true, isMerged: false, dismantled: false, parentPackingNo: 'PK-L2-629001', contents: [{ id: 'C-629-1', itemNo: 'LOT-629-0001', itemType: 'LOT', itemLevel: 0, packQty: 4, unitNo: 'PCS' }], qty: 4, createdAt: '2026/11/19 10:04:12' },
      { packingNo: 'PK-L1-629002', level: 1, isClosed: false, isMerged: false, dismantled: false, parentPackingNo: '', contents: [{ id: 'C-629-2', itemNo: 'LOT-629-0002', itemType: 'LOT', itemLevel: 0, packQty: 4, unitNo: 'PCS' }], qty: 4, createdAt: '2026/11/19 10:12:35' },
      { packingNo: 'PK-L2-629001', level: 2, isClosed: false, isMerged: false, dismantled: false, parentPackingNo: '', contents: [{ id: 'C-629-3', itemNo: 'PK-L1-629001', itemType: 'PACK', itemLevel: 1, packQty: 4, unitNo: 'PCS' }], qty: 4, createdAt: '2026/11/19 10:05:01' },
    ]
    return {
      version: 1, computerName: 'DEV-PACK-629', pdLineNo: 'LINE-WIP-INV-01', moNo: sampleMoNo, productNo: sampleProductNo, productVersion: 'A01',
      packingRuleNo: 'PKR-00629', packingRuleVersion: '01', levels, lots, boxes,
      accessories: [{ barcode: 'ACC-ESD-00629', name: '防靜電袋' }, { barcode: 'ACC-LABEL-00629', name: '產品識別標籤' }],
      printers: { L1_T1: 'DEV-LABEL-01', L1_T2: 'DEV-LABEL-02', L2_T1: 'DEV-ZEBRA-03' }, printJobs: [], nextPackageSeq: 3, nextRowSeq: 4,
      logs: ['10:12:35　LOT-629-0002 已加入 PK-L1-629002', '10:05:01　PK-L1-629001 已加入 PK-L2-629001', '10:04:12　PK-L1-629001 封箱完成'],
    }
  }
  const lots: Lot[] = [
    { lotNo: 'LOT-601-0001', productNo: 'PRD-0060101', productVersion: 'A01', moNo: 'MO-26-00601', opNo: 'OP-PACK', brNo: 'L_CO_PACKING_MOLINE', status: 1, qty: 4, availableQty: 0, unitNo: 'PCS', componentCount: 0, packed: true, splitFrom: '' },
    { lotNo: 'LOT-601-0002', productNo: 'PRD-0060101', productVersion: 'A01', moNo: 'MO-26-00601', opNo: 'OP-PACK', brNo: 'L_CO_PACKING_MOLINE', status: 1, qty: 4, availableQty: 0, unitNo: 'PCS', componentCount: 0, packed: true, splitFrom: '' },
    { lotNo: 'LOT-601-0003', productNo: 'PRD-0060101', productVersion: 'A01', moNo: 'MO-26-00601', opNo: 'OP-PACK', brNo: 'L_CO_PACKING_MOLINE', status: 1, qty: 6, availableQty: 0, unitNo: 'PCS', componentCount: 0, packed: true, splitFrom: '' },
    { lotNo: 'LOT-601-0004', productNo: 'PRD-0060101', productVersion: 'A01', moNo: 'MO-26-00601', opNo: 'OP-PACK', brNo: 'L_CO_PACKING_MOLINE', status: 1, qty: 8, availableQty: 8, unitNo: 'PCS', componentCount: 0, packed: false, splitFrom: '' },
    { lotNo: 'LOT-601-0005', productNo: 'PRD-0060101', productVersion: 'A01', moNo: 'MO-26-00601', opNo: 'OP-PACK', brNo: 'L_CO_PACKING_MOLINE', status: 1, qty: 16, availableQty: 16, unitNo: 'PCS', componentCount: 2, packed: false, splitFrom: '' },
    { lotNo: 'LOT-601-0006', productNo: 'PRD-0060101', productVersion: 'A01', moNo: 'MO-26-00601', opNo: 'OP-PACK', brNo: 'L_CO_PACKING_MOLINE', status: 1, qty: 5, availableQty: 5, unitNo: 'PCS', componentCount: 0, packed: false, splitFrom: '' },
    { lotNo: 'LOT-601-0099', productNo: 'PRD-0060101', productVersion: 'A01', moNo: 'MO-26-00601', opNo: 'OP-TEST', brNo: 'L_CO_TEST', status: 1, qty: 5, availableQty: 5, unitNo: 'PCS', componentCount: 0, packed: false, splitFrom: '' },
  ]
  const boxes: PackBox[] = [
    { packingNo: 'PK-L1-000101', level: 1, isClosed: true, isMerged: false, dismantled: false, parentPackingNo: 'PK-L2-000024', contents: [{ id: 'C-101-1', itemNo: 'LOT-601-0001', itemType: 'LOT', itemLevel: 0, packQty: 4, unitNo: 'PCS' }, { id: 'C-101-2', itemNo: 'LOT-601-0002', itemType: 'LOT', itemLevel: 0, packQty: 4, unitNo: 'PCS' }], qty: 8, createdAt: '2026/11/19 09:24:10' },
    { packingNo: 'PK-L1-000102', level: 1, isClosed: false, isMerged: false, dismantled: false, parentPackingNo: '', contents: [{ id: 'C-102-1', itemNo: 'LOT-601-0003', itemType: 'LOT', itemLevel: 0, packQty: 6, unitNo: 'PCS' }], qty: 6, createdAt: '2026/11/19 09:31:48' },
    { packingNo: 'PK-L2-000024', level: 2, isClosed: false, isMerged: false, dismantled: false, parentPackingNo: '', contents: [{ id: 'C-24-1', itemNo: 'PK-L1-000101', itemType: 'PACK', itemLevel: 1, packQty: 8, unitNo: 'PCS' }], qty: 8, createdAt: '2026/11/19 09:25:02' },
  ]
  return {
    version: 1, computerName: 'DEV-PACK-02', pdLineNo: 'LINE-PACK-02', moNo: 'MO-26-00601', productNo: 'PRD-0060101', productVersion: 'A01',
    packingRuleNo: 'PKR-00601', packingRuleVersion: '02', levels, lots, boxes,
    accessories: [{ barcode: 'ACC-ESD-00601', name: '防靜電袋' }, { barcode: 'ACC-LABEL-00601', name: '產品識別標籤' }],
    printers: { L1_T1: 'DEV-LABEL-01', L1_T2: 'DEV-LABEL-02', L2_T1: 'DEV-ZEBRA-03' }, printJobs: [], nextPackageSeq: 103, nextRowSeq: 10,
    logs: ['09:31:48　LOT-601-0003 已加入 PK-L1-000102', '09:25:02　PK-L1-000101 已加入 PK-L2-000024', '09:24:10　PK-L1-000101 封箱完成'],
  }
}

function emptySnapshot(): Snapshot {
  return { version: 1, computerName: '', pdLineNo: '', moNo: '', productNo: '', productVersion: '', packingRuleNo: '', packingRuleVersion: '', levels: [], lots: [], boxes: [], accessories: [], printers: {}, printJobs: [], nextPackageSeq: 1, nextRowSeq: 1, logs: [] }
}
function loadSnapshot(): Snapshot {
  if (!dev.value) return emptySnapshot()
  try {
    const raw = localStorage.getItem(storageKey)
    if (raw) {
      const saved = JSON.parse(raw) as Snapshot
      if (saved.version === 1 && saved.moNo === sampleMoNo && Array.isArray(saved.levels) && Array.isArray(saved.boxes)) return saved
    }
  } catch { /* use the development seed */ }
  return seedSnapshot()
}

const state = reactive<Snapshot>(loadSnapshot())
const activeLevel = ref<LevelNo>(1)
const lotBarcode = ref('')
const accessoryBarcode = ref('')
const packQtyInput = ref('')
const pendingScan = ref<{ type: 'LOT' | 'PACK'; itemNo: string; qty: number } | null>(null)
const checkedAccessories = ref<string[]>([])
const selectedContentId = ref('')
const actionMessage = ref('')
const actionError = ref('')
const packConfigOpen = ref(false)
const configMoNo = ref('')
const configLevels = ref<number[]>([])
const configPrinters = ref<Record<string, string>>({})
const operationDialog = ref<'combine' | 'unboxing' | null>(null)
const operationSourceNo = ref('')
const operationTargetNo = ref('')
const pendingAutoCloseLevel = ref<LevelNo | null>(null)
const pendingTransferPackingNo = ref('')
const pendingTransferTargetLevel = ref<LevelNo | null>(null)

const enabledLevels = computed(() => state.levels.filter(level => level.enabled).sort((a, b) => a.level - b.level))
const minLevel = computed(() => enabledLevels.value[0]?.level ?? 0)
const maxLevel = computed(() => enabledLevels.value[enabledLevels.value.length - 1]?.level ?? 0)
const selectedRule = computed(() => state.levels.find(level => level.level === activeLevel.value))
const openBox = computed(() => state.boxes.find(box => box.level === activeLevel.value && !box.isClosed && !box.isMerged && !box.dismantled))
const currentContents = computed(() => openBox.value?.contents ?? [])
const currentLot = computed(() => pendingScan.value?.type === 'LOT' ? state.lots.find(lot => lot.lotNo === pendingScan.value?.itemNo) : undefined)
const currentInventoryNo = computed(() => {
  if (pendingScan.value?.type === 'LOT') return lotFor(pendingScan.value.itemNo)?.inventoryNo ?? ''
  if (pendingScan.value?.type === 'PACK') return inventoryNoForBox(boxFor(pendingScan.value.itemNo))
  return inventoryNoForBox(openBox.value)
})
const scannedAccessoryRows = computed(() => state.accessories.map(item => ({ ...item, checked: checkedAccessories.value.includes(item.barcode) })))
const printJobs = computed(() => state.printJobs.slice().reverse())
const usableClosedBoxes = computed(() => state.boxes.filter(box => box.isClosed && !box.isMerged && !box.dismantled))
const openBoxes = computed(() => state.boxes.filter(box => !box.isClosed && !box.isMerged && !box.dismantled))
const selectedClosedBox = computed(() => state.boxes.find(box => box.packingNo === operationSourceNo.value && box.isClosed && !box.isMerged && !box.dismantled))
const selectedTargetBox = computed(() => state.boxes.find(box => box.packingNo === operationTargetNo.value && !box.isClosed && !box.isMerged && !box.dismantled))

function persist(): void { if (dev.value) localStorage.setItem(storageKey, JSON.stringify(state)) }
function clearFeedback(): void { actionMessage.value = ''; actionError.value = '' }
function formatQty(value: number): string { return Number(value).toLocaleString(undefined, { maximumFractionDigits: 3 }) }
function levelName(level: number): string { return state.levels.find(item => item.level === level)?.name ?? ('L' + level) }
function boxFor(no: string): PackBox | undefined { return state.boxes.find(box => box.packingNo === no) }
function lotFor(no: string): Lot | undefined { return state.lots.find(lot => lot.lotNo === no) }
function inventoryNoForBox(box: PackBox | undefined): string {
  if (!box) return ''
  const lotNos = collectLotNos(box)
  if (!lotNos.length) return ''
  const inventoryNosForLots = lotNos.map(no => lotFor(no)?.inventoryNo ?? '')
  if (inventoryNosForLots.some(inventoryNo => !inventoryNo)) return ''
  const inventoryNos = [...new Set(inventoryNosForLots)]
  return inventoryNos.length === 1 ? inventoryNos[0] : ''
}
function nextPackageNo(level: number): string {
  const value = String(state.nextPackageSeq++).padStart(6, '0')
  return 'PK-L' + level + '-' + value
}
function createBox(level: LevelNo): PackBox {
  const box: PackBox = { packingNo: nextPackageNo(level), level, isClosed: false, isMerged: false, dismantled: false, parentPackingNo: '', contents: [], qty: 0, createdAt: new Date().toLocaleString() }
  state.boxes.unshift(box)
  return box
}
function getOrCreateOpenBox(level: LevelNo): PackBox {
  return state.boxes.find(box => box.level === level && !box.isClosed && !box.isMerged && !box.dismantled) ?? createBox(level)
}
function contentCount(box: PackBox): number { return box.contents.length }
function limitStatus(box: PackBox, rule: LevelRule): string {
  const countText = rule.maxCount === null ? '不限' : formatQty(box.contents.length) + '/' + formatQty(rule.maxCount)
  const qtyText = rule.maxQty === null ? '不限' : formatQty(box.qty) + '/' + formatQty(rule.maxQty)
  return countText + ' 件 · ' + qtyText + ' ' + (box.contents[0]?.unitNo ?? 'PCS')
}
function isFull(box: PackBox, rule: LevelRule): boolean {
  const countOk = rule.maxCount === null || box.contents.length >= rule.maxCount
  const qtyOk = rule.maxQty === null || box.qty >= rule.maxQty
  return countOk && qtyOk
}
function isOverLimit(box: PackBox, rule: LevelRule, addQty: number): boolean {
  return (rule.maxCount !== null && box.contents.length + 1 > rule.maxCount) || (rule.maxQty !== null && box.qty + addQty > rule.maxQty)
}
function selectedPackageIsInParent(no: string): boolean { return state.boxes.some(box => box.contents.some(item => item.itemType === 'PACK' && item.itemNo === no)) }
function log(message: string): void {
  state.logs.unshift(new Date().toLocaleTimeString() + '　' + message)
  state.logs = state.logs.slice(0, 30)
}
function setActiveLevel(level: number): void {
  if (level < 1 || level > 6) return
  activeLevel.value = level as LevelNo
  selectedContentId.value = ''
  clearFeedback()
}

function openConfig(): void {
  clearFeedback()
  configMoNo.value = state.moNo || sampleMoNo
  configLevels.value = state.levels.filter(item => item.enabled).map(item => item.level)
  configPrinters.value = { ...state.printers }
  packConfigOpen.value = true
}
function toggleConfigLevel(level: number): void {
  const selected = new Set(configLevels.value)
  if (selected.has(level)) selected.delete(level)
  else selected.add(level)
  configLevels.value = [...selected].sort((a, b) => a - b)
}
function saveConfig(): void {
  const moNo = configMoNo.value.trim().toUpperCase()
  if (moNo !== sampleMoNo) { actionError.value = 'DEV 工單查無資料，請選 ' + sampleMoNo + '。'; return }
  if (!configLevels.value.length) { actionError.value = '至少選取一個包裝階層。'; return }
  const first = Math.min(...configLevels.value)
  const last = Math.max(...configLevels.value)
  if (Array.from({ length: last - first + 1 }, (_, index) => first + index).some(level => !configLevels.value.includes(level))) { actionError.value = '可包裝階層必須連續。'; return }
  const openWork = state.boxes.some(box => !box.isClosed && !box.isMerged && !box.dismantled && box.contents.length > 0)
  if (openWork && (moNo !== state.moNo || configLevels.value.join(',') !== enabledLevels.value.map(level => level.level).join(','))) { actionError.value = '目前有未封箱內容；需先完成或封箱，再更換工單／階層。'; return }
  state.moNo = moNo
  state.pdLineNo = inventoryMode.value ? 'LINE-WIP-INV-01' : 'LINE-PACK-02'
  state.productNo = sampleProductNo
  state.productVersion = 'A01'
  state.levels.forEach(level => { level.enabled = configLevels.value.includes(level.level) })
  state.printers = { ...configPrinters.value }
  activeLevel.value = minLevel.value || 1
  persist()
  packConfigOpen.value = false
  actionMessage.value = '已儲存本機 DEV 包裝設定；正式設定由 API 維護。'
}

function scanBarcode(): void {
  clearFeedback()
  const code = lotBarcode.value.trim().toUpperCase()
  lotBarcode.value = code
  pendingScan.value = null
  checkedAccessories.value = []
  if (!dev.value) return
  if (!state.moNo || !enabledLevels.value.length) { actionError.value = '請先執行 Pack Config。'; openConfig(); return }
  if (!code) { actionError.value = '請輸入 LotNo 或 PackingNo。'; return }
  const lot = lotFor(code)
  if (lot) {
    if (inventoryMode.value && lot.fgdIn) { actionError.value = '此 Lot 已有成品入庫關聯，禁止重複包裝。'; return }
    if (minLevel.value !== 1) { actionError.value = '目前最小包裝階層為 L' + minLevel.value + '，請刷入已封箱的 L' + (minLevel.value - 1) + ' 包裝編號。'; return }
    if (lot.status !== (inventoryMode.value ? 11 : 1)) { actionError.value = inventoryMode.value ? 'Lot 狀態不是 WIP Inventory（Status=11），禁止此功能包裝。' : 'Lot 狀態不是 Running，禁止包裝。'; return }
    if (!inventoryMode.value && lot.brNo.toUpperCase() !== 'L_CO_PACKING_MOLINE') { actionError.value = 'Lot 所在 BR 不是 L_CO_PACKING_MOLINE。'; return }
    if (inventoryMode.value && !lot.inventoryNo) { actionError.value = '此 Lot 查無 WIP InventoryNo。'; return }
    if (lot.moNo !== state.moNo || lot.productNo !== state.productNo || lot.productVersion !== state.productVersion) { actionError.value = 'Lot 的工單／產品／版本與目前包裝設定不一致。'; return }
    if (lot.packed || lot.availableQty <= 0) { actionError.value = '此 Lot 已加入包裝關係，不能重複包裝。'; return }
    const existingOpen = state.boxes.find(box => box.level === minLevel.value && !box.isClosed && !box.isMerged && !box.dismantled)
    if (inventoryMode.value && existingOpen?.contents.length && inventoryNoForBox(existingOpen) !== lot.inventoryNo) { actionError.value = '目前未封箱包裝的 WIP InventoryNo 不同，不可混庫包裝。'; return }
    const current = getOrCreateOpenBox(1)
    if (current.contents.some(item => item.itemType === 'LOT' && item.itemNo === lot.lotNo)) { actionError.value = '同一 L1 箱內不可重複加入相同 Lot。'; return }
    pendingScan.value = { type: 'LOT', itemNo: lot.lotNo, qty: lot.availableQty }
    packQtyInput.value = String(lot.availableQty)
    actionMessage.value = '已載入 Lot 資料；確認數量後包裝，若有配件需求請先掃描配件條碼。'
    return
  }
  const box = boxFor(code)
  if (!box) { actionError.value = '查無此 LotNo／PackingNo。'; return }
  if (inventoryMode.value && collectLotNos(box).some(no => lotFor(no)?.fgdIn)) { actionError.value = '此包裝內 Lot 已有成品入庫關聯，禁止拆併或重複包裝。'; return }
  if (!box.isClosed || box.isMerged || box.dismantled) { actionError.value = '只能掃描有效且已封箱的包裝編號。'; return }
  if (box.level !== minLevel.value - 1) { actionError.value = '此包裝階層不能直接加入目前設定的最小階層。'; return }
  if (selectedPackageIsInParent(box.packingNo)) { actionError.value = '此包裝已加入其他外層包裝，不能重複刷入。'; return }
  if (inventoryMode.value) {
    const lotNos = collectLotNos(box)
    const sourceLots = lotNos.map(no => lotFor(no))
    const inventoryNo = inventoryNoForBox(box)
    if (!sourceLots.length || sourceLots.some(item => !item || item.status !== 11 || !item.inventoryNo) || !inventoryNo) { actionError.value = '包裝內所有 Lot 必須仍在 WIP Inventory（Status=11），且庫位一致。'; return }
    const existingOpen = state.boxes.find(item => item.level === minLevel.value && !item.isClosed && !item.isMerged && !item.dismantled)
    if (existingOpen?.contents.length && inventoryNoForBox(existingOpen) !== inventoryNo) { actionError.value = '目前未封箱包裝的 WIP InventoryNo 不同，不可混庫包裝。'; return }
  }
  pendingScan.value = { type: 'PACK', itemNo: box.packingNo, qty: box.qty }
  packQtyInput.value = '1'
  actionMessage.value = '已載入封箱資料；按「包裝」加入目前階層。'
}

function scanAccessory(): void {
  clearFeedback()
  const barcode = accessoryBarcode.value.trim().toUpperCase()
  accessoryBarcode.value = ''
  if (!pendingScan.value) { actionError.value = '請先刷入 LotNo。'; return }
  const accessory = state.accessories.find(item => item.barcode === barcode)
  if (!accessory) { actionError.value = '配件條碼不在產品包裝配件設定中。'; return }
  if (checkedAccessories.value.includes(barcode)) { actionError.value = '此配件條碼已確認。'; return }
  checkedAccessories.value.push(barcode)
  actionMessage.value = '配件確認：' + accessory.name
  if (checkedAccessories.value.length === state.accessories.length) submitPending()
}

function submitPending(): void {
  clearFeedback()
  const pending = pendingScan.value
  if (!dev.value || !pending) { actionError.value = '請先刷入有效 LotNo／PackingNo。'; return }
  const level = minLevel.value as LevelNo
  if (pending.type === 'LOT') {
    const lot = lotFor(pending.itemNo)
    const qty = Number(packQtyInput.value)
    if (!lot || !Number.isFinite(qty) || qty <= 0 || qty > lot.availableQty) { actionError.value = 'PackQty 必須大於 0 且不可超過 Lot 可包數量。'; return }
    if (inventoryMode.value && lot.fgdIn) { actionError.value = '此 Lot 已有成品入庫關聯，禁止重複包裝。'; return }
    if (inventoryMode.value && (lot.status !== 11 || !lot.inventoryNo)) { actionError.value = 'Lot 已離開 WIP Inventory 或查無庫位，請重新刷入。'; return }
    if (state.accessories.length && checkedAccessories.value.length !== state.accessories.length) { actionError.value = '此產品需先掃描完成所有 Pack Accessory。'; return }
    const box = getOrCreateOpenBox(level)
    const rule = state.levels.find(item => item.level === level)!
    if (box.contents.length && isFull(box, rule)) { actionError.value = '目前包裝已達滿箱，請先 Pack Close。'; return }
    let packQty = qty
    if (rule.maxQty !== null) {
      const remaining = rule.maxQty - box.qty
      if (packQty > remaining) {
        if (lot.componentCount > 0) { actionError.value = 'Lot 含 Component，WinForms 不允許以此流程拆批包裝。'; return }
        if (remaining <= 0) { actionError.value = '此包裝數量已滿，請先封箱。'; return }
        packQty = remaining
      }
    }
    if (rule.maxCount !== null && box.contents.length + 1 > rule.maxCount) { actionError.value = '目前包裝箱數已達上限，請先封箱。'; return }
    if (qty < lot.availableQty && lot.componentCount > 0) { actionError.value = 'Lot 含 Component，不允許部分數量拆批。'; return }
    const remainder = lot.availableQty - packQty
    if (remainder > 0) {
      if (lot.componentCount > 0) { actionError.value = 'Lot 含 Component，不能自動拆批。'; return }
      lot.availableQty = 0
      lot.packed = true
      const splitNo = lot.lotNo + '-S' + String(state.nextRowSeq++).padStart(2, '0')
      state.lots.push({ ...lot, lotNo: splitNo, qty: remainder, availableQty: remainder, packed: false, splitFrom: lot.lotNo })
      log(lot.lotNo + ' DEV 拆批 ' + formatQty(packQty) + '/' + formatQty(qty) + '，餘量建立 ' + splitNo)
    } else {
      lot.availableQty = 0
      lot.packed = true
    }
    box.contents.push({ id: 'C-' + state.nextRowSeq++, itemNo: lot.lotNo, itemType: 'LOT', itemLevel: 0, packQty, unitNo: lot.unitNo })
    box.qty += packQty
    log(lot.lotNo + ' 已加入 ' + box.packingNo + '，PackQty=' + formatQty(packQty))
    persist()
    pendingScan.value = null
    checkedAccessories.value = []
    lotBarcode.value = ''
    packQtyInput.value = ''
    handleFullBox(level)
    return
  }
  const sourceBox = boxFor(pending.itemNo)
  if (!sourceBox || !sourceBox.isClosed || selectedPackageIsInParent(sourceBox.packingNo)) { actionError.value = '來源包裝狀態已變更或已被其他外箱使用，請重新刷入。'; return }
  if (inventoryMode.value && collectLotNos(sourceBox).some(no => lotFor(no)?.fgdIn)) { actionError.value = '此包裝內 Lot 已有成品入庫關聯，禁止拆併或重複包裝。'; return }
  if (inventoryMode.value && (!inventoryNoForBox(sourceBox) || collectLotNos(sourceBox).some(no => lotFor(no)?.status !== 11))) { actionError.value = '包裝內 Lot 已離開 WIP Inventory 或庫位資料不一致，請重新刷入。'; return }
  const target = getOrCreateOpenBox(level)
  if (inventoryMode.value && target.contents.length && inventoryNoForBox(target) !== inventoryNoForBox(sourceBox)) { actionError.value = '目前未封箱包裝的 WIP InventoryNo 不同，不可混庫包裝。'; return }
  const rule = state.levels.find(item => item.level === level)!
  if (target.contents.length && isFull(target, rule)) { actionError.value = '目前外箱已滿，請先封箱。'; return }
  if (isOverLimit(target, rule, sourceBox.qty)) { actionError.value = '來源包裝會超過目前階層滿箱條件，請先封閉目前外箱。'; return }
  target.contents.push({ id: 'C-' + state.nextRowSeq++, itemNo: sourceBox.packingNo, itemType: 'PACK', itemLevel: sourceBox.level, packQty: sourceBox.qty, unitNo: 'PCS' })
  target.qty += sourceBox.qty
  sourceBox.parentPackingNo = target.packingNo
  log(sourceBox.packingNo + ' 已加入 ' + target.packingNo)
  persist()
  pendingScan.value = null
  lotBarcode.value = ''
  packQtyInput.value = ''
  handleFullBox(level)
}

function handleFullBox(level: LevelNo): void {
  const box = state.boxes.find(item => item.level === level && !item.isClosed && !item.isMerged && !item.dismantled)
  const rule = state.levels.find(item => item.level === level)
  if (!box || !rule || !box.contents.length || !isFull(box, rule)) return
  if (autoClose.value) closeBox(level, true)
  else pendingAutoCloseLevel.value = level
}
const autoClose = ref(true)
function collectLotNos(box: PackBox): string[] {
  return box.contents.flatMap(item => item.itemType === 'LOT' ? [item.itemNo] : (boxFor(item.itemNo) ? collectLotNos(boxFor(item.itemNo)!) : []))
}
function transferClosedBoxToParent(box: PackBox, forceCloseParent = false): void {
  const next = enabledLevels.value.find(item => item.level > box.level)
  if (!next) { actionMessage.value = box.packingNo + ' 封箱完成。'; return }
  let target = state.boxes.find(item => item.level === next.level && !item.isClosed && !item.isMerged && !item.dismantled)
  const parentRule = state.levels.find(item => item.level === next.level)!
  if (!target) target = createBox(next.level)
  if (inventoryMode.value && target.contents.length && inventoryNoForBox(target) !== inventoryNoForBox(box)) { actionError.value = '內外層包裝的 WIP InventoryNo 不同，禁止轉入。'; return }
  if (isOverLimit(target, parentRule, box.qty)) {
    if (!target.contents.length) { actionError.value = '單一包裝已超過外層滿箱限制，無法轉入 ' + levelName(next.level) + '。'; return }
    if (!autoClose.value && !forceCloseParent) {
      pendingTransferPackingNo.value = box.packingNo
      pendingTransferTargetLevel.value = next.level
      pendingAutoCloseLevel.value = next.level
      actionMessage.value = '外層 ' + target.packingNo + ' 容量不足；封閉外層後才會轉入 ' + box.packingNo + '。'
      return
    }
    closeBox(next.level, true, true)
    target = getOrCreateOpenBox(next.level)
    if (isOverLimit(target, parentRule, box.qty)) { actionError.value = '新開的 ' + levelName(next.level) + ' 仍無法容納此包裝。'; return }
  }
  target.contents.push({ id: 'C-' + state.nextRowSeq++, itemNo: box.packingNo, itemType: 'PACK', itemLevel: box.level, packQty: box.qty, unitNo: 'PCS' })
  target.qty += box.qty
  box.parentPackingNo = target.packingNo
  activeLevel.value = next.level
  if (!actionMessage.value) actionMessage.value = box.packingNo + ' 已封箱並加入 ' + target.packingNo + '。'
  handleFullBox(next.level)
}
function closeBox(levelValue: number, automatic = false, forceCloseParent = false): void {
  clearFeedback()
  const level = levelValue as LevelNo
  const box = state.boxes.find(item => item.level === level && !item.isClosed && !item.isMerged && !item.dismantled)
  if (!box || !box.contents.length) { actionError.value = '選取階層沒有可封箱的內容。'; return }
  const minimum = minLevel.value
  if (level > minimum) {
    const lowerOpen = state.boxes.find(item => item.level === level - 1 && !item.isClosed && !item.isMerged && !item.dismantled && item.contents.length)
    if (lowerOpen) { actionError.value = '較低階層 ' + lowerOpen.packingNo + ' 尚未封箱，請先封閉較低階層。'; return }
  }
  const resumesWaitingChild = pendingTransferTargetLevel.value === level
  const waitingChildNo = resumesWaitingChild ? pendingTransferPackingNo.value : ''
  box.isClosed = true
  for (const labelType of state.levels.find(item => item.level === level)?.labelTypes ?? []) {
    state.printJobs.push({ id: 'P-' + state.nextRowSeq++, packingNo: box.packingNo, packingLevel: level, labelType, labelName: levelName(level) + '-' + labelType.slice(-2), printerName: state.printers[labelType] ?? '預設印表機', path: 'DEV 模擬標籤範本/' + labelType, result: '待模擬列印', printCount: 0 })
  }
  log((automatic ? '滿箱自動封箱 ' : '手動封箱 ') + box.packingNo)
  if (level === maxLevel.value) {
    if (!inventoryMode.value) {
      for (const lotNo of collectLotNos(box)) {
        const lot = lotFor(lotNo)
        if (lot) lot.status = 4
      }
      actionMessage.value = box.packingNo + ' 已封箱；DEV 模擬 MaxLevel 完工移轉。'
    } else {
      actionMessage.value = box.packingNo + ' 已封箱；在製品庫包裝不改變 Lot 的 Status=11 與庫存位置。'
    }
  } else {
    transferClosedBoxToParent(box, forceCloseParent || resumesWaitingChild)
  }
  if (waitingChildNo) {
    pendingTransferPackingNo.value = ''
    pendingTransferTargetLevel.value = null
    if (pendingAutoCloseLevel.value === level) pendingAutoCloseLevel.value = null
    const waitingChild = boxFor(waitingChildNo)
    if (waitingChild?.isClosed) transferClosedBoxToParent(waitingChild, true)
  }
  persist()
}
function confirmPendingAutoClose(): void {
  const level = pendingAutoCloseLevel.value
  if (level === null) return
  const waitingChildNo = pendingTransferTargetLevel.value === level ? pendingTransferPackingNo.value : ''
  pendingAutoCloseLevel.value = null
  if (waitingChildNo) {
    pendingTransferPackingNo.value = ''
    pendingTransferTargetLevel.value = null
  }
  closeBox(level, true, true)
  if (waitingChildNo) {
    const waitingChild = boxFor(waitingChildNo)
    if (waitingChild?.isClosed) transferClosedBoxToParent(waitingChild, true)
  }
}

function deleteContent(): void {
  clearFeedback()
  const box = openBox.value
  if (!box) { actionError.value = '沒有未封箱的包裝內容。'; return }
  const index = box.contents.findIndex(item => item.id === selectedContentId.value)
  if (index < 0) { actionError.value = '請先選取待移除的內容列。'; return }
  const [content] = box.contents.splice(index, 1)
  box.qty = Math.max(0, box.qty - content.packQty)
  if (content.itemType === 'LOT') {
    const lot = lotFor(content.itemNo)
    if (lot) { lot.availableQty += content.packQty; lot.packed = false }
  } else {
    const child = boxFor(content.itemNo)
    if (child) child.parentPackingNo = ''
  }
  selectedContentId.value = ''
  log(content.itemNo + ' 已從未封箱 ' + box.packingNo + ' 移出')
  persist()
  actionMessage.value = '已移除所選內容；正式資料需由 Core 交易回復包裝關係與狀態。'
}

function openOperation(mode: 'combine' | 'unboxing'): void {
  clearFeedback()
  operationDialog.value = mode
  operationSourceNo.value = ''
  operationTargetNo.value = ''
}
function confirmOperation(): void {
  clearFeedback()
  const source = selectedClosedBox.value
  if (!source) { actionError.value = '請選取來源已封箱包裝。'; return }
  if (operationDialog.value === 'combine') {
    const target = selectedTargetBox.value
    if (!target || target.level !== source.level) { actionError.value = '請選取同階層且未封箱的目標包裝。'; return }
    if (inventoryMode.value && target.contents.length && inventoryNoForBox(target) !== inventoryNoForBox(source)) { actionError.value = '來源與目標包裝的 WIP InventoryNo 不同，不可合併。'; return }
    const rule = state.levels.find(item => item.level === target.level)!
    if ((rule.maxQty !== null && target.qty + source.qty > rule.maxQty) || (rule.maxCount !== null && target.contents.length + source.contents.length > rule.maxCount)) { actionError.value = '合併後會超過目標包裝規則容量。'; return }
    target.contents.push(...source.contents.map(item => ({ ...item, id: 'C-' + state.nextRowSeq++ })))
    target.qty += source.qty
    source.isMerged = true
    source.parentPackingNo = target.packingNo
    log('DEV 模擬 Combine ' + source.packingNo + ' → ' + target.packingNo)
    persist()
    operationDialog.value = null
    actionMessage.value = '已完成 DEV 合併模擬。WinForms 透過 INVMOPackingCombineDismantle 外掛，正式合併規則仍需 API 端依 Core 原子化實作。'
    return
  }
  for (const content of source.contents) {
    if (content.itemType === 'LOT') {
      const lot = lotFor(content.itemNo)
      if (lot) { lot.availableQty += content.packQty; lot.packed = false; if (!inventoryMode.value && lot.status === 4) lot.status = 1 }
    } else {
      const child = boxFor(content.itemNo)
      if (child) child.parentPackingNo = ''
    }
  }
  source.dismantled = true
  log('DEV 模擬 Unboxing ' + source.packingNo)
  persist()
  operationDialog.value = null
  actionMessage.value = inventoryMode.value ? '已完成 DEV 拆箱模擬；Lot 保持 WIP Inventory Status=11。正式拆併箱由外掛與 Core 處理。' : '已完成 DEV 拆箱模擬。正式拆併箱由 INVMOPackingCombineDismantle 外掛與 Core 處理。'
}
function simulatePrint(job: PrintJob): void {
  clearFeedback()
  job.printCount += 1
  job.result = 'DEV 模擬列印完成'
  persist()
  actionMessage.value = job.packingNo + ' / ' + job.labelType + '：已記錄模擬列印，未呼叫本機印表機。'
}
function resetDevData(): void {
  if (!dev.value) return
  Object.assign(state, seedSnapshot())
  localStorage.setItem(storageKey, JSON.stringify(state))
  activeLevel.value = 1
  pendingScan.value = null
  pendingAutoCloseLevel.value = null
  pendingTransferPackingNo.value = ''
  pendingTransferTargetLevel.value = null
  clearFeedback()
  actionMessage.value = '已還原 ' + (inventoryMode.value ? 'WP629' : 'WP601') + ' DEV 假資料。'
}
</script>

<template>
  <section class="packing-page">
    <header class="page-heading">
      <div><span class="function-code">{{ inventoryMode ? 'WP629 · PACKING_MOLINE_INV' : 'WP601 · PACKING_MOLINE' }}</span><h1>{{ inventoryMode ? '在製品庫包裝作業' : '包裝作業' }}</h1></div>
      <div class="heading-context"><span>設備 {{ state.computerName || '未設定' }}</span><span>線別 {{ state.pdLineNo || '—' }}</span><span>狀態 {{ dev ? 'DEV 假資料' : 'Production API 尚未接入' }}</span></div>
    </header>

    <fieldset class="barcode-panel form-panel">
      <legend>BarCode</legend>
      <div class="barcode-grid">
        <label class="scan-field"><span>LotNo</span><input v-model="lotBarcode" :disabled="!dev" autofocus autocomplete="off" @keydown.enter.prevent="scanBarcode"></label>
        <label><span>Cur_LotQty</span><input v-model="packQtyInput" :disabled="!dev || !pendingScan || pendingScan.type !== 'LOT'" inputmode="decimal" @keydown.enter.prevent="submitPending"></label>
        <label><span>{{ inventoryMode ? 'WIP INV NO' : 'OPNo' }}</span><input :value="inventoryMode ? (currentInventoryNo || '—') : (currentLot?.opNo || (dev ? 'OP-PACK' : ''))" readonly></label>
        <label><span>MONo</span><input :value="state.moNo" readonly></label>
        <label class="accessory-scan"><span>Accessory BarCode</span><input v-model="accessoryBarcode" :disabled="!dev || !pendingScan || !state.accessories.length" autocomplete="off" @keydown.enter.prevent="scanAccessory"></label>
        <label><span>ProductNo</span><input :value="state.productNo" readonly></label>
        <label><span>ProductVersion</span><input :value="state.productVersion" readonly></label>
        <label><span>PackingLevelName</span><input :value="levelName(activeLevel)" readonly></label>
        <label><span>PackingNo</span><input :value="openBox?.packingNo || ''" readonly></label>
        <label><span>Unit</span><input :value="currentLot?.unitNo || (dev ? 'PCS' : '')" readonly></label>
        <label class="auto-close"><input v-model="autoClose" type="checkbox" :disabled="!dev"><span>Full Boxe Auto Closed</span></label>
        <button type="button" :disabled="!dev" @click="openConfig">Pack Config</button>
        <button v-if="pendingScan" type="button" class="primary-action" :disabled="!dev" @click="submitPending">包裝</button>
      </div>
    </fieldset>

    <main class="packing-workspace">
      <div class="left-workspace">
        <section class="grid-panel level-panel">
          <div class="panel-title"><h2>Pack Set Content</h2><span>工單 {{ state.moNo || '未設定' }} · {{ state.packingRuleNo }} / {{ state.packingRuleVersion }}</span></div>
          <div class="table-wrap"><table class="packing-grid"><thead><tr><th>Level</th><th>PackingLevelName</th><th>滿箱箱數</th><th>滿箱數量</th><th>PackingNo</th><th>Cur_PackQty</th><th>Cur_PackLotQty</th><th>狀態</th></tr></thead><tbody>
            <tr v-for="rule in enabledLevels" :key="rule.level" :class="{ selected: activeLevel === rule.level }" @click="setActiveLevel(rule.level)"><td>L{{ rule.level }}</td><td>{{ rule.name }}</td><td class="numeric">{{ rule.maxCount ?? '—' }}</td><td class="numeric">{{ rule.maxQty === null ? '—' : formatQty(rule.maxQty) }}</td><td>{{ state.boxes.find(box => box.level === rule.level && !box.isClosed && !box.isMerged && !box.dismantled)?.packingNo || '（封箱後自動開新箱）' }}</td><td class="numeric">{{ openBox && activeLevel === rule.level ? contentCount(openBox) : (state.boxes.find(box => box.level === rule.level && !box.isClosed && !box.isMerged && !box.dismantled)?.contents.length ?? 0) }}</td><td class="numeric">{{ formatQty(state.boxes.find(box => box.level === rule.level && !box.isClosed && !box.isMerged && !box.dismantled)?.qty ?? 0) }}</td><td>{{ state.boxes.some(box => box.level === rule.level && !box.isClosed && !box.isMerged && !box.dismantled) ? '未封箱' : '待開箱' }}</td></tr>
            <tr v-if="!enabledLevels.length"><td colspan="8" class="empty-row">請按 Pack Config 設定包裝工單與階層</td></tr>
          </tbody></table></div>
        </section>
        <section class="grid-panel label-panel">
          <div class="panel-title"><h2>Print Label</h2><span>封箱後依產品標籤規則建立列印工作</span></div>
          <div class="table-wrap"><table class="packing-grid"><thead><tr><th>Level</th><th>LabelType</th><th>PackingNo</th><th>Label Printer</th><th>Label Path</th><th>Result</th><th>列印</th></tr></thead><tbody>
            <tr v-for="job in printJobs" :key="job.id"><td>L{{ job.packingLevel }}</td><td>{{ job.labelType }}</td><td>{{ job.packingNo }}</td><td>{{ job.printerName }}</td><td>{{ job.path }}</td><td>{{ job.result }}</td><td><button type="button" class="small-button" :disabled="!dev" @click="simulatePrint(job)">DEV Print</button></td></tr>
            <tr v-if="!printJobs.length"><td colspan="7" class="empty-row">尚無封箱標籤工作；列印範本與軟體設定依產品標籤規則載入</td></tr>
          </tbody></table></div>
        </section>
      </div>
      <div class="right-workspace">
        <section class="grid-panel wait-panel">
          <div class="panel-title"><h2>Wait Close Box Content</h2><span>{{ openBox?.packingNo || '尚未開箱' }}</span></div>
          <div class="table-wrap detail-wrap"><table class="packing-grid"><thead><tr><th>ItemNo</th><th>ItemType</th><th>Level</th><th>PackQty</th><th>Unit</th></tr></thead><tbody>
            <tr v-for="item in currentContents" :key="item.id" :class="{ selected: selectedContentId === item.id }" @click="selectedContentId = item.id"><td>{{ item.itemNo }}</td><td>{{ item.itemType }}</td><td>{{ item.itemType === 'LOT' ? 'LOT' : levelName(item.itemLevel) }}</td><td class="numeric">{{ formatQty(item.packQty) }}</td><td>{{ item.unitNo }}</td></tr>
            <tr v-if="!currentContents.length"><td colspan="5" class="empty-row">選取左側包裝階層以查看未封箱明細</td></tr>
          </tbody></table></div>
          <div class="panel-actions"><label class="quick-lot"><span>LotNo</span><input v-model="lotBarcode" :disabled="!dev" @keydown.enter.prevent="scanBarcode"></label><button type="button" class="danger-button" :disabled="!dev || !selectedContentId" @click="deleteContent">Delete</button></div>
        </section>
        <section class="grid-panel accessory-panel">
          <div class="panel-title"><h2>Pack Accessory</h2><span>{{ checkedAccessories.length }}/{{ state.accessories.length }} 已確認</span></div>
          <div class="table-wrap"><table class="packing-grid"><thead><tr><th>AccessoryBarCode</th><th>AccessoryName</th><th>CheckFlag</th></tr></thead><tbody>
            <tr v-for="row in scannedAccessoryRows" :key="row.barcode"><td>{{ row.barcode }}</td><td>{{ row.name }}</td><td>{{ row.checked ? '是' : '否' }}</td></tr>
            <tr v-if="!state.accessories.length"><td colspan="3" class="empty-row">此包裝規則未設定配件條碼</td></tr>
          </tbody></table></div>
        </section>
      </div>
    </main>

    <footer class="action-bar"><button type="button" class="primary-action" :disabled="!dev" @click="closeBox(activeLevel, false)">Pack Close</button><button type="button" :disabled="!dev" @click="openOperation('combine')">Combine</button><button type="button" :disabled="!dev" @click="openOperation('unboxing')">Unboxing</button><span class="action-spacer"></span><button type="button" class="small-button" :disabled="!dev" @click="resetDevData">還原 DEV 資料</button><button type="button" @click="emit('close')">Close (&amp;X)</button></footer>
    <div v-if="actionError" class="feedback error" role="alert">{{ actionError }}</div><div v-else-if="actionMessage" class="feedback success" role="status">{{ actionMessage }}</div>
    <div v-if="state.logs.length" class="activity-line"><span>操作紀錄</span>{{ state.logs[0] }}</div>
    <p v-if="!dev" class="production-note">此頁僅提供公司端 DEV 操作預覽。Production API、資料庫交易與本機印表機尚未接入。</p>

    <div v-if="packConfigOpen" class="modal-backdrop" @click.self="packConfigOpen = false"><section class="modal-card config-modal" role="dialog" aria-modal="true" aria-label="PackConfig">
      <header><h2>PackConfig</h2><button type="button" class="modal-x" @click="packConfigOpen = false">×</button></header>
      <div class="config-head"><label><span>ComputerName</span><input :value="state.computerName" readonly></label><label><span>MONo</span><select v-model="configMoNo"><option :value="sampleMoNo">{{ sampleMoNo }} · {{ inventoryMode ? '在製品庫包裝 DEV' : '控制模組' }}</option></select></label></div>
      <fieldset class="config-grid"><legend>PackLevel</legend><table class="packing-grid"><thead><tr><th>Select</th><th>PackingLevel</th><th>PackingLevelName</th><th>滿箱箱數</th><th>滿箱數量</th></tr></thead><tbody><tr v-for="rule in state.levels" :key="rule.level"><td><input type="checkbox" :checked="configLevels.includes(rule.level)" @change="toggleConfigLevel(rule.level)"></td><td>L{{ rule.level }}</td><td>{{ rule.name }}</td><td>{{ rule.maxCount ?? '—' }}</td><td>{{ rule.maxQty ?? '—' }}</td></tr></tbody></table></fieldset>
      <fieldset class="config-printers"><legend>Label Printer</legend><div class="table-wrap"><table class="packing-grid"><thead><tr><th>LabelType</th><th>PrinterName</th></tr></thead><tbody><tr v-for="rule in state.levels.filter(item => configLevels.includes(item.level))" :key="rule.level"><template v-for="labelType in rule.labelTypes" :key="labelType"><td>{{ rule.name }}-{{ labelType.slice(-2) }}<span class="type-code">{{ labelType }}</span></td><td><select v-model="configPrinters[labelType]"><option value="">--</option><option v-for="printer in printerChoices" :key="printer" :value="printer">{{ printer }}</option></select></td></template></tr></tbody></table></div></fieldset>
      <p v-if="actionError" class="feedback error" role="alert">{{ actionError }}</p><footer><span>標籤印表機依電腦名稱保存</span><button type="button" @click="packConfigOpen = false">Close</button><button type="button" class="primary-action" @click="saveConfig">Confirm</button></footer>
    </section></div>

    <div v-if="operationDialog" class="modal-backdrop" @click.self="operationDialog = null"><section class="modal-card operation-modal" role="dialog" aria-modal="true" :aria-label="operationDialog">
      <header><h2>{{ operationDialog === 'combine' ? 'Combine' : 'Unboxing' }}</h2><button type="button" class="modal-x" @click="operationDialog = null">×</button></header>
      <p class="modal-intro">DEV 僅模擬包裝明細與狀態。WinForms 此兩項會呼叫 ObjectFileHandler「INVMOPackingCombineDismantle」；正式規則需由 API/Core 接手。</p>
      <div class="form-grid"><label><span>來源已封箱 PackingNo</span><select v-model="operationSourceNo"><option value="">請選擇</option><option v-for="box in usableClosedBoxes" :key="box.packingNo" :value="box.packingNo">{{ box.packingNo }} · {{ levelName(box.level) }} · {{ formatQty(box.qty) }}</option></select></label><label v-if="operationDialog === 'combine'"><span>目標未封箱 PackingNo</span><select v-model="operationTargetNo"><option value="">請選擇</option><option v-for="box in openBoxes" :key="box.packingNo" :value="box.packingNo">{{ box.packingNo }} · {{ levelName(box.level) }}</option></select></label></div>
      <p v-if="actionError" class="feedback error" role="alert">{{ actionError }}</p><footer><button type="button" @click="operationDialog = null">Cancel</button><button type="button" class="primary-action" @click="confirmOperation">Confirm DEV 模擬</button></footer>
    </section></div>

    <div v-if="pendingAutoCloseLevel !== null" class="modal-backdrop" @click.self="pendingAutoCloseLevel = null"><section class="modal-card prompt-modal" role="dialog" aria-modal="true" aria-label="Full Box Close">
      <header><h2>Full Box</h2></header><p>{{ levelName(pendingAutoCloseLevel) }} 已達滿箱條件。若目前有內層箱待轉入，需先封閉此箱；是否封箱並繼續？</p><footer><button type="button" @click="pendingAutoCloseLevel = null">稍後封箱</button><button type="button" class="primary-action" @click="confirmPendingAutoClose">封箱</button></footer>
    </section></div>
  </section>
</template>

<style scoped>
.packing-page{height:100%;min-height:590px;display:flex;flex-direction:column;gap:8px;padding:10px 12px 8px;background:#f5f6f8;color:#253142;box-sizing:border-box;font:13px/1.35 "Segoe UI",Arial,sans-serif;overflow:auto}.page-heading{display:flex;justify-content:space-between;align-items:center;min-height:46px}.function-code{font-size:11px;color:#738092;letter-spacing:.08em}.page-heading h1{font-size:20px;margin:2px 0 0;font-weight:600;color:#233148}.heading-context{display:flex;gap:8px;flex-wrap:wrap;justify-content:flex-end}.heading-context span{border:1px solid #d7dce3;background:#fff;padding:5px 8px;border-radius:3px;color:#5d6878;font-size:12px}.form-panel,.grid-panel{border:1px solid #c8ced7;border-radius:3px;background:#fff;box-shadow:0 1px 2px #1520310d}.barcode-panel{min-height:126px;margin:0;padding:7px 10px 10px}.barcode-panel legend{padding:0 6px;font-weight:600;color:#435066}.barcode-grid{display:grid;grid-template-columns:1.2fr .68fr .7fr .9fr 1.25fr .98fr .85fr 1fr 1.18fr .54fr auto auto;gap:6px 8px;align-items:end}.barcode-grid label,.config-head label,.form-grid label{display:flex;flex-direction:column;gap:3px;min-width:0}.barcode-grid label span,.config-head label span,.form-grid label span{font-size:11px;color:#5c6674}.barcode-grid input,.config-head input,.config-head select,.form-grid input,.form-grid select,.quick-lot input,.config-printers select{height:27px;min-width:0;border:1px solid #aeb6c2;border-radius:2px;padding:3px 7px;box-sizing:border-box;background:#fff;color:#243246;font:inherit}.barcode-grid input:read-only,.config-head input:read-only{background:#f0f1f3;color:#5d6774}.barcode-grid .scan-field input,.barcode-grid .accessory-scan input{background:#fff2b8}.barcode-grid .auto-close{flex-direction:row;align-items:center;align-self:center;gap:5px;white-space:nowrap;padding-top:15px}.barcode-grid .auto-close input{width:15px;height:15px}.barcode-grid button,.action-bar button,.panel-actions button,.modal-card button{height:30px;padding:0 12px;border:1px solid #aeb6c2;border-radius:3px;background:linear-gradient(#fff,#f4f5f6);color:#263548;font:inherit;white-space:nowrap;cursor:pointer}.barcode-grid button{align-self:end}.barcode-grid button:hover,.action-bar button:hover,.panel-actions button:hover,.modal-card button:hover{background:#eaf0f7}.barcode-grid button:disabled,.action-bar button:disabled,.panel-actions button:disabled,.modal-card button:disabled{opacity:.5;cursor:not-allowed}.primary-action{background:#f8a12d!important;border-color:#ec941e!important;color:#fff!important;font-weight:600!important}.primary-action:hover{background:#ed941f!important}.danger-button{color:#a53a30!important;border-color:#d4aaa5!important}.packing-workspace{flex:1;min-height:340px;display:grid;grid-template-columns:minmax(0,1.66fr) minmax(340px,1fr);gap:8px}.left-workspace,.right-workspace{min-height:0;display:flex;flex-direction:column;gap:8px}.level-panel{flex:1.13;min-height:190px}.label-panel{flex:.87;min-height:150px}.wait-panel{flex:1.25;min-height:230px}.accessory-panel{flex:.75;min-height:150px}.grid-panel{display:flex;flex-direction:column;min-height:0;overflow:hidden}.panel-title{height:31px;min-height:31px;display:flex;align-items:center;justify-content:space-between;padding:0 9px;border-bottom:1px solid #d9dee5;background:#f7f8fa}.panel-title h2{font-size:13px;font-weight:600;margin:0;color:#3d4a5d}.panel-title span{font-size:11px;color:#748093;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;margin-left:8px}.table-wrap{overflow:auto;flex:1;min-height:0}.packing-grid{border-collapse:collapse;width:100%;font-size:12px;white-space:nowrap}.packing-grid th{position:sticky;top:0;background:#4e9fe2;color:white;font-weight:600;text-align:left;z-index:1}.packing-grid th,.packing-grid td{border:1px solid #d0d5dc;padding:5px 7px;height:22px}.packing-grid td{background:#fff}.packing-grid tbody tr:hover td{background:#f2f6fa}.packing-grid tbody tr.selected td{background:#fff0ae}.packing-grid .numeric{text-align:right}.empty-row{text-align:center!important;color:#828d9b!important;padding:15px!important}.panel-actions{display:flex;align-items:center;gap:7px;padding:6px 7px;border-top:1px solid #d9dee5;background:#fafbfc}.quick-lot{display:flex;align-items:center;gap:6px;margin-right:auto;color:#687486;font-size:11px}.quick-lot input{width:170px;height:26px}.action-bar{display:flex;align-items:center;gap:7px;min-height:44px;padding:5px 7px;border:1px solid #c8ced7;background:#fff}.action-bar .action-spacer{flex:1}.small-button{height:24px!important;padding:0 7px!important;font-size:11px!important}.feedback{margin:0;padding:7px 10px;border-radius:3px;font-size:12px}.feedback.error{color:#922d26;background:#fff0ef;border:1px solid #e7c1bd}.feedback.success{color:#25623d;background:#edf8f0;border:1px solid #bbddc4}.activity-line{min-height:18px;color:#778293;font-size:11px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}.activity-line span{font-weight:600;color:#515d6e;margin-right:8px}.production-note{margin:0;color:#8a6a27;font-size:11px}.modal-backdrop{position:fixed;inset:0;z-index:80;background:#17233373;display:flex;align-items:center;justify-content:center;padding:16px}.modal-card{width:min(760px,96vw);max-height:94vh;overflow:auto;background:#fff;border:1px solid #abb4c0;border-radius:4px;box-shadow:0 14px 42px #15203140;padding:0 14px 14px}.modal-card>header{display:flex;align-items:center;justify-content:space-between;margin:0 -14px 12px;padding:9px 14px;background:#f3f5f7;border-bottom:1px solid #d1d7df}.modal-card h2{margin:0;font-size:16px;color:#334156}.modal-card .modal-x{width:28px;height:28px;padding:0;font-size:19px}.config-head{display:grid;grid-template-columns:1fr 1fr;gap:10px;margin:4px 0 9px}.config-grid,.config-printers{margin:8px 0;border:1px solid #cbd1da;padding:8px 9px}.config-grid legend,.config-printers legend{padding:0 5px;font-weight:600;color:#48566a}.config-grid table{width:100%}.config-printers{height:210px;display:flex;flex-direction:column}.config-printers .table-wrap{flex:1}.config-printers select{width:180px}.type-code{display:block;color:#788496;font-size:10px}.modal-card>footer{display:flex;justify-content:flex-end;align-items:center;gap:7px;margin-top:12px}.config-modal>footer span{margin-right:auto;color:#7b8594;font-size:11px}.operation-modal,.prompt-modal{width:min(600px,94vw)}.modal-intro{color:#636f7f;line-height:1.5}.form-grid{display:grid;grid-template-columns:1fr 1fr;gap:12px;margin:14px 0}.prompt-modal p{line-height:1.5;color:#515d6e}@media(max-width:1100px){.barcode-grid{grid-template-columns:repeat(4,minmax(120px,1fr))}.packing-workspace{grid-template-columns:minmax(0,1.35fr) minmax(300px,1fr)}}@media(max-width:780px){.packing-page{height:auto;min-height:100%;overflow:visible}.page-heading{align-items:flex-start;gap:8px;flex-direction:column}.heading-context{justify-content:flex-start}.barcode-grid{grid-template-columns:repeat(2,minmax(130px,1fr))}.packing-workspace{grid-template-columns:1fr}.left-workspace,.right-workspace{min-height:420px}.action-bar{flex-wrap:wrap}.config-head,.form-grid{grid-template-columns:1fr}}
</style>
