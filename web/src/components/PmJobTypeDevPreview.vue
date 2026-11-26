<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type IssueState = 0 | 1 | 2 | -1
type FormMode = 'add' | 'edit' | 'copy'
interface JobType {
  pmJobNo: string; pmJobName: string; memo: string; description: string
  creator: string; createDate: string; reviser: string; reviseDate: string
  issueState: IssueState; dataStamp: number
}
interface DevState { version: 1; approvalWorkflowConfigured: boolean; records: JobType[] }
type JobDraft = Pick<JobType, 'pmJobNo' | 'pmJobName' | 'memo' | 'description'>

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.pm.job-type.v1'
const currentUser = 'DEV01'
const issueTabs: Array<{ value: IssueState; label: string }> = [
  { value: 0, label: '未凍結' }, { value: 1, label: '簽核中' }, { value: 2, label: '已生效' }, { value: -1, label: '未使用' },
]

function seedState(): DevState {
  return {
    version: 1,
    approvalWorkflowConfigured: false,
    records: [
      { pmJobNo: 'CALIBRATION', pmJobName: '設備校正', memo: '年度例行', description: '依設備校正週期安排執行', creator: 'PMUSER', createDate: '2026-11-14 09:20', reviser: '', reviseDate: '', issueState: 2, dataStamp: 2 },
      { pmJobNo: 'CLEANING', pmJobName: '設備清潔', memo: '停機保養', description: '清理設備內部與周邊', creator: 'PMUSER', createDate: '2026-11-17 14:05', reviser: 'PMUSER', reviseDate: '2026-11-19 10:30', issueState: 0, dataStamp: 1 },
      { pmJobNo: 'LUBRICATION', pmJobName: '潤滑保養', memo: '', description: '依保養規範補充潤滑油脂', creator: 'DEV02', createDate: '2026-11-21 08:45', reviser: '', reviseDate: '', issueState: 1, dataStamp: 1 },
      { pmJobNo: 'OLD-INSPECTION', pmJobName: '舊式巡檢', memo: '停用範例', description: '', creator: 'PMUSER', createDate: '2026-09-02 11:10', reviser: 'PMUSER', reviseDate: '2026-10-03 16:40', issueState: -1, dataStamp: 3 },
    ],
  }
}

function loadState(): DevState {
  try {
    const raw = window.localStorage.getItem(storageKey)
    if (raw) {
      const value = JSON.parse(raw) as DevState
      if (value?.version === 1 && Array.isArray(value.records)) return value
    }
  } catch { /* Fall back to fixtures when local storage is unavailable. */ }
  return seedState()
}

const state = ref<DevState>(loadState())
const issueState = ref<IssueState>(0)
const searchCode = ref('')
const activeCode = ref('')
const selectedCodes = ref<string[]>([])
const formMode = ref<FormMode | null>(null)
const draft = ref<JobDraft | null>(null)
const message = ref('')
const messageKind = ref<'success' | 'error'>('success')
const nowLabel = computed(() => new Date().toLocaleString('zh-TW', { hour12: false }))
const visibleRows = computed(() => state.value.records
  .filter((record) => record.issueState === issueState.value)
  .filter((record) => !searchCode.value.trim() || record.pmJobNo.toLocaleLowerCase().includes(searchCode.value.trim().toLocaleLowerCase()))
  .sort((a, b) => a.pmJobNo.localeCompare(b.pmJobNo)))
const activeRecord = computed(() => state.value.records.find((record) => record.pmJobNo === activeCode.value) ?? null)
const actionTargets = computed(() => {
  const selected = state.value.records.filter((record) => selectedCodes.value.includes(record.pmJobNo))
  return selected.length ? selected : activeRecord.value ? [activeRecord.value] : []
})
const allVisibleSelected = computed(() => visibleRows.value.length > 0 && visibleRows.value.every((record) => selectedCodes.value.includes(record.pmJobNo)))
const issueStateLabel = (value: IssueState) => issueTabs.find((tab) => tab.value === value)?.label ?? '未知'

watch(state, (value) => {
  try { window.localStorage.setItem(storageKey, JSON.stringify(value)) } catch { /* Keep DEV edits in memory for this session. */ }
}, { deep: true })
watch(visibleRows, (rows) => {
  if (!rows.some((record) => record.pmJobNo === activeCode.value)) activeCode.value = rows[0]?.pmJobNo ?? ''
  selectedCodes.value = selectedCodes.value.filter((code) => rows.some((record) => record.pmJobNo === code))
}, { immediate: true })

function toast(text: string, kind: 'success' | 'error' = 'success') {
  message.value = text
  messageKind.value = kind
  window.setTimeout(() => { if (message.value === text) message.value = '' }, 3600)
}
function selectIssueState(value: IssueState) {
  issueState.value = value
  selectedCodes.value = []
}
function selectRecord(code: string) {
  activeCode.value = code
  if (!selectedCodes.value.includes(code)) selectedCodes.value = []
}
function toggleRecord(code: string) {
  selectedCodes.value = selectedCodes.value.includes(code)
    ? selectedCodes.value.filter((value) => value !== code)
    : [...selectedCodes.value, code]
}
function toggleAll() {
  selectedCodes.value = allVisibleSelected.value ? [] : visibleRows.value.map((record) => record.pmJobNo)
}
function openForm(mode: FormMode) {
  const source = activeRecord.value
  if (mode !== 'add' && !source) return
  if (mode === 'edit' && source?.issueState !== 0) {
    toast('只有未凍結資料可以修改。', 'error')
    return
  }
  formMode.value = mode
  draft.value = mode === 'add'
    ? { pmJobNo: '', pmJobName: '', memo: '', description: '' }
    : { pmJobNo: mode === 'copy' ? '' : source!.pmJobNo, pmJobName: source!.pmJobName, memo: source!.memo, description: source!.description }
}
function saveForm(closeAfterSave: boolean) {
  const form = draft.value
  if (!form) return
  form.pmJobNo = form.pmJobNo.toUpperCase()
  if (form.pmJobNo === '' || form.pmJobName === '') {
    toast('PM Job No 與 PM Job Name 不可空白。', 'error')
    return
  }
  const originalCode = formMode.value === 'edit' ? activeCode.value : ''
  if (state.value.records.some((record) => record.pmJobNo.toUpperCase() === form.pmJobNo && record.pmJobNo !== originalCode)) {
    toast('PM Job No 已存在。', 'error')
    return
  }
  if (formMode.value === 'edit' && activeRecord.value) {
    const record = activeRecord.value
    record.pmJobName = form.pmJobName
    // The legacy service treats empty Memo/Description as omitted fields, so blank values do not clear them.
    if (form.memo !== '') record.memo = form.memo
    if (form.description !== '') record.description = form.description
    record.reviser = currentUser
    record.reviseDate = nowLabel.value
    record.dataStamp += 1
    toast('工作形式已在 DEV 假資料中修改。')
  } else {
    const record: JobType = {
      pmJobNo: form.pmJobNo, pmJobName: form.pmJobName, memo: form.memo, description: form.description,
      creator: currentUser, createDate: nowLabel.value, reviser: '', reviseDate: '', issueState: 0, dataStamp: 0,
    }
    state.value.records.push(record)
    searchCode.value = ''
    issueState.value = 0
    activeCode.value = record.pmJobNo
    toast(formMode.value === 'copy' ? '工作形式已複製至 DEV 假資料。' : '工作形式已新增至 DEV 假資料。')
  }
  if (closeAfterSave) {
    formMode.value = null
    draft.value = null
  }
}
function deleteRows() {
  let stopped = false
  for (const record of actionTargets.value) {
    if (record.issueState === 1) {
      toast(`簽核中資料 ${record.pmJobNo} 不可刪除；此前已處理的列不回復。`, 'error')
      stopped = true
      break
    }
    if (record.issueState === 2) {
      record.issueState = -1
      record.dataStamp += 1
      record.reviser = currentUser
      record.reviseDate = nowLabel.value
    } else {
      state.value.records = state.value.records.filter((item) => item.pmJobNo !== record.pmJobNo)
      if (activeCode.value === record.pmJobNo) activeCode.value = ''
    }
  }
  selectedCodes.value = []
  if (!stopped) toast('所選資料已依狀態刪除或移至未使用。')
}
function approveRows() {
  for (const record of actionTargets.value) {
    if (record.issueState !== 0) {
      toast(`只有未凍結資料可核准；${record.pmJobNo} 之後的列未處理。`, 'error')
      return
    }
    record.issueState = state.value.approvalWorkflowConfigured ? 1 : 2
    record.dataStamp += 1
    record.reviser = currentUser
    record.reviseDate = nowLabel.value
  }
  selectedCodes.value = []
  toast(state.value.approvalWorkflowConfigured ? '資料已模擬送入簽核中。' : '未設定簽核流程，資料直接模擬生效。')
}
</script>

<template>
  <section class="pm-job-type" aria-label="工作形式設定 DEV 預覽">
    <header class="page-header">
      <div><p>PM 設定／JOB TYPE</p><h1>工作形式設定</h1></div>
      <div class="header-actions"><span class="dev-tag">DEV 假資料</span><button type="button" class="close" @click="emit('close')">Close (X)</button></div>
    </header>

    <div class="filter-row">
      <label>PM Job No <input v-model="searchCode" autocomplete="off" placeholder="搜尋工作形式代碼"></label>
      <span class="row-count">{{ visibleRows.length }} 筆</span>
    </div>
    <nav class="issue-tabs" aria-label="簽核狀態">
      <button v-for="tab in issueTabs" :key="tab.value" type="button" :class="{ active: issueState === tab.value }" @click="selectIssueState(tab.value)">{{ tab.label }}</button>
    </nav>
    <div class="toolbar">
      <button type="button" @click="openForm('add')">新增</button>
      <button type="button" :disabled="activeRecord?.issueState !== 0" @click="openForm('edit')">修改</button>
      <button type="button" :disabled="!activeRecord" @click="openForm('copy')">複製</button>
      <button type="button" :disabled="actionTargets.length === 0" @click="deleteRows">刪除</button>
      <button type="button" :disabled="actionTargets.length === 0" @click="approveRows">核准</button>
      <label class="workflow-toggle"><input v-model="state.approvalWorkflowConfigured" type="checkbox"> DEV 模擬已設定簽核流程</label>
    </div>

    <div class="table-wrap">
      <table>
        <thead><tr><th class="check-col"><input type="checkbox" :checked="allVisibleSelected" aria-label="全選" @change="toggleAll"></th><th>PM Job No</th><th>PM Job Name</th><th>Memo</th><th>Description</th><th>Creator</th><th>Create Date</th><th>Reviser</th><th>Revise Date</th><th>狀態</th></tr></thead>
        <tbody>
          <tr v-for="record in visibleRows" :key="record.pmJobNo" :class="{ selected: activeCode === record.pmJobNo }" @click="selectRecord(record.pmJobNo)">
            <td class="check-col" @click.stop><input type="checkbox" :checked="selectedCodes.includes(record.pmJobNo)" :aria-label="`選取 ${record.pmJobNo}`" @change="toggleRecord(record.pmJobNo)"></td>
            <td>{{ record.pmJobNo }}</td><td>{{ record.pmJobName }}</td><td>{{ record.memo }}</td><td class="description-cell">{{ record.description }}</td><td>{{ record.creator }}</td><td>{{ record.createDate }}</td><td>{{ record.reviser }}</td><td>{{ record.reviseDate }}</td><td><span class="state-pill" :class="`state-${record.issueState}`">{{ issueStateLabel(record.issueState) }}</span></td>
          </tr>
          <tr v-if="visibleRows.length === 0"><td colspan="10" class="empty">沒有符合條件的工作形式。</td></tr>
        </tbody>
      </table>
    </div>
    <p v-if="message" class="toast" :class="messageKind" role="status">{{ message }}</p>

    <div v-if="formMode && draft" class="modal-backdrop" @click.self="formMode = null; draft = null">
      <section class="edit-dialog" role="dialog" aria-modal="true" aria-labelledby="job-type-title">
        <header><div><p>JOB TYPE / {{ formMode.toUpperCase() }}</p><h2 id="job-type-title">{{ formMode === 'add' ? '新增工作形式' : formMode === 'edit' ? '修改工作形式' : '複製工作形式' }}</h2></div><button type="button" class="close" aria-label="關閉" @click="formMode = null; draft = null">×</button></header>
        <label>PM Job No <input v-model="draft.pmJobNo" :readonly="formMode === 'edit'" autocomplete="off" @blur="draft.pmJobNo = draft.pmJobNo.toUpperCase()"></label>
        <label>PM Job Name <input v-model="draft.pmJobName" autocomplete="off"></label>
        <label>Memo <textarea v-model="draft.memo" rows="3"></textarea></label>
        <label>Description <textarea v-model="draft.description" rows="4"></textarea></label>
        <footer><button type="button" class="secondary" @click="formMode = null; draft = null">取消</button><button type="button" class="secondary" @click="saveForm(false)">Apply</button><button type="button" @click="saveForm(true)">Confirm</button></footer>
      </section>
    </div>
    <footer class="page-footer"><span>DEV localStorage：{{ storageKey }}</span><span>PMJobNo 排序｜DataStamp {{ activeRecord?.dataStamp ?? '—' }}</span></footer>
  </section>
</template>

<style scoped>
.pm-job-type { height: 100%; min-height: 0; display: flex; flex-direction: column; gap: 10px; padding: 14px; box-sizing: border-box; background: #f7f5ee; color: #26251f; font: 13px/1.45 "Segoe UI", "Microsoft JhengHei", sans-serif; }
.page-header, .header-actions, .filter-row, .toolbar, .page-footer, .edit-dialog header, .edit-dialog footer { display: flex; align-items: center; }
.page-header { justify-content: space-between; border-bottom: 1px solid #d6d2c6; padding-bottom: 10px; }
.page-header p, .edit-dialog header p { margin: 0; color: #777366; font-size: 11px; letter-spacing: .06em; }
h1 { margin: 2px 0 0; font-size: 21px; font-weight: 600; }
.header-actions { gap: 10px; }
.dev-tag { padding: 3px 8px; border-radius: 3px; background: #dce9de; color: #28533a; font-size: 11px; }
button { min-height: 29px; padding: 4px 11px; border: 1px solid #454237; border-radius: 3px; background: #454237; color: white; font: inherit; cursor: pointer; }
button:disabled { opacity: .42; cursor: default; }
button.secondary, .close { border-color: #bab6a8; background: #fffef9; color: #333126; }
.filter-row { gap: 12px; }
.filter-row label, .edit-dialog label { display: grid; gap: 4px; color: #514f45; }
input:not([type=checkbox]), textarea { min-height: 30px; padding: 5px 8px; box-sizing: border-box; border: 1px solid #bbb7a9; border-radius: 3px; background: #fffef9; color: #26251f; font: inherit; }
.filter-row input { width: 260px; }
.row-count { margin-top: 16px; color: #777366; }
.issue-tabs, .toolbar { display: flex; gap: 6px; align-items: center; flex-wrap: wrap; }
.issue-tabs button { border-color: #c4c0b4; background: #eeece4; color: #49473e; }
.issue-tabs button.active { border-color: #555144; background: #555144; color: #fff; }
.workflow-toggle { margin-left: auto; display: inline-flex; align-items: center; gap: 5px; color: #6c695d; font-size: 12px; }
.table-wrap { flex: 1; min-height: 130px; overflow: auto; border: 1px solid #d0ccbf; background: #fffef9; }
table { width: 100%; border-collapse: collapse; white-space: nowrap; }
th, td { padding: 7px 9px; border-bottom: 1px solid #e7e4db; text-align: left; }
th { position: sticky; top: 0; z-index: 1; background: #e9e6dc; color: #555246; font-weight: 600; }
tbody tr { cursor: pointer; }
tbody tr:hover { background: #f5f2e8; }
tbody tr.selected { background: #e8edf0; }
.check-col { width: 28px; text-align: center; }
.description-cell { min-width: 210px; white-space: normal; }
.state-pill { display: inline-block; padding: 2px 7px; border-radius: 10px; background: #e8e6df; color: #555246; font-size: 11px; }
.state-0 { background: #eee6cf; color: #705716; }.state-1 { background: #e2e8f1; color: #394e71; }.state-2 { background: #dce9de; color: #28533a; }.state--1 { background: #ece8e5; color: #78685d; }
.empty { padding: 28px; text-align: center; color: #777366; }
.toast { margin: 0; padding: 8px 10px; border-radius: 3px; background: #e4eee4; color: #28533a; }.toast.error { background: #f4e5df; color: #8a3527; }
.page-footer { justify-content: space-between; color: #8a877c; font-size: 11px; }
.modal-backdrop { position: fixed; inset: 0; z-index: 120; display: grid; place-items: center; padding: 18px; background: #28271f80; }
.edit-dialog { width: min(520px, 100%); display: grid; gap: 12px; padding: 16px; border: 1px solid #b8b4a7; border-radius: 5px; background: #f7f5ee; box-shadow: 0 18px 60px #0004; }
.edit-dialog header { justify-content: space-between; border-bottom: 1px solid #d6d2c6; padding-bottom: 9px; }
.edit-dialog h2 { margin: 2px 0 0; font-size: 18px; }.edit-dialog textarea { resize: vertical; }
.edit-dialog footer { justify-content: flex-end; gap: 7px; padding-top: 5px; }
@media (max-width: 760px) { .pm-job-type { padding: 9px; }.workflow-toggle { margin-left: 0; }.page-footer { align-items: flex-start; flex-direction: column; gap: 3px; } }
</style>
