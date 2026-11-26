<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type GroupMode = 'add' | 'edit' | 'copy'
interface GroupBasis {
  groupNo: string; substitutionType: string; description: string
  creator: string; createDate: string; reviser: string; reviseDate: string
  dataStamp: number; members: string[]
}
interface SubstitutionBasis { substitutionNo: string; substitutionType: string; substitutionName: string; issueState: number }
interface DevState { version: 1; records: GroupBasis[] }
type GroupDraft = Omit<GroupBasis, 'members'>

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.pm.substitution-group.v1'
const basisStorageKey = 'ymes.dev.pm.substitution-basis.v1'
const typeStorageKey = 'ymes.dev.pm.substitution-type.v1'
const currentUser = 'DEV01'
const fallbackTypes = ['BEARING', 'ELECTRICAL', 'MOTOR', 'PNEUMATIC']
const fallbackBasis: SubstitutionBasis[] = [
  { substitutionNo: 'BRG-6204', substitutionType: 'BEARING', substitutionName: '深溝滾珠軸承 6204', issueState: 2 },
  { substitutionNo: 'BRG-6305', substitutionType: 'BEARING', substitutionName: '深溝滾珠軸承 6305', issueState: 2 },
  { substitutionNo: 'MTR-1KW', substitutionType: 'MOTOR', substitutionName: 'AC 馬達 1kW', issueState: 0 },
  { substitutionNo: 'PNEU-SV01', substitutionType: 'PNEUMATIC', substitutionName: '電磁閥維修組', issueState: 1 },
]

function readTypes(): string[] {
  try {
    const parsed = JSON.parse(window.localStorage.getItem(typeStorageKey) ?? 'null')
    const values = Array.isArray(parsed?.records) ? parsed.records.map((row: { substitutionType?: unknown }) => String(row.substitutionType ?? '')).filter(Boolean) : []
    if (values.length) return [...new Set(values)]
  } catch { /* Use DEV reference choices. */ }
  return fallbackTypes
}
function readActiveBasis(): SubstitutionBasis[] {
  try {
    const parsed = JSON.parse(window.localStorage.getItem(basisStorageKey) ?? 'null')
    if (Array.isArray(parsed?.records)) {
      return parsed.records.filter((row: SubstitutionBasis) => row.issueState === 2).map((row: SubstitutionBasis) => ({
        substitutionNo: String(row.substitutionNo), substitutionType: String(row.substitutionType), substitutionName: String(row.substitutionName), issueState: 2,
      }))
    }
  } catch { /* Use DEV reference choices. */ }
  return fallbackBasis.filter((row) => row.issueState === 2)
}
function seedState(): DevState {
  return { version: 1, records: [
    { groupNo: 'BRG-STD', substitutionType: 'BEARING', description: '軸承類標準替換群組', creator: 'PMUSER', createDate: '2026-11-17 09:20', reviser: '', reviseDate: '', dataStamp: 1, members: ['BRG-6204'] },
    { groupNo: 'BRG-ALT', substitutionType: 'BEARING', description: '備援軸承群組', creator: 'DEV02', createDate: '2026-11-20 10:45', reviser: '', reviseDate: '', dataStamp: 0, members: [] },
    { groupNo: 'MOTOR-STD', substitutionType: 'MOTOR', description: '馬達類替換群組', creator: 'PMUSER', createDate: '2026-11-21 13:50', reviser: 'PMUSER', reviseDate: '2026-11-22 08:20', dataStamp: 2, members: [] },
  ] }
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
const basisTypes = ref(readTypes())
const activeBasis = ref(readActiveBasis())
const searchCode = ref('')
const activeCode = ref('')
const selectedCodes = ref<string[]>([])
const memberDraft = ref<string[]>([])
const availableSelection = ref<string[]>([])
const memberSelection = ref<string[]>([])
const groupMode = ref<GroupMode | null>(null)
const groupDraft = ref<GroupDraft | null>(null)
const message = ref('')
const messageKind = ref<'success' | 'error'>('success')
const nowLabel = computed(() => new Date().toLocaleString('zh-TW', { hour12: false }))
const visibleRows = computed(() => state.value.records.filter((record) => !searchCode.value.trim() || record.groupNo.toLocaleLowerCase().includes(searchCode.value.trim().toLocaleLowerCase())))
const activeGroup = computed(() => state.value.records.find((record) => record.groupNo === activeCode.value) ?? null)
const availableMembers = computed(() => activeBasis.value
  .filter((record) => record.substitutionType === activeGroup.value?.substitutionType && !memberDraft.value.includes(record.substitutionNo)))
const selectedTargets = computed(() => {
  const selected = state.value.records.filter((record) => selectedCodes.value.includes(record.groupNo))
  return selected.length ? selected : activeGroup.value ? [activeGroup.value] : []
})
const allVisibleSelected = computed(() => visibleRows.value.length > 0 && visibleRows.value.every((record) => selectedCodes.value.includes(record.groupNo)))

watch(state, (value) => {
  try { window.localStorage.setItem(storageKey, JSON.stringify(value)) } catch { /* Keep DEV edits in memory for this session. */ }
}, { deep: true })
watch(visibleRows, (rows) => {
  if (!rows.some((record) => record.groupNo === activeCode.value)) activeCode.value = rows[0]?.groupNo ?? ''
  selectedCodes.value = selectedCodes.value.filter((code) => rows.some((record) => record.groupNo === code))
}, { immediate: true })
watch(activeCode, () => {
  memberDraft.value = activeGroup.value ? [...activeGroup.value.members] : []
  availableSelection.value = []
  memberSelection.value = []
})

function toast(text: string, kind: 'success' | 'error' = 'success') {
  message.value = text
  messageKind.value = kind
  window.setTimeout(() => { if (message.value === text) message.value = '' }, 3800)
}
function selectGroup(code: string) {
  activeCode.value = code
  if (!selectedCodes.value.includes(code)) selectedCodes.value = []
}
function toggleGroup(code: string) {
  selectedCodes.value = selectedCodes.value.includes(code)
    ? selectedCodes.value.filter((value) => value !== code)
    : [...selectedCodes.value, code]
}
function toggleAll() {
  selectedCodes.value = allVisibleSelected.value ? [] : visibleRows.value.map((record) => record.groupNo)
}
function openGroup(mode: GroupMode) {
  const source = activeGroup.value
  if (mode !== 'add' && !source) return
  groupMode.value = mode
  groupDraft.value = mode === 'add'
    ? { groupNo: '', substitutionType: '', description: '', creator: currentUser, createDate: nowLabel.value, reviser: '', reviseDate: '', dataStamp: 0 }
    : { ...source!, groupNo: mode === 'copy' ? '' : source!.groupNo, creator: mode === 'copy' ? currentUser : source!.creator, createDate: mode === 'copy' ? nowLabel.value : source!.createDate, reviser: mode === 'copy' ? '' : source!.reviser, reviseDate: mode === 'copy' ? '' : source!.reviseDate, dataStamp: mode === 'copy' ? 0 : source!.dataStamp }
}
function saveGroup() {
  const draft = groupDraft.value
  if (!draft) return
  if (draft.groupNo === '' || draft.substitutionType === '') {
    toast('GroupNo 與備品類別皆為必填。', 'error')
    return
  }
  const duplicate = state.value.records.some((record) => record.groupNo.toLocaleUpperCase() === draft.groupNo.toLocaleUpperCase() && record.groupNo !== (groupMode.value === 'edit' ? activeCode.value : ''))
  if (duplicate) {
    toast('GroupNo 已存在。', 'error')
    return
  }
  if (groupMode.value === 'edit' && activeGroup.value) {
    const typeChanged = activeGroup.value.substitutionType !== draft.substitutionType
    const description = draft.description === '' ? activeGroup.value.description : draft.description
    Object.assign(activeGroup.value, draft, { description, reviser: currentUser, reviseDate: nowLabel.value, dataStamp: activeGroup.value.dataStamp + 1 })
    if (typeChanged) {
      activeGroup.value.members = []
      memberDraft.value = []
      toast('群組類別已修改；舊程式會同時刪除該群組全部成員。')
    } else toast('群組資料已在 DEV 假資料中修改。')
  } else {
    const record: GroupBasis = { ...draft, members: groupMode.value === 'copy' && activeGroup.value ? [...activeGroup.value.members] : [] }
    state.value.records.push(record)
    searchCode.value = ''
    activeCode.value = record.groupNo
    toast(groupMode.value === 'copy' ? `群組已新增並模擬複製 ${record.members.length} 位成員。舊 Service 可能吞掉複製錯誤。` : '群組已新增至 DEV 假資料。')
  }
  groupMode.value = null
  groupDraft.value = null
}
function deleteGroups() {
  const targets = selectedTargets.value
  if (!targets.length) return
  const deleted = targets.map((record) => record.groupNo)
  state.value.records = state.value.records.filter((record) => !deleted.includes(record.groupNo))
  if (deleted.includes(activeCode.value)) activeCode.value = ''
  selectedCodes.value = []
  toast(`已刪除 ${targets.length} 個群組及其成員設定。`)
}
function moveToGroup() {
  const moving = availableMembers.value.filter((record) => availableSelection.value.includes(record.substitutionNo)).map((record) => record.substitutionNo)
  memberDraft.value = [...memberDraft.value, ...moving]
  availableSelection.value = []
  memberSelection.value = []
}
function removeFromGroup() {
  memberDraft.value = memberDraft.value.filter((code) => !memberSelection.value.includes(code))
  availableSelection.value = []
  memberSelection.value = []
}
function reloadReferenceData() {
  activeBasis.value = readActiveBasis()
  basisTypes.value = readTypes()
  availableSelection.value = []
  toast(`已重新載入 DEV 參照資料：${activeBasis.value.length} 筆已生效備品。`)
}
function saveMembers() {
  const targets = selectedTargets.value
  if (!activeGroup.value || !targets.length) return
  let saved = 0
  for (const target of targets) {
    const duplicate = state.value.records.find((record) => record.groupNo !== target.groupNo && record.members.some((code) => memberDraft.value.includes(code)))
    if (duplicate) {
      toast(`備品 ${duplicate.members.find((code) => memberDraft.value.includes(code))} 已屬於群組 ${duplicate.groupNo}；此前已完成 ${saved} 個群組更新。`, 'error')
      return
    }
    target.members = [...memberDraft.value]
    saved += 1
  }
  toast(saved > 1 ? `已依目前群組的成員清單，逐一更新 ${saved} 個選取群組。` : `已儲存 ${memberDraft.value.length} 位群組成員。`)
}
</script>

<template>
  <section class="pm-group" aria-label="備品群組資料 DEV 預覽">
    <header class="pm-group__header"><div><p>PM 設定／SUBSTITUTION GROUP</p><h1>備品群組資料</h1></div><div class="header-actions"><span class="dev-tag">DEV 假資料</span><span class="issue-tag">無簽核狀態</span><button type="button" @click="emit('close')">Close (X)</button></div></header>
    <div class="pm-group__filters"><label>GroupNo <input v-model="searchCode" autocomplete="off" placeholder="輸入群組代碼"></label><span>{{ visibleRows.length }} 個群組</span></div>
    <div class="pm-group__actions"><div><button class="primary" type="button" @click="openGroup('add')">新增</button><button type="button" :disabled="!activeGroup" @click="openGroup('edit')">修改</button><button type="button" :disabled="!activeGroup" @click="openGroup('copy')">複製</button><button type="button" :disabled="!selectedTargets.length" @click="deleteGroups">刪除</button></div><small>按住核取方塊可選取多筆；成員儲存會逐群組處理。</small></div>
    <div class="pm-group__table-wrap master-wrap"><table class="master-table"><thead><tr><th class="check"><input type="checkbox" :checked="allVisibleSelected" aria-label="選取全部群組" @change="toggleAll"></th><th>GroupNo</th><th>SubstitutionType</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead><tbody>
      <tr v-for="record in visibleRows" :key="record.groupNo" :class="{ selected: activeCode === record.groupNo }" @click="selectGroup(record.groupNo)"><td class="check" @click.stop><input type="checkbox" :checked="selectedCodes.includes(record.groupNo)" :aria-label="`選取群組 ${record.groupNo}`" @change="toggleGroup(record.groupNo)"></td><td>{{ record.groupNo }}</td><td>{{ record.substitutionType }}</td><td>{{ record.description }}</td><td>{{ record.creator }}</td><td>{{ record.createDate }}</td><td>{{ record.reviser }}</td><td>{{ record.reviseDate }}</td></tr><tr v-if="!visibleRows.length"><td colspan="8" class="empty">沒有符合條件的群組</td></tr>
    </tbody></table></div>

    <section class="pm-group__members"><header><div><h2>群組成員</h2><span>{{ activeGroup ? `${activeGroup.groupNo}／${activeGroup.substitutionType}` : '請先選取群組' }}</span></div><div class="member-actions"><button type="button" @click="reloadReferenceData">重新載入有效備品</button><button type="button" class="primary" :disabled="!activeGroup" @click="saveMembers">儲存成員{{ selectedCodes.length > 1 ? `（套用至 ${selectedCodes.length} 群組）` : '' }}</button></div></header>
      <div class="pm-group__picker"><div class="pick-list"><h3>可選備品 <span>{{ availableMembers.length }}</span></h3><div class="pick-scroll"><button v-for="record in availableMembers" :key="record.substitutionNo" type="button" :class="{ chosen: availableSelection.includes(record.substitutionNo) }" @click="availableSelection = availableSelection.includes(record.substitutionNo) ? availableSelection.filter((code) => code !== record.substitutionNo) : [...availableSelection, record.substitutionNo]">{{ record.substitutionNo }} <small>{{ record.substitutionName }}</small></button><p v-if="activeGroup && availableMembers.length === 0" class="empty">沒有同類別且未加入群組的已生效備品</p><p v-if="!activeGroup" class="empty">選取群組後載入備品</p></div></div>
        <div class="move-actions"><button type="button" :disabled="!availableSelection.length" aria-label="加入群組" @click="moveToGroup">加入 →</button><button type="button" :disabled="!memberSelection.length" aria-label="移出群組" @click="removeFromGroup">← 移出</button></div>
        <div class="pick-list"><h3>已選成員 <span>{{ memberDraft.length }}</span></h3><div class="pick-scroll"><button v-for="code in memberDraft" :key="code" type="button" :class="{ chosen: memberSelection.includes(code) }" @click="memberSelection = memberSelection.includes(code) ? memberSelection.filter((value) => value !== code) : [...memberSelection, code]">{{ code }}</button><p v-if="activeGroup && !memberDraft.length" class="empty">尚未加入成員</p></div></div>
      </div>
      <p class="picker-note">僅列出與群組類別相同且 IssueState=2 的備品；每個備品只能加入一個群組。</p>
    </section>

    <p v-if="message" class="pm-group__toast" :class="messageKind" role="status">{{ message }}</p>
    <p class="pm-group__note">以上操作只修改瀏覽器 DEV localStorage。舊 WinForms 多選儲存會把目前群組的成員清單逐一套用到所選群組。</p>

    <div v-if="groupMode && groupDraft" class="pm-group__backdrop" @click.self="groupMode = null"><section class="pm-group__dialog" role="dialog" aria-modal="true" :aria-label="groupMode === 'edit' ? '修改備品群組' : groupMode === 'copy' ? '複製備品群組' : '新增備品群組'">
      <header><h2>{{ groupMode === 'edit' ? '修改備品群組' : groupMode === 'copy' ? '複製備品群組' : '新增備品群組' }}</h2><button type="button" aria-label="關閉" @click="groupMode = null">×</button></header>
      <form @submit.prevent="saveGroup"><div class="form-grid"><label>GroupNo<input v-model="groupDraft.groupNo" :readonly="groupMode === 'edit'" required autocomplete="off"></label><label>備品類別<select v-model="groupDraft.substitutionType" required><option value="" disabled>選擇類別</option><option v-for="option in basisTypes" :key="option">{{ option }}</option></select><small v-if="groupMode === 'edit'">修改類別會清除目前群組成員，依舊 Core 行為模擬。</small></label><label class="wide">Description<textarea v-model="groupDraft.description" rows="3"></textarea></label></div><footer><button type="button" @click="groupMode = null">取消</button><button class="primary" type="submit">儲存</button></footer></form>
    </section></div>
  </section>
</template>

<style scoped>
.pm-group { height: 100%; min-height: 0; display: flex; flex-direction: column; gap: 8px; padding: 10px 12px; box-sizing: border-box; background: #f7f5ee; color: #292820; font-size: 12px; }
.pm-group button, .pm-group input, .pm-group select, .pm-group textarea { font: inherit; }
.pm-group button { min-height: 27px; padding: 4px 10px; border: 1px solid #bcb9ad; border-radius: 3px; background: #fffefa; color: #292820; cursor: pointer; white-space: nowrap; }
.pm-group button:hover:not(:disabled) { background: #efede3; }
.pm-group button:disabled { opacity: .48; cursor: not-allowed; }
.pm-group button.primary { border-color: #3b3930; background: #3b3930; color: #fff; }
.pm-group__header, .header-actions, .pm-group__filters, .pm-group__actions, .pm-group__members > header, .pm-group__members > header > div { display: flex; align-items: center; }
.pm-group__header { justify-content: space-between; border-bottom: 1px solid #d7d4c9; padding-bottom: 6px; }
.pm-group__header p { margin: 0 0 2px; color: #777366; font-size: 10px; letter-spacing: .06em; }
.pm-group__header h1 { margin: 0; font-size: 19px; }
.header-actions { gap: 8px; }
.dev-tag, .issue-tag { border-radius: 20px; padding: 3px 8px; }
.dev-tag { color: #775b12; background: #f6edcf; border: 1px solid #e6d49d; }
.issue-tag { color: #5f5d54; background: #eeece3; border: 1px solid #d7d4c9; }
.pm-group__filters { gap: 9px; }
.pm-group__filters label { display: flex; align-items: center; gap: 7px; }
.pm-group__filters input { width: 220px; height: 26px; padding: 3px 7px; border: 1px solid #c9c6bb; border-radius: 3px; background: #fffefa; }
.pm-group__filters > span, .pm-group__actions small, .pm-group__members header span { color: #777366; }
.pm-group__actions { justify-content: space-between; gap: 10px; }
.pm-group__actions > div { display: flex; gap: 5px; }
.pm-group__table-wrap { min-height: 74px; overflow: auto; border: 1px solid #ceccc2; background: #fffefa; }
.master-wrap { flex: .85; }
.pm-group table { width: 100%; border-collapse: collapse; white-space: nowrap; }
.pm-group table.master-table { min-width: 930px; }
.pm-group th, .pm-group td { padding: 5px 8px; border-bottom: 1px solid #e4e2d9; text-align: left; vertical-align: top; }
.pm-group th { position: sticky; top: 0; z-index: 1; background: #eeece3; font-weight: 650; }
.pm-group tbody tr { cursor: pointer; }
.pm-group tbody tr:hover { background: #f7f5ec; }
.pm-group tbody tr.selected { background: #e8edf2; }
.pm-group .check { width: 30px; text-align: center; }
.pm-group .empty { padding: 13px; color: #888477; text-align: center; cursor: default; }
.pm-group__members { display: flex; flex: 1; min-height: 190px; flex-direction: column; gap: 8px; }
.pm-group__members > header { justify-content: space-between; gap: 10px; }
.pm-group__members > header > div:first-child { gap: 9px; }
.pm-group__members h2 { margin: 0; font-size: 14px; }
.member-actions { gap: 6px; }
.pm-group__picker { display: grid; flex: 1; min-height: 140px; grid-template-columns: 1fr 100px 1fr; gap: 8px; }
.pick-list { display: flex; min-height: 0; flex-direction: column; overflow: hidden; border: 1px solid #ceccc2; background: #fffefa; }
.pick-list h3 { display: flex; justify-content: space-between; margin: 0; padding: 7px 9px; border-bottom: 1px solid #dddacf; background: #eeece3; font-size: 12px; }
.pick-list h3 span { color: #777366; font-weight: 400; }
.pick-scroll { flex: 1; min-height: 0; overflow: auto; padding: 5px; }
.pick-scroll button { display: flex; width: 100%; min-height: 29px; justify-content: space-between; gap: 8px; margin-bottom: 3px; text-align: left; }
.pick-scroll button.chosen { border-color: #8297a8; background: #e8edf2; }
.pick-scroll small { overflow: hidden; color: #777366; text-overflow: ellipsis; }
.move-actions { display: flex; flex-direction: column; justify-content: center; gap: 8px; }
.move-actions button { white-space: normal; }
.picker-note, .pm-group__note { margin: 0; color: #777366; font-size: 11px; }
.pm-group__toast { margin: 0; padding: 7px 9px; border: 1px solid #c9d9c8; background: #edf5eb; color: #315430; }
.pm-group__toast.error { border-color: #e4c5bd; background: #fbefec; color: #863c2e; }
.pm-group__backdrop { position: fixed; inset: 0; z-index: 5000; display: grid; place-items: center; padding: 18px; background: #1f1e1a66; }
.pm-group__dialog { width: min(520px, 96vw); max-height: 90vh; overflow: auto; border: 1px solid #aaa79b; border-radius: 5px; background: #fffefa; box-shadow: 0 15px 50px #0003; }
.pm-group__dialog > header { display: flex; align-items: center; justify-content: space-between; padding: 11px 15px; border-bottom: 1px solid #dddacf; background: #f4f2e9; }
.pm-group__dialog h2 { margin: 0; font-size: 15px; }
.pm-group__dialog > header button { min-height: 24px; padding: 0 7px; font-size: 18px; }
.pm-group__dialog form { padding: 15px 17px; }
.pm-group .form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px 15px; }
.pm-group .form-grid label { display: flex; flex-direction: column; gap: 5px; }
.pm-group .form-grid .wide { grid-column: 1 / -1; }
.pm-group .form-grid input, .pm-group .form-grid select, .pm-group .form-grid textarea { width: 100%; min-height: 29px; box-sizing: border-box; padding: 5px 7px; border: 1px solid #c9c6bb; border-radius: 3px; background: white; }
.pm-group .form-grid textarea { resize: vertical; }
.pm-group .form-grid small { color: #888477; font-size: 11px; }
.pm-group__dialog form > footer { display: flex; justify-content: flex-end; gap: 7px; margin-top: 18px; padding-top: 12px; border-top: 1px solid #dddacf; }
@media (max-width: 700px) { .pm-group { padding: 8px; } .pm-group__actions { align-items: flex-start; flex-direction: column; } .pm-group__members > header { align-items: flex-start; flex-direction: column; } .pm-group__picker { grid-template-columns: 1fr 74px 1fr; } .pm-group__filters input { width: min(44vw, 220px); } .pm-group .form-grid { grid-template-columns: 1fr; } .pm-group .form-grid .wide { grid-column: auto; } }
</style>
