<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

interface Broker {
  brokerNo: string
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

type BrokerForm = Pick<Broker, 'brokerNo' | 'contactorName' | 'telNo' | 'faxNo' | 'email' | 'address' | 'bankNo'>
type IssueFilter = 0 | 1 | 2 | -1

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mockMode = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes-dev-mock:ent-broker-basis:v1'
const rows = ref<Broker[]>([])
const filterState = ref<IssueFilter>(0)
const keyword = ref('')
const selectedKeys = ref<string[]>([])
const activeKey = ref('')
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const formMode = ref<'add' | 'edit' | null>(null)
const form = ref<BrokerForm>(blankForm())
const originalDataStamp = ref<number | null>(null)
const audit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const formError = ref('')

function blankForm(): BrokerForm {
  return { brokerNo: '', contactorName: '', telNo: '', faxNo: '', email: '', address: '', bankNo: '' }
}

const seedRows: Broker[] = [
  { brokerNo: 'DEV-BROKER-01', contactorName: '開發用報關行甲', telNo: '02-0000-0101', faxNo: '02-0000-0102', email: 'broker.a@example.test', address: '台北市開發路 1 號', bankNo: 'DEV-BANK-01', issueState: 0, creator: 'DEV001', createDate: '2026-11-01T08:00:00+08:00', dataStamp: 0, reviser: null, reviseDate: null },
  { brokerNo: 'DEV-BROKER-02', contactorName: '開發用報關行乙', telNo: '02-0000-0201', faxNo: '', email: 'broker.b@example.test', address: '新北市範例路 2 號', bankNo: '', issueState: 2, creator: 'DEV001', createDate: '2026-11-02T08:00:00+08:00', dataStamp: 2, reviser: 'DEV002', reviseDate: '2026-11-03T09:00:00+08:00' },
  { brokerNo: 'DEV-BROKER-03', contactorName: '開發用報關行丙', telNo: '', faxNo: '', email: '', address: '', bankNo: '', issueState: 1, creator: 'DEV001', createDate: '2026-11-04T08:00:00+08:00', dataStamp: 1, reviser: 'DEV002', reviseDate: '2026-11-05T09:00:00+08:00' },
  { brokerNo: 'DEV-BROKER-04', contactorName: '開發用報關行丁', telNo: '', faxNo: '', email: '', address: '', bankNo: '', issueState: -1, creator: 'DEV001', createDate: '2026-11-06T08:00:00+08:00', dataStamp: 2, reviser: 'DEV002', reviseDate: '2026-11-07T09:00:00+08:00' },
]

const visibleRows = computed(() => {
  const term = keyword.value.trim().toLocaleLowerCase()
  return rows.value
    .filter(row => row.issueState === filterState.value)
    .filter(row => !term || row.brokerNo.toLocaleLowerCase().includes(term))
    .sort((a, b) => a.brokerNo.localeCompare(b.brokerNo))
})
const activeRow = computed(() => rows.value.find(row => row.brokerNo === activeKey.value) ?? null)
const allVisibleSelected = computed(() => visibleRows.value.length > 0 && visibleRows.value.every(row => selectedKeys.value.includes(row.brokerNo)))

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
    if (response.status === 404) throw new Error('報關行基本資料 API 尚未建立；請依 todolist_homepc.md 完成 ET103 API。')
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 回應錯誤 (${response.status})。`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}

function readMockRows(): Broker[] {
  try {
    const saved = window.localStorage.getItem(storageKey)
    return saved ? JSON.parse(saved) as Broker[] : structuredClone(seedRows)
  } catch { return structuredClone(seedRows) }
}
function saveMockRows(value: Broker[]): void { window.localStorage.setItem(storageKey, JSON.stringify(value)) }
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
      if (keyword.value.trim()) query.set('brokerNo', keyword.value.trim())
      const queryString = query.toString()
      rows.value = await requestJson<Broker[]>(`api/ent/broker-basis${queryString ? `?${queryString}` : ''}`)
    }
    selectedKeys.value = []; activeKey.value = ''
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '報關行清單載入失敗。'; rows.value = [] }
  finally { loading.value = false }
}

function selectRow(row: Broker): void {
  activeKey.value = row.brokerNo
  selectedKeys.value = [row.brokerNo]
}
function toggleRow(row: Broker, checked: boolean): void {
  selectedKeys.value = checked ? [...new Set([...selectedKeys.value, row.brokerNo])] : selectedKeys.value.filter(key => key !== row.brokerNo)
  activeKey.value = row.brokerNo
}
function toggleAll(checked: boolean): void {
  selectedKeys.value = checked ? visibleRows.value.map(row => row.brokerNo) : []
  activeKey.value = checked ? (visibleRows.value[0]?.brokerNo ?? '') : ''
}
function setFilter(value: IssueFilter): void { filterState.value = value; selectedKeys.value = []; activeKey.value = '' }

function openForm(mode: 'add' | 'edit'): void {
  errorMessage.value = ''; successMessage.value = ''; formError.value = ''
  if (mode === 'edit' && (!activeRow.value || activeRow.value.issueState !== 0)) return
  formMode.value = mode
  if (mode === 'add') {
    form.value = blankForm(); originalDataStamp.value = 0
    audit.value = { creator: props.currentUserNo || (mockMode.value ? 'DEV001' : ''), createDate: new Date().toISOString(), reviser: '', reviseDate: '' }
  } else {
    const row = activeRow.value!
    form.value = { brokerNo: row.brokerNo, contactorName: row.contactorName || '', telNo: row.telNo || '', faxNo: row.faxNo || '', email: row.email || '', address: row.address || '', bankNo: row.bankNo || '' }
    originalDataStamp.value = row.dataStamp
    audit.value = { creator: row.creator || '', createDate: row.createDate || '', reviser: props.currentUserNo || '', reviseDate: new Date().toISOString() }
  }
}
function closeForm(): void { formMode.value = null; formError.value = '' }
function normalizeBrokerNo(): void { form.value.brokerNo = form.value.brokerNo.toLocaleUpperCase() }
function formPayload(): BrokerForm { return { ...form.value, brokerNo: form.value.brokerNo.trim().toLocaleUpperCase() } }

async function saveForm(closeAfterSave: boolean): Promise<void> {
  if (!formMode.value) return
  normalizeBrokerNo()
  if (!form.value.brokerNo.trim()) { formError.value = '請輸入 Broker No。'; return }
  const payload = formPayload()
  if (payload.brokerNo.length > 50 || payload.contactorName.length > 50 || payload.telNo.length > 25 || payload.faxNo.length > 25 || payload.email.length > 255 || payload.address.length > 500 || payload.bankNo.length > 20) {
    formError.value = '欄位長度超過 Oracle 資料表定義。'; return
  }
  busy.value = true; formError.value = ''; errorMessage.value = ''
  const wasAdd = formMode.value === 'add'
  try {
    let saved: Broker
    if (mockMode.value) {
      const data = readMockRows()
      const now = new Date().toISOString()
      if (wasAdd) {
        if (data.some(row => row.brokerNo === payload.brokerNo)) throw new Error('Broker No 已存在。')
        saved = { ...payload, issueState: 0, creator: props.currentUserNo || 'DEV001', createDate: now, dataStamp: 0, reviser: null, reviseDate: null }
        data.push(saved)
      } else {
        const index = data.findIndex(row => row.brokerNo === payload.brokerNo)
        if (index < 0) throw new Error('資料已不存在，請重新查詢。')
        saved = { ...data[index], ...payload, reviser: props.currentUserNo || 'DEV001', reviseDate: now, dataStamp: (data[index].dataStamp ?? 0) + 1 }
        data[index] = saved
      }
      saveMockRows(data)
    } else if (wasAdd) {
      saved = await requestJson<Broker>('api/ent/broker-basis', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
      saved = saved || { ...payload, issueState: 0, creator: audit.value.creator, createDate: audit.value.createDate, dataStamp: 0, reviser: null, reviseDate: null }
    } else {
      const query = new URLSearchParams({ dataStamp: String(originalDataStamp.value ?? 0) })
      saved = await requestJson<Broker>(`api/ent/broker-basis/${encodeURIComponent(payload.brokerNo)}?${query}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
      saved = saved || { ...payload, issueState: 0, creator: audit.value.creator, createDate: audit.value.createDate, dataStamp: (originalDataStamp.value ?? 0) + 1, reviser: props.currentUserNo || '', reviseDate: new Date().toISOString() }
    }
    await loadRows()
    activeKey.value = saved.brokerNo; selectedKeys.value = [saved.brokerNo]
    if (wasAdd && !closeAfterSave) {
      // WinForms keeps ADD mode after Apply, so a second Apply can hit the PK again.
      // Transition to EDIT after the first insert to make continued editing safe.
      formMode.value = 'edit'; originalDataStamp.value = saved.dataStamp ?? 0
      form.value = { brokerNo: saved.brokerNo, contactorName: saved.contactorName || '', telNo: saved.telNo || '', faxNo: saved.faxNo || '', email: saved.email || '', address: saved.address || '', bankNo: saved.bankNo || '' }
      audit.value = { creator: saved.creator || '', createDate: saved.createDate || '', reviser: props.currentUserNo || '', reviseDate: new Date().toISOString() }
    }
    successMessage.value = wasAdd ? '報關行資料已新增。' : '報關行資料已修改。'
    if (closeAfterSave) closeForm()
  } catch (error) { formError.value = error instanceof Error ? error.message : '報關行資料儲存失敗。' }
  finally { busy.value = false }
}

async function deleteSelected(): Promise<void> {
  const targets = rows.value.filter(row => selectedKeys.value.includes(row.brokerNo))
  if (targets.length === 0) { errorMessage.value = '請先選取要刪除的報關行。'; return }
  if (!window.confirm(`確定刪除／停用選取的 ${targets.length} 筆報關行資料？`)) return
  busy.value = true; errorMessage.value = ''; successMessage.value = ''; let completed = 0
  try {
    for (const row of targets) {
      if (row.issueState !== 0 && row.issueState !== -1 && row.issueState !== 2) throw new Error(`「${row.brokerNo}」目前為${stateLabel(row.issueState)}，不允許刪除。`)
      const query = new URLSearchParams({ issueState: String(row.issueState), dataStamp: String(row.dataStamp ?? 0) })
      if (mockMode.value) {
        const data = readMockRows()
        if (row.issueState === 2) {
          const index = data.findIndex(item => item.brokerNo === row.brokerNo)
          if (index >= 0) data[index] = { ...data[index], issueState: -1, reviser: props.currentUserNo || 'DEV001', reviseDate: new Date().toISOString(), dataStamp: (data[index].dataStamp ?? 0) + 1 }
        } else saveMockRows(data.filter(item => item.brokerNo !== row.brokerNo))
        if (row.issueState === 2) saveMockRows(data)
      } else {
        await requestJson<void>(`api/ent/broker-basis/${encodeURIComponent(row.brokerNo)}?${query}`, { method: 'DELETE' })
      }
      completed++
    }
    await loadRows(); successMessage.value = `已處理 ${completed} 筆報關行資料。`
  } catch (error) { const message = error instanceof Error ? error.message : '報關行資料刪除失敗。'; await loadRows(); errorMessage.value = message }
  finally { busy.value = false }
}

async function approveSelected(): Promise<void> {
  const targets = rows.value.filter(row => selectedKeys.value.includes(row.brokerNo))
  if (targets.length === 0) { errorMessage.value = '請先選取要核准的報關行。'; return }
  if (!window.confirm(`確定核准選取的 ${targets.length} 筆報關行資料？`)) return
  busy.value = true; errorMessage.value = ''; successMessage.value = ''; let completed = 0
  try {
    for (const row of targets) {
      if (row.issueState !== 0) throw new Error(`「${row.brokerNo}」目前為${stateLabel(row.issueState)}，不允許核准。`)
      if (mockMode.value) {
        const data = readMockRows(); const index = data.findIndex(item => item.brokerNo === row.brokerNo)
        if (index >= 0) data[index] = { ...data[index], issueState: 2, reviser: props.currentUserNo || 'DEV001', reviseDate: new Date().toISOString(), dataStamp: (data[index].dataStamp ?? 0) + 1 }
        saveMockRows(data)
      } else {
        const query = new URLSearchParams({ dataStamp: String(row.dataStamp ?? 0) })
        await requestJson<void>(`api/ent/broker-basis/${encodeURIComponent(row.brokerNo)}/approve?${query}`, { method: 'POST' })
      }
      completed++
    }
    await loadRows(); successMessage.value = `已送出 ${completed} 筆核准。DEV 假資料以直接核准方式模擬。`
  } catch (error) { const message = error instanceof Error ? error.message : '報關行核准失敗。'; await loadRows(); errorMessage.value = message }
  finally { busy.value = false }
}

function closePage(): void { rows.value = []; selectedKeys.value = []; activeKey.value = ''; errorMessage.value = ''; successMessage.value = '' }

onMounted(() => {
  if (mockMode.value && !window.localStorage.getItem(storageKey)) saveMockRows(seedRows)
  void loadRows()
})
</script>

<template>
  <div class="broker-page">
    <div class="broker-query">
      <div class="broker-state-filter" role="radiogroup" aria-label="簽核狀態">
        <label><input type="radio" :checked="filterState === 0" @change="setFilter(0)" />未簽核</label>
        <label><input type="radio" :checked="filterState === 1" @change="setFilter(1)" />簽核中</label>
        <label><input type="radio" :checked="filterState === 2" @change="setFilter(2)" />已簽核</label>
        <label><input type="radio" :checked="filterState === -1" @change="setFilter(-1)" />不使用</label>
      </div>
      <label for="broker-no-search">報關行編號</label>
      <input id="broker-no-search" v-model="keyword" @keyup.enter="loadRows" />
      <span>%</span>
      <button type="button" :disabled="loading || busy" @click="loadRows">查詢</button>
    </div>
    <div v-if="mockMode" class="broker-note">Development 假資料｜只儲存在此瀏覽器，不會寫入正式資料庫；簽核流程以直接核准模擬</div>
    <div v-if="errorMessage" class="broker-message error" role="alert">{{ errorMessage }}</div>
    <div v-if="successMessage" class="broker-message success" role="status">{{ successMessage }}</div>

    <section class="broker-list">
      <div class="broker-list-layout">
        <div class="broker-table-scroll">
          <table class="broker-table"><thead><tr>
            <th class="select-col"><input type="checkbox" aria-label="選取所有報關行" :checked="allVisibleSelected" @change="toggleAll(($event.target as HTMLInputElement).checked)" /></th>
            <th>報關行編號</th><th>連絡人</th><th>電話</th><th>傳真</th><th>Email</th><th>地址</th><th>銀行編號</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th>
          </tr></thead><tbody>
            <tr v-for="row in visibleRows" :key="row.brokerNo" tabindex="0" :aria-selected="selectedKeys.includes(row.brokerNo)" :class="{ selected: selectedKeys.includes(row.brokerNo) }" @click="selectRow(row)" @keydown.enter.prevent="selectRow(row)">
              <td class="select-col" @click.stop><input type="checkbox" :aria-label="`選取報關行 ${row.brokerNo}`" :checked="selectedKeys.includes(row.brokerNo)" @change="toggleRow(row, ($event.target as HTMLInputElement).checked)" /></td>
              <td>{{ row.brokerNo }}</td><td>{{ row.contactorName }}</td><td>{{ row.telNo }}</td><td>{{ row.faxNo }}</td><td>{{ row.email }}</td><td>{{ row.address }}</td><td>{{ row.bankNo }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td>
            </tr>
            <tr v-if="loading"><td colspan="12" class="broker-empty">資料載入中…</td></tr>
            <tr v-else-if="visibleRows.length === 0 && !errorMessage"><td colspan="12" class="broker-empty">沒有符合的報關行資料。</td></tr>
          </tbody></table>
        </div>
        <div class="broker-actions">
          <button class="primary" type="button" :disabled="busy" @click="openForm('add')">新增(A)</button>
          <button type="button" :disabled="busy || !activeRow || activeRow.issueState !== 0" @click="openForm('edit')">修改(E)</button>
          <button type="button" :disabled="busy || selectedKeys.length === 0" @click="deleteSelected">刪除(D)</button>
          <button type="button" :disabled="busy || selectedKeys.length === 0" @click="approveSelected">核准</button>
          <button class="broker-print" type="button" disabled>Print</button>
          <button class="broker-close" type="button" @click="closePage">關閉</button>
        </div>
      </div>
      <footer class="broker-count">資料總筆數 <strong>{{ visibleRows.length }}</strong></footer>
    </section>

    <div v-if="formMode" class="broker-overlay" @click.self="closeForm">
      <form class="broker-dialog" @submit.prevent="saveForm(true)">
        <header><h2>{{ formMode === 'add' ? '新增' : '修改' }}報關行基本資料</h2><button type="button" aria-label="關閉" @click="closeForm">×</button></header>
        <div class="broker-form-scroll"><fieldset><legend>報關行資料</legend><div class="broker-fields">
          <label><span>Broker No *</span><input v-model="form.brokerNo" :readonly="formMode === 'edit'" required @blur="normalizeBrokerNo" /></label>
          <label><span>Contactor</span><input v-model="form.contactorName" /></label>
          <label><span>TEL</span><input v-model="form.telNo" /></label>
          <label><span>Fax</span><input v-model="form.faxNo" /></label>
          <label><span>Email</span><input v-model="form.email" /></label>
          <label><span>Bank No</span><input v-model="form.bankNo" /></label>
          <label class="wide"><span>Address</span><textarea v-model="form.address" rows="3"></textarea></label>
        </div></fieldset>
        <div class="broker-audit"><label>建立人<input :value="audit.creator" readonly /></label><label>建立日期<input :value="formatDate(audit.createDate)" readonly /></label><label>修改人<input :value="audit.reviser" readonly /></label><label>修改日期<input :value="formatDate(audit.reviseDate)" readonly /></label></div>
        <p v-if="formError" class="broker-message error" role="alert">{{ formError }}</p></div>
        <footer class="broker-dialog-actions"><button type="button" :disabled="busy" @click="closeForm">關閉</button><button type="button" :disabled="busy" @click="saveForm(false)">套用</button><button class="primary" type="submit" :disabled="busy">確定</button></footer>
      </form>
    </div>
  </div>
</template>

<style scoped>
.broker-page input[type=radio],.broker-page input[type=checkbox]{accent-color:#b59a00}
.broker-page{display:flex;min-height:0;flex-direction:column;gap:7px;padding:8px;background:#f8f7f3;color:#333;font-size:13px}.broker-query{display:flex;min-height:40px;align-items:center;gap:8px;padding:5px 10px;border:1px solid #d0cec7;background:#fff}.broker-state-filter{display:flex;align-items:center;gap:12px;margin-right:10px}.broker-state-filter label{display:flex;align-items:center;gap:4px;white-space:nowrap}.broker-query>label{margin-left:2px}.broker-query>input{width:190px;height:27px;padding:4px 6px;border:1px solid #aaa}.broker-query>span{margin-left:-3px}.broker-query>button,.broker-actions button,.broker-dialog-actions button{min-height:29px;padding:4px 9px;border:1px solid #c4c4c4;border-radius:3px;background:linear-gradient(#fff,#f4f3ef);color:#333}.broker-list{display:flex;min-height:0;flex:1;flex-direction:column;border:1px solid #c7c4bb;background:#fff}.broker-list-layout{display:grid;min-height:260px;flex:1;grid-template-columns:minmax(0,1fr) 92px;gap:8px;padding:7px}.broker-table-scroll{min-width:0;overflow:auto}.broker-table{width:max-content;min-width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}.broker-table th{position:sticky;top:0;z-index:1;height:30px;padding:5px 8px;border:1px solid #9cb6c3;background:#56a9df;color:#fff;text-align:left;font-weight:500}.broker-table td{height:30px;max-width:260px;overflow:hidden;padding:5px 8px;border:1px solid #d0d0d0;text-overflow:ellipsis}.broker-table tbody tr{cursor:pointer}.broker-table tbody tr:hover{background:#faf7e7}.broker-table tbody tr.selected{background:#fff0b5;box-shadow:inset 3px 0 #b59a00}.broker-table tbody tr:focus-visible{outline:2px solid #b59a00;outline-offset:-2px}.select-col{width:27px;text-align:center}.broker-empty{text-align:center;color:#777}.broker-actions{display:flex;flex-direction:column;gap:6px}.broker-actions button:disabled,.broker-dialog-actions button:disabled{color:#aaa;cursor:not-allowed}.broker-actions button.primary,.broker-dialog-actions button.primary{border-color:#cf9b33;background:#ffad32;color:#fff;font-weight:600}.broker-print{margin-top:auto}.broker-close{margin-top:3px}.broker-count{flex:none;padding:2px 12px 8px;color:#777;font-size:11px}.broker-count strong{margin-left:7px;color:#555;font-weight:500}.broker-note{padding:6px 10px;border:1px solid #d8c77b;background:#fff9dd;color:#665810;font-size:12px}.broker-message{margin:0;padding:7px 10px;border:1px solid;font-size:12px}.broker-message.error{border-color:#d5a3a3;background:#fff5f3;color:#7a2424}.broker-message.success{border-color:#b6c9a8;background:#f5f8ef;color:#435c32}.broker-overlay{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:16px;background:rgb(20 20 16 / 35%)}.broker-dialog{display:flex;width:min(610px,100%);max-height:94vh;flex-direction:column;border:1px solid #c4c1b8;background:#fff;box-shadow:0 8px 28px rgb(0 0 0 / 20%)}.broker-dialog>header{display:flex;flex:none;align-items:center;justify-content:space-between;padding:9px 12px;border-bottom:1px solid #ddd;background:#f8f7f3}.broker-dialog>header h2{margin:0;font-size:16px;font-weight:500}.broker-dialog>header button{width:30px;height:28px;border:1px solid #ccc;background:#fff;font-size:20px;line-height:1}.broker-form-scroll{overflow:auto;padding:3px 10px}.broker-dialog fieldset{margin:9px 2px;padding:11px 13px 14px;border:1px solid #c8c5bc}.broker-dialog legend{padding:0 5px;color:#555}.broker-fields{display:grid;grid-template-columns:1fr 1fr;gap:9px 18px}.broker-fields>label{display:grid;grid-template-columns:105px minmax(0,1fr);align-items:center;gap:8px}.broker-fields input,.broker-fields textarea{width:100%;min-height:30px;padding:5px 7px;border:1px solid #999;border-radius:0;background:#fff0ae;color:#26251f;font:inherit}.broker-fields input:read-only{background:#eee}.broker-fields textarea{resize:vertical}.broker-fields .wide{grid-column:1/-1}.broker-audit{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:8px;margin:0 2px 9px;padding:10px;border:1px solid #d6d2c7;background:#faf9f6}.broker-audit label{display:grid;gap:4px;color:#666;font-size:11px}.broker-audit input{width:100%;min-height:28px;padding:4px;border:1px solid #ccc;background:#eee;color:#555;font:inherit}.broker-dialog-actions{display:flex;flex:none;justify-content:flex-end;gap:8px;padding:8px 12px 12px;border-top:1px solid #ddd}@media(max-width:950px){.broker-state-filter{gap:6px;margin-right:2px}.broker-query{flex-wrap:wrap}.broker-fields,.broker-audit{grid-template-columns:1fr}.broker-fields .wide{grid-column:auto}.broker-fields>label{grid-template-columns:90px minmax(0,1fr)}}
</style>
