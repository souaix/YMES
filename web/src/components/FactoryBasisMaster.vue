<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

interface FactoryRow {
  factoryNo: string
  description: string | null
  creator: string | null
  createDate: string | null
  issueState: number
  fabCode: string | null
  factoryName: string | null
  reviser: string | null
  reviseDate: string | null
  dataStamp: number | null
}
interface FactoryForm {
  factoryNo: string
  factoryName: string
  fabCode: string
  description: string
}
type FormMode = 'add' | 'edit' | 'copy' | null

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mockMode = computed(() => import.meta.env.DEV && props.developmentPreview)
const rowsKey = 'ymes-dev-mock:smd-factory-basis:v1'
const issueStateFilter = ref(0)
const queryFactoryNo = ref('')
const appliedFactoryNo = ref('')
const allRows = ref<FactoryRow[]>([])
const selectedKeys = ref<string[]>([])
const activeKey = ref('')
const anchorKey = ref('')
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const formError = ref('')
const formMode = ref<FormMode>(null)
const form = ref<FactoryForm>({ factoryNo: '', factoryName: '', fabCode: '', description: '' })
const formBaseline = ref<FactoryForm | null>(null)
const formDirty = computed(() => !!formBaseline.value && JSON.stringify(form.value) !== JSON.stringify(formBaseline.value))
const dataStamp = ref<number | null>(null)
const audit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const factoryNameEnabled = ref(false)
const activeRow = computed(() => visibleRows.value.find(row => row.factoryNo === activeKey.value) ?? null)
const visibleRows = computed(() => {
  const query = appliedFactoryNo.value.trim().toLocaleLowerCase()
  return allRows.value
    .filter(row => row.issueState === issueStateFilter.value)
    .filter(row => !query || (row.factoryNo ?? '').toLocaleLowerCase().includes(query))
    .sort((a, b) => a.factoryNo.localeCompare(b.factoryNo))
})
const selectedRows = computed(() => visibleRows.value.filter(row => selectedKeys.value.includes(row.factoryNo)))
const seedRows: FactoryRow[] = [
  { factoryNo: 'DEV-FAB', description: 'Development factory sample', creator: 'DEV001', createDate: '2026-11-10T08:00:00+08:00', issueState: 0, fabCode: 'DEV', factoryName: '開發測試廠', reviser: 'DEV002', reviseDate: '2026-11-11T09:20:00+08:00', dataStamp: 2 },
  { factoryNo: 'DEV-FAB-P', description: '簽核中假資料', creator: 'DEV001', createDate: '2026-11-12T10:15:00+08:00', issueState: 1, fabCode: 'DEV-P', factoryName: '簽核中工廠', reviser: null, reviseDate: null, dataStamp: 1 },
  { factoryNo: 'DEV-FAB-A', description: '已簽核假資料', creator: 'DEV001', createDate: '2026-11-13T10:15:00+08:00', issueState: 2, fabCode: 'DEV-A', factoryName: '已簽核工廠', reviser: 'DEV002', reviseDate: '2026-11-14T09:00:00+08:00', dataStamp: 3 },
  { factoryNo: 'DEV-FAB-X', description: '未使用假資料', creator: 'DEV001', createDate: '2026-11-14T10:15:00+08:00', issueState: -1, fabCode: 'DEV-X', factoryName: '', reviser: 'DEV002', reviseDate: '2026-11-15T09:00:00+08:00', dataStamp: 1 },
]
const issueStates = [
  { value: 0, label: '未簽核' },
  { value: 1, label: '簽核中' },
  { value: 2, label: '已簽核' },
  { value: -1, label: '不使用' },
]

function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function requestJson<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init })
  if (response.status === 401) { window.location.assign(appBase + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    if (response.status === 404) throw new Error('工廠基本資料 API 尚未建立；請依 todolist_homepc.md 完成 SMD SD101 API。')
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 回應錯誤 (${response.status})。`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function readRows(): FactoryRow[] {
  try { const value = window.localStorage.getItem(rowsKey); return value ? JSON.parse(value) as FactoryRow[] : structuredClone(seedRows) }
  catch { return structuredClone(seedRows) }
}
function saveRows(value: FactoryRow[]): void { window.localStorage.setItem(rowsKey, JSON.stringify(value)) }
function formatDate(value: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat('zh-TW', { dateStyle: 'short', timeStyle: 'medium', hour12: false }).format(date)
}
function rowKey(row: Pick<FactoryRow, 'factoryNo'>): string { return row.factoryNo }

async function loadRows(): Promise<void> {
  loading.value = true; errorMessage.value = ''; successMessage.value = ''
  try {
    if (mockMode.value) {
      allRows.value = readRows()
      if (!window.localStorage.getItem(rowsKey)) saveRows(allRows.value)
      factoryNameEnabled.value = true
    } else {
      const [list, options] = await Promise.all([
        requestJson<FactoryRow[]>(`api/smd/factory-basis?factoryNo=${encodeURIComponent(queryFactoryNo.value.trim())}`),
        requestJson<{ factoryNameEnabled: boolean }>('api/smd/factory-basis/options'),
      ])
      allRows.value = list
      factoryNameEnabled.value = options.factoryNameEnabled
    }
    appliedFactoryNo.value = queryFactoryNo.value
    const visibleKeys = new Set(visibleRows.value.map(rowKey))
    selectedKeys.value = selectedKeys.value.filter(key => visibleKeys.has(key))
    if (!visibleKeys.has(activeKey.value)) { activeKey.value = ''; anchorKey.value = '' }
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '工廠基本資料載入失敗。' }
  finally { loading.value = false }
}
function applyIssueFilter(value: number): void {
  issueStateFilter.value = value
  const visibleKeys = new Set(visibleRows.value.map(rowKey))
  selectedKeys.value = selectedKeys.value.filter(key => visibleKeys.has(key))
  if (!visibleKeys.has(activeKey.value)) { activeKey.value = ''; anchorKey.value = '' }
}
function selectRow(row: FactoryRow, event: MouseEvent): void {
  const key = rowKey(row); const index = visibleRows.value.findIndex(item => rowKey(item) === key)
  if (event.shiftKey && anchorKey.value) {
    const anchor = visibleRows.value.findIndex(item => rowKey(item) === anchorKey.value)
    selectedKeys.value = anchor < 0 ? [key] : visibleRows.value.slice(Math.min(anchor, index), Math.max(anchor, index) + 1).map(rowKey)
  } else if (event.ctrlKey || event.metaKey) {
    selectedKeys.value = selectedKeys.value.includes(key) ? selectedKeys.value.filter(item => item !== key) : [...selectedKeys.value, key]
    anchorKey.value = key
  } else { selectedKeys.value = [key]; anchorKey.value = key }
  activeKey.value = key
}
function openForm(mode: Exclude<FormMode, null>): void {
  const source = activeRow.value
  if (mode !== 'add' && !source) { errorMessage.value = '請先選取一筆資料。'; return }
  if (mode === 'edit' && source?.issueState !== 0) { errorMessage.value = '只有未簽核資料可以修改。'; return }
  errorMessage.value = ''; successMessage.value = ''; formError.value = ''; formMode.value = mode
  form.value = mode === 'add' ? { factoryNo: '', factoryName: '', fabCode: '', description: '' } : {
    factoryNo: source?.factoryNo ?? '', factoryName: source?.factoryName ?? '', fabCode: source?.fabCode ?? '', description: source?.description ?? '',
  }
  formBaseline.value = structuredClone(form.value)
  dataStamp.value = mode === 'edit' ? source?.dataStamp ?? 0 : null
  const now = new Date().toISOString()
  audit.value = mode === 'edit' && source
    ? { creator: source.creator ?? '', createDate: formatDate(source.createDate), reviser: source.reviser ?? '', reviseDate: formatDate(source.reviseDate) }
    : { creator: props.currentUserNo || 'DEV001', createDate: formatDate(now), reviser: '', reviseDate: '' }
}
function validateForm(): string {
  if (!form.value.factoryNo.trim()) return 'FactoryNo 為必填。'
  if (form.value.factoryNo.trim().length > 20) return 'FactoryNo 最多 20 個字元。'
  if (form.value.factoryName.length > 50) return 'FactoryName 最多 50 個字元。'
  if (form.value.fabCode.length > 10) return 'FabCode 最多 10 個字元。'
  if (form.value.description.length > 255) return 'Description 最多 255 個字元。'
  const duplicate = allRows.value.some(row => row.factoryNo.toLocaleLowerCase() === form.value.factoryNo.trim().toLocaleLowerCase() && !(formMode.value === 'edit' && row.factoryNo === activeKey.value))
  if (duplicate) return 'FactoryNo 已存在。'
  return ''
}
async function saveForm(closeAfterSave: boolean): Promise<void> {
  if (!formMode.value || busy.value) return
  const validation = validateForm(); if (validation) { formError.value = validation; return }
  busy.value = true; formError.value = ''; errorMessage.value = ''
  const mode = formMode.value; const now = new Date().toISOString()
  const existing = allRows.value.find(row => row.factoryNo === activeKey.value)
  const body = {
    factoryNo: form.value.factoryNo.trim().toUpperCase(),
    factoryName: form.value.factoryName.trim() || (mode === 'edit' ? existing?.factoryName ?? null : null),
    fabCode: form.value.fabCode.trim().toUpperCase() || null,
    description: form.value.description || null,
  }
  try {
    let saved: FactoryRow
    if (mockMode.value) {
      const list = readRows(); const source = list.find(row => row.factoryNo === activeKey.value)
      if (mode === 'edit') {
        if (!source) throw new Error('資料已不存在，請重新查詢。')
        if ((source.dataStamp ?? 0) !== (dataStamp.value ?? 0)) throw new Error('資料已被其他人修改，請重新查詢後再操作。')
        saved = { ...source, ...body, reviser: props.currentUserNo || 'DEV001', reviseDate: now, dataStamp: (source.dataStamp ?? 0) + 1 }
      } else {
        if (list.some(row => row.factoryNo.toLocaleLowerCase() === body.factoryNo.toLocaleLowerCase())) throw new Error('FactoryNo 已存在。')
        saved = { ...body, creator: props.currentUserNo || 'DEV001', createDate: now, issueState: 0, reviser: null, reviseDate: null, dataStamp: 0 }
      }
      const next = mode === 'edit' ? list.map(row => row.factoryNo === source?.factoryNo ? saved : row) : [...list, saved]
      saveRows(next); allRows.value = next
    } else if (mode === 'edit') {
      saved = await requestJson<FactoryRow>(`api/smd/factory-basis/${encodeURIComponent(body.factoryNo)}?issueState=0&dataStamp=${encodeURIComponent(String(dataStamp.value ?? 0))}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
      allRows.value = allRows.value.map(row => row.factoryNo === saved.factoryNo ? saved : row)
    } else {
      saved = await requestJson<FactoryRow>('api/smd/factory-basis', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
      allRows.value = [...allRows.value, saved]
    }
    selectedKeys.value = [saved.factoryNo]; activeKey.value = saved.factoryNo; anchorKey.value = saved.factoryNo
    audit.value = { creator: saved.creator ?? '', createDate: formatDate(saved.createDate), reviser: saved.reviser ?? '', reviseDate: formatDate(saved.reviseDate) }
    dataStamp.value = saved.dataStamp ?? 0
    form.value.factoryNo = saved.factoryNo
    if (mode !== 'edit') { formMode.value = 'edit'; issueStateFilter.value = 0 }
    formBaseline.value = structuredClone(form.value)
    if (closeAfterSave) formMode.value = null
    await loadRows()
    if (!closeAfterSave) successMessage.value = '資料已儲存。'
  } catch (error) { formError.value = error instanceof Error ? error.message : '工廠資料儲存失敗。' }
  finally { busy.value = false }
}
async function deleteSelected(): Promise<void> {
  const targets = [...selectedRows.value]
  if (!targets.length || busy.value) { if (!targets.length) errorMessage.value = '請先選取要刪除的資料。'; return }
  if (!window.confirm(`確定刪除選取的 ${targets.length} 筆工廠資料？`)) return
  busy.value = true; errorMessage.value = ''; successMessage.value = ''
  let completed = 0
  try {
    for (const row of targets) {
      if (![0, 2, -1].includes(row.issueState)) throw new Error(`${row.factoryNo} 正在簽核中，不允許刪除。`)
      if (mockMode.value) {
        const next = readRows().filter(item => item.factoryNo !== row.factoryNo); saveRows(next); allRows.value = next
      } else await requestJson<void>(`api/smd/factory-basis/${encodeURIComponent(row.factoryNo)}?issueState=${row.issueState}&dataStamp=${encodeURIComponent(String(row.dataStamp ?? 0))}`, { method: 'DELETE' })
      completed++
    }
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '刪除工廠資料失敗。' }
  finally {
    busy.value = false
    if (completed) { selectedKeys.value = []; activeKey.value = ''; anchorKey.value = ''; await loadRows(); if (!errorMessage.value) successMessage.value = `已刪除 ${completed} 筆資料。` }
  }
}
async function approveSelected(): Promise<void> {
  const targets = [...selectedRows.value]
  if (!targets.length || busy.value) { if (!targets.length) errorMessage.value = '請先選取要核准的資料。'; return }
  if (!window.confirm(`確定核准選取的 ${targets.length} 筆工廠資料？`)) return
  busy.value = true; errorMessage.value = ''; successMessage.value = ''
  let completed = 0
  try {
    for (const row of targets) {
      if (row.issueState !== 0) throw new Error(`${row.factoryNo} 不是未簽核狀態，停止後續核准。`)
      if (mockMode.value) {
        const next = readRows().map(item => item.factoryNo === row.factoryNo ? { ...item, issueState: 2, reviser: props.currentUserNo || 'DEV001', reviseDate: new Date().toISOString(), dataStamp: (item.dataStamp ?? 0) + 1 } : item)
        saveRows(next); allRows.value = next
      } else {
        await requestJson<FactoryRow>(`api/smd/factory-basis/${encodeURIComponent(row.factoryNo)}/approve`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ issueState: 0, dataStamp: row.dataStamp ?? 0 }) })
      }
      completed++
    }
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '工廠資料核准失敗。' }
  finally {
    busy.value = false
    if (completed) { selectedKeys.value = []; activeKey.value = ''; anchorKey.value = ''; await loadRows(); if (!errorMessage.value) successMessage.value = `已送出 ${completed} 筆核准。` }
  }
}

onMounted(() => { void loadRows() })
</script>

<template>
  <section class="factory-master">
    <header class="page-heading"><h1>工廠基本資料</h1></header>
    <div class="query-strip">
      <fieldset class="issue-filter"><legend>狀態</legend><label v-for="state in issueStates" :key="state.value"><input type="radio" name="factory-issue-state" :checked="issueStateFilter === state.value" @change="applyIssueFilter(state.value)" />{{ state.label }}</label></fieldset>
      <label for="factory-query">工廠編號</label><input id="factory-query" v-model="queryFactoryNo" @keyup.enter="loadRows" /><span class="wildcard">%</span><button class="btn" type="button" :disabled="loading" @click="loadRows">查詢</button><span v-if="loading" class="muted">載入中…</span>
    </div>
    <div class="work-area">
      <div class="grid-panel">
        <div class="table-scroll"><table class="data-grid"><thead><tr><th class="row-selector"></th><th class="factory-no">工廠編號</th><th v-if="factoryNameEnabled" class="factory-name">工廠名稱</th><th class="fab-code">FabCode</th><th class="description">說明</th><th class="audit-user">建立人</th><th class="audit-date">建立日期</th><th class="audit-user">修改人</th><th class="audit-date">修改日期</th></tr></thead><tbody>
          <tr v-for="row in visibleRows" :key="row.factoryNo" :class="{ active: row.factoryNo === activeKey, selected: selectedKeys.includes(row.factoryNo) }" @click="selectRow(row, $event)" @dblclick="row.issueState === 0 && openForm('edit')"><td class="row-selector">{{ row.factoryNo === activeKey ? '▶' : '' }}</td><td>{{ row.factoryNo }}</td><td v-if="factoryNameEnabled">{{ row.factoryName ?? '' }}</td><td>{{ row.fabCode ?? '' }}</td><td class="description-cell">{{ row.description ?? '' }}</td><td>{{ row.creator ?? '' }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser ?? '' }}</td><td>{{ formatDate(row.reviseDate) }}</td></tr>
          <tr v-if="!visibleRows.length"><td class="empty" :colspan="factoryNameEnabled ? 9 : 8">{{ loading ? '載入中…' : '沒有符合條件的資料' }}</td></tr>
        </tbody></table></div><footer class="grid-footer">資料總筆數 {{ visibleRows.length }}</footer>
      </div>
      <aside class="side-actions">
        <button class="btn btn-primary" type="button" :disabled="busy" @click="openForm('add')">新增(A)</button>
        <button class="btn" type="button" :disabled="busy || !activeRow || activeRow.issueState !== 0" @click="openForm('edit')">修改(E)</button>
        <button class="btn" type="button" :disabled="busy || !selectedKeys.length" @click="deleteSelected">刪除(D)</button>
        <button class="btn" type="button" :disabled="busy || !activeRow" @click="openForm('copy')">複製(C)</button>
        <button class="btn" type="button" :disabled="busy || !selectedKeys.length" @click="approveSelected">核准</button>
        <button class="btn close-btn" type="button" @click="emit('close')">關閉</button>
      </aside>
    </div>
    <p v-if="errorMessage" class="notice error" role="alert">{{ errorMessage }}</p><p v-if="successMessage" class="notice success" role="status">{{ successMessage }}</p><p v-if="mockMode" class="preview-note">Development 假資料預覽；資料只保存在此瀏覽器。</p>

    <div v-if="formMode" class="modal-backdrop" @click.self="formMode = null"><section class="editor-modal" role="dialog" aria-modal="true" aria-label="工廠基本資料定義"><header class="modal-heading"><h2>工廠基本資料（{{ formMode === 'edit' ? '修改' : formMode === 'copy' ? '複製' : '新增' }}）</h2><button class="icon-close" type="button" aria-label="關閉" @click="formMode = null">×</button></header>
      <fieldset class="editor-fields"><legend>工廠主檔資料</legend>
        <div class="field-row"><label for="factory-no">工廠編號</label><input id="factory-no" v-model.trim="form.factoryNo" maxlength="20" :readonly="formMode === 'edit'" class="kd-input" @blur="form.factoryNo = form.factoryNo.toUpperCase()" /></div>
        <div class="field-row"><label for="factory-name">工廠名稱</label><input id="factory-name" v-model="form.factoryName" maxlength="50" class="kd-input" /></div>
        <div class="field-row"><label for="factory-fab-code">FabCode</label><input id="factory-fab-code" v-model="form.fabCode" maxlength="10" class="kd-input" @blur="form.fabCode = form.fabCode.toUpperCase()" /></div>
        <div class="field-row description-row"><label for="factory-description">說明</label><textarea id="factory-description" v-model="form.description" maxlength="255" class="kd-input" /></div>
      </fieldset>
      <fieldset class="audit-fields"><legend>建立／修改資訊</legend><label>建立人<input :value="audit.creator" readonly /></label><label>建立日期<input :value="audit.createDate" readonly /></label><label>修改人<input :value="audit.reviser" readonly /></label><label>修改日期<input :value="audit.reviseDate" readonly /></label></fieldset>
      <p v-if="formError" class="notice error form-error" role="alert">{{ formError }}</p><footer class="modal-actions"><button class="btn btn-primary" :disabled="busy" type="button" @click="saveForm(true)">確定</button><button class="btn" :disabled="busy" type="button" @click="formMode = null">關閉</button><button class="btn" :disabled="busy || !formDirty" type="button" @click="saveForm(false)">套用</button></footer>
    </section></div>
  </section>
</template>

<style scoped>
.factory-master{height:100%;min-height:0;display:flex;flex-direction:column;background:#fff;color:#333;font-size:14px}.page-heading{padding:8px 14px;border-bottom:1px solid #d5d5d5;background:#f5f3ed}.page-heading h1{margin:0;font-size:20px;font-weight:500}.query-strip{min-height:48px;display:flex;align-items:center;gap:8px;padding:5px 12px;border-bottom:1px solid #d4d4d4}.issue-filter{display:flex;align-items:center;gap:12px;margin:0 8px 0 0;padding:3px 8px;border:0}.issue-filter legend{position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0)}.issue-filter label{display:flex;align-items:center;gap:4px;white-space:nowrap}.issue-filter input{accent-color:#d59a27}.query-strip>label{white-space:nowrap}.query-strip>input{height:28px;width:190px;border:1px solid #999;padding:3px 6px}.wildcard{margin-left:-6px;color:#555}.muted,.preview-note{color:#777}.btn{min-height:30px;padding:4px 11px;border:1px solid #b8b8b8;border-radius:3px;background:linear-gradient(#fff,#f1f1f1);color:#333;white-space:nowrap}.btn:hover:not(:disabled){border-color:#888;background:#eee}.btn:disabled{opacity:.52}.btn-primary{border-color:#ed9c21;background:#ffa928;color:#fff}.work-area{display:flex;flex:1;min-height:0;gap:10px;padding:7px}.grid-panel{display:flex;flex:1;min-width:0;min-height:0;flex-direction:column;border:1px solid #c7c7c7}.table-scroll{flex:1;min-height:0;overflow:auto}.data-grid{width:max-content;min-width:100%;border-collapse:collapse;table-layout:fixed;white-space:nowrap}.data-grid th{position:sticky;top:0;z-index:1;background:#48a3df;color:#fff;font-weight:500;text-align:left}.data-grid th,.data-grid td{height:29px;padding:4px 7px;border:1px solid #d2d2d2}.data-grid tbody tr{cursor:pointer}.data-grid tbody tr:hover{background:#fff4ca}.data-grid tbody tr.active{background:#ffe699}.data-grid tbody tr.selected:not(.active){background:#fff1c3}.row-selector{width:26px;text-align:center}.factory-no{width:120px}.factory-name{width:160px}.fab-code{width:90px}.description{width:200px}.description-cell{max-width:200px;overflow:hidden;text-overflow:ellipsis}.audit-user{width:80px}.audit-date{width:150px}.empty{text-align:center;color:#777;padding:18px!important}.grid-footer{min-height:25px;padding:4px 8px;color:#666;border-top:1px solid #ccc}.side-actions{width:88px;display:flex;flex-direction:column;gap:6px;padding-top:0}.side-actions .btn{width:100%}.close-btn{margin-top:auto}.notice{margin:4px 12px;padding:7px 10px;border-radius:3px}.error{color:#9b1c1c;background:#fff1f0;border:1px solid #e6b3ae}.success{color:#32633a;background:#f0f8ef;border:1px solid #b9d6b7}.preview-note{margin:2px 14px 7px;font-size:12px}.modal-backdrop{position:fixed;inset:0;z-index:40;display:flex;align-items:center;justify-content:center;padding:20px;background:rgba(0,0,0,.28)}.editor-modal{width:min(485px,96vw);max-height:94vh;overflow:auto;border:1px solid #aaa;border-radius:4px;background:#fff;box-shadow:0 5px 24px #0004}.modal-heading{display:flex;align-items:center;justify-content:space-between;padding:9px 13px;border-bottom:1px solid #ccc;background:#f5f3ed}.modal-heading h2{margin:0;font-size:17px;font-weight:500}.icon-close{border:0;background:transparent;font-size:25px;line-height:1;color:#555}.editor-fields{margin:10px 12px 6px;padding:9px 11px 11px;border:1px solid #bbb}.editor-fields legend,.audit-fields legend{padding:0 5px;color:#555}.field-row{display:grid;grid-template-columns:105px minmax(120px,1fr);align-items:center;gap:8px;min-height:35px}.field-row>label{text-align:right}.kd-input{height:27px;width:100%;border:1px solid #999;border-radius:0;background:#fff0ad;padding:3px 6px;color:#222;font:inherit}.kd-input:read-only{background:#eee}.description-row{align-items:start;padding-top:5px}.description-row>label{padding-top:5px}.description-row textarea{height:76px;resize:vertical}.audit-fields{display:grid;grid-template-columns:1fr 1fr;gap:6px 10px;margin:7px 12px;padding:8px 10px;border:1px solid #bbb}.audit-fields label{display:grid;grid-template-columns:65px minmax(0,1fr);align-items:center;gap:5px;white-space:nowrap;font-size:12px}.audit-fields input{min-width:0;width:100%;height:25px;border:1px solid #bbb;background:#eee;padding:3px 5px}.form-error{margin:0 12px}.modal-actions{display:flex;justify-content:flex-end;gap:7px;padding:9px 12px 11px}@media(max-width:850px){.query-strip{flex-wrap:wrap}.issue-filter{gap:5px;margin-right:0}.work-area{gap:6px;padding:5px}.side-actions{width:76px}.audit-fields{grid-template-columns:1fr}.field-row{grid-template-columns:88px minmax(100px,1fr)}}
</style>
<style scoped>
.editor-modal{height:min(347px,94vh);display:flex;flex-direction:column}.modal-heading{min-height:29px;padding:4px 12px}.editor-fields{box-sizing:border-box;flex:none;height:175px;margin:12px 12px 8px;padding:7px 9px 6px}.field-row{grid-template-columns:76px minmax(0,1fr);min-height:28px;gap:8px}.field-row:not(.description-row) .kd-input{max-width:182px}.description-row{height:62px;padding-top:0}.description-row>label{padding-top:5px}.description-row textarea{width:366px;max-width:100%;height:57px}.kd-input{background:#ffffc0}.audit-fields{box-sizing:border-box;flex:none;height:70px;margin:0 12px;padding:5px 8px}.modal-actions{box-sizing:border-box;min-height:45px;margin-top:auto;padding:7px 12px 8px}.description-cell{white-space:pre-wrap;overflow-wrap:anywhere;text-overflow:clip}
</style>
