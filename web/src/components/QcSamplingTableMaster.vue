<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type Mode = 'add' | 'edit' | 'copy' | null
type SearchField = 'SamplingTable' | 'MinNum' | 'MaxNum'
interface Row {
  samplingTable: string
  minNum: number
  maxNum: number
  sampleNum: number
  rejectNum: number | null
  creator: string | null
  createDate: string | null
  reviser: string | null
  reviseDate: string | null
  dataStamp: number | null
}
interface FormValues { samplingTable: string; minNum: string; maxNum: string; sampleNum: string; rejectNum: string }
const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const storageKey = 'ymes-dev-mock:qc-sampling-tables:v1'
const seed: Row[] = [
  { samplingTable: 'DEV-AQL-1.0', minNum: 1, maxNum: 8, sampleNum: 2, rejectNum: 0, creator: 'DEV001', createDate: '2026-11-10T08:00:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
  { samplingTable: 'DEV-AQL-1.0', minNum: 9, maxNum: 15, sampleNum: 3, rejectNum: 0, creator: 'DEV001', createDate: '2026-11-10T08:01:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
  { samplingTable: 'DEV-AQL-1.0', minNum: 16, maxNum: 25, sampleNum: 5, rejectNum: 1, creator: 'DEV001', createDate: '2026-11-10T08:02:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-11T09:00:00+08:00', dataStamp: 1 },
  { samplingTable: 'DEV-AQL-2.5', minNum: 1, maxNum: 10, sampleNum: 3, rejectNum: 0, creator: 'DEV002', createDate: '2026-11-11T10:00:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
]
const rows = ref<Row[]>([])
const searchField = ref<SearchField>('SamplingTable')
const searchValue = ref('')
const appliedSearchField = ref<SearchField>('SamplingTable')
const appliedSearchValue = ref('')
const selectedKeys = ref<string[]>([])
const activeKey = ref('')
const loading = ref(false)
const busy = ref(false)
const saving = ref(false)
const error = ref('')
const notice = ref('')
const mode = ref<Mode>(null)
const dirty = ref(false)
const source = ref<Row | null>(null)
const form = ref<FormValues>({ samplingTable: '', minNum: '', maxNum: '', sampleNum: '', rejectNum: '' })
const formError = ref('')
const formCreateDate = ref('')
const copyTableOpen = ref(false)
const copyTableSource = ref('')
const copyTableTarget = ref('')
const copyTableError = ref('')
const copyTableSaving = ref(false)
const visibleRows = computed(() => {
  const value = appliedSearchValue.value.trim().toLowerCase()
  return rows.value.filter(row => {
    if (!value) return true
    const candidate = String(row[appliedSearchField.value]).toLowerCase()
    return candidate.includes(value)
  }).sort((a, b) => a.samplingTable.localeCompare(b.samplingTable) || a.minNum - b.minNum || a.maxNum - b.maxNum)
})
const selectedRows = computed(() => visibleRows.value.filter(row => selectedKeys.value.includes(keyOf(row))))
const activeRow = computed(() => rows.value.find(row => keyOf(row) === activeKey.value) ?? null)
const dialogTitle = computed(() => '抽樣表格（' + (mode.value === 'add' ? '新增' : mode.value === 'copy' ? '複製' : '修改') + '）')
const nowText = ref(new Date().toLocaleString('zh-TW', { hour12: false }))

function keyOf(row: Pick<Row, 'samplingTable' | 'minNum' | 'maxNum'>): string {
  return row.samplingTable + '|' + row.minNum + '|' + row.maxNum
}
function clone<T>(value: T): T { return structuredClone(value) }
function nowIso(): string { return new Date().toISOString() }
function readStore(): { rows: Row[] } {
  try {
    const raw = window.localStorage.getItem(storageKey)
    if (raw) {
      const parsed = JSON.parse(raw) as { rows?: Row[] }
      if (Array.isArray(parsed.rows)) return parsed as { rows: Row[] }
    }
  } catch { /* replace invalid development-only data */ }
  const data = { rows: clone(seed) }
  writeStore(data)
  return data
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
  if (response.status === 401) {
    window.location.assign(appBase + '/')
    throw new Error('登入狀態已逾期，請重新登入。')
  }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    if (response.status === 404) throw new Error('QC101 API 尚未建立；請依 todolist_homepc.md 完成資料表與 API。')
    throw new Error(problem?.detail || problem?.message || problem?.title || 'API 回應錯誤（' + response.status + '）。')
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
async function load(): Promise<void> {
  if (mock.value) {
    rows.value = clone(readStore().rows)
    return
  }
  const query = new URLSearchParams()
  if (appliedSearchValue.value) {
    query.set('searchFieldName', appliedSearchField.value)
    query.set('searchFieldValue', appliedSearchValue.value)
  }
  const suffix = query.toString()
  rows.value = await request<Row[]>('api/qc/sampling-tables' + (suffix ? '?' + suffix : ''))
}
function setActive(prefer = ''): void {
  const row = visibleRows.value.find(item => keyOf(item) === prefer) ?? visibleRows.value[0]
  activeKey.value = row ? keyOf(row) : ''
  selectedKeys.value = row ? [keyOf(row)] : []
}
async function query(): Promise<void> {
  appliedSearchField.value = searchField.value
  appliedSearchValue.value = searchValue.value.trim()
  loading.value = true
  error.value = ''
  notice.value = ''
  try { await load(); setActive() }
  catch (reason) { error.value = reason instanceof Error ? reason.message : '查詢抽樣表格失敗。' }
  finally { loading.value = false }
}
async function refresh(prefer = ''): Promise<void> { await load(); setActive(prefer) }
function selectRow(row: Row): void { activeKey.value = keyOf(row); selectedKeys.value = [activeKey.value] }
function toggleRow(row: Row): void {
  const key = keyOf(row)
  selectedKeys.value = selectedKeys.value.includes(key) ? selectedKeys.value.filter(item => item !== key) : [...selectedKeys.value, key]
  activeKey.value = key
}
function selectAll(event: Event): void {
  selectedKeys.value = (event.target as HTMLInputElement).checked ? visibleRows.value.map(keyOf) : []
}
function openForm(next: Exclude<Mode, null>, row: Row | null = null): void {
  nowText.value = new Date().toLocaleString('zh-TW', { hour12: false })
  source.value = row ? clone(row) : null
  form.value = {
    samplingTable: row?.samplingTable ?? '',
    minNum: row ? String(row.minNum) : '',
    maxNum: row ? String(row.maxNum) : '',
    sampleNum: row ? String(row.sampleNum) : '',
    rejectNum: row?.rejectNum === null || row?.rejectNum === undefined ? '' : String(row.rejectNum),
  }
  formCreateDate.value = row?.createDate ?? nowIso()
  formError.value = ''
  dirty.value = false
  mode.value = next
}
function closeForm(): void { mode.value = null; source.value = null; formError.value = ''; dirty.value = false }
function focusNext(event: KeyboardEvent): void {
  const current = event.currentTarget as HTMLInputElement
  const fields = Array.from(current.form?.querySelectorAll<HTMLInputElement>('input:not([readonly])') ?? [])
  fields[fields.indexOf(current) + 1]?.focus()
}
function parseNumber(value: string): number | null {
  const normalized = value.trim()
  if (!/^-?\d+(?:\.\d+)?$/.test(normalized)) return null
  const parsed = Number(normalized)
  return Number.isFinite(parsed) && Math.abs(parsed) <= 99999999.9999 ? parsed : null
}
function validate(): string {
  const table = form.value.samplingTable.trim()
  const min = parseNumber(form.value.minNum)
  const max = parseNumber(form.value.maxNum)
  const sample = parseNumber(form.value.sampleNum)
  const reject = form.value.rejectNum.trim() ? parseNumber(form.value.rejectNum) : null
  if (!table) return 'SamplingTable 為必填。'
  if (table.length > 25) return 'SamplingTable 不可超過 25 個字元。'
  if (min === null) return 'MinNum 為必填，且須為有效數字。'
  if (max === null) return 'MaxNum 為必填，且須為有效數字。'
  if (min > max) return 'MaxNum 必須大於或等於 MinNum。'
  if (sample === null || sample < 0) return 'SampleNum 為必填，且須為非負數。'
  if (!Number.isInteger(sample)) return 'SampleNum 必須為整數。'
  if (form.value.rejectNum.trim() && reject === null) return 'RejectNum 必須為有效數字。'
  if (reject !== null && reject < 0) return 'RejectNum 不可小於 0。'
  if (reject !== null && reject > sample) return 'RejectNum 必須小於或等於 SampleNum。'
  if (reject !== null && !Number.isInteger(reject)) return 'RejectNum 必須為整數。'
  if (mode.value !== 'edit' && rows.value.some(row => row.samplingTable.toUpperCase() === table.toUpperCase() && row.minNum === min && row.maxNum === max)) return '相同 SamplingTable、MinNum、MaxNum 的資料已存在。'
  return ''
}
function normalizedNumber(value: string): number {
  return Number(value.trim())
}
async function save(keepOpen: boolean): Promise<void> {
  formError.value = validate()
  if (formError.value || !mode.value) return
  const currentMode = mode.value
  const original = source.value
  const samplingTable = form.value.samplingTable.trim().toUpperCase()
  const minNum = normalizedNumber(form.value.minNum)
  const maxNum = normalizedNumber(form.value.maxNum)
  const sampleNum = normalizedNumber(form.value.sampleNum)
  const rejectNum = form.value.rejectNum.trim() ? normalizedNumber(form.value.rejectNum) : null
  saving.value = true
  try {
    if (mock.value) {
      const data = readStore()
      if (currentMode === 'edit') {
        const index = data.rows.findIndex(row => original && keyOf(row) === keyOf(original))
        if (index < 0) throw new Error('資料已不存在，請重新查詢。')
        data.rows[index] = { ...data.rows[index], sampleNum, rejectNum, reviser: props.currentUserNo || 'DEV001', reviseDate: nowIso(), dataStamp: (data.rows[index].dataStamp ?? 0) + 1 }
      } else data.rows.push({ samplingTable, minNum, maxNum, sampleNum, rejectNum, creator: props.currentUserNo || 'DEV001', createDate: formCreateDate.value, reviser: null, reviseDate: null, dataStamp: 0 })
      writeStore(data)
    } else if (currentMode === 'edit') {
      const row = original!
      const query = new URLSearchParams()
      query.set('dataStampIsNull', row.dataStamp === null ? 'true' : 'false')
      if (row.dataStamp !== null) query.set('dataStamp', String(row.dataStamp))
      await request('api/qc/sampling-tables/' + encodeURIComponent(row.samplingTable) + '/' + encodeURIComponent(String(row.minNum)) + '/' + encodeURIComponent(String(row.maxNum)) + (query.toString() ? '?' + query.toString() : ''), {
        method: 'PUT', body: JSON.stringify({ sampleNum, rejectNum }),
      })
    } else await request('api/qc/sampling-tables', { method: 'POST', body: JSON.stringify({ samplingTable, minNum, maxNum, sampleNum, rejectNum }) })
    await refresh(samplingTable + '|' + minNum + '|' + maxNum)
    dirty.value = false
    if (keepOpen && currentMode !== 'edit') {
      const nextMin = maxNum + 1
      form.value.samplingTable = samplingTable
      form.value.minNum = Number.isInteger(nextMin) ? String(nextMin) : String(Number(nextMin.toFixed(4)))
      form.value.maxNum = ''
      notice.value = '資料已套用；可直接輸入下一個區間。'
    } else if (keepOpen) {
      source.value = activeRow.value ? clone(activeRow.value) : null
      notice.value = '資料已套用。'
    } else closeForm()
  } catch (reason) { formError.value = reason instanceof Error ? reason.message : '儲存抽樣表格失敗。' }
  finally { saving.value = false }
}
async function removeSelected(): Promise<void> {
  const items = [...selectedRows.value]
  if (!items.length) return
  if (!window.confirm('確定刪除選取的 ' + items.length + ' 筆抽樣表格資料？')) return
  busy.value = true
  error.value = ''
  let removed = 0
  try {
    for (const row of items) {
      if (mock.value) {
        const data = readStore()
        data.rows = data.rows.filter(item => keyOf(item) !== keyOf(row))
        writeStore(data)
      } else {
        const query = new URLSearchParams()
        query.set('dataStampIsNull', row.dataStamp === null ? 'true' : 'false')
        if (row.dataStamp !== null) query.set('dataStamp', String(row.dataStamp))
        await request('api/qc/sampling-tables/' + encodeURIComponent(row.samplingTable) + '/' + encodeURIComponent(String(row.minNum)) + '/' + encodeURIComponent(String(row.maxNum)) + (query.toString() ? '?' + query.toString() : ''), { method: 'DELETE' })
      }
      removed++
    }
    await refresh()
    notice.value = '已刪除 ' + removed + ' 筆資料。'
  } catch (reason) {
    error.value = (reason instanceof Error ? reason.message : '刪除抽樣表格失敗。') + (removed ? ' 前 ' + removed + ' 筆已完成。' : '')
    try { await refresh() } catch { /* retain the original failure */ }
  } finally { busy.value = false }
}
function openTableCopy(): void {
  if (!activeRow.value) return
  copyTableSource.value = activeRow.value.samplingTable
  copyTableTarget.value = ''
  copyTableError.value = ''
  copyTableOpen.value = true
}
async function copyTable(): Promise<void> {
  const target = copyTableTarget.value.trim().toUpperCase()
  if (!target) { copyTableError.value = '請輸入目的 SamplingTable。'; return }
  if (target.length > 25) { copyTableError.value = '目的 SamplingTable 不可超過 25 個字元。'; return }
  copyTableSaving.value = true
  copyTableError.value = ''
  try {
    let added = 0
    if (mock.value) {
      const data = readStore()
      const sourceRows = data.rows.filter(row => row.samplingTable === copyTableSource.value)
      for (const row of sourceRows) {
        if (data.rows.some(existing => existing.samplingTable.toUpperCase() === target && existing.minNum === row.minNum && existing.maxNum === row.maxNum)) continue
        data.rows.push({ ...row, samplingTable: target, creator: props.currentUserNo || 'DEV001', createDate: nowIso(), reviser: null, reviseDate: null, dataStamp: 0 })
        added++
      }
      writeStore(data)
    } else {
      const result = await request<{ insertedCount: number; skippedCount?: number; error?: string }>('api/qc/sampling-tables/copy', {
        method: 'POST', body: JSON.stringify({ sourceSamplingTable: copyTableSource.value, targetSamplingTable: target }),
      })
      added = result.insertedCount
      if (result.error) notice.value = '複製部分完成，已新增 ' + added + ' 個區間；' + result.error
      else notice.value = '整張表複製完成，新增 ' + added + ' 個區間、略過 ' + (result.skippedCount ?? 0) + ' 個重複區間。'
    }
    await refresh()
    copyTableOpen.value = false
    if (mock.value) notice.value = '整張表複製完成，新增 ' + added + ' 個區間。'
  } catch (reason) { copyTableError.value = reason instanceof Error ? reason.message : '複製整張抽樣表失敗。' }
  finally { copyTableSaving.value = false }
}
function formatDate(value: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return date.toLocaleString('zh-TW', { hour12: false })
}
onMounted(async () => {
  nowText.value = new Date().toLocaleString('zh-TW', { hour12: false })
  loading.value = true
  try { await load(); setActive() }
  catch (reason) { error.value = reason instanceof Error ? reason.message : '載入抽樣表格失敗。' }
  finally { loading.value = false }
})
</script>

<template>
  <section class="sampling-page">
    <header class="page-heading"><h1>抽樣表格</h1><span v-if="mock" class="dev-badge">DEV 假資料</span></header>
    <div class="query-panel">
      <label class="search-label" for="sampling-search-field">抽樣表格</label>
      <select id="sampling-search-field" v-model="searchField" class="search-field">
        <option value="SamplingTable">SamplingTable</option><option value="MinNum">MinNum</option><option value="MaxNum">MaxNum</option>
      </select>
      <input v-model="searchValue" class="search-value" aria-label="查詢值" @keydown.enter="query">
      <span class="percent">%</span>
      <button class="button" type="button" :disabled="loading" @click="query">查詢</button>
    </div>
    <p v-if="error" class="message error" role="alert">{{ error }}</p>
    <p v-if="notice" class="message notice" role="status">{{ notice }}</p>
    <div class="grid-panel">
      <div class="table-scroll">
        <table class="data-grid">
          <thead><tr>
            <th class="check"><input type="checkbox" aria-label="全選" :checked="visibleRows.length > 0 && selectedKeys.length === visibleRows.length" @change="selectAll"></th>
            <th>SamplingTable</th><th class="numeric">MinNum</th><th class="numeric">MaxNum</th><th class="numeric">SampleNum</th><th class="numeric">RejectNum</th>
            <th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th>
          </tr></thead>
          <tbody>
            <tr v-for="row in visibleRows" :key="keyOf(row)" :class="{ active: activeKey === keyOf(row), selected: selectedKeys.includes(keyOf(row)) }" @click="selectRow(row)">
              <td class="check"><input type="checkbox" :checked="selectedKeys.includes(keyOf(row))" :aria-label="'選取 ' + row.samplingTable + ' ' + row.minNum + '-' + row.maxNum" @click.stop @change="toggleRow(row)"></td>
              <td class="key">{{ row.samplingTable }}</td><td class="numeric">{{ row.minNum }}</td><td class="numeric">{{ row.maxNum }}</td><td class="numeric">{{ row.sampleNum }}</td><td class="numeric">{{ row.rejectNum ?? '' }}</td>
              <td>{{ row.creator ?? '' }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser ?? '' }}</td><td>{{ formatDate(row.reviseDate) }}</td>
            </tr>
            <tr v-if="!visibleRows.length"><td colspan="10" class="empty">{{ loading ? '載入中…' : '沒有符合條件的資料。' }}</td></tr>
          </tbody>
        </table>
      </div>
      <aside class="actions">
        <button class="button primary" type="button" :disabled="busy || saving" @click="openForm('add')">新增(A)</button>
        <button class="button" type="button" :disabled="!activeRow || busy" @click="openForm('edit', activeRow)">修改(E)</button>
        <button class="button" type="button" :disabled="!activeRow || busy" @click="openForm('copy', activeRow)">複製(C)</button>
        <button class="button" type="button" :disabled="!selectedRows.length || busy" @click="removeSelected">刪除(D)</button>
        <button class="button" type="button" :disabled="!activeRow || busy" @click="openTableCopy">CopyTable(S)</button>
        <button class="button close" type="button" @click="emit('close')">關閉(X)</button>
      </aside>
      <footer class="grid-footer">資料總筆數 {{ visibleRows.length }}<span>已選 {{ selectedRows.length }} 筆</span><span class="clock">{{ nowText }}</span></footer>
    </div>

    <Teleport to="body">
      <div v-if="mode" class="backdrop" @mousedown.self="closeForm">
        <section class="dialog definition-dialog" role="dialog" aria-modal="true" :aria-label="dialogTitle">
          <header class="titlebar"><h2>{{ dialogTitle }}</h2><button class="x" type="button" aria-label="關閉" @click="closeForm">×</button></header>
          <form class="editor" @submit.prevent="save(false)">
            <fieldset class="sampling-fields"><legend>Sampling Table</legend>
              <label><span>SamplingTable<span class="required">*</span></span><input v-model="form.samplingTable" class="kd-field" maxlength="25" :readonly="mode === 'edit'" @input="dirty = true" @keydown.enter.prevent="focusNext" @blur="form.samplingTable = form.samplingTable.toUpperCase()"></label>
              <label><span>MinNum<span class="required">*</span></span><input v-model="form.minNum" class="kd-field numeric" inputmode="decimal" maxlength="20" :readonly="mode === 'edit'" @input="dirty = true" @keydown.enter.prevent="focusNext"></label>
              <label><span>MaxNum<span class="required">*</span></span><input v-model="form.maxNum" class="kd-field numeric" inputmode="decimal" maxlength="20" :readonly="mode === 'edit'" @input="dirty = true" @keydown.enter.prevent="focusNext"></label>
              <label><span>SampleNum<span class="required">*</span></span><input v-model="form.sampleNum" class="kd-field numeric" inputmode="numeric" maxlength="12" @input="dirty = true" @keydown.enter.prevent="focusNext"></label>
              <label><span>RejectNum</span><input v-model="form.rejectNum" class="kd-field numeric" inputmode="numeric" maxlength="12" @input="dirty = true" @keydown.enter.prevent="focusNext"></label>
            </fieldset>
            <fieldset class="audit"><legend>基本資料</legend>
              <label><span>建立人</span><input :value="mode === 'edit' ? (source?.creator || '') : (currentUserNo || 'DEV001')" readonly></label>
              <label><span>建立日期</span><input :value="formatDate(mode === 'edit' ? (source?.createDate ?? null) : formCreateDate)" readonly></label>
              <label><span>修改人</span><input :value="mode === 'edit' ? (currentUserNo || 'DEV001') : ''" readonly></label>
              <label><span>修改日期</span><input :value="mode === 'edit' ? nowText : ''" readonly></label>
            </fieldset>
            <p v-if="formError" class="message error" role="alert">{{ formError }}</p>
            <footer class="dialog-actions">
              <button class="button primary" type="submit" :disabled="saving">確定</button>
              <button class="button" type="button" :disabled="saving" @click="closeForm">關閉</button>
              <button class="button" type="button" :disabled="saving || !dirty" @click="save(true)">套用</button>
            </footer>
          </form>
        </section>
      </div>
      <div v-if="copyTableOpen" class="backdrop" @mousedown.self="copyTableOpen = false">
        <section class="dialog table-copy-dialog" role="dialog" aria-modal="true" aria-label="複製整張抽樣表">
          <header class="titlebar"><h2>SamplingTableCopy</h2><button class="x" type="button" aria-label="關閉" @click="copyTableOpen = false">×</button></header>
          <form class="editor" @submit.prevent="copyTable">
            <fieldset><legend>Sampling Table</legend>
              <label><span>From</span><input :value="copyTableSource" class="kd-field" readonly></label>
              <label><span>To<span class="required">*</span></span><input v-model="copyTableTarget" class="kd-field" maxlength="25" @blur="copyTableTarget = copyTableTarget.toUpperCase()"></label>
              <p class="copy-note">目的抽樣表已存在的相同 MinNum／MaxNum 區間會略過。</p>
            </fieldset>
            <p v-if="copyTableError" class="message error" role="alert">{{ copyTableError }}</p>
            <footer class="dialog-actions"><button class="button primary" type="submit" :disabled="copyTableSaving">確定</button><button class="button" type="button" @click="copyTableOpen = false">關閉</button></footer>
          </form>
        </section>
      </div>
    </Teleport>
  </section>
</template>

<style scoped>
.sampling-page{--line:#c8c4bb;--text:#30302e;--muted:#77746e;--field:#fff0ad;--orange:#f7a32b;display:flex;flex-direction:column;gap:8px;height:100%;min-height:0;padding:12px;color:var(--text);background:#f7f5ef;font:14px/1.35 "Segoe UI","Microsoft JhengHei",sans-serif}
.page-heading{display:flex;align-items:center;justify-content:space-between;min-height:42px}.page-heading h1{margin:0;font-size:20px;font-weight:600}.dev-badge{padding:4px 9px;border:1px solid #d7a044;background:#fff1bd;color:#77551a;font-size:12px}
.query-panel{display:flex;flex-wrap:wrap;align-items:center;gap:8px;min-height:46px;padding:6px 10px;border:1px solid var(--line);background:#fff}.search-label{white-space:nowrap}.search-field{width:190px;background:#fff}.search-value{width:190px}.percent{margin-left:-4px}
input,select{box-sizing:border-box;min-height:30px;padding:5px 7px;border:1px solid #a9a69f;border-radius:2px;background:#fff;color:var(--text);font:inherit}input:focus,select:focus{border-color:#d18a21;outline:2px solid rgb(209 138 33 / 20%)}input[readonly],input:disabled{background:#ecebe7;color:#777}.numeric{text-align:right;font-variant-numeric:tabular-nums}
.button{min-height:30px;padding:4px 11px;border:1px solid #bdb9b0;border-radius:3px;background:linear-gradient(#fff,#f0efec);color:#343331;font:inherit;white-space:nowrap;cursor:pointer}.button:hover:not(:disabled){border-color:#d08a23;background:#fff6df}.button:disabled{opacity:.48;cursor:not-allowed}.button.primary{border-color:#df8612;background:var(--orange);color:#fff}
.message{margin:0;padding:7px 10px;border:1px solid}.error{border-color:#d99b86;background:#fff0e9;color:#873820}.notice{border-color:#c9bc91;background:#fff8dc;color:#67551d}
.grid-panel{display:grid;grid-template-columns:minmax(0,1fr) 88px;grid-template-rows:minmax(0,1fr) auto;gap:7px 9px;flex:1;min-height:220px;padding:6px;border:1px solid var(--line);background:#fff}.table-scroll{min-width:0;min-height:0;overflow:auto;border:1px solid #c9c6bf}.data-grid{width:100%;border-collapse:separate;border-spacing:0;font-size:13px;white-space:nowrap}.data-grid th{position:sticky;top:0;z-index:1;padding:6px 8px;border-right:1px solid #e1ded8;border-bottom:1px solid #c9c6bf;background:#f5cd64;color:#39342a;text-align:left}.data-grid td{height:27px;padding:3px 7px;border-right:1px solid #e1ded8;border-bottom:1px solid #e1ded8}.data-grid tbody tr{cursor:pointer}.data-grid tbody tr:hover{background:#fff8e3}.data-grid tbody tr.active,.data-grid tbody tr.selected{background:#ffedaa}.data-grid input[type=checkbox]{width:15px;min-height:15px;height:15px;margin:0;accent-color:#d98717}.check{width:28px;text-align:center!important}.key{font-weight:600}.empty{color:#777;text-align:center}.actions{display:flex;flex-direction:column;gap:7px}.actions .button{min-height:32px;padding:4px 6px}.close{margin-top:auto}.grid-footer{grid-column:1/-1;display:flex;gap:12px;min-height:20px;padding:3px 2px 0;color:var(--muted);font-size:12px}.clock{margin-left:auto}
.backdrop{position:fixed;z-index:50;inset:0;display:flex;align-items:center;justify-content:center;padding:18px;background:rgb(28 27 25 / 42%)}.dialog{display:flex;flex-direction:column;max-height:94vh;overflow:auto;border:1px solid #b9b4aa;background:#f8f6f1;box-shadow:0 12px 36px rgb(0 0 0 / 24%)}.definition-dialog{width:min(484px,97vw)}.table-copy-dialog{width:min(464px,97vw)}.titlebar{position:sticky;top:0;z-index:2;display:flex;align-items:center;justify-content:space-between;padding:10px 15px;border-bottom:1px solid #d2cec5;background:#f2efe8}.titlebar h2{margin:0;font-size:17px;font-weight:600}.x{border:0;background:none;font-size:23px;cursor:pointer}.editor{display:flex;flex-direction:column;gap:9px;padding:12px}.editor fieldset{min-width:0;border:1px solid #c9c4ba;padding:9px}.editor legend{padding:0 5px;color:#5a564f}.editor label{display:grid;grid-template-columns:105px minmax(0,1fr);align-items:center;gap:8px;margin:4px 0}.editor label>span{font-size:13px;text-align:right}.sampling-fields input{width:136px;background:var(--field)}.sampling-fields label{grid-template-columns:96px 136px;justify-content:start;margin:0 0 9px}.sampling-fields label>span{text-align:right}.audit{display:grid;grid-template-columns:1fr 1fr;gap:4px 10px}.audit label{grid-template-columns:58px minmax(0,1fr);margin:0}.audit input{width:100%;background:#ecebe7}.required{padding-left:3px;color:#a33a25}.dialog-actions{position:sticky;bottom:0;display:flex;justify-content:flex-end;gap:8px;padding:8px 12px 12px;border-top:1px solid #ddd9d1;background:#f8f6f1}.copy-note{margin:8px 0 0;color:#77746e;font-size:12px}
@media(max-width:700px){.grid-panel{grid-template-columns:minmax(0,1fr);grid-template-rows:minmax(220px,1fr) auto auto}.actions{display:grid;grid-template-columns:repeat(3,minmax(0,1fr))}.close{margin-top:0}.clock{display:none}.sampling-fields label{grid-template-columns:96px minmax(0,1fr)}.sampling-fields input{width:100%}}
</style>
