<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'

type WaitRow = {
  waitNo: string; lotNo: string; productNo: string; lotSerial: string; status: number
  opNo: string; customerNo: string; waitDescription: string; ruleNo: string
  createQty: number; creator: string; createDate: string; releaseQty: number | null
  releaser: string; releaseDate: string | null; waitSource: number
}
type Disposition = {
  waitNo: string; lotDispType: number; description: string; creator: string
  createDate: string; opNo: string; processNo: string; processVersion: string
}
type LookupKey = 'customerNo' | 'opNo'

const props = defineProps<{ developmentPreview?: boolean; currentUserNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview !== false)
const wp502StorageKey = 'ymes-dev-mock:wip-immediate-wait:state:v1'
const wp511StorageKey = 'ymes-dev-mock:wip-wait-disposition:state:v1'
const statuses: Record<number, string> = {
  0: 'Wait Undisposition', 1: 'Wait Wait Disposition',
  2: 'Notice Customer and Wait Disposition', 3: 'Waive',
  11: 'Wait Lot Disposition', 12: 'Abort', 21: 'Release', 31: 'Hold Bank',
}
const dispositionTypes: Record<number, string> = {
  0: 'GO', 1: 'Jump OP', 2: 'Jump Process', 3: 'WIP Semi Inventory',
  4: 'Process Tuning', 5: 'Change ENGNo', 6: 'Jump Module',
  7: 'WIP Scrap Inventory', 8: 'Jump Main', 11: 'Back To ERF Disposition',
  12: 'Split', 13: 'Merge', 14: 'Qty Adjust',
}
const flags = reactive({ lotNo: false, customerNo: false, opNo: false, waitNo: false, createDate: false, releaseDate: false })
const fields = reactive({ lotNo: '', customerNo: '', opNo: '', waitNo: '' })
const dates = reactive({ createFrom: '', createTo: '', releaseFrom: '', releaseTo: '' })
const allRows = ref<WaitRow[]>([])
const rows = ref<WaitRow[]>([])
const dispositions = ref<Disposition[]>([])
const selectedWaitNo = ref('')
const infoWaitNo = ref('')
const lookup = ref<LookupKey | null>(null)
const searched = ref(false)
const error = ref('')
const message = ref('勾選並填入至少一項條件後，按 Query 查詢等待單。')
const selected = computed(() => rows.value.find(item => item.waitNo === selectedWaitNo.value) || null)
const info = computed(() => allRows.value.find(item => item.waitNo === infoWaitNo.value) || null)
const infoRows = computed(() => dispositions.value.filter(item => item.waitNo === infoWaitNo.value)
  .sort((a, b) => b.createDate.localeCompare(a.createDate)))
const lookupOptions = computed(() => lookup.value ? [...new Set(allRows.value.map(item => item[lookup.value!]))].filter(Boolean).sort() : [])

function localDate(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
}
function stamp(daysAgo: number): string { return new Date(Date.now() - daysAgo * 86400000).toISOString() }
function sampleRows(): WaitRow[] {
  return [
    { waitNo: 'DEV-WB-001', lotNo: 'DEV-WAIT-001', productNo: 'PRD-A', lotSerial: 'DEV-WS-101', status: 0, opNo: 'OP-TEST', customerNo: 'CUS-A', waitDescription: '待測試確認', ruleNo: '', createQty: 12, creator: 'DEV', createDate: stamp(1), releaseQty: null, releaser: '', releaseDate: null, waitSource: 0 },
    { waitNo: 'DEV-WB-002', lotNo: 'DEV-WAIT-002', productNo: 'PRD-A', lotSerial: 'DEV-WS-102', status: 11, opNo: 'OP-TEST', customerNo: 'CUS-A', waitDescription: '待處置', ruleNo: 'DEV-RULE', createQty: 6, creator: 'DEV', createDate: stamp(2), releaseQty: null, releaser: '', releaseDate: null, waitSource: 0 },
    { waitNo: 'DEV-WB-003', lotNo: 'DEV-WAIT-003', productNo: 'PRD-B', lotSerial: 'DEV-WS-103', status: 21, opNo: 'OP-PACK', customerNo: 'CUS-B', waitDescription: '已解除', ruleNo: '', createQty: 8, creator: 'DEV', createDate: stamp(5), releaseQty: 8, releaser: 'DEV', releaseDate: stamp(4), waitSource: 0 },
    { waitNo: 'DEV-WB-004', lotNo: 'DEV-WAIT-004', productNo: 'PRD-A', lotSerial: 'DEV-WS-104', status: 0, opNo: 'OP-TEST', customerNo: 'CUS-A', waitDescription: '等待工程處置', ruleNo: 'RULE-A', createQty: 8, creator: 'DEV', createDate: stamp(0.5), releaseQty: null, releaser: '', releaseDate: null, waitSource: 0 },
    { waitNo: 'DEV-WB-005', lotNo: 'DEV-WAIT-005', productNo: 'PRD-B', lotSerial: 'DEV-WS-105', status: 0, opNo: 'OP-PACK', customerNo: 'CUS-B', waitDescription: '部分過帳等待', ruleNo: '', createQty: 5, creator: 'DEV', createDate: stamp(0.2), releaseQty: null, releaser: '', releaseDate: null, waitSource: 0 },
  ]
}
function load(): void {
  if (!dev.value) return
  const result = sampleRows()
  const detail: Disposition[] = [
    { waitNo: 'DEV-WB-002', lotDispType: 1, description: '等待主管確認', creator: 'DEV', createDate: stamp(1.5), opNo: 'OP-TEST', processNo: 'PROC-A', processVersion: '1' },
    { waitNo: 'DEV-WB-003', lotDispType: 2, description: '同意解除', creator: 'DEV', createDate: stamp(4), opNo: 'OP-PACK', processNo: 'PROC-B', processVersion: '1' },
  ]
  try {
    const saved = JSON.parse(window.localStorage.getItem(wp502StorageKey) || 'null') as unknown
    if (saved && typeof saved === 'object' && 'waits' in saved && 'lots' in saved && Array.isArray(saved.waits) && Array.isArray(saved.lots)) {
      for (const raw of saved.waits) {
        if (!raw || typeof raw.waitNo !== 'string' || typeof raw.lotNo !== 'string') continue
        const lot = saved.lots.find(item => item && item.lotNo === raw.lotNo)
        if (!lot || result.some(item => item.waitNo === raw.waitNo)) continue
        result.push({ waitNo: raw.waitNo, lotNo: raw.lotNo, productNo: lot.productNo || '', lotSerial: lot.lotSerial || '', status: 0, opNo: raw.opNo || lot.opNo || '', customerNo: lot.customerNo || '', waitDescription: raw.description || '', ruleNo: '', createQty: Number(raw.createQty) || 0, creator: raw.actor || 'DEV', createDate: raw.createdAt || stamp(0), releaseQty: null, releaser: '', releaseDate: null, waitSource: 0 })
      }
    }
  } catch { /* show built-in DEV examples */ }
  try {
    const saved = JSON.parse(window.localStorage.getItem(wp511StorageKey) || 'null') as { releases?: { waitNo: string; lotDispType: number; description: string; actor: string; releasedAt: string }[] } | null
    if (Array.isArray(saved?.releases)) for (const release of saved.releases) {
      const row = result.find(item => item.waitNo === release.waitNo)
      if (!row) continue
      row.status = 21; row.releaseQty = row.createQty; row.releaser = release.actor; row.releaseDate = release.releasedAt
      detail.push({ waitNo: row.waitNo, lotDispType: release.lotDispType, description: release.description, creator: release.actor, createDate: release.releasedAt, opNo: row.opNo, processNo: '', processVersion: '' })
    }
  } catch { /* show original DEV rows */ }
  allRows.value = result
  dispositions.value = detail
}
function chooseLookup(value: string): void { if (lookup.value) { fields[lookup.value] = value; flags[lookup.value] = true }; lookup.value = null }
function query(): void {
  error.value = ''; lookup.value = null
  if (!dev.value) { error.value = '此頁目前只提供 DEV 假資料。'; return }
  for (const key of Object.keys(fields) as (keyof typeof fields)[]) fields[key] = fields[key].trim().toUpperCase()
  const textActive = (Object.keys(fields) as (keyof typeof fields)[]).some(key => flags[key] && !!fields[key])
  if (!textActive && !flags.createDate && !flags.releaseDate) { error.value = '請至少勾選並輸入一項有效查詢條件。'; return }
  if (flags.createDate && (!dates.createFrom || !dates.createTo || dates.createFrom > dates.createTo)) { error.value = '建立日期區間無效。'; return }
  if (flags.releaseDate && (!dates.releaseFrom || !dates.releaseTo || dates.releaseFrom > dates.releaseTo)) { error.value = '解除日期區間無效。'; return }
  rows.value = allRows.value.filter(item => item.waitSource === 0 &&
    (!flags.lotNo || !fields.lotNo || item.lotNo.includes(fields.lotNo)) &&
    (!flags.customerNo || !fields.customerNo || item.customerNo === fields.customerNo) &&
    (!flags.opNo || !fields.opNo || item.opNo === fields.opNo) &&
    (!flags.waitNo || !fields.waitNo || item.waitNo === fields.waitNo) &&
    (!flags.createDate || (item.createDate >= new Date(dates.createFrom).toISOString() && item.createDate <= new Date(dates.createTo).toISOString())) &&
    (!flags.releaseDate || (item.releaseDate !== null && item.releaseDate >= new Date(dates.releaseFrom).toISOString() && item.releaseDate <= new Date(dates.releaseTo).toISOString())))
    .sort((a, b) => a.waitNo.localeCompare(b.waitNo))
  searched.value = true; selectedWaitNo.value = rows.value[0]?.waitNo || ''
  message.value = `查得 ${rows.value.length} 筆等待單。`
}
function openInfo(): void { if (selected.value) infoWaitNo.value = selected.value.waitNo }
function displayDate(value: string | null): string { return value ? value.replace('T', ' ').slice(0, 19) : '' }
for (const key of ['lotNo', 'customerNo', 'opNo', 'waitNo'] as const) {
  watch(() => flags[key], checked => { if (!checked) fields[key] = '' })
}
onMounted(() => {
  const now = new Date(); const weekAgo = new Date(now.getTime() - 7 * 86400000)
  dates.createFrom = localDate(weekAgo); dates.createTo = localDate(now)
  dates.releaseFrom = localDate(weekAgo); dates.releaseTo = localDate(now)
  load()
})
watch(dev, enabled => { if (enabled) load(); else { allRows.value = []; rows.value = []; infoWaitNo.value = ''; searched.value = false } })
</script>

<template>
  <section class="wait-page">
    <h1>等待單資料 <small>WAIT BASIS · WP510</small></h1>
    <p v-if="dev" class="dev-note">DEV 假資料；包含 WP502 已建立的本機等待單。</p>
    <p v-if="error" role="alert" class="error">{{ error }}</p>
    <fieldset class="query"><legend>Query</legend>
      <div class="query-grid">
        <label class="query-field"><span><input v-model="flags.lotNo" type="checkbox"> Lot No</span><input v-model="fields.lotNo" class="yellow" :disabled="!dev || !flags.lotNo" @keydown.enter.prevent="query"></label>
        <label class="query-field"><span><input v-model="flags.waitNo" type="checkbox"> Wait No</span><input v-model="fields.waitNo" class="yellow" :disabled="!dev || !flags.waitNo" @keydown.enter.prevent="query"></label>
        <label class="query-field"><span><input v-model="flags.opNo" type="checkbox"> OP No</span><input v-model="fields.opNo" class="yellow" :disabled="!dev || !flags.opNo" @keydown.enter.prevent="query"><button type="button" :disabled="!dev" @click="lookup = lookup === 'opNo' ? null : 'opNo'">...</button></label>
        <label class="query-field"><span><input v-model="flags.customerNo" type="checkbox"> Customer No</span><input v-model="fields.customerNo" class="yellow" :disabled="!dev || !flags.customerNo" @keydown.enter.prevent="query"><button type="button" :disabled="!dev" @click="lookup = lookup === 'customerNo' ? null : 'customerNo'">...</button></label>
        <label class="query-field date"><span><input v-model="flags.createDate" type="checkbox"> Create Date</span><input v-model="dates.createFrom" type="datetime-local" step="1" :disabled="!dev || !flags.createDate"> To <input v-model="dates.createTo" type="datetime-local" step="1" :disabled="!dev || !flags.createDate"></label>
        <label class="query-field date"><span><input v-model="flags.releaseDate" type="checkbox"> Release Date</span><input v-model="dates.releaseFrom" type="datetime-local" step="1" :disabled="!dev || !flags.releaseDate"> To <input v-model="dates.releaseTo" type="datetime-local" step="1" :disabled="!dev || !flags.releaseDate"></label>
        <button type="button" class="button query-button" :disabled="!dev" @click="query">Query (Q)</button>
      </div>
      <div v-if="lookup" class="lookup"><strong>{{ lookup }} 查找</strong><button v-for="option in lookupOptions" :key="option" type="button" @click="chooseLookup(option)">{{ option }}</button></div>
    </fieldset>
    <fieldset class="result"><legend>Result</legend>
      <div class="result-body"><div class="grid-wrap"><table><thead><tr><th>Wait No</th><th>Lot No</th><th>Product No</th><th>Lot Serial</th><th>Status</th><th>OPNo</th><th>CustomerNo</th><th>Wait Description</th><th>RuleNo</th><th>CreateQty</th><th>Creator</th><th>Create Date</th><th>Release Qty</th><th>Release UserNo</th><th>Release Date</th></tr></thead><tbody>
        <tr v-for="row in rows" :key="row.waitNo" :class="{ selected: selectedWaitNo === row.waitNo }" @click="selectedWaitNo = row.waitNo" @dblclick="openInfo"><td>{{ row.waitNo }}</td><td>{{ row.lotNo }}</td><td>{{ row.productNo }}</td><td>{{ row.lotSerial }}</td><td>{{ statuses[row.status] || '' }}</td><td>{{ row.opNo }}</td><td>{{ row.customerNo }}</td><td>{{ row.waitDescription }}</td><td>{{ row.ruleNo }}</td><td class="num">{{ row.createQty }}</td><td>{{ row.creator }}</td><td>{{ displayDate(row.createDate) }}</td><td class="num">{{ row.releaseQty ?? '' }}</td><td>{{ row.releaser }}</td><td>{{ displayDate(row.releaseDate) }}</td></tr>
        <tr v-if="!rows.length"><td class="empty" colspan="15">{{ searched ? '沒有符合條件的等待單。' : '請先查詢。' }}</td></tr>
      </tbody></table></div><div class="actions"><button type="button" class="button" :disabled="!selected" @click="openInfo">Wait Info</button><button type="button" class="button" @click="emit('close')">Close (X)</button></div></div>
      <div class="status"><span>UserNo: {{ currentUserNo || 'DEV' }}</span><span>{{ message }}</span><span>{{ rows.length }} 筆</span></div>
    </fieldset>
    <div v-if="info" class="overlay"><div class="dialog" role="dialog" aria-modal="true" aria-label="Wait Information"><h2>Wait Information</h2>
      <fieldset><legend>Wait Basis</legend><label>Lot No<input :value="info.lotNo" readonly></label><label>Wait No<input :value="info.waitNo" readonly></label></fieldset>
      <div class="grid-wrap info-grid"><table><thead><tr><th>LotDispType</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>OPNo</th><th>ProcessNo</th><th>ProcessVersion</th></tr></thead><tbody><tr v-for="row in infoRows" :key="row.waitNo + row.createDate + row.lotDispType"><td>{{ dispositionTypes[row.lotDispType] || row.lotDispType }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ displayDate(row.createDate) }}</td><td>{{ row.opNo }}</td><td>{{ row.processNo }}</td><td>{{ row.processVersion }}</td></tr><tr v-if="!infoRows.length"><td class="empty" colspan="7">沒有等待處置明細。</td></tr></tbody></table></div>
      <div class="dialog-buttons"><button type="button" class="button" @click="infoWaitNo = ''">Close</button></div>
    </div></div>
  </section>
</template>

<style scoped>
.wait-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:7px;padding:9px;box-sizing:border-box;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif}h1{margin:0;font-size:18px}h1 small{font-size:12px;font-weight:400;color:#777}.dev-note{margin:0;padding:6px 9px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}.error{margin:0;padding:5px 8px;border:1px solid #d6aaa6;background:#fff0ef;color:#99251e}fieldset{min-width:0;margin:0;padding:7px 8px;border:1px solid #bbb;background:#fff}legend{padding:0 5px}.query{position:relative}.query-grid{display:grid;grid-template-columns:330px 330px 1fr;gap:7px 9px;align-items:center}.query-field{display:flex;align-items:center;gap:5px;min-width:0}.query-field>span{min-width:118px;white-space:nowrap}.query-field>input:not([type=checkbox]){height:25px;min-width:0;width:162px;box-sizing:border-box;padding:3px 5px;border:1px solid #888;font:inherit}.query-field input.yellow{background:#fff0b3}.query-field.date{grid-column:1/3}.query-field.date>input{width:165px}.query-field button,.button,.lookup button{min-height:27px;padding:3px 9px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#f2f2f2);color:#333;font:inherit;cursor:pointer}.button:disabled,.query-field button:disabled{color:#aaa;cursor:default}.query-button{grid-column:3;grid-row:4;justify-self:end}.lookup{position:absolute;z-index:3;left:8px;top:100%;display:flex;gap:6px;align-items:center;flex-wrap:wrap;padding:7px;border:1px solid #aaa;background:#fffbe9;box-shadow:0 3px 9px #0002}.result{flex:1;min-height:180px;display:flex;flex-direction:column}.result-body{display:flex;flex:1;min-height:0;gap:9px}.grid-wrap{flex:1;min-width:0;overflow:auto;border:1px solid #aaa}.grid-wrap table{border-collapse:collapse;min-width:1450px;white-space:nowrap}.grid-wrap th{position:sticky;top:0;background:#ead591;font-weight:500}.grid-wrap th,.grid-wrap td{border:1px solid #d4d4d4;padding:5px 7px;text-align:left}.grid-wrap td{background:#ffe89c}.grid-wrap tr.selected td{background:#ffdb73}.grid-wrap td.num{text-align:right}.grid-wrap .empty{text-align:center;background:#fff;color:#777;height:38px}.actions{display:flex;flex-direction:column;justify-content:space-between;width:83px}.status{display:flex;justify-content:space-between;gap:8px;border-top:1px solid #ddd;margin-top:6px;padding-top:5px;color:#666}.overlay{position:fixed;inset:0;z-index:220;display:flex;align-items:center;justify-content:center;padding:18px;background:#0007}.dialog{display:flex;flex-direction:column;gap:10px;width:min(840px,100%);max-height:calc(100vh - 36px);overflow:auto;padding:14px;border:1px solid #888;background:#f7f5ee;box-shadow:0 8px 28px #0005}.dialog h2{margin:0;font-size:17px}.dialog fieldset{display:flex;gap:20px}.dialog label{display:flex;align-items:center;gap:6px}.dialog input{height:25px;border:1px solid #888;padding:3px 5px}.dialog .info-grid{min-height:200px}.dialog .info-grid table{min-width:810px}.dialog-buttons{display:flex;justify-content:flex-end}@media(max-width:950px){.query-grid{grid-template-columns:1fr 1fr}.query-field.date{grid-column:1/-1}.query-button{grid-column:2;grid-row:auto}.result-body{min-height:200px}}
</style>
