<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type FormMode = 'add' | 'edit' | 'copy'
type RegistrationState = 0 | 1
interface EquipmentRow { equipmentNo: string; equipmentType: string; eqpSerialNo: string; parentEquipmentNo: string; chamber: boolean; equipmentState: string }
interface BreakdownRecord { breakDownSerialNo: string; eqpSerialNo: string; equipmentNo: string; state: RegistrationState; breakDownStartTime: string; description: string; creator: string; createDate: string; reviser: string; reviseDate: string; dataStamp: number; changeStateFlag: 'Y' | 'N'; changeState: string; nextPmDate: string }
interface BreakdownDetail { breakDownSerialNo: string; breakDownNo: string }
interface DevState { version: 1; chamberManage: boolean; nextSerial: number; equipment: EquipmentRow[]; records: BreakdownRecord[]; details: BreakdownDetail[] }
interface CauseChoice { breakDownNo: string; breakDownCause: string; description: string; issueState?: number }
interface StateChoice { value: string; name: string; description: string }
interface Draft { breakDownSerialNo: string; equipmentNo: string; breakDownStartTime: string; nextPmDate: string; description: string; breakDownNos: string[]; stateChoice: string }

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.pm.eqp-breakdown-register.v1'
const mapStorageKey = 'ymes.dev.pm.eqp-type-breakdown-map.v1'
const causeStorageKey = 'ymes.dev.pm.breakdown-basis.v1'
const currentUser = 'DEV01'
const defaultBreakdownState = 'ES102'
const stateChoices: StateChoice[] = [
  { value: 'ES102', name: 'ES102（預設故障狀態）', description: 'CHANGED' },
  { value: 'NOCHANGE', name: '不變更設備狀態', description: 'NOCHANGED' },
]

function localInput(date = new Date()) {
  const adjusted = new Date(date.getTime() - date.getTimezoneOffset() * 60_000)
  return adjusted.toISOString().slice(0, 16)
}
function formatDateTime(value: string) { return value ? value.replace('T', ' ') : '—' }
function seedState(): DevState {
  return {
    version: 1,
    chamberManage: false,
    nextSerial: 124,
    equipment: [
      { equipmentNo: 'ETCH-001', equipmentType: 'ETCHER-01', eqpSerialNo: 'EQP-0001', parentEquipmentNo: '', chamber: false, equipmentState: 'RUN' },
      { equipmentNo: 'ETCH-002', equipmentType: 'ETCHER-01', eqpSerialNo: 'EQP-0002', parentEquipmentNo: 'ETCH-001', chamber: true, equipmentState: 'IDLE' },
      { equipmentNo: 'ETCH-003', equipmentType: 'ETCHER-01', eqpSerialNo: 'EQP-0003', parentEquipmentNo: '', chamber: false, equipmentState: 'RUN' },
      { equipmentNo: 'CVD-101', equipmentType: 'CVD-02', eqpSerialNo: 'EQP-0101', parentEquipmentNo: '', chamber: false, equipmentState: 'RUN' },
      { equipmentNo: 'CVD-102', equipmentType: 'CVD-02', eqpSerialNo: 'EQP-0102', parentEquipmentNo: 'CVD-101', chamber: true, equipmentState: 'IDLE' },
      { equipmentNo: 'SCR-201', equipmentType: 'SCRUBBER-03', eqpSerialNo: 'EQP-0201', parentEquipmentNo: '', chamber: false, equipmentState: 'RUN' },
      { equipmentNo: 'WET-301', equipmentType: 'WET-BENCH-04', eqpSerialNo: 'EQP-0301', parentEquipmentNo: '', chamber: false, equipmentState: 'RUN' },
    ],
    records: [
      { breakDownSerialNo: '000000122', eqpSerialNo: 'EQP-0003', equipmentNo: 'ETCH-003', state: 0, breakDownStartTime: '2026-11-26T08:42', description: '感測器讀值間歇中斷。', creator: 'PMUSER', createDate: '2026-11-26 08:45', reviser: '', reviseDate: '', dataStamp: 2, changeStateFlag: 'Y', changeState: 'ES102', nextPmDate: '2026-11-26' },
      { breakDownSerialNo: '000000123', eqpSerialNo: 'EQP-0101', equipmentNo: 'CVD-101', state: 0, breakDownStartTime: '2026-11-26T09:20', description: '冷卻水壓力低於設定值。', creator: 'DEV02', createDate: '2026-11-26 09:25', reviser: '', reviseDate: '', dataStamp: 1, changeStateFlag: 'N', changeState: '', nextPmDate: '2026-11-27' },
      { breakDownSerialNo: '000000121', eqpSerialNo: 'EQP-0301', equipmentNo: 'WET-301', state: 1, breakDownStartTime: '2026-11-25T16:10', description: '前一筆已確認，留作待處理範例。', creator: 'PMUSER', createDate: '2026-11-25 16:14', reviser: 'PMUSER', reviseDate: '2026-11-25 16:16', dataStamp: 2, changeStateFlag: 'Y', changeState: 'ES102', nextPmDate: '2026-11-28' },
    ],
    details: [
      { breakDownSerialNo: '000000122', breakDownNo: 'BD-SENSOR-03' },
      { breakDownSerialNo: '000000123', breakDownNo: 'BD-HYD-08' },
      { breakDownSerialNo: '000000121', breakDownNo: 'BD-ELEC-01' },
    ],
  }
}
function loadState(): DevState {
  try {
    const parsed = JSON.parse(window.localStorage.getItem(storageKey) ?? 'null') as DevState | null
    if (parsed?.version === 1 && Array.isArray(parsed.equipment) && Array.isArray(parsed.records) && Array.isArray(parsed.details)) return parsed
  } catch { /* Keep the DEV screen available without local storage. */ }
  return seedState()
}
function loadMapRows(): Array<{ equipmentType?: string; breakDownNo?: string }> {
  try {
    const parsed = JSON.parse(window.localStorage.getItem(mapStorageKey) ?? 'null') as { records?: Array<{ equipmentType?: string; breakDownNo?: string }> } | null
    if (Array.isArray(parsed?.records)) return parsed.records
  } catch { /* Use the illustrative PM403 mappings below. */ }
  return [
    { equipmentType: 'ETCHER-01', breakDownNo: 'BD-ELEC-01' }, { equipmentType: 'ETCHER-01', breakDownNo: 'BD-MECH-02' }, { equipmentType: 'ETCHER-01', breakDownNo: 'BD-SENSOR-03' },
    { equipmentType: 'CVD-02', breakDownNo: 'BD-ELEC-01' }, { equipmentType: 'CVD-02', breakDownNo: 'BD-HYD-08' },
    { equipmentType: 'SCRUBBER-03', breakDownNo: 'BD-HYD-08' },
  ]
}
function loadCauseBasis(): Map<string, CauseChoice> {
  try {
    const parsed = JSON.parse(window.localStorage.getItem(causeStorageKey) ?? 'null') as { records?: CauseChoice[] } | null
    if (Array.isArray(parsed?.records)) return new Map<string, CauseChoice>(parsed.records.filter((row) => row.breakDownNo).map((row) => [row.breakDownNo.toUpperCase(), row] as [string, CauseChoice]))
  } catch { /* Use DEV display names below. */ }
  return new Map<string, CauseChoice>([
    ['BD-ELEC-01', { breakDownNo: 'BD-ELEC-01', breakDownCause: '電源異常', description: '檢查電源線、保險絲及供電模組。', issueState: 2 }],
    ['BD-MECH-02', { breakDownNo: 'BD-MECH-02', breakDownCause: '傳動異音', description: '確認皮帶、軸承與固定座。', issueState: 2 }],
    ['BD-SENSOR-03', { breakDownNo: 'BD-SENSOR-03', breakDownCause: '感測器訊號異常', description: '清潔感測面並檢查線路接頭。', issueState: 2 }],
    ['BD-HYD-08', { breakDownNo: 'BD-HYD-08', breakDownCause: '油壓不足', description: '檢查油位、管路及壓力值。', issueState: 2 }],
  ])
}

const state = ref<DevState>(loadState())
const serialQuery = ref('')
const activeSerial = ref('')
const selectedSerials = ref<string[]>([])
const formMode = ref<FormMode | null>(null)
const draft = ref<Draft | null>(null)
const message = ref('')
const messageKind = ref<'success' | 'error'>('success')
const nowLabel = computed(() => localInput().replace('T', ' '))
const visibleEquipment = computed(() => state.value.equipment.filter((row) => state.value.chamberManage || !row.chamber))
const visibleRecords = computed(() => state.value.records.filter((row) => row.state === 0)
  .filter((row) => !serialQuery.value.trim() || row.breakDownSerialNo.toLocaleLowerCase().includes(serialQuery.value.trim().toLocaleLowerCase()))
  .sort((a, b) => b.breakDownStartTime.localeCompare(a.breakDownStartTime)))
const activeRecord = computed(() => state.value.records.find((row) => row.breakDownSerialNo === activeSerial.value) ?? null)
const detailRows = computed(() => activeRecord.value ? state.value.details.filter((row) => row.breakDownSerialNo === activeRecord.value!.breakDownSerialNo) : [])
const equipmentByNo = computed(() => new Map(state.value.equipment.map((row) => [row.equipmentNo, row])))
const selectedEquipment = computed(() => draft.value ? equipmentByNo.value.get(draft.value.equipmentNo) ?? null : null)
const mappedCauseNos = computed(() => {
  if (!selectedEquipment.value) return []
  return [...new Set(loadMapRows().filter((row) => row.equipmentType === selectedEquipment.value!.equipmentType && row.breakDownNo).map((row) => row.breakDownNo!.toUpperCase()))]
    .filter((no) => causeBasis.value.get(no)?.issueState === 2)
    .sort()
})
const causeBasis = computed(() => loadCauseBasis())
const causeNames = computed(() => new Map<string, string>([...causeBasis.value].map(([no, row]) => [no, row.breakDownCause] as [string, string])))
const actionTargets = computed(() => selectedSerials.value.length ? [...selectedSerials.value] : activeRecord.value ? [activeRecord.value.breakDownSerialNo] : [])
const allVisibleSelected = computed(() => visibleRecords.value.length > 0 && visibleRecords.value.every((row) => selectedSerials.value.includes(row.breakDownSerialNo)))
const formTitle = computed(() => formMode.value === 'edit' ? '修改設備故障登錄' : formMode.value === 'copy' ? '複製設備故障登錄' : '新增設備故障登錄')
watch(state, (value) => { try { window.localStorage.setItem(storageKey, JSON.stringify(value)) } catch { /* The preview remains usable without persistence. */ } }, { deep: true })

function toast(text: string, kind: 'success' | 'error' = 'success') { message.value = text; messageKind.value = kind }
function causeDescription(breakDownNo: string) { return causeBasis.value.get(breakDownNo.toUpperCase())?.description || '—' }
function selectRecord(serial: string) { activeSerial.value = serial; selectedSerials.value = [] }
function toggleRecord(serial: string) {
  selectedSerials.value = selectedSerials.value.includes(serial) ? selectedSerials.value.filter((value) => value !== serial) : [...selectedSerials.value, serial]
  activeSerial.value = serial
}
function toggleAll() {
  selectedSerials.value = allVisibleSelected.value ? [] : visibleRecords.value.map((row) => row.breakDownSerialNo)
  if (selectedSerials.value.length) activeSerial.value = selectedSerials.value[0]
}
function nextSerial() { const value = String(state.value.nextSerial++).padStart(9, '0'); return value }
function setEquipment(no: string) {
  if (!draft.value) return
  draft.value.equipmentNo = no
  draft.value.breakDownNos = []
}
function openForm(mode: FormMode) {
  const row = activeRecord.value
  if ((mode === 'edit' || mode === 'copy') && !row) return toast('請先選取設備故障登錄。', 'error')
  const now = localInput()
  const chosenEquipment = mode === 'add' ? '' : row!.equipmentNo
  const selectedState = mode === 'add' ? '' : row!.changeStateFlag === 'N' ? 'NOCHANGE' : row!.changeState
  formMode.value = mode
  draft.value = {
    breakDownSerialNo: mode === 'edit' ? row!.breakDownSerialNo : nextSerial(),
    equipmentNo: chosenEquipment,
    breakDownStartTime: mode === 'add' ? now : mode === 'copy' ? now : row!.breakDownStartTime,
    nextPmDate: mode === 'add' || mode === 'copy' ? now.slice(0, 10) : row!.nextPmDate,
    description: mode === 'add' ? '' : row!.description,
    breakDownNos: mode === 'add' ? [] : state.value.details.filter((item) => item.breakDownSerialNo === row!.breakDownSerialNo).map((item) => item.breakDownNo),
    stateChoice: selectedState,
  }
  message.value = ''
}
function closeForm() { formMode.value = null; draft.value = null }
function saveForm() {
  const mode = formMode.value
  const form = draft.value
  if (!mode || !form) return
  const serial = form.breakDownSerialNo.trim().toLocaleUpperCase()
  const equipment = equipmentByNo.value.get(form.equipmentNo)
  if (!serial) return toast('BreakDownSerialNo 為必填。', 'error')
  if (!equipment) return toast('請選擇 EquipmentNo。', 'error')
  const startTime = new Date(form.breakDownStartTime).getTime()
  if (!Number.isFinite(startTime) || startTime >= Date.now()) return toast('故障開始時間必須是有效日期，且早於目前時間。', 'error')
  if (form.breakDownNos.length === 0) return toast('請至少勾選一項故障原因。', 'error')
  const duplicateSerial = state.value.records.some((row) => row.breakDownSerialNo === serial && !(mode === 'edit' && row.breakDownSerialNo === activeSerial.value))
  if (duplicateSerial) return toast('BreakDownSerialNo 已存在。', 'error')
  const occupiedEquipment = state.value.records.some((row) => row.equipmentNo === equipment.equipmentNo && !(mode === 'edit' && row.breakDownSerialNo === activeSerial.value))
  if (occupiedEquipment) return toast('此設備已有故障登錄，請先處理既有單據。', 'error')

  const selectedState = stateChoices.find((choice) => choice.value === form.stateChoice)
  const changeStateFlag: 'Y' | 'N' = selectedState?.description.toUpperCase().includes('NOCHANGED') ? 'N' : 'Y'
  const changeState = changeStateFlag === 'N' ? '' : selectedState?.value ?? defaultBreakdownState
  const stamp = nowLabel.value
  if (mode === 'edit') {
    const row = state.value.records.find((item) => item.breakDownSerialNo === activeSerial.value)
    if (!row || row.state !== 0) return toast('只有未確認故障單可以修改。', 'error')
    row.breakDownStartTime = form.breakDownStartTime
    row.description = form.description
    // The custom Core Edit method does not receive or update Reviser/ReviseDate.
    row.changeStateFlag = changeStateFlag
    row.changeState = changeState
    row.nextPmDate = form.nextPmDate
    state.value.details = state.value.details.filter((item) => item.breakDownSerialNo !== row.breakDownSerialNo)
    state.value.details.push(...form.breakDownNos.map((breakDownNo) => ({ breakDownSerialNo: row.breakDownSerialNo, breakDownNo })))
    toast('設備故障登錄已修改；舊 Core 未落實 DataStamp 併發檢查。')
    activeSerial.value = row.breakDownSerialNo
  } else {
    const row: BreakdownRecord = {
      breakDownSerialNo: serial,
      eqpSerialNo: equipment.eqpSerialNo,
      equipmentNo: equipment.equipmentNo,
      state: 0,
      breakDownStartTime: form.breakDownStartTime,
      description: form.description,
      creator: currentUser,
      createDate: stamp,
      reviser: '',
      reviseDate: '',
      dataStamp: 0,
      changeStateFlag,
      changeState,
      nextPmDate: form.nextPmDate,
    }
    state.value.records.push(row)
    state.value.details.push(...form.breakDownNos.map((breakDownNo) => ({ breakDownSerialNo: serial, breakDownNo })))
    activeSerial.value = serial
    toast(mode === 'copy' ? '故障登錄已複製。' : '故障登錄已新增。')
  }
  selectedSerials.value = []
  closeForm()
}
function deleteSelected() {
  const targets = actionTargets.value
  if (!targets.length || !window.confirm(`確定刪除 ${targets.length} 筆未確認故障登錄及其原因明細？`)) return
  const targetSet = new Set(targets)
  state.value.details = state.value.details.filter((row) => !targetSet.has(row.breakDownSerialNo))
  state.value.records = state.value.records.filter((row) => !targetSet.has(row.breakDownSerialNo))
  activeSerial.value = ''
  selectedSerials.value = []
  toast(`已刪除 ${targets.length} 筆未確認故障登錄。`)
}
function confirmSelected() {
  const targets = actionTargets.value
  if (!targets.length || !window.confirm(`確定確認 ${targets.length} 筆故障登錄？確認後將移出未確認清單。`)) return
  const targetSet = new Set(targets)
  const confirmed = state.value.records.filter((row) => targetSet.has(row.breakDownSerialNo) && row.state === 0)
  for (const record of confirmed) {
    if (record.changeStateFlag === 'Y' && record.changeState) {
      const equipment = equipmentByNo.value.get(record.equipmentNo)
      if (equipment) {
        const choice = stateChoices.find((item) => item.value === record.changeState)
        equipment.equipmentState = choice?.value ?? record.changeState
        if (state.value.chamberManage) {
          for (const child of state.value.equipment.filter((item) => item.parentEquipmentNo === equipment.equipmentNo)) child.equipmentState = equipment.equipmentState
        }
      }
    }
    record.state = 1
    record.eqpSerialNo = equipmentByNo.value.get(record.equipmentNo)?.eqpSerialNo ?? record.eqpSerialNo
  }
  activeSerial.value = ''
  selectedSerials.value = []
  toast(`已確認 ${confirmed.length} 筆。DEV 預覽不會寄送工程群組郵件。`)
}
</script>

<template>
  <section class="breakdown-page" aria-label="設備故障登錄 DEV 預覽">
    <header class="page-heading">
      <div><div class="eyebrow">PM404 · 設備維修</div><h1>設備故障登錄</h1><p>目前清單顯示未確認（State=0）的故障登錄。</p></div>
      <div class="heading-actions"><span class="dev-badge">DEV 假資料</span><button type="button" class="secondary-button" @click="emit('close')">關閉</button></div>
    </header>
    <div class="context-bar"><span>{{ visibleRecords.length }} 筆未確認登錄</span><span>設備狀態連動依故障狀態設定</span><label class="chamber-toggle"><input v-model="state.chamberManage" type="checkbox">DEV 子設備管理</label></div>

    <section class="panel main-panel">
      <div class="panel-heading">
        <div><h2>Break Down Register</h2><span>未確認故障單</span></div>
        <div class="search-toolbar"><label>BreakDownSerialNo<input v-model="serialQuery" type="search" placeholder="搜尋單號"></label><button type="button" @click="serialQuery = serialQuery.trim()">查詢</button></div>
      </div>
      <div class="toolbar"><button type="button" class="primary-button" @click="openForm('add')">新增</button><button type="button" :disabled="!activeRecord" @click="openForm('edit')">修改</button><button type="button" :disabled="actionTargets.length === 0" @click="deleteSelected">刪除</button><button type="button" :disabled="!activeRecord" @click="openForm('copy')">複製</button><button type="button" class="confirm-button" :disabled="actionTargets.length === 0" @click="confirmSelected">確認</button></div>
      <div class="table-scroll main-scroll">
        <table>
          <thead><tr><th class="select-column"><input type="checkbox" aria-label="全選未確認故障單" :checked="allVisibleSelected" :disabled="visibleRecords.length === 0" @change="toggleAll"></th><th>BreakDownSerialNo</th><th>EquipmentNo</th><th>State</th><th>BreakDownStartTime</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
          <tbody>
            <tr v-for="row in visibleRecords" :key="row.breakDownSerialNo" :class="{ active: row.breakDownSerialNo === activeSerial, selected: selectedSerials.includes(row.breakDownSerialNo) }" @click="selectRecord(row.breakDownSerialNo)">
              <td class="select-column" @click.stop><input type="checkbox" :checked="selectedSerials.includes(row.breakDownSerialNo)" :aria-label="`選取 ${row.breakDownSerialNo}`" @change="toggleRecord(row.breakDownSerialNo)"></td>
              <td class="serial-cell">{{ row.breakDownSerialNo }}</td><td>{{ row.equipmentNo }}</td><td><span class="state-pill">未確認 · 0</span></td><td>{{ formatDateTime(row.breakDownStartTime) }}</td><td class="description-cell">{{ row.description || '—' }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser || '—' }}</td><td>{{ row.reviseDate || '—' }}</td>
            </tr>
            <tr v-if="visibleRecords.length === 0"><td colspan="10" class="empty-cell">沒有符合的未確認故障登錄。</td></tr>
          </tbody>
        </table>
      </div>
      <p v-if="message" class="status-message" :class="messageKind" role="status">{{ message }}</p>
    </section>

    <section class="panel detail-panel">
      <div class="panel-heading"><div><h2>Break Down Cause Detail</h2><span>{{ activeRecord?.breakDownSerialNo ?? '請選取未確認故障單' }} · {{ detailRows.length }} 項</span></div><span v-if="activeRecord" class="equipment-state">設備目前狀態：{{ equipmentByNo.get(activeRecord.equipmentNo)?.equipmentState ?? '—' }}</span></div>
      <div class="table-scroll detail-scroll"><table><thead><tr><th>BreakDownSerialNo</th><th>BreakDownNo</th><th>BreakDownCause</th><th>Description</th></tr></thead><tbody>
        <tr v-for="detail in detailRows" :key="`${detail.breakDownSerialNo}-${detail.breakDownNo}`"><td>{{ detail.breakDownSerialNo }}</td><td class="serial-cell">{{ detail.breakDownNo }}</td><td>{{ causeNames.get(detail.breakDownNo.toUpperCase()) || '（故障原因主檔不存在）' }}</td><td>{{ causeDescription(detail.breakDownNo) }}</td></tr>
        <tr v-if="!detailRows.length"><td colspan="4" class="empty-cell">選取一筆故障登錄後顯示其原因明細。</td></tr>
      </tbody></table></div>
    </section>

    <div v-if="formMode && draft" class="dialog-backdrop" @click.self="closeForm">
      <section class="edit-dialog" role="dialog" aria-modal="true" :aria-label="formTitle">
        <header><div><div class="eyebrow">{{ formMode.toUpperCase() }}</div><h2>{{ formTitle }}</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="closeForm">×</button></header>
        <p v-if="message" class="dialog-message" :class="messageKind" role="status">{{ message }}</p>
        <div class="form-grid">
          <label>BreakDownSerialNo<div class="serial-input"><input v-model="draft.breakDownSerialNo" :readonly="formMode === 'edit'" @blur="draft.breakDownSerialNo = draft.breakDownSerialNo.toLocaleUpperCase()"><button v-if="formMode !== 'edit'" type="button" @click="draft.breakDownSerialNo = nextSerial()">重新取號</button></div></label>
          <label>EquipmentNo<select :value="draft.equipmentNo" :disabled="formMode === 'edit'" @change="setEquipment(($event.target as HTMLSelectElement).value)"><option value="">請選擇設備</option><option v-for="eq in visibleEquipment" :key="eq.equipmentNo" :value="eq.equipmentNo">{{ eq.equipmentNo }} · {{ eq.equipmentType }} · {{ eq.equipmentState }}</option></select></label>
          <label>BreakDownStartTime<input v-model="draft.breakDownStartTime" type="datetime-local"></label>
          <label>Next PM Date<input v-model="draft.nextPmDate" type="date"></label>
          <label class="full-row">設備狀態處理<select v-model="draft.stateChoice"><option value="">使用 ES102（BreakDown 控制預設值）</option><option v-for="choice in stateChoices" :key="choice.value" :value="choice.value">{{ choice.name }} · {{ choice.description }}</option></select><small>NOCHANGED 選項會保留設備既有狀態。</small></label>
          <label class="full-row">Description<textarea v-model="draft.description" rows="3"></textarea></label>
        </div>
        <fieldset class="cause-picker"><legend>故障原因（至少勾選一項）</legend><p v-if="!selectedEquipment">請先選擇設備以載入其設備類型對應原因。</p><p v-else-if="mappedCauseNos.length === 0">此設備類型尚未在 PM403 設定故障原因。</p><label v-for="no in mappedCauseNos" :key="no" class="cause-option"><input v-model="draft.breakDownNos" type="checkbox" :value="no"><span><b>{{ no }}</b><small>{{ causeNames.get(no) || '（原因名稱待維護）' }}</small></span></label></fieldset>
        <p class="form-footnote">設備與故障原因資料均為 DEV 假資料；確認只模擬狀態異動，不會呼叫正式服務或寄信。</p>
        <footer><button type="button" @click="closeForm">取消</button><button type="button" class="primary-button" @click="saveForm">確定</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.breakdown-page { display:flex; flex-direction:column; gap:12px; min-height:0; height:100%; box-sizing:border-box; padding:16px; overflow:auto; background:#f6f4ed; color:#26251f; font:inherit; }
.page-heading,.panel-heading,.heading-actions,.search-toolbar,.toolbar,.serial-input,.edit-dialog header,.edit-dialog footer { display:flex; align-items:center; justify-content:space-between; gap:10px; }
.page-heading h1 { margin:2px 0 3px; font-size:20px; font-weight:700; }
.page-heading p { margin:0; color:#716e63; font-size:12px; }
.eyebrow { color:#777366; font-size:10px; letter-spacing:.1em; font-weight:700; }
.dev-badge { padding:4px 8px; border:1px solid #c3a56c; border-radius:12px; background:#fff8e8; color:#76571d; font-size:11px; font-weight:700; }
.context-bar { display:flex; align-items:center; gap:18px; padding:7px 10px; border-left:3px solid #537b72; background:#eff4f1; color:#52665e; font-size:11px; }
.chamber-toggle { margin-left:auto; display:flex; align-items:center; gap:6px; }
.panel { min-width:0; border:1px solid #cfccc1; background:#fff; box-shadow:0 1px 2px #28251c0c; }
.panel-heading { min-height:44px; padding:7px 12px; border-bottom:1px solid #e2dfd5; }
.panel-heading h2 { display:inline; margin:0 8px 0 0; font-size:14px; }
.panel-heading span { color:#777366; font-size:11px; }
.search-toolbar label { display:flex; align-items:center; gap:6px; color:#666255; font-size:11px; }
input,select,textarea,button { font:inherit; }
.search-toolbar input,.edit-dialog input,.edit-dialog select,.edit-dialog textarea { padding:7px 8px; border:1px solid #c9c6bb; border-radius:3px; background:#fff; color:#292820; }
.search-toolbar input { width:170px; }
.toolbar { justify-content:flex-start; padding:8px 10px; border-bottom:1px solid #e2dfd5; }
.toolbar button,.search-toolbar button,.secondary-button,.edit-dialog footer button,.serial-input button { min-height:28px; padding:4px 10px; border:1px solid #aaa69a; border-radius:3px; background:#fff; color:#36352d; cursor:pointer; }
button:disabled { opacity:.45; cursor:not-allowed; }
.toolbar .primary-button,.primary-button { border-color:#365b55; background:#365b55; color:white; }
.toolbar .confirm-button { border-color:#82683c; background:#82683c; color:white; }
.table-scroll { overflow:auto; }
.main-scroll { max-height:min(38vh,360px); }
.detail-scroll { max-height:min(24vh,220px); }
table { width:100%; border-collapse:collapse; font-size:11px; white-space:nowrap; }
th { position:sticky; top:0; z-index:1; padding:7px 8px; background:#eeece4; color:#575449; text-align:left; font-weight:700; }
td { padding:7px 8px; border-top:1px solid #eceae3; color:#39382f; }
tbody tr { cursor:pointer; }
tbody tr:hover { background:#f7f6f1; }
tbody tr.active { background:#e8f1ef; }
tbody tr.selected { box-shadow:inset 3px 0 #6b9188; }
.select-column { width:28px; text-align:center; }
.serial-cell { color:#355c57; font-weight:700; }
.state-pill { padding:3px 7px; border-radius:10px; background:#fff2df; color:#875e20; font-size:10px; }
.description-cell { max-width:260px; overflow:hidden; text-overflow:ellipsis; }
.empty-cell { padding:16px; text-align:center; color:#888477; }
.equipment-state { margin-left:auto; }
.status-message,.dialog-message { margin:8px 10px; padding:7px 10px; border-radius:3px; font-size:11px; }
.status-message.success,.dialog-message.success { background:#edf6ef; color:#2d673e; }
.status-message.error,.dialog-message.error { background:#fff0ed; color:#9a3d2c; }
.dialog-backdrop { position:fixed; inset:0; z-index:30; display:grid; place-items:center; padding:16px; background:#15140f77; }
.edit-dialog { display:flex; flex-direction:column; gap:12px; width:min(700px,100%); max-height:92vh; box-sizing:border-box; overflow:auto; padding:18px; border:1px solid #c9c5b8; border-radius:5px; background:#fcfbf7; box-shadow:0 12px 40px #0003; }
.edit-dialog header { align-items:flex-start; }
.edit-dialog h2 { margin:3px 0 0; font-size:16px; }
.icon-button { border:0; background:transparent; color:#555247; font-size:24px; cursor:pointer; }
.form-grid { display:grid; grid-template-columns:1fr 1fr; gap:10px 14px; }
.edit-dialog label { display:flex; flex-direction:column; gap:5px; color:#575449; font-size:11px; font-weight:700; }
.edit-dialog input,.edit-dialog select,.edit-dialog textarea { width:100%; box-sizing:border-box; font-weight:400; }
.edit-dialog input[readonly],.edit-dialog select:disabled { background:#eeece4; }
.serial-input { gap:5px; }
.serial-input input { min-width:0; flex:1; }
.full-row { grid-column:1 / -1; }
.edit-dialog small { color:#777366; font-weight:400; }
.cause-picker { display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); gap:6px 10px; margin:0; padding:10px; border:1px solid #d6d2c6; }
.cause-picker legend { padding:0 5px; color:#575449; font-size:11px; font-weight:700; }
.cause-picker p { grid-column:1 / -1; margin:2px 0; color:#777366; font-size:11px; }
.cause-option { display:flex !important; flex-direction:row !important; align-items:center; gap:8px !important; padding:6px; border:1px solid #ece9df; background:#fff; cursor:pointer; }
.cause-option input { width:auto; }
.cause-option span { display:flex; flex-direction:column; gap:2px; }
.cause-option b { color:#355c57; }
.form-footnote { margin:0; color:#777366; font-size:10px; }
.edit-dialog footer { justify-content:flex-end; padding-top:7px; border-top:1px solid #e2dfd5; }
@media (max-width:760px) { .breakdown-page { padding:10px; } .page-heading,.panel-heading { align-items:flex-start; flex-direction:column; } .heading-actions,.search-toolbar { width:100%; justify-content:flex-start; flex-wrap:wrap; } .search-toolbar label { width:100%; } .search-toolbar input { flex:1; } .form-grid { grid-template-columns:1fr; } .full-row { grid-column:auto; } .cause-picker { grid-template-columns:1fr; } }
</style>
