<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'

type Cell = string | number | null
type Basis = Record<string, Cell> & { qrsNo: string; issueState: number; curStage: string }
type Release = { qrsNo: string; releaseMemo: string; releaseDepartment: string; releaser: string; releaserName: string; releaseDate: string; description: string; issueState: number; creator: string; createDate: string }
type ReleaseLog = { qrsNo: string; releaseType: number; releaseMemo: string; releaseDepartment: string; releaser: string; releaserName: string; releaseDate: string; description: string; creator: string; createDate: string }
type TeamMember = { departmentNo: string; departmentName: string; memberNo: string; memberName: string }
type ChildRecord = { tables?: Record<string, Record<string, unknown>[]>; attachments?: Record<string, string[]> }

const props = defineProps<{ developmentPreview?: boolean; currentUserNo?: string }>()
const emit = defineEmits<{ close: []; open8dQuery: [qrsNo: string] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview !== false)
const unavailableText = '正式環境尚未開放此功能。'
const basisKey = 'ymes-dev-mock:wip-8d-basis-factory:state:v1'
const childKey = 'ymes-dev-mock:wip-8d-maintain:state:v1'
const gridColumns = [
  ['qrsNo', 'QRSNo'], ['releaseMemo', 'ReleaseMemo8D'], ['releaseDepartment', 'ReleaseDepartment'],
  ['releaser', 'Releaser8D'], ['releaserName', 'ReleaserName'], ['releaseDate', 'ReleaseDate8D'],
  ['description', 'Description'], ['creator', 'Creator'], ['createDate', 'CreateDate'],
] as const
const basisRows = ref<Basis[]>([])
const childRows = ref<Record<string, ChildRecord>>({})
const releases = ref<Release[]>([])
const results = ref<Release[]>([])
const issueState = ref(0)
const activeNo = ref('')
const selectedNos = ref<string[]>([])
const searched = ref(false)
const filters = reactive({ qrsNo: '', factoryNo: '', projectDepartment: '', createFrom: '', createTo: '', closeFrom: '', closeTo: '' })
const checks = reactive({ qrsNo: false, factoryNo: false, projectDepartment: false, createDate: false, closeDate: false })
const editorOpen = ref(false)
const lookup = ref<'department' | 'user' | ''>('')
const approveConfirm = ref(false)
const issueJobMode = ref<'direct' | 'workflow'>('direct')
const form = reactive({ releaseDepartment: '', releaser: '', releaserName: '', releaseMemo: '', releaseDate: '', description: '' })
const initialMessage = '先在 WP802 完成 D8 總結並選擇非自動結案，再回此頁查詢結案申請。'
const message = ref(dev.value ? initialMessage : unavailableText)
const error = ref('')
function unavailable(): boolean { if (dev.value) return false; error.value = ''; message.value = unavailableText; return true }

const active = computed(() => dev.value ? results.value.find(row => row.qrsNo === activeNo.value) || null : null)
const activeBasis = computed(() => dev.value ? basisRows.value.find(row => row.qrsNo === activeNo.value) || null : null)
const teams = computed<TeamMember[]>(() => {
  if (!dev.value) return []
  const source = childRows.value[activeNo.value]?.tables?.d1
  const rows = Array.isArray(source) ? source : []
  return rows.map(row => ({ departmentNo: String(row.departmentNo || ''), departmentName: String(row.departmentName || ''), memberNo: String(row.memberNo || ''), memberName: String(row.memberName || '') }))
})
const departments = computed(() => dev.value ? [...new Set(teams.value.map(row => row.departmentNo).filter(Boolean))] : [])
const members = computed(() => dev.value ? teams.value.filter(row => row.departmentNo === form.releaseDepartment) : [])
const canEdit = computed(() => dev.value && active.value?.issueState === 0)
const pendingRows = computed(() => dev.value ? results.value.filter(row => row.issueState === 1) : [])

function load(): void {
  if (!dev.value) return
  try {
    const rows: unknown = JSON.parse(window.localStorage.getItem(basisKey) || '[]')
    basisRows.value = Array.isArray(rows) ? rows.filter(row => row && typeof row.qrsNo === 'string' && typeof row.issueState === 'number') as Basis[] : []
  } catch { basisRows.value = [] }
  try {
    const value: unknown = JSON.parse(window.localStorage.getItem(childKey) || '{}')
    childRows.value = value && typeof value === 'object' && !Array.isArray(value) ? value as Record<string, ChildRecord> : {}
  } catch { childRows.value = {} }
  releases.value = Object.entries(childRows.value).flatMap(([qrsNo, record]) => (Array.isArray(record?.tables?.release) ? record.tables.release : []).map(value => ({
    qrsNo, releaseMemo: String(value.releaseMemo || ''), releaseDepartment: String(value.releaseDepartment || ''),
    releaser: String(value.releaser || ''), releaserName: String(value.releaserName || ''),
    releaseDate: String(value.releaseDate || ''), description: String(value.description || ''),
    issueState: Number(value.issueState ?? 0), creator: String(value.creator || ''), createDate: String(value.createDate || ''),
  }))).filter(row => basisRows.value.some(basis => basis.qrsNo === row.qrsNo))
}

function persist(): void {
  if (!dev.value) return
  window.localStorage.setItem(basisKey, JSON.stringify(basisRows.value))
  window.localStorage.setItem(childKey, JSON.stringify(childRows.value))
}

function readDate(value: string): string { return value.slice(0, 10) }
function matches(row: Release): boolean {
  const basis = basisRows.value.find(item => item.qrsNo === row.qrsNo)
  if (!basis || row.issueState !== issueState.value) return false
  if (checks.qrsNo && filters.qrsNo.trim() && !row.qrsNo.toUpperCase().includes(filters.qrsNo.trim().toUpperCase())) return false
  if (checks.factoryNo && filters.factoryNo.trim() && String(basis.factoryNo || '').toUpperCase() !== filters.factoryNo.trim().toUpperCase()) return false
  if (checks.projectDepartment && filters.projectDepartment.trim() && String(basis.projectDepartment || '').toUpperCase() !== filters.projectDepartment.trim().toUpperCase()) return false
  if (checks.createDate && (readDate(row.createDate) < filters.createFrom || readDate(row.createDate) > filters.createTo)) return false
  if (checks.closeDate && (readDate(row.releaseDate) < filters.closeFrom || readDate(row.releaseDate) > filters.closeTo)) return false
  return true
}

function query(): void {
  error.value = ''
  if (unavailable()) return
  if ((checks.createDate && (!filters.createFrom || !filters.createTo || filters.createFrom > filters.createTo)) ||
      (checks.closeDate && (!filters.closeFrom || !filters.closeTo || filters.closeFrom > filters.closeTo))) {
    error.value = '日期起迄不正確。'; return
  }
  load()
  results.value = releases.value.filter(matches).sort((a, b) => a.qrsNo.localeCompare(b.qrsNo))
  selectedNos.value = []
  activeNo.value = ''
  searched.value = true
  message.value = `查得 ${results.value.length} 筆結案申請。`
}

function choose(row: Release): void { if (unavailable()) return; activeNo.value = row.qrsNo; selectedNos.value = [row.qrsNo] }
function toggleSelected(qrsNo: string): void { if (unavailable()) return;
  activeNo.value = qrsNo
  selectedNos.value = selectedNos.value.includes(qrsNo) ? selectedNos.value.filter(no => no !== qrsNo) : [...selectedNos.value, qrsNo]
}
function openEditor(): void { if (unavailable()) return;
  const row = active.value
  if (!row) { error.value = '請先選取一筆結案申請。'; return }
  if (row.issueState !== 0) { error.value = '只有未簽核申請可修改結案資料。'; return }
  Object.assign(form, { releaseDepartment: row.releaseDepartment, releaser: row.releaser, releaserName: row.releaserName,
    releaseMemo: row.releaseMemo, releaseDate: readDate(row.releaseDate) || new Date().toISOString().slice(0, 10), description: row.description })
  editorOpen.value = true
  error.value = ''
}
function chooseDepartment(no: string): void { if (unavailable()) return; form.releaseDepartment = no; form.releaser = ''; form.releaserName = ''; lookup.value = '' }
function chooseMember(member: TeamMember): void { if (unavailable()) return; form.releaser = member.memberNo; form.releaserName = member.memberName; lookup.value = '' }
function saveEditor(): void { if (unavailable()) return;
  const row = active.value
  if (!row || !canEdit.value) return
  for (const key of ['releaseDepartment', 'releaser', 'releaserName', 'releaseMemo', 'releaseDate'] as const) {
    if (!form[key].trim()) { error.value = `${key} 不可空白。`; return }
  }
  if (Number.isNaN(Date.parse(form.releaseDate))) { error.value = 'CloseDate 不正確。'; return }
  Object.assign(row, { releaseDepartment: form.releaseDepartment, releaser: form.releaser, releaserName: form.releaserName,
    releaseMemo: form.releaseMemo, releaseDate: `${form.releaseDate} 00:00:00`, description: form.description })
  const record = childRows.value[row.qrsNo]
  if (!record) return
  record.tables ||= {}
  record.tables.release = [ { ...row } as Record<string, unknown> ]
  persist()
  editorOpen.value = false
  message.value = `${row.qrsNo} 的 DEV 結案資料已儲存；尚未簽核。`
  error.value = ''
}

function requestApprove(): void {
  if (unavailable()) return
  if (activeNo.value && !selectedNos.value.includes(activeNo.value)) selectedNos.value = [activeNo.value]
  if (!selectedNos.value.length) { error.value = '請先選取申請。'; return }
  const rows = releases.value.filter(row => selectedNos.value.includes(row.qrsNo))
  if (rows.some(row => row.issueState !== 0 || !row.releaseDate)) { error.value = '選取列必須全為未簽核，且已填結案日期。'; return }
  if (rows.some(row => childRows.value[row.qrsNo]?.tables?.releaseLog?.length)) { error.value = '選取列已有結案紀錄，不能重複結案。'; return }
  approveConfirm.value = true
  error.value = ''
}
function finalize(row: Release): void { if (unavailable()) return;
  const basis = basisRows.value.find(item => item.qrsNo === row.qrsNo)
  const record = childRows.value[row.qrsNo]
  if (!basis || !record) return
  record.tables ||= {}
  const now = new Date().toISOString().slice(0, 19).replace('T', ' ')
  const log: ReleaseLog = { qrsNo: row.qrsNo, releaseType: 2, releaseMemo: row.releaseMemo, releaseDepartment: row.releaseDepartment,
    releaser: row.releaser, releaserName: row.releaserName, releaseDate: now, description: row.description,
    creator: props.currentUserNo || 'DEV', createDate: now }
  record.tables.releaseLog = [ { ...log } as Record<string, unknown> ]
  basis.curStage = 'D9'
  basis.closeType = 2
  basis.closeDate = now
  basis.closeDepartment = row.releaseDepartment
  basis.closeUserNo = row.releaser
  basis.closeUserName = row.releaserName
  basis.closeDescription = row.releaseMemo
  basis.lastUpdateDate = now
  basis.dataStamp = Number(basis.dataStamp || 0) + 1
}
function approve(): void {
  if (!approveConfirm.value || !dev.value) return
  const rows = releases.value.filter(row => selectedNos.value.includes(row.qrsNo))
  for (const row of rows) {
    row.issueState = issueJobMode.value === 'workflow' ? 1 : 2
    const record = childRows.value[row.qrsNo]
    if (record) { record.tables ||= {}; record.tables.release = [ { ...row } as Record<string, unknown> ] }
    if (issueJobMode.value === 'direct') finalize(row)
  }
  persist()
  approveConfirm.value = false
  const count = rows.length
  query()
  message.value = issueJobMode.value === 'direct' ? `${count} 筆 DEV 申請已直接核准並結案。` : `${count} 筆 DEV 申請已送簽，狀態 Pending；須待模擬簽核通過才結案。`
}
function completePending(): void {
  const row = active.value
  if (!row || row.issueState !== 1 || !dev.value) return
  if (childRows.value[row.qrsNo]?.tables?.releaseLog?.length) { error.value = '已有結案紀錄。'; return }
  row.issueState = 2
  const record = childRows.value[row.qrsNo]
  if (record) { record.tables ||= {}; record.tables.release = [ { ...row } as Record<string, unknown> ] }
  finalize(row)
  persist()
  const no = row.qrsNo
  query()
  message.value = `${no} 的 DEV 模擬簽核已通過，寫入 ReleaseLog 並將主單推進 D9。`
}

function openDetail(): void { if (unavailable() || !active.value) return; emit('open8dQuery', active.value.qrsNo) }
function openLookup(kind: 'department' | 'user'): void { if (unavailable() || !canEdit.value) return; lookup.value = kind }
onMounted(() => { if (dev.value) query() })
watch(dev, enabled => {
  if (enabled) { error.value = ''; message.value = initialMessage; return }
  basisRows.value = []; childRows.value = {}; releases.value = []; results.value = []
  activeNo.value = ''; selectedNos.value = []; searched.value = false
  editorOpen.value = false; lookup.value = ''; approveConfirm.value = false
  issueState.value = 0; issueJobMode.value = 'direct'
  Object.assign(filters, { qrsNo: '', factoryNo: '', projectDepartment: '', createFrom: '', createTo: '', closeFrom: '', closeTo: '' })
  Object.assign(checks, { qrsNo: false, factoryNo: false, projectDepartment: false, createDate: false, closeDate: false })
  Object.assign(form, { releaseDepartment: '', releaser: '', releaserName: '', releaseMemo: '', releaseDate: '', description: '' })
  error.value = ''; message.value = unavailableText
})
</script>

<template>
  <section class="wp803">
    <div v-if="dev" class="dev-note">DEV 假資料預覽 · 結案申請沿用 WP802 的本機資料；正式簽核及資料庫寫入由 API 執行。</div>
    <h1>8D 矯正單結案作業 <small>8D CLOSE · WP803</small></h1>
    <p v-if="error" class="feedback error" role="alert">{{ error }}</p><p v-else class="feedback" role="status">{{ message }}</p>
    <fieldset><legend>Query</legend><div class="query-grid">
      <label class="query-item"><input v-model="checks.qrsNo" type="checkbox" :disabled="!dev">QRSNo <input v-model="filters.qrsNo" class="yellow" :disabled="!dev || !checks.qrsNo" @keydown.enter.prevent="query"><span>%</span></label>
      <label class="query-item"><input v-model="checks.factoryNo" type="checkbox" :disabled="!dev">FactoryNo <input v-model="filters.factoryNo" class="yellow" :disabled="!dev || !checks.factoryNo" @keydown.enter.prevent="query"></label>
      <label class="query-item"><input v-model="checks.projectDepartment" type="checkbox" :disabled="!dev">ProjectDepartment <input v-model="filters.projectDepartment" class="yellow" :disabled="!dev || !checks.projectDepartment" @keydown.enter.prevent="query"></label>
      <label class="query-item"><input v-model="checks.createDate" type="checkbox" :disabled="!dev">Create Date <input v-model="filters.createFrom" type="date" class="yellow" :disabled="!dev || !checks.createDate"> To <input v-model="filters.createTo" type="date" class="yellow" :disabled="!dev || !checks.createDate"></label>
      <label class="query-item"><input v-model="checks.closeDate" type="checkbox" :disabled="!dev">CloseDate <input v-model="filters.closeFrom" type="date" class="yellow" :disabled="!dev || !checks.closeDate"> To <input v-model="filters.closeTo" type="date" class="yellow" :disabled="!dev || !checks.closeDate"></label>
      <button type="button" class="mes-button" :disabled="!dev" @click="query">Query (&Q)</button>
    </div></fieldset>
    <div class="state-row"><label v-for="item in [[0,'UNFROZEN'],[1,'PENDING'],[2,'ACTIVE'],[-1,'UNUSED']]" :key="item[0]"><input v-model.number="issueState" type="radio" :value="item[0]" name="wp803-state" @change="query" :disabled="!dev">{{ item[1] }}</label><label v-if="dev" class="job-mode">DEV Issue Job <select v-model="issueJobMode" :disabled="!dev"><option value="direct">無 Job／無 Detail：直接核准</option><option value="workflow">有 Job／Detail：送簽</option></select></label></div>
    <fieldset class="result"><legend>8D Close Data</legend><div class="result-content"><div class="table-wrap"><table><thead><tr><th aria-label="選取"></th><th v-for="column in gridColumns" :key="column[0]">{{ column[1] }}</th></tr></thead><tbody><tr v-for="row in results" :key="row.qrsNo" :class="{ selected: activeNo === row.qrsNo }" @click="choose(row)" @dblclick="choose(row); openEditor()"><td><input type="checkbox" :checked="selectedNos.includes(row.qrsNo)" @click.stop="toggleSelected(row.qrsNo)" :disabled="!dev"></td><td v-for="column in gridColumns" :key="column[0]">{{ row[column[0]] }}</td></tr><tr v-if="!results.length"><td :colspan="gridColumns.length + 1" class="empty">{{ !dev ? unavailableText : searched ? '沒有符合資料。若尚未建立結案申請，請先到 WP802 完成 D8 Summary（非自動結案）。' : '請按 Query 查詢' }}</td></tr></tbody></table></div><aside><button type="button" class="mes-button" :disabled="!canEdit" @click="openEditor">8DClose</button><button type="button" class="mes-button" :disabled="!dev || !selectedNos.length || issueState !== 0" @click="requestApprove">Approve</button><button type="button" class="mes-button" :disabled="!dev || !active" @click="openDetail">8DQuery (Q)</button><button v-if="dev && issueState === 1" type="button" class="mes-button" :disabled="!active || !pendingRows.length" @click="completePending">DEV 模擬簽核通過</button><button type="button" class="mes-button close" @click="emit('close')">Close (&X)</button></aside></div><div class="status">UserNo: {{ currentUserNo || (dev ? 'DEV' : '—') }}　資料總筆數 {{ results.length }}</div></fieldset>
    <div v-if="dev && editorOpen && active" class="overlay" @click.self="editorOpen = false"><div class="modal" role="dialog" aria-modal="true"><header>8D Close Define · {{ active.qrsNo }}</header><fieldset><legend>8D Close Define</legend><div class="basis-grid"><label>QRSType<input value="8DFACTORY" readonly :disabled="!dev"></label><label>QRSNo<input :value="active.qrsNo" readonly :disabled="!dev"></label><label>QRSSummary<input :value="activeBasis?.qrsSummary ?? ''" readonly :disabled="!dev"></label><label>FactoryNo<input :value="activeBasis?.factoryNo ?? ''" readonly :disabled="!dev"></label><label>ProductNo<input :value="activeBasis?.productNo ?? ''" readonly :disabled="!dev"></label><label>ProjectDepartment<input :value="activeBasis?.projectDepartment ?? ''" readonly :disabled="!dev"></label><label>ProjectManager<input :value="activeBasis?.projectManager ?? ''" readonly :disabled="!dev"></label><label>PlanCloseDate<input :value="activeBasis?.planCloseDate ?? ''" readonly :disabled="!dev"></label><label>CloseDepartment<div class="picker"><input v-model="form.releaseDepartment" class="yellow" @input="form.releaser = ''; form.releaserName = ''" :disabled="!dev"><button type="button" class="mes-button" @click="openLookup('department')">...</button></div></label><label>CloseDepartmentName<input :value="teams.find(item => item.departmentNo === form.releaseDepartment)?.departmentName || ''" readonly :disabled="!dev"></label><label>CloseUserNo<div class="picker"><input v-model="form.releaser" class="yellow" @input="form.releaserName = members.find(item => item.memberNo === form.releaser)?.memberName || ''" :disabled="!dev"><button type="button" class="mes-button" :disabled="!form.releaseDepartment" @click="openLookup('user')">...</button></div></label><label>CloseUserName<input v-model="form.releaserName" class="yellow" readonly :disabled="!dev"></label><label>CloseDate<input v-model="form.releaseDate" type="date" class="yellow" :disabled="!dev"></label><label>Creator<input :value="active.creator || currentUserNo || (dev ? 'DEV' : '—')" readonly :disabled="!dev"></label><label class="wide">CloseDescription<textarea v-model="form.releaseMemo" class="yellow" rows="3" :disabled="!dev"></textarea></label><label class="wide">Comment<textarea v-model="form.description" rows="3" :disabled="!dev"></textarea></label><label>CreateDate<input :value="active.createDate" readonly :disabled="!dev"></label></div></fieldset><div class="buttons"><button type="button" class="mes-button primary" @click="saveEditor">OK</button><button type="button" class="mes-button" @click="editorOpen = false">Close</button></div></div></div>
    <div v-if="dev && lookup" class="overlay"><div class="modal small" role="dialog" aria-modal="true"><header>{{ lookup === 'department' ? 'CloseDepartment' : 'CloseUserNo' }} 查找</header><div class="lookup-list"><button v-for="no in lookup === 'department' ? departments : []" :key="no" type="button" class="mes-button" @click="chooseDepartment(no)">{{ no }}</button><button v-for="member in lookup === 'user' ? members : []" :key="member.memberNo" type="button" class="mes-button" @click="chooseMember(member)">{{ member.memberNo }}　{{ member.memberName }}</button><p v-if="lookup === 'department' && !departments.length || lookup === 'user' && !members.length">D1 TeamMember 尚無可選資料。</p></div><div class="buttons"><button type="button" class="mes-button" @click="lookup = ''">Close</button></div></div></div>
    <div v-if="dev && approveConfirm" class="overlay"><div class="modal small" role="alertdialog" aria-modal="true"><header>Approve</header><p>確定核准選取的 {{ selectedNos.length }} 筆結案申請？</p><div class="buttons"><button type="button" class="mes-button primary" @click="approve">確定</button><button type="button" class="mes-button" @click="approveConfirm = false">取消</button></div></div></div>
  </section>
</template>

<style scoped>
.wp803{height:100%;min-height:0;box-sizing:border-box;display:flex;flex-direction:column;gap:8px;padding:9px;background:#f7f5ee;color:#333;font:13px Arial,'Microsoft JhengHei',sans-serif}.dev-note{border:1px solid #d8c994;background:#fff8dc;padding:6px}.wp803 h1{font-size:18px;margin:0}.wp803 h1 small{font-size:12px;font-weight:400;color:#777}.feedback{min-height:18px;margin:0}.feedback.error{color:#a22;background:#fff0ef;padding:4px}.wp803 fieldset{min-width:0;margin:0;padding:8px;border:1px solid #aaa;background:#fff}.wp803 legend{padding:0 5px}.query-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:6px}.query-item{display:flex;align-items:center;gap:7px;min-height:28px}.query-item>input.yellow{flex:1;min-width:0}.query-item input[type=date]{width:140px;flex:none}.yellow{box-sizing:border-box;min-height:26px;padding:3px 5px;border:1px solid #888;background:#fff0b3!important;color:#333;font:inherit}.mes-button{min-height:29px;padding:3px 9px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#f1f1f1);color:#333;font:inherit;cursor:pointer}.mes-button:disabled{color:#999;cursor:default}.mes-button.primary{background:#f8bc48}.state-row{display:flex;align-items:center;gap:14px;flex-wrap:wrap;padding:4px}.state-row label{display:flex;align-items:center;gap:5px}.state-row .job-mode{margin-left:auto}.state-row select{max-width:220px}.result{flex:1;min-height:230px;display:flex;flex-direction:column}.result-content{display:flex;gap:8px;min-height:0;flex:1}.table-wrap{flex:1;min-width:0;overflow:auto;border:1px solid #aaa}.table-wrap table{border-collapse:collapse;white-space:nowrap}.table-wrap th,.table-wrap td{padding:5px 7px;border:1px solid #d3d3d3;text-align:left}.table-wrap th{position:sticky;top:0;background:#ead591}.table-wrap td{background:#ffe89c}.table-wrap tr.selected td{background:#f4ca68}.table-wrap tr{cursor:pointer}.table-wrap .empty{text-align:center;background:#fff}.result-content aside{min-width:145px;display:flex;flex-direction:column;gap:6px}.result-content aside .mes-button{width:145px}.result-content aside .close{margin-top:auto}.status{padding-top:5px;color:#666}.overlay{position:fixed;inset:0;z-index:2100;display:grid;place-items:center;background:#0006}.modal{width:min(800px,95vw);max-height:88vh;overflow:auto;background:#f7f5ee;border:1px solid #888;padding:14px;box-shadow:0 10px 30px #0004}.modal.small{width:min(430px,90vw)}.modal header{font-size:16px;font-weight:600;margin-bottom:10px}.basis-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:8px 15px}.basis-grid label{display:grid;grid-template-columns:145px minmax(0,1fr);align-items:center;gap:5px}.basis-grid label.wide{grid-column:1/-1}.basis-grid input,.basis-grid textarea{width:100%;box-sizing:border-box}.picker{display:flex;gap:4px}.picker input{min-width:0}.picker .mes-button{width:32px;padding:0}.buttons{display:flex;justify-content:flex-end;gap:7px;padding-top:8px}.lookup-list{display:flex;flex-direction:column;align-items:stretch;gap:4px;max-height:45vh;overflow:auto}@media(max-width:800px){.query-grid,.basis-grid{grid-template-columns:1fr}.basis-grid label.wide{grid-column:auto}}
</style>
