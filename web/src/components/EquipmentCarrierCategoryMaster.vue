<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from 'vue'

type EditMode = 'new' | 'edit' | 'copy' | null
type IssueState = -1 | 0 | 1 | 2
interface CarrierCategory {
  carrierCategory: string
  description: string | null
  issueState: IssueState
  dataStamp: number
  creator: string | null
  createDate: string | null
  reviser: string | null
  reviseDate: string | null
}
interface CategoryForm { carrierCategory: string; description: string }
interface AuditFields { creator: string; createDate: string; reviser: string; reviseDate: string }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const usesDevelopmentMock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes-dev-mock:equipment-carrier-categories:v1'
const issueStates: { value: IssueState; label: string }[] = [
  { value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' },
]
const seedRows: CarrierCategory[] = [
  { carrierCategory: 'DEV-CARRIER-01', description: 'DEV 假資料：一般載具', issueState: 0, dataStamp: 0, creator: 'DEV001', createDate: '2026-11-01T08:00:00+08:00', reviser: null, reviseDate: null },
  { carrierCategory: 'DEV-CARRIER-02', description: 'DEV 假資料：簽核中載具分類', issueState: 1, dataStamp: 1, creator: 'DEV001', createDate: '2026-11-02T08:00:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-03T09:30:00+08:00' },
  { carrierCategory: 'DEV-CARRIER-03', description: 'DEV 假資料：已簽核載具分類', issueState: 2, dataStamp: 1, creator: 'DEV001', createDate: '2026-11-04T08:00:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-05T09:30:00+08:00' },
  { carrierCategory: 'DEV-CARRIER-04', description: 'DEV 假資料：停用載具分類', issueState: -1, dataStamp: 2, creator: 'DEV001', createDate: '2026-11-06T08:00:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-07T09:30:00+08:00' },
]

const rows = ref<CarrierCategory[]>([])
const issueState = ref<IssueState>(0)
const queryText = ref('')
const appliedQuery = ref('')
const currentNo = ref('')
const selectedNos = ref<string[]>([])
const loading = ref(false)
const busy = ref(false)
const error = ref('')
const notice = ref('')
const mode = ref<EditMode>(null)
const sourceNo = ref('')
const form = ref<CategoryForm>(emptyForm())
const formDataStamp = ref(0)
const audit = ref<AuditFields>({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const formDirty = ref(false)
const categoryInput = ref<HTMLInputElement | null>(null)
const loadRequestId = ref(0)

const visibleRows = computed(() => rows.value
  .filter((row) => row.issueState === issueState.value)
  .filter((row) => !appliedQuery.value || row.carrierCategory.toLocaleLowerCase().includes(appliedQuery.value.toLocaleLowerCase()))
  .slice().sort((a, b) => a.carrierCategory.localeCompare(b.carrierCategory)))
const current = computed(() => visibleRows.value.find((row) => row.carrierCategory === currentNo.value) ?? null)
const selectedRows = computed(() => visibleRows.value.filter((row) => selectedNos.value.includes(row.carrierCategory)))
const formTitle = computed(() => mode.value === 'edit' ? '載具分類資料定義(EDIT)' : mode.value === 'copy' ? '載具分類資料定義(COPY)' : '載具分類資料定義(ADD)')

function emptyForm(): CategoryForm { return { carrierCategory: '', description: '' } }
function userNo(): string { return props.currentUserNo?.trim() || (usesDevelopmentMock.value ? 'DEV001' : '') }
function readRows(): CarrierCategory[] {
  try { const raw = localStorage.getItem(storageKey); return raw ? JSON.parse(raw) as CarrierCategory[] : structuredClone(seedRows) }
  catch { return structuredClone(seedRows) }
}
function writeRows(value: CarrierCategory[]): void { localStorage.setItem(storageKey, JSON.stringify(value)) }
function stateName(value: number): string { return issueStates.find((state) => state.value === value)?.label ?? String(value) }
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
    throw new Error(problem?.detail || problem?.message || problem?.title || '載具分類 API 回應錯誤 (' + response.status + ')。')
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function setQuery(): void {
  appliedQuery.value = queryText.value.trim()
  selectedNos.value = []
  void loadRows()
}
function setIssueState(value: IssueState): void {
  issueState.value = value
  selectedNos.value = []
  void loadRows()
}
function selectRow(no: string): void { currentNo.value = no }
function toggleSelected(no: string): void {
  selectedNos.value = selectedNos.value.includes(no) ? selectedNos.value.filter((value) => value !== no) : [...selectedNos.value, no]
  selectRow(no)
}
function markDirty(): void { formDirty.value = true }
function focusDescription(event: KeyboardEvent): void {
  if (mode.value === 'edit') return
  const target = event.target
  if (target instanceof HTMLInputElement) target.form?.querySelector('textarea')?.focus()
}
function closeForm(): void {
  if (busy.value) return
  mode.value = null
  formDirty.value = false
}
async function openForm(nextMode: Exclude<EditMode, null>): Promise<void> {
  const row = current.value
  if (nextMode !== 'new' && !row) { error.value = '請先選取一筆載具分類資料。'; return }
  if (nextMode === 'edit' && row?.issueState !== 0) { error.value = '只有未簽核的載具分類可以修改。'; return }
  mode.value = nextMode
  error.value = ''
  notice.value = ''
  formDirty.value = false
  sourceNo.value = nextMode === 'edit' ? row?.carrierCategory ?? '' : ''
  formDataStamp.value = row?.dataStamp ?? 0
  const now = new Date().toISOString()
  if (nextMode === 'new') {
    form.value = emptyForm()
    audit.value = { creator: userNo(), createDate: now, reviser: '', reviseDate: '' }
  } else if (row) {
    form.value = { carrierCategory: nextMode === 'edit' ? row.carrierCategory : '', description: row.description || '' }
    audit.value = nextMode === 'edit'
      ? { creator: row.creator || '', createDate: row.createDate || '', reviser: userNo(), reviseDate: now }
      : { creator: userNo(), createDate: now, reviser: '', reviseDate: '' }
  }
  await nextTick()
  if (nextMode !== 'edit') categoryInput.value?.focus()
}
function normalizeCategory(): void { form.value.carrierCategory = form.value.carrierCategory.trim().toUpperCase() }
function validateForm(): string {
  normalizeCategory()
  if (!form.value.carrierCategory) return 'Carrier Category 為必填。'
  if (form.value.carrierCategory.length > 50) return 'Carrier Category 不可超過 50 個字元。'
  if (form.value.description.length > 255) return 'Description 不可超過資料庫欄位長度 255。'
  return ''
}
async function saveForm(keepOpen: boolean): Promise<void> {
  if (!mode.value || busy.value) return
  const problem = validateForm()
  if (problem) { error.value = problem; return }
  const saveMode = mode.value
  const category = form.value.carrierCategory
  const description = form.value.description.trim() || null
  const now = new Date().toISOString()
  if (saveMode === 'edit') audit.value = { ...audit.value, reviser: userNo(), reviseDate: now }
  busy.value = true
  error.value = ''
  notice.value = ''
  try {
    let nextDataStamp = formDataStamp.value
    if (usesDevelopmentMock.value) {
      const allRows = readRows()
      if (saveMode === 'edit') {
        const index = allRows.findIndex((row) => row.carrierCategory === sourceNo.value)
        if (index < 0) throw new Error('載具分類資料已不存在，請重新載入。')
        if (allRows[index].issueState !== 0) throw new Error('只有未簽核的載具分類可以修改。')
        if (allRows[index].dataStamp !== formDataStamp.value) throw new Error('資料已被其他使用者修改，請重新載入。')
        nextDataStamp = allRows[index].dataStamp + 1
        allRows[index] = { ...allRows[index], description, dataStamp: nextDataStamp, reviser: audit.value.reviser, reviseDate: audit.value.reviseDate }
      } else {
        if (allRows.some((row) => row.carrierCategory.toLocaleUpperCase() === category)) throw new Error('Carrier Category 已存在。')
        allRows.push({ carrierCategory: category, description, issueState: 0, dataStamp: 0, creator: audit.value.creator, createDate: audit.value.createDate, reviser: null, reviseDate: null })
        nextDataStamp = 0
      }
      writeRows(allRows)
    } else {
      const body = saveMode === 'edit'
        ? { description, issueState: 0, dataStamp: formDataStamp.value }
        : { carrierCategory: category, description }
      const result = await request<{ dataStamp?: number }>(saveMode === 'edit' ? 'api/equipment/carrier-categories/' + encodeURIComponent(sourceNo.value) : 'api/equipment/carrier-categories', {
        method: saveMode === 'edit' ? 'PUT' : 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      })
      if (result?.dataStamp !== undefined) nextDataStamp = result.dataStamp
      else if (saveMode === 'edit') nextDataStamp += 1
    }
    currentNo.value = category
    await loadRows()
    const saved = rows.value.find((row) => row.carrierCategory === category)
    if (saved) audit.value = { creator: saved.creator || '', createDate: saved.createDate || '', reviser: saved.reviser || '', reviseDate: saved.reviseDate || '' }
    formDataStamp.value = nextDataStamp
    formDirty.value = false
    notice.value = saveMode === 'edit' ? '載具分類資料已修改。' : saveMode === 'copy' ? '載具分類資料已複製。' : '載具分類資料已新增。'
    if (!keepOpen) closeForm()
  } catch (e) { error.value = e instanceof Error ? e.message : '載具分類資料儲存失敗。' }
  finally { busy.value = false }
}
async function deleteSelected(): Promise<void> {
  if (busy.value) return
  const targetsByNo = new Set(selectedNos.value)
  if (current.value) targetsByNo.add(current.value.carrierCategory)
  const targets = visibleRows.value.filter((row) => targetsByNo.has(row.carrierCategory))
  if (!targets.length) { error.value = '請先選取要刪除的載具分類資料。'; return }
  if (!window.confirm('確定刪除／停用選取的 ' + targets.length + ' 筆載具分類資料？')) return
  busy.value = true
  error.value = ''
  notice.value = ''
  let changed = 0
  let failure = ''
  try {
    for (const row of targets) {
      if (![0, 2, -1].includes(row.issueState)) throw new Error('簽核中的載具分類不可刪除。')
      if (usesDevelopmentMock.value) {
        const allRows = readRows()
        if (row.issueState === 2) {
          const index = allRows.findIndex((item) => item.carrierCategory === row.carrierCategory)
          if (index >= 0) allRows[index] = { ...allRows[index], issueState: -1, dataStamp: allRows[index].dataStamp + 1, reviser: userNo(), reviseDate: new Date().toISOString() }
        } else {
          const index = allRows.findIndex((item) => item.carrierCategory === row.carrierCategory)
          if (index >= 0) allRows.splice(index, 1)
        }
        writeRows(allRows)
      } else {
        const query = new URLSearchParams({ issueState: String(row.issueState), dataStamp: String(row.dataStamp) })
        await request<void>('api/equipment/carrier-categories/' + encodeURIComponent(row.carrierCategory) + '?' + query.toString(), { method: 'DELETE' })
      }
      changed += 1
    }
    selectedNos.value = []
  } catch (e) { failure = e instanceof Error ? e.message : '載具分類資料刪除失敗。' }
  finally { busy.value = false }
  if (changed) await loadRows()
  if (failure) error.value = changed ? failure + ' 已完成 ' + changed + ' 筆。' : failure
  else if (changed) notice.value = '已完成 ' + changed + ' 筆刪除／停用。'
}
async function approveSelected(): Promise<void> {
  if (busy.value) return
  const targetsByNo = new Set(selectedNos.value)
  if (current.value) targetsByNo.add(current.value.carrierCategory)
  const targets = visibleRows.value.filter((row) => targetsByNo.has(row.carrierCategory))
  if (!targets.length) { error.value = '請先選取要核准的載具分類資料。'; return }
  if (!window.confirm('確定核准選取的 ' + targets.length + ' 筆載具分類資料？')) return
  busy.value = true
  error.value = ''
  notice.value = ''
  let changed = 0
  let failure = ''
  try {
    for (const row of targets) {
      if (row.issueState !== 0) throw new Error('只有未簽核的載具分類可以核准。')
      if (usesDevelopmentMock.value) {
        const allRows = readRows()
        const index = allRows.findIndex((item) => item.carrierCategory === row.carrierCategory)
        if (index < 0) throw new Error('載具分類資料已不存在，請重新載入。')
        allRows[index] = { ...allRows[index], issueState: 2, dataStamp: allRows[index].dataStamp + 1, reviser: userNo(), reviseDate: new Date().toISOString() }
        writeRows(allRows)
      } else {
        await request<void>('api/equipment/carrier-categories/' + encodeURIComponent(row.carrierCategory) + '/approve', {
          method: 'POST', headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ issueState: row.issueState, dataStamp: row.dataStamp }),
        })
      }
      changed += 1
    }
    selectedNos.value = []
  } catch (e) { failure = e instanceof Error ? e.message : '載具分類核准失敗。' }
  finally { busy.value = false }
  if (changed) await loadRows()
  if (failure) error.value = changed ? failure + ' 已完成 ' + changed + ' 筆。' : failure
  else if (changed) notice.value = usesDevelopmentMock.value ? 'DEV 簡化核准已完成 ' + changed + ' 筆（直接轉為已簽核）。' : '已核准 ' + changed + ' 筆載具分類資料。'
}
async function loadRows(): Promise<void> {
  const requestId = ++loadRequestId.value
  loading.value = true
  error.value = ''
  notice.value = ''
  try {
    let data: CarrierCategory[]
    if (usesDevelopmentMock.value) data = readRows()
    else {
      const query = new URLSearchParams({ issueState: String(issueState.value) })
      if (appliedQuery.value) query.set('carrierCategory', appliedQuery.value)
      data = await request<CarrierCategory[]>('api/equipment/carrier-categories?' + query.toString())
    }
    if (requestId !== loadRequestId.value) return
    rows.value = data
    rows.value = rows.value.slice().sort((a, b) => a.carrierCategory.localeCompare(b.carrierCategory))
    selectedNos.value = selectedNos.value.filter((no) => visibleRows.value.some((row) => row.carrierCategory === no))
    if (!visibleRows.value.some((row) => row.carrierCategory === currentNo.value)) currentNo.value = visibleRows.value[0]?.carrierCategory ?? ''
  } catch (e) {
    if (requestId !== loadRequestId.value) return
    rows.value = []
    currentNo.value = ''
    selectedNos.value = []
    error.value = e instanceof Error ? e.message : '載具分類資料載入失敗。'
  } finally { if (requestId === loadRequestId.value) loading.value = false }
}
function closePage(): void { window.history.pushState({}, '', appBase || '/'); window.dispatchEvent(new PopStateEvent('popstate')) }

onMounted(() => { void loadRows() })
watch(() => [props.developmentPreview, props.apiBaseUrl], () => {
  closeForm()
  selectedNos.value = []
  void loadRows()
})
</script>

<template>
  <section class="carrier-category-page">
    <div v-if="usesDevelopmentMock" class="carrier-category-dev-note">開發模式：目前資料為 DEV 假資料，儲存在此瀏覽器；核准流程已簡化，不會寫入 MES 或正式資料庫。</div>
    <div class="carrier-category-search">
      <div class="carrier-category-states" role="radiogroup" aria-label="簽核狀態">
        <label v-for="state in issueStates" :key="state.value"><input type="radio" name="carrierCategoryIssueState" :checked="issueState===state.value" @change="setIssueState(state.value)"> {{ state.label }}</label>
      </div>
      <label for="carrierCategorySearch">載具分類</label>
      <input id="carrierCategorySearch" v-model="queryText" type="text" @keydown.enter.prevent="setQuery">
      <span class="carrier-category-like">%</span>
      <button type="button" class="carrier-category-primary" :disabled="loading" @click="setQuery">查詢</button>
    </div>
    <p v-if="error && !mode" class="carrier-category-message carrier-category-error" role="alert">{{ error }}</p>
    <p v-else-if="notice && !mode" class="carrier-category-message carrier-category-success" role="status">{{ notice }}</p>
    <div class="carrier-category-main">
      <div class="carrier-category-grid-wrap">
        <table class="carrier-category-grid">
          <thead><tr><th class="carrier-category-check"></th><th>載具分類</th><th>Description</th><th>Creator</th><th>Create Date</th><th>Reviser</th><th>Revise Date</th></tr></thead>
          <tbody>
            <tr v-if="loading"><td colspan="7" class="carrier-category-empty">載入中…</td></tr>
            <tr v-else-if="visibleRows.length===0"><td colspan="7" class="carrier-category-empty">沒有符合的載具分類資料。</td></tr>
            <tr v-for="row in visibleRows" :key="row.carrierCategory" :class="{ 'carrier-category-current': currentNo===row.carrierCategory, 'carrier-category-marked': selectedNos.includes(row.carrierCategory) }" @click="selectRow(row.carrierCategory)" @dblclick="selectRow(row.carrierCategory); openForm('edit')">
              <td class="carrier-category-check" @click.stop><input type="checkbox" :checked="selectedNos.includes(row.carrierCategory)" :aria-label="'選取 ' + row.carrierCategory" @change="toggleSelected(row.carrierCategory)"></td>
              <td>{{ row.carrierCategory }}</td><td>{{ row.description || '' }}</td><td>{{ row.creator || '' }}</td><td>{{ dateText(row.createDate) }}</td><td>{{ row.reviser || '' }}</td><td>{{ dateText(row.reviseDate) }}</td>
            </tr>
          </tbody>
        </table>
      </div>
      <aside class="carrier-category-actions">
        <button type="button" class="carrier-category-primary" :disabled="busy" @click="openForm('new')">新增(A)</button>
        <button type="button" :disabled="busy || current?.issueState!==0" @click="openForm('edit')">修改(E)</button>
        <button type="button" :disabled="busy || (!selectedNos.length && !current)" @click="deleteSelected">刪除(D)</button>
        <button type="button" :disabled="busy || !current" @click="openForm('copy')">複製(C)</button>
        <button type="button" :disabled="busy || (!selectedNos.length && !current)" @click="approveSelected">核准</button>
        <button type="button" class="carrier-category-close-page" :disabled="busy" @click="closePage">關閉</button>
      </aside>
    </div>

    <div v-if="mode" class="carrier-category-overlay" @click.self="closeForm">
      <form class="carrier-category-dialog" @submit.prevent="saveForm(false)">
        <header class="carrier-category-dialog-header"><strong>{{ formTitle }}</strong><button type="button" aria-label="關閉" :disabled="busy" @click="closeForm">×</button></header>
        <div class="carrier-category-dialog-body">
          <fieldset class="carrier-category-fields">
            <legend>Carrier Category</legend>
            <label><span>Carrier Category</span><input ref="categoryInput" v-model="form.carrierCategory" type="text" :readonly="mode==='edit'" @input="markDirty" @blur="normalizeCategory" @keydown.enter.prevent="focusDescription"></label>
            <label class="carrier-category-description"><span>Description</span><textarea v-model="form.description" rows="3" @input="markDirty"></textarea></label>
          </fieldset>
          <fieldset class="carrier-category-audit">
            <legend>建立／修改資料</legend>
            <label><span>建立人</span><input :value="audit.creator" readonly></label>
            <label><span>建立日期</span><input :value="dateText(audit.createDate)" readonly></label>
            <label><span>修改人</span><input :value="audit.reviser" readonly></label>
            <label><span>修改日期</span><input :value="dateText(audit.reviseDate)" readonly></label>
          </fieldset>
          <p v-if="error" class="carrier-category-message carrier-category-error" role="alert">{{ error }}</p>
        </div>
        <footer class="carrier-category-dialog-footer">
          <button type="submit" class="carrier-category-primary" :disabled="busy">確定</button>
          <button type="button" :disabled="busy" @click="closeForm">關閉</button>
          <button type="button" :disabled="busy || !formDirty" @click="saveForm(true)">套用</button>
        </footer>
      </form>
    </div>
  </section>
</template>

<style scoped>
.carrier-category-page{display:flex;height:100%;min-height:0;flex-direction:column;gap:6px;color:#333;font-size:13px}
.carrier-category-dev-note{padding:7px 10px;border:1px solid #d8c577;background:#fff6c8;color:#66551b}
.carrier-category-search{display:flex;min-height:38px;align-items:center;gap:7px;padding:5px 9px;border:1px solid #c9c9c9;background:#fafafa}
.carrier-category-states{display:flex;align-items:center;gap:13px;margin-right:10px}
.carrier-category-states label{display:flex;align-items:center;gap:3px;white-space:nowrap}
.carrier-category-states input,.carrier-category-check input{margin:0;accent-color:#ad831b}
.carrier-category-search>input{width:min(270px,28vw);min-height:29px;padding:4px 7px;border:1px solid #a9a9a9;background:#fff0b3;color:#333;font:inherit}
.carrier-category-like{margin-left:-7px;color:#555}
.carrier-category-search button,.carrier-category-actions button,.carrier-category-dialog-footer button{min-height:30px;padding:4px 12px;border:1px solid #bdbdbd;border-radius:3px;background:#fafafa;color:#333;font:inherit;cursor:pointer}
.carrier-category-search button:hover,.carrier-category-actions button:hover,.carrier-category-dialog-footer button:hover{background:#fff2c3;border-color:#c8aa54}
.carrier-category-search button:disabled,.carrier-category-actions button:disabled,.carrier-category-dialog-footer button:disabled{opacity:.55;cursor:not-allowed}
.carrier-category-primary{background:#f5a623!important;border-color:#e49a24!important;color:#fff!important}
.carrier-category-message{margin:0;padding:7px 10px;border:1px solid}
.carrier-category-error{border-color:#dca7a2;background:#fff1ef;color:#942f24}
.carrier-category-success{border-color:#c8d8ac;background:#f4f8eb;color:#496622}
.carrier-category-main{display:grid;min-height:0;flex:1;grid-template-columns:minmax(0,1fr) 88px;gap:7px}
.carrier-category-grid-wrap{min-height:0;overflow:auto;border:1px solid #bdbdbd;background:#fff}
.carrier-category-grid{width:100%;border-collapse:collapse;white-space:nowrap}
.carrier-category-grid th{position:sticky;top:0;z-index:1;padding:5px 7px;border:1px solid #d2c36d;background:#e7d978;color:#453d20;text-align:left;font-weight:600}
.carrier-category-grid td{height:27px;padding:4px 7px;border:1px solid #d6d6d6}
.carrier-category-grid tbody tr{cursor:pointer}
.carrier-category-grid tbody tr:nth-child(even){background:#fafafa}
.carrier-category-grid tbody tr:hover{background:#fff6d6}
.carrier-category-grid tbody tr.carrier-category-marked{background:#fff7dc}
.carrier-category-grid tbody tr.carrier-category-current{background:#fff0b7}
.carrier-category-check{width:28px;text-align:center}
.carrier-category-empty{height:45px!important;color:#777;text-align:center}
.carrier-category-actions{display:flex;min-height:0;flex-direction:column;gap:6px;padding-top:0}
.carrier-category-actions button{width:100%;min-height:32px}
.carrier-category-close-page{margin-top:auto!important}
.carrier-category-overlay{position:fixed;inset:0;z-index:80;display:grid;place-items:center;padding:16px;background:rgba(0,0,0,.35)}
.carrier-category-dialog{display:flex;width:min(492px,96vw);height:min(319px,92vh);max-height:92vh;flex-direction:column;border:1px solid #999;background:#fdfdfd;box-shadow:0 5px 20px rgba(0,0,0,.24)}
.carrier-category-dialog-header{display:flex;min-height:38px;align-items:center;justify-content:space-between;padding:6px 12px;border-bottom:1px solid #c9c9c9;background:#f4f1e8;font-size:15px}
.carrier-category-dialog-header button{width:28px;height:26px;border:0;background:transparent;font-size:22px;cursor:pointer}
.carrier-category-dialog-body{display:flex;min-height:0;flex:1;flex-direction:column;gap:7px;overflow:auto;padding:8px}
.carrier-category-fields{display:grid;flex:none;grid-template-columns:1fr;gap:7px;margin:0;padding:7px 9px 9px;border:1px solid #bdbdbd}
.carrier-category-fields legend,.carrier-category-audit legend{padding:0 4px;color:#555}
.carrier-category-fields label{display:grid;min-width:0;grid-template-columns:100px minmax(0,1fr);align-items:center;gap:7px}
.carrier-category-fields label>span{color:#555;text-align:right}
.carrier-category-fields input,.carrier-category-fields textarea{min-width:0;padding:4px 7px;border:1px solid #a9a9a9;background:#fff0b3;color:#333;font:inherit}
.carrier-category-fields input{width:152px;min-height:25px}
.carrier-category-fields input[readonly]{background:#eee}
.carrier-category-fields .carrier-category-description{align-items:start}
.carrier-category-fields textarea{min-height:65px;resize:vertical}
.carrier-category-audit{display:grid;flex:none;grid-template-columns:1fr 1fr;gap:4px 8px;margin:0;padding:5px 8px 7px;border:1px solid #bdbdbd}
.carrier-category-audit label{display:flex;min-width:0;align-items:center;gap:5px}
.carrier-category-audit label span{width:56px;flex:none;color:#555;text-align:right}
.carrier-category-audit input{flex:1;min-width:0;min-height:25px;padding:3px 5px;border:1px solid #c5c5c5;background:#eee;color:#555;font:inherit}
.carrier-category-dialog-footer{display:flex;min-height:48px;align-items:center;justify-content:flex-end;gap:7px;padding:7px 12px;border-top:1px solid #ccc}
@media(max-width:800px){.carrier-category-search{flex-wrap:wrap}.carrier-category-states{width:100%;flex-wrap:wrap}.carrier-category-grid{min-width:850px}.carrier-category-dialog{width:96vw;height:92vh}.carrier-category-audit{grid-template-columns:1fr}}
</style>
