<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'

type MasterKey = 'operation-master' | 'material-master' | 'process-master' | 'product-master'
type Value = string | number | boolean | null
type Row = Record<string, Value>
type Field = { key: string; label: string; type?: 'text' | 'number' | 'checkbox' | 'textarea'; required?: boolean; step?: string; wide?: boolean; readonly?: boolean }

const props = defineProps<{ masterKey: MasterKey; apiBaseUrl: string }>()
const window = globalThis.window
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const configs: Record<MasterKey, { title: string; plural: string; endpoint: string; keyFields: string[]; columns: Field[]; sections: { title: string; fields: Field[] }[] }> = {
  'operation-master': {
    title: '作業站主檔', plural: '作業站', endpoint: 'operations', keyFields: ['operationNo'],
    columns: [{ key: 'operationNo', label: '作業編號' }, { key: 'operationName', label: '作業名稱' }, { key: 'shortName', label: '簡稱' }, { key: 'operationType', label: '作業類別' }, { key: 'operationClass', label: '作業分類' }, { key: 'productionSegmentNo', label: '製程段' }, { key: 'operationOrder', label: '工序順序' }, { key: 'countOperationUnitQty', label: '單位批量' }, { key: 'standardUnitRunTime', label: '標準工時' }, { key: 'standardQueueTime', label: '標準等待時間' }, { key: 'materialOption', label: '物料選項' }, { key: 'multiOperatorMode', label: '多人作業' }, { key: 'printOutOnRunCard', label: 'Run Card' }, { key: 'spcInspAbnormalHoldLot', label: 'SPC Hold' }, { key: 'errorQtyToSpc', label: 'SPC 數量門檻' }],
    sections: [
      { title: '作業識別', fields: [{ key: 'operationNo', label: '作業編號', required: true }, { key: 'operationType', label: '作業類別', required: true }, { key: 'operationName', label: '作業名稱', required: true, wide: true }, { key: 'operationClass', label: '作業分類', type: 'number' }, { key: 'shortName', label: '簡稱' }, { key: 'productionSegmentNo', label: '製程段', required: true }, { key: 'operationOrder', label: '工序順序', type: 'number' }, { key: 'description', label: '說明', type: 'textarea', wide: true }] },
      { title: '製程與品質設定', fields: [{ key: 'standardUnitRunTime', label: '標準單位工時', type: 'number', step: '0.01' }, { key: 'standardQueueTime', label: '標準等待時間', type: 'number', step: '0.01' }, { key: 'countOperationUnitQty', label: '作業單位批量', type: 'number' }, { key: 'materialOption', label: '物料選項', type: 'number' }, { key: 'multiOperatorMode', label: '多人作業模式', type: 'number' }, { key: 'errorQtyToSpc', label: 'SPC 數量門檻', type: 'number', step: '0.001' }, { key: 'printOutOnRunCard', label: '列印於 Run Card', type: 'checkbox' }, { key: 'spcInspAbnormalHoldLot', label: 'SPC 異常時 Hold Lot', type: 'checkbox' }] },
    ],
  },
  'material-master': {
    title: '物料主檔', plural: '物料', endpoint: 'materials', keyFields: ['materialNo'],
    columns: [{ key: 'materialNo', label: '物料編號' }, { key: 'materialName', label: '物料名稱' }, { key: 'materialSpec', label: '物料規格' }, { key: 'materialType', label: '物料類別' }, { key: 'putInPlace', label: '投料點' }, { key: 'unitNo', label: '單位編號' }, { key: 'safeQty', label: '安全存量' }, { key: 'keyMaterials', label: '關鍵物料' }, { key: 'expireDateFlag', label: '保存期限管理' }],
    sections: [{ title: '物料主檔資料', fields: [{ key: 'materialNo', label: '物料編號', required: true }, { key: 'materialName', label: '物料名稱', required: true }, { key: 'materialSpec', label: '物料規格' }, { key: 'materialType', label: '物料類別', required: true }, { key: 'putInPlace', label: '投料點', required: true }, { key: 'countWay', label: '計量方法', type: 'number' }, { key: 'checkLotNo', label: '是否檢查批號', type: 'number' }, { key: 'unitType', label: '單位類別' }, { key: 'unitNo', label: '單位編號', required: true }, { key: 'safeQty', label: '安全存量', type: 'number', step: '0.001' }, { key: 'expireDateFlag', label: '保存期限管理', type: 'checkbox' }, { key: 'openLimitTimeFlag', label: '開封後有效時間管理', type: 'checkbox' }, { key: 'openLimitTime', label: '開封時限（小時）', type: 'number', step: '0.1' }, { key: 'resetOpenDate', label: '下料後重啟開封計時日', type: 'checkbox' }, { key: 'keyMaterials', label: '是否關鍵物料', type: 'checkbox' }, { key: 'description', label: '說明', type: 'textarea', wide: true }] }],
  },
  'process-master': {
    title: '流程主檔', plural: '流程', endpoint: 'processes', keyFields: ['processNo', 'processVersion'],
    columns: [{ key: 'processNo', label: '流程編號' }, { key: 'processVersion', label: '流程版本' }, { key: 'processType', label: '流程類別' }, { key: 'productionSegmentNo', label: '製程段' }, { key: 'processClass', label: '流程分類' }, { key: 'currentVersion', label: '目前版本' }, { key: 'tuningProcess', label: '調機流程' }],
    sections: [{ title: '流程識別與版本', fields: [{ key: 'processNo', label: '流程編號', required: true }, { key: 'processVersion', label: '流程版本', required: true }, { key: 'processType', label: '流程類別', required: true }, { key: 'productionSegmentNo', label: '製程段', required: true }, { key: 'processClass', label: '流程分類', type: 'number' }, { key: 'partialType', label: 'Partial 類型', type: 'number' }, { key: 'tuningProcess', label: '調機流程', type: 'checkbox' }, { key: 'description', label: '說明', type: 'textarea', wide: true }] }],
  },
  'product-master': {
    title: '產品主檔', plural: '產品', endpoint: 'products', keyFields: ['productNo', 'productVersion'],
    columns: [{ key: 'productNo', label: '產品編號' }, { key: 'productVersion', label: '產品版本' }, { key: 'productName', label: '產品名稱' }, { key: 'productType', label: '產品類別' }, { key: 'unitNo', label: '庫存單位' }, { key: 'lotStdQty', label: 'Lot 標準量' }, { key: 'specNo', label: '規格編號' }, { key: 'erpNo', label: 'ERP 編號' }, { key: 'currentVersion', label: '目前版本' }],
    sections: [{ title: '產品識別與版本', fields: [{ key: 'productNo', label: '產品編號', required: true }, { key: 'productVersion', label: '產品版本', required: true }, { key: 'productName', label: '產品名稱', required: true, wide: true }, { key: 'productType', label: '產品類別', required: true }, { key: 'lotStdQty', label: 'Lot 標準量', type: 'number', step: '0.001' }, { key: 'unitType', label: '單位類型' }, { key: 'unitNo', label: '庫存單位', required: true }, { key: 'erpNo', label: 'ERP 編號' }, { key: 'specNo', label: '規格編號' }, { key: 'productCode', label: '產品代碼' }, { key: 'serialTypeNoLot', label: 'Lot 序號類型' }, { key: 'serialTypeNoComp', label: 'Component 序號類型' }, { key: 'description', label: '說明', type: 'textarea', wide: true }] }],
  },
}

const config = computed(() => configs[props.masterKey])
const pageTitle = computed(() => config.value.title)
const rows = ref<Row[]>([])
const selected = ref<Row | null>(null)
const issueState = ref(0)
const keyword = ref('')
const busy = ref(false)
const error = ref('')
const notice = ref('')
const formOpen = ref(false)
const formMode = ref<'new' | 'edit' | 'copy'>('new')
const form = ref<Row>({})
const processes = ref<Row[]>([])
const bom = ref<Row[]>([])
const availableProcesses = ref<Row[]>([])
const availableOperations = ref<Row[]>([])
const availableMaterials = ref<Row[]>([])
const availableProducts = ref<Row[]>([])
const graph = ref<{ nodes: Row[]; edges: Row[]; dataStamp: number }>({ nodes: [], edges: [], dataStamp: 0 })
const processDraft = ref<Row>({ moTypeNo: 10, productionSegmentNo: 'FRONT', processNo: '', processVersionSelector: '*', defaultProcess: true })
const bomDraft = ref<Row>({ componentType: 'MATERIAL', componentNo: '', inputOperationNo: '', standardQty: 1, putInPlaceType: 'At OP', specified: false, decreaseRate: 0, description: '' })
const newNode = ref({ nodeId: '', nodeNo: '', nodeType: 'OP', sequence: 10, operationNo: '', description: '' })
const newEdge = ref({ fromNodeId: '', toNodeId: '', action: 'pass' })
let initialSelection = ''

const selectedKey = computed(() => selected.value ? config.value.keyFields.map((key) => String(selected.value?.[key] ?? '')).join('/') : '')
const availableProcessNos = computed(() => [...new Set(availableProcesses.value.map((item) => String(item.processNo ?? '')))].filter(Boolean))
const availableProcessVersions = computed(() => availableProcesses.value.filter((item) => String(item.processNo) === String(processDraft.value.processNo)).map((item) => String(item.processVersion ?? '')).filter(Boolean))
const isEditable = computed(() => selected.value?.issueState === 0)
const selectedRouteKey = (row: Row) => config.value.keyFields.map((key) => String(row[key] ?? '')).join('/')
const stateLabel = (value: Value) => ({ 0: '未簽核', 1: '簽核中', 2: '已簽核', '-1': '不使用' } as Record<string, string>)[String(value)] ?? '未知'
const displayValue = (value: Value) => typeof value === 'boolean' ? (value ? '是' : '否') : value === null || value === undefined || value === '' ? '—' : String(value)
const fieldValue = (field: Field) => {
  const value = form.value[field.key]
  return value === null || value === undefined || typeof value === 'boolean' ? '' : String(value)
}

function updateFormField(field: Field, event: Event): void {
  const value = (event.target as HTMLInputElement | HTMLTextAreaElement).value
  form.value[field.key] = field.type === 'number' ? (value === '' ? null : Number(value)) : value
}

function updateCheckboxField(key: string, event: Event): void {
  form.value[key] = (event.target as HTMLInputElement).checked
}

function makeUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  const resolvedBase = new URL(base, window.location.origin)
  return new URL(path.replace(/^\/+/, ''), resolvedBase.toString().replace(/\/+$/, '') + '/').toString()
}

async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(makeUrl(`api/${path}`), { credentials: 'include', ...init })
  if (response.status === 401) {
    window.location.assign(appBase + '/')
    throw new Error('登入狀態已逾期，請重新登入。')
  }
  if (!response.ok) {
    const body = await response.json().catch(() => null) as { detail?: string; title?: string; message?: string } | null
    throw new Error(body?.detail || body?.message || body?.title || `API 回應錯誤 (${response.status})`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}

function rowPath(row: Row): string {
  const key = config.value.keyFields.map((field) => encodeURIComponent(String(row[field] ?? '')))
  if (props.masterKey === 'product-master') return `products/${key[0]}/versions/${key[1]}`
  return `${config.value.endpoint}/${key.join('/')}`
}

async function loadRows(): Promise<void> {
  busy.value = true
  error.value = ''
  try {
    const query = new URLSearchParams({ issueState: String(issueState.value) })
    if (keyword.value.trim()) query.set('keyword', keyword.value.trim())
    rows.value = await api<Row[]>(`${config.value.endpoint}?${query}`)
    const url = new URL(window.location.href)
    const hasAddressForm = url.searchParams.has('form')
    const wanted = url.searchParams.get('selected') || initialSelection
    selected.value = rows.value.find((row) => selectedRouteKey(row) === wanted) ?? rows.value[0] ?? null
    initialSelection = ''
    if (!hasAddressForm) writeAddress()
    await loadRelated()
    restoreFormFromAddress()
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : `${config.value.plural}資料載入失敗。`
  } finally {
    busy.value = false
  }
}

async function loadRelated(): Promise<void> {
  processes.value = []
  bom.value = []
  availableProcesses.value = []
  availableOperations.value = []
  availableMaterials.value = []
  availableProducts.value = []
  graph.value = { nodes: [], edges: [], dataStamp: 0 }
  if (!selected.value) return
  try {
    if (props.masterKey === 'product-master') {
      const base = rowPath(selected.value)
      const [p, b, processList, operationList, materialList, productList] = await Promise.all([
        api<Row[]>(`${base}/processes`), api<Row[]>(`${base}/bom`),
        api<Row[]>('processes?issueState=2'), api<Row[]>('operations?issueState=2'), api<Row[]>('materials?issueState=2'), api<Row[]>('products?issueState=2'),
      ])
      processes.value = p; bom.value = b; availableProcesses.value = processList; availableOperations.value = operationList; availableMaterials.value = materialList; availableProducts.value = productList
    }
    if (props.masterKey === 'process-master') {
      const [processGraph, operationList] = await Promise.all([api<{ nodes: Row[]; edges: Row[]; dataStamp: number }>(`${rowPath(selected.value)}/graph`), api<Row[]>('operations?issueState=2')])
      graph.value = processGraph; availableOperations.value = operationList
    }
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : '關聯資料載入失敗。'
  }
}

function writeAddress(formQuery?: string): void {
  const url = new URL(window.location.href)
  url.searchParams.set('issueState', String(issueState.value))
  if (keyword.value.trim()) url.searchParams.set('keyword', keyword.value.trim()); else url.searchParams.delete('keyword')
  if (selected.value) url.searchParams.set('selected', selectedRouteKey(selected.value)); else url.searchParams.delete('selected')
  if (formQuery) url.searchParams.set('form', formQuery); else { url.searchParams.delete('form'); url.searchParams.delete('id'); url.searchParams.delete('version') }
  window.history.replaceState(window.history.state, '', url.pathname + url.search)
}

function defaults(): Row {
  const result: Row = {}
  for (const section of config.value.sections) for (const field of section.fields) {
    result[field.key] = field.type === 'checkbox' ? false : field.type === 'number' ? 0 : ''
  }
  result.dataStamp = 0
  if (props.masterKey === 'operation-master') { result.countOperationUnitQty = 1; result.operationClass = 1; result.productionSegmentNo = 'FRONT' }
  if (props.masterKey === 'material-master') { result.unitNo = 'PCS'; result.checkLotNo = 0; result.erpNo = '' }
  if (props.masterKey === 'process-master') { result.processVersion = '01'; result.processClass = 1; result.productionSegmentNo = 'FRONT' }
  if (props.masterKey === 'product-master') { result.productVersion = '01'; result.unitNo = 'PCS'; result.lotStdQty = 0 }
  return result
}

function openForm(mode: 'new' | 'edit' | 'copy', row: Row | null = null): void {
  formMode.value = mode
  form.value = mode === 'new' ? defaults() : { ...row }
  if (mode === 'copy') {
    form.value.dataStamp = 0
    if (props.masterKey === 'operation-master') form.value.operationNo = ''
    if (props.masterKey === 'material-master') form.value.materialNo = ''
    if (props.masterKey === 'process-master') { form.value.processNo = ''; form.value.processVersion = '01' }
    if (props.masterKey === 'product-master') { form.value.productNo = ''; form.value.productVersion = '01' }
  }
  formOpen.value = true
  const id = row ? String(row[config.value.keyFields[0]] ?? '') : ''
  const version = row && config.value.keyFields[1] ? String(row[config.value.keyFields[1]] ?? '') : ''
  const url = new URL(window.location.href)
  url.searchParams.set('form', mode)
  if (id) url.searchParams.set('id', id)
  if (version) url.searchParams.set('version', version)
  window.history.pushState({ masterForm: true }, '', url.pathname + url.search)
}

function restoreFormFromAddress(): void {
  const url = new URL(window.location.href)
  const mode = url.searchParams.get('form')
  if (mode !== 'new' && mode !== 'edit' && mode !== 'copy') return
  const id = url.searchParams.get('id')
  const version = url.searchParams.get('version')
  const row = rows.value.find((item) => String(item[config.value.keyFields[0]]) === id && (!version || String(item[config.value.keyFields[1]]) === version)) ?? null
  formMode.value = mode
  form.value = mode === 'new' ? defaults() : { ...(row ?? defaults()) }
  if (mode === 'copy') {
    form.value.dataStamp = 0
    for (const key of config.value.keyFields) form.value[key] = key.endsWith('Version') ? '01' : ''
  }
  formOpen.value = true
}

function closeForm(fromHistory = false): void {
  formOpen.value = false
  if (!fromHistory) {
    const url = new URL(window.location.href)
    url.searchParams.delete('form'); url.searchParams.delete('id'); url.searchParams.delete('version')
    window.history.replaceState(window.history.state, '', url.pathname + url.search)
  }
}

async function save(): Promise<void> {
  busy.value = true; error.value = ''
  try {
    const isEdit = formMode.value === 'edit'
    const body = JSON.stringify(form.value)
    const method = isEdit ? 'PUT' : 'POST'
    const path = isEdit ? rowPath(form.value) : config.value.endpoint
    const saved = await api<Row>(path, { method, headers: { 'Content-Type': 'application/json' }, body })
    notice.value = `${config.value.plural} ${config.value.keyFields.map((key) => String(saved[key])).join('/')} 已${isEdit ? '更新' : '建立'}。`
    closeForm()
    await loadRows()
    selected.value = rows.value.find((row) => selectedRouteKey(row) === selectedRouteKey(saved)) ?? saved
    await loadRelated()
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '資料儲存失敗。' }
  finally { busy.value = false }
}

async function command(action: 'approve' | 'delete' | 'new-version' | 'set-current'): Promise<void> {
  const item = selected.value
  if (!item || busy.value) return
  if (action === 'delete' && !window.confirm(`確定${item.issueState === 2 ? '停用' : '刪除'}${config.value.plural} ${selectedRouteKey(item)}？`)) return
  busy.value = true; error.value = ''
  try {
    const suffix = action === 'approve' ? '/approve' : action === 'new-version' ? '/new-version' : action === 'set-current' ? '/set-current' : ''
    if (action === 'delete') {
      const query = new URLSearchParams({ issueState: String(item.issueState), dataStamp: String(item.dataStamp) })
      await api(`${rowPath(item)}?${query}`, { method: 'DELETE' })
    } else if (action === 'new-version') {
      const next = await api<Row>(`${rowPath(item)}${suffix}`, { method: 'POST' })
      notice.value = `已建立新版本 ${selectedRouteKey(next)}。`
    } else {
      const next = await api<Row>(`${rowPath(item)}${suffix}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ dataStamp: item.dataStamp }) })
      notice.value = action === 'approve' ? '資料已核准。' : '已設為目前版本。'
      selected.value = next
    }
    if (action === 'delete') notice.value = `${config.value.plural}已${item.issueState === 2 ? '停用' : '刪除'}。`
    await loadRows()
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '操作失敗。' }
  finally { busy.value = false }
}

async function saveGraph(): Promise<void> {
  if (!selected.value) return
  busy.value = true
  try {
    graph.value = await api(`${rowPath(selected.value)}/graph`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(graph.value) })
    notice.value = '流程圖已儲存。'
    await loadRows()
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '流程圖儲存失敗。' }
  finally { busy.value = false }
}

async function addProductProcess(): Promise<void> {
  if (!selected.value) return
  busy.value = true
  try {
    await api(`${rowPath(selected.value)}/processes`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(processDraft.value) })
    notice.value = '產品流程已加入。'; await loadRelated()
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '產品流程新增失敗。' }
  finally { busy.value = false }
}

async function deleteProductProcess(item: Row): Promise<void> {
  if (!selected.value) return
  const query = new URLSearchParams({ moTypeNo: String(item.moTypeNo), productionSegmentNo: String(item.productionSegmentNo) })
  try { await api(`${rowPath(selected.value)}/processes/${encodeURIComponent(String(item.processNo))}/${encodeURIComponent(String(item.processVersionSelector))}?${query}`, { method: 'DELETE' }); await loadRelated() }
  catch (reason) { error.value = reason instanceof Error ? reason.message : '產品流程移除失敗。' }
}

async function addBom(): Promise<void> {
  if (!selected.value) return
  busy.value = true
  try { await api(`${rowPath(selected.value)}/bom`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(bomDraft.value) }); notice.value = 'BOM 項目已加入。'; await loadRelated() }
  catch (reason) { error.value = reason instanceof Error ? reason.message : 'BOM 新增失敗。' }
  finally { busy.value = false }
}

async function deleteBom(item: Row): Promise<void> {
  if (!selected.value) return
  try { await api(`${rowPath(selected.value)}/bom/${encodeURIComponent(String(item.id))}`, { method: 'DELETE' }); await loadRelated() }
  catch (reason) { error.value = reason instanceof Error ? reason.message : 'BOM 移除失敗。' }
}

function appendNode(): void {
  if (!newNode.value.nodeId || !newNode.value.nodeNo) return
  graph.value.nodes.push({ ...newNode.value })
  newNode.value = { nodeId: '', nodeNo: '', nodeType: 'OP', sequence: graph.value.nodes.length * 10, operationNo: '', description: '' }
}

function removeNode(nodeId: string): void {
  graph.value.nodes = graph.value.nodes.filter((node) => String(node.nodeId) !== nodeId)
  graph.value.edges = graph.value.edges.filter((edge) => String(edge.fromNodeId) !== nodeId && String(edge.toNodeId) !== nodeId)
}

function handlePopState(): void {
  const url = new URL(window.location.href)
  const mode = url.searchParams.get('form')
  if (!mode) { closeForm(true); return }
  const id = url.searchParams.get('id')
  const version = url.searchParams.get('version')
  const row = rows.value.find((item) => String(item[config.value.keyFields[0]]) === id && (!version || String(item[config.value.keyFields[1]]) === version))
  const normalizedMode = mode === 'edit' ? 'edit' : mode === 'copy' ? 'copy' : 'new'
  formMode.value = normalizedMode
  form.value = normalizedMode === 'new' ? defaults() : { ...(row ?? defaults()) }
  formOpen.value = true
}

function printPage(): void { window.print() }

function downloadCsv(): void {
  const headings = config.value.columns.map((column) => column.label)
  const body = rows.value.map((row) => config.value.columns.map((column) => String(row[column.key] ?? '').replaceAll('"', '""')))
  const csv = [headings, ...body].map((line) => line.map((value) => `"${value}"`).join(',')).join('\r\n')
  const url = URL.createObjectURL(new Blob(['\uFEFF', csv], { type: 'text/csv;charset=utf-8' }))
  const link = document.createElement('a'); link.href = url; link.download = `${config.value.endpoint}.csv`; link.click(); URL.revokeObjectURL(url)
}

onMounted(() => {
  const url = new URL(window.location.href)
  issueState.value = Number(url.searchParams.get('issueState') ?? 0)
  keyword.value = url.searchParams.get('keyword') ?? ''
  initialSelection = url.searchParams.get('selected') ?? ''
  void loadRows()
  window.addEventListener('popstate', handlePopState)
})
onBeforeUnmount(() => window.removeEventListener('popstate', handlePopState))
watch(() => props.masterKey, () => { selected.value = null; void loadRows() })
</script>

<template>
  <div class="md-master">
    <form class="md-query" @submit.prevent="loadRows">
      <fieldset><legend>{{ config.plural }}</legend><label v-for="state in [{value:0,label:'未簽核'},{value:1,label:'簽核中'},{value:2,label:'已簽核'},{value:-1,label:'不使用'}]" :key="state.value"><input v-model.number="issueState" type="radio" name="issueState" :value="state.value" />{{ state.label }}</label></fieldset>
      <label class="md-search">{{ config.plural }}編號 <input v-model="keyword" type="search" /></label><span>%</span>
      <button type="submit" :disabled="busy">查詢</button>
    </form>
    <p v-if="error" class="md-message error" role="alert">{{ error }}</p><p v-if="notice" class="md-message" role="status">{{ notice }}</p>
    <section class="md-workspace">
      <div class="md-list-panel">
        <div class="md-tools"><button class="primary" type="button" @click="openForm('new')">新增(A)</button><button v-if="selected && selected.issueState === 0" type="button" :disabled="busy" @click="openForm('edit', selected)">修改(E)</button><button v-if="selected" type="button" @click="openForm('copy', selected)">複製(C)</button><button v-if="selected && selected.issueState === 0" type="button" :disabled="busy" @click="command('approve')">核准</button><button v-if="selected && [0,2,-1].includes(Number(selected.issueState))" type="button" :disabled="busy" @click="command('delete')">{{ selected.issueState === 2 ? '停用(D)' : '刪除(D)' }}</button><button v-if="selected && selected.issueState === 2 && masterKey === 'product-master'" type="button" :disabled="busy" @click="command('new-version')">複製新版本</button><button v-if="selected && selected.issueState === 2 && selected.currentVersion === false && masterKey === 'product-master'" type="button" :disabled="busy" @click="command('set-current')">設為目前版</button><button type="button" @click="downloadCsv">Download</button><button type="button" @click="window.print()">Print</button><button type="button" disabled>匯入</button></div>
        <div class="md-table-scroll"><table><thead><tr><th v-for="col in config.columns" :key="col.key">{{ col.label }}</th><th>狀態</th></tr></thead><tbody>
          <tr v-for="row in rows" :key="selectedRouteKey(row)" :class="{selected: selectedKey === selectedRouteKey(row)}" tabindex="0" @click="selected = row; writeAddress(); void loadRelated()" @keydown.enter.prevent="selected = row; writeAddress(); void loadRelated()"><td v-for="col in config.columns" :key="col.key">{{ displayValue(row[col.key]) }}</td><td>{{ stateLabel(row.issueState) }}</td></tr>
          <tr v-if="!busy && rows.length === 0"><td :colspan="config.columns.length + 1" class="empty">沒有符合條件的{{ config.plural }}資料</td></tr>
          <tr v-if="busy && rows.length === 0"><td :colspan="config.columns.length + 1" class="empty">資料載入中…</td></tr>
        </tbody></table></div><footer>資料總筆數 <b>{{ rows.length }}</b></footer>
      </div>
      <aside class="md-detail" v-if="selected">
        <header><div><small>{{ config.title }}</small><h2>{{ selectedRouteKey(selected) }}</h2><p>{{ selected.operationName || selected.materialName || selected.description || selected.productName || selected.processType }}</p></div><span class="md-state">{{ stateLabel(selected.issueState) }}</span></header>
        <div class="md-detail-fields"><div v-for="col in config.columns.slice(1)" :key="col.key"><small>{{ col.label }}</small><b>{{ displayValue(selected[col.key]) }}</b></div></div>
        <section v-if="masterKey === 'process-master'" class="md-relations"><div class="md-relation-title"><h3>流程節點與連線</h3><button type="button" :disabled="!isEditable || busy" @click="saveGraph">儲存流程圖</button></div><div class="md-node-add"><input v-model="newNode.nodeId" placeholder="Node ID"/><input v-model="newNode.nodeNo" placeholder="節點編號"/><select v-model="newNode.nodeType"><option value="START">START</option><option value="OP">作業</option><option value="END">END</option></select><input v-model.number="newNode.sequence" type="number" placeholder="順序"/><select v-model="newNode.operationNo"><option value="">選擇作業</option><option v-for="item in availableOperations" :key="String(item.operationNo)" :value="item.operationNo">{{ item.operationNo }}</option></select><input v-model="newNode.description" placeholder="說明"/><button type="button" :disabled="!isEditable" @click="appendNode">加入節點</button></div><div class="md-table-scroll"><table><thead><tr><th>節點</th><th>類型</th><th>順序</th><th>作業</th><th>說明</th><th>操作</th></tr></thead><tbody><tr v-for="node in graph.nodes" :key="String(node.nodeId)"><td>{{ node.nodeNo }}</td><td>{{ node.nodeType }}</td><td>{{ node.sequence }}</td><td>{{ node.operationNo }}</td><td>{{ node.description }}</td><td><button type="button" :disabled="!isEditable" @click="removeNode(String(node.nodeId))">移除</button></td></tr></tbody></table></div><div class="md-edge-add"><select v-model="newEdge.fromNodeId"><option value="">起始節點</option><option v-for="node in graph.nodes" :key="String(node.nodeId)" :value="node.nodeId">{{ node.nodeNo }}</option></select><select v-model="newEdge.toNodeId"><option value="">目標節點</option><option v-for="node in graph.nodes" :key="String(node.nodeId)" :value="node.nodeId">{{ node.nodeNo }}</option></select><input v-model="newEdge.action" placeholder="條件"/><button type="button" :disabled="!isEditable || !newEdge.fromNodeId || !newEdge.toNodeId" @click="graph.edges.push({...newEdge}); newEdge={fromNodeId:'',toNodeId:'',action:'pass'}">新增連線</button></div><ul class="md-edge-list"><li v-for="(edge,index) in graph.edges" :key="index">{{ edge.fromNodeId }} → {{ edge.toNodeId }}（{{ edge.action }}） <button type="button" :disabled="!isEditable" @click="graph.edges.splice(index,1)">移除</button></li></ul></section>
        <section v-if="masterKey === 'product-master'" class="md-relations"><h3>產品流程指派（{{ processes.length }}）</h3><div class="md-table-scroll"><table><thead><tr><th>工單類型</th><th>製程段</th><th>流程</th><th>版本</th><th>預設</th><th v-if="isEditable">操作</th></tr></thead><tbody><tr v-for="(item,index) in processes" :key="index"><td>{{ item.moTypeNo }}</td><td>{{ item.productionSegmentNo }}</td><td>{{ item.processNo }}</td><td>{{ item.processVersionSelector }}</td><td>{{ item.defaultProcess ? '是' : '否' }}</td><td v-if="isEditable"><button type="button" @click="deleteProductProcess(item)">移除</button></td></tr></tbody></table></div><form v-if="isEditable" class="md-relation-form" @submit.prevent="addProductProcess"><label>工單類型<input v-model.number="processDraft.moTypeNo" type="number"/></label><label>製程段<input v-model="processDraft.productionSegmentNo" required/></label><label>流程編號<select v-model="processDraft.processNo" required><option value="">選擇流程</option><option v-for="no in availableProcessNos" :key="no" :value="no">{{ no }}</option></select></label><label>流程版本<select v-model="processDraft.processVersionSelector" required><option value="*">*（目前版本）</option><option v-for="version in availableProcessVersions" :key="version" :value="version">{{ version }}（固定）</option></select></label><label><input v-model="processDraft.defaultProcess" type="checkbox"/>預設流程</label><button type="submit" :disabled="busy">加入流程</button></form>
          <h3>產品 BOM（{{ bom.length }}）</h3><div class="md-table-scroll"><table><thead><tr><th>類型</th><th>Component</th><th>投入作業</th><th>標準用量</th><th>放置方式</th><th v-if="isEditable">操作</th></tr></thead><tbody><tr v-for="(item,index) in bom" :key="String(item.id || index)"><td>{{ item.componentType }}</td><td>{{ item.componentNo }}</td><td>{{ item.inputOperationNo }}</td><td>{{ item.standardQty }}</td><td>{{ item.putInPlaceType }}</td><td v-if="isEditable"><button type="button" @click="deleteBom(item)">移除</button></td></tr></tbody></table></div><form v-if="isEditable" class="md-relation-form" @submit.prevent="addBom"><label>Component 類型<select v-model="bomDraft.componentType"><option value="MATERIAL">物料</option><option value="PRODUCT">子產品</option></select></label><label>Component 編號<select v-model="bomDraft.componentNo" required><option value="">選擇 Component</option><template v-if="bomDraft.componentType === 'MATERIAL'"><option v-for="item in availableMaterials" :key="String(item.materialNo)" :value="item.materialNo">{{ item.materialNo }} {{ item.materialName }}</option></template><template v-else><option v-for="item in availableProducts" :key="String(item.productNo)" :value="item.productNo">{{ item.productNo }} {{ item.productName }}</option></template></select></label><label>投入作業<select v-model="bomDraft.inputOperationNo" required><option value="">選擇作業</option><option v-for="item in availableOperations" :key="String(item.operationNo)" :value="item.operationNo">{{ item.operationNo }}</option></select></label><label>標準用量<input v-model.number="bomDraft.standardQty" type="number" min="0.000001" step="0.001"/></label><label>放置方式<input v-model="bomDraft.putInPlaceType"/></label><label>損耗率 %<input v-model.number="bomDraft.decreaseRate" type="number" min="0" max="100" step="0.1"/></label><button type="submit" :disabled="busy">新增 BOM</button></form>
        </section>
        <div class="md-meta">建立於 {{ selected.createdAt || '—' }} · DataStamp {{ selected.dataStamp }}</div>
      </aside>
      <aside v-else class="md-detail md-no-selection"><h2>選取一筆{{ config.plural }}資料</h2><p>查看主檔內容與可用功能。</p></aside>
    </section>

    <div v-if="formOpen" class="md-modal-backdrop" @click.self="closeForm()"><form class="md-form-dialog" @submit.prevent="save"><header><div><small>{{ config.title }}定義（{{ formMode === 'edit' ? 'EDIT' : formMode === 'copy' ? 'COPY' : 'ADD' }}）</small><h2>{{ config.plural }}基本資料</h2></div><button type="button" aria-label="關閉" @click="closeForm()">×</button></header><div class="md-form-scroll"><section v-for="section in config.sections" :key="section.title" class="md-form-section"><h3>{{ section.title }}</h3><div class="md-form-grid"><label v-for="field in section.fields" :key="field.key" :class="{wide:field.wide, checkbox:field.type==='checkbox'}"><span>{{ field.label }}<i v-if="field.required">*</i></span><input v-if="field.type==='checkbox'" type="checkbox" :checked="Boolean(form[field.key])" @change="updateCheckboxField(field.key,$event)"/><textarea v-else-if="field.type==='textarea'" :value="fieldValue(field)" rows="3" @input="updateFormField(field,$event)"/><input v-else :value="fieldValue(field)" :type="field.type || 'text'" :step="field.step" :required="field.required" :readonly="field.readonly || (formMode==='edit' && config.keyFields.includes(field.key))" @input="updateFormField(field,$event)"/></label></div></section><div class="md-audit"><label>建立人<input readonly value="—"/></label><label>建立日期<input readonly :value="form.createdAt ? String(form.createdAt) : '—'"/></label><label>修改人<input readonly value="—"/></label><label>修改日期<input readonly :value="form.revisedAt ? String(form.revisedAt) : '—'"/></label></div></div><footer><button class="primary" type="submit" :disabled="busy">{{ busy ? '儲存中…' : '確定' }}</button><button type="button" @click="closeForm()">關閉</button><button type="button" disabled>套用</button></footer></form></div>
  </div>
</template>

<style scoped>
.md-master{min-width:0;padding:10px;background:#fff;color:#333;font:12px 'Segoe UI','Microsoft JhengHei',sans-serif}.md-query{display:flex;align-items:center;gap:10px;flex-wrap:wrap;margin-bottom:7px;padding:6px;border:1px solid #ddd}.md-query fieldset{display:flex;gap:12px;margin:0;padding:0 8px;border:0}.md-query legend{position:absolute;clip:rect(0 0 0 0)}.md-query label,.md-query button,.md-query input,.md-query select{font:inherit}.md-search{display:flex;align-items:center;gap:8px}.md-search input{height:28px;width:190px;border:1px solid #999;padding:4px}.md-query button,.md-tools button,.md-relation-title button,.md-node-add button,.md-edge-add button,.md-relation-form button,.md-detail button{min-height:28px;padding:4px 12px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#eee);color:#333;font:inherit}.md-tools button.primary,.md-form-dialog footer .primary{background:#ffa62b;border-color:#f3a02a;color:#fff}.md-message{margin:5px 0;padding:7px 10px;border:1px solid #9bcab2;background:#effaf3}.md-message.error{border-color:#dba4a4;background:#fff3f3;color:#9d2424}.md-workspace{display:grid;grid-template-columns:minmax(0,1fr) minmax(300px,34%);gap:10px;align-items:start}.md-list-panel,.md-detail{min-width:0;border:1px solid #e8178a;padding:6px;background:#fff}.md-tools{display:flex;gap:5px;align-items:center;flex-wrap:wrap;margin-bottom:6px}.md-table-scroll{min-width:0;max-height:56vh;overflow:auto;border:1px solid #bbb}.md-master table{width:100%;border-collapse:collapse;white-space:nowrap}.md-master th,.md-master td{padding:5px 8px;border:1px solid #ccc;text-align:left}.md-master th{position:sticky;top:0;background:#38a0ea;color:white;font-weight:500}.md-master tbody tr{cursor:pointer}.md-master tbody tr:hover{background:#fff6d7}.md-master tbody tr.selected{background:#ffe99a}.md-master .empty{text-align:center;padding:25px;color:#777}.md-list-panel footer{padding:7px 2px;color:#777}.md-detail header{display:flex;justify-content:space-between;gap:12px;padding:4px 5px 10px;border-bottom:1px solid #ddd}.md-detail header small,.md-detail small{color:#777}.md-detail h2{margin:4px 0;font-size:18px}.md-detail p{margin:3px 0;color:#777}.md-state{height:fit-content;padding:4px 7px;border:1px solid #ccc;background:#f8f8f8}.md-detail-fields{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:8px;padding:10px 4px}.md-detail-fields div{display:grid;gap:3px;min-width:0}.md-detail-fields b{overflow-wrap:anywhere;font-weight:500}.md-relations{border-top:1px solid #ddd;padding:8px 0}.md-relations h3{font-size:13px;margin:7px 0}.md-relation-title{display:flex;align-items:center;justify-content:space-between}.md-node-add,.md-edge-add,.md-relation-form{display:flex;gap:5px;flex-wrap:wrap;margin:6px 0}.md-node-add input,.md-node-add select,.md-edge-add input,.md-edge-add select,.md-relation-form input,.md-relation-form select{min-width:90px;height:28px;padding:4px;border:1px solid #aaa;font:inherit}.md-relation-form label{display:grid;gap:3px}.md-edge-list{padding-left:18px}.md-meta{margin-top:10px;padding-top:6px;border-top:1px solid #ddd;color:#777;font-size:11px}.md-no-selection{min-height:200px;padding:25px;text-align:center}.md-modal-backdrop{position:fixed;inset:0;z-index:20;display:grid;place-items:center;padding:18px;background:#0005}.md-form-dialog{display:grid;width:min(980px,100%);max-height:92vh;grid-template-rows:auto minmax(0,1fr) auto;background:#fff;border:1px solid #999;box-shadow:0 12px 40px #0004}.md-form-dialog>header{display:flex;align-items:center;justify-content:space-between;padding:12px 18px;border-bottom:1px solid #ddd}.md-form-dialog h2{margin:3px 0;font-size:19px}.md-form-dialog>header button{width:32px;height:32px;border:1px solid #bbb;background:#fafafa;font-size:20px}.md-form-scroll{overflow:auto;padding:12px 18px}.md-form-section{margin-bottom:12px;padding:12px;border:1px solid #bbb}.md-form-section h3{margin:0 0 12px;font-size:15px;font-weight:500}.md-form-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:10px 24px}.md-form-grid label{display:grid;grid-template-columns:minmax(105px,35%) minmax(0,1fr);align-items:center;gap:8px}.md-form-grid label.wide{grid-column:1/-1}.md-form-grid label.checkbox{display:flex}.md-form-grid label>span{color:#444}.md-form-grid label i{color:#d00;font-style:normal}.md-form-grid input:not([type=checkbox]),.md-form-grid textarea{width:100%;min-height:30px;padding:4px 7px;border:1px solid #888;background:#fff;font:inherit}.md-form-grid label:nth-child(3n+1) input:not([type=checkbox]){background:#fff1aa}.md-form-grid input[readonly]{background:#eee!important;color:#666}.md-form-grid input[type=checkbox]{width:15px;height:15px}.md-audit{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:8px;padding:10px;border:1px solid #ed4b9c}.md-audit label{display:grid;grid-template-columns:auto minmax(0,1fr);align-items:center;gap:5px}.md-audit input{min-width:0;height:28px;border:1px solid #ccc;background:#eee;padding:4px;font:inherit}.md-form-dialog>footer{display:flex;justify-content:flex-end;gap:8px;padding:10px 16px;border-top:1px solid #ddd}.md-form-dialog>footer button{min-width:86px;height:34px;border:1px solid #bbb;background:#fafafa;font:inherit}@media(max-width:900px){.md-workspace{grid-template-columns:minmax(0,1fr)}.md-detail{max-height:none}.md-table-scroll{max-height:44vh}}@media(max-width:600px){.md-form-grid{grid-template-columns:minmax(0,1fr)}.md-form-grid label.wide{grid-column:auto}.md-audit{grid-template-columns:repeat(2,minmax(0,1fr))}.md-form-dialog{max-height:96vh}.md-query fieldset{flex-wrap:wrap}}
</style>
