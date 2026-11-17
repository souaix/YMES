<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

interface Customer { customerNo: string; customerName: string; customerSName: string; issueState: number }
interface Contactor {
  customerNo: string; contactorName: string; telNo: string | null; faxNo: string | null; email: string | null
  title: string | null; deptName: string | null; creator: string | null; createdAt: string | null
  description: string | null; dataStamp: number; reviser: string | null; revisedAt: string | null
}
type FormMode = 'new' | 'edit' | 'copy' | null

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mockMode = computed(() => import.meta.env.DEV && props.developmentPreview)
const contactorStorageKey = 'ymes-dev-mock:ent-customer-contactors:v1'
const customerStorageKey = 'ymes-dev-mock:ent-customer-basis:v1'
const customers = ref<Customer[]>([])
const customer = ref<Customer | null>(null)
const contactors = ref<Contactor[]>([])
const selectedNames = ref<string[]>([])
const pickerOpen = ref(false)
const pickerKeyword = ref('')
const pickerLoading = ref(false)
const pickerError = ref('')
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const formMode = ref<FormMode>(null)
const form = ref<Contactor>(blankContactor())
const formError = ref('')
const audit = ref({ creator: '', createdAt: '', reviser: '', revisedAt: '' })
let loadSequence = 0

const selectedContactor = computed(() => selectedNames.value.length === 1 ? contactors.value.find(row => row.contactorName === selectedNames.value[0]) ?? null : null)
const visibleCustomers = computed(() => customers.value.filter(row => {
  const term = pickerKeyword.value.trim().toLocaleLowerCase()
  return !term || `${row.customerNo} ${row.customerName} ${row.customerSName}`.toLocaleLowerCase().includes(term)
}))

function blankContactor(): Contactor {
  return { customerNo: '', contactorName: '', telNo: '', faxNo: '', email: '', title: '', deptName: '', creator: null, createdAt: null, description: '', dataStamp: 0, reviser: null, revisedAt: null }
}

const seedCustomers: Customer[] = [
  { customerNo: 'DEV-C001', customerName: '開發用客戶甲股份有限公司', customerSName: '客戶甲', issueState: 0 },
  { customerNo: 'DEV-C002', customerName: '開發用客戶乙有限公司', customerSName: '客戶乙', issueState: 2 },
  { customerNo: 'DEV-C003', customerName: '開發用客戶丙企業', customerSName: '客戶丙', issueState: -1 },
]
const seedContactors: Contactor[] = [
  { ...blankContactor(), customerNo: 'DEV-C001', contactorName: '林怡君', telNo: '02-0000-0011', faxNo: '02-0000-0012', email: 'dev.contact.a@example.test', title: '採購', deptName: '採購部', description: 'Development 假資料', creator: 'DEV001', createdAt: '2026-11-01T08:00:00+08:00', dataStamp: 1 },
  { ...blankContactor(), customerNo: 'DEV-C001', contactorName: '陳志明', telNo: '02-0000-0021', email: 'dev.contact.b@example.test', title: '工程師', deptName: '工程部', description: '', creator: 'DEV001', createdAt: '2026-11-02T08:00:00+08:00', dataStamp: 1 },
  { ...blankContactor(), customerNo: 'DEV-C002', contactorName: '佐藤花子', telNo: '+81-3-0000-0001', email: 'dev.contact.c@example.test', title: '採購經理', deptName: '採購部', description: 'Development 假資料', creator: 'DEV001', createdAt: '2026-11-03T08:00:00+08:00', dataStamp: 1 },
]

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
    if (response.status === 404) throw new Error(path.toLowerCase().includes('/contactors')
      ? '客戶連絡人 API 尚未建立；請依 todolist_homepc.md 完成 API。'
      : '客戶基本資料查詢 API 尚未建立；請依 todolist_homepc.md 完成 API。')
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 回應錯誤 (${response.status})。`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}

function readMockRows(): Contactor[] {
  try { const saved = window.localStorage.getItem(contactorStorageKey); return saved ? JSON.parse(saved) as Contactor[] : structuredClone(seedContactors) }
  catch { return structuredClone(seedContactors) }
}
function saveMockRows(rows: Contactor[]): void { window.localStorage.setItem(contactorStorageKey, JSON.stringify(rows)) }
function readMockCustomers(): Customer[] {
  try {
    const saved = window.localStorage.getItem(customerStorageKey)
    if (!saved) return structuredClone(seedCustomers)
    return (JSON.parse(saved) as Customer[]).map(row => ({ customerNo: row.customerNo, customerName: row.customerName, customerSName: row.customerSName, issueState: row.issueState }))
  } catch { return structuredClone(seedCustomers) }
}
function formatDate(value: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.valueOf()) ? value : date.toLocaleString('zh-TW')
}
function stateName(value: number): string { return ({ 0: '未簽核', 1: '簽核中', 2: '已簽核', '-1': '不使用' } as Record<string, string>)[String(value)] ?? '未知' }
function rowsForCustomer(customerNo: string): Contactor[] {
  return readMockRows().filter(row => row.customerNo === customerNo).sort((a, b) => a.contactorName.localeCompare(b.contactorName, 'zh-Hant'))
}
function contactorsPath(customerNo: string): string { return `api/ent/customer-basis/${encodeURIComponent(customerNo)}/contactors` }

async function loadContactors(customerNo: string): Promise<void> {
  const sequence = ++loadSequence
  loading.value = true; errorMessage.value = ''; successMessage.value = ''; selectedNames.value = []; contactors.value = []
  try {
    const rows = mockMode.value ? rowsForCustomer(customerNo) : await requestJson<Contactor[]>(contactorsPath(customerNo))
    if (sequence === loadSequence) contactors.value = [...rows].sort((a, b) => a.contactorName.localeCompare(b.contactorName, 'zh-Hant'))
  } catch (error) { if (sequence === loadSequence) errorMessage.value = error instanceof Error ? error.message : '客戶連絡人載入失敗。' }
  finally { if (sequence === loadSequence) loading.value = false }
}

async function openCustomerPicker(): Promise<void> {
  pickerOpen.value = true; pickerLoading.value = true; pickerError.value = ''; pickerKeyword.value = ''
  try {
    customers.value = mockMode.value ? readMockCustomers() : await requestJson<Customer[]>('api/ent/customer-basis')
  } catch (error) { pickerError.value = error instanceof Error ? error.message : '客戶清單載入失敗。'; customers.value = [] }
  finally { pickerLoading.value = false }
}
async function chooseCustomer(row: Customer): Promise<void> {
  customer.value = row; pickerOpen.value = false
  await loadContactors(row.customerNo)
}
function selectRow(row: Contactor): void { selectedNames.value = [row.contactorName] }
function toggleSelection(row: Contactor, checked: boolean): void {
  selectedNames.value = checked ? [...new Set([...selectedNames.value, row.contactorName])] : selectedNames.value.filter(name => name !== row.contactorName)
}
function toggleAll(checked: boolean): void { selectedNames.value = checked ? contactors.value.map(row => row.contactorName) : [] }

function openForm(mode: Exclude<FormMode, null>): void {
  errorMessage.value = ''; successMessage.value = ''; formError.value = ''
  if (!customer.value) return
  const source = selectedContactor.value
  if (mode !== 'new' && !source) { errorMessage.value = mode === 'edit' ? '修改請選取單筆連絡人。' : '複製請選取單筆連絡人。'; return }
  formMode.value = mode
  form.value = mode === 'new' ? { ...blankContactor(), customerNo: customer.value.customerNo } : { ...source! }
  if (mode === 'copy') { form.value.contactorName = ''; form.value.dataStamp = 0; form.value.creator = null; form.value.createdAt = null; form.value.reviser = null; form.value.revisedAt = null }
  if (mode !== 'edit') {
    const now = new Date().toISOString()
    audit.value = { creator: props.currentUserNo || (mockMode.value ? 'DEV001' : ''), createdAt: now, reviser: '', revisedAt: '' }
  } else {
    audit.value = { creator: source!.creator || '', createdAt: source!.createdAt || '', reviser: props.currentUserNo || '', revisedAt: new Date().toISOString() }
  }
}
function closeForm(): void { formMode.value = null; formError.value = '' }
function closePage(): void { ++loadSequence; customer.value = null; contactors.value = []; selectedNames.value = []; errorMessage.value = ''; successMessage.value = '' }

async function saveForm(closeAfterSave: boolean): Promise<void> {
  if (!customer.value || !formMode.value) return
  const name = form.value.contactorName.trim()
  if (!name) { formError.value = '請輸入連絡人姓名。'; return }
  if (name.length > 50) { formError.value = '連絡人姓名不可超過 50 個字元。'; return }
  if ((form.value.telNo || '').length > 25 || (form.value.faxNo || '').length > 25 || (form.value.email || '').length > 50 || (form.value.deptName || '').length > 25 || (form.value.description || '').length > 255) {
    formError.value = '電話、傳真、Email、部門或說明超過資料庫欄位長度。'; return
  }
  if ((form.value.title || '').length > 25) { formError.value = '職稱最多可輸入 50 字，但資料庫欄位限制 25 字，請縮短後再儲存。'; return }
  form.value.contactorName = name
  busy.value = true; formError.value = ''; errorMessage.value = ''
  try {
    const mode = formMode.value
    const payload = { ...form.value, contactorName: name }
    let result: Contactor
    if (mockMode.value) {
      const rows = readMockRows()
      const duplicate = rows.some(row => row.customerNo === customer.value!.customerNo && row.contactorName === name && (mode !== 'edit' || row.contactorName !== selectedContactor.value?.contactorName))
      if (duplicate) throw new Error('此客戶已存在相同的連絡人姓名。')
      const now = new Date().toISOString()
      if (mode === 'edit') {
        const index = rows.findIndex(row => row.customerNo === customer.value!.customerNo && row.contactorName === selectedContactor.value?.contactorName)
        if (index < 0) throw new Error('資料已不存在，請重新載入。')
        result = { ...rows[index], ...payload, reviser: props.currentUserNo || 'DEV001', revisedAt: now, dataStamp: rows[index].dataStamp + 1 }
        rows[index] = result
      } else {
        result = { ...payload, customerNo: customer.value.customerNo, creator: props.currentUserNo || 'DEV001', createdAt: now, reviser: null, revisedAt: null, dataStamp: 0 }
        rows.push(result)
      }
      saveMockRows(rows)
    } else {
      const root = contactorsPath(customer.value.customerNo)
      const path = mode === 'edit' ? `${root}/${encodeURIComponent(selectedContactor.value!.contactorName)}` : root
      result = await requestJson<Contactor>(path, { method: mode === 'edit' ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
      result = result || payload
    }
    await loadContactors(customer.value.customerNo)
    selectedNames.value = [result.contactorName]
    successMessage.value = mode === 'edit' ? '連絡人資料已修改。' : '連絡人資料已新增。'
    if (closeAfterSave) closeForm()
    else if (mode === 'edit') form.value.dataStamp = result.dataStamp
  } catch (error) { formError.value = error instanceof Error ? error.message : '連絡人資料儲存失敗。' }
  finally { busy.value = false }
}

async function deleteSelected(): Promise<void> {
  if (!customer.value || selectedNames.value.length === 0) { errorMessage.value = '請先選取要刪除的連絡人資料。'; return }
  const names = [...selectedNames.value]
  if (!window.confirm(`確定刪除選取的 ${names.length} 筆連絡人資料？`)) return
  busy.value = true; errorMessage.value = ''; successMessage.value = ''
  try {
    if (mockMode.value) {
      const selected = new Set(names)
      saveMockRows(readMockRows().filter(row => row.customerNo !== customer.value!.customerNo || !selected.has(row.contactorName)))
    } else {
      for (const name of names) {
        const row = contactors.value.find(item => item.contactorName === name)
        const path = `${contactorsPath(customer.value.customerNo)}/${encodeURIComponent(name)}?dataStamp=${encodeURIComponent(String(row?.dataStamp ?? 0))}`
        await requestJson<void>(path, { method: 'DELETE' })
      }
    }
    await loadContactors(customer.value.customerNo)
    successMessage.value = `已刪除 ${names.length} 筆連絡人資料。`
  } catch (error) {
    const message = error instanceof Error ? error.message : '連絡人資料刪除失敗。'
    if (customer.value) await loadContactors(customer.value.customerNo)
    errorMessage.value = message
  }
  finally { busy.value = false }
}

onMounted(() => { if (mockMode.value && !window.localStorage.getItem(contactorStorageKey)) saveMockRows(seedContactors) })
</script>

<template>
  <div class="contactor-page">
    <div class="contactor-query">
      <label for="customer-number">選擇客戶編號：</label>
      <input id="customer-number" :value="customer?.customerNo || ''" readonly />
      <button type="button" aria-label="選擇客戶" @click="openCustomerPicker">…</button>
      <span v-if="customer" class="customer-description">{{ customer.customerName }}　{{ customer.customerSName }}</span>
    </div>
    <div v-if="mockMode" class="contactor-note" role="status">Development 假資料｜只儲存在此瀏覽器，不會寫入正式資料庫</div>
    <div v-if="errorMessage" class="contactor-message error" role="alert">{{ errorMessage }}</div>
    <div v-if="successMessage" class="contactor-message success" role="status">{{ successMessage }}</div>

    <section class="contactor-panel">
      <div class="contactor-layout">
        <div class="contactor-table-scroll">
          <table class="contactor-table">
            <thead><tr>
              <th class="select-column"><input type="checkbox" aria-label="選取全部連絡人" :checked="contactors.length > 0 && selectedNames.length === contactors.length" @change="toggleAll(($event.target as HTMLInputElement).checked)" /></th>
              <th>連絡人姓名</th><th>客戶編號</th><th>電話</th><th>傳真</th><th>Email</th><th>職稱</th><th>部門</th><th>建立人</th><th>建立日期</th><th>說明</th><th>建立人</th><th>建立日期</th>
            </tr></thead>
            <tbody>
              <tr v-for="row in contactors" :key="row.contactorName" tabindex="0" :aria-selected="selectedNames.includes(row.contactorName)" :class="{ selected: selectedNames.includes(row.contactorName) }" @click="selectRow(row)" @keydown.enter.prevent="selectRow(row)">
                <td class="select-column" @click.stop><input type="checkbox" :aria-label="`選取 ${row.contactorName}`" :checked="selectedNames.includes(row.contactorName)" @change="toggleSelection(row, ($event.target as HTMLInputElement).checked)" /></td>
                <td class="contactor-code">{{ row.contactorName }}</td><td>{{ row.customerNo }}</td><td>{{ row.telNo }}</td><td>{{ row.faxNo }}</td><td>{{ row.email }}</td><td>{{ row.title }}</td><td>{{ row.deptName }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createdAt) }}</td><td>{{ row.description }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.revisedAt) }}</td>
              </tr>
              <tr v-if="!customer"><td colspan="13" class="contactor-empty">請按「…」選擇客戶編號。</td></tr>
              <tr v-else-if="contactors.length === 0 && !loading && !errorMessage"><td colspan="13" class="contactor-empty">此客戶目前沒有連絡人資料。</td></tr>
              <tr v-else-if="loading"><td colspan="13" class="contactor-empty">資料載入中…</td></tr>
            </tbody>
          </table>
        </div>
        <div class="contactor-actions">
          <button type="button" class="primary" :disabled="!customer || busy" @click="openForm('new')">新增(A)</button>
          <button type="button" :disabled="!customer || !selectedContactor || busy" @click="openForm('edit')">修改(E)</button>
          <button type="button" :disabled="!customer || selectedNames.length === 0 || busy" @click="deleteSelected">刪除(D)</button>
          <button type="button" :disabled="!customer || !selectedContactor || busy" @click="openForm('copy')">複製(C)</button>
          <span class="contactor-action-spacer"></span>
          <button type="button" @click="closePage">關閉(X)</button>
        </div>
      </div>
      <footer class="contactor-count">資料總筆數 <strong>{{ contactors.length }}</strong><span v-if="customer">　客戶狀態：{{ stateName(customer.issueState) }}</span></footer>
    </section>

    <div v-if="pickerOpen" class="contactor-overlay" @click.self="pickerOpen = false">
      <section class="contactor-dialog picker-dialog" role="dialog" aria-modal="true" aria-labelledby="customer-picker-title">
        <header><h2 id="customer-picker-title">選擇客戶資料</h2><button type="button" aria-label="關閉" @click="pickerOpen = false">×</button></header>
        <div class="picker-toolbar"><input v-model="pickerKeyword" type="search" placeholder="客戶編號／客戶名稱" /><span>客戶狀態包含未簽核、簽核中、已簽核及不使用</span></div>
        <div v-if="pickerError" class="contactor-message error" role="alert">{{ pickerError }}</div>
        <div class="picker-table-scroll"><table class="contactor-table"><thead><tr><th>客戶編號</th><th>客戶名稱</th><th>客戶簡稱</th><th>狀態</th></tr></thead><tbody>
          <tr v-for="row in visibleCustomers" :key="row.customerNo" tabindex="0" @keydown.enter.prevent="chooseCustomer(row)" @click="chooseCustomer(row)"><td>{{ row.customerNo }}</td><td>{{ row.customerName }}</td><td>{{ row.customerSName }}</td><td>{{ stateName(row.issueState) }}</td></tr>
          <tr v-if="pickerLoading"><td colspan="4" class="contactor-empty">客戶清單載入中…</td></tr>
          <tr v-else-if="!pickerError && visibleCustomers.length === 0"><td colspan="4" class="contactor-empty">沒有符合的客戶資料。</td></tr>
        </tbody></table></div>
        <footer class="contactor-dialog-actions"><button type="button" @click="pickerOpen = false">關閉</button></footer>
      </section>
    </div>

    <div v-if="formMode" class="contactor-overlay" @click.self="closeForm">
      <form class="contactor-dialog define-dialog" @submit.prevent="saveForm(true)">
        <header><h2>{{ formMode === 'new' ? '新增' : formMode === 'edit' ? '修改' : '複製' }}客戶連絡人資料</h2><button type="button" aria-label="關閉" @click="closeForm">×</button></header>
        <div class="contactor-form-scroll">
          <fieldset><legend>客戶連絡人資料</legend><div class="contactor-fields">
            <label><span>客戶編號</span><input :value="form.customerNo" readonly maxlength="50" /></label>
            <label><span>連絡人姓名 *</span><input v-model="form.contactorName" :readonly="formMode === 'edit'" maxlength="50" required /></label>
            <label><span>電話</span><input v-model="form.telNo" maxlength="25" /></label>
            <label><span>傳真</span><input v-model="form.faxNo" maxlength="25" /></label>
            <label class="wide"><span>Email</span><input v-model="form.email" maxlength="50" /></label>
            <label><span>職稱</span><input v-model="form.title" maxlength="50" /></label>
            <label><span>部門</span><input v-model="form.deptName" maxlength="25" /></label>
            <label class="wide"><span>說明</span><textarea v-model="form.description" rows="5"></textarea></label>
          </div></fieldset>
          <div class="contactor-audit"><label>建立人<input :value="audit.creator" readonly /></label><label>建立日期<input :value="formatDate(audit.createdAt)" readonly /></label><label>修改人<input :value="audit.reviser" readonly /></label><label>修改日期<input :value="formatDate(audit.revisedAt)" readonly /></label></div>
          <p v-if="formMode === 'edit'" class="contactor-hint">連絡人姓名為主鍵，修改時不可變更。</p>
          <div v-if="formError" class="contactor-message error" role="alert">{{ formError }}</div>
        </div>
        <footer class="contactor-dialog-actions"><button type="button" :disabled="busy" @click="closeForm">關閉</button><button type="button" :disabled="busy" @click="saveForm(false)">套用</button><button type="submit" class="primary" :disabled="busy">確定</button></footer>
      </form>
    </div>
  </div>
</template>

<style scoped>
.contactor-page{display:flex;min-height:0;flex-direction:column;gap:7px;padding:8px;background:#f8f7f3;color:#333;font-size:13px}.contactor-query{display:flex;min-height:42px;align-items:center;gap:7px;padding:5px 10px;border:1px solid #d0cec7;background:#fff}.contactor-query label{width:112px;text-align:right}.contactor-query>input{width:176px;height:24px;padding:3px 6px;border:1px solid #aaa;background:#eee}.contactor-query>button{width:28px;height:25px;border:1px solid #c4c4c4;background:linear-gradient(#fff,#f4f3ef);font-size:18px;line-height:16px}.customer-description{color:#68665f}.contactor-note{padding:6px 10px;border:1px solid #d8c77b;background:#fff9dd;color:#665810;font-size:12px}.contactor-message{margin:0;padding:7px 10px;border:1px solid;font-size:12px}.contactor-message.error{border-color:#d5a3a3;background:#fff5f3;color:#7a2424}.contactor-message.success{border-color:#b6c9a8;background:#f5f8ef;color:#435c32}.contactor-panel{min-width:0;border:1px solid #c7c4bb;background:#fff}.contactor-layout{display:grid;grid-template-columns:minmax(0,1fr) 98px;gap:8px;padding:7px}.contactor-table-scroll{min-width:0;overflow:auto;max-height:calc(100vh - 235px)}.contactor-table{width:max-content;min-width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}.contactor-table th{position:sticky;top:0;z-index:1;height:30px;padding:5px 8px;border:1px solid #a9bac4;background:#56a9df;color:#fff;text-align:left;font-weight:500}.contactor-table td{height:31px;padding:5px 8px;border:1px solid #d0d0d0;max-width:260px;overflow:hidden;text-overflow:ellipsis}.contactor-table tbody tr{cursor:pointer}.contactor-table tbody tr:hover{background:#faf7e7}.contactor-table tbody tr.selected{background:#fff0b5;box-shadow:inset 3px 0 #b59a00}.contactor-table tbody tr:focus-visible{outline:2px solid #b59a00;outline-offset:-2px}.contactor-code{font-family:'Segoe UI',sans-serif}.select-column{width:30px;text-align:center}.contactor-empty{text-align:center!important;color:#777!important}.contactor-actions{display:flex;flex-direction:column;gap:5px}.contactor-actions button,.contactor-query>button,.contactor-dialog-actions button{min-height:30px;padding:5px 8px;border:1px solid #c4c4c4;border-radius:3px;background:linear-gradient(#fff,#f4f3ef);color:#333}.contactor-actions button:disabled,.contactor-dialog-actions button:disabled{color:#aaa;cursor:not-allowed}.contactor-actions button.primary,.contactor-dialog-actions button.primary{border-color:#cf9b33;background:#ffad32;color:#fff;font-weight:600}.contactor-action-spacer{flex:1}.contactor-count{padding:0 9px 7px;color:#777;font-size:11px}.contactor-count strong{margin-left:7px;color:#555;font-weight:500}.contactor-overlay{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:18px;background:rgb(20 20 16 / 35%)}.contactor-dialog{display:flex;width:min(850px,100%);max-height:min(94vh,760px);flex-direction:column;border:1px solid #c4c1b8;background:#fff;box-shadow:0 8px 28px rgb(0 0 0 / 20%)}.contactor-dialog>header{display:flex;flex:none;align-items:center;justify-content:space-between;padding:9px 12px;border-bottom:1px solid #ddd;background:#f8f7f3}.contactor-dialog>header h2{margin:0;font-size:16px;font-weight:500}.contactor-dialog>header button{width:30px;height:28px;border:1px solid #ccc;background:#fff;font-size:20px;line-height:1}.picker-toolbar{display:flex;align-items:center;gap:12px;padding:12px}.picker-toolbar input{flex:1;min-width:160px;height:30px;padding:5px 7px;border:1px solid #aaa}.picker-toolbar span{color:#777;font-size:11px}.picker-table-scroll{overflow:auto;margin:0 12px;min-height:160px}.picker-table-scroll .contactor-table{width:100%}.contactor-form-scroll{overflow:auto;padding:3px 10px}.contactor-dialog fieldset{margin:9px 2px;padding:11px 13px 14px;border:1px solid #c8c5bc}.contactor-dialog legend{padding:0 5px;color:#555}.contactor-fields{display:grid;grid-template-columns:1fr 1fr;gap:11px 18px}.contactor-fields>label{display:grid;grid-template-columns:120px minmax(0,1fr);align-items:center;gap:8px}.contactor-fields input,.contactor-fields textarea{width:100%;min-height:30px;padding:5px 7px;border:1px solid #999;border-radius:0;background:#fff0ae;color:#26251f;font:inherit}.contactor-fields input:read-only{background:#eee}.contactor-fields textarea{resize:vertical}.contactor-fields .wide{grid-column:1/-1}.contactor-audit{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:8px;margin:0 2px 9px;padding:10px;border:1px solid #d6d2c7;background:#faf9f6}.contactor-audit label{display:grid;gap:4px;color:#666;font-size:11px}.contactor-audit input{width:100%;min-height:28px;padding:4px;border:1px solid #ccc;background:#eee;color:#555;font:inherit}.contactor-hint{margin:6px 3px;color:#706a55;font-size:12px}.contactor-dialog-actions{display:flex;flex:none;justify-content:flex-end;gap:8px;padding:8px 12px 12px;border-top:1px solid #ddd}
@media(max-width:800px){.contactor-layout{grid-template-columns:minmax(0,1fr) 82px}.contactor-actions button{padding:4px;font-size:11px}.contactor-dialog{max-height:96vh}.contactor-fields,.contactor-audit{grid-template-columns:1fr}.contactor-fields .wide{grid-column:auto}.contactor-fields>label{grid-template-columns:105px minmax(0,1fr)}.picker-toolbar{align-items:stretch;flex-direction:column}}
</style>
