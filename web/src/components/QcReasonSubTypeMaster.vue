<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type IssueState = -1 | 0 | 1 | 2
type EditorMode = 'add' | 'edit' | 'copy' | null
type EditableField = 'reasonType' | 'reasonSubTypeName' | 'description'
interface ReasonTypeOption { value: number; label: string }
interface ReasonSubTypeRow {
  reasonType: number | null
  reasonTypeName?: string | null
  reasonSubType: string
  reasonSubTypeName: string | null
  description: string | null
  creator: string | null
  createDate: string | null
  issueState: IssueState
  dataStamp: number | null
  reviser: string | null
  reviseDate: string | null
  editableFields?: EditableField[]
}
interface MockStore { rows: ReasonSubTypeRow[] }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes-dev-mock:qc-reason-subtype-basis:v1'
const issueStates: { value: IssueState; label: string }[] = [
  { value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' },
]
// These values are only a local preview based on the legacy form's commented examples.
// Production options must come from the MES value-list equivalent.
const demoReasonTypes: ReasonTypeOption[] = [
  { value: 0, label: '0 — Scrap（DEMO）' }, { value: 1, label: '1 — Defect（DEMO）' },
  { value: 5, label: '5 — Adjust（DEMO）' }, { value: 6, label: '6 — EQP（DEMO）' },
]
const seedRows: ReasonSubTypeRow[] = [
  { reasonType: 0, reasonTypeName: '0 — Scrap（DEMO）', reasonSubType: 'DEV-RS-SCRAP', reasonSubTypeName: 'DEV SCRAP SAMPLE', description: 'DEV 未簽核示例。', creator: 'DEV001', createDate: '2026-11-10T08:00:00+08:00', issueState: 0, dataStamp: 0, reviser: null, reviseDate: null },
  { reasonType: 1, reasonTypeName: '1 — Defect（DEMO）', reasonSubType: 'DEV-RS-DEFECT', reasonSubTypeName: 'DEV DEFECT SAMPLE', description: 'DEV 簽核中示例。', creator: 'DEV001', createDate: '2026-11-10T08:20:00+08:00', issueState: 1, dataStamp: 1, reviser: 'DEV002', reviseDate: '2026-11-10T09:00:00+08:00' },
  { reasonType: 5, reasonTypeName: '5 — Adjust（DEMO）', reasonSubType: 'DEV-RS-ADJUST', reasonSubTypeName: 'DEV ADJUST SAMPLE', description: 'DEV 已簽核示例。', creator: 'DEV002', createDate: '2026-11-11T08:00:00+08:00', issueState: 2, dataStamp: 2, reviser: 'DEV001', reviseDate: '2026-11-11T09:00:00+08:00' },
  { reasonType: 6, reasonTypeName: '6 — EQP（DEMO）', reasonSubType: 'DEV-RS-EQP', reasonSubTypeName: 'DEV UNUSED SAMPLE', description: 'DEV 不使用示例。', creator: 'DEV001', createDate: '2026-11-12T08:00:00+08:00', issueState: -1, dataStamp: 3, reviser: 'DEV001', reviseDate: '2026-11-12T09:00:00+08:00' },
]

const rows = ref<ReasonSubTypeRow[]>([])
const reasonTypes = ref<ReasonTypeOption[]>([])
const issueState = ref<IssueState>(0)
const searchInput = ref('')
const appliedSearch = ref('')
const selectedKeys = ref<string[]>([])
const activeKey = ref('')
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const notice = ref('')
const editorMode = ref<EditorMode>(null)
const sourceRow = ref<ReasonSubTypeRow | null>(null)
const editor = ref({ reasonType: 0, reasonSubType: '', reasonSubTypeName: '', description: '' })
const editorError = ref('')
const editorNotice = ref('')
const saving = ref(false)
const createDate = ref('')

const visibleRows = computed(() => rows.value
  .filter(row => row.issueState === issueState.value)
  .filter(row => matchesSearch(row.reasonSubType, appliedSearch.value))
  .sort((a, b) => a.reasonSubType.localeCompare(b.reasonSubType)))
const selectedRows = computed(() => visibleRows.value.filter(row => selectedKeys.value.includes(row.reasonSubType)))
const activeRow = computed(() => rows.value.find(row => row.reasonSubType === activeKey.value) ?? null)
const editorTitle = computed(() => '品質原因子類別資料（' + (editorMode.value === 'add' ? '新增' : editorMode.value === 'copy' ? '複製' : '修改') + '）')
const currentUser = computed(() => props.currentUserNo?.trim() || (mock.value ? 'DEV001' : ''))
const canSave = computed(() => editorMode.value !== null && !saving.value &&
  (editorMode.value !== 'edit' || editableFields().length > 0))

function clone<T>(value: T): T { return structuredClone(value) }
function now(): string { return new Date().toISOString() }
function formatDate(value: string | null): string {
  if (!value) return ''
  const parsed = new Date(value)
  return Number.isNaN(parsed.getTime()) ? value : parsed.toLocaleString('zh-TW', { hour12: false })
}
function matchesSearch(value: string, pattern: string): boolean {
  if (!pattern) return true
  const escaped = pattern.replace(/[.*+?^${}()|[\]\\]/g, '\\$&').replace(/%/g, '.*').replace(/_/g, '.')
  return new RegExp('^' + escaped + '$', 'i').test(value)
}
function readStore(): MockStore {
  try {
    const stored = window.localStorage.getItem(storageKey)
    if (stored) {
      const parsed = JSON.parse(stored) as MockStore
      if (Array.isArray(parsed.rows)) return parsed
    }
  } catch { /* reset invalid preview data */ }
  const initial = { rows: clone(seedRows) }
  writeStore(initial)
  return initial
}
function writeStore(store: MockStore): void { window.localStorage.setItem(storageKey, JSON.stringify(store)) }
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body && !(init.body instanceof FormData) && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init, headers })
  if (response.status === 401) {
    window.location.assign(appBase + '/')
    throw new Error('登入狀態已逾期，請重新登入。')
  }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; title?: string; message?: string } | null
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 呼叫失敗（${response.status}）。`)
  }
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}
function lookupLabel(value: number | null, fallback?: string | null): string {
  if (value === null) return ''
  return reasonTypes.value.find(option => option.value === value)?.label ?? fallback ?? String(value)
}
function editableFields(): EditableField[] {
  if (editorMode.value !== 'edit' || !sourceRow.value) return ['reasonType', 'reasonSubTypeName', 'description']
  if (sourceRow.value.issueState === 0) return ['reasonType', 'reasonSubTypeName', 'description']
  if (sourceRow.value.issueState === 1) return []
  return sourceRow.value.editableFields ?? []
}
function canEdit(field: EditableField): boolean { return editorMode.value !== 'edit' || editableFields().includes(field) }
function refreshSelection(key = ''): void {
  const match = visibleRows.value.find(row => row.reasonSubType === key)
  activeKey.value = match?.reasonSubType ?? visibleRows.value[0]?.reasonSubType ?? ''
  selectedKeys.value = match ? [match.reasonSubType] : []
}
async function refresh(focusKey = ''): Promise<void> {
  loading.value = true
  errorMessage.value = ''
  try {
    if (mock.value) rows.value = clone(readStore().rows)
    else {
      const query = new URLSearchParams({ searchFieldName: 'ReasonSubType', searchFieldValue: appliedSearch.value })
      const result = await request<ReasonSubTypeRow[] | { items?: ReasonSubTypeRow[] }>('api/qc/reason-subtypes?' + query.toString())
      rows.value = Array.isArray(result) ? result : result.items ?? []
    }
    refreshSelection(focusKey)
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '載入品質原因子類別資料失敗。'
  } finally { loading.value = false }
}
async function loadReasonTypes(): Promise<void> {
  try {
    if (mock.value) reasonTypes.value = clone(demoReasonTypes)
    else {
      const result = await request<ReasonTypeOption[] | { items?: ReasonTypeOption[] }>('api/qc/reason-subtypes/reason-types')
      reasonTypes.value = Array.isArray(result) ? result : result.items ?? []
    }
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '載入原因類別選項失敗。'
  }
}
function query(): void { appliedSearch.value = searchInput.value.trim(); selectedKeys.value = []; void refresh() }
function setIssueState(value: IssueState): void { issueState.value = value; selectedKeys.value = []; refreshSelection() }
function setActive(row: ReasonSubTypeRow): void { activeKey.value = row.reasonSubType }
function toggleSelected(row: ReasonSubTypeRow): void {
  selectedKeys.value = selectedKeys.value.includes(row.reasonSubType)
    ? selectedKeys.value.filter(key => key !== row.reasonSubType)
    : [...selectedKeys.value, row.reasonSubType]
  activeKey.value = row.reasonSubType
}
function openAdd(): void {
  editorMode.value = 'add'; sourceRow.value = null
  editor.value = { reasonType: reasonTypes.value[0]?.value ?? 0, reasonSubType: '', reasonSubTypeName: '', description: '' }
  createDate.value = now(); editorError.value = ''; editorNotice.value = ''
}
function openEdit(): void {
  if (!activeRow.value) return
  editorMode.value = 'edit'; sourceRow.value = clone(activeRow.value)
  editor.value = { reasonType: activeRow.value.reasonType ?? 0, reasonSubType: activeRow.value.reasonSubType, reasonSubTypeName: activeRow.value.reasonSubTypeName ?? '', description: activeRow.value.description ?? '' }
  createDate.value = activeRow.value.createDate ?? ''; editorError.value = ''; editorNotice.value = ''
}
function openCopy(): void {
  if (!activeRow.value) return
  editorMode.value = 'copy'; sourceRow.value = clone(activeRow.value)
  editor.value = { reasonType: activeRow.value.reasonType ?? 0, reasonSubType: activeRow.value.reasonSubType, reasonSubTypeName: activeRow.value.reasonSubTypeName ?? '', description: activeRow.value.description ?? '' }
  createDate.value = now(); editorError.value = ''; editorNotice.value = '複製會帶入來源資料；請輸入新的 ReasonSubType。'
}
function closeEditor(): void { editorMode.value = null; sourceRow.value = null; editorError.value = ''; editorNotice.value = '' }
function validate(): string {
  const key = editor.value.reasonSubType.trim().toUpperCase()
  const name = editor.value.reasonSubTypeName.trim().toUpperCase()
  if (!key) return 'ReasonSubType 為必填。'
  if (key.length > 50) return 'ReasonSubType 不可超過 50 個字元。'
  if (!name) return 'ReasonSubType Name 為必填。'
  if (name.length > 100) return 'ReasonSubType Name 不可超過 100 個字元。'
  if (!reasonTypes.value.some(option => option.value === editor.value.reasonType)) return '請選擇有效的 Reason Type。'
  if (editor.value.description.length > 255) return 'Description 不可超過 255 個字元。'
  if (editorMode.value === 'edit' && sourceRow.value?.issueState === 1) return '簽核中的資料不可修改。'
  if (editorMode.value !== 'edit' && rows.value.some(row => row.reasonSubType.toUpperCase() === key)) return 'ReasonSubType 已存在。'
  if (editorMode.value === 'edit' && editableFields().length === 0) return '此資料狀態目前沒有可修改欄位。'
  return ''
}
async function save(keepOpen: boolean): Promise<void> {
  editorError.value = validate()
  if (editorError.value || !editorMode.value) return
  const mode = editorMode.value
  const original = sourceRow.value
  const key = editor.value.reasonSubType.trim().toUpperCase()
  const name = editor.value.reasonSubTypeName.trim().toUpperCase()
  const payload = { reasonType: editor.value.reasonType, reasonSubTypeName: name, description: editor.value.description.trim() || null }
  saving.value = true
  try {
    if (mock.value) {
      const store = readStore()
      if (mode === 'edit') {
        const row = store.rows.find(item => item.reasonSubType === original?.reasonSubType)
        if (!row) throw new Error('資料已不存在，請重新查詢。')
        if (canEdit('reasonType')) { row.reasonType = payload.reasonType; row.reasonTypeName = lookupLabel(payload.reasonType) }
        if (canEdit('reasonSubTypeName')) row.reasonSubTypeName = payload.reasonSubTypeName
        if (canEdit('description')) row.description = payload.description
        row.reviser = currentUser.value; row.reviseDate = now(); row.dataStamp = (row.dataStamp ?? 0) + 1
      } else {
        store.rows.push({ ...payload, reasonSubType: key, creator: currentUser.value, createDate: createDate.value, issueState: 0, dataStamp: 0, reviser: null, reviseDate: null, reasonTypeName: lookupLabel(payload.reasonType) })
      }
      writeStore(store)
    } else if (mode === 'edit') {
      const row = original!
      const query = new URLSearchParams({ issueState: String(row.issueState) })
      if (row.dataStamp !== null) query.set('dataStamp', String(row.dataStamp))
      await request('api/qc/reason-subtypes/' + encodeURIComponent(row.reasonSubType) + '?' + query.toString(), { method: 'PUT', body: JSON.stringify(payload) })
    } else {
      await request('api/qc/reason-subtypes', { method: 'POST', body: JSON.stringify({ ...payload, reasonSubType: key }) })
    }
    await refresh(key)
    if (keepOpen && mode !== 'edit') editorNotice.value = '資料已套用；新增或複製前請輸入新的 ReasonSubType。'
    else if (keepOpen) { sourceRow.value = activeRow.value ? clone(activeRow.value) : null; editorNotice.value = '資料已套用。' }
    else closeEditor()
  } catch (error) {
    editorError.value = error instanceof Error ? error.message : '儲存品質原因子類別資料失敗。'
  } finally { saving.value = false }
}
function deleteQuery(row: ReasonSubTypeRow): string {
  const query = new URLSearchParams({ issueState: String(row.issueState) })
  if (row.dataStamp !== null) query.set('dataStamp', String(row.dataStamp))
  return query.toString()
}
async function removeSelected(): Promise<void> {
  const targets = [...selectedRows.value]
  if (!targets.length) { errorMessage.value = '請先選取要刪除的資料。'; return }
  if (issueState.value === 1 || targets.some(row => row.issueState === 1)) { errorMessage.value = '簽核中的資料不可刪除。'; return }
  const action = issueState.value === 2 ? '停用' : '刪除'
  if (!window.confirm(`確定${action}選取的 ${targets.length} 筆資料？`)) return
  busy.value = true; errorMessage.value = ''; notice.value = ''
  let completed = 0
  let failureMessage = ''
  try {
    for (const row of targets) {
      if (mock.value) {
        const store = readStore()
        if (row.issueState === 2) {
          const current = store.rows.find(item => item.reasonSubType === row.reasonSubType)!
          current.issueState = -1; current.reviser = currentUser.value; current.reviseDate = now(); current.dataStamp = (current.dataStamp ?? 0) + 1
        } else store.rows = store.rows.filter(item => item.reasonSubType !== row.reasonSubType)
        writeStore(store)
      } else {
        await request('api/qc/reason-subtypes/' + encodeURIComponent(row.reasonSubType) + '?' + deleteQuery(row), { method: 'DELETE' })
      }
      completed++
    }
    notice.value = `已完成 ${completed} 筆資料的刪除或停用。`
  } catch (error) {
    failureMessage = `${error instanceof Error ? error.message : '刪除資料失敗。'}${completed ? `（前 ${completed} 筆已完成）` : ''}`
  } finally {
    if (completed) await refresh()
    if (failureMessage) errorMessage.value = failureMessage
    busy.value = false
  }
}
async function approveSelected(): Promise<void> {
  const targets = [...selectedRows.value]
  if (!targets.length) { errorMessage.value = '請先選取要核准的資料。'; return }
  if (issueState.value !== 0 || targets.some(row => row.issueState !== 0)) { errorMessage.value = '只有未簽核資料可以送出核准。'; return }
  if (!window.confirm(`確定送出核准選取的 ${targets.length} 筆資料？`)) return
  busy.value = true; errorMessage.value = ''; notice.value = ''
  let completed = 0
  let failureMessage = ''
  try {
    for (const row of targets) {
      if (mock.value) {
        const store = readStore()
        const current = store.rows.find(item => item.reasonSubType === row.reasonSubType)!
        current.issueState = 2; current.reviser = currentUser.value; current.reviseDate = now(); current.dataStamp = (current.dataStamp ?? 0) + 1
        writeStore(store)
      } else {
        const query = new URLSearchParams({ issueState: String(row.issueState) })
        if (row.dataStamp !== null) query.set('dataStamp', String(row.dataStamp))
        await request('api/qc/reason-subtypes/' + encodeURIComponent(row.reasonSubType) + '/approve?' + query.toString(), { method: 'POST' })
      }
      completed++
    }
    notice.value = mock.value ? `已核准 ${completed} 筆預覽資料。` : `已送出 ${completed} 筆核准；狀態依 Issue Job 流程更新。`
  } catch (error) {
    failureMessage = `${error instanceof Error ? error.message : '核准資料失敗。'}${completed ? `（前 ${completed} 筆已完成）` : ''}`
  } finally {
    if (completed) await refresh()
    if (failureMessage) errorMessage.value = failureMessage
    busy.value = false
  }
}

onMounted(async () => { await refresh(); await loadReasonTypes() })
</script>

<template>
  <main class="reason-subtype-page">
    <header class="page-heading">
      <h1>品質原因子類別資料</h1>
      <span v-if="mock" class="dev-badge">DEV 假資料</span>
    </header>

    <section class="query-panel" aria-label="查詢條件">
      <div class="states" role="radiogroup" aria-label="簽核狀態">
        <label v-for="state in issueStates" :key="state.value" class="state-option">
          <input type="radio" name="reason-subtype-state" :value="state.value" :checked="issueState === state.value" @change="setIssueState(state.value)">
          <span>{{ state.label }}</span>
        </label>
      </div>
      <label class="search-label" for="reason-subtype-search">ReasonSubType</label>
      <input id="reason-subtype-search" v-model="searchInput" class="search-input" maxlength="50" @keydown.enter="query">
      <span class="wildcard">%</span>
      <button type="button" class="button" :disabled="loading" @click="query">查詢</button>
    </section>

    <section class="list-panel" aria-label="品質原因子類別清單">
      <div class="action-column">
        <button type="button" class="button primary" :disabled="busy" @click="openAdd">新增(A)</button>
        <button type="button" class="button" :disabled="busy || !activeRow" @click="openEdit">修改(E)</button>
        <button type="button" class="button" :disabled="busy || !selectedRows.length" @click="removeSelected">刪除(D)</button>
        <button type="button" class="button" :disabled="busy || !activeRow" @click="openCopy">複製(C)</button>
        <button type="button" class="button" :disabled="busy || !selectedRows.length" @click="approveSelected">核准</button>
        <button type="button" class="button close-button" @click="emit('close')">關閉(X)</button>
      </div>

      <div class="grid-wrap">
        <table class="data-grid">
          <thead><tr>
            <th class="select-col" aria-label="選取"></th><th>ReasonType</th><th>ReasonSubType</th><th>ReasonSubTypeName</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th>
          </tr></thead>
          <tbody>
            <tr v-for="row in visibleRows" :key="row.reasonSubType" :class="{ active: activeKey === row.reasonSubType }" @click="setActive(row)">
              <td class="select-col"><input type="checkbox" :checked="selectedKeys.includes(row.reasonSubType)" :aria-label="`選取 ${row.reasonSubType}`" @click.stop @change="toggleSelected(row)"></td>
              <td>{{ lookupLabel(row.reasonType, row.reasonTypeName) }}</td><td>{{ row.reasonSubType }}</td><td>{{ row.reasonSubTypeName }}</td><td class="description-cell">{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td>
            </tr>
            <tr v-if="!visibleRows.length"><td colspan="9" class="empty-cell">{{ loading ? '載入中…' : '查無資料' }}</td></tr>
          </tbody>
        </table>
      </div>
    </section>
    <footer class="list-footer">資料總筆數 {{ visibleRows.length }}</footer>
    <p v-if="errorMessage" class="message error" role="alert">{{ errorMessage }}</p>
    <p v-if="notice" class="message notice" role="status">{{ notice }}</p>

    <div v-if="editorMode" class="backdrop" @click.self="closeEditor">
      <section class="dialog" role="dialog" aria-modal="true" :aria-label="editorTitle">
        <header class="titlebar"><h2>{{ editorTitle }}</h2><button class="x" type="button" :disabled="saving" aria-label="關閉" @click="closeEditor">×</button></header>
        <form class="editor-form" @submit.prevent="save(false)">
          <fieldset class="basis-fields" :disabled="saving">
            <legend>品質原因子類別基本資料</legend>
            <label class="field"><span>Reason Type</span><select v-model.number="editor.reasonType" :disabled="saving || !canEdit('reasonType')" required><option v-for="option in reasonTypes" :key="option.value" :value="option.value">{{ option.label }}</option></select></label>
            <label class="field"><span>ReasonSubType</span><input v-model="editor.reasonSubType" maxlength="50" :readonly="editorMode === 'edit' || !canEdit('reasonSubTypeName')" required @blur="editor.reasonSubType = editor.reasonSubType.toUpperCase()"></label>
            <label class="field"><span>ReasonSubType Name</span><input v-model="editor.reasonSubTypeName" maxlength="100" :readonly="!canEdit('reasonSubTypeName')" required @blur="editor.reasonSubTypeName = editor.reasonSubTypeName.toUpperCase()"></label>
            <label class="field description-field"><span>Description</span><textarea v-model="editor.description" maxlength="255" :readonly="!canEdit('description')" rows="4"></textarea></label>
          </fieldset>
          <fieldset class="audit-fields" disabled>
            <legend>建立／修改資訊</legend>
            <label><span>建立人</span><input :value="editorMode === 'edit' ? sourceRow?.creator ?? '' : currentUser" readonly></label>
            <label><span>建立日期</span><input :value="formatDate(editorMode === 'edit' ? sourceRow?.createDate ?? null : createDate)" readonly></label>
            <label><span>修改人</span><input :value="editorMode === 'edit' ? sourceRow?.reviser ?? '' : ''" readonly></label>
            <label><span>修改日期</span><input :value="editorMode === 'edit' ? formatDate(sourceRow?.reviseDate ?? null) : ''" readonly></label>
          </fieldset>
          <p v-if="editorNotice" class="message notice" role="status">{{ editorNotice }}</p>
          <p v-if="editorError" class="message error" role="alert">{{ editorError }}</p>
          <footer class="dialog-actions"><button type="submit" class="button primary" :disabled="!canSave">確定</button><button type="button" class="button" :disabled="saving" @click="closeEditor">關閉</button><button type="button" class="button" :disabled="!canSave" @click="save(true)">套用</button></footer>
        </form>
      </section>
    </div>
  </main>
</template>

<style scoped>
.reason-subtype-page{--line:#c8c4bb;--text:#30302e;--muted:#77746e;--field:#fff0ad;--orange:#f7a32b;display:flex;flex-direction:column;gap:7px;height:100%;min-height:0;padding:10px;color:var(--text);background:#f7f5ef;font:14px/1.35 "Segoe UI","Microsoft JhengHei",sans-serif}
.page-heading{display:flex;align-items:center;justify-content:space-between;min-height:38px}.page-heading h1{margin:0;font-size:20px;font-weight:600}.dev-badge{padding:4px 9px;border:1px solid #d7a044;background:#fff1bd;color:#77551a;font-size:12px}
.query-panel{display:flex;flex-wrap:wrap;align-items:center;gap:9px;min-height:42px;padding:5px 9px;border:1px solid var(--line);background:#fff}.states{display:flex;flex-wrap:wrap;gap:11px}.state-option{display:inline-flex;align-items:center;gap:4px;white-space:nowrap;cursor:pointer}.state-option input{accent-color:#e18b17}.search-label{margin-left:5px;white-space:nowrap}.search-input{width:190px}.wildcard{margin-left:-5px}
input,textarea,select{box-sizing:border-box;min-height:29px;padding:4px 7px;border:1px solid #a9a69f;border-radius:2px;background:var(--field);color:var(--text);font:inherit}input:focus,textarea:focus,select:focus{border-color:#d18a21;outline:2px solid rgb(209 138 33 / 20%)}input[readonly],input:disabled,textarea[readonly],select:disabled{background:#ecebe7;color:#777}textarea{resize:vertical}.button{min-height:30px;padding:4px 10px;border:1px solid #bdb9b0;border-radius:3px;background:linear-gradient(#fff,#f0efec);color:#343331;font:inherit;white-space:nowrap;cursor:pointer}.button:hover:not(:disabled){border-color:#d08a23;background:#fff6df}.button:disabled{opacity:.48;cursor:not-allowed}.button.primary{border-color:#df8612;background:var(--orange);color:#fff}
.list-panel{display:grid;grid-template-columns:minmax(0,1fr) 88px;gap:8px;flex:1;min-height:0;padding:7px;border:1px solid var(--line);background:#fff}.grid-wrap{min-width:0;min-height:0;overflow:auto;border:1px solid #bcb8af}.data-grid{width:100%;border-collapse:collapse;white-space:nowrap;font-size:13px}.data-grid th{position:sticky;top:0;z-index:1;padding:6px 8px;border:1px solid #c2bfb9;background:#eeeae0;text-align:left;font-weight:600}.data-grid td{height:29px;padding:3px 8px;border:1px solid #d1cec8;background:#fff}.data-grid tbody tr{cursor:pointer}.data-grid tbody tr:hover td{background:#fff7dd}.data-grid tbody tr.active td{background:#ffedb1}.data-grid input[type=checkbox]{min-height:auto;accent-color:#dc8e1c}.data-grid .select-col{width:28px;text-align:center}.data-grid .description-cell{min-width:180px;max-width:300px;overflow:hidden;text-overflow:ellipsis}.empty-cell{text-align:center;color:var(--muted)}.action-column{display:flex;flex-direction:column;gap:8px}.action-column .button{width:100%}.close-button{margin-top:auto}.list-footer{min-height:19px;padding:0 2px;color:var(--muted);font-size:12px}
.message{margin:0;padding:7px 10px;border:1px solid}.error{border-color:#d99b86;background:#fff0e9;color:#873820}.notice{border-color:#c9bc91;background:#fff8dc;color:#67551d}.backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:20px;background:rgb(25 24 22 / 28%)}.dialog{display:flex;flex-direction:column;width:min(560px,100%);max-height:calc(100vh - 40px);overflow:auto;border:1px solid #a8a49c;background:#f8f7f3;box-shadow:0 8px 28px rgb(0 0 0 / 22%)}.titlebar{display:flex;align-items:center;justify-content:space-between;padding:10px 14px;border-bottom:1px solid var(--line);background:#f0eee8}.titlebar h2{margin:0;font-size:17px;font-weight:600}.x{border:0;background:transparent;font-size:24px;line-height:1;cursor:pointer}.editor-form{display:flex;flex-direction:column;gap:10px;padding:12px}.basis-fields{display:grid;grid-template-columns:140px minmax(0,1fr);gap:9px 10px;margin:0;padding:10px;border:1px solid var(--line)}.basis-fields legend,.audit-fields legend{padding:0 5px;color:#57544e}.field{display:contents}.field>span{text-align:right;padding-top:6px}.field select,.field input,.field textarea{width:100%}.description-field textarea{min-height:74px}.audit-fields{display:grid;grid-template-columns:1fr 1fr;gap:8px 12px;margin:0;padding:9px;border:1px solid var(--line)}.audit-fields label{display:grid;grid-template-columns:72px minmax(0,1fr);align-items:center;gap:6px}.audit-fields input{width:100%;background:#ecebe7}.dialog-actions{display:flex;justify-content:flex-end;gap:8px;padding-top:4px}.dialog-actions .primary{min-width:94px}
@media(max-width:760px){.reason-subtype-page{padding:7px}.query-panel{align-items:flex-start}.states{width:100%}.list-panel{grid-template-columns:1fr}.action-column{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));grid-row:2}.close-button{margin-top:0}.basis-fields{grid-template-columns:1fr}.field{display:grid;gap:3px}.field>span{text-align:left}.audit-fields{grid-template-columns:1fr}}
</style>
