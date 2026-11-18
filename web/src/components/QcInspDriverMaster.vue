<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'

type IssueState = -1 | 0 | 1 | 2
type EditMode = 'add' | 'edit' | 'copy' | null
interface DriverRow {
  inspDriverNo: string
  functionName: string
  description: string | null
  creator: string | null
  createDate: string | null
  issueState: IssueState
  dataStamp: number | null
  reviser: string | null
  reviseDate: string | null
}
interface Envelope<T> { items?: T[]; rows?: T[]; data?: T[] }
interface DriverForm { inspDriverNo: string; functionName: string; description: string }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes-dev-mock:qc-inspection-driver:v1'
const statusOptions: { value: IssueState; label: string }[] = [
  { value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' },
]
// wsQC dynamically exposes private funDr* methods from udDriverFunction.
const demoFunctions = ['funDrv_Thick', 'funDrv_Channel', 'funDrFunctionC']
const seed: DriverRow[] = [
  { inspDriverNo: 'DEV-THICK', functionName: 'funDrv_Thick', description: 'DEV 厚度量測解析。', creator: 'DEV001', createDate: '2026-11-10T08:00:00+08:00', issueState: 0, dataStamp: 0, reviser: null, reviseDate: null },
  { inspDriverNo: 'DEV-CHANNEL', functionName: 'funDrv_Channel', description: 'DEV Channel 讀值解析。', creator: 'DEV002', createDate: '2026-11-10T09:00:00+08:00', issueState: 1, dataStamp: 1, reviser: 'DEV001', reviseDate: '2026-11-10T10:00:00+08:00' },
  { inspDriverNo: 'DEV-FUNC-C', functionName: 'funDrFunctionC', description: 'DEV 自訂解析函式。', creator: 'DEV002', createDate: '2026-11-11T08:00:00+08:00', issueState: 2, dataStamp: 2, reviser: 'DEV001', reviseDate: '2026-11-11T10:00:00+08:00' },
  { inspDriverNo: 'DEV-OLD', functionName: 'funDrv_Thick', description: 'DEV 停用範例。', creator: 'DEV001', createDate: '2026-11-12T08:00:00+08:00', issueState: -1, dataStamp: 3, reviser: 'DEV001', reviseDate: '2026-11-12T09:00:00+08:00' },
]
const rows = ref<DriverRow[]>([])
const functions = ref<string[]>([])
const issueState = ref<IssueState>(0)
const queryText = ref('')
const appliedQuery = ref('')
const activeNo = ref('')
const selected = ref<string[]>([])
const loading = ref(false)
const busy = ref(false)
const error = ref('')
const notice = ref('')
const mode = ref<EditMode>(null)
const source = ref<DriverRow | null>(null)
const formError = ref('')
const form = ref<DriverForm>(blankForm())

const visibleRows = computed(() => rows.value.filter(row => row.issueState === issueState.value && row.inspDriverNo.toLowerCase().includes(appliedQuery.value.toLowerCase())))
const active = computed(() => visibleRows.value.find(row => row.inspDriverNo === activeNo.value) ?? null)
const picked = computed(() => visibleRows.value.filter(row => selected.value.includes(row.inspDriverNo)))
const userNo = computed(() => props.currentUserNo?.trim() || (mock.value ? 'DEV001' : ''))
const formLocked = computed(() => mode.value === 'edit' && source.value?.issueState !== 0)
const title = computed(() => mode.value === 'add' ? '新增' : mode.value === 'copy' ? '複製' : '修改')

function blankForm(): DriverForm { return { inspDriverNo: '', functionName: '', description: '' } }
function value(v: Record<string, unknown>, key: string): unknown { return v[key] ?? v[key[0].toUpperCase() + key.slice(1)] }
function normalize(v: Record<string, unknown>): DriverRow {
  return {
    inspDriverNo: String(value(v, 'inspDriverNo') ?? ''), functionName: String(value(v, 'functionName') ?? ''),
    description: (value(v, 'description') as string | null) ?? null, creator: (value(v, 'creator') as string | null) ?? null,
    createDate: (value(v, 'createDate') as string | null) ?? null, issueState: Number(value(v, 'issueState') ?? 0) as IssueState,
    dataStamp: value(v, 'dataStamp') == null ? null : Number(value(v, 'dataStamp')),
    reviser: (value(v, 'reviser') as string | null) ?? null, reviseDate: (value(v, 'reviseDate') as string | null) ?? null,
  }
}
function list<T>(response: T[] | Envelope<T>): T[] { return Array.isArray(response) ? response : response.items ?? response.rows ?? response.data ?? [] }
function endpoint(path: string): string {
  if (!props.apiBaseUrl.trim()) throw new Error('目前尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(props.apiBaseUrl.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(endpoint(path), { credentials: 'include', ...init })
  if (response.status === 401) { window.location.assign(import.meta.env.BASE_URL + 'login'); throw new Error('登入已逾期，請重新登入。') }
  const text = await response.text()
  if (!response.ok) {
    let message = text || `API 請求失敗 (${response.status})`
    try { const problem = JSON.parse(text) as { detail?: string; title?: string }; message = problem.detail?.trim() || problem.title?.trim() || message } catch { /* preserve plain text */ }
    throw new Error(message)
  }
  return (text ? JSON.parse(text) : undefined) as T
}
function persist(): void { localStorage.setItem(storageKey, JSON.stringify(rows.value)) }
function demoRows(): DriverRow[] {
  try { const saved = localStorage.getItem(storageKey); if (saved) return (JSON.parse(saved) as Record<string, unknown>[]).map(normalize) }
  catch { /* reset invalid preview data */ }
  return seed.map(row => ({ ...row }))
}
async function loadFunctions(): Promise<void> {
  if (mock.value) { functions.value = [...demoFunctions]; return }
  const response = await api<unknown>('/api/qc/inspection-drivers/functions')
  const items = Array.isArray(response) ? response : list(response as Envelope<unknown>)
  functions.value = items.map(item => typeof item === 'string' ? item : String(value(item as Record<string, unknown>, 'functionName') ?? '')).filter(Boolean)
}
async function load(): Promise<void> {
  loading.value = true; error.value = ''; notice.value = ''
  try {
    if (mock.value) rows.value = demoRows()
    else {
      const query = new URLSearchParams({ issueState: String(issueState.value) })
      if (queryText.value.trim()) query.set('inspDriverNo', queryText.value.trim())
      rows.value = list(await api<Record<string, unknown>[] | Envelope<Record<string, unknown>>>('/api/qc/inspection-drivers?' + query)).map(normalize)
    }
    appliedQuery.value = queryText.value.trim(); activeNo.value = ''; selected.value = []
    if (!visibleRows.value.length) notice.value = '沒有符合查詢條件的檢測 Driver。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入檢測 Driver 失敗。' }
  finally { loading.value = false }
}
function open(next: Exclude<EditMode, null>): void {
  if (next !== 'add' && !active.value) { notice.value = '請先選取一筆資料。'; return }
  source.value = next === 'add' ? null : active.value
  const row = source.value
  form.value = row ? { inspDriverNo: next === 'copy' ? '' : row.inspDriverNo, functionName: row.functionName, description: row.description ?? '' } : blankForm()
  mode.value = next; formError.value = ''
  void loadFunctions().catch(cause => { formError.value = cause instanceof Error ? cause.message : '載入 Function Name 失敗。' })
}
function validate(): string {
  if (!form.value.inspDriverNo.trim()) return '請輸入 InspDriverNo。'
  if (form.value.inspDriverNo.length > 25) return 'InspDriverNo 最長 25 個字元。'
  if (!form.value.functionName) return '請選擇 Function Name。'
  if (form.value.functionName.length > 50) return 'FunctionName 最長 50 個字元。'
  if (form.value.description.length > 255) return 'Description 最長 255 個字元。'
  return ''
}
function payload(): Record<string, unknown> { return { inspDriverNo: form.value.inspDriverNo.trim().toUpperCase(), functionName: form.value.functionName, description: form.value.description || null } }
async function save(apply = false): Promise<void> {
  const message = validate()
  if (message) { formError.value = message; return }
  if (formLocked.value) { formError.value = '只有未簽核資料可修改。'; return }
  busy.value = true; formError.value = ''
  const data = payload(); const now = new Date().toISOString(); const no = String(data.inspDriverNo)
  try {
    let saved: DriverRow
    if (mock.value) {
      if (mode.value !== 'edit' && rows.value.some(row => row.inspDriverNo.toUpperCase() === no.toUpperCase())) throw new Error('InspDriverNo 已存在。')
      saved = normalize({ ...data, creator: mode.value === 'edit' ? source.value?.creator : userNo.value || 'DEV001', createDate: mode.value === 'edit' ? source.value?.createDate : now, issueState: mode.value === 'edit' ? source.value?.issueState : 0, dataStamp: mode.value === 'edit' ? (source.value?.dataStamp ?? 0) + 1 : 0, reviser: mode.value === 'edit' ? userNo.value || 'DEV001' : null, reviseDate: mode.value === 'edit' ? now : null })
      rows.value = mode.value === 'edit' ? rows.value.map(row => row.inspDriverNo === source.value?.inspDriverNo ? saved : row) : [...rows.value, saved]
      persist()
    } else if (mode.value === 'edit' && source.value) {
      saved = normalize(await api<Record<string, unknown>>('/api/qc/inspection-drivers/' + encodeURIComponent(source.value.inspDriverNo), { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ ...data, issueState: source.value.issueState, dataStamp: source.value.dataStamp }) }))
    } else {
      saved = normalize(await api<Record<string, unknown>>('/api/qc/inspection-drivers', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) }))
    }
    if (apply) { source.value = saved; form.value.inspDriverNo = saved.inspDriverNo; mode.value = 'edit' }
    else if (!apply) mode.value = null
    await load(); activeNo.value = saved.inspDriverNo; notice.value = apply ? '資料已套用。' : '資料已儲存。'
  } catch (cause) { formError.value = cause instanceof Error ? cause.message : '儲存檢測 Driver 失敗。' }
  finally { busy.value = false }
}
function actionRows(): DriverRow[] { return picked.value.length ? picked.value : active.value ? [active.value] : [] }
async function remove(): Promise<void> {
  const eligible = actionRows().filter(row => [0, 2, -1].includes(row.issueState))
  if (!eligible.length) { notice.value = '請選取未簽核、已簽核或不使用的資料。'; return }
  if (!(await requestConfirmation(`確定刪除 ${eligible.length} 筆檢測 Driver？已簽核資料會改為不使用。`))) return
  busy.value = true
  try {
    for (const row of eligible) {
      if (mock.value) rows.value = row.issueState === 2 ? rows.value.map(item => item.inspDriverNo === row.inspDriverNo ? { ...item, issueState: -1, dataStamp: (item.dataStamp ?? 0) + 1 } : item) : rows.value.filter(item => item.inspDriverNo !== row.inspDriverNo)
      else await api('/api/qc/inspection-drivers/' + encodeURIComponent(row.inspDriverNo) + '?issueState=' + row.issueState + '&dataStamp=' + (row.dataStamp ?? ''), { method: 'DELETE' })
    }
    if (mock.value) persist()
    await load(); notice.value = `已處理 ${eligible.length} 筆。`
  } catch (cause) { const message = cause instanceof Error ? cause.message : '刪除檢測 Driver 失敗。'; await load(); error.value = message }
  finally { busy.value = false }
}
async function approve(): Promise<void> {
  const eligible = actionRows().filter(row => row.issueState === 0)
  if (!eligible.length) { notice.value = '核准只允許未簽核資料。'; return }
  if (!(await requestConfirmation(`確定送出 ${eligible.length} 筆核准？`))) return
  busy.value = true
  try {
    for (const row of eligible) {
      if (mock.value) rows.value = rows.value.map(item => item.inspDriverNo === row.inspDriverNo ? { ...item, issueState: 1, dataStamp: (item.dataStamp ?? 0) + 1, reviser: userNo.value || 'DEV001', reviseDate: new Date().toISOString() } : item)
      else await api('/api/qc/inspection-drivers/' + encodeURIComponent(row.inspDriverNo) + '/approve', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ issueState: row.issueState, dataStamp: row.dataStamp }) })
    }
    if (mock.value) persist()
    await load(); notice.value = `已送出 ${eligible.length} 筆核准。`
  } catch (cause) { const message = cause instanceof Error ? cause.message : '核准檢測 Driver 失敗。'; await load(); error.value = message }
  finally { busy.value = false }
}
function date(value: string | null): string { return value ? new Date(value).toLocaleString('zh-TW', { hour12: false }) : '' }
function setActive(row: DriverRow): void { activeNo.value = row.inspDriverNo }
function toggleSelected(row: DriverRow): void {
  activeNo.value = row.inspDriverNo
  selected.value = selected.value.includes(row.inspDriverNo) ? selected.value.filter(no => no !== row.inspDriverNo) : [...selected.value, row.inspDriverNo]
}
onMounted(() => { void load() })
</script>

<template>
  <main class="driver-page">
    <div class="heading"><h1>檢測設備連線程式</h1><span v-if="mock" class="dev">DEV 假資料</span></div>
    <form class="query" @submit.prevent="load">
      <fieldset class="states"><legend>狀態</legend><label v-for="state in statusOptions" :key="state.value"><input v-model.number="issueState" type="radio" :value="state.value" @change="load">{{ state.label }}</label></fieldset>
      <label>檢測 Driver 編號<input v-model="queryText" autocomplete="off" @keydown.enter.prevent="load"></label><button class="button" type="submit" :disabled="loading">查詢</button>
    </form>
    <p v-if="error" class="message error">{{ error }}</p><p v-else-if="notice" class="message notice">{{ notice }}</p>
    <section class="list">
      <div class="grid"><table><thead><tr><th>選取</th><th>DriverNo</th><th>FunctionName</th><th>Description</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th></tr></thead><tbody>
        <tr v-for="row in visibleRows" :key="row.inspDriverNo" :class="{active:row.inspDriverNo===activeNo}" @click="setActive(row)">
          <td><input type="checkbox" :checked="selected.includes(row.inspDriverNo)" @click.stop="toggleSelected(row)"></td><td>{{ row.inspDriverNo }}</td><td>{{ row.functionName }}</td><td class="description-cell">{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ date(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ date(row.reviseDate) }}</td>
        </tr><tr v-if="!visibleRows.length"><td colspan="8" class="empty">{{ loading ? '載入中…' : '沒有資料' }}</td></tr>
      </tbody></table></div>
      <aside><button class="button primary" :disabled="busy" @click="open('add')">新增(A)</button><button class="button" :disabled="busy||!active" @click="open('edit')">修改(E)</button><button class="button" :disabled="busy||!active" @click="open('copy')">複製(C)</button><button class="button" :disabled="busy||(!active&&!selected.length)" @click="approve">核准</button><button class="button" :disabled="busy||(!active&&!selected.length)" @click="remove">刪除(D)</button><button class="button close" @click="emit('close')">關閉</button></aside>
    </section>
    <footer class="count">資料總筆數 {{ visibleRows.length }}　已選取 {{ picked.length }}</footer>

    <div v-if="mode" class="shade" @click.self="mode=null"><section class="dialog" role="dialog" aria-modal="true" :aria-label="`檢測設備連線程式（${title}）`">
      <header><b>檢測設備連線程式（{{ title }}）</b><button type="button" @click="mode=null">×</button></header>
      <form @submit.prevent="save()">
        <fieldset class="fields" :disabled="formLocked"><legend>Inspection Machine Driver Data</legend>
          <label><span>DriverNo</span><input v-model="form.inspDriverNo" maxlength="25" :readonly="mode==='edit'" autocomplete="off" @blur="form.inspDriverNo=form.inspDriverNo.toUpperCase()"></label>
          <label><span>Function Name</span><select v-model="form.functionName"><option value="">請選擇</option><option v-for="name in functions" :key="name" :value="name">{{ name }}</option></select></label>
          <label class="description"><span>Description</span><textarea v-model="form.description" maxlength="255" rows="4"></textarea></label>
        </fieldset>
        <fieldset class="audit"><legend>建立／修改資訊</legend>
          <label>建立人<input :value="mode==='edit' ? source?.creator??'' : userNo" readonly></label><label>建立日期<input :value="mode==='edit' ? date(source?.createDate??null) : '儲存時建立'" readonly></label>
          <label>修改人<input :value="mode==='edit' ? userNo : ''" readonly></label><label>修改日期<input :value="mode==='edit' ? '儲存時更新' : ''" readonly></label>
        </fieldset>
        <p v-if="formError" class="message error">{{ formError }}</p>
        <footer class="actions"><button class="button primary" type="submit" :disabled="busy||formLocked">確定</button><button class="button" type="button" @click="mode=null">關閉</button><button class="button" type="button" :disabled="busy||formLocked" @click="save(true)">套用</button></footer>
      </form>
    </section></div>
  </main>
  <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
</template>

<style scoped>
.driver-page{--line:#c8c4bb;--field:#fff0ad;display:flex;flex-direction:column;gap:7px;height:100%;min-height:0;padding:10px;color:#30302e;background:#f7f5ef;font:13px/1.35 "Segoe UI","Microsoft JhengHei",sans-serif}.heading{display:flex;justify-content:space-between;align-items:center;min-height:38px}.heading h1{margin:0;font-size:20px}.dev{padding:4px 9px;border:1px solid #d7a044;background:#fff1bd;color:#77551a}.query{display:flex;align-items:center;gap:9px;flex-wrap:wrap;padding:5px 9px;min-height:48px;border:1px solid var(--line);background:#fff}.query>label{display:flex;align-items:center;gap:7px}.states{display:flex;gap:10px;border:0;margin:0;padding:0}.states legend{position:absolute;clip:rect(0,0,0,0)}.states label{display:flex;gap:4px;align-items:center;white-space:nowrap}.states input{accent-color:#e18b17}.query input:not([type=radio]){width:210px}input,select,textarea{box-sizing:border-box;min-height:29px;padding:4px 7px;border:1px solid #a9a69f;border-radius:2px;background:var(--field);font:inherit;color:#30302e}input[readonly],input:disabled,select:disabled,textarea:disabled{background:#ecebe7;color:#777}.button{min-height:30px;padding:4px 10px;border:1px solid #bdb9b0;border-radius:3px;background:linear-gradient(#fff,#f0efec);font:inherit;white-space:nowrap;cursor:pointer}.button:disabled{opacity:.5;cursor:not-allowed}.primary{background:#f7a32b;color:#fff}.list{display:grid;grid-template-columns:minmax(0,1fr) 88px;gap:8px;flex:1;min-height:0;padding:7px;border:1px solid var(--line);background:#fff}.grid{overflow:auto;min-height:0;border:1px solid #bcb8af}.grid table{border-collapse:collapse;white-space:nowrap;font-size:12px;width:100%}.grid th{position:sticky;top:0;padding:6px 8px;border:1px solid #c2bfb9;background:#eeeae0;text-align:left}.grid td{height:29px;padding:3px 8px;border:1px solid #d1cec8}.grid tbody tr{cursor:pointer}.grid tbody tr:hover td{background:#fff7dd}.grid tbody tr.active td{background:#ffedb1}.grid td:first-child,.grid th:first-child{text-align:center}.grid input[type=checkbox]{min-height:auto;accent-color:#dc8e1c}.grid td.description-cell{max-width:360px;min-width:160px;white-space:pre-wrap;overflow-wrap:anywhere}.grid .empty{text-align:center;color:#777}.list aside{display:flex;flex-direction:column;gap:7px}.list aside button{width:100%}.close{margin-top:auto}.count{color:#777;font-size:12px}.message{padding:7px 10px;margin:0;border:1px solid}.error{background:#fff0e9;border-color:#d99b86;color:#873820}.notice{background:#fff8dc;border-color:#c9bc91;color:#67551d}.shade{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:20px;background:#1918164d}.dialog{width:min(560px,100%);max-height:calc(100vh - 36px);overflow:auto;border:1px solid #aaa;background:#f8f7f3;box-shadow:0 8px 28px #0003}.dialog>header{display:flex;justify-content:space-between;align-items:center;padding:9px 13px;border-bottom:1px solid var(--line);background:#f0eee8}.dialog>header button{border:0;background:none;font-size:22px}.dialog form{display:flex;flex-direction:column;gap:8px;padding:10px}.fields{display:grid;grid-template-columns:120px minmax(0,1fr);gap:9px 10px;padding:10px;border:1px solid var(--line)}.fields legend,.audit legend{padding:0 5px;color:#57544e}.fields>label{display:contents}.fields>label>span{text-align:right;padding-top:6px}.fields input,.fields select,.fields textarea{width:100%}.description{grid-column:1/-1}.description textarea{min-height:80px}.audit{display:grid;grid-template-columns:1fr 1fr;gap:8px 12px;padding:9px;border:1px solid var(--line)}.audit label{display:grid;grid-template-columns:70px 1fr;align-items:center;gap:6px}.audit input{background:#ecebe7}.actions{display:flex;justify-content:flex-end;gap:8px}@media(max-width:850px){.states{width:100%;flex-wrap:wrap}.list{grid-template-columns:1fr}.list aside{display:grid;grid-template-columns:repeat(3,1fr)}.close{margin:0}.fields{grid-template-columns:1fr}.fields>label{display:grid;grid-template-columns:120px 1fr;gap:6px}.fields>label>span{text-align:left}.audit{grid-template-columns:1fr}}
</style>
