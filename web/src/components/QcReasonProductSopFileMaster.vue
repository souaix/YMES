<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

interface ReasonItem {
  reasonNo: string
  reasonName: string
  reasonLevel: string
  reasonType: string
  reasonSubType: string
  description: string
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
  issueState: number
  dataStamp: number | null
}
interface ProductItem { productNo: string; productName: string; issueState: number; currentVersion: number }
interface SopFileItem { reasonNo: string; productNo: string; sopFileName: string; sopFilePath: string }
interface MockStore { reasons: ReasonItem[]; products: ProductItem[]; files: SopFileItem[] }
interface ImportItem extends SopFileItem { result: string; message: string }
type ListPayload<T> = T[] | { items?: T[]; reasons?: T[]; products?: T[]; files?: T[] }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const apiRoot = 'api/qc/reason-product-sop-file'
const storageKey = 'ymes-dev-mock:qc-reason-product-sop-file:v1'
const reasons = ref<ReasonItem[]>([])
const files = ref<SopFileItem[]>([])
const selectedReasonNo = ref('')
const selectedAvailableProducts = ref<string[]>([])
const selectedAssignedProducts = ref<string[]>([])
const availableProducts = ref<ProductItem[]>([])
const productSearch = ref('')
const selectedFile = ref<File | null>(null)
const selectedFileName = ref('')
const fileInput = ref<HTMLInputElement | null>(null)
const workbookInput = ref<HTMLInputElement | null>(null)
const attachmentInput = ref<HTMLInputElement | null>(null)
const attachments = ref<File[]>([])
const importWorkbook = ref<File | null>(null)
const importRows = ref<ImportItem[]>([])
const loading = ref(false)
const loadingFiles = ref(false)
const saving = ref(false)
const importing = ref(false)
const cancelImportRequested = ref(false)
const importProgress = ref(0)
const errorMessage = ref('')
const notice = ref('')
const importMessage = ref('')
const importError = ref('')
const maintainOpen = ref(false)
const importOpen = ref(false)
const userNo = computed(() => props.currentUserNo?.trim() || (mock.value ? 'DEV001' : ''))
const selectedReason = computed(() => reasons.value.find(item => item.reasonNo === selectedReasonNo.value) ?? null)
const currentFiles = computed(() => files.value.filter(item => item.reasonNo === selectedReasonNo.value))
const assignedProductSet = computed(() => new Set(currentFiles.value.map(item => item.productNo)))
const filteredAvailableProducts = computed(() => availableProducts.value.filter(item => !assignedProductSet.value.has(item.productNo)))
const importPercent = computed(() => importRows.value.length ? Math.round(importProgress.value * 100 / importRows.value.length) : 0)

function makeDemoStore(): MockStore {
  return {
    reasons: [
      { reasonNo: 'DEV-RSN-001', reasonName: '外觀破損（DEMO）', reasonLevel: '1', reasonType: '1', reasonSubType: '', description: '開發用假原因', creator: 'DEV001', createDate: '2026/11/17 08:00:00', reviser: '', reviseDate: '', issueState: 2, dataStamp: 0 },
      { reasonNo: 'DEV-RSN-002', reasonName: '尺寸異常（DEMO）', reasonLevel: '2', reasonType: '1', reasonSubType: '', description: '不含 MES 實際資料', creator: 'DEV001', createDate: '2026/11/17 08:00:00', reviser: '', reviseDate: '', issueState: 2, dataStamp: 0 },
      { reasonNo: 'DEV-RSN-003', reasonName: '報廢原因（DEMO）', reasonLevel: '3', reasonType: '0', reasonSubType: '', description: '', creator: 'DEV001', createDate: '2026/11/17 08:00:00', reviser: '', reviseDate: '', issueState: 2, dataStamp: 0 },
    ],
    products: Array.from({ length: 12 }, (_, index) => ({
      productNo: `DEV-PD-${String(index + 1).padStart(3, '0')}`,
      productName: `開發用產品 ${index + 1}`,
      issueState: 2,
      currentVersion: 1,
    })),
    files: [
      { reasonNo: 'DEV-RSN-001', productNo: 'DEV-PD-001', sopFileName: 'DEV-appearance.pdf', sopFilePath: '/dev-files/DEV-appearance.pdf' },
      { reasonNo: 'DEV-RSN-001', productNo: 'DEV-PD-002', sopFileName: 'DEV-appearance.pdf', sopFilePath: '/dev-files/DEV-appearance.pdf' },
      { reasonNo: 'DEV-RSN-002', productNo: 'DEV-PD-003', sopFileName: 'DEV-dimension.png', sopFilePath: '/dev-files/DEV-dimension.png' },
    ],
  }
}
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function readMockStore(): MockStore {
  try {
    const raw = window.localStorage.getItem(storageKey)
    if (raw) {
      const store = JSON.parse(raw) as MockStore
      if (Array.isArray(store.reasons) && Array.isArray(store.products) && Array.isArray(store.files)) return store
    }
  } catch { /* initialize fresh DEV-only data */ }
  const store = makeDemoStore()
  window.localStorage.setItem(storageKey, JSON.stringify(store))
  return store
}
function writeMockStore(store: MockStore): void { window.localStorage.setItem(storageKey, JSON.stringify(store)) }
function listOf<T>(payload: ListPayload<T>, key: 'items' | 'reasons' | 'products' | 'files' = 'items'): T[] {
  if (Array.isArray(payload)) return payload
  return payload[key] ?? payload.items ?? []
}
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body && !(init.body instanceof FormData) && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init, headers })
  if (response.status === 401) { window.location.assign(appBase + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; title?: string; message?: string } | null
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 呼叫失敗（${response.status}）。`)
  }
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}
function normalizeReason(value: Record<string, unknown>): ReasonItem {
  return {
    reasonNo: String(value.reasonNo ?? value.REASONNO ?? ''),
    reasonName: String(value.reasonName ?? value.REASONNAME ?? ''),
    reasonLevel: String(value.reasonLevel ?? value.REASONLEVEL ?? ''),
    reasonType: String(value.reasonType ?? value.REASONTYPE ?? ''),
    reasonSubType: String(value.reasonSubType ?? value.REASONSUBTYPE ?? ''),
    description: String(value.description ?? value.DESCRIPTION ?? ''),
    creator: String(value.creator ?? value.CREATOR ?? ''),
    createDate: String(value.createDate ?? value.CREATEDATE ?? ''),
    reviser: String(value.reviser ?? value.REVISER ?? ''),
    reviseDate: String(value.reviseDate ?? value.REVISEDATE ?? ''),
    issueState: Number(value.issueState ?? value.ISSUESTATE ?? 2),
    dataStamp: value.dataStamp === null || value.DATASTAMP === null ? null : Number(value.dataStamp ?? value.DATASTAMP ?? 0),
  }
}
function normalizeProduct(value: Record<string, unknown>): ProductItem {
  return {
    productNo: String(value.productNo ?? value.PRODUCTNO ?? ''),
    productName: String(value.productName ?? value.PRODUCTNAME ?? ''),
    issueState: Number(value.issueState ?? value.ISSUESTATE ?? 2),
    currentVersion: Number(value.currentVersion ?? value.curVersion ?? value.CURVERSION ?? 1),
  }
}
function normalizeFile(value: Record<string, unknown>): SopFileItem {
  return {
    reasonNo: String(value.reasonNo ?? value.REASONNO ?? ''),
    productNo: String(value.productNo ?? value.PRODUCTNO ?? ''),
    sopFileName: String(value.sopFileName ?? value.fileName ?? value.SOPFILENAME ?? ''),
    sopFilePath: String(value.displayPath ?? value.sopFilePathDisplay ?? value.sopFilePath ?? value.filePath ?? value.SOPFILEPATH ?? ''),
  }
}
async function loadFiles(): Promise<void> {
  if (!selectedReasonNo.value) { files.value = []; return }
  loadingFiles.value = true
  try {
    let normalized: SopFileItem[]
    if (mock.value) {
      normalized = readMockStore().files.filter(item => item.reasonNo === selectedReasonNo.value)
    } else {
      const payload = await request<ListPayload<Record<string, unknown>>>(`${apiRoot}/reasons/${encodeURIComponent(selectedReasonNo.value)}/files`)
      normalized = listOf(payload, 'files').map(item => normalizeFile(item))
    }
    files.value = [...files.value.filter(item => item.reasonNo !== selectedReasonNo.value), ...normalized]
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '載入產品指導書資料失敗。' }
  finally { loadingFiles.value = false }
}
async function loadReasons(): Promise<void> {
  loading.value = true
  errorMessage.value = ''
  try {
    if (mock.value) {
      reasons.value = readMockStore().reasons.filter(item => item.issueState === 2)
    } else {
      const payload = await request<ListPayload<Record<string, unknown>>>(`${apiRoot}/reasons?issueState=2`)
      reasons.value = listOf(payload, 'reasons').map(item => normalizeReason(item))
    }
    if (!reasons.value.some(item => item.reasonNo === selectedReasonNo.value)) selectedReasonNo.value = reasons.value[0]?.reasonNo ?? ''
    await loadFiles()
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '載入核准品質原因失敗。' }
  finally { loading.value = false }
}
async function selectReason(reason: ReasonItem): Promise<void> {
  if (selectedReasonNo.value === reason.reasonNo) return
  selectedReasonNo.value = reason.reasonNo
  errorMessage.value = ''
  notice.value = ''
  await loadFiles()
}
function reasonTypeLabel(value: string): string {
  return ({ '0': 'Scrap', '1': 'Defect', '5': 'Adjust', '6': 'EQP' } as Record<string, string>)[value] ?? value
}
function displayDate(value: string): string { return value ? value.replace('T', ' ').slice(0, 19) : '' }

function openMaintain(): void {
  if (!selectedReason.value) { errorMessage.value = '請先選取品質原因。'; return }
  productSearch.value = ''
  availableProducts.value = []
  selectedAvailableProducts.value = []
  selectedAssignedProducts.value = []
  selectedFile.value = null
  selectedFileName.value = ''
  maintainOpen.value = true
}
async function searchProducts(): Promise<void> {
  const query = productSearch.value.trim()
  if (!query) { errorMessage.value = '請輸入 ProductNo 查詢條件；輸入 * 可查詢全部核准中版本。'; return }
  errorMessage.value = ''
  try {
    if (mock.value) {
      availableProducts.value = readMockStore().products.filter(item => item.issueState === 2 && item.currentVersion === 1 && (query === '*' || item.productNo.toLowerCase().includes(query.toLowerCase())))
    } else {
      const payload = await request<ListPayload<Record<string, unknown>>>(`${apiRoot}/products?issueState=2&currentVersion=1&search=${encodeURIComponent(query)}`)
      availableProducts.value = listOf(payload, 'products').map(item => normalizeProduct(item))
    }
    selectedAvailableProducts.value = []
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '查詢產品資料失敗。' }
}
function toggleAvailable(productNo: string, checked: boolean): void {
  selectedAvailableProducts.value = checked
    ? [...new Set([...selectedAvailableProducts.value, productNo])]
    : selectedAvailableProducts.value.filter(item => item !== productNo)
}
function toggleAssigned(productNo: string, checked: boolean): void {
  selectedAssignedProducts.value = checked
    ? [...new Set([...selectedAssignedProducts.value, productNo])]
    : selectedAssignedProducts.value.filter(item => item !== productNo)
}
function addOneProduct(productNo: string): void {
  selectedAvailableProducts.value = [productNo]
  void addSelectedProducts()
}
function removeOneProduct(productNo: string): void {
  selectedAssignedProducts.value = [productNo]
  void removeSelectedProducts()
}
function chooseSopFile(event: Event): void {
  const input = event.target as HTMLInputElement
  selectedFile.value = input.files?.[0] ?? null
  selectedFileName.value = selectedFile.value?.name ?? ''
}
async function addSelectedProducts(): Promise<void> {
  const reason = selectedReason.value
  const file = selectedFile.value
  if (!reason) { errorMessage.value = '請先選取品質原因。'; return }
  if (!file) { errorMessage.value = '請先選擇 SOP 檔案。'; fileInput.value?.click(); return }
  if (!selectedAvailableProducts.value.length) { errorMessage.value = '請在 UnSelected 清單選取產品。'; return }
  saving.value = true
  errorMessage.value = ''
  try {
    for (const productNo of selectedAvailableProducts.value) {
      if (mock.value) {
        const store = readMockStore()
        if (store.files.some(item => item.reasonNo === reason.reasonNo && item.productNo === productNo)) continue
        store.files.push({ reasonNo: reason.reasonNo, productNo, sopFileName: file.name, sopFilePath: `/dev-files/${encodeURIComponent(file.name)}` })
        writeMockStore(store)
      } else {
        const body = new FormData()
        body.append('file', file)
        body.append('sopFileName', file.name)
        await request(`${apiRoot}/reasons/${encodeURIComponent(reason.reasonNo)}/products/${encodeURIComponent(productNo)}/file`, { method: 'POST', body })
      }
    }
    await loadFiles()
    selectedAvailableProducts.value = []
    selectedAssignedProducts.value = []
    selectedFile.value = null
    selectedFileName.value = ''
    if (fileInput.value) fileInput.value.value = ''
    notice.value = mock.value ? 'DEV 假檔案對應已保存至瀏覽器。' : '產品指導書已更新。'
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '新增產品指導書失敗。' }
  finally { saving.value = false }
}
async function removeSelectedProducts(): Promise<void> {
  const reason = selectedReason.value
  if (!reason || !selectedAssignedProducts.value.length) { errorMessage.value = '請在 Selected 清單選取產品。'; return }
  if (!window.confirm(`確定移除 ${selectedAssignedProducts.value.length} 筆產品指導書對應？`)) return
  saving.value = true
  errorMessage.value = ''
  try {
    for (const productNo of selectedAssignedProducts.value) {
      if (mock.value) {
        const store = readMockStore()
        store.files = store.files.filter(item => !(item.reasonNo === reason.reasonNo && item.productNo === productNo))
        writeMockStore(store)
      } else {
        await request(`${apiRoot}/reasons/${encodeURIComponent(reason.reasonNo)}/products/${encodeURIComponent(productNo)}/file`, { method: 'DELETE' })
      }
    }
    await loadFiles()
    selectedAssignedProducts.value = []
    selectedAvailableProducts.value = []
    notice.value = mock.value ? '已移除 DEV 假資料對應。' : '產品指導書對應已移除。'
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '移除產品指導書失敗。' }
  finally { saving.value = false }
}
function closeMaintain(): void { maintainOpen.value = false }

function parseCsvLine(line: string): string[] {
  const cells: string[] = []
  let value = ''
  let quoted = false
  for (let index = 0; index < line.length; index++) {
    const char = line[index]
    if (char === '"' && quoted && line[index + 1] === '"') { value += '"'; index++ }
    else if (char === '"') quoted = !quoted
    else if (char === ',' && !quoted) { cells.push(value); value = '' }
    else value += char
  }
  cells.push(value)
  return cells
}
function normalizeImportRow(value: Record<string, unknown>): ImportItem {
  return {
    reasonNo: String(value.reasonNo ?? value.REASONNO ?? ''),
    productNo: String(value.productNo ?? value.PRODUCTNO ?? ''),
    sopFileName: String(value.sopFileName ?? value.fileName ?? value.FileName ?? value.FILENAME ?? ''),
    sopFilePath: String(value.sopFilePath ?? value.filePath ?? value.FilePath ?? value.FILEPATH ?? ''),
    result: '待處理',
    message: '',
  }
}
async function selectWorkbook(event: Event): Promise<void> {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0] ?? null
  importWorkbook.value = file
  importRows.value = []
  importError.value = ''
  importMessage.value = ''
  if (!file) return
  try {
    if (mock.value) {
      if (!file.name.toLowerCase().endsWith('.csv')) throw new Error('DEV 預覽支援 CSV；XLS/XLSX 由正式 API 解析。')
      const lines = (await file.text()).replace(/^\uFEFF/, '').split(/\r?\n/).filter(line => line.trim())
      const dataLines = lines.slice(1)
      importRows.value = dataLines.map(parseCsvLine).filter(row => row.some(cell => cell.trim())).map(row => ({
        reasonNo: (row[0] ?? '').trim(), productNo: (row[1] ?? '').trim(), sopFileName: (row[2] ?? '').trim(), sopFilePath: (row[3] ?? '').trim(), result: '待處理', message: '',
      }))
    } else {
      const body = new FormData()
      body.append('file', file)
      const result = await request<{ rows?: Record<string, unknown>[]; items?: Record<string, unknown>[] }>(`${apiRoot}/import/preview`, { method: 'POST', body })
      importRows.value = (result.rows ?? result.items ?? []).map(normalizeImportRow)
    }
    if (!importRows.value.length) throw new Error('工作簿沒有可匯入資料列。')
    if (importRows.value.some(row => !row.reasonNo || !row.productNo || !row.sopFileName)) throw new Error('每列都必須有 ReasonNo、ProductNo、FileName；FilePath 可留空。')
    importMessage.value = `已讀取 ${importRows.value.length} 列。請選取附件檔，檔名需對應 FileName 欄。`
  } catch (error) {
    importRows.value = []
    importError.value = error instanceof Error ? error.message : '讀取匯入工作簿失敗。'
  }
}
function selectAttachments(event: Event): void {
  const input = event.target as HTMLInputElement
  attachments.value = Array.from(input.files ?? [])
  importError.value = ''
  importMessage.value = attachments.value.length ? `已選取 ${attachments.value.length} 個附件。` : ''
}
function openImport(): void {
  importWorkbook.value = null
  importRows.value = []
  attachments.value = []
  importProgress.value = 0
  importError.value = ''
  importMessage.value = ''
  cancelImportRequested.value = false
  importOpen.value = true
}
function closeImport(): void {
  if (importing.value) return
  importOpen.value = false
}
function cancelImport(): void { cancelImportRequested.value = true }
async function downloadTemplate(): Promise<void> {
  try {
    let blob: Blob
    let fileName = 'ReasonPrdSOPFile.csv'
    if (mock.value) {
      blob = new Blob(['ReasonNo,ProductNo,FileName,FilePath\r\nDEV-RSN-001,DEV-PD-001,DEV-guide.pdf,\r\n'], { type: 'text/csv;charset=utf-8' })
    } else {
      const response = await fetch(apiUrl(`${apiRoot}/import/template`), { credentials: 'include' })
      if (response.status === 401) { window.location.assign(appBase + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
      if (!response.ok) throw new Error(`下載範本失敗（${response.status}）。`)
      blob = await response.blob()
      fileName = 'ReasonPrdSOPFile.xlsx'
    }
    const link = document.createElement('a')
    link.href = URL.createObjectURL(blob)
    link.download = fileName
    link.click()
    window.setTimeout(() => URL.revokeObjectURL(link.href), 1000)
  } catch (error) { importError.value = error instanceof Error ? error.message : '下載匯入範本失敗。' }
}
async function startImport(): Promise<void> {
  if (!importWorkbook.value || !importRows.value.length) { importError.value = '請先選取並載入 Excel 工作簿。'; return }
  if (!attachments.value.length) { importError.value = '請選取工作簿 FileName 欄對應的附件檔。'; attachmentInput.value?.click(); return }
  const attachmentByName = new Map(attachments.value.map(file => [file.name.toLowerCase(), file]))
  importing.value = true
  cancelImportRequested.value = false
  importProgress.value = 0
  importError.value = ''
  let successCount = 0
  try {
    for (let index = 0; index < importRows.value.length; index++) {
      if (cancelImportRequested.value) break
      const row = importRows.value[index]
      const file = attachmentByName.get(row.sopFileName.toLowerCase())
      if (!file) {
        row.result = '失敗'
        row.message = '未選取名稱相符的附件檔。'
        importProgress.value = index + 1
        continue
      }
      try {
        if (mock.value) {
          const store = readMockStore()
          if (store.files.some(item => item.reasonNo === row.reasonNo && item.productNo === row.productNo)) throw new Error('此品質原因與產品已存在對應。')
          store.files.push({ reasonNo: row.reasonNo, productNo: row.productNo, sopFileName: row.sopFileName, sopFilePath: `/dev-files/${encodeURIComponent(file.name)}` })
          writeMockStore(store)
        } else {
          const body = new FormData()
          body.append('file', file)
          body.append('reasonNo', row.reasonNo)
          body.append('productNo', row.productNo)
          body.append('sopFileName', row.sopFileName)
          await request(`${apiRoot}/import/item`, { method: 'POST', body })
        }
        row.result = '成功'
        row.message = '上傳並完成對應。'
        successCount++
      } catch (error) {
        row.result = '失敗'
        row.message = error instanceof Error ? error.message : '匯入失敗。'
      }
      importProgress.value = index + 1
    }
    importMessage.value = cancelImportRequested.value ? `已取消後續匯入；完成 ${successCount} 列。` : `匯入完成：成功 ${successCount} 列，失敗 ${importRows.value.length - successCount} 列。`
    if (mock.value && selectedReasonNo.value) await loadFiles()
  } finally { importing.value = false }
}
function closePage(): void { emit('close') }

onMounted(loadReasons)
</script>

<template>
  <main class="reason-sop-page">
    <header class="page-heading"><h1>品質原因產品指導書</h1><span v-if="mock" class="dev-badge">DEV 假資料</span></header>
    <div class="main-layout">
      <div class="grid-stack">
        <section class="grid-panel reason-panel" aria-label="核准品質原因">
          <div class="grid-title">品質原因</div>
          <div class="grid-wrap"><table class="reason-grid"><thead><tr>
            <th>Reason No</th><th>Reason Name</th><th>Reason Level</th><th>Reason Type</th><th>Reason Sub Type</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th>
          </tr></thead><tbody>
            <tr v-for="item in reasons" :key="item.reasonNo" :class="{ active: selectedReasonNo === item.reasonNo }" @click="selectReason(item)">
              <td>{{ item.reasonNo }}</td><td>{{ item.reasonName }}</td><td>{{ item.reasonLevel }}</td><td>{{ reasonTypeLabel(item.reasonType) }}</td><td>{{ item.reasonSubType }}</td><td class="description-cell">{{ item.description }}</td><td>{{ item.creator }}</td><td>{{ displayDate(item.createDate) }}</td><td>{{ item.reviser }}</td><td>{{ displayDate(item.reviseDate) }}</td>
            </tr>
            <tr v-if="!reasons.length"><td colspan="10" class="empty">{{ loading ? '載入中…' : '查無已核准品質原因' }}</td></tr>
          </tbody></table></div>
        </section>
        <section class="grid-panel file-panel" aria-label="產品指導書對應">
          <div class="grid-title">產品指導書{{ selectedReason ? ` — ${selectedReason.reasonNo}` : '' }}</div>
          <div class="grid-wrap"><table class="file-grid"><thead><tr><th>Product No</th><th>File Name</th><th>File Path</th></tr></thead><tbody>
            <tr v-for="item in currentFiles" :key="item.reasonNo + item.productNo"><td>{{ item.productNo }}</td><td>{{ item.sopFileName }}</td><td>{{ item.sopFilePath }}</td></tr>
            <tr v-if="!currentFiles.length"><td colspan="3" class="empty">{{ loadingFiles ? '載入中…' : selectedReason ? '此品質原因尚未設定產品指導書' : '請選取品質原因' }}</td></tr>
          </tbody></table></div>
        </section>
      </div>
      <aside class="main-actions"><button type="button" class="win-button" :disabled="!selectedReason" @click="openMaintain">PrdSOPFileMaintain</button><button type="button" class="win-button" @click="openImport">Import</button><button type="button" class="win-button close-button" @click="closePage">Close</button></aside>
    </div>
    <p v-if="errorMessage" class="message error" role="alert">{{ errorMessage }}</p><p v-if="notice" class="message notice" role="status">{{ notice }}</p>

    <div v-if="maintainOpen && selectedReason" class="modal-backdrop" @click.self="closeMaintain">
      <section class="dialog maintain-dialog" role="dialog" aria-modal="true" aria-label="產品指導書維護">
        <header class="dialog-heading"><h2>Reason PrdSOP File Maintain</h2><span>{{ selectedReason.reasonNo }}</span></header>
        <div class="reason-summary">
          <label><span>ReasonNo</span><input :value="selectedReason.reasonNo" readonly></label>
          <label><span>ReasonName</span><input :value="selectedReason.reasonName" readonly></label>
          <label><span>ReasonType</span><input :value="reasonTypeLabel(selectedReason.reasonType)" readonly></label>
          <label><span>Reason Level</span><input :value="selectedReason.reasonLevel" readonly></label>
        </div>
        <div class="product-search"><label for="product-search">ProductNo</label><input id="product-search" v-model="productSearch" @keyup.enter="searchProducts"><button class="win-button" type="button" @click="searchProducts">Search</button></div>
        <div class="file-picker"><label for="sop-file-path">File Path</label><input id="sop-file-path" :value="selectedFileName" readonly placeholder="請選擇 JPG、BMP、PNG 或 PDF 檔"><input ref="fileInput" type="file" accept=".jpg,.jpeg,.jpe,.jfif,.png,.bmp,.pdf" class="hidden-file" @change="chooseSopFile"><button class="win-button browse-button" type="button" @click="fileInput?.click()">...</button></div>
        <div class="dual-lists">
          <section class="list-box"><h3>UnSelected</h3><div class="list-table-wrap"><table><thead><tr><th></th><th>ProductNo</th><th>ProductName</th></tr></thead><tbody>
            <tr v-for="item in filteredAvailableProducts" :key="item.productNo" @dblclick="addOneProduct(item.productNo)"><td><input type="checkbox" :checked="selectedAvailableProducts.includes(item.productNo)" @click.stop @change="toggleAvailable(item.productNo, ($event.target as HTMLInputElement).checked)"></td><td>{{ item.productNo }}</td><td>{{ item.productName }}</td></tr>
            <tr v-if="!filteredAvailableProducts.length"><td colspan="3" class="empty">輸入 ProductNo 後按 Search</td></tr>
          </tbody></table></div></section>
          <div class="move-actions"><button class="win-button move-button" type="button" :disabled="saving || !selectedAvailableProducts.length" title="新增對應" @click="addSelectedProducts">➜</button><button class="win-button move-button" type="button" :disabled="saving || !selectedAssignedProducts.length" title="移除對應" @click="removeSelectedProducts">⬅</button></div>
          <section class="list-box"><h3>Selected</h3><div class="list-table-wrap"><table><thead><tr><th></th><th>ProductNo</th><th>FileName</th></tr></thead><tbody>
            <tr v-for="item in currentFiles" :key="item.productNo" @dblclick="removeOneProduct(item.productNo)"><td><input type="checkbox" :checked="selectedAssignedProducts.includes(item.productNo)" @click.stop @change="toggleAssigned(item.productNo, ($event.target as HTMLInputElement).checked)"></td><td>{{ item.productNo }}</td><td>{{ item.sopFileName }}</td></tr>
            <tr v-if="!currentFiles.length"><td colspan="3" class="empty">尚無產品對應</td></tr>
          </tbody></table></div></section>
        </div>
        <p v-if="errorMessage" class="message error" role="alert">{{ errorMessage }}</p><p v-if="notice" class="message notice" role="status">{{ notice }}</p>
        <footer class="dialog-actions"><span class="dialog-hint">新增與移除會立即送出；檔案由 API 安全保存。</span><button class="win-button" type="button" :disabled="saving" @click="closeMaintain">Cancel</button></footer>
      </section>
    </div>

    <div v-if="importOpen" class="modal-backdrop" @click.self="closeImport">
      <section class="dialog import-dialog" role="dialog" aria-modal="true" aria-label="產品指導書匯入">
        <header class="dialog-heading"><h2>Reason Product SOP File Import</h2><span v-if="importing">{{ importProgress }}/{{ importRows.length }}</span></header>
        <div class="workbook-picker"><label for="workbook-path">File Path</label><input id="workbook-path" :value="importWorkbook?.name ?? ''" readonly placeholder="選擇 XLS / XLSX 工作簿"><input ref="workbookInput" type="file" accept=".xls,.xlsx,.csv" class="hidden-file" @change="selectWorkbook"><button class="win-button browse-button" type="button" :disabled="importing" @click="workbookInput?.click()">...</button><button class="win-button template-button" type="button" :disabled="importing" @click="downloadTemplate">Template DownLoad</button></div>
        <div class="attachment-picker"><label for="attachment-files">附件檔</label><input id="attachment-files" :value="attachments.map(item => item.name).join('、')" readonly placeholder="選取工作簿 FileName 欄對應的檔案"><input ref="attachmentInput" type="file" multiple accept=".jpg,.jpeg,.jpe,.jfif,.png,.bmp,.pdf" class="hidden-file" @change="selectAttachments"><button class="win-button browse-button" type="button" :disabled="importing" @click="attachmentInput?.click()">...</button></div>
        <div class="import-hint">工作簿第一張工作表依序讀取 ReasonNo、ProductNo、FileName、FilePath；Web 版請另外選取附件檔，並以 FileName 配對。</div>
        <div v-if="importRows.length" class="import-grid-wrap"><table class="import-grid"><thead><tr><th>ReasonNo</th><th>ProductNo</th><th>FileName</th><th>FilePath</th><th>Result</th><th>Message</th></tr></thead><tbody>
          <tr v-for="(row, index) in importRows" :key="row.reasonNo + row.productNo + index"><td>{{ row.reasonNo }}</td><td>{{ row.productNo }}</td><td>{{ row.sopFileName }}</td><td>{{ row.sopFilePath }}</td><td>{{ row.result }}</td><td>{{ row.message }}</td></tr>
        </tbody></table></div>
        <div v-else class="import-empty">選取 Excel 後，預覽資料會顯示在此表格。</div>
        <div v-if="importing" class="progress-row"><progress :max="Math.max(importRows.length, 1)" :value="importProgress"></progress><span>{{ importProgress }} / {{ importRows.length }}</span></div>
        <p v-if="importError" class="message error" role="alert">{{ importError }}</p><p v-if="importMessage" class="message notice" role="status">{{ importMessage }}</p>
        <footer class="dialog-actions"><span class="dialog-hint">{{ userNo ? `UserNo: ${userNo}` : '' }}</span><button v-if="importing" class="win-button" type="button" @click="cancelImport">Cancel Import</button><button v-else class="win-button primary-button" type="button" :disabled="!importRows.length" @click="startImport">Import</button><button class="win-button" type="button" :disabled="importing" @click="closeImport">Close</button></footer>
      </section>
    </div>
  </main>
</template>

<style scoped>
.reason-sop-page{--line:#c8c4bb;--text:#30302e;--muted:#77746e;display:flex;flex-direction:column;gap:4px;height:100%;min-height:0;color:var(--text);background:#f7f5ef;font:14px/1.3 "Segoe UI","Microsoft JhengHei",sans-serif}.page-heading{display:flex;align-items:center;justify-content:space-between;min-height:34px;padding:0 10px}.page-heading h1{margin:0;font-size:20px;font-weight:600}.dev-badge{padding:3px 8px;border:1px solid #d7a044;background:#fff1bd;color:#77551a;font-size:12px}.main-layout{display:grid;grid-template-columns:minmax(0,1fr) 88px;gap:6px;flex:1;min-height:0;padding:0 8px}.grid-stack{display:grid;grid-template-rows:minmax(180px,265fr) minmax(160px,296fr);gap:6px;min-height:0}.grid-panel{display:flex;flex-direction:column;gap:3px;min-height:0;padding:3px;border:1px solid var(--line);background:#fff}.grid-title{height:18px;padding-left:5px;color:#555;font-size:12px}.grid-wrap{min-width:0;min-height:0;flex:1;overflow:auto;border:1px solid #bcb8af}.reason-grid,.file-grid,.list-box table,.import-grid{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}.reason-grid th,.file-grid th,.list-box th,.import-grid th{position:sticky;top:0;z-index:1;padding:5px 6px;border:1px solid #c2bfb9;background:#eeeae0;text-align:left;font-weight:600}.reason-grid td,.file-grid td,.list-box td,.import-grid td{height:25px;padding:3px 6px;border:1px solid #d1cec8;background:#fff}.reason-grid tbody tr,.list-box tbody tr{cursor:pointer}.reason-grid tbody tr:hover td,.list-box tbody tr:hover td{background:#fff7dd}.reason-grid tbody tr.active td{background:#ffedb1}.description-cell{max-width:220px;overflow:hidden;text-overflow:ellipsis}.empty{padding:14px!important;text-align:center;color:var(--muted)}.main-actions{display:flex;flex-direction:column;gap:7px;padding-top:0}.win-button{min-height:32px;padding:4px 7px;border:1px solid #bdb9b0;border-radius:3px;background:linear-gradient(#fff,#f0efec);color:#343331;font:inherit;white-space:normal;cursor:pointer}.win-button:hover:not(:disabled){border-color:#d08a23;background:#fff6df}.win-button:disabled{opacity:.5;cursor:not-allowed}.main-actions .win-button{width:100%;font-size:12px}.close-button{margin-top:auto}.message{margin:0 8px;padding:7px 10px;border:1px solid}.error{border-color:#d99b86;background:#fff0e9;color:#873820}.notice{border-color:#c9bc91;background:#fff8dc;color:#67551d}.modal-backdrop{position:fixed;inset:0;z-index:1200;display:flex;align-items:center;justify-content:center;padding:18px;background:#16161670}.dialog{display:flex;flex-direction:column;gap:8px;max-height:min(92vh,760px);padding:12px;border:1px solid #aaa69e;background:#f7f5ef;box-shadow:0 10px 35px #0004}.maintain-dialog{width:min(858px,96vw);height:min(536px,92vh)}.import-dialog{width:min(704px,96vw);height:min(530px,92vh)}.dialog-heading{display:flex;align-items:center;justify-content:space-between;min-height:28px;border-bottom:1px solid var(--line)}.dialog-heading h2{margin:0;font-size:17px;font-weight:600}.dialog-heading span{color:var(--muted);font-size:12px}.reason-summary{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:8px}.reason-summary label,.product-search,.file-picker,.workbook-picker,.attachment-picker{display:flex;align-items:center;gap:6px}.reason-summary label{flex-direction:column;align-items:stretch;gap:3px}.reason-summary label span,.product-search label,.file-picker label,.workbook-picker label,.attachment-picker label{flex:0 0 80px;text-align:right;color:#555}.reason-summary input,.product-search input,.file-picker>input:not([type=file]),.workbook-picker>input:not([type=file]),.attachment-picker>input:not([type=file]){box-sizing:border-box;min-width:0;height:27px;padding:3px 6px;border:1px solid #aaa69e;background:#fff;color:#333;font:inherit}.reason-summary input{width:100%;background:#f1f0ed}.product-search input{flex:1}.file-picker>input:not([type=file]),.workbook-picker>input:not([type=file]),.attachment-picker>input:not([type=file]){flex:1}.browse-button{flex:0 0 30px;min-width:30px}.template-button{flex:0 0 126px;white-space:nowrap}.hidden-file{display:none}.dual-lists{display:grid;grid-template-columns:minmax(0,1fr) 54px minmax(0,1fr);gap:6px;flex:1;min-height:140px}.list-box{display:flex;flex-direction:column;min-width:0;min-height:0;border:1px solid var(--line);background:#fff}.list-box h3{margin:0;padding:5px 8px;border-bottom:1px solid var(--line);font-size:13px;font-weight:600}.list-table-wrap{min-height:0;overflow:auto}.list-box table{font-size:12px}.list-box td:first-child,.list-box th:first-child{width:28px;text-align:center}.list-box input[type=checkbox]{margin:0}.move-actions{display:flex;flex-direction:column;align-items:center;justify-content:center;gap:9px}.move-button{width:42px;font-size:17px}.dialog-actions{display:flex;align-items:center;justify-content:flex-end;gap:7px;min-height:36px}.dialog-hint{flex:1;color:#68645e;font-size:11px}.primary-button{background:#f4a62a;color:#fff}.workbook-picker,.attachment-picker{min-height:32px}.import-hint{padding:5px 8px;border:1px solid #d9c99b;background:#fff8dc;color:#67551d;font-size:11px}.import-grid-wrap{min-height:0;flex:1;overflow:auto;border:1px solid #bcb8af;background:#fff}.import-grid{font-size:11px}.import-grid td{height:23px}.import-empty{display:grid;flex:1;place-items:center;border:1px solid var(--line);background:#fff;color:var(--muted)}.progress-row{display:flex;align-items:center;gap:8px}.progress-row progress{flex:1;height:18px}.progress-row span{min-width:62px;text-align:right;font-size:12px}@media(max-width:760px){.main-layout{grid-template-columns:minmax(0,1fr) 72px;padding:0 4px}.main-actions .win-button{font-size:10px}.reason-summary{grid-template-columns:repeat(2,minmax(0,1fr))}.maintain-dialog,.import-dialog{height:auto;max-height:94vh}.dual-lists{grid-template-columns:minmax(0,1fr) 42px minmax(0,1fr)}.list-box table{font-size:10px}.dialog-actions{flex-wrap:wrap}.dialog-hint{flex-basis:100%}.template-button{flex-basis:112px;font-size:11px}}
</style>
