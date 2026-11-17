<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type EditorMode = 'add' | 'edit' | 'copy'
interface FactorDef { factorNo: string; factorName: string; parameterType: number; options: string[] }
interface QcItem { qcItemNo: string; qcItemName: string; creator: string; createDate: string; factors?: FactorDef[] }
interface RuleOption { ruleNo: string; ruleName: string }
interface RuleSelection { ruleNo: string; action: string }
interface LimitRow {
  qcItemNo: string; factorValues: Record<string, string>; us: string; ls: string; ucl: string; lcl: string; cl: string
  ruleSet: string; creator: string; createDate: string; reviser: string; reviseDate: string; dataStamp: number | null
}
interface MockStore { items: QcItem[]; limits: LimitRow[]; rules: RuleOption[] }
interface EditorForm { us: string; ls: string; ucl: string; lcl: string; cl: string }
interface ListPayload<T> { items?: T[]; rows?: T[]; data?: T[] }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const basePath = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const mockKey = 'ymes-dev-mock:qc-spc-att-limit:v1'
const items = ref<QcItem[]>([])
const limits = ref<LimitRow[]>([])
const ruleOptions = ref<RuleOption[]>([])
const factorOptionsByItem = ref<Record<string, FactorDef[]>>({})
const activeQcItemNo = ref('')
const activeLimitId = ref('')
const selectedLimitIds = ref<string[]>([])
const loading = ref(false)
const saving = ref(false)
const errorMessage = ref('')
const notice = ref('')
const editorOpen = ref(false)
const editorMode = ref<EditorMode>('add')
const editorError = ref('')
const editorForm = ref<EditorForm>(blankForm())
const editorFactorValues = ref<Record<string, string>>({})
const editorRules = ref<Array<RuleOption & { selected: boolean; action: string }>>([])
const originalFactorValues = ref<Record<string, string>>({})
const editingDataStamp = ref<number | null>(0)

function blankForm(): EditorForm { return { us: '', ls: '', ucl: '', lcl: '', cl: '' } }
function demoStore(): MockStore {
  const commonFactors: FactorDef[] = [
    { factorNo: 'MO', factorName: '工單', parameterType: 1, options: ['DEV-MO-001', 'DEV-MO-002'] },
    { factorNo: 'PRODUCT', factorName: '產品', parameterType: 1, options: ['DEV-PRD-001', 'DEV-PRD-002'] },
    { factorNo: 'OP', factorName: '作業站', parameterType: 1, options: ['DEV-OP-PACK', 'DEV-OP-TEST'] },
  ]
  const items: QcItem[] = [
    { qcItemNo: 'DEV-QC202-001', qcItemName: '外觀缺陷計數（DEMO）', creator: 'DEV001', createDate: '2026/11/17 08:00:00', factors: commonFactors },
    { qcItemNo: 'DEV-QC202-002', qcItemName: '端子漏件計數（DEMO）', creator: 'DEV001', createDate: '2026/11/16 15:30:00', factors: [
      { factorNo: 'EQUIPMENT', factorName: '設備', parameterType: 1, options: ['DEV-EQP-01', 'DEV-EQP-02'] },
      { factorNo: 'EMPLOYEE', factorName: '作業人員', parameterType: 2, options: [] },
    ] },
  ]
  const rules: RuleOption[] = [
    { ruleNo: 'DEV-AR-01', ruleName: '連續 7 點落在中心線同側（DEMO）' },
    { ruleNo: 'DEV-AR-02', ruleName: '連續 6 點遞增或遞減（DEMO）' },
    { ruleNo: 'DEV-AR-03', ruleName: '連續 14 點交替上升下降（DEMO）' },
  ]
  const limits: LimitRow[] = [
    { qcItemNo: items[0].qcItemNo, factorValues: { MO: 'DEV-MO-001', PRODUCT: 'DEV-PRD-001', OP: 'DEV-OP-PACK' }, us: '10', ls: '0', ucl: '8', lcl: '1', cl: '4', ruleSet: 'DEV-AR-01,Warning;DEV-AR-03,Hold', creator: 'DEV001', createDate: '2026/11/17 08:10:00', reviser: 'DEV001', reviseDate: '2026/11/17 09:00:00', dataStamp: 2 },
    { qcItemNo: items[0].qcItemNo, factorValues: { MO: '*', PRODUCT: 'DEV-PRD-002', OP: 'DEV-OP-TEST' }, us: '12', ls: '0', ucl: '9', lcl: '2', cl: '5', ruleSet: 'DEV-AR-02,Alert', creator: 'DEV001', createDate: '2026/11/17 08:20:00', reviser: '', reviseDate: '', dataStamp: 0 },
    { qcItemNo: items[1].qcItemNo, factorValues: { EQUIPMENT: 'DEV-EQP-01', EMPLOYEE: '*' }, us: '5', ls: '0', ucl: '4', lcl: '1', cl: '2', ruleSet: 'DEV-AR-01,Warning', creator: 'DEV001', createDate: '2026/11/16 15:45:00', reviser: '', reviseDate: '', dataStamp: 0 },
  ]
  return { items, limits, rules }
}
function readStore(): MockStore {
  try {
    const raw = window.localStorage.getItem(mockKey)
    if (raw) {
      const parsed = JSON.parse(raw) as MockStore
      if (Array.isArray(parsed.items) && Array.isArray(parsed.limits) && Array.isArray(parsed.rules)) return parsed
    }
  } catch { /* initialize isolated DEV records */ }
  const value = demoStore()
  saveStore(value)
  return value
}
function saveStore(value: MockStore): void { window.localStorage.setItem(mockKey, JSON.stringify(value)) }
function listOf<T>(payload: T[] | ListPayload<T>): T[] {
  if (Array.isArray(payload)) return payload
  return payload.items ?? payload.rows ?? payload.data ?? []
}
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init, headers })
  if (response.status === 401) { window.location.assign(basePath + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; title?: string; message?: string } | null
    throw new Error(problem?.detail || problem?.message || problem?.title || 'API 呼叫失敗（' + response.status + '）。')
  }
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}
function normalizeItem(value: Record<string, unknown>): QcItem {
  return {
    qcItemNo: String(value.qcItemNo ?? value.QCITEMNO ?? ''),
    qcItemName: String(value.qcItemName ?? value.QCITEMNAME ?? ''),
    creator: String(value.creator ?? value.CREATOR ?? ''),
    createDate: String(value.createDate ?? value.CREATEDATE ?? ''),
  }
}
function normalizeFactor(value: Record<string, unknown>): FactorDef {
  return {
    factorNo: String(value.factorNo ?? value.FACTORNO ?? ''),
    factorName: String(value.factorName ?? value.FACTORNAME ?? value.parameterName ?? ''),
    parameterType: Number(value.parameterType ?? value.PARAMETERTYPE ?? 0),
    options: Array.isArray(value.options) ? value.options.map(String) : [],
  }
}
function normalizeLimit(value: Record<string, unknown>): LimitRow {
  const values = value.factorValues ?? value.FACTORVALUES
  return {
    qcItemNo: String(value.qcItemNo ?? value.QCITEMNO ?? ''),
    factorValues: values && typeof values === 'object' ? Object.fromEntries(Object.entries(values as Record<string, unknown>).map(([key, entry]) => [key, String(entry ?? '*')])) : {},
    us: String(value.us ?? value.US ?? ''), ls: String(value.ls ?? value.LS ?? ''),
    ucl: String(value.ucl ?? value.UCL ?? ''), lcl: String(value.lcl ?? value.LCL ?? ''), cl: String(value.cl ?? value.CL ?? ''),
    ruleSet: String(value.ruleSet ?? value.RULESET ?? ''),
    creator: String(value.creator ?? value.CREATOR ?? ''), createDate: String(value.createDate ?? value.CREATEDATE ?? ''),
    reviser: String(value.reviser ?? value.REVISER ?? ''), reviseDate: String(value.reviseDate ?? value.REVISEDATE ?? ''),
    dataStamp: value.dataStamp == null && value.DATASTAMP == null ? null : Number(value.dataStamp ?? value.DATASTAMP),
  }
}
function rowId(row: LimitRow): string {
  return row.qcItemNo + '|' + JSON.stringify(Object.entries(row.factorValues).sort(([a], [b]) => a.localeCompare(b)))
}
const activeItem = computed(() => items.value.find(row => row.qcItemNo === activeQcItemNo.value) ?? null)
const activeFactors = computed(() => factorOptionsByItem.value[activeQcItemNo.value] ?? activeItem.value?.factors ?? [])
const currentLimits = computed(() => limits.value.filter(row => row.qcItemNo === activeQcItemNo.value))
const activeLimit = computed(() => currentLimits.value.find(row => rowId(row) === activeLimitId.value) ?? null)
const parsedRules = computed(() => activeLimit.value ? parseRuleSet(activeLimit.value.ruleSet) : [])
function parseRuleSet(value: string): RuleSelection[] {
  return value.split(';').map(part => part.split(',')).filter(parts => parts.length >= 2 && parts[0]).map(parts => ({ ruleNo: parts[0], action: parts[1] }))
}
function formatDate(value: string): string { return value ? value.replace('T', ' ').slice(0, 19) : '' }
function nowText(): string { return new Date().toLocaleString('sv-SE').replace('T', ' ') }
function factorLabel(row: LimitRow, factorNo: string): string { return row.factorValues[factorNo] ?? '*' }
function selectQcItem(item: QcItem): void {
  activeQcItemNo.value = item.qcItemNo
  activeLimitId.value = ''
  selectedLimitIds.value = []
  void loadDetails(item.qcItemNo)
}
async function loadDetails(qcItemNo: string): Promise<void> {
  if (!qcItemNo) return
  loading.value = true
  errorMessage.value = ''
  try {
    if (mock.value) {
      const store = readStore()
      factorOptionsByItem.value[qcItemNo] = store.items.find(row => row.qcItemNo === qcItemNo)?.factors ?? []
      limits.value = store.limits
      ruleOptions.value = store.rules
    } else {
      const encoded = encodeURIComponent(qcItemNo)
      const [factorPayload, limitPayload, optionPayload] = await Promise.all([
        request<ListPayload<Record<string, unknown>>>('api/qc/attribute-qc-items/' + encoded + '/factors?factorOnline=Y'),
        request<ListPayload<Record<string, unknown>>>('api/qc/attribute-control-limits?qcItemNo=' + encoded),
        request<{ rules?: Array<Record<string, unknown>>; factors?: Array<Record<string, unknown>>; factorOptions?: Record<string, string[]> }>('api/qc/attribute-control-limits/options?qcItemNo=' + encoded),
      ])
      factorOptionsByItem.value[qcItemNo] = listOf(factorPayload).map(normalizeFactor)
      const options = optionPayload.factors ?? []
      const byNo = new Map(options.map(option => [String(option.factorNo ?? option.FACTORNO ?? ''), option]))
      factorOptionsByItem.value[qcItemNo] = factorOptionsByItem.value[qcItemNo].map(factor => ({
        ...factor,
        options: optionPayload.factorOptions?.[factor.factorNo] ?? (byNo.get(factor.factorNo)?.options as string[] | undefined) ?? [],
      }))
      limits.value = listOf(limitPayload).map(normalizeLimit)
      ruleOptions.value = (optionPayload.rules ?? []).map(row => ({ ruleNo: String(row.ruleNo ?? row.RULENO ?? ''), ruleName: String(row.ruleName ?? row.RULENAME ?? '') }))
    }
    const firstRow = limits.value.filter(row => row.qcItemNo === qcItemNo)[0]
    activeLimitId.value = firstRow ? rowId(firstRow) : ''
    selectedLimitIds.value = firstRow ? [rowId(firstRow)] : []
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '載入計數管制內容失敗。' }
  finally { loading.value = false }
}
function selectLimit(row: LimitRow, event: MouseEvent): void {
  const id = rowId(row)
  activeLimitId.value = id
  if (event.ctrlKey || event.metaKey) selectedLimitIds.value = selectedLimitIds.value.includes(id) ? selectedLimitIds.value.filter(value => value !== id) : [...selectedLimitIds.value, id]
  else if (!selectedLimitIds.value.includes(id)) selectedLimitIds.value = [id]
}
function toggleLimit(row: LimitRow, checked: boolean): void {
  const id = rowId(row)
  selectedLimitIds.value = checked ? [...new Set([...selectedLimitIds.value, id])] : selectedLimitIds.value.filter(value => value !== id)
  activeLimitId.value = id
}
function openEditor(mode: EditorMode): void {
  errorMessage.value = ''
  editorError.value = ''
  editorMode.value = mode
  const source = mode === 'add' ? null : activeLimit.value
  if (mode !== 'add' && !source) { errorMessage.value = '請先選取一筆管制內容。'; return }
  if (!activeItem.value) { errorMessage.value = '請先選取一個已核准的計數品管項目。'; return }
  editorForm.value = source ? { us: source.us, ls: source.ls, ucl: source.ucl, lcl: source.lcl, cl: source.cl } : blankForm()
  originalFactorValues.value = source ? { ...source.factorValues } : {}
  editorFactorValues.value = source ? { ...source.factorValues } : Object.fromEntries(activeFactors.value.map(factor => [factor.factorNo, '']))
  const selected = new Map(source ? parseRuleSet(source.ruleSet).map(rule => [rule.ruleNo, rule.action]) : [])
  editorRules.value = ruleOptions.value.map(rule => ({ ...rule, selected: selected.has(rule.ruleNo), action: selected.get(rule.ruleNo) ?? 'Warning' }))
  editingDataStamp.value = source ? source.dataStamp : 0
  editorOpen.value = true
}
const editorTitle = computed(() => ({ add: '新增管制內容', edit: '修改管制內容', copy: '複製管制內容' }[editorMode.value]))
const editorAudit = computed(() => ({
  creator: editorMode.value === 'edit' ? activeLimit.value?.creator ?? '' : props.currentUserNo || (mock.value ? 'DEV001' : ''),
  createDate: editorMode.value === 'edit' ? formatDate(activeLimit.value?.createDate ?? '') : '儲存時建立',
  reviser: editorMode.value === 'edit' ? props.currentUserNo || (mock.value ? 'DEV001' : '') : '',
  reviseDate: editorMode.value === 'edit' ? '儲存時更新' : '',
}))
function factorValueOptions(factor: FactorDef): string[] { return ['*', ...factor.options.filter(value => value !== '*')] }
function selectedRulesText(): string {
  return editorRules.value.filter(rule => rule.selected).map(rule => rule.ruleNo + ',' + rule.action).join(';')
}
function normalizedFactorValues(): Record<string, string> {
  return Object.fromEntries(activeFactors.value.map(factor => [factor.factorNo, (editorFactorValues.value[factor.factorNo] ?? '').trim() || '*']))
}
function validateEditor(): string {
  if (!editorForm.value.ucl.trim()) return 'UCL 為必填欄位。'
  for (const field of ['us', 'ls', 'ucl', 'lcl', 'cl'] as const) {
    const value = editorForm.value[field].trim()
    if (value && !Number.isFinite(Number(value))) return field.toUpperCase() + ' 必須是數字。'
  }
  if (!activeFactors.value.length) return '此 QC Item 沒有 FactorOnline=Y 的要因，無法設定管制內容。'
  const order = ['us', 'ucl', 'cl', 'lcl', 'ls'] as const
  const filled = order.map(field => ({ field, value: editorForm.value[field].trim() })).filter(row => row.value)
  for (let index = 1; index < filled.length; index++) {
    if (Number(filled[index - 1].value) < Number(filled[index].value)) return '界限順序需符合 US ≥ UCL ≥ CL ≥ LCL ≥ LS。'
  }
  const us = editorForm.value.us.trim()
  const ls = editorForm.value.ls.trim()
  if (us && ls && Number(us) <= Number(ls)) return 'US 必須大於 LS。'
  return ''
}
function hasDuplicate(values: Record<string, string>, excludingId = ''): boolean {
  const target = JSON.stringify(Object.entries(values).sort(([a], [b]) => a.localeCompare(b)))
  return currentLimits.value.some(row => rowId(row) !== excludingId && JSON.stringify(Object.entries(row.factorValues).sort(([a], [b]) => a.localeCompare(b))) === target)
}
async function saveEditor(closeAfterSave: boolean): Promise<void> {
  editorError.value = ''
  const validation = validateEditor()
  if (validation) { editorError.value = validation; return }
  const factorValues = normalizedFactorValues()
  if (editorMode.value !== 'edit' && hasDuplicate(factorValues)) { editorError.value = '相同 QCItemNo 與要因組合已存在。'; return }
  saving.value = true
  try {
    const payload: Record<string, unknown> = {
      qcItemNo: activeQcItemNo.value,
      ...editorForm.value,
      ruleSet: selectedRulesText(),
    }
    if (editorMode.value === 'edit') {
      payload.originalFactorValues = originalFactorValues.value
      payload.dataStamp = editingDataStamp.value
    } else {
      payload.factorValues = factorValues
    }
    if (mock.value) {
      const store = readStore()
      if (editorMode.value === 'edit') {
        const target = store.limits.find(row => row.qcItemNo === activeQcItemNo.value && rowId(row) === activeLimitId.value)
        if (!target) throw new Error('資料已不存在，請重新載入。')
        if (target.dataStamp !== editingDataStamp.value) throw new Error('資料已被其他人修改，請重新載入後再編輯。')
        Object.assign(target, editorForm.value, { ruleSet: selectedRulesText(), reviser: props.currentUserNo || 'DEV001', reviseDate: nowText(), dataStamp: (target.dataStamp ?? 0) + 1 })
      } else {
        store.limits.push({
          qcItemNo: activeQcItemNo.value, factorValues, ...editorForm.value, ruleSet: selectedRulesText(),
          creator: props.currentUserNo || 'DEV001', createDate: nowText(), reviser: '', reviseDate: '', dataStamp: 0,
        })
      }
      saveStore(store)
      limits.value = store.limits
      const saved = editorMode.value === 'edit' ? store.limits.find(row => row.qcItemNo === activeQcItemNo.value && rowId(row) === activeLimitId.value) : store.limits[store.limits.length - 1]
      if (saved) {
        activeLimitId.value = rowId(saved)
        selectedLimitIds.value = [activeLimitId.value]
        originalFactorValues.value = { ...saved.factorValues }
        editingDataStamp.value = saved.dataStamp
      }
    } else if (editorMode.value === 'edit') {
      await request('api/qc/attribute-control-limits/' + encodeURIComponent(activeQcItemNo.value), { method: 'PUT', body: JSON.stringify(payload) })
      await loadDetails(activeQcItemNo.value)
    } else {
      await request('api/qc/attribute-control-limits', { method: 'POST', body: JSON.stringify(payload) })
      await loadDetails(activeQcItemNo.value)
    }
    if (!mock.value) {
      const targetValues = editorMode.value === 'edit' ? originalFactorValues.value : factorValues
      const targetValuesJson = JSON.stringify(Object.entries(targetValues).sort(([a], [b]) => a.localeCompare(b)))
      const saved = limits.value.find(row => row.qcItemNo === activeQcItemNo.value &&
        JSON.stringify(Object.entries(row.factorValues).sort(([a], [b]) => a.localeCompare(b))) === targetValuesJson)
      if (saved) {
        activeLimitId.value = rowId(saved)
        selectedLimitIds.value = [activeLimitId.value]
        originalFactorValues.value = { ...saved.factorValues }
        editingDataStamp.value = saved.dataStamp
      }
    }
    notice.value = editorMode.value === 'edit' ? '管制內容已修改。' : '管制內容已新增。'
    if (closeAfterSave) editorOpen.value = false
  } catch (error) { editorError.value = error instanceof Error ? error.message : '儲存失敗。' }
  finally { saving.value = false }
}
async function deleteSelected(): Promise<void> {
  const selected = currentLimits.value.filter(row => selectedLimitIds.value.includes(rowId(row)))
  if (!selected.length) { errorMessage.value = '請先選取要刪除的管制內容。'; return }
  if (!window.confirm('確定刪除選取的 ' + selected.length + ' 筆管制內容？')) return
  saving.value = true
  errorMessage.value = ''
  let deletedCount = 0
  try {
    if (mock.value) {
      const removed = new Set(selected.map(rowId))
      const store = readStore()
      store.limits = store.limits.filter(row => !removed.has(rowId(row)))
      saveStore(store)
      limits.value = store.limits
      deletedCount = selected.length
    } else {
      for (const row of selected) {
        await request('api/qc/attribute-control-limits/' + encodeURIComponent(row.qcItemNo), {
          method: 'DELETE', headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ factorValues: row.factorValues, dataStamp: row.dataStamp }),
        })
        deletedCount++
      }
      await loadDetails(activeQcItemNo.value)
    }
    activeLimitId.value = ''
    selectedLimitIds.value = []
    notice.value = '已刪除 ' + selected.length + ' 筆管制內容。'
  } catch (error) {
    if (!mock.value && deletedCount > 0) await loadDetails(activeQcItemNo.value)
    errorMessage.value = (error instanceof Error ? error.message : '刪除失敗。') + (deletedCount ? ' 已成功刪除 ' + deletedCount + ' 筆。' : '')
  }
  finally { saving.value = false }
}
async function loadPage(): Promise<void> {
  loading.value = true
  errorMessage.value = ''
  try {
    if (mock.value) {
      const store = readStore()
      items.value = store.items
      limits.value = store.limits
      ruleOptions.value = store.rules
      factorOptionsByItem.value = Object.fromEntries(store.items.map(item => [item.qcItemNo, item.factors ?? []]))
    } else {
      const payload = await request<ListPayload<Record<string, unknown>>>('api/qc/attribute-qc-items?issueState=2')
      items.value = listOf(payload).map(normalizeItem)
    }
    const first = items.value[0]
    if (first) {
      activeQcItemNo.value = first.qcItemNo
      await loadDetails(first.qcItemNo)
    }
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '載入計數管制內容失敗。' }
  finally { loading.value = false }
}
function closePage(): void { emit('close') }
onMounted(loadPage)
</script>

<template>
  <main class="spc-att-limit-page">
    <header class="page-title"><h1>計數管制內容</h1><span v-if="mock" class="dev-badge">DEV 假資料</span></header>
    <section class="context-bar"><span>查詢範圍：已核准計數品管項目（IssueState=2）</span><span v-if="activeItem">目前項目：{{ activeItem.qcItemNo }}　{{ activeItem.qcItemName }}</span></section>
    <div class="work-area">
      <section class="master-panel">
        <div class="panel-heading"><strong>計數品管項目</strong><span>{{ items.length }} 筆</span></div>
        <div class="table-scroll master-scroll"><table class="data-grid master-grid"><thead><tr><th>QCItemNo</th><th>QCItemName</th><th>Creator</th><th>CreateDate</th></tr></thead><tbody>
          <tr v-for="item in items" :key="item.qcItemNo" :class="{ active: item.qcItemNo === activeQcItemNo }" @click="selectQcItem(item)">
            <td>{{ item.qcItemNo }}</td><td>{{ item.qcItemName }}</td><td>{{ item.creator }}</td><td>{{ formatDate(item.createDate) }}</td>
          </tr><tr v-if="!items.length"><td colspan="4" class="empty">沒有已核准的計數品管項目</td></tr>
        </tbody></table></div>
      </section>
      <section class="detail-panel">
        <div class="panel-heading"><strong>Attributes Limit</strong><span>{{ currentLimits.length }} 筆</span></div>
        <div class="detail-body">
          <div class="table-scroll limit-scroll"><table class="data-grid limit-grid"><thead><tr>
            <th class="check"></th><th>US</th><th>LS</th><th>UCL</th><th>LCL</th><th>CL</th>
            <th v-for="factor in activeFactors" :key="factor.factorNo">{{ factor.factorNo }}</th>
            <th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th>
          </tr></thead><tbody>
            <tr v-for="row in currentLimits" :key="rowId(row)" :class="{ active: rowId(row) === activeLimitId }" @click="selectLimit(row, $event)">
              <td class="check"><input type="checkbox" :checked="selectedLimitIds.includes(rowId(row))" @click.stop @change="toggleLimit(row, ($event.target as HTMLInputElement).checked)"></td>
              <td>{{ row.us }}</td><td>{{ row.ls }}</td><td>{{ row.ucl }}</td><td>{{ row.lcl }}</td><td>{{ row.cl }}</td>
              <td v-for="factor in activeFactors" :key="factor.factorNo">{{ factorLabel(row, factor.factorNo) }}</td>
              <td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td>
            </tr><tr v-if="!currentLimits.length"><td :colspan="10 + activeFactors.length" class="empty">此 QC Item 尚未設定管制內容</td></tr>
          </tbody></table></div>
          <section class="rules-panel"><div class="panel-heading"><strong>RuleSet</strong><span>{{ parsedRules.length }} 項</span></div>
            <div class="table-scroll rules-scroll"><table class="data-grid"><thead><tr><th>RuleNo</th><th>RuleAction</th></tr></thead><tbody>
              <tr v-for="rule in parsedRules" :key="rule.ruleNo"><td>{{ rule.ruleNo }}</td><td>{{ rule.action }}</td></tr>
              <tr v-if="!parsedRules.length"><td colspan="2" class="empty">未套用判別法則</td></tr>
            </tbody></table></div>
          </section>
          <aside class="actions">
            <button class="win-button" type="button" :disabled="loading || saving || !activeItem" @click="openEditor('add')">新增(A)</button>
            <button class="win-button" type="button" :disabled="loading || saving || !activeLimit" @click="openEditor('edit')">修改(E)</button>
            <button class="win-button" type="button" :disabled="loading || saving || !activeLimit" @click="openEditor('copy')">複製(C)</button>
            <button class="win-button" type="button" :disabled="loading || saving || !selectedLimitIds.length" @click="deleteSelected">刪除(D)</button>
            <button class="win-button close-button" type="button" @click="closePage">關閉</button>
          </aside>
        </div>
        <div class="grid-footer">管制內容筆數：{{ currentLimits.length }}</div>
      </section>
    </div>
    <p v-if="loading" class="notice">資料載入中…</p>
    <p v-if="errorMessage" class="message error">{{ errorMessage }}</p>
    <p v-if="notice" class="message notice">{{ notice }}</p>

    <div v-if="editorOpen" class="backdrop">
      <section class="editor-dialog" role="dialog" aria-modal="true" :aria-label="editorTitle">
        <header class="dialog-title"><h2>SPC Attributes Limit Define（{{ editorTitle }}）</h2><span>{{ activeQcItemNo }}</span></header>
        <div class="qc-item-field"><label for="qc-item-no">QCItemNo</label><input id="qc-item-no" :value="activeQcItemNo" readonly></div>
        <div class="editor-body">
          <div class="editor-left">
            <section class="editor-panel factors-panel"><div class="panel-heading"><strong>Factor（FactorOnline=Y）</strong><span>{{ activeFactors.length }} 項</span></div>
              <div class="table-scroll"><table class="data-grid factor-grid"><thead><tr><th>FactorNo</th><th>FactorName</th><th>Value</th><th v-if="editorMode !== 'edit'">清除</th></tr></thead><tbody>
                <tr v-for="factor in activeFactors" :key="factor.factorNo">
                  <td>{{ factor.factorNo }}</td><td>{{ factor.factorName }}</td>
                  <td>
                    <input v-if="editorMode === 'edit' || factor.parameterType === 2" v-model="editorFactorValues[factor.factorNo]" :readonly="editorMode === 'edit'" :disabled="editorMode === 'edit'" :aria-label="factor.factorNo + ' value'" @input="factor.parameterType === 2 && (editorFactorValues[factor.factorNo] = editorFactorValues[factor.factorNo].toUpperCase())">
                    <select v-else v-model="editorFactorValues[factor.factorNo]" :aria-label="factor.factorNo + ' value'"><option value="">請選擇</option><option v-for="value in factorValueOptions(factor)" :key="value" :value="value">{{ value === '*' ? '全部 (*)' : value }}</option></select>
                  </td>
                  <td v-if="editorMode !== 'edit'"><button class="link-button" type="button" :disabled="factor.parameterType === 2" @click="editorFactorValues[factor.factorNo] = ''">Clear</button></td>
                </tr><tr v-if="!activeFactors.length"><td colspan="4" class="empty">沒有可設定的線上要因</td></tr>
              </tbody></table></div>
            </section>
            <fieldset class="limits-panel"><legend>Sampling / Control Limits</legend>
              <label v-for="field in [{ key: 'us', label: 'US' }, { key: 'ucl', label: 'UCL *' }, { key: 'cl', label: 'CL' }, { key: 'lcl', label: 'LCL' }, { key: 'ls', label: 'LS' }]" :key="field.key" class="limit-field">
                <span>{{ field.label }}</span><input v-model="editorForm[field.key as keyof EditorForm]" inputmode="decimal" maxlength="10">
              </label>
            </fieldset>
          </div>
          <section class="editor-panel rule-panel"><div class="panel-heading"><strong>Rule</strong><span>{{ ruleOptions.length }} 項已核准法則</span></div>
            <div class="table-scroll"><table class="data-grid rule-grid"><thead><tr><th>使用</th><th>RuleNo</th><th>RuleAction</th></tr></thead><tbody>
              <tr v-for="rule in editorRules" :key="rule.ruleNo"><td><input v-model="rule.selected" type="checkbox"></td><td :title="rule.ruleName">{{ rule.ruleNo }}<small>{{ rule.ruleName }}</small></td>
                <td><select v-model="rule.action"><option>Warning</option><option>Hold</option><option>Alert</option></select></td></tr>
              <tr v-if="!editorRules.length"><td colspan="3" class="empty">沒有已核准的計量法則</td></tr>
            </tbody></table></div>
          </section>
        </div>
        <div class="audit-row"><span>建立人　{{ editorAudit.creator }}</span><span>建立日期　{{ editorAudit.createDate }}</span><span>修改人　{{ editorAudit.reviser || '—' }}</span><span>修改日期　{{ editorAudit.reviseDate || '—' }}</span></div>
        <p v-if="editorError" class="message error">{{ editorError }}</p>
        <footer class="dialog-actions"><span class="hint">新增/複製以 QCItemNo + 全部 FactorOnline 值檢查重複；修改時要因唯讀。</span>
          <button class="win-button primary" type="button" :disabled="saving" @click="saveEditor(true)">確定</button>
          <button class="win-button" type="button" :disabled="saving" @click="saveEditor(false)">套用</button>
          <button class="win-button" type="button" :disabled="saving" @click="editorOpen = false">關閉</button>
        </footer>
      </section>
    </div>
  </main>
</template>

<style scoped>
.spc-att-limit-page{--line:#c9c5bc;--text:#302f2c;--muted:#77746e;display:flex;flex-direction:column;gap:6px;height:100%;min-height:0;background:#f7f5ef;color:var(--text);font:13px/1.35 "Segoe UI","Microsoft JhengHei",sans-serif}.page-title{display:flex;align-items:center;justify-content:space-between;padding:2px 10px}.page-title h1{margin:0;font-size:20px}.dev-badge{padding:3px 8px;border:1px solid #d7a044;background:#fff1bd;color:#76551a;font-size:11px}.context-bar{display:flex;justify-content:space-between;gap:12px;min-height:29px;align-items:center;padding:4px 10px;border-block:1px solid #d4d0c8;background:#fff;color:#555}.work-area{display:grid;grid-template-rows:minmax(145px,34%) minmax(0,1fr);gap:5px;flex:1;min-height:0;padding:0 6px}.master-panel,.detail-panel,.editor-panel{display:flex;flex-direction:column;min-width:0;min-height:0;padding:5px;border:1px solid var(--line);background:#fff}.panel-heading{display:flex;align-items:center;justify-content:space-between;min-height:22px;padding:0 3px;color:#555;font-size:12px}.panel-heading span{color:var(--muted);font-size:11px}.table-scroll{flex:1;min-height:0;overflow:auto;border:1px solid #bbb8b1;background:#fff}.data-grid{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}.data-grid th{position:sticky;top:0;z-index:1;padding:5px 7px;border:1px solid #c5c1b9;background:#eeeae0;text-align:left}.data-grid td{height:25px;padding:3px 6px;border:1px solid #d1cec8;background:#fff}.data-grid tbody tr{cursor:pointer}.data-grid tbody tr:hover td{background:#fff8e2}.data-grid tbody tr.active td{background:#ffedb1}.master-grid{min-width:670px}.limit-grid{min-width:830px}.empty{padding:14px!important;text-align:center;color:var(--muted)}.detail-panel{gap:4px}.detail-body{display:grid;grid-template-columns:minmax(0,1fr) 230px 88px;gap:5px;flex:1;min-height:0}.limit-scroll{min-width:0}.rules-panel{display:flex;flex-direction:column;min-height:0;padding:4px;border:1px solid var(--line);background:#fff}.rules-panel .data-grid{white-space:normal}.actions{display:flex;flex-direction:column;gap:6px}.win-button{min-height:30px;padding:4px 8px;border:1px solid #bdb9b0;border-radius:3px;background:linear-gradient(#fff,#efeeeb);color:#343331;font:inherit;white-space:nowrap;cursor:pointer}.win-button:hover:not(:disabled){border-color:#d08a23;background:#fff6df}.win-button:disabled{opacity:.48;cursor:not-allowed}.actions .win-button{width:100%;min-height:32px;padding:4px 5px;font-size:11px}.close-button{margin-top:auto}.grid-footer{height:17px;padding:0 3px;color:#777;font-size:11px}.notice,.message{margin:0 8px;padding:6px 9px;border:1px solid #d8c79a;background:#fff8dc;color:#65541d;font-size:12px}.error{border-color:#d99b86;background:#fff0e9;color:#873820}.check{width:28px;text-align:center!important}.check input,.rule-grid input[type=checkbox]{accent-color:#d89221}.backdrop{position:fixed;inset:0;z-index:1200;display:flex;align-items:center;justify-content:center;padding:12px;background:#17171770}.editor-dialog{display:flex;flex-direction:column;gap:7px;width:min(1008px,97vw);height:min(591px,95vh);padding:11px;border:1px solid #a9a59e;background:#f7f5ef;box-shadow:0 9px 30px #0004}.dialog-title{display:flex;align-items:center;justify-content:space-between;min-height:28px;border-bottom:1px solid var(--line)}.dialog-title h2{margin:0;font-size:17px}.dialog-title>span{color:var(--muted);font-size:12px}.qc-item-field{display:grid;grid-template-columns:100px minmax(0,240px);align-items:center;gap:7px}.qc-item-field label{text-align:right}.qc-item-field input{height:25px;padding:3px 6px;border:1px solid #aaa69e;background:#f0efed;font:inherit}.editor-body{display:grid;grid-template-columns:minmax(0,468px) minmax(0,1fr);gap:8px;flex:1;min-height:0}.editor-left{display:grid;grid-template-rows:minmax(0,1fr) 180px;gap:7px;min-height:0}.editor-panel{padding:5px}.factors-panel .data-grid{min-width:400px}.factor-grid input,.factor-grid select,.rule-grid select,.limit-field input{box-sizing:border-box;width:100%;height:25px;padding:3px 5px;border:1px solid #aaa69e;background:#fff1bf;color:#333;font:inherit}.factor-grid input:read-only,.factor-grid input:disabled{background:#f0efed}.factor-grid td{min-width:88px}.link-button{border:0;background:transparent;color:#8a5a13;text-decoration:underline;cursor:pointer}.link-button:disabled{opacity:.45}.limits-panel{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));align-content:center;gap:7px 12px;margin:0;padding:8px;border:1px solid var(--line);background:#fff}.limits-panel legend{padding:0 5px}.limit-field{display:grid;grid-template-columns:44px minmax(0,1fr);align-items:center;gap:6px}.limit-field span{text-align:right}.rule-grid{min-width:390px}.rule-grid small{display:block;max-width:300px;overflow:hidden;color:#777;font-size:10px;text-overflow:ellipsis}.rule-grid td:first-child{text-align:center}.dialog-actions{display:flex;align-items:center;justify-content:flex-end;gap:6px;min-height:34px}.hint{flex:1;color:#69665e;font-size:10px}.primary{background:#f5a72b;color:white}.audit-row{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:4px 14px;padding:6px;border:1px solid #c9c5bc;background:#f4f2ed;color:#666;font-size:11px}
@media(max-width:900px){.context-bar{flex-wrap:wrap}.detail-body{grid-template-columns:minmax(0,1fr) 200px 78px}.editor-body{grid-template-columns:minmax(0,1fr) minmax(0,1fr)}.editor-left{grid-template-rows:minmax(180px,1fr) auto}.limits-panel{grid-template-columns:1fr}}
@media(max-width:640px){.work-area{grid-template-rows:minmax(125px,30%) minmax(0,1fr)}.detail-body{grid-template-columns:minmax(0,1fr) 70px;grid-template-rows:minmax(0,1fr) 135px}.rules-panel{grid-column:1}.actions{grid-column:2;grid-row:1/span 2}.editor-dialog{height:96vh}.editor-body{grid-template-columns:1fr;overflow:auto}.editor-left{grid-template-rows:minmax(160px,1fr) auto}.rule-panel{min-height:180px}.dialog-actions{flex-wrap:wrap}.hint{flex-basis:100%}}
</style>
