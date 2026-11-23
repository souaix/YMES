<script setup lang="ts">
import { computed, ref } from 'vue'

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

type OpenStatus = 0 | 1
type Operand = '=' | '>' | '>=' | '<' | '<='
type EditorMode = 'add' | 'edit' | 'copy'
type LookupTarget = 'query-type' | 'query-equipment' | 'query-cap-item' | 'editor-equipment' | 'editor-cap-item' | null
const operandOptions: Operand[] = ['=', '>', '>=', '<', '<=']
interface EquipmentOption { equipmentNo: string; equipmentType: string; description: string }
interface MappingRow {
  equipmentNo: string
  capItem: string
  condOperand: Operand
  capValue: string
  openStatus: OpenStatus
  description: string
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
}
interface EditorRow extends MappingRow { localId: number }
interface LookupOption { value: string; detail: string }

const equipmentOptions: EquipmentOption[] = [
  { equipmentNo: 'DEV-ETCH-01', equipmentType: 'DEV-ETCH', description: '蝕刻主機 A' },
  { equipmentNo: 'DEV-ETCH-02', equipmentType: 'DEV-ETCH', description: '蝕刻主機 B' },
  { equipmentNo: 'DEV-CVD-01', equipmentType: 'DEV-CVD', description: '薄膜主機 A' },
  { equipmentNo: 'DEV-CVD-02', equipmentType: 'DEV-CVD', description: '薄膜主機 B' },
  { equipmentNo: 'DEV-CLEAN-01', equipmentType: 'DEV-CLEAN', description: '清洗主機 A' },
  { equipmentNo: 'DEV-ETCH-03', equipmentType: 'DEV-ETCH', description: '新設備，尚未設定能力' },
]
const capItemOptions = [
  { value: 'CHAMBER_TEMP', detail: '腔體溫度能力項目' },
  { value: 'RF_POWER', detail: '射頻輸出功率設定' },
  { value: 'VACUUM_LEVEL', detail: '製程真空能力範圍' },
  { value: 'WAFER_CAPACITY', detail: '腔體可處理晶圓數' },
]
const timestamp = (hoursAgo: number) => new Date(Date.now() - hoursAgo * 3_600_000).toISOString()
const mappingRows = ref<MappingRow[]>([
  { equipmentNo: 'DEV-ETCH-01', capItem: 'CHAMBER_TEMP', condOperand: '<=', capValue: '85', openStatus: 1, description: '上腔溫度上限', creator: 'DEV_USER', createDate: timestamp(44), reviser: '', reviseDate: '' },
  { equipmentNo: 'DEV-ETCH-01', capItem: 'RF_POWER', condOperand: '>=', capValue: '1200', openStatus: 1, description: '穩定製程功率下限', creator: 'DEV_USER', createDate: timestamp(43), reviser: 'DEV_LEAD', reviseDate: timestamp(12) },
  { equipmentNo: 'DEV-ETCH-01', capItem: 'VACUUM_LEVEL', condOperand: '<', capValue: '4.5', openStatus: 0, description: '真空度測試條件', creator: 'DEV_USER', createDate: timestamp(41), reviser: '', reviseDate: '' },
  { equipmentNo: 'DEV-ETCH-02', capItem: 'CHAMBER_TEMP', condOperand: '<=', capValue: '90', openStatus: 1, description: '設備能力條件', creator: 'DEV_USER', createDate: timestamp(38), reviser: '', reviseDate: '' },
  { equipmentNo: 'DEV-ETCH-02', capItem: 'WAFER_CAPACITY', condOperand: '=', capValue: '25', openStatus: 1, description: '每批晶圓容量', creator: 'DEV_USER', createDate: timestamp(36), reviser: '', reviseDate: '' },
  { equipmentNo: 'DEV-CVD-01', capItem: 'RF_POWER', condOperand: '<=', capValue: '800', openStatus: 0, description: '射頻輸出功率上限', creator: 'DEV_USER', createDate: timestamp(30), reviser: '', reviseDate: '' },
])

const query = ref({ equipmentType: '', equipmentNo: '', capItem: '', useType: false, useEquipment: false, useCapItem: false })
const appliedQuery = ref({ equipmentType: '', equipmentNo: '', capItem: '' })
const queryApplied = ref(false)
const activeEquipmentNo = ref('')
const selectedEquipmentNos = ref<string[]>([])
const selectedMappingKeys = ref<string[]>([])
const actionMessage = ref('')
const validationError = ref('')
const showEquipmentDelete = ref(false)
const editorMode = ref<EditorMode | null>(null)
const editorSourceEquipment = ref('')
const editorEquipmentNo = ref('')
const editorRows = ref<EditorRow[]>([])
const nextLocalId = ref(1)
const newCondition = ref({ capItem: '', condOperand: '' as '' | Operand, capValue: '', openStatus: 1 as OpenStatus, description: '' })
const editorSelectedIds = ref<number[]>([])
const editorError = ref('')
const editorNotice = ref('')
const lookupTarget = ref<LookupTarget>(null)
const overwriteTargetEquipment = ref('')
const showOverwriteChoice = ref(false)

const typeOptions = computed(() => [...new Set(equipmentOptions.map((item) => item.equipmentType))].sort())
const lookupOptions = computed<LookupOption[]>(() => {
  if (lookupTarget.value === 'query-type') return typeOptions.value.map((value) => ({ value, detail: '設備類別' }))
  if (lookupTarget.value === 'query-equipment' || lookupTarget.value === 'editor-equipment') return equipmentOptions.map((item) => ({ value: item.equipmentNo, detail: `${item.equipmentType} · ${item.description}` }))
  return capItemOptions.map((item) => ({ value: item.value, detail: item.detail }))
})
const equipmentResults = computed(() => {
  if (!queryApplied.value) return []
  const mappedEquipment = new Set(mappingRows.value.map((row) => row.equipmentNo))
  return equipmentOptions
    .filter((item) => mappedEquipment.has(item.equipmentNo))
    .filter((item) => !appliedQuery.value.equipmentType || item.equipmentType === appliedQuery.value.equipmentType)
    .filter((item) => !appliedQuery.value.equipmentNo || item.equipmentNo === appliedQuery.value.equipmentNo)
    .filter((item) => !appliedQuery.value.capItem || mappingRows.value.some((row) => row.equipmentNo === item.equipmentNo && row.capItem === appliedQuery.value.capItem))
    .sort((a, b) => a.equipmentNo.localeCompare(b.equipmentNo))
})
const currentMappingRows = computed(() => mappingRows.value
  .filter((row) => row.equipmentNo === activeEquipmentNo.value)
  .sort((a, b) => a.capItem.localeCompare(b.capItem) || a.condOperand.localeCompare(b.condOperand) || a.capValue.localeCompare(b.capValue)))
const activeEquipment = computed(() => equipmentOptions.find((item) => item.equipmentNo === activeEquipmentNo.value) ?? null)
const selectedMappingRows = computed(() => currentMappingRows.value.filter((row) => selectedMappingKeys.value.includes(mappingKey(row))))
const editorTitle = computed(() => editorMode.value === 'add' ? '新增設備能力設定' : editorMode.value === 'edit' ? '編輯設備能力設定' : '複製設備能力設定')
const showEditor = computed(() => editorMode.value !== null)

function mappingKey(row: Pick<MappingRow, 'equipmentNo' | 'capItem' | 'condOperand' | 'capValue'>) {
  return JSON.stringify([row.equipmentNo, row.capItem, row.condOperand, row.capValue])
}

function runQuery() {
  validationError.value = ''
  if (query.value.useType && !query.value.equipmentType.trim()) { validationError.value = '已勾選 Equipment Type，請輸入或選取類別。'; return }
  if (query.value.useEquipment && !query.value.equipmentNo.trim()) { validationError.value = '已勾選 Equipment No，請輸入或選取設備。'; return }
  if (query.value.useCapItem && !query.value.capItem.trim()) { validationError.value = '已勾選 CapItem，請輸入或選取能力項目。'; return }
  appliedQuery.value = {
    equipmentType: query.value.useType ? query.value.equipmentType.trim().toUpperCase() : '',
    equipmentNo: query.value.useEquipment ? query.value.equipmentNo.trim().toUpperCase() : '',
    capItem: query.value.useCapItem ? query.value.capItem.trim().toUpperCase() : '',
  }
  queryApplied.value = true
  selectedEquipmentNos.value = []
  selectedMappingKeys.value = []
  activeEquipmentNo.value = equipmentResults.value[0]?.equipmentNo ?? ''
  actionMessage.value = ''
}

function setActiveEquipment(equipmentNo: string) {
  activeEquipmentNo.value = equipmentNo
  selectedMappingKeys.value = []
}

function toggleEquipmentSelection(equipmentNo: string) {
  activeEquipmentNo.value = equipmentNo
  selectedEquipmentNos.value = selectedEquipmentNos.value.includes(equipmentNo)
    ? selectedEquipmentNos.value.filter((item) => item !== equipmentNo)
    : [...selectedEquipmentNos.value, equipmentNo]
}

function toggleMappingSelection(row: MappingRow) {
  const key = mappingKey(row)
  selectedMappingKeys.value = selectedMappingKeys.value.includes(key)
    ? selectedMappingKeys.value.filter((item) => item !== key)
    : [...selectedMappingKeys.value, key]
}

function selectVisibleMappings(event: Event) {
  const checked = (event.target as HTMLInputElement).checked
  selectedMappingKeys.value = checked ? currentMappingRows.value.map(mappingKey) : []
}

function selectVisibleEquipment(event: Event) {
  const checked = (event.target as HTMLInputElement).checked
  selectedEquipmentNos.value = checked ? equipmentResults.value.map((item) => item.equipmentNo) : []
}

function dateText(value: string) {
  return value ? new Date(value).toLocaleString('zh-TW', { hour12: false }) : '—'
}

function toggleSelectedStatus(nextStatus: OpenStatus) {
  if (!activeEquipment.value) return
  const targets = selectedMappingRows.value.length ? selectedMappingRows.value : currentMappingRows.value.filter((row) => mappingKey(row) === selectedMappingKeys.value[0])
  if (!targets.length) return
  let updated = 0
  for (const target of targets) {
    if (target.openStatus === nextStatus) continue
    target.openStatus = nextStatus
    target.reviser = 'DEV_USER'
    target.reviseDate = new Date().toISOString()
    updated += 1
  }
  actionMessage.value = updated ? `已模擬${nextStatus ? '開啟' : '關閉'} ${updated} 筆條件。` : `所選條件已是${nextStatus ? '開啟' : '關閉'}狀態。`
  selectedMappingKeys.value = []
}

function deleteEquipmentMappings() {
  const targets = selectedEquipmentNos.value.length ? [...selectedEquipmentNos.value] : activeEquipmentNo.value ? [activeEquipmentNo.value] : []
  if (!targets.length) return
  mappingRows.value = mappingRows.value.filter((row) => !targets.includes(row.equipmentNo))
  selectedEquipmentNos.value = []
  selectedMappingKeys.value = []
  showEquipmentDelete.value = false
  runQuery()
  actionMessage.value = `已模擬刪除 ${targets.length} 台設備的全部能力條件。`
}

function cloneForEditor(rows: MappingRow[]) {
  return rows.map((row) => ({ ...row, localId: nextLocalId.value++ }))
}

function openEditor(mode: EditorMode) {
  editorMode.value = mode
  editorError.value = ''
  editorNotice.value = ''
  editorSelectedIds.value = []
  newCondition.value = { capItem: '', condOperand: '', capValue: '', openStatus: 1, description: '' }
  if (mode === 'add') {
    editorSourceEquipment.value = ''
    editorEquipmentNo.value = ''
    editorRows.value = []
  } else if (mode === 'edit') {
    editorSourceEquipment.value = activeEquipmentNo.value
    editorEquipmentNo.value = activeEquipmentNo.value
    editorRows.value = cloneForEditor(currentMappingRows.value)
  } else {
    editorSourceEquipment.value = activeEquipmentNo.value
    editorEquipmentNo.value = ''
    editorRows.value = cloneForEditor(currentMappingRows.value)
  }
}

function chooseEditorEquipment(equipmentNo: string) {
  editorEquipmentNo.value = equipmentNo
  if (editorMode.value === 'add') {
    editorRows.value = cloneForEditor(mappingRows.value.filter((row) => row.equipmentNo === equipmentNo))
    editorNotice.value = editorRows.value.length ? '已載入目的設備現有條件，可接續新增或刪除後整組套用。' : '此設備目前沒有能力條件。'
    return
  }
  if (editorMode.value === 'copy' && equipmentNo !== editorSourceEquipment.value && mappingRows.value.some((row) => row.equipmentNo === equipmentNo)) {
    overwriteTargetEquipment.value = equipmentNo
    showOverwriteChoice.value = true
  }
}

function chooseLookup(option: LookupOption) {
  const target = lookupTarget.value
  lookupTarget.value = null
  if (target === 'query-type') { query.value.equipmentType = option.value; query.value.useType = true }
  else if (target === 'query-equipment') { query.value.equipmentNo = option.value; query.value.useEquipment = true }
  else if (target === 'query-cap-item') { query.value.capItem = option.value; query.value.useCapItem = true }
  else if (target === 'editor-cap-item') newCondition.value.capItem = option.value
  else if (target === 'editor-equipment') chooseEditorEquipment(option.value)
}

function resolveOverwrite(loadDestination: boolean) {
  if (loadDestination) editorRows.value = cloneForEditor(mappingRows.value.filter((row) => row.equipmentNo === overwriteTargetEquipment.value))
  editorNotice.value = loadDestination ? '已載入目的設備原條件，儲存時會整組替換。' : '保留來源設備條件，儲存時會整組替換目的設備設定。'
  showOverwriteChoice.value = false
}

function addCondition() {
  editorError.value = ''
  const capItem = newCondition.value.capItem.trim().toUpperCase()
  const operand = newCondition.value.condOperand
  let capValue = newCondition.value.capValue.trim().toUpperCase()
  if (!capItem) { editorError.value = 'CapItem 不可空白。'; return }
  if (!operand) { editorError.value = '請選擇運算元。'; return }
  if (!capValue) { editorError.value = 'CapValue 不可空白。'; return }
  if (capItem.length > 50 || capValue.length > 20 || newCondition.value.description.trim().length > 255) { editorError.value = '欄位超過 WinForms 長度限制。'; return }
  if (operand !== '=') {
    const numericValue = Number(capValue)
    if (!Number.isFinite(numericValue)) { editorError.value = '運算元不是等號時，CapValue 必須是數字。'; return }
    capValue = String(numericValue)
  }
  const duplicate = editorRows.value.some((row) => row.capItem.toUpperCase() === capItem && row.condOperand === operand && row.capValue.toUpperCase() === capValue)
  if (duplicate) { editorError.value = '相同 CapItem／運算元／CapValue 已存在。'; return }
  editorRows.value.push({
    equipmentNo: editorEquipmentNo.value,
    capItem,
    condOperand: operand,
    capValue,
    openStatus: newCondition.value.openStatus,
    description: newCondition.value.description.trim(),
    creator: 'DEV_USER',
    createDate: new Date().toISOString(),
    reviser: '',
    reviseDate: '',
    localId: nextLocalId.value++,
  })
  newCondition.value = { capItem: '', condOperand: '', capValue: '', openStatus: 1, description: '' }
  editorNotice.value = '已將條件加入目前編輯清單，尚未套用至 DEV 假資料。'
}

function deleteEditorCondition() {
  if (!editorSelectedIds.value.length) return
  const id = editorSelectedIds.value[editorSelectedIds.value.length - 1]
  editorRows.value = editorRows.value.filter((row) => row.localId !== id)
  editorSelectedIds.value = editorSelectedIds.value.filter((item) => item !== id)
}

function saveEditor(closeAfter: boolean) {
  editorError.value = ''
  const equipmentNo = editorEquipmentNo.value.trim().toUpperCase()
  if (!equipmentNo) { editorError.value = '請選擇 EquipmentNo。'; return }
  if (editorRows.value.length === 0) { editorError.value = '至少需要一筆設備能力條件；要清空整台設備請使用主畫面刪除。'; return }
  if (!equipmentOptions.some((item) => item.equipmentNo === equipmentNo)) { editorError.value = 'DEV 查找清單沒有此設備。'; return }
  const now = new Date().toISOString()
  const persisted = editorRows.value.map((row) => ({ ...row, equipmentNo, creator: 'DEV_USER', createDate: now, reviser: '', reviseDate: '' }))
  mappingRows.value = [...mappingRows.value.filter((row) => row.equipmentNo !== equipmentNo), ...persisted]
  selectedEquipmentNos.value = []
  selectedMappingKeys.value = []
  if (queryApplied.value) {
    runQuery()
    if (equipmentResults.value.some((item) => item.equipmentNo === equipmentNo)) activeEquipmentNo.value = equipmentNo
  } else activeEquipmentNo.value = equipmentNo
  actionMessage.value = `已模擬整組替換 ${equipmentNo} 的 ${persisted.length} 筆能力條件。DEV 資料只保存在目前畫面。`
  editorNotice.value = actionMessage.value
  if (closeAfter) editorMode.value = null
}
</script>

<template>
  <section class="cap-mapping" aria-label="設備能力設定 DEV 預覽" :data-development-preview="developmentPreview ? 'true' : 'false'">
    <header class="page-heading"><div><p class="eyebrow">EMS / EMS CONFIG · ES108</p><h1>設備能力設定</h1><p>依設備維護能力項目條件與開啟狀態。</p></div><span class="dev-badge">DEV MOCK</span></header>

    <section class="filter-panel" aria-label="設備能力查詢條件">
      <label class="filter-item"><input v-model="query.useType" type="checkbox" /><span>Equipment Type</span><input v-model="query.equipmentType" :disabled="!query.useType" maxlength="50" @keyup.enter="runQuery" /><button type="button" :disabled="!query.useType" aria-label="選取設備類別" @click="lookupTarget = 'query-type'">…</button></label>
      <label class="filter-item"><input v-model="query.useEquipment" type="checkbox" /><span>Equipment No</span><input v-model="query.equipmentNo" :disabled="!query.useEquipment" maxlength="50" @keyup.enter="runQuery" /><button type="button" :disabled="!query.useEquipment" aria-label="選取設備" @click="lookupTarget = 'query-equipment'">…</button></label>
      <label class="filter-item"><input v-model="query.useCapItem" type="checkbox" /><span>CapItem</span><input v-model="query.capItem" :disabled="!query.useCapItem" maxlength="50" @keyup.enter="runQuery" /><button type="button" :disabled="!query.useCapItem" aria-label="選取設備能力項目" @click="lookupTarget = 'query-cap-item'">…</button></label>
      <div class="filter-actions"><button type="button" class="primary-button" @click="runQuery">查詢</button><button type="button" @click="query = { equipmentType: '', equipmentNo: '', capItem: '', useType: false, useEquipment: false, useCapItem: false }; runQuery()">清除</button></div>
    </section>
    <p v-if="validationError" class="error-banner" role="alert">{{ validationError }}</p>
    <div v-if="actionMessage" class="action-message" role="status"><span>{{ actionMessage }}</span><button type="button" aria-label="關閉訊息" @click="actionMessage = ''">×</button></div>

    <section class="results-panel">
      <div class="panel-heading"><div><h2>已設定設備</h2><span>{{ equipmentResults.length }} 台</span></div><div class="button-row"><button type="button" class="primary-button" @click="openEditor('add')">新增</button><button type="button" :disabled="!activeEquipmentNo" @click="openEditor('edit')">編輯</button><button type="button" :disabled="!activeEquipmentNo" @click="openEditor('copy')">複製至設備</button><button type="button" class="danger-button" :disabled="!selectedEquipmentNos.length && !activeEquipmentNo" @click="showEquipmentDelete = true">刪除設備設定</button><button type="button" @click="emit('close')">關閉</button></div></div>
      <div class="split-grid">
        <div class="grid-frame equipment-grid">
          <table><thead><tr><th class="check-cell"><input type="checkbox" aria-label="全選已設定設備" :checked="equipmentResults.length > 0 && selectedEquipmentNos.length === equipmentResults.length" @change="selectVisibleEquipment" /></th><th>EquipmentNo</th></tr></thead>
            <tbody><tr v-for="item in equipmentResults" :key="item.equipmentNo" :class="{ active: activeEquipmentNo === item.equipmentNo }" @click="setActiveEquipment(item.equipmentNo)"><td class="check-cell"><input type="checkbox" :checked="selectedEquipmentNos.includes(item.equipmentNo)" :aria-label="`選取 ${item.equipmentNo}`" @click.stop @change="toggleEquipmentSelection(item.equipmentNo)" /></td><td class="code-cell">{{ item.equipmentNo }}</td></tr><tr v-if="queryApplied && !equipmentResults.length"><td colspan="2" class="empty">查無已設定能力的設備。</td></tr><tr v-if="!queryApplied"><td colspan="2" class="empty">設定查詢條件後按「查詢」。</td></tr></tbody>
          </table>
        </div>
        <div class="grid-frame mapping-grid">
          <div class="mapping-toolbar"><strong>{{ activeEquipmentNo || '請先選取設備' }} · 能力條件</strong><div class="button-row"><button type="button" :disabled="!currentMappingRows.length" @click="toggleSelectedStatus(1)">開啟</button><button type="button" :disabled="!currentMappingRows.length" @click="toggleSelectedStatus(0)">關閉</button></div></div>
          <table><thead><tr><th class="check-cell"><input type="checkbox" aria-label="全選能力條件" :checked="currentMappingRows.length > 0 && selectedMappingKeys.length === currentMappingRows.length" @change="selectVisibleMappings" /></th><th>CapItem</th><th>CondOperand</th><th>CapValue</th><th>OpenStatus</th><th>Description</th><th>Creator</th><th>CreateDate</th></tr></thead>
            <tbody><tr v-for="row in currentMappingRows" :key="mappingKey(row)" :class="{ active: selectedMappingKeys.includes(mappingKey(row)) }" @click="toggleMappingSelection(row)"><td class="check-cell"><input type="checkbox" :checked="selectedMappingKeys.includes(mappingKey(row))" :aria-label="`選取 ${row.capItem} 條件`" @click.stop @change="toggleMappingSelection(row)" /></td><td class="code-cell">{{ row.capItem }}</td><td>{{ row.condOperand }}</td><td>{{ row.capValue }}</td><td><span :class="['state-chip', row.openStatus ? 'on' : 'off']">{{ row.openStatus ? '開 (On)' : '關 (Off)' }}</span></td><td>{{ row.description || '—' }}</td><td>{{ row.creator }}</td><td>{{ dateText(row.createDate) }}</td></tr><tr v-if="activeEquipmentNo && !currentMappingRows.length"><td colspan="8" class="empty">此設備沒有能力條件。</td></tr><tr v-if="!activeEquipmentNo"><td colspan="8" class="empty">選取左側設備以載入完整能力條件。</td></tr></tbody>
          </table>
        </div>
      </div>
      <footer class="panel-footer">主清單僅列出已存在對應的設備；CapItem 查詢篩選左側設備，不縮減明細條件。</footer>
    </section>

    <div v-if="showEditor" class="backdrop" @click.self="editorMode = null">
      <section class="editor-dialog" role="dialog" aria-modal="true" :aria-label="editorTitle">
        <header><div><p class="eyebrow">整組替換 · {{ editorMode === 'copy' ? `來源 ${editorSourceEquipment}` : '設備能力條件' }}</p><h2>{{ editorTitle }}</h2></div><button type="button" aria-label="關閉" @click="editorMode = null">×</button></header>
        <div class="equipment-target"><label>Equipment No <input v-model="editorEquipmentNo" readonly maxlength="50" placeholder="請選取設備" /></label><button type="button" :disabled="editorMode === 'edit'" @click="lookupTarget = 'editor-equipment'">選取設備…</button></div>
        <fieldset class="condition-form"><legend>新增能力條件</legend>
          <label>CapItem <span>*</span><input v-model="newCondition.capItem" readonly maxlength="50" placeholder="請選取" /><button type="button" @click="lookupTarget = 'editor-cap-item'">…</button></label>
          <label>運算元 <span>*</span><select v-model="newCondition.condOperand"><option value="">請選擇</option><option v-for="operand in operandOptions" :key="operand" :value="operand">{{ operand }}</option></select></label>
          <label>CapValue <span>*</span><input v-model="newCondition.capValue" maxlength="20" @blur="newCondition.capValue = newCondition.capValue.trim().toUpperCase()" /></label>
          <label>狀態 <select v-model.number="newCondition.openStatus"><option :value="1">開 (On)</option><option :value="0">關 (Off)</option></select></label>
          <label class="description-field">Description <textarea v-model="newCondition.description" maxlength="255" rows="2" /></label>
          <button type="button" class="add-condition" @click="addCondition">加入條件</button>
        </fieldset>
        <div v-if="editorError" class="error-banner" role="alert">{{ editorError }}</div>
        <div v-if="editorNotice" class="notice-banner" role="status">{{ editorNotice }}</div>
        <div class="editor-table-frame"><table><thead><tr><th>選取</th><th>CapItem</th><th>運算元</th><th>CapValue</th><th>OpenStatus</th><th>Description</th></tr></thead><tbody>
          <tr v-for="row in editorRows" :key="row.localId" :class="{ active: editorSelectedIds.includes(row.localId) }" @click="editorSelectedIds = editorSelectedIds.includes(row.localId) ? editorSelectedIds.filter((id) => id !== row.localId) : [...editorSelectedIds, row.localId]"><td><input type="checkbox" :checked="editorSelectedIds.includes(row.localId)" @click.stop @change="editorSelectedIds = editorSelectedIds.includes(row.localId) ? editorSelectedIds.filter((id) => id !== row.localId) : [...editorSelectedIds, row.localId]" /></td><td class="code-cell">{{ row.capItem }}</td><td>{{ row.condOperand }}</td><td>{{ row.capValue }}</td><td>{{ row.openStatus ? '開 (On)' : '關 (Off)' }}</td><td>{{ row.description || '—' }}</td></tr>
          <tr v-if="!editorRows.length"><td colspan="6" class="empty">目前沒有條件；整組套用要求至少一筆。</td></tr>
        </tbody></table></div>
        <div class="editor-controls"><button type="button" class="danger-button" :disabled="!editorSelectedIds.length" @click="deleteEditorCondition">刪除選取條件</button><span>{{ editorRows.length }} 筆條件</span></div>
        <footer><span>Apply 儲存後留在視窗；Confirm 儲存並關閉。</span><button type="button" @click="editorMode = null">取消</button><button type="button" @click="saveEditor(false)">套用</button><button type="button" class="primary-button" @click="saveEditor(true)">確定</button></footer>
      </section>
    </div>

    <div v-if="showEquipmentDelete" class="backdrop" @click.self="showEquipmentDelete = false"><section class="confirm-dialog" role="dialog" aria-modal="true" aria-label="刪除設備能力設定"><h2>刪除整台設備的能力設定？</h2><p>舊 Service 依 EquipmentNo 刪除該設備全部能力條件。多選逐台執行，發生錯誤時之前成功的設備不會回復。</p><div class="target-list">{{ selectedEquipmentNos.length ? selectedEquipmentNos.join('、') : activeEquipmentNo }}</div><footer><button type="button" @click="showEquipmentDelete = false">取消</button><button type="button" class="danger-primary" @click="deleteEquipmentMappings">確認刪除</button></footer></section></div>

    <div v-if="lookupTarget" class="backdrop nested" @click.self="lookupTarget = null"><section class="lookup-dialog" role="dialog" aria-modal="true" aria-label="選取資料"><header><h2>{{ lookupTarget.includes('type') ? '選取設備類別' : lookupTarget.includes('equipment') ? '選取設備' : '選取能力項目' }}</h2><button type="button" @click="lookupTarget = null">關閉</button></header><button v-for="item in lookupOptions" :key="item.value" class="lookup-option" type="button" @click="chooseLookup(item)"><strong>{{ item.value }}</strong><span>{{ item.detail }}</span></button><p v-if="!lookupOptions.length" class="empty">沒有假查找資料。</p></section></div>

    <div v-if="showOverwriteChoice" class="backdrop nested"><section class="confirm-dialog" role="dialog" aria-modal="true" aria-label="目的設備已存在能力設定"><h2>目的設備已有能力條件</h2><p>{{ overwriteTargetEquipment }} 已有對應。可載入目的設備現況覆蓋目前編輯清單，或保留複製來源條件；儲存時會整組替換。</p><footer><button type="button" @click="resolveOverwrite(false)">保留複製來源</button><button type="button" class="primary-button" @click="resolveOverwrite(true)">載入目的設備現況</button></footer></section></div>
  </section>
</template>

<style scoped>
.cap-mapping { min-height: 100%; box-sizing: border-box; padding: 12px; color: #302f29; background: #f5f3eb; font: inherit; }.page-heading,.panel-heading,.button-row,.filter-item,.filter-actions,.mapping-toolbar,.equipment-target,.editor-controls,.editor-dialog header,.editor-dialog footer,.confirm-dialog footer,.lookup-dialog header { display: flex; align-items: center; }.page-heading { justify-content: space-between; margin-bottom: 10px; }.page-heading h1 { margin: 2px 0 4px; font-size: 20px; }.page-heading p { margin: 0; color: #777466; font-size: 12px; }.eyebrow { color: #79745d !important; font-size: 10px !important; letter-spacing: .08em; }.dev-badge { padding: 4px 8px; border: 1px solid #b7aa65; color: #605525; background: #f7f0cc; font-size: 10px; font-weight: 700; }
.filter-panel { display: grid; grid-template-columns: repeat(3, minmax(220px, 1fr)) auto; gap: 9px; align-items: end; padding: 10px; border: 1px solid #d0cdc2; background: #fbfaf6; }.filter-item { gap: 6px; flex-wrap: wrap; font-size: 11px; }.filter-item span { flex: 1 0 100%; color: #5b584d; }.filter-item input:not([type="checkbox"]) { flex: 1; min-width: 60px; width: 60px; box-sizing: border-box; min-height: 30px; padding: 5px 7px; border: 1px solid #bdb9ad; background: white; font: inherit; }.filter-item button { min-width: 30px; }.filter-actions,.button-row { gap: 5px; flex-wrap: wrap; }.cap-mapping button { min-height: 28px; padding: 4px 9px; border: 1px solid #aaa69a; border-radius: 2px; color: #333127; background: #f6f5f0; font: inherit; font-size: 11px; cursor: pointer; }.cap-mapping button:hover:not(:disabled) { background: #ebe9df; }.cap-mapping button:disabled { cursor: not-allowed; opacity: .45; }.cap-mapping .primary-button { border-color: #746525; color: white; background: #746525; }.cap-mapping .danger-button { color: #963f34; }.cap-mapping .danger-primary { border-color: #8a392e; color: white; background: #994637; }.cap-mapping input:focus-visible,.cap-mapping select:focus-visible,.cap-mapping textarea:focus-visible,.cap-mapping button:focus-visible { outline: 2px solid #a48b27; outline-offset: 1px; }.error-banner,.notice-banner,.action-message { margin: 8px 0; padding: 8px 10px; border: 1px solid #c5bc94; color: #5d512e; background: #f7f0d5; font-size: 11px; }.error-banner { border-color: #d9aca1; color: #903c30; background: #fbefec; }.action-message { display: flex; justify-content: space-between; gap: 8px; }.action-message button { border: 0; background: transparent; font-size: 16px; }
.results-panel { display: flex; flex-direction: column; min-height: 340px; margin-top: 10px; border: 1px solid #d0cdc2; background: #fbfaf6; }.panel-heading { justify-content: space-between; flex-wrap: wrap; gap: 8px; padding: 9px 10px; border-bottom: 1px solid #dfdcd2; }.panel-heading h2 { display: inline; margin: 0 8px 0 0; font-size: 14px; }.panel-heading span { color: #777466; font-size: 11px; }.split-grid { display: grid; grid-template-columns: minmax(180px, 24%) minmax(520px, 1fr); gap: 8px; padding: 8px; min-height: 250px; }.grid-frame,.editor-table-frame { overflow: auto; border: 1px solid #e0ddd3; }.grid-frame table,.editor-table-frame table { width: 100%; border-collapse: collapse; font-size: 11px; }.grid-frame th,.grid-frame td,.editor-table-frame th,.editor-table-frame td { padding: 6px 7px; border-bottom: 1px solid #e2dfd5; text-align: left; white-space: nowrap; }.grid-frame th,.editor-table-frame th { position: sticky; top: 0; z-index: 1; color: #555246; background: #eeece3; font-weight: 650; }.grid-frame tbody tr,.editor-table-frame tbody tr { cursor: pointer; }.grid-frame tbody tr:hover,.editor-table-frame tbody tr:hover { background: #f5f3e9; }.grid-frame tbody tr.active,.editor-table-frame tbody tr.active { background: #ecebdc; }.check-cell { width: 28px; text-align: center !important; }.code-cell { font-family: Consolas, monospace; font-weight: 650; }.empty { padding: 24px 8px !important; color: #777466; text-align: center !important; white-space: normal !important; }.mapping-toolbar { justify-content: space-between; gap: 8px; padding: 7px; border-bottom: 1px solid #e0ddd3; font-size: 11px; }.state-chip { display: inline-block; padding: 2px 6px; border-radius: 9px; }.state-chip.on { color: #3f5a3a; background: #e5eddf; }.state-chip.off { color: #8a5144; background: #f2e5df; }.panel-footer { padding: 7px 10px; border-top: 1px solid #dfdcd2; color: #777466; font-size: 10px; }
.backdrop { position: fixed; inset: 0; z-index: 1200; display: grid; place-items: center; padding: 15px; background: #17170fb0; }.nested { z-index: 1220; }.editor-dialog,.confirm-dialog,.lookup-dialog { width: min(920px, 100%); max-height: 94vh; overflow: auto; border: 1px solid #aaa596; border-radius: 3px; background: #fbfaf6; box-shadow: 0 16px 48px #0004; }.editor-dialog header,.lookup-dialog header { justify-content: space-between; padding: 10px 13px; border-bottom: 1px solid #d4d0c4; }.editor-dialog header h2,.lookup-dialog h2 { margin: 0; font-size: 15px; }.editor-dialog header .eyebrow { margin: 0; }.equipment-target { gap: 8px; padding: 9px 13px; }.equipment-target label { display: grid; gap: 3px; width: min(340px, 65%); color: #595649; font-size: 11px; }.equipment-target input,.condition-form input,.condition-form select,.condition-form textarea { min-height: 29px; box-sizing: border-box; padding: 4px 6px; border: 1px solid #b7b3a8; background: white; color: #302f29; font: inherit; font-size: 11px; }.condition-form { display: grid; grid-template-columns: 1.4fr .8fr 1fr .8fr 1.4fr auto; gap: 7px; align-items: end; margin: 0 13px 9px; padding: 9px; border: 1px solid #d9d5c9; }.condition-form legend { padding: 0 5px; color: #5d594c; font-size: 11px; }.condition-form label { display: grid; gap: 3px; color: #5c594e; font-size: 10px; }.condition-form label span { color: #a23e30; }.condition-form .description-field { min-width: 120px; }.condition-form .add-condition { align-self: end; }.editor-table-frame { max-height: 260px; margin: 0 13px; }.editor-controls { justify-content: space-between; padding: 7px 13px; color: #777466; font-size: 10px; }.editor-dialog footer,.confirm-dialog footer { justify-content: flex-end; flex-wrap: wrap; gap: 6px; padding: 9px 13px; border-top: 1px solid #d4d0c4; }.editor-dialog footer span { flex: 1; min-width: 220px; color: #777466; font-size: 10px; }.confirm-dialog { width: min(470px, 100%); padding: 15px; box-sizing: border-box; }.confirm-dialog h2 { margin: 0 0 8px; font-size: 15px; }.confirm-dialog p { color: #686558; font-size: 11px; }.target-list { padding: 8px; border: 1px solid #dedbd1; background: #f4f2ea; font-family: Consolas, monospace; font-size: 11px; overflow-wrap: anywhere; }.lookup-dialog { width: min(460px, 100%); padding-bottom: 7px; }.lookup-dialog header { margin-bottom: 4px; }.lookup-option { display: flex; justify-content: space-between; gap: 12px; width: calc(100% - 14px); margin: 3px 7px; padding: 8px 9px !important; border: 0 !important; text-align: left; }.lookup-option span { color: #777466; }
@media (max-width: 920px) { .filter-panel { grid-template-columns: 1fr 1fr; }.split-grid { grid-template-columns: 1fr; }.equipment-grid { max-height: 190px; }.condition-form { grid-template-columns: 1fr 1fr; }.condition-form .description-field { grid-column: 1 / -1; } }
@media (max-width: 560px) { .cap-mapping { padding: 7px; }.filter-panel { grid-template-columns: 1fr; }.panel-heading { align-items: flex-start; flex-direction: column; }.button-row { flex-wrap: wrap; }.equipment-target { align-items: flex-end; }.editor-dialog footer span { flex-basis: 100%; } }
</style>
