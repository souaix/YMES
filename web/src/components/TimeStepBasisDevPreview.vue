<script setup lang="ts">
import { computed, onUnmounted, reactive, ref } from 'vue'

type IssueState = 0 | 1 | 2 | -1
type FormMode = 'add' | 'edit' | 'copy'
type PendingAction = 'delete' | 'approve'
interface TimeStepRow {
  timeStepNo: string
  timeStepName: string
  fromTime: string
  toTime: string
  description: string
  creator: string
  createDate: string
  issueState: IssueState
}
interface Draft {
  timeStepNo: string
  timeStepName: string
  fromTime: string
  toTime: string
  description: string
}

const emit = defineEmits<{ close: [] }>()
const statusFilters: Array<{ value: IssueState; label: string }> = [
  { value: 0, label: '未核准 UNFROZEN' },
  { value: 1, label: '待處理 PENDING' },
  { value: 2, label: '啟用 ACTIVE' },
  { value: -1, label: '停用 UNUSED' },
]
const timeStepOptions = Array.from({ length: 15 }, (_, index) => String(index + 1).padStart(2, '0'))
const rows = ref<TimeStepRow[]>([
  { timeStepNo: '01', timeStepName: '早班', fromTime: '06:00', toTime: '14:00', description: '第一段生產時段', creator: 'DEMO', createDate: '2026-11-20 08:12:00', issueState: 2 },
  { timeStepNo: '02', timeStepName: '中班', fromTime: '14:00', toTime: '22:00', description: '第二段生產時段', creator: 'DEMO', createDate: '2026-11-20 08:14:00', issueState: 2 },
  { timeStepNo: '03', timeStepName: '夜班', fromTime: '22:00', toTime: '04:00', description: '跨午夜時段；DEV 僅儲存時分', creator: 'DEMO', createDate: '2026-11-21 09:30:00', issueState: 0 },
  { timeStepNo: '04', timeStepName: '待確認測試時段', fromTime: '08:00', toTime: '10:00', description: '示範舊資料 IssueState=1', creator: 'DEMO', createDate: '2026-11-21 10:00:00', issueState: 1 },
  { timeStepNo: '05', timeStepName: '舊夜間時段', fromTime: '01:00', toTime: '03:00', description: '停用範例', creator: 'DEMO', createDate: '2026-11-18 15:20:00', issueState: -1 },
])
const activeFilter = ref<IssueState>(0)
const selectedNos = ref<string[]>([])
const formMode = ref<FormMode | null>(null)
const editingNo = ref('')
const formDirty = ref(false)
const formError = ref('')
const draft = reactive<Draft>({ timeStepNo: '', timeStepName: '', fromTime: '04:00', toTime: '06:00', description: '' })
const pendingAction = ref<PendingAction | null>(null)
const notice = ref('')
const noticeTone = ref<'success' | 'error'>('success')
let noticeTimer: ReturnType<typeof setTimeout> | undefined

const visibleRows = computed(() => rows.value
  .filter((row) => row.issueState === activeFilter.value)
  .slice()
  .sort((left, right) => Number(left.timeStepNo) - Number(right.timeStepNo)))
const selectedRows = computed(() => rows.value.filter((row) => selectedNos.value.includes(row.timeStepNo)))
const allVisibleSelected = computed(() => visibleRows.value.length > 0 && visibleRows.value.every((row) => selectedNos.value.includes(row.timeStepNo)))
const formTitle = computed(() => formMode.value === 'add' ? '新增時段' : formMode.value === 'edit' ? '編輯時段' : '複製時段')
const confirmTitle = computed(() => pendingAction.value === 'approve' ? '核准時段' : '刪除時段')
const confirmText = computed(() => {
  const count = selectedNos.value.length
  if (pendingAction.value === 'approve') return '確定將 ' + count + ' 筆未核准時段設為 ACTIVE？'
  if (activeFilter.value === 2) return '確定將 ' + count + ' 筆 ACTIVE 時段停用為 UNUSED？'
  return '確定刪除所選 ' + count + ' 筆時段？'
})
const activeCount = computed(() => rows.value.filter((row) => row.issueState === activeFilter.value).length)

function announce(message: string, tone: 'success' | 'error' = 'success') {
  notice.value = message
  noticeTone.value = tone
  if (noticeTimer) clearTimeout(noticeTimer)
  noticeTimer = setTimeout(() => { notice.value = '' }, 3200)
}
function setFilter(value: IssueState) {
  activeFilter.value = value
  selectedNos.value = []
}
function toggleRow(timeStepNo: string, checked: boolean) {
  selectedNos.value = checked
    ? [...new Set([...selectedNos.value, timeStepNo])]
    : selectedNos.value.filter((value) => value !== timeStepNo)
}
function toggleVisible(checked: boolean) {
  const visibleNos = visibleRows.value.map((row) => row.timeStepNo)
  selectedNos.value = checked
    ? [...new Set([...selectedNos.value, ...visibleNos])]
    : selectedNos.value.filter((value) => !visibleNos.includes(value))
}
function resetDraft(value: Draft) {
  draft.timeStepNo = value.timeStepNo
  draft.timeStepName = value.timeStepName
  draft.fromTime = value.fromTime
  draft.toTime = value.toTime
  draft.description = value.description
  formError.value = ''
  formDirty.value = false
}
function openAdd() {
  formMode.value = 'add'
  editingNo.value = ''
  resetDraft({ timeStepNo: '', timeStepName: '', fromTime: '04:00', toTime: '06:00', description: '' })
}
function openEdit() {
  if (selectedRows.value.length !== 1) {
    announce('請先選取一筆時段。', 'error')
    return
  }
  const row = selectedRows.value[0]
  if (row.issueState !== 0) {
    announce('只有 UNFROZEN 時段可以編輯。', 'error')
    return
  }
  formMode.value = 'edit'
  editingNo.value = row.timeStepNo
  resetDraft({ timeStepNo: row.timeStepNo, timeStepName: row.timeStepName, fromTime: row.fromTime, toTime: row.toTime, description: row.description })
}
function openCopy() {
  if (selectedRows.value.length !== 1) {
    announce('請先選取一筆時段。', 'error')
    return
  }
  const row = selectedRows.value[0]
  formMode.value = 'copy'
  editingNo.value = ''
  resetDraft({ timeStepNo: row.timeStepNo, timeStepName: row.timeStepName, fromTime: row.fromTime, toTime: row.toTime, description: row.description })
}
function closeForm() {
  formMode.value = null
  formError.value = ''
  formDirty.value = false
}
function minutes(value: string) {
  const [hours, minute] = value.split(':').map(Number)
  return hours * 60 + minute
}
function overlaps(leftStart: number, leftEnd: number, rightStart: number, rightEnd: number) {
  for (const dayOffset of [-1440, 0, 1440]) {
    const shiftedStart = rightStart + dayOffset
    const shiftedEnd = rightEnd + dayOffset
    if (leftStart < shiftedEnd && shiftedStart < leftEnd) return true
  }
  return false
}
function findOverlap(fromTime: string, toTime: string, excludeNo: string) {
  const start = minutes(fromTime)
  const endMinute = minutes(toTime)
  const end = endMinute <= start ? endMinute + 1440 : endMinute
  return rows.value.find((row) => {
    if ((row.issueState !== 0 && row.issueState !== 2) || row.timeStepNo === excludeNo) return false
    const otherStart = minutes(row.fromTime)
    const otherEndMinute = minutes(row.toTime)
    const otherEnd = otherEndMinute <= otherStart ? otherEndMinute + 1440 : otherEndMinute
    return overlaps(start, end, otherStart, otherEnd)
  })
}
function currentTimestamp() {
  const date = new Date()
  const pad = (value: number) => String(value).padStart(2, '0')
  return date.getFullYear() + '-' + pad(date.getMonth() + 1) + '-' + pad(date.getDate()) + ' ' + pad(date.getHours()) + ':' + pad(date.getMinutes()) + ':' + pad(date.getSeconds())
}
function saveForm(closeAfterSave: boolean) {
  if (!formMode.value) return
  const rawNo = draft.timeStepNo.trim()
  if (!rawNo) { formError.value = 'TimeStep No 不可空白。'; return }
  if (!/^\d{1,2}$/.test(rawNo) || Number(rawNo) < 1 || Number(rawNo) > 15) { formError.value = '請選擇 01–15 的 TimeStep No。'; return }
  if (!draft.timeStepName.trim()) { formError.value = 'TimeStep Name 不可空白。'; return }
  if (draft.timeStepName.trim().length > 50) { formError.value = 'TimeStep Name 最多 50 個字元。'; return }
  if (!draft.fromTime || !draft.toTime) { formError.value = 'From 與 To 時間都必須填寫。'; return }
  const timeStepNo = String(Number(rawNo)).padStart(2, '0')
  const duplicate = rows.value.find((row) => row.timeStepNo === timeStepNo && row.timeStepNo !== editingNo.value)
  if (duplicate) { formError.value = 'TimeStep No ' + timeStepNo + ' 已存在。'; return }
  const conflict = findOverlap(draft.fromTime, draft.toTime, formMode.value === 'edit' ? editingNo.value : '')
  if (conflict) { formError.value = '時段與 ' + conflict.timeStepNo + ' ' + conflict.timeStepName + ' 重疊。'; return }

  const savedNo = timeStepNo
  if (formMode.value === 'edit') {
    const index = rows.value.findIndex((row) => row.timeStepNo === editingNo.value)
    if (index < 0 || rows.value[index].issueState !== 0) { formError.value = '資料已變更；只有 UNFROZEN 時段可以編輯。'; return }
    const original = rows.value[index]
    rows.value[index] = {
      ...original,
      timeStepName: draft.timeStepName.trim(),
      fromTime: draft.fromTime,
      toTime: draft.toTime,
      description: draft.description.trim() ? draft.description : original.description,
    }
  } else {
    rows.value.push({
      timeStepNo,
      timeStepName: draft.timeStepName.trim(),
      fromTime: draft.fromTime,
      toTime: draft.toTime,
      description: draft.description,
      creator: 'DEMO',
      createDate: currentTimestamp(),
      issueState: 0,
    })
  }
  announce(formMode.value === 'edit' ? '時段 ' + savedNo + ' 已更新。' : '時段 ' + savedNo + ' 已新增為 UNFROZEN。')
  formDirty.value = false
  if (closeAfterSave) {
    closeForm()
    activeFilter.value = 0
    selectedNos.value = [savedNo]
  }
}
function askFor(action: PendingAction) {
  if (!selectedRows.value.length) {
    announce('請先選取時段。', 'error')
    return
  }
  pendingAction.value = action
}
function applyPendingAction() {
  if (!pendingAction.value) return
  const action = pendingAction.value
  const targets = selectedRows.value.slice()
  if (action === 'approve') {
    const invalid = targets.find((row) => row.issueState !== 0)
    if (invalid) {
      announce(invalid.timeStepNo + ' 不是 UNFROZEN 狀態，無法核准。', 'error')
    } else {
      targets.forEach((target) => {
        const row = rows.value.find((item) => item.timeStepNo === target.timeStepNo)
        if (row) row.issueState = 2
      })
      activeFilter.value = 2
      selectedNos.value = targets.map((row) => row.timeStepNo)
      announce(targets.length + ' 筆時段已設為 ACTIVE。')
    }
  } else {
    let changed = 0
    let blocked = ''
    for (const target of targets) {
      const row = rows.value.find((item) => item.timeStepNo === target.timeStepNo)
      if (!row) continue
      if (row.issueState === 1) { blocked = row.timeStepNo + ' 為 PENDING，舊服務不允許刪除。'; break }
      if (row.issueState === 2) row.issueState = -1
      else rows.value = rows.value.filter((item) => item.timeStepNo !== row.timeStepNo)
      changed += 1
    }
    selectedNos.value = []
    if (blocked) announce((changed ? changed + ' 筆已處理；' : '') + blocked, 'error')
    else announce(changed + ' 筆時段已刪除或停用。')
  }
  pendingAction.value = null
}
function statusLabel(issueState: IssueState) {
  return statusFilters.find((item) => item.value === issueState)?.label.split(' ')[0] ?? '未知'
}
function markDirty() { formDirty.value = true; formError.value = '' }
function formatTimestamp(value: string) { return value }
onUnmounted(() => { if (noticeTimer) clearTimeout(noticeTimer) })
</script>

<template>
  <section class="time-step-board" aria-label="產出看板時段主檔 DEV 預覽">
    <header class="page-heading">
      <div>
        <p class="eyebrow">RPT · RT204</p>
        <h1>產出看板時段主檔</h1>
        <p class="subtitle">維護看板使用的日內時段；核准後提供 RT205–RT207 產出看板使用。</p>
      </div>
      <button type="button" class="icon-button" aria-label="關閉" @click="emit('close')">×</button>
    </header>

    <nav class="status-tabs" role="tablist" aria-label="依核准狀態篩選">
      <button v-for="filter in statusFilters" :key="filter.value" type="button" role="tab" :aria-selected="activeFilter === filter.value" :class="{ active: activeFilter === filter.value }" @click="setFilter(filter.value)">
        <span>{{ filter.label }}</span><strong>{{ rows.filter((row) => row.issueState === filter.value).length }}</strong>
      </button>
    </nav>

    <div class="toolbar">
      <div class="selection-note"><strong>{{ activeCount }}</strong> 筆 {{ statusFilters.find((item) => item.value === activeFilter)?.label }}</div>
      <div class="actions">
        <button type="button" class="primary-button" @click="openAdd">Add</button>
        <button type="button" class="secondary-button" :disabled="selectedRows.length !== 1" @click="openEdit">Edit</button>
        <button type="button" class="danger-button" :disabled="selectedRows.length === 0" @click="askFor('delete')">Del</button>
        <button type="button" class="secondary-button" :disabled="selectedRows.length !== 1" @click="openCopy">Copy</button>
        <button type="button" class="approve-button" :disabled="selectedRows.length === 0" @click="askFor('approve')">Approve</button>
        <button type="button" class="secondary-button" @click="emit('close')">Close</button>
      </div>
    </div>

    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th class="check-cell"><input type="checkbox" :checked="allVisibleSelected" :aria-label="'選取目前 ' + visibleRows.length + ' 筆'" @change="toggleVisible(($event.target as HTMLInputElement).checked)"></th>
            <th>TimeStep No</th><th>TimeStep Name</th><th>FromTime</th><th>ToTime</th><th>Description</th><th>Creator</th><th>CreateDate</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="row in visibleRows" :key="row.timeStepNo" :class="{ selected: selectedNos.includes(row.timeStepNo) }">
            <td class="check-cell"><input type="checkbox" :checked="selectedNos.includes(row.timeStepNo)" :aria-label="'選取 ' + row.timeStepNo" @change="toggleRow(row.timeStepNo, ($event.target as HTMLInputElement).checked)"></td>
            <td class="code-cell">{{ row.timeStepNo }}</td>
            <td><strong>{{ row.timeStepName }}</strong><span class="state-pill" :class="'state-' + String(row.issueState).replace('-', 'minus')">{{ statusLabel(row.issueState) }}</span></td>
            <td>{{ row.fromTime }}</td><td>{{ row.toTime }}<small v-if="row.toTime <= row.fromTime" class="overnight">+1 day</small></td>
            <td class="description-cell">{{ row.description || '—' }}</td><td>{{ row.creator }}</td><td class="date-cell">{{ formatTimestamp(row.createDate) }}</td>
          </tr>
          <tr v-if="visibleRows.length === 0"><td colspan="8" class="empty-row">此狀態沒有時段。</td></tr>
        </tbody>
      </table>
    </div>
    <footer class="page-footer"><span>FromTime／ToTime 為日內時間；To 不晚於 From 時表示跨午夜。</span><span>DEV 假資料 · 只保存在本頁記憶體</span></footer>

    <div v-if="formMode" class="modal-backdrop" @click.self="closeForm">
      <form class="dialog form-dialog" aria-labelledby="form-title" @submit.prevent="saveForm(true)">
        <header class="dialog-header">
          <div><p class="eyebrow">RT204 · {{ formMode.toUpperCase() }}</p><h2 id="form-title">{{ formTitle }}</h2></div>
          <button type="button" class="icon-button" aria-label="關閉" @click="closeForm">×</button>
        </header>
        <label class="field">TimeStep No
          <select v-model="draft.timeStepNo" :disabled="formMode === 'edit'" @change="markDirty">
            <option value="">選擇 01–15</option><option v-for="value in timeStepOptions" :key="value" :value="value">{{ value }}</option>
          </select>
        </label>
        <label class="field">TimeStep Name
          <input v-model="draft.timeStepName" type="text" maxlength="50" autocomplete="off" @input="markDirty">
        </label>
        <div class="time-fields">
          <label class="field">From <input v-model="draft.fromTime" type="time" step="60" required @input="markDirty"></label>
          <label class="field">To <input v-model="draft.toTime" type="time" step="60" required @input="markDirty"></label>
        </div>
        <label class="field">Description
          <textarea v-model="draft.description" rows="4" @input="markDirty"></textarea>
        </label>
        <p class="dialog-note">重疊檢查涵蓋 UNFROZEN／ACTIVE，允許前一段的 To 與下一段的 From 相接。清空既有 Description 時，DEV 保留原值以貼近舊 Service 行為。</p>
        <p v-if="formError" class="form-error" role="alert">{{ formError }}</p>
        <footer class="dialog-actions">
          <button type="button" class="secondary-button" @click="closeForm">Cancel</button>
          <button type="button" class="secondary-button" :disabled="!formDirty" @click="saveForm(false)">Apply</button>
          <button type="submit" class="primary-button">Confirm</button>
        </footer>
      </form>
    </div>

    <div v-if="pendingAction" class="modal-backdrop" @click.self="pendingAction = null">
      <section class="dialog confirm-dialog" role="alertdialog" aria-modal="true" aria-labelledby="confirm-title">
        <header class="dialog-header"><div><p class="eyebrow">RT204 · DEV</p><h2 id="confirm-title">{{ confirmTitle }}</h2></div></header>
        <p class="confirm-text">{{ confirmText }}</p>
        <p class="dialog-note">刪除 UNFROZEN／UNUSED 會移除假資料列；ACTIVE 轉為 UNUSED；PENDING 不允許刪除。</p>
        <footer class="dialog-actions">
          <button type="button" class="secondary-button" @click="pendingAction = null">Cancel</button>
          <button type="button" class="danger-button" @click="applyPendingAction">Confirm</button>
        </footer>
      </section>
    </div>
    <div v-if="notice" class="toast" :class="'toast-' + noticeTone" role="status">{{ notice }}</div>
  </section>
</template>

<style scoped>
.time-step-board{box-sizing:border-box;height:100%;min-height:0;padding:22px 26px;display:flex;flex-direction:column;gap:14px;overflow:auto;background:#eff3f6;color:#243746;font-family:Inter,"Noto Sans TC",sans-serif}
.page-heading,.toolbar,.status-tabs,.status-tabs button,.actions,.dialog-header,.dialog-actions,.time-fields{display:flex;align-items:center;justify-content:space-between;gap:10px}
.page-heading h1{margin:3px 0 4px;font-size:24px}.eyebrow{margin:0;color:#527a99;font-size:11px;font-weight:800;letter-spacing:.1em}.subtitle{margin:0;color:#718493;font-size:12px}
.icon-button{width:32px;height:32px;border:1px solid #d4dfe6;border-radius:7px;background:#fff;color:#40596c;font-size:20px;cursor:pointer}
.status-tabs{justify-content:flex-start;flex-wrap:wrap;padding:5px;border:1px solid #d7e1e7;border-radius:9px;background:#e5ebef}
.status-tabs button{justify-content:center;min-height:37px;padding:7px 11px;border:1px solid transparent;border-radius:6px;background:transparent;color:#637887;font:inherit;font-size:11px;cursor:pointer}
.status-tabs button strong{min-width:21px;padding:2px 5px;border-radius:10px;background:#d5dfe5;color:#425968;font-size:10px}
.status-tabs button.active{border-color:#c9d8e1;background:#fff;color:#244960;box-shadow:0 2px 5px #2e48540c}.status-tabs button.active strong{background:#deebf2;color:#245976}
.toolbar{flex-wrap:wrap}.selection-note{color:#6e808c;font-size:11px}.selection-note strong{color:#304a5b}
.actions{justify-content:flex-end;flex-wrap:wrap}
.primary-button,.secondary-button,.danger-button,.approve-button{min-height:31px;padding:6px 12px;border:1px solid transparent;border-radius:5px;font:inherit;font-size:11px;font-weight:700;cursor:pointer}
.primary-button{background:#1e6f9c;color:#fff}.secondary-button{border-color:#d0dbe2;background:#fff;color:#40596c}.danger-button{background:#b94d45;color:#fff}.approve-button{background:#43825c;color:#fff}
.primary-button:disabled,.secondary-button:disabled,.danger-button:disabled,.approve-button:disabled{opacity:.45;cursor:not-allowed}
.table-wrap{flex:1;min-height:180px;overflow:auto;border:1px solid #d8e2e8;border-radius:9px;background:#fff}
table{width:100%;min-width:800px;border-collapse:collapse;white-space:nowrap;font-size:11px}
th,td{padding:10px 11px;border-bottom:1px solid #e8edf0;text-align:left}
thead th{position:sticky;top:0;z-index:1;background:#f2f6f8;color:#587080;font-size:10px}
tbody tr:hover{background:#f7fafb}tbody tr.selected{background:#e8f3f8}
.check-cell{width:34px;text-align:center}.check-cell input{accent-color:#1e6f9c}
.code-cell{font-family:ui-monospace,Consolas,monospace;font-weight:800;color:#285c78}
.state-pill{display:inline-flex;margin-left:8px;padding:3px 6px;border-radius:10px;font-size:9px;font-weight:800}
.state-0{background:#fff1cc;color:#89610a}.state-1{background:#eee8ff;color:#6047a0}.state-2{background:#dff2e6;color:#36764d}.state-minus1{background:#e8ecef;color:#74818a}
.overnight{display:block;color:#8a7250;font-size:9px}.description-cell{max-width:260px;overflow:hidden;text-overflow:ellipsis;color:#657987}.date-cell{color:#6e808c;font-variant-numeric:tabular-nums}
.empty-row{padding:34px;text-align:center;color:#81919c}
.page-footer{display:flex;justify-content:space-between;gap:10px;flex-wrap:wrap;color:#738490;font-size:10px}
.modal-backdrop{position:fixed;inset:0;z-index:80;display:grid;place-items:center;padding:20px;background:#11233575;backdrop-filter:blur(2px)}
.dialog{box-sizing:border-box;width:min(560px,100%);max-height:90vh;overflow:auto;padding:20px;border:1px solid #d4dfe6;border-radius:12px;background:#fff;box-shadow:0 24px 70px #172a3c42;color:#263b49}
.dialog-header h2{margin:3px 0 0;font-size:19px}.dialog-actions{justify-content:flex-end;margin-top:15px}.field{display:grid;gap:6px;margin:12px 0;color:#526a7b;font-size:11px;font-weight:700}
.field input,.field select,.field textarea{box-sizing:border-box;width:100%;min-height:35px;padding:8px 10px;border:1px solid #d4dfe6;border-radius:5px;background:#fff;color:#263b49;font:inherit}
.field textarea{resize:vertical}.field select:disabled{background:#f0f3f5;color:#7a8992}.time-fields{align-items:flex-start}.time-fields .field{flex:1}
.dialog-note{margin:12px 0 4px;color:#7b8b96;font-size:10px;line-height:1.55}.form-error{margin:9px 0;color:#b43d36;font-size:11px}
.confirm-dialog{width:min(420px,100%)}.confirm-text{margin:20px 0 8px;font-size:14px;font-weight:700;color:#314b5c}
.toast{position:fixed;right:24px;bottom:24px;z-index:110;max-width:460px;padding:11px 14px;border:1px solid;border-radius:7px;font-size:11px;box-shadow:0 8px 30px #172a3c32}
.toast-success{border-color:#b9d8c4;background:#f2fbf5;color:#39754d}.toast-error{border-color:#e5c0bd;background:#fff5f4;color:#a54039}
@media(max-width:900px){.time-step-board{padding:16px}.toolbar{align-items:flex-start;flex-direction:column}.actions{justify-content:flex-start}}
@media(max-width:600px){.time-step-board{padding:12px;gap:10px}.page-heading h1{font-size:19px}.status-tabs{justify-content:stretch}.status-tabs button{flex:1;flex-direction:column;gap:4px;padding:6px}.page-footer{align-items:flex-start;flex-direction:column}.time-fields{gap:8px}.dialog{padding:16px}}
</style>
