<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type IssueState = 0 | 1 | 2 | -1
type MainMode = 'add' | 'edit' | 'copy'
type StepMode = 'add' | 'edit' | 'copy'
interface SopStep { pmItemNo: string; serialNo: number; creator: string; createDate: string; reviser: string; reviseDate: string; dataStamp: number }
interface SopRecord {
  sopNo: string; sopName: string; sopFilePath: string; estDoneTime: number; description: string
  creator: string; createDate: string; reviser: string; reviseDate: string; issueState: IssueState; dataStamp: number; steps: SopStep[]
}
interface DevState { version: 1; approvalWorkflowConfigured: boolean; records: SopRecord[] }
interface MainDraft { sopNo: string; sopName: string; sopFilePath: string; estDoneTime: number | string; description: string }
interface StepDraft { pmItemNo: string; serialNo: number | string }
interface MaintenanceItem { pmItemNo: string; pmItemName: string }

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.pm.sop-basis.v1'
const currentUser = 'DEV01'
const issueTabs: Array<{ value: IssueState; label: string }> = [
  { value: 0, label: '未凍結' }, { value: 1, label: '簽核中' }, { value: 2, label: '已生效' }, { value: -1, label: '未使用' },
]
const samplePaths = [
  '\\\\MES-FILE\\PM\\SOP\\PM-CLEAN-01.doc',
  '\\\\MES-FILE\\PM\\SOP\\PM-CAL-02.txt',
  '\\\\MES-FILE\\PM\\SOP\\PM-LUBE-03.doc',
]
const fallbackItems: MaintenanceItem[] = [
  { pmItemNo: 'PM-CLEAN-01', pmItemName: '腔體定期清潔' },
  { pmItemNo: 'PM-LUBE-03', pmItemName: '主軸潤滑保養' },
  { pmItemNo: 'PM-CAL-02', pmItemName: '感測器校正' },
  { pmItemNo: 'PM-PUMP-01', pmItemName: '冷卻泵更換' },
]

function seedState(): DevState {
  const created = '2026-11-18 09:00'
  return {
    version: 1,
    approvalWorkflowConfigured: false,
    records: [
      { sopNo: 'SOP-CLEAN-01', sopName: '腔體定期清潔程序', sopFilePath: samplePaths[0], estDoneTime: 45, description: '停機、清潔腔體與載台，確認無殘留物。', creator: 'PMUSER', createDate: created, reviser: 'PMUSER', reviseDate: '2026-11-20 10:30', issueState: 0, dataStamp: 2,
        steps: [
          { pmItemNo: 'PM-CLEAN-01', serialNo: 1, creator: 'PMUSER', createDate: created, reviser: '', reviseDate: '', dataStamp: 0 },
          { pmItemNo: 'PM-LUBE-03', serialNo: 2, creator: 'PMUSER', createDate: created, reviser: '', reviseDate: '', dataStamp: 0 },
        ] },
      { sopNo: 'SOP-CAL-02', sopName: '感測器校正程序', sopFilePath: samplePaths[1], estDoneTime: 30, description: '以標準器確認輸出值並記錄結果。', creator: 'DEV02', createDate: '2026-11-19 13:15', reviser: '', reviseDate: '', issueState: 1, dataStamp: 1,
        steps: [{ pmItemNo: 'PM-CAL-02', serialNo: 1, creator: 'DEV02', createDate: '2026-11-19 13:18', reviser: '', reviseDate: '', dataStamp: 0 }] },
      { sopNo: 'SOP-LUBE-03', sopName: '主軸潤滑保養程序', sopFilePath: samplePaths[2], estDoneTime: 25, description: '依週期補充指定油脂並記錄用量。', creator: 'PMUSER', createDate: '2026-10-01 08:00', reviser: 'PMUSER', reviseDate: '2026-10-03 08:00', issueState: 2, dataStamp: 2,
        steps: [{ pmItemNo: 'PM-LUBE-03', serialNo: 1, creator: 'PMUSER', createDate: '2026-10-01 08:05', reviser: '', reviseDate: '', dataStamp: 0 }] },
      { sopNo: 'SOP-OLD-00', sopName: '舊式風扇檢查程序', sopFilePath: '', estDoneTime: 15, description: '停用範例。', creator: 'PMUSER', createDate: '2026-08-01 10:00', reviser: 'PMUSER', reviseDate: '2026-10-01 10:00', issueState: -1, dataStamp: 3, steps: [] },
    ],
  }
}
function loadState(): DevState {
  try {
    const parsed = JSON.parse(window.localStorage.getItem(storageKey) ?? 'null') as DevState | null
    if (parsed?.version === 1 && Array.isArray(parsed.records)) return parsed
  } catch { /* Fall back to DEV fixtures when local storage is unavailable. */ }
  return seedState()
}
function loadMaintenanceItems(): MaintenanceItem[] {
  try {
    const parsed = JSON.parse(window.localStorage.getItem('ymes.dev.pm.maintenance-item.v1') ?? 'null') as { records?: Array<Record<string, unknown>> } | null
    if (Array.isArray(parsed?.records)) return parsed.records.map((row) => ({ pmItemNo: String(row.pmItemNo ?? ''), pmItemName: String(row.pmItemName ?? '') })).filter((row) => row.pmItemNo)
  } catch { /* Use local fixtures when the PM108 DEV state is unavailable. */ }
  return fallbackItems
}

const state = ref<DevState>(loadState())
const issueState = ref<IssueState>(0)
const searchNo = ref('')
const activeNo = ref('')
const selectedNos = ref<string[]>([])
const selectedStepNo = ref('')
const mainMode = ref<MainMode | null>(null)
const mainDraft = ref<MainDraft | null>(null)
const stepMode = ref<StepMode | null>(null)
const stepDraft = ref<StepDraft | null>(null)
const detailOpen = ref(false)
const filePickerOpen = ref(false)
const appliedMainSignature = ref('')
const appliedStepSignature = ref('')
const message = ref('')
const messageKind = ref<'success' | 'error'>('success')
const nowLabel = computed(() => new Date().toLocaleString('zh-TW', { hour12: false }))
const maintenanceItems = computed(loadMaintenanceItems)
const visibleRows = computed(() => state.value.records
  .filter((record) => record.issueState === issueState.value)
  .filter((record) => !searchNo.value.trim() || record.sopNo.toLocaleLowerCase().includes(searchNo.value.trim().toLocaleLowerCase()))
  .sort((a, b) => a.sopNo.localeCompare(b.sopNo)))
const activeRecord = computed(() => state.value.records.find((record) => record.sopNo === activeNo.value) ?? null)
const selectedSteps = computed(() => (activeRecord.value?.steps ?? []).slice().sort((a, b) => a.serialNo - b.serialNo || a.pmItemNo.localeCompare(b.pmItemNo)))
const activeStep = computed(() => activeRecord.value?.steps.find((step) => step.pmItemNo === selectedStepNo.value) ?? null)
const actionTargets = computed(() => selectedNos.value.length ? selectedNos.value : activeRecord.value ? [activeRecord.value.sopNo] : [])
const allVisibleSelected = computed(() => visibleRows.value.length > 0 && visibleRows.value.every((row) => selectedNos.value.includes(row.sopNo)))
const itemName = (code: string) => maintenanceItems.value.find((item) => item.pmItemNo === code)?.pmItemName ?? ''
watch(state, (value) => { try { window.localStorage.setItem(storageKey, JSON.stringify(value)) } catch { /* DEV preview remains usable without persistence. */ } }, { deep: true })

function toast(text: string, kind: 'success' | 'error' = 'success') { message.value = text; messageKind.value = kind }
function selectIssueState(value: IssueState) { issueState.value = value; selectedNos.value = []; activeNo.value = '' }
function selectRecord(no: string) { activeNo.value = no; selectedStepNo.value = ''; if (!selectedNos.value.includes(no)) selectedNos.value = [] }
function toggleRecord(no: string) { selectedNos.value = selectedNos.value.includes(no) ? selectedNos.value.filter((item) => item !== no) : [...selectedNos.value, no]; activeNo.value = no }
function toggleAll() { selectedNos.value = allVisibleSelected.value ? [] : visibleRows.value.map((row) => row.sopNo); if (selectedNos.value.length) activeNo.value = selectedNos.value[0] }
function openMain(mode: MainMode) {
  const record = activeRecord.value
  if (mode === 'edit' && record?.issueState !== 0) return toast('只有未凍結 SOP 可以修改。', 'error')
  if ((mode === 'edit' || mode === 'copy') && !record) return toast('請先選取 SOP。', 'error')
  appliedMainSignature.value = ''
  mainMode.value = mode
  mainDraft.value = mode === 'add' ? { sopNo: '', sopName: '', sopFilePath: '', estDoneTime: '', description: '' } : {
    sopNo: record!.sopNo, sopName: record!.sopName, sopFilePath: record!.sopFilePath, estDoneTime: record!.estDoneTime, description: record!.description,
  }
}
function saveMain(closeAfter: boolean) {
  if (!mainMode.value || !mainDraft.value) return
  const mode = mainMode.value
  const draft = mainDraft.value
  const sopNo = draft.sopNo.trim().toLocaleUpperCase()
  const name = draft.sopName
  const estimate = Number(draft.estDoneTime)
  const signature = JSON.stringify([mode, sopNo, name, draft.sopFilePath, String(draft.estDoneTime), draft.description])
  if (!sopNo || !name) return toast('SOP No 與 SOP Name 為必填。', 'error')
  if (!Number.isFinite(estimate) || estimate < 0 || String(draft.estDoneTime).trim() === '') return toast('預估工時必須是大於或等於 0 的數字。', 'error')
  if ((mode === 'add' || mode === 'copy') && appliedMainSignature.value === signature) { if (closeAfter) mainMode.value = null; return }
  if ((mode === 'add' || mode === 'copy') && state.value.records.some((row) => row.sopNo.toLocaleUpperCase() === sopNo)) return toast('SOP No 已存在。', 'error')
  if (mode === 'add' || mode === 'copy') {
    const source = mode === 'copy' ? activeRecord.value : null
    const record: SopRecord = {
      sopNo, sopName: name, sopFilePath: draft.sopFilePath || '', estDoneTime: estimate, description: draft.description,
      creator: currentUser, createDate: nowLabel.value, reviser: '', reviseDate: '', issueState: 0, dataStamp: 0,
      steps: source ? source.steps.map((step) => ({ ...step, creator: currentUser, createDate: nowLabel.value, reviser: '', reviseDate: '', dataStamp: 0 })) : [],
    }
    state.value.records.push(record)
    activeNo.value = sopNo
    issueState.value = 0
    selectedNos.value = []
    appliedMainSignature.value = signature
  } else {
    const current = activeRecord.value
    if (!current || current.issueState !== 0) return toast('只有未凍結 SOP 可以修改。', 'error')
    current.sopName = name
    // Legacy Core treats blank path/description as “not supplied”, so they cannot be cleared in Edit.
    current.sopFilePath = draft.sopFilePath || current.sopFilePath
    current.estDoneTime = estimate
    current.description = draft.description || current.description
    current.reviser = currentUser
    current.reviseDate = nowLabel.value
    current.dataStamp += 1
  }
  toast(mode === 'edit' ? 'SOP 主檔已儲存。' : mode === 'copy' ? 'SOP 主檔與步驟已複製。' : 'SOP 主檔已新增。')
  if (closeAfter) mainMode.value = null
}
function deleteRows() {
  const targets = [...actionTargets.value]
  if (!targets.length || !window.confirm(`確定刪除／停用 ${targets.length} 筆 SOP？`)) return
  let changed = 0
  let stopped = false
  for (const no of targets) {
    const record = state.value.records.find((row) => row.sopNo === no)
    if (!record) continue
    if (record.issueState === 0 || record.issueState === -1) state.value.records = state.value.records.filter((row) => row.sopNo !== no)
    else if (record.issueState === 2) { record.issueState = -1; record.dataStamp += 1; record.reviser = currentUser; record.reviseDate = nowLabel.value }
    else { toast('簽核中的 SOP 不允許刪除；此前已完成的選取列保留。', 'error'); stopped = true; break }
    changed += 1
  }
  selectedNos.value = []; activeNo.value = ''
  if (changed && !stopped) toast(`已處理 ${changed} 筆 SOP。`)
}
function approveRows() {
  const targets = [...actionTargets.value]
  if (!targets.length || !window.confirm(`確定核准 ${targets.length} 筆 SOP？`)) return
  let changed = 0
  let stopped = false
  for (const no of targets) {
    const record = state.value.records.find((row) => row.sopNo === no)
    if (!record) continue
    if (record.issueState !== 0) { toast('只接受未凍結的 SOP；此前已完成的選取列保留。', 'error'); stopped = true; break }
    record.issueState = state.value.approvalWorkflowConfigured ? 1 : 2
    record.reviser = currentUser
    record.reviseDate = nowLabel.value
    record.dataStamp += 1
    changed += 1
  }
  if (!stopped) toast(state.value.approvalWorkflowConfigured ? `已送入簽核 ${changed} 筆，狀態為簽核中。` : `已直接模擬生效 ${changed} 筆 SOP。`)
}
function openSteps() { if (activeRecord.value) { detailOpen.value = true; selectedStepNo.value = '' } }
function openStep(mode: StepMode) {
  const step = activeStep.value
  if ((mode === 'edit' || mode === 'copy') && !step) return toast('請先選取步驟。', 'error')
  stepMode.value = mode
  appliedStepSignature.value = ''
  stepDraft.value = mode === 'add' ? { pmItemNo: '', serialNo: '' } : { pmItemNo: step!.pmItemNo, serialNo: step!.serialNo }
}
function saveStep(closeAfter: boolean) {
  const record = activeRecord.value
  const mode = stepMode.value
  const draft = stepDraft.value
  if (!record || !mode || !draft) return
  const serial = Number(draft.serialNo)
  if (!draft.pmItemNo || !maintenanceItems.value.some((item) => item.pmItemNo === draft.pmItemNo)) return toast('請選擇有效的 PM Item。', 'error')
  if (String(draft.serialNo).trim() === '' || !Number.isFinite(serial) || serial < 0 || String(draft.serialNo).length > 7) return toast('步驟序號必須是 0 以上的數字，最多 7 碼。', 'error')
  const signature = `${mode}:${draft.pmItemNo}:${String(draft.serialNo)}`
  if (appliedStepSignature.value === signature) { if (closeAfter) stepMode.value = null; return }
  if (mode !== 'edit' && record.steps.some((row) => row.pmItemNo === draft.pmItemNo)) return toast('同一 SOP 已有此 PM Item；複合鍵仍須家用端依 live schema 核實。', 'error')
  if (mode === 'edit') {
    const target = record.steps.find((row) => row.pmItemNo === activeStep.value?.pmItemNo)
    if (!target) return toast('步驟已不存在，請重新載入。', 'error')
    // Legacy AddSOPDetail truncates decimal serials to integers; EditSOPDetail keeps one decimal place.
    target.serialNo = Math.floor(serial * 10) / 10
    target.reviser = currentUser; target.reviseDate = nowLabel.value; target.dataStamp += 1
  } else {
    const storedSerial = Math.trunc(Math.floor(serial * 10) / 10)
    record.steps.push({ pmItemNo: draft.pmItemNo, serialNo: storedSerial, creator: currentUser, createDate: nowLabel.value, reviser: '', reviseDate: '', dataStamp: 0 })
    selectedStepNo.value = draft.pmItemNo
  }
  appliedStepSignature.value = signature
  toast(mode === 'edit' ? '步驟已更新。' : mode === 'copy' ? '步驟已複製。' : '步驟已新增。')
  if (closeAfter) stepMode.value = null
}
function chooseFilePath(path: string) { if (mainDraft.value) mainDraft.value.sopFilePath = path; filePickerOpen.value = false }
function deleteSteps() {
  const record = activeRecord.value
  const targets = selectedStepNo.value ? [selectedStepNo.value] : activeStep.value ? [activeStep.value.pmItemNo] : []
  if (!record || !targets.length || !window.confirm(`確定刪除 ${targets.length} 個 SOP 步驟？`)) return
  record.steps = record.steps.filter((step) => !targets.includes(step.pmItemNo))
  selectedStepNo.value = ''
  toast(`已刪除 ${targets.length} 個步驟。`)
}
</script>

<template>
  <section class="pm-sop-basis" aria-label="標準作業程序 DEV 預覽">
    <header class="page-header"><div><p>PM 設定／SOP BASIS</p><h1>標準作業程序</h1></div><div class="header-actions"><span class="dev-tag">DEV 假資料</span><button type="button" class="secondary" @click="emit('close')">Close (X)</button></div></header>
    <div class="filter-row"><label>SOP No <input v-model="searchNo" autocomplete="off" placeholder="搜尋 SOP 編號"></label><span class="row-count">{{ visibleRows.length }} 筆</span></div>
    <nav class="issue-tabs" aria-label="簽核狀態"><button v-for="tab in issueTabs" :key="tab.value" type="button" :class="{ active: issueState === tab.value }" @click="selectIssueState(tab.value)">{{ tab.label }}</button></nav>
    <div class="toolbar"><button type="button" @click="openMain('add')">新增</button><button type="button" :disabled="activeRecord?.issueState !== 0" @click="openMain('edit')">修改</button><button type="button" :disabled="!activeRecord" @click="openMain('copy')">複製</button><button type="button" :disabled="!actionTargets.length" @click="deleteRows">刪除</button><button type="button" :disabled="!actionTargets.length" @click="approveRows">核准</button><button type="button" :disabled="!activeRecord" @click="openSteps">Detail</button><label class="workflow-toggle"><input v-model="state.approvalWorkflowConfigured" type="checkbox"> DEV 模擬已設定簽核流程</label></div>
    <div class="table-wrap main-table"><table><thead><tr><th class="check-col"><input type="checkbox" :checked="allVisibleSelected" aria-label="全選" @change="toggleAll"></th><th>SOP No</th><th>SOP Name</th><th>SOP File Path</th><th>預估工時</th><th>Description</th><th>Creator</th><th>Create Date</th><th>Reviser</th><th>Revise Date</th></tr></thead><tbody>
      <tr v-for="record in visibleRows" :key="record.sopNo" :class="{ selected: activeNo === record.sopNo }" @click="selectRecord(record.sopNo)"><td class="check-col" @click.stop><input type="checkbox" :checked="selectedNos.includes(record.sopNo)" :aria-label="`選取 ${record.sopNo}`" @change="toggleRecord(record.sopNo)"></td><td>{{ record.sopNo }}</td><td>{{ record.sopName }}</td><td class="path-cell">{{ record.sopFilePath }}</td><td class="number-cell">{{ record.estDoneTime }}</td><td class="description-cell">{{ record.description }}</td><td>{{ record.creator }}</td><td>{{ record.createDate }}</td><td>{{ record.reviser }}</td><td>{{ record.reviseDate }}</td></tr>
      <tr v-if="visibleRows.length === 0"><td colspan="10" class="empty">沒有符合條件的 SOP。</td></tr>
    </tbody></table></div>
    <p v-if="message" class="toast" :class="messageKind" role="status">{{ message }}</p>
    <footer class="page-footer"><span>DEV localStorage：{{ storageKey }}</span><span>SOP No 排序｜DataStamp {{ activeRecord?.dataStamp ?? '—' }}</span></footer>

    <div v-if="mainMode && mainDraft" class="modal-backdrop" @click.self="mainMode = null">
      <section class="dialog main-dialog" role="dialog" aria-modal="true" aria-labelledby="sop-main-title"><header><div><p>SOP BASIS / {{ mainMode.toUpperCase() }}</p><h2 id="sop-main-title">{{ mainMode === 'add' ? '新增 SOP' : mainMode === 'edit' ? '修改 SOP' : '複製 SOP' }}</h2></div><button type="button" class="secondary close-x" @click="mainMode = null">×</button></header>
        <label>SOP No <input v-model="mainDraft.sopNo" maxlength="50" :readonly="mainMode === 'edit'" autocomplete="off" @blur="mainDraft.sopNo = mainDraft.sopNo.toUpperCase()"></label>
        <label>SOP Name <input v-model="mainDraft.sopName" maxlength="50" autocomplete="off"></label>
        <label>SOP File Path <span class="path-input"><input v-model="mainDraft.sopFilePath" readonly><button type="button" class="secondary" @click="filePickerOpen = true">選擇檔案路徑</button></span><small>DEV 僅模擬儲存路徑字串，不會上傳檔案。</small></label>
        <label>預估工時 <input v-model="mainDraft.estDoneTime" inputmode="decimal" autocomplete="off"></label>
        <label>Description <textarea v-model="mainDraft.description" rows="4"></textarea></label>
        <footer><button type="button" class="secondary" @click="mainMode = null">取消</button><button type="button" class="secondary" @click="saveMain(false)">Apply</button><button type="button" @click="saveMain(true)">Confirm</button></footer>
      </section>
    </div>

    <div v-if="filePickerOpen && mainDraft" class="modal-backdrop nested-modal" @click.self="filePickerOpen = false"><section class="dialog file-dialog" role="dialog" aria-modal="true" aria-labelledby="file-picker-title"><header><div><p>DEV FILE PICKER</p><h2 id="file-picker-title">選擇假路徑</h2></div><button type="button" class="secondary close-x" @click="filePickerOpen = false">×</button></header><button v-for="path in samplePaths" :key="path" type="button" class="file-choice" @click="chooseFilePath(path)">{{ path }}</button><button type="button" class="secondary" @click="chooseFilePath('')">清除（新增時）</button></section></div>

    <div v-if="detailOpen && activeRecord" class="modal-backdrop" @click.self="detailOpen = false"><section class="dialog detail-dialog" role="dialog" aria-modal="true" aria-labelledby="sop-steps-title"><header><div><p>{{ activeRecord.sopNo }}</p><h2 id="sop-steps-title">{{ activeRecord.sopName }}｜步驟明細</h2></div><button type="button" class="secondary close-x" @click="detailOpen = false">×</button></header>
      <div class="toolbar"><button type="button" @click="openStep('add')">新增步驟</button><button type="button" :disabled="!activeStep" @click="openStep('edit')">修改</button><button type="button" :disabled="!activeStep" @click="openStep('copy')">複製</button><button type="button" :disabled="!activeStep" @click="deleteSteps">刪除</button></div>
      <div class="table-wrap steps-table"><table><thead><tr><th>序號</th><th>PM Item No</th><th>PM Item Name</th><th>Creator</th><th>Create Date</th><th>Reviser</th><th>Revise Date</th></tr></thead><tbody>
        <tr v-for="step in selectedSteps" :key="step.pmItemNo" :class="{ selected: selectedStepNo === step.pmItemNo }" @click="selectedStepNo = step.pmItemNo"><td class="number-cell">{{ step.serialNo }}</td><td>{{ step.pmItemNo }}</td><td>{{ itemName(step.pmItemNo) }}</td><td>{{ step.creator }}</td><td>{{ step.createDate }}</td><td>{{ step.reviser }}</td><td>{{ step.reviseDate }}</td></tr><tr v-if="selectedSteps.length === 0"><td colspan="7" class="empty">此 SOP 尚無步驟。</td></tr>
      </tbody></table></div><p class="detail-note">明細依序號顯示。原程式未檢查父 SOP 狀態；DEV 預覽保留此操作方式。</p>
    </section></div>

    <div v-if="stepMode && stepDraft && activeRecord" class="modal-backdrop nested-modal" @click.self="stepMode = null"><section class="dialog step-dialog" role="dialog" aria-modal="true" aria-labelledby="step-editor-title"><header><div><p>{{ activeRecord.sopNo }} / {{ stepMode.toUpperCase() }}</p><h2 id="step-editor-title">{{ stepMode === 'add' ? '新增 SOP 步驟' : stepMode === 'edit' ? '修改 SOP 步驟' : '複製 SOP 步驟' }}</h2></div><button type="button" class="secondary close-x" @click="stepMode = null">×</button></header>
      <label>PM Item No <select v-model="stepDraft.pmItemNo" :disabled="stepMode === 'edit'"><option value="">請選擇保養項目</option><option v-for="item in maintenanceItems" :key="item.pmItemNo" :value="item.pmItemNo">{{ item.pmItemNo }}　{{ item.pmItemName }}</option></select></label>
      <label>SOP Serial No <input v-model="stepDraft.serialNo" maxlength="7" inputmode="decimal" autocomplete="off"></label>
      <p class="detail-note">DEV lookup 讀取 PM108 假資料，沒有資料時使用本頁內建樣本。新增／複製序號依舊 Service 行為取整數；修改保留一位小數。</p>
      <footer><button type="button" class="secondary" @click="stepMode = null">取消</button><button type="button" class="secondary" @click="saveStep(false)">Apply</button><button type="button" @click="saveStep(true)">Confirm</button></footer>
    </section></div>
  </section>
</template>

<style scoped>
.pm-sop-basis { height:100%; min-height:0; display:flex; flex-direction:column; gap:10px; padding:14px; box-sizing:border-box; background:#f7f5ee; color:#26251f; font:13px/1.45 "Segoe UI","Microsoft JhengHei",sans-serif; }
.page-header,.header-actions,.filter-row,.toolbar,.page-footer,.dialog header,.dialog footer { display:flex; align-items:center; }
.page-header { justify-content:space-between; border-bottom:1px solid #d6d2c6; padding-bottom:10px; }.page-header p,.dialog header p { margin:0; color:#777366; font-size:11px; letter-spacing:.06em; } h1 { margin:2px 0 0; font-size:21px; font-weight:600; }.header-actions { gap:10px; }.dev-tag { padding:3px 8px; border-radius:3px; background:#dce9de; color:#28533a; font-size:11px; }
button { min-height:29px; padding:4px 11px; border:1px solid #454237; border-radius:3px; background:#454237; color:#fff; font:inherit; cursor:pointer; } button:disabled { opacity:.42; cursor:default; } button.secondary { border-color:#bab6a8; background:#fffef9; color:#333126; }
.filter-row { gap:12px; }.filter-row label,.dialog label { display:grid; gap:4px; color:#514f45; }.filter-row input,.dialog input,.dialog textarea,.dialog select { min-height:30px; padding:5px 8px; box-sizing:border-box; border:1px solid #bbb7a9; border-radius:3px; background:#fffef9; color:#26251f; font:inherit; }.filter-row input { width:260px; }.row-count { margin-top:16px; color:#777366; }
.issue-tabs,.toolbar { display:flex; gap:6px; align-items:center; flex-wrap:wrap; }.issue-tabs button { border-color:#c4c0b4; background:#eeece4; color:#49473e; }.issue-tabs button.active { border-color:#555144; background:#555144; color:#fff; }.workflow-toggle { margin-left:auto; display:inline-flex; align-items:center; gap:5px; color:#6c695d; font-size:12px; }
.table-wrap { min-height:130px; overflow:auto; border:1px solid #d0ccbf; background:#fffef9; }.main-table { flex:1; }.steps-table { flex:1; min-height:180px; } table { width:100%; border-collapse:collapse; white-space:nowrap; } th,td { padding:7px 9px; border-bottom:1px solid #e7e4db; text-align:left; } th { position:sticky; top:0; z-index:1; background:#e9e6dc; color:#555246; font-weight:600; } tbody tr { cursor:pointer; } tbody tr:hover { background:#f5f2e8; } tbody tr.selected { background:#e8edf0; }.check-col { width:28px; text-align:center; }.number-cell { text-align:right; }.path-cell { max-width:290px; overflow:hidden; text-overflow:ellipsis; }.description-cell { min-width:190px; max-width:280px; white-space:normal; }.empty { padding:28px; text-align:center; color:#777366; }
.toast { margin:0; padding:8px 10px; border-radius:3px; background:#e4eee4; color:#28533a; }.toast.error { background:#f4e5df; color:#8a3527; }.page-footer { justify-content:space-between; color:#8a877c; font-size:11px; }
.modal-backdrop { position:fixed; inset:0; z-index:120; display:grid; place-items:center; padding:18px; background:#28271f80; }.nested-modal { z-index:130; }.dialog { width:min(560px,100%); max-height:calc(100vh - 36px); overflow:auto; display:grid; gap:12px; padding:16px; border:1px solid #b8b4a7; border-radius:5px; background:#f7f5ee; box-shadow:0 18px 60px #0004; }.dialog header { justify-content:space-between; border-bottom:1px solid #d6d2c6; padding-bottom:9px; }.dialog h2 { margin:2px 0 0; font-size:18px; }.dialog textarea { resize:vertical; }.dialog footer { justify-content:flex-end; gap:7px; padding-top:5px; }.close-x { min-width:30px; padding:2px 8px; font-size:18px; }.path-input { display:flex; gap:6px; }.path-input input { flex:1; min-width:0; }.dialog small,.detail-note { color:#777366; font-size:11px; }.detail-dialog { width:min(1040px,100%); height:min(660px,calc(100vh - 36px)); grid-template-rows:auto auto 1fr auto; }.detail-dialog .toolbar { padding-bottom:2px; }.detail-note { margin:0; }.step-dialog { width:min(500px,100%); }.file-dialog { width:min(680px,100%); }.file-choice { text-align:left; overflow-wrap:anywhere; }
@media (max-width:760px) { .pm-sop-basis { padding:9px; }.workflow-toggle { margin-left:0; }.page-footer { align-items:flex-start; flex-direction:column; gap:3px; }.path-input { align-items:stretch; flex-direction:column; } }
</style>
