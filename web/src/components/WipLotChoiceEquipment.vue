<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type Equipment = {
  equipmentNo: string
  capacity: number
  eqpLoadPort: number
  description: string
  allowExecute: boolean
  equipmentState: string
  reason: string
}
type Lot = {
  lotNo: string
  lotStamp: number
  status: number
  opStatus: number
  areaNo: string
  opNo: string
  productNo: string
  currentEquipmentNos: string[]
}
type Assignment = {
  serial: string
  lotNo: string
  lotStamp: number
  equipmentNo: string
  loadPort: number | null
  userNo: string
  eventTime: string
}
type PortRow = { loadPort: number; lotNo: string; equipmentNo: string }
type PickerMode = 'eligible' | 'area' | 'freePorts' | 'allPorts' | null

const props = defineProps<{ currentUserNo?: string; developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const storageKey = 'ymes-dev-mock:wip-lot-choice-equipment:assignments:v1'

const lots: Lot[] = [
  { lotNo: 'DEV-LOT-205-01', lotStamp: 2050001, status: 1, opStatus: 1, areaNo: 'DEV-AREA-ETCH', opNo: 'DEV-OP-ETCH-20', productNo: 'DEV-PRD-ETCH-A', currentEquipmentNos: [] },
  { lotNo: 'DEV-LOT-205-02', lotStamp: 2050002, status: 1, opStatus: 0, areaNo: 'DEV-AREA-ETCH', opNo: 'DEV-OP-ETCH-20', productNo: 'DEV-PRD-ETCH-A', currentEquipmentNos: [] },
  { lotNo: 'DEV-LOT-205-03', lotStamp: 2050003, status: 1, opStatus: 1, areaNo: 'DEV-AREA-ETCH', opNo: 'DEV-OP-ETCH-20', productNo: 'DEV-PRD-ETCH-A', currentEquipmentNos: ['DEV-EQP-ETCH-01'] },
]
const areaEquipment: Equipment[] = [
  { equipmentNo: 'DEV-EQP-ETCH-01', capacity: 8, eqpLoadPort: 2, description: 'DEV 蝕刻設備 01', allowExecute: true, equipmentState: 'RUN', reason: '' },
  { equipmentNo: 'DEV-EQP-ETCH-02', capacity: -1, eqpLoadPort: 0, description: 'DEV 蝕刻設備 02', allowExecute: true, equipmentState: 'IDLE', reason: '' },
  { equipmentNo: 'DEV-EQP-ETCH-03', capacity: 4, eqpLoadPort: 1, description: 'DEV 蝕刻設備 03', allowExecute: false, equipmentState: 'DOWN', reason: '設備狀態不允許生產' },
]
const initialPortUsage: PortRow[] = [
  { equipmentNo: 'DEV-EQP-ETCH-01', loadPort: 0, lotNo: 'DEV-LOT-205-77' },
]

const lotNo = ref('')
const loadedLot = ref<Lot | null>(null)
const lotReady = ref(false)
const selectedEquipment = ref<Equipment[]>([])
const selectedLoadPort = ref<number | null>(null)
const assignments = ref<Assignment[]>([])
const pickerMode = ref<PickerMode>(null)
const pickerSearch = ref('')
const pendingEquipmentNos = ref<string[]>([])
const errorText = ref('')
const message = ref('輸入 Lot No 後按 Enter，系統會檢查生產批狀態與可用設備。')
const confirmed = ref(false)

const currentEquipmentText = computed(() => selectedEquipment.value.map(item => item.equipmentNo).join(','))
const loadPortText = computed(() => selectedLoadPort.value === null ? '' : String(selectedLoadPort.value))
const canChoose = computed(() => dev.value && lotReady.value && !confirmed.value)
const canConfirm = computed(() => canChoose.value)
const eligibleEquipment = computed(() => areaEquipment
  .filter(item => item.allowExecute)
  .filter(item => !pickerSearch.value || `${item.equipmentNo} ${item.description} ${item.equipmentState}`.toLowerCase().includes(pickerSearch.value.trim().toLowerCase())))
const areaEquipmentRows = computed(() => areaEquipment
  .filter(item => !pickerSearch.value || `${item.equipmentNo} ${item.description} ${item.equipmentState} ${item.reason}`.toLowerCase().includes(pickerSearch.value.trim().toLowerCase())))

function nowText(): string {
  const now = new Date()
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${now.getFullYear()}/${pad(now.getMonth() + 1)}/${pad(now.getDate())} ${pad(now.getHours())}:${pad(now.getMinutes())}:${pad(now.getSeconds())}`
}

function readAssignments(): void {
  if (!dev.value) return
  try {
    const raw = window.localStorage.getItem(storageKey)
    assignments.value = raw ? JSON.parse(raw) as Assignment[] : []
  } catch {
    assignments.value = []
  }
}

function persistAssignments(): void {
  if (dev.value) window.localStorage.setItem(storageKey, JSON.stringify(assignments.value))
}

function clearLotResult(): void {
  loadedLot.value = null
  lotReady.value = false
  selectedEquipment.value = []
  selectedLoadPort.value = null
  confirmed.value = false
  errorText.value = ''
  message.value = '輸入 Lot No 後按 Enter，系統會檢查生產批狀態與可用設備。'
}

function lotHasEquipment(lot: Lot): boolean {
  return lot.currentEquipmentNos.length > 0 || assignments.value.some(item => item.lotNo === lot.lotNo)
}

function loadLot(): void {
  const normalized = lotNo.value.trim().toUpperCase()
  lotNo.value = normalized
  if (!normalized) {
    clearLotResult()
    errorText.value = 'Lot No 不可空白。'
    return
  }
  if (!dev.value) {
    errorText.value = '目前頁面僅提供 DEV 假資料預覽。'
    return
  }
  if (loadedLot.value?.lotNo === normalized) return

  clearLotResult()
  const lot = lots.find(item => item.lotNo === normalized && item.status === 1)
  if (!lot) {
    errorText.value = '找不到狀態為 1 的生產批。'
    message.value = 'Lot Basis／Lot State 查詢沒有符合的資料。'
    return
  }
  loadedLot.value = lot
  if (lot.opStatus !== 1) {
    errorText.value = '生產批目前不在站中，不能執行設備選用。'
    message.value = '來源規則要求 OPStatus = 1。'
    return
  }
  if (!areaEquipment.length) {
    errorText.value = '此區域沒有可查詢的設備。'
    message.value = 'LoadAvailableEquipment_All 未回傳區域設備。'
    return
  }
  if (lotHasEquipment(lot)) {
    errorText.value = '此生產批已有上機台資料，不需要再次選機。'
    message.value = 'LoadTemp_Equipment(LotNo) 已有設備記錄。'
    return
  }
  lotReady.value = true
  message.value = 'Lot Stamp ' + lot.lotStamp + '　|　Area ' + lot.areaNo + '　|　OP ' + lot.opNo + '　|　產品 ' + lot.productNo
}

function onLotInput(): void {
  clearLotResult()
}

function openPicker(mode: Exclude<PickerMode, null>): void {
  if (!canChoose.value) return
  if ((mode === 'freePorts' || mode === 'allPorts') && (selectedEquipment.value.length !== 1 || selectedEquipment.value[0].eqpLoadPort <= 0)) {
    errorText.value = '請先選擇一台有 LoadPort 管制的設備。'
    return
  }
  pickerMode.value = mode
  pickerSearch.value = ''
  if (mode === 'eligible') pendingEquipmentNos.value = selectedEquipment.value.map(item => item.equipmentNo)
}

function closePicker(): void {
  pickerMode.value = null
  pickerSearch.value = ''
}

function togglePendingEquipment(item: Equipment): void {
  const selected = pendingEquipmentNos.value.includes(item.equipmentNo)
  pendingEquipmentNos.value = selected
    ? pendingEquipmentNos.value.filter(value => value !== item.equipmentNo)
    : [...pendingEquipmentNos.value, item.equipmentNo]
}

function applyEquipmentSelection(): void {
  selectedEquipment.value = pendingEquipmentNos.value
    .map(no => areaEquipment.find(item => item.equipmentNo === no))
    .filter((item): item is Equipment => item !== undefined)
  selectedLoadPort.value = null
  errorText.value = ''
  closePicker()
}

function allPortRows(equipment: Equipment): PortRow[] {
  const occupied = [
    ...initialPortUsage.filter(item => item.equipmentNo === equipment.equipmentNo),
    ...assignments.value
      .filter(item => item.equipmentNo === equipment.equipmentNo && item.loadPort !== null)
      .map(item => ({ equipmentNo: item.equipmentNo, loadPort: item.loadPort as number, lotNo: item.lotNo })),
  ]
  const rows: PortRow[] = []
  for (let port = 0; port < equipment.eqpLoadPort; port += 1) {
    const use = occupied.find(item => item.loadPort === port)
    rows.push({ equipmentNo: equipment.equipmentNo, loadPort: port, lotNo: use?.lotNo ?? '' })
  }
  return rows
}

const currentPortRows = computed(() => {
  const equipment = selectedEquipment.value[0]
  if (!equipment || equipment.eqpLoadPort <= 0) return []
  const query = pickerSearch.value.trim().toLowerCase()
  return allPortRows(equipment).filter(item => !query || String(item.loadPort).includes(query) || item.lotNo.toLowerCase().includes(query))
})
const freePortRows = computed(() => currentPortRows.value.filter(item => !item.lotNo))

function choosePort(row: PortRow): void {
  if (row.lotNo) return
  selectedLoadPort.value = row.loadPort
  errorText.value = ''
  closePicker()
}

function confirmChoice(): void {
  errorText.value = ''
  if (!loadedLot.value) {
    errorText.value = '請先輸入並確認 Lot No。'
    return
  }
  if (!selectedEquipment.value.length) {
    errorText.value = 'EquipmentNo 不可空白。'
    return
  }
  if (selectedEquipment.value.some(item => !item.allowExecute)) {
    errorText.value = '選取的設備目前不可執行此生產批。'
    return
  }
  if (selectedLoadPort.value !== null && selectedEquipment.value.length !== 1) {
    errorText.value = '多選設備時不可設定 LoadPort。'
    return
  }
  if (lotHasEquipment(loadedLot.value)) {
    errorText.value = '生產批設備狀態已變更，請重新查詢。'
    clearLotResult()
    return
  }

  const createdAt = nowText()
  for (const equipment of selectedEquipment.value) {
    assignments.value.push({
      serial: 'DEV-WP205-' + Date.now() + '-' + equipment.equipmentNo,
      lotNo: loadedLot.value.lotNo,
      lotStamp: loadedLot.value.lotStamp,
      equipmentNo: equipment.equipmentNo,
      loadPort: selectedEquipment.value.length === 1 ? selectedLoadPort.value : null,
      userNo: props.currentUserNo || 'DEVUSER',
      eventTime: createdAt,
    })
  }
  persistAssignments()
  confirmed.value = true
  message.value = 'DEV 已模擬完成設備選用：' + currentEquipmentText.value + (loadPortText.value ? '，LoadPort ' + loadPortText.value : '')
}

onMounted(readAssignments)
</script>

<template>
  <section class="wp205-page">
    <div v-if="dev" class="dev-banner">DEV 假資料預覽；所有操作只儲存在此瀏覽器的 localStorage。</div>
    <h1>生產批選用生產設備 <small>Lot Choice EQP</small></h1>
    <p v-if="errorText" class="message error-message" role="alert">{{ errorText }}</p>
    <p v-else-if="confirmed" class="message success-message" role="status">{{ message }}</p>

    <fieldset class="choice-frame" aria-label="Input Data">
      <div class="choice-grid">
        <label for="wp205-lot-no">Lot No</label>
        <input id="wp205-lot-no" v-model="lotNo" class="mes-input editable lot-input" :disabled="!dev || confirmed" autocomplete="off" placeholder="輸入 Lot No 後按 Enter" @input="onLotInput" @keydown.enter.prevent="loadLot" @blur="loadLot">
        <span class="lot-hint">{{ loadedLot ? '狀態 1' : 'Enter' }}</span>

        <label for="wp205-equipment-no">EquipmentNo</label>
        <input id="wp205-equipment-no" class="mes-input readonly" :value="currentEquipmentText" readonly>
        <button type="button" class="mes-button lookup-button equipment-picker" :disabled="!canChoose" aria-label="選擇可用設備" @click="openPicker('eligible')">...</button>
        <button type="button" class="mes-button area-button" :disabled="!canChoose" @click="openPicker('area')">Area EQP</button>

        <label for="wp205-load-port">LoadPortPosition</label>
        <input id="wp205-load-port" class="mes-input readonly" :value="loadPortText" readonly>
        <button type="button" class="mes-button lookup-button port-picker" :disabled="!canChoose || selectedEquipment.length !== 1 || selectedEquipment[0].eqpLoadPort <= 0" aria-label="選擇可用 LoadPort" @click="openPicker('freePorts')">...</button>
        <button type="button" class="mes-button loadport-button" :disabled="!canChoose || selectedEquipment.length !== 1 || selectedEquipment[0].eqpLoadPort <= 0" @click="openPicker('allPorts')">LoadPort</button>
      </div>
      <div v-if="loadedLot" class="lot-context">
        <span>Area：{{ loadedLot.areaNo }}</span><span>OP：{{ loadedLot.opNo }}</span><span>產品：{{ loadedLot.productNo }}</span>
      </div>
      <div class="hint-line">單筆設備可選 LoadPort；多選設備時會清除 LoadPort。LoadPort 可不選，依 WinForms CI 流程。</div>
    </fieldset>

    <div class="status-line" role="status">{{ confirmed ? '操作完成' : message }}</div>
    <div class="actions">
      <span class="action-note">{{ selectedEquipment.length ? '已選 ' + selectedEquipment.length + ' 台設備' : '請先查詢 Lot No' }}</span>
      <button type="button" class="mes-button confirm-button" :disabled="!canConfirm" @click="confirmChoice">Confirm</button>
      <button type="button" class="mes-button" @click="emit('close')">Close</button>
    </div>

    <div v-if="pickerMode" class="modal-backdrop" @click.self="closePicker">
      <section class="picker-dialog" role="dialog" aria-modal="true" :aria-label="pickerMode === 'eligible' ? 'Equipment selection' : pickerMode === 'area' ? 'Area equipment' : 'LoadPort selection'">
        <header class="picker-header">
          <strong>{{ pickerMode === 'eligible' ? '選擇可用設備' : pickerMode === 'area' ? 'Area EQP' : pickerMode === 'freePorts' ? '選擇可用 LoadPort' : 'LoadPort 狀態' }}</strong>
          <button type="button" class="mes-button" @click="closePicker">Close</button>
        </header>
        <input v-if="pickerMode === 'eligible' || pickerMode === 'area' || pickerMode === 'allPorts'" v-model="pickerSearch" class="mes-input picker-search" :placeholder="pickerMode === 'allPorts' ? 'LoadPort / LotNo' : 'EquipmentNo / Description'">

        <div v-if="pickerMode === 'eligible' || pickerMode === 'area'" class="table-scroll">
          <table class="choice-table">
            <thead><tr><th v-if="pickerMode === 'eligible'">選擇</th><th>EquipmentNo</th><th>Capacity</th><th>EQPLoadPort</th><th>AllowExecute</th><th>EquipmentState</th><th>Description</th></tr></thead>
            <tbody>
              <tr v-for="item in pickerMode === 'eligible' ? eligibleEquipment : areaEquipmentRows" :key="item.equipmentNo" :class="{ unavailable: !item.allowExecute }">
                <td v-if="pickerMode === 'eligible'" class="center-cell"><input type="checkbox" :checked="pendingEquipmentNos.includes(item.equipmentNo)" @change="togglePendingEquipment(item)"></td>
                <td>{{ item.equipmentNo }}</td><td>{{ item.capacity === -1 ? '-1（不限）' : item.capacity }}</td><td>{{ item.eqpLoadPort }}</td>
                <td>{{ item.allowExecute ? '1' : '0' }}<span v-if="item.reason">　{{ item.reason }}</span></td><td>{{ item.equipmentState }}</td><td>{{ item.description }}</td>
              </tr>
              <tr v-if="(pickerMode === 'eligible' ? eligibleEquipment : areaEquipmentRows).length === 0"><td :colspan="pickerMode === 'eligible' ? 7 : 6" class="empty-cell">沒有符合資料</td></tr>
            </tbody>
          </table>
        </div>

        <div v-else class="table-scroll">
          <table class="choice-table port-table">
            <thead><tr><th>LoadPort</th><th>LotNo</th><th>EquipmentNo</th><th v-if="pickerMode === 'freePorts'">選擇</th></tr></thead>
            <tbody>
              <tr v-for="row in pickerMode === 'freePorts' ? freePortRows : currentPortRows" :key="row.equipmentNo + '-' + row.loadPort" :class="{ occupied: !!row.lotNo }">
                <td>{{ row.loadPort }}</td><td>{{ row.lotNo || '（空）' }}</td><td>{{ row.equipmentNo }}</td>
                <td v-if="pickerMode === 'freePorts'"><button type="button" class="mes-button" @click="choosePort(row)">選取</button></td>
              </tr>
              <tr v-if="(pickerMode === 'freePorts' ? freePortRows : currentPortRows).length === 0"><td :colspan="pickerMode === 'freePorts' ? 4 : 3" class="empty-cell">沒有符合資料</td></tr>
            </tbody>
          </table>
        </div>
        <p v-if="pickerMode === 'eligible'" class="picker-hint">AllowExecute=1 才能選取；多選時不允許指定 LoadPort。</p>
        <p v-if="pickerMode === 'area'" class="picker-hint">此視窗只顯示區域機台與可用狀態，符合 WinForms Area EQP 查詢。</p>
        <footer v-if="pickerMode === 'eligible'" class="picker-actions">
          <button type="button" class="mes-button confirm-button" :disabled="pendingEquipmentNos.length === 0" @click="applyEquipmentSelection">Confirm</button>
          <button type="button" class="mes-button" @click="closePicker">Close</button>
        </footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.wp205-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:8px;padding:10px;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif;box-sizing:border-box}.dev-banner{padding:7px 10px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}.wp205-page h1{margin:0;padding:0 2px;font-size:18px;font-weight:600;color:#333}.wp205-page h1 small{margin-left:8px;color:#777;font-size:13px;font-weight:400}.choice-frame{width:min(640px,100%);margin:8px auto 0;padding:13px 14px 14px;border:1px solid #bdbdbd;background:#fff;box-sizing:border-box}.choice-grid{display:grid;grid-template-columns:105px minmax(120px,1fr) 34px 84px;grid-auto-rows:31px;align-items:center;gap:7px 6px}.choice-grid>label{text-align:right;padding-right:4px;white-space:nowrap}.mes-input{height:26px;min-width:0;width:100%;padding:4px 7px;border:1px solid #888;border-radius:2px;background:#fff;color:#333;font:inherit;box-sizing:border-box}.mes-input.editable{background:#fff0b3}.mes-input.readonly{background:#eee;color:#555}.lot-input{grid-column:2}.lot-hint{font-size:11px;color:#777;text-align:center}.lookup-button{width:32px;padding:2px}.mes-button{min-height:29px;padding:3px 11px;border:1px solid #b9b9b9;border-radius:3px;background:linear-gradient(#fff,#f4f4f4);color:#333;font:inherit;white-space:nowrap;cursor:pointer}.mes-button:hover:not(:disabled){background:#fff8e6}.mes-button:disabled{color:#999;cursor:default}.lot-context{display:flex;flex-wrap:wrap;gap:8px 20px;margin:9px 0 0 112px;color:#555}.hint-line{margin:9px 0 0 112px;color:#777;font-size:11px}.message{width:min(640px,100%);margin:0 auto;padding:7px 10px;border:1px solid}.error-message{border-color:#d8aaa6;background:#fff0ef;color:#9c241d}.success-message{border-color:#a9c6a2;background:#f1faed;color:#2e6332}.status-line{margin-top:auto;min-height:28px;padding:6px 7px;border-top:1px solid #ccc;color:#555;box-sizing:border-box}.actions{display:flex;align-items:center;justify-content:flex-end;gap:8px}.action-note{margin-right:auto;color:#777}.confirm-button{min-width:90px;color:#fff;background:#f3a62a;border-color:#e99512;font-weight:600}.confirm-button:hover:not(:disabled){background:#e99a1b}.modal-backdrop{position:fixed;inset:0;z-index:1200;display:grid;place-items:center;padding:16px;background:#0005}.picker-dialog{width:min(1000px,96vw);max-height:88vh;display:flex;flex-direction:column;gap:9px;padding:14px;background:#fff;border:1px solid #aaa;box-shadow:0 8px 28px #0004;box-sizing:border-box}.picker-header{display:flex;align-items:center;justify-content:space-between}.picker-search{max-width:420px}.table-scroll{max-height:55vh;overflow:auto;border:1px solid #bcbcbc}.choice-table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}.choice-table th{position:sticky;top:0;z-index:1;padding:6px 7px;border:1px solid #d1d1d1;background:#dfeaf2;text-align:left}.choice-table td{height:29px;padding:3px 7px;border:1px solid #d1d1d1;background:#fff0b3}.choice-table tbody tr:nth-child(even) td{background:#ffeaa2}.choice-table tr.unavailable td,.choice-table tr.occupied td{color:#777;background:#eee}.center-cell{text-align:center}.empty-cell{text-align:center!important;background:#f8f8f8!important;color:#777!important;padding:18px!important}.picker-hint{margin:0;color:#777;font-size:12px}.picker-actions{display:flex;justify-content:flex-end;gap:8px;padding-top:4px}@media(max-width:680px){.choice-frame{margin-top:0}.choice-grid{grid-template-columns:98px minmax(0,1fr) 32px}.choice-grid>label{grid-column:1}.choice-grid>input{grid-column:2}.lot-hint{grid-column:3}.area-button,.loadport-button{grid-column:2/4;justify-self:end}.lot-context,.hint-line{margin-left:0}.actions{flex-wrap:wrap}.action-note{width:100%}}
</style>
