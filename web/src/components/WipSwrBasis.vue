<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, reactive, ref, watch } from 'vue'
import WipSwrBasisDevPreview from './WipSwrBasisDevPreview.vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'
import { createWp109ReadService, type FilterKey, type LookupOption, type SwrDto, type SwrItem, type SwrRow } from '../services/wp109'
import { createWp109WriteService, validateSwrFields, SwrWriteError, type SwrFields, type SwrVersionedItem, type SwrWriterOptions, type SwrNumberPreview, type SwrDevice, type EditAttachmentAction, type NewAttachmentAction } from '../services/wp109-write'

const props = defineProps<{ apiBaseUrl?: string; developmentPreview?: boolean; currentUserNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const statusLabels: Record<number,string> = { 0:'UNFROZEN',1:'PENDING',2:'ACTIVE',99:'UNUSED' }
const typeLabels: Record<number,string> = { 0:'作業站指示',1:'包裝作業說明' }
const queryFields: { key: FilterKey; label: string; lookup?: boolean; multi?: boolean }[] = [
  {key:'swrType',label:'SWRType'}, {key:'customerNo',label:'CustomerNo',lookup:true},
  {key:'lotNo',label:'LotNo',multi:true}, {key:'deviceNo',label:'DeviceNo',lookup:true},
  {key:'opNo',label:'OPNo',lookup:true,multi:true}, {key:'customerLotNo',label:'CustomerLotNo'}, {key:'moNo',label:'MONo',lookup:true},
]
const columns = ['SWRNO','SWRTYPE','CUSTOMERNO','DEVICENO','OPNO','LOTNO','MONO','STATUS','STARTTIME','ENDTIME','CUSTOMERLOTNO','CREATOR','CREATEDATE','HaveAttachFile']
const flags = reactive<Record<FilterKey,boolean>>({ swrType:false,customerNo:false,lotNo:false,deviceNo:false,opNo:false,customerLotNo:false,moNo:false })
const filters = reactive<Record<FilterKey,string>>({ swrType:'',customerNo:'',lotNo:'',deviceNo:'',opNo:'',customerLotNo:'',moNo:'' })
const pickedOperationNos = ref<string[] | null>(null)
const status = ref(0), limitText = ref('200'), rows = ref<SwrRow[]>([]), selectedNos = ref<string[]>([]), activeNo = ref<string | null>(null)
const active = ref<SwrItem | null>(null), phase = ref<'initial'|'loading'|'ready'|'empty'|'error'>('initial')
const queryBusy = ref(false), itemBusy = ref(false), hasMore = ref(false), error = ref(''), message = ref('請先Query。')
const root = ref<HTMLElement | null>(null), pageBody = ref<HTMLElement | null>(null), lookupDialog = ref<HTMLElement | null>(null)
const lookup = ref<'customerNo'|'opNo'|'deviceNo'|null>(null), lookupKeyword = ref(''), lookupOptions = ref<LookupOption[]>([]), lookupSelected = ref<string[]>([])
const lookupContext = ref<'query'|'editor'>('query'), deviceOptions = ref<SwrDevice[]>([]), devicesHaveMore = ref(false)
const lookupBusy = ref(false), lookupError = ref('')
type Mode = ''|'add'|'edit'|'copy'
const mode = ref<Mode>(''), editor = ref<HTMLElement|null>(null), reasonDialog = ref<HTMLElement|null>(null)
const queryButton = ref<HTMLButtonElement|null>(null), formFile = ref<HTMLInputElement|null>(null)
const draft = ref<SwrFields>(blank()), schedule = ref(false), sourceItem = ref<SwrVersionedItem|null>(null), preview = ref<SwrNumberPreview|null>(null)
const writerOptions = ref<SwrWriterOptions|null>(null), formLoading = ref(false), formReady = ref(false), formError = ref('')
const attachmentAction = ref<NewAttachmentAction|EditAttachmentAction>('none'), selectedFile = ref<File|null>(null), fileReading = ref(false)
const writing = ref(false), preparing = ref(false), uncertain = ref(false), reasonOpen = ref(false), abortReason = ref('')
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()
let formEpoch = 0, batchEpoch = 0, fileEpoch = 0, writeEpoch = 0, disposed = false, originalDraft = '', formKey: string|null = null, formApiBase = ''
let formAbort: AbortController|null = null, writeAbort: AbortController|null = null, batchAbort: AbortController|null = null
let formTrigger: HTMLElement|null = null, reasonSettle: ((value:string|null)=>void)|undefined
let queryEpoch = 0, itemEpoch = 0, lookupEpoch = 0
let queryAbort: AbortController | null = null, itemAbort: AbortController | null = null, lookupAbort: AbortController | null = null
let lookupTrigger: HTMLElement | null = null
const busy = computed(() => queryBusy.value || itemBusy.value || writing.value || preparing.value)
const service = () => createWp109ReadService(props.apiBaseUrl || '', window.location.origin)
const writer = () => createWp109WriteService(props.apiBaseUrl || '', window.location.origin)
const pageLocked = computed(() => mode.value !== '' || lookup.value !== null || reasonOpen.value || writing.value || preparing.value || confirmationMessage.value !== null)
const commandBlocked = computed(() => busy.value || mode.value !== '' || lookup.value !== null || reasonOpen.value || confirmationMessage.value !== null || uncertain.value)
const canEdit = computed(() => !commandBlocked.value && itemReady.value && selectedNos.value.length === 1 && active.value!.item.status === 0 && active.value!.details.length === 0)
const canCopy = computed(() => !commandBlocked.value && itemReady.value && selectedNos.value.length === 1 && active.value!.details.length === 0)
const selectedNonempty = computed(() => selectedNos.value.length === 1 && itemReady.value && active.value!.details.length > 0)
const canApprove = computed(() => !commandBlocked.value && !selectedNonempty.value && selectedNos.value.length > 0 && status.value === 0)
const canVoid = computed(() => !commandBlocked.value && !selectedNonempty.value && selectedNos.value.length > 0 && [0,2,99].includes(status.value))
const title = computed(() => mode.value === 'edit' ? '修改特殊生產需求' : mode.value === 'copy' ? '複製特殊生產需求' : '新增特殊生產需求')
const dirty = computed(() => draftFingerprint() !== originalDraft)
const itemReady = computed(() => !itemBusy.value && active.value !== null && active.value.item.swrNo === activeNo.value && selectedNos.value.includes(active.value.item.swrNo))
const canDownload = computed(() => itemReady.value && selectedNos.value.length === 1 && active.value!.item.attachment.downloadAvailable)
const attachmentHref = computed(() => {
  if (!canDownload.value || activeNo.value === null) return ''
  try { return service().attachmentUrl(activeNo.value) } catch { return '' }
})
const emptyText = computed(() => phase.value === 'loading' ? '查詢中…' : phase.value === 'error' ? '查詢未完成。' : phase.value === 'empty' ? '沒有符合條件的SWR。' : '請先Query。')
const description = computed(() => itemReady.value ? active.value!.item.swrDesc : '')
const detailLots = computed(() => itemReady.value ? active.value!.details.map(row => row.lotNo).join('\n') : '')
function blank(): SwrFields { return { swrType:0,customerNo:'',opNo:'',deviceNo:null,startTime:null,endTime:null,customerLotNo:null,moNo:null,swrDesc:'',lotNos:[] } }
function draftFingerprint(): string { return JSON.stringify({ fields:draft.value,schedule:schedule.value,action:attachmentAction.value,file:selectedFile.value ? fileEpoch : null }) }

function cancelReads(): void {
  queryEpoch++; itemEpoch++; lookupEpoch++
  queryAbort?.abort(); itemAbort?.abort(); lookupAbort?.abort()
  queryBusy.value = false; itemBusy.value = false; lookupBusy.value = false
}
function clearSelection(): void { itemEpoch++; itemAbort?.abort(); itemBusy.value = false; selectedNos.value = []; activeNo.value = null; active.value = null }
function reset(): void {
  cancelReads(); clearSelection(); formEpoch++; batchEpoch++; fileEpoch++; writeEpoch++
  formAbort?.abort(); batchAbort?.abort(); writeAbort?.abort(); resolveConfirmation(false); resolveReason(null)
  mode.value = ''; sourceItem.value = null; formKey = null; formReady.value = false; formLoading.value = false
  selectedFile.value = null; fileReading.value = false; writing.value = false; preparing.value = false; uncertain.value = false
  rows.value = []; hasMore.value = false; phase.value = 'initial'; error.value = ''; message.value = '請先Query。'; lookup.value = null
}
function failure(value: unknown): string { return value instanceof Error ? value.message : '讀取未完成，請重新查詢。' }
async function query(): Promise<void> {
  if (dev.value || pageLocked.value || queryBusy.value) return
  const wasUncertain = uncertain.value, context = props.apiBaseUrl || '', owner = queryEpoch+1
  const target = document.activeElement instanceof HTMLElement ? document.activeElement : null
  if (await runQuery()) {
    uncertain.value = false
    if (wasUncertain) message.value += ' 請核對上次操作的實際結果後再繼續。'
  }
  if (!disposed && context === (props.apiBaseUrl || '') && owner === queryEpoch && !usable(document.activeElement instanceof HTMLElement ? document.activeElement : null)) await restoreFocus(target)
}
async function runQuery(): Promise<boolean> {
  if (disposed || dev.value) return false
  const context = props.apiBaseUrl || ''
  const epoch = ++queryEpoch; queryAbort?.abort(); queryAbort = new AbortController(); const signal = queryAbort.signal
  clearSelection(); rows.value = []; hasMore.value = false; error.value = ''; queryBusy.value = true; phase.value = 'loading'; message.value = '查詢中…'
  try {
    if (!/^\d+$/.test(limitText.value)) throw new Error('查詢筆數請輸入1至500。')
    const result = await service().query({ enabled:{...flags},values:{...filters},status:status.value,limit:Number(limitText.value),
      ...(pickedOperationNos.value === null ? {} : {opNos:[...pickedOperationNos.value]}) }, signal)
    if (signal.aborted || epoch !== queryEpoch || dev.value || context !== (props.apiBaseUrl || '')) return false
    rows.value = result.items; hasMore.value = result.hasMore; phase.value = result.items.length ? 'ready' : 'empty'
    message.value = `查得${result.items.length}列／${new Set(result.items.map(row => row.item.swrNo)).size}張SWR。`
    return true
  } catch (value) {
    if (signal.aborted || epoch !== queryEpoch || context !== (props.apiBaseUrl || '')) return false
    phase.value = 'error'; error.value = failure(value); message.value = '查詢未完成。'
    return false
  } finally { if (epoch === queryEpoch) queryBusy.value = false }
}
async function readItem(rawNo: string): Promise<void> {
  const context = props.apiBaseUrl || ''
  const epoch = ++itemEpoch; itemAbort?.abort(); itemAbort = new AbortController(); const signal = itemAbort.signal
  active.value = null; itemBusy.value = true; error.value = ''
  try {
    const result = await service().item(rawNo, signal)
    if (signal.aborted || epoch !== itemEpoch || dev.value || context !== (props.apiBaseUrl || '') || activeNo.value !== rawNo || !selectedNos.value.includes(rawNo)) return
    active.value = result
  } catch (value) {
    if (signal.aborted || epoch !== itemEpoch || context !== (props.apiBaseUrl || '')) return
    error.value = failure(value); message.value = '單張內容讀取未完成；請重新選取或Query。'
  } finally { if (epoch === itemEpoch) itemBusy.value = false }
}
function selectRow(rawNo: string, multi: boolean): void {
  if (dev.value || pageLocked.value || queryBusy.value) return
  const values = multi ? selectedNos.value.includes(rawNo) ? selectedNos.value.filter(no => no !== rawNo) : [...selectedNos.value, rawNo] : [rawNo]
  if (!values.length) { clearSelection(); return }
  selectedNos.value = values; activeNo.value = values.includes(rawNo) ? rawNo : values[values.length - 1]
  void readItem(activeNo.value)
}
function cell(row: SwrRow, index: number): string {
  const item = itemReady.value && active.value!.item.swrNo === row.item.swrNo ? active.value!.item : row.item
  const values = [item.swrNo,typeLabels[item.swrType] ?? String(item.swrType),item.customerNo,item.deviceNo,item.opNo,row.lotNo,item.moNo,
    statusLabels[item.status] ?? String(item.status),item.startTime,item.endTime,item.customerLotNo,item.creator,item.createDate,item.attachment.sourceHaveAttachFile]
  return values[index] === null ? '' : String(values[index])
}
function changeStatus(value: number): void { if (dev.value || busy.value || pageLocked.value) return; status.value = value; void query() }
function downloadStarted(): void { if (!canDownload.value) return; message.value = '下載請求已交由瀏覽器處理；尚未確認完成。' }
function closePage(): void { if (pageLocked.value) return; reset(); emit('close') }

async function openLookup(key: FilterKey, event: Event, context: 'query'|'editor' = 'query'): Promise<void> {
  if (dev.value || busy.value || lookup.value || confirmationMessage.value !== null || !['customerNo','opNo','deviceNo'].includes(key)
    || context === 'query' && mode.value !== '' || context === 'editor' && (!formCurrent(formEpoch) || !formReady.value || uncertain.value)) return
  lookupTrigger = event.currentTarget instanceof HTMLElement ? event.currentTarget : null
  lookupContext.value = context; lookup.value = key as 'customerNo'|'opNo'|'deviceNo'
  lookupKeyword.value = ''; lookupError.value = ''; lookupOptions.value = []; deviceOptions.value = []; devicesHaveMore.value = false
  lookupSelected.value = context === 'query' && key === 'opNo' && flags.opNo ? (pickedOperationNos.value === null ? filters.opNo.split(/\r?\n/) : [...pickedOperationNos.value]).filter(value => !/^ *$/.test(value)) : []
  await nextTick(); if (!lookup.value || disposed) return
  lookupDialog.value?.focus(); await lookupSearch()
}
async function lookupSearch(): Promise<void> {
  const kind = lookup.value; if (!kind || dev.value || lookupBusy.value) return
  const context = props.apiBaseUrl || ''
  const target = document.activeElement instanceof HTMLElement ? document.activeElement : null
  const epoch = ++lookupEpoch; lookupAbort?.abort(); lookupAbort = new AbortController(); const signal = lookupAbort.signal
  lookupBusy.value = true; lookupError.value = ''; lookupOptions.value = []; deviceOptions.value = []; devicesHaveMore.value = false
  try {
    if (kind === 'deviceNo') {
      const result = await writer().devices(lookupKeyword.value,signal)
      if (signal.aborted || epoch !== lookupEpoch || lookup.value !== kind || dev.value || context !== (props.apiBaseUrl || '')) return
      deviceOptions.value = result.items; devicesHaveMore.value = result.hasMore
    } else {
      const result = kind === 'customerNo' ? await service().customers(lookupKeyword.value, signal) : await service().operations(lookupKeyword.value, signal)
      if (signal.aborted || epoch !== lookupEpoch || lookup.value !== kind || dev.value || context !== (props.apiBaseUrl || '')) return
      lookupOptions.value = result
    }
  } catch (value) { if (!signal.aborted && epoch === lookupEpoch && context === (props.apiBaseUrl || '')) lookupError.value = failure(value) }
  finally { if (epoch === lookupEpoch) { lookupBusy.value = false; await focusModal(lookupDialog.value,target,() => epoch === lookupEpoch && lookup.value === kind && context === (props.apiBaseUrl || '')) } }
}
function chooseCustomer(value: string): void {
  if (lookup.value !== 'customerNo' || lookupBusy.value || !lookupOptions.value.some(row => row.key === value)) return
  if (lookupContext.value === 'editor') draft.value.customerNo = value
  else { filters.customerNo = value; flags.customerNo = true }
  closeLookup()
}
function toggleOperation(value: string): void {
  if (lookup.value !== 'opNo' || lookupBusy.value || !lookupOptions.value.some(row => row.key === value)) return
  if (lookupContext.value === 'editor') { draft.value.opNo = value; closeLookup(); return }
  lookupSelected.value = lookupSelected.value.includes(value) ? lookupSelected.value.filter(v => v !== value) : [...lookupSelected.value, value]
}
function confirmOperations(): void {
  if (lookupContext.value !== 'query' || lookup.value !== 'opNo' || lookupBusy.value || !lookupSelected.value.length) return
  pickedOperationNos.value = [...lookupSelected.value]
  filters.opNo = lookupSelected.value.join('\n'); flags.opNo = true; closeLookup()
}
function chooseDevice(index: number): void {
  if (lookup.value !== 'deviceNo' || lookupBusy.value || !deviceOptions.value[index]) return
  const value = deviceOptions.value[index].deviceNo
  if (lookupContext.value === 'editor') draft.value.deviceNo = value
  else {
    if (value === null || /^ *$/.test(value)) { lookupError.value = '所選產品沒有DeviceNo，請另選資料。'; return }
    filters.deviceNo = value; flags.deviceNo = true
  }
  closeLookup()
}
function manualFilterInput(key: FilterKey): void { if (key === 'opNo') pickedOperationNos.value = null }
function closeLookup(): void {
  lookupEpoch++; lookupAbort?.abort(); lookupBusy.value = false; lookup.value = null
  const trigger = lookupTrigger; lookupTrigger = null
  void restoreFocus(trigger)
}
function usable(node: HTMLElement|null): node is HTMLElement {
  return !!node?.isConnected && node.matches('button,input:not([type=hidden]),select,textarea,a[href],[tabindex],[contenteditable=true]') && !node.matches(':disabled') && !node.closest('[inert],[hidden]') && node.getClientRects().length > 0
}
function controls(shell: HTMLElement): HTMLElement[] {
  return [...shell.querySelectorAll<HTMLElement>('button,input:not([type=hidden]),select,textarea,a[href],[tabindex="0"]')].filter(usable)
}
async function focusModal(shell: HTMLElement|null, target: HTMLElement|null, current: ()=>boolean): Promise<void> {
  await nextTick()
  if (disposed || !current() || confirmationMessage.value !== null || !shell?.isConnected || shell.closest('[inert]')) return
  const focused = document.activeElement
  if (focused instanceof HTMLElement && shell.contains(focused) && focused !== shell && usable(focused)) return
  if (target && shell.contains(target) && usable(target)) target.focus()
  else {
    const first = shell.querySelector<HTMLElement>('[data-focus]')
    ;(usable(first) ? first : controls(shell)[0] || shell).focus()
  }
}
async function restoreFocus(target: HTMLElement|null): Promise<void> {
  await nextTick()
  if (disposed || confirmationMessage.value !== null || writing.value || preparing.value) return
  if (lookup.value) { await focusModal(lookupDialog.value,target,()=>lookup.value !== null); return }
  if (mode.value) { await focusModal(editor.value,target,()=>mode.value !== ''); return }
  if (usable(target)) target.focus()
  else if (usable(queryButton.value)) queryButton.value.focus()
  else root.value?.focus()
}
function trap(event: KeyboardEvent): void {
  if (confirmationMessage.value !== null) return
  const shell = lookup.value ? lookupDialog.value : reasonOpen.value ? reasonDialog.value : mode.value ? editor.value : null
  if (!shell) return
  if (event.key === 'Escape') {
    event.preventDefault(); event.stopPropagation()
    if (writing.value || fileReading.value) return
    if (lookup.value) closeLookup()
    else if (reasonOpen.value) resolveReason(null)
    else void closeForm()
    return
  }
  if (event.key !== 'Tab') return
  const nodes = controls(shell); if (!nodes.length) { event.preventDefault(); shell.focus(); return }
  const first = nodes[0], last = nodes[nodes.length - 1], activeElement = document.activeElement
  if (!nodes.includes(activeElement as HTMLElement) || event.shiftKey && activeElement === first || !event.shiftKey && activeElement === last) {
    event.preventDefault(); (event.shiftKey ? last : first).focus()
  }
}

function formCurrent(epoch: number): boolean { return !disposed && !dev.value && epoch === formEpoch && mode.value !== '' && formApiBase === (props.apiBaseUrl || '') }
function fromItem(value: SwrVersionedItem): SwrFields {
  const r = value.item
  return { swrType:r.swrType,customerNo:r.customerNo,opNo:r.opNo,deviceNo:r.deviceNo,startTime:r.startTime,endTime:r.endTime,
    customerLotNo:r.customerLotNo,moNo:r.moNo,swrDesc:r.swrDesc,lotNos:[] }
}
async function openForm(next: Exclude<Mode,''>): Promise<void> {
  if (dev.value || commandBlocked.value || next === 'edit' && !canEdit.value || next === 'copy' && !canCopy.value) return
  formKey = next === 'add' ? null : active.value!.item.swrNo
  formApiBase = props.apiBaseUrl || ''
  formTrigger = document.activeElement instanceof HTMLElement ? document.activeElement : null
  mode.value = next; draft.value = blank(); sourceItem.value = null; preview.value = null; writerOptions.value = null
  schedule.value = false; attachmentAction.value = next === 'edit' ? 'keep' : 'none'; selectedFile.value = null; fileReading.value = false
  formReady.value = false; formError.value = ''; originalDraft = draftFingerprint(); error.value = ''
  const owner = ++formEpoch
  await nextTick(); if (!formCurrent(owner)) return
  editor.value?.focus(); await loadForm(owner)
}
async function loadForm(owner = formEpoch): Promise<void> {
  if (!formCurrent(owner) || writing.value || formLoading.value) return
  const currentMode = mode.value, rawKey = formKey
  formAbort?.abort(); formAbort = new AbortController(); const signal = formAbort.signal
  formLoading.value = true; formReady.value = false; formError.value = ''
  try {
    const api = writer()
    const [options, item, number] = await Promise.all([
      api.options(signal),rawKey === null ? Promise.resolve(null) : api.item(rawKey,signal),
      currentMode === 'edit' ? Promise.resolve(null) : api.preview(signal),
    ])
    if (!formCurrent(owner) || signal.aborted) return
    if (item && item.details.length) throw new SwrWriteError('此單含Lot明細，目前可查詢與下載。',503)
    if (currentMode === 'edit' && item?.item.status !== 0) throw new SwrWriteError('只有UNFROZEN狀態可修改，請重新Query。',409)
    sourceItem.value = item; preview.value = number; writerOptions.value = options
    draft.value = item ? fromItem(item) : blank()
    schedule.value = draft.value.startTime !== null || draft.value.endTime !== null
    attachmentAction.value = currentMode === 'edit' ? 'keep' : 'none'; selectedFile.value = null; fileEpoch++
    originalDraft = draftFingerprint(); formReady.value = true
  } catch (value) { if (formCurrent(owner) && !signal.aborted) formError.value = failure(value) }
  finally {
    if (formCurrent(owner)) { formLoading.value = false; await focusModal(editor.value,null,()=>formCurrent(owner) && lookup.value === null) }
  }
}
function dismissForm(): void {
  formEpoch++; fileEpoch++; formAbort?.abort(); lookupEpoch++; lookupAbort?.abort(); lookup.value = null; lookupBusy.value = false
  mode.value = ''; sourceItem.value = null; formKey = null; formReady.value = false; formLoading.value = false
  selectedFile.value = null; fileReading.value = false; preview.value = null
}
async function closeForm(): Promise<void> {
  if (!mode.value || writing.value || fileReading.value || confirmationMessage.value !== null) return
  if (lookup.value) { closeLookup(); return }
  const owner = formEpoch
  if (dirty.value && !await requestConfirmation('尚有未儲存內容，要放棄變更嗎？')) return
  if (!formCurrent(owner)) return
  const target = formTrigger; dismissForm(); await restoreFocus(target)
}
async function reconcile(): Promise<void> {
  if (!uncertain.value || writing.value || preparing.value || confirmationMessage.value !== null) return
  if (mode.value && !await requestConfirmation('將關閉編輯並重新Query，請核對上次操作的實際結果。')) return
  if (disposed) return
  if (mode.value) dismissForm()
  await query(); await restoreFocus(queryButton.value)
}
function nullableInput(key: 'customerNo'|'opNo'|'deviceNo'|'customerLotNo'|'moNo'|'swrDesc', event: Event): void {
  draft.value[key] = (event.target as HTMLInputElement|HTMLTextAreaElement).value
}
// HTML datetime-local supports at most millisecond display precision. Keep the
// server's full naive timestamp in draft; repeated equivalent control values
// must not discard its original PostgreSQL microseconds.
function displayTime(value: string|null): string {
  if (value === null) return ''
  const match = /^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2})(?::(\d{2})(?:\.(\d+))?)?$/.exec(value)
  if (!match) return value
  const seconds = match[2] || '00', fraction = (match[3] || '').slice(0,3).replace(/0+$/,'')
  return match[1] + (seconds !== '00' || fraction ? ':'+seconds : '') + (fraction ? '.'+fraction : '')
}
function timeInput(key: 'startTime'|'endTime', event: Event): void {
  const next = (event.target as HTMLInputElement).value || null
  if (displayTime(next) !== displayTime(draft.value[key])) draft.value[key] = next
}
function scheduleChanged(event: Event): void {
  schedule.value = (event.target as HTMLInputElement).checked
  if (!schedule.value) { draft.value.startTime = null; draft.value.endTime = null }
}
function requestFields(): SwrFields {
  const normalize = (value:string|null):string|null => value !== null && /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/.test(value) ? value+':00' : value
  if (schedule.value && (!draft.value.startTime || !draft.value.endTime)) throw new SwrWriteError('請一起填入開始與結束時間。',400)
  return validateSwrFields({ ...draft.value,startTime:schedule.value ? normalize(draft.value.startTime) : null,
    endTime:schedule.value ? normalize(draft.value.endTime) : null,lotNos:[] })
}
function attachmentChanged(): void {
  selectedFile.value = null; fileEpoch++; fileReading.value = false
  if (formFile.value) formFile.value.value = ''
}
async function chooseFile(event: Event): Promise<void> {
  if (!mode.value || writing.value || uncertain.value || !formReady.value || attachmentAction.value !== 'replace') return
  const input = event.target as HTMLInputElement, file = input.files?.[0] || null, owner = formEpoch, generation = ++fileEpoch
  selectedFile.value = null; formError.value = ''; fileReading.value = !!file
  if (!file) return
  try {
    if (!writerOptions.value || file.size > writerOptions.value.fileSizeLimitBytes) throw new Error('附件超過目前大小上限。')
    const prefix = new Uint8Array(await file.slice(0,5).arrayBuffer())
    if (prefix.length !== 5 || !prefix.every((v,i)=>v===[37,80,68,70,45][i])) throw new Error('請選擇非空的PDF附件。')
    if (formCurrent(owner) && generation === fileEpoch) selectedFile.value = file
  } catch (value) { if (formCurrent(owner) && generation === fileEpoch) { formError.value = failure(value); input.value = '' } }
  finally { if (formCurrent(owner) && generation === fileEpoch) fileReading.value = false }
}
async function saveForm(): Promise<void> {
  if (dev.value || !mode.value || !formReady.value || formLoading.value || writing.value || fileReading.value || uncertain.value || lookup.value || confirmationMessage.value !== null) return
  let fields: SwrFields
  try { fields = requestFields() } catch (value) { formError.value = failure(value); return }
  const owner = formEpoch, operation = ++writeEpoch, currentMode = mode.value, source = sourceItem.value, target = formTrigger
  formError.value = ''; writing.value = true; writeAbort = new AbortController(); await nextTick()
  if (!formCurrent(owner) || operation !== writeEpoch) return
  editor.value?.focus()
  try {
    const api = writer()
    const result = currentMode === 'edit'
      ? await api.edit(source!.item.swrNo,fields,source!.version,attachmentAction.value as EditAttachmentAction,selectedFile.value,writeAbort.signal)
      : await api.create(fields,attachmentAction.value as NewAttachmentAction,selectedFile.value,writeAbort.signal)
    if (!formCurrent(owner) || operation !== writeEpoch) return
    const savedNo = result.item.swrNo
    dismissForm()
    const refreshed = await runQuery()
    if (disposed || operation !== writeEpoch) return
    message.value = `${currentMode === 'edit' ? '已儲存' : '已建立'} ${savedNo}。${refreshed ? '' : '清單查詢未完成，請重新Query。'}`
  } catch (value) {
    if (formCurrent(owner) && operation === writeEpoch) {
      if (value instanceof SwrWriteError && value.unknownWrite) uncertain.value = true
      formError.value = failure(value)
    }
  } finally {
    if (!disposed && operation === writeEpoch) {
      writing.value = false; writeAbort = null
      if (mode.value) await focusModal(editor.value,null,()=>formCurrent(owner) && lookup.value === null)
      else await restoreFocus(target)
    }
  }
}
function sameVisibleItem(a: SwrDto, b: SwrDto): boolean {
  const keys = ['swrNo','swrType','status','customerNo','opNo','deviceNo','startTime','endTime','customerLotNo','moNo','swrDesc','creator','createDate'] as const
  return keys.every(key=>a[key]===b[key]) && a.attachment.sourceHaveAttachFile === b.attachment.sourceHaveAttachFile
    && a.attachment.blobLength === b.attachment.blobLength && a.attachment.downloadAvailable === b.attachment.downloadAvailable
}
function resolveReason(value: string|null): void {
  reasonOpen.value = false
  const settle = reasonSettle; reasonSettle = undefined; settle?.(value)
}
async function askReason(current: ()=>boolean): Promise<string|null> {
  abortReason.value = ''; reasonOpen.value = true
  const result = new Promise<string|null>(resolve=>{ reasonSettle = resolve })
  await nextTick()
  if (!current()) resolveReason(null)
  await focusModal(reasonDialog.value,null,()=>reasonOpen.value && current())
  return result
}
function cancelBatchPreparation(): void {
  if (!preparing.value || writing.value || reasonOpen.value || confirmationMessage.value !== null) return
  batchEpoch++; batchAbort?.abort(); preparing.value = false; message.value = '已取消操作準備，尚未送出變更。'; void restoreFocus(queryButton.value)
}
async function batch(kind: 'approve'|'void'): Promise<void> {
  if (dev.value || (kind === 'approve' ? !canApprove.value : !canVoid.value)) return
  const target = document.activeElement instanceof HTMLElement ? document.activeElement : null
  const context = props.apiBaseUrl || '', owner = ++batchEpoch, current = () => !disposed && owner === batchEpoch && !dev.value && context === (props.apiBaseUrl || '')
  const queueNos = [...new Set(rows.value.filter(row=>selectedNos.value.includes(row.item.swrNo)).map(row=>row.item.swrNo))]
  if (queueNos.length !== selectedNos.value.length) { error.value = '選取資料已變更，請重新Query。'; return }
  const expected = new Map(queueNos.map(no=>[no,itemReady.value && active.value!.item.swrNo === no ? active.value!.item : rows.value.find(row=>row.item.swrNo===no)!.item]))
  preparing.value = true; error.value = ''; batchAbort?.abort(); batchAbort = new AbortController()
  let completed = 0, started = false, problem = '', lastNo = ''
  try {
    const api = writer()
    const queue: SwrVersionedItem[] = []
    const options = kind === 'void' ? await api.options(batchAbort.signal) : null
    for (const no of queueNos) {
      const item = await api.item(no,batchAbort.signal)
      if (!current()) return
      if (!sameVisibleItem(expected.get(no)!,item.item)) throw new SwrWriteError('選取資料已變更，請重新Query後再操作。',409)
      queue.push(item)
    }
    if (!current() || !await requestConfirmation(`確定${kind === 'approve' ? '核准' : '作廢'}選取的 ${queue.length} 張SWR？`)) return
    if (!current()) return
    const reason = options?.promptForAbortReason ? await askReason(current) : ''
    if (!current() || reason === null) return
    preparing.value = false; writing.value = true; started = true; writeAbort = new AbortController(); root.value?.focus()
    for (const item of queue) {
      lastNo = item.item.swrNo
      if (item.details.length) throw new SwrWriteError('此單含Lot明細，目前可查詢與下載。',503)
      if (kind === 'approve' && item.item.status !== 0 || kind === 'void' && ![0,2,99].includes(item.item.status)) throw new SwrWriteError('此單狀態不允許本次操作，請重新Query。',409)
      if (kind === 'approve') await api.approve(lastNo,item.version,writeAbort.signal)
      else await api.void(lastNo,item.version,reason,item.item.status,writeAbort.signal)
      if (!current()) return
      completed++; message.value = `已${kind === 'approve' ? '核准' : '作廢'} ${completed}/${queue.length} 張。`
    }
  } catch (value) {
    if (current()) {
      if (value instanceof SwrWriteError && value.unknownWrite) uncertain.value = true
      problem = (lastNo ? lastNo+'：' : '') + failure(value); error.value = problem
    }
  } finally {
    if (current()) {
      preparing.value = false
      if (started) {
        clearSelection()
        const summary = `已${kind === 'approve' ? '核准' : '作廢'} ${completed}/${queueNos.length} 張。${completed < queueNos.length ? `其餘 ${queueNos.length-completed} 張未確認完成。` : ''}`
        if (!uncertain.value) {
          const refreshed = await runQuery()
          if (current()) { message.value = summary + (refreshed ? '' : '請重新Query。'); if (problem) error.value = problem + (error.value && error.value !== problem ? ' '+error.value : '') }
        } else message.value = summary + '請重新Query核對實際結果，勿重複送出。'
      }
      if (current()) { writing.value = false; writeAbort = null; await restoreFocus(target) }
    }
  }
}
watch(() => [props.apiBaseUrl,dev.value], reset)
onBeforeUnmount(()=>{ disposed = true; reset() })
</script>

<template>
  <WipSwrBasisDevPreview v-if="dev" :development-preview="true" :current-user-no="currentUserNo" @close="emit('close')" />
  <section v-else ref="root" class="swr-page wp109-read-page" tabindex="-1" @keydown.capture="trap">
    <p v-if="preparing && confirmationMessage === null && !reasonOpen" class="progress" role="status">正在確認選取單據… <button type="button" class="button" @click="cancelBatchPreparation">取消</button></p>
    <p v-if="writing && !mode" class="progress" role="status">處理中，請稍候…</p>
    <div ref="pageBody" class="page-body" :inert="pageLocked" :aria-hidden="pageLocked ? 'true' : undefined">
      <p v-if="error" class="error" role="alert">{{ error }}</p>
      <p v-if="uncertain" class="more" role="status">請重新Query核對上次操作，勿重複送出。</p>
      <fieldset :disabled="busy"><legend>Query</legend>
        <div class="query-grid">
          <label v-for="field in queryFields" :key="field.key"><span><input v-model="flags[field.key]" type="checkbox"> {{ field.label }}</span>
            <select v-if="field.key === 'swrType'" v-model="filters.swrType" :disabled="!flags.swrType"><option value="">請選擇</option><option value="0">作業站指示</option><option value="1">包裝作業說明</option></select>
            <textarea v-else-if="field.multi" v-model="filters[field.key]" class="yellow multi-filter" @input="manualFilterInput(field.key)" rows="1" :disabled="!flags[field.key]" placeholder="每行一筆" />
            <input v-else v-model="filters[field.key]" class="yellow" :disabled="!flags[field.key]" @keydown.enter.prevent="query">
            <button v-if="field.lookup" type="button" class="button" :disabled="!flags[field.key] || !['customerNo','opNo','deviceNo'].includes(field.key)" :aria-label="field.label + '查找'" :title="['customerNo','opNo','deviceNo'].includes(field.key) ? '查找' : '可直接輸入完整值查詢'" @click="openLookup(field.key, $event)">...</button>
          </label>
          <label class="limit">筆數 <input type="number" min="1" max="500" :value="limitText" class="yellow" @input="limitText = ($event.target as HTMLInputElement).value"></label>
          <button ref="queryButton" type="button" class="button query-button" @click="query">Query</button>
        </div>
      </fieldset>
      <div class="work-area"><div class="main-pane">
        <fieldset class="list"><legend>SWR Basis</legend>
          <div class="status-radios"><label v-for="value in [0,1,2,99]" :key="value"><input type="radio" name="swr-read-status" :value="value" :checked="status === value" :disabled="busy" @change="changeStatus(value)"> {{ statusLabels[value] }}</label></div>
          <div class="scroll" :aria-busy="queryBusy"><table><thead><tr><th v-for="column in columns" :key="column">{{ column }}</th></tr></thead><tbody>
            <tr v-for="row in rows" :key="JSON.stringify([row.item.swrNo,row.lotNo])" tabindex="0" :class="{ active:activeNo === row.item.swrNo,selected:selectedNos.includes(row.item.swrNo) }" :aria-selected="selectedNos.includes(row.item.swrNo)" @click="selectRow(row.item.swrNo, $event.ctrlKey || $event.metaKey)" @keydown.enter.prevent="selectRow(row.item.swrNo, $event.ctrlKey || $event.metaKey)" @keydown.space.prevent="selectRow(row.item.swrNo, $event.ctrlKey || $event.metaKey)">
              <td v-for="(_,index) in columns" :key="index">{{ cell(row,index) }}</td>
            </tr>
            <tr v-if="!rows.length"><td :colspan="columns.length" class="empty">{{ emptyText }}</td></tr>
          </tbody></table></div>
          <div class="status-bar" role="status">{{ itemBusy ? '讀取單張內容中…' : message }}<span>已選{{ selectedNos.length }}張</span></div>
          <p v-if="hasMore" class="more">已達{{ rows.length }}列上限，仍有其他結果；請縮小條件。單張明細不受本清單截斷影響。</p>
          <p v-if="selectedNonempty" class="more" role="status">此單含Lot明細，目前可查詢與下載。</p>
        </fieldset>
        <fieldset class="detail"><legend>SWRDescription／SWRDetail</legend><div class="detail-grid">
          <label>SWRDescription<textarea :value="description" readonly aria-label="SWRDescription" /></label>
          <label>LotNo（{{ itemReady ? active!.details.length : 0 }}筆）<textarea :value="detailLots" readonly aria-label="完整SWR明細LotNo" /></label>
        </div></fieldset>
      </div><div class="actions">
        <button type="button" class="button" :disabled="commandBlocked" @click="openForm('add')">ADD (A)</button>
        <button type="button" class="button" :disabled="!canEdit" @click="openForm('edit')">EDIT (E)</button>
        <button type="button" class="button" :disabled="!canCopy" @click="openForm('copy')">Copy (C)</button>
        <button type="button" class="button" :disabled="!canApprove" @click="batch('approve')">Approve (Y)</button>
        <button type="button" class="button" :disabled="!canVoid" @click="batch('void')">Void</button>
        <a v-if="canDownload && attachmentHref" :href="attachmentHref" target="_blank" rel="noopener" class="button download" @click="downloadStarted">DownLoad File</a>
        <button v-else type="button" class="button" disabled>DownLoad File</button>
        <button type="button" class="button" :disabled="writing || preparing" @click="closePage">Close (X)</button>
      </div></div>
    </div>
    <div v-if="mode" class="overlay editor-overlay">
      <section ref="editor" class="dialog editor-dialog" role="dialog" aria-modal="true" :aria-label="title" tabindex="-1" :inert="lookup !== null || confirmationMessage !== null">
        <header class="editor-header"><h2>{{ title }}</h2><button type="button" class="button" aria-label="關閉SWR編輯" :disabled="writing || fileReading" @click="closeForm">×</button></header>
        <form novalidate @submit.prevent="saveForm">
          <div class="form-scroll">
            <p v-if="formLoading" class="more" role="status">載入單據資料中…</p>
            <p v-if="formError" class="error" role="alert">{{ formError }}</p>
            <button v-if="!formLoading && !formReady" type="button" class="button" @click="loadForm()">重試</button>
            <fieldset :disabled="writing || fileReading || !formReady || uncertain"><legend>SWR Basis</legend>
              <div class="form-grid">
                <label>SWRNo<input :value="mode === 'edit' ? sourceItem?.item.swrNo ?? formKey ?? '' : preview?.swrNo ?? ''" readonly aria-label="SWR單號"></label>
                <label>SWRType<select v-model="draft.swrType" aria-label="SWR類型"><option :value="0">作業站指示</option><option :value="1">包裝作業說明</option></select></label>
                <label>CustomerNo<span class="field-lookup"><input data-focus :value="draft.customerNo ?? ''" aria-label="SWR客戶編號" @input="nullableInput('customerNo',$event)"><button type="button" class="button" aria-label="SWR客戶查找" @click="openLookup('customerNo',$event,'editor')">…</button></span></label>
                <label>OPNo<span class="field-lookup"><input :value="draft.opNo ?? ''" aria-label="SWR作業站編號" @input="nullableInput('opNo',$event)"><button type="button" class="button" aria-label="SWR作業站查找" @click="openLookup('opNo',$event,'editor')">…</button></span></label>
                <label>MONo<input :value="draft.moNo ?? ''" aria-label="SWR工單編號" @input="nullableInput('moNo',$event)"></label>
                <label>CustomerLotNo<input :value="draft.customerLotNo ?? ''" aria-label="SWR客戶批號" @input="nullableInput('customerLotNo',$event)"></label>
                <label>DeviceNo<span class="field-lookup"><input :value="draft.deviceNo ?? ''" aria-label="SWR DeviceNo" @input="nullableInput('deviceNo',$event)"><button type="button" class="button" aria-label="SWR產品查找" @click="openLookup('deviceNo',$event,'editor')">…</button></span></label>
                <label>LotNo<input value="" readonly aria-label="SWR LotNo"><span class="field-note">目前僅提供未指定Lot的單據寫入。</span></label>
                <div class="schedule-row"><label class="schedule-toggle"><input type="checkbox" :checked="schedule" @change="scheduleChanged"> 排程時間</label><div class="form-grid">
                  <label>StartTime<input type="datetime-local" step="0.001" :value="displayTime(draft.startTime)" :title="draft.startTime ?? ''" :disabled="!schedule" aria-label="SWR開始時間" @input="timeInput('startTime',$event)"></label>
                  <label>EndTime<input type="datetime-local" step="0.001" :value="displayTime(draft.endTime)" :title="draft.endTime ?? ''" :disabled="!schedule" aria-label="SWR結束時間" @input="timeInput('endTime',$event)"></label>
                </div></div>
                <label class="wide">SWRDescription<textarea :value="draft.swrDesc ?? ''" rows="3" aria-label="SWR說明" @input="nullableInput('swrDesc',$event)" /></label>
                <div class="wide attachment-fields"><label>AttachFile<select v-model="attachmentAction" aria-label="SWR附件動作" @change="attachmentChanged">
                  <option v-if="mode !== 'edit'" value="none">不附加附件</option><option v-if="mode === 'edit'" value="keep">保留目前附件</option><option v-if="mode === 'edit'" value="clear">移除目前附件</option><option value="replace">選擇PDF附件</option>
                </select></label><label v-if="attachmentAction === 'replace'">PDF<input ref="formFile" type="file" accept=".pdf,application/pdf" aria-label="SWR PDF附件" @change="chooseFile"></label>
                  <span v-if="writerOptions && attachmentAction === 'replace'" class="field-note">上限 {{ (writerOptions.fileSizeLimitBytes / 1048576).toLocaleString() }} MB</span>
                  <span v-if="mode === 'copy'" class="field-note">複製時不沿用來源附件。</span>
                </div>
              </div>
            </fieldset>
            <fieldset class="audit"><legend>建立資訊</legend><div class="form-grid"><label>Creator<input :value="mode === 'edit' ? sourceItem?.item.creator ?? '' : ''" readonly aria-label="SWR建立人"></label><label>CreateDate<input :value="mode === 'edit' ? sourceItem?.item.createDate ?? '' : ''" readonly aria-label="SWR建立日期"></label></div></fieldset>
            <p v-if="mode !== 'edit' && preview" class="field-note">預覽單號，正式單號以儲存結果為準。</p>
            <p v-if="uncertain" class="error" role="status">結果尚未確認，請重新Query核對，勿重複送出。</p>
            <p v-if="writing || fileReading" class="more" role="status">{{ writing ? '儲存中，請稍候…' : '正在檢查附件…' }}</p>
          </div>
          <footer class="dialog-actions"><button v-if="uncertain" type="button" class="button" :disabled="writing" @click="reconcile">關閉並重新Query</button><button type="button" class="button" :disabled="writing || fileReading" @click="closeForm">Close</button><button type="submit" class="button primary" :disabled="writing || fileReading || formLoading || !formReady || uncertain">Confirm</button></footer>
        </form>
      </section>
    </div>
    <div v-if="lookup" class="overlay lookup-overlay"><div ref="lookupDialog" class="dialog" role="dialog" aria-modal="true" :aria-label="lookup + '查找'" tabindex="-1" :inert="confirmationMessage !== null">
      <h2>{{ lookup === 'customerNo' ? 'CustomerNo' : lookup === 'opNo' ? 'OPNo' : 'DeviceNo' }} Lookup</h2>
      <form @submit.prevent="lookupSearch"><label>查詢 <input v-model="lookupKeyword" data-focus class="yellow" :disabled="lookupBusy" aria-label="SWR查找條件"></label><button class="button" :disabled="lookupBusy">Query</button></form>
      <p v-if="lookupError" class="error" role="alert">{{ lookupError }}</p>
      <div class="lookup-scroll" :aria-busy="lookupBusy"><table v-if="lookup !== 'deviceNo'"><thead><tr><th v-if="lookup === 'opNo' && lookupContext === 'query'">選取</th><th>代碼</th><th>名稱</th><th>IssueState</th></tr></thead><tbody>
        <tr v-for="(option,index) in lookupOptions" :key="JSON.stringify([option.key,index])"><td v-if="lookup === 'opNo' && lookupContext === 'query'"><input type="checkbox" :checked="lookupSelected.includes(option.key)" :disabled="lookupBusy" :aria-label="option.key" @change="toggleOperation(option.key)"></td><td><button type="button" class="button" :disabled="lookupBusy" @click="lookup === 'customerNo' ? chooseCustomer(option.key) : toggleOperation(option.key)">{{ option.key }}</button></td><td>{{ option.name }}</td><td>{{ option.issueState === null ? 'NULL' : option.issueState }}</td></tr>
        <tr v-if="!lookupOptions.length"><td :colspan="lookup === 'opNo' && lookupContext === 'query' ? 4 : 3" class="empty">{{ lookupBusy ? '讀取中…' : lookupError ? '讀取未完成。' : '沒有符合資料。' }}</td></tr>
      </tbody></table><table v-else><thead><tr><th>ProductNo</th><th>Version</th><th>ProductName</th><th>DeviceNo</th></tr></thead><tbody>
        <tr v-for="(option,index) in deviceOptions" :key="JSON.stringify([option.productNo,option.productVersion])"><td><button type="button" class="button" :disabled="lookupBusy" @click="chooseDevice(index)">{{ option.productNo }}</button></td><td>{{ option.productVersion }}</td><td>{{ option.productName }}</td><td>{{ option.deviceNo === null ? '未設定' : option.deviceNo }}</td></tr>
        <tr v-if="!deviceOptions.length"><td colspan="4" class="empty">{{ lookupBusy ? '讀取中…' : lookupError ? '讀取未完成。' : '沒有符合資料。' }}</td></tr>
      </tbody></table></div>
      <p v-if="devicesHaveMore" class="more">仍有其他產品，請縮小查找條件。</p>
      <div class="dialog-actions"><button v-if="lookup === 'opNo' && lookupContext === 'query'" type="button" class="button primary" :disabled="lookupBusy || !lookupSelected.length" @click="confirmOperations">Confirm</button><button type="button" class="button" @click="closeLookup">Close</button></div>
    </div></div>
    <div v-if="reasonOpen" class="overlay reason-overlay"><section ref="reasonDialog" class="dialog reason-dialog" role="dialog" aria-modal="true" aria-label="作廢原因" tabindex="-1"><h2>作廢原因</h2><form @submit.prevent="resolveReason(abortReason)"><label>原因<textarea v-model="abortReason" data-focus rows="4" aria-label="SWR作廢原因" /></label><footer class="dialog-actions"><button type="button" class="button" @click="resolveReason(null)">取消</button><button type="submit" class="button primary">Confirm</button></footer></form></section></div>
    <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
  </section>
</template>

<style scoped>
.swr-page{height:100%;min-height:0;display:flex;flex-direction:column;padding:9px;box-sizing:border-box;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif}.page-body{height:100%;min-height:0;display:flex;flex-direction:column;gap:7px}fieldset{min-width:0;margin:0;padding:7px;border:1px solid #bbb;background:#fff}legend{padding:0 5px}.query-grid{display:grid;grid-template-columns:repeat(4,minmax(190px,1fr));gap:5px}.query-grid label{display:flex;align-items:center;gap:4px}.query-grid label span{min-width:100px}.query-grid input:not([type=checkbox]),.query-grid select,.multi-filter{min-width:0;width:120px;min-height:24px;border:1px solid #888;box-sizing:border-box;font:inherit}.multi-filter{height:32px;resize:vertical}.query-grid .limit input{width:75px}.yellow{background:var(--input-yellow,#fff0b3)}input:disabled,select:disabled,textarea:disabled{background:#eee}.button{min-height:26px;padding:3px 8px;border:1px solid #aaa;border-radius:3px;background:linear-gradient(#fff,#eee);color:#333;font:inherit;cursor:pointer}.button:disabled{color:#aaa;cursor:default}.query-button{justify-self:end}.work-area{flex:1;min-height:250px;display:grid;grid-template-columns:minmax(0,1fr) 110px;gap:7px}.main-pane{min-height:0;display:flex;flex-direction:column;gap:7px}.list{flex:1;min-height:180px;display:flex;flex-direction:column}.status-radios{display:flex;gap:15px;padding-bottom:6px}.scroll{flex:1;min-height:0;overflow:auto;border:1px solid #aaa}table{min-width:1750px;border-collapse:collapse;white-space:nowrap}th,td{padding:4px 7px;border:1px solid #ccc;text-align:left}th{position:sticky;top:0;background:#e9e5d8;font-weight:500}td{background:#fff8dd}tr.active td{background:#ffe4a0}tr.selected td{outline:1px solid #a86f17}.empty{text-align:center;background:#fff;color:#777;height:36px}.status-bar{display:flex;justify-content:space-between;color:#666;padding-top:5px}.more{margin:4px 0 0;color:#665b43}.detail-grid{display:grid;grid-template-columns:2fr 1fr;gap:8px}.detail label{display:flex;flex-direction:column;gap:3px}.detail textarea{width:100%;height:82px;box-sizing:border-box;border:1px solid #888;font:inherit}.actions{display:flex;flex-direction:column;align-items:stretch;gap:6px;padding-top:65px}.actions>.button:last-child{margin-top:auto}.download{display:flex;align-items:center;justify-content:center;text-decoration:none;box-sizing:border-box}.error{margin:0;padding:6px;border:1px solid #d8a7a2;background:#fff1ef;color:#8f261f}.overlay{position:fixed;inset:0;z-index:2100;display:flex;align-items:center;justify-content:center;padding:12px;background:#0007}.dialog{width:min(740px,96vw);max-height:90vh;overflow:auto;padding:12px;border:1px solid #888;background:#f7f5ee;box-shadow:0 8px 28px #0005}.dialog h2{margin:0 0 8px;font-size:16px}.dialog form{display:flex;gap:6px;margin-bottom:8px}.dialog input:not([type=checkbox]){height:25px;border:1px solid #888;font:inherit}.lookup-scroll{max-height:50vh;overflow:auto}.lookup-scroll table{min-width:100%;width:100%}.dialog-actions{display:flex;justify-content:flex-end;gap:6px;margin-top:10px}.primary{background:#594d30;border-color:#594d30;color:#fff}@media(max-width:1000px){.query-grid{grid-template-columns:repeat(2,minmax(190px,1fr))}}

/* The formal read page alone allocates the actual remaining framework height. */
:global(.framework-main:has(> .wp109-read-page)){display:flex;flex-direction:column;height:100dvh;min-height:0;overflow:hidden}
:global(.framework-main:has(> .wp109-read-page) > .framework-header){flex-shrink:0}
.wp109-read-page{flex:1;height:auto;overflow:auto}
.wp109-read-page > .page-body{height:auto;flex:1;min-height:0}
.progress{display:flex;align-items:center;gap:8px;margin:0 0 6px;color:#665b43}
.editor-overlay,.reason-overlay{z-index:1800}.lookup-overlay{z-index:1900}.overlay{box-sizing:border-box}
.editor-dialog{display:flex;flex-direction:column;width:min(820px,100%);max-height:calc(100dvh - 32px);padding:0;overflow:hidden;background:#fff}
.editor-header{display:flex;align-items:center;justify-content:space-between;flex-shrink:0;gap:12px;padding:10px 12px;border-bottom:1px solid #c8c4bb;background:#eeebe1}
.editor-header h2{margin:0}.editor-header .button{min-width:32px}
.editor-dialog form{display:flex;flex-direction:column;min-height:0;gap:0;margin:0;overflow:hidden}
.form-scroll{min-height:0;padding:10px 12px;overflow:auto;overscroll-behavior:contain}
.form-scroll fieldset{padding:8px}.form-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:8px}
.form-grid>label,.attachment-fields>label{display:flex;flex-direction:column;gap:4px;min-width:0}
.form-grid input:not([type=checkbox]),.form-grid select,.form-grid textarea{width:100%;min-width:0;min-height:28px;box-sizing:border-box;padding:4px;border:1px solid #aaa;background:var(--input-yellow,#fff0b3);font:inherit;color:inherit}
.form-grid input[readonly],.audit input{background:#eeede9}.form-grid input:disabled,.form-grid select:disabled,.form-grid textarea:disabled{background:#eeede9}
.form-grid .field-lookup{display:flex;min-width:0;gap:4px}.field-lookup input{flex:1}.field-lookup .button{flex-shrink:0;min-width:32px}
.form-grid .wide,.schedule-row{grid-column:1/-1}.schedule-toggle{display:flex;align-items:center;gap:5px;margin:0 0 5px}
.form-grid textarea{resize:vertical;min-height:78px}.field-note{display:block;margin:3px 0 0;color:#777366;font-size:12px;overflow-wrap:anywhere}
.attachment-fields{display:grid;grid-template-columns:minmax(150px,1fr) minmax(0,2fr);align-items:end;gap:6px}.attachment-fields .field-note{grid-column:1/-1}
.attachment-fields input[type=file]{padding:3px;background:#fff}.audit{margin-top:8px!important}
.editor-dialog .dialog-actions{margin:0;padding:9px 12px;flex-shrink:0;border-top:1px solid #d3cfc4;background:#f8f7f3}
.reason-dialog{width:min(480px,100%);box-sizing:border-box}.reason-dialog form{display:block;margin:0}.reason-dialog label{display:flex;flex-direction:column;gap:5px}.reason-dialog textarea{width:100%;box-sizing:border-box;resize:vertical;min-height:80px;max-height:45dvh;background:var(--input-yellow,#fff0b3);font:inherit}
.dialog [inert]{pointer-events:none}.dialog :focus-visible,.swr-page .button:focus-visible{outline:2px solid #8b761c;outline-offset:2px}
@media(max-width:680px){.query-grid{grid-template-columns:minmax(0,1fr)}.query-grid label{min-width:0}.form-grid{grid-template-columns:minmax(0,1fr)}.form-grid .wide,.schedule-row{grid-column:auto}.attachment-fields{grid-template-columns:minmax(0,1fr)}.attachment-fields .field-note{grid-column:auto}}
</style>
