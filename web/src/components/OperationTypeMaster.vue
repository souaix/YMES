<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from 'vue'
import OperationFeatureDialog from './OperationFeatureDialog.vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'
import { apiErrorMessage } from '../utils/apiError'
type IssueState = -1 | 0 | 1 | 2
type Mode = 'new' | 'edit' | 'copy'
interface OperationTypeRow { opType: string; description: string | null; creator: string | null; createDate: string | null; issueState: IssueState; dataStamp: number | null; reviser: string | null; reviseDate: string | null }
const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes.dev.op-type-master.v1'
const seedRows: OperationTypeRow[] = [
  { opType: 'DEV-ASSY', description: 'DEV 組裝作業站類別', creator: 'DEV001', createDate: '2026-11-01T08:30:00', issueState: 0, dataStamp: 0, reviser: '', reviseDate: '' },
  { opType: 'DEV-QC', description: 'DEV 品質檢驗作業站類別', creator: 'DEV001', createDate: '2026-10-28T09:00:00', issueState: 2, dataStamp: 3, reviser: 'DEV002', reviseDate: '2026-11-02T13:10:00' },
  { opType: 'DEV-OLD', description: 'DEV 停用範例', creator: 'DEV001', createDate: '2026-10-12T11:00:00', issueState: -1, dataStamp: 4, reviser: 'DEV002', reviseDate: '2026-10-20T15:30:00' },
]

const rows = ref<OperationTypeRow[]>([])
const selected = ref(''), selectedKeys = ref<string[]>([])
const issueState = ref<IssueState>(0), keyword = ref('')
const loading = ref(false), busy = ref(false), closing = ref(false)
const error = ref(''), notice = ref('')
const dialogMode = ref<Mode | ''>('')
const draft = ref({ opType: '', description: '' }), originalDraft = ref('')
const editing = ref<OperationTypeRow | null>(null)
const editor = ref<HTMLFormElement | null>(null)
const dialogOpen = computed(() => dialogMode.value !== '')
const propertyOpen = ref(false), propertyTarget = ref('')
const blocked = computed(() => busy.value || loading.value || closing.value || dialogOpen.value || propertyOpen.value)
const visibleRows = computed(() => rows.value.filter(row => row.issueState === issueState.value && row.opType.toUpperCase().includes(keyword.value.trim().toUpperCase())))
const selectedRow = computed(() => visibleRows.value.find(row => row.opType === selected.value) ?? null)
const selectedRows = computed(() => visibleRows.value.filter(row => selectedKeys.value.includes(row.opType)))
const actionTargets = computed(() => selectedRows.value.length ? selectedRows.value : selectedRow.value ? [selectedRow.value] : [])
const canApprove = computed(() => actionTargets.value.length > 0 && actionTargets.value.every(row => row.issueState === 0))
const canDelete = computed(() => actionTargets.value.length > 0 && actionTargets.value.every(row => row.issueState !== 1))
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function failure(reason: unknown): string { return reason instanceof Error ? reason.message : '操作失敗，請重新查詢。' }
function displayDate(value?: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : new Intl.DateTimeFormat('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23' }).format(date)
}
function apiUrl(path = '', key?: string): string {
  if (!props.apiBaseUrl.trim()) throw new Error('目前尚未設定 API 位址。')
  const url = new URL('api/operation-types' + path, new URL(props.apiBaseUrl.replace(/\/+$/, '') + '/', window.location.origin))
  if (key !== undefined) url.searchParams.set('opType', key)
  return url.toString()
}
async function api<T>(path = '', method = 'GET', body?: unknown, key?: string): Promise<T> {
  const response = await fetch(apiUrl(path, key), { method, credentials: 'include', headers: body === undefined ? undefined : { 'Content-Type': 'application/json' }, body: body === undefined ? undefined : JSON.stringify(body) })
  if (!response.ok) {
    if (response.status === 401) location.assign(import.meta.env.BASE_URL)
    throw new Error(await apiErrorMessage(response))
  }
  return await response.json() as T
}
function syncSelection(preferred = selected.value): void {
  selected.value = visibleRows.value.some(row => row.opType === preferred) ? preferred : visibleRows.value[0]?.opType ?? ''
  selectedKeys.value = selectedKeys.value.filter(key => visibleRows.value.some(row => row.opType === key))
}
function persistMock(next: OperationTypeRow[]): void {
  if (!mock.value) throw new Error('本機資料只限 DEV。')
  localStorage.setItem(storageKey, JSON.stringify(next)); rows.value = next
}
async function loadRows(preferred = selected.value): Promise<void> {
  let next: OperationTypeRow[]
  if (mock.value) {
    const raw = localStorage.getItem(storageKey)
    next = raw ? JSON.parse(raw) as OperationTypeRow[] : clone(seedRows)
  } else next = await api<OperationTypeRow[]>()
  if (!Array.isArray(next) || next.some(row => typeof row.opType !== 'string' || ![-1,0,1,2].includes(row.issueState) || !Object.hasOwn(row, 'dataStamp'))) throw new Error('類別回應欄位不完整，請重新查詢。')
  rows.value = next; syncSelection(preferred)
}
async function queryRows(): Promise<void> {
  if (blocked.value) return
  loading.value = true; error.value = ''; notice.value = ''
  try { await loadRows(); notice.value = '資料總筆數 ' + visibleRows.value.length }
  catch (reason) { error.value = failure(reason) }
  finally { loading.value = false }
}
function selectRow(row: OperationTypeRow, event?: MouseEvent): void {
  if (blocked.value) return
  if (event?.ctrlKey || event?.metaKey) {
    const wasSelected = selectedKeys.value.includes(row.opType)
    selectedKeys.value = wasSelected ? selectedKeys.value.filter(key => key !== row.opType) : [...selectedKeys.value, row.opType]
    selected.value = wasSelected
      ? selectedKeys.value.includes(selected.value) ? selected.value : selectedKeys.value.at(-1) ?? ''
      : row.opType
  } else { selectedKeys.value = [row.opType]; selected.value = row.opType }
}
function upsert(row: OperationTypeRow): void {
  const index = rows.value.findIndex(value => value.opType === row.opType)
  if (index < 0) rows.value.push(row); else rows.value.splice(index, 1, row)
}
function setDraft(row: OperationTypeRow | null): void {
  draft.value = { opType: row?.opType ?? '', description: row?.description ?? '' }
  originalDraft.value = JSON.stringify(draft.value)
}
async function openDialog(mode: Mode, row: OperationTypeRow | null = null): Promise<void> {
  if (blocked.value || (mode !== 'new' && !row)) return
  busy.value = true; error.value = ''; notice.value = ''
  try {
    const fresh = row ? mock.value ? clone(row) : await api<OperationTypeRow>('/item', 'GET', undefined, row.opType) : null
    if (fresh) upsert(fresh)
    if (mode === 'edit' && fresh?.issueState !== 0) throw new Error('僅未簽核資料可修改。')
    editing.value = fresh; setDraft(fresh); dialogMode.value = mode
    busy.value = false
    await nextTick(); editor.value?.querySelector<HTMLInputElement>('input:not([readonly]):not([disabled])')?.focus()
  } catch (reason) { error.value = failure(reason) } finally { busy.value = false }
}
async function saveForm(closeAfter: boolean): Promise<void> {
  if (busy.value || loading.value || closing.value || !dialogMode.value) return
  const editingMode = dialogMode.value === 'edit', source = editing.value
  const opType = editingMode ? source?.opType ?? '' : draft.value.opType.trim().toUpperCase()
  const description = draft.value.description.trim()
  if (!opType || opType.length > 20 || description.length > 255) { error.value = 'OP Type必填最多20字，Description最多255字。'; return }
  busy.value = true; error.value = ''; notice.value = ''
  try {
    let saved: OperationTypeRow
    if (mock.value) {
      if (!editingMode && rows.value.some(row => row.opType.trim().toUpperCase() === opType)) throw new Error('作業站類別已存在。')
      const now = new Date().toISOString(), actor = props.currentUserNo || 'DEVUSER'
      saved = editingMode && source ? { ...source, description, reviser: actor, reviseDate: now, dataStamp: (source.dataStamp ?? 0) + 1 }
        : { opType, description, creator: actor, createDate: now, issueState: 0, dataStamp: 0, reviser: null, reviseDate: null }
      persistMock(editingMode ? rows.value.map(row => row.opType === opType ? saved : row) : [...rows.value, saved])
    } else {
      if (editingMode && !source) throw new Error('請重新開啟編輯視窗。')
      saved = editingMode ? await api<OperationTypeRow>('/item', 'PUT', { opType, description, issueState: source!.issueState, dataStamp: source!.dataStamp }, source!.opType)
        : await api<OperationTypeRow>('', 'POST', { opType, description })
    }
    upsert(saved); editing.value = clone(saved); setDraft(saved)
    issueState.value = saved.issueState; keyword.value = ''; selected.value = saved.opType; selectedKeys.value = []
    dialogMode.value = closeAfter ? '' : 'edit'; notice.value = '作業站類別已儲存。'
    try { await loadRows(saved.opType) } catch (reason) { error.value = '保存已完成，但清單重查失敗：' + failure(reason) }
  } catch (reason) { error.value = failure(reason) } finally { busy.value = false }
}
async function mutate(action: 'delete' | 'approve'): Promise<void> {
  if (blocked.value) return
  const targets = clone(actionTargets.value)
  if (!targets.length) return
  if (targets.some(row => action === 'approve' ? row.issueState !== 0 : row.issueState === 1)) { error.value = '目前狀態不能執行此操作。'; return }
  busy.value = true; error.value = ''; notice.value = ''
  try {
    if (!await requestConfirmation('確定' + (action === 'approve' ? '核准' : '刪除／停用') + targets.length + '筆作業站類別？')) return
    let completed = 0, stopped = ''
    for (const target of targets) {
      try {
        if (mock.value) {
          const next = action === 'delete' && target.issueState !== 2 ? null : { ...target, issueState: (action === 'approve' ? 2 : -1) as IssueState, dataStamp: (target.dataStamp ?? 0) + 1, reviser: props.currentUserNo || 'DEVUSER', reviseDate: new Date().toISOString() }
          persistMock(next ? rows.value.map(row => row.opType === target.opType ? next : row) : rows.value.filter(row => row.opType !== target.opType))
          if (!next) localStorage.removeItem('ymes.dev.operation.' + target.opType + '.property')
        } else if (action === 'approve') upsert(await api<OperationTypeRow>('/approve', 'POST', { issueState: target.issueState, dataStamp: target.dataStamp }, target.opType))
        else {
          const result = await api<{ outcome: string; item: OperationTypeRow }>('/item', 'DELETE', { issueState: target.issueState, dataStamp: target.dataStamp }, target.opType)
          if (result.outcome === 'DELETED') rows.value = rows.value.filter(row => row.opType !== result.item.opType)
          else upsert(result.item)
        }
        completed++
      } catch (reason) { stopped = target.opType + '：' + failure(reason); break }
    }
    selectedKeys.value = []; syncSelection(); notice.value = '完成 ' + completed + '/' + targets.length + ' 筆。'
    if (stopped) error.value = stopped
    try { await loadRows() } catch (reason) { error.value += (error.value ? ' ' : '') + '清單重查失敗：' + failure(reason) }
  } finally { busy.value = false }
}
async function allowClose(): Promise<boolean> {
  if (busy.value || loading.value || closing.value || propertyOpen.value) return false
  closing.value = true
  try { return !dialogOpen.value || JSON.stringify(draft.value) === originalDraft.value || await requestConfirmation('尚有未儲存的修改，確定放棄？') }
  finally { closing.value = false }
}
async function closeDialog(): Promise<void> { if (await allowClose()) { dialogMode.value = ''; editing.value = null; error.value = '' } }
async function closePage(): Promise<void> { if (await allowClose()) emit('close') }
function openProperty(): void {
  if (blocked.value || !selectedRow.value) return
  if (!mock.value) { error.value = 'Property 子功能的正式服務尚未提供。'; return }
  propertyTarget.value = selectedRow.value.opType; propertyOpen.value = true
}
function trapTab(event: KeyboardEvent): void {
  const controls = Array.from(editor.value?.querySelectorAll<HTMLElement>('input:not(:disabled),textarea:not(:disabled),button:not(:disabled)') ?? [])
  const first = controls[0], last = controls[controls.length - 1]
  if ((event.shiftKey && document.activeElement === first) || (!event.shiftKey && document.activeElement === last)) { event.preventDefault(); (event.shiftKey ? last : first)?.focus() }
}
watch(visibleRows, () => syncSelection())
watch(issueState, () => { selectedKeys.value = []; syncSelection() })
onMounted(async () => { loading.value = true; try { await loadRows() } catch (reason) { error.value = failure(reason) } finally { loading.value = false } })
</script>

<template>
  <div class="operation-type-page">
    <header class="page-title"><h1>作業站類別資料</h1><span>OP TYPE · TBLOPTYPE</span></header>
    <form class="query-panel" @submit.prevent="queryRows" :inert="dialogOpen || propertyOpen">
      <fieldset class="state-filter"><legend>作業站類別</legend>
        <label v-for="state in [{value:0,label:'未簽核'},{value:1,label:'簽核中'},{value:2,label:'已簽核'},{value:-1,label:'不使用'}]" :key="state.value">
          <input v-model.number="issueState" type="radio" name="op-type-state" :value="state.value" :disabled="blocked" />{{ state.label }}
        </label>
      </fieldset>
      <label class="search-field"><span>OP Type</span><input v-model="keyword" class="kd-field" maxlength="20" :disabled="blocked" /></label><span class="wildcard">%</span>
      <button class="button" type="submit" :disabled="loading || busy">查詢</button>
    </form>
    <p v-if="error && !dialogOpen" class="message error-message" role="alert">{{ error }}</p>
    <p v-if="notice" class="message" role="status">{{ notice }}</p>
    <p v-if="mock" class="dev-note">開發區本機假資料：只存於目前瀏覽器 localStorage，不會寫入 MES 或 YMES 資料庫。</p>
    <section class="master-screen" :inert="dialogOpen || propertyOpen">
      <div class="grid-panel">
        <div class="table-scroll"><table class="data-grid">
          <thead><tr><th>OP Type</th><th>Description</th><th>Creator</th><th>Create Date</th><th>Reviser</th><th>Revise Date</th></tr></thead>
          <tbody><tr v-for="row in visibleRows" :key="row.opType" :class="{ selected: selected === row.opType || selectedKeys.includes(row.opType) }" :aria-selected="selected === row.opType || selectedKeys.includes(row.opType)" tabindex="0" @click="selectRow(row, $event)" @keydown.enter.prevent="selectRow(row)" @dblclick="row.issueState === 0 && openDialog('edit', row)">
            <td>{{ row.opType }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ displayDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ displayDate(row.reviseDate) }}</td>
          </tr><tr v-if="!loading && !visibleRows.length"><td colspan="6" class="empty-cell">沒有符合條件的作業站類別</td></tr><tr v-if="loading && !visibleRows.length"><td colspan="6" class="empty-cell">資料載入中…</td></tr></tbody>
        </table></div>
        <footer class="grid-footer">資料總筆數 {{ visibleRows.length }}</footer>
      </div>
      <aside class="action-panel">
        <button class="button primary" :disabled="blocked" @click="openDialog('new')">新增(A)</button>
        <button class="button" :disabled="blocked || selectedRow?.issueState !== 0" @click="openDialog('edit', selectedRow)">修改(E)</button>
        <button class="button" :disabled="blocked || !canDelete" @click="mutate('delete')">刪除(D)</button>
        <button class="button" :disabled="blocked || !selectedRow" @click="openDialog('copy', selectedRow)">複製(C)</button>
        <button class="button" :disabled="blocked || !canApprove" @click="mutate('approve')">核准</button>
        <button class="button" :disabled="blocked || !selectedRow" @click="openProperty">Property</button>
        <button class="button close-grid" :disabled="busy || loading || closing" @click="closePage">關閉</button>
      </aside>
    </section>

    <div v-if="dialogOpen" class="dialog-backdrop" @click.self="closeDialog">
      <form ref="editor" class="definition-dialog" role="dialog" aria-modal="true" aria-labelledby="op-type-dialog-title" @submit.prevent="saveForm(true)" @keydown.esc.stop.prevent="closeDialog" @keydown.tab="trapTab">
        <h2 id="op-type-dialog-title">作業站類別資料 {{ dialogMode === 'new' ? '(新增)' : dialogMode === 'edit' ? '(修改)' : '(複製)' }}</h2>
        <fieldset class="data-group" :disabled="busy || closing"><legend>OP Type Data</legend>
          <label class="form-row"><span>OP Type</span><input v-model="draft.opType" class="kd-field" maxlength="20" :readonly="dialogMode === 'edit'" @blur="dialogMode !== 'edit' && (draft.opType = draft.opType.trim().toUpperCase())" /></label>
          <label class="form-row description-row"><span>Description</span><textarea v-model="draft.description" maxlength="255" /></label>
        </fieldset>
        <fieldset class="audit-group"><legend>建立/修改資訊</legend><label><span>建立人</span><input :value="dialogMode === 'edit' ? editing?.creator : currentUserNo" disabled /></label><label><span>建立日期</span><input :value="dialogMode === 'edit' ? displayDate(editing?.createDate ?? '') : '儲存時由伺服器建立'" disabled /></label><label v-if="dialogMode === 'edit'"><span>修改人</span><input :value="editing?.reviser ?? ''" disabled /></label><label v-if="dialogMode === 'edit'"><span>修改日期</span><input :value="displayDate(editing?.reviseDate)" disabled /></label></fieldset>
        <p v-if="error" class="message error-message" role="alert">{{ error }}</p>
        <footer class="dialog-actions"><button type="submit" class="button primary" :disabled="busy || closing">確定</button><button type="button" class="button" :disabled="busy || closing" @click="saveForm(false)">套用</button><button type="button" class="button" :disabled="busy || closing" @click="closeDialog">關閉</button></footer>
      </form>
    </div>
    <OperationFeatureDialog v-if="mock && propertyOpen" feature="property" :operation-no="propertyTarget" :operation-name="'作業站類別 ' + propertyTarget" @close="propertyOpen = false" @saved="notice = 'Property DEV 假資料已儲存。'" />
    <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
  </div>
</template>

<style scoped>
.operation-type-page{min-height:100%;padding:0 5px 12px;color:#353535;font:14px Arial,"Microsoft JhengHei",sans-serif}.page-title{display:flex;align-items:center;gap:14px;min-height:58px;padding:0 24px;border-bottom:1px solid #dedbd3;background:#f7f5ef}.page-title h1{margin:0;color:#342d22;font-size:21px;font-weight:600}.page-title>span{color:#827b70;font-size:12px}.query-panel{display:flex;align-items:center;gap:8px;min-height:48px;margin-top:8px;padding:5px 10px;border:1px solid #d2d2d2;background:#fff}.state-filter{display:flex;align-items:center;gap:12px;margin:0;padding:2px 7px;border:0}.state-filter legend{position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0)}.state-filter label,.search-field{display:flex;align-items:center;gap:5px;white-space:nowrap}.state-filter input{accent-color:#e89a1c}.search-field{margin-left:8px}.kd-field{height:29px;border:1px solid #929292;border-radius:2px;padding:4px 7px;background:#fff0b0;color:#303030;font:inherit}.kd-field:focus{outline:2px solid #e0a031;outline-offset:1px}.search-field .kd-field{width:190px}.wildcard{margin-left:-5px}.button{min-height:31px;padding:4px 12px;border:1px solid #bbb;border-radius:4px;background:linear-gradient(#fff,#f3f3f3);color:#333;cursor:pointer;font:inherit;white-space:nowrap}.button:hover:not(:disabled){border-color:#dd9b2d;background:#fff8e7}.button:disabled{color:#aaa;cursor:not-allowed}.button.primary{border-color:#f1a32c;background:#ff9f1c;color:#fff}.message{margin:6px 0 0;padding:7px 9px;color:#62522e;background:#fff9e8}.error-message{color:#8c251e;background:#fff0ed}.dev-note{margin:6px 0 0;padding:6px 9px;color:#695a35;background:#fff9e8;font-size:12px}.master-screen{display:grid;grid-template-columns:minmax(0,1fr) 96px;gap:6px;min-height:calc(100vh - 190px);margin-top:7px;border:1px solid #ccc;padding:6px;background:#fff}.grid-panel{display:flex;min-height:calc(100vh - 204px);min-width:0;flex-direction:column}.action-panel{display:flex;flex-direction:column;gap:8px;padding-left:2px}.action-panel .button{width:88px}.table-scroll{flex:1;min-height:300px;overflow:auto;border:1px solid #d0d0d0}.data-grid{width:100%;border-collapse:collapse;white-space:nowrap}.data-grid th,.data-grid td{padding:5px 8px;border:1px solid #d0d0d0;text-align:left}.data-grid th{position:sticky;top:0;z-index:1;background:#e7e9eb;color:#383838;font-weight:600}.data-grid tbody tr{cursor:pointer}.data-grid tbody tr:hover{background:#fff8e7}.data-grid tbody tr.selected{background:#ffecb4}.empty-cell{padding:22px!important;color:#777;text-align:center!important}.grid-footer{padding:6px 2px 0;color:#666;font-size:12px}.close-grid{margin-top:auto}.dialog-backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:14px;background:#17161166}.definition-dialog{width:min(497px,96vw);padding:10px 12px 12px;border:1px solid #aaa;background:#fff;box-shadow:0 12px 32px #0003}.definition-dialog h2{margin:0 0 7px;font-size:16px;font-weight:600}.data-group{min-height:137px;margin:0;padding:7px 10px 9px;border:1px solid #bcbcbc}.data-group legend,.audit-group legend{padding:0 5px}.form-row{display:grid;grid-template-columns:88px 1fr;align-items:start;gap:8px;margin:6px 0}.form-row>span{text-align:right;padding-top:5px}.form-row>.kd-field{width:152px}.description-row textarea{height:64px;resize:none;border:1px solid #888;padding:5px 7px;font:inherit}.audit-group{display:grid;grid-template-columns:1fr 1fr;gap:5px 8px;margin:7px 0;padding:7px;border:1px solid #bcbcbc}.audit-group label{display:grid;grid-template-columns:70px minmax(0,1fr);align-items:center;gap:5px;font-size:12px}.audit-group label>span{text-align:right}.audit-group input{width:100%;height:25px;border:1px solid #bbb;padding:3px 5px;background:#eee;color:#666;font:inherit}.dialog-actions{display:flex;justify-content:flex-end;gap:6px;margin-top:8px}.dialog-actions .primary{margin-left:auto}@media(max-width:760px){.query-panel{flex-wrap:wrap}.state-filter{flex-wrap:wrap}.master-screen{grid-template-columns:1fr;min-height:65vh}.grid-panel{min-height:55vh}.action-panel{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));padding:0}.action-panel .button{width:100%}.close-grid{margin:0}.audit-group{grid-template-columns:1fr}}
</style>
