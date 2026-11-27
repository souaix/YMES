<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { apiErrorMessage } from '../utils/apiError'

interface ReleaseRow {
  qrsNo: string
  releaseMemo: string | null
  releaseDepartment: string | null
  releaser: string | null
  releaserName: string | null
  releaseDate: string | null
  description: string | null
  issueState: number
  creator: string | null
  createDate: string | null
}
interface BasisDetail {
  qrsClass: number
  qrsSummary: string
  factoryNo: string
  productNo: string | null
  productVersion: string | null
  projectDepartment: string | null
  projectManager: string | null
  projectManagerName: string | null
  planCloseDate: string | null
  curStage: string
  closeDate: string | null
}
interface CloseDetail { release: ReleaseRow; basis: BasisDetail }
interface Department { departmentNo: string; departmentName: string | null }
interface Member { departmentNo: string; departmentName: string | null; memberNo: string; memberName: string }
interface TeamResult { qrsNo: string; departments: Department[]; members: Member[] }
interface PageResult { totalCount: number; page: number; pageSize: number; pageCount: number; items: ReleaseRow[] }
class ApiRequestError extends Error { constructor(message: string, readonly status: number) { super(message) } }

const props = defineProps<{ apiBaseUrl: string; currentUserNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const filters = reactive({ qrsNo: '', factoryNo: '', projectDepartment: '', createFrom: '', createTo: '', closeFrom: '', closeTo: '' })
const checks = reactive({ qrsNo: false, factoryNo: false, projectDepartment: false, createDate: false, closeDate: false })
const issueState = ref(0)
const rows = ref<ReleaseRow[]>([])
const currentPage = ref(1)
const pageCount = ref(0)
const totalCount = ref(0)
const activeNo = ref('')
const selectedNos = ref<string[]>([])
const queried = ref(false)
const loading = ref(false)
const saving = ref(false)
const editorOpen = ref(false)
const detailLoading = ref(false)
const detail = ref<CloseDetail | null>(null)
const lookup = ref<'department' | 'user' | ''>('')
const lookupLoading = ref(false)
const lookupError = ref('')
const team = ref<TeamResult | null>(null)
const form = reactive({ releaseDepartment: '', releaser: '', releaserName: '', releaseMemo: '', releaseDate: '', description: '' })
const error = ref('')
const message = ref('請設定查詢條件並按 Query。')
const active = computed(() => rows.value.find(row => row.qrsNo === activeNo.value) || null)
const departments = computed(() => team.value?.departments ?? [])
const members = computed(() => (team.value?.members ?? []).filter(member => member.departmentNo === form.releaseDepartment))
const lookupOptions = computed(() => lookup.value === 'department'
  ? departments.value.map(item => ({ key: item.departmentNo, label: `${item.departmentNo}　${item.departmentName || ''}`, department: item as Department | null, member: null as Member | null }))
  : members.value.map(item => ({ key: item.memberNo, label: `${item.memberNo}　${item.memberName}`, department: null as Department | null, member: item as Member | null })))
const departmentName = computed(() => departments.value.find(item => item.departmentNo === form.releaseDepartment)?.departmentName || '')
const canEdit = computed(() => active.value?.issueState === 0 && !loading.value && !saving.value && !detailLoading.value)
const hasMore = computed(() => currentPage.value < pageCount.value)
const statusMessage = computed(() => loading.value ? '查詢中…' : queried.value ? `第 ${currentPage.value} 頁，共 ${totalCount.value.toLocaleString()} 筆${hasMore.value ? '，可載入下一頁。' : '。'}` : message.value)

function apiUrl(path: string): URL {
  const base = props.apiBaseUrl.trim().replace(/\/+$/, '')
  if (!base) throw new Error('目前環境尚未設定 YMES API 位址，無法讀取正式資料。')
  const root = new URL(`${base}/`, window.location.origin)
  const local = root.hostname === 'localhost' || root.hostname === '127.0.0.1'
  if (root.protocol !== 'https:' && !local) throw new Error('正式 API 必須使用 HTTPS。')
  return new URL(path.replace(/^\/+/, ''), root)
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(apiUrl(path), { ...init, credentials: 'include', cache: 'no-store', headers: { ...(init?.body ? { 'Content-Type': 'application/json' } : {}), ...init?.headers } })
  if (!response.ok) throw new ApiRequestError(await apiErrorMessage(response), response.status)
  if (response.status === 204) return undefined as T
  return await response.json() as T
}

function validateDates(): string {
  if (checks.createDate && (!filters.createFrom || !filters.createTo || filters.createFrom > filters.createTo)) return 'Create Date 起迄需成對且起日不可晚於迄日。'
  if (checks.closeDate && (!filters.closeFrom || !filters.closeTo || filters.closeFrom > filters.closeTo)) return 'CloseDate 起迄需成對且起日不可晚於迄日。'
  const ranges = [
    ...(checks.createDate ? [[filters.createFrom, filters.createTo]] : []),
    ...(checks.closeDate ? [[filters.closeFrom, filters.closeTo]] : []),
  ]
  for (const [from, to] of ranges) {
    if (from && to && (Date.parse(`${to}T00:00:00Z`) - Date.parse(`${from}T00:00:00Z`)) / 86_400_000 > 366) return '日期區間最多 366 天。'
  }
  return ''
}

async function query(page = 1): Promise<void> {
  if (loading.value || saving.value) return
  error.value = ''
  const dateError = validateDates()
  if (dateError) { error.value = dateError; return }
  loading.value = true
  try {
    const url = apiUrl('api/wip/8d-close')
    url.searchParams.set('issueState', String(issueState.value))
    url.searchParams.set('page', String(page))
    url.searchParams.set('pageSize', '50')
    if (checks.qrsNo && filters.qrsNo.trim()) url.searchParams.set('qrsNo', filters.qrsNo.trim())
    if (checks.factoryNo && filters.factoryNo.trim()) url.searchParams.set('factoryNo', filters.factoryNo.trim())
    if (checks.projectDepartment && filters.projectDepartment.trim()) url.searchParams.set('projectDepartment', filters.projectDepartment.trim())
    if (checks.createDate) { url.searchParams.set('createFrom', filters.createFrom); url.searchParams.set('createTo', filters.createTo) }
    if (checks.closeDate) { url.searchParams.set('releaseFrom', filters.closeFrom); url.searchParams.set('releaseTo', filters.closeTo) }
    const response = await fetch(url, { credentials: 'include', cache: 'no-store' })
    if (!response.ok) throw new ApiRequestError(await apiErrorMessage(response), response.status)
    const result = await response.json() as PageResult
    rows.value = Array.isArray(result.items) ? result.items : []
    currentPage.value = result.page
    pageCount.value = result.pageCount
    totalCount.value = result.totalCount
    activeNo.value = ''
    selectedNos.value = []
    detail.value = null
    queried.value = true
    message.value = rows.value.length ? `查得 ${rows.value.length} 筆結案申請。` : '查無資料；若尚未建立結案申請，請先在 WP802 完成 D8 Summary。'
  } catch (cause) {
    rows.value = []
    pageCount.value = 0
    totalCount.value = 0
    activeNo.value = ''
    selectedNos.value = []
    queried.value = true
    error.value = cause instanceof Error ? cause.message : '8D 結案查詢失敗，請重試。'
  } finally { loading.value = false }
}

function choose(row: ReleaseRow): void { activeNo.value = row.qrsNo; selectedNos.value = [row.qrsNo]; detail.value = null }
function toggleSelected(row: ReleaseRow): void {
  activeNo.value = row.qrsNo
  detail.value = null
  selectedNos.value = selectedNos.value.includes(row.qrsNo) ? selectedNos.value.filter(no => no !== row.qrsNo) : [...selectedNos.value, row.qrsNo]
}
function datePart(value: string | null | undefined): string { return value ? value.slice(0, 10) : '' }
function displayDate(value: string | null | undefined): string { return value ? value.replace('T', ' ').slice(0, 19) : '' }

async function openEditor(): Promise<void> {
  const row = active.value
  if (!row) { error.value = '請先選取一筆結案申請。'; return }
  if (row.issueState !== 0) { error.value = '只有未簽核申請可修改結案資料。'; return }
  detailLoading.value = true
  error.value = ''
  try {
    const loaded = await request<CloseDetail>(`api/wip/8d-close/${encodeURIComponent(row.qrsNo)}`)
    if (activeNo.value !== row.qrsNo) return
    detail.value = loaded
    if (loaded.release.issueState !== 0 || loaded.basis.curStage === 'D9' || loaded.basis.closeDate) throw new Error('結案申請狀態或 8D 主單已變更，請重新查詢。')
    Object.assign(form, {
      releaseDepartment: loaded.release.releaseDepartment || '', releaser: loaded.release.releaser || '',
      releaserName: loaded.release.releaserName || '', releaseMemo: loaded.release.releaseMemo || '',
      releaseDate: datePart(loaded.release.releaseDate) || new Date().toISOString().slice(0, 10), description: loaded.release.description || '',
    })
    team.value = await request<TeamResult>(`api/wip/8d-close/${encodeURIComponent(row.qrsNo)}/team-members`)
    editorOpen.value = true
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '無法讀取結案明細或 D1 團隊。' }
  finally { detailLoading.value = false }
}

async function openLookup(kind: 'department' | 'user'): Promise<void> {
  if (!activeNo.value) return
  if (kind === 'user' && !form.releaseDepartment.trim()) { error.value = '請先指定 CloseDepartment。'; return }
  lookup.value = kind
  lookupLoading.value = true
  lookupError.value = ''
  try {
    const path = `api/wip/8d-close/${encodeURIComponent(activeNo.value)}/team-members${kind === 'user' ? `?departmentNo=${encodeURIComponent(form.releaseDepartment.trim())}` : ''}`
    team.value = await request<TeamResult>(path)
  } catch (cause) { lookupError.value = cause instanceof Error ? cause.message : 'D1 團隊查找失敗。' }
  finally { lookupLoading.value = false }
}

function chooseDepartment(item: Department): void {
  form.releaseDepartment = item.departmentNo
  form.releaser = ''
  form.releaserName = ''
  lookup.value = ''
}
function chooseMember(item: Member): void {
  form.releaser = item.memberNo
  form.releaserName = item.memberName
  lookup.value = ''
}
function departmentChanged(): void { form.releaser = ''; form.releaserName = '' }

async function saveEditor(): Promise<void> {
  if (saving.value || !detail.value || !canEdit.value) return
  for (const [key, value, max] of [
    ['CloseDepartment', form.releaseDepartment, 20], ['CloseUserNo', form.releaser, 10],
    ['CloseUserName', form.releaserName, 50], ['CloseDescription', form.releaseMemo, 500],
  ] as const) {
    if (!value.trim()) { error.value = `${key} 不可空白。`; return }
    if (value.length > max) { error.value = `${key} 不可超過 ${max} 個字元。`; return }
  }
  if (!form.releaseDate || Number.isNaN(Date.parse(form.releaseDate))) { error.value = 'CloseDate 不正確。'; return }
  if (form.description.length > 255) { error.value = 'Comment 不可超過 255 個字元。'; return }
  saving.value = true
  error.value = ''
  const qrsNo = detail.value.release.qrsNo
  try {
    await request<ReleaseRow>(`api/wip/8d-close/${encodeURIComponent(qrsNo)}`, {
      method: 'PUT', body: JSON.stringify({
        expected: detail.value.release,
        value: { releaseMemo: form.releaseMemo, releaseDepartment: form.releaseDepartment.trim(), releaser: form.releaser.trim(),
          releaserName: form.releaserName.trim(), releaseDate: form.releaseDate, description: form.description },
      }),
    })
    editorOpen.value = false
    detail.value = null
    saving.value = false
    await query(currentPage.value)
    message.value = `${qrsNo} 的結案申請資料已保存，尚未簽核。`
  } catch (cause) {
    if (cause instanceof ApiRequestError && cause.status === 409) {
      saving.value = false
      await query(currentPage.value)
      error.value = `${cause.message} 已重新載入清單，請重新選取後編輯。`
    } else error.value = cause instanceof Error ? cause.message : '結案申請更新失敗，請重新查詢確認資料。'
  } finally { saving.value = false }
}

</script>

<template>
  <section class="wp803-live" aria-label="8D 矯正單結案作業" :aria-busy="loading || saving">
    <header class="page-head"><div><p class="eyebrow">WIP · WP803 · PRODUCTION DATA</p><h1>8D 矯正單結案作業</h1><p>查詢正式結案申請並維護簽核前資料。</p></div><button type="button" class="mes-button" @click="emit('close')">Close (&X)</button></header>
    <p v-if="error" class="feedback error" role="alert">{{ error }}</p><p v-else class="feedback" role="status">{{ statusMessage }}</p>
    <fieldset class="query"><legend>Query</legend><div class="query-grid">
      <label><input v-model="checks.qrsNo" type="checkbox" :disabled="loading">QRSNo <input v-model="filters.qrsNo" maxlength="30" :disabled="!checks.qrsNo || loading" @keydown.enter.prevent="query(1)"></label>
      <label><input v-model="checks.factoryNo" type="checkbox" :disabled="loading">FactoryNo <input v-model="filters.factoryNo" maxlength="20" :disabled="!checks.factoryNo || loading" @keydown.enter.prevent="query(1)"></label>
      <label><input v-model="checks.projectDepartment" type="checkbox" :disabled="loading">ProjectDepartment <input v-model="filters.projectDepartment" maxlength="20" :disabled="!checks.projectDepartment || loading" @keydown.enter.prevent="query(1)"></label>
      <label><input v-model="checks.createDate" type="checkbox" :disabled="loading">Create Date <input v-model="filters.createFrom" type="date" :disabled="!checks.createDate || loading"> To <input v-model="filters.createTo" type="date" :disabled="!checks.createDate || loading"></label>
      <label><input v-model="checks.closeDate" type="checkbox" :disabled="loading">CloseDate <input v-model="filters.closeFrom" type="date" :disabled="!checks.closeDate || loading"> To <input v-model="filters.closeTo" type="date" :disabled="!checks.closeDate || loading"></label>
      <button type="button" class="mes-button primary" :disabled="loading || saving" @click="query(1)">{{ loading ? '查詢中…' : 'Query (&Q)' }}</button>
    </div></fieldset>
    <div class="state-row"><label v-for="item in [{value:0,label:'UNFROZEN'},{value:1,label:'PENDING'},{value:2,label:'ACTIVE'},{value:-1,label:'UNUSED'}]" :key="item.value"><input v-model.number="issueState" type="radio" name="wp803-live-state" :value="item.value" :disabled="loading" @change="query(1)">{{ item.label }}</label><span class="approve-note" title="正式簽核端點目前安全回覆 503，待簽核來源交付後再接通。">Approve 暫未開放</span></div>
    <fieldset class="result"><legend>8D Close Data</legend><div class="table-wrap"><table><thead><tr><th>選取</th><th>QRSNo</th><th>ReleaseMemo8D</th><th>ReleaseDepartment</th><th>Releaser8D</th><th>ReleaserName</th><th>ReleaseDate8D</th><th>Description</th><th>Creator</th><th>CreateDate</th></tr></thead><tbody>
      <tr v-for="row in rows" :key="row.qrsNo" :class="{ selected: activeNo === row.qrsNo }" @click="choose(row)" @dblclick="choose(row); openEditor()"><td><input type="checkbox" :checked="selectedNos.includes(row.qrsNo)" @click.stop="toggleSelected(row)"></td><td>{{ row.qrsNo }}</td><td>{{ row.releaseMemo || '' }}</td><td>{{ row.releaseDepartment || '' }}</td><td>{{ row.releaser || '' }}</td><td>{{ row.releaserName || '' }}</td><td>{{ displayDate(row.releaseDate) }}</td><td>{{ row.description || '' }}</td><td>{{ row.creator || '' }}</td><td>{{ displayDate(row.createDate) }}</td></tr>
      <tr v-if="!rows.length"><td colspan="10" class="empty">{{ queried ? (error ? '查詢失敗，請參閱上方訊息。' : '沒有符合資料；若尚未建立結案申請，請先於 WP802 完成 D8 Summary。') : '請按 Query 查詢正式資料。' }}</td></tr>
    </tbody></table></div><footer class="grid-footer"><span>UserNo: {{ currentUserNo || '—' }} · {{ totalCount.toLocaleString() }} 筆</span><nav aria-label="結案申請分頁"><button type="button" class="mes-button" :disabled="loading || currentPage <= 1" @click="query(currentPage - 1)">上一頁</button><span>第 {{ currentPage }} / {{ Math.max(pageCount, 1) }} 頁</span><button type="button" class="mes-button" :disabled="loading || !hasMore" @click="query(currentPage + 1)">下一頁</button></nav></footer></fieldset>
    <aside class="actions"><button type="button" class="mes-button" :disabled="!active || active.issueState !== 0 || loading || saving || detailLoading" @click="openEditor">{{ detailLoading ? '讀取中…' : '8DClose' }}</button><button type="button" class="mes-button" disabled title="正式簽核 API 尚未就緒。">Approve</button><button type="button" class="mes-button" disabled title="WP804 正式唯讀查詢尚未接通。">8DQuery (Q)</button></aside>

    <div v-if="editorOpen && detail" class="overlay" @click.self="editorOpen = false"><div class="modal" role="dialog" aria-modal="true" aria-label="8D Close Define"><header>8D Close Define · {{ detail.release.qrsNo }}</header><div class="basis-grid">
      <label>QRSType<input value="8DFACTORY" readonly></label><label>QRSNo<input :value="detail.release.qrsNo" readonly></label>
      <label>QRSSummary<input :value="detail.basis.qrsSummary" readonly></label><label>FactoryNo<input :value="detail.basis.factoryNo" readonly></label>
      <label>ProductNo<input :value="detail.basis.productNo || ''" readonly></label><label>ProjectDepartment<input :value="detail.basis.projectDepartment || ''" readonly></label>
      <label>ProjectManager<input :value="detail.basis.projectManagerName || detail.basis.projectManager || ''" readonly></label><label>PlanCloseDate<input :value="displayDate(detail.basis.planCloseDate)" readonly></label>
      <label>CloseDepartment<div class="picker"><input v-model="form.releaseDepartment" maxlength="20" @input="departmentChanged"><button type="button" class="mes-button" :disabled="lookupLoading" @click="openLookup('department')">...</button></div></label>
      <label>CloseDepartmentName<input :value="departmentName" readonly></label>
      <label>CloseUserNo<div class="picker"><input v-model="form.releaser" maxlength="10" @input="form.releaserName = ''"><button type="button" class="mes-button" :disabled="!form.releaseDepartment || lookupLoading" @click="openLookup('user')">...</button></div></label>
      <label>CloseUserName<input v-model="form.releaserName" maxlength="50"></label>
      <label>CloseDate<input v-model="form.releaseDate" type="date"></label><label>Creator<input :value="detail.release.creator || currentUserNo || ''" readonly></label>
      <label class="wide">CloseDescription<textarea v-model="form.releaseMemo" maxlength="500" rows="3"></textarea></label>
      <label class="wide">Comment<textarea v-model="form.description" maxlength="255" rows="3"></textarea></label>
      <label>CreateDate<input :value="displayDate(detail.release.createDate)" readonly></label>
    </div><p v-if="detail.basis.curStage === 'D9' || detail.basis.closeDate" class="feedback error">8D 主單已結案，不可修改。</p><div class="buttons"><button type="button" class="mes-button primary" :disabled="saving || detail.basis.curStage === 'D9' || !!detail.basis.closeDate" @click="saveEditor">{{ saving ? '儲存中…' : 'OK' }}</button><button type="button" class="mes-button" :disabled="saving" @click="editorOpen = false">Close</button></div></div></div>
    <div v-if="lookup" class="overlay"><div class="modal small" role="dialog" aria-modal="true"><header>{{ lookup === 'department' ? 'CloseDepartment' : 'CloseUserNo' }} 查找</header><p v-if="lookupError" class="feedback error" role="alert">{{ lookupError }}</p><p v-else-if="lookupLoading" class="feedback" role="status">查詢 D1 團隊中…</p><div v-else class="lookup-list"><button v-for="item in lookupOptions" :key="item.key" type="button" class="mes-button" @click="item.department ? chooseDepartment(item.department) : item.member && chooseMember(item.member)">{{ item.label }}</button><p v-if="!lookupOptions.length">D1 TeamMember 沒有可選資料。</p></div><div class="buttons"><button type="button" class="mes-button" @click="lookup = ''">Close</button></div></div></div>
  </section>
</template>

<style scoped>
.wp803-live{height:100%;min-height:0;box-sizing:border-box;display:flex;flex-direction:column;gap:8px;padding:10px;background:#f7f5ee;color:#333;font:13px Arial,'Microsoft JhengHei',sans-serif}.page-head{display:flex;justify-content:space-between;align-items:flex-start}.eyebrow{margin:0 0 3px;color:#6e6859;font-size:10px;font-weight:700;letter-spacing:.08em}.page-head h1{margin:0;font-size:19px}.page-head p:not(.eyebrow){margin:3px 0;color:#666}.feedback{min-height:18px;margin:0;color:#555}.feedback.error{padding:5px;background:#fff0ef;color:#922}.query,.result{min-width:0;margin:0;padding:8px;border:1px solid #aaa;background:#fff}.query legend,.result legend{padding:0 5px}.query-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:7px}.query-grid label{display:flex;align-items:center;gap:6px;min-height:28px}.query-grid label input:not([type=checkbox]){min-width:0;flex:1;min-height:25px;box-sizing:border-box}.query-grid label input[type=date]{flex:none;width:145px}.mes-button{min-height:28px;padding:3px 9px;border:1px solid #aaa;border-radius:3px;background:linear-gradient(#fff,#eee);color:#333;font:inherit;cursor:pointer}.mes-button:disabled{color:#999;cursor:default}.mes-button.primary{background:#f8bc48}.state-row{display:flex;align-items:center;gap:15px;flex-wrap:wrap;padding:2px 4px}.state-row label{display:flex;align-items:center;gap:4px}.approve-note{margin-left:auto;color:#7c5e28}.result{flex:1;min-height:170px;display:flex;flex-direction:column}.table-wrap{flex:1;min-height:0;overflow:auto;border:1px solid #aaa}.table-wrap table{width:100%;border-collapse:collapse;white-space:nowrap}.table-wrap th,.table-wrap td{padding:5px 7px;border:1px solid #d1d1d1;text-align:left}.table-wrap th{position:sticky;top:0;z-index:1;background:#ead591}.table-wrap tr{cursor:pointer}.table-wrap tr.selected td{background:#f4ca68}.table-wrap .empty{text-align:center;background:#fff;cursor:default}.grid-footer{display:flex;justify-content:space-between;align-items:center;gap:8px;padding-top:6px}.grid-footer nav{display:flex;align-items:center;gap:8px}.actions{display:flex;justify-content:flex-end;gap:7px}.overlay{position:fixed;inset:0;z-index:2100;display:grid;place-items:center;background:#0006}.modal{width:min(820px,95vw);max-height:88vh;overflow:auto;box-sizing:border-box;padding:14px;background:#f7f5ee;border:1px solid #888;box-shadow:0 10px 30px #0004}.modal.small{width:min(440px,90vw)}.modal header{margin-bottom:10px;font-size:16px;font-weight:600}.basis-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:8px 14px}.basis-grid label{display:grid;grid-template-columns:150px minmax(0,1fr);align-items:center;gap:5px}.basis-grid label.wide{grid-column:1/-1}.basis-grid input,.basis-grid textarea{width:100%;box-sizing:border-box;min-height:26px;padding:3px 5px;font:inherit}.basis-grid input[readonly]{background:#eee}.basis-grid textarea{resize:vertical}.picker{display:flex;gap:4px}.picker input{min-width:0}.picker .mes-button{width:34px;padding:0}.buttons{display:flex;justify-content:flex-end;gap:7px;padding-top:10px}.lookup-list{display:flex;flex-direction:column;align-items:stretch;gap:4px;max-height:48vh;overflow:auto}@media(max-width:800px){.query-grid,.basis-grid{grid-template-columns:1fr}.basis-grid label.wide{grid-column:auto}.grid-footer{align-items:flex-start;flex-direction:column}}
</style>
