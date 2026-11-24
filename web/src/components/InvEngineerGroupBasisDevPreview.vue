<script setup lang="ts">
import { computed, ref, watch } from 'vue'

interface EngineerGroup {
  engineerGroupNo: string
  engineerGroupName: string
  description: string
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
  dataStamp: number
  members: string[]
}
interface UserRow { userNo: string; userName: string; department: string; issueState: 2 }
interface GroupDraft { engineerGroupNo: string; engineerGroupName: string; description: string }
type DialogMode = 'add' | 'edit' | 'copy'

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.inv.engineer-groups.v1'
const currentUser = 'DEVUSER'
const users: UserRow[] = [
  { userNo: 'E1001', userName: '林志明', department: '工程一課', issueState: 2 },
  { userNo: 'E1002', userName: '陳怡君', department: '工程一課', issueState: 2 },
  { userNo: 'E1003', userName: '張建宏', department: '工程二課', issueState: 2 },
  { userNo: 'E1004', userName: '黃雅婷', department: '工程二課', issueState: 2 },
  { userNo: 'E1005', userName: '王俊傑', department: '廠務工程', issueState: 2 },
  { userNo: 'E1006', userName: '吳佩珊', department: '廠務工程', issueState: 2 },
  { userNo: 'E1007', userName: '劉冠廷', department: '設備工程', issueState: 2 },
  { userNo: 'E1008', userName: '蔡佳蓉', department: '設備工程', issueState: 2 },
  { userNo: 'E1009', userName: '許文豪', department: '製程工程', issueState: 2 },
  { userNo: 'E1010', userName: '鄭欣怡', department: '製程工程', issueState: 2 },
  { userNo: 'E1011', userName: '郭柏廷', department: '資訊工程', issueState: 2 },
  { userNo: 'E1012', userName: '葉佳玲', department: '資訊工程', issueState: 2 },
]
const seedGroups: EngineerGroup[] = [
  { engineerGroupNo: 'ENG-EQ-01', engineerGroupName: '設備工程一組', description: '負責一廠設備維護與異常支援。', creator: 'M1001', createDate: '2025-04-08 09:20', reviser: 'M1008', reviseDate: '2026-08-12 14:35', dataStamp: 6, members: ['E1001', 'E1002', 'E1007'] },
  { engineerGroupNo: 'ENG-EQ-02', engineerGroupName: '設備工程二組', description: '負責二廠設備保養與改善。', creator: 'M1001', createDate: '2025-04-10 10:05', reviser: '', reviseDate: '', dataStamp: 3, members: ['E1003', 'E1004', 'E1008'] },
  { engineerGroupNo: 'ENG-FAC-01', engineerGroupName: '廠務工程組', description: '水電、空調及公用系統支援。', creator: 'M2002', createDate: '2025-06-01 08:10', reviser: 'M2002', reviseDate: '2026-02-20 16:12', dataStamp: 2, members: ['E1005', 'E1006'] },
  { engineerGroupNo: 'ENG-PE-01', engineerGroupName: '製程工程組', description: '製程條件與工程變更支援。', creator: 'M3003', createDate: '2025-07-18 13:00', reviser: '', reviseDate: '', dataStamp: 1, members: ['E1009', 'E1010'] },
  { engineerGroupNo: 'ENG-IT-01', engineerGroupName: '資訊工程組', description: 'MES 與資訊系統支援。', creator: 'M4004', createDate: '2025-09-02 15:40', reviser: 'M4004', reviseDate: '2026-03-03 11:25', dataStamp: 4, members: ['E1011', 'E1012'] },
]

function loadGroups(): EngineerGroup[] {
  try {
    const raw = window.localStorage.getItem(storageKey)
    if (raw) {
      const parsed = JSON.parse(raw) as EngineerGroup[]
      if (Array.isArray(parsed) && parsed.every((row) => row && typeof row.engineerGroupNo === 'string' && Array.isArray(row.members))) return parsed
    }
  } catch { /* use the in-memory fixture when storage is unavailable */ }
  return structuredClone(seedGroups)
}

const groups = ref<EngineerGroup[]>(loadGroups())
const queryNo = ref('')
const selectedGroupNo = ref(groups.value[0]?.engineerGroupNo ?? '')
const checkedGroups = ref<string[]>([])
const memberDraft = ref<string[]>([])
const pickedAvailable = ref<string[]>([])
const pickedAssigned = ref<string[]>([])
const availableSearch = ref('')
const assignedSearch = ref('')
const userToAdd = ref('')
const userToRemove = ref('')
const notice = ref('DEV 假資料：變更只存在本機瀏覽器，不會寫入 MES。')
const dialogMode = ref<DialogMode | null>(null)
const dialogError = ref('')
const draft = ref<GroupDraft>({ engineerGroupNo: '', engineerGroupName: '', description: '' })

const filteredGroups = computed(() => {
  const query = queryNo.value.trim().toLocaleUpperCase()
  return groups.value.filter((group) => !query || group.engineerGroupNo.toLocaleUpperCase().includes(query))
})
const selectedGroup = computed(() => groups.value.find((group) => group.engineerGroupNo === selectedGroupNo.value) ?? null)
const assignedUsers = computed(() => users.filter((user) => memberDraft.value.includes(user.userNo) && matchesUser(user, assignedSearch.value)))
const availableUsers = computed(() => users.filter((user) => !memberDraft.value.includes(user.userNo) && matchesUser(user, availableSearch.value)))
const membersChanged = computed(() => Boolean(selectedGroup.value) && !sameMembers(memberDraft.value, selectedGroup.value?.members ?? []))
const checkedGroupCount = computed(() => checkedGroups.value.length)

function matchesUser(user: UserRow, query: string) {
  const normalized = query.trim().toLocaleUpperCase()
  return !normalized || user.userNo.toLocaleUpperCase().includes(normalized) || user.userName.includes(query.trim()) || user.department.includes(query.trim())
}
function sameMembers(first: string[], second: string[]) {
  if (first.length !== second.length) return false
  const normalizedFirst = [...first].sort()
  const normalizedSecond = [...second].sort()
  return normalizedFirst.every((value, index) => value === normalizedSecond[index])
}
function selectGroup(group: EngineerGroup) {
  selectedGroupNo.value = group.engineerGroupNo
  memberDraft.value = [...group.members]
  pickedAvailable.value = []
  pickedAssigned.value = []
}
function toggleChecked(groupNo: string) {
  checkedGroups.value = checkedGroups.value.includes(groupNo) ? checkedGroups.value.filter((value) => value !== groupNo) : [...checkedGroups.value, groupNo]
}
function openDialog(mode: DialogMode) {
  if (mode !== 'add' && !selectedGroup.value) return
  const source = selectedGroup.value
  dialogMode.value = mode
  dialogError.value = ''
  draft.value = mode === 'edit' && source
    ? { engineerGroupNo: source.engineerGroupNo, engineerGroupName: source.engineerGroupName, description: source.description }
    : mode === 'copy' && source
      ? { engineerGroupNo: `${source.engineerGroupNo}-COPY`, engineerGroupName: `${source.engineerGroupName} 複製`, description: source.description }
      : { engineerGroupNo: '', engineerGroupName: '', description: '' }
}
function saveGroup(closeAfterSave: boolean) {
  const no = draft.value.engineerGroupNo.trim().toLocaleUpperCase()
  const name = draft.value.engineerGroupName.trim()
  if (!no || !name) { dialogError.value = '群組編號與群組名稱為必填。'; return }
  const duplicate = groups.value.some((group) => group.engineerGroupNo.toLocaleUpperCase() === no && !(dialogMode.value === 'edit' && group.engineerGroupNo === selectedGroupNo.value))
  if (duplicate) { dialogError.value = '群組編號已存在。'; return }
  const now = new Date().toLocaleString('zh-TW', { hour12: false })
  if (dialogMode.value === 'edit' && selectedGroup.value) {
    Object.assign(selectedGroup.value, { engineerGroupName: name, description: draft.value.description, reviser: currentUser, reviseDate: now, dataStamp: selectedGroup.value.dataStamp + 1 })
    selectedGroupNo.value = selectedGroup.value.engineerGroupNo
    notice.value = `已在 DEV 假資料修改 ${selectedGroup.value.engineerGroupNo}。`
  } else {
    const source = dialogMode.value === 'copy' ? selectedGroup.value : null
    const created: EngineerGroup = {
      engineerGroupNo: no,
      engineerGroupName: name,
      description: draft.value.description,
      creator: currentUser,
      createDate: now,
      reviser: '',
      reviseDate: '',
      dataStamp: 1,
      members: source ? [...source.members] : [],
    }
    groups.value = [...groups.value, created].sort((a, b) => a.engineerGroupNo.localeCompare(b.engineerGroupNo))
    selectedGroupNo.value = created.engineerGroupNo
    memberDraft.value = [...created.members]
    checkedGroups.value = [created.engineerGroupNo]
    notice.value = source ? `已在 DEV 假資料複製 ${source.engineerGroupNo} 與 ${source.members.length} 位直接成員。` : `已在 DEV 假資料新增 ${created.engineerGroupNo}。`
  }
  if (closeAfterSave) dialogMode.value = null
}
function deleteGroups() {
  const targets = checkedGroups.value.filter((groupNo) => groups.value.some((group) => group.engineerGroupNo === groupNo))
  if (!targets.length) { notice.value = '請先勾選要刪除的群組。'; return }
  if (!window.confirm(`確定刪除 ${targets.length} 個群組及其工程群組成員設定？`)) return
  groups.value = groups.value.filter((group) => !targets.includes(group.engineerGroupNo))
  checkedGroups.value = []
  if (targets.includes(selectedGroupNo.value)) {
    selectedGroupNo.value = groups.value[0]?.engineerGroupNo ?? ''
    memberDraft.value = selectedGroup.value ? [...selectedGroup.value.members] : []
  }
  notice.value = `已在 DEV 假資料刪除 ${targets.length} 個群組。`
}
function addUsers(userNos: string[]) {
  const valid = userNos.map((value) => value.trim().toLocaleUpperCase()).filter((value) => users.some((user) => user.userNo === value) && !memberDraft.value.includes(value))
  memberDraft.value = [...memberDraft.value, ...valid]
  userToAdd.value = ''
}
function removeUsers(userNos: string[]) {
  memberDraft.value = memberDraft.value.filter((userNo) => !userNos.includes(userNo))
  userToRemove.value = ''
}
function addTypedUser() {
  const value = userToAdd.value.trim().toLocaleUpperCase()
  if (!users.some((user) => user.userNo === value)) { notice.value = `找不到有效使用者 ${value || '(空白)'}。`; return }
  addUsers([value])
}
function addPickedUsers() {
  addUsers([...pickedAvailable.value])
  pickedAvailable.value = []
}
function removeTypedUser() {
  const value = userToRemove.value.trim().toLocaleUpperCase()
  if (!memberDraft.value.includes(value)) { notice.value = `此群組沒有直接成員 ${value || '(空白)'}。`; return }
  removeUsers([value])
}
function removePickedUsers() {
  removeUsers([...pickedAssigned.value])
  pickedAssigned.value = []
}
function confirmMembers() {
  if (!selectedGroup.value) { notice.value = '請先選擇工程群組。'; return }
  selectedGroup.value.members = [...memberDraft.value]
  selectedGroup.value.dataStamp += 1
  selectedGroup.value.reviser = currentUser
  selectedGroup.value.reviseDate = new Date().toLocaleString('zh-TW', { hour12: false })
  notice.value = `已模擬替換 ${selectedGroup.value.engineerGroupNo} 的 ${memberDraft.value.length} 位直接成員；DataStamp 已遞增。`
}
function revertMembers() {
  memberDraft.value = selectedGroup.value ? [...selectedGroup.value.members] : []
}

watch(groups, (value) => {
  try { window.localStorage.setItem(storageKey, JSON.stringify(value)) } catch { notice.value = '瀏覽器未允許 localStorage，這次操作只保留在目前頁面。' }
}, { deep: true })
watch(selectedGroupNo, () => {
  memberDraft.value = selectedGroup.value ? [...selectedGroup.value.members] : []
  availableSearch.value = ''
  assignedSearch.value = ''
})
</script>

<template>
  <main class="inv-engineer" aria-label="庫房工程群組 DEV">
    <header class="titlebar">
      <div><p class="eyebrow">INV · IV101</p><h1>庫房工程群組</h1></div>
      <button class="close-button" type="button" title="Close" aria-label="Close" @click="emit('close')">×</button>
    </header>

    <div class="toolbar">
      <label class="query-field">EngineerGroupNo <input v-model="queryNo" type="search" placeholder="輸入群組編號" /></label>
      <button type="button" @click="queryNo = queryNo.trim()">Query</button>
      <span class="toolbar-spacer"></span>
      <button type="button" @click="openDialog('add')">Add</button>
      <button type="button" :disabled="!selectedGroup" @click="openDialog('edit')">Edit</button>
      <button type="button" :disabled="!selectedGroup" @click="openDialog('copy')">Copy</button>
      <button type="button" class="danger" :disabled="checkedGroupCount === 0" @click="deleteGroups">Delete<span v-if="checkedGroupCount"> ({{ checkedGroupCount }})</span></button>
    </div>

    <div class="workspace">
      <section class="group-panel" aria-label="工程群組清單">
        <div class="section-heading"><h2>Engineer Group Basis</h2><span>{{ filteredGroups.length }} 筆</span></div>
        <div class="table-wrap">
          <table>
            <thead><tr><th class="check-col"></th><th>EngineerGroupNo</th><th>EngineerGroupName</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
            <tbody>
              <tr v-for="group in filteredGroups" :key="group.engineerGroupNo" :class="{ active: selectedGroupNo === group.engineerGroupNo }" @click="selectGroup(group)">
                <td class="check-col"><input type="checkbox" :checked="checkedGroups.includes(group.engineerGroupNo)" :aria-label="`選取 ${group.engineerGroupNo}`" @click.stop="selectGroup(group); toggleChecked(group.engineerGroupNo)" /></td>
                <td class="mono">{{ group.engineerGroupNo }}</td><td>{{ group.engineerGroupName }}</td><td class="description-cell">{{ group.description }}</td><td>{{ group.creator }}</td><td>{{ group.createDate }}</td><td>{{ group.reviser }}</td><td>{{ group.reviseDate }}</td>
              </tr>
              <tr v-if="filteredGroups.length === 0"><td colspan="8" class="empty">查無工程群組</td></tr>
            </tbody>
          </table>
        </div>
        <p class="grid-note">選取列以載入直接成員；左側核取方塊可多選刪除。</p>
      </section>

      <section class="members-panel" aria-label="工程群組使用者">
        <div class="section-heading"><h2>Engineer Group’s User</h2><span>{{ selectedGroup?.engineerGroupNo || '尚未選取群組' }}</span></div>
        <div class="transfer-columns">
          <div class="user-list">
            <div class="list-title"><strong>Available User</strong><span>{{ availableUsers.length }}</span></div>
            <input v-model="availableSearch" class="list-search" type="search" placeholder="查 UserNo／姓名／部門" aria-label="搜尋可加入使用者" />
            <div class="list-scroll">
              <label v-for="user in availableUsers" :key="user.userNo" class="user-row" @dblclick="addUsers([user.userNo])"><input v-model="pickedAvailable" type="checkbox" :value="user.userNo" /><span class="user-no">{{ user.userNo }}</span><span>{{ user.userName }}</span><small>{{ user.department }}</small></label>
              <p v-if="!availableUsers.length" class="empty">沒有可加入使用者</p>
            </div>
            <div class="typed-action"><input v-model="userToAdd" type="text" placeholder="UserNo + Enter" aria-label="輸入 UserNo 加入" @keydown.enter.prevent="addTypedUser" /><button type="button" @click="addTypedUser">加入</button></div>
          </div>
          <div class="transfer-actions" aria-label="移動使用者">
            <button type="button" title="加入勾選的使用者" :disabled="!selectedGroup || !pickedAvailable.length" @click="addPickedUsers">＋</button>
            <button type="button" title="移除勾選的使用者" :disabled="!selectedGroup || !pickedAssigned.length" @click="removePickedUsers">−</button>
          </div>
          <div class="user-list">
            <div class="list-title"><strong>Assigned User</strong><span>{{ memberDraft.length }}</span></div>
            <input v-model="assignedSearch" class="list-search" type="search" placeholder="查 UserNo／姓名／部門" aria-label="搜尋已指派使用者" />
            <div class="list-scroll">
              <label v-for="user in assignedUsers" :key="user.userNo" class="user-row" @dblclick="removeUsers([user.userNo])"><input v-model="pickedAssigned" type="checkbox" :value="user.userNo" /><span class="user-no">{{ user.userNo }}</span><span>{{ user.userName }}</span><small>{{ user.department }}</small></label>
              <p v-if="!assignedUsers.length" class="empty">目前沒有直接成員</p>
            </div>
            <div class="typed-action"><input v-model="userToRemove" type="text" placeholder="UserNo + Enter" aria-label="輸入 UserNo 移除" @keydown.enter.prevent="removeTypedUser" /><button type="button" @click="removeTypedUser">移除</button></div>
          </div>
        </div>
        <div class="member-footer">
          <p>只顯示直接成員（GroupType 3、AgentOf = NA）；使用者清單模擬 IssueState = 2。</p>
          <span class="footer-spacer"></span>
          <button type="button" @click="revertMembers" :disabled="!membersChanged">還原</button>
          <button type="button" class="primary" @click="confirmMembers" :disabled="!selectedGroup || !membersChanged">Confirm</button>
        </div>
      </section>
    </div>

    <footer class="statusbar" role="status"><span>{{ notice }}</span><span v-if="selectedGroup">DataStamp {{ selectedGroup.dataStamp }}</span></footer>

    <div v-if="dialogMode" class="modal-backdrop" @click.self="dialogMode = null">
      <section class="group-dialog" role="dialog" aria-modal="true" :aria-label="dialogMode === 'add' ? '新增工程群組' : dialogMode === 'edit' ? '修改工程群組' : '複製工程群組'">
        <header><h2>{{ dialogMode === 'add' ? 'Add Engineer Group' : dialogMode === 'edit' ? 'Edit Engineer Group' : 'Copy Engineer Group' }}</h2><button type="button" aria-label="關閉" @click="dialogMode = null">×</button></header>
        <label>EngineerGroupNo <input v-model="draft.engineerGroupNo" :readonly="dialogMode === 'edit'" autocomplete="off" @blur="draft.engineerGroupNo = draft.engineerGroupNo.toLocaleUpperCase()" /></label>
        <label>EngineerGroupName <input v-model="draft.engineerGroupName" autocomplete="off" /></label>
        <label>Description <textarea v-model="draft.description" rows="4"></textarea></label>
        <p v-if="dialogMode === 'edit' && selectedGroup" class="audit">Creator {{ selectedGroup.creator }} · {{ selectedGroup.createDate }}<template v-if="selectedGroup.reviser"> · Reviser {{ selectedGroup.reviser }} · {{ selectedGroup.reviseDate }}</template></p>
        <p v-if="dialogError" class="form-error" role="alert">{{ dialogError }}</p>
        <footer><button type="button" @click="dialogMode = null">Cancel</button><span class="footer-spacer"></span><button type="button" @click="saveGroup(false)">Apply</button><button type="button" class="primary" @click="saveGroup(true)">Confirm</button></footer>
      </section>
    </div>
  </main>
</template>

<style scoped>
.inv-engineer { height: 100%; min-height: 0; display: flex; flex-direction: column; gap: 10px; box-sizing: border-box; padding: 12px; overflow: auto; color: #292820; background: #f3f1e9; font: 13px/1.35 "Segoe UI", "Microsoft JhengHei", sans-serif; }
.titlebar, .toolbar, .section-heading, .member-footer, .statusbar { display: flex; align-items: center; gap: 8px; }
.titlebar { justify-content: space-between; border-bottom: 1px solid #c9c6b8; padding: 0 0 8px; }
h1, h2, p { margin: 0; }
h1 { font-size: 20px; font-weight: 650; }
.eyebrow { margin-bottom: 2px; color: #767265; font-size: 11px; letter-spacing: .08em; }
.close-button { width: 30px; height: 28px; border: 1px solid #777366; background: #e8e5d9; color: #37352d; font-size: 20px; cursor: pointer; }
.toolbar { flex-wrap: wrap; padding: 8px; border: 1px solid #c9c6b8; background: #ebe9df; }
.query-field { display: flex; align-items: center; gap: 6px; }
.query-field input { width: min(260px, 40vw); }
button, input, textarea { font: inherit; }
button { min-height: 28px; padding: 4px 10px; border: 1px solid #8e8a7b; border-radius: 2px; color: #292820; background: #f8f7f1; cursor: pointer; }
button:hover:not(:disabled) { background: #e3e0d2; }
button:disabled { opacity: .5; cursor: not-allowed; }
button.primary { border-color: #3f5847; color: #fff; background: #49664f; }
button.danger { color: #8b3028; }
.toolbar-spacer, .footer-spacer { flex: 1; }
input:not([type=checkbox]), textarea { min-height: 27px; padding: 4px 7px; border: 1px solid #9e9a8c; border-radius: 2px; color: #292820; background: #fffef9; }
.workspace { flex: 1; min-height: 450px; display: grid; grid-template-rows: minmax(180px, 1.15fr) minmax(235px, 1fr); gap: 10px; }
.group-panel, .members-panel { min-height: 0; display: flex; flex-direction: column; border: 1px solid #c5c1b4; background: #faf9f4; }
.section-heading { justify-content: space-between; padding: 7px 9px; border-bottom: 1px solid #d5d2c8; background: #e8e6dc; }
.section-heading h2 { font-size: 13px; font-weight: 650; }
.section-heading span { color: #736f63; font-size: 12px; }
.table-wrap { flex: 1; min-height: 0; overflow: auto; }
table { width: 100%; border-collapse: collapse; white-space: nowrap; font-size: 12px; }
th, td { padding: 5px 7px; border-right: 1px solid #dfddd4; border-bottom: 1px solid #e5e3dc; text-align: left; }
th { position: sticky; top: 0; z-index: 1; background: #f0efe8; color: #514f45; font-weight: 600; }
tbody tr { cursor: pointer; }
tbody tr:hover { background: #f3f0e1; }
tbody tr.active { background: #e5eddf; }
.check-col { width: 27px; text-align: center; }
.check-col input { margin: 0; }
.mono, .user-no { font-family: Consolas, "Courier New", monospace; }
.description-cell { max-width: 240px; overflow: hidden; text-overflow: ellipsis; }
.grid-note { padding: 4px 8px; color: #777366; font-size: 11px; }
.transfer-columns { flex: 1; min-height: 0; display: grid; grid-template-columns: minmax(0, 1fr) 42px minmax(0, 1fr); gap: 8px; padding: 8px; }
.user-list { min-width: 0; min-height: 0; display: flex; flex-direction: column; border: 1px solid #d2cfc3; background: #fffef9; }
.list-title { display: flex; align-items: center; justify-content: space-between; padding: 5px 7px; background: #f0efe8; border-bottom: 1px solid #d9d6cb; }
.list-title strong { font-size: 12px; }
.list-title span { color: #777366; }
.list-search { margin: 6px; }
.list-scroll { flex: 1; min-height: 76px; overflow: auto; }
.user-row { display: grid; grid-template-columns: 18px 75px minmax(54px, 1fr) minmax(60px, auto); gap: 5px; align-items: center; min-height: 27px; padding: 2px 6px; border-bottom: 1px solid #efeee9; cursor: pointer; }
.user-row:hover { background: #f4f2e8; }
.user-row small { color: #79766d; white-space: nowrap; }
.user-row input { margin: 0; }
.typed-action { display: flex; gap: 5px; padding: 6px; border-top: 1px solid #e5e2d7; }
.typed-action input { min-width: 0; flex: 1; }
.typed-action button { padding-inline: 8px; }
.transfer-actions { display: flex; flex-direction: column; justify-content: center; gap: 8px; }
.transfer-actions button { padding: 0; font-size: 19px; }
.member-footer { padding: 0 8px 8px; }
.member-footer p { color: #777366; font-size: 11px; }
.statusbar { justify-content: space-between; min-height: 26px; padding: 0 8px; border: 1px solid #d0cdbf; color: #625f54; background: #e9e7dc; font-size: 11px; }
.empty { padding: 14px 8px; color: #898579; text-align: center; }
.modal-backdrop { position: fixed; inset: 0; z-index: 20; display: grid; place-items: center; padding: 18px; background: rgb(25 24 20 / 42%); }
.group-dialog { width: min(580px, 100%); padding: 14px; border: 1px solid #777366; box-shadow: 0 8px 30px rgb(0 0 0 / 24%); background: #f7f5ee; }
.group-dialog > header, .group-dialog > footer { display: flex; align-items: center; gap: 8px; }
.group-dialog > header { justify-content: space-between; margin-bottom: 14px; padding-bottom: 8px; border-bottom: 1px solid #d1cec1; }
.group-dialog h2 { font-size: 16px; }
.group-dialog > header button { min-width: 28px; padding: 0; font-size: 18px; }
.group-dialog > label { display: grid; grid-template-columns: 160px minmax(0, 1fr); align-items: center; gap: 10px; margin: 9px 0; }
.group-dialog textarea { min-height: 82px; resize: vertical; }
.group-dialog .audit { margin: 12px 0; color: #777366; font-size: 11px; }
.group-dialog .form-error { margin: 8px 0; color: #9a2924; }
.group-dialog > footer { margin-top: 14px; padding-top: 10px; border-top: 1px solid #d1cec1; }
@media (max-width: 900px) {
  .workspace { min-height: 650px; grid-template-rows: minmax(220px, .85fr) minmax(390px, 1.15fr); }
  .transfer-columns { grid-template-columns: minmax(0, 1fr) 34px minmax(0, 1fr); }
  .user-row { grid-template-columns: 18px 66px minmax(46px, 1fr); }
  .user-row small { display: none; }
  .member-footer { flex-wrap: wrap; }
}
@media (max-width: 600px) {
  .transfer-columns { grid-template-columns: minmax(0, 1fr); }
  .transfer-actions { flex-direction: row; }
  .workspace { min-height: 900px; }
  .toolbar { align-items: flex-start; }
  .toolbar-spacer { display: none; }
  .group-dialog > label { grid-template-columns: 1fr; gap: 3px; }
}
</style>
