<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'

type FilterKey = 'swrType' | 'customerNo' | 'lotNo' | 'deviceNo' | 'opNo' | 'customerLotNo' | 'moNo'
type Swr = {
  swrNo: string; swrType: number; status: number; customerNo: string; opNo: string; deviceNo: string
  startTime: string; endTime: string; customerLotNo: string; moNo: string; swrDesc: string
  creator: string; createDate: string; attachmentName: string; lotNos: string[]
}
type Saved = { items: Swr[]; serialByDay: Record<string, number> }
type Mode = 'add' | 'edit' | 'copy'
type Action = 'approve' | 'void'

const props = defineProps<{ developmentPreview?: boolean; currentUserNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const unavailableMessage = "正式環境尚未開放此功能。"
const storageKey = 'ymes-dev-mock:wip-swr-basis:state:v1'
const statusLabels: Record<number, string> = { 0: 'UNFROZEN', 1: 'PENDING', 2: 'ACTIVE', 99: 'UNUSED' }
const typeLabels = ['作業站指示', '包裝作業說明']
const queryFields: { key: FilterKey; label: string; lookup: boolean }[] = [
  { key: 'swrType', label: 'SWRType', lookup: false }, { key: 'customerNo', label: 'CustomerNo', lookup: true },
  { key: 'lotNo', label: 'LotNo', lookup: false }, { key: 'deviceNo', label: 'DeviceNo', lookup: true },
  { key: 'opNo', label: 'OPNo', lookup: true }, { key: 'customerLotNo', label: 'CustomerLotNo', lookup: false },
  { key: 'moNo', label: 'MONo', lookup: true },
]
const columns: { label: string; key: keyof Swr | 'lotNo' | 'haveAttachFile' }[] = [
  { key: 'swrNo', label: 'SWRNO' }, { key: 'swrType', label: 'SWRTYPE' }, { key: 'customerNo', label: 'CUSTOMERNO' },
  { key: 'deviceNo', label: 'DEVICENO' }, { key: 'opNo', label: 'OPNO' }, { key: 'lotNo', label: 'LOTNO' },
  { key: 'moNo', label: 'MONO' }, { key: 'status', label: 'STATUS' }, { key: 'startTime', label: 'STARTTIME' },
  { key: 'endTime', label: 'ENDTIME' }, { key: 'customerLotNo', label: 'CUSTOMERLOTNO' },
  { key: 'creator', label: 'CREATOR' }, { key: 'createDate', label: 'CREATEDATE' }, { key: 'haveAttachFile', label: 'HaveAttachFile' },
]
const lookupData: Record<string, string[]> = dev.value ? ({
  customerNo: ['CUS-A', 'CUS-B'], deviceNo: ['DEVICE-A', 'DEVICE-B'], opNo: ['OP-CUT', 'OP-TEST'],
  moNo: ['MO-001', 'MO-002'], lotNo: ['DEV-LOT-001', 'DEV-LOT-002', 'DEV-LOT-005'],
}) : ({})
const lotCustomer: Record<string, string> = dev.value ? ({ 'DEV-LOT-001': 'CUS-A', 'DEV-LOT-002': 'CUS-B', 'DEV-LOT-005': 'CUS-A' }) : ({})
const flags = reactive<Record<FilterKey, boolean>>({ swrType: false, customerNo: false, lotNo: false, deviceNo: false, opNo: false, customerLotNo: false, moNo: false })
const filters = reactive<Record<FilterKey, string>>({ swrType: '', customerNo: '', lotNo: '', deviceNo: '', opNo: '', customerLotNo: '', moNo: '' })
const status = ref(0)
const saved = ref<Saved>({ items: [], serialByDay: {} })
const rows = ref<{ swr: Swr; lotNo: string }[]>([])
const activeNo = ref('')
const selectedNos = ref<string[]>([])
const queried = ref(false)
const modal = ref<Mode | null>(null)
const action = ref<Action | null>(null)
const abortReason = ref('')
const lookup = ref<{ key: string; target: 'query' | 'form' } | null>(null)
const error = ref('')
const message = ref(dev.value ? '查詢條件可單獨勾選；Grid 以 Ctrl+點擊多選核准／作廢。' : unavailableMessage)
const draft = reactive<Swr>({ swrNo: '', swrType: 0, status: 0, customerNo: '', opNo: '', deviceNo: '', startTime: '', endTime: '', customerLotNo: '', moNo: '', swrDesc: '', creator: '', createDate: '', attachmentName: '', lotNos: [] })
const draftLots = ref('')
const startChecked = ref(false)
const endChecked = ref(false)
const active = computed(() => dev.value ? (saved.value.items.find(item => item.swrNo === activeNo.value) || null) : (null))
const lookupOptions = computed(() => dev.value ? (lookup.value ? lookupData[lookup.value.key] || [] : []) : ([]))

function fixtures(): Swr[] {
  if (!dev.value) return []
 return [
  { swrNo: 'SWRDEV00001', swrType: 0, status: 0, customerNo: 'CUS-A', opNo: 'OP-CUT', deviceNo: 'DEVICE-A', startTime: '', endTime: '', customerLotNo: 'CL-001', moNo: 'MO-001', swrDesc: 'DEV 作業站特殊指示', creator: 'DEV', createDate: '2026-11-19 09:00:00', attachmentName: '', lotNos: ['DEV-LOT-001', 'DEV-LOT-005'] },
  { swrNo: 'SWRDEV00002', swrType: 1, status: 2, customerNo: 'CUS-B', opNo: 'OP-TEST', deviceNo: '', startTime: '2026-11-19T08:00', endTime: '2026-11-30T18:00', customerLotNo: 'CL-002', moNo: 'MO-002', swrDesc: 'DEV 包裝注意事項', creator: 'DEV', createDate: '2026-11-19 10:00:00', attachmentName: 'sample.pdf', lotNos: ['DEV-LOT-002'] },
  { swrNo: 'SWRDEV00003', swrType: 0, status: 1, customerNo: 'CUS-A', opNo: 'OP-CUT', deviceNo: '', startTime: '', endTime: '', customerLotNo: '', moNo: '', swrDesc: 'DEV 待處理資料', creator: 'DEV', createDate: '2026-11-19 11:00:00', attachmentName: '', lotNos: [] },
  { swrNo: 'SWRDEV00004', swrType: 0, status: 99, customerNo: 'CUS-A', opNo: 'OP-CUT', deviceNo: '', startTime: '', endTime: '', customerLotNo: '', moNo: '', swrDesc: 'DEV 已作廢資料', creator: 'DEV', createDate: '2026-11-19 12:00:00', attachmentName: '', lotNos: [] },
] }
function nowText(): string { const date = new Date(); return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')} ${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}:${String(date.getSeconds()).padStart(2, '0')}` }
function dayKey(): string { const date = new Date(); return `${String(date.getFullYear()).slice(2)}${String(date.getMonth() + 1).padStart(2, '0')}${String(date.getDate()).padStart(2, '0')}` }
function nextNo(): string {
  if (!dev.value) return ''
 const day = dayKey(); const serial = saved.value.serialByDay[day] || 1; return serial > 999 ? '' : `SWR${day}${String(serial).padStart(3, '0')}` }
function norm(value: string): string { return value.trim().toUpperCase() }
function load(): void {
  if (!dev.value) return
  try {
    const raw = window.localStorage.getItem(storageKey)
    const parsed = raw ? JSON.parse(raw) as Saved : null
    saved.value = parsed && Array.isArray(parsed.items) && parsed.serialByDay && typeof parsed.serialByDay === 'object' ? parsed : { items: fixtures(), serialByDay: {} }
  } catch { saved.value = { items: fixtures(), serialByDay: {} }; error.value = 'DEV SWR 資料無法讀取，已使用初始假資料。' }
}
function persist(): boolean {
  if (!dev.value) return false
 try { window.localStorage.setItem(storageKey, JSON.stringify(saved.value)); return true } catch { error.value = 'DEV SWR 資料無法儲存。'; return false } }
function query(): void {
  error.value = ''
  if (!dev.value) { error.value = unavailableMessage; return }
  load()
  for (const field of queryFields) if (field.key !== 'swrType') filters[field.key] = norm(filters[field.key])
  for (const field of queryFields) if (flags[field.key] && !filters[field.key]) { error.value = `${field.label} 已勾選，請輸入值。`; return }
  const result = saved.value.items.filter(item => item.status === status.value && queryFields.every(field => {
    if (!flags[field.key]) return true
    const value = filters[field.key]
    if (field.key === 'swrType') return item.swrType === Number(value)
    if (field.key === 'lotNo') return value.split(',').map(norm).some(no => item.lotNos.includes(no))
    if (field.key === 'opNo') return value.split(',').map(norm).includes(item.opNo)
    return String(item[field.key]).toUpperCase() === value
  }))
  const wantedLots = flags.lotNo ? filters.lotNo.split(',').map(norm) : null
  rows.value = result.sort((a, b) => a.swrNo.localeCompare(b.swrNo)).flatMap(item => (item.lotNos.length ? item.lotNos : ['']).filter(lotNo => !wantedLots || wantedLots.includes(lotNo)).map(lotNo => ({ swr: item, lotNo })))
  activeNo.value = rows.value[0]?.swr.swrNo || ''; selectedNos.value = []; queried.value = true
  message.value = `查得 ${rows.value.length} 列、${result.length} 張 SWR；一張 SWR 可有多筆 Lot 明細。`
}
function cell(row: { swr: Swr; lotNo: string }, key: keyof Swr | 'lotNo' | 'haveAttachFile'): string {
  if (!dev.value) return ''

  if (key === 'lotNo') return row.lotNo
  if (key === 'haveAttachFile') return row.swr.attachmentName ? 'Y' : 'N'
  if (key === 'swrType') return typeLabels[row.swr.swrType] || String(row.swr.swrType)
  if (key === 'status') return statusLabels[row.swr.status] || String(row.swr.status)
  return String(row.swr[key] ?? '')
}
function selectRow(swrNo: string, multi: boolean): void {
  if (!dev.value) return

  activeNo.value = swrNo
  if (multi) selectedNos.value = selectedNos.value.includes(swrNo) ? selectedNos.value.filter(no => no !== swrNo) : [...selectedNos.value, swrNo]
  else selectedNos.value = [swrNo]
}
function openForm(mode: Mode): void {
  error.value = ''
  if (!dev.value) return
  if (mode !== 'add' && !active.value) { error.value = '請選資料列。'; return }
  if (mode === 'edit' && active.value?.status !== 0) { error.value = '只有 UNFROZEN 可以修改。'; return }
  const source = mode === 'add' ? null : active.value
  Object.assign(draft, source ? { ...source, lotNos: [...source.lotNos] } : { swrNo: '', swrType: 0, status: 0, customerNo: '', opNo: '', deviceNo: '', startTime: '', endTime: '', customerLotNo: '', moNo: '', swrDesc: '', creator: '', createDate: '', attachmentName: '', lotNos: [] })
  if (mode !== 'edit') { draft.swrNo = nextNo(); draft.creator = props.currentUserNo || 'DEV'; draft.createDate = nowText(); draft.status = 0; draft.attachmentName = ''; if (!draft.swrNo) { error.value = '今日 SWR 流水號已超過 999；請處理編號規則。'; return } }
  draftLots.value = draft.lotNos.join(','); startChecked.value = !!draft.startTime; endChecked.value = !!draft.endTime; modal.value = mode
}
function validateDraft(): string {
  if (!dev.value) return unavailableMessage

  draft.customerNo = norm(draft.customerNo); draft.opNo = norm(draft.opNo); draft.deviceNo = norm(draft.deviceNo); draft.moNo = norm(draft.moNo)
  draft.customerLotNo = norm(draft.customerLotNo); draft.lotNos = [...new Set(draftLots.value.split(',').map(norm).filter(Boolean))]
  if (!draft.swrNo || !draft.customerNo || !draft.opNo || !draft.swrDesc.trim() || ![0, 1].includes(draft.swrType)) return 'SWRNo、CustomerNo、OPNo、SWRDescription、SWRType 必填。'
  if (draft.swrNo.length > 20 || draft.customerNo.length > 50 || draft.opNo.length > 20 || draft.deviceNo.length > 50 || draft.customerLotNo.length > 50 || draft.moNo.length > 50 || draft.lotNos.some(no => no.length > 50)) return '輸入值超過 Oracle 欄位長度。'
  if (!lookupData.customerNo.includes(draft.customerNo) || !lookupData.opNo.includes(draft.opNo)) return 'DEV lookup 中找不到 CustomerNo 或 OPNo。'
  if (startChecked.value !== endChecked.value) return 'StartTime 與 EndTime 必須一起設定。'
  if (startChecked.value && (!draft.startTime || !draft.endTime || new Date(draft.startTime).getTime() >= new Date(draft.endTime).getTime())) return 'EndTime 必須晚於 StartTime。'
  for (const lotNo of draft.lotNos) if (lotCustomer[lotNo] !== draft.customerNo) return `LotNo ${lotNo} 不屬於 ${draft.customerNo}。`
  if (draft.swrDesc.length > 400) return 'SWRDescription 超過 Oracle 400 字元。'
  if (draft.attachmentName.length > 200) return '附件檔名超過 Oracle AttachFilePath 200 字元。'
  return ''
}
function confirmForm(): void {
  if (!dev.value) return

  error.value = validateDraft()
  if (error.value || !modal.value) return
  load()
  const item: Swr = { ...draft, status: 0, startTime: startChecked.value ? draft.startTime : '', endTime: endChecked.value ? draft.endTime : '', lotNos: [...draft.lotNos] }
  const previous = structuredClone(saved.value)
  if (modal.value === 'edit') {
    const index = saved.value.items.findIndex(value => value.swrNo === item.swrNo && value.status === 0)
    if (index < 0) { error.value = '資料已變更，請重新查詢。'; return }
    saved.value.items[index] = item
  } else {
    if (saved.value.items.some(value => value.swrNo === item.swrNo)) { error.value = 'SWRNo 已存在，請重新開啟新增。'; return }
    saved.value.items.push(item)
    const day = dayKey(); saved.value.serialByDay[day] = (saved.value.serialByDay[day] || 1) + 1
  }
  if (!persist()) { saved.value = previous; return }
  modal.value = null; status.value = 0; query(); activeNo.value = item.swrNo; message.value = `DEV 已${previous.items.some(value => value.swrNo === item.swrNo) ? '修改' : '新增'} ${item.swrNo}。`
}
function selectedItems(): Swr[] {
  if (!dev.value) return []
 return [...new Set(selectedNos.value.length ? selectedNos.value : activeNo.value ? [activeNo.value] : [])].map(no => saved.value.items.find(item => item.swrNo === no)).filter((item): item is Swr => !!item) }
function openAction(kind: Action): void {
  if (!dev.value) return
 error.value = ''; if (!selectedItems().length) { error.value = '請選資料列。'; return }; abortReason.value = ''; action.value = kind }
function confirmAction(): void {
  error.value = ''
  if (!action.value || !dev.value) return
  if (action.value === 'void' && !abortReason.value.trim()) { error.value = '請填 DEV 作廢原因（模擬 InputAbortLog=1）。'; return }
  const targets = selectedItems()
  const actionLabel = action.value === 'approve' ? '核准' : '作廢'
  const previous = structuredClone(saved.value)
  let count = 0
  for (const item of targets) {
    if (action.value === 'approve') {
      if (item.status !== 0) { error.value = `${item.swrNo} 只有 UNFROZEN 可核准；先前成功 ${count} 筆。`; break }
      if (item.startTime && (!item.endTime || new Date(item.startTime) >= new Date(item.endTime))) { error.value = `${item.swrNo} 日期區間無效；先前成功 ${count} 筆。`; break }
      item.status = 2
    } else if (item.status === 2) item.status = 99
    else if (item.status === 0 || item.status === 99) saved.value.items = saved.value.items.filter(value => value.swrNo !== item.swrNo)
    else { error.value = `${item.swrNo} PENDING 不可作廢；先前成功 ${count} 筆。`; break }
    count += 1
  }
  if (!persist()) { saved.value = previous; return }
  const failure = error.value
  action.value = null; query(); if (failure) error.value = failure
  message.value = `DEV 已${actionLabel} ${count} 筆；沒有正式交易。`
}
function download(): void {
  if (!dev.value) return
 if (!active.value) { error.value = '請選資料列。'; return }; message.value = active.value.attachmentName ? `${active.value.attachmentName} 只有 DEV 檔名；正式附件下載由 API 提供。` : '此 SWR 沒有附件。' }
function upload(event: Event): void {
  if (!dev.value) return
 const input = event.target as HTMLInputElement; const file = input.files?.[0]; if (!file) return; if (!file.name.toLowerCase().endsWith('.pdf')) { error.value = '原 WinForms 僅接受 PDF 檔案。'; input.value = ''; return }; error.value = ''; draft.attachmentName = file.name }
function chooseLookup(value: string): void {
  if (!dev.value) return
 if (!lookup.value) return; if (lookup.value.target === 'query') { filters[lookup.value.key as FilterKey] = value; flags[lookup.value.key as FilterKey] = true } else if (lookup.value.key === 'lotNo') draftLots.value = [...new Set([...draftLots.value.split(',').map(norm).filter(Boolean), value])].join(','); else (draft as unknown as Record<string, string>)[lookup.value.key] = value; lookup.value = null }
function openLookup(key: string, target: 'query' | 'form'): void { if (!dev.value) return; lookup.value = { key, target } }
onMounted(load)
</script>

<template>
  <section class="swr-page">
    <h1>特殊生產需求維護 <small>SWR BASIS · WP109</small></h1>
    <p class="note">{{ dev ? 'DEV 假資料；附件僅保存檔名，正式 SWR／簽核／作廢需 API。' : unavailableMessage }}</p>
    <p v-if="error" class="error" role="alert">{{ error }}</p>
    <fieldset :disabled="!dev"><legend>Query</legend><div class="query-grid"><label v-for="field in queryFields" :key="field.key"><span><input v-model="flags[field.key]" type="checkbox"> {{ field.label }}</span><select v-if="field.key === 'swrType'" v-model="filters.swrType" :disabled="!dev || !flags.swrType"><option value="">請選擇</option><option value="0">作業站指示</option><option value="1">包裝作業說明</option></select><input v-else v-model="filters[field.key]" class="yellow" :disabled="!dev || !flags[field.key]"><button v-if="field.lookup" type="button" class="button" @click="openLookup(field.key, 'query')">...</button></label><button type="button" class="button query-button" :disabled="!dev" @click="query">Query</button></div></fieldset>
    <div class="work-area"><div class="main-pane"><fieldset class="list" :disabled="!dev"><legend>SWR Basis</legend><div class="status-radios"><label v-for="value in [0,1,2,99]" :key="value"><input v-model="status" type="radio" name="swr-status" :value="value" @change="query"> {{ statusLabels[value] }}</label></div><div class="scroll"><table><thead><tr><th v-for="column in columns" :key="column.label">{{ column.label }}</th></tr></thead><tbody><tr v-for="(row,index) in rows" :key="`${row.swr.swrNo}-${row.lotNo}-${index}`" :class="{ active: activeNo === row.swr.swrNo, selected: selectedNos.includes(row.swr.swrNo) }" @click="selectRow(row.swr.swrNo, $event.ctrlKey || $event.metaKey)" @dblclick="openForm('edit')"><td v-for="column in columns" :key="column.label">{{ cell(row, column.key) }}</td></tr><tr v-if="!rows.length"><td :colspan="columns.length" class="empty">{{ !dev ? unavailableMessage : queried ? '沒有符合條件的 SWR。' : '請先 Query。' }}</td></tr></tbody></table></div><div class="status-bar">{{ message }} <span>{{ rows.length }} 列／已選 {{ selectedNos.length }} 張</span></div></fieldset>
    <fieldset :disabled="!dev"><legend>SWRDescription</legend><textarea :value="active?.swrDesc || ''" readonly></textarea></fieldset>
    </div><div class="actions"><button type="button" class="button" :disabled="!dev" @click="openForm('add')">ADD (A)</button><button type="button" class="button" :disabled="!active || !dev" @click="openForm('edit')">EDIT (E)</button><button type="button" class="button" :disabled="!active || !dev" @click="openForm('copy')">Copy (C)</button><button type="button" class="button" :disabled="!active || !dev" @click="openAction('approve')">Approve (Y)</button><button type="button" class="button" :disabled="!active || !dev" @click="openAction('void')">Void</button><button type="button" class="button" :disabled="!active || !dev" @click="download">DownLoad File</button><button type="button" class="button" @click="emit('close')">Close (X)</button></div></div>
    <div v-if="dev && modal" class="overlay"><div class="dialog form-dialog" role="dialog" aria-modal="true"><h2>SWR Basis Define ({{ modal.toUpperCase() }})</h2><div class="form-grid"><label>SWRNo <input :value="draft.swrNo" readonly></label><label>SWRType <select v-model.number="draft.swrType"><option :value="0">作業站指示</option><option :value="1">包裝作業說明</option></select></label><label>CustomerNo <input v-model="draft.customerNo" class="yellow"><button type="button" class="button" @click="openLookup('customerNo', 'form')">...</button></label><label>OPNo <input v-model="draft.opNo" class="yellow"><button type="button" class="button" @click="openLookup('opNo', 'form')">...</button></label><label>MONo <input v-model="draft.moNo" class="yellow"><button type="button" class="button" @click="openLookup('moNo', 'form')">...</button></label><label>CustomerLotNo <input v-model="draft.customerLotNo" class="yellow"></label><label>DeviceNo <input v-model="draft.deviceNo" class="yellow"><button type="button" class="button" @click="openLookup('deviceNo', 'form')">...</button></label><label>LotNo <input v-model="draftLots" class="yellow"><button type="button" class="button" @click="openLookup('lotNo', 'form')">...</button></label><label><input v-model="startChecked" type="checkbox"> StartTime <input v-model="draft.startTime" type="datetime-local" :disabled="!startChecked"></label><label><input v-model="endChecked" type="checkbox"> EndTime <input v-model="draft.endTime" type="datetime-local" :disabled="!endChecked"></label><label>AttachFile <input type="file" accept=".pdf,application/pdf" @change="upload"><small>{{ draft.attachmentName || '無' }} · DEV 僅檔名</small></label><label>Creator <input :value="draft.creator" readonly></label><label>Create Date <input :value="draft.createDate" readonly></label><label class="wide">SWRDescription <textarea v-model="draft.swrDesc" class="yellow"></textarea></label></div><div class="dialog-actions"><button type="button" class="button primary" @click="confirmForm">Confirm</button><button type="button" class="button" @click="modal = null">Close</button></div></div></div>
    <div v-if="dev && action" class="overlay"><div class="dialog" role="alertdialog" aria-modal="true"><h2>{{ action === 'approve' ? 'Approve' : 'Void' }} SWR</h2><p>{{ selectedItems().length }} 張 SWR；逐筆處理，遇錯停止。</p><label v-if="action === 'void'">AbortReason <input v-model="abortReason" class="yellow"></label><div class="dialog-actions"><button type="button" class="button" @click="action = null">Cancel</button><button type="button" class="button primary" @click="confirmAction">OK</button></div></div></div>
    <div v-if="dev && lookup" class="overlay top"><div class="dialog" role="dialog" aria-modal="true"><h2>{{ lookup.key }} Lookup</h2><div class="lookup-list"><button v-for="value in lookupOptions" :key="value" type="button" class="button" @click="chooseLookup(value)">{{ value }}</button></div><button type="button" class="button" @click="lookup = null">Close</button></div></div>
  </section>
</template>

<style scoped>
.swr-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:7px;padding:9px;box-sizing:border-box;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif}h1{margin:0;font-size:18px}h1 small{font-size:12px;font-weight:400;color:#777}.note,.error{margin:0;padding:6px;border:1px solid #d8c994;background:#fff8dc}.error{border-color:#d8a7a2;background:#fff1ef;color:#8f261f}fieldset{min-width:0;margin:0;padding:7px;border:1px solid #bbb;background:#fff}legend{padding:0 5px}.query-grid{display:grid;grid-template-columns:repeat(4,minmax(190px,1fr));gap:5px}.query-grid label{display:flex;align-items:center;gap:4px}.query-grid label span{min-width:100px}.query-grid input:not([type=checkbox]),.query-grid select{min-width:0;width:120px;height:24px;border:1px solid #888;box-sizing:border-box}.yellow{background:#fff0b3}.query-grid input:disabled{background:#eee}.button{min-height:26px;padding:3px 8px;border:1px solid #aaa;border-radius:3px;background:linear-gradient(#fff,#eee);color:#333;font:inherit;cursor:pointer}.button:disabled{color:#aaa;cursor:default}.query-button{grid-column:4;justify-self:end}.work-area{flex:1;min-height:250px;display:grid;grid-template-columns:minmax(0,1fr) 110px;gap:7px}.main-pane{min-height:0;display:flex;flex-direction:column;gap:7px}.list{flex:1;min-height:180px;display:flex;flex-direction:column}.status-radios{display:flex;gap:15px;padding-bottom:6px}.scroll{flex:1;min-height:0;overflow:auto;border:1px solid #aaa}table{min-width:1750px;border-collapse:collapse;white-space:nowrap}th,td{padding:4px 7px;border:1px solid #ccc;text-align:left}th{position:sticky;top:0;background:#ead591;font-weight:500}td{background:#ffe89c}tr.active td{background:#ffdb73}tr.selected td{outline:1px solid #a86f17}.empty{text-align:center;background:#fff;color:#777;height:36px}.status-bar{display:flex;justify-content:space-between;color:#666;padding-top:5px}fieldset textarea{width:100%;height:82px;box-sizing:border-box;border:1px solid #888}.actions{display:flex;flex-direction:column;align-items:stretch;gap:6px;justify-content:flex-start;padding-top:65px}.actions .button:last-child{margin-top:auto}.overlay{position:fixed;inset:0;z-index:2000;display:flex;align-items:center;justify-content:center;padding:12px;background:#0007}.overlay.top{z-index:2100}.dialog{width:min(540px,96vw);max-height:90vh;overflow:auto;padding:12px;border:1px solid #888;background:#f7f5ee;box-shadow:0 8px 28px #0005}.form-dialog{width:min(950px,96vw)}.dialog h2{margin:0 0 8px}.dialog p{margin:6px 0}.dialog label{display:flex;align-items:center;gap:4px}.dialog input:not([type=checkbox]):not([type=file]),.dialog select{min-width:0;height:25px;box-sizing:border-box;border:1px solid #888;padding:2px 5px}.form-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:7px}.form-grid label{justify-content:flex-end}.form-grid label>input:not([type=checkbox]),.form-grid label>select{width:58%}.form-grid .wide{grid-column:1/-1}.form-grid .wide textarea{width:75%;height:70px}.dialog-actions{display:flex;justify-content:flex-end;gap:6px;margin-top:10px}.primary{background:#f3a62a;border-color:#e99512;color:#fff}.lookup-list{display:flex;flex-wrap:wrap;gap:6px;margin:8px 0}@media(max-width:1000px){.query-grid{grid-template-columns:repeat(2,minmax(190px,1fr))}.query-button{grid-column:2}}@media(max-width:680px){.form-grid{grid-template-columns:1fr}.form-grid .wide{grid-column:1}}
@media(max-width:680px){.work-area{grid-template-columns:1fr}.actions{padding-top:0;flex-direction:row;flex-wrap:wrap}.actions .button:last-child{margin-top:0}}</style>
