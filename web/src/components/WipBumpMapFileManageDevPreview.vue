<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type FileRow = {
  id: string
  waferLot: string
  waferId: number
  bumpCode: string
  orgFileName: string
  fileName: string
  fileLength: number
  filePath: string
  uploadUserNo: string
  uploadDate: string
  selected: boolean
}
type MockFile = {
  id: string
  fileName: string
  waferLot: string
  waferId: number
  bumpCode: string
  length: number
  created: string
  valid: boolean
  validationMessage: string
}
type UploadRow = MockFile & { result: 'READY' | 'ERROR' | 'DUPLICATE' }
type MapTemplate = {
  templateNo: string
  description: string
  mapType: number
  encoding: string
  headerSplitSymbol: string
  lotNoColName: string
  waferIdColName: string
  notchColName: string
  waferXColName: string
  waferYColName: string
  mapPrefixText: string
  dieTextLength: number
  dummyDieSymbol: string
  markingDieSymbol: string
}
type SearchScenario = 'success' | 'state-error' | 'history-error'
type UploadScenario = 'success' | 'first-error' | 'second-error'
type ScrapScenario = 'success' | 'file-missing' | 'state-error'
type DownloadKind = 'state' | 'history'

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)

const templates: MapTemplate[] = [
  { templateNo: 'DEV-MAP-CP-001', description: 'DEV CP Bump MAP text v1', mapType: 1, encoding: 'UTF-8', headerSplitSymbol: ':', lotNoColName: 'LOT', waferIdColName: 'WAFER', notchColName: 'NOTCH', waferXColName: 'COLS', waferYColName: 'ROWS', mapPrefixText: 'MAP:', dieTextLength: 1, dummyDieSymbol: '.', markingDieSymbol: 'M' },
  { templateNo: 'DEV-MAP-CP-002', description: 'DEV CP Bump MAP text v2', mapType: 1, encoding: 'ASCII', headerSplitSymbol: '=', lotNoColName: 'LOTNO', waferIdColName: 'WAFERID', notchColName: 'FLAT', waferXColName: 'XCOUNT', waferYColName: 'YCOUNT', mapPrefixText: 'DATA:', dieTextLength: 1, dummyDieSymbol: '-', markingDieSymbol: 'X' },
]
const mockFiles: MockFile[] = [
  { id: 'F001', fileName: 'DEV-WAFERLOT-001.B1-01.txt', waferLot: 'DEV-WAFERLOT-001', waferId: 1, bumpCode: '.B1', length: 1842, created: '2026-11-22 09:14:00', valid: true, validationMessage: 'MAP header 與 X/Y row size 符合 DEV template。' },
  { id: 'F002', fileName: 'DEV-WAFERLOT-001.B1-02.txt', waferLot: 'DEV-WAFERLOT-001', waferId: 2, bumpCode: '.B1', length: 1828, created: '2026-11-22 09:16:00', valid: true, validationMessage: 'MAP header 與 X/Y row size 符合 DEV template。' },
  { id: 'F003', fileName: 'DEV-WAFERLOT-002-03.TXT', waferLot: 'DEV-WAFERLOT-002', waferId: 3, bumpCode: '', length: 2120, created: '2026-11-22 09:20:00', valid: true, validationMessage: '大寫副檔名保留；MAP header 與 row size 符合 DEV template。' },
  { id: 'F004', fileName: 'DEV-BAD-WAFERMAP.txt', waferLot: '', waferId: 0, bumpCode: '', length: 644, created: '2026-11-22 09:24:00', valid: false, validationMessage: '缺少 WaferLot 或 WaferID header，無法形成可用的 MAP row。' },
]
const initialState: FileRow[] = [
  { id: 'S001', waferLot: 'DEV-WAFERLOT-001', waferId: 1, bumpCode: '.B1', orgFileName: 'DEV-WAFERLOT-001.B1-01.txt', fileName: 'DEV-WAFERLOT-001.B1-01.txt', fileLength: 1760, filePath: 'DEV://bump/state/DEV-WAFERLOT-001/', uploadUserNo: 'DEVUSER01', uploadDate: '2026-11-21 10:15:00', selected: false },
  { id: 'S002', waferLot: 'DEV-WAFERLOT-001', waferId: 2, bumpCode: '.B1', orgFileName: 'DEV-WAFERLOT-001.B1-02.txt', fileName: 'DEV-WAFERLOT-001.B1-02.txt', fileLength: 1784, filePath: 'DEV://bump/state/DEV-WAFERLOT-001/', uploadUserNo: 'DEVUSER02', uploadDate: '2026-11-21 10:40:00', selected: false },
  { id: 'S003', waferLot: 'DEV-WAFERLOT-002', waferId: 3, bumpCode: '', orgFileName: 'DEV-WAFERLOT-002-03.TXT', fileName: 'DEV-WAFERLOT-002-03.TXT', fileLength: 1980, filePath: 'DEV://bump/state/DEV-WAFERLOT-002/', uploadUserNo: 'DEVUSER01', uploadDate: '2026-11-22 08:05:00', selected: false },
]
const initialHistory: FileRow[] = [
  { id: 'H001', waferLot: 'DEV-WAFERLOT-001', waferId: 1, bumpCode: '.B1', orgFileName: 'DEV-WAFERLOT-001.B1-01.txt', fileName: 'DEV-WAFERLOT-001.B1-01.txt', fileLength: 1660, filePath: 'DEV://bump/state/DEV-WAFERLOT-001/', uploadUserNo: 'DEVUSER02', uploadDate: '2026-11-20 13:30:00', selected: false },
  { id: 'H002', waferLot: 'DEV-WAFERLOT-001', waferId: 1, bumpCode: '.B1', orgFileName: 'DEV-WAFERLOT-001.B1-01.txt', fileName: 'DEV-WAFERLOT-001.B1-01_0001.txt', fileLength: 1660, filePath: 'DEV://bump/backup/DEV-WAFERLOT-001/', uploadUserNo: 'DEVUSER02', uploadDate: '2026-11-20 13:30:00', selected: false },
  { id: 'H003', waferLot: 'DEV-WAFERLOT-002', waferId: 3, bumpCode: '', orgFileName: 'DEV-WAFERLOT-002-03.TXT', fileName: 'DEV-WAFERLOT-002-03.TXT', fileLength: 1904, filePath: 'DEV://bump/state/DEV-WAFERLOT-002/', uploadUserNo: 'DEVUSER01', uploadDate: '2026-11-21 15:06:00', selected: false },
]

const stateStore = ref(initialState.map(row => ({ ...row })))
const historyStore = ref(initialHistory.map(row => ({ ...row })))
const stateRows = ref<FileRow[]>([])
const historyRows = ref<FileRow[]>([])
const lotFilterEnabled = ref(false)
const lotFilter = ref('')
const timeFilterEnabled = ref(false)
const fromDate = ref('2026-11-01T00:00:00')
const toDate = ref('2026-11-22T23:59:59')
const searchScenario = ref<SearchScenario>('success')
const uploadScenario = ref<UploadScenario>('success')
const scrapScenario = ref<ScrapScenario>('success')
const downloadScenario = ref<'success' | 'remote-missing' | 'local-conflict'>('success')
const loading = ref(false)
const errorText = ref('')
const statusText = ref('請勾選 Lot 或 UploadTime 條件後按 Query。')
const lastQueried = ref('')
const actionLog = ref<{ id: number; time: string; action: string; file: string; result: string; detail: string }[]>([])
const nextLogId = ref(1)
const uploadOpen = ref(false)
const templatePickerOpen = ref(false)
const filePickerOpen = ref(false)
const selectedTemplateNo = ref('')
const selectedMockFileIds = ref<string[]>([])
const stagedFiles = ref<UploadRow[]>([])
const parserErrors = ref<{ fileName: string; message: string }[]>([])
const uploadHasRun = ref(false)
const uploadProgress = ref(0)
const uploadSummary = ref('')
const downloadOpen = ref(false)
const downloadKind = ref<DownloadKind>('state')
const downloadFolder = ref('DEV-local-folder')
const overwriteConfirmed = ref(false)
const scrapBusy = ref(false)

const selectedTemplate = computed(() => templates.find(row => row.templateNo === selectedTemplateNo.value) ?? null)
const selectedUploadCount = computed(() => stagedFiles.value.filter(row => row.result === 'READY').length)
const uploadProgressMax = computed(() => Math.max(1, stagedFiles.value.filter(row => row.result === 'READY' || row.result === 'ERROR').length))
const selectedForDownload = computed(() => downloadKind.value === 'state' ? stateRows.value.filter(row => row.selected) : historyRows.value.filter(row => row.selected))
const currentRowsSelected = computed(() => stateRows.value.filter(row => row.selected).length)
const historyRowsSelected = computed(() => historyRows.value.filter(row => row.selected).length)

function nowText(): string { return new Date().toLocaleString('zh-TW', { hour12: false }) }
function logAction(action: string, file: string, result: string, detail: string): void {
  actionLog.value.unshift({ id: nextLogId.value++, time: nowText(), action, file, result, detail })
}
function resetNotice(): void { errorText.value = '' }

function filterByQuery(rows: FileRow[]): FileRow[] {
  const lot = lotFilter.value.trim().toUpperCase()
  const from = timeFilterEnabled.value ? new Date(fromDate.value).getTime() : Number.NEGATIVE_INFINITY
  const to = timeFilterEnabled.value ? new Date(toDate.value).getTime() : Number.POSITIVE_INFINITY
  return rows.filter(row => {
    if (lotFilterEnabled.value && lot && !row.waferLot.toUpperCase().startsWith(lot)) return false
    if (timeFilterEnabled.value) {
      const uploaded = new Date(row.uploadDate.replace(' ', 'T')).getTime()
      if (uploaded < from || uploaded > to) return false
    }
    return true
  }).map(row => ({ ...row, selected: false }))
}

function searchData(): void {
  resetNotice()
  if (!lotFilterEnabled.value && !timeFilterEnabled.value) {
    errorText.value = '請至少勾選 WaferLot 或 UploadTime 查詢條件。'
    return
  }
  loading.value = true
  if (searchScenario.value === 'state-error') {
    errorText.value = '模擬 LoadBumpMAPFileState 失敗；保留目前 state 與 history Grid。'
    loading.value = false
    return
  }
  stateRows.value = filterByQuery(stateStore.value)
  if (searchScenario.value === 'history-error') {
    errorText.value = 'state 已更新；模擬 LoadBumpMAPFileLog 失敗，history 保留上一份查詢結果。'
    loading.value = false
    lastQueried.value = nowText()
    return
  }
  historyRows.value = filterByQuery(historyStore.value)
  lastQueried.value = nowText()
  loading.value = false
  const broadLot = lotFilterEnabled.value && !lotFilter.value.trim() && !timeFilterEnabled.value
  statusText.value = broadLot
    ? `DEV 模擬查詢 ${stateRows.value.length} 筆 state／${historyRows.value.length} 筆 history；來源勾選空 Lot 欄位時會省略 Lot 參數。`
    : `已載入 ${stateRows.value.length} 筆目前檔案及 ${historyRows.value.length} 筆歷程（DEV）。`
}

function setSelection(kind: DownloadKind, rowId: string, checked: boolean): void {
  const target = kind === 'state' ? stateRows : historyRows
  target.value = target.value.map(row => row.id === rowId ? { ...row, selected: checked } : row)
}

function selectAll(kind: DownloadKind, checked: boolean): void {
  const target = kind === 'state' ? stateRows : historyRows
  target.value = target.value.map(row => ({ ...row, selected: checked }))
}

function openUpload(): void {
  resetNotice()
  selectedTemplateNo.value = ''
  stagedFiles.value = []
  parserErrors.value = []
  selectedMockFileIds.value = []
  uploadHasRun.value = false
  uploadProgress.value = 0
  uploadSummary.value = ''
  uploadOpen.value = true
}

function openFilePicker(): void {
  if (!selectedTemplate.value) {
    errorText.value = '請先使用 Get 選取 MAPTemplateNo，並確認有 template detail。'
    return
  }
  selectedMockFileIds.value = []
  filePickerOpen.value = true
}

function toggleMockFile(fileId: string): void {
  selectedMockFileIds.value = selectedMockFileIds.value.includes(fileId)
    ? selectedMockFileIds.value.filter(id => id !== fileId)
    : [...selectedMockFileIds.value, fileId]
}

function addMockFiles(): void {
  parserErrors.value = []
  const selectedFiles = mockFiles.filter(file => selectedMockFileIds.value.includes(file.id))
  if (!selectedFiles.length) {
    errorText.value = '請在 DEV file chooser 選取至少一個 .txt fixture。'
    return
  }
  for (const file of selectedFiles) {
    if (!file.valid) {
      parserErrors.value.push({ fileName: file.fileName, message: file.validationMessage })
      continue
    }
    const duplicate = stagedFiles.value.some(row => row.waferLot === file.waferLot && row.waferId === file.waferId && row.fileName.toUpperCase() === file.fileName.toUpperCase())
    if (duplicate) {
      parserErrors.value.push({ fileName: file.fileName, message: '此 WaferLot／WaferID／FileName 已在本次待上傳清單，來源會略過重複列。' })
      continue
    }
    stagedFiles.value.push({ ...file, result: 'READY' })
  }
  filePickerOpen.value = false
  if (selectedFiles.length && !parserErrors.value.length) errorText.value = ''
}

function removeUploadFile(fileId: string): void {
  stagedFiles.value = stagedFiles.value.filter(file => file.id !== fileId)
}

function archivedName(fileName: string, rows: FileRow[]): string {
  const stem = fileName.replace(/\.txt$/i, '')
  const versions = rows
    .filter(row => row.fileName.toUpperCase().startsWith(`${stem.toUpperCase()}_`))
    .map(row => Number(row.fileName.match(/_(\d{4})\.txt$/i)?.[1] ?? 0))
  return `${stem}_${String(Math.max(0, ...versions) + 1).padStart(4, '0')}.txt`
}

function moveCurrentToHistory(row: FileRow): void {
  const archiveFileName = archivedName(row.fileName, historyStore.value)
  const existingHistory = historyStore.value.find(item => item.waferLot === row.waferLot && item.waferId === row.waferId && item.fileName === row.fileName && item.filePath === row.filePath)
  if (existingHistory) {
    existingHistory.fileName = archiveFileName
    existingHistory.filePath = `DEV://bump/backup/${row.waferLot}/`
  } else {
    historyStore.value.unshift({ ...row, id: `H${nextLogId.value++}`, fileName: archiveFileName, filePath: `DEV://bump/backup/${row.waferLot}/`, selected: false })
  }
}

function commitUpload(file: UploadRow): void {
  const existingIndex = stateStore.value.findIndex(row => row.waferLot === file.waferLot && row.waferId === file.waferId)
  if (existingIndex >= 0) moveCurrentToHistory(stateStore.value[existingIndex])
  const row: FileRow = {
    id: `S${nextLogId.value++}`, waferLot: file.waferLot, waferId: file.waferId, bumpCode: file.bumpCode,
    orgFileName: file.fileName, fileName: file.fileName, fileLength: file.length,
    filePath: `DEV://bump/state/${file.waferLot}/`, uploadUserNo: 'DEVUSER01', uploadDate: nowText(), selected: false,
  }
  if (existingIndex >= 0) stateStore.value.splice(existingIndex, 1, row)
  else stateStore.value.unshift(row)
  historyStore.value.unshift({ ...row, id: `H${nextLogId.value++}` })
}

function submitUpload(): void {
  resetNotice()
  if (!selectedTemplate.value) {
    errorText.value = '請先選擇 template。'
    return
  }
  if (!stagedFiles.value.some(row => row.result === 'READY')) {
    errorText.value = '沒有可上傳的有效 fixture。'
    return
  }
  uploadHasRun.value = true
  uploadProgress.value = 0
  let successes = 0
  let failures = 0
  const allReady = stagedFiles.value.filter(row => row.result === 'READY')
  allReady.forEach((file, index) => {
    const failIndex = uploadScenario.value === 'first-error' ? 0 : uploadScenario.value === 'second-error' ? 1 : -1
    if (index === failIndex) {
      failures += 1
      file.result = 'ERROR'
      logAction('Upload', file.fileName, '失敗', '模擬 FTP upload error；檔案與資料列可能需由服務端重新對帳。')
    } else {
      commitUpload(file)
      successes += 1
      file.result = 'READY'
      logAction('Upload', file.fileName, '完成', `已模擬 state upsert 和歷程新增；WaferLot=${file.waferLot}、WaferID=${file.waferId}。`)
    }
    uploadProgress.value += 1
  })
  uploadSummary.value = `處理 ${allReady.length} 筆：成功 ${successes}、失敗 ${failures}。DEV 未傳送檔案或資料。`
}

function closeUpload(): void {
  uploadOpen.value = false
  filePickerOpen.value = false
  templatePickerOpen.value = false
  if (uploadHasRun.value && lotFilterEnabled.value && lotFilter.value.trim()) searchData()
}

function runScrap(): void {
  resetNotice()
  const selectedRows = stateRows.value.filter(row => row.selected)
  if (!selectedRows.length) {
    errorText.value = '請先勾選要 Scrap 的目前檔案。'
    return
  }
  scrapBusy.value = true
  selectedRows.forEach((row, index) => {
    const failure = scrapScenario.value === 'file-missing' ? 'FTP 找不到 state path 檔案；來源不會刪除 state row。' : scrapScenario.value === 'state-error' ? '檔案已模擬移至 backup，但 ScrapBumpMAPFile 刪除 state 失敗，需 reconciliation。' : ''
    if (failure && (scrapScenario.value === 'file-missing' || index === 0)) {
      if (scrapScenario.value === 'state-error') moveCurrentToHistory(row)
      logAction('Scrap', row.fileName, '失敗', failure)
      return
    }
    moveCurrentToHistory(row)
    stateStore.value = stateStore.value.filter(item => item.id !== row.id)
    logAction('Scrap', row.fileName, '完成', `已模擬搬入 backup 並刪除 state；WaferLot=${row.waferLot}、WaferID=${row.waferId}。`)
  })
  scrapBusy.value = false
  if (lotFilterEnabled.value && lotFilter.value.trim()) searchData()
  else errorText.value = 'Scrap 操作完成；查詢條件未限定 Lot，Grid 仍保留原查詢快照，請按 Query 對帳。'
}

function openDownload(kind: DownloadKind): void {
  resetNotice()
  downloadKind.value = kind
  if (!selectedForDownload.value.length) {
    errorText.value = kind === 'state' ? '請先勾選目前檔案再下載。' : '請先勾選 history 檔案再下載。'
    return
  }
  downloadFolder.value = 'DEV-local-folder'
  overwriteConfirmed.value = false
  downloadOpen.value = true
}

function confirmDownload(): void {
  resetNotice()
  const selectedRows = selectedForDownload.value
  if (!selectedRows.length) return
  if (downloadScenario.value === 'local-conflict' && !overwriteConfirmed.value) {
    errorText.value = '模擬目的地已有同名檔；請勾選覆寫確認後再繼續。'
    return
  }
  const action = downloadKind.value === 'state' ? 'Download state' : 'Download history'
  selectedRows.forEach(row => {
    if (downloadScenario.value === 'remote-missing') {
      logAction(action, row.fileName, '失敗', '模擬 FTP 檔案不存在；未寫入本機檔案。')
    } else {
      logAction(action, row.fileName, 'DEV 模擬完成', `目的地 ${downloadFolder.value}；本機檔案未建立或覆寫。`)
    }
  })
  statusText.value = `已模擬 ${selectedRows.length} 筆下載；未連線 file service、未建立檔案。`
  downloadOpen.value = false
}

function closeDownload(): void { downloadOpen.value = false }

onMounted(() => {
  if (!dev.value) statusText.value = '正式 file service 尚未接入。'
})
</script>

<template>
  <section class="bump-file-page">
    <header class="page-header">
      <div><p class="eyebrow">WIP · CP BUMP MAP FILES · DEV</p><h1>Bump MAP 檔案管理</h1><p class="subtitle">查詢目前檔案／歷程，上傳、下載或 Scrap Bump MAP。</p></div>
      <div class="head-actions"><span class="dev-pill">DEV 假資料</span><button type="button" class="quiet" @click="emit('close')">關閉</button></div>
    </header>
    <div v-if="!dev" class="unavailable-note" role="status">正式 MAP template 與 file service 尚未接入；此頁不會讀寫檔案或資料庫。</div>
    <template v-else>
      <div class="source-banner"><span><b>服務</b> `wsWIP`／`clsLot` · `wsSYS`／`clsSYSEng`</span><span><b>FTP</b> disabled</span><span><b>資料表</b> fixture only</span><span><b>最後 Query</b> {{ lastQueried || '—' }}</span></div>
      <section class="panel query-panel">
        <div class="panel-title"><div><h2>Query Condition</h2><p>至少選 Lot 或 UploadTime；兩項均選取時套用交集。</p></div><span class="count">{{ stateRows.length }} current · {{ historyRows.length }} history</span></div>
        <div class="query-grid">
          <label class="check-line"><input v-model="lotFilterEnabled" type="checkbox"> WaferLot</label>
          <input v-model="lotFilter" type="text" placeholder="DEV-WAFERLOT" :disabled="!lotFilterEnabled" @keyup.enter="searchData">
          <label class="check-line"><input v-model="timeFilterEnabled" type="checkbox"> UploadTime</label>
          <label class="date-field">From <input v-model="fromDate" type="datetime-local" step="1" :disabled="!timeFilterEnabled"></label>
          <label class="date-field">To <input v-model="toDate" type="datetime-local" step="1" :disabled="!timeFilterEnabled"></label>
          <button type="button" class="primary" :disabled="loading" @click="searchData">{{ loading ? 'Query 中…' : 'Query' }}</button>
        </div>
        <div class="query-controls"><label>DEV query outcome<select v-model="searchScenario"><option value="success">State + History 成功</option><option value="state-error">State query error</option><option value="history-error">History query error</option></select></label><span>{{ statusText }}</span></div>
        <p v-if="errorText" class="notice error" role="status">{{ errorText }}<button type="button" aria-label="清除訊息" @click="resetNotice">×</button></p>
      </section>

      <section class="panel data-panel">
        <div class="panel-title"><div><h2>BumpMAP File Data</h2><p>目前檔案；選取列可下載或 Scrap。WaferLot／WaferIDCP 對應 Core state key。</p></div><div class="panel-actions"><span class="count">已選 {{ currentRowsSelected }}</span><label>DEV Scrap scenario<select v-model="scrapScenario"><option value="success">正常</option><option value="file-missing">state file missing</option><option value="state-error">state update error</option></select></label><button type="button" class="quiet" @click="openDownload('state')">Download File</button><button type="button" class="warning" :disabled="scrapBusy" @click="runScrap">Scrap File</button><button type="button" class="primary" @click="openUpload">Update File</button></div></div>
        <div class="table-scroll"><table><thead><tr><th><input type="checkbox" aria-label="全選目前檔案" :checked="stateRows.length > 0 && currentRowsSelected === stateRows.length" @change="selectAll('state', ($event.target as HTMLInputElement).checked)"></th><th>WaferLot</th><th>WaferIDCP</th><th>FileName</th><th>UploadUserNo</th><th>UploadDate</th></tr></thead><tbody><tr v-for="row in stateRows" :key="row.id" :class="{ selected: row.selected }" @click="setSelection('state', row.id, !row.selected)"><td @click.stop><input type="checkbox" :checked="row.selected" :aria-label="`選取 ${row.fileName}`" @change="setSelection('state', row.id, ($event.target as HTMLInputElement).checked)"></td><td>{{ row.waferLot }}</td><td class="numeric">{{ row.waferId }}</td><td class="mono">{{ row.fileName }}</td><td>{{ row.uploadUserNo }}</td><td>{{ row.uploadDate }}</td></tr><tr v-if="!stateRows.length"><td colspan="6" class="empty">尚未 Query 或沒有符合的目前檔案</td></tr></tbody></table></div>
      </section>

      <section class="panel data-panel history-panel">
        <div class="panel-title"><div><h2>BumpMAP File History Data</h2><p>保存目前版本與已歸檔版本；欄位依目前來源 Grid 顯示。</p></div><div class="panel-actions"><span class="count">已選 {{ historyRowsSelected }}</span><button type="button" class="quiet" @click="openDownload('history')">Download History</button></div></div>
        <div class="table-scroll"><table><thead><tr><th><input type="checkbox" aria-label="全選歷程檔案" :checked="historyRows.length > 0 && historyRowsSelected === historyRows.length" @change="selectAll('history', ($event.target as HTMLInputElement).checked)"></th><th>WaferLot</th><th>WaferIDCP</th><th>FileName</th><th>UploadUserNo</th><th>UploadDate</th></tr></thead><tbody><tr v-for="row in historyRows" :key="row.id" :class="{ selected: row.selected }" @click="setSelection('history', row.id, !row.selected)"><td @click.stop><input type="checkbox" :checked="row.selected" :aria-label="`選取 ${row.fileName}`" @change="setSelection('history', row.id, ($event.target as HTMLInputElement).checked)"></td><td>{{ row.waferLot }}</td><td class="numeric">{{ row.waferId }}</td><td class="mono">{{ row.fileName }}</td><td>{{ row.uploadUserNo }}</td><td>{{ row.uploadDate }}</td></tr><tr v-if="!historyRows.length"><td colspan="6" class="empty">尚未 Query 或沒有符合的歷程檔案</td></tr></tbody></table></div>
      </section>

      <section class="panel log-panel"><div class="panel-title"><div><h2>DEV 操作紀錄</h2><p>只保留此頁本地狀態。</p></div><button type="button" class="quiet" @click="actionLog = []">清除</button></div><div class="table-scroll"><table><thead><tr><th>時間</th><th>操作</th><th>FileName</th><th>結果</th><th>說明</th></tr></thead><tbody><tr v-for="entry in actionLog" :key="entry.id"><td>{{ entry.time }}</td><td>{{ entry.action }}</td><td class="mono">{{ entry.file }}</td><td><span class="result-pill" :class="{ failed: entry.result === '失敗' }">{{ entry.result }}</span></td><td>{{ entry.detail }}</td></tr><tr v-if="!actionLog.length"><td colspan="5" class="empty">尚無操作紀錄</td></tr></tbody></table></div></section>

      <footer>DEV 只用本地 fixtures；不連 FTP／SFTP、Oracle、MES Core、正式 API 或 PostgreSQL，也不讀取或建立任何本機檔案。</footer>
    </template>

    <section v-if="uploadOpen" class="backdrop" role="presentation"><div class="dialog upload-dialog" role="dialog" aria-modal="true" aria-label="Update Bump MAP files">
      <header class="dialog-head"><div><p class="eyebrow">DEV FILE UPDATE</p><h2>Update Bump MAP files</h2><p>來源在本機選多個 .txt、依 MAP template 解析 header，再逐檔上傳。</p></div><button type="button" class="icon-close" aria-label="關閉上傳視窗" @click="closeUpload">×</button></header>
      <div class="template-bar"><div><b>MAPTemplateNo</b><span>{{ selectedTemplate?.templateNo || '未選擇' }}</span></div><button type="button" class="quiet" @click="templatePickerOpen = true">Get</button></div>
      <div v-if="selectedTemplate" class="template-details"><span>{{ selectedTemplate.description }}</span><span>Encoding {{ selectedTemplate.encoding }}</span><span>Header {{ selectedTemplate.headerSplitSymbol }}</span><span>MAP prefix {{ selectedTemplate.mapPrefixText }}</span><span>Die len {{ selectedTemplate.dieTextLength }}</span></div>
      <div class="dialog-actions"><button type="button" class="quiet" :disabled="!selectedTemplate" @click="openFilePicker">選擇 DEV .txt 檔案</button><label>DEV upload scenario<select v-model="uploadScenario" :disabled="uploadHasRun"><option value="success">逐檔成功</option><option value="first-error">第 1 筆 FTP error</option><option value="second-error">第 2 筆 FTP error</option></select></label></div>
      <div v-if="parserErrors.length" class="parser-errors"><strong>略過／解析訊息</strong><p v-for="entry in parserErrors" :key="entry.fileName">{{ entry.fileName }}：{{ entry.message }}</p></div>
      <div class="table-scroll staged-table"><table><thead><tr><th>WaferLot</th><th>WaferIDCP</th><th>FileName</th><th>FileDate</th><th>Parse</th><th></th></tr></thead><tbody><tr v-for="row in stagedFiles" :key="row.id"><td>{{ row.waferLot }}</td><td>{{ row.waferId || '—' }}</td><td class="mono">{{ row.fileName }}</td><td>{{ row.created }}</td><td><span class="result-pill" :class="{ failed: row.result === 'ERROR' }">{{ row.result }}</span></td><td><button type="button" class="text-button" :disabled="uploadHasRun" @click="removeUploadFile(row.id)">移除</button></td></tr><tr v-if="!stagedFiles.length"><td colspan="6" class="empty">尚未選取 DEV file fixtures</td></tr></tbody></table></div>
      <div v-if="uploadHasRun" class="upload-progress"><span>Progress</span><progress :value="uploadProgress" :max="uploadProgressMax"/><strong>{{ uploadSummary }}</strong></div>
      <div class="dialog-foot"><button type="button" class="quiet" @click="closeUpload">{{ uploadHasRun ? '完成／關閉' : 'Cancel' }}</button><button type="button" class="primary" :disabled="uploadHasRun || !selectedUploadCount" @click="submitUpload">Confirm upload</button></div>
      <div v-if="templatePickerOpen" class="nested-backdrop"><div class="dialog picker-dialog" role="dialog" aria-modal="true" aria-label="選擇 MAP Template"><header class="dialog-head"><div><p class="eyebrow">MAP TYPE 1</p><h2>MAP Template</h2></div><button type="button" class="icon-close" @click="templatePickerOpen = false">×</button></header><div class="template-list"><button v-for="template in templates" :key="template.templateNo" type="button" class="template-option" @click="selectedTemplateNo = template.templateNo; templatePickerOpen = false; errorText = ''"><b>{{ template.templateNo }}</b><span>{{ template.description }}</span><small>{{ template.encoding }} · {{ template.mapPrefixText }} · {{ template.dieTextLength }} chars/die</small></button></div><p class="picker-note">以上均為 DEV fixtures；未呼叫 `wsSYS.LoadMAPTemplate`／`LoadMAPTemplateDetail`。</p></div></div>
      <div v-if="filePickerOpen" class="nested-backdrop"><div class="dialog picker-dialog file-picker" role="dialog" aria-modal="true" aria-label="選取 DEV MAP file"><header class="dialog-head"><div><p class="eyebrow">LOCAL FILE CHOOSER MOCK</p><h2>選擇 .txt 檔案</h2><p>Source 支援多選並依 template encoding 解析。</p></div><button type="button" class="icon-close" @click="filePickerOpen = false">×</button></header><div class="mock-file-list"><label v-for="file in mockFiles" :key="file.id" class="mock-file"><input type="checkbox" :checked="selectedMockFileIds.includes(file.id)" @change="toggleMockFile(file.id)"><span><b class="mono">{{ file.fileName }}</b><small>{{ file.length.toLocaleString('zh-TW') }} bytes · {{ file.created }} · {{ file.valid ? 'DEV MAP valid' : 'DEV parse error' }}</small></span></label></div><div class="dialog-foot"><button type="button" class="quiet" @click="filePickerOpen = false">Cancel</button><button type="button" class="primary" @click="addMockFiles">加入選取檔案</button></div></div></div>
    </div></section>

    <section v-if="downloadOpen" class="backdrop" role="presentation"><div class="dialog small-dialog" role="dialog" aria-modal="true" aria-label="DEV download destination"><header class="dialog-head"><div><p class="eyebrow">DEV DOWNLOAD</p><h2>{{ downloadKind === 'state' ? 'Download File' : 'Download History' }}</h2><p>已選 {{ selectedForDownload.length }} 筆；source 會先選本機目的資料夾。</p></div><button type="button" class="icon-close" @click="closeDownload">×</button></header><label class="destination-field">DEV destination<select v-model="downloadFolder"><option>DEV-local-folder</option><option>DEV-desktop-folder</option><option>DEV-temp-folder</option></select></label><label>DEV result<select v-model="downloadScenario"><option value="success">download success</option><option value="remote-missing">remote file missing</option><option value="local-conflict">同名檔存在</option></select></label><label v-if="downloadScenario === 'local-conflict'" class="check-line"><input v-model="overwriteConfirmed" type="checkbox"> 模擬確認覆寫本機同名檔</label><div class="dialog-foot"><button type="button" class="quiet" @click="closeDownload">Cancel</button><button type="button" class="primary" @click="confirmDownload">Start download</button></div></div></section>
  </section>
</template>

<style scoped>
.bump-file-page{--ink:#1d2d3c;--muted:#718496;--line:#dce4eb;height:100%;min-height:0;overflow:auto;padding:18px 22px 26px;box-sizing:border-box;background:#f6f8fa;color:var(--ink);font:12px/1.45 "Segoe UI","Microsoft JhengHei",sans-serif}.page-header{display:flex;justify-content:space-between;align-items:center;gap:14px;margin-bottom:12px}.eyebrow{margin:0;color:#71879a;font-size:9px;font-weight:800;letter-spacing:.12em}.page-header h1{margin:2px 0 0;font-size:21px}.subtitle{margin:0;color:var(--muted);font-size:10px}.head-actions,.panel-actions,.dialog-actions{display:flex;align-items:center;gap:8px}.dev-pill,.count,.path-pill,.result-pill{display:inline-flex;padding:3px 8px;border-radius:999px;background:#e6f3ec;color:#28744e;font-size:9px;font-weight:750}.quiet,.primary,.warning{min-height:29px;padding:0 10px;border:1px solid #cbd6df;border-radius:4px;background:#fff;color:#38526a;font:inherit;cursor:pointer}.primary{border-color:#255579;background:#255579;color:#fff;font-weight:700}.warning{border-color:#e5c9a7;background:#fff7eb;color:#8b5525}.quiet:disabled,.primary:disabled,.warning:disabled{cursor:not-allowed;opacity:.48}.source-banner{display:flex;gap:15px;flex-wrap:wrap;padding:7px 10px;margin-bottom:9px;border:1px solid #dbe4ec;border-radius:5px;background:#edf3f8;color:#647b8e;font-size:9px}.source-banner b{margin-right:4px;color:#38546e}.panel{margin-bottom:9px;border:1px solid var(--line);border-radius:6px;background:#fff;box-shadow:0 2px 5px #142b3a08;overflow:hidden}.panel-title{display:flex;align-items:center;justify-content:space-between;gap:12px;padding:9px 11px}.panel-title h2{margin:0;font-size:12px}.panel-title p{margin:2px 0 0;color:var(--muted);font-size:9px}.query-grid{display:grid;grid-template-columns:100px minmax(150px,1fr) 100px minmax(150px,1fr) minmax(150px,1fr) auto;gap:8px;align-items:center;padding:0 11px 9px}.query-grid input[type=text],.query-grid input[type=datetime-local],select{height:28px;min-width:0;padding:0 7px;border:1px solid #cad6df;border-radius:4px;background:#fff;color:#3e566b;font:inherit}.query-grid input:disabled{background:#f1f3f5;color:#98a5af}.check-line{display:flex;align-items:center;gap:6px;color:#536c81;font-size:10px}.query-controls{display:flex;align-items:center;gap:12px;padding:8px 11px;border-top:1px solid #edf1f4;color:#718393;font-size:9px}.query-controls label,.panel-actions label,.dialog-actions label,.small-dialog>label:not(.check-line),.destination-field{display:flex;align-items:center;gap:6px;color:#718393;font-size:9px}.query-controls select,.panel-actions select,.dialog-actions select,.small-dialog select{height:26px;padding:0 6px;border:1px solid #ccd8e1;border-radius:4px;background:#fff;color:#3c556b;font:inherit}.query-controls>span{flex:1}.notice{display:flex;justify-content:space-between;gap:10px;padding:8px 10px;margin:0 11px 9px;border:1px solid #e7c2bd;border-radius:4px;background:#fff3f1;color:#963e37;font-size:10px}.notice button{border:0;background:transparent;color:inherit;font-size:15px;cursor:pointer}.panel-actions{flex-wrap:wrap}.panel-actions select{max-width:132px}.table-scroll{overflow:auto;border-top:1px solid #e7edf2}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:9px}th{position:sticky;top:0;background:#f1f5f8;color:#657b8d;font-size:8px;text-align:left}th,td{padding:6px 8px;border-bottom:1px solid #edf1f4}tbody tr{cursor:pointer}tbody tr.selected{background:#eaf4fb}.mono{font-family:Consolas,"Cascadia Mono",monospace;color:#526d84}.numeric{text-align:right;font-variant-numeric:tabular-nums}.empty{text-align:center;color:#8694a1;padding:14px}.path-pill{background:#f1f4f6;color:#61798b;border-radius:4px}.result-pill{border-radius:4px;background:#e8f2eb;color:#377451}.result-pill.failed{background:#fff0ed;color:#a24b41}.log-panel .table-scroll{max-height:155px}.upload-dialog{width:min(1000px,96vw);max-height:90vh;overflow:auto;padding:14px}.dialog{border:1px solid #d4dee6;border-radius:7px;background:#fff;box-shadow:0 18px 55px #10233540}.backdrop{position:fixed;inset:0;z-index:50;display:grid;place-items:center;padding:14px;background:#132636a6}.dialog-head{display:flex;align-items:flex-start;justify-content:space-between;gap:12px;padding-bottom:10px}.dialog-head h2{margin:2px 0;font-size:16px}.dialog-head p:not(.eyebrow){margin:0;color:var(--muted);font-size:9px}.icon-close{width:28px;height:28px;border:0;border-radius:4px;background:#f0f3f5;color:#536b7f;font-size:18px;cursor:pointer}.template-bar{display:flex;justify-content:space-between;align-items:center;gap:12px;padding:8px 10px;border:1px solid #dce5ec;border-radius:4px;background:#f5f8fa}.template-bar div{display:flex;gap:12px;color:#516b80;font-size:10px}.template-bar span{font-family:Consolas,monospace}.template-details{display:flex;gap:12px;flex-wrap:wrap;padding:7px 2px;color:#778a9a;font-size:9px}.dialog-actions{justify-content:space-between;margin:8px 0}.staged-table{max-height:235px;border:1px solid #e7edf2;border-radius:4px}.parser-errors{padding:7px 9px;border:1px solid #ecd4a8;border-radius:4px;background:#fff9ec;color:#805f29;font-size:9px}.parser-errors p{margin:4px 0 0}.upload-progress{display:flex;align-items:center;gap:10px;padding:8px 2px;color:#687f92;font-size:9px}.upload-progress progress{width:130px;height:10px}.upload-progress strong{font-weight:600}.dialog-foot{display:flex;justify-content:flex-end;gap:8px;padding-top:10px}.nested-backdrop{position:fixed;inset:0;z-index:52;display:grid;place-items:center;padding:14px;background:#14273770}.picker-dialog{width:min(590px,95vw);max-height:80vh;overflow:auto;padding:14px}.template-list,.mock-file-list{display:grid;gap:6px}.template-option{display:grid;gap:4px;width:100%;padding:9px 10px;border:1px solid #dce4ec;border-radius:5px;background:#fff;text-align:left;color:#476178;font:inherit;cursor:pointer}.template-option:hover,.template-option:focus-visible{border-color:#7da7c6;background:#f3f8fb}.template-option small,.mock-file small{color:#8393a0;font-size:9px}.picker-note{margin:9px 0 0;color:#8493a0;font-size:9px}.mock-file{display:flex;align-items:center;gap:9px;padding:8px 9px;border:1px solid #e1e7ec;border-radius:4px;background:#fbfcfd}.mock-file span{display:grid;gap:2px}.text-button{border:0;background:transparent;color:#617d94;font:inherit;cursor:pointer}.small-dialog{width:min(480px,95vw);padding:15px}.small-dialog>label{margin:9px 0}.small-dialog select{flex:1}.destination-field{margin-top:3px}@media(max-width:900px){.bump-file-page{padding:12px}.query-grid{grid-template-columns:100px minmax(130px,1fr) 100px minmax(130px,1fr);}.panel-title{align-items:flex-start;flex-direction:column}.panel-actions{width:100%}.query-controls{align-items:flex-start;flex-direction:column}.query-controls>span{flex:auto}}@media(max-width:580px){.page-header{align-items:flex-start}.query-grid{grid-template-columns:100px 1fr}.query-grid .primary{grid-column:1/-1}.panel-actions{gap:5px}.dialog-actions{align-items:flex-start;flex-direction:column}.template-bar div{flex-direction:column;gap:3px}}
</style>
