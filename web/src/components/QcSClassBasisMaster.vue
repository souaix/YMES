<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

type State = -1 | 0 | 1 | 2
type Mode = 'add' | 'edit' | 'copy' | null
interface Row { sClassNo: string; mClassNo: string | null; description: string | null; creator: string | null; createDate: string | null; issueState: State; dataStamp: number | null; reviser: string | null; reviseDate: string | null }
interface MClassOption { mClassNo: string; issueState: State; description?: string | null }
interface ImportPair { sClassNo: string; mClassNo: string; description: string }
interface ImportResult extends ImportPair { result: 'success' | 'fail'; message: string }
const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes-dev-mock:qc-s-class-basis:v1'
const states: { value: State; label: string }[] = [{ value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' }]
const seed: Row[] = [
  { sClassNo: 'DEV-SC-01', mClassNo: 'DEV-MC-03', description: 'DEV 外觀刮傷', creator: 'DEV001', createDate: '2026-11-10T08:00:00+08:00', issueState: 0, dataStamp: 0, reviser: null, reviseDate: null },
  { sClassNo: 'DEV-SC-02', mClassNo: 'DEV-MC-03', description: 'DEV 尺寸偏差', creator: 'DEV001', createDate: '2026-11-10T09:00:00+08:00', issueState: 1, dataStamp: 1, reviser: 'DEV002', reviseDate: '2026-11-10T09:30:00+08:00' },
  { sClassNo: 'DEV-SC-03', mClassNo: 'DEV-MC-03', description: 'DEV 製程異常', creator: 'DEV002', createDate: '2026-11-11T08:00:00+08:00', issueState: 2, dataStamp: 2, reviser: 'DEV001', reviseDate: '2026-11-11T09:00:00+08:00' },
  { sClassNo: 'DEV-SC-04', mClassNo: 'DEV-MC-03', description: 'DEV 不使用範例', creator: 'DEV001', createDate: '2026-11-12T08:00:00+08:00', issueState: -1, dataStamp: 3, reviser: 'DEV001', reviseDate: '2026-11-12T09:00:00+08:00' },
]
const rows = ref<Row[]>([])
const state = ref<State>(0)
const search = ref('')
const appliedSearch = ref('')
const selected = ref<string[]>([])
const active = ref('')
const loading = ref(false)
const busy = ref(false)
const error = ref('')
const notice = ref('')
const mode = ref<Mode>(null)
const source = ref<Row | null>(null)
const form = ref({ sClassNo: '', mClassNo: '', description: '' })
const mClassOptions = ref<MClassOption[]>([])
const mClassLookupOpen = ref(false)
const mClassLookupSearch = ref('')
const formCreateDate = ref('')
const formError = ref('')
const saving = ref(false)
const importOpen = ref(false)
const importName = ref('')
const importFile = ref<File | null>(null)
const importPairs = ref<ImportPair[]>([])
const rawImportRows = ref<ImportPair[]>([])
const importResults = ref<ImportResult[]>([])
const importError = ref('')
const importing = ref(false)
const importProgress = ref(0)
const cancelRequested = ref(false)
let importAbortController: AbortController | null = null
const visible = computed(() => rows.value.filter(r => r.issueState === state.value && (!appliedSearch.value || r.sClassNo.toLowerCase().includes(appliedSearch.value.toLowerCase()))).sort((a, b) => a.sClassNo.localeCompare(b.sClassNo)))
const selectedRows = computed(() => visible.value.filter(r => selected.value.includes(r.sClassNo)))
const activeRow = computed(() => rows.value.find(r => r.sClassNo === active.value) ?? null)
const formTitle = computed(() => '次分類基本資料（' + (mode.value === 'add' ? '新增' : mode.value === 'copy' ? '複製' : '修改') + '）')
const readOnly = computed(() => mode.value === 'edit' && (source.value?.issueState === 1 || source.value?.issueState === -1))
const availableMClasses = computed(() => mClassOptions.value.filter(row => row.issueState === 2 && (!mClassLookupSearch.value || row.mClassNo.toLowerCase().includes(mClassLookupSearch.value.trim().toLowerCase()) || (row.description || '').toLowerCase().includes(mClassLookupSearch.value.trim().toLowerCase()))).sort((a, b) => a.mClassNo.localeCompare(b.mClassNo)))
function copy<T>(v: T): T { return structuredClone(v) }
function now(): string { return new Date().toISOString() }
function readStore(): { rows: Row[] } {
  try { const value = window.localStorage.getItem(storageKey); if (value) { const data = JSON.parse(value); if (Array.isArray(data.rows)) return data } } catch { /* reset invalid dev data */ }
  const data = { rows: copy(seed) }; writeStore(data); return data
}
function writeStore(data: { rows: Row[] }): void { window.localStorage.setItem(storageKey, JSON.stringify(data)) }
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body && !(init.body instanceof FormData) && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init, headers })
  if (response.status === 401) { window.location.assign(appBase + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    if (response.status === 404) throw new Error('QC402 API 尚未建立；請依 todolist_homepc.md 完成資料表與 API。')
    throw new Error(problem?.detail || problem?.message || problem?.title || 'API 回應錯誤（' + response.status + '）。')
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
async function load(): Promise<void> {
  if (mock.value) { rows.value = copy(readStore().rows); return }
  const q = new URLSearchParams()
  if (appliedSearch.value) { q.set('searchFieldName', 'SClassNo'); q.set('searchFieldValue', appliedSearch.value) }
  const suffix = q.toString()
  rows.value = await request<Row[]>('api/qc/s-class-basis' + (suffix ? '?' + suffix : ''))
}
async function loadMClassOptions(): Promise<void> {
  if (mock.value) {
    const stored = window.localStorage.getItem('ymes-dev-mock:qc-m-class-basis:v1')
    let sourceRows: MClassOption[] | undefined
    try { sourceRows = stored ? (JSON.parse(stored) as { rows?: MClassOption[] }).rows : undefined } catch { sourceRows = undefined }
    mClassOptions.value = sourceRows ?? [{ mClassNo: 'DEV-MC-03', issueState: 2, description: 'DEV 製程異常' }]
    return
  }
  mClassOptions.value = await request<MClassOption[]>('api/qc/s-class-basis/m-class-options')
}
function setActive(prefer = ''): void {
  const row = visible.value.find(r => r.sClassNo === prefer) ?? visible.value[0]
  active.value = row?.sClassNo ?? ''; selected.value = row ? [row.sClassNo] : []
}
async function query(): Promise<void> {
  appliedSearch.value = search.value.trim(); loading.value = true; error.value = ''; notice.value = ''
  try { await load(); setActive() } catch (e) { error.value = e instanceof Error ? e.message : '查詢次分類失敗。' } finally { loading.value = false }
}
async function refresh(prefer = ''): Promise<void> { await load(); setActive(prefer) }
function selectRow(row: Row): void { active.value = row.sClassNo; selected.value = [row.sClassNo] }
function toggleRow(row: Row): void { selected.value = selected.value.includes(row.sClassNo) ? selected.value.filter(x => x !== row.sClassNo) : [...selected.value, row.sClassNo]; active.value = row.sClassNo }
function selectAll(e: Event): void { selected.value = (e.target as HTMLInputElement).checked ? visible.value.map(r => r.sClassNo) : [] }
function openForm(next: Exclude<Mode, null>, row: Row | null = null): void {
  source.value = row ? copy(row) : null; form.value = { sClassNo: row?.sClassNo ?? '', mClassNo: row?.mClassNo ?? '', description: row?.description ?? '' }; formCreateDate.value = row?.createDate ?? now(); formError.value = ''; mode.value = next
}
function closeForm(): void { mode.value = null; source.value = null; formError.value = '' }
function openMClassLookup(): void { mClassLookupSearch.value = ''; mClassLookupOpen.value = true }
function selectMClass(row: MClassOption): void { form.value.mClassNo = row.mClassNo; mClassLookupOpen.value = false }
function validate(): string {
  const no = form.value.sClassNo.trim()
  if (!no) return '次分類編號為必填。'
  if (no.length > 30) return '次分類編號不可超過 30 個字元。'
  if (!form.value.mClassNo.trim()) return '主分類編號為必填。'
  if (!mClassOptions.value.some(row => row.issueState === 2 && row.mClassNo.toUpperCase() === form.value.mClassNo.trim().toUpperCase())) return '主分類必須選擇已簽核的主分類資料。'
  if (form.value.description.length > 255) return '說明不可超過 255 個字元。'
  if (readOnly.value) return '簽核中的資料不可修改。'
  if (mode.value !== 'edit' && rows.value.some(r => r.sClassNo.toUpperCase() === no.toUpperCase())) return '次分類編號已存在。'
  return ''
}
async function save(keepOpen: boolean): Promise<void> {
  formError.value = validate(); if (formError.value || !mode.value) return
  const currentMode = mode.value; const original = source.value; const no = form.value.sClassNo.trim().toUpperCase(); const mClassNo = form.value.mClassNo.trim().toUpperCase(); const description = form.value.description.trim() || null
  saving.value = true
  try {
    if (mock.value) {
      const data = readStore()
      if (currentMode === 'edit') {
        const i = data.rows.findIndex(r => r.sClassNo === original?.sClassNo)
        if (i < 0) throw new Error('資料已不存在，請重新查詢。')
        data.rows[i] = { ...data.rows[i], mClassNo, description, reviser: props.currentUserNo || 'DEV001', reviseDate: now(), dataStamp: (data.rows[i].dataStamp ?? 0) + 1 }
      } else data.rows.push({ sClassNo: no, mClassNo, description, creator: props.currentUserNo || 'DEV001', createDate: formCreateDate.value, issueState: 0, dataStamp: 0, reviser: null, reviseDate: null })
      writeStore(data)
    } else if (currentMode === 'edit') {
      const row = original!
      const q = new URLSearchParams({ issueState: String(row.issueState) })
      if (row.dataStamp !== null) q.set('dataStamp', String(row.dataStamp))
      await request('api/qc/s-class-basis/' + encodeURIComponent(row.sClassNo) + '?' + q.toString(), { method: 'PUT', body: JSON.stringify({ mClassNo, description }) })
    } else await request('api/qc/s-class-basis', { method: 'POST', body: JSON.stringify({ sClassNo: no, mClassNo, description }) })
    await refresh(no)
    if (keepOpen && currentMode !== 'edit') { notice.value = '資料已套用；再次新增前請輸入新的次分類編號。' }
    else if (keepOpen) { source.value = activeRow.value ? copy(activeRow.value) : null; notice.value = '資料已套用。' }
    else closeForm()
  } catch (e) { formError.value = e instanceof Error ? e.message : '儲存次分類失敗。' } finally { saving.value = false }
}
async function removeSelected(): Promise<void> {
  if (!selectedRows.value.length) return
  if (selectedRows.value.some(r => r.issueState === 1)) { error.value = '簽核中的資料不可刪除。'; return }
  if (!window.confirm('確定刪除或停用選取的 ' + selectedRows.value.length + ' 筆資料？')) return
  busy.value = true; error.value = ''
  try {
    for (const row of [...selectedRows.value]) {
      if (mock.value) {
        const data = readStore()
        if (row.issueState === 2) { const item = data.rows.find(r => r.sClassNo === row.sClassNo)!; item.issueState = -1; item.reviser = props.currentUserNo || 'DEV001'; item.reviseDate = now(); item.dataStamp = (item.dataStamp ?? 0) + 1 }
        else data.rows = data.rows.filter(r => r.sClassNo !== row.sClassNo)
        writeStore(data)
      } else {
        const q = new URLSearchParams({ issueState: String(row.issueState) }); if (row.dataStamp !== null) q.set('dataStamp', String(row.dataStamp))
        await request('api/qc/s-class-basis/' + encodeURIComponent(row.sClassNo) + '?' + q.toString(), { method: 'DELETE' })
      }
    }
    await refresh(); notice.value = '選取資料已完成刪除或停用。'
  } catch (e) { error.value = e instanceof Error ? e.message : '刪除次分類失敗。' } finally { busy.value = false }
}
async function approveSelected(): Promise<void> {
  if (!selectedRows.value.length) return
  if (selectedRows.value.some(r => r.issueState !== 0)) { error.value = '只有未簽核資料可以核准。'; return }
  if (!window.confirm('確定核准選取的 ' + selectedRows.value.length + ' 筆次分類？')) return
  busy.value = true; error.value = ''
  try {
    for (const row of [...selectedRows.value]) {
      if (mock.value) { const data = readStore(); const item = data.rows.find(r => r.sClassNo === row.sClassNo)!; item.issueState = 2; item.reviser = props.currentUserNo || 'DEV001'; item.reviseDate = now(); item.dataStamp = (item.dataStamp ?? 0) + 1; writeStore(data) }
      else { const q = new URLSearchParams({ issueState: String(row.issueState) }); if (row.dataStamp !== null) q.set('dataStamp', String(row.dataStamp)); await request('api/qc/s-class-basis/' + encodeURIComponent(row.sClassNo) + '/approve?' + q.toString(), { method: 'POST' }) }
    }
    await refresh(); notice.value = '已送出核准；正式簽核結果依 Issue Job 流程更新。'
  } catch (e) { error.value = e instanceof Error ? e.message : '核准次分類失敗。' } finally { busy.value = false }
}
function openImport(): void { importOpen.value = true; importName.value = ''; importFile.value = null; importPairs.value = []; rawImportRows.value = []; importResults.value = []; importError.value = ''; importProgress.value = 0; cancelRequested.value = false }
function parseCsv(text: string): string[][] {
  const records: string[][] = []; let row: string[] = []; let field = ''; let quoted = false
  for (let i = 0; i < text.length; i++) {
    const ch = text[i]
    if (ch === '"' && quoted && text[i + 1] === '"') { field += '"'; i++ }
    else if (ch === '"') quoted = !quoted
    else if (ch === ',' && !quoted) { row.push(field); field = '' }
    else if ((ch === '\n' || ch === '\r') && !quoted) { if (ch === '\r' && text[i + 1] === '\n') i++; row.push(field); if (row.some(value => value.trim())) records.push(row); row = []; field = '' }
    else field += ch
  }
  row.push(field); if (row.some(value => value.trim())) records.push(row)
  return records
}
async function chooseFile(e: Event): Promise<void> {
  const file = (e.target as HTMLInputElement).files?.[0]; if (!file) return
  importName.value = file.name; importFile.value = file; importPairs.value = []; rawImportRows.value = []; importResults.value = []; importError.value = ''
  if (mock.value) {
    if (!file.name.toLowerCase().endsWith('.csv')) { importError.value = 'DEV 預覽暫用 CSV；正式端依 WinForms 支援 .xls/.xlsx。'; return }
    const text = await file.text(); const records = parseCsv(text.replace(/^\uFEFF/, ''))
    if (records.length < 2) { importError.value = '匯入檔沒有資料列。'; return }
    const pairs = records.slice(1).map(row => ({ sClassNo: (row[0] ?? '').trim(), mClassNo: (row[1] ?? '').trim(), description: (row[2] ?? '').trim() }))
    if (pairs.some(row => !row.sClassNo || !row.mClassNo)) { importError.value = '次分類編號與主分類編號必填；檔案前兩欄不可空白。'; return }
    rawImportRows.value = pairs
    importPairs.value = pairs.filter((item, index) => pairs.findIndex(other => other.sClassNo.toLocaleUpperCase() === item.sClassNo.toLocaleUpperCase() && other.mClassNo.toLocaleUpperCase() === item.mClassNo.toLocaleUpperCase()) === index)
    return
  }
  try {
    const body = new FormData(); body.append('file', file)
    importPairs.value = await request<ImportPair[]>('api/qc/s-class-basis/import/preview', { method: 'POST', body })
  } catch (error) { importError.value = error instanceof Error ? error.message : '讀取匯入 Excel 失敗。' }
}
async function downloadTemplate(): Promise<void> {
  try {
    let blob: Blob; let filename = 'SClassTemplate.csv'
    if (mock.value) blob = new Blob(['SClassNo,MClassNo,Description\r\nSC-EXAMPLE,DEV-MC-03,Example description\r\n'], { type: 'text/csv;charset=utf-8' })
    else {
      const response = await fetch(apiUrl('api/qc/s-class-basis/import/template'), { credentials: 'include' })
      if (response.status === 401) { window.location.assign(appBase + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
      if (!response.ok) throw new Error('QC402 匯入範本 API 尚未建立。')
      blob = await response.blob()
      const disposition = response.headers.get('Content-Disposition') ?? ''
      filename = disposition.match(/filename\*?=(?:UTF-8''|\")?([^\";]+)/i)?.[1] ?? 'SClassTemplate.xlsx'
    }
    const url = URL.createObjectURL(blob); const link = document.createElement('a'); link.href = url; link.download = decodeURIComponent(filename); link.click(); URL.revokeObjectURL(url)
  } catch (error) { importError.value = error instanceof Error ? error.message : '下載範本失敗。' }
}
async function startImport(): Promise<void> {
  if (!importFile.value || !importPairs.value.length) return
  if (!window.confirm('確定匯入 ' + importPairs.value.length + ' 筆次分類資料？')) return
  importing.value = true; importError.value = ''; importResults.value = []; importProgress.value = 0; cancelRequested.value = false
  try {
    if (!mock.value) {
      const body = new FormData(); body.append('file', importFile.value)
      importAbortController = new AbortController()
      importResults.value = await request<ImportResult[]>('api/qc/s-class-basis/import', { method: 'POST', body, signal: importAbortController.signal })
      importProgress.value = importResults.value.length
    } else {
      const store = readStore(); const groups = new Map<string, ImportPair[]>()
      for (const pair of rawImportRows.value) { const key = pair.sClassNo.toLocaleUpperCase(); groups.set(key, [...(groups.get(key) ?? []), pair]) }
      for (const [sClassNo, pairs] of groups) {
        if (cancelRequested.value) break
        const first = pairs[0]
        let result: ImportResult['result'] = 'fail'; let message = ''
        if (pairs.length > 1) message = '同一 SClassNo 有多筆資料。'
        else if (store.rows.some(row => row.sClassNo.toUpperCase() === sClassNo.toUpperCase())) message = '次分類編號已存在。'
        else if (!mClassOptions.value.some(row => row.issueState === 2 && row.mClassNo.toUpperCase() === first.mClassNo.toUpperCase())) message = '主分類不存在或尚未簽核。'
        else if (sClassNo.length > 30 || first.mClassNo.length > 30 || first.description.length > 255) message = '欄位長度超過資料表限制。'
        else {
          store.rows.push({ sClassNo: sClassNo.toUpperCase(), mClassNo: first.mClassNo.toUpperCase(), description: first.description || null, creator: props.currentUserNo || 'DEV001', createDate: now(), issueState: 0, dataStamp: 0, reviser: null, reviseDate: null })
          result = 'success'
        }
        for (const item of importPairs.value.filter(row => row.sClassNo.toUpperCase() === sClassNo)) importResults.value.push({ ...item, result, message })
        importProgress.value += importPairs.value.filter(row => row.sClassNo.toUpperCase() === sClassNo).length
        await new Promise<void>(resolve => window.setTimeout(resolve, 0))
      }
      writeStore(store)
    }
    if (importResults.value.some(result => result.result === 'success')) { await refresh(); notice.value = '匯入完成，成功 ' + importResults.value.filter(result => result.result === 'success').length + ' 筆。' }
  } catch (error) { importError.value = error instanceof Error ? error.message : '匯入次分類失敗。' }
  finally { importing.value = false; importAbortController = null }
}
function cancelImport(): void { cancelRequested.value = true; importAbortController?.abort() }
function date(value: string | null): string {
  if (!value) return ''
  const d = new Date(value); if (Number.isNaN(d.getTime())) return value
  return new Intl.DateTimeFormat('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false }).format(d)
}
function closeImport(): void { if (!importing.value) importOpen.value = false }
watch(state, () => setActive())
onMounted(() => { void (async () => { loading.value = true; try { await load(); setActive() } catch (e) { error.value = e instanceof Error ? e.message : '載入次分類失敗。' } try { await loadMClassOptions() } catch (e) { error.value = e instanceof Error ? e.message : '載入已簽核主分類清單失敗。' } finally { loading.value = false } })() })
</script>

<template>
  <main class="sclass-page">
    <header class="page-heading"><div><h1>次分類基本資料</h1><p>QC402 · SCLASS BASIS</p></div><span v-if="mock" class="dev-badge">DEV 假資料</span></header>
    <form class="query-panel" @submit.prevent="query">
      <div class="states"><label v-for="s in states" :key="s.value" class="state-option"><input v-model="state" type="radio" :value="s.value" :disabled="loading || busy"><span>{{ s.label }}</span></label></div>
      <label>次分類編號</label><input v-model="search" class="search-input" aria-label="次分類編號" :disabled="loading || busy"><button class="button" type="submit" :disabled="loading || busy">{{ loading ? '查詢中' : '查詢' }}</button>
    </form>
    <p v-if="error" class="message error" role="alert">{{ error }}</p><p v-if="notice" class="message notice" role="status">{{ notice }}</p>
    <section class="grid-panel"><div class="table-scroll"><table class="data-grid">
      <thead><tr><th class="check"><input type="checkbox" aria-label="全選" :checked="visible.length > 0 && selectedRows.length === visible.length" @change="selectAll"></th><th>次分類編號</th><th>主分類編號</th><th>說明</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th></tr></thead>
      <tbody><tr v-for="row in visible" :key="row.sClassNo" :class="{ active: active === row.sClassNo }" @click="selectRow(row)" @dblclick="openForm('edit', row)">
        <td class="check" @click.stop><input type="checkbox" :aria-label="'選取 ' + row.sClassNo" :checked="selected.includes(row.sClassNo)" @change="toggleRow(row)"></td><td class="key">{{ row.sClassNo }}</td><td>{{ row.mClassNo }}</td><td class="description">{{ row.description || '' }}</td><td>{{ row.creator || '' }}</td><td>{{ date(row.createDate) }}</td><td>{{ row.reviser || '' }}</td><td>{{ date(row.reviseDate) }}</td>
      </tr><tr v-if="!visible.length" class="empty"><td colspan="8">{{ loading ? '資料載入中…' : '查無資料' }}</td></tr></tbody>
    </table></div><aside class="actions">
      <button class="button primary" :disabled="busy || loading" @click="openForm('add')">新增(A)</button><button class="button" :disabled="busy || loading || !activeRow" @click="activeRow && openForm('edit', activeRow)">修改(E)</button><button class="button" :disabled="busy || loading || !selectedRows.length" @click="removeSelected">刪除(D)</button><button class="button" :disabled="busy || loading || !activeRow" @click="activeRow && openForm('copy', activeRow)">複製(C)</button><button class="button" :disabled="busy || loading || !selectedRows.length" @click="approveSelected">核准</button><button class="button" :disabled="busy || loading" @click="openImport">SClassImport</button><button class="button close" @click="emit('close')">關閉</button>
    </aside><footer class="grid-footer">資料總筆數：{{ visible.length }} <span v-if="busy">資料處理中…</span></footer></section>

    <div v-if="mode" class="backdrop" @click.self="closeForm"><section class="dialog sclass-form-dialog" role="dialog" aria-modal="true" :aria-label="formTitle">
      <header class="titlebar"><h2>{{ formTitle }}</h2><button class="x" type="button" @click="closeForm">×</button></header>
      <form class="editor" @submit.prevent="save(false)"><fieldset><legend>次分類主檔資料</legend><label><span>次分類編號<span class="required">*</span></span><input v-model="form.sClassNo" class="kd-field" maxlength="30" :readonly="mode === 'edit' || readOnly" @blur="form.sClassNo = form.sClassNo.toUpperCase()"></label></fieldset>
        <fieldset><legend>主分類及說明</legend><label><span>主分類編號<span class="required">*</span></span><span class="picker-control"><input :value="form.mClassNo" readonly class="kd-field"><button class="button picker-button" type="button" :disabled="readOnly" @click="openMClassLookup">…</button></span></label><label><span>說明</span><textarea v-model="form.description" class="kd-field" maxlength="255" :readonly="readOnly"></textarea></label></fieldset>
        <fieldset class="audit"><legend>建立/修改資訊</legend><label>建立人<input :value="mode === 'edit' ? (source?.creator || '') : (props.currentUserNo || 'DEV001')" readonly></label><label>建立日期<input :value="date(formCreateDate)" readonly></label><label>修改人<input :value="mode === 'edit' ? (source?.reviser || '') : ''" readonly></label><label>修改日期<input :value="mode === 'edit' ? date(source?.reviseDate ?? null) : ''" readonly></label></fieldset>
        <p v-if="formError" class="message error" role="alert">{{ formError }}</p>
      </form><footer class="dialog-actions"><button type="button" class="button primary" :disabled="saving || readOnly" @click="save(false)">確定</button><button type="button" class="button" @click="closeForm">關閉</button><button type="button" class="button" :disabled="saving || readOnly" @click="save(true)">套用</button></footer>
    </section></div>
    <div v-if="mClassLookupOpen" class="backdrop" @click.self="mClassLookupOpen = false"><section class="dialog lookup-dialog" role="dialog" aria-modal="true" aria-label="選擇已簽核主分類">
      <header class="titlebar"><h2>選擇主分類</h2><button class="x" type="button" @click="mClassLookupOpen = false">×</button></header>
      <div class="picker-search"><label><span>主分類編號／說明</span><input v-model="mClassLookupSearch" class="kd-field" autofocus></label></div>
      <div class="import-results"><table class="data-grid"><thead><tr><th>主分類編號</th><th>說明</th><th>狀態</th></tr></thead><tbody>
        <tr v-for="row in availableMClasses" :key="row.mClassNo" @dblclick="selectMClass(row)"><td class="key">{{ row.mClassNo }}</td><td>{{ row.description || '' }}</td><td>已簽核</td></tr>
        <tr v-if="!availableMClasses.length" class="empty"><td colspan="3">沒有可選的已簽核主分類</td></tr>
      </tbody></table></div>
      <footer class="dialog-actions"><button class="button" @click="mClassLookupOpen = false">關閉</button></footer>
    </section></div>
    <div v-if="importOpen" class="backdrop" @click.self="closeImport"><section class="dialog import-dialog" role="dialog" aria-modal="true" aria-label="SClassImport">
      <header class="titlebar"><h2>SClassImport</h2><button class="x" type="button" :disabled="importing" @click="closeImport">×</button></header>
      <p class="import-hint">Excel 第一個工作表：略過第 1 列，固定讀取第 1 欄 SClassNo、第 2 欄 MClassNo（必填）及第 3 欄 Description（可空），不檢查標題文字。預覽依 SClassNo、MClassNo 去除重複對；匯入時逐個 SClassNo 檢查，該編號有多列就回報重複。DEV 假資料只解析相同欄序的 CSV。</p>
      <div class="import-toolbar"><input type="file" accept=".csv,.xls,.xlsx" :disabled="importing" @change="chooseFile"><span>{{ importName || '尚未選擇檔案' }}</span><button class="button" type="button" :disabled="importing" @click="downloadTemplate">下載範本</button></div>
      <p v-if="importError" class="message error" role="alert">{{ importError }}</p>
      <p v-if="importing" class="import-progress">處理中 {{ importProgress }} / {{ importPairs.length }}</p>
      <div class="import-results"><table class="data-grid"><thead><tr><th>SClassNo</th><th>MClassNo</th><th>Description</th><th>Message</th></tr></thead><tbody>
        <tr v-for="(row, index) in (importResults.length ? importResults : importPairs)" :key="index"><td>{{ row.sClassNo }}</td><td>{{ row.mClassNo }}</td><td>{{ row.description }}</td><td v-if="'result' in row" :class="row.result === 'success' ? 'result-ok' : 'result-fail'">{{ row.result === 'success' ? 'success' : row.message }}</td><td v-else>待匯入</td></tr>
        <tr v-if="!importPairs.length && !importResults.length"><td colspan="4">選取 Excel 檔後預覽資料與逐筆匯入結果。</td></tr>
      </tbody></table></div>
      <footer class="dialog-actions"><button class="button primary" :disabled="importing || !importFile || !importPairs.length" @click="startImport">{{ importing ? '匯入中…' : '匯入' }}</button><button v-if="importing" class="button" @click="cancelImport">取消後續資料</button><button class="button" :disabled="importing" @click="closeImport">關閉</button></footer>
    </section></div>
  </main>
</template>

<style scoped>
.sclass-page{--line:#c8c4bb;--text:#30302e;--muted:#77746e;--field:#fff0ad;--orange:#f7a32b;display:flex;flex-direction:column;gap:8px;height:100%;min-height:0;padding:12px;color:var(--text);background:#f7f5ef;font:14px/1.35 "Segoe UI","Microsoft JhengHei",sans-serif}
.page-heading{display:flex;align-items:center;justify-content:space-between;min-height:42px}.page-heading h1{margin:0;font-size:20px;font-weight:600}.page-heading p{margin:2px 0 0;color:var(--muted);font-size:12px}.dev-badge{padding:4px 9px;border:1px solid #d7a044;background:#fff1bd;color:#77551a;font-size:12px}
.query-panel{display:flex;flex-wrap:wrap;align-items:center;gap:10px;min-height:46px;padding:6px 10px;border:1px solid var(--line);background:#fff}.states{display:flex;flex-wrap:wrap;gap:12px}.state-option{display:inline-flex;align-items:center;gap:5px;white-space:nowrap;cursor:pointer}.state-option input{accent-color:#e18b17}.search-input{width:190px}
input,textarea{box-sizing:border-box;min-height:30px;padding:5px 7px;border:1px solid #a9a69f;border-radius:2px;background:var(--field);color:var(--text);font:inherit}input:focus,textarea:focus{border-color:#d18a21;outline:2px solid rgb(209 138 33 / 20%)}input[readonly],input:disabled,textarea[readonly]{background:#ecebe7;color:#777}textarea{resize:vertical}.button{min-height:30px;padding:4px 11px;border:1px solid #bdb9b0;border-radius:3px;background:linear-gradient(#fff,#f0efec);color:#343331;font:inherit;white-space:nowrap;cursor:pointer}.button:hover:not(:disabled){border-color:#d08a23;background:#fff6df}.button:disabled{opacity:.48;cursor:not-allowed}.button.primary{border-color:#df8612;background:var(--orange);color:#fff}
.message{margin:0;padding:7px 10px;border:1px solid}.error{border-color:#d99b86;background:#fff0e9;color:#873820}.notice{border-color:#c9bc91;background:#fff8dc;color:#67551d}
.grid-panel{display:grid;grid-template-columns:minmax(0,1fr) 88px;grid-template-rows:minmax(0,1fr) auto;gap:7px 9px;flex:1;min-height:220px;padding:6px;border:1px solid var(--line);background:#fff}.table-scroll{min-width:0;min-height:0;overflow:auto;border:1px solid #c9c6bf}.data-grid{width:100%;border-collapse:separate;border-spacing:0;font-size:13px;white-space:nowrap}.data-grid th{position:sticky;top:0;z-index:1;padding:6px 8px;border-right:1px solid #e1ded8;border-bottom:1px solid #c9c6bf;background:#f5cd64;color:#39342a;text-align:left}.data-grid td{height:27px;padding:3px 7px;border-right:1px solid #e1ded8;border-bottom:1px solid #e1ded8}.data-grid tbody tr{cursor:pointer}.data-grid tbody tr:hover{background:#fff8e3}.data-grid tbody tr.active{background:#ffedaa}.data-grid input[type=checkbox]{width:15px;min-height:15px;height:15px;margin:0;accent-color:#d98717}.check{width:28px;text-align:center!important}.key{font-weight:600}.description{min-width:180px;white-space:pre-wrap}.empty{color:#777;text-align:center}.actions{display:flex;flex-direction:column;gap:7px}.actions .button{min-height:32px}.close{margin-top:auto}.grid-footer{grid-column:1/-1;min-height:20px;padding:3px 2px 0;color:var(--muted);font-size:12px}.grid-footer span{margin-left:12px}
.backdrop{position:fixed;z-index:50;inset:0;display:flex;align-items:center;justify-content:center;padding:18px;background:rgb(28 27 25 / 42%)}.dialog{display:flex;flex-direction:column;width:min(580px,97vw);max-height:94vh;overflow:auto;border:1px solid #b9b4aa;background:#f8f6f1;box-shadow:0 12px 36px rgb(0 0 0 / 24%)}.titlebar{position:sticky;top:0;z-index:2;display:flex;align-items:center;justify-content:space-between;padding:10px 15px;border-bottom:1px solid #d2cec5;background:#f2efe8}.titlebar h2{margin:0;font-size:17px;font-weight:600}.x{border:0;background:none;font-size:23px;cursor:pointer}.editor{display:flex;flex-direction:column;gap:9px;padding:12px}.editor fieldset{min-width:0;border:1px solid #c9c4ba;padding:9px}.editor legend{padding:0 5px;color:#5a564f}.editor label{display:flex;flex-direction:column;gap:5px}.editor label>span{font-size:13px}.editor fieldset:first-child label{max-width:440px}.kd-field{background:var(--field)}.editor textarea{min-height:120px}.audit{display:grid;grid-template-columns:1fr 1fr;gap:8px 12px}.audit input{width:100%;background:#ecebe7}.required{padding-left:3px;color:#a33a25}.dialog-actions{position:sticky;bottom:0;display:flex;justify-content:flex-end;gap:8px;padding:8px 12px 12px;border-top:1px solid #ddd9d1;background:#f8f6f1}.import-dialog{width:min(900px,97vw);padding:0 14px 12px}.import-hint{margin:10px 0;color:#68645c}.import-toolbar{display:flex;align-items:center;gap:9px;padding:7px 0}.import-toolbar span{flex:1;min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.import-results{min-height:170px;max-height:50vh;overflow:auto;border:1px solid #c9c6bf}.import-results .data-grid{white-space:normal}.import-progress{margin:3px 0;color:#6a6253;font-size:12px}.result-ok{color:#28643d}.result-fail{color:#a33a25}
.picker-control{display:flex;align-items:center;gap:5px}.picker-control input{flex:1;min-width:0}.picker-button{width:36px;padding:3px}.sclass-form-dialog{width:min(508px,97vw)}.sclass-form-dialog .editor{gap:5px;padding:8px}.sclass-form-dialog .editor textarea{min-height:63px;height:63px}.picker-search{padding:10px 14px}.picker-search label{display:flex;align-items:center;gap:10px}.picker-search label span{white-space:nowrap}.picker-search input{flex:1}.lookup-dialog{width:min(700px,97vw);padding-bottom:12px}.lookup-dialog .import-results{margin:0 14px;max-height:55vh}.lookup-dialog .data-grid tbody tr{cursor:pointer}.lookup-dialog .data-grid tbody tr:hover{background:#fff0ad}
@media(max-width:620px){.query-panel{align-items:flex-start}.states{width:100%}.audit{grid-template-columns:1fr}.picker-search label{align-items:flex-start;flex-direction:column}}
</style>
