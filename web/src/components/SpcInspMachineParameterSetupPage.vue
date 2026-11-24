<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

interface QcItem { no: string; name: string }
interface GroupItem extends QcItem { groupNo: string; groupName: string }
interface Machine {
  no: string; name: string; inspType: number; baudRate: string; parityBit: string; dataBit: string; stopBit: string
  endCode: string; format1: string; format2: string; chNo: string
}
interface ApiMachine extends Record<string, unknown> {
  inspMachineNo: string; inspMachineName?: string | null; inspType: number; baudRate?: number | null
  parityBit?: string | null; dataBit?: number | null; stopBit?: number | null; endCode?: string | null
  format1?: string | null; format2?: string | null; chNo?: string | null
}
interface ApiLookups {
  variableItems: Array<{ qcItemNo: string; qcItemName: string }>
  groupItems: Array<{ groupNo: string; groupName?: string | null; qcItemNo: string; qcItemName?: string | null }>
  inspectionMachines: ApiMachine[]
}
interface SetupRow {
  id: string; comPort: string; inspMachineNo: string; channel: string; groupNo: string
  qcItemNo: string; qcItemName: string; inspType: number; comSetup: string; endCode: string
  format1: string; format2: string; chNo: string; enable: boolean; unitConversion: string
}
interface SetupForm {
  comPort: string; inspMachineNo: string; qcItems: GroupItem[]; enable: boolean; channel: string
  baudRate: string; parityBit: string; dataBit: string; stopBit: string; endCode: string
  format1: string; format2: string; chNo: string
}
type LookupKind = 'qcitem' | 'group' | 'machine' | ''

const props = withDefaults(defineProps<{ developmentPreview?: boolean; apiBaseUrl?: string }>(), {
  developmentPreview: false, apiBaseUrl: '',
})
const emit = defineEmits<{ close: [] }>()
const storageKey = props.developmentPreview ? 'ymes.dev.spc.insp-machine-setup.v1' : 'ymes.spc.insp-machine-setup.v1'
const demoQcItems: QcItem[] = [
  { no: 'QC-V-001', name: '軸心外徑' }, { no: 'QC-V-002', name: '軸心圓度' },
  { no: 'QC-V-018', name: '壓合高度' }, { no: 'QC-V-025', name: '端面寬度' },
]
const demoGroupItems: GroupItem[] = [
  { groupNo: 'GRP-DIM', groupName: '尺寸檢驗', no: 'QC-V-001', name: '軸心外徑' },
  { groupNo: 'GRP-DIM', groupName: '尺寸檢驗', no: 'QC-V-002', name: '軸心圓度' },
  { groupNo: 'GRP-FIT', groupName: '組裝檢驗', no: 'QC-V-018', name: '壓合高度' },
  { groupNo: 'GRP-FIT', groupName: '組裝檢驗', no: 'QC-V-025', name: '端面寬度' },
]
const demoMachines: Machine[] = [
  { no: 'IM-DEV-01', name: 'Mitutoyo LSM-6200', inspType: 0, baudRate: '9600', parityBit: 'None', dataBit: '8', stopBit: '1', endCode: '13', format1: 'VALUE,1', format2: 'F2', chNo: '1' },
  { no: 'IM-DEV-02', name: 'Keyence IM-8000', inspType: 0, baudRate: '19200', parityBit: 'Even', dataBit: '8', stopBit: '1', endCode: '13', format1: 'D1,1,D2,1', format2: 'F3', chNo: '2' },
  { no: 'IM-DEV-03', name: '高度計 HG-300', inspType: 0, baudRate: '9600', parityBit: 'Odd', dataBit: '7', stopBit: '1', endCode: '10', format1: 'VALUE,1', format2: '', chNo: '1' },
]
const seedRows: SetupRow[] = [
  { id: 'seed-1', comPort: '1', inspMachineNo: 'IM-DEV-01', channel: '1', groupNo: 'GRP-DIM', qcItemNo: 'QC-V-001', qcItemName: '軸心外徑', inspType: 0, comSetup: '9600,N,8,1', endCode: '13', format1: 'VALUE,1', format2: 'F2', chNo: '1', enable: true, unitConversion: 'mm' },
  { id: 'seed-2', comPort: '2', inspMachineNo: 'IM-DEV-02', channel: '1', groupNo: 'GRP-DIM', qcItemNo: 'QC-V-001', qcItemName: '軸心外徑', inspType: 0, comSetup: '19200,E,8,1', endCode: '13', format1: 'D1,1,D2,1', format2: 'F3', chNo: '2', enable: false, unitConversion: 'mm' },
  { id: 'seed-3', comPort: '2', inspMachineNo: 'IM-DEV-02', channel: '2', groupNo: 'GRP-DIM', qcItemNo: 'QC-V-002', qcItemName: '軸心圓度', inspType: 0, comSetup: '19200,E,8,1', endCode: '13', format1: 'D1,1,D2,1', format2: 'F3', chNo: '2', enable: true, unitConversion: 'μm' },
  { id: 'seed-4', comPort: '3', inspMachineNo: 'IM-DEV-03', channel: '', groupNo: 'GRP-FIT', qcItemNo: 'QC-V-018', qcItemName: '壓合高度', inspType: 0, comSetup: '9600,O,7,1', endCode: '10', format1: 'VALUE,1', format2: '', chNo: '1', enable: true, unitConversion: 'mm' },
]

const qcItems = ref<QcItem[]>(props.developmentPreview ? demoQcItems.map((item) => ({ ...item })) : [])
const groupItems = ref<GroupItem[]>(props.developmentPreview ? demoGroupItems.map((item) => ({ ...item })) : [])
const machines = ref<Machine[]>(props.developmentPreview ? demoMachines.map((item) => ({ ...item })) : [])
const lookupLoading = ref(!props.developmentPreview)
const lookupReady = ref(props.developmentPreview)
const rows = ref<SetupRow[]>(loadLocalRows())
const selectedIds = ref<string[]>([])
const dirty = ref(false)
const editing = ref(false)
const mode = ref<'add' | 'edit' | 'copy'>('add')
const editId = ref('')
const form = ref<SetupForm>(emptyForm())
const notice = ref(props.developmentPreview
  ? 'DEV 假資料；設定只保存在此瀏覽器，XML 可匯入或匯出。'
  : '正在載入核准的 QC Item、群組及檢測儀器。')
const noticeKind = ref<'info' | 'success' | 'error'>('info')
const lookupKind = ref<LookupKind>('')
const lookupSearch = ref('')
const lookupSelection = ref<string[]>([])
const simulationRow = ref<SetupRow | null>(null)
const importInput = ref<HTMLInputElement | null>(null)

function emptyForm(): SetupForm {
  return { comPort: '', inspMachineNo: '', qcItems: [], enable: true, channel: '', baudRate: '', parityBit: 'None', dataBit: '8', stopBit: '1', endCode: '', format1: '', format2: '', chNo: '' }
}
async function apiGet<T>(path: string): Promise<T> {
  const base = props.apiBaseUrl.trim().replace(/\/+$/, '')
  if (!base) throw new Error('目前作業環境尚未設定 API 位址。')
  const response = await fetch(`${base}${path}`, { credentials: 'include', headers: { Accept: 'application/json' } })
  const body = await response.json().catch(() => null) as ({ detail?: string; title?: string } & T) | null
  if (!response.ok) throw new Error(body?.detail || body?.title || `HTTP ${response.status}`)
  if (!body) throw new Error('API 回應格式無效。')
  return body
}
function mapParity(value: string | null | undefined): string {
  const key = (value || 'N').trim().toUpperCase()
  const options: Record<string, string> = { N: 'None', NONE: 'None', O: 'Odd', OD: 'Odd', ODD: 'Odd', E: 'Even', EVEN: 'Even', M: 'Mark', MARK: 'Mark' }
  return options[key] || value || 'None'
}
async function loadLookups(): Promise<void> {
  if (props.developmentPreview) return
  lookupLoading.value = true
  lookupReady.value = false
  try {
    const data = await apiGet<ApiLookups>('/api/spc/insp-machine-setup/lookups')
    if (!Array.isArray(data.variableItems) || !Array.isArray(data.groupItems) || !Array.isArray(data.inspectionMachines))
      throw new Error('SC501 選項資料不完整。')
    qcItems.value = data.variableItems.map((item) => ({ no: item.qcItemNo, name: item.qcItemName }))
    groupItems.value = data.groupItems.map((item) => ({
      groupNo: item.groupNo, groupName: item.groupName || item.groupNo, no: item.qcItemNo, name: item.qcItemName || item.qcItemNo,
    }))
    machines.value = data.inspectionMachines.filter((item) => Number(item.inspType) === 0).map((item) => ({
      no: item.inspMachineNo, name: item.inspMachineName || item.inspMachineNo, inspType: 0,
      baudRate: String(item.baudRate ?? ''), parityBit: mapParity(item.parityBit), dataBit: String(item.dataBit ?? ''),
      stopBit: String(item.stopBit ?? ''), endCode: item.endCode || '', format1: item.format1 || '', format2: item.format2 || '', chNo: item.chNo || '',
    }))
    lookupReady.value = true
    notice.value = `已載入 ${qcItems.value.length} 項計量 QC Item、${groupItems.value.length} 筆群組明細與 ${machines.value.length} 台 RS232 儀器。設定保存在目前瀏覽器，可用 XML 匯入／匯出。`
    noticeKind.value = 'info'
  } catch (error) {
    const message = error instanceof Error ? error.message : '選項載入失敗。'
    notice.value = `正式選項載入失敗：${message}`
    noticeKind.value = 'error'
  } finally { lookupLoading.value = false }
}
function loadLocalRows(): SetupRow[] {
  try {
    const stored = localStorage.getItem(storageKey)
    if (stored) return (JSON.parse(stored) as SetupRow[]).map(normalizeRow)
  } catch { /* Invalid local settings start empty and can be restored from XML. */ }
  return props.developmentPreview ? seedRows.map((row) => ({ ...row })) : []
}
function normalizeRow(value: Partial<SetupRow>): SetupRow {
  return {
    id: value.id || makeId(), comPort: String(value.comPort ?? ''), inspMachineNo: String(value.inspMachineNo ?? ''),
    channel: String(value.channel ?? ''), groupNo: String(value.groupNo ?? ''), qcItemNo: String(value.qcItemNo ?? ''),
    qcItemName: String(value.qcItemName ?? ''), inspType: Number(value.inspType ?? 0), comSetup: String(value.comSetup ?? ''),
    endCode: String(value.endCode ?? ''), format1: String(value.format1 ?? ''), format2: String(value.format2 ?? ''),
    chNo: String(value.chNo ?? ''), enable: value.enable === true || String(value.enable).toLowerCase() === 'true',
    unitConversion: String(value.unitConversion ?? ''),
  }
}
function makeId(): string { return `${Date.now()}-${Math.random().toString(36).slice(2, 8)}` }
function markChanged(message = '有尚未 Confirm 的變更。'): void { dirty.value = true; notice.value = message; noticeKind.value = 'info' }

const sortedRows = computed(() => [...rows.value].sort((a, b) => a.comPort.localeCompare(b.comPort, undefined, { numeric: true }) || a.inspMachineNo.localeCompare(b.inspMachineNo) || a.channel.localeCompare(b.channel, undefined, { numeric: true }) || a.qcItemNo.localeCompare(b.qcItemNo)))
const groupedRows = computed(() => {
  const groups: Array<{ label: string; rows: SetupRow[] }> = []
  for (const row of sortedRows.value) {
    const label = `COM${row.comPort}　/　${row.inspMachineNo}　/　Channel ${row.channel || '（空白）'}`
    let group = groups[groups.length - 1]
    if (!group || group.label !== label) { group = { label, rows: [] }; groups.push(group) }
    group.rows.push(row)
  }
  return groups
})
const selectedRows = computed(() => rows.value.filter((row) => selectedIds.value.includes(row.id)))
const channelOptions = computed(() => {
  const parts = form.value.format1.split(',')
  const count = parts.length > 2 ? Math.floor(parts.length / 2) : 6
  return Array.from({ length: count }, (_, index) => String(index + 1))
})
const channelRequired = computed(() => form.value.format1.split(',').length > 2)
const lookupRows = computed(() => {
  const term = lookupSearch.value.trim().toLowerCase()
  const source = lookupKind.value === 'machine' ? machines.value.map((item) => ({ key: item.no, no: item.no, name: item.name, group: '' }))
    : lookupKind.value === 'group' ? groupItems.value.map((item) => ({ key: `${item.groupNo}|${item.no}`, no: item.no, name: item.name, group: `${item.groupNo} — ${item.groupName}` }))
      : qcItems.value.map((item) => ({ key: `|${item.no}`, no: item.no, name: item.name, group: '未指定群組' }))
  return source.filter((item) => !term || `${item.no} ${item.name} ${item.group}`.toLowerCase().includes(term))
})

watch(() => form.value.format1, () => { if (!channelOptions.value.includes(form.value.channel)) form.value.channel = '' })

function startAdd(): void { mode.value = 'add'; editId.value = ''; form.value = emptyForm(); editing.value = true }
function startEdit(copy = false): void {
  const row = selectedRows.value[0]
  if (!row || selectedRows.value.length !== 1) return
  mode.value = copy ? 'copy' : 'edit'
  editId.value = copy ? '' : row.id
  const item = groupItems.value.find((candidate) => candidate.no === row.qcItemNo && candidate.groupNo === row.groupNo) || { no: row.qcItemNo, name: row.qcItemName, groupNo: row.groupNo, groupName: row.groupNo }
  const [baudRate = '', parity = 'N', dataBit = '', stopBit = ''] = row.comSetup.split(',')
  form.value = {
    comPort: row.comPort, inspMachineNo: row.inspMachineNo, qcItems: [{ ...item }], enable: row.enable, channel: row.channel,
    baudRate, parityBit: ({ N: 'None', O: 'Odd', E: 'Even', M: 'Mark' } as Record<string, string>)[parity] || 'None',
    dataBit, stopBit, endCode: row.endCode, format1: row.format1, format2: row.format2, chNo: row.chNo,
  }
  editing.value = true
}
function openLookup(kind: Exclude<LookupKind, ''>): void {
  lookupKind.value = kind; lookupSearch.value = ''; lookupSelection.value = []
}
function toggleLookup(key: string): void {
  lookupSelection.value = lookupSelection.value.includes(key) ? lookupSelection.value.filter((value) => value !== key) : [...lookupSelection.value, key]
}
function applyLookup(): void {
  if (lookupKind.value === 'machine') {
    const machine = machines.value.find((item) => item.no === lookupSelection.value[0])
    if (machine) {
      form.value.inspMachineNo = machine.no
      form.value.baudRate = machine.baudRate; form.value.parityBit = machine.parityBit
      form.value.dataBit = machine.dataBit; form.value.stopBit = machine.stopBit
      form.value.endCode = machine.endCode; form.value.format1 = machine.format1
      form.value.format2 = machine.format2; form.value.chNo = machine.chNo
    }
  } else if (lookupKind.value === 'group') {
    form.value.qcItems = lookupSelection.value.map((key) => groupItems.value.find((item) => `${item.groupNo}|${item.no}` === key)).filter((item): item is GroupItem => !!item)
  } else {
    form.value.qcItems = lookupSelection.value.map((key) => qcItems.value.find((item) => `|${item.no}` === key)).filter((item): item is QcItem => !!item).map((item) => ({ ...item, groupNo: '', groupName: '' }))
  }
  lookupKind.value = ''
}

function rowKey(row: Pick<SetupRow, 'comPort' | 'qcItemNo' | 'groupNo' | 'inspMachineNo' | 'channel'>): string {
  return [row.comPort, row.qcItemNo, row.groupNo, row.inspMachineNo, row.channel].join('\u0000')
}
function buildRow(item: GroupItem): SetupRow {
  const parityCode = ({ None: 'N', Odd: 'O', Even: 'E', Mark: 'M' } as Record<string, string>)[form.value.parityBit] || 'N'
  return {
    id: makeId(), comPort: form.value.comPort, inspMachineNo: form.value.inspMachineNo, channel: form.value.channel,
    groupNo: item.groupNo, qcItemNo: item.no, qcItemName: item.name, inspType: 0,
    comSetup: `${form.value.baudRate},${parityCode},${form.value.dataBit},${form.value.stopBit}`,
    endCode: form.value.endCode, format1: form.value.format1, format2: form.value.format2, chNo: form.value.chNo,
    enable: form.value.enable, unitConversion: '',
  }
}
function validateForm(): string {
  if (!form.value.qcItems.length) return '請選擇至少一個 QC Item。'
  if (!form.value.inspMachineNo) return '請選擇 InspMachineNo。'
  if (!form.value.comPort) return '請選擇 COM Port。'
  if (!form.value.channel && channelRequired.value) return 'Format1 含多組欄位，請選擇 Channel。'
  if (!form.value.baudRate || !form.value.dataBit || !form.value.stopBit || !form.value.format1) return 'RS232 設定未完整，請重新選取檢測儀器或補齊參數。'
  return ''
}
function complete(): void {
  const error = validateForm()
  if (error) { notice.value = error; noticeKind.value = 'error'; return }
  const next = form.value.qcItems.map(buildRow)
  const current = rows.value.filter((row) => row.id !== editId.value)
  for (const row of next) {
    if (current.some((candidate) => rowKey(candidate) === rowKey(row) && (mode.value !== 'edit' || candidate.enable === row.enable))) {
      notice.value = `${row.qcItemNo} / ${row.groupNo || '無群組'} / ${row.inspMachineNo} / COM${row.comPort} / Channel ${row.channel || '空白'} 已存在。`
      noticeKind.value = 'error'; return
    }
  }
  if (mode.value === 'edit' && editId.value) {
    const edited = { ...next[0], id: editId.value, unitConversion: rows.value.find((row) => row.id === editId.value)?.unitConversion || '' }
    rows.value = rows.value.map((row) => row.id === editId.value ? edited : row)
  } else rows.value = [...rows.value, ...next]
  if (form.value.enable) {
    const activeKeys = new Set(next.map((row) => `${row.qcItemNo}\u0000${row.groupNo}`))
    rows.value = rows.value.map((row) => activeKeys.has(`${row.qcItemNo}\u0000${row.groupNo}`) && !next.some((fresh) => fresh.id === row.id) ? { ...row, enable: false } : row)
  }
  selectedIds.value = next.map((row) => mode.value === 'edit' ? editId.value : row.id)
  markChanged(`${next.length} 筆本機設定已更新，按 Confirm 才會保存。`)
  editing.value = false
}
function removeSelected(): void {
  if (!selectedIds.value.length) return
  if (!window.confirm(`確定刪除選取的 ${selectedIds.value.length} 筆設定？`)) return
  rows.value = rows.value.filter((row) => !selectedIds.value.includes(row.id)); selectedIds.value = []
  markChanged('已刪除選取設定；按 Confirm 保存。')
}
function setEnabled(row: SetupRow, checked: boolean): void {
  rows.value = rows.value.map((candidate) => ({
    ...candidate,
    enable: candidate.id === row.id ? checked : checked && candidate.qcItemNo === row.qcItemNo && candidate.groupNo === row.groupNo ? false : candidate.enable,
  }))
  markChanged()
}
function toggleSelected(row: SetupRow, event: MouseEvent): void {
  if (event.ctrlKey || event.metaKey) selectedIds.value = selectedIds.value.includes(row.id) ? selectedIds.value.filter((id) => id !== row.id) : [...selectedIds.value, row.id]
  else selectedIds.value = [row.id]
}
function setUnitConversion(row: SetupRow, value: string): void {
  rows.value = rows.value.map((candidate) => candidate.id === row.id ? { ...candidate, unitConversion: value } : candidate)
  markChanged()
}
function loadFile(): void {
  if (dirty.value && !window.confirm('重新載入會捨棄尚未 Confirm 的變更，是否繼續？')) return
  rows.value = loadLocalRows(); selectedIds.value = []; dirty.value = false
  notice.value = props.developmentPreview ? '已載入預覽中的設定。' : '已載入目前瀏覽器保存的設定。'; noticeKind.value = 'success'
}
function toXml(): string {
  const esc = (value: unknown) => String(value ?? '').replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;').replaceAll("'", '&apos;')
  const fields: Array<[keyof SetupRow, string]> = [
    ['comPort', 'COMPort'], ['inspMachineNo', 'InspMachineNo'], ['channel', 'Channel'], ['groupNo', 'GroupNo'],
    ['qcItemNo', 'QCItemNo'], ['qcItemName', 'QCItemName'], ['inspType', 'InspType'], ['comSetup', 'COMSetup'],
    ['endCode', 'EndCode'], ['format1', 'Format1'], ['format2', 'Format2'], ['chNo', 'ChNo'], ['enable', 'Enable'], ['unitConversion', 'UnitConversion'],
  ]
  return `<?xml version="1.0" encoding="utf-8"?>\n<NewDataSet>\n${rows.value.map((row) => `  <Setup>${fields.map(([field, column]) => `\n    <${column}>${esc(row[field])}</${column}>`).join('')}\n  </Setup>`).join('\n')}\n</NewDataSet>\n`
}
function downloadXml(): void {
  const blob = new Blob([toXml()], { type: 'application/xml;charset=utf-8' })
  const url = URL.createObjectURL(blob); const anchor = document.createElement('a')
  anchor.href = url; anchor.download = 'InspMachineSetup.xml'; anchor.click(); URL.revokeObjectURL(url)
  notice.value = '已匯出 InspMachineSetup.xml。'; noticeKind.value = 'success'
}
function importFile(event: Event): void {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return
  const reader = new FileReader()
  reader.onload = () => {
    try {
      const xml = new DOMParser().parseFromString(String(reader.result || ''), 'application/xml')
      if (xml.querySelector('parsererror')) throw new Error('XML 格式無法解析。')
      const records = Array.from(xml.getElementsByTagName('Setup'))
      if (!records.length) throw new Error('找不到 Setup 設定列。')
      const read = (element: Element, key: string) => Array.from(element.children).find((child) => child.tagName.toLowerCase() === key.toLowerCase())?.textContent || ''
      rows.value = records.map((element) => normalizeRow({
        comPort: read(element, 'COMPort'), inspMachineNo: read(element, 'InspMachineNo'), channel: read(element, 'Channel'), groupNo: read(element, 'GroupNo'),
        qcItemNo: read(element, 'QCItemNo'), qcItemName: read(element, 'QCItemName'), inspType: Number(read(element, 'InspType') || 0),
        comSetup: read(element, 'COMSetup'), endCode: read(element, 'EndCode'), format1: read(element, 'Format1'), format2: read(element, 'Format2'),
        chNo: read(element, 'ChNo'), enable: read(element, 'Enable') === 'true', unitConversion: read(element, 'UnitConversion'),
      }))
      selectedIds.value = []; markChanged(`已匯入 ${rows.value.length} 筆 XML 設定；按 Confirm 保存。`); noticeKind.value = 'success'
    } catch (error) { notice.value = error instanceof Error ? error.message : '匯入失敗。'; noticeKind.value = 'error' }
    input.value = ''
  }
  reader.readAsText(file)
}
function confirmSave(): void {
  if (!dirty.value) return
  if (localStorage.getItem(storageKey) && !window.confirm('目前已有瀏覽器設定，確定取代？')) return
  try {
    localStorage.setItem(storageKey, JSON.stringify(rows.value)); dirty.value = false
    notice.value = '已保存目前瀏覽器設定。'; noticeKind.value = 'success'; emit('close')
  } catch { notice.value = '瀏覽器無法保存設定。'; noticeKind.value = 'error' }
}
function closePage(): void {
  if (dirty.value && !window.confirm('確定不保存變更並離開？')) return
  emit('close')
}
function simulate(): void { simulationRow.value = selectedRows.value[0] || null }
onMounted(() => { void loadLookups() })
const simulatedData = computed(() => {
  const row = simulationRow.value
  if (!row) return null
  const machine = machines.value.find((candidate) => candidate.no === row.inspMachineNo)
  const raw = row.channel === '2' ? '08.642' : '12.345'
  return { row, raw: `${raw}${row.endCode === '13' ? '\\r' : '\\n'}`, value: `${raw}${row.unitConversion ? ` ${row.unitConversion}` : ''}`, machine, com: `COM${row.comPort}` }
})
</script>

<template>
  <section class="setup-page" aria-label="品管項目檢測儀器對應">
    <header class="page-head">
      <div><div class="eyebrow">SPC / INSPECTION MACHINE</div><h1>品管項目檢測儀器對應 <small>SC501</small></h1><p>設定 QC Item 與 RS232 儀器的本機對應資料</p></div>
      <div class="device-chip" :class="{ unavailable: !props.developmentPreview && !lookupReady }"><span class="device-dot"></span>{{ props.developmentPreview ? 'DEV 假資料' : lookupLoading ? '載入選項' : lookupReady ? '正式查詢' : '選項未載入' }}</div>
    </header>

    <div class="toolbar">
      <button type="button" @click="loadFile">Load</button><button type="button" @click="downloadXml">Export</button>
      <button type="button" @click="importInput?.click()">Import</button><input ref="importInput" class="hidden-input" type="file" accept=".xml,text/xml,application/xml" @change="importFile">
      <span class="toolbar-separator"></span><button type="button" :disabled="lookupLoading || (!props.developmentPreview && !lookupReady)" @click="startAdd">Add</button>
      <button type="button" :disabled="selectedRows.length !== 1" @click="startEdit(false)">Edit</button>
      <button type="button" :disabled="selectedRows.length !== 1" @click="startEdit(true)">Copy</button>
      <button type="button" :disabled="selectedRows.length === 0" @click="removeSelected">Delete</button>
      <button type="button" :disabled="selectedRows.length !== 1" @click="simulate">Simulation</button>
      <div class="toolbar-spacer"></div><button type="button" class="primary" :disabled="!dirty" @click="confirmSave">Confirm</button><button type="button" @click="closePage">Close</button>
    </div>

    <div class="notice" :class="noticeKind" role="status"><span>{{ notice }}</span><button v-if="!props.developmentPreview && !lookupReady && !lookupLoading" type="button" @click="loadLookups">重試</button><span class="record-count">{{ rows.length }} 筆設定</span></div>

    <div class="grid-card">
      <div class="grid-title"><div><b>Inspection Machine Setup</b><span>依 COMPort → InspMachineNo → Channel 分組；Enable 與 UnitConversion 可直接修改</span></div><span class="selected-count">已選 {{ selectedIds.length }} 筆（Ctrl / ⌘ 多選）</span></div>
      <div class="grid-scroll">
        <table>
          <thead><tr><th>COMPort</th><th>InspMachineNo</th><th>Channel</th><th>GroupNo</th><th>QCItemNo</th><th>QCItemName</th><th>InspType</th><th>COMSetup</th><th>EndCode</th><th>Format1</th><th>Format2</th><th>ChNo</th><th>Enable</th><th>UnitConversion</th></tr></thead>
          <tbody v-if="rows.length">
            <template v-for="group in groupedRows" :key="group.label">
              <tr class="group-row"><td colspan="14">▾　{{ group.label }}　<span>{{ group.rows.length }} 筆</span></td></tr>
              <tr v-for="row in group.rows" :key="row.id" class="data-row" :class="{ selected: selectedIds.includes(row.id), disabled: !row.enable }" @click="toggleSelected(row, $event)">
                <td>COM{{ row.comPort }}</td><td class="machine-cell">{{ row.inspMachineNo }}</td><td>{{ row.channel || '—' }}</td><td>{{ row.groupNo || '—' }}</td>
                <td class="code-cell">{{ row.qcItemNo }}</td><td>{{ row.qcItemName }}</td><td><span class="type-pill">RS232</span></td><td>{{ row.comSetup }}</td><td>{{ row.endCode }}</td>
                <td>{{ row.format1 }}</td><td>{{ row.format2 || '—' }}</td><td>{{ row.chNo }}</td>
                <td @click.stop><input class="enable-check" type="checkbox" :checked="row.enable" :aria-label="`Enable ${row.qcItemNo}`" @change="setEnabled(row, ($event.target as HTMLInputElement).checked)"></td>
                <td @click.stop><input class="inline-input" :value="row.unitConversion" aria-label="UnitConversion" @change="setUnitConversion(row, ($event.target as HTMLInputElement).value)"></td>
              </tr>
            </template>
          </tbody>
          <tbody v-else><tr><td class="empty" colspan="14">尚無設定。按 Add 新增品管項目與檢測儀器對應。</td></tr></tbody>
        </table>
      </div>
    </div>

    <section v-if="editing" class="editor-card">
      <div class="editor-head"><div><b>{{ mode === 'add' ? '新增對應' : mode === 'copy' ? '複製對應' : '編輯對應' }}</b><span>Complete 先更新暫存 Grid，Confirm 才保存本機設定</span></div><button type="button" class="plain-close" @click="editing = false">×</button></div>
      <div class="editor-grid">
        <label>COM Port <select v-model="form.comPort"><option value="">請選擇</option><option v-for="n in 20" :key="n" :value="String(n)">COM{{ n }}</option></select></label>
        <label>InspMachineNo <div class="picker"><input v-model="form.inspMachineNo" :readonly="mode !== 'copy'" @input="form.inspMachineNo = form.inspMachineNo.toUpperCase()"><button type="button" :disabled="mode !== 'add' || (!props.developmentPreview && !lookupReady)" @click="openLookup('machine')">...</button></div></label>
        <label>Channel <select v-model="form.channel"><option value="">{{ channelRequired ? '請選擇' : '空白' }}</option><option v-for="channel in channelOptions" :key="channel" :value="channel">{{ channel }}</option></select><small v-if="channelRequired">多組 Format1 必須指定 Channel</small></label>
        <label class="qc-field">QC Item <div class="picker"><input :value="form.qcItems.map((item) => item.no).join(' | ')" readonly><button type="button" :disabled="mode === 'edit' || (!props.developmentPreview && !lookupReady)" @click="openLookup('qcitem')">...</button><button type="button" class="group-pick" :disabled="mode === 'edit' || (!props.developmentPreview && !lookupReady)" @click="openLookup('group')">Group</button></div><small>{{ form.qcItems.map((item) => item.groupNo || '無群組').join('、') || '可選單一或多個 QC Item，也可按 Group 選取群組項目' }}</small></label>
        <label class="check-field"><input v-model="form.enable" type="checkbox"> Enable</label>
        <label>InspType <input value="0.RS232" readonly></label>
      </div>
      <div class="serial-panel"><div class="serial-title"><b>RS232 設定</b><span>依所選檢測儀器帶入，可再調整</span></div><div class="serial-grid">
        <label>BaudRate <input v-model="form.baudRate" inputmode="numeric"></label>
        <label>ParityBit <select v-model="form.parityBit"><option>None</option><option>Odd</option><option>Even</option><option>Mark</option></select></label>
        <label>DataBit <select v-model="form.dataBit"><option value="">請選擇</option><option>7</option><option>8</option></select></label>
        <label>StopBit <select v-model="form.stopBit"><option value="">請選擇</option><option>1</option><option>2</option></select></label>
        <label>EndCode <input v-model="form.endCode"></label><label>Format1 <input v-model="form.format1"></label>
        <label>Format2 <input v-model="form.format2"></label><label>ChNo <input v-model="form.chNo"></label>
      </div></div>
      <div class="editor-actions"><span>啟用同一 QC Item / Group 時，其他對應會自動停用。</span><button type="button" @click="editing = false">Cancel</button><button type="button" class="primary" @click="complete">Complete</button></div>
    </section>

    <div v-if="lookupKind" class="modal-backdrop" @click.self="lookupKind = ''">
      <section class="lookup-modal" role="dialog" aria-modal="true" :aria-label="lookupKind === 'machine' ? '選擇檢測儀器' : '選擇 QC Item'">
        <header><div><b>{{ lookupKind === 'machine' ? '選擇檢測儀器' : lookupKind === 'group' ? '選擇品管群組項目' : '選擇計量 QC Item' }}</b><span>{{ props.developmentPreview ? '來源：DEV 假資料' : '來源：YMES 核准主檔唯讀查詢' }}</span></div><button type="button" class="plain-close" @click="lookupKind = ''">×</button></header>
        <input v-model="lookupSearch" class="lookup-search" placeholder="輸入編號、名稱或群組搜尋">
        <div class="lookup-list"><label v-for="item in lookupRows" :key="item.key" class="lookup-row" :class="{ checked: lookupSelection.includes(item.key) }">
          <input v-if="lookupKind === 'machine'" type="radio" name="machine-lookup" :checked="lookupSelection[0] === item.key" @change="lookupSelection = [item.key]">
          <input v-else type="checkbox" :checked="lookupSelection.includes(item.key)" @change="toggleLookup(item.key)">
          <span><b>{{ item.no }}</b><small>{{ item.name }}<template v-if="item.group">　·　{{ item.group }}</template></small></span>
        </label><div v-if="!lookupRows.length" class="empty">沒有符合項目</div></div>
        <footer><span>已選 {{ lookupSelection.length }} 項</span><button type="button" @click="lookupKind = ''">Cancel</button><button type="button" class="primary" :disabled="!lookupSelection.length" @click="applyLookup">Select</button></footer>
      </section>
    </div>

    <div v-if="simulationRow" class="modal-backdrop" @click.self="simulationRow = null">
      <section class="simulation-modal" role="dialog" aria-modal="true" aria-label="儀器通訊模擬">
        <header><div><b>Simulation — RS232</b><span>此模擬不連接真實序列埠</span></div><button type="button" class="plain-close" @click="simulationRow = null">×</button></header>
        <div v-if="simulatedData" class="sim-summary"><span><small>QC Item</small><b>{{ simulatedData.row.qcItemNo }} · {{ simulatedData.row.qcItemName }}</b></span><span><small>儀器 / Port / Channel</small><b>{{ simulatedData.row.inspMachineNo }} / {{ simulatedData.com }} / {{ simulatedData.row.channel || '—' }}</b></span><span><small>COMSetup</small><b>{{ simulatedData.row.comSetup }}</b></span></div>
        <div v-if="simulatedData" class="sim-data"><label>接收資料 <code>{{ simulatedData.raw }}</code></label><label>解析結果 <strong>{{ simulatedData.value }}</strong></label><label>Format1 <code>{{ simulatedData.row.format1 }}</code></label></div>
        <footer><span>設備連線、COM port 讀寫與真實量測值皆未執行。</span><button type="button" class="primary" @click="simulationRow = null">Close</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.setup-page { height: 100%; min-height: 0; overflow: auto; box-sizing: border-box; padding: 18px 22px 24px; background: #f4f6f8; color: #263447; font: 13px/1.45 "Segoe UI", "Microsoft JhengHei", sans-serif; }
.page-head { display: flex; align-items: center; justify-content: space-between; gap: 16px; margin: 0 0 16px; }
.eyebrow { color: #6680a0; font-size: 10px; font-weight: 800; letter-spacing: .14em; }
h1 { margin: 2px 0 1px; color: #1f334a; font-size: 21px; letter-spacing: -.02em; }
h1 small { margin-left: 7px; padding: 3px 6px; border-radius: 4px; background: #e7edf5; color: #607893; font-size: 10px; vertical-align: middle; }
.page-head p { margin: 0; color: #78879a; font-size: 12px; }
.device-chip { display: inline-flex; align-items: center; gap: 8px; padding: 7px 11px; border: 1px solid #d8e4ed; border-radius: 20px; background: #edf6f4; color: #3c796a; font-size: 11px; font-weight: 700; white-space: nowrap; }
.device-dot { width: 7px; height: 7px; border-radius: 50%; background: #45a78e; box-shadow: 0 0 0 3px #d7eee8; }
.toolbar { display: flex; align-items: center; gap: 6px; padding: 8px; border: 1px solid #dce3eb; border-radius: 8px 8px 0 0; background: #fff; }
button { min-height: 30px; padding: 5px 11px; border: 1px solid #d2dbe5; border-radius: 5px; background: #fff; color: #334860; font: inherit; font-size: 12px; cursor: pointer; }
button:hover:not(:disabled) { border-color: #91a8c1; background: #f5f8fb; }
button:disabled { opacity: .46; cursor: not-allowed; }
button.primary { border-color: #245d91; background: #245d91; color: #fff; }
button.primary:hover:not(:disabled) { border-color: #1d4d79; background: #1d4d79; }
.toolbar-separator { width: 1px; height: 20px; margin: 0 4px; background: #dce3eb; }
.toolbar-spacer { flex: 1; }
.hidden-input { display: none; }
.notice { display: flex; align-items: center; justify-content: space-between; gap: 12px; min-height: 34px; padding: 5px 11px; border: 1px solid #dce3eb; border-top: 0; background: #f8fafc; color: #687c92; }
.notice.success { background: #f0f8f5; color: #387766; }.notice.error { background: #fff3f1; color: #a44b40; }
.record-count { color: #6c7e91; font-size: 11px; white-space: nowrap; }
.grid-card, .editor-card { overflow: hidden; margin-top: 14px; border: 1px solid #dce3eb; border-radius: 8px; background: white; box-shadow: 0 2px 7px #243d5710; }
.grid-title, .editor-head { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 12px 14px; border-bottom: 1px solid #e8edf2; }
.grid-title > div, .editor-head > div, .modal-backdrop header > div { display: flex; flex-direction: column; gap: 2px; }
.grid-title b, .editor-head b { color: #263d57; font-size: 13px; }.grid-title span, .editor-head span { color: #8090a2; font-size: 11px; }
.selected-count { white-space: nowrap; }
.grid-scroll { overflow: auto; max-height: min(48vh, 510px); }
table { width: max(100%, 1320px); border-collapse: separate; border-spacing: 0; font-size: 11px; }
th { position: sticky; z-index: 1; top: 0; padding: 8px 9px; border-bottom: 1px solid #dbe3ec; background: #f4f7fa; color: #667d95; font-size: 10px; font-weight: 700; text-align: left; white-space: nowrap; }
td { padding: 7px 9px; border-bottom: 1px solid #edf1f5; color: #465a70; white-space: nowrap; }
.group-row td { padding: 7px 10px; background: #eaf0f6; color: #4d6885; font-weight: 700; }.group-row span { color: #8595a7; font-weight: 500; }
.data-row { cursor: pointer; }.data-row:hover td { background: #f7fafe; }.data-row.selected td { background: #e4f0fb; }.data-row.disabled td { color: #8996a5; }
.machine-cell, .code-cell { color: #2b577e; font-weight: 650; }.type-pill { padding: 2px 6px; border: 1px solid #d7e6f2; border-radius: 10px; background: #f1f7fb; color: #4b7295; font-size: 10px; }
.enable-check { width: 15px; height: 15px; accent-color: #2875ae; }.inline-input { width: 78px; min-height: 25px; padding: 3px 5px; border: 1px solid transparent; border-radius: 4px; color: #42576e; font: inherit; }.inline-input:hover, .inline-input:focus { border-color: #c8d7e6; outline: 0; background: #fff; }
.empty { padding: 28px 12px; color: #8a98a8; text-align: center; }
.editor-card { border-color: #cbd9e6; }.editor-head { background: #f6f9fc; }.plain-close { width: 30px; min-height: 28px; padding: 0; border: 0; background: transparent; color: #71849a; font-size: 22px; }
.editor-grid { display: grid; grid-template-columns: repeat(3, minmax(180px, 1fr)); gap: 12px 16px; padding: 15px; }
label { display: flex; min-width: 0; flex-direction: column; gap: 5px; color: #61758b; font-size: 11px; font-weight: 650; }
input, select { box-sizing: border-box; width: 100%; min-height: 31px; padding: 5px 8px; border: 1px solid #d7e0e9; border-radius: 4px; background: #fff; color: #33485e; font: inherit; font-size: 12px; }
input:focus, select:focus { border-color: #78a3c8; outline: 2px solid #dcebf6; }
input[readonly] { background: #f6f8fa; color: #536d86; }
.picker { display: flex; gap: 5px; }.picker button { min-width: 35px; padding: 3px 8px; }.picker .group-pick { min-width: 50px; background: #f3f7fb; }
.editor-grid small { color: #93a0af; font-size: 10px; font-weight: 400; }.qc-field { grid-column: span 2; }.check-field { flex-direction: row; align-items: center; padding-top: 21px; }.check-field input { width: 15px; min-height: 15px; }
.serial-panel { margin: 0 15px; padding: 12px; border: 1px solid #e1e8ef; border-radius: 6px; background: #f8fafc; }
.serial-title { display: flex; align-items: center; gap: 10px; margin-bottom: 10px; }.serial-title b { color: #435e79; font-size: 12px; }.serial-title span { color: #93a0af; font-size: 10px; }
.serial-grid { display: grid; grid-template-columns: repeat(4, minmax(120px, 1fr)); gap: 10px 12px; }
.editor-actions, .modal-backdrop footer { display: flex; align-items: center; justify-content: flex-end; gap: 8px; padding: 12px 15px; }.editor-actions span, .modal-backdrop footer span { flex: 1; color: #8493a3; font-size: 10px; }
.modal-backdrop { position: fixed; z-index: 200; inset: 0; display: grid; place-items: center; padding: 18px; background: #15283b80; }
.lookup-modal, .simulation-modal { display: flex; width: min(560px, 96vw); max-height: min(690px, 92vh); flex-direction: column; overflow: hidden; border: 1px solid #d7e1eb; border-radius: 9px; background: #fff; box-shadow: 0 18px 55px #1e30444a; }
.modal-backdrop header { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 13px 15px; border-bottom: 1px solid #e4eaf0; }.modal-backdrop header b { color: #2e435b; font-size: 13px; }.modal-backdrop header span { color: #8795a5; font-size: 10px; }
.lookup-search { width: auto; margin: 12px 14px 8px; }.lookup-list { overflow: auto; padding: 0 14px 8px; }.lookup-row { display: flex; flex-direction: row; align-items: center; gap: 10px; padding: 9px 8px; border-bottom: 1px solid #eff2f5; cursor: pointer; }.lookup-row.checked { background: #eef6fc; }.lookup-row > input { width: 15px; min-height: 15px; }.lookup-row > span { display: flex; flex-direction: column; gap: 2px; }.lookup-row b { color: #3b6387; font-size: 11px; }.lookup-row small { color: #76879a; font-size: 11px; }
.sim-summary { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; padding: 14px; background: #f8fafc; }.sim-summary span { display: flex; flex-direction: column; gap: 3px; }.sim-summary small { color: #8493a3; font-size: 10px; }.sim-summary b { color: #435d76; font-size: 11px; }
.sim-data { display: grid; gap: 12px; padding: 18px 16px; }.sim-data label { flex-direction: row; align-items: center; justify-content: space-between; padding-bottom: 9px; border-bottom: 1px solid #edf1f5; }.sim-data code { color: #345f87; font-size: 12px; }.sim-data strong { color: #2f816c; font-size: 14px; }
@media (max-width: 900px) { .setup-page { padding: 13px; }.toolbar { flex-wrap: wrap; }.toolbar-spacer { flex-basis: 100%; }.editor-grid { grid-template-columns: 1fr 1fr; }.serial-grid { grid-template-columns: 1fr 1fr; } }
@media (max-width: 560px) { .page-head { align-items: flex-start; flex-direction: column; }.editor-grid, .serial-grid { grid-template-columns: 1fr; }.qc-field { grid-column: auto; }.sim-summary { grid-template-columns: 1fr; } }
.setup-page { background: #f7f5ee; color: #292820; font-family: "Segoe UI", "Microsoft JhengHei", sans-serif; }
.eyebrow { color: #80752a; }
h1 { color: #292820; }
h1 small { background: #f4f0d8; color: #8f811d; }
.page-head p { color: #777366; }
.device-chip { border-color: #e5dfc4; background: #f7f4e4; color: #82740e; }
.device-dot { background: #a38f19; box-shadow: 0 0 0 3px #ebe6c7; }
.device-chip.unavailable { border-color: #e7d6cf; background: #fbf0ed; color: #9d5548; }
.device-chip.unavailable .device-dot { background: #b75c4d; box-shadow: 0 0 0 3px #f2dfd9; }
.toolbar, .grid-card, .editor-card { border-color: #e2dfd4; border-radius: 5px; box-shadow: 0 1px 4px #332f2010; }
button { border-color: #d8d5c9; border-radius: 4px; color: #3c3a32; }
button:hover:not(:disabled) { border-color: #b6a536; background: #fbf8e9; }
button.primary { border-color: #39372f; background: #39372f; color: #fff; }
button.primary:hover:not(:disabled) { border-color: #27261f; background: #27261f; }
.notice { border-color: #e2dfd4; background: #f5f3ec; color: #716d60; }
.notice.success { background: #eef2e8; color: #526847; }.notice.error { background: #fbefec; color: #9b4d41; }
.record-count { color: #777366; }
.grid-title, .editor-head { border-color: #e8e5da; }
.grid-title b, .editor-head b { color: #39372f; }.grid-title span, .editor-head span { color: #777366; }
th { border-color: #e4e1d8; background: #f2f0e8; color: #716d60; }
td { border-color: #ece9df; color: #4b4940; }
.group-row td { background: #f2f0e8; color: #625d43; }.group-row span { color: #817c6e; }
.data-row:hover td { background: #fbf9ef; }.data-row.selected td { background: #f6f1d7; }.data-row.disabled td { color: #898678; }
.machine-cell, .code-cell { color: #74651a; }.type-pill { border-color: #e5dfc4; background: #f7f4e4; color: #82740e; }
.enable-check { accent-color: #9b8714; }.inline-input:hover, .inline-input:focus { border-color: #ddd4a5; }
.editor-card { border-color: #d8d0a9; }.editor-head { background: #f7f4e7; }
input, select { border-color: #d8d5c9; border-radius: 4px; color: #39372f; }
input:focus, select:focus { border-color: #b6a536; outline-color: #f0ebcb; }
input[readonly] { background: #f4f2eb; color: #686456; }
.serial-panel { border-color: #e2dfd4; border-radius: 4px; background: #f8f7f2; }
.serial-title b { color: #625d43; }.serial-title span, .editor-grid small { color: #858173; }
.modal-backdrop { background: #26241f70; }
.lookup-modal, .simulation-modal { border-color: #e2dfd4; border-radius: 5px; }
.modal-backdrop header { border-color: #e7e3d8; }.modal-backdrop header b { color: #39372f; }.modal-backdrop header span { color: #777366; }
.lookup-row.checked { background: #f6f1d7; }.lookup-row b { color: #74651a; }.lookup-row small { color: #716d60; }
</style>
