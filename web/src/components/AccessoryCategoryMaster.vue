<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import AccessoryCategoryStateMaster from './AccessoryCategoryStateMaster.vue'

type IssueState = 0 | 1 | 2 | -1
type Mode = 'add' | 'edit' | 'copy' | null
interface Category { accessoryCategory: string; description: string | null; issueState: IssueState; dataStamp: number; creator: string | null; createDate: string | null; reviser: string | null; reviseDate: string | null }
interface ImportRow { accessoryCategory: string; description: string; result: string; message: string }
const props = defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>()
const base = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mock = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const storageKey = 'ymes-dev-mock:accessory-categories:ep201:v1'
const seed: Category[] = [
  { accessoryCategory: 'JIG', description: '開發假資料：治具類配件', issueState: 0, dataStamp: 0, creator: 'DEV001', createDate: '2026-11-01T08:00:00+08:00', reviser: null, reviseDate: null },
  { accessoryCategory: 'CONSUMABLE', description: '開發假資料：耗材', issueState: 2, dataStamp: 2, creator: 'DEV001', createDate: '2026-11-02T08:00:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-04T09:10:00+08:00' },
  { accessoryCategory: 'MEASURING', description: '開發假資料：量具', issueState: -1, dataStamp: 1, creator: 'DEV001', createDate: '2026-11-03T08:00:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-05T10:00:00+08:00' },
  { accessoryCategory: 'WAITING', description: '開發假資料：簽核中', issueState: 1, dataStamp: 1, creator: 'DEV001', createDate: '2026-11-04T08:00:00+08:00', reviser: null, reviseDate: null },
]
const rows = ref<Category[]>([])
const status = ref<IssueState>(0)
const keyword = ref('')
const active = ref('')
const selected = ref<string[]>([])
const loading = ref(false)
const busy = ref(false)
const error = ref('')
const notice = ref('')
const mode = ref<Mode>(null)
const form = ref({ accessoryCategory: '', description: '' })
const audit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const statesOpen = ref(false)
const stateCategory = ref('')
const importOpen = ref(false)
const importFile = ref('')
const importSource = ref<File | null>(null)
const importRows = ref<ImportRow[]>([])
const importError = ref('')
const visible = computed(() => rows.value.filter(row => row.issueState === status.value && row.accessoryCategory.toLowerCase().includes(keyword.value.trim().toLowerCase())).sort((a,b) => a.accessoryCategory.localeCompare(b.accessoryCategory)))
const current = computed(() => rows.value.find(row => row.accessoryCategory === active.value) || null)
const targets = computed(() => { const chosen = rows.value.filter(row => selected.value.includes(row.accessoryCategory)); return chosen.length ? chosen : current.value ? [current.value] : [] })
const canEdit = computed(() => current.value?.issueState === 0)
const canCopy = computed(() => !!current.value)
const canApprove = computed(() => targets.value.length > 0 && targets.value.every(row => row.issueState === 0))
const canDelete = computed(() => targets.value.length > 0 && targets.value.every(row => [0, 2, -1].includes(row.issueState)))
function readLocal(): Category[] { try { const raw = localStorage.getItem(storageKey); return raw ? JSON.parse(raw) as Category[] : structuredClone(seed) } catch { return structuredClone(seed) } }
function saveLocal(value: Category[]): void { localStorage.setItem(storageKey, JSON.stringify(value)) }
function userNo(): string { return props.currentUserNo || 'DEV001' }
function now(): string { return new Date().toISOString() }
function dateText(value: string | null): string { if (!value) return ''; const date = new Date(value); return Number.isNaN(+date) ? value : date.toLocaleString('zh-TW', { hour12: false }) }
function apiPath(path: string): string { if (!props.apiBaseUrl.trim()) throw Error('目前環境尚未設定 API 位址。'); return new URL(path.replace(/^\/+/, ''), new URL(props.apiBaseUrl.replace(/\/+$/, '') + '/', location.origin)).toString() }
async function api<T>(path: string, init: RequestInit = {}): Promise<T> { const r = await fetch(apiPath(path), { credentials: 'include', ...init }); if (r.status === 401) { location.assign(base + '/'); throw Error('登入狀態已逾期，請重新登入。') } if (!r.ok) { const body = await r.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null; if (r.status === 404) throw Error('配件分類 API 尚未建立，請依 todolist_homepc.md 完成資料表與 API。'); throw Error(body?.detail || body?.message || body?.title || 'API 回應錯誤 (' + r.status + ')。') } return r.status === 204 ? undefined as T : await r.json() as T }
function reconcile(): void { selected.value = selected.value.filter(id => visible.value.some(row => row.accessoryCategory === id)); if (!visible.value.some(row => row.accessoryCategory === active.value)) active.value = visible.value[0]?.accessoryCategory || '' }
function query(): void { reconcile(); error.value = ''; notice.value = '' }
async function load(): Promise<void> { loading.value = true; error.value = ''; try { rows.value = mock.value ? readLocal() : await api<Category[]>('api/equipment/accessory-categories'); reconcile() } catch (e) { rows.value = []; error.value = e instanceof Error ? e.message : '配件分類載入失敗。' } finally { loading.value = false } }
function choose(row: Category): void { active.value = row.accessoryCategory; error.value = '' }
function toggle(id: string): void { selected.value = selected.value.includes(id) ? selected.value.filter(x => x !== id) : [...selected.value, id]; active.value = id }
function openForm(next: Exclude<Mode,null>): void {
  const row = next === 'add' ? null : current.value
  if (next !== 'add' && !row) { error.value = '請先選取一筆配件分類。'; return }
  if (next === 'edit' && row?.issueState !== 0) { error.value = '只有未簽核資料可以修改。'; return }
  mode.value = next
  form.value = next === 'edit' && row ? { accessoryCategory: row.accessoryCategory, description: row.description || '' } : { accessoryCategory: next === 'copy' && row ? row.accessoryCategory : '', description: next === 'copy' && row ? row.description || '' : '' }
  audit.value = next === 'edit' && row ? { creator: row.creator || '', createDate: dateText(row.createDate), reviser: userNo(), reviseDate: new Date().toLocaleString('zh-TW',{hour12:false}) } : { creator: userNo(), createDate: new Date().toLocaleString('zh-TW',{hour12:false}), reviser: '', reviseDate: '' }
  error.value = ''
}
function validate(): string { const id = form.value.accessoryCategory.trim(); if (!id) return '配件分類為必填。'; if (id.length > 50) return '配件分類不可超過 50 個字元。'; if (form.value.description.length > 255) return '說明不可超過 255 個字元。'; if (mode.value !== 'edit' && rows.value.some(row => row.accessoryCategory.toUpperCase() === id.toUpperCase())) return '配件分類已存在。'; return '' }
async function save(keep: boolean): Promise<void> {
  if (!mode.value) return
  const problem = validate(); if (problem) { error.value = problem; return }
  busy.value = true; error.value = ''
  const oldId = current.value?.accessoryCategory || '', action = mode.value, id = form.value.accessoryCategory.trim().toUpperCase(), stamp = now()
  try {
    if (mock.value) {
      const all = readLocal()
      if (action === 'edit') { const i = all.findIndex(row => row.accessoryCategory === oldId); if (i < 0) throw Error('找不到要修改的資料。'); all[i] = { ...all[i], description: form.value.description || null, dataStamp: all[i].dataStamp + 1, reviser: userNo(), reviseDate: stamp } }
      else all.push({ accessoryCategory: id, description: form.value.description || null, issueState: 0, dataStamp: 0, creator: userNo(), createDate: stamp, reviser: null, reviseDate: null })
      saveLocal(all)
    } else if (action === 'edit') await api('api/equipment/accessory-categories/' + encodeURIComponent(oldId), { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ description: form.value.description, dataStamp: current.value?.dataStamp }) })
    else await api('api/equipment/accessory-categories', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ accessoryCategory: id, description: form.value.description }) })
    await load(); active.value = action === 'edit' ? oldId : id; selected.value = []; notice.value = action === 'edit' ? '配件分類已修改。' : '配件分類已新增。'
    if (!keep) mode.value = null
    else if (action !== 'edit') { form.value = { accessoryCategory: '', description: '' }; audit.value = { creator: userNo(), createDate: new Date().toLocaleString('zh-TW',{hour12:false}), reviser: '', reviseDate: '' } }
  } catch (e) { error.value = e instanceof Error ? e.message : '儲存失敗。' } finally { busy.value = false }
}
async function approve(): Promise<void> {
  const list = targets.value; if (!canApprove.value || !list.length) return
  if (!window.confirm('確定核准選取的配件分類？')) return
  busy.value = true; error.value = ''
  try {
    if (mock.value) { const keys = new Set(list.map(row => row.accessoryCategory)), stamp = now(); saveLocal(readLocal().map(row => keys.has(row.accessoryCategory) ? { ...row, issueState: 2 as IssueState, dataStamp: row.dataStamp + 1, reviser: userNo(), reviseDate: stamp } : row)) }
    else for (const row of list) await api('api/equipment/accessory-categories/approve', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ accessoryCategory: row.accessoryCategory, issueState: row.issueState, dataStamp: row.dataStamp }) })
    await load(); selected.value = []; notice.value = '已送出核准；正式簽核結果依 API 回傳狀態顯示。'
  } catch (e) { error.value = e instanceof Error ? e.message : '核准失敗。' } finally { busy.value = false }
}
async function remove(): Promise<void> {
  const list = targets.value; if (!canDelete.value || !list.length) return
  if (!window.confirm('確定刪除選取的配件分類？')) return
  busy.value = true; error.value = ''
  try {
    if (mock.value) { const hard = new Set(list.filter(row => row.issueState !== 2).map(row => row.accessoryCategory)), inactive = new Set(list.filter(row => row.issueState === 2).map(row => row.accessoryCategory)), stamp = now(); saveLocal(readLocal().filter(row => !hard.has(row.accessoryCategory)).map(row => inactive.has(row.accessoryCategory) ? { ...row, issueState: -1 as IssueState, dataStamp: row.dataStamp + 1, reviser: userNo(), reviseDate: stamp } : row)) }
    else for (const row of list) await api('api/equipment/accessory-categories/delete', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ accessoryCategory: row.accessoryCategory, issueState: row.issueState, dataStamp: row.dataStamp }) })
    selected.value = []; await load(); notice.value = '配件分類已刪除或停用。'
  } catch (e) { error.value = e instanceof Error ? e.message : '刪除失敗。' } finally { busy.value = false }
}
function openStates(): void { if (!current.value) { error.value = '請選取一筆配件分類，再開啟可用狀態。'; return } stateCategory.value = current.value.accessoryCategory; statesOpen.value = true }
function csvCells(line: string): string[] { const out: string[] = []; let value = '', quoted = false; for (let i=0;i<line.length;i++) { const c=line[i]; if(c==='"'&&line[i+1]==='"'&&quoted){value+='"';i++}else if(c==='"')quoted=!quoted;else if(c===','&&!quoted){out.push(value);value=''}else value+=c } out.push(value); return out }
async function readImport(event: Event): Promise<void> {
  const file = (event.target as HTMLInputElement).files?.[0]
  importRows.value = []
  importError.value = ''
  importSource.value = file || null
  if (!file) return
  importFile.value = file.name
  const extension = file.name.toLowerCase().split('.').pop() || ''
  if (extension !== 'csv') {
    if (!mock.value && ['xls', 'xlsx'].includes(extension)) {
      importError.value = '正式環境將由 API 讀取第一工作表；按「匯入」開始處理。'
      return
    }
    importError.value = 'Development 預覽目前支援 CSV；WinForms 的 .xls/.xlsx 解析交由正式 API。'
    return
  }
  const lines = (await file.text()).replace(/^\uFEFF/, '').split(/\r?\n/).filter(line => line.trim())
  if (lines.length < 2) { importError.value = '檔案至少要有標題列及一筆資料。'; return }
  const header = csvCells(lines[0]).map(value => value.trim().toLowerCase())
  const categoryIndex = header.indexOf('accessorycategory')
  const descriptionIndex = header.indexOf('description')
  if (categoryIndex < 0) { importError.value = '找不到必要欄位 AccessoryCategory。'; return }
  importRows.value = lines.slice(1).map(line => {
    const cells = csvCells(line)
    return { accessoryCategory: (cells[categoryIndex] || '').trim().toUpperCase(), description: (cells[descriptionIndex] || '').trim(), result: '待匯入', message: '' }
  })
}
function downloadTemplate(): void { const blob=new Blob(['AccessoryCategory,Description\r\nJIG,Fixture accessories\r\n'],{type:'text/csv;charset=utf-8'}),url=URL.createObjectURL(blob),a=document.createElement('a');a.href=url;a.download='配件分類匯入範本.csv';a.click();URL.revokeObjectURL(url) }
async function runImport(): Promise<void> {
  const excelFile = importSource.value && /\.(xls|xlsx)$/i.test(importSource.value.name)
  if (!importRows.value.length && (mock.value || !excelFile)) { importError.value = '請先選取 CSV 資料。'; return }
  busy.value = true
  importError.value = ''
  try {
    if (mock.value) {
      const all = readLocal()
      for (const row of importRows.value) {
        if (!row.accessoryCategory) { row.result = '失敗'; row.message = '配件分類為必填' }
        else if (all.some(value => value.accessoryCategory === row.accessoryCategory)) { row.result = '失敗'; row.message = '配件分類已存在' }
        else if (row.accessoryCategory.length > 50 || row.description.length > 255) { row.result = '失敗'; row.message = '欄位超過長度限制' }
        else { all.push({ accessoryCategory: row.accessoryCategory, description: row.description || null, issueState: 0, dataStamp: 0, creator: userNo(), createDate: now(), reviser: null, reviseDate: null }); row.result = '成功'; row.message = '建立未簽核資料' }
      }
      saveLocal(all)
    } else if (excelFile && importSource.value) {
      const data = new FormData()
      data.append('file', importSource.value)
      const result = await api<{ results: { accessoryCategory: string; result: string; message: string }[] }>('api/equipment/accessory-categories/import', { method: 'POST', body: data })
      importRows.value = result.results.map(item => ({ accessoryCategory: item.accessoryCategory, description: '', result: item.result, message: item.message }))
    } else {
      const result = await api<{ results: { accessoryCategory: string; result: string; message: string }[] }>('api/equipment/accessory-categories/import', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(importRows.value.map(({ accessoryCategory, description }) => ({ accessoryCategory, description }))) })
      for (const row of importRows.value) {
        const item = result.results.find(value => value.accessoryCategory === row.accessoryCategory)
        row.result = item?.result || '失敗'
        row.message = item?.message || 'API 未回傳結果'
      }
    }
    await load()
  } catch (cause) {
    importError.value = cause instanceof Error ? cause.message : '匯入失敗。'
  } finally { busy.value = false }
}
function closePage(): void { history.pushState({},'',base||'/'); dispatchEvent(new PopStateEvent('popstate')) }
onMounted(() => { void load() })
watch(() => props.developmentPreview, () => { void load() })
</script>

<template>
  <section class="category-page">
    <header class="page-title"><h1>配件分類資料</h1><span v-if="mock" class="source-tag">Development 假資料</span></header>
    <div class="query-bar"><div class="status-filter" role="radiogroup" aria-label="簽核狀態">
      <label><input v-model.number="status" type="radio" :value="0" @change="query">未簽核</label><label><input v-model.number="status" type="radio" :value="1" @change="query">簽核中</label><label><input v-model.number="status" type="radio" :value="2" @change="query">已簽核</label><label><input v-model.number="status" type="radio" :value="-1" @change="query">不使用</label>
    </div><label for="category-search">配件分類</label><input id="category-search" v-model="keyword" class="query-input" maxlength="50" @keyup.enter="query"><span>%</span><button class="win-btn" @click="query">查詢</button></div>
    <p v-if="mock" class="mock-note">本機 Development 假資料，只存於瀏覽器 localStorage。</p><p v-if="error" class="message error">{{error}}</p><p v-if="notice" class="message success">{{notice}}</p>
    <section class="grid-panel"><div class="actions">
      <button class="win-btn primary" :disabled="busy" @click="openForm('add')">新增(A)</button><button class="win-btn" :disabled="busy||!canEdit" @click="openForm('edit')">修改(E)</button><button class="win-btn" :disabled="busy||!canCopy" @click="openForm('copy')">複製(C)</button><button class="win-btn" :disabled="busy||!canApprove" @click="approve">核准</button><button class="win-btn" :disabled="busy||!canDelete" @click="remove">刪除(D)</button><button class="win-btn" :disabled="busy||!current" @click="openStates">可用狀態</button><button class="win-btn" :disabled="busy" @click="importOpen=true;importRows=[];importError='';importFile='';importSource=null">資料匯入</button><button class="win-btn close" @click="closePage">關閉</button>
    </div><div class="grid-scroll"><table class="category-grid"><thead><tr><th class="check-col"></th><th>配件分類</th><th>說明</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th></tr></thead><tbody>
      <tr v-for="row in visible" :key="row.accessoryCategory" :class="{selected:active===row.accessoryCategory}" @click="choose(row)" @dblclick="openForm('edit')"><td class="check-col" @click.stop><input type="checkbox" :checked="selected.includes(row.accessoryCategory)" :aria-label="'選取 '+row.accessoryCategory" @change="toggle(row.accessoryCategory)"></td><td>{{row.accessoryCategory}}</td><td>{{row.description}}</td><td>{{row.creator}}</td><td>{{dateText(row.createDate)}}</td><td>{{row.reviser}}</td><td>{{dateText(row.reviseDate)}}</td></tr>
      <tr v-if="!visible.length"><td colspan="7" class="empty">{{loading?'載入中…':'查無資料'}}</td></tr>
    </tbody></table></div><footer class="grid-footer">資料總筆數 <b>{{visible.length}}</b></footer></section>
    <div v-if="mode" class="backdrop" @click.self="mode=null"><form class="dialog main-dialog" role="dialog" aria-modal="true" @submit.prevent="save(false)">
      <header class="dialog-head"><h2>配件分類資料（{{mode==='add'?'新增':mode==='edit'?'修改':'複製'}}）</h2><button type="button" class="x" @click="mode=null">×</button></header>
      <fieldset class="group"><legend>配件分類主檔資料</legend><label class="form-row"><span>配件分類</span><input v-model.trim="form.accessoryCategory" maxlength="50" class="kd-input" :readonly="mode==='edit'" @blur="form.accessoryCategory=form.accessoryCategory.toUpperCase()"></label><label class="form-row"><span>說明</span><textarea v-model="form.description" maxlength="255" class="kd-input description"></textarea></label></fieldset>
      <div class="audit"><label>建立人<input :value="audit.creator" readonly></label><label>建立日期<input :value="audit.createDate" readonly></label><label>修改人<input :value="audit.reviser" readonly></label><label>修改日期<input :value="audit.reviseDate" readonly></label></div>
      <p v-if="error" class="message error">{{error}}</p><footer class="dialog-foot"><button class="win-btn primary" :disabled="busy">確定</button><button type="button" class="win-btn" @click="mode=null">關閉</button><button type="button" class="win-btn" :disabled="busy" @click="save(true)">套用</button></footer>
    </form></div>
    <div v-if="importOpen" class="backdrop" @click.self="importOpen=false"><section class="dialog import-dialog" role="dialog" aria-modal="true">
      <header class="dialog-head"><h2>配件分類資料匯入</h2><button class="x" @click="importOpen=false">×</button></header><div class="import-file"><input type="file" accept=".csv,.xls,.xlsx" @change="readImport"><button class="win-btn" @click="downloadTemplate">下載範本</button></div><p class="file-name">{{importFile}}</p>
      <div class="grid-scroll"><table class="category-grid"><thead><tr><th>配件分類</th><th>說明</th><th>結果</th><th>訊息</th></tr></thead><tbody><tr v-for="(row,index) in importRows" :key="index"><td>{{row.accessoryCategory}}</td><td>{{row.description}}</td><td>{{row.result}}</td><td>{{row.message}}</td></tr><tr v-if="!importRows.length"><td colspan="4" class="empty">請選取 CSV 檔案。</td></tr></tbody></table></div>
      <p v-if="importError" class="message error">{{importError}}</p><footer class="dialog-foot"><button class="win-btn primary" :disabled="busy" @click="runImport">匯入</button><button class="win-btn" @click="importOpen=false">關閉</button></footer>
    </section></div>
    <AccessoryCategoryStateMaster v-if="statesOpen" :api-base-url="apiBaseUrl" :current-user-no="currentUserNo" :initial-category="stateCategory" :compact="true" :development-preview="props.developmentPreview" @close="statesOpen=false" />
  </section>
</template>

<style scoped>
.category-page{display:flex;flex-direction:column;gap:10px;min-height:100%;padding:10px;color:#343434;background:#fff;font:14px "Microsoft JhengHei",Arial,sans-serif}.page-title{display:flex;align-items:center;gap:12px;padding:2px 8px 8px;border-bottom:1px solid #d7d2c7}.page-title h1{margin:0;color:#3c3529;font-size:21px;font-weight:600}.source-tag{padding:3px 8px;border:1px solid #dfc56c;border-radius:3px;background:#fff4c6;color:#755916;font-size:12px}.query-bar{display:flex;align-items:center;gap:8px;min-height:42px;padding:6px 10px;border:1px solid #d6d2c9;background:#faf9f6}.status-filter{display:flex;align-items:center;gap:12px;margin-right:12px}.status-filter label{white-space:nowrap}.status-filter input{accent-color:#e28a22;margin-right:4px}.query-input{width:190px;height:28px}.query-bar>label{white-space:nowrap}.mock-note{margin:0;padding:5px 9px;border-left:3px solid #d5a535;background:#fff8df;color:#6e5a25;font-size:12px}.grid-panel{min-height:0;padding:6px;border:1px solid #c9c6bf;background:#fff}.actions{display:flex;flex-wrap:wrap;gap:5px;margin-bottom:6px}.win-btn{min-height:29px;padding:4px 11px;border:1px solid #bdb9b0;border-radius:3px;background:linear-gradient(#fff,#f0eee9);color:#35332e;font:14px "Microsoft JhengHei",Arial,sans-serif;white-space:nowrap;cursor:pointer}.win-btn:hover:not(:disabled){background:#fff0c1;border-color:#c39b42}.win-btn:disabled{opacity:.48;cursor:not-allowed}.win-btn.primary{border-color:#e9a33b;background:#f8a32d;color:#fff}.win-btn.close{margin-left:auto}.grid-scroll{width:100%;overflow:auto}.category-grid{width:100%;border-collapse:collapse;table-layout:auto}.category-grid th,.category-grid td{height:29px;padding:4px 8px;border:1px solid #c9c6bf;text-align:left;white-space:nowrap}.category-grid th{background:#f5cd64;color:#39342a;font-weight:600}.category-grid tbody tr{cursor:pointer}.category-grid tbody tr:nth-child(even){background:#fbfaf7}.category-grid tbody tr:hover{background:#fff4cf}.category-grid tbody tr.selected{background:#ffe59a}.check-col{width:30px;text-align:center!important}.check-col input{accent-color:#d98b22}.grid-footer{padding:7px 2px 0;color:#666;font-size:12px}.grid-footer b{margin-left:10px;font-weight:400}.empty{text-align:center!important;color:#777;padding:18px!important}.message{margin:0;padding:7px 10px;border-radius:3px;font-size:13px}.error{background:#fff1ee;color:#972c1b}.success{background:#edf6e9;color:#426b34}.backdrop{position:fixed;inset:0;z-index:3000;display:grid;place-items:center;padding:18px;background:rgba(35,33,29,.34)}.dialog{width:min(760px,96vw);max-height:94vh;overflow:auto;border:1px solid #aaa69d;border-radius:4px;background:#fff;box-shadow:0 8px 30px #0003}.main-dialog{padding:12px}.import-dialog{width:min(960px,96vw);padding:12px}.dialog-head{display:flex;align-items:center;justify-content:space-between;padding:4px 2px 10px;border-bottom:1px solid #d7d2c7}.dialog-head h2{margin:0;color:#40392d;font-size:18px;font-weight:600}.x{width:29px;height:27px;border:1px solid #c5c0b6;border-radius:3px;background:#f5f3ef;color:#565149;font-size:20px;line-height:20px;cursor:pointer}.group{margin:13px 0 10px;padding:12px 15px;border:1px solid #bcb8af}.group legend{padding:0 6px;color:#554a35}.form-row{display:grid;grid-template-columns:150px minmax(180px,1fr);align-items:center;gap:12px;margin:10px 0}.form-row>span:first-child{text-align:right}.kd-input,.query-input{box-sizing:border-box;padding:4px 7px;border:1px solid #8f8b82;border-radius:0;background:#fff0ae;color:#25231e;font:14px "Microsoft JhengHei",Arial,sans-serif}.kd-input:read-only{background:#efeee9;color:#666}.description{min-height:84px;resize:vertical}.audit{display:grid;grid-template-columns:1fr 1fr;gap:9px;padding:9px;border:1px solid #d2cfc7}.audit label{display:grid;grid-template-columns:85px minmax(0,1fr);align-items:center;gap:8px}.audit input{min-width:0;height:27px;padding:3px 6px;border:1px solid #aaa69d;background:#efeee9;color:#666}.dialog-foot{display:flex;justify-content:flex-end;gap:7px;padding-top:12px}.import-file{display:flex;align-items:center;gap:10px;margin:12px 0 4px}.file-name{min-height:18px;color:#666;font-size:12px}@media(max-width:760px){.status-filter{flex-wrap:wrap;gap:8px}.query-bar{flex-wrap:wrap}.audit{grid-template-columns:1fr}.form-row{grid-template-columns:110px minmax(120px,1fr)}.win-btn.close{margin-left:0}}
</style>
