<script setup lang="ts">
import { computed, ref } from 'vue'

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)

type ControlType = 'PM Work Order' | 'WIP Tracking' | 'EQP Break Down'
type SaveMode = 'apply' | 'confirm'

interface ControlDefinition {
  controlNo: string
  label: string
}

interface ControlGroup {
  controlType: ControlType
  label: string
  controls: ControlDefinition[]
}

interface StateControlRow {
  controlType: ControlType
  controlNo: string
  equipmentState: number
  stateName: string
}

interface EquipmentStateOption {
  equipmentState: number
  stateName: string
  stateType: string
  utilization: string
}

const controlGroups: ControlGroup[] = [
  {
    controlType: 'PM Work Order',
    label: 'PM Work Order',
    controls: [
      { controlNo: 'PMWoReleased', label: 'PM WO Release State' },
      { controlNo: 'PMWoFinished', label: 'PM WO Finish State' },
    ],
  },
  {
    controlType: 'WIP Tracking',
    label: 'WIP Tracking',
    controls: [
      { controlNo: 'CheckInConfirm', label: 'Check In Confirm' },
      { controlNo: 'CheckOutRelease', label: 'Check Out Release' },
    ],
  },
  {
    controlType: 'EQP Break Down',
    label: 'EQP BreakDown And Repair',
    controls: [
      { controlNo: 'BreakDown', label: 'Break Down' },
      { controlNo: 'Idle', label: 'Idle' },
      { controlNo: 'ContinueToProduction', label: 'Continue to Produce' },
    ],
  },
]

const initialControls: StateControlRow[] = [
  { controlType: 'PM Work Order', controlNo: 'PMWoReleased', equipmentState: 3, stateName: 'DEV 假資料：保養中' },
  { controlType: 'PM Work Order', controlNo: 'PMWoFinished', equipmentState: 0, stateName: 'DEV 假資料：閒置' },
  { controlType: 'WIP Tracking', controlNo: 'CheckInConfirm', equipmentState: 1, stateName: 'DEV 假資料：生產中' },
  { controlType: 'WIP Tracking', controlNo: 'CheckOutRelease', equipmentState: 0, stateName: 'DEV 假資料：閒置' },
  { controlType: 'EQP Break Down', controlNo: 'BreakDown', equipmentState: 2, stateName: 'DEV 假資料：故障' },
  { controlType: 'EQP Break Down', controlNo: 'Idle', equipmentState: 0, stateName: 'DEV 假資料：閒置' },
  { controlType: 'EQP Break Down', controlNo: 'ContinueToProduction', equipmentState: 1, stateName: 'DEV 假資料：生產中' },
]

const equipmentStates = ref<EquipmentStateOption[]>(dev.value ? [
  { equipmentState: 0, stateName: 'DEV 假資料：閒置', stateType: '系統', utilization: '非稼動' },
  { equipmentState: 1, stateName: 'DEV 假資料：生產中', stateType: '系統', utilization: '稼動' },
  { equipmentState: 2, stateName: 'DEV 假資料：故障', stateType: '使用者定義', utilization: '非稼動' },
  { equipmentState: 3, stateName: 'DEV 假資料：保養中', stateType: '使用者定義', utilization: '非稼動' },
  { equipmentState: 4, stateName: 'DEV 假資料：待確認', stateType: '使用者定義', utilization: '非稼動' },
  { equipmentState: 5, stateName: 'DEV 假資料：保養完成', stateType: '使用者定義', utilization: '稼動' },
] : [])

const savedControls = ref<StateControlRow[]>(dev.value ? initialControls.map(row => ({ ...row })) : [])
const workingControls = ref<StateControlRow[]>(dev.value ? initialControls.map(row => ({ ...row })) : [])
const lastAppliedRows = ref<StateControlRow[]>([])
const activeGroup = ref<ControlType>('PM Work Order')
const activeControl = ref<{ controlType: ControlType; controlNo: string } | null>(null)
const pickerOpen = ref(false)
const pickerSearch = ref('')
const message = ref('')
const messageKind = ref<'error' | 'success' | 'info'>('info')
const successDialog = ref(false)

const activeGroupDefinition = computed(() => controlGroups.find(group => group.controlType === activeGroup.value) ?? controlGroups[0])
const activeControls = computed(() => activeGroupDefinition.value.controls.map(definition => ({
  ...definition,
  row: workingControls.value.find(row => row.controlType === activeGroupDefinition.value.controlType && row.controlNo === definition.controlNo),
})))
const changedGroups = computed(() => controlGroups
  .filter(group => JSON.stringify(rowsForGroup(savedControls.value, group.controlType)) !== JSON.stringify(rowsForGroup(workingControls.value, group.controlType)))
  .map(group => group.controlType))
const matchingStates = computed(() => {
  const query = pickerSearch.value.trim().toLocaleLowerCase()
  return equipmentStates.value.filter(row => !query
    || String(row.equipmentState).includes(query)
    || row.stateName.toLocaleLowerCase().includes(query)
    || row.stateType.toLocaleLowerCase().includes(query))
})
const activeControlLabel = computed(() => {
  if (!activeControl.value) return ''
  return controlGroups.find(group => group.controlType === activeControl.value?.controlType)
    ?.controls.find(control => control.controlNo === activeControl.value?.controlNo)?.label ?? activeControl.value.controlNo
})

function rowsForGroup(rows: StateControlRow[], controlType: ControlType): StateControlRow[] {
  return rows.filter(row => row.controlType === controlType).slice().sort((left, right) => left.controlNo.localeCompare(right.controlNo))
}

function setMessage(text: string, kind: 'error' | 'success' | 'info' = 'info'): void {
  message.value = text
  messageKind.value = kind
}

function openStatePicker(controlNo: string): void {
  if (!dev.value) return
  activeControl.value = { controlType: activeGroupDefinition.value.controlType, controlNo }
  pickerSearch.value = ''
  pickerOpen.value = true
}

function chooseState(state: EquipmentStateOption): void {
  if (!dev.value || !activeControl.value) return
  const key = activeControl.value
  workingControls.value = workingControls.value.map(row => row.controlType === key.controlType && row.controlNo === key.controlNo
    ? { ...row, equipmentState: state.equipmentState, stateName: state.stateName }
    : row)
  pickerOpen.value = false
  setMessage(`已將 ${activeControlLabel.value} 設為 ${state.equipmentState} ${state.stateName}。尚未套用。`)
}

function validateAllControls(): boolean {
  for (const group of controlGroups) {
    for (const definition of group.controls) {
      const row = workingControls.value.find(item => item.controlType === group.controlType && item.controlNo === definition.controlNo)
      const selectedState = equipmentStates.value.find(item => item.equipmentState === row?.equipmentState && item.stateName === row?.stateName)
      if (!row || !selectedState || !row.stateName.trim()) {
        activeGroup.value = group.controlType
        setMessage(`${group.label} 的「${definition.label}」尚未選擇有效設備狀態。`, 'error')
        return false
      }
    }
  }
  return true
}

function applyChanges(mode: SaveMode): void {
  if (!dev.value) return
  if (!validateAllControls()) return
  const changedGroupNames = changedGroups.value
  const changedGroupSet = new Set(changedGroupNames)
  const rowsToWrite = workingControls.value.filter(row => changedGroupSet.has(row.controlType)).map(row => ({ ...row }))
  lastAppliedRows.value = rowsToWrite
  savedControls.value = workingControls.value.map(row => ({ ...row }))
  const result = rowsToWrite.length
    ? `DEV 已模擬 ${rowsToWrite.length} 筆設定寫入（${changedGroupNames.length} 個設定群組）；沒有呼叫 API。`
    : '目前沒有待套用的設定；DEV 沒有呼叫 API。'
  setMessage(result, 'success')
  if (mode === 'confirm') successDialog.value = true
}

function finishConfirm(): void {
  successDialog.value = false
  emit('close')
}
</script>

<template>
  <section class="ems-state-control" aria-label="設備狀態控制設定">
    <template v-if="dev">
      <header class="page-heading">
        <div><h1>EQP State Control Set</h1><p>設定 PM Work Order、WIP Tracking 與設備故障／修復狀態對應。</p></div>
        <span class="fixture-badge">DEV 假資料</span>
      </header>

      <div class="workspace">
        <nav class="group-tabs" role="tablist" aria-label="設備狀態控制類別">
          <button
            v-for="group in controlGroups"
            :key="group.controlType"
            type="button"
            role="tab"
            :aria-selected="activeGroup === group.controlType"
            :class="{ active: activeGroup === group.controlType, changed: changedGroups.includes(group.controlType) }"
            @click="activeGroup = group.controlType"
          >
            {{ group.label }}<span v-if="changedGroups.includes(group.controlType)" aria-label="未套用變更"> •</span>
          </button>
        </nav>

        <section class="settings-panel" role="tabpanel" :aria-label="activeGroupDefinition.label">
          <div class="panel-heading">
            <div><h2>{{ activeGroupDefinition.label }}</h2><p>狀態與名稱由設備狀態主檔選取，欄位本身唯讀。</p></div>
            <span v-if="changedGroups.includes(activeGroupDefinition.controlType)" class="dirty-badge">有未套用變更</span>
          </div>
          <div class="settings-table">
            <div class="table-head"><span>控制項</span><span>EquipmentState</span><span>StateName</span><span>選擇</span></div>
            <div v-for="control in activeControls" :key="control.controlNo" class="setting-row">
              <strong>{{ control.label }}</strong>
              <input :value="control.row?.equipmentState ?? ''" readonly :aria-label="control.label + ' EquipmentState'">
              <input :value="control.row?.stateName ?? ''" readonly :aria-label="control.label + ' StateName'">
              <button type="button" @click="openStatePicker(control.controlNo)">...</button>
            </div>
          </div>
        </section>
      </div>

      <section v-if="lastAppliedRows.length" class="last-write" aria-label="DEV 最近套用設定">
        <h2>DEV 最近套用</h2>
        <div class="table-scroll"><table>
          <thead><tr><th>ControlType</th><th>ControlNo</th><th>EquipmentState</th><th>StateName</th></tr></thead>
          <tbody><tr v-for="row in lastAppliedRows" :key="row.controlType + ':' + row.controlNo">
            <td>{{ row.controlType }}</td><td>{{ row.controlNo }}</td><td>{{ row.equipmentState }}</td><td>{{ row.stateName }}</td>
          </tr></tbody>
        </table></div>
      </section>

      <div class="actions">
        <p class="fixture-note">每個設定群組會一起送出完整控制項；設備狀態及對應值均為 DEV 示意。</p>
        <button type="button" :disabled="changedGroups.length === 0" @click="applyChanges('apply')">Apply</button>
        <button type="button" class="primary" @click="applyChanges('confirm')">Confirm</button>
        <button type="button" @click="emit('close')">Close</button>
      </div>
      <p v-if="message" class="message" :class="messageKind" :role="messageKind === 'error' ? 'alert' : 'status'">{{ message }}</p>

      <div v-if="pickerOpen" class="dialog-backdrop" @click.self="pickerOpen = false">
        <section class="picker-dialog" role="dialog" aria-modal="true" aria-labelledby="state-picker-title">
          <header><div><h2 id="state-picker-title">選擇設備狀態</h2><p>{{ activeControlLabel }}</p></div><button type="button" aria-label="關閉" @click="pickerOpen = false">×</button></header>
          <label class="search-field">查找狀態<input v-model="pickerSearch" type="search" placeholder="狀態代碼或名稱"></label>
          <div class="table-scroll"><table>
            <thead><tr><th>EquipmentState</th><th>StateName</th><th>Type</th><th>稼動</th><th></th></tr></thead>
            <tbody><tr v-for="state in matchingStates" :key="state.equipmentState">
              <td>{{ state.equipmentState }}</td><td>{{ state.stateName }}</td><td>{{ state.stateType }}</td><td>{{ state.utilization }}</td>
              <td><button type="button" @click="chooseState(state)">選取</button></td>
            </tr>
            <tr v-if="matchingStates.length === 0"><td colspan="5">沒有符合的 DEV 狀態。</td></tr></tbody>
          </table></div>
        </section>
      </div>

      <div v-if="successDialog" class="dialog-backdrop">
        <section class="success-dialog" role="alertdialog" aria-modal="true" aria-labelledby="save-success-title">
          <h2 id="save-success-title">EQP State Control Setting Successfully!</h2>
          <p>{{ message }}</p>
          <button type="button" class="primary" @click="finishConfirm">確定並關閉</button>
        </section>
      </div>
    </template>
    <template v-else>
      <p role="status">正式環境尚未開放此功能。</p><button type="button" @click="emit('close')">Close (X)</button>
    </template>
  </section>
</template>

<style scoped>
.ems-state-control { display: grid; align-content: start; gap: 12px; min-height: 100%; padding: 14px; box-sizing: border-box; color: var(--ink, #26251f); }
.page-heading, .panel-heading { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.page-heading h1, .panel-heading h2, .last-write h2 { margin: 0; font-size: 17px; }
.page-heading p, .panel-heading p { margin: 4px 0 0; color: var(--muted, #777366); font-size: 12px; }
.fixture-badge, .dirty-badge { padding: 4px 8px; border: 1px solid #b49d20; border-radius: 3px; background: #fff4b4; color: #62530b; font-size: 12px; white-space: nowrap; }
.workspace { display: grid; grid-template-columns: 210px minmax(0, 1fr); min-height: 300px; border: 1px solid var(--line, #cfcbbd); background: #fffdf6; }
.group-tabs { display: flex; flex-direction: column; gap: 1px; padding: 8px; border-right: 1px solid var(--line, #cfcbbd); background: #f2f0e7; }
.group-tabs button { min-height: 38px; padding: 7px 10px; border: 1px solid transparent; background: transparent; text-align: left; }
.group-tabs button.active { border-color: #c9ba6d; background: #fff8d8; }
.group-tabs button.changed { font-weight: 700; }
.settings-panel { min-width: 0; padding: 14px; }
.panel-heading { margin-bottom: 14px; }
.settings-table { display: grid; border: 1px solid #d8d4c7; }
.table-head, .setting-row { display: grid; grid-template-columns: minmax(180px, 1.2fr) minmax(120px, .65fr) minmax(220px, 1.4fr) 55px; align-items: center; gap: 8px; padding: 9px 10px; }
.table-head { background: #efede4; font-weight: 700; }
.setting-row + .setting-row { border-top: 1px solid #e4e0d5; }
.setting-row strong { font-weight: 600; }
.setting-row input { box-sizing: border-box; width: 100%; min-height: 30px; padding: 4px 7px; border: 1px solid var(--line, #cfcbbd); background: #f5f4ef; color: inherit; font: inherit; }
button, input { font: inherit; }
button { min-height: 30px; padding: 4px 10px; border: 1px solid #5a5547; border-radius: 3px; background: #f8f5e9; color: inherit; cursor: pointer; }
button:hover:not(:disabled) { background: #f3e69a; }
button:disabled { cursor: not-allowed; opacity: .55; }
.actions { display: flex; justify-content: flex-end; align-items: center; gap: 8px; }
.actions .primary, .success-dialog .primary { background: #3b3930; color: white; }
.fixture-note { margin: 0 auto 0 0; color: var(--muted, #777366); font-size: 12px; }
.message { margin: 0; padding: 8px 10px; border: 1px solid #cfcbbd; background: #fffdf6; }
.message.error { border-color: #d7a3a3; background: #fff1f1; color: #932525; }
.message.success { border-color: #a5c9a4; background: #eff8ed; color: #205d25; }
.last-write { border: 1px solid var(--line, #cfcbbd); background: white; }
.last-write h2 { padding: 8px 10px; border-bottom: 1px solid var(--line, #cfcbbd); background: #f5f0d6; }
.table-scroll { max-width: 100%; overflow: auto; }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
th, td { padding: 6px 8px; border-bottom: 1px solid #e4e0d5; text-align: left; white-space: nowrap; }
th { position: sticky; top: 0; background: #efede4; }
.dialog-backdrop { position: fixed; inset: 0; z-index: 50; display: grid; place-items: center; padding: 18px; background: #0008; }
.picker-dialog, .success-dialog { display: grid; gap: 12px; width: min(760px, 96vw); max-height: 85vh; padding: 14px; background: #fffdf6; box-shadow: 0 12px 36px #0004; }
.picker-dialog header { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.picker-dialog h2 { margin: 0; font-size: 16px; }
.picker-dialog header p { margin: 4px 0 0; color: var(--muted, #777366); }
.picker-dialog header button { min-width: 34px; font-size: 18px; }
.picker-dialog .table-scroll { max-height: 60vh; }
.search-field { display: flex; align-items: center; gap: 10px; }
.search-field input { flex: 1; min-width: 0; min-height: 32px; padding: 4px 8px; border: 1px solid var(--line, #cfcbbd); }
.success-dialog { width: min(460px, 96vw); }
.success-dialog h2 { margin: 0; font-size: 17px; }
.success-dialog p { margin: 0; overflow-wrap: anywhere; }
.success-dialog button { justify-self: end; }
@media (max-width: 760px) {
  .workspace { grid-template-columns: 1fr; }
  .group-tabs { flex-direction: row; overflow-x: auto; border-right: 0; border-bottom: 1px solid var(--line, #cfcbbd); }
  .group-tabs button { flex: 0 0 auto; }
  .table-head { display: none; }
  .setting-row { grid-template-columns: 1fr 1fr; }
  .setting-row strong { grid-column: 1 / -1; }
  .setting-row button { grid-column: 2; }
  .actions { flex-wrap: wrap; }
  .fixture-note { flex-basis: 100%; }
}
</style>
