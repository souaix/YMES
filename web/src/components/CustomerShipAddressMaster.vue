<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

interface Customer { customerNo: string; customerName: string; customerSName: string; issueState: number }
interface ShipAddress {
  customerNo: string; shipAddr: string; telNo: string; contactorName: string; shipCustomerName: string; creator: string | null
  createdAt: string | null; description: string; flow: string; declarationFlag: string; overseaFlag: string; faxNo: string
  email: string; shipAddr2: string; country: string; companyName: string; takerNo: string; forwarderNo: string; broker: string
  dataStamp: number | null; reviser: string | null; revisedAt: string | null; clientKey?: string
}
interface ShipDeclaration {
  customerNo: string; shipTo: string; flow: string; overseaFlag: string; declarationFlag: string; bondFlag: string
  handCarryFlag: string; declarationWay: string; creator: string | null; createdAt: string | null; dataStamp: number | null
  reviser: string | null; revisedAt: string | null; clientKey?: string
}
type AddressMode = 'new' | 'edit' | 'copy' | null
type DeclarationMode = 'new' | 'edit' | null
type LookupKind = 'taker' | 'forwarder' | 'broker' | null
interface LookupOption { code: string; name: string }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mockMode = computed(() => import.meta.env.DEV && props.developmentPreview)
const customerStorageKey = 'ymes-dev-mock:ent-customer-basis:v1'
const addressStorageKey = 'ymes-dev-mock:ent-customer-ship-address:v1'
const declarationStorageKey = 'ymes-dev-mock:ent-customer-ship-declaration:v1'
const customers = ref<Customer[]>([])
const customer = ref<Customer | null>(null)
const addresses = ref<ShipAddress[]>([])
const declarations = ref<ShipDeclaration[]>([])
const selectedAddressKeys = ref<string[]>([])
const selectedDeclarationKeys = ref<string[]>([])
const pickerOpen = ref(false)
const pickerKeyword = ref('')
const pickerLoading = ref(false)
const pickerError = ref('')
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const addressMode = ref<AddressMode>(null)
const addressForm = ref<ShipAddress>(blankAddress())
const addressOriginalShipAddr = ref('')
const addressError = ref('')
const declarationMode = ref<DeclarationMode>(null)
const declarationForm = ref<ShipDeclaration>(blankDeclaration())
const declarationOriginal = ref<ShipDeclaration | null>(null)
const declarationError = ref('')
const addressAudit = ref({ creator: '', createdAt: '', reviser: '', revisedAt: '' })
const declarationAudit = ref({ creator: '', createdAt: '', reviser: '', revisedAt: '' })
const lookupKind = ref<LookupKind>(null)
const lookupOptions = ref<LookupOption[]>([])
const lookupKeyword = ref('')
const lookupError = ref('')
let loadSequence = 0

function blankAddress(): ShipAddress {
  return { customerNo: '', shipAddr: '', telNo: '', contactorName: '', shipCustomerName: '', creator: null, createdAt: null, description: '', flow: '', declarationFlag: '1', overseaFlag: '1', faxNo: '', email: '', shipAddr2: '', country: '', companyName: '', takerNo: '', forwarderNo: '', broker: '', dataStamp: 0, reviser: null, revisedAt: null }
}
function blankDeclaration(): ShipDeclaration {
  return { customerNo: '', shipTo: '', flow: '', overseaFlag: '0', declarationFlag: '0', bondFlag: '1', handCarryFlag: '0', declarationWay: '', creator: null, createdAt: null, dataStamp: 0, reviser: null, revisedAt: null }
}
const seedCustomers: Customer[] = [
  { customerNo: 'DEV-C001', customerName: '開發用客戶甲股份有限公司', customerSName: '客戶甲', issueState: 2 },
  { customerNo: 'DEV-C002', customerName: '開發用客戶乙有限公司', customerSName: '客戶乙', issueState: 2 },
  { customerNo: 'DEV-C003', customerName: '開發用客戶丙企業', customerSName: '客戶丙', issueState: 0 },
]
const seedAddresses: ShipAddress[] = [
  { ...blankAddress(), customerNo: 'DEV-C001', shipAddr: '台北市開發路 1 號', shipCustomerName: 'DEV001', flow: 'N/A', declarationFlag: '1', overseaFlag: '0', contactorName: '林怡君', telNo: '02-0000-0011', faxNo: '02-0000-0012', email: 'ship.a@example.test', shipAddr2: '一樓收貨區', country: '台灣', companyName: '開發收貨公司', takerNo: 'DEV-TAKER-01', forwarderNo: 'N/A', broker: 'N/A', description: 'Development 假資料', creator: 'DEV001', createdAt: '2026-11-01T08:00:00+08:00', dataStamp: 1, clientKey: 'DEV-SHIP-1' },
  { ...blankAddress(), customerNo: 'DEV-C002', shipAddr: 'Tokyo Dev Dock 2', shipCustomerName: 'DEV002', flow: 'N/A', declarationFlag: '1', overseaFlag: '1', contactorName: '佐藤花子', telNo: '+81-3-0000-0001', email: 'ship.b@example.test', country: '日本', companyName: 'Dev Japan Receiver', takerNo: 'N/A', forwarderNo: 'DEV-FWD-01', broker: 'DEV-BROKER-01', description: 'Development 假資料', creator: 'DEV001', createdAt: '2026-11-02T08:00:00+08:00', dataStamp: 1, clientKey: 'DEV-SHIP-2' },
]
const seedDeclarations: ShipDeclaration[] = [
  { ...blankDeclaration(), customerNo: 'DEV-C001', shipTo: 'DEV001', flow: 'N/A', overseaFlag: '0', declarationFlag: '1', bondFlag: '1', handCarryFlag: '0', declarationWay: '一般報關', creator: 'DEV001', createdAt: '2026-11-01T08:00:00+08:00', dataStamp: 1, clientKey: 'DEV-DECL-1' },
  { ...blankDeclaration(), customerNo: 'DEV-C002', shipTo: 'DEV002', flow: 'N/A', overseaFlag: '1', declarationFlag: '1', bondFlag: '0', handCarryFlag: '1', declarationWay: '快遞出口', creator: 'DEV001', createdAt: '2026-11-02T08:00:00+08:00', dataStamp: 1, clientKey: 'DEV-DECL-2' },
]

const selectedAddress = computed(() => selectedAddressKeys.value.length === 1 ? addresses.value.find(row => row.clientKey === selectedAddressKeys.value[0]) ?? null : null)
const selectedDeclaration = computed(() => selectedDeclarationKeys.value.length === 1 ? declarations.value.find(row => row.clientKey === selectedDeclarationKeys.value[0]) ?? null : null)
const approvedCustomers = computed(() => customers.value.filter(row => row.issueState === 2))
const visibleCustomers = computed(() => {
  const term = pickerKeyword.value.trim().toLocaleLowerCase()
  return approvedCustomers.value.filter(row => !term || `${row.customerNo} ${row.customerName} ${row.customerSName}`.toLocaleLowerCase().includes(term))
})
const visibleLookupOptions = computed(() => lookupOptions.value.filter(row => `${row.code} ${row.name}`.toLocaleLowerCase().includes(lookupKeyword.value.trim().toLocaleLowerCase())))

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
    if (response.status === 404) throw new Error(path.includes('/ship-')
      ? '客戶出貨地址 API 尚未建立；請依 todolist_homepc.md 完成 ET106 API。'
      : path.endsWith('/takers') || path.endsWith('/forwarders') || path.endsWith('/brokers')
        ? '取貨公司、貨運行或報關行選項 API 尚未建立。'
        : '客戶基本資料 API 尚未建立；請依 todolist_homepc.md 完成 ET101 API。')
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 回應錯誤 (${response.status})。`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function readRows<T>(key: string, fallback: T[]): T[] {
  try { const saved = window.localStorage.getItem(key); return saved ? JSON.parse(saved) as T[] : structuredClone(fallback) }
  catch { return structuredClone(fallback) }
}
function saveRows<T>(key: string, rows: T[]): void { window.localStorage.setItem(key, JSON.stringify(rows)) }
function readMockCustomers(): Customer[] {
  try {
    const saved = window.localStorage.getItem(customerStorageKey)
    if (!saved) return structuredClone(seedCustomers)
    return (JSON.parse(saved) as Customer[]).map(row => ({ customerNo: row.customerNo, customerName: row.customerName, customerSName: row.customerSName, issueState: row.issueState }))
  } catch { return structuredClone(seedCustomers) }
}
function stampAddress(row: ShipAddress, index: number): ShipAddress { return { ...row, clientKey: row.clientKey || `ADDR:${row.customerNo}:${row.shipAddr}:${index}` } }
function stampDeclaration(row: ShipDeclaration, index: number): ShipDeclaration { return { ...row, clientKey: row.clientKey || `DECL:${row.customerNo}:${row.shipTo}:${row.flow}:${index}` } }
function addressRoot(customerNo: string): string { return `api/ent/customer-basis/${encodeURIComponent(customerNo)}/ship-addresses` }
function declarationRoot(customerNo: string): string { return `api/ent/customer-basis/${encodeURIComponent(customerNo)}/ship-declarations` }
function formatDate(value: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.valueOf()) ? value : date.toLocaleString('zh-TW')
}
function flagName(value: string): string { return value === '1' ? '是' : value === '0' ? '否' : value }
function customerStateName(value: number): string { return ({ 0: '未簽核', 1: '簽核中', 2: '已簽核', '-1': '不使用' } as Record<string, string>)[String(value)] ?? '未知' }
function rowKeyQuery(row: ShipDeclaration): string {
  const query = new URLSearchParams({ shipTo: row.shipTo, flow: row.flow, overseaFlag: row.overseaFlag, declarationFlag: row.declarationFlag, bondFlag: row.bondFlag, handCarryFlag: row.handCarryFlag, dataStamp: String(row.dataStamp ?? 0) })
  return query.toString()
}

async function loadCustomerData(customerNo: string): Promise<void> {
  const sequence = ++loadSequence
  loading.value = true; errorMessage.value = ''; successMessage.value = ''; selectedAddressKeys.value = []; selectedDeclarationKeys.value = []; addresses.value = []; declarations.value = []
  try {
    let addressRows: ShipAddress[]
    let declarationRows: ShipDeclaration[]
    if (mockMode.value) {
      addressRows = readRows(addressStorageKey, seedAddresses).filter(row => row.customerNo === customerNo)
      declarationRows = readRows(declarationStorageKey, seedDeclarations).filter(row => row.customerNo === customerNo)
    } else {
      ;[addressRows, declarationRows] = await Promise.all([
        requestJson<ShipAddress[]>(addressRoot(customerNo)),
        requestJson<ShipDeclaration[]>(declarationRoot(customerNo)),
      ])
    }
    if (sequence === loadSequence) {
      addresses.value = addressRows.map(stampAddress).sort((a, b) => a.shipAddr.localeCompare(b.shipAddr, 'zh-Hant'))
      declarations.value = declarationRows.map(stampDeclaration).sort((a, b) => a.shipTo.localeCompare(b.shipTo, 'zh-Hant') || a.flow.localeCompare(b.flow))
    }
  } catch (error) { if (sequence === loadSequence) errorMessage.value = error instanceof Error ? error.message : '客戶出貨地址載入失敗。' }
  finally { if (sequence === loadSequence) loading.value = false }
}
async function openCustomerPicker(): Promise<void> {
  pickerOpen.value = true; pickerLoading.value = true; pickerError.value = ''; pickerKeyword.value = ''
  try { customers.value = mockMode.value ? readMockCustomers() : await requestJson<Customer[]>('api/ent/customer-basis') }
  catch (error) { pickerError.value = error instanceof Error ? error.message : '客戶清單載入失敗。'; customers.value = [] }
  finally { pickerLoading.value = false }
}
async function chooseCustomer(row: Customer): Promise<void> { customer.value = row; pickerOpen.value = false; await loadCustomerData(row.customerNo) }
function selectAddress(row: ShipAddress): void { selectedAddressKeys.value = [row.clientKey || '']; selectedDeclarationKeys.value = [] }
function toggleAddress(row: ShipAddress, checked: boolean): void {
  const key = row.clientKey || ''
  selectedAddressKeys.value = checked ? [...new Set([...selectedAddressKeys.value, key])] : selectedAddressKeys.value.filter(item => item !== key)
}
function toggleAllAddresses(checked: boolean): void { selectedAddressKeys.value = checked ? addresses.value.map(row => row.clientKey || '') : [] }
function selectDeclaration(row: ShipDeclaration): void { selectedDeclarationKeys.value = [row.clientKey || ''] }
function toggleDeclaration(row: ShipDeclaration, checked: boolean): void {
  const key = row.clientKey || ''
  selectedDeclarationKeys.value = checked ? [...new Set([...selectedDeclarationKeys.value, key])] : selectedDeclarationKeys.value.filter(item => item !== key)
}
function toggleAllDeclarations(checked: boolean): void { selectedDeclarationKeys.value = checked ? declarations.value.map(row => row.clientKey || '') : [] }

function openAddressForm(mode: Exclude<AddressMode, null>): void {
  if (!customer.value) return
  const source = selectedAddress.value
  if (mode !== 'new' && !source) { errorMessage.value = mode === 'edit' ? '修改請選取單筆出貨地址。' : '複製請選取單筆出貨地址。'; return }
  addressMode.value = mode; addressError.value = ''; errorMessage.value = ''; successMessage.value = ''
  addressOriginalShipAddr.value = mode === 'edit' ? source!.shipAddr : ''
  addressForm.value = mode === 'new' ? { ...blankAddress(), customerNo: customer.value.customerNo } : { ...source! }
  if (mode !== 'edit') {
    addressForm.value.dataStamp = 0; addressForm.value.creator = null; addressForm.value.createdAt = null; addressForm.value.reviser = null; addressForm.value.revisedAt = null; addressForm.value.clientKey = undefined
  }
  // Legacy defaults are conditional on EnterpriseNo=YUHSIN; DEV preview uses these example values.
  if (mode !== 'edit' && mockMode.value) { addressForm.value.flow ||= 'N/A'; addressForm.value.companyName ||= 'N/A'; addressForm.value.takerNo ||= 'N/A'; addressForm.value.forwarderNo ||= 'N/A'; addressForm.value.broker ||= 'N/A' }
  const now = new Date().toISOString()
  addressAudit.value = mode === 'edit'
    ? { creator: source!.creator || '', createdAt: source!.createdAt || '', reviser: props.currentUserNo || '', revisedAt: now }
    : { creator: props.currentUserNo || (mockMode.value ? 'DEV001' : ''), createdAt: now, reviser: '', revisedAt: '' }
}
function closeAddressForm(): void { addressMode.value = null; addressError.value = '' }
function addressPayload(): Omit<ShipAddress, 'clientKey'> {
  const { clientKey: _clientKey, ...payload } = addressForm.value
  return payload
}
async function saveAddress(closeAfterSave: boolean): Promise<void> {
  if (!customer.value || !addressMode.value) return
  if (closeAfterSave && !addressForm.value.flow.trim()) addressForm.value.flow = 'N/A'
  if (!addressForm.value.flow.trim()) { addressError.value = '請輸入流程，或以確定儲存時使用 N/A。'; return }
  if (!addressForm.value.shipCustomerName.trim()) { addressError.value = '請輸入出貨客戶名稱。'; return }
  if (!addressForm.value.shipAddr.trim()) { addressError.value = '請輸入出貨地址。'; return }
  const max: [keyof ShipAddress, number][] = [['shipAddr',255],['customerNo',50],['telNo',25],['shipCustomerName',30],['description',255],['flow',10],['declarationFlag',1],['overseaFlag',1],['faxNo',25],['email',255],['shipAddr2',500],['country',50],['companyName',150],['takerNo',50],['forwarderNo',50],['broker',50]]
  const overLength = max.find(([field, length]) => String(addressForm.value[field] ?? '').length > length)
  if (overLength) { addressError.value = `欄位 ${overLength[0]} 超過資料庫長度 ${overLength[1]}。`; return }
  busy.value = true; addressError.value = ''; errorMessage.value = ''
  try {
    const mode = addressMode.value
    const payload = addressPayload()
    let result: ShipAddress
    if (mockMode.value) {
      const rows = readRows(addressStorageKey, seedAddresses)
      const duplicate = rows.some(row => row.customerNo === customer.value!.customerNo && row.shipAddr === payload.shipAddr && (mode !== 'edit' || row.shipAddr !== addressOriginalShipAddr.value))
      if (duplicate) throw new Error('此客戶已存在相同的出貨地址。')
      const now = new Date().toISOString()
      if (mode === 'edit') {
        const index = rows.findIndex(row => row.customerNo === customer.value!.customerNo && row.shipAddr === addressOriginalShipAddr.value)
        if (index < 0) throw new Error('資料已不存在，請重新載入。')
        result = { ...rows[index], ...payload, clientKey: rows[index].clientKey, reviser: props.currentUserNo || 'DEV001', revisedAt: now, dataStamp: (rows[index].dataStamp ?? 0) + 1 }
        rows[index] = result
      } else {
        result = { ...payload, creator: props.currentUserNo || 'DEV001', createdAt: now, reviser: null, revisedAt: null, dataStamp: 0, clientKey: `DEV-${Date.now()}-${Math.random().toString(36).slice(2)}` }
        rows.push(result)
      }
      saveRows(addressStorageKey, rows)
    } else {
      const root = addressRoot(customer.value.customerNo)
      const path = mode === 'edit' ? `${root}/${encodeURIComponent(addressOriginalShipAddr.value)}` : root
      result = await requestJson<ShipAddress>(path, { method: mode === 'edit' ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
      result = result || payload
    }
    await loadCustomerData(customer.value.customerNo)
    const selected = addresses.value.find(row => row.shipAddr === result.shipAddr)
    selectedAddressKeys.value = selected?.clientKey ? [selected.clientKey] : []
    successMessage.value = mode === 'edit' ? '出貨地址已修改。' : '出貨地址已新增。'
    if (closeAfterSave) closeAddressForm()
  } catch (error) { addressError.value = error instanceof Error ? error.message : '出貨地址儲存失敗。' }
  finally { busy.value = false }
}

async function deleteAddresses(): Promise<void> {
  if (!customer.value || selectedAddressKeys.value.length === 0) { errorMessage.value = '請先選取要刪除的出貨地址。'; return }
  const rows = addresses.value.filter(row => selectedAddressKeys.value.includes(row.clientKey || ''))
  if (!window.confirm(`確定刪除選取的 ${rows.length} 筆出貨地址？`)) return
  busy.value = true; errorMessage.value = ''; successMessage.value = ''; let deleted = 0
  try {
    if (mockMode.value) {
      const keys = new Set(rows.map(row => row.clientKey))
      saveRows(addressStorageKey, readRows(addressStorageKey, seedAddresses).filter(row => !keys.has(row.clientKey)))
    } else {
      for (const row of rows) {
        const query = new URLSearchParams({ flow: row.flow, shipCustomerName: row.shipCustomerName, declarationFlag: row.declarationFlag, dataStamp: String(row.dataStamp ?? 0) })
        await requestJson<void>(`${addressRoot(customer.value.customerNo)}/${encodeURIComponent(row.shipAddr)}?${query}`, { method: 'DELETE' }); deleted++
      }
    }
    await loadCustomerData(customer.value.customerNo)
    successMessage.value = `已刪除 ${mockMode.value ? rows.length : deleted} 筆出貨地址。`
  } catch (error) { const message = error instanceof Error ? error.message : '出貨地址刪除失敗。'; await loadCustomerData(customer.value.customerNo); errorMessage.value = message }
  finally { busy.value = false }
}

function openDeclarationForm(mode: Exclude<DeclarationMode, null>): void {
  const currentCustomer = customer.value
  if (!currentCustomer) return
  const address = selectedAddress.value
  if (mode === 'new' && !address) { errorMessage.value = '新增報關資料前，請先選取單筆出貨地址。'; return }
  const source = selectedDeclaration.value
  if (mode === 'edit' && !source) { errorMessage.value = '修改請選取單筆報關資料。'; return }
  declarationMode.value = mode; declarationError.value = ''; errorMessage.value = ''; successMessage.value = ''
  declarationOriginal.value = mode === 'edit' ? { ...source! } : null
  if (mode === 'new') {
    declarationForm.value = { ...blankDeclaration(), customerNo: currentCustomer.customerNo, shipTo: address!.shipCustomerName, overseaFlag: address!.overseaFlag, declarationFlag: address!.declarationFlag }
  } else declarationForm.value = { ...source! }
  if (mode === 'new') { declarationForm.value.dataStamp = 0; declarationForm.value.creator = null; declarationForm.value.createdAt = null; declarationForm.value.reviser = null; declarationForm.value.revisedAt = null; declarationForm.value.clientKey = undefined }
  const now = new Date().toISOString()
  declarationAudit.value = mode === 'edit'
    ? { creator: source!.creator || '', createdAt: source!.createdAt || '', reviser: props.currentUserNo || '', revisedAt: now }
    : { creator: props.currentUserNo || (mockMode.value ? 'DEV001' : ''), createdAt: now, reviser: '', revisedAt: '' }
}
function closeDeclarationForm(): void { declarationMode.value = null; declarationError.value = '' }
function declarationPayload(): Omit<ShipDeclaration, 'clientKey'> { const { clientKey: _clientKey, ...payload } = declarationForm.value; return payload }
function declarationMatch(a: ShipDeclaration, b: ShipDeclaration): boolean {
  return a.customerNo === b.customerNo && a.shipTo === b.shipTo && a.flow === b.flow && a.overseaFlag === b.overseaFlag && a.declarationFlag === b.declarationFlag && a.bondFlag === b.bondFlag && a.handCarryFlag === b.handCarryFlag
}
async function saveDeclaration(closeAfterSave: boolean): Promise<void> {
  if (!customer.value || !declarationMode.value) return
  if (!declarationForm.value.shipTo) { declarationError.value = '出貨對象不可空白。'; return }
  if (!declarationForm.value.flow.trim()) { declarationError.value = '流程不可空白。'; return }
  if (!declarationForm.value.declarationWay.trim()) { declarationError.value = '報關方式不可空白。'; return }
  if (declarationForm.value.shipTo.length > 10 || declarationForm.value.flow.length > 10 || declarationForm.value.declarationWay.length > 50) { declarationError.value = '出貨對象、流程或報關方式超過資料庫欄位長度。'; return }
  busy.value = true; declarationError.value = ''; errorMessage.value = ''
  try {
    const mode = declarationMode.value
    const payload = declarationPayload()
    let result: ShipDeclaration
    if (mockMode.value) {
      const rows = readRows(declarationStorageKey, seedDeclarations)
      const now = new Date().toISOString()
      if (mode === 'edit' && declarationOriginal.value) {
        const indexes = rows.map((row, index) => declarationMatch(row, declarationOriginal.value!) ? index : -1).filter(index => index >= 0)
        if (indexes.length === 0) throw new Error('報關資料已不存在，請重新載入。')
        for (const index of indexes) rows[index] = { ...rows[index], ...payload, clientKey: rows[index].clientKey, reviser: props.currentUserNo || 'DEV001', revisedAt: now, dataStamp: (rows[index].dataStamp ?? 0) + 1 }
        result = rows[indexes[0]]
      } else {
        result = { ...payload, creator: props.currentUserNo || 'DEV001', createdAt: now, reviser: null, revisedAt: null, dataStamp: 0, clientKey: `DEV-DECL-${Date.now()}-${Math.random().toString(36).slice(2)}` }
        rows.push(result)
      }
      saveRows(declarationStorageKey, rows)
    } else if (mode === 'edit' && declarationOriginal.value) {
      result = await requestJson<ShipDeclaration>(`${declarationRoot(customer.value.customerNo)}?${rowKeyQuery(declarationOriginal.value)}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
      result = result || payload
    } else {
      result = await requestJson<ShipDeclaration>(declarationRoot(customer.value.customerNo), { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
      result = result || payload
    }
    await loadCustomerData(customer.value.customerNo)
    const selected = declarations.value.find(row => declarationMatch(row, result))
    selectedDeclarationKeys.value = selected?.clientKey ? [selected.clientKey] : []
    successMessage.value = mode === 'edit' ? '報關資料已修改。' : '報關資料已新增。'
    if (closeAfterSave) closeDeclarationForm()
  } catch (error) { declarationError.value = error instanceof Error ? error.message : '報關資料儲存失敗。' }
  finally { busy.value = false }
}
async function deleteDeclarations(): Promise<void> {
  if (!customer.value || selectedDeclarationKeys.value.length === 0) { errorMessage.value = '請先選取要刪除的報關資料。'; return }
  const rows = declarations.value.filter(row => selectedDeclarationKeys.value.includes(row.clientKey || ''))
  if (!window.confirm(`確定刪除選取的 ${rows.length} 筆報關資料？`)) return
  busy.value = true; errorMessage.value = ''; successMessage.value = ''
  try {
    if (mockMode.value) {
      saveRows(declarationStorageKey, readRows(declarationStorageKey, seedDeclarations).filter(row => !rows.some(selected => declarationMatch(row, selected))))
    } else {
      for (const row of rows) await requestJson<void>(`${declarationRoot(customer.value.customerNo)}?${rowKeyQuery(row)}`, { method: 'DELETE' })
    }
    await loadCustomerData(customer.value.customerNo)
    successMessage.value = `已刪除 ${rows.length} 筆報關資料。`
  } catch (error) { const message = error instanceof Error ? error.message : '報關資料刪除失敗。'; await loadCustomerData(customer.value.customerNo); errorMessage.value = message }
  finally { busy.value = false }
}

async function openLookup(kind: Exclude<LookupKind, null>): Promise<void> {
  lookupKind.value = kind; lookupError.value = ''; lookupKeyword.value = ''
  if (mockMode.value) {
    lookupOptions.value = kind === 'taker'
      ? [{ code: 'DEV-TAKER-01', name: '開發用取貨公司' }, { code: 'DEV-TAKER-02', name: '開發用取貨公司乙' }]
      : kind === 'forwarder' ? [{ code: 'DEV-FWD-01', name: '開發用貨運行' }]
        : [{ code: 'DEV-BROKER-01', name: '開發用報關行' }]
    return
  }
  const path = kind === 'taker' ? 'api/ent/taker-basis?issueState=2' : kind === 'forwarder' ? 'api/ent/forwarder-basis?issueState=2' : 'api/ent/broker-basis?issueState=2'
  try {
    const rows = await requestJson<Record<string, string>[]>(path)
    const keys = kind === 'taker' ? ['takerNo', 'contactorName'] : kind === 'forwarder' ? ['forwarderNo', 'contactorName'] : ['brokerNo', 'contactorName']
    lookupOptions.value = rows.map(row => ({ code: row[keys[0]] || '', name: row[keys[1]] || '' }))
  } catch (error) { lookupOptions.value = []; lookupError.value = error instanceof Error ? error.message : '選項清單載入失敗。' }
}
function chooseLookup(option: LookupOption): void {
  if (lookupKind.value === 'taker') addressForm.value.takerNo = option.code
  if (lookupKind.value === 'forwarder') addressForm.value.forwarderNo = option.code
  if (lookupKind.value === 'broker') addressForm.value.broker = option.code
  lookupKind.value = null
}
function closePage(): void { ++loadSequence; customer.value = null; addresses.value = []; declarations.value = []; selectedAddressKeys.value = []; selectedDeclarationKeys.value = []; errorMessage.value = ''; successMessage.value = '' }

onMounted(() => {
  if (mockMode.value) {
    if (!window.localStorage.getItem(addressStorageKey)) saveRows(addressStorageKey, seedAddresses)
    if (!window.localStorage.getItem(declarationStorageKey)) saveRows(declarationStorageKey, seedDeclarations)
  }
})
</script>

<template>
  <div class="ship-page">
    <div class="ship-query"><label for="ship-customer-no">選擇客戶編號：</label><input id="ship-customer-no" :value="customer?.customerNo || ''" readonly /><button type="button" aria-label="選擇已核准客戶" @click="openCustomerPicker">…</button><span v-if="customer">{{ customer.customerName }}　{{ customer.customerSName }}</span></div>
    <div v-if="mockMode" class="ship-note" role="status">Development 假資料｜只儲存在此瀏覽器，不會寫入正式資料庫</div>
    <div v-if="errorMessage" class="ship-message error" role="alert">{{ errorMessage }}</div>
    <div v-if="successMessage" class="ship-message success" role="status">{{ successMessage }}</div>

    <div class="ship-panels">
      <section class="ship-panel ship-address-panel">
        <header class="ship-panel-title">出貨地點設定</header>
        <div class="ship-panel-layout">
          <div class="ship-table-scroll">
            <table class="ship-table"><thead><tr>
              <th class="select-col"><input type="checkbox" aria-label="選取所有出貨地址" :checked="addresses.length > 0 && selectedAddressKeys.length === addresses.length" @change="toggleAllAddresses(($event.target as HTMLInputElement).checked)" /></th>
              <th>客戶編號</th><th>流程</th><th>出貨客戶名稱</th><th>國家</th><th>海外</th><th>報關</th><th>連絡人</th><th>電話</th><th>傳真</th><th>Email</th><th>出貨地址</th><th>出貨地址2</th><th>公司名稱</th><th>取貨公司</th><th>貨運行</th><th>報關行</th><th>說明</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th>
            </tr></thead><tbody>
              <tr v-for="row in addresses" :key="row.clientKey" tabindex="0" :aria-selected="selectedAddressKeys.includes(row.clientKey || '')" :class="{ selected: selectedAddressKeys.includes(row.clientKey || '') }" @click="selectAddress(row)" @keydown.enter.prevent="selectAddress(row)">
                <td class="select-col" @click.stop><input type="checkbox" :aria-label="`選取 ${row.shipAddr}`" :checked="selectedAddressKeys.includes(row.clientKey || '')" @change="toggleAddress(row, ($event.target as HTMLInputElement).checked)" /></td>
                <td>{{ row.customerNo }}</td><td>{{ row.flow }}</td><td>{{ row.shipCustomerName }}</td><td>{{ row.country }}</td><td>{{ flagName(row.overseaFlag) }}</td><td>{{ flagName(row.declarationFlag) }}</td><td>{{ row.contactorName }}</td><td>{{ row.telNo }}</td><td>{{ row.faxNo }}</td><td>{{ row.email }}</td><td>{{ row.shipAddr }}</td><td>{{ row.shipAddr2 }}</td><td>{{ row.companyName }}</td><td>{{ row.takerNo }}</td><td>{{ row.forwarderNo }}</td><td>{{ row.broker }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createdAt) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.revisedAt) }}</td>
              </tr>
              <tr v-if="!customer"><td colspan="22" class="ship-empty">請按「…」選擇已核准客戶。</td></tr>
              <tr v-else-if="loading"><td colspan="22" class="ship-empty">資料載入中…</td></tr>
              <tr v-else-if="addresses.length === 0 && !errorMessage"><td colspan="22" class="ship-empty">此客戶目前沒有出貨地址。</td></tr>
            </tbody></table>
          </div>
          <div class="ship-actions">
            <button type="button" class="primary" :disabled="!customer || busy" @click="openAddressForm('new')">新增(A)</button>
            <button type="button" :disabled="!selectedAddress || busy" @click="openAddressForm('edit')">修改(E)</button>
            <button type="button" :disabled="selectedAddressKeys.length === 0 || busy" @click="deleteAddresses">刪除(D)</button>
            <button type="button" :disabled="!selectedAddress || busy" @click="openAddressForm('copy')">複製(C)</button>
          </div>
        </div>
        <footer class="ship-count">資料總筆數 <strong>{{ addresses.length }}</strong></footer>
      </section>

      <section class="ship-panel ship-declaration-panel">
        <header class="ship-panel-title">出貨報關設定 <span v-if="selectedAddress">目前地址：{{ selectedAddress.shipCustomerName }} / {{ selectedAddress.shipAddr }}</span></header>
        <div class="ship-panel-layout">
          <div class="ship-table-scroll">
            <table class="ship-table"><thead><tr>
              <th class="select-col"><input type="checkbox" aria-label="選取所有報關資料" :checked="declarations.length > 0 && selectedDeclarationKeys.length === declarations.length" @change="toggleAllDeclarations(($event.target as HTMLInputElement).checked)" /></th>
              <th>客戶編號</th><th>出貨對象</th><th>流程</th><th>海外</th><th>報關</th><th>保稅</th><th>手提</th><th>報關方式</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th>
            </tr></thead><tbody>
              <tr v-for="row in declarations" :key="row.clientKey" tabindex="0" :aria-selected="selectedDeclarationKeys.includes(row.clientKey || '')" :class="{ selected: selectedDeclarationKeys.includes(row.clientKey || '') }" @click="selectDeclaration(row)" @keydown.enter.prevent="selectDeclaration(row)">
                <td class="select-col" @click.stop><input type="checkbox" :aria-label="`選取報關設定 ${row.shipTo} ${row.flow}`" :checked="selectedDeclarationKeys.includes(row.clientKey || '')" @change="toggleDeclaration(row, ($event.target as HTMLInputElement).checked)" /></td>
                <td>{{ row.customerNo }}</td><td>{{ row.shipTo }}</td><td>{{ row.flow }}</td><td>{{ flagName(row.overseaFlag) }}</td><td>{{ flagName(row.declarationFlag) }}</td><td>{{ flagName(row.bondFlag) }}</td><td>{{ flagName(row.handCarryFlag) }}</td><td>{{ row.declarationWay }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createdAt) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.revisedAt) }}</td>
              </tr>
              <tr v-if="!customer"><td colspan="13" class="ship-empty">選擇客戶後載入報關資料。</td></tr>
              <tr v-else-if="loading"><td colspan="13" class="ship-empty">資料載入中…</td></tr>
              <tr v-else-if="declarations.length === 0 && !errorMessage"><td colspan="13" class="ship-empty">此客戶目前沒有報關資料。</td></tr>
            </tbody></table>
          </div>
          <div class="ship-actions">
            <button type="button" class="primary" :disabled="!selectedAddress || busy" @click="openDeclarationForm('new')">新增</button>
            <button type="button" :disabled="!selectedDeclaration || busy" @click="openDeclarationForm('edit')">修改</button>
            <button type="button" :disabled="selectedDeclarationKeys.length === 0 || busy" @click="deleteDeclarations">刪除</button>
          </div>
        </div>
        <footer class="ship-count">資料總筆數 <strong>{{ declarations.length }}</strong><span>　選取出貨地址後才能新增報關資料</span></footer>
      </section>
    </div>
    <div class="ship-close-row"><button type="button" @click="closePage">關閉(X)</button></div>

    <div v-if="pickerOpen" class="ship-overlay" @click.self="pickerOpen = false">
      <section class="ship-dialog picker-dialog" role="dialog" aria-modal="true" aria-labelledby="ship-customer-picker-title">
        <header><h2 id="ship-customer-picker-title">選擇已核准客戶</h2><button type="button" aria-label="關閉" @click="pickerOpen = false">×</button></header>
        <div class="ship-picker-toolbar"><input v-model="pickerKeyword" type="search" placeholder="客戶編號／客戶名稱" /><span>此功能只列出已簽核客戶（IssueState=2）</span></div>
        <div v-if="pickerError" class="ship-message error">{{ pickerError }}</div>
        <div class="ship-modal-table"><table class="ship-table"><thead><tr><th>客戶編號</th><th>客戶名稱</th><th>客戶簡稱</th></tr></thead><tbody>
          <tr v-for="row in visibleCustomers" :key="row.customerNo" tabindex="0" @click="chooseCustomer(row)" @keydown.enter.prevent="chooseCustomer(row)"><td>{{ row.customerNo }}</td><td>{{ row.customerName }}</td><td>{{ row.customerSName }}</td></tr>
          <tr v-if="pickerLoading"><td colspan="3" class="ship-empty">客戶清單載入中…</td></tr>
          <tr v-else-if="!pickerError && visibleCustomers.length === 0"><td colspan="3" class="ship-empty">沒有符合的已核准客戶。</td></tr>
        </tbody></table></div>
        <footer class="ship-dialog-actions"><button type="button" @click="pickerOpen = false">關閉</button></footer>
      </section>
    </div>

    <div v-if="addressMode" class="ship-overlay" @click.self="closeAddressForm">
      <form class="ship-dialog define-dialog" @submit.prevent="saveAddress(true)">
        <header><h2>{{ addressMode === 'new' ? '新增' : addressMode === 'edit' ? '修改' : '複製' }}出貨地點設定</h2><button type="button" aria-label="關閉" @click="closeAddressForm">×</button></header>
        <div class="ship-form-scroll"><fieldset><legend>客戶出貨地址資料</legend><div class="ship-fields">
          <label><span>客戶編號</span><input :value="addressForm.customerNo" readonly /></label>
          <label><span>出貨客戶名稱 *</span><input v-model="addressForm.shipCustomerName" :readonly="addressMode === 'edit'" maxlength="25" required /></label>
          <label><span>流程 *</span><input v-model="addressForm.flow" :readonly="addressMode === 'edit'" required /></label>
          <label><span>出貨地址 *</span><input v-model="addressForm.shipAddr" required /></label>
          <label><span>國家</span><input v-model="addressForm.country" /></label>
          <label><span>海外</span><span class="ship-radio"><span><input v-model="addressForm.overseaFlag" type="radio" value="1" />是</span><span><input v-model="addressForm.overseaFlag" type="radio" value="0" />否</span></span></label>
          <label><span>報關</span><span class="ship-radio"><span><input v-model="addressForm.declarationFlag" type="radio" value="1" :disabled="addressMode === 'edit'" />是</span><span><input v-model="addressForm.declarationFlag" type="radio" value="0" :disabled="addressMode === 'edit'" />否</span></span></label>
          <label><span>連絡人</span><input v-model="addressForm.contactorName" maxlength="50" /></label>
          <label><span>電話</span><input v-model="addressForm.telNo" maxlength="25" /></label>
          <label><span>傳真</span><input v-model="addressForm.faxNo" /></label>
          <label><span>Email</span><input v-model="addressForm.email" /></label>
          <label><span>出貨地址2</span><input v-model="addressForm.shipAddr2" /></label>
          <label><span>公司名稱</span><input v-model="addressForm.companyName" /></label>
          <label><span>取貨公司</span><span class="ship-lookup"><input v-model="addressForm.takerNo" readonly /><button type="button" @click="openLookup('taker')">…</button></span></label>
          <label><span>貨運行</span><span class="ship-lookup"><input v-model="addressForm.forwarderNo" readonly /><button type="button" @click="openLookup('forwarder')">…</button></span></label>
          <label><span>報關行</span><span class="ship-lookup"><input v-model="addressForm.broker" readonly /><button type="button" @click="openLookup('broker')">…</button></span></label>
          <label class="wide"><span>說明</span><textarea v-model="addressForm.description" rows="3"></textarea></label>
        </div></fieldset>
        <div class="ship-audit"><label>建立人<input :value="addressAudit.creator" readonly /></label><label>建立日期<input :value="formatDate(addressAudit.createdAt)" readonly /></label><label>修改人<input :value="addressAudit.reviser" readonly /></label><label>修改日期<input :value="formatDate(addressAudit.revisedAt)" readonly /></label></div>
        <p v-if="addressMode === 'edit'" class="ship-hint">修改時客戶編號、流程、出貨客戶名稱及報關旗標唯讀；出貨地址仍可修改。</p>
        <div v-if="addressError" class="ship-message error" role="alert">{{ addressError }}</div></div>
        <footer class="ship-dialog-actions"><button type="button" :disabled="busy" @click="closeAddressForm">關閉</button><button type="button" :disabled="busy" @click="saveAddress(false)">套用</button><button type="submit" class="primary" :disabled="busy">確定</button></footer>
      </form>
    </div>

    <div v-if="declarationMode" class="ship-overlay" @click.self="closeDeclarationForm">
      <form class="ship-dialog declaration-dialog" @submit.prevent="saveDeclaration(true)">
        <header><h2>{{ declarationMode === 'new' ? '新增' : '修改' }}出貨報關設定</h2><button type="button" aria-label="關閉" @click="closeDeclarationForm">×</button></header>
        <div class="ship-form-scroll"><fieldset><legend>出貨報關資料</legend><div class="ship-fields">
          <label><span>客戶編號</span><input :value="declarationForm.customerNo" readonly /></label>
          <label><span>出貨對象 *</span><input v-model="declarationForm.shipTo" :readonly="declarationMode === 'edit'" required /></label>
          <label><span>流程 *</span><input v-model="declarationForm.flow" :readonly="declarationMode === 'edit'" required /></label>
          <label><span>報關方式 *</span><input v-model="declarationForm.declarationWay" required /></label>
          <label><span>海外</span><span class="ship-radio"><span><input v-model="declarationForm.overseaFlag" type="radio" value="1" :disabled="declarationMode === 'edit'" />是</span><span><input v-model="declarationForm.overseaFlag" type="radio" value="0" :disabled="declarationMode === 'edit'" />否</span></span></label>
          <label><span>報關</span><span class="ship-radio"><span><input v-model="declarationForm.declarationFlag" type="radio" value="1" :disabled="declarationMode === 'edit'" />是</span><span><input v-model="declarationForm.declarationFlag" type="radio" value="0" :disabled="declarationMode === 'edit'" />否</span></span></label>
          <label><span>保稅</span><span class="ship-radio"><span><input v-model="declarationForm.bondFlag" type="radio" value="1" :disabled="declarationMode === 'edit'" />是</span><span><input v-model="declarationForm.bondFlag" type="radio" value="0" :disabled="declarationMode === 'edit'" />否</span></span></label>
          <label><span>手提</span><span class="ship-radio"><span><input v-model="declarationForm.handCarryFlag" type="radio" value="1" :disabled="declarationMode === 'edit'" />是</span><span><input v-model="declarationForm.handCarryFlag" type="radio" value="0" :disabled="declarationMode === 'edit'" />否</span></span></label>
        </div></fieldset>
        <div class="ship-audit"><label>建立人<input :value="declarationAudit.creator" readonly /></label><label>建立日期<input :value="formatDate(declarationAudit.createdAt)" readonly /></label><label>修改人<input :value="declarationAudit.reviser" readonly /></label><label>修改日期<input :value="formatDate(declarationAudit.revisedAt)" readonly /></label></div>
        <p v-if="declarationMode === 'edit'" class="ship-hint">修改時出貨對象、流程與四個旗標唯讀，僅報關方式可編輯。</p>
        <div v-if="declarationError" class="ship-message error" role="alert">{{ declarationError }}</div></div>
        <footer class="ship-dialog-actions"><button type="button" :disabled="busy" @click="closeDeclarationForm">關閉</button><button type="button" :disabled="busy" @click="saveDeclaration(false)">套用</button><button type="submit" class="primary" :disabled="busy">確定</button></footer>
      </form>
    </div>

    <div v-if="lookupKind" class="ship-overlay lookup-overlay" @click.self="lookupKind = null">
      <section class="ship-dialog lookup-dialog" role="dialog" aria-modal="true" aria-labelledby="ship-lookup-title">
        <header><h2 id="ship-lookup-title">{{ lookupKind === 'taker' ? '選擇取貨公司' : lookupKind === 'forwarder' ? '選擇貨運行' : '選擇報關行' }}</h2><button type="button" aria-label="關閉" @click="lookupKind = null">×</button></header>
        <div class="ship-picker-toolbar"><input v-model="lookupKeyword" type="search" placeholder="編號／名稱" /></div>
        <div v-if="lookupError" class="ship-message error">{{ lookupError }}</div>
        <div class="ship-modal-table"><table class="ship-table"><thead><tr><th>編號</th><th>名稱</th></tr></thead><tbody>
          <tr v-for="row in visibleLookupOptions" :key="row.code" tabindex="0" @click="chooseLookup(row)" @keydown.enter.prevent="chooseLookup(row)"><td>{{ row.code }}</td><td>{{ row.name }}</td></tr>
          <tr v-if="!lookupError && visibleLookupOptions.length === 0"><td colspan="2" class="ship-empty">沒有符合的資料。</td></tr>
        </tbody></table></div>
        <footer class="ship-dialog-actions"><button type="button" @click="lookupKind = null">關閉</button></footer>
      </section>
    </div>
  </div>
</template>

<style scoped>
.ship-page{display:flex;min-height:0;flex-direction:column;gap:7px;padding:8px;background:#f8f7f3;color:#333;font-size:13px}.ship-query{display:flex;min-height:40px;align-items:center;gap:7px;padding:5px 10px;border:1px solid #d0cec7;background:#fff}.ship-query label{width:112px;text-align:right}.ship-query>input{width:184px;height:24px;padding:3px 6px;border:1px solid #aaa;background:#eee}.ship-query>button{width:28px;height:25px;border:1px solid #c4c4c4;background:linear-gradient(#fff,#f4f3ef);font-size:18px}.ship-query>span{color:#68665f}.ship-note{padding:6px 10px;border:1px solid #d8c77b;background:#fff9dd;color:#665810;font-size:12px}.ship-message{margin:0;padding:7px 10px;border:1px solid;font-size:12px}.ship-message.error{border-color:#d5a3a3;background:#fff5f3;color:#7a2424}.ship-message.success{border-color:#b6c9a8;background:#f5f8ef;color:#435c32}.ship-panels{display:grid;grid-template-rows:minmax(275px,1.55fr) minmax(190px,1fr);min-height:calc(100vh - 240px);gap:7px}.ship-panel{display:flex;min-height:0;flex-direction:column;border:1px solid #c7c4bb;background:#fff}.ship-panel-title{flex:none;padding:5px 10px;border-bottom:1px solid #ddd;color:#555;font-weight:500}.ship-panel-title span{margin-left:10px;color:#777;font-size:11px;font-weight:400}.ship-panel-layout{display:grid;min-height:0;flex:1;grid-template-columns:minmax(0,1fr) 84px;gap:7px;padding:6px}.ship-table-scroll{min-width:0;overflow:auto}.ship-table{width:max-content;min-width:100%;border-collapse:collapse;white-space:nowrap;font-size:11px}.ship-table th{position:sticky;top:0;z-index:1;height:28px;padding:4px 7px;border:1px solid #a9bac4;background:#56a9df;color:#fff;text-align:left;font-weight:500}.ship-table td{height:29px;padding:4px 7px;border:1px solid #d0d0d0;max-width:230px;overflow:hidden;text-overflow:ellipsis}.ship-table tbody tr{cursor:pointer}.ship-table tbody tr:hover{background:#faf7e7}.ship-table tbody tr.selected{background:#fff0b5;box-shadow:inset 3px 0 #b59a00}.ship-table tbody tr:focus-visible{outline:2px solid #b59a00;outline-offset:-2px}.select-col{width:27px;text-align:center}.ship-empty{text-align:center!important;color:#777!important}.ship-actions{display:flex;flex-direction:column;gap:5px}.ship-actions button,.ship-query>button,.ship-close-row button,.ship-dialog-actions button{min-height:29px;padding:4px 7px;border:1px solid #c4c4c4;border-radius:3px;background:linear-gradient(#fff,#f4f3ef);color:#333}.ship-actions button:disabled,.ship-dialog-actions button:disabled{color:#aaa;cursor:not-allowed}.ship-actions button.primary,.ship-dialog-actions button.primary{border-color:#cf9b33;background:#ffad32;color:#fff;font-weight:600}.ship-count{flex:none;padding:0 9px 6px;color:#777;font-size:11px}.ship-count strong{margin-left:7px;color:#555;font-weight:500}.ship-count span{color:#777}.ship-close-row{display:flex;justify-content:flex-end}.ship-close-row button{min-width:80px}.ship-overlay{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:18px;background:rgb(20 20 16 / 35%)}.ship-dialog{display:flex;width:min(1060px,100%);max-height:min(94vh,900px);flex-direction:column;border:1px solid #c4c1b8;background:#fff;box-shadow:0 8px 28px rgb(0 0 0 / 20%)}.ship-dialog>header{display:flex;flex:none;align-items:center;justify-content:space-between;padding:9px 12px;border-bottom:1px solid #ddd;background:#f8f7f3}.ship-dialog>header h2{margin:0;font-size:16px;font-weight:500}.ship-dialog>header button{width:30px;height:28px;border:1px solid #ccc;background:#fff;font-size:20px;line-height:1}.ship-picker-toolbar{display:flex;align-items:center;gap:12px;padding:12px}.ship-picker-toolbar input{flex:1;min-width:160px;height:30px;padding:5px 7px;border:1px solid #aaa}.ship-picker-toolbar span{color:#777;font-size:11px}.ship-modal-table{overflow:auto;min-height:150px;margin:0 12px}.ship-modal-table .ship-table{width:100%}.ship-form-scroll{overflow:auto;padding:3px 10px}.ship-dialog fieldset{margin:9px 2px;padding:11px 13px 14px;border:1px solid #c8c5bc}.ship-dialog legend{padding:0 5px;color:#555}.ship-fields{display:grid;grid-template-columns:1fr 1fr;gap:9px 18px}.ship-fields>label{display:grid;grid-template-columns:135px minmax(0,1fr);align-items:center;gap:8px}.ship-fields input,.ship-fields textarea{width:100%;min-height:30px;padding:5px 7px;border:1px solid #999;border-radius:0;background:#fff0ae;color:#26251f;font:inherit}.ship-fields input:read-only{background:#eee}.ship-fields input:disabled{background:#eee;color:#666}.ship-fields textarea{resize:vertical}.ship-fields .wide{grid-column:1/-1}.ship-radio{display:flex;min-height:30px;align-items:center;gap:18px;padding:4px 8px;border:1px solid #aaa;background:#fff}.ship-radio label{display:inline-flex;align-items:center;gap:4px}.ship-radio input{width:auto;min-height:0;margin:0;accent-color:#b59a00}.ship-lookup{display:flex;gap:4px}.ship-lookup input{flex:1;min-width:0}.ship-lookup button{width:30px;border:1px solid #c4c4c4;background:#f4f3ef}.ship-audit{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:8px;margin:0 2px 9px;padding:10px;border:1px solid #d6d2c7;background:#faf9f6}.ship-audit label{display:grid;gap:4px;color:#666;font-size:11px}.ship-audit input{width:100%;min-height:28px;padding:4px;border:1px solid #ccc;background:#eee;color:#555;font:inherit}.ship-hint{margin:6px 3px;color:#706a55;font-size:12px}.ship-dialog-actions{display:flex;flex:none;justify-content:flex-end;gap:8px;padding:8px 12px 12px;border-top:1px solid #ddd}.picker-dialog,.lookup-dialog{width:min(820px,100%)}.lookup-overlay{z-index:1100}
.ship-radio>span{display:inline-flex;align-items:center;gap:4px}
@media(max-width:900px){.ship-panels{grid-template-rows:minmax(260px,1fr) minmax(210px,.8fr);min-height:calc(100vh - 255px)}.ship-panel-layout{grid-template-columns:minmax(0,1fr) 72px}.ship-actions button{padding:3px;font-size:11px}.ship-fields,.ship-audit{grid-template-columns:1fr}.ship-fields .wide{grid-column:auto}.ship-fields>label{grid-template-columns:112px minmax(0,1fr)}.ship-query>span{display:none}}
</style>
