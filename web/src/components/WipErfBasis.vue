<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'

type ErfRow = Record<string, string | number | null | string[]> & {
  erfNo: string; lotNo: string; opNo: string; customerNo: string; mClassNo: string; curGroupNo: string;
  createdAt: string; releaseAt: string | null; status: number; erfSource: number;
  batchLotNos: string[]; qrsNo: string; qrsIssueState: number; holdItems: string[]
}
type FilterKey = 'lotNo' | 'opNo' | 'customerNo' | 'mClassNo' | 'curGroupNo' | 'erfNo'
type LookupKey = 'opNo' | 'customerNo' | 'mClassNo' | 'curGroupNo'
type DateKey = 'createdAt' | 'releaseAt'
type DetailRecord = Record<string, string | number>
type MockAttachment = { source: 'ERF' | 'OCAP'; name: string; attachType: string | number; creator: string; createdAt: string; ocapNo: string; lotNo?: string; ocapVersion?: string; ocapNodeNo?: string }
type ErfInfoPayload = {
  createHoldCodes: { componentNo: string; areaNo: string; holdItemNo: string; holdItemName: string | null; recordQty: number; description: string | null; ocapNo: string | null }[]
  holdCodeDispositions: { componentNo: string; areaNo: string; holdItemNo: string; holdItemName: string | null; recordQty: number; description: string | null; holdDisposition: string | null }[]
  erfDispositions: { dispOrder: number; groupNo: string | null; dispDescription: string | null; contactorName: string | null; nextDispType: string | null; nextGroupNo: string | null; creator: string | null; createDate: string | null; ocapNo: string | null; holdItemNo: string | null; ocapNodeNo: string | null; dispUserNo: string | null; dispDate: string | null }[]
  lotDispositions: { lotDispOrder: number; lotDispType: number; description: string | null; creator: string | null; createDate: string | null; opNo: string | null; processNo: string | null; processVersion: string | null }[]
  attachments: { source: 'ERF' | 'OCAP'; lotNo: string; name: string; attachType: string; creator: string | null; createdAt: string | null; ocapNo: string | null; ocapVersion: string | null; ocapNodeNo: string | null }[]
}

const props = defineProps<{ developmentPreview?: boolean; currentUserNo?: string; initialErfNo?: string; functionVariant?: 'wip' | 'spc'; apiBaseUrl?: string }>()
const emit = defineEmits<{ close: []; create8d: [erf: { erfNo: string; lotNo: string; productNo: string; opNo: string; customerNo: string; lotQty: number; errorQty: number; holdDescription: string }]; open8dMaintain: [qrsNo: string] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview !== false)
const spcMode = props.functionVariant === 'spc'
const unavailableText = '正式環境尚未開放此功能。'
const filters = reactive<Record<FilterKey, string>>({ lotNo: '', opNo: '', customerNo: '', mClassNo: '', curGroupNo: '', erfNo: '' })
const checked = reactive<Record<FilterKey, boolean>>({ lotNo: false, opNo: false, customerNo: false, mClassNo: false, curGroupNo: false, erfNo: false })
const dateChecked = reactive<Record<DateKey, boolean>>({ createdAt: false, releaseAt: false })
const dates = reactive<Record<DateKey, { from: string; to: string }>>({ createdAt: { from: '', to: '' }, releaseAt: { from: '', to: '' } })
const lookupField = ref<LookupKey | null>(null)
const lookupOptions = computed(() => dev.value && lookupField.value ? [...new Set(fixtures.filter(row => spcMode ? [1, 2].includes(row.erfSource) : row.erfSource === 0).map(row => String(row[lookupField.value!] || '')).filter(Boolean))].sort() : [])
const rows = ref<ErfRow[]>([]); const activeNo = ref(''); const searched = ref(false)
const loading = ref(false); const page = ref(1); const pageSize = 100; const totalCount = ref(0); const pageCount = ref(0)
let queryVersion = 0
let queryController: AbortController | null = null
const active = computed(() => rows.value.find(row => row.erfNo === activeNo.value) || null)
const dialog = ref<'info' | '8d' | null>(null); const infoTab = ref('Create Hold Code')
const infoMessage = ref(''); const pendingDeleteAttachment = ref<MockAttachment | null>(null)
const formalInfo = ref<ErfInfoPayload | null>(null); const infoLoading = ref(false)
let infoController: AbortController | null = null
const initialMessage = '請至少選一個查詢條件，再按 Query (Q)。'
const errorText = ref(''); const message = ref(dev.value ? initialMessage : '正式唯讀查詢，請輸入條件後按 Query。')
function unavailable(): boolean { if (dev.value) return false; errorText.value = ''; message.value = unavailableText; return true }
const statusLabels: Record<number, string> = { 0: 'ERF Undisposition', 1: 'ERF Wait Disposition', 2: 'Notice Customer and Wait Disposition', 3: 'Waive', 11: 'Wait Lot Disposition', 12: 'Abort', 21: 'Release', 31: 'Hold Bank' }
const spcStatusLabels: Record<number, string> = { 0: 'ERF Undisposition', 1: 'ERF Wait Disposition', 2: 'Notice Customer and Wait Disposition', 11: 'Wait Lot Disposition', 12: 'Abort', 21: 'Release' }
const filterFields: { key: FilterKey; label: string }[] = [
  { key: 'lotNo', label: 'Lot No' }, { key: 'opNo', label: 'OP No' }, { key: 'customerNo', label: 'Customer No' },
  { key: 'mClassNo', label: 'MClass No' }, { key: 'curGroupNo', label: 'Cur Group' }, { key: 'erfNo', label: 'ERF No' },
]
const columns = [
  ['erfNo','ERF No'],['lotNo','Lot No'],['productNo','Product No'],['productName','Product Name'],['customerLotNo','Customer Lot No'],['status','Status'],['opNo','OPNo'],['customerNo','CustomerNo'],['customerName','Customer Name'],['mClassNo','MClassNo'],['holdDescription','HoldDescription'],['curGroupNo','CurGroupNo'],['dispDescription','DispDescription'],['ruleNo','RuleNo'],['createQty','CreateQty'],['createdAt','Create Date'],['erfRecordQty','ERF Record Qty'],['curQty','CurQty'],['releaseQty','Release Qty'],['releaser','Release UserNo'],['releaseAt','Release Date'],['calHoldTime','Cal Hold Time'],['priority','Priority'],['batchLot','BatchLot'],['qrsNo','QRSNo'],
] as const
const spcColumns = [
  ['erfNo','ERF No'],['lotNo','Lot No'],['customerLotNo','Customer Lot No'],['productNo','Product No'],['status','Status'],['opNo','OPNo'],['customerNo','CustomerNo'],['vendorNo','Vendor No'],['materialNo','Material No'],['mClassNo','MClassNo'],['holdDescription','HoldDescription'],['curGroupNo','CurGroupNo'],['dispDescription','DispDescription'],['ruleNo','RuleNo'],['createQty','CreateQty'],['scrapQty','ScrapQty'],['releaseQty','Release Qty'],['releaser','Release UserNo'],['releaseAt','Release Date'],['calHoldTime','Cal Hold Time'],['priority','Priority'],['qrsNo','QRSNo'],
] as const
const resultColumns = spcMode ? spcColumns : columns
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
const erfDispositionOcapColumns = ['OCAPNO', 'HoldItemNo', 'OCAPNodeNo', 'DispUserNo', 'DispDate']
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
  'DEV-SPC-ERF-401': {
    'Create Hold Code': [{ ComponentNo: 'N/A', AreaNo: 'IQC', HoldItemNo: 'H-SPC-01', HoldItemName: '量測超規', RECORDQTY: 2, Description: 'SPC 量測超出管制上限', OCAPNO: '' }],
    'Hold Code Disposition': [], 'ERF Disposition': [], 'Lot Disposition': [],
  },
  'DEV-SPC-ERF-402': {
    'Create Hold Code': [{ ComponentNo: 'N/A', AreaNo: 'IQC', HoldItemNo: 'H-SPC-02', HoldItemName: '來料批次異常', RECORDQTY: 2, Description: '進料抽驗不良已解除', OCAPNO: '' }],
    'Hold Code Disposition': [{ ComponentNo: 'N/A', AreaNo: 'IQC', HoldItemNo: 'H-SPC-02', HoldItemName: '來料批次異常', RECORDQTY: 2, Description: '完成供應商複驗', HoldDisposition: '解除' }],
    'ERF Disposition': [{ DispOrder: 1, GroupNo: 'QA-01', DispDescription: '已完成複驗', ContactorName: '', NextDispType: 2, NextGroupNo: '', Creator: 'DEV', CreateDate: '2026/11/23 11:40:00' }],
    'Lot Disposition': [{ LotDispOrder: 1, LotDispType: 0, Description: '完成複驗後放行', Creator: 'DEV', CreateDate: '2026/11/23 11:40:00', OPNo: 'OP-OQC', ProcessNo: 'PROC-OQC', ProcessVersion: '1' }],
  },
  'DEV-SPC-ERF-403': {
    'Create Hold Code': [{ ComponentNo: 'N/A', AreaNo: 'IQC', HoldItemNo: 'H-SPC-03', HoldItemName: '尺寸異常', RECORDQTY: 1, Description: '供應商來料尺寸異常', OCAPNO: '' }],
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
  'DEV-SPC-ERF-401': [{ source: 'ERF', name: 'DEV-SPC-ERF-401.pdf', attachType: 0, creator: 'DEV', createdAt: '2026/11/21 08:10:00', ocapNo: '' }],
  'DEV-SPC-ERF-402': [],
  'DEV-SPC-ERF-403': [{ source: 'ERF', name: 'DEV-SPC-ERF-403.pdf', attachType: 0, creator: 'DEV', createdAt: '2026/11/23 10:45:00', ocapNo: '' }],
}
function seedAttachments(): Record<string, MockAttachment[]> { return Object.fromEntries(Object.entries(attachmentFixtures).map(([key, rows]) => [key, rows.map(row => ({ ...row }))])) }
const attachmentStore = ref<Record<string, MockAttachment[]>>(dev.value ? seedAttachments() : {})
const detailRows = computed<DetailRecord[]>(() => {
  if (dev.value) return detailFixtures[activeNo.value]?.[infoTab.value] || []
  const info = formalInfo.value
  if (!info) return []
  if (infoTab.value === 'Create Hold Code') return info.createHoldCodes.map(row => ({ ComponentNo: row.componentNo, AreaNo: row.areaNo, HoldItemNo: row.holdItemNo, HoldItemName: row.holdItemName || '', RECORDQTY: row.recordQty, Description: row.description || '', OCAPNO: row.ocapNo || '' }))
  if (infoTab.value === 'Hold Code Disposition') return info.holdCodeDispositions.map(row => ({ ComponentNo: row.componentNo, AreaNo: row.areaNo, HoldItemNo: row.holdItemNo, HoldItemName: row.holdItemName || '', RECORDQTY: row.recordQty, Description: row.description || '', HoldDisposition: row.holdDisposition || '' }))
  if (infoTab.value === 'ERF Disposition') return info.erfDispositions.map(row => ({ DispOrder: row.dispOrder, GroupNo: row.groupNo || '', DispDescription: row.dispDescription || '', ContactorName: row.contactorName || '', NextDispType: row.nextDispType || '', NextGroupNo: row.nextGroupNo || '', Creator: row.creator || '', CreateDate: row.createDate || '', OCAPNO: row.ocapNo || '', HoldItemNo: row.holdItemNo || '', OCAPNodeNo: row.ocapNodeNo || '', DispUserNo: row.dispUserNo || '', DispDate: row.dispDate || '' }))
  if (infoTab.value === 'Lot Disposition') return info.lotDispositions.map(row => ({ LotDispOrder: row.lotDispOrder, LotDispType: row.lotDispType, Description: row.description || '', Creator: row.creator || '', CreateDate: row.createDate || '', OPNo: row.opNo || '', ProcessNo: row.processNo || '', ProcessVersion: row.processVersion || '' }))
  return []
})
const hasOcap = computed(() => dev.value
  ? (detailFixtures[activeNo.value]?.['Create Hold Code'] || []).some(row => !!row.OCAPNO)
  : !!formalInfo.value && (formalInfo.value.erfDispositions.some(row => !!row.ocapNo) || formalInfo.value.attachments.some(row => row.source === 'OCAP')))
const currentDetailColumns = computed<string[]>(() => {
  if (infoTab.value === 'ERF Disposition' && hasOcap.value) return [...(detailColumns[infoTab.value] || []), ...erfDispositionOcapColumns]
  if (infoTab.value === 'Create Hold Code' && !hasOcap.value) return (detailColumns[infoTab.value] || []).filter(column => column !== 'OCAPNO')
  return detailColumns[infoTab.value] || []
})
const attachmentRows = computed<MockAttachment[]>(() => dev.value ? attachmentStore.value[activeNo.value] || [] : (formalInfo.value?.attachments || []).map(row => ({ ...row, creator: row.creator || '', createdAt: row.createdAt || '', ocapNo: row.ocapNo || '', ocapVersion: row.ocapVersion || undefined, ocapNodeNo: row.ocapNodeNo || undefined })))
function formatDetailCell(row: DetailRecord, column: string): string | number { const value = row[column]; if (column === 'NextDispType') return nextDispTypeLabels[Number(value)] || value || ''; if (column === 'LotDispType') return lotDispTypeLabels[Number(value)] || value || ''; if ((column === 'CreateDate' || column === 'DispDate') && typeof value === 'string') return value.slice(0,19).replace('T',' '); return value ?? '' }
function isGeneratedErfFile(name: string): boolean { return !!active.value && name.toLocaleUpperCase().includes(`${active.value.erfNo}.`.toLocaleUpperCase()) }
async function viewAttachment(row: MockAttachment): Promise<void> {
  if (dev.value) { infoMessage.value = `DEV 假附件 ${row.name}：沒有真實位元組；未呼叫 API。`; return }
  if (!active.value || !row.lotNo) { infoMessage.value = '附件資料不完整，無法下載。'; return }
  infoMessage.value = ''
  try {
    const base = props.apiBaseUrl?.trim() || ''
    if (!base) throw new Error('目前環境尚未設定 API 位址。')
    const url = new URL(base, window.location.origin)
    if (url.protocol !== 'https:' && url.hostname !== 'localhost' && url.hostname !== '127.0.0.1') throw new Error('附件連線必須使用 HTTPS。')
    url.pathname = `${url.pathname.replace(/\/+$/, '')}/api/wip/erfs/${encodeURIComponent(active.value.erfNo)}/attachments/content`
    url.searchParams.set('erfSource', String(active.value.erfSource)); url.searchParams.set('lotNo', row.lotNo); url.searchParams.set('source', row.source); url.searchParams.set('name', row.name)
    if (row.source === 'OCAP') url.searchParams.set('ocapNo', row.ocapNo)
    const response = await fetch(url, { credentials: 'include', headers: { Accept: 'application/octet-stream' } })
    if (!response.ok) {
      const payload = await response.json().catch(() => ({})) as { detail?: string; title?: string }
      throw new Error(payload.detail || payload.title || `API 回應 ${response.status}`)
    }
    const objectUrl = URL.createObjectURL(await response.blob())
    const anchor = document.createElement('a'); anchor.href = objectUrl; anchor.download = row.name; anchor.style.display = 'none'
    document.body.append(anchor); anchor.click(); anchor.remove(); window.setTimeout(() => URL.revokeObjectURL(objectUrl), 30_000)
    infoMessage.value = `已開始下載 ${row.name}。`
  } catch (cause) { infoMessage.value = cause instanceof Error ? cause.message : '附件下載失敗。' }
}
function requestDeleteAttachment(row: MockAttachment): void {
  if (!dev.value) { infoMessage.value = '正式環境目前僅開放附件下載。'; return }
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
  { erfNo:'DEV-SPC-ERF-401', lotNo:'DEV-SPC-LOT-401', opNo:'OP-IQC', customerNo:'CUS-A', mClassNo:'M-DIMENSION', curGroupNo:'QA-01', createdAt:'2026-11-21T08:10:00', releaseAt:null, status:0, erfSource:1, batchLotNos:[], qrsNo:'', qrsIssueState:0, holdItems:['H-SPC-01 量測超規'], productNo:'PRD-QA-A', productName:'示範 SPC 產品 A', customerLotNo:'CUSLOT-401', customerName:'示範客戶 A', vendorNo:'SUP-A', materialNo:'MTL-RESIN-A', holdDescription:'SPC 量測超出管制上限', dispDescription:'', ruleNo:'RULE-SPC-01', createQty:20, scrapQty:2, curQty:18, releaseQty:0, releaser:'', priority:1, batchLot:'', calHoldTime:125 },
  { erfNo:'DEV-SPC-ERF-404', lotNo:'DEV-SPC-LOT-405', opNo:'OP-IQC', customerNo:'CUS-A', mClassNo:'M-DIMENSION', curGroupNo:'ENG-01', createdAt:'2026-11-21T09:00:00', releaseAt:null, status:1, erfSource:1, batchLotNos:[], qrsNo:'', qrsIssueState:0, holdItems:['H-SPC-04 待工程判定'], productNo:'PRD-QA-A', productName:'示範 SPC 產品 A', customerLotNo:'CUSLOT-405', customerName:'示範客戶 A', vendorNo:'SUP-A', materialNo:'MTL-RESIN-A', holdDescription:'量測偏移待工程判定', dispDescription:'已送工程群組', ruleNo:'RULE-SPC-04', createQty:16, scrapQty:1, curQty:15, releaseQty:0, releaser:'', priority:2, batchLot:'', calHoldTime:205 },
  { erfNo:'DEV-SPC-ERF-405', lotNo:'DEV-SPC-LOT-406', opNo:'OP-OQC', customerNo:'CUS-B', mClassNo:'M-ATTRIBUTE', curGroupNo:'QA-01', createdAt:'2026-11-22T13:15:00', releaseAt:null, status:2, erfSource:2, batchLotNos:[], qrsNo:'', qrsIssueState:0, holdItems:['H-SPC-05 客戶通知'], productNo:'PRD-QA-B', productName:'示範 SPC 產品 B', customerLotNo:'CUSLOT-406', customerName:'示範客戶 B', vendorNo:'SUP-B', materialNo:'MTL-STEEL-B', holdDescription:'收料缺點待客戶確認', dispDescription:'已通知客戶', ruleNo:'RULE-SPC-05', createQty:10, scrapQty:1, curQty:9, releaseQty:0, releaser:'', priority:2, batchLot:'', calHoldTime:310 },
  { erfNo:'DEV-SPC-ERF-406', lotNo:'DEV-SPC-LOT-407', opNo:'OP-IQC', customerNo:'CUS-A', mClassNo:'M-DIMENSION', curGroupNo:'QA-01', createdAt:'2026-11-23T07:20:00', releaseAt:null, status:11, erfSource:1, batchLotNos:[], qrsNo:'', qrsIssueState:0, holdItems:['H-SPC-06 批次處置'], productNo:'PRD-QA-A', productName:'示範 SPC 產品 A', customerLotNo:'CUSLOT-407', customerName:'示範客戶 A', vendorNo:'SUP-A', materialNo:'MTL-RESIN-A', holdDescription:'Lot 待最終處置', dispDescription:'異常單處置完成', ruleNo:'RULE-SPC-06', createQty:10, scrapQty:2, curQty:8, releaseQty:0, releaser:'', priority:1, batchLot:'', calHoldTime:420 },
  { erfNo:'DEV-SPC-ERF-402', lotNo:'DEV-SPC-LOT-402', opNo:'OP-OQC', customerNo:'CUS-B', mClassNo:'M-ATTRIBUTE', curGroupNo:'ENG-01', createdAt:'2026-11-22T09:20:00', releaseAt:'2026-11-23T11:40:00', status:21, erfSource:2, batchLotNos:['DEV-SPC-LOT-402','DEV-SPC-LOT-403'], qrsNo:'', qrsIssueState:0, holdItems:['H-SPC-02 來料批次異常'], productNo:'PRD-QA-B', productName:'示範 SPC 產品 B', customerLotNo:'CUSLOT-402', customerName:'示範客戶 B', vendorNo:'SUP-B', materialNo:'MTL-STEEL-B', holdDescription:'進料抽驗不良已解除', dispDescription:'完成複驗後放行', ruleNo:'RULE-SPC-02', createQty:12, scrapQty:2, curQty:10, releaseQty:10, releaser:'DEV', priority:2, batchLot:'DEV-SPC-LOT-402,DEV-SPC-LOT-403', calHoldTime:1580 },
  { erfNo:'DEV-SPC-ERF-403', lotNo:'DEV-SPC-LOT-404', opNo:'OP-IQC', customerNo:'CUS-A', mClassNo:'M-DIMENSION', curGroupNo:'QA-01', createdAt:'2026-11-23T10:45:00', releaseAt:'2026-11-23T12:00:00', status:12, erfSource:2, batchLotNos:[], qrsNo:'QRS-DEV-SPC-403', qrsIssueState:-1, holdItems:['H-SPC-03 尺寸異常'], productNo:'PRD-QA-A', productName:'示範 SPC 產品 A', customerLotNo:'CUSLOT-404', customerName:'示範客戶 A', vendorNo:'SUP-A', materialNo:'MTL-RESIN-A', holdDescription:'供應商來料尺寸異常', dispDescription:'已作廢', ruleNo:'RULE-SPC-03', createQty:8, scrapQty:1, curQty:7, releaseQty:0, releaser:'QA-01', priority:1, batchLot:'', calHoldTime:75 },
]

function setFilterEnabled(key: FilterKey): void { if (!checked[key]) filters[key] = '' }
function setDateEnabled(key: DateKey): void { if (!dateChecked[key]) { dates[key].from = ''; dates[key].to = '' } }
function chooseLookup(value: string): void { if (unavailable()) return; if (lookupField.value) { filters[lookupField.value] = value; checked[lookupField.value] = true }; lookupField.value = null }
function toggleLookup(key: FilterKey): void { if (unavailable()) return; if (key === 'lotNo' || key === 'erfNo') return; lookupField.value = lookupField.value === key ? null : key }
function dateMatches(value: string | null, range: { from: string; to: string }): boolean {
  if (!value) return false
  const instant = Date.parse(value)
  return (!range.from || instant >= Date.parse(range.from)) && (!range.to || instant <= Date.parse(range.to))
}
function load8dLinks(): Record<string, string> { if (!dev.value) return {}; try { const data = JSON.parse(window.localStorage.getItem(linkKey) || '{}') as unknown; return data && typeof data === 'object' && !Array.isArray(data) ? data as Record<string, string> : {} } catch { return {} } }
function load8dIssueStates(): Record<string, number> { if (!dev.value) return {}; try { const rows = JSON.parse(window.localStorage.getItem('ymes-dev-mock:wip-8d-basis-factory:state:v1') || '[]') as unknown; return Array.isArray(rows) ? Object.fromEntries(rows.filter(row => row && typeof row.qrsNo === 'string' && typeof row.issueState === 'number').map(row => [row.qrsNo, row.issueState])) : {} } catch { return {} } }
async function queryFormal(requestedPage: number): Promise<void> {
  queryVersion += 1
  const version = queryVersion
  queryController?.abort()
  queryController = null
  errorText.value = ''
  const selected: Partial<Record<FilterKey, string>> = {}
  for (const field of filterFields) {
    const value = checked[field.key] ? filters[field.key].trim().toUpperCase() : ''
    filters[field.key] = value
    if (value) selected[field.key] = value
  }
  const dateFilters: Partial<Record<'createdFrom' | 'createdTo' | 'releasedFrom' | 'releasedTo', string>> = {}
  if (!spcMode) {
    for (const key of Object.keys(dateChecked) as DateKey[]) {
      if (!dateChecked[key]) continue
      const range = dates[key]
      if (!range.from || !range.to || !Number.isFinite(Date.parse(range.from)) || !Number.isFinite(Date.parse(range.to)) || Date.parse(range.from) > Date.parse(range.to)) {
        errorText.value = `${key === 'createdAt' ? 'Create Date' : 'Release Date'} 起迄時間不正確。`
        message.value = '請修正日期範圍後重新查詢。'
        return
      }
      dateFilters[key === 'createdAt' ? 'createdFrom' : 'releasedFrom'] = range.from
      dateFilters[key === 'createdAt' ? 'createdTo' : 'releasedTo'] = range.to
    }
  }
  if (!Object.keys(selected).length && !Object.keys(dateFilters).length) {
    rows.value = []; activeNo.value = ''; totalCount.value = 0; pageCount.value = 0; page.value = 1
    searched.value = false; loading.value = false
    errorText.value = '至少輸入一個有效查詢條件。'
    message.value = '請選擇並輸入至少一個查詢條件。'
    return
  }

  const controller = new AbortController()
  queryController = controller
  loading.value = true
  page.value = requestedPage
  message.value = '正在查詢 YMES 異常單…'
  try {
    const base = props.apiBaseUrl?.trim() || ''
    if (!base) throw new Error('目前環境尚未設定 API 位址。')
    const url = new URL(base, window.location.origin)
    if (url.protocol !== 'https:' && url.hostname !== 'localhost' && url.hostname !== '127.0.0.1')
      throw new Error('API 連線必須使用 HTTPS。')
    url.pathname = `${url.pathname.replace(/\/+$/, '')}${spcMode ? '/api/spc/erfs/query' : '/api/wip/erfs/query'}`
    url.search = ''
    for (const [key, value] of Object.entries(selected)) if (value) url.searchParams.set(key, value)
    for (const [key, value] of Object.entries(dateFilters)) if (value) url.searchParams.set(key, value)
    url.searchParams.set('page', String(requestedPage))
    url.searchParams.set('pageSize', String(pageSize))

    const response = await fetch(url, { credentials: 'include', headers: { Accept: 'application/json' }, signal: controller.signal })
    const payload = await response.json().catch(() => ({})) as {
      detail?: string; title?: string; totalCount?: number; page?: number; pageCount?: number; items?: Record<string, unknown>[]
    }
    if (!response.ok) throw new Error(payload.detail || payload.title || `API 回應 ${response.status}`)
    if (version !== queryVersion) return
    rows.value = (payload.items || []).map(item => {
      const batchLot = String(item.batchLot || '')
      const common = {
        erfNo: String(item.erfNo || ''), lotNo: String(item.lotNo || ''),
        productNo: String(item.productNo || ''), productName: String(item.productName || ''), customerLotNo: String(item.customerLotNo || ''),
        status: Number(item.status) || 0, opNo: String(item.opNo || ''), opName: String(item.opName || ''),
        customerNo: String(item.customerNo || ''), customerName: String(item.customerName || ''), mClassNo: String(item.mClassNo || ''),
        holdDescription: String(item.holdDescription || ''), curGroupNo: String(item.curGroupNo || ''),
        dispDescription: String(item.dispDescription || ''), ruleNo: String(item.ruleNo || ''),
        createQty: Number(item.createQty) || 0, scrapQty: Number(item.erfRecordQty ?? item.scrapQty) || 0,
        releaseQty: Number(item.releaseQty) || 0, releaser: String(item.releaser || ''),
        createdAt: String(item.createDate || ''), releaseAt: item.releaseDate ? String(item.releaseDate) : null,
        creator: String(item.creator || ''), erfRecordQty: Number(item.erfRecordQty ?? item.scrapQty) || 0,
        calHoldTimeMinutes: Number(item.calHoldTimeMinutes) || 0, priority: Number(item.priority) || 0,
        qrsNo: String(item.qrsNo || ''), qrsIssueState: 0, batchLotNos: batchLot ? batchLot.split(',').map((lot: string) => lot.trim()).filter(Boolean) : [], batchLot, holdItems: [],
        erfSource: Number(item.erfSource ?? (spcMode ? 1 : 0)), erfType: Number(item.erfType) || 0,
        curQty: Number(item.curQty) || 0,
      }
      return common as ErfRow
    })
    totalCount.value = Number(payload.totalCount) || 0
    page.value = Number(payload.page) || requestedPage
    pageCount.value = Number(payload.pageCount) || 0
    activeNo.value = rows.value[0]?.erfNo || ''
    searched.value = true
    message.value = `查得 ${totalCount.value.toLocaleString()} 筆${spcMode ? ' SPC／收料' : ' WIP'}異常單。`
  } catch (cause) {
    if (version !== queryVersion || (cause instanceof DOMException && cause.name === 'AbortError')) return
    errorText.value = cause instanceof Error ? cause.message : `${spcMode ? 'SC402' : 'WP508'} 查詢失敗。`
    rows.value = []; totalCount.value = 0; pageCount.value = 0; activeNo.value = ''; searched.value = true
    message.value = `${spcMode ? 'SC402' : 'WP508'} 查詢失敗。`
  } finally {
    if (version === queryVersion) loading.value = false
  }
}

async function query(requestedPage = 1): Promise<void> {
  if (!dev.value) { await queryFormal(requestedPage); return }
  if (unavailable()) return;
  errorText.value = ''; lookupField.value = null
  for (const key of Object.keys(filters) as FilterKey[]) filters[key] = filters[key].trim().toUpperCase()
  if (!(Object.keys(filters) as FilterKey[]).some(key => checked[key] && filters[key]) && !(Object.keys(dateChecked) as DateKey[]).some(key => dateChecked[key])) { errorText.value = '請至少選一個查詢條件。'; return }
  for (const key of Object.keys(dateChecked) as DateKey[]) if (dateChecked[key] && (!dates[key].from || !dates[key].to || !Number.isFinite(Date.parse(dates[key].from)) || !Number.isFinite(Date.parse(dates[key].to)) || Date.parse(dates[key].from) > Date.parse(dates[key].to))) { errorText.value = `${key === 'createdAt' ? 'Create Date' : 'Release Date'} 起迄時間不正確。`; return }
  const links = load8dLinks()
  const issueStates = load8dIssueStates()
  const hasSaved8dState = window.localStorage.getItem('ymes-dev-mock:wip-8d-basis-factory:state:v1') !== null
  rows.value = fixtures.filter(row => (spcMode ? [1, 2].includes(row.erfSource) : row.erfSource === 0) &&
    (Object.keys(filters) as FilterKey[]).every(key => {
      if (!checked[key] || !filters[key]) return true
      if (key === 'lotNo') return (row.batchLotNos.length ? row.batchLotNos : [row.lotNo]).some(lotNo => lotNo.includes(filters.lotNo))
      return String(row[key] || '') === filters[key]
    }) &&
    (!dateChecked.createdAt || dateMatches(row.createdAt, dates.createdAt)) &&
    (!dateChecked.releaseAt || dateMatches(row.releaseAt, dates.releaseAt)))
    .map(row => { const candidate = links[row.erfNo] || row.qrsNo; const qrsNo = spcMode && row.qrsNo ? candidate : hasSaved8dState && candidate && !Object.prototype.hasOwnProperty.call(issueStates, candidate) ? '' : candidate; return { ...row, qrsNo, qrsIssueState: qrsNo ? issueStates[qrsNo] ?? row.qrsIssueState : 0 } })
    .sort((a,b) => a.erfNo.localeCompare(b.erfNo))
  activeNo.value = ''; searched.value = true; totalCount.value = rows.value.length; page.value = 1; pageCount.value = rows.value.length ? 1 : 0
  message.value = `查得 ${rows.value.length} 筆${spcMode ? ' SPC／收料' : ' WIP'}異常單。`
}
function formatCell(row: ErfRow, key: string): string | number {
  if (key === 'status') return (spcMode ? spcStatusLabels : statusLabels)[row.status] || ''
  if (key === 'erfRecordQty') return Number(row.scrapQty || 0)
  if (key === 'calHoldTime') return typeof row.calHoldTimeMinutes === 'number' ? `${row.calHoldTimeMinutes} Minutes` : `${Math.max(0,Math.round((Date.parse(row.releaseAt && [12,21].includes(row.status) ? row.releaseAt : new Date().toISOString()) - Date.parse(row.createdAt))/60000))} Minutes`
  if (key === 'createdAt' || key === 'releaseAt') return String(row[key] || '').slice(0,19).replace('T',' ')
  return String(row[key] ?? '')
}
onBeforeUnmount(() => { queryController?.abort(); infoController?.abort() })
function openInfo(): void {
  if (!active.value) return
  infoTab.value = detailTabs[0]; infoMessage.value = ''; pendingDeleteAttachment.value = null; formalInfo.value = null; dialog.value = 'info'
  if (!dev.value) void loadFormalInfo(active.value.erfNo, active.value.erfSource)
}
async function loadFormalInfo(erfNo: string, erfSource: number): Promise<void> {
  infoController?.abort(); const controller = new AbortController(); infoController = controller
  infoLoading.value = true; infoMessage.value = ''
  try {
    const base = props.apiBaseUrl?.trim() || ''
    if (!base) throw new Error('目前環境尚未設定 API 位址。')
    const url = new URL(base, window.location.origin)
    if (url.protocol !== 'https:' && url.hostname !== 'localhost' && url.hostname !== '127.0.0.1') throw new Error('API 連線必須使用 HTTPS。')
    url.pathname = `${url.pathname.replace(/\/+$/, '')}/api/wip/erfs/${encodeURIComponent(erfNo)}/info`; url.search = ''
    url.searchParams.set('source', String(erfSource))
    const response = await fetch(url, { credentials: 'include', headers: { Accept: 'application/json' }, signal: controller.signal })
    const payload = await response.json().catch(() => ({})) as ErfInfoPayload & { detail?: string; title?: string }
    if (!response.ok) throw new Error(payload.detail || payload.title || `API 回應 ${response.status}`)
    if (controller.signal.aborted) return
    formalInfo.value = payload
  } catch (cause) {
    if (controller.signal.aborted) return
    infoMessage.value = cause instanceof Error ? cause.message : 'ERF 詳細資料載入失敗。'
  } finally { if (!controller.signal.aborted) infoLoading.value = false }
}
function closeDialog(): void { infoController?.abort(); infoLoading.value = false; pendingDeleteAttachment.value = null; dialog.value = null }
function selectInfoTab(tab: string): void { infoTab.value = tab; pendingDeleteAttachment.value = null; infoMessage.value = '' }
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
watch(dev, enabled => { if (enabled) { attachmentStore.value = seedAttachments(); errorText.value = ''; message.value = initialMessage } else { rows.value = []; activeNo.value = ''; closeDialog(); attachmentStore.value = {}; searched.value = false; lookupField.value = null; for (const key of Object.keys(filters) as FilterKey[]) { filters[key] = ''; checked[key] = false }; for (const key of Object.keys(dates) as DateKey[]) { dates[key].from = ''; dates[key].to = ''; dateChecked[key] = false }; infoMessage.value = ''; errorText.value = ''; message.value = '正式唯讀查詢，請輸入條件後按 Query。' } })
onMounted(() => {
  if (!dev.value || !props.initialErfNo) return
  checked.erfNo = true; filters.erfNo = props.initialErfNo; query()
  if (rows.value.length) { activeNo.value = rows.value[0].erfNo; openInfo() }
})
</script>

<template>
  <section class="wp508-page" :class="{ 'spc-mode': spcMode }">
    <div v-if="dev" class="dev-banner">{{ spcMode ? 'DEV 假資料。此頁查詢 SPC／收料來源 ERF；ERF Info 與 8D 僅使用既有 DEV 假互動。' : 'DEV 假資料。此頁查詢 WIP 異常單；ERF Info 的附件操作只改本機假資料，8D 為獨立 WP801 入口。' }}</div>
    <h1>{{ spcMode ? 'SPC 異常單查詢' : '異常單資料' }} <small>{{ spcMode ? 'SPC ERF QUERY · SC402' : 'ERF BASIS · WP508' }}</small></h1>
    <p v-if="errorText" class="notice error" role="alert">{{ errorText }}</p><p v-else class="notice" role="status">{{ message }}</p>
    <fieldset class="query-frame"><legend>Query</legend>
      <div class="query-grid">
        <label v-for="field in filterFields" :key="field.key" :class="'field-'+field.key">
          <input v-model="checked[field.key]" type="checkbox" @change="setFilterEnabled(field.key)">
          {{ field.label }}<span v-if="field.key === 'lotNo'" class="percent">%</span>
          <input v-model="filters[field.key]" class="mes-input yellow" @keydown.enter.prevent="query(1)">
          <button v-if="field.key !== 'lotNo' && field.key !== 'erfNo'" type="button" class="lookup-button" :disabled="!dev" @click="toggleLookup(field.key)">...</button>
        </label>
        <label v-if="!spcMode" class="date-field create-date"><input v-model="dateChecked.createdAt" type="checkbox" @change="setDateEnabled('createdAt')">Create Date<input v-model="dates.createdAt.from" type="datetime-local" step="1" class="mes-input yellow" :disabled="!dateChecked.createdAt"><span>To</span><input v-model="dates.createdAt.to" type="datetime-local" step="1" class="mes-input yellow" :disabled="!dateChecked.createdAt"></label>
        <label v-if="!spcMode" class="date-field release-date"><input v-model="dateChecked.releaseAt" type="checkbox" @change="setDateEnabled('releaseAt')">Release Date<input v-model="dates.releaseAt.from" type="datetime-local" step="1" class="mes-input yellow" :disabled="!dateChecked.releaseAt"><span>To</span><input v-model="dates.releaseAt.to" type="datetime-local" step="1" class="mes-input yellow" :disabled="!dateChecked.releaseAt"></label>
        <button type="button" class="mes-button query-button" :disabled="loading" @click="query(1)">{{ loading ? '查詢中…' : 'Query (Q)' }}</button>
      </div>
      <div v-if="dev && lookupField" class="lookup-options"><button v-for="option in lookupOptions" :key="option" type="button" @click="chooseLookup(option)">{{ option }}</button></div>
    </fieldset>
    <fieldset class="result-frame"><legend>Result</legend><div class="result-main">
      <div class="grid-wrap"><table><thead><tr><th v-for="column in resultColumns" :key="column[0]">{{ column[1] }}</th></tr></thead><tbody>
        <tr v-for="row in rows" :key="row.erfNo" :class="{active: activeNo === row.erfNo}" tabindex="0" @click="activeNo = row.erfNo" @keydown.enter="activeNo = row.erfNo"><td v-for="column in resultColumns" :key="column[0]">{{ formatCell(row,column[0]) }}</td></tr>
        <tr v-if="!rows.length"><td :colspan="resultColumns.length" class="empty">{{ searched ? '沒有符合條件的異常單。' : '請輸入條件後按 Query。' }}</td></tr>
      </tbody></table></div>
      <nav v-if="!dev && searched" class="query-pager" aria-label="查詢分頁">
        <button type="button" class="mes-button" :disabled="loading || page <= 1 || pageCount === 0" @click="query(page - 1)">上一頁</button>
        <span v-if="pageCount">第 {{ page }} / {{ pageCount }} 頁（共 {{ totalCount.toLocaleString() }} 筆）</span><span v-else>沒有查詢結果</span>
        <button type="button" class="mes-button" :disabled="loading || page >= pageCount || pageCount === 0" @click="query(page + 1)">下一頁</button>
      </nav>
      <div class="actions"><div><button type="button" class="mes-button" :disabled="!active" @click="openInfo">ERF Info</button><button type="button" class="mes-button" :disabled="!dev || !active" @click="open8d">8D</button></div><button type="button" class="mes-button" @click="emit('close')">Close (X)</button></div>
    </div><div class="status-bar"><span>UserNo: {{ props.currentUserNo || (dev ? 'DEV' : '—') }}</span><span>資料總筆數 {{ !dev ? totalCount : rows.length }}</span><span>{{ new Date().toLocaleString('zh-TW') }}</span></div></fieldset>
    <div v-if="dialog && active" class="dialog-backdrop" @click.self="closeDialog">
      <div class="dialog" role="dialog" aria-modal="true" :aria-label="dialog === 'info' ? 'ERF Information' : '8D'">
        <header><strong>{{ dialog === 'info' ? 'ERF Information' : '8D' }} · {{ active.erfNo }}</strong><button type="button" class="mes-button" @click="closeDialog">Close</button></header>
        <template v-if="dialog === 'info'">
          <fieldset>
            <legend>ERFBasis</legend>
            <div class="detail-grid">
              <span>ERF No: {{ active.erfNo }}</span><span>Lot No: {{ active.lotNo }}</span>
              <span>Product No: {{ active.productNo }}</span><span>OP No: {{ active.opNo }}</span>
              <span>Customer No: {{ active.customerNo }}</span><span>MClass No: {{ active.mClassNo }}</span>
              <span>Cur GroupNo: {{ active.curGroupNo }}</span><span>ERF Type: {{ active.erfType ?? (dev ? 'DEV' : '—') }}</span>
              <span>Rule No: {{ active.ruleNo || '—' }}</span><span>Creator: {{ active.creator || (dev ? 'DEV' : '—') }}</span>
              <span>Create Date: {{ formatCell(active,'createdAt') }}</span><span>ERF Record Qty: {{ formatCell(active,'erfRecordQty') }}</span>
              <span class="wide">Hold Description: {{ active.holdDescription }}</span>
            </div>
          </fieldset>
          <fieldset>
            <legend>ERF Detail</legend>
            <div class="tabs">
              <button v-for="tab in detailTabs" :key="tab" type="button" class="mes-button" :class="{selected: infoTab === tab}" @click="selectInfoTab(tab)">{{ tab }}</button>
            </div>
            <div class="tab-body">
              <p v-if="infoLoading" class="info-message" role="status">正在載入 ERF 詳細資料…</p>
              <p v-else-if="infoMessage && !dev && !formalInfo" class="info-message" role="alert">{{ infoMessage }}</p>
              <div v-else-if="infoTab !== 'ERF Attach'" class="detail-table-wrap">
                <table class="detail-table"><thead><tr><th v-for="column in currentDetailColumns" :key="column">{{ column }}</th></tr></thead>
                  <tbody><tr v-for="(row, index) in detailRows" :key="index"><td v-for="column in currentDetailColumns" :key="column">{{ formatDetailCell(row, column) }}</td></tr>
                    <tr v-if="!detailRows.length"><td :colspan="currentDetailColumns.length || 1">無資料</td></tr></tbody>
                </table>
              </div>
              <template v-else>
                <div v-for="source in attachmentSources" :key="source" class="attachment-group">
                  <strong>{{ source }} Attach</strong>
                  <div class="detail-table-wrap">
                    <table class="detail-table"><thead><tr><th>AttachName</th><th>Attach File</th><th>DelAttachFile</th><th>Creator</th><th>CreateDate</th><th v-if="source === 'OCAP'">OCAPNO</th></tr></thead>
                      <tbody>
                        <tr v-for="row in attachmentRows.filter(item => item.source === source)" :key="row.name + row.ocapNo">
                          <td>{{ row.name }}</td><td><button type="button" class="mes-button" :disabled="!dev && (!formalInfo || infoLoading)" @click="viewAttachment(row)">{{ dev ? 'View' : '下載' }}</button></td>
                          <td><button type="button" class="mes-button" :disabled="!dev || isGeneratedErfFile(row.name)" @click="requestDeleteAttachment(row)">Delete</button></td>
                          <td>{{ row.creator }}</td><td>{{ row.createdAt }}</td><td v-if="source === 'OCAP'">{{ row.ocapNo }}</td>
                        </tr>
                        <tr v-if="!attachmentRows.some(item => item.source === source)"><td :colspan="source === 'OCAP' ? 6 : 5">無附件</td></tr>
                      </tbody>
                    </table>
                  </div>
                </div>
                <p v-if="infoMessage" class="info-message" role="status">{{ infoMessage }}</p>
                <div v-if="dev && pendingDeleteAttachment" class="delete-confirm" role="alertdialog" aria-label="確認刪除假附件">
                  <span>確定從 DEV 假資料移除 {{ pendingDeleteAttachment.name }}？</span>
                  <button type="button" class="mes-button" :disabled="!dev" @click="confirmDeleteAttachment">確定</button>
                  <button type="button" class="mes-button" @click="pendingDeleteAttachment = null">取消</button>
                </div>
              </template>
            </div>
          </fieldset>
        </template>
        <template v-else><p>已關聯 8D：{{ active.qrsNo }}。原畫面會依 IssueState 開啟 WP_8DMaintainDef。</p><p>此處顯示 DEV 關聯資訊；按 8D 可開 WP802 的假資料十頁明細。</p></template>
      </div>
    </div>
  </section>
</template>

<style scoped>
.wp508-page.spc-mode {
  display: flex;
  width: 100%;
  max-width: 100%;
  height: 100%;
  min-width: 0;
  min-height: 0;
  flex-direction: column;
  gap: 8px;
  box-sizing: border-box;
  overflow: auto;
  padding: 10px;
  background: #f7f5ee;
  color: #303030;
  font: 13px Arial, "Microsoft JhengHei", sans-serif;
}
.wp508-page.spc-mode h1 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
}
.wp508-page.spc-mode h1 small {
  color: #777366;
  font-size: 12px;
  font-weight: 400;
}
.wp508-page.spc-mode > .notice {
  margin: 0;
  padding: 6px 8px;
  border: 1px solid #dfd5ad;
  background: #fff9dc;
  color: #5f5740;
}
.wp508-page.spc-mode > .notice.error {
  border-color: #d6aaa6;
  background: #fff0ef;
  color: #99251e;
}
.wp508-page.spc-mode fieldset {
  min-width: 0;
  margin: 0;
  padding: 8px 10px;
  border: 1px solid #d4d1c7;
  background: #fff;
}
.wp508-page.spc-mode legend {
  padding: 0 5px;
  color: #5f5b50;
}
.wp508-page.spc-mode .query-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  grid-template-areas: "lot op customer" "mclass group erf" ". . query";
  align-items: center;
  gap: 8px 10px;
  min-width: 0;
}
.wp508-page.spc-mode .query-grid > label {
  display: grid;
  grid-template-columns: auto max-content minmax(0, 1fr) 30px;
  align-items: center;
  gap: 5px;
  min-width: 0;
  white-space: nowrap;
}
.wp508-page.spc-mode .field-lotNo {
  grid-area: lot;
  grid-template-columns: auto max-content max-content minmax(0, 1fr) !important;
}
.wp508-page.spc-mode .field-opNo { grid-area: op; }
.wp508-page.spc-mode .field-customerNo { grid-area: customer; }
.wp508-page.spc-mode .field-mClassNo { grid-area: mclass; }
.wp508-page.spc-mode .field-curGroupNo { grid-area: group; }
.wp508-page.spc-mode .field-erfNo {
  grid-area: erf;
  grid-template-columns: auto max-content minmax(0, 1fr) !important;
}
.wp508-page.spc-mode .query-grid input:not([type="checkbox"]) {
  width: 100%;
  min-width: 0;
  height: 30px;
  box-sizing: border-box;
  border: 1px solid #c8c5bb;
  border-radius: 3px;
  padding: 4px 7px;
  color: #292821;
  font: inherit;
}
.wp508-page.spc-mode .query-grid input.yellow { background: #fff2bd; }
.wp508-page.spc-mode .query-grid input:disabled { background: #eee; color: #777; }
.wp508-page.spc-mode .query-grid input[type="checkbox"] { margin: 0; accent-color: #b79a19; }
.wp508-page.spc-mode .lookup-button,
.wp508-page.spc-mode .mes-button {
  min-height: 30px;
  border: 1px solid #c8c5bb;
  border-radius: 3px;
  background: #faf9f6;
  color: #39372e;
  font: inherit;
  cursor: pointer;
}
.wp508-page.spc-mode .lookup-button {
  width: 30px;
  min-width: 30px;
  padding: 2px;
}
.wp508-page.spc-mode .lookup-button:disabled,
.wp508-page.spc-mode .mes-button:disabled {
  color: #999;
  cursor: default;
}
.wp508-page.spc-mode .query-button {
  grid-area: query;
  justify-self: end;
  min-width: 88px;
  padding: 4px 12px;
  border-color: #bba228;
  background: #3d3a31;
  color: #fff;
}
.wp508-page.spc-mode .result-frame {
  display: flex;
  flex: 1 1 auto;
  min-width: 0;
  min-height: 150px;
  flex-direction: column;
  overflow: hidden;
}
.wp508-page.spc-mode .result-main {
  display: flex;
  flex: 1 1 auto;
  min-width: 0;
  min-height: 0;
  flex-direction: column;
}
.wp508-page.spc-mode .grid-wrap {
  flex: 1 1 auto;
  min-width: 0;
  min-height: 90px;
  overflow: auto;
  border: 1px solid #e3e0d6;
}
.wp508-page.spc-mode .grid-wrap table {
  width: max-content;
  min-width: 100%;
  border-collapse: collapse;
  white-space: nowrap;
  font-size: 12px;
}
.wp508-page.spc-mode .grid-wrap th,
.wp508-page.spc-mode .grid-wrap td {
  padding: 6px 8px;
  border: 1px solid #e4e1d8;
  text-align: left;
}
.wp508-page.spc-mode .grid-wrap th {
  position: sticky;
  top: 0;
  background: #f3f1ea;
  color: #5e5b50;
  font-weight: 600;
}
.wp508-page.spc-mode .grid-wrap .empty {
  background: #fff;
  color: #888;
  text-align: center;
}
.wp508-page.spc-mode .query-pager {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 10px;
  padding: 8px 0;
}
.wp508-page.spc-mode .actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  padding-top: 8px;
}
.wp508-page.spc-mode .actions > div { display: flex; gap: 6px; }
.wp508-page.spc-mode .status-bar {
  display: flex;
  justify-content: space-between;
  gap: 10px;
  flex-wrap: wrap;
  padding-top: 5px;
  border-top: 1px solid #e4e1d8;
  color: #777366;
  font-size: 11px;
}
@media (max-width: 950px) {
  .wp508-page.spc-mode .query-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
    grid-template-areas: "lot op" "customer mclass" "group erf" "query query";
  }
}
@media (max-width: 600px) {
  .wp508-page.spc-mode { padding: 8px; }
  .wp508-page.spc-mode .query-grid {
    grid-template-columns: minmax(0, 1fr);
    grid-template-areas: "lot" "op" "customer" "mclass" "group" "erf" "query";
  }
  .wp508-page.spc-mode .query-button { justify-self: stretch; }
}

/* WP508 WIP query shares the ERF grid, but also has two date ranges. */
.wp508-page:not(.spc-mode) {
  display: flex;
  width: 100%;
  max-width: 100%;
  height: 100%;
  min-width: 0;
  min-height: 0;
  flex-direction: column;
  gap: 8px;
  box-sizing: border-box;
  overflow: auto;
  padding: 10px;
  background: #f7f5ee;
  color: #303030;
  font: 13px Arial, "Microsoft JhengHei", sans-serif;
}
.wp508-page:not(.spc-mode) h1 { margin: 0; font-size: 18px; font-weight: 600; }
.wp508-page:not(.spc-mode) h1 small { color: #777366; font-size: 12px; font-weight: 400; }
.wp508-page:not(.spc-mode) > .notice {
  margin: 0;
  padding: 6px 8px;
  border: 1px solid #dfd5ad;
  background: #fff9dc;
  color: #5f5740;
}
.wp508-page:not(.spc-mode) > .notice.error { border-color: #d6aaa6; background: #fff0ef; color: #99251e; }
.wp508-page:not(.spc-mode) fieldset {
  min-width: 0;
  margin: 0;
  padding: 8px 10px;
  border: 1px solid #d4d1c7;
  background: #fff;
}
.wp508-page:not(.spc-mode) legend { padding: 0 5px; color: #5f5b50; }
.wp508-page:not(.spc-mode) .query-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  grid-template-areas: "lot op customer" "mclass group erf" "created created created" "release release release" "query query query";
  align-items: center;
  gap: 8px 10px;
  min-width: 0;
}
.wp508-page:not(.spc-mode) .query-grid > label {
  display: grid;
  grid-template-columns: auto max-content minmax(0, 1fr) 30px;
  align-items: center;
  gap: 5px;
  min-width: 0;
  white-space: nowrap;
}
.wp508-page:not(.spc-mode) .query-grid > label.field-lotNo { grid-area: lot; grid-template-columns: auto max-content max-content minmax(0, 1fr); }
.wp508-page:not(.spc-mode) .field-opNo { grid-area: op; }
.wp508-page:not(.spc-mode) .field-customerNo { grid-area: customer; }
.wp508-page:not(.spc-mode) .field-mClassNo { grid-area: mclass; }
.wp508-page:not(.spc-mode) .field-curGroupNo { grid-area: group; }
.wp508-page:not(.spc-mode) .query-grid > label.field-erfNo { grid-area: erf; grid-template-columns: auto max-content minmax(0, 1fr); }
.wp508-page:not(.spc-mode) .create-date { grid-area: created; }
.wp508-page:not(.spc-mode) .release-date { grid-area: release; }
.wp508-page:not(.spc-mode) .query-grid > label.date-field {
  grid-column: 1 / -1;
  grid-template-columns: auto max-content minmax(0, 1fr) max-content minmax(0, 1fr);
}
.wp508-page:not(.spc-mode) .query-grid input:not([type="checkbox"]) {
  width: 100%;
  min-width: 0;
  height: 30px;
  box-sizing: border-box;
  border: 1px solid #c8c5bb;
  border-radius: 3px;
  padding: 4px 7px;
  color: #292821;
  font: inherit;
}
.wp508-page:not(.spc-mode) .query-grid input.yellow { background: #fff2bd; }
.wp508-page:not(.spc-mode) .query-grid input:disabled { background: #eee; color: #777; }
.wp508-page:not(.spc-mode) .query-grid input[type="checkbox"] { margin: 0; accent-color: #b79a19; }
.wp508-page:not(.spc-mode) .lookup-button,
.wp508-page:not(.spc-mode) .mes-button {
  min-height: 30px;
  border: 1px solid #c8c5bb;
  border-radius: 3px;
  background: #faf9f6;
  color: #39372e;
  font: inherit;
  cursor: pointer;
}
.wp508-page:not(.spc-mode) .lookup-button { width: 30px; min-width: 30px; padding: 2px; }
.wp508-page:not(.spc-mode) .lookup-button:disabled,
.wp508-page:not(.spc-mode) .mes-button:disabled { color: #999; cursor: default; }
.wp508-page:not(.spc-mode) .query-button {
  grid-area: query;
  justify-self: end;
  min-width: 88px;
  padding: 4px 12px;
  border-color: #bba228;
  background: #3d3a31;
  color: #fff;
}
.wp508-page:not(.spc-mode) .result-frame {
  display: flex;
  flex: 1 1 auto;
  min-width: 0;
  min-height: 150px;
  flex-direction: column;
  overflow: hidden;
}
.wp508-page:not(.spc-mode) .result-main { display: flex; flex: 1 1 auto; min-width: 0; min-height: 0; flex-direction: column; }
.wp508-page:not(.spc-mode) .grid-wrap { flex: 1 1 auto; min-width: 0; min-height: 90px; overflow: auto; border: 1px solid #e3e0d6; }
.wp508-page:not(.spc-mode) .grid-wrap table { width: max-content; min-width: 100%; border-collapse: collapse; white-space: nowrap; font-size: 12px; }
.wp508-page:not(.spc-mode) .grid-wrap th,
.wp508-page:not(.spc-mode) .grid-wrap td { padding: 6px 8px; border: 1px solid #e4e1d8; text-align: left; }
.wp508-page:not(.spc-mode) .grid-wrap th { position: sticky; top: 0; background: #f3f1ea; color: #5e5b50; font-weight: 600; }
.wp508-page:not(.spc-mode) .grid-wrap .empty { background: #fff; color: #888; text-align: center; }
.wp508-page:not(.spc-mode) .query-pager { display: flex; align-items: center; justify-content: flex-end; gap: 10px; padding: 8px 0; }
.wp508-page:not(.spc-mode) .actions { display: flex; align-items: center; justify-content: space-between; gap: 10px; padding-top: 8px; }
.wp508-page:not(.spc-mode) .actions > div { display: flex; gap: 6px; }
.wp508-page:not(.spc-mode) .status-bar { display: flex; justify-content: space-between; gap: 10px; flex-wrap: wrap; padding-top: 5px; border-top: 1px solid #e4e1d8; color: #777366; font-size: 11px; }
.wp508-page:not(.spc-mode) .dialog-backdrop { position: fixed; inset: 0; z-index: 1200; display: grid; place-items: center; padding: 16px; background: #17161180; }
.wp508-page:not(.spc-mode) .dialog { width: min(920px, 100%); max-height: 90vh; overflow: auto; padding: 16px; border: 1px solid #d4d1c7; border-radius: 8px; background: #fff; box-shadow: 0 18px 48px #0003; }
.wp508-page:not(.spc-mode) .dialog > header { display: flex; align-items: center; justify-content: space-between; gap: 12px; margin-bottom: 12px; }
.wp508-page:not(.spc-mode) .detail-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 8px 14px; }
.wp508-page:not(.spc-mode) .detail-grid .wide { grid-column: 1 / -1; }
.wp508-page:not(.spc-mode) .tabs { display: flex; flex-wrap: wrap; gap: 6px; margin-bottom: 10px; }
.wp508-page:not(.spc-mode) .tabs .selected { border-color: #bba228; background: #fff5c7; }
.wp508-page:not(.spc-mode) .tab-body { min-width: 0; }
.wp508-page:not(.spc-mode) .detail-table-wrap { max-width: 100%; overflow: auto; }
.wp508-page:not(.spc-mode) .detail-table { min-width: 100%; border-collapse: collapse; white-space: nowrap; }
.wp508-page:not(.spc-mode) .detail-table th,
.wp508-page:not(.spc-mode) .detail-table td { padding: 6px 8px; border: 1px solid #e4e1d8; text-align: left; }
.wp508-page:not(.spc-mode) .detail-table th { background: #f3f1ea; }
.wp508-page:not(.spc-mode) .info-message { color: #6c6240; }
.wp508-page:not(.spc-mode) .delete-confirm { display: flex; align-items: center; flex-wrap: wrap; gap: 8px; padding: 10px; border: 1px solid #dfd5ad; background: #fff9dc; }
@media (max-width: 950px) {
  .wp508-page:not(.spc-mode) .query-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
    grid-template-areas: "lot op" "customer mclass" "group erf" "created created" "release release" "query query";
  }
}
@media (max-width: 600px) {
  .wp508-page:not(.spc-mode) { padding: 8px; }
  .wp508-page:not(.spc-mode) .query-grid {
    grid-template-columns: minmax(0, 1fr);
    grid-template-areas: "lot" "op" "customer" "mclass" "group" "erf" "created" "release" "query";
  }
  .wp508-page:not(.spc-mode) .query-button { justify-self: stretch; }
  .wp508-page:not(.spc-mode) .date-field { gap: 4px; font-size: 12px; }
  .wp508-page:not(.spc-mode) .detail-grid { grid-template-columns: minmax(0, 1fr); }
}
</style>
