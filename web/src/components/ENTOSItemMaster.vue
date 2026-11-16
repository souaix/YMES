<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

interface OsItem {
  osItemNo: string
  osItemName: string
  planDays: number
  processNo: string
  returnType: number
  description: string
  creator: string | null
  createDate: string | null
  dataStamp: number | null
  reviser: string | null
  reviseDate: string | null
}

interface ProcessRow {
  processNo: string
  processVersion: string
  processClass: number
  issueState: number
  description?: string
  currentVersion?: boolean
}

type FormMode = 'add' | 'edit' | 'copy' | null
type OsItemForm = Pick<OsItem, 'osItemNo' | 'osItemName' | 'planDays' | 'processNo' | 'returnType' | 'description'>

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mockMode = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes-dev-mock:ent-os-item:v1'

const seedRows: OsItem[] = [
  { osItemNo: 'OS', osItemName: '開發用外包加工', planDays: 5, processNo: 'DEV-OUTSOURCE', returnType: 0, description: '僅供 Development 預覽', creator: 'DEV001', createDate: '2026-11-10T08:00:00+08:00', dataStamp: 1, reviser: 'DEV002', reviseDate: '2026-11-12T09:00:00+08:00' },
  { osItemNo: 'PK', osItemName: '開發用包裝加工', planDays: 2, processNo: 'DEV-PACK', returnType: 1, description: '模擬回原製程的外包項目', creator: 'DEV001', createDate: '2026-11-11T08:00:00+08:00', dataStamp: 0, reviser: null, reviseDate: null },
]
const seedProcesses: ProcessRow[] = [
  { processNo: 'DEV-OUTSOURCE', processVersion: '01', processClass: 3, issueState: 2, description: '開發用外包製程' },
  { processNo: 'DEV-PACK', processVersion: '01', processClass: 3, issueState: 2, description: '開發用包裝製程' },
  { processNo: 'DEV-INACTIVE', processVersion: '01', processClass: 3, issueState: 0, description: '未核准流程，不可選' },
]

const rows = ref<OsItem[]>([])
const processes = ref<ProcessRow[]>([])
const searchText = ref('')
const processSearch = ref('')
const selectedNos = ref<string[]>([])
const activeNo = ref('')
const selectionAnchorNo = ref('')
const loading = ref(false)
const busy = ref(false)
const processLoading = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const formError = ref('')
const processError = ref('')
const formMode = ref<FormMode>(null)
const processPickerOpen = ref(false)
const form = ref<OsItemForm>(blankForm())
const audit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const formDataStamp = ref<number | null>(null)

function blankForm(): OsItemForm {
  return { osItemNo: '', osItemName: '', planDays: 0, processNo: '', returnType: 0, description: '' }
}

const visibleRows = computed(() => {
  const term = searchText.value.trim().toLocaleLowerCase()
  return [...rows.value]
    .filter(row => !term || row.osItemNo.toLocaleLowerCase().includes(term))
    .sort((a, b) => a.osItemNo.localeCompare(b.osItemNo))
})
const activeRow = computed(() => rows.value.find(row => row.osItemNo === activeNo.value) ?? null)
const visibleProcesses = computed(() => {
  const term = processSearch.value.trim().toLocaleLowerCase()
  return processes.value
    .filter(row => row.issueState === 2 && row.processClass === 3)
    .filter(row => !term || row.processNo.toLocaleLowerCase().includes(term) || (row.description ?? '').toLocaleLowerCase().includes(term))
    .sort((a, b) => a.processNo.localeCompare(b.processNo) || a.processVersion.localeCompare(b.processVersion))
})

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
    if (response.status === 404) throw new Error('外包項目 API 尚未建立；請依 todolist_homepc.md 完成 ET202 API。')
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 回應錯誤 (${response.status})。`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}

function readMockRows(): OsItem[] {
  try {
    const value = window.localStorage.getItem(storageKey)
    return value ? JSON.parse(value) as OsItem[] : structuredClone(seedRows)
  } catch { return structuredClone(seedRows) }
}
function saveMockRows(value: OsItem[]): void { window.localStorage.setItem(storageKey, JSON.stringify(value)) }
function formatDate(value: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat('zh-TW', { dateStyle: 'short', timeStyle: 'medium', hour12: false }).format(date)
}
function returnTypeLabel(value: number): string { return value === 1 ? '回原製程' : '不回原製程' }
function osItemPath(no: string): string { return `api/ent/os-items/${encodeURIComponent(no)}` }
function selectRow(row: OsItem, event: MouseEvent): void {
  const index = visibleRows.value.findIndex(item => item.osItemNo === row.osItemNo)
  if (event.shiftKey && selectionAnchorNo.value) {
    const anchorIndex = visibleRows.value.findIndex(item => item.osItemNo === selectionAnchorNo.value)
    if (anchorIndex >= 0) {
      const start = Math.min(anchorIndex, index)
      const end = Math.max(anchorIndex, index)
      selectedNos.value = visibleRows.value.slice(start, end + 1).map(item => item.osItemNo)
    } else selectedNos.value = [row.osItemNo]
  } else if (event.ctrlKey || event.metaKey) {
    selectedNos.value = selectedNos.value.includes(row.osItemNo)
      ? selectedNos.value.filter(no => no !== row.osItemNo)
      : [...selectedNos.value, row.osItemNo]
    selectionAnchorNo.value = row.osItemNo
  } else {
    selectedNos.value = [row.osItemNo]
    selectionAnchorNo.value = row.osItemNo
  }
  activeNo.value = row.osItemNo
}

async function loadRows(): Promise<void> {
  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''
  try {
    if (mockMode.value) {
      rows.value = readMockRows()
      if (!window.localStorage.getItem(storageKey)) saveMockRows(rows.value)
    } else {
      const query = new URLSearchParams()
      if (searchText.value.trim()) query.set('osItemNo', searchText.value.trim())
      rows.value = await requestJson<OsItem[]>(`api/ent/os-items${query.size ? `?${query.toString()}` : ''}`)
    }
    const visibleNos = new Set(visibleRows.value.map(row => row.osItemNo))
    selectedNos.value = selectedNos.value.filter(no => visibleNos.has(no))
    if (!visibleNos.has(activeNo.value)) {
      activeNo.value = visibleRows.value[0]?.osItemNo ?? ''
      selectedNos.value = activeNo.value ? [activeNo.value] : []
    }
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '外包項目資料載入失敗。' }
  finally { loading.value = false }
}

async function loadProcesses(): Promise<void> {
  processLoading.value = true
  processError.value = ''
  try {
    if (mockMode.value) processes.value = structuredClone(seedProcesses)
    else processes.value = await requestJson<ProcessRow[]>('api/processes?issueState=2')
  } catch (error) {
    processes.value = []
    processError.value = error instanceof Error ? error.message : '流程資料載入失敗。'
  } finally { processLoading.value = false }
}

function clearAudit(): void { audit.value = { creator: '', createDate: '', reviser: '', reviseDate: '' } }
function openForm(mode: Exclude<FormMode, null>): void {
  const source = activeRow.value
  if (mode !== 'add' && !source) { errorMessage.value = '請先選取一筆外包項目資料。'; return }
  formError.value = ''
  successMessage.value = ''
  formMode.value = mode
  formDataStamp.value = mode === 'edit' ? source?.dataStamp ?? 0 : null
  if (mode === 'add') {
    form.value = blankForm()
    clearAudit()
    audit.value.creator = props.currentUserNo || 'DEV001'
    audit.value.createDate = formatDate(new Date().toISOString())
  } else if (source) {
    form.value = { osItemNo: source.osItemNo, osItemName: source.osItemName, planDays: source.planDays, processNo: source.processNo, returnType: source.returnType, description: source.description ?? '' }
    audit.value = {
      creator: mode === 'edit' ? source.creator ?? '' : props.currentUserNo || 'DEV001',
      createDate: mode === 'edit' ? formatDate(source.createDate) : formatDate(new Date().toISOString()),
      reviser: mode === 'edit' ? props.currentUserNo || source.reviser || '' : '',
      reviseDate: mode === 'edit' ? formatDate(new Date().toISOString()) : '',
    }
    if (mode === 'copy') form.value.osItemNo = ''
  }
  processPickerOpen.value = false
  processSearch.value = ''
  void loadProcesses()
}

function validateForm(): string {
  form.value.osItemNo = form.value.osItemNo.trim().toLocaleUpperCase()
  form.value.osItemName = form.value.osItemName.trim()
  form.value.processNo = form.value.processNo.trim()
  if (!form.value.osItemNo) return '外包項目編號為必填。'
  if (form.value.osItemNo.length > 2) return '外包項目編號最多 2 個字元。'
  if (!form.value.osItemName) return '外包項目名稱為必填。'
  if (form.value.osItemName.length > 50) return '外包項目名稱最多 50 個字元。'
  if (!Number.isInteger(Number(form.value.planDays)) || Number(form.value.planDays) < 0 || Number(form.value.planDays) > 999) return '預計天數必須是 0 到 999 的整數。'
  if (!form.value.processNo) return '流程編號為必填。'
  if (form.value.processNo.length > 25) return '流程編號最多 25 個字元。'
  if (processLoading.value) return '流程資料載入中，請稍候再儲存。'
  if (!processes.value.some(row => row.issueState === 2 && row.processClass === 3 && row.processNo.toLocaleUpperCase() === form.value.processNo.toLocaleUpperCase())) return '流程編號必須是已簽核的外包類流程。'
  if (form.value.description.length > 255) return '說明最多 255 個字元。'
  return ''
}

async function saveForm(closeAfterSave: boolean): Promise<void> {
  if (!formMode.value || busy.value) return
  const validation = validateForm()
  if (validation) { formError.value = validation; return }
  busy.value = true
  formError.value = ''
  errorMessage.value = ''
  const now = new Date().toISOString()
  try {
    const body = {
      osItemNo: form.value.osItemNo,
      osItemName: form.value.osItemName,
      planDays: Number(form.value.planDays),
      processNo: form.value.processNo,
      returnType: Number(form.value.returnType),
      description: form.value.description,
      ...(formMode.value === 'edit' ? { dataStamp: formDataStamp.value } : {}),
    }
    let saved: OsItem
    if (mockMode.value) {
      const current = readMockRows()
      if (formMode.value !== 'edit' && current.some(row => row.osItemNo.toLocaleUpperCase() === body.osItemNo.toLocaleUpperCase())) throw new Error('外包項目編號已存在。')
      const previous = current.find(row => row.osItemNo === body.osItemNo)
      saved = {
        ...body,
        creator: formMode.value === 'edit' ? previous?.creator ?? null : props.currentUserNo || 'DEV001',
        createDate: formMode.value === 'edit' ? previous?.createDate ?? null : now,
        reviser: formMode.value === 'edit' ? props.currentUserNo || 'DEV001' : null,
        reviseDate: formMode.value === 'edit' ? now : null,
        dataStamp: formMode.value === 'edit' ? (previous?.dataStamp ?? 0) + 1 : 0,
      }
      const next = formMode.value === 'edit' ? current.map(row => row.osItemNo === body.osItemNo ? saved : row) : [...current, saved]
      saveMockRows(next)
      rows.value = next
    } else if (formMode.value === 'edit') {
      saved = await requestJson<OsItem>(`${osItemPath(body.osItemNo)}?dataStamp=${encodeURIComponent(String(formDataStamp.value ?? 0))}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
      rows.value = rows.value.map(row => row.osItemNo === saved.osItemNo ? saved : row)
    } else {
      saved = await requestJson<OsItem>('api/ent/os-items', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
      rows.value = [...rows.value, saved].sort((a, b) => a.osItemNo.localeCompare(b.osItemNo))
    }
    activeNo.value = saved.osItemNo
    selectedNos.value = [saved.osItemNo]
    successMessage.value = '資料已儲存。'
    if (formMode.value === 'edit') {
      formDataStamp.value = saved.dataStamp ?? (formDataStamp.value ?? 0) + 1
      audit.value.reviser = props.currentUserNo || audit.value.reviser
      audit.value.reviseDate = formatDate(saved.reviseDate ?? now)
    } else {
      audit.value.creator = props.currentUserNo || 'DEV001'
      audit.value.createDate = formatDate(saved.createDate ?? now)
    }
    if (closeAfterSave) formMode.value = null
  } catch (error) { formError.value = error instanceof Error ? error.message : '外包項目儲存失敗。' }
  finally { busy.value = false }
}

async function deleteSelected(): Promise<void> {
  if (!selectedNos.value.length || busy.value) return
  const toDelete = [...selectedNos.value]
  if (!window.confirm(`確定刪除選取的 ${toDelete.length} 筆外包項目？`)) return
  busy.value = true
  errorMessage.value = ''
  successMessage.value = ''
  let completed = 0
  try {
    if (mockMode.value) {
      const remove = new Set(toDelete)
      rows.value = readMockRows().filter(row => !remove.has(row.osItemNo))
      saveMockRows(rows.value)
      completed = toDelete.length
    } else {
      for (const no of toDelete) {
        const item = rows.value.find(row => row.osItemNo === no)
        await requestJson<void>(`${osItemPath(no)}?dataStamp=${encodeURIComponent(String(item?.dataStamp ?? 0))}`, { method: 'DELETE' })
        completed += 1
      }
      rows.value = rows.value.filter(row => !toDelete.includes(row.osItemNo))
    }
    selectedNos.value = []
    if (toDelete.includes(activeNo.value)) activeNo.value = visibleRows.value[0]?.osItemNo ?? ''
    successMessage.value = `已刪除 ${completed} 筆資料。`
  } catch (error) {
    const message = `${error instanceof Error ? error.message : '外包項目刪除失敗。'}（已完成 ${completed}/${toDelete.length} 筆）`
    if (completed > 0) await loadRows()
    errorMessage.value = message
  } finally { busy.value = false }
}

function chooseProcess(row: ProcessRow): void {
  form.value.processNo = row.processNo
  processPickerOpen.value = false
}

onMounted(() => { void loadRows() })
</script>

<template>
  <section class="ositem-master">
    <header class="page-heading"><h1>外包項目設定</h1></header>

    <form class="query-strip" @submit.prevent="loadRows">
      <label for="ositem-query">外包項目編號</label>
      <input id="ositem-query" v-model="searchText" maxlength="2" @input="searchText = searchText.toLocaleUpperCase()" />
      <span class="query-percent">%</span>
      <button class="btn" type="submit" :disabled="loading">查詢</button>
      <span v-if="loading" class="muted">載入中…</span>
    </form>

    <div class="work-area">
      <div class="grid-panel">
        <div class="toolbar">
          <button class="btn btn-primary" type="button" @click="openForm('add')">新增(A)</button>
          <button class="btn" type="button" :disabled="!activeRow" @click="openForm('edit')">修改(E)</button>
          <button class="btn" type="button" :disabled="!selectedNos.length || busy" @click="deleteSelected">刪除(D)</button>
          <button class="btn" type="button" :disabled="!activeRow" @click="openForm('copy')">複製(C)</button>
        </div>
        <div class="table-scroll">
          <table class="data-grid">
            <thead>
              <tr>
                <th class="row-selector" aria-label="選取列"></th>
                <th>外包項目編號</th><th>外包項目名稱</th><th class="numeric">預計天數</th><th>流程編號</th><th>退回原流程</th><th>說明</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="row in visibleRows" :key="row.osItemNo" :class="{ active: row.osItemNo === activeNo, selected: selectedNos.includes(row.osItemNo) }" @click="selectRow(row, $event)">
                <td class="row-selector">{{ row.osItemNo === activeNo ? '▶' : '' }}</td>
                <td>{{ row.osItemNo }}</td><td>{{ row.osItemName }}</td><td class="numeric">{{ row.planDays }}</td><td>{{ row.processNo }}</td><td>{{ returnTypeLabel(row.returnType) }}</td><td class="description-cell">{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td>
              </tr>
              <tr v-if="!loading && visibleRows.length === 0"><td class="empty" colspan="11">沒有符合條件的資料</td></tr>
            </tbody>
          </table>
        </div>
        <footer class="grid-footer">資料總筆數 {{ visibleRows.length }}</footer>
      </div>
      <aside class="side-actions">
        <button class="btn close-btn" type="button" @click="emit('close')">關閉(X)</button>
      </aside>
    </div>

    <p v-if="errorMessage" class="notice error" role="alert">{{ errorMessage }}</p>
    <p v-if="successMessage" class="notice success" role="status">{{ successMessage }}</p>
    <p v-if="mockMode" class="preview-note">Development 假資料預覽；不會寫入 MES 或 Production 資料庫。</p>

    <div v-if="formMode" class="modal-backdrop" @click.self="formMode = null">
      <section class="editor-modal" role="dialog" aria-modal="true" :aria-label="formMode === 'edit' ? '修改外包項目' : formMode === 'copy' ? '複製外包項目' : '新增外包項目'">
        <header class="modal-heading"><h2>外包項目資料（{{ formMode === 'edit' ? '修改' : formMode === 'copy' ? '複製' : '新增' }}）</h2><button type="button" class="icon-close" aria-label="關閉" @click="formMode = null">×</button></header>
        <fieldset class="editor-fields">
          <legend>外包項目資料</legend>
          <div class="field-row"><label for="ositem-no">外包項目編號</label><input id="ositem-no" v-model="form.osItemNo" maxlength="2" :disabled="formMode === 'edit'" class="kd-input" @blur="form.osItemNo = form.osItemNo.toLocaleUpperCase()" /></div>
          <div class="field-row"><label for="ositem-name">外包項目名稱</label><input id="ositem-name" v-model="form.osItemName" maxlength="50" class="kd-input" /></div>
          <div class="field-row"><label for="ositem-days">預計天數</label><input id="ositem-days" v-model.number="form.planDays" type="number" min="0" max="999" step="1" class="kd-input short-input" /><span class="field-unit">天</span></div>
          <div class="field-row"><label for="ositem-process">流程編號</label><input id="ositem-process" v-model="form.processNo" maxlength="25" class="kd-input" /><button class="btn lookup-btn" type="button" aria-label="查找流程" @click="processPickerOpen = true">…</button></div>
          <div class="field-row"><span class="field-label">退回原流程</span><div class="radio-options"><label><input v-model.number="form.returnType" type="radio" :value="0" /> 不回原流程</label><label><input v-model.number="form.returnType" type="radio" :value="1" /> 回原流程</label></div></div>
          <div class="field-row description-row"><label for="ositem-description">說明</label><textarea id="ositem-description" v-model="form.description" maxlength="255" rows="5"></textarea></div>
        </fieldset>
        <fieldset class="audit-fields">
          <legend>建立／修改資訊</legend>
          <label>建立人<input :value="audit.creator" readonly /></label><label>建立日期<input :value="audit.createDate" readonly /></label>
          <label>修改人<input :value="audit.reviser" readonly /></label><label>修改日期<input :value="audit.reviseDate" readonly /></label>
        </fieldset>
        <p v-if="formError" class="notice error" role="alert">{{ formError }}</p>
        <footer class="modal-actions"><button class="btn btn-primary" type="button" :disabled="busy" @click="saveForm(true)">確定</button><button class="btn" type="button" @click="formMode = null">關閉</button><button class="btn" type="button" :disabled="busy" @click="saveForm(false)">套用</button></footer>
      </section>
    </div>

    <div v-if="processPickerOpen" class="modal-backdrop nested-backdrop" @click.self="processPickerOpen = false">
      <section class="picker-modal" role="dialog" aria-modal="true" aria-label="選擇外包流程">
        <header class="modal-heading"><h2>選擇流程（已簽核／外包類）</h2><button type="button" class="icon-close" aria-label="關閉" @click="processPickerOpen = false">×</button></header>
        <div class="picker-search"><label for="process-query">流程編號</label><input id="process-query" v-model="processSearch" /><button class="btn" type="button" @click="loadProcesses">查詢</button></div>
        <p v-if="processError" class="notice error">{{ processError }}</p>
        <div v-if="processLoading" class="picker-empty">流程資料載入中…</div>
        <div v-else class="table-scroll picker-table-scroll"><table class="data-grid"><thead><tr><th>流程編號</th><th>版本</th><th>說明</th></tr></thead><tbody>
          <tr v-for="row in visibleProcesses" :key="`${row.processNo}/${row.processVersion}`" tabindex="0" @click="chooseProcess(row)" @keydown.enter="chooseProcess(row)"><td>{{ row.processNo }}</td><td>{{ row.processVersion }}</td><td>{{ row.description }}</td></tr>
          <tr v-if="!visibleProcesses.length"><td colspan="3" class="empty">沒有可選的已簽核外包流程</td></tr>
        </tbody></table></div>
        <footer class="modal-actions"><button class="btn" type="button" @click="processPickerOpen = false">關閉</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.ositem-master{height:100%;min-height:0;display:flex;flex-direction:column;background:#fff;color:#333;font-size:14px}.page-heading{padding:8px 14px;border-bottom:1px solid #d5d5d5;background:#f5f3ed}.page-heading h1{margin:0;font-size:20px;font-weight:500}.query-strip{min-height:48px;display:flex;align-items:center;gap:8px;padding:6px 14px;border-bottom:1px solid #d4d4d4}.query-strip label,.picker-search label{font-weight:500}.query-strip input,.picker-search input{height:29px;width:190px;border:1px solid #aaa;padding:3px 7px}.query-percent{margin-left:-4px}.work-area{display:flex;flex:1;min-height:0;gap:10px;padding:7px}.grid-panel{display:flex;flex:1;min-width:0;min-height:0;flex-direction:column;border:1px solid #c7c7c7}.toolbar{display:flex;flex-wrap:wrap;gap:5px;padding:6px;border-bottom:1px solid #ccc}.btn{min-height:30px;padding:4px 12px;border:1px solid #b8b8b8;border-radius:3px;background:linear-gradient(#fff,#f1f1f1);color:#333;white-space:nowrap}.btn:hover:not(:disabled){border-color:#888;background:#eee}.btn:disabled{opacity:.52}.btn-primary{border-color:#ed9c21;background:#ffa928;color:#fff}.btn-primary:hover:not(:disabled){background:#ed991f}.table-scroll{flex:1;min-height:0;overflow:auto}.data-grid{width:100%;border-collapse:collapse;white-space:nowrap}.data-grid th{position:sticky;top:0;z-index:1;background:#48a3df;color:#fff;font-weight:500;text-align:left}.data-grid th,.data-grid td{height:29px;padding:4px 7px;border:1px solid #d2d2d2}.data-grid tbody tr{cursor:pointer}.data-grid tbody tr:hover{background:#fff4ca}.data-grid tbody tr.active{background:#ffe699}.data-grid tbody tr.active:hover{background:#ffdf7a}.check-cell{width:28px;text-align:center}.check-cell input{accent-color:#f4a21c}.numeric{text-align:right}.description-cell{max-width:300px;overflow:hidden;text-overflow:ellipsis}.empty{text-align:center;color:#777;padding:18px!important}.grid-footer{min-height:25px;padding:4px 8px;color:#666;border-top:1px solid #ccc}.side-actions{width:112px;display:flex;flex-direction:column;align-items:stretch;gap:7px;padding-top:42px}.close-btn{margin-top:auto}.notice{margin:4px 12px;padding:7px 10px;border-radius:3px}.error{color:#9b1c1c;background:#fff1f0;border:1px solid #e6b3ae}.success{color:#32633a;background:#f0f8ef;border:1px solid #b9d6b7}.muted,.preview-note{color:#777}.preview-note{margin:2px 14px 7px;font-size:12px}.modal-backdrop{position:fixed;inset:0;z-index:40;display:flex;align-items:center;justify-content:center;padding:20px;background:rgba(0,0,0,.28)}.editor-modal,.picker-modal{width:min(540px,96vw);max-height:94vh;overflow:auto;border:1px solid #aaa;border-radius:4px;background:#fff;box-shadow:0 5px 24px #0004}.modal-heading{display:flex;align-items:center;justify-content:space-between;padding:10px 14px;border-bottom:1px solid #ccc;background:#f5f3ed}.modal-heading h2{margin:0;font-size:17px;font-weight:500}.icon-close{border:0;background:transparent;font-size:25px;line-height:1;color:#555}.editor-fields{margin:12px 14px 7px;padding:10px 12px 12px;border:1px solid #bbb}.editor-fields legend,.audit-fields legend{padding:0 5px;color:#555}.field-row{display:grid;grid-template-columns:105px minmax(150px,1fr) 30px;align-items:center;gap:8px;min-height:35px}.field-row>label,.field-label{text-align:right}.kd-input{height:27px;width:100%;border:1px solid #999;border-radius:0;background:#fff0ad;padding:3px 6px;color:#222}.kd-input:disabled{background:#eee;color:#777}.short-input{width:100px}.field-unit{margin-left:-18px}.lookup-btn{grid-column:3}.radio-options{grid-column:2/4;display:flex;gap:18px}.radio-options input{accent-color:#f1a021;vertical-align:middle}.description-row{align-items:start;padding-top:5px}.description-row textarea{grid-column:2/4;min-height:100px;resize:vertical;border:1px solid #999;padding:5px}.audit-fields{display:grid;grid-template-columns:1fr 1fr;gap:7px 12px;margin:8px 14px;padding:10px 12px;border:1px solid #bbb}.audit-fields label{display:flex;align-items:center;gap:7px;white-space:nowrap}.audit-fields input{min-width:0;width:100%;height:25px;border:1px solid #bbb;background:#eee;padding:3px 5px}.modal-actions{display:flex;justify-content:flex-end;gap:8px;padding:10px 14px}.nested-backdrop{z-index:50}.picker-modal{width:min(680px,96vw)}.picker-search{display:flex;align-items:center;gap:8px;padding:10px 14px}.picker-table-scroll{max-height:50vh}.picker-table-scroll tbody tr{cursor:pointer}.picker-empty{padding:20px;text-align:center;color:#777}
@media(max-width:760px){.work-area{padding:5px}.side-actions{width:86px}.audit-fields{grid-template-columns:1fr}.query-strip{flex-wrap:wrap}.field-row{grid-template-columns:95px minmax(100px,1fr) 30px}.editor-modal{max-height:98vh}}
</style>

<style scoped>
.side-actions{width:88px}
.row-selector{width:24px;padding:2px!important;text-align:center;color:#333}
.data-grid tbody tr.selected:not(.active){background:#fff1c3}
</style>
