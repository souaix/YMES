<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'
import { apiErrorMessage } from '../utils/apiError'

type SamplingType = '0' | '1'
type FormMode = 'add' | 'edit' | 'copy' | null
interface SamplingRow {
  samplingType: SamplingType
  minNum: number
  maxNum: number
  sLevel: string
  sampleNum: number | null
  rejectNum: number | null
  kValue: number | null
  fValue: number | null
  creator: string | null
  createDate: string | null
  reviser: string | null
  reviseDate: string | null
  dataStamp: number | null
}
interface SamplingForm {
  sLevel: string
  minNum: string
  maxNum: string
  sampleNum: string
  rejectNum: string
  kValue: string
  fValue: string
}

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()
const isMock = computed(() => import.meta.env.DEV && props.developmentPreview)
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const storageKey = 'ymes-dev-mock:qc-sampling-1916:v1'
const seed: SamplingRow[] = [
  { samplingType: '0', minNum: 1, maxNum: 50, sLevel: 'II', sampleNum: 8, rejectNum: 1, kValue: null, fValue: null, creator: 'DEV001', createDate: '2026-11-10T08:00:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
  { samplingType: '0', minNum: 51, maxNum: 90, sLevel: 'II', sampleNum: 13, rejectNum: 1, kValue: null, fValue: null, creator: 'DEV001', createDate: '2026-11-10T08:01:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-11T09:00:00+08:00', dataStamp: 1 },
  { samplingType: '0', minNum: 1, maxNum: 90, sLevel: 'III', sampleNum: 13, rejectNum: 2, kValue: null, fValue: null, creator: 'DEV001', createDate: '2026-11-10T08:02:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
  { samplingType: '1', minNum: 1, maxNum: 50, sLevel: 'II', sampleNum: 8, rejectNum: 1, kValue: 0.65, fValue: 0.12, creator: 'DEV002', createDate: '2026-11-11T10:00:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
  { samplingType: '1', minNum: 51, maxNum: 90, sLevel: 'II', sampleNum: 13, rejectNum: 1, kValue: 0.75, fValue: 0.18, creator: 'DEV002', createDate: '2026-11-11T10:01:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
]
const rows = ref<SamplingRow[]>([])
const samplingType = ref<SamplingType>('0')
const selectedKeys = ref<string[]>([])
const activeKey = ref('')
const loading = ref(false)
const saving = ref(false)
const error = ref('')
const notice = ref('')
const formMode = ref<FormMode>(null)
const original = ref<SamplingRow | null>(null)
const dirty = ref(false)
const formError = ref('')
const form = ref<SamplingForm>(emptyForm())
const createdBy = ref('')
const createdAt = ref('')
const revisedBy = ref('')
const revisedAt = ref('')

function emptyForm(): SamplingForm {
  return { sLevel: '', minNum: '', maxNum: '', sampleNum: '', rejectNum: '1', kValue: '', fValue: '' }
}
function keyOf(row: Pick<SamplingRow, 'samplingType' | 'minNum' | 'maxNum' | 'sLevel'>): string {
  return [row.samplingType, row.minNum, row.maxNum, row.sLevel.toUpperCase()].join('|')
}
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function nowIso(): string { return new Date().toISOString() }
function readStore(): { rows: SamplingRow[] } {
  try {
    const raw = window.localStorage.getItem(storageKey)
    if (raw) {
      const parsed = JSON.parse(raw) as { rows?: SamplingRow[] }
      if (Array.isArray(parsed.rows)) return parsed as { rows: SamplingRow[] }
    }
  } catch { /* reset development-only sample data */ }
  const data = { rows: clone(seed) }
  writeStore(data)
  return data
}
function writeStore(data: { rows: SamplingRow[] }): void { window.localStorage.setItem(storageKey, JSON.stringify(data)) }
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
    throw new Error(await apiErrorMessage(response))
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
async function load(): Promise<void> {
  rows.value = isMock.value ? clone(readStore().rows) : await request<SamplingRow[]>('api/qc/sampling-1916')
}
const visibleRows = computed(() => rows.value.filter(row => row.samplingType === samplingType.value)
  .slice().sort((a, b) => a.minNum - b.minNum || a.maxNum - b.maxNum || a.sLevel.localeCompare(b.sLevel)))
const selectedRows = computed(() => visibleRows.value.filter(row => selectedKeys.value.includes(keyOf(row))))
const activeRow = computed(() => rows.value.find(row => keyOf(row) === activeKey.value) ?? null)
const dialogTitle = computed(() => 'Sampling 1916 (' + (formMode.value === 'add' ? 'ADD' : formMode.value === 'copy' ? 'COPY' : 'EDIT') + ')')
const isEdit = computed(() => formMode.value === 'edit')
const variableMode = computed(() => samplingType.value === '1')

function selectRow(row: SamplingRow): void { activeKey.value = keyOf(row); selectedKeys.value = [activeKey.value] }
function toggleRow(row: SamplingRow): void {
  const key = keyOf(row)
  selectedKeys.value = selectedKeys.value.includes(key) ? selectedKeys.value.filter(item => item !== key) : [...selectedKeys.value, key]
  activeKey.value = key
}
function selectVisible(event: Event): void {
  selectedKeys.value = (event.target as HTMLInputElement).checked ? visibleRows.value.map(keyOf) : []
  activeKey.value = selectedKeys.value[0] ?? ''
}
function setActive(prefer = ''): void {
  const row = visibleRows.value.find(item => keyOf(item) === prefer) ?? visibleRows.value[0]
  activeKey.value = row ? keyOf(row) : ''
  selectedKeys.value = row ? [keyOf(row)] : []
}
async function refresh(prefer = ''): Promise<void> { await load(); setActive(prefer) }
function changeType(value: SamplingType): void {
  samplingType.value = value
  notice.value = ''
  setActive()
}
function openForm(mode: Exclude<FormMode, null>, row: SamplingRow | null = null): void {
  if (saving.value) return
  original.value = row ? clone(row) : null
  form.value = row ? {
    sLevel: row.sLevel,
    minNum: String(row.minNum),
    maxNum: String(row.maxNum),
    sampleNum: row.sampleNum == null ? '' : String(row.sampleNum),
    rejectNum: row.rejectNum == null ? '' : String(row.rejectNum),
    kValue: row.kValue == null ? '' : String(row.kValue),
    fValue: row.fValue == null ? '' : String(row.fValue),
  } : emptyForm()
  createdBy.value = row?.creator ?? props.currentUserNo ?? 'DEV001'
  createdAt.value = row?.createDate ?? nowIso()
  revisedBy.value = row?.reviser ?? ''
  revisedAt.value = row?.reviseDate ?? ''
  formError.value = ''
  dirty.value = false
  formMode.value = mode
}
function closeForm(): void { formMode.value = null; original.value = null; formError.value = ''; dirty.value = false }
function numberValue(value: string): number | null {
  const trimmed = value.trim()
  if (!/^-?\d+(?:\.\d+)?$/.test(trimmed)) return null
  const result = Number(trimmed)
  return Number.isFinite(result) ? result : null
}
function validate(): string {
  const level = form.value.sLevel.trim().toUpperCase()
  const min = numberValue(form.value.minNum)
  const max = numberValue(form.value.maxNum)
  const sample = numberValue(form.value.sampleNum)
  const reject = numberValue(form.value.rejectNum)
  if (!level) return 'SLevel 為必填。'
  if (level.length > 6) return 'SLevel 不可超過 6 個字元。'
  if (min === null || !Number.isInteger(min) || Math.abs(min) > 99999999) return 'MinNum 為必填，且須為 8 位數範圍內的整數。'
  if (max === null || !Number.isInteger(max) || Math.abs(max) > 99999999) return 'MaxNum 為必填，且須為 8 位數範圍內的整數。'
  if (min > max) return 'MaxNum 必須大於或等於 MinNum。'
  if (sample === null || !Number.isInteger(sample) || sample < 0 || sample > 999999) return 'SampleNum 為必填，且須為 0 至 999999 的整數。'
  if (reject === null || !Number.isInteger(reject) || reject < 0 || reject > 999999) return 'RejectNum 為必填，且須為 0 至 999999 的整數。'
  if (reject > sample) return 'RejectNum 必須小於或等於 SampleNum。'
  let kValue: number | null = null
  let fValue: number | null = null
  if (variableMode.value) {
    kValue = numberValue(form.value.kValue)
    fValue = numberValue(form.value.fValue)
    if (kValue === null || Math.abs(kValue) > 9999999.999 || decimalPlaces(form.value.kValue) > 3) return 'KValue 為必填，最多 3 位小數。'
    if (fValue === null || Math.abs(fValue) > 9999999.999 || decimalPlaces(form.value.fValue) > 3) return 'FValue 為必填，最多 3 位小數。'
  }
  if (formMode.value !== 'edit' && rows.value.some(row => row.samplingType === samplingType.value && row.minNum === min && row.maxNum === max && row.sLevel.toUpperCase() === level)) return '相同 SamplingType、MinNum、MaxNum、SLevel 的資料已存在。'
  return ''
}
function decimalPlaces(value: string): number { return value.trim().split('.')[1]?.length ?? 0 }
function markDirty(): void { dirty.value = true; formError.value = '' }
function stampQuery(row: SamplingRow): URLSearchParams {
  const query = new URLSearchParams({ dataStampIsNull: row.dataStamp === null ? 'true' : 'false' })
  if (row.dataStamp !== null) query.set('dataStamp', String(row.dataStamp))
  return query
}
function formatDate(value: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  const parts = new Intl.DateTimeFormat('zh-TW', { timeZone: 'Asia/Taipei', year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false }).formatToParts(date)
  const part = (type: string) => parts.find(item => item.type === type)?.value ?? ''
  return `${part('year')}/${part('month')}/${part('day')} ${part('hour')}:${part('minute')}:${part('second')}`
}
async function save(keepOpen: boolean): Promise<void> {
  if (saving.value) return
  formError.value = validate()
  if (formError.value || !formMode.value) return
  const mode = formMode.value
  const source = original.value
  const record: SamplingRow = {
    samplingType: samplingType.value,
    minNum: Number(form.value.minNum),
    maxNum: Number(form.value.maxNum),
    sLevel: form.value.sLevel.trim().toUpperCase(),
    sampleNum: Number(form.value.sampleNum),
    rejectNum: Number(form.value.rejectNum),
    kValue: variableMode.value ? Number(form.value.kValue) : null,
    fValue: variableMode.value ? Number(form.value.fValue) : null,
    creator: createdBy.value || null,
    createDate: createdAt.value || null,
    reviser: revisedBy.value || null,
    reviseDate: revisedAt.value || null,
    dataStamp: mode === 'edit' ? source?.dataStamp ?? null : 0,
  }
  saving.value = true
  try {
    let saved: SamplingRow
    if (isMock.value) {
      const data = readStore()
      if (mode === 'edit') {
        const index = data.rows.findIndex(row => source && keyOf(row) === keyOf(source))
        if (index < 0) throw new Error('資料已不存在，請重新查詢。')
        data.rows[index] = { ...record, creator: data.rows[index].creator, createDate: data.rows[index].createDate, reviser: props.currentUserNo || 'DEV001', reviseDate: nowIso(), dataStamp: (data.rows[index].dataStamp ?? 0) + 1 }
        saved = clone(data.rows[index])
      } else {
        saved = { ...record, creator: props.currentUserNo || 'DEV001', createDate: nowIso(), reviser: null, reviseDate: null, dataStamp: 0 }
        data.rows.push(saved)
      }
      writeStore(data)
    } else if (mode === 'edit') {
      const row = source!
      const query = stampQuery(row)
      const path = 'api/qc/sampling-1916/' + [row.samplingType, row.minNum, row.maxNum, row.sLevel].map(value => encodeURIComponent(String(value))).join('/') + '?' + query.toString()
      saved = await request<SamplingRow>(path, { method: 'PUT', body: JSON.stringify({ sampleNum: record.sampleNum, rejectNum: record.rejectNum, kValue: record.kValue, fValue: record.fValue }) })
    } else {
      saved = await request<SamplingRow>('api/qc/sampling-1916', { method: 'POST', body: JSON.stringify({ samplingType: record.samplingType, minNum: record.minNum, maxNum: record.maxNum, sLevel: record.sLevel, sampleNum: record.sampleNum, rejectNum: record.rejectNum, kValue: record.kValue, fValue: record.fValue }) })
    }
    await refresh(keyOf(saved))
    dirty.value = false
    createdBy.value = saved.creator ?? ''
    createdAt.value = saved.createDate ?? ''
    revisedBy.value = saved.reviser ?? ''
    revisedAt.value = saved.reviseDate ?? ''
    if (keepOpen && mode !== 'edit') {
      original.value = null
      form.value.sLevel = saved.sLevel
      form.value.minNum = String(saved.maxNum + 1)
      form.value.maxNum = ''
      form.value.sampleNum = saved.sampleNum == null ? '' : String(saved.sampleNum)
      form.value.rejectNum = saved.rejectNum == null ? '' : String(saved.rejectNum)
      form.value.kValue = saved.kValue == null ? '' : String(saved.kValue)
      form.value.fValue = saved.fValue == null ? '' : String(saved.fValue)
      notice.value = '資料已套用；可接續輸入下一個區間。'
    } else if (keepOpen) {
      original.value = clone(saved)
      notice.value = '資料已套用。'
    } else closeForm()
  } catch (reason) { formError.value = reason instanceof Error ? reason.message : '儲存抽樣計劃失敗。' }
  finally { saving.value = false }
}
async function removeSelected(): Promise<void> {
  if (loading.value || saving.value) return
  const items = [...selectedRows.value]
  if (!items.length) return
  if (!await requestConfirmation(`確定刪除選取的 ${items.length} 筆資料？`)) return
  loading.value = true
  error.value = ''
  try {
    for (const row of items) {
      if (isMock.value) {
        const data = readStore()
        data.rows = data.rows.filter(item => keyOf(item) !== keyOf(row))
        writeStore(data)
      } else {
        const path = 'api/qc/sampling-1916/' + [row.samplingType, row.minNum, row.maxNum, row.sLevel].map(value => encodeURIComponent(String(value))).join('/') + '?' + stampQuery(row).toString()
        await request(path, { method: 'DELETE' })
      }
    }
    await refresh()
    notice.value = '刪除完成。'
  } catch (reason) {
    const message = reason instanceof Error ? reason.message : '刪除資料失敗。'
    try { await refresh() } catch { /* Retain the original first-failure message. */ }
    error.value = message
  }
  finally { loading.value = false }
}
function closePage(): void { emit('close') }
onMounted(async () => {
  loading.value = true
  try { await load(); setActive() }
  catch (reason) { error.value = reason instanceof Error ? reason.message : '載入抽樣計劃失敗。' }
  finally { loading.value = false }
})
</script>

<template>
  <section class="sampling1916-page" aria-label="抽樣計劃設定_1916">
    <header class="page-title"><h1>抽樣計劃設定_1916</h1><span v-if="isMock" class="preview-tag">開發假資料</span></header>
    <div class="mode-bar">
      <label><input type="radio" name="samplingType" value="0" :checked="samplingType === '0'" @change="changeType('0')"> Attributes</label>
      <label><input type="radio" name="samplingType" value="1" :checked="samplingType === '1'" @change="changeType('1')"> Variables</label>
      <span class="mode-help">{{ samplingType === '0' ? '計數抽樣' : '計量抽樣' }}</span>
    </div>
    <div v-if="error" class="message error" role="alert">{{ error }}</div>
    <div v-if="notice" class="message notice" role="status">{{ notice }}</div>
    <div class="master-layout">
      <div class="grid-frame" :aria-busy="loading">
        <table class="sampling-grid">
          <thead><tr>
            <th class="selector"><input type="checkbox" :checked="visibleRows.length > 0 && selectedRows.length === visibleRows.length" aria-label="選取全部" @change="selectVisible"></th>
            <th>SLevel</th><th>MinNum</th><th>MaxNum</th><th>SampleNum</th>
            <th v-if="variableMode">KValue</th><th v-if="variableMode">FValue</th>
            <th>RejectNum</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th>
          </tr></thead>
          <tbody>
            <tr v-for="row in visibleRows" :key="keyOf(row)" :class="{ active: activeKey === keyOf(row), selected: selectedKeys.includes(keyOf(row)) }" @click="selectRow(row)" @dblclick="openForm('edit', row)">
              <td class="selector" @click.stop><input type="checkbox" :checked="selectedKeys.includes(keyOf(row))" :aria-label="`選取 ${row.sLevel} ${row.minNum}-${row.maxNum}`" @change="toggleRow(row)"></td>
              <td>{{ row.sLevel }}</td><td class="number">{{ row.minNum }}</td><td class="number">{{ row.maxNum }}</td><td class="number">{{ row.sampleNum }}</td>
              <td v-if="variableMode" class="number">{{ row.kValue ?? '' }}</td><td v-if="variableMode" class="number">{{ row.fValue ?? '' }}</td>
              <td class="number">{{ row.rejectNum ?? '' }}</td><td>{{ row.creator ?? '' }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser ?? '' }}</td><td>{{ formatDate(row.reviseDate) }}</td>
            </tr>
            <tr v-if="!visibleRows.length"><td class="empty" :colspan="variableMode ? 12 : 10">{{ loading ? '載入中…' : '沒有資料' }}</td></tr>
          </tbody>
        </table>
      </div>
      <aside class="actions" aria-label="資料操作">
        <button class="primary" type="button" @click="openForm('add')">ADD (&amp;A)</button>
        <button type="button" :disabled="!activeRow" @click="activeRow && openForm('edit', activeRow)">EDIT (&amp;E)</button>
        <button type="button" :disabled="!selectedRows.length" @click="removeSelected">DEL (&amp;D)</button>
        <button type="button" :disabled="!activeRow" @click="activeRow && openForm('copy', activeRow)">Copy (&amp;C)</button>
        <button class="close" type="button" @click="closePage">Close (&amp;X)</button>
      </aside>
    </div>
    <footer class="grid-footer">資料總筆數：{{ visibleRows.length }}</footer>

    <div v-if="formMode" class="backdrop" @click.self="closeForm">
      <section class="dialog" role="dialog" aria-modal="true" :aria-label="dialogTitle">
        <header class="dialog-title"><h2>{{ dialogTitle }}</h2><button type="button" class="x-button" aria-label="關閉" @click="closeForm">×</button></header>
        <form class="dialog-body" @submit.prevent="save(false)">
          <fieldset class="sampling-group"><legend>Sampling</legend>
            <div class="dialog-types"><label><input type="radio" :checked="samplingType === '0'" disabled> Attributes</label><label><input type="radio" :checked="samplingType === '1'" disabled> Variables</label></div>
            <label class="field-row"><span>S Level</span><input v-model="form.sLevel" maxlength="6" :readonly="isEdit" class="kd-field" @blur="form.sLevel = form.sLevel.toUpperCase()" @input="markDirty"></label>
            <label class="field-row"><span>Min Num</span><input v-model="form.minNum" inputmode="numeric" :readonly="isEdit" class="kd-field numeric" @input="markDirty"></label>
            <label class="field-row"><span>Max Num</span><input v-model="form.maxNum" inputmode="numeric" :readonly="isEdit" class="kd-field numeric" @input="markDirty"></label>
            <label class="field-row"><span>Sample Num</span><input v-model="form.sampleNum" inputmode="numeric" class="kd-field numeric" @input="markDirty"></label>
            <label class="field-row"><span>Reject Num</span><input v-model="form.rejectNum" inputmode="numeric" class="kd-field numeric" @input="markDirty"></label>
            <label class="field-row"><span>K Value</span><input v-model="form.kValue" inputmode="decimal" :disabled="!variableMode" class="kd-field numeric" @input="markDirty"></label>
            <label class="field-row"><span>F Value</span><input v-model="form.fValue" inputmode="decimal" :disabled="!variableMode" class="kd-field numeric" @input="markDirty"></label>
          </fieldset>
          <fieldset class="audit-group"><legend>Basis Data</legend>
            <label><span>Creator</span><input :value="createdBy" readonly></label><label><span>Create Date</span><input :value="formatDate(createdAt)" readonly></label>
            <label><span>Reviser</span><input :value="revisedBy" readonly></label><label><span>Revise Date</span><input :value="formatDate(revisedAt)" readonly></label>
          </fieldset>
          <p v-if="formError" class="form-error" role="alert">{{ formError }}</p>
          <div class="dialog-actions"><button class="primary" type="submit" :disabled="saving">Confirm</button><button type="button" :disabled="saving" @click="closeForm">Close</button><button type="button" :disabled="saving || !dirty" @click="save(true)">Apply</button></div>
        </form>
      </section>
    </div>
  </section>
  <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
</template>

<style scoped>
.sampling1916-page{display:flex;flex-direction:column;gap:8px;min-width:0;height:100%;padding:12px;background:#fff;color:#282722;font-size:13px}.page-title{display:flex;align-items:center;gap:12px;min-height:34px;border-bottom:1px solid #dedbd4}.page-title h1{margin:0;font-size:20px;font-weight:600}.preview-tag{padding:3px 8px;border:1px solid #d8c57d;border-radius:3px;background:#fff4bf;color:#66551b;font-size:11px}.mode-bar{display:flex;align-items:center;gap:22px;min-height:38px;padding:0 10px;border:1px solid #d7d4cd;background:#faf9f6}.mode-bar label,.dialog-types label{display:flex;align-items:center;gap:4px;white-space:nowrap}.mode-bar input,.dialog-types input{accent-color:#666}.mode-help{margin-left:auto;color:#77746e}.message{padding:6px 9px;border:1px solid #d8d2c4}.message.error{background:#fff0ed;color:#902b1d}.message.notice{background:#f5f2e9;color:#595548}.master-layout{display:grid;grid-template-columns:minmax(0,1fr) 88px;gap:10px;min-height:250px;flex:1}.grid-frame{min-width:0;overflow:auto;border:1px solid #c9c6bf;background:#fff}.sampling-grid{width:100%;border-collapse:collapse;white-space:nowrap}.sampling-grid th,.sampling-grid td{height:29px;padding:4px 7px;border:1px solid #d0cdc6;text-align:left}.sampling-grid th{position:sticky;top:0;z-index:1;background:#e8e6e0;color:#37352f;font-weight:600}.sampling-grid tbody tr{cursor:pointer}.sampling-grid tbody tr:hover{background:#fff8dc}.sampling-grid tbody tr.active{outline:1px solid #a9975b;outline-offset:-1px}.sampling-grid tbody tr.selected{background:#fff0b5}.sampling-grid .selector{width:28px;padding:2px;text-align:center}.sampling-grid .selector input{margin:0;accent-color:#776328}.sampling-grid .number{text-align:right;font-variant-numeric:tabular-nums}.sampling-grid .empty{height:56px;color:#777;text-align:center}.actions{display:flex;flex-direction:column;gap:7px}.actions button,.dialog-actions button{min-height:32px;padding:5px 8px;border:1px solid #c5c1b8;border-radius:3px;background:#f8f7f3;color:#333;cursor:pointer}.actions button:hover,.dialog-actions button:hover{background:#efede7}.actions button:disabled,.dialog-actions button:disabled{opacity:.5;cursor:default}.actions .primary,.dialog-actions .primary{border-color:#da941b;background:#ffab2e;color:#fff;font-weight:600}.actions .primary:hover,.dialog-actions .primary:hover{background:#f29b20}.actions .close{margin-top:auto}.grid-footer{min-height:20px;color:#77746e}.backdrop{position:fixed;z-index:60;inset:0;display:flex;align-items:center;justify-content:center;padding:18px;background:rgb(28 27 25 / 42%)}.dialog{display:flex;flex-direction:column;width:min(489px,97vw);max-height:94vh;overflow:auto;border:1px solid #b9b4aa;background:#f8f6f1;box-shadow:0 12px 36px rgb(0 0 0 / 24%)}.dialog-title{position:sticky;top:0;z-index:2;display:flex;align-items:center;justify-content:space-between;padding:9px 14px;border-bottom:1px solid #d2cec5;background:#f2efe8}.dialog-title h2{margin:0;font-size:17px;font-weight:600}.x-button{border:0;background:transparent;font-size:23px;cursor:pointer}.dialog-body{display:flex;flex-direction:column;gap:8px;padding:9px}.dialog-body fieldset{min-width:0;border:1px solid #c9c4ba;padding:8px 12px}.dialog-body legend{padding:0 5px;color:#5a564f}.sampling-group{display:grid;grid-template-columns:1fr 1fr;gap:6px 10px}.dialog-types{grid-column:1/-1;display:flex;gap:20px;padding:1px 30px 5px}.field-row{display:grid;grid-template-columns:92px 120px;align-items:center;gap:8px;min-height:25px}.field-row>span{text-align:right}.kd-field{height:24px;width:120px;padding:2px 5px;border:1px solid #99958c;border-radius:0;background:#fff0ad;color:#282722}.kd-field[readonly]{background:#efede7}.kd-field:disabled{background:#ecebe7;color:#777}.numeric{text-align:right}.audit-group{display:grid;grid-template-columns:1fr 1fr;gap:6px 9px}.audit-group label{display:grid;grid-template-columns:72px minmax(0,1fr);align-items:center;gap:5px}.audit-group label>span{text-align:right}.audit-group input{width:100%;height:24px;padding:2px 5px;border:1px solid #c7c4bd;background:#ecebe7;color:#555}.form-error{margin:0;color:#a12e20}.dialog-actions{position:sticky;bottom:0;display:flex;justify-content:flex-end;gap:8px;padding-top:6px}.dialog-actions button{min-width:76px}.grid-footer{font-variant-numeric:tabular-nums}
@media(max-width:760px){.sampling1916-page{padding:8px}.master-layout{grid-template-columns:1fr;grid-template-rows:minmax(220px,1fr) auto}.actions{display:grid;grid-template-columns:repeat(4,minmax(0,1fr))}.actions .close{margin-top:0}.sampling-grid{min-width:930px}.sampling-group{grid-template-columns:1fr}.dialog-types{grid-column:auto}.audit-group{grid-template-columns:1fr}.field-row{grid-template-columns:92px minmax(0,1fr)}.kd-field{width:100%}}
</style>
