<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

interface Taker {
  takerNo: string
  contactorName: string
  telNo: string
  faxNo: string
  email: string
  address: string
  bankNo: string
  issueState: number
  creator: string | null
  createDate: string | null
  dataStamp: number | null
  reviser: string | null
  reviseDate: string | null
}
type TakerForm = Pick<Taker, 'takerNo' | 'contactorName' | 'telNo' | 'faxNo' | 'email' | 'address' | 'bankNo'>
type IssueFilter = 0 | 1 | 2 | -1

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mockMode = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes-dev-mock:ent-taker-basis:v1'
const rows = ref<Taker[]>([])
const filterState = ref<IssueFilter>(0)
const keyword = ref('')
const selectedKeys = ref<string[]>([])
const activeKey = ref('')
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const formMode = ref<'add' | 'edit' | null>(null)
const form = ref<TakerForm>(blankForm())
const formDirty = ref(false)
const originalDataStamp = ref<number | null>(null)
const audit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const formError = ref('')

function blankForm(): TakerForm { return { takerNo: '', contactorName: '', telNo: '', faxNo: '', email: '', address: '', bankNo: '' } }
const seedRows: Taker[] = [
  { takerNo: 'DEV-TAKER-01', contactorName: '開發用取貨公司甲', telNo: '02-0000-0501', faxNo: '02-0000-0502', email: 'taker.a@example.test', address: '台北市範例路 5 號', bankNo: 'DEV-BANK-03', issueState: 0, creator: 'DEV001', createDate: '2026-11-08T08:00:00+08:00', dataStamp: 0, reviser: null, reviseDate: null },
  { takerNo: 'DEV-TAKER-02', contactorName: '開發用取貨公司乙', telNo: '03-0000-0601', faxNo: '', email: 'taker.b@example.test', address: '桃園市測試路 6 號', bankNo: '', issueState: 2, creator: 'DEV001', createDate: '2026-11-09T08:00:00+08:00', dataStamp: 2, reviser: 'DEV002', reviseDate: '2026-11-10T09:00:00+08:00' },
  { takerNo: 'DEV-TAKER-03', contactorName: '', telNo: '', faxNo: '', email: '', address: '', bankNo: '', issueState: 1, creator: 'DEV001', createDate: '2026-11-11T08:00:00+08:00', dataStamp: 1, reviser: 'DEV002', reviseDate: '2026-11-12T09:00:00+08:00' },
  { takerNo: 'DEV-TAKER-04', contactorName: '', telNo: '', faxNo: '', email: '', address: '', bankNo: '', issueState: -1, creator: 'DEV001', createDate: '2026-11-13T08:00:00+08:00', dataStamp: 2, reviser: 'DEV002', reviseDate: '2026-11-14T09:00:00+08:00' },
]
const visibleRows = computed(() => {
  const term = keyword.value.trim().toLocaleLowerCase()
  return rows.value.filter(row => row.issueState === filterState.value)
    .filter(row => !term || row.takerNo.toLocaleLowerCase().includes(term))
    .sort((a, b) => a.takerNo.localeCompare(b.takerNo))
})
const activeRow = computed(() => rows.value.find(row => row.takerNo === activeKey.value) ?? null)
const allVisibleSelected = computed(() => visibleRows.value.length > 0 && visibleRows.value.every(row => selectedKeys.value.includes(row.takerNo)))

function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function requestJson<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init })
  if (response.status === 401) { window.location.assign(appBase + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    if (response.status === 404) throw new Error('取貨公司基本資料 API 尚未建立；請依 todolist_homepc.md 完成 ET105 API。')
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 回應錯誤 (${response.status})。`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function readMockRows(): Taker[] {
  try { const saved = window.localStorage.getItem(storageKey); return saved ? JSON.parse(saved) as Taker[] : structuredClone(seedRows) }
  catch { return structuredClone(seedRows) }
}
function saveMockRows(value: Taker[]): void { window.localStorage.setItem(storageKey, JSON.stringify(value)) }
function stateLabel(value: number): string { return ({ 0: '未簽核', 1: '簽核中', 2: '已簽核', '-1': '不使用' } as Record<string, string>)[String(value)] ?? '未知' }
function formatDate(value: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.valueOf()) ? value : date.toLocaleString('zh-TW')
}
async function loadRows(): Promise<void> {
  loading.value = true; errorMessage.value = ''; successMessage.value = ''
  try {
    if (mockMode.value) rows.value = readMockRows()
    else {
      const query = new URLSearchParams()
      if (keyword.value.trim()) query.set('takerNo', keyword.value.trim())
      const queryString = query.toString()
      rows.value = await requestJson<Taker[]>(`api/ent/taker-basis${queryString ? `?${queryString}` : ''}`)
    }
    selectedKeys.value = []; activeKey.value = ''
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '取貨公司清單載入失敗。'; rows.value = [] }
  finally { loading.value = false }
}
function selectRow(row: Taker): void { activeKey.value = row.takerNo; selectedKeys.value = [row.takerNo] }
function toggleRow(row: Taker, checked: boolean): void {
  selectedKeys.value = checked ? [...new Set([...selectedKeys.value, row.takerNo])] : selectedKeys.value.filter(key => key !== row.takerNo)
  activeKey.value = row.takerNo
}
function toggleAll(checked: boolean): void { selectedKeys.value = checked ? visibleRows.value.map(row => row.takerNo) : []; activeKey.value = checked ? (visibleRows.value[0]?.takerNo ?? '') : '' }
function setFilter(value: IssueFilter): void { filterState.value = value; selectedKeys.value = []; activeKey.value = '' }
function formFromRow(row: Taker): TakerForm {
  return { takerNo: row.takerNo, contactorName: row.contactorName || '', telNo: row.telNo || '', faxNo: row.faxNo || '', email: row.email || '', address: row.address || '', bankNo: row.bankNo || '' }
}
function openForm(mode: 'add' | 'edit'): void {
  errorMessage.value = ''; successMessage.value = ''; formError.value = ''; formDirty.value = false
  if (mode === 'edit' && (!activeRow.value || activeRow.value.issueState !== 0)) return
  formMode.value = mode
  if (mode === 'add') {
    form.value = blankForm(); originalDataStamp.value = 0
    audit.value = { creator: props.currentUserNo || (mockMode.value ? 'DEV001' : ''), createDate: new Date().toISOString(), reviser: '', reviseDate: '' }
  } else {
    const row = activeRow.value!
    form.value = formFromRow(row); originalDataStamp.value = row.dataStamp
    audit.value = { creator: row.creator || '', createDate: row.createDate || '', reviser: props.currentUserNo || '', reviseDate: new Date().toISOString() }
  }
}
function closeForm(): void { formMode.value = null; formError.value = ''; formDirty.value = false }
function normalizeTakerNo(): void { form.value.takerNo = form.value.takerNo.toLocaleUpperCase() }
function formPayload(): TakerForm { return { ...form.value, takerNo: form.value.takerNo.trim().toLocaleUpperCase() } }
async function saveForm(closeAfterSave: boolean): Promise<void> {
  if (!formMode.value) return
  normalizeTakerNo()
  if (!form.value.takerNo.trim()) { formError.value = '請輸入 Taker No。'; return }
  const payload = formPayload()
  if (payload.takerNo.length > 50 || payload.contactorName.length > 50 || payload.telNo.length > 25 || payload.faxNo.length > 25 || payload.email.length > 255 || payload.address.length > 500 || payload.bankNo.length > 20) { formError.value = '欄位長度超過 Oracle 資料表定義。'; return }
  busy.value = true; formError.value = ''; errorMessage.value = ''
  const wasAdd = formMode.value === 'add'
  try {
    let saved: Taker
    if (mockMode.value) {
      const data = readMockRows(); const now = new Date().toISOString()
      if (wasAdd) {
        if (data.some(row => row.takerNo === payload.takerNo)) throw new Error('Taker No 已存在。')
        saved = { ...payload, issueState: 0, creator: props.currentUserNo || 'DEV001', createDate: now, dataStamp: 0, reviser: null, reviseDate: null }; data.push(saved)
      } else {
        const index = data.findIndex(row => row.takerNo === payload.takerNo)
        if (index < 0) throw new Error('資料已不存在，請重新查詢。')
        if (data[index].issueState !== 0) throw new Error('只有未簽核的資料可以修改。')
        saved = { ...data[index], ...payload, reviser: props.currentUserNo || 'DEV001', reviseDate: now, dataStamp: (data[index].dataStamp ?? 0) + 1 }; data[index] = saved
      }
      saveMockRows(data)
    } else if (wasAdd) {
      saved = await requestJson<Taker>('api/ent/taker-basis', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
      saved = saved || { ...payload, issueState: 0, creator: audit.value.creator, createDate: audit.value.createDate, dataStamp: 0, reviser: null, reviseDate: null }
    } else {
      const query = new URLSearchParams({ dataStamp: String(originalDataStamp.value ?? 0) })
      saved = await requestJson<Taker>(`api/ent/taker-basis/${encodeURIComponent(payload.takerNo)}?${query}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
      saved = saved || { ...payload, issueState: 0, creator: audit.value.creator, createDate: audit.value.createDate, dataStamp: (originalDataStamp.value ?? 0) + 1, reviser: props.currentUserNo || '', reviseDate: new Date().toISOString() }
    }
    await loadRows()
    const savedVisible = visibleRows.value.some(row => row.takerNo === saved.takerNo)
    activeKey.value = savedVisible ? saved.takerNo : ''; selectedKeys.value = savedVisible ? [saved.takerNo] : []
    if (!closeAfterSave) {
      if (wasAdd) formMode.value = 'edit'
      originalDataStamp.value = saved.dataStamp ?? 0; form.value = formFromRow(saved)
      audit.value = { creator: saved.creator || '', createDate: saved.createDate || '', reviser: props.currentUserNo || '', reviseDate: new Date().toISOString() }
    }
    formDirty.value = false
    successMessage.value = wasAdd ? '取貨公司資料已新增。' : '取貨公司資料已修改。'
    if (closeAfterSave) closeForm()
  } catch (error) { formError.value = error instanceof Error ? error.message : '取貨公司資料儲存失敗。' }
  finally { busy.value = false }
}
async function deleteSelected(): Promise<void> {
  const targets = rows.value.filter(row => selectedKeys.value.includes(row.takerNo))
  if (!targets.length) { errorMessage.value = '請先選取要刪除的取貨公司。'; return }
  if (!window.confirm(`確定刪除／停用選取的 ${targets.length} 筆取貨公司資料？`)) return
  busy.value = true; errorMessage.value = ''; successMessage.value = ''; let completed = 0
  try {
    for (const row of targets) {
      if (![0, -1, 2].includes(row.issueState)) throw new Error(`「${row.takerNo}」目前為${stateLabel(row.issueState)}，不允許刪除。`)
      if (mockMode.value) {
        const data = readMockRows()
        if (row.issueState === 2) { const index = data.findIndex(item => item.takerNo === row.takerNo); if (index >= 0) data[index] = { ...data[index], issueState: -1, reviser: props.currentUserNo || 'DEV001', reviseDate: new Date().toISOString(), dataStamp: (data[index].dataStamp ?? 0) + 1 }; saveMockRows(data) }
        else saveMockRows(data.filter(item => item.takerNo !== row.takerNo))
      } else {
        const query = new URLSearchParams({ issueState: String(row.issueState), dataStamp: String(row.dataStamp ?? 0) })
        await requestJson<void>(`api/ent/taker-basis/${encodeURIComponent(row.takerNo)}?${query}`, { method: 'DELETE' })
      }
      completed++
    }
    await loadRows(); successMessage.value = `已處理 ${completed} 筆取貨公司資料。`
  } catch (error) { const message = error instanceof Error ? error.message : '取貨公司資料刪除失敗。'; await loadRows(); errorMessage.value = message }
  finally { busy.value = false }
}
async function approveSelected(): Promise<void> {
  const targets = rows.value.filter(row => selectedKeys.value.includes(row.takerNo))
  if (!targets.length) { errorMessage.value = '請先選取要核准的取貨公司。'; return }
  if (!window.confirm(`確定核准選取的 ${targets.length} 筆取貨公司資料？`)) return
  busy.value = true; errorMessage.value = ''; successMessage.value = ''; let completed = 0
  try {
    for (const row of targets) {
      if (row.issueState !== 0) throw new Error(`「${row.takerNo}」目前為${stateLabel(row.issueState)}，不允許核准。`)
      if (mockMode.value) {
        const data = readMockRows(); const index = data.findIndex(item => item.takerNo === row.takerNo)
        if (index >= 0) data[index] = { ...data[index], issueState: 2, reviser: props.currentUserNo || 'DEV001', reviseDate: new Date().toISOString(), dataStamp: (data[index].dataStamp ?? 0) + 1 }
        saveMockRows(data)
      } else { const query = new URLSearchParams({ dataStamp: String(row.dataStamp ?? 0) }); await requestJson<void>(`api/ent/taker-basis/${encodeURIComponent(row.takerNo)}/approve?${query}`, { method: 'POST' }) }
      completed++
    }
    await loadRows(); successMessage.value = `已送出 ${completed} 筆核准。DEV 假資料以直接核准方式模擬。`
  } catch (error) { const message = error instanceof Error ? error.message : '取貨公司核准失敗。'; await loadRows(); errorMessage.value = message }
  finally { busy.value = false }
}
function closePage(): void { rows.value = []; selectedKeys.value = []; activeKey.value = ''; errorMessage.value = ''; successMessage.value = '' }
onMounted(() => { if (mockMode.value && !window.localStorage.getItem(storageKey)) saveMockRows(seedRows); void loadRows() })
</script>

<template>
  <div class="taker-page">
    <div class="taker-query"><div class="taker-state-filter" role="radiogroup" aria-label="簽核狀態">
      <label><input type="radio" :checked="filterState === 0" @change="setFilter(0)" />未簽核</label><label><input type="radio" :checked="filterState === 1" @change="setFilter(1)" />簽核中</label><label><input type="radio" :checked="filterState === 2" @change="setFilter(2)" />已簽核</label><label><input type="radio" :checked="filterState === -1" @change="setFilter(-1)" />不使用</label>
    </div><label for="taker-no-search">取貨公司編號</label><input id="taker-no-search" v-model="keyword" @keyup.enter="loadRows" /><span>%</span><button type="button" :disabled="loading || busy" @click="loadRows">查詢</button></div>
    <div v-if="mockMode" class="taker-note">Development 假資料｜只儲存在此瀏覽器，不會寫入正式資料庫；簽核流程以直接核准模擬</div>
    <div v-if="errorMessage" class="taker-message error" role="alert">{{ errorMessage }}</div><div v-if="successMessage" class="taker-message success" role="status">{{ successMessage }}</div>
    <section class="taker-list"><div class="taker-list-layout"><div class="taker-table-scroll"><table class="taker-table"><thead><tr>
      <th class="select-col"><input type="checkbox" aria-label="選取所有取貨公司" :checked="allVisibleSelected" @change="toggleAll(($event.target as HTMLInputElement).checked)" /></th><th>取貨公司編號</th><th>連絡人</th><th>電話</th><th>傳真</th><th>Email</th><th>地址</th><th>銀行編號</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th>
    </tr></thead><tbody>
      <tr v-for="row in visibleRows" :key="row.takerNo" tabindex="0" :aria-selected="selectedKeys.includes(row.takerNo)" :class="{ selected: selectedKeys.includes(row.takerNo) }" @click="selectRow(row)" @keydown.enter.prevent="selectRow(row)">
        <td class="select-col" @click.stop><input type="checkbox" :aria-label="`選取取貨公司 ${row.takerNo}`" :checked="selectedKeys.includes(row.takerNo)" @change="toggleRow(row, ($event.target as HTMLInputElement).checked)" /></td><td>{{ row.takerNo }}</td><td>{{ row.contactorName }}</td><td>{{ row.telNo }}</td><td>{{ row.faxNo }}</td><td>{{ row.email }}</td><td>{{ row.address }}</td><td>{{ row.bankNo }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td>
      </tr><tr v-if="loading"><td colspan="12" class="taker-empty">資料載入中…</td></tr><tr v-else-if="visibleRows.length === 0 && !errorMessage"><td colspan="12" class="taker-empty">沒有符合的取貨公司資料。</td></tr>
    </tbody></table></div><div class="taker-actions"><button class="primary" type="button" :disabled="busy" @click="openForm('add')">新增(A)</button><button type="button" :disabled="busy || !activeRow || activeRow.issueState !== 0" @click="openForm('edit')">修改(E)</button><button type="button" :disabled="busy || !selectedKeys.length" @click="deleteSelected">刪除(D)</button><button type="button" :disabled="busy || !selectedKeys.length" @click="approveSelected">核准</button><button class="taker-print" type="button" disabled>Print</button><button class="taker-close" type="button" @click="closePage">關閉</button></div></div><footer class="taker-count">資料總筆數 <strong>{{ visibleRows.length }}</strong></footer></section>

    <div v-if="formMode" class="taker-overlay" @click.self="closeForm"><form class="taker-dialog" @submit.prevent="saveForm(true)"><header><h2>{{ formMode === 'add' ? '新增' : '修改' }}取貨公司基本資料</h2><button type="button" aria-label="關閉" @click="closeForm">×</button></header>
      <div class="taker-form-scroll"><fieldset><legend>取貨公司主檔資料</legend><div class="taker-fields">
        <label><span>Taker No *</span><input v-model="form.takerNo" :readonly="formMode === 'edit'" required @input="formDirty = true" @blur="normalizeTakerNo" /></label><label><span>Contactor</span><input v-model="form.contactorName" @input="formDirty = true" /></label>
        <label><span>TEL</span><input v-model="form.telNo" @input="formDirty = true" /></label><label><span>Fax</span><input v-model="form.faxNo" @input="formDirty = true" /></label><label class="wide"><span>Email</span><input v-model="form.email" @input="formDirty = true" /></label><label class="wide"><span>Address</span><input v-model="form.address" @input="formDirty = true" /></label><label><span>Bank No</span><input v-model="form.bankNo" @input="formDirty = true" /></label>
      </div></fieldset><div class="taker-audit"><label>建立人<input :value="audit.creator" readonly /></label><label>建立日期<input :value="formatDate(audit.createDate)" readonly /></label><label>修改人<input :value="audit.reviser" readonly /></label><label>修改日期<input :value="formatDate(audit.reviseDate)" readonly /></label></div><p v-if="formError" class="taker-message error" role="alert">{{ formError }}</p></div>
      <footer class="taker-dialog-actions"><button type="button" :disabled="busy" @click="closeForm">關閉</button><button type="button" :disabled="busy || !formDirty" @click="saveForm(false)">套用</button><button class="primary" type="submit" :disabled="busy">確定</button></footer>
    </form></div>
  </div>
</template>

<style scoped>
.taker-page input[type=radio],.taker-page input[type=checkbox]{accent-color:#b59a00}.taker-page{display:flex;min-height:0;flex-direction:column;gap:7px;padding:8px;background:#f8f7f3;color:#333;font-size:13px}.taker-query{display:flex;min-height:40px;align-items:center;gap:8px;padding:5px 10px;border:1px solid #d0cec7;background:#fff}.taker-state-filter{display:flex;align-items:center;gap:12px;margin-right:10px}.taker-state-filter label{display:flex;align-items:center;gap:4px;white-space:nowrap}.taker-query>label{margin-left:2px}.taker-query>input{width:190px;height:27px;padding:4px 6px;border:1px solid #aaa}.taker-query>span{margin-left:-6px}.taker-query>button,.taker-actions button,.taker-dialog-actions button{min-height:29px;padding:4px 9px;border:1px solid #c4c4c4;border-radius:3px;background:linear-gradient(#fff,#f4f3ef);color:#333}.taker-list{display:flex;min-height:0;flex:1;flex-direction:column;border:1px solid #c7c4bb;background:#fff}.taker-list-layout{display:grid;min-height:260px;flex:1;grid-template-columns:minmax(0,1fr) 92px;gap:8px;padding:7px}.taker-table-scroll{min-width:0;overflow:auto}.taker-table{width:max-content;min-width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}.taker-table th{position:sticky;top:0;z-index:1;height:30px;padding:5px 8px;border:1px solid #9cb6c3;background:#56a9df;color:#fff;text-align:left;font-weight:500}.taker-table td{height:30px;max-width:260px;overflow:hidden;padding:5px 8px;border:1px solid #d0d0d0;text-overflow:ellipsis}.taker-table tbody tr{cursor:pointer}.taker-table tbody tr:hover{background:#faf7e7}.taker-table tbody tr.selected{background:#fff0b5;box-shadow:inset 3px 0 #b59a00}.taker-table tbody tr:focus-visible{outline:2px solid #b59a00;outline-offset:-2px}.select-col{width:27px;text-align:center}.taker-empty{text-align:center;color:#777}.taker-actions{display:flex;flex-direction:column;gap:6px}.taker-actions button:disabled,.taker-dialog-actions button:disabled{color:#aaa;cursor:not-allowed}.taker-actions button.primary,.taker-dialog-actions button.primary{border-color:#cf9b33;background:#ffad32;color:#fff;font-weight:600}.taker-print{margin-top:auto}.taker-close{margin-top:3px}.taker-count{flex:none;padding:2px 12px 8px;color:#777;font-size:11px}.taker-count strong{margin-left:7px;color:#555;font-weight:500}.taker-note{padding:6px 10px;border:1px solid #d8c77b;background:#fff9dd;color:#665810;font-size:12px}.taker-message{margin:0;padding:7px 10px;border:1px solid;font-size:12px}.taker-message.error{border-color:#d5a3a3;background:#fff5f3;color:#7a2424}.taker-message.success{border-color:#b6c9a8;background:#f5f8ef;color:#435c32}.taker-overlay{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:16px;background:rgb(20 20 16 / 35%)}.taker-dialog{display:flex;width:min(570px,100%);max-height:94vh;flex-direction:column;border:1px solid #c4c1b8;background:#fff;box-shadow:0 8px 28px rgb(0 0 0 / 20%)}.taker-dialog>header{display:flex;flex:none;align-items:center;justify-content:space-between;padding:9px 12px;border-bottom:1px solid #ddd;background:#f8f7f3}.taker-dialog>header h2{margin:0;font-size:16px;font-weight:500}.taker-dialog>header button{width:30px;height:28px;border:1px solid #ccc;background:#fff;font-size:20px;line-height:1}.taker-form-scroll{overflow:auto;padding:3px 10px}.taker-dialog fieldset{margin:9px 2px;padding:11px 13px 14px;border:1px solid #c8c5bc}.taker-dialog legend{padding:0 5px;color:#555}.taker-fields{display:grid;grid-template-columns:1fr 1fr;gap:9px 18px}.taker-fields>label{display:grid;grid-template-columns:80px minmax(0,1fr);align-items:center;gap:8px}.taker-fields input{width:100%;min-height:30px;padding:5px 7px;border:1px solid #999;border-radius:0;background:#fff0ae;color:#26251f;font:inherit}.taker-fields input:read-only{background:#eee}.taker-fields .wide{grid-column:1/-1}.taker-audit{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:8px;margin:0 2px 9px;padding:10px;border:1px solid #d6d2c7;background:#faf9f6}.taker-audit label{display:grid;gap:4px;color:#666;font-size:11px}.taker-audit input{width:100%;min-height:28px;padding:4px;border:1px solid #ccc;background:#eee;color:#555;font:inherit}.taker-dialog-actions{display:flex;flex:none;justify-content:flex-end;gap:8px;padding:8px 12px 12px;border-top:1px solid #ddd}@media(max-width:900px){.taker-state-filter{gap:6px;margin-right:2px}.taker-query{flex-wrap:wrap}.taker-fields,.taker-audit{grid-template-columns:1fr}.taker-fields .wide{grid-column:auto}.taker-fields>label{grid-template-columns:90px minmax(0,1fr)}}
</style>
