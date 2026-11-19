<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import ProductParameterSetup from './ProductParameterSetup.vue'
import ProductProcessEditor from './ProductProcessEditor.vue'
import ProductPropertyMaster from './ProductPropertyMaster.vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'
import { apiErrorMessage } from '../utils/apiError'
import { loadDevelopmentProductTypes, productTypeStorageKey, type ProductTypeRow } from './productTypeDevelopment'

type TypeRow = {
  productType: string; productTypeCode: string | null; description: string | null; issueState: number
  creator: string | null; createDate: string | null; reviser?: string | null; reviseDate?: string | null
  exPrdtype1?: string | null; exPrdtype2?: string | null; dataStamp: number | null
  processes?: ProductTypeRow['processes']; properties?: ProductTypeRow['properties']; parameters?: ProductTypeRow['parameters']
}
type Draft = { productType: string; productTypeCode: string; description: string }
const props = withDefaults(defineProps<{ currentUserNo: string; developmentPreview: boolean; apiBaseUrl?: string }>(), { apiBaseUrl: '' })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const rows = ref<TypeRow[]>(mock.value ? loadDevelopmentProductTypes() : [])
const issueState = ref(0)
const states = [{ value: 0, label: '未核准' }, { value: 1, label: '簽核中' }, { value: 2, label: '已核准' }, { value: -1, label: '不使用' }]
const keyword = ref(''), query = ref(''), activeKey = ref(''), marked = ref<string[]>([])
const visibleRows = computed(() => rows.value.filter(row => row.issueState === issueState.value && row.productType.toLocaleUpperCase().includes(query.value)))
const active = computed(() => visibleRows.value.find(row => row.productType === activeKey.value))
const mode = ref<'add' | 'edit' | 'copy' | ''>('')
const child = ref<'process' | 'property' | 'parameter' | ''>('')
const busy = ref(false), loading = ref(false), closing = ref(false)
const blocked = computed(() => busy.value || loading.value || closing.value || !!mode.value || !!child.value)
const editable = computed(() => active.value?.issueState === 0)
const draft = ref<Draft>({ productType: '', productTypeCode: '', description: '' })
const originalDraft = ref('')
const editTarget = ref<TypeRow>()
const page = ref<HTMLElement | null>(null)
const editor = ref<HTMLFormElement | null>(null)
let previousEditorFocus: HTMLElement | null = null
const error = ref(''), notice = ref('')
const currentUser = computed(() => props.currentUserNo || 'DEV001')
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()
const productContext = computed(() => ({ productNo: active.value?.productType, issueState: active.value?.issueState, productVersion: '-1' }))

function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function message(reason: unknown): string { return reason instanceof Error ? reason.message : '操作失敗，請重新查詢。' }
function formatDate(value?: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value.replace('T', ' ').slice(0, 19)
  const pad = (part: number) => String(part).padStart(2, '0')
  return `${date.getFullYear()}/${pad(date.getMonth() + 1)}/${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
}
function apiUrl(path = '', productType?: string): string {
  const base = props.apiBaseUrl.replace(/\/$/, '')
  if (!base) throw new Error('尚未設定產品類別 API。')
  return base + '/api/prd/product-types' + path + (productType === undefined ? '' : '?' + new URLSearchParams({ productType }))
}
async function request<T>(path = '', method = 'GET', body?: unknown, productType?: string): Promise<T> {
  const response = await fetch(apiUrl(path, productType), {
    method, credentials: 'include', headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  if (!response.ok) {
    if (response.status === 401) window.location.assign(import.meta.env.BASE_URL)
    throw new Error(await apiErrorMessage(response))
  }
  return await response.json() as T
}
function selectFirst(preferred = activeKey.value): void {
  activeKey.value = visibleRows.value.some(row => row.productType === preferred) ? preferred : visibleRows.value[0]?.productType ?? ''
  marked.value = marked.value.filter(key => visibleRows.value.some(row => row.productType === key))
}
async function loadRows(preferred = activeKey.value): Promise<void> {
  const loaded = mock.value ? loadDevelopmentProductTypes() : await request<TypeRow[]>()
  rows.value = loaded
  selectFirst(preferred)
}
async function search(): Promise<void> {
  if (blocked.value) return
  query.value = keyword.value.trim().toLocaleUpperCase()
  marked.value = []; error.value = ''; loading.value = true
  try { await loadRows() } catch (reason) { error.value = message(reason) } finally { loading.value = false }
}
function changeFilter(): void { marked.value = []; selectFirst() }
function select(row: TypeRow): void { if (!blocked.value) activeKey.value = row.productType }
function saveRows(next: TypeRow[]): void {
  if (!mock.value) throw new Error('本機資料只限 DEV。')
  localStorage.setItem(productTypeStorageKey, JSON.stringify(next))
  rows.value = next
}
function upsert(row: TypeRow): void {
  const index = rows.value.findIndex(value => value.productType === row.productType)
  if (index < 0) rows.value.push(row)
  else rows.value.splice(index, 1, row)
}
function rememberDraft(row?: TypeRow): void {
  draft.value = { productType: row?.productType ?? '', productTypeCode: row?.productTypeCode ?? '', description: row?.description ?? '' }
  originalDraft.value = JSON.stringify(draft.value)
}
async function openEditor(nextMode: 'add' | 'edit' | 'copy'): Promise<void> {
  if (blocked.value || (nextMode === 'edit' && !editable.value) || (nextMode === 'copy' && !mock.value)) return
  const source = nextMode === 'add' ? undefined : active.value
  if (nextMode !== 'add' && !source) return
  previousEditorFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null
  busy.value = true; error.value = ''; notice.value = ''
  try {
    const fresh = source ? mock.value ? clone(source) : await request<TypeRow>('/item', 'GET', undefined, source.productType) : undefined
    if (fresh) upsert(fresh)
    if (nextMode === 'edit' && fresh?.issueState !== 0) throw new Error('只有未核准的產品類別可以修改，請重新查詢。')
    editTarget.value = fresh
    rememberDraft(fresh)
    mode.value = nextMode
    busy.value = false
    await restoreEditorFocus()
  } catch (reason) { error.value = message(reason) } finally { busy.value = false; await restoreEditorFocus() }
}
async function saveEditor(close: boolean): Promise<void> {
  if (busy.value || loading.value || closing.value || !mode.value) return
  const editing = mode.value === 'edit'
  const productType = editing ? editTarget.value?.productType ?? '' : draft.value.productType.trim().toUpperCase()
  if (!productType || productType.length > 50 || /[\x00-\x1f\x7f]/.test(productType)) { error.value = '請輸入最多 50 個字元的產品類別。'; return }
  busy.value = true; error.value = ''; notice.value = ''
  try {
    const original = editTarget.value
    const fields = { productType, productTypeCode: draft.value.productTypeCode.trim(), description: draft.value.description.trim() }
    let saved: TypeRow
    if (mock.value) {
      if (!editing && rows.value.some(row => row.productType.trim().toUpperCase() === productType)) throw new Error('產品類別已存在。')
      const now = new Date().toISOString()
      saved = editing && original
        ? { ...clone(original), ...fields, reviser: currentUser.value, reviseDate: now, dataStamp: (original.dataStamp ?? 0) + 1 }
        : { ...fields, issueState: 0, creator: currentUser.value, createDate: now, dataStamp: 0 }
      if (mode.value === 'copy' && original) {
        Object.assign(saved, clone({ processes: original.processes, properties: original.properties, parameters: original.parameters }))
        for (const row of saved.properties ?? []) row.productNo = productType
        for (const row of saved.parameters?.ops ?? []) row.productNo = productType
      }
      saveRows(editing ? rows.value.map(row => row.productType === productType ? saved : row) : [...rows.value, saved])
    } else {
      if (editing && !original) throw new Error('請重新開啟修改視窗。')
      saved = editing
        ? await request<TypeRow>('/item', 'PUT', { ...fields, issueState: original!.issueState, dataStamp: original!.dataStamp }, original!.productType)
        : await request<TypeRow>('', 'POST', fields)
    }
    // A committed mutation remains visible if the independent refresh fails.
    upsert(saved); issueState.value = saved.issueState; keyword.value = ''; query.value = ''
    activeKey.value = saved.productType; marked.value = []; editTarget.value = clone(saved); rememberDraft(saved)
    mode.value = close ? '' : 'edit'
    notice.value = '產品類別已儲存。'
    try { await loadRows(saved.productType) }
    catch (reason) { error.value = '已儲存，但清單重新查詢失敗：' + message(reason) }
  } catch (reason) { error.value = message(reason) } finally { busy.value = false; await restoreEditorFocus() }
}
async function changeState(action: 'approve' | 'delete'): Promise<void> {
  if (blocked.value || !active.value || issueState.value === 1 || (action === 'approve' && !editable.value)) return
  const selected = marked.value.length ? visibleRows.value.filter(row => marked.value.includes(row.productType)) : [active.value]
  if (!selected.length) return
  const targets = clone(selected)
  busy.value = true; error.value = ''; notice.value = ''
  try {
    if (!await requestConfirmation(action === 'approve' ? '確定核准 ' + targets.length + ' 筆產品類別？' : '確定刪除或停用 ' + targets.length + ' 筆產品類別？')) return
    let completed = 0, failure = ''
    for (const target of targets) {
      try {
        if (mock.value) {
          if (action === 'approve' || target.issueState === 2) {
            const saved = { ...target, issueState: action === 'approve' ? 2 : -1, reviser: currentUser.value, reviseDate: new Date().toISOString(), dataStamp: (target.dataStamp ?? 0) + 1 }
            saveRows(rows.value.map(row => row.productType === target.productType ? saved : row))
          } else saveRows(rows.value.filter(row => row.productType !== target.productType))
        } else if (action === 'approve') {
          upsert(await request<TypeRow>('/approve', 'POST', { issueState: target.issueState, dataStamp: target.dataStamp }, target.productType))
        } else {
          const result = await request<{ outcome: string; item: TypeRow }>('/item', 'DELETE', { issueState: target.issueState, dataStamp: target.dataStamp }, target.productType)
          if (result.outcome === 'DELETED') rows.value = rows.value.filter(row => row.productType !== result.item.productType)
          else upsert(result.item)
        }
        completed++
      } catch (reason) { failure = target.productType + '：' + message(reason); break }
    }
    marked.value = []; selectFirst()
    notice.value = '已' + (action === 'approve' ? '核准' : '刪除或停用') + ' ' + completed + ' 筆。'
    if (failure) error.value = failure + ' 後續未處理 ' + (targets.length - completed - 1) + ' 筆。'
    try { await loadRows() } catch (reason) { error.value += (error.value ? ' ' : '') + '清單重新查詢失敗：' + message(reason) }
  } finally { busy.value = false }
}
async function allowClose(): Promise<boolean> {
  if (busy.value || loading.value || closing.value || child.value) return false
  closing.value = true
  try { return !mode.value || JSON.stringify(draft.value) === originalDraft.value || await requestConfirmation('尚有未儲存的修改，確定放棄？') }
  finally { closing.value = false }
}
async function closeEditor(): Promise<void> {
  if (await allowClose()) { mode.value = ''; error.value = '' }
  await restoreEditorFocus()
}
async function closePage(): Promise<void> {
  if (await allowClose()) emit('close')
  else await restoreEditorFocus()
}
function openChild(value: 'process' | 'property' | 'parameter'): void {
  if (!mock.value || blocked.value || !active.value || (value === 'process' && !editable.value)) return
  child.value = value
}
function saveDetail(detail: Partial<Pick<TypeRow, 'processes' | 'properties' | 'parameters'>>): void {
  const key = active.value?.productType
  if (key && mock.value) {
    try { saveRows(rows.value.map(row => row.productType === key ? { ...row, ...detail } : row)) }
    catch (reason) { error.value = message(reason) }
  }
}
function canFocus(element: HTMLElement): boolean {
  return element.isConnected && element.getClientRects().length > 0 && !element.matches(':disabled') && !element.closest('[inert]')
}
async function restoreEditorFocus(): Promise<void> {
  await nextTick()
  if (confirmationMessage.value !== null || child.value) return
  if (mode.value) {
    if (busy.value || loading.value || closing.value || !editor.value) return
    const first = Array.from(editor.value.querySelectorAll<HTMLElement>('input:not([readonly]):not([disabled]),textarea:not([disabled]),button:not([disabled])')).find(canFocus)
    ;(first ?? editor.value).focus()
  } else if (previousEditorFocus && page.value?.contains(previousEditorFocus) && canFocus(previousEditorFocus)) previousEditorFocus.focus()
  else {
    const first = Array.from(page.value?.querySelectorAll<HTMLElement>('input:not([disabled]),button:not([disabled])') ?? []).find(canFocus)
    ;(first ?? page.value)?.focus()
  }
}
function keepEditorFocus(event: KeyboardEvent): void {
  if (event.key !== 'Tab' || !mode.value || child.value || confirmationMessage.value !== null) return
  if (busy.value || loading.value || closing.value) { event.preventDefault(); return }
  if (!editor.value?.contains(document.activeElement)) { event.preventDefault(); void restoreEditorFocus() }
}
function trapTab(event: KeyboardEvent): void {
  if (busy.value || loading.value || closing.value || confirmationMessage.value !== null) { event.preventDefault(); return }
  const controls = Array.from(editor.value?.querySelectorAll<HTMLElement>('input:not([disabled]),textarea:not([disabled]),button:not([disabled])') ?? []).filter(canFocus)
  const first = controls[0], last = controls[controls.length - 1]
  if (!first || !last) { event.preventDefault(); editor.value?.focus(); return }
  if ((event.shiftKey && document.activeElement === first) || (!event.shiftKey && document.activeElement === last)) {
    event.preventDefault(); (event.shiftKey ? last : first)?.focus()
  }
}
onMounted(async () => {
  document.addEventListener('keydown', keepEditorFocus, true)
  loading.value = true
  try { await loadRows() } catch (reason) { error.value = message(reason) } finally { loading.value = false }
})
onBeforeUnmount(() => { document.removeEventListener('keydown', keepEditorFocus, true) })
</script>

<template>
  <main ref="page" class="type-master" tabindex="-1">
    <header><h1>產品類別資料</h1><button type="button" :disabled="busy || loading || closing || !!child" @click="closePage">關閉</button></header>
    <section :inert="!!mode || !!child" class="content">
      <form class="toolbar" @submit.prevent="search">
        <label v-for="state in states" :key="state.value"><input v-model="issueState" type="radio" :value="state.value" :disabled="blocked" @change="changeFilter" /> {{ state.label }}</label>
        <input v-model="keyword" type="search" aria-label="產品類別" placeholder="產品類別" maxlength="50" :disabled="blocked" /><button :disabled="blocked">查詢</button>
      </form>
      <div class="toolbar">
        <button :disabled="blocked" @click="openEditor('add')">ADD</button><button :disabled="blocked || !editable" @click="openEditor('edit')">EDIT</button>
        <button :disabled="blocked || !active || issueState === 1" @click="changeState('delete')">DEL</button>
        <button :disabled="blocked || !active || !mock" :title="mock ? '' : '完整類別複製尚未開放'" @click="openEditor('copy')">Copy</button>
        <button :disabled="blocked || !editable" @click="changeState('approve')">Approve</button>
        <button :disabled="blocked || !active || !mock" :title="mock ? '' : '類別屬性尚未開放'" @click="openChild('property')">Property</button>
        <button :disabled="blocked || !active?.processes?.length || !mock" :title="mock ? '' : '類別參數尚未開放'" @click="openChild('parameter')">Parameter Setup</button>
      </div>
      <div class="grid" :aria-busy="loading">
        <table>
          <thead><tr><th>選取</th><th>ProductType</th><th>ProductTypeCode</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
          <tbody><tr v-for="row in visibleRows" :key="row.productType" :class="{ active: active?.productType === row.productType }" @click="select(row)">
            <td><input v-model="marked" type="checkbox" :value="row.productType" :disabled="blocked" :aria-label="'選取 ' + row.productType" @click.stop /></td>
            <td><button type="button" :disabled="blocked" @click.stop="select(row)">{{ row.productType }}</button></td><td>{{ row.productTypeCode }}</td><td>{{ row.description }}</td>
            <td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td>
          </tr><tr v-if="!visibleRows.length"><td colspan="8">{{ loading ? '載入中…' : '沒有符合的產品類別。' }}</td></tr></tbody>
        </table>
      </div>
      <section v-if="active" class="detail">
        <header><h2>Product Process</h2><button :disabled="blocked || !editable || !mock" :title="mock ? '' : '類別流程設定尚未開放'" @click="openChild('process')">Set …</button></header>
        <p v-if="!mock">類別流程設定尚未開放。</p>
        <div v-else class="grid"><table><thead><tr><th>MOTypeNo</th><th>PSNo</th><th>PSOrder</th><th>ProcessNo</th><th>ProcessVersion</th><th>Default</th></tr></thead>
          <tbody><tr v-for="(row, index) in active.processes ?? []" :key="index"><td>{{ row.moTypeNo }}</td><td>{{ row.psNo }}</td><td>{{ row.psOrder }}</td><td>{{ row.processNo }}</td><td>{{ row.processVersion }}</td><td>{{ Number(row.defaultProcess) ? 'Yes' : 'No' }}</td></tr>
          <tr v-if="!active.processes?.length"><td colspan="6">沒有設定流程。</td></tr></tbody></table></div>
      </section>
    </section>
    <p v-if="error && !mode" class="error" role="alert">{{ error }}</p><p v-if="notice" role="status">{{ notice }}</p>
    <div v-if="mode" class="overlay">
      <form ref="editor" class="editor" role="dialog" aria-modal="true" aria-labelledby="product-type-edit-title" tabindex="-1" :inert="confirmationMessage !== null" @submit.prevent="saveEditor(true)" @keydown.esc.stop.prevent="closeEditor" @keydown.tab="trapTab">
        <h2 id="product-type-edit-title">產品類別 {{ mode.toUpperCase() }}</h2>
        <label>ProductType <input v-model="draft.productType" :readonly="mode === 'edit'" :disabled="busy || closing" maxlength="50" required /></label>
        <label>ProductTypeCode <input v-model="draft.productTypeCode" :disabled="busy || closing" maxlength="10" /></label>
        <label>Description <textarea v-model="draft.description" :disabled="busy || closing" maxlength="255" /></label>
        <p v-if="error" class="error" role="alert">{{ error }}</p>
        <footer><button :disabled="busy || closing">Confirm</button><button type="button" :disabled="busy || closing" @click="saveEditor(false)">Apply</button><button type="button" :disabled="busy || closing" @click="closeEditor">Close</button></footer>
      </form>
    </div>
    <div v-if="mock && child && active" class="overlay">
      <ProductProcessEditor v-if="child === 'process'" :product="productContext" :processes="active.processes ?? []" :development-preview="editable" @save="saveDetail({ processes: $event })" @close="child = ''" />
      <ProductPropertyMaster v-else-if="child === 'property'" :product="productContext" :properties="active.properties ?? []" :development-preview="mock" :can-edit="editable" @save="saveDetail({ properties: $event })" @close="child = ''" />
      <ProductParameterSetup v-else :product="productContext" :processes="active.processes ?? []" :parameters="active.parameters" :development-preview="mock" :can-edit="editable" @save="saveDetail({ parameters: $event })" @close="child = ''" />
    </div>
    <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
  </main>
</template>

<style scoped>
.type-master{display:flex;flex-direction:column;gap:8px;min-width:0;padding:10px;background:#fff;color:#292820;font:12px "Segoe UI","Microsoft JhengHei",sans-serif}.content{display:grid;gap:8px;min-width:0}.type-master header,.toolbar,footer{display:flex;align-items:center;gap:8px}.type-master header{justify-content:space-between}.toolbar{flex-wrap:wrap;padding:6px;background:#f5f3eb;border:1px solid #dedbd0}.toolbar label{white-space:nowrap}h1,h2,p{margin:0}h1{font-size:17px}h2{font-size:14px}
.grid{overflow:auto;border:1px solid #d9d6cb}table{width:100%;border-collapse:collapse;text-align:left;white-space:nowrap}th,td{padding:6px;border:1px solid #e3e0d6}thead{background:#f5f3eb}.active{background:#fff2c6}.detail{display:grid;gap:6px}button,input,textarea{font:inherit}button{padding:4px 10px;border:1px solid #aaa797;border-radius:3px;background:#f8f7f2;color:inherit}button:disabled{opacity:.5}.error{color:#a32020}
.overlay{position:fixed;inset:0;z-index:91;display:grid;place-items:center;padding:12px;background:#0005}.editor{display:grid;gap:12px;width:min(520px,95vw);padding:16px;background:#fff;box-shadow:0 8px 30px #0004}.editor label{display:grid;gap:4px}.editor input,.editor textarea{padding:5px;border:1px solid #aaa797;background:#fff5d6}.editor input:read-only{background:#f4f3ec}footer{justify-content:flex-end}
</style>
