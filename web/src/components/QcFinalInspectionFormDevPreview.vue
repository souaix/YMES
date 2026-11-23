<script setup lang="ts">
import { computed, ref } from 'vue'

type FormStatus = 0 | 1 | 2 | 3 | 4
type ItemResult = '待檢' | '合格' | '不合格' | '警示'
interface QcItem { qcItemNo: string; name: string; type: string; status: 0 | 1; result: ItemResult; serial: string; note: string }
interface FinalForm {
  id: string; qcFormNo: string; productNo: string; productName: string; opNo: string; productVersion: string; lotNo: string
  customerNo: string; qty: number; inspDocLink: string; status: FormStatus; callBackType: string; erpSourceNo: string; erpSourceSeq: string
  creator: string; createDate: string; reviser: string; reviseDate: string; finishTime: string; dataStamp: number; items: QcItem[]
}
interface Draft extends Omit<FinalForm, 'id' | 'status' | 'creator' | 'createDate' | 'reviser' | 'reviseDate' | 'finishTime' | 'dataStamp' | 'items'> {
  id?: string; status?: FormStatus; creator?: string; createDate?: string; reviser?: string; reviseDate?: string; finishTime?: string; dataStamp?: number; items: QcItem[]
}

const props = defineProps<{ developmentPreview: boolean; currentUserNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const statusTabs: Array<{ status: FormStatus; label: string }> = [
  { status: 0, label: '未確認' }, { status: 1, label: '待檢' }, { status: 2, label: '檢驗完成' }, { status: 3, label: '允收' }, { status: 4, label: '拒收' },
]
const statusName = (status: FormStatus): string => statusTabs.find((tab) => tab.status === status)?.label || '未知'
const itemCatalog: QcItem[] = [
  { qcItemNo: 'FQC-V-DIM-01', name: '成品尺寸', type: '計量', status: 0, result: '待檢', serial: '', note: '' },
  { qcItemNo: 'FQC-A-VIS-02', name: '外觀檢查', type: '計數', status: 0, result: '待檢', serial: '', note: '' },
  { qcItemNo: 'FQC-V-FUNC-03', name: '功能測試', type: '計量', status: 0, result: '待檢', serial: '', note: '' },
  { qcItemNo: 'FQC-A-LABEL-04', name: '標籤與包裝', type: '計數', status: 0, result: '待檢', serial: '', note: '' },
]
const products = [
  { productNo: 'PRD-CHAIR-01', productName: '辦公椅 A 款', opNo: 'OP-FINAL-01', productVersion: 'A.03', customerNo: 'CUS-1001', qcItems: ['FQC-V-DIM-01', 'FQC-A-VIS-02', 'FQC-A-LABEL-04'] },
  { productNo: 'PRD-DESK-02', productName: '升降桌 B 款', opNo: 'OP-FINAL-02', productVersion: 'B.01', customerNo: 'CUS-1002', qcItems: ['FQC-V-DIM-01', 'FQC-V-FUNC-03', 'FQC-A-LABEL-04'] },
  { productNo: 'PRD-LAMP-03', productName: '工作燈 C 款', opNo: 'OP-FINAL-03', productVersion: 'C.02', customerNo: 'CUS-1003', qcItems: ['FQC-A-VIS-02', 'FQC-V-FUNC-03'] },
]
const lotCatalog = [
  { lotNo: 'LOT-F-261123-01', productNo: 'PRD-CHAIR-01', qty: 120, customerNo: 'CUS-1001', opNo: 'OP-FINAL-01', productVersion: 'A.03' },
  { lotNo: 'LOT-F-261122-07', productNo: 'PRD-DESK-02', qty: 48, customerNo: 'CUS-1002', opNo: 'OP-FINAL-02', productVersion: 'B.01' },
  { lotNo: 'LOT-F-261120-03', productNo: 'PRD-LAMP-03', qty: 80, customerNo: 'CUS-1003', opNo: 'OP-FINAL-03', productVersion: 'C.02' },
]
const cloneItem = (source: QcItem): QcItem => ({ ...source })
const makeItems = (keys: string[], done = false): QcItem[] => keys.map((key, index) => {
  const item = itemCatalog.find((row) => row.qcItemNo === key) || itemCatalog[0]
  return { ...cloneItem(item), status: done ? 1 : 0, result: done ? (index % 2 === 0 ? '合格' : '警示') : '待檢', serial: done ? `DEV-SPC-${String(index + 11).padStart(3, '0')}` : '', note: done ? 'DEV 假量測已完成' : '' }
})
const nowText = (): string => new Date().toLocaleString('sv-SE').replace('T', ' ')
const forms = ref<FinalForm[]>([
  { id: 'DEV-FQC-261123-001', qcFormNo: 'DEV-FQC-261123-001', productNo: 'PRD-CHAIR-01', productName: '辦公椅 A 款', opNo: 'OP-FINAL-01', productVersion: 'A.03', lotNo: 'LOT-F-261123-01', customerNo: 'CUS-1001', qty: 120, inspDocLink: 'DEV://FQC/CHAIR-A', status: 0, callBackType: '', erpSourceNo: '', erpSourceSeq: '', creator: 'DEV-USER', createDate: '2026-11-23 09:10:00', reviser: '', reviseDate: '', finishTime: '', dataStamp: 1, items: makeItems(['FQC-V-DIM-01', 'FQC-A-VIS-02', 'FQC-A-LABEL-04']) },
  { id: 'DEV-FQC-261123-002', qcFormNo: 'DEV-FQC-261123-002', productNo: 'PRD-DESK-02', productName: '升降桌 B 款', opNo: 'OP-FINAL-02', productVersion: 'B.01', lotNo: 'LOT-F-261122-07', customerNo: 'CUS-1002', qty: 48, inspDocLink: '', status: 1, callBackType: '', erpSourceNo: '', erpSourceSeq: '', creator: 'DEV-USER', createDate: '2026-11-23 08:30:00', reviser: '', reviseDate: '', finishTime: '', dataStamp: 2, items: makeItems(['FQC-V-DIM-01', 'FQC-V-FUNC-03']) },
  { id: 'DEV-FQC-261122-005', qcFormNo: 'DEV-FQC-261122-005', productNo: 'PRD-LAMP-03', productName: '工作燈 C 款', opNo: 'OP-FINAL-03', productVersion: 'C.02', lotNo: 'LOT-F-261120-03', customerNo: 'CUS-1003', qty: 80, inspDocLink: 'DEV://FQC/LAMP-C', status: 2, callBackType: 'Workflow', erpSourceNo: 'DEV-ERP-45021', erpSourceSeq: '1', creator: 'DEV-USER', createDate: '2026-11-22 15:40:00', reviser: '', reviseDate: '', finishTime: '2026-11-22 16:05:00', dataStamp: 3, items: makeItems(['FQC-A-VIS-02', 'FQC-V-FUNC-03'], true) },
  { id: 'DEV-FQC-261121-003', qcFormNo: 'DEV-FQC-261121-003', productNo: 'PRD-CHAIR-01', productName: '辦公椅 A 款', opNo: 'OP-FINAL-01', productVersion: 'A.03', lotNo: 'LOT-F-261123-01', customerNo: 'CUS-1001', qty: 64, inspDocLink: '', status: 3, callBackType: 'T100', erpSourceNo: 'DEV-ERP-44881', erpSourceSeq: '2', creator: 'DEV-USER', createDate: '2026-11-21 10:15:00', reviser: 'DEV-QC-02', reviseDate: '2026-11-21 11:00:00', finishTime: '2026-11-21 11:00:00', dataStamp: 4, items: makeItems(['FQC-V-DIM-01', 'FQC-A-VIS-02'], true) },
  { id: 'DEV-FQC-261120-004', qcFormNo: 'DEV-FQC-261120-004', productNo: 'PRD-DESK-02', productName: '升降桌 B 款', opNo: 'OP-FINAL-02', productVersion: 'B.01', lotNo: 'LOT-F-261122-07', customerNo: 'CUS-1002', qty: 32, inspDocLink: '', status: 4, callBackType: 'Workflow', erpSourceNo: 'DEV-ERP-44605', erpSourceSeq: '1', creator: 'DEV-USER', createDate: '2026-11-20 14:20:00', reviser: 'DEV-QC-04', reviseDate: '2026-11-20 15:10:00', finishTime: '2026-11-20 15:10:00', dataStamp: 5, items: makeItems(['FQC-V-DIM-01', 'FQC-V-FUNC-03'], true) },
])

const activeStatus = ref<FormStatus>(0)
const selectedFormId = ref('DEV-FQC-261123-001')
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
const resultValue = ref<ItemResult>('合格')
const resultNote = ref('')
const formNoSequence = ref(10)
const selectedForm = computed(() => forms.value.find((row) => row.id === selectedFormId.value) || null)
const selectedItem = computed(() => selectedForm.value?.items.find((row) => row.qcItemNo === selectedItemNo.value) || null)
const visibleForms = computed(() => forms.value.filter((row) => {
  if (row.status !== activeStatus.value) return false
  const needle = search.value.trim().toLowerCase()
  if (needle && ![row.qcFormNo, row.lotNo, row.productNo, row.productName, row.customerNo].some((value) => String(value || '').toLowerCase().includes(needle))) return false
  if (useCreateDate.value && fromDate.value && row.createDate.slice(0, 10) < fromDate.value) return false
  if (useCreateDate.value && toDate.value && row.createDate.slice(0, 10) > toDate.value) return false
  return true
}))
const availableItems = computed(() => itemCatalog.filter((row) => !draft.value?.items.some((mapped) => mapped.qcItemNo === row.qcItemNo)))
const availableSelectedItems = computed(() => itemCatalog.filter((row) => !selectedForm.value?.items.some((mapped) => mapped.qcItemNo === row.qcItemNo)))
const pendingCount = computed(() => selectedForm.value?.items.filter((row) => row.status === 0).length || 0)
const canReturnToUnconfirmed = computed(() => selectedForm.value?.items.every((row) => row.status === 0) ?? false)

function setMessage(text: string, kind: typeof messageKind.value = 'info'): void { message.value = text; messageKind.value = kind }
function setStatus(status: FormStatus): void {
  activeStatus.value = status
  const first = forms.value.find((row) => row.status === status)
  selectedFormId.value = first?.id || ''
  selectedItemNo.value = first?.items[0]?.qcItemNo || ''
  selectedCatalogItem.value = ''
  resultMode.value = false
}
function selectForm(row: FinalForm): void { selectedFormId.value = row.id; selectedItemNo.value = row.items[0]?.qcItemNo || ''; selectedCatalogItem.value = ''; resultMode.value = false }
function selectItem(qcItemNo: string): void { selectedItemNo.value = qcItemNo }
function createDraft(mode: 'add' | 'edit' | 'copy', source?: FinalForm): void {
  const base = source
  draftMode.value = mode
  draft.value = {
    id: mode === 'edit' ? base?.id : undefined,
    qcFormNo: mode === 'edit' ? (base?.qcFormNo || '') : '',
    productNo: base?.productNo || '', productName: base?.productName || '', opNo: base?.opNo || '', productVersion: base?.productVersion || '',
    lotNo: base?.lotNo || '', customerNo: base?.customerNo || '', qty: base?.qty || 0, inspDocLink: base?.inspDocLink || '',
    callBackType: mode === 'edit' ? (base?.callBackType || '') : '', erpSourceNo: '', erpSourceSeq: '',
    dataStamp: base?.dataStamp || 0,
    items: mode === 'edit' ? (base?.items || []).map(cloneItem) : (base?.items || []).map((row) => ({ ...cloneItem(row), status: 0, result: '待檢', serial: '', note: '' })),
  }
  if (mode === 'add') setMessage('新增終檢單會依產品／工序載入 DEV QC Item 對應。')
}
function closeDraft(): void { draftMode.value = null; draft.value = null }
function populateFromLot(lotNo: string): void {
  const lot = lotCatalog.find((row) => row.lotNo === lotNo)
  const product = products.find((row) => row.productNo === lot?.productNo)
  if (!draft.value || !lot || !product) return
  draft.value.lotNo = lot.lotNo; draft.value.productNo = product.productNo; draft.value.productName = product.productName
  draft.value.opNo = lot.opNo; draft.value.productVersion = lot.productVersion; draft.value.customerNo = lot.customerNo; draft.value.qty = lot.qty
  draft.value.items = product.qcItems.map((key) => cloneItem(itemCatalog.find((row) => row.qcItemNo === key) || itemCatalog[0]))
  setMessage('已從有效 DEV 批號帶入產品、工序、版本、客戶、數量及預設 QC Item。')
}
function onLotChange(event: Event): void { populateFromLot((event.target as HTMLSelectElement).value) }
function onProductChange(event: Event): void {
  const product = products.find((row) => row.productNo === (event.target as HTMLSelectElement).value)
  if (!draft.value || !product) return
  draft.value.productNo = product.productNo; draft.value.productName = product.productName; draft.value.opNo = product.opNo
  draft.value.productVersion = product.productVersion; draft.value.customerNo = product.customerNo
  draft.value.items = product.qcItems.map((key) => cloneItem(itemCatalog.find((row) => row.qcItemNo === key) || itemCatalog[0]))
}
function assignDevFormNo(): void { if (draft.value) draft.value.qcFormNo = `DEV-FQC-261123-${String(formNoSequence.value++).padStart(3, '0')}` }
function addDraftItem(): void {
  if (!draft.value || !selectedCatalogItem.value) return
  const item = itemCatalog.find((row) => row.qcItemNo === selectedCatalogItem.value)
  if (item) draft.value.items.push(cloneItem(item))
  selectedCatalogItem.value = ''
}
function removeDraftItem(qcItemNo: string): void { if (draft.value) draft.value.items = draft.value.items.filter((row) => row.qcItemNo !== qcItemNo) }
function saveDraft(): void {
  const current = draft.value
  if (!current) return
  if (!current.qcFormNo.trim()) return setMessage('請先取得 DEV 終檢單號。', 'error')
  if (!current.productNo || !current.opNo || !current.productVersion) return setMessage('產品、工序與產品版本不可空白。', 'error')
  if (!Number.isFinite(Number(current.qty)) || Number(current.qty) <= 0) return setMessage('檢驗數量必須大於 0。', 'error')
  if (draftMode.value === 'edit') {
    const original = forms.value.find((row) => row.id === current.id)
    if (!original || original.dataStamp !== current.dataStamp) return setMessage('DEV DataStamp 已變更，請重新載入後再編輯。', 'error')
    forms.value = forms.value.map((row) => row.id === current.id ? { ...row, ...current, qty: Number(current.qty), reviser: props.currentUserNo?.trim() || 'DEV-USER', reviseDate: nowText(), dataStamp: row.dataStamp + 1, items: current.items.map(cloneItem) } as FinalForm : row)
    setMessage('未確認終檢單已於 DEV 假資料中修改。', 'success')
  } else {
    if (forms.value.some((row) => row.qcFormNo === current.qcFormNo)) return setMessage('DEV 終檢單號已存在。', 'error')
    const created: FinalForm = { ...current, id: current.qcFormNo, status: 0, qty: Number(current.qty), creator: props.currentUserNo?.trim() || 'DEV-USER', createDate: nowText(), reviser: '', reviseDate: '', finishTime: '', dataStamp: 1, items: current.items.map(cloneItem) }
    forms.value.unshift(created); activeStatus.value = 0; selectedFormId.value = created.id; selectedItemNo.value = created.items[0]?.qcItemNo || ''
    setMessage(`${draftMode.value === 'copy' ? '已複製' : '已新增'}未確認終檢單 ${created.qcFormNo}。`, 'success')
  }
  closeDraft()
}
function deleteSelected(): void {
  if (!selectedForm.value || activeStatus.value !== 0) return
  const deleted = selectedForm.value.qcFormNo
  forms.value = forms.value.filter((row) => row.id !== selectedFormId.value)
  selectedFormId.value = visibleForms.value[0]?.id || ''
  selectedItemNo.value = ''
  setMessage(`已從 DEV 記憶體刪除未確認單 ${deleted} 及其 QC Item map。`, 'success')
}
function confirmSelected(): void {
  const form = selectedForm.value
  if (!form || activeStatus.value !== 0) return
  if (form.items.length === 0) return setMessage('確認前至少需要一個 QC Item。', 'error')
  if (form.status !== 0) return setMessage('目前單據狀態已變更，不能確認。', 'error')
  form.status = 1; form.reviser = props.currentUserNo?.trim() || 'DEV-USER'; form.reviseDate = nowText(); form.dataStamp += 1; activeStatus.value = 1; selectedFormId.value = form.id; selectedItemNo.value = form.items.find((row) => row.status === 0)?.qcItemNo || ''
  setMessage('已確認，單據進入待檢。', 'success')
}
function addMapItem(): void {
  if (!selectedForm.value || selectedForm.value.status !== 0 || !selectedCatalogItem.value) return
  const item = itemCatalog.find((row) => row.qcItemNo === selectedCatalogItem.value)
  if (item) selectedForm.value.items.push(cloneItem(item))
  selectedCatalogItem.value = ''
}
function removeMapItem(qcItemNo: string): void {
  if (!selectedForm.value || selectedForm.value.status !== 0) return
  selectedForm.value.items = selectedForm.value.items.filter((row) => row.qcItemNo !== qcItemNo)
  selectedItemNo.value = selectedForm.value.items[0]?.qcItemNo || ''
}
function beginResult(): void {
  if (!selectedForm.value || !selectedItem.value || activeStatus.value !== 1 || selectedItem.value.status !== 0) return
  resultValue.value = '合格'; resultNote.value = ''; resultMode.value = true
}
function saveResult(): void {
  if (!selectedForm.value || !selectedItem.value || !resultMode.value) return
  const item = selectedItem.value
  item.status = 1; item.result = resultValue.value; item.note = resultNote.value.trim() || 'DEV 假 SPC 結果'; item.serial = `DEV-SPC-${String(Date.now()).slice(-6)}`
  const form = selectedForm.value
  resultMode.value = false
  if (form.items.every((row) => row.status === 1)) {
    form.status = 2; form.finishTime = nowText(); form.dataStamp += 1; activeStatus.value = 2; selectedFormId.value = form.id
    setMessage('所有 QC Item 已完成，終檢單自動進入檢驗完成。', 'success')
  } else setMessage(`QC Item ${item.qcItemNo} 已記錄 DEV 結果，尚有 ${pendingCount.value} 項待檢。`, 'success')
}
function returnToUnconfirmed(): void {
  const form = selectedForm.value
  if (!form || activeStatus.value !== 1 || !canReturnToUnconfirmed.value) return
  form.status = 0; form.reviser = props.currentUserNo?.trim() || 'DEV-USER'; form.reviseDate = nowText(); form.dataStamp += 1; activeStatus.value = 0; selectedFormId.value = form.id
  setMessage('此單尚無 QC Item 開始檢驗，已退回未確認。', 'success')
}
function finalizeForm(status: 3 | 4): void {
  const form = selectedForm.value
  if (!form || activeStatus.value !== 2) return
  if (form.status !== 2) return setMessage('目前單據狀態已變更，不能結案。', 'error')
  if (form.callBackType) setMessage(`DEV 已模擬 ${form.callBackType} ${status === 3 ? '允收' : '拒收'} callback 成功；沒有呼叫外部服務。`, 'success')
  else setMessage(`DEV 已模擬終檢單${status === 3 ? '允收' : '拒收'}。`, 'success')
  form.status = status; form.reviser = props.currentUserNo?.trim() || 'DEV-USER'; form.reviseDate = nowText(); form.dataStamp += 1; activeStatus.value = status; selectedFormId.value = form.id
}
function copySelected(): void { if (selectedForm.value) createDraft('copy', selectedForm.value) }
</script>

<template>
  <main class="fqc-page" aria-label="終檢檢驗單 DEV">
    <header class="fqc-title"><div><p class="eyebrow">SPC / FINAL QUALITY CONTROL</p><h1>終檢檢驗單</h1><p>WinForms SC104 對應頁 · DEV 假資料工作區</p></div><button type="button" class="quiet" @click="emit('close')">關閉</button></header>
    <section class="query-panel">
      <label class="query-search">單號／批號／產品<input v-model="search" type="search" placeholder="輸入關鍵字" /></label>
      <label class="check-label"><input v-model="useCreateDate" type="checkbox" />建立日期</label>
      <label>起日<input v-model="fromDate" type="date" :disabled="!useCreateDate" /></label>
      <label>迄日<input v-model="toDate" type="date" :disabled="!useCreateDate" /></label>
      <button type="button" class="primary" @click="setMessage(`已查詢 ${visibleForms.length} 筆 DEV ${statusName(activeStatus)} 單據。`, 'success')">查詢</button>
      <span class="query-source">WinForms 以建立日期區間查詢</span>
    </section>
    <nav class="status-tabs" aria-label="檢驗單狀態">
      <button v-for="tab in statusTabs" :key="tab.status" type="button" :class="{ active: activeStatus === tab.status }" @click="setStatus(tab.status)">{{ tab.label }} <span>{{ forms.filter((row) => row.status === tab.status).length }}</span></button>
    </nav>
    <section class="workspace">
      <div class="list-pane">
        <div class="pane-heading"><div><h2>{{ statusName(activeStatus) }}清單</h2><span>{{ visibleForms.length }} 筆</span></div>
          <div v-if="activeStatus === 0" class="actions"><button class="primary" @click="createDraft('add')">新增</button><button :disabled="!selectedForm" @click="createDraft('edit', selectedForm || undefined)">編輯</button><button :disabled="!selectedForm" @click="copySelected">複製</button><button class="danger" :disabled="!selectedForm" @click="deleteSelected">刪除</button><button class="primary" :disabled="!selectedForm" @click="confirmSelected">確認</button></div>
          <div v-else-if="activeStatus === 1" class="actions"><button class="primary" :disabled="!selectedItem || selectedItem.status === 1" @click="beginResult">SPC 檢驗</button><button :disabled="!canReturnToUnconfirmed" @click="returnToUnconfirmed">退回未確認</button><button :disabled="!selectedForm" @click="copySelected">複製</button></div>
          <div v-else-if="activeStatus === 2" class="actions"><button class="primary" :disabled="!selectedForm" @click="finalizeForm(3)">允收</button><button class="danger" :disabled="!selectedForm" @click="finalizeForm(4)">拒收</button><button :disabled="!selectedForm" @click="copySelected">複製</button></div>
          <div v-else class="actions"><button :disabled="!selectedForm" @click="copySelected">複製</button></div>
        </div>
        <div class="table-wrap"><table><thead><tr><th>終檢單號</th><th>批號</th><th>產品</th><th>版本／工序</th><th>數量</th><th>客戶</th><th>建立時間</th></tr></thead><tbody>
          <tr v-for="row in visibleForms" :key="row.id" :class="{ selected: row.id === selectedFormId }" tabindex="0" @click="selectForm(row)" @keydown.enter="selectForm(row)"><td class="mono">{{ row.qcFormNo }}</td><td>{{ row.lotNo || '—' }}</td><td>{{ row.productNo }}<small>{{ row.productName }}</small></td><td>{{ row.productVersion }}<small>{{ row.opNo }}</small></td><td>{{ row.qty }}</td><td>{{ row.customerNo || '—' }}</td><td>{{ row.createDate }}</td></tr>
          <tr v-if="visibleForms.length === 0"><td colspan="7" class="empty">此狀態沒有符合條件的 DEV 單據。</td></tr>
        </tbody></table></div>
      </div>
      <aside class="detail-pane">
        <template v-if="selectedForm">
          <div class="pane-heading"><div><h2>終檢單明細</h2><span class="status-pill" :data-status="selectedForm.status">{{ statusName(selectedForm.status) }}</span></div><span class="mono">{{ selectedForm.qcFormNo }}</span></div>
          <dl class="form-facts"><div><dt>產品</dt><dd>{{ selectedForm.productNo }} · {{ selectedForm.productName }}</dd></div><div><dt>批號</dt><dd>{{ selectedForm.lotNo || '—' }}</dd></div><div><dt>產品版本／工序</dt><dd>{{ selectedForm.productVersion }} / {{ selectedForm.opNo }}</dd></div><div><dt>數量／客戶</dt><dd>{{ selectedForm.qty }} / {{ selectedForm.customerNo || '—' }}</dd></div><div><dt>檢驗文件</dt><dd>{{ selectedForm.inspDocLink || '—' }}</dd></div><div><dt>CallBackType</dt><dd>{{ selectedForm.callBackType || 'SPC 開單' }}</dd></div><div><dt>建立人／時間</dt><dd>{{ selectedForm.creator }} · {{ selectedForm.createDate }}</dd></div><div><dt>檢驗完成</dt><dd>{{ selectedForm.finishTime || '—' }}</dd></div><div><dt>狀態修改人／時間</dt><dd>{{ selectedForm.reviser || '—' }} {{ selectedForm.reviseDate }}</dd></div></dl>
          <div class="item-heading"><div><h3>QC Item 對應</h3><span>{{ selectedForm.items.length }} 項 · {{ pendingCount }} 項待檢</span></div>
            <div v-if="activeStatus === 0" class="map-actions"><select v-model="selectedCatalogItem" aria-label="選擇 QC Item"><option value="">選擇 QC Item</option><option v-for="row in availableSelectedItems" :key="row.qcItemNo" :value="row.qcItemNo">{{ row.qcItemNo }} · {{ row.name }}</option></select><button :disabled="!selectedCatalogItem" @click="addMapItem">加入</button></div>
          </div>
          <div class="item-list"><div v-for="row in selectedForm.items" :key="row.qcItemNo" class="item-row" :class="{ chosen: selectedItemNo === row.qcItemNo }" role="button" tabindex="0" @click="selectItem(row.qcItemNo)" @keydown.enter="selectItem(row.qcItemNo)"><span class="item-state" :data-done="row.status === 1">{{ row.status === 1 ? '完成' : '待檢' }}</span><span class="item-name"><b>{{ row.qcItemNo }}</b><small>{{ row.name }} · {{ row.type }}</small></span><span class="item-result">{{ row.result }}</span><button v-if="activeStatus === 0" type="button" class="remove" aria-label="移除 QC Item" @click.stop="removeMapItem(row.qcItemNo)">移除</button></div><p v-if="selectedForm.items.length === 0" class="empty">尚未對應 QC Item；產品／工序選擇或手動加入。</p></div>
          <section v-if="activeStatus === 1 && selectedItem" class="inspection-card"><div><h3>選取檢驗項目</h3><p>{{ selectedItem.qcItemNo }} · {{ selectedItem.name }}</p></div><p v-if="selectedItem.status === 1" class="result-summary">{{ selectedItem.result }} · {{ selectedItem.serial }}</p><div v-else-if="resultMode" class="result-editor"><label>DEV 判定<select v-model="resultValue"><option>合格</option><option>不合格</option><option>警示</option></select></label><label>備註<input v-model="resultNote" placeholder="可留空" /></label><div><button @click="resultMode = false">取消</button><button class="primary" @click="saveResult">記錄結果</button></div></div><p v-else class="hint">按「SPC 檢驗」開啟假量測輸入；不連接量測服務或設備。</p></section>
          <section v-if="activeStatus === 2" class="inspection-card"><h3>檢驗結果待處置</h3><p>QC Item 全部完成後，WinForms 允許將單據允收或拒收。ERP Workflow／T100 callback 在 DEV 僅模擬成功提示。</p></section>
        </template>
        <div v-else class="empty empty-panel">選取一筆終檢單查看 QC Item 與狀態。</div>
      </aside>
    </section>
    <section v-if="draft && draftMode" class="draft-panel">
      <div class="pane-heading"><div><h2>{{ draftMode === 'add' ? '新增終檢單' : draftMode === 'edit' ? '編輯終檢單' : '複製終檢單' }}</h2><span>來源欄位依 frmFQCFormDef；所有輸入皆為 DEV fixture</span></div><button @click="closeDraft">關閉</button></div>
      <div class="draft-grid"><label>終檢單號<div class="inline-field"><input v-model="draft.qcFormNo" :readonly="draftMode !== 'edit'" /><button v-if="draftMode !== 'edit'" type="button" @click="assignDevFormNo">取得 DEV 單號</button></div></label>
        <label>有效批號<select v-model="draft.lotNo" @change="onLotChange"><option value="">選擇 DEV 批號</option><option v-for="lot in lotCatalog" :key="lot.lotNo" :value="lot.lotNo">{{ lot.lotNo }} · {{ lot.productNo }}</option></select></label>
        <label>產品<select v-model="draft.productNo" @change="onProductChange"><option value="">選擇產品</option><option v-for="product in products" :key="product.productNo" :value="product.productNo">{{ product.productNo }} · {{ product.productName }}</option></select></label>
        <label>產品名稱<input v-model="draft.productName" /></label><label>工序<input v-model="draft.opNo" /></label><label>產品版本<input v-model="draft.productVersion" /></label>
        <label>客戶<input v-model="draft.customerNo" /></label><label>檢驗數量<input v-model.number="draft.qty" type="number" min="0" step="0.001" /></label><label>檢驗文件連結<input v-model="draft.inspDocLink" /></label>
      </div>
      <div class="item-heading"><div><h3>初始 QC Item</h3><span>產品選擇會套用該產品 DEV QC Item 對應</span></div><div class="map-actions"><select v-model="selectedCatalogItem"><option value="">選擇 QC Item</option><option v-for="row in availableItems" :key="row.qcItemNo" :value="row.qcItemNo">{{ row.qcItemNo }} · {{ row.name }}</option></select><button @click="addDraftItem">加入</button></div></div>
      <div class="draft-items"><span v-for="row in draft.items" :key="row.qcItemNo" class="draft-chip">{{ row.qcItemNo }} · {{ row.name }} <button type="button" aria-label="移除 QC Item" @click="removeDraftItem(row.qcItemNo)">×</button></span><span v-if="draft.items.length === 0" class="hint">尚未載入品管項目</span></div>
      <div class="draft-footer"><span>儲存後預設狀態為未確認；確認需至少一個 QC Item。</span><button @click="closeDraft">取消</button><button class="primary" @click="saveDraft">儲存 DEV 單據</button></div>
    </section>
    <footer class="status-message" :data-kind="messageKind"><span>{{ message }}</span><span>來源操作只存於記憶體 · DataStamp {{ selectedForm?.dataStamp ?? '—' }}</span></footer>
  </main>
</template>

<style scoped>
.fqc-page { min-height: 100%; padding: 14px; box-sizing: border-box; color: #292820; background: #f4f2e9; font: inherit; display: flex; flex-direction: column; gap: 10px; }
.fqc-title,.pane-heading,.item-heading,.draft-footer { display:flex; align-items:center; justify-content:space-between; gap:12px; }
.fqc-title { padding: 12px 14px; background:#fffdf7; border:1px solid #d8d4c7; border-radius:5px; }
.fqc-title h1,.pane-heading h2,.item-heading h3,.inspection-card h3 { margin:0; font-size:16px; }
.fqc-title p,.pane-heading span,.item-heading span,.inspection-card p { margin:4px 0 0; color:#777366; font-size:12px; }
.fqc-title .eyebrow { margin:0 0 4px; color:#827510; font-size:10px; letter-spacing:.12em; }
.query-panel { display:flex; align-items:end; gap:8px; flex-wrap:wrap; padding:10px 12px; border:1px solid #d8d4c7; background:#fffdf7; border-radius:4px; }
.query-panel label,.draft-grid label,.result-editor label { display:flex; flex-direction:column; gap:4px; color:#625f53; font-size:11px; }
.query-panel input,.draft-grid input,.draft-grid select,.result-editor input,.result-editor select,.map-actions select { min-height:30px; border:1px solid #c9c4b5; background:white; border-radius:3px; padding:4px 7px; color:#292820; font:inherit; }
.query-search { min-width:220px; flex:1; }.check-label { display:flex!important; flex-direction:row!important; align-items:center; min-height:30px; }.query-source { color:#858173; font-size:11px; padding:0 4px 7px; }
button { min-height:29px; border:1px solid #bdb8aa; border-radius:3px; padding:4px 9px; background:#fff; color:#343228; font:inherit; font-size:12px; cursor:pointer; }
button:hover:not(:disabled) { border-color:#817619; background:#fffbea; }button:disabled { opacity:.45; cursor:not-allowed; }.primary { color:#fff; background:#3e3b30; border-color:#3e3b30; }.primary:hover:not(:disabled) { color:#fff; background:#605c45; }.danger { color:#8c3028; }
.quiet { align-self:flex-start; }.status-tabs { display:flex; gap:2px; padding:0 4px; border-bottom:1px solid #b9b4a5; }.status-tabs button { border:0; border-radius:4px 4px 0 0; color:#5f5b4e; background:transparent; padding:8px 13px; }.status-tabs button.active { color:#28271f; background:#fffdf7; box-shadow:0 -2px 0 #b6a428 inset; font-weight:600; }.status-tabs span { display:inline-grid; min-width:17px; height:17px; place-items:center; margin-left:3px; border-radius:10px; background:#ebe8dd; font-size:10px; }
.workspace { display:grid; grid-template-columns:minmax(0,1.65fr) minmax(360px,1fr); gap:10px; min-height:350px; flex:1; }.list-pane,.detail-pane,.draft-panel { min-width:0; border:1px solid #d8d4c7; border-radius:4px; background:#fffdf7; padding:10px; }.detail-pane { display:flex; flex-direction:column; gap:10px; }.pane-heading h2 { margin-bottom:2px; }.pane-heading>div:first-child span { font-size:11px; }.actions,.map-actions { display:flex; gap:5px; flex-wrap:wrap; align-items:center; }.table-wrap { overflow:auto; margin-top:9px; max-height:430px; }table { width:100%; border-collapse:collapse; white-space:nowrap; font-size:11px; }th { position:sticky; top:0; z-index:1; text-align:left; color:#5d594d; background:#efede3; font-weight:600; }th,td { padding:7px 8px; border-bottom:1px solid #e4e1d8; }tbody tr { cursor:pointer; }tbody tr:hover,tbody tr.selected { background:#f8f3cf; }td small { display:block; margin-top:2px; color:#858173; font-size:10px; }.mono { font-family:Consolas,monospace; }.empty { padding:20px; color:#837f71; text-align:center; }.empty-panel { margin:auto; }
.form-facts { display:grid; grid-template-columns:1fr 1fr; gap:6px 12px; margin:0; padding:9px; background:#f6f4eb; border:1px solid #e3dfd3; }.form-facts div { min-width:0; }.form-facts dt { color:#858173; font-size:10px; }.form-facts dd { overflow-wrap:anywhere; margin:2px 0 0; font-size:11px; }.status-pill { display:inline-block; padding:3px 7px; border-radius:10px; background:#ece9de; color:#615d50; }.status-pill[data-status="2"] { background:#f8edb8; }.status-pill[data-status="3"] { background:#dcebd8; }.status-pill[data-status="4"] { background:#f0dcd6; }
.item-heading { border-bottom:1px solid #dedacd; padding-bottom:6px; }.item-list { display:flex; flex-direction:column; gap:4px; }.item-row { display:flex; width:100%; align-items:center; text-align:left; gap:8px; min-height:43px; padding:5px 7px; border-color:#e0ddd3; }.item-row.chosen { border-color:#a99b34; background:#fbf6d9; }.item-state { color:#766f54; font-size:10px; }.item-state[data-done="true"] { color:#3e7041; }.item-name { flex:1; }.item-name b,.item-name small { display:block; }.item-name b { font-size:11px; }.item-name small { margin-top:2px; color:#7e7a6d; font-size:10px; }.item-result { color:#5e594a; font-size:11px; }.remove { min-height:23px; padding:2px 5px; color:#8c3028; }.inspection-card { padding:9px; border:1px solid #e0ddd3; background:#faf8ef; }.result-editor { display:grid; grid-template-columns:1fr 1fr; gap:8px; margin-top:9px; }.result-editor>div { grid-column:1/-1; display:flex; justify-content:flex-end; gap:6px; }.hint { color:#858173; font-size:11px; }.result-summary { padding:7px; border-radius:3px; background:#e8efdf; }.draft-panel { display:flex; flex-direction:column; gap:10px; }.draft-grid { display:grid; grid-template-columns:repeat(3,minmax(0,1fr)); gap:8px; }.inline-field { display:flex; gap:4px; }.inline-field input { min-width:80px; flex:1; }.draft-items { display:flex; gap:6px; flex-wrap:wrap; min-height:26px; }.draft-chip { display:inline-flex; align-items:center; gap:5px; padding:4px 7px; border:1px solid #ddd8c8; border-radius:12px; background:#f5f2e8; font-size:11px; }.draft-chip button { min-height:20px; width:20px; padding:0; border:0; font-size:16px; }.draft-footer { justify-content:flex-end; padding-top:8px; border-top:1px solid #e2dfd6; }.draft-footer span { margin-right:auto; color:#858173; font-size:11px; }.status-message { display:flex; justify-content:space-between; gap:10px; padding:8px 10px; border:1px solid #ddd8c8; border-radius:3px; background:#efede4; color:#625f53; font-size:11px; }.status-message[data-kind="success"] { color:#3e7041; }.status-message[data-kind="error"] { color:#9a382e; }
@media (max-width:1000px) { .workspace { grid-template-columns:1fr; }.detail-pane { min-height:330px; }.draft-grid { grid-template-columns:repeat(2,minmax(0,1fr)); } }
@media (max-width:620px) { .fqc-page { padding:7px; }.query-search { flex-basis:100%; }.workspace { min-height:0; }.draft-grid,.form-facts { grid-template-columns:1fr; }.pane-heading { align-items:flex-start; flex-direction:column; }.actions { width:100%; }.status-tabs { overflow:auto; }.status-tabs button { white-space:nowrap; }.status-message { flex-direction:column; } }
</style>
