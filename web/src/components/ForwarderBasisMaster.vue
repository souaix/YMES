<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

interface Forwarder {
  forwarderNo: string
  contactorName: string
  telNo: string
  faxNo: string
  email: string
  address: string
  brokerNo: string
  bankNo: string
  acNo: string
  issueState: number
  creator: string | null
  createDate: string | null
  dataStamp: number | null
  reviser: string | null
  reviseDate: string | null
}
interface BrokerOption { brokerNo: string; contactorName?: string; issueState: number }
type ForwarderForm = Pick<Forwarder, 'forwarderNo' | 'contactorName' | 'telNo' | 'faxNo' | 'email' | 'address' | 'brokerNo' | 'bankNo' | 'acNo'>
type IssueFilter = 0 | 1 | 2 | -1

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mockMode = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes-dev-mock:ent-forwarder-basis:v1'
const brokerStorageKey = 'ymes-dev-mock:ent-broker-basis:v1'
const rows = ref<Forwarder[]>([])
const filterState = ref<IssueFilter>(0)
const keyword = ref('')
const selectedKeys = ref<string[]>([])
const activeKey = ref('')
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const formMode = ref<'add' | 'edit' | null>(null)
const form = ref<ForwarderForm>(blankForm())
const formDirty = ref(false)
const originalDataStamp = ref<number | null>(null)
const audit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const formError = ref('')
const pickerOpen = ref(false)
const pickerBusy = ref(false)
const pickerError = ref('')
const brokerKeyword = ref('')
const brokerOptions = ref<BrokerOption[]>([])

function blankForm(): ForwarderForm {
  return { forwarderNo: '', contactorName: '', telNo: '', faxNo: '', email: '', address: '', brokerNo: '', bankNo: '', acNo: '' }
}

const seedRows: Forwarder[] = [
  { forwarderNo: 'DEV-FORWARDER-01', contactorName: '開發用貨運行甲', telNo: '02-0000-0301', faxNo: '02-0000-0302', email: 'forwarder.a@example.test', address: '台北市範例路 3 號', brokerNo: 'DEV-BROKER-02', bankNo: 'DEV-BANK-02', acNo: 'DEV-AC-01', issueState: 0, creator: 'DEV001', createDate: '2026-11-08T08:00:00+08:00', dataStamp: 0, reviser: null, reviseDate: null },
  { forwarderNo: 'DEV-FORWARDER-02', contactorName: '開發用貨運行乙', telNo: '03-0000-0401', faxNo: '', email: 'forwarder.b@example.test', address: '桃園市測試路 4 號', brokerNo: 'DEV-BROKER-02', bankNo: '', acNo: '', issueState: 2, creator: 'DEV001', createDate: '2026-11-09T08:00:00+08:00', dataStamp: 2, reviser: 'DEV002', reviseDate: '2026-11-10T09:00:00+08:00' },
  { forwarderNo: 'DEV-FORWARDER-03', contactorName: '', telNo: '', faxNo: '', email: '', address: '', brokerNo: 'DEV-BROKER-02', bankNo: '', acNo: '', issueState: 1, creator: 'DEV001', createDate: '2026-11-11T08:00:00+08:00', dataStamp: 1, reviser: 'DEV002', reviseDate: '2026-11-12T09:00:00+08:00' },
  { forwarderNo: 'DEV-FORWARDER-04', contactorName: '', telNo: '', faxNo: '', email: '', address: '', brokerNo: 'DEV-BROKER-02', bankNo: '', acNo: '', issueState: -1, creator: 'DEV001', createDate: '2026-11-13T08:00:00+08:00', dataStamp: 2, reviser: 'DEV002', reviseDate: '2026-11-14T09:00:00+08:00' },
]
const seedBrokers: BrokerOption[] = [{ brokerNo: 'DEV-BROKER-02', contactorName: '開發用報關行乙', issueState: 2 }]

const visibleRows = computed(() => {
  const term = keyword.value.trim().toLocaleLowerCase()
  return rows.value.filter(row => row.issueState === filterState.value)
    .filter(row => !term || row.forwarderNo.toLocaleLowerCase().includes(term))
    .sort((a, b) => a.forwarderNo.localeCompare(b.forwarderNo))
})
const activeRow = computed(() => rows.value.find(row => row.forwarderNo === activeKey.value) ?? null)
const allVisibleSelected = computed(() => visibleRows.value.length > 0 && visibleRows.value.every(row => selectedKeys.value.includes(row.forwarderNo)))
const visibleBrokers = computed(() => brokerOptions.value.filter(row => row.issueState === 2 && (!brokerKeyword.value.trim() || row.brokerNo.toLocaleLowerCase().includes(brokerKeyword.value.trim().toLocaleLowerCase()) || (row.contactorName || '').toLocaleLowerCase().includes(brokerKeyword.value.trim().toLocaleLowerCase()))))

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
    if (response.status === 404) throw new Error('貨運行基本資料 API 尚未建立；請依 todolist_homepc.md 完成 ET104 API。')
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 回應錯誤 (${response.status})。`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function readMockRows(): Forwarder[] {
  try { const saved = window.localStorage.getItem(storageKey); return saved ? JSON.parse(saved) as Forwarder[] : structuredClone(seedRows) }
  catch { return structuredClone(seedRows) }
}
function saveMockRows(value: Forwarder[]): void { window.localStorage.setItem(storageKey, JSON.stringify(value)) }
function readMockBrokers(): BrokerOption[] {
  try {
    const saved = window.localStorage.getItem(brokerStorageKey)
    const all = saved ? JSON.parse(saved) as BrokerOption[] : seedBrokers
    const approved = all.filter(row => row.issueState === 2)
    return approved.length ? approved : seedBrokers
  } catch { return seedBrokers }
}
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
      if (keyword.value.trim()) query.set('forwarderNo', keyword.value.trim())
      const queryString = query.toString()
      rows.value = await requestJson<Forwarder[]>(`api/ent/forwarder-basis${queryString ? `?${queryString}` : ''}`)
    }
    selectedKeys.value = []; activeKey.value = ''
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '貨運行清單載入失敗。'; rows.value = [] }
  finally { loading.value = false }
}
function selectRow(row: Forwarder): void { activeKey.value = row.forwarderNo; selectedKeys.value = [row.forwarderNo] }
function toggleRow(row: Forwarder, checked: boolean): void {
  selectedKeys.value = checked ? [...new Set([...selectedKeys.value, row.forwarderNo])] : selectedKeys.value.filter(key => key !== row.forwarderNo)
  activeKey.value = row.forwarderNo
}
function toggleAll(checked: boolean): void { selectedKeys.value = checked ? visibleRows.value.map(row => row.forwarderNo) : []; activeKey.value = checked ? (visibleRows.value[0]?.forwarderNo ?? '') : '' }
function setFilter(value: IssueFilter): void { filterState.value = value; selectedKeys.value = []; activeKey.value = '' }
function formFromRow(row: Forwarder): ForwarderForm {
  return { forwarderNo: row.forwarderNo, contactorName: row.contactorName || '', telNo: row.telNo || '', faxNo: row.faxNo || '', email: row.email || '', address: row.address || '', brokerNo: row.brokerNo || '', bankNo: row.bankNo || '', acNo: row.acNo || '' }
}
function openForm(mode: 'add' | 'edit'): void {
  errorMessage.value = ''; successMessage.value = ''; formError.value = ''; formDirty.value = false
  if (mode === 'edit' && (!activeRow.value || ![0, 2].includes(activeRow.value.issueState))) return
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
function closeForm(): void { formMode.value = null; formError.value = ''; formDirty.value = false; pickerOpen.value = false }
function normalizeForwarderNo(): void { form.value.forwarderNo = form.value.forwarderNo.toLocaleUpperCase() }
function formPayload(): ForwarderForm { return { ...form.value, forwarderNo: form.value.forwarderNo.trim().toLocaleUpperCase(), brokerNo: form.value.brokerNo.trim().toLocaleUpperCase() } }
async function openBrokerPicker(): Promise<void> {
  pickerOpen.value = true; pickerError.value = ''; brokerKeyword.value = ''; pickerBusy.value = true
  try {
    if (mockMode.value) brokerOptions.value = readMockBrokers()
    else brokerOptions.value = await requestJson<BrokerOption[]>('api/ent/broker-basis?issueState=2')
    if (!brokerOptions.value.some(row => row.issueState === 2)) pickerError.value = '目前沒有已簽核的報關行。'
  } catch (error) { pickerError.value = error instanceof Error ? error.message : '已簽核報關行載入失敗。' }
  finally { pickerBusy.value = false }
}
function chooseBroker(row: BrokerOption): void { if (row.issueState === 2) { form.value.brokerNo = row.brokerNo; formDirty.value = true; pickerOpen.value = false } }
async function saveForm(closeAfterSave: boolean): Promise<void> {
  if (!formMode.value) return
  normalizeForwarderNo()
  if (!form.value.forwarderNo.trim()) { formError.value = '請輸入 Forwarder No。'; return }
  if (!form.value.brokerNo.trim()) { formError.value = '請選擇已簽核的 Broker No。'; return }
  const payload = formPayload()
  if (payload.forwarderNo.length > 50 || payload.contactorName.length > 50 || payload.telNo.length > 25 || payload.faxNo.length > 25 || payload.email.length > 255 || payload.address.length > 500 || payload.brokerNo.length > 50 || payload.bankNo.length > 20 || payload.acNo.length > 50) { formError.value = '欄位長度超過 Oracle 資料表定義。'; return }
  busy.value = true; formError.value = ''; errorMessage.value = ''
  const wasAdd = formMode.value === 'add'
  try {
    let saved: Forwarder
    if (mockMode.value) {
      const data = readMockRows(); const now = new Date().toISOString()
      if (wasAdd) {
        if (data.some(row => row.forwarderNo === payload.forwarderNo)) throw new Error('Forwarder No 已存在。')
        saved = { ...payload, issueState: 0, creator: props.currentUserNo || 'DEV001', createDate: now, dataStamp: 0, reviser: null, reviseDate: null }; data.push(saved)
      } else {
        const index = data.findIndex(row => row.forwarderNo === payload.forwarderNo)
        if (index < 0) throw new Error('資料已不存在，請重新查詢。')
        if (![0, 2].includes(data[index].issueState)) throw new Error('目前狀態不允許修改。')
        saved = { ...data[index], ...payload, reviser: props.currentUserNo || 'DEV001', reviseDate: now, dataStamp: (data[index].dataStamp ?? 0) + 1 }; data[index] = saved
      }
      saveMockRows(data)
    } else if (wasAdd) {
      saved = await requestJson<Forwarder>('api/ent/forwarder-basis', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
      saved = saved || { ...payload, issueState: 0, creator: audit.value.creator, createDate: audit.value.createDate, dataStamp: 0, reviser: null, reviseDate: null }
    } else {
      const query = new URLSearchParams({ dataStamp: String(originalDataStamp.value ?? 0) })
      saved = await requestJson<Forwarder>(`api/ent/forwarder-basis/${encodeURIComponent(payload.forwarderNo)}?${query}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
      saved = saved || { ...payload, issueState: activeRow.value?.issueState ?? 0, creator: audit.value.creator, createDate: audit.value.createDate, dataStamp: (originalDataStamp.value ?? 0) + 1, reviser: props.currentUserNo || '', reviseDate: new Date().toISOString() }
    }
    await loadRows()
    const savedRowIsVisible = visibleRows.value.some(row => row.forwarderNo === saved.forwarderNo)
    activeKey.value = savedRowIsVisible ? saved.forwarderNo : ''
    selectedKeys.value = savedRowIsVisible ? [saved.forwarderNo] : []
    if (wasAdd && !closeAfterSave) {
      formMode.value = 'edit'; originalDataStamp.value = saved.dataStamp ?? 0; form.value = formFromRow(saved)
      audit.value = { creator: saved.creator || '', createDate: saved.createDate || '', reviser: props.currentUserNo || '', reviseDate: new Date().toISOString() }
    } else if (!closeAfterSave) {
      originalDataStamp.value = saved.dataStamp ?? 0; form.value = formFromRow(saved)
      audit.value = { creator: saved.creator || '', createDate: saved.createDate || '', reviser: props.currentUserNo || '', reviseDate: new Date().toISOString() }
    }
    successMessage.value = wasAdd ? '貨運行資料已新增。' : '貨運行資料已修改。'
    formDirty.value = false
    if (closeAfterSave) closeForm()
  } catch (error) { formError.value = error instanceof Error ? error.message : '貨運行資料儲存失敗。' }
  finally { busy.value = false }
}
async function deleteSelected(): Promise<void> {
  const targets = rows.value.filter(row => selectedKeys.value.includes(row.forwarderNo))
  if (!targets.length) { errorMessage.value = '請先選取要刪除的貨運行。'; return }
  if (!window.confirm(`確定刪除／停用選取的 ${targets.length} 筆貨運行資料？`)) return
  busy.value = true; errorMessage.value = ''; successMessage.value = ''; let completed = 0
  try {
    for (const row of targets) {
      if (![0, -1, 2].includes(row.issueState)) throw new Error(`「${row.forwarderNo}」目前為${stateLabel(row.issueState)}，不允許刪除。`)
      if (mockMode.value) {
        const data = readMockRows()
        if (row.issueState === 2) { const index = data.findIndex(item => item.forwarderNo === row.forwarderNo); if (index >= 0) data[index] = { ...data[index], issueState: -1, reviser: props.currentUserNo || 'DEV001', reviseDate: new Date().toISOString(), dataStamp: (data[index].dataStamp ?? 0) + 1 }; saveMockRows(data) }
        else saveMockRows(data.filter(item => item.forwarderNo !== row.forwarderNo))
      } else {
        const query = new URLSearchParams({ issueState: String(row.issueState), dataStamp: String(row.dataStamp ?? 0) })
        await requestJson<void>(`api/ent/forwarder-basis/${encodeURIComponent(row.forwarderNo)}?${query}`, { method: 'DELETE' })
      }
      completed++
    }
    await loadRows(); successMessage.value = `已處理 ${completed} 筆貨運行資料。`
  } catch (error) { const message = error instanceof Error ? error.message : '貨運行資料刪除失敗。'; await loadRows(); errorMessage.value = message }
  finally { busy.value = false }
}
async function approveSelected(): Promise<void> {
  const targets = rows.value.filter(row => selectedKeys.value.includes(row.forwarderNo))
  if (!targets.length) { errorMessage.value = '請先選取要核准的貨運行。'; return }
  if (!window.confirm(`確定核准選取的 ${targets.length} 筆貨運行資料？`)) return
  busy.value = true; errorMessage.value = ''; successMessage.value = ''; let completed = 0
  try {
    for (const row of targets) {
      if (row.issueState !== 0) throw new Error(`「${row.forwarderNo}」目前為${stateLabel(row.issueState)}，不允許核准。`)
      if (mockMode.value) {
        const data = readMockRows(); const index = data.findIndex(item => item.forwarderNo === row.forwarderNo)
        if (index >= 0) data[index] = { ...data[index], issueState: 2, reviser: props.currentUserNo || 'DEV001', reviseDate: new Date().toISOString(), dataStamp: (data[index].dataStamp ?? 0) + 1 }
        saveMockRows(data)
      } else { const query = new URLSearchParams({ dataStamp: String(row.dataStamp ?? 0) }); await requestJson<void>(`api/ent/forwarder-basis/${encodeURIComponent(row.forwarderNo)}/approve?${query}`, { method: 'POST' }) }
      completed++
    }
    await loadRows(); successMessage.value = `已送出 ${completed} 筆核准。DEV 假資料以直接核准方式模擬。`
  } catch (error) { const message = error instanceof Error ? error.message : '貨運行核准失敗。'; await loadRows(); errorMessage.value = message }
  finally { busy.value = false }
}
function closePage(): void { rows.value = []; selectedKeys.value = []; activeKey.value = ''; errorMessage.value = ''; successMessage.value = '' }
onMounted(() => { if (mockMode.value && !window.localStorage.getItem(storageKey)) saveMockRows(seedRows); void loadRows() })
</script>

<template>
  <div class="forwarder-page">
    <div class="forwarder-query">
      <div class="forwarder-state-filter" role="radiogroup" aria-label="簽核狀態">
        <label><input type="radio" :checked="filterState === 0" @change="setFilter(0)" />未簽核</label>
        <label><input type="radio" :checked="filterState === 1" @change="setFilter(1)" />簽核中</label>
        <label><input type="radio" :checked="filterState === 2" @change="setFilter(2)" />已簽核</label>
        <label><input type="radio" :checked="filterState === -1" @change="setFilter(-1)" />不使用</label>
      </div>
      <label for="forwarder-no-search">貨運行編號</label><input id="forwarder-no-search" v-model="keyword" @keyup.enter="loadRows" /> <span>%</span>
      <button type="button" :disabled="loading || busy" @click="loadRows">查詢</button>
    </div>
    <div v-if="mockMode" class="forwarder-note">Development 假資料｜只儲存在此瀏覽器；已簽核報關行選擇器使用 ET103 假資料。</div>
    <div v-if="errorMessage" class="forwarder-message error" role="alert">{{ errorMessage }}</div>
    <div v-if="successMessage" class="forwarder-message success" role="status">{{ successMessage }}</div>
    <section class="forwarder-list"><div class="forwarder-list-layout"><div class="forwarder-table-scroll">
      <table class="forwarder-table"><thead><tr>
        <th class="select-col"><input type="checkbox" aria-label="選取所有貨運行" :checked="allVisibleSelected" @change="toggleAll(($event.target as HTMLInputElement).checked)" /></th>
        <th>貨運行編號</th><th>連絡人</th><th>電話</th><th>傳真</th><th>Email</th><th>地址</th><th>報關行編號</th><th>銀行編號</th><th>建立人</th><th>建立日期</th><th>AC No</th><th>修改人</th><th>修改日期</th>
      </tr></thead><tbody>
        <tr v-for="row in visibleRows" :key="row.forwarderNo" tabindex="0" :aria-selected="selectedKeys.includes(row.forwarderNo)" :class="{ selected: selectedKeys.includes(row.forwarderNo) }" @click="selectRow(row)" @keydown.enter.prevent="selectRow(row)">
          <td class="select-col" @click.stop><input type="checkbox" :aria-label="`選取貨運行 ${row.forwarderNo}`" :checked="selectedKeys.includes(row.forwarderNo)" @change="toggleRow(row, ($event.target as HTMLInputElement).checked)" /></td>
          <td>{{ row.forwarderNo }}</td><td>{{ row.contactorName }}</td><td>{{ row.telNo }}</td><td>{{ row.faxNo }}</td><td>{{ row.email }}</td><td>{{ row.address }}</td><td>{{ row.brokerNo }}</td><td>{{ row.bankNo }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.acNo }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td>
        </tr>
        <tr v-if="loading"><td colspan="14" class="forwarder-empty">資料載入中…</td></tr>
        <tr v-else-if="visibleRows.length === 0 && !errorMessage"><td colspan="14" class="forwarder-empty">沒有符合的貨運行資料。</td></tr>
      </tbody></table>
    </div><div class="forwarder-actions">
      <button class="primary" type="button" :disabled="busy" @click="openForm('add')">新增(A)</button>
      <button type="button" :disabled="busy || !activeRow || ![0,2].includes(activeRow.issueState)" @click="openForm('edit')">修改(E)</button>
      <button type="button" :disabled="busy || selectedKeys.length === 0" @click="deleteSelected">刪除(D)</button>
      <button type="button" :disabled="busy || selectedKeys.length === 0" @click="approveSelected">核准</button>
      <button class="forwarder-print" type="button" disabled>Print</button><button class="forwarder-close" type="button" @click="closePage">關閉</button>
    </div></div><footer class="forwarder-count">資料總筆數 <strong>{{ visibleRows.length }}</strong></footer></section>

    <div v-if="formMode" class="forwarder-overlay" @click.self="closeForm"><form class="forwarder-dialog" @submit.prevent="saveForm(true)">
      <header><h2>{{ formMode === 'add' ? '新增' : '修改' }}貨運行基本資料</h2><button type="button" aria-label="關閉" @click="closeForm">×</button></header>
      <div class="forwarder-form-scroll"><fieldset><legend>貨運行主檔資料</legend><div class="forwarder-fields">
        <label><span>Forwarder No *</span><input v-model="form.forwarderNo" :readonly="formMode === 'edit'" required @input="formDirty = true" @blur="normalizeForwarderNo" /></label>
        <label class="broker-field"><span>Broker No *</span><div><input v-model="form.brokerNo" readonly required /><button type="button" :disabled="formMode === 'edit'" @click="openBrokerPicker">...</button></div></label>
        <label><span>連絡人</span><input v-model="form.contactorName" @input="formDirty = true" /></label><label><span>電話</span><input v-model="form.telNo" @input="formDirty = true" /></label>
        <label><span>傳真</span><input v-model="form.faxNo" @input="formDirty = true" /></label><label><span>Email</span><input v-model="form.email" @input="formDirty = true" /></label>
        <label><span>銀行編號</span><input v-model="form.bankNo" @input="formDirty = true" /></label><label><span>AC No</span><input v-model="form.acNo" @input="formDirty = true" /></label>
        <label class="wide"><span>地址</span><textarea v-model="form.address" rows="3" @input="formDirty = true"></textarea></label>
      </div></fieldset>
      <div class="forwarder-audit"><label>建立人<input :value="audit.creator" readonly /></label><label>建立日期<input :value="formatDate(audit.createDate)" readonly /></label><label>修改人<input :value="audit.reviser" readonly /></label><label>修改日期<input :value="formatDate(audit.reviseDate)" readonly /></label></div>
      <p v-if="formError" class="forwarder-message error" role="alert">{{ formError }}</p></div>
      <footer class="forwarder-dialog-actions"><button type="button" :disabled="busy" @click="closeForm">關閉</button><button type="button" :disabled="busy || !formDirty" @click="saveForm(false)">套用</button><button class="primary" type="submit" :disabled="busy">確定</button></footer>
    </form></div>

    <div v-if="pickerOpen" class="forwarder-overlay picker-overlay" @click.self="pickerOpen = false"><section class="broker-picker" role="dialog" aria-modal="true" aria-label="選擇已簽核報關行">
      <header><h2>選擇報關行</h2><button type="button" aria-label="關閉" @click="pickerOpen = false">×</button></header>
      <div class="picker-query"><label for="broker-picker-search">報關行編號／名稱</label><input id="broker-picker-search" v-model="brokerKeyword" /></div>
      <p v-if="pickerBusy" class="picker-empty">資料載入中…</p><p v-else-if="pickerError" class="picker-empty">{{ pickerError }}</p>
      <div v-else class="picker-table-scroll"><table class="picker-table"><thead><tr><th>報關行編號</th><th>連絡人</th></tr></thead><tbody>
        <tr v-for="row in visibleBrokers" :key="row.brokerNo" tabindex="0" @click="chooseBroker(row)" @keydown.enter.prevent="chooseBroker(row)"><td>{{ row.brokerNo }}</td><td>{{ row.contactorName }}</td></tr>
        <tr v-if="!visibleBrokers.length"><td colspan="2" class="picker-empty">沒有符合的已簽核報關行。</td></tr>
      </tbody></table></div>
      <footer><button type="button" @click="pickerOpen = false">關閉</button></footer>
    </section></div>
  </div>
</template>

<style scoped>
.forwarder-page input[type=radio],.forwarder-page input[type=checkbox]{accent-color:#b59a00}
.forwarder-page{display:flex;min-height:0;flex-direction:column;gap:7px;padding:8px;background:#f8f7f3;color:#333;font-size:13px}.forwarder-query{display:flex;min-height:40px;align-items:center;gap:8px;padding:5px 10px;border:1px solid #d0cec7;background:#fff}.forwarder-state-filter{display:flex;align-items:center;gap:12px;margin-right:10px}.forwarder-state-filter label{display:flex;align-items:center;gap:4px;white-space:nowrap}.forwarder-query>label{margin-left:2px}.forwarder-query>input{width:190px;height:27px;padding:4px 6px;border:1px solid #aaa}.forwarder-query>span{margin-left:-6px}.forwarder-query>button,.forwarder-actions button,.forwarder-dialog-actions button,.broker-picker footer button{min-height:29px;padding:4px 9px;border:1px solid #c4c4c4;border-radius:3px;background:linear-gradient(#fff,#f4f3ef);color:#333}.forwarder-list{display:flex;min-height:0;flex:1;flex-direction:column;border:1px solid #c7c4bb;background:#fff}.forwarder-list-layout{display:grid;min-height:260px;flex:1;grid-template-columns:minmax(0,1fr) 92px;gap:8px;padding:7px}.forwarder-table-scroll,.picker-table-scroll{min-width:0;overflow:auto}.forwarder-table{width:max-content;min-width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}.forwarder-table th,.picker-table th{position:sticky;top:0;z-index:1;height:30px;padding:5px 8px;border:1px solid #9cb6c3;background:#56a9df;color:#fff;text-align:left;font-weight:500}.forwarder-table td,.picker-table td{height:30px;max-width:260px;overflow:hidden;padding:5px 8px;border:1px solid #d0d0d0;text-overflow:ellipsis}.forwarder-table tbody tr,.picker-table tbody tr{cursor:pointer}.forwarder-table tbody tr:hover,.picker-table tbody tr:hover{background:#faf7e7}.forwarder-table tbody tr.selected{background:#fff0b5;box-shadow:inset 3px 0 #b59a00}.forwarder-table tbody tr:focus-visible,.picker-table tbody tr:focus-visible{outline:2px solid #b59a00;outline-offset:-2px}.select-col{width:27px;text-align:center}.forwarder-empty,.picker-empty{text-align:center;color:#777}.forwarder-actions{display:flex;flex-direction:column;gap:6px}.forwarder-actions button:disabled,.forwarder-dialog-actions button:disabled{color:#aaa;cursor:not-allowed}.forwarder-actions button.primary,.forwarder-dialog-actions button.primary{border-color:#cf9b33;background:#ffad32;color:#fff;font-weight:600}.forwarder-print{margin-top:auto}.forwarder-close{margin-top:3px}.forwarder-count{flex:none;padding:2px 12px 8px;color:#777;font-size:11px}.forwarder-count strong{margin-left:7px;color:#555;font-weight:500}.forwarder-note{padding:6px 10px;border:1px solid #d8c77b;background:#fff9dd;color:#665810;font-size:12px}.forwarder-message{margin:0;padding:7px 10px;border:1px solid;font-size:12px}.forwarder-message.error{border-color:#d5a3a3;background:#fff5f3;color:#7a2424}.forwarder-message.success{border-color:#b6c9a8;background:#f5f8ef;color:#435c32}.forwarder-overlay{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:16px;background:rgb(20 20 16 / 35%)}.forwarder-dialog,.broker-picker{display:flex;width:min(650px,100%);max-height:94vh;flex-direction:column;border:1px solid #c4c1b8;background:#fff;box-shadow:0 8px 28px rgb(0 0 0 / 20%)}.forwarder-dialog>header,.broker-picker>header{display:flex;flex:none;align-items:center;justify-content:space-between;padding:9px 12px;border-bottom:1px solid #ddd;background:#f8f7f3}.forwarder-dialog>header h2,.broker-picker>header h2{margin:0;font-size:16px;font-weight:500}.forwarder-dialog>header button,.broker-picker>header button{width:30px;height:28px;border:1px solid #ccc;background:#fff;font-size:20px;line-height:1}.forwarder-form-scroll{overflow:auto;padding:3px 10px}.forwarder-dialog fieldset{margin:9px 2px;padding:11px 13px 14px;border:1px solid #c8c5bc}.forwarder-dialog legend{padding:0 5px;color:#555}.forwarder-fields{display:grid;grid-template-columns:1fr 1fr;gap:9px 18px}.forwarder-fields>label{display:grid;grid-template-columns:105px minmax(0,1fr);align-items:center;gap:8px}.forwarder-fields input,.forwarder-fields textarea{width:100%;min-height:30px;padding:5px 7px;border:1px solid #999;border-radius:0;background:#fff0ae;color:#26251f;font:inherit}.forwarder-fields input:read-only{background:#eee}.forwarder-fields textarea{resize:vertical}.forwarder-fields .wide{grid-column:1/-1}.forwarder-fields .broker-field>div{display:flex;gap:4px}.forwarder-fields .broker-field>div input{min-width:0}.forwarder-fields .broker-field>div button{width:36px;border:1px solid #aaa;background:#f4f3ef}.forwarder-audit{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:8px;margin:0 2px 9px;padding:10px;border:1px solid #d6d2c7;background:#faf9f6}.forwarder-audit label{display:grid;gap:4px;color:#666;font-size:11px}.forwarder-audit input{width:100%;min-height:28px;padding:4px;border:1px solid #ccc;background:#eee;color:#555;font:inherit}.forwarder-dialog-actions,.broker-picker>footer{display:flex;flex:none;justify-content:flex-end;gap:8px;padding:8px 12px 12px;border-top:1px solid #ddd}.picker-overlay{z-index:1010}.broker-picker{width:min(520px,100%);height:min(430px,88vh)}.picker-query{display:flex;align-items:center;gap:10px;padding:10px;border-bottom:1px solid #ddd}.picker-query input{flex:1;min-width:0;height:28px;padding:4px 6px;border:1px solid #aaa}.broker-picker .picker-empty{padding:14px}.picker-table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}.broker-picker>footer{margin-top:auto}@media(max-width:950px){.forwarder-state-filter{gap:6px;margin-right:2px}.forwarder-query{flex-wrap:wrap}.forwarder-fields,.forwarder-audit{grid-template-columns:1fr}.forwarder-fields .wide{grid-column:auto}.forwarder-fields>label{grid-template-columns:90px minmax(0,1fr)}}
</style>
