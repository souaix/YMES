<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'
import { apiErrorMessage } from '../utils/apiError'
import { addressKey, addressWrite, addressLimits, blankAddress, codePoints, declarationKey, groupKey,
  readAddress, readAddresses, readCustomers, readDeclarations, readDeletedDeclaration, readReferences,
  readSavedDeclaration, sameGroup, type Address, type AddressWrite, type CustomerOption, type Declaration,
  type DeclarationGroup, type DeclarationKey, type ReferenceKind, type ReferenceOption } from '../utils/et106'

type Mode = 'new' | 'edit' | 'copy'
type Editor = 'address' | 'declaration' | null
type DeclarationDraft = DeclarationKey & { declarationWay: string }
type Picker = 'customer' | ReferenceKind | null
const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
// Every environment uses the same real API; this page has no DEV storage path.
const addressRoot = 'api/ent/customer-ship-addresses', declarationRoot = 'api/ent/customer-ship-declarations'
const ownerInput = ref(''), owner = ref<string | null>(null), approvedCustomer = ref<CustomerOption | null>(null)
const addresses = ref<Address[]>([]), declarations = ref<Declaration[]>([]), groups = ref<DeclarationGroup[]>([])
const addressesReady = ref(false), declarationsReady = ref(false), addressLoadError = ref(''), declarationLoadError = ref('')
const selectedAddresses = ref<string[]>([]), selectedGroups = ref<string[]>([])
const loading = ref(false), busy = ref(false), saving = ref(false), error = ref(''), notice = ref('')
const editor = ref<Editor>(null), mode = ref<Mode>('new'), formOwner = ref(''), formReady = ref(false), formError = ref(''), formNotice = ref('')
const addressSource = ref<Address | null>(null), addressDraft = ref<AddressWrite>(blankAddress())
const declarationSource = ref<DeclarationGroup | null>(null), declarationSourceRows = ref<Declaration[]>([]), contextAddress = ref<Address | null>(null)
const declarationDraft = ref<DeclarationDraft>({ shipTo: '', flow: '', overseaFlag: '0', declarationFlag: '0', bondFlag: '1', handCarryFlag: '0', declarationWay: '' })
const baseline = ref(''), picker = ref<Picker>(null), pickerSearch = ref(''), pickerLoading = ref(false), pickerError = ref('')
const customerOptions = ref<CustomerOption[]>([]), referenceOptions = ref<ReferenceOption[]>([])
const pageElement = ref<HTMLElement | null>(null), addressElement = ref<HTMLElement | null>(null), declarationElement = ref<HTMLElement | null>(null), pickerElement = ref<HTMLElement | null>(null)
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()
let disposed = false, listEpoch = 0, formEpoch = 0, pickerEpoch = 0, operationEpoch = 0
let previousFormFocus: HTMLElement | null = null, previousPickerFocus: HTMLElement | null = null
const writing = computed(() => busy.value || saving.value)
const blocked = computed(() => loading.value || writing.value || editor.value !== null || picker.value !== null || confirmationMessage.value !== null)
const unappliedOwner = computed(() => owner.value === null || ownerInput.value !== owner.value)
const commandsBlocked = computed(() => blocked.value || unappliedOwner.value)
const approvedOwner = computed(() => owner.value !== null && approvedCustomer.value?.customerNo === owner.value && approvedCustomer.value.issueState === 2)
const addressTargets = computed(() => addresses.value.filter(row => selectedAddresses.value.includes(addressKey(row))))
const singleAddress = computed(() => addressTargets.value.length === 1 && selectedAddresses.value.length === 1 ? addressTargets.value[0]! : null)
const groupTargets = computed(() => owner.value === null ? [] : groups.value.filter(group => selectedGroups.value.includes(groupKey(owner.value!, group.key))))
const singleGroup = computed(() => groupTargets.value.length === 1 && selectedGroups.value.length === 1 ? groupTargets.value[0]! : null)
const allAddresses = computed(() => addresses.value.length > 0 && addressTargets.value.length === addresses.value.length)
const allGroups = computed(() => groups.value.length > 0 && groupTargets.value.length === groups.value.length)
const selectedDeclarationRows = computed(() => groupTargets.value.reduce((sum, group) => sum + group.rowCount, 0))
const dirty = computed(() => JSON.stringify(editor.value === 'address' ? addressDraft.value : declarationDraft.value) !== baseline.value)
const formBlocked = computed(() => writing.value || picker.value !== null || confirmationMessage.value !== null || !formReady.value)
const visibleCustomers = computed(() => customerOptions.value.filter(row => searchMatch([row.customerNo, row.customerName, row.customerSName])))
const visibleReferences = computed(() => referenceOptions.value.filter(row => searchMatch([row.code, row.name])))
const pickerTitle = computed(() => picker.value === 'customer' ? '選擇已簽核客戶' : picker.value === 'taker' ? '選擇取貨公司' : picker.value === 'forwarder' ? '選擇貨運行' : '選擇報關行')
const addressColumns: ReadonlyArray<readonly [keyof Address, string]> = [
  ['customerNo', '客戶編號'], ['flow', '流程'], ['shipCustomerName', '出貨客戶名稱'], ['country', '國家'], ['overseaFlag', '海外'], ['declarationFlag', '報關'],
  ['contactorName', '連絡人'], ['telNo', '電話'], ['faxNo', '傳真'], ['email', 'Email'], ['shipAddr', '出貨地址'], ['shipAddr2', '出貨地址2'],
  ['companyName', '公司名稱'], ['takerNo', '取貨公司'], ['forwarderNo', '貨運行'], ['broker', '報關行'], ['description', '說明'],
  ['creator', '建立人'], ['createDate', '建立日期'], ['reviser', '修改人'], ['reviseDate', '修改日期']]
const declarationColumns: ReadonlyArray<readonly [keyof Declaration, string]> = [
  ['customerNo', '客戶編號'], ['shipTo', '出貨對象'], ['flow', '流程'], ['overseaFlag', '海外'], ['declarationFlag', '報關'], ['bondFlag', '保稅'], ['handCarryFlag', '手提'],
  ['declarationWay', '報關方式'], ['creator', '建立人'], ['createDate', '建立日期'], ['dataStamp', 'DataStamp'], ['reviser', '修改人'], ['reviseDate', '修改日期']]
const textFields: ReadonlyArray<readonly [keyof Pick<AddressWrite, 'country' | 'contactorName' | 'telNo' | 'faxNo' | 'email' | 'shipAddr2' | 'companyName'>, string]> = [
  ['country', '國家'], ['contactorName', '連絡人'], ['telNo', '電話'], ['faxNo', '傳真'], ['email', 'Email'], ['shipAddr2', '出貨地址2'], ['companyName', '公司名稱']]
const refs: ReadonlyArray<readonly [ReferenceKind, 'takerNo' | 'forwarderNo' | 'broker', string]> = [['taker', 'takerNo', '取貨公司'], ['forwarder', 'forwarderNo', '貨運行'], ['broker', 'broker', '報關行']]
const flagFields: ReadonlyArray<readonly ['overseaFlag' | 'declarationFlag' | 'bondFlag' | 'handCarryFlag', string]> = [['overseaFlag', '海外'], ['declarationFlag', '報關'], ['bondFlag', '保稅'], ['handCarryFlag', '手提']]
const auditFields = [['creator', '建立人'], ['createDate', '建立日期'], ['reviser', '修改人'], ['reviseDate', '修改日期']] as const

function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function fail(cause: unknown): string { return cause instanceof Error ? cause.message : '操作失敗，請重新查詢。' }
function searchMatch(parts: (string | null)[]): boolean { return parts.join(' ').toLocaleLowerCase().includes(pickerSearch.value.toLocaleLowerCase()) }
function path(root: string, rawOwner: string, resource = '', rawAddress?: string): string {
  const query = new URLSearchParams({ customerNo: rawOwner })
  if (rawAddress !== undefined) query.set('shipAddr', rawAddress)
  return root + (resource ? '/' + resource : '') + '?' + query.toString()
}
class ApiFailure extends Error { constructor(readonly status: number, message: string) { super(message) } }
async function request(resource: string, init: RequestInit = {}): Promise<Response> {
  if (!props.apiBaseUrl.trim()) throw new Error('目前環境尚未設定 API 位址。')
  const base = new URL(props.apiBaseUrl.replace(/\/+$/, '') + '/', window.location.origin)
  const headers = new Headers(init.headers); if (init.body) headers.set('Content-Type', 'application/json')
  const response = await fetch(new URL(resource.replace(/^\/+/, ''), base), { ...init, headers, credentials: 'include' })
  if (response.status === 401) { window.location.assign(import.meta.env.BASE_URL); throw new ApiFailure(401, '登入狀態已逾期，請重新登入。') }
  if (!response.ok) throw new ApiFailure(response.status, await apiErrorMessage(response))
  return response
}
async function json(response: Response): Promise<unknown> {
  if (response.status !== 200) throw new Error('伺服器未回傳完整的操作結果。')
  return response.json() as Promise<unknown>
}
function display(value: string | number | null, field: string): string {
  if (value === null) return ''
  if (field === 'createDate' || field === 'reviseDate') {
    const date = new Date(String(value)); return Number.isNaN(date.valueOf()) ? String(value) : date.toLocaleString('zh-TW', { hour12: false })
  }
  return String(value)
}
function prune(): void {
  const addressSet = new Set(addresses.value.map(addressKey)), groupSet = new Set(groups.value.map(group => groupKey(owner.value!, group.key)))
  selectedAddresses.value = selectedAddresses.value.filter(key => addressSet.has(key)); selectedGroups.value = selectedGroups.value.filter(key => groupSet.has(key))
}
async function refreshLists(rawOwner: string, preserve: boolean): Promise<void> {
  const epoch = ++listEpoch
  if (!preserve) { addresses.value = []; declarations.value = []; groups.value = []; selectedAddresses.value = []; selectedGroups.value = [] }
  addressesReady.value = false; declarationsReady.value = false; addressLoadError.value = ''; declarationLoadError.value = ''
  const results = await Promise.allSettled([
    request(path(addressRoot, rawOwner)).then(json).then(value => readAddresses(value, rawOwner)),
    request(path(declarationRoot, rawOwner)).then(json).then(value => readDeclarations(value, rawOwner))])
  if (disposed || epoch !== listEpoch || owner.value !== rawOwner) return
  const [addressResult, declarationResult] = results
  if (addressResult.status === 'fulfilled') { addresses.value = addressResult.value; addressesReady.value = true } else addressLoadError.value = fail(addressResult.reason)
  if (declarationResult.status === 'fulfilled') { declarations.value = declarationResult.value.items; groups.value = declarationResult.value.groups; declarationsReady.value = true } else declarationLoadError.value = fail(declarationResult.reason)
  prune()
}
async function queryOwner(): Promise<void> {
  if (blocked.value || disposed) return
  if (codePoints(ownerInput.value) > 50 || ownerInput.value.includes('\0')) { error.value = '客戶編號最多50字元，且不能包含無效字元。'; return }
  owner.value = ownerInput.value
  if (approvedCustomer.value?.customerNo !== owner.value) approvedCustomer.value = null
  loading.value = true; error.value = ''; notice.value = ''
  try { await refreshLists(owner.value, false) } finally { loading.value = false }
}
function selectAddress(row: Address, checked?: boolean): void {
  if (commandsBlocked.value || !addressesReady.value) return
  const key = addressKey(row)
  selectedAddresses.value = checked === undefined ? [key] : checked ? [...new Set([...selectedAddresses.value, key])] : selectedAddresses.value.filter(value => value !== key)
}
function selectAllAddresses(checked: boolean): void { if (!commandsBlocked.value && addressesReady.value) selectedAddresses.value = checked ? addresses.value.map(addressKey) : [] }
function selectGroup(row: Declaration, checked?: boolean): void {
  if (commandsBlocked.value || !declarationsReady.value || owner.value === null) return
  const key = groupKey(owner.value, row)
  selectedGroups.value = checked === undefined ? [key] : checked ? [...new Set([...selectedGroups.value, key])] : selectedGroups.value.filter(value => value !== key)
}
function selectAllGroups(checked: boolean): void { if (!commandsBlocked.value && declarationsReady.value && owner.value !== null) selectedGroups.value = checked ? groups.value.map(group => groupKey(owner.value!, group.key)) : [] }
async function readAddressItem(row: Address): Promise<Address> {
  const saved = readAddress(await json(await request(path(addressRoot, row.customerNo, 'item', row.shipAddr))))
  if (addressKey(saved) !== addressKey(row)) throw new Error('出貨地址的原始主鍵回應不一致。')
  return saved
}
function setBaseline(): void { baseline.value = JSON.stringify(editor.value === 'address' ? addressDraft.value : declarationDraft.value) }
async function openAddress(nextMode: Mode): Promise<void> {
  if (commandsBlocked.value || !addressesReady.value || owner.value === null || nextMode !== 'edit' && !approvedOwner.value || nextMode !== 'new' && !singleAddress.value) return
  const epoch = ++formEpoch, rawOwner = owner.value, selected = singleAddress.value ? clone(singleAddress.value) : null
  previousFormFocus = activeElement(); busy.value = true; error.value = ''; notice.value = ''
  try {
    const source = nextMode === 'new' ? null : await readAddressItem(selected!)
    if (disposed || epoch !== formEpoch || owner.value !== rawOwner) return
    formOwner.value = rawOwner; addressSource.value = source; addressDraft.value = source ? addressWrite(source) : blankAddress()
    editor.value = 'address'; mode.value = nextMode; formReady.value = true; formError.value = ''; formNotice.value = ''; setBaseline()
  } catch (cause) { if (!disposed) error.value = fail(cause) }
  finally { busy.value = false; await restoreFocus() }
}
async function openDeclaration(nextMode: 'new' | 'edit'): Promise<void> {
  if (commandsBlocked.value || !declarationsReady.value || owner.value === null || nextMode === 'new' && (!approvedOwner.value || !addressesReady.value || !singleAddress.value) || nextMode === 'edit' && !singleGroup.value) return
  const epoch = ++formEpoch, rawOwner = owner.value, selectedAddress = singleAddress.value ? clone(singleAddress.value) : null, selected = singleGroup.value ? clone(singleGroup.value) : null
  previousFormFocus = activeElement(); busy.value = true; error.value = ''; notice.value = ''
  try {
    let source: Address | null = null, group: DeclarationGroup | null = null, items: Declaration[] = []
    if (nextMode === 'new') source = await readAddressItem(selectedAddress!)
    else {
      const list = readDeclarations(await json(await request(path(declarationRoot, rawOwner))), rawOwner)
      group = list.groups.find(value => groupKey(rawOwner, value.key) === groupKey(rawOwner, selected!.key)) ?? null
      if (!group) throw new Error('原始報關群組已不存在，請重新查詢。')
      items = list.items.filter(row => sameGroup(rawOwner, row, group!.key))
      if (!disposed && epoch === formEpoch) { declarations.value = list.items; groups.value = list.groups; prune() }
    }
    if (disposed || epoch !== formEpoch || owner.value !== rawOwner) return
    formOwner.value = rawOwner; contextAddress.value = source; declarationSource.value = group; declarationSourceRows.value = items
    const commonWay = items.length && items.every(row => row.declarationWay === items[0]!.declarationWay) ? items[0]!.declarationWay : ''
    declarationDraft.value = group ? { ...declarationKey(group.key), declarationWay: commonWay } : { shipTo: source!.shipCustomerName ?? '', flow: '', overseaFlag: source!.overseaFlag ?? '', declarationFlag: source!.declarationFlag, bondFlag: '1', handCarryFlag: '0', declarationWay: '' }
    editor.value = 'declaration'; mode.value = nextMode; formReady.value = true; formNotice.value = ''; formError.value = source && codePoints(source.shipCustomerName ?? '') > 10 ? '出貨客戶名稱超過出貨對象10字元上限，請自行輸入正確出貨對象；未截斷原值。' : ''; setBaseline()
  } catch (cause) { if (!disposed) error.value = fail(cause) }
  finally { busy.value = false; await restoreFocus() }
}
function validateAddress(close: boolean): string {
  const value = addressDraft.value
  if (mode.value !== 'edit' && close && /^ *$/.test(value.flow)) value.flow = 'N/A'
  if (mode.value !== 'edit') for (const [field, label] of [['shipAddr', '出貨地址'], ['shipCustomerName', '出貨客戶名稱'], ['flow', '流程'], ['declarationFlag', '報關']] as const)
    if (value[field] === null || /^ *$/.test(value[field]!)) return '請輸入' + label + '。'
  if (mode.value === 'edit' && addressSource.value && value.shipAddr !== addressSource.value.shipAddr && /^ *$/.test(value.shipAddr)) return '新的出貨地址不可空白。'
  for (const [field, limit, label] of addressLimits) if (value[field] !== null && (codePoints(value[field]!) > limit || value[field]!.includes('\0'))) return label + '最多' + limit + '字元，且不能包含無效字元。'
  return ''
}
function validateDeclaration(): string {
  const value = declarationDraft.value
  const fields = mode.value === 'edit' ? [['declarationWay', 50, '報關方式']] as const : [['shipTo', 10, '出貨對象'], ['flow', 10, '流程'], ['overseaFlag', 1, '海外'], ['declarationFlag', 1, '報關'], ['bondFlag', 1, '保稅'], ['handCarryFlag', 1, '手提'], ['declarationWay', 50, '報關方式']] as const
  for (const [field, limit, label] of fields) if (/^ *$/.test(value[field]) || codePoints(value[field]) > limit || value[field].includes('\0')) return label + '必填，且最多' + limit + '字元。'
  return ''
}
function adoptAddress(saved: Address, old: Address | null, editing: boolean): void {
  if (saved.customerNo !== formOwner.value || saved.shipAddr !== addressDraft.value.shipAddr || editing && old && (saved.flow !== old.flow || saved.shipCustomerName !== old.shipCustomerName || saved.declarationFlag !== old.declarationFlag)) throw new Error('保存回應的原始主鍵或唯讀欄位不一致。')
  const oldKey = old && editing ? addressKey(old) : null
  addresses.value = addresses.value.filter(row => addressKey(row) !== addressKey(saved) && addressKey(row) !== oldKey)
  addresses.value.push(clone(saved)); selectedAddresses.value = [addressKey(saved)]
  addressSource.value = clone(saved); addressDraft.value = addressWrite(saved); mode.value = 'edit'; formReady.value = true; setBaseline()
}
function adoptDeclaration(saved: ReturnType<typeof readSavedDeclaration>, expectedKey: DeclarationKey, way: string): void {
  if (groupKey(formOwner.value, saved.group.key) !== groupKey(formOwner.value, expectedKey)) throw new Error('保存回應的原始報關群組不一致。')
  declarations.value = declarations.value.filter(row => !sameGroup(formOwner.value, row, saved.group.key)).concat(clone(saved.items))
  groups.value = groups.value.filter(group => groupKey(formOwner.value, group.key) !== groupKey(formOwner.value, saved.group.key)).concat(clone(saved.group))
  selectedGroups.value = [groupKey(formOwner.value, saved.group.key)]; declarationSource.value = clone(saved.group); declarationSourceRows.value = clone(saved.items)
  declarationDraft.value = { ...declarationKey(saved.group.key), declarationWay: saved.inserted?.declarationWay ?? way }
  contextAddress.value = null; mode.value = 'edit'; formReady.value = true; setBaseline()
}
function closeSavedEditor(): void { editor.value = null; formReady.value = false; ++formEpoch }
async function saveEditor(close: boolean): Promise<void> {
  if (!editor.value || formBlocked.value || disposed) return
  if (mode.value === 'edit' && !dirty.value) { if (close) { closeSavedEditor(); await restoreFocus() }; return }
  if (!close && !dirty.value) return
  formError.value = editor.value === 'address' ? validateAddress(close) : validateDeclaration()
  if (formError.value) return
  const epoch = formEpoch, kind = editor.value, editing = mode.value === 'edit', rawOwner = formOwner.value
  const oldAddress = addressSource.value ? clone(addressSource.value) : null, oldGroup = declarationSource.value ? clone(declarationSource.value) : null
  if (editing && !(kind === 'address' ? oldAddress : oldGroup)) { formReady.value = false; formError.value = '原始資料與版本未載入，請關閉後重新查詢。'; return }
  saving.value = true; formNotice.value = ''; notice.value = ''; error.value = ''; let returnedSuccess = false
  try {
    let message: string
    if (kind === 'address') {
      const payload = clone(addressDraft.value)
      const response = await request(path(addressRoot, rawOwner, editing ? 'item' : '', editing ? oldAddress!.shipAddr : undefined), { method: editing ? 'PUT' : 'POST', body: JSON.stringify(editing ? { ...payload, dataStamp: oldAddress!.dataStamp } : payload) })
      returnedSuccess = true; const saved = readAddress(await json(response))
      if (disposed || epoch !== formEpoch) return
      adoptAddress(saved, oldAddress, editing); message = '出貨地址已儲存。'
    } else {
      const key = declarationKey(declarationDraft.value), way = declarationDraft.value.declarationWay
      if (!editing && !contextAddress.value) throw new Error('新增使用的原始出貨地址未載入，請關閉後重新選取。')
      const payload = editing ? { key: oldGroup!.key, expectedGroupVersion: oldGroup!.aggregateVersion, declarationWay: way } : { ...key, declarationWay: way, sourceShipAddr: contextAddress.value!.shipAddr }
      const response = await request(path(declarationRoot, rawOwner, editing ? 'group' : ''), { method: editing ? 'PUT' : 'POST', body: JSON.stringify(payload) })
      returnedSuccess = true; const saved = readSavedDeclaration(await json(response), rawOwner, !editing)
      if (disposed || epoch !== formEpoch) return
      adoptDeclaration(saved, editing ? oldGroup!.key : key, way); message = '報關資料已儲存，實際' + (editing ? '更新' : '新增') + saved.affectedRows + '筆；目前整組' + saved.group.rowCount + '筆。'
    }
    formNotice.value = message; notice.value = message
    if (close) closeSavedEditor()
    await refreshLists(rawOwner, true)
    if (!addressesReady.value || !declarationsReady.value) { notice.value = message + ' 清單更新未完成，已保留保存結果，請重新查詢。'; formNotice.value = notice.value }
  } catch (cause) {
    if (!disposed && epoch === formEpoch) {
      formError.value = fail(cause)
      if (returnedSuccess || !(cause instanceof ApiFailure) || cause.status >= 500) { formReady.value = false; formError.value += ' 儲存結果無法確認，請關閉後重新查詢，勿重送。' }
      else if (cause.status === 404 || cause.status === 409) { formReady.value = false; formError.value += ' 請關閉後重新查詢最新資料。' }
    }
  } finally { saving.value = false; await restoreFocus() }
}
async function deleteSelected(kind: 'address' | 'declaration'): Promise<void> {
  if (commandsBlocked.value || owner.value === null || disposed || kind === 'address' && (!addressesReady.value || !addressTargets.value.length) || kind === 'declaration' && (!declarationsReady.value || !groupTargets.value.length)) return
  const rawOwner = owner.value, targets = kind === 'address' ? clone(addressTargets.value) : clone(groupTargets.value), epoch = ++operationEpoch
  const previous = activeElement(), totalRows = kind === 'address' ? targets.length : (targets as DeclarationGroup[]).reduce((sum, group) => sum + group.rowCount, 0)
  busy.value = true; error.value = ''; notice.value = ''; let completed = 0, affected = 0
  try {
    if (!await requestConfirmation(kind === 'address' ? '確定刪除選取的' + targets.length + '筆出貨地址？報關資料會保留。' : '確定刪除選取的' + targets.length + '組報關資料，共' + totalRows + '筆？每組的全部資料列都會刪除。')) return
    if (disposed || epoch !== operationEpoch) return
    for (const target of targets) {
      if (kind === 'address') {
        const row = target as Address
        const response = await request(path(addressRoot, rawOwner, 'item', row.shipAddr), { method: 'DELETE', body: JSON.stringify({ dataStamp: row.dataStamp, expectedFlow: row.flow, expectedShipCustomerName: row.shipCustomerName, expectedDeclarationFlag: row.declarationFlag }) })
        if (response.status !== 204) throw new Error('未取得完整地址刪除結果。')
        addresses.value = addresses.value.filter(item => addressKey(item) !== addressKey(row)); affected++
      } else {
        const group = target as DeclarationGroup
        const response = await request(path(declarationRoot, rawOwner, 'group'), { method: 'DELETE', body: JSON.stringify({ key: group.key, expectedGroupVersion: group.aggregateVersion }) })
        affected += readDeletedDeclaration(await json(response), rawOwner, group)
        declarations.value = declarations.value.filter(item => !sameGroup(rawOwner, item, group.key)); groups.value = groups.value.filter(item => groupKey(rawOwner, item.key) !== groupKey(rawOwner, group.key))
      }
      completed++; prune()
    }
    notice.value = '已確認刪除' + completed + '/' + targets.length + (kind === 'address' ? '筆出貨地址。' : '組報關資料，實際' + affected + '筆。')
    await refreshLists(rawOwner, true)
    if (!addressesReady.value || !declarationsReady.value) error.value = '刪除已完成；清單更新未完成，請重新查詢。'
  } catch (cause) {
    if (!disposed) {
      error.value = fail(cause) + ' 已確認完成' + completed + '/' + targets.length + (kind === 'address' ? '筆' : '組，實際' + affected + '筆') + '；其餘結果請重新查詢確認。'
      await refreshLists(rawOwner, true)
    }
  } finally { busy.value = false; await restorePageFocus(previous) }
}
async function closeEditor(): Promise<void> {
  if (writing.value || picker.value || confirmationMessage.value !== null || disposed) return
  if (dirty.value && !await requestConfirmation(formReady.value ? '尚有未儲存的修改，確定放棄？' : '儲存結果需重新查詢確認，確定關閉此視窗？')) { await restoreFocus(); return }
  if (disposed) return
  closeSavedEditor(); formError.value = ''; formNotice.value = ''; await restoreFocus()
}
async function openPicker(kind: Exclude<Picker, null>): Promise<void> {
  if (disposed || writing.value || confirmationMessage.value !== null || picker.value || (kind === 'customer' ? blocked.value : editor.value !== 'address' || !formReady.value)) return
  previousPickerFocus = activeElement(); picker.value = kind; pickerSearch.value = ''; pickerError.value = ''; pickerLoading.value = true
  customerOptions.value = []; referenceOptions.value = []; const epoch = ++pickerEpoch, sourceEpoch = formEpoch
  await restoreFocus()
  try {
    const value = await json(await request(kind === 'customer' ? addressRoot + '/lookups/customers' : 'api/ent/' + (kind === 'taker' ? 'taker-basis' : kind === 'forwarder' ? 'forwarder-basis' : 'broker-basis') + '?issueState=2'))
    if (disposed || epoch !== pickerEpoch || picker.value !== kind || kind !== 'customer' && sourceEpoch !== formEpoch) return
    if (kind === 'customer') customerOptions.value = readCustomers(value); else referenceOptions.value = readReferences(value, kind)
  } catch (cause) { if (!disposed && epoch === pickerEpoch) pickerError.value = fail(cause) }
  finally { if (epoch === pickerEpoch) pickerLoading.value = false }
}
async function closePicker(): Promise<void> {
  if (writing.value || confirmationMessage.value !== null) return
  picker.value = null; ++pickerEpoch; pickerLoading.value = false; await nextTick()
  if (previousPickerFocus?.isConnected && !previousPickerFocus.closest('[inert]') && !previousPickerFocus.matches(':disabled')) previousPickerFocus.focus(); else await restoreFocus()
}
async function chooseCustomer(row: CustomerOption): Promise<void> {
  if (pickerLoading.value || picker.value !== 'customer' || row.issueState !== 2 || writing.value) return
  approvedCustomer.value = clone(row); ownerInput.value = row.customerNo; await closePicker(); await queryOwner()
}
async function chooseReference(row: ReferenceOption): Promise<void> {
  if (pickerLoading.value || !picker.value || picker.value === 'customer' || row.issueState !== 2 || writing.value || editor.value !== 'address' || !formReady.value) return
  const field = picker.value === 'taker' ? 'takerNo' : picker.value === 'forwarder' ? 'forwarderNo' : 'broker'
  addressDraft.value[field] = row.code; await closePicker()
}
function clearReference(field: 'takerNo' | 'forwarderNo' | 'broker'): void { if (!formBlocked.value) addressDraft.value[field] = null }
function audit(field: 'creator' | 'createDate' | 'reviser' | 'reviseDate'): string {
  if (editor.value === 'address') return addressSource.value && mode.value === 'edit' ? display(addressSource.value[field], field) : ''
  const rows = declarationSourceRows.value
  if (!rows.length) return ''
  return rows.every(row => row[field] === rows[0]![field]) ? display(rows[0]![field], field) : '各列不同'
}
function activeElement(): HTMLElement | null { return document.activeElement instanceof HTMLElement ? document.activeElement : null }
function topModal(): HTMLElement | null { return picker.value ? pickerElement.value : editor.value === 'address' ? addressElement.value : editor.value === 'declaration' ? declarationElement.value : null }
function controls(modal: HTMLElement): HTMLElement[] { return Array.from(modal.querySelectorAll<HTMLElement>('input:not(:disabled),textarea:not(:disabled),button:not(:disabled),select:not(:disabled),[tabindex="0"]')).filter(item => item.offsetParent !== null && !item.closest('[inert]')) }
async function restorePageFocus(previous: HTMLElement | null): Promise<void> {
  await nextTick(); if (disposed || editor.value || picker.value || confirmationMessage.value !== null || writing.value) return
  if (previous?.isConnected && !previous.matches(':disabled') && !previous.closest('[inert]')) previous.focus()
  else pageElement.value?.querySelector<HTMLElement>('.master-surface input:not(:disabled),.master-surface button:not(:disabled)')?.focus()
}
async function restoreFocus(): Promise<void> {
  await nextTick(); if (disposed || confirmationMessage.value !== null || writing.value) return
  const modal = topModal()
  if (modal) { const preferred = modal.querySelector<HTMLElement>('[data-first-focus]:not(:disabled):not([readonly])'); (preferred ?? controls(modal)[0] ?? modal).focus() }
  else await restorePageFocus(previousFormFocus)
}
function trapTab(event: KeyboardEvent): void {
  if (confirmationMessage.value !== null) return
  if (writing.value) { event.preventDefault(); return }
  const modal = topModal(); if (!modal) return
  const items = controls(modal), first = items[0], last = items.at(-1)
  if (!first || !last) { event.preventDefault(); modal.focus(); return }
  if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus() }
  else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus() }
}
function keepFocus(event: KeyboardEvent): void {
  if (event.key !== 'Tab' || confirmationMessage.value !== null || !topModal()) return
  if (writing.value) { event.preventDefault(); return }
  if (!topModal()!.contains(document.activeElement)) { event.preventDefault(); void restoreFocus() }
}
function closePage(): void {
  if (blocked.value || disposed) return
  ++listEpoch; ++formEpoch; ++pickerEpoch; ++operationEpoch; addresses.value = []; declarations.value = []; groups.value = []; selectedAddresses.value = []; selectedGroups.value = []; emit('close')
}
onMounted(() => window.addEventListener('keydown', keepFocus, true))
onBeforeUnmount(() => { disposed = true; ++listEpoch; ++formEpoch; ++pickerEpoch; ++operationEpoch; window.removeEventListener('keydown', keepFocus, true) })
</script>

<template>
  <section ref="pageElement" class="ship-page" aria-label="客戶出貨地址資料">
    <div class="master-surface" :inert="editor!==null || picker!==null || writing || confirmationMessage!==null">
      <fieldset class="query-strip" :disabled="blocked">
        <label>客戶編號<input v-model="ownerInput" class="kd-input" aria-label="客戶編號查詢" @keydown.enter.prevent="queryOwner"></label>
        <button class="kd-button" type="button" aria-label="查找已簽核客戶" @click="openPicker('customer')">…</button>
        <button class="kd-button" type="button" @click="queryOwner">查詢</button>
        <span v-if="approvedCustomer?.customerNo===ownerInput" class="customer-name">{{ approvedCustomer.customerName }}</span>
      </fieldset>
      <p v-if="error" class="message error" role="alert">{{ error }}</p><p v-if="notice" class="message success" role="status">{{ notice }}</p>
      <div class="ship-panels">
        <fieldset class="win-group"><legend>Ship To Code（出貨地點）</legend>
          <p v-if="addressLoadError" class="message error" role="alert">{{ addressLoadError }}</p>
          <div class="panel-layout"><div class="grid-scroll"><table class="kd-grid"><thead><tr>
            <th class="check-cell"><input type="checkbox" aria-label="選取所有出貨地址" :checked="allAddresses" :indeterminate="addressTargets.length>0 && !allAddresses" :disabled="commandsBlocked || !addressesReady || !addresses.length" @change="selectAllAddresses(($event.target as HTMLInputElement).checked)"></th>
            <th v-for="[field,label] in addressColumns" :key="field">{{ label }}</th>
          </tr></thead><tbody>
            <tr v-for="row in addresses" :key="addressKey(row)" :tabindex="commandsBlocked || !addressesReady ? -1 : 0" :class="{selected:selectedAddresses.includes(addressKey(row))}" :aria-selected="selectedAddresses.includes(addressKey(row))" @click="selectAddress(row)" @keydown.enter.prevent="selectAddress(row)">
              <td class="check-cell" @click.stop><input type="checkbox" :aria-label="'選取出貨地址 '+row.shipAddr" :checked="selectedAddresses.includes(addressKey(row))" :disabled="commandsBlocked || !addressesReady" @change="selectAddress(row,($event.target as HTMLInputElement).checked)"></td>
              <td v-for="[field] in addressColumns" :key="field" :title="row[field]===null ? 'NULL' : String(row[field])">{{ display(row[field],field) }}</td>
            </tr>
            <tr v-if="!addresses.length"><td :colspan="addressColumns.length+1" class="empty">{{ loading?'資料載入中…':owner===null?'請選擇或查詢客戶。':addressLoadError?'出貨地址未能載入。':'沒有出貨地址資料。' }}</td></tr>
          </tbody></table></div><div class="vertical-actions">
            <button class="kd-button primary" type="button" :disabled="commandsBlocked || !addressesReady || !approvedOwner" @click="openAddress('new')">新增</button>
            <button class="kd-button" type="button" :disabled="commandsBlocked || !addressesReady || !singleAddress" @click="openAddress('edit')">修改</button>
            <button class="kd-button" type="button" :disabled="commandsBlocked || !addressesReady || !addressTargets.length" @click="deleteSelected('address')">刪除</button>
            <button class="kd-button" type="button" :disabled="commandsBlocked || !addressesReady || !approvedOwner || !singleAddress" @click="openAddress('copy')">複製</button>
          </div></div><footer class="grid-status">資料總筆數 {{ addresses.length }}<span v-if="owner!==null && !approvedOwner">　新增／複製請先查找已簽核客戶。</span></footer>
        </fieldset>
        <fieldset class="win-group"><legend>Ship To Declaration（出貨報關）</legend>
          <p v-if="declarationLoadError" class="message error" role="alert">{{ declarationLoadError }}</p>
          <div class="panel-layout"><div class="grid-scroll"><table class="kd-grid"><thead><tr>
            <th class="check-cell"><input type="checkbox" aria-label="選取所有報關群組" :checked="allGroups" :indeterminate="groupTargets.length>0 && !allGroups" :disabled="commandsBlocked || !declarationsReady || !groups.length" @change="selectAllGroups(($event.target as HTMLInputElement).checked)"></th>
            <th v-for="[field,label] in declarationColumns" :key="field">{{ label }}</th>
          </tr></thead><tbody>
            <tr v-for="(row,index) in declarations" :key="JSON.stringify([groupKey(row.customerNo,row),index])" :tabindex="commandsBlocked || !declarationsReady ? -1 : 0" :class="{selected:selectedGroups.includes(groupKey(row.customerNo,row))}" :aria-selected="selectedGroups.includes(groupKey(row.customerNo,row))" @click="selectGroup(row)" @keydown.enter.prevent="selectGroup(row)">
              <td class="check-cell" @click.stop><input type="checkbox" :aria-label="'選取報關群組 '+row.shipTo+' '+row.flow+' 第'+(index+1)+'列'" :checked="selectedGroups.includes(groupKey(row.customerNo,row))" :disabled="commandsBlocked || !declarationsReady" @change="selectGroup(row,($event.target as HTMLInputElement).checked)"></td>
              <td v-for="[field] in declarationColumns" :key="field" :title="row[field]===null ? 'NULL' : String(row[field])">{{ display(row[field],field) }}</td>
            </tr>
            <tr v-if="!declarations.length"><td :colspan="declarationColumns.length+1" class="empty">{{ loading?'資料載入中…':owner===null?'請選擇或查詢客戶。':declarationLoadError?'報關資料未能載入。':'沒有報關資料。' }}</td></tr>
          </tbody></table></div><div class="vertical-actions">
            <button class="kd-button primary" type="button" :disabled="commandsBlocked || !declarationsReady || !addressesReady || !approvedOwner || !singleAddress" @click="openDeclaration('new')">新增</button>
            <button class="kd-button" type="button" :disabled="commandsBlocked || !declarationsReady || !singleGroup" @click="openDeclaration('edit')">修改</button>
            <button class="kd-button" type="button" :disabled="commandsBlocked || !declarationsReady || !groupTargets.length" @click="deleteSelected('declaration')">刪除</button>
          </div></div><footer class="grid-status">資料總筆數 {{ declarations.length }}　選取 {{ groupTargets.length }} 組／{{ selectedDeclarationRows }} 筆<span>　修改與刪除作用於整組；新增請先選單筆出貨地址。</span></footer>
        </fieldset>
      </div><div class="close-row"><button class="kd-button" type="button" :disabled="blocked" @click="closePage">關閉</button></div>
    </div>

    <div v-if="editor==='address'" class="dialog-backdrop" @click.self="closeEditor"><section ref="addressElement" class="win-dialog" role="dialog" aria-modal="true" aria-label="出貨地點設定" tabindex="-1" :inert="writing || picker!==null || confirmationMessage!==null" @keydown.tab="trapTab" @keydown.esc.stop.prevent="closeEditor">
      <header><h2>{{ mode==='new'?'新增':mode==='copy'?'複製':'修改' }}出貨地點設定</h2><button class="kd-button" type="button" aria-label="關閉出貨地點設定" :disabled="writing || picker!==null || confirmationMessage!==null" @click="closeEditor">×</button></header>
      <form novalidate @submit.prevent="saveEditor(true)"><div class="form-scroll"><fieldset :disabled="formBlocked"><legend>客戶出貨地址資料</legend><div class="form-grid">
        <label>客戶編號<input :value="formOwner" class="kd-input" readonly aria-label="地址客戶編號"></label>
        <label>出貨客戶名稱 *<input v-model="addressDraft.shipCustomerName" class="kd-input" :readonly="mode==='edit'" aria-label="出貨客戶名稱" :data-first-focus="mode!=='edit'?true:undefined"></label>
        <label>流程 *<input v-model="addressDraft.flow" class="kd-input" :readonly="mode==='edit'" aria-label="地址流程"></label>
        <label>出貨地址 *<input v-model="addressDraft.shipAddr" class="kd-input" aria-label="出貨地址" :data-first-focus="mode==='edit'?true:undefined"></label>
        <label v-for="[field,label] in textFields" :key="field">{{ label }}<input v-model="addressDraft[field]" class="kd-input" :aria-label="label"></label>
        <div class="field"><span>海外</span><div class="flag-options" role="radiogroup" aria-label="地址海外"><label><input v-model="addressDraft.overseaFlag" type="radio" value="1" name="address-oversea">是</label><label><input v-model="addressDraft.overseaFlag" type="radio" value="0" name="address-oversea">否</label><span v-if="addressDraft.overseaFlag!=='0' && addressDraft.overseaFlag!=='1'">原值：{{ addressDraft.overseaFlag===null?'NULL':addressDraft.overseaFlag }}</span></div></div>
        <div class="field"><span>報關</span><input v-if="mode==='edit'" :value="addressDraft.declarationFlag" class="kd-input" readonly aria-label="地址報關"><div v-else class="flag-options" role="radiogroup" aria-label="地址報關"><label><input v-model="addressDraft.declarationFlag" type="radio" value="1" name="address-declaration">是</label><label><input v-model="addressDraft.declarationFlag" type="radio" value="0" name="address-declaration">否</label><span v-if="!['0','1'].includes(addressDraft.declarationFlag)">原值：{{ addressDraft.declarationFlag }}</span></div></div>
        <div v-for="[kind,field,label] in refs" :key="kind" class="field"><label :for="'address-'+kind">{{ label }}</label><div class="lookup-field"><input :id="'address-'+kind" :value="addressDraft[field]" class="kd-input" readonly :aria-label="label"><button class="kd-button" type="button" :aria-label="'查找'+label" @click="openPicker(kind)">…</button><button class="kd-button" type="button" :aria-label="'清除'+label" :disabled="addressDraft[field]===null" @click="clearReference(field)">清除</button></div></div>
        <label class="wide">說明<textarea v-model="addressDraft.description" class="kd-input" rows="3" aria-label="地址說明"></textarea></label>
      </div></fieldset><div class="audit"><label v-for="[field,label] in auditFields" :key="field">{{ label }}<input :value="audit(field)" readonly></label></div>
      <p v-if="formError" class="message error" role="alert">{{ formError }}</p><p v-if="formNotice" class="message success" role="status">{{ formNotice }}</p></div>
      <footer class="dialog-actions"><button class="kd-button" type="button" :disabled="writing || confirmationMessage!==null" @click="closeEditor">關閉</button><button class="kd-button" type="button" :disabled="formBlocked || !dirty" @click="saveEditor(false)">套用</button><button class="kd-button primary" type="submit" :disabled="formBlocked">確定</button></footer></form>
    </section></div>

    <div v-if="editor==='declaration'" class="dialog-backdrop" @click.self="closeEditor"><section ref="declarationElement" class="win-dialog declaration-dialog" role="dialog" aria-modal="true" aria-label="出貨報關設定" tabindex="-1" :inert="writing || confirmationMessage!==null" @keydown.tab="trapTab" @keydown.esc.stop.prevent="closeEditor">
      <header><h2>{{ mode==='new'?'新增':'修改' }}出貨報關設定</h2><button class="kd-button" type="button" aria-label="關閉出貨報關設定" :disabled="writing || confirmationMessage!==null" @click="closeEditor">×</button></header>
      <form novalidate @submit.prevent="saveEditor(true)"><div class="form-scroll"><p v-if="mode==='edit' && declarationSource" class="scope-note">本次修改整組 {{ declarationSource.rowCount }} 筆，僅更新報關方式。</p>
      <fieldset :disabled="formBlocked"><legend>出貨報關資料</legend><div class="form-grid">
        <label>客戶編號<input :value="formOwner" class="kd-input" readonly aria-label="報關客戶編號"></label>
        <label>出貨對象 *<input v-model="declarationDraft.shipTo" class="kd-input" :readonly="mode==='edit'" aria-label="出貨對象" :data-first-focus="mode==='new'?true:undefined"></label>
        <label>流程 *<input v-model="declarationDraft.flow" class="kd-input" :readonly="mode==='edit'" aria-label="報關流程"></label>
        <label>報關方式 *<input v-model="declarationDraft.declarationWay" class="kd-input" aria-label="報關方式" :data-first-focus="mode==='edit'?true:undefined"></label>
        <div v-for="[field,label] in flagFields" :key="field" class="field"><span>{{ label }}</span><input v-if="mode==='edit'" :value="declarationDraft[field]" class="kd-input" readonly :aria-label="'報關'+label"><div v-else class="flag-options" role="radiogroup" :aria-label="'報關'+label"><label><input v-model="declarationDraft[field]" type="radio" value="1" :name="'declaration-'+field">是</label><label><input v-model="declarationDraft[field]" type="radio" value="0" :name="'declaration-'+field">否</label><span v-if="!['0','1'].includes(declarationDraft[field])">原值：{{ declarationDraft[field] }}</span></div></div>
      </div></fieldset><div class="audit"><label v-for="[field,label] in auditFields" :key="field">{{ label }}<input :value="audit(field)" readonly></label></div>
      <p v-if="mode==='edit' && declarationSourceRows.some(row=>row.declarationWay!==declarationSourceRows[0]?.declarationWay)" class="scope-note">這組含有不同報關方式；套用後整組使用本次輸入值。</p>
      <p v-if="formError" class="message error" role="alert">{{ formError }}</p><p v-if="formNotice" class="message success" role="status">{{ formNotice }}</p></div>
      <footer class="dialog-actions"><button class="kd-button" type="button" :disabled="writing || confirmationMessage!==null" @click="closeEditor">關閉</button><button class="kd-button" type="button" :disabled="formBlocked || !dirty" @click="saveEditor(false)">套用</button><button class="kd-button primary" type="submit" :disabled="formBlocked">確定</button></footer></form>
    </section></div>

    <div v-if="picker" class="dialog-backdrop picker-backdrop" @click.self="closePicker"><section ref="pickerElement" class="win-dialog picker-dialog" role="dialog" aria-modal="true" :aria-label="pickerTitle" tabindex="-1" :inert="writing || confirmationMessage!==null" @keydown.tab="trapTab" @keydown.esc.stop.prevent="closePicker">
      <header><h2>{{ pickerTitle }}</h2><button class="kd-button" type="button" aria-label="關閉查找" @click="closePicker">×</button></header><label class="picker-search">查找<input v-model="pickerSearch" class="kd-input" aria-label="查找關鍵字" data-first-focus></label>
      <p v-if="pickerError" class="message error" role="alert">{{ pickerError }}</p><div class="grid-scroll picker-grid"><table class="kd-grid"><thead><tr><th>編號</th><th>名稱</th><th v-if="picker==='customer'">簡稱</th></tr></thead><tbody>
        <template v-if="picker==='customer'"><tr v-for="row in visibleCustomers" :key="row.customerNo" :tabindex="pickerLoading?-1:0" @click="chooseCustomer(row)" @keydown.enter.prevent="chooseCustomer(row)"><td>{{ row.customerNo }}</td><td>{{ row.customerName }}</td><td>{{ row.customerSName }}</td></tr></template>
        <template v-else><tr v-for="row in visibleReferences" :key="row.code" :tabindex="pickerLoading?-1:0" @click="chooseReference(row)" @keydown.enter.prevent="chooseReference(row)"><td>{{ row.code }}</td><td>{{ row.name }}</td></tr></template>
        <tr v-if="pickerLoading || (picker==='customer' ? !visibleCustomers.length : !visibleReferences.length)"><td :colspan="picker==='customer'?3:2" class="empty">{{ pickerLoading?'資料載入中…':pickerError?'查找資料未能載入。':'沒有符合的已簽核資料。' }}</td></tr>
      </tbody></table></div><footer class="dialog-actions"><button class="kd-button" type="button" @click="closePicker">關閉</button></footer>
    </section></div>
    <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
  </section>
</template>

<style scoped>
.ship-page{color:var(--ink,#26251f);font:13px/1.45 "Segoe UI","Microsoft JhengHei",sans-serif}.master-surface{display:flex;flex-direction:column;gap:9px}.query-strip{display:flex;flex-wrap:wrap;align-items:center;gap:8px;border:1px solid var(--line,#e5e1d5);padding:9px 12px;background:#fff;min-width:0}.query-strip label{display:flex;align-items:center;gap:8px}.query-strip input{width:230px}.customer-name{color:var(--muted,#777366)}.kd-input{box-sizing:border-box;width:100%;min-width:0;min-height:31px;padding:5px 7px;border:1px solid #bfb9aa;border-radius:2px;background:#fff8d8;color:var(--ink,#26251f);font:inherit}.kd-input:read-only{background:#f1efe8}.kd-input:disabled{color:#777366;background:#f1efe8}.kd-input:focus-visible,.kd-button:focus-visible,input[type=checkbox]:focus-visible,input[type=radio]:focus-visible,tr:focus-visible{outline:2px solid var(--yellow,#e7c600);outline-offset:1px}.kd-button{min-height:31px;padding:5px 13px;border:1px solid #c8c2b4;border-radius:3px;background:#fff;color:inherit;font:inherit;cursor:pointer;white-space:nowrap}.kd-button.primary{background:#3b3930;border-color:#3b3930;color:#fff}.kd-button:disabled{opacity:.5;cursor:default}.ship-panels{display:grid;grid-template-rows:minmax(240px,1fr) minmax(225px,.9fr);gap:10px;min-height:540px}.win-group{min-width:0;min-height:0;margin:0;padding:7px 9px;border:1px solid var(--line,#e5e1d5);background:#fff;display:flex;flex-direction:column}.win-group legend{padding:0 6px}.panel-layout{display:grid;grid-template-columns:minmax(0,1fr) 78px;gap:9px;flex:1;min-height:0}.grid-scroll{min-width:0;overflow:auto;max-height:330px;border:1px solid var(--line,#e5e1d5)}.kd-grid{border-collapse:collapse;width:max-content;min-width:100%;white-space:nowrap;font-size:12px}.kd-grid th{position:sticky;top:0;z-index:1;background:#eeeae0;color:var(--ink,#26251f);font-weight:500;text-align:left}.kd-grid th,.kd-grid td{height:29px;padding:4px 8px;border-right:1px solid var(--line,#e5e1d5);border-bottom:1px solid var(--line,#e5e1d5);max-width:320px;overflow:hidden;text-overflow:ellipsis}.kd-grid tbody tr{cursor:pointer}.kd-grid tr.selected{background:#fff3bd}.kd-grid tbody tr:hover{background:#faf6df}.kd-grid tr.selected:hover{background:#fff3bd}.check-cell{width:28px;text-align:center!important}input[type=checkbox],input[type=radio]{accent-color:#9b8515}.empty{text-align:center!important;color:var(--muted,#777366);height:68px!important}.vertical-actions{display:flex;flex-direction:column;gap:6px}.vertical-actions .kd-button{padding-inline:7px}.grid-status{padding-top:7px;color:var(--muted,#777366);font-size:12px}.close-row{display:flex;justify-content:flex-end}.message{margin:5px 0;padding:7px 10px;border:1px solid;font-size:13px;white-space:pre-wrap;overflow-wrap:anywhere}.message.error{color:#8e3937;background:#fff6f4;border-color:#e3bdb5}.message.success{color:#485f38;background:#f6f8f0;border-color:#c6d2ba}.dialog-backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:18px;background:#19181666}.win-dialog{display:flex;flex-direction:column;width:min(1040px,100%);max-height:94vh;min-height:0;border:1px solid #c7c1b3;background:#fff;box-shadow:0 10px 35px #0004;outline:none}.win-dialog>header{display:flex;justify-content:space-between;align-items:center;gap:12px;padding:9px 13px;background:#f5f3ed;border-bottom:1px solid var(--line,#e5e1d5)}h2{margin:0;font-size:16px;font-weight:600}.win-dialog>form{display:flex;flex-direction:column;min-height:0;overflow:hidden}.form-scroll{overflow:auto;padding:7px 13px}.form-scroll fieldset{border:1px solid var(--line,#e5e1d5);padding:10px 13px;margin:6px 0 10px;min-width:0}.form-scroll legend{padding:0 6px}.form-grid{display:grid;grid-template-columns:1fr 1fr;gap:10px 18px}.form-grid>label,.form-grid>.field{display:grid;grid-template-columns:118px minmax(0,1fr);align-items:center;gap:8px;min-width:0}.form-grid>.wide{grid-column:1/-1}.flag-options{display:flex;align-items:center;flex-wrap:wrap;gap:12px;min-height:31px}.flag-options label{display:inline-flex;align-items:center;gap:4px}.flag-options span{color:var(--muted,#777366);overflow-wrap:anywhere}.lookup-field{display:flex;gap:4px;min-width:0}.lookup-field>.kd-input{flex:1;width:0}.lookup-field>.kd-button{padding-inline:7px}.audit{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:9px;padding:10px;background:#f8f7f2;border:1px solid var(--line,#e5e1d5)}.audit label{display:grid;gap:4px;color:var(--muted,#777366);font-size:12px}.audit input{box-sizing:border-box;width:100%;min-width:0;padding:5px;border:1px solid #d7d1c5;background:#f1efe8;color:inherit;font:inherit}.dialog-actions{display:flex;justify-content:flex-end;gap:8px;padding:10px 13px;border-top:1px solid var(--line,#e5e1d5);flex:none}.scope-note{margin:5px 0 9px;color:var(--muted,#777366)}.declaration-dialog{width:min(870px,100%)}.picker-backdrop{z-index:1100}.picker-dialog{width:min(800px,100%)}.picker-search{display:flex;align-items:center;gap:9px;padding:12px}.picker-grid{margin:0 12px;min-height:170px;max-height:52vh}.picker-grid .kd-grid{width:100%}
@media(max-width:850px){.form-grid{grid-template-columns:1fr}.form-grid>.wide{grid-column:auto}.audit{grid-template-columns:1fr 1fr}.query-strip input{width:min(230px,45vw)}.dialog-backdrop{padding:9px}.grid-status span{display:block}.panel-layout{grid-template-columns:minmax(0,1fr) 68px}.vertical-actions .kd-button{padding-inline:4px}.form-grid>label,.form-grid>.field{grid-template-columns:108px minmax(0,1fr)}}
</style>
