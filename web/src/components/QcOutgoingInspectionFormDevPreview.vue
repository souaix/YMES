<script setup lang="ts">
import { computed, ref } from 'vue'

type FormStatus = 0 | 1 | 2 | 3 | 4
type ItemType = 0 | 1 | 2 | 3
type ResultCode = 0 | 1 | 2 | 3 | 4
interface QcItem { qcItemNo: string; name: string; type: ItemType; status: 0 | 1; result: ResultCode; serial: string; note: string }
interface OqcForm {
  id: string; qcFormNo: string; productNo: string; productName: string; productVersion: string; opNo: string; lotNo: string
  customerNo: string; qty: number; status: FormStatus; callbackType: string; erpSourceNo: string; erpSourceSeq: string
  inspDocLink: string; creator: string; createDate: string; reviser: string; reviseDate: string; finishTime: string
  dataStamp: number; items: QcItem[]
}
interface Draft extends Omit<OqcForm, 'id' | 'status' | 'creator' | 'createDate' | 'reviser' | 'reviseDate' | 'finishTime' | 'items' | 'erpSourceNo' | 'erpSourceSeq'> {
  id?: string; status?: FormStatus; creator?: string; createDate?: string; reviser?: string; reviseDate?: string; finishTime?: string
  erpSourceNo?: string; erpSourceSeq?: string; items: QcItem[]
}
interface Product { no: string; name: string; version: string; customerNo: string; items: string[]; ops: string[] }
interface LotFixture { lotNo: string; productNo: string; opNo: string; qty: number; valid: boolean }

const props = defineProps<{ developmentPreview: boolean; currentUserNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const tabs: Array<{ status: FormStatus; label: string }> = [
  { status: 0, label: '未確認' }, { status: 1, label: '待檢' }, { status: 2, label: '檢驗完成' },
  { status: 3, label: '已接收' }, { status: 4, label: '已拒收' },
]
const statusName = (status: FormStatus): string => tabs.find((tab) => tab.status === status)?.label || '未知'
const itemTypeName = (type: ItemType): string => type === 0 ? '計數' : type === 1 ? '計量' : type === 2 ? '群組' : '屬性群組'
const resultName = (code: ResultCode): string => ['合格', '不合格／Hold', 'Wait', 'Hold', '警示'][code] || '未知'
const itemCatalog: QcItem[] = [
  { qcItemNo: 'OQC-ATT-01', name: '出貨外觀', type: 0, status: 0, result: 0, serial: '', note: '' },
  { qcItemNo: 'OQC-VAR-02', name: '出貨尺寸', type: 1, status: 0, result: 0, serial: '', note: '' },
  { qcItemNo: 'OQC-GRP-03', name: '包裝檢查群組', type: 2, status: 0, result: 0, serial: '', note: '' },
  { qcItemNo: 'OQC-ATT-GRP-04', name: '標籤檢查順序群組', type: 3, status: 0, result: 0, serial: '', note: '' },
]
const products: Product[] = [
  { no: 'PRD-CHAIR-01', name: '辦公椅 A 款', version: 'A.03', customerNo: 'CUS-1001', items: ['OQC-ATT-01', 'OQC-VAR-02', 'OQC-GRP-03'], ops: ['OP-FINAL-90'] },
  { no: 'PRD-DESK-02', name: '升降桌 B 款', version: 'B.01', customerNo: 'CUS-1002', items: ['OQC-VAR-02', 'OQC-ATT-GRP-04'], ops: ['OP-PACK-95'] },
]
const lots: LotFixture[] = [
  { lotNo: 'LOT-O-261123-01', productNo: 'PRD-CHAIR-01', opNo: 'OP-FINAL-90', qty: 48, valid: true },
  { lotNo: 'LOT-O-261122-04', productNo: 'PRD-DESK-02', opNo: 'OP-PACK-95', qty: 64, valid: true },
  { lotNo: 'LOT-O-261120-08', productNo: 'PRD-CHAIR-01', opNo: 'OP-FINAL-90', qty: 12, valid: false },
]
const copyItem = (no: string, status: 0 | 1 = 0, result: ResultCode = 0, serial = ''): QcItem => ({ ...(itemCatalog.find((item) => item.qcItemNo === no) || itemCatalog[0]), status, result, serial, note: status ? 'DEV SPC 假結果' : '' })
const cloneItems = (items: QcItem[], reset = false): QcItem[] => items.map((item) => ({ ...item, status: reset ? 0 : item.status, result: reset ? 0 : item.result, serial: reset ? '' : item.serial, note: reset ? '' : item.note }))
const dateTime = (): string => new Date().toLocaleString('sv-SE').replace('T', ' ')
const forms = ref<OqcForm[]>([
  { id: 'DEV-OQC-001', qcFormNo: 'DEV-OQC-001', productNo: 'PRD-CHAIR-01', productName: '辦公椅 A 款', productVersion: 'A.03', opNo: 'OP-FINAL-90', lotNo: 'LOT-O-261123-01', customerNo: 'CUS-1001', qty: 48, status: 0, callbackType: '', erpSourceNo: '', erpSourceSeq: '', inspDocLink: 'DEV 文件連結 A', creator: 'DEV-QC-01', createDate: '2026-11-23 08:30:00', reviser: '', reviseDate: '', finishTime: '', dataStamp: 1, items: [copyItem('OQC-ATT-01'), copyItem('OQC-VAR-02')] },
  { id: 'DEV-OQC-002', qcFormNo: 'DEV-OQC-002', productNo: 'PRD-DESK-02', productName: '升降桌 B 款', productVersion: 'B.01', opNo: 'OP-PACK-95', lotNo: 'LOT-O-261122-04', customerNo: 'CUS-1002', qty: 64, status: 1, callbackType: 'WFGP', erpSourceNo: 'WF-SO-261122', erpSourceSeq: '0003', inspDocLink: 'DEV 文件連結 B', creator: 'DEV-ERP', createDate: '2026-11-22 15:10:00', reviser: 'DEV-QC-02', reviseDate: '2026-11-22 15:20:00', finishTime: '', dataStamp: 2, items: [copyItem('OQC-VAR-02'), copyItem('OQC-ATT-GRP-04')] },
  { id: 'DEV-OQC-003', qcFormNo: 'DEV-OQC-003', productNo: 'PRD-CHAIR-01', productName: '辦公椅 A 款', productVersion: 'A.03', opNo: 'OP-FINAL-90', lotNo: 'LOT-O-261123-01', customerNo: 'CUS-1001', qty: 48, status: 2, callbackType: 'TIPTOP', erpSourceNo: 'TT-SO-261123', erpSourceSeq: '0010', inspDocLink: 'DEV 文件連結 C', creator: 'DEV-ERP', createDate: '2026-11-23 09:10:00', reviser: 'DEV-QC-03', reviseDate: '2026-11-23 09:40:00', finishTime: '2026-11-23 09:35:00', dataStamp: 3, items: [copyItem('OQC-ATT-01', 1, 0, 'DEV-SPC-101'), copyItem('OQC-VAR-02', 1, 1, 'DEV-SPC-102')] },
  { id: 'DEV-OQC-004', qcFormNo: 'DEV-OQC-004', productNo: 'PRD-DESK-02', productName: '升降桌 B 款', productVersion: 'B.01', opNo: 'OP-PACK-95', lotNo: 'LOT-O-261122-04', customerNo: 'CUS-1002', qty: 64, status: 2, callbackType: 'WFGP', erpSourceNo: 'WF-SO-261122', erpSourceSeq: '0004', inspDocLink: 'DEV 文件連結 D', creator: 'DEV-ERP', createDate: '2026-11-22 11:30:00', reviser: 'DEV-QC-02', reviseDate: '2026-11-22 12:00:00', finishTime: '2026-11-22 11:55:00', dataStamp: 4, items: [copyItem('OQC-ATT-01', 1, 0, 'DEV-SPC-111')] },
  { id: 'DEV-OQC-005', qcFormNo: 'DEV-OQC-005', productNo: 'PRD-CHAIR-01', productName: '辦公椅 A 款', productVersion: 'A.03', opNo: 'OP-FINAL-90', lotNo: 'LOT-O-261121-05', customerNo: 'CUS-1001', qty: 20, status: 3, callbackType: 'T100', erpSourceNo: 'T100-DO-261121', erpSourceSeq: '0001', inspDocLink: '', creator: 'DEV-ERP', createDate: '2026-11-21 10:00:00', reviser: 'DEV-QC-04', reviseDate: '2026-11-21 10:25:00', finishTime: '2026-11-21 10:20:00', dataStamp: 5, items: [copyItem('OQC-ATT-01', 1, 0, 'DEV-SPC-120')] },
  { id: 'DEV-OQC-006', qcFormNo: 'DEV-OQC-006', productNo: 'PRD-DESK-02', productName: '升降桌 B 款', productVersion: 'B.01', opNo: 'OP-PACK-95', lotNo: 'LOT-O-261120-08', customerNo: 'CUS-1002', qty: 12, status: 4, callbackType: '', erpSourceNo: '', erpSourceSeq: '', inspDocLink: '', creator: 'DEV-QC-01', createDate: '2026-11-20 13:20:00', reviser: 'DEV-QC-05', reviseDate: '2026-11-20 14:00:00', finishTime: '2026-11-20 13:55:00', dataStamp: 6, items: [copyItem('OQC-VAR-02', 1, 1, 'DEV-SPC-130')] },
])

const activeStatus = ref<FormStatus>(0)
const selectedFormId = ref('DEV-OQC-001')
const selectedItemNo = ref('')
const search = ref('')
const useCreateDate = ref(false)
const fromDate = ref('2026-11-01')
const toDate = ref('2026-11-23')
const draftMode = ref<'add' | 'edit' | 'copy' | null>(null)
const draft = ref<Draft | null>(null)
const itemToAdd = ref('')
const resultMode = ref(false)
const resultValue = ref<ResultCode>(0)
const resultNote = ref('')
const callbackOutcome = ref<'success' | 'failure'>('success')
const sequence = ref(20)
const message = ref('DEV 假資料；外部 ERP、SPC 與異常單均不會實際呼叫。')
const messageKind = ref<'info' | 'success' | 'warning' | 'error'>('info')
const selectedForm = computed(() => forms.value.find((form) => form.id === selectedFormId.value) || null)
const selectedItem = computed(() => selectedForm.value?.items.find((item) => item.qcItemNo === selectedItemNo.value) || null)
const visibleForms = computed(() => forms.value.filter((form) => {
  if (form.status !== activeStatus.value) return false
  const value = search.value.trim().toLowerCase()
  if (value && ![form.qcFormNo, form.productNo, form.productName, form.customerNo, form.lotNo, form.erpSourceNo, form.inspDocLink].some((field) => String(field || '').toLowerCase().includes(value))) return false
  if (useCreateDate.value && fromDate.value && form.createDate.slice(0, 10) < fromDate.value) return false
  if (useCreateDate.value && toDate.value && form.createDate.slice(0, 10) > toDate.value) return false
  return true
}))
const pendingCount = computed(() => selectedForm.value?.items.filter((item) => item.status === 0).length || 0)
const canRemoveWaitForm = computed(() => !!selectedForm.value && selectedForm.value.items.every((item) => item.status === 0))
const availableItems = computed(() => {
  const product = products.find((row) => row.no === draft.value?.productNo)
  const keys = product?.items || itemCatalog.map((item) => item.qcItemNo)
  return itemCatalog.filter((item) => keys.includes(item.qcItemNo) && !draft.value?.items.some((mapped) => mapped.qcItemNo === item.qcItemNo))
})
const availableMappedItems = computed(() => {
  const product = products.find((row) => row.no === selectedForm.value?.productNo)
  const keys = product?.items || itemCatalog.map((item) => item.qcItemNo)
  return itemCatalog.filter((item) => keys.includes(item.qcItemNo) && !selectedForm.value?.items.some((mapped) => mapped.qcItemNo === item.qcItemNo))
})

function setMessage(text: string, kind: typeof messageKind.value = 'info'): void { message.value = text; messageKind.value = kind }
function setStatus(status: FormStatus): void {
  activeStatus.value = status
  const first = forms.value.find((form) => form.status === status)
  selectedFormId.value = first?.id || ''; selectedItemNo.value = first?.items[0]?.qcItemNo || ''; itemToAdd.value = ''; resultMode.value = false
}
function selectForm(form: OqcForm): void { selectedFormId.value = form.id; selectedItemNo.value = form.items[0]?.qcItemNo || ''; resultMode.value = false }
function selectItem(no: string): void { selectedItemNo.value = no }
function updateStatus(form: OqcForm, status: FormStatus): void {
  form.status = status; form.reviser = props.currentUserNo?.trim() || 'DEV-QC'; form.reviseDate = dateTime(); form.dataStamp += 1
}
function createDraft(mode: 'add' | 'edit' | 'copy', source?: OqcForm): void {
  const editing = mode === 'edit'
  draftMode.value = mode
  draft.value = {
    id: editing ? source?.id : undefined, qcFormNo: editing ? source?.qcFormNo || '' : '',
    productNo: source?.productNo || '', productName: source?.productName || '', productVersion: source?.productVersion || '',
    opNo: source?.opNo || '', lotNo: source?.lotNo || '', customerNo: source?.customerNo || '', qty: source?.qty || 0,
    callbackType: editing ? source?.callbackType || '' : '', erpSourceNo: editing ? source?.erpSourceNo || '' : '',
    erpSourceSeq: editing ? source?.erpSourceSeq || '' : '', inspDocLink: source?.inspDocLink || '',
    dataStamp: source?.dataStamp || 0, items: source ? cloneItems(source.items, !editing) : [],
  }
  if (mode === 'add') draft.value.items = []
  itemToAdd.value = ''
}
function closeDraft(): void { draftMode.value = null; draft.value = null }
function assignDevNo(): void { if (draft.value) draft.value.qcFormNo = `DEV-OQC-${String(sequence.value++).padStart(3, '0')}` }
function fillProduct(no: string): void {
  const product = products.find((row) => row.no === no)
  if (!draft.value || !product) return
  draft.value.productNo = product.no; draft.value.productName = product.name; draft.value.productVersion = product.version; draft.value.customerNo = product.customerNo
  draft.value.items = product.items.map((key) => copyItem(key))
  if (draft.value.opNo && !product.ops.includes(draft.value.opNo)) draft.value.opNo = ''
}
function fillLot(no: string): void {
  const lot = lots.find((row) => row.lotNo === no)
  if (!draft.value || !lot) return
  const product = products.find((row) => row.no === lot.productNo)
  if (!product) return
  fillProduct(product.no); draft.value.lotNo = lot.lotNo; draft.value.opNo = lot.opNo; draft.value.qty = lot.qty
  setMessage(`已由 DEV 批號帶入 ${lot.valid ? '有效' : '非有效'}批號、產品與 QC Item。`)
}
function addDraftItem(): void { if (draft.value && itemToAdd.value) { draft.value.items.push(copyItem(itemToAdd.value)); itemToAdd.value = '' } }
function removeDraftItem(no: string): void { if (draft.value) draft.value.items = draft.value.items.filter((item) => item.qcItemNo !== no) }
function saveDraft(): void {
  const current = draft.value
  if (!current) return
  if (!current.qcFormNo.trim()) return setMessage('請先取得 DEV OQC 單號。', 'error')
  if (!current.productNo || !current.opNo || !current.productVersion) return setMessage('產品、工序與產品版本不可空白。', 'error')
  if (!Number.isFinite(Number(current.qty)) || Number(current.qty) <= 0) return setMessage('檢驗數量必須大於 0。', 'error')
  if (current.lotNo && !lots.some((lot) => lot.lotNo === current.lotNo && lot.valid)) return setMessage('輸入的批號不是有效 DEV 批號。', 'error')
  if (draftMode.value === 'edit') {
    const old = forms.value.find((form) => form.id === current.id)
    if (!old || old.dataStamp !== current.dataStamp) return setMessage('DEV DataStamp 已變更，請重新載入後再編輯。', 'error')
    Object.assign(old, current, { qty: Number(current.qty), dataStamp: old.dataStamp + 1, reviser: props.currentUserNo?.trim() || 'DEV-QC', reviseDate: dateTime(), items: cloneItems(current.items) })
    setMessage('未確認 OQC 單已於 DEV 假資料修改。', 'success')
  } else {
    if (forms.value.some((form) => form.qcFormNo === current.qcFormNo)) return setMessage('DEV OQC 單號已存在。', 'error')
    const created: OqcForm = { ...current, id: current.qcFormNo, status: 0, callbackType: '', erpSourceNo: '', erpSourceSeq: '', creator: props.currentUserNo?.trim() || 'DEV-QC', createDate: dateTime(), reviser: '', reviseDate: '', finishTime: '', dataStamp: 1, qty: Number(current.qty), items: cloneItems(current.items) }
    forms.value.unshift(created); activeStatus.value = 0; selectedFormId.value = created.id; selectedItemNo.value = created.items[0]?.qcItemNo || ''
    setMessage(`${draftMode.value === 'copy' ? '已複製' : '已新增'}未確認 OQC 單 ${created.qcFormNo}。`, 'success')
  }
  closeDraft()
}
function deleteUnconfirmed(): void {
  const form = selectedForm.value
  if (!form || activeStatus.value !== 0) return
  if (form.callbackType) return setMessage('來源 WinForms 禁止刪除 ERP 開立單據。', 'error')
  forms.value = forms.value.filter((row) => row.id !== form.id); selectedFormId.value = visibleForms.value[0]?.id || ''; selectedItemNo.value = ''
  setMessage('已從 DEV 記憶體刪除未確認單及 QC Item Map。', 'success')
}
function confirmSelected(): void {
  const form = selectedForm.value
  if (!form || activeStatus.value !== 0) return
  updateStatus(form, 1); activeStatus.value = 1; selectedFormId.value = form.id; selectedItemNo.value = form.items[0]?.qcItemNo || ''
  setMessage('已確認，OQC 單進入待檢。', 'success')
}
function removeWaitForm(): void {
  const form = selectedForm.value
  if (!form || activeStatus.value !== 1 || !canRemoveWaitForm.value) return
  if (form.callbackType) {
    updateStatus(form, 0); activeStatus.value = 0; selectedFormId.value = form.id
    setMessage('DEV 模擬 ERP 來源單據由待檢退回未確認；原生服務使用 DataStamp。', 'success')
  } else {
    forms.value = forms.value.filter((row) => row.id !== form.id); selectedFormId.value = visibleForms.value[0]?.id || ''; selectedItemNo.value = ''
    setMessage('已刪除尚未開始檢驗的待檢單及 Map。', 'success')
  }
}
function addMappedItem(): void { if (selectedForm.value?.status === 0 && itemToAdd.value) { selectedForm.value.items.push(copyItem(itemToAdd.value)); itemToAdd.value = '' } }
function removeMappedItem(no: string): void { if (selectedForm.value?.status === 0) selectedForm.value.items = selectedForm.value.items.filter((item) => item.qcItemNo !== no) }
function beginSpc(): void { if (selectedItem.value?.status === 0 && activeStatus.value === 1) { resultValue.value = 0; resultNote.value = ''; resultMode.value = true } }
function saveSpcResult(): void {
  const item = selectedItem.value; const form = selectedForm.value
  if (!item || !form) return
  item.status = 1; item.result = resultValue.value; item.note = resultNote.value.trim() || 'DEV SPC 假結果'; item.serial = `DEV-SPC-${String(Date.now()).slice(-6)}`; resultMode.value = false
  const allDone = form.items.length > 0 && form.items.every((row) => row.status === 1)
  const hasAbnormal = form.items.some((row) => row.result !== 0)
  if (allDone) {
    form.status = 2; form.finishTime = dateTime(); form.dataStamp += 1; activeStatus.value = 2; selectedFormId.value = form.id
    if (hasAbnormal) setMessage('DEV 假設 OP 的 SPCInspAbnormalHoldLot 規則啟用，來源會開 WP_BRERFCreate；此頁不呼叫 ERF。所有 QC Item 已完成，OQC 單進入檢驗完成。', 'warning')
    else setMessage('所有 QC Item 已完成，OQC 單進入檢驗完成。', 'success')
  } else setMessage(`已記錄 ${item.qcItemNo} DEV 結果，尚有 ${pendingCount.value} 項待檢。`, 'success')
}
function finalize(status: 3 | 4): void {
  const form = selectedForm.value
  if (!form || activeStatus.value !== 2) return
  const callbackType = form.callbackType.toUpperCase()
  const usesSqms = callbackType === 'TIPTOP' || callbackType === 'T100'
  if (usesSqms && callbackOutcome.value === 'failure') return setMessage(`DEV 模擬 ${callbackType} ERP 回報失敗；來源流程不更新 OQC Status，單據留在檢驗完成。`, 'error')
  if (usesSqms) setMessage(`DEV 模擬 ${callbackType} ERP 回報成功：${status === 3 ? `AcceptQty=${form.qty}` : `RejectQty=${form.qty}`}、ScrapQty=0；沒有連外部 ERP。`, 'success')
  else if (callbackType === 'WFGP') setMessage('DEV 依 WinForms 分支略過 WFGP ERP 回報，更新本機 OQC Status；未呼叫外部服務。', 'warning')
  else setMessage(`DEV 已模擬${statusName(status)}；此單不走 sQMS 回報。`, 'success')
  updateStatus(form, status); activeStatus.value = status; selectedFormId.value = form.id
}
function copySelected(): void { if (selectedForm.value) createDraft('copy', selectedForm.value) }
</script>

<template>
  <main class="oqc-page" aria-label="出貨檢驗單 DEV">
    <header class="oqc-title"><div><p class="eyebrow">SPC / OUTGOING QUALITY CONTROL</p><h1>出貨檢驗單</h1><p>WinForms SC106 對應頁 · DEV 假資料工作區</p></div><button class="quiet" @click="emit('close')">關閉</button></header>
    <section class="query-panel"><label class="query-search">OQC 單／批號／產品／ERP 單<input v-model="search" type="search" placeholder="輸入關鍵字" /></label><label class="check-label"><input v-model="useCreateDate" type="checkbox" />建立日期</label><label>起日<input v-model="fromDate" type="date" :disabled="!useCreateDate" /></label><label>迄日<input v-model="toDate" type="date" :disabled="!useCreateDate" /></label><button class="primary" @click="setMessage(`已查詢 ${visibleForms.length} 筆 DEV ${statusName(activeStatus)} 單據。`, 'success')">查詢</button><span class="query-source">關鍵字為本機 DEV 輔助篩選</span></section>
    <nav class="status-tabs" aria-label="OQC 單狀態"><button v-for="tab in tabs" :key="tab.status" :class="{ active: activeStatus === tab.status }" @click="setStatus(tab.status)">{{ tab.label }} <span>{{ forms.filter((form) => form.status === tab.status).length }}</span></button></nav>
    <section class="workspace"><div class="list-pane"><div class="pane-heading"><div><h2>{{ statusName(activeStatus) }}清單</h2><span>{{ visibleForms.length }} 筆</span></div>
      <div v-if="activeStatus === 0" class="actions"><button class="primary" @click="createDraft('add')">新增</button><button :disabled="!selectedForm" @click="createDraft('edit', selectedForm || undefined)">編輯</button><button :disabled="!selectedForm" @click="copySelected">複製</button><button class="danger" :disabled="!selectedForm || !!selectedForm.callbackType" @click="deleteUnconfirmed">刪除</button><button class="primary" :disabled="!selectedForm" @click="confirmSelected">確認</button></div>
      <div v-else-if="activeStatus === 1" class="actions"><button class="primary" :disabled="!selectedItem || selectedItem.status === 1" @click="beginSpc">SPC 檢驗</button><button class="danger" :disabled="!canRemoveWaitForm" @click="removeWaitForm">{{ selectedForm?.callbackType ? '退回未確認' : '刪除未開始單據' }}</button><button :disabled="!selectedForm" @click="copySelected">複製</button></div>
      <div v-else-if="activeStatus === 2" class="actions"><button class="primary" :disabled="!selectedForm" @click="finalize(3)">接收</button><button class="danger" :disabled="!selectedForm" @click="finalize(4)">拒收</button><button :disabled="!selectedForm" @click="copySelected">複製</button></div>
      <div v-else class="actions"><button :disabled="!selectedForm" @click="copySelected">複製</button></div>
    </div><div class="table-wrap"><table><thead><tr><th>OQC 單號</th><th>ERP 來源</th><th>產品</th><th>客戶／批號</th><th>工序</th><th>Qty</th><th>回報類型</th><th>建立時間</th></tr></thead><tbody>
      <tr v-for="form in visibleForms" :key="form.id" :class="{ selected: form.id === selectedFormId }" tabindex="0" @click="selectForm(form)" @keydown.enter="selectForm(form)"><td class="mono">{{ form.qcFormNo }}</td><td>{{ form.erpSourceNo || 'MES 開單' }}<small>{{ form.erpSourceSeq }}</small></td><td>{{ form.productNo }}<small>{{ form.productName }}</small></td><td>{{ form.customerNo }}<small>{{ form.lotNo || '—' }}</small></td><td>{{ form.opNo }}</td><td>{{ form.qty }}</td><td>{{ form.callbackType || '一般 OQC' }}</td><td>{{ form.createDate }}</td></tr>
      <tr v-if="visibleForms.length === 0"><td colspan="8" class="empty">此狀態沒有符合條件的 DEV 單據。</td></tr>
    </tbody></table></div></div>
    <aside class="detail-pane"><template v-if="selectedForm"><div class="pane-heading"><div><h2>OQC 單明細</h2><span class="status-pill" :data-status="selectedForm.status">{{ statusName(selectedForm.status) }}</span></div><span class="mono">{{ selectedForm.qcFormNo }}</span></div>
      <dl class="form-facts"><div><dt>產品／版本</dt><dd>{{ selectedForm.productNo }} · {{ selectedForm.productName }} / {{ selectedForm.productVersion }}</dd></div><div><dt>客戶／數量</dt><dd>{{ selectedForm.customerNo }} / {{ selectedForm.qty }}</dd></div><div><dt>批號／工序</dt><dd>{{ selectedForm.lotNo || '—' }} / {{ selectedForm.opNo }}</dd></div><div><dt>CallbackType</dt><dd>{{ selectedForm.callbackType || '一般 OQC' }}</dd></div><div><dt>ERP 來源單號／序號</dt><dd>{{ selectedForm.erpSourceNo || '—' }} / {{ selectedForm.erpSourceSeq || '—' }}</dd></div><div><dt>InspDocLink</dt><dd>{{ selectedForm.inspDocLink || '—' }}</dd></div><div><dt>建立人／時間</dt><dd>{{ selectedForm.creator }} · {{ selectedForm.createDate }}</dd></div><div><dt>完成時間</dt><dd>{{ selectedForm.finishTime || '—' }}</dd></div><div><dt>最後修改</dt><dd>{{ selectedForm.reviser || '—' }} {{ selectedForm.reviseDate }}</dd></div></dl>
      <div class="item-heading"><div><h3>QC Item Map</h3><span>{{ selectedForm.items.length }} 項 · {{ pendingCount }} 項待檢</span></div><div v-if="activeStatus === 0" class="map-actions"><select v-model="itemToAdd" aria-label="選擇 QC Item"><option value="">選擇 QC Item</option><option v-for="item in availableMappedItems" :key="item.qcItemNo" :value="item.qcItemNo">{{ item.qcItemNo }} · {{ item.name }}</option></select><button :disabled="!itemToAdd" @click="addMappedItem">加入</button></div></div>
      <div class="item-list"><div v-for="item in selectedForm.items" :key="item.qcItemNo" class="item-row" :class="{ chosen: selectedItemNo === item.qcItemNo }" role="button" tabindex="0" @click="selectItem(item.qcItemNo)" @keydown.enter="selectItem(item.qcItemNo)"><span class="item-state" :data-done="item.status === 1">{{ item.status === 1 ? '完成' : '待檢' }}</span><span class="item-name"><b>{{ item.qcItemNo }}</b><small>{{ item.name }} · {{ itemTypeName(item.type) }}</small></span><span class="item-result">{{ resultName(item.result) }}</span><button v-if="activeStatus === 0" class="remove" @click.stop="removeMappedItem(item.qcItemNo)">移除</button></div><p v-if="selectedForm.items.length === 0" class="empty">尚未加入 QC Item Map。</p></div>
      <section v-if="activeStatus === 1 && selectedItem" class="inspection-card"><h3>待檢 QC Item</h3><p>{{ selectedItem.qcItemNo }} · {{ selectedItem.name }}（{{ itemTypeName(selectedItem.type) }}）</p><p v-if="selectedItem.status === 1" class="result-summary">{{ resultName(selectedItem.result) }} · {{ selectedItem.serial }}</p><div v-else-if="resultMode" class="result-editor"><label>DEV 判定<select v-model.number="resultValue"><option :value="0">合格</option><option :value="1">不合格／Hold</option><option :value="2">Wait</option><option :value="3">Hold</option><option :value="4">警示</option></select></label><label>備註<input v-model="resultNote" placeholder="可留空" /></label><div><button @click="resultMode = false">取消</button><button class="primary" @click="saveSpcResult">記錄假結果</button></div></div><p v-else class="hint">模擬 SPC 假量測；不連量測設備或 Service。</p></section>
      <section v-if="activeStatus === 2" class="inspection-card"><h3>ERP 回報模擬</h3><p>來源僅在 TIPTOP／T100 分支呼叫 sQMS。選擇模擬回傳，失敗時 OQC 狀態保留在檢驗完成。</p><label class="outcome">假回傳<select v-model="callbackOutcome"><option value="success">成功</option><option value="failure">失敗</option></select></label><p>接收回報 AcceptQty=Qty、RejectQty=0；拒收回報 RejectQty=Qty、AcceptQty=0；ScrapQty=0。</p></section>
    </template><div v-else class="empty empty-panel">選取一筆 OQC 單查看 QC Item 與 ERP 來源。</div></aside></section>
    <section v-if="draft && draftMode" class="draft-panel"><div class="pane-heading"><div><h2>{{ draftMode === 'add' ? '新增' : draftMode === 'edit' ? '編輯' : '複製' }} OQC 檢驗單</h2><span>依 frmOQCFormDef；資料皆為 DEV fixture</span></div><button @click="closeDraft">關閉</button></div>
      <div class="draft-grid"><label>檢驗單號<div class="inline-field"><input v-model="draft.qcFormNo" :readonly="draftMode !== 'edit'" /><button v-if="draftMode !== 'edit'" @click="assignDevNo">取得 DEV 單號</button></div></label><label>產品<select v-model="draft.productNo" @change="fillProduct(draft.productNo)"><option value="">選擇產品</option><option v-for="product in products" :key="product.no" :value="product.no">{{ product.no }} · {{ product.name }}</option></select></label><label>產品名稱<input v-model="draft.productName" /></label><label>產品版本<input v-model="draft.productVersion" /></label><label>工序 OPNo<select v-model="draft.opNo"><option value="">選擇工序</option><option v-for="op in products.find((product) => product.no === draft?.productNo)?.ops || []" :key="op" :value="op">{{ op }}</option></select></label><label>客戶<input v-model="draft.customerNo" /></label><label>批號（可空白）<select v-model="draft.lotNo" @change="fillLot(draft.lotNo)"><option value="">不指定批號</option><option v-for="lot in lots" :key="lot.lotNo" :value="lot.lotNo">{{ lot.lotNo }} · {{ lot.productNo }}</option></select></label><label>檢驗數量<input v-model.number="draft.qty" type="number" min="0" step="0.00001" /></label><label>檢驗文件連結<input v-model="draft.inspDocLink" placeholder="來源文件參照" /></label><label>ERP 回報類型<input :value="draft.callbackType || '一般 OQC（新增／複製）'" readonly /></label></div>
      <div class="item-heading"><div><h3>QC Item Map</h3><span>依產品／版本／工序帶入；可調整 DEV 對應項目</span></div><div class="map-actions"><select v-model="itemToAdd"><option value="">選擇 QC Item</option><option v-for="item in availableItems" :key="item.qcItemNo" :value="item.qcItemNo">{{ item.qcItemNo }} · {{ item.name }}</option></select><button :disabled="!itemToAdd" @click="addDraftItem">加入</button></div></div>
      <div class="draft-items"><span v-for="item in draft.items" :key="item.qcItemNo" class="draft-chip">{{ item.qcItemNo }} · {{ item.name }}<button @click="removeDraftItem(item.qcItemNo)">×</button></span><span v-if="draft.items.length === 0" class="hint">尚未載入品管項目</span></div><div class="draft-footer"><span>ERP CallBackType／來源單據由 ERP 建立；複製會建立一般 DEV OQC 單。</span><button @click="closeDraft">取消</button><button class="primary" @click="saveDraft">儲存 DEV 單據</button></div>
    </section><footer class="status-message" :data-kind="messageKind"><span>{{ message }}</span><span>本頁記憶體假資料 · DataStamp {{ selectedForm?.dataStamp ?? '—' }}</span></footer>
  </main>
</template>

<style scoped>
.oqc-page{min-height:100%;padding:14px;box-sizing:border-box;color:#292820;background:#f4f2e9;font:inherit;display:flex;flex-direction:column;gap:10px}.oqc-title,.pane-heading,.item-heading,.draft-footer{display:flex;align-items:center;justify-content:space-between;gap:12px}.oqc-title{padding:12px 14px;background:#fffdf7;border:1px solid #d8d4c7;border-radius:5px}.oqc-title h1,.pane-heading h2,.item-heading h3,.inspection-card h3{margin:0;font-size:16px}.oqc-title p,.pane-heading span,.item-heading span,.inspection-card p{margin:4px 0 0;color:#777366;font-size:12px}.eyebrow{margin:0 0 4px!important;color:#827510!important;font-size:10px!important;letter-spacing:.12em}.query-panel{display:flex;align-items:end;gap:8px;flex-wrap:wrap;padding:10px 12px;border:1px solid #d8d4c7;background:#fffdf7;border-radius:4px}.query-panel label,.draft-grid label,.result-editor label,.outcome{display:flex;flex-direction:column;gap:4px;color:#625f53;font-size:11px}.query-search{min-width:220px;flex:1}.check-label{display:flex!important;flex-direction:row!important;align-items:center;min-height:30px}.query-source{color:#858173;font-size:11px;padding:0 4px 7px}button{min-height:29px;border:1px solid #bdb8aa;border-radius:3px;padding:4px 9px;background:#fff;color:#343228;font:inherit;font-size:12px;cursor:pointer}button:hover:not(:disabled){border-color:#817619;background:#fffbea}button:disabled{opacity:.45;cursor:not-allowed}.primary{color:#fff;background:#3e3b30;border-color:#3e3b30}.primary:hover:not(:disabled){color:#fff;background:#605c45}.danger{color:#8c3028}.quiet{align-self:flex-start}.query-panel input,.draft-grid input,.draft-grid select,.result-editor input,.result-editor select,.outcome select,.map-actions select{min-height:30px;border:1px solid #c9c4b5;background:white;border-radius:3px;padding:4px 7px;color:#292820;font:inherit}.status-tabs{display:flex;gap:2px;padding:0 4px;border-bottom:1px solid #b9b4a5;overflow:auto}.status-tabs button{border:0;border-radius:4px 4px 0 0;color:#5f5b4e;background:transparent;padding:8px 13px;white-space:nowrap}.status-tabs button.active{color:#28271f;background:#fffdf7;box-shadow:0 -2px 0 #b6a428 inset;font-weight:600}.status-tabs span{display:inline-grid;min-width:17px;height:17px;place-items:center;margin-left:3px;border-radius:10px;background:#ebe8dd;font-size:10px}.workspace{display:grid;grid-template-columns:minmax(0,1.7fr) minmax(360px,1fr);gap:10px;min-height:350px;flex:1}.list-pane,.detail-pane,.draft-panel{min-width:0;border:1px solid #d8d4c7;border-radius:4px;background:#fffdf7;padding:10px}.detail-pane{display:flex;flex-direction:column;gap:10px}.actions,.map-actions{display:flex;gap:5px;flex-wrap:wrap;align-items:center}.table-wrap{overflow:auto;margin-top:9px;max-height:430px}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:11px}th{position:sticky;top:0;z-index:1;text-align:left;color:#5d594d;background:#efede3;font-weight:600}th,td{padding:7px 8px;border-bottom:1px solid #e4e1d8}tbody tr{cursor:pointer}tbody tr:hover,tbody tr.selected{background:#f8f3cf}td small{display:block;margin-top:2px;color:#858173;font-size:10px}.mono{font-family:Consolas,monospace}.empty{padding:20px;color:#837f71;text-align:center}.empty-panel{margin:auto}.status-pill{display:inline-block;padding:3px 7px;border-radius:10px;background:#ece9de;color:#615d50}.status-pill[data-status="2"]{background:#f8edb8}.status-pill[data-status="3"]{background:#dcebd8}.status-pill[data-status="4"]{background:#f0dcd6}.form-facts{display:grid;grid-template-columns:1fr 1fr;gap:6px 12px;margin:0;padding:9px;background:#f6f4eb;border:1px solid #e3dfd3}.form-facts dt{color:#858173;font-size:10px}.form-facts dd{overflow-wrap:anywhere;margin:2px 0 0;font-size:11px}.item-heading{border-bottom:1px solid #dedacd;padding-bottom:6px}.item-list{display:flex;flex-direction:column;gap:4px}.item-row{display:flex;width:100%;align-items:center;gap:8px;min-height:43px;padding:5px 7px;border:1px solid #e0ddd3;border-radius:3px;text-align:left;cursor:pointer}.item-row.chosen{border-color:#a99b34;background:#fbf6d9}.item-state{color:#766f54;font-size:10px}.item-state[data-done="true"]{color:#3e7041}.item-name{flex:1}.item-name b,.item-name small{display:block}.item-name b{font-size:11px}.item-name small{margin-top:2px;color:#7e7a6d;font-size:10px}.item-result{color:#5e594a;font-size:11px}.remove{min-height:23px;padding:2px 5px;color:#8c3028}.inspection-card{padding:9px;border:1px solid #e0ddd3;background:#faf8ef}.result-summary{padding:7px;border-radius:3px;background:#e8efdf}.result-editor{display:grid;grid-template-columns:1fr 1fr;gap:8px;margin-top:9px}.result-editor>div{grid-column:1/-1;display:flex;justify-content:flex-end;gap:6px}.hint{color:#858173;font-size:11px}.draft-panel{display:flex;flex-direction:column;gap:10px}.draft-grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:8px}.inline-field{display:flex;gap:4px}.inline-field input{min-width:80px;flex:1}.draft-items{display:flex;gap:6px;flex-wrap:wrap;min-height:26px}.draft-chip{display:inline-flex;align-items:center;gap:5px;padding:4px 7px;border:1px solid #ddd8c8;border-radius:12px;background:#f5f2e8;font-size:11px}.draft-chip button{min-height:20px;width:20px;padding:0;border:0;font-size:16px}.draft-footer{justify-content:flex-end;padding-top:8px;border-top:1px solid #e2dfd6}.draft-footer span{margin-right:auto;color:#858173;font-size:11px}.status-message{display:flex;justify-content:space-between;gap:10px;padding:8px 10px;border:1px solid #ddd8c8;border-radius:3px;background:#efede4;color:#625f53;font-size:11px}.status-message[data-kind="success"]{color:#3e7041}.status-message[data-kind="warning"]{color:#876b00}.status-message[data-kind="error"]{color:#9a382e}@media(max-width:1000px){.workspace{grid-template-columns:1fr}.detail-pane{min-height:330px}.draft-grid{grid-template-columns:repeat(2,minmax(0,1fr))}}@media(max-width:620px){.oqc-page{padding:7px}.query-search{flex-basis:100%}.workspace{min-height:0}.draft-grid,.form-facts{grid-template-columns:1fr}.pane-heading{align-items:flex-start;flex-direction:column}.actions{width:100%}.status-message{flex-direction:column}}
</style>
