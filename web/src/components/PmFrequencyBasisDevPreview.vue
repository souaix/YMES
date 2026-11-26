<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type IssueState = 0 | 1 | 2 | -1
type FixedType = 0 | 1 | 2 | 3
type FormMode = 'add' | 'edit' | 'copy'
interface FrequencyBasis {
  frequencyNo: string
  frequencyName: string
  priority: number
  fixedType: FixedType
  fixedTypeValue: string
  period: number | null
  description: string
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
  issueState: IssueState
  dataStamp: number
}
interface DevState { version: 1; records: FrequencyBasis[]; approvalWorkflowConfigured: boolean }

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.pm.frequency-basis.v1'
const currentUser = 'DEV01'
const issueTabs: Array<{ value: IssueState; label: string }> = [
  { value: 0, label: '未凍結' }, { value: 1, label: '簽核中' }, { value: 2, label: '已生效' }, { value: -1, label: '未使用' },
]
const fixedTypes: Array<{ value: FixedType; label: string }> = [
  { value: 0, label: '期間（無固定日期）' }, { value: 1, label: '每週' }, { value: 2, label: '每月' }, { value: 3, label: '每年' },
]
const weekDays = ['星期日', '星期一', '星期二', '星期三', '星期四', '星期五', '星期六']
const months = Array.from({ length: 12 }, (_, index) => index + 1)

function seedState(): DevState {
  return {
    version: 1,
    approvalWorkflowConfigured: false,
    records: [
      { frequencyNo: 'PM-DAILY-01', frequencyName: '每日點檢', priority: 1, fixedType: 0, fixedTypeValue: '', period: 1, description: '設備每日保養檢查', creator: 'PMUSER', createDate: '2026-11-18 09:15', reviser: '', reviseDate: '', issueState: 2, dataStamp: 3 },
      { frequencyNo: 'PM-WEEKLY-01', frequencyName: '每週保養', priority: 3, fixedType: 1, fixedTypeValue: '1', period: null, description: '星期一執行週保養', creator: 'PMUSER', createDate: '2026-11-19 10:20', reviser: '', reviseDate: '', issueState: 0, dataStamp: 0 },
      { frequencyNo: 'PM-MONTHLY-15', frequencyName: '月中設備校驗', priority: 5, fixedType: 2, fixedTypeValue: '15', period: null, description: '每月 15 日', creator: 'DEV02', createDate: '2026-11-20 14:05', reviser: 'DEV02', reviseDate: '2026-11-21 08:30', issueState: 1, dataStamp: 2 },
      { frequencyNo: 'PM-YEARLY-06', frequencyName: '年度保養', priority: 8, fixedType: 3, fixedTypeValue: '6/30', period: null, description: '每年六月底安排年度保養', creator: 'PMUSER', createDate: '2026-10-05 16:45', reviser: 'PMUSER', reviseDate: '2026-10-06 11:10', issueState: -1, dataStamp: 2 },
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
  } catch { /* Keep working with the in-memory DEV fixtures when storage is unavailable. */ }
  return seedState()
}

const state = ref<DevState>(loadState())
const selectedIssueState = ref<IssueState>(0)
const searchNo = ref('')
const selectedNos = ref<string[]>([])
const mode = ref<FormMode | null>(null)
const draft = ref<FrequencyBasis | null>(null)
const periodInput = ref<number>(0)
const weekDayInput = ref(0)
const monthDayInput = ref(1)
const yearMonthInput = ref(1)
const yearDayInput = ref(1)
const message = ref('')
const messageKind = ref<'success' | 'error'>('success')
const nowLabel = computed(() => new Date().toLocaleString('zh-TW', { hour12: false }))
const records = computed(() => state.value.records)
const visibleRecords = computed(() => records.value
  .filter((record) => record.issueState === selectedIssueState.value)
  .filter((record) => !searchNo.value.trim() || record.frequencyNo.toLocaleLowerCase().includes(searchNo.value.trim().toLocaleLowerCase())))
const selectedRecords = computed(() => selectedNos.value
  .map((frequencyNo) => records.value.find((record) => record.frequencyNo === frequencyNo))
  .filter((record): record is FrequencyBasis => Boolean(record)))
const selectedOne = computed(() => selectedRecords.value.length === 1 ? selectedRecords.value[0] : null)
const canEdit = computed(() => selectedOne.value?.issueState === 0)
const selectedAllVisible = computed(() => visibleRecords.value.length > 0 && visibleRecords.value.every((record) => selectedNos.value.includes(record.frequencyNo)))
const fixedTypeLabel = (value: FixedType) => fixedTypes.find((item) => item.value === value)?.label ?? '未知'
const issueStateLabel = (value: IssueState) => issueTabs.find((item) => item.value === value)?.label ?? '未知'
const displayFixedValue = (record: FrequencyBasis) => {
  if (record.fixedType === 0) return record.period === null ? '—' : `每 ${record.period} 天`
  if (record.fixedType === 1) return weekDays[Number(record.fixedTypeValue)] ?? record.fixedTypeValue
  if (record.fixedType === 2) return `每月 ${record.fixedTypeValue} 日`
  return `每年 ${record.fixedTypeValue.replace('/', ' 月 ')} 日`
}

watch(state, (value) => {
  try { window.localStorage.setItem(storageKey, JSON.stringify(value)) } catch { /* DEV preview remains usable for this page session. */ }
}, { deep: true })
function toast(text: string, kind: 'success' | 'error' = 'success') {
  message.value = text
  messageKind.value = kind
  window.setTimeout(() => { if (message.value === text) message.value = '' }, 3600)
}

function toggleVisibleSelection() {
  const visibleNos = visibleRecords.value.map((record) => record.frequencyNo)
  selectedNos.value = selectedAllVisible.value
    ? selectedNos.value.filter((frequencyNo) => !visibleNos.includes(frequencyNo))
    : [...new Set([...selectedNos.value, ...visibleNos])]
}

function toggleRecordSelection(frequencyNo: string) {
  selectedNos.value = selectedNos.value.includes(frequencyNo)
    ? selectedNos.value.filter((value) => value !== frequencyNo)
    : [...selectedNos.value, frequencyNo]
}

function emptyRecord(): FrequencyBasis {
  return {
    frequencyNo: '', frequencyName: '', priority: 1, fixedType: 0, fixedTypeValue: '', period: 0,
    description: '', creator: '', createDate: '', reviser: '', reviseDate: '', issueState: 0, dataStamp: 0,
  }
}

function openForm(nextMode: FormMode) {
  if (nextMode !== 'add' && !selectedOne.value) return
  const source = selectedOne.value
  mode.value = nextMode
  draft.value = nextMode === 'add' ? emptyRecord() : { ...source! }
  periodInput.value = source?.period ?? 0
  weekDayInput.value = source?.fixedType === 1 ? Number(source.fixedTypeValue) : 0
  monthDayInput.value = source?.fixedType === 2 ? Number(source.fixedTypeValue) : 1
  if (source?.fixedType === 3) {
    const [month, day] = source.fixedTypeValue.split('/').map(Number)
    yearMonthInput.value = month || 1
    yearDayInput.value = day || 1
  } else {
    yearMonthInput.value = 1
    yearDayInput.value = 1
  }
}

const maxYearDay = computed(() => {
  if (yearMonthInput.value === 2) return new Date().getFullYear() % 4 === 0 ? 29 : 28
  return [4, 6, 9, 11].includes(yearMonthInput.value) ? 30 : 31
})

function frequencyValueFromForm(record: FrequencyBasis): string {
  if (record.fixedType === 1) return String(weekDayInput.value)
  if (record.fixedType === 2) return String(monthDayInput.value)
  if (record.fixedType === 3) return `${yearMonthInput.value}/${yearDayInput.value}`
  return ''
}

function saveForm() {
  if (!draft.value || !mode.value) return
  const value = { ...draft.value, frequencyNo: draft.value.frequencyNo.trim(), frequencyName: draft.value.frequencyName.trim() }
  if (!value.frequencyNo) return toast('請輸入頻率代碼。', 'error')
  if (value.frequencyNo.length > 30) return toast('頻率代碼不可超過 30 個字元。', 'error')
  if (!value.frequencyName) return toast('請輸入頻率名稱。', 'error')
  if (value.frequencyName.length > 50) return toast('頻率名稱不可超過 50 個字元。', 'error')
  if (!Number.isInteger(value.priority) || value.priority < 1 || value.priority > 99) return toast('Priority 必須介於 1 到 99。', 'error')
  if (value.fixedType === 0 && (!Number.isFinite(periodInput.value) || periodInput.value < 0)) return toast('期間不可小於 0。', 'error')
  if (value.fixedType === 1 && (weekDayInput.value < 0 || weekDayInput.value > 6)) return toast('請選擇星期。', 'error')
  if (value.fixedType === 2 && (monthDayInput.value < 1 || monthDayInput.value > 31)) return toast('每月日期須介於 1 到 31。', 'error')
  if (value.fixedType === 3 && (yearDayInput.value < 1 || yearDayInput.value > maxYearDay.value)) return toast('請選擇有效的月日。', 'error')
  const duplicate = records.value.some((record) => record.frequencyNo === value.frequencyNo && (mode.value !== 'edit' || record.frequencyNo !== selectedOne.value?.frequencyNo))
  if (duplicate) return toast('頻率代碼已存在；複製時需自行輸入新代碼。', 'error')

  value.fixedTypeValue = frequencyValueFromForm(value)
  value.period = value.fixedType === 0 ? periodInput.value : null
  if (mode.value === 'edit') {
    const original = selectedOne.value
    if (!original) return
    const index = state.value.records.findIndex((record) => record.frequencyNo === original.frequencyNo)
    if (index < 0 || state.value.records[index].dataStamp !== original.dataStamp) return toast('資料已變更，請重新選取後再修改。', 'error')
    state.value.records[index] = { ...original, ...value, frequencyNo: original.frequencyNo, creator: original.creator, createDate: original.createDate, reviser: currentUser, reviseDate: nowLabel.value, issueState: 0, dataStamp: original.dataStamp + 1 }
    toast('頻率資料已修改。')
  } else {
    const created: FrequencyBasis = { ...value, creator: currentUser, createDate: nowLabel.value, reviser: '', reviseDate: '', issueState: 0, dataStamp: 0 }
    state.value.records.unshift(created)
    selectedIssueState.value = 0
    selectedNos.value = [created.frequencyNo]
    toast(mode.value === 'copy' ? '已複製為新的未凍結資料。' : '頻率資料已新增。')
  }
  mode.value = null
  draft.value = null
}

function approveSelected() {
  const targets = selectedRecords.value.filter((record) => record.issueState === 0)
  if (!targets.length) return toast('請選取未凍結資料。', 'error')
  const nextState: IssueState = state.value.approvalWorkflowConfigured ? 1 : 2
  if (!window.confirm(`將 ${targets.length} 筆資料送出${nextState === 1 ? '簽核' : '並直接生效'}？`)) return
  for (const record of targets) {
    record.issueState = nextState
    record.reviser = currentUser
    record.reviseDate = nowLabel.value
    record.dataStamp += 1
  }
  selectedNos.value = []
  toast(nextState === 1 ? 'DEV 模擬已送入簽核中，未呼叫郵件或簽核服務。' : 'DEV 模擬已直接生效。')
}

function deleteSelected() {
  if (!selectedRecords.value.length) return toast('請先選取資料。', 'error')
  if (selectedRecords.value.some((record) => record.issueState === 1)) return toast('簽核中的資料不可刪除。', 'error')
  if (!window.confirm(`確認處理 ${selectedRecords.value.length} 筆資料？未凍結／未使用資料會刪除，已生效資料會標為未使用。`)) return
  const physicalDelete = new Set(selectedRecords.value.filter((record) => record.issueState === 0 || record.issueState === -1).map((record) => record.frequencyNo))
  for (const record of selectedRecords.value) {
    if (record.issueState === 2) {
      record.issueState = -1
      record.reviser = currentUser
      record.reviseDate = nowLabel.value
      record.dataStamp += 1
    }
  }
  state.value.records = state.value.records.filter((record) => !physicalDelete.has(record.frequencyNo))
  selectedNos.value = []
  toast('資料處理完成。')
}

function resetFixtures() {
  if (!window.confirm('重設本頁 DEV 假資料？')) return
  state.value = seedState()
  selectedIssueState.value = 0
  selectedNos.value = []
  searchNo.value = ''
  toast('DEV 假資料已重設。')
}
</script>

<template>
  <section class="pm-frequency" aria-label="頻率基本資料 DEV 預覽">
    <header class="pm-frequency__header">
      <div>
        <div class="pm-frequency__title-row"><span class="pm-frequency__app-mark">PM</span><h1>頻率基本資料</h1><span class="pm-frequency__dev-tag">DEV 假資料</span></div>
        <p>維護設備保養頻率的週期、優先順序與簽核狀態。</p>
      </div>
      <button type="button" class="pm-frequency__close" title="關閉" @click="emit('close')">×</button>
    </header>

    <div class="pm-frequency__toolbar">
      <button class="primary" type="button" @click="openForm('add')">新增</button>
      <button type="button" :disabled="!canEdit" @click="openForm('edit')">修改</button>
      <button type="button" :disabled="!selectedOne" @click="openForm('copy')">複製</button>
      <button type="button" :disabled="!selectedRecords.length || selectedRecords.some((record) => record.issueState === 1)" @click="deleteSelected">刪除</button>
      <button type="button" :disabled="!selectedRecords.some((record) => record.issueState === 0)" @click="approveSelected">核准／送簽</button>
      <span class="pm-frequency__toolbar-spacer"></span>
      <label class="pm-frequency__workflow"><input v-model="state.approvalWorkflowConfigured" type="checkbox"> DEV 模擬已設定簽核流程</label>
      <button type="button" class="quiet" @click="resetFixtures">重設假資料</button>
    </div>

    <nav class="pm-frequency__tabs" aria-label="簽核狀態">
      <button v-for="tab in issueTabs" :key="tab.value" type="button" :class="{ active: selectedIssueState === tab.value }" @click="selectedIssueState = tab.value; selectedNos = []">
        {{ tab.label }}<span>{{ records.filter((record) => record.issueState === tab.value).length }}</span>
      </button>
    </nav>

    <div class="pm-frequency__filters">
      <label>頻率代碼 <input v-model="searchNo" type="search" maxlength="30" placeholder="輸入代碼搜尋"></label>
      <span class="pm-frequency__hint">查詢欄位依舊系統：FrequencyNo</span>
      <span class="pm-frequency__count">{{ visibleRecords.length }} 筆</span>
    </div>

    <div class="pm-frequency__table-wrap">
      <table>
        <thead><tr>
          <th class="check"><input type="checkbox" :checked="selectedAllVisible" aria-label="選取全部可見資料" @change="toggleVisibleSelection"></th>
          <th>頻率代碼</th><th>頻率名稱</th><th>優先順序</th><th>固定方式</th><th>固定值／期間</th><th>說明</th><th>建立者</th><th>建立時間</th><th>修改者</th><th>修改時間</th>
        </tr></thead>
        <tbody>
          <tr v-for="record in visibleRecords" :key="record.frequencyNo" :class="{ selected: selectedNos.includes(record.frequencyNo) }" @click="toggleRecordSelection(record.frequencyNo)">
            <td class="check" @click.stop><input type="checkbox" :checked="selectedNos.includes(record.frequencyNo)" :aria-label="`選取 ${record.frequencyNo}`" @change="toggleRecordSelection(record.frequencyNo)"></td>
            <td class="mono">{{ record.frequencyNo }}</td><td>{{ record.frequencyName }}</td><td class="numeric">{{ record.priority }}</td><td>{{ fixedTypeLabel(record.fixedType) }}</td><td>{{ displayFixedValue(record) }}</td><td class="description">{{ record.description }}</td><td>{{ record.creator }}</td><td>{{ record.createDate }}</td><td>{{ record.reviser || '—' }}</td><td>{{ record.reviseDate || '—' }}</td>
          </tr>
          <tr v-if="!visibleRecords.length"><td colspan="11" class="empty">目前狀態沒有符合條件的資料。</td></tr>
        </tbody>
      </table>
    </div>

    <footer class="pm-frequency__footer">
      <span>狀態：<strong>{{ issueStateLabel(selectedIssueState) }}</strong></span>
      <span>本頁 DEV 操作僅保存在此瀏覽器的 localStorage。</span>
    </footer>

    <Transition><div v-if="message" class="pm-frequency__toast" :class="messageKind" role="status">{{ message }}</div></Transition>

    <div v-if="mode && draft" class="pm-frequency__backdrop" @click.self="mode = null">
      <section class="pm-frequency__dialog" role="dialog" aria-modal="true" :aria-label="mode === 'edit' ? '修改頻率資料' : mode === 'copy' ? '複製頻率資料' : '新增頻率資料'">
        <header><div><h2>{{ mode === 'edit' ? '修改頻率資料' : mode === 'copy' ? '複製頻率資料' : '新增頻率資料' }}</h2><p>來源欄位對應 TBLPMFREQUENCYBASIS；目前由 DEV 假資料模擬。</p></div><button type="button" aria-label="關閉" @click="mode = null">×</button></header>
        <form @submit.prevent="saveForm">
          <div class="pm-frequency__form-grid">
            <label>頻率代碼 <input v-model="draft.frequencyNo" :readonly="mode === 'edit'" maxlength="30" required autocomplete="off"></label>
            <label>頻率名稱 <input v-model="draft.frequencyName" maxlength="50" required></label>
            <label>優先順序 <input v-model.number="draft.priority" type="number" min="1" max="99" step="1" required></label>
            <label>固定方式 <select v-model.number="draft.fixedType"><option v-for="item in fixedTypes" :key="item.value" :value="item.value">{{ item.label }}</option></select></label>
            <label v-if="draft.fixedType === 0" class="wide">期間（日）<input v-model.number="periodInput" type="number" min="0" step="1" required><small>舊表單程式只拒絕小於 0；訊息文字稱須大於 0，0 仍可能通過。</small></label>
            <label v-else-if="draft.fixedType === 1" class="wide">星期 <select v-model.number="weekDayInput"><option v-for="(day, index) in weekDays" :key="day" :value="index">{{ day }}</option></select></label>
            <label v-else-if="draft.fixedType === 2" class="wide">每月日期 <select v-model.number="monthDayInput"><option v-for="day in 31" :key="day" :value="day">{{ day }} 日</option></select></label>
            <div v-else class="pm-frequency__year-grid wide"><label>月份 <select v-model.number="yearMonthInput" @change="yearDayInput = Math.min(yearDayInput, maxYearDay)"><option v-for="month in months" :key="month" :value="month">{{ month }} 月</option></select></label><label>日期 <select v-model.number="yearDayInput"><option v-for="day in maxYearDay" :key="day" :value="day">{{ day }} 日</option></select></label><small>沿用舊畫面以目前年份是否可被 4 整除判斷 2 月天數。</small></div>
            <label class="wide">說明 <textarea v-model="draft.description" rows="4"></textarea></label>
          </div>
          <footer><button type="button" @click="mode = null">取消</button><button class="primary" type="submit">儲存</button></footer>
        </form>
      </section>
    </div>
  </section>
</template>

<style scoped>
.pm-frequency { height: 100%; min-height: 0; display: flex; flex-direction: column; gap: 10px; padding: 16px; box-sizing: border-box; background: #f4f3ee; color: #26251f; font: 13px/1.45 "Segoe UI", "Microsoft JhengHei", sans-serif; }
.pm-frequency button, .pm-frequency input, .pm-frequency select, .pm-frequency textarea { font: inherit; }
.pm-frequency button { min-height: 30px; padding: 4px 12px; border: 1px solid #c9c7bd; border-radius: 3px; background: #fffefa; color: #333229; cursor: pointer; }
.pm-frequency button:hover:not(:disabled) { border-color: #787467; background: #f5f3e9; }
.pm-frequency button:disabled { opacity: .45; cursor: not-allowed; }
.pm-frequency button.primary { border-color: #425746; background: #425746; color: #fff; }
.pm-frequency button.quiet { background: transparent; }
.pm-frequency__header { display: flex; align-items: flex-start; justify-content: space-between; border-bottom: 1px solid #d9d7ce; padding-bottom: 10px; }
.pm-frequency__title-row { display: flex; align-items: center; gap: 9px; }
.pm-frequency__header h1 { margin: 0; font-size: 20px; font-weight: 600; }
.pm-frequency__header p { margin: 5px 0 0 41px; color: #777366; }
.pm-frequency__app-mark { display: grid; place-items: center; width: 31px; height: 28px; border-radius: 3px; background: #526855; color: white; font-weight: 700; }
.pm-frequency__dev-tag { padding: 2px 7px; border-radius: 10px; background: #e8e2bd; color: #574d1f; font-size: 11px; }
.pm-frequency .pm-frequency__close { min-width: 32px; padding: 0; font-size: 22px; line-height: 1; }
.pm-frequency__toolbar { display: flex; align-items: center; gap: 6px; flex-wrap: wrap; }
.pm-frequency__toolbar-spacer { flex: 1; }
.pm-frequency__workflow { display: inline-flex; align-items: center; gap: 5px; color: #68655b; font-size: 12px; }
.pm-frequency__workflow input { accent-color: #526855; }
.pm-frequency__tabs { display: flex; gap: 2px; border-bottom: 1px solid #cac8be; }
.pm-frequency__tabs button { min-height: 34px; border: 0; border-radius: 3px 3px 0 0; background: transparent; color: #716e63; }
.pm-frequency__tabs button.active { border-bottom: 2px solid #4a674e; color: #2d4732; font-weight: 600; }
.pm-frequency__tabs span { margin-left: 7px; padding: 1px 6px; border-radius: 9px; background: #e5e3da; font-size: 11px; }
.pm-frequency__filters { display: flex; align-items: end; gap: 12px; }
.pm-frequency__filters label { display: flex; align-items: center; gap: 7px; }
.pm-frequency__filters input { width: 220px; }
.pm-frequency input:not([type=checkbox]), .pm-frequency select, .pm-frequency textarea { min-height: 30px; box-sizing: border-box; padding: 5px 7px; border: 1px solid #c9c7bd; border-radius: 3px; background: #fffefa; color: inherit; }
.pm-frequency__hint { color: #898679; font-size: 11px; }
.pm-frequency__count { margin-left: auto; color: #777366; }
.pm-frequency__table-wrap { min-height: 120px; flex: 1; overflow: auto; border: 1px solid #ceccc2; background: #fffefa; }
.pm-frequency table { width: 100%; min-width: 1060px; border-collapse: collapse; white-space: nowrap; }
.pm-frequency th { position: sticky; top: 0; z-index: 1; padding: 7px 8px; border-bottom: 1px solid #c8c6bc; background: #eae9e1; color: #4e4b41; text-align: left; font-size: 12px; font-weight: 600; }
.pm-frequency td { padding: 7px 8px; border-bottom: 1px solid #eeece5; }
.pm-frequency tbody tr { cursor: pointer; }
.pm-frequency tbody tr:hover { background: #f6f5ef; }
.pm-frequency tbody tr.selected { background: #e8eee5; }
.pm-frequency th.check, .pm-frequency td.check { width: 30px; text-align: center; }
.pm-frequency .mono { font-family: Consolas, monospace; }
.pm-frequency .numeric { text-align: right; }
.pm-frequency .description { max-width: 240px; overflow: hidden; text-overflow: ellipsis; }
.pm-frequency .empty { padding: 35px; color: #858174; text-align: center; }
.pm-frequency__footer { display: flex; justify-content: space-between; color: #817e73; font-size: 11px; }
.pm-frequency__footer strong { color: #47443a; }
.pm-frequency__toast { position: fixed; right: 24px; bottom: 24px; z-index: 30; max-width: 440px; padding: 11px 16px; border: 1px solid #c4d2c1; border-radius: 4px; background: #f0f7ee; box-shadow: 0 4px 18px #0002; }
.pm-frequency__toast.error { border-color: #d7b7ad; background: #fcf1ed; color: #842d20; }
.pm-frequency__backdrop { position: fixed; inset: 0; z-index: 20; display: grid; place-items: center; padding: 18px; background: #25251f99; }
.pm-frequency__dialog { width: min(620px, 100%); max-height: min(92vh, 760px); overflow: auto; border: 1px solid #a9a79d; border-radius: 5px; background: #f9f8f3; box-shadow: 0 12px 40px #0004; }
.pm-frequency__dialog > header { display: flex; align-items: flex-start; justify-content: space-between; padding: 14px 18px 11px; border-bottom: 1px solid #dddacf; }
.pm-frequency__dialog h2 { margin: 0; font-size: 17px; }
.pm-frequency__dialog header p { margin: 4px 0 0; color: #817e73; font-size: 11px; }
.pm-frequency__dialog header button { min-width: 28px; padding: 0; border: 0; background: transparent; font-size: 20px; }
.pm-frequency__dialog form { padding: 16px 18px 14px; }
.pm-frequency__form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 13px 15px; }
.pm-frequency__form-grid > label { display: flex; flex-direction: column; gap: 5px; }
.pm-frequency__form-grid .wide { grid-column: 1 / -1; }
.pm-frequency__form-grid small { color: #888477; font-size: 11px; }
.pm-frequency__form-grid textarea { resize: vertical; }
.pm-frequency__year-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.pm-frequency__year-grid label { display: flex; flex-direction: column; gap: 5px; }
.pm-frequency__year-grid small { grid-column: 1 / -1; }
.pm-frequency__dialog form > footer { display: flex; justify-content: flex-end; gap: 7px; margin-top: 18px; padding-top: 12px; border-top: 1px solid #dddacf; }
.v-enter-active, .v-leave-active { transition: opacity .14s ease, transform .14s ease; }
.v-enter-from, .v-leave-to { opacity: 0; transform: translateY(5px); }
@media (max-width: 750px) { .pm-frequency { padding: 10px; } .pm-frequency__toolbar-spacer { display: none; } .pm-frequency__workflow { width: 100%; margin: 2px 0; } .pm-frequency__filters { flex-wrap: wrap; } .pm-frequency__filters input { width: min(220px, 58vw); } .pm-frequency__footer { flex-direction: column; gap: 3px; } }
</style>
