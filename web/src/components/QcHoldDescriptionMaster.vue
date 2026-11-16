<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

type IssueState = -1 | 0 | 1 | 2
type EditorMode = 'add' | 'edit' | 'copy' | null
interface HoldDescription {
  holdDescriptionId: string
  mClassNo: string
  holdDescription: string
  description: string | null
  issueState: IssueState
  creator: string | null
  createDate: string | null
  reviser: string | null
  reviseDate: string | null
  dataStamp: number | null
}
interface MClassOption { mClassNo: string; description?: string | null }
interface MockStore { rows: HoldDescription[] }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const useMock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes-dev-mock:qc-hold-description:v1'
const issueStates: { value: IssueState; label: string }[] = [
  { value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' },
]
const seedRows: HoldDescription[] = [
  { holdDescriptionId: 'DEV-HOLD-DESC-001', mClassNo: 'DEV-MCLASS-A', holdDescription: '發現產品外觀刮傷，請隔離待判。', description: '一般外觀異常說明', issueState: 0, creator: 'DEV001', createDate: '2026-11-04T08:00:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
  { holdDescriptionId: 'DEV-HOLD-DESC-002', mClassNo: 'DEV-MCLASS-A', holdDescription: '尺寸量測結果超出允收規格。', description: '尺寸異常', issueState: 1, creator: 'DEV001', createDate: '2026-11-04T08:30:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-04T09:00:00+08:00', dataStamp: 1 },
  { holdDescriptionId: 'DEV-HOLD-DESC-003', mClassNo: 'DEV-MCLASS-B', holdDescription: '製程參數異常，停止後續加工並通知工程。', description: '參數超限處理', issueState: 2, creator: 'DEV001', createDate: '2026-11-05T08:00:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-05T09:00:00+08:00', dataStamp: 2 },
  { holdDescriptionId: 'DEV-HOLD-DESC-004', mClassNo: 'DEV-MCLASS-B', holdDescription: '待確認批次及影響範圍。', description: '已停用的範例', issueState: -1, creator: 'DEV001', createDate: '2026-11-06T08:00:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-06T09:00:00+08:00', dataStamp: 3 },
]
const seedMClasses: MClassOption[] = [
  { mClassNo: 'DEV-MCLASS-A', description: 'DEV 主分類 A' }, { mClassNo: 'DEV-MCLASS-B', description: 'DEV 主分類 B' },
]

const rows = ref<HoldDescription[]>([])
const mClasses = ref<MClassOption[]>([])
const issueState = ref<IssueState>(0)
const searchText = ref('')
const selectedIds = ref<string[]>([])
const activeId = ref('')
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const notice = ref('')
const editorMode = ref<EditorMode>(null)
const sourceRow = ref<HoldDescription | null>(null)
const editor = ref({ mClassNo: '', holdDescription: '', description: '' })
const editorError = ref('')
const editorNotice = ref('')
const editorBusy = ref(false)
const editorStartedAt = ref('')
const pickerOpen = ref(false)
const pickerSearch = ref('')
const pickerSelection = ref('')

const visibleRows = computed(() => rows.value
  .filter(row => row.issueState === issueState.value)
  .filter(row => !searchText.value.trim() || row.mClassNo.toLocaleLowerCase().includes(searchText.value.trim().toLocaleLowerCase())))
const selectedRows = computed(() => rows.value.filter(row => selectedIds.value.includes(row.holdDescriptionId)))
const activeRow = computed(() => rows.value.find(row => row.holdDescriptionId === activeId.value) ?? null)
const editorTitle = computed(() => `異常說明（${editorMode.value === 'add' ? '新增' : editorMode.value === 'copy' ? '複製' : '修改'}）`)
const isEditLocked = computed(() => editorMode.value === 'edit' && (sourceRow.value?.issueState === 1 || sourceRow.value?.issueState === -1))
const keyFieldsLocked = computed(() => editorMode.value === 'edit' || isEditLocked.value)
const auditCreator = computed(() => editorMode.value === 'edit' ? sourceRow.value?.creator || '' : props.currentUserNo || '')
const auditCreateDate = computed(() => editorMode.value === 'edit' ? sourceRow.value?.createDate || '' : editorStartedAt.value)
const auditReviser = computed(() => editorMode.value === 'edit' ? sourceRow.value?.reviser || '' : '')
const auditReviseDate = computed(() => editorMode.value === 'edit' ? sourceRow.value?.reviseDate || '' : '')
const pickerRows = computed(() => {
  const query = pickerSearch.value.trim().toLocaleLowerCase()
  return mClasses.value.filter(row => !query || row.mClassNo.toLocaleLowerCase().includes(query) || String(row.description ?? '').toLocaleLowerCase().includes(query))
})

function dateText(value?: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat('zh-TW', { dateStyle: 'short', timeStyle: 'medium', hour12: false }).format(date)
}
function rowKey(row: Pick<HoldDescription, 'holdDescriptionId'>): string { return row.holdDescriptionId }
function now(): string { return new Date().toISOString() }
function enc(value: string): string { return encodeURIComponent(value) }
function stampQuery(value: number | null): string { return value === null ? 'dataStamp=0&dataStampIsNull=true' : `dataStamp=${enc(String(value))}` }
function fakeId(): string {
  return globalThis.crypto?.randomUUID?.() ?? `DEV-${Date.now()}-${Math.random().toString(16).slice(2)}`
}
function readStore(): MockStore {
  try {
    const saved = window.localStorage.getItem(storageKey)
    if (!saved) return { rows: structuredClone(seedRows) }
    const parsed = JSON.parse(saved) as Partial<MockStore>
    return { rows: parsed.rows ?? structuredClone(seedRows) }
  } catch { return { rows: structuredClone(seedRows) } }
}
function writeStore(store: MockStore): void { window.localStorage.setItem(storageKey, JSON.stringify(store)) }
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init, headers: { ...(init.body ? { 'Content-Type': 'application/json' } : {}), ...init.headers } })
  if (response.status === 401) { window.location.assign(appBase + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    if (response.status === 404) throw new Error('異常說明 API 尚未建立；請依 todolist_homepc.md 完成 QC404。')
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 回應錯誤（${response.status}）。`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}

async function loadRows(): Promise<void> {
  if (useMock.value) { rows.value = readStore().rows; return }
  const query = new URLSearchParams()
  if (searchText.value.trim()) { query.set('searchFieldName', 'MClassNo'); query.set('searchFieldValue', searchText.value.trim()) }
  const queryText = query.toString()
  rows.value = await request<HoldDescription[]>(`api/qc/hold-descriptions${queryText ? '?' + queryText : ''}`)
}
async function loadOptions(): Promise<void> {
  if (useMock.value) { mClasses.value = structuredClone(seedMClasses); return }
  mClasses.value = await request<MClassOption[]>('api/qc/hold-descriptions/options/machine-classes?issueState=2')
}
async function query(): Promise<void> {
  loading.value = true; busy.value = true; errorMessage.value = ''; notice.value = ''; selectedIds.value = []; activeId.value = ''
  try { await loadRows() } catch (error) { errorMessage.value = error instanceof Error ? error.message : '查詢異常說明失敗。' }
  finally { loading.value = false; busy.value = false }
}
function selectRow(row: HoldDescription): void { selectedIds.value = [rowKey(row)]; activeId.value = rowKey(row) }
function toggleRow(row: HoldDescription, checked: boolean): void {
  const key = rowKey(row)
  activeId.value = key
  selectedIds.value = checked ? [...new Set([...selectedIds.value, key])] : selectedIds.value.filter(item => item !== key)
}
function isSelected(row: HoldDescription): boolean { return selectedIds.value.includes(rowKey(row)) }
watch(issueState, () => { selectedIds.value = []; activeId.value = '' })

function openEditor(mode: Exclude<EditorMode, null>): void {
  errorMessage.value = ''; editorError.value = ''; editorNotice.value = ''
  sourceRow.value = mode === 'add' ? null : activeRow.value ? structuredClone(activeRow.value) : null
  if (mode !== 'add' && !sourceRow.value) { errorMessage.value = '請先選取一筆異常說明。'; return }
  editorStartedAt.value = mode === 'edit' ? '' : now()
  if (mode === 'add') editor.value = { mClassNo: '', holdDescription: '', description: '' }
  else if (sourceRow.value) editor.value = { mClassNo: sourceRow.value.mClassNo, holdDescription: sourceRow.value.holdDescription, description: sourceRow.value.description ?? '' }
  editorMode.value = mode
}
function closeEditor(): void { editorMode.value = null; sourceRow.value = null; editorStartedAt.value = ''; editorError.value = ''; editorNotice.value = '' }
function openPicker(): void { pickerOpen.value = true; pickerSearch.value = ''; pickerSelection.value = '' }
function closePicker(): void { pickerOpen.value = false; pickerSearch.value = ''; pickerSelection.value = '' }
function confirmPicker(): void { if (pickerSelection.value) { editor.value.mClassNo = pickerSelection.value; closePicker() } }
function validateEditor(): string {
  if (!editor.value.mClassNo.trim()) return '主分類編號為必填欄位。'
  if (!editor.value.holdDescription.trim()) return '異常說明為必填欄位。'
  if (editor.value.mClassNo.length > 30) return '主分類編號最多 30 字。'
  if (editor.value.holdDescription.length > 4000) return '異常說明最多 4000 字。'
  if (editor.value.description.length > 255) return 'Description 最多 255 字。'
  if (isEditLocked.value) return '此狀態的資料不可修改。'
  return ''
}
async function refreshRows(preserve: string[] = []): Promise<void> {
  await loadRows()
  const available = new Set(rows.value.filter(row => row.issueState === issueState.value).map(rowKey))
  selectedIds.value = preserve.filter(key => available.has(key))
  activeId.value = selectedIds.value[0] ?? ''
}
async function saveEditor(keepOpen: boolean): Promise<void> {
  editorError.value = validateEditor(); editorNotice.value = ''
  if (editorError.value) return
  const mode = editorMode.value
  const original = sourceRow.value
  editorBusy.value = true; errorMessage.value = ''; notice.value = ''
  try {
    let savedId = ''
    if (useMock.value) {
      const store = readStore()
      if (mode === 'edit' && original) {
        const index = store.rows.findIndex(row => rowKey(row) === rowKey(original))
        if (index < 0) throw new Error('資料已不存在，請重新查詢。')
        store.rows[index] = { ...store.rows[index], description: editor.value.description.trim() || null, reviser: props.currentUserNo || 'DEV001', reviseDate: now(), dataStamp: (store.rows[index].dataStamp ?? 0) + 1 }
        savedId = original.holdDescriptionId
      } else {
        savedId = fakeId()
        store.rows.push({ holdDescriptionId: savedId, mClassNo: editor.value.mClassNo.trim(), holdDescription: editor.value.holdDescription, description: editor.value.description.trim() || null, issueState: 0, creator: props.currentUserNo || 'DEV001', createDate: now(), reviser: null, reviseDate: null, dataStamp: 0 })
      }
      writeStore(store)
    } else if (mode === 'edit' && original) {
      await request(`api/qc/hold-descriptions/${enc(original.holdDescriptionId)}?${stampQuery(original.dataStamp)}`, { method: 'PUT', body: JSON.stringify({ description: editor.value.description.trim() || null }) })
      savedId = original.holdDescriptionId
    } else {
      const created = await request<HoldDescription>('api/qc/hold-descriptions', { method: 'POST', body: JSON.stringify({ mClassNo: editor.value.mClassNo.trim(), holdDescription: editor.value.holdDescription, description: editor.value.description.trim() || null }) })
      savedId = created.holdDescriptionId
    }
    await refreshRows(savedId ? [savedId] : [])
    if (mode === 'edit' && original) sourceRow.value = structuredClone(rows.value.find(row => rowKey(row) === rowKey(original)) ?? original)
    notice.value = mode === 'edit' ? '異常說明已儲存。' : '已建立異常說明。'
    if (keepOpen) editorNotice.value = '儲存完成。'
    else closeEditor()
  } catch (error) { editorError.value = error instanceof Error ? error.message : '儲存異常說明失敗。' }
  finally { editorBusy.value = false }
}
async function deleteSelected(): Promise<void> {
  const selected = selectedRows.value
  if (!selected.length) { errorMessage.value = '請選取要刪除的異常說明。'; return }
  if (selected.some(row => row.issueState === 1)) { errorMessage.value = '簽核中的資料不可刪除。'; return }
  if (!window.confirm(`確定刪除選取的 ${selected.length} 筆資料？已簽核資料會標為不使用。`)) return
  busy.value = true; errorMessage.value = ''; notice.value = ''; let completed = 0
  try {
    if (useMock.value) {
      const store = readStore()
      for (const row of selected) {
        if (row.issueState === 2) {
          const index = store.rows.findIndex(item => rowKey(item) === rowKey(row))
          if (index >= 0) store.rows[index] = { ...store.rows[index], issueState: -1, reviser: props.currentUserNo || 'DEV001', reviseDate: now(), dataStamp: (store.rows[index].dataStamp ?? 0) + 1 }
        } else store.rows = store.rows.filter(item => rowKey(item) !== rowKey(row))
        completed++
      }
      writeStore(store)
    } else {
      for (const row of selected) {
        await request(`api/qc/hold-descriptions/${enc(row.holdDescriptionId)}?issueState=${row.issueState}&${stampQuery(row.dataStamp)}`, { method: 'DELETE' })
        completed++
      }
    }
    await refreshRows()
    notice.value = `已處理 ${completed} 筆異常說明。`
  } catch (error) { errorMessage.value = `${error instanceof Error ? error.message : '刪除異常說明失敗。'}（已完成 ${completed} 筆）`; await refreshRows() }
  finally { busy.value = false }
}
async function approveSelected(): Promise<void> {
  const selected = selectedRows.value
  if (!selected.length) { errorMessage.value = '請選取要核准的異常說明。'; return }
  if (selected.some(row => row.issueState !== 0)) { errorMessage.value = '只有未簽核資料可以送出核准。'; return }
  if (!window.confirm(`確定核准選取的 ${selected.length} 筆異常說明？`)) return
  busy.value = true; errorMessage.value = ''; notice.value = ''; let completed = 0
  try {
    if (useMock.value) {
      const store = readStore()
      for (const row of selected) {
        const index = store.rows.findIndex(item => rowKey(item) === rowKey(row))
        if (index >= 0) store.rows[index] = { ...store.rows[index], issueState: 2, reviser: props.currentUserNo || 'DEV001', reviseDate: now(), dataStamp: (store.rows[index].dataStamp ?? 0) + 1 }
        completed++
      }
      writeStore(store)
    } else {
      for (const row of selected) {
        await request(`api/qc/hold-descriptions/${enc(row.holdDescriptionId)}/approve`, { method: 'POST', body: JSON.stringify({ issueState: row.issueState, dataStamp: row.dataStamp, creator: props.currentUserNo || '' }) })
        completed++
      }
    }
    await refreshRows()
    notice.value = `已送出 ${completed} 筆異常說明核准。`
  } catch (error) { errorMessage.value = `${error instanceof Error ? error.message : '核准異常說明失敗。'}（已完成 ${completed} 筆）`; await refreshRows() }
  finally { busy.value = false }
}

onMounted(() => { void loadOptions().catch(error => { errorMessage.value = error instanceof Error ? error.message : '載入主分類失敗。' }); void query() })
</script>

<template>
  <main class="hold-description-page">
    <header class="page-heading"><div><h1>異常說明</h1><p>依主分類維護品質異常說明</p></div><span v-if="useMock" class="dev-badge">DEV 假資料</span></header>
    <section class="query-panel" aria-label="查詢條件">
      <div class="state-options"><label v-for="state in issueStates" :key="state.value" class="state-option"><input v-model="issueState" type="radio" :value="state.value" name="hold-description-issue-state"><span>{{ state.label }}</span></label></div>
      <label class="search-field"><span>主分類編號</span><input v-model.trim="searchText" class="search-input" @keyup.enter="query"></label><span class="like-hint">%</span>
      <button type="button" class="button query-button" :disabled="loading || busy" @click="query">{{ loading ? '查詢中…' : '查詢' }}</button>
    </section>
    <p v-if="errorMessage" class="message error-message" role="alert">{{ errorMessage }}</p><p v-if="notice" class="message notice-message" role="status">{{ notice }}</p>
    <section class="grid-panel">
      <div class="table-scroll"><table class="data-grid"><thead><tr><th class="check-column"></th><th>主分類編號</th><th>異常說明</th><th>Description</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th></tr></thead><tbody>
        <tr v-for="row in visibleRows" :key="row.holdDescriptionId" :class="[{ 'active-row': activeId === row.holdDescriptionId }, `state-row-${row.issueState}`]" @click="selectRow(row)">
          <td class="check-column"><input type="checkbox" :checked="isSelected(row)" :aria-label="`選取 ${row.mClassNo} ${row.holdDescription.slice(0, 24)}`" @click.stop @change="toggleRow(row, ($event.target as HTMLInputElement).checked)"></td>
          <td>{{ row.mClassNo }}</td><td class="multiline-cell">{{ row.holdDescription }}</td><td class="multiline-cell">{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ dateText(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ dateText(row.reviseDate) }}</td>
        </tr><tr v-if="visibleRows.length === 0"><td colspan="8" class="empty-row">{{ loading ? '資料載入中…' : '查無異常說明資料' }}</td></tr>
      </tbody></table></div>
      <aside class="action-rail" aria-label="異常說明功能">
        <button type="button" class="button primary" :disabled="busy" @click="openEditor('add')">新增(A)</button>
        <button type="button" class="button" :disabled="busy || !activeRow" @click="openEditor('edit')">修改(E)</button>
        <button type="button" class="button" :disabled="busy || !activeRow" @click="openEditor('copy')">複製(C)</button>
        <button type="button" class="button" :disabled="busy || selectedRows.length === 0 || selectedRows.some(row => row.issueState === 1)" @click="deleteSelected">刪除(D)</button>
        <button type="button" class="button" :disabled="busy || selectedRows.length === 0 || selectedRows.some(row => row.issueState !== 0)" @click="approveSelected">核准(Y)</button>
        <button type="button" class="button close-button" @click="emit('close')">關閉(X)</button>
      </aside>
      <footer class="grid-footer">資料總筆數：{{ visibleRows.length }} <span>HoldDescriptionID、IssueState 與 DataStamp 沿用來源表，主畫面不顯示。</span></footer>
    </section>

    <div v-if="editorMode" class="dialog-backdrop" role="presentation" @click.self="closeEditor">
      <section class="editor-dialog" role="dialog" aria-modal="true" :aria-label="editorTitle">
        <header class="dialog-titlebar"><h2>{{ editorTitle }}</h2><button type="button" class="icon-close" aria-label="關閉" @click="closeEditor">×</button></header>
        <form class="editor-form" @submit.prevent="saveEditor(false)">
          <fieldset class="hold-panel"><legend>異常說明主檔</legend>
            <label class="form-field"><span>主分類編號<span class="required">*</span></span><div class="picker-control"><input :value="editor.mClassNo" readonly><button type="button" class="button picker-button" :disabled="keyFieldsLocked" @click="openPicker">...</button></div></label>
            <label class="form-field hold-text-field"><span>異常說明<span class="required">*</span></span><textarea v-model="editor.holdDescription" maxlength="4000" rows="5" :readonly="keyFieldsLocked"></textarea></label>
          </fieldset>
          <fieldset class="description-panel"><legend>Description</legend><textarea v-model="editor.description" maxlength="255" rows="3" :readonly="isEditLocked"></textarea></fieldset>
          <fieldset class="audit-panel"><legend>建立/修改資訊</legend><label class="form-field"><span>建立人</span><input :value="auditCreator" readonly></label><label class="form-field"><span>建立日期</span><input :value="dateText(auditCreateDate)" readonly></label><label class="form-field"><span>修改人</span><input :value="auditReviser" readonly></label><label class="form-field"><span>修改日期</span><input :value="dateText(auditReviseDate)" readonly></label></fieldset>
          <p v-if="editorError" class="message error-message" role="alert">{{ editorError }}</p><p v-if="editorNotice" class="message notice-message" role="status">{{ editorNotice }}</p>
          <footer class="dialog-actions"><button type="submit" class="button primary" :disabled="editorBusy || isEditLocked">確定</button><button type="button" class="button" :disabled="editorBusy" @click="closeEditor">關閉</button><button type="button" class="button" :disabled="editorBusy || isEditLocked" @click="saveEditor(true)">套用</button></footer>
        </form>
      </section>
    </div>

    <div v-if="pickerOpen" class="dialog-backdrop picker-backdrop" role="presentation" @click.self="closePicker">
      <section class="picker-dialog" role="dialog" aria-modal="true" aria-label="選擇主分類"><header class="dialog-titlebar"><h2>選擇主分類</h2><button type="button" class="icon-close" aria-label="關閉" @click="closePicker">×</button></header>
        <label class="picker-search"><span>查詢</span><input v-model.trim="pickerSearch" type="search" autofocus></label>
        <div class="table-scroll picker-table"><table class="data-grid"><thead><tr><th>主分類編號</th><th>Description</th></tr></thead><tbody>
          <tr v-for="option in pickerRows" :key="option.mClassNo" :class="{ 'active-row': pickerSelection === option.mClassNo }" @click="pickerSelection = option.mClassNo" @dblclick="pickerSelection = option.mClassNo; confirmPicker()"><td>{{ option.mClassNo }}</td><td>{{ option.description }}</td></tr>
          <tr v-if="pickerRows.length === 0"><td colspan="2" class="empty-row">查無資料</td></tr>
        </tbody></table></div>
        <footer class="dialog-actions"><button type="button" class="button primary" :disabled="!pickerSelection" @click="confirmPicker">確定</button><button type="button" class="button" @click="closePicker">關閉</button></footer>
      </section>
    </div>
  </main>
</template>

<style scoped>
.hold-description-page { --kd-line:#c8c4bb; --kd-text:#30302e; --kd-muted:#77746e; --kd-field:#fff0ad; --kd-button:#f7a32b; --kd-button-dark:#df8612; display:flex; flex-direction:column; gap:8px; height:100%; min-height:0; padding:12px; color:var(--kd-text); background:#f7f5ef; font:14px/1.35 "Segoe UI","Microsoft JhengHei",sans-serif; }
.page-heading { display:flex; align-items:center; justify-content:space-between; min-height:42px; } .page-heading h1 { margin:0; font-size:20px; font-weight:600; } .page-heading p { margin:2px 0 0; color:var(--kd-muted); font-size:12px; } .dev-badge { padding:4px 9px; border:1px solid #d7a044; border-radius:3px; background:#fff1bd; color:#77551a; font-size:12px; }
.query-panel { display:flex; flex-wrap:wrap; align-items:center; gap:12px; min-height:46px; padding:6px 10px; border:1px solid var(--kd-line); background:#fff; } .state-options { display:flex; flex-wrap:wrap; align-items:center; gap:12px; } .state-option,.search-field { display:inline-flex; align-items:center; gap:5px; white-space:nowrap; } .state-option { cursor:pointer; } .state-option input { accent-color:#e18b17; } .search-input { width:190px; } .like-hint { margin-left:-8px; }
input,select,textarea { box-sizing:border-box; min-height:30px; padding:5px 7px; border:1px solid #a9a69f; border-radius:2px; background:var(--kd-field); color:var(--kd-text); font:inherit; } input:focus,select:focus,textarea:focus { border-color:#d18a21; outline:2px solid rgb(209 138 33 / 20%); } input[readonly],input:disabled,select:disabled,textarea[readonly] { background:#ecebe7; color:#777; } textarea { resize:vertical; }
.button { min-height:30px; padding:4px 11px; border:1px solid #bdb9b0; border-radius:3px; background:linear-gradient(#fff,#f0efec); color:#343331; font:inherit; white-space:nowrap; cursor:pointer; } .button:hover:not(:disabled) { border-color:#d08a23; background:#fff6df; } .button:disabled { opacity:.48; cursor:not-allowed; } .button.primary { border-color:var(--kd-button-dark); background:var(--kd-button); color:#fff; } .button.primary:hover:not(:disabled) { background:var(--kd-button-dark); } .query-button { min-width:58px; }
.message { flex:0 0 auto; margin:0; padding:7px 10px; border:1px solid; border-radius:2px; } .error-message { border-color:#d99b86; background:#fff0e9; color:#873820; } .notice-message { border-color:#c9bc91; background:#fff8dc; color:#67551d; }
.grid-panel { display:grid; grid-template-columns:minmax(0,1fr) 88px; grid-template-rows:minmax(0,1fr) auto; gap:7px 9px; flex:1 1 auto; min-height:220px; padding:6px; border:1px solid var(--kd-line); background:#fff; } .table-scroll { min-width:0; min-height:0; overflow:auto; border:1px solid #c5c1b9; } .data-grid { width:100%; border-collapse:separate; border-spacing:0; font-size:13px; white-space:nowrap; } .data-grid th { position:sticky; top:0; z-index:1; padding:6px 7px; border-right:1px solid #d4d0c8; border-bottom:1px solid #bcb8af; background:#eee9de; color:#3f3c36; text-align:left; font-weight:600; } .data-grid td { height:29px; padding:4px 7px; border-right:1px solid #e1ded8; border-bottom:1px solid #e1ded8; } .data-grid tbody tr { cursor:pointer; } .data-grid tbody tr:hover { background:#fff8e3; } .data-grid tbody tr.active-row { background:#ffedaa; } .check-column { width:28px; min-width:28px; text-align:center!important; } .data-grid input[type=checkbox] { width:15px; min-height:15px; height:15px; margin:0; accent-color:#d98717; vertical-align:middle; } .multiline-cell { min-width:170px; max-width:360px; white-space:pre-wrap; overflow-wrap:anywhere; } .empty-row { height:48px!important; color:#777; text-align:center; cursor:default; }
.action-rail { display:flex; flex-direction:column; gap:7px; } .action-rail .button { min-height:32px; } .close-button { margin-top:auto; } .grid-footer { grid-column:1/-1; display:flex; gap:14px; min-height:20px; padding:3px 2px 0; color:var(--kd-muted); font-size:12px; }
.dialog-backdrop { position:fixed; z-index:50; inset:0; display:flex; align-items:center; justify-content:center; padding:18px; background:rgb(28 27 25 / 42%); } .editor-dialog,.picker-dialog { display:flex; flex-direction:column; width:min(560px,96vw); max-height:min(90vh,680px); overflow:auto; border:1px solid #b9b4aa; background:#f8f6f1; box-shadow:0 12px 36px rgb(0 0 0 / 24%); } .editor-dialog { width:min(484px,96vw); } .picker-dialog { width:min(620px,96vw); } .dialog-titlebar { position:sticky; top:0; z-index:2; display:flex; align-items:center; justify-content:space-between; padding:10px 15px; border-bottom:1px solid #d2cec5; background:#f2efe8; } .dialog-titlebar h2 { margin:0; font-size:17px; font-weight:600; } .icon-close { width:30px; height:28px; border:0; background:transparent; color:#4c4943; font-size:23px; cursor:pointer; } .editor-form { display:flex; flex-direction:column; gap:10px; padding:12px; } .hold-panel,.description-panel,.audit-panel { border:1px solid #c9c4ba; padding:9px; } .hold-panel legend,.description-panel legend,.audit-panel legend { padding:0 5px; color:#5a564f; } .form-field { display:flex; flex-direction:column; gap:5px; min-width:0; color:#46433d; } .form-field>span { font-size:13px; } .picker-control { display:flex; gap:6px; } .picker-control input { flex:1; } .picker-button { width:38px; } .hold-text-field textarea,.description-panel textarea { width:100%; } .hold-text-field textarea { min-height:92px; } .description-panel textarea { min-height:60px; } .audit-panel { display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); gap:8px 12px; } .audit-panel .form-field input { width:100%; background:#ecebe7; } .required { padding-left:3px; color:#a33a25; } .dialog-actions { display:flex; justify-content:flex-end; gap:8px; padding:7px 12px 12px; border-top:1px solid #ddd9d1; } .picker-search { display:flex; align-items:center; gap:10px; padding:10px 14px; } .picker-search input { flex:1; } .picker-table { min-height:190px; max-height:50vh; margin:0 14px 12px; }
@media(max-width:850px) { .hold-description-page { height:auto; min-height:100%; } .grid-panel { grid-template-columns:1fr; grid-template-rows:minmax(340px,1fr) auto auto; } .action-rail { flex-direction:row; flex-wrap:wrap; } .close-button { margin:0 0 0 auto; } .grid-footer { grid-column:1; } }
@media(max-width:600px) { .state-options { width:100%; } .audit-panel { grid-template-columns:1fr; } .grid-footer { flex-direction:column; gap:2px; } }
</style>
