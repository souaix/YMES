<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type RepairState = 0 | 1
interface EquipmentRow { equipmentNo: string; equipmentType: string; eqpSerialNo: string; parentEquipmentNo: string; chamber: boolean; equipmentState: string }
interface BreakdownRecord { breakDownSerialNo: string; eqpSerialNo: string; equipmentNo: string; state: RepairState; breakDownStartTime: string; description: string; creator: string; createDate: string; changeStateFlag: 'Y' | 'N'; nextPmDate: string }
interface BreakdownDetail { breakDownSerialNo: string; breakDownNo: string }
interface BreakdownStore { version: number; chamberManage: boolean; nextSerial: number; equipment: EquipmentRow[]; records: BreakdownRecord[]; details: BreakdownDetail[] }
interface RepairBasisRow { repairNo: string; repairName: string; description?: string; issueState: number }
interface BreakdownLog { eqpSerialNo: string; breakDownSerialNo: string; equipmentNo: string; breakDownStartTime: string; breakDownEndTime: string; creator: string; createDate: string; description: string }
interface BreakdownLogDetail extends BreakdownDetail { eqpSerialNo: string }
interface RepairLog { eqpSerialNo: string; breakDownSerialNo: string; equipmentNo: string; breakDownStartTime: string; creator: string; createDate: string; repairSerialNo: string; repairStartTime: string; repairEndTime: string; repairUserNo: string; description: string; equipmentState: string }
interface DevState { version: 1; breakdownLogs: BreakdownLog[]; repairLogs: RepairLog[]; breakdownLogDetails: BreakdownLogDetail[]; repairLogDetails: Array<{ eqpSerialNo: string; repairSerialNo: string; repairNo: string }> }
interface Draft { breakDownSerialNo: string; repairUserNo: string; repairStartTime: string; repairEndTime: string; repairState: '' | '0' | '1'; description: string; repairNos: string[] }
interface CauseBasisRow { breakDownNo: string; breakDownCause: string }

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const breakdownStorageKey = 'ymes.dev.pm.eqp-breakdown-register.v1'
const repairBasisStorageKey = 'ymes.dev.pm.repair-basis.v1'
const breakdownBasisStorageKey = 'ymes.dev.pm.breakdown-basis.v1'
const storageKey = 'ymes.dev.pm.eqp-repair-register.v1'
const currentUser = 'DEV01'
const sampleUsers = ['DEV01', 'DEV02', 'PMUSER']

function localInput(date = new Date()) {
  return new Date(date.getTime() - date.getTimezoneOffset() * 60_000).toISOString().slice(0, 16)
}
function asLocalInput(value: string, fallback = new Date()) {
  if (!value) return localInput(fallback)
  const parsed = new Date(value.includes('T') ? value : value.replace(' ', 'T'))
  return Number.isFinite(parsed.getTime()) ? localInput(parsed) : localInput(fallback)
}
function formatDateTime(value: string) { return value ? value.replace('T', ' ') : '—' }
function seedBreakdownStore(): BreakdownStore {
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
      { breakDownSerialNo: '000000121', eqpSerialNo: 'EQP-0301', equipmentNo: 'WET-301', state: 1, breakDownStartTime: '2026-11-25T16:10', description: '前一筆已確認，等待維修登錄。', creator: 'PMUSER', createDate: '2026-11-25 16:14', changeStateFlag: 'Y', nextPmDate: '2026-11-28' },
    ],
    details: [{ breakDownSerialNo: '000000121', breakDownNo: 'BD-ELEC-01' }],
  }
}
function loadBreakdownStore(): BreakdownStore {
  try {
    const value = JSON.parse(window.localStorage.getItem(breakdownStorageKey) ?? 'null') as BreakdownStore | null
    if (value?.version === 1 && Array.isArray(value.equipment) && Array.isArray(value.records) && Array.isArray(value.details)) return value
  } catch { /* Use the PM404 DEV example when there is no shared browser state. */ }
  return seedBreakdownStore()
}
function loadRepairBasis(): RepairBasisRow[] {
  try {
    const value = JSON.parse(window.localStorage.getItem(repairBasisStorageKey) ?? 'null') as { records?: RepairBasisRow[] } | null
    if (Array.isArray(value?.records)) return value.records.filter((row) => row.issueState === 2)
  } catch { /* Use active PM402 DEV examples. */ }
  return [
    { repairNo: 'RP-SENSOR-03', repairName: '更換感測器', description: '更換後執行訊號校驗。', issueState: 2 },
    { repairNo: 'RP-HYD-08', repairName: '修復油壓泵浦', description: '檢修後確認壓力與洩漏狀態。', issueState: 2 },
  ]
}
function loadBreakdownNames(): Map<string, string> {
  try {
    const value = JSON.parse(window.localStorage.getItem(breakdownBasisStorageKey) ?? 'null') as { records?: CauseBasisRow[] } | null
    if (Array.isArray(value?.records)) return new Map<string, string>(value.records.filter((row) => row.breakDownNo).map((row) => [row.breakDownNo.toUpperCase(), row.breakDownCause] as [string, string]))
  } catch { /* Use DEV display names below. */ }
  return new Map<string, string>([['BD-ELEC-01', '電源異常'], ['BD-HYD-08', '油壓不足'], ['BD-SENSOR-03', '感測器訊號異常']])
}
function loadState(): DevState {
  try {
    const value = JSON.parse(window.localStorage.getItem(storageKey) ?? 'null') as DevState | null
    if (value?.version === 1 && Array.isArray(value.repairLogs) && Array.isArray(value.breakdownLogs) && Array.isArray(value.breakdownLogDetails) && Array.isArray(value.repairLogDetails)) return value
  } catch { /* Keep the DEV preview available without local storage. */ }
  return { version: 1, breakdownLogs: [], repairLogs: [], breakdownLogDetails: [], repairLogDetails: [] }
}

const breakdownStore = ref<BreakdownStore>(loadBreakdownStore())
const state = ref<DevState>(loadState())
const repairBasis = ref<RepairBasisRow[]>(loadRepairBasis())
const breakdownNames = ref<Map<string, string>>(loadBreakdownNames())
const activeSerial = ref('')
const draft = ref<Draft | null>(null)
const userPickerOpen = ref(false)
const message = ref('')
const messageKind = ref<'success' | 'error'>('success')
const visibleRecords = computed(() => breakdownStore.value.records
  .filter((row) => row.state === 1)
  .filter((row) => breakdownStore.value.chamberManage || !breakdownStore.value.equipment.find((eq) => eq.equipmentNo === row.equipmentNo)?.chamber)
  .sort((a, b) => a.breakDownStartTime.localeCompare(b.breakDownStartTime)))
const activeRecord = computed(() => visibleRecords.value.find((row) => row.breakDownSerialNo === activeSerial.value) ?? null)
const activeEquipment = computed(() => activeRecord.value ? breakdownStore.value.equipment.find((row) => row.equipmentNo === activeRecord.value!.equipmentNo) ?? null : null)
const detailRows = computed(() => activeRecord.value ? breakdownStore.value.details.filter((row) => row.breakDownSerialNo === activeRecord.value!.breakDownSerialNo) : [])
const formRecord = computed(() => draft.value ? breakdownStore.value.records.find((row) => row.breakDownSerialNo === draft.value!.breakDownSerialNo) ?? null : null)
const detailSummary = computed(() => detailRows.value.map((row) => `${row.breakDownNo} · ${breakdownNames.value.get(row.breakDownNo.toUpperCase()) ?? '未找到原因主檔'}`).join('、'))
watch(visibleRecords, (rows) => { if (!rows.some((row) => row.breakDownSerialNo === activeSerial.value)) activeSerial.value = rows[0]?.breakDownSerialNo ?? '' }, { immediate: true })
watch(breakdownStore, (value) => { try { window.localStorage.setItem(breakdownStorageKey, JSON.stringify(value)) } catch { /* Shared PM404 mock remains in memory for this view. */ } }, { deep: true })
watch(state, (value) => { try { window.localStorage.setItem(storageKey, JSON.stringify(value)) } catch { /* Keep the DEV preview usable without persistence. */ } }, { deep: true })

function toast(text: string, kind: 'success' | 'error' = 'success') { message.value = text; messageKind.value = kind }
function selectRecord(serial: string) { activeSerial.value = serial }
function openRepair() {
  const row = activeRecord.value
  if (!row) return toast('請先選取等待修復的設備故障單。', 'error')
  const now = localInput()
  draft.value = {
    breakDownSerialNo: row.breakDownSerialNo,
    repairUserNo: currentUser,
    repairStartTime: asLocalInput(row.createDate),
    repairEndTime: now,
    repairState: '',
    description: '',
    repairNos: [],
  }
  message.value = ''
  userPickerOpen.value = false
}
function closeForm() { draft.value = null; userPickerOpen.value = false }
function chooseRepairUser(userNo: string) { if (draft.value) draft.value.repairUserNo = userNo; userPickerOpen.value = false }
function saveRepair() {
  const form = draft.value
  const row = formRecord.value
  if (!form || !row || row.state !== 1) return toast('此故障單已不在待修復狀態。', 'error')
  const userNo = form.repairUserNo.trim().toUpperCase()
  if (!userNo) return toast('RepairUserNo 為必填。', 'error')
  const breakdownStart = new Date(row.breakDownStartTime).getTime()
  const repairStart = new Date(form.repairStartTime).getTime()
  const repairEnd = new Date(form.repairEndTime).getTime()
  if (![breakdownStart, repairStart, repairEnd].every(Number.isFinite)) return toast('故障與修復時間必須是有效日期。', 'error')
  if (repairStart <= breakdownStart) return toast('RepairStartTime 必須晚於 BreakDownStartTime。', 'error')
  if (repairStart >= repairEnd) return toast('RepairStartTime 必須早於 RepairEndTime。', 'error')
  if (form.repairNos.length === 0) return toast('請至少勾選一項維修項目。', 'error')
  const equipment = breakdownStore.value.equipment.find((item) => item.equipmentNo === row.equipmentNo)
  if (!equipment) return toast('找不到設備假資料，無法完成狀態模擬。', 'error')

  const repairStartAt = form.repairStartTime
  const repairEndAt = form.repairEndTime
  const effectiveState = row.changeStateFlag === 'N' ? 'NOCHANGE' : form.repairState === '1' ? 'RUN' : 'IDLE'
  const breakdownLog: BreakdownLog = {
    eqpSerialNo: row.eqpSerialNo,
    breakDownSerialNo: row.breakDownSerialNo,
    equipmentNo: row.equipmentNo,
    breakDownStartTime: row.breakDownStartTime,
    breakDownEndTime: repairStartAt,
    creator: row.creator,
    createDate: row.createDate,
    description: row.description,
  }
  state.value.breakdownLogs.push(breakdownLog)
  state.value.breakdownLogDetails.push(...detailRows.value.map((detail) => ({ eqpSerialNo: row.eqpSerialNo, ...detail })))
  state.value.repairLogs.push({
    eqpSerialNo: row.eqpSerialNo,
    breakDownSerialNo: row.breakDownSerialNo,
    equipmentNo: row.equipmentNo,
    breakDownStartTime: row.breakDownStartTime,
    creator: row.creator,
    createDate: row.createDate,
    repairSerialNo: row.breakDownSerialNo,
    repairStartTime: repairStartAt,
    repairEndTime: repairEndAt,
    repairUserNo: userNo,
    description: form.description,
    equipmentState: effectiveState,
  })
  state.value.repairLogDetails.push(...form.repairNos.map((repairNo) => ({ eqpSerialNo: row.eqpSerialNo, repairSerialNo: row.breakDownSerialNo, repairNo })))

  if (row.changeStateFlag === 'Y') {
    equipment.equipmentState = effectiveState
    if (breakdownStore.value.chamberManage) {
      for (const child of breakdownStore.value.equipment.filter((item) => item.parentEquipmentNo === equipment.equipmentNo)) child.equipmentState = effectiveState
    }
  }
  breakdownStore.value.records = breakdownStore.value.records.filter((item) => item.breakDownSerialNo !== row.breakDownSerialNo)
  breakdownStore.value.details = breakdownStore.value.details.filter((item) => item.breakDownSerialNo !== row.breakDownSerialNo)
  const stateText = effectiveState === 'NOCHANGE' ? '設備狀態維持不變' : `設備狀態：${effectiveState}`
  activeSerial.value = ''
  closeForm()
  toast(`DEV 已模擬修復登錄；故障單轉入歷史，${stateText}。不會呼叫 EMS 或寄送郵件。`)
}
</script>

<template>
  <section class="repair-page" aria-label="設備修復登錄 DEV 預覽">
    <header class="page-heading">
      <div><div class="eyebrow">PM405 · 設備維修</div><h1>設備修復登錄</h1><p>清單只顯示已確認、等待修復（State=1）的故障單。</p></div>
      <div class="heading-actions"><span class="dev-badge">DEV 假資料</span><button type="button" class="secondary-button" @click="emit('close')">關閉</button></div>
    </header>
    <div class="context-bar"><span>{{ visibleRecords.length }} 筆待修復</span><span>資料來自 PM404 DEV 故障登錄</span><span v-if="activeEquipment">目前設備狀態：{{ activeEquipment.equipmentState }}</span></div>
    <section class="panel main-panel">
      <div class="panel-heading"><div><h2>Equipment Breakdown State</h2><span>選取一筆故障單後執行維修登錄</span></div><button type="button" class="primary-button" :disabled="!activeRecord" @click="openRepair">Repair Register</button></div>
      <div class="table-scroll main-scroll"><table>
        <thead><tr><th>BreakDownSerialNo</th><th>EquipmentNo</th><th>State</th><th>BreakDownStartTime</th><th>Description</th><th>Creator</th><th>CreateDate</th></tr></thead>
        <tbody>
          <tr v-for="row in visibleRecords" :key="row.breakDownSerialNo" :class="{ active: row.breakDownSerialNo === activeSerial }" @click="selectRecord(row.breakDownSerialNo)">
            <td class="serial-cell">{{ row.breakDownSerialNo }}</td><td>{{ row.equipmentNo }}</td><td><span class="state-pill">Waiting · 1</span></td><td>{{ formatDateTime(row.breakDownStartTime) }}</td><td class="description-cell">{{ row.description || '—' }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td>
          </tr>
          <tr v-if="visibleRecords.length === 0"><td colspan="7" class="empty-cell">目前沒有等待修復的設備故障單。</td></tr>
        </tbody>
      </table></div>
      <p v-if="message" class="status-message" :class="messageKind" role="status">{{ message }}</p>
    </section>
    <section class="panel detail-panel">
      <div class="panel-heading"><div><h2>Break Down Cause Detail</h2><span>{{ activeRecord?.breakDownSerialNo ?? '請選取等待修復故障單' }} · {{ detailRows.length }} 項</span></div><span class="cause-summary">{{ detailSummary || '—' }}</span></div>
      <div class="table-scroll detail-scroll"><table><thead><tr><th>BreakDownSerialNo</th><th>BreakDownNo</th><th>BREAKDOWNCAUSE</th></tr></thead><tbody>
        <tr v-for="detail in detailRows" :key="`${detail.breakDownSerialNo}-${detail.breakDownNo}`"><td>{{ detail.breakDownSerialNo }}</td><td class="serial-cell">{{ detail.breakDownNo }}</td><td>{{ breakdownNames.get(detail.breakDownNo.toUpperCase()) || '（故障原因主檔不存在）' }}</td></tr>
        <tr v-if="!detailRows.length"><td colspan="3" class="empty-cell">選取故障單後顯示其原因明細。</td></tr>
      </tbody></table></div>
    </section>

    <div v-if="draft && formRecord" class="dialog-backdrop" @click.self="closeForm">
      <section class="edit-dialog" role="dialog" aria-modal="true" aria-label="設備修復登錄">
        <header><div><div class="eyebrow">REPAIR REGISTER</div><h2>設備修復登錄 · {{ formRecord.breakDownSerialNo }}</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="closeForm">×</button></header>
        <p v-if="message" class="dialog-message" :class="messageKind" role="status">{{ message }}</p>
        <div class="form-grid">
          <label>EquipmentNo<input :value="formRecord.equipmentNo" readonly></label>
          <label>BreakDownStartTime<input :value="asLocalInput(formRecord.breakDownStartTime).replace('T', ' ')" readonly></label>
          <label>RepairUserNo<div class="input-actions"><input v-model="draft.repairUserNo" autocomplete="off"><button type="button" @click="userPickerOpen = !userPickerOpen">選擇人員</button></div><select v-if="userPickerOpen" :value="draft.repairUserNo" @change="chooseRepairUser(($event.target as HTMLSelectElement).value)"><option v-for="user in sampleUsers" :key="user" :value="user">{{ user }}</option></select></label>
          <label>RepairStartTime<input v-model="draft.repairStartTime" type="datetime-local"></label>
          <label>RepairEndTime<input :value="draft.repairEndTime.replace('T', ' ')" readonly></label>
          <fieldset class="state-choice"><legend>修復後設備狀態</legend><label><input v-model="draft.repairState" type="radio" value="0" :disabled="formRecord.changeStateFlag === 'N'">Idle</label><label><input v-model="draft.repairState" type="radio" value="1" :disabled="formRecord.changeStateFlag === 'N'">Continue to Produce</label><small v-if="formRecord.changeStateFlag === 'N'">此故障單設定 NOCHANGED，修復登錄不變更設備狀態。</small><small v-else>舊 Service 未收到狀態時預設為 0（Idle）。</small></fieldset>
          <fieldset class="repair-picker"><legend>Repair Item（至少勾選一項）</legend><p v-if="repairBasis.length === 0">目前沒有已生效的 PM402 維修項目。</p><label v-for="item in repairBasis" :key="item.repairNo" class="repair-option"><input v-model="draft.repairNos" type="checkbox" :value="item.repairNo"><span><b>{{ item.repairNo }}</b><small>{{ item.repairName }}</small></span></label></fieldset>
          <label class="full-row">Description<textarea v-model="draft.description" rows="3"></textarea></label>
        </div>
        <p class="form-footnote">儲存會建立故障／維修歷史、移除待修復登錄，並模擬設備狀態；DEV 不連 EMS、不寄信。</p>
        <footer><button type="button" @click="closeForm">取消</button><button type="button" class="primary-button" @click="saveRepair">確定</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.repair-page { display:flex; flex-direction:column; gap:12px; min-height:0; height:100%; box-sizing:border-box; padding:16px; overflow:auto; background:#f6f4ed; color:#26251f; font:inherit; }
.page-heading,.panel-heading,.heading-actions,.edit-dialog header,.edit-dialog footer,.input-actions { display:flex; align-items:center; justify-content:space-between; gap:10px; }
.page-heading h1 { margin:2px 0 3px; font-size:20px; font-weight:700; }
.page-heading p { margin:0; color:#716e63; font-size:12px; }
.eyebrow { color:#777366; font-size:10px; letter-spacing:.1em; font-weight:700; }
.dev-badge { padding:4px 8px; border:1px solid #c3a56c; border-radius:12px; background:#fff8e8; color:#76571d; font-size:11px; font-weight:700; }
.context-bar { display:flex; flex-wrap:wrap; align-items:center; gap:18px; padding:7px 10px; border-left:3px solid #537b72; background:#eff4f1; color:#52665e; font-size:11px; }
.panel { min-width:0; border:1px solid #cfccc1; background:#fff; box-shadow:0 1px 2px #28251c0c; }
.panel-heading { min-height:44px; padding:7px 12px; border-bottom:1px solid #e2dfd5; }
.panel-heading h2 { display:inline; margin:0 8px 0 0; font-size:14px; }
.panel-heading span { color:#777366; font-size:11px; }
.cause-summary { margin-left:auto; max-width:56%; overflow:hidden; text-overflow:ellipsis; white-space:nowrap; }
.primary-button,.secondary-button,.edit-dialog footer button,.input-actions button { min-height:28px; padding:4px 10px; border:1px solid #365b55; border-radius:3px; background:#365b55; color:white; font:inherit; cursor:pointer; }
.secondary-button,.edit-dialog footer button { border-color:#aaa69a; background:#fff; color:#36352d; }
button:disabled { opacity:.45; cursor:not-allowed; }
.table-scroll { overflow:auto; }
.main-scroll { max-height:min(48vh,430px); }
.detail-scroll { max-height:min(24vh,220px); }
table { width:100%; border-collapse:collapse; font-size:11px; white-space:nowrap; }
th { position:sticky; top:0; z-index:1; padding:7px 8px; background:#eeece4; color:#575449; text-align:left; font-weight:700; }
td { padding:7px 8px; border-top:1px solid #eceae3; color:#39382f; }
tbody tr { cursor:pointer; }
tbody tr:hover { background:#f7f6f1; }
tbody tr.active { background:#e8f1ef; }
.serial-cell { color:#355c57; font-weight:700; }
.state-pill { padding:3px 7px; border-radius:10px; background:#e7f1ed; color:#41645a; font-size:10px; }
.description-cell { max-width:300px; overflow:hidden; text-overflow:ellipsis; }
.empty-cell { padding:16px; text-align:center; color:#888477; }
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
.edit-dialog input,.edit-dialog select,.edit-dialog textarea { width:100%; box-sizing:border-box; padding:7px 8px; border:1px solid #c9c6bb; border-radius:3px; background:#fff; color:#292820; font:inherit; font-weight:400; }
.edit-dialog input[readonly] { background:#eeece4; }
.input-actions { gap:5px; }
.input-actions input { min-width:0; flex:1; }
.input-actions button { flex:0 0 auto; }
.state-choice,.repair-picker { display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); gap:6px 10px; margin:0; padding:10px; border:1px solid #d6d2c6; }
.state-choice { grid-column:1 / -1; }
.state-choice legend,.repair-picker legend { padding:0 5px; color:#575449; font-size:11px; font-weight:700; }
.state-choice label { flex-direction:row; align-items:center; }
.state-choice input,.repair-option input { width:auto; }
.state-choice small,.repair-picker p { grid-column:1 / -1; color:#777366; font-size:10px; font-weight:400; }
.repair-picker { grid-column:1 / -1; }
.repair-picker p { margin:0; }
.repair-option { display:flex !important; flex-direction:row !important; align-items:center; gap:8px !important; padding:6px; border:1px solid #ece9df; background:#fff; cursor:pointer; }
.repair-option span { display:flex; flex-direction:column; gap:2px; }
.repair-option b { color:#355c57; }
.repair-option small { color:#777366; font-weight:400; }
.full-row { grid-column:1 / -1; }
.form-footnote { margin:0; color:#777366; font-size:10px; }
.edit-dialog footer { justify-content:flex-end; padding-top:7px; border-top:1px solid #e2dfd5; }
@media (max-width:760px) { .repair-page { padding:10px; } .page-heading,.panel-heading { align-items:flex-start; flex-direction:column; } .heading-actions { width:100%; justify-content:flex-start; } .cause-summary { max-width:100%; margin-left:0; } .form-grid,.state-choice,.repair-picker { grid-template-columns:1fr; } .state-choice,.repair-picker,.full-row { grid-column:auto; } }
</style>
