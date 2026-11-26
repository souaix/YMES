<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type IssueState = 0 | 1 | 2 | -1
type FormMode = 'add' | 'edit' | 'copy'
interface SubstitutionType {
  substitutionType: string; description: string; creator: string; createDate: string
  reviser: string; reviseDate: string; issueState: IssueState; dataStamp: number
}
interface DevState { version: 1; approvalWorkflowConfigured: boolean; records: SubstitutionType[] }

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.pm.substitution-type.v1'
const currentUser = 'DEV01'
const issueTabs: Array<{ value: IssueState; label: string }> = [
  { value: 0, label: '未凍結' }, { value: 1, label: '簽核中' }, { value: 2, label: '已生效' }, { value: -1, label: '未使用' },
]

function seedState(): DevState {
  return {
    version: 1,
    approvalWorkflowConfigured: false,
    records: [
      { substitutionType: 'BEARING', description: '軸承類備品', creator: 'PMUSER', createDate: '2026-11-18 09:15', reviser: '', reviseDate: '', issueState: 2, dataStamp: 2 },
      { substitutionType: 'MOTOR', description: '馬達與驅動備品', creator: 'DEV02', createDate: '2026-11-20 10:30', reviser: '', reviseDate: '', issueState: 0, dataStamp: 0 },
      { substitutionType: 'PNEUMATIC', description: '氣壓元件', creator: 'PMUSER', createDate: '2026-11-21 13:40', reviser: 'PMUSER', reviseDate: '2026-11-21 13:42', issueState: 1, dataStamp: 1 },
      { substitutionType: 'LEGACY', description: '舊分類範例', creator: 'PMUSER', createDate: '2026-10-01 08:00', reviser: 'PMUSER', reviseDate: '2026-10-10 16:10', issueState: -1, dataStamp: 2 },
    ],
  }
}

function loadState(): DevState {
  try {
    const raw = window.localStorage.getItem(storageKey)
    if (raw) {
      const parsed = JSON.parse(raw) as DevState
      if (parsed?.version === 1 && Array.isArray(parsed.records)) return parsed
    }
  } catch { /* Fall back to local DEV fixtures when storage is unavailable. */ }
  return seedState()
}

const state = ref<DevState>(loadState())
const issueState = ref<IssueState>(0)
const searchCode = ref('')
const activeCode = ref('')
const selectedCodes = ref<string[]>([])
const mode = ref<FormMode | null>(null)
const draft = ref<SubstitutionType | null>(null)
const message = ref('')
const messageKind = ref<'success' | 'error'>('success')
const nowLabel = computed(() => new Date().toLocaleString('zh-TW', { hour12: false }))
const visibleRows = computed(() => state.value.records
  .filter((record) => record.issueState === issueState.value)
  .filter((record) => !searchCode.value.trim() || record.substitutionType.toLocaleLowerCase().includes(searchCode.value.trim().toLocaleLowerCase())))
const activeRecord = computed(() => state.value.records.find((record) => record.substitutionType === activeCode.value) ?? null)
const selectedTargets = computed(() => {
  const selected = state.value.records.filter((record) => selectedCodes.value.includes(record.substitutionType))
  return selected.length ? selected : activeRecord.value ? [activeRecord.value] : []
})
const canEdit = computed(() => activeRecord.value?.issueState === 0)
const selectedAllVisible = computed(() => visibleRows.value.length > 0 && visibleRows.value.every((record) => selectedCodes.value.includes(record.substitutionType)))
const issueStateLabel = (value: IssueState) => issueTabs.find((tab) => tab.value === value)?.label ?? '未知'

watch(state, (value) => {
  try { window.localStorage.setItem(storageKey, JSON.stringify(value)) } catch { /* Keep in-memory DEV changes for this session. */ }
}, { deep: true })
watch(visibleRows, (rows) => {
  if (!rows.some((record) => record.substitutionType === activeCode.value)) activeCode.value = rows[0]?.substitutionType ?? ''
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
  if (!selectedCodes.value.includes(code)) selectedCodes.value = [...selectedCodes.value, code]
}

function toggleRecord(code: string) {
  activeCode.value = code
  selectedCodes.value = selectedCodes.value.includes(code) ? selectedCodes.value.filter((value) => value !== code) : [...selectedCodes.value, code]
}

function toggleAllVisible() {
  const visible = visibleRows.value.map((record) => record.substitutionType)
  selectedCodes.value = selectedAllVisible.value
    ? selectedCodes.value.filter((code) => !visible.includes(code))
    : [...new Set([...selectedCodes.value, ...visible])]
}

function emptyRecord(): SubstitutionType {
  return { substitutionType: '', description: '', creator: '', createDate: '', reviser: '', reviseDate: '', issueState: 0, dataStamp: 0 }
}

function openForm(nextMode: FormMode) {
  if (nextMode !== 'add' && !activeRecord.value) return
  mode.value = nextMode
  draft.value = nextMode === 'add' ? emptyRecord() : { ...activeRecord.value! }
}

function saveForm() {
  if (!draft.value || !mode.value) return
  const modeAtSave = mode.value
  const code = draft.value.substitutionType.trim().toUpperCase()
  if (!code) return toast('替代料類別不可空白。', 'error')
  const original = activeRecord.value
  if (modeAtSave === 'edit' && !original) return
  if (state.value.records.some((record) => record.substitutionType === code && (modeAtSave !== 'edit' || record.substitutionType !== original?.substitutionType))) return toast('類別代碼已存在。', 'error')
  if (modeAtSave === 'edit') {
    const index = state.value.records.findIndex((record) => record.substitutionType === original!.substitutionType)
    if (index < 0 || state.value.records[index].dataStamp !== original!.dataStamp) return toast('資料已變更，請重新選取後再修改。', 'error')
    state.value.records[index] = { ...original!, description: draft.value.description, reviser: currentUser, reviseDate: nowLabel.value, dataStamp: original!.dataStamp + 1 }
    toast('備品類別已修改。')
  } else {
    const created: SubstitutionType = { ...draft.value, substitutionType: code, creator: currentUser, createDate: nowLabel.value, reviser: '', reviseDate: '', issueState: 0, dataStamp: 0 }
    state.value.records.unshift(created)
    issueState.value = 0
    activeCode.value = code
    selectedCodes.value = [code]
    toast(modeAtSave === 'copy' ? '已複製為新的未凍結類別。' : '備品類別已新增。')
  }
  mode.value = null
  draft.value = null
}

function approveSelected() {
  const targets = selectedTargets.value.filter((record) => record.issueState === 0)
  if (!targets.length) return toast('請選取未凍結資料。', 'error')
  const nextState: IssueState = state.value.approvalWorkflowConfigured ? 1 : 2
  if (!window.confirm(`將 ${targets.length} 筆資料送出${nextState === 1 ? '簽核' : '並直接生效'}？`)) return
  for (const record of targets) {
    record.issueState = nextState
    record.reviser = currentUser
    record.reviseDate = nowLabel.value
    record.dataStamp += 1
  }
  selectedCodes.value = []
  toast(nextState === 1 ? 'DEV 模擬已送入簽核中，未呼叫郵件或簽核服務。' : 'DEV 模擬已直接生效。')
}

function deleteSelected() {
  if (!selectedTargets.value.length) return toast('請先選取資料。', 'error')
  if (selectedTargets.value.some((record) => record.issueState === 1)) return toast('簽核中的資料不可刪除。', 'error')
  if (!window.confirm(`確認處理 ${selectedTargets.value.length} 筆？未凍結／未使用會硬刪，已生效會改為未使用。`)) return
  const remove = new Set(selectedTargets.value.filter((record) => record.issueState === 0 || record.issueState === -1).map((record) => record.substitutionType))
  for (const record of selectedTargets.value) {
    if (record.issueState === 2) {
      record.issueState = -1
      record.reviser = currentUser
      record.reviseDate = nowLabel.value
      record.dataStamp += 1
    }
  }
  state.value.records = state.value.records.filter((record) => !remove.has(record.substitutionType))
  selectedCodes.value = []
  toast('備品類別處理完成。')
}

function resetFixtures() {
  if (!window.confirm('重設本頁 DEV 假資料？')) return
  state.value = seedState()
  issueState.value = 0
  searchCode.value = ''
  selectedCodes.value = []
  toast('DEV 假資料已重設。')
}
</script>

<template>
  <section class="pm-type" aria-label="備品類別資料 DEV 預覽">
    <header class="pm-type__header"><div><div class="title-row"><span class="app-mark">PM</span><h1>備品類別資料</h1><span class="dev-tag">DEV 假資料</span></div><p>維護設備備品的替代料類別與簽核狀態。</p></div><button type="button" class="close" aria-label="關閉" @click="emit('close')">×</button></header>
    <div class="toolbar"><button class="primary" type="button" @click="openForm('add')">新增</button><button type="button" :disabled="!canEdit" @click="openForm('edit')">修改</button><button type="button" :disabled="!activeRecord" @click="openForm('copy')">複製</button><button type="button" :disabled="!selectedTargets.length || selectedTargets.some((record) => record.issueState === 1)" @click="deleteSelected">刪除</button><button type="button" :disabled="!selectedTargets.some((record) => record.issueState === 0)" @click="approveSelected">核准／送簽</button><span class="spacer"></span><label class="workflow"><input v-model="state.approvalWorkflowConfigured" type="checkbox"> DEV 模擬已設定簽核流程</label><button type="button" class="quiet" @click="resetFixtures">重設假資料</button></div>
    <nav class="tabs" aria-label="簽核狀態"><button v-for="tab in issueTabs" :key="tab.value" type="button" :class="{ active: issueState === tab.value }" @click="selectIssueState(tab.value)">{{ tab.label }}<span>{{ state.records.filter((record) => record.issueState === tab.value).length }}</span></button></nav>
    <div class="filter"><label>替代料類別 <input v-model="searchCode" type="search" placeholder="輸入類別代碼搜尋"></label><span>查詢欄位依舊系統：SubstitutionType</span><b>{{ visibleRows.length }} 筆</b></div>
    <div class="table-wrap"><table><thead><tr><th class="check"><input type="checkbox" :checked="selectedAllVisible" aria-label="選取全部可見資料" @change="toggleAllVisible"></th><th>替代料類別</th><th>說明</th><th>建立者</th><th>建立時間</th><th>修改者</th><th>修改時間</th></tr></thead><tbody>
      <tr v-for="record in visibleRows" :key="record.substitutionType" :class="{ selected: activeCode === record.substitutionType }" @click="selectRecord(record.substitutionType)"><td class="check" @click.stop><input type="checkbox" :checked="selectedCodes.includes(record.substitutionType)" :aria-label="'選取 ' + record.substitutionType" @change="toggleRecord(record.substitutionType)"></td><td class="mono">{{ record.substitutionType }}</td><td class="description">{{ record.description }}</td><td>{{ record.creator }}</td><td>{{ record.createDate }}</td><td>{{ record.reviser || '—' }}</td><td>{{ record.reviseDate || '—' }}</td></tr><tr v-if="!visibleRows.length"><td colspan="7" class="empty">目前狀態沒有符合條件的資料。</td></tr>
    </tbody></table></div>
    <footer class="page-footer"><span>狀態：<strong>{{ issueStateLabel(issueState) }}</strong></span><span>DEV 操作僅保存在此瀏覽器 localStorage。</span></footer>
    <Transition><div v-if="message" class="toast" :class="messageKind" role="status">{{ message }}</div></Transition>
    <div v-if="mode && draft" class="backdrop" @click.self="mode = null"><section class="dialog" role="dialog" aria-modal="true" aria-label="備品類別編輯"><header><div><h2>{{ mode === 'add' ? '新增備品類別' : mode === 'copy' ? '複製備品類別' : '修改備品類別' }}</h2><p>代碼儲存前會去除前後空白並轉成大寫。</p></div><button type="button" @click="mode = null">×</button></header><form @submit.prevent="saveForm"><div class="form-grid"><label>替代料類別<input v-model="draft.substitutionType" :readonly="mode === 'edit'" required autocomplete="off" @blur="draft.substitutionType = draft.substitutionType.trim().toUpperCase()"></label><label class="wide">說明<textarea v-model="draft.description" rows="4"></textarea></label></div><footer><button type="button" @click="mode = null">取消</button><button class="primary" type="submit">儲存</button></footer></form></section></div>
  </section>
</template>

<style scoped>
.pm-type { height: 100%; min-height: 0; display: flex; flex-direction: column; gap: 10px; padding: 16px; box-sizing: border-box; background: #f4f3ee; color: #26251f; font: 13px/1.45 "Segoe UI", "Microsoft JhengHei", sans-serif; }
.pm-type button, .pm-type input, .pm-type textarea { font: inherit; }
.pm-type button { min-height: 30px; padding: 4px 12px; border: 1px solid #c9c7bd; border-radius: 3px; background: #fffefa; color: #333229; cursor: pointer; }
.pm-type button:hover:not(:disabled) { border-color: #787467; background: #f5f3e9; }
.pm-type button:disabled { opacity: .45; cursor: not-allowed; }
.pm-type button.primary { border-color: #425746; background: #425746; color: white; }
.pm-type button.quiet { background: transparent; }
.pm-type__header { display: flex; align-items: flex-start; justify-content: space-between; border-bottom: 1px solid #d9d7ce; padding-bottom: 10px; }
.pm-type__header h1 { margin: 0; font-size: 20px; font-weight: 600; }
.pm-type__header p { margin: 5px 0 0 40px; color: #777366; }
.pm-type .title-row { display: flex; align-items: center; gap: 9px; }
.pm-type .app-mark { display: grid; place-items: center; width: 30px; height: 28px; border-radius: 3px; background: #526855; color: white; font-weight: 700; }
.pm-type .dev-tag { padding: 2px 7px; border-radius: 10px; background: #e8e2bd; color: #574d1f; font-size: 11px; }
.pm-type button.close { min-width: 32px; padding: 0; font-size: 22px; }
.pm-type .toolbar { display: flex; align-items: center; gap: 6px; flex-wrap: wrap; }
.pm-type .spacer { flex: 1; }
.pm-type .workflow { display: inline-flex; align-items: center; gap: 5px; color: #68655b; font-size: 12px; }
.pm-type .workflow input { accent-color: #526855; }
.pm-type .tabs { display: flex; gap: 2px; border-bottom: 1px solid #cac8be; }
.pm-type .tabs button { min-height: 34px; border: 0; border-radius: 3px 3px 0 0; background: transparent; color: #716e63; }
.pm-type .tabs button.active { border-bottom: 2px solid #4a674e; color: #2d4732; font-weight: 600; }
.pm-type .tabs span { margin-left: 7px; padding: 1px 6px; border-radius: 9px; background: #e5e3da; font-size: 11px; }
.pm-type .filter { display: flex; align-items: end; gap: 12px; color: #898679; font-size: 11px; }
.pm-type .filter label { display: flex; align-items: center; gap: 7px; color: #39372e; }
.pm-type .filter input, .pm-type .form-grid input, .pm-type textarea { box-sizing: border-box; min-height: 30px; padding: 5px 7px; border: 1px solid #c9c7bd; border-radius: 3px; background: #fffefa; color: inherit; }
.pm-type .filter input { width: 220px; }
.pm-type .filter b { margin-left: auto; color: #777366; font-weight: 400; }
.pm-type .table-wrap { min-height: 120px; flex: 1; overflow: auto; border: 1px solid #ceccc2; background: #fffefa; }
.pm-type table { width: 100%; min-width: 800px; border-collapse: collapse; white-space: nowrap; }
.pm-type th { position: sticky; top: 0; z-index: 1; padding: 7px 8px; border-bottom: 1px solid #c8c6bc; background: #eae9e1; color: #4e4b41; text-align: left; font-size: 12px; font-weight: 600; }
.pm-type td { padding: 7px 8px; border-bottom: 1px solid #eeece5; }
.pm-type tbody tr { cursor: pointer; }
.pm-type tbody tr:hover { background: #f6f5ef; }
.pm-type tbody tr.selected { background: #e8eee5; }
.pm-type th.check, .pm-type td.check { width: 30px; text-align: center; }
.pm-type .mono { font-family: Consolas, monospace; }
.pm-type .description { max-width: 380px; overflow: hidden; text-overflow: ellipsis; }
.pm-type .empty { padding: 35px; color: #858174; text-align: center; }
.pm-type .page-footer { display: flex; justify-content: space-between; color: #817e73; font-size: 11px; }
.pm-type .page-footer strong { color: #47443a; }
.pm-type .toast { position: fixed; right: 24px; bottom: 24px; z-index: 30; max-width: 440px; padding: 11px 16px; border: 1px solid #c4d2c1; border-radius: 4px; background: #f0f7ee; box-shadow: 0 4px 18px #0002; }
.pm-type .toast.error { border-color: #d7b7ad; background: #fcf1ed; color: #842d20; }
.pm-type .backdrop { position: fixed; inset: 0; z-index: 20; display: grid; place-items: center; padding: 18px; background: #25251f99; }
.pm-type .dialog { width: min(520px, 100%); max-height: min(90vh, 600px); overflow: auto; border: 1px solid #a9a79d; border-radius: 5px; background: #f9f8f3; box-shadow: 0 12px 40px #0004; }
.pm-type .dialog > header { display: flex; align-items: flex-start; justify-content: space-between; padding: 14px 18px 11px; border-bottom: 1px solid #dddacf; }
.pm-type .dialog h2 { margin: 0; font-size: 17px; }
.pm-type .dialog header p { margin: 4px 0 0; color: #817e73; font-size: 11px; }
.pm-type .dialog header button { min-width: 28px; padding: 0; border: 0; background: transparent; font-size: 20px; }
.pm-type .dialog form { padding: 16px 18px 14px; }
.pm-type .form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 13px 15px; }
.pm-type .form-grid label { display: flex; flex-direction: column; gap: 5px; }
.pm-type .form-grid .wide { grid-column: 1 / -1; }
.pm-type .form-grid textarea { resize: vertical; }
.pm-type .dialog form > footer { display: flex; justify-content: flex-end; gap: 7px; margin-top: 18px; padding-top: 12px; border-top: 1px solid #dddacf; }
.pm-type .v-enter-active, .pm-type .v-leave-active { transition: opacity .14s ease; }
.pm-type .v-enter-from, .pm-type .v-leave-to { opacity: 0; }
@media (max-width: 760px) { .pm-type { padding: 10px; } .pm-type .spacer { display: none; } .pm-type .workflow { width: 100%; } .pm-type .filter { flex-wrap: wrap; } .pm-type .page-footer { flex-direction: column; gap: 3px; } }
</style>
