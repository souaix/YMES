<script setup lang="ts">
import { computed, ref } from 'vue'

type SetupStatus = 0 | 1 | 2 | 99
type DetailMode = 'add' | 'edit' | 'view' | 'close'
type PlannedAccessory = {
  equipmentNo: string
  accessoryType: string
  qty: number
  accessoryNo: string
  accessoryGroup: string
  socket: boolean
}
type CurrentAccessory = {
  equipmentNo: string
  equipmentType: string
  accessoryType: string
  accessoryNo: string
  version: string
  applySerialNo: string
  selected: boolean
  siteNo: string
  siteState: string
  socket: boolean
}
type SetupRow = {
  setupNo: string
  mainEquipmentNo: string
  mainEquipmentType: 'PROBER' | 'HANDLER'
  runCardNo: string
  opNo: string
  pid: string
  testProgram: string
  ecnNo: string
  creator: string
  createDate: string
  status: SetupStatus
  closeDate: string
  closeUser: string
  testerNos: string[]
  plannedAccessories: PlannedAccessory[]
  currentAccessories: CurrentAccessory[]
}
type SetupDraft = Omit<SetupRow, 'status' | 'closeDate' | 'closeUser' | 'createDate'>

const props = withDefaults(defineProps<{ currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '' })
const emit = defineEmits<{ close: [] }>()
const currentUser = computed(() => props.currentUserNo || 'DEVUSER01')
const storageKey = 'ymes.dev.wpp03-eqp-setup-basis-cp.v1'

const statusMeta: Record<SetupStatus, { label: string; tone: string }> = {
  0: { label: '新建', tone: 'new' },
  1: { label: '配件發料', tone: 'issued' },
  2: { label: '已生效', tone: 'effective' },
  99: { label: '歸檔', tone: 'archived' },
}
const statusOptions: SetupStatus[] = [0, 1, 2, 99]
const mainEquipmentOptions = [
  { no: 'DEV-PROBER-01', name: 'TEL P8XL-DEV', type: 'PROBER' as const, state: 'Idle', running: false },
  { no: 'DEV-PROBER-02', name: 'UF2000-DEV', type: 'PROBER' as const, state: 'Running', running: true },
  { no: 'DEV-PROBER-03', name: 'DISCO DFD-DEV', type: 'PROBER' as const, state: 'Idle', running: false },
]
const testerOptions = [
  { no: 'DEV-TESTER-01', name: 'T2000-DEV', type: 'TESTER', state: 'Idle', running: false },
  { no: 'DEV-TESTER-02', name: '93K-DEV', type: 'TESTER', state: 'Idle', running: false },
  { no: 'DEV-TESTER-03', name: 'UFLEX-DEV', type: 'TESTER', state: 'Maintenance', running: false },
]
const runCards = [
  { no: 'DEV-CPLOT-24001', product: 'DEV-CP-1001', device: 'DEV-DEVICE-A' },
  { no: 'DEV-CPLOT-24002', product: 'DEV-CP-1002', device: 'DEV-DEVICE-B' },
  { no: 'DEV-CPLOT-24003', product: 'DEV-CP-1003', device: 'DEV-DEVICE-C' },
]
const opOptions = [
  { no: 'DEV-CP-OP-010', name: '晶圓針測', pid: 'DEV-PID-CP-01', program: 'DEV-CP-PGM-01', ecn: 'DEV-ECN-CP-101' },
  { no: 'DEV-CP-OP-020', name: 'CP 複測', pid: 'DEV-PID-CP-02', program: 'DEV-CP-PGM-02', ecn: 'DEV-ECN-CP-102' },
]
const accessoryRules: Record<string, PlannedAccessory[]> = {
  'DEV-PROBER-01': [
    { equipmentNo: 'DEV-PROBER-01', accessoryType: 'SOCKET', qty: 2, accessoryNo: '*', accessoryGroup: 'DEV-GRP-SOCKET', socket: true },
    { equipmentNo: 'DEV-PROBER-01', accessoryType: 'PROBE CARD', qty: 1, accessoryNo: '*', accessoryGroup: '', socket: false },
  ],
  'DEV-PROBER-02': [
    { equipmentNo: 'DEV-PROBER-02', accessoryType: 'SOCKET', qty: 2, accessoryNo: '*', accessoryGroup: 'DEV-GRP-SOCKET', socket: true },
  ],
  'DEV-PROBER-03': [
    { equipmentNo: 'DEV-PROBER-03', accessoryType: 'SOCKET', qty: 1, accessoryNo: '*', accessoryGroup: '', socket: true },
    { equipmentNo: 'DEV-PROBER-03', accessoryType: 'ALIGNMENT JIG', qty: 1, accessoryNo: 'DEV-JIG-03', accessoryGroup: '', socket: false },
  ],
  'DEV-TESTER-01': [
    { equipmentNo: 'DEV-TESTER-01', accessoryType: 'LOAD BOARD', qty: 1, accessoryNo: '*', accessoryGroup: 'DEV-GRP-LB', socket: false },
  ],
  'DEV-TESTER-02': [
    { equipmentNo: 'DEV-TESTER-02', accessoryType: 'LOAD BOARD', qty: 1, accessoryNo: '*', accessoryGroup: 'DEV-GRP-LB', socket: false },
    { equipmentNo: 'DEV-TESTER-02', accessoryType: 'CONTACTOR', qty: 1, accessoryNo: '*', accessoryGroup: '', socket: false },
  ],
  'DEV-TESTER-03': [
    { equipmentNo: 'DEV-TESTER-03', accessoryType: 'LOAD BOARD', qty: 1, accessoryNo: '*', accessoryGroup: '', socket: false },
  ],
}

function makeCurrentAccessories(equipmentNo: string, equipmentType: string, serialBase: string): CurrentAccessory[] {
  const rules = accessoryRules[equipmentNo] || []
  return rules.flatMap((rule, index) => Array.from({ length: rule.qty }, (_, copy) => ({
    equipmentNo,
    equipmentType,
    accessoryType: rule.accessoryType,
    accessoryNo: rule.accessoryType === 'SOCKET'
      ? `DEV-SCK-${serialBase}-${String(index * rule.qty + copy + 1).padStart(2, '0')}`
      : `DEV-ACC-${serialBase}-${String(index + 1).padStart(2, '0')}`,
    version: 'DEV-V01',
    applySerialNo: `DEV-APPLY-${serialBase}`,
    selected: false,
    siteNo: rule.socket ? String(copy + 1) : '',
    siteState: rule.socket ? 'ON' : '',
    socket: rule.socket,
  })))
}
function makeEquipmentAccessoryInventory(equipmentNo: string, equipmentType: string, serialBase: string): CurrentAccessory[] {
  const installed = makeCurrentAccessories(equipmentNo, equipmentType, serialBase)
  // The DEV PROBER-03 fixture deliberately has no installed alignment jig, so it exercises Status=0 accessory issue.
  return equipmentNo === 'DEV-PROBER-03' ? installed.filter((item) => item.accessoryType !== 'ALIGNMENT JIG') : installed
}

const initialRows: SetupRow[] = [
  {
    setupNo: 'SFN26B21003', mainEquipmentNo: 'DEV-PROBER-03', mainEquipmentType: 'PROBER',
    runCardNo: 'DEV-CPLOT-24001', opNo: 'DEV-CP-OP-010', pid: 'DEV-PID-CP-01',
    testProgram: 'DEV-CP-PGM-01', ecnNo: 'DEV-ECN-CP-101',
    creator: 'DEVUSER01', createDate: '2026-11-21 13:45', status: 0, closeDate: '', closeUser: '',
    testerNos: ['DEV-TESTER-01'],
    plannedAccessories: [...(accessoryRules['DEV-PROBER-03'] || []), ...(accessoryRules['DEV-TESTER-01'] || [])],
    currentAccessories: [...makeEquipmentAccessoryInventory('DEV-PROBER-03', 'PROBER', 'P03B'), ...makeEquipmentAccessoryInventory('DEV-TESTER-01', 'TESTER', 'T01B')],
  },
  {
    setupNo: 'SFN26B22001', mainEquipmentNo: 'DEV-PROBER-01', mainEquipmentType: 'PROBER',
    runCardNo: 'DEV-CPLOT-24001', opNo: 'DEV-CP-OP-010', pid: 'DEV-PID-CP-01',
    testProgram: 'DEV-CP-PGM-01', ecnNo: 'DEV-ECN-CP-101',
    creator: 'DEVUSER01', createDate: '2026-11-22 08:10', status: 1, closeDate: '', closeUser: '',
    testerNos: ['DEV-TESTER-01'],
    plannedAccessories: [...(accessoryRules['DEV-PROBER-01'] || []), ...(accessoryRules['DEV-TESTER-01'] || [])],
    currentAccessories: [...makeCurrentAccessories('DEV-PROBER-01', 'PROBER', 'P01'), ...makeCurrentAccessories('DEV-TESTER-01', 'TESTER', 'T01')],
  },
  {
    setupNo: 'SFN26B18004', mainEquipmentNo: 'DEV-PROBER-03', mainEquipmentType: 'PROBER',
    runCardNo: 'DEV-CPLOT-24002', opNo: 'DEV-CP-OP-020', pid: 'DEV-PID-CP-02',
    testProgram: 'DEV-CP-PGM-02', ecnNo: 'DEV-ECN-CP-102',
    creator: 'DEVUSER02', createDate: '2026-11-18 15:22', status: 2, closeDate: '2026-11-18 16:05', closeUser: 'DEVUSER02',
    testerNos: ['DEV-TESTER-02'],
    plannedAccessories: [...(accessoryRules['DEV-PROBER-03'] || []), ...(accessoryRules['DEV-TESTER-02'] || [])],
    currentAccessories: [...makeCurrentAccessories('DEV-PROBER-03', 'PROBER', 'P03'), ...makeCurrentAccessories('DEV-TESTER-02', 'TESTER', 'T02')],
  },
  {
    setupNo: 'SFN26B20002', mainEquipmentNo: 'DEV-PROBER-02', mainEquipmentType: 'PROBER',
    runCardNo: 'DEV-CPLOT-24003', opNo: 'DEV-CP-OP-010', pid: 'DEV-PID-CP-01',
    testProgram: 'DEV-CP-PGM-01', ecnNo: 'DEV-ECN-CP-101',
    creator: 'DEVUSER03', createDate: '2026-11-20 10:06', status: 99, closeDate: '2026-11-20 12:33', closeUser: 'DEVUSER03',
    testerNos: ['DEV-TESTER-03'],
    plannedAccessories: [...(accessoryRules['DEV-PROBER-02'] || []), ...(accessoryRules['DEV-TESTER-03'] || [])],
    currentAccessories: [...makeCurrentAccessories('DEV-PROBER-02', 'PROBER', 'P02'), ...makeCurrentAccessories('DEV-TESTER-03', 'TESTER', 'T03')],
  },
]

function loadRows(): SetupRow[] {
  if (import.meta.env.DEV && props.developmentPreview === true) {
    try {
      const saved = localStorage.getItem(storageKey)
      if (saved) return JSON.parse(saved) as SetupRow[]
    } catch { /* keep deterministic fixture rows */ }
  }
  return initialRows.map((row) => structuredClone(row))
}
function formatLocalDateTime(value: Date): string {
  return new Date(value.getTime() - value.getTimezoneOffset() * 60000).toISOString().slice(0, 16)
}
function getDefaultDateRange(): { from: string; to: string } {
  const now = new Date()
  const from = new Date(now)
  if (now.getHours() < 7 || (now.getHours() === 7 && now.getMinutes() < 30)) from.setDate(from.getDate() - 1)
  from.setHours(7, 30, 0, 0)
  return { from: formatLocalDateTime(from), to: formatLocalDateTime(now) }
}
const initialDateRange = getDefaultDateRange()
const rows = ref<SetupRow[]>(loadRows())
const filters = ref({ setupNo: '', mainEquipmentNo: '', runCardNo: '', opNo: '', createFrom: initialDateRange.from, createTo: initialDateRange.to, closeFrom: initialDateRange.from, closeTo: initialDateRange.to })
const useCreateRange = ref(false)
const useCloseRange = ref(false)
const useStatus = ref(false)
const statusFilter = ref<SetupStatus[]>([0, 1, 2, 99])
const searched = ref(false)
const selectedSetupNo = ref('')
const mode = ref<DetailMode | null>(null)
const draft = ref<SetupDraft | null>(null)
const errorMessage = ref('')
const noticeMessage = ref('')
const selectedRow = computed(() => rows.value.find((row) => row.setupNo === selectedSetupNo.value) || null)
const filteredRows = computed(() => {
  if (!searched.value) return []
  return rows.value.filter((row) => {
    if (filters.value.setupNo && row.setupNo.toUpperCase() !== filters.value.setupNo.toUpperCase()) return false
    if (filters.value.mainEquipmentNo && row.mainEquipmentNo.toUpperCase() !== filters.value.mainEquipmentNo.toUpperCase()) return false
    if (filters.value.runCardNo && row.runCardNo.toUpperCase() !== filters.value.runCardNo.toUpperCase()) return false
    if (filters.value.opNo && row.opNo.toUpperCase() !== filters.value.opNo.toUpperCase()) return false
    if (useStatus.value && statusFilter.value.length && !statusFilter.value.includes(row.status)) return false
    if (useCreateRange.value && !insideRange(row.createDate, filters.value.createFrom, filters.value.createTo)) return false
    if (useCloseRange.value && (!row.closeDate || !insideRange(row.closeDate, filters.value.closeFrom, filters.value.closeTo))) return false
    return true
  })
})
const modeLabel = computed(() => ({ add: '新增設備設置單', edit: '編輯新建單', view: '設備設置單明細', close: '設備設置單結單' }[mode.value || 'view']))
const runningEquipment = computed(() => [draft.value?.mainEquipmentNo, ...(draft.value?.testerNos || [])]
  .filter((equipmentNo): equipmentNo is string => Boolean(equipmentNo))
  .filter((equipmentNo) => mainEquipmentOptions.some((item) => item.no === equipmentNo && item.running)
    || testerOptions.some((item) => item.no === equipmentNo && item.running)))

function insideRange(source: string, from: string, to: string): boolean {
  const value = source.replace(' ', 'T')
  return (!from || value >= from) && (!to || value <= to)
}
function installedCount(rule: PlannedAccessory): number {
  return draft.value?.currentAccessories.filter((item) => item.equipmentNo === rule.equipmentNo && item.accessoryType === rule.accessoryType).length || 0
}
function persist(): void {
  if (!import.meta.env.DEV || props.developmentPreview !== true) return
  try { localStorage.setItem(storageKey, JSON.stringify(rows.value)) } catch { noticeMessage.value = '瀏覽器無法保存本機 DEV 狀態。' }
}
function query(): void {
  errorMessage.value = ''
  if (useCreateRange.value && (!filters.value.createFrom || !filters.value.createTo || filters.value.createFrom >= filters.value.createTo)) {
    errorMessage.value = '建立日期區間需完整填寫，且起始需早於結束。'
    return
  }
  if (useCloseRange.value && (!filters.value.closeFrom || !filters.value.closeTo || filters.value.closeFrom >= filters.value.closeTo)) {
    errorMessage.value = '結單日期區間需完整填寫，且起始需早於結束。'
    return
  }
  searched.value = true
  selectedSetupNo.value = ''
  noticeMessage.value = `查詢完成，共 ${filteredRows.value.length} 筆 DEV 設置單。`
}
function resetFilters(): void {
  const range = getDefaultDateRange()
  filters.value = { setupNo: '', mainEquipmentNo: '', runCardNo: '', opNo: '', createFrom: range.from, createTo: range.to, closeFrom: range.from, closeTo: range.to }
  useCreateRange.value = false
  useCloseRange.value = false
  useStatus.value = false
  statusFilter.value = [0, 1, 2, 99]
  errorMessage.value = ''
}
function makeDraft(row?: SetupRow): SetupDraft {
  if (row) {
    return {
      setupNo: row.setupNo, mainEquipmentNo: row.mainEquipmentNo, mainEquipmentType: row.mainEquipmentType,
      runCardNo: row.runCardNo, opNo: row.opNo, pid: row.pid, testProgram: row.testProgram,
      ecnNo: row.ecnNo, creator: row.creator, testerNos: [...row.testerNos],
      plannedAccessories: row.plannedAccessories.map((item) => ({ ...item })),
      currentAccessories: row.currentAccessories.map((item) => ({ ...item })),
    }
  }
  return {
    setupNo: '', mainEquipmentNo: '', mainEquipmentType: 'PROBER', runCardNo: '', opNo: '',
    pid: '', testProgram: '', ecnNo: '', creator: currentUser.value,
    testerNos: [], plannedAccessories: [], currentAccessories: [],
  }
}
function open(modeValue: DetailMode, row?: SetupRow): void {
  if (modeValue === 'edit' && row?.status !== 0) return
  if (modeValue === 'close' && row?.status !== 1) return
  mode.value = modeValue
  draft.value = makeDraft(row)
  errorMessage.value = ''
  noticeMessage.value = ''
  if (modeValue === 'close' && draft.value) {
    const selectedEquipment = new Set([draft.value.mainEquipmentNo, ...draft.value.testerNos])
    draft.value.currentAccessories = draft.value.currentAccessories
      .filter((accessory) => selectedEquipment.has(accessory.equipmentNo))
      .map((accessory) => ({ ...accessory, selected: false }))
  }
}
function updateOp(): void {
  if (!draft.value) return
  const op = opOptions.find((item) => item.no === draft.value?.opNo)
  if (op) {
    draft.value.pid = op.pid
    draft.value.testProgram = op.program
    draft.value.ecnNo = op.ecn
  }
}
function updateEquipmentSelection(): void {
  if (!draft.value) return
  draft.value.mainEquipmentType = 'PROBER'
  const equipmentNos = [draft.value.mainEquipmentNo, ...draft.value.testerNos].filter(Boolean)
  draft.value.plannedAccessories = equipmentNos.flatMap((equipmentNo) => (accessoryRules[equipmentNo] || []).map((item) => ({ ...item })))
  const selected = new Set(equipmentNos)
  const existing = draft.value.currentAccessories.filter((item) => selected.has(item.equipmentNo))
  const built = equipmentNos.flatMap((equipmentNo) => {
    const type = equipmentNo.startsWith('DEV-TESTER') ? 'TESTER' : 'PROBER'
    const current = existing.filter((item) => item.equipmentNo === equipmentNo)
    return current.length ? current : makeEquipmentAccessoryInventory(equipmentNo, type, equipmentNo.slice(-2))
  })
  draft.value.currentAccessories = built
}
function toggleTester(equipmentNo: string): void {
  if (!draft.value || mode.value === 'view' || mode.value === 'close') return
  const picked = new Set(draft.value.testerNos)
  if (picked.has(equipmentNo)) picked.delete(equipmentNo)
  else picked.add(equipmentNo)
  draft.value.testerNos = Array.from(picked)
  updateEquipmentSelection()
}
function saveSetup(): void {
  if (!draft.value || (mode.value !== 'add' && mode.value !== 'edit')) return
  const value = draft.value
  if (!value.runCardNo || !value.opNo) { errorMessage.value = 'RunCard 與 OP 為必填。'; return }
  if (!value.mainEquipmentNo) { errorMessage.value = '請選擇一部 PROBER 主設備。'; return }
  if (!value.testerNos.length) { errorMessage.value = '至少需選擇一部 TESTER。'; return }
  if (mode.value === 'add' && rows.value.some((row) => row.status === 0 && row.mainEquipmentNo === value.mainEquipmentNo)) {
    errorMessage.value = '該主設備已有新建中的設置單。'
    return
  }
  if (value.testerNos.some((no) => testerOptions.find((item) => item.no === no)?.state === 'Maintenance')) {
    errorMessage.value = '維護中的 TESTER 不可加入設置單。'
    return
  }
  if (value.plannedAccessories.some((item) => !Number.isFinite(item.qty) || item.qty < 0)) {
    errorMessage.value = '配件需求量需為大於或等於 0 的數字。'
    return
  }
  const now = new Date()
  const localDate = new Date(now.getTime() - now.getTimezoneOffset() * 60000).toISOString().slice(0, 16).replace('T', ' ')
  let setupNo = value.setupNo
  if (mode.value === 'add') {
    const yy = String(now.getFullYear()).slice(-2)
    const monthHex = now.getMonth().toString(16).toUpperCase()
    const day = String(now.getDate()).padStart(2, '0')
    const prefix = `SFN${yy}${monthHex}${day}`
    const next = Math.max(0, ...rows.value.filter((row) => row.setupNo.startsWith(prefix)).map((row) => Number(row.setupNo.slice(prefix.length)) || 0)) + 1
    setupNo = `${prefix}${String(next).padStart(3, '0')}`
  }
  const requestedAccessories = value.plannedAccessories.filter((item) => item.qty > 0).map((item) => ({ ...item }))
  const missingAccessory = requestedAccessories.some((rule) => {
    const installed = value.currentAccessories.filter((item) => item.equipmentNo === rule.equipmentNo && item.accessoryType === rule.accessoryType)
    return installed.length < rule.qty || (rule.accessoryNo !== '*' && installed.some((item) => item.accessoryNo !== rule.accessoryNo))
  })
  const status: SetupStatus = missingAccessory ? 0 : 1
  const row: SetupRow = {
    ...value, setupNo, status,
    createDate: mode.value === 'edit' ? (rows.value.find((item) => item.setupNo === setupNo)?.createDate || localDate) : localDate,
    closeDate: '', closeUser: '',
    plannedAccessories: requestedAccessories,
    currentAccessories: value.currentAccessories.map((item) => ({ ...item })),
  }
  if (mode.value === 'edit') {
    const index = rows.value.findIndex((item) => item.setupNo === setupNo)
    if (index >= 0) rows.value.splice(index, 1, row)
  } else rows.value.unshift(row)
  searched.value = true
  selectedSetupNo.value = setupNo
  persist()
  noticeMessage.value = `${setupNo} 已${mode.value === 'add' ? '建立' : '更新'} DEV 設置單；配件申請／發料仍由獨立 EMS 流程負責。`
  mode.value = null
}
function completeClose(): void {
  if (!draft.value || mode.value !== 'close') return
  const value = draft.value
  if (runningEquipment.value.length) {
    errorMessage.value = `設備正在生產批使用中，不能結單：${runningEquipment.value.join('、')}`
    return
  }
  const duplicateSites = value.currentAccessories.filter((item) => item.selected && item.siteNo).reduce((set, item) => {
    const key = `${item.equipmentNo}:${item.siteNo}`
    if (set.has(key)) set.add(`DUP:${key}`)
    else set.add(key)
    return set
  }, new Set<string>())
  if (Array.from(duplicateSites).some((site) => site.startsWith('DUP:'))) { errorMessage.value = '同一設備的 Site Number 不可重複。'; return }
  const missingSocketSite = value.currentAccessories.find((item) => item.selected && item.socket && !item.siteNo)
  if (missingSocketSite) { errorMessage.value = `${missingSocketSite.accessoryType} 配件必須指定 Site Number。`; return }
  const selectionMismatch = value.plannedAccessories.find((rule) => {
    const selectedCount = value.currentAccessories.filter((item) => item.equipmentNo === rule.equipmentNo && item.accessoryType === rule.accessoryType && item.selected).length
    return selectedCount !== rule.qty
  })
  if (selectionMismatch) { errorMessage.value = `${selectionMismatch.equipmentNo} 的 ${selectionMismatch.accessoryType} 已勾選數量需為 ${selectionMismatch.qty}。`; return }
  const index = rows.value.findIndex((row) => row.setupNo === value.setupNo)
  if (index < 0) { errorMessage.value = '找不到這張 DEV 設置單。'; return }
  const selected = new Set([value.mainEquipmentNo, ...value.testerNos])
  rows.value.forEach((row, rowIndex) => {
    if (rowIndex !== index && row.status === 2 && selected.has(row.mainEquipmentNo)) {
      rows.value[rowIndex] = { ...row, status: 99 }
    }
  })
  const now = new Date()
  const closeDate = new Date(now.getTime() - now.getTimezoneOffset() * 60000).toISOString().slice(0, 16).replace('T', ' ')
  rows.value.splice(index, 1, { ...rows.value[index]!, status: 2, closeDate, closeUser: currentUser.value, currentAccessories: value.currentAccessories.map((item) => ({ ...item })) })
  persist()
  selectedSetupNo.value = value.setupNo
  noticeMessage.value = `${value.setupNo} 已模擬結單並生效；實際設備關聯與 EMS 配件狀態寫入未執行。`
  mode.value = null
}
function clearNotice(): void { noticeMessage.value = '' }
</script>

<template>
  <main class="wpp03-page" aria-label="設備設置單維護_晶片測試 DEV">
    <header class="wpp03-header">
      <div><p class="wpp03-eyebrow">WIP · CHIP TEST · WPP03 · DEV</p><h1>設備設置單維護_晶片測試</h1><p class="wpp03-subtitle">PROBER 主設備、TESTER 附屬設備與配件狀態的本機假資料流程</p></div>
      <div class="wpp03-header-actions"><span class="wpp03-dev-badge">DEV FIXTURES</span><button type="button" class="wpp03-close" @click="emit('close')">關閉</button></div>
    </header>

    <section class="wpp03-filter panel">
      <div class="wpp03-panel-title"><div><h2>查詢條件</h2><span>條件逐項啟用；日期與狀態只在勾選後套用</span></div><div class="wpp03-actions"><button type="button" class="button secondary" @click="resetFilters">清除</button><button type="button" class="button primary" @click="query">查詢</button></div></div>
      <div class="wpp03-filter-grid">
        <label>設置單號<input v-model.trim="filters.setupNo" placeholder="SFN…"></label>
        <label>主設備編號<input v-model.trim="filters.mainEquipmentNo" placeholder="PROBER no."></label>
        <label>RunCard<input v-model.trim="filters.runCardNo" placeholder="RunCard no."></label>
        <label>OP<input v-model.trim="filters.opNo" placeholder="OP no."></label>
      </div>
      <div class="wpp03-filter-ranges">
        <fieldset><legend><label class="checkline"><input v-model="useCreateRange" type="checkbox"> 建立日期</label></legend><input v-model="filters.createFrom" type="datetime-local" :disabled="!useCreateRange"><span>至</span><input v-model="filters.createTo" type="datetime-local" :disabled="!useCreateRange"></fieldset>
        <fieldset><legend><label class="checkline"><input v-model="useCloseRange" type="checkbox"> 結單日期</label></legend><input v-model="filters.closeFrom" type="datetime-local" :disabled="!useCloseRange"><span>至</span><input v-model="filters.closeTo" type="datetime-local" :disabled="!useCloseRange"></fieldset>
        <fieldset class="status-filter"><legend><label class="checkline"><input v-model="useStatus" type="checkbox"> 狀態</label></legend><label v-for="status in statusOptions" :key="status" class="checkline"><input v-model="statusFilter" type="checkbox" :value="status"> {{ statusMeta[status].label }}</label></fieldset>
      </div>
      <p v-if="errorMessage && !mode" class="wpp03-error" role="alert">{{ errorMessage }}</p>
    </section>

    <section class="wpp03-results panel">
      <div class="wpp03-panel-title"><div><h2>設備設置單</h2><span>{{ searched ? `共 ${filteredRows.length} 筆` : '請設定條件後查詢' }}</span></div><div class="wpp03-actions"><button type="button" class="button primary" @click="open('add')">新增</button><button type="button" class="button secondary" :disabled="selectedRow?.status !== 0" @click="selectedRow && open('edit', selectedRow)">編輯</button><button type="button" class="button secondary" :disabled="!selectedRow" @click="selectedRow && open('view', selectedRow)">明細</button><button type="button" class="button secondary" :disabled="selectedRow?.status !== 1" @click="selectedRow && open('close', selectedRow)">結單</button></div></div>
      <div class="wpp03-grid-wrap">
        <table class="wpp03-grid">
          <thead><tr><th>設置單號</th><th>主設備</th><th>RunCard</th><th>OP</th><th>狀態</th><th>建立者</th><th>建立時間</th><th>結單時間</th><th>結單者</th></tr></thead>
          <tbody>
            <tr v-for="row in filteredRows" :key="row.setupNo" :class="{ selected: row.setupNo === selectedSetupNo }" @click="selectedSetupNo = row.setupNo" @dblclick="open('view', row)">
              <td class="mono">{{ row.setupNo }}</td><td class="mono">{{ row.mainEquipmentNo }}</td><td class="mono">{{ row.runCardNo }}</td><td class="mono">{{ row.opNo }}</td><td><span class="status-pill" :class="statusMeta[row.status].tone">{{ statusMeta[row.status].label }}</span></td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.closeDate || '—' }}</td><td>{{ row.closeUser || '—' }}</td>
            </tr>
            <tr v-if="searched && !filteredRows.length"><td colspan="9" class="empty-cell">沒有符合條件的設備設置單。</td></tr>
            <tr v-if="!searched"><td colspan="9" class="empty-cell">WPP03 主畫面先選條件並按「查詢」。</td></tr>
          </tbody>
        </table>
      </div>
      <p v-if="noticeMessage" class="wpp03-notice" role="status"><span>{{ noticeMessage }}</span><button type="button" aria-label="關閉提示" @click="clearNotice">×</button></p>
    </section>

    <div v-if="mode && draft" class="wpp03-backdrop" @click.self="mode = null">
      <section class="wpp03-dialog" role="dialog" aria-modal="true" :aria-label="modeLabel">
        <header class="wpp03-dialog-header"><div><p class="wpp03-eyebrow">{{ mode === 'close' ? 'CLOSE / BIND EQUIPMENT' : 'SETUP BASIS' }}</p><h2>{{ modeLabel }}</h2><p>{{ mode === 'add' ? '新增單號依 SFN + 年月日 + 三位流水號模擬。' : draft.setupNo }}</p></div><button type="button" class="wpp03-icon-button" aria-label="關閉" @click="mode = null">×</button></header>
        <div class="wpp03-dialog-body">
          <div v-if="mode === 'close'" class="wpp03-close-callout"><strong>結單前檢查</strong><span>狀態需為「配件發料」；主設備與 TESTER 不可被生產批使用。成功後設為已生效，舊設備關聯單歸檔。</span></div>
          <div v-if="mode !== 'close'" class="wpp03-form-grid">
            <label class="wide">RunCard <select v-model="draft.runCardNo" :disabled="mode === 'view'"><option value="">選擇 RunCard</option><option v-for="lot in runCards" :key="lot.no" :value="lot.no">{{ lot.no }} · {{ lot.product }} / {{ lot.device }}</option></select></label>
            <label>OP <select v-model="draft.opNo" :disabled="mode === 'view'" @change="updateOp"><option value="">選擇 OP</option><option v-for="op in opOptions" :key="op.no" :value="op.no">{{ op.no }} · {{ op.name }}</option></select></label>
            <label>PID <input v-model.trim="draft.pid" :readonly="mode === 'view'"></label>
            <label>Test Program <input v-model.trim="draft.testProgram" :readonly="mode === 'view'"></label>
            <label>OP ECN <input v-model.trim="draft.ecnNo" :readonly="mode === 'view'"></label>
            <label>建立者 <input v-model.trim="draft.creator" :readonly="mode === 'view'"></label>
          </div>
          <div v-if="mode !== 'close'" class="wpp03-equipment-columns">
            <section class="equipment-card"><div class="subhead"><div><h3>PROBER 主設備</h3><p>來源分支目前僅顯示 PROBER，HANDLER 選項隱藏。</p></div><span>單選</span></div><label class="equipment-option" v-for="item in mainEquipmentOptions" :key="item.no"><input v-model="draft.mainEquipmentNo" type="radio" name="wpp03-main-equipment" :value="item.no" :disabled="mode === 'view'" @change="updateEquipmentSelection"><span><strong>{{ item.no }}</strong><small>{{ item.name }} · {{ item.state }}</small></span><em v-if="item.running" class="busy">使用中</em></label></section>
            <section class="equipment-card"><div class="subhead"><div><h3>TESTER 附屬設備</h3><p>至少選一部；選取配件維護中的設備會被阻擋。</p></div><span>可複選</span></div><label class="equipment-option" v-for="item in testerOptions" :key="item.no"><input type="checkbox" :checked="draft.testerNos.includes(item.no)" :disabled="mode === 'view'" @change="toggleTester(item.no)"><span><strong>{{ item.no }}</strong><small>{{ item.name }} · {{ item.state }}</small></span><em v-if="item.state === 'Maintenance'" class="busy">維護中</em></label></section>
          </div>
          <div v-if="mode === 'close' || mode === 'view'" class="wpp03-equipment-columns">
            <section class="equipment-card"><div class="subhead"><div><h3>設備與製程</h3><p>主設備、RunCard、OP 與設置程式</p></div><span>{{ statusMeta[selectedRow?.status ?? 0].label }}</span></div><dl class="detail-list"><dt>主設備</dt><dd>{{ draft.mainEquipmentNo }} · {{ draft.mainEquipmentType }}</dd><dt>RunCard</dt><dd>{{ draft.runCardNo }}</dd><dt>OP</dt><dd>{{ draft.opNo }}</dd><dt>PID</dt><dd>{{ draft.pid }}</dd><dt>Test Program</dt><dd>{{ draft.testProgram }}</dd><dt>TESTER</dt><dd>{{ draft.testerNos.join('、') || '—' }}</dd><dt>ECN</dt><dd>{{ draft.ecnNo || '—' }}</dd></dl></section>
            <section class="equipment-card"><div class="subhead"><div><h3>{{ mode === 'close' ? '目前配件狀態' : '配件設置需求' }}</h3><p>{{ mode === 'close' ? '結單時需核對配件數量與 Site Number' : '依 DEV release fixture 模擬' }}</p></div><span>{{ draft.currentAccessories.length }} 件</span></div>
              <div class="wpp03-accessory-list">
                <div v-for="(item, index) in draft.currentAccessories" :key="`${item.equipmentNo}:${item.accessoryNo}`" class="accessory-row">
                  <label class="accessory-select"><input v-model="item.selected" type="checkbox" :disabled="mode === 'view'"><span><strong>{{ item.accessoryType }}</strong><small>{{ item.equipmentNo }} · {{ item.accessoryNo }} · {{ item.version }}</small></span></label>
                  <label class="site-field">Site<input v-model.trim="item.siteNo" :disabled="mode !== 'close' || !item.selected" placeholder="—"></label>
                  <label class="site-field">Site State<select v-model="item.siteState" :disabled="mode !== 'close' || !item.selected"><option value="">—</option><option value="ON">ON</option><option value="OFF">OFF</option></select></label>
                </div>
                <p v-if="!draft.currentAccessories.length" class="empty-accessory">目前沒有配件狀態資料。</p>
              </div>
            </section>
          </div>
          <section v-if="mode !== 'close'" class="equipment-card accessory-plan-card"><div class="subhead"><div><h3>設備配件需求</h3><p>數量與允用配件由 product/accessory release 設定；這裡只展示 DEV 範本。</p></div><span>{{ draft.plannedAccessories.length }} 類</span></div><div class="wpp03-plan-grid"><div v-for="item in draft.plannedAccessories" :key="`${item.equipmentNo}:${item.accessoryType}`"><strong>{{ item.accessoryType }}</strong><span>{{ item.equipmentNo }}</span><label class="qty-field">需求量<input v-model.number="item.qty" type="number" min="0" step="0.000001" :disabled="mode === 'view'"></label><small>已裝 {{ installedCount(item) }} · 允用 {{ item.accessoryNo }}{{ item.accessoryGroup ? ` · ${item.accessoryGroup}` : '' }}</small></div><p v-if="!draft.plannedAccessories.length" class="empty-accessory">選擇主設備與 TESTER 後顯示配件需求。</p></div></section>
          <p v-if="mode === 'close' && runningEquipment.length" class="wpp03-error" role="alert">目前設備使用狀態阻擋結單：{{ runningEquipment.join('、') }}</p>
          <p v-if="errorMessage" class="wpp03-error" role="alert">{{ errorMessage }}</p>
          <div class="wpp03-modal-actions"><button type="button" class="button secondary" @click="mode = null">{{ mode === 'view' ? '關閉' : '取消' }}</button><button v-if="mode === 'add' || mode === 'edit'" type="button" class="button primary" @click="saveSetup">{{ mode === 'add' ? '建立 DEV 設置單' : '儲存 DEV 修改' }}</button><button v-else-if="mode === 'close'" type="button" class="button primary" :disabled="runningEquipment.length > 0" @click="completeClose">模擬結單</button></div>
        </div>
      </section>
    </div>
  </main>
</template>

<style scoped>
.wpp03-page { min-height: 0; height: 100%; overflow: auto; box-sizing: border-box; padding: 18px 20px 24px; color: #28271f; background: #f4f2eb; font: inherit; }
.wpp03-header, .wpp03-panel-title, .wpp03-header-actions, .wpp03-actions, .wpp03-dialog-header, .wpp03-modal-actions { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.wpp03-header { margin-bottom: 16px; }
.wpp03-header h1 { margin: 0; font-size: 24px; font-weight: 650; letter-spacing: .01em; }
.wpp03-eyebrow { margin: 0 0 5px; color: #8d5a31; font-size: 10px; font-weight: 750; letter-spacing: .13em; }
.wpp03-subtitle, .wpp03-panel-title span, .subhead p { color: #78766b; font-size: 12px; }
.wpp03-subtitle { margin: 5px 0 0; }
.wpp03-dev-badge { border: 1px solid #b9c7af; border-radius: 999px; padding: 4px 9px; background: #e8eee2; color: #486541; font-size: 10px; font-weight: 750; letter-spacing: .05em; }
.wpp03-close, .wpp03-icon-button { border: 1px solid #c7c3b8; background: #fbfaf6; color: #48463d; border-radius: 4px; min-height: 30px; padding: 4px 10px; font: inherit; cursor: pointer; }
.panel, .equipment-card { border: 1px solid #d9d6cb; border-radius: 6px; background: #fffefa; box-shadow: 0 1px 2px #26251f0b; }
.panel { padding: 14px; margin-bottom: 12px; }
.wpp03-panel-title { margin-bottom: 12px; }
.wpp03-panel-title h2 { margin: 0; font-size: 15px; font-weight: 700; }
.wpp03-panel-title span { display: inline-block; margin-top: 3px; }
.wpp03-actions { flex-wrap: wrap; }
.button { border: 1px solid #bbb7aa; border-radius: 4px; min-height: 30px; padding: 4px 11px; font: inherit; font-size: 12px; cursor: pointer; }
.button.primary { border-color: #38372f; background: #38372f; color: #fff; }
.button.secondary { background: #fffefa; color: #38372f; }
.button:disabled { opacity: .42; cursor: not-allowed; }
.wpp03-filter-grid { display: grid; grid-template-columns: repeat(4, minmax(150px, 1fr)); gap: 10px; }
.wpp03-page label { display: block; color: #59574d; font-size: 11px; }
.wpp03-page input:not([type=checkbox]):not([type=radio]), .wpp03-page select { width: 100%; min-width: 0; min-height: 31px; box-sizing: border-box; margin-top: 4px; border: 1px solid #d1cec3; border-radius: 4px; padding: 4px 7px; color: #302f28; background: #fff; font: inherit; font-size: 12px; }
.wpp03-page input:disabled, .wpp03-page select:disabled { color: #99968b; background: #f2f0e9; }
.wpp03-filter-ranges { display: grid; grid-template-columns: 1fr 1fr 1.2fr; gap: 10px; margin-top: 10px; }
.wpp03-filter-ranges fieldset { display: flex; align-items: center; gap: 7px; min-width: 0; border: 1px solid #e0ddd3; border-radius: 4px; padding: 8px; }
.wpp03-filter-ranges legend { padding: 0 4px; }
.wpp03-filter-ranges fieldset > input { min-width: 0; width: 46%; border: 1px solid #d1cec3; border-radius: 3px; padding: 4px; font: inherit; font-size: 11px; }
.checkline { display: inline-flex !important; align-items: center; gap: 4px; white-space: nowrap; }
.checkline input, .equipment-option input, .accessory-select input { margin: 0; accent-color: #74603f; }
.status-filter { flex-wrap: wrap; }
.wpp03-filter-ranges .status-filter { gap: 8px; }
.wpp03-grid-wrap { overflow: auto; border: 1px solid #e0ddd3; border-radius: 4px; }
.wpp03-grid { width: 100%; min-width: 930px; border-collapse: collapse; font-size: 11px; }
.wpp03-grid th { position: sticky; top: 0; padding: 8px 9px; border-bottom: 1px solid #d4d0c4; background: #f0eee6; color: #575449; text-align: left; white-space: nowrap; font-weight: 700; }
.wpp03-grid td { padding: 8px 9px; border-bottom: 1px solid #eeece5; white-space: nowrap; }
.wpp03-grid tbody tr { cursor: pointer; }
.wpp03-grid tbody tr:hover { background: #f8f6ef; }
.wpp03-grid tbody tr.selected { background: #ede8db; }
.wpp03-grid tbody tr:last-child td { border-bottom: 0; }
.mono { font-family: Consolas, 'Courier New', monospace; }
.status-pill { display: inline-block; border-radius: 999px; padding: 3px 8px; font-size: 10px; font-weight: 700; }
.status-pill.new { background: #ece8d9; color: #6e5a21; }
.status-pill.issued { background: #e4ecef; color: #315a67; }
.status-pill.effective { background: #e4efe2; color: #42613c; }
.status-pill.archived { background: #eee9e6; color: #777068; }
.empty-cell { height: 80px; color: #89867b; text-align: center; }
.wpp03-notice { display: flex; justify-content: space-between; align-items: center; gap: 12px; margin: 11px 0 0; padding: 8px 10px; border: 1px solid #d1ddc9; border-radius: 4px; background: #eff4ea; color: #48623f; font-size: 11px; }
.wpp03-notice button { border: 0; background: transparent; color: inherit; font-size: 17px; cursor: pointer; }
.wpp03-error { margin: 9px 0 0; padding: 8px 10px; border: 1px solid #e4b7ad; border-radius: 4px; background: #fff1ed; color: #963f30; font-size: 12px; }
.wpp03-backdrop { position: fixed; z-index: 1000; inset: 0; display: flex; align-items: center; justify-content: center; padding: 20px; background: #1f1e19a8; }
.wpp03-dialog { display: flex; flex-direction: column; width: min(1120px, 100%); max-height: min(92vh, 960px); overflow: hidden; border: 1px solid #d2cec2; border-radius: 7px; background: #f8f7f1; box-shadow: 0 18px 60px #0005; }
.wpp03-dialog-header { align-items: flex-start; padding: 15px 18px; border-bottom: 1px solid #dedbd1; background: #fffefa; }
.wpp03-dialog-header h2 { margin: 0; font-size: 18px; }
.wpp03-dialog-header p:last-child { margin: 4px 0 0; color: #777468; font: 11px Consolas, 'Courier New', monospace; }
.wpp03-icon-button { width: 30px; padding: 0; font-size: 20px; }
.wpp03-dialog-body { overflow: auto; padding: 14px 18px 17px; }
.wpp03-close-callout { display: grid; gap: 4px; margin-bottom: 11px; padding: 9px 11px; border: 1px solid #d7cda8; border-radius: 4px; background: #f7f1db; color: #675722; font-size: 11px; }
.wpp03-form-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 9px; margin-bottom: 12px; }
.wpp03-form-grid label.wide { grid-column: span 2; }
.wpp03-equipment-columns { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin-bottom: 10px; }
.equipment-card { min-width: 0; padding: 11px; }
.subhead { display: flex; justify-content: space-between; align-items: flex-start; gap: 10px; margin-bottom: 8px; }
.subhead h3 { margin: 0; font-size: 13px; }
.subhead p { margin: 4px 0 0; line-height: 1.45; }
.subhead > span { flex: 0 0 auto; border-radius: 999px; padding: 3px 7px; background: #f0eee7; color: #6d695e; font-size: 10px; }
.equipment-option { display: flex !important; align-items: center; gap: 8px; min-height: 45px; padding: 6px 2px; border-top: 1px solid #eeece5; cursor: pointer; }
.equipment-option > span, .accessory-select > span { display: grid; gap: 2px; min-width: 0; flex: 1; }
.equipment-option strong, .accessory-select strong { color: #323129; font: 11px Consolas, 'Courier New', monospace; }
.equipment-option small, .accessory-select small { overflow: hidden; color: #858176; font-size: 10px; text-overflow: ellipsis; white-space: nowrap; }
.equipment-option em { font-size: 10px; font-style: normal; }
.equipment-option em.busy { color: #a44432; }
.wpp03-plan-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 7px; }
.wpp03-plan-grid > div { display: grid; gap: 2px; padding: 7px 8px; border: 1px solid #e7e4da; border-radius: 4px; background: #fcfbf7; }
.wpp03-plan-grid strong { font-size: 11px; }
.wpp03-plan-grid span, .wpp03-plan-grid small { color: #777468; font-size: 10px; }
.qty-field { max-width: 100px; color: #777468 !important; font-size: 9px !important; }
.qty-field input { min-height: 25px !important; margin-top: 2px !important; padding: 3px 5px !important; font-size: 10px !important; }
.empty-accessory { margin: 8px 0; color: #89867b; font-size: 11px; }
.detail-list { display: grid; grid-template-columns: minmax(90px, .8fr) 2fr; gap: 7px 10px; margin: 0; font-size: 11px; }
.detail-list dt { color: #7a776c; }
.detail-list dd { min-width: 0; margin: 0; overflow-wrap: anywhere; color: #302f28; font-family: Consolas, 'Courier New', monospace; }
.wpp03-accessory-list { display: grid; gap: 4px; }
.accessory-row { display: grid; grid-template-columns: minmax(0, 1fr) 75px 95px; align-items: center; gap: 7px; padding: 6px 0; border-top: 1px solid #eeece5; }
.accessory-select { display: flex !important; align-items: center; gap: 7px; min-width: 0; cursor: pointer; }
.site-field { font-size: 9px !important; }
.site-field input, .site-field select { min-height: 26px !important; margin-top: 2px !important; padding: 3px 5px !important; font-size: 10px !important; }
.wpp03-modal-actions { justify-content: flex-end; margin-top: 14px; padding-top: 12px; border-top: 1px solid #dedbd1; }
@media (max-width: 960px) {
  .wpp03-filter-grid { grid-template-columns: repeat(2, minmax(140px, 1fr)); }
  .wpp03-filter-ranges { grid-template-columns: 1fr 1fr; }
  .status-filter { grid-column: 1 / -1; }
}
@media (max-width: 700px) {
  .wpp03-page { padding: 12px; }
  .wpp03-header, .wpp03-panel-title { align-items: flex-start; flex-direction: column; }
  .wpp03-header-actions { align-self: stretch; justify-content: flex-end; }
  .wpp03-filter-grid, .wpp03-filter-ranges, .wpp03-equipment-columns, .wpp03-form-grid { grid-template-columns: 1fr; }
  .wpp03-form-grid label.wide { grid-column: auto; }
  .wpp03-filter-ranges fieldset { flex-wrap: wrap; }
  .wpp03-filter-ranges fieldset > input { flex: 1 1 40%; width: auto; }
  .wpp03-actions { width: 100%; justify-content: flex-start; }
  .wpp03-backdrop { padding: 8px; }
  .wpp03-dialog { max-height: 96vh; }
  .wpp03-plan-grid { grid-template-columns: 1fr 1fr; }
}
</style>
