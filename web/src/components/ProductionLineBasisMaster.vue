<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

interface ProductionLineRow {
  pdLineNo: string
  pdLineName: string
  pdLineStatus: number
  issueState: number
  description: string | null
  creator: string | null
  createDate: string | null
  reviser: string | null
  reviseDate: string | null
  dataStamp: number | string | null
}
interface ProductionLineForm {
  pdLineNo: string
  pdLineName: string
  pdLineStatus: number | null
  description: string
}
interface ValueOption { value: number; label: string }
type FormMode = 'add' | 'edit' | 'copy' | null

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mockMode = computed(() => import.meta.env.DEV && props.developmentPreview)
const rowsKey = 'ymes-dev-mock:smd-pd-line-basis:v1'
const issueStateFilter = ref(0)
const queryPdLineNo = ref('')
const appliedPdLineNo = ref('')
const allRows = ref<ProductionLineRow[]>([])
const statusOptions = ref<ValueOption[]>([
  { value: 0, label: '生產中（DEV）' },
  { value: 1, label: '停線（DEV）' },
  { value: 2, label: '換線（DEV）' },
])
const selectedKeys = ref<string[]>([])
const activeKey = ref('')
const anchorKey = ref('')
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const formError = ref('')
const formMode = ref<FormMode>(null)
const form = ref<ProductionLineForm>({ pdLineNo: '', pdLineName: '', pdLineStatus: 1, description: '' })
const formBaseline = ref<ProductionLineForm | null>(null)
const formDirty = computed(() => !!formBaseline.value && JSON.stringify(form.value) !== JSON.stringify(formBaseline.value))
const formDataStamp = ref<number>(0)
const audit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const issueStates = [
  { value: 0, label: '未簽核' },
  { value: 1, label: '簽核中' },
  { value: 2, label: '已簽核' },
  { value: -1, label: '不使用' },
]
const seedRows: ProductionLineRow[] = [
  { pdLineNo: 'LINE-DEV-01', pdLineName: '組裝線 A（DEV）', pdLineStatus: 0, issueState: 0, description: '本機假資料：未簽核', creator: 'DEV001', createDate: '2026-11-10T08:00:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-11T09:20:00+08:00', dataStamp: 2 },
  { pdLineNo: 'LINE-DEV-02', pdLineName: '包裝線 B（DEV）', pdLineStatus: 1, issueState: 1, description: '本機假資料：簽核中', creator: 'DEV001', createDate: '2026-11-12T10:15:00+08:00', reviser: null, reviseDate: null, dataStamp: 1 },
  { pdLineNo: 'LINE-DEV-03', pdLineName: '測試線 C（DEV）', pdLineStatus: 2, issueState: 2, description: '本機假資料：已簽核', creator: 'DEV001', createDate: '2026-11-13T10:15:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-14T09:00:00+08:00', dataStamp: 3 },
  { pdLineNo: 'LINE-OLD-01', pdLineName: '舊生產線（DEV）', pdLineStatus: 1, issueState: -1, description: '本機假資料：不使用', creator: 'DEV001', createDate: '2026-11-14T10:15:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-15T09:00:00+08:00', dataStamp: 1 },
]

const visibleRows = computed(() => {
  const query = appliedPdLineNo.value.trim().toLocaleUpperCase()
  return allRows.value
    .filter(row => Number(row.issueState) === issueStateFilter.value)
    .filter(row => !query || row.pdLineNo.toLocaleUpperCase().includes(query))
    .sort((a, b) => a.pdLineNo.localeCompare(b.pdLineNo))
})
const selectedRows = computed(() => visibleRows.value.filter(row => selectedKeys.value.includes(row.pdLineNo)))
const activeRow = computed(() => visibleRows.value.find(row => row.pdLineNo === activeKey.value) ?? null)

function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function requestJson<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init })
  if (response.status === 401) {
    window.location.assign(appBase + '/')
    throw new Error('登入狀態已逾期，請重新登入。')
  }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    if (response.status === 404) throw new Error('生產線別基本資料 API 尚未建立；請依家用端待辦完成後再使用 Production。')
    throw new Error(problem?.detail || problem?.message || problem?.title || '生產線別基本資料 API 呼叫失敗。')
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function readRows(): ProductionLineRow[] {
  try {
    const stored = window.localStorage.getItem(rowsKey)
    return stored ? JSON.parse(stored) as ProductionLineRow[] : structuredClone(seedRows)
  } catch { return structuredClone(seedRows) }
}
function saveRows(rows: ProductionLineRow[]): void {
  window.localStorage.setItem(rowsKey, JSON.stringify(rows))
}
function formatDate(value: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return new Intl.DateTimeFormat('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false })
    .format(date).replaceAll('-', '/')
}
function statusLabel(value: number): string {
  return statusOptions.value.find(option => option.value === Number(value))?.label ?? String(value)
}
function applyIssueFilter(value: number): void {
  issueStateFilter.value = value
  selectedKeys.value = []
  activeKey.value = ''
  anchorKey.value = ''
  errorMessage.value = ''
  successMessage.value = ''
}
async function loadRows(): Promise<void> {
  appliedPdLineNo.value = queryPdLineNo.value.trim().toLocaleUpperCase()
  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''
  try {
    if (mockMode.value) {
      allRows.value = readRows()
      if (!window.localStorage.getItem(rowsKey)) saveRows(allRows.value)
    } else {
      const query = new URLSearchParams()
      if (appliedPdLineNo.value) query.set('pdLineNo', appliedPdLineNo.value)
      const encodedQuery = query.toString()
      const suffix = encodedQuery ? '?' + encodedQuery : ''
      const result = await requestJson<ProductionLineRow[] | { items?: ProductionLineRow[] }>('api/smd/pd-line-basis' + suffix)
      allRows.value = Array.isArray(result) ? result : result.items ?? []
    }
    if (activeKey.value && !allRows.value.some(row => row.pdLineNo === activeKey.value)) {
      activeKey.value = ''
      selectedKeys.value = []
      anchorKey.value = ''
    }
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '生產線別資料載入失敗。'
  } finally { loading.value = false }
}
async function loadStatusOptions(): Promise<void> {
  if (mockMode.value) return
  try {
    const result = await requestJson<{ pdLineStatuses?: ValueOption[] }>('api/smd/pd-line-basis/options')
    statusOptions.value = result.pdLineStatuses ?? []
  } catch (error) {
    statusOptions.value = []
    errorMessage.value = error instanceof Error ? error.message : '生產線狀態選項載入失敗。'
  }
}
function selectRow(row: ProductionLineRow, event: MouseEvent): void {
  const key = row.pdLineNo
  const rows = visibleRows.value
  if (event.shiftKey && anchorKey.value) {
    const start = rows.findIndex(item => item.pdLineNo === anchorKey.value)
    const end = rows.findIndex(item => item.pdLineNo === key)
    if (start >= 0 && end >= 0) selectedKeys.value = rows.slice(Math.min(start, end), Math.max(start, end) + 1).map(item => item.pdLineNo)
  } else if (event.ctrlKey || event.metaKey) {
    selectedKeys.value = selectedKeys.value.includes(key) ? selectedKeys.value.filter(item => item !== key) : [...selectedKeys.value, key]
    anchorKey.value = key
  } else {
    selectedKeys.value = [key]
    anchorKey.value = key
  }
  activeKey.value = key
  errorMessage.value = ''
  successMessage.value = ''
}
function openForm(mode: Exclude<FormMode, null>): void {
  formError.value = ''
  errorMessage.value = ''
  successMessage.value = ''
  const source = activeRow.value
  if ((mode === 'edit' || mode === 'copy') && !source) { errorMessage.value = '請先選取一筆生產線別資料。'; return }
  if (mode === 'edit' && source?.issueState !== 0) { errorMessage.value = '只有未簽核資料可以修改。'; return }
  if (mode === 'add') {
    form.value = { pdLineNo: '', pdLineName: '', pdLineStatus: 1, description: '' }
    formDataStamp.value = 0
    audit.value = { creator: props.currentUserNo || 'DEV001', createDate: formatDate(new Date().toISOString()), reviser: '', reviseDate: '' }
  } else if (source) {
    form.value = {
      pdLineNo: source.pdLineNo,
      pdLineName: source.pdLineName,
      pdLineStatus: Number(source.pdLineStatus),
      description: source.description ?? '',
    }
    formDataStamp.value = Number(source.dataStamp ?? 0)
    audit.value = mode === 'copy'
      ? { creator: props.currentUserNo || 'DEV001', createDate: formatDate(new Date().toISOString()), reviser: '', reviseDate: '' }
      : { creator: source.creator ?? '', createDate: formatDate(source.createDate), reviser: source.reviser ?? '', reviseDate: formatDate(source.reviseDate) }
  }
  formMode.value = mode
  formBaseline.value = structuredClone(form.value)
}
function closeForm(): void {
  if (busy.value) return
  if (formDirty.value && !window.confirm('資料尚未儲存，確定關閉？')) return
  formMode.value = null
  formBaseline.value = null
}
function validateForm(): string {
  const value = form.value
  if (!value.pdLineNo.trim()) return '請輸入生產線別編號。'
  if (!value.pdLineName.trim()) return '請輸入生產線別名稱。'
  if (value.pdLineNo.length > 50) return '生產線別編號不可超過 50 個字元。'
  if (value.pdLineName.length > 50) return '生產線別名稱不可超過 50 個字元。'
  if (value.description.length > 255) return '說明不可超過 255 個字元。'
  if (value.pdLineStatus === null || ![0, 1, 2].includes(Number(value.pdLineStatus))) return '請選擇有效的生產線狀態。'
  if (formMode.value !== 'edit' && allRows.value.some(row => row.pdLineNo.toLocaleUpperCase() === value.pdLineNo.trim().toLocaleUpperCase())) return '生產線別編號已存在。'
  return ''
}
async function saveForm(closeAfterSave: boolean): Promise<void> {
  if (!formMode.value || busy.value) return
  const validation = validateForm()
  if (validation) { formError.value = validation; return }
  const mode = formMode.value
  const original = activeRow.value
  const body: Record<string, unknown> = {
    pdLineNo: form.value.pdLineNo.trim().toLocaleUpperCase(),
    pdLineName: form.value.pdLineName.trim(),
    pdLineStatus: Number(form.value.pdLineStatus),
  }
  if (form.value.description !== '') body.description = form.value.description
  busy.value = true
  formError.value = ''
  errorMessage.value = ''
  try {
    let saved: ProductionLineRow
    const now = new Date().toISOString()
    if (mockMode.value) {
      const rows = readRows()
      if (mode === 'edit') {
        if (!original) throw new Error('資料已不存在，請重新查詢。')
        const current = rows.find(row => row.pdLineNo === original.pdLineNo)
        if (!current) throw new Error('資料已不存在，請重新查詢。')
        if (Number(current.dataStamp ?? 0) !== formDataStamp.value) throw new Error('資料已被修改，請重新查詢後再操作。')
        saved = {
          ...current,
          pdLineName: form.value.pdLineName.trim(),
          pdLineStatus: Number(form.value.pdLineStatus),
          description: form.value.description === '' ? current.description : form.value.description,
          reviser: props.currentUserNo || 'DEV001',
          reviseDate: now,
          dataStamp: Number(current.dataStamp ?? 0) + 1,
        }
        saveRows(rows.map(row => row.pdLineNo === current.pdLineNo ? saved : row))
      } else {
        const pdLineNo = String(body.pdLineNo)
        if (rows.some(row => row.pdLineNo.toLocaleUpperCase() === pdLineNo)) throw new Error('生產線別編號已存在。')
        saved = {
          pdLineNo,
          pdLineName: String(body.pdLineName),
          pdLineStatus: Number(body.pdLineStatus),
          issueState: 0,
          description: form.value.description || null,
          creator: props.currentUserNo || 'DEV001',
          createDate: now,
          reviser: null,
          reviseDate: null,
          dataStamp: 0,
        }
        saveRows([...rows, saved])
      }
    } else if (mode === 'edit' && original) {
      const query = new URLSearchParams({ issueState: String(original.issueState), dataStamp: String(formDataStamp.value) })
      saved = await requestJson<ProductionLineRow>('api/smd/pd-line-basis/' + encodeURIComponent(original.pdLineNo) + '?' + query.toString(), {
        method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body),
      })
    } else {
      saved = await requestJson<ProductionLineRow>('api/smd/pd-line-basis', {
        method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body),
      })
    }
    await loadRows()
    activeKey.value = saved.pdLineNo
    selectedKeys.value = [saved.pdLineNo]
    anchorKey.value = saved.pdLineNo
    form.value = { pdLineNo: saved.pdLineNo, pdLineName: saved.pdLineName, pdLineStatus: Number(saved.pdLineStatus), description: saved.description ?? '' }
    formDataStamp.value = Number(saved.dataStamp ?? 0)
    audit.value = { creator: saved.creator ?? '', createDate: formatDate(saved.createDate), reviser: saved.reviser ?? '', reviseDate: formatDate(saved.reviseDate) }
    formMode.value = closeAfterSave ? null : 'edit'
    formBaseline.value = closeAfterSave ? null : structuredClone(form.value)
    if (!closeAfterSave) successMessage.value = '資料已儲存。'
  } catch (error) {
    formError.value = error instanceof Error ? error.message : '生產線別資料儲存失敗。'
  } finally { busy.value = false }
}
async function deleteSelected(): Promise<void> {
  const targets = [...selectedRows.value]
  if (!targets.length || busy.value) { if (!targets.length) errorMessage.value = '請先選取要刪除的資料。'; return }
  if (!window.confirm('確定刪除選取的 ' + targets.length + ' 筆生產線別資料？')) return
  busy.value = true
  errorMessage.value = ''
  successMessage.value = ''
  let completed = 0
  try {
    for (const row of targets) {
      if (![0, 1, 2, -1].includes(Number(row.issueState))) throw new Error(row.pdLineNo + ' 的簽核狀態不允許刪除；已停止後續資料。')
      if (mockMode.value) {
        const rows = readRows()
        const current = rows.find(item => item.pdLineNo === row.pdLineNo)
        if (!current || Number(current.dataStamp ?? 0) !== Number(row.dataStamp ?? 0)) throw new Error(row.pdLineNo + ' 資料已被修改，已停止後續資料。')
        if (Number(row.issueState) === 2) {
          saveRows(rows.map(item => item.pdLineNo === row.pdLineNo ? { ...item, issueState: -1, reviser: props.currentUserNo || 'DEV001', reviseDate: new Date().toISOString(), dataStamp: Number(item.dataStamp ?? 0) + 1 } : item))
        } else {
          saveRows(rows.filter(item => item.pdLineNo !== row.pdLineNo))
        }
      } else {
        const query = new URLSearchParams({ issueState: String(row.issueState), dataStamp: String(row.dataStamp ?? 0) })
        await requestJson<void>('api/smd/pd-line-basis/' + encodeURIComponent(row.pdLineNo) + '?' + query.toString(), { method: 'DELETE' })
      }
      completed++
    }
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '生產線別資料刪除失敗。' }
  finally {
    busy.value = false
    if (completed) {
      selectedKeys.value = []
      activeKey.value = ''
      anchorKey.value = ''
      await loadRows()
      if (!errorMessage.value) successMessage.value = '已完成 ' + completed + ' 筆刪除或停用。'
    }
  }
}
async function approveSelected(): Promise<void> {
  const targets = [...selectedRows.value]
  if (!targets.length || busy.value) { if (!targets.length) errorMessage.value = '請先選取要核准的資料。'; return }
  if (issueStateFilter.value !== 0 || targets.some(row => Number(row.issueState) !== 0)) { errorMessage.value = '只有未簽核資料可以核准。'; return }
  if (!window.confirm('確定核准選取的 ' + targets.length + ' 筆生產線別資料？')) return
  busy.value = true
  errorMessage.value = ''
  successMessage.value = ''
  let completed = 0
  try {
    for (const row of targets) {
      if (mockMode.value) {
        const rows = readRows()
        saveRows(rows.map(item => item.pdLineNo === row.pdLineNo
          ? { ...item, issueState: 2, reviser: props.currentUserNo || 'DEV001', reviseDate: new Date().toISOString(), dataStamp: Number(item.dataStamp ?? 0) + 1 }
          : item))
      } else {
        const query = new URLSearchParams({ issueState: '0', dataStamp: String(row.dataStamp ?? 0) })
        await requestJson<ProductionLineRow>('api/smd/pd-line-basis/' + encodeURIComponent(row.pdLineNo) + '/approve?' + query.toString(), { method: 'POST' })
      }
      completed++
    }
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '生產線別資料核准失敗。' }
  finally {
    busy.value = false
    if (completed) {
      selectedKeys.value = []
      activeKey.value = ''
      anchorKey.value = ''
      await loadRows()
      if (!errorMessage.value) successMessage.value = mockMode.value ? '已模擬核准 ' + completed + ' 筆資料。DEV 假資料直接轉為已簽核。' : '已送出 ' + completed + ' 筆核准。'
    }
  }
}

onMounted(() => { void loadRows(); void loadStatusOptions() })
</script>

<template>
  <section class="pdline-master">
    <header class="page-heading"><h1>生產線別基本資料</h1></header>
    <div class="query-strip">
      <fieldset class="issue-filter"><legend>狀態</legend><label v-for="state in issueStates" :key="state.value"><input type="radio" name="pdline-issue-state" :checked="issueStateFilter === state.value" @change="applyIssueFilter(state.value)" />{{ state.label }}</label></fieldset>
      <label for="pdline-query">生產線別編號</label><input id="pdline-query" v-model="queryPdLineNo" maxlength="50" @keyup.enter="loadRows" /><span class="wildcard">%</span>
      <button class="btn" type="button" :disabled="loading" @click="loadRows">查詢</button><span v-if="loading" class="muted">載入中…</span>
    </div>
    <div v-if="mockMode" class="preview-note">Development Preview：使用本機 localStorage 假資料，不連接正式 API；狀態選項為 DEV 樣例。</div>
    <div v-if="errorMessage" class="notice error" role="alert">{{ errorMessage }}</div>
    <div v-if="successMessage" class="notice success" role="status">{{ successMessage }}</div>
    <div class="work-area">
      <div class="grid-panel">
        <div class="table-scroll"><table class="data-grid"><thead><tr><th class="row-selector"></th><th class="pdline-no">生產線別編號</th><th class="pdline-name">生產線別名稱</th><th class="pdline-status">生產線狀態</th><th class="description">說明</th><th class="audit-user">建立人</th><th class="audit-date">建立日期</th><th class="audit-user">修改人</th><th class="audit-date">修改日期</th></tr></thead><tbody>
          <tr v-for="row in visibleRows" :key="row.pdLineNo" :class="{ active: row.pdLineNo === activeKey, selected: selectedKeys.includes(row.pdLineNo) }" @click="selectRow(row, $event)" @dblclick.stop="row.issueState === 0 && openForm('edit')">
            <td class="row-selector">{{ row.pdLineNo === activeKey ? '▶' : '' }}</td><td>{{ row.pdLineNo }}</td><td>{{ row.pdLineName }}</td><td>{{ statusLabel(row.pdLineStatus) }}</td><td class="description-cell">{{ row.description ?? '' }}</td><td>{{ row.creator ?? '' }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser ?? '' }}</td><td>{{ formatDate(row.reviseDate) }}</td>
          </tr>
          <tr v-if="!visibleRows.length"><td class="empty" colspan="9">{{ loading ? '載入中…' : '沒有符合條件的資料' }}</td></tr>
        </tbody></table></div>
        <footer class="grid-footer">資料總筆數 {{ visibleRows.length }}</footer>
      </div>
      <aside class="side-actions">
        <button class="btn btn-primary" type="button" :disabled="busy" @click="openForm('add')">新增(A)</button>
        <button class="btn" type="button" :disabled="busy || activeRow?.issueState !== 0" @click="openForm('edit')">修改(E)</button>
        <button class="btn" type="button" :disabled="busy || !selectedRows.length" @click="deleteSelected">刪除(D)</button>
        <button class="btn" type="button" :disabled="busy || !activeRow" @click="openForm('copy')">複製(C)</button>
        <button class="btn" type="button" :disabled="busy || issueStateFilter !== 0 || !selectedRows.length" @click="approveSelected">核准(Y)</button>
        <button class="btn close-btn" type="button" :disabled="busy" @click="emit('close')">關閉(X)</button>
      </aside>
    </div>
    <div v-if="formMode" class="modal-backdrop" @click.self="closeForm">
      <form class="editor-modal" @submit.prevent="saveForm(true)">
        <header class="modal-heading"><h2>生產線別定義（{{ formMode === 'add' ? '新增' : formMode === 'edit' ? '修改' : '複製' }}）</h2><button class="icon-close" type="button" aria-label="關閉" @click="closeForm">×</button></header>
        <fieldset class="editor-fields"><legend>生產線別資料</legend>
          <div class="field-row"><label for="pdline-no">PDLineNo</label><input id="pdline-no" v-model="form.pdLineNo" class="kd-input" maxlength="50" :readonly="formMode === 'edit'" @input="form.pdLineNo = form.pdLineNo.toLocaleUpperCase()" /></div>
          <div class="field-row"><label for="pdline-name">PDLineName</label><input id="pdline-name" v-model="form.pdLineName" class="kd-input" maxlength="50" /></div>
          <div class="field-row"><label for="pdline-status">PDLine Status</label><select id="pdline-status" v-model.number="form.pdLineStatus" class="kd-input"><option :value="null">請選擇</option><option v-for="option in statusOptions" :key="option.value" :value="option.value">{{ option.label }}</option></select></div>
          <div class="field-row description-row"><label for="pdline-description">Description</label><textarea id="pdline-description" v-model="form.description" class="plain-input description-input" maxlength="255"></textarea></div>
        </fieldset>
        <fieldset class="audit-fields"><legend>稽核資料</legend><label>建立人<input :value="audit.creator" readonly /></label><label>建立日期<input :value="audit.createDate" readonly /></label><label>修改人<input :value="audit.reviser" readonly /></label><label>修改日期<input :value="audit.reviseDate" readonly /></label></fieldset>
        <div v-if="formError" class="notice error form-error" role="alert">{{ formError }}</div>
        <div class="modal-actions"><button class="btn btn-primary" type="submit" :disabled="busy">確定</button><button class="btn" type="button" :disabled="busy" @click="closeForm">關閉</button><button class="btn" type="button" :disabled="busy || !formDirty" @click="saveForm(false)">套用</button></div>
      </form>
    </div>
  </section>
</template>

<style scoped>
.pdline-master{display:flex;flex-direction:column;width:100%;height:100%;min-height:0;background:#fff;color:#333;font:14px Arial,"Microsoft JhengHei",sans-serif}.page-heading{height:54px;flex:none;padding:9px 42px;background:#f5f3ed;border-bottom:1px solid #ddd}.page-heading h1{margin:0;font-size:21px;font-weight:500}.query-strip{display:flex;align-items:center;gap:7px;min-height:52px;padding:5px 10px;border-bottom:1px solid #ccc;white-space:nowrap}.query-strip>input{width:190px;height:28px;border:1px solid #999;padding:3px 6px;font:inherit}.wildcard{margin-left:-4px}.issue-filter{display:flex;gap:11px;align-items:center;margin:0 8px 0 0;padding:4px 8px;border:1px solid #ccc}.issue-filter legend{padding:0 4px;color:#666}.issue-filter label{display:flex;align-items:center;gap:3px}.issue-filter input{margin:0;accent-color:#ffa928}.muted{color:#777}.btn{min-height:29px;padding:4px 10px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#f1f1f1);color:#333;white-space:nowrap}.btn:hover:not(:disabled){border-color:#888;background:#eee}.btn:disabled{opacity:.52}.btn-primary{border-color:#ed9c21;background:#ffa928;color:#fff}.work-area{display:flex;flex:1;min-height:0;gap:10px;padding:7px}.grid-panel{display:flex;flex:1;min-width:0;min-height:0;flex-direction:column;border:1px solid #c7c7c7}.table-scroll{flex:1;min-height:0;overflow:auto}.data-grid{width:max-content;min-width:100%;border-collapse:collapse;table-layout:fixed;white-space:nowrap}.data-grid th{position:sticky;top:0;z-index:1;background:#48a3df;color:#fff;font-weight:500;text-align:left}.data-grid th,.data-grid td{height:29px;padding:4px 7px;border:1px solid #d2d2d2}.data-grid tbody tr{cursor:pointer}.data-grid tbody tr:hover{background:#fff4ca}.data-grid tbody tr.active{background:#ffe699}.data-grid tbody tr.selected:not(.active){background:#fff1c3}.row-selector{width:26px;text-align:center}.pdline-no{width:100px}.pdline-name{width:110px}.pdline-status{width:110px}.description{width:250px}.description-cell{max-width:250px;white-space:pre-wrap;overflow-wrap:anywhere}.audit-user{width:80px}.audit-date{width:150px}.empty{text-align:center;color:#777;padding:18px!important}.grid-footer{min-height:25px;padding:4px 8px;color:#666;border-top:1px solid #ccc}.side-actions{width:88px;display:flex;flex-direction:column;gap:6px}.side-actions .btn{width:100%}.close-btn{margin-top:auto}.notice{margin:4px 12px;padding:7px 10px;border-radius:3px}.error{color:#9b1c1c;background:#fff1f0;border:1px solid #e6b3ae}.success{color:#32633a;background:#f0f8ef;border:1px solid #b9d6b7}.preview-note{margin:2px 14px 7px;font-size:12px;color:#666}.modal-backdrop{position:fixed;inset:0;z-index:40;display:flex;align-items:center;justify-content:center;padding:20px;background:rgba(0,0,0,.28)}.editor-modal{box-sizing:border-box;width:min(511px,96vw);height:min(400px,94vh);display:flex;flex-direction:column;overflow:auto;border:1px solid #aaa;border-radius:4px;background:#fff;box-shadow:0 5px 24px #0004}.modal-heading{display:flex;align-items:center;justify-content:space-between;padding:5px 12px;border-bottom:1px solid #ccc;background:#f5f3ed}.modal-heading h2{margin:0;font-size:16px;font-weight:500}.icon-close{border:0;background:transparent;font-size:25px;line-height:1;color:#555}.editor-fields{box-sizing:border-box;flex:none;height:256px;margin:7px 10px 5px;padding:7px 9px 6px;border:1px solid #bbb}.editor-fields legend,.audit-fields legend{padding:0 5px;color:#555}.field-row{display:grid;grid-template-columns:96px minmax(0,1fr);align-items:center;gap:7px;min-height:28px}.field-row>label{text-align:right}.kd-input,.plain-input{box-sizing:border-box;width:100%;height:25px;border:1px solid #999;border-radius:0;padding:3px 6px;color:#222;font:inherit}.kd-input{background:#ffffc0}.kd-input:read-only{background:#eee}.plain-input{background:#fff}.description-row{align-items:start;padding-top:2px}.description-row>label{padding-top:5px}.description-input{height:145px;resize:none}.audit-fields{box-sizing:border-box;flex:none;display:grid;grid-template-columns:1fr 1fr;gap:5px 9px;height:70px;margin:0 10px;padding:5px 8px;border:1px solid #bbb}.audit-fields label{display:grid;grid-template-columns:58px minmax(0,1fr);align-items:center;gap:4px;white-space:nowrap;font-size:11px}.audit-fields input{box-sizing:border-box;min-width:0;width:100%;height:22px;border:1px solid #bbb;background:#eee;padding:2px 4px}.form-error{margin:2px 10px}.modal-actions{display:flex;justify-content:flex-end;gap:7px;margin-top:auto;padding:5px 10px 7px}@media(max-width:850px){.query-strip{flex-wrap:wrap}.issue-filter{gap:5px;margin-right:0}.work-area{gap:6px;padding:5px}.side-actions{width:76px}.audit-fields{grid-template-columns:1fr}.editor-modal{height:auto;max-height:94vh}.editor-fields{height:auto}.modal-actions{margin-top:auto}}
</style>