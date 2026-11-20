<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, reactive, ref, watch } from 'vue'
import WipSwrBasisDevPreview from './WipSwrBasisDevPreview.vue'
import { createWp109ReadService, type FilterKey, type LookupOption, type SwrItem, type SwrRow } from '../services/wp109'

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
const lookup = ref<'customerNo'|'opNo'|null>(null), lookupKeyword = ref(''), lookupOptions = ref<LookupOption[]>([]), lookupSelected = ref<string[]>([])
const lookupBusy = ref(false), lookupError = ref('')
let queryEpoch = 0, itemEpoch = 0, lookupEpoch = 0
let queryAbort: AbortController | null = null, itemAbort: AbortController | null = null, lookupAbort: AbortController | null = null
let lookupTrigger: HTMLElement | null = null
const busy = computed(() => queryBusy.value || itemBusy.value)
const service = () => createWp109ReadService(props.apiBaseUrl || '', window.location.origin)
const itemReady = computed(() => !itemBusy.value && active.value !== null && active.value.item.swrNo === activeNo.value && selectedNos.value.includes(active.value.item.swrNo))
const canDownload = computed(() => itemReady.value && selectedNos.value.length === 1 && active.value!.item.attachment.downloadAvailable)
const attachmentHref = computed(() => {
  if (!canDownload.value || activeNo.value === null) return ''
  try { return service().attachmentUrl(activeNo.value) } catch { return '' }
})
const emptyText = computed(() => phase.value === 'loading' ? '查詢中…' : phase.value === 'error' ? '查詢未完成。' : phase.value === 'empty' ? '沒有符合條件的SWR。' : '請先Query。')
const description = computed(() => itemReady.value ? active.value!.item.swrDesc : '')
const detailLots = computed(() => itemReady.value ? active.value!.details.map(row => row.lotNo).join('\n') : '')
const unavailable = '目前僅提供查詢及附件下載。'

function cancelReads(): void {
  queryEpoch++; itemEpoch++; lookupEpoch++
  queryAbort?.abort(); itemAbort?.abort(); lookupAbort?.abort()
  queryBusy.value = false; itemBusy.value = false; lookupBusy.value = false
}
function clearSelection(): void { itemEpoch++; itemAbort?.abort(); itemBusy.value = false; selectedNos.value = []; activeNo.value = null; active.value = null }
function reset(): void { cancelReads(); clearSelection(); rows.value = []; hasMore.value = false; phase.value = 'initial'; error.value = ''; message.value = '請先Query。'; lookup.value = null }
function failure(value: unknown): string { return value instanceof Error ? value.message : '讀取未完成，請重新查詢。' }
async function query(): Promise<void> {
  if (dev.value || lookup.value || queryBusy.value) return
  const epoch = ++queryEpoch; queryAbort?.abort(); queryAbort = new AbortController(); const signal = queryAbort.signal
  clearSelection(); rows.value = []; hasMore.value = false; error.value = ''; queryBusy.value = true; phase.value = 'loading'; message.value = '查詢中…'
  try {
    if (!/^\d+$/.test(limitText.value)) throw new Error('查詢筆數請輸入1至500。')
    const result = await service().query({ enabled:{...flags},values:{...filters},status:status.value,limit:Number(limitText.value),
      ...(pickedOperationNos.value === null ? {} : {opNos:[...pickedOperationNos.value]}) }, signal)
    if (signal.aborted || epoch !== queryEpoch || dev.value) return
    rows.value = result.items; hasMore.value = result.hasMore; phase.value = result.items.length ? 'ready' : 'empty'
    message.value = `查得${result.items.length}列／${new Set(result.items.map(row => row.item.swrNo)).size}張SWR。`
  } catch (value) {
    if (signal.aborted || epoch !== queryEpoch) return
    phase.value = 'error'; error.value = failure(value); message.value = '查詢未完成。'
  } finally { if (epoch === queryEpoch) queryBusy.value = false }
}
async function readItem(rawNo: string): Promise<void> {
  const epoch = ++itemEpoch; itemAbort?.abort(); itemAbort = new AbortController(); const signal = itemAbort.signal
  active.value = null; itemBusy.value = true; error.value = ''
  try {
    const result = await service().item(rawNo, signal)
    if (signal.aborted || epoch !== itemEpoch || dev.value || activeNo.value !== rawNo || !selectedNos.value.includes(rawNo)) return
    active.value = result
  } catch (value) {
    if (signal.aborted || epoch !== itemEpoch) return
    error.value = failure(value); message.value = '單張內容讀取未完成；請重新選取或Query。'
  } finally { if (epoch === itemEpoch) itemBusy.value = false }
}
function selectRow(rawNo: string, multi: boolean): void {
  if (dev.value || queryBusy.value || lookup.value) return
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
function changeStatus(value: number): void { if (dev.value || busy.value || lookup.value) return; status.value = value; void query() }
function blockedCommand(): void { if (!dev.value) error.value = unavailable }
function downloadStarted(): void { if (!canDownload.value) return; message.value = '下載請求已交由瀏覽器處理；尚未確認完成。' }
function closePage(): void { reset(); emit('close') }

async function openLookup(key: FilterKey, event: Event): Promise<void> {
  if (dev.value || busy.value || lookup.value || !['customerNo','opNo'].includes(key)) return
  lookupTrigger = event.currentTarget instanceof HTMLElement ? event.currentTarget : null
  lookup.value = key as 'customerNo'|'opNo'; lookupKeyword.value = ''; lookupError.value = ''; lookupOptions.value = []
  lookupSelected.value = key === 'opNo' && flags.opNo ? (pickedOperationNos.value === null ? filters.opNo.split(/\r?\n/) : [...pickedOperationNos.value]).filter(value => !/^ *$/.test(value)) : []
  await nextTick(); void lookupSearch(); await nextTick(); focusFirst()
}
async function lookupSearch(): Promise<void> {
  const kind = lookup.value; if (!kind || dev.value || lookupBusy.value) return
  const epoch = ++lookupEpoch; lookupAbort?.abort(); lookupAbort = new AbortController(); const signal = lookupAbort.signal
  lookupBusy.value = true; lookupError.value = ''; lookupOptions.value = []
  try {
    const result = kind === 'customerNo' ? await service().customers(lookupKeyword.value, signal) : await service().operations(lookupKeyword.value, signal)
    if (signal.aborted || epoch !== lookupEpoch || lookup.value !== kind || dev.value) return
    lookupOptions.value = result
  } catch (value) { if (!signal.aborted && epoch === lookupEpoch) lookupError.value = failure(value) }
  finally { if (epoch === lookupEpoch) lookupBusy.value = false }
}
function chooseCustomer(value: string): void {
  if (lookup.value !== 'customerNo' || lookupBusy.value || !lookupOptions.value.some(row => row.key === value)) return
  filters.customerNo = value; flags.customerNo = true; closeLookup()
}
function toggleOperation(value: string): void {
  if (lookup.value !== 'opNo' || lookupBusy.value || !lookupOptions.value.some(row => row.key === value)) return
  lookupSelected.value = lookupSelected.value.includes(value) ? lookupSelected.value.filter(v => v !== value) : [...lookupSelected.value, value]
}
function confirmOperations(): void {
  if (lookup.value !== 'opNo' || lookupBusy.value || !lookupSelected.value.length) return
  pickedOperationNos.value = [...lookupSelected.value]
  filters.opNo = lookupSelected.value.join('\n'); flags.opNo = true; closeLookup()
}
function manualFilterInput(key: FilterKey): void { if (key === 'opNo') pickedOperationNos.value = null }
function closeLookup(): void {
  lookupEpoch++; lookupAbort?.abort(); lookupBusy.value = false; lookup.value = null
  const trigger = lookupTrigger; lookupTrigger = null
  void nextTick(() => { if (trigger?.isConnected) trigger.focus(); else root.value?.focus() })
}
function focusable(): HTMLElement[] {
  return [...(lookupDialog.value?.querySelectorAll<HTMLElement>('button,input,select,textarea,[tabindex="0"]') || [])].filter(node => !node.hasAttribute('disabled') && node.getClientRects().length > 0)
}
function focusFirst(): void { focusable()[0]?.focus() }
function trap(event: KeyboardEvent): void {
  if (!lookup.value) return
  if (event.key === 'Escape') { event.preventDefault(); event.stopPropagation(); closeLookup(); return }
  if (event.key !== 'Tab') return
  const nodes = focusable(); if (!nodes.length) { event.preventDefault(); return }
  const first = nodes[0], last = nodes[nodes.length - 1], activeElement = document.activeElement
  if (!nodes.includes(activeElement as HTMLElement) || event.shiftKey && activeElement === first || !event.shiftKey && activeElement === last) {
    event.preventDefault(); (event.shiftKey ? last : first).focus()
  }
}
watch(() => [props.apiBaseUrl,dev.value], reset)
onBeforeUnmount(cancelReads)
</script>

<template>
  <WipSwrBasisDevPreview v-if="dev" :development-preview="true" :current-user-no="currentUserNo" @close="emit('close')" />
  <section v-else ref="root" class="swr-page wp109-read-page" tabindex="-1" @keydown.capture="trap">
    <div ref="pageBody" class="page-body" :inert="lookup !== null" :aria-hidden="lookup ? 'true' : undefined">
      <p v-if="error" class="error" role="alert">{{ error }}</p>
      <fieldset :disabled="busy"><legend>Query</legend>
        <div class="query-grid">
          <label v-for="field in queryFields" :key="field.key"><span><input v-model="flags[field.key]" type="checkbox"> {{ field.label }}</span>
            <select v-if="field.key === 'swrType'" v-model="filters.swrType" :disabled="!flags.swrType"><option value="">請選擇</option><option value="0">作業站指示</option><option value="1">包裝作業說明</option></select>
            <textarea v-else-if="field.multi" v-model="filters[field.key]" class="yellow multi-filter" @input="manualFilterInput(field.key)" rows="1" :disabled="!flags[field.key]" placeholder="每行一筆" />
            <input v-else v-model="filters[field.key]" class="yellow" :disabled="!flags[field.key]" @keydown.enter.prevent="query">
            <button v-if="field.lookup" type="button" class="button" :disabled="!flags[field.key] || !['customerNo','opNo'].includes(field.key)" :aria-label="field.label + '查找'" :title="['customerNo','opNo'].includes(field.key) ? '查找' : '查找尚未提供；可輸入完整值查詢'" @click="openLookup(field.key, $event)">...</button>
          </label>
          <label class="limit">筆數 <input type="number" min="1" max="500" :value="limitText" class="yellow" @input="limitText = ($event.target as HTMLInputElement).value"></label>
          <button type="button" class="button query-button" @click="query">Query</button>
        </div>
      </fieldset>
      <div class="work-area"><div class="main-pane">
        <fieldset class="list"><legend>SWR Basis</legend>
          <div class="status-radios"><label v-for="value in [0,1,2,99]" :key="value"><input type="radio" name="swr-read-status" :value="value" :checked="status === value" :disabled="busy" @change="changeStatus(value)"> {{ statusLabels[value] }}</label></div>
          <div class="scroll" :aria-busy="queryBusy"><table><thead><tr><th v-for="column in columns" :key="column">{{ column }}</th></tr></thead><tbody>
            <tr v-for="row in rows" :key="JSON.stringify([row.item.swrNo,row.lotNo])" tabindex="0" :class="{ active:activeNo === row.item.swrNo,selected:selectedNos.includes(row.item.swrNo) }" :aria-selected="selectedNos.includes(row.item.swrNo)" @click="selectRow(row.item.swrNo, $event.ctrlKey || $event.metaKey)" @keydown.enter.prevent="selectRow(row.item.swrNo, $event.ctrlKey || $event.metaKey)" @keydown.space.prevent="selectRow(row.item.swrNo, $event.ctrlKey || $event.metaKey)" @dblclick="blockedCommand">
              <td v-for="(_,index) in columns" :key="index">{{ cell(row,index) }}</td>
            </tr>
            <tr v-if="!rows.length"><td :colspan="columns.length" class="empty">{{ emptyText }}</td></tr>
          </tbody></table></div>
          <div class="status-bar" role="status">{{ itemBusy ? '讀取單張內容中…' : message }}<span>已選{{ selectedNos.length }}張</span></div>
          <p v-if="hasMore" class="more">已達{{ rows.length }}列上限，仍有其他結果；請縮小條件。單張明細不受本清單截斷影響。</p>
        </fieldset>
        <fieldset class="detail"><legend>SWRDescription／SWRDetail</legend><div class="detail-grid">
          <label>SWRDescription<textarea :value="description" readonly aria-label="SWRDescription" /></label>
          <label>LotNo（{{ itemReady ? active!.details.length : 0 }}筆）<textarea :value="detailLots" readonly aria-label="完整SWR明細LotNo" /></label>
        </div></fieldset>
      </div><div class="actions">
        <button v-for="label in ['ADD (A)','EDIT (E)','Copy (C)','Approve (Y)','Void']" :key="label" type="button" class="button" disabled @click="blockedCommand">{{ label }}</button>
        <a v-if="canDownload && attachmentHref" :href="attachmentHref" target="_blank" rel="noopener" class="button download" @click="downloadStarted">DownLoad File</a>
        <button v-else type="button" class="button" disabled>DownLoad File</button>
        <button type="button" class="button" @click="closePage">Close (X)</button>
      </div></div>
    </div>
    <div v-if="lookup" class="overlay"><div ref="lookupDialog" class="dialog" role="dialog" aria-modal="true" :aria-label="lookup + '查找'">
      <h2>{{ lookup === 'customerNo' ? 'CustomerNo' : 'OPNo' }} Lookup</h2>
      <form @submit.prevent="lookupSearch"><label>查詢 <input v-model="lookupKeyword" class="yellow" :disabled="lookupBusy"></label><button class="button" :disabled="lookupBusy">Query</button></form>
      <p v-if="lookupError" class="error" role="alert">{{ lookupError }}</p>
      <div class="lookup-scroll" :aria-busy="lookupBusy"><table><thead><tr><th v-if="lookup === 'opNo'">選取</th><th>代碼</th><th>名稱</th><th>IssueState</th></tr></thead><tbody>
        <tr v-for="(option,index) in lookupOptions" :key="JSON.stringify([option.key,index])"><td v-if="lookup === 'opNo'"><input type="checkbox" :checked="lookupSelected.includes(option.key)" :disabled="lookupBusy" :aria-label="option.key" @change="toggleOperation(option.key)"></td><td><button type="button" class="button" :disabled="lookupBusy" @click="lookup === 'customerNo' ? chooseCustomer(option.key) : toggleOperation(option.key)">{{ option.key }}</button></td><td>{{ option.name }}</td><td>{{ option.issueState === null ? 'NULL' : option.issueState }}</td></tr>
        <tr v-if="!lookupOptions.length"><td :colspan="lookup === 'opNo' ? 4 : 3" class="empty">{{ lookupBusy ? '讀取中…' : lookupError ? '讀取未完成。' : '沒有符合資料。' }}</td></tr>
      </tbody></table></div>
      <div class="dialog-actions"><button v-if="lookup === 'opNo'" type="button" class="button primary" :disabled="lookupBusy || !lookupSelected.length" @click="confirmOperations">Confirm</button><button type="button" class="button" @click="closeLookup">Close</button></div>
    </div></div>
  </section>
</template>

<style scoped>
.swr-page{height:100%;min-height:0;display:flex;flex-direction:column;padding:9px;box-sizing:border-box;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif}.page-body{height:100%;min-height:0;display:flex;flex-direction:column;gap:7px}fieldset{min-width:0;margin:0;padding:7px;border:1px solid #bbb;background:#fff}legend{padding:0 5px}.query-grid{display:grid;grid-template-columns:repeat(4,minmax(190px,1fr));gap:5px}.query-grid label{display:flex;align-items:center;gap:4px}.query-grid label span{min-width:100px}.query-grid input:not([type=checkbox]),.query-grid select,.multi-filter{min-width:0;width:120px;min-height:24px;border:1px solid #888;box-sizing:border-box;font:inherit}.multi-filter{height:32px;resize:vertical}.query-grid .limit input{width:75px}.yellow{background:var(--input-yellow,#fff0b3)}input:disabled,select:disabled,textarea:disabled{background:#eee}.button{min-height:26px;padding:3px 8px;border:1px solid #aaa;border-radius:3px;background:linear-gradient(#fff,#eee);color:#333;font:inherit;cursor:pointer}.button:disabled{color:#aaa;cursor:default}.query-button{justify-self:end}.work-area{flex:1;min-height:250px;display:grid;grid-template-columns:minmax(0,1fr) 110px;gap:7px}.main-pane{min-height:0;display:flex;flex-direction:column;gap:7px}.list{flex:1;min-height:180px;display:flex;flex-direction:column}.status-radios{display:flex;gap:15px;padding-bottom:6px}.scroll{flex:1;min-height:0;overflow:auto;border:1px solid #aaa}table{min-width:1750px;border-collapse:collapse;white-space:nowrap}th,td{padding:4px 7px;border:1px solid #ccc;text-align:left}th{position:sticky;top:0;background:#e9e5d8;font-weight:500}td{background:#fff8dd}tr.active td{background:#ffe4a0}tr.selected td{outline:1px solid #a86f17}.empty{text-align:center;background:#fff;color:#777;height:36px}.status-bar{display:flex;justify-content:space-between;color:#666;padding-top:5px}.more{margin:4px 0 0;color:#665b43}.detail-grid{display:grid;grid-template-columns:2fr 1fr;gap:8px}.detail label{display:flex;flex-direction:column;gap:3px}.detail textarea{width:100%;height:82px;box-sizing:border-box;border:1px solid #888;font:inherit}.actions{display:flex;flex-direction:column;align-items:stretch;gap:6px;padding-top:65px}.actions>.button:last-child{margin-top:auto}.download{display:flex;align-items:center;justify-content:center;text-decoration:none;box-sizing:border-box}.error{margin:0;padding:6px;border:1px solid #d8a7a2;background:#fff1ef;color:#8f261f}.overlay{position:fixed;inset:0;z-index:2100;display:flex;align-items:center;justify-content:center;padding:12px;background:#0007}.dialog{width:min(740px,96vw);max-height:90vh;overflow:auto;padding:12px;border:1px solid #888;background:#f7f5ee;box-shadow:0 8px 28px #0005}.dialog h2{margin:0 0 8px;font-size:16px}.dialog form{display:flex;gap:6px;margin-bottom:8px}.dialog input:not([type=checkbox]){height:25px;border:1px solid #888;font:inherit}.lookup-scroll{max-height:50vh;overflow:auto}.lookup-scroll table{min-width:100%;width:100%}.dialog-actions{display:flex;justify-content:flex-end;gap:6px;margin-top:10px}.primary{background:#594d30;border-color:#594d30;color:#fff}@media(max-width:1000px){.query-grid{grid-template-columns:repeat(2,minmax(190px,1fr))}}

/* The formal read page alone allocates the actual remaining framework height. */
:global(.framework-main:has(> .wp109-read-page)){display:flex;flex-direction:column;height:100dvh;min-height:0;overflow:hidden}
:global(.framework-main:has(> .wp109-read-page) > .framework-header){flex-shrink:0}
.wp109-read-page{flex:1;height:auto;overflow:auto}
.wp109-read-page > .page-body{height:auto;flex:1;min-height:0}
</style>
