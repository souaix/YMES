<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type IssueState = -1 | 0 | 1 | 2
type FormMode = 'new' | 'edit' | 'copy' | ''
interface BusinessRuleRow {
  brNo: string; brName: string; ruleType: number | null; ruleMethod: number | null
  functionName: string; executionFile: string; interfaceName: string; interfaceUrl: string; description: string
  allowTimeLimitControl: number | null; brProdType: number | null; issueState: IssueState
  creator: string; createDate: string; reviser: string; reviseDate: string; dataStamp: number | null
}
interface ValueOption { code: number; displayName: string }
interface BusinessRuleOptions {
  functions: { functionName: string; executionFile: string }[]
  interfaces: { interfaceName: string; executionFile: string }[]
  scenarios: { scenarioNo: string; scenarioName: string }[]
  brProductTypes: ValueOption[]
  allowTimeLimitControl: ValueOption[]
}
interface Draft {
  brNo: string; brName: string; ruleType: number | null; ruleMethod: number | null
  functionName: string; interfaceName: string; interfaceUrl: string; scenarioNo: string
  description: string; allowTimeLimitControl: number; brProdType: number | null
}
interface Envelope<T> { items?: T[]; rows?: T[]; data?: T[]; message?: string; title?: string }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes.dev.op-business-rule-basis.v1'
const issueStates: { value: IssueState; label: string }[] = [
  { value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' },
]
const seedOptions: BusinessRuleOptions = {
  functions: [
    { functionName: 'FUNBR_CHECK_WIP', executionFile: 'DEV_OPBusinessRule.dll' },
    { functionName: 'FUNBR_VALIDATE_LOT', executionFile: 'DEV_OPBusinessRule.dll' },
    { functionName: 'FUNBR_CUSTOMER_CHECK', executionFile: 'DEV_CustomerBR.dll' },
  ],
  interfaces: [
    { interfaceName: 'FRMBR_WIPCHECK', executionFile: 'DEV_OPInterface.dll' },
    { interfaceName: 'FRMBR_LOTCHECK', executionFile: 'DEV_OPInterface.dll' },
  ],
  scenarios: [
    { scenarioNo: 'DEV-SCN-001', scenarioName: 'DEV 生產前檢核' },
    { scenarioNo: 'DEV-SCN-002', scenarioName: 'DEV 首件流程' },
  ],
  brProductTypes: [
    { code: 1, displayName: 'DEV 假產品類型 1' }, { code: 2, displayName: 'DEV 假產品類型 2' }, { code: 3, displayName: 'DEV 假產品類型 3' },
  ],
  allowTimeLimitControl: [{ code: 0, displayName: 'No' }, { code: 1, displayName: 'Yes' }],
}
const seedRows: BusinessRuleRow[] = [
  { brNo: 'DEV-BR-001', brName: 'DEV 開工前批次檢核', ruleType: 0, ruleMethod: 0, functionName: 'FUNBR_CHECK_WIP', executionFile: 'DEV_OPBusinessRule.dll', interfaceName: '', interfaceUrl: '', description: 'DEV 假資料：確認批次狀態與可開工條件。', allowTimeLimitControl: 0, brProdType: 1, issueState: 0, creator: 'DEV001', createDate: '2026-11-03T08:30:00', reviser: '', reviseDate: '', dataStamp: 1 },
  { brNo: 'DEV-BR-002', brName: 'DEV 客戶介面檢核', ruleType: 1, ruleMethod: 1, functionName: '', executionFile: 'DEV_OPInterface.dll', interfaceName: 'FRMBR_LOTCHECK', interfaceUrl: 'https://dev.example.invalid/lot-check', description: 'DEV 假資料：Manual 介面呼叫範例。', allowTimeLimitControl: 0, brProdType: 2, issueState: 2, creator: 'DEV001', createDate: '2026-10-24T10:20:00', reviser: 'DEV002', reviseDate: '2026-11-02T14:05:00', dataStamp: 3 },
  { brNo: 'DEV-BR-003', brName: 'DEV 首件情境條件', ruleType: 0, ruleMethod: 2, functionName: '', executionFile: '', interfaceName: 'DEV-SCN-002', interfaceUrl: '', description: 'DEV 假資料：DT 情境規則，選項只列核准情境。', allowTimeLimitControl: 0, brProdType: 1, issueState: 1, creator: 'DEV001', createDate: '2026-10-26T13:15:00', reviser: 'DEV002', reviseDate: '2026-11-04T09:00:00', dataStamp: 2 },
  { brNo: 'DEV-BR-004', brName: 'DEV 舊版包裝規則', ruleType: 1, ruleMethod: 0, functionName: 'FUNBR_VALIDATE_LOT', executionFile: 'DEV_OPBusinessRule.dll', interfaceName: '', interfaceUrl: '', description: 'DEV 假資料：不使用狀態範例。', allowTimeLimitControl: 1, brProdType: 3, issueState: -1, creator: 'DEV001', createDate: '2026-09-12T15:00:00', reviser: 'DEV002', reviseDate: '2026-10-01T08:30:00', dataStamp: 4 },
]
const rows = ref<BusinessRuleRow[]>([])
const options = ref<BusinessRuleOptions>(JSON.parse(JSON.stringify(seedOptions)) as BusinessRuleOptions)
const issueState = ref<IssueState>(0)
const keyword = ref('')
const selectedBrNos = ref<string[]>([])
const activeBrNo = ref('')
const loading = ref(false)
const busy = ref(false)
const error = ref('')
const notice = ref('')
const dialogMode = ref<FormMode>('')
const editing = ref<BusinessRuleRow | null>(null)
const draft = ref<Draft>(emptyDraft())
const visibleRows = computed(() => rows.value.filter((row) => row.issueState === issueState.value &&
  (!keyword.value.trim() || row.brNo.toLocaleUpperCase().includes(keyword.value.trim().toLocaleUpperCase()))))
const selectedRow = computed(() => visibleRows.value.find((row) => row.brNo === activeBrNo.value) ?? null)
const selectedRows = computed(() => visibleRows.value.filter((row) => selectedBrNos.value.includes(row.brNo)))
const dialogOpen = computed(() => dialogMode.value !== '')
const functionExecutionFile = computed(() => options.value.functions.find((item) => item.functionName === draft.value.functionName)?.executionFile ?? '')
const interfaceExecutionFile = computed(() => options.value.interfaces.find((item) => item.interfaceName === draft.value.interfaceName)?.executionFile ?? '')

function emptyDraft(): Draft { return { brNo: '', brName: '', ruleType: null, ruleMethod: null, functionName: '', interfaceName: '', interfaceUrl: '', scenarioNo: '', description: '', allowTimeLimitControl: 0, brProdType: null } }
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function displayDate(value: string): string {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return new Intl.DateTimeFormat('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23' }).format(date).replaceAll('-', '/')
}
function persistMock(): void { localStorage.setItem(storageKey, JSON.stringify(rows.value)) }
function loadMock(): void {
  try {
    const saved = localStorage.getItem(storageKey)
    rows.value = saved ? JSON.parse(saved) as BusinessRuleRow[] : clone(seedRows)
    if (!saved) persistMock()
  } catch { rows.value = clone(seedRows) }
}
function apiUrl(path: string): string {
  if (!props.apiBaseUrl.trim()) throw new Error('目前尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(props.apiBaseUrl.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init, headers: { 'Content-Type': 'application/json', ...init.headers } })
  const raw = await response.text()
  let body: unknown = null
  try { body = raw ? JSON.parse(raw) : null } catch { body = raw }
  if (!response.ok) {
    const envelope = body as { message?: string; title?: string } | null
    throw new Error(envelope && typeof envelope === 'object' ? envelope.message ?? envelope.title ?? 'API 回應 ' + response.status : String(body || 'API 回應 ' + response.status))
  }
  return body as T
}
function pick(row: Record<string, unknown>, camel: string, upper: string): unknown { return row[camel] ?? row[upper] ?? row[upper.toLocaleLowerCase()] }
function normalizeRow(row: Record<string, unknown>): BusinessRuleRow {
  const numberValue = (camel: string, upper: string): number | null => {
    const value = pick(row, camel, upper)
    return value === null || value === undefined || value === '' ? null : Number(value)
  }
  return {
    brNo: String(pick(row, 'brNo', 'BRNO') ?? ''), brName: String(pick(row, 'brName', 'BRNAME') ?? ''),
    ruleType: numberValue('ruleType', 'RULETYPE'), ruleMethod: numberValue('ruleMethod', 'RULEMETHOD'),
    functionName: String(pick(row, 'functionName', 'FUNCTIONNAME') ?? ''), executionFile: String(pick(row, 'executionFile', 'EXECUTIONFILE') ?? ''),
    interfaceName: String(pick(row, 'interfaceName', 'INTERFACENAME') ?? ''), interfaceUrl: String(pick(row, 'interfaceUrl', 'INTERFACEURL') ?? ''),
    description: String(pick(row, 'description', 'DESCRIPTION') ?? ''), allowTimeLimitControl: numberValue('allowTimeLimitControl', 'ALLOWTIMELIMITCONTROL'),
    brProdType: numberValue('brProdType', 'BRPRODTYPE'), issueState: Number(pick(row, 'issueState', 'ISSUESTATE') ?? 0) as IssueState,
    creator: String(pick(row, 'creator', 'CREATOR') ?? ''), createDate: String(pick(row, 'createDate', 'CREATEDATE') ?? ''),
    reviser: String(pick(row, 'reviser', 'REVISER') ?? ''), reviseDate: String(pick(row, 'reviseDate', 'REVISEDATE') ?? ''),
    dataStamp: numberValue('dataStamp', 'DATASTAMP'),
  }
}
function normalizeOptions(value: unknown): BusinessRuleOptions {
  const raw = value as Record<string, unknown>
  const list = <T>(key: string): T[] => Array.isArray(raw?.[key]) ? raw[key] as T[] : []
  return {
    functions: list<BusinessRuleOptions['functions'][number]>('functions'),
    interfaces: list<BusinessRuleOptions['interfaces'][number]>('interfaces'),
    scenarios: list<BusinessRuleOptions['scenarios'][number]>('scenarios'),
    brProductTypes: list<ValueOption>('brProductTypes'), allowTimeLimitControl: list<ValueOption>('allowTimeLimitControl'),
  }
}
async function loadRows(): Promise<void> {
  loading.value = true; error.value = ''; notice.value = ''; selectedBrNos.value = []
  try {
    if (mock.value) loadMock()
    else {
      const query = new URLSearchParams({ issueState: 'all' })
      if (keyword.value.trim()) query.set('brNo', keyword.value.trim())
      const result = await api<BusinessRuleRow[] | Envelope<BusinessRuleRow>>('/api/operation-business-rules?' + query.toString())
      const optionResult = await api<BusinessRuleOptions>('/api/operation-business-rules/options')
      const list = Array.isArray(result) ? result : result.items ?? result.rows ?? result.data ?? []
      rows.value = list.map((row) => normalizeRow(row as unknown as Record<string, unknown>))
      options.value = normalizeOptions(optionResult)
    }
    if (activeBrNo.value && !rows.value.some((row) => row.brNo === activeBrNo.value)) activeBrNo.value = ''
    notice.value = '資料總筆數 ' + visibleRows.value.length
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入企業邏輯失敗。' }
  finally { loading.value = false }
}
function selectRow(row: BusinessRuleRow, event?: MouseEvent): void {
  if (event?.ctrlKey || event?.metaKey) {
    selectedBrNos.value = selectedBrNos.value.includes(row.brNo) ? selectedBrNos.value.filter((key) => key !== row.brNo) : [...selectedBrNos.value, row.brNo]
    activeBrNo.value = row.brNo
  } else { selectedBrNos.value = [row.brNo]; activeBrNo.value = row.brNo }
}
function openDialog(mode: Exclude<FormMode, ''>, row: BusinessRuleRow | null = null): void {
  error.value = ''; notice.value = ''; dialogMode.value = mode; editing.value = row ? clone(row) : null
  if (!row) draft.value = emptyDraft()
  else draft.value = {
    brNo: row.brNo, brName: row.brName, ruleType: row.ruleType, ruleMethod: row.ruleMethod,
    functionName: row.functionName, interfaceName: row.interfaceName, interfaceUrl: row.interfaceUrl,
    scenarioNo: row.ruleMethod === 2 ? row.interfaceName : '', description: row.description,
    allowTimeLimitControl: row.allowTimeLimitControl ?? 0, brProdType: row.brProdType,
  }
}
function closeDialog(): void { dialogMode.value = ''; editing.value = null; error.value = '' }
function handleRuleMethodChange(): void { draft.value.functionName = ''; draft.value.interfaceName = ''; draft.value.interfaceUrl = ''; draft.value.scenarioNo = '' }
function validateDraft(): boolean {
  draft.value.brNo = draft.value.brNo.trim().toUpperCase()
  if (!draft.value.brNo) { error.value = 'BR No 為必填欄位。'; return false }
  if (draft.value.brNo.length > 20) { error.value = 'BR No 最多 20 個字元。'; return false }
  if (!draft.value.brName.trim()) { error.value = 'BR Name 為必填欄位。'; return false }
  if (draft.value.brName.length > 50) { error.value = 'BR Name 最多 50 個字元。'; return false }
  if (draft.value.ruleType === null) { error.value = '請選擇 Rule Type。'; return false }
  if (draft.value.ruleMethod === null) { error.value = '請選擇 Rule Method。'; return false }
  if (draft.value.interfaceUrl.length > 100) { error.value = 'Interface URL 最多 100 個字元。'; return false }
  if (draft.value.description.length > 255) { error.value = 'Description 最多 255 個字元。'; return false }
  if (draft.value.ruleMethod === 0 && !draft.value.functionName) { error.value = 'Automatic 必須選擇 Function Name。'; return false }
  if (draft.value.ruleMethod === 1) {
    if (!draft.value.functionName && !draft.value.interfaceName && !draft.value.interfaceUrl.trim()) { error.value = 'Manual 請指定 Function Name、Interface Name 或 Interface URL。'; return false }
    if (draft.value.functionName && (draft.value.interfaceName || draft.value.interfaceUrl.trim())) { error.value = 'Manual 的 Function Name 與 Interface/URL 僅能擇一。'; return false }
  }
  if (draft.value.ruleMethod === 2 && !draft.value.scenarioNo) { error.value = 'DT 必須選擇核准的 Scenario No。'; return false }
  return true
}
function buildPayload(): Record<string, unknown> {
  const payload: Record<string, unknown> = {
    brNo: draft.value.brNo, brName: draft.value.brName.trim(), ruleType: draft.value.ruleType,
    ruleMethod: draft.value.ruleMethod, description: draft.value.description, allowTimeLimitControl: draft.value.allowTimeLimitControl,
  }
  if (draft.value.brProdType !== null) payload.brProdType = draft.value.brProdType
  if (draft.value.ruleMethod === 0) {
    payload.functionName = draft.value.functionName; payload.executionFile = functionExecutionFile.value
    payload.interfaceName = ''; payload.interfaceUrl = ''
  } else if (draft.value.ruleMethod === 1) {
    if (draft.value.functionName) {
      payload.functionName = draft.value.functionName; payload.executionFile = functionExecutionFile.value
      payload.interfaceName = ''; payload.interfaceUrl = ''
    } else {
      payload.functionName = ''; payload.interfaceName = draft.value.interfaceName
      payload.executionFile = interfaceExecutionFile.value; payload.interfaceUrl = draft.value.interfaceUrl
    }
  } else { payload.functionName = ''; payload.interfaceName = draft.value.scenarioNo; payload.interfaceUrl = '' }
  return payload
}
async function saveForm(closeAfterSave: boolean): Promise<void> {
  if (!validateDraft()) return
  busy.value = true; error.value = ''; notice.value = ''
  const mode = dialogMode.value; const payload = buildPayload(); const now = new Date().toISOString()
  try {
    if (mock.value) {
      if (mode === 'edit' && editing.value) {
        const current = rows.value.find((row) => row.brNo === editing.value?.brNo)
        if (!current || current.issueState !== 0) throw new Error('資料狀態已變更或已不存在，請重新查詢。')
        if (current.dataStamp !== editing.value.dataStamp) throw new Error('資料已被其他人修改，請重新載入後再編輯。')
        const updated = { ...current, ...payload, brNo: current.brNo, issueState: current.issueState, dataStamp: (current.dataStamp ?? 0) + 1, reviser: props.currentUserNo || 'DEVUSER', reviseDate: now } as BusinessRuleRow
        rows.value = rows.value.map((item) => item.brNo === current.brNo ? updated : item); editing.value = clone(updated)
      } else {
        if (rows.value.some((row) => row.brNo.toLocaleUpperCase() === draft.value.brNo)) throw new Error('BR No 已存在，請輸入其他編號。')
        const added = { ...payload, brNo: draft.value.brNo, creator: props.currentUserNo || 'DEVUSER', createDate: now, reviser: '', reviseDate: '', issueState: 0 as IssueState, dataStamp: 0, executionFile: String(payload.executionFile ?? '') } as BusinessRuleRow
        rows.value.unshift(added)
      }
      persistMock()
    } else if (mode === 'edit' && editing.value) {
      delete payload.brNo; payload.dataStamp = editing.value.dataStamp
      await api('/api/operation-business-rules/' + encodeURIComponent(editing.value.brNo), { method: 'PUT', body: JSON.stringify(payload) })
    } else await api('/api/operation-business-rules', { method: 'POST', body: JSON.stringify(payload) })
    await loadRows()
    if (rows.value.some((row) => row.brNo === draft.value.brNo)) { activeBrNo.value = draft.value.brNo; selectedBrNos.value = [draft.value.brNo] }
    notice.value = closeAfterSave ? '企業邏輯已儲存。' : '已套用；定義視窗保持開啟。'
    if (closeAfterSave) closeDialog()
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '儲存企業邏輯失敗。' }
  finally { busy.value = false }
}
async function deleteSelected(): Promise<void> {
  const targets = selectedRows.value.length ? [...selectedRows.value] : selectedRow.value ? [selectedRow.value] : []
  if (!targets.length) { notice.value = '請先選取企業邏輯。'; return }
  if (targets.some((row) => row.issueState === 1)) { error.value = '簽核中資料不可刪除。'; return }
  const hardCount = targets.filter((row) => row.issueState === 0 || row.issueState === -1).length
  const softCount = targets.length - hardCount
  if (!window.confirm('確定處理選取的 ' + targets.length + ' 筆資料？未簽核/不使用會實體刪除，已簽核會改為不使用。')) return
  busy.value = true; error.value = ''; notice.value = ''
  try {
    for (const row of targets) {
      if (mock.value) {
        if (row.issueState === 2) rows.value = rows.value.map((item) => item.brNo === row.brNo ? { ...item, issueState: -1, dataStamp: (item.dataStamp ?? 0) + 1, reviser: props.currentUserNo || 'DEVUSER', reviseDate: new Date().toISOString() } : item)
        else rows.value = rows.value.filter((item) => item.brNo !== row.brNo)
      } else await api('/api/operation-business-rules/' + encodeURIComponent(row.brNo), { method: 'DELETE', body: JSON.stringify({ issueState: row.issueState, dataStamp: row.dataStamp }) })
    }
    if (mock.value) persistMock()
    await loadRows(); notice.value = '已完成 ' + hardCount + ' 筆實體刪除與 ' + softCount + ' 筆停用。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '刪除企業邏輯失敗；前面已成功處理的資料不會自動復原。' }
  finally { busy.value = false }
}
async function approveSelected(): Promise<void> {
  const targets = selectedRows.value.length ? [...selectedRows.value] : selectedRow.value ? [selectedRow.value] : []
  if (!targets.length) { notice.value = '請先選取企業邏輯。'; return }
  if (targets.some((row) => row.issueState !== 0)) { error.value = '只能核准未簽核資料。'; return }
  if (!window.confirm('確定送出 ' + targets.length + ' 筆企業邏輯核准？')) return
  busy.value = true; error.value = ''; notice.value = ''
  try {
    for (const row of targets) {
      if (mock.value) rows.value = rows.value.map((item) => item.brNo === row.brNo ? { ...item, issueState: 2, dataStamp: (item.dataStamp ?? 0) + 1, reviser: props.currentUserNo || 'DEVUSER', reviseDate: new Date().toISOString() } : item)
      else await api('/api/operation-business-rules/' + encodeURIComponent(row.brNo) + '/approve', { method: 'POST', body: JSON.stringify({ issueState: row.issueState, dataStamp: row.dataStamp }) })
    }
    if (mock.value) persistMock()
    await loadRows(); notice.value = '已送出 ' + targets.length + ' 筆核准。正式環境會依簽核設定轉為簽核中或已簽核。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '核准企業邏輯失敗；前面已成功送出的資料不會自動復原。' }
  finally { busy.value = false }
}
function optionLabel(value: number | null, list: ValueOption[]): string { return value === null ? '' : list.find((item) => item.code === value)?.displayName ?? String(value) }
onMounted(() => { void loadRows() })
</script>

<template>
  <div class="business-rule-page">
    <header class="page-title"><h1>企業邏輯設定</h1><span>Business Rule Basis</span></header>
    <section class="query-panel" aria-label="查詢條件">
      <fieldset class="state-filter"><legend>簽核狀態</legend>
        <label v-for="state in issueStates" :key="state.value"><input v-model="issueState" type="radio" :value="state.value" @change="selectedBrNos = []; activeBrNo = ''" />{{ state.label }}</label>
      </fieldset>
      <label class="search-field"><span>BR No</span><input v-model="keyword" class="kd-field" maxlength="20" @keydown.enter="loadRows" /></label>
      <span class="wildcard">%</span><button class="button" :disabled="loading || busy" @click="loadRows">查詢</button>
    </section>
    <p v-if="mock" class="dev-note">DEV 假資料預覽：資料僅儲存在本機瀏覽器 localStorage；函式、介面、情境與產品類型均為示意。</p>
    <p v-if="error" class="message error-message" role="alert">{{ error }}</p><p v-else-if="notice && !dialogOpen" class="message" role="status">{{ notice }}</p>
    <main class="master-screen">
      <section class="grid-panel" aria-label="企業邏輯清單">
        <div class="table-scroll"><table class="data-grid">
          <thead><tr><th>BR No</th><th>BR Name</th><th>Rule Type</th><th>Rule Method</th><th>Function Name</th><th>Interface Name</th><th>Interface URL</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>AllowTimeLimitControl</th><th>BRProdType</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
          <tbody>
            <tr v-for="row in visibleRows" :key="row.brNo" :class="{ selected: selectedBrNos.includes(row.brNo) }" @click="selectRow(row, $event)">
              <td>{{ row.brNo }}</td><td>{{ row.brName }}</td><td>{{ row.ruleType === 0 ? 'System' : row.ruleType === 1 ? 'Customer' : '' }}</td>
              <td>{{ row.ruleMethod === 0 ? 'Automatic' : row.ruleMethod === 1 ? 'Manual' : row.ruleMethod === 2 ? 'DT' : '' }}</td>
              <td>{{ row.functionName }}</td><td>{{ row.interfaceName }}</td><td>{{ row.interfaceUrl }}</td><td class="description-cell">{{ row.description }}</td>
              <td>{{ row.creator }}</td><td>{{ displayDate(row.createDate) }}</td><td>{{ optionLabel(row.allowTimeLimitControl, options.allowTimeLimitControl) }}</td>
              <td>{{ optionLabel(row.brProdType, options.brProductTypes) }}</td><td>{{ row.reviser }}</td><td>{{ displayDate(row.reviseDate) }}</td>
            </tr>
            <tr v-if="!visibleRows.length"><td class="empty-cell" colspan="14">{{ loading ? '載入中…' : '沒有符合條件的資料。' }}</td></tr>
          </tbody>
        </table></div>
        <footer class="grid-footer">資料總筆數 {{ visibleRows.length }}<span v-if="selectedBrNos.length">；已選取 {{ selectedBrNos.length }} 筆</span></footer>
      </section>
      <aside class="action-panel" aria-label="功能">
        <button class="button primary" :disabled="busy" @click="openDialog('new')">ADD (A)</button>
        <button class="button" :disabled="busy || !selectedRow || selectedRow.issueState !== 0" @click="openDialog('edit', selectedRow)">EDIT (E)</button>
        <button class="button" :disabled="busy || (!selectedBrNos.length && !selectedRow)" @click="deleteSelected">DEL (D)</button>
        <button class="button" :disabled="busy || !selectedRow" @click="openDialog('copy', selectedRow)">Copy (C)</button>
        <button class="button" :disabled="busy || (!selectedBrNos.length && !selectedRow)" @click="approveSelected">Approve (Y)</button>
        <button class="button close-grid" @click="emit('close')">Close (X)</button>
      </aside>
    </main>

    <div v-if="dialogOpen" class="dialog-backdrop" @click.self="closeDialog">
      <section class="definition-dialog" role="dialog" aria-modal="true" aria-labelledby="business-rule-dialog-title">
        <h2 id="business-rule-dialog-title">Business Rule Basis Define ({{ dialogMode === 'new' ? 'ADD' : dialogMode === 'edit' ? 'EDIT' : 'Copy' }})</h2>
        <fieldset class="business-definition"><legend>Business Rule Basis Define</legend>
          <label class="form-row"><span>BR No</span><input v-model="draft.brNo" class="kd-field" maxlength="20" :readonly="dialogMode === 'edit'" @blur="draft.brNo = draft.brNo.trim().toUpperCase()" /></label>
          <label class="form-row"><span>BR Name</span><input v-model="draft.brName" class="kd-field" maxlength="50" /></label>
          <label class="form-row"><span>BR Prod Type</span><select v-model.number="draft.brProdType" class="kd-field" disabled><option :value="null"></option><option v-for="item in options.brProductTypes" :key="item.code" :value="item.code">{{ item.displayName }}</option></select></label>
          <div class="form-row"><span>Rule Type</span><div class="radio-box"><label><input v-model="draft.ruleType" type="radio" :value="0" /> System</label><label><input v-model="draft.ruleType" type="radio" :value="1" /> Customer</label></div></div>
          <div class="form-row"><span>Rule Method</span><div class="radio-box method-box"><label><input v-model="draft.ruleMethod" type="radio" :value="0" @change="handleRuleMethodChange" /> Automatic</label><label><input v-model="draft.ruleMethod" type="radio" :value="1" @change="handleRuleMethodChange" /> Manual</label><label><input v-model="draft.ruleMethod" type="radio" :value="2" @change="handleRuleMethodChange" /> DT</label></div></div>
          <label class="form-row"><span>Function Name</span><select v-model="draft.functionName" class="kd-field" :disabled="draft.ruleMethod === 2 || draft.ruleMethod === null"><option value=""></option><option v-for="item in options.functions" :key="item.functionName" :value="item.functionName">{{ item.functionName }}</option></select></label>
          <label class="form-row"><span>Interface Name</span><select v-model="draft.interfaceName" class="kd-field" :disabled="draft.ruleMethod !== 1"><option value=""></option><option v-for="item in options.interfaces" :key="item.interfaceName" :value="item.interfaceName">{{ item.interfaceName }}</option></select></label>
          <label class="form-row"><span>Interface URL</span><input v-model="draft.interfaceUrl" class="kd-field" maxlength="100" :readonly="draft.ruleMethod !== 1" /></label>
          <label class="form-row"><span>DT ScenarioNo</span><select v-model="draft.scenarioNo" class="kd-field" :disabled="draft.ruleMethod !== 2"><option value=""></option><option v-for="item in options.scenarios" :key="item.scenarioNo" :value="item.scenarioNo">{{ item.scenarioNo }} — {{ item.scenarioName }}</option></select></label>
          <div class="form-row"><span>AllowTimeLimitControl</span><div class="radio-box disabled-box"><label><input v-model.number="draft.allowTimeLimitControl" type="radio" :value="1" disabled /> Yes</label><label><input v-model.number="draft.allowTimeLimitControl" type="radio" :value="0" disabled /> No</label></div></div>
          <label class="form-row description-row"><span>Description</span><textarea v-model="draft.description" class="kd-field" maxlength="255"></textarea></label>
        </fieldset>
        <fieldset class="audit-group"><legend>建立/修改資訊</legend>
          <label><span>建立人</span><input :value="dialogMode === 'edit' ? editing?.creator : (currentUserNo || '儲存時由伺服器設定')" disabled /></label>
          <label><span>建立日期</span><input :value="dialogMode === 'edit' ? displayDate(editing?.createDate ?? '') : '儲存時由伺服器設定'" disabled /></label>
          <label v-if="dialogMode === 'edit'"><span>修改人</span><input :value="currentUserNo || '由伺服器設定'" disabled /></label>
          <label v-if="dialogMode === 'edit'"><span>修改日期</span><input :value="displayDate(new Date().toISOString())" disabled /></label>
        </fieldset>
        <footer class="dialog-actions"><button class="button primary" :disabled="busy" @click="saveForm(true)">確定</button><button class="button" :disabled="busy" @click="closeDialog">關閉</button><button class="button" :disabled="busy" @click="saveForm(false)">套用</button></footer>
        <p v-if="error" class="message error-message" role="alert">{{ error }}</p>
        <p v-else-if="notice" class="message" role="status">{{ notice }}</p>
      </section>
    </div>
  </div>
</template>

<style scoped>
.business-rule-page{min-height:100%;padding:0 5px 12px;color:#353535;font:14px Arial,"Microsoft JhengHei",sans-serif}.page-title{display:flex;align-items:center;gap:14px;min-height:58px;padding:0 24px;border-bottom:1px solid #dedbd3;background:#f7f5ef}.page-title h1{margin:0;color:#342d22;font-size:21px;font-weight:600}.page-title>span{color:#827b70;font-size:12px}.query-panel{display:flex;align-items:center;gap:8px;min-height:48px;margin-top:8px;padding:5px 10px;border:1px solid #d2d2d2;background:#fff}.state-filter{display:flex;align-items:center;gap:12px;margin:0;padding:2px 7px;border:0}.state-filter legend{position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0)}.state-filter label,.search-field{display:flex;align-items:center;gap:5px;white-space:nowrap}.state-filter input,.radio-box input{accent-color:#e89a1c}.search-field{margin-left:8px}.kd-field{height:29px;border:1px solid #929292;border-radius:2px;padding:4px 7px;background:#fff0b0;color:#303030;font:inherit}.kd-field:focus{outline:2px solid #e0a031;outline-offset:1px}.search-field .kd-field{width:190px}.wildcard{margin-left:-5px}.button{min-height:31px;padding:4px 12px;border:1px solid #bbb;border-radius:4px;background:linear-gradient(#fff,#f3f3f3);color:#333;cursor:pointer;font:inherit;white-space:nowrap}.button:hover:not(:disabled){border-color:#dd9b2d;background:#fff8e7}.button:disabled{color:#aaa;cursor:not-allowed}.button.primary{border-color:#f1a32c;background:#ff9f1c;color:#fff}.message{margin:6px 0 0;padding:7px 9px;color:#62522e;background:#fff9e8}.error-message{color:#8c251e;background:#fff0ed}.dev-note{margin:6px 0 0;padding:6px 9px;color:#695a35;background:#fff9e8;font-size:12px}.master-screen{display:grid;grid-template-columns:minmax(0,1fr) 96px;gap:6px;min-height:calc(100vh - 190px);margin-top:7px;border:1px solid #ccc;padding:6px;background:#fff}.grid-panel{display:flex;min-height:calc(100vh - 204px);min-width:0;flex-direction:column}.action-panel{display:flex;flex-direction:column;gap:8px;padding-left:2px}.action-panel .button{width:88px}.table-scroll{flex:1;min-height:300px;overflow:auto;border:1px solid #d0d0d0}.data-grid{width:100%;border-collapse:collapse;white-space:nowrap}.data-grid th,.data-grid td{padding:5px 7px;border:1px solid #d0d0d0;text-align:left}.data-grid th{position:sticky;top:0;z-index:1;background:#e7e9eb;color:#383838;font-weight:600}.data-grid tbody tr{cursor:pointer}.data-grid tbody tr:hover{background:#fff8e7}.data-grid tbody tr.selected{background:#ffecb4}.data-grid td.description-cell{max-width:220px;overflow:hidden;text-overflow:ellipsis}.empty-cell{padding:22px!important;color:#777;text-align:center!important}.grid-footer{padding:6px 2px 0;color:#666;font-size:12px}.close-grid{margin-top:auto}.dialog-backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:12px;background:#17161166;overflow:auto}.definition-dialog{width:min(490px,96vw);max-height:96vh;overflow:auto;padding:8px 10px 10px;border:1px solid #aaa;background:#fff;box-shadow:0 12px 32px #0003}.definition-dialog h2{margin:0 0 4px;font-size:16px;font-weight:600}.business-definition{min-height:451px;margin:0;padding:4px 10px 7px;border:1px solid #bcbcbc}.business-definition legend,.audit-group legend{padding:0 5px}.form-row{display:grid;grid-template-columns:120px minmax(0,1fr);align-items:center;gap:8px;margin:6px 0}.form-row>span{text-align:right}.form-row>.kd-field{width:100%;min-width:0}.radio-box{display:flex;align-items:center;gap:13px;min-height:29px;padding:2px 6px;border:1px solid #bbb}.radio-box label{display:flex;align-items:center;gap:3px;white-space:nowrap}.method-box{gap:7px;padding-left:4px;padding-right:4px}.disabled-box{background:#f2f2f2;color:#777}.description-row{align-items:start}.description-row>span{padding-top:6px}.description-row textarea{height:66px;resize:vertical}.audit-group{display:grid;grid-template-columns:1fr 1fr;gap:5px 8px;margin:6px 0 0;padding:6px;border:1px solid #bcbcbc}.audit-group label{display:grid;grid-template-columns:58px minmax(0,1fr);align-items:center;gap:4px;font-size:12px}.audit-group label>span{text-align:right}.audit-group input{width:100%;height:24px;border:1px solid #bbb;padding:3px 5px;background:#eee;color:#666;font:inherit}.dialog-actions{display:flex;gap:6px;margin-top:7px}.dialog-actions .primary{margin-left:auto}@media(max-width:760px){.query-panel{flex-wrap:wrap}.state-filter{flex-wrap:wrap}.master-screen{grid-template-columns:1fr;min-height:65vh}.grid-panel{min-height:55vh}.action-panel{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));padding:0}.action-panel .button{width:100%}.close-grid{margin:0}.audit-group{grid-template-columns:1fr}}
</style>
