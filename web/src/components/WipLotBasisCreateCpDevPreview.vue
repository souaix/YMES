<script setup lang="ts">
import { computed, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'

type Mode = 'add' | 'edit' | 'copy'
type DetailTab = 'basic-ecn' | 'process-ecn' | 'op-ecn' | 'component' | 'property' | 'carrier'
type HotLot = 0 | 1 | 2
type Ecn = { no: string; type: string }
type Mo = {
  moNo: string; moClass: string; moState: 3 | 6 | 2; roNo: string; itemNo: number
  productNo: string; productVersion: string; engNo: string; engVersion: string
  customerNo: string; customerLotNo: string; moTypeNo: string; moTypeName: string
  inputUnitNo: string; moQty: number; unreleaseQty: number; isWafer: boolean
  chooseWafer: boolean; haveComponent: boolean; haveMaterialState: boolean
  binControl: boolean; materialConfirmed: boolean; basicEcn: Ecn; processEcn: Ecn
  opEcn: Ecn; processNo: string; processVersion: string; psNo: string; psOrder: number
}
type ComponentRow = {
  componentNo: string; productNo: string; goodQty: number; unitNo: string
  materialLotNo: string; selected: boolean; carrierNo: string; carrierPosition: number
}
type PropertyRow = {
  propertyNo: string; propertyValue: string; dataType: '文字' | '數字' | '日期'
  tableType: number; system: boolean
}
type LotRow = {
  baseLotNo: string; orgLotNo: string; roNo: string; itemNo: number; returnNo: string
  moNo: string; productNo: string; productVersion: string; engNo: string; engVersion: string
  deviceNo: string; customerNo: string; inputQty: number; inputUnitNo: string
  customerLotNo: string; priority: number; hotLot: HotLot; description: string
  creator: string; createDate: string; lotState: 0 | 1; ecnBasic: Ecn; ecnProcess: Ecn
  opEcns: { opNo: string; ecn: Ecn }[]; ecnValid: boolean
  processNo: string; processVersion: string; psNo: string; psOrder: number; haveComponent: boolean
  components: ComponentRow[]; skippedOps: string[]; properties: PropertyRow[]
}

const props = withDefaults(defineProps<{ currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '' })
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()
const currentUser = computed(() => props.currentUserNo || 'DEVUSER01')
const tabOptions: { key: DetailTab; label: string }[] = [
  { key: 'basic-ecn', label: 'Basic ECN' }, { key: 'process-ecn', label: 'Process ECN' },
  { key: 'op-ecn', label: 'OP ECN' }, { key: 'component', label: 'Component' },
  { key: 'property', label: 'Lot Property' }, { key: 'carrier', label: 'Carrier' },
]
const mos = ref<Mo[]>([
  { moNo: 'DEV-CP-MO-001', moClass: 'PKG', moState: 3, roNo: 'DEV-CP-RO-1001', itemNo: 1, productNo: 'DEV-CP-PRD-1001', productVersion: '02', engNo: 'DEV-ENG-CP-01', engVersion: '01', customerNo: 'DEV-CUST-01', customerLotNo: 'CUST-LOT-1001', moTypeNo: 'DEV-MT-CP', moTypeName: 'CP 生產工單', inputUnitNo: 'WAFER', moQty: 120, unreleaseQty: 2, isWafer: true, chooseWafer: true, haveComponent: true, haveMaterialState: true, binControl: true, materialConfirmed: true, basicEcn: { no: 'DEV-ECN-B-100', type: 'BASIC' }, processEcn: { no: 'DEV-ECN-P-210', type: 'PROCESS' }, opEcn: { no: 'DEV-ECN-OP-10', type: 'OP' }, processNo: 'DEV-CP-PROC-01', processVersion: '02', psNo: 'DEV-CP-PS-01', psOrder: 10 },
  { moNo: 'DEV-CP-MO-002', moClass: 'PKG', moState: 6, roNo: 'DEV-CP-RO-1002', itemNo: 2, productNo: 'DEV-CP-PRD-1002', productVersion: '01', engNo: 'DEV-ENG-CP-02', engVersion: '01', customerNo: 'DEV-CUST-02', customerLotNo: 'CUST-LOT-1002', moTypeNo: 'DEV-MT-CP', moTypeName: 'CP 生產工單', inputUnitNo: 'WAFER', moQty: 8, unreleaseQty: 0, isWafer: true, chooseWafer: false, haveComponent: false, haveMaterialState: true, binControl: false, materialConfirmed: true, basicEcn: { no: 'DEV-ECN-B-220', type: 'BASIC' }, processEcn: { no: 'DEV-ECN-P-220', type: 'PROCESS' }, opEcn: { no: 'DEV-ECN-OP-22', type: 'OP' }, processNo: 'DEV-CP-PROC-02', processVersion: '01', psNo: 'DEV-CP-PS-02', psOrder: 10 },
  { moNo: 'DEV-CP-MO-003', moClass: 'PKG', moState: 2, roNo: 'DEV-CP-RO-1003', itemNo: 1, productNo: 'DEV-CP-PRD-1003', productVersion: '01', engNo: 'DEV-ENG-CP-03', engVersion: '01', customerNo: 'DEV-CUST-03', customerLotNo: 'CUST-LOT-1003', moTypeNo: 'DEV-MT-CP', moTypeName: 'CP 生產工單', inputUnitNo: 'WAFER', moQty: 20, unreleaseQty: 20, isWafer: false, chooseWafer: false, haveComponent: false, haveMaterialState: false, binControl: false, materialConfirmed: false, basicEcn: { no: 'DEV-ECN-B-300', type: 'BASIC' }, processEcn: { no: 'DEV-ECN-P-300', type: 'PROCESS' }, opEcn: { no: 'DEV-ECN-OP-30', type: 'OP' }, processNo: 'DEV-CP-PROC-03', processVersion: '01', psNo: 'DEV-CP-PS-03', psOrder: 10 },
  { moNo: 'DEV-ASM-MO-001', moClass: 'ASSY', moState: 3, roNo: 'DEV-ASM-RO-01', itemNo: 1, productNo: 'DEV-ASM-PRD-1', productVersion: '01', engNo: 'DEV-ENG-A-1', engVersion: '01', customerNo: 'DEV-CUST-01', customerLotNo: 'DEV-CUST-LOT', moTypeNo: 'DEV-MT-A', moTypeName: '組裝工單', inputUnitNo: 'PCS', moQty: 500, unreleaseQty: 300, isWafer: false, chooseWafer: false, haveComponent: false, haveMaterialState: false, binControl: false, materialConfirmed: false, basicEcn: { no: 'DEV-ECN-A-1', type: 'BASIC' }, processEcn: { no: 'DEV-ECN-A-2', type: 'PROCESS' }, opEcn: { no: 'DEV-ECN-A-3', type: 'OP' }, processNo: 'DEV-ASM-PROC', processVersion: '01', psNo: 'DEV-ASM-PS', psOrder: 10 },
])

function makeComponents(mo: Mo, count: number, startIndex = 0): ComponentRow[] {
  return Array.from({ length: count }, (_, index) => ({
    componentNo: 'DEV-COMP-' + mo.moNo.slice(-3) + '-' + String(startIndex + index + 1).padStart(3, '0'),
    productNo: 'DEV-WAFER-' + String(startIndex + index + 1).padStart(3, '0'),
    goodQty: 1, unitNo: 'WAFER', materialLotNo: mo.chooseWafer ? 'DEV-MTL-CP-001' : '',
    selected: true, carrierNo: '', carrierPosition: index + 1,
  }))
}
function nextComponentStart(mo: Mo): number {
  const prefix = 'DEV-COMP-' + mo.moNo.slice(-3) + '-'
  return Math.max(0, ...lotStore.value.filter(row => row.moNo === mo.moNo)
    .flatMap(row => row.components.map(item => Number(item.componentNo.startsWith(prefix) ? item.componentNo.slice(prefix.length) : 0) || 0)))
}
function makeComponentsForNewLot(mo: Mo, count: number): ComponentRow[] {
  return makeComponents(mo, count, nextComponentStart(mo))
}
function lotFromMo(mo: Mo, lotNo: string, ecnValid = true, componentStart = 0): LotRow {
  return {
    baseLotNo: lotNo, orgLotNo: lotNo, roNo: mo.roNo, itemNo: mo.itemNo, returnNo: 'N/A',
    moNo: mo.moNo, productNo: mo.productNo, productVersion: mo.productVersion, engNo: mo.engNo,
    engVersion: mo.engVersion, deviceNo: 'DEV-DEVICE-CP', customerNo: mo.customerNo,
    inputQty: 2, inputUnitNo: mo.inputUnitNo, customerLotNo: mo.customerLotNo, priority: 50,
    hotLot: 0, description: '', creator: 'DEVUSER01', createDate: '2026-11-22 08:30:00',
    lotState: 0, ecnBasic: { ...mo.basicEcn }, ecnProcess: { ...mo.processEcn },
    opEcns: [{ opNo: 'DEV-CP-OP-010', ecn: { ...mo.opEcn } }], ecnValid,
    processNo: mo.processNo, processVersion: mo.processVersion, psNo: mo.psNo,
    psOrder: mo.psOrder, haveComponent: mo.haveComponent,
    components: mo.haveComponent ? makeComponents(mo, 2, componentStart) : [], skippedOps: ['DEV-CP-OP-099'],
    properties: [
      { propertyNo: 'DEV-LOT-GRADE', propertyValue: 'A', dataType: '文字', tableType: 0, system: false },
      { propertyNo: 'DEV-LOT-CP-DATE', propertyValue: '2026-11-21', dataType: '日期', tableType: 1, system: true },
    ],
  }
}
const initialLots: LotRow[] = [
  lotFromMo(mos.value[0]!, 'DEV-CPLOT-001'),
  lotFromMo(mos.value[0]!, 'DEV-CPLOT-002', false, 2),
  lotFromMo(mos.value[1]!, 'DEV-CPLOT-003'),
]
const storageKey = 'ymes.dev.wpp01-lot-basis-cp.v1'
function loadLots(): LotRow[] {
  if (dev.value) {
    try {
      const saved = localStorage.getItem(storageKey)
      if (saved) return JSON.parse(saved) as LotRow[]
    } catch { /* use the deterministic DEV fixtures */ }
  }
  return initialLots.map(row => JSON.parse(JSON.stringify(row)) as LotRow)
}
const lotStore = ref<LotRow[]>(loadLots())
const selectedMoNo = ref('')
const moText = ref('')
const moDialog = ref(false)
const moSearch = ref('')
const appliedLotSearch = ref('')
const lotSearch = ref('')
const selectedLotNos = ref<string[]>([])
const activeLotNo = ref('')
const pageError = ref('')
const notice = ref('請先選擇工單，清單只顯示 LotState=0 的待下線批次。')
const actionLog = ref<{ id: number; time: string; action: string; lot: string; result: string; detail: string }[]>([])
const nextLogId = ref(1)
const mode = ref<Mode | null>(null)
const detailTab = ref<DetailTab>('basic-ecn')
const draft = ref<LotRow | null>(null)
const formError = ref('')
const autoSerial = ref(true)
const autoRelease = ref(false)
const releaseScenario = ref<'success' | 'service-error-second'>('success')
const deleteScenario = ref<'success' | 'service-error-second'>('success')
const busyAction = ref(false)

const eligibleMOs = computed(() => {
  const query = moSearch.value.trim().toUpperCase()
  return mos.value.filter(mo => mo.moClass === 'PKG' && [3, 6].includes(mo.moState)
    && (mo.unreleaseQty > 0 || lotStore.value.some(row => row.moNo === mo.moNo && row.lotState === 0))
    && (!query || [mo.moNo, mo.roNo, mo.productNo, mo.customerNo].some(value => value.toUpperCase().includes(query))))
})
const selectedMo = computed(() => mos.value.find(mo => mo.moNo === selectedMoNo.value) ?? null)
const rows = computed(() => lotStore.value
  .filter(row => row.moNo === selectedMoNo.value && row.lotState === 0
    && (!appliedLotSearch.value || [row.baseLotNo, row.orgLotNo, row.customerLotNo].some(value => value.toUpperCase().includes(appliedLotSearch.value))))
  .sort((a, b) => a.baseLotNo.localeCompare(b.baseLotNo)))
const activeLot = computed(() => rows.value.find(row => row.baseLotNo === activeLotNo.value) ?? null)
const selectedRows = computed(() => rows.value.filter(row => selectedLotNos.value.includes(row.baseLotNo)))
const canAdd = computed(() => !!selectedMo.value && selectedMo.value.unreleaseQty > 0)
const selectedComponentCount = computed(() => draft.value?.components.filter(row => row.selected).length ?? 0)
const hasCarrierBinding = computed(() => !!draft.value?.components.some(row => row.carrierNo.trim()))

function nowText(): string { return new Date().toLocaleString('zh-TW', { hour12: false }) }
function logAction(action: string, lot: string, result: string, detail: string): void {
  actionLog.value.unshift({ id: nextLogId.value++, time: nowText(), action, lot, result, detail })
}
function persist(next: LotRow[]): boolean {
  if (!dev.value) return false
  try {
    localStorage.setItem(storageKey, JSON.stringify(next))
    lotStore.value = next
    return true
  } catch {
    pageError.value = 'DEV 假資料無法寫入此瀏覽器的 localStorage。'
    return false
  }
}
function queryLots(): void {
  pageError.value = ''
  if (!selectedMoNo.value) {
    pageError.value = '請使用 ... 選取工單後查詢。'
    return
  }
  refreshLotList()
}
function refreshLotList(): void {
  if (!selectedMoNo.value) return
  appliedLotSearch.value = lotSearch.value.trim().toUpperCase()
  selectedLotNos.value = []
  activeLotNo.value = ''
  notice.value = '已讀取 ' + rows.value.length + ' 筆 LotState=0 的待下線批次（DEV）。'
}
function chooseMO(mo: Mo): void {
  selectedMoNo.value = mo.moNo
  moText.value = mo.moNo
  moDialog.value = false
  lotSearch.value = ''
  queryLots()
}
function applyMOText(): void {
  const text = moText.value.trim().toUpperCase()
  if (!text) {
    selectedMoNo.value = ''
    selectedLotNos.value = []
    activeLotNo.value = ''
    pageError.value = ''
    return
  }
  const match = mos.value.find(mo => mo.moNo.toUpperCase() === text && mo.moClass === 'PKG'
    && [3, 6].includes(mo.moState)
    && (mo.unreleaseQty > 0 || lotStore.value.some(row => row.moNo === mo.moNo && row.lotState === 0)))
  if (match) chooseMO(match)
  else pageError.value = '此工單不在 CP 可選的已發放工單（MOState 3／6）清單。'
}
function setRowSelected(row: LotRow, checked: boolean): void {
  activeLotNo.value = row.baseLotNo
  selectedLotNos.value = checked
    ? [...new Set([...selectedLotNos.value, row.baseLotNo])]
    : selectedLotNos.value.filter(value => value !== row.baseLotNo)
}
function selectAll(checked: boolean): void {
  selectedLotNos.value = checked ? rows.value.map(row => row.baseLotNo) : []
  activeLotNo.value = selectedLotNos.value[0] ?? ''
}
function selectActive(row: LotRow): void {
  activeLotNo.value = row.baseLotNo
}
function cloneLot(row: LotRow): LotRow { return JSON.parse(JSON.stringify(row)) as LotRow }
function currentMOFor(row: LotRow | null): Mo | null {
  return row ? mos.value.find(mo => mo.moNo === row.moNo) ?? null : selectedMo.value
}
function createDraft(mo: Mo, row?: LotRow): LotRow {
  if (row) return cloneLot(row)
  const base = lotFromMo(mo, '')
  base.inputQty = mo.unreleaseQty
  base.components = mo.haveComponent ? makeComponentsForNewLot(mo, Math.max(0, vbCLng(base.inputQty))) : []
  base.creator = currentUser.value
  base.createDate = nowText()
  base.properties = []
  return base
}
function openForm(nextMode: Mode): void {
  pageError.value = ''
  const row = activeLot.value
  const mo = nextMode === 'add' ? selectedMo.value : currentMOFor(row)
  if (!mo) {
    pageError.value = nextMode === 'add' ? '請先選擇工單。' : '請先選取一筆批次。'
    return
  }
  if (nextMode === 'add' && mo.unreleaseQty <= 0) {
    pageError.value = '工單未下線量需大於 0 才能新增生產批。'
    return
  }
  if (nextMode !== 'add' && !row) {
    pageError.value = 'Edit／Copy 使用目前作用列；請先點選單一批次。'
    return
  }
  if (nextMode === 'edit' && row?.lotState !== 0) {
    pageError.value = '只可修改尚未下線的批次。'
    return
  }
  if (mo.chooseWafer && mo.isWafer && !mo.materialConfirmed) {
    pageError.value = '工單必須挑片且領料尚未確認，來源會阻止開批。'
    return
  }
  mode.value = nextMode
  detailTab.value = 'basic-ecn'
  formError.value = ''
  draft.value = createDraft(mo, row ?? undefined)
  if (nextMode === 'copy') {
    draft.value.baseLotNo = ''
    draft.value.orgLotNo = row!.orgLotNo
    draft.value.lotState = 0
    draft.value.creator = currentUser.value
    draft.value.createDate = nowText()
    draft.value.ecnValid = true
    draft.value.components = mo.haveComponent ? makeComponentsForNewLot(mo, Math.max(0, vbCLng(draft.value.inputQty))) : []
  }
  if (!mo.chooseWafer && mo.isWafer && !mo.haveComponent && mo.haveMaterialState) {
    if (mo.binControl) {
      draft.value.components = makeComponentsForNewLot(mo, Math.min(2, Math.max(0, vbCLng(draft.value.inputQty))))
      notice.value = 'DEV 模擬由 wafer summary 產生工單元件，因啟用 ID Bin Control。'
    } else {
      notice.value = 'DEV 使用 FT 生產批開立子窗分支（來源在此條件下切換）。'
    }
  }
}
function closeForm(): void { draft.value = null; mode.value = null; formError.value = '' }
function getNextLotNo(): void {
  const mo = currentMOFor(draft.value)
  if (!mo || !draft.value) return
  const prefix = 'DEV-CPLOT-' + mo.moNo.slice(-3) + '-'
  const next = Math.max(0, ...lotStore.value.filter(row => row.baseLotNo.startsWith(prefix)
    ).map(row => Number(row.baseLotNo.slice(prefix.length)) || 0)) + 1
  draft.value.baseLotNo = prefix + String(next).padStart(3, '0')
}
function addComponent(): void {
  if (!draft.value) return
  const mo = currentMOFor(draft.value)
  const componentIndex = mo ? nextComponentStart(mo) + draft.value.components.length + 1 : draft.value.components.length + 1
  draft.value.components.push({
    componentNo: 'DEV-COMP-' + (mo?.moNo.slice(-3) ?? 'NEW') + '-' + String(componentIndex).padStart(3, '0'),
    productNo: 'DEV-WAFER-' + String(componentIndex).padStart(3, '0'), goodQty: 1, unitNo: 'WAFER',
    materialLotNo: currentMOFor(draft.value)?.chooseWafer ? 'DEV-MTL-CP-001' : '',
    selected: true, carrierNo: '', carrierPosition: draft.value.components.length + 1,
  })
}
function addProperty(): void {
  draft.value?.properties.push({ propertyNo: '', propertyValue: '', dataType: '文字', tableType: 0, system: false })
}
function vbCLng(value: number): number {
  const lower = Math.floor(value)
  const fraction = value - lower
  if (fraction < 0.5) return lower
  if (fraction > 0.5) return lower + 1
  return lower % 2 === 0 ? lower : lower + 1
}
function validateDraft(): string {
  const row = draft.value
  if (!row) return '沒有可保存的批次資料。'
  if (!row.baseLotNo.trim()) return 'BaseLotNo 不可空白；請取得 DEV 序號或自行輸入。'
  if (!row.roNo.trim() || !String(row.itemNo) || !row.moNo.trim() || !row.productNo.trim() || !row.productVersion.trim()) return 'RO、Item、MO、Product 與版本必須有值。'
  if (!Number.isFinite(row.inputQty) || row.inputQty <= 0) return 'InputQty 必須大於 0。'
  if (!row.inputUnitNo.trim()) return 'InputUnitNo 不可空白。'
  if (!Number.isInteger(row.priority) || row.priority < 0 || row.priority > 99) return 'Priority 須介於 0 到 99。'
  if (!row.customerLotNo.trim()) return 'CustomerLotNo 不可空白。'
  if (autoSerial.value && mode.value !== 'edit' && !row.baseLotNo.startsWith('DEV-CPLOT-')) return 'DEV 自動序號格式不符，請重新取得批號。'
  if (mode.value !== 'edit' && lotStore.value.some(item => item.baseLotNo.toUpperCase() === row.baseLotNo.trim().toUpperCase())) return 'BaseLotNo 已存在。'
  if (row.haveComponent) {
    if (row.components.length === 0) return '工單含元件時至少須載入元件清單。'
    if (row.components.filter(item => item.selected).length !== vbCLng(row.inputQty)) return '勾選元件數須符合來源 CLng(InputQty) 計算的元件筆數。'
    const mo = currentMOFor(row)
    if (mo?.isWafer && mo.chooseWafer) {
      const materialLots = new Set(row.components.filter(item => item.selected).map(item => item.materialLotNo.trim()))
      if (materialLots.has('') || materialLots.size > 1) return '必須挑片的 CP 工單要求已確認且所有勾選元件使用同一物料批。'
    }
  }
  const carrierCount = row.components.filter(item => item.selected && item.carrierNo.trim()).length
  if (carrierCount !== 0 && carrierCount !== row.components.filter(item => item.selected).length) return '載具元件必須全上或全不上。'
  for (const property of row.properties) {
    if (!property.propertyNo.trim()) return 'Lot Property 編號不可空白。'
    if (property.dataType === '數字' && property.propertyValue && !Number.isFinite(Number(property.propertyValue))) return property.propertyNo + ' 必須輸入數字。'
    if (property.dataType === '日期' && property.propertyValue && Number.isNaN(Date.parse(property.propertyValue))) return property.propertyNo + ' 必須輸入有效日期。'
  }
  return ''
}
function releaseFailure(row: LotRow): string {
  const mo = currentMOFor(row)
  if (row.lotState !== 0) return '批次已不是未下線狀態。'
  if (!mo) return '找不到 DEV 工單資料。'
  if (mo.unreleaseQty < row.inputQty) return '工單未下線量不足；正式 Service 會再執行 ChkLotBasisConfirm。'
  if (!row.ecnValid || row.ecnBasic.no !== mo.basicEcn.no || row.ecnBasic.type !== mo.basicEcn.type) return '基本 ECN 與目前產品 ECN 不一致。'
  if (row.ecnProcess.no !== mo.processEcn.no || row.ecnProcess.type !== mo.processEcn.type) return '流程 ECN 與目前產品／工單 ECN 不一致。'
  if (!row.opEcns.length) return '找不到批次的 OP ECN 紀錄。'
  if (row.opEcns.some(item => item.ecn.no !== mo.opEcn.no || item.ecn.type !== mo.opEcn.type)) return 'OP ECN 紀錄與目前製程 ECN 不一致。'
  return ''
}
function simulateRelease(row: LotRow): boolean {
  const failure = releaseFailure(row)
  if (failure) {
    logAction('Release', row.baseLotNo, '失敗', failure)
    pageError.value = row.baseLotNo + '：' + failure
    return false
  }
  const mo = currentMOFor(row)!
  if (!persist(lotStore.value.map(item => item.baseLotNo === row.baseLotNo ? { ...item, lotState: 1 } : item))) return false
  mos.value = mos.value.map(item => item.moNo === mo.moNo ? { ...item, unreleaseQty: Math.max(0, item.unreleaseQty - row.inputQty) } : item)
  logAction('Release', row.baseLotNo, '完成', 'DEV 模擬 SetLotBasisLotState：ECN／OP ECN 守門通過、LotState 0→1；未扣料或建立 WIP 狀態。')
  return true
}
async function saveDraft(close: boolean): Promise<void> {
  if (!dev.value || !draft.value || !mode.value) return
  formError.value = validateDraft()
  if (formError.value) return
  const row = cloneLot(draft.value)
  row.baseLotNo = row.baseLotNo.trim()
  row.creator = mode.value === 'edit' ? row.creator : currentUser.value
  row.createDate = mode.value === 'edit' ? row.createDate : nowText()
  const action = mode.value === 'edit' ? 'Edit' : mode.value === 'copy' ? 'Copy' : 'Add'
  const next = mode.value === 'edit'
    ? lotStore.value.map(item => item.baseLotNo === row.baseLotNo ? row : item)
    : [...lotStore.value, row]
  if (!persist(next)) return
  selectedMoNo.value = row.moNo
  moText.value = row.moNo
  activeLotNo.value = row.baseLotNo
  selectedLotNos.value = [row.baseLotNo]
  logAction(action, row.baseLotNo, '完成', 'DEV 僅保存假 Lot Basis／Component／Property 資料。')
  pageError.value = ''
  if (autoRelease.value && mode.value !== 'edit') {
    if (!simulateRelease(row)) {
      notice.value = 'Lot Basis 已保存，但自動下線模擬失敗；與來源一樣可能已先成功建立批次。'
    }
  }
  if (close) closeForm()
  else {
    if (autoSerial.value && mode.value !== 'edit') getNextLotNo()
    formError.value = ''
  }
  refreshLotList()
}
async function removeSelected(): Promise<void> {
  if (!dev.value) return
  const target = selectedRows.value
  if (!target.length) { pageError.value = '請先勾選要刪除的批次。'; return }
  if (target.some(row => row.lotState !== 0)) { pageError.value = '只有 LotState=0 的批次可以刪除。'; return }
  if (!await requestConfirmation('確定刪除 ' + target.length + ' 筆選取批次？來源會逐筆執行 CloseLot 與關聯資料清理。')) return
  busyAction.value = true
  let removed = 0
  for (let index = 0; index < target.length; index++) {
    const row = target[index]!
    if (deleteScenario.value === 'service-error-second' && index === 1) {
      logAction('Delete', row.baseLotNo, '失敗', '模擬 DelLotBasis Service error；先前已成功列不會回滾。')
      pageError.value = row.baseLotNo + '：模擬 DelLotBasis 失敗，停止後續批次。'
      break
    }
    if (!persist(lotStore.value.filter(item => item.baseLotNo !== row.baseLotNo))) break
    removed++
    logAction('Delete', row.baseLotNo, '完成', 'DEV 模擬 CloseLot 後刪除 Lot Basis 與接收屬性；未動真資料。')
  }
  busyAction.value = false
  selectedLotNos.value = []
  activeLotNo.value = ''
  if (removed) refreshLotList()
}
async function releaseSelected(): Promise<void> {
  if (!dev.value) return
  const target = selectedRows.value
  if (!target.length) { pageError.value = '請先勾選要下線的批次。'; return }
  if (!await requestConfirmation('確定下線 ' + target.length + ' 筆批次？來源會逐筆核對 ECN 並執行下線。')) return
  busyAction.value = true
  let completed = 0
  for (let index = 0; index < target.length; index++) {
    if (releaseScenario.value === 'service-error-second' && index === 1) {
      logAction('Release', target[index]!.baseLotNo, '失敗', '模擬 Service 下線錯誤；先前逐筆成功的批次保留。')
      pageError.value = target[index]!.baseLotNo + '：模擬 SetLotBasisLotState 失敗，停止後續批次。'
      break
    }
    if (!simulateRelease(target[index]!)) break
    completed++
  }
  busyAction.value = false
  selectedLotNos.value = []
  activeLotNo.value = ''
  notice.value = '本次逐筆完成 ' + completed + ' 筆；如有失敗，已保留先前成功列並停止後續處理。'
  refreshLotList()
}

const componentCountHint = computed(() => {
  if (!draft.value?.haveComponent) return '此 MO fixture 沒有需要選取的元件。'
  return '已選 ' + selectedComponentCount.value + ' ／輸入量 ' + draft.value.inputQty + '；來源要求選取元件筆數符合整數投入量。'
})
</script>

<template>
  <section class="lot-cp-page">
    <header class="page-head">
      <div><p class="eyebrow">WIP · CHIP TEST · WPP01 · DEV</p><h1>生產批開立_晶片測試</h1><p class="subtitle">MOState 3／6 · MOClass PKG · 只查詢未下線批次</p></div>
      <div class="head-actions"><span class="dev-tag">DEV 假資料</span><button type="button" class="quiet" @click="emit('close')">關閉</button></div>
    </header>

    <div class="source-strip"><span><b>Form</b> frmLotBasis_CP</span><span><b>Service</b> IC_CP_PKG.wsWIP</span><span><b>Core</b> clsLot／clsLot_CP</span><span><b>資料</b> localStorage fixture</span></div>

    <section class="panel filter-panel">
      <div class="filter-row">
        <label for="moNo">Select MO No.</label>
        <div class="mo-entry"><input id="moNo" v-model="moText" type="text" placeholder="請選擇 CP 工單" @keyup.enter="applyMOText" @change="applyMOText"><button type="button" class="quiet lookup" aria-label="選擇工單" @click="moDialog = true">…</button></div>
        <button type="button" class="primary" :disabled="!selectedMoNo" @click="queryLots">Get</button>
        <label for="lotSearch">DEV Lot 搜尋</label><input id="lotSearch" v-model="lotSearch" type="search" placeholder="BaseLotNo／OrgLotNo／CustomerLotNo" @keyup.enter="queryLots">
        <button type="button" class="quiet" @click="queryLots">Query</button>
      </div>
      <p class="hint">{{ selectedMo ? selectedMo.moNo + ' · ' + selectedMo.productNo + ' v' + selectedMo.productVersion + ' · 未下線量 ' + selectedMo.unreleaseQty + ' ' + selectedMo.inputUnitNo : '選擇工單後才載入 LotState=0 清單。' }}</p>
      <p v-if="pageError" class="error" role="alert">{{ pageError }}</p>
    </section>

    <div class="main-layout">
      <section class="panel list-panel">
        <div class="panel-title"><div><h2>Lot Basis</h2><p>{{ rows.length }} 筆待下線批次 · 作用列 {{ activeLot?.baseLotNo || '—' }} · 已勾選 {{ selectedRows.length }}</p></div><span class="state-tag">LotState = 0</span></div>
        <div class="table-scroll"><table class="lot-grid">
          <thead><tr><th class="check-col"><input type="checkbox" aria-label="全選批次" :checked="rows.length > 0 && selectedRows.length === rows.length" @change="selectAll(($event.target as HTMLInputElement).checked)"></th><th>BaseLotNo</th><th>OrgLotNo</th><th>RONo</th><th>ItemNo</th><th>ReturnNo</th><th>MONo</th><th>ProductNo</th><th>ProductVersion</th><th>ENGNo</th><th>ENGVersion</th><th>CustomerNo</th><th>InputQTY</th><th>InputUnitNo</th><th>CustomerLotNo</th><th>Priority</th><th>HotLot</th><th>Description</th><th>Creator</th><th>CreateDate</th></tr></thead>
          <tbody><tr v-for="row in rows" :key="row.baseLotNo" :class="{ active: activeLotNo === row.baseLotNo, selected: selectedLotNos.includes(row.baseLotNo) }" @click="selectActive(row)" @dblclick="openForm('edit')">
            <td class="check-col" @click.stop><input type="checkbox" :checked="selectedLotNos.includes(row.baseLotNo)" :aria-label="'選取 ' + row.baseLotNo" @change="setRowSelected(row, ($event.target as HTMLInputElement).checked)"></td>
            <td class="mono">{{ row.baseLotNo }}</td><td>{{ row.orgLotNo }}</td><td>{{ row.roNo }}</td><td class="number">{{ row.itemNo }}</td><td>{{ row.returnNo }}</td><td>{{ row.moNo }}</td><td>{{ row.productNo }}</td><td>{{ row.productVersion }}</td><td>{{ row.engNo }}</td><td>{{ row.engVersion }}</td><td>{{ row.customerNo }}</td><td class="number">{{ row.inputQty }}</td><td>{{ row.inputUnitNo }}</td><td>{{ row.customerLotNo }}</td><td class="number">{{ row.priority }}</td><td>{{ row.hotLot === 2 ? '特急' : row.hotLot === 1 ? '急件' : '一般' }}</td><td class="desc">{{ row.description || '—' }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td>
          </tr><tr v-if="!selectedMoNo"><td colspan="20" class="empty">請先選擇工單</td></tr><tr v-else-if="!rows.length"><td colspan="20" class="empty">查無 LotState=0 生產批</td></tr></tbody>
        </table></div>
        <div class="table-foot"><span>{{ notice }}</span><span>工單條件：MOState 3／6；Lot 查詢固定 LotState 0。</span></div>
      </section>
      <aside class="action-rail">
        <button type="button" class="primary" :disabled="!canAdd || busyAction" @click="openForm('add')">Add (A)</button>
        <button type="button" class="quiet" :disabled="!activeLot || busyAction" @click="openForm('edit')">Edit (E)</button>
        <button type="button" class="quiet" :disabled="!activeLot || busyAction" @click="openForm('copy')">Copy (C)</button>
        <button type="button" class="danger" :disabled="!selectedRows.length || busyAction" @click="removeSelected">Delete (D)</button>
        <button type="button" class="confirm" :disabled="!selectedRows.length || busyAction" @click="releaseSelected">Release (R)</button>
        <button type="button" class="quiet" :disabled="busyAction" @click="emit('close')">Close (X)</button>
        <label class="dev-control">DEV Release<select v-model="releaseScenario"><option value="success">逐筆成功</option><option value="service-error-second">第 2 筆 Service error</option></select></label>
        <label class="dev-control">DEV Delete<select v-model="deleteScenario"><option value="success">逐筆成功</option><option value="service-error-second">第 2 筆 Service error</option></select></label>
        <label class="dev-check"><input v-model="autoSerial" type="checkbox"> 模擬自動序號設定</label>
        <label class="dev-check"><input v-model="autoRelease" type="checkbox"> 模擬 LotCreateAutoRelease=1</label>
      </aside>
    </div>

    <section class="panel log-panel"><div class="panel-title"><div><h2>DEV 操作紀錄</h2><p>只記錄本頁假資料互動；不代表正式 Service 成功。</p></div><button type="button" class="quiet" @click="actionLog = []">清除</button></div>
      <div class="table-scroll"><table><thead><tr><th>時間</th><th>操作</th><th>BaseLotNo</th><th>結果</th><th>說明</th></tr></thead><tbody><tr v-for="item in actionLog" :key="item.id"><td>{{ item.time }}</td><td>{{ item.action }}</td><td class="mono">{{ item.lot }}</td><td><span class="result" :class="{ failed: item.result === '失敗' }">{{ item.result }}</span></td><td>{{ item.detail }}</td></tr><tr v-if="!actionLog.length"><td colspan="5" class="empty">尚無操作紀錄</td></tr></tbody></table></div>
    </section>
    <footer class="boundary">DEV only：不呼叫 wsWIP／wsOE／wsPRS／wsPRD，不連 Oracle／PostgreSQL，不執行扣料、序號服務、StartLot、CloseLot 或自動報工。</footer>

    <section v-if="moDialog" class="overlay" role="presentation"><div class="dialog mo-dialog" role="dialog" aria-modal="true" aria-label="選擇 CP 工單">
      <header class="dialog-head"><div><p class="eyebrow">MO CLASS = PKG</p><h2>選擇工單</h2><p>MOState 3／6 且未下線量大於 0，或仍有 LotState=0 生產批。</p></div><button type="button" class="icon-close" aria-label="關閉" @click="moDialog = false">×</button></header>
      <input v-model="moSearch" type="search" placeholder="工單／訂單／產品／客戶編號">
      <div class="table-scroll"><table><thead><tr><th>MONo</th><th>MOState</th><th>RONo</th><th>ItemNo</th><th>ProductNo</th><th>版本</th><th>CustomerNo</th><th>MOQty</th><th>UnReleaseLotQty</th><th></th></tr></thead><tbody>
        <tr v-for="mo in eligibleMOs" :key="mo.moNo"><td class="mono">{{ mo.moNo }}</td><td>{{ mo.moState }}</td><td>{{ mo.roNo }}</td><td>{{ mo.itemNo }}</td><td>{{ mo.productNo }}</td><td>{{ mo.productVersion }}</td><td>{{ mo.customerNo }}</td><td class="number">{{ mo.moQty }}</td><td class="number">{{ mo.unreleaseQty }}</td><td><button type="button" class="primary" @click="chooseMO(mo)">選取</button></td></tr>
        <tr v-if="!eligibleMOs.length"><td colspan="10" class="empty">沒有符合 CP 條件的 DEV 工單</td></tr>
      </tbody></table></div>
      <footer><button type="button" class="quiet" @click="moDialog = false">Close</button></footer>
    </div></section>

    <section v-if="draft && mode" class="overlay" role="presentation"><form class="dialog detail-dialog" role="dialog" aria-modal="true" aria-label="Lot Basis Define" @submit.prevent="saveDraft(true)">
      <header class="dialog-head"><div><p class="eyebrow">WP_P_LOTBASISDEF · CP</p><h2>Lot Basis {{ mode === 'add' ? '（ADD）' : mode === 'edit' ? '（EDIT）' : '（COPY）' }}</h2><p>{{ draft.moNo }} · LotState 0 · DEV sequence / ECN fixtures</p></div><button type="button" class="icon-close" aria-label="關閉" @click="closeForm">×</button></header>
      <section class="basic-grid">
        <label>BaseLotNo<div class="input-action"><input v-model="draft.baseLotNo" :readonly="autoSerial && mode !== 'edit'" required><button v-if="autoSerial && mode !== 'edit'" type="button" class="quiet" @click="getNextLotNo">Get</button></div></label>
        <label>ReturnNo<input v-model="draft.returnNo"></label><label>MO Type<input :value="(currentMOFor(draft)?.moTypeNo || '') + ' / ' + (currentMOFor(draft)?.moTypeName || '')" readonly></label>
        <label>MONo<input v-model="draft.moNo" readonly></label><label>RONo<input v-model="draft.roNo" readonly></label><label>ItemNo<input :value="draft.itemNo" readonly></label>
        <label>ProductNo<input v-model="draft.productNo" readonly></label><label>ProductVersion<input v-model="draft.productVersion" readonly></label>
        <label>ENGNo<input v-model="draft.engNo"></label><label>ENGVersion<input v-model="draft.engVersion"></label><label>DeviceNo<input v-model="draft.deviceNo"></label>
        <label>CustomerNo<input v-model="draft.customerNo" readonly></label><label>CustomerLotNo<input v-model="draft.customerLotNo" required></label>
        <label>InputQty<input v-model.number="draft.inputQty" type="number" min="0.0001" step="0.0001"></label><label>InputUnitNo<input v-model="draft.inputUnitNo" required></label>
        <label>Priority<input v-model.number="draft.priority" type="number" min="0" max="99"></label>
        <label>HotLot<select v-model.number="draft.hotLot"><option :value="0">一般</option><option :value="1">急件</option><option :value="2">特急</option></select></label>
        <label class="wide">Description<textarea v-model="draft.description" rows="2"></textarea></label>
        <label>Creator<input v-model="draft.creator" readonly></label><label>CreateDate<input :value="draft.createDate" readonly></label>
      </section>
      <div class="form-flags"><span>Wafer {{ currentMOFor(draft)?.isWafer ? 'Y' : 'N' }}</span><span>MOChooseWafer {{ currentMOFor(draft)?.chooseWafer ? 'Y' : 'N' }}</span><span>HaveComponent {{ draft.haveComponent ? 'Y' : 'N' }}</span><span>Material {{ currentMOFor(draft)?.materialConfirmed ? 'Confirmed (DEV)' : 'Not confirmed (DEV)' }}</span><label class="dev-check"><input v-model="autoSerial" type="checkbox"> DEV 自動批號</label></div>
      <nav class="tabs" aria-label="Lot Define pages"><button v-for="tab in tabOptions" :key="tab.key" type="button" :class="{ active: detailTab === tab.key }" @click="detailTab = tab.key">{{ tab.label }}</button></nav>

      <section v-if="detailTab === 'basic-ecn'" class="tab-content">
        <h3>Basic ECN</h3><p class="tab-hint">依 ProductNo／ProductVersion 載入的唯讀 DEV ECN；來源在 Release 時比對 LotState。</p>
        <dl class="ecn-grid"><dt>產品</dt><dd>{{ draft.productNo }} / {{ draft.productVersion }}</dd><dt>Lot ECN No</dt><dd>{{ draft.ecnBasic.no }}</dd><dt>Lot ECN Type</dt><dd>{{ draft.ecnBasic.type }}</dd><dt>目前產品 ECN</dt><dd>{{ currentMOFor(draft)?.basicEcn.no }} / {{ currentMOFor(draft)?.basicEcn.type }}</dd><dt>DEV 比對情境</dt><dd><label class="dev-check"><input v-model="draft.ecnValid" type="checkbox"> 模擬 Basic ECN 符合</label></dd></dl>
      </section>
      <section v-else-if="detailTab === 'process-ecn'" class="tab-content">
        <h3>Process ECN / MO Process</h3><p class="tab-hint">本 THEIL Form 在 Load 時移除可編輯 Process 頁；MO Process、PS、版本由 Service 載入並唯讀顯示。</p>
        <div class="table-scroll"><table><thead><tr><th>PSOrder</th><th>PSNo</th><th>ProcessNo</th><th>ProcessVersion</th><th>Lot ECN No</th><th>Lot ECN Type</th><th>目前 MO ECN</th></tr></thead><tbody><tr><td>{{ draft.psOrder }}</td><td>{{ draft.psNo }}</td><td>{{ draft.processNo }}</td><td>{{ draft.processVersion }}</td><td>{{ draft.ecnProcess.no }}</td><td>{{ draft.ecnProcess.type }}</td><td>{{ currentMOFor(draft)?.processEcn.no }}</td></tr></tbody></table></div>
        <div class="skipped-ops"><b>Not Execute OP</b><span v-for="op in draft.skippedOps" :key="op" class="op-chip">{{ op }}</span><span v-if="!draft.skippedOps.length" class="tab-hint">無略過站點</span></div>
      </section>
      <section v-else-if="detailTab === 'op-ecn'" class="tab-content">
        <h3>Operation ECN</h3><p class="tab-hint">顯示工單流程的 OP ECN；Release 前逐筆比對批次 LotOPECNRecord 與目前 OP ECN。</p>
        <div class="table-scroll"><table><thead><tr><th>OPNo</th><th>Lot ECN No</th><th>Lot ECN Type</th><th>目前 ECN</th><th>DEV result</th></tr></thead><tbody><tr v-for="item in draft.opEcns" :key="item.opNo"><td>{{ item.opNo }}</td><td>{{ item.ecn.no }}</td><td>{{ item.ecn.type }}</td><td>{{ currentMOFor(draft)?.opEcn.no }} / {{ currentMOFor(draft)?.opEcn.type }}</td><td>{{ item.ecn.no === currentMOFor(draft)?.opEcn.no ? '相符' : '不符' }}</td></tr></tbody></table></div>
      </section>
      <section v-else-if="detailTab === 'component'" class="tab-content">
        <div class="tab-head"><div><h3>MO Component</h3><p class="tab-hint">{{ componentCountHint }}</p></div><div class="tools"><button type="button" class="quiet" :disabled="!draft.haveComponent" @click="addComponent">Add</button><button type="button" class="quiet" @click="draft.components.forEach(item => item.selected = true)">Select All</button><button type="button" class="quiet" @click="draft.components.forEach(item => item.selected = false)">UnSelect All</button></div></div>
        <div class="table-scroll"><table><thead><tr><th>Select</th><th>ComponentNo</th><th>ProductNo</th><th>GoodQty</th><th>UnitNo</th><th>MaterialLotNo</th><th>CarrierNo</th><th>CarrierPosition</th></tr></thead><tbody><tr v-for="(item, index) in draft.components" :key="item.componentNo + index"><td><input v-model="item.selected" type="checkbox"></td><td class="mono">{{ item.componentNo }}</td><td>{{ item.productNo }}</td><td><input v-model.number="item.goodQty" type="number" min="0" class="small-input"></td><td><input v-model="item.unitNo" class="small-input"></td><td><input v-model="item.materialLotNo" class="small-input"></td><td><input v-model="item.carrierNo" class="small-input" placeholder="—"></td><td><input v-model.number="item.carrierPosition" type="number" min="1" class="small-input"></td></tr><tr v-if="!draft.components.length"><td colspan="8" class="empty">沒有載入元件；依來源 MO／wafer 條件判斷。</td></tr></tbody></table></div>
        <p class="tab-hint">必須挑片時：領料確認是開批守門，同一 Lot 勾選元件的 MaterialLotNo 須一致。已綁載具時，來源要求元件全上或全不上。</p>
      </section>
      <section v-else-if="detailTab === 'property'" class="tab-content">
        <div class="tab-head"><div><h3>Lot Property</h3><p class="tab-hint">TableType=0 屬性隨開批保存；TableType=1 的目標欄位由 server metadata 決定。</p></div><button type="button" class="quiet" @click="addProperty">Add Property</button></div>
        <div class="table-scroll"><table><thead><tr><th>PropertyNo</th><th>PropertyValue</th><th>DataType</th><th>TableType</th><th>來源</th><th></th></tr></thead><tbody><tr v-for="(item, index) in draft.properties" :key="index"><td><input v-model="item.propertyNo" class="small-input"></td><td><input v-model="item.propertyValue" class="small-input"></td><td><select v-model="item.dataType"><option>文字</option><option>數字</option><option>日期</option></select></td><td><select v-model.number="item.tableType"><option :value="0">0 · Lot property</option><option :value="1">1 · 下線處理</option></select></td><td>{{ item.system ? '系統' : '輸入' }}</td><td><button type="button" class="text-button" @click="draft!.properties.splice(index, 1)">移除</button></td></tr><tr v-if="!draft.properties.length"><td colspan="6" class="empty">沒有屬性資料</td></tr></tbody></table></div>
      </section>
      <section v-else class="tab-content">
        <div class="tab-head"><div><h3>Carrier</h3><p class="tab-hint">Edit 時載入目前批次的 CarrierStateDetail；DEV 以元件列表示綁定關係，不會執行上載具。</p></div><span class="state-tag">{{ hasCarrierBinding ? '有載具資料' : '目前未綁載具' }}</span></div>
        <div class="table-scroll"><table><thead><tr><th>ComponentNo</th><th>CarrierNo</th><th>CarrierPosition</th><th>DEV state</th></tr></thead><tbody><tr v-for="item in draft.components" :key="item.componentNo"><td class="mono">{{ item.componentNo }}</td><td>{{ item.carrierNo || '—' }}</td><td>{{ item.carrierNo ? item.carrierPosition : '—' }}</td><td>{{ item.carrierNo ? '已綁定' : '未綁定' }}</td></tr><tr v-if="!draft.components.length"><td colspan="4" class="empty">沒有元件／載具對應</td></tr></tbody></table></div>
      </section>
      <p v-if="formError" class="error" role="alert">{{ formError }}</p>
      <footer class="dialog-foot"><button type="button" class="quiet" @click="closeForm">Close</button><button type="button" class="quiet" @click="saveDraft(false)">Apply</button><button type="submit" class="primary">Confirm</button></footer>
    </form></section>
    <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
  </section>
</template>

<style scoped>
.lot-cp-page{--ink:#203142;--muted:#6b7e8e;--line:#d5dfe7;display:grid;gap:10px;min-height:0;height:100%;overflow:auto;padding:16px 18px 22px;box-sizing:border-box;background:#f4f7f9;color:var(--ink);font:12px/1.45 "Segoe UI","Microsoft JhengHei",sans-serif}.page-head{display:flex;align-items:center;justify-content:space-between;gap:12px}.eyebrow{margin:0;color:#71869a;font-size:9px;font-weight:800;letter-spacing:.12em}.page-head h1{margin:2px 0;font-size:20px}.subtitle{margin:0;color:var(--muted);font-size:10px}.head-actions{display:flex;align-items:center;gap:8px}.dev-tag,.state-tag{display:inline-flex;padding:3px 8px;border:1px solid #cde1d4;border-radius:999px;background:#eaf5ee;color:#31734c;font-size:9px;font-weight:750}.source-strip{display:flex;gap:14px;flex-wrap:wrap;padding:7px 9px;border:1px solid #dce5ec;border-radius:5px;background:#edf3f7;color:#687d8e;font-size:9px}.source-strip b{margin-right:4px;color:#3e5a71}.panel{min-width:0;border:1px solid var(--line);border-radius:6px;background:#fff;box-shadow:0 2px 5px #152d3a08;overflow:hidden}.filter-panel{padding:9px 10px}.filter-row{display:grid;grid-template-columns:100px minmax(180px,1fr) auto 95px minmax(160px,1.2fr) auto;align-items:center;gap:7px}.filter-row>label{color:#51697e;font-size:10px}.filter-row input,.basic-grid input,.basic-grid select,.basic-grid textarea,.mo-dialog>input,.tabs+input,.tab-content input,.tab-content select{min-width:0;min-height:28px;padding:4px 7px;border:1px solid #cbd7e0;border-radius:4px;background:#fff;color:#334c61;font:inherit;box-sizing:border-box}.mo-entry{display:flex;gap:5px;min-width:0}.mo-entry input{flex:1}.lookup{min-width:32px;font-weight:800}.hint{margin:6px 0 0;color:var(--muted);font-size:9px}.error{padding:7px 9px;margin:7px 0 0;border:1px solid #e8c1bd;border-radius:4px;background:#fff2f0;color:#9c4038;font-size:10px}.main-layout{display:grid;grid-template-columns:minmax(0,1fr) 132px;gap:9px;min-height:290px}.list-panel{display:flex;flex-direction:column}.panel-title{display:flex;align-items:center;justify-content:space-between;gap:10px;padding:8px 10px}.panel-title h2{margin:0;font-size:12px}.panel-title p{margin:2px 0 0;color:var(--muted);font-size:9px}.table-scroll{min-width:0;max-height:42vh;overflow:auto;border-top:1px solid #e6edf2}.lot-grid{width:max-content;min-width:100%;border-collapse:collapse;white-space:nowrap;font-size:9px}.lot-grid th{position:sticky;top:0;z-index:1;background:#edf3f7;color:#63798b;text-align:left;font-size:8px}.lot-grid th,.lot-grid td,.table-scroll table th,.table-scroll table td{padding:5px 7px;border-bottom:1px solid #edf1f4}.lot-grid tbody tr{cursor:pointer}.lot-grid tbody tr.active{outline:1px solid #8bb0cb;outline-offset:-1px}.lot-grid tbody tr.selected td{background:#eaf4fb}.lot-grid tbody tr:hover td{background:#f5f8fa}.check-col{text-align:center}.check-col input{margin:0}.mono{font-family:Consolas,"Cascadia Mono",monospace;color:#536e85}.number{text-align:right;font-variant-numeric:tabular-nums}.desc{max-width:180px;overflow:hidden;text-overflow:ellipsis}.empty{text-align:center!important;padding:16px!important;color:#8694a0}.table-foot{display:flex;justify-content:space-between;gap:10px;flex-wrap:wrap;padding:7px 9px;color:#758694;font-size:9px;border-top:1px solid #e8edf1}.action-rail{display:flex;flex-direction:column;gap:6px}.quiet,.primary,.confirm,.danger{min-height:29px;padding:4px 9px;border:1px solid #c6d2dc;border-radius:4px;background:#fff;color:#3f596f;font:inherit;cursor:pointer}.primary{border-color:#255a7d;background:#255a7d;color:white;font-weight:700}.confirm{border-color:#4f8a69;background:#eaf5ee;color:#2c6846;font-weight:700}.danger{border-color:#dfc8c2;background:#fff4f1;color:#964940}.quiet:disabled,.primary:disabled,.confirm:disabled,.danger:disabled{opacity:.45;cursor:not-allowed}.dev-control{display:grid;gap:3px;padding-top:5px;color:#718393;font-size:9px;border-top:1px solid #e3e9ee}.dev-control select{height:25px;padding:0 5px;border:1px solid #cad6de;border-radius:4px;background:#fff;color:#4c657a;font:inherit}.dev-check{display:flex;align-items:center;gap:5px;color:#687d8e;font-size:9px}.log-panel .table-scroll{max-height:125px}.table-scroll table{border-collapse:collapse;min-width:100%;width:max-content;white-space:nowrap;font-size:9px}.table-scroll table th{position:sticky;top:0;background:#edf3f7;color:#63798b;text-align:left}.result{display:inline-flex;padding:2px 6px;border-radius:4px;background:#e9f3ec;color:#34714b}.result.failed{background:#fff0ed;color:#a2483f}.boundary{color:#7b8b98;font-size:9px}.overlay{position:fixed;inset:0;z-index:150;display:grid;place-items:center;padding:14px;background:#152b3ca6}.dialog{display:grid;gap:9px;width:min(940px,96vw);max-height:92vh;overflow:auto;padding:13px;border:1px solid #ccd7e0;border-radius:7px;background:#fff;box-shadow:0 16px 48px #0e233640}.mo-dialog{width:min(1000px,96vw)}.detail-dialog{width:min(1120px,97vw)}.dialog-head{display:flex;align-items:flex-start;justify-content:space-between;gap:12px}.dialog-head h2{margin:2px 0;font-size:16px}.dialog-head p:not(.eyebrow){margin:0;color:var(--muted);font-size:9px}.icon-close{width:28px;height:28px;border:0;border-radius:4px;background:#eef2f5;color:#526a7e;font-size:18px;cursor:pointer}.basic-grid{display:grid;grid-template-columns:repeat(4,minmax(120px,1fr));gap:7px 10px;padding:9px;border:1px solid #e2e8ed;border-radius:5px;background:#fbfcfd}.basic-grid label{display:grid;gap:3px;color:#617689;font-size:9px}.basic-grid label input,.basic-grid label select{width:100%}.basic-grid input[readonly]{background:#f1f4f6;color:#758491}.input-action{display:flex;gap:4px}.input-action input{flex:1}.input-action button{min-height:28px}.wide{grid-column:1/-1}.form-flags{display:flex;gap:9px;align-items:center;flex-wrap:wrap;color:#738697;font-size:9px}.tabs{display:flex;gap:3px;overflow:auto;border-bottom:1px solid #dce4ea}.tabs button{padding:6px 9px;border:0;border-bottom:2px solid transparent;background:transparent;color:#647c90;font:inherit;white-space:nowrap;cursor:pointer}.tabs button.active{border-color:#2c6387;color:#245a7e;font-weight:750}.tab-content{display:grid;gap:7px;min-height:190px}.tab-content h3{margin:0;font-size:12px}.tab-hint{margin:0;color:#738697;font-size:9px}.tab-head{display:flex;align-items:center;justify-content:space-between;gap:8px;flex-wrap:wrap}.tools{display:flex;gap:5px}.tools button{min-height:25px}.ecn-grid{display:grid;grid-template-columns:130px minmax(180px,1fr);gap:4px 8px;margin:0;padding:8px;border:1px solid #e4eaef;border-radius:4px}.ecn-grid dt{color:#718596}.ecn-grid dd{margin:0;color:#304b61}.small-input{width:140px;min-height:25px!important;padding:3px 5px!important}.text-button{border:0;background:transparent;color:#587690;font:inherit;cursor:pointer}.dialog-foot{display:flex;justify-content:flex-end;gap:7px;padding-top:8px;border-top:1px solid #e5ebef}.dialog-foot button{min-width:74px}@media(max-width:1100px){.main-layout{grid-template-columns:1fr}.action-rail{display:grid;grid-template-columns:repeat(3,minmax(90px,1fr))}.dev-control{grid-column:span 1}.filter-row{grid-template-columns:95px minmax(150px,1fr) auto 90px minmax(150px,1fr) auto}}@media(max-width:720px){.lot-cp-page{padding:10px}.filter-row{grid-template-columns:90px minmax(0,1fr) auto}.filter-row label:nth-of-type(2){grid-column:1}.filter-row input[type=search]{grid-column:2}.action-rail{grid-template-columns:repeat(2,minmax(100px,1fr))}.basic-grid{grid-template-columns:repeat(2,minmax(120px,1fr))}.wide{grid-column:1/-1}.table-foot{flex-direction:column}.page-head{align-items:flex-start}.form-flags{gap:6px}.tabs button{padding:6px 7px}}
.skipped-ops{display:flex;align-items:center;gap:6px;flex-wrap:wrap;padding:7px;border:1px solid #e4eaef;border-radius:4px;color:#697f92;font-size:9px}.op-chip{padding:2px 6px;border-radius:4px;background:#edf3f7;color:#49657c}
</style>
