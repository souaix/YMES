<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

type IssueState = -1 | 0 | 1 | 2
type RecipeType = -1 | 0 | 1 | null
type BasisMode = 'new' | 'edit' | 'copy' | 'version' | null
type DetailMode = 'new' | 'edit' | null

interface RecipeBasisRow {
  recipeGroup: string
  recipeVersion: number
  description: string | null
  creator: string | null
  createDate: string | null
  issueState: IssueState
  curVersion: 0 | 1
  dataStamp: number
  reviser: string | null
  reviseDate: string | null
}
interface RecipeDetailRow {
  recipeGroup: string
  recipeVersion: number
  recipeNo: string
  recipeValue: string
  sequence: number | null
  description: string | null
  type: RecipeType
  stdValue: number | null
  maximum: number | null
  minimum: number | null
  dataStamp: number
  creator: string | null
  createDate: string | null
  reviser: string | null
  reviseDate: string | null
}
interface RecipeStore { basis: RecipeBasisRow[]; details: RecipeDetailRow[] }
interface BasisForm { recipeGroup: string; recipeVersion: string; description: string }
interface DetailForm {
  recipeNo: string; recipeValue: string; sequence: string; description: string
  type: -1 | 0 | 1; stdValue: string; maximum: string; minimum: string
}
interface ElementOption { elementNo: string; description: string; defaultValue: string; sequence: number }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const useDevMock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes-dev-mock:equipment-recipe-basis:v1'
const issueStates: { value: IssueState; label: string }[] = [
  { value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' },
]
const seedStore: RecipeStore = {
  basis: [
    { recipeGroup: 'DEV-CLEAN', recipeVersion: 1, description: 'DEV 假資料：清洗設備基本配方', creator: 'DEV001', createDate: '2026-11-01T08:00:00+08:00', issueState: 2, curVersion: 1, dataStamp: 2, reviser: 'DEV002', reviseDate: '2026-11-03T09:00:00+08:00' },
    { recipeGroup: 'DEV-ETCH', recipeVersion: 1, description: 'DEV 假資料：蝕刻舊版配方', creator: 'DEV001', createDate: '2026-11-02T08:00:00+08:00', issueState: 2, curVersion: 0, dataStamp: 1, reviser: 'DEV001', reviseDate: '2026-11-04T10:00:00+08:00' },
    { recipeGroup: 'DEV-ETCH', recipeVersion: 2, description: 'DEV 假資料：蝕刻目前版本', creator: 'DEV001', createDate: '2026-11-04T10:00:00+08:00', issueState: 2, curVersion: 1, dataStamp: 1, reviser: 'DEV002', reviseDate: '2026-11-05T10:00:00+08:00' },
    { recipeGroup: 'DEV-NEW', recipeVersion: 1, description: 'DEV 假資料：待維護配方', creator: 'DEV001', createDate: '2026-11-06T08:00:00+08:00', issueState: 0, curVersion: 0, dataStamp: 0, reviser: null, reviseDate: null },
  ],
  details: [
    { recipeGroup: 'DEV-CLEAN', recipeVersion: 1, recipeNo: 'TEMP', recipeValue: '45', sequence: 10, description: '槽體溫度', type: 0, stdValue: 45, maximum: null, minimum: null, dataStamp: 1, creator: 'DEV001', createDate: '2026-11-01T08:05:00+08:00', reviser: null, reviseDate: null },
    { recipeGroup: 'DEV-CLEAN', recipeVersion: 1, recipeNo: 'FLOW', recipeValue: '8 ~ 12', sequence: 20, description: '清洗液流量', type: 1, stdValue: null, maximum: 12, minimum: 8, dataStamp: 1, creator: 'DEV001', createDate: '2026-11-01T08:06:00+08:00', reviser: null, reviseDate: null },
    { recipeGroup: 'DEV-ETCH', recipeVersion: 1, recipeNo: 'RF_POWER', recipeValue: '800', sequence: 10, description: '射頻功率', type: 0, stdValue: 800, maximum: null, minimum: null, dataStamp: 0, creator: 'DEV001', createDate: '2026-11-02T08:05:00+08:00', reviser: null, reviseDate: null },
    { recipeGroup: 'DEV-ETCH', recipeVersion: 2, recipeNo: 'RF_POWER', recipeValue: '850', sequence: 10, description: '射頻功率', type: 0, stdValue: 850, maximum: null, minimum: null, dataStamp: 0, creator: 'DEV002', createDate: '2026-11-04T10:05:00+08:00', reviser: null, reviseDate: null },
    { recipeGroup: 'DEV-NEW', recipeVersion: 1, recipeNo: 'PRESSURE', recipeValue: '20 ~ 30', sequence: 10, description: '腔體壓力', type: 1, stdValue: null, maximum: 30, minimum: 20, dataStamp: 0, creator: 'DEV001', createDate: '2026-11-06T08:05:00+08:00', reviser: null, reviseDate: null },
  ],
}
const elementOptions: ElementOption[] = [
  { elementNo: 'FLOW', description: '清洗液流量', defaultValue: '10', sequence: 20 },
  { elementNo: 'PRESSURE', description: '腔體壓力', defaultValue: '25', sequence: 10 },
  { elementNo: 'RF_POWER', description: '射頻功率', defaultValue: '800', sequence: 10 },
  { elementNo: 'TEMP', description: '槽體溫度', defaultValue: '45', sequence: 10 },
]

const issueFilter = ref<IssueState>(0)
const searchInput = ref('')
const appliedSearch = ref('')
const rows = ref<RecipeBasisRow[]>([])
const details = ref<RecipeDetailRow[]>([])
const activeBasisKey = ref<string | null>(null)
const selectedBasisKeys = ref<string[]>([])
const activeDetailNo = ref<string | null>(null)
const selectedDetailNos = ref<string[]>([])
const basisMode = ref<BasisMode>(null)
const detailMode = ref<DetailMode>(null)
const basisDraft = ref<BasisForm>({ recipeGroup: '', recipeVersion: '1', description: '' })
const detailDraft = ref<DetailForm>({ recipeNo: '', recipeValue: '', sequence: '0', description: '', type: -1, stdValue: '', maximum: '', minimum: '' })
const basisSource = ref<RecipeBasisRow | null>(null)
const detailSource = ref<RecipeDetailRow | null>(null)
const basisDirty = ref(false)
const detailDirty = ref(false)
const busy = ref(false)
const error = ref('')
const notice = ref('')
const elementPickerOpen = ref(false)
const elementSearch = ref('')

function basisKey(row: Pick<RecipeBasisRow, 'recipeGroup' | 'recipeVersion'>): string {
  return `${row.recipeGroup}\u001f${row.recipeVersion}`
}
function detailKey(row: Pick<RecipeDetailRow, 'recipeGroup' | 'recipeVersion' | 'recipeNo'>): string {
  return `${row.recipeGroup}\u001f${row.recipeVersion}\u001f${row.recipeNo}`
}
function userNo(): string { return props.currentUserNo?.trim() || 'DEVUSER' }
function closePage(): void { window.history.back() }
function now(): string { return new Date().toISOString() }
function readStore(): RecipeStore {
  try {
    const parsed = JSON.parse(localStorage.getItem(storageKey) || 'null') as RecipeStore | null
    if (parsed && Array.isArray(parsed.basis) && Array.isArray(parsed.details)) return parsed
  } catch { /* reset malformed local demo data */ }
  const initial = structuredClone(seedStore)
  localStorage.setItem(storageKey, JSON.stringify(initial))
  return initial
}
function writeStore(store: RecipeStore): void { localStorage.setItem(storageKey, JSON.stringify(store)) }
function issueLabel(value: IssueState): string { return issueStates.find((item) => item.value === value)?.label ?? String(value) }
function typeLabel(value: RecipeType): string { return value === 0 ? 'Value' : value === 1 ? 'Range' : 'None' }
function dateText(value: string | null): string { return value ? new Date(value).toLocaleString('zh-TW', { hour12: false }) : '' }

const visibleRows = computed(() => rows.value
  .filter((row) => row.issueState === issueFilter.value)
  .filter((row) => !appliedSearch.value || row.recipeGroup.toLocaleUpperCase().includes(appliedSearch.value.toLocaleUpperCase()))
  .sort((a, b) => a.recipeGroup.localeCompare(b.recipeGroup) || a.recipeVersion - b.recipeVersion))
const activeBasis = computed(() => visibleRows.value.find((row) => basisKey(row) === activeBasisKey.value) ?? null)
const visibleDetails = computed(() => {
  if (!activeBasis.value) return []
  return details.value.filter((row) => row.recipeGroup === activeBasis.value?.recipeGroup && row.recipeVersion === activeBasis.value?.recipeVersion)
})
const filteredElements = computed(() => elementOptions.filter((item) =>
  !elementSearch.value || `${item.elementNo} ${item.description}`.toLocaleUpperCase().includes(elementSearch.value.toLocaleUpperCase())))
const basisDialogTitle = computed(() => basisMode.value === 'new' ? 'Recipe Basis Define (ADD)' : basisMode.value === 'edit' ? 'Recipe Basis Define (EDIT)' : basisMode.value === 'copy' ? 'Recipe Basis Define (Copy)' : 'Recipe Basis Define (Version)')
const detailDialogTitle = computed(() => detailMode.value === 'edit' ? 'Recipe Item Define (EDIT)' : 'Recipe Item Define (ADD)')
const canEditActive = computed(() => activeBasis.value?.issueState === 0)
const canVersionActive = computed(() => activeBasis.value?.issueState === 2 && activeBasis.value.curVersion === 1)

function loadDetails(row: RecipeBasisRow | null): void {
  if (!row || !useDevMock.value) { details.value = []; activeDetailNo.value = null; selectedDetailNos.value = []; return }
  const store = readStore()
  details.value = store.details.filter((item) => item.recipeGroup === row.recipeGroup && item.recipeVersion === row.recipeVersion)
  activeDetailNo.value = details.value[0]?.recipeNo ?? null
  selectedDetailNos.value = []
}
function setActiveBasis(row: RecipeBasisRow): void {
  activeBasisKey.value = basisKey(row)
  error.value = ''
  loadDetails(row)
}
function selectFirstVisible(): void {
  selectedBasisKeys.value = []
  const first = visibleRows.value[0]
  activeBasisKey.value = first ? basisKey(first) : null
  loadDetails(first ?? null)
}
function loadRows(): void {
  if (!useDevMock.value) {
    rows.value = []
    details.value = []
    activeBasisKey.value = null
    error.value = '設備 Recipe Basis 正式 API 尚未建置。正式環境不使用本機假資料。'
    return
  }
  rows.value = readStore().basis
  selectFirstVisible()
}
function applyQuery(): void { appliedSearch.value = searchInput.value.trim(); selectFirstVisible(); error.value = ''; notice.value = '' }
watch(issueFilter, () => { selectFirstVisible(); error.value = ''; notice.value = '' })
watch([appliedSearch, rows], () => {
  if (!visibleRows.value.some((row) => basisKey(row) === activeBasisKey.value)) selectFirstVisible()
}, { deep: true })
onMounted(loadRows)

function toggleBasisSelection(row: RecipeBasisRow): void {
  const key = basisKey(row)
  selectedBasisKeys.value = selectedBasisKeys.value.includes(key)
    ? selectedBasisKeys.value.filter((item) => item !== key)
    : [...selectedBasisKeys.value, key]
}
function toggleDetailSelection(row: RecipeDetailRow): void {
  const key = row.recipeNo
  selectedDetailNos.value = selectedDetailNos.value.includes(key)
    ? selectedDetailNos.value.filter((item) => item !== key)
    : [...selectedDetailNos.value, key]
}
function startBasis(mode: Exclude<BasisMode, null>): void {
  error.value = ''
  notice.value = ''
  basisSource.value = mode === 'new' ? null : activeBasis.value
  if (mode !== 'new' && !basisSource.value) { error.value = '請先選取 Recipe 資料。'; return }
  const source = basisSource.value
  if (mode === 'edit' && source?.issueState !== 0) { error.value = '只有未簽核的 Recipe 可以修改。'; return }
  if (mode === 'version' && !(source?.issueState === 2 && source.curVersion === 1)) { error.value = '只有已簽核且為目前版本的 Recipe 可以建立新版本。'; return }
  basisDraft.value = mode === 'new'
    ? { recipeGroup: '', recipeVersion: '1', description: '' }
    : mode === 'edit'
      ? { recipeGroup: source?.recipeGroup ?? '', recipeVersion: String(source?.recipeVersion ?? 1), description: source?.description ?? '' }
      : mode === 'copy'
        ? { recipeGroup: source?.recipeGroup ?? '', recipeVersion: '1', description: source?.description ?? '' }
        : { recipeGroup: source?.recipeGroup ?? '', recipeVersion: String((source?.recipeVersion ?? 0) + 1), description: source?.description ?? '' }
  basisDirty.value = false
  basisMode.value = mode
}
function normalizeRecipeGroup(): void { basisDraft.value.recipeGroup = basisDraft.value.recipeGroup.trim().toUpperCase() }
function closeBasisDialog(): void {
  if (basisDirty.value && !window.confirm('資料尚未套用，確定關閉？')) return
  basisMode.value = null
  basisSource.value = null
  basisDirty.value = false
}
function validateBasisDraft(): string {
  normalizeRecipeGroup()
  if (!basisDraft.value.recipeGroup) return 'Recipe ID 為必填。'
  if (basisDraft.value.recipeGroup.length > 50) return 'Recipe ID 不可超過 50 個字元。'
  const version = Number(basisDraft.value.recipeVersion)
  if (!Number.isInteger(version) || version < 0 || version > 99) return 'Version 必須是 0 至 99 的整數。'
  if (basisDraft.value.description.length > 255) return 'Description 不可超過 255 個字元。'
  const currentKey = basisSource.value ? basisKey(basisSource.value) : ''
  if (rows.value.some((row) => basisKey(row) === `${basisDraft.value.recipeGroup}\u001f${version}` && basisKey(row) !== currentKey)) return '相同 Recipe ID 與 Version 已存在。'
  return ''
}
function saveBasis(keepOpen = false): void {
  if (busy.value || !basisMode.value) return
  if (!useDevMock.value) { error.value = '正式 API 尚未建置，未寫入假資料。'; return }
  const validation = validateBasisDraft()
  if (validation) { error.value = validation; return }
  busy.value = true
  try {
    const store = readStore()
    const mode = basisMode.value
    const source = basisSource.value
    const time = now()
    const version = Number(basisDraft.value.recipeVersion)
    let saved: RecipeBasisRow
    if (mode === 'edit' && source) {
      const index = store.basis.findIndex((row) => basisKey(row) === basisKey(source))
      if (index < 0) throw new Error('Recipe 資料已不存在。')
      if (store.basis[index].dataStamp !== source.dataStamp) throw new Error('資料已被其他使用者修改。')
      saved = { ...store.basis[index], description: basisDraft.value.description || null, reviser: userNo(), reviseDate: time, dataStamp: store.basis[index].dataStamp + 1 }
      store.basis[index] = saved
    } else {
      saved = { recipeGroup: basisDraft.value.recipeGroup, recipeVersion: version, description: basisDraft.value.description || null, creator: userNo(), createDate: time, issueState: 0, curVersion: 0, dataStamp: 0, reviser: null, reviseDate: null }
      store.basis.push(saved)
      if ((mode === 'copy' || mode === 'version') && source) {
        const sourceDetails = store.details.filter((row) => row.recipeGroup === source.recipeGroup && row.recipeVersion === source.recipeVersion)
        for (const sourceDetail of sourceDetails) {
          store.details.push({
            recipeGroup: saved.recipeGroup, recipeVersion: saved.recipeVersion, recipeNo: sourceDetail.recipeNo,
            recipeValue: sourceDetail.recipeValue, sequence: sourceDetail.sequence ?? 0, description: sourceDetail.description,
            type: null, stdValue: null, maximum: null, minimum: null, dataStamp: 0, creator: null, createDate: null, reviser: null, reviseDate: null,
          })
        }
      }
    }
    writeStore(store)
    rows.value = store.basis
    if (issueFilter.value !== 0) issueFilter.value = 0
    appliedSearch.value = ''
    searchInput.value = ''
    activeBasisKey.value = basisKey(saved)
    loadDetails(saved)
    selectedBasisKeys.value = []
    basisDirty.value = false
    basisMode.value = keepOpen ? 'edit' : null
    basisSource.value = keepOpen ? saved : null
    if (keepOpen) basisDraft.value = { recipeGroup: saved.recipeGroup, recipeVersion: String(saved.recipeVersion), description: saved.description ?? '' }
    notice.value = mode === 'edit' ? 'Recipe Basis 已修改。' : mode === 'version' ? '新版本已建立，狀態為未簽核。' : mode === 'copy' ? 'Recipe Basis 已複製。' : 'Recipe Basis 已新增。'
    error.value = ''
  } catch (cause) { error.value = cause instanceof Error ? cause.message : 'Recipe Basis 儲存失敗。' }
  finally { busy.value = false }
}

function selectedBasisTargets(): RecipeBasisRow[] {
  const keys = new Set(selectedBasisKeys.value)
  if (activeBasis.value) keys.add(basisKey(activeBasis.value))
  return visibleRows.value.filter((row) => keys.has(basisKey(row)))
}
function deleteBasis(): void {
  const targets = selectedBasisTargets()
  if (!targets.length) { error.value = '請先選取要刪除的 Recipe Basis。'; return }
  if (targets.some((row) => row.issueState === 1)) { error.value = '簽核中的 Recipe 不可刪除。'; return }
  if (!window.confirm(`確定刪除選取的 ${targets.length} 筆 Recipe Basis 與其明細？`)) return
  if (!useDevMock.value) { error.value = '正式 API 尚未建置，未刪除資料。'; return }
  const store = readStore()
  for (const row of targets) {
    const index = store.basis.findIndex((item) => basisKey(item) === basisKey(row))
    if (index >= 0) store.basis.splice(index, 1)
    store.details = store.details.filter((item) => item.recipeGroup !== row.recipeGroup || item.recipeVersion !== row.recipeVersion)
  }
  writeStore(store)
  rows.value = store.basis
  selectedBasisKeys.value = []
  selectFirstVisible()
  notice.value = `已刪除 ${targets.length} 筆 Recipe Basis 與明細。`
  error.value = ''
}
function approveBasis(): void {
  const targets = selectedBasisTargets()
  if (!targets.length) { error.value = '請先選取要核准的 Recipe Basis。'; return }
  if (targets.some((row) => row.issueState !== 0)) { error.value = '只有未簽核的 Recipe 可以核准。'; return }
  if (!window.confirm(`確定核准選取的 ${targets.length} 筆 Recipe Basis？`)) return
  if (!useDevMock.value) { error.value = '正式 API 尚未建置，未核准資料。'; return }
  const store = readStore()
  for (const target of targets) {
    for (const row of store.basis) {
      if (row.recipeGroup === target.recipeGroup && row.curVersion === 1) row.curVersion = 0
    }
    const current = store.basis.find((row) => basisKey(row) === basisKey(target))
    if (current) { current.issueState = 2; current.curVersion = 1; current.reviser = userNo(); current.reviseDate = now(); current.dataStamp += 1 }
  }
  writeStore(store)
  rows.value = store.basis
  selectFirstVisible()
  notice.value = '開發假資料已直接核准並設為目前版本；正式簽核流程待 API 實作。'
  error.value = ''
}

function startDetail(mode: Exclude<DetailMode, null>): void {
  error.value = ''
  notice.value = ''
  if (!activeBasis.value) { error.value = '請先選取 Recipe Basis。'; return }
  if (!canEditActive.value) { error.value = '只有未簽核的 Recipe Basis 可以維護明細。'; return }
  detailSource.value = mode === 'edit' ? visibleDetails.value.find((row) => row.recipeNo === activeDetailNo.value) ?? null : null
  if (mode === 'edit' && !detailSource.value) { error.value = '請先選取 Recipe 明細。'; return }
  const source = detailSource.value
  detailDraft.value = source ? {
    recipeNo: source.recipeNo, recipeValue: source.recipeValue, sequence: String(source.sequence ?? 0), description: source.description ?? '',
    type: source.type === 0 || source.type === 1 ? source.type : -1,
    stdValue: source.stdValue == null ? '' : String(source.stdValue), maximum: source.maximum == null ? '' : String(source.maximum), minimum: source.minimum == null ? '' : String(source.minimum),
  } : { recipeNo: '', recipeValue: '', sequence: '0', description: '', type: -1, stdValue: '', maximum: '', minimum: '' }
  detailDirty.value = false
  detailMode.value = mode
}
function closeDetailDialog(): void {
  if (detailDirty.value && !window.confirm('資料尚未套用，確定關閉？')) return
  detailMode.value = null
  detailSource.value = null
  detailDirty.value = false
}
function updateRecipeValueFromSpec(field: 'stdValue' | 'maximum' | 'minimum'): void {
  if (detailDraft.value.type === 0 && field === 'stdValue' && detailDraft.value.stdValue.trim()) detailDraft.value.recipeValue = detailDraft.value.stdValue.trim()
  if (detailDraft.value.type === 1 && (field === 'minimum' || field === 'maximum') && (detailDraft.value.minimum.trim() || detailDraft.value.maximum.trim())) {
    detailDraft.value.recipeValue = `${detailDraft.value.minimum.trim()} ~ ${detailDraft.value.maximum.trim()}`
  }
  detailDirty.value = true
}
function setRecipeType(type: -1 | 0 | 1): void {
  detailDraft.value.type = type
  if (type === 0) { detailDraft.value.maximum = ''; detailDraft.value.minimum = '' }
  else if (type === 1) detailDraft.value.stdValue = ''
  else { detailDraft.value.stdValue = ''; detailDraft.value.maximum = ''; detailDraft.value.minimum = '' }
  detailDirty.value = true
}
function chooseElement(item: ElementOption): void {
  detailDraft.value.recipeNo = item.elementNo
  detailDraft.value.sequence = String(item.sequence)
  detailDraft.value.stdValue = item.defaultValue
  detailDraft.value.recipeValue = item.defaultValue
  detailDraft.value.type = 0
  elementPickerOpen.value = false
  detailDirty.value = true
}
function validateDetailDraft(): string {
  const active = activeBasis.value
  if (!active) return '請先選取 Recipe Basis。'
  if (!detailDraft.value.recipeNo.trim()) return 'Recipe No 為必填。'
  if (detailDraft.value.recipeNo.length > 20) return 'Recipe No 不可超過 20 個字元。'
  if (!detailDraft.value.recipeValue.trim()) return 'Recipe Value 為必填。'
  if (detailDraft.value.recipeValue.length > 255) return 'Recipe Value 不可超過 255 個字元。'
  const sequence = Number(detailDraft.value.sequence)
  if (!Number.isInteger(sequence) || sequence < 0 || sequence > 99) return 'Sequence 必須是 0 至 99 的整數。'
  if (detailDraft.value.description.length > 255) return 'Description 不可超過 255 個字元。'
  if (detailMode.value === 'new' && visibleDetails.value.some((row) => row.recipeNo === detailDraft.value.recipeNo.trim())) return '相同 Recipe No 已存在於此版本。'
  if (detailDraft.value.type === 0) {
    if (!detailDraft.value.stdValue.trim() || !Number.isFinite(Number(detailDraft.value.stdValue))) return 'StdValue 必須為數字。'
  }
  if (detailDraft.value.type === 1) {
    const maxText = detailDraft.value.maximum.trim(), minText = detailDraft.value.minimum.trim()
    if (!maxText && !minText) return 'Maximum 與 Minimum 至少須輸入一項。'
    if ((maxText && !Number.isFinite(Number(maxText))) || (minText && !Number.isFinite(Number(minText)))) return 'Maximum 與 Minimum 必須為數字。'
    const maximum = maxText ? Number(maxText) : 0, minimum = minText ? Number(minText) : 0
    if ((maximum !== 0 || minimum !== 0) && maximum <= minimum) return 'Maximum 必須大於 Minimum。'
  }
  return ''
}
function saveDetail(keepOpen = false): void {
  if (busy.value || !detailMode.value) return
  if (!useDevMock.value) { error.value = '正式 API 尚未建置，未寫入假資料。'; return }
  const validation = validateDetailDraft()
  if (validation) { error.value = validation; return }
  const parent = activeBasis.value
  if (!parent) return
  busy.value = true
  try {
    const store = readStore()
    const mode = detailMode.value
    const source = detailSource.value
    const sequence = Number(detailDraft.value.sequence)
    const time = now()
    let saved: RecipeDetailRow
    if (mode === 'edit' && source) {
      const index = store.details.findIndex((row) => detailKey(row) === detailKey(source))
      if (index < 0) throw new Error('Recipe 明細已不存在。')
      if (store.details[index].dataStamp !== source.dataStamp) throw new Error('明細已被其他使用者修改。')
      saved = { ...store.details[index], recipeValue: detailDraft.value.recipeValue.trim(), sequence, description: detailDraft.value.description || null,
        type: detailDraft.value.type, stdValue: detailDraft.value.type === 0 ? Number(detailDraft.value.stdValue) : null,
        maximum: detailDraft.value.type === 1 && detailDraft.value.maximum.trim() ? Number(detailDraft.value.maximum) : null,
        minimum: detailDraft.value.type === 1 && detailDraft.value.minimum.trim() ? Number(detailDraft.value.minimum) : null,
        reviser: userNo(), reviseDate: time, dataStamp: store.details[index].dataStamp + 1 }
      store.details[index] = saved
    } else {
      saved = { recipeGroup: parent.recipeGroup, recipeVersion: parent.recipeVersion, recipeNo: detailDraft.value.recipeNo.trim().toUpperCase(),
        recipeValue: detailDraft.value.recipeValue.trim(), sequence, description: detailDraft.value.description || null,
        type: detailDraft.value.type, stdValue: detailDraft.value.type === 0 ? Number(detailDraft.value.stdValue) : null,
        maximum: detailDraft.value.type === 1 && detailDraft.value.maximum.trim() ? Number(detailDraft.value.maximum) : null,
        minimum: detailDraft.value.type === 1 && detailDraft.value.minimum.trim() ? Number(detailDraft.value.minimum) : null,
        dataStamp: 0, creator: userNo(), createDate: time, reviser: null, reviseDate: null }
      store.details.push(saved)
    }
    writeStore(store)
    details.value = store.details.filter((row) => row.recipeGroup === parent.recipeGroup && row.recipeVersion === parent.recipeVersion)
    activeDetailNo.value = saved.recipeNo
    selectedDetailNos.value = []
    detailDirty.value = false
    detailMode.value = keepOpen ? 'edit' : null
    detailSource.value = keepOpen ? saved : null
    if (keepOpen) detailDraft.value = { recipeNo: saved.recipeNo, recipeValue: saved.recipeValue, sequence: String(saved.sequence ?? 0), description: saved.description ?? '', type: saved.type === 0 || saved.type === 1 ? saved.type : -1, stdValue: saved.stdValue == null ? '' : String(saved.stdValue), maximum: saved.maximum == null ? '' : String(saved.maximum), minimum: saved.minimum == null ? '' : String(saved.minimum) }
    notice.value = mode === 'edit' ? 'Recipe 明細已修改。' : 'Recipe 明細已新增。'
    error.value = ''
  } catch (cause) { error.value = cause instanceof Error ? cause.message : 'Recipe 明細儲存失敗。' }
  finally { busy.value = false }
}
function deleteDetails(): void {
  if (!activeBasis.value || !canEditActive.value) { error.value = '只有未簽核的 Recipe Basis 可以刪除明細。'; return }
  const targets = new Set(selectedDetailNos.value)
  if (activeDetailNo.value) targets.add(activeDetailNo.value)
  if (!targets.size) { error.value = '請先選取 Recipe 明細。'; return }
  if (!window.confirm(`確定刪除選取的 ${targets.size} 筆 Recipe 明細？`)) return
  if (!useDevMock.value) { error.value = '正式 API 尚未建置，未刪除明細。'; return }
  const parent = activeBasis.value
  const store = readStore()
  store.details = store.details.filter((row) => row.recipeGroup !== parent.recipeGroup || row.recipeVersion !== parent.recipeVersion || !targets.has(row.recipeNo))
  writeStore(store)
  details.value = store.details.filter((row) => row.recipeGroup === parent.recipeGroup && row.recipeVersion === parent.recipeVersion)
  activeDetailNo.value = details.value[0]?.recipeNo ?? null
  selectedDetailNos.value = []
  notice.value = `已刪除 ${targets.size} 筆 Recipe 明細。`
  error.value = ''
}
</script>

<template>
  <section class="recipe-basis-page" aria-label="設備參數資料">
    <div v-if="useDevMock" class="recipe-basis-dev-note">Development Preview：畫面使用本機 DEV 假資料，與 MES／正式資料庫隔離。Recipe 核准在此直接模擬；正式簽核由 API 處理。</div>
    <div class="recipe-basis-search">
      <div class="recipe-basis-state-filter" role="radiogroup" aria-label="簽核狀態">
        <label v-for="state in issueStates" :key="state.value"><input v-model="issueFilter" type="radio" :value="state.value">{{ state.label }}</label>
      </div>
      <label class="recipe-basis-search-field"><span>Recipe ID</span><input v-model="searchInput" type="text" maxlength="50" @keydown.enter.prevent="applyQuery"></label>
      <span class="recipe-basis-like">%</span><button type="button" @click="applyQuery">查詢</button>
    </div>

    <div class="recipe-basis-grid-section">
      <div class="recipe-basis-grid-wrap">
        <table class="recipe-basis-grid" aria-label="Recipe Basis 清單">
          <thead><tr><th class="recipe-basis-check-col"></th><th>Recipe ID</th><th>Version</th><th>Current</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th><th>狀態</th></tr></thead>
          <tbody>
            <tr v-for="row in visibleRows" :key="basisKey(row)" :class="{ 'recipe-basis-active': basisKey(row) === activeBasisKey }" @click="setActiveBasis(row)">
              <td class="recipe-basis-check-col"><input type="checkbox" :checked="selectedBasisKeys.includes(basisKey(row))" :aria-label="`選取 ${row.recipeGroup} v${row.recipeVersion}`" @click.stop="toggleBasisSelection(row)"></td>
              <td>{{ row.recipeGroup }}</td><td>{{ row.recipeVersion }}</td><td>{{ row.curVersion === 1 ? 'Yes' : 'No' }}</td><td>{{ row.description }}</td>
              <td>{{ row.creator }}</td><td>{{ dateText(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ dateText(row.reviseDate) }}</td><td>{{ issueLabel(row.issueState) }}</td>
            </tr>
            <tr v-if="visibleRows.length === 0"><td colspan="10" class="recipe-basis-empty">{{ useDevMock ? '沒有符合條件的 Recipe Basis。' : '正式 API 尚未建置。' }}</td></tr>
          </tbody>
        </table>
      </div>
      <div class="recipe-basis-actions" aria-label="Recipe Basis 操作">
        <button type="button" class="recipe-basis-primary" @click="startBasis('new')">新增(A)</button>
        <button type="button" :disabled="!canEditActive" @click="startBasis('edit')">修改(E)</button>
        <button type="button" :disabled="!activeBasis" @click="startBasis('copy')">複製(C)</button>
        <button type="button" :disabled="!selectedBasisTargets().length" @click="approveBasis">核准</button>
        <button type="button" :disabled="!selectedBasisTargets().length" @click="deleteBasis">刪除(D)</button>
        <button type="button" :disabled="!canVersionActive" @click="startBasis('version')">Version(V)</button>
        <button type="button" @click="closePage">關閉</button>
      </div>
    </div>

    <div class="recipe-basis-count">資料總筆數 {{ visibleRows.length }}</div>
    <p v-if="error" class="recipe-basis-message recipe-basis-error" role="alert">{{ error }}</p>
    <p v-else-if="notice" class="recipe-basis-message recipe-basis-success" role="status">{{ notice }}</p>

    <div class="recipe-detail-title">Recipe Detail{{ activeBasis ? ` — ${activeBasis.recipeGroup} / ${activeBasis.recipeVersion}` : '' }}</div>
    <div class="recipe-detail-section">
      <div class="recipe-detail-grid-wrap">
        <table class="recipe-detail-grid" aria-label="Recipe 明細">
          <thead><tr><th class="recipe-basis-check-col"></th><th>RecipeNo</th><th>Value</th><th>Type</th><th>StdValue</th><th>Maximum</th><th>Minimum</th><th>Sequence</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
          <tbody>
            <tr v-for="row in visibleDetails" :key="detailKey(row)" :class="{ 'recipe-detail-active': row.recipeNo === activeDetailNo }" @click="activeDetailNo = row.recipeNo">
              <td class="recipe-basis-check-col"><input type="checkbox" :checked="selectedDetailNos.includes(row.recipeNo)" :aria-label="`選取 ${row.recipeNo}`" @click.stop="toggleDetailSelection(row)"></td>
              <td>{{ row.recipeNo }}</td><td>{{ row.recipeValue }}</td><td>{{ typeLabel(row.type) }}</td><td>{{ row.stdValue ?? '' }}</td><td>{{ row.maximum ?? '' }}</td><td>{{ row.minimum ?? '' }}</td><td>{{ row.sequence ?? '' }}</td><td>{{ row.description }}</td>
              <td>{{ row.creator }}</td><td>{{ dateText(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ dateText(row.reviseDate) }}</td>
            </tr>
            <tr v-if="visibleDetails.length === 0"><td colspan="13" class="recipe-basis-empty">{{ activeBasis ? '此版本沒有 Recipe 明細。' : '請先選取 Recipe Basis。' }}</td></tr>
          </tbody>
        </table>
      </div>
      <div class="recipe-detail-actions" aria-label="Recipe 明細操作">
        <button type="button" class="recipe-basis-primary" :disabled="!canEditActive" @click="startDetail('new')">ADD</button>
        <button type="button" :disabled="!canEditActive || !activeDetailNo" @click="startDetail('edit')">EDIT</button>
        <button type="button" :disabled="!canEditActive || (!activeDetailNo && !selectedDetailNos.length)" @click="deleteDetails">DEL</button>
      </div>
    </div>

    <div v-if="basisMode" class="recipe-basis-overlay" @click.self="closeBasisDialog">
      <form class="recipe-basis-dialog" role="dialog" aria-modal="true" :aria-label="basisDialogTitle" @submit.prevent="saveBasis(false)">
        <header class="recipe-basis-dialog-title">{{ basisDialogTitle }}</header>
        <fieldset class="recipe-basis-fields"><legend>Recipe Data</legend>
          <label><span>Recipe ID</span><input v-model="basisDraft.recipeGroup" type="text" maxlength="50" :readonly="basisMode === 'edit' || basisMode === 'version'" :class="{ 'recipe-basis-yellow': basisMode === 'new' || basisMode === 'copy' }" @input="basisDirty = true" @blur="normalizeRecipeGroup"></label>
          <label><span>Version</span><input v-model="basisDraft.recipeVersion" type="number" min="0" max="99" readonly></label>
          <label class="recipe-basis-description"><span>Description</span><textarea v-model="basisDraft.description" maxlength="255" rows="3" @input="basisDirty = true"></textarea></label>
        </fieldset>
        <fieldset class="recipe-basis-audit"><legend>建立／修改資料</legend>
          <label><span>建立人</span><input :value="basisMode === 'edit' ? basisSource?.creator || '' : userNo()" readonly></label><label><span>建立日期</span><input :value="basisMode === 'edit' ? dateText(basisSource?.createDate || null) : dateText(now())" readonly></label>
          <label><span>修改人</span><input :value="basisMode === 'edit' ? userNo() : ''" readonly></label><label><span>修改日期</span><input :value="basisMode === 'edit' ? dateText(now()) : ''" readonly></label>
        </fieldset>
        <footer><button type="submit" class="recipe-basis-primary" :disabled="busy">確定</button><button type="button" :disabled="busy" @click="closeBasisDialog">關閉</button><button type="button" :disabled="busy || !basisDirty" @click="saveBasis(true)">套用</button></footer>
      </form>
    </div>

    <div v-if="detailMode" class="recipe-basis-overlay" @click.self="closeDetailDialog">
      <form class="recipe-detail-dialog" role="dialog" aria-modal="true" :aria-label="detailDialogTitle" @submit.prevent="saveDetail(false)">
        <header class="recipe-basis-dialog-title">{{ detailDialogTitle }}</header>
        <fieldset class="recipe-detail-fields"><legend>Recipe Item Data</legend>
          <div class="recipe-detail-pair"><label><span>Recipe ID</span><input :value="activeBasis?.recipeGroup" readonly></label><label><span>Version</span><input :value="activeBasis?.recipeVersion" readonly></label></div>
          <label class="recipe-detail-number-picker"><span>Recipe No</span><input v-model="detailDraft.recipeNo" type="text" maxlength="20" readonly><button type="button" aria-label="選取 Element" :disabled="detailMode === 'edit'" @click="elementPickerOpen = true">...</button></label>
          <div class="recipe-detail-pair"><label><span>Recipe Value</span><input v-model="detailDraft.recipeValue" class="recipe-basis-yellow" type="text" maxlength="255" @input="detailDirty = true"></label><label><span>Sequence</span><input v-model="detailDraft.sequence" type="number" min="0" max="99" @input="detailDirty = true"></label></div>
          <div class="recipe-detail-type"><span>Type</span><label><input type="radio" :checked="detailDraft.type === -1" @change="setRecipeType(-1)">None</label><label><input type="radio" :checked="detailDraft.type === 0" @change="setRecipeType(0)">Value</label><label><input type="radio" :checked="detailDraft.type === 1" @change="setRecipeType(1)">Range</label></div>
          <label><span>StdValue</span><input v-model="detailDraft.stdValue" type="number" step="0.0001" :readonly="detailDraft.type !== 0" @input="detailDirty = true" @blur="updateRecipeValueFromSpec('stdValue')"></label>
          <div class="recipe-detail-pair"><label><span>Minimum</span><input v-model="detailDraft.minimum" type="number" step="0.0001" :readonly="detailDraft.type !== 1" @input="detailDirty = true" @blur="updateRecipeValueFromSpec('minimum')"></label><label><span>Maximum</span><input v-model="detailDraft.maximum" type="number" step="0.0001" :readonly="detailDraft.type !== 1" @input="detailDirty = true" @blur="updateRecipeValueFromSpec('maximum')"></label></div>
          <label class="recipe-detail-description"><span>Description</span><textarea v-model="detailDraft.description" maxlength="255" rows="3" @input="detailDirty = true"></textarea></label>
        </fieldset>
        <fieldset class="recipe-basis-audit"><legend>建立／修改資料</legend>
          <label><span>建立人</span><input :value="detailSource?.creator || userNo()" readonly></label><label><span>建立日期</span><input :value="dateText(detailSource?.createDate || now())" readonly></label>
          <label><span>修改人</span><input :value="detailMode === 'edit' ? userNo() : ''" readonly></label><label><span>修改日期</span><input :value="detailMode === 'edit' ? dateText(now()) : ''" readonly></label>
        </fieldset>
        <footer><button type="submit" class="recipe-basis-primary" :disabled="busy">確定</button><button type="button" :disabled="busy" @click="closeDetailDialog">關閉</button><button type="button" :disabled="busy || !detailDirty" @click="saveDetail(true)">套用</button></footer>
      </form>
    </div>

    <div v-if="elementPickerOpen" class="recipe-basis-overlay recipe-element-overlay" @click.self="elementPickerOpen = false">
      <section class="recipe-element-dialog" role="dialog" aria-modal="true" aria-label="選擇 Element">
        <header class="recipe-basis-dialog-title">選擇 Recipe No（ElementType = 9）</header>
        <label class="recipe-element-search"><span>查詢</span><input v-model="elementSearch" type="text" @keydown.enter.prevent></label>
        <div class="recipe-element-list"><table><thead><tr><th>ElementNo</th><th>Description</th><th>DefaultValue</th><th>Sequence</th><th></th></tr></thead><tbody>
          <tr v-for="item in filteredElements" :key="item.elementNo" @dblclick="chooseElement(item)"><td>{{ item.elementNo }}</td><td>{{ item.description }}</td><td>{{ item.defaultValue }}</td><td>{{ item.sequence }}</td><td><button type="button" @click="chooseElement(item)">選取</button></td></tr>
          <tr v-if="filteredElements.length === 0"><td colspan="5" class="recipe-basis-empty">沒有符合的 Element。</td></tr>
        </tbody></table></div>
        <footer><button type="button" @click="elementPickerOpen = false">關閉</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.recipe-basis-page{display:flex;height:100%;min-height:0;flex-direction:column;gap:6px;color:#333;font-size:13px}
.recipe-basis-dev-note{padding:7px 10px;border:1px solid #d8c577;background:#fff6c8;color:#66551b}
.recipe-basis-search{display:flex;min-height:38px;align-items:center;gap:8px;padding:5px 9px;border:1px solid #c9c9c9;background:#fafafa}
.recipe-basis-state-filter{display:flex;align-items:center;gap:12px;margin-right:8px}.recipe-basis-state-filter label{display:flex;align-items:center;gap:3px;white-space:nowrap}.recipe-basis-state-filter input,.recipe-basis-check-col input,.recipe-detail-type input{margin:0;accent-color:#ad831b}
.recipe-basis-search-field{display:flex;align-items:center;gap:7px;white-space:nowrap}.recipe-basis-search-field input{width:min(220px,24vw);min-height:28px;padding:4px 7px;border:1px solid #aaa;background:#fff0b3;color:#333;font:inherit}.recipe-basis-like{margin-left:-7px;color:#555}
.recipe-basis-page button{min-height:30px;padding:4px 10px;border:1px solid #bdbdbd;border-radius:3px;background:#fafafa;color:#333;font:inherit;cursor:pointer}.recipe-basis-page button:hover{background:#fff2c3;border-color:#c8aa54}.recipe-basis-page button:disabled{opacity:.5;cursor:not-allowed}.recipe-basis-page button.recipe-basis-primary{background:#f5a623;border-color:#e49a24;color:white}
.recipe-basis-grid-section,.recipe-detail-section{display:grid;min-height:0;grid-template-columns:minmax(0,1fr) 94px;gap:7px}.recipe-basis-grid-section{flex:1.05}.recipe-detail-section{flex:.95;min-height:160px}
.recipe-basis-grid-wrap,.recipe-detail-grid-wrap{min-height:0;overflow:auto;border:1px solid #bdbdbd;background:#fff}.recipe-basis-grid,.recipe-detail-grid{width:100%;border-collapse:collapse;white-space:nowrap}.recipe-basis-grid{min-width:1120px}.recipe-detail-grid{min-width:1460px}
.recipe-basis-grid th,.recipe-detail-grid th{position:sticky;top:0;z-index:1;padding:5px 7px;border:1px solid #d2c36d;background:#e7d978;color:#453d20;text-align:left;font-weight:600}.recipe-basis-grid td,.recipe-detail-grid td{height:27px;padding:4px 7px;border:1px solid #d6d6d6}.recipe-basis-grid tbody tr,.recipe-detail-grid tbody tr{cursor:pointer}.recipe-basis-grid tbody tr:nth-child(even),.recipe-detail-grid tbody tr:nth-child(even){background:#fafafa}.recipe-basis-grid tbody tr:hover,.recipe-detail-grid tbody tr:hover{background:#fff6d6}.recipe-basis-grid tbody tr.recipe-basis-active,.recipe-detail-grid tbody tr.recipe-detail-active{background:#fff0b7}
.recipe-basis-check-col{width:27px;text-align:center!important}.recipe-basis-empty{text-align:center;color:#777;padding:14px!important}.recipe-basis-actions,.recipe-detail-actions{display:flex;flex-direction:column;gap:6px}.recipe-basis-actions button,.recipe-detail-actions button{width:100%}.recipe-basis-count{color:#777;font-size:12px}.recipe-basis-message{margin:0;padding:7px 10px;border:1px solid}.recipe-basis-error{border-color:#dca7a2;background:#fff1ef;color:#942f24}.recipe-basis-success{border-color:#c8d8ac;background:#f4f8eb;color:#496622}
.recipe-detail-title{padding:5px 8px;border:1px solid #c9c9c9;background:#f6f6f6;font-weight:600;color:#555}
.recipe-basis-overlay{position:fixed;inset:0;z-index:70;display:flex;align-items:center;justify-content:center;padding:16px;background:rgba(25,25,25,.35)}.recipe-basis-dialog,.recipe-detail-dialog,.recipe-element-dialog{width:min(540px,96vw);max-height:95vh;overflow:auto;padding:12px;border:1px solid #aaa;background:#f8f8f8;box-shadow:0 5px 24px rgba(0,0,0,.24)}.recipe-detail-dialog{width:min(570px,96vw)}.recipe-basis-dialog-title{margin:-12px -12px 10px;padding:9px 12px;border-bottom:1px solid #c8c8c8;background:#eee;font-size:15px;font-weight:600}.recipe-basis-fields,.recipe-detail-fields,.recipe-basis-audit{margin:0 0 9px;padding:12px;border:1px solid #bdbdbd}.recipe-basis-fields legend,.recipe-detail-fields legend,.recipe-basis-audit legend{padding:0 5px;color:#555}
.recipe-basis-fields{display:grid;grid-template-columns:1fr 150px;gap:9px 12px}.recipe-basis-fields>label,.recipe-detail-fields>label,.recipe-detail-pair label{display:grid;grid-template-columns:100px minmax(0,1fr);align-items:center;gap:7px}.recipe-basis-fields>label>span,.recipe-detail-fields>label>span,.recipe-detail-pair label>span{color:#555;text-align:right}.recipe-basis-fields input,.recipe-basis-fields textarea,.recipe-detail-fields input,.recipe-detail-fields textarea,.recipe-element-search input{min-width:0;min-height:28px;padding:4px 7px;border:1px solid #a9a9a9;background:white;color:#333;font:inherit}.recipe-basis-fields input[readonly],.recipe-basis-fields textarea[readonly],.recipe-detail-fields input[readonly],.recipe-detail-fields input:read-only{background:#eee}.recipe-basis-fields input.recipe-basis-yellow,.recipe-detail-fields input.recipe-basis-yellow{background:#fff0b3}.recipe-basis-fields .recipe-basis-description{grid-column:1/-1;align-items:start}.recipe-basis-fields textarea,.recipe-detail-fields textarea{resize:vertical}.recipe-basis-audit{display:grid;grid-template-columns:1fr 1fr;gap:8px}.recipe-basis-audit label{display:grid;grid-template-columns:64px minmax(0,1fr);align-items:center;gap:6px}.recipe-basis-audit label>span{color:#555;text-align:right}.recipe-basis-audit input{min-width:0;min-height:26px;padding:3px 6px;border:1px solid #ccc;background:#eee;color:#666;font:inherit}.recipe-basis-dialog footer,.recipe-detail-dialog footer,.recipe-element-dialog footer{display:flex;justify-content:flex-end;gap:8px;margin-top:9px}.recipe-detail-pair{display:grid;grid-template-columns:1fr 1fr;gap:9px;margin-bottom:9px}.recipe-detail-type{display:flex;align-items:center;gap:14px;margin:8px 0 11px 107px}.recipe-detail-type>span{margin-left:-107px;width:100px;color:#555;text-align:right}.recipe-detail-type label{display:flex;align-items:center;gap:4px}.recipe-detail-number-picker{grid-template-columns:100px minmax(0,1fr) 34px!important;margin-bottom:9px}.recipe-detail-number-picker button{min-width:32px;padding:3px}.recipe-detail-fields>.recipe-detail-pair:last-of-type{margin-top:9px}.recipe-detail-description{align-items:start!important}.recipe-element-overlay{z-index:80}.recipe-element-dialog{width:min(760px,96vw)}.recipe-element-search{display:flex;align-items:center;gap:8px;margin-bottom:8px}.recipe-element-search input{flex:1}.recipe-element-list{max-height:52vh;overflow:auto;border:1px solid #ccc;background:#fff}.recipe-element-list table{width:100%;border-collapse:collapse;white-space:nowrap}.recipe-element-list th{position:sticky;top:0;padding:6px;border:1px solid #d2c36d;background:#e7d978;text-align:left}.recipe-element-list td{padding:5px 7px;border:1px solid #ddd}
@media(max-width:900px){.recipe-basis-search{flex-wrap:wrap}.recipe-basis-state-filter{flex-wrap:wrap}.recipe-basis-grid-section,.recipe-detail-section{grid-template-columns:minmax(0,1fr)}.recipe-basis-actions,.recipe-detail-actions{flex-direction:row;flex-wrap:wrap}.recipe-basis-actions button,.recipe-detail-actions button{width:auto}.recipe-basis-grid-section{flex:none;height:40vh}.recipe-detail-section{flex:none;height:35vh}.recipe-basis-fields,.recipe-basis-audit{grid-template-columns:1fr}.recipe-detail-pair{grid-template-columns:1fr}}
</style>
