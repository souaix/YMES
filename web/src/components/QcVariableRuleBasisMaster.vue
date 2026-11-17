<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type IssueState = -1 | 0 | 1 | 2
type RuleType = 0 | 1 | 2
type EditorMode = 'add' | 'edit' | 'copy' | null
interface RuleBasis {
  ruleSerial: string
  ruleNo: string
  ruleName: string
  functionName: string | null
  ruleType: RuleType | null
  issueState: IssueState
  creator: string | null
  createDate: string | null
  description: string | null
  ruleOption: number | null
  testOutof: number | null
  testCount: number | null
  compareValue1: string | null
  compareValue2: string | null
  relation: string | null
  weType: number | null
  dataStamp: number | null
  reviser: string | null
  reviseDate: string | null
  editableFields?: string[]
}
interface RuleOption { uiValue: number; storedValue: number; label: string }
interface ApiOption { value: number; label: string }
interface RuleDraft {
  ruleNo: string; ruleName: string; ruleType: RuleType; functionName: string; description: string
  uiRuleOption: number | null; testOutof: string; testCount: string; compareValue1: string; compareValue2: string; relation: string
}
interface MockStore { items: RuleBasis[] }
type ListPayload<T> = T[] | { items?: T[] }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes-dev-mock:qc-variable-rule-basis:v1'
const states: { value: IssueState; label: string }[] = [
  { value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' },
]
const storedToUi: Record<number, number> = { 0: 0, 1: 2, 2: 3, 3: 1, 4: 4, 5: 5, 6: 6, 9: 5 }
const demoRuleOptions: RuleOption[] = [
  { uiValue: 0, storedValue: 0, label: 'Value（DEMO）' }, { uiValue: 1, storedValue: 3, label: 'Value Range（DEMO）' },
  { uiValue: 2, storedValue: 1, label: 'Sigma（DEMO）' }, { uiValue: 3, storedValue: 2, label: 'Line（DEMO）' },
  { uiValue: 4, storedValue: 4, label: 'Sigma Range（DEMO）' }, { uiValue: 5, storedValue: 5, label: 'Other（DEMO）' },
  { uiValue: 6, storedValue: 6, label: 'Special Value（DEMO）' },
]
const demoFunctions = ['FUNQRCheckTrend（DEMO）', 'FUNQRCheckValue（DEMO）', 'FUNQRCheckSigma（DEMO）']
const compareRelations = ['>', '>=', '<', '<=', '=', '<>']
const specialRelations = ['>', '>=', '<', '<=', '<>']
const lineValues = ['US', 'UCL', 'CL', 'LCL', 'LS']
const specialValues = ['CpkGoal', 'US', 'UCL', 'CL', 'LCL', 'LS', 'SpecLimit']
const seedRows: RuleBasis[] = [
  { ruleSerial: 'DEV-VAR-001', ruleNo: 'DEV-VAR-001', ruleName: 'Value 趨勢檢查', functionName: null, ruleType: 0, issueState: 0, creator: 'DEV001', createDate: '2026-11-10T08:00:00+08:00', description: '連續樣本依設定值判定', ruleOption: 0, testOutof: 5, testCount: 3, compareValue1: '10', compareValue2: null, relation: '>', weType: 0, dataStamp: 0, reviser: null, reviseDate: null },
  { ruleSerial: 'DEV-VAR-002', ruleNo: 'DEV-VAR-002', ruleName: 'Sigma 規則', functionName: null, ruleType: 0, issueState: 1, creator: 'DEV001', createDate: '2026-11-10T09:00:00+08:00', description: null, ruleOption: 1, testOutof: 8, testCount: 5, compareValue1: '2', compareValue2: null, relation: '>', weType: 0, dataStamp: 1, reviser: 'DEV002', reviseDate: '2026-11-11T08:00:00+08:00' },
  { ruleSerial: 'DEV-VAR-003', ruleNo: 'DEV-VAR-003', ruleName: '自訂趨勢函數', functionName: 'FUNQRCheckTrend（DEMO）', ruleType: 1, issueState: 2, creator: 'DEV002', createDate: '2026-11-11T08:00:00+08:00', description: '使用自訂函數', ruleOption: null, testOutof: null, testCount: null, compareValue1: null, compareValue2: null, relation: null, weType: 0, dataStamp: 2, reviser: 'DEV002', reviseDate: '2026-11-12T08:00:00+08:00' },
  { ruleSerial: 'DEV-VAR-004', ruleNo: 'DEV-VAR-004', ruleName: '區間法則', functionName: null, ruleType: 0, issueState: -1, creator: 'DEV001', createDate: '2026-11-12T08:00:00+08:00', description: '上下界範例', ruleOption: 3, testOutof: 6, testCount: 2, compareValue1: '1.5', compareValue2: '4.5', relation: 'Between', weType: 0, dataStamp: 3, reviser: 'DEV001', reviseDate: '2026-11-13T08:00:00+08:00' },
  { ruleSerial: 'DEV-VAR-005', ruleNo: 'DEV-VAR-005', ruleName: '自訂 Other', functionName: null, ruleType: 0, issueState: 0, creator: 'DEV001', createDate: '2026-11-14T08:00:00+08:00', description: null, ruleOption: 9, testOutof: 7, testCount: 4, compareValue1: null, compareValue2: null, relation: 'Increase', weType: 0, dataStamp: 0, reviser: null, reviseDate: null },
]
const items = ref<RuleBasis[]>([])
const ruleOptions = ref<RuleOption[]>([])
const functionOptions = ref<string[]>([])
const issueState = ref<IssueState>(0)
const selectedRuleNos = ref<string[]>([])
const activeRuleNo = ref('')
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const notice = ref('')
const editorMode = ref<EditorMode>(null)
const sourceRow = ref<RuleBasis | null>(null)
const editor = ref<RuleDraft>(emptyDraft())
const editorDirty = ref(false)
const editorError = ref('')
const editorNotice = ref('')
const saving = ref(false)
const functionsError = ref('')

function emptyDraft(): RuleDraft { return { ruleNo: '', ruleName: '', ruleType: 0, functionName: '', description: '', uiRuleOption: 0, testOutof: '', testCount: '', compareValue1: '', compareValue2: '', relation: '' } }
const visibleRows = computed(() => items.value.filter(row => row.issueState === issueState.value && row.ruleType !== 2 && row.weType === 0).sort((a, b) => a.ruleSerial.localeCompare(b.ruleSerial) || a.ruleNo.localeCompare(b.ruleNo)))
const activeRow = computed(() => visibleRows.value.find(row => row.ruleNo === activeRuleNo.value) ?? null)
const selectedRows = computed(() => visibleRows.value.filter(row => selectedRuleNos.value.includes(row.ruleNo)))
const currentUser = computed(() => props.currentUserNo?.trim() || (mock.value ? 'DEV001' : ''))
const currentOption = computed(() => ruleOptions.value.find(option => option.uiValue === editor.value.uiRuleOption) ?? null)
const isSpecial = computed(() => editor.value.uiRuleOption === 6)
const isCpkGoal = computed(() => isSpecial.value && editor.value.compareValue1.toUpperCase() === 'CPKGOAL')
const isRange = computed(() => editor.value.uiRuleOption === 1 || editor.value.uiRuleOption === 4)
const relationOptions = computed(() => {
  if (editor.value.uiRuleOption === 1 || editor.value.uiRuleOption === 4 || (isSpecial.value && editor.value.compareValue1.toUpperCase() === 'SPECLIMIT')) return ['Between', 'Beyond']
  if (editor.value.uiRuleOption === 3) return compareRelations
  if (isSpecial.value) return specialRelations
  if (editor.value.uiRuleOption === 5) return ['Increase', 'Decrease', 'Alternate']
  return compareRelations
})
const permittedFields = computed(() => {
  if (editorMode.value !== 'edit' || !sourceRow.value) return ['ruleName', 'ruleType', 'functionName', 'description', 'ruleOption', 'testOutof', 'testCount', 'compareValue1', 'compareValue2', 'relation']
  if (sourceRow.value.issueState === 0) return ['ruleName', 'ruleType', 'functionName', 'description', 'ruleOption', 'testOutof', 'testCount', 'compareValue1', 'compareValue2', 'relation']
  if (sourceRow.value.issueState === 1) return []
  return sourceRow.value.editableFields ?? []
})
const canEditRuleDefinition = computed(() => editorMode.value !== 'edit' || ['ruleOption', 'testOutof', 'testCount', 'compareValue1', 'compareValue2', 'relation'].some(field => permittedFields.value.includes(field)))
const canEdit = computed(() => editorMode.value !== 'edit' || permittedFields.value.length > 0)
const canDelete = computed(() => selectedRows.value.length > 0 && issueState.value !== 1)
const canApprove = computed(() => selectedRows.value.length > 0 && issueState.value === 0 && selectedRows.value.every(row => row.issueState === 0))
const editorTitle = computed(() => '計量品管法則基本資料（' + (editorMode.value === 'add' ? '新增' : editorMode.value === 'copy' ? '複製' : '修改') + '）')

function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function listOf<T>(payload: ListPayload<T>): T[] { return Array.isArray(payload) ? payload : payload.items ?? [] }
function toUiOption(value: number | null): number | null { return value === null ? null : Object.prototype.hasOwnProperty.call(storedToUi, value) ? storedToUi[value] : null }
function toStoredOption(uiValue: number, originalValue: number | null): number { return uiValue === 5 && originalValue === 9 ? 9 : ruleOptions.value.find(item => item.uiValue === uiValue)?.storedValue ?? uiValue }
function formatDate(value: string | null | undefined): string { if (!value) return ''; const date = new Date(value); return Number.isNaN(date.getTime()) ? value : new Intl.DateTimeFormat('zh-TW', { dateStyle: 'short', timeStyle: 'medium' }).format(date) }
function optionLabel(value: number | null): string { if (value === null) return ''; if (value === 9) return 'Other'; const option = ruleOptions.value.find(item => item.storedValue === value); return option?.label.replace('（DEMO）', '') ?? String(value) }
function ruleTypeLabel(value: number | null): string { return value === 0 ? 'System' : value === 1 ? 'User Define' : value === null ? '' : String(value) }
function issueStateLabel(value: number): string { return states.find(state => state.value === value)?.label ?? String(value) }
function hasField(name: string): boolean { return editorMode.value !== 'edit' || permittedFields.value.includes(name) }
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
  if (!response.ok) { const problem = await response.json().catch(() => null) as { detail?: string; title?: string; message?: string } | null; throw new Error(problem?.detail || problem?.message || problem?.title || 'API 呼叫失敗（' + response.status + '）。') }
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}
function readStore(): MockStore {
  try { const stored = window.localStorage.getItem(storageKey); if (stored) { const parsed = JSON.parse(stored) as MockStore; if (Array.isArray(parsed.items)) return parsed } } catch { /* reset invalid preview data */ }
  const initial = { items: clone(seedRows) }; writeStore(initial); return initial
}
function writeStore(store: MockStore): void { window.localStorage.setItem(storageKey, JSON.stringify(store)) }
function focusRow(ruleNo = ''): void { const row = visibleRows.value.find(item => item.ruleNo === ruleNo) ?? visibleRows.value[0] ?? null; activeRuleNo.value = row?.ruleNo ?? ''; selectedRuleNos.value = row ? [row.ruleNo] : [] }
async function loadOptions(): Promise<void> {
  if (mock.value) { ruleOptions.value = clone(demoRuleOptions); functionOptions.value = [...demoFunctions]; return }
  const results = await Promise.allSettled([request<ListPayload<ApiOption>>('api/qc/rule-bases/options/rule-options'), request<ListPayload<string>>('api/qc/rule-bases/options/functions')])
  if (results[0].status === 'fulfilled') ruleOptions.value = listOf(results[0].value).filter(item => Object.prototype.hasOwnProperty.call(storedToUi, item.value)).map(item => ({ uiValue: storedToUi[item.value], storedValue: item.value, label: item.label })).filter((item, index, all) => all.findIndex(candidate => candidate.uiValue === item.uiValue) === index).sort((a, b) => a.uiValue - b.uiValue)
  else errorMessage.value = results[0].reason instanceof Error ? results[0].reason.message : '載入 RuleOption 選項失敗。'
  if (results[1].status === 'fulfilled') functionOptions.value = listOf(results[1].value)
  else { functionsError.value = results[1].reason instanceof Error ? results[1].reason.message : '載入 FunctionName 選項失敗。'; if (!errorMessage.value) errorMessage.value = functionsError.value }
}
async function refresh(focusKey = ''): Promise<void> {
  loading.value = true; errorMessage.value = ''
  try { items.value = mock.value ? clone(readStore().items) : listOf(await request<ListPayload<RuleBasis>>('api/qc/rule-bases')); focusRow(focusKey || activeRuleNo.value) }
  catch (error) { errorMessage.value = error instanceof Error ? error.message : '載入計量品管法則失敗。' }
  finally { loading.value = false }
}
function selectRow(row: RuleBasis): void { activeRuleNo.value = row.ruleNo; selectedRuleNos.value = [row.ruleNo]; errorMessage.value = '' }
function toggleRow(row: RuleBasis): void { selectedRuleNos.value = selectedRuleNos.value.includes(row.ruleNo) ? selectedRuleNos.value.filter(value => value !== row.ruleNo) : [...selectedRuleNos.value, row.ruleNo]; activeRuleNo.value = row.ruleNo }
function setState(value: IssueState): void { issueState.value = value; focusRow() }
function setRuleOption(value: number | null): void { editor.value.uiRuleOption = value; editorDirty.value = true; if (value !== null && relationOptions.value.length && !relationOptions.value.includes(editor.value.relation)) editor.value.relation = relationOptions.value[0] }
function setRuleType(value: RuleType): void { editor.value.ruleType = value; editorDirty.value = true; if (value === 1) editor.value.uiRuleOption = null }
function openAdd(): void { editorMode.value = 'add'; sourceRow.value = null; editor.value = emptyDraft(); editorError.value = ''; editorNotice.value = ''; editorDirty.value = false }
function draftFrom(row: RuleBasis): RuleDraft { return { ruleNo: row.ruleNo, ruleName: row.ruleName, ruleType: row.ruleType === 1 ? 1 : 0, functionName: row.functionName ?? '', description: row.description ?? '', uiRuleOption: toUiOption(row.ruleOption), testOutof: row.testOutof?.toString() ?? '', testCount: row.testCount?.toString() ?? '', compareValue1: row.compareValue1 ?? '', compareValue2: row.compareValue2 ?? '', relation: row.relation ?? '' } }
function openEdit(): void { if (!activeRow.value) return; sourceRow.value = clone(activeRow.value); editorMode.value = 'edit'; editor.value = draftFrom(activeRow.value); editorError.value = ''; editorNotice.value = ''; editorDirty.value = false }
function openCopy(): void { if (!activeRow.value) return; sourceRow.value = clone(activeRow.value); editorMode.value = 'copy'; editor.value = draftFrom(activeRow.value); editorError.value = ''; editorNotice.value = '已複製規則內容，請輸入新的 RuleNo。'; editorDirty.value = false }
function closeEditor(): void { if (saving.value) return; if (editorDirty.value && !window.confirm('放棄尚未保存的變更？')) return; editorMode.value = null; sourceRow.value = null; editorError.value = ''; editorNotice.value = ''; editorDirty.value = false }
function inputChanged(): void { editorDirty.value = true; editorError.value = '' }
function validate(): string {
  const value = editor.value
  const ruleNo = value.ruleNo.trim().toUpperCase()
  if (!ruleNo) return 'RuleNo 為必填。'
  if (ruleNo.length > 30) return 'RuleNo 不可超過 30 個字元。'
  if (!value.ruleName.trim()) return 'RuleName 為必填。'
  if (value.ruleName.length > 50) return 'RuleName 不可超過 50 個字元。'
  if (value.description.length > 255) return 'Description 不可超過 255 個字元。'
  if (editorMode.value !== 'edit' && items.value.some(row => row.ruleNo.toUpperCase() === ruleNo || row.ruleSerial.toUpperCase() === ruleNo)) return 'RuleNo／RuleSerial 已存在。'
  if (value.ruleType !== 0 && value.ruleType !== 1) return 'RuleType 不正確。'
  if (value.ruleType === 1 && !value.functionName.trim()) return 'User Define 必須選擇 FunctionName。'
  if (value.ruleType === 1 && value.functionName.length > 50) return 'FunctionName 不可超過 50 個字元。'
  if (value.ruleType === 0) {
    if (value.uiRuleOption === null || !ruleOptions.value.some(item => item.uiValue === value.uiRuleOption)) return 'System Rule 必須選擇 Rule Option。'
    const specialGoal = isSpecial.value && value.compareValue1.toUpperCase() === 'CPKGOAL'
    if (!specialGoal) {
      const outOf = Number(value.testOutof); const count = Number(value.testCount)
      if (!value.testOutof.trim() || !Number.isFinite(outOf)) return 'TestOutof 必須是數字。'
      if (!value.testCount.trim() || !Number.isFinite(count)) return 'TestCount 必須是數字。'
      if (outOf < 1 || outOf > 99 || !Number.isInteger(outOf)) return 'TestOutof 必須是 1 到 99 的整數。'
      if (count < 1 || count > 99 || !Number.isInteger(count)) return 'TestCount 必須是 1 到 99 的整數。'
      if (outOf < count) return 'TestOutof 必須大於或等於 TestCount。'
      if (editor.value.uiRuleOption === 1 && (!Number.isInteger(outOf) || !Number.isInteger(count))) return 'Value Range 的 TestOutof 與 TestCount 必須是整數。'
    }
    if ([0, 2, 4].includes(value.uiRuleOption) && (!value.compareValue1.trim() || !Number.isFinite(Number(value.compareValue1)))) return 'CompareValue1 必須是數字。'
    if (value.uiRuleOption === 1 && (!value.compareValue1.trim() || !value.compareValue2.trim() || !Number.isFinite(Number(value.compareValue1)) || !Number.isFinite(Number(value.compareValue2)))) return 'Value Range 的上下限必須是數字。'
    if (value.uiRuleOption === 1 && Number(value.compareValue2) < Number(value.compareValue1)) return '上限不可小於下限。'
    if (value.uiRuleOption === 3 && !lineValues.includes(value.compareValue1)) return 'Line 必須選擇有效的管制界線。'
    if (isSpecial.value && !specialValues.includes(value.compareValue1)) return 'Special Value 必須選擇有效的項目。'
    if (!value.relation || !relationOptions.value.includes(value.relation)) return 'Relation 為必填。'
    if (value.compareValue1.length > 10 || value.compareValue2.length > 10 || value.relation.length > 10) return 'CompareValue 或 Relation 超過資料庫欄位長度。'
  }
  return ''
}
function buildPayload() {
  const value = editor.value
  const payload: Record<string, unknown> = { ruleNo: value.ruleNo.trim().toUpperCase(), ruleName: value.ruleName.trim(), ruleType: value.ruleType, functionName: value.ruleType === 1 ? value.functionName : '', description: value.description, issueState: editorMode.value === 'edit' ? sourceRow.value?.issueState : 0, dataStamp: sourceRow.value?.dataStamp }
  if (value.ruleType === 0 && value.uiRuleOption !== null) {
    const specialGoal = value.uiRuleOption === 6 && value.compareValue1.toUpperCase() === 'CPKGOAL'
    payload.ruleOption = toStoredOption(value.uiRuleOption, sourceRow.value?.ruleOption ?? null)
    if (specialGoal) { payload.testOutof = 1; payload.testCount = 1 }
    else if (value.uiRuleOption === 6) { payload.testOutof = Number(value.testCount); payload.testCount = Number(value.testCount) }
    else { payload.testOutof = Number(value.testOutof); payload.testCount = Number(value.testCount) }
    if ([0, 1, 2, 3, 4, 6].includes(value.uiRuleOption)) payload.compareValue1 = value.compareValue1
    if (value.uiRuleOption === 1) payload.compareValue2 = value.compareValue2
    if (value.uiRuleOption !== 5) payload.relation = value.relation
    else payload.relation = value.relation
  }
  return payload
}
async function save(keepOpen: boolean): Promise<void> {
  editor.value.ruleNo = editor.value.ruleNo.trim().toUpperCase()
  editorError.value = validate()
  if (editorError.value || !editorMode.value || !canEdit.value) return
  const mode = editorMode.value; const original = sourceRow.value; const payload = buildPayload(); saving.value = true
  try {
    if (mock.value) {
      const store = readStore()
      if (mode === 'edit') {
        const row = store.items.find(item => item.ruleNo === original?.ruleNo)
        if (!row) throw new Error('資料已不存在，請重新查詢。')
        if (permittedFields.value.includes('ruleName')) row.ruleName = editor.value.ruleName.trim()
        if (permittedFields.value.includes('ruleType')) row.ruleType = editor.value.ruleType
        if (permittedFields.value.includes('functionName')) row.functionName = editor.value.ruleType === 1 ? editor.value.functionName : null
        if (permittedFields.value.includes('description')) row.description = editor.value.description || null
        applyRuleValues(row, payload)
        row.dataStamp = (row.dataStamp ?? 0) + 1; row.reviser = currentUser.value; row.reviseDate = new Date().toISOString()
      } else {
        const next: RuleBasis = { ruleSerial: String(payload.ruleNo), ruleNo: String(payload.ruleNo), ruleName: String(payload.ruleName), functionName: editor.value.ruleType === 1 ? editor.value.functionName : null, ruleType: editor.value.ruleType, issueState: 0, creator: currentUser.value, createDate: new Date().toISOString(), description: editor.value.description || null, ruleOption: null, testOutof: null, testCount: null, compareValue1: null, compareValue2: null, relation: null, weType: 0, dataStamp: 0, reviser: null, reviseDate: null }
        applyRuleValues(next, payload); store.items.push(next)
      }
      writeStore(store)
    } else if (mode === 'edit') {
      const query = new URLSearchParams({ issueState: String(original!.issueState), dataStamp: original!.dataStamp === null ? '' : String(original!.dataStamp) })
      await request('api/qc/rule-bases/' + encodeURIComponent(original!.ruleNo) + '?' + query.toString(), { method: 'PUT', body: JSON.stringify(payload) })
    } else await request('api/qc/rule-bases', { method: 'POST', body: JSON.stringify(payload) })
    await refresh(String(payload.ruleNo))
    editorDirty.value = false
    if (keepOpen && mode !== 'edit') editorNotice.value = '資料已套用；請輸入新的 RuleNo 再新增下一筆。'
    else if (keepOpen) { sourceRow.value = activeRow.value ? clone(activeRow.value) : null; editor.value = activeRow.value ? draftFrom(activeRow.value) : editor.value; editorNotice.value = '資料已套用。' }
    else closeEditor()
  } catch (error) { editorError.value = error instanceof Error ? error.message : '儲存計量品管法則失敗。' }
  finally { saving.value = false }
}
function applyRuleValues(row: RuleBasis, payload: Record<string, unknown>): void {
  if (typeof payload.ruleOption === 'number') row.ruleOption = payload.ruleOption
  if (typeof payload.testOutof === 'number') row.testOutof = payload.testOutof
  if (typeof payload.testCount === 'number') row.testCount = payload.testCount
  if (typeof payload.compareValue1 === 'string') row.compareValue1 = payload.compareValue1
  if (typeof payload.compareValue2 === 'string') row.compareValue2 = payload.compareValue2
  if (typeof payload.relation === 'string') row.relation = payload.relation
}
async function removeSelected(): Promise<void> {
  const targets = [...selectedRows.value]
  if (!targets.length || !canDelete.value) { errorMessage.value = '請先選取可刪除的資料。'; return }
  if (!window.confirm('確定刪除／停用選取的 ' + targets.length + ' 筆資料？')) return
  busy.value = true; errorMessage.value = ''; notice.value = ''; let completed = 0; let failure = ''
  try {
    for (const row of targets) {
      if (row.issueState !== 0 && row.issueState !== -1 && row.issueState !== 2) throw new Error('簽核中的資料不可刪除。')
      if (mock.value) { const store = readStore(); const current = store.items.find(item => item.ruleNo === row.ruleNo); if (!current) throw new Error('資料已不存在。'); if (current.issueState === 2) { current.issueState = -1; current.dataStamp = (current.dataStamp ?? 0) + 1; current.reviser = currentUser.value; current.reviseDate = new Date().toISOString() } else store.items = store.items.filter(item => item.ruleNo !== row.ruleNo); writeStore(store) }
      else await request('api/qc/rule-bases/' + encodeURIComponent(row.ruleNo) + '?issueState=' + row.issueState + '&dataStamp=' + (row.dataStamp ?? ''), { method: 'DELETE' })
      completed++
    }
    notice.value = '已處理 ' + completed + ' 筆資料。'
  } catch (error) { failure = (error instanceof Error ? error.message : '刪除失敗。') + (completed ? '（前 ' + completed + ' 筆已完成）' : '') }
  finally { if (completed) await refresh(); if (failure) errorMessage.value = failure; busy.value = false }
}
async function approveSelected(): Promise<void> {
  const targets = [...selectedRows.value]
  if (!targets.length || !canApprove.value) { errorMessage.value = '只有未簽核資料可送出核准。'; return }
  if (!window.confirm('確定送出核准選取的 ' + targets.length + ' 筆資料？')) return
  busy.value = true; errorMessage.value = ''; notice.value = ''; let completed = 0; let failure = ''
  try {
    for (const row of targets) {
      if (mock.value) { const store = readStore(); const current = store.items.find(item => item.ruleNo === row.ruleNo); if (!current) throw new Error('資料已不存在。'); current.issueState = 2; current.dataStamp = (current.dataStamp ?? 0) + 1; current.reviser = currentUser.value; current.reviseDate = new Date().toISOString(); writeStore(store) }
      else await request('api/qc/rule-bases/' + encodeURIComponent(row.ruleNo) + '/approve?issueState=0&dataStamp=' + (row.dataStamp ?? ''), { method: 'POST' })
      completed++
    }
    notice.value = mock.value ? '已核准 ' + completed + ' 筆 DEV 預覽資料。' : '已送出 ' + completed + ' 筆；實際狀態依 QC RULE BASIS 簽核流程更新。'
  } catch (error) { failure = (error instanceof Error ? error.message : '核准失敗。') + (completed ? '（前 ' + completed + ' 筆已完成）' : '') }
  finally { if (completed) await refresh(); if (failure) errorMessage.value = failure; busy.value = false }
}
function selectSpecial(value: string): void { editor.value.compareValue1 = value; if (value.toUpperCase() === 'CPKGOAL') { editor.value.testCount = '1'; editor.value.testOutof = '1' } if (value.toUpperCase() === 'SPECLIMIT' && !['Between', 'Beyond'].includes(editor.value.relation)) editor.value.relation = 'Between'; if (value.toUpperCase() !== 'SPECLIMIT' && ['Between', 'Beyond'].includes(editor.value.relation)) editor.value.relation = '>'; inputChanged() }

onMounted(async () => { await loadOptions(); const initialError = errorMessage.value; await refresh(); if (initialError) errorMessage.value = initialError })
</script>

<template>
  <main class="qc-rule-page">
    <header class="page-heading"><h1>計量品管法則基本資料</h1><span v-if="mock" class="dev-badge">DEV 假資料</span></header>
    <section class="query-panel" aria-label="簽核狀態"><div class="states" role="radiogroup" aria-label="簽核狀態"><label v-for="state in states" :key="state.value" class="state-option"><input type="radio" name="qc-rule-state" :value="state.value" :checked="issueState === state.value" @change="setState(state.value)"><span>{{ state.label }}</span></label></div></section>
    <section class="list-panel" aria-label="計量品管法則清單">
      <div class="grid-wrap"><table class="data-grid"><thead><tr><th class="select-col"></th><th>RuleNo</th><th>RuleName</th><th>Description</th><th>RuleType</th><th>FunctionName</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead><tbody>
        <tr v-for="row in visibleRows" :key="row.ruleSerial" :class="{ active: activeRuleNo === row.ruleNo }" @click="selectRow(row)"><td class="select-col"><input type="checkbox" :checked="selectedRuleNos.includes(row.ruleNo)" :aria-label="'選取 ' + row.ruleNo" @click.stop @change="toggleRow(row)"></td><td>{{ row.ruleNo }}</td><td>{{ row.ruleName }}</td><td class="description-cell">{{ row.description }}</td><td>{{ ruleTypeLabel(row.ruleType) }}</td><td>{{ row.functionName }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td></tr>
        <tr v-if="!visibleRows.length"><td colspan="10" class="empty-cell">{{ loading ? '載入中…' : '查無資料' }}</td></tr>
      </tbody></table></div>
      <div class="action-column"><button type="button" class="button primary" :disabled="busy" @click="openAdd">新增(A)</button><button type="button" class="button" :disabled="busy || !activeRow" @click="openEdit">修改(E)</button><button type="button" class="button" :disabled="busy || !canDelete" @click="removeSelected">刪除(D)</button><button type="button" class="button" :disabled="busy || !activeRow" @click="openCopy">複製(C)</button><button type="button" class="button" :disabled="busy || !canApprove" @click="approveSelected">核准</button><button type="button" class="button close-button" @click="emit('close')">關閉</button></div>
    </section>
    <footer class="list-footer">資料總筆數 {{ visibleRows.length }}<span v-if="activeRow">　RuleOption：{{ optionLabel(activeRow.ruleOption) }}　狀態：{{ issueStateLabel(activeRow.issueState) }}</span></footer>
    <p v-if="errorMessage" class="message error" role="alert">{{ errorMessage }}</p><p v-if="notice" class="message notice" role="status">{{ notice }}</p>

    <div v-if="editorMode" class="backdrop" @click.self="closeEditor"><section class="dialog" role="dialog" aria-modal="true" :aria-label="editorTitle">
      <header class="titlebar"><h2>{{ editorTitle }}</h2><button class="x" type="button" aria-label="關閉" :disabled="saving" @click="closeEditor">×</button></header>
      <form class="editor-form" @submit.prevent="save(false)">
        <fieldset class="basis-fields" :disabled="saving"><legend>品管法則基本資料</legend>
          <label class="field"><span>RuleNo</span><input v-model="editor.ruleNo" maxlength="30" :readonly="editorMode === 'edit'" required @input="inputChanged" @blur="editor.ruleNo = editor.ruleNo.toUpperCase()"></label>
          <label class="field"><span>RuleName</span><input v-model="editor.ruleName" maxlength="50" :disabled="!hasField('ruleName')" required @input="inputChanged"></label>
          <div class="field rule-type"><span>RuleType</span><label><input type="radio" :checked="editor.ruleType === 0" :disabled="!hasField('ruleType')" @change="setRuleType(0)"> System</label><label><input type="radio" :checked="editor.ruleType === 1" :disabled="!hasField('ruleType')" @change="setRuleType(1)"> User Define</label></div>
          <label class="field"><span>FunctionName</span><select v-model="editor.functionName" :disabled="editor.ruleType !== 1 || !hasField('functionName')" @change="inputChanged"><option value=""></option><option v-for="option in functionOptions" :key="option" :value="option">{{ mock ? option : option }}</option></select></label>
          <label class="field description-field"><span>Description</span><textarea v-model="editor.description" rows="2" maxlength="255" :disabled="!hasField('description')" @input="inputChanged"></textarea></label>
        </fieldset>
        <fieldset class="rule-definition" :disabled="saving || editor.ruleType !== 0 || !canEditRuleDefinition"><legend>Rule Option</legend>
          <label class="field option-select"><span>Rule Option</span><select :value="editor.uiRuleOption ?? ''" :disabled="!hasField('ruleOption')" @change="setRuleOption($event.target instanceof HTMLSelectElement && $event.target.value !== '' ? Number($event.target.value) : null)"><option value=""></option><option v-for="option in ruleOptions" :key="option.uiValue" :value="option.uiValue">{{ option.label }}</option></select></label>
          <div class="option-tabs" role="tablist" aria-label="Rule Option 類型"><button v-for="option in ruleOptions" :key="option.uiValue" type="button" role="tab" :aria-selected="editor.uiRuleOption === option.uiValue" :disabled="editor.ruleType !== 0 || !hasField('ruleOption')" :class="{ selected: editor.uiRuleOption === option.uiValue }" @click="setRuleOption(option.uiValue)">{{ option.label.replace('（DEMO）', '') }}</button></div>
          <section v-if="editor.uiRuleOption !== null" class="option-panel" :aria-label="currentOption?.label ?? 'Rule Option'">
            <template v-if="editor.uiRuleOption === 0 || editor.uiRuleOption === 2"><label class="field"><span>Test Out of</span><input v-model="editor.testOutof" :disabled="!hasField('testOutof')" inputmode="decimal" maxlength="2" @input="inputChanged"></label><label class="field"><span>Test Count</span><input v-model="editor.testCount" :disabled="!hasField('testCount')" inputmode="decimal" maxlength="2" @input="inputChanged"></label><label class="field"><span>Value</span><input v-model="editor.compareValue1" :disabled="!hasField('compareValue1')" inputmode="decimal" maxlength="10" @input="inputChanged"></label><label class="field"><span>Relation</span><select v-model="editor.relation" :disabled="!hasField('relation')" @change="inputChanged"><option value=""></option><option v-for="item in relationOptions" :key="item">{{ item }}</option></select></label></template>
            <template v-else-if="editor.uiRuleOption === 1"><label class="field"><span>Test Out of</span><input v-model="editor.testOutof" :disabled="!hasField('testOutof')" inputmode="numeric" maxlength="2" @input="inputChanged"></label><label class="field"><span>Test Count</span><input v-model="editor.testCount" :disabled="!hasField('testCount')" inputmode="numeric" maxlength="2" @input="inputChanged"></label><label class="field"><span>Lower Value</span><input v-model="editor.compareValue1" :disabled="!hasField('compareValue1')" inputmode="decimal" maxlength="10" @input="inputChanged"></label><label class="field"><span>Upper Value</span><input v-model="editor.compareValue2" :disabled="!hasField('compareValue2')" inputmode="decimal" maxlength="10" @input="inputChanged"></label><label class="field"><span>Relation</span><select v-model="editor.relation" :disabled="!hasField('relation')" @change="inputChanged"><option value=""></option><option v-for="item in relationOptions" :key="item">{{ item }}</option></select></label></template>
            <template v-else-if="editor.uiRuleOption === 3"><label class="field"><span>Test Out of</span><input v-model="editor.testOutof" :disabled="!hasField('testOutof')" inputmode="decimal" maxlength="2" @input="inputChanged"></label><label class="field"><span>Test Count</span><input v-model="editor.testCount" :disabled="!hasField('testCount')" inputmode="decimal" maxlength="2" @input="inputChanged"></label><label class="field"><span>Value</span><select v-model="editor.compareValue1" :disabled="!hasField('compareValue1')" @change="inputChanged"><option value=""></option><option v-for="item in lineValues" :key="item">{{ item }}</option></select></label><label class="field"><span>Relation</span><select v-model="editor.relation" :disabled="!hasField('relation')" @change="inputChanged"><option value=""></option><option v-for="item in compareRelations" :key="item">{{ item }}</option></select></label></template>
            <template v-else-if="editor.uiRuleOption === 4"><label class="field"><span>Test Out of</span><input v-model="editor.testOutof" :disabled="!hasField('testOutof')" inputmode="decimal" maxlength="2" @input="inputChanged"></label><label class="field"><span>Test Count</span><input v-model="editor.testCount" :disabled="!hasField('testCount')" inputmode="decimal" maxlength="2" @input="inputChanged"></label><label class="field"><span>Sigma</span><input v-model="editor.compareValue1" :disabled="!hasField('compareValue1')" inputmode="decimal" maxlength="10" @input="inputChanged"></label><label class="field"><span>Relation</span><select v-model="editor.relation" :disabled="!hasField('relation')" @change="inputChanged"><option value=""></option><option v-for="item in relationOptions" :key="item">{{ item }}</option></select></label></template>
            <template v-else-if="editor.uiRuleOption === 5"><label class="field"><span>Test Out of</span><input v-model="editor.testOutof" :disabled="!hasField('testOutof')" inputmode="decimal" maxlength="2" @input="inputChanged"></label><label class="field"><span>Test Count</span><input v-model="editor.testCount" :disabled="!hasField('testCount')" inputmode="decimal" maxlength="2" @input="inputChanged"></label><label class="field"><span>Relation</span><select v-model="editor.relation" :disabled="!hasField('relation')" @change="inputChanged"><option value=""></option><option v-for="item in relationOptions" :key="item">{{ item }}</option></select></label></template>
            <template v-else-if="editor.uiRuleOption === 6"><label class="field"><span>Value</span><select :value="editor.compareValue1" :disabled="!hasField('compareValue1')" @change="selectSpecial(($event.target as HTMLSelectElement).value)"><option value=""></option><option v-for="item in specialValues" :key="item">{{ item }}</option></select></label><label v-if="!isCpkGoal" class="field"><span>Test Count</span><input v-model="editor.testCount" :disabled="!hasField('testCount')" inputmode="decimal" maxlength="2" @input="inputChanged"></label><label class="field"><span>Relation</span><select v-model="editor.relation" :disabled="!hasField('relation')" @change="inputChanged"><option value=""></option><option v-for="item in relationOptions" :key="item">{{ item }}</option></select></label><p class="field-hint">CpkGoal 會將 TestCount 與 TestOutof 設為 1。</p></template>
          </section>
          <p v-else class="empty-state">選擇 Rule Option 後設定法則參數。</p>
        </fieldset>
        <fieldset class="audit-fields" disabled><legend>建立／修改資訊</legend><label><span>Creator</span><input :value="editorMode === 'edit' ? sourceRow?.creator ?? '' : currentUser" readonly></label><label><span>CreateDate</span><input :value="formatDate(editorMode === 'edit' ? sourceRow?.createDate : new Date().toISOString())" readonly></label><label><span>Reviser</span><input :value="editorMode === 'edit' ? sourceRow?.reviser ?? '' : ''" readonly></label><label><span>ReviseDate</span><input :value="editorMode === 'edit' ? formatDate(sourceRow?.reviseDate) : ''" readonly></label></fieldset>
        <p v-if="functionsError" class="message notice">{{ functionsError }}</p><p v-if="editorNotice" class="message notice" role="status">{{ editorNotice }}</p><p v-if="editorError" class="message error" role="alert">{{ editorError }}</p>
        <footer class="dialog-actions"><button type="submit" class="button primary" :disabled="saving || !canEdit">確定</button><button type="button" class="button" :disabled="saving" @click="closeEditor">關閉</button><button type="button" class="button" :disabled="saving || !canEdit" @click="save(true)">套用</button></footer>
      </form>
    </section></div>
  </main>
</template>

<style scoped>
.qc-rule-page{--line:#c8c4bb;--text:#30302e;--muted:#77746e;--field:#fff0ad;--orange:#f7a32b;display:flex;flex-direction:column;gap:7px;height:100%;min-height:0;padding:10px;color:var(--text);background:#f7f5ef;font:14px/1.35 "Segoe UI","Microsoft JhengHei",sans-serif}.page-heading{display:flex;align-items:center;justify-content:space-between;min-height:34px}.page-heading h1{margin:0;font-size:20px;font-weight:600}.dev-badge{padding:4px 9px;border:1px solid #d7a044;background:#fff1bd;color:#77551a;font-size:12px}.query-panel{display:flex;align-items:center;min-height:38px;padding:4px 9px;border:1px solid var(--line);background:#fff}.states{display:flex;flex-wrap:wrap;gap:16px}.state-option{display:inline-flex;align-items:center;gap:4px;white-space:nowrap;cursor:pointer}.state-option input,.rule-type input{accent-color:#e18b17}.list-panel{display:grid;grid-template-columns:minmax(0,1fr) 88px;gap:8px;flex:1;min-height:160px;padding:7px;border:1px solid var(--line);background:#fff}.grid-wrap{min-width:0;min-height:0;overflow:auto;border:1px solid #bcb8af}.data-grid{width:100%;border-collapse:collapse;white-space:nowrap;font-size:13px}.data-grid th{position:sticky;top:0;z-index:1;padding:5px 7px;border:1px solid #c2bfb9;background:#eeeae0;text-align:left;font-weight:600}.data-grid td{height:27px;padding:3px 7px;border:1px solid #d1cec8;background:#fff}.data-grid tbody tr{cursor:pointer}.data-grid tbody tr:hover td{background:#fff7dd}.data-grid tbody tr.active td{background:#ffedb1}.data-grid input[type=checkbox]{min-height:auto;accent-color:#dc8e1c}.select-col{width:26px;text-align:center}.description-cell{max-width:260px;overflow:hidden;text-overflow:ellipsis}.empty-cell{text-align:center;color:var(--muted)}.action-column{display:flex;flex-direction:column;gap:7px}.action-column .button{width:100%}.close-button{margin-top:auto}.button{min-height:30px;padding:4px 10px;border:1px solid #bdb9b0;border-radius:3px;background:linear-gradient(#fff,#f0efec);color:#343331;font:inherit;white-space:nowrap;cursor:pointer}.button:hover:not(:disabled){border-color:#d08a23;background:#fff6df}.button:disabled{opacity:.48;cursor:not-allowed}.button.primary{border-color:#df8612;background:var(--orange);color:#fff}.list-footer{min-height:18px;padding:0 2px;color:var(--muted);font-size:12px}.message{margin:0;padding:7px 10px;border:1px solid}.error{border-color:#d99b86;background:#fff0e9;color:#873820}.notice{border-color:#c9bc91;background:#fff8dc;color:#67551d}.backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:16px;background:rgb(25 24 22 / 28%)}.dialog{display:flex;flex-direction:column;width:min(700px,100%);max-height:calc(100vh - 32px);overflow:auto;border:1px solid #a8a49c;background:#f8f7f3;box-shadow:0 8px 28px rgb(0 0 0 / 22%)}.titlebar{display:flex;align-items:center;justify-content:space-between;padding:9px 14px;border-bottom:1px solid var(--line);background:#f0eee8}.titlebar h2{margin:0;font-size:17px;font-weight:600}.x{border:0;background:transparent;font-size:24px;line-height:1;cursor:pointer}.editor-form{display:flex;flex-direction:column;gap:9px;padding:11px}.basis-fields,.rule-definition{display:flex;flex-direction:column;gap:8px;margin:0;padding:9px;border:1px solid var(--line);background:#fff}.basis-fields legend,.rule-definition legend,.audit-fields legend{padding:0 5px;color:#57544e}.field{display:grid;grid-template-columns:125px minmax(0,1fr);align-items:center;gap:8px}.field>span{text-align:right}.field input,.field select,.field textarea{box-sizing:border-box;width:100%;min-height:29px;padding:4px 7px;border:1px solid #a9a69f;border-radius:2px;background:var(--field);color:var(--text);font:inherit}.field textarea{resize:vertical}.field input:focus,.field select:focus,.field textarea:focus{border-color:#d18a21;outline:2px solid rgb(209 138 33 / 20%)}.field input[readonly],.field input:disabled,.field select:disabled,.field textarea:disabled{background:#ecebe7;color:#777}.rule-type{display:flex;align-items:center;gap:12px}.rule-type>span{width:125px;text-align:right}.rule-type label{display:inline-flex;align-items:center;gap:4px}.rule-type input{min-height:auto}.description-field{align-items:start}.option-select{max-width:500px}.option-tabs{display:flex;flex-wrap:wrap;gap:2px;border-bottom:1px solid var(--line)}.option-tabs button{padding:5px 9px;border:1px solid var(--line);border-bottom:0;background:#f0eee8;color:#42403c;font:inherit;cursor:pointer}.option-tabs button.selected{background:#fff0ad}.option-tabs button:disabled{color:#999;cursor:not-allowed}.option-panel{display:grid;grid-template-columns:1fr 1fr;gap:8px 14px;min-height:85px;padding:7px}.option-panel .field{grid-template-columns:105px minmax(0,1fr)}.option-panel .field>span{text-align:right}.empty-state,.field-hint{margin:4px 8px;color:var(--muted);font-size:12px}.field-hint{grid-column:1/-1}.audit-fields{display:grid;grid-template-columns:1fr 1fr;gap:7px 12px;margin:0;padding:8px;border:1px solid var(--line)}.audit-fields label{display:grid;grid-template-columns:85px minmax(0,1fr);align-items:center;gap:6px}.audit-fields input{width:100%;min-height:27px;padding:4px 7px;border:1px solid #bcb8af;background:#ecebe7;color:#777;font:inherit}.dialog-actions{display:flex;justify-content:flex-end;gap:8px;padding-top:3px}.dialog-actions .primary{min-width:92px}@media(max-width:780px){.list-panel{grid-template-columns:1fr;min-height:260px}.action-column{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));grid-row:2}.close-button{margin-top:0}.option-panel{grid-template-columns:1fr}.basis-fields .field{grid-template-columns:100px minmax(0,1fr)}.rule-type{flex-wrap:wrap}.rule-type>span{width:100px}.audit-fields{grid-template-columns:1fr}}
</style>
