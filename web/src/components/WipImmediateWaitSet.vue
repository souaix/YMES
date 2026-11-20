<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'

type LookupField = 'areaNo' | 'psNo' | 'opNo' | 'customerNo' | 'productNo' | 'moNo'
type Lot = {
  lotNo: string; baseLotNo: string; lotSerial: string; areaNo: string; psNo: string; processNo: string
  opNo: string; nodeId: string; brNo: string; customerNo: string; productNo: string; moNo: string
  status: number; lotState: number; curQty: number; curUnitNo: string; sysQty: number; sysUnitNo: string
  partialMode: boolean; partialType: number; linkName: string; packed: boolean; lotStamp: number; waitNo: string
}
type WaitRecord = { waitNo: string; batchWaitNo: string; lotNo: string; opNo: string; previousStatus: number; createQty: number; description: string; actor: string; createdAt: string }
type MockState = { lots: Lot[]; waits: WaitRecord[]; nextSerial: number }

const props = defineProps<{ developmentPreview?: boolean; currentUserNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview !== false)
const unavailableMessage = '立即等待服務尚未提供，暫無法查詢或操作。'
const storageKey = 'ymes-dev-mock:wip-immediate-wait:state:v1'
const statuses: Record<number, string> = { 0: 'Queue', 1: 'Running', 2: 'Wait', 4: 'Complete' }
const queryFields: { key: LookupField; label: string }[] = [
  { key: 'areaNo', label: 'Area No' }, { key: 'psNo', label: 'PS No' }, { key: 'opNo', label: 'OP No' },
  { key: 'customerNo', label: 'Customer No' }, { key: 'productNo', label: 'Product No' }, { key: 'moNo', label: 'MO No' },
]
const fixtures: Lot[] = [
  { lotNo: 'DEV-WAIT-001', baseLotNo: 'DEV-BASE-WAIT-01', lotSerial: 'DEV-WS-101', areaNo: 'A01', psNo: 'PS-01', processNo: 'PROC-A', opNo: 'OP-TEST', nodeId: 'N10', brNo: 'BR-TEST', customerNo: 'CUS-A', productNo: 'PRD-A', moNo: 'MO-501', status: 0, lotState: 1, curQty: 12, curUnitNo: 'PCS', sysQty: 12, sysUnitNo: 'PCS', partialMode: false, partialType: -1, linkName: '', packed: false, lotStamp: 50201, waitNo: '' },
  { lotNo: 'DEV-WAIT-002', baseLotNo: 'DEV-BASE-WAIT-02', lotSerial: 'DEV-WS-102', areaNo: 'A01', psNo: 'PS-01', processNo: 'PROC-A', opNo: 'OP-TEST', nodeId: 'N10', brNo: 'BR-TEST', customerNo: 'CUS-A', productNo: 'PRD-A', moNo: 'MO-501', status: 1, lotState: 1, curQty: 6, curUnitNo: 'PCS', sysQty: 6, sysUnitNo: 'PCS', partialMode: true, partialType: 0, linkName: '', packed: false, lotStamp: 50202, waitNo: '' },
  { lotNo: 'DEV-WAIT-PACK', baseLotNo: 'DEV-BASE-WAIT-03', lotSerial: 'DEV-WS-103', areaNo: 'A02', psNo: 'PS-02', processNo: 'PROC-B', opNo: 'OP-PACK', nodeId: 'N20', brNo: 'BR-PACK', customerNo: 'CUS-B', productNo: 'PRD-B', moNo: 'MO-502', status: 4, lotState: 1, curQty: 8, curUnitNo: 'PCS', sysQty: 8, sysUnitNo: 'PCS', partialMode: false, partialType: -1, linkName: '', packed: true, lotStamp: 50203, waitNo: '' },
  { lotNo: 'DEV-WAIT-LINK', baseLotNo: 'DEV-BASE-WAIT-04', lotSerial: 'DEV-WS-104', areaNo: 'A02', psNo: 'PS-02', processNo: 'PROC-B', opNo: 'OP-PACK', nodeId: 'N20', brNo: 'BR-PACK', customerNo: 'CUS-B', productNo: 'PRD-B', moNo: 'MO-502', status: 4, lotState: 1, curQty: 5, curUnitNo: 'PCS', sysQty: 5, sysUnitNo: 'PCS', partialMode: false, partialType: -1, linkName: 'HOLD', packed: false, lotStamp: 50204, waitNo: '' },
  { lotNo: 'DEV-WAIT-HELD', baseLotNo: 'DEV-BASE-WAIT-05', lotSerial: 'DEV-WS-105', areaNo: 'A01', psNo: 'PS-01', processNo: 'PROC-A', opNo: 'OP-TEST', nodeId: 'N10', brNo: 'BR-TEST', customerNo: 'CUS-A', productNo: 'PRD-A', moNo: 'MO-501', status: 2, lotState: 1, curQty: 3, curUnitNo: 'PCS', sysQty: 3, sysUnitNo: 'PCS', partialMode: false, partialType: -1, linkName: 'WAIT', packed: false, lotStamp: 50205, waitNo: 'OLD-WAIT' },
]

const state = ref<MockState>({ lots: [], waits: [], nextSerial: 1 })
const filters = reactive({ areaNo: '', psNo: '', opNo: '', customerNo: '', productNo: '', moNo: '', lotNo: '' })
const lookupField = ref<LookupField | null>(null)
const lookupOptions = computed(() => lookupField.value ? [...new Set(state.value.lots.map(item => item[lookupField.value!]))].sort() : [])
const rows = ref<Lot[]>([])
const selected = ref<string[]>([])
const scanLotNo = ref('')
const searched = ref(false)
const modalFirst = ref<Lot | null>(null)
const pending = ref<Lot[]>([])
const description = ref('')
const errorText = ref('')
const message = ref('請輸入至少一個條件查詢批號，再勾選並設定等待。')

function seed(): MockState { return { lots: fixtures.map(item => ({ ...item })), waits: [], nextSerial: 1 } }
function load(): void {
  if (!dev.value) return
  try {
    const data = JSON.parse(window.localStorage.getItem(storageKey) || 'null') as unknown
    if (data && typeof data === 'object' && 'lots' in data && 'waits' in data && 'nextSerial' in data && Array.isArray(data.lots) && Array.isArray(data.waits)) { state.value = data as MockState; return }
  } catch { /* use fixtures */ }
  state.value = seed()
}
function norm(value: string): string { return value.trim().toUpperCase() }
function chooseLookup(value: string): void { if (lookupField.value) filters[lookupField.value] = value; lookupField.value = null }
function query(): void {
  errorText.value = ''; lookupField.value = null
  if (!dev.value) { errorText.value = unavailableMessage; return }
  for (const key of Object.keys(filters) as (keyof typeof filters)[]) filters[key] = norm(filters[key])
  if (!Object.values(filters).some(Boolean)) { errorText.value = '請至少輸入一個查詢條件。'; return }
  rows.value = state.value.lots.filter(item => [0, 1, 4].includes(item.status) && item.curQty > 0 && item.lotState !== 0 && !(item.partialMode && item.status > 1) && !(item.status === 4 && ['HOLD', 'WAIT'].includes(item.linkName)) &&
    (Object.keys(filters) as (keyof typeof filters)[]).every(key => !filters[key] || item[key] === filters[key]))
  selected.value = []; searched.value = true
  message.value = `查得 ${rows.value.length} 筆批號。`
}
function scan(): void {
  const lotNo = norm(scanLotNo.value); scanLotNo.value = ''
  if (rows.value.some(item => item.lotNo === lotNo) && !selected.value.includes(lotNo)) selected.value = [...selected.value, lotNo]
}
function openSet(): void {
  errorText.value = ''
  const chosen = rows.value.filter(item => selected.value.includes(item.lotNo))
  if (!dev.value || !chosen.length) { errorText.value = '請至少選一筆 Lot。'; return }
  const packed = chosen.find(item => item.packed)
  if (packed) { errorText.value = `${packed.lotNo} 已包裝，這批選取資料不能設定 Wait。`; return }
  pending.value = chosen.map(item => ({ ...item })); modalFirst.value = pending.value[0] || null; description.value = ''
}
function confirmWait(): void {
  if (!dev.value || !modalFirst.value || !pending.value.length) return
  errorText.value = ''
  const total = pending.value.length
  let done = 0; let batchWaitNo = ''; let failed = ''
  for (const selectedLot of pending.value) {
    let current: MockState
    try {
      const data = JSON.parse(window.localStorage.getItem(storageKey) || 'null') as unknown
      current = data && typeof data === 'object' && 'lots' in data && 'waits' in data && 'nextSerial' in data && Array.isArray(data.lots) && Array.isArray(data.waits) ? data as MockState : seed()
    } catch { failed = '無法讀取 DEV 模擬狀態。'; break }
    const lot = current.lots.find(item => item.lotNo === selectedLot.lotNo && item.opNo === selectedLot.opNo)
    if (!lot || lot.lotStamp !== selectedLot.lotStamp || lot.status !== selectedLot.status || lot.curQty !== selectedLot.curQty || lot.curQty <= 0 || lot.lotState === 0 || lot.packed || ![0, 1, 4].includes(lot.status) || (lot.partialMode && lot.status > 1) || (lot.status === 4 && ['HOLD', 'WAIT'].includes(lot.linkName))) {
      failed = `${selectedLot.lotNo} 狀態或 LotStamp 已變更，請重新查詢。`; break
    }
    const waitNo = `DEV-WAIT-${String(current.nextSerial).padStart(4, '0')}`
    if (current.waits.some(item => item.waitNo === waitNo)) { failed = `${waitNo} 已使用。`; break }
    if (!batchWaitNo) batchWaitNo = waitNo
    current.waits.push({ waitNo, batchWaitNo, lotNo: lot.lotNo, opNo: lot.opNo, previousStatus: lot.status, createQty: lot.curQty, description: description.value.trim(), actor: props.currentUserNo || 'DEV', createdAt: new Date().toISOString() })
    current.nextSerial += 1; lot.waitNo = waitNo; lot.status = 2; lot.linkName = 'WAIT'; lot.lotStamp += 1
    try { window.localStorage.setItem(storageKey, JSON.stringify(current)) }
    catch { failed = '瀏覽器無法儲存 DEV 模擬狀態。'; break }
    state.value = current; done += 1
  }
  modalFirst.value = null; pending.value = []; query()
  if (failed) errorText.value = `已成功 ${done}/${total} 筆；${failed}。原 WinForms 也是逐批執行，可能部分完成。`
  else message.value = `DEV 已模擬設定 ${done} 筆 Wait；BatchWaitNo ${batchWaitNo}。未更新 MES。`
}
onMounted(load)
watch(dev, enabled => { if (enabled) load(); else { rows.value = []; selected.value = []; modalFirst.value = null; searched.value = false } })
</script>

<template>
  <section class="wp502-page">
    <div v-if="dev" class="dev-banner">DEV 假資料：查 A01 或 MO-501 可勾選兩筆；DEV-WAIT-PACK 示範已包裝拒絕。</div>
    <h1>立即等待設定 <small>IMMEDIATE WAIT SET · WP502</small></h1>
    <p v-if="!dev" class="notice" role="status">{{ unavailableMessage }}</p><p v-else-if="errorText" class="notice error" role="alert">{{ errorText }}</p><p v-else class="notice" role="status">{{ message }}</p>
    <fieldset class="query-frame"><legend>Lot Query</legend>
      <div class="query-grid">
        <label v-for="field in queryFields" :key="field.key">
          {{ field.label }}
          <input v-model="filters[field.key]" class="mes-input yellow" :disabled="!dev" @keydown.enter.prevent="query">
          <button type="button" class="lookup-button" :disabled="!dev" @click.prevent="lookupField = lookupField === field.key ? null : field.key">...</button>
        </label>
        <label>LotNo<input v-model="filters.lotNo" class="mes-input yellow" :disabled="!dev" @keydown.enter.prevent="query"></label>
        <button type="button" class="mes-button" :disabled="!dev" @click="query">Query</button>
      </div>
      <div v-if="dev && lookupField" class="lookup-options"><strong>{{ lookupField }} 查找</strong><button v-for="option in lookupOptions" :key="option" type="button" @click="chooseLookup(option)">{{ option }}</button></div>
    </fieldset>
    <fieldset class="lot-frame"><legend>Lot</legend>
      <div class="selection-bar"><button type="button" class="mes-button" :disabled="!dev || !rows.length" @click="selected = rows.map(item => item.lotNo)">Select All</button><button type="button" class="mes-button" :disabled="!dev || !rows.length" @click="selected = []">UnSelect All</button><label>LotNo<input v-model="scanLotNo" class="mes-input yellow" :disabled="!dev" @keydown.enter.prevent="scan"></label></div>
      <div class="grid-wrap"><table><thead><tr><th>Select</th><th>LotNo</th><th>Status</th><th>LotSerial</th><th>AreaNo</th><th>PSNo</th><th>ProcessNo</th><th>OPNo</th><th>NodeID</th><th>BRNo</th><th>CurQTY</th><th>CurUnitNo</th><th>SysQTY</th><th>SysUnitNo</th><th>BaseLotNo</th></tr></thead><tbody>
        <tr v-for="item in rows" :key="item.lotNo"><td><input v-model="selected" type="checkbox" :value="item.lotNo" :disabled="!dev"></td><td>{{ item.lotNo }}</td><td>{{ statuses[item.status] }}</td><td>{{ item.lotSerial }}</td><td>{{ item.areaNo }}</td><td>{{ item.psNo }}</td><td>{{ item.processNo }}</td><td>{{ item.opNo }}</td><td>{{ item.nodeId }}</td><td>{{ item.brNo }}</td><td class="num">{{ item.curQty }}</td><td>{{ item.curUnitNo }}</td><td class="num">{{ item.sysQty }}</td><td>{{ item.sysUnitNo }}</td><td>{{ item.baseLotNo }}</td></tr>
        <tr v-if="!rows.length"><td colspan="15" class="empty">{{ searched ? '沒有符合條件的生產批。' : '請先查詢。' }}</td></tr>
      </tbody></table></div>
      <div class="footer"><button type="button" class="mes-button primary" :disabled="!dev || !selected.length" @click="openSet">Set</button><button type="button" class="mes-button" @click="emit('close')">Close</button></div>
    </fieldset>
    <div v-if="dev && modalFirst" class="modal-backdrop"><div class="wait-dialog" role="dialog" aria-modal="true" aria-label="Wait Description">
      <h2>Wait Description</h2><fieldset><legend>Lot</legend><div class="dialog-fields"><label>LotNo<input :value="modalFirst.lotNo" readonly class="mes-input"></label><label>CurQTY<input :value="modalFirst.curQty" readonly class="mes-input"></label><label>CurUnitNo<input :value="modalFirst.curUnitNo" readonly class="mes-input"></label></div></fieldset>
      <fieldset><legend>Wait Data</legend><label class="description-field">Description<textarea v-model="description" class="yellow"></textarea></label><small>此說明會套用到選取的 {{ pending.length }} 筆批號；原視窗允許空白說明。</small></fieldset>
      <div class="footer"><button type="button" class="mes-button primary" @click="confirmWait">Confirm</button><button type="button" class="mes-button" @click="modalFirst = null; pending = []">Close</button></div>
    </div></div>
  </section>
</template>

<style scoped>
.wp502-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:7px;padding:9px;box-sizing:border-box;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif}.dev-banner{padding:6px 9px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}h1{margin:0;font-size:18px}h1 small{font-size:12px;font-weight:400;color:#777}.notice{margin:0;min-height:18px}.notice.error{padding:5px 8px;border:1px solid #d6aaa6;background:#fff0ef;color:#99251e}fieldset{min-width:0;margin:0;padding:7px 8px;border:1px solid #bbb;background:#fff}legend{padding:0 5px}.query-frame{position:relative}.query-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:6px}.query-grid label,.selection-bar label,.dialog-fields label{display:flex;align-items:center;justify-content:flex-end;gap:4px;white-space:nowrap}.mes-input{height:26px;min-width:0;padding:3px 5px;box-sizing:border-box;border:1px solid #888;font:inherit}.query-grid .mes-input{width:105px}.yellow{background:#fff0b3!important}.mes-button,.lookup-button{min-height:29px;padding:3px 10px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#f2f2f2);color:#333;font:inherit;cursor:pointer}.lookup-button{min-width:27px;padding:1px}.mes-button:disabled,.lookup-button:disabled{color:#999;cursor:default}.mes-button.primary{background:#f3a62a;border-color:#e99512;color:#fff}.lookup-options{position:absolute;z-index:5;top:100%;left:8px;display:flex;align-items:center;flex-wrap:wrap;gap:6px;padding:7px;border:1px solid #aaa;background:#fffbe9;box-shadow:0 3px 9px #0002}.lookup-options button{padding:3px 7px;border:1px solid #bbb;background:#fff;cursor:pointer}.lot-frame{flex:1;min-height:200px;display:flex;flex-direction:column}.selection-bar{display:flex;align-items:center;gap:7px;padding-bottom:6px}.selection-bar .mes-input{width:150px}.grid-wrap{flex:1;min-height:100px;overflow:auto;border:1px solid #aaa}.grid-wrap table{border-collapse:collapse;min-width:1240px;white-space:nowrap}.grid-wrap th{position:sticky;top:0;background:#ead591;border:1px solid #d4d4d4;padding:5px 7px;text-align:left;font-weight:500}.grid-wrap td{border:1px solid #d4d4d4;padding:4px 6px;background:#ffe89c}.grid-wrap .num{text-align:right}.empty{text-align:center!important;background:#fff!important;color:#777;height:36px}.footer{display:flex;justify-content:flex-end;gap:8px;padding-top:7px}.modal-backdrop{position:fixed;inset:0;z-index:200;display:flex;align-items:center;justify-content:center;padding:20px;background:#0007}.wait-dialog{display:flex;flex-direction:column;gap:8px;width:min(528px,100%);padding:12px;border:1px solid #888;background:#f7f5ee;box-shadow:0 8px 28px #0005}.wait-dialog h2{margin:0;font-size:17px}.dialog-fields{display:flex;gap:8px}.dialog-fields label{flex:1;display:flex;flex-direction:column;align-items:stretch}.dialog-fields .mes-input{width:100%}.description-field{display:flex;gap:8px}.description-field textarea{flex:1;height:176px;padding:5px;border:1px solid #888;font:inherit}.wait-dialog small{color:#777}@media(max-width:900px){.query-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.dialog-fields{flex-wrap:wrap}}
</style>
