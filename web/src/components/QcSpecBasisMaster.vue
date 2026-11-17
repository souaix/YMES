<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type IssueState = 0 | 1 | 2 | -1
type EditorMode = 'ADD' | 'EDIT' | 'COPY' | null
type CopyState = 'both' | 'unfrozen' | 'active'
interface QcFactor { factorNo: string; parameterName: string; factorSpec: boolean; parameterType: number; showName?: string }
interface QcItem {
  qcItemNo: string; qcItemName: string; qcDataType: string; chartType: number; specLineType: number
  componentCollect: number; unit: string; lotIsValid: number; factors: QcFactor[]
}
interface SpecDetail { spcId: number; chartName: string; us: string; cs: string; ls: string; ucl: string; cl: string; lcl: string; memo: string }
interface SpecBasis {
  qcSpecSerialNo: string; qcItemNo: string; qcItemName: string; issueState: IssueState; dataStamp: number
  description: string; creator: string; createDate: string; reviser: string; reviseDate: string
  factorValues: Record<string, string>; factorDisplayValues: Record<string, string>; details: SpecDetail[]
  issueJobHasDetail?: boolean
}
interface ApiEnvelope<T> { items?: T[]; rows?: T[]; data?: T }
interface FactorOption { value: string; name: string }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const devMock = computed(() => import.meta.env.DEV && props.developmentPreview)
const stateFilters: { value: IssueState; label: string }[] = [
  { value: 0, label: 'UNFROZEN' }, { value: 1, label: 'PENDING' }, { value: 2, label: 'ACTIVE' }, { value: -1, label: 'UNUSED' },
]
const stateLabel: Record<IssueState, string> = { 0: 'UNFROZEN', 1: 'PENDING', 2: 'ACTIVE', '-1': 'UNUSED' }
const items = ref<QcItem[]>([])
const specs = ref<SpecBasis[]>([])
const selectedQcItemNo = ref('')
const selectedSerial = ref('')
const selectedSerials = ref<string[]>([])
const issueState = ref<IssueState>(0)
const busy = ref(false)
const error = ref('')
const message = ref('')
const editorMode = ref<EditorMode>(null)
const draft = ref<SpecBasis>(blankBasis())
const memo = ref('')
const factorPicker = ref<{ open: boolean; factorNo: string; options: FactorOption[] }>({ open: false, factorNo: '', options: [] })
const copyDialog = ref(false)
const copyTargetNo = ref('')
const copyState = ref<CopyState>('both')
const audit = ref<string[]>([])
const aboveValues = ref<Record<number, string>>({})

function blankBasis(): SpecBasis {
  return { qcSpecSerialNo: '', qcItemNo: '', qcItemName: '', issueState: 0, dataStamp: 0, description: '',
    creator: '', createDate: '', reviser: '', reviseDate: '', factorValues: {}, factorDisplayValues: {}, details: [] }
}
function expectedDetails(item: QcItem | null): SpecDetail[] {
  if (!item) return []
  const ids: { id: number; name: string }[] = item.chartType === 1
    ? [{ id: 0, name: 'Trend Chart' }, { id: 1, name: 'Xbar Chart' }, { id: 3, name: 'S Chart' }]
    : item.chartType === 2
      ? [{ id: 0, name: 'X Chart' }, { id: 4, name: 'Rm Chart' }]
      : item.chartType === -1
        ? [{ id: 0, name: 'Trend Chart' }]
        : [{ id: 0, name: 'Trend Chart' }, { id: 1, name: 'Xbar Chart' }, { id: 2, name: 'R Chart' }]
  return ids.map(row => ({ spcId: row.id, chartName: row.name, us: '', cs: '', ls: '', ucl: '', cl: '', lcl: '', memo: '' }))
}
const activeItem = computed(() => items.value.find(row => row.qcItemNo === selectedQcItemNo.value) ?? null)
const visibleFactors = computed(() => (activeItem.value?.factors ?? []).filter(row => row.factorSpec))
const visibleSpecs = computed(() => specs.value
  .filter(row => row.qcItemNo === selectedQcItemNo.value && row.issueState === issueState.value)
  .sort((a, b) => b.createDate.localeCompare(a.createDate)))
const activeSpec = computed(() => visibleSpecs.value.find(row => row.qcSpecSerialNo === selectedSerial.value) ?? null)
const selectedSpecs = computed(() => visibleSpecs.value.filter(row => selectedSerials.value.includes(row.qcSpecSerialNo)))
const detailRows = computed(() => activeSpec.value?.details ?? [])
const copyCount = computed(() => specs.value.filter(row => row.qcItemNo === selectedQcItemNo.value &&
  (copyState.value === 'both' ? row.issueState === 0 || row.issueState === 2 : copyState.value === 'unfrozen' ? row.issueState === 0 : row.issueState === 2)).length)
const stateName = computed(() => stateFilters.find(row => row.value === issueState.value)?.label ?? '')

function apiUrl(path: string): string {
  if (!props.apiBaseUrl.trim()) throw new Error('目前尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(props.apiBaseUrl.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init })
  if (response.status === 401) {
    window.location.assign(import.meta.env.BASE_URL + 'login')
    throw new Error('登入已逾期，請重新登入。')
  }
  const body = await response.text()
  if (!response.ok) throw new Error(body || 'API 請求失敗 (' + response.status + ')')
  return (body ? JSON.parse(body) : undefined) as T
}
function records<T>(payload: T[] | ApiEnvelope<T>): T[] {
  if (Array.isArray(payload)) return payload
  return payload.items ?? payload.rows ?? []
}
function demoData(): { items: QcItem[]; specs: SpecBasis[] } {
  const itemRows: QcItem[] = [
    { qcItemNo: 'QCV-0101', qcItemName: '晶圓厚度', qcDataType: '計量', chartType: 0, specLineType: 0, componentCollect: 0, unit: 'μm', lotIsValid: 1,
      factors: [{ factorNo: 'PRODUCT', parameterName: '產品編號', factorSpec: true, parameterType: 1, showName: '產品名稱' }, { factorNo: 'MATERIAL', parameterName: '材料編號', factorSpec: true, parameterType: 1 }, { factorNo: 'EQUIPMENT', parameterName: '設備編號', factorSpec: true, parameterType: 1 }] },
    { qcItemNo: 'QCV-0102', qcItemName: '焊點拉力', qcDataType: '計量', chartType: 1, specLineType: 1, componentCollect: 0, unit: 'N', lotIsValid: 1,
      factors: [{ factorNo: 'PRODUCT', parameterName: '產品編號', factorSpec: true, parameterType: 1, showName: '產品名稱' }, { factorNo: 'OP', parameterName: '作業站', factorSpec: true, parameterType: 0 }] },
    { qcItemNo: 'QCV-0103', qcItemName: '點膠寬度', qcDataType: '計量', chartType: 2, specLineType: 2, componentCollect: 1, unit: 'mm', lotIsValid: 1,
      factors: [{ factorNo: 'PRODUCT', parameterName: '產品編號', factorSpec: true, parameterType: 1, showName: '產品名稱' }, { factorNo: 'MATERIAL', parameterName: '材料編號', factorSpec: true, parameterType: 1 }] },
    { qcItemNo: 'QCV-0104', qcItemName: '外觀檢查值', qcDataType: '計量', chartType: -1, specLineType: 3, componentCollect: 0, unit: '級', lotIsValid: 0,
      factors: [{ factorNo: 'PRODUCT', parameterName: '產品編號', factorSpec: true, parameterType: 1, showName: '產品名稱' }] },
  ]
  const build = (item: QcItem, serial: string, state: IssueState, values: Record<string, string>, day: number): SpecBasis => {
    const details = expectedDetails(item).map((row, index) => ({
      ...row, us: index === 0 ? '18.0000' : '', cs: index === 0 ? '15.0000' : '', ls: index === 0 ? '12.0000' : '',
      ucl: index === 0 ? '17.5000' : '16.5000', cl: index === 0 ? '15.0000' : '15.0000', lcl: index === 0 ? '12.5000' : '13.5000',
      memo: 'DEV 規格明細',
    }))
    const factorDisplayValues: Record<string, string> = {}
    if (values.PRODUCT) factorDisplayValues.PRODUCT = values.PRODUCT.endsWith('A') ? '晶圓產品 A' : '晶圓產品 B'
    return { ...blankBasis(), qcSpecSerialNo: serial, qcItemNo: item.qcItemNo, qcItemName: item.qcItemName, issueState: state, dataStamp: 1,
      description: 'DEV 品管規格說明', creator: 'A11019', createDate: '2026-11-' + String(day).padStart(2, '0') + ' 10:30:00',
      reviser: 'A11019', reviseDate: '2026-11-' + String(day).padStart(2, '0') + ' 10:45:00', factorValues: values, factorDisplayValues, details }
  }
  return { items: itemRows, specs: [
    build(itemRows[0], 'QCV-0101-000000001', 0, { PRODUCT: 'PROD-A', MATERIAL: 'MAT-01', EQUIPMENT: 'EQP-01' }, 17),
    build(itemRows[0], 'QCV-0101-000000002', 2, { PRODUCT: 'PROD-B', MATERIAL: 'MAT-02', EQUIPMENT: 'EQP-02' }, 16),
    build(itemRows[0], 'QCV-0101-000000003', 1, { PRODUCT: '*', MATERIAL: 'MAT-01', EQUIPMENT: '*' }, 15),
    build(itemRows[1], 'QCV-0102-000000001', 0, { PRODUCT: 'PROD-A', OP: 'OP-ASSY-01' }, 14),
    build(itemRows[2], 'QCV-0103-000000001', -1, { PRODUCT: '*', MATERIAL: 'MAT-01' }, 13),
  ] }
}
async function loadPage(): Promise<void> {
  busy.value = true; error.value = ''
  try {
    if (devMock.value) {
      const demo = demoData(); items.value = demo.items; specs.value = demo.specs
    } else {
      items.value = records(await request<QcItem[] | ApiEnvelope<QcItem>>('/api/qc/spec-bases/qc-items'))
    }
    if (!selectedQcItemNo.value && items.value.length) selectedQcItemNo.value = items.value[0].qcItemNo
    await loadSpecs()
    selectFirst()
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '載入品管規格失敗。' }
  finally { busy.value = false }
}
async function loadSpecs(): Promise<void> {
  if (!activeItem.value || devMock.value) return
  const query = new URLSearchParams({ qcItemNo: activeItem.value.qcItemNo, issueState: String(issueState.value) })
  const result = await request<SpecBasis[] | ApiEnvelope<SpecBasis>>('/api/qc/spec-bases?' + query)
  specs.value = [...specs.value.filter(row => !(row.qcItemNo === activeItem.value?.qcItemNo && row.issueState === issueState.value)), ...records(result)]
}
function selectFirst(): void {
  selectedSerial.value = visibleSpecs.value[0]?.qcSpecSerialNo ?? ''
  selectedSerials.value = selectedSerial.value ? [selectedSerial.value] : []
}
async function chooseItem(item: QcItem): Promise<void> {
  selectedQcItemNo.value = item.qcItemNo; selectedSerial.value = ''; selectedSerials.value = []; error.value = ''; message.value = ''
  try { await loadSpecs(); selectFirst() } catch (reason) { error.value = reason instanceof Error ? reason.message : '載入規格資料失敗。' }
}
async function setIssueState(value: IssueState): Promise<void> {
  issueState.value = value; error.value = ''; message.value = ''
  try { await loadSpecs(); selectFirst() } catch (reason) { error.value = reason instanceof Error ? reason.message : '載入狀態資料失敗。' }
}
async function chooseSpec(row: SpecBasis, event?: MouseEvent): Promise<void> {
  selectedSerial.value = row.qcSpecSerialNo
  if (event?.ctrlKey || event?.metaKey) {
    selectedSerials.value = selectedSerials.value.includes(row.qcSpecSerialNo)
      ? selectedSerials.value.filter(serial => serial !== row.qcSpecSerialNo)
      : [...selectedSerials.value, row.qcSpecSerialNo]
  } else selectedSerials.value = [row.qcSpecSerialNo]
  await loadDetails(row)
}
async function loadDetails(row: SpecBasis): Promise<void> {
  if (devMock.value) return
  try {
    row.details = records(await request<SpecDetail[] | ApiEnvelope<SpecDetail>>('/api/qc/spec-bases/' + encodeURIComponent(row.qcSpecSerialNo) + '/details'))
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '讀取規格明細失敗。' }
}
async function toggleSpecSelection(row: SpecBasis, event: Event): Promise<void> {
  selectedSerial.value = row.qcSpecSerialNo
  const checked = (event.target as HTMLInputElement).checked
  selectedSerials.value = checked
    ? [...new Set([...selectedSerials.value, row.qcSpecSerialNo])]
    : selectedSerials.value.filter(serial => serial !== row.qcSpecSerialNo)
  await loadDetails(row)
}
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function openEditor(mode: Exclude<EditorMode, null>): void {
  error.value = ''; message.value = ''
  if (!activeItem.value) { error.value = '請先選擇 QC Item。'; return }
  if (mode !== 'ADD' && !activeSpec.value) { error.value = '請先選擇品管規格。'; return }
  if (mode === 'EDIT' && activeSpec.value?.issueState !== 0) { error.value = '只有 UNFROZEN 規格可修改。'; return }
  if (mode === 'ADD') {
    draft.value = { ...blankBasis(), qcItemNo: activeItem.value.qcItemNo, qcItemName: activeItem.value.qcItemName,
      factorValues: Object.fromEntries(visibleFactors.value.map(row => [row.factorNo, ''])), details: expectedDetails(activeItem.value) }
  } else {
    draft.value = clone(activeSpec.value!)
    if (mode === 'COPY') {
      draft.value.qcSpecSerialNo = ''; draft.value.issueState = 0; draft.value.dataStamp = 0
      draft.value.details = clone(activeSpec.value!.details)
    }
  }
  aboveValues.value = Object.fromEntries(draft.value.details.map(row => [row.spcId,
    row.us && row.ls && Number.isFinite(Number(row.us)) && Number.isFinite(Number(row.ls)) ? String((Number(row.us) - Number(row.ls)) / 2) : '']))
  memo.value = ''
  editorMode.value = mode
}
function currentFactors(itemNo: string): QcFactor[] { return items.value.find(row => row.qcItemNo === itemNo)?.factors.filter(factor => factor.factorSpec) ?? [] }
function hasDuplicate(itemNo: string, values: Record<string, string>, exceptSerial = ''): boolean {
  const factors = currentFactors(itemNo).map(row => row.factorNo)
  return specs.value.some(row => row.qcItemNo === itemNo && row.qcSpecSerialNo !== exceptSerial &&
    factors.every(name => (row.factorValues[name] ?? '') === (values[name] ?? '')))
}
function validateDraft(requireMemo: boolean): string {
  const item = activeItem.value
  if (!item) return '請先選擇 QC Item。'
  if (requireMemo && editorMode.value === 'EDIT' && !memo.value.trim()) return '修改時請填寫 MEMO 作為修改原因。'
  for (const detail of draft.value.details) {
    for (const key of ['us', 'cs', 'ls', 'ucl', 'cl', 'lcl'] as const) {
      const value = detail[key].trim()
      if (value && !Number.isFinite(Number(value))) return detail.chartName + ' 的 ' + key.toUpperCase() + ' 必須是數值。'
    }
    const ordered = [detail.us, detail.ucl, detail.cl, detail.lcl, detail.ls].filter(value => value.trim()).map(Number)
    for (let index = 1; index < ordered.length; index++) if (ordered[index - 1] < ordered[index]) return detail.chartName + ' 的規格順序需符合 US ≥ UCL ≥ CL ≥ LCL ≥ LS。'
  }
  const first = draft.value.details[0]
  if (first && item.specLineType === 0 && (!first.us.trim() || !first.ls.trim())) return '雙邊規格必須填寫 US 與 LS。'
  if (first && item.specLineType === 1 && !first.us.trim()) return '上限規格必須填寫 US。'
  if (first && item.specLineType === 2 && !first.ls.trim()) return '下限規格必須填寫 LS。'
  return ''
}
function factorValue(no: string): string { return draft.value.factorValues[no] ?? '' }
function setFactorValue(no: string, value: string, name = ''): void {
  draft.value.factorValues[no] = value
  draft.value.factorDisplayValues[no] = name
}
function calculateFromCenter(row: SpecDetail, helper: 'center' | 'above', value: string): void {
  if (helper === 'center') row.cs = value
  else if (value && Number.isFinite(Number(value)) && Number(value) < 0) { error.value = 'Above 必須為大於或等於 0 的數值。'; return }
  if (helper === 'above') aboveValues.value[row.spcId] = value
  const aboveValue = helper === 'above' ? value : aboveValues.value[row.spcId]
  if (row.cs.trim() && aboveValue && Number.isFinite(Number(row.cs)) && Number.isFinite(Number(aboveValue))) {
    const above = Number(aboveValue)
    row.us = String(Number(row.cs) + above); row.ls = String(Number(row.cs) - above)
  }
}
async function openFactorPicker(factor: QcFactor): Promise<void> {
  error.value = ''
  try {
    const options = devMock.value
      ? factor.factorNo === 'PRODUCT' ? [{ value: '*', name: '全部產品' }, { value: 'PROD-A', name: '晶圓產品 A' }, { value: 'PROD-B', name: '晶圓產品 B' }]
        : factor.factorNo === 'EQUIPMENT' ? [{ value: '*', name: '全部設備' }, { value: 'EQP-01', name: '設備 01' }, { value: 'EQP-02', name: '設備 02' }]
          : [{ value: '*', name: '全部' }, { value: factor.factorNo + '-01', name: factor.parameterName + ' 01' }, { value: factor.factorNo + '-02', name: factor.parameterName + ' 02' }]
      : records(await request<FactorOption[] | ApiEnvelope<FactorOption>>('/api/qc/spec-bases/factors/' + encodeURIComponent(factor.factorNo) + '/options?' + new URLSearchParams({ qcItemNo: activeItem.value?.qcItemNo ?? '' })))
    factorPicker.value = { open: true, factorNo: factor.factorNo, options }
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '讀取因子選項失敗。' }
}
function recordAudit(action: string, serial: string): void {
  audit.value.unshift(new Date().toLocaleString('zh-TW', { hour12: false }) + ' ' + (props.currentUserNo || 'DEV001') + ' ' + action + ' ' + serial)
}
function basisCommand(concurrency: boolean): Record<string, unknown> {
  const details = draft.value.details.map(row => ({ spcId: row.spcId, us: row.us, cs: row.cs, ls: row.ls, ucl: row.ucl, cl: row.cl, lcl: row.lcl, memo: memo.value }))
  const command: Record<string, unknown> = { qcItemNo: draft.value.qcItemNo, description: draft.value.description, factorValues: draft.value.factorValues, details, memo: memo.value }
  if (concurrency) { command.issueState = draft.value.issueState; command.dataStamp = draft.value.dataStamp }
  return command
}
async function saveEditor(closeAfter: boolean): Promise<void> {
  const validation = validateDraft(closeAfter)
  if (validation) { error.value = validation; return }
  busy.value = true; error.value = ''
  try {
    if (devMock.value) {
      const isNew = editorMode.value !== 'EDIT'
      if (isNew && hasDuplicate(draft.value.qcItemNo, draft.value.factorValues)) throw new Error('相同 QC Item 與規格因子組合已存在。')
      if (isNew) {
        draft.value.qcSpecSerialNo = 'DEV-QCSN-' + String(Date.now()).slice(-7)
        draft.value.issueState = 0; draft.value.dataStamp = 1
        draft.value.creator = props.currentUserNo || 'DEV001'; draft.value.createDate = new Date().toLocaleString('sv-SE')
        if (editorMode.value === 'COPY') recordAudit('複製品管規格', draft.value.qcSpecSerialNo)
        else recordAudit('新增品管規格', draft.value.qcSpecSerialNo)
        specs.value.push(clone(draft.value))
      } else {
        draft.value.dataStamp += 1; draft.value.reviser = props.currentUserNo || 'DEV001'; draft.value.reviseDate = new Date().toLocaleString('sv-SE')
        const index = specs.value.findIndex(row => row.qcSpecSerialNo === draft.value.qcSpecSerialNo)
        if (index >= 0) specs.value[index] = clone(draft.value)
        recordAudit('修改品管規格', draft.value.qcSpecSerialNo)
      }
    } else if (editorMode.value === 'EDIT') {
      await request('/api/qc/spec-bases/' + encodeURIComponent(draft.value.qcSpecSerialNo), { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(basisCommand(true)) })
    } else {
      const saved = await request<SpecBasis>('/api/qc/spec-bases', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(basisCommand(false)) })
      if (saved?.qcSpecSerialNo) draft.value.qcSpecSerialNo = saved.qcSpecSerialNo
    }
    if (closeAfter) editorMode.value = null
    await loadSpecs()
    if (draft.value.qcSpecSerialNo) {
      selectedSerial.value = draft.value.qcSpecSerialNo
      selectedSerials.value = [draft.value.qcSpecSerialNo]
      const row = visibleSpecs.value.find(value => value.qcSpecSerialNo === draft.value.qcSpecSerialNo)
      if (row && !devMock.value) await chooseSpec(row)
    }
    message.value = '資料已儲存；新資料狀態為 UNFROZEN。'
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '儲存品管規格失敗。' }
  finally { busy.value = false }
}
async function deleteSpec(): Promise<void> {
  const targets = selectedSpecs.value.length ? selectedSpecs.value : activeSpec.value ? [activeSpec.value] : []
  if (!targets.length) { error.value = '請先選擇品管規格。'; return }
  if (targets.some(row => row.issueState === 1)) { error.value = '選取資料含 PENDING 狀態，不可刪除。'; return }
  if (!window.confirm('確定處理選取的 ' + targets.length + ' 筆規格？ACTIVE 會改標 UNUSED。')) return
  let completed = 0
  try {
    for (const row of targets) {
      if (devMock.value) {
        if (row.issueState === 2) row.issueState = -1
        else specs.value = specs.value.filter(item => item.qcSpecSerialNo !== row.qcSpecSerialNo)
        recordAudit('刪除品管規格', row.qcSpecSerialNo)
      } else await request('/api/qc/spec-bases/' + encodeURIComponent(row.qcSpecSerialNo), { method: 'DELETE', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ issueState: row.issueState, dataStamp: row.dataStamp }) })
      completed++
    }
    await loadSpecs(); selectFirst(); message.value = '已處理 ' + completed + ' 筆規格。'
  } catch (reason) { error.value = '已完成 ' + completed + ' 筆；' + (reason instanceof Error ? reason.message : '刪除失敗。') }
}
async function approveSpec(): Promise<void> {
  const targets = selectedSpecs.value.length ? selectedSpecs.value : activeSpec.value ? [activeSpec.value] : []
  if (!targets.length || targets.some(row => row.issueState !== 0)) { error.value = '選取資料必須全為 UNFROZEN 才可核准。'; return }
  if (!window.confirm('確定核准選取的 ' + targets.length + ' 筆規格？')) return
  let completed = 0
  try {
    for (const row of targets) {
      if (devMock.value) {
        row.issueState = row.issueJobHasDetail ? 1 : 2; row.dataStamp += 1
        row.reviser = props.currentUserNo || 'DEV001'; row.reviseDate = new Date().toLocaleString('sv-SE')
        recordAudit('核准品管規格', row.qcSpecSerialNo)
      } else await request('/api/qc/spec-bases/' + encodeURIComponent(row.qcSpecSerialNo) + '/approve', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ dataStamp: row.dataStamp }) })
      completed++
    }
    await loadSpecs(); selectFirst(); message.value = '已提交 ' + completed + ' 筆核准；未設定簽核工作時直接成為 ACTIVE。'
  } catch (reason) { error.value = '已完成 ' + completed + ' 筆；' + (reason instanceof Error ? reason.message : '核准失敗。') }
}
async function disapproveSpec(): Promise<void> {
  const targets = selectedSpecs.value.length ? selectedSpecs.value : activeSpec.value ? [activeSpec.value] : []
  if (!targets.length || targets.some(row => row.issueState !== 2)) { error.value = '選取資料必須全為 ACTIVE 才可取消核准。'; return }
  if (!window.confirm('取消核准選取的 ' + targets.length + ' 筆規格？')) return
  let completed = 0
  try {
    for (const row of targets) {
      if (devMock.value) { row.issueState = 0; row.dataStamp += 1; recordAudit('取消核准', row.qcSpecSerialNo) }
      else await request('/api/qc/spec-bases/' + encodeURIComponent(row.qcSpecSerialNo) + '/disapprove', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ issueState: 2, dataStamp: row.dataStamp }) })
      completed++
    }
    await loadSpecs(); selectFirst(); message.value = '已將 ' + completed + ' 筆規格退回 UNFROZEN。'
  } catch (reason) { error.value = '已完成 ' + completed + ' 筆；' + (reason instanceof Error ? reason.message : '取消核准失敗。') }
}
function openCopyDialog(): void {
  if (!activeItem.value) { error.value = '請先選擇來源 QC Item。'; return }
  copyTargetNo.value = ''; copyState.value = 'both'; copyDialog.value = true
}
async function copyToItem(): Promise<void> {
  const source = activeItem.value; const target = items.value.find(row => row.qcItemNo === copyTargetNo.value)
  if (!source || !target) { error.value = '請選擇目標 QC Item。'; return }
  if (source.qcItemNo === target.qcItemNo) { error.value = '來源與目標 QC Item 不可相同。'; return }
  const sourceSpecs = source.factors.filter(row => row.factorSpec).map(row => row.factorNo).sort().join('|')
  const targetSpecs = target.factors.filter(row => row.factorSpec).map(row => row.factorNo).sort().join('|')
  if (sourceSpecs !== targetSpecs) { error.value = '來源與目標 QC Item 的 Spec Factor 不完全相同，無法複製。'; return }
  try {
    let count = 0
    if (devMock.value) {
      const chosen = specs.value.filter(row => row.qcItemNo === source.qcItemNo &&
        (copyState.value === 'both' ? row.issueState === 0 || row.issueState === 2 : copyState.value === 'unfrozen' ? row.issueState === 0 : row.issueState === 2))
      if (!chosen.length) { error.value = '目前沒有符合狀態的規格可複製。'; return }
      for (const row of chosen) {
        if (hasDuplicate(target.qcItemNo, row.factorValues)) continue
        const copy = clone(row); copy.qcSpecSerialNo = 'DEV-QCSN-' + String(Date.now() + count).slice(-7)
        copy.qcItemNo = target.qcItemNo; copy.qcItemName = target.qcItemName; copy.issueState = 0; copy.dataStamp = 1
        copy.creator = props.currentUserNo || 'DEV001'; copy.createDate = new Date().toLocaleString('sv-SE')
        specs.value.push(copy); count++
      }
      recordAudit('跨 QC Item 複製', source.qcItemNo + ' → ' + target.qcItemNo)
    } else {
      const copyType = copyState.value === 'both' ? 3 : copyState.value === 'unfrozen' ? 0 : 2
      const result = await request<{ copiedCount?: number }>('/api/qc/spec-bases/copy', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ fromQcItemNo: source.qcItemNo, toQcItemNo: target.qcItemNo, copyType }) })
      count = result?.copiedCount ?? 0
    }
    copyDialog.value = false; await loadSpecs(); message.value = '已複製 ' + count + ' 筆規格至 ' + target.qcItemNo + '；副本均為 UNFROZEN。'
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '跨項目複製失敗。' }
}
function clearFactor(factorNo: string): void { setFactorValue(factorNo, '', '') }
onMounted(() => { void loadPage() })
</script>

<template>
  <main class="qc-spec-page">
    <div class="qc-spec-heading">
      <div><h1>品管項目規格</h1><span>QC SPEC BASIS　｜　QC303</span></div>
      <button class="qc-button" @click="emit('close')">關閉</button>
    </div>
    <div v-if="devMock" class="qc-dev-note">DEV 假資料模式：本頁變更只作用於記憶體假資料。</div>
    <div v-if="error" class="qc-message error" role="alert">{{ error }}</div>
    <div v-if="message" class="qc-message">{{ message }}</div>

    <section class="qc-panel qc-item-panel">
      <div class="qc-panel-title">計量品管項目</div>
      <div class="qc-scroll qc-item-scroll"><table class="qc-table">
        <thead><tr><th>QCItemNo</th><th>QCItemName</th><th>QCDataType</th><th>ChartType</th><th>SpecLineType</th><th>ComponentCollect</th><th>Unit</th><th>LotIsValid</th></tr></thead>
        <tbody>
          <tr v-for="item in items" :key="item.qcItemNo" :class="{ selected: item.qcItemNo === selectedQcItemNo }" @click="chooseItem(item)">
            <td>{{ item.qcItemNo }}</td><td>{{ item.qcItemName }}</td><td>{{ item.qcDataType }}</td><td>{{ item.chartType }}</td><td>{{ item.specLineType }}</td><td>{{ item.componentCollect }}</td><td>{{ item.unit }}</td><td>{{ item.lotIsValid }}</td>
          </tr>
          <tr v-if="!items.length"><td colspan="8" class="qc-empty">{{ busy ? '載入中…' : '沒有可用的計量 QC Item' }}</td></tr>
        </tbody>
      </table></div>
    </section>

    <section class="qc-status-bar" aria-label="規格狀態篩選">
      <label v-for="filter in stateFilters" :key="filter.value"><input type="radio" name="qc-spec-state" :checked="issueState === filter.value" @change="setIssueState(filter.value)"> {{ filter.label }}</label>
      <span class="qc-current-item">目前 QC Item：<b>{{ activeItem?.qcItemNo || '未選擇' }}</b>　{{ activeItem?.qcItemName }}</span>
    </section>

    <div class="qc-workspace">
      <div class="qc-left">
        <section class="qc-panel qc-basis-panel">
          <div class="qc-panel-title">品管項目規格 <span>{{ stateName }}</span></div>
          <div class="qc-scroll qc-basis-scroll"><table class="qc-table qc-basis-table">
            <thead><tr><th>選取</th>
              <template v-for="factor in visibleFactors" :key="factor.factorNo">
                <th>{{ factor.factorNo }}</th><th v-if="factor.showName">{{ factor.showName }}</th>
              </template>
              <th>Creator</th><th>CreateDate</th>
            </tr></thead>
            <tbody>
              <tr v-for="row in visibleSpecs" :key="row.qcSpecSerialNo" :class="{ selected: row.qcSpecSerialNo === selectedSerial }" @click="chooseSpec(row, $event)">
                <td><input type="checkbox" :checked="selectedSerials.includes(row.qcSpecSerialNo)" aria-label="選取品管規格" @click.stop @change="toggleSpecSelection(row, $event)"></td>
                <template v-for="factor in visibleFactors" :key="factor.factorNo">
                  <td>{{ row.factorValues[factor.factorNo] || '—' }}</td><td v-if="factor.showName">{{ row.factorDisplayValues[factor.factorNo] || '—' }}</td>
                </template>
                <td>{{ row.creator }}</td><td>{{ row.createDate }}</td>
              </tr>
              <tr v-if="!visibleSpecs.length"><td :colspan="3 + visibleFactors.length + visibleFactors.filter(factor => factor.showName).length" class="qc-empty">{{ busy ? '載入中…' : '此狀態沒有規格資料' }}</td></tr>
            </tbody>
          </table></div>
        </section>

        <section class="qc-panel qc-detail-panel">
          <div class="qc-panel-title">Detail</div>
          <div class="qc-scroll qc-detail-scroll"><table class="qc-table">
            <thead><tr><th>SPCIDShow</th><th>US</th><th>CS</th><th>LS</th><th>UCL</th><th>CL</th><th>LCL</th><th>MEMO</th></tr></thead>
            <tbody>
              <tr v-for="row in detailRows" :key="row.spcId"><td>{{ row.chartName }}</td><td>{{ row.us }}</td><td>{{ row.cs }}</td><td>{{ row.ls }}</td><td>{{ row.ucl }}</td><td>{{ row.cl }}</td><td>{{ row.lcl }}</td><td>{{ row.memo }}</td></tr>
              <tr v-if="!detailRows.length"><td colspan="8" class="qc-empty">請選擇一筆品管規格</td></tr>
            </tbody>
          </table></div>
        </section>
      </div>

      <aside class="qc-actions">
        <button class="qc-button primary" :disabled="!activeItem || busy" @click="openEditor('ADD')">ADD (&amp;A)</button>
        <button class="qc-button" :disabled="!activeSpec || busy" @click="openEditor('EDIT')">EDIT (&amp;E)</button>
        <button class="qc-button" :disabled="(!selectedSpecs.length && !activeSpec) || busy" @click="deleteSpec">DEL (&amp;D)</button>
        <button class="qc-button" :disabled="!activeSpec || busy" @click="openEditor('COPY')">Copy (&amp;C)</button>
        <button class="qc-button" :disabled="(!selectedSpecs.length && !activeSpec) || busy" @click="approveSpec">Approve (&amp;Y)</button>
        <button class="qc-button" :disabled="(!selectedSpecs.length && !activeSpec) || busy" @click="disapproveSpec">Disapprove (&amp;U)</button>
        <button class="qc-button" :disabled="!activeItem || busy" @click="openCopyDialog">Copy2</button>
        <div class="qc-action-spacer"></div>
        <button class="qc-button" @click="emit('close')">Close (&amp;X)</button>
      </aside>
    </div>

    <section v-if="devMock && audit.length" class="qc-audit"><b>DEV 操作記錄</b><span v-for="entry in audit.slice(0, 4)" :key="entry">{{ entry }}</span></section>

    <div v-if="editorMode" class="qc-modal-backdrop" @click.self="editorMode = null">
      <section class="qc-modal qc-editor" role="dialog" aria-modal="true" aria-label="QC Item Spec Define">
        <header class="qc-modal-heading"><div><h2>QC Item Spec ({{ editorMode }})</h2><small>依 WinForms frmQCSpecBasisDef 欄位編輯</small></div><button class="qc-icon-close" aria-label="關閉" @click="editorMode = null">×</button></header>
        <div class="qc-modal-scroll">
          <fieldset class="qc-fieldset"><legend>QCItem Spec</legend>
            <div class="qc-form-grid">
              <label>QCItem<input :value="draft.qcItemNo" readonly></label><label>QCItem Name<input :value="draft.qcItemName" readonly></label>
              <label>QC Spec Serial No<input :value="draft.qcSpecSerialNo || '系統產生'" readonly></label><label>IssueState<input value="UNFROZEN" readonly></label>
            </div>
            <div class="qc-subform-heading"><b>Factor</b><span>只列出此 QC Item 的 FactorSpec=Y 欄位</span></div>
            <div class="qc-scroll qc-factor-scroll"><table class="qc-table qc-factor-table">
              <thead><tr><th>FactorNo</th><th>FactorName</th><th>Value</th><th>Name</th><th>選取</th></tr></thead>
              <tbody><tr v-for="factor in visibleFactors" :key="factor.factorNo">
                <td>{{ factor.factorNo }}</td><td>{{ factor.parameterName }}</td>
                <td><input :value="factorValue(factor.factorNo)" @input="setFactorValue(factor.factorNo, ($event.target as HTMLInputElement).value, draft.factorDisplayValues[factor.factorNo] || '')"></td>
                <td>{{ draft.factorDisplayValues[factor.factorNo] || '—' }}</td>
                <td><button v-if="factor.parameterType !== 2" class="qc-button compact" @click="openFactorPicker(factor)">...</button><button class="qc-button compact" @click="clearFactor(factor.factorNo)">Clear</button></td>
              </tr><tr v-if="!visibleFactors.length"><td colspan="5" class="qc-empty">此 QC Item 沒有規格因子</td></tr></tbody>
            </table></div>
            <button v-if="factorPicker.open" class="qc-inline-picker-close" @click="factorPicker.open = false">關閉因子選單</button>
            <div v-if="factorPicker.open" class="qc-factor-options"><b>{{ factorPicker.factorNo }} 選項</b><button v-for="option in factorPicker.options" :key="option.value" class="qc-button compact" @click="setFactorValue(factorPicker.factorNo, option.value, option.name); factorPicker.open = false">{{ option.value }}　{{ option.name }}</button></div>
            <div class="qc-form-grid qc-description-grid"><label>Description<textarea v-model="draft.description" rows="3"></textarea></label></div>
            <label class="qc-memo">MEMO（修改原因）<input v-model="memo" :class="{ 'required-field': editorMode === 'EDIT' }" :placeholder="editorMode === 'EDIT' ? 'Confirm 時必填；Apply 可留白' : ''"></label>
          </fieldset>

          <fieldset class="qc-fieldset"><legend>QCItem Spec Detail</legend>
            <p class="qc-note">ChartType：{{ activeItem?.chartType }}　｜　SpecLineType：{{ activeItem?.specLineType }}（0雙邊、1上限、2下限、3無管制）。Center + Above 會計算 US / LS。</p>
            <div class="qc-scroll qc-limit-scroll"><table class="qc-table qc-limit-table">
              <thead><tr><th>Chart</th><th>US</th><th>Center (CS)</th><th>Above</th><th>LS</th><th>UCL</th><th>CL</th><th>LCL</th></tr></thead>
              <tbody><tr v-for="row in draft.details" :key="row.spcId">
                <td>{{ row.chartName }}</td>
                <td><input v-model="row.us" :class="{ 'required-field': row === draft.details[0] && (activeItem?.specLineType === 0 || activeItem?.specLineType === 1) }"></td>
                <td><input v-model="row.cs" @change="calculateFromCenter(row, 'center', row.cs)"></td>
                <td><input :value="aboveValues[row.spcId] || ''" @input="aboveValues[row.spcId] = ($event.target as HTMLInputElement).value" @change="calculateFromCenter(row, 'above', aboveValues[row.spcId])"></td>
                <td><input v-model="row.ls" :class="{ 'required-field': row === draft.details[0] && (activeItem?.specLineType === 0 || activeItem?.specLineType === 2) }"></td>
                <td><input v-model="row.ucl"></td><td><input v-model="row.cl"></td><td><input v-model="row.lcl"></td>
              </tr></tbody>
            </table></div>
          </fieldset>

          <fieldset class="qc-fieldset qc-audit-fields"><legend>Basis Data</legend>
            <label>Creator<input :value="draft.creator || currentUserNo || '登入使用者'" readonly></label><label>CreateDate<input :value="draft.createDate || '儲存時由系統產生'" readonly></label>
            <label>Reviser<input :value="draft.reviser || '儲存時由系統產生'" readonly></label><label>ReviseDate<input :value="draft.reviseDate || '儲存時由系統產生'" readonly></label>
          </fieldset>
        </div>
        <footer class="qc-modal-footer"><span v-if="error" class="qc-modal-error">{{ error }}</span><div class="qc-footer-buttons">
          <button class="qc-button primary" :disabled="busy" @click="saveEditor(true)">Confirm</button><button class="qc-button" :disabled="busy" @click="editorMode = null">Close</button><button class="qc-button" :disabled="busy" @click="saveEditor(false)">Apply</button>
        </div></footer>
      </section>
    </div>

    <div v-if="copyDialog" class="qc-modal-backdrop" @click.self="copyDialog = false">
      <section class="qc-modal qc-copy-modal" role="dialog" aria-modal="true" aria-label="Copy QC Item Spec">
        <header class="qc-modal-heading"><div><h2>Copy QC Item Spec</h2><small>跨 QC Item 複製規格</small></div><button class="qc-icon-close" aria-label="關閉" @click="copyDialog = false">×</button></header>
        <div class="qc-copy-content">
          <fieldset class="qc-fieldset"><legend>From QCItem</legend><label>QCItem<input :value="activeItem?.qcItemNo + '　' + activeItem?.qcItemName" readonly></label>
            <label>Copy<select v-model="copyState"><option value="both">Unfrozen &amp; Active</option><option value="unfrozen">Unfrozen</option><option value="active">Active</option></select></label><p v-if="devMock">符合狀態的來源規格：{{ copyCount }}</p>
          </fieldset>
          <fieldset class="qc-fieldset"><legend>To QCItem</legend><label>QCItem<select v-model="copyTargetNo"><option value="">— 請選擇 —</option><option v-for="item in items.filter(row => row.qcItemNo !== selectedQcItemNo)" :key="item.qcItemNo" :value="item.qcItemNo">{{ item.qcItemNo }}　{{ item.qcItemName }}</option></select></label></fieldset>
        </div>
        <footer class="qc-modal-footer"><span v-if="error" class="qc-modal-error">{{ error }}</span><div class="qc-footer-buttons"><button class="qc-button primary" @click="copyToItem">Confirm</button><button class="qc-button" @click="copyDialog = false">Close</button></div></footer>
      </section>
    </div>
  </main>
</template>

<style scoped>
.qc-spec-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:7px;padding:10px 14px 14px;background:#f8f7f3;color:#242424;font:13px Arial,"Microsoft JhengHei",sans-serif;overflow:auto}
.qc-spec-heading{display:flex;align-items:center;justify-content:space-between;border-bottom:1px solid #d6d2ca;padding:0 2px 7px}.qc-spec-heading h1{font-size:21px;font-weight:600;margin:0}.qc-spec-heading span{display:block;color:#777;font-size:11px;margin-top:3px}
.qc-dev-note{padding:6px 10px;background:#fff3cd;border:1px solid #e8d59c;color:#66551d}.qc-message{padding:6px 10px;background:#eff6e8;border:1px solid #c7d6b6;color:#315322}.qc-message.error{background:#fff0ef;border-color:#e1b8b4;color:#8a2720}
.qc-panel{border:1px solid #bcb9b2;background:#fff;min-width:0}.qc-panel-title{height:25px;padding:5px 9px;background:#f2f0eb;border-bottom:1px solid #cbc8c1;font-weight:600}.qc-panel-title span{float:right;color:#786e5b;font-weight:400}
.qc-scroll{width:100%;overflow:auto}.qc-item-panel{flex:0 0 160px}.qc-item-scroll{max-height:135px}.qc-table{border-collapse:collapse;white-space:nowrap;width:max-content;min-width:100%;font-size:12px}.qc-table th{background:#3795d5;color:#fff;font-weight:600;height:26px;padding:4px 8px;border:1px solid #69addd;text-align:left}.qc-table td{height:25px;padding:3px 8px;border:1px solid #d5d1c9;background:#fff}.qc-table tbody tr{cursor:pointer}.qc-table tbody tr:hover td{background:#fff6d8}.qc-table tbody tr.selected td{background:#ffe69a}.qc-empty{text-align:center!important;color:#888!important;padding:12px!important;cursor:default!important}
.qc-status-bar{display:flex;align-items:center;gap:18px;border:1px solid #c7c3bc;background:#fff;padding:5px 10px;min-height:30px}.qc-status-bar label{display:inline-flex;align-items:center;gap:4px;cursor:pointer}.qc-status-bar input{accent-color:#ff9f24}.qc-current-item{margin-left:auto;color:#6b675f}.qc-current-item b{color:#242424}
.qc-workspace{display:grid;grid-template-columns:minmax(0,1fr) 114px;gap:8px;min-height:0;flex:1}.qc-left{display:flex;flex-direction:column;gap:7px;min-width:0}.qc-basis-panel{min-height:168px;flex:1}.qc-basis-scroll{max-height:calc(100% - 25px)}.qc-basis-table th,.qc-basis-table td{min-width:90px}.qc-basis-table th:first-child,.qc-basis-table td:first-child{min-width:130px}.qc-detail-panel{height:204px;display:flex;flex-direction:column}.qc-detail-scroll{flex:1;min-height:0}.qc-actions{display:flex;flex-direction:column;gap:6px}.qc-action-spacer{flex:1}
.qc-button{min-height:31px;padding:5px 9px;border:1px solid #c7c4be;border-radius:3px;background:linear-gradient(#fff,#f5f4f1);color:#302f2c;cursor:pointer;font-size:12px;white-space:nowrap}.qc-button:hover:not(:disabled){border-color:#ff9f24;background:#fff7e7}.qc-button.primary{background:#ff9f24;border-color:#ed941d;color:#fff;font-weight:600}.qc-button:disabled{opacity:.48;cursor:default}.qc-button.compact{min-height:25px;padding:2px 7px}.required-field{background:#fff0aa!important}.qc-audit{display:flex;gap:12px;flex-wrap:wrap;padding:5px 8px;border:1px solid #dedbd4;background:#fff;color:#777;font-size:10px}
.qc-modal-backdrop{position:fixed;inset:0;z-index:1500;background:rgba(20,20,20,.38);display:flex;align-items:center;justify-content:center;padding:18px}.qc-modal{display:flex;flex-direction:column;background:#f8f7f3;border:1px solid #a9a69e;box-shadow:0 8px 30px #0004;max-height:calc(100vh - 36px);min-width:min(760px,95vw)}.qc-editor{width:min(900px,96vw)}.qc-modal-heading{display:flex;justify-content:space-between;align-items:center;padding:11px 15px;background:#f1efe9;border-bottom:1px solid #c8c5bf}.qc-modal-heading h2{margin:0;font-size:18px;font-weight:600}.qc-modal-heading small{display:block;color:#777;margin-top:3px}.qc-icon-close{border:0;background:transparent;font-size:24px;cursor:pointer;color:#555}
.qc-modal-scroll{overflow:auto;padding:9px 11px;display:flex;flex-direction:column;gap:9px}.qc-fieldset{border:1px solid #aaa79f;padding:9px 11px 11px;margin:0;min-width:0}.qc-fieldset legend{padding:0 6px;color:#4b4944;font-weight:600}.qc-form-grid{display:grid;grid-template-columns:1fr 1fr;gap:8px 14px}.qc-form-grid label,.qc-copy-content label,.qc-memo{display:flex;flex-direction:column;gap:4px;font-size:12px}.qc-form-grid input,.qc-form-grid textarea,.qc-factor-table input,.qc-limit-table input,.qc-memo input,.qc-copy-content input,.qc-copy-content select,.qc-audit-fields input{height:28px;border:1px solid #999;background:#fff;padding:4px 7px;color:#242424}.qc-form-grid input[readonly],.qc-audit-fields input{background:#ededeb;color:#555}.qc-description{grid-column:1/-1}.qc-description-grid{margin-top:8px}.qc-description-grid label{display:flex;flex-direction:column;gap:4px;font-size:12px}.qc-description-grid textarea{border:1px solid #999;padding:5px;resize:vertical}.qc-subform-heading{display:flex;justify-content:space-between;align-items:center;margin:10px 0 5px}.qc-subform-heading span,.qc-note{font-size:11px;color:#777}.qc-factor-scroll{max-height:145px;border:1px solid #c9c6bf}.qc-factor-table input{min-width:110px}.qc-factor-table td:last-child{display:flex;gap:4px}.qc-factor-options{display:flex;flex-wrap:wrap;align-items:center;gap:5px;margin:7px 0;padding:7px;border:1px solid #d5d1c9;background:#fff}.qc-inline-picker-close{margin-top:6px;border:0;background:none;text-decoration:underline;cursor:pointer;color:#555}.qc-memo{margin-top:8px}.qc-note{margin:0 0 7px}.qc-limit-scroll{max-height:220px;border:1px solid #c9c6bf}.qc-limit-table input{min-width:76px;width:100%}.qc-limit-table th:first-child{min-width:100px}.qc-audit-fields{display:grid;grid-template-columns:1fr 1fr;gap:7px 12px}.qc-audit-fields label{display:flex;flex-direction:column;gap:4px;font-size:11px}.qc-modal-footer{display:flex;justify-content:space-between;align-items:center;gap:10px;padding:8px 11px;background:#f1efe9;border-top:1px solid #c8c5bf}.qc-footer-buttons{display:flex;gap:7px}.qc-modal-error{color:#9b332a;font-size:12px}.qc-copy-modal{width:min(600px,95vw)}.qc-copy-content{padding:13px;display:flex;flex-direction:column;gap:10px}.qc-copy-content fieldset{display:flex;flex-direction:column;gap:9px}.qc-copy-content p{margin:0;color:#777;font-size:11px}
@media(max-width:900px){.qc-spec-page{padding:8px}.qc-workspace{grid-template-columns:minmax(0,1fr)}.qc-actions{display:grid;grid-template-columns:repeat(4,minmax(72px,1fr))}.qc-action-spacer{display:none}.qc-detail-panel{height:180px}.qc-current-item{margin-left:0;width:100%}.qc-status-bar{gap:8px;flex-wrap:wrap}.qc-form-grid,.qc-audit-fields{grid-template-columns:1fr}.qc-description{grid-column:auto}}
</style>
