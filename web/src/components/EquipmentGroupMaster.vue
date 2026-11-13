<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type IssueState = -1 | 0 | 1 | 2
type GroupMode = 'new' | 'edit' | null
interface EquipmentGroup {
  equipmentGroup: string
  description: string
  creator: string
  createDate: string
  reviser: string | null
  reviseDate: string | null
  issueState: IssueState
  dataStamp: number
}
interface GroupEquipment {
  equipmentGroup: string
  equipmentNo: string
}
interface EquipmentOption {
  equipmentNo: string
  equipmentType: string
  equipmentName: string
  issueState: number
  chamberFlag: number
}

const props = defineProps<{ apiBaseUrl: string; currentUserNo?: string }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mock = import.meta.env.DEV
const keys = {
  groups: 'ymes-dev-mock:equipment-groups:v1',
  members: 'ymes-dev-mock:equipment-group-members:v1',
}
const states: { value: IssueState; label: string }[] = [
  { value: 0, label: '未審核' }, { value: 1, label: '審核中' },
  { value: 2, label: '已審核' }, { value: -1, label: '不使用' },
]
const seedGroups: EquipmentGroup[] = [
  { equipmentGroup: 'DEV-EQP-GROUP-01', description: '開發假資料：前段製程設備群', creator: 'DEV001', createDate: '2026-11-01T08:00:00+08:00', reviser: null, reviseDate: null, issueState: 0, dataStamp: 0 },
  { equipmentGroup: 'DEV-EQP-GROUP-02', description: '開發假資料：量測設備群', creator: 'DEV001', createDate: '2026-10-18T09:20:00+08:00', reviser: 'DEV002', reviseDate: '2026-10-20T14:00:00+08:00', issueState: 1, dataStamp: 1 },
  { equipmentGroup: 'DEV-EQP-GROUP-03', description: '開發假資料：後段設備群', creator: 'DEV002', createDate: '2026-10-02T10:00:00+08:00', reviser: 'DEV002', reviseDate: '2026-10-04T10:00:00+08:00', issueState: 2, dataStamp: 2 },
]
const seedMembers: Record<string, string[]> = {
  'DEV-EQP-GROUP-01': ['DEV-EQP-001', 'DEV-EQP-002'],
  'DEV-EQP-GROUP-02': ['DEV-EQP-003'],
  'DEV-EQP-GROUP-03': [],
}
const seedEquipment: EquipmentOption[] = [
  { equipmentNo: 'DEV-EQP-001', equipmentType: 'DEV-TYPE-CNC', equipmentName: '開發用加工設備 A', issueState: 2, chamberFlag: 0 },
  { equipmentNo: 'DEV-EQP-002', equipmentType: 'DEV-TYPE-CNC', equipmentName: '開發用加工設備 B', issueState: 2, chamberFlag: 0 },
  { equipmentNo: 'DEV-EQP-003', equipmentType: 'DEV-TYPE-MEASURE', equipmentName: '開發用量測設備 A', issueState: 2, chamberFlag: 0 },
  { equipmentNo: 'DEV-EQP-004', equipmentType: 'DEV-TYPE-CHAMBER', equipmentName: '開發用腔體設備', issueState: 2, chamberFlag: 1 },
  { equipmentNo: 'DEV-EQP-005', equipmentType: 'DEV-TYPE-CNC', equipmentName: '未核准設備（不可加入）', issueState: 0, chamberFlag: 0 },
  { equipmentNo: 'DEV-EQP-006', equipmentType: 'DEV-TYPE-ROBOT', equipmentName: '尚未分組設備', issueState: 2, chamberFlag: 0 },
]
const rows = ref<EquipmentGroup[]>([])
const details = ref<GroupEquipment[]>([])
const availableRows = ref<EquipmentOption[]>([])
const status = ref<IssueState>(0)
const keyword = ref('')
const currentNo = ref('')
const checkedGroups = ref<string[]>([])
const checkedEquipment = ref<string[]>([])
const checkedAvailable = ref<string[]>([])
const availableKeyword = ref('')
const loading = ref(false)
const busy = ref(false)
const error = ref('')
const notice = ref('')
const mode = ref<GroupMode>(null)
const form = ref({ equipmentGroup: '', description: '' })
const addEquipmentOpen = ref(false)

const current = computed(() => rows.value.find((x) => x.equipmentGroup === currentNo.value) ?? null)
const visibleRows = computed(() => rows.value
  .filter((x) => x.issueState === status.value)
  .filter((x) => x.equipmentGroup.toLowerCase().includes(keyword.value.trim().toLowerCase()))
  .slice().sort((a, b) => a.equipmentGroup.localeCompare(b.equipmentGroup)))
const filteredAvailable = computed(() => availableRows.value
  .filter((x) => x.equipmentNo.toLowerCase().includes(availableKeyword.value.trim().toLowerCase())
    || x.equipmentName.toLowerCase().includes(availableKeyword.value.trim().toLowerCase())
    || x.equipmentType.toLowerCase().includes(availableKeyword.value.trim().toLowerCase())))

function read<T>(key: string, initial: T): T {
  try {
    const value = localStorage.getItem(key)
    return value ? JSON.parse(value) as T : structuredClone(initial)
  } catch { return structuredClone(initial) }
}
function write<T>(key: string, value: T): void { localStorage.setItem(key, JSON.stringify(value)) }
function stateName(value: number): string { return states.find((x) => x.value === value)?.label ?? String(value) }
function dateText(value?: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.valueOf()) ? value : date.toLocaleString('zh-TW')
}
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  const root = new URL(base.endsWith('/') ? base : base + '/', window.location.origin)
  return new URL(path.replace(/^\/+/, ''), root).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init })
  if (response.status === 401) {
    window.location.assign(appBase + '/')
    throw new Error('登入狀態已逾期，請重新登入。')
  }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    throw new Error(problem?.detail || problem?.message || problem?.title || '設備群組 API 回應錯誤 (' + response.status + ')。')
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function mockAvailable(): EquipmentOption[] {
  const memberMap = read<Record<string, string[]>>(keys.members, seedMembers)
  const assigned = new Set(Object.values(memberMap).flat())
  return seedEquipment.filter((x) => x.issueState === 2 && x.chamberFlag === 0 && !assigned.has(x.equipmentNo))
}
async function loadDetails(): Promise<void> {
  const group = current.value
  if (!group) { details.value = []; return }
  try {
    if (mock) {
      const memberMap = read<Record<string, string[]>>(keys.members, seedMembers)
      details.value = (memberMap[group.equipmentGroup] ?? []).map((equipmentNo) => ({ equipmentGroup: group.equipmentGroup, equipmentNo }))
    } else {
      details.value = await request<GroupEquipment[]>('api/equipment/groups/' + encodeURIComponent(group.equipmentGroup) + '/equipment')
    }
    checkedEquipment.value = checkedEquipment.value.filter((no) => details.value.some((x) => x.equipmentNo === no))
  } catch (e) {
    details.value = []
    error.value = e instanceof Error ? e.message : '設備群組明細載入失敗。'
  }
}
async function loadGroups(): Promise<void> {
  loading.value = true
  error.value = ''
  notice.value = ''
  try {
    if (mock) rows.value = read<EquipmentGroup[]>(keys.groups, seedGroups)
    else {
      const query = new URLSearchParams()
      if (keyword.value.trim()) query.set('equipmentGroup', keyword.value.trim())
      const suffix = query.toString()
      rows.value = await request<EquipmentGroup[]>('api/equipment/groups' + (suffix ? '?' + suffix : ''))
    }
    checkedGroups.value = checkedGroups.value.filter((key) => rows.value.some((x) => x.equipmentGroup === key))
    if (!visibleRows.value.some((x) => x.equipmentGroup === currentNo.value)) {
      currentNo.value = visibleRows.value[0]?.equipmentGroup ?? ''
    }
    await loadDetails()
  } catch (e) {
    rows.value = []
    currentNo.value = ''
    details.value = []
    error.value = e instanceof Error ? e.message : '設備群組清單載入失敗。'
  } finally { loading.value = false }
}
function query(): void { void loadGroups() }
function applyStatus(): void {
  if (!visibleRows.value.some((x) => x.equipmentGroup === currentNo.value)) {
    currentNo.value = visibleRows.value[0]?.equipmentGroup ?? ''
    void loadDetails()
  }
}
function selectGroup(group: string): void {
  if (currentNo.value === group) return
  currentNo.value = group
  checkedEquipment.value = []
  void loadDetails()
}
function toggleGroup(group: string): void {
  checkedGroups.value = checkedGroups.value.includes(group)
    ? checkedGroups.value.filter((x) => x !== group) : [...checkedGroups.value, group]
}
function toggleEquipment(no: string): void {
  checkedEquipment.value = checkedEquipment.value.includes(no)
    ? checkedEquipment.value.filter((x) => x !== no) : [...checkedEquipment.value, no]
}
function closePage(): void {
  window.history.pushState({}, '', appBase || '/')
  window.dispatchEvent(new PopStateEvent('popstate'))
}
function openGroup(modeValue: Exclude<GroupMode, null>): void {
  const row = current.value
  if (modeValue === 'edit' && !row) return
  if (modeValue === 'edit' && row?.issueState !== 0) {
    error.value = '只有未審核的設備群組可以修改。'
    return
  }
  mode.value = modeValue
  form.value = row && modeValue === 'edit'
    ? { equipmentGroup: row.equipmentGroup, description: row.description ?? '' }
    : { equipmentGroup: '', description: '' }
  error.value = ''
}
function validateGroup(): string {
  if (!form.value.equipmentGroup.trim()) return '設備群組編號為必填。'
  if (form.value.equipmentGroup.trim().length > 50) return '設備群組編號不可超過 50 個字元。'
  if (form.value.description.length > 255) return '說明不可超過 255 個字元。'
  return ''
}
async function saveGroup(): Promise<void> {
  if (!mode.value || busy.value) return
  const problem = validateGroup()
  if (problem) { error.value = problem; return }
  busy.value = true
  error.value = ''
  const saveMode = mode.value
  const equipmentGroup = form.value.equipmentGroup.trim()
  const source = current.value
  const body = {
    equipmentGroup,
    description: form.value.description.trim() || null,
    ...(saveMode === 'edit' && source ? { issueState: source.issueState, dataStamp: source.dataStamp } : {}),
  }
  try {
    if (mock) {
      const list = read<EquipmentGroup[]>(keys.groups, seedGroups)
      const index = list.findIndex((x) => x.equipmentGroup === equipmentGroup)
      if (saveMode === 'new' && index >= 0) throw new Error('設備群組編號已存在。')
      const now = new Date().toISOString()
      if (saveMode === 'edit') {
        if (!source || index < 0) throw new Error('找不到要修改的設備群組。')
        list[index] = { ...list[index], ...body, description: body.description ?? '', reviser: props.currentUserNo || 'DEV001', reviseDate: now, dataStamp: list[index].dataStamp + 1 }
      } else {
        list.push({ ...body, description: body.description ?? '', creator: props.currentUserNo || 'DEV001', createDate: now, reviser: null, reviseDate: null, issueState: 0, dataStamp: 0 })
      }
      write(keys.groups, list)
    } else if (saveMode === 'edit' && source) {
      await request<void>('api/equipment/groups/' + encodeURIComponent(source.equipmentGroup), {
        method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body),
      })
    } else {
      await request<void>('api/equipment/groups', {
        method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body),
      })
    }
    const savedKey = equipmentGroup
    mode.value = null
    await loadGroups()
    currentNo.value = savedKey
    await loadDetails()
    notice.value = saveMode === 'edit' ? '設備群組已修改。' : '設備群組已新增。'
  } catch (e) { error.value = e instanceof Error ? e.message : '設備群組儲存失敗。' }
  finally { busy.value = false }
}
async function groupAction(action: 'delete' | 'approve'): Promise<void> {
  const selected = rows.value.filter((x) => checkedGroups.value.includes(x.equipmentGroup))
  const targets = selected.length ? selected : current.value ? [current.value] : []
  const valid = targets.filter((x) => action === 'approve' ? x.issueState === 0 : [0, -1, 2].includes(x.issueState))
  if (!valid.length) {
    error.value = action === 'approve' ? '只有未審核的設備群組可以核准。' : '選取的設備群組狀態不允許刪除。'
    return
  }
  if (!window.confirm('確定' + (action === 'approve' ? '核准' : '刪除') + ' ' + valid.length + ' 筆設備群組？')) return
  busy.value = true
  error.value = ''
  try {
    if (mock) {
      const list = read<EquipmentGroup[]>(keys.groups, seedGroups)
      if (action === 'approve') {
        for (const target of valid) {
          const item = list.find((x) => x.equipmentGroup === target.equipmentGroup)
          if (item) {
            item.issueState = 2
            item.dataStamp++
            item.reviser = props.currentUserNo || 'DEV001'
            item.reviseDate = new Date().toISOString()
          }
        }
        write(keys.groups, list)
      } else {
        const removed = new Set(valid.map((x) => x.equipmentGroup))
        write(keys.groups, list.filter((x) => !removed.has(x.equipmentGroup)))
        const members = read<Record<string, string[]>>(keys.members, seedMembers)
        for (const key of removed) delete members[key]
        write(keys.members, members)
      }
    } else {
      for (const item of valid) {
        const path = 'api/equipment/groups/' + encodeURIComponent(item.equipmentGroup)
        if (action === 'approve') {
          await request<void>(path + '/approve', {
            method: 'POST', headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ issueState: item.issueState, dataStamp: item.dataStamp }),
          })
        } else {
          await request<void>(path + '?issueState=' + item.issueState + '&dataStamp=' + item.dataStamp, { method: 'DELETE' })
        }
      }
    }
    checkedGroups.value = []
    await loadGroups()
    notice.value = '已' + (action === 'approve' ? '核准' : '刪除') + ' ' + valid.length + ' 筆設備群組。'
  } catch (e) { error.value = e instanceof Error ? e.message : '設備群組操作失敗。' }
  finally { busy.value = false }
}
async function openAddEquipment(): Promise<void> {
  if (!current.value) return
  if (current.value.issueState !== 0) { error.value = '只有未審核的設備群組可以調整設備明細。'; return }
  try {
    if (mock) availableRows.value = mockAvailable()
    else availableRows.value = await request<EquipmentOption[]>('api/equipment/groups/' + encodeURIComponent(current.value.equipmentGroup) + '/available-equipment')
    checkedAvailable.value = []
    availableKeyword.value = ''
    addEquipmentOpen.value = true
  } catch (e) { error.value = e instanceof Error ? e.message : '可加入設備清單載入失敗。' }
}
async function addEquipment(): Promise<void> {
  if (!current.value || !checkedAvailable.value.length || busy.value) return
  busy.value = true
  error.value = ''
  try {
    if (mock) {
      const map = read<Record<string, string[]>>(keys.members, seedMembers)
      const members = new Set(map[current.value.equipmentGroup] ?? [])
      for (const no of checkedAvailable.value) members.add(no)
      map[current.value.equipmentGroup] = [...members]
      write(keys.members, map)
    } else {
      await request<void>('api/equipment/groups/' + encodeURIComponent(current.value.equipmentGroup) + '/equipment', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ equipmentNos: checkedAvailable.value }),
      })
    }
    addEquipmentOpen.value = false
    await loadDetails()
    notice.value = '設備群組明細已新增。'
  } catch (e) { error.value = e instanceof Error ? e.message : '設備群組明細新增失敗。' }
  finally { busy.value = false }
}
async function deleteEquipment(): Promise<void> {
  if (!current.value || current.value.issueState !== 0) {
    error.value = '只有未審核的設備群組可以調整設備明細。'
    return
  }
  const selected = [...checkedEquipment.value]
  if (!selected.length) return
  if (!window.confirm('確定從此群組移除 ' + selected.length + ' 台設備？')) return
  busy.value = true
  error.value = ''
  try {
    if (mock) {
      const map = read<Record<string, string[]>>(keys.members, seedMembers)
      map[current.value.equipmentGroup] = (map[current.value.equipmentGroup] ?? []).filter((x) => !selected.includes(x))
      write(keys.members, map)
    } else {
      await request<void>('api/equipment/groups/' + encodeURIComponent(current.value.equipmentGroup) + '/equipment', {
        method: 'DELETE', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ equipmentNos: selected }),
      })
    }
    checkedEquipment.value = []
    await loadDetails()
    notice.value = '已從群組移除 ' + selected.length + ' 台設備。'
  } catch (e) { error.value = e instanceof Error ? e.message : '設備群組明細刪除失敗。' }
  finally { busy.value = false }
}
onMounted(() => { void loadGroups() })
</script>

<template>
  <section class="eqgroup-page">
    <div v-if="mock" class="eqgroup-dev-note">開發假資料模式｜資料只保存在本機瀏覽器；正式環境只呼叫 API。</div>
    <div v-if="error" class="eqgroup-message error" role="alert">{{ error }}<button type="button" @click="error = ''">×</button></div>
    <div v-if="notice" class="eqgroup-message success" role="status">{{ notice }}<button type="button" @click="notice = ''">×</button></div>

    <section class="eqgroup-query">
      <fieldset><legend>設備群組</legend><label v-for="item in states" :key="item.value">
        <input v-model="status" type="radio" :value="item.value" @change="applyStatus" />{{ item.label }}
      </label></fieldset>
      <label for="eqgroup-keyword">設備群組編號</label>
      <input id="eqgroup-keyword" v-model.trim="keyword" @keyup.enter="query" />
      <span>%</span><button type="button" :disabled="loading" @click="query">查詢</button>
    </section>

    <div class="eqgroup-workspace">
      <section class="eqgroup-panel">
        <header><strong>設備群組主檔</strong><span>資料總筆數 {{ visibleRows.length }}</span></header>
        <div class="eqgroup-panel-body">
          <div class="eqgroup-grid-scroll">
            <table class="eqgroup-table"><thead><tr>
              <th class="check"><input type="checkbox" aria-label="全選設備群組" :checked="visibleRows.length>0&&visibleRows.every((x)=>checkedGroups.includes(x.equipmentGroup))" @change="checkedGroups=($event.target as HTMLInputElement).checked?visibleRows.map((x)=>x.equipmentGroup):[]" /></th>
              <th>設備群組</th><th>說明</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th>
            </tr></thead><tbody>
              <tr v-for="row in visibleRows" :key="row.equipmentGroup" :class="{selected:row.equipmentGroup===currentNo}" tabindex="0" @click="selectGroup(row.equipmentGroup)" @keydown.enter="selectGroup(row.equipmentGroup)">
                <td class="check" @click.stop><input type="checkbox" :checked="checkedGroups.includes(row.equipmentGroup)" :aria-label="'選取 '+row.equipmentGroup" @change="toggleGroup(row.equipmentGroup)" /></td>
                <td>{{ row.equipmentGroup }}</td><td class="description">{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ dateText(row.createDate) }}</td><td>{{ row.reviser || '' }}</td><td>{{ dateText(row.reviseDate) }}</td>
              </tr>
              <tr v-if="!visibleRows.length"><td class="empty" colspan="7">{{ loading?'載入中…':'沒有符合的設備群組' }}</td></tr>
            </tbody></table>
          </div>
          <div class="eqgroup-actions">
            <button type="button" class="primary" :disabled="busy" @click="openGroup('new')">新增(A)</button>
            <button type="button" :disabled="!current||busy" @click="openGroup('edit')">修改(E)</button>
            <button type="button" class="danger" :disabled="(!current&&!checkedGroups.length)||busy" @click="groupAction('delete')">刪除(D)</button>
            <button type="button" :disabled="(!current&&!checkedGroups.length)||busy" @click="groupAction('approve')">核准</button>
          </div>
        </div>
      </section>

      <section class="eqgroup-panel">
        <header><strong>設備明細<span v-if="current">｜{{ current.equipmentGroup }}</span></strong><span>資料總筆數 {{ details.length }}</span></header>
        <div class="eqgroup-panel-body">
          <div class="eqgroup-grid-scroll">
            <table class="eqgroup-table"><thead><tr>
              <th class="check"><input type="checkbox" aria-label="全選設備明細" :checked="details.length>0&&details.every((x)=>checkedEquipment.includes(x.equipmentNo))" @change="checkedEquipment=($event.target as HTMLInputElement).checked?details.map((x)=>x.equipmentNo):[]" /></th>
              <th>設備編號</th>
            </tr></thead><tbody>
              <tr v-for="row in details" :key="row.equipmentNo" :class="{selected:checkedEquipment.includes(row.equipmentNo)}" @click="toggleEquipment(row.equipmentNo)">
                <td class="check" @click.stop><input type="checkbox" :checked="checkedEquipment.includes(row.equipmentNo)" @change="toggleEquipment(row.equipmentNo)" /></td>
                <td>{{ row.equipmentNo }}</td>
              </tr>
              <tr v-if="!details.length"><td class="empty" colspan="2">{{ current?'此設備群組尚未配置設備':'請先選取設備群組' }}</td></tr>
            </tbody></table>
          </div>
          <div class="eqgroup-actions detail-actions">
            <button type="button" class="primary" :disabled="!current||current.issueState!==0||busy" @click="openAddEquipment">加入設備</button>
            <button type="button" class="danger" :disabled="!current||current.issueState!==0||!checkedEquipment.length||busy" @click="deleteEquipment">移除設備</button>
            <span></span><button type="button" @click="closePage">關閉</button>
          </div>
        </div>
      </section>
    </div>

    <div v-if="mode" class="eqgroup-overlay" @click.self="mode=null">
      <form class="eqgroup-form" @submit.prevent="saveGroup">
        <header><strong>設備群組資料定義（{{ mode==='edit'?'EDIT':'ADD' }}）</strong><button type="button" aria-label="關閉" @click="mode=null">×</button></header>
        <fieldset><legend>設備群組資料</legend>
          <label><span>設備群組</span><input v-model.trim="form.equipmentGroup" maxlength="50" required :readonly="mode==='edit'" /></label>
          <label class="long"><span>說明</span><textarea v-model="form.description" maxlength="255" rows="4" /></label>
        </fieldset>
        <footer><button class="primary" type="submit" :disabled="busy">確定</button><button type="button" @click="mode=null">關閉</button><button type="button" disabled>套用</button></footer>
      </form>
    </div>

    <div v-if="addEquipmentOpen&&current" class="eqgroup-overlay" @click.self="addEquipmentOpen=false">
      <section class="eqgroup-form eqgroup-picker">
        <header><strong>選擇設備｜{{ current.equipmentGroup }}</strong><button type="button" aria-label="關閉" @click="addEquipmentOpen=false">×</button></header>
        <label class="eqgroup-search"><span>查詢設備</span><input v-model.trim="availableKeyword" placeholder="設備編號、名稱或設備類別" /></label>
        <div class="eqgroup-picker-grid">
          <table class="eqgroup-table"><thead><tr><th class="check">選取</th><th>設備編號</th><th>設備類別</th><th>設備名稱</th></tr></thead><tbody>
            <tr v-for="row in filteredAvailable" :key="row.equipmentNo" @click="checkedAvailable=checkedAvailable.includes(row.equipmentNo)?checkedAvailable.filter((x)=>x!==row.equipmentNo):[...checkedAvailable,row.equipmentNo]">
              <td class="check"><input type="checkbox" :checked="checkedAvailable.includes(row.equipmentNo)" @click.stop @change="checkedAvailable=checkedAvailable.includes(row.equipmentNo)?checkedAvailable.filter((x)=>x!==row.equipmentNo):[...checkedAvailable,row.equipmentNo]" /></td>
              <td>{{ row.equipmentNo }}</td><td>{{ row.equipmentType }}</td><td>{{ row.equipmentName }}</td>
            </tr>
            <tr v-if="!filteredAvailable.length"><td class="empty" colspan="4">沒有可加入的已核准非 Chamber 設備</td></tr>
          </tbody></table>
        </div>
        <footer><button class="primary" type="button" :disabled="!checkedAvailable.length||busy" @click="addEquipment">加入選取設備（{{ checkedAvailable.length }}）</button><button type="button" @click="addEquipmentOpen=false">關閉</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.eqgroup-page{display:flex;min-height:0;height:100%;flex-direction:column;gap:8px;color:#343434;font:13px "Segoe UI","Microsoft JhengHei",sans-serif}
.eqgroup-dev-note{padding:6px 10px;border:1px solid #d7c787;background:#fff6ce;color:#6e5c20}
.eqgroup-message{display:flex;justify-content:space-between;padding:8px 10px;border:1px solid #bbb}.eqgroup-message.error{background:#fff0ed;color:#842d22}.eqgroup-message.success{background:#f1f6e9;color:#3c6122}.eqgroup-message button{border:0;background:none;cursor:pointer}
.eqgroup-query{display:flex;min-height:48px;align-items:center;gap:8px;padding:5px 10px;border:1px solid #d0d0d0;background:#fff}.eqgroup-query fieldset{display:flex;align-items:center;gap:13px;margin:0 10px 0 0;padding:4px 8px;border:0}.eqgroup-query legend{float:left;margin-right:8px;color:#6c5c2e}.eqgroup-query label{display:flex;align-items:center;gap:4px;white-space:nowrap}.eqgroup-query input[type=radio]{accent-color:#8a7c4b}.eqgroup-query>input{width:min(280px,30vw);height:29px;border:1px solid #999;padding:4px 7px}.eqgroup-query>button{height:30px;padding:0 16px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#f1f1f1);cursor:pointer}
.eqgroup-workspace{display:grid;min-height:0;flex:1;grid-template-rows:minmax(180px,1fr) minmax(180px,1fr);gap:8px}
.eqgroup-panel{display:flex;min-height:0;flex-direction:column;border:1px solid #c6c6c6;background:#fff}
.eqgroup-panel>header{display:flex;min-height:31px;align-items:center;justify-content:space-between;padding:4px 9px;border-bottom:1px solid #ccc;background:#f6f6f6}.eqgroup-panel>header span{color:#777;font-size:12px}
.eqgroup-panel-body{display:grid;min-height:0;flex:1;grid-template-columns:minmax(0,1fr) 96px;gap:6px;padding:5px}
.eqgroup-grid-scroll{min-height:0;overflow:auto;border:1px solid #c9c9c9}
.eqgroup-table{width:100%;border-collapse:collapse;white-space:nowrap}.eqgroup-table th{position:sticky;top:0;z-index:1;padding:5px 7px;border:1px solid #c9b75e;background:#f3d878;color:#51491f;text-align:left;font-weight:500}.eqgroup-table td{height:26px;padding:3px 7px;border:1px solid #d3d3d3}.eqgroup-table tbody tr{cursor:pointer}.eqgroup-table tbody tr:hover{background:#fff8df}.eqgroup-table tbody tr.selected{background:#ffeba0;box-shadow:inset 3px 0 #d99c2b}.eqgroup-table .check{width:30px;text-align:center}.eqgroup-table input[type=checkbox]{width:14px;height:14px;accent-color:#77734f}.eqgroup-table .description{max-width:360px;overflow:hidden;text-overflow:ellipsis}.eqgroup-table .empty{height:52px;text-align:center;color:#888}
.eqgroup-actions{display:flex;flex-direction:column;gap:5px;padding:1px 0 1px 4px}.eqgroup-actions button{min-height:32px;padding:4px;border:1px solid #c2c2c2;border-radius:3px;background:linear-gradient(#fff,#f2f2f2);color:#333;font:inherit;cursor:pointer}.eqgroup-actions button.primary,.eqgroup-form footer button.primary{border-color:#e6a02c;background:#ffad34;color:#fff;font-weight:600}.eqgroup-actions button.danger{color:#792d2d}.eqgroup-actions button:disabled,.eqgroup-form button:disabled{color:#aaa;cursor:not-allowed}.eqgroup-actions>span{flex:1}.detail-actions{justify-content:flex-start}
.eqgroup-overlay{position:fixed;inset:0;z-index:45;display:grid;place-items:center;padding:14px;background:#0005}
.eqgroup-form{display:flex;width:min(620px,96vw);max-height:92vh;flex-direction:column;gap:11px;overflow:auto;padding:13px 16px;border:1px solid #aaa;background:#fff;box-shadow:0 8px 30px #0003}
.eqgroup-form>header{display:flex;align-items:center;justify-content:space-between;font-size:15px}.eqgroup-form>header button{width:30px;height:28px;border:1px solid #ccc;background:#fafafa;font-size:19px;cursor:pointer}
.eqgroup-form fieldset{min-width:0;margin:0;padding:12px;border:1px solid #aaa}.eqgroup-form legend{padding:0 5px}
.eqgroup-form fieldset>label,.eqgroup-search{display:grid;grid-template-columns:135px minmax(0,1fr);align-items:center;gap:8px;margin:6px 0}.eqgroup-form input,.eqgroup-form textarea{width:100%;min-height:30px;padding:5px 7px;border:1px solid #a99e6b;border-radius:0;background:#fff1b2;font:inherit}.eqgroup-form input[readonly]{background:#eee}.eqgroup-form textarea{resize:vertical}.eqgroup-form .long{align-items:start}.eqgroup-form footer{display:flex;justify-content:flex-end;gap:8px}.eqgroup-form footer button{min-width:92px;height:36px;border:1px solid #ccc;background:#fafafa;font:inherit;cursor:pointer}
.eqgroup-picker{width:min(960px,97vw)}.eqgroup-search{max-width:600px;margin:0}.eqgroup-search input{background:#fff}.eqgroup-picker-grid{min-height:220px;max-height:55vh;overflow:auto;border:1px solid #c8c8c8}
@media(max-width:900px){.eqgroup-workspace{grid-template-rows:minmax(220px,1fr) minmax(220px,1fr)}.eqgroup-panel-body{grid-template-columns:minmax(0,1fr) 82px}.eqgroup-actions button{font-size:11px}}
@media(max-width:640px){.eqgroup-page{height:auto;min-height:calc(100vh - 100px)}.eqgroup-query{flex-wrap:wrap}.eqgroup-query fieldset{flex-wrap:wrap}.eqgroup-workspace{grid-template-rows:minmax(280px,50vh) minmax(220px,40vh)}.eqgroup-panel-body{grid-template-columns:minmax(0,1fr)}.eqgroup-actions{flex-direction:row;flex-wrap:wrap}.eqgroup-actions button{flex:1}.eqgroup-table{min-width:720px}}
</style>