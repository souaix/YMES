<script setup lang="ts">
import { computed, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'

const props = defineProps<{ developmentPreview: boolean; currentUserNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)

interface StateDefinition {
  code: number
  name: string
  color: string
}

interface EquipmentRow {
  equipmentNo: string
  equipmentType: string
  equipmentName: string
  areaNo: string
  state: number
  assignedLotNos: string[]
  activePmOrderCount: number
  checkedInOperators: number
}

interface ReasonType {
  no: string
  name: string
}

interface ReasonRow {
  no: string
  name: string
  typeNo: string
  level: number
  description: string
}

interface LotRow {
  lotNo: string
  areaNo: string
  moNo: string
  opNo: string
  productNo: string
  qty: number
  status: number
}

interface ChangeLogRow {
  serialNo: string
  equipmentNo: string
  previousState: string
  nextState: string
  reasonNos: string[]
  description: string
  userNo: string
  at: string
}

const equipmentRows = ref<EquipmentRow[]>(dev.value ? [
  { equipmentNo: 'DEV-EQP-101', equipmentType: 'DEV-TESTER', equipmentName: '測試機 101', areaNo: 'DEV-AREA-A', state: 0, assignedLotNos: [], activePmOrderCount: 0, checkedInOperators: 0 },
  { equipmentNo: 'DEV-EQP-102', equipmentType: 'DEV-TESTER', equipmentName: '測試機 102', areaNo: 'DEV-AREA-A', state: 1, assignedLotNos: ['DEV-LOT-210'], activePmOrderCount: 0, checkedInOperators: 2 },
  { equipmentNo: 'DEV-EQP-201', equipmentType: 'DEV-BONDER', equipmentName: '打線機 201', areaNo: 'DEV-AREA-B', state: 2, assignedLotNos: [], activePmOrderCount: 1, checkedInOperators: 0 },
  { equipmentNo: 'DEV-EQP-202', equipmentType: 'DEV-BONDER', equipmentName: '打線機 202', areaNo: 'DEV-AREA-B', state: 4, assignedLotNos: [], activePmOrderCount: 0, checkedInOperators: 0 },
] : [])

const stateRows = ref<StateDefinition[]>(dev.value ? [
  { code: 0, name: '閒置', color: '#d9e8df' },
  { code: 1, name: '生產', color: '#a5d6a7' },
  { code: 2, name: '故障', color: '#ef9a9a' },
  { code: 3, name: '保養', color: '#ffe082' },
  { code: 4, name: '待設定', color: '#b3e5fc' },
] : [])

const reasonTypes = ref<ReasonType[]>(dev.value ? [
  { no: 'DEV-01', name: '設備故障' },
  { no: 'DEV-02', name: '保養／維修' },
  { no: 'DEV-03', name: '生產安排' },
] : [])

const reasonRows = ref<ReasonRow[]>(dev.value ? [
  { no: 'DEV-R001', name: '設備異常停機', typeNo: 'DEV-01', level: 1, description: '設備狀態異常' },
  { no: 'DEV-R002', name: '待工程人員檢修', typeNo: 'DEV-01', level: 2, description: '通知工程處理' },
  { no: 'DEV-R010', name: '預防保養', typeNo: 'DEV-02', level: 1, description: '例行保養作業' },
  { no: 'DEV-R011', name: '保養完成待確認', typeNo: 'DEV-02', level: 2, description: '保養後檢查' },
  { no: 'DEV-R020', name: '生產準備完成', typeNo: 'DEV-03', level: 1, description: '設備已完成點檢' },
  { no: 'DEV-R021', name: '等待工單安排', typeNo: 'DEV-03', level: 2, description: '待安排生產批' },
] : [])

const lotRows = ref<LotRow[]>(dev.value ? [
  { lotNo: 'DEV-LOT-210', areaNo: 'DEV-AREA-A', moNo: 'DEV-MO-21', opNo: 'DEV-OP-FT', productNo: 'DEV-PRD-A', qty: 120, status: 1 },
  { lotNo: 'DEV-LOT-211', areaNo: 'DEV-AREA-A', moNo: 'DEV-MO-21', opNo: 'DEV-OP-FT', productNo: 'DEV-PRD-A', qty: 80, status: 1 },
  { lotNo: 'DEV-LOT-220', areaNo: 'DEV-AREA-A', moNo: 'DEV-MO-22', opNo: 'DEV-OP-QA', productNo: 'DEV-PRD-B', qty: 45, status: 1 },
  { lotNo: 'DEV-LOT-310', areaNo: 'DEV-AREA-B', moNo: 'DEV-MO-31', opNo: 'DEV-OP-WB', productNo: 'DEV-PRD-C', qty: 240, status: 1 },
  { lotNo: 'DEV-LOT-900', areaNo: 'DEV-AREA-A', moNo: 'DEV-MO-90', opNo: 'DEV-OP-FT', productNo: 'DEV-PRD-Z', qty: 60, status: 4 },
] : [])

// These state IDs and rules are illustrative DEV values. MES reads both from state-control metadata.
const productionState = 1
const pmWorkOrderReleasedState = 3
const changeProductionMode = '1'
const violatePairs = new Set(['2>1', '4>2'])

const equipmentNoInput = ref('')
const activeEquipmentNo = ref('')
const targetStateCode = ref<number | ''>('')
const selectedReasonType = ref('DEV-01')
const selectedReasonNos = ref<string[]>([])
const description = ref('')
const selectedLotNos = ref<string[]>([])
const pickerOpen = ref(false)
const confirmation = ref<string | null>(null)
const message = ref('')
const messageKind = ref<'error' | 'success' | 'info'>('info')
const changeLogs = ref<ChangeLogRow[]>([])
let nextSerial = 1

const activeEquipment = computed(() => equipmentRows.value.find(row => row.equipmentNo === activeEquipmentNo.value))
const currentState = computed(() => stateRows.value.find(row => row.code === activeEquipment.value?.state))
const targetState = computed(() => stateRows.value.find(row => row.code === targetStateCode.value))
const targetOptions = computed(() => stateRows.value.filter(row => activeEquipment.value
  && row.code !== activeEquipment.value.state
  && !violatePairs.has(`${activeEquipment.value.state}>${row.code}`)))
const visibleReasons = computed(() => reasonRows.value.filter(row => row.typeNo === selectedReasonType.value))
const isProductionTarget = computed(() => targetStateCode.value === productionState)
const availableLots = computed(() => lotRows.value.filter(row => row.status === 1 && row.areaNo === activeEquipment.value?.areaNo))
const activePmWorkOrder = computed(() => Boolean(activeEquipment.value && activeEquipment.value.activePmOrderCount > 0))
const pmWorkOrderBlocked = computed(() => activePmWorkOrder.value
  && targetStateCode.value !== ''
  && targetStateCode.value !== pmWorkOrderReleasedState)
const lotBindingRequired = computed(() => isProductionTarget.value && changeProductionMode === '1')
const selectedReasonNames = computed(() => reasonRows.value.filter(row => selectedReasonNos.value.includes(row.no)).map(row => row.name))
const displayUserNo = computed(() => props.currentUserNo?.trim() || 'DEVUSER')

function setMessage(text: string, kind: 'error' | 'success' | 'info' = 'info'): void {
  message.value = text
  messageKind.value = kind
}

function chooseEquipment(equipmentNo: string): void {
  if (!dev.value) return
  const row = equipmentRows.value.find(item => item.equipmentNo.toUpperCase() === equipmentNo.trim().toUpperCase())
  if (!row) {
    activeEquipmentNo.value = ''
    targetStateCode.value = ''
    selectedLotNos.value = []
    setMessage(`查無 DEV 設備：${equipmentNo || '（空白）'}`, 'error')
    return
  }
  equipmentNoInput.value = row.equipmentNo
  activeEquipmentNo.value = row.equipmentNo
  targetStateCode.value = ''
  selectedReasonNos.value = []
  selectedLotNos.value = row.assignedLotNos.filter(lotNo => lotRows.value.some(lot => lot.lotNo === lotNo && lot.areaNo === row.areaNo))
  description.value = ''
  setMessage(`已載入 ${row.equipmentNo}。目前狀態及設備主檔來自 DEV fixture。`)
}

function loadEquipment(): void {
  chooseEquipment(equipmentNoInput.value)
}

function openEquipmentPicker(): void {
  if (!dev.value) return
  pickerOpen.value = true
}

function selectTargetState(): void {
  if (!isProductionTarget.value) {
    selectedLotNos.value = []
    return
  }
  selectedLotNos.value = (activeEquipment.value?.assignedLotNos ?? []).filter(lotNo => availableLots.value.some(lot => lot.lotNo === lotNo))
}

function toggleReason(reasonNo: string): void {
  selectedReasonNos.value = selectedReasonNos.value.includes(reasonNo)
    ? selectedReasonNos.value.filter(value => value !== reasonNo)
    : [...selectedReasonNos.value, reasonNo]
}

function buildDescription(): void {
  if (!selectedReasonNos.value.length) {
    setMessage('請先選取至少一筆設備狀態變更原因。', 'error')
    return
  }
  description.value = reasonRows.value
    .filter(row => selectedReasonNos.value.includes(row.no))
    .map((row, index) => `(${index + 1})${row.no} : ${row.name}`)
    .join('\n')
  setMessage('已依選取原因產生描述，可再編輯內容。')
}

function toggleLot(lotNo: string): void {
  selectedLotNos.value = selectedLotNos.value.includes(lotNo)
    ? selectedLotNos.value.filter(value => value !== lotNo)
    : [...selectedLotNos.value, lotNo]
}

function requestSave(): void {
  if (!dev.value) return
  const equipment = activeEquipment.value
  if (!equipment) {
    setMessage('請先輸入並載入有效設備編號。', 'error')
    return
  }
  if (targetStateCode.value === '') {
    setMessage('請選擇變更後的設備狀態。', 'error')
    return
  }
  if (!targetOptions.value.some(row => row.code === targetStateCode.value)) {
    setMessage('此狀態與目前設備狀態相同或被互斥設定禁止。', 'error')
    return
  }
  if (!selectedReasonNos.value.length) {
    setMessage('請至少勾選一筆設備狀態變更原因。', 'error')
    return
  }
  if (!description.value.trim()) {
    setMessage('請按「Get Description」產生描述，或自行填寫描述。', 'error')
    return
  }
  if (pmWorkOrderBlocked.value) {
    setMessage('設備仍有未完成的保養工單，不可變更為目前選取狀態。', 'error')
    return
  }
  if (lotBindingRequired.value && selectedLotNos.value.length === 0) {
    setMessage('ChangePRODEQPStateMode=1：切換至生產狀態時至少需綁定一個生產批。', 'error')
    return
  }
  const lotSelectionIsCurrent = selectedLotNos.value.every(lotNo => availableLots.value.some(row => row.lotNo === lotNo))
  if (isProductionTarget.value && !lotSelectionIsCurrent) {
    setMessage('所選生產批已不在此設備作業區的可選清單中，請重新載入設備。', 'error')
    return
  }
  confirmation.value = `確認將 ${equipment.equipmentNo} 從「${currentState.value?.name ?? equipment.state}」變更為「${targetState.value?.name ?? targetStateCode.value}」？`
  message.value = ''
}

function resolveSave(accepted: boolean): void {
  confirmation.value = null
  if (!accepted || !dev.value) return
  const equipment = activeEquipment.value
  const nextState = targetState.value
  const previousState = currentState.value
  if (!equipment || !nextState || !previousState) return
  if (pmWorkOrderBlocked.value || (lotBindingRequired.value && selectedLotNos.value.length === 0)) {
    setMessage('設備或生產批條件已變更，請重新檢查。', 'error')
    return
  }
  const oldAssignedLots = [...equipment.assignedLotNos]
  const selectedReasonLabels = selectedReasonNames.value
  const auditDescription = `${description.value.trim()} EQP State Change by User:${displayUserNo.value}`
  const serialNo = `DEV-ES204-${String(nextSerial++).padStart(4, '0')}`
  equipment.state = nextState.code
  equipment.assignedLotNos = nextState.code === productionState ? [...selectedLotNos.value] : []
  changeLogs.value.unshift({
    serialNo,
    equipmentNo: equipment.equipmentNo,
    previousState: previousState.name,
    nextState: nextState.name,
    reasonNos: [...selectedReasonNos.value],
    description: auditDescription,
    userNo: displayUserNo.value,
    at: new Date().toLocaleString('zh-TW', { hour12: false }),
  })
  const transitionNote = previousState.code === productionState && nextState.code !== productionState
    ? `；模擬結束設備上的 ${oldAssignedLots.length} 筆生產批、設備用料紀錄及設備登入人員簽退`
    : nextState.code === productionState
      ? `；模擬建立 ${selectedLotNos.value.length} 筆 Lot／設備關聯，設備目前有 ${equipment.checkedInOperators} 位登入人員`
      : ''
  setMessage(`DEV 已模擬狀態歷程、新設備序號 ${serialNo}、${selectedReasonLabels.length} 筆原因保存${transitionNote}。`, 'success')
  targetStateCode.value = ''
  selectedReasonNos.value = []
  selectedLotNos.value = [...equipment.assignedLotNos]
  description.value = ''
}

function stateName(code: number): string {
  return stateRows.value.find(row => row.code === code)?.name ?? `狀態 ${code}`
}
</script>

<template>
  <section class="ems-state-change" aria-label="改變設備狀態">
    <template v-if="dev">
      <div class="equipment-panel">
        <label class="equipment-field" for="es204-equipment-no">Equipment No</label>
        <input
          id="es204-equipment-no"
          v-model="equipmentNoInput"
          list="es204-equipment-list"
          maxlength="50"
          autocomplete="off"
          @keydown.enter.prevent="loadEquipment"
        >
        <datalist id="es204-equipment-list">
          <option v-for="row in equipmentRows" :key="row.equipmentNo" :value="row.equipmentNo">{{ row.equipmentName }}</option>
        </datalist>
        <button type="button" @click="loadEquipment">載入</button>
        <button type="button" aria-label="查找設備" @click="openEquipmentPicker">...</button>
        <div class="current-state">
          <span class="field-caption">CurStatus</span>
          <span
            class="state-chip"
            :style="{ backgroundColor: currentState?.color ?? '#f5f0bf' }"
          >{{ activeEquipment ? currentState?.name ?? '未知狀態' : '尚未載入設備' }}</span>
        </div>
        <label class="target-label" for="es204-target-state">Last Status</label>
        <select id="es204-target-state" v-model.number="targetStateCode" :disabled="!activeEquipment" @change="selectTargetState">
          <option value="">請選擇</option>
          <option v-for="row in targetOptions" :key="row.code" :value="row.code">{{ row.name }}</option>
        </select>
        <span v-if="targetState" class="state-chip target-chip" :style="{ backgroundColor: targetState.color }">{{ targetState.name }}</span>
      </div>

      <div class="reason-panel">
        <section class="subtype-panel" aria-label="原因類別">
          <h2>Reason SubType</h2>
          <div class="subtype-list">
            <button
              v-for="row in reasonTypes"
              :key="row.no"
              type="button"
              :class="{ active: selectedReasonType === row.no }"
              @click="selectedReasonType = row.no"
            >
              <span>{{ row.no }}</span><strong>{{ row.name }}</strong>
            </button>
          </div>
        </section>
        <section class="reason-list-panel" aria-label="設備狀態變更原因">
          <h2>Reason Basis</h2>
          <div class="table-scroll">
            <table>
              <thead><tr><th>Check</th><th>ReasonNo</th><th>ReasonName</th><th>Level</th><th>Description</th></tr></thead>
              <tbody>
                <tr v-for="row in visibleReasons" :key="row.no">
                  <td><input :checked="selectedReasonNos.includes(row.no)" type="checkbox" :aria-label="'選取 ' + row.no" @change="toggleReason(row.no)"></td>
                  <td>{{ row.no }}</td><td>{{ row.name }}</td><td>{{ row.level }}</td><td>{{ row.description }}</td>
                </tr>
                <tr v-if="!visibleReasons.length"><td colspan="5">此原因類別目前沒有 DEV 項目。</td></tr>
              </tbody>
            </table>
          </div>
        </section>
        <div class="description-panel">
          <label for="es204-description">Description</label>
          <textarea id="es204-description" v-model="description" rows="4" maxlength="1000" />
          <button type="button" @click="buildDescription">Get Description</button>
        </div>
      </div>

      <section v-if="isProductionTarget" class="lot-panel" aria-label="生產批綁定">
        <div class="lot-heading">
          <h2>生產批</h2>
          <span>{{ activeEquipment?.areaNo }} · 同區域執行中批次</span>
          <strong v-if="lotBindingRequired">此 DEV 參數需至少選一批</strong>
        </div>
        <p v-if="activeEquipment?.assignedLotNos.length" class="lot-hint">已載入此設備目前 CheckIn 的生產批，請確認要保留的批次。</p>
        <div class="table-scroll">
          <table>
            <thead><tr><th>選取</th><th>LotNo</th><th>AreaNo</th><th>MONo</th><th>OPNo</th><th>ProductNo</th><th>CurQty</th></tr></thead>
            <tbody>
              <tr v-for="row in availableLots" :key="row.lotNo">
                <td><input :checked="selectedLotNos.includes(row.lotNo)" type="checkbox" :aria-label="'綁定 ' + row.lotNo" @change="toggleLot(row.lotNo)"></td>
                <td>{{ row.lotNo }}</td><td>{{ row.areaNo }}</td><td>{{ row.moNo }}</td><td>{{ row.opNo }}</td><td>{{ row.productNo }}</td><td>{{ row.qty }}</td>
              </tr>
              <tr v-if="!availableLots.length"><td colspan="7">此 DEV 作業區沒有執行中生產批。</td></tr>
            </tbody>
          </table>
        </div>
      </section>

      <div class="work-order-hint" :class="{ blocked: pmWorkOrderBlocked }">
        {{ activeEquipment
          ? activePmWorkOrder
            ? `DEV 有 ${activeEquipment.activePmOrderCount} 張未完成保養工單；只有切換至保養工單設定狀態 ${pmWorkOrderReleasedState} 才不阻擋。`
            : 'DEV 保養工單查詢：沒有未完成工單。'
          : '請載入設備後檢查設備狀態與保養工單。' }}
      </div>

      <div class="actions">
        <p class="fixture-note">DEV 假資料；狀態碼、互斥規則、保養設定及生產綁批模式均為示意值。</p>
        <button type="button" class="primary" :disabled="!activeEquipment" @click="requestSave">OK</button>
        <button type="button" @click="emit('close')">Close</button>
      </div>
      <p v-if="message" class="message" :class="messageKind" :role="messageKind === 'error' ? 'alert' : 'status'">{{ message }}</p>

      <section v-if="changeLogs.length" class="audit-panel" aria-label="DEV 狀態異動紀錄">
        <h2>DEV 本次異動</h2>
        <div class="table-scroll"><table>
          <thead><tr><th>EQPSerialNo</th><th>EquipmentNo</th><th>Before</th><th>After</th><th>ReasonNo</th><th>UserNo</th><th>Time</th></tr></thead>
          <tbody><tr v-for="row in changeLogs" :key="row.serialNo">
            <td>{{ row.serialNo }}</td><td>{{ row.equipmentNo }}</td><td>{{ row.previousState }}</td><td>{{ row.nextState }}</td>
            <td>{{ row.reasonNos.join(', ') }}</td><td>{{ row.userNo }}</td><td>{{ row.at }}</td>
          </tr></tbody>
        </table></div>
        <p class="audit-description">{{ changeLogs[0]?.description }}</p>
      </section>

      <div v-if="pickerOpen" class="picker-backdrop" @click.self="pickerOpen = false">
        <section class="picker-dialog" role="dialog" aria-modal="true" aria-labelledby="es204-picker-title">
          <header><h2 id="es204-picker-title">查找設備</h2><button type="button" aria-label="關閉設備查找" @click="pickerOpen = false">×</button></header>
          <div class="table-scroll"><table>
            <thead><tr><th>EquipmentNo</th><th>設備</th><th>類別</th><th>區域</th><th>現況</th><th></th></tr></thead>
            <tbody><tr v-for="row in equipmentRows" :key="row.equipmentNo">
              <td>{{ row.equipmentNo }}</td><td>{{ row.equipmentName }}</td><td>{{ row.equipmentType }}</td><td>{{ row.areaNo }}</td><td>{{ stateName(row.state) }}</td>
              <td><button type="button" @click="chooseEquipment(row.equipmentNo); pickerOpen = false">選取</button></td>
            </tr></tbody>
          </table></div>
        </section>
      </div>
      <ActionConfirmationDialog :message="confirmation" @resolve="resolveSave" />
    </template>
    <template v-else>
      <p role="status">正式環境尚未開放此功能。</p><button type="button" @click="emit('close')">Close (X)</button>
    </template>
  </section>
</template>

<style scoped>
.ems-state-change { display: grid; align-content: start; gap: 12px; padding: 14px; color: var(--ink, #26251f); }
.equipment-panel { display: grid; grid-template-columns: auto minmax(150px, 1fr) auto auto minmax(155px, .7fr) auto minmax(150px, 1fr) auto; align-items: center; gap: 8px; padding: 12px; border: 1px solid var(--line, #cfcbbd); background: #fffdf6; }
.equipment-field, .target-label, .field-caption { font-weight: 700; white-space: nowrap; }
input, select, textarea, button { font: inherit; }
.equipment-panel input[type='text'], .equipment-panel > input:not([type]), .equipment-panel select { min-width: 0; min-height: 30px; padding: 4px 7px; border: 1px solid var(--line, #cfcbbd); background: white; color: inherit; }
button { min-height: 30px; padding: 4px 10px; border: 1px solid #5a5547; border-radius: 3px; background: #f8f5e9; color: inherit; cursor: pointer; }
button:hover:not(:disabled) { background: #f3e69a; }
button:disabled { cursor: not-allowed; opacity: .55; }
.current-state { display: flex; align-items: center; gap: 8px; min-width: 0; }
.state-chip { display: inline-flex; align-items: center; min-height: 25px; padding: 2px 10px; border: 1px solid #777; border-radius: 2px; white-space: nowrap; }
.reason-panel { display: grid; grid-template-columns: minmax(180px, .7fr) minmax(420px, 2fr); gap: 10px; }
.subtype-panel, .reason-list-panel, .lot-panel, .audit-panel { min-width: 0; border: 1px solid var(--line, #cfcbbd); background: #fff; }
h2 { margin: 0; font-size: 15px; }
.subtype-panel h2, .reason-list-panel h2, .audit-panel h2 { padding: 8px 10px; border-bottom: 1px solid var(--line, #cfcbbd); background: #f5f0d6; }
.subtype-list { display: grid; gap: 1px; padding: 6px; }
.subtype-list button { display: flex; justify-content: flex-start; gap: 10px; min-height: 38px; text-align: left; }
.subtype-list button.active { border-color: #a38b00; background: #f4e7a4; }
.subtype-list strong { font-weight: 600; }
.table-scroll { max-width: 100%; overflow: auto; }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
th, td { padding: 6px 8px; border-bottom: 1px solid #e4e0d5; text-align: left; white-space: nowrap; }
th { position: sticky; top: 0; background: #efede4; z-index: 1; }
td input[type='checkbox'] { width: 16px; height: 16px; }
.description-panel { grid-column: 1 / -1; display: grid; grid-template-columns: 110px minmax(200px, 1fr) auto; align-items: start; gap: 8px; padding: 10px; border: 1px solid var(--line, #cfcbbd); background: #fffdf6; }
.description-panel label { padding-top: 6px; font-weight: 700; }
.description-panel textarea { width: 100%; min-height: 62px; resize: vertical; box-sizing: border-box; padding: 6px 8px; border: 1px solid var(--line, #cfcbbd); background: #fffde4; color: inherit; }
.lot-panel { display: grid; gap: 4px; }
.lot-heading { display: flex; align-items: center; gap: 12px; padding: 8px 10px; background: #f5f0d6; }
.lot-heading span, .lot-hint { color: var(--muted, #777366); }
.lot-heading strong { margin-left: auto; color: #8a6200; }
.lot-hint { margin: 4px 10px; font-size: 12px; }
.work-order-hint { padding: 8px 10px; border-left: 4px solid #879487; background: #f0f4ed; }
.work-order-hint.blocked { border-left-color: #a62f2f; background: #fbebeb; color: #8f2525; }
.actions { display: flex; justify-content: flex-end; align-items: center; gap: 8px; }
.fixture-note { margin: 0 auto 0 0; color: var(--muted, #777366); font-size: 12px; }
.actions .primary { background: #3b3930; color: white; }
.message { margin: 0; padding: 8px 10px; border: 1px solid #cfcbbd; background: #fffdf6; }
.message.error { border-color: #d7a3a3; background: #fff1f1; color: #932525; }
.message.success { border-color: #a5c9a4; background: #eff8ed; color: #205d25; }
.audit-panel { display: grid; }
.audit-description { margin: 0; padding: 8px 10px; white-space: pre-wrap; }
.picker-backdrop { position: fixed; inset: 0; z-index: 50; display: grid; place-items: center; padding: 18px; background: #0008; }
.picker-dialog { display: grid; gap: 10px; width: min(900px, 96vw); max-height: 85vh; padding: 12px; background: #fffdf6; box-shadow: 0 12px 36px #0004; }
.picker-dialog header { display: flex; align-items: center; justify-content: space-between; }
.picker-dialog header button { min-width: 32px; font-size: 18px; }
.picker-dialog .table-scroll { max-height: 65vh; }
.picker-dialog tr:hover { background: #fff8d9; }
.ems-state-change > p { margin: 0; }
@media (max-width: 1000px) {
  .equipment-panel { grid-template-columns: auto minmax(120px, 1fr) auto auto; }
  .current-state { grid-column: 1 / span 2; }
  .target-label { grid-column: 3; }
  .target-chip { grid-column: 2; }
  .reason-panel { grid-template-columns: minmax(150px, .8fr) minmax(300px, 2fr); }
}
@media (max-width: 700px) {
  .equipment-panel, .reason-panel { grid-template-columns: 1fr; }
  .description-panel { grid-template-columns: 1fr; }
  .description-panel label { padding: 0; }
  .lot-heading { align-items: flex-start; flex-direction: column; }
  .lot-heading strong { margin-left: 0; }
  .actions { flex-wrap: wrap; }
  .fixture-note { flex-basis: 100%; }
}
</style>
