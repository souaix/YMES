<script setup lang="ts">
import { computed, ref } from 'vue'
import { parseFirstWorksheetXlsx, parseMaterialCsv } from './materialImportWorkbook'

type FormStatus = 0 | 1 | 2 | 3 | 4 | 5 | 6
type MapStatus = 0 | 1
interface QcItem { qcItemNo: string; name: string; type: 0 | 1 | 2; status: MapStatus; result: string; serial: string }
interface FormRow {
  id: string; qcFormNo: string; incomingNo: string; materialNo: string; materialName: string; qty: number; vendorNo: string
  oriVendor: string; oriVenLot: string; materialLotNo: string; inspDocLink: string; callBackType: string; status: FormStatus
  creator: string; createDate: string; reviser: string; reviseDate: string; finishTime: string; rejudgeAt: string
  dataStamp: number; passQty: number; returnQty: number; qtySplitEnabled: boolean; components: string[]; componentsSaved: boolean; items: QcItem[]
}
interface FormDraft {
  id?: string; qcFormNo: string; incomingNo: string; materialNo: string; materialName: string; qty: number; vendorNo: string
  oriVendor: string; oriVenLot: string; materialLotNo: string; inspDocLink: string; callBackType: string
  dataStamp: number; components: string[]; items: QcItem[]
}

const props = defineProps<{ developmentPreview: boolean; currentUserNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const statusTabs: Array<{ status: FormStatus; label: string }> = [
  { status: 0, label: '未確認' }, { status: 1, label: '待驗' }, { status: 2, label: '檢驗完成' },
  { status: 3, label: '已接收' }, { status: 4, label: '已拒收' }, { status: 5, label: '特採' }, { status: 6, label: '作廢' },
]
const qcCatalog: Array<Pick<QcItem, 'qcItemNo' | 'name' | 'type'>> = [
  { qcItemNo: 'DEV-IQC-V-01', name: '元件尺寸', type: 1 }, { qcItemNo: 'DEV-IQC-A-02', name: '元件外觀', type: 0 },
  { qcItemNo: 'DEV-IQC-G-03', name: '元件包裝群組', type: 2 }, { qcItemNo: 'DEV-IQC-V-04', name: '材料證明', type: 1 },
]
const qcTypeName = (type: QcItem['type']): string => type === 0 ? '計數' : type === 1 ? '計量' : '群組'
const statusName = (status: FormStatus): string => statusTabs.find((tab) => tab.status === status)?.label || '未知'
const cloneItem = (qcItemNo: string, status: MapStatus = 0, result = '', serial = ''): QcItem => {
  const source = qcCatalog.find((item) => item.qcItemNo === qcItemNo) || qcCatalog[0]
  return { ...source, status, result, serial }
}
const nowText = (): string => new Date().toLocaleString('sv-SE').replace('T', ' ')
const forms = ref<FormRow[]>([
  { id: 'DEV-IQCC-001', qcFormNo: 'DEV-IQCC-001', incomingNo: 'IN-PO-88421', materialNo: 'MTL-AL-6061', materialName: '鋁合金板 6061', qty: 3, vendorNo: 'V-1001', oriVendor: 'OV-77', oriVenLot: 'OVL-2611-A', materialLotNo: 'AL-2611-A', inspDocLink: 'DEV://iqc/spec-001', callBackType: '', status: 0, creator: 'DEV-QC-01', createDate: '2026-11-23 08:30:00', reviser: '', reviseDate: '', finishTime: '', rejudgeAt: '', dataStamp: 1, passQty: 0, returnQty: 0, qtySplitEnabled: false, components: ['COMP-AL-001', 'COMP-AL-002', 'COMP-AL-003'], componentsSaved: true, items: [cloneItem('DEV-IQC-V-01'), cloneItem('DEV-IQC-A-02')] },
  { id: 'DEV-IQCC-002', qcFormNo: 'DEV-IQCC-002', incomingNo: 'IN-PO-88436', materialNo: 'MTL-RES-PA66', materialName: 'PA66 工程塑料', qty: 2, vendorNo: 'V-1002', oriVendor: 'OV-81', oriVenLot: 'OVL-2611-B', materialLotNo: 'PA-2611-07', inspDocLink: '', callBackType: 'T100', status: 1, creator: 'DEV-ERP', createDate: '2026-11-22 15:10:00', reviser: 'DEV-QC-02', reviseDate: '2026-11-22 15:20:00', finishTime: '', rejudgeAt: '', dataStamp: 2, passQty: 0, returnQty: 0, qtySplitEnabled: true, components: ['COMP-PA-011', 'COMP-PA-012'], componentsSaved: true, items: [cloneItem('DEV-IQC-V-01')] },
  { id: 'DEV-IQCC-003', qcFormNo: 'DEV-IQCC-003', incomingNo: 'IN-PO-88370', materialNo: 'MTL-ELEC-01', materialName: '電子連接器', qty: 2, vendorNo: 'V-1003', oriVendor: 'OV-93', oriVenLot: 'OVL-2611-C', materialLotNo: 'CN-2611-11', inspDocLink: '', callBackType: 'Workflow', status: 2, creator: 'DEV-ERP', createDate: '2026-11-22 09:10:00', reviser: 'DEV-QC-03', reviseDate: '2026-11-22 09:40:00', finishTime: '2026-11-22 09:35:00', rejudgeAt: '', dataStamp: 3, passQty: 2, returnQty: 0, qtySplitEnabled: true, components: ['COMP-CN-101', 'COMP-CN-102'], componentsSaved: true, items: [cloneItem('DEV-IQC-A-02', 1, 'Pass', 'DEV-SPC-108'), cloneItem('DEV-IQC-V-04', 1, 'Warning', 'DEV-SPC-109')] },
  { id: 'DEV-IQCC-004', qcFormNo: 'DEV-IQCC-004', incomingNo: 'IN-PO-88250', materialNo: 'MTL-AL-6061', materialName: '鋁合金板 6061', qty: 2, vendorNo: 'V-1001', oriVendor: 'OV-77', oriVenLot: 'OVL-2611-D', materialLotNo: 'AL-2611-B', inspDocLink: '', callBackType: '', status: 3, creator: 'DEV-QC-01', createDate: '2026-11-21 10:00:00', reviser: 'DEV-QC-04', reviseDate: '2026-11-21 10:25:00', finishTime: '2026-11-21 10:20:00', rejudgeAt: '', dataStamp: 4, passQty: 2, returnQty: 0, qtySplitEnabled: false, components: ['COMP-AL-101', 'COMP-AL-102'], componentsSaved: true, items: [cloneItem('DEV-IQC-V-01', 1, 'Pass', 'DEV-SPC-110')] },
  { id: 'DEV-IQCC-005', qcFormNo: 'DEV-IQCC-005', incomingNo: 'IN-PO-88206', materialNo: 'MTL-RES-PA66', materialName: 'PA66 工程塑料', qty: 1, vendorNo: 'V-1002', oriVendor: 'OV-81', oriVenLot: 'OVL-2611-E', materialLotNo: 'PA-2611-03', inspDocLink: '', callBackType: 'T100', status: 4, creator: 'DEV-ERP', createDate: '2026-11-20 13:20:00', reviser: 'DEV-QC-05', reviseDate: '2026-11-20 14:00:00', finishTime: '2026-11-20 13:55:00', rejudgeAt: '', dataStamp: 5, passQty: 0, returnQty: 1, qtySplitEnabled: true, components: ['COMP-PA-201'], componentsSaved: true, items: [cloneItem('DEV-IQC-V-01', 1, 'Fail', 'DEV-SPC-111')] },
  { id: 'DEV-IQCC-006', qcFormNo: 'DEV-IQCC-006', incomingNo: 'IN-PO-88190', materialNo: 'MTL-ELEC-01', materialName: '電子連接器', qty: 1, vendorNo: 'V-1003', oriVendor: 'OV-93', oriVenLot: 'OVL-2611-F', materialLotNo: 'CN-2611-08', inspDocLink: '', callBackType: 'TIPTOP', status: 5, creator: 'DEV-ERP', createDate: '2026-11-19 11:00:00', reviser: 'DEV-QC-06', reviseDate: '2026-11-19 12:00:00', finishTime: '2026-11-19 11:45:00', rejudgeAt: '', dataStamp: 6, passQty: 1, returnQty: 0, qtySplitEnabled: true, components: ['COMP-CN-201'], componentsSaved: true, items: [cloneItem('DEV-IQC-A-02', 1, 'Concession', 'DEV-SPC-112')] },
  { id: 'DEV-IQCC-007', qcFormNo: 'DEV-IQCC-007', incomingNo: 'IN-PO-88101', materialNo: 'MTL-AL-6061', materialName: '鋁合金板 6061', qty: 1, vendorNo: 'V-1001', oriVendor: 'OV-77', oriVenLot: 'OVL-2611-G', materialLotNo: 'AL-2611-C', inspDocLink: '', callBackType: '', status: 6, creator: 'DEV-QC-01', createDate: '2026-11-18 08:30:00', reviser: 'DEV-QC-02', reviseDate: '2026-11-18 08:45:00', finishTime: '', rejudgeAt: '', dataStamp: 7, passQty: 0, returnQty: 0, qtySplitEnabled: false, components: ['COMP-AL-201'], componentsSaved: true, items: [] },
])

const activeStatus = ref<FormStatus>(0)
const selectedId = ref('DEV-IQCC-001')
const selectedItemNo = ref('')
const qcFormQuery = ref('')
const fromDate = ref('')
const endDate = ref('')
const draftMode = ref<'add' | 'edit' | 'copy' | null>(null)
const draft = ref<FormDraft | null>(null)
const newMapItemNo = ref('')
const componentInput = ref('')
const pasteText = ref('')
const fileInput = ref<HTMLInputElement | null>(null)
const callbackResult = ref<'success' | 'fail'>('success')
const notice = ref('SC108 DEV 假資料；所有變更僅存在此頁記憶體。')
const messageKind = ref<'info' | 'success' | 'error'>('info')
const serialSequence = ref(8)

const visibleForms = computed(() => forms.value.filter((form) => {
  if (form.status !== activeStatus.value) return false
  if (qcFormQuery.value && !form.qcFormNo.toLowerCase().includes(qcFormQuery.value.trim().toLowerCase())) return false
  const date = form.createDate.slice(0, 10)
  if (fromDate.value && date < fromDate.value) return false
  if (endDate.value && date > endDate.value) return false
  return true
}))
const selectedForm = computed(() => forms.value.find((form) => form.id === selectedId.value) || null)
const selectedItem = computed(() => selectedForm.value?.items.find((item) => item.qcItemNo === selectedItemNo.value) || null)
const pendingItems = computed(() => selectedForm.value?.items.filter((item) => item.status === 0).length || 0)
const componentCountText = computed(() => (selectedForm.value?.components.length || 0) + ' / ' + (selectedForm.value?.qty || 0))

function setMessage(text: string, kind: 'info' | 'success' | 'error' = 'info'): void { notice.value = text; messageKind.value = kind }
function selectForm(form: FormRow): void { selectedId.value = form.id; selectedItemNo.value = form.items[0]?.qcItemNo || ''; componentInput.value = '' }
function runQuery(): void {
  if (!visibleForms.value.some((form) => form.id === selectedId.value)) {
    const first = visibleForms.value[0]
    if (first) selectForm(first)
    else { selectedId.value = ''; selectedItemNo.value = '' }
  }
  setMessage('查詢完成：' + visibleForms.value.length + ' 筆 DEV 假資料。')
}
function setActiveStatus(status: FormStatus): void {
  activeStatus.value = status
  if (!visibleForms.value.some((form) => form.id === selectedId.value)) {
    const first = visibleForms.value[0]
    if (first) selectForm(first)
    else { selectedId.value = ''; selectedItemNo.value = '' }
  }
}
function openDraft(mode: 'add' | 'edit' | 'copy'): void {
  const source = selectedForm.value
  if (mode !== 'add' && !source) return
  if (mode === 'edit' && source?.status !== 0) return setMessage('只有未確認單據可編輯。', 'error')
  if (mode === 'edit' && source?.callBackType) return setMessage('ERP 回拋單據不可編輯。', 'error')
  const isCopy = mode === 'copy'
  draft.value = {
    id: isCopy ? undefined : source?.id,
    qcFormNo: isCopy ? '' : source?.qcFormNo || '', incomingNo: source?.incomingNo || '', materialNo: source?.materialNo || '',
    materialName: source?.materialName || '', qty: source?.qty || 0, vendorNo: source?.vendorNo || '', oriVendor: source?.oriVendor || '',
    oriVenLot: source?.oriVenLot || '', materialLotNo: source?.materialLotNo || '', inspDocLink: source?.inspDocLink || '',
    callBackType: isCopy ? '' : source?.callBackType || '', dataStamp: source?.dataStamp || 0,
    components: mode === 'edit' ? [...(source?.components || [])] : [], items: (source?.items || []).map((item) => cloneItem(item.qcItemNo)),
  }
  if (mode === 'add') draft.value.items = []
  draftMode.value = mode
}
function closeDraft(): void { draftMode.value = null; draft.value = null }
function assignFormNo(): void { if (draft.value) draft.value.qcFormNo = 'DEV-IQCC-261123-' + String(serialSequence.value++).padStart(3, '0') }
function saveDraft(): void {
  const value = draft.value
  if (!value) return
  if (!value.qcFormNo.trim()) return setMessage('請輸入或取得 DEV 檢驗單號。', 'error')
  if (!value.materialNo.trim() || !value.materialName.trim()) return setMessage('請填物料編號與名稱。', 'error')
  if (!Number.isFinite(Number(value.qty)) || Number(value.qty) <= 0) return setMessage('檢驗數量必須大於 0。', 'error')
  if (draftMode.value === 'edit') {
    const original = forms.value.find((form) => form.id === value.id)
    if (!original || original.dataStamp !== value.dataStamp) return setMessage('DEV DataStamp 已變更，請重新載入後再編輯。', 'error')
    Object.assign(original, { ...value, qty: Number(value.qty), reviser: props.currentUserNo || 'DEV-QC-01', reviseDate: nowText(), dataStamp: original.dataStamp + 1, componentsSaved: false })
    setMessage('已在 DEV 假資料修改未確認單據。', 'success')
  } else {
    if (forms.value.some((form) => form.qcFormNo === value.qcFormNo)) return setMessage('檢驗單號已存在。', 'error')
    const created: FormRow = {
      ...value, id: value.qcFormNo, qty: Number(value.qty), status: 0, creator: props.currentUserNo || 'DEV-QC-01', createDate: nowText(),
      reviser: '', reviseDate: '', finishTime: '', rejudgeAt: '', passQty: 0, returnQty: 0, qtySplitEnabled: false,
      components: [], componentsSaved: false, dataStamp: 1,
    }
    forms.value.unshift(created)
    activeStatus.value = 0
    selectForm(created)
    setMessage((draftMode.value === 'copy' ? '已複製' : '已新增') + '未確認 DEV 單據；元件清單需另行匯入並儲存。', 'success')
  }
  closeDraft()
}
function deleteUnconfirmed(): void {
  const form = selectedForm.value
  if (!form || form.status !== 0) return
  if (form.callBackType) return setMessage('ERP 回拋單據不可刪除。', 'error')
  forms.value = forms.value.filter((row) => row.id !== form.id)
  selectedId.value = visibleForms.value[0]?.id || ''
  setMessage('已在本頁移除未確認 DEV 單據。', 'success')
}
function importComponents(numbers: string[], sourceName: string): void {
  const form = selectedForm.value
  if (!form || form.status !== 0) return setMessage('只有未確認單據可維護元件清單。', 'error')
  const clean = numbers.map((number) => number.trim().toUpperCase()).filter(Boolean)
  const duplicates = clean.filter((number, index) => clean.indexOf(number) !== index)
  if (!clean.length) return setMessage('檔案沒有 ComponentNo 資料。', 'error')
  if (duplicates.length) return setMessage('匯入清單有重複元件：' + [...new Set(duplicates)].join(', '), 'error')
  form.components = clean
  form.componentsSaved = false
  form.dataStamp += 1
  componentInput.value = ''
  setMessage('已載入 ' + clean.length + ' 筆元件號（' + sourceName + '）；請核對數量後按 Save。')
}
function matrixComponentNumbers(matrix: string[][]): string[] {
  if (!matrix.length) return []
  const first = matrix[0].map((cell) => cell.trim().toUpperCase())
  const componentIndex = first.findIndex((cell) => cell === 'COMPONENTNO' || cell === 'COMPONENT NO')
  const index = componentIndex >= 0 ? componentIndex : (matrix[0].length > 1 ? 1 : 0)
  const start = componentIndex >= 0 ? 1 : 0
  return matrix.slice(start).map((row) => row[index] || '').filter((value) => value.trim())
}
async function loadComponentFile(event: Event): Promise<void> {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file) return
  try {
    const extension = file.name.toLowerCase().split('.').pop()
    let matrix: string[][]
    if (extension === 'xlsx') matrix = await parseFirstWorksheetXlsx(file)
    else if (extension === 'csv') matrix = parseMaterialCsv(await file.text())
    else throw new Error('DEV 預覽支援 .xlsx 與 .csv；WinForms .xls 解析交由正式 Service。')
    importComponents(matrixComponentNumbers(matrix), file.name)
  } catch (error) { setMessage(error instanceof Error ? error.message : '讀取元件匯入檔失敗。', 'error') }
}
function addComponent(): void {
  const value = componentInput.value.trim().toUpperCase()
  const form = selectedForm.value
  if (!form || !value) return
  if (form.components.includes(value)) return setMessage('元件號重複：' + value, 'error')
  form.components.push(value)
  form.componentsSaved = false
  componentInput.value = ''
}
function pasteComponents(): void {
  const matrix = pasteText.value.split(/\r?\n/).filter((line) => line.trim()).map((line) => line.split(/[\t,]/))
  const first = matrix[0]?.map((cell) => cell.trim().toUpperCase()) || []
  const componentIndex = first.findIndex((cell) => cell === 'COMPONENTNO' || cell === 'COMPONENT NO')
  const index = componentIndex >= 0 ? componentIndex : (matrix[0]?.length || 0) > 1 ? 1 : 0
  const start = componentIndex >= 0 ? 1 : 0
  importComponents(matrix.slice(start).map((row) => row[index] || ''), '貼上資料')
  pasteText.value = ''
}
function removeComponent(index: number): void {
  const form = selectedForm.value
  if (!form || form.status !== 0) return
  form.components.splice(index, 1)
  form.componentsSaved = false
}
function saveComponents(): void {
  const form = selectedForm.value
  if (!form || form.status !== 0) return
  if (new Set(form.components).size !== form.components.length) return setMessage('元件號不可重複。', 'error')
  if (form.components.length !== Number(form.qty)) return setMessage('元件筆數需等於檢驗數量：目前 ' + form.components.length + '，應為 ' + form.qty + '。', 'error')
  form.componentsSaved = true
  form.dataStamp += 1
  setMessage('已在 DEV 假資料儲存 ' + form.components.length + ' 筆元件；尚未送出資料庫。', 'success')
}
function addMapItem(): void {
  const form = selectedForm.value
  const number = newMapItemNo.value
  if (!form || form.status !== 0 || !number) return
  if (form.items.some((item) => item.qcItemNo === number)) return setMessage('QC Item 已在對應清單中。', 'error')
  form.items.push(cloneItem(number))
  newMapItemNo.value = ''
}
function removeMapItem(number: string): void {
  const form = selectedForm.value
  if (form?.status === 0) form.items = form.items.filter((item) => item.qcItemNo !== number)
}
function moveToStatus(form: FormRow, status: FormStatus): void {
  form.status = status
  form.dataStamp += 1
  form.reviser = props.currentUserNo || 'DEV-QC-01'
  form.reviseDate = nowText()
  if (status === 2) form.finishTime = nowText()
}
function confirmForm(): void {
  const form = selectedForm.value
  if (!form || form.status !== 0) return
  if (!form.componentsSaved || form.components.length !== form.qty) return setMessage('元件清單需先儲存，且筆數等於檢驗數量。', 'error')
  moveToStatus(form, 1)
  activeStatus.value = 1
  setMessage('檢驗單已由未確認移至待驗；DEV 狀態模擬。', 'success')
}
function returnToUnconfirmed(): void {
  const form = selectedForm.value
  if (!form || form.status !== 1) return
  if (form.items.some((item) => item.status !== 0)) return setMessage('已有 QC Item 開始檢驗，不能退回未確認。', 'error')
  moveToStatus(form, 0)
  activeStatus.value = 0
  setMessage('待驗單已退回未確認；DEV 狀態模擬。', 'success')
}
function simulateSpc(): void {
  const form = selectedForm.value
  const item = selectedItem.value
  if (!form || !item || form.status !== 1) return
  if (item.status !== 0) return setMessage('此 QC Item 已完成檢驗。', 'error')
  item.status = 1
  item.result = item.qcItemNo === 'DEV-IQC-V-04' ? 'Warning' : 'Pass'
  item.serial = 'DEV-SPC-' + String(serialSequence.value++).padStart(3, '0')
  if (form.items.length && form.items.every((row) => row.status === 1)) {
    moveToStatus(form, 2)
    activeStatus.value = 2
    setMessage('所有 QC Item 已完成，表頭自動轉為檢驗完成；DEV 假結果。', 'success')
  } else setMessage('已模擬一次 SPC 收集：' + item.qcItemNo + ' / ' + item.result + '。')
}
function completeDisposition(status: 3 | 4 | 5): void {
  const form = selectedForm.value
  if (!form || form.status !== 2) return
  if (form.qtySplitEnabled && form.passQty + form.returnQty !== form.qty) return setMessage('允收數量 + 退回數量必須等於檢驗數量。', 'error')
  if (form.callBackType && callbackResult.value === 'fail') return setMessage('DEV callback 模擬失敗；保留 MES 檢驗完成狀態。', 'error')
  moveToStatus(form, status)
  activeStatus.value = status
  setMessage((form.callBackType ? 'DEV 外部 callback 成功後，' : '') + '單據已轉為「' + statusName(status) + '」。', 'success')
}
function reviseForm(): void {
  const form = selectedForm.value
  if (!form || ![3, 4, 5].includes(form.status)) return
  if (form.callBackType && callbackResult.value === 'fail') return setMessage('DEV 複判 callback 模擬失敗；狀態維持不變。', 'error')
  moveToStatus(form, 2)
  form.rejudgeAt = nowText()
  activeStatus.value = 2
  setMessage('複判完成並返回檢驗完成；DEV 假流程。', 'success')
}
</script>

<template>
  <section v-if="props.developmentPreview" class="component-iqc-page" aria-label="進料檢驗_元件">
    <header class="page-head">
      <div><p class="eyebrow">SPC / SC108</p><h1>進料檢驗_元件</h1><p class="subtitle">以元件序號明細進行進料檢驗；畫面資料為 DEV 假資料。</p></div>
      <button type="button" class="button" @click="emit('close')">Close (X)</button>
    </header>

    <section class="query-bar" aria-label="查詢條件">
      <label>檢驗單號<input v-model="qcFormQuery" type="search" placeholder="單號包含" @keyup.enter="runQuery"></label>
      <label>建立日期起<input v-model="fromDate" type="date"></label>
      <label>建立日期迄<input v-model="endDate" type="date"></label>
      <button type="button" class="button primary" @click="runQuery">Query</button>
    </section>

    <nav class="status-tabs" aria-label="檢驗單狀態">
      <button v-for="tab in statusTabs" :key="tab.status" type="button" :class="{ active: activeStatus === tab.status }" @click="setActiveStatus(tab.status)">{{ tab.label }} <small>{{ forms.filter((form) => form.status === tab.status).length }}</small></button>
    </nav>

    <div class="workspace">
      <section class="list-pane panel">
        <header class="panel-head"><h2>檢驗單清單</h2><span>{{ visibleForms.length }} 筆</span></header>
        <div class="table-scroll"><table><thead><tr><th>檢驗單號</th><th>進貨單號</th><th>物料</th><th>批號</th><th>數量</th></tr></thead>
          <tbody><tr v-for="form in visibleForms" :key="form.id" :class="{ selected: selectedId === form.id }" @click="selectForm(form)"><td>{{ form.qcFormNo }}</td><td>{{ form.incomingNo }}</td><td>{{ form.materialNo }}<small>{{ form.materialName }}</small></td><td>{{ form.materialLotNo }}</td><td class="number">{{ form.qty }}</td></tr><tr v-if="!visibleForms.length"><td colspan="5" class="empty">沒有符合條件的檢驗單</td></tr></tbody>
        </table></div>
        <div class="list-actions"><button v-if="activeStatus === 0" class="button primary" @click="openDraft('add')">Add</button><button v-if="activeStatus === 0" class="button" :disabled="!selectedForm" @click="openDraft('edit')">Edit</button><button class="button" :disabled="!selectedForm" @click="openDraft('copy')">Copy</button><button v-if="activeStatus === 0" class="button danger" :disabled="!selectedForm" @click="deleteUnconfirmed">Delete</button></div>
      </section>

      <section v-if="selectedForm" class="detail-pane">
        <section class="panel">
          <header class="panel-head"><h2>檢驗單資料</h2><span class="status-chip" :data-status="selectedForm.status">{{ statusName(selectedForm.status) }}</span></header>
          <dl class="header-grid">
            <div><dt>QCFormNo</dt><dd>{{ selectedForm.qcFormNo }}</dd></div><div><dt>IncomingNo</dt><dd>{{ selectedForm.incomingNo || '—' }}</dd></div>
            <div><dt>MaterialNo / Name</dt><dd>{{ selectedForm.materialNo }} / {{ selectedForm.materialName }}</dd></div><div><dt>MaterialLotNo</dt><dd>{{ selectedForm.materialLotNo || '—' }}</dd></div>
            <div><dt>VendorNo</dt><dd>{{ selectedForm.vendorNo || '—' }}</dd></div><div><dt>OriVendor / OriVenLot</dt><dd>{{ selectedForm.oriVendor || '—' }} / {{ selectedForm.oriVenLot || '—' }}</dd></div>
            <div><dt>Qty / 元件數</dt><dd>{{ selectedForm.qty }} / {{ componentCountText }}</dd></div><div><dt>CallBackType</dt><dd>{{ selectedForm.callBackType || '手開單' }}</dd></div>
            <div><dt>InspDocLink</dt><dd>{{ selectedForm.inspDocLink || '—' }}</dd></div><div><dt>DataStamp</dt><dd>{{ selectedForm.dataStamp }}</dd></div>
          </dl>
          <div class="status-actions" v-if="selectedForm.status === 0"><button class="button primary" :disabled="!selectedForm.componentsSaved || selectedForm.components.length !== selectedForm.qty" @click="confirmForm">Confirm</button><span>Confirm 前元件筆數必須等於 Qty。</span></div>
          <div class="status-actions" v-else-if="selectedForm.status === 1"><button class="button" @click="returnToUnconfirmed">退回未確認</button><label>QC Item<select v-model="selectedItemNo"><option v-for="item in selectedForm.items" :key="item.qcItemNo" :value="item.qcItemNo">{{ item.qcItemNo }} — {{ item.name }}</option></select></label><button class="button primary" :disabled="!selectedItem || selectedItem.status !== 0" @click="simulateSpc">開啟 SPC（DEV 假結果）</button><span>待驗項目 {{ pendingItems }} 筆</span></div>
          <div class="status-actions disposition" v-else-if="selectedForm.status === 2"><label v-if="selectedForm.qtySplitEnabled">允收 Qty<input v-model.number="selectedForm.passQty" type="number" min="0" :max="selectedForm.qty"></label><label v-if="selectedForm.qtySplitEnabled">退回 Qty<input v-model.number="selectedForm.returnQty" type="number" min="0" :max="selectedForm.qty"></label><label v-if="selectedForm.callBackType">Callback 模擬<select v-model="callbackResult"><option value="success">成功</option><option value="fail">失敗</option></select></label><button class="button primary" @click="completeDisposition(3)">Receive</button><button class="button danger" @click="completeDisposition(4)">Reject</button><button class="button" @click="completeDisposition(5)">Concession</button></div>
          <div class="status-actions" v-else-if="[3, 4, 5].includes(selectedForm.status)"><label v-if="selectedForm.callBackType">複判 callback<select v-model="callbackResult"><option value="success">成功</option><option value="fail">失敗</option></select></label><button class="button" @click="reviseForm">Revise / 複判</button><span>複判成功後回到檢驗完成。</span></div>
        </section>

        <section class="panel component-panel">
          <header class="panel-head"><h2>元件序號明細</h2><span :class="selectedForm.componentsSaved ? 'saved' : 'unsaved'">{{ selectedForm.componentsSaved ? '已儲存' : '尚未儲存' }} · {{ componentCountText }}</span></header>
          <div class="toolbar" v-if="selectedForm.status === 0"><button class="button" @click="fileInput?.click()">匯入 CSV / XLSX</button><input ref="fileInput" class="hidden" type="file" accept=".csv,.xlsx,.xls" @change="loadComponentFile"><input v-model="componentInput" placeholder="ComponentNo" @keyup.enter="addComponent"><button class="button" @click="addComponent">Add</button><button class="button primary" @click="saveComponents">Save</button></div>
          <div class="paste-row" v-if="selectedForm.status === 0"><textarea v-model="pasteText" rows="2" placeholder="貼上 ComponentNo 清單；每行一筆，也可貼上試算表欄位"></textarea><button class="button" @click="pasteComponents">Paste</button></div>
          <div class="table-scroll small-table"><table><thead><tr><th>#</th><th>ComponentNo</th><th v-if="selectedForm.status === 0">操作</th></tr></thead><tbody><tr v-for="(component, index) in selectedForm.components" :key="component + '-' + index"><td>{{ index + 1 }}</td><td>{{ component }}</td><td v-if="selectedForm.status === 0"><button class="link-button" @click="removeComponent(index)">移除</button></td></tr><tr v-if="!selectedForm.components.length"><td colspan="3" class="empty">尚無元件序號</td></tr></tbody></table></div>
          <p class="hint">來源以元件清單取代式儲存；DEV 支援 XLSX／CSV 與貼上，舊 XLS 解析留待正式 Service。</p>
        </section>

        <section class="panel map-panel">
          <header class="panel-head"><h2>QC Item 對應</h2><span>完成 {{ selectedForm.items.length - pendingItems }} / {{ selectedForm.items.length }}</span></header>
          <div class="toolbar" v-if="selectedForm.status === 0"><select v-model="newMapItemNo"><option value="">選擇 QC Item</option><option v-for="item in qcCatalog" :key="item.qcItemNo" :value="item.qcItemNo">{{ item.qcItemNo }} — {{ item.name }}</option></select><button class="button" @click="addMapItem">加入</button></div>
          <div class="table-scroll small-table"><table><thead><tr><th>QCItemNo</th><th>名稱</th><th>類型</th><th>狀態</th><th>結果</th><th>SPCSerial</th><th v-if="selectedForm.status === 0">操作</th></tr></thead><tbody><tr v-for="item in selectedForm.items" :key="item.qcItemNo" :class="{ chosen: selectedItemNo === item.qcItemNo }" @click="selectedItemNo = item.qcItemNo"><td>{{ item.qcItemNo }}</td><td>{{ item.name }}</td><td>{{ qcTypeName(item.type) }}</td><td>{{ item.status ? '完成' : '待驗' }}</td><td>{{ item.result || '—' }}</td><td>{{ item.serial || '—' }}</td><td v-if="selectedForm.status === 0"><button class="link-button" @click.stop="removeMapItem(item.qcItemNo)">移除</button></td></tr><tr v-if="!selectedForm.items.length"><td colspan="7" class="empty">尚未對應 QC Item</td></tr></tbody></table></div>
        </section>

        <section class="audit panel"><strong>稽核欄位</strong><span>Creator：{{ selectedForm.creator }} · CreateDate：{{ selectedForm.createDate }}</span><span>Reviser：{{ selectedForm.reviser || '—' }} · ReviseDate：{{ selectedForm.reviseDate || '—' }}</span><span>FinishTime：{{ selectedForm.finishTime || '—' }} · QcRejudgeDate：{{ selectedForm.rejudgeAt || '—' }}</span></section>
      </section>
      <section v-else class="panel no-selection">選取一筆檢驗單，或在未確認頁新增單據。</section>
    </div>

    <p class="message" :class="messageKind" role="status">{{ notice }}</p>
    <div v-if="draft" class="modal-backdrop" @click.self="closeDraft"><section class="draft-dialog" role="dialog" aria-modal="true" aria-label="檢驗單編輯"><header><h2>{{ draftMode === 'edit' ? 'Edit' : draftMode === 'copy' ? 'Copy' : 'Add' }} IQC Form</h2><button class="link-button" @click="closeDraft">×</button></header><div class="draft-grid">
      <label>QCFormNo <span class="inline"><input v-model="draft.qcFormNo"><button class="button" :disabled="draftMode === 'edit'" @click="assignFormNo">取 DEV 單號</button></span></label><label>IncomingNo<input v-model="draft.incomingNo"></label>
      <label>MaterialNo<input v-model="draft.materialNo"></label><label>MaterialName<input v-model="draft.materialName"></label><label>Qty<input v-model.number="draft.qty" type="number" min="1"></label><label>VendorNo<input v-model="draft.vendorNo"></label>
      <label>OriVendor<input v-model="draft.oriVendor"></label><label>OriVenLot<input v-model="draft.oriVenLot"></label><label>MaterialLotNo<input v-model="draft.materialLotNo"></label><label>InspDocLink<input v-model="draft.inspDocLink"></label>
    </div><p class="hint">DEV 可編輯檢驗單欄位；正式序號、核准物料、供應商及 ERP ownership 由 server 驗證。</p><footer><button class="button primary" @click="saveDraft">Save</button><button class="button" @click="closeDraft">Cancel</button></footer></section></div>
  </section>
  <section v-else class="pending">DEV 預覽已關閉。<button class="button" @click="emit('close')">Close</button></section>
</template>

<style scoped>
.component-iqc-page{height:100%;min-height:0;overflow:auto;box-sizing:border-box;padding:14px;background:#f4f3ef;color:#292821;font:13px/1.45 "Segoe UI",Arial,sans-serif}.page-head{display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:12px}.eyebrow{margin:0;color:#9a6b00;font-size:11px;font-weight:700;letter-spacing:.08em}.page-head h1{margin:2px 0;font-size:22px}.subtitle{margin:0;color:#777366}.button{min-height:30px;padding:5px 11px;border:1px solid #aaa79a;border-radius:3px;background:#fff;color:#292821;font:inherit;cursor:pointer}.button:hover:not(:disabled){background:#f9edbd;border-color:#c8a300}.button:disabled{opacity:.45;cursor:not-allowed}.button.primary{background:#e5c400;border-color:#c6a800;font-weight:700}.button.danger{color:#a33127}.query-bar,.panel{border:1px solid #d4d1c7;border-radius:4px;background:#fff}.query-bar{display:flex;align-items:end;gap:10px;padding:9px;margin-bottom:9px}.query-bar label,.status-actions label,.draft-grid label{display:flex;flex-direction:column;gap:3px;color:#625f55;font-size:11px}.query-bar input,.toolbar input,.toolbar select,.toolbar select,.status-actions input,.status-actions select,.draft-grid input,.inline input{min-height:29px;box-sizing:border-box;border:1px solid #c8c5bb;border-radius:3px;padding:4px 7px;font:inherit;color:#292821;background:#fff}.query-bar label:first-child{min-width:190px}.status-tabs{display:flex;gap:4px;margin:0 0 9px;border-bottom:2px solid #d5cdb3;overflow-x:auto}.status-tabs button{border:1px solid transparent;border-bottom:0;padding:7px 12px;background:transparent;color:#555246;white-space:nowrap;font:inherit;cursor:pointer}.status-tabs button.active{border-color:#d5cdb3;background:#fff;color:#292821;font-weight:700}.status-tabs small{margin-left:3px;color:#777366}.workspace{display:grid;grid-template-columns:minmax(320px,38%) minmax(420px,1fr);gap:10px;align-items:start}.panel{margin-bottom:9px;overflow:hidden}.panel-head{display:flex;align-items:center;justify-content:space-between;padding:8px 10px;border-bottom:1px solid #e4e1d8;background:#faf9f6}.panel-head h2{margin:0;font-size:14px}.panel-head>span{color:#777366;font-size:11px}.table-scroll{overflow:auto;max-height:340px}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}th,td{padding:6px 7px;border-bottom:1px solid #eceae4;text-align:left}th{position:sticky;top:0;background:#f3f1ea;color:#5e5b50;font-weight:600}tbody tr{cursor:pointer}tbody tr:hover{background:#fcf7df}tbody tr.selected,tbody tr.chosen{background:#fff1ad}.table-scroll td small{display:block;color:#777366;font-size:10px}.number{text-align:right}.empty{text-align:center!important;color:#888!important;padding:18px!important}.list-actions,.toolbar,.status-actions{display:flex;align-items:center;gap:6px;flex-wrap:wrap;padding:8px 9px}.list-actions{border-top:1px solid #eceae4}.header-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:0;margin:0;padding:4px 10px}.header-grid div{display:grid;grid-template-columns:112px minmax(0,1fr);gap:7px;padding:5px 0;border-bottom:1px solid #f0eee8}.header-grid dt{color:#777366;font-size:11px}.header-grid dd{margin:0;overflow-wrap:anywhere}.status-chip{padding:2px 8px;border-radius:10px;background:#e9e6da!important;color:#48463d!important}.status-chip[data-status="0"]{background:#fff0b7!important}.status-chip[data-status="1"]{background:#dfeefa!important}.status-chip[data-status="2"]{background:#dff2df!important}.status-actions{border-top:1px solid #eceae4}.status-actions>span{color:#777366;font-size:11px}.status-actions select{min-width:125px}.component-panel .toolbar input{width:190px}.hidden{display:none}.paste-row{display:flex;gap:7px;align-items:stretch;padding:0 9px 8px}.paste-row textarea{flex:1;resize:vertical;min-height:42px;border:1px solid #c8c5bb;border-radius:3px;padding:6px;font:inherit}.small-table{max-height:210px}.small-table th{top:0}.saved{color:#32723c!important}.unsaved{color:#a46700!important}.hint{margin:5px 10px 9px;color:#777366;font-size:11px}.toolbar select{min-width:250px}.link-button{border:0;background:transparent;color:#8f3a2e;text-decoration:underline;font:inherit;cursor:pointer}.audit{display:flex;gap:7px 15px;flex-wrap:wrap;padding:8px 10px;color:#777366;font-size:11px}.audit strong{color:#47443a}.message{position:sticky;bottom:0;margin:0;padding:8px 10px;border:1px solid #e3d9a8;border-radius:3px;background:#fff8d8;color:#63551b}.message.success{border-color:#acd0ae;background:#eff8ef;color:#346238}.message.error{border-color:#e0b2aa;background:#fff0ed;color:#922f25}.modal-backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:15px;background:#20201866}.draft-dialog{width:min(720px,96vw);max-height:90vh;overflow:auto;padding:13px;border:1px solid #c5c1b4;border-radius:5px;background:#fff;box-shadow:0 16px 48px #0003}.draft-dialog>header,.draft-dialog>footer{display:flex;justify-content:space-between;align-items:center;gap:8px}.draft-dialog h2{margin:0 0 10px;font-size:17px}.draft-grid{display:grid;grid-template-columns:1fr 1fr;gap:9px}.draft-grid label input{width:100%}.inline{display:flex;gap:5px}.inline input{min-width:0;flex:1}.draft-dialog>footer{justify-content:flex-end;margin-top:12px}.no-selection,.pending{padding:18px;color:#777366}.pending{background:#f4f3ef}.pending .button{margin-left:10px}
@media(max-width:1000px){.workspace{grid-template-columns:1fr}.list-pane .table-scroll{max-height:220px}.query-bar{flex-wrap:wrap}.header-grid{grid-template-columns:1fr}.component-iqc-page{height:auto;min-height:100%}}
</style>
