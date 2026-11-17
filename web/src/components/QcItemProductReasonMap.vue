<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type IssueState = 0 | 1 | 2 | -1
type ReasonType = 0 | 1
interface QcItem { qcItemNo: string; qcItemName: string; lotIsValid: number; componentCollect: number; planNo: string; sampleSizeInteger: number; ruleNo: string; creator: string; createDate: string; reviser: string; reviseDate: string }
interface Reason { reasonNo: string; reasonName: string; reasonType: ReasonType; reasonLevel: number; description: string; issueState: number }
interface Product { productNo: string; productName: string; issueState: number; currentVersion: number }
interface Mapping { qcItemNo: string; productNo: string; reasonNo: string; reasonName: string; reasonType: ReasonType; reasonLevel: number; creator: string; createDate: string; reviser: string; reviseDate: string }
interface ImportRow { qcItemNo: string; productNo: string; reasonNo: string; result: string; message: string }
interface Store { items: QcItem[]; reasons: Reason[]; products: Product[]; mappings: { qcItemNo: string; productNo: string; reasonNo: string }[] }
type Payload<T> = T[] | { items?: T[]; rows?: T[]; [key: string]: unknown }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const apiRoot = 'api/qc/item-product-reasons'
const storageKey = 'ymes-dev-mock:qc-item-product-reason-map:v1'
const items = ref<QcItem[]>([])
const mappings = ref<Mapping[]>([])
const selectedItemNo = ref('')
const selectedMappingProduct = ref('*')
const itemQuery = ref('')
const productFilter = ref('')
const productFilterInput = ref('')
const itemIssueState: IssueState = 2
const loading = ref(false)
const saving = ref(false)
const error = ref('')
const notice = ref('')
const editorOpen = ref(false)
const importOpen = ref(false)
const productPickerOpen = ref(false)
const pickerTarget = ref<'filter' | 'editor'>('filter')
const editorProduct = ref('*')
const loadedEditorProduct = ref('')
const originalReasonNos = ref<string[]>([])
const selectedReasonNos = ref<string[]>([])
const availableReasonNos = ref<string[]>([])
const checkedAvailableNos = ref<string[]>([])
const checkedSelectedNos = ref<string[]>([])
const reasonType = ref<ReasonType>(0)
const reasonLevel = ref(0)
const productSearch = ref('')
const products = ref<Product[]>([])
const selectedProductNo = ref('')
const workbook = ref<File | null>(null)
const importRows = ref<ImportRow[]>([])
const importing = ref(false)
const importProgress = ref(0)
const cancelImport = ref(false)
const importError = ref('')
const importNotice = ref('')
const fileInput = ref<HTMLInputElement | null>(null)
const currentItem = computed(() => items.value.find(row => row.qcItemNo === selectedItemNo.value) ?? null)
const reasonOptions = ref<Reason[]>([])
const visibleMappings = computed(() => {
  const needle = productFilter.value.trim().toLocaleLowerCase()
  return mappings.value.filter(row => !needle || (needle === '*' ? row.productNo === '*' : row.productNo.toLocaleLowerCase().includes(needle)))
})
const mappingGroups = computed(() => {
  const groups = new Map<string, Mapping[]>()
  for (const row of visibleMappings.value) groups.set(row.productNo, [...(groups.get(row.productNo) ?? []), row])
  return [...groups.entries()].map(([productNo, rows]) => ({ productNo, rows })).sort((left, right) => left.productNo.localeCompare(right.productNo))
})
const selectedReasons = computed(() => reasonOptions.value.filter(row => selectedReasonNos.value.includes(row.reasonNo)).sort((a, b) => a.reasonNo.localeCompare(b.reasonNo)))
const availableReasons = computed(() => reasonOptions.value.filter(row => availableReasonNos.value.includes(row.reasonNo) && row.reasonType === reasonType.value && row.reasonLevel === reasonLevel.value).sort((a, b) => a.reasonNo.localeCompare(b.reasonNo)))
const allProductNos = computed(() => [...new Set(mappings.value.filter(row => row.qcItemNo === selectedItemNo.value && row.productNo !== '*').map(row => row.productNo))].sort())

function seedStore(): Store {
  return {
    items: [
      { qcItemNo: 'DEV-QCA-001', qcItemName: '外觀缺點判定（開發假資料）', lotIsValid: 1, componentCollect: 0, planNo: 'DEV-PLAN-A', sampleSizeInteger: 1, ruleNo: 'DEV-RULE-01', creator: 'DEV001', createDate: '2026-11-17 08:00:00', reviser: 'DEV001', reviseDate: '2026-11-17 08:00:00' },
      { qcItemNo: 'DEV-QCA-002', qcItemName: '包裝完整性（開發假資料）', lotIsValid: 0, componentCollect: 1, planNo: 'DEV-PLAN-B', sampleSizeInteger: 0, ruleNo: 'DEV-RULE-02', creator: 'DEV001', createDate: '2026-11-17 08:00:00', reviser: '', reviseDate: '' },
      { qcItemNo: 'DEV-QCA-003', qcItemName: '標示確認（開發假資料）', lotIsValid: 1, componentCollect: 0, planNo: '', sampleSizeInteger: 0, ruleNo: '', creator: 'DEV001', createDate: '2026-11-17 08:00:00', reviser: '', reviseDate: '' },
    ],
    reasons: [
      { reasonNo: 'DEV-RSN-001', reasonName: '刮傷（開發假資料）', reasonType: 1, reasonLevel: 0, description: 'DEV only', issueState: 2 },
      { reasonNo: 'DEV-RSN-002', reasonName: '破損（開發假資料）', reasonType: 1, reasonLevel: 0, description: 'DEV only', issueState: 2 },
      { reasonNo: 'DEV-RSN-003', reasonName: '報廢（開發假資料）', reasonType: 0, reasonLevel: 0, description: 'DEV only', issueState: 2 },
      { reasonNo: 'DEV-RSN-004', reasonName: '待核准原因（開發假資料）', reasonType: 1, reasonLevel: 0, description: 'not approved', issueState: 0 },
    ],
    products: [1, 2, 3, 4].map(n => ({ productNo: `DEV-PD-${String(n).padStart(3, '0')}`, productName: `開發用產品 ${n}`, issueState: 2, currentVersion: 1 })),
    mappings: [
      { qcItemNo: 'DEV-QCA-001', productNo: '*', reasonNo: 'DEV-RSN-003' },
      { qcItemNo: 'DEV-QCA-001', productNo: 'DEV-PD-001', reasonNo: 'DEV-RSN-001' },
      { qcItemNo: 'DEV-QCA-001', productNo: 'DEV-PD-002', reasonNo: 'DEV-RSN-002' },
    ],
  }
}
function readStore(): Store {
  try {
    const raw = localStorage.getItem(storageKey)
    if (raw) return JSON.parse(raw) as Store
  } catch { /* initialize isolated development fixtures */ }
  const store = seedStore(); localStorage.setItem(storageKey, JSON.stringify(store)); return store
}
function writeStore(store: Store): void { localStorage.setItem(storageKey, JSON.stringify(store)) }
function enc(value: string): string { return encodeURIComponent(value) }
function apiUrl(path: string): string {
  if (!props.apiBaseUrl.trim()) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(props.apiBaseUrl.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init })
  if (response.status === 401) { window.location.assign(`${import.meta.env.BASE_URL}login`); throw new Error('登入已逾期，請重新登入。') }
  const body = await response.text()
  if (!response.ok) throw new Error(body || `API 請求失敗 (${response.status})`)
  return (body ? JSON.parse(body) : undefined) as T
}
function listOf<T>(payload: Payload<T> | T[] | null | undefined): T[] {
  if (Array.isArray(payload)) return payload
  if (payload && Array.isArray(payload.items)) return payload.items
  if (payload && Array.isArray(payload.rows)) return payload.rows
  return []
}
function normalizeItem(raw: Record<string, unknown>): QcItem {
  return { qcItemNo: String(raw.qcItemNo ?? raw.QCITEMNO ?? ''), qcItemName: String(raw.qcItemName ?? raw.QCITEMNAME ?? ''), lotIsValid: Number(raw.lotIsValid ?? raw.LOTISVALID ?? 0), componentCollect: Number(raw.componentCollect ?? raw.COMPONENTCOLLECT ?? 0), planNo: String(raw.planNo ?? raw.PLANNO ?? ''), sampleSizeInteger: Number(raw.sampleSizeInteger ?? raw.ISSAMPLESIZEINTEGER ?? 0), ruleNo: String(raw.ruleNo ?? raw.RULENO ?? ''), creator: String(raw.creator ?? raw.CREATOR ?? ''), createDate: String(raw.createDate ?? raw.CREATEDATE ?? ''), reviser: String(raw.reviser ?? raw.REVISER ?? ''), reviseDate: String(raw.reviseDate ?? raw.REVISEDATE ?? '') }
}
function normalizeReason(raw: Record<string, unknown>): Reason {
  return { reasonNo: String(raw.reasonNo ?? raw.REASONNO ?? ''), reasonName: String(raw.reasonName ?? raw.REASONNAME ?? ''), reasonType: Number(raw.reasonType ?? raw.REASONTYPE ?? 0) as ReasonType, reasonLevel: Number(raw.reasonLevel ?? raw.REASONLEVEL ?? 0), description: String(raw.description ?? raw.DESCRIPTION ?? ''), issueState: Number(raw.issueState ?? raw.ISSUESTATE ?? 2) }
}
function normalizeProduct(raw: Record<string, unknown>): Product {
  return { productNo: String(raw.productNo ?? raw.PRODUCTNO ?? ''), productName: String(raw.productName ?? raw.PRODUCTNAME ?? ''), issueState: Number(raw.issueState ?? raw.ISSUESTATE ?? 2), currentVersion: Number(raw.currentVersion ?? raw.curVersion ?? raw.CURVERSION ?? 1) }
}
function normalizeMapping(raw: Record<string, unknown>): Mapping {
  return { qcItemNo: String(raw.qcItemNo ?? raw.QCITEMNO ?? ''), productNo: String(raw.productNo ?? raw.PRODUCTNO ?? ''), reasonNo: String(raw.reasonNo ?? raw.REASONNO ?? ''), reasonName: String(raw.reasonName ?? raw.REASONNAME ?? ''), reasonType: Number(raw.reasonType ?? raw.REASONTYPE ?? 0) as ReasonType, reasonLevel: Number(raw.reasonLevel ?? raw.REASONLEVEL ?? 0), creator: String(raw.creator ?? raw.CREATOR ?? ''), createDate: String(raw.createDate ?? raw.CREATEDATE ?? ''), reviser: String(raw.reviser ?? raw.REVISER ?? ''), reviseDate: String(raw.reviseDate ?? raw.REVISEDATE ?? '') }
}
async function queryItems(): Promise<void> {
  loading.value = true; error.value = ''; notice.value = ''; selectedItemNo.value = ''; mappings.value = []
  try {
    if (mock.value) {
      const store = readStore(); items.value = store.items.filter(row => row.qcItemNo.toLocaleLowerCase().includes(itemQuery.value.trim().toLocaleLowerCase()))
    } else {
      const payload = await request<Payload<Record<string, unknown>>>(`${apiRoot}/qc-items?issueState=${itemIssueState}&qcItemNo=${enc(itemQuery.value.trim())}`)
      items.value = listOf(payload).map(normalizeItem)
    }
    if (items.value.length) await selectItem(items.value[0].qcItemNo)
  } catch (reason) { items.value = []; error.value = reason instanceof Error ? reason.message : '查詢核准計數品管項目失敗。' }
  finally { loading.value = false }
}
async function selectItem(qcItemNo: string): Promise<void> {
  if (selectedItemNo.value !== qcItemNo) { productFilter.value = ''; productFilterInput.value = '' }
  selectedItemNo.value = qcItemNo; selectedMappingProduct.value = '*'; notice.value = ''; error.value = ''
  try {
    let rows: Mapping[]
    if (mock.value) {
      const store = readStore()
      rows = store.mappings.filter(row => row.qcItemNo === qcItemNo).map(row => {
        const reason = store.reasons.find(item => item.reasonNo === row.reasonNo)
        return { ...row, reasonName: reason?.reasonName ?? row.reasonNo, description: reason?.description ?? '', reasonType: reason?.reasonType ?? 0, reasonLevel: reason?.reasonLevel ?? 0, creator: 'DEV001', createDate: '2026-11-17 08:00:00', reviser: 'DEV001', reviseDate: '2026-11-17 08:00:00' }
      })
    } else {
      const payload = await request<Payload<Record<string, unknown>>>(`${apiRoot}?qcItemNo=${enc(qcItemNo)}&allData=true`)
      rows = listOf(payload).map(normalizeMapping)
    }
    mappings.value = rows
  } catch (reason) { mappings.value = []; error.value = reason instanceof Error ? reason.message : '載入產品原因對應失敗。' }
}
function reasonTypeName(value: number): string { return value === 0 ? 'Scrap' : 'Defect' }
function yn(value: number): string { return value === 1 ? 'Y' : 'N' }
function ynSampleSizeInteger(value: number): string { return value === 0 ? 'Y' : 'N' }
function dateText(value: string): string { return value ? value.replace('T', ' ').slice(0, 19) : '' }
function openProductPicker(target: 'filter' | 'editor'): void { pickerTarget.value = target; productSearch.value = ''; selectedProductNo.value = ''; productPickerOpen.value = true; searchProducts() }
async function searchProducts(): Promise<void> {
  try {
    if (pickerTarget.value === 'filter') {
      const codes = mock.value ? allProductNos.value : listOf(await request<Payload<Record<string, unknown>>>(`${apiRoot}/${enc(selectedItemNo.value)}/products`)).map(row => String(row.productNo ?? row.PRODUCTNO ?? ''))
      products.value = codes.filter(code => !productSearch.value.trim() || code.toLocaleLowerCase().includes(productSearch.value.trim().toLocaleLowerCase())).map(productNo => ({ productNo, productName: '', issueState: 2, currentVersion: 1 }))
    } else if (mock.value) products.value = readStore().products.filter(row => row.issueState === 2 && row.currentVersion === 1 && (!productSearch.value.trim() || row.productNo.toLocaleLowerCase().includes(productSearch.value.trim().toLocaleLowerCase())))
    else { const payload = await request<Payload<Record<string, unknown>>>(`${apiRoot}/products?issueState=2&currentVersion=1&search=${enc(productSearch.value.trim())}`); products.value = listOf(payload).map(normalizeProduct) }
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '查詢產品失敗。'; products.value = [] }
}
async function chooseProduct(productNo: string): Promise<void> {
  if (pickerTarget.value === 'filter') productFilterInput.value = productNo
  else {
    if (editorOpen.value && [...selectedReasonNos.value].sort().join('|') !== [...originalReasonNos.value].sort().join('|') && !window.confirm('目前原因尚未儲存。切換產品會捨棄變更，仍要繼續嗎？')) return
    editorProduct.value = productNo
  }
  productPickerOpen.value = false
  if (pickerTarget.value === 'editor' && editorOpen.value) await refreshEditorProduct(true)
}
async function applyProductFilter(): Promise<void> {
  productFilter.value = productFilterInput.value.trim()
  if (selectedItemNo.value) await selectItem(selectedItemNo.value)
}
async function openEditor(): Promise<void> {
  if (!currentItem.value) { error.value = '請先選取一筆計數品管項目。'; return }
  editorProduct.value = selectedMappingProduct.value || '*'
  originalReasonNos.value = []; selectedReasonNos.value = []; availableReasonNos.value = []
  checkedAvailableNos.value = []; checkedSelectedNos.value = []
  reasonType.value = 0; reasonLevel.value = 0; error.value = ''; notice.value = ''
  try {
    if (mock.value) reasonOptions.value = readStore().reasons.filter(row => row.issueState === 2)
    else { const payload = await request<Payload<Record<string, unknown>>>(`${apiRoot}/reasons?issueState=2`); reasonOptions.value = listOf(payload).map(normalizeReason) }
    await loadEditorProduct()
    editorOpen.value = true
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '載入原因清單失敗。' }
}
async function loadEditorProduct(): Promise<void> {
  const qcItemNo = currentItem.value?.qcItemNo
  if (!qcItemNo) return
  let assigned: Mapping[]
  if (mock.value) {
    const store = readStore()
    assigned = store.mappings.filter(row => row.qcItemNo === qcItemNo && row.productNo === editorProduct.value).map(row => {
      const reason = store.reasons.find(item => item.reasonNo === row.reasonNo)
      return { ...row, reasonName: reason?.reasonName ?? '', reasonType: reason?.reasonType ?? 0, reasonLevel: reason?.reasonLevel ?? 0, creator: 'DEV001', createDate: '2026-11-17 08:00:00', reviser: 'DEV001', reviseDate: '2026-11-17 08:00:00' }
    })
  } else {
    const payload = await request<Payload<Record<string, unknown>>>(`${apiRoot}/${enc(qcItemNo)}/${enc(editorProduct.value)}/reasons`)
    assigned = listOf(payload).map(normalizeMapping)
  }
  originalReasonNos.value = assigned.map(row => row.reasonNo); selectedReasonNos.value = [...originalReasonNos.value]
  loadedEditorProduct.value = editorProduct.value
  availableReasonNos.value = reasonOptions.value.filter(row => !selectedReasonNos.value.includes(row.reasonNo)).map(row => row.reasonNo)
  checkedAvailableNos.value = []; checkedSelectedNos.value = []
}
async function refreshEditorProduct(skipConfirm = false): Promise<void> {
  const changed = [...selectedReasonNos.value].sort().join('|') !== [...originalReasonNos.value].sort().join('|')
  if (!skipConfirm && changed && !window.confirm('目前原因尚未儲存。重新載入會捨棄變更，仍要繼續嗎？')) return
  try { await loadEditorProduct(); error.value = '' }
  catch (reason) { error.value = reason instanceof Error ? reason.message : '載入產品原因對應失敗。' }
}
function moveReasons(direction: 'add' | 'remove'): void {
  if (direction === 'add') {
    const chosen = availableReasons.value.filter(row => checkedAvailableNos.value.includes(row.reasonNo)).map(row => row.reasonNo)
    selectedReasonNos.value = [...new Set([...selectedReasonNos.value, ...chosen])]
    availableReasonNos.value = availableReasonNos.value.filter(code => !chosen.includes(code))
    checkedAvailableNos.value = []; checkedSelectedNos.value = []
  } else {
    const chosen = selectedReasons.value.filter(row => checkedSelectedNos.value.includes(row.reasonNo)).map(row => row.reasonNo)
    selectedReasonNos.value = selectedReasonNos.value.filter(code => !chosen.includes(code)); availableReasonNos.value = [...new Set([...availableReasonNos.value, ...chosen])]
    checkedAvailableNos.value = []; checkedSelectedNos.value = []
  }
}
function moveAvailableOne(reasonNo: string): void {
  if (!selectedReasonNos.value.includes(reasonNo)) selectedReasonNos.value = [...selectedReasonNos.value, reasonNo]
  availableReasonNos.value = availableReasonNos.value.filter(code => code !== reasonNo)
  checkedAvailableNos.value = checkedAvailableNos.value.filter(code => code !== reasonNo)
}
function moveSelectedOne(reasonNo: string): void {
  selectedReasonNos.value = selectedReasonNos.value.filter(code => code !== reasonNo)
  if (!availableReasonNos.value.includes(reasonNo)) availableReasonNos.value = [...availableReasonNos.value, reasonNo]
  checkedSelectedNos.value = checkedSelectedNos.value.filter(code => code !== reasonNo)
}
function closeEditor(): void {
  const changed = [...selectedReasonNos.value].sort().join('|') !== [...originalReasonNos.value].sort().join('|')
  if (changed && !window.confirm('對應內容尚未儲存。要放棄變更並關閉嗎？')) return
  editorOpen.value = false
}
async function saveMapping(): Promise<void> {
  const qcItemNo = currentItem.value?.qcItemNo
  if (!qcItemNo || saving.value) return
  if (loadedEditorProduct.value !== editorProduct.value) { error.value = 'ProductNo 已變更，請按 Enter 載入該產品的對應後再儲存。'; return }
  saving.value = true; error.value = ''; notice.value = ''
  try {
    const body = JSON.stringify({ reasonNos: selectedReasonNos.value })
    if (mock.value) {
      const store = readStore()
      if (editorProduct.value !== '*' && !store.products.some(row => row.productNo.toLocaleUpperCase() === editorProduct.value.toLocaleUpperCase() && row.issueState === 2 && row.currentVersion === 1)) throw new Error('ProductNo 不存在或不是核准中的目前版本。')
      store.mappings = store.mappings.filter(row => !(row.qcItemNo === qcItemNo && row.productNo === editorProduct.value))
      store.mappings.push(...selectedReasonNos.value.map(reasonNo => ({ qcItemNo, productNo: editorProduct.value, reasonNo }))); writeStore(store)
    } else await request(`${apiRoot}/${enc(qcItemNo)}/${enc(editorProduct.value)}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body })
    editorOpen.value = false; await selectItem(qcItemNo); notice.value = `已儲存 ${qcItemNo} / ${editorProduct.value} 的原因對應。`
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '儲存原因對應失敗。' }
  finally { saving.value = false }
}
function openImport(): void { workbook.value = null; importRows.value = []; importError.value = ''; importNotice.value = ''; importProgress.value = 0; importOpen.value = true }
function setWorkbook(event: Event): void { workbook.value = (event.target as HTMLInputElement).files?.[0] ?? null; importRows.value = []; importError.value = ''; importNotice.value = '' }
async function previewImport(): Promise<void> {
  if (!workbook.value) { importError.value = '請先選取 Excel 工作簿。'; return }
  importError.value = ''; importNotice.value = ''
  try {
    if (mock.value) {
      if (!/\.csv$/i.test(workbook.value.name)) throw new Error('DEV 預覽接受 UTF-8 CSV；正式 API 需解析 WinForms 支援的 .xlsx/.xls。')
      const text = await workbook.value.text(); const lines = text.replace(/^\uFEFF/, '').split(/\r?\n/).filter(Boolean); const headers = (lines.shift() ?? '').split(',').map(value => value.trim().toUpperCase())
      const indexes = ['QCITEMNO', 'PRODUCTNO', 'REASONNO'].map(name => headers.indexOf(name))
      if (indexes.some(index => index < 0)) throw new Error('第一列必須包含 QCItemNo、ProductNo、ReasonNo 欄名。')
      importRows.value = lines.map(line => { const cells = line.split(',').map(value => value.trim()); return { qcItemNo: cells[indexes[0]] ?? '', productNo: cells[indexes[1]] ?? '', reasonNo: cells[indexes[2]] ?? '', result: '待匯入', message: '' } }).filter(row => row.qcItemNo || row.productNo || row.reasonNo)
    } else {
      const form = new FormData(); form.append('file', workbook.value)
      const payload = await request<Payload<Record<string, unknown>>>(`${apiRoot}/import/preview`, { method: 'POST', body: form })
      importRows.value = listOf(payload).map(row => ({ qcItemNo: String(row.qcItemNo ?? row.QCITEMNO ?? ''), productNo: String(row.productNo ?? row.PRODUCTNO ?? ''), reasonNo: String(row.reasonNo ?? row.REASONNO ?? ''), result: String(row.result ?? '待匯入'), message: String(row.message ?? '') }))
    }
    if (!importRows.value.length) throw new Error('工作簿沒有可匯入資料。')
    const counts = new Map<string, number>()
    for (const row of importRows.value) { const key = `${row.qcItemNo.toLocaleUpperCase()}|${row.productNo.toLocaleUpperCase()}|${row.reasonNo.toLocaleUpperCase()}`; counts.set(key, (counts.get(key) ?? 0) + 1) }
    for (const row of importRows.value) {
      const key = `${row.qcItemNo.toLocaleUpperCase()}|${row.productNo.toLocaleUpperCase()}|${row.reasonNo.toLocaleUpperCase()}`
      if ((counts.get(key) ?? 0) > 1 && row.result === '待匯入') { row.result = '失敗'; row.message = '匯入檔內有重複的 QCItemNo/ProductNo/ReasonNo 組合。' }
    }
    importNotice.value = `已讀取 ${importRows.value.length} 筆資料；每組 QCItemNo/ProductNo/ReasonNo 會獨立檢核與回報。`
  } catch (reason) { importError.value = reason instanceof Error ? reason.message : '讀取匯入檔失敗。' }
}
async function downloadTemplate(): Promise<void> {
  try {
    if (mock.value) {
      const blob = new Blob(['QCItemNo,ProductNo,ReasonNo\nDEV-QCA-001,DEV-PD-001,DEV-RSN-001\n'], { type: 'text/csv;charset=utf-8' }); const link = document.createElement('a'); link.href = URL.createObjectURL(blob); link.download = 'QCItemPRDReason.csv'; link.click(); URL.revokeObjectURL(link.href)
    } else {
      const response = await fetch(apiUrl(`${apiRoot}/template`), { credentials: 'include' }); if (!response.ok) throw new Error(`範本下載失敗 (${response.status})`)
      const blob = await response.blob(); const link = document.createElement('a'); link.href = URL.createObjectURL(blob); link.download = 'QCItemPRDReason.xlsx'; link.click(); URL.revokeObjectURL(link.href)
    }
  } catch (reason) { importError.value = reason instanceof Error ? reason.message : '範本下載失敗。' }
}
async function startImport(): Promise<void> {
  if (!importRows.value.length || importing.value) return
  importing.value = true; cancelImport.value = false; importProgress.value = 0; importError.value = ''; importNotice.value = ''
  try {
    for (let index = 0; index < importRows.value.length; index++) {
      if (cancelImport.value) break
      const row = importRows.value[index]
      if (row.result !== '待匯入') { importProgress.value = index + 1; continue }
      try {
        if (mock.value) {
          const store = readStore(); const item = store.items.find(value => value.qcItemNo.toLocaleUpperCase() === row.qcItemNo.toLocaleUpperCase()); const reason = store.reasons.find(value => value.reasonNo.toLocaleUpperCase() === row.reasonNo.toLocaleUpperCase()); const product = row.productNo === '*' ? null : store.products.find(value => value.productNo.toLocaleUpperCase() === row.productNo.toLocaleUpperCase() && value.issueState === 2 && value.currentVersion === 1)
          if (!item) throw new Error('QCItemNo 不存在或未核准。'); if (!reason || reason.issueState !== 2) throw new Error('ReasonNo 不存在或未核准。'); if (row.productNo !== '*' && !product) throw new Error('ProductNo 不存在或不是核准中的版本。')
          if (store.mappings.some(value => value.qcItemNo.toLocaleUpperCase() === row.qcItemNo.toLocaleUpperCase() && value.productNo.toLocaleUpperCase() === row.productNo.toLocaleUpperCase() && value.reasonNo.toLocaleUpperCase() === row.reasonNo.toLocaleUpperCase())) throw new Error('對應資料已存在。')
          store.mappings.push({ qcItemNo: item.qcItemNo, productNo: row.productNo, reasonNo: reason.reasonNo }); writeStore(store)
        } else await request(`${apiRoot}/import`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ qcItemNo: row.qcItemNo, productNo: row.productNo, reasonNo: row.reasonNo }) })
        row.result = '成功'; row.message = '已新增對應。'
      } catch (reason) { row.result = '失敗'; row.message = reason instanceof Error ? reason.message : '匯入失敗。' }
      importProgress.value = index + 1
    }
    importNotice.value = cancelImport.value ? `已取消；完成 ${importProgress.value} / ${importRows.value.length} 筆。` : `匯入完成：成功 ${importRows.value.filter(row => row.result === '成功').length} 筆，失敗 ${importRows.value.filter(row => row.result === '失敗').length} 筆。`
    if (selectedItemNo.value) await selectItem(selectedItemNo.value)
  } finally { importing.value = false }
}
function closeImport(): void { if (importing.value) cancelImport.value = true; else importOpen.value = false }
onMounted(queryItems)
</script>

<template>
  <main class="item-reason-page">
    <header class="page-heading"><h1>計數品管項目產品原因對應設定</h1><span v-if="mock" class="dev-badge">DEV 假資料</span></header>
    <section class="search-strip">
      <label for="qc-item-query">QCItemNo</label>
      <input id="qc-item-query" v-model="itemQuery" @keydown.enter="queryItems" />
      <button class="win-button" type="button" :disabled="loading" @click="queryItems">Query</button>
    </section>
    <section class="content-layout">
      <div class="grids">
        <div class="grid-panel item-panel">
          <div class="grid-title">已核准計數品管項目</div>
          <div class="grid-wrap">
            <table class="data-grid">
              <thead><tr><th>QCItemNo</th><th>QCItemName</th><th>LotIsValid</th><th>ComponentCollect</th><th>PlanNo</th><th>IsSampleSizeInteger</th><th>RuleNo</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
              <tbody>
                <tr v-for="row in items" :key="row.qcItemNo" :class="{ active: selectedItemNo === row.qcItemNo }" @click="selectItem(row.qcItemNo)" @dblclick="openEditor">
                  <td>{{ row.qcItemNo }}</td><td>{{ row.qcItemName }}</td><td>{{ yn(row.lotIsValid) }}</td><td>{{ yn(row.componentCollect) }}</td><td>{{ row.planNo }}</td><td>{{ ynSampleSizeInteger(row.sampleSizeInteger) }}</td><td>{{ row.ruleNo }}</td><td>{{ row.creator }}</td><td>{{ dateText(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ dateText(row.reviseDate) }}</td>
                </tr>
                <tr v-if="!items.length"><td colspan="11" class="empty">{{ loading ? '資料載入中…' : '查無核准的計數品管項目' }}</td></tr>
              </tbody>
            </table>
          </div>
        </div>
        <div class="product-label"><label for="product-filter">ProductNo</label><input id="product-filter" v-model="productFilterInput" @keydown.enter="applyProductFilter" /><button class="win-button lookup-button" type="button" title="從已有對應的產品選取" :disabled="!selectedItemNo" @click="openProductPicker('filter')">...</button><small>空白：全部；*：只看共用原因。QCItemNo：{{ selectedItemNo || '—' }}</small></div>
        <div class="grid-panel mapping-panel">
          <div class="grid-title">產品原因對應（依 ProductNo 分組；選取群組後按 ReasonMap 維護）</div>
          <div class="grid-wrap">
            <table class="data-grid">
              <thead><tr><th>ReasonNo</th><th>ReasonName</th><th>ReasonType</th><th>ReasonLevel</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
              <tbody>
                <template v-for="group in mappingGroups" :key="group.productNo">
                  <tr class="product-group" :class="{ selected: selectedMappingProduct === group.productNo }" @click="selectedMappingProduct = group.productNo"><td colspan="8">ProductNo：{{ group.productNo }} <span>（{{ group.rows.length }}）</span></td></tr>
                  <tr v-for="row in group.rows" :key="`${row.qcItemNo}|${group.productNo}|${row.reasonNo}`" :class="{ selected: selectedMappingProduct === group.productNo }" @click="selectedMappingProduct = group.productNo" @dblclick="selectedMappingProduct = group.productNo; openEditor()">
                    <td>{{ row.reasonNo }}</td><td>{{ row.reasonName }}</td><td>{{ reasonTypeName(row.reasonType) }}</td><td>{{ row.reasonLevel }}</td><td>{{ row.creator }}</td><td>{{ dateText(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ dateText(row.reviseDate) }}</td>
                  </tr>
                </template>
                <tr v-if="!visibleMappings.length"><td colspan="8" class="empty">{{ selectedItemNo ? '此品管項目沒有符合篩選條件的對應' : '請先選取品管項目' }}</td></tr>
              </tbody>
            </table>
          </div>
        </div>
        <footer class="record-count">資料總筆數 {{ visibleMappings.length }}</footer>
      </div>
      <aside class="action-rail">
        <button class="win-button" type="button" @click="openImport">Import</button>
        <button class="win-button" type="button" :disabled="!currentItem" @click="openEditor">ReasonMap</button>
        <button class="win-button close-button" type="button" @click="emit('close')">Close</button>
      </aside>
    </section>
    <p v-if="error" class="message error" role="alert">{{ error }}</p><p v-if="notice" class="message notice" role="status">{{ notice }}</p>

    <div v-if="editorOpen" class="modal-backdrop" @click.self="closeEditor">
      <section class="dialog editor-dialog" role="dialog" aria-modal="true" aria-label="QC Reason Map">
        <header class="dialog-heading"><h2>QC Reason Map</h2><span>{{ currentItem?.qcItemNo }}　{{ currentItem?.qcItemName }}</span></header>
        <div class="editor-fields">
          <label>QCItemNo<input :value="currentItem?.qcItemNo" readonly /></label><label>QCItemName<input :value="currentItem?.qcItemName" readonly /></label>
          <label>ProductNo<div class="inline-field"><input v-model="editorProduct" @keydown.enter.prevent="refreshEditorProduct()" /><button class="win-button lookup-button" type="button" @click="openProductPicker('editor')">...</button></div></label>
          <label>ReasonType<select v-model.number="reasonType"><option :value="0">Scrap</option><option :value="1">Defect</option></select></label>
          <label>ReasonLevel<input v-model.number="reasonLevel" type="number" min="0" max="9" /></label>
          <span class="global-hint">ProductNo = * 代表所有產品共用原因</span>
        </div>
        <div class="dual-lists">
          <section class="list-box"><h3>UnSelected</h3><div class="list-table-wrap"><table><thead><tr><th>選取</th><th>ReasonNo</th><th>ReasonName</th><th>Description</th></tr></thead><tbody>
            <tr v-for="row in availableReasons" :key="row.reasonNo" @dblclick="moveAvailableOne(row.reasonNo)"><td><input type="checkbox" :value="row.reasonNo" v-model="checkedAvailableNos" /></td><td>{{ row.reasonNo }}</td><td>{{ row.reasonName }}</td><td>{{ row.description }}</td></tr>
            <tr v-if="!availableReasons.length"><td colspan="4" class="empty">此類別與層級沒有可加入的核准原因</td></tr>
          </tbody></table></div></section>
          <div class="move-actions"><button class="win-button move-button" type="button" title="加入右側 Selected" @click="moveReasons('add')">➜</button><button class="win-button move-button" type="button" title="移回左側 UnSelected" @click="moveReasons('remove')">⬅</button></div>
          <section class="list-box"><h3>Selected</h3><div class="list-table-wrap"><table><thead><tr><th>選取</th><th>ReasonNo</th><th>ReasonName</th><th>ReasonType</th><th>ReasonLevel</th></tr></thead><tbody>
            <tr v-for="row in selectedReasons" :key="row.reasonNo" @dblclick="moveSelectedOne(row.reasonNo)"><td><input type="checkbox" :value="row.reasonNo" v-model="checkedSelectedNos" /></td><td>{{ row.reasonNo }}</td><td>{{ row.reasonName }}</td><td>{{ reasonTypeName(row.reasonType) }}</td><td>{{ row.reasonLevel }}</td></tr>
            <tr v-if="!selectedReasons.length"><td colspan="5" class="empty">尚未選取原因</td></tr>
          </tbody></table></div></section>
        </div>
        <p class="dialog-note">Confirm 會完整取代此 QCItemNo / ProductNo 的既有對應；空清單會清除該組對應。</p>
        <footer class="dialog-actions"><span class="dialog-hint">{{ selectedReasonNos.length }} 個原因</span><button class="win-button primary-button" type="button" :disabled="saving" @click="saveMapping">Confirm</button><button class="win-button" type="button" :disabled="saving" @click="closeEditor">Cancel</button></footer>
      </section>
    </div>

    <div v-if="productPickerOpen" class="modal-backdrop" @click.self="productPickerOpen = false">
      <section class="dialog picker-dialog" role="dialog" aria-modal="true" aria-label="選取產品">
        <header class="dialog-heading"><h2>選取核准中產品</h2><button class="win-button" type="button" @click="chooseProduct('*')">共用 *</button></header>
        <div class="picker-search"><label>ProductNo<input v-model="productSearch" @keydown.enter="searchProducts" /></label><button class="win-button" type="button" @click="searchProducts">Search</button></div>
        <div class="list-table-wrap picker-grid"><table><thead><tr><th></th><th>ProductNo</th><th>ProductName</th><th>IssueState</th><th>CurVersion</th></tr></thead><tbody>
          <tr v-for="row in products" :key="row.productNo" @dblclick="chooseProduct(row.productNo)"><td><input type="radio" name="product-picker" :value="row.productNo" v-model="selectedProductNo" /></td><td>{{ row.productNo }}</td><td>{{ row.productName }}</td><td>{{ row.issueState }}</td><td>{{ row.currentVersion }}</td></tr>
          <tr v-if="!products.length"><td colspan="5" class="empty">沒有核准中的產品</td></tr>
        </tbody></table></div>
        <footer class="dialog-actions"><button class="win-button primary-button" type="button" :disabled="!selectedProductNo" @click="chooseProduct(selectedProductNo)">Select</button><button class="win-button" type="button" @click="productPickerOpen = false">Close</button></footer>
      </section>
    </div>

    <div v-if="importOpen" class="modal-backdrop" @click.self="closeImport">
      <section class="dialog import-dialog" role="dialog" aria-modal="true" aria-label="Import QC Item Product Reason Map">
        <header class="dialog-heading"><h2>Import QCItem Product Reason Map</h2><span>每筆獨立檢核，允許部分成功</span></header>
        <div class="workbook-picker"><label for="reason-map-file">Excel File</label><input id="reason-map-file" :value="workbook?.name ?? ''" readonly placeholder="選取含 QCItemNo、ProductNo、ReasonNo 的工作簿"><input ref="fileInput" class="hidden-file" type="file" accept=".xlsx,.xls,.csv" @change="setWorkbook"><button class="win-button" type="button" :disabled="importing" @click="fileInput?.click()">Browse...</button><button class="win-button" type="button" :disabled="importing" @click="downloadTemplate">Template DownLoad</button><button class="win-button" type="button" :disabled="importing || !workbook" @click="previewImport">Preview</button></div>
        <p class="import-hint">欄位：QCItemNo、ProductNo、ReasonNo。ProductNo = * 為共用原因。正式環境支援 .xlsx/.xls；DEV 假資料預覽支援 UTF-8 CSV。</p>
        <div class="import-grid-wrap"><table class="import-grid"><thead><tr><th>QCItemNo</th><th>ProductNo</th><th>ReasonNo</th><th>Result</th><th>Message</th></tr></thead><tbody>
          <tr v-for="(row, index) in importRows" :key="`${row.qcItemNo}|${row.productNo}|${row.reasonNo}|${index}`"><td>{{ row.qcItemNo }}</td><td>{{ row.productNo }}</td><td>{{ row.reasonNo }}</td><td>{{ row.result }}</td><td>{{ row.message }}</td></tr>
          <tr v-if="!importRows.length"><td colspan="5" class="empty">選取檔案並按 Preview 顯示匯入資料</td></tr>
        </tbody></table></div>
        <div v-if="importing" class="progress-row"><progress :max="Math.max(importRows.length, 1)" :value="importProgress"></progress><span>{{ importProgress }} / {{ importRows.length }}</span></div>
        <p v-if="importError" class="message error">{{ importError }}</p><p v-if="importNotice" class="message notice">{{ importNotice }}</p>
        <footer class="dialog-actions"><span class="dialog-hint">{{ props.currentUserNo ? `UserNo: ${props.currentUserNo}` : '' }}</span><button v-if="importing" class="win-button" type="button" @click="cancelImport = true">Cancel Import</button><button v-else class="win-button primary-button" type="button" :disabled="!importRows.length" @click="startImport">Import</button><button class="win-button" type="button" :disabled="importing" @click="closeImport">Close</button></footer>
      </section>
    </div>
  </main>
</template>

<style scoped>
.item-reason-page{--line:#c8c4bb;display:flex;flex-direction:column;gap:5px;height:100%;min-height:0;background:#f7f5ef;color:#30302e;font:13px/1.3 "Segoe UI","Microsoft JhengHei",sans-serif}.page-heading{display:flex;align-items:center;justify-content:space-between;min-height:34px;padding:0 10px}.page-heading h1{margin:0;font-size:20px;font-weight:600}.dev-badge{padding:3px 8px;border:1px solid #d7a044;background:#fff1bd;color:#77551a;font-size:12px}.search-strip{display:flex;align-items:center;gap:6px;min-height:38px;padding:4px 8px;border:1px solid var(--line);background:#fff}.search-strip label{white-space:nowrap}.search-strip input,.editor-fields input,.editor-fields select,.picker-search input{box-sizing:border-box;height:26px;padding:3px 6px;border:1px solid #aaa69e;background:#fff7d7;color:#333;font:inherit}.search-strip input{width:145px}.search-strip #product-filter{width:150px}.lookup-button{min-width:30px;padding:2px 5px}.search-hint{margin-left:auto;color:#6c6860;font-size:11px;white-space:nowrap}.content-layout{display:grid;grid-template-columns:minmax(0,1fr) 88px;gap:6px;flex:1;min-height:0;padding:0 8px}.grids{display:grid;grid-template-rows:minmax(170px,250fr) 28px minmax(180px,275fr) 20px;gap:4px;min-height:0}.grid-panel{display:flex;flex-direction:column;gap:3px;min-height:0;padding:3px;border:1px solid var(--line);background:#fff}.grid-title{height:18px;padding-left:5px;color:#555;font-size:12px}.grid-wrap{min-width:0;min-height:0;flex:1;overflow:auto;border:1px solid #bcb8af}.data-grid,.import-grid,.list-box table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}.data-grid th,.import-grid th,.list-box th{position:sticky;top:0;z-index:1;padding:5px 6px;border:1px solid #c2bfb9;background:#eeeae0;text-align:left;font-weight:600}.data-grid td,.import-grid td,.list-box td{height:24px;padding:3px 6px;border:1px solid #d1cec8;background:#fff}.item-panel tbody tr,.mapping-panel tbody tr{cursor:pointer}.item-panel tbody tr:hover td,.mapping-panel tbody tr:hover td{background:#fff7dd}.data-grid tbody tr.active td{background:#ffedb1}.empty{text-align:center!important;color:#77746e!important;padding:12px!important}.product-label{display:flex;align-items:center;gap:12px;padding:3px 7px;border:1px solid var(--line);background:#f2f0eb}.product-label span{font-weight:600}.product-label b{min-width:100px;font-weight:400}.product-label small{margin-left:auto;color:#77746e}.check-cell{text-align:center}.record-count{padding-left:4px;color:#77746e;font-size:11px}.action-rail{display:flex;flex-direction:column;gap:7px}.win-button{min-height:30px;padding:4px 8px;border:1px solid #bdb9b0;border-radius:3px;background:linear-gradient(#fff,#f0efec);color:#343331;font:inherit;cursor:pointer;white-space:nowrap}.win-button:hover:not(:disabled){border-color:#d08a23;background:#fff6df}.win-button:disabled{opacity:.5;cursor:not-allowed}.action-rail .win-button{width:100%;font-size:12px}.close-button{margin-top:auto}.message{margin:0 8px;padding:6px 10px;border:1px solid}.error{border-color:#d99b86;background:#fff0e9;color:#873820}.notice{border-color:#c9bc91;background:#fff8dc;color:#67551d}.modal-backdrop{position:fixed;inset:0;z-index:1200;display:flex;align-items:center;justify-content:center;padding:16px;background:#16161670}.dialog{display:flex;flex-direction:column;gap:8px;max-height:94vh;padding:12px;border:1px solid #aaa69e;background:#f7f5ef;box-shadow:0 10px 35px #0004}.editor-dialog{width:min(870px,97vw);height:min(538px,94vh)}.import-dialog{width:min(850px,97vw);height:min(560px,94vh)}.picker-dialog{width:min(660px,96vw);height:min(520px,90vh)}.dialog-heading{display:flex;align-items:center;justify-content:space-between;gap:8px;min-height:28px;border-bottom:1px solid var(--line)}.dialog-heading h2{margin:0;font-size:17px;font-weight:600}.dialog-heading span{color:#77746e;font-size:12px}.editor-fields{display:grid;grid-template-columns:1fr 1.5fr 1.1fr 1fr;gap:7px}.editor-fields label{display:flex;flex-direction:column;gap:3px;color:#555;font-size:11px}.editor-fields input,.editor-fields select{width:100%;height:27px}.inline-field{display:flex;gap:4px}.inline-field input{flex:1;min-width:0}.global-hint{grid-column:span 2;align-self:end;padding:6px;color:#67551d;font-size:11px}.dual-lists{display:grid;grid-template-columns:minmax(0,1fr) 48px minmax(0,1fr);gap:6px;flex:1;min-height:160px}.list-box{display:flex;flex-direction:column;min-width:0;min-height:0;border:1px solid var(--line);background:#fff}.list-box h3{margin:0;padding:5px 8px;border-bottom:1px solid var(--line);font-size:13px;font-weight:600}.list-table-wrap{min-height:0;overflow:auto}.list-box table{font-size:11px}.list-box td:first-child,.list-box th:first-child{width:34px;text-align:center}.list-box input{margin:0}.move-actions{display:flex;flex-direction:column;align-items:center;justify-content:center;gap:10px}.move-button{width:42px;font-size:16px}.dialog-note{margin:0;padding:5px 8px;border:1px solid #d9c99b;background:#fff8dc;color:#67551d;font-size:11px}.dialog-actions{display:flex;align-items:center;justify-content:flex-end;gap:7px;min-height:34px}.dialog-hint{flex:1;color:#68645e;font-size:11px}.primary-button{background:#f4a62a;color:#fff}.picker-search{display:flex;align-items:end;gap:8px}.picker-search label{display:flex;flex:1;align-items:center;gap:8px}.picker-search input{flex:1}.picker-grid{flex:1;border:1px solid var(--line);background:#fff}.picker-grid table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}.picker-grid td,.picker-grid th{padding:5px;border:1px solid #d1cec8;text-align:left}.picker-grid tbody tr{cursor:pointer}.picker-grid tbody tr:hover td{background:#fff7dd}.workbook-picker{display:flex;align-items:center;gap:5px}.workbook-picker>label{flex:0 0 85px;text-align:right}.workbook-picker>input:not([type=file]){flex:1;min-width:0;height:28px;padding:3px 6px;border:1px solid #aaa69e;background:#fff;color:#333;font:inherit}.hidden-file{display:none}.import-hint{margin:0;padding:6px 8px;border:1px solid #d9c99b;background:#fff8dc;color:#67551d;font-size:11px}.import-grid-wrap{flex:1;min-height:100px;overflow:auto;border:1px solid #bcb8af;background:#fff}.import-grid td{height:23px}.progress-row{display:flex;align-items:center;gap:8px}.progress-row progress{flex:1;height:18px}.progress-row span{min-width:62px;text-align:right;font-size:12px}@media(max-width:900px){.search-strip{flex-wrap:wrap}.search-hint{width:100%;margin-left:0}.data-grid,.import-grid{font-size:10px}.content-layout{grid-template-columns:minmax(0,1fr) 76px;padding:0 3px}.editor-fields{grid-template-columns:repeat(2,minmax(0,1fr))}.global-hint{grid-column:span 2}.dual-lists{grid-template-columns:minmax(0,1fr) 38px minmax(0,1fr)}}
.product-label{gap:8px}.product-label label{font-weight:600}.product-label input{box-sizing:border-box;width:218px;height:26px;padding:3px 6px;border:1px solid #aaa69e;background:#fff7d7;color:#333;font:inherit}.product-label small{margin-left:6px}.product-group td{background:#eeeae0;font-weight:600}.product-group.selected td,.mapping-panel tr.selected td{background:#ffedb1}.product-panel tr{cursor:pointer}
</style>
