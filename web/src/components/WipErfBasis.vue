<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'

type ErfRow = Record<string, string | number | null | string[]> & {
  erfNo: string; lotNo: string; opNo: string; customerNo: string; mClassNo: string; curGroupNo: string;
  createdAt: string; releaseAt: string | null; status: number; erfSource: number;
  batchLotNos: string[]; qrsNo: string; qrsIssueState: number; holdItems: string[]
}
type FilterKey = 'lotNo' | 'opNo' | 'customerNo' | 'mClassNo' | 'curGroupNo' | 'erfNo'
type LookupKey = 'opNo' | 'customerNo' | 'mClassNo' | 'curGroupNo'
type DateKey = 'createdAt' | 'releaseAt'
type DetailRecord = Record<string, string | number>
type MockAttachment = { source: 'ERF' | 'OCAP'; name: string; attachType: number; creator: string; createdAt: string; ocapNo: string }

const props = defineProps<{ developmentPreview?: boolean; currentUserNo?: string; initialErfNo?: string }>()
const emit = defineEmits<{ close: []; create8d: [erf: { erfNo: string; lotNo: string; productNo: string; opNo: string; customerNo: string; lotQty: number; errorQty: number; holdDescription: string }]; open8dMaintain: [qrsNo: string] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview !== false)
const unavailableText = '正式環境尚未開放此功能。'
const filters = reactive<Record<FilterKey, string>>({ lotNo: '', opNo: '', customerNo: '', mClassNo: '', curGroupNo: '', erfNo: '' })
const checked = reactive<Record<FilterKey, boolean>>({ lotNo: false, opNo: false, customerNo: false, mClassNo: false, curGroupNo: false, erfNo: false })
const dateChecked = reactive<Record<DateKey, boolean>>({ createdAt: false, releaseAt: false })
const dates = reactive<Record<DateKey, { from: string; to: string }>>({ createdAt: { from: '', to: '' }, releaseAt: { from: '', to: '' } })
const lookupField = ref<LookupKey | null>(null)
const lookupOptions = computed(() => dev.value && lookupField.value ? [...new Set(fixtures.map(row => String(row[lookupField.value!] || '')).filter(Boolean))].sort() : [])
const rows = ref<ErfRow[]>([]); const activeNo = ref(''); const searched = ref(false)
const active = computed(() => rows.value.find(row => row.erfNo === activeNo.value) || null)
const dialog = ref<'info' | '8d' | null>(null); const infoTab = ref('Create Hold Code')
const infoMessage = ref(''); const pendingDeleteAttachment = ref<MockAttachment | null>(null)
const initialMessage = '請至少選一個查詢條件，再按 Query (Q)。'
const errorText = ref(''); const message = ref(dev.value ? initialMessage : unavailableText)
function unavailable(): boolean { if (dev.value) return false; errorText.value = ''; message.value = unavailableText; return true }
const statusLabels: Record<number, string> = { 0: 'ERF Undisposition', 1: 'ERF Wait Disposition', 2: 'Notice Customer and Wait Disposition', 3: 'Waive', 11: 'Wait Lot Disposition', 12: 'Abort', 21: 'Release', 31: 'Hold Bank' }
const filterFields: { key: FilterKey; label: string }[] = [
  { key: 'lotNo', label: 'Lot No' }, { key: 'opNo', label: 'OP No' }, { key: 'customerNo', label: 'Customer No' },
  { key: 'mClassNo', label: 'MClass No' }, { key: 'curGroupNo', label: 'Cur Group' }, { key: 'erfNo', label: 'ERF No' },
]
const columns = [
  ['erfNo','ERF No'],['lotNo','Lot No'],['productNo','Product No'],['productName','Product Name'],['customerLotNo','Customer Lot No'],['status','Status'],['opNo','OPNo'],['customerNo','CustomerNo'],['customerName','Customer Name'],['mClassNo','MClassNo'],['holdDescription','HoldDescription'],['curGroupNo','CurGroupNo'],['dispDescription','DispDescription'],['ruleNo','RuleNo'],['createQty','CreateQty'],['createdAt','Create Date'],['erfRecordQty','ERF Record Qty'],['curQty','CurQty'],['releaseQty','Release Qty'],['releaser','Release UserNo'],['releaseAt','Release Date'],['calHoldTime','Cal Hold Time'],['priority','Priority'],['batchLot','BatchLot'],['qrsNo','QRSNo'],
] as const
const detailTabs = ['Create Hold Code', 'Hold Code Disposition', 'ERF Disposition', 'Lot Disposition', 'ERF Attach']
const linkKey = 'ymes-dev-mock:wip-8d-erf-links:v1'
const attachmentSources = ['ERF', 'OCAP'] as const
const nextDispTypeLabels: Record<number, string> = { 0: 'Next Group', 1: 'Notice Customer', 2: 'Final', 3: 'Waive' }
const lotDispTypeLabels: Record<number, string> = { 0: 'GO', 1: 'Jump OP', 2: 'Jump Process', 3: 'WIP Semi Inventory', 4: 'Process Tuning', 5: 'Change ENGNo', 6: 'Jump Module', 7: 'WIP Scrap Inventory', 8: 'Jump Main', 11: 'Back To ERF Disposition', 12: 'Split', 13: 'Merge', 14: 'Qty Adjust' }
const detailColumns: Record<string, string[]> = {
  'Create Hold Code': ['ComponentNo', 'AreaNo', 'HoldItemNo', 'HoldItemName', 'RECORDQTY', 'Description', 'OCAPNO'],
  'Hold Code Disposition': ['ComponentNo', 'AreaNo', 'HoldItemNo', 'HoldItemName', 'RECORDQTY', 'Description', 'HoldDisposition'],
  'ERF Disposition': ['DispOrder', 'GroupNo', 'DispDescription', 'ContactorName', 'NextDispType', 'NextGroupNo', 'Creator', 'CreateDate'],
  'Lot Disposition': ['LotDispOrder', 'LotDispType', 'Description', 'Creator', 'CreateDate', 'OPNo', 'ProcessNo', 'ProcessVersion'],
}
const detailFixtures: Record<string, Record<string, DetailRecord[]>> = {
  'DEV-ERF-001': {
    'Create Hold Code': [{ ComponentNo: 'N/A', AreaNo: 'N/A', HoldItemNo: 'H-001', HoldItemName: '外觀異常', RECORDQTY: 8, Description: '表面刮傷', OCAPNO: '' }],
    'Hold Code Disposition': [], 'ERF Disposition': [], 'Lot Disposition': [],
  },
  'DEV-ERF-002': {
    'Create Hold Code': [{ ComponentNo: 'N/A', AreaNo: 'N/A', HoldItemNo: 'H-003', HoldItemName: '設備異常', RECORDQTY: 1, Description: '設備點檢異常', OCAPNO: 'OCAP-DEV-02' }],
    'Hold Code Disposition': [{ ComponentNo: 'N/A', AreaNo: 'N/A', HoldItemNo: 'H-003', HoldItemName: '設備異常', RECORDQTY: 1, Description: '完成確認', HoldDisposition: '解除' }],
    'ERF Disposition': [{ DispOrder: 1, GroupNo: 'ENG-01', DispDescription: '完成處置', ContactorName: 'DEV', NextDispType: 2, NextGroupNo: '', Creator: 'DEV', CreateDate: '2026/11/19 12:30:00' }],
    'Lot Disposition': [{ LotDispOrder: 1, LotDispType: 0, Description: '解除異常', Creator: 'DEV', CreateDate: '2026/11/19 12:30:00', OPNo: 'OP-TEST', ProcessNo: 'PROC-DEV', ProcessVersion: '1' }],
  },
  'DEV-ERF-003': {
    'Create Hold Code': [{ ComponentNo: 'N/A', AreaNo: 'N/A', HoldItemNo: 'H-002', HoldItemName: '尺寸異常', RECORDQTY: 2, Description: '批次尺寸異常', OCAPNO: '' }],
    'Hold Code Disposition': [], 'ERF Disposition': [], 'Lot Disposition': [],
  },
}
const attachmentFixtures: Record<string, MockAttachment[]> = {
  'DEV-ERF-001': [
    { source: 'ERF', name: 'DEV-ERF-001.pdf', attachType: 0, creator: 'DEV', createdAt: '2026/11/19 08:00:00', ocapNo: '' },
    { source: 'ERF', name: 'photo-001.jpg', attachType: 1, creator: 'DEV', createdAt: '2026/11/19 08:05:00', ocapNo: '' },
  ],
  'DEV-ERF-002': [
    { source: 'ERF', name: 'DEV-ERF-002.pdf', attachType: 0, creator: 'DEV', createdAt: '2026/11/18 10:00:00', ocapNo: '' },
    { source: 'OCAP', name: 'ocap-review.txt', attachType: 1, creator: 'DEV', createdAt: '2026/11/19 12:00:00', ocapNo: 'OCAP-DEV-02' },
  ],
  'DEV-ERF-003': [],
}
function seedAttachments(): Record<string, MockAttachment[]> { return Object.fromEntries(Object.entries(attachmentFixtures).map(([key, rows]) => [key, rows.map(row => ({ ...row }))])) }
const attachmentStore = ref<Record<string, MockAttachment[]>>(dev.value ? seedAttachments() : {})
const detailRows = computed<DetailRecord[]>(() => dev.value ? detailFixtures[activeNo.value]?.[infoTab.value] || [] : [])
const hasOcap = computed(() => dev.value && (detailFixtures[activeNo.value]?.['Create Hold Code'] || []).some(row => !!row.OCAPNO))
const currentDetailColumns = computed<string[]>(() => infoTab.value === 'Create Hold Code' && !hasOcap.value ? (detailColumns[infoTab.value] || []).filter(column => column !== 'OCAPNO') : detailColumns[infoTab.value] || [])
const attachmentRows = computed<MockAttachment[]>(() => dev.value ? attachmentStore.value[activeNo.value] || [] : [])
function formatDetailCell(row: DetailRecord, column: string): string | number { const value = row[column]; if (column === 'NextDispType') return nextDispTypeLabels[Number(value)] || value || ''; if (column === 'LotDispType') return lotDispTypeLabels[Number(value)] || value || ''; return value ?? '' }
function isGeneratedErfFile(name: string): boolean { return !!active.value && name.toLocaleUpperCase().includes(`${active.value.erfNo}.`.toLocaleUpperCase()) }
function viewAttachment(row: MockAttachment): void { if (unavailable()) return; infoMessage.value = `DEV 假附件 ${row.name}：沒有真實位元組；正式端需經授權 API 下載。` }
function requestDeleteAttachment(row: MockAttachment): void { if (unavailable()) return;
  infoMessage.value = ''
  if (!dev.value || isGeneratedErfFile(row.name)) { infoMessage.value = '異常單產生檔不可刪除。'; return }
  pendingDeleteAttachment.value = row
}
function confirmDeleteAttachment(): void { if (unavailable()) return;
  const row = pendingDeleteAttachment.value
  pendingDeleteAttachment.value = null
  if (!row || !dev.value || !active.value || isGeneratedErfFile(row.name)) return
  const current = attachmentStore.value[active.value.erfNo] || []
  if (!current.includes(row)) { infoMessage.value = '附件已不存在，請重新開啟詳細資料。'; return }
  attachmentStore.value[active.value.erfNo] = current.filter(item => item !== row)
  infoMessage.value = `已從 DEV 假資料移除 ${row.name}；未呼叫 API。`
}

const fixtures: ErfRow[] = [
  { erfNo:'DEV-ERF-001', lotNo:'DEV-LOT-001', opNo:'OP-CUT', customerNo:'CUS-A', mClassNo:'M-QUALITY', curGroupNo:'QA-01', createdAt:'2026-11-19T08:00:00', releaseAt:null, status:0, erfSource:0, batchLotNos:[], qrsNo:'', qrsIssueState:0, holdItems:['H-001 外觀異常'], productNo:'PRD-A', productName:'示範產品 A', customerLotNo:'CL-001', customerName:'示範客戶 A', holdDescription:'外觀檢查異常', dispDescription:'', ruleNo:'', createQty:8, scrapQty:0, curQty:8, releaseQty:0, releaser:'', priority:1, batchLot:'' },
  { erfNo:'DEV-ERF-002', lotNo:'DEV-LOT-002', opNo:'OP-TEST', customerNo:'CUS-B', mClassNo:'M-EQUIPMENT', curGroupNo:'ENG-01', createdAt:'2026-11-18T10:00:00', releaseAt:'2026-11-19T12:30:00', status:21, erfSource:0, batchLotNos:[], qrsNo:'QRS-DEV-002', qrsIssueState:2, holdItems:['H-003 設備異常'], productNo:'PRD-B', productName:'示範產品 B', customerLotNo:'CL-002', customerName:'示範客戶 B', holdDescription:'設備點檢異常', dispDescription:'完成處置', ruleNo:'RULE-02', createQty:6, scrapQty:1, curQty:5, releaseQty:5, releaser:'DEV', priority:2, batchLot:'' },
  { erfNo:'DEV-ERF-003', lotNo:'DEV-LOT-003', opNo:'OP-CUT', customerNo:'CUS-A', mClassNo:'M-QUALITY', curGroupNo:'QA-01', createdAt:'2026-11-20T07:00:00', releaseAt:null, status:1, erfSource:0, batchLotNos:['DEV-LOT-003','DEV-LOT-004'], qrsNo:'', qrsIssueState:0, holdItems:['H-002 尺寸異常'], productNo:'PRD-A', productName:'示範產品 A', customerLotNo:'CL-003', customerName:'示範客戶 A', holdDescription:'批次尺寸異常', dispDescription:'待判定', ruleNo:'', createQty:12, scrapQty:2, curQty:10, releaseQty:0, releaser:'', priority:1, batchLot:'DEV-LOT-003,DEV-LOT-004' },
]

function setFilterEnabled(key: FilterKey): void { if (unavailable()) return; if (!checked[key]) filters[key] = '' }
function setDateEnabled(key: DateKey): void { if (unavailable()) return; if (!dateChecked[key]) { dates[key].from = ''; dates[key].to = '' } }
function chooseLookup(value: string): void { if (unavailable()) return; if (lookupField.value) { filters[lookupField.value] = value; checked[lookupField.value] = true }; lookupField.value = null }
function toggleLookup(key: FilterKey): void { if (unavailable()) return; if (key === 'lotNo' || key === 'erfNo') return; lookupField.value = lookupField.value === key ? null : key }
function dateMatches(value: string | null, range: { from: string; to: string }): boolean {
  if (!value) return false
  const instant = Date.parse(value)
  return (!range.from || instant >= Date.parse(range.from)) && (!range.to || instant <= Date.parse(range.to))
}
function load8dLinks(): Record<string, string> { if (!dev.value) return {}; try { const data = JSON.parse(window.localStorage.getItem(linkKey) || '{}') as unknown; return data && typeof data === 'object' && !Array.isArray(data) ? data as Record<string, string> : {} } catch { return {} } }
function load8dIssueStates(): Record<string, number> { if (!dev.value) return {}; try { const rows = JSON.parse(window.localStorage.getItem('ymes-dev-mock:wip-8d-basis-factory:state:v1') || '[]') as unknown; return Array.isArray(rows) ? Object.fromEntries(rows.filter(row => row && typeof row.qrsNo === 'string' && typeof row.issueState === 'number').map(row => [row.qrsNo, row.issueState])) : {} } catch { return {} } }
function query(): void { if (unavailable()) return;
  errorText.value = ''; lookupField.value = null
  for (const key of Object.keys(filters) as FilterKey[]) filters[key] = filters[key].trim().toUpperCase()
  if (!(Object.keys(filters) as FilterKey[]).some(key => checked[key] && filters[key]) && !(Object.keys(dateChecked) as DateKey[]).some(key => dateChecked[key])) { errorText.value = '請至少選一個查詢條件。'; return }
  for (const key of Object.keys(dateChecked) as DateKey[]) if (dateChecked[key] && (!dates[key].from || !dates[key].to || !Number.isFinite(Date.parse(dates[key].from)) || !Number.isFinite(Date.parse(dates[key].to)) || Date.parse(dates[key].from) > Date.parse(dates[key].to))) { errorText.value = `${key === 'createdAt' ? 'Create Date' : 'Release Date'} 起迄時間不正確。`; return }
  const links = load8dLinks()
  const issueStates = load8dIssueStates()
  const hasSaved8dState = window.localStorage.getItem('ymes-dev-mock:wip-8d-basis-factory:state:v1') !== null
  rows.value = fixtures.filter(row => row.erfSource === 0 &&
    (Object.keys(filters) as FilterKey[]).every(key => {
      if (!checked[key] || !filters[key]) return true
      if (key === 'lotNo') return (row.batchLotNos.length ? row.batchLotNos : [row.lotNo]).some(lotNo => lotNo.includes(filters.lotNo))
      return String(row[key] || '') === filters[key]
    }) &&
    (!dateChecked.createdAt || dateMatches(row.createdAt, dates.createdAt)) &&
    (!dateChecked.releaseAt || dateMatches(row.releaseAt, dates.releaseAt)))
    .map(row => { const candidate = links[row.erfNo] || row.qrsNo; const qrsNo = hasSaved8dState && candidate && !Object.prototype.hasOwnProperty.call(issueStates, candidate) ? '' : candidate; return { ...row, qrsNo, qrsIssueState: qrsNo ? issueStates[qrsNo] ?? row.qrsIssueState : 0 } })
    .sort((a,b) => a.erfNo.localeCompare(b.erfNo))
  activeNo.value = ''; searched.value = true; message.value = `查得 ${rows.value.length} 筆 WIP 異常單。`
}
function formatCell(row: ErfRow, key: string): string | number {
  if (key === 'status') return statusLabels[row.status] || ''
  if (key === 'erfRecordQty') return Number(row.scrapQty || 0)
  if (key === 'calHoldTime') return `${Math.max(0,Math.round((Date.parse(row.releaseAt && [12,21].includes(row.status) ? row.releaseAt : new Date().toISOString()) - Date.parse(row.createdAt))/60000))} Minutes`
  if (key === 'createdAt' || key === 'releaseAt') return String(row[key] || '').slice(0,19).replace('T',' ')
  return String(row[key] ?? '')
}
function openInfo(): void { if (unavailable()) return; if (!active.value) return; infoTab.value = detailTabs[0]; infoMessage.value = ''; pendingDeleteAttachment.value = null; dialog.value = 'info' }
function closeDialog(): void { pendingDeleteAttachment.value = null; dialog.value = null }
function selectInfoTab(tab: string): void { if (unavailable()) return; infoTab.value = tab; pendingDeleteAttachment.value = null; infoMessage.value = '' }
function open8d(): void {
  if (unavailable()) return
  const row = active.value
  if (!row) return
  if (!row.qrsNo) {
    emit('create8d', { erfNo: row.erfNo, lotNo: row.lotNo, productNo: String(row.productNo || ''), opNo: row.opNo, customerNo: row.customerNo, lotQty: Number(row.curQty || 0), errorQty: Number(row.scrapQty || 0), holdDescription: String(row.holdDescription || '') })
    return
  }
  if (row.qrsIssueState === -1) { errorText.value = '此 8D 單已作廢，來源畫面不開啟。'; return }
  emit('open8dMaintain', row.qrsNo)
}
watch(dev, enabled => { if (enabled) { attachmentStore.value = seedAttachments(); errorText.value = ''; message.value = initialMessage } else { rows.value = []; activeNo.value = ''; closeDialog(); attachmentStore.value = {}; searched.value = false; lookupField.value = null; for (const key of Object.keys(filters) as FilterKey[]) { filters[key] = ''; checked[key] = false }; for (const key of Object.keys(dates) as DateKey[]) { dates[key].from = ''; dates[key].to = ''; dateChecked[key] = false }; infoMessage.value = ''; errorText.value = ''; message.value = unavailableText } })
onMounted(() => {
  if (!dev.value || !props.initialErfNo) return
  checked.erfNo = true; filters.erfNo = props.initialErfNo; query()
  if (rows.value.length) { activeNo.value = rows.value[0].erfNo; openInfo() }
})
</script>

<template>
  <section class="wp508-page">
    <div v-if="dev" class="dev-banner">DEV 假資料。此頁查詢 WIP 異常單；ERF Info 的附件操作只改本機假資料，8D 為獨立 WP801 入口。</div>
    <h1>異常單資料 <small>ERF BASIS · WP508</small></h1>
    <p v-if="errorText" class="notice error" role="alert">{{ errorText }}</p><p v-else class="notice" role="status">{{ message }}</p>
    <fieldset class="query-frame"><legend>Query</legend>
      <div class="query-grid">
        <label v-for="field in filterFields" :key="field.key" :class="'field-'+field.key"><input v-model="checked[field.key]" type="checkbox" :disabled="!dev" @change="setFilterEnabled(field.key)">{{ field.label }}<span v-if="field.key === 'lotNo'" class="percent">%</span><input v-model="filters[field.key]" class="mes-input yellow" :disabled="!dev" @keydown.enter.prevent="query"><button v-if="field.key !== 'lotNo' && field.key !== 'erfNo'" type="button" class="lookup-button" :disabled="!dev" @click="toggleLookup(field.key)">...</button></label>
        <label class="date-field create-date"><input v-model="dateChecked.createdAt" type="checkbox" :disabled="!dev" @change="setDateEnabled('createdAt')">Create Date<input v-model="dates.createdAt.from" type="datetime-local" step="1" class="mes-input yellow" :disabled="!dev"><span>To</span><input v-model="dates.createdAt.to" type="datetime-local" step="1" class="mes-input yellow" :disabled="!dev"></label>
        <label class="date-field release-date"><input v-model="dateChecked.releaseAt" type="checkbox" :disabled="!dev" @change="setDateEnabled('releaseAt')">Release Date<input v-model="dates.releaseAt.from" type="datetime-local" step="1" class="mes-input yellow" :disabled="!dev"><span>To</span><input v-model="dates.releaseAt.to" type="datetime-local" step="1" class="mes-input yellow" :disabled="!dev"></label>
        <button type="button" class="mes-button query-button" :disabled="!dev" @click="query">Query (Q)</button>
      </div>
      <div v-if="dev && lookupField" class="lookup-options"><button v-for="option in lookupOptions" :key="option" type="button" @click="chooseLookup(option)">{{ option }}</button></div>
    </fieldset>
    <fieldset class="result-frame"><legend>Result</legend><div class="result-main"><div class="grid-wrap"><table><thead><tr><th v-for="column in columns" :key="column[0]">{{ column[1] }}</th></tr></thead><tbody><tr v-for="row in rows" :key="row.erfNo" :class="{active: activeNo === row.erfNo}" tabindex="0" @click="dev && (activeNo = row.erfNo)" @keydown.enter="dev && (activeNo = row.erfNo)"><td v-for="column in columns" :key="column[0]">{{ formatCell(row,column[0]) }}</td></tr><tr v-if="!rows.length"><td :colspan="columns.length" class="empty">{{ !dev ? unavailableText : searched ? '沒有符合條件的異常單。' : '請輸入條件後按 Query。' }}</td></tr></tbody></table></div><div class="actions"><div><button type="button" class="mes-button" :disabled="!dev || !active" @click="openInfo">ERF Info</button><button type="button" class="mes-button" :disabled="!dev || !active" @click="open8d">8D</button></div><button type="button" class="mes-button" @click="emit('close')">Close (X)</button></div></div><div class="status-bar"><span>UserNo: {{ props.currentUserNo || (dev ? 'DEV' : '—') }}</span><span>資料總筆數 {{ rows.length }}</span><span>{{ new Date().toLocaleString('zh-TW') }}</span></div></fieldset>
    <div v-if="dev && dialog && active" class="dialog-backdrop" @click.self="closeDialog"><div class="dialog" role="dialog" aria-modal="true" :aria-label="dialog === 'info' ? 'ERF Information' : '8D'">
      <header><strong>{{ dialog === 'info' ? 'ERF Information' : '8D' }} · {{ active.erfNo }}</strong><button type="button" class="mes-button" @click="closeDialog">Close</button></header>
      <template v-if="dialog === 'info'"><fieldset><legend>ERFBasis</legend><div class="detail-grid"><span>ERF No: {{ active.erfNo }}</span><span>Lot No: {{ active.lotNo }}</span><span>Product No: {{ active.productNo }}</span><span>OP No: {{ active.opNo }}</span><span>Customer No: {{ active.customerNo }}</span><span>MClass No: {{ active.mClassNo }}</span><span>Cur GroupNo: {{ active.curGroupNo }}</span><span>ERF Type: {{ active.erfType ?? (dev ? 'DEV' : '—') }}</span><span>Rule No: {{ active.ruleNo || '—' }}</span><span>Creator: {{ active.creator || (dev ? 'DEV' : '—') }}</span><span>Create Date: {{ formatCell(active,'createdAt') }}</span><span>ERF Record Qty: {{ formatCell(active,'erfRecordQty') }}</span><span class="wide">Hold Description: {{ active.holdDescription }}</span></div></fieldset><fieldset><legend>ERF Detail</legend><div class="tabs"><button v-for="tab in detailTabs" :key="tab" type="button" class="mes-button" :class="{selected: infoTab === tab}" @click="selectInfoTab(tab)">{{ tab }}</button></div><div class="tab-body"><div v-if="infoTab !== 'ERF Attach'" class="detail-table-wrap"><table class="detail-table"><thead><tr><th v-for="column in currentDetailColumns" :key="column">{{ column }}</th></tr></thead><tbody><tr v-for="(row, index) in detailRows" :key="index"><td v-for="column in currentDetailColumns" :key="column">{{ formatDetailCell(row, column) }}</td></tr><tr v-if="!detailRows.length"><td :colspan="currentDetailColumns.length || 1">無資料</td></tr></tbody></table></div><template v-else><div v-for="source in attachmentSources" :key="source" class="attachment-group"><strong>{{ source }} Attach</strong><div class="detail-table-wrap"><table class="detail-table"><thead><tr><th>AttachName</th><th>Attach File</th><th>DelAttachFile</th><th>Creator</th><th>CreateDate</th><th v-if="source === 'OCAP'">OCAPNO</th></tr></thead><tbody><tr v-for="row in attachmentRows.filter(item => item.source === source)" :key="row.name + row.ocapNo"><td>{{ row.name }}</td><td><button type="button" class="mes-button" :disabled="!dev" @click="viewAttachment(row)">View</button></td><td><button type="button" class="mes-button" :disabled="!dev || isGeneratedErfFile(row.name)" @click="requestDeleteAttachment(row)">Delete</button></td><td>{{ row.creator }}</td><td>{{ row.createdAt }}</td><td v-if="source === 'OCAP'">{{ row.ocapNo }}</td></tr><tr v-if="!attachmentRows.some(item => item.source === source)"><td :colspan="source === 'OCAP' ? 6 : 5">無附件</td></tr></tbody></table></div></div><p v-if="infoMessage" class="info-message" role="status">{{ infoMessage }}</p><div v-if="dev && pendingDeleteAttachment" class="delete-confirm" role="alertdialog" aria-label="確認刪除假附件"><span>確定從 DEV 假資料移除 {{ pendingDeleteAttachment.name }}？</span><button type="button" class="mes-button" :disabled="!dev" @click="confirmDeleteAttachment">確定</button><button type="button" class="mes-button" @click="pendingDeleteAttachment = null">取消</button></div></template></div></fieldset></template>
      <template v-else><p>已關聯 8D：{{ active.qrsNo }}。原畫面會依 IssueState 開啟 WP_8DMaintainDef。</p><p>WP802 的維護交易尚待獨立移植；此處僅顯示 DEV 關聯資訊。</p></template>
    </div></div>
  </section>
</template>

<style scoped>
.wp508-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:7px;box-sizing:border-box;padding:9px;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif}.dev-banner{padding:6px 9px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}h1{margin:0;font-size:18px}h1 small{font-size:12px;font-weight:400;color:#777}.notice{margin:0;min-height:18px}.notice.error{padding:5px 8px;border:1px solid #d6aaa6;background:#fff0ef;color:#99251e}fieldset{position:relative;min-width:0;margin:0;padding:7px 8px;border:1px solid #bbb;background:#fff}legend{padding:0 5px}.query-grid{display:grid;grid-template-columns:325px 325px minmax(10px,1fr) 90px;grid-template-areas:'lot mclass . .' 'op group . .' 'customer erf . .' 'create create . .' 'release release . query';gap:5px 8px;align-items:center}.query-grid>label{display:flex;align-items:center;gap:4px;white-space:nowrap}.field-lotNo{grid-area:lot}.field-opNo{grid-area:op}.field-customerNo{grid-area:customer}.field-mClassNo{grid-area:mclass}.field-curGroupNo{grid-area:group}.field-erfNo{grid-area:erf}.create-date{grid-area:create}.release-date{grid-area:release}.query-button{grid-area:query}.query-grid label>.mes-input{width:135px;margin-left:auto}.query-grid .date-field>.mes-input{width:175px;margin-left:5px}.percent{color:#777}.mes-input{height:26px;min-width:0;box-sizing:border-box;padding:3px 5px;border:1px solid #888;font:inherit}.yellow{background:#fff0b3!important}.mes-button,.lookup-button{min-height:29px;padding:3px 10px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#f2f2f2);color:#333;font:inherit;cursor:pointer}.lookup-button{min-width:27px;padding:1px}.mes-button:disabled,.lookup-button:disabled{color:#999;cursor:default}.lookup-options{position:absolute;z-index:5;top:100%;left:10px;display:flex;gap:5px;padding:7px;border:1px solid #aaa;background:#fffbe9;box-shadow:0 3px 9px #0002}.lookup-options button{padding:3px 7px;border:1px solid #bbb;background:#fff;cursor:pointer}.result-frame{flex:1;min-height:200px;display:flex;flex-direction:column}.result-main{display:flex;flex:1;min-height:0;gap:9px}.grid-wrap{flex:1;min-width:0;overflow:auto;border:1px solid #aaa}.grid-wrap table{border-collapse:collapse;white-space:nowrap}.grid-wrap th{position:sticky;top:0;padding:5px 7px;border:1px solid #d4d4d4;background:#ead591;text-align:left;font-weight:500}.grid-wrap td{padding:4px 6px;border:1px solid #d4d4d4;background:#ffe89c}.grid-wrap tr.active td{background:#f4ca68}.grid-wrap tr{cursor:pointer}.grid-wrap th:first-child,.grid-wrap td:first-child{position:sticky;left:0;z-index:1}.grid-wrap th:nth-child(2),.grid-wrap td:nth-child(2){position:sticky;left:115px;z-index:1}.grid-wrap th:first-child,.grid-wrap th:nth-child(2){z-index:3}.grid-wrap th:first-child,.grid-wrap td:first-child{min-width:115px}.grid-wrap th:nth-child(2),.grid-wrap td:nth-child(2){min-width:115px}.empty{text-align:center!important;background:#fff!important;color:#777;height:33px}.actions{display:flex;flex-direction:column;justify-content:space-between}.actions>div{display:flex;flex-direction:column;gap:6px}.actions .mes-button{width:90px}.status-bar{display:flex;justify-content:space-between;padding:4px;color:#666}.dialog-backdrop{position:fixed;inset:0;z-index:2000;display:flex;justify-content:center;align-items:center;background:#0006}.dialog{width:min(900px,94vw);max-height:88vh;overflow:auto;padding:13px;background:#f7f5ee;border:1px solid #888;box-shadow:0 10px 32px #0004}.dialog header{display:flex;justify-content:space-between;align-items:center;margin-bottom:9px}.dialog fieldset{margin:8px 0}.detail-grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:8px}.detail-grid .wide{grid-column:1/-1}.tabs{display:flex;flex-wrap:wrap;gap:5px}.tabs .selected{background:#ffe89c}.tab-body{min-height:70px;padding:8px;border:1px solid #ccc}.tab-body p{margin:4px 0}.detail-table-wrap{overflow:auto}.detail-table{width:100%;border-collapse:collapse;white-space:nowrap}.detail-table th,.detail-table td{padding:5px 7px;border:1px solid #ccc;text-align:left}.detail-table th{background:#ead591}.detail-table td{background:#fff3c8}.attachment-group{margin:5px 0 12px}.delete-confirm{display:flex;align-items:center;gap:8px;padding:6px;background:#fff0cb;border:1px solid #d2bd83}.info-message{padding:6px;background:#fff8dc}@media(max-width:950px){.query-grid{grid-template-columns:repeat(2,minmax(0,1fr));grid-template-areas:'lot mclass' 'op group' 'customer erf' 'create create' 'release release' 'query query'}.query-button{justify-self:end}.query-grid .date-field>.mes-input{width:140px}}
</style>
