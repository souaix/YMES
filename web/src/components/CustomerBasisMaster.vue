<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

interface Customer {
  customerNo: string
  customerName: string
  customerSName: string
  customerEName: string
  telNo: string
  faxNo: string
  www: string
  taxCode: string
  address: string
  address2: string
  director: string
  description: string
  country: string
  status: string
  currency: string
  invTypeNo: string
  invAddr: string
  invAddr2: string
  invCustNo: string
  invRemark: string
  custType: string
  arType: string
  freightTerms: string
  paymentTerms: string
  acctNo: string
  deliveryTerm: string
  forwarderAssigned: string
  brokerAssigned: string
  insuranceCoverage: string
  issueState: number
  creator: string | null
  createdAt: string | null
  reviser: string | null
  revisedAt: string | null
  dataStamp: number
  erpNo: string | null
  sapFiskn: string | null
  xxName1: string | null
  xxName2: string | null
}

interface ImportRow { customerNo: string; customerName: string; customerSName: string; result: string; message: string }
type FormMode = 'new' | 'edit' | 'copy' | null

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mockMode = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes-dev-mock:ent-customer-basis:v1'
const issueStates = [{ value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' }]
const statusOptions = [{ value: '', label: '未設定' }, { value: 'O', label: '有效' }, { value: 'C', label: '失效' }, { value: 'B', label: '呆帳' }, { value: 'P', label: '暫停' }]
const invoiceTypeOptions = [{ value: '', label: '未設定' }, { value: '0', label: '內銷' }, { value: '1', label: '外銷' }]
const customerTypeOptions = [{ value: '', label: '未設定' }, { value: '0', label: '國內' }, { value: '1', label: '國外' }]
const arTypeOptions = [{ value: '', label: '未設定' }, { value: '0', label: '入庫計價' }, { value: '1', label: '出貨計價' }]

function blankCustomer(): Customer {
  return {
    customerNo: '', customerName: '', customerSName: '', customerEName: '', telNo: '', faxNo: '', www: '', taxCode: '',
    address: '', address2: '', director: '', description: '', country: '', status: '', currency: '', invTypeNo: '',
    invAddr: '', invAddr2: '', invCustNo: '', invRemark: '', custType: '', arType: '', freightTerms: '', paymentTerms: '',
    acctNo: '', deliveryTerm: '', forwarderAssigned: '', brokerAssigned: '', insuranceCoverage: '', issueState: 0,
    creator: null, createdAt: null, reviser: null, revisedAt: null, dataStamp: 0, erpNo: null, sapFiskn: null, xxName1: null, xxName2: null,
  }
}

const seedCustomers: Customer[] = [
  { ...blankCustomer(), customerNo: 'DEV-C001', customerName: '開發用客戶甲股份有限公司', customerSName: '客戶甲', customerEName: 'DEV CUSTOMER A', telNo: '02-0000-0001', faxNo: '02-0000-0002', address: '開發用地址一', country: '台灣', director: '開發負責人', status: 'O', currency: 'TWD', invTypeNo: '0', custType: '0', arType: '0', description: '僅供 Development 畫面操作展示', issueState: 0, creator: 'DEV001', createdAt: '2026-11-01T08:00:00+08:00', reviser: 'DEV001', revisedAt: '2026-11-01T08:00:00+08:00' },
  { ...blankCustomer(), customerNo: 'DEV-C002', customerName: '開發用客戶乙有限公司', customerSName: '客戶乙', customerEName: 'DEV CUSTOMER B', telNo: '03-0000-0003', country: '日本', director: '開發負責人', status: 'P', currency: 'JPY', invTypeNo: '1', custType: '1', arType: '1', issueState: 2, creator: 'DEV001', createdAt: '2026-11-02T08:00:00+08:00', reviser: 'DEV002', revisedAt: '2026-11-03T09:30:00+08:00', dataStamp: 1 },
  { ...blankCustomer(), customerNo: 'DEV-C003', customerName: '開發用客戶丙企業', customerSName: '客戶丙', status: 'C', currency: 'USD', issueState: -1, creator: 'DEV001', createdAt: '2026-11-04T08:00:00+08:00', reviser: 'DEV002', revisedAt: '2026-11-05T09:30:00+08:00', dataStamp: 1 },
]

const customers = ref<Customer[]>([])
const issueState = ref(0)
const keyword = ref('')
const selectedCustomerNos = ref<string[]>([])
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const formMode = ref<FormMode>(null)
const form = ref<Customer>(blankCustomer())
const formError = ref('')
const audit = ref({ creator: '', createdAt: '', reviser: '', revisedAt: '' })
const importOpen = ref(false)
const importFile = ref<File | null>(null)
const importFileName = ref('')
const importRows = ref<ImportRow[]>([])
const importPreviewToken = ref('')
const importBusy = ref(false)
const importError = ref('')
const importProgress = ref('')

const visibleCustomers = computed(() => customers.value.filter(row => row.issueState === issueState.value && row.customerNo.toLocaleLowerCase().includes(keyword.value.trim().toLocaleLowerCase())))
const selectedCustomers = computed(() => selectedCustomerNos.value.map(no => customers.value.find(row => row.customerNo === no)).filter((row): row is Customer => Boolean(row)))
const selectedCustomer = computed(() => selectedCustomers.value.length === 1 ? selectedCustomers.value[0] : null)
const lockBasicFields = computed(() => formMode.value === 'edit' && form.value.issueState !== 0)

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
    if (response.status === 404) throw new Error('客戶基本資料 API 尚未建立；請依 todolist_homepc.md 完成資料表與 API。')
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 回應錯誤 (${response.status})。`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}

function readMockRows(): Customer[] {
  try {
    const saved = window.localStorage.getItem(storageKey)
    return saved ? JSON.parse(saved) as Customer[] : structuredClone(seedCustomers)
  } catch { return structuredClone(seedCustomers) }
}
function saveMockRows(rows: Customer[]): void { window.localStorage.setItem(storageKey, JSON.stringify(rows)) }
function stateName(state: number): string { return issueStates.find(item => item.value === state)?.label ?? '未知' }
function valueName(options: { value: string; label: string }[], value: string): string { return value ? options.find(item => item.value === value)?.label ?? value : '' }
function formatDate(value: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.valueOf()) ? value : date.toLocaleString('zh-TW')
}

async function loadCustomers(): Promise<void> {
  loading.value = true
  errorMessage.value = ''
  try {
    if (mockMode.value) {
      customers.value = readMockRows()
      if (!window.localStorage.getItem(storageKey)) saveMockRows(customers.value)
    } else {
      customers.value = await requestJson<Customer[]>('api/ent/customer-basis')
    }
    const visible = new Set(visibleCustomers.value.map(row => row.customerNo))
    selectedCustomerNos.value = selectedCustomerNos.value.filter(no => visible.has(no))
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '客戶資料載入失敗。' }
  finally { loading.value = false }
}

function applyFilter(): void {
  const visible = new Set(visibleCustomers.value.map(row => row.customerNo))
  selectedCustomerNos.value = selectedCustomerNos.value.filter(no => visible.has(no))
}
function selectRow(row: Customer): void { selectedCustomerNos.value = [row.customerNo] }
function toggleSelection(row: Customer, checked: boolean): void {
  selectedCustomerNos.value = checked
    ? [...new Set([...selectedCustomerNos.value, row.customerNo])]
    : selectedCustomerNos.value.filter(no => no !== row.customerNo)
}
function toggleAllVisible(checked: boolean): void {
  if (checked) selectedCustomerNos.value = [...new Set([...selectedCustomerNos.value, ...visibleCustomers.value.map(row => row.customerNo)])]
  else selectedCustomerNos.value = selectedCustomerNos.value.filter(no => !visibleCustomers.value.some(row => row.customerNo === no))
}

function openForm(mode: Exclude<FormMode, null>): void {
  formError.value = ''
  successMessage.value = ''
  if (mode === 'new') form.value = blankCustomer()
  else if (!selectedCustomer.value) { errorMessage.value = mode === 'edit' ? '修改請選取單筆客戶資料。' : '複製請選取單筆客戶資料。'; return }
  else form.value = { ...selectedCustomer.value }
  if (mode === 'copy') {
    form.value.customerNo = ''
    form.value.issueState = 0
    form.value.dataStamp = 0
    form.value.creator = null
    form.value.createdAt = null
    form.value.reviser = null
    form.value.revisedAt = null
  }
  const now = new Date().toISOString()
  if (mode !== 'edit') {
    form.value.issueState = 0
    form.value.creator = props.currentUserNo || 'DEV001'
    form.value.createdAt = now
    form.value.reviser = props.currentUserNo || 'DEV001'
    form.value.revisedAt = now
  }
  audit.value = { creator: form.value.creator ?? '', createdAt: formatDate(form.value.createdAt), reviser: form.value.reviser ?? '', revisedAt: formatDate(form.value.revisedAt) }
  formMode.value = mode
}

function validateForm(closeAfterSave: boolean): string {
  if (!form.value.customerNo.trim()) return '請輸入客戶編號。'
  if (!form.value.customerName.trim()) return '請輸入客戶名稱。'
  if (closeAfterSave && !form.value.customerSName.trim()) return '請輸入客戶簡稱。'
  return ''
}

async function saveCustomer(closeAfterSave: boolean): Promise<void> {
  if (!formMode.value || busy.value) return
  formError.value = validateForm(closeAfterSave)
  if (formError.value) return
  busy.value = true
  formError.value = ''
  const mode = formMode.value
  const customerNo = form.value.customerNo.trim().toUpperCase()
  const payload: Customer = { ...form.value, customerNo }
  try {
    let saved: Customer
    if (mockMode.value) {
      const rows = readMockRows()
      const index = rows.findIndex(row => row.customerNo.toUpperCase() === customerNo)
      const now = new Date().toISOString()
      if (mode !== 'edit' && index >= 0) throw new Error('客戶編號已存在。')
      if (mode === 'edit') {
        if (index < 0) throw new Error('找不到要修改的客戶。')
        const original = rows[index]
        saved = { ...original, ...payload, issueState: original.issueState, dataStamp: original.dataStamp + 1, creator: original.creator, createdAt: original.createdAt, reviser: props.currentUserNo || 'DEV001', revisedAt: now }
        rows[index] = saved
      } else {
        saved = { ...payload, issueState: 0, dataStamp: 0, creator: props.currentUserNo || 'DEV001', createdAt: now, reviser: props.currentUserNo || 'DEV001', revisedAt: now }
        rows.push(saved)
      }
      saveMockRows(rows)
    } else {
      const path = 'api/ent/customer-basis' + (mode === 'edit' ? '/' + encodeURIComponent(customerNo) : '')
      saved = await requestJson<Customer>(path, { method: mode === 'edit' ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
    }
    audit.value = { creator: saved.creator ?? '', createdAt: formatDate(saved.createdAt), reviser: saved.reviser ?? '', revisedAt: formatDate(saved.revisedAt) }
    form.value = { ...saved }
    successMessage.value = '客戶資料已儲存。'
    await loadCustomers()
    if (closeAfterSave) {
      formMode.value = null
      selectedCustomerNos.value = customers.value.some(row => row.customerNo === customerNo && row.issueState === issueState.value) ? [customerNo] : []
    }
  } catch (error) { formError.value = error instanceof Error ? error.message : '客戶資料儲存失敗。' }
  finally { busy.value = false }
}

async function deleteSelected(): Promise<void> {
  if (!selectedCustomers.value.length || busy.value) return
  if (!window.confirm(`確定刪除選取的 ${selectedCustomers.value.length} 筆客戶資料？`)) return
  busy.value = true; errorMessage.value = ''; successMessage.value = ''
  let completed = 0
  try {
    for (const row of selectedCustomers.value) {
      if (![0, -1, 2].includes(row.issueState)) throw new Error(`${row.customerNo} 為簽核中，不能刪除。`)
      if (mockMode.value) {
        const rows = readMockRows()
        if (row.issueState === 2) {
          const index = rows.findIndex(item => item.customerNo === row.customerNo)
          if (index >= 0) rows[index] = { ...rows[index], issueState: -1, dataStamp: rows[index].dataStamp + 1, reviser: props.currentUserNo || 'DEV001', revisedAt: new Date().toISOString() }
        } else {
          const kept = rows.filter(item => item.customerNo !== row.customerNo)
          saveMockRows(kept)
        }
        if (row.issueState === 2) saveMockRows(rows)
      } else {
        const query = new URLSearchParams({ issueState: String(row.issueState), dataStamp: String(row.dataStamp) })
        await requestJson<void>('api/ent/customer-basis/' + encodeURIComponent(row.customerNo) + '?' + query, { method: 'DELETE' })
      }
      completed++
    }
    successMessage.value = `已處理 ${completed} 筆資料。已核准資料改為不使用，未簽核或不使用資料實體刪除。`
    selectedCustomerNos.value = []
    await loadCustomers()
  } catch (error) {
    errorMessage.value = `${error instanceof Error ? error.message : '刪除客戶資料失敗。'}${completed ? `（前 ${completed} 筆已完成）` : ''}`
    if (completed) await loadCustomers()
  } finally { busy.value = false }
}

async function approveSelected(): Promise<void> {
  if (!selectedCustomers.value.length || busy.value) return
  if (!window.confirm(`確定核准選取的 ${selectedCustomers.value.length} 筆客戶資料？`)) return
  busy.value = true; errorMessage.value = ''; successMessage.value = ''
  let completed = 0
  try {
    for (const row of selectedCustomers.value) {
      if (row.issueState !== 0) throw new Error(`${row.customerNo} 不是未簽核狀態。`)
      if (mockMode.value) {
        const rows = readMockRows(); const index = rows.findIndex(item => item.customerNo === row.customerNo)
        if (index < 0) throw new Error('找不到要核准的客戶。')
        rows[index] = { ...rows[index], issueState: 2, dataStamp: rows[index].dataStamp + 1, reviser: props.currentUserNo || 'DEV001', revisedAt: new Date().toISOString() }
        saveMockRows(rows)
      } else {
        const query = new URLSearchParams({ dataStamp: String(row.dataStamp) })
        await requestJson<void>('api/ent/customer-basis/' + encodeURIComponent(row.customerNo) + '/approve?' + query, { method: 'POST' })
      }
      completed++
    }
    successMessage.value = `已送出 ${completed} 筆核准；正式核准可能依簽核設定進入簽核中狀態。`
    selectedCustomerNos.value = []
    await loadCustomers()
  } catch (error) {
    errorMessage.value = `${error instanceof Error ? error.message : '核准客戶資料失敗。'}${completed ? `（前 ${completed} 筆已完成）` : ''}`
    if (completed) await loadCustomers()
  } finally { busy.value = false }
}

function openImport(): void {
  importOpen.value = true; importFile.value = null; importFileName.value = ''; importRows.value = []; importPreviewToken.value = ''; importError.value = ''; importProgress.value = ''
}
function parseCsvLine(line: string): string[] {
  const values: string[] = []; let value = ''; let quoted = false
  for (let index = 0; index < line.length; index++) {
    const char = line[index]
    if (char === '"' && quoted && line[index + 1] === '"') { value += '"'; index++ }
    else if (char === '"') quoted = !quoted
    else if (char === ',' && !quoted) { values.push(value); value = '' }
    else value += char
  }
  values.push(value)
  return values
}
async function chooseImportFile(event: Event): Promise<void> {
  const file = (event.target as HTMLInputElement).files?.[0] ?? null
  importFile.value = file; importFileName.value = file?.name ?? ''; importRows.value = []; importPreviewToken.value = ''; importError.value = ''; importProgress.value = ''
  if (!file) return
  const ext = file.name.split('.').pop()?.toLowerCase()
  if (mockMode.value && ext !== 'csv') { importError.value = 'Development 預覽目前只解析 CSV；舊版 .xls/.xlsx 由正式 API 解析。'; return }
  if (mockMode.value && ext === 'csv') {
    const lines = (await file.text()).replace(/^\uFEFF/, '').split(/\r?\n/).filter(line => line.trim())
    importRows.value = lines.slice(1).map(line => { const columns = parseCsvLine(line); return { customerNo: (columns[0] ?? '').trim(), customerName: (columns[1] ?? '').trim(), customerSName: (columns[2] ?? '').trim(), result: '待檢核', message: '' } })
    return
  }
  if (!mockMode.value) {
    importBusy.value = true
    try {
      const body = new FormData(); body.append('file', file)
      const preview = await requestJson<{ previewToken: string; rows: ImportRow[] }>('api/ent/customer-basis/import/preview', { method: 'POST', body })
      importPreviewToken.value = preview.previewToken
      importRows.value = preview.rows.map(row => ({ ...row, result: row.result || '待匯入' }))
    } catch (error) { importError.value = error instanceof Error ? error.message : 'Excel 預覽失敗。' }
    finally { importBusy.value = false }
  }
}
function downloadBlob(blob: Blob, name: string): void {
  const url = URL.createObjectURL(blob); const anchor = document.createElement('a'); anchor.href = url; anchor.download = name; anchor.click(); URL.revokeObjectURL(url)
}
async function downloadTemplate(): Promise<void> {
  importError.value = ''
  try {
    if (mockMode.value) {
      downloadBlob(new Blob(['CustomerNo,CustomerName,CustomerSName\r\nDEV-C100,開發客戶,開發簡稱\r\n'], { type: 'text/csv;charset=utf-8' }), 'CustomerTemplate_DEVELOPMENT.csv')
    } else {
      const response = await fetch(apiUrl('api/ent/customer-basis/import/template'), { credentials: 'include' })
      if (!response.ok) throw new Error(response.status === 404 ? '客戶匯入範本 API 尚未建立。' : `範本下載失敗 (${response.status})。`)
      downloadBlob(await response.blob(), 'CustomerTemplate.xlsx')
    }
  } catch (error) { importError.value = error instanceof Error ? error.message : '範本下載失敗。' }
}
async function runImport(): Promise<void> {
  if (!importFile.value || importBusy.value) { importError.value = '請先選擇 Excel 匯入檔案。'; return }
  importBusy.value = true; importError.value = ''; importProgress.value = ''
  try {
    if (mockMode.value) {
      const records = readMockRows(); let done = 0
      for (const row of importRows.value) {
        if (!row.customerNo || !row.customerName || !row.customerSName) { row.result = '失敗'; row.message = '客戶編號、客戶名稱、客戶簡稱皆為必填。'; continue }
        const customerNo = row.customerNo.toUpperCase()
        if (records.some(item => item.customerNo.toUpperCase() === customerNo)) { row.result = '失敗'; row.message = '客戶編號已存在。'; continue }
        const now = new Date().toISOString()
        records.push({ ...blankCustomer(), customerNo, customerName: row.customerName, customerSName: row.customerSName, issueState: 0, creator: props.currentUserNo || 'DEV001', createdAt: now, reviser: props.currentUserNo || 'DEV001', revisedAt: now })
        row.result = '成功'; row.message = 'Development 假資料'; done++
      }
      saveMockRows(records); importProgress.value = `完成 ${done} 筆假資料；舊系統 checker 未在瀏覽器執行。`
      await loadCustomers()
    } else {
      if (!importPreviewToken.value) throw new Error('請先成功預覽匯入檔案。')
      const result = await requestJson<{ results?: ImportRow[]; completedCount?: number }>('api/ent/customer-basis/import/commit', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ previewToken: importPreviewToken.value }) })
      if (result.results) importRows.value = result.results
      importProgress.value = `完成 ${result.completedCount ?? result.results?.filter(row => row.result === '成功').length ?? 0} 筆檢核/匯入。`
      await loadCustomers()
    }
  } catch (error) { importError.value = error instanceof Error ? error.message : '客戶資料匯入失敗。' }
  finally { importBusy.value = false }
}
function closePage(): void { window.history.pushState({}, '', appBase + '/'); window.dispatchEvent(new PopStateEvent('popstate')) }
function onCustomerNoBlur(): void { form.value.customerNo = form.value.customerNo.toUpperCase() }
function auditValue(value: string | null): string { return value ? formatDate(value) : '' }

onMounted(() => { void loadCustomers() })
watch(() => [props.developmentPreview, props.apiBaseUrl], () => { void loadCustomers() })
watch([issueState, keyword], applyFilter)
</script>

<template>
  <div class="customer-page">
    <form class="customer-query" @submit.prevent="applyFilter">
      <fieldset class="customer-state-filter"><legend>客戶</legend>
        <label v-for="item in issueStates" :key="item.value"><input v-model.number="issueState" type="radio" name="customerState" :value="item.value" />{{ item.label }}</label>
      </fieldset>
      <label class="customer-search-label" for="customer-no-search">客戶編號</label>
      <input id="customer-no-search" v-model="keyword" type="search" /><span>%</span>
      <button type="submit" :disabled="loading">查詢</button>
    </form>
    <div v-if="mockMode" class="customer-note" role="status">Development 假資料｜只儲存在此瀏覽器，不會寫入正式資料庫</div>
    <div v-if="errorMessage" class="customer-message error" role="alert">{{ errorMessage }}</div>
    <div v-if="successMessage" class="customer-message success" role="status">{{ successMessage }}</div>

    <section class="customer-panel">
      <div class="customer-list-layout">
        <div class="customer-table-scroll">
          <table class="customer-table">
            <thead><tr>
              <th class="select-column"><input type="checkbox" aria-label="選取目前清單全部資料" :checked="visibleCustomers.length > 0 && visibleCustomers.every(row => selectedCustomerNos.includes(row.customerNo))" @change="toggleAllVisible(($event.target as HTMLInputElement).checked)" /></th>
              <th>客戶編號</th><th>客戶名稱</th><th>客戶簡稱</th><th>客戶英文名稱</th><th>負責人</th><th>電話</th><th>國家</th><th>傳真</th><th>WWW</th><th>統一編號</th><th>地址</th><th>地址2</th><th>說明</th><th>建立人</th><th>建立日期</th><th>狀態</th><th>幣別</th><th>發票類型</th><th>發票地址</th><th>發票地址2</th><th>發票客戶編號</th><th>發票備註</th><th>客戶類別</th><th>應收類別</th><th>運費條件</th><th>付款條件</th><th>帳號</th><th>交貨條件</th><th>指定貨運行</th><th>指定報關行</th><th>保險</th><th>修改人</th><th>修改日期</th>
            </tr></thead>
            <tbody>
              <tr v-for="row in visibleCustomers" :key="row.customerNo" tabindex="0" :aria-selected="selectedCustomerNos.includes(row.customerNo)" :class="{ selected: selectedCustomerNos.includes(row.customerNo) }" @click="selectRow(row)" @keydown.enter.prevent="selectRow(row)">
                <td class="select-column" @click.stop><input type="checkbox" :aria-label="`選取 ${row.customerNo}`" :checked="selectedCustomerNos.includes(row.customerNo)" @change="toggleSelection(row, ($event.target as HTMLInputElement).checked)" /></td>
                <td class="customer-code">{{ row.customerNo }}</td><td>{{ row.customerName }}</td><td>{{ row.customerSName }}</td><td>{{ row.customerEName }}</td><td>{{ row.director }}</td><td>{{ row.telNo }}</td><td>{{ row.country }}</td><td>{{ row.faxNo }}</td><td>{{ row.www }}</td><td>{{ row.taxCode }}</td><td>{{ row.address }}</td><td>{{ row.address2 }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createdAt) }}</td><td>{{ valueName(statusOptions, row.status) }}</td><td>{{ row.currency }}</td><td>{{ valueName(invoiceTypeOptions, row.invTypeNo) }}</td><td>{{ row.invAddr }}</td><td>{{ row.invAddr2 }}</td><td>{{ row.invCustNo }}</td><td>{{ row.invRemark }}</td><td>{{ valueName(customerTypeOptions, row.custType) }}</td><td>{{ valueName(arTypeOptions, row.arType) }}</td><td>{{ row.freightTerms }}</td><td>{{ row.paymentTerms }}</td><td>{{ row.acctNo }}</td><td>{{ row.deliveryTerm }}</td><td>{{ row.forwarderAssigned }}</td><td>{{ row.brokerAssigned }}</td><td>{{ row.insuranceCoverage }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.revisedAt) }}</td>
              </tr>
              <tr v-if="!loading && !visibleCustomers.length"><td colspan="34" class="customer-empty">{{ customers.length ? '查無符合條件的客戶資料。' : '目前沒有客戶資料。' }}</td></tr>
              <tr v-if="loading"><td colspan="34" class="customer-empty">資料載入中…</td></tr>
            </tbody>
          </table>
        </div>
        <aside class="customer-actions" aria-label="客戶資料操作">
          <button class="primary" type="button" :disabled="busy" @click="openForm('new')">新增(A)</button>
          <button type="button" :disabled="busy || selectedCustomerNos.length !== 1" @click="openForm('edit')">修改(E)</button>
          <button type="button" :disabled="busy || selectedCustomerNos.length !== 1" @click="openForm('copy')">複製(C)</button>
          <button type="button" :disabled="busy || !selectedCustomers.length" @click="approveSelected">核准</button>
          <button type="button" :disabled="busy || !selectedCustomers.length || selectedCustomers.some(row => ![0, 2, -1].includes(row.issueState))" @click="deleteSelected">刪除(D)</button>
          <button type="button" :disabled="busy" @click="openImport">匯入</button>
          <span class="customer-state">{{ selectedCustomers.length ? `${selectedCustomers.length} 筆已選取` : stateName(issueState) }}</span>
          <button type="button" @click="closePage">關閉</button>
        </aside>
      </div>
      <footer class="customer-count">資料總筆數 <strong>{{ visibleCustomers.length }}</strong></footer>
    </section>

    <div v-if="formMode" class="customer-overlay" @click.self="formMode = null">
      <form class="customer-dialog" @submit.prevent="saveCustomer(true)">
        <header><strong>客戶基本資料 ({{ formMode === 'edit' ? 'EDIT' : formMode === 'copy' ? 'COPY' : 'ADD' }})</strong><button type="button" aria-label="關閉" @click="formMode = null">×</button></header>
        <div class="customer-form-scroll">
          <fieldset><legend>客戶主檔資料</legend><div class="customer-fields">
            <label><span>客戶編號</span><input v-model.trim="form.customerNo" required maxlength="50" :readonly="formMode === 'edit'" @blur="onCustomerNoBlur" /></label>
            <label><span>客戶名稱</span><input v-model.trim="form.customerName" required maxlength="50" :readonly="lockBasicFields" /></label>
            <label><span>客戶簡稱</span><input v-model.trim="form.customerSName" maxlength="50" :readonly="lockBasicFields" /></label>
            <label><span>客戶英文名稱</span><input v-model.trim="form.customerEName" maxlength="25" /></label>
            <label><span>電話</span><input v-model.trim="form.telNo" maxlength="25" :readonly="lockBasicFields" /></label>
            <label><span>傳真</span><input v-model.trim="form.faxNo" maxlength="25" :readonly="lockBasicFields" /></label>
            <label><span>WWW</span><input v-model.trim="form.www" maxlength="50" :readonly="lockBasicFields" /></label>
            <label><span>統一編號</span><input v-model.trim="form.taxCode" maxlength="25" :readonly="lockBasicFields" /></label>
            <label><span>負責人</span><input v-model.trim="form.director" maxlength="50" :readonly="lockBasicFields" /></label>
            <label><span>國家</span><input v-model.trim="form.country" maxlength="50" :readonly="lockBasicFields" /></label>
            <label><span>地址</span><input v-model.trim="form.address" maxlength="255" :readonly="lockBasicFields" /></label>
            <label><span>地址2</span><input v-model.trim="form.address2" maxlength="255" :readonly="lockBasicFields" /></label>
            <label class="wide"><span>說明</span><textarea v-model="form.description" maxlength="255" rows="2" :readonly="lockBasicFields" /></label>
          </div></fieldset>
          <fieldset><legend>交易與發票資料</legend><div class="customer-fields">
            <label><span>狀態</span><select v-model="form.status"><option v-for="item in statusOptions" :key="item.value" :value="item.value">{{ item.label }}</option></select></label>
            <label><span>幣別</span><input v-model.trim="form.currency" maxlength="50" /></label>
            <label><span>發票類型</span><select v-model="form.invTypeNo"><option v-for="item in invoiceTypeOptions" :key="item.value" :value="item.value">{{ item.label }}</option></select></label>
            <label><span>客戶類別</span><select v-model="form.custType"><option v-for="item in customerTypeOptions" :key="item.value" :value="item.value">{{ item.label }}</option></select></label>
            <label><span>應收類別</span><select v-model="form.arType"><option v-for="item in arTypeOptions" :key="item.value" :value="item.value">{{ item.label }}</option></select></label>
            <label><span>發票客戶編號</span><input v-model.trim="form.invCustNo" maxlength="50" /></label>
            <label><span>運費條件</span><input v-model.trim="form.freightTerms" maxlength="50" /></label>
            <label><span>付款條件</span><input v-model.trim="form.paymentTerms" maxlength="50" /></label>
            <label><span>帳號</span><input v-model.trim="form.acctNo" maxlength="50" /></label>
            <label><span>交貨條件</span><input v-model.trim="form.deliveryTerm" maxlength="50" /></label>
            <label><span>指定貨運行</span><input v-model.trim="form.forwarderAssigned" maxlength="50" /></label>
            <label><span>指定報關行</span><input v-model.trim="form.brokerAssigned" maxlength="50" /></label>
            <label><span>保險</span><input v-model.trim="form.insuranceCoverage" maxlength="50" /></label>
            <label><span>發票地址</span><input v-model.trim="form.invAddr" maxlength="255" /></label>
            <label><span>發票地址2</span><input v-model.trim="form.invAddr2" maxlength="255" /></label>
            <label class="wide"><span>發票備註</span><input v-model.trim="form.invRemark" maxlength="50" /></label>
          </div></fieldset>
          <div class="customer-audit"><label>建立人<input :value="audit.creator" readonly /></label><label>建立日期<input :value="audit.createdAt" readonly /></label><label>修改人<input :value="audit.reviser" readonly /></label><label>修改日期<input :value="audit.revisedAt" readonly /></label></div>
          <p v-if="lockBasicFields" class="locked-note">目前為 {{ stateName(form.issueState) }}；WinForms 僅允許修改交易及發票資料。</p>
          <p v-if="formError" class="customer-message error" role="alert">{{ formError }}</p>
          <p v-if="successMessage" class="customer-message success" role="status">{{ successMessage }}</p>
        </div>
        <footer class="customer-dialog-actions"><button class="primary" type="submit" :disabled="busy">確定</button><button type="button" @click="formMode = null">關閉</button><button type="button" :disabled="busy" @click="saveCustomer(false)">套用</button></footer>
      </form>
    </div>

    <div v-if="importOpen" class="customer-overlay" @click.self="importOpen = false">
      <section class="customer-dialog import-dialog">
        <header><strong>客戶資料匯入</strong><button type="button" aria-label="關閉" @click="importOpen = false">×</button></header>
        <div class="import-toolbar"><label>檔案路徑 <input :value="importFileName" readonly placeholder="請選擇 Excel 檔案" /></label><input type="file" accept=".xlsx,.xls,.csv" @change="chooseImportFile" /><button type="button" :disabled="importBusy" @click="downloadTemplate">Template Download</button></div>
        <p class="import-hint">舊系統讀取第一個工作表前三欄：CustomerNo、CustomerName、CustomerSName；三欄不得空白。</p>
        <div class="customer-table-scroll import-grid"><table class="customer-table"><thead><tr><th>客戶編號</th><th>客戶名稱</th><th>客戶簡稱</th><th>結果</th><th>訊息</th></tr></thead><tbody><tr v-for="(row,index) in importRows" :key="index"><td>{{ row.customerNo }}</td><td>{{ row.customerName }}</td><td>{{ row.customerSName }}</td><td>{{ row.result }}</td><td>{{ row.message }}</td></tr><tr v-if="!importRows.length"><td colspan="5" class="customer-empty">請選取 .xlsx/.xls 檔案；Development 可預覽 CSV。</td></tr></tbody></table></div>
        <p v-if="importProgress" class="import-hint" role="status">{{ importProgress }}</p><p v-if="importError" class="customer-message error" role="alert">{{ importError }}</p>
        <footer class="customer-dialog-actions"><button class="primary" type="button" :disabled="importBusy" @click="runImport">{{ importBusy ? '匯入中…' : 'Import' }}</button><button type="button" @click="importOpen = false">關閉</button></footer>
      </section>
    </div>
  </div>
</template>

<style scoped>
.customer-page{display:grid;gap:8px;min-width:0;padding:8px;color:#26251f;font-size:13px}
.customer-query{display:flex;min-height:48px;align-items:center;gap:8px;flex-wrap:wrap;padding:5px 8px;border:1px solid #d6d2c7;background:#fff}
.customer-state-filter{display:flex;align-items:center;gap:14px;margin:0 8px 0 0;padding:0;border:0}.customer-state-filter legend{position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0)}.customer-state-filter label{display:flex;align-items:center;gap:4px;white-space:nowrap}.customer-state-filter input,.select-column input{accent-color:#b59a00}
.customer-search-label{margin-left:6px}.customer-query>input{width:min(220px,35vw);height:29px;padding:4px 7px;border:1px solid #aaa;background:#fff}
.customer-query>button,.customer-actions button,.customer-dialog-actions button,.import-toolbar>button{min-height:30px;padding:4px 10px;border:1px solid #c4c4c4;border-radius:3px;background:linear-gradient(#fff,#f4f3ef);color:#333}.customer-query>button:disabled,.customer-actions button:disabled,.customer-dialog-actions button:disabled{color:#aaa;cursor:not-allowed}
.customer-panel{min-width:0;border:1px solid #c7c4bb;background:#fff}.customer-list-layout{display:grid;grid-template-columns:minmax(0,1fr) 98px;gap:8px;padding:7px}.customer-table-scroll{min-width:0;overflow:auto;max-height:calc(100vh - 235px)}.customer-table{width:max-content;min-width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}.customer-table th{position:sticky;top:0;z-index:1;height:30px;padding:5px 8px;border:1px solid #c7b852;background:#e7c600;text-align:left;font-weight:500}.customer-table td{height:31px;padding:5px 8px;border:1px solid #d0d0d0;max-width:280px;overflow:hidden;text-overflow:ellipsis}.customer-table tbody tr{cursor:pointer}.customer-table tbody tr:hover{background:#faf7e7}.customer-table tbody tr.selected{background:#fff0b5;box-shadow:inset 3px 0 #b59a00}.customer-table tbody tr:focus-visible{outline:2px solid #b59a00;outline-offset:-2px}.customer-code{font-family:'Segoe UI',sans-serif}.select-column{width:30px;text-align:center}.customer-empty{text-align:center!important;color:#777!important}
.customer-actions{display:flex;flex-direction:column;gap:5px}.customer-actions button{text-align:center}.customer-actions button.primary,.customer-dialog-actions button.primary{border-color:#cf9b33;background:#ffb33b;color:#fff;font-weight:600}.customer-state{padding:4px;color:#777;text-align:center;font-size:11px}.customer-count{padding:0 9px 7px;color:#777;font-size:11px}.customer-count strong{margin-left:7px;color:#555;font-weight:500}
.customer-note{padding:6px 10px;border:1px solid #d8c77b;background:#fff9dd;color:#665810;font-size:12px}.customer-message{margin:8px;padding:7px 10px;border:1px solid;font-size:12px}.customer-message.error{border-color:#d5a3a3;background:#fff5f3;color:#7a2424}.customer-message.success{border-color:#b6c9a8;background:#f5f8ef;color:#435c32}
.customer-overlay{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:18px;background:rgb(20 20 16 / 35%)}.customer-dialog{display:flex;flex-direction:column;width:min(1060px,100%);max-height:min(94vh,900px);border:1px solid #c4c1b8;background:#fff;box-shadow:0 8px 28px rgb(0 0 0 / 20%)}.customer-dialog>header{display:flex;flex:none;align-items:center;justify-content:space-between;padding:9px 12px;border-bottom:1px solid #ddd;background:#f8f7f3}.customer-dialog>header button{width:30px;height:28px;border:1px solid #ccc;background:#fff;font-size:20px;line-height:1}.customer-form-scroll{overflow:auto;padding:3px 10px}.customer-dialog fieldset{margin:9px 2px;padding:11px 13px 14px;border:1px solid #c8c5bc}.customer-dialog legend{padding:0 5px;color:#555}.customer-fields{display:grid;grid-template-columns:1fr 1fr;gap:9px 18px}.customer-fields>label{display:grid;grid-template-columns:120px minmax(0,1fr);align-items:center;gap:8px}.customer-fields input,.customer-fields select,.customer-fields textarea{width:100%;min-height:30px;padding:5px 7px;border:1px solid #999;border-radius:0;background:#fff0ae;color:#26251f;font:inherit}.customer-fields input:read-only,.customer-fields textarea:read-only{background:#eee}.customer-fields textarea{resize:vertical}.customer-fields .wide{grid-column:1/-1}.customer-fields .wide textarea{min-height:48px}.customer-audit{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:8px;margin:0 2px 9px;padding:10px;border:1px solid #d6d2c7;background:#faf9f6}.customer-audit label{display:grid;gap:4px;color:#666;font-size:11px}.customer-audit input{width:100%;min-height:28px;padding:4px;border:1px solid #ccc;background:#eee;color:#555;font:inherit}.locked-note,.import-hint{margin:6px 3px;color:#706a55;font-size:12px}.customer-dialog-actions{display:flex;flex:none;justify-content:flex-end;gap:8px;padding:8px 12px 12px;border-top:1px solid #ddd}.import-dialog{width:min(1000px,100%)}.import-toolbar{display:flex;align-items:center;gap:8px;flex-wrap:wrap;padding:12px}.import-toolbar label{display:flex;flex:1;min-width:260px;align-items:center;gap:8px}.import-toolbar label input{flex:1;min-width:0;height:30px;padding:4px;border:1px solid #aaa}.import-toolbar input[type=file]{max-width:300px}.import-grid{max-height:55vh;margin:0 12px;border:1px solid #ccc}.import-grid .customer-table{width:100%}
@media(max-width:800px){.customer-list-layout{grid-template-columns:minmax(0,1fr) 82px}.customer-actions button{padding:4px;font-size:11px}.customer-state-filter{gap:8px}.customer-dialog{max-height:96vh}.customer-fields,.customer-audit{grid-template-columns:1fr}.customer-fields .wide{grid-column:auto}.customer-fields>label{grid-template-columns:105px minmax(0,1fr)}}
</style>
