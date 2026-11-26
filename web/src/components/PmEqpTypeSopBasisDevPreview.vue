<script setup lang="ts">
import { computed, ref, watch } from 'vue'

interface YearChoice { yearNo: string; startDate: string; endDate: string }
interface EquipmentChoice { equipmentType: string; equipmentTypeName: string }
interface FrequencyChoice { frequencyNo: string; frequencyName: string; fixedType: 0 | 1 | 2 | 3; fixedTypeValue: string; period: number | null }
interface SopChoice { sopNo: string; sopName: string }
interface EquipmentStateChoice { equipmentState: number; stateName: string }
interface SopBasis {
  pmeqpSopNo: string; pmYearNo: string; equipmentType: string; equipmentTypeName: string
  frequencyNo: string; frequencyName: string; sopNo: string; sopName: string
  toleranceQty: number; autoCreateWo: boolean; createAndReleaseWo: boolean; beforehandDay: number | null
  lockEquipment: boolean; equipmentState: number | null; stateName: string; autoEmail: boolean
  hurryTime: number; overDueTime: number; description: string
  creator: string; createDate: string; reviser: string; reviseDate: string; dataStamp: number
}
interface PlanRow { serialNo: string; pmeqpSopNo: string; pmSerialNo: number; equipmentNo: string; frequencyNo: string; sopNo: string; estimatedDate: string; executeState: number; creator: string; createDate: string }
interface PlanLogRow extends PlanRow { logCreator: string; logCreateDate: string }
interface DevState { version: 1; records: SopBasis[]; plans: PlanRow[]; planLogs: PlanLogRow[] }
type FormMode = 'add' | 'edit' | 'copy'
type AuxiliaryMode = 'plans' | 'plan-set' | 'batch-copy'
type SopDraft = Omit<SopBasis, 'pmeqpSopNo' | 'creator' | 'createDate' | 'reviser' | 'reviseDate' | 'dataStamp' | 'beforehandDay'> & { beforehandDay: number | string | null }

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.pm.eqp-type-sop-basis.v1'
const currentUser = 'DEV01'
const years: YearChoice[] = [
  { yearNo: '2025', startDate: '2025-01-01', endDate: '2025-12-31' },
  { yearNo: '2026', startDate: '2026-01-01', endDate: '2026-12-31' },
  { yearNo: '2027', startDate: '2027-01-01', endDate: '2027-12-31' },
  { yearNo: '2028', startDate: '2028-01-01', endDate: '2028-12-31' },
]
const equipmentChoices: EquipmentChoice[] = [
  { equipmentType: 'CNC-200', equipmentTypeName: 'CNC 加工中心' },
  { equipmentType: 'PUMP-120', equipmentTypeName: '製程冷卻泵' },
  { equipmentType: 'OVEN-50', equipmentTypeName: '真空烘箱' },
  { equipmentType: 'CHILLER-01', equipmentTypeName: '冷卻機' },
]
const fallbackFrequencies: FrequencyChoice[] = [
  { frequencyNo: 'PM-DAILY-01', frequencyName: '每日點檢', fixedType: 0, fixedTypeValue: '', period: 1 },
  { frequencyNo: 'PM-WEEKLY-01', frequencyName: '每週保養', fixedType: 1, fixedTypeValue: '1', period: null },
  { frequencyNo: 'PM-MONTHLY-15', frequencyName: '月中設備校驗', fixedType: 2, fixedTypeValue: '15', period: null },
  { frequencyNo: 'PM-YEARLY-06', frequencyName: '年度保養', fixedType: 3, fixedTypeValue: '6/30', period: null },
]
const fallbackSops: SopChoice[] = [
  { sopNo: 'SOP-CLEAN-01', sopName: '腔體定期清潔程序' },
  { sopNo: 'SOP-CAL-02', sopName: '感測器校正程序' },
  { sopNo: 'SOP-LUBE-03', sopName: '主軸潤滑保養程序' },
  { sopNo: 'SOP-OLD-00', sopName: '舊式風扇檢查程序' },
]
const equipmentStates: EquipmentStateChoice[] = [
  { equipmentState: 1, stateName: '待機' }, { equipmentState: 2, stateName: '運轉中' },
  { equipmentState: 3, stateName: '維修中' }, { equipmentState: 4, stateName: '停機' },
]

function seedState(): DevState {
  const records: SopBasis[] = [
    { pmeqpSopNo: 'EQPSOP2600001', pmYearNo: '2026', equipmentType: 'CNC-200', equipmentTypeName: 'CNC 加工中心', frequencyNo: 'PM-MONTHLY-15', frequencyName: '月中設備校驗', sopNo: 'SOP-CAL-02', sopName: '感測器校正程序', toleranceQty: 0, autoCreateWo: true, createAndReleaseWo: false, beforehandDay: 3, lockEquipment: true, equipmentState: 3, stateName: '維修中', autoEmail: true, hurryTime: 0, overDueTime: 0, description: '依設備類型與年度頻率建立保養標準。', creator: 'PMUSER', createDate: '2026-01-08 09:30', reviser: 'PMUSER', reviseDate: '2026-03-04 15:20', dataStamp: 2 },
    { pmeqpSopNo: 'EQPSOP2600002', pmYearNo: '2026', equipmentType: 'PUMP-120', equipmentTypeName: '製程冷卻泵', frequencyNo: 'PM-WEEKLY-01', frequencyName: '每週保養', sopNo: 'SOP-LUBE-03', sopName: '主軸潤滑保養程序', toleranceQty: 0, autoCreateWo: false, createAndReleaseWo: false, beforehandDay: null, lockEquipment: false, equipmentState: null, stateName: '', autoEmail: false, hurryTime: 0, overDueTime: 0, description: '週保養由現場排程人員確認。', creator: 'DEV02', createDate: '2026-02-10 11:15', reviser: '', reviseDate: '', dataStamp: 0 },
    { pmeqpSopNo: 'EQPSOP2500001', pmYearNo: '2025', equipmentType: 'OVEN-50', equipmentTypeName: '真空烘箱', frequencyNo: 'PM-YEARLY-06', frequencyName: '年度保養', sopNo: 'SOP-CLEAN-01', sopName: '腔體定期清潔程序', toleranceQty: 0, autoCreateWo: true, createAndReleaseWo: true, beforehandDay: 5, lockEquipment: true, equipmentState: 4, stateName: '停機', autoEmail: false, hurryTime: 2, overDueTime: 1, description: '年度停機保養範例。', creator: 'PMUSER', createDate: '2025-01-03 08:20', reviser: 'PMUSER', reviseDate: '2025-05-12 10:00', dataStamp: 1 },
  ]
  return {
    version: 1,
    records,
    plans: [
      { serialNo: 'DEV-PLAN-0001', pmeqpSopNo: 'EQPSOP2600001', pmSerialNo: 1, equipmentNo: 'CNC-02', frequencyNo: 'PM-MONTHLY-15', sopNo: 'SOP-CAL-02', estimatedDate: '2026-12-15', executeState: 0, creator: 'PMUSER', createDate: '2026-01-08 09:30' },
      { serialNo: 'DEV-PLAN-0002', pmeqpSopNo: 'EQPSOP2600001', pmSerialNo: 2, equipmentNo: 'CNC-02', frequencyNo: 'PM-MONTHLY-15', sopNo: 'SOP-CAL-02', estimatedDate: '2026-11-15', executeState: 1, creator: 'PMUSER', createDate: '2026-01-08 09:30' },
      { serialNo: 'DEV-PLAN-0003', pmeqpSopNo: 'EQPSOP2600002', pmSerialNo: 1, equipmentNo: 'PUMP-07', frequencyNo: 'PM-WEEKLY-01', sopNo: 'SOP-LUBE-03', estimatedDate: '2026-11-30', executeState: 0, creator: 'DEV02', createDate: '2026-02-10 11:15' },
    ],
    planLogs: [],
  }
}
function loadState(): DevState {
  try {
    const value = JSON.parse(window.localStorage.getItem(storageKey) ?? 'null') as DevState | null
    if (value?.version === 1 && Array.isArray(value.records) && Array.isArray(value.plans) && Array.isArray(value.planLogs)) return value
  } catch { /* Keep the DEV preview usable with in-memory fixtures. */ }
  return seedState()
}
function readFrequencyChoices(): FrequencyChoice[] {
  try {
    const data = JSON.parse(window.localStorage.getItem('ymes.dev.pm.frequency-basis.v1') ?? 'null') as { records?: Array<Record<string, unknown>> } | null
    if (Array.isArray(data?.records)) {
      const rows = data.records.map((row) => ({ frequencyNo: String(row.frequencyNo ?? ''), frequencyName: String(row.frequencyName ?? ''), fixedType: Number(row.fixedType ?? 0) as FrequencyChoice['fixedType'], fixedTypeValue: String(row.fixedTypeValue ?? ''), period: row.period == null ? null : Number(row.period) })).filter((row) => row.frequencyNo)
      if (rows.length) return rows
    }
  } catch { /* Use local fallback choices when the PM frequency page has not been used. */ }
  return fallbackFrequencies
}
function readSopChoices(): SopChoice[] {
  try {
    const data = JSON.parse(window.localStorage.getItem('ymes.dev.pm.sop-basis.v1') ?? 'null') as { records?: Array<Record<string, unknown>> } | null
    if (Array.isArray(data?.records)) {
      const rows = data.records.map((row) => ({ sopNo: String(row.sopNo ?? ''), sopName: String(row.sopName ?? '') })).filter((row) => row.sopNo)
      if (rows.length) return rows
    }
  } catch { /* Use local fallback choices when the PM SOP page has not been used. */ }
  return fallbackSops
}

const state = ref<DevState>(loadState())
const frequencyChoices = ref(readFrequencyChoices())
const sopChoices = ref(readSopChoices())
const searchNo = ref('')
const activeNo = ref('')
const selectedNos = ref<string[]>([])
const formMode = ref<FormMode | null>(null)
const draft = ref<SopDraft | null>(null)
const auxiliaryMode = ref<AuxiliaryMode | null>(null)
const batchSourceYear = ref('2025')
const batchEquipmentType = ref('')
const batchTargetYear = ref('2026')
const batchLaunchDate = ref('2026-01-01')
const batchSelectedNos = ref<string[]>([])
const message = ref('')
const messageKind = ref<'success' | 'error'>('success')
const nowLabel = computed(() => new Date().toLocaleString('zh-TW', { hour12: false }))
const visibleRows = computed(() => state.value.records
  .filter((row) => !searchNo.value.trim() || row.pmeqpSopNo.toLocaleLowerCase().includes(searchNo.value.trim().toLocaleLowerCase()))
  .sort((a, b) => a.pmeqpSopNo.localeCompare(b.pmeqpSopNo)))
const activeRecord = computed(() => state.value.records.find((row) => row.pmeqpSopNo === activeNo.value) ?? null)
const actionTargets = computed(() => selectedNos.value.length ? selectedNos.value : activeRecord.value ? [activeRecord.value.pmeqpSopNo] : [])
const allVisibleSelected = computed(() => visibleRows.value.length > 0 && visibleRows.value.every((row) => selectedNos.value.includes(row.pmeqpSopNo)))
const activePlans = computed(() => state.value.plans.filter((row) => row.pmeqpSopNo === activeRecord.value?.pmeqpSopNo).sort((a, b) => a.estimatedDate.localeCompare(b.estimatedDate)))
const batchSourceRows = computed(() => state.value.records.filter((row) => row.pmYearNo === batchSourceYear.value && (!batchEquipmentType.value || row.equipmentType === batchEquipmentType.value)).sort((a, b) => a.pmeqpSopNo.localeCompare(b.pmeqpSopNo)))
const allBatchRowsSelected = computed(() => batchSourceRows.value.length > 0 && batchSourceRows.value.every((row) => batchSelectedNos.value.includes(row.pmeqpSopNo)))
watch(state, (value) => { try { window.localStorage.setItem(storageKey, JSON.stringify(value)) } catch { /* Keep DEV actions in memory if storage is unavailable. */ } }, { deep: true })
watch(visibleRows, (rows) => {
  if (!rows.some((row) => row.pmeqpSopNo === activeNo.value)) activeNo.value = rows[0]?.pmeqpSopNo ?? ''
  selectedNos.value = selectedNos.value.filter((no) => rows.some((row) => row.pmeqpSopNo === no))
}, { immediate: true })
watch(batchSourceRows, (rows) => {
  batchSelectedNos.value = batchSelectedNos.value.filter((no) => rows.some((row) => row.pmeqpSopNo === no))
})
watch(() => draft.value?.autoCreateWo, (enabled) => { if (!enabled && draft.value) { draft.value.beforehandDay = null; draft.value.createAndReleaseWo = false } })
watch(() => draft.value?.lockEquipment, (enabled) => { if (!enabled && draft.value) draft.value.equipmentState = null })

function toast(text: string, kind: 'success' | 'error' = 'success') { message.value = text; messageKind.value = kind }
function selectRecord(no: string) { activeNo.value = no; if (!selectedNos.value.includes(no)) selectedNos.value = [] }
function toggleRecord(no: string) { selectedNos.value = selectedNos.value.includes(no) ? selectedNos.value.filter((value) => value !== no) : [...selectedNos.value, no]; activeNo.value = no }
function toggleAllVisible() { selectedNos.value = allVisibleSelected.value ? [] : visibleRows.value.map((row) => row.pmeqpSopNo); if (selectedNos.value.length) activeNo.value = selectedNos.value[0] }
function freshId(): string {
  const prefix = `EQPSOP${String(new Date().getFullYear()).slice(-2)}`
  const largest = state.value.records.map((row) => Number(row.pmeqpSopNo.startsWith(prefix) ? row.pmeqpSopNo.slice(prefix.length) : 0)).filter(Number.isFinite).reduce((max, value) => Math.max(max, value), 0)
  return `${prefix}${String(largest + 1).padStart(5, '0')}`
}
function makeDraft(record?: SopBasis): SopDraft {
  return record ? {
    pmYearNo: record.pmYearNo, equipmentType: record.equipmentType, equipmentTypeName: record.equipmentTypeName,
    frequencyNo: record.frequencyNo, frequencyName: record.frequencyName, sopNo: record.sopNo, sopName: record.sopName,
    toleranceQty: record.toleranceQty, autoCreateWo: record.autoCreateWo, createAndReleaseWo: record.createAndReleaseWo,
    beforehandDay: record.beforehandDay, lockEquipment: record.lockEquipment, equipmentState: record.equipmentState,
    stateName: record.stateName, autoEmail: record.autoEmail, hurryTime: record.hurryTime, overDueTime: record.overDueTime, description: record.description,
  } : {
    pmYearNo: '', equipmentType: '', equipmentTypeName: '', frequencyNo: '', frequencyName: '', sopNo: '', sopName: '',
    toleranceQty: 0, autoCreateWo: false, createAndReleaseWo: false, beforehandDay: null, lockEquipment: false,
    equipmentState: null, stateName: '', autoEmail: false, hurryTime: 0, overDueTime: 0, description: '',
  }
}
function openForm(mode: FormMode) {
  const record = activeRecord.value
  if (mode !== 'add' && !record) return toast('請先選取保養標準資料。', 'error')
  formMode.value = mode
  draft.value = mode === 'add' ? makeDraft() : makeDraft(record!)
}
function onEquipmentTypeChange(value: string) { const option = equipmentChoices.find((row) => row.equipmentType === value); if (draft.value && option) { draft.value.equipmentType = value; draft.value.equipmentTypeName = option.equipmentTypeName } }
function onFrequencyChange(value: string) { const option = frequencyChoices.value.find((row) => row.frequencyNo === value); if (draft.value && option) { draft.value.frequencyNo = value; draft.value.frequencyName = option.frequencyName } }
function onSopChange(value: string) { const option = sopChoices.value.find((row) => row.sopNo === value); if (draft.value && option) { draft.value.sopNo = value; draft.value.sopName = option.sopName } }
function onEquipmentStateChange(value: string) { const option = equipmentStates.find((row) => row.equipmentState === Number(value)); if (draft.value) { draft.value.equipmentState = option?.equipmentState ?? null; draft.value.stateName = option?.stateName ?? '' } }
function validateDraft(form: SopDraft): string {
  if (!form.pmYearNo || !form.equipmentType || !form.frequencyNo || !form.sopNo) return '年度、設備類型、保養頻率與 SOP 為必填。'
  if (form.lockEquipment && form.equipmentState == null) return '啟用設備鎖定時必須指定設備狀態。'
  if (form.beforehandDay !== null && String(form.beforehandDay).trim() !== '' && (!Number.isInteger(Number(form.beforehandDay)) || Number(form.beforehandDay) <= 0)) return '提前天數必須為大於 0 的整數。'
  return ''
}
function saveForm(closeAfter: boolean) {
  if (!draft.value || !formMode.value) return
  const form = draft.value
  const error = validateDraft(form)
  if (error) return toast(error, 'error')
  const normalizedForm: Omit<SopBasis, 'pmeqpSopNo' | 'creator' | 'createDate' | 'reviser' | 'reviseDate' | 'dataStamp'> = {
    ...form,
    beforehandDay: form.beforehandDay === null || String(form.beforehandDay).trim() === '' ? null : Number(form.beforehandDay),
  }
  if (formMode.value === 'edit' && activeRecord.value) {
    const record = activeRecord.value
    const oldDescription = record.description
    Object.assign(record, normalizedForm, { description: form.description || oldDescription, reviser: currentUser, reviseDate: nowLabel.value, dataStamp: record.dataStamp + 1 })
    toast('設備保養標準資料已在 DEV 假資料中修改。')
  } else {
    const record: SopBasis = { ...normalizedForm, pmeqpSopNo: freshId(), creator: currentUser, createDate: nowLabel.value, reviser: '', reviseDate: '', dataStamp: 0, toleranceQty: 0 }
    state.value.records.push(record)
    activeNo.value = record.pmeqpSopNo
    searchNo.value = ''
    selectedNos.value = []
    toast(formMode.value === 'copy' ? '保養標準設定已複製到 DEV 假資料。' : '設備保養標準資料已新增到 DEV 假資料。')
  }
  if (closeAfter) { formMode.value = null; draft.value = null }
}
function deleteRows() {
  const targets = [...actionTargets.value]
  if (!targets.length || !window.confirm(`確定刪除 ${targets.length} 筆標準設定？既有計畫會移入計畫歷史。`)) return
  let archivedCount = 0
  for (const no of targets) {
    const planRows = state.value.plans.filter((row) => row.pmeqpSopNo === no)
    for (const row of planRows) state.value.planLogs.push({ ...row, executeState: 5, logCreator: currentUser, logCreateDate: nowLabel.value })
    archivedCount += planRows.length
    state.value.plans = state.value.plans.filter((row) => row.pmeqpSopNo !== no)
    state.value.records = state.value.records.filter((row) => row.pmeqpSopNo !== no)
  }
  activeNo.value = ''; selectedNos.value = []
  toast(`已刪除 ${targets.length} 筆設定，並將 ${archivedCount} 筆計畫移入 DEV 歷史。`)
}
function openAuxiliary(mode: AuxiliaryMode) {
  if (!activeRecord.value) return toast('請先選取保養標準資料。', 'error')
  auxiliaryMode.value = mode
  if (mode === 'batch-copy') { batchSourceYear.value = activeRecord.value.pmYearNo; batchEquipmentType.value = activeRecord.value.equipmentType; batchTargetYear.value = String(Number(activeRecord.value.pmYearNo) + 1); batchLaunchDate.value = `${batchTargetYear.value}-01-01`; batchSelectedNos.value = [] }
}
function toggleBatchRow(no: string) { batchSelectedNos.value = batchSelectedNos.value.includes(no) ? batchSelectedNos.value.filter((value) => value !== no) : [...batchSelectedNos.value, no] }
function toggleAllBatchRows() { batchSelectedNos.value = allBatchRowsSelected.value ? [] : batchSourceRows.value.map((row) => row.pmeqpSopNo) }
function nextPlanDates(startText: string, year: YearChoice, frequency: FrequencyChoice): string[] {
  const end = new Date(`${year.yearNo}-12-31T00:00:00`)
  const now = new Date(); const requested = new Date(`${startText}T00:00:00`)
  const date = requested <= new Date(now.getFullYear(), now.getMonth(), now.getDate()) ? new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1) : requested
  const dates: string[] = []
  const add = (value: Date) => { dates.push(`${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, '0')}-${String(value.getDate()).padStart(2, '0')}`) }
  if (frequency.fixedType === 3) {
    if (frequency.fixedTypeValue) { const parts = frequency.fixedTypeValue.split('/').map(Number); date.setFullYear(Number(year.yearNo), (parts[0] || 1) - 1, parts[1] || 1) }
    if (date <= end && date > now) add(date)
  } else if (frequency.fixedType === 1 && frequency.fixedTypeValue) {
    const targetDay = (Number(frequency.fixedTypeValue) || 0) + 1
    const currentDay = date.getDay() + 1
    date.setDate(date.getDate() + (targetDay - currentDay + 7) % 7)
    while (date <= end && dates.length < 60) { add(date); date.setDate(date.getDate() + 7) }
  } else if (frequency.fixedType === 2 && frequency.fixedTypeValue) {
    const day = Math.max(1, Number(frequency.fixedTypeValue) || 1)
    let cursor = new Date(date.getFullYear(), date.getMonth(), day)
    if (cursor <= now) cursor = new Date(cursor.getFullYear(), cursor.getMonth() + 1, day)
    while (cursor <= end && dates.length < 100) { add(cursor); cursor = new Date(cursor.getFullYear(), cursor.getMonth() + 1, day) }
  } else {
    const step = frequency.fixedType === 0 ? Math.max(1, frequency.period ?? 1) : frequency.fixedType === 1 ? 7 : Math.max(1, frequency.period ?? 30)
    while (date <= end && dates.length < 400) { add(date); date.setDate(date.getDate() + step) }
  }
  return dates
}
function runBatchCopy() {
  if (!batchSelectedNos.value.length) return toast('請至少勾選一筆設備保養標準。', 'error')
  const targetYear = years.find((row) => row.yearNo === batchTargetYear.value)
  if (!targetYear) return toast('請選擇有效的目標保養年度。', 'error')
  if (new Date(`${batchLaunchDate.value}T00:00:00`).getFullYear() !== Number(batchTargetYear.value)) return toast('計畫啟動日年度必須與新保養年度相同。', 'error')
  let copied = 0; let generatedPlans = 0
  for (const sourceNo of batchSelectedNos.value) {
    const source = state.value.records.find((row) => row.pmeqpSopNo === sourceNo)
    if (!source) continue
    const id = freshId()
    const copy: SopBasis = { ...source, pmeqpSopNo: id, pmYearNo: batchTargetYear.value, creator: currentUser, createDate: nowLabel.value, reviser: '', reviseDate: '', dataStamp: 0 }
    state.value.records.push(copy)
    const equipmentNos = [...new Set(state.value.plans.filter((row) => row.pmeqpSopNo === sourceNo).map((row) => row.equipmentNo))]
    const frequency = frequencyChoices.value.find((row) => row.frequencyNo === source.frequencyNo) ?? fallbackFrequencies[0]
    const planDates = nextPlanDates(batchLaunchDate.value, targetYear, frequency)
    for (const equipmentNo of equipmentNos) for (const [index, estimatedDate] of planDates.entries()) {
      state.value.plans.push({ serialNo: `DEV-${id}-${equipmentNo}-${index + 1}`, pmeqpSopNo: id, pmSerialNo: index + 1, equipmentNo, frequencyNo: source.frequencyNo, sopNo: source.sopNo, estimatedDate, executeState: 0, creator: currentUser, createDate: nowLabel.value })
      generatedPlans += 1
    }
    copied += 1
  }
  toast(`DEV 已複製 ${copied} 筆標準設定並模擬產生 ${generatedPlans} 筆保養計畫。`)
  auxiliaryMode.value = null
  searchNo.value = ''
}
</script>

<template>
  <section class="pm-eqp-type-sop" aria-label="設備保養標準程序 DEV 預覽">
    <header class="page-header"><div><p>設備 PM 設定／EQPTYPE SOP BASIS</p><h1>設備保養標準程序</h1></div><div class="header-actions"><span class="dev-tag">DEV 假資料</span><button type="button" class="secondary" @click="emit('close')">Close (X)</button></div></header>
    <div class="filter-row"><label>PMEQPSOPNo <input v-model="searchNo" autocomplete="off" placeholder="搜尋設定編號"></label><span class="row-count">{{ visibleRows.length }} 筆</span></div>
    <div class="toolbar"><button type="button" @click="openForm('add')">新增</button><button type="button" :disabled="!activeRecord" @click="openForm('edit')">修改</button><button type="button" :disabled="!activeRecord" @click="openForm('copy')">複製</button><button type="button" :disabled="!actionTargets.length" @click="deleteRows">刪除</button><span class="toolbar-gap"></span><button type="button" class="secondary" :disabled="!activeRecord" @click="openAuxiliary('plans')">保養計劃資料</button><button type="button" class="secondary" :disabled="!activeRecord" @click="openAuxiliary('plan-set')">計劃設定</button><button type="button" class="secondary" :disabled="!activeRecord" @click="openAuxiliary('batch-copy')">批次複製</button></div>
    <div class="table-wrap"><table><thead><tr><th class="check-col"><input type="checkbox" :checked="allVisibleSelected" aria-label="全選" @change="toggleAllVisible"></th><th>PMEQPSOPNo</th><th>PMYearNo</th><th>EquipmentType</th><th>FrequencyNo</th><th>PMSOPNo</th><th>FrequencyName</th><th>PMSOPName</th><th>AutoCreateWO</th><th>CreateAndReleaseWO</th><th>BeforehandDay</th><th>LockEquipment</th><th>EquipmentState</th><th>StateName</th><th>AutoEmail</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th><th>Description</th></tr></thead><tbody>
      <tr v-for="record in visibleRows" :key="record.pmeqpSopNo" :class="{ selected: activeNo === record.pmeqpSopNo }" @click="selectRecord(record.pmeqpSopNo)"><td class="check-col" @click.stop><input type="checkbox" :checked="selectedNos.includes(record.pmeqpSopNo)" :aria-label="`選取 ${record.pmeqpSopNo}`" @change="toggleRecord(record.pmeqpSopNo)"></td><td>{{ record.pmeqpSopNo }}</td><td>{{ record.pmYearNo }}</td><td>{{ record.equipmentType }}</td><td>{{ record.frequencyNo }}</td><td>{{ record.sopNo }}</td><td>{{ record.frequencyName }}</td><td>{{ record.sopName }}</td><td>{{ record.autoCreateWo ? 'Yes' : 'No' }}</td><td>{{ record.createAndReleaseWo ? 'Yes' : 'No' }}</td><td class="number-cell">{{ record.beforehandDay ?? '' }}</td><td>{{ record.lockEquipment ? 'Yes' : 'No' }}</td><td class="number-cell">{{ record.equipmentState ?? '' }}</td><td>{{ record.stateName }}</td><td>{{ record.autoEmail ? 'Yes' : 'No' }}</td><td>{{ record.creator }}</td><td>{{ record.createDate }}</td><td>{{ record.reviser }}</td><td>{{ record.reviseDate }}</td><td class="description-cell">{{ record.description }}</td></tr>
      <tr v-if="visibleRows.length === 0"><td colspan="20" class="empty">沒有符合條件的保養標準資料。</td></tr>
    </tbody></table></div>
    <p v-if="message" class="toast" :class="messageKind" role="status">{{ message }}</p>
    <footer class="page-footer"><span>DEV localStorage：{{ storageKey }}</span><span>PMEQPSOPNo 排序｜隱藏舊欄位：ToleranceQty、HurryTime、OverDueTime｜DataStamp {{ activeRecord?.dataStamp ?? '—' }}</span></footer>

    <div v-if="formMode && draft" class="modal-backdrop" @click.self="formMode = null; draft = null"><section class="dialog form-dialog" role="dialog" aria-modal="true" aria-labelledby="eqp-sop-title"><header><div><p>EQPTYPE SOP BASIS / {{ formMode.toUpperCase() }}</p><h2 id="eqp-sop-title">{{ formMode === 'add' ? '新增設備保養標準' : formMode === 'edit' ? '修改設備保養標準' : '複製設備保養標準' }}</h2></div><button type="button" class="secondary close-x" aria-label="關閉" @click="formMode = null; draft = null">×</button></header>
      <div class="form-grid"><label>PM Year No <select v-model="draft.pmYearNo"><option value="">選擇年度</option><option v-for="year in years" :key="year.yearNo" :value="year.yearNo">{{ year.yearNo }}</option></select></label>
        <label>Equipment Type <select :value="draft.equipmentType" @change="onEquipmentTypeChange(($event.target as HTMLSelectElement).value)"><option value="">選擇設備類型</option><option v-for="row in equipmentChoices" :key="row.equipmentType" :value="row.equipmentType">{{ row.equipmentType }} — {{ row.equipmentTypeName }}</option></select></label>
        <label>Frequency No <select :value="draft.frequencyNo" @change="onFrequencyChange(($event.target as HTMLSelectElement).value)"><option value="">選擇保養頻率</option><option v-for="row in frequencyChoices" :key="row.frequencyNo" :value="row.frequencyNo">{{ row.frequencyNo }} — {{ row.frequencyName }}</option></select></label>
        <label>SOP No <select :value="draft.sopNo" @change="onSopChange(($event.target as HTMLSelectElement).value)"><option value="">選擇 SOP</option><option v-for="row in sopChoices" :key="row.sopNo" :value="row.sopNo">{{ row.sopNo }} — {{ row.sopName }}</option></select></label>
      </div>
      <div class="option-grid"><fieldset><legend>Auto Create WO</legend><label><input v-model="draft.autoCreateWo" type="radio" :value="true"> Yes</label><label><input v-model="draft.autoCreateWo" type="radio" :value="false"> No</label><label class="inline-field">Beforehand Day <input v-model="draft.beforehandDay" type="number" min="1" step="1" :disabled="!draft.autoCreateWo" placeholder="選填"></label><fieldset class="nested"><legend>Create And Release WO</legend><label><input v-model="draft.createAndReleaseWo" type="radio" :value="true" :disabled="!draft.autoCreateWo"> Yes</label><label><input v-model="draft.createAndReleaseWo" type="radio" :value="false" :disabled="!draft.autoCreateWo"> No</label></fieldset></fieldset>
        <fieldset><legend>Lock Equipment</legend><label><input v-model="draft.lockEquipment" type="radio" :value="true"> Yes</label><label><input v-model="draft.lockEquipment" type="radio" :value="false"> No</label><label>Equipment State <select :value="draft.equipmentState ?? ''" :disabled="!draft.lockEquipment" @change="onEquipmentStateChange(($event.target as HTMLSelectElement).value)"><option value="">選擇設備狀態</option><option v-for="row in equipmentStates" :key="row.equipmentState" :value="row.equipmentState">{{ row.equipmentState }} — {{ row.stateName }}</option></select></label></fieldset>
        <fieldset><legend>Auto Email</legend><label><input v-model="draft.autoEmail" type="radio" :value="true"> Yes</label><label><input v-model="draft.autoEmail" type="radio" :value="false"> No</label><p class="hint">寄送通知由正式流程處理；DEV 只保存設定。</p></fieldset></div>
      <label>Description <textarea v-model="draft.description" rows="3"></textarea></label><p class="form-note">空白 Description 在舊 Core 修改時代表未提供，因此無法清除既有內容。HurryTime、OverDueTime、ToleranceQty 在 WinForms 中隱藏，不提供編輯。</p>
      <footer><button type="button" class="secondary" @click="formMode = null; draft = null">取消</button><button type="button" class="secondary" @click="saveForm(false)">Apply</button><button type="button" @click="saveForm(true)">Confirm</button></footer>
    </section></div>

    <div v-if="auxiliaryMode" class="modal-backdrop" @click.self="auxiliaryMode = null"><section class="dialog auxiliary-dialog" role="dialog" aria-modal="true" aria-labelledby="aux-title"><header><div><p>{{ auxiliaryMode === 'batch-copy' ? 'BATCH COPY' : 'MAINTENANCE PLAN' }} / DEV</p><h2 id="aux-title">{{ auxiliaryMode === 'plans' ? '保養計劃資料' : auxiliaryMode === 'plan-set' ? '計劃設定' : '保養標準批次複製' }}</h2></div><button type="button" class="secondary close-x" aria-label="關閉" @click="auxiliaryMode = null">×</button></header>
      <template v-if="auxiliaryMode === 'plans'"><p class="context-line">{{ activeRecord?.pmeqpSopNo }}｜{{ activeRecord?.equipmentType }}｜{{ activeRecord?.frequencyName }}</p><div class="table-wrap compact-table"><table><thead><tr><th>Equipment No</th><th>PM Serial No</th><th>PM Est Date</th><th>Execute State</th><th>Serial No</th></tr></thead><tbody><tr v-for="row in activePlans" :key="row.serialNo"><td>{{ row.equipmentNo }}</td><td>{{ row.pmSerialNo }}</td><td>{{ row.estimatedDate }}</td><td>{{ row.executeState }}</td><td>{{ row.serialNo }}</td></tr><tr v-if="activePlans.length === 0"><td colspan="5" class="empty">此筆 DEV 標準目前沒有計畫列。</td></tr></tbody></table></div><p class="form-note">WinForms 由 ObjectFileHandler 開啟獨立的 PM_EQPMaintenancePlan；此預覽提供唯讀樣例，未復刻該外部視窗的排程編輯。</p></template>
      <template v-else-if="auxiliaryMode === 'plan-set'"><p class="context-line">{{ activeRecord?.pmeqpSopNo }}｜{{ activeRecord?.equipmentType }}｜{{ activeRecord?.frequencyName }}｜{{ activeRecord?.sopNo }}</p><div class="summary-grid"><span>年度</span><strong>{{ activeRecord?.pmYearNo }}</strong><span>自動開工單</span><strong>{{ activeRecord?.autoCreateWo ? 'Yes' : 'No' }}</strong><span>提前天數</span><strong>{{ activeRecord?.beforehandDay ?? '—' }}</strong><span>設備鎖定</span><strong>{{ activeRecord?.lockEquipment ? `Yes — ${activeRecord.stateName}` : 'No' }}</strong></div><p class="form-note">WinForms 由 ObjectFileHandler 開啟獨立的 PM_EQPMaintenancePlanSet；本頁只展示本筆設定摘要，詳細計畫維護列為外部功能。</p></template>
      <template v-else><div class="batch-fields"><label>來源年度 <select v-model="batchSourceYear"><option v-for="year in years" :key="year.yearNo" :value="year.yearNo">{{ year.yearNo }}</option></select></label><label>設備類型 <select v-model="batchEquipmentType"><option value="">全部類型</option><option v-for="row in equipmentChoices" :key="row.equipmentType" :value="row.equipmentType">{{ row.equipmentType }} — {{ row.equipmentTypeName }}</option></select></label><label>新保養年度 <select v-model="batchTargetYear"><option v-for="year in years" :key="year.yearNo" :value="year.yearNo">{{ year.yearNo }}</option></select></label><label>Plan Launch Date <input v-model="batchLaunchDate" type="date"></label></div><div class="batch-actions"><button type="button" class="secondary" @click="toggleAllBatchRows">{{ allBatchRowsSelected ? '全部取消' : '全選' }}</button><span>{{ batchSelectedNos.length }} / {{ batchSourceRows.length }} 筆已選</span></div><div class="table-wrap compact-table"><table><thead><tr><th class="check-col"></th><th>PMEQPSOPNo</th><th>Equipment Type</th><th>Frequency</th><th>SOP</th></tr></thead><tbody><tr v-for="row in batchSourceRows" :key="row.pmeqpSopNo"><td class="check-col"><input type="checkbox" :checked="batchSelectedNos.includes(row.pmeqpSopNo)" @change="toggleBatchRow(row.pmeqpSopNo)"></td><td>{{ row.pmeqpSopNo }}</td><td>{{ row.equipmentType }}</td><td>{{ row.frequencyNo }}</td><td>{{ row.sopNo }}</td></tr><tr v-if="batchSourceRows.length === 0"><td colspan="5" class="empty">來源年度沒有資料。</td></tr></tbody></table></div><p class="form-note">依舊 Core 模擬複製設定；有來源設備計畫時，依頻率產生目標年度計畫。DEV 日期計算是預覽近似值，正式批次須由家用端以 transaction/API 重建。</p></template>
      <footer><button type="button" class="secondary" @click="auxiliaryMode = null">關閉</button><button v-if="auxiliaryMode === 'batch-copy'" type="button" @click="runBatchCopy">執行 DEV 批次複製</button></footer>
    </section></div>
  </section>
</template>

<style scoped>
.pm-eqp-type-sop { height:100%; min-height:0; display:flex; flex-direction:column; gap:10px; padding:12px; box-sizing:border-box; background:#f7f5ee; color:#26251f; font:12px/1.42 "Segoe UI","Microsoft JhengHei",sans-serif; }
.page-header,.header-actions,.filter-row,.toolbar,.page-footer,.dialog header,.dialog footer,.batch-actions { display:flex; align-items:center; }
.page-header { justify-content:space-between; border-bottom:1px solid #d6d2c6; padding-bottom:8px; }.page-header p,.dialog header p { margin:0; color:#777366; font-size:10px; letter-spacing:.06em; }h1 { margin:2px 0 0; font-size:20px; font-weight:600; }.header-actions { gap:9px; }.dev-tag { padding:3px 8px; border-radius:3px; background:#dce9de; color:#28533a; font-size:10px; }
button { min-height:27px; padding:4px 9px; border:1px solid #454237; border-radius:3px; background:#454237; color:#fff; font:inherit; cursor:pointer; white-space:nowrap; }button:disabled { opacity:.42; cursor:default; }button.secondary { border-color:#bab6a8; background:#fffef9; color:#333126; }
.filter-row { gap:10px; }.filter-row label,.dialog label { display:grid; gap:3px; color:#514f45; }.filter-row input,.dialog input:not([type=radio]):not([type=checkbox]),.dialog select,.dialog textarea { min-height:28px; padding:4px 7px; box-sizing:border-box; border:1px solid #bbb7a9; border-radius:3px; background:#fffef9; color:#26251f; font:inherit; }.filter-row input { width:235px; }.row-count { margin-top:14px; color:#777366; }
.toolbar { gap:5px; flex-wrap:wrap; }.toolbar-gap { flex:1; }.table-wrap { flex:1; min-height:130px; overflow:auto; border:1px solid #d0ccbf; background:#fffef9; }table { width:100%; border-collapse:collapse; white-space:nowrap; }th,td { padding:5px 7px; border-bottom:1px solid #e7e4db; text-align:left; }th { position:sticky; top:0; z-index:1; background:#e9e6dc; color:#555246; font-weight:600; }tbody tr { cursor:pointer; }tbody tr:hover { background:#f5f2e8; }tbody tr.selected { background:#e8edf0; }.check-col { width:26px; text-align:center; }.number-cell { text-align:right; }.description-cell { min-width:190px; max-width:340px; white-space:normal; }.empty { padding:20px; text-align:center; color:#777366; }.toast { margin:0; padding:7px 9px; border-radius:3px; background:#e4eee4; color:#28533a; }.toast.error { background:#f4e5df; color:#8a3527; }.page-footer { justify-content:space-between; gap:8px; color:#8a877c; font-size:10px; }
.modal-backdrop { position:fixed; inset:0; z-index:120; display:grid; place-items:center; padding:16px; background:#28271f80; }.dialog { width:min(680px,100%); max-height:calc(100vh - 32px); overflow:auto; display:grid; gap:10px; padding:14px; border:1px solid #b8b4a7; border-radius:5px; background:#f7f5ee; box-shadow:0 18px 60px #0004; }.form-dialog { width:min(760px,100%); }.dialog header { justify-content:space-between; border-bottom:1px solid #d6d2c6; padding-bottom:8px; }.dialog h2 { margin:2px 0 0; font-size:17px; }.dialog footer { justify-content:flex-end; gap:6px; padding-top:3px; }.close-x { min-width:30px; font-size:18px; }.form-grid { display:grid; grid-template-columns:1fr 1fr; gap:9px; }.form-grid select { width:100%; }.option-grid { display:grid; grid-template-columns:1.15fr 1fr .65fr; gap:9px; }fieldset { min-width:0; display:flex; align-items:center; gap:10px; flex-wrap:wrap; padding:8px; border:1px solid #c9c5b8; }.dialog fieldset label { display:flex; align-items:center; gap:4px; }.dialog fieldset input[type=radio] { accent-color:#555144; }.dialog .inline-field { margin-left:auto; }.dialog .inline-field input { width:76px; }.dialog fieldset select { width:100%; }.nested { flex-basis:100%; padding:5px; }.nested legend { font-size:10px; }.hint,.form-note { margin:0; color:#777366; font-size:11px; }.form-note { padding:6px 8px; background:#eeece4; }.dialog textarea { resize:vertical; }.auxiliary-dialog { width:min(740px,100%); }.context-line { margin:0; padding:6px 8px; background:#eeece4; font-weight:600; }.compact-table { flex:initial; max-height:280px; }.batch-fields { display:grid; grid-template-columns:1fr 1fr 1fr; gap:9px; }.batch-actions { justify-content:space-between; }.summary-grid { display:grid; grid-template-columns:150px 1fr; gap:6px 10px; padding:10px; background:#fffef9; }.summary-grid span { color:#777366; }
@media(max-width:850px) { .pm-eqp-type-sop { padding:8px; }.option-grid,.form-grid,.batch-fields { grid-template-columns:1fr; }.page-footer { align-items:flex-start; flex-direction:column; }.toolbar-gap { flex-basis:100%; } }
</style>
