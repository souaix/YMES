<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'
import { blankCustomerWrite, copyCustomerHeader, createEt101Service, editCustomerBody, Et101ServiceError,
  type CustomerDto, type CustomerWrite, type BrokerOption, type ForwarderOption } from '../services/et101'

type FormMode = 'new' | 'edit' | 'copy' | null
type StateFilter = number | 'all' | 'legacy'
type PickerKind = 'broker' | 'forwarder' | null
const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()
const issueStates: { value: StateFilter; label: string }[] = [{ value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' }, { value: 'all', label: '全部' }, { value: 'legacy', label: '未設定／其他' }]
const statusOptions = [{ value: '', label: '空白' }, { value: 'O', label: '有效' }, { value: 'C', label: '失效' }, { value: 'B', label: '呆帳' }, { value: 'P', label: '暫停' }]
const invoiceTypeOptions = [{ value: '', label: '空白' }, { value: '0', label: '內銷' }, { value: '1', label: '外銷' }]
const customerTypeOptions = [{ value: '', label: '空白' }, { value: '0', label: '國內' }, { value: '1', label: '國外' }]
const arTypeOptions = [{ value: '', label: '空白' }, { value: '0', label: '入庫計價' }, { value: '1', label: '出貨計價' }]
const customers = ref<CustomerDto[]>([]), issueState = ref<StateFilter>(0), keyword = ref(''), selectedCustomerNos = ref<string[]>([])
const loading = ref(false), rowsReady = ref(false), writing = ref(false), formLoading = ref(false), closing = ref(false), writeUncertain = ref(false)
const errorMessage = ref(''), successMessage = ref(''), readWarning = ref(''), formError = ref('')
const formMode = ref<FormMode>(null), formReady = ref(false), form = ref<CustomerWrite>(blankCustomerWrite()), originalRow = ref<CustomerDto | null>(null), formOriginal = ref('')
const pageElement = ref<HTMLElement | null>(null), formElement = ref<HTMLElement | null>(null), pickerElement = ref<HTMLElement | null>(null)
const pickerKind = ref<PickerKind>(null), pickerLoading = ref(false), pickerReady = ref(false), pickerKeyword = ref(''), pickerError = ref('')
const brokerRows = ref<BrokerOption[]>([]), forwarderRows = ref<ForwarderOption[]>([])
let disposed = false, scopeEpoch = 0, listEpoch = 0, formEpoch = 0, pickerEpoch = 0
let previousModalFocus: HTMLElement | null = null, previousPickerFocus: HTMLElement | null = null
const modalOpen = computed(() => formMode.value !== null || pickerKind.value !== null)
const pageBlocked = computed(() => loading.value || writing.value || closing.value || modalOpen.value || confirmationMessage.value !== null)
const mutationBlocked = computed(() => pageBlocked.value || writeUncertain.value)
const formBusy = computed(() => writing.value || formLoading.value || closing.value || pickerKind.value !== null || confirmationMessage.value !== null)
const dirty = computed(() => formMode.value !== null && JSON.stringify(form.value) !== formOriginal.value)
const visibleCustomers = computed(() => customers.value.filter(row => issueState.value === 'all' || (issueState.value === 'legacy' ? ![-1, 0, 1, 2].includes(row.issueState as number) : row.issueState === issueState.value)))
const selectedCustomers = computed(() => visibleCustomers.value.filter(row => selectedCustomerNos.value.includes(row.customerNo)))
const selectedCustomer = computed(() => selectedCustomers.value.length === 1 ? selectedCustomers.value[0] : null)
const lockBasicFields = computed(() => formMode.value === 'edit' && originalRow.value?.issueState !== 0)
const audit = computed(() => ({ creator: originalRow.value?.creator ?? '', createDate: formatDate(originalRow.value?.createDate ?? null), reviser: originalRow.value?.reviser ?? '', reviseDate: formatDate(originalRow.value?.reviseDate ?? null) }))
const pickerRows = computed(() => {
  const rows = pickerKind.value === 'broker' ? brokerRows.value.map(row => ({ no: row.brokerNo, name: row.contactorName, state: row.issueState }))
    : forwarderRows.value.map(row => ({ no: row.forwarderNo, name: row.contactorName, state: row.issueState }))
  const search = pickerKeyword.value.toLowerCase()
  return rows.filter(row => row.no.toLowerCase().includes(search))
})
const fail = (cause: unknown): string => cause instanceof Error ? cause.message : '客戶服務操作失敗。'
const service = () => createEt101Service(props.apiBaseUrl, window.location.origin)
function stateName(value: StateFilter | null): string { return issueStates.find(item => item.value === value)?.label ?? (value === null ? '未設定' : String(value)) }
function valueName(options: { value: string; label: string }[], value: string | null): string { return value === null ? '' : options.find(item => item.value === value)?.label ?? value }
function legacyOption(options: { value: string; label: string }[], value: string | null): boolean { return value !== null && !options.some(item => item.value === value) }
function formatDate(value: string | null): string { return value === null ? '' : new Date(value).toLocaleString('zh-TW') }
function applyFilter(): void { const keys = new Set(visibleCustomers.value.map(row => row.customerNo)); selectedCustomerNos.value = selectedCustomerNos.value.filter(key => keys.has(key)) }
function upsert(row: CustomerDto): void { const index = customers.value.findIndex(item => item.customerNo === row.customerNo); if (index < 0) customers.value.push(row); else customers.value[index] = row }
function selectRow(row: CustomerDto, event?: MouseEvent | KeyboardEvent): void {
  if (pageBlocked.value || !rowsReady.value) return
  if (event?.ctrlKey || event?.metaKey) toggleSelection(row, !selectedCustomerNos.value.includes(row.customerNo))
  else selectedCustomerNos.value = [row.customerNo]
}
function toggleSelection(row: CustomerDto, checked: boolean): void {
  if (pageBlocked.value || !rowsReady.value) return
  selectedCustomerNos.value = checked ? [...new Set([...selectedCustomerNos.value, row.customerNo])] : selectedCustomerNos.value.filter(key => key !== row.customerNo)
}
function toggleAllVisible(checked: boolean): void {
  if (pageBlocked.value || !rowsReady.value) return
  const keys = new Set(visibleCustomers.value.map(row => row.customerNo))
  selectedCustomerNos.value = checked ? [...keys] : selectedCustomerNos.value.filter(key => !keys.has(key))
}
async function reloadRows(): Promise<void> {
  const epoch = ++listEpoch, scope = scopeEpoch, api = service()
  const rows = await api.list(keyword.value)
  if (disposed || epoch !== listEpoch || scope !== scopeEpoch) return
  customers.value = rows; rowsReady.value = true; applyFilter()
}
async function queryCustomers(): Promise<void> {
  if (pageBlocked.value) return
  const scope = scopeEpoch
  loading.value = true; errorMessage.value = ''; readWarning.value = ''
  try { await reloadRows() }
  catch (cause) { if (!disposed && scope === scopeEpoch) { rowsReady.value = false; errorMessage.value = fail(cause) } }
  finally { if (!disposed && scope === scopeEpoch) { loading.value = false; await restoreFocus() } }
}
async function openForm(mode: Exclude<FormMode, null>): Promise<void> {
  if (mutationBlocked.value || (mode !== 'new' && (!rowsReady.value || !selectedCustomer.value))) return
  const selected = selectedCustomer.value, epoch = ++formEpoch, scope = scopeEpoch
  previousModalFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null
  formMode.value = mode; form.value = blankCustomerWrite(); originalRow.value = null; formReady.value = false; formError.value = ''; successMessage.value = ''; formLoading.value = true
  formOriginal.value = JSON.stringify(form.value)
  try {
    if (mode !== 'new' && selected) {
      const fresh = await service().get(selected.customerNo)
      if (disposed || epoch !== formEpoch || scope !== scopeEpoch) return
      upsert(fresh); applyFilter()
      form.value = mode === 'copy' ? copyCustomerHeader(fresh) : editCustomerBody(fresh)
      if (mode === 'edit') originalRow.value = fresh
    }
    formOriginal.value = JSON.stringify(form.value); formReady.value = true
  } catch (cause) { if (!disposed && epoch === formEpoch && scope === scopeEpoch) formError.value = fail(cause) }
  finally { if (!disposed && epoch === formEpoch && scope === scopeEpoch) { formLoading.value = false; await restoreFocus() } }
}
function writeOnly(): CustomerWrite {
  // Edit body helper includes tokens; never spread those into POST or hidden
  // source fields. Re-project29 visible values for every submission.
  return Object.fromEntries(Object.keys(blankCustomerWrite()).map(key => [key, form.value[key as keyof CustomerWrite]])) as unknown as CustomerWrite
}
function validateForm(): string {
  const body = writeOnly(), old = originalRow.value
  if (!body.customerNo.trim()) return '請輸入客戶編號。'
  for (const [key, label] of [['customerName', '客戶名稱'], ['customerSName', '客戶簡稱']] as const)
    if ((formMode.value !== 'edit' || body[key] !== old?.[key]) && !body[key]?.trim()) return '請輸入' + label + '。'
  const limits: Partial<Record<keyof CustomerWrite, number>> = { customerNo: 50, customerName: 100, customerSName: 100, customerEName: 50, telNo: 25, faxNo: 25, www: 50, taxCode: 25, address: 255, address2: 255, director: 50, country: 50, description: 255, status: 5, currency: 6, invTypeNo: 1, invAddr: 255, invAddr2: 255, invCustNo: 20, invRemark: 100, custType: 1, arType: 1, freightTerms: 20, paymentTerms: 20, acctNo: 50, deliveryTerm: 50, forwarderAssigned: 50, brokerAssigned: 50, insuranceCoverage: 50 }
  for (const [key, limit] of Object.entries(limits)) { const value = body[key as keyof CustomerWrite]; if (value !== null && Array.from(value).length > limit!) return key + ' 超過來源欄位長度。' }
  return ''
}
function adoptSaved(saved: CustomerDto): void {
  upsert(saved); originalRow.value = saved; form.value = editCustomerBody(saved); formMode.value = 'edit'; formReady.value = true
  formOriginal.value = JSON.stringify(form.value); selectedCustomerNos.value = [saved.customerNo]; applyFilter()
}
async function saveCustomer(closeAfterSave: boolean): Promise<void> {
  if (!formMode.value || !formReady.value || formBusy.value || writeUncertain.value) return
  if (!dirty.value && formMode.value === 'edit') { if (closeAfterSave) await closeForm(); return }
  formError.value = validateForm(); if (formError.value) return
  const scope = scopeEpoch, epoch = formEpoch, body = writeOnly(), old = originalRow.value
  const updating = formMode.value === 'edit' && old !== null
  writing.value = true; formError.value = ''; successMessage.value = ''; readWarning.value = ''
  try {
    const api = service()
    const saved = updating && old
      ? await api.update(old.customerNo, { ...body, issueState: old.issueState, dataStamp: old.dataStamp })
      : await api.create(body)
    if (disposed || scope !== scopeEpoch || epoch !== formEpoch) return
    if ((updating && old && (saved.customerNo !== old.customerNo || saved.issueState !== old.issueState || saved.dataStamp !== (old.dataStamp ?? 0) + 1))
      || (!updating && (saved.issueState !== 0 || saved.dataStamp !== 0)))
      throw new Et101ServiceError('保存回傳身份或版本不符契約，請先核對伺服器結果。', 200, true)
    adoptSaved(saved); successMessage.value = '客戶資料已儲存。'
    if (closeAfterSave) { formMode.value = null; formReady.value = false; ++formEpoch }
    try { await reloadRows() } catch (cause) { if (!disposed && scope === scopeEpoch) { rowsReady.value = false; readWarning.value = '已儲存；清單重新讀取失敗：' + fail(cause) } }
  } catch (cause) {
    if (!disposed && scope === scopeEpoch && epoch === formEpoch) {
      formError.value = fail(cause)
      if (cause instanceof Et101ServiceError && cause.unknownCommit) { writeUncertain.value = true; rowsReady.value = false }
    }
  } finally { if (!disposed && scope === scopeEpoch) { writing.value = false; await restoreFocus() } }
}
async function deleteSelected(): Promise<void> {
  if (mutationBlocked.value || !rowsReady.value || !selectedCustomers.value.length) return
  const targets = selectedCustomers.value.map(row => ({ ...row })), scope = scopeEpoch
  const focus = document.activeElement instanceof HTMLElement ? document.activeElement : null
  if (!await requestConfirmation('確定處理選取的 ' + targets.length + ' 筆客戶？已核准資料會停用；未簽核／不使用資料會連同地址及連絡人刪除。')) { await restorePageFocus(focus); return }
  if (disposed || scope !== scopeEpoch) return
  writing.value = true; errorMessage.value = ''; successMessage.value = ''; readWarning.value = ''
  let completed = 0, retired = 0, deleted = 0, addresses = 0, contacts = 0
  try {
    const api = service()
    for (const row of targets) {
      const result = await api.remove(row.customerNo, { issueState: row.issueState, dataStamp: row.dataStamp })
      if (disposed || scope !== scopeEpoch) return
      if (result.customerNo !== row.customerNo) throw new Et101ServiceError('刪除回傳身份不符，請先核對伺服器結果。', 200, true)
      if (result.kind === 'retired') { upsert(result.item); retired++ }
      else { customers.value = customers.value.filter(item => item.customerNo !== result.customerNo); deleted++; addresses += result.deletedAddressRows; contacts += result.deletedContactorRows }
      completed++; selectedCustomerNos.value = selectedCustomerNos.value.filter(key => key !== row.customerNo); applyFilter()
    }
  } catch (cause) {
    if (!disposed && scope === scopeEpoch) { errorMessage.value = fail(cause); if (cause instanceof Et101ServiceError && cause.unknownCommit) { writeUncertain.value = true; rowsReady.value = false } }
  } finally {
    if (!disposed && scope === scopeEpoch) {
      successMessage.value = '已確認完成 ' + completed + '/' + targets.length + ' 筆（停用 ' + retired + '、刪除 ' + deleted + '；地址 ' + addresses + '、連絡人 ' + contacts + '）。'
      try { await reloadRows() } catch (cause) { if (!disposed && scope === scopeEpoch) { rowsReady.value = false; readWarning.value = '已完成的結果保留；清單讀取失敗：' + fail(cause) } }
      if (!disposed && scope === scopeEpoch) { writing.value = false; await restorePageFocus(focus) }
    }
  }
}
async function approveSelected(): Promise<void> {
  if (mutationBlocked.value || !rowsReady.value || !selectedCustomers.value.length) return
  const targets = selectedCustomers.value.map(row => ({ ...row })), scope = scopeEpoch
  if (targets.some(row => row.issueState !== 0)) { errorMessage.value = '只有未簽核客戶可提出核准。'; return }
  const focus = document.activeElement instanceof HTMLElement ? document.activeElement : null
  if (!await requestConfirmation('確定核准選取的 ' + targets.length + ' 筆客戶？若需要簽核流程，會保留原資料並停止。')) { await restorePageFocus(focus); return }
  if (disposed || scope !== scopeEpoch) return
  writing.value = true; errorMessage.value = ''; successMessage.value = ''; readWarning.value = ''
  let completed = 0
  try {
    const api = service()
    for (const row of targets) {
      const saved = await api.approve(row.customerNo, { issueState: row.issueState, dataStamp: row.dataStamp })
      if (disposed || scope !== scopeEpoch) return
      // The service requires the complete true41DTO, state2, exact original
      // raw identity and bumped nullable stamp before accepting this result.
      upsert(saved); completed++; selectedCustomerNos.value = selectedCustomerNos.value.filter(key => key !== row.customerNo); applyFilter()
    }
  } catch (cause) {
    if (!disposed && scope === scopeEpoch) { errorMessage.value = fail(cause); if (cause instanceof Et101ServiceError && cause.unknownCommit) { writeUncertain.value = true; rowsReady.value = false } }
  } finally {
    if (!disposed && scope === scopeEpoch) {
      successMessage.value = '已確認核准 ' + completed + '/' + targets.length + ' 筆。'
      try { await reloadRows() } catch (cause) { if (!disposed && scope === scopeEpoch) { rowsReady.value = false; readWarning.value = '已核准結果保留；清單讀取失敗：' + fail(cause) } }
      if (!disposed && scope === scopeEpoch) { writing.value = false; await restorePageFocus(focus) }
    }
  }
}
async function openParentPicker(kind: Exclude<PickerKind, null>): Promise<void> {
  if (!formMode.value || !formReady.value || formBusy.value || writeUncertain.value) return
  previousPickerFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null
  pickerKind.value = kind; pickerReady.value = false; pickerLoading.value = true; pickerError.value = ''; pickerKeyword.value = ''; brokerRows.value = []; forwarderRows.value = []
  const epoch = ++pickerEpoch, scope = scopeEpoch
  await restoreFocus()
  try {
    const api = service(), rows = kind === 'broker' ? await api.brokers() : await api.forwarders()
    if (disposed || scope !== scopeEpoch || epoch !== pickerEpoch || pickerKind.value !== kind) return
    if (kind === 'broker') brokerRows.value = rows as BrokerOption[]; else forwarderRows.value = rows as ForwarderOption[]
    pickerReady.value = true
  } catch (cause) { if (!disposed && scope === scopeEpoch && epoch === pickerEpoch) pickerError.value = fail(cause) }
  finally { if (!disposed && scope === scopeEpoch && epoch === pickerEpoch) { pickerLoading.value = false; await restoreFocus() } }
}
function chooseParent(no: string): void {
  if (!pickerKind.value || !pickerReady.value || pickerLoading.value || writing.value || confirmationMessage.value !== null) return
  if (pickerKind.value === 'broker') form.value.brokerAssigned = no; else form.value.forwarderAssigned = no
  void closeParentPicker()
}
async function closeParentPicker(): Promise<void> {
  if (writing.value || confirmationMessage.value !== null || !pickerKind.value) return
  ++pickerEpoch; pickerKind.value = null; pickerReady.value = false; pickerLoading.value = false
  await nextTick(); if (previousPickerFocus?.isConnected && !previousPickerFocus.matches(':disabled')) previousPickerFocus.focus(); else await restoreFocus()
}
async function closeForm(): Promise<void> {
  if (!formMode.value || writing.value || formLoading.value || closing.value || pickerKind.value || confirmationMessage.value !== null) return
  closing.value = true
  try {
    if (dirty.value && !await requestConfirmation('尚有未儲存的修改，確定放棄？')) return
    formMode.value = null; formReady.value = false; ++formEpoch
  } finally { closing.value = false; await restoreFocus() }
}
async function closePage(): Promise<void> {
  if (pageBlocked.value) return
  ++listEpoch; ++formEpoch; ++pickerEpoch; customers.value = []; selectedCustomerNos.value = []; rowsReady.value = false
  emit('close')
}
function onCustomerNoBlur(): void { if (formMode.value !== 'edit') form.value.customerNo = form.value.customerNo.toUpperCase() }
function currentModal(): HTMLElement | null { return pickerKind.value ? pickerElement.value : formMode.value ? formElement.value : null }
async function restorePageFocus(previous: HTMLElement | null): Promise<void> {
  await nextTick(); if (disposed || modalOpen.value || confirmationMessage.value !== null) return
  if (previous?.isConnected && !previous.matches(':disabled') && !previous.closest('[inert]')) previous.focus()
  else (pageElement.value?.querySelector<HTMLElement>('.customer-main button:not(:disabled),.customer-main input:not(:disabled)') ?? pageElement.value)?.focus()
}
async function restoreFocus(): Promise<void> {
  await nextTick(); if (disposed || confirmationMessage.value !== null) return
  const modal = currentModal()
  if (modal) (modal.querySelector<HTMLElement>('input:not(:disabled):not([readonly]),select:not(:disabled),textarea:not(:disabled):not([readonly]),button:not(:disabled)') ?? modal).focus()
  else await restorePageFocus(previousModalFocus)
}
function trapTab(event: KeyboardEvent): void {
  const modal = currentModal(); if (!modal || confirmationMessage.value !== null) return
  const controls = Array.from(modal.querySelectorAll<HTMLElement>('input:not(:disabled),select:not(:disabled),textarea:not(:disabled),button:not(:disabled),[tabindex="0"]')).filter(element => element.offsetParent !== null && !element.closest('[inert]'))
  const first = controls[0], last = controls.at(-1)
  if (!first || !last) { event.preventDefault(); modal.focus(); return }
  if (!modal.contains(document.activeElement) || document.activeElement === modal) { event.preventDefault(); (event.shiftKey ? last : first).focus() }
  else if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus() }
  else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus() }
}
function captureTab(event: KeyboardEvent): void {
  if (event.key !== 'Tab' || !modalOpen.value || confirmationMessage.value !== null) return
  if (writing.value || formLoading.value || closing.value) { event.preventDefault(); return }
  trapTab(event)
}
onMounted(() => { window.addEventListener('keydown', captureTab, true); void queryCustomers() })
onBeforeUnmount(() => { disposed = true; ++scopeEpoch; ++listEpoch; ++formEpoch; ++pickerEpoch; window.removeEventListener('keydown', captureTab, true) })
watch(issueState, applyFilter)
watch(() => props.apiBaseUrl, () => {
  ++scopeEpoch; ++listEpoch; ++formEpoch; ++pickerEpoch
  if (writing.value) writeUncertain.value = true
  writing.value = false; loading.value = false; formLoading.value = false; formMode.value = null; pickerKind.value = null; formReady.value = false
  customers.value = []; selectedCustomerNos.value = []; rowsReady.value = false
  void queryCustomers()
})

</script>

<template>
  <div ref="pageElement" class="customer-page" tabindex="-1">
    <div class="customer-main" :inert="modalOpen || writing || confirmationMessage !== null">
    <form class="customer-query" @submit.prevent="queryCustomers">
      <fieldset class="customer-state-filter"><legend>客戶</legend>
        <label v-for="item in issueStates" :key="item.value"><input v-model="issueState" type="radio" name="customerState" :value="item.value" :disabled="pageBlocked" />{{ item.label }}</label>
      </fieldset>
      <label class="customer-search-label" for="customer-no-search">客戶編號</label>
      <input id="customer-no-search" v-model="keyword" type="search" :disabled="pageBlocked" /><span title="字元依字面查詢，不作萬用比對">字面包含</span>
      <button type="submit" :disabled="pageBlocked">查詢</button>
    </form>
    <div class="customer-note" role="status">匯入服務尚未提供；核准僅支援無簽核流程的客戶，需流程時會保留原資料。</div>
    <p v-if="writeUncertain" class="customer-message error" role="alert">先前寫入結果尚未確認，本頁暫停寫入；請先查詢核對，關閉後重新進入再操作。</p>
    <p v-if="readWarning" class="customer-message error" role="status">{{ readWarning }}</p>
    <div v-if="errorMessage" class="customer-message error" role="alert">{{ errorMessage }}</div>
    <div v-if="successMessage" class="customer-message success" role="status">{{ successMessage }}</div>

    <section class="customer-panel">
      <div class="customer-list-layout">
        <div class="customer-table-scroll">
          <table class="customer-table">
            <thead><tr>
              <th class="select-column"><input type="checkbox" aria-label="選取目前清單全部資料" :checked="visibleCustomers.length > 0 && visibleCustomers.every(row => selectedCustomerNos.includes(row.customerNo))" :indeterminate="selectedCustomers.length > 0 && selectedCustomers.length < visibleCustomers.length" :disabled="pageBlocked || !rowsReady" @change="toggleAllVisible(($event.target as HTMLInputElement).checked)" /></th>
              <th>客戶編號</th><th>客戶名稱</th><th>客戶簡稱</th><th>客戶英文名稱</th><th>負責人</th><th>電話</th><th>國家</th><th>傳真</th><th>WWW</th><th>統一編號</th><th>地址</th><th>地址2</th><th>說明</th><th>建立人</th><th>建立日期</th><th>狀態</th><th>幣別</th><th>發票類型</th><th>發票地址</th><th>發票地址2</th><th>發票客戶編號</th><th>發票備註</th><th>客戶類別</th><th>應收類別</th><th>運費條件</th><th>付款條件</th><th>帳號</th><th>交貨條件</th><th>指定貨運行</th><th>指定報關行</th><th>保險</th><th>修改人</th><th>修改日期</th>
            </tr></thead>
            <tbody>
              <tr v-for="row in visibleCustomers" :key="row.customerNo" tabindex="0" :aria-selected="selectedCustomerNos.includes(row.customerNo)" :class="{ selected: selectedCustomerNos.includes(row.customerNo) }" @click="selectRow(row, $event)" @keydown.enter.prevent="selectRow(row, $event)" @keydown.space.prevent="toggleSelection(row, !selectedCustomerNos.includes(row.customerNo))">
                <td class="select-column" @click.stop><input type="checkbox" :aria-label="`選取 ${row.customerNo}`" :checked="selectedCustomerNos.includes(row.customerNo)" :disabled="pageBlocked || !rowsReady" @change="toggleSelection(row, ($event.target as HTMLInputElement).checked)" /></td>
                <td class="customer-code">{{ row.customerNo }}</td><td>{{ row.customerName }}</td><td>{{ row.customerSName }}</td><td>{{ row.customerEName }}</td><td>{{ row.director }}</td><td>{{ row.telNo }}</td><td>{{ row.country }}</td><td>{{ row.faxNo }}</td><td>{{ row.www }}</td><td>{{ row.taxCode }}</td><td>{{ row.address }}</td><td>{{ row.address2 }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ valueName(statusOptions, row.status) }}</td><td>{{ row.currency }}</td><td>{{ valueName(invoiceTypeOptions, row.invTypeNo) }}</td><td>{{ row.invAddr }}</td><td>{{ row.invAddr2 }}</td><td>{{ row.invCustNo }}</td><td>{{ row.invRemark }}</td><td>{{ valueName(customerTypeOptions, row.custType) }}</td><td>{{ valueName(arTypeOptions, row.arType) }}</td><td>{{ row.freightTerms }}</td><td>{{ row.paymentTerms }}</td><td>{{ row.acctNo }}</td><td>{{ row.deliveryTerm }}</td><td>{{ row.forwarderAssigned }}</td><td>{{ row.brokerAssigned }}</td><td>{{ row.insuranceCoverage }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td>
              </tr>
              <tr v-if="!loading && !visibleCustomers.length"><td colspan="34" class="customer-empty">{{ !rowsReady ? '客戶資料尚未取得；請查詢真實來源。' : customers.length ? '查無符合條件的客戶資料。' : '目前沒有客戶資料。' }}</td></tr>
              <tr v-if="loading"><td colspan="34" class="customer-empty">資料載入中…</td></tr>
            </tbody>
          </table>
        </div>
        <aside class="customer-actions" aria-label="客戶資料操作">
          <button class="primary" type="button" :disabled="mutationBlocked" @click="openForm('new')">新增(A)</button>
          <button type="button" :disabled="mutationBlocked || !rowsReady || selectedCustomers.length !== 1" @click="openForm('edit')">修改(E)</button>
          <button type="button" :disabled="mutationBlocked || !rowsReady || selectedCustomers.length !== 1" @click="openForm('copy')">複製(C)</button>
          <button type="button" :disabled="mutationBlocked || !rowsReady || !selectedCustomers.length || selectedCustomers.some(row => row.issueState !== 0)" @click="approveSelected">核准</button>
          <button type="button" :disabled="mutationBlocked || !rowsReady || !selectedCustomers.length || selectedCustomers.some(row => ![0, 2, -1].includes(row.issueState as number))" @click="deleteSelected">刪除(D)</button>
          <button type="button" disabled title="原生匯入檢核及範本服務尚未提供">匯入</button>
          <span class="customer-state">{{ selectedCustomers.length ? `${selectedCustomers.length} 筆已選取` : stateName(issueState) }}</span>
          <button type="button" :disabled="pageBlocked" @click="closePage">關閉</button>
        </aside>
      </div>
      <footer class="customer-count">資料總筆數 <strong>{{ visibleCustomers.length }}</strong></footer>
    </section>

    </div>
    <div v-if="formMode" class="customer-overlay" @click.self="closeForm" :inert="pickerKind !== null || confirmationMessage !== null">
      <form ref="formElement" novalidate class="customer-dialog" role="dialog" aria-modal="true" aria-label="客戶基本資料編輯" tabindex="-1" @keydown.esc.stop.prevent="closeForm" @keydown.tab="trapTab" @submit.prevent="saveCustomer(true)">
        <header><strong>客戶基本資料 ({{ formMode === 'edit' ? 'EDIT' : formMode === 'copy' ? 'COPY' : 'ADD' }})</strong><button type="button" aria-label="關閉" :disabled="writing || formLoading || closing || confirmationMessage !== null" @click="closeForm">×</button></header>
        <div class="customer-form-scroll">
          <p v-if="formLoading" class="import-hint" role="status">正在讀取目前伺服器資料…</p>
          <fieldset :disabled="formBusy || !formReady || writeUncertain"><legend>客戶主檔資料</legend><div class="customer-fields">
            <label><span>客戶編號</span><input v-model="form.customerNo" required maxlength="100" :readonly="formMode === 'edit'" @blur="onCustomerNoBlur" /></label>
            <label><span>客戶名稱</span><input v-model="form.customerName" required maxlength="200" :readonly="lockBasicFields" /></label>
            <label><span>客戶簡稱</span><input v-model="form.customerSName" required maxlength="200" :readonly="lockBasicFields" /></label>
            <label><span>客戶英文名稱</span><input v-model="form.customerEName" maxlength="100" /></label>
            <label><span>電話</span><input v-model="form.telNo" maxlength="50" :readonly="lockBasicFields" /></label>
            <label><span>傳真</span><input v-model="form.faxNo" maxlength="50" :readonly="lockBasicFields" /></label>
            <label><span>WWW</span><input v-model="form.www" maxlength="100" :readonly="lockBasicFields" /></label>
            <label><span>統一編號</span><input v-model="form.taxCode" maxlength="50" :readonly="lockBasicFields" /></label>
            <label><span>負責人</span><input v-model="form.director" maxlength="100" :readonly="lockBasicFields" /></label>
            <label><span>國家</span><input v-model="form.country" maxlength="100" :readonly="lockBasicFields" /></label>
            <label><span>地址</span><input v-model="form.address" maxlength="510" :readonly="lockBasicFields" /></label>
            <label><span>地址2</span><input v-model="form.address2" maxlength="510" :readonly="lockBasicFields" /></label>
            <label class="wide"><span>說明</span><textarea v-model="form.description" maxlength="510" rows="2" :readonly="lockBasicFields" /></label>
          </div></fieldset>
          <fieldset :disabled="formBusy || !formReady || writeUncertain"><legend>交易與發票資料</legend><div class="customer-fields">
            <label><span>狀態</span><select v-model="form.status"><option :value="null">未設定</option><option v-if="legacyOption(statusOptions, form.status)" :value="form.status">原值：{{ form.status }}</option><option v-for="item in statusOptions" :key="item.value" :value="item.value">{{ item.label }}</option></select></label>
            <label><span>幣別</span><input v-model="form.currency" maxlength="12" /></label>
            <label><span>發票類型</span><select v-model="form.invTypeNo"><option :value="null">未設定</option><option v-if="legacyOption(invoiceTypeOptions, form.invTypeNo)" :value="form.invTypeNo">原值：{{ form.invTypeNo }}</option><option v-for="item in invoiceTypeOptions" :key="item.value" :value="item.value">{{ item.label }}</option></select></label>
            <label><span>客戶類別</span><select v-model="form.custType"><option :value="null">未設定</option><option v-if="legacyOption(customerTypeOptions, form.custType)" :value="form.custType">原值：{{ form.custType }}</option><option v-for="item in customerTypeOptions" :key="item.value" :value="item.value">{{ item.label }}</option></select></label>
            <label><span>應收類別</span><select v-model="form.arType"><option :value="null">未設定</option><option v-if="legacyOption(arTypeOptions, form.arType)" :value="form.arType">原值：{{ form.arType }}</option><option v-for="item in arTypeOptions" :key="item.value" :value="item.value">{{ item.label }}</option></select></label>
            <label><span>發票客戶編號</span><input v-model="form.invCustNo" maxlength="40" /></label>
            <label><span>運費條件</span><input v-model="form.freightTerms" maxlength="40" /></label>
            <label><span>付款條件</span><input v-model="form.paymentTerms" maxlength="40" /></label>
            <label><span>帳號</span><input v-model="form.acctNo" maxlength="100" /></label>
            <label><span>交貨條件</span><input v-model="form.deliveryTerm" maxlength="100" /></label>
            <label><span>指定貨運行</span><div class="customer-field-with-picker"><input v-model="form.forwarderAssigned" maxlength="100" /><button type="button" @click="openParentPicker('forwarder')" aria-label="查找指定貨運行">查找</button></div></label>
            <label><span>指定報關行</span><div class="customer-field-with-picker"><input v-model="form.brokerAssigned" maxlength="100" /><button type="button" @click="openParentPicker('broker')" aria-label="查找指定報關行">查找</button></div></label>
            <label><span>保險</span><input v-model="form.insuranceCoverage" maxlength="100" /></label>
            <label><span>發票地址</span><input v-model="form.invAddr" maxlength="510" /></label>
            <label><span>發票地址2</span><input v-model="form.invAddr2" maxlength="510" /></label>
            <label class="wide"><span>發票備註</span><input v-model="form.invRemark" maxlength="200" /></label>
          </div></fieldset>
          <div class="customer-audit"><label>建立人<input :value="audit.creator" readonly /></label><label>建立日期<input :value="audit.createDate" readonly /></label><label>修改人<input :value="audit.reviser" readonly /></label><label>修改日期<input :value="audit.reviseDate" readonly /></label></div>
          <p v-if="lockBasicFields" class="locked-note">目前為 {{ stateName(originalRow?.issueState ?? null) }}；原生基本身份／聯絡欄位鎖定；英文名稱、交易及發票資料仍可修改。</p>
          <p v-if="formError" class="customer-message error" role="alert">{{ formError }}</p>
          <p v-if="successMessage" class="customer-message success" role="status">{{ successMessage }}</p>
        </div>
        <footer class="customer-dialog-actions"><button class="primary" type="submit" :disabled="formBusy || !formReady || writeUncertain">確定</button><button type="button" :disabled="writing || formLoading || closing || confirmationMessage !== null" @click="closeForm">關閉</button><button type="button" :disabled="formBusy || !formReady || writeUncertain || !dirty" @click="saveCustomer(false)">套用</button></footer>
      </form>
    </div>

    <div v-if="pickerKind" class="customer-overlay customer-picker-overlay" @click.self="closeParentPicker" :inert="confirmationMessage !== null">
      <section ref="pickerElement" class="customer-dialog" role="dialog" aria-modal="true" :aria-label="pickerKind === 'broker' ? '選擇報關行' : '選擇貨運行'" tabindex="-1" @keydown.esc.stop.prevent="closeParentPicker" @keydown.tab="trapTab">
        <header><strong>{{ pickerKind === 'broker' ? '選擇報關行' : '選擇貨運行' }}</strong><button type="button" aria-label="關閉查找" :disabled="writing || confirmationMessage !== null" @click="closeParentPicker">×</button></header>
        <div class="customer-picker-body">
          <label>編號查找 <input v-model="pickerKeyword" :disabled="pickerLoading" /></label>
          <p class="import-hint">顯示真實來源全部狀態；未變動的舊指定值可保留。</p>
          <p v-if="pickerLoading" role="status">查找資料讀取中…</p><p v-if="pickerError" class="customer-message error" role="alert">{{ pickerError }}</p>
          <table class="customer-table"><thead><tr><th>編號</th><th>連絡人</th><th>狀態</th><th>操作</th></tr></thead><tbody>
            <tr v-for="row in pickerRows" :key="row.no"><td>{{ row.no }}</td><td>{{ row.name }}</td><td>{{ stateName(row.state) }}</td><td><button type="button" :disabled="pickerLoading || !pickerReady" @click="chooseParent(row.no)">選取</button></td></tr>
            <tr v-if="pickerReady && !pickerRows.length"><td colspan="4" class="customer-empty">沒有符合條件的真實資料。</td></tr>
          </tbody></table>
        </div><footer class="customer-dialog-actions"><button type="button" @click="closeParentPicker">關閉</button></footer>
      </section>
    </div>
    <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
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

<style scoped>
.customer-main{display:grid;gap:8px;min-width:0}.customer-picker-overlay{z-index:1100}.customer-picker-overlay .customer-dialog{width:min(760px,100%)}.customer-picker-body{padding:12px;overflow:auto}.customer-field-with-picker{display:flex;min-width:0;gap:5px}.customer-field-with-picker input{min-width:0;flex:1}.customer-field-with-picker button{border:1px solid #c4c4c4;background:#fff;color:inherit}
</style>
