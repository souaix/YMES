<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

interface EngineerGroup {
  engineerGroupNo: string
  engineerGroupName: string
  description: string | null
  creator: string | null
  createDate: string | null
  reviser: string | null
  reviseDate: string | null
  dataStamp: number
}
interface EngineerUser {
  userNo: string
  userName: string
  selected?: boolean
}
type GroupMode = 'new' | 'edit' | 'copy' | null

const props = defineProps<{ apiBaseUrl: string; currentUserNo?: string }>()
const appBase = import.meta.env.BASE_URL.endsWith('/') ? import.meta.env.BASE_URL.slice(0, -1) : import.meta.env.BASE_URL
const usesDevelopmentMock = import.meta.env.DEV
const groupStoreKey = 'ymes-dev-mock:equipment-engineer-groups:v1'
const memberStoreKey = 'ymes-dev-mock:equipment-engineer-group-users:v1'
const seedGroups: EngineerGroup[] = [
  { engineerGroupNo: 'DEV-EG-01', engineerGroupName: '開發用設備維護組', description: '設備保養與一般維護範例', creator: 'DEV001', createDate: '2026-11-01T08:00:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-05T09:30:00+08:00', dataStamp: 1 },
  { engineerGroupNo: 'DEV-EG-02', engineerGroupName: '開發用公用設備組', description: '空調、空壓與廠務設備範例', creator: 'DEV001', createDate: '2026-11-02T08:00:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
  { engineerGroupNo: 'DEV-EG-03', engineerGroupName: '開發用設備品保組', description: '設備驗收與點檢範例', creator: 'DEV002', createDate: '2026-11-03T08:00:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
]
const seedUsers: EngineerUser[] = [
  { userNo: 'DEV001', userName: '開發使用者一' },
  { userNo: 'DEV002', userName: '開發使用者二' },
  { userNo: 'DEV003', userName: '工程使用者三' },
  { userNo: 'DEV004', userName: '工程使用者四' },
  { userNo: 'DEV005', userName: '設備使用者五' },
  { userNo: 'DEV006', userName: '品保使用者六' },
]
const seedMembers: Record<string, string[]> = {
  'DEV-EG-01': ['DEV001', 'DEV003', 'DEV004'],
  'DEV-EG-02': ['DEV002', 'DEV005'],
  'DEV-EG-03': ['DEV004', 'DEV006'],
}

const groups = ref<EngineerGroup[]>([])
const users = ref<EngineerUser[]>([])
const activeGroupNo = ref('')
const checkedUserNos = ref<string[]>([])
const savedUserNos = ref<string[]>([])
const selectedForDelete = ref<string[]>([])
const keyword = ref('')
const showSelectedOnly = ref(true)
const loading = ref(false)
const membersLoading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const groupMode = ref<GroupMode>(null)
const groupForm = ref({ engineerGroupNo: '', engineerGroupName: '', description: '', dataStamp: 0 })
const groupAudit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })

const visibleGroups = computed(() => groups.value
  .filter((row) => row.engineerGroupNo.toLocaleLowerCase().includes(keyword.value.trim().toLocaleLowerCase()))
  .slice()
  .sort((a, b) => a.engineerGroupNo.localeCompare(b.engineerGroupNo)))
const selectedGroup = computed(() => groups.value.find((row) => row.engineerGroupNo === activeGroupNo.value) ?? null)
const visibleUsers = computed(() => showSelectedOnly.value
  ? users.value.filter((user) => checkedUserNos.value.includes(user.userNo))
  : users.value)
const membershipDirty = computed(() => {
  const current = [...checkedUserNos.value].sort()
  const saved = [...savedUserNos.value].sort()
  return current.length !== saved.length || current.some((userNo, index) => userNo !== saved[index])
})

function makeApiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  const baseUrl = new URL(base.endsWith('/') ? base : base + '/', window.location.origin)
  return new URL(path.startsWith('/') ? path.slice(1) : path, baseUrl).toString()
}
async function requestJson<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(makeApiUrl(path), { credentials: 'include', ...init })
  if (response.status === 401) {
    window.location.assign(appBase + '/')
    throw new Error('登入狀態已逾期，請重新登入。')
  }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    throw new Error(problem?.detail || problem?.message || problem?.title || '設備工程群組 API 回應錯誤 (' + response.status + ')。')
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function readMock<T>(key: string, seed: T): T {
  try {
    const saved = window.localStorage.getItem(key)
    return saved ? JSON.parse(saved) as T : structuredClone(seed)
  } catch { return structuredClone(seed) }
}
function writeMock<T>(key: string, value: T): void { window.localStorage.setItem(key, JSON.stringify(value)) }
function formatDate(value: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.valueOf()) ? value : date.toLocaleString('zh-TW')
}
function clearMemberDetail(): void {
  activeGroupNo.value = ''
  checkedUserNos.value = []
  savedUserNos.value = []
  users.value = []
}
async function loadMembers(groupNo: string): Promise<void> {
  if (!groupNo) { clearMemberDetail(); return }
  membersLoading.value = true
  try {
    if (usesDevelopmentMock) {
      users.value = structuredClone(seedUsers)
      const store = readMock<Record<string, string[]>>(memberStoreKey, seedMembers)
      checkedUserNos.value = [...(store[groupNo] ?? [])]
    } else {
      const rows = await requestJson<EngineerUser[]>('api/equipment/engineer-groups/' + encodeURIComponent(groupNo) + '/users')
      users.value = rows
      checkedUserNos.value = rows.filter((user) => user.selected).map((user) => user.userNo)
    }
    savedUserNos.value = [...checkedUserNos.value]
  } catch (error) {
    users.value = []
    checkedUserNos.value = []
    savedUserNos.value = []
    errorMessage.value = error instanceof Error ? error.message : '群組使用者載入失敗。'
  } finally { membersLoading.value = false }
}
async function selectGroup(groupNo: string, discardPending = false): Promise<void> {
  if (membershipDirty.value && !discardPending && !window.confirm('使用者勾選尚未確認，切換群組會放棄這些變更。要繼續嗎？')) return
  activeGroupNo.value = groupNo
  await loadMembers(groupNo)
}
async function loadGroups(): Promise<void> {
  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''
  try {
    if (usesDevelopmentMock) {
      groups.value = readMock<EngineerGroup[]>(groupStoreKey, seedGroups)
    } else {
      const query = new URLSearchParams()
      if (keyword.value.trim()) query.set('engineerGroupNo', keyword.value.trim())
      const queryString = query.toString()
      groups.value = await requestJson<EngineerGroup[]>('api/equipment/engineer-groups' + (queryString ? '?' + queryString : ''))
    }
    selectedForDelete.value = selectedForDelete.value.filter((no) => groups.value.some((group) => group.engineerGroupNo === no))
    const next = visibleGroups.value.find((group) => group.engineerGroupNo === activeGroupNo.value) ?? visibleGroups.value[0]
    await selectGroup(next?.engineerGroupNo ?? '', true)
  } catch (error) {
    groups.value = []
    clearMemberDetail()
    errorMessage.value = error instanceof Error ? error.message : '設備工程群組載入失敗。'
  } finally { loading.value = false }
}
function applyQuery(): void { void loadGroups() }
function toggleDeleteSelection(groupNo: string): void {
  selectedForDelete.value = selectedForDelete.value.includes(groupNo)
    ? selectedForDelete.value.filter((no) => no !== groupNo)
    : [...selectedForDelete.value, groupNo]
}
function openGroupForm(mode: Exclude<GroupMode, null>): void {
  const row = selectedGroup.value
  if ((mode === 'edit' || mode === 'copy') && !row) return
  groupMode.value = mode
  groupForm.value = mode === 'edit' && row
    ? { engineerGroupNo: row.engineerGroupNo, engineerGroupName: row.engineerGroupName, description: row.description ?? '', dataStamp: row.dataStamp }
    : mode === 'copy' && row
      ? { engineerGroupNo: row.engineerGroupNo, engineerGroupName: row.engineerGroupName, description: row.description ?? '', dataStamp: 0 }
      : { engineerGroupNo: '', engineerGroupName: '', description: '', dataStamp: 0 }
  groupAudit.value = mode === 'edit' && row
    ? { creator: row.creator ?? '', createDate: formatDate(row.createDate), reviser: props.currentUserNo || '', reviseDate: new Date().toLocaleString('zh-TW') }
    : { creator: props.currentUserNo || 'DEV001', createDate: new Date().toLocaleString('zh-TW'), reviser: '', reviseDate: '' }
}
async function saveGroup(): Promise<void> {
  if (busy.value || !groupMode.value) return
  busy.value = true
  errorMessage.value = ''
  const mode = groupMode.value
  const groupNo = groupForm.value.engineerGroupNo.trim()
  try {
    let result: EngineerGroup
    if (usesDevelopmentMock) {
      const rows = readMock<EngineerGroup[]>(groupStoreKey, seedGroups)
      const index = rows.findIndex((row) => row.engineerGroupNo.toLocaleLowerCase() === groupNo.toLocaleLowerCase())
      if (mode !== 'edit' && index >= 0) throw new Error('工程群組編號已存在。')
      const now = new Date().toISOString()
      if (mode === 'edit') {
        if (index < 0) throw new Error('找不到要修改的工程群組。')
        const original = rows[index]
        result = { ...original, engineerGroupName: groupForm.value.engineerGroupName.trim(), description: groupForm.value.description.trim() || null, reviser: props.currentUserNo || 'DEV001', reviseDate: now, dataStamp: original.dataStamp + 1 }
        rows[index] = result
      } else {
        result = { engineerGroupNo: groupNo, engineerGroupName: groupForm.value.engineerGroupName.trim(), description: groupForm.value.description.trim() || null, creator: props.currentUserNo || 'DEV001', createDate: now, reviser: null, reviseDate: null, dataStamp: 0 }
        rows.push(result)
      }
      writeMock(groupStoreKey, rows)
      if (mode === 'copy' && selectedGroup.value) {
        const members = readMock<Record<string, string[]>>(memberStoreKey, seedMembers)
        members[groupNo] = [...(members[selectedGroup.value.engineerGroupNo] ?? [])]
        writeMock(memberStoreKey, members)
      }
    } else {
      const body = { engineerGroupNo: groupNo, engineerGroupName: groupForm.value.engineerGroupName.trim(), description: groupForm.value.description.trim(), dataStamp: groupForm.value.dataStamp }
      const sourceGroupNo = selectedGroup.value?.engineerGroupNo
      const endpoint = mode === 'edit'
        ? 'api/equipment/engineer-groups/' + encodeURIComponent(groupNo)
        : mode === 'copy' && sourceGroupNo
          ? 'api/equipment/engineer-groups/' + encodeURIComponent(sourceGroupNo) + '/copy'
          : 'api/equipment/engineer-groups'
      result = await requestJson<EngineerGroup>(endpoint, {
        method: mode === 'edit' ? 'PUT' : 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      })
    }
    groupMode.value = null
    keyword.value = ''
    activeGroupNo.value = result.engineerGroupNo
    await loadGroups()
    successMessage.value = mode === 'edit' ? '工程群組資料已修改。' : mode === 'copy' ? '工程群組資料已複製。' : '工程群組資料已新增。'
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '工程群組資料儲存失敗。' }
  finally { busy.value = false }
}
async function deleteGroups(): Promise<void> {
  const targets = selectedForDelete.value.length
    ? selectedForDelete.value
    : selectedGroup.value ? [selectedGroup.value.engineerGroupNo] : []
  if (!targets.length || busy.value || !window.confirm('確定刪除選取的 ' + targets.length + ' 筆設備工程群組及其成員設定？')) return
  busy.value = true
  errorMessage.value = ''
  try {
    if (usesDevelopmentMock) {
      const removed = new Set(targets)
      writeMock(groupStoreKey, readMock<EngineerGroup[]>(groupStoreKey, seedGroups).filter((row) => !removed.has(row.engineerGroupNo)))
      const members = readMock<Record<string, string[]>>(memberStoreKey, seedMembers)
      for (const groupNo of targets) delete members[groupNo]
      writeMock(memberStoreKey, members)
    } else {
      for (const groupNo of targets) {
        await requestJson<void>('api/equipment/engineer-groups/' + encodeURIComponent(groupNo), { method: 'DELETE' })
      }
    }
    selectedForDelete.value = []
    if (targets.includes(activeGroupNo.value)) clearMemberDetail()
    await loadGroups()
    successMessage.value = '設備工程群組已刪除。'
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '設備工程群組刪除失敗。' }
  finally { busy.value = false }
}
async function saveMembers(): Promise<void> {
  const row = selectedGroup.value
  if (!row || busy.value) return
  busy.value = true
  errorMessage.value = ''
  try {
    const userNos = [...new Set(checkedUserNos.value)]
    if (usesDevelopmentMock) {
      const members = readMock<Record<string, string[]>>(memberStoreKey, seedMembers)
      members[row.engineerGroupNo] = userNos
      writeMock(memberStoreKey, members)
    } else {
      await requestJson<void>('api/equipment/engineer-groups/' + encodeURIComponent(row.engineerGroupNo) + '/users', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userNos }),
      })
    }
    checkedUserNos.value = userNos
    savedUserNos.value = [...userNos]
    successMessage.value = '群組使用者設定已儲存。'
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '群組使用者設定儲存失敗。' }
  finally { busy.value = false }
}
function closePage(): void {
  window.history.pushState({}, '', import.meta.env.BASE_URL)
  window.dispatchEvent(new PopStateEvent('popstate'))
}
onMounted(() => { void loadGroups() })
</script>

<template>
  <div class="engineer-group-page">
    <div v-if="usesDevelopmentMock" class="mock-note" role="status">Development 假資料｜操作結果保存在此瀏覽器，不會寫入正式資料庫</div>
    <form class="group-query" @submit.prevent="applyQuery">
      <label for="engineer-group-query">工程群組編號</label>
      <input id="engineer-group-query" v-model="keyword" type="search" />
      <span>%</span>
      <button type="submit" :disabled="loading">查詢</button>
    </form>
    <div v-if="errorMessage" class="group-message error" role="alert">{{ errorMessage }}</div>
    <div v-if="successMessage" class="group-message success" role="status">{{ successMessage }}</div>

    <section class="group-panel group-basis">
      <header class="panel-title"><h2>設備工程群組資料</h2><span>資料總筆數 {{ visibleGroups.length }}</span></header>
      <div class="group-table-layout">
        <div class="table-scroll">
          <table class="group-table">
            <thead><tr><th class="check-column">刪除</th><th>工程群組編號</th><th>工程群組名稱</th><th>說明</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th></tr></thead>
            <tbody>
              <tr v-for="row in visibleGroups" :key="row.engineerGroupNo" tabindex="0" :aria-selected="activeGroupNo === row.engineerGroupNo" :class="{ active: activeGroupNo === row.engineerGroupNo }" @click="selectGroup(row.engineerGroupNo)" @keydown.enter.prevent="selectGroup(row.engineerGroupNo)">
                <td class="check-column" @click.stop><input type="checkbox" :checked="selectedForDelete.includes(row.engineerGroupNo)" :aria-label="'選取刪除 ' + row.engineerGroupNo" @change="toggleDeleteSelection(row.engineerGroupNo)" /></td>
                <td class="group-code">{{ row.engineerGroupNo }}</td><td>{{ row.engineerGroupName }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td>
              </tr>
              <tr v-if="!loading && visibleGroups.length === 0"><td colspan="8" class="empty-row">{{ groups.length ? '查無符合條件的工程群組。' : '目前沒有工程群組資料。' }}</td></tr>
              <tr v-if="loading"><td colspan="8" class="empty-row">工程群組載入中…</td></tr>
            </tbody>
          </table>
        </div>
        <aside class="group-actions" aria-label="工程群組操作">
          <button class="primary" type="button" :disabled="busy" @click="openGroupForm('new')">新增(A)</button>
          <button type="button" :disabled="busy || !selectedGroup" @click="openGroupForm('edit')">修改(E)</button>
          <button type="button" :disabled="busy || !selectedGroup" @click="openGroupForm('copy')">複製(C)</button>
          <button type="button" :disabled="busy || (!selectedGroup && selectedForDelete.length === 0)" @click="deleteGroups">刪除(D)</button>
          <button type="button" @click="closePage">關閉</button>
        </aside>
      </div>
    </section>

    <section class="group-panel group-members">
      <header class="panel-title"><h2>工程群組使用者</h2><span>{{ selectedGroup?.engineerGroupNo || '請先選擇工程群組' }}</span></header>
      <div class="member-toolbar">
        <label><input v-model="showSelectedOnly" type="checkbox" />只顯示已選使用者</label>
        <span>已選 {{ checkedUserNos.length }} / {{ users.length }} 人</span>
        <span v-if="membershipDirty" class="unsaved-note">有尚未確認的變更</span>
      </div>
      <div class="table-scroll member-table-scroll">
        <table class="group-table member-table">
          <thead><tr><th class="check-column">加入</th><th>使用者編號</th><th>使用者名稱</th></tr></thead>
          <tbody>
            <tr v-for="user in visibleUsers" :key="user.userNo">
              <td class="check-column"><input v-model="checkedUserNos" type="checkbox" :value="user.userNo" :disabled="!selectedGroup || membersLoading || busy" :aria-label="'加入使用者 ' + user.userNo" /></td>
              <td class="group-code">{{ user.userNo }}</td><td>{{ user.userName }}</td>
            </tr>
            <tr v-if="membersLoading"><td colspan="3" class="empty-row">使用者資料載入中…</td></tr>
            <tr v-else-if="!selectedGroup"><td colspan="3" class="empty-row">選擇工程群組後設定成員。</td></tr>
            <tr v-else-if="visibleUsers.length === 0"><td colspan="3" class="empty-row">{{ users.length ? '此群組目前沒有已選使用者。取消「只顯示已選使用者」可加入成員。' : '目前沒有可設定的使用者。' }}</td></tr>
          </tbody>
        </table>
      </div>
      <footer class="member-footer"><span>資料總筆數 {{ visibleUsers.length }}</span><button class="primary" type="button" :disabled="busy || !selectedGroup || membersLoading" @click="saveMembers">確認</button></footer>
    </section>

    <div v-if="groupMode" class="group-overlay" @click.self="groupMode = null">
      <form class="group-dialog" @submit.prevent="saveGroup">
        <header><strong>設備工程群組資料 ({{ groupMode === 'edit' ? 'EDIT' : groupMode === 'copy' ? 'COPY' : 'ADD' }})</strong><button type="button" aria-label="關閉" @click="groupMode = null">×</button></header>
        <fieldset><legend>工程群組資料</legend>
          <label><span>工程群組編號</span><input v-model.trim="groupForm.engineerGroupNo" required maxlength="20" :readonly="groupMode === 'edit'" /></label>
          <label><span>工程群組名稱</span><input v-model.trim="groupForm.engineerGroupName" required maxlength="50" /></label>
          <label class="description-field"><span>說明</span><textarea v-model="groupForm.description" maxlength="255" rows="4" /></label>
        </fieldset>
        <div class="group-audit">
          <label>建立人<input :value="groupAudit.creator" readonly /></label><label>建立日期<input :value="groupAudit.createDate" readonly /></label>
          <label>修改人<input :value="groupAudit.reviser" readonly /></label><label>修改日期<input :value="groupAudit.reviseDate" readonly /></label>
        </div>
        <footer class="dialog-actions"><button type="button" @click="groupMode = null">關閉</button><button class="primary" type="submit" :disabled="busy">確定</button></footer>
      </form>
    </div>
  </div>
</template>

<style scoped>
.engineer-group-page{display:grid;gap:8px;min-width:0;padding:8px;color:#26251f;font-size:13px}
.mock-note{padding:6px 10px;border:1px solid #d8c77b;background:#fff9dd;color:#665810;font-size:12px}
.group-query{display:flex;min-height:46px;align-items:center;gap:8px;flex-wrap:wrap;padding:5px 8px;border:1px solid #d6d2c7;background:#fff}
.group-query input{width:min(240px,40vw);height:29px;padding:4px 7px;border:1px solid #aaa;background:#fff}
.group-query button,.group-actions button,.dialog-actions button,.member-footer button{min-height:30px;padding:4px 10px;border:1px solid #c4c4c4;border-radius:3px;background:linear-gradient(#fff,#f4f3ef);color:#333}
.group-query button:disabled,.group-actions button:disabled,.dialog-actions button:disabled,.member-footer button:disabled{color:#aaa;cursor:not-allowed}
.group-panel{min-width:0;border:1px solid #c7c4bb;background:#fff}
.panel-title{display:flex;align-items:center;justify-content:space-between;gap:12px;padding:7px 9px;border-bottom:1px solid #dedbd2;background:#faf9f6}
.panel-title h2{margin:0;font-size:14px;font-weight:600}
.panel-title span,.member-toolbar>span:first-of-type{color:#777366;font-size:12px}
.group-table-layout{display:grid;grid-template-columns:minmax(0,1fr) 100px;gap:8px;padding:7px}
.table-scroll{min-width:0;overflow:auto}
.group-table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}
.group-table th{height:30px;padding:5px 8px;border:1px solid #c7b852;background:#e7c600;text-align:left;font-weight:500}
.group-table td{height:31px;padding:5px 8px;border:1px solid #d0d0d0;max-width:280px;overflow:hidden;text-overflow:ellipsis}
.group-table tbody tr{cursor:pointer}
.group-table tbody tr:hover{background:#faf7e7}
.group-table tbody tr.active{background:#fff0b5;box-shadow:inset 3px 0 #b59a00}
.group-table tbody tr:focus-visible{outline:2px solid #b59a00;outline-offset:-2px}
.group-code{font-family:'Segoe UI',sans-serif}
.check-column{width:48px;text-align:center!important}
.group-table input[type=checkbox],.member-toolbar input{accent-color:#b59a00}
.empty-row{text-align:center;color:#777366}
.group-actions{display:flex;flex-direction:column;gap:5px}
.group-actions button{text-align:center}
.group-actions button.primary,.dialog-actions button.primary,.member-footer button.primary{border-color:#b59a00;background:#e7c600;color:#26251f;font-weight:600}
.group-members{display:grid;grid-template-rows:auto auto minmax(140px,1fr) auto;min-height:250px}
.member-toolbar{display:flex;align-items:center;gap:18px;padding:7px 10px;border-bottom:1px solid #e5e1d5}
.member-toolbar label{display:flex;align-items:center;gap:6px}
.unsaved-note{color:#806900;font-size:12px}
.member-table-scroll{max-height:340px}
.member-table .check-column{width:58px}
.member-footer{display:flex;align-items:center;justify-content:space-between;gap:12px;padding:7px 10px;border-top:1px solid #e5e1d5;color:#777366;font-size:12px}
.group-message{padding:7px 10px;border:1px solid;font-size:12px}
.group-message.error{border-color:#d5a3a3;background:#fff5f3;color:#7a2424}
.group-message.success{border-color:#b6c9a8;background:#f5f8ef;color:#435c32}
.group-overlay{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:18px;background:rgb(20 20 16 / 35%)}
.group-dialog{width:min(680px,100%);max-height:min(90vh,740px);overflow:auto;border:1px solid #c4c1b8;background:#fff;box-shadow:0 8px 28px rgb(0 0 0 / 20%)}
.group-dialog>header{display:flex;align-items:center;justify-content:space-between;padding:9px 12px;border-bottom:1px solid #ddd;background:#f8f7f3}
.group-dialog>header button{width:30px;height:28px;border:1px solid #ccc;background:#fff;font-size:20px;line-height:1}
.group-dialog fieldset{display:grid;grid-template-columns:1fr 1fr;gap:12px 18px;margin:12px;padding:14px;border:1px solid #c8c5bc}
.group-dialog legend{padding:0 5px;color:#555}
.group-dialog fieldset>label{display:grid;grid-template-columns:120px minmax(0,1fr);align-items:center;gap:8px}
.group-dialog input,.group-dialog textarea{width:100%;min-height:30px;padding:5px 7px;border:1px solid #999;background:#fff0ae;color:#26251f;font:inherit}
.group-dialog input:read-only{background:#eee}
.group-dialog textarea{resize:vertical}
.group-dialog .description-field{grid-column:1/-1;align-items:start}
.group-audit{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:8px;margin:0 12px 12px;padding:10px;border:1px solid #cfcbbd;background:#fbfaf5}
.group-audit label{display:grid;gap:4px;color:#666;font-size:11px}
.group-audit input{width:100%;min-height:28px;padding:4px;border:1px solid #ccc;background:#eee;color:#555;font:inherit}
.dialog-actions{display:flex;justify-content:flex-end;gap:8px;padding:4px 12px 12px}
@media(max-width:850px){.group-table{min-width:950px}.member-table{min-width:500px}.group-table-layout{grid-template-columns:minmax(0,1fr) 88px}.group-actions button{padding:4px;font-size:11px}.group-dialog fieldset{grid-template-columns:1fr}.group-dialog .description-field{grid-column:auto}.group-audit{grid-template-columns:1fr 1fr}}
</style>