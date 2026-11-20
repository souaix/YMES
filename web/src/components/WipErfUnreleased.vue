<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'

type FilterKey = 'lotNo' | 'opNo' | 'curGroupNo' | 'customerNo' | 'mClassNo' | 'erfNo'
type LookupKey = FilterKey | 'nextGroup' | 'finalGroup' | 'targetOp' | 'targetProcess' | 'returnOp' | 'targetModule' | 'inventory' | 'mergeLot'
type LotOption = 'split' | 'merge' | 'back' | 'qtyAdjust' | 'scrap' | 'withdraw' | 'go' | 'jumpOp' | 'jumpMain' | 'jumpProcess' | 'jumpModule' | 'semiInv' | 'scrapInv' | 'tuning'
type Dialog = 'disposition' | 'abort' | 'recreate' | 'info' | 'child' | null
type ErfRow = {
  erfNo: string; lotNo: string; productNo: string; productName: string; customerLotNo: string
  status: number; opNo: string; opName: string; customerNo: string; customerName: string; mClassNo: string
  holdDescription: string; curGroupNo: string; dispDescription: string; ruleNo: string; createQty: number
  erfRecordQty: number; curQty: number; priority: number; batchLot: string; creator: string; createDate: string
  erfType: number; erfSource: number; lotStamp: number; processNo: string; psNo: string; partialMode: boolean
  hasGoodBin: boolean; hasComponent: boolean; carrierNo: string; inStocker: boolean; ocapNo: string
  createErf?: boolean; lotDispTypeLimit?: number[]; opReference?: boolean; onMainProcess?: boolean
}
type Action = { erfNo: string; kind: 'erfDisposition' | 'lotDisposition' | 'abort' | 'qtyAdjust' | 'back' | 'split' | 'merge'; status: number; description: string; actor: string; at: string; releaseType?: number; curQty?: number; target?: string; reasons?: Reason[]; attachments?: string[]; dispositionType?: string }
type Saved = { actions: Action[]; recreated: ErfRow[]; nextSerial: number }
type Reason = { holdItemNo: string; goodQty: number; scrapQty: number; description: string; holdDisposition: string }

const props = defineProps<{ developmentPreview?: boolean; currentUserNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview !== false)
const unavailableMessage = '未解除異常單服務尚未提供，暫無法查詢或處置。'
const previewMessage = 'Query 查詢未解除異常單；選列後可處置、作廢、重開或查看。'
const storageKey = 'ymes-dev-mock:wip-erf-unreleased:state:v1'
const wp501Key = 'ymes-dev-mock:wip-immediate-hold:state:v1'
const statusLabels: Record<number, string> = { 0: 'ERF Undisposition', 1: 'ERF Wait Disposition', 2: 'Notice Customer and Wait Disposition', 3: 'Waive', 11: 'Wait Lot Disposition', 12: 'Abort', 21: 'Release' }
const queryFields: { key: FilterKey; label: string; lookup: boolean }[] = [
  { key: 'lotNo', label: 'Lot No', lookup: false }, { key: 'customerNo', label: 'Customer No', lookup: true },
  { key: 'opNo', label: 'OP No', lookup: true }, { key: 'mClassNo', label: 'MClass No', lookup: true },
  { key: 'curGroupNo', label: 'Cur Group', lookup: true }, { key: 'erfNo', label: 'ERF No', lookup: true },
]
const columns: { key: keyof ErfRow; label: string }[] = [
  { key: 'lotNo', label: 'Lot No' }, { key: 'erfNo', label: 'ERF No' }, { key: 'productNo', label: 'Product No' },
  { key: 'productName', label: 'Product Name' }, { key: 'customerLotNo', label: 'Customer Lot No' },
  { key: 'status', label: 'Status' }, { key: 'opNo', label: 'OP No' }, { key: 'opName', label: 'OP Name' },
  { key: 'customerNo', label: 'Customer No' }, { key: 'customerName', label: 'Customer Name' },
  { key: 'mClassNo', label: 'MClass No' }, { key: 'holdDescription', label: 'Hold Description' },
  { key: 'curGroupNo', label: 'CurGroup No' }, { key: 'dispDescription', label: 'Disp Description' },
  { key: 'ruleNo', label: 'Rule No' }, { key: 'createQty', label: 'ERF Create Qty' },
  { key: 'erfRecordQty', label: 'ERF Record Qty' }, { key: 'curQty', label: 'ERF Cur Qty' },
  { key: 'createDate', label: 'Cal Hold Time' }, { key: 'priority', label: 'Priority' },
  { key: 'batchLot', label: 'Batch Lot' }, { key: 'creator', label: 'Creator' }, { key: 'createDate', label: 'CreateDate' },
]
const lotOptions: { key: LotOption; label: string; releaseType: number; privilege: string }[] = [
  { key: 'split', label: 'Split', releaseType: 12, privilege: 'SPLIT' }, { key: 'merge', label: 'Merge', releaseType: 13, privilege: 'MERGE' },
  { key: 'back', label: 'Back to ERFDisposition', releaseType: 11, privilege: 'BACK_DISPOSITION' },
  { key: 'qtyAdjust', label: 'Qty Adjust', releaseType: 14, privilege: 'QTY_ADJUST' },
  { key: 'scrap', label: 'Scrap', releaseType: 98, privilege: 'SCRAP' }, { key: 'withdraw', label: 'WithDrawWork', releaseType: 99, privilege: 'RETURN' },
  { key: 'go', label: 'Go', releaseType: 0, privilege: 'GO' }, { key: 'jumpOp', label: 'Jump OP', releaseType: 1, privilege: 'JUMP_OP' },
  { key: 'jumpMain', label: 'Jump Main', releaseType: 8, privilege: 'JUMP_MAIN' }, { key: 'jumpProcess', label: 'Jump Process', releaseType: 2, privilege: 'JUMP_PROCESS_01/02' },
  { key: 'jumpModule', label: 'Jump Module', releaseType: 6, privilege: 'JUMP_MODULE' }, { key: 'semiInv', label: 'Semi Inventory', releaseType: 3, privilege: 'INVENTORY' },
  { key: 'scrapInv', label: 'Scrap Inventory', releaseType: 7, privilege: 'SCRAP_INVENTORY' }, { key: 'tuning', label: 'Process Tuning', releaseType: 4, privilege: 'PROCESS_TURNING' },
]
const routes = [{ processNo: 'PROC-A', opNo: 'OP-CUT' }, { processNo: 'PROC-A', opNo: 'OP-CUT-20' }, { processNo: 'PROC-B', opNo: 'OP-TEST' }, { processNo: 'PROC-B', opNo: 'OP-TEST-20' }]
const flags = reactive<Record<FilterKey, boolean>>({ lotNo: false, opNo: false, curGroupNo: false, customerNo: false, mClassNo: false, erfNo: false })
const filters = reactive<Record<FilterKey, string>>({ lotNo: '', opNo: '', curGroupNo: '', customerNo: '', mClassNo: '', erfNo: '' })
const saved = ref<Saved>({ actions: [], recreated: [], nextSerial: 1 })
const rows = ref<ErfRow[]>([])
const activeNo = ref('')
const queried = ref(false)
const dialog = ref<Dialog>(null)
const tab = ref<'erf' | 'lot'>('erf')
const infoTab = ref('Create Hold Code')
const lookup = ref<LookupKey | null>(null)
const ocapPicker = ref(false)
const ocapNode = ref('')
const erfDispType = ref<'next' | 'notice' | 'final'>('next')
const dispDescription = ref('')
const nextGroup = ref('')
const finalGroup = ref('')
const contactorName = ref('')
const ocapResult = ref('')
const reasonDraft = reactive({ holdItemNo: '', goodQty: '0', scrapQty: '0', description: '', holdDisposition: '' })
const reasons = ref<Reason[]>([])
const reasonIndex = ref(-1)
const attachmentNames = ref<string[]>([])
const attachmentDraft = ref('')
const lotOption = ref<LotOption | ''>('')
const releaseDisp = ref('')
const target = reactive({ processNo: '', opNo: '', returnOp: '', moduleNo: '', moduleVersion: '01', inventoryNo: '' })
const returnOrg = ref(true)
const childLotNo = ref('')
const childQty = ref('')
const error = ref('')
const message = ref('Query 查詢未解除異常單；選列後可處置、作廢、重開或查看。')
const active = computed(() => rows.value.find(row => row.erfNo === activeNo.value) || null)
const activeActions = computed(() => saved.value.actions.filter(item => item.erfNo === activeNo.value))
const latestReasons = computed(() => [...activeActions.value].reverse().find(item => item.reasons?.length)?.reasons || [])
const latestAttachments = computed(() => [...activeActions.value].reverse().find(item => item.attachments?.length)?.attachments || [])
const canErfDisposition = computed(() => dev.value && !!active.value && [0, 1, 2, 3].includes(active.value.status))
const canLotDisposition = computed(() => dev.value && active.value?.status === 11)
const allowedGroups = ['QA-01', 'ENG-01'] // DEV fixture only; server must use real group membership.
const devPrivileges: Record<string, string[]> = {
  'QA-01': lotOptions.map(item => item.privilege).flatMap(item => item === 'JUMP_PROCESS_01/02' ? ['JUMP_PROCESS_01', 'JUMP_PROCESS_02'] : [item]),
  'ENG-01': ['GO', 'JUMP_OP', 'JUMP_MAIN', 'JUMP_PROCESS_01', 'PROCESS_TURNING', 'BACK_DISPOSITION', 'SCRAP'],
}
function optionAvailable(key: LotOption): boolean {
  if (!dev.value) { return false }

  const row = active.value
  if (!row || row.status !== 11) return false
  const item = lotOptions.find(option => option.key === key)
  if (!item) return false
  const permissions = row.curGroupNo.split(',').flatMap(group => devPrivileges[group.trim()] || [])
  if (key === 'jumpProcess' ? !permissions.includes(returnOrg.value ? 'JUMP_PROCESS_02' : 'JUMP_PROCESS_01') : !permissions.includes(item.privilege)) return false
  if (row.lotDispTypeLimit?.length && !row.lotDispTypeLimit.includes(item.releaseType) && !['scrap', 'withdraw'].includes(key)) return false
  if (row.erfType === 6 && ['scrap', 'withdraw'].includes(key)) return false
  if (row.partialMode && !['go', 'back', 'scrap', 'withdraw'].includes(key)) return false
  if (row.ocapNo && ['split', 'merge', 'back', 'qtyAdjust'].includes(key)) return false
  if (row.inStocker && ['split', 'merge'].includes(key)) return false
  if (row.hasGoodBin || row.hasComponent) { if (key === 'qtyAdjust') return false }
  if (key === 'back' && row.createErf === false) return false
  if (key === 'jumpProcess' && (row.opReference || row.onMainProcess === false)) return false
  return true
}
const lookupOptions = computed(() => {
  if (!dev.value) return []
  const key = lookup.value
  if (!key) return []
  if (key === 'nextGroup' || key === 'finalGroup') return allowedGroups
  if (key === 'targetProcess') return [...new Set(routes.map(item => item.processNo))]
  if (key === 'targetOp') return routes.filter(item => item.processNo === (lotOption.value === 'jumpOp' ? active.value?.processNo : target.processNo)).map(item => item.opNo)
  if (key === 'returnOp') return routes.filter(item => item.processNo === active.value?.processNo).map(item => item.opNo)
  if (key === 'targetModule') return ['MODULE-A', 'MODULE-B']
  if (key === 'inventory') return ['INV-WIP-01', 'INV-SCRAP-01']
  if (key === 'mergeLot') return allRows().filter(row => row.erfNo !== activeNo.value && row.productNo === active.value?.productNo).map(row => row.lotNo)
  return [...new Set(allRows().map(row => String(row[key])))].filter(Boolean).sort()
})
const childVariant = computed(() => active.value?.hasGoodBin ? 'Bin' : active.value?.hasComponent ? active.value.carrierNo ? 'Carrier' : 'Component' : 'Standard')

function clearFormal(): void {
  saved.value = { actions: [], recreated: [], nextSerial: 0 }; rows.value = []; activeNo.value = ''; queried.value = false
  dialog.value = null; lookup.value = null; ocapPicker.value = false; ocapNode.value = ''; reasons.value = []; reasonIndex.value = -1
  attachmentNames.value = []; attachmentDraft.value = ''; lotOption.value = ''; releaseDisp.value = ''; childLotNo.value = ''; childQty.value = ''
  dispDescription.value = ''; nextGroup.value = ''; finalGroup.value = ''; contactorName.value = ''; ocapResult.value = ''; error.value = ''; message.value = unavailableMessage
  Object.assign(filters, { lotNo: '', opNo: '', curGroupNo: '', customerNo: '', mClassNo: '', erfNo: '' })
  Object.assign(flags, { lotNo: false, opNo: false, curGroupNo: false, customerNo: false, mClassNo: false, erfNo: false })
  Object.assign(target, { processNo: '', opNo: '', returnOp: '', moduleNo: '', moduleVersion: '', inventoryNo: '' })
}
function fixtures(): ErfRow[] {
  if (!dev.value) { return [] }

  return [
    { erfNo: 'DEV-ERF-001', lotNo: 'DEV-LOT-001', productNo: 'PRD-A', productName: '示範產品 A', customerLotNo: 'CL-001', status: 0, opNo: 'OP-CUT', opName: '裁切', customerNo: 'CUS-A', customerName: '示範客戶 A', mClassNo: 'M-QUALITY', holdDescription: '外觀檢查異常', curGroupNo: 'QA-01', dispDescription: '', ruleNo: '', createQty: 8, erfRecordQty: 0, curQty: 8, priority: 1, batchLot: '', creator: 'DEV', createDate: '2026-11-19T08:00:00', erfType: 2, erfSource: 0, lotStamp: 50901, processNo: 'PROC-A', psNo: 'PS-01', partialMode: false, hasGoodBin: false, hasComponent: false, carrierNo: '', inStocker: false, ocapNo: '' },
    { erfNo: 'DEV-ERF-003', lotNo: 'DEV-LOT-003', productNo: 'PRD-A', productName: '示範產品 A', customerLotNo: 'CL-003', status: 1, opNo: 'OP-CUT', opName: '裁切', customerNo: 'CUS-A', customerName: '示範客戶 A', mClassNo: 'M-QUALITY', holdDescription: '批次尺寸異常', curGroupNo: 'QA-01', dispDescription: '待判定', ruleNo: '', createQty: 12, erfRecordQty: 2, curQty: 10, priority: 1, batchLot: 'DEV-LOT-003,DEV-LOT-004', creator: 'DEV', createDate: '2026-11-20T07:00:00', erfType: 6, erfSource: 0, lotStamp: 50903, processNo: 'PROC-A', psNo: 'PS-01', partialMode: false, hasGoodBin: false, hasComponent: false, carrierNo: '', inStocker: false, ocapNo: '' },
    { erfNo: 'DEV-ERF-004', lotNo: 'DEV-LOT-004', productNo: 'PRD-B', productName: '示範產品 B', customerLotNo: 'CL-004', status: 0, opNo: 'OP-TEST', opName: '測試', customerNo: 'CUS-B', customerName: '示範客戶 B', mClassNo: 'M-EQUIPMENT', holdDescription: 'OCAP 處置待確認', curGroupNo: 'ENG-01', dispDescription: '', ruleNo: 'RULE-04', createQty: 6, erfRecordQty: 1, curQty: 5, priority: 2, batchLot: '', creator: 'DEV', createDate: '2026-11-19T16:00:00', erfType: 2, erfSource: 0, lotStamp: 50904, processNo: 'PROC-B', psNo: 'PS-02', partialMode: false, hasGoodBin: false, hasComponent: true, carrierNo: '', inStocker: false, ocapNo: 'OCAP-DEV-04' },
    { erfNo: 'DEV-ERF-005', lotNo: 'DEV-LOT-005', productNo: 'PRD-A', productName: '示範產品 A', customerLotNo: 'CL-005', status: 11, opNo: 'OP-CUT-20', opName: '裁切檢查', customerNo: 'CUS-A', customerName: '示範客戶 A', mClassNo: 'M-QUALITY', holdDescription: '待生產批處置', curGroupNo: 'QA-01', dispDescription: '最後處置完成', ruleNo: '', createQty: 9, erfRecordQty: 0, curQty: 9, priority: 1, batchLot: '', creator: 'DEV', createDate: '2026-11-18T08:00:00', erfType: 2, erfSource: 0, lotStamp: 50905, processNo: 'PROC-A', psNo: 'PS-01', partialMode: false, hasGoodBin: false, hasComponent: false, carrierNo: '', inStocker: false, ocapNo: '', createErf: true, onMainProcess: true },
  ]
}
function wp501Rows(): ErfRow[] {
  if (!dev.value) { return [] }

  try {
    const raw = JSON.parse(window.localStorage.getItem(wp501Key) || 'null') as { erfs?: Record<string, unknown>[]; lots?: Record<string, unknown>[] } | null
    if (!raw || !Array.isArray(raw.erfs) || !Array.isArray(raw.lots)) return []
    const lots = raw.lots
    return raw.erfs.flatMap(erf => {
      const lot = lots.find(item => item.lotNo === erf.lotNo)
      if (!lot || typeof erf.erfNo !== 'string' || typeof erf.lotNo !== 'string' || ![3, 31].includes(Number(lot.status))) return []
      return [{ erfNo: erf.erfNo, lotNo: erf.lotNo, productNo: String(lot.productNo || ''), productName: '', customerLotNo: '', status: 0, opNo: String(lot.opNo || ''), opName: '', customerNo: String(lot.customerNo || ''), customerName: '', mClassNo: String(erf.mClassNo || ''), holdDescription: String(erf.holdDescription || ''), curGroupNo: 'QA-01', dispDescription: '', ruleNo: '', createQty: Number(lot.curQty) || 0, erfRecordQty: 0, curQty: Number(lot.curQty) || 0, priority: 0, batchLot: '', creator: String(erf.actor || 'DEV'), createDate: String(erf.createdAt || new Date().toISOString()), erfType: Number(erf.erfType) || 2, erfSource: 0, lotStamp: Number(lot.lotStamp) || 0, processNo: String(lot.processNo || ''), psNo: String(lot.psNo || ''), partialMode: !!lot.partialMode, hasGoodBin: false, hasComponent: Array.isArray(lot.components) && lot.components.length > 0, carrierNo: '', inStocker: false, ocapNo: '', createErf: erf.createERF === true } satisfies ErfRow]
    })
  } catch { return [] }
}
function allRows(): ErfRow[] {
  if (!dev.value) { return [] }

  const source = [...fixtures(), ...wp501Rows(), ...saved.value.recreated]
  return source.map(row => {
    const history = saved.value.actions.filter(item => item.erfNo === row.erfNo)
    const last = history[history.length - 1]
    return last ? { ...row, status: last.status, dispDescription: last.description || row.dispDescription, curQty: last.curQty ?? row.curQty, curGroupNo: last.kind === 'erfDisposition' && last.dispositionType !== 'notice' && last.target ? last.target : row.curGroupNo } : row
  })
}
function loadSaved(): void {
  if (!dev.value) { clearFormal(); return }

  if (!dev.value) return
  try { const raw = JSON.parse(window.localStorage.getItem(storageKey) || 'null') as Saved | null; saved.value = raw && Array.isArray(raw.actions) && Array.isArray(raw.recreated) && Number.isFinite(raw.nextSerial) ? raw : { actions: [], recreated: [], nextSerial: 1 } }
  catch { saved.value = { actions: [], recreated: [], nextSerial: 1 } }
}
function persist(): boolean {
  if (!dev.value) { return false }
 try { window.localStorage.setItem(storageKey, JSON.stringify(saved.value)); return true } catch { error.value = '無法儲存 DEV 假資料。'; return false } }
function norm(value: string): string { return value.trim().toUpperCase() }
function query(): void {
  if (!dev.value) { clearFormal(); return }

  error.value = ''; lookup.value = null
  if (!dev.value) { error.value = '此頁目前只提供 DEV 假資料。'; return }
  loadSaved()
  for (const field of queryFields) filters[field.key] = norm(filters[field.key])
  rows.value = allRows().filter(row => row.erfSource === 0 && [0, 1, 2, 3, 11].includes(row.status) &&
    queryFields.every(field => !flags[field.key] || !filters[field.key] || (field.key === 'lotNo' ? [row.lotNo, ...row.batchLot.split(',')].some(no => no.includes(filters.lotNo)) : String(row[field.key]) === filters[field.key])))
    .sort((a, b) => a.lotNo.localeCompare(b.lotNo) || a.erfNo.localeCompare(b.erfNo))
  activeNo.value = rows.value[0]?.erfNo || ''; queried.value = true; message.value = `查得 ${rows.value.length} 筆未解除異常單。`
}
function pick(value: string): void {
  if (!dev.value) { clearFormal(); return }

  const key = lookup.value
  if (!key) return
  if (key === 'nextGroup') nextGroup.value = value
  else if (key === 'finalGroup') finalGroup.value = value
  else if (key === 'targetOp') target.opNo = value
  else if (key === 'targetProcess') { target.processNo = value; target.opNo = '' }
  else if (key === 'returnOp') target.returnOp = value
  else if (key === 'targetModule') target.moduleNo = value
  else if (key === 'inventory') target.inventoryNo = value
  else if (key === 'mergeLot') childLotNo.value = value
  else { filters[key] = value; flags[key] = true }
  lookup.value = null
}
function holdMinutes(date: string): string { const timestamp = new Date(date).getTime(); return Number.isFinite(timestamp) ? `${Math.max(0, Math.round((Date.now() - timestamp) / 60000))} Minutes` : '' }
function cell(row: ErfRow, key: keyof ErfRow, label: string): string | number {
  if (key === 'status') return statusLabels[row.status] || row.status
  if (label === 'Cal Hold Time') return holdMinutes(row.createDate)
  if (key === 'createDate') return row.createDate.replace('T', ' ').slice(0, 19)
  return String(row[key] ?? '')
}
function permitted(row: ErfRow): boolean {
  if (!dev.value) { return false }
 return row.curGroupNo.split(',').some(group => allowedGroups.includes(group.trim())) }
function permittedExact(row: ErfRow): boolean {
  if (!dev.value) { return false }
 return allowedGroups.includes(row.curGroupNo) }
function openDisposition(): void {
  if (!dev.value) { clearFormal(); return }

  error.value = ''
  if (!active.value) { error.value = '請先選擇異常單。'; return }
  if (!canErfDisposition.value && !canLotDisposition.value) { error.value = '異常單狀態不可處置。'; return }
  if (!active.value.ocapNo && !permitted(active.value)) { error.value = 'DEV 示意群組沒有此異常單處置權限。'; return }
  if (active.value.ocapNo) { ocapPicker.value = true; ocapNode.value = ''; return }
  showDisposition()
}
function showDisposition(): void {
  if (!dev.value) { clearFormal(); return }

  tab.value = canLotDisposition.value ? 'lot' : 'erf'; erfDispType.value = 'next'; dispDescription.value = ''; nextGroup.value = ''; finalGroup.value = ''; contactorName.value = ''; ocapResult.value = ''
  lotOption.value = ''; releaseDisp.value = ''; reasons.value = [{ holdItemNo: 'H-001', goodQty: active.value?.curQty || 0, scrapQty: 0, description: '', holdDisposition: '' }]
  attachmentNames.value = []; reasonIndex.value = -1; dialog.value = 'disposition'; ocapPicker.value = false
}
function chooseOcap(): void {
  if (!dev.value) { clearFormal(); return }
 if (!ocapNode.value) { error.value = '請選擇未完成的 OCAP 節點。'; return }; showDisposition() }
function addReason(): void {
  if (!dev.value) { clearFormal(); return }

  error.value = ''
  const holdItemNo = norm(reasonDraft.holdItemNo)
  const goodQty = Number(reasonDraft.goodQty); const scrapQty = Number(reasonDraft.scrapQty)
  if (!holdItemNo || !Number.isFinite(goodQty) || !Number.isFinite(scrapQty) || goodQty < 0 || scrapQty < 0) { error.value = 'Hold Item、良品數與報廢數必須有效。'; return }
  const item: Reason = { holdItemNo, goodQty, scrapQty, description: reasonDraft.description.trim(), holdDisposition: reasonDraft.holdDisposition.trim() }
  if (reasonIndex.value >= 0) reasons.value[reasonIndex.value] = item
  else reasons.value.push(item)
  reasonIndex.value = -1; reasonDraft.holdItemNo = ''; reasonDraft.goodQty = '0'; reasonDraft.scrapQty = '0'; reasonDraft.description = ''; reasonDraft.holdDisposition = ''
}
function editReason(index: number): void {
  if (!dev.value) { clearFormal(); return }
 const item = reasons.value[index]; reasonIndex.value = index; Object.assign(reasonDraft, { holdItemNo: item.holdItemNo, goodQty: String(item.goodQty), scrapQty: String(item.scrapQty), description: item.description, holdDisposition: item.holdDisposition }) }
function addAttachment(): void {
  if (!dev.value) { clearFormal(); return }
 const name = attachmentDraft.value.trim(); if (!name || name.includes('/') || name.includes('\\')) { error.value = '請輸入有效的 DEV 附件檔名。'; return }; if (!attachmentNames.value.includes(name)) attachmentNames.value.push(name); attachmentDraft.value = '' }
function record(action: Action): boolean {
  if (!dev.value) { return false }

  loadSaved()
  if (saved.value.actions.some(item => item.erfNo === action.erfNo && ['abort','lotDisposition'].includes(item.kind) && [12,21].includes(item.status))) { error.value = '此異常單已處理，請重新查詢。'; return false }
  saved.value.actions.push(action)
  if (!persist()) { saved.value.actions.pop(); return false }
  return true
}
function confirmErfDisposition(): void {
  if (!dev.value) { clearFormal(); return }

  error.value = ''
  const row = active.value
  if (!row || !canErfDisposition.value || (!row.ocapNo && !permitted(row))) { error.value = '目前狀態不可做 ERF 處置或缺少群組權限。'; return }
  if (!dispDescription.value.trim()) { error.value = '請填 Disp Description。'; return }
  if (erfDispType.value === 'next' && !nextGroup.value) { error.value = '請選下一處置群組。'; return }
  if (erfDispType.value === 'notice' && !contactorName.value.trim()) { error.value = '請填聯絡人。'; return }
  if (erfDispType.value === 'final' && !finalGroup.value) { error.value = '請選最後處置群組。'; return }
  if (row.ocapNo && (!ocapNode.value || !ocapResult.value.trim())) { error.value = 'OCAP 節點與處置結果不得為空。'; return }
  const status = erfDispType.value === 'next' ? 1 : erfDispType.value === 'notice' ? 2 : 11
  if (!record({ erfNo: row.erfNo, kind: 'erfDisposition', status, description: dispDescription.value.trim(), actor: props.currentUserNo || (dev ? 'DEV' : ''), at: new Date().toISOString(), target: erfDispType.value === 'next' ? nextGroup.value : erfDispType.value === 'final' ? finalGroup.value : contactorName.value.trim(), dispositionType: erfDispType.value, reasons: reasons.value.map(item => ({ ...item })), attachments: [...attachmentNames.value] })) return
  dialog.value = null; query(); message.value = `DEV 已模擬 ${row.erfNo} 異常單處置。`
}
function validateLot(): string {
  if (!dev.value) { return unavailableMessage }

  const row = active.value
  if (!row || !canLotDisposition.value || !permitted(row)) return '目前狀態不可做 Lot 處置或缺少群組權限。'
  if (!lotOption.value) return '請選 Lot Disposition 選項。'
  if (!optionAvailable(lotOption.value)) return '此選項不符合群組權限、Lot 狀態或來源限制。'
  if (['jumpOp','jumpProcess','tuning'].includes(lotOption.value) && !target.opNo) return '請選目標 OP No。'
  if (['jumpProcess','tuning'].includes(lotOption.value) && !target.processNo) return '請選目標 Process No。'
  if (lotOption.value === 'jumpProcess' && returnOrg.value && !target.returnOp) return '請選返回原流程 OP No。'
  if (lotOption.value === 'jumpMain' && !target.opNo) return '請選主流程 OP No。'
  if (lotOption.value === 'jumpModule' && (!target.moduleNo || !target.moduleVersion || !target.returnOp)) return '請選 Module、版本與返回主流程 OP。'
  if (['semiInv','scrapInv'].includes(lotOption.value) && !target.inventoryNo) return '請選 Inventory No。'
  return ''
}
function confirmLot(): void {
  if (!dev.value) { clearFormal(); return }

  error.value = validateLot()
  if (error.value || !active.value) return
  if (['split','merge','qtyAdjust'].includes(lotOption.value)) { childLotNo.value = ''; childQty.value = ''; dialog.value = 'child'; return }
  const row = active.value
  const item = lotOptions.find(option => option.key === lotOption.value)
  if (!item) return
  const isBack = lotOption.value === 'back'
  if (!record({ erfNo: row.erfNo, kind: isBack ? 'back' : 'lotDisposition', status: isBack ? 0 : 21, description: releaseDisp.value.trim(), actor: props.currentUserNo || (dev ? 'DEV' : ''), at: new Date().toISOString(), releaseType: item.releaseType, target: target.opNo || target.moduleNo || target.inventoryNo || '' })) return
  dialog.value = null; query(); message.value = `DEV 已模擬 ${row.erfNo} ${item.label}。`
}
function confirmChild(): void {
  if (!dev.value) { clearFormal(); return }

  error.value = ''
  const row = active.value
  if (!row) return
  if (!['split', 'merge', 'qtyAdjust'].includes(lotOption.value) || !optionAvailable(lotOption.value as LotOption)) { error.value = '此選項目前不可處置。'; return }
  const qty = Number(childQty.value)
  if (!Number.isFinite(qty) || qty <= 0 || (lotOption.value === 'split' && qty >= row.curQty)) { error.value = '數量必須有效，分批量要小於原批量。'; return }
  if (lotOption.value === 'merge') {
    const mate = allRows().find(item => item.lotNo === norm(childLotNo.value) && item.erfNo !== row.erfNo)
    if (!mate || mate.productNo !== row.productNo || mate.processNo !== row.processNo) { error.value = '併批目標需存在且產品/流程相同。'; return }
  } else if (lotOption.value === 'split' && (!norm(childLotNo.value) || norm(childLotNo.value) === row.lotNo)) { error.value = '請輸入不同的新批號。'; return }
  const kind = lotOption.value === 'split' ? 'split' : lotOption.value === 'merge' ? 'merge' : 'qtyAdjust'
  if (!record({ erfNo: row.erfNo, kind, status: row.status, description: `${releaseDisp.value.trim()} ${norm(childLotNo.value)}`.trim(), actor: props.currentUserNo || (dev ? 'DEV' : ''), at: new Date().toISOString(), releaseType: lotOptions.find(item => item.key === lotOption.value)?.releaseType, curQty: kind === 'qtyAdjust' ? qty : row.curQty })) return
  dialog.value = null; query(); message.value = `DEV 已記錄 ${row.erfNo} ${kind} 示意；正式 Lot 交易尚未執行。`
}
function openAbort(): void {
  if (!dev.value) { clearFormal(); return }
 error.value = ''; if (!active.value || !permittedExact(active.value)) { error.value = '請選有處置權限的異常單。'; return }; if (active.value.ocapNo) { error.value = 'OCAP 異常單不允許 ERF Abort。'; return }; dialog.value = 'abort' }
function confirmAbort(): void {
  if (!dev.value) { clearFormal(); return }

  const row = active.value
  if (!row || !permittedExact(row) || row.ocapNo) return
  if (!record({ erfNo: row.erfNo, kind: 'abort', status: 12, description: 'ERF Abort', actor: props.currentUserNo || (dev ? 'DEV' : ''), at: new Date().toISOString() })) return
  dialog.value = null; query(); message.value = `DEV 已模擬 ${row.erfNo} 作廢；正式應依 ERFType 走單筆或批次 Service。`
}
function openRecreate(): void {
  if (!dev.value) { clearFormal(); return }
 error.value = ''; if (!active.value || !permittedExact(active.value)) { error.value = '請選有處置權限的異常單。'; return }; dialog.value = 'recreate' }
function confirmRecreate(): void {
  if (!dev.value) { clearFormal(); return }

  const row = active.value
  if (!row || !permittedExact(row)) return
  loadSaved()
  const newNo = `DEV-ERF-NEW-${String(saved.value.nextSerial).padStart(4,'0')}`
  if (allRows().some(item => item.erfNo === newNo)) { error.value = '假異常單號已存在。'; return }
  saved.value.recreated.push({ ...row, erfNo: newNo, erfType: 5, status: 0, creator: props.currentUserNo || (dev ? 'DEV' : ''), createDate: new Date().toISOString(), dispDescription: '', ocapNo: '', batchLot: '' })
  saved.value.nextSerial += 1
  if (!persist()) { saved.value.recreated.pop(); saved.value.nextSerial -= 1; return }
  dialog.value = null; flags.erfNo = true; filters.erfNo = newNo; query(); message.value = `DEV 已模擬以 ERFType=5 重開 ${newNo}。`
}
function openInfo(): void {
  if (!dev.value) { clearFormal(); return }
 if (!active.value) { error.value = '請先選擇異常單。'; return }; infoTab.value = 'Create Hold Code'; dialog.value = 'info' }
for (const field of queryFields) watch(() => flags[field.key], enabled => { if (!enabled) filters[field.key] = '' })
function loadPreview(): void { loadSaved() }
onMounted(() => { if (dev.value) loadPreview(); else clearFormal() })
watch(dev, enabled => { if (enabled) { message.value = previewMessage; loadPreview() } else clearFormal() })
</script>

<template>
  <section class="erf-page">
    <h1>未解除異常單 <small>WIP ERF UNRELEASED · WP509</small></h1>
    <p v-if="dev" class="note">DEV 假資料；ERF／Lot／OCAP 交易需正式 API 完成。</p>
    <p v-if="!dev" class="note" role="status">{{ unavailableMessage }}</p>
    <p v-if="error" class="error" role="alert">{{ error }}</p>
    <fieldset><legend>Query</legend><div class="query-grid">
      <label v-for="field in queryFields" :key="field.key"><span><input v-model="flags[field.key]" type="checkbox" :disabled="!dev"> {{ field.label }}</span><input v-model="filters[field.key]" class="yellow" :disabled="!dev || !flags[field.key]" @keydown.enter.prevent="dev && (query())"><button v-if="field.lookup" type="button" :disabled="!dev" @click="dev && (lookup = field.key)">...</button></label>
      <button type="button" class="button query-button" :disabled="!dev" @click="dev && (query())">Query</button>
    </div></fieldset>
    <fieldset class="result"><legend>Result</legend><div class="result-layout"><div class="scroll"><table><thead><tr><th v-for="column in columns" :key="column.key + column.label">{{ column.label }}</th></tr></thead><tbody>
      <tr v-for="row in rows" :key="row.erfNo" :class="{ active: activeNo === row.erfNo }" @click="dev && (activeNo = row.erfNo)" @dblclick="openInfo"><td v-for="column in columns" :key="column.key + column.label">{{ cell(row, column.key, column.label) }}</td></tr>
      <tr v-if="!rows.length"><td :colspan="columns.length" class="empty">{{ queried ? '沒有符合條件的未解除異常單。' : '請先 Query。' }}</td></tr>
    </tbody></table></div><div class="actions"><button type="button" class="button" :disabled="!active || !dev" @click="dev && (openDisposition())">ERF Disp</button><button type="button" class="button" :disabled="!active || !dev" @click="dev && (openAbort())">ERF Abort</button><button type="button" class="button" :disabled="!active || !dev" @click="dev && (openRecreate())">ERF Create</button><button type="button" class="button" :disabled="!dev || (!active)" @click="dev && (openInfo())">ERF Info</button><button type="button" class="button close-button" @click="emit('close')">Close (X)</button></div></div><div class="status"><span>{{ message }}</span><span>UserNo {{ currentUserNo || (dev ? 'DEV' : '') }} · {{ rows.length }} 筆</span></div></fieldset>
    <div v-if="dev && (ocapPicker)" class="overlay"><div class="dialog small" role="dialog" aria-modal="true"><h2>OCAP Process Show</h2><p>選擇未完成的 OCAP 節點；DEV 示意。</p><label><input v-model="ocapNode" type="radio" :value="active?.ocapNo + '-NODE-01'" :disabled="!dev"> {{ active?.ocapNo }} · NODE-01 · 待處置</label><div class="dialog-actions"><button type="button" class="button primary" @click="dev && (chooseOcap())" :disabled="!dev">Confirm</button><button type="button" class="button" @click="dev && (ocapPicker = false)" :disabled="!dev">Close</button></div></div></div>
    <div v-if="dev && (dialog === 'disposition' && active)" class="overlay"><div class="dialog disposition-dialog" role="dialog" aria-modal="true"><h2>ERF Disposition · {{ active.erfNo }}</h2><fieldset class="basis"><legend>ERFBasis</legend><span>Lot No: {{ active.lotNo }}</span><span>ERF Type: {{ active.erfType }}</span><span>Status: {{ statusLabels[active.status] }}</span><span>CurGroup: {{ active.curGroupNo }}</span><span>Hold Description: {{ active.holdDescription }}</span></fieldset><div class="tabs"><button type="button" class="button" :class="{ selected: tab === 'erf' }" :disabled="!dev || (!canErfDisposition)" @click="dev && (tab = 'erf')">ERF Disposition</button><button type="button" class="button" :class="{ selected: tab === 'lot' }" :disabled="!dev || (!canLotDisposition)" @click="dev && (tab = 'lot')">Lot Disposition</button></div>
      <template v-if="tab === 'erf'"><fieldset><legend>Disposition Select</legend><div class="radios"><label><input v-model="erfDispType" type="radio" value="next" :disabled="!dev"> Select next disp group</label><label><input v-model="erfDispType" type="radio" value="notice" :disabled="!dev"> Notice Customer</label><label><input v-model="erfDispType" type="radio" value="final" :disabled="!dev"> Final Disposition</label></div><label>Disp Description <textarea v-model="dispDescription" class="yellow" :disabled="!dev"></textarea></label><label v-if="erfDispType === 'next'">Next Group <input v-model="nextGroup" readonly class="yellow" :disabled="!dev"><button type="button" @click="dev && (lookup = 'nextGroup')" :disabled="!dev">...</button></label><label v-if="erfDispType === 'notice'">Contactor Name <input v-model="contactorName" class="yellow" :disabled="!dev"></label><label v-if="erfDispType === 'final'">Final Group <input v-model="finalGroup" readonly class="yellow" :disabled="!dev"><button type="button" @click="dev && (lookup = 'finalGroup')" :disabled="!dev">...</button></label><label v-if="active.ocapNo">OCAP Result <input v-model="ocapResult" class="yellow" :disabled="!dev"></label></fieldset>
        <fieldset><legend>Hold Item</legend><div class="scroll short"><table><thead><tr><th>Hold Item No</th><th>Good Qty</th><th>Scrap Qty</th><th>Description</th><th>Hold Disposition</th><th>Action</th></tr></thead><tbody><tr v-for="(item,index) in reasons" :key="index"><td>{{ item.holdItemNo }}</td><td>{{ item.goodQty }}</td><td>{{ item.scrapQty }}</td><td>{{ item.description }}</td><td>{{ item.holdDisposition }}</td><td><button type="button" @click="dev && (editReason(index))" :disabled="!dev">Edit</button><button type="button" @click="dev && (reasons.splice(index, 1))" :disabled="!dev">Del</button></td></tr></tbody></table></div><div class="reason-edit"><input v-model="reasonDraft.holdItemNo" placeholder="Hold Item No" class="yellow" :disabled="!dev"><input v-model="reasonDraft.goodQty" type="number" placeholder="Good Qty" class="yellow" :disabled="!dev"><input v-model="reasonDraft.scrapQty" type="number" placeholder="Scrap Qty" class="yellow" :disabled="!dev"><input v-model="reasonDraft.description" placeholder="Description" class="yellow" :disabled="!dev"><input v-model="reasonDraft.holdDisposition" placeholder="Hold Disposition" class="yellow" :disabled="!dev"><button type="button" class="button" @click="dev && (addReason())" :disabled="!dev">{{ reasonIndex >= 0 ? 'Apply' : 'Add' }}</button></div></fieldset>
        <fieldset><legend>ERF Attach</legend><div class="reason-edit"><input v-model="attachmentDraft" class="yellow" placeholder="DEV 檔名" :disabled="!dev"><button type="button" class="button" @click="dev && (addAttachment())" :disabled="!dev">Add</button><span v-for="name in attachmentNames" :key="name">{{ name }} <button type="button" @click="dev && (attachmentNames = attachmentNames.filter(item => item !== name))" :disabled="!dev">Delete</button></span></div></fieldset>
      </template>
      <template v-else><fieldset><legend>Lot Disposition</legend><div class="lot-layout"><div class="lot-options"><label v-for="item in lotOptions" v-show="active.erfType !== 6 || !['scrap','withdraw'].includes(item.key)" :key="item.key"><input v-model="lotOption" type="radio" name="erf-lot-option" :value="item.key" :disabled="!dev || (!optionAvailable(item.key))"> {{ item.label }}</label></div><div class="target-fields"><fieldset v-if="['jumpOp','jumpMain','jumpProcess','tuning'].includes(lotOption)"><legend>Target Routing</legend><label v-if="['jumpProcess','tuning'].includes(lotOption)">Process No <input v-model="target.processNo" readonly class="yellow" :disabled="!dev"><button type="button" @click="dev && (lookup = 'targetProcess')" :disabled="!dev">...</button></label><label>OP No <input v-model="target.opNo" readonly class="yellow" :disabled="!dev"><button type="button" @click="dev && (lookup = 'targetOp')" :disabled="!dev">...</button></label><template v-if="lotOption === 'jumpProcess'"><label><input v-model="returnOrg" type="checkbox" :disabled="!dev"> Return Original Process</label><label v-if="returnOrg">Return OP <input v-model="target.returnOp" readonly class="yellow" :disabled="!dev"><button type="button" @click="dev && (lookup = 'returnOp')" :disabled="!dev">...</button></label></template></fieldset><fieldset v-if="lotOption === 'jumpModule'"><legend>Jump Module</legend><label>Module <input v-model="target.moduleNo" readonly class="yellow" :disabled="!dev"><button type="button" @click="dev && (lookup = 'targetModule')" :disabled="!dev">...</button></label><label>Version <input v-model="target.moduleVersion" readonly class="yellow" :disabled="!dev"></label><label>Return Main OP <input v-model="target.returnOp" readonly class="yellow" :disabled="!dev"><button type="button" @click="dev && (lookup = 'returnOp')" :disabled="!dev">...</button></label></fieldset><fieldset v-if="['semiInv','scrapInv'].includes(lotOption)"><legend>Inventory</legend><label>Inventory No <input v-model="target.inventoryNo" readonly class="yellow" :disabled="!dev"><button type="button" @click="dev && (lookup = 'inventory')" :disabled="!dev">...</button></label></fieldset><label>Release Disp <textarea v-model="releaseDisp" class="yellow" :disabled="!dev"></textarea></label></div></div></fieldset></template>
      <div class="dialog-actions"><button type="button" class="button primary" @click="dev && (tab === 'erf' ? confirmErfDisposition() : confirmLot())" :disabled="!dev">Confirm (C)</button><button type="button" class="button" @click="dev && (dialog = null)" :disabled="!dev">Close (X)</button></div></div></div>
    <div v-if="dev && (dialog === 'abort' && active)" class="overlay"><div class="dialog small" role="dialog" aria-modal="true"><h2>ERF Abort · {{ active.erfNo }}</h2><p>{{ active.erfType === 6 ? 'Batch' : 'Single' }} 異常單作廢；DEV 假資料。</p><div class="dialog-actions"><button type="button" class="button primary" @click="dev && (confirmAbort())" :disabled="!dev">Confirm</button><button type="button" class="button" @click="dev && (dialog = null)" :disabled="!dev">Cancel</button></div></div></div>
    <div v-if="dev && (dialog === 'recreate' && active)" class="overlay"><div class="dialog small" role="dialog" aria-modal="true"><h2>ERF Create · {{ active.erfNo }}</h2><p>以 Lot {{ active.lotNo }} 開立 ERFType=5；DEV 模擬。</p><div class="dialog-actions"><button type="button" class="button primary" @click="dev && (confirmRecreate())" :disabled="!dev">Confirm</button><button type="button" class="button" @click="dev && (dialog = null)" :disabled="!dev">Cancel</button></div></div></div>
    <div v-if="dev && (dialog === 'info' && active)" class="overlay"><div class="dialog info-dialog" role="dialog" aria-modal="true"><h2>ERF Information · {{ active.erfNo }}</h2><fieldset class="basis"><legend>ERFBasis</legend><span>Lot No: {{ active.lotNo }}</span><span>Product No: {{ active.productNo }}</span><span>OP No: {{ active.opNo }}</span><span>Status: {{ statusLabels[active.status] }}</span><span>CurGroup: {{ active.curGroupNo }}</span><span>Hold: {{ active.holdDescription }}</span></fieldset><div class="tabs"><button v-for="name in ['Create Hold Code','Hold Code Disposition','ERF Disposition','Lot Disposition','ERF Attach']" :key="name" type="button" class="button" :class="{ selected: infoTab === name }" @click="dev && (infoTab = name)" :disabled="!dev">{{ name }}</button></div><div class="info-body">
  <template v-if="infoTab === 'Create Hold Code'">{{ active.holdDescription }}</template>
  <template v-else-if="infoTab === 'Hold Code Disposition'"><div v-for="(item,index) in latestReasons" :key="index">{{ item.holdItemNo }} · Good {{ item.goodQty }} · Scrap {{ item.scrapQty }} · {{ item.holdDisposition }}</div><p v-if="!latestReasons.length">沒有 DEV Hold Reason Disp。</p></template>
  <template v-else-if="infoTab === 'ERF Attach'"><div v-for="name in latestAttachments" :key="name">{{ name }}</div><p v-if="!latestAttachments.length">沒有 DEV 附件檔名；正式附件需 WP508 Info API。</p></template>
  <template v-else><div v-for="(item,index) in activeActions.filter(action => infoTab === 'ERF Disposition' ? action.kind === 'erfDisposition' : action.kind !== 'erfDisposition')" :key="index">{{ item.at }} · {{ item.kind }} · {{ item.description }}</div><p v-if="!activeActions.length">無 DEV 處置紀錄。</p></template>
</div><div class="dialog-actions"><button type="button" class="button" @click="dev && (dialog = null)" :disabled="!dev">Close</button></div></div></div>
    <div v-if="dev && (dialog === 'child' && active)" class="overlay"><div class="dialog small" role="dialog" aria-modal="true"><h2>{{ lotOption }} · {{ childVariant }}</h2><p>DEV 簡化示意；正式版依原 WinForms 子窗與 Service/Core 處理。</p><label v-if="lotOption !== 'qtyAdjust'">{{ lotOption === 'split' ? 'New Lot No' : 'Merge Lot No' }} <input v-model="childLotNo" class="yellow" :disabled="!dev"><button v-if="lotOption === 'merge'" type="button" @click="dev && (lookup = 'mergeLot')" :disabled="!dev">...</button></label><label>{{ lotOption === 'split' ? 'Split Qty' : lotOption === 'qtyAdjust' ? 'New Qty' : 'Merge Qty' }} <input v-model="childQty" type="number" class="yellow" :disabled="!dev"></label><div class="dialog-actions"><button type="button" class="button primary" @click="dev && (confirmChild())" :disabled="!dev">Confirm</button><button type="button" class="button" @click="dev && (dialog = 'disposition')" :disabled="!dev">Close</button></div></div></div>
    <div v-if="dev && (lookup)" class="overlay lookup-overlay"><div class="dialog small" role="dialog" aria-modal="true"><h2>Lookup · {{ lookup }}</h2><div class="lookup-list"><button v-for="value in lookupOptions" :key="value" type="button" class="button" @click="dev && (pick(value))" :disabled="!dev">{{ value }}</button><span v-if="!lookupOptions.length">沒有可選的 DEV 資料。</span></div><button type="button" class="button" @click="dev && (lookup = null)" :disabled="!dev">Close</button></div></div>
  </section>
</template>

<style scoped>
.erf-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:7px;padding:9px;box-sizing:border-box;background:#f7f5ee;color:#303030;font:13px Arial,"Microsoft JhengHei",sans-serif}h1{margin:0;font-size:18px}h1 small{font-size:12px;font-weight:400;color:#777}.note,.error{margin:0;padding:6px;border:1px solid #d9c88e;background:#fff8dc}.error{border-color:#d6aaa6;background:#fff0ef;color:#a22}fieldset{min-width:0;margin:0;padding:7px 8px;border:1px solid #bbb;background:#fff}legend{padding:0 5px}.query-grid{display:grid;grid-template-columns:repeat(3,minmax(210px,1fr));gap:7px}.query-grid label,.dialog label,.target-fields label{display:flex;align-items:center;gap:5px}.query-grid label span{width:108px;white-space:nowrap}.query-grid input:not([type=checkbox]),.dialog input:not([type=radio]):not([type=checkbox]),.dialog textarea{min-width:0;height:25px;box-sizing:border-box;padding:3px 5px;border:1px solid #888;font:inherit}.yellow{background:#fff0b3}.query-grid input:not([type=checkbox]){width:150px}.query-grid input:disabled{background:#eee}.button,.query-grid button,.dialog label button,.dialog table button{min-height:26px;padding:3px 9px;border:1px solid #b5b5b5;border-radius:3px;background:linear-gradient(#fff,#eee);color:#333;font:inherit;cursor:pointer}.button:disabled{color:#aaa;cursor:default}.query-button{grid-column:3;justify-self:end}.result{flex:1;min-height:200px;display:flex;flex-direction:column}.result-layout{display:flex;flex:1;min-height:0;gap:8px}.scroll{overflow:auto;min-width:0;flex:1;border:1px solid #aaa}.scroll table{min-width:2220px;border-collapse:collapse;white-space:nowrap}.scroll th,.scroll td{padding:4px 7px;border:1px solid #ccc;text-align:left}.scroll th{position:sticky;top:0;background:#ead591;font-weight:500}.scroll td{background:#ffe89c}.scroll tr.active td{background:#ffdb73}.scroll .empty{text-align:center;background:#fff;color:#777;height:38px}.actions{display:flex;flex-direction:column;gap:6px;width:88px}.close-button{margin-top:auto}.status{display:flex;justify-content:space-between;color:#666;padding-top:5px}.overlay{position:fixed;inset:0;z-index:2000;display:flex;align-items:center;justify-content:center;padding:15px;background:#0007}.lookup-overlay{z-index:2100}.dialog{width:min(930px,96vw);max-height:90vh;overflow:auto;display:flex;flex-direction:column;gap:8px;padding:13px;border:1px solid #888;background:#f7f5ee;box-shadow:0 8px 28px #0005}.dialog.small{width:min(490px,95vw)}.dialog h2,.dialog p{margin:0}.dialog fieldset{display:flex;flex-direction:column;gap:6px}.dialog .basis{flex-direction:row;flex-wrap:wrap;gap:6px 20px}.tabs,.radios,.reason-edit,.dialog-actions,.lookup-list{display:flex;flex-wrap:wrap;gap:6px;align-items:center}.tabs .selected{background:#ffe89c}.dialog textarea{height:55px;min-width:220px}.dialog-actions{justify-content:flex-end}.primary{background:#ffb43a;border-color:#e3a235}.short{max-height:115px}.short table{min-width:750px}.reason-edit input{width:110px}.lot-layout{display:grid;grid-template-columns:240px 1fr;gap:10px}.lot-options{display:grid;grid-template-columns:1fr 1fr;gap:3px}.target-fields fieldset{margin-bottom:6px}.target-fields input:not([type=radio]):not([type=checkbox]){width:140px}.info-body{min-height:80px;border:1px solid #ccc;padding:9px;background:#fff}.lookup-list{max-height:230px;overflow:auto}@media(max-width:950px){.query-grid{grid-template-columns:1fr 1fr}.query-button{grid-column:2}.result-layout{min-height:210px}.lot-layout{grid-template-columns:1fr}}

/* The framework owns the page title for this opt-in mount. */
.erf-page.office-wip-module > h1 { display: none; }
</style>
