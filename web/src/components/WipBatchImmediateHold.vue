<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'

type FilterKey = 'roNo' | 'moNo' | 'productNo' | 'areaNo' | 'opNo' | 'lotNo' | 'customerNo' | 'psNo' | 'equipmentNo' | 'componentNo'
type Lot = { lotNo: string; baseLotNo: string; roNo: string; moNo: string; productNo: string; productVersion: string; areaNo: string; opNo: string; customerNo: string; psNo: string; equipmentNo: string; components: string[]; checkInTime: string; status: number; lotState: number; partialMode: boolean; curQty: number; curUnitNo: string; sysQty: number; sysUnitNo: string; lotStamp: number; hotLot: boolean; holdTimes: number; erfNos: string[] }
type Reason = { key: string; holdItemNo: string; holdItemName: string; componentNo: string; areaNo: string; qty: number | ''; description: string; createERF: boolean }
type ERF = { erfNo: string; erfType: 2 | 6; refLotNo: string; lotNos: string[]; holdDescription: string; mClassNo: string; reasons: Reason[]; createERF: boolean; releaseEQP: boolean; equipmentState: string; attachmentNames: string[]; actor: string; createdAt: string }
type MockState = { lots: Lot[]; erfs: ERF[]; nextSerial: number }

const props = defineProps<{ developmentPreview?: boolean; currentUserNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview !== false)
const unavailableMessage = '批次立即異常服務尚未提供，暫無法查詢或操作。'
const storageKey = 'ymes-dev-mock:wip-batch-immediate-hold:state:v1'
const multipleHoldEnabled = true
const fields: { key: FilterKey; label: string; lookup: boolean }[] = [
  { key: 'roNo', label: 'RO No', lookup: true }, { key: 'moNo', label: 'MO No', lookup: true }, { key: 'productNo', label: 'Product No', lookup: true },
  { key: 'areaNo', label: 'Area No', lookup: true }, { key: 'opNo', label: 'OP No', lookup: true }, { key: 'lotNo', label: 'LotNo', lookup: false },
  { key: 'customerNo', label: 'Customer No', lookup: true }, { key: 'psNo', label: 'PS No', lookup: false },
  { key: 'equipmentNo', label: 'Equipment No', lookup: true }, { key: 'componentNo', label: 'Component No', lookup: false },
]
const statuses: Record<number, string> = { 0: 'Queue', 1: 'Running', 3: 'Hold', 4: 'Complete' }
const holdItems = [
  { mClassNo: 'M-QUALITY', sClassNo: 'S-VISUAL', holdItemNo: 'H-001', holdItemName: '外觀異常', description: '外觀檢查異常', createERF: true },
  { mClassNo: 'M-QUALITY', sClassNo: 'S-VISUAL', holdItemNo: 'H-002', holdItemName: '尺寸異常', description: '尺寸超出規格', createERF: true },
  { mClassNo: 'M-EQUIPMENT', sClassNo: 'S-EQP', holdItemNo: 'H-003', holdItemName: '設備異常', description: '設備點檢異常', createERF: true },
]
const fixtures: Lot[] = [
  { lotNo: 'DEV-BH-001', baseLotNo: 'DEV-BH-BASE-01', roNo: 'RO-501', moNo: 'MO-501', productNo: 'PRD-A', productVersion: '1', areaNo: 'A01', opNo: 'OP-TEST', customerNo: 'CUS-A', psNo: 'PS-01', equipmentNo: 'EQ-01', components: ['COMP-01'], checkInTime: '2026-11-20T08:30:00', status: 0, lotState: 1, partialMode: false, curQty: 6, curUnitNo: 'PCS', sysQty: 6, sysUnitNo: 'PCS', lotStamp: 50301, hotLot: false, holdTimes: 0, erfNos: [] },
  { lotNo: 'DEV-BH-002', baseLotNo: 'DEV-BH-BASE-02', roNo: 'RO-501', moNo: 'MO-501', productNo: 'PRD-A', productVersion: '1', areaNo: 'A01', opNo: 'OP-TEST', customerNo: 'CUS-A', psNo: 'PS-01', equipmentNo: 'EQ-01', components: ['COMP-02'], checkInTime: '2026-11-20T09:00:00', status: 1, lotState: 1, partialMode: false, curQty: 6, curUnitNo: 'PCS', sysQty: 6, sysUnitNo: 'PCS', lotStamp: 50302, hotLot: true, holdTimes: 0, erfNos: [] },
  { lotNo: 'DEV-BH-PART', baseLotNo: 'DEV-BH-BASE-03', roNo: 'RO-502', moNo: 'MO-502', productNo: 'PRD-B', productVersion: '1', areaNo: 'A02', opNo: 'OP-PART', customerNo: 'CUS-B', psNo: 'PS-02', equipmentNo: 'EQ-02', components: [], checkInTime: '2026-11-20T10:00:00', status: 0, lotState: 1, partialMode: true, curQty: 4, curUnitNo: 'PCS', sysQty: 4, sysUnitNo: 'PCS', lotStamp: 50303, hotLot: false, holdTimes: 0, erfNos: [] },
  { lotNo: 'DEV-BH-HELD', baseLotNo: 'DEV-BH-BASE-04', roNo: 'RO-502', moNo: 'MO-502', productNo: 'PRD-B', productVersion: '1', areaNo: 'A02', opNo: 'OP-PART', customerNo: 'CUS-B', psNo: 'PS-02', equipmentNo: 'EQ-02', components: [], checkInTime: '2026-11-20T10:30:00', status: 3, lotState: 1, partialMode: false, curQty: 3, curUnitNo: 'PCS', sysQty: 3, sysUnitNo: 'PCS', lotStamp: 50304, hotLot: false, holdTimes: 1, erfNos: ['DEV-ERF-OLD'] },
]

const state = ref<MockState>({ lots: [], erfs: [], nextSerial: 1 })
const filters = reactive<Record<FilterKey, string>>({ roNo: '', moNo: '', productNo: '', areaNo: '', opNo: '', lotNo: '', customerNo: '', psNo: '', equipmentNo: '', componentNo: '' })
const timeFrom = ref(''); const timeTo = ref('')
const lookupField = ref<FilterKey | null>(null)
const lookupOptions = computed(() => lookupField.value ? [...new Set(state.value.lots.flatMap(item => lookupField.value === 'componentNo' ? item.components : [String(item[lookupField.value as Exclude<FilterKey, 'componentNo'>])]))].sort() : [])
const rows = ref<Lot[]>([]); const selected = ref<string[]>([]); const searched = ref(false)
const pending = ref<Lot[]>([]); const mode = ref<'0' | '1'>('0')
const mClassNo = ref(''); const sClassNo = ref(''); const holdDescription = ref(''); const selectedHoldItems = ref<string[]>([])
const collectBy = ref<'lot' | 'component'>('lot'); const area = ref(''); const selectedComponents = ref<string[]>([])
const reasons = ref<Reason[]>([]); const selectedReasons = ref<string[]>([])
const releaseEQP = ref(false); const equipmentState = ref(''); const attachmentNames = ref<string[]>([])
const errorText = ref(''); const modalError = ref(''); const message = ref('請輸入查詢條件，選擇生產批後按 Hold Set。')
const refLot = computed(() => pending.value[0] || null)
const batchComponents = computed(() => [...new Set(pending.value.flatMap(item => item.components))].sort())
const releaseAvailable = computed(() => pending.value.some(item => item.status === 1 && Boolean(item.equipmentNo)))
const mClasses = [...new Set(holdItems.map(item => item.mClassNo))]
const sClasses = computed(() => [...new Set(holdItems.filter(item => item.mClassNo === mClassNo.value).map(item => item.sClassNo))])
const visibleItems = computed(() => holdItems.filter(item => item.mClassNo === mClassNo.value && item.sClassNo === sClassNo.value))

function seed(): MockState { return { lots: fixtures.map(item => ({ ...item, components: [...item.components], erfNos: [...item.erfNos] })), erfs: [], nextSerial: 1 } }
function load(): void {
  if (!dev.value) return
  try {
    const data = JSON.parse(window.localStorage.getItem(storageKey) || 'null') as unknown
    if (data && typeof data === 'object' && 'lots' in data && 'erfs' in data && 'nextSerial' in data && Array.isArray(data.lots) && Array.isArray(data.erfs)) { state.value = data as MockState; return }
  } catch { /* use fixtures */ }
  state.value = seed()
}
function norm(value: string): string { return value.trim().toUpperCase() }
function chooseLookup(value: string): void { if (lookupField.value) filters[lookupField.value] = value; lookupField.value = null }
function query(): void {
  errorText.value = ''; lookupField.value = null
  if (!dev.value) { errorText.value = unavailableMessage; return }
  for (const field of fields) filters[field.key] = norm(filters[field.key])
  if (!Object.values(filters).some(Boolean) && !timeFrom.value && !timeTo.value) { errorText.value = '請至少輸入一個查詢條件。'; return }
  if (timeFrom.value && timeTo.value && timeFrom.value >= timeTo.value) { errorText.value = 'CheckInTime 起點必須早於終點。'; return }
  rows.value = state.value.lots.filter(item => (multipleHoldEnabled ? [0, 1, 3, 4] : [0, 1, 4]).includes(item.status) && item.lotState === 1 && item.curQty > 0 && !(item.partialMode && item.status !== 0) &&
    fields.every(field => !filters[field.key] || (field.key === 'componentNo' ? item.components.includes(filters.componentNo) : item[field.key as Exclude<FilterKey, 'componentNo'>] === filters[field.key])) &&
    (!timeFrom.value || item.checkInTime >= timeFrom.value) && (!timeTo.value || item.checkInTime <= timeTo.value)).sort((a, b) => a.lotNo.localeCompare(b.lotNo))
  selected.value = []; searched.value = true; message.value = `查得 ${rows.value.length} 筆生產批。`
}
function openHold(): void {
  errorText.value = ''
  const chosen = rows.value.filter(item => selected.value.includes(item.lotNo)).sort((a, b) => a.lotNo.localeCompare(b.lotNo))
  if (!dev.value || !chosen.length) { errorText.value = '請選擇至少一筆生產批。'; return }
  if (chosen.some(item => item.partialMode !== chosen[0]!.partialMode)) { errorText.value = '所選生產批的 PartialMode 必須相同。'; return }
  pending.value = chosen.map(item => ({ ...item, components: [...item.components], erfNos: [...item.erfNos] }))
  mClassNo.value = ''; sClassNo.value = ''; holdDescription.value = ''; selectedHoldItems.value = []
  collectBy.value = 'lot'; area.value = ''; selectedComponents.value = []; reasons.value = []; selectedReasons.value = []
  releaseEQP.value = releaseAvailable.value; equipmentState.value = ''; attachmentNames.value = []; modalError.value = ''
}
function changeClass(): void { selectedHoldItems.value = []; reasons.value = []; selectedReasons.value = [] }
function addReasons(): void {
  modalError.value = ''
  if (!refLot.value || !selectedHoldItems.value.length) { modalError.value = '請選擇 HoldItem。'; return }
  if (collectBy.value === 'component' && !selectedComponents.value.length) { modalError.value = '請選擇元件。'; return }
  const components = collectBy.value === 'lot' ? ['N/A'] : selectedComponents.value
  const areaNo = area.value.trim() || 'N/A'
  for (const item of visibleItems.value.filter(value => selectedHoldItems.value.includes(value.holdItemNo))) for (const componentNo of components) {
    const key = `${item.holdItemNo}|${componentNo}|${areaNo}`
    if (!reasons.value.some(reason => reason.key === key)) reasons.value.push({ key, holdItemNo: item.holdItemNo, holdItemName: item.holdItemName, componentNo, areaNo, qty: 1, description: item.description, createERF: item.createERF })
  }
  selectedHoldItems.value = []
}
function removeReasons(): void { reasons.value = reasons.value.filter(item => !selectedReasons.value.includes(item.key)); selectedReasons.value = [] }
function addFiles(event: Event): void { attachmentNames.value = Array.from((event.target as HTMLInputElement).files || []).map(file => file.name) }
function validQty(value: number | ''): boolean { const qty = Number(value); return value !== '' && Number.isFinite(qty) && qty > 0 && Math.abs(qty * 10000 - Math.round(qty * 10000)) < 0.000001 }
function validateLive(snapshot: Lot, current: MockState): string {
  const lot = current.lots.find(item => item.lotNo === snapshot.lotNo && item.opNo === snapshot.opNo)
  if (!lot || lot.lotStamp !== snapshot.lotStamp || lot.status !== snapshot.status || lot.curQty !== snapshot.curQty || lot.lotState !== 1 || lot.curQty <= 0 || (lot.partialMode && lot.status !== 0)) return `${snapshot.lotNo} 狀態或 LotStamp 已變更`
  if (lot.partialMode !== pending.value[0]?.partialMode) return `${snapshot.lotNo} PartialMode 已變更`
  return ''
}
function makeRecord(erfNo: string, lotNos: string[], referenceNo: string): ERF {
  const target = state.value.lots.find(item => item.lotNo === referenceNo)
  const applicableReasons = mode.value === '1' ? reasons.value.filter(item => item.componentNo === 'N/A' || target?.components.includes(item.componentNo)) : reasons.value
  return { erfNo, erfType: mode.value === '0' ? 6 : 2, refLotNo: referenceNo, lotNos, holdDescription: holdDescription.value.trim(), mClassNo: mClassNo.value, reasons: applicableReasons.map(item => ({ ...item, qty: Number(item.qty) })), createERF: reasons.value.some(item => item.createERF), releaseEQP: releaseEQP.value, equipmentState: releaseEQP.value ? equipmentState.value : '', attachmentNames: [...attachmentNames.value], actor: props.currentUserNo || 'DEV', createdAt: new Date().toISOString() }
}
function confirmHold(): void {
  modalError.value = ''
  if (!dev.value || !pending.value.length) return
  if (!mClassNo.value || !holdDescription.value.trim() || !reasons.value.length) { modalError.value = '請填 MClassNo、HoldDescription 並加入 HoldReason。'; return }
  if (reasons.value.some(item => !validQty(item.qty))) { modalError.value = '原因 Qty 必須大於 0 且最多四位小數。'; return }
  if (releaseEQP.value && !equipmentState.value) { modalError.value = 'Release EQP 為 Yes 時請選擇 Equipment State。'; return }
  const total = pending.value.length
  let done = 0; let failed = ''; const created: string[] = []
  const targets = mode.value === '0' ? [pending.value] : pending.value.map(item => [item])
  for (const group of targets) {
    let current: MockState
    try {
      const data = JSON.parse(window.localStorage.getItem(storageKey) || 'null') as unknown
      current = data && typeof data === 'object' && 'lots' in data && 'erfs' in data && 'nextSerial' in data && Array.isArray(data.lots) && Array.isArray(data.erfs) ? data as MockState : seed()
    } catch { failed = '無法讀取 DEV 狀態'; break }
    failed = group.map(item => validateLive(item, current)).find(Boolean) || ''
    if (failed) break
    const erfNo = `DEV-BERF-${String(current.nextSerial).padStart(4, '0')}`
    if (current.erfs.some(item => item.erfNo === erfNo)) { failed = `${erfNo} 已使用`; break }
    const refNo = group[0]!.lotNo
    current.erfs.push(makeRecord(erfNo, group.map(item => item.lotNo), refNo))
    current.nextSerial += 1
    for (const item of group) {
      const live = current.lots.find(lot => lot.lotNo === item.lotNo && lot.opNo === item.opNo)!
      live.status = 3; live.holdTimes += 1; live.lotStamp += 1; live.erfNos.push(erfNo)
    }
    try { window.localStorage.setItem(storageKey, JSON.stringify(current)) }
    catch { failed = '瀏覽器無法儲存 DEV 狀態'; break }
    state.value = current; done += group.length; created.push(erfNo)
  }
  pending.value = []; query()
  if (failed) errorText.value = `已完成 ${done}/${total} 批；${failed}。一單一批模式可能部分完成。`
  else message.value = `DEV 已模擬 ${done} 批、${created.length} 張異常單（${created.join('、')}），未更新 MES。`
}
onMounted(load)
watch(dev, enabled => { if (enabled) load(); else { rows.value = []; selected.value = []; pending.value = []; searched.value = false } })
</script>

<template>
  <section class="wp503-page">
    <div v-if="dev" class="dev-banner">DEV 假資料：RO-501 可查兩批；ERFBatchHoldGenType 在下方彈窗示範 0 一單多批／1 一單一批。</div>
    <h1>批次立即異常設定 <small>IMMEDIATE HOLD SET_BATCH · WP503</small></h1>
    <p v-if="!dev" class="notice" role="status">{{ unavailableMessage }}</p><p v-else-if="errorText" class="notice error" role="alert">{{ errorText }}</p><p v-else class="notice" role="status">{{ message }}</p>
    <fieldset class="query-frame"><legend>Lot Query</legend><div class="query-grid">
      <label v-for="field in fields" :key="field.key">{{ field.label }}<input v-model="filters[field.key]" class="mes-input yellow" :disabled="!dev" @keydown.enter.prevent="query"><button v-if="field.lookup" type="button" class="lookup-button" :disabled="!dev" @click.prevent="lookupField = lookupField === field.key ? null : field.key">...</button></label>
      <label>CheckInTime From<input v-model="timeFrom" type="datetime-local" class="mes-input yellow" :disabled="!dev"></label><label>To<input v-model="timeTo" type="datetime-local" class="mes-input yellow" :disabled="!dev"></label><button type="button" class="mes-button" :disabled="!dev" @click="query">Query</button>
    </div><div v-if="dev && lookupField" class="lookup-options"><strong>{{ lookupField }} 查找</strong><button v-for="option in lookupOptions" :key="option" type="button" @click="chooseLookup(option)">{{ option }}</button></div></fieldset>
    <fieldset class="lot-frame"><legend>Lot</legend><div class="toolbar"><button type="button" class="mes-button" :disabled="!dev || !rows.length" @click="selected = rows.map(item => item.lotNo)">Select All</button><button type="button" class="mes-button" :disabled="!dev || !rows.length" @click="selected = []">UnSelect All</button></div>
      <div class="grid-wrap"><table><thead><tr><th>Select</th><th>LotNo</th><th>HotLot</th><th>OPNo</th><th>Status</th><th>CurQty</th><th>CurUnitNo</th><th>SysQty</th><th>SysUnitNo</th><th>HoldTimes</th><th>MONo</th><th>ProductNo</th><th>ProductVersion</th><th>ModuleNo</th><th>ModuleVersion</th><th>ModuleStageNo</th></tr></thead><tbody>
        <tr v-for="item in rows" :key="item.lotNo"><td><input v-model="selected" type="checkbox" :value="item.lotNo" :disabled="!dev"></td><td>{{ item.lotNo }}</td><td>{{ item.hotLot ? 'Yes' : 'No' }}</td><td>{{ item.opNo }}</td><td>{{ statuses[item.status] }}</td><td class="num">{{ item.curQty }}</td><td>{{ item.curUnitNo }}</td><td class="num">{{ item.sysQty }}</td><td>{{ item.sysUnitNo }}</td><td><button type="button" class="grid-link" :disabled="!item.holdTimes" @click="message = item.erfNos.join('、') || '來源異常單在 DEV 假資料中不可查。'">{{ item.holdTimes }}</button></td><td>{{ item.moNo }}</td><td>{{ item.productNo }}</td><td>{{ item.productVersion }}</td><td>—</td><td>—</td><td>—</td></tr>
        <tr v-if="!rows.length"><td colspan="16" class="empty">{{ searched ? '沒有符合條件的生產批。' : '請先查詢。' }}</td></tr>
      </tbody></table></div><div class="footer"><button type="button" class="mes-button primary" :disabled="!dev || !selected.length" @click="openHold">Hold Set</button><button type="button" class="mes-button" @click="emit('close')">Close</button></div>
    </fieldset>
    <div v-if="dev && refLot" class="modal-backdrop"><div class="erf-dialog" role="dialog" aria-modal="true" aria-label="Batch ERF Create"><h2>Batch ERF Create <small>參考批 {{ refLot.lotNo }} · {{ pending.length }} 批</small></h2><p v-if="modalError" class="notice error" role="alert">{{ modalError }}</p>
      <fieldset><legend>Data of Lot State</legend><div class="modal-fields"><label>Ref LotNo<input :value="refLot.lotNo" readonly class="mes-input"></label><label>OPNo<input :value="refLot.opNo" readonly class="mes-input"></label><label>ProductNo<input :value="refLot.productNo" readonly class="mes-input"></label><label>Lot Detail<input :value="pending.filter(item => item.lotNo !== refLot?.lotNo).map(item => item.lotNo).join(', ')" readonly class="mes-input wide"></label><label>Release EQP<select v-model="releaseEQP" class="mes-input yellow" :disabled="!releaseAvailable"><option :value="false">No</option><option :value="true">Yes</option></select></label><label>Equipment State<select v-model="equipmentState" class="mes-input yellow" :disabled="!releaseEQP"><option value="">請選擇</option><option value="IDLE">IDLE</option><option value="MAINTAIN">MAINTAIN</option></select></label></div></fieldset>
      <fieldset><legend>ERF Data</legend><div class="modal-fields"><label>ERFBatchHoldGenType<select v-model="mode" class="mes-input yellow"><option value="0">0 · 一單多批</option><option value="1">1 · 一單一批</option></select></label><label>ERF No<input :value="`DEV-BERF-${String(state.nextSerial).padStart(4,'0')}`" readonly class="mes-input"></label><label>MClass No<select v-model="mClassNo" class="mes-input yellow" @change="sClassNo = ''; changeClass()"><option value="">選擇主分類</option><option v-for="item in mClasses" :key="item">{{ item }}</option></select></label><label>SClass No<select v-model="sClassNo" class="mes-input yellow" :disabled="!mClassNo" @change="changeClass"><option value="">選擇次分類</option><option v-for="item in sClasses" :key="item">{{ item }}</option></select></label></div><label class="description">Hold Description<textarea v-model="holdDescription" class="yellow"></textarea></label><label>Attach File<input type="file" multiple @change="addFiles"></label><small>DEV 僅記檔名；不處理檔案內容、報表或通知。{{ attachmentNames.join('、') }}</small>
        <div class="component-picker"><label>Collect By<select v-model="collectBy" class="mes-input yellow" :disabled="!batchComponents.length"><option value="lot">Lot</option><option value="component">Component</option></select></label><label>Area<input v-model="area" class="mes-input yellow" placeholder="空白 = N/A"></label><label v-for="item in batchComponents" :key="item"><input v-model="selectedComponents" type="checkbox" :value="item" :disabled="collectBy !== 'component'">{{ item }}</label></div>
        <div class="reason-panels"><div><strong>HoldItem</strong><table><thead><tr><th>Select</th><th>HoldItemNo</th><th>HoldItemName</th></tr></thead><tbody><tr v-for="item in visibleItems" :key="item.holdItemNo"><td><input v-model="selectedHoldItems" type="checkbox" :value="item.holdItemNo"></td><td>{{ item.holdItemNo }}</td><td>{{ item.holdItemName }}</td></tr></tbody></table></div><div class="reason-buttons"><button type="button" class="mes-button" @click="addReasons">Add →</button><button type="button" class="mes-button" :disabled="!selectedReasons.length" @click="removeReasons">← Remove</button></div><div><strong>HoldReason</strong><div class="reason-scroll"><table><thead><tr><th>Select</th><th>HoldItemNo</th><th>Qty</th><th>Description</th><th>ComponentNo</th><th>AreaNo</th></tr></thead><tbody><tr v-for="item in reasons" :key="item.key"><td><input v-model="selectedReasons" type="checkbox" :value="item.key"></td><td>{{ item.holdItemNo }}</td><td><input v-model.number="item.qty" type="number" min="0" step="0.0001" class="grid-input qty"></td><td><input v-model="item.description" class="grid-input"></td><td>{{ item.componentNo }}</td><td>{{ item.areaNo }}</td></tr></tbody></table></div></div></div>
      </fieldset><div class="footer"><button type="button" class="mes-button primary" @click="confirmHold">Confirm</button><button type="button" class="mes-button" @click="pending = []">Close</button></div>
    </div></div>
  </section>
</template>

<style scoped>
.wp503-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:7px;padding:9px;box-sizing:border-box;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif}.dev-banner{padding:6px 9px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}h1{margin:0;font-size:18px}h1 small,h2 small{font-size:12px;font-weight:400;color:#777}.notice{margin:0;min-height:18px}.notice.error{padding:5px 8px;border:1px solid #d6aaa6;background:#fff0ef;color:#99251e}fieldset{min-width:0;margin:0;padding:7px 8px;border:1px solid #bbb;background:#fff}legend{padding:0 5px}.query-frame{position:relative}.query-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:6px}.query-grid label,.modal-fields label{display:flex;align-items:center;justify-content:flex-end;gap:4px;white-space:nowrap}.mes-input{height:26px;min-width:0;padding:3px 5px;box-sizing:border-box;border:1px solid #888;font:inherit}.query-grid .mes-input{width:105px}.yellow{background:#fff0b3!important}.mes-button,.lookup-button{min-height:29px;padding:3px 10px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#f2f2f2);color:#333;font:inherit;cursor:pointer}.lookup-button{min-width:27px;padding:1px}.mes-button:disabled,.lookup-button:disabled{color:#999;cursor:default}.mes-button.primary{background:#f3a62a;border-color:#e99512;color:#fff}.lookup-options{position:absolute;z-index:5;top:100%;left:8px;display:flex;align-items:center;flex-wrap:wrap;gap:6px;padding:7px;border:1px solid #aaa;background:#fffbe9;box-shadow:0 3px 9px #0002}.lookup-options button{padding:3px 7px;border:1px solid #bbb;background:#fff;cursor:pointer}.lot-frame{flex:1;min-height:200px;display:flex;flex-direction:column}.toolbar{display:flex;gap:7px;padding-bottom:6px}.grid-wrap{flex:1;min-height:100px;overflow:auto;border:1px solid #aaa}.grid-wrap table,.reason-panels table{border-collapse:collapse;white-space:nowrap}.grid-wrap table{min-width:1260px}.grid-wrap th,.reason-panels th{position:sticky;top:0;background:#ead591;border:1px solid #d4d4d4;padding:5px 7px;text-align:left;font-weight:500}.grid-wrap td,.reason-panels td{border:1px solid #d4d4d4;padding:4px 6px;background:#ffe89c}.grid-wrap .num{text-align:right}.grid-link{border:0;background:none;color:#705200;text-decoration:underline;cursor:pointer}.grid-link:disabled{color:#777;cursor:default}.empty{text-align:center!important;background:#fff!important;color:#777;height:36px}.footer{display:flex;justify-content:flex-end;gap:8px;padding-top:7px}.modal-backdrop{position:fixed;inset:0;z-index:200;display:flex;align-items:center;justify-content:center;padding:20px;background:#0007}.erf-dialog{display:flex;flex-direction:column;gap:7px;width:min(940px,100%);max-height:calc(100vh - 40px);overflow:auto;padding:12px;border:1px solid #888;background:#f7f5ee;box-shadow:0 8px 28px #0005}.erf-dialog h2{margin:0;font-size:17px}.modal-fields{display:flex;flex-wrap:wrap;gap:7px}.modal-fields .mes-input{width:130px}.modal-fields .wide{width:220px}.description{display:flex;gap:8px;padding:8px 0}.description textarea{flex:1;min-height:75px;padding:5px;border:1px solid #888;font:inherit}.erf-dialog small{color:#777}.component-picker{display:flex;flex-wrap:wrap;gap:10px;padding:7px 0}.reason-panels{display:grid;grid-template-columns:minmax(180px,1fr) 85px minmax(300px,1.3fr);gap:7px}.reason-panels>div:first-child,.reason-scroll{overflow:auto;max-height:220px}.reason-buttons{display:flex;flex-direction:column;justify-content:center;gap:8px}.grid-input{min-width:80px;max-width:180px;padding:3px;border:1px solid #aaa;background:#fff0b3;font:inherit}.grid-input.qty{width:60px;min-width:60px}@media(max-width:900px){.query-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.reason-panels{grid-template-columns:1fr}.reason-buttons{flex-direction:row}}
</style>
