<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'

type PickerMode = 'equipmentType' | 'equipment' | 'waitQc' | 'reQc' | null
type QcResult = -1 | 0 | 1
type QcDetail = {
  qcOrder: number
  qcItem: string
  qcType: number
  inputDataCount: number
  inputValue: string
  readings: string[]
  result: '' | 'Y' | 'N'
  qcResult: QcResult
  stdValue: string
  miniValue: string
  maxiValue: string
  inspectionMethod: string
  inspectionStandards: string
}
type Equipment = { equipmentNo: string; equipmentType: string; name: string; issueState: number; unvalidAction: number }
type QcList = { qcListNo: string; qcListName: string; inspType: number; startTime: string; details: QcDetail[] }
type Candidate = {
  taskNo: string; qcListNo: string; inspType: number; startTime: string; plannedAt: string
  shiftNo: string; lotNo: string; lotSerial: string; logGroupSerial: string; status: 0 | 2
}
type InspectionRecord = {
  qcListSerial: string; equipmentNo: string; equipmentType: string; qcListNo: string; qcListName: string
  qcResult: QcResult; description: string; creator: string; createDate: string; reviser: string; reviseDate: string
  taskNo: string; inspType: number; shiftNo: string; lotNo: string; lotSerial: string; logGroupSerial: string
  details: QcDetail[]
}
type Snapshot = { version: 1; records: InspectionRecord[]; taskStatuses: Record<string, 0 | 1 | 2> }

const props = defineProps<{
  currentUserNo?: string
  developmentPreview?: boolean
  initialEquipmentNo?: string
  initialLotSerial?: string
  initialLogGroupSerial?: string
  callByCheckIn?: boolean
  editRecordSerial?: string
}>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const userNo = computed(() => props.currentUserNo || 'DEVUSER')
const storageKey = 'ymes.dev.wp305-equipment-online-qc.v1'

function detail(qcOrder: number, qcItem: string, qcType: number, inputDataCount: number, values: Partial<QcDetail> = {}): QcDetail {
  return {
    qcOrder, qcItem, qcType, inputDataCount, inputValue: '', readings: Array.from({ length: inputDataCount }, () => ''),
    result: '', qcResult: -1, stdValue: '', miniValue: '', maxiValue: '', inspectionMethod: '', inspectionStandards: '', ...values,
  }
}
function qcItems(kind: 'daily' | 'shift' | 'manual'): QcDetail[] {
  const prefix = kind === 'daily' ? '每日' : kind === 'shift' ? '每班' : '一般'
  return [
    detail(10, `${prefix}確認設備壓力`, 0, 1, { stdValue: '12', inspectionMethod: '壓力表讀值', inspectionStandards: '須等於標準值' }),
    detail(20, `${prefix}量測冷卻水溫`, 1, 3, { miniValue: '18', maxiValue: '28', inspectionMethod: '連續量測三次', inspectionStandards: '每次均須落在上下限內' }),
    detail(30, '確認安全門及連鎖裝置', 2, 1, { inspectionMethod: '目視及功能確認', inspectionStandards: '選擇 OK 或 NG' }),
    detail(40, '點檢人員補充說明', 3, 1, { inspectionMethod: '文字輸入', inspectionStandards: '記錄必要異常資訊' }),
  ]
}
const equipment: Equipment[] = [
  { equipmentNo: 'DEV-EQP-ETCH-01', equipmentType: 'DEV-ETCH', name: 'DEV 蝕刻設備 01', issueState: 2, unvalidAction: -1 },
  { equipmentNo: 'DEV-EQP-ETCH-02', equipmentType: 'DEV-ETCH', name: 'DEV 蝕刻設備 02', issueState: 2, unvalidAction: 1 },
  { equipmentNo: 'DEV-EQP-CMP-01', equipmentType: 'DEV-CMP', name: 'DEV CMP 設備 01', issueState: 2, unvalidAction: -1 },
]
const qcLists: QcList[] = [
  { qcListNo: 'DEV-QC-ETCH-MANUAL', qcListName: '蝕刻設備一般點檢', inspType: 0, startTime: 'N/A', details: qcItems('manual') },
  { qcListNo: 'DEV-QC-ETCH-SHIFT', qcListName: '蝕刻設備每班點檢', inspType: 1, startTime: 'N/A', details: qcItems('shift') },
  { qcListNo: 'DEV-QC-ETCH-DAILY', qcListName: '蝕刻設備每日點檢', inspType: 2, startTime: '08:00', details: qcItems('daily') },
  { qcListNo: 'DEV-QC-CMP-DAILY', qcListName: 'CMP 設備每日點檢', inspType: 2, startTime: '08:00', details: qcItems('daily') },
]
const candidateFixtures: Record<string, Candidate[]> = {
  'DEV-EQP-ETCH-01': [
    { taskNo: 'N/A', qcListNo: 'DEV-QC-ETCH-MANUAL', inspType: 0, startTime: 'N/A', plannedAt: 'N/A', shiftNo: 'N/A', lotNo: '', lotSerial: '', logGroupSerial: '', status: 0 },
    { taskNo: 'DEV-TASK-ETCH-071', qcListNo: 'DEV-QC-ETCH-SHIFT', inspType: 1, startTime: 'N/A', plannedAt: '2026/11/20 08:00', shiftNo: 'DEV-A', lotNo: 'DEV-LOT-26001', lotSerial: 'DEV-LOT-SER-26001', logGroupSerial: 'DEV-LOG-26001', status: 0 },
    { taskNo: 'DEV-TASK-ETCH-072', qcListNo: 'DEV-QC-ETCH-DAILY', inspType: 2, startTime: '08:00', plannedAt: '2026/11/20 08:00', shiftNo: '', lotNo: '', lotSerial: '', logGroupSerial: '', status: 0 },
    { taskNo: 'DEV-TASK-ETCH-031', qcListNo: 'DEV-QC-ETCH-SHIFT', inspType: 1, startTime: 'N/A', plannedAt: '2026/11/19 20:00', shiftNo: 'DEV-B', lotNo: 'DEV-LOT-25990', lotSerial: 'DEV-LOT-SER-25990', logGroupSerial: 'DEV-LOG-25990', status: 2 },
  ],
  'DEV-EQP-ETCH-02': [
    { taskNo: 'DEV-TASK-ETCH2-011', qcListNo: 'DEV-QC-ETCH-DAILY', inspType: 2, startTime: '08:00', plannedAt: '2026/11/20 08:00', shiftNo: '', lotNo: '', lotSerial: '', logGroupSerial: '', status: 0 },
  ],
  'DEV-EQP-CMP-01': [
    { taskNo: 'DEV-TASK-CMP-041', qcListNo: 'DEV-QC-CMP-DAILY', inspType: 2, startTime: '08:00', plannedAt: '2026/11/20 08:00', shiftNo: '', lotNo: '', lotSerial: '', logGroupSerial: '', status: 0 },
  ],
}

function seedSnapshot(): Snapshot {
  return { version: 1, records: [], taskStatuses: {} }
}
function readSnapshot(): Snapshot {
  if (!dev.value) return seedSnapshot()
  try {
    const raw = window.localStorage.getItem(storageKey)
    if (raw) {
      const parsed = JSON.parse(raw) as Snapshot
      if (parsed.version === 1 && Array.isArray(parsed.records) && parsed.taskStatuses && typeof parsed.taskStatuses === 'object') return parsed
    }
  } catch { /* restore deterministic DEV sample state */ }
  return seedSnapshot()
}
const snapshot = ref<Snapshot>(readSnapshot())
const equipmentType = ref('')
const equipmentNo = ref('')
const equipmentDescription = ref('')
const qcListNo = ref('')
const qcListSerial = ref('')
const currentQcList = ref<QcList | null>(null)
const details = ref<QcDetail[]>([])
const description = ref('')
const pickerMode = ref<PickerMode>(null)
const pickerSearch = ref('')
const editingRecord = ref<InspectionRecord | null>(null)
const selectedCandidate = ref<Candidate | null>(null)
const errorText = ref('')
const successText = ref('')
const pendingPartialSave = ref(false)
const partialConfirmOpen = ref(false)
const recordMessage = ref('')
const userAudit = reactive({ creator: userNo.value, createDate: nowText(), reviser: '', reviseDate: '' })
const callByCheckIn = computed(() => props.callByCheckIn === true)
const lockedEquipment = computed(() => callByCheckIn.value || Boolean(props.initialEquipmentNo) || Boolean(editingRecord.value))
const showTaskButtons = computed(() => !callByCheckIn.value && !editingRecord.value)
const currentEquipment = computed(() => equipment.find(item => item.equipmentNo === equipmentNo.value.trim().toUpperCase()) ?? null)
const maxSampleCount = computed(() => Math.max(1, ...details.value.map(item => item.inputDataCount)))
const sampleColumns = computed(() => maxSampleCount.value > 1 ? Array.from({ length: Math.min(10, maxSampleCount.value) }, (_, index) => index) : [])
const overallResult = computed<QcResult>(() => {
  if (details.value.length === 0 || details.value.some(item => item.qcResult === -1)) return -1
  return details.value.some(item => item.qcResult === 1) ? 1 : 0
})
const overallResultLabel = computed(() => overallResult.value === 0 ? 'OK' : overallResult.value === 1 ? 'NG' : '未完成')
const filteredEquipment = computed(() => equipment
  .filter(item => item.issueState === 2)
  .filter(item => !equipmentType.value || item.equipmentType === equipmentType.value)
  .filter(item => !pickerSearch.value || `${item.equipmentNo} ${item.equipmentType} ${item.name}`.toLowerCase().includes(pickerSearch.value.toLowerCase())))
const filteredTypes = computed(() => [...new Set(equipment.filter(item => item.issueState === 2).map(item => item.equipmentType))]
  .filter(item => !pickerSearch.value || item.toLowerCase().includes(pickerSearch.value.toLowerCase())))
const candidates = computed(() => {
  const all = candidateFixtures[equipmentNo.value.trim().toUpperCase()] ?? []
  const statusWanted = pickerMode.value === 'reQc' ? 2 : 0
  return all.filter(item => item.status === statusWanted && (item.taskNo === 'N/A' || (snapshot.value.taskStatuses[item.taskNo] ?? item.status) === statusWanted))
})
const pickerTitle = computed(() => pickerMode.value === 'equipmentType' ? '選擇設備類別' : pickerMode.value === 'equipment' ? '選擇設備' : pickerMode.value === 'reQc' ? '選擇補驗點檢任務' : '選擇待點檢任務')
const pickerRows = computed(() => {
  if (pickerMode.value === 'waitQc' || pickerMode.value === 'reQc') return candidates.value
  return []
})

watch(() => props.initialEquipmentNo, value => {
  if (!value) return
  const found = equipment.find(item => item.equipmentNo === value.trim().toUpperCase())
  if (found) setEquipment(found)
}, { immediate: true })
watch(() => props.editRecordSerial, value => {
  if (!value) return
  const record = snapshot.value.records.find(item => item.qcListSerial === value)
  if (record) loadRecord(record)
}, { immediate: true })

function nowText(): string {
  return new Date().toLocaleString('zh-TW', { hour12: false }).replaceAll('-', '/')
}
function cloneDetails(rows: QcDetail[]): QcDetail[] {
  return rows.map(row => ({ ...row, readings: [...row.readings] }))
}
function persist(): void {
  if (dev.value) window.localStorage.setItem(storageKey, JSON.stringify(snapshot.value))
}
function resetMessages(): void { errorText.value = ''; successText.value = '' }
function clearInspection(): void {
  qcListNo.value = ''
  qcListSerial.value = ''
  currentQcList.value = null
  details.value = []
  description.value = ''
  selectedCandidate.value = null
  recordMessage.value = ''
}
function setEquipment(item: Equipment): void {
  equipmentType.value = item.equipmentType
  equipmentNo.value = item.equipmentNo
  equipmentDescription.value = item.name
  clearInspection()
  resetMessages()
}
function openPicker(mode: Exclude<PickerMode, null>): void {
  pickerMode.value = mode
  pickerSearch.value = ''
  if (mode === 'waitQc' || mode === 'reQc') {
    if (!validateEquipment()) { pickerMode.value = null; return }
    if (candidates.value.length === 0) {
      errorText.value = mode === 'reQc' ? '目前設備沒有待補驗任務。' : '目前設備沒有待點檢任務。'
      pickerMode.value = null
      return
    }
    if (candidates.value.length === 1) {
      chooseCandidate(candidates.value[0])
      pickerMode.value = null
    }
  }
}
function chooseType(type: string): void {
  equipmentType.value = type
  equipmentNo.value = ''
  equipmentDescription.value = ''
  clearInspection()
  pickerMode.value = null
}
function chooseEquipment(item: Equipment): void {
  setEquipment(item)
  pickerMode.value = null
}
function onEquipmentTypeInput(): void {
  equipmentNo.value = ''
  equipmentDescription.value = ''
  clearInspection()
  resetMessages()
}
function onEquipmentInput(): void {
  equipmentDescription.value = ''
  clearInspection()
  resetMessages()
}
function normalizeEquipment(): void {
  equipmentNo.value = equipmentNo.value.trim().toUpperCase()
  const found = equipment.find(item => item.equipmentNo === equipmentNo.value)
  if (found) {
    equipmentType.value = found.equipmentType
    equipmentDescription.value = found.name
  } else if (equipmentNo.value) {
    equipmentDescription.value = ''
    clearInspection()
  }
}
function validateEquipment(): boolean {
  if (!equipmentType.value.trim() || !equipmentNo.value.trim()) {
    errorText.value = '請先選擇設備類別與設備編號。'
    return false
  }
  if (!currentEquipment.value || currentEquipment.value.equipmentType !== equipmentType.value) {
    errorText.value = '設備編號不存在，或設備類別與設備主檔不一致。'
    return false
  }
  return true
}
function serialFor(qcNo: string): string {
  const token = crypto.randomUUID().replaceAll('-', '').toUpperCase()
  return `DEV${qcNo.replace(/[^A-Z0-9]/gi, '').slice(-4)}${token.slice(0, 6)}${String(Math.floor(Math.random() * 999) + 1).padStart(3, '0')}`
}
function chooseCandidate(item: Candidate): void {
  const list = qcLists.find(value => value.qcListNo === item.qcListNo)
  if (!list) { errorText.value = `假資料缺少點檢表 ${item.qcListNo}。`; return }
  selectedCandidate.value = { ...item }
  currentQcList.value = { ...list, details: cloneDetails(list.details) }
  qcListNo.value = list.qcListNo
  qcListSerial.value = serialFor(list.qcListNo)
  details.value = cloneDetails(list.details)
  description.value = ''
  userAudit.creator = userNo.value
  userAudit.createDate = nowText()
  userAudit.reviser = ''
  userAudit.reviseDate = ''
  resetMessages()
}
function chooseMessageResult(item: QcDetail, result: 'Y' | 'N'): void {
  item.result = result
  item.qcResult = result === 'Y' ? 0 : 1
  recordMessage.value = ''
}
function checkInputs(item: QcDetail): string[] {
  return item.inputDataCount <= 1 ? [item.inputValue.trim()] : item.readings.slice(0, item.inputDataCount).map(value => value.trim())
}
function evaluateItem(item: QcDetail): void {
  if (item.qcType === 2) return
  const values = checkInputs(item)
  if (values.some(value => value === '')) {
    item.result = ''
    item.qcResult = -1
    return
  }
  if (item.qcType === 0 || item.qcType === 1) {
    const numbers = values.map(Number)
    if (numbers.some(value => !Number.isFinite(value))) {
      item.result = ''
      item.qcResult = -1
      recordMessage.value = `${item.qcItem} 僅接受數值。`
      return
    }
    const passes = item.qcType === 0
      ? numbers.every(value => value === Number(item.stdValue))
      : numbers.every(value => (item.miniValue === '*' || value >= Number(item.miniValue)) && (item.maxiValue === '*' || value <= Number(item.maxiValue)))
    item.qcResult = passes ? 0 : 1
    item.result = passes ? 'Y' : 'N'
    if (item.inputDataCount > 1) item.inputValue = values[0]
    return
  }
  if (item.qcType === 3) {
    item.qcResult = 0
    item.result = 'Y'
    if (item.inputDataCount > 1) item.inputValue = values[0]
  }
}
function onItemInput(item: QcDetail): void {
  recordMessage.value = ''
  evaluateItem(item)
}
function finalValidation(): boolean {
  resetMessages()
  if (!dev.value) { errorText.value = '此頁目前僅提供 DEV 假資料操作。'; return false }
  if (!validateEquipment()) return false
  if (!qcListNo.value || !qcListSerial.value || details.value.length === 0) {
    errorText.value = '請先按 Wait QC 或 Re QC 載入點檢表。'
    return false
  }
  for (const item of details.value) evaluateItem(item)
  if (recordMessage.value) { errorText.value = recordMessage.value; return false }
  if (overallResult.value === -1) {
    if (callByCheckIn.value) {
      errorText.value = '由報工點檢呼叫時，不允許未完成的項目存檔。'
      return false
    }
    pendingPartialSave.value = true
    partialConfirmOpen.value = true
    return false
  }
  return true
}
function saveRecord(allowPartial = false): void {
  if (!finalValidation() && !(allowPartial && pendingPartialSave.value)) return
  partialConfirmOpen.value = false
  pendingPartialSave.value = false
  const now = nowText()
  const task = selectedCandidate.value
  const prior = editingRecord.value
  const record: InspectionRecord = {
    qcListSerial: prior?.qcListSerial || qcListSerial.value,
    equipmentNo: equipmentNo.value,
    equipmentType: equipmentType.value,
    qcListNo: qcListNo.value,
    qcListName: currentQcList.value?.qcListName || qcListNo.value,
    qcResult: overallResult.value,
    description: description.value,
    creator: prior?.creator || userNo.value,
    createDate: prior?.createDate || now,
    reviser: prior ? userNo.value : '',
    reviseDate: prior ? now : '',
    taskNo: task?.taskNo || prior?.taskNo || 'N/A',
    inspType: task?.inspType ?? prior?.inspType ?? 0,
    shiftNo: task?.shiftNo || prior?.shiftNo || '',
    lotNo: task?.lotNo || prior?.lotNo || '',
    lotSerial: task?.lotSerial || prior?.lotSerial || props.initialLotSerial || '',
    logGroupSerial: task?.logGroupSerial || prior?.logGroupSerial || props.initialLogGroupSerial || '',
    details: cloneDetails(details.value),
  }
  const existingIndex = snapshot.value.records.findIndex(item => item.qcListSerial === record.qcListSerial)
  if (existingIndex >= 0) snapshot.value.records.splice(existingIndex, 1, record)
  else snapshot.value.records.unshift(record)
  if (record.taskNo !== 'N/A') snapshot.value.taskStatuses[record.taskNo] = 1
  persist()
  successText.value = record.qcResult === 0 ? '點檢完成，結果 OK。' : record.qcResult === 1 ? '點檢完成，結果 NG。' : '未完成項目已依確認存入 DEV 假資料。'
  if (currentEquipment.value?.unvalidAction !== -1 && record.qcResult === 1) {
    successText.value += ` 假資料未模擬來源的設備狀態切換（UnValidAction=${currentEquipment.value?.unvalidAction}）。`
  }
  editingRecord.value = null
  if (callByCheckIn.value || prior) {
    window.setTimeout(() => emit('close'), 500)
    return
  }
  clearInspection()
  userAudit.creator = userNo.value
  userAudit.createDate = nowText()
}
function confirmPartialSave(): void { saveRecord(true) }
function loadRecord(record: InspectionRecord): void {
  editingRecord.value = record
  equipmentNo.value = record.equipmentNo
  equipmentType.value = record.equipmentType
  equipmentDescription.value = equipment.find(item => item.equipmentNo === record.equipmentNo)?.name || ''
  qcListNo.value = record.qcListNo
  qcListSerial.value = record.qcListSerial
  currentQcList.value = qcLists.find(item => item.qcListNo === record.qcListNo) || null
  details.value = cloneDetails(record.details)
  description.value = record.description
  userAudit.creator = record.creator
  userAudit.createDate = record.createDate
  userAudit.reviser = userNo.value
  userAudit.reviseDate = nowText()
}
function closePicker(): void { pickerMode.value = null }
function candidateInspType(value: number): string { return value === 0 ? '每批/一般' : value === 1 ? '每班' : value === 2 ? '每日' : String(value) }
function displayResult(value: QcResult): string { return value === 0 ? 'OK' : value === 1 ? 'NG' : '未完成' }
</script>

<template>
  <section class="wp305-page" aria-label="設備線上點檢">
    <div v-if="!dev" class="preview-note">目前為唯讀預覽；切換至 DEV 假資料模式後可操作點檢流程。</div>
    <div v-if="errorText" class="message error-message" role="alert">{{ errorText }}</div>
    <div v-if="successText" class="message success-message" role="status">{{ successText }}</div>

    <fieldset class="inspection-frame">
      <legend>設備線上點檢</legend>
      <div class="top-line">
        <label for="wp305-equipment-type">Equipment Type</label>
        <div class="input-with-lookup">
          <input id="wp305-equipment-type" v-model.trim="equipmentType" class="mes-input" :readonly="lockedEquipment || !dev" @input="onEquipmentTypeInput">
          <button type="button" class="lookup-button" aria-label="選擇設備類別" :disabled="lockedEquipment || !dev" @click="openPicker('equipmentType')">...</button>
        </div>
        <label for="wp305-equipment-no">EquipmentNo</label>
        <div class="input-with-lookup">
          <input id="wp305-equipment-no" v-model="equipmentNo" class="mes-input" :readonly="lockedEquipment || !dev" @input="onEquipmentInput" @change="normalizeEquipment" @keydown.enter.prevent="normalizeEquipment">
          <button type="button" class="lookup-button" aria-label="選擇設備" :disabled="lockedEquipment || !dev" @click="openPicker('equipment')">...</button>
        </div>
        <button v-if="showTaskButtons" type="button" class="mes-button" :disabled="!dev" @click="openPicker('waitQc')">Wait QC</button>
        <button v-if="showTaskButtons" type="button" class="mes-button" :disabled="!dev" @click="openPicker('reQc')">Re QC</button>
      </div>

      <div class="second-line">
        <label for="wp305-qc-serial">QCList Serial</label>
        <input id="wp305-qc-serial" :value="qcListSerial" class="mes-input readonly-input" readonly>
        <label for="wp305-qc-no">QCList No</label>
        <input id="wp305-qc-no" :value="qcListNo" class="mes-input readonly-input" readonly>
        <span v-if="currentQcList" class="qc-list-name">{{ currentQcList.qcListName }}</span>
      </div>

      <div class="grid-wrap">
        <table class="qc-grid">
          <thead>
            <tr>
              <th class="order-col">QCOrder</th>
              <th class="result-col">Result</th>
              <th class="input-col">InputValue</th>
              <th v-for="index in sampleColumns" :key="'d' + index" class="sample-col">D{{ String(index + 1).padStart(2, '0') }}</th>
              <th class="item-col">CheckItem</th>
              <th class="method-col">Inspection Method</th>
              <th class="standard-col">Inspection Standards</th>
              <th class="limit-col">StdValue</th>
              <th class="limit-col">MiniValue</th>
              <th class="limit-col">MaxiValue</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in details" :key="item.qcOrder" :class="{ 'row-ng': item.qcResult === 1, 'row-unset': item.qcResult === -1 }">
              <td class="center-cell">{{ item.qcOrder }}</td>
              <td class="result-cell">
                <template v-if="item.qcType === 2">
                  <button type="button" class="result-choice" :class="{ chosen: item.result === 'Y' }" :disabled="!dev" @click="chooseMessageResult(item, 'Y')">OK</button>
                  <button type="button" class="result-choice ng-choice" :class="{ chosen: item.result === 'N' }" :disabled="!dev" @click="chooseMessageResult(item, 'N')">NG</button>
                </template>
                <span v-else class="result-value" :class="item.qcResult === 0 ? 'pass' : item.qcResult === 1 ? 'fail' : 'unset'">{{ item.result || '—' }}</span>
              </td>
              <td>
                <input v-if="item.qcType !== 2 && item.inputDataCount === 1" v-model="item.inputValue" class="cell-input" :type="item.qcType === 3 ? 'text' : 'number'" :step="item.qcType === 3 ? undefined : 'any'" :disabled="!dev" @input="onItemInput(item)">
                <span v-else-if="item.qcType !== 2" class="input-summary">{{ item.inputValue || '—' }}</span>
                <span v-else class="muted-cell">—</span>
              </td>
              <td v-for="index in sampleColumns" :key="item.qcOrder + '-d' + index">
                <input v-if="item.inputDataCount > 1 && index < item.inputDataCount && item.qcType !== 2" v-model="item.readings[index]" class="cell-input" :type="item.qcType === 3 ? 'text' : 'number'" :step="item.qcType === 3 ? undefined : 'any'" :disabled="!dev" @input="onItemInput(item)">
                <span v-else class="muted-cell">—</span>
              </td>
              <td>{{ item.qcItem }}</td>
              <td>{{ item.inspectionMethod }}</td>
              <td>{{ item.inspectionStandards }}</td>
              <td class="center-cell">{{ item.stdValue || '—' }}</td>
              <td class="center-cell">{{ item.miniValue || '—' }}</td>
              <td class="center-cell">{{ item.maxiValue || '—' }}</td>
            </tr>
            <tr v-if="details.length === 0"><td :colspan="9 + sampleColumns.length" class="empty-grid">先選設備，再按 Wait QC 或 Re QC 載入點檢表。</td></tr>
          </tbody>
        </table>
      </div>

      <label class="description-label" for="wp305-description">Description</label>
      <textarea id="wp305-description" v-model="description" class="description-input" maxlength="255" :disabled="!dev" />

      <div class="result-and-audit">
        <div class="total-result">QCResult：<strong :class="overallResult === 0 ? 'pass' : overallResult === 1 ? 'fail' : 'unset'">{{ overallResultLabel }}</strong></div>
        <div class="audit-grid">
          <label>Creator</label><input :value="userAudit.creator" readonly>
          <label>Create Date</label><input :value="userAudit.createDate" readonly>
          <label>Reviser</label><input :value="userAudit.reviser" readonly>
          <label>Revise Date</label><input :value="userAudit.reviseDate" readonly>
        </div>
      </div>
    </fieldset>

    <div class="page-actions">
      <span class="dev-caption">{{ dev ? 'DEV 假資料，不連線 MES API' : '唯讀預覽' }}<template v-if="qcListSerial">　QCResult {{ overallResultLabel }}</template></span>
      <button type="button" class="confirm-button" :disabled="!dev || details.length === 0" @click="saveRecord()">Confirm</button>
      <button type="button" class="mes-button close-button" @click="emit('close')">Close</button>
    </div>

    <div v-if="pickerMode" class="modal-backdrop" role="presentation" @click.self="closePicker">
      <section class="picker-dialog" role="dialog" aria-modal="true" :aria-label="pickerTitle">
        <h2>{{ pickerTitle }}</h2>
        <label v-if="pickerMode === 'equipmentType' || pickerMode === 'equipment'" class="picker-search">搜尋<input v-model="pickerSearch" class="mes-input" autocomplete="off"></label>
        <div v-if="pickerMode === 'equipmentType'" class="picker-list">
          <button v-for="item in filteredTypes" :key="item" type="button" @click="chooseType(item)"><strong>{{ item }}</strong><span>設備類別</span></button>
          <p v-if="filteredTypes.length === 0" class="empty-grid">沒有符合條件的設備類別</p>
        </div>
        <div v-else-if="pickerMode === 'equipment'" class="picker-list">
          <button v-for="item in filteredEquipment" :key="item.equipmentNo" type="button" @click="chooseEquipment(item)"><strong>{{ item.equipmentNo }}</strong><span>{{ item.equipmentType }}　{{ item.name }}</span></button>
          <p v-if="filteredEquipment.length === 0" class="empty-grid">沒有符合條件的啟用設備</p>
        </div>
        <div v-else class="picker-table-wrap">
          <table class="picker-table">
            <thead><tr><th>QCListNo</th><th>QCListName</th><th>InspType</th><th>InspStartTime</th><th>QCTaskNo</th><th>PlanQCTime</th><th>LotNo</th></tr></thead>
            <tbody>
              <tr v-for="item in pickerRows" :key="item.taskNo + item.qcListNo" tabindex="0" @click="chooseCandidate(item)" @keydown.enter="chooseCandidate(item)">
                <td>{{ item.qcListNo }}</td><td>{{ qcLists.find(list => list.qcListNo === item.qcListNo)?.qcListName }}</td><td>{{ candidateInspType(item.inspType) }}</td><td>{{ item.startTime }}</td><td>{{ item.taskNo }}</td><td>{{ item.plannedAt }}</td><td>{{ item.lotNo || '—' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
        <div class="dialog-actions"><button type="button" class="mes-button" @click="closePicker">Close</button></div>
      </section>
    </div>

    <div v-if="partialConfirmOpen" class="modal-backdrop" role="presentation" @click.self="partialConfirmOpen = false">
      <section class="confirm-dialog" role="dialog" aria-modal="true" aria-labelledby="wp305-partial-title">
        <h2 id="wp305-partial-title">點檢項目尚未完成</h2>
        <p>仍有 QCItem 未定義結果。是否依 WinForms 規則，以 QCResult = -1 儲存部分資料？</p>
        <div class="dialog-actions"><button type="button" class="mes-button" @click="partialConfirmOpen = false">取消</button><button type="button" class="confirm-button" @click="confirmPartialSave">仍要儲存</button></div>
      </section>
    </div>
  </section>
</template>

<style scoped>
.wp305-page{display:flex;flex-direction:column;gap:8px;min-width:0;color:#252525;font-size:13px}.inspection-frame{border:1px solid #b9b9b9;background:#fff;padding:12px 10px 10px;min-width:0;display:flex;flex-direction:column;gap:10px}.inspection-frame legend{padding:0 7px;font-size:15px;font-weight:600;color:#4d4d4d}.top-line{display:grid;grid-template-columns:95px minmax(170px,1fr) 85px minmax(170px,1fr) 88px 88px;align-items:center;gap:8px}.second-line{display:grid;grid-template-columns:95px minmax(160px,1fr) 95px minmax(160px,1fr) minmax(140px,1.5fr);align-items:center;gap:8px}.top-line>label,.second-line>label{text-align:right;white-space:nowrap}.input-with-lookup{display:flex;gap:4px;min-width:0}.mes-input{height:29px;min-width:0;width:100%;padding:4px 7px;border:1px solid #888;background:#fff0b3;color:#222;border-radius:2px;font:inherit}.mes-input:read-only,.readonly-input{background:#eee;color:#555}.lookup-button,.mes-button,.confirm-button{height:30px;padding:3px 12px;border:1px solid #b7b7b7;border-radius:3px;background:linear-gradient(#fff,#f1f1f1);color:#333;font:inherit;white-space:nowrap}.lookup-button{width:31px;padding:2px}.mes-button:hover,.lookup-button:hover{background:#e9e9e9}.mes-button:disabled,.confirm-button:disabled,.lookup-button:disabled{opacity:.55;cursor:not-allowed}.confirm-button{min-width:90px;color:#fff;background:#f5a623;border-color:#e69b1c;font-weight:600}.confirm-button:hover{background:#e99912}.qc-list-name{color:#555;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.grid-wrap{width:100%;overflow:auto;border:1px solid #bcbcbc;max-height:calc(100vh - 390px);min-height:220px}.qc-grid{border-collapse:collapse;min-width:1300px;width:100%;font-size:12px}.qc-grid th{position:sticky;top:0;z-index:1;background:#438fca;color:white;border:1px solid #dedede;padding:6px 5px;font-weight:600;text-align:left;white-space:nowrap}.qc-grid td{height:32px;padding:3px 5px;border:1px solid #d1d1d1;vertical-align:middle;background:#fff1b5}.qc-grid tbody tr:nth-child(even) td{background:#ffeaa2}.qc-grid .order-col{width:56px}.qc-grid .result-col{width:94px}.qc-grid .input-col,.qc-grid .sample-col{width:80px}.qc-grid .item-col{min-width:180px}.qc-grid .method-col,.qc-grid .standard-col{min-width:145px}.qc-grid .limit-col{width:75px}.center-cell{text-align:center}.result-cell{white-space:nowrap;text-align:center}.result-choice{min-width:37px;margin:0 2px;padding:2px 5px;border:1px solid #aaa;border-radius:2px;background:#f7f7f7;font-size:11px}.result-choice.chosen{background:#a7d9a5;border-color:#6eaa6a}.result-choice.ng-choice.chosen{background:#f0b0a8;border-color:#c66c63}.result-value{font-weight:700}.pass{color:#23703b}.fail{color:#b12720}.unset{color:#8a6d3b}.cell-input{width:100%;min-width:45px;height:25px;border:1px solid #8c8c8c;border-radius:2px;background:#fff8d9;padding:3px 5px;font:inherit}.input-summary{display:block;text-align:center;color:#666}.muted-cell{color:#999;display:block;text-align:center}.row-ng td{box-shadow:inset 0 0 0 999px rgba(245,160,150,.08)}.row-unset td{box-shadow:inset 0 0 0 999px rgba(255,238,170,.12)}.empty-grid{text-align:center!important;color:#777!important;background:#f8f8f8!important;padding:18px!important}.description-label{margin:1px 0 -7px 2px;color:#555}.description-input{resize:vertical;min-height:52px;max-height:120px;border:1px solid #888;border-radius:2px;background:#fff0b3;padding:6px;font:inherit}.result-and-audit{display:grid;grid-template-columns:1fr minmax(480px,1.2fr);gap:10px;align-items:end}.total-result{padding:8px 5px;color:#555}.audit-grid{display:grid;grid-template-columns:82px minmax(100px,1fr) 82px minmax(155px,1fr);align-items:center;gap:6px}.audit-grid label{text-align:right;color:#555}.audit-grid input{min-width:0;height:27px;padding:4px 6px;border:1px solid #ccc;background:#eee;color:#666;font:inherit}.page-actions{display:flex;align-items:center;justify-content:flex-end;gap:8px}.dev-caption{margin-right:auto;color:#777;font-size:12px}.close-button{min-width:84px}.message,.preview-note{padding:8px 10px;border:1px solid;border-radius:3px}.error-message{border-color:#d8aaa6;background:#fff0ef;color:#9c241d}.success-message{border-color:#a9c6a2;background:#f1faed;color:#2e6332}.preview-note{border-color:#d5c9a7;background:#fff9e8;color:#655321}.modal-backdrop{position:fixed;inset:0;z-index:1000;background:rgba(0,0,0,.32);display:grid;place-items:center;padding:18px}.picker-dialog,.confirm-dialog{width:min(900px,96vw);max-height:88vh;overflow:auto;background:#fff;border:1px solid #aaa;box-shadow:0 8px 28px rgba(0,0,0,.25);padding:16px}.confirm-dialog{width:min(480px,96vw)}.picker-dialog h2,.confirm-dialog h2{margin:0 0 14px;font-size:16px;font-weight:600;color:#444}.confirm-dialog p{line-height:1.6}.picker-search{display:flex;align-items:center;gap:10px;margin-bottom:10px}.picker-search .mes-input{max-width:360px}.picker-list{max-height:55vh;overflow:auto;border:1px solid #ccc}.picker-list button{display:flex;width:100%;align-items:center;gap:18px;padding:10px 12px;text-align:left;border:0;border-bottom:1px solid #ddd;background:#fff;color:#222}.picker-list button:hover{background:#fff1b5}.picker-list button strong{min-width:190px}.picker-list button span{color:#666}.picker-table-wrap{overflow:auto;max-height:60vh;border:1px solid #c8c8c8}.picker-table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}.picker-table th{position:sticky;top:0;background:#438fca;color:white;text-align:left;padding:7px;border:1px solid #ddd}.picker-table td{padding:7px;border:1px solid #ddd}.picker-table tbody tr{cursor:pointer}.picker-table tbody tr:nth-child(even){background:#fff8df}.picker-table tbody tr:hover{background:#ffe69a}.dialog-actions{display:flex;justify-content:flex-end;gap:8px;margin-top:14px}@media(max-width:1000px){.top-line{grid-template-columns:95px minmax(140px,1fr) 85px minmax(140px,1fr)}.top-line button.mes-button{grid-column:auto}.second-line{grid-template-columns:95px minmax(140px,1fr) 95px minmax(140px,1fr)}.second-line .qc-list-name{grid-column:2 / -1}.result-and-audit{grid-template-columns:1fr}.audit-grid{max-width:800px}}@media(max-width:680px){.top-line,.second-line{grid-template-columns:1fr}.top-line>label,.second-line>label{text-align:left}.input-with-lookup{width:100%}.top-line button.mes-button{width:100%}.second-line .qc-list-name{grid-column:auto}.audit-grid{grid-template-columns:80px minmax(100px,1fr)}.page-actions{flex-wrap:wrap}.dev-caption{flex-basis:100%}}
</style>
