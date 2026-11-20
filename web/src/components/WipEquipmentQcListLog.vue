<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import WipEquipmentOnlineQc from './WipEquipmentOnlineQc.vue'

type QcResult = -1 | 0 | 1
type QcDetail = {
  qcOrder: number
  qcItem: string
  qcType: number
  inputDataCount: number
  inputValue: string
  readings: string[]
  result: '' | 'Y' | 'N'
  qcResult: QcResult
  stdValue: string
  miniValue: string
  maxiValue: string
  inspectionMethod: string
  inspectionStandards: string
}
type InspectionRecord = {
  qcListSerial: string
  equipmentNo: string
  equipmentType: string
  qcListNo: string
  qcListName: string
  qcResult: QcResult
  description: string
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
  taskNo: string
  inspType: number
  shiftNo: string
  lotNo: string
  lotSerial: string
  logGroupSerial: string
  details: QcDetail[]
}
type Snapshot = { version: number; records: InspectionRecord[]; taskStatuses?: Record<string, number> }
type Header = Pick<InspectionRecord,
  'qcListSerial' | 'equipmentNo' | 'qcListNo' | 'lotSerial' | 'logGroupSerial' | 'qcResult' |
  'description' | 'creator' | 'createDate' | 'reviser' | 'reviseDate'> & {
    equipmentType: string
    qcListName: string
    details: QcDetail[]
    stored: boolean
  }
type Equipment = { equipmentNo: string; equipmentType: string; name: string }

const props = defineProps<{ currentUserNo?: string; developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.wp305-equipment-online-qc.v1'
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const equipmentBasis: Equipment[] = [
  { equipmentNo: 'DEV-EQP-ETCH-01', equipmentType: 'DEV-ETCH', name: 'DEV 蝕刻設備 01' },
  { equipmentNo: 'DEV-EQP-ETCH-02', equipmentType: 'DEV-ETCH', name: 'DEV 蝕刻設備 02' },
  { equipmentNo: 'DEV-EQP-CMP-01', equipmentType: 'DEV-CMP', name: 'DEV CMP 設備 01' },
]

function sampleDetail(qcOrder: number, qcItem: string, qcType: number, inputValue: string, qcResult: QcResult, limits: Partial<QcDetail> = {}): QcDetail {
  return {
    qcOrder, qcItem, qcType, inputDataCount: 1, inputValue, readings: [],
    result: qcResult === 0 ? 'Y' : qcResult === 1 ? 'N' : '', qcResult,
    stdValue: '', miniValue: '', maxiValue: '', inspectionMethod: '', inspectionStandards: '', ...limits,
  }
}
const sampleHeaders: Header[] = [
  {
    qcListSerial: 'DEV-QCLOG-20261120-001', equipmentNo: 'DEV-EQP-ETCH-01', equipmentType: 'DEV-ETCH',
    qcListNo: 'DEV-QC-ETCH-DAILY', qcListName: '蝕刻設備每日點檢', lotSerial: '', logGroupSerial: '', qcResult: 0,
    description: 'DEV 範例：設備每日點檢完成', creator: 'DEVUSER', createDate: '2026/11/20 08:12:30', reviser: '', reviseDate: '', stored: false,
    details: [
      sampleDetail(10, '每日確認設備壓力', 0, '12', 0, { stdValue: '12' }),
      sampleDetail(20, '每日量測冷卻水溫', 1, '24', 0, { miniValue: '18', maxiValue: '28' }),
      sampleDetail(30, '確認安全門及連鎖裝置', 2, '', 0),
    ],
  },
  {
    qcListSerial: 'DEV-QCLOG-20261119-002', equipmentNo: 'DEV-EQP-CMP-01', equipmentType: 'DEV-CMP',
    qcListNo: 'DEV-QC-CMP-DAILY', qcListName: 'CMP 設備每日點檢', lotSerial: 'DEV-LOT-SER-26001', logGroupSerial: 'DEV-LOG-26001', qcResult: 1,
    description: 'DEV 範例：冷卻水溫超出上限', creator: 'DEVUSER', createDate: '2026/11/19 09:04:12', reviser: 'DEVUSER', reviseDate: '2026/11/19 09:06:45', stored: false,
    details: [
      sampleDetail(10, '每日確認設備壓力', 0, '12', 0, { stdValue: '12' }),
      sampleDetail(20, '每日量測冷卻水溫', 1, '31', 1, { miniValue: '18', maxiValue: '28' }),
      sampleDetail(30, '確認安全門及連鎖裝置', 2, '', 0),
    ],
  },
]

function readSnapshot(): Snapshot {
  if (!dev.value) return { version: 1, records: [] }
  try {
    const raw = window.localStorage.getItem(storageKey)
    if (raw) {
      const value = JSON.parse(raw) as Snapshot
      if (value.version === 1 && Array.isArray(value.records)) return value
    }
  } catch { /* keep the page readable with DEV sample rows */ }
  return { version: 1, records: [] }
}
const snapshot = ref<Snapshot>(readSnapshot())
const equipmentNo = ref('')
const startEnabled = ref(false)
const endEnabled = ref(false)
const startDate = ref(nowInputValue())
const endDate = ref(nowInputValue())
const pickerOpen = ref(false)
const pickerSearch = ref('')
const editorOpen = ref(false)
const errorText = ref('')
const queried = ref(false)
const headers = ref<Header[]>([])
const selectedSerial = ref('')

function nowInputValue(): string {
  const now = new Date()
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}T${pad(now.getHours())}:${pad(now.getMinutes())}:${pad(now.getSeconds())}`
}
function formatDate(value: string): string { return value || '' }
function parseDate(value: string): number {
  if (!value) return Number.NaN
  const match = value.match(/^(\d{4})[/-](\d{1,2})[/-](\d{1,2})[T ](\d{1,2}):(\d{2})(?::(\d{2}))?$/)
  if (!match) return Number.NaN
  return new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]), Number(match[4]), Number(match[5]), Number(match[6] || 0)).getTime()
}
function statusLabel(value: QcResult): string { return value === -1 ? 'N/A' : value === 0 ? 'OK' : 'NG' }
function typeLabel(value: number): string {
  return value === 0 ? '標準值' : value === 1 ? '範圍' : value === 2 ? '顯示訊息' : value === 3 ? '輸入資料' : String(value)
}
function mapStoredRecord(record: InspectionRecord): Header {
  return {
    qcListSerial: record.qcListSerial, equipmentNo: record.equipmentNo, equipmentType: record.equipmentType,
    qcListNo: record.qcListNo, qcListName: record.qcListName, lotSerial: record.lotSerial,
    logGroupSerial: record.logGroupSerial, qcResult: record.qcResult, description: record.description,
    creator: record.creator, createDate: record.createDate, reviser: record.reviser, reviseDate: record.reviseDate,
    details: record.details || [], stored: true,
  }
}
const selectedHeader = computed(() => headers.value.find(item => item.qcListSerial === selectedSerial.value) || null)
const detailRows = computed(() => selectedHeader.value?.details || [])
const sampleColumns = computed(() => {
  const maxCount = Math.max(0, ...detailRows.value.map(item => item.inputDataCount || 0))
  return maxCount > 1 ? Array.from({ length: Math.min(10, maxCount) }, (_, index) => index) : []
})
const filteredEquipment = computed(() => equipmentBasis.filter(item =>
  `${item.equipmentNo} ${item.equipmentType} ${item.name}`.toLowerCase().includes(pickerSearch.value.trim().toLowerCase())))
const selectedEquipment = computed(() => equipmentBasis.find(item => item.equipmentNo === equipmentNo.value) || null)
const equipmentDescription = computed(() => selectedEquipment.value?.name || '')
const canEdit = computed(() => {
  const current = selectedHeader.value
  return Boolean(current?.stored && current.qcResult === -1)
})

function query(): void {
  errorText.value = ''
  if (!dev.value) { headers.value = []; selectedSerial.value = ''; queried.value = true; return }
  // WinForms funLoadQCListLog guards both StartDate and EndDate with dtpStartDate.Checked.
  // Keep that observed behavior here: an End Date alone is not sent; checking Start Date sends both values.
  const from = startEnabled.value ? parseDate(startDate.value) : Number.NaN
  const to = startEnabled.value ? parseDate(endDate.value) : Number.NaN
  if (startEnabled.value && (!Number.isFinite(from) || !Number.isFinite(to))) {
    errorText.value = '請確認查詢起訖時間。'
    return
  }
  if (startEnabled.value && from > to) {
    errorText.value = 'Start Date 不可晚於 End Date。'
    return
  }
  snapshot.value = readSnapshot()
  const source = snapshot.value.records.length > 0 ? snapshot.value.records.map(mapStoredRecord) : sampleHeaders
  headers.value = source.filter(item => {
    if (equipmentNo.value.trim() && item.equipmentNo.toUpperCase() !== equipmentNo.value.trim().toUpperCase()) return false
    const created = parseDate(item.createDate)
    if (Number.isFinite(from) && (!Number.isFinite(created) || created < from)) return false
    if (Number.isFinite(to) && (!Number.isFinite(created) || created > to)) return false
    return true
  }).sort((left, right) => left.qcListSerial.localeCompare(right.qcListSerial))
  selectedSerial.value = headers.value[0]?.qcListSerial || ''
  queried.value = true
}
function selectHeader(row: Header): void { selectedSerial.value = row.qcListSerial }
function chooseEquipment(item: Equipment): void {
  equipmentNo.value = item.equipmentNo
  pickerOpen.value = false
  pickerSearch.value = ''
}
function refreshAfterEdit(): void {
  editorOpen.value = false
  const current = selectedSerial.value
  query()
  if (headers.value.some(item => item.qcListSerial === current)) selectedSerial.value = current
}
function onStorage(event: StorageEvent): void {
  if (event.key === storageKey) query()
}
onMounted(() => {
  query()
  window.addEventListener('storage', onStorage)
})
onBeforeUnmount(() => window.removeEventListener('storage', onStorage))
</script>

<template>
  <section class="wp306-page" aria-label="設備點檢歷程">
    <div v-if="!dev" class="preview-note">目前為唯讀預覽；切換至 DEV 假資料模式後可檢視範例點檢歷程。</div>
    <div v-if="errorText" class="message error-message" role="alert">{{ errorText }}</div>

    <fieldset class="query-frame">
      <legend>Query</legend>
      <div class="query-grid">
        <label for="wp306-equipment-no">Equipment No</label>
        <div class="lookup-row">
          <input id="wp306-equipment-no" v-model.trim="equipmentNo" class="mes-input" maxlength="30" :disabled="!dev" @input="equipmentNo = equipmentNo.toUpperCase()" @keydown.enter.prevent="query">
          <button type="button" class="lookup-button" :disabled="!dev" aria-label="選擇設備" @click="pickerOpen = true">...</button>
          <span class="equipment-description">{{ equipmentDescription }}</span>
        </div>
        <label class="date-label"><span>Start Date</span><span class="date-control"><input v-model="startEnabled" type="checkbox" :disabled="!dev"><input v-model="startDate" type="datetime-local" step="1" class="date-input" :disabled="!dev || !startEnabled"></span></label>
        <label class="date-label"><span>End Date</span><span class="date-control"><input v-model="endEnabled" type="checkbox" :disabled="!dev"><input v-model="endDate" type="datetime-local" step="1" class="date-input" :disabled="!dev || !endEnabled"></span></label>
        <button type="button" class="mes-button query-button" :disabled="!dev" @click="query">Query</button>
      </div>
    </fieldset>

    <div class="grid-panel header-panel">
      <div class="grid-toolbar"><span>設備點檢歷程</span><button type="button" class="mes-button" :disabled="!canEdit" @click="editorOpen = true">Edit</button></div>
      <div class="grid-scroll header-scroll">
        <table class="history-grid">
          <thead><tr>
            <th>QCListSerial</th><th>EquipmentNo</th><th>QCListNo</th><th>LotSerial</th><th>LogGroupSerial</th><th>QCResult</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th>
          </tr></thead>
          <tbody>
            <tr v-for="row in headers" :key="row.qcListSerial" :class="{ selected: row.qcListSerial === selectedSerial }" tabindex="0" @click="selectHeader(row)" @keydown.enter="selectHeader(row)">
              <td>{{ row.qcListSerial }}</td><td>{{ row.equipmentNo }}</td><td>{{ row.qcListNo }}</td><td>{{ row.lotSerial || '' }}</td><td>{{ row.logGroupSerial || '' }}</td>
              <td :class="row.qcResult === 1 ? 'ng-text' : row.qcResult === 0 ? 'ok-text' : 'pending-text'">{{ statusLabel(row.qcResult) }}</td><td class="description-cell">{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td>
            </tr>
            <tr v-if="headers.length === 0"><td colspan="11" class="empty-grid">{{ queried ? '查無資料' : '按 Query 查詢點檢歷程' }}</td></tr>
          </tbody>
        </table>
      </div>
      <div class="grid-footer">資料總筆數：{{ headers.length }}<span v-if="!headers.some(row => row.stored) && headers.length">DEV 範例資料</span></div>
    </div>

    <div class="grid-panel detail-panel">
      <div class="grid-toolbar"><span>點檢明細　{{ selectedHeader?.qcListSerial || '' }}</span><span v-if="selectedHeader">QCResult：<strong :class="selectedHeader.qcResult === 1 ? 'ng-text' : selectedHeader.qcResult === 0 ? 'ok-text' : 'pending-text'">{{ statusLabel(selectedHeader.qcResult) }}</strong></span></div>
      <div class="grid-scroll detail-scroll">
        <table class="history-grid detail-grid">
          <thead><tr><th>QCItem</th><th>QCOrder</th><th>InputDataCount</th><th>Type</th><th>InputValue</th><th v-for="index in sampleColumns" :key="'d' + index">D{{ String(index + 1).padStart(2, '0') }}</th><th>StdValue</th><th>MiniValue</th><th>MaxiValue</th><th>QCResult</th></tr></thead>
          <tbody>
            <tr v-for="item in detailRows" :key="item.qcOrder + '-' + item.qcItem">
              <td>{{ item.qcItem }}</td><td class="number-cell">{{ item.qcOrder }}</td><td class="number-cell">{{ item.inputDataCount }}</td><td>{{ typeLabel(item.qcType) }}</td><td>{{ item.inputValue }}</td>
              <td v-for="index in sampleColumns" :key="item.qcOrder + '-d' + index">{{ item.readings?.[index] || '' }}</td>
              <td>{{ item.stdValue }}</td><td>{{ item.miniValue }}</td><td>{{ item.maxiValue }}</td><td :class="item.qcResult === 1 ? 'ng-text' : item.qcResult === 0 ? 'ok-text' : 'pending-text'">{{ statusLabel(item.qcResult) }}</td>
            </tr>
            <tr v-if="detailRows.length === 0"><td :colspan="9 + sampleColumns.length" class="empty-grid">選取上方點檢記錄以檢視明細。</td></tr>
          </tbody>
        </table>
      </div>
    </div>

    <div class="page-actions"><span class="dev-caption">{{ dev ? 'DEV 假資料；查詢結果取自 WP305 共用 localStorage' : '唯讀預覽' }}</span><button type="button" class="mes-button close-button" @click="emit('close')">Close</button></div>

    <div v-if="pickerOpen" class="modal-backdrop" role="presentation" @click.self="pickerOpen = false">
      <section class="picker-dialog" role="dialog" aria-modal="true" aria-label="選擇設備">
        <h2>選擇設備</h2><label class="picker-search">搜尋<input v-model="pickerSearch" class="mes-input" autocomplete="off"></label>
        <div class="picker-list"><button v-for="item in filteredEquipment" :key="item.equipmentNo" type="button" @click="chooseEquipment(item)"><strong>{{ item.equipmentNo }}</strong><span>{{ item.equipmentType }}　{{ item.name }}</span></button><p v-if="filteredEquipment.length === 0" class="empty-grid">沒有符合條件的設備</p></div>
        <div class="dialog-actions"><button type="button" class="mes-button" @click="pickerOpen = false">Close</button></div>
      </section>
    </div>

    <div v-if="editorOpen && selectedHeader?.stored" class="editor-backdrop">
      <WipEquipmentOnlineQc class="embedded-editor" :current-user-no="props.currentUserNo" :development-preview="props.developmentPreview" :edit-record-serial="selectedHeader.qcListSerial" @close="refreshAfterEdit" />
    </div>
  </section>
</template>

<style scoped>
.wp306-page{display:flex;flex-direction:column;gap:8px;min-width:0;color:#252525;font-size:13px}.query-frame{margin:0;border:1px solid #b9b9b9;background:#fff;padding:10px;min-width:0}.query-frame legend{padding:0 7px;font-size:14px;font-weight:600;color:#4d4d4d}.query-grid{display:grid;grid-template-columns:100px minmax(250px,1.2fr) 250px 250px 80px;gap:8px;align-items:center}.query-grid>label:first-child{text-align:right}.lookup-row{display:flex;gap:4px;align-items:center;min-width:0}.mes-input,.date-input{height:28px;min-width:0;padding:4px 7px;border:1px solid #888;background:#fff0b3;color:#222;border-radius:2px;font:inherit}.lookup-row .mes-input{width:180px}.lookup-button,.mes-button{height:30px;padding:3px 12px;border:1px solid #b7b7b7;border-radius:3px;background:linear-gradient(#fff,#f1f1f1);color:#333;font:inherit;white-space:nowrap}.lookup-button{width:31px;padding:2px}.mes-button:hover,.lookup-button:hover{background:#e9e9e9}.mes-button:disabled,.lookup-button:disabled{opacity:.55;cursor:not-allowed}.equipment-description{overflow:hidden;text-overflow:ellipsis;white-space:nowrap;color:#666}.date-label{display:flex;align-items:center;gap:8px;white-space:nowrap}.date-label>span:first-child{width:70px;text-align:right}.date-control{display:flex;align-items:center;gap:4px}.date-input{width:178px}.query-button{justify-self:end;min-width:76px}.grid-panel{border:1px solid #bcbcbc;background:#fff;min-width:0;padding:6px}.header-panel{height:260px;display:flex;flex-direction:column}.detail-panel{flex:1;min-height:210px;display:flex;flex-direction:column}.grid-toolbar{display:flex;align-items:center;justify-content:space-between;min-height:31px;padding:0 2px;color:#444;font-weight:600}.grid-toolbar .mes-button{height:27px}.grid-scroll{overflow:auto;border:1px solid #c6c6c6;min-width:0}.header-scroll{flex:1}.detail-scroll{flex:1}.history-grid{border-collapse:collapse;width:100%;min-width:1450px;font-size:12px}.detail-grid{min-width:960px}.history-grid th{position:sticky;top:0;z-index:1;background:#438fca;color:#fff;border:1px solid #dedede;padding:6px 5px;text-align:left;white-space:nowrap;font-weight:600}.history-grid td{height:29px;padding:4px 5px;border:1px solid #d1d1d1;background:#fff1b5;white-space:nowrap}.history-grid tbody tr:nth-child(even) td{background:#ffeaa2}.history-grid tbody tr{cursor:pointer}.history-grid tbody tr:hover td{background:#fff0bf}.history-grid tbody tr.selected td{background:#ffe08a;outline:1px solid #d49a24;outline-offset:-1px}.history-grid .description-cell{max-width:230px;overflow:hidden;text-overflow:ellipsis}.ok-text{color:#23703b;font-weight:700}.ng-text{color:#b12720;font-weight:700}.pending-text{color:#8a6d3b;font-weight:700}.number-cell{text-align:right}.empty-grid{text-align:center!important;color:#777!important;background:#f8f8f8!important;padding:16px!important}.grid-footer{display:flex;justify-content:space-between;color:#777;padding:4px 2px 0;font-size:12px}.page-actions{display:flex;align-items:center;justify-content:flex-end;gap:8px}.dev-caption{margin-right:auto;color:#777;font-size:12px}.close-button{min-width:84px}.preview-note,.message{padding:8px 10px;border:1px solid;border-radius:3px}.preview-note{border-color:#d5c9a7;background:#fff9e8;color:#655321}.error-message{border-color:#d8aaa6;background:#fff0ef;color:#9c241d}.modal-backdrop,.editor-backdrop{position:fixed;inset:0;z-index:1000;background:rgba(0,0,0,.32);display:grid;place-items:center;padding:18px}.picker-dialog{width:min(720px,96vw);max-height:88vh;overflow:auto;background:#fff;border:1px solid #aaa;box-shadow:0 8px 28px rgba(0,0,0,.25);padding:16px}.picker-dialog h2{margin:0 0 14px;font-size:16px;font-weight:600;color:#444}.picker-search{display:flex;align-items:center;gap:10px;margin-bottom:10px}.picker-search .mes-input{max-width:360px}.picker-list{max-height:55vh;overflow:auto;border:1px solid #ccc}.picker-list button{display:flex;width:100%;align-items:center;gap:18px;padding:10px 12px;text-align:left;border:0;border-bottom:1px solid #ddd;background:#fff;color:#222}.picker-list button:hover{background:#fff1b5}.picker-list button strong{min-width:190px}.picker-list button span{color:#666}.dialog-actions{display:flex;justify-content:flex-end;gap:8px;margin-top:14px}.editor-backdrop{z-index:1100;background:rgba(0,0,0,.48);overflow:auto;place-items:center}.embedded-editor{width:min(1280px,98vw);max-height:94vh;overflow:auto;background:#fff;padding:16px;box-shadow:0 8px 30px rgba(0,0,0,.3)}@media(max-width:1100px){.query-grid{grid-template-columns:100px minmax(230px,1fr) 250px 250px}.query-button{grid-column:4;grid-row:2}.grid-panel{min-height:220px}}@media(max-width:760px){.query-grid{grid-template-columns:1fr}.query-grid>label:first-child{text-align:left}.date-label{justify-content:flex-start}.date-label>span:first-child{width:80px;text-align:left}.query-button{grid-column:auto;grid-row:auto;justify-self:stretch}.header-panel{height:300px}.page-actions{flex-wrap:wrap}.dev-caption{flex-basis:100%}}
</style>
