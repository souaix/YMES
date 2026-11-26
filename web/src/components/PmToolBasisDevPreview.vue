<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type IssueState = 0 | 1 | 2 | -1
type FormMode = 'add' | 'edit' | 'copy'
interface ToolRecord { toolNo: string; toolName: string; description: string; creator: string; createDate: string; reviser: string; reviseDate: string; issueState: IssueState; dataStamp: number }
interface DevState { version: 1; approvalWorkflowConfigured: boolean; records: ToolRecord[] }
interface ToolDraft { toolNo: string; toolName: string; description: string }

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.eqp.tool-basis.v1'
const currentUser = 'DEV01'
const issueTabs: Array<{ value: IssueState; label: string }> = [
  { value: 0, label: '未凍結' }, { value: 1, label: '簽核中' }, { value: 2, label: '已生效' }, { value: -1, label: '未使用' },
]
function seedState(): DevState {
  return {
    version: 1,
    approvalWorkflowConfigured: false,
    records: [
      { toolNo: 'TL-0001', toolName: 'TORQUE WRENCH 20–200 N·m', description: '定期校正，作業後歸還工具櫃。', creator: 'PMUSER', createDate: '2026-11-10 09:10', reviser: 'PMUSER', reviseDate: '2026-11-15 14:20', issueState: 0, dataStamp: 2 },
      { toolNo: 'TL-0012', toolName: 'DIGITAL CALIPER', description: '量測範圍 0–150 mm。', creator: 'DEV02', createDate: '2026-11-12 10:30', reviser: '', reviseDate: '', issueState: 1, dataStamp: 1 },
      { toolNo: 'TL-0038', toolName: 'INSULATION TESTER', description: '需確認檢測線與絕緣棒完整。', creator: 'PMUSER', createDate: '2026-10-18 11:00', reviser: 'PMUSER', reviseDate: '2026-10-19 08:30', issueState: 2, dataStamp: 2 },
      { toolNo: 'TL-0105', toolName: 'LUBRICATION GUN', description: '依工具保養規範清潔並封存。', creator: 'PMUSER', createDate: '2026-10-20 08:40', reviser: '', reviseDate: '', issueState: 2, dataStamp: 0 },
      { toolNo: 'TL-OLD-00', toolName: 'OLD HAND TOOL', description: '停用範例。', creator: 'PMUSER', createDate: '2026-07-01 15:00', reviser: 'PMUSER', reviseDate: '2026-08-01 09:00', issueState: -1, dataStamp: 3 },
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

const state = ref<DevState>(loadState())
const issueState = ref<IssueState>(0)
const searchNo = ref('')
const activeNo = ref('')
const selectedNos = ref<string[]>([])
const formMode = ref<FormMode | null>(null)
const draft = ref<ToolDraft | null>(null)
const appliedSignature = ref('')
const message = ref('')
const messageKind = ref<'success' | 'error'>('success')
const nowLabel = computed(() => new Date().toLocaleString('zh-TW', { hour12: false }))
const visibleRows = computed(() => state.value.records
  .filter((record) => record.issueState === issueState.value)
  .filter((record) => !searchNo.value.trim() || record.toolNo.toLocaleLowerCase().includes(searchNo.value.trim().toLocaleLowerCase()))
  .sort((a, b) => a.toolNo.localeCompare(b.toolNo)))
const activeRecord = computed(() => state.value.records.find((record) => record.toolNo === activeNo.value) ?? null)
const actionTargets = computed(() => selectedNos.value.length ? selectedNos.value : activeRecord.value ? [activeRecord.value.toolNo] : [])
const allVisibleSelected = computed(() => visibleRows.value.length > 0 && visibleRows.value.every((row) => selectedNos.value.includes(row.toolNo)))
const fieldSignature = computed(() => draft.value ? JSON.stringify([formMode.value, draft.value.toolNo, draft.value.toolName, draft.value.description]) : '')
watch(state, (value) => { try { window.localStorage.setItem(storageKey, JSON.stringify(value)) } catch { /* Keep this DEV preview usable without persistence. */ } }, { deep: true })

function toast(text: string, kind: 'success' | 'error' = 'success') { message.value = text; messageKind.value = kind }
function selectIssueState(value: IssueState) { issueState.value = value; activeNo.value = ''; selectedNos.value = [] }
function selectRecord(no: string) { activeNo.value = no; if (!selectedNos.value.includes(no)) selectedNos.value = [] }
function toggleRecord(no: string) { selectedNos.value = selectedNos.value.includes(no) ? selectedNos.value.filter((value) => value !== no) : [...selectedNos.value, no]; activeNo.value = no }
function toggleAll() { selectedNos.value = allVisibleSelected.value ? [] : visibleRows.value.map((row) => row.toolNo); if (selectedNos.value.length) activeNo.value = selectedNos.value[0] }
function openForm(mode: FormMode) {
  const record = activeRecord.value
  if (mode === 'edit' && record?.issueState !== 0) return toast('只有未凍結工具資料可以修改。', 'error')
  if ((mode === 'edit' || mode === 'copy') && !record) return toast('請先選取工具資料。', 'error')
  formMode.value = mode
  appliedSignature.value = ''
  draft.value = mode === 'add' ? { toolNo: '', toolName: '', description: '' } : { toolNo: record!.toolNo, toolName: record!.toolName, description: record!.description }
}
function saveForm(closeAfter: boolean) {
  const mode = formMode.value
  const form = draft.value
  if (!mode || !form) return
  const toolNo = form.toolNo.toLocaleUpperCase()
  const toolName = form.toolName.toLocaleUpperCase()
  if (form.toolNo === '' || form.toolName === '') return toast('Tool No 與 Tool Name 為必填。', 'error')
  if ((mode === 'add' || mode === 'copy') && appliedSignature.value === fieldSignature.value) { if (closeAfter) formMode.value = null; return }
  if ((mode === 'add' || mode === 'copy') && state.value.records.some((record) => record.toolNo.toLocaleUpperCase() === toolNo)) return toast('Tool No 已存在。', 'error')
  if (mode === 'add' || mode === 'copy') {
    state.value.records.push({ toolNo, toolName, description: form.description, creator: currentUser, createDate: nowLabel.value, reviser: '', reviseDate: '', issueState: 0, dataStamp: 0 })
    activeNo.value = toolNo
    issueState.value = 0
    selectedNos.value = []
    appliedSignature.value = JSON.stringify([mode, toolNo, toolName, form.description])
  } else {
    const record = activeRecord.value
    if (!record || record.issueState !== 0) return toast('只有未凍結工具資料可以修改。', 'error')
    record.toolName = toolName
    // Legacy Core treats blank Description as “not supplied”, so Edit cannot clear it.
    record.description = form.description || record.description
    record.reviser = currentUser
    record.reviseDate = nowLabel.value
    record.dataStamp += 1
  }
  toast(mode === 'edit' ? '工具資料已儲存。' : mode === 'copy' ? '工具資料已複製。' : '工具資料已新增。')
  if (closeAfter) formMode.value = null
}
function deleteRows() {
  const targets = [...actionTargets.value]
  if (!targets.length || !window.confirm(`確定刪除／停用 ${targets.length} 筆工具資料？`)) return
  let changed = 0
  let stopped = false
  for (const no of targets) {
    const record = state.value.records.find((row) => row.toolNo === no)
    if (!record) continue
    if (record.issueState === 0 || record.issueState === -1) state.value.records = state.value.records.filter((row) => row.toolNo !== no)
    else if (record.issueState === 2) { record.issueState = -1; record.reviser = currentUser; record.reviseDate = nowLabel.value; record.dataStamp += 1 }
    else { toast('簽核中的工具資料不允許刪除；此前已完成的選取列保留。', 'error'); stopped = true; break }
    changed += 1
  }
  activeNo.value = ''; selectedNos.value = []
  if (changed && !stopped) toast(`已處理 ${changed} 筆工具資料。`)
}
function approveRows() {
  const targets = [...actionTargets.value]
  if (!targets.length || !window.confirm(`確定核准 ${targets.length} 筆工具資料？`)) return
  let changed = 0
  let stopped = false
  for (const no of targets) {
    const record = state.value.records.find((row) => row.toolNo === no)
    if (!record) continue
    if (record.issueState !== 0) { toast('只接受未凍結的工具資料；此前已完成的選取列保留。', 'error'); stopped = true; break }
    record.issueState = state.value.approvalWorkflowConfigured ? 1 : 2
    record.reviser = currentUser
    record.reviseDate = nowLabel.value
    record.dataStamp += 1
    changed += 1
  }
  if (!stopped) toast(state.value.approvalWorkflowConfigured ? `已送入簽核 ${changed} 筆，狀態為簽核中。` : `已直接模擬生效 ${changed} 筆工具資料。`)
}
</script>

<template>
  <section class="pm-tool-basis" aria-label="工具基本資料 DEV 預覽">
    <header class="page-header"><div><p>PM 設定／TOOL BASIS</p><h1>工具基本資料</h1></div><div class="header-actions"><span class="dev-tag">DEV 假資料</span><button type="button" class="secondary" @click="emit('close')">Close (X)</button></div></header>
    <div class="filter-row"><label>Tool No <input v-model="searchNo" autocomplete="off" placeholder="搜尋工具編號"></label><span class="row-count">{{ visibleRows.length }} 筆</span></div>
    <nav class="issue-tabs" aria-label="簽核狀態"><button v-for="tab in issueTabs" :key="tab.value" type="button" :class="{ active: issueState === tab.value }" @click="selectIssueState(tab.value)">{{ tab.label }}</button></nav>
    <div class="toolbar"><button type="button" @click="openForm('add')">新增</button><button type="button" :disabled="activeRecord?.issueState !== 0" @click="openForm('edit')">修改</button><button type="button" :disabled="!activeRecord" @click="openForm('copy')">複製</button><button type="button" :disabled="!actionTargets.length" @click="deleteRows">刪除</button><button type="button" :disabled="!actionTargets.length" @click="approveRows">核准</button><label class="workflow-toggle"><input v-model="state.approvalWorkflowConfigured" type="checkbox"> DEV 模擬已設定簽核流程</label></div>
    <div class="table-wrap"><table><thead><tr><th class="check-col"><input type="checkbox" :checked="allVisibleSelected" aria-label="全選" @change="toggleAll"></th><th>Tool No</th><th>Tool Name</th><th>Description</th><th>Creator</th><th>Create Date</th><th>Reviser</th><th>Revise Date</th></tr></thead><tbody>
      <tr v-for="record in visibleRows" :key="record.toolNo" :class="{ selected: activeNo === record.toolNo }" @click="selectRecord(record.toolNo)"><td class="check-col" @click.stop><input type="checkbox" :checked="selectedNos.includes(record.toolNo)" :aria-label="`選取 ${record.toolNo}`" @change="toggleRecord(record.toolNo)"></td><td>{{ record.toolNo }}</td><td>{{ record.toolName }}</td><td class="description-cell">{{ record.description }}</td><td>{{ record.creator }}</td><td>{{ record.createDate }}</td><td>{{ record.reviser }}</td><td>{{ record.reviseDate }}</td></tr>
      <tr v-if="visibleRows.length === 0"><td colspan="8" class="empty">沒有符合條件的工具資料。</td></tr>
    </tbody></table></div>
    <p v-if="message" class="toast" :class="messageKind" role="status">{{ message }}</p>
    <footer class="page-footer"><span>DEV localStorage：{{ storageKey }}</span><span>Tool No 排序｜DataStamp {{ activeRecord?.dataStamp ?? '—' }}</span></footer>

    <div v-if="formMode && draft" class="modal-backdrop" @click.self="formMode = null"><section class="dialog" role="dialog" aria-modal="true" aria-labelledby="tool-title"><header><div><p>TOOL BASIS / {{ formMode.toUpperCase() }}</p><h2 id="tool-title">{{ formMode === 'add' ? '新增工具資料' : formMode === 'edit' ? '修改工具資料' : '複製工具資料' }}</h2></div><button type="button" class="secondary close-x" @click="formMode = null">×</button></header>
      <label>Tool No <input v-model="draft.toolNo" maxlength="20" :readonly="formMode === 'edit'" autocomplete="off" @blur="draft.toolNo = draft.toolNo.toUpperCase()"></label>
      <label>Tool Name <input v-model="draft.toolName" maxlength="50" autocomplete="off" @blur="draft.toolName = draft.toolName.toUpperCase()"></label>
      <label>Description <textarea v-model="draft.description" maxlength="255" rows="4"></textarea></label>
      <p class="form-note">空白 Description 在舊 Core 修改時會被視為未提供，無法清除既有文字。</p>
      <footer><button type="button" class="secondary" @click="formMode = null">取消</button><button type="button" class="secondary" @click="saveForm(false)">Apply</button><button type="button" @click="saveForm(true)">Confirm</button></footer>
    </section></div>
  </section>
</template>

<style scoped>
.pm-tool-basis { height:100%; min-height:0; display:flex; flex-direction:column; gap:10px; padding:14px; box-sizing:border-box; background:#f7f5ee; color:#26251f; font:13px/1.45 "Segoe UI","Microsoft JhengHei",sans-serif; }
.page-header,.header-actions,.filter-row,.toolbar,.page-footer,.dialog header,.dialog footer { display:flex; align-items:center; }.page-header { justify-content:space-between; border-bottom:1px solid #d6d2c6; padding-bottom:10px; }.page-header p,.dialog header p { margin:0; color:#777366; font-size:11px; letter-spacing:.06em; }h1 { margin:2px 0 0; font-size:21px; font-weight:600; }.header-actions { gap:10px; }.dev-tag { padding:3px 8px; border-radius:3px; background:#dce9de; color:#28533a; font-size:11px; }
button { min-height:29px; padding:4px 11px; border:1px solid #454237; border-radius:3px; background:#454237; color:#fff; font:inherit; cursor:pointer; }button:disabled { opacity:.42; cursor:default; }button.secondary { border-color:#bab6a8; background:#fffef9; color:#333126; }
.filter-row { gap:12px; }.filter-row label,.dialog label { display:grid; gap:4px; color:#514f45; }.filter-row input,.dialog input,.dialog textarea { min-height:30px; padding:5px 8px; box-sizing:border-box; border:1px solid #bbb7a9; border-radius:3px; background:#fffef9; color:#26251f; font:inherit; }.filter-row input { width:260px; }.row-count { margin-top:16px; color:#777366; }
.issue-tabs,.toolbar { display:flex; gap:6px; align-items:center; flex-wrap:wrap; }.issue-tabs button { border-color:#c4c0b4; background:#eeece4; color:#49473e; }.issue-tabs button.active { border-color:#555144; background:#555144; color:#fff; }.workflow-toggle { margin-left:auto; display:inline-flex; align-items:center; gap:5px; color:#6c695d; font-size:12px; }
.table-wrap { flex:1; min-height:130px; overflow:auto; border:1px solid #d0ccbf; background:#fffef9; }table { width:100%; border-collapse:collapse; white-space:nowrap; }th,td { padding:7px 9px; border-bottom:1px solid #e7e4db; text-align:left; }th { position:sticky; top:0; z-index:1; background:#e9e6dc; color:#555246; font-weight:600; }tbody tr { cursor:pointer; }tbody tr:hover { background:#f5f2e8; }tbody tr.selected { background:#e8edf0; }.check-col { width:28px; text-align:center; }.description-cell { min-width:190px; max-width:400px; white-space:normal; }.empty { padding:28px; text-align:center; color:#777366; }
.toast { margin:0; padding:8px 10px; border-radius:3px; background:#e4eee4; color:#28533a; }.toast.error { background:#f4e5df; color:#8a3527; }.page-footer { justify-content:space-between; color:#8a877c; font-size:11px; }
.modal-backdrop { position:fixed; inset:0; z-index:120; display:grid; place-items:center; padding:18px; background:#28271f80; }.dialog { width:min(520px,100%); max-height:calc(100vh - 36px); overflow:auto; display:grid; gap:12px; padding:16px; border:1px solid #b8b4a7; border-radius:5px; background:#f7f5ee; box-shadow:0 18px 60px #0004; }.dialog header { justify-content:space-between; border-bottom:1px solid #d6d2c6; padding-bottom:9px; }.dialog h2 { margin:2px 0 0; font-size:18px; }.dialog textarea { resize:vertical; }.dialog footer { justify-content:flex-end; gap:7px; padding-top:5px; }.close-x { min-width:30px; padding:2px 8px; font-size:18px; }.form-note { margin:0; color:#777366; font-size:11px; }
@media (max-width:760px) { .pm-tool-basis { padding:9px; }.workflow-toggle { margin-left:0; }.page-footer { align-items:flex-start; flex-direction:column; gap:3px; } }
</style>
