<script setup lang="ts">
import { computed, ref } from 'vue'

type FormStatus = 0 | 1 | 2 | 3 | 4 | 5
type ModuleType = 0 | 1
type ItemType = 0 | 1 | 2 | 3
type ResultCode = 0 | 1 | 2 | 3 | 4
interface QcItem { qcItemNo: string; name: string; type: ItemType; status: 0 | 1; result: ResultCode; serial: string; note: string }
interface QtyReport { ok: number; defect: number; return: number; rework: number; unknown: number; surplus: number }
interface InProcessForm {
  id: string; qcFormNo: string; productNo: string; productName: string; productVersion: string; moduleType: ModuleType; opNo: string
  lotNo: string; customerNo: string; qty: number; status: FormStatus; callbackType: string; offlineHold: 0 | 1
  creator: string; createDate: string; reviser: string; reviseDate: string; finishTime: string; dataStamp: number; qtyReport: QtyReport; items: QcItem[]
}
interface Draft extends Omit<InProcessForm, 'id' | 'status' | 'callbackType' | 'creator' | 'createDate' | 'reviser' | 'reviseDate' | 'finishTime' | 'dataStamp' | 'qtyReport' | 'items'> {
  id?: string; status?: FormStatus; callbackType?: string; creator?: string; createDate?: string; reviser?: string; reviseDate?: string; finishTime?: string; dataStamp?: number; qtyReport?: QtyReport; items: QcItem[]
}
interface Entity { no: string; name: string; version: string; moduleType: ModuleType; customerNo: string; qcItems: string[] }
interface LotFixture { lotNo: string; entityNo: string; moduleType: ModuleType; opNo: string; customerNo: string; qty: number; callbackType: string; valid: boolean; lotState: 'Queue' | 'Hold' | 'Wait' | 'Run'; opStatus: 0 | 1 }

const props = defineProps<{ developmentPreview: boolean; currentUserNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const statusTabs: Array<{ status: FormStatus; label: string }> = [
  { status: 0, label: '未確認' }, { status: 1, label: '待檢' }, { status: 2, label: '檢驗完成' },
  { status: 3, label: '允收' }, { status: 4, label: '拒收' }, { status: 5, label: '特採' },
]
const statusName = (status: FormStatus): string => statusTabs.find((tab) => tab.status === status)?.label || '未知'
const typeName = (type: ItemType): string => type === 0 ? '計數' : type === 1 ? '計量' : type === 2 ? '群組' : '屬性群組'
const resultName = (result: ResultCode, type: ItemType): string => {
  if (result === 0) return '合格'
  if (result === 1) return type === 1 ? 'HOLD（計量）' : '不合格'
  if (result === 2) return 'WAIT'
  if (result === 3) return type === 0 ? 'HOLD（計數）' : '不合格'
  return '警示'
}
const qcCatalog: QcItem[] = [
  { qcItemNo: 'IPQC-A-SURF-01', name: '表面外觀', type: 0, status: 0, result: 0, serial: '', note: '' },
  { qcItemNo: 'IPQC-V-DIM-02', name: '站中尺寸', type: 1, status: 0, result: 0, serial: '', note: '' },
  { qcItemNo: 'IPQC-G-PROC-03', name: '製程參數群組', type: 2, status: 0, result: 0, serial: '', note: '' },
  { qcItemNo: 'IPQC-AG-ORDER-06', name: '外觀檢查順序群組', type: 3, status: 0, result: 0, serial: '', note: '' },
  { qcItemNo: 'IPQC-A-LABEL-04', name: '標示檢查', type: 0, status: 0, result: 0, serial: '', note: '' },
  { qcItemNo: 'IPQC-V-FUNC-05', name: '站中功能值', type: 1, status: 0, result: 0, serial: '', note: '' },
]
const entities: Entity[] = [
  { no: 'PRD-CHAIR-01', name: '辦公椅 A 款', version: 'A.03', moduleType: 0, customerNo: 'CUS-1001', qcItems: ['IPQC-A-SURF-01', 'IPQC-V-DIM-02', 'IPQC-A-LABEL-04'] },
  { no: 'PRD-DESK-02', name: '升降桌 B 款', version: 'B.01', moduleType: 0, customerNo: 'CUS-1002', qcItems: ['IPQC-V-DIM-02', 'IPQC-G-PROC-03'] },
  { no: 'MOD-CONTROL-07', name: '控制模組', version: 'M.07', moduleType: 1, customerNo: 'CUS-1003', qcItems: ['IPQC-A-SURF-01', 'IPQC-V-FUNC-05'] },
]
const lots: LotFixture[] = [
  { lotNo: 'LOT-P-261123-01', entityNo: 'PRD-CHAIR-01', moduleType: 0, opNo: 'OP-ASSEMBLY-10', customerNo: 'CUS-1001', qty: 120, callbackType: '', valid: true, lotState: 'Run', opStatus: 1 },
  { lotNo: 'LOT-P-261122-03', entityNo: 'PRD-DESK-02', moduleType: 0, opNo: 'OP-COATING-20', customerNo: 'CUS-1002', qty: 64, callbackType: 'SFT', valid: true, lotState: 'Queue', opStatus: 0 },
  { lotNo: 'LOT-M-261121-06', entityNo: 'MOD-CONTROL-07', moduleType: 1, opNo: 'OP-MOD-TEST-05', customerNo: 'CUS-1003', qty: 32, callbackType: '', valid: true, lotState: 'Wait', opStatus: 0 },
]
const emptyQtyReport = (): QtyReport => ({ ok: 0, defect: 0, return: 0, rework: 0, unknown: 0, surplus: 0 })
const itemFor = (key: string, status: 0 | 1 = 0, result: ResultCode = 0, serial = ''): QcItem => ({ ...(qcCatalog.find((row) => row.qcItemNo === key) || qcCatalog[0]), status, result, serial, note: status ? 'DEV 假 SPC 結果' : '' })
const dateTime = (): string => new Date().toLocaleString('sv-SE').replace('T', ' ')
const forms = ref<InProcessForm[]>([
  { id: 'DEV-IPQC-261123-001', qcFormNo: 'DEV-IPQC-261123-001', productNo: 'PRD-CHAIR-01', productName: '辦公椅 A 款', productVersion: 'A.03', moduleType: 0, opNo: 'OP-ASSEMBLY-10', lotNo: 'LOT-P-261123-01', customerNo: 'CUS-1001', qty: 120, status: 0, callbackType: '', offlineHold: 0, creator: 'DEV-USER', createDate: '2026-11-23 08:30:00', reviser: '', reviseDate: '', finishTime: '', dataStamp: 1, qtyReport: emptyQtyReport(), items: [itemFor('IPQC-A-SURF-01'), itemFor('IPQC-V-DIM-02')] },
  { id: 'DEV-IPQC-261123-002', qcFormNo: 'DEV-IPQC-261123-002', productNo: 'MOD-CONTROL-07', productName: '控制模組', productVersion: 'M.07', moduleType: 1, opNo: 'OP-MOD-TEST-05', lotNo: 'LOT-M-261121-06', customerNo: 'CUS-1003', qty: 32, status: 1, callbackType: '', offlineHold: 0, creator: 'DEV-USER', createDate: '2026-11-23 07:50:00', reviser: 'DEV-QC-02', reviseDate: '2026-11-23 08:00:00', finishTime: '', dataStamp: 2, qtyReport: emptyQtyReport(), items: [itemFor('IPQC-A-SURF-01', 1, 0, 'DEV-SPC-011'), itemFor('IPQC-V-FUNC-05')] },
  { id: 'DEV-IPQC-261122-004', qcFormNo: 'DEV-IPQC-261122-004', productNo: 'PRD-DESK-02', productName: '升降桌 B 款', productVersion: 'B.01', moduleType: 0, opNo: 'OP-COATING-20', lotNo: 'LOT-P-261122-03', customerNo: 'CUS-1002', qty: 64, status: 2, callbackType: 'SFT', offlineHold: 1, creator: 'DEV-USER', createDate: '2026-11-22 15:10:00', reviser: '', reviseDate: '', finishTime: '2026-11-22 16:00:00', dataStamp: 3, qtyReport: { ok: 60, defect: 1, return: 1, rework: 1, unknown: 1, surplus: 0 }, items: [itemFor('IPQC-V-DIM-02', 1, 1, 'DEV-SPC-021'), itemFor('IPQC-G-PROC-03', 1, 0, 'DEV-SPC-022')] },
  { id: 'DEV-IPQC-261121-003', qcFormNo: 'DEV-IPQC-261121-003', productNo: 'PRD-CHAIR-01', productName: '辦公椅 A 款', productVersion: 'A.03', moduleType: 0, opNo: 'OP-ASSEMBLY-10', lotNo: 'LOT-P-261123-01', customerNo: 'CUS-1001', qty: 48, status: 3, callbackType: '', offlineHold: 0, creator: 'DEV-USER', createDate: '2026-11-21 10:00:00', reviser: 'DEV-QC-03', reviseDate: '2026-11-21 10:30:00', finishTime: '2026-11-21 10:25:00', dataStamp: 4, qtyReport: emptyQtyReport(), items: [itemFor('IPQC-A-SURF-01', 1, 0, 'DEV-SPC-030')] },
  { id: 'DEV-IPQC-261120-006', qcFormNo: 'DEV-IPQC-261120-006', productNo: 'MOD-CONTROL-07', productName: '控制模組', productVersion: 'M.07', moduleType: 1, opNo: 'OP-MOD-TEST-05', lotNo: 'LOT-M-261121-06', customerNo: 'CUS-1003', qty: 32, status: 4, callbackType: '', offlineHold: 1, creator: 'DEV-USER', createDate: '2026-11-20 13:20:00', reviser: 'DEV-QC-04', reviseDate: '2026-11-20 14:00:00', finishTime: '2026-11-20 13:55:00', dataStamp: 5, qtyReport: emptyQtyReport(), items: [itemFor('IPQC-V-FUNC-05', 1, 1, 'DEV-SPC-041'), itemFor('IPQC-A-SURF-01', 1, 2, 'DEV-SPC-042')] },
  { id: 'DEV-IPQC-261119-008', qcFormNo: 'DEV-IPQC-261119-008', productNo: 'PRD-DESK-02', productName: '升降桌 B 款', productVersion: 'B.01', moduleType: 0, opNo: 'OP-COATING-20', lotNo: 'LOT-P-261122-03', customerNo: 'CUS-1002', qty: 64, status: 5, callbackType: 'SFT', offlineHold: 0, creator: 'DEV-USER', createDate: '2026-11-19 09:40:00', reviser: 'DEV-QC-01', reviseDate: '2026-11-19 11:00:00', finishTime: '2026-11-19 10:50:00', dataStamp: 6, qtyReport: { ok: 58, defect: 2, return: 1, rework: 1, unknown: 2, surplus: 0 }, items: [itemFor('IPQC-V-DIM-02', 1, 1, 'DEV-SPC-051'), itemFor('IPQC-G-PROC-03', 1, 0, 'DEV-SPC-052')] },
])

const activeStatus = ref<FormStatus>(0)
const selectedFormId = ref('DEV-IPQC-261123-001')
const selectedItemNo = ref('')
const search = ref('')
const useCreateDate = ref(false)
const fromDate = ref('2026-11-01')
const toDate = ref('2026-11-23')
const message = ref('DEV 假資料；所有操作只保存在目前頁面。')
const messageKind = ref<'info' | 'success' | 'warning' | 'error'>('info')
const draftMode = ref<'add' | 'edit' | 'copy' | null>(null)
const draft = ref<Draft | null>(null)
const selectedCatalogItem = ref('')
const resultMode = ref(false)
const resultValue = ref<ResultCode>(0)
const resultNote = ref('')
const formNoSequence = ref(20)
const selectedForm = computed(() => forms.value.find((row) => row.id === selectedFormId.value) || null)
const selectedItem = computed(() => selectedForm.value?.items.find((row) => row.qcItemNo === selectedItemNo.value) || null)
const visibleForms = computed(() => forms.value.filter((row) => {
  if (row.status !== activeStatus.value) return false
  const needle = search.value.trim().toLowerCase()
  if (needle && ![row.qcFormNo, row.lotNo, row.productNo, row.productName, row.opNo, row.customerNo].some((value) => String(value || '').toLowerCase().includes(needle))) return false
  if (useCreateDate.value && fromDate.value && row.createDate.slice(0, 10) < fromDate.value) return false
  if (useCreateDate.value && toDate.value && row.createDate.slice(0, 10) > toDate.value) return false
  return true
}))
const availableItems = computed(() => qcCatalog.filter((row) => !draft.value?.items.some((mapped) => mapped.qcItemNo === row.qcItemNo)))
const availableSelectedItems = computed(() => qcCatalog.filter((row) => !selectedForm.value?.items.some((mapped) => mapped.qcItemNo === row.qcItemNo)))
const pendingCount = computed(() => selectedForm.value?.items.filter((row) => row.status === 0).length || 0)
const canDeleteUnstarted = computed(() => selectedForm.value?.items.every((row) => row.status === 0) ?? false)
const qtyTotal = computed(() => {
  const q = selectedForm.value?.qtyReport || emptyQtyReport()
  return Math.round((q.ok + q.defect + q.return + q.rework + q.unknown - q.surplus) * 100000) / 100000
})

function setMessage(text: string, kind: typeof messageKind.value = 'info'): void { message.value = text; messageKind.value = kind }
function setStatus(status: FormStatus): void {
  activeStatus.value = status
  const first = forms.value.find((row) => row.status === status)
  selectedFormId.value = first?.id || ''; selectedItemNo.value = first?.items[0]?.qcItemNo || ''
  selectedCatalogItem.value = ''; resultMode.value = false
}
function selectForm(row: InProcessForm): void { selectedFormId.value = row.id; selectedItemNo.value = row.items[0]?.qcItemNo || ''; selectedCatalogItem.value = ''; resultMode.value = false }
function selectItem(qcItemNo: string): void { selectedItemNo.value = qcItemNo }
function updateFormStatus(form: InProcessForm, status: FormStatus): void {
  form.status = status; form.reviser = props.currentUserNo?.trim() || 'DEV-USER'; form.reviseDate = dateTime(); form.dataStamp += 1
}
function createDraft(mode: 'add' | 'edit' | 'copy', source?: InProcessForm): void {
  const base = source
  const entity = entities.find((row) => row.no === base?.productNo)
  draftMode.value = mode
  draft.value = {
    id: mode === 'edit' ? base?.id : undefined,
    qcFormNo: mode === 'edit' ? (base?.qcFormNo || '') : '',
    callbackType: base?.callbackType || '',
    productNo: base?.productNo || '', productName: base?.productName || '', productVersion: base?.productVersion || '',
    moduleType: base?.moduleType ?? 0, opNo: base?.opNo || '', lotNo: base?.lotNo || '', customerNo: base?.customerNo || '', qty: base?.qty || 0,
    offlineHold: base?.offlineHold || 0, dataStamp: base?.dataStamp || 0,
    items: mode === 'edit' ? (base?.items || []).map((row) => ({ ...row })) : (base?.items || []).map((row): QcItem => ({ ...row, status: 0, result: 0, serial: '', note: '' })),
  }
  if (mode === 'add') draft.value.items = []
  if (mode === 'copy' && entity) draft.value.items = entity.qcItems.map((key) => itemFor(key))
}
function closeDraft(): void { draftMode.value = null; draft.value = null }
function populateFromLot(lotNo: string): void {
  const lot = lots.find((row) => row.lotNo === lotNo)
  const entity = entities.find((row) => row.no === lot?.entityNo)
  if (!draft.value || !lot || !entity) return
  draft.value.lotNo = lot.lotNo; draft.value.productNo = entity.no; draft.value.productName = entity.name
  draft.value.productVersion = entity.version; draft.value.moduleType = lot.moduleType; draft.value.opNo = lot.opNo
  draft.value.customerNo = lot.customerNo; draft.value.qty = lot.qty; draft.value.callbackType = lot.callbackType
  draft.value.items = entity.qcItems.map((key) => itemFor(key))
  setMessage(`已帶入 ${lot.valid ? '有效' : '非有效'} DEV 批號資料與產品 QC Item。`)
}
function onLotChange(event: Event): void { populateFromLot((event.target as HTMLSelectElement).value) }
function onEntityChange(event: Event): void {
  const entity = entities.find((row) => row.no === (event.target as HTMLSelectElement).value)
  if (!draft.value || !entity) return
  draft.value.productNo = entity.no; draft.value.productName = entity.name; draft.value.productVersion = entity.version
  draft.value.moduleType = entity.moduleType; draft.value.customerNo = entity.customerNo; draft.value.items = entity.qcItems.map((key) => itemFor(key))
}
function assignDevFormNo(): void { if (draft.value) draft.value.qcFormNo = `DEV-IPQC-261123-${String(formNoSequence.value++).padStart(3, '0')}` }
function addDraftItem(): void { if (!draft.value || !selectedCatalogItem.value) return; draft.value.items.push(itemFor(selectedCatalogItem.value)); selectedCatalogItem.value = '' }
function removeDraftItem(qcItemNo: string): void { if (draft.value) draft.value.items = draft.value.items.filter((row) => row.qcItemNo !== qcItemNo) }
function saveDraft(): void {
  const current = draft.value
  if (!current) return
  if (!current.qcFormNo.trim()) return setMessage('請先取得 DEV PQC 單號。', 'error')
  if (!current.productNo || !current.opNo || !current.productVersion || !current.lotNo) return setMessage('產品／Module、工序、版本與批號不可空白。', 'error')
  if (!Number.isFinite(Number(current.qty)) || Number(current.qty) <= 0) return setMessage('檢驗數量必須大於 0。', 'error')
  if (draftMode.value === 'edit') {
    const original = forms.value.find((row) => row.id === current.id)
    if (!original || original.dataStamp !== current.dataStamp) return setMessage('DEV DataStamp 已變更，請重新載入後再編輯。', 'error')
    forms.value = forms.value.map((row) => row.id === current.id ? { ...row, ...current, qty: Number(current.qty), reviser: props.currentUserNo?.trim() || 'DEV-USER', reviseDate: dateTime(), dataStamp: row.dataStamp + 1, items: current.items.map((item) => ({ ...item })) } as InProcessForm : row)
    setMessage('未確認 PQC 單已於 DEV 假資料中修改。', 'success')
  } else {
    if (forms.value.some((row) => row.qcFormNo === current.qcFormNo)) return setMessage('DEV PQC 單號已存在。', 'error')
    const created: InProcessForm = { ...current, id: current.qcFormNo, status: 0, callbackType: current.callbackType || '', creator: props.currentUserNo?.trim() || 'DEV-USER', createDate: dateTime(), reviser: '', reviseDate: '', finishTime: '', dataStamp: 1, qtyReport: emptyQtyReport(), qty: Number(current.qty), items: current.items.map((item) => ({ ...item })) }
    forms.value.unshift(created); activeStatus.value = 0; selectedFormId.value = created.id; selectedItemNo.value = created.items[0]?.qcItemNo || ''
    setMessage(`${draftMode.value === 'copy' ? '已複製' : '已新增'}未確認 PQC 單 ${created.qcFormNo}。`, 'success')
  }
  closeDraft()
}
function deleteUnconfirmed(): void {
  const form = selectedForm.value
  if (!form || activeStatus.value !== 0) return
  forms.value = forms.value.filter((row) => row.id !== form.id); selectedFormId.value = visibleForms.value[0]?.id || ''; selectedItemNo.value = ''
  setMessage('已從 DEV 記憶體刪除未確認單及其 QC Item map。', 'success')
}
function confirmSelected(): void {
  const form = selectedForm.value
  if (!form || activeStatus.value !== 0) return
  updateFormStatus(form, 1); activeStatus.value = 1; selectedFormId.value = form.id; selectedItemNo.value = form.items[0]?.qcItemNo || ''
  setMessage('已確認，PQC 單進入待檢。', 'success')
}
function deleteUnstartedWaitForm(): void {
  const form = selectedForm.value
  if (!form || activeStatus.value !== 1 || !canDeleteUnstarted.value) return
  forms.value = forms.value.filter((row) => row.id !== form.id); selectedFormId.value = visibleForms.value[0]?.id || ''; selectedItemNo.value = ''
  setMessage('已刪除尚未開始檢驗的待檢單及 Map。', 'success')
}
function addMapItem(): void { if (!selectedForm.value || selectedForm.value.status !== 0 || !selectedCatalogItem.value) return; selectedForm.value.items.push(itemFor(selectedCatalogItem.value)); selectedCatalogItem.value = '' }
function removeMapItem(qcItemNo: string): void { if (!selectedForm.value || selectedForm.value.status !== 0) return; selectedForm.value.items = selectedForm.value.items.filter((row) => row.qcItemNo !== qcItemNo); selectedItemNo.value = selectedForm.value.items[0]?.qcItemNo || '' }
function beginResult(): void {
  if (!selectedItem.value || selectedItem.value.status !== 0 || activeStatus.value !== 1) return
  resultValue.value = 0; resultNote.value = ''; resultMode.value = true
}
function saveResult(): void {
  const item = selectedItem.value
  const form = selectedForm.value
  if (!item || !form) return
  item.status = 1; item.result = resultValue.value; item.note = resultNote.value.trim() || 'DEV 假 SPC 結果'; item.serial = `DEV-SPC-${String(Date.now()).slice(-6)}`; resultMode.value = false
  if (form.items.length > 0 && form.items.every((row) => row.status === 1)) {
    form.status = 2; form.finishTime = dateTime(); form.dataStamp += 1; activeStatus.value = 2; selectedFormId.value = form.id
    setMessage('所有 QC Item 已完成，PQC 單自動進入檢驗完成。', 'success')
  } else setMessage(`已記錄 ${item.qcItemNo} DEV 結果，尚有 ${pendingCount.value} 項待檢。`, 'success')
}
function holdWaitImpact(form: InProcessForm): string {
  const lot = lots.find((row) => row.lotNo === form.lotNo)
  if (form.offlineHold !== 1 || !lot?.valid) return ''
  const hasHold = form.items.some((item) => (item.type === 0 && item.result === 3) || (item.type === 1 && item.result === 1))
  if (hasHold) return `DEV 假設：批號 ${lot.lotNo} 將觸發 WIP HOLD 分支 (${lot.lotState}, OPStatus=${lot.opStatus})。`
  if (form.items.some((item) => (item.type === 0 || item.type === 1) && item.result === 2)) return `DEV 假設：批號 ${lot.lotNo} 將觸發 WIP WAIT 分支 (${lot.lotState}, OPStatus=${lot.opStatus})。`
  return ''
}
function finalizeForm(status: 3 | 4 | 5): void {
  const form = selectedForm.value
  if (!form || activeStatus.value !== 2) return
  if (form.callbackType === 'SFT' && qtyTotal.value !== form.qty) return setMessage(`SFT 數量回報合計 ${qtyTotal.value}，必須等於檢驗數量 ${form.qty}。`, 'error')
  const impact = status === 4 ? holdWaitImpact(form) : ''
  if (form.callbackType === 'SFT') setMessage(`DEV 已模擬 SFT ${statusName(status)} callback 成功；沒有呼叫外部服務。 ${impact}`, 'success')
  else setMessage(`DEV 已模擬${statusName(status)}。 ${impact}`, impact ? 'warning' : 'success')
  updateFormStatus(form, status); activeStatus.value = status; selectedFormId.value = form.id
}
function copySelected(): void { if (selectedForm.value) createDraft('copy', selectedForm.value) }
</script>

<template>
  <main class="ipqc-page" aria-label="PQC 檢驗單 DEV">
    <header class="ipqc-title"><div><p class="eyebrow">SPC / IN-PROCESS QUALITY CONTROL</p><h1>PQC 檢驗單</h1><p>WinForms SC105 對應頁 · DEV 假資料工作區</p></div><button class="quiet" @click="emit('close')">關閉</button></header>
    <section class="query-panel"><label class="query-search">單號／批號／產品<input v-model="search" type="search" placeholder="輸入關鍵字" /></label><label class="check-label"><input v-model="useCreateDate" type="checkbox" />建立日期</label><label>起日<input v-model="fromDate" type="date" :disabled="!useCreateDate" /></label><label>迄日<input v-model="toDate" type="date" :disabled="!useCreateDate" /></label><button class="primary" @click="setMessage(`已查詢 ${visibleForms.length} 筆 DEV ${statusName(activeStatus)} 單據。`, 'success')">查詢</button><span class="query-source">另可依批號載入條件查詢</span></section>
    <nav class="status-tabs" aria-label="PQC 單狀態"><button v-for="tab in statusTabs" :key="tab.status" :class="{ active: activeStatus === tab.status }" @click="setStatus(tab.status)">{{ tab.label }} <span>{{ forms.filter((row) => row.status === tab.status).length }}</span></button></nav>
    <section class="workspace"><div class="list-pane"><div class="pane-heading"><div><h2>{{ statusName(activeStatus) }}清單</h2><span>{{ visibleForms.length }} 筆</span></div>
      <div v-if="activeStatus === 0" class="actions"><button class="primary" @click="createDraft('add')">新增</button><button :disabled="!selectedForm" @click="createDraft('edit', selectedForm || undefined)">編輯</button><button :disabled="!selectedForm" @click="copySelected">複製</button><button class="danger" :disabled="!selectedForm" @click="deleteUnconfirmed">刪除</button><button class="primary" :disabled="!selectedForm" @click="confirmSelected">確認</button></div>
      <div v-else-if="activeStatus === 1" class="actions"><button class="primary" :disabled="!selectedItem || selectedItem.status === 1" @click="beginResult">SPC 檢驗</button><button class="danger" :disabled="!canDeleteUnstarted" @click="deleteUnstartedWaitForm">刪除未開始單據</button><button :disabled="!selectedForm" @click="copySelected">複製</button></div>
      <div v-else-if="activeStatus === 2" class="actions"><button class="primary" :disabled="!selectedForm" @click="finalizeForm(3)">允收</button><button class="danger" :disabled="!selectedForm" @click="finalizeForm(4)">拒收</button><button class="special" :disabled="!selectedForm" @click="finalizeForm(5)">特採</button><button :disabled="!selectedForm" @click="copySelected">複製</button></div>
      <div v-else class="actions"><button :disabled="!selectedForm" @click="copySelected">複製</button></div>
    </div><div class="table-wrap"><table><thead><tr><th>PQC 單號</th><th>批號</th><th>產品／Module</th><th>版本／工序</th><th>數量</th><th>Module Type</th><th>建立時間</th></tr></thead><tbody>
      <tr v-for="row in visibleForms" :key="row.id" :class="{ selected: row.id === selectedFormId }" tabindex="0" @click="selectForm(row)" @keydown.enter="selectForm(row)"><td class="mono">{{ row.qcFormNo }}</td><td>{{ row.lotNo }}</td><td>{{ row.productNo }}<small>{{ row.productName }}</small></td><td>{{ row.productVersion }}<small>{{ row.opNo }}</small></td><td>{{ row.qty }}</td><td>{{ row.moduleType === 1 ? 'Module Info' : 'Product' }}</td><td>{{ row.createDate }}</td></tr>
      <tr v-if="visibleForms.length === 0"><td colspan="7" class="empty">此狀態沒有符合條件的 DEV 單據。</td></tr>
    </tbody></table></div></div>
    <aside class="detail-pane"><template v-if="selectedForm"><div class="pane-heading"><div><h2>PQC 單明細</h2><span class="status-pill" :data-status="selectedForm.status">{{ statusName(selectedForm.status) }}</span></div><span class="mono">{{ selectedForm.qcFormNo }}</span></div>
      <dl class="form-facts"><div><dt>Product／Module</dt><dd>{{ selectedForm.productNo }} · {{ selectedForm.productName }}</dd></div><div><dt>批號／數量</dt><dd>{{ selectedForm.lotNo }} / {{ selectedForm.qty }}</dd></div><div><dt>產品版本／工序</dt><dd>{{ selectedForm.productVersion }} / {{ selectedForm.opNo }}</dd></div><div><dt>客戶／Module Type</dt><dd>{{ selectedForm.customerNo }} / {{ selectedForm.moduleType === 1 ? 'Module Info' : 'Product' }}</dd></div><div><dt>CallbackType</dt><dd>{{ selectedForm.callbackType || '一般 PQC' }}</dd></div><div><dt>S_OffLineHold</dt><dd>{{ selectedForm.offlineHold ? '啟用' : '停用' }}</dd></div><div><dt>建立人／時間</dt><dd>{{ selectedForm.creator }} · {{ selectedForm.createDate }}</dd></div><div><dt>完成時間</dt><dd>{{ selectedForm.finishTime || '—' }}</dd></div><div><dt>狀態修改人／時間</dt><dd>{{ selectedForm.reviser || '—' }} {{ selectedForm.reviseDate }}</dd></div></dl>
      <div class="item-heading"><div><h3>QC Item 對應</h3><span>{{ selectedForm.items.length }} 項 · {{ pendingCount }} 項待檢</span></div><div v-if="activeStatus === 0" class="map-actions"><select v-model="selectedCatalogItem" aria-label="選擇 QC Item"><option value="">選擇 QC Item</option><option v-for="row in availableSelectedItems" :key="row.qcItemNo" :value="row.qcItemNo">{{ row.qcItemNo }} · {{ row.name }}</option></select><button :disabled="!selectedCatalogItem" @click="addMapItem">加入</button></div></div>
      <div class="item-list"><div v-for="row in selectedForm.items" :key="row.qcItemNo" class="item-row" :class="{ chosen: selectedItemNo === row.qcItemNo }" role="button" tabindex="0" @click="selectItem(row.qcItemNo)" @keydown.enter="selectItem(row.qcItemNo)"><span class="item-state" :data-done="row.status === 1">{{ row.status === 1 ? '完成' : '待檢' }}</span><span class="item-name"><b>{{ row.qcItemNo }}</b><small>{{ row.name }} · {{ typeName(row.type) }}</small></span><span class="item-result">{{ resultName(row.result, row.type) }}</span><button v-if="activeStatus === 0" class="remove" aria-label="移除 QC Item" @click.stop="removeMapItem(row.qcItemNo)">移除</button></div><p v-if="selectedForm.items.length === 0" class="empty">此表單目前沒有 QC Item map。</p></div>
      <section v-if="activeStatus === 1 && selectedItem" class="inspection-card"><h3>選取檢驗項目</h3><p>{{ selectedItem.qcItemNo }} · {{ selectedItem.name }}（{{ typeName(selectedItem.type) }}）</p><p v-if="selectedItem.status === 1" class="result-summary">{{ resultName(selectedItem.result, selectedItem.type) }} · {{ selectedItem.serial }}</p><div v-else-if="resultMode" class="result-editor"><label>DEV 判定<select v-model.number="resultValue"><option :value="0">合格</option><option :value="1">{{ resultName(1, selectedItem.type) }}</option><option :value="2">WAIT</option><option :value="3">{{ resultName(3, selectedItem.type) }}</option><option :value="4">警示</option></select></label><label>備註<input v-model="resultNote" placeholder="可留空" /></label><div><button @click="resultMode = false">取消</button><button class="primary" @click="saveResult">記錄假結果</button></div></div><p v-else class="hint">按「SPC 檢驗」模擬假量測；不連設備或 SPC Service。</p></section>
      <section v-if="activeStatus === 2 && selectedForm.callbackType === 'SFT'" class="inspection-card"><h3>SFT 數量回報</h3><p>Good + Defect + Return + Rework + Unknown − Surplus 必須等於檢驗 Qty {{ selectedForm.qty }}。</p><div class="qty-grid"><label>Good<input v-model.number="selectedForm.qtyReport.ok" type="number" min="0" step="0.001" /></label><label>Defect<input v-model.number="selectedForm.qtyReport.defect" type="number" min="0" step="0.001" /></label><label>Return<input v-model.number="selectedForm.qtyReport.return" type="number" min="0" step="0.001" /></label><label>Rework<input v-model.number="selectedForm.qtyReport.rework" type="number" min="0" step="0.001" /></label><label>Unknown<input v-model.number="selectedForm.qtyReport.unknown" type="number" min="0" step="0.001" /></label><label>Surplus<input v-model.number="selectedForm.qtyReport.surplus" type="number" min="0" step="0.001" /></label></div><p :class="{ balanced: qtyTotal === selectedForm.qty, unbalanced: qtyTotal !== selectedForm.qty }">回報淨額 {{ qtyTotal }} / {{ selectedForm.qty }}</p></section>
      <section v-if="activeStatus === 2" class="inspection-card"><h3>狀態處置</h3><p>允收／拒收／特採由此處轉換。DEV 可模擬 S_OffLineHold 啟用時的 WIP HOLD／WAIT 分支提示。</p></section>
    </template><div v-else class="empty empty-panel">選取一筆 PQC 單查看 QC Item 與狀態。</div></aside></section>
    <section v-if="draft && draftMode" class="draft-panel"><div class="pane-heading"><div><h2>{{ draftMode === 'add' ? '新增' : draftMode === 'edit' ? '編輯' : '複製' }} PQC 檢驗單</h2><span>來源欄位依 frmIPQCFormDef；所有資料都是 DEV fixture</span></div><button @click="closeDraft">關閉</button></div>
      <div class="draft-grid"><label>檢驗單號<div class="inline-field"><input v-model="draft.qcFormNo" :readonly="draftMode !== 'edit'" /><button v-if="draftMode !== 'edit'" @click="assignDevFormNo">取得 DEV 單號</button></div></label><label>批號<select v-model="draft.lotNo" @change="onLotChange"><option value="">選擇有效 DEV 批號</option><option v-for="lot in lots" :key="lot.lotNo" :value="lot.lotNo">{{ lot.lotNo }} · {{ lot.entityNo }}</option></select></label><label>資料來源<select v-model.number="draft.moduleType"><option :value="0">Product</option><option :value="1">Module Info</option></select></label>
        <label>產品／Module<select v-model="draft.productNo" @change="onEntityChange"><option value="">選擇產品或 Module</option><option v-for="entity in entities.filter((row) => row.moduleType === draft?.moduleType)" :key="entity.no" :value="entity.no">{{ entity.no }} · {{ entity.name }}</option></select></label><label>名稱<input v-model="draft.productName" /></label><label>產品／Module 版本<input v-model="draft.productVersion" /></label><label>OPNo<input v-model="draft.opNo" /></label><label>客戶<input v-model="draft.customerNo" /></label><label>Qty<input v-model.number="draft.qty" type="number" min="0" step="0.001" /></label><label class="hold-option"><input v-model.number="draft.offlineHold" type="checkbox" :true-value="1" :false-value="0" />檢驗異常自動暫停生產批（SEMI_WAFER）</label>
      </div><div class="item-heading"><div><h3>初始 QC Item</h3><span>產品／Module 與工序載入對應項目</span></div><div class="map-actions"><select v-model="selectedCatalogItem"><option value="">選擇 QC Item</option><option v-for="row in availableItems" :key="row.qcItemNo" :value="row.qcItemNo">{{ row.qcItemNo }} · {{ row.name }}</option></select><button @click="addDraftItem">加入</button></div></div>
      <div class="draft-items"><span v-for="row in draft.items" :key="row.qcItemNo" class="draft-chip">{{ row.qcItemNo }} · {{ row.name }}<button @click="removeDraftItem(row.qcItemNo)">×</button></span><span v-if="draft.items.length === 0" class="hint">尚未載入品管項目</span></div><div class="draft-footer"><span>來源 Confirm 不檢查 Map 數量；DEV 也保留這個行為。</span><button @click="closeDraft">取消</button><button class="primary" @click="saveDraft">儲存 DEV 單據</button></div>
    </section><footer class="status-message" :data-kind="messageKind"><span>{{ message }}</span><span>本頁記憶體假資料 · DataStamp {{ selectedForm?.dataStamp ?? '—' }}</span></footer>
  </main>
</template>

<style scoped>
.ipqc-page { min-height:100%; padding:14px; box-sizing:border-box; color:#292820; background:#f4f2e9; font:inherit; display:flex; flex-direction:column; gap:10px; }
.ipqc-title,.pane-heading,.item-heading,.draft-footer { display:flex; align-items:center; justify-content:space-between; gap:12px; }.ipqc-title { padding:12px 14px; background:#fffdf7; border:1px solid #d8d4c7; border-radius:5px; }.ipqc-title h1,.pane-heading h2,.item-heading h3,.inspection-card h3 { margin:0; font-size:16px; }.ipqc-title p,.pane-heading span,.item-heading span,.inspection-card p { margin:4px 0 0; color:#777366; font-size:12px; }.eyebrow { margin:0 0 4px!important; color:#827510!important; font-size:10px!important; letter-spacing:.12em; }
.query-panel { display:flex; align-items:end; gap:8px; flex-wrap:wrap; padding:10px 12px; border:1px solid #d8d4c7; background:#fffdf7; border-radius:4px; }.query-panel label,.draft-grid label,.result-editor label,.qty-grid label { display:flex; flex-direction:column; gap:4px; color:#625f53; font-size:11px; }.query-search { min-width:220px; flex:1; }.check-label { display:flex!important; flex-direction:row!important; align-items:center; min-height:30px; }.query-source { color:#858173; font-size:11px; padding:0 4px 7px; }
button { min-height:29px; border:1px solid #bdb8aa; border-radius:3px; padding:4px 9px; background:#fff; color:#343228; font:inherit; font-size:12px; cursor:pointer; }button:hover:not(:disabled) { border-color:#817619; background:#fffbea; }button:disabled { opacity:.45; cursor:not-allowed; }.primary { color:#fff; background:#3e3b30; border-color:#3e3b30; }.primary:hover:not(:disabled) { color:#fff; background:#605c45; }.danger { color:#8c3028; }.special { color:#645409; background:#fbf2c9; }.quiet { align-self:flex-start; }
.query-panel input,.draft-grid input,.draft-grid select,.result-editor input,.result-editor select,.map-actions select,.qty-grid input { min-height:30px; border:1px solid #c9c4b5; background:white; border-radius:3px; padding:4px 7px; color:#292820; font:inherit; }.status-tabs { display:flex; gap:2px; padding:0 4px; border-bottom:1px solid #b9b4a5; overflow:auto; }.status-tabs button { border:0; border-radius:4px 4px 0 0; color:#5f5b4e; background:transparent; padding:8px 13px; white-space:nowrap; }.status-tabs button.active { color:#28271f; background:#fffdf7; box-shadow:0 -2px 0 #b6a428 inset; font-weight:600; }.status-tabs span { display:inline-grid; min-width:17px; height:17px; place-items:center; margin-left:3px; border-radius:10px; background:#ebe8dd; font-size:10px; }
.workspace { display:grid; grid-template-columns:minmax(0,1.65fr) minmax(360px,1fr); gap:10px; min-height:350px; flex:1; }.list-pane,.detail-pane,.draft-panel { min-width:0; border:1px solid #d8d4c7; border-radius:4px; background:#fffdf7; padding:10px; }.detail-pane { display:flex; flex-direction:column; gap:10px; }.actions,.map-actions { display:flex; gap:5px; flex-wrap:wrap; align-items:center; }.table-wrap { overflow:auto; margin-top:9px; max-height:430px; }table { width:100%; border-collapse:collapse; white-space:nowrap; font-size:11px; }th { position:sticky; top:0; z-index:1; text-align:left; color:#5d594d; background:#efede3; font-weight:600; }th,td { padding:7px 8px; border-bottom:1px solid #e4e1d8; }tbody tr { cursor:pointer; }tbody tr:hover,tbody tr.selected { background:#f8f3cf; }td small { display:block; margin-top:2px; color:#858173; font-size:10px; }.mono { font-family:Consolas,monospace; }.empty { padding:20px; color:#837f71; text-align:center; }.empty-panel { margin:auto; }
.status-pill { display:inline-block; padding:3px 7px; border-radius:10px; background:#ece9de; color:#615d50; }.status-pill[data-status="2"] { background:#f8edb8; }.status-pill[data-status="3"] { background:#dcebd8; }.status-pill[data-status="4"] { background:#f0dcd6; }.status-pill[data-status="5"] { background:#fbf2c9; }.form-facts { display:grid; grid-template-columns:1fr 1fr; gap:6px 12px; margin:0; padding:9px; background:#f6f4eb; border:1px solid #e3dfd3; }.form-facts dt { color:#858173; font-size:10px; }.form-facts dd { overflow-wrap:anywhere; margin:2px 0 0; font-size:11px; }.item-heading { border-bottom:1px solid #dedacd; padding-bottom:6px; }.item-list { display:flex; flex-direction:column; gap:4px; }.item-row { display:flex; width:100%; align-items:center; gap:8px; min-height:43px; padding:5px 7px; border:1px solid #e0ddd3; border-radius:3px; text-align:left; cursor:pointer; }.item-row.chosen { border-color:#a99b34; background:#fbf6d9; }.item-state { color:#766f54; font-size:10px; }.item-state[data-done="true"] { color:#3e7041; }.item-name { flex:1; }.item-name b,.item-name small { display:block; }.item-name b { font-size:11px; }.item-name small { margin-top:2px; color:#7e7a6d; font-size:10px; }.item-result { color:#5e594a; font-size:11px; }.remove { min-height:23px; padding:2px 5px; color:#8c3028; }.inspection-card { padding:9px; border:1px solid #e0ddd3; background:#faf8ef; }.result-summary { padding:7px; border-radius:3px; background:#e8efdf; }.result-editor { display:grid; grid-template-columns:1fr 1fr; gap:8px; margin-top:9px; }.result-editor>div { grid-column:1/-1; display:flex; justify-content:flex-end; gap:6px; }.qty-grid { display:grid; grid-template-columns:repeat(3,1fr); gap:7px; margin-top:8px; }.balanced { color:#3d7042!important; }.unbalanced { color:#9a382e!important; }.hint { color:#858173; font-size:11px; }
.draft-panel { display:flex; flex-direction:column; gap:10px; }.draft-grid { display:grid; grid-template-columns:repeat(3,minmax(0,1fr)); gap:8px; }.inline-field { display:flex; gap:4px; }.inline-field input { min-width:80px; flex:1; }.hold-option { flex-direction:row!important; align-items:center; }.draft-items { display:flex; gap:6px; flex-wrap:wrap; min-height:26px; }.draft-chip { display:inline-flex; align-items:center; gap:5px; padding:4px 7px; border:1px solid #ddd8c8; border-radius:12px; background:#f5f2e8; font-size:11px; }.draft-chip button { min-height:20px; width:20px; padding:0; border:0; font-size:16px; }.draft-footer { justify-content:flex-end; padding-top:8px; border-top:1px solid #e2dfd6; }.draft-footer span { margin-right:auto; color:#858173; font-size:11px; }.status-message { display:flex; justify-content:space-between; gap:10px; padding:8px 10px; border:1px solid #ddd8c8; border-radius:3px; background:#efede4; color:#625f53; font-size:11px; }.status-message[data-kind="success"] { color:#3e7041; }.status-message[data-kind="warning"] { color:#876b00; }.status-message[data-kind="error"] { color:#9a382e; }
@media (max-width:1000px) { .workspace { grid-template-columns:1fr; }.detail-pane { min-height:330px; }.draft-grid { grid-template-columns:repeat(2,minmax(0,1fr)); } }@media (max-width:620px) { .ipqc-page { padding:7px; }.query-search { flex-basis:100%; }.workspace { min-height:0; }.draft-grid,.form-facts { grid-template-columns:1fr; }.pane-heading { align-items:flex-start; flex-direction:column; }.actions { width:100%; }.status-message { flex-direction:column; } }
</style>
