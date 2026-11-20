<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'

type QueryKey = 'customerNo' | 'opNo' | 'lotNo' | 'waitNo' | 'productNo' | 'moNo'
type Option = 'split' | 'merge' | 'go' | 'jumpOp' | 'jumpMain' | 'jumpProcess' | 'jumpModule' | 'inventory' | 'scrapInventory' | 'tuning'
type LookupKey = QueryKey | 'targetOp' | 'targetProcess' | 'returnOp' | 'moduleNo' | 'mainOp' | 'inventoryNo' | 'mergeLot'
type WaitLot = {
  waitNo: string; lotNo: string; baseLotNo: string; productNo: string; customerNo: string; opNo: string
  areaNo: string; psNo: string; processNo: string; ruleNo: string; curQty: number; curUnitNo: string
  priority: number; batchWaitNo: string; goStatus: string; createDate: string; waitDescription: string
  moNo: string; partialMode: boolean; ecnActive: boolean; inModule: boolean; inStocker: boolean
  hasGoodBin: boolean; hasComponent: boolean; carrierNo: string; lotStamp: number
}
type Release = {
  waitNo: string; lotNo: string; lotDispType: number; description: string; actor: string; releasedAt: string
  target: string; newLotNo?: string; quantity?: number
}
type Saved = { releases: Release[] }

const props = defineProps<{ developmentPreview?: boolean; currentUserNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview !== false)
const unavailableMessage = '等待處置服務尚未提供，暫無法查詢或操作。'
const previewMessage = 'Query 待處置 Lot，勾選後選擇處置。'
const storageKey = 'ymes-dev-mock:wip-wait-disposition:state:v1'
const wp502Key = 'ymes-dev-mock:wip-immediate-wait:state:v1'
const queryFields: { key: QueryKey; label: string; lookup: boolean }[] = [
  { key: 'customerNo', label: 'Customer No', lookup: true }, { key: 'lotNo', label: 'Lot No', lookup: false },
  { key: 'productNo', label: 'Product No', lookup: true }, { key: 'opNo', label: 'OP No', lookup: true },
  { key: 'waitNo', label: 'Wait No', lookup: false }, { key: 'moNo', label: 'MO No', lookup: true },
]
const options: { key: Option; label: string; type: number; privilege: string }[] = [
  { key: 'split', label: 'Split', type: 12, privilege: 'SPLIT' },
  { key: 'merge', label: 'Merge', type: 13, privilege: 'MERGE' },
  { key: 'go', label: 'Go', type: 0, privilege: 'GO' },
  { key: 'jumpOp', label: 'Jump OP', type: 1, privilege: 'JUMP_OP' },
  { key: 'jumpMain', label: 'Jump Main', type: 8, privilege: 'JUMP_MAIN' },
  { key: 'jumpProcess', label: 'Jump Process', type: 2, privilege: 'JUMP_PROCESS_01/02' },
  { key: 'jumpModule', label: 'Jump Module', type: 6, privilege: 'JUMP_MODULE' },
  { key: 'inventory', label: 'Inventory', type: 3, privilege: 'INVENTORY' },
  { key: 'scrapInventory', label: 'Scrap Inventory', type: 7, privilege: 'SCRAP_INVENTORY' },
  { key: 'tuning', label: 'Process Tuning', type: 4, privilege: 'PROCESS_TURNING' },
]
const routes = [
  { processNo: 'PROC-A', opNo: 'OP-TEST' }, { processNo: 'PROC-A', opNo: 'OP-TEST-20' },
  { processNo: 'PROC-B', opNo: 'OP-PACK' }, { processNo: 'PROC-B', opNo: 'OP-PACK-20' },
]
const columns: { key: keyof WaitLot; label: string }[] = [
  { key: 'lotNo', label: 'Lot No' }, { key: 'goStatus', label: 'GoStatus' }, { key: 'waitNo', label: 'Wait No' },
  { key: 'baseLotNo', label: 'Base Lot No' }, { key: 'productNo', label: 'Product No' },
  { key: 'customerNo', label: 'Customer No' }, { key: 'opNo', label: 'OP No' }, { key: 'areaNo', label: 'Area No' },
  { key: 'psNo', label: 'PS No' }, { key: 'processNo', label: 'Process No' }, { key: 'ruleNo', label: 'Rule No' },
  { key: 'curQty', label: 'Wait CurQty' }, { key: 'curUnitNo', label: 'Unit No' },
  { key: 'createDate', label: 'Cal Wait Time' }, { key: 'priority', label: 'Priority' },
  { key: 'batchWaitNo', label: 'Batch WaitNo' },
]
const flags = reactive<Record<QueryKey, boolean>>({ customerNo: false, opNo: false, lotNo: false, waitNo: false, productNo: false, moNo: false })
const fields = reactive<Record<QueryKey, string>>({ customerNo: '', opNo: '', lotNo: '', waitNo: '', productNo: '', moNo: '' })
const dates = reactive({ fromOn: false, toOn: false, from: '', to: '' })
const target = reactive({ op: '', process: '', returnOp: '', mainOp: '', module: '', moduleVersion: '01', inventory: '' })
const rows = ref<WaitLot[]>([])
const checked = ref<string[]>([])
const activeWaitNo = ref('')
const scanLotNo = ref('')
const option = ref<Option | ''>('')
const returnOrg = ref(true)
const returnMain = ref(true)
const releaseDisp = ref('')
const lookup = ref<LookupKey | null>(null)
const childOpen = ref(false)
const childQty = ref('')
const childLotNo = ref('')
const saved = ref<Saved>({ releases: [] })
const error = ref('')
const message = ref('Query 待處置 Lot，勾選後選擇處置。')
const queried = ref(false)
const selected = computed(() => rows.value.find(item => item.waitNo === activeWaitNo.value) || null)
const chosen = computed(() => rows.value.filter(item => checked.value.includes(item.waitNo)))
const allLots = computed(() => dev.value ? [...builtInLots(), ...wp502Lots()].filter(item => !saved.value.releases.some(release => release.waitNo === item.waitNo)) : [])
const lookupOptions = computed(() => {
  if (!dev.value) return []
  const key = lookup.value
  if (!key) return []
  if (key === 'targetOp') return routes.filter(row => row.processNo === (option.value === 'jumpOp' ? chosen.value[0]?.processNo : target.process)).map(row => row.opNo)
  if (key === 'returnOp') return routes.filter(row => row.processNo === chosen.value[0]?.processNo).map(row => row.opNo)
  if (key === 'mainOp') return routes.map(row => row.opNo)
  if (key === 'targetProcess') return [...new Set(routes.map(row => row.processNo))].sort()
  if (key === 'moduleNo') return ['MODULE-A', 'MODULE-B']
  if (key === 'inventoryNo') return ['INV-WIP-01', 'INV-SCRAP-01']
  if (key === 'mergeLot') return allLots.value.filter(item => item.waitNo !== chosen.value[0]?.waitNo && item.productNo === chosen.value[0]?.productNo).map(item => item.lotNo)
  return [...new Set(allLots.value.map(item => item[key]))].filter(Boolean).sort()
})
const childVariant = computed(() => { const item = chosen.value[0]; return item?.hasGoodBin ? 'Bin' : item?.hasComponent ? item.carrierNo ? 'Carrier' : 'Component' : 'Standard' })
const releaseType = computed(() => options.find(item => item.key === option.value)?.type ?? -1)

function clearFormal(): void {
  rows.value = []; checked.value = []; saved.value = { releases: [] }; activeWaitNo.value = ''; scanLotNo.value = ''
  option.value = ''; lookup.value = null; childOpen.value = false; childQty.value = ''; childLotNo.value = ''; releaseDisp.value = ''
  queried.value = false; error.value = ''; message.value = unavailableMessage
  Object.assign(flags, { customerNo: false, opNo: false, lotNo: false, waitNo: false, productNo: false, moNo: false })
  Object.assign(fields, { customerNo: '', opNo: '', lotNo: '', waitNo: '', productNo: '', moNo: '' })
  Object.assign(dates, { fromOn: false, toOn: false, from: '', to: '' })
  Object.assign(target, { op: '', process: '', returnOp: '', mainOp: '', module: '', moduleVersion: '', inventory: '' })
}
function isoDaysAgo(days: number): string { return new Date(Date.now() - days * 86400000).toISOString() }
function builtInLots(): WaitLot[] {
  if (!dev.value) { return [] }

  return [
    { waitNo: 'DEV-WB-001', lotNo: 'DEV-WAIT-001', baseLotNo: 'DEV-BASE-WAIT-01', productNo: 'PRD-A', customerNo: 'CUS-A', opNo: 'OP-TEST', areaNo: 'A01', psNo: 'PS-01', processNo: 'PROC-A', ruleNo: '', curQty: 12, curUnitNo: 'PCS', priority: 1, batchWaitNo: '', goStatus: 'Queue', createDate: isoDaysAgo(1), waitDescription: '待測試確認', moNo: 'MO-501', partialMode: false, ecnActive: false, inModule: false, inStocker: false, hasGoodBin: false, hasComponent: false, carrierNo: '', lotStamp: 51101 },
    { waitNo: 'DEV-WB-004', lotNo: 'DEV-WAIT-004', baseLotNo: 'DEV-BASE-WAIT-04', productNo: 'PRD-A', customerNo: 'CUS-A', opNo: 'OP-TEST', areaNo: 'A01', psNo: 'PS-01', processNo: 'PROC-A', ruleNo: 'RULE-A', curQty: 8, curUnitNo: 'PCS', priority: 2, batchWaitNo: '', goStatus: 'Running', createDate: isoDaysAgo(0.5), waitDescription: '等待工程處置', moNo: 'MO-501', partialMode: false, ecnActive: false, inModule: false, inStocker: false, hasGoodBin: false, hasComponent: false, carrierNo: '', lotStamp: 51104 },
    { waitNo: 'DEV-WB-005', lotNo: 'DEV-WAIT-005', baseLotNo: 'DEV-BASE-WAIT-05', productNo: 'PRD-B', customerNo: 'CUS-B', opNo: 'OP-PACK', areaNo: 'A02', psNo: 'PS-02', processNo: 'PROC-B', ruleNo: '', curQty: 5, curUnitNo: 'PCS', priority: 1, batchWaitNo: '', goStatus: 'Running', createDate: isoDaysAgo(0.2), waitDescription: '部分過帳等待', moNo: 'MO-502', partialMode: true, ecnActive: false, inModule: false, inStocker: false, hasGoodBin: false, hasComponent: true, carrierNo: '', lotStamp: 51105 },
  ]
}
function wp502Lots(): WaitLot[] {
  if (!dev.value) { return [] }

  try {
    const raw = JSON.parse(window.localStorage.getItem(wp502Key) || 'null') as { waits?: Record<string, unknown>[]; lots?: Record<string, unknown>[] } | null
    if (!raw || !Array.isArray(raw.waits) || !Array.isArray(raw.lots)) return []
    const sourceLots = raw.lots
    return raw.waits.flatMap(wait => {
      const lot = sourceLots.find(item => item.lotNo === wait.lotNo)
      if (!lot || typeof wait.waitNo !== 'string' || typeof wait.lotNo !== 'string') return []
      return [{ waitNo: wait.waitNo, lotNo: wait.lotNo, baseLotNo: String(lot.baseLotNo || ''), productNo: String(lot.productNo || ''), customerNo: String(lot.customerNo || ''), opNo: String(wait.opNo || lot.opNo || ''), areaNo: String(lot.areaNo || ''), psNo: String(lot.psNo || ''), processNo: String(lot.processNo || ''), ruleNo: '', curQty: Number(wait.createQty) || 0, curUnitNo: String(lot.curUnitNo || ''), priority: 0, batchWaitNo: String(wait.batchWaitNo || ''), goStatus: 'Wait', createDate: String(wait.createdAt || isoDaysAgo(0)), waitDescription: String(wait.description || ''), moNo: String(lot.moNo || ''), partialMode: !!lot.partialMode, ecnActive: false, inModule: false, inStocker: false, hasGoodBin: false, hasComponent: false, carrierNo: '', lotStamp: Number(lot.lotStamp) || 0 } satisfies WaitLot]
    })
  } catch { return [] }
}
function loadSaved(): void {
  if (!dev.value) { clearFormal(); return }

  if (!dev.value) return
  try { const raw = JSON.parse(window.localStorage.getItem(storageKey) || 'null') as Saved | null; saved.value = raw && Array.isArray(raw.releases) ? raw : { releases: [] } }
  catch { saved.value = { releases: [] } }
}
function normalize(value: string): string { return value.trim().toUpperCase() }
function localDate(value: Date): string { const pad = (n: number) => String(n).padStart(2, '0'); return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}` }
function query(): void {
  if (!dev.value) { clearFormal(); return }

  error.value = ''; lookup.value = null
  if (!dev.value) { error.value = '此頁目前僅提供 DEV 假資料。'; return }
  loadSaved()
  for (const item of queryFields) fields[item.key] = normalize(fields[item.key])
  if ((dates.fromOn && !dates.from) || (dates.toOn && !dates.to) || (dates.fromOn && dates.toOn && dates.from > dates.to)) { error.value = 'Create Date 區間無效。'; return }
  rows.value = allLots.value.filter(item =>
    queryFields.every(field => !flags[field.key] || !fields[field.key] ||
      (field.key === 'lotNo' ? item.lotNo.includes(fields.lotNo) : item[field.key] === fields[field.key])) &&
    (!dates.fromOn || item.createDate >= new Date(dates.from).toISOString()) &&
    (!dates.toOn || item.createDate <= new Date(dates.to).toISOString()))
    .sort((a, b) => a.lotNo.localeCompare(b.lotNo))
  checked.value = []; activeWaitNo.value = rows.value[0]?.waitNo || ''; queried.value = true
  message.value = `查得 ${rows.value.length} 筆待處置 Lot。`
}
function pick(value: string): void {
  if (!dev.value) { clearFormal(); return }

  const key = lookup.value
  if (!key) return
  if (key === 'targetOp') target.op = value
  else if (key === 'targetProcess') { target.process = value; target.op = '' }
  else if (key === 'returnOp') target.returnOp = value
  else if (key === 'mainOp') target.mainOp = value
  else if (key === 'moduleNo') target.module = value
  else if (key === 'inventoryNo') target.inventory = value
  else if (key === 'mergeLot') childLotNo.value = value
  else { fields[key] = value; flags[key] = true }
  lookup.value = null
}
function scan(): void {
  if (!dev.value) { clearFormal(); return }

  const found = rows.value.find(item => item.lotNo === normalize(scanLotNo.value))
  if (found && !checked.value.includes(found.waitNo)) checked.value = [...checked.value, found.waitNo]
  scanLotNo.value = ''
}
function calWaitTime(value: string): string { return `${Math.max(0, Math.round((Date.now() - new Date(value).getTime()) / 60000))} Minutes` }
function validateSelection(): string {
  if (!dev.value) { return unavailableMessage }

  const items = chosen.value
  if (!items.length) return '請選擇要處置的 Lot。'
  if (!option.value) return '請選擇處置項目。'
  if (new Set(items.map(item => item.partialMode)).size > 1) return 'PartialMode 不同，不能一起處置。'
  if (new Set(items.map(item => item.ecnActive)).size > 1) return '所選 Lot 的 ECN 狀態必須相同。'
  if (items[0].partialMode && !['go', 'jumpOp', 'jumpProcess', 'tuning', 'inventory', 'scrapInventory'].includes(option.value)) return '部分過帳 Lot 不支援此處置選項。'
  if (['split', 'merge'].includes(option.value)) return items.length === 1 ? items[0].inStocker ? '貨架內 Lot 不可分併批。' : '' : '分批或併批一次只能選一個 Lot。'
  if (['jumpOp', 'tuning'].includes(option.value) && new Set(items.map(item => item.processNo)).size > 1) return '批次處置必須有相同 ProcessNo。'
  if (option.value === 'jumpProcess' && (new Set(items.map(item => item.psNo)).size > 1 || items.some(item => item.inModule))) return '跳流程需相同 PSNo，且 Lot 不可在 Module 內。'
  if (['jumpOp', 'tuning', 'jumpProcess'].includes(option.value) && !target.op) return '請選擇目標 OP No。'
  if (['jumpProcess', 'tuning'].includes(option.value) && !target.process) return '請選擇目標 Process No。'
  if (['jumpOp', 'tuning', 'jumpProcess'].includes(option.value) && !routes.some(row => row.opNo === target.op && row.processNo === (option.value === 'jumpOp' ? items[0].processNo : target.process))) return '目標 OP 不屬於選定流程。'
  if (option.value === 'jumpProcess' && returnOrg.value && !target.returnOp) return '請選擇返回原流程的 OP No。'
  if (option.value === 'jumpMain' && !target.mainOp) return '請選擇主流程 OP No。'
  if (option.value === 'jumpModule' && (!target.module || !target.moduleVersion || returnMain.value && !target.mainOp)) return '請選擇 Module、版本與返回主流程 OP No。'
  if (['inventory', 'scrapInventory'].includes(option.value) && !target.inventory) return '請選擇 Inventory No。'
  return ''
}
function commitOne(item: WaitLot, extra: Pick<Release, 'newLotNo' | 'quantity'> = {}): boolean {
  if (!dev.value) { return false }

  let latest: Saved
  try { const raw = JSON.parse(window.localStorage.getItem(storageKey) || 'null') as Saved | null; latest = raw && Array.isArray(raw.releases) ? raw : { releases: [] } }
  catch { error.value = '無法讀取 DEV 處置紀錄。'; return false }
  if (latest.releases.some(row => row.waitNo === item.waitNo)) { error.value = `${item.waitNo} 已處置，請重新查詢。`; return false }
  const event: Release = { waitNo: item.waitNo, lotNo: item.lotNo, lotDispType: releaseType.value, description: releaseDisp.value.trim(), actor: props.currentUserNo || (dev ? 'DEV' : ''), releasedAt: new Date().toISOString(), target: target.op || target.process || target.module || target.mainOp || target.inventory || '', ...extra }
  latest.releases.push(event)
  try { window.localStorage.setItem(storageKey, JSON.stringify(latest)); saved.value = latest; return true }
  catch { error.value = '無法儲存 DEV 處置紀錄。'; return false }
}
function confirm(): void {
  if (!dev.value) { clearFormal(); return }

  error.value = ''
  if (!dev.value) { error.value = '此頁目前僅提供 DEV 假資料。'; return }
  const problem = validateSelection()
  if (problem) { error.value = problem; return }
  if (option.value === 'split' || option.value === 'merge') { childQty.value = ''; childLotNo.value = ''; childOpen.value = true; return }
  let done = 0
  for (const item of chosen.value) { if (!commitOne(item)) break; done++ }
  const failed = error.value
  query()
  error.value = failed
  message.value = done ? `DEV 已處置 ${done} 筆；${failed ? '後續批號未處置。' : '逐筆成功。'}` : message.value
}
function confirmChild(): void {
  if (!dev.value) { clearFormal(); return }

  error.value = ''
  const item = chosen.value[0]
  if (!item) return
  childLotNo.value = normalize(childLotNo.value)
  if (!childLotNo.value || childLotNo.value === item.lotNo) { error.value = '請選擇或輸入不同的批號。'; return }
  if (option.value === 'split') {
    const qty = Number(childQty.value)
    if (!Number.isFinite(qty) || qty <= 0 || qty >= item.curQty) { error.value = '分批數量必須大於 0，且小於原批數量。'; return }
    if (!commitOne(item, { newLotNo: childLotNo.value, quantity: qty })) return
  } else {
    const mate = allLots.value.find(row => row.lotNo === childLotNo.value)
    if (!mate || mate.productNo !== item.productNo || mate.processNo !== item.processNo) { error.value = '併入批號需存在，且產品、流程相同。'; return }
    if (!commitOne(item, { newLotNo: childLotNo.value, quantity: item.curQty })) return
  }
  childOpen.value = false; query(); message.value = 'DEV 分併批示意已保存。'
}
for (const field of queryFields) watch(() => flags[field.key], enabled => { if (!enabled) fields[field.key] = '' })
watch(option, () => { error.value = ''; lookup.value = null })
function loadPreview(): void { const now = new Date(); dates.from = localDate(new Date(now.getTime() - 7 * 86400000)); dates.to = localDate(now); loadSaved() }
onMounted(() => { if (dev.value) loadPreview(); else clearFormal() })
watch(dev, enabled => { if (enabled) { message.value = previewMessage; loadPreview() } else clearFormal() })
</script>

<template>
  <section class="wait-disposition">
    <h1>等待處置 <small>WAIT DISPOSITION · WP511</small></h1>
    <p v-if="dev" class="note">DEV 假資料；權限、路由及分併批交易仍由正式 API 驗證。</p>
    <p v-if="!dev" class="note" role="status">{{ unavailableMessage }}</p>
    <p v-if="error" class="error" role="alert">{{ error }}</p>
    <fieldset class="query"><legend>Query</legend>
      <div class="query-fields">
        <label v-for="field in queryFields" :key="field.key"><span><input v-model="flags[field.key]" type="checkbox" :disabled="!dev"> {{ field.label }}</span><input v-model="fields[field.key]" :disabled="!dev || !flags[field.key]" class="yellow" @keydown.enter.prevent="dev && (query())"><button v-if="field.lookup" type="button" :disabled="!dev" @click="dev && (lookup = field.key)">...</button></label>
        <div class="date-row"><span>Create Date</span><label>From <input v-model="dates.fromOn" type="checkbox" :disabled="!dev"><input v-model="dates.from" type="datetime-local" :disabled="!dev || !dates.fromOn"></label><label>To <input v-model="dates.toOn" type="checkbox" :disabled="!dev"><input v-model="dates.to" type="datetime-local" :disabled="!dev || !dates.toOn"></label></div>
        <button type="button" class="button query-button" :disabled="!dev" @click="dev && (query())">Query</button>
      </div>
    </fieldset>
    <fieldset class="wait-lot"><legend>Wait Lot</legend>
      <div class="lot-toolbar"><button type="button" class="button" :disabled="!dev || (!rows.length)" @click="dev && (checked = rows.map(row => row.waitNo))">Select All</button><button type="button" class="button" :disabled="!dev || (!rows.length)" @click="dev && (checked = [])">UnSelect All</button><label>Lot No <input v-model="scanLotNo" class="yellow" :disabled="!dev || (!rows.length)" @keydown.enter.prevent="dev && (scan())"></label><span>{{ rows.length }} 筆</span></div>
      <div class="grid-scroll"><table><thead><tr><th>Select</th><th v-for="column in columns" :key="column.key">{{ column.label }}</th></tr></thead><tbody>
        <tr v-for="row in rows" :key="row.waitNo" :class="{ active: activeWaitNo === row.waitNo }" @click="dev && (activeWaitNo = row.waitNo)"><td><input v-model="checked" type="checkbox" :value="row.waitNo" :disabled="!dev"></td><td v-for="column in columns" :key="column.key">{{ column.key === 'createDate' ? calWaitTime(row.createDate) : row[column.key] }}</td></tr>
        <tr v-if="!rows.length"><td colspan="17" class="empty">{{ !dev ? '服務尚未提供。' : queried ? '沒有符合條件的待處置 Lot。' : '請先 Query。' }}</td></tr>
      </tbody></table></div>
    </fieldset>
    <fieldset class="description"><legend>Wait Description</legend><textarea :value="selected?.waitDescription || ''" readonly :disabled="!dev"></textarea></fieldset>
    <fieldset class="disposition"><legend>Lot Disposition</legend>
      <div class="disposition-grid">
        <div class="option-section"><strong>Disposition Option</strong><label v-for="choice in options.slice(0, 2)" :key="choice.key"><input v-model="option" type="radio" name="wait-option" :value="choice.key" :disabled="!dev"> {{ choice.label }}</label></div>
        <div class="option-section"><strong>Release Option</strong><label v-for="choice in options.slice(2)" :key="choice.key"><input v-model="option" type="radio" name="wait-option" :value="choice.key" :disabled="!dev"> {{ choice.label }}</label></div>
        <div class="option-fields">
          <fieldset v-if="option === 'jumpOp'"><legend>Jump OP</legend><label>OP No <input v-model="target.op" class="yellow" readonly :disabled="!dev"><button type="button" @click="dev && (lookup = 'targetOp')" :disabled="!dev">...</button></label></fieldset>
          <fieldset v-if="option === 'jumpProcess' || option === 'tuning'"><legend>{{ option === 'jumpProcess' ? 'Jump Process' : 'Process Tuning' }}</legend><label>Process No <input v-model="target.process" class="yellow" readonly :disabled="!dev"><button type="button" @click="dev && (lookup = 'targetProcess')" :disabled="!dev">...</button></label><label>OP No <input v-model="target.op" class="yellow" readonly :disabled="!dev"><button type="button" @click="dev && (lookup = 'targetOp')" :disabled="!dev">...</button></label><div v-if="option === 'jumpProcess'" class="return-controls"><label><input v-model="returnOrg" type="radio" :value="true" :disabled="!dev"> Return Original Process</label><label><input v-model="returnOrg" type="radio" :value="false" :disabled="!dev"> Not Return Original Process</label><label v-if="returnOrg">Return OP No <input v-model="target.returnOp" class="yellow" readonly :disabled="!dev"><button type="button" @click="dev && (lookup = 'returnOp')" :disabled="!dev">...</button></label></div></fieldset>
          <fieldset v-if="option === 'jumpMain'"><legend>Jump Main</legend><label>OP No <input v-model="target.mainOp" class="yellow" readonly :disabled="!dev"><button type="button" @click="dev && (lookup = 'mainOp')" :disabled="!dev">...</button></label></fieldset>
          <fieldset v-if="option === 'jumpModule'"><legend>Jump Module</legend><label>Module <input v-model="target.module" class="yellow" readonly :disabled="!dev"><button type="button" @click="dev && (lookup = 'moduleNo')" :disabled="!dev">...</button></label><label>Version <input v-model="target.moduleVersion" class="yellow" readonly :disabled="!dev"></label><div class="return-controls"><label><input v-model="returnMain" type="radio" :value="true" :disabled="!dev"> Return Main Process</label></div><label v-if="returnMain">OP No <input v-model="target.mainOp" class="yellow" readonly :disabled="!dev"><button type="button" @click="dev && (lookup = 'mainOp')" :disabled="!dev">...</button></label></fieldset>
          <fieldset v-if="option === 'inventory' || option === 'scrapInventory'"><legend>Inventory</legend><label>Inventory No <input v-model="target.inventory" class="yellow" readonly :disabled="!dev"><button type="button" @click="dev && (lookup = 'inventoryNo')" :disabled="!dev">...</button></label></fieldset>
          <label class="release-description">Release Disp <textarea v-model="releaseDisp" :disabled="!dev"></textarea></label>
        </div>
      </div>
    </fieldset>
    <div class="footer"><span>{{ message }}　選取 {{ chosen.length }} 筆 · UserNo {{ currentUserNo || (dev ? 'DEV' : '') }}</span><button type="button" class="button primary" :disabled="!dev" @click="dev && (confirm())">Confirm (C)</button><button type="button" class="button" @click="emit('close')">Close (X)</button></div>
    <div v-if="dev && (lookup)" class="overlay lookup-overlay"><div class="dialog" role="dialog" aria-modal="true"><h2>Lookup · {{ lookup }}</h2><div class="lookup-list"><button v-for="item in lookupOptions" :key="item" type="button" class="button" @click="dev && (pick(item))" :disabled="!dev">{{ item }}</button><span v-if="!lookupOptions.length">沒有可選的 DEV 資料。</span></div><button type="button" class="button" @click="dev && (lookup = null)" :disabled="!dev">Close</button></div></div>
    <div v-if="dev && (childOpen)" class="overlay"><div class="dialog" role="dialog" aria-modal="true"><h2>{{ option === 'split' ? 'Wait Lot Split' : 'Wait Lot Merge' }} · {{ childVariant }}</h2><p>DEV 簡化示意；正式版需依 {{ childVariant }} 子窗與 Service/Core 驗證。</p><label>Wait Lot <input :value="chosen[0]?.lotNo || ''" readonly :disabled="!dev"></label><label>{{ option === 'split' ? 'New Lot No' : 'Merge Lot No' }} <input v-model="childLotNo" class="yellow" :disabled="!dev"><button v-if="option === 'merge'" type="button" @click="dev && (lookup = 'mergeLot')" :disabled="!dev">...</button></label><label v-if="option === 'split'">Split Qty <input v-model="childQty" type="number" min="1" :max="(chosen[0]?.curQty || 1) - 1" class="yellow" :disabled="!dev"></label><div class="dialog-actions"><button type="button" class="button primary" @click="dev && (confirmChild())" :disabled="!dev">Confirm</button><button type="button" class="button" @click="dev && (childOpen = false)" :disabled="!dev">Close</button></div></div></div>
  </section>
</template>

<style scoped>
.wait-disposition{height:100%;min-height:0;display:flex;flex-direction:column;gap:6px;padding:8px;box-sizing:border-box;background:#f7f5ee;color:#303030;font:13px Arial,"Microsoft JhengHei",sans-serif}h1{font-size:18px;margin:0}h1 small{font-size:12px;font-weight:400;color:#777}.note,.error{margin:0;padding:5px 8px;border:1px solid #d4c48e;background:#fff7df}.error{border-color:#d9a3a3;background:#fff1ef;color:#a22}fieldset{min-width:0;margin:0;padding:7px;border:1px solid #b7b7b7;background:#fff}legend{padding:0 5px}.query-fields{display:grid;grid-template-columns:repeat(3,minmax(220px,1fr));gap:6px}.query-fields>label,.date-row,.lot-toolbar,.option-fields label,.dialog label{display:flex;align-items:center;gap:5px}.query-fields>label>span{width:95px;flex:none;white-space:nowrap}.query-fields input:not([type=checkbox]),.lot-toolbar input,.option-fields input:not([type=radio]),.dialog input,.option-fields textarea{min-width:0;height:25px;box-sizing:border-box;padding:3px 5px;border:1px solid #888;font:inherit}.yellow{background:#fff0b3}.query-fields>label>input{width:120px}.query-fields input:disabled{background:#eee}.date-row{grid-column:1/3;gap:10px}.date-row>span{width:95px}.date-row input[type=datetime-local]{width:155px}.query-button{justify-self:end}.button,.query-fields button,.option-fields button,.dialog label button{min-height:26px;padding:3px 9px;border:1px solid #b5b5b5;border-radius:3px;background:linear-gradient(#fff,#eee);color:#333;font:inherit;cursor:pointer}.button:disabled{color:#aaa;cursor:default}.wait-lot{flex:1;min-height:175px;display:flex;flex-direction:column}.lot-toolbar{gap:7px;margin-bottom:6px}.lot-toolbar input{width:170px}.lot-toolbar span{margin-left:auto}.grid-scroll{flex:1;min-height:0;overflow:auto;border:1px solid #aaa}table{border-collapse:collapse;min-width:1700px;white-space:nowrap}th{position:sticky;top:0;background:#ead591;font-weight:500}th,td{padding:4px 7px;border:1px solid #ccc;text-align:left}td{background:#ffe89c}tr.active td{background:#ffdb73}.empty{text-align:center;background:#fff;color:#777;height:35px}.description textarea{width:100%;height:42px;box-sizing:border-box;resize:vertical;border:1px solid #aaa;background:#f7f7f7}.disposition{min-height:225px}.disposition-grid{display:grid;grid-template-columns:180px 190px minmax(280px,1fr);gap:9px}.option-section{display:flex;flex-direction:column;gap:3px}.option-section strong{margin-bottom:5px;font-weight:500}.option-fields{display:flex;flex-direction:column;gap:5px}.option-fields fieldset{display:flex;flex-wrap:wrap;gap:5px 10px}.option-fields label{white-space:nowrap}.option-fields input:not([type=radio]){width:135px}.return-controls{display:flex;flex-wrap:wrap;gap:5px 12px;width:100%}.release-description{display:flex;align-items:flex-start!important}.release-description textarea{width:min(440px,100%);height:55px}.footer{display:flex;align-items:center;justify-content:flex-end;gap:7px}.footer span{margin-right:auto;color:#666}.primary{background:#ffb43a;border-color:#e3a235}.overlay{position:fixed;inset:0;z-index:220;display:flex;align-items:center;justify-content:center;padding:16px;background:#0007}.lookup-overlay{z-index:240}.dialog{width:min(510px,100%);max-height:90vh;overflow:auto;display:flex;flex-direction:column;align-items:flex-start;gap:10px;padding:15px;border:1px solid #888;background:#f7f5ee;box-shadow:0 8px 25px #0005}.dialog h2,.dialog p{margin:0}.dialog input{width:170px}.lookup-list{width:100%;max-height:250px;overflow:auto;display:flex;flex-wrap:wrap;gap:7px}.dialog-actions{display:flex;gap:8px;align-self:flex-end}@media(max-width:950px){.query-fields{grid-template-columns:1fr 1fr}.disposition-grid{grid-template-columns:135px 145px minmax(220px,1fr)}.wait-lot{min-height:210px}}

/* The framework owns the page title for this opt-in mount. */
.wait-disposition.office-wip-module > h1 { display: none; }
</style>
