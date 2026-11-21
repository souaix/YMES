<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type SkipStatus = 0 | 1
type SkipEntry = {
  lotNo: string; opNo: string; status: SkipStatus; moNo: string; productNo: string
  productName: string; productVersion: string; reason: string; release: string; creator: string; createDate: string
}
type LotOption = {
  lotNo: string; moNo: string; productNo: string; productName: string; productVersion: string; lotStatus: number
}
type OperationOption = { opNo: string; opName: string; opType: string }
type PickerMode = 'filter-ops' | 'new-lots' | 'new-ops' | null

const props = defineProps<{ developmentPreview?: boolean; initialLotNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)

const operationOptions: OperationOption[] = [
  { opNo: 'DEV-OP-QC-20', opName: 'Final Inspection', opType: 'QC' },
  { opNo: 'DEV-OP-QC-40', opName: 'Appearance Check', opType: 'QC' },
  { opNo: 'DEV-OP-CLEAN-30', opName: 'Cleaning', opType: 'Production' },
  { opNo: 'DEV-OP-PACK-80', opName: 'Packing', opType: 'Production' },
]
const lotOptions: LotOption[] = [
  { lotNo: 'DEV-LOT-SKIP-001', moNo: 'DEV-MO-261121-01', productNo: 'DEV-PROD-GLASS-01', productName: 'DEV Optical Glass', productVersion: 'A', lotStatus: 1 },
  { lotNo: 'DEV-LOT-SKIP-002', moNo: 'DEV-MO-261121-01', productNo: 'DEV-PROD-GLASS-01', productName: 'DEV Optical Glass', productVersion: 'A', lotStatus: 4 },
  { lotNo: 'DEV-LOT-SKIP-003', moNo: 'DEV-MO-261120-02', productNo: 'DEV-PROD-GLASS-02', productName: 'DEV Precision Glass', productVersion: 'B', lotStatus: 1 },
  { lotNo: 'DEV-LOT-SKIP-004', moNo: 'DEV-MO-261120-02', productNo: 'DEV-PROD-GLASS-02', productName: 'DEV Precision Glass', productVersion: 'B', lotStatus: 11 },
]
const initialRows: SkipEntry[] = [
  { lotNo: 'DEV-LOT-SKIP-001', opNo: 'DEV-OP-QC-20', status: 0, moNo: 'DEV-MO-261121-01', productNo: 'DEV-PROD-GLASS-01', productName: 'DEV Optical Glass', productVersion: 'A', reason: 'DEV approved skip inspection sample', release: '', creator: 'DEV-11019', createDate: '2026-11-21T09:20:00' },
  { lotNo: 'DEV-LOT-SKIP-002', opNo: 'DEV-OP-QC-40', status: 1, moNo: 'DEV-MO-261121-01', productNo: 'DEV-PROD-GLASS-01', productName: 'DEV Optical Glass', productVersion: 'A', reason: 'DEV historical skip reason', release: 'DEV inspection resumed after sample review', creator: 'DEV-11018', createDate: '2026-11-20T16:45:00' },
  { lotNo: 'DEV-LOT-SKIP-003', opNo: 'DEV-OP-QC-20', status: 0, moNo: 'DEV-MO-261120-02', productNo: 'DEV-PROD-GLASS-02', productName: 'DEV Precision Glass', productVersion: 'B', reason: 'DEV alternate process inspection waiver', release: '', creator: 'DEV-11019', createDate: '2026-11-20T10:05:00' },
]

const filters = ref({ lotNo: '', moNo: '', opNos: [] as string[], status: 0 as 0 | 1 | 9, useDate: false, dateFrom: '', dateEnd: '' })
const rows = ref<SkipEntry[]>([])
const backingRows = ref<SkipEntry[]>(initialRows.map(row => ({ ...row })))
const selectedKey = ref('')
const queried = ref(false)
const errorText = ref('')
const statusText = ref('Ready')
const addOpen = ref(false)
const addForm = ref({ lotNos: [] as string[], opNos: [] as string[], reason: '' })
const statusTarget = ref<SkipEntry | null>(null)
const statusReason = ref('')
const pickerMode = ref<PickerMode>(null)
const pickerSearch = ref('')
const pickerSelected = ref<string[]>([])

const selectedEntry = computed(() => rows.value.find(row => rowKey(row) === selectedKey.value) || null)
const pickerTitle = computed(() => pickerMode.value === 'filter-ops' ? '選擇查詢作業站' : pickerMode.value === 'new-lots' ? '選擇生產批' : '選擇免檢作業站')
const pickerItems = computed(() => {
  const term = pickerSearch.value.trim().toLowerCase()
  if (pickerMode.value === 'new-lots') {
    return lotOptions.filter(item => item.lotStatus < 11 && `${item.lotNo} ${item.moNo} ${item.productNo} ${item.productName}`.toLowerCase().includes(term))
      .map(item => ({ key: item.lotNo, title: item.lotNo, detail: `${item.moNo} · ${item.productNo} ${item.productVersion} · Lot Status ${item.lotStatus}` }))
  }
  return operationOptions.filter(item => `${item.opNo} ${item.opName} ${item.opType}`.toLowerCase().includes(term))
    .map(item => ({ key: item.opNo, title: item.opNo, detail: `${item.opName} · ${item.opType}` }))
})
const statusTitle = computed(() => statusTarget.value?.status === 0 ? '執行中 → 解除' : '解除 → 執行中')
const statusNext = computed(() => statusTarget.value?.status === 0 ? 1 : 0)

function rowKey(row: Pick<SkipEntry, 'lotNo' | 'opNo'>): string { return `${row.lotNo}::${row.opNo}` }
function statusLabel(status: SkipStatus): string { return status === 0 ? '執行中' : '解除' }
function formatDate(value: string): string { return value ? value.replace('T', ' ').replace(/-/g, '/') : '' }
function nowText(): string { return new Date().toISOString().slice(0, 19) }
function parseDate(value: string): number { return value ? new Date(value).getTime() : Number.NaN }

function query(): void {
  errorText.value = ''
  queried.value = true
  if (!dev.value) {
    rows.value = []
    selectedKey.value = ''
    statusText.value = 'Production skip-inspection data is not connected.'
    return
  }
  const lotTerm = filters.value.lotNo.trim().toLowerCase()
  const moTerm = filters.value.moNo.trim().toLowerCase()
  const from = filters.value.useDate ? parseDate(filters.value.dateFrom) : Number.NaN
  const end = filters.value.useDate ? parseDate(filters.value.dateEnd) : Number.NaN
  if (filters.value.useDate && (!Number.isFinite(from) || !Number.isFinite(end))) {
    errorText.value = '起訖建立日期都要設定。'
    rows.value = []
    return
  }
  rows.value = backingRows.value.filter(row =>
    (!lotTerm || row.lotNo.toLowerCase().includes(lotTerm)) &&
    (!moTerm || row.moNo.toLowerCase().includes(moTerm)) &&
    (filters.value.status === 9 || row.status === filters.value.status) &&
    (filters.value.opNos.length === 0 || filters.value.opNos.includes(row.opNo)) &&
    (!filters.value.useDate || (parseDate(row.createDate) >= from && parseDate(row.createDate) <= end)),
  )
  selectedKey.value = rows.value[0] ? rowKey(rows.value[0]) : ''
  statusText.value = `Rows: ${rows.value.length}`
}

function openPicker(mode: Exclude<PickerMode, null>): void {
  pickerMode.value = mode
  pickerSearch.value = ''
  if (mode === 'filter-ops') pickerSelected.value = [...filters.value.opNos]
  if (mode === 'new-lots') pickerSelected.value = [...addForm.value.lotNos]
  if (mode === 'new-ops') pickerSelected.value = [...addForm.value.opNos]
}
function togglePicker(itemNo: string, checked: boolean): void {
  pickerSelected.value = checked
    ? [...new Set([...pickerSelected.value, itemNo])]
    : pickerSelected.value.filter(value => value !== itemNo)
}
function applyPicker(): void {
  if (pickerMode.value === 'filter-ops') filters.value.opNos = [...pickerSelected.value]
  if (pickerMode.value === 'new-lots') addForm.value.lotNos = [...pickerSelected.value]
  if (pickerMode.value === 'new-ops') addForm.value.opNos = [...pickerSelected.value]
  pickerMode.value = null
}
function selectedText(values: string[]): string { return values.length ? values.join(', ') : '未選擇' }
function openAdd(): void {
  errorText.value = ''
  addForm.value = { lotNos: [], opNos: [], reason: '' }
  addOpen.value = true
}
function saveAdd(): void {
  errorText.value = ''
  if (!addForm.value.lotNos.length || !addForm.value.opNos.length || !addForm.value.reason.trim()) {
    errorText.value = '請選擇 Lot、OP，並輸入免檢原因。'
    return
  }
  const lotByNo = new Map(lotOptions.map(lot => [lot.lotNo, lot]))
  const newRows: SkipEntry[] = []
  for (const lotNo of addForm.value.lotNos) {
    const lot = lotByNo.get(lotNo)
    if (!lot || lot.lotStatus >= 11) { errorText.value = `${lotNo} 已不符合 Lot Status < 11。`; return }
    for (const opNo of addForm.value.opNos) {
      if (backingRows.value.some(row => row.lotNo === lotNo && row.opNo === opNo)) {
        errorText.value = `${lotNo} / ${opNo} 已存在；來源複合主鍵會拒絕整批 INSERT ALL。`
        return
      }
      newRows.push({ lotNo, opNo, status: 0, moNo: lot.moNo, productNo: lot.productNo, productName: lot.productName, productVersion: lot.productVersion, reason: addForm.value.reason.trim(), release: '', creator: 'DEV-11019', createDate: nowText() })
    }
  }
  backingRows.value = [...newRows, ...backingRows.value]
  addOpen.value = false
  query()
  statusText.value = `Added ${newRows.length} DEV skip-inspection row(s).`
}
function openStatusChange(): void {
  if (!selectedEntry.value) return
  errorText.value = ''
  statusReason.value = ''
  statusTarget.value = { ...selectedEntry.value }
}
function saveStatusChange(): void {
  errorText.value = ''
  if (!statusTarget.value || !statusReason.value.trim()) { errorText.value = '請輸入解除原因。'; return }
  const nextStatus = statusNext.value as SkipStatus
  const targetKey = rowKey(statusTarget.value)
  backingRows.value = backingRows.value.map(row => rowKey(row) === targetKey
    ? { ...row, status: nextStatus, release: statusReason.value.trim() }
    : row)
  statusTarget.value = null
  query()
  statusText.value = `Status changed to ${statusLabel(nextStatus)} in DEV.`
}

onMounted(() => {
  filters.value.dateFrom = `${new Date().toISOString().slice(0, 10)}T00:00:00`
  filters.value.dateEnd = `${new Date().toISOString().slice(0, 10)}T23:59:59`
  if (props.initialLotNo?.trim()) {
    filters.value.lotNo = props.initialLotNo.trim().toUpperCase()
    query()
  }
})
</script>

<template>
  <section class="skip-page" aria-label="免檢批設定">
    <header class="page-title">
      <div><h1>免檢批設定</h1><p>Skip Inspection Setting · 依 Lot × OP 設定免檢狀態</p></div>
      <button class="mes-button" @click="emit('close')">Close (&amp;X)</button>
    </header>

    <div class="query-box">
      <h2>Query</h2>
      <div class="query-grid">
        <label>LotNo <input v-model="filters.lotNo" class="text-input" @keyup.enter="query"></label>
        <label>MONo <input v-model="filters.moNo" class="text-input" @keyup.enter="query"></label>
        <div class="lookup-field"><span>OPNo</span><input :value="selectedText(filters.opNos)" readonly><button class="lookup-button" @click="openPicker('filter-ops')">...</button></div>
        <label>Status <select v-model.number="filters.status"><option :value="0">執行中</option><option :value="1">解除</option><option :value="9">全部</option></select></label>
      </div>
      <div class="date-row">
        <label class="check-field"><input v-model="filters.useDate" type="checkbox"> 建立日期</label>
        <label>From <input v-model="filters.dateFrom" type="datetime-local" step="1"></label>
        <label>To <input v-model="filters.dateEnd" type="datetime-local" step="1"></label>
        <button class="mes-button primary" @click="query">Query (Q)</button>
      </div>
    </div>

    <div v-if="errorText" class="message error" role="alert">{{ errorText }}</div>
    <div v-if="queried && !dev" class="message info" role="status">正式免檢資料服務尚未連接；DEV 假資料只在開發預覽提供。</div>

    <div class="toolbar"><span>已選取：{{ selectedEntry ? `${selectedEntry.lotNo} / ${selectedEntry.opNo}` : '未選取' }}</span><div><button class="mes-button" :disabled="!dev" @click="openAdd">Add</button><button class="mes-button primary" :disabled="!dev || !selectedEntry" @click="openStatusChange">Change Status</button></div></div>

    <div class="grid-wrap">
      <table class="result-grid">
        <thead><tr><th>LotNo</th><th>OPNo</th><th>Status</th><th>MONo</th><th>Product</th><th>ProductName</th><th>Version</th><th>Creator</th><th>CreateDate</th><th>Skip Reason</th><th>Release Reason</th></tr></thead>
        <tbody>
          <tr v-for="row in rows" :key="rowKey(row)" :class="{ selected: selectedKey === rowKey(row) }" @click="selectedKey = rowKey(row)">
            <td>{{ row.lotNo }}</td><td>{{ row.opNo }}</td><td><span class="status" :class="row.status === 0 ? 'running' : 'released'">{{ statusLabel(row.status) }}</span></td><td>{{ row.moNo }}</td><td>{{ row.productNo }}</td><td>{{ row.productName }}</td><td>{{ row.productVersion }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reason }}</td><td>{{ row.release }}</td>
          </tr>
          <tr v-if="rows.length === 0"><td colspan="11" class="empty">{{ queried ? 'No matching skip-inspection rows.' : '按 Query 載入免檢設定。' }}</td></tr>
        </tbody>
      </table>
    </div>
    <footer class="statusbar"><span>{{ statusText }}</span><span>DEV Mock · 0=執行中 / 1=解除 / 9=全部</span></footer>

    <div v-if="addOpen" class="overlay" role="dialog" aria-modal="true" aria-label="新增免檢設定">
      <section class="dialog">
        <header><strong>新增免檢設定</strong><button class="mes-button" @click="addOpen = false">Close</button></header>
        <label class="dialog-field">LotNo <div class="lookup-field"><input :value="selectedText(addForm.lotNos)" readonly><button class="lookup-button" @click="openPicker('new-lots')">...</button></div><small>批次查找只列 Lot Status &lt; 11。</small></label>
        <label class="dialog-field">OPNo <div class="lookup-field"><input :value="selectedText(addForm.opNos)" readonly><button class="lookup-button" @click="openPicker('new-ops')">...</button></div></label>
        <label class="dialog-field">免檢原因 <textarea v-model="addForm.reason" rows="5" maxlength="100"></textarea><small>{{ addForm.reason.length }} / 100</small></label>
        <div v-if="errorText" class="message error" role="alert">{{ errorText }}</div>
        <footer><span>新增會對每個 Lot × OP 組合建立 Status=0；撞到既有主鍵時整個批次失敗。</span><div><button class="mes-button" @click="addOpen = false">Cancel</button><button class="mes-button primary" @click="saveAdd">Complete</button></div></footer>
      </section>
    </div>

    <div v-if="statusTarget" class="overlay" role="dialog" aria-modal="true" aria-label="變更免檢狀態">
      <section class="dialog status-dialog">
        <header><strong>{{ statusTitle }}</strong><button class="mes-button" @click="statusTarget = null">Close</button></header>
        <p>{{ statusTarget.lotNo }} / {{ statusTarget.opNo }}：{{ statusLabel(statusTarget.status) }} → {{ statusLabel(statusNext as SkipStatus) }}</p>
        <label class="dialog-field">解除原因 <textarea v-model="statusReason" rows="4" maxlength="100"></textarea></label>
        <div v-if="errorText" class="message error" role="alert">{{ errorText }}</div>
        <footer><span>來源要求輸入原因；同一筆記錄保留最近一次解除原因。</span><div><button class="mes-button" @click="statusTarget = null">Cancel</button><button class="mes-button primary" @click="saveStatusChange">Confirm</button></div></footer>
      </section>
    </div>

    <div v-if="pickerMode" class="overlay picker-overlay" role="dialog" aria-modal="true" :aria-label="pickerTitle">
      <section class="dialog picker-dialog">
        <header><strong>{{ pickerTitle }}</strong><button class="mes-button" @click="pickerMode = null">Close</button></header>
        <input v-model="pickerSearch" class="text-input" placeholder="搜尋編號或名稱">
        <div class="picker-list"><label v-for="item in pickerItems" :key="item.key" class="picker-row"><input type="checkbox" :checked="pickerSelected.includes(item.key)" @change="togglePicker(item.key, ($event.target as HTMLInputElement).checked)"><span><b>{{ item.title }}</b><small>{{ item.detail }}</small></span></label><div v-if="pickerItems.length === 0" class="empty">沒有符合的候選資料。</div></div>
        <footer><span>已選 {{ pickerSelected.length }} 筆</span><div><button class="mes-button" @click="pickerMode = null">Cancel</button><button class="mes-button primary" @click="applyPicker">Apply</button></div></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.skip-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:10px;padding:12px;box-sizing:border-box;background:#f3f5f6;color:#30363b;font:13px Arial,"Microsoft JhengHei",sans-serif}.page-title{display:flex;align-items:center;justify-content:space-between;gap:12px}.page-title h1{margin:0 0 4px;font-size:20px;font-weight:600}.page-title p{margin:0;color:#69747c;font-size:12px}.mes-button,.lookup-button{height:30px;padding:4px 12px;border:1px solid #9ca6ac;border-radius:2px;background:#f7f8f9;color:#30363b;cursor:pointer;white-space:nowrap}.mes-button:hover:not(:disabled),.lookup-button:hover{background:#e7edf1}.mes-button:disabled{opacity:.48;cursor:not-allowed}.mes-button.primary{border-color:#35789c;background:#397fa2;color:#fff}.query-box{padding:10px;border:1px solid #aeb8bd;background:#fff}.query-box h2{margin:0 0 9px;font-size:14px;font-weight:600}.query-grid{display:grid;grid-template-columns:minmax(150px,1fr) minmax(150px,1fr) minmax(260px,1.6fr) minmax(135px,.8fr);align-items:end;gap:9px}.query-grid>label,.lookup-field{display:flex;align-items:center;gap:7px}.query-grid>label{justify-content:space-between;white-space:nowrap}.text-input,.query-grid select,.date-row input[type=datetime-local],.lookup-field input{height:30px;padding:4px 7px;border:1px solid #b9c2c7;background:#fff;box-sizing:border-box;color:#30363b;min-width:0}.query-grid>label .text-input{width:calc(100% - 52px)}.query-grid select{width:100%}.lookup-field{min-width:0}.lookup-field span{white-space:nowrap}.lookup-field input{flex:1;width:100%;overflow:hidden;text-overflow:ellipsis}.lookup-button{width:31px;padding:3px;flex:0 0 31px}.date-row{display:grid;grid-template-columns:auto minmax(190px,1fr) minmax(190px,1fr) auto;align-items:center;gap:9px;margin-top:10px}.date-row>label:not(.check-field){display:flex;align-items:center;gap:8px;white-space:nowrap}.date-row input[type=datetime-local]{width:100%}.check-field{display:flex;align-items:center;gap:6px;white-space:nowrap}.check-field input,.picker-row input{accent-color:#367d9e}.message{padding:7px 10px;border:1px solid}.message.error{border-color:#d38c8c;background:#fff0f0;color:#a33333}.message.info{border-color:#9bbac7;background:#edf7fb;color:#315d6d}.toolbar{display:flex;align-items:center;justify-content:space-between;gap:10px;color:#65737a}.toolbar>div{display:flex;gap:7px}.grid-wrap{flex:1;min-height:150px;overflow:auto;border:1px solid #aeb8bd;background:#fff}.result-grid{width:100%;border-collapse:collapse;white-space:nowrap}.result-grid th{position:sticky;top:0;z-index:1;padding:6px 8px;border:1px solid #b8c2c7;background:#dfeaf0;text-align:left;font-weight:600}.result-grid td{height:30px;padding:3px 8px;border:1px solid #cbd2d6;background:#fff}.result-grid tbody tr{cursor:pointer}.result-grid tbody tr.selected td{background:#fff0c5}.status{display:inline-block;min-width:52px;padding:3px 7px;border-radius:10px;text-align:center;font-size:11px}.status.running{background:#e2f3e8;color:#2c6f43}.status.released{background:#edf0f2;color:#5f6b72}.empty{text-align:center!important;color:#778188;background:#fff!important;height:46px!important}.statusbar{height:24px;flex:0 0 24px;display:flex;align-items:center;justify-content:space-between;padding:0 5px;border-top:1px solid #c7cdd1;color:#68747b}.overlay{position:fixed;inset:0;z-index:1600;display:grid;place-items:center;padding:18px;background:#0006}.dialog{width:min(650px,94vw);max-height:min(760px,92vh);display:flex;flex-direction:column;gap:10px;padding:13px;background:#f7f8f9;border:1px solid #89949a;box-shadow:0 8px 26px #0004;box-sizing:border-box}.dialog>header,.dialog>footer{display:flex;align-items:center;justify-content:space-between;gap:10px}.dialog>footer{padding-top:7px;border-top:1px solid #d1d7da}.dialog>footer>span{flex:1;color:#65737a;font-size:11px}.dialog>footer>div{display:flex;gap:7px}.dialog-field{display:flex;flex-direction:column;gap:5px}.dialog-field small{color:#69747c;font-size:11px}.dialog-field textarea{width:100%;padding:7px;border:1px solid #b9c2c7;box-sizing:border-box;resize:vertical;font:13px Arial,"Microsoft JhengHei",sans-serif}.dialog-field .lookup-field{width:100%}.status-dialog{width:min(530px,94vw)}.status-dialog p{margin:1px 0;color:#4e5a60}.picker-overlay{z-index:1700}.picker-dialog{height:min(620px,88vh)}.picker-list{flex:1;overflow:auto;border:1px solid #c0c8cc;background:#fff}.picker-row{display:flex;align-items:flex-start;gap:9px;padding:9px;border-bottom:1px solid #e0e4e6;cursor:pointer}.picker-row:hover{background:#f2f8fb}.picker-row input{margin-top:2px}.picker-row span{display:flex;flex-direction:column;gap:3px}.picker-row small{color:#67737a}@media(max-width:900px){.query-grid{grid-template-columns:1fr 1fr}.date-row{grid-template-columns:auto 1fr}.date-row .mes-button{grid-column:2;justify-self:end}}@media(max-width:620px){.query-grid,.date-row{grid-template-columns:1fr}.query-grid>label{justify-content:space-between}.date-row .mes-button{grid-column:1}.toolbar{align-items:flex-start;flex-direction:column}.dialog>footer{align-items:flex-start;flex-direction:column}}
</style>
