<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'

interface FixedRow {
  equipmentNo: string
  equipmentType: string
  typeCode: 0 | 1
  quantity: number
  toleranceQty: number
  qtyType: number
  autoCreateWo: boolean
  lockEquipment: boolean
  autoEmail: boolean
  equipmentState: number | null
  stateName: string
  pmSopNo: string
  pmSopName: string
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
  dataStamp: number
}
interface DevState { version: 1; records: FixedRow[] }
interface CatalogEquipment { equipmentNo: string; equipmentType: string; equipmentName: string }
interface SopOption { pmSopNo: string; pmSopName: string }
type DialogMode = 'add-type' | 'edit-type' | 'copy-type' | 'edit-equipment' | ''
interface Draft {
  equipmentType: string
  equipmentNo: string
  quantity: string
  toleranceEnabled: boolean
  toleranceQty: string
  qtyType: string
  autoCreateWo: boolean
  pmSopNo: string
  lockEquipment: boolean
  equipmentState: string
  autoEmail: boolean
}

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.pm.fixed-times.v1'
const currentUser = 'DEV01'
const qtyTypeOptions = [{ value: 0, label: 'Times' }, { value: 1, label: 'Hours' }, { value: 2, label: 'Minutes' }]
const stateOptions = [{ value: 3, name: '保養中' }, { value: 5, name: '待維修' }, { value: 8, name: '停機檢修' }]
const fallbackSops: SopOption[] = [
  { pmSopNo: 'SOP-CAL-02', pmSopName: '感測器校正程序' },
  { pmSopNo: 'SOP-LUBE-03', pmSopName: '主軸潤滑保養程序' },
  { pmSopNo: 'SOP-OVEN-01', pmSopName: '烘箱定期清潔程序' },
]
const catalog: CatalogEquipment[] = [
  { equipmentNo: 'CNC-02', equipmentType: 'CNC-200', equipmentName: 'CNC 加工中心 02' },
  { equipmentNo: 'CNC-03', equipmentType: 'CNC-200', equipmentName: 'CNC 加工中心 03' },
  { equipmentNo: 'CNC-04', equipmentType: 'CNC-200', equipmentName: 'CNC 加工中心 04' },
  { equipmentNo: 'PUMP-07', equipmentType: 'PUMP-120', equipmentName: '製程冷卻泵 07' },
  { equipmentNo: 'PUMP-08', equipmentType: 'PUMP-120', equipmentName: '製程冷卻泵 08' },
  { equipmentNo: 'OVEN-05', equipmentType: 'OVEN-50', equipmentName: '真空烘箱 05' },
  { equipmentNo: 'OVEN-06', equipmentType: 'OVEN-50', equipmentName: '真空烘箱 06' },
]
const equipmentTypeLabels: Record<string, string> = { 'CNC-200': 'CNC 加工中心', 'PUMP-120': '製程冷卻泵', 'OVEN-50': '真空烘箱' }

function nowText() { return new Date().toLocaleString('sv-SE') }
function qtyTypeLabel(value: number) { return qtyTypeOptions.find((row) => row.value === value)?.label ?? `QtyType ${value}` }
function rowFromTemplate(equipmentNo: string, equipmentType: string, template: Partial<FixedRow>, typeCode: 0 | 1, creator = currentUser): FixedRow {
  const now = nowText()
  return {
    equipmentNo, equipmentType, typeCode,
    quantity: Number(template.quantity ?? 0), toleranceQty: Number(template.toleranceQty ?? 0), qtyType: Number(template.qtyType ?? 0),
    autoCreateWo: Boolean(template.autoCreateWo), lockEquipment: Boolean(template.lockEquipment), autoEmail: Boolean(template.autoEmail),
    equipmentState: template.lockEquipment ? (template.equipmentState ?? 3) : null, stateName: template.lockEquipment ? (template.stateName || '保養中') : '',
    pmSopNo: template.autoCreateWo ? String(template.pmSopNo ?? '') : '', pmSopName: template.autoCreateWo ? String(template.pmSopName ?? '') : '',
    creator, createDate: template.createDate || now, reviser: String(template.reviser ?? ''), reviseDate: String(template.reviseDate ?? ''), dataStamp: Number(template.dataStamp ?? 0),
  }
}
function seedState(): DevState {
  const cnc = rowFromTemplate('CNC-200', 'CNC-200', { quantity: 1200, toleranceQty: 1500, qtyType: 1, autoCreateWo: true, pmSopNo: 'SOP-CAL-02', pmSopName: '感測器校正程序', lockEquipment: true, equipmentState: 3, stateName: '保養中', autoEmail: true, creator: 'PMUSER', createDate: '2026-01-08 09:30', dataStamp: 2 }, 1, 'PMUSER')
  const pump = rowFromTemplate('PUMP-120', 'PUMP-120', { quantity: 500, toleranceQty: 0, qtyType: 0, autoCreateWo: false, lockEquipment: false, autoEmail: false, creator: 'PMUSER', createDate: '2026-02-10 11:15' }, 1, 'PMUSER')
  return {
    version: 1,
    records: [
      cnc,
      rowFromTemplate('CNC-02', 'CNC-200', cnc, 0, 'PMUSER'),
      rowFromTemplate('CNC-03', 'CNC-200', cnc, 0, 'PMUSER'),
      pump,
      rowFromTemplate('PUMP-07', 'PUMP-120', pump, 0, 'PMUSER'),
    ],
  }
}
function loadState(): DevState {
  try {
    const parsed = JSON.parse(window.localStorage.getItem(storageKey) ?? 'null') as DevState | null
    if (parsed?.version === 1 && Array.isArray(parsed.records)) return parsed
  } catch { /* Keep the DEV preview available when browser storage is disabled. */ }
  return seedState()
}
function readSops(): SopOption[] {
  try {
    const parsed = JSON.parse(window.localStorage.getItem('ymes.dev.pm.sop-basis.v1') ?? 'null') as { records?: Array<Record<string, unknown>> } | null
    const rows = parsed?.records?.map((row) => ({
      pmSopNo: String(row.pmSopNo ?? row.pmsopNo ?? row.PMSOPNo ?? ''),
      pmSopName: String(row.pmSopName ?? row.pmsopName ?? row.PMSOPName ?? ''),
    })).filter((row) => row.pmSopNo)
    if (rows?.length) return rows
  } catch { /* Use the local sample SOP list until the PM109 preview has data. */ }
  return fallbackSops
}

const state = ref<DevState>(loadState())
const sopOptions = ref(readSops())
const searchText = ref('')
const selectedTypeNo = ref('')
const selectedEquipmentNo = ref('')
const dialogMode = ref<DialogMode>('')
const setDialogOpen = ref(false)
const message = ref('')
const messageKind = ref<'success' | 'error'>('success')
const availableEquipment = ref<string[]>([])
const assignedEquipment = ref<string[]>([])
const availableSelection = ref<string[]>([])
const assignedSelection = ref<string[]>([])
const draft = reactive<Draft>({ equipmentType: '', equipmentNo: '', quantity: '', toleranceEnabled: false, toleranceQty: '', qtyType: '0', autoCreateWo: false, pmSopNo: '', lockEquipment: false, equipmentState: '', autoEmail: false })
const currentType = computed(() => state.value.records.find((row) => row.typeCode === 1 && row.equipmentNo === selectedTypeNo.value) ?? null)
const typeRows = computed(() => {
  const term = searchText.value.trim().toLowerCase()
  return state.value.records.filter((row) => row.typeCode === 1 && (!term || `${row.equipmentNo} ${row.equipmentType}`.toLowerCase().includes(term) || state.value.records.some((child) => child.typeCode === 0 && child.equipmentType === row.equipmentType && child.equipmentNo.toLowerCase().includes(term)))).sort((a, b) => a.equipmentNo.localeCompare(b.equipmentNo))
})
const equipmentRows = computed(() => state.value.records.filter((row) => row.typeCode === 0 && row.equipmentType === currentType.value?.equipmentType && `${row.equipmentNo} ${row.equipmentType}`.toLowerCase().includes(searchText.value.trim().toLowerCase())).sort((a, b) => a.equipmentNo.localeCompare(b.equipmentNo)))
const equipmentTypeOptions = computed(() => Array.from(new Set([...Object.keys(equipmentTypeLabels), ...state.value.records.filter((row) => row.typeCode === 1).map((row) => row.equipmentType)])).sort())
const dialogTitle = computed(() => ({ 'add-type': '新增設備類型定次定時保養', 'edit-type': '修改設備類型定次定時保養', 'copy-type': '複製設備類型定次定時保養', 'edit-equipment': '修改設備定次定時保養' }[dialogMode.value] ?? ''))
const isEquipmentEdit = computed(() => dialogMode.value === 'edit-equipment')
const isTypeEdit = computed(() => dialogMode.value === 'edit-type')
const hasDialog = computed(() => dialogMode.value !== '')
watch(state, (value) => { try { window.localStorage.setItem(storageKey, JSON.stringify(value)) } catch { /* Keep DEV edits in memory if local storage is disabled. */ } }, { deep: true })
watch(typeRows, (rows) => {
  if (!rows.some((row) => row.equipmentNo === selectedTypeNo.value)) selectedTypeNo.value = rows[0]?.equipmentNo ?? ''
}, { immediate: true })
watch(equipmentRows, (rows) => {
  if (!rows.some((row) => row.equipmentNo === selectedEquipmentNo.value)) selectedEquipmentNo.value = rows[0]?.equipmentNo ?? ''
}, { immediate: true })

function toast(text: string, kind: 'success' | 'error' = 'success') { message.value = text; messageKind.value = kind }
function stateName(value: number | null) { return stateOptions.find((row) => row.value === value)?.name ?? (value == null ? '—' : `狀態 ${value}`) }
function equipmentName(no: string) { return catalog.find((row) => row.equipmentNo === no)?.equipmentName ?? no }
function selectType(row: FixedRow) { selectedTypeNo.value = row.equipmentNo; selectedEquipmentNo.value = '' }
function prepareDraft(row?: FixedRow, mode: DialogMode = 'add-type') {
  dialogMode.value = mode
  Object.assign(draft, {
    equipmentType: row?.equipmentType ?? '', equipmentNo: row?.equipmentNo ?? '', quantity: row ? String(row.quantity) : '',
    toleranceEnabled: Boolean(row?.toleranceQty), toleranceQty: row?.toleranceQty ? String(row.toleranceQty) : '', qtyType: row ? String(row.qtyType) : '0',
    autoCreateWo: row?.autoCreateWo ?? false, pmSopNo: row?.pmSopNo ?? '', lockEquipment: row?.lockEquipment ?? false,
    equipmentState: row?.equipmentState == null ? '' : String(row.equipmentState), autoEmail: row?.autoEmail ?? false,
  })
}
function openAddType() { prepareDraft(undefined, 'add-type') }
function openEditType() { if (currentType.value) prepareDraft(currentType.value, 'edit-type') }
function openCopyType() { if (currentType.value) prepareDraft(currentType.value, 'copy-type') }
function openEditEquipment() {
  const row = equipmentRows.value.find((item) => item.equipmentNo === selectedEquipmentNo.value)
  if (row) prepareDraft(row, 'edit-equipment')
}
function closeDialog() { dialogMode.value = '' }
function validateDraft(): string {
  if (isEquipmentEdit.value && !draft.equipmentNo.trim()) return '設備編號不可空白。'
  if (!isEquipmentEdit.value && !draft.equipmentType.trim()) return '設備類型不可空白。'
  if (!/^\d+$/.test(draft.quantity.trim()) || Number(draft.quantity) <= 0) return '定次／定時量必須是大於 0 的整數。'
  if (draft.toleranceQty.trim() && (!/^\d+$/.test(draft.toleranceQty.trim()) || Number(draft.toleranceQty) <= Number(draft.quantity))) return '累計寬限量若有輸入，必須是大於定次／定時量的正整數。'
  if (!qtyTypeOptions.some((item) => String(item.value) === draft.qtyType)) return '請選擇定次定時類型。'
  if (draft.autoCreateWo && !draft.pmSopNo) return '啟用自動開立保養工單時，必須選擇 SOP。'
  if (draft.lockEquipment && !draft.equipmentState) return '啟用鎖定設備時，必須選擇設備狀態。'
  const targetNo = isEquipmentEdit.value ? draft.equipmentNo.trim().toUpperCase() : (isTypeEdit.value ? draft.equipmentNo : draft.equipmentType.trim().toUpperCase())
  if (!isEquipmentEdit.value && dialogMode.value !== 'edit-type' && state.value.records.some((row) => row.typeCode === 1 && row.equipmentNo === targetNo)) return `設備類型 ${targetNo} 已有定次定時保養設定。`
  return ''
}
function saveDraft() {
  const issue = validateDraft()
  if (issue) return toast(issue, 'error')
  const isEquipment = isEquipmentEdit.value
  const equipmentType = isEquipment ? (state.value.records.find((row) => row.typeCode === 0 && row.equipmentNo === draft.equipmentNo)?.equipmentType ?? currentType.value?.equipmentType ?? '') : draft.equipmentType.trim().toUpperCase()
  const equipmentNo = isEquipment ? draft.equipmentNo.trim().toUpperCase() : (isTypeEdit.value ? draft.equipmentNo : equipmentType)
  const existing = state.value.records.find((row) => row.equipmentNo === equipmentNo && row.typeCode === (isEquipment ? 0 : 1))
  const chosenSop = sopOptions.value.find((row) => row.pmSopNo === draft.pmSopNo)
  const next = rowFromTemplate(equipmentNo, equipmentType, {
    quantity: Number(draft.quantity), toleranceQty: draft.toleranceEnabled && draft.toleranceQty.trim() ? Number(draft.toleranceQty) : 0, qtyType: Number(draft.qtyType),
    autoCreateWo: draft.autoCreateWo, pmSopNo: draft.autoCreateWo ? draft.pmSopNo : '', pmSopName: chosenSop?.pmSopName ?? (draft.pmSopNo === existing?.pmSopNo ? existing.pmSopName : ''),
    lockEquipment: draft.lockEquipment, equipmentState: draft.lockEquipment ? Number(draft.equipmentState) : null,
    stateName: stateName(draft.lockEquipment ? Number(draft.equipmentState) : null), autoEmail: draft.autoEmail,
    creator: existing?.creator, createDate: existing?.createDate,
  }, isEquipment ? 0 : 1, existing?.creator ?? currentUser)
  if (existing) {
    next.reviser = currentUser
    next.reviseDate = nowText()
    next.dataStamp = existing.dataStamp + 1
    state.value.records = state.value.records.map((row) => row === existing ? next : row)
  } else {
    state.value.records.push(next)
  }
  selectedTypeNo.value = isEquipment ? equipmentType : equipmentNo
  selectedEquipmentNo.value = isEquipment ? equipmentNo : ''
  toast(`${isEquipment ? '設備' : '設備類型'} ${equipmentNo} 已儲存 DEV 假資料。`)
  closeDialog()
}
function deleteType() {
  const row = currentType.value
  if (!row || !window.confirm(`刪除設備類型 ${row.equipmentType} 的定次定時設定？舊 Core 會一併刪除該類型下所有設備列。`)) return
  state.value.records = state.value.records.filter((item) => item.equipmentType !== row.equipmentType)
  selectedTypeNo.value = ''
  toast(`設備類型 ${row.equipmentType} 與其設備定次定時設定已刪除。`)
}
function deleteEquipment() {
  const row = equipmentRows.value.find((item) => item.equipmentNo === selectedEquipmentNo.value)
  if (!row || !window.confirm(`刪除設備 ${row.equipmentNo} 的定次定時設定？`)) return
  state.value.records = state.value.records.filter((item) => item !== row)
  toast(`設備 ${row.equipmentNo} 的定次定時設定已刪除。`)
}
function openSetDialog() {
  const row = currentType.value
  if (!row) return toast('請先選取設備類型設定。', 'error')
  const assigned = state.value.records.filter((item) => item.typeCode === 0 && item.equipmentType === row.equipmentType).map((item) => item.equipmentNo)
  const candidates = catalog.filter((item) => item.equipmentType === row.equipmentType).map((item) => item.equipmentNo)
  assignedEquipment.value = assigned.sort((a, b) => a.localeCompare(b))
  availableEquipment.value = candidates.filter((no) => !assigned.includes(no)).sort((a, b) => a.localeCompare(b))
  availableSelection.value = []
  assignedSelection.value = []
  setDialogOpen.value = true
}
function moveEquipment(direction: 'assign' | 'unassign') {
  if (direction === 'assign') {
    assignedEquipment.value = Array.from(new Set([...assignedEquipment.value, ...availableSelection.value])).sort((a, b) => a.localeCompare(b))
    availableEquipment.value = availableEquipment.value.filter((no) => !availableSelection.value.includes(no))
    availableSelection.value = []
  } else {
    availableEquipment.value = Array.from(new Set([...availableEquipment.value, ...assignedSelection.value])).sort((a, b) => a.localeCompare(b))
    assignedEquipment.value = assignedEquipment.value.filter((no) => !assignedSelection.value.includes(no))
    assignedSelection.value = []
  }
}
function saveSet() {
  const row = currentType.value
  if (!row) return
  const prompt = assignedEquipment.value.length ? `將 ${row.equipmentType} 的定次定時設定套用到 ${assignedEquipment.value.length} 台設備？` : `清除 ${row.equipmentType} 下所有設備定次定時設定？`
  if (!window.confirm(prompt)) return
  const replacedCount = state.value.records.filter((item) => item.typeCode === 0 && item.equipmentType === row.equipmentType).length
  const children = assignedEquipment.value.map((equipmentNo) => rowFromTemplate(equipmentNo, row.equipmentType, {
    ...row, creator: currentUser, createDate: nowText(), dataStamp: 0,
  }, 0, currentUser))
  state.value.records = state.value.records.filter((item) => item.typeCode !== 0 || item.equipmentType !== row.equipmentType).concat(children)
  selectedEquipmentNo.value = children.some((item) => item.equipmentNo === selectedEquipmentNo.value) ? selectedEquipmentNo.value : (children[0]?.equipmentNo ?? '')
  setDialogOpen.value = false
  toast(`已重設 ${row.equipmentType} 的設備適用清單（${children.length} 台）。${replacedCount ? '既有設備個別值已改以類型預設取代。' : ''}`)
}
</script>

<template>
  <section class="pm-fixed-times" aria-label="設備定次定時保養設定 DEV 預覽">
    <header class="page-header"><div><p>設備 PM 設定／EQP FIXED TIME SETUP</p><h1>設備定次定時保養設定</h1></div><div class="header-actions"><span class="dev-tag">DEV 假資料</span><button type="button" class="secondary" @click="emit('close')">Close (X)</button></div></header>
    <div class="toolbar"><label>設備類型／設備編號 <input v-model="searchText" type="search" placeholder="輸入 EquipmentNo 搜尋"></label><button type="button" @click="openAddType">新增類型</button><button type="button" class="secondary" :disabled="!currentType" @click="openEditType">修改類型</button><button type="button" class="secondary" :disabled="!currentType" @click="openCopyType">複製類型</button><button type="button" class="danger" :disabled="!currentType" @click="deleteType">刪除類型</button><button type="button" class="secondary" :disabled="!currentType" @click="openSetDialog">SET 設備清單</button></div>
    <div class="panels">
      <section class="data-panel type-panel"><header><div><h2>設備類型定次定時設定 <small>TYPECODE = 1</small></h2><span>{{ typeRows.length }} 筆類型主檔</span></div></header><div class="table-wrap"><table><thead><tr><th>Equipment Type</th><th>Quantity</th><th>累計寬限量</th><th>QtyType</th><th>自動開立工單</th><th>鎖定設備</th><th>設備狀態</th><th>建立者</th><th>建立時間</th><th>修改者</th><th>修改時間</th><th>SOP</th><th>自動 Email</th></tr></thead><tbody><tr v-for="row in typeRows" :key="row.equipmentNo" :class="{ active: row.equipmentNo === selectedTypeNo }" @click="selectType(row)"><td><strong>{{ row.equipmentType }}</strong><small>{{ equipmentTypeLabels[row.equipmentType] ?? '設備類型' }}</small></td><td>{{ row.quantity }}</td><td>{{ row.toleranceQty || '—' }}</td><td>{{ qtyTypeLabel(row.qtyType) }}</td><td>{{ row.autoCreateWo ? '是' : '否' }}</td><td>{{ row.lockEquipment ? '是' : '否' }}</td><td>{{ row.lockEquipment ? `${row.equipmentState}｜${row.stateName}` : '—' }}</td><td>{{ row.creator || '—' }}</td><td>{{ row.createDate || '—' }}</td><td>{{ row.reviser || '—' }}</td><td>{{ row.reviseDate || '—' }}</td><td>{{ row.pmSopNo || '—' }}<small>{{ row.pmSopName }}</small></td><td>{{ row.autoEmail ? '是' : '否' }}</td></tr><tr v-if="typeRows.length === 0"><td colspan="13" class="empty">沒有符合的設備類型定次定時設定。</td></tr></tbody></table></div></section>
      <section class="data-panel equipment-panel"><header><div><h2>設備定次定時設定明細 <small>TYPECODE = 0</small></h2><span>{{ currentType?.equipmentType ?? '請先選擇類型' }}｜{{ equipmentRows.length }} 台設備</span></div><div class="row-actions"><button type="button" class="secondary" :disabled="!selectedEquipmentNo" @click="openEditEquipment">修改設備列</button><button type="button" class="danger" :disabled="!selectedEquipmentNo" @click="deleteEquipment">刪除設備列</button></div></header><div class="table-wrap"><table><thead><tr><th>Equipment No</th><th>Quantity</th><th>累計寬限量</th><th>自動開立工單</th><th>鎖定設備</th><th>設備狀態</th><th>建立者</th><th>建立時間</th><th>修改者</th><th>修改時間</th><th>SOP</th><th>自動 Email</th></tr></thead><tbody><tr v-for="row in equipmentRows" :key="row.equipmentNo" :class="{ active: row.equipmentNo === selectedEquipmentNo }" @click="selectedEquipmentNo = row.equipmentNo"><td><strong>{{ row.equipmentNo }}</strong><small>{{ equipmentName(row.equipmentNo) }}</small></td><td>{{ row.quantity }}</td><td>{{ row.toleranceQty || '—' }}</td><td>{{ row.autoCreateWo ? '是' : '否' }}</td><td>{{ row.lockEquipment ? '是' : '否' }}</td><td>{{ row.lockEquipment ? `${row.equipmentState}｜${row.stateName}` : '—' }}</td><td>{{ row.creator || '—' }}</td><td>{{ row.createDate || '—' }}</td><td>{{ row.reviser || '—' }}</td><td>{{ row.reviseDate || '—' }}</td><td>{{ row.pmSopNo || '—' }}<small>{{ row.pmSopName }}</small></td><td>{{ row.autoEmail ? '是' : '否' }}</td></tr><tr v-if="equipmentRows.length === 0"><td colspan="12" class="empty">此設備類型目前沒有個別設備定次定時列。按「SET 設備清單」套用類型預設。</td></tr></tbody></table></div></section>
    </div>
    <p v-if="message" class="toast" :class="messageKind" role="status">{{ message }}</p>
    <footer class="page-footer"><span>DEV localStorage：{{ storageKey }}</span><span>Times／Hours／Minutes；SET 會以設備類型設定覆寫該類型設備列</span></footer>

    <div v-if="hasDialog" class="modal-backdrop" @click.self="closeDialog"><section class="dialog" role="dialog" aria-modal="true" aria-labelledby="fixed-times-title"><header><div><p>EQP FIXED TIME SETUP / {{ dialogMode.toUpperCase() }}</p><h2 id="fixed-times-title">{{ dialogTitle }}</h2></div><button type="button" class="secondary close-x" aria-label="關閉" @click="closeDialog">×</button></header><div class="form-grid">
      <label v-if="isEquipmentEdit">Equipment No<input v-model="draft.equipmentNo" readonly></label>
      <label v-if="!isEquipmentEdit">Equipment Type<select v-model="draft.equipmentType" :disabled="isTypeEdit"><option value="">請選擇設備類型</option><option v-for="code in equipmentTypeOptions" :key="code" :value="code">{{ code }}｜{{ equipmentTypeLabels[code] ?? 'DEV 類型' }}</option></select></label>
      <label>定次／定時量 Quantity<input v-model="draft.quantity" type="number" min="1" step="1" inputmode="numeric"></label>
      <label class="check-field"><input v-model="draft.toleranceEnabled" type="checkbox">啟用累計寬限量</label><label>累計寬限量<input v-model="draft.toleranceQty" type="number" min="1" step="1" :disabled="!draft.toleranceEnabled"></label>
      <label>QtyType<select v-model="draft.qtyType"><option v-for="option in qtyTypeOptions" :key="option.value" :value="String(option.value)">{{ option.value }}｜{{ option.label }}</option></select></label>
      <fieldset><legend>自動開立保養工單</legend><label class="check-field"><input v-model="draft.autoCreateWo" type="checkbox">達量時依 SOP 建立</label><label v-if="draft.autoCreateWo">PM SOP<select v-model="draft.pmSopNo"><option value="">請選擇 SOP</option><option v-for="sop in sopOptions" :key="sop.pmSopNo" :value="sop.pmSopNo">{{ sop.pmSopNo }}｜{{ sop.pmSopName }}</option></select></label></fieldset>
      <fieldset><legend>設備鎖定</legend><label class="check-field"><input v-model="draft.lockEquipment" type="checkbox">保養時鎖定設備</label><label v-if="draft.lockEquipment">Equipment State<select v-model="draft.equipmentState"><option value="">請選擇狀態</option><option v-for="item in stateOptions" :key="item.value" :value="String(item.value)">{{ item.value }}｜{{ item.name }}</option></select></label></fieldset>
      <label class="check-field"><input v-model="draft.autoEmail" type="checkbox">自動 Email 通知</label>
    </div><p class="form-note">TypeCode {{ isEquipmentEdit ? 0 : 1 }}｜修改採 DataStamp 條件更新。類型主檔變更不會自動同步既有設備列，請使用 SET 套用。</p><footer><span class="footer-gap"></span><button type="button" class="secondary" @click="closeDialog">取消</button><button type="button" @click="saveDraft">儲存</button></footer></section></div>

    <div v-if="setDialogOpen && currentType" class="modal-backdrop" @click.self="setDialogOpen = false"><section class="dialog set-dialog" role="dialog" aria-modal="true" aria-labelledby="set-equipment-title"><header><div><p>EQP FIXED TIME SETUP / SET</p><h2 id="set-equipment-title">{{ currentType.equipmentType }} 設備適用清單</h2></div><button type="button" class="secondary close-x" aria-label="關閉" @click="setDialogOpen = false">×</button></header><p class="form-note">雙向移動設備或多選後按箭頭；確認時會先移除這個類型的舊設備列，再逐台套用類型定次定時設定。</p><div class="equipment-transfer"><label>可選設備<select v-model="availableSelection" multiple size="11"><option v-for="no in availableEquipment" :key="no" :value="no">{{ no }}｜{{ equipmentName(no) }}</option></select></label><div class="transfer-buttons"><button type="button" @click="moveEquipment('assign')">加入 →</button><button type="button" class="secondary" @click="moveEquipment('unassign')">← 移除</button></div><label>已套用設備<select v-model="assignedSelection" multiple size="11"><option v-for="no in assignedEquipment" :key="no" :value="no">{{ no }}｜{{ equipmentName(no) }}</option></select></label></div><footer><span class="footer-gap"></span><button type="button" class="secondary" @click="setDialogOpen = false">取消</button><button type="button" @click="saveSet">確認套用</button></footer></section></div>
  </section>
</template>

<style scoped>
.pm-fixed-times { height:100%; min-height:0; display:flex; flex-direction:column; gap:9px; padding:12px; box-sizing:border-box; background:#f7f5ee; color:#26251f; font:12px/1.42 "Segoe UI","Microsoft JhengHei",sans-serif; }
.page-header,.header-actions,.toolbar,.data-panel>header,.row-actions,.page-footer,.dialog header,.dialog footer { display:flex; align-items:center; }.page-header { justify-content:space-between; border-bottom:1px solid #d6d2c6; padding-bottom:8px; }.page-header p,.dialog header p { margin:0; color:#777366; font-size:10px; letter-spacing:.06em; }h1 { margin:2px 0 0; font-size:20px; font-weight:600; }.header-actions { gap:9px; }.dev-tag { padding:3px 8px; border-radius:3px; background:#dce9de; color:#28533a; font-size:10px; }
button { min-height:28px; padding:4px 10px; border:1px solid #454237; border-radius:3px; background:#454237; color:#fff; font:inherit; cursor:pointer; }button:disabled { opacity:.42; cursor:default; }button.secondary { border-color:#bab6a8; background:#fffef9; color:#333126; }button.danger { border-color:#a34a38; background:#a34a38; }
.toolbar { gap:7px; flex-wrap:wrap; padding:8px; border:1px solid #d0ccbf; background:#eeece4; }.toolbar label { display:flex; align-items:center; gap:7px; margin-right:auto; color:#514f45; }.toolbar input { width:230px; min-height:28px; padding:4px 7px; border:1px solid #bbb7a9; border-radius:3px; background:#fffef9; font:inherit; }
.panels { min-height:0; flex:1; display:grid; grid-template-rows:minmax(0,1fr) minmax(0,1fr); gap:8px; }.data-panel { min-height:0; display:flex; flex-direction:column; gap:6px; padding:8px; border:1px solid #d0ccbf; background:#fffef9; }.data-panel>header { justify-content:space-between; gap:9px; }.data-panel h2 { margin:0; font-size:14px; }.data-panel h2 small { margin-left:6px; color:#8a877c; font-size:10px; font-weight:400; }.data-panel>header span { display:block; margin-top:2px; color:#777366; font-size:10px; }.row-actions { gap:5px; }.table-wrap { min-height:0; flex:1; overflow:auto; border:1px solid #e0ddd4; }table { width:100%; border-collapse:collapse; white-space:nowrap; font-size:11px; }th { position:sticky; top:0; z-index:1; background:#e9e6dc; color:#555246; font-weight:600; text-align:left; }th,td { padding:5px 7px; border-bottom:1px solid #e5e2d9; }tbody tr { cursor:pointer; }tbody tr:hover { background:#f7f4eb; }tbody tr.active { background:#e9eee9; }td strong,td small { display:block; }td small { color:#888478; font-size:10px; }.empty { padding:18px; color:#888478; text-align:center; }
.toast { margin:0; padding:7px 9px; border-radius:3px; background:#e4eee4; color:#28533a; }.toast.error { background:#f4e5df; color:#8a3527; }.page-footer { justify-content:space-between; gap:8px; color:#8a877c; font-size:10px; }
.modal-backdrop { position:fixed; inset:0; z-index:120; display:grid; place-items:center; padding:16px; background:#28271f80; }.dialog { width:min(620px,100%); max-height:calc(100vh - 32px); overflow:auto; display:grid; gap:10px; padding:14px; border:1px solid #b8b4a7; border-radius:5px; background:#f7f5ee; box-shadow:0 18px 60px #0004; }.dialog header { justify-content:space-between; border-bottom:1px solid #d6d2c6; padding-bottom:8px; }.dialog h2 { margin:2px 0 0; font-size:17px; }.close-x { min-width:30px; font-size:18px; }.form-grid { display:grid; grid-template-columns:1fr 1fr; gap:9px; }.form-grid>label,.form-grid fieldset label { display:grid; gap:3px; color:#514f45; }.form-grid input:not([type=checkbox]),.form-grid select { min-height:29px; padding:4px 7px; box-sizing:border-box; border:1px solid #bbb7a9; border-radius:3px; background:#fffef9; color:#26251f; font:inherit; }.form-grid input:disabled { opacity:.55; }.check-field { display:flex!important; align-items:center; gap:7px!important; }.form-grid fieldset { display:grid; gap:7px; margin:0; padding:8px; border:1px solid #d0ccbf; }.form-grid legend { padding:0 4px; color:#777366; }.form-note { margin:0; padding:6px 8px; background:#eeece4; color:#777366; font-size:11px; }.dialog footer { gap:6px; }.footer-gap { flex:1; }.set-dialog { width:min(760px,100%); }.equipment-transfer { display:grid; grid-template-columns:1fr 90px 1fr; align-items:center; gap:10px; }.equipment-transfer>label { display:grid; gap:4px; color:#514f45; }.equipment-transfer select { width:100%; min-height:240px; padding:4px; border:1px solid #bbb7a9; background:#fffef9; font:inherit; }.transfer-buttons { display:grid; gap:8px; }
@media(max-width:850px) { .pm-fixed-times { padding:7px; }.panels { grid-template-rows:minmax(220px,1fr) minmax(220px,1fr); }.page-footer { align-items:flex-start; flex-direction:column; }.equipment-transfer { grid-template-columns:1fr; }.transfer-buttons { grid-template-columns:1fr 1fr; }.form-grid { grid-template-columns:1fr; }.toolbar label { width:100%; }.toolbar input { flex:1; min-width:80px; } }
</style>
