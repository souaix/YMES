<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { loadDevelopmentRecipeGroups } from './recipeGroupDevelopment'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'

type RecipeGroupRow = { productNo: string; productVersion: string; recipeGroup: string }
type RecipeCatalogRow = { recipeGroup: string; recipeVersion: number; description: string | null; issueState: number; curVersion: number }
type InputRow = Record<string, unknown>

const props = withDefaults(defineProps<{
  product: InputRow
  recipeGroups: InputRow[] | null
  apiBaseUrl?: string
  developmentPreview: boolean
}>(), { apiBaseUrl: '' })
const emit = defineEmits<{ save: [rows: RecipeGroupRow[]]; close: [] }>()
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const working = ref<RecipeGroupRow[]>([])
const catalog = ref<RecipeCatalogRow[]>([])
const selectedKeys = ref<string[]>([])
const activeKey = ref('')
const definitionOpen = ref(false)
const lookupOpen = ref(false)
const draftRecipeGroup = ref('')
const dirty = ref(false)
const appliedRecipeGroup = ref('')
const error = ref('')
const notice = ref('')
const loading = ref(false)
const lookupLoading = ref(false)
const busy = ref(false)
const productNo = computed(() => String(props.product.productNo ?? props.product.ProductNo ?? ''))
const productVersion = computed(() => String(props.product.productVersion ?? props.product.ProductVersion ?? ''))
const assignmentPath = computed(() => 'products/' + encodeURIComponent(productNo.value) + '/versions/' + encodeURIComponent(productVersion.value) + '/recipe-groups')

const approvedCurrentCatalog = computed(() => catalog.value.filter(row => row.issueState === 2 && row.curVersion === 1))
const displayRows = computed(() => working.value.map(row => ({ ...row, key: row.recipeGroup })))
const selectedRows = computed(() => displayRows.value.filter(row => selectedKeys.value.includes(row.key)))
const canApply = computed(() => !busy.value && !loading.value && dirty.value && Boolean(draftRecipeGroup.value))
let listRequest = 0

watch(() => props.recipeGroups, value => {
  if (mock.value) replaceRows(normalizeRows(value))
}, { immediate: true, deep: true })
watch([productNo, productVersion, mock], async () => {
  error.value = ''; notice.value = ''; definitionOpen.value = false; lookupOpen.value = false
  replaceRows([])
  try { await loadAssignments() }
  catch (cause) { error.value = message(cause, '載入產品 RecipeGroup 失敗。') }
}, { immediate: true })

function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function normalizeRows(value: InputRow[] | null): RecipeGroupRow[] {
  return (Array.isArray(value) ? value : []).map(row => ({
    productNo: String(row.productNo ?? row.ProductNo ?? productNo.value),
    productVersion: String(row.productVersion ?? row.ProductVersion ?? productVersion.value),
    recipeGroup: String(row.recipeGroup ?? row.RecipeGroup ?? ''),
  }))
}
function message(cause: unknown, fallback: string): string { return cause instanceof Error ? cause.message : fallback }
async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  if (!props.apiBaseUrl.trim()) throw new Error('尚未設定 Recipe Group 服務位址。')
  const headers = new Headers(init.headers)
  if (init.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  const response = await fetch(props.apiBaseUrl.replace(/\/+$/, '') + '/api/' + path, { credentials: 'include', ...init, headers })
  if (response.status === 401) {
    window.location.assign(import.meta.env.BASE_URL + 'login')
    throw new Error('登入狀態已逾期，請重新登入。')
  }
  if (!response.ok) {
    const raw = await response.text()
    let detail = raw
    try {
      const body: unknown = JSON.parse(raw)
      if (body && typeof body === 'object') {
        const problem = body as Record<string, unknown>
        detail = [problem.detail, problem.message, problem.title].find((value): value is string => typeof value === 'string' && value.trim().length > 0) || raw
      }
    } catch { /* Non-JSON responses retain their original text. */ }
    throw new Error(detail || 'API 回應錯誤 (' + response.status + ')')
  }
  return response.status === 204 ? undefined as T : await response.json() as T
}
function replaceRows(rows: RecipeGroupRow[], focus = ''): void {
  working.value = clone(rows)
  if (focus && rows.some(row => row.recipeGroup === focus)) { activeKey.value = focus; selectedKeys.value = [focus] }
  else {
    if (!rows.some(row => row.recipeGroup === activeKey.value)) activeKey.value = rows[0]?.recipeGroup ?? ''
    selectedKeys.value = selectedKeys.value.filter(key => rows.some(row => row.recipeGroup === key))
  }
}
async function loadAssignments(focus = ''): Promise<void> {
  const requestId = ++listRequest
  loading.value = true
  try {
    const rows = mock.value ? normalizeRows(props.recipeGroups) : await api<RecipeGroupRow[]>(assignmentPath.value)
    if (requestId === listRequest) replaceRows(rows, focus)
  } finally { if (requestId === listRequest) loading.value = false }
}
function persistDevelopment(): void { emit('save', clone(working.value)) }
function selectRow(row: RecipeGroupRow, event: MouseEvent): void {
  if (busy.value) return
  const key = row.recipeGroup
  activeKey.value = key
  if (event.ctrlKey || event.metaKey) selectedKeys.value = selectedKeys.value.includes(key) ? selectedKeys.value.filter(item => item !== key) : [...selectedKeys.value, key]
  else if (event.shiftKey && selectedKeys.value.length) {
    const keys = displayRows.value.map(item => item.key)
    const from = keys.indexOf(selectedKeys.value[selectedKeys.value.length - 1]); const to = keys.indexOf(key)
    selectedKeys.value = keys.slice(Math.min(from, to), Math.max(from, to) + 1)
  } else selectedKeys.value = [key]
}
function openAdd(): void {
  if (busy.value || loading.value) return
  error.value = ''; notice.value = ''; draftRecipeGroup.value = ''; dirty.value = false; appliedRecipeGroup.value = ''
  lookupOpen.value = false; definitionOpen.value = true
}
async function openLookup(): Promise<void> {
  if (busy.value || lookupLoading.value) return
  lookupOpen.value = true; lookupLoading.value = true; error.value = ''; catalog.value = []
  try { catalog.value = mock.value ? clone(loadDevelopmentRecipeGroups()) : await api<RecipeCatalogRow[]>(assignmentPath.value + '/options') }
  catch (cause) { error.value = message(cause, '載入 RecipeGroup 查找失敗。') }
  finally { lookupLoading.value = false }
}
function chooseRecipeGroup(row: RecipeCatalogRow): void {
  if (busy.value || lookupLoading.value) return
  draftRecipeGroup.value = row.recipeGroup; dirty.value = true; appliedRecipeGroup.value = ''; error.value = ''; lookupOpen.value = false
}
async function saveAssignment(closeAfter: boolean): Promise<void> {
  if (busy.value || loading.value) return
  const group = draftRecipeGroup.value.trim()
  if (!group) { error.value = '請選擇 Recipe Group。'; return }
  if (!approvedCurrentCatalog.value.some(row => row.recipeGroup === group)) { error.value = 'Recipe Group 必須是已核准的目前版本。'; return }
  if (working.value.some(row => row.recipeGroup.toUpperCase() === group.toUpperCase())) { error.value = '此產品版本已指派這個 Recipe Group。'; return }
  busy.value = true; error.value = ''; notice.value = ''
  try {
    const saved = mock.value
      ? { productNo: productNo.value, productVersion: productVersion.value, recipeGroup: group }
      : await api<RecipeGroupRow>(assignmentPath.value, { method: 'POST', body: JSON.stringify({ recipeGroup: group }) })
    replaceRows([...working.value, saved], saved.recipeGroup)
    if (mock.value) persistDevelopment()
    dirty.value = false; appliedRecipeGroup.value = saved.recipeGroup; draftRecipeGroup.value = saved.recipeGroup
    notice.value = (mock.value ? 'DEV 假資料：' : '') + '已新增 RecipeGroup ' + saved.recipeGroup + '。'
    try { if (!mock.value) await loadAssignments(saved.recipeGroup) }
    catch (cause) { error.value = '新增已完成，但重新載入失敗：' + message(cause, '請稍後重試。') }
    if (closeAfter) definitionOpen.value = false
  } catch (cause) { error.value = message(cause, '新增 RecipeGroup 指派失敗。') }
  finally { busy.value = false }
}
async function confirmAssignment(): Promise<void> {
  if (busy.value || loading.value) return
  if (!dirty.value && appliedRecipeGroup.value === draftRecipeGroup.value && appliedRecipeGroup.value) { definitionOpen.value = false; return }
  await saveAssignment(true)
}
async function closeDefinition(): Promise<void> {
  if (busy.value || (dirty.value && !await requestConfirmation('放棄尚未新增的 RecipeGroup 選擇？'))) return
  definitionOpen.value = false; lookupOpen.value = false; dirty.value = false; error.value = ''
}
async function closeWindow(): Promise<void> {
  if (busy.value || (definitionOpen.value && dirty.value && !await requestConfirmation('放棄尚未新增的 RecipeGroup 選擇並關閉？'))) return
  emit('close')
}
async function deleteSelected(): Promise<void> {
  if (busy.value || loading.value) return
  const keys = selectedRows.value.length ? selectedRows.value.map(row => row.key) : activeKey.value ? [activeKey.value] : []
  if (!keys.length) { error.value = '請先選取資料。'; return }
  if (!await requestConfirmation('確定刪除選取的 ' + keys.length + ' 筆 RecipeGroup 指派？')) return
  busy.value = true; error.value = ''; notice.value = ''; let completed = 0; let failure = ''
  try {
    for (const group of keys) {
      if (!mock.value) await api<RecipeGroupRow>(assignmentPath.value + '/' + encodeURIComponent(group), { method: 'DELETE' })
      working.value = working.value.filter(row => row.recipeGroup !== group)
      if (mock.value) persistDevelopment()
      completed++
    }
  } catch (cause) { failure = message(cause, '刪除 RecipeGroup 指派失敗。') + (completed ? '（前 ' + completed + ' 筆已完成）' : '') }
  finally {
    try { if (mock.value) replaceRows(working.value); else await loadAssignments() }
    catch (cause) { if (!failure) failure = message(cause, '重新載入 RecipeGroup 失敗。') }
    finally { if (completed) notice.value = '已刪除 ' + completed + ' 筆 RecipeGroup 指派。'; if (failure) error.value = failure; busy.value = false }
  }
}
</script>

<template>
  <section class="prd-recipe-window" role="dialog" aria-modal="true" aria-label="Product Recipe">
    <header class="prd-recipe-title"><span>Product Recipe</span><button type="button" aria-label="關閉" :disabled="busy" @click="closeWindow">×</button></header>
    <p v-if="notice" class="prd-recipe-message">{{ notice }}</p>
    <p v-if="error && !definitionOpen" class="prd-recipe-message is-error">{{ error }}</p>
    <main class="prd-recipe-main">
      <div class="prd-recipe-grid-wrap">
        <table class="prd-recipe-grid">
          <thead><tr><th>RecipeGroup</th></tr></thead>
          <tbody>
            <tr v-for="row in displayRows" :key="row.key" :class="{ selected: selectedKeys.includes(row.key), active: activeKey === row.key }" @click="selectRow(row, $event)"><td>{{ row.recipeGroup }}</td></tr>
            <tr v-if="loading"><td class="prd-recipe-empty">載入中…</td></tr>
            <tr v-else-if="!displayRows.length"><td class="prd-recipe-empty">目前產品版本尚未指派 Recipe Group。</td></tr>
          </tbody>
        </table>
      </div>
      <aside class="prd-recipe-actions">
        <button class="prd-recipe-button confirm" type="button" :disabled="busy || loading" @click="openAdd">ADD (&amp;A)</button>
        <button class="prd-recipe-button" type="button" :disabled="busy || loading || (!selectedKeys.length && !activeKey)" @click="deleteSelected">DEL (&amp;D)</button>
        <span class="prd-recipe-spacer"></span>
        <button class="prd-recipe-button" type="button" :disabled="busy" @click="closeWindow">Close (&amp;X)</button>
      </aside>
    </main>

    <div v-if="definitionOpen" class="prd-recipe-form-backdrop">
      <section class="prd-recipe-definition" role="dialog" aria-modal="true" aria-label="Product Recipe Define">
        <header class="prd-recipe-title"><span>Product Recipe Define</span><button type="button" aria-label="關閉" :disabled="busy" @click="closeDefinition">×</button></header>
        <fieldset class="prd-recipe-fields">
          <legend>Product Recipe</legend>
          <label for="prd-recipe-group">Recipe Group</label>
          <input id="prd-recipe-group" class="prd-recipe-yellow" :value="draftRecipeGroup" readonly>
          <button class="prd-recipe-lookup" type="button" :disabled="busy || lookupLoading" @click="openLookup">...</button>
        </fieldset>
        <p v-if="error" class="prd-recipe-message is-error prd-recipe-form-error">{{ error }}</p>
        <footer class="prd-recipe-form-actions">
          <button class="prd-recipe-button confirm" type="button" :disabled="busy || loading" @click="confirmAssignment">Confirm</button>
          <button class="prd-recipe-button" type="button" :disabled="busy" @click="closeDefinition">Close</button>
          <button class="prd-recipe-button" type="button" :disabled="!canApply" @click="saveAssignment(false)">Apply</button>
        </footer>
        <div v-if="lookupOpen" class="prd-recipe-lookup-backdrop" @click.self="lookupOpen = false">
          <section class="prd-recipe-lookup-dialog" role="dialog" aria-modal="true" aria-label="選擇 Recipe Group">
            <header><strong>選擇已核准的目前 Recipe Group</strong><button type="button" aria-label="關閉查找" @click="lookupOpen = false">×</button></header>
            <p v-if="error" class="prd-recipe-message is-error">{{ error }}</p>
            <table><thead><tr><th>RecipeGroup</th><th>Version</th><th>Description</th></tr></thead>
              <tbody>
                <tr v-for="row in approvedCurrentCatalog" :key="row.recipeGroup + '|' + row.recipeVersion" @click="chooseRecipeGroup(row)"><td>{{ row.recipeGroup }}</td><td>{{ row.recipeVersion }}</td><td>{{ row.description }}</td></tr>
                <tr v-if="lookupLoading"><td colspan="3">載入中…</td></tr>
                <tr v-else-if="!approvedCurrentCatalog.length"><td colspan="3">沒有可選的已核准目前版本。</td></tr>
              </tbody>
            </table>
          </section>
        </div>
      </section>
    </div>
  </section>
  <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
</template>

<style scoped>
.prd-recipe-window{position:relative;width:min(684px,96vw);height:min(486px,calc(100vh - 24px));min-height:320px;background:#fff;border:1px solid #aaa79c;box-shadow:0 14px 44px #0004;color:#28271f;font:12px "Segoe UI","Microsoft JhengHei",sans-serif;display:flex;flex-direction:column;overflow:hidden}
.prd-recipe-title{height:30px;min-height:30px;display:flex;align-items:center;justify-content:space-between;padding:0 8px 0 12px;background:#f5f4f0;border-bottom:1px solid #d4d1c8;color:#333}.prd-recipe-title button{border:0;background:transparent;font-size:20px;color:#555;cursor:pointer;width:26px;height:26px}
.prd-recipe-note,.prd-recipe-message{margin:5px 12px 0;padding:4px 7px;background:#fff8df;border:1px solid #e4d7a3;color:#605326}.prd-recipe-message.is-error{background:#fff0ed;border-color:#d5a59b;color:#7a261c}.prd-recipe-main{display:grid;grid-template-columns:minmax(0,1fr) 80px;gap:12px;padding:14px 12px 12px;flex:1;min-height:0}.prd-recipe-grid-wrap{overflow:auto;border:1px solid #aaa79c}.prd-recipe-grid{width:100%;border-collapse:collapse;table-layout:fixed}.prd-recipe-grid th{position:sticky;top:0;background:#f1d982;border:1px solid #c6b983;padding:5px 6px;text-align:left;font-weight:600}.prd-recipe-grid td{border:1px solid #d3d0c7;padding:4px 6px;white-space:nowrap}.prd-recipe-grid tr{cursor:pointer}.prd-recipe-grid tbody tr.selected{background:#fff0b0}.prd-recipe-grid tbody tr.active{outline:1px solid #bf982e;outline-offset:-1px}.prd-recipe-empty{text-align:center;color:#777;padding:20px!important}.prd-recipe-actions{display:flex;flex-direction:column;gap:6px}.prd-recipe-spacer{flex:1}.prd-recipe-button{min-height:29px;padding:4px 6px;border:1px solid #aaa79c;border-radius:3px;background:linear-gradient(#fff,#f1efe9);color:#292820;font:inherit;white-space:nowrap;cursor:pointer}.prd-recipe-button:hover:not(:disabled),.prd-recipe-lookup:hover{border-color:#c49a35;background:#fff5d6}.prd-recipe-button.confirm{background:#f5a62a;border-color:#dc941d;color:#fff;font-weight:600}.prd-recipe-button:disabled{opacity:.5;cursor:not-allowed}
.prd-recipe-form-backdrop,.prd-recipe-lookup-backdrop{position:absolute;inset:0;background:#0005;display:grid;place-items:center;z-index:2}.prd-recipe-definition{position:relative;width:min(392px,96vw);height:min(180px,calc(100vh - 44px));min-height:180px;background:#fff;border:1px solid #aaa79c;box-shadow:0 12px 36px #0005;display:flex;flex-direction:column}.prd-recipe-fields{position:relative;margin:13px 15px 0;border:1px solid #aaa79c;padding:13px 9px 8px;height:83px;min-height:83px}.prd-recipe-fields legend{padding:0 5px;color:#555}.prd-recipe-fields label{position:absolute;left:3px;top:24px;width:90px;text-align:right;color:#444}.prd-recipe-yellow{position:absolute;left:96px;top:19px;width:176px;height:22px;padding:3px 5px;border:1px solid #888;background:#fff0b0;font:inherit}.prd-recipe-lookup{position:absolute;left:280px;top:18px;width:25px;height:23px;border:1px solid #aaa79c;background:#f8f7f3;cursor:pointer}.prd-recipe-form-actions{display:flex;justify-content:flex-end;gap:6px;margin:auto 12px 14px}.prd-recipe-form-actions .prd-recipe-button{height:32px}.prd-recipe-form-error{margin:3px 15px 0;padding:3px 6px}.prd-recipe-lookup-backdrop{inset:30px 0 0;z-index:3}.prd-recipe-lookup-dialog{width:min(440px,94vw);max-height:70%;overflow:auto;border:1px solid #aaa79c;background:#fff;box-shadow:0 12px 35px #0005}.prd-recipe-lookup-dialog header{display:flex;justify-content:space-between;align-items:center;padding:7px 10px;background:#f5f4f0;border-bottom:1px solid #d4d1c8}.prd-recipe-lookup-dialog header button{border:0;background:none;font-size:18px;cursor:pointer}.prd-recipe-lookup-dialog table{width:100%;border-collapse:collapse}.prd-recipe-lookup-dialog th,.prd-recipe-lookup-dialog td{border:1px solid #d3d0c7;padding:6px;text-align:left}.prd-recipe-lookup-dialog th{position:sticky;top:0;background:#f1d982}.prd-recipe-lookup-dialog tbody tr{cursor:pointer}.prd-recipe-lookup-dialog tbody tr:hover{background:#fff0b0}
@media(max-width:600px){.prd-recipe-main{grid-template-columns:minmax(0,1fr);grid-template-rows:minmax(150px,1fr) auto}.prd-recipe-actions{flex-direction:row;flex-wrap:wrap}.prd-recipe-spacer{display:none}}
</style>
