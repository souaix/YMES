<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type IssueState = 0 | 1 | 2 | -1
type EditorMode = 'ADD' | 'EDIT' | 'COPY' | null
interface QcFactor { factorNo: string; parameterName: string; factorSpec: boolean; parameterType: number }
interface QcItem {
  qcItemNo: string
  qcItemName: string
  qcDataType: string
  chartType: 0 | 1 | 2 | -1 | null
  specLineType: 0 | 1 | 2
  componentCollect: number
  unit: string
  lotIsValid: number
  planNo: string
  sampleSize: number
  factors: QcFactor[]
}
interface ChartDetail {
  spcId: number
  chartName: string
  us: string
  cs: string
  ls: string
  ucl: string
  cl: string
  lcl: string
  target: string
  cpkGoal: string
  yAxisFrom: string
  yAxisTo: string
}
interface ChartRule { spcId: number; chartName: string; ruleNo: string; ruleName: string; action: number }
interface PlanParameter { parameterNo: string; parameterValue: string }
interface PlanOption { planNo: string; planName: string; componentCollect?: number }
interface ChartBasis {
  factorSerial: string
  qcItemNo: string
  qcItemName: string
  issueState: IssueState
  dataStamp: number
  sampleSize: number
  keyNode: string
  chartTitle: string
  chartUnit: string
  sponsor: string
  planNo: string
  memo: string
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
  factorValues: Record<string, string>
  details: ChartDetail[]
  rules: ChartRule[]
  parameters: PlanParameter[]
  hasMeasurementData: boolean
  issueJobHasDetail: boolean
}
interface ApiEnvelope<T> { items?: T[]; rows?: T[]; data?: T }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const devMock = computed(() => import.meta.env.DEV && props.developmentPreview)

const stateFilters: { value: IssueState; label: string }[] = [
  { value: 0, label: 'UNFROZEN' },
  { value: 1, label: 'PENDING' },
  { value: 2, label: 'ACTIVE' },
  { value: -1, label: 'UNUSED' },
]
const issueLabel: Record<IssueState, string> = { 0: 'UNFROZEN', 1: 'PENDING', 2: 'ACTIVE', '-1': 'UNUSED' }
const chartTypes = ['Xbar-R', 'Xbar-S', 'X-Rm']
const specLineTypes = ['雙邊規格', '上限規格', '下限規格']
const actionNames: Record<number, string> = { 0: 'Warning', 1: 'Wait', 2: 'Hold', 3: 'Alert' }
const itemRows = ref<QcItem[]>([])
const basisRows = ref<ChartBasis[]>([])
const selectedQcItemNo = ref('')
const selectedBasisSerial = ref('')
const issueState = ref<IssueState>(0)
const planOptions = ref<PlanOption[]>([])
const busy = ref(false)
const message = ref('')
const error = ref('')
const editorMode = ref<EditorMode>(null)
const editorDraft = ref<ChartBasis>(blankBasis())
const ruleEditorOpen = ref(false)
const ruleDraft = ref<ChartRule[]>([])
const copyDialogOpen = ref(false)
const copyTargetNo = ref('')
const copyState = ref<'both' | 'unfrozen' | 'active'>('both')
const auditEntries = ref<string[]>([])

const activeQcItem = computed(() => itemRows.value.find(row => row.qcItemNo === selectedQcItemNo.value) ?? null)
const visibleBasisRows = computed(() => basisRows.value
  .filter(row => row.qcItemNo === selectedQcItemNo.value && row.issueState === issueState.value)
  .sort((a, b) => b.createDate.localeCompare(a.createDate)))
const activeBasis = computed(() => visibleBasisRows.value.find(row => row.factorSerial === selectedBasisSerial.value) ?? null)
const detailRows = computed(() => activeBasis.value?.details ?? [])
const ruleRows = computed(() => activeBasis.value?.rules ?? [])
const chartFilterLabel = computed(() => stateFilters.find(item => item.value === issueState.value)?.label ?? '')
const sourceCopyCount = computed(() => basisRows.value.filter(row => row.qcItemNo === selectedQcItemNo.value && (copyState.value === 'both' || (copyState.value === 'unfrozen' ? row.issueState === 0 : row.issueState === 2))).length)

function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function blankDetail(spcId: number, chartName: string): ChartDetail {
  return { spcId, chartName, us: '', cs: '', ls: '', ucl: '', cl: '', lcl: '', target: '', cpkGoal: '', yAxisFrom: '', yAxisTo: '' }
}
function blankBasis(): ChartBasis {
  return {
    factorSerial: '', qcItemNo: '', qcItemName: '', issueState: 0, dataStamp: 0, sampleSize: 1, keyNode: 'N',
    chartTitle: '', chartUnit: '', sponsor: '', planNo: '', memo: '', creator: '', createDate: '', reviser: '', reviseDate: '',
    factorValues: {}, details: [], rules: [], parameters: [], hasMeasurementData: false, issueJobHasDetail: false,
  }
}
function expectedDetails(chartType: number): ChartDetail[] {
  if (chartType === -1) return [blankDetail(0, 'Trend Chart')]
  if (chartType === 1) return [blankDetail(0, 'Trend Chart'), blankDetail(1, 'Xbar Chart'), blankDetail(3, 'S Chart')]
  if (chartType === 2) return [blankDetail(0, 'X Chart'), blankDetail(4, 'Rm Chart')]
  return [blankDetail(0, 'Trend Chart'), blankDetail(1, 'Xbar Chart'), blankDetail(2, 'R Chart')]
}
function defaultRules(details: ChartDetail[]): ChartRule[] {
  const first = details[0]
  return first ? [
    { spcId: first.spcId, chartName: first.chartName, ruleNo: 'VR-001', ruleName: '單點超出管制界限', action: 2 },
    { spcId: first.spcId, chartName: first.chartName, ruleNo: 'VR-004', ruleName: '連續九點位於中心線同側', action: 0 },
  ] : []
}
function chartTypeLabel(chartType: number | null): string { return chartType === null || chartType < 0 ? 'No Chart' : chartTypes[chartType] ?? `ChartType ${chartType}` }
function demoData(): { items: QcItem[]; charts: ChartBasis[] } {
  const items: QcItem[] = [
    { qcItemNo: 'QCV-0101', qcItemName: '晶圓厚度', qcDataType: 'Variable', chartType: 0, specLineType: 0, componentCollect: 0, unit: 'μm', lotIsValid: 1, planNo: 'SP-100', sampleSize: 5, factors: [{ factorNo: 'PRODUCT', parameterName: '產品編號', factorSpec: true, parameterType: 0 }, { factorNo: 'MATERIAL', parameterName: '材料編號', factorSpec: false, parameterType: 0 }, { factorNo: 'EQUIPMENT', parameterName: '設備編號', factorSpec: false, parameterType: 1 }] },
    { qcItemNo: 'QCV-0102', qcItemName: '焊點拉力', qcDataType: 'Variable', chartType: 1, specLineType: 0, componentCollect: 0, unit: 'N', lotIsValid: 1, planNo: 'SP-101', sampleSize: 4, factors: [{ factorNo: 'PRODUCT', parameterName: '產品編號', factorSpec: true, parameterType: 0 }, { factorNo: 'OP', parameterName: '作業站', factorSpec: false, parameterType: 1 }] },
    { qcItemNo: 'QCV-0103', qcItemName: '點膠寬度', qcDataType: 'Variable', chartType: 2, specLineType: 1, componentCollect: 1, unit: 'mm', lotIsValid: 0, planNo: '', sampleSize: 1, factors: [{ factorNo: 'PRODUCT', parameterName: '產品編號', factorSpec: true, parameterType: 0 }, { factorNo: 'MATERIAL', parameterName: '材料編號', factorSpec: false, parameterType: 0 }] },
  ]
  const build = (qc: QcItem, serial: string, status: IssueState, factorValues: Record<string, string>, offset: number): ChartBasis => {
    const details = expectedDetails(qc.chartType ?? -1).map((row, i) => ({
      ...row, us: i === 0 ? '18.00' : '', cs: i === 0 ? '15.00' : '', ls: i === 0 ? '12.00' : '',
      ucl: i === 0 ? '' : '16.50', cl: i === 0 ? '' : '15.00', lcl: i === 0 ? '' : '13.50',
      target: i === 0 ? '15.00' : '', cpkGoal: i === 0 ? '1.33' : '', yAxisFrom: '8', yAxisTo: '22',
    }))
    return {
      ...blankBasis(), factorSerial: serial, qcItemNo: qc.qcItemNo, qcItemName: qc.qcItemName, issueState: status,
      dataStamp: 1 + offset, sampleSize: qc.sampleSize, keyNode: 'N', chartTitle: `${qc.qcItemName}管制圖`, chartUnit: qc.unit,
      sponsor: '品保工程', planNo: qc.planNo, memo: '', creator: 'A11019', createDate: `2026-11-${String(17 - offset).padStart(2, '0')} 10:30:00`,
      reviser: 'A11019', reviseDate: '2026-11-17 10:45:00', factorValues, details,
      rules: [
        { spcId: details[0].spcId, chartName: details[0].chartName, ruleNo: 'VR-001', ruleName: '單點超出管制界限', action: 2 },
        { spcId: details[0].spcId, chartName: details[0].chartName, ruleNo: 'VR-004', ruleName: '連續九點位於中心線同側', action: 0 },
      ],
      parameters: qc.planNo ? [{ parameterNo: 'FREQUENCY', parameterValue: '每批' }, { parameterNo: 'LOT-QTY', parameterValue: '25' }] : [],
      hasMeasurementData: serial === 'DEV-FS-0004', issueJobHasDetail: false,
    }
  }
  return { items, charts: [
    build(items[0], 'DEV-FS-0001', 0, { PRODUCT: 'DEV-PROD-A', MATERIAL: 'DEV-MAT-01', EQUIPMENT: 'DEV-EQP-01' }, 0),
    build(items[0], 'DEV-FS-0002', 2, { PRODUCT: 'DEV-PROD-B', MATERIAL: 'DEV-MAT-02', EQUIPMENT: 'DEV-EQP-02' }, 1),
    build(items[0], 'DEV-FS-0003', -1, { PRODUCT: 'DEV-PROD-C', MATERIAL: 'DEV-MAT-03', EQUIPMENT: 'DEV-EQP-01' }, 2),
    build(items[0], 'DEV-FS-0004', 0, { PRODUCT: 'DEV-PROD-D', MATERIAL: 'DEV-MAT-01', EQUIPMENT: 'DEV-EQP-03' }, 3),
    build(items[1], 'DEV-FS-0005', 1, { PRODUCT: 'DEV-PROD-A', OP: 'OP-ASSY-01' }, 1),
    build(items[1], 'DEV-FS-0006', 0, { PRODUCT: 'DEV-PROD-B', OP: 'OP-ASSY-02' }, 2),
    build(items[2], 'DEV-FS-0007', 0, { PRODUCT: 'DEV-PROD-A', MATERIAL: 'DEV-MAT-01' }, 3),
  ] }
}

function apiUrl(path: string): string {
  if (!props.apiBaseUrl.trim()) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(props.apiBaseUrl.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init })
  if (response.status === 401) {
    window.location.assign(`${import.meta.env.BASE_URL}login`)
    throw new Error('登入已逾期，請重新登入。')
  }
  const body = await response.text()
  if (!response.ok) throw new Error(body || `API 請求失敗 (${response.status})`)
  return (body ? JSON.parse(body) : undefined) as T
}
function records<T>(payload: T[] | ApiEnvelope<T>): T[] {
  if (Array.isArray(payload)) return payload
  return payload.items ?? payload.rows ?? []
}
async function loadPage(): Promise<void> {
  busy.value = true
  error.value = ''
  try {
    if (devMock.value) {
      const demo = demoData()
      itemRows.value = demo.items
      basisRows.value = demo.charts
    } else {
      itemRows.value = records(await request<QcItem[] | ApiEnvelope<QcItem>>('/api/qc/chart-bases/qc-items'))
      if (!selectedQcItemNo.value && itemRows.value.length) selectedQcItemNo.value = itemRows.value[0].qcItemNo
      await loadChartRows()
    }
    if (!selectedQcItemNo.value && itemRows.value.length) selectedQcItemNo.value = itemRows.value[0].qcItemNo
    await loadPlanOptions()
    selectFirstChart()
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : '載入管制圖資料失敗。'
  } finally { busy.value = false }
}
async function loadChartRows(): Promise<void> {
  if (devMock.value || !selectedQcItemNo.value) return
  const query = new URLSearchParams({ qcItemNo: selectedQcItemNo.value, issueState: String(issueState.value) })
  const response = await request<ChartBasis[] | ApiEnvelope<ChartBasis>>(`/api/qc/chart-bases?${query}`)
  const incoming = records(response)
  basisRows.value = [...basisRows.value.filter(row => !(row.qcItemNo === selectedQcItemNo.value && row.issueState === issueState.value)), ...incoming]
}
async function loadPlanOptions(): Promise<void> {
  const componentCollect = activeQcItem.value?.componentCollect
  if (componentCollect === undefined) { planOptions.value = []; return }
  if (devMock.value) {
    planOptions.value = [
      { planNo: 'SP-100', planName: '一般抽樣計畫', componentCollect },
      { planNo: 'SP-101', planName: '高頻抽樣計畫', componentCollect },
    ]
    return
  }
  const query = new URLSearchParams({ issueState: '2', componentCollect: String(componentCollect) })
  planOptions.value = records(await request<PlanOption[] | ApiEnvelope<PlanOption>>(`/api/qc/sampling-plans?${query}`))
}
function selectFirstChart(): void { selectedBasisSerial.value = visibleBasisRows.value[0]?.factorSerial ?? '' }
async function setIssueFilter(value: IssueState): Promise<void> {
  issueState.value = value
  message.value = ''
  try { await loadChartRows(); selectFirstChart() } catch (reason) { error.value = reason instanceof Error ? reason.message : '載入狀態資料失敗。' }
}
async function chooseQcItem(item: QcItem): Promise<void> {
  selectedQcItemNo.value = item.qcItemNo
  selectedBasisSerial.value = ''
  message.value = ''
  try { await Promise.all([loadChartRows(), loadPlanOptions()]); selectFirstChart() } catch (reason) { error.value = reason instanceof Error ? reason.message : '載入管制圖失敗。' }
}
async function chooseBasis(row: ChartBasis): Promise<void> {
  selectedBasisSerial.value = row.factorSerial
  if (devMock.value) return
  try {
    const [details, rules] = await Promise.all([
      request<ChartDetail[] | ApiEnvelope<ChartDetail>>(`/api/qc/chart-bases/${encodeURIComponent(row.factorSerial)}/detail`),
      request<ChartRule[] | ApiEnvelope<ChartRule>>(`/api/qc/chart-bases/${encodeURIComponent(row.factorSerial)}/rules`),
    ])
    row.details = records(details)
    row.rules = records(rules)
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '載入圖表明細失敗。' }
}

function openEditor(mode: Exclude<EditorMode, null>): void {
  error.value = ''
  message.value = ''
  if (!activeQcItem.value) { error.value = '請先選擇品管項目。'; return }
  if (mode !== 'ADD' && !activeBasis.value) { error.value = '請先選擇管制圖資料。'; return }
  if (mode === 'ADD') {
    const draft = blankBasis()
    draft.qcItemNo = activeQcItem.value.qcItemNo
    draft.qcItemName = activeQcItem.value.qcItemName
    draft.sampleSize = activeQcItem.value.sampleSize
    draft.chartUnit = activeQcItem.value.unit
    draft.planNo = activeQcItem.value.planNo
    draft.factorValues = Object.fromEntries(activeQcItem.value.factors.map(f => [f.factorNo, '']))
    draft.details = expectedDetails(activeQcItem.value.chartType ?? -1)
    draft.rules = devMock.value ? defaultRules(draft.details) : []
    editorDraft.value = draft
  } else {
    editorDraft.value = clone(activeBasis.value!)
    if (mode === 'COPY') {
      editorDraft.value.factorSerial = ''
      editorDraft.value.issueState = 0
      editorDraft.value.dataStamp = 0
      editorDraft.value.factorValues = Object.fromEntries(activeQcItem.value.factors.map(f => [f.factorNo, editorDraft.value.factorValues[f.factorNo] ?? '']))
    }
  }
  editorMode.value = mode
}
function activeChartName(spcId: number): string { return expectedDetails(activeQcItem.value?.chartType ?? -1).find(row => row.spcId === spcId)?.chartName ?? `SPCID ${spcId}` }
function basisCommand(includeConcurrency: boolean): Record<string, unknown> {
  const details = editorDraft.value.details.map(({ spcId, us, cs, ls, ucl, cl, lcl, target, cpkGoal, yAxisFrom, yAxisTo }) => ({ spcId, us, cs, ls, ucl, cl, lcl, target, cpkGoal, yAxisFrom, yAxisTo }))
  const command: Record<string, unknown> = {
    qcItemNo: editorDraft.value.qcItemNo, sampleSize: editorDraft.value.sampleSize, keyNode: editorDraft.value.keyNode,
    chartTitle: editorDraft.value.chartTitle, chartUnit: editorDraft.value.chartUnit, sponsor: editorDraft.value.sponsor,
    planNo: editorDraft.value.planNo, memo: editorDraft.value.memo, factorValues: editorDraft.value.factorValues,
    details, samplingPlanParameters: editorDraft.value.parameters.map(({ parameterNo, parameterValue }) => ({ parameterNo, parameterValue })),
  }
  if (includeConcurrency) {
    command.expectedIssueState = editorDraft.value.issueState
    command.dataStamp = editorDraft.value.dataStamp
  }
  return command
}
function validateDraft(requireMemo: boolean): string {
  if (!activeQcItem.value) return '請先選擇品管項目。'
  const sampleSize = Number(editorDraft.value.sampleSize)
  if (!Number.isInteger(sampleSize) || sampleSize <= 0) return 'SampleSize 必須是大於 0 的整數。'
  if (activeQcItem.value.chartType === 2 && sampleSize !== 1) return 'X-Rm 圖的 SampleSize 必須為 1。'
  if (requireMemo && editorMode.value === 'EDIT' && !editorDraft.value.memo.trim()) return '修改時請填寫 MEMO 作為修改原因。'
  for (const factor of activeQcItem.value.factors.filter(row => row.factorNo)) {
    const value = editorDraft.value.factorValues[factor.factorNo]?.trim() ?? ''
    if (!value) return `${factor.factorNo} 的要因值不可空白。`
    if (value === '*') return '要因值不可為 *。'
  }
  for (const detail of editorDraft.value.details) {
    if (detail.spcId === 0 && activeQcItem.value.chartType !== null && activeQcItem.value.chartType >= 0) {
      const requiredFields = activeQcItem.value.specLineType === 0 ? ['us', 'ls'] : activeQcItem.value.specLineType === 1 ? ['us'] : ['ls']
      for (const key of requiredFields) if (!detail[key as keyof ChartDetail]) return `${detail.chartName} 的 ${key.toUpperCase()} 不可空白。`
    }
    for (const key of ['us', 'cs', 'ls', 'ucl', 'cl', 'lcl', 'target', 'cpkGoal', 'yAxisFrom', 'yAxisTo'] as const) {
      const value = detail[key].trim()
      if (value && !Number.isFinite(Number(value))) return `${detail.chartName} 的 ${key} 必須是數值。`
    }
    if (detail.yAxisFrom && detail.yAxisTo && Number(detail.yAxisFrom) >= Number(detail.yAxisTo)) return `${detail.chartName} 的 YAxisTo 必須大於 YAxisFrom。`
  }
  if (editorDraft.value.planNo && activeQcItem.value.componentCollect === 0 && !editorDraft.value.parameters.length) return '已指定 PlanNo，請按 Get Plan Parameter 載入抽樣計畫參數。'
  if (editorDraft.value.parameters.some(row => !row.parameterNo.trim())) return '抽樣參數 ParameterNo 不可空白。'
  if (new Set(editorDraft.value.parameters.map(row => row.parameterNo.trim())).size !== editorDraft.value.parameters.length) return '抽樣參數 ParameterNo 不可重複。'
  return ''
}
function recordAudit(action: string, serial: string): void {
  const stamp = new Date().toLocaleString('zh-TW', { hour12: false })
  auditEntries.value.unshift(`${stamp} ${props.currentUserNo || 'DEV001'} ${action} ${serial}`)
}
async function persistEditor(closeAfter: boolean): Promise<void> {
  const validation = validateDraft(closeAfter)
  if (validation) { error.value = validation; return }
  busy.value = true
  error.value = ''
  try {
    if (devMock.value) {
      const isNew = editorMode.value !== 'EDIT'
      if (isNew && basisRows.value.some(row => row.qcItemNo === editorDraft.value.qcItemNo && JSON.stringify(row.factorValues) === JSON.stringify(editorDraft.value.factorValues) && row.issueState !== -1)) {
        throw new Error('相同 QCItem 與要因組合已存在管制圖。')
      }
      if (isNew) {
        editorDraft.value.factorSerial = `DEV-FS-${String(Date.now()).slice(-6)}`
        editorDraft.value.creator = props.currentUserNo || 'DEV001'
        editorDraft.value.createDate = new Date().toLocaleString('sv-SE')
        editorDraft.value.issueState = 0
        editorDraft.value.dataStamp = 1
        basisRows.value.push(clone(editorDraft.value))
        recordAudit(editorMode.value === 'COPY' ? '複製管制圖' : '新增管制圖', editorDraft.value.factorSerial)
      } else {
        editorDraft.value.dataStamp += 1
        editorDraft.value.reviser = props.currentUserNo || 'DEV001'
        editorDraft.value.reviseDate = new Date().toLocaleString('sv-SE')
        const index = basisRows.value.findIndex(row => row.factorSerial === editorDraft.value.factorSerial)
        if (index >= 0) basisRows.value[index] = clone(editorDraft.value)
        recordAudit('修改管制圖', editorDraft.value.factorSerial)
      }
    } else if (editorMode.value === 'EDIT') {
      await request(`/api/qc/chart-bases/${encodeURIComponent(editorDraft.value.factorSerial)}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(basisCommand(true)) })
    } else {
      const created = await request<ChartBasis>('/api/qc/chart-bases', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(basisCommand(false)) })
      if (created?.factorSerial) editorDraft.value.factorSerial = created.factorSerial
    }
    if (closeAfter) editorMode.value = null
    await loadChartRows()
    if (editorDraft.value.factorSerial) {
      selectedBasisSerial.value = editorDraft.value.factorSerial
      const saved = visibleBasisRows.value.find(row => row.factorSerial === editorDraft.value.factorSerial)
      if (saved && !devMock.value) await chooseBasis(saved)
    }
    message.value = '資料已儲存。新資料預設為 UNFROZEN。'
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '儲存管制圖失敗。' }
  finally { busy.value = false }
}

async function deleteBasis(): Promise<void> {
  const row = activeBasis.value
  if (!row) { error.value = '請先選擇管制圖資料。'; return }
  if (row.issueState === 1) { error.value = 'PENDING 狀態不可刪除。'; return }
  const originalState = row.issueState
  const action = originalState === 2 ? '停用' : '刪除'
  if (!window.confirm(`確定${action}管制圖 ${row.factorSerial}？`)) return
  try {
    if (devMock.value) {
      if (row.issueState === 2) row.issueState = -1
      else {
        if (row.hasMeasurementData) throw new Error('此管制圖已有量測資料，不允許刪除。')
        basisRows.value = basisRows.value.filter(item => item.factorSerial !== row.factorSerial)
      }
      recordAudit(action, row.factorSerial)
    } else {
      await request(`/api/qc/chart-bases/${encodeURIComponent(row.factorSerial)}`, { method: 'DELETE', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ issueState: row.issueState, dataStamp: row.dataStamp }) })
    }
    await loadChartRows()
    selectFirstChart()
    message.value = originalState === 2 ? '已核准的管制圖已標示為 UNUSED。' : '管制圖已刪除。'
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '刪除失敗。' }
}
async function approveBasis(): Promise<void> {
  const row = activeBasis.value
  if (!row || row.issueState !== 0) { error.value = '僅 UNFROZEN 資料可核准。'; return }
  if (!window.confirm(`確定核准 ${row.factorSerial}？`)) return
  try {
    if (devMock.value) {
      const duplicate = basisRows.value.some(other => other.factorSerial !== row.factorSerial && other.qcItemNo === row.qcItemNo && other.issueState === 2 && JSON.stringify(other.factorValues) === JSON.stringify(row.factorValues))
      if (duplicate) throw new Error('相同要因組合已有 ACTIVE 管制圖，請先確認重複資料。')
      row.issueState = row.issueJobHasDetail ? 1 : 2
      row.dataStamp += 1
      row.reviser = props.currentUserNo || 'DEV001'
      row.reviseDate = new Date().toLocaleString('sv-SE')
      recordAudit('核准管制圖', row.factorSerial)
    } else {
      await request(`/api/qc/chart-bases/${encodeURIComponent(row.factorSerial)}/approve`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ dataStamp: row.dataStamp }) })
    }
    await loadChartRows(); selectFirstChart(); message.value = '核准處理完成；Issue Job 設定會決定進入 PENDING 或 ACTIVE。'
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '核准失敗。' }
}
async function disapproveBasis(): Promise<void> {
  const row = activeBasis.value
  if (!row || ![2, -1].includes(row.issueState)) { error.value = '僅 ACTIVE 或 UNUSED 資料可取消核准。'; return }
  if (!window.confirm(`取消核准 ${row.factorSerial} 並回到 UNFROZEN？`)) return
  try {
    if (devMock.value) { row.issueState = 0; row.dataStamp += 1; recordAudit('取消核准', row.factorSerial) }
    else await request(`/api/qc/chart-bases/${encodeURIComponent(row.factorSerial)}/disapprove`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ issueState: row.issueState, dataStamp: row.dataStamp }) })
    await loadChartRows(); selectFirstChart(); message.value = '資料已回到 UNFROZEN。'
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '取消核准失敗。' }
}

function openRuleEditor(): void {
  if (!activeBasis.value) { error.value = '請先選擇管制圖資料。'; return }
  if (activeQcItem.value?.chartType === null || activeQcItem.value?.chartType === -1) { error.value = 'No Chart 的 QC Item 不提供 Rule 設定。'; return }
  ruleDraft.value = clone(activeBasis.value.rules)
  ruleEditorOpen.value = true
}
function addRule(): void {
  const spcId = activeBasis.value?.details[0]?.spcId ?? 0
  ruleDraft.value.push({ spcId, chartName: activeChartName(spcId), ruleNo: '', ruleName: '', action: 0 })
}
async function saveRules(): Promise<void> {
  const row = activeBasis.value
  if (!row) return
  if (ruleDraft.value.some(item => !item.ruleNo.trim())) { error.value = 'RuleNo 不可空白。'; return }
  if (new Set(ruleDraft.value.map(item => `${item.spcId}:${item.ruleNo}`)).size !== ruleDraft.value.length) { error.value = '同一圖表的 RuleNo 不可重複。'; return }
  try {
    if (devMock.value) recordAudit('維護管制圖法則', row.factorSerial)
    else {
      const saved = await request<{ dataStamp: number; reviser: string; reviseDate: string }>(`/api/qc/chart-bases/${encodeURIComponent(row.factorSerial)}/rules`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ rules: ruleDraft.value.map(({ spcId, ruleNo, action }) => ({ spcId, ruleNo, action })) }) })
      row.dataStamp = saved.dataStamp
      row.reviser = saved.reviser
      row.reviseDate = saved.reviseDate
    }
    row.rules = clone(ruleDraft.value)
    ruleEditorOpen.value = false
    message.value = 'Rule 資料已儲存。'
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '儲存 Rule 失敗。' }
}

function openCopyDialog(): void {
  if (!activeQcItem.value) { error.value = '請先選擇來源品管項目。'; return }
  copyTargetNo.value = ''
  copyState.value = 'both'
  copyDialogOpen.value = true
}
async function copyToItem(): Promise<void> {
  const source = activeQcItem.value
  const target = itemRows.value.find(row => row.qcItemNo === copyTargetNo.value)
  if (!source || !target) { error.value = '請選擇目標品管項目。'; return }
  if (source.qcItemNo === target.qcItemNo) { error.value = '來源與目標品管項目不可相同。'; return }
  if (source.factors.filter(f => f.factorSpec).map(f => f.factorNo).join('|') !== target.factors.filter(f => f.factorSpec).map(f => f.factorNo).join('|')) { error.value = '來源與目標品管項目的規格要因欄位不同，無法複製。'; return }
  const selected = devMock.value ? basisRows.value.filter(row => row.qcItemNo === source.qcItemNo && (copyState.value === 'both' || (copyState.value === 'unfrozen' ? row.issueState === 0 : row.issueState === 2))) : []
  if (devMock.value && !selected.length) { error.value = '目前選取狀態沒有可複製的管制圖。'; return }
  let copiedCount = selected.length
  try {
    if (devMock.value) {
      for (const row of selected) {
        const copy = clone(row)
        copy.factorSerial = `DEV-FS-${String(Date.now() + basisRows.value.length).slice(-6)}`
        copy.qcItemNo = target.qcItemNo
        copy.qcItemName = target.qcItemName
        copy.issueState = 0
        copy.dataStamp = 1
        copy.creator = props.currentUserNo || 'DEV001'
        copy.createDate = new Date().toLocaleString('sv-SE')
        basisRows.value.push(copy)
      }
      recordAudit('跨 QC Item 複製', `${source.qcItemNo} → ${target.qcItemNo}`)
    } else {
      const copyType = copyState.value === 'both' ? 3 : copyState.value === 'unfrozen' ? 0 : 2
      const result = await request<{ copiedCount?: number }>('/api/qc/chart-bases/copy', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ fromQcItemNo: source.qcItemNo, toQcItemNo: target.qcItemNo, copyType }) })
      copiedCount = result?.copiedCount ?? 0
    }
    copyDialogOpen.value = false
    await loadChartRows()
    message.value = `已複製 ${copiedCount} 筆管制圖至 ${target.qcItemNo}；副本狀態為 UNFROZEN。`
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '複製管制圖失敗。' }
}

function setPlanNo(value: string): void { editorDraft.value.planNo = value }
function getPlanParameters(): void {
  if (!editorDraft.value.planNo.trim()) { error.value = '請先選擇 PlanNo。'; return }
  if (devMock.value) {
    editorDraft.value.parameters = editorDraft.value.planNo === 'SP-101'
      ? [{ parameterNo: 'FREQUENCY', parameterValue: '每 2 小時' }, { parameterNo: 'LOT-QTY', parameterValue: '20' }]
      : [{ parameterNo: 'FREQUENCY', parameterValue: '每批' }, { parameterNo: 'LOT-QTY', parameterValue: '25' }]
  } else {
    void request<PlanParameter[] | ApiEnvelope<PlanParameter>>(`/api/qc/sampling-plans/${encodeURIComponent(editorDraft.value.planNo)}/parameters?issueState=2`)
      .then(response => { editorDraft.value.parameters = records(response) })
      .catch(reason => { error.value = reason instanceof Error ? reason.message : '讀取抽樣計畫參數失敗。' })
  }
}
function clearPlan(): void { editorDraft.value.planNo = ''; editorDraft.value.parameters = [] }
function factorValue(factorNo: string): string { return editorDraft.value.factorValues[factorNo] ?? '' }
function updateFactorValue(factorNo: string, value: string): void { editorDraft.value.factorValues[factorNo] = value }
function addPlanParameter(): void { editorDraft.value.parameters.push({ parameterNo: '', parameterValue: '' }) }
function formatDate(value: string): string { return value || '—' }

onMounted(() => { void loadPage() })
</script>

<template>
  <main class="qc-chart-page">
    <div class="qc-chart-heading">
      <div><h1>管制圖內容</h1><span class="qc-chart-caption">QC Chart Basis　｜　QC301</span></div>
      <button class="qc-button" @click="emit('close')">關閉</button>
    </div>

    <div v-if="devMock" class="qc-chart-dev-note">DEV 假資料模式：所有新增、修改、簽核、刪除及規則操作只在本頁記憶體中模擬。</div>
    <div v-if="error" class="qc-chart-message error" role="alert">{{ error }}</div>
    <div v-if="message" class="qc-chart-message">{{ message }}</div>

    <section class="qc-chart-panel qc-items-panel">
      <div class="qc-panel-title">計量品管項目</div>
      <div class="qc-table-scroll qc-item-scroll">
        <table class="qc-table qc-items-table">
          <thead><tr><th>QCItemNo</th><th>QCItemName</th><th>QCDataType</th><th>ChartType</th><th>SpecLineType</th><th>ComponentCollect</th><th>Unit</th><th>LotIsValid</th><th>PlanNo</th></tr></thead>
          <tbody>
            <tr v-for="item in itemRows" :key="item.qcItemNo" :class="{ selected: item.qcItemNo === selectedQcItemNo }" @click="chooseQcItem(item)">
              <td>{{ item.qcItemNo }}</td><td>{{ item.qcItemName }}</td><td>{{ item.qcDataType }}</td><td>{{ chartTypeLabel(item.chartType) }}</td><td>{{ specLineTypes[item.specLineType] }}</td><td>{{ item.componentCollect }}</td><td>{{ item.unit }}</td><td>{{ item.lotIsValid }}</td><td>{{ item.planNo || '—' }}</td>
            </tr>
            <tr v-if="!itemRows.length"><td colspan="9" class="qc-empty">{{ busy ? '載入中…' : '沒有已核准計量品管項目' }}</td></tr>
          </tbody>
        </table>
      </div>
    </section>

    <section class="qc-status-bar" aria-label="管制圖狀態篩選">
      <label v-for="filter in stateFilters" :key="filter.value" class="qc-radio"><input type="radio" name="qc-chart-state" :checked="issueState === filter.value" @change="setIssueFilter(filter.value)"><span>{{ filter.label }}</span></label>
      <span class="qc-current-item">目前項目：<b>{{ activeQcItem?.qcItemNo || '未選擇' }}</b>　{{ activeQcItem?.qcItemName }}</span>
    </section>

    <div class="qc-chart-workspace">
      <div class="qc-chart-left">
        <section class="qc-chart-panel qc-basis-panel">
          <div class="qc-panel-title">管制圖主檔 <span>{{ chartFilterLabel }}</span></div>
          <div class="qc-table-scroll qc-basis-scroll">
            <table class="qc-table qc-basis-table">
              <thead><tr><th v-for="factor in activeQcItem?.factors ?? []" :key="factor.factorNo">{{ factor.factorNo }}</th><th>SampleSize</th><th>PlanNo</th><th>KeyNode</th><th>ChartTitle</th><th>Sponsor</th><th>Creator</th><th>CreateDate</th><th>ChartUnit</th><th>MEMO</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
              <tbody>
                <tr v-for="row in visibleBasisRows" :key="row.factorSerial" :class="{ selected: row.factorSerial === selectedBasisSerial }" @click="chooseBasis(row)">
                  <td v-for="factor in activeQcItem?.factors ?? []" :key="factor.factorNo">{{ row.factorValues[factor.factorNo] || '—' }}</td><td>{{ row.sampleSize }}</td><td>{{ row.planNo || '—' }}</td><td>{{ row.keyNode }}</td><td>{{ row.chartTitle }}</td><td>{{ row.sponsor }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.chartUnit }}</td><td>{{ row.memo }}</td><td>{{ row.reviser }}</td><td>{{ row.reviseDate }}</td>
                </tr>
                <tr v-if="!visibleBasisRows.length"><td :colspan="11 + (activeQcItem?.factors.length ?? 0)" class="qc-empty">{{ busy ? '載入中…' : '此狀態沒有管制圖資料' }}</td></tr>
              </tbody>
            </table>
          </div>
        </section>

        <section class="qc-chart-panel qc-lower-panel">
          <div class="qc-panel-title">管制圖明細</div>
          <div class="qc-table-scroll qc-subgrid-scroll">
            <table class="qc-table qc-sub-table"><thead><tr><th>SPCID</th><th>Chart Type</th><th>US</th><th>CS</th><th>LS</th><th>UCL</th><th>CL</th><th>LCL</th><th>Target</th><th>CpkGoal</th><th>YAxisFrom</th><th>YAxisTo</th></tr></thead>
              <tbody><tr v-for="row in detailRows" :key="row.spcId"><td>{{ row.spcId }}</td><td>{{ row.chartName }}</td><td>{{ row.us }}</td><td>{{ row.cs }}</td><td>{{ row.ls }}</td><td>{{ row.ucl }}</td><td>{{ row.cl }}</td><td>{{ row.lcl }}</td><td>{{ row.target }}</td><td>{{ row.cpkGoal }}</td><td>{{ row.yAxisFrom }}</td><td>{{ row.yAxisTo }}</td></tr><tr v-if="!detailRows.length"><td colspan="12" class="qc-empty">請選擇一筆管制圖</td></tr></tbody>
            </table>
          </div>
        </section>

        <section class="qc-chart-panel qc-lower-panel">
          <div class="qc-panel-title">管制圖規則</div>
          <div class="qc-table-scroll qc-subgrid-scroll">
            <table class="qc-table qc-sub-table"><thead><tr><th>SPCID</th><th>Chart Type</th><th>RuleNo</th><th>RuleName</th><th>Action</th></tr></thead>
              <tbody><tr v-for="(row, index) in ruleRows" :key="`${row.spcId}-${row.ruleNo}-${index}`"><td>{{ row.spcId }}</td><td>{{ row.chartName }}</td><td>{{ row.ruleNo }}</td><td>{{ row.ruleName }}</td><td>{{ actionNames[row.action] }}</td></tr><tr v-if="!ruleRows.length"><td colspan="5" class="qc-empty">請選擇一筆管制圖</td></tr></tbody>
            </table>
          </div>
        </section>
      </div>

      <aside class="qc-actions">
        <button class="qc-button primary" :disabled="!activeQcItem || busy" @click="openEditor('ADD')">新增 (A)</button>
        <button class="qc-button" :disabled="!activeBasis || busy" @click="openEditor('EDIT')">修改 (E)</button>
        <button class="qc-button" :disabled="!activeBasis || busy" @click="deleteBasis">刪除 (D)</button>
        <button class="qc-button" :disabled="!activeBasis || busy" @click="openEditor('COPY')">複製 (C)</button>
        <button class="qc-button" :disabled="!activeBasis || busy" @click="approveBasis">核准</button>
        <button class="qc-button" :disabled="!activeBasis || busy" @click="disapproveBasis">取消核准</button>
        <button class="qc-button" :disabled="!activeBasis || busy || activeQcItem?.chartType === -1 || activeQcItem?.chartType === null" @click="openRuleEditor">Rule (R)</button>
        <button class="qc-button" :disabled="!activeQcItem || busy" @click="openCopyDialog">Copy2</button>
        <div class="qc-action-spacer"></div>
        <button class="qc-button" @click="emit('close')">關閉 (X)</button>
      </aside>
    </div>

    <section v-if="auditEntries.length && devMock" class="qc-audit-preview"><b>DEV 操作記錄</b><span v-for="entry in auditEntries.slice(0, 3)" :key="entry">{{ entry }}</span></section>

    <div v-if="editorMode" class="qc-modal-backdrop" @click.self="editorMode = null">
      <section class="qc-modal qc-editor-modal" role="dialog" aria-modal="true" :aria-label="`QC Chart Basis ${editorMode}`">
        <header class="qc-modal-heading"><div><h2>QC Chart Basis Define ({{ editorMode }})</h2><small>依 WinForms frmQCChartBasisDef 欄位與圖表規格設定</small></div><button class="qc-icon-close" aria-label="關閉" @click="editorMode = null">×</button></header>
        <div class="qc-modal-scroll">
          <fieldset class="qc-fieldset"><legend>QC Chart Basis</legend>
            <div class="qc-form-grid">
              <label>QCItemNo<input v-model="editorDraft.qcItemNo" readonly></label><label>QCItemName<input v-model="editorDraft.qcItemName" readonly></label>
              <label>QC Chart Serial No<input :value="editorDraft.factorSerial || '新增時由系統產生'" readonly></label><label>Chart Type<input :value="chartTypeLabel(activeQcItem?.chartType ?? null)" readonly></label>
              <label>SampleSize <em>*</em><input v-model.number="editorDraft.sampleSize" type="number" min="1" class="required-field"></label><label>KeyNode<select v-model="editorDraft.keyNode" class="required-field"><option value="N">N</option><option value="Y">Y</option></select></label>
              <label>ChartTitle<input v-model="editorDraft.chartTitle"></label><label>Unit<input v-model="editorDraft.chartUnit"></label>
              <label>Sponsor<input v-model="editorDraft.sponsor"></label><label>Plan No<div class="qc-input-action"><select :value="editorDraft.planNo" class="required-field" @change="setPlanNo(($event.target as HTMLSelectElement).value)"><option value="">— 請選擇 —</option><option v-for="plan in planOptions" :key="plan.planNo" :value="plan.planNo">{{ plan.planNo }} {{ plan.planName }}</option><option v-if="editorDraft.planNo && !planOptions.some(plan => plan.planNo === editorDraft.planNo)" :value="editorDraft.planNo">{{ editorDraft.planNo }}（目前資料）</option></select><button class="qc-button compact" @click="getPlanParameters">...</button></div></label>
              <label class="qc-memo-field">MEMO<input v-model="editorDraft.memo" :placeholder="editorMode === 'EDIT' ? '修改時請填寫修改原因' : ''" :class="{ 'required-field': editorMode === 'EDIT' }"></label>
            </div>
            <div class="qc-subform-heading"><b>Factor</b><span>FactorOnLine 要因值；黃色欄位為 FactorSpec 規格要因</span></div>
            <div class="qc-table-scroll qc-factor-scroll"><table class="qc-table"><thead><tr><th>FactorNo</th><th>FactorName</th><th>Value</th><th>ParameterType</th></tr></thead><tbody>
              <tr v-for="factor in activeQcItem?.factors ?? []" :key="factor.factorNo"><td>{{ factor.factorNo }}</td><td>{{ factor.parameterName }}</td><td><input :value="factorValue(factor.factorNo)" :class="{ 'required-field': factor.factorSpec }" @input="updateFactorValue(factor.factorNo, ($event.target as HTMLInputElement).value)"></td><td>{{ factor.parameterType === 2 ? '自由輸入' : '選項' }}</td></tr>
            </tbody></table></div>
            <div class="qc-subform-heading"><b>Sampling Plan Parameter</b><span>PlanNo 綁定的參數值</span></div>
            <div class="qc-table-scroll qc-param-scroll"><table class="qc-table"><thead><tr><th>ParameterNo</th><th>ParameterValue</th></tr></thead><tbody>
              <tr v-for="(parameter, index) in editorDraft.parameters" :key="`${parameter.parameterNo}-${index}`"><td><input v-model="parameter.parameterNo" readonly></td><td><input v-model="parameter.parameterValue"></td></tr><tr v-if="!editorDraft.parameters.length"><td colspan="2" class="qc-empty">尚未載入抽樣計畫參數</td></tr>
            </tbody></table></div>
            <div class="qc-inline-actions"><button class="qc-button compact" @click="getPlanParameters">Get Plan Parameter</button><button class="qc-button compact" @click="clearPlan">Clear Plan</button><button class="qc-button compact" @click="addPlanParameter">新增參數列</button></div>
          </fieldset>

          <fieldset class="qc-fieldset"><legend>QC Chart Detail</legend>
            <p class="qc-spec-note">Specification Type：{{ specLineTypes[activeQcItem?.specLineType ?? 0] }}；黃色欄位為必填規格限值。各管制圖的控制界限及 Y 軸可分別設定。</p>
            <div v-for="detail in editorDraft.details" :key="detail.spcId" class="qc-limit-block">
              <h3>{{ detail.chartName }} <small>SPCID {{ detail.spcId }}</small></h3>
              <div class="qc-limit-grid">
                <label>US<input v-model="detail.us" :disabled="activeQcItem?.chartType === -1 || activeQcItem?.chartType === null" :class="{ 'required-field': detail.spcId === 0 && activeQcItem?.chartType !== -1 && activeQcItem?.specLineType !== 2 }"></label>
                <label>CS<input v-model="detail.cs" :disabled="activeQcItem?.chartType === -1 || activeQcItem?.chartType === null"></label>
                <label>LS<input v-model="detail.ls" :disabled="activeQcItem?.chartType === -1 || activeQcItem?.chartType === null" :class="{ 'required-field': detail.spcId === 0 && activeQcItem?.chartType !== -1 && activeQcItem?.specLineType !== 1 }"></label>
                <label>UCL<input v-model="detail.ucl" :disabled="activeQcItem?.chartType === -1 || activeQcItem?.chartType === null"></label><label>CL<input v-model="detail.cl" :disabled="activeQcItem?.chartType === -1 || activeQcItem?.chartType === null"></label><label>LCL<input v-model="detail.lcl" :disabled="activeQcItem?.chartType === -1 || activeQcItem?.chartType === null"></label>
                <label v-if="detail.spcId === 0">Target<input v-model="detail.target" :disabled="activeQcItem?.chartType === -1 || activeQcItem?.chartType === null"></label><label v-if="detail.spcId === 0">Cpk_Goal<input v-model="detail.cpkGoal" :disabled="activeQcItem?.chartType === -1 || activeQcItem?.chartType === null"></label>
                <label>YAxisFrom<input v-model="detail.yAxisFrom" :disabled="activeQcItem?.chartType === -1 || activeQcItem?.chartType === null"></label><label>YAxisTo<input v-model="detail.yAxisTo" :disabled="activeQcItem?.chartType === -1 || activeQcItem?.chartType === null"></label>
              </div>
            </div>
          </fieldset>
        </div>
        <footer class="qc-modal-footer"><span v-if="error" class="qc-modal-error">{{ error }}</span><div class="qc-footer-buttons"><button class="qc-button primary" :disabled="busy" @click="persistEditor(true)">確定</button><button class="qc-button" :disabled="busy" @click="editorMode = null">關閉</button><button class="qc-button" :disabled="busy" @click="persistEditor(false)">套用</button></div></footer>
      </section>
    </div>

    <div v-if="ruleEditorOpen" class="qc-modal-backdrop" @click.self="ruleEditorOpen = false">
      <section class="qc-modal qc-rule-modal" role="dialog" aria-modal="true" aria-label="QC Chart Rule Define">
        <header class="qc-modal-heading"><div><h2>QC Chart Rule</h2><small>{{ activeBasis?.factorSerial }}　{{ activeBasis?.chartTitle }}</small></div><button class="qc-icon-close" aria-label="關閉" @click="ruleEditorOpen = false">×</button></header>
        <div class="qc-table-scroll"><table class="qc-table"><thead><tr><th>SPCID</th><th>Chart Type</th><th>RuleNo</th><th>RuleName</th><th>Action</th><th></th></tr></thead><tbody>
          <tr v-for="(rule, index) in ruleDraft" :key="index"><td><select v-model.number="rule.spcId" @change="rule.chartName = activeChartName(rule.spcId)"><option v-for="detail in activeBasis?.details ?? []" :key="detail.spcId" :value="detail.spcId">{{ detail.spcId }} - {{ detail.chartName }}</option></select></td><td>{{ rule.chartName }}</td><td><select v-model="rule.ruleNo" @change="rule.ruleName = rule.ruleNo === 'VR-004' ? '連續九點位於中心線同側' : '單點超出管制界限'"><option value="">選擇 RuleNo</option><option value="VR-001">VR-001</option><option value="VR-004">VR-004</option><option value="VR-007">VR-007</option></select></td><td>{{ rule.ruleName }}</td><td><select v-model.number="rule.action"><option :value="0">Warning</option><option :value="1">Wait</option><option :value="2">Hold</option><option :value="3">Alert</option></select></td><td><button class="qc-button compact danger" @click="ruleDraft.splice(index, 1)">刪除</button></td></tr>
          <tr v-if="!ruleDraft.length"><td colspan="6" class="qc-empty">目前沒有已設定的 Rule</td></tr>
        </tbody></table></div>
        <footer class="qc-modal-footer"><button class="qc-button" @click="addRule">新增</button><div class="qc-footer-buttons"><button class="qc-button primary" @click="saveRules">確定</button><button class="qc-button" @click="ruleEditorOpen = false">關閉</button></div></footer>
      </section>
    </div>

    <div v-if="copyDialogOpen" class="qc-modal-backdrop" @click.self="copyDialogOpen = false">
      <section class="qc-modal qc-copy-modal" role="dialog" aria-modal="true" aria-label="Copy QC Chart Basis">
        <header class="qc-modal-heading"><div><h2>Copy QC Chart Basis</h2><small>複製來源項目管制圖至另一個 QC Item</small></div><button class="qc-icon-close" aria-label="關閉" @click="copyDialogOpen = false">×</button></header>
        <div class="qc-copy-content">
          <fieldset class="qc-fieldset"><legend>From QCItem</legend><div class="qc-copy-row"><label>QCItem<input :value="`${activeQcItem?.qcItemNo ?? ''}　${activeQcItem?.qcItemName ?? ''}`" readonly></label></div><label>Copy<select v-model="copyState" class="required-field"><option value="both">UNFROZEN &amp; ACTIVE</option><option value="unfrozen">UNFROZEN</option><option value="active">ACTIVE</option></select></label><p v-if="devMock">符合目前條件的來源筆數：{{ sourceCopyCount }}</p></fieldset>
          <fieldset class="qc-fieldset"><legend>To QCItem</legend><label>QCItem<select v-model="copyTargetNo" class="required-field"><option value="">— 請選擇 —</option><option v-for="item in itemRows.filter(row => row.qcItemNo !== selectedQcItemNo)" :key="item.qcItemNo" :value="item.qcItemNo">{{ item.qcItemNo }}　{{ item.qcItemName }}</option></select></label></fieldset>
        </div>
        <footer class="qc-modal-footer"><span v-if="error" class="qc-modal-error">{{ error }}</span><div class="qc-footer-buttons"><button class="qc-button primary" @click="copyToItem">確定</button><button class="qc-button" @click="copyDialogOpen = false">關閉</button></div></footer>
      </section>
    </div>
  </main>
</template>

<style scoped>
.qc-chart-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:8px;padding:10px 14px 14px;background:#f8f7f3;color:#242424;font-family:Arial,"Microsoft JhengHei",sans-serif;font-size:13px;overflow:auto}
.qc-chart-heading{display:flex;align-items:center;justify-content:space-between;border-bottom:1px solid #d6d2ca;padding:0 2px 8px}.qc-chart-heading h1{font-size:21px;font-weight:600;margin:0}.qc-chart-caption{display:block;color:#777;font-size:11px;margin-top:3px}
.qc-chart-dev-note{padding:7px 10px;background:#fff3cd;border:1px solid #e8d59c;color:#66551d}.qc-chart-message{padding:7px 10px;background:#eff6e8;border:1px solid #c7d6b6;color:#315322}.qc-chart-message.error{background:#fff0ef;border-color:#e1b8b4;color:#8a2720}
.qc-chart-panel{border:1px solid #bcb9b2;background:#fff;min-width:0}.qc-panel-title{height:25px;padding:5px 9px;background:#f2f0eb;border-bottom:1px solid #cbc8c1;font-weight:600}.qc-panel-title span{float:right;color:#786e5b;font-weight:400}.qc-table-scroll{width:100%;overflow:auto}.qc-item-scroll{max-height:155px}.qc-table{border-collapse:collapse;white-space:nowrap;width:max-content;min-width:100%;font-size:12px}.qc-table th{background:#3795d5;color:#fff;font-weight:600;height:26px;padding:4px 8px;border:1px solid #69addd;text-align:left}.qc-table td{height:25px;padding:3px 8px;border:1px solid #d5d1c9;background:#fff}.qc-table tbody tr{cursor:pointer}.qc-table tbody tr:hover td{background:#fff6d8}.qc-table tbody tr.selected td{background:#ffe69a}.qc-table input,.qc-table select{min-width:100px;width:100%;height:25px;border:1px solid #999;padding:3px 5px;background:#fff;color:#242424}.qc-empty{text-align:center!important;color:#888!important;padding:12px!important;cursor:default!important}.qc-grid-footer{height:23px;padding:4px 8px;color:#777;font-size:11px;border-top:1px solid #e4e1db}
.qc-status-bar{display:flex;align-items:center;gap:20px;border:1px solid #c7c3bc;background:#fff;padding:7px 10px;min-height:36px}.qc-radio{display:inline-flex;align-items:center;gap:5px;cursor:pointer}.qc-radio input{accent-color:#ff9f24}.qc-current-item{margin-left:auto;color:#6b675f}.qc-current-item b{color:#242424}
.qc-chart-workspace{display:grid;grid-template-columns:minmax(0,1fr) 114px;gap:8px;min-height:0;flex:1}.qc-chart-left{display:flex;flex-direction:column;gap:8px;min-width:0}.qc-basis-panel{min-height:168px;flex:1}.qc-basis-scroll{max-height:calc(100% - 25px)}.qc-basis-table th,.qc-basis-table td{min-width:84px}.qc-basis-table td:first-child,.qc-basis-table th:first-child{min-width:110px}.qc-lower-panel{height:118px;display:flex;flex-direction:column}.qc-subgrid-scroll{flex:1;min-height:0}.qc-sub-table{white-space:normal}.qc-sub-table th,.qc-sub-table td{min-width:72px}.qc-sub-table td:nth-child(2){min-width:135px}
.qc-actions{display:flex;flex-direction:column;gap:6px;padding-top:0}.qc-button{min-height:31px;padding:5px 10px;border:1px solid #c7c4be;border-radius:3px;background:linear-gradient(#fff,#f5f4f1);color:#302f2c;cursor:pointer;font-size:12px;white-space:nowrap}.qc-button:hover:not(:disabled){border-color:#ff9f24;background:#fff7e7}.qc-button.primary{background:#ff9f24;border-color:#ed941d;color:#fff;font-weight:600}.qc-button.primary:hover:not(:disabled){background:#f39218}.qc-button:disabled{opacity:.48;cursor:default}.qc-button.compact{min-height:26px;padding:3px 8px}.qc-button.danger{color:#9b332a}.qc-action-spacer{flex:1}.qc-state-pill{display:inline-block;border-radius:10px;padding:2px 7px;font-size:10px;background:#eee;color:#555}.state-0{background:#fff0c7;color:#755600}.state-1{background:#e7edf6;color:#375375}.state-2{background:#e6f1df;color:#355c2d}.state--1{background:#eee;color:#666}.qc-audit-preview{display:flex;gap:14px;flex-wrap:wrap;padding:5px 8px;border:1px solid #dedbd4;background:#fff;color:#777;font-size:10px}
.qc-modal-backdrop{position:fixed;inset:0;z-index:1500;background:rgba(20,20,20,.38);display:flex;align-items:center;justify-content:center;padding:18px}.qc-modal{display:flex;flex-direction:column;background:#f8f7f3;border:1px solid #a9a69e;box-shadow:0 8px 30px #0004;max-height:calc(100vh - 36px);min-width:min(780px,95vw)}.qc-editor-modal{width:min(1120px,96vw)}.qc-modal-heading{display:flex;justify-content:space-between;align-items:center;padding:12px 16px;background:#f1efe9;border-bottom:1px solid #c8c5bf}.qc-modal-heading h2{margin:0;font-size:18px;font-weight:600}.qc-modal-heading small{display:block;color:#777;margin-top:3px}.qc-icon-close{border:0;background:transparent;font-size:24px;cursor:pointer;color:#555}.qc-modal-scroll{overflow:auto;padding:10px 12px;display:flex;flex-direction:column;gap:10px}.qc-fieldset{border:1px solid #aaa79f;padding:10px 12px 12px;margin:0;min-width:0}.qc-fieldset legend{padding:0 6px;color:#4b4944;font-weight:600}.qc-form-grid{display:grid;grid-template-columns:1fr 1fr;gap:8px 16px}.qc-form-grid label,.qc-copy-content label{display:flex;flex-direction:column;gap:4px;font-size:12px}.qc-form-grid input,.qc-form-grid select,.qc-limit-grid input,.qc-copy-content select,.qc-copy-content input{height:29px;border:1px solid #999;background:#fff;padding:4px 7px;color:#242424}.qc-form-grid input[readonly]{background:#ededeb;color:#555}.qc-form-grid em{color:#bd422f;font-style:normal}.required-field{background:#fff0aa!important}.qc-memo-field{grid-column:1/-1}.qc-input-action{display:flex;gap:5px}.qc-input-action select{flex:1;min-width:0}.qc-subform-heading{display:flex;justify-content:space-between;align-items:center;margin:12px 0 5px}.qc-subform-heading span,.qc-spec-note{font-size:11px;color:#777}.qc-factor-scroll,.qc-param-scroll{max-height:150px;border:1px solid #c9c6bf}.qc-inline-actions{display:flex;gap:6px;margin-top:6px}.qc-spec-note{margin:0 0 8px}.qc-limit-block{border:1px solid #cfcbc2;background:#fff;margin:8px 0;padding:8px 10px}.qc-limit-block h3{font-size:13px;margin:0 0 7px;font-weight:600}.qc-limit-block h3 small{font-weight:400;color:#777}.qc-limit-grid{display:grid;grid-template-columns:repeat(5,minmax(90px,1fr));gap:7px}.qc-limit-grid label{display:flex;flex-direction:column;gap:3px;font-size:11px;color:#555}.qc-modal-footer{display:flex;justify-content:space-between;align-items:center;gap:10px;padding:9px 12px;background:#f1efe9;border-top:1px solid #c8c5bf}.qc-footer-buttons{display:flex;gap:7px}.qc-modal-error{color:#9b332a;font-size:12px}.qc-rule-modal{width:min(900px,95vw)}.qc-rule-modal>.qc-table-scroll{max-height:55vh}.qc-rule-modal .qc-table{width:100%}.qc-rule-modal .qc-table td{min-width:80px}.qc-rule-modal .qc-table td:nth-child(4){min-width:210px}.qc-copy-modal{width:min(620px,95vw)}.qc-copy-content{padding:14px;display:flex;flex-direction:column;gap:12px}.qc-copy-content fieldset{display:flex;flex-direction:column;gap:10px}.qc-copy-content p{margin:0;color:#777;font-size:11px}.qc-copy-row input{width:100%}
@media(max-width:900px){.qc-chart-page{padding:8px}.qc-chart-workspace{grid-template-columns:minmax(0,1fr)}.qc-actions{display:grid;grid-template-columns:repeat(4,minmax(72px,1fr));padding:0}.qc-action-spacer{display:none}.qc-lower-panel{height:180px}.qc-form-grid{grid-template-columns:1fr}.qc-memo-field{grid-column:auto}.qc-limit-grid{grid-template-columns:repeat(3,minmax(80px,1fr))}.qc-status-bar{gap:10px;flex-wrap:wrap}.qc-current-item{margin-left:0;width:100%}}
</style>
