<script setup lang="ts">
import { computed, ref } from 'vue'

type FormStatus = 0 | 1 | 2 | 3 | 4 | 5 | 6
interface QcItem { qcItemNo: string; name: string; type: number; status: 0 | 1; result: string; spcSerial: string }
interface IncomingForm {
  id: string; qcFormNo: string; incomingNo: string; materialNo: string; materialName: string; qty: number; vendorNo: string; vendorName: string
  materialLotNo: string; oriVendor: string; oriVenLot: string; inspDocLink: string; status: FormStatus; callBackType: string
  creator: string; createdAt: string; dataStamp: number; passQty: number; returnQty: number; finishTime: string; rejudgeAt: string; items: QcItem[]
}
interface FormDraft extends Omit<IncomingForm, 'id' | 'createdAt' | 'dataStamp' | 'passQty' | 'returnQty' | 'finishTime' | 'rejudgeAt' | 'items'> { items: QcItem[]; id?: string; dataStamp?: number }

const props = defineProps<{ developmentPreview: boolean; currentUserNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const statusTabs: Array<{ status: FormStatus; label: string }> = [
  { status: 0, label: '未確認' }, { status: 1, label: '待驗' }, { status: 2, label: '檢驗完成' },
  { status: 3, label: '已收貨' }, { status: 4, label: '已驗退' }, { status: 5, label: '特採' }, { status: 6, label: '作廢' },
]
const itemCatalog = [
  { qcItemNo: 'IQC-V-DIM-01', name: '尺寸檢驗', type: 1 }, { qcItemNo: 'IQC-A-VIS-02', name: '外觀檢驗', type: 0 },
  { qcItemNo: 'IQC-G-PKG-03', name: '包裝檢驗群組', type: 2 }, { qcItemNo: 'IQC-V-CERT-04', name: '材質證明核對', type: 1 },
]
const materials = [
  { materialNo: 'MTL-AL-6061', materialName: '鋁合金板 6061', items: ['IQC-V-DIM-01', 'IQC-A-VIS-02'] },
  { materialNo: 'MTL-RES-PA66', materialName: 'PA66 工程塑料', items: ['IQC-V-DIM-01', 'IQC-G-PKG-03'] },
  { materialNo: 'MTL-ELEC-01', materialName: '電子連接器', items: ['IQC-A-VIS-02', 'IQC-V-CERT-04'] },
]
const vendors = [
  { vendorNo: 'V-1001', vendorName: '東源材料' }, { vendorNo: 'V-1002', vendorName: '宏達工業' }, { vendorNo: 'V-1003', vendorName: '新曜電子' },
]
const item = (qcItemNo: string, status: 0 | 1 = 0, result = '', spcSerial = ''): QcItem => {
  const definition = itemCatalog.find((row) => row.qcItemNo === qcItemNo) || itemCatalog[0]
  return { qcItemNo: definition.qcItemNo, name: definition.name, type: definition.type, status, result, spcSerial }
}
const makeForm = (data: Partial<IncomingForm> & Pick<IncomingForm, 'qcFormNo' | 'materialNo' | 'materialName' | 'qty' | 'status' | 'items'>): IncomingForm => ({
  id: data.qcFormNo, qcFormNo: data.qcFormNo, incomingNo: '', materialNo: data.materialNo, materialName: data.materialName, qty: data.qty,
  vendorNo: '', vendorName: '', materialLotNo: '', oriVendor: '', oriVenLot: '', inspDocLink: '', status: data.status, callBackType: '',
  creator: 'DEV-USER', createdAt: new Date().toISOString(), dataStamp: 1, passQty: 0, returnQty: 0,
  finishTime: '', rejudgeAt: '', items: data.items, ...data,
})
const forms = ref<IncomingForm[]>([
  makeForm({ qcFormNo: 'DEV-IQC-261123-001', incomingNo: 'IN-PO-88421', materialNo: materials[0].materialNo, materialName: materials[0].materialName, qty: 120, vendorNo: vendors[0].vendorNo, vendorName: vendors[0].vendorName, materialLotNo: 'AL-2611-A', status: 0, items: [item('IQC-V-DIM-01'), item('IQC-A-VIS-02')] }),
  makeForm({ qcFormNo: 'DEV-IQC-261123-002', incomingNo: 'IN-PO-88436', materialNo: materials[1].materialNo, materialName: materials[1].materialName, qty: 60, vendorNo: vendors[1].vendorNo, vendorName: vendors[1].vendorName, materialLotNo: 'PA-2611-07', status: 1, callBackType: 'T100', items: [item('IQC-V-DIM-01')] }),
  makeForm({ qcFormNo: 'DEV-IQC-261122-008', incomingNo: 'IN-PO-88370', materialNo: materials[2].materialNo, materialName: materials[2].materialName, qty: 240, vendorNo: vendors[2].vendorNo, vendorName: vendors[2].vendorName, materialLotNo: 'CN-2611-11', status: 2, callBackType: 'Workflow', passQty: 232, returnQty: 8, items: [item('IQC-A-VIS-02', 1, 'Pass', 'DEV-SPC-001'), item('IQC-V-CERT-04', 1, 'Warning', 'DEV-SPC-002')] }),
  makeForm({ qcFormNo: 'DEV-IQC-261121-005', incomingNo: 'IN-PO-88250', materialNo: materials[0].materialNo, materialName: materials[0].materialName, qty: 45, vendorNo: vendors[0].vendorNo, vendorName: vendors[0].vendorName, materialLotNo: 'AL-2611-B', status: 3, passQty: 45, items: [item('IQC-V-DIM-01', 1, 'Pass', 'DEV-SPC-003')] }),
  makeForm({ qcFormNo: 'DEV-IQC-261120-003', incomingNo: 'IN-PO-88206', materialNo: materials[1].materialNo, materialName: materials[1].materialName, qty: 25, vendorNo: vendors[1].vendorNo, vendorName: vendors[1].vendorName, materialLotNo: 'PA-2611-03', status: 4, passQty: 0, returnQty: 25, items: [item('IQC-V-DIM-01', 1, 'Fail', 'DEV-SPC-004')] }),
  makeForm({ qcFormNo: 'DEV-IQC-261119-010', incomingNo: 'IN-PO-88190', materialNo: materials[2].materialNo, materialName: materials[2].materialName, qty: 32, vendorNo: vendors[2].vendorNo, vendorName: vendors[2].vendorName, materialLotNo: 'CN-2611-08', status: 5, callBackType: 'T100', passQty: 30, returnQty: 2, items: [item('IQC-A-VIS-02', 1, 'Concession', 'DEV-SPC-005')] }),
  makeForm({ qcFormNo: 'DEV-IQC-261118-004', incomingNo: 'IN-PO-88112', materialNo: materials[0].materialNo, materialName: materials[0].materialName, qty: 10, vendorNo: vendors[0].vendorNo, vendorName: vendors[0].vendorName, materialLotNo: 'AL-2611-X', status: 6, items: [item('IQC-V-DIM-01')] }),
])

const activeStatus = ref<FormStatus>(0)
const selectedFormId = ref('DEV-IQC-261123-001')
const selectedItemNo = ref('IQC-V-DIM-01')
const reportedPassQty = ref(0)
const reportedReturnQty = ref(0)
const search = ref('')
const useCreateDate = ref(false)
const fromDate = ref('')
const toDate = ref('')
const message = ref('DEV 假資料；所有操作只保存在目前頁面。')
const messageKind = ref<'info' | 'success' | 'warning' | 'error'>('info')
const draftMode = ref<'add' | 'edit' | 'copy' | null>(null)
const draft = ref<FormDraft | null>(null)
const newMapItemNo = ref('')
const formNoSequence = ref(20)
const selectedForm = computed(() => forms.value.find((row) => row.id === selectedFormId.value) || null)
const selectedItem = computed(() => selectedForm.value?.items.find((row) => row.qcItemNo === selectedItemNo.value) || null)
const visibleForms = computed(() => forms.value.filter((row) => {
  if (row.status !== activeStatus.value) return false
  const needle = search.value.trim().toLowerCase()
  if (needle && ![row.qcFormNo, row.incomingNo, row.materialNo, row.materialName, row.materialLotNo, row.vendorNo, row.vendorName].some((value) => String(value || '').toLowerCase().includes(needle))) return false
  const created = new Date(row.createdAt).getTime()
  if (useCreateDate.value && fromDate.value && created < new Date(fromDate.value + 'T00:00:00').getTime()) return false
  if (useCreateDate.value && toDate.value && created > new Date(toDate.value + 'T23:59:59').getTime()) return false
  return true
}))
const availableItems = computed(() => itemCatalog.filter((row) => !draft.value?.items.some((mapped) => mapped.qcItemNo === row.qcItemNo)))
const selectedFormAvailableItems = computed(() => itemCatalog.filter((row) => !selectedForm.value?.items.some((mapped) => mapped.qcItemNo === row.qcItemNo)))
const pendingItemCount = computed(() => selectedForm.value?.items.filter((row) => row.status === 0).length || 0)
const showQuantityReport = computed(() => activeStatus.value === 2 && Boolean(selectedForm.value?.callBackType))

function setMessage(text: string, kind: typeof messageKind.value = 'info'): void { message.value = text; messageKind.value = kind }
function setStatus(status: FormStatus): void {
  activeStatus.value = status
  const first = forms.value.find((row) => row.status === status)
  selectedFormId.value = first?.id || ''
  selectedItemNo.value = first?.items[0]?.qcItemNo || ''
  reportedPassQty.value = first?.passQty || 0
  reportedReturnQty.value = first?.returnQty || 0
}
function moveFormToStatus(form: IncomingForm, status: FormStatus): void {
  form.status = status
  activeStatus.value = status
  selectedFormId.value = form.id
  selectedItemNo.value = form.items[0]?.qcItemNo || ''
  reportedPassQty.value = form.passQty
  reportedReturnQty.value = form.returnQty
}
function selectForm(row: IncomingForm): void {
  selectedFormId.value = row.id
  selectedItemNo.value = row.items[0]?.qcItemNo || ''
  reportedPassQty.value = row.passQty
  reportedReturnQty.value = row.returnQty
}
function selectItem(qcItemNo: string): void { selectedItemNo.value = qcItemNo }
function addSelectedMapItem(): void {
  if (!selectedForm.value || !newMapItemNo.value) return
  selectedForm.value.items.push(item(newMapItemNo.value))
  newMapItemNo.value = ''
}
function removeSelectedMapItem(qcItemNo: string): void {
  if (!selectedForm.value || selectedForm.value.status !== 0) return
  selectedForm.value.items = selectedForm.value.items.filter((row) => row.qcItemNo !== qcItemNo)
  selectedItemNo.value = selectedForm.value.items[0]?.qcItemNo || ''
}
function mapItemsForMaterial(materialNo: string): QcItem[] {
  const material = materials.find((row) => row.materialNo === materialNo)
  return (material?.items || []).map((qcItemNo) => item(qcItemNo))
}
function openDraft(mode: 'add' | 'edit' | 'copy', source?: IncomingForm): void {
  const base = source
  draftMode.value = mode
  draft.value = {
    id: mode === 'edit' ? base?.id : undefined,
    qcFormNo: mode === 'edit' ? (base?.qcFormNo || '') : '',
    incomingNo: base?.incomingNo || '', materialNo: base?.materialNo || '', materialName: base?.materialName || '', qty: base?.qty || 0,
    vendorNo: base?.vendorNo || '', vendorName: base?.vendorName || '', materialLotNo: base?.materialLotNo || '', oriVendor: base?.oriVendor || '',
    oriVenLot: base?.oriVenLot || '', inspDocLink: base?.inspDocLink || '', status: 0, callBackType: mode === 'edit' ? (base?.callBackType || '') : '',
    creator: base?.creator || props.currentUserNo?.trim() || 'DEV-USER', items: (base?.items || []).map((row) => item(row.qcItemNo)), dataStamp: base?.dataStamp || 0,
  }
  if (mode === 'add' && draft.value) draft.value.items = []
  if (mode === 'copy') draft.value.callBackType = ''
}
function closeDraft(): void { draftMode.value = null; draft.value = null }
function setDraftMaterial(materialNo: string): void {
  const material = materials.find((row) => row.materialNo === materialNo)
  if (!draft.value || !material) return
  draft.value.materialNo = material.materialNo
  draft.value.materialName = material.materialName
  draft.value.items = mapItemsForMaterial(material.materialNo)
  setMessage('已載入該物料的 DEV QC Item 對應；來源可選擇套用物料 QC Item。')
}
function onDraftMaterialChange(event: Event): void { setDraftMaterial((event.target as HTMLSelectElement).value) }
function onDraftVendorChange(event: Event): void { setDraftVendor((event.target as HTMLSelectElement).value) }
function setDraftVendor(vendorNo: string): void {
  const vendor = vendors.find((row) => row.vendorNo === vendorNo)
  if (draft.value) { draft.value.vendorNo = vendorNo; draft.value.vendorName = vendor?.vendorName || '' }
}
function assignDevFormNo(): void {
  if (draft.value) draft.value.qcFormNo = 'DEV-IQC-261123-' + String(formNoSequence.value++).padStart(3, '0')
}
function addMapItem(): void {
  if (!draft.value || !newMapItemNo.value) return
  draft.value.items.push(item(newMapItemNo.value))
  newMapItemNo.value = ''
}
function removeMapItem(qcItemNo: string): void { if (draft.value) draft.value.items = draft.value.items.filter((row) => row.qcItemNo !== qcItemNo) }
function saveDraft(): void {
  if (!draft.value) return
  const current = draft.value
  if (!current.qcFormNo.trim()) return setMessage('請輸入或取得 DEV 檢驗單號。', 'error')
  if (!current.materialNo || !current.materialName) return setMessage('請選擇已核准物料。', 'error')
  if (!Number.isFinite(Number(current.qty)) || Number(current.qty) <= 0) return setMessage('檢驗數量必須大於 0。', 'error')
  if (draftMode.value === 'edit') {
    const original = forms.value.find((row) => row.id === current.id)
    if (!original || original.dataStamp !== current.dataStamp) return setMessage('DEV DataStamp 已變更，請重新載入後再編輯。', 'error')
    forms.value = forms.value.map((row) => row.id === current.id ? { ...row, ...current, qty: Number(current.qty), vendorName: vendors.find((vendor) => vendor.vendorNo === current.vendorNo)?.vendorName || current.vendorName, items: current.items.map((mapped) => item(mapped.qcItemNo)), dataStamp: row.dataStamp + 1 } : row)
    setMessage('未確認檢驗單已於 DEV 假資料中修改。', 'success')
  } else {
    if (forms.value.some((row) => row.qcFormNo === current.qcFormNo)) return setMessage('DEV 檢驗單號已存在。', 'error')
    const createdAt = new Date().toISOString()
    const next: IncomingForm = { ...current, id: current.qcFormNo, qty: Number(current.qty), vendorName: vendors.find((vendor) => vendor.vendorNo === current.vendorNo)?.vendorName || current.vendorName, createdAt, dataStamp: 1, passQty: 0, returnQty: 0, finishTime: '', rejudgeAt: '', items: current.items.map((mapped) => item(mapped.qcItemNo)) }
    forms.value.unshift(next)
    activeStatus.value = 0
    selectedFormId.value = next.id
    selectedItemNo.value = next.items[0]?.qcItemNo || ''
    setMessage((draftMode.value === 'copy' ? '已複製' : '已新增') + '未確認檢驗單 DEV-' + next.qcFormNo + '。', 'success')
  }
  closeDraft()
}
function editSelected(): void {
  if (!selectedForm.value || selectedForm.value.callBackType) return setMessage('來源不允許編輯由 ERP 回拋的檢驗單。', 'error')
  openDraft('edit', selectedForm.value)
}
function copySelected(): void { if (selectedForm.value) openDraft('copy', selectedForm.value) }
function deleteUnconfirmed(): void {
  if (!selectedForm.value) return
  if (selectedForm.value.callBackType) return setMessage('ERP 建立的檢驗單不可刪除。', 'error')
  forms.value = forms.value.filter((row) => row.id !== selectedFormId.value)
  selectedFormId.value = visibleForms.value[0]?.id || ''
  setMessage('已在本頁移除未確認 DEV 檢驗單。', 'success')
}
function confirmForm(): void {
  if (!selectedForm.value || selectedForm.value.status !== 0) return
  const form = selectedForm.value
  form.dataStamp += 1
  moveFormToStatus(form, 1)
  setMessage('檢驗單已由未確認移至待驗；DEV 狀態模擬。', 'success')
}
function returnToUnconfirmed(): void {
  if (!selectedForm.value || pendingItemCount.value !== selectedForm.value.items.length) return setMessage('已有 QC Item 完成檢驗，不能退回未確認。', 'error')
  const form = selectedForm.value
  form.dataStamp += 1
  moveFormToStatus(form, 0)
  setMessage('待驗單已退回未確認；DEV 狀態模擬。', 'success')
}
function simulateSpc(): void {
  const form = selectedForm.value
  const qcItem = selectedItem.value
  if (!form || !qcItem || form.status !== 1) return
  if (qcItem.status !== 0) return setMessage('此 QC Item 已完成收集。', 'warning')
  const isPass = qcItem.qcItemNo !== 'IQC-V-CERT-04'
  const serial = 'DEV-SPC-' + Date.now().toString(36).toUpperCase()
  qcItem.status = 1
  qcItem.result = isPass ? 'Pass' : 'Warning'
  qcItem.spcSerial = serial
  if (form.items.every((row) => row.status === 1)) {
    form.finishTime = new Date().toLocaleString('zh-TW', { hour12: false })
    moveFormToStatus(form, 2)
    setMessage('所有 QC Item 已完成；檢驗單進入檢驗完成。量測、SPC Serial 與結果都是 DEV fixture。', 'success')
  } else setMessage('已模擬完成 ' + qcItem.qcItemNo + '；尚有 ' + pendingItemCount.value + ' 個 QC Item 待驗。', 'success')
}
function finalizeComplete(status: 3 | 4 | 5): void {
  const form = selectedForm.value
  if (!form || form.status !== 2) return
  if (form.callBackType && Number(reportedPassQty.value) + Number(reportedReturnQty.value) !== Number(form.qty)) return setMessage('回報合格數與驗退數的合計必須等於檢驗數量。', 'error')
  form.passQty = Number(reportedPassQty.value)
  form.returnQty = Number(reportedReturnQty.value)
  form.dataStamp += 1
  form.finishTime = new Date().toLocaleString('zh-TW', { hour12: false })
  moveFormToStatus(form, status)
  setMessage((status === 3 ? '已模擬收貨' : status === 4 ? '已模擬驗退' : '已模擬特採') + '；沒有呼叫 ERP／sQMS。', 'success')
}
function reviseSelected(): void {
  if (!selectedForm.value || ![3, 4, 5].includes(selectedForm.value.status)) return
  selectedForm.value.rejudgeAt = new Date().toLocaleString('zh-TW', { hour12: false })
  setMessage('已記錄 DEV 複判時間；來源還會回呼 ERP／sQMS 並檢查狀態。', 'warning')
}
</script>

<template>
  <main class="iqc-page" :data-development-preview="props.developmentPreview ? 'true' : 'false'">
    <header class="page-heading"><div><div class="eyebrow">SPC · SC103 · DEV fixtures</div><h1>進料檢驗單 <span>INCOMING QC FORM</span></h1><p>檢驗單資料、QC Item 對應與收貨結果分狀態管理。</p></div><button type="button" class="icon-button" aria-label="關閉" @click="emit('close')">×</button></header>
    <div class="fixture-banner"><strong>DEV 假資料</strong><span>不連 MES Service、資料庫、ERP／sQMS；所有操作只保存在目前頁面。</span></div>

    <section class="query-card">
      <div class="query-grid"><label>檢驗單／來料單／物料／供應商<input v-model="search" placeholder="輸入編號或名稱" /></label><label class="date-check"><input v-model="useCreateDate" type="checkbox" />依建立日期篩選</label><label>From<input v-model="fromDate" type="date" :disabled="!useCreateDate" /></label><label>To<input v-model="toDate" type="date" :disabled="!useCreateDate" /></label><button type="button" class="secondary-button" @click="setMessage('已套用本頁條件；正式查詢來源使用 wsSPC.LoadIncomingQCForm。')">查詢</button></div>
      <p v-if="message" class="feedback" :class="'feedback-' + messageKind" role="status">{{ message }}</p>
    </section>

    <nav class="status-tabs" aria-label="檢驗單狀態">
      <button v-for="tab in statusTabs" :key="tab.status" type="button" :class="{ active: activeStatus === tab.status }" @click="setStatus(tab.status)">{{ tab.label }} <span>{{ forms.filter((row) => row.status === tab.status).length }}</span></button>
    </nav>

    <section class="action-bar">
      <div v-if="activeStatus === 0" class="actions"><button class="primary-button" type="button" @click="openDraft('add')">Add 新增</button><button class="secondary-button" type="button" :disabled="!selectedForm" @click="editSelected">Edit 編輯</button><button class="secondary-button" type="button" :disabled="!selectedForm" @click="copySelected">Copy 複製</button><button class="secondary-button danger-button" type="button" :disabled="!selectedForm" @click="deleteUnconfirmed">Delete 刪除</button><button class="primary-button" type="button" :disabled="!selectedForm" @click="confirmForm">Confirm 確認</button></div>
      <div v-else-if="activeStatus === 1" class="actions"><button class="primary-button" type="button" :disabled="!selectedItem || selectedItem.status !== 0" @click="simulateSpc">SPC 模擬收集選取 QC Item</button><button class="secondary-button" type="button" :disabled="!selectedForm" @click="returnToUnconfirmed">退回未確認</button><button class="secondary-button" type="button" :disabled="!selectedForm" @click="copySelected">Copy 複製</button></div>
      <div v-else-if="activeStatus === 2" class="actions"><label v-if="showQuantityReport">合格數 <input v-model.number="reportedPassQty" type="number" min="0" /></label><label v-if="showQuantityReport">驗退數 <input v-model.number="reportedReturnQty" type="number" min="0" /></label><button class="primary-button" type="button" :disabled="!selectedForm" @click="finalizeComplete(3)">Receive 收貨</button><button class="secondary-button danger-button" type="button" :disabled="!selectedForm" @click="finalizeComplete(4)">Reject 驗退</button><button class="secondary-button" type="button" :disabled="!selectedForm" @click="finalizeComplete(5)">Concession 特採</button></div>
      <div v-else-if="[3, 4, 5].includes(activeStatus)" class="actions"><button class="secondary-button" type="button" :disabled="!selectedForm" @click="reviseSelected">Revise 複判</button><button class="secondary-button" type="button" :disabled="!selectedForm" @click="copySelected">Copy 複製</button></div>
      <div v-else class="actions"><span class="muted-note">此狀態只提供來源記錄查閱。</span><button class="secondary-button" type="button" :disabled="!selectedForm" @click="copySelected">Copy 複製</button></div>
      <span class="status-hint">目前：{{ statusTabs.find((row) => row.status === activeStatus)?.label }}；{{ selectedForm?.qcFormNo || '未選擇檢驗單' }}</span>
    </section>

    <div class="workspace-grid">
      <section class="master-card">
        <div class="section-heading"><div><h2>檢驗單清單</h2><span>{{ visibleForms.length }} 筆 · Status={{ activeStatus }}</span></div><span class="count-badge">DataStamp 樂觀鎖模擬</span></div>
        <div class="table-scroll"><table><thead><tr><th>QCFormNo</th><th>IncomingNo</th><th>Material</th><th>Material Lot</th><th>Vendor</th><th>Qty</th><th>結果</th><th>CallBack</th><th>建立時間</th></tr></thead><tbody>
          <tr v-for="row in visibleForms" :key="row.id" :class="{ selected: row.id === selectedFormId }" tabindex="0" @click="selectForm(row)" @keydown.enter="selectForm(row)"><td class="code-cell">{{ row.qcFormNo }}</td><td>{{ row.incomingNo || '—' }}</td><td>{{ row.materialNo }} · {{ row.materialName }}</td><td>{{ row.materialLotNo || '—' }}</td><td>{{ row.vendorNo }} · {{ row.vendorName }}</td><td>{{ row.qty }}</td><td>{{ row.items.map((qi) => qi.result).filter(Boolean).join(' / ') || '待驗' }}</td><td>{{ row.callBackType || 'MES' }}</td><td>{{ row.createdAt }}</td></tr>
          <tr v-if="!visibleForms.length"><td colspan="9" class="empty-row">此狀態沒有符合條件的檢驗單。</td></tr>
        </tbody></table></div>
      </section>

      <section class="detail-card">
        <div class="section-heading"><div><h2>QC Item 明細</h2><span>{{ selectedForm?.qcFormNo || '請選擇檢驗單' }}</span></div><span class="count-badge">{{ selectedForm?.items.length || 0 }} 項</span></div>
        <div class="detail-meta" v-if="selectedForm"><span>Material<strong>{{ selectedForm.materialNo }}</strong></span><span>Qty<strong>{{ selectedForm.qty }}</strong></span><span>DataStamp<strong>{{ selectedForm.dataStamp }}</strong></span><span>Finish<strong>{{ selectedForm.finishTime || '—' }}</strong></span></div>
        <div class="table-scroll"><table><thead><tr><th>QCItemNo</th><th>品管項目</th><th>類型</th><th>狀態</th><th>結果</th><th>SPC Serial</th><th>操作</th></tr></thead><tbody>
          <tr v-for="qi in selectedForm?.items || []" :key="qi.qcItemNo" :class="{ selected: qi.qcItemNo === selectedItemNo }" tabindex="0" @click="selectItem(qi.qcItemNo)" @keydown.enter="selectItem(qi.qcItemNo)"><td class="code-cell">{{ qi.qcItemNo }}</td><td>{{ qi.name }}</td><td>{{ qi.type === 0 ? '計數' : qi.type === 1 ? '計量' : '群組' }}</td><td>{{ qi.status === 0 ? '待驗' : '完成' }}</td><td>{{ qi.result || '—' }}</td><td>{{ qi.spcSerial || '—' }}</td><td><button v-if="activeStatus === 0" type="button" class="text-button" @click.stop="removeSelectedMapItem(qi.qcItemNo)">移除</button><button v-else-if="activeStatus === 1 && qi.status === 0" type="button" class="text-button" @click.stop="selectItem(qi.qcItemNo); simulateSpc()">模擬檢驗</button></td></tr>
          <tr v-if="selectedForm && !selectedForm.items.length"><td colspan="7" class="empty-row">尚未設定 QC Item。</td></tr>
        </tbody></table></div>
        <div v-if="activeStatus === 0 && selectedForm" class="map-editor"><select v-model="newMapItemNo"><option value="">選擇 QC Item</option><option v-for="option in selectedFormAvailableItems" :key="option.qcItemNo" :value="option.qcItemNo">{{ option.qcItemNo }} · {{ option.name }}</option></select><button type="button" class="secondary-button" :disabled="!newMapItemNo" @click="addSelectedMapItem">Add QC Item</button><span>DEV 只允許未確認單調整對應。</span></div>
        <p class="detail-note">{{ activeStatus === 1 ? '選取待驗項目後，可模擬 SPC 量測；全部項目完成後進入檢驗完成。' : 'QC Item Map 來源由物料檢驗項目或未確認單維護。' }}</p>
      </section>
    </div>

    <div v-if="draft && draftMode" class="modal-backdrop" @click.self="closeDraft">
      <section class="draft-dialog" role="dialog" aria-modal="true" :aria-label="draftMode === 'add' ? '新增進料檢驗單' : draftMode === 'edit' ? '編輯進料檢驗單' : '複製進料檢驗單'">
        <header class="dialog-heading"><div><h2>{{ draftMode === 'add' ? '新增進料檢驗單' : draftMode === 'edit' ? '編輯進料檢驗單' : '複製進料檢驗單' }}</h2><span>來源定義 Form：frmIncomingQCFormDef</span></div><button type="button" class="icon-button" aria-label="關閉視窗" @click="closeDraft">×</button></header>
        <div class="draft-grid">
          <label>QC Form No<div class="inline-input"><input v-model="draft.qcFormNo" :readonly="draftMode === 'edit'" /><button v-if="draftMode !== 'edit'" type="button" class="secondary-button" @click="assignDevFormNo">取 DEV 單號</button></div></label>
          <label>物料編號<div class="inline-input"><select :value="draft.materialNo" @change="onDraftMaterialChange"><option value="">選擇物料</option><option v-for="material in materials" :key="material.materialNo" :value="material.materialNo">{{ material.materialNo }} · {{ material.materialName }}</option></select></div></label>
          <label>物料名稱<input v-model="draft.materialName" readonly /></label><label>檢驗數量 Qty<input v-model.number="draft.qty" type="number" min="0" step="0.001" /></label>
          <label>來料單 Incoming No<input v-model="draft.incomingNo" /></label><label>物料批號<input v-model="draft.materialLotNo" /></label>
          <label>供應商<div class="inline-input"><select :value="draft.vendorNo" @change="onDraftVendorChange"><option value="">選擇供應商</option><option v-for="vendor in vendors" :key="vendor.vendorNo" :value="vendor.vendorNo">{{ vendor.vendorNo }} · {{ vendor.vendorName }}</option></select></div></label>
          <label>原供應商<input v-model="draft.oriVendor" /></label><label>原供應商批號<input v-model="draft.oriVenLot" /></label><label>檢驗文件連結<input v-model="draft.inspDocLink" /></label>
        </div>
        <div class="draft-map"><div class="section-heading"><div><h3>品管項目對應</h3><span>載入物料 QC Item 後可調整此 DEV 清單</span></div><span class="count-badge">{{ draft.items.length }} 項</span></div>
          <div class="map-options"><label v-for="qi in draft.items" :key="qi.qcItemNo"><input type="checkbox" checked @change="removeMapItem(qi.qcItemNo)" />{{ qi.qcItemNo }} · {{ qi.name }}<small>{{ qi.type === 0 ? '計數' : qi.type === 1 ? '計量' : '群組' }}</small></label><span v-if="!draft.items.length" class="muted-note">選物料載入預設 QC Item，或從清單加入。</span></div>
          <div class="map-add"><select v-model="newMapItemNo"><option value="">選擇其他 QC Item</option><option v-for="option in availableItems" :key="option.qcItemNo" :value="option.qcItemNo">{{ option.qcItemNo }} · {{ option.name }}</option></select><button class="secondary-button" type="button" :disabled="!newMapItemNo" @click="addMapItem">Add</button></div>
        </div>
        <footer class="dialog-footer"><span>Creator：{{ draft.creator }} · DEV DataStamp：{{ draft.dataStamp || '新建' }}</span><div><button type="button" class="secondary-button" @click="closeDraft">取消</button><button type="button" class="primary-button" @click="saveDraft">{{ draftMode === 'edit' ? '套用修改' : '新增檢驗單' }}</button></div></footer>
      </section>
    </div>
  </main>
</template>

<style scoped>
.iqc-page { display: grid; gap: 11px; min-height: 100%; padding: 13px; background: #f7f5ee; color: #302f29; font: inherit; box-sizing: border-box; }
.page-heading,.section-heading,.dialog-heading { display: flex; justify-content: space-between; align-items: center; gap: 10px; }.page-heading h1 { margin: 3px 0; font-size: 21px; }.page-heading h1 span { margin-left: 5px; color: #8b7625; font-size: 13px; }.page-heading p,.section-heading span,.dialog-heading span { margin: 0; color: #777366; font-size: 11px; }.eyebrow { color: #8b7625; font-size: 10px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; }.icon-button { width: 30px; height: 30px; border: 1px solid #d5d1c3; border-radius: 4px; background: white; font-size: 20px; cursor: pointer; }
.fixture-banner { display: flex; gap: 10px; padding: 8px 10px; border: 1px solid #e4d9a7; border-radius: 4px; background: #fff9dc; color: #625521; font-size: 11px; }.query-card,.master-card,.detail-card { padding: 10px; border: 1px solid #dfdcd2; border-radius: 4px; background: white; }.query-grid { display: grid; grid-template-columns: 1.5fr auto 1fr 1fr auto; align-items: end; gap: 8px; }.query-grid label,.draft-grid label,.action-bar label { display: grid; gap: 4px; color: #565349; font-size: 11px; }.date-check { display: flex !important; flex-direction: row; align-items: center; white-space: nowrap; }.date-check input { width: auto; min-height: 0; }.feedback { margin: 7px 0 0; color: #5d5a50; font-size: 11px; }.feedback-success { color: #397045; }.feedback-warning { color: #8a6920; }.feedback-error { color: #a53029; }
input,select { width: 100%; min-width: 0; min-height: 29px; padding: 4px 7px; border: 1px solid #d8d5ca; border-radius: 3px; background: white; color: #302f29; font: inherit; font-size: 11px; box-sizing: border-box; }input:disabled,select:disabled { background: #f5f3ed; color: #8a877d; }input[type=checkbox] { width: auto; min-height: auto; }
.status-tabs { display: flex; gap: 3px; overflow-x: auto; border-bottom: 1px solid #d9d5c8; }.status-tabs button { display: flex; align-items: center; gap: 7px; min-height: 34px; padding: 5px 10px; border: 1px solid transparent; border-bottom: 0; border-radius: 4px 4px 0 0; background: transparent; color: #666256; white-space: nowrap; font: inherit; font-size: 11px; cursor: pointer; }.status-tabs button.active { border-color: #d9d5c8; background: white; color: #302f29; font-weight: 650; }.status-tabs button span,.count-badge { padding: 3px 7px; border-radius: 10px; background: #f1eee4; color: #635d4b; font-size: 10px; }
.action-bar { display: flex; justify-content: space-between; align-items: center; gap: 10px; flex-wrap: wrap; padding: 8px; border: 1px solid #dfdcd2; border-radius: 4px; background: white; }.actions { display: flex; align-items: end; flex-wrap: wrap; gap: 6px; }.action-bar label { min-width: 90px; }.status-hint,.muted-note { color: #777366; font-size: 10px; }.primary-button,.secondary-button { min-height: 29px; padding: 4px 10px; border: 1px solid #3b3930; border-radius: 3px; font: inherit; font-size: 11px; cursor: pointer; }.primary-button { background: #3b3930; color: white; }.secondary-button { background: white; color: #333; }.danger-button { border-color: #984740; color: #8a4338; }.action-bar button:disabled,.map-add button:disabled { opacity: .45; cursor: not-allowed; }
.workspace-grid { display: grid; grid-template-columns: minmax(0,1.25fr) minmax(330px,.9fr); gap: 10px; min-height: 0; }.section-heading { margin-bottom: 8px; }.section-heading h2 { margin: 0 0 2px; font-size: 14px; }.section-heading h3 { margin: 0 0 2px; font-size: 13px; }.master-card,.detail-card { min-width: 0; }.table-scroll { overflow: auto; max-height: 340px; border: 1px solid #e2dfd5; border-radius: 3px; }.table-scroll table { width: 100%; border-collapse: collapse; font-size: 10px; }.table-scroll th,.table-scroll td { padding: 6px 7px; border-bottom: 1px solid #e7e4da; text-align: left; white-space: nowrap; }.table-scroll th { position: sticky; top: 0; z-index: 1; background: #f2f0e8; color: #555246; font-weight: 650; }.table-scroll tbody tr { cursor: pointer; }.table-scroll tbody tr.selected { background: #fff8dc; }.table-scroll tbody tr:last-child td { border-bottom: 0; }.code-cell { font-family: Consolas,monospace; font-weight: 650; }.empty-row { padding: 18px !important; color: #777366; text-align: center !important; white-space: normal !important; }.detail-meta { display: flex; flex-wrap: wrap; gap: 7px 13px; margin-bottom: 8px; color: #777366; font-size: 10px; }.detail-meta strong { margin-left: 4px; color: #343228; }.detail-note { margin: 8px 0 0; color: #777366; font-size: 10px; }.map-editor { display: flex; gap: 6px; align-items: center; margin-top: 8px; }.map-editor select { max-width: 230px; }.map-editor span { color: #777366; font-size: 10px; }.text-button { border: 0; background: transparent; color: #6e5e21; font: inherit; font-size: 10px; text-decoration: underline; cursor: pointer; }
.modal-backdrop { position: fixed; z-index: 1200; inset: 0; display: grid; place-items: center; padding: 16px; background: rgb(20 20 18 / 42%); }.draft-dialog { display: grid; gap: 12px; width: min(820px,100%); max-height: 92vh; overflow: auto; padding: 14px; border: 1px solid #d5d1c3; border-radius: 5px; background: white; box-shadow: 0 18px 54px rgb(0 0 0 / 22%); }.dialog-heading h2 { margin: 0 0 3px; font-size: 17px; }.draft-grid { display: grid; grid-template-columns: repeat(2,minmax(0,1fr)); gap: 9px; }.inline-input { display: flex; gap: 5px; }.inline-input button { flex: 0 0 auto; }.draft-map { padding-top: 9px; border-top: 1px solid #e7e4da; }.map-options { display: flex; flex-wrap: wrap; gap: 7px; margin: 8px 0; }.map-options label { display: grid; grid-template-columns: auto 1fr; align-items: center; gap: 4px 6px; padding: 7px; border: 1px solid #e2dfd5; border-radius: 3px; font-size: 10px; }.map-options small { grid-column: 2; color: #777366; }.map-add { display: flex; gap: 6px; }.dialog-footer { display: flex; justify-content: space-between; align-items: center; gap: 10px; padding-top: 9px; border-top: 1px solid #e7e4da; color: #777366; font-size: 10px; }.dialog-footer > div { display: flex; gap: 6px; }
@media (max-width: 900px) { .query-grid { grid-template-columns: 1fr 1fr; }.query-grid label:first-child { grid-column: 1 / -1; }.workspace-grid { grid-template-columns: 1fr; }.table-scroll { max-height: 280px; } }
@media (max-width: 560px) { .draft-grid { grid-template-columns: 1fr; }.dialog-footer { align-items: flex-start; flex-direction: column; }.action-bar { align-items: flex-start; flex-direction: column; } }
</style>
