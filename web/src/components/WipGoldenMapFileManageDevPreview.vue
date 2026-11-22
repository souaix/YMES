<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

type GoldenMapRow = {
  id: string
  deviceNo: string
  productNo: string
  productVersion: string
  orgFileName: string
  fileName: string
  fileLength: number
  filePath: string
  uploadUserNo: string
  uploadDate: string
  selected: boolean
}
type LocalFile = {
  id: string
  fileName: string
  fileLength: number
  createdAt: string
  valid: boolean
}
type UploadRow = LocalFile & {
  productNo: string
  productVersion: string
  targetName: string
  result: 'READY' | 'ERROR' | 'DONE'
  message: string
}

const props = withDefaults(defineProps<{ currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '' })
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const userNo = computed(() => props.currentUserNo || 'DEVUSER01')
const storageKey = 'ymes.dev.wpp05-golden-map-file-manage.v1'

const initialState: GoldenMapRow[] = [
  { id: 'GS-001', deviceNo: 'DEV-CP-5100', productNo: 'DEV-MAP-A12', productVersion: 'B2', orgFileName: 'DEV-MAP-A12-B2.dat', fileName: 'DEV-MAP-A12-B2.dat', fileLength: 8640, filePath: 'DEV://golden/state/', uploadUserNo: 'DEVUSER01', uploadDate: '2026-11-22 08:35:00', selected: false },
  { id: 'GS-002', deviceNo: 'DEV-CP-5200', productNo: 'DEV-MAP-B07', productVersion: 'A1', orgFileName: 'DEV-MAP-B07-A1.dat', fileName: 'DEV-MAP-B07-A1.dat', fileLength: 12288, filePath: 'DEV://golden/state/', uploadUserNo: 'DEVUSER02', uploadDate: '2026-11-21 15:18:00', selected: false },
  { id: 'GS-003', deviceNo: 'DEV-CP-5100', productNo: 'DEV-MAP-A12', productVersion: 'B3', orgFileName: 'DEV-MAP-A12-B3.dat', fileName: 'DEV-MAP-A12-B3.dat', fileLength: 9216, filePath: 'DEV://golden/missing/', uploadUserNo: 'DEVUSER02', uploadDate: '2026-11-20 11:02:00', selected: false },
]
const initialHistory: GoldenMapRow[] = [
  { id: 'GH-001', deviceNo: 'DEV-CP-5100', productNo: 'DEV-MAP-A12', productVersion: 'B2', orgFileName: 'DEV-MAP-A12-B2.dat', fileName: 'DEV-MAP-A12-B2_0001.dat', fileLength: 8192, filePath: 'DEV://golden/backup/', uploadUserNo: 'DEVUSER03', uploadDate: '2026-11-19 10:14:00', selected: false },
  { id: 'GH-002', deviceNo: 'DEV-CP-5300', productNo: 'DEV-MAP-C03', productVersion: 'C1', orgFileName: 'DEV-MAP-C03-C1.dat', fileName: 'DEV-MAP-C03-C1.dat', fileLength: 7680, filePath: 'DEV://golden/backup/', uploadUserNo: 'DEVUSER01', uploadDate: '2026-11-18 16:42:00', selected: false },
  { id: 'GH-003', deviceNo: 'DEV-CP-5100', productNo: 'DEV-MAP-A12', productVersion: 'B2', orgFileName: 'DEV-MAP-A12-B2.dat', fileName: 'DEV-MAP-A12-B2.dat', fileLength: 8640, filePath: 'DEV://golden/state/', uploadUserNo: 'DEVUSER01', uploadDate: '2026-11-22 08:35:00', selected: false },
  { id: 'GH-004', deviceNo: 'DEV-CP-5100', productNo: 'DEV-MAP-A12', productVersion: 'B3', orgFileName: 'DEV-MAP-A12-B3.dat', fileName: 'DEV-MAP-A12-B3.dat', fileLength: 9216, filePath: 'DEV://golden/missing/', uploadUserNo: 'DEVUSER02', uploadDate: '2026-11-20 11:02:00', selected: false },
  { id: 'GH-005', deviceNo: 'DEV-CP-5200', productNo: 'DEV-MAP-B07', productVersion: 'A1', orgFileName: 'DEV-MAP-B07-A1.dat', fileName: 'DEV-MAP-B07-A1.dat', fileLength: 12288, filePath: 'DEV://golden/state/', uploadUserNo: 'DEVUSER02', uploadDate: '2026-11-21 15:18:00', selected: false },
]
const fileFixtures: LocalFile[] = [
  { id: 'F-01', fileName: 'DEV-MAP-A12-B2.dat', fileLength: 8704, createdAt: '2026-11-22 09:14:00', valid: true },
  { id: 'F-02', fileName: 'DEV-MAP-B07-A2.dat', fileLength: 12800, createdAt: '2026-11-22 09:20:00', valid: true },
  { id: 'F-03', fileName: 'DEV_MAP_NO_VERSION.dat', fileLength: 6144, createdAt: '2026-11-22 09:25:00', valid: false },
  { id: 'F-04', fileName: 'DEV-CUSTOM-MAP.bin', fileLength: 4096, createdAt: '2026-11-22 09:30:00', valid: true },
]
const products = [
  { productNo: 'DEV-MAP-A12', productVersion: 'B2', deviceNo: 'DEV-CP-5100' },
  { productNo: 'DEV-MAP-A12', productVersion: 'B3', deviceNo: 'DEV-CP-5100' },
  { productNo: 'DEV-MAP-B07', productVersion: 'A1', deviceNo: 'DEV-CP-5200' },
  { productNo: 'DEV-MAP-C03', productVersion: 'C1', deviceNo: 'DEV-CP-5300' },
]

const stateStore = ref<GoldenMapRow[]>(initialState.map(row => ({ ...row })))
const historyStore = ref<GoldenMapRow[]>(initialHistory.map(row => ({ ...row })))
const stateRows = ref<GoldenMapRow[]>([])
const historyRows = ref<GoldenMapRow[]>([])
const deviceEnabled = ref(true)
const deviceNo = ref('DEV-CP-5100')
const productEnabled = ref(false)
const productNo = ref('')
const dateEnabled = ref(true)
const fromDate = ref('2026-11-01T00:00')
const toDate = ref('2026-11-22T23:59')
const lastQuery = ref('')
const notice = ref('請設定查詢條件後按 Query；裝置、產品與上傳日期可併用。')
const error = ref('')
const updateOpen = ref(false)
const updateMode = ref<'original' | 'assign'>('original')
const assignProduct = ref<{ productNo: string; productVersion: string; deviceNo: string } | null>(null)
const productPickerOpen = ref(false)
const filePickerOpen = ref(false)
const allFiles = ref(false)
const autoRename = ref(false)
const selectedFixtureIds = ref<string[]>([])
const uploadRows = ref<UploadRow[]>([])
const uploadProgress = ref(0)
const uploadSummary = ref('')
const actionLog = ref<{ id: number; time: string; action: string; file: string; result: string; detail: string }[]>([])
const nextId = ref(1)
const downloadOpen = ref(false)
const downloadKind = ref<'state' | 'history'>('state')
const downloadTarget = ref('DEV-local-folder')
const overwriteExisting = ref(false)

const filteredFixtures = computed(() => allFiles.value ? fileFixtures : fileFixtures.filter(file => /\.dat$/i.test(file.fileName)))
const selectedStateCount = computed(() => stateRows.value.filter(row => row.selected).length)
const selectedHistoryCount = computed(() => historyRows.value.filter(row => row.selected).length)
const selectedDownloadRows = computed(() => (downloadKind.value === 'state' ? stateRows.value : historyRows.value).filter(row => row.selected))
const readyUploads = computed(() => uploadRows.value.filter(row => row.result === 'READY').length)
const productChoice = computed(() => assignProduct.value ? assignProduct.value.productNo + ' / ' + assignProduct.value.productVersion + ' · ' + assignProduct.value.deviceNo : '尚未選擇產品')

function timeText(): string { return new Date().toLocaleString('zh-TW', { hour12: false }) }
function addLog(action: string, file: string, result: string, detail: string): void {
  actionLog.value.unshift({ id: nextId.value++, time: timeText(), action, file, result, detail })
  actionLog.value = actionLog.value.slice(0, 30)
}
function clearNotice(): void { error.value = '' }
function normalizeRows(rows: GoldenMapRow[]): GoldenMapRow[] { return rows.map(row => ({ ...row, selected: false })) }
function matchesLike(value: string, pattern: string): boolean {
  const expression = pattern.replace(/[.*+?^$(){}|[\]\\]/g, '\\$&').replace(/%/g, '.*').replace(/_/g, '.')
  return new RegExp('^' + expression + '$', 'i').test(value)
}

function persist(): void {
  if (!dev.value) return
  try {
    localStorage.setItem(storageKey, JSON.stringify({ state: stateStore.value, history: historyStore.value }))
  } catch {
    notice.value = '瀏覽器無法儲存 DEV fixtures；本次操作仍保留在頁面記憶體。'
  }
}

function queryRows(rows: GoldenMapRow[]): GoldenMapRow[] {
  const device = deviceNo.value.trim().toUpperCase()
  const product = productNo.value.trim().toUpperCase()
  const from = dateEnabled.value ? new Date(fromDate.value).getTime() : Number.NEGATIVE_INFINITY
  const to = dateEnabled.value ? new Date(toDate.value).getTime() : Number.POSITIVE_INFINITY
  return normalizeRows(rows.filter(row => {
    // DEV follows the visible checkbox intent. The WinForms request builder currently
    // comments out the checkbox guards and overwrites its DeviceNo parameter.
    if (deviceEnabled.value && device && !matchesLike(row.deviceNo, device)) return false
    if (productEnabled.value && product && !matchesLike(row.productNo, product)) return false
    if (dateEnabled.value) {
      const upload = new Date(row.uploadDate.replace(' ', 'T')).getTime()
      if (upload < from || upload > to) return false
    }
    return true
  }))
}

function search(): void {
  clearNotice()
  stateRows.value = queryRows(stateStore.value)
  historyRows.value = queryRows(historyStore.value)
  lastQuery.value = timeText()
  const unrestricted = !(deviceEnabled.value && deviceNo.value.trim()) && !(productEnabled.value && productNo.value.trim()) && !dateEnabled.value
  notice.value = '已載入 ' + stateRows.value.length + ' 筆現況、' + historyRows.value.length + ' 筆歷程（DEV fixtures）。' + (unrestricted ? '來源允許空條件查詢；正式 API 應避免無界讀取。' : '')
}

function setRowSelected(kind: 'state' | 'history', rowId: string, checked: boolean): void {
  const target = kind === 'state' ? stateRows : historyRows
  target.value = target.value.map(row => row.id === rowId ? { ...row, selected: checked } : row)
}
function selectAll(kind: 'state' | 'history', checked: boolean): void {
  const target = kind === 'state' ? stateRows : historyRows
  target.value = target.value.map(row => ({ ...row, selected: checked }))
}

function openUpdate(): void {
  clearNotice()
  updateMode.value = 'original'
  assignProduct.value = null
  allFiles.value = false
  autoRename.value = false
  selectedFixtureIds.value = []
  uploadRows.value = []
  uploadProgress.value = 0
  uploadSummary.value = ''
  updateOpen.value = true
}
function changeUpdateMode(mode: 'original' | 'assign'): void {
  updateMode.value = mode
  selectedFixtureIds.value = []
  uploadRows.value = []
  uploadProgress.value = 0
  uploadSummary.value = ''
  if (mode === 'original') {
    assignProduct.value = null
    autoRename.value = false
  }
}
function chooseProduct(product: typeof products[number]): void {
  assignProduct.value = product
  productPickerOpen.value = false
  clearNotice()
}
function toggleFile(id: string): void {
  selectedFixtureIds.value = selectedFixtureIds.value.includes(id)
    ? selectedFixtureIds.value.filter(value => value !== id)
    : updateMode.value === 'assign' ? [id] : [...selectedFixtureIds.value, id]
}

function addFiles(): void {
  clearNotice()
  if (updateMode.value === 'assign' && !assignProduct.value) {
    error.value = 'Assign Product Info 模式請先選取產品及版本。'
    return
  }
  const chosen = filteredFixtures.value.filter(file => selectedFixtureIds.value.includes(file.id))
  if (!chosen.length) {
    error.value = '請在 DEV 檔案清單選取檔案。'
    return
  }
  const errors: string[] = []
  for (const file of chosen) {
    if (!file.valid && updateMode.value === 'original') {
      errors.push(file.fileName + '：來源檔名不能解析 ProductNo-Version。')
      continue
    }
    let product = assignProduct.value
    let targetName = file.fileName
    if (updateMode.value === 'original') {
      const stem = file.fileName.replace(/\.[^.]+$/, '')
      const separator = stem.lastIndexOf('-')
      if (separator <= 0 || !stem.slice(separator + 1)) {
        errors.push(file.fileName + '：檔名須為 ProductNo-ProductVersion.ext。')
        continue
      }
      product = { productNo: stem.slice(0, separator), productVersion: stem.slice(separator + 1), deviceNo: 'DEV-CP-5100' }
    } else if (autoRename.value && product) {
      targetName = product.productNo + '-' + product.productVersion + (file.fileName.match(/\.[^.]+$/)?.[0] ?? '.dat')
    }
    if (!product) continue
    const key = product.productNo.toUpperCase() + '/' + product.productVersion.toUpperCase()
    if (uploadRows.value.some(row => row.productNo.toUpperCase() + '/' + row.productVersion.toUpperCase() === key)) {
      errors.push(file.fileName + '：本批次已有相同 ProductNo／Version。')
      continue
    }
    uploadRows.value.push({
      ...file,
      productNo: product.productNo,
      productVersion: product.productVersion,
      targetName,
      result: 'READY',
      message: updateMode.value === 'original' ? '由檔名解析產品與版本' : autoRename.value ? '依選定產品重命名' : '保留來源檔名',
    })
  }
  selectedFixtureIds.value = []
  filePickerOpen.value = false
  if (errors.length) error.value = errors.join('；')
}

function archiveName(fileName: string): string {
  const backupRows = historyStore.value.filter(row => row.filePath.toLowerCase().includes('/backup/'))
  const exactBackupExists = backupRows.some(row => row.fileName.toUpperCase() === fileName.toUpperCase())
  if (!exactBackupExists) return fileName
  const stem = fileName.replace(/\.[^.]+$/, '')
  const used = backupRows
    .filter(row => row.fileName.toUpperCase().startsWith(stem.toUpperCase() + '_'))
    .map(row => Number(row.fileName.match(/_(\d{4})\.[^.]+$/)?.[1] ?? 0))
  return stem + '_' + String(Math.max(0, ...used) + 1).padStart(4, '0') + '.dat'
}
function archiveCurrent(row: GoldenMapRow, action: string): void {
  const fileName = archiveName(row.fileName)
  let matched = 0
  historyStore.value = historyStore.value.map(item => {
    const sameFile = item.productNo.toUpperCase() === row.productNo.toUpperCase()
      && item.productVersion.toUpperCase() === row.productVersion.toUpperCase()
      && item.fileName === row.fileName
      && item.filePath === row.filePath
    if (!sameFile) return item
    matched++
    return { ...item, fileName, filePath: 'DEV://golden/backup/', selected: false }
  })
  addLog(action, row.fileName, '已備份', 'DEV 只記錄備份名稱 ' + fileName + '；未操作遠端檔案。')
  if (!matched) addLog(action, row.fileName, '需對帳', '找不到 State 對應的 Log row；DEV 保留來源分散式操作可能不一致的狀況。')
}

function confirmUpdate(): void {
  clearNotice()
  if (!uploadRows.value.length) {
    error.value = '請先加入至少一個待處理檔案。'
    return
  }
  let success = 0
  uploadProgress.value = 0
  for (const upload of uploadRows.value) {
    const existingIndex = stateStore.value.findIndex(row => row.productNo.toUpperCase() === upload.productNo.toUpperCase() && row.productVersion.toUpperCase() === upload.productVersion.toUpperCase())
    if (existingIndex >= 0) {
      const current = stateStore.value[existingIndex]
      if (current.filePath.toLowerCase().includes('missing')) {
        addLog('更新 Golden MAP', current.fileName, '需對帳', '模擬 State 檔不存在；來源會略過舊檔備份並嘗試上傳新檔。')
      } else {
        archiveCurrent(current, '更新備份')
      }
    }
    const row: GoldenMapRow = {
      id: 'GS-DEV-' + nextId.value++,
      deviceNo: assignProduct.value?.deviceNo ?? products.find(item => item.productNo === upload.productNo)?.deviceNo ?? 'DEV-CP-5100',
      productNo: upload.productNo,
      productVersion: upload.productVersion,
      orgFileName: upload.fileName,
      fileName: upload.targetName,
      fileLength: upload.fileLength,
      filePath: 'DEV://golden/state/',
      uploadUserNo: userNo.value,
      uploadDate: timeText(),
      selected: false,
    }
    if (existingIndex >= 0) stateStore.value.splice(existingIndex, 1, row)
    else stateStore.value.unshift(row)
    historyStore.value.unshift({ ...row, id: 'GH-DEV-' + nextId.value++, filePath: 'DEV://golden/state/', selected: false })
    addLog('上傳 Golden MAP', row.fileName, '成功', 'State 依 ProductNo+ProductVersion 覆蓋；Log 新增一筆，僅更新 DEV fixtures。')
    success++
    uploadProgress.value++
  }
  uploadSummary.value = '已完成 ' + uploadProgress.value + ' / ' + uploadRows.value.length + ' 筆。'
  uploadRows.value = uploadRows.value.map(row => ({ ...row, result: 'DONE', message: 'DEV 模擬成功' }))
  persist()
  search()
  notice.value = 'DEV 模擬完成：' + success + ' 筆上傳成功，State 已依產品／版本更新，歷程已新增。'
}

function scrapSelected(): void {
  clearNotice()
  const selected = stateRows.value.filter(row => row.selected)
  if (!selected.length) {
    error.value = '請先勾選要作廢的目前檔案。'
    return
  }
  let success = 0
  let failed = 0
  for (const row of selected) {
    if (row.filePath.toLowerCase().includes('missing')) {
      addLog('作廢 Golden MAP', row.fileName, '失敗', '模擬遠端 State 檔不存在；保留 State row。')
      failed++
      continue
    }
    archiveCurrent(row, '作廢備份')
    stateStore.value = stateStore.value.filter(item => item.id !== row.id)
    addLog('作廢 Golden MAP', row.fileName, '成功', 'DEV 模擬從 State 移除；來源會先備份檔案，再刪除遠端檔與 State。')
    success++
  }
  persist()
  search()
  notice.value = 'DEV Scrap 完成 ' + success + ' 筆' + (failed ? '；' + failed + ' 筆遠端檔缺失而保留 State。' : '。')
}

function openDownload(kind: 'state' | 'history'): void {
  clearNotice()
  downloadKind.value = kind
  overwriteExisting.value = false
  downloadOpen.value = true
}
function downloadSelected(): void {
  const rows = selectedDownloadRows.value
  if (!rows.length) {
    error.value = '請先勾選要下載的 ' + (downloadKind.value === 'state' ? '目前檔案' : '歷程檔案') + '。'
    return
  }
  let success = 0
  let failed = 0
  for (const row of rows) {
    if (row.filePath.toLowerCase().includes('missing')) {
      addLog(downloadKind.value === 'state' ? '下載目前檔案' : '下載歷程檔案', row.fileName, '失敗', '模擬遠端檔案不存在。')
      failed++
      continue
    }
    addLog(downloadKind.value === 'state' ? '下載目前檔案' : '下載歷程檔案', row.fileName, '模擬完成', '目標資料夾 ' + downloadTarget.value + (overwriteExisting.value ? '；已模擬覆寫確認' : '') + '，未建立本機檔案。')
    success++
  }
  downloadOpen.value = false
  notice.value = 'DEV 下載完成 ' + success + ' 筆' + (failed ? '；' + failed + ' 筆遠端檔案缺失。' : '；沒有寫入本機檔案。')
}

function resetFixtures(): void {
  stateStore.value = initialState.map(row => ({ ...row }))
  historyStore.value = initialHistory.map(row => ({ ...row }))
  persist()
  search()
  notice.value = 'DEV fixtures 已還原初始資料。'
}

onMounted(() => {
  if (!dev.value) return
  try {
    const raw = localStorage.getItem(storageKey)
    if (raw) {
      const parsed = JSON.parse(raw) as { state?: GoldenMapRow[]; history?: GoldenMapRow[] }
      if (Array.isArray(parsed.state) && Array.isArray(parsed.history)) {
        stateStore.value = parsed.state
        historyStore.value = parsed.history
      }
    }
  } catch {
    notice.value = 'DEV 儲存內容無法讀取，改用頁面初始 fixtures。'
  }
  search()
})
watch([stateStore, historyStore], persist, { deep: true })
</script>

<template>
  <section v-if="dev" class="golden-map-dev" aria-label="Golden MAP 檔案管理 DEV">
    <header class="golden-header">
      <div><p class="eyebrow">WIP · WPP05 · DEVELOPMENT PREVIEW</p><h1>Golden MAP 檔案管理</h1><p class="subheading">產品／版本標準圖譜現況、歷程與檔案維護</p></div>
      <div class="header-actions"><button type="button" class="button secondary" @click="resetFixtures">還原 DEV 資料</button><button type="button" class="button secondary" @click="emit('close')">Close (X)</button></div>
    </header>

    <div class="notice" role="status"><span class="status-dot"></span>{{ notice }}<span v-if="lastQuery" class="query-time">最近查詢 {{ lastQuery }}</span></div>
    <p v-if="error" class="error" role="alert">{{ error }}</p>

    <section class="panel query-panel">
      <div class="section-heading"><div><p class="eyebrow">SEARCH</p><h2>查詢條件</h2></div><button type="button" class="button primary" @click="search">Query</button></div>
      <div class="query-grid">
        <label class="filter-control"><input v-model="deviceEnabled" type="checkbox"><span>DeviceNo</span><input v-model="deviceNo" type="text" :disabled="!deviceEnabled" placeholder="DEV-CP-5100"></label>
        <label class="filter-control"><input v-model="productEnabled" type="checkbox"><span>ProductNo</span><input v-model="productNo" type="text" :disabled="!productEnabled" placeholder="DEV-MAP-A12"></label>
        <label class="date-filter"><input v-model="dateEnabled" type="checkbox"><span>UploadTime</span><input v-model="fromDate" type="datetime-local" :disabled="!dateEnabled"><span class="range-separator">至</span><input v-model="toDate" type="datetime-local" :disabled="!dateEnabled"></label>
      </div>
      <p class="hint">DEV 依勾選欄位查詢。來源可由 Core 將 DeviceNo 映射至產品，但 WinForms 請求組字串時第二次 assignment 會覆蓋 DeviceNo；相關 checkbox 守門也已註解，DEV 保留可用的畫面查詢意圖。查詢同時載入 State 與 History。</p>
    </section>

    <section class="panel">
      <div class="section-heading"><div><p class="eyebrow">CURRENT STATE · {{ stateRows.length }} ROWS</p><h2>Golden MAP File Data</h2></div>
        <div class="toolbar"><span class="selection-count">已選 {{ selectedStateCount }}</span><button type="button" class="button primary" @click="openUpdate">Update File</button><button type="button" class="button danger" @click="scrapSelected">Scrap File</button><button type="button" class="button secondary" @click="openDownload('state')">Download File</button></div>
      </div>
      <div class="table-wrap"><table><thead><tr><th><input type="checkbox" aria-label="全選現況" :checked="stateRows.length > 0 && selectedStateCount === stateRows.length" @change="selectAll('state', ($event.target as HTMLInputElement).checked)"></th><th>ProductNo</th><th>ProductVersion</th><th>FileName</th><th>UploadUserNo</th><th>UploadDate</th></tr></thead>
        <tbody><tr v-for="row in stateRows" :key="row.id"><td><input type="checkbox" :checked="row.selected" :aria-label="'選取 ' + row.fileName" @change="setRowSelected('state', row.id, ($event.target as HTMLInputElement).checked)"></td><td>{{ row.productNo }}</td><td>{{ row.productVersion }}</td><td class="file-name">{{ row.fileName }}</td><td>{{ row.uploadUserNo }}</td><td>{{ row.uploadDate }}</td></tr><tr v-if="!stateRows.length"><td colspan="6" class="empty">沒有符合條件的現況檔案</td></tr></tbody></table></div>
    </section>

    <section class="panel">
      <div class="section-heading"><div><p class="eyebrow">HISTORY · {{ historyRows.length }} ROWS</p><h2>Golden MAP File History Data</h2></div><div class="toolbar"><span class="selection-count">已選 {{ selectedHistoryCount }}</span><button type="button" class="button secondary" @click="openDownload('history')">Download File</button></div></div>
      <div class="table-wrap"><table><thead><tr><th><input type="checkbox" aria-label="全選歷程" :checked="historyRows.length > 0 && selectedHistoryCount === historyRows.length" @change="selectAll('history', ($event.target as HTMLInputElement).checked)"></th><th>ProductNo</th><th>ProductVersion</th><th>FileName</th><th>UploadUserNo</th><th>UploadDate</th></tr></thead>
        <tbody><tr v-for="row in historyRows" :key="row.id"><td><input type="checkbox" :checked="row.selected" :aria-label="'選取 ' + row.fileName" @change="setRowSelected('history', row.id, ($event.target as HTMLInputElement).checked)"></td><td>{{ row.productNo }}</td><td>{{ row.productVersion }}</td><td class="file-name">{{ row.fileName }}</td><td>{{ row.uploadUserNo }}</td><td>{{ row.uploadDate }}</td></tr><tr v-if="!historyRows.length"><td colspan="6" class="empty">沒有符合條件的歷程檔案</td></tr></tbody></table></div>
    </section>

    <section class="panel log-panel"><div class="section-heading"><div><p class="eyebrow">DEV ACTIVITY</p><h2>本頁操作紀錄</h2></div><span class="mock-badge">僅本機 fixtures</span></div>
      <div v-if="actionLog.length" class="log-list"><article v-for="entry in actionLog" :key="entry.id" class="log-row"><time>{{ entry.time }}</time><strong>{{ entry.action }}</strong><span class="file-name">{{ entry.file }}</span><span class="result">{{ entry.result }}</span><small>{{ entry.detail }}</small></article></div><p v-else class="empty">尚無操作紀錄</p>
    </section>

    <div v-if="updateOpen" class="modal-backdrop" role="presentation" @click.self="updateOpen = false">
      <section class="modal update-modal" role="dialog" aria-modal="true" aria-label="Update Golden MAP File">
        <header class="modal-header"><div><p class="eyebrow">UPDATE FILE</p><h2>Golden MAP 檔案更新</h2></div><button class="icon-button" type="button" aria-label="關閉" @click="updateOpen = false">×</button></header>
        <div class="mode-tabs"><label><input type="radio" value="original" :checked="updateMode === 'original'" @change="changeUpdateMode('original')"> Org File Update</label><label><input type="radio" value="assign" :checked="updateMode === 'assign'" @change="changeUpdateMode('assign')"> Assign Product Info</label></div>
        <div v-if="updateMode === 'assign'" class="product-select"><span>產品／版本</span><strong>{{ productChoice }}</strong><button type="button" class="button secondary" @click="productPickerOpen = true">選取產品</button><label class="inline-check"><input v-model="autoRename" type="checkbox">依產品版本重新命名</label></div>
        <div class="file-tools"><div><h3>選取 DEV 檔案 fixture</h3><p>{{ updateMode === 'original' ? '可多選 .dat；檔名須含 ProductNo-ProductVersion。' : '單選檔案；可選擇保留原檔名或使用產品版本命名。' }}</p></div><label class="inline-check"><input v-model="allFiles" type="checkbox">All File</label></div>
        <button type="button" class="button secondary" @click="filePickerOpen = !filePickerOpen">{{ filePickerOpen ? '收合檔案清單' : '選取檔案…' }}</button>
        <div v-if="filePickerOpen" class="fixture-list"><label v-for="file in filteredFixtures" :key="file.id" class="fixture-row"><input type="checkbox" :checked="selectedFixtureIds.includes(file.id)" :disabled="updateMode === 'assign' && selectedFixtureIds.length > 0 && !selectedFixtureIds.includes(file.id)" @change="toggleFile(file.id)"><span><strong>{{ file.fileName }}</strong><small>{{ file.fileLength.toLocaleString() }} bytes · {{ file.createdAt }}</small></span><span :class="file.valid ? 'valid' : 'invalid'">{{ file.valid ? '可解析' : '格式錯誤案例' }}</span></label><p v-if="!filteredFixtures.length" class="empty">目前副檔名篩選沒有 fixture。</p></div>
        <div v-if="uploadRows.length" class="staged"><div class="file-tools"><h3>待更新清單 <span class="mock-badge">READY {{ readyUploads }}</span></h3><button type="button" class="button secondary" @click="uploadRows = []">清空</button></div><div v-for="(row, index) in uploadRows" :key="row.id" class="staged-row"><span class="staged-index">{{ index + 1 }}</span><span><strong>{{ row.targetName }}</strong><small>{{ row.productNo }} / {{ row.productVersion }} · {{ row.fileLength.toLocaleString() }} bytes</small></span><span :class="row.result === 'ERROR' ? 'invalid' : 'valid'">{{ row.result }} · {{ row.message }}</span></div><div v-if="uploadSummary" class="progress-line"><progress :max="Math.max(1, uploadRows.length)" :value="uploadProgress"></progress><span>{{ uploadSummary }}</span></div></div>
        <p v-if="error" class="error" role="alert">{{ error }}</p>
        <div class="modal-actions"><button type="button" class="button secondary" @click="updateOpen = false">Close</button><button type="button" class="button primary" :disabled="readyUploads === 0" @click="confirmUpdate">Confirm DEV Update</button></div>
        <p class="modal-note">不會開啟本機檔案選擇器或連 File Service；以上為固定 DEV fixture 的操作模擬。</p>
      </section>
    </div>

    <div v-if="productPickerOpen" class="modal-backdrop nested" role="presentation" @click.self="productPickerOpen = false">
      <section class="modal small-modal" role="dialog" aria-modal="true" aria-label="DEV Product Search"><header class="modal-header"><div><p class="eyebrow">DEV PRODUCT LOOKUP</p><h2>選取產品版本</h2></div><button class="icon-button" type="button" aria-label="關閉" @click="productPickerOpen = false">×</button></header><button v-for="item in products" :key="item.productNo + item.productVersion" type="button" class="product-option" @click="chooseProduct(item)"><strong>{{ item.productNo }} / {{ item.productVersion }}</strong><span>{{ item.deviceNo }}</span></button><p class="modal-note">模擬 frmCOMSearch／wsPRD.LoadProduct 的產品選取。</p></section>
    </div>

    <div v-if="downloadOpen" class="modal-backdrop" role="presentation" @click.self="downloadOpen = false">
      <section class="modal small-modal" role="dialog" aria-modal="true" aria-label="Download Golden MAP"><header class="modal-header"><div><p class="eyebrow">DOWNLOAD</p><h2>{{ downloadKind === 'state' ? '下載目前檔案' : '下載歷程檔案' }}</h2></div><button class="icon-button" type="button" aria-label="關閉" @click="downloadOpen = false">×</button></header><p>已選取 {{ selectedDownloadRows.length }} 筆</p><label class="field-label">DEV 目標資料夾<input v-model="downloadTarget" type="text"></label><label class="inline-check"><input v-model="overwriteExisting" type="checkbox">模擬目的地同名檔覆寫確認</label><div class="modal-actions"><button type="button" class="button secondary" @click="downloadOpen = false">取消</button><button type="button" class="button primary" @click="downloadSelected">模擬下載</button></div><p class="modal-note">來源會檢查 FTP 檔案存在與目的地覆寫；DEV 只記錄結果，不寫入資料夾。</p></section>
    </div>
  </section>
  <section v-else class="golden-pending"><p>Golden MAP 檔案管理正式服務尚未接入。</p><button type="button" @click="emit('close')">Close (X)</button></section>
</template>

<style scoped>
.golden-map-dev,.golden-pending{height:100%;min-height:0;overflow:auto;box-sizing:border-box;padding:18px 22px;background:#f3f2ed;color:#292922;font:inherit}
.golden-header,.section-heading,.modal-header,.file-tools{display:flex;align-items:center;justify-content:space-between;gap:16px}
.golden-header{margin-bottom:14px}.golden-header h1{margin:2px 0 3px;font-size:23px;font-weight:650}.subheading{margin:0;color:#716f66;font-size:13px}.eyebrow{margin:0 0 4px;color:#7d7869;font-size:10px;font-weight:700;letter-spacing:.12em}.header-actions,.toolbar,.modal-actions{display:flex;align-items:center;gap:8px;flex-wrap:wrap}
.notice{display:flex;align-items:center;gap:9px;min-height:36px;margin-bottom:12px;padding:6px 12px;border:1px solid #d7d2c3;border-radius:5px;background:#fbfaf6;color:#49473e;font-size:12px}.status-dot{width:8px;height:8px;flex:none;border-radius:50%;background:#6a8c58}.query-time{margin-left:auto;color:#858172;font-size:11px}
.panel{margin:0 0 12px;padding:14px 16px;border:1px solid #d9d6cb;border-radius:6px;background:#fffefa;box-shadow:0 1px 2px #312f2610}.section-heading{margin-bottom:11px}.section-heading h2,.modal-header h2{margin:0;font-size:16px;font-weight:650}.query-grid{display:grid;grid-template-columns:1fr 1fr 2fr;gap:10px}.filter-control,.date-filter{display:flex;align-items:center;gap:7px;min-width:0;color:#47453e;font-size:12px}.filter-control>span,.date-filter>span:not(.range-separator){min-width:75px;font-weight:600}.filter-control>input[type=text]{min-width:60px;flex:1}.date-filter input[type=datetime-local]{min-width:0;width:calc(50% - 35px);flex:1}.range-separator{color:#777266}.filter-control input[type=text],.date-filter input[type=datetime-local],.field-label input{height:30px;box-sizing:border-box;border:1px solid #c9c5b9;border-radius:4px;background:white;padding:4px 8px;font:inherit}.filter-control input:disabled,.date-filter input:disabled{background:#efeee9;color:#aaa}.hint,.modal-note{margin:8px 0 0;color:#807c70;font-size:11px}
.button{min-height:29px;border:1px solid #bdb9ad;border-radius:4px;padding:4px 10px;background:#fff;color:#343229;font:inherit;font-size:12px;cursor:pointer}.button:hover:not(:disabled){filter:brightness(.97)}.button:disabled{opacity:.5;cursor:not-allowed}.button.primary{border-color:#465947;background:#465947;color:white}.button.secondary{background:#f8f7f2}.button.danger{border-color:#a85547;background:#a85547;color:white}.selection-count{color:#777366;font-size:11px}
.table-wrap{overflow:auto;border:1px solid #e3e0d7;border-radius:4px}table{width:100%;border-collapse:collapse;font-size:12px;white-space:nowrap}th,td{padding:7px 9px;border-bottom:1px solid #ece9e1;text-align:left}th{background:#f4f2eb;color:#5f5b50;font-size:10px;font-weight:700;letter-spacing:.04em}tbody tr:hover{background:#faf9f4}tbody tr:last-child td{border-bottom:0}td:first-child,th:first-child{width:28px;text-align:center}.file-name{font-family:Consolas,monospace;font-size:11px}.empty{padding:20px;text-align:center;color:#908b7d;font-size:12px}.log-panel .section-heading{margin-bottom:5px}.mock-badge{display:inline-flex;align-items:center;border:1px solid #d6d0bf;border-radius:12px;padding:3px 8px;background:#f7f4e9;color:#70694f;font-size:10px;white-space:nowrap}.log-list{display:grid;gap:0}.log-row{display:grid;grid-template-columns:140px 132px minmax(150px,1fr) 75px minmax(220px,1.5fr);align-items:center;gap:10px;padding:8px 2px;border-bottom:1px solid #eeeae2;font-size:11px}.log-row time,.log-row small{color:#817c70}.log-row .result{color:#48644a;font-weight:650}.error{margin:9px 0;padding:8px 10px;border:1px solid #ddb0a9;border-radius:4px;background:#fff5f3;color:#963f33;font-size:12px}
.modal-backdrop{position:fixed;z-index:1200;inset:0;display:flex;align-items:center;justify-content:center;padding:24px;background:#211f1c75}.modal-backdrop.nested{z-index:1210}.modal{width:min(920px,100%);max-height:min(90vh,850px);overflow:auto;box-sizing:border-box;border:1px solid #d6d2c6;border-radius:7px;background:#fffefa;padding:18px;box-shadow:0 16px 48px #1115}.small-modal{width:min(540px,100%)}.modal-header{margin-bottom:14px}.icon-button{width:30px;height:30px;border:1px solid #d1cdc1;border-radius:4px;background:white;color:#524f44;font-size:20px;line-height:1;cursor:pointer}.mode-tabs{display:flex;gap:20px;padding:10px 0;border-top:1px solid #ece9e1;border-bottom:1px solid #ece9e1;font-size:12px}.mode-tabs label,.inline-check{display:inline-flex;align-items:center;gap:6px;font-size:12px}.product-select{display:grid;grid-template-columns:100px minmax(0,1fr) auto auto;align-items:center;gap:10px;margin:12px 0;padding:10px;border:1px solid #e6e2d7;border-radius:4px;background:#faf9f4;font-size:12px}.product-select>span{color:#736f62}.file-tools{margin:13px 0 8px}.file-tools h3{margin:0;font-size:13px}.file-tools p{margin:3px 0 0;color:#7f7a6d;font-size:11px}.fixture-list,.staged{margin-top:10px;border:1px solid #e3e0d7;border-radius:4px}.fixture-row,.staged-row{display:grid;grid-template-columns:24px minmax(0,1fr) auto;align-items:center;gap:9px;padding:8px 10px;border-bottom:1px solid #eeeae2;font-size:11px}.fixture-row:last-child,.staged-row:last-child{border-bottom:0}.fixture-row span:nth-child(2),.staged-row span:nth-child(2){display:grid;gap:3px}.fixture-row small,.staged-row small{color:#817c70}.valid{color:#4d744e;font-size:10px}.invalid{color:#a14a3e;font-size:10px}.staged{padding:0 10px 8px}.staged>.file-tools{margin:8px 0}.staged-row{grid-template-columns:20px minmax(0,1fr) auto;padding:7px 2px}.staged-index{color:#898476}.modal-actions{justify-content:flex-end;margin-top:16px;padding-top:12px;border-top:1px solid #ece9e1}.field-label{display:grid;gap:5px;margin:12px 0;color:#5e5a50;font-size:11px}.product-option{display:flex;width:100%;justify-content:space-between;gap:12px;margin:5px 0;padding:10px;border:1px solid #e1ddd2;border-radius:4px;background:#fff;text-align:left;font:inherit;font-size:12px;cursor:pointer}.product-option:hover{background:#f7f5ee}.product-option span{color:#797466}.golden-pending{height:auto}.golden-pending p{margin:0 0 10px;color:#777366}.golden-pending button{min-height:28px;padding:4px 10px;border:1px solid #3b3930;border-radius:3px;background:#3b3930;color:#fff;font:inherit;cursor:pointer}
.progress-line{display:flex;align-items:center;gap:10px;margin-top:8px;color:#716c60;font-size:11px}.progress-line progress{width:min(320px,60%);height:12px;accent-color:#586d50}
@media(max-width:900px){.query-grid{grid-template-columns:1fr 1fr}.date-filter{grid-column:1/-1}.log-row{grid-template-columns:1fr 1fr;gap:5px}.log-row small{grid-column:1/-1}.product-select{grid-template-columns:100px minmax(0,1fr) auto}.product-select .inline-check{grid-column:2/-1}.header-actions{align-self:flex-start}}
@media(max-width:620px){.golden-map-dev{padding:12px}.golden-header,.section-heading{align-items:flex-start;flex-direction:column}.query-grid{grid-template-columns:1fr}.date-filter{grid-column:auto;flex-wrap:wrap}.date-filter input[type=datetime-local]{width:calc(50% - 28px)}.toolbar{width:100%}.product-select{grid-template-columns:1fr}.product-select .inline-check{grid-column:auto}.modal-backdrop{padding:8px}}
</style>
