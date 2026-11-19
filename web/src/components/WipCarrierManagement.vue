<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'

type Carrier = {
  carrierNo: string; carrierCategory: string; carrierType: string; carrierState: number
  description: string; carrierLayer: number; carrierCapacity: number; issueState?: number; useLayer?: number
}
type CarrierDetail = {
  id: string; carrierNo: string; carrierPosition: number; bcip: string; contentId: string
  flag: number; lotNo: string; componentNo: string; eventTime: string
}
type Lot = {
  lotNo: string; lotState: number; status: number; carrierNo: string | null; components: string[]
  componentSlotMap?: Record<string, number>; stockerNo?: string; locatorNo?: string
}
type Snapshot = { version: 1; carriers: Carrier[]; details: CarrierDetail[]; lots: Lot[]; logs: CarrierDetail[] }
type PickerMode = 'type-filter' | 'in-carrier' | 'org-carrier' | 'new-carrier' | null
type PickerOption = { value: string; label: string; carrier?: Carrier }

const props = defineProps<{ currentUserNo?: string; developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const storageKey = 'ymes.dev.wp302-carrier-state.v1'

function seedSnapshot(): Snapshot {
  return {
    version: 1,
    carriers: [
      { carrierNo: 'DEV-CARRIER-01', carrierCategory: 'Tray 載具', carrierType: 'TRAY-25', carrierState: 1, description: 'DEV 多層 Tray 載具', carrierLayer: 1, carrierCapacity: 25, issueState: 2, useLayer: 0 },
      { carrierNo: 'DEV-CARRIER-02', carrierCategory: 'Tray 載具', carrierType: 'TRAY-10', carrierState: 0, description: 'DEV 空載具', carrierLayer: 1, carrierCapacity: 10, issueState: 2, useLayer: 0 },
      { carrierNo: 'DEV-CARRIER-03', carrierCategory: 'Tray 載具', carrierType: 'TRAY-25', carrierState: 0, description: 'DEV 備用載具', carrierLayer: 1, carrierCapacity: 25, issueState: 2, useLayer: 0 },
    ],
    details: [
      { id: 'wp302-detail-1', carrierNo: 'DEV-CARRIER-01', carrierPosition: 1, bcip: 'DEVUSER', contentId: 'DEV-LOT-001', flag: 0, lotNo: 'DEV-LOT-001', componentNo: 'N/A', eventTime: '2026/11/20 08:30:00' },
      { id: 'wp302-detail-2', carrierNo: 'DEV-CARRIER-01', carrierPosition: 2, bcip: 'DEVUSER', contentId: 'DEV-LOT-COMP-001', flag: 0, lotNo: 'DEV-LOT-COMP-001', componentNo: 'DEV-COMP-001', eventTime: '2026/11/20 08:31:00' },
      { id: 'wp302-detail-3', carrierNo: 'DEV-CARRIER-01', carrierPosition: 3, bcip: 'DEVUSER', contentId: 'DEV-LOT-COMP-001', flag: 0, lotNo: 'DEV-LOT-COMP-001', componentNo: 'DEV-COMP-002', eventTime: '2026/11/20 08:31:00' },
    ],
    lots: [
      { lotNo: 'DEV-LOT-001', lotState: 1, status: 1, carrierNo: 'DEV-CARRIER-01', components: [] },
      { lotNo: 'DEV-LOT-COMP-001', lotState: 1, status: 1, carrierNo: 'DEV-CARRIER-01', components: ['DEV-COMP-001', 'DEV-COMP-002', 'DEV-COMP-003'], componentSlotMap: { 'DEV-COMP-001': 2, 'DEV-COMP-002': 3, 'DEV-COMP-003': 4 } },
      { lotNo: 'DEV-LOT-FREE-001', lotState: 1, status: 0, carrierNo: null, components: [] },
      { lotNo: 'DEV-LOT-FREE-COMP-001', lotState: 1, status: 2, carrierNo: null, components: ['DEV-COMP-101', 'DEV-COMP-102', 'DEV-COMP-103'], componentSlotMap: { 'DEV-COMP-101': 1, 'DEV-COMP-102': 2, 'DEV-COMP-103': 3 } },
      { lotNo: 'DEV-LOT-STOCKER-001', lotState: 1, status: 1, carrierNo: null, components: ['DEV-COMP-ST-01'], stockerNo: 'DEV-STOCKER-01', locatorNo: 'A-01-02' },
      { lotNo: 'DEV-LOT-CLOSED-001', lotState: 1, status: 12, carrierNo: null, components: [] },
      { lotNo: 'DEV-WP308-LOT-001', lotState: 1, status: 7, carrierNo: null, components: ['DEV-WP308-COMP-001', 'DEV-WP308-COMP-002'], componentSlotMap: { 'DEV-WP308-COMP-001': 1, 'DEV-WP308-COMP-002': 2 } },
      { lotNo: 'DEV-WP308-LOT-002', lotState: 1, status: 10, carrierNo: null, components: ['DEV-WP308-COMP-003', 'DEV-WP308-COMP-004', 'DEV-WP308-COMP-005'] },
    ],
    logs: [],
  }
}

function readSnapshot(): Snapshot {
  if (!dev.value) return seedSnapshot()
  try {
    const raw = window.localStorage.getItem(storageKey)
    if (raw) {
      const parsed = JSON.parse(raw) as Snapshot
      if (parsed.version === 1 && Array.isArray(parsed.carriers) && Array.isArray(parsed.details) && Array.isArray(parsed.lots) && Array.isArray(parsed.logs)) return parsed
    }
  } catch { /* use deterministic fixtures when DEV storage is unavailable */ }
  return seedSnapshot()
}

const snapshot = ref<Snapshot>(readSnapshot())
const selectedCarrierNo = ref('DEV-CARRIER-01')
const selectedPosition = ref<number | null>(null)
const carrierTypeFilter = ref('')
const carrierNoFilter = ref('')
const inOpen = ref(false)
const changeOpen = ref(false)
const pickerMode = ref<PickerMode>(null)
const pickerError = ref('')
const inCarrierNo = ref('')
const inCarrierType = ref('')
const inLotInput = ref('')
const inDraft = ref<CarrierDetail[]>([])
const inSelectedPosition = ref<number | null>(null)
const orgCarrierNo = ref('')
const orgCarrierType = ref('')
const newCarrierNo = ref('')
const newCarrierType = ref('')
const changeLotInput = ref('')
const changeDraft = ref<CarrierDetail[]>([])
const changeSelectedPosition = ref<number | null>(null)
const carrierOutConfirm = ref(false)
const actionError = ref('')
const actionMessage = ref('')
const updatedAt = ref(new Date().toLocaleString('zh-TW', { hour12: false }))

function persist(): void {
  if (!dev.value) return
  try { window.localStorage.setItem(storageKey, JSON.stringify(snapshot.value)) } catch { /* local DEV state is best effort */ }
}
function refreshFromStorage(): void {
  if (!dev.value) return
  snapshot.value = readSnapshot()
  if (!snapshot.value.carriers.some((item) => item.carrierNo === selectedCarrierNo.value)) selectedCarrierNo.value = snapshot.value.carriers[0]?.carrierNo ?? ''
}
onMounted(() => {
  if (!snapshot.value.carriers.some((item) => item.carrierNo === selectedCarrierNo.value)) selectedCarrierNo.value = snapshot.value.carriers[0]?.carrierNo ?? ''
  window.addEventListener('storage', refreshFromStorage)
})
onBeforeUnmount(() => window.removeEventListener('storage', refreshFromStorage))

const carriers = computed(() => snapshot.value.carriers)
const filteredCarriers = computed(() => carriers.value.filter((carrier) =>
  (!carrierTypeFilter.value || carrier.carrierType === carrierTypeFilter.value)
  && (!carrierNoFilter.value.trim() || carrier.carrierNo.toUpperCase().startsWith(carrierNoFilter.value.trim().toUpperCase()))))
const selectedCarrier = computed(() => carriers.value.find((item) => item.carrierNo === selectedCarrierNo.value) ?? null)
const carrierDetails = computed(() => snapshot.value.details
  .filter((item) => item.carrierNo === selectedCarrierNo.value)
  .sort((a, b) => a.carrierPosition - b.carrierPosition))
const carrierCapacity = computed(() => selectedCarrier.value ? selectedCarrier.value.carrierLayer * selectedCarrier.value.carrierCapacity : 0)
const carrierSlots = computed(() => Array.from({ length: carrierCapacity.value }, (_, index) => {
  const position = index + 1
  return { position, detail: carrierDetails.value.find((item) => item.carrierPosition === position) ?? null }
}))
const carrierTypeOptions = computed(() => [...new Set(carriers.value.map((item) => item.carrierType))].sort())
const inBoundRows = computed(() => inDraft.value.filter((row) => row.lotNo !== '' && row.componentNo !== ''))
const changeBoundRows = computed(() => changeDraft.value.filter((row) => row.lotNo !== '' && row.componentNo !== ''))
const inCarrier = computed(() => carriers.value.find((item) => item.carrierNo === inCarrierNo.value) ?? null)
const orgCarrier = computed(() => carriers.value.find((item) => item.carrierNo === orgCarrierNo.value) ?? null)
const newCarrier = computed(() => carriers.value.find((item) => item.carrierNo === newCarrierNo.value) ?? null)
const pickerOptions = computed<PickerOption[]>(() => {
  if (pickerMode.value === 'type-filter') return carrierTypeOptions.value.map((value) => ({ value, label: value }))
  if (pickerMode.value === 'in-carrier') return carriers.value.filter((item) => (item.issueState ?? 2) === 2)
    .map((carrier) => ({ value: carrier.carrierNo, label: carrier.carrierNo + '　' + carrier.carrierType + '　' + (carrier.carrierState === 1 ? 'In Use' : 'Empty'), carrier }))
  if (pickerMode.value === 'org-carrier') return carriers.value.filter((item) => (item.issueState ?? 2) === 2
    && snapshot.value.details.some((detail) => detail.carrierNo === item.carrierNo && detail.componentNo && detail.componentNo !== 'N/A'))
    .map((carrier) => ({ value: carrier.carrierNo, label: carrier.carrierNo + '　' + carrier.carrierType + '　In Use', carrier }))
  if (pickerMode.value === 'new-carrier' && orgCarrier.value) {
    const requireLayeredType = carrierUseLayer(orgCarrier.value) === 1
    return carriers.value.filter((item) => item.carrierNo !== orgCarrierNo.value
      && (item.issueState ?? 2) === 2
      && !snapshot.value.details.some((detail) => detail.carrierNo === item.carrierNo)
      && (!requireLayeredType || carrierUseLayer(item) === 1))
      .map((carrier) => ({ value: carrier.carrierNo, label: carrier.carrierNo + '　' + carrier.carrierType + '　容量 ' + (carrier.carrierLayer * carrier.carrierCapacity), carrier }))
  }
  return []
})

function carrierUseLayer(carrier: Carrier): number {
  return carrier.useLayer ?? (carrier.carrierLayer > 1 ? 1 : 0)
}
function findLot(lotNo: string): Lot | undefined {
  const normalized = lotNo.trim().toUpperCase()
  return snapshot.value.lots.find((item) => item.lotNo.toUpperCase() === normalized)
}
function clearFeedback(): void { actionError.value = ''; actionMessage.value = '' }
function showError(message: string): void { actionError.value = message; actionMessage.value = '' }
function showMessage(message: string): void { actionMessage.value = message; actionError.value = '' }
function currentTime(): string { return new Date().toLocaleString('zh-TW', { hour12: false }) }
function blankDetail(carrierNo: string, position: number): CarrierDetail {
  return { id: 'wp308-draft-' + carrierNo + '-' + position, carrierNo, carrierPosition: position, bcip: '', contentId: '', flag: 0, lotNo: '', componentNo: '', eventTime: '' }
}
function makeDraft(carrierNo: string, capacity: number, rows: CarrierDetail[]): CarrierDetail[] {
  const byPosition = new Map(rows.map((row) => [row.carrierPosition, row]))
  return Array.from({ length: capacity }, (_, index) => {
    const position = index + 1
    return byPosition.get(position) ? { ...byPosition.get(position)! } : blankDetail(carrierNo, position)
  })
}
function query(): void {
  clearFeedback()
  if (!filteredCarriers.value.some((item) => item.carrierNo === selectedCarrierNo.value)) selectedCarrierNo.value = filteredCarriers.value[0]?.carrierNo ?? ''
  selectedPosition.value = null
}
function selectCarrier(carrierNo: string): void { selectedCarrierNo.value = carrierNo; selectedPosition.value = null; clearFeedback() }
function selectPosition(position: number): void { selectedPosition.value = position; clearFeedback() }
function resetDev(): void {
  snapshot.value = seedSnapshot()
  selectedCarrierNo.value = 'DEV-CARRIER-01'
  selectedPosition.value = null
  carrierTypeFilter.value = ''
  carrierNoFilter.value = ''
  inOpen.value = false
  changeOpen.value = false
  carrierOutConfirm.value = false
  persist()
  showMessage('已還原載具管理相關 DEV 假資料。')
}
function openPicker(mode: Exclude<PickerMode, null>): void { pickerError.value = ''; pickerMode.value = mode }
function closePicker(): void { pickerMode.value = null; pickerError.value = '' }
function chooseCarrier(option: PickerOption): void {
  if (pickerMode.value === 'type-filter') {
    carrierTypeFilter.value = option.value
    closePicker()
    return
  }
  const carrier = option.carrier
  if (!carrier) return
  if (pickerMode.value === 'in-carrier') {
    inCarrierNo.value = carrier.carrierNo
    inCarrierType.value = carrier.carrierType
    inDraft.value = makeDraft(carrier.carrierNo, carrier.carrierLayer * carrier.carrierCapacity, snapshot.value.details.filter((row) => row.carrierNo === carrier.carrierNo))
    inSelectedPosition.value = null
  } else if (pickerMode.value === 'org-carrier') {
    orgCarrierNo.value = carrier.carrierNo
    orgCarrierType.value = carrier.carrierType
    newCarrierNo.value = ''
    newCarrierType.value = ''
    changeDraft.value = []
    changeSelectedPosition.value = null
    changeLotInput.value = [...new Set(snapshot.value.details.filter((row) => row.carrierNo === carrier.carrierNo).map((row) => row.lotNo))].join(',')
  } else if (pickerMode.value === 'new-carrier') {
    newCarrierNo.value = carrier.carrierNo
    newCarrierType.value = carrier.carrierType
    const oldRows = snapshot.value.details.filter((row) => row.carrierNo === orgCarrierNo.value).sort((a, b) => a.carrierPosition - b.carrierPosition)
    const capacity = carrier.carrierLayer * carrier.carrierCapacity
    if (oldRows.length > capacity || oldRows.some((row) => row.carrierPosition > capacity)) {
      pickerError.value = '新載具容量 ' + capacity + ' 不足以保留原載具位置。'
      newCarrierNo.value = ''
      newCarrierType.value = ''
      return
    }
    changeDraft.value = makeDraft(carrier.carrierNo, capacity, oldRows.map((row) => ({ ...row, carrierNo: carrier.carrierNo })))
    changeSelectedPosition.value = null
  }
  closePicker()
}

function beginCarrierIn(): void {
  clearFeedback()
  inOpen.value = true
  inLotInput.value = ''
  inSelectedPosition.value = null
  if (selectedCarrier.value) {
    inCarrierNo.value = selectedCarrier.value.carrierNo
    inCarrierType.value = selectedCarrier.value.carrierType
    inDraft.value = makeDraft(selectedCarrier.value.carrierNo, carrierCapacity.value, carrierDetails.value)
  } else {
    inCarrierNo.value = ''
    inCarrierType.value = ''
    inDraft.value = []
    openPicker('in-carrier')
  }
}
function closeCarrierIn(): void { inOpen.value = false; inLotInput.value = ''; inDraft.value = []; inSelectedPosition.value = null }
function addLotToIn(): void {
  clearFeedback()
  const carrier = inCarrier.value
  const lotNo = inLotInput.value.trim().toUpperCase()
  if (!dev.value) { showError('DEV 假資料模式停用。'); return }
  if (!carrier) { showError('請先選擇 CarrierNo。'); return }
  if (!lotNo) { showError('LotNo 不可空白。'); return }
  if (inDraft.value.some((row) => row.lotNo.toUpperCase() === lotNo)) { showError('批號 ' + lotNo + ' 已在目前載具。'); return }
  const lot = findLot(lotNo)
  if (!lot || lot.lotState !== 1) { showError('找不到可用的 LotState：' + lotNo + '。'); return }
  if (lot.status > 11) { showError('Lot ' + lotNo + ' 已進入庫存狀態（Status > 11）。'); return }
  if (lot.stockerNo) { showError('Lot ' + lotNo + ' 在 Stocker ' + lot.stockerNo + '／' + (lot.locatorNo ?? '') + '，不能上載具。'); return }
  if (lot.carrierNo || snapshot.value.details.some((row) => row.lotNo.toUpperCase() === lotNo)) { showError('Lot ' + lotNo + ' 已綁定載具。'); return }
  if (lot.components.length === 0) { showError('Lot ' + lotNo + ' 沒有元件，不能執行載具管理元件。'); return }
  const emptyCount = inDraft.value.filter((row) => !row.lotNo).length
  if (emptyCount < lot.components.length) { showError('載具剩餘位置 ' + emptyCount + ' 小於元件數 ' + lot.components.length + '。'); return }
  const components = [...lot.components].sort((a, b) => (lot.componentSlotMap?.[a] ?? Number.MAX_SAFE_INTEGER) - (lot.componentSlotMap?.[b] ?? Number.MAX_SAFE_INTEGER))
  for (const componentNo of components) {
    const preferredPosition = lot.componentSlotMap?.[componentNo]
    const slot = (preferredPosition ? inDraft.value.find((row) => row.carrierPosition === preferredPosition && !row.lotNo) : undefined)
      ?? inDraft.value.find((row) => !row.lotNo)
    if (!slot) { showError('載具位置不足，無法放入全部元件。'); return }
    slot.lotNo = lot.lotNo
    slot.contentId = lot.lotNo
    slot.componentNo = componentNo
    slot.bcip = props.currentUserNo || 'DEVUSER'
    slot.flag = 0
    slot.eventTime = currentTime()
  }
  inLotInput.value = ''
  showMessage('已將 ' + lot.lotNo + ' 的 ' + lot.components.length + ' 個元件排入載具草稿；按「確認」才會更新 DEV 狀態。')
}
function swapContent(rows: CarrierDetail[], position: number, direction: -1 | 1, changeFlag: boolean): CarrierDetail[] {
  const next = rows.map((row) => ({ ...row }))
  const currentIndex = next.findIndex((row) => row.carrierPosition === position)
  const otherIndex = next.findIndex((row) => row.carrierPosition === position + direction)
  if (currentIndex < 0 || otherIndex < 0) return rows
  const fields: (keyof CarrierDetail)[] = ['lotNo', 'contentId', 'componentNo', 'flag']
  for (const field of fields) [next[currentIndex][field], next[otherIndex][field]] = [next[otherIndex][field], next[currentIndex][field]]
  if (changeFlag) {
    next[currentIndex].flag = next[currentIndex].lotNo ? 1 : 0
    next[otherIndex].flag = next[otherIndex].lotNo ? 1 : 0
  } else {
    next[currentIndex].flag = 0
    next[otherIndex].flag = 0
  }
  return next
}
function moveIn(position: number, direction: -1 | 1): void { inDraft.value = swapContent(inDraft.value, position, direction, false); inSelectedPosition.value = position + direction }
function confirmCarrierIn(): void {
  clearFeedback()
  if (!dev.value) { showError('DEV 假資料模式停用。'); return }
  const carrier = inCarrier.value
  if (!carrier) { showError('請先選擇 CarrierNo。'); return }
  if (!inBoundRows.value.length) { showError('請先輸入有效 LotNo。'); return }
  const stamp = currentTime()
  const desiredRows = inBoundRows.value.map((row) => ({ ...row }))
  const currentRows = snapshot.value.details.filter((item) => item.carrierNo === carrier.carrierNo)
  const movingRows = currentRows.filter((old) => desiredRows.some((row) => row.lotNo === old.lotNo && row.componentNo === old.componentNo && row.carrierPosition !== old.carrierPosition))
  const movingIds = new Set(movingRows.map((row) => row.id))
  const collision = desiredRows.find((row) => snapshot.value.details.some((item) => item.carrierNo === carrier.carrierNo
    && item.carrierPosition === row.carrierPosition
    && !movingIds.has(item.id)
    && !(item.lotNo === row.lotNo && item.componentNo === row.componentNo)))
  if (collision) { showError('CarrierPosition ' + collision.carrierPosition + ' 已被其他資料占用，請重新載入草稿。'); return }
  snapshot.value.logs.push(...movingRows.map((row) => ({ ...row })))
  snapshot.value.details = snapshot.value.details.filter((row) => !movingIds.has(row.id))
  for (const row of desiredRows) {
    const existing = snapshot.value.details.find((item) => item.carrierNo === carrier.carrierNo && item.lotNo === row.lotNo && item.componentNo === row.componentNo)
    if (existing && existing.carrierPosition === row.carrierPosition) continue
    snapshot.value.details.push({ ...row, id: 'wp308-' + Date.now() + '-' + row.carrierPosition, eventTime: stamp })
    const lot = findLot(row.lotNo)
    if (lot) {
      lot.carrierNo = carrier.carrierNo
      lot.componentSlotMap = { ...(lot.componentSlotMap ?? {}), [row.componentNo]: row.carrierPosition }
    }
  }
  carrier.carrierState = 1
  updatedAt.value = stamp
  persist()
  selectedCarrierNo.value = carrier.carrierNo
  const count = inBoundRows.value.length
  closeCarrierIn()
  showMessage('DEV 已模擬將 ' + count + ' 個元件上載具。')
}

function requestCarrierOut(): void {
  clearFeedback()
  if (!selectedCarrier.value || selectedPosition.value === null) { showError('請先選擇載具及其位置列。'); return }
  if (!carrierDetails.value.length) { showError('目前載具沒有可卸載的元件。'); return }
  const missingLot = [...new Set(carrierDetails.value.map((row) => row.lotNo))].find((lotNo) => !findLot(lotNo))
  if (missingLot) { showError('找不到批次 ' + missingLot + ' 的現況；請先修正載具／批次關聯。'); return }
  carrierOutConfirm.value = true
}
function confirmCarrierOut(): void {
  if (!dev.value || !selectedCarrier.value) { carrierOutConfirm.value = false; showError('DEV 假資料模式停用或尚未選取載具。'); return }
  const carrierNo = selectedCarrier.value.carrierNo
  const removed = snapshot.value.details.filter((row) => row.carrierNo === carrierNo)
  const stamp = currentTime()
  snapshot.value.logs.push(...removed.map((row) => ({ ...row })))
  snapshot.value.details = snapshot.value.details.filter((row) => row.carrierNo !== carrierNo)
  for (const lotNo of new Set(removed.map((row) => row.lotNo))) {
    const lot = findLot(lotNo)
    if (lot) lot.carrierNo = null
  }
  selectedCarrier.value.carrierState = 0
  selectedPosition.value = null
  carrierOutConfirm.value = false
  updatedAt.value = stamp
  persist()
  showMessage('DEV 已模擬卸下載具 ' + carrierNo + '，並新增 ' + removed.length + ' 筆內容歷程。')
}

function beginCarrierChange(): void {
  clearFeedback()
  if (!selectedCarrier.value || selectedCarrier.value.carrierState !== 1) { showError('更換載具前，原載具必須是 In Use。'); return }
  if (selectedPosition.value === null) { showError('請先選取原載具位置列。'); return }
  const sourceRows = snapshot.value.details.filter((row) => row.carrierNo === selectedCarrierNo.value)
  if (!sourceRows.length) { showError('原載具沒有綁定內容，不需要更換。'); return }
  changeOpen.value = true
  orgCarrierNo.value = selectedCarrier.value.carrierNo
  orgCarrierType.value = selectedCarrier.value.carrierType
  newCarrierNo.value = ''
  newCarrierType.value = ''
  changeLotInput.value = [...new Set(sourceRows.map((row) => row.lotNo))].join(',')
  changeDraft.value = []
  changeSelectedPosition.value = null
}
function closeCarrierChange(): void { changeOpen.value = false; changeDraft.value = []; changeLotInput.value = ''; changeSelectedPosition.value = null }
function addLotToChange(): void {
  clearFeedback()
  const carrier = newCarrier.value
  const lotNo = changeLotInput.value.trim().toUpperCase()
  if (!dev.value) { showError('DEV 假資料模式停用。'); return }
  if (!orgCarrier.value) { showError('請先選擇原載具。'); return }
  if (!carrier) { showError('請先選擇新載具。'); return }
  if (!lotNo) { showError('LotNo 不可空白。'); return }
  if (changeDraft.value.some((row) => row.lotNo.toUpperCase() === lotNo)) { showError('批號 ' + lotNo + ' 已在新載具草稿。'); return }
  const lot = findLot(lotNo)
  if (!lot || lot.lotState !== 1) { showError('找不到可用的 LotState：' + lotNo + '。'); return }
  if (lot.status > 11) { showError('Lot ' + lotNo + ' 已進入庫存狀態（Status > 11）。'); return }
  if (lot.carrierNo || snapshot.value.details.some((row) => row.lotNo.toUpperCase() === lotNo)) { showError('Lot ' + lotNo + ' 已綁定載具。'); return }
  if (!lot.components.length) { showError('Lot ' + lotNo + ' 沒有元件。'); return }
  const empty = changeDraft.value.filter((row) => !row.lotNo)
  if (empty.length < lot.components.length) { showError('新載具剩餘位置 ' + empty.length + ' 小於元件數 ' + lot.components.length + '。'); return }
  for (const componentNo of lot.components) {
    const slot = changeDraft.value.find((row) => row.carrierPosition === lot.componentSlotMap?.[componentNo] && !row.lotNo)
      ?? changeDraft.value.find((row) => !row.lotNo)
    if (!slot) { showError('新載具位置不足，無法放入全部元件。'); return }
    slot.lotNo = lot.lotNo
    slot.contentId = lot.lotNo
    slot.componentNo = componentNo
    slot.bcip = props.currentUserNo || 'DEVUSER'
    slot.flag = 1
    slot.eventTime = currentTime()
  }
  changeLotInput.value = ''
  showMessage('已將 ' + lot.lotNo + ' 的 ' + lot.components.length + ' 個元件排入新載具草稿。')
}
function moveChange(position: number, direction: -1 | 1): void { changeDraft.value = swapContent(changeDraft.value, position, direction, true); changeSelectedPosition.value = position + direction }
function confirmCarrierChange(): void {
  clearFeedback()
  if (!dev.value) { showError('DEV 假資料模式停用。'); return }
  const original = orgCarrier.value
  const replacement = newCarrier.value
  if (!original) { showError('請先選擇原載具。'); return }
  if (!replacement || replacement.carrierNo === original.carrierNo) { showError('請選擇不同的空載具。'); return }
  if (snapshot.value.details.some((row) => row.carrierNo === replacement.carrierNo)) { showError('新載具已不是空載具，請重新查詢。'); return }
  const oldRows = snapshot.value.details.filter((row) => row.carrierNo === original.carrierNo)
  if (!oldRows.length) { showError('原載具沒有綁定內容。'); return }
  if (!changeBoundRows.value.length) { showError('請保留至少一筆有效批號內容。'); return }
  const stamp = currentTime()
  snapshot.value.logs.push(...oldRows.map((row) => ({ ...row })))
  snapshot.value.details = snapshot.value.details.filter((row) => row.carrierNo !== original.carrierNo)
  for (const row of oldRows) {
    const lot = findLot(row.lotNo)
    if (lot) {
      lot.carrierNo = null
      if (lot.componentSlotMap) delete lot.componentSlotMap[row.componentNo]
    }
  }
  for (const row of changeBoundRows.value) {
    const saved = { ...row, id: 'wp308-' + Date.now() + '-' + row.carrierPosition + '-' + Math.random().toString(36).slice(2, 6), carrierNo: replacement.carrierNo, bcip: props.currentUserNo || 'DEVUSER', flag: 0, eventTime: stamp }
    snapshot.value.details.push(saved)
    const lot = findLot(saved.lotNo)
    if (lot) {
      lot.carrierNo = replacement.carrierNo
      lot.componentSlotMap = { ...(lot.componentSlotMap ?? {}), [saved.componentNo]: saved.carrierPosition }
    }
  }
  original.carrierState = 0
  replacement.carrierState = 1
  selectedCarrierNo.value = replacement.carrierNo
  selectedPosition.value = null
  updatedAt.value = stamp
  persist()
  closeCarrierChange()
  showMessage('DEV 已模擬將 ' + original.carrierNo + ' 的內容更換至 ' + replacement.carrierNo + '。')
}
</script>

<template>
  <section class="carrier-management-page">
    <header class="page-heading">
      <div><span class="function-code">WP308 · CARRIER MANAGEMENT COMP</span><h1>載具管理_元件</h1></div>
      <div class="heading-tools"><span v-if="dev" class="dev-badge">DEV 假資料</span><button v-if="dev" class="quiet-button" type="button" @click="resetDev">還原假資料</button></div>
    </header>

    <fieldset class="query-box">
      <legend>載具查詢</legend>
      <label for="wp308-carrier-type">CarrierType</label>
      <div class="lookup-field"><input id="wp308-carrier-type" v-model="carrierTypeFilter" class="kd-input" autocomplete="off"><button type="button" aria-label="選擇載具類別" @click="openPicker('type-filter')">...</button></div>
      <label for="wp308-carrier-no">CarrierNo</label>
      <input id="wp308-carrier-no" v-model="carrierNoFilter" class="kd-input" autocomplete="off" placeholder="輸入開頭查詢"><span>%</span>
      <button class="query-button" type="button" @click="query">查詢 (Query)</button>
    </fieldset>

    <div class="main-layout">
      <section class="grid-panel carrier-panel">
        <div class="grid-title"><span>CarrierState</span><span>資料總筆數 {{ filteredCarriers.length }}</span></div>
        <div class="table-wrap"><table class="carrier-grid">
          <thead><tr><th>CarrierType</th><th>CarrierNo</th><th>Carrier Status</th></tr></thead>
          <tbody>
            <tr v-for="carrier in filteredCarriers" :key="carrier.carrierNo" :class="{ selected: carrier.carrierNo === selectedCarrierNo }" @click="selectCarrier(carrier.carrierNo)">
              <td>{{ carrier.carrierType }}</td><td>{{ carrier.carrierNo }}</td><td><span class="state-chip" :class="carrier.carrierState === 1 ? 'occupied' : 'empty'">{{ carrier.carrierState === 1 ? 'In Use' : 'Empty' }}</span></td>
            </tr>
            <tr v-if="filteredCarriers.length === 0"><td colspan="3" class="empty-state">沒有符合條件的載具</td></tr>
          </tbody>
        </table></div>
        <div class="grid-footer"><span>目前選取 {{ selectedCarrier?.carrierNo || '無' }}</span><span>{{ selectedCarrier?.description || '' }}</span></div>
      </section>

      <section class="grid-panel detail-panel">
        <div class="grid-title"><span>CarrierState Detail</span><span v-if="selectedCarrier">{{ carrierDetails.length }} / {{ carrierCapacity }} 個位置</span></div>
        <div class="table-wrap"><table class="carrier-grid">
          <thead><tr><th>CarrierPosition</th><th>LotNo</th><th>ComponentNo</th></tr></thead>
          <tbody>
            <tr v-for="slot in carrierSlots" :key="slot.position" :class="{ selected: slot.position === selectedPosition }" @click="selectPosition(slot.position)">
              <td class="numeric">{{ slot.position }}</td><td>{{ slot.detail?.lotNo || '' }}</td><td>{{ slot.detail?.componentNo || '' }}</td>
            </tr>
            <tr v-if="!selectedCarrier"><td colspan="3" class="empty-state">請先選取載具</td></tr>
          </tbody>
        </table></div>
        <div class="grid-footer"><span>載具容量 {{ carrierCapacity }}</span><span>空位 {{ Math.max(0, carrierCapacity - carrierDetails.length) }}</span></div>
      </section>

      <aside class="action-rail" aria-label="載具操作">
        <button class="primary-button" type="button" :disabled="!dev" @click="beginCarrierIn">Carrier In</button>
        <button type="button" :disabled="!dev || !selectedCarrier || selectedPosition === null || carrierDetails.length === 0" @click="requestCarrierOut">Carrier Out</button>
        <button type="button" :disabled="!dev || !selectedCarrier || selectedCarrier.carrierState !== 1 || selectedPosition === null" @click="beginCarrierChange">Change Carrier</button>
        <div class="action-note">選取載具與位置列後執行操作</div>
      </aside>
    </div>

    <p v-if="actionError" class="feedback error" role="alert">{{ actionError }}</p>
    <p v-else-if="actionMessage" class="feedback success" role="status">{{ actionMessage }}</p>
    <div class="page-footer-actions"><button class="quiet-button close-button" type="button" @click="emit('close')">關閉 (Close)</button></div>
    <footer class="status-bar"><span>{{ dev ? 'DEV 假資料' : '唯讀畫面' }}</span><span>操作者 {{ props.currentUserNo || 'DEVUSER' }}</span><span class="status-spacer">可在 WP302 載具現況管理檢視共用假資料</span><span>{{ updatedAt }}</span></footer>

    <div v-if="inOpen" class="modal-backdrop" role="presentation" @click.self="closeCarrierIn">
      <section class="carrier-dialog in-dialog" role="dialog" aria-modal="true" aria-labelledby="wp308-in-title">
        <h2 id="wp308-in-title">載具管理元件 (Carrier In)</h2>
        <div class="form-line">
          <label for="wp308-in-no">CarrierNo</label><input id="wp308-in-no" :value="inCarrierNo" class="kd-input" readonly><button type="button" @click="openPicker('in-carrier')">...</button>
          <label for="wp308-in-type">CarrierType</label><input id="wp308-in-type" :value="inCarrierType" class="kd-input" readonly>
        </div>
        <fieldset class="lot-entry"><legend>載具內容</legend><label for="wp308-in-lot">LotNo</label><input id="wp308-in-lot" v-model="inLotInput" class="kd-input" autocomplete="off" @keydown.enter.prevent="addLotToIn"><button class="primary-button" type="button" @click="addLotToIn">加入批號</button></fieldset>
        <div class="draft-layout">
          <div class="table-wrap draft-table-wrap"><table class="carrier-grid">
            <thead><tr><th>CarrierPosition</th><th>LotNo</th><th>ComponentNo</th></tr></thead>
            <tbody><tr v-for="row in inDraft" :key="row.carrierPosition" :class="{ occupied: row.lotNo !== '', selected: row.carrierPosition === inSelectedPosition }" @click="inSelectedPosition = row.carrierPosition"><td class="numeric">{{ row.carrierPosition }}</td><td>{{ row.lotNo }}</td><td>{{ row.componentNo }}</td></tr></tbody>
          </table></div>
          <div class="move-buttons"><button type="button" :disabled="inSelectedPosition === null || inSelectedPosition <= 1" @click="inSelectedPosition !== null && moveIn(inSelectedPosition, -1)">上移</button><button type="button" :disabled="inSelectedPosition === null || inSelectedPosition >= inDraft.length" @click="inSelectedPosition !== null && moveIn(inSelectedPosition, 1)">下移</button></div>
        </div>
        <p v-if="actionError" class="feedback error" role="alert">{{ actionError }}</p><p v-else-if="actionMessage" class="feedback success" role="status">{{ actionMessage }}</p>
        <div class="dialog-actions"><button type="button" @click="closeCarrierIn">取消</button><button class="primary-button" type="button" :disabled="!dev" @click="confirmCarrierIn">確認</button></div>
      </section>
    </div>

    <div v-if="changeOpen" class="modal-backdrop" role="presentation" @click.self="closeCarrierChange">
      <section class="carrier-dialog change-dialog" role="dialog" aria-modal="true" aria-labelledby="wp308-change-title">
        <h2 id="wp308-change-title">更換載具 (Change Carrier)</h2>
        <div class="form-grid">
          <label for="wp308-org-no">OrgCarrierNo</label><input id="wp308-org-no" :value="orgCarrierNo" class="kd-input" readonly><button type="button" @click="openPicker('org-carrier')">...</button>
          <label for="wp308-org-type">OrgCarrierType</label><input id="wp308-org-type" :value="orgCarrierType" class="kd-input" readonly>
          <label for="wp308-new-no">NewCarrierNo</label><input id="wp308-new-no" :value="newCarrierNo" class="kd-input" readonly><button type="button" :disabled="!orgCarrier" @click="openPicker('new-carrier')">...</button>
          <label for="wp308-new-type">NewCarrierType</label><input id="wp308-new-type" :value="newCarrierType" class="kd-input" readonly>
        </div>
        <fieldset class="lot-entry change-lot-entry"><legend>新增批號內容</legend><label for="wp308-change-lot">LotNo</label><input id="wp308-change-lot" v-model="changeLotInput" class="kd-input" autocomplete="off" @keydown.enter.prevent="addLotToChange"><button class="primary-button" type="button" @click="addLotToChange">加入批號</button></fieldset>
        <div class="draft-layout">
          <div class="table-wrap draft-table-wrap"><table class="carrier-grid">
            <thead><tr><th>CarrierPosition</th><th>LotNo</th><th>ComponentNo</th></tr></thead>
            <tbody><tr v-for="row in changeDraft" :key="row.carrierPosition" :class="{ occupied: row.lotNo !== '', selected: row.carrierPosition === changeSelectedPosition }" @click="changeSelectedPosition = row.carrierPosition"><td class="numeric">{{ row.carrierPosition }}</td><td>{{ row.lotNo }}</td><td>{{ row.componentNo }}</td></tr></tbody>
          </table></div>
          <div class="move-buttons"><button type="button" :disabled="changeSelectedPosition === null || changeSelectedPosition <= 1" @click="changeSelectedPosition !== null && moveChange(changeSelectedPosition, -1)">上移</button><button type="button" :disabled="changeSelectedPosition === null || changeSelectedPosition >= changeDraft.length" @click="changeSelectedPosition !== null && moveChange(changeSelectedPosition, 1)">下移</button></div>
        </div>
        <p v-if="actionError" class="feedback error" role="alert">{{ actionError }}</p><p v-else-if="actionMessage" class="feedback success" role="status">{{ actionMessage }}</p>
        <div class="dialog-actions"><button type="button" @click="closeCarrierChange">取消</button><button class="primary-button" type="button" :disabled="!dev" @click="confirmCarrierChange">確認</button></div>
      </section>
    </div>

    <div v-if="carrierOutConfirm" class="modal-backdrop top-modal" role="presentation" @click.self="carrierOutConfirm = false">
      <section class="confirm-dialog" role="dialog" aria-modal="true" aria-labelledby="wp308-out-title">
        <h2 id="wp308-out-title">卸下載具確認</h2><p>確定卸下載具 {{ selectedCarrierNo }} 的全部元件？系統會記錄載具內容歷程。</p>
        <div class="dialog-actions"><button type="button" @click="carrierOutConfirm = false">取消</button><button class="danger-button" type="button" @click="confirmCarrierOut">確定</button></div>
      </section>
    </div>

    <div v-if="pickerMode" class="modal-backdrop picker-backdrop" role="presentation" @click.self="closePicker">
      <section class="picker-dialog" role="dialog" aria-modal="true" aria-labelledby="wp308-picker-title">
        <h2 id="wp308-picker-title">{{ pickerMode === 'type-filter' ? '選擇 CarrierType' : pickerMode === 'in-carrier' ? '選擇載具' : pickerMode === 'org-carrier' ? '選擇原載具' : '選擇空載具' }}</h2>
        <p v-if="pickerMode === 'new-carrier' && orgCarrier">候選載具需為空載具，且 IssueState = 2。{{ carrierUseLayer(orgCarrier) === 1 ? '多層載具另需 UseLayer = 1。' : '' }}</p>
        <p v-if="pickerError" class="feedback error" role="alert">{{ pickerError }}</p>
        <div class="picker-list"><button v-for="option in pickerOptions" :key="option.value" type="button" @click="chooseCarrier(option)">{{ option.label }}</button><p v-if="pickerOptions.length === 0" class="empty-state">沒有符合條件的資料</p></div>
        <div class="dialog-actions"><button type="button" @click="closePicker">關閉</button></div>
      </section>
    </div>
  </section>
</template>

<style scoped>
.carrier-management-page { min-height: 0; height: 100%; display: flex; flex-direction: column; gap: 8px; padding: 12px; color: #292929; background: #fff; overflow: auto; }
.page-heading { display: flex; align-items: center; justify-content: space-between; min-height: 38px; }
.page-heading h1 { display: inline; margin: 0 0 0 10px; font-size: 20px; font-weight: 600; }
.function-code { color: #6d6d6d; font-size: 11px; letter-spacing: .04em; }
.heading-tools, .lookup-field, .form-line, .dialog-actions { display: flex; align-items: center; gap: 8px; }
.dev-badge { border: 1px solid #b48a29; border-radius: 3px; padding: 4px 8px; color: #76580e; background: #fff3c7; font-size: 12px; }
.query-box { display: flex; align-items: center; gap: 8px; min-height: 64px; margin: 0; border: 1px solid #aaa; padding: 5px 10px 8px; }
.query-box legend { padding: 0 5px; color: #444; font-weight: 600; }
.query-box label, .form-line label { white-space: nowrap; }
.kd-input { width: 175px; height: 30px; border: 1px solid #929292; border-radius: 0; padding: 4px 7px; color: #292929; background: #fff0ad; font: inherit; }
.kd-input[readonly] { background: #eee; }
.kd-input:focus { outline: 2px solid #c99a30; }
.lookup-field input { width: 170px; }
button { min-height: 32px; border: 1px solid #aaa; border-radius: 3px; padding: 4px 10px; color: #333; background: #f8f8f8; font: inherit; white-space: nowrap; cursor: pointer; }
button:disabled { cursor: not-allowed; color: #999 !important; border-color: #ccc !important; background: #eee !important; }
.primary-button { border-color: #d58b16; color: #fff; background: #f59a18; }
.query-button { min-width: 80px; }
.main-layout { min-height: 0; flex: 1 1 auto; display: grid; grid-template-columns: minmax(300px, 1fr) minmax(360px, 1fr) 100px; gap: 10px; }
.grid-panel { min-height: 0; display: flex; flex-direction: column; border: 1px solid #aaa; }
.grid-title { display: flex; justify-content: space-between; gap: 8px; padding: 7px 9px; color: #444; background: #f3f3f3; border-bottom: 1px solid #aaa; font-weight: 600; }
.table-wrap { min-height: 0; flex: 1 1 auto; overflow: auto; }
.carrier-grid { width: 100%; border-collapse: collapse; font-size: 12px; }
.carrier-grid th { position: sticky; top: 0; z-index: 1; padding: 7px 6px; border: 1px solid #c5c5c5; color: #303030; background: #e8e8e8; text-align: left; white-space: nowrap; }
.carrier-grid td { padding: 6px; border: 1px solid #d1d1d1; background: #fff0ad; white-space: nowrap; }
.carrier-grid tbody tr { cursor: pointer; }
.carrier-grid tr.selected td { outline: 1px solid #a77a16; outline-offset: -1px; background: #ffe18a; }
.carrier-grid tr.occupied td { background: #fff0ad; }
.numeric { text-align: right; }
.state-chip { display: inline-block; min-width: 50px; border-radius: 2px; padding: 2px 6px; text-align: center; }
.state-chip.occupied { color: #7a4800; background: #ffe2a0; }
.state-chip.empty { color: #555; background: #e9e9e9; }
.empty-state { padding: 14px !important; color: #777; text-align: center; background: #fafafa !important; }
.grid-footer { display: flex; justify-content: space-between; gap: 8px; padding: 5px 9px; color: #666; border-top: 1px solid #c5c5c5; font-size: 12px; }
.action-rail { display: flex; flex-direction: column; align-items: stretch; gap: 8px; }
.action-rail button { min-height: 34px; }
.action-note { margin-top: 4px; color: #777; font-size: 11px; line-height: 1.5; }
.feedback { flex: 0 0 auto; margin: 0; padding: 7px 10px; border: 1px solid; font-size: 13px; }
.feedback.error { border-color: #d5a2a2; color: #812c2c; background: #fff0f0; }
.feedback.success { border-color: #b5c9a8; color: #3e6631; background: #f1faeb; }
.page-footer-actions { display: flex; justify-content: flex-end; }
.quiet-button { min-height: 28px; font-size: 12px; }
.close-button { min-width: 88px; }
.status-bar { min-height: 24px; display: flex; align-items: center; gap: 16px; padding: 2px 7px; border: 1px solid #bbb; color: #555; background: #f5f5f5; font-size: 11px; }
.status-spacer { flex: 1; }
.modal-backdrop { position: fixed; inset: 0; z-index: 1000; display: grid; place-items: center; padding: 18px; background: rgb(0 0 0 / 34%); }
.top-modal { z-index: 1005; }
.picker-backdrop { z-index: 1010; }
.carrier-dialog, .picker-dialog, .confirm-dialog { max-height: min(92vh, 720px); overflow: auto; border: 1px solid #aaa; border-radius: 4px; padding: 16px; background: #fff; box-shadow: 0 8px 30px rgb(0 0 0 / 20%); }
.in-dialog { width: min(655px, 100%); }
.change-dialog { width: min(832px, 100%); }
.carrier-dialog h2, .picker-dialog h2, .confirm-dialog h2 { margin: 0 0 12px; font-size: 17px; }
.form-line { flex-wrap: wrap; margin-bottom: 10px; }
.form-line .kd-input { width: 150px; }
.form-line label { min-width: 82px; text-align: right; }
.lot-entry { display: flex; align-items: center; gap: 8px; margin: 0 0 10px; border: 1px solid #aaa; padding: 4px 8px 8px; }
.lot-entry legend { padding: 0 5px; color: #444; font-weight: 600; }
.lot-entry label { min-width: 48px; }
.draft-layout { display: grid; grid-template-columns: minmax(0, 1fr) 76px; gap: 8px; min-height: 200px; }
.draft-table-wrap { max-height: 260px; border: 1px solid #aaa; }
.move-buttons { display: flex; flex-direction: column; gap: 8px; }
.dialog-actions { justify-content: flex-end; margin-top: 12px; }
.change-dialog .form-grid { display: grid; grid-template-columns: 110px 1fr 35px 110px 1fr; align-items: center; gap: 8px; margin-bottom: 10px; }
.change-dialog .form-grid label { text-align: right; white-space: nowrap; }
.change-dialog .form-grid .kd-input { width: 100%; min-width: 0; }
.change-lot-entry .kd-input { flex: 1; }
.picker-dialog { width: min(520px, 100%); }
.picker-dialog p { margin: 0 0 10px; color: #555; font-size: 13px; }
.picker-list { max-height: 55vh; overflow: auto; display: flex; flex-direction: column; gap: 5px; }
.picker-list button { text-align: left; }
.confirm-dialog { width: min(430px, 100%); }
.confirm-dialog p { margin: 0; line-height: 1.6; }
.danger-button { border-color: #a54236; color: #fff; background: #b64a42; }
@media (max-width: 1000px) {
  .carrier-management-page { height: auto; min-height: 100%; }
  .query-box { flex-wrap: wrap; }
  .main-layout { grid-template-columns: 1fr; }
  .grid-panel { min-height: 280px; }
  .action-rail { flex-direction: row; flex-wrap: wrap; }
  .action-note { flex-basis: 100%; }
  .change-dialog .form-grid { grid-template-columns: 100px minmax(0, 1fr) 35px; }
  .status-bar { flex-wrap: wrap; }
}
</style>
