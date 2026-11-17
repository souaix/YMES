<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type State = 0 | 1 | 2 | -1
type Mode = 'add' | 'edit' | 'copy' | null
interface MaterialType { materialType: string; materialTypeName: string | null; description: string | null; issueState: State; creator: string | null; createDate: string | null; reviser: string | null; reviseDate: string | null; dataStamp: number | null }
interface MaterialProperty { material: string; propertyNo: string; defaultValue: string | null; propertySequence: number | null; issueState: State; description: string | null; creator: string | null; createDate: string | null; reviser: string | null; reviseDate: string | null; dataStamp: number | null }
interface GumRule { materialType: string; materialNo: string; accMin: number; accMax: number; reforzenTimes: number; refrozenFlag: number; frozenMin: number | null; reacclimateFlag: number; reacclimateMin: number | null; reacclimateMax: number | null; dataStamp: number | null; creator: string | null; createDate: string | null; reviser: string | null; reviseDate: string | null }
interface Store { types: MaterialType[]; properties: MaterialProperty[]; gumRules: GumRule[] }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const useMock = computed(() => import.meta.env.DEV && props.developmentPreview)
const rootApi = 'api/material-types'
const storageKey = 'ymes-dev-mock:mtl-material-type:v1'
const states: { value: State; label: string }[] = [{ value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' }]
const gumMaterialTypes = ['DEV-GUM-ADHESIVE']
const multiTimeControl = true
const seed: Store = {
  types: [
    { materialType: 'DEV-RESIN', materialTypeName: '開發用樹脂', description: '一般物料類別假資料', issueState: 0, creator: 'DEV001', createDate: '2026-11-02T08:00:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
    { materialType: 'DEV-GUM-ADHESIVE', materialTypeName: '開發用固晶膠', description: '固晶膠時效設定示例', issueState: 0, creator: 'DEV001', createDate: '2026-11-03T08:00:00+08:00', reviser: 'DEV001', reviseDate: '2026-11-04T09:00:00+08:00', dataStamp: 1 },
    { materialType: 'DEV-PACK', materialTypeName: '包裝物料', description: '已核准範例', issueState: 2, creator: 'DEV001', createDate: '2026-11-05T08:00:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-06T09:00:00+08:00', dataStamp: 2 },
  ],
  properties: [
    { material: 'DEV-RESIN', propertyNo: 'DEV-COLOR', defaultValue: '透明', propertySequence: 1, issueState: 0, description: '開發用屬性', creator: 'DEV001', createDate: '2026-11-02T08:10:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
    { material: 'DEV-GUM-ADHESIVE', propertyNo: 'DEV-VISCOSITY', defaultValue: '120', propertySequence: 1, issueState: 2, description: '黏度示範', creator: 'DEV001', createDate: '2026-11-03T08:10:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-04T09:10:00+08:00', dataStamp: 1 },
  ],
  gumRules: [{ materialType: 'DEV-GUM-ADHESIVE', materialNo: '*', accMin: 2, accMax: 8, reforzenTimes: 3, refrozenFlag: 1, frozenMin: 1, reacclimateFlag: 0, reacclimateMin: null, reacclimateMax: null, dataStamp: 1, creator: 'DEV001', createDate: '2026-11-03T08:20:00+08:00', reviser: 'DEV001', reviseDate: '2026-11-04T09:20:00+08:00' }],
}

const rows = ref<MaterialType[]>([])
const query = ref('')
const issueState = ref<State>(0)
const selectedKeys = ref<string[]>([])
const activeKey = ref('')
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const editorMode = ref<Mode>(null)
const sourceType = ref<MaterialType | null>(null)
const typeForm = ref({ materialType: '', materialTypeName: '', description: '' })
const typeAudit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const typeError = ref('')
const propertiesOpen = ref(false)
const propertyRows = ref<MaterialProperty[]>([])
const propertyState = ref<State>(0)
const propertyKey = ref('')
const propertyMode = ref<Mode>(null)
const sourceProperty = ref<MaterialProperty | null>(null)
const propertyForm = ref({ propertyNo: '', defaultValue: '', propertySequence: '1', description: '' })
const propertyAudit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const propertyError = ref('')
const gumOpen = ref(false)
const gumRule = ref<GumRule | null>(null)
const gumForm = ref({ accMin: '0', accMax: '0', refrozenFlag: false, frozenMin: '', reforzenTimes: '1', reacclimateFlag: false, reacclimateMin: '', reacclimateMax: '' })
const gumAudit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const gumError = ref('')

const activeType = computed(() => rows.value.find(row => row.materialType === activeKey.value) ?? null)
const visibleRows = computed(() => rows.value.filter(row => row.issueState === issueState.value && row.materialType.toLocaleLowerCase().includes(query.value.trim().toLocaleLowerCase())).sort((a, b) => a.materialType.localeCompare(b.materialType)))
const visibleProperties = computed(() => propertyRows.value.filter(row => row.issueState === propertyState.value).sort((a, b) => (a.propertySequence ?? 0) - (b.propertySequence ?? 0) || a.propertyNo.localeCompare(b.propertyNo)))
const activeProperty = computed(() => visibleProperties.value.find(row => row.propertyNo === propertyKey.value) ?? null)
const typeTitle = computed(() => `物料類別資料（${editorMode.value === 'add' ? '新增' : editorMode.value === 'copy' ? '複製' : '修改'}）`)
const propertyTitle = computed(() => `物料屬性（${propertyMode.value === 'add' ? '新增' : propertyMode.value === 'copy' ? '複製' : '修改'}）`)

function cloneSeed(): Store { return structuredClone(seed) }
function readStore(): Store {
  try { const raw = window.localStorage.getItem(storageKey); if (!raw) return cloneSeed(); const parsed = JSON.parse(raw) as Partial<Store>; return { types: parsed.types ?? structuredClone(seed.types), properties: parsed.properties ?? structuredClone(seed.properties), gumRules: parsed.gumRules ?? structuredClone(seed.gumRules) } }
  catch { return cloneSeed() }
}
function writeStore(store: Store): void { window.localStorage.setItem(storageKey, JSON.stringify(store)) }
function now(): string { return new Date().toISOString() }
function enc(value: string): string { return encodeURIComponent(value) }
function apiUrl(path: string): string { const base = props.apiBaseUrl.trim(); if (!base) throw new Error('目前環境尚未設定 API 位址。'); return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString() }
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init })
  if (response.status === 401) { window.location.assign(appBase + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) { const problem = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null; if (response.status === 404) throw new Error('物料類別 API 尚未建立；請依 todolist_homepc.md 完成 MTL ML102。'); throw new Error(problem?.detail || problem?.message || problem?.title || `API 回應錯誤 (${response.status})。`) }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function stamp(value: number | null): string { return value === null ? 'dataStamp=0&dataStampIsNull=true' : `dataStamp=${enc(String(value))}` }
function dateText(value: string | null): string { if (!value) return ''; const date = new Date(value); return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat('zh-TW', { dateStyle: 'short', timeStyle: 'medium', hour12: false }).format(date) }
function stateText(value: number): string { return states.find(item => item.value === value)?.label ?? String(value) }

async function loadTypes(): Promise<void> {
  loading.value = true; errorMessage.value = ''
  try {
    if (useMock.value) { const q = query.value.trim().toLocaleLowerCase(); rows.value = readStore().types.filter(row => row.issueState === issueState.value && (!q || row.materialType.toLocaleLowerCase().includes(q))) }
    else { const params = new URLSearchParams({ issueState: String(issueState.value) }); if (query.value.trim()) params.set('materialType', query.value.trim()); rows.value = await request<MaterialType[]>(`${rootApi}?${params}`) }
    selectedKeys.value = []; activeKey.value = rows.value[0]?.materialType ?? ''
  } catch (error) { rows.value = []; activeKey.value = ''; errorMessage.value = error instanceof Error ? error.message : '物料類別載入失敗。' }
  finally { loading.value = false }
}
function selectType(row: MaterialType, event: MouseEvent): void {
  activeKey.value = row.materialType
  if (event.ctrlKey || event.metaKey) selectedKeys.value = selectedKeys.value.includes(row.materialType) ? selectedKeys.value.filter(key => key !== row.materialType) : [...selectedKeys.value, row.materialType]
  else if (event.shiftKey && selectedKeys.value.length) { const start = visibleRows.value.findIndex(item => item.materialType === selectedKeys.value[0]); const end = visibleRows.value.findIndex(item => item.materialType === row.materialType); selectedKeys.value = visibleRows.value.slice(Math.min(start, end), Math.max(start, end) + 1).map(item => item.materialType) }
  else selectedKeys.value = [row.materialType]
}
function targets(): MaterialType[] { const keys = selectedKeys.value.length ? selectedKeys.value : activeKey.value ? [activeKey.value] : []; return keys.map(key => rows.value.find(row => row.materialType === key)).filter((row): row is MaterialType => Boolean(row)) }
function openTypeEditor(mode: Exclude<Mode, null>): void {
  const row = activeType.value
  if ((mode === 'edit' || mode === 'copy') && !row) return
  if (mode === 'edit' && row?.issueState !== 0) { errorMessage.value = '只有未簽核資料可以修改。'; return }
  editorMode.value = mode; sourceType.value = mode === 'add' ? null : row; typeError.value = ''
  typeForm.value = row && mode !== 'add' ? { materialType: row.materialType, materialTypeName: row.materialTypeName ?? '', description: row.description ?? '' } : { materialType: '', materialTypeName: '', description: '' }
  typeAudit.value = mode === 'edit' && row ? { creator: row.creator ?? '', createDate: row.createDate ?? '', reviser: row.reviser ?? '', reviseDate: row.reviseDate ?? '' } : { creator: props.currentUserNo || 'DEV001', createDate: now(), reviser: '', reviseDate: '' }
}
function closeTypeEditor(): void { editorMode.value = null; sourceType.value = null; typeError.value = '' }
async function saveType(closeAfter: boolean): Promise<void> {
  if (!editorMode.value || busy.value) return
  typeForm.value.materialType = typeForm.value.materialType.trim().toUpperCase(); typeForm.value.materialTypeName = typeForm.value.materialTypeName.trim(); typeForm.value.description = typeForm.value.description.trim(); typeError.value = ''
  if (!typeForm.value.materialType) { typeError.value = '物料類別編號為必填。'; return }
  if (typeForm.value.materialType.length > 50 || typeForm.value.materialTypeName.length > 50 || typeForm.value.description.length > 255) { typeError.value = '欄位長度超過資料庫限制。'; return }
  busy.value = true
  try {
    const mode = editorMode.value; const key = typeForm.value.materialType; const payload = { materialType: key, materialTypeName: typeForm.value.materialTypeName, description: typeForm.value.description }
    let saved: MaterialType | null = null
    if (useMock.value) {
      const store = readStore()
      if (store.types.some(row => row.materialType.toUpperCase() === key && !(mode === 'edit' && sourceType.value?.materialType === row.materialType))) throw new Error('相同物料類別編號已存在。')
      if (['DEV-MATERIAL-001', 'DEV-SHARED-CODE'].includes(key)) throw new Error('物料類別編號不可與物料編號相同。')
      const time = now()
      saved = mode === 'edit' && sourceType.value
        ? { ...sourceType.value, ...payload, reviser: props.currentUserNo || 'DEV001', reviseDate: time, dataStamp: (sourceType.value.dataStamp ?? 0) + 1 }
        : { ...payload, issueState: 0, creator: props.currentUserNo || 'DEV001', createDate: time, reviser: null, reviseDate: null, dataStamp: 0 }
      if (mode === 'edit' && sourceType.value) store.types = store.types.map(row => row.materialType === sourceType.value?.materialType ? saved! : row)
      else {
        store.types.push(saved)
        if (mode === 'copy' && sourceType.value) {
          const sourceRule = store.gumRules.find(rule => rule.materialType === sourceType.value?.materialType && rule.materialNo === '*')
          if (sourceRule) store.gumRules.push({ ...sourceRule, materialType: key, materialNo: '*', creator: props.currentUserNo || 'DEV001', createDate: time, reviser: null, reviseDate: null, dataStamp: 0 })
        }
      }
      writeStore(store)
    } else if (mode === 'edit' && sourceType.value) {
      await request(`${rootApi}/${enc(sourceType.value.materialType)}?${stamp(sourceType.value.dataStamp)}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
    } else if (mode === 'copy' && sourceType.value) {
      await request(`${rootApi}/${enc(sourceType.value.materialType)}/copy`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
    } else {
      await request(rootApi, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
    }
    await loadTypes(); const selected = rows.value.find(row => row.materialType === key) ?? saved
    if (closeAfter) closeTypeEditor(); else if (selected) { editorMode.value = 'edit'; sourceType.value = selected; typeForm.value = { materialType: selected.materialType, materialTypeName: selected.materialTypeName ?? '', description: selected.description ?? '' }; typeAudit.value = { creator: selected.creator ?? '', createDate: selected.createDate ?? '', reviser: selected.reviser ?? '', reviseDate: selected.reviseDate ?? '' } }
  } catch (error) { typeError.value = error instanceof Error ? error.message : '物料類別儲存失敗。' }
  finally { busy.value = false }
}
async function deleteTypes(): Promise<void> {
  const selected = targets(); if (!selected.length) return
  if (selected.some(row => row.issueState === 1)) { errorMessage.value = '簽核中的物料類別不可刪除。'; return }
  if (!window.confirm(`確定處理 ${selected.length} 筆物料類別？已核准資料將設為不使用。`)) return
  busy.value = true; let done = 0
  try {
    for (const row of selected) {
      if (useMock.value) { const store = readStore(); if (row.issueState === 2) store.types = store.types.map(item => item.materialType === row.materialType ? { ...item, issueState: -1, dataStamp: (item.dataStamp ?? 0) + 1, reviser: props.currentUserNo || 'DEV001', reviseDate: now() } : item); else { store.types = store.types.filter(item => item.materialType !== row.materialType); store.properties = store.properties.filter(item => item.material !== row.materialType); store.gumRules = store.gumRules.filter(item => item.materialType !== row.materialType) } writeStore(store) }
      else await request(`${rootApi}/${enc(row.materialType)}?issueState=${row.issueState}&${stamp(row.dataStamp)}`, { method: 'DELETE' })
      done++
    }
    await loadTypes()
  } catch (error) { const message = `已完成 ${done} 筆；刪除停止：${error instanceof Error ? error.message : '刪除失敗。'}`; await loadTypes(); errorMessage.value = message }
  finally { busy.value = false }
}
async function approveTypes(): Promise<void> {
  const selected = targets(); if (!selected.length) return
  if (selected.some(row => row.issueState !== 0)) { errorMessage.value = '只有未簽核資料可以核准。'; return }
  if (useMock.value && selected.some(row => gumMaterialTypes.includes(row.materialType) && !readStore().gumRules.some(rule => rule.materialType === row.materialType && rule.materialNo === '*'))) { errorMessage.value = '此固晶膠類尚未設定膠類時效，無法核准。'; return }
  if (!window.confirm(`確定核准 ${selected.length} 筆物料類別？`)) return
  busy.value = true; let done = 0
  try { for (const row of selected) { if (useMock.value) { const store = readStore(); store.types = store.types.map(item => item.materialType === row.materialType ? { ...item, issueState: 2, dataStamp: (item.dataStamp ?? 0) + 1, reviser: props.currentUserNo || 'DEV001', reviseDate: now() } : item); writeStore(store) } else await request(`${rootApi}/${enc(row.materialType)}/approve?${stamp(row.dataStamp)}`, { method: 'POST' }); done++ } await loadTypes() }
  catch (error) { const message = `已完成 ${done} 筆；核准停止：${error instanceof Error ? error.message : '核准失敗。'}`; await loadTypes(); errorMessage.value = message }
  finally { busy.value = false }
}

async function openProperties(): Promise<void> {
  if (!activeType.value) return
  propertiesOpen.value = true; propertyState.value = 0; propertyKey.value = ''; propertyMode.value = null
  try { propertyRows.value = useMock.value ? readStore().properties.filter(row => row.material === activeType.value?.materialType) : await request<MaterialProperty[]>(`${rootApi}/${enc(activeType.value.materialType)}/properties`) }
  catch (error) { propertyRows.value = []; errorMessage.value = error instanceof Error ? error.message : '物料屬性載入失敗。' }
}
function openPropertyEditor(mode: Exclude<Mode, null>): void {
  const row = activeProperty.value; if ((mode === 'edit' || mode === 'copy') && !row) return
  if (mode === 'edit' && row?.issueState !== 0) { propertyError.value = '只有未簽核屬性可以修改。'; return }
  sourceProperty.value = mode === 'edit' ? row : null; propertyMode.value = mode; propertyError.value = ''
  propertyForm.value = row && mode !== 'add' ? { propertyNo: row.propertyNo, defaultValue: row.defaultValue ?? '', propertySequence: String(row.propertySequence ?? 1), description: row.description ?? '' } : { propertyNo: '', defaultValue: '', propertySequence: '1', description: '' }
  propertyAudit.value = mode === 'edit' && row ? { creator: row.creator ?? '', createDate: row.createDate ?? '', reviser: row.reviser ?? '', reviseDate: row.reviseDate ?? '' } : { creator: props.currentUserNo || 'DEV001', createDate: now(), reviser: '', reviseDate: '' }
}
async function saveProperty(closeAfter = true): Promise<void> {
  if (!propertyMode.value || !activeType.value || busy.value) return
  const form = propertyForm.value; form.propertyNo = form.propertyNo.trim().toUpperCase(); form.defaultValue = form.defaultValue.trim(); form.description = form.description.trim(); propertyError.value = ''
  const sequence = Number(form.propertySequence)
  if (!form.propertyNo || !Number.isInteger(sequence) || sequence < 0 || sequence > 99) { propertyError.value = '屬性編號必填；順序須為 0 到 99 的整數。'; return }
  if (form.propertyNo.length > 20 || form.defaultValue.length > 255 || form.description.length > 255) { propertyError.value = '欄位長度超過資料庫限制。'; return }
  busy.value = true
  try {
    const mode = propertyMode.value; const material = activeType.value.materialType; const body = { propertyNo: form.propertyNo, defaultValue: form.defaultValue, propertySequence: sequence, description: form.description }
    if (useMock.value) { const store = readStore(); if (store.properties.some(row => row.material === material && row.propertyNo.toUpperCase() === form.propertyNo && !(mode === 'edit' && sourceProperty.value?.propertyNo === row.propertyNo))) throw new Error('此物料類別已存在相同屬性編號。'); const time = now(); const saved: MaterialProperty = mode === 'edit' && sourceProperty.value ? { ...sourceProperty.value, ...body, reviser: props.currentUserNo || 'DEV001', reviseDate: time, dataStamp: (sourceProperty.value.dataStamp ?? 0) + 1 } : { material, ...body, issueState: 0, creator: props.currentUserNo || 'DEV001', createDate: time, reviser: null, reviseDate: null, dataStamp: 0 }; if (mode === 'edit' && sourceProperty.value) store.properties = store.properties.map(row => row.material === material && row.propertyNo === sourceProperty.value?.propertyNo ? saved : row); else store.properties.push(saved); writeStore(store); propertyRows.value = store.properties.filter(row => row.material === material) }
    else if (mode === 'edit' && sourceProperty.value) await request(`${rootApi}/${enc(material)}/properties/${enc(sourceProperty.value.propertyNo)}?${stamp(sourceProperty.value.dataStamp)}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
    else await request(`${rootApi}/${enc(material)}/properties`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
    if (!useMock.value) propertyRows.value = await request<MaterialProperty[]>(`${rootApi}/${enc(material)}/properties`)
    propertyKey.value = form.propertyNo; propertyMode.value = closeAfter ? null : 'edit'; sourceProperty.value = propertyRows.value.find(row => row.propertyNo === form.propertyNo) ?? null
    if (sourceProperty.value) propertyAudit.value = { creator: sourceProperty.value.creator ?? '', createDate: sourceProperty.value.createDate ?? '', reviser: sourceProperty.value.reviser ?? '', reviseDate: sourceProperty.value.reviseDate ?? '' }
  } catch (error) { propertyError.value = error instanceof Error ? error.message : '物料屬性儲存失敗。' }
  finally { busy.value = false }
}
async function deleteProperty(): Promise<void> {
  const row = activeProperty.value; if (!row || !window.confirm('確定刪除此屬性？已核准資料將設為不使用。')) return
  busy.value = true
  try { if (useMock.value) { const store = readStore(); if (row.issueState === 2) store.properties = store.properties.map(item => item.material === row.material && item.propertyNo === row.propertyNo ? { ...item, issueState: -1, dataStamp: (item.dataStamp ?? 0) + 1 } : item); else store.properties = store.properties.filter(item => !(item.material === row.material && item.propertyNo === row.propertyNo)); writeStore(store); propertyRows.value = store.properties.filter(item => item.material === row.material) } else { await request(`${rootApi}/${enc(row.material)}/properties/${enc(row.propertyNo)}?issueState=${row.issueState}&${stamp(row.dataStamp)}`, { method: 'DELETE' }); propertyRows.value = await request<MaterialProperty[]>(`${rootApi}/${enc(row.material)}/properties`) } propertyKey.value = '' }
  catch (error) { propertyError.value = error instanceof Error ? error.message : '屬性刪除失敗。' }
  finally { busy.value = false }
}
async function approveProperty(): Promise<void> {
  const row = activeProperty.value; if (!row || row.issueState !== 0) return
  busy.value = true
  try { if (useMock.value) { const store = readStore(); store.properties = store.properties.map(item => item.material === row.material && item.propertyNo === row.propertyNo ? { ...item, issueState: 2, dataStamp: (item.dataStamp ?? 0) + 1, reviser: props.currentUserNo || 'DEV001', reviseDate: now() } : item); writeStore(store); propertyRows.value = store.properties.filter(item => item.material === row.material) } else { await request(`${rootApi}/${enc(row.material)}/properties/${enc(row.propertyNo)}/approve?${stamp(row.dataStamp)}`, { method: 'POST' }); propertyRows.value = await request<MaterialProperty[]>(`${rootApi}/${enc(row.material)}/properties`) } }
  catch (error) { propertyError.value = error instanceof Error ? error.message : '屬性核准失敗。' }
  finally { busy.value = false }
}

async function openGumRule(): Promise<void> {
  const row = activeType.value; if (!row || !gumMaterialTypes.includes(row.materialType)) return
  gumError.value = ''; gumOpen.value = true
  try {
    const rule = useMock.value ? readStore().gumRules.find(item => item.materialType === row.materialType && item.materialNo === '*') ?? null : await request<GumRule | null>(`${rootApi}/${enc(row.materialType)}/acclimation`)
    gumRule.value = rule
    gumForm.value = rule ? { accMin: String(rule.accMin), accMax: String(rule.accMax), refrozenFlag: rule.refrozenFlag === 1, frozenMin: rule.frozenMin === null ? '' : String(rule.frozenMin), reforzenTimes: String(rule.reforzenTimes), reacclimateFlag: rule.reacclimateFlag === 1, reacclimateMin: rule.reacclimateMin === null ? '' : String(rule.reacclimateMin), reacclimateMax: rule.reacclimateMax === null ? '' : String(rule.reacclimateMax) } : { accMin: '', accMax: '', refrozenFlag: false, frozenMin: '', reforzenTimes: '1', reacclimateFlag: false, reacclimateMin: '', reacclimateMax: '' }
    gumAudit.value = rule ? { creator: rule.creator ?? '', createDate: rule.createDate ?? '', reviser: rule.reviser ?? '', reviseDate: rule.reviseDate ?? '' } : { creator: props.currentUserNo || 'DEV001', createDate: now(), reviser: '', reviseDate: '' }
  } catch (error) { gumError.value = error instanceof Error ? error.message : '膠類時效載入失敗。' }
}
async function saveGumRule(closeAfter: boolean): Promise<void> {
  if (!activeType.value || busy.value) return
  const f = gumForm.value; const accMin = Number(f.accMin); const accMax = Number(f.accMax); const frozenMin = Number(f.frozenMin); const times = Number(f.reforzenTimes); const reacclimateMin = Number(f.reacclimateMin); const reacclimateMax = Number(f.reacclimateMax); gumError.value = ''
  if (activeType.value.issueState !== 0) { gumError.value = '已簽核或不使用的物料類別不可修改膠類時效。'; return }
  if (f.accMin.trim() === '' || f.accMax.trim() === '' || !Number.isFinite(accMin) || !Number.isFinite(accMax) || accMin < 0 || accMax < 0 || accMin > 999.99 || accMax > 999.99 || ((accMin !== 0 || accMax !== 0) && accMax <= accMin)) { gumError.value = 'ACC 最小/最大時效須為 0 到 999.99，最大值須大於最小值。'; return }
  if (f.refrozenFlag && (!f.frozenMin.trim() || !Number.isFinite(frozenMin) || frozenMin < 0 || frozenMin > 999.99 || !Number.isInteger(times) || times <= 0 || times > 999)) { gumError.value = '回冰啟用時，最小回冰時效須為 0 到 999.99，次數須為 1 到 999 的整數。'; return }
  if (f.reacclimateFlag && (!f.reacclimateMin.trim() || !f.reacclimateMax.trim() || !Number.isFinite(reacclimateMin) || !Number.isFinite(reacclimateMax) || reacclimateMin < 0 || reacclimateMax < 0 || reacclimateMin > 999.99 || reacclimateMax > 999.99 || ((reacclimateMin !== 0 || reacclimateMax !== 0) && reacclimateMin >= reacclimateMax))) { gumError.value = '再次回溫啟用時，最小/最大時效須為 0 到 999.99 且最大值大於最小值。'; return }
  const materialType = activeType.value.materialType; const body = { materialType, materialNo: '*', accMin, accMax, reforzenTimes: times, refrozenFlag: f.refrozenFlag ? 1 : 0, frozenMin: f.refrozenFlag ? frozenMin : null, reacclimateFlag: f.reacclimateFlag ? 1 : 0, reacclimateMin: f.reacclimateFlag ? reacclimateMin : null, reacclimateMax: f.reacclimateFlag ? reacclimateMax : null }
  busy.value = true
  try {
    if (useMock.value) { const store = readStore(); const time = now(); const saved: GumRule = { ...body, dataStamp: (gumRule.value?.dataStamp ?? 0) + 1, creator: gumRule.value?.creator ?? props.currentUserNo ?? 'DEV001', createDate: gumRule.value?.createDate ?? time, reviser: props.currentUserNo || 'DEV001', reviseDate: time }; store.gumRules = [...store.gumRules.filter(item => !(item.materialType === materialType && item.materialNo === '*')), saved]; writeStore(store); gumRule.value = saved; gumAudit.value = { creator: saved.creator ?? '', createDate: saved.createDate ?? '', reviser: saved.reviser ?? '', reviseDate: saved.reviseDate ?? '' } }
    else {
      const method = gumRule.value ? 'PUT' : 'POST'
      await request(`${rootApi}/${enc(materialType)}/acclimation${gumRule.value ? `?${stamp(gumRule.value.dataStamp)}` : ''}`, { method, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
      gumRule.value = await request<GumRule>(`${rootApi}/${enc(materialType)}/acclimation`)
      gumAudit.value = { creator: gumRule.value.creator ?? '', createDate: gumRule.value.createDate ?? '', reviser: gumRule.value.reviser ?? '', reviseDate: gumRule.value.reviseDate ?? '' }
    }
    if (closeAfter) gumOpen.value = false
  } catch (error) { gumError.value = error instanceof Error ? error.message : '膠類時效儲存失敗。' }
  finally { busy.value = false }
}
onMounted(() => { void loadTypes() })
</script>

<template>
  <section class="material-type-page">
    <header class="page-title"><h1>物料類別資料</h1></header>
    <div class="search-strip">
      <div class="issue-states"><label v-for="state in states" :key="state.value"><input v-model.number="issueState" type="radio" :value="state.value" @change="loadTypes"><span>{{ state.label }}</span></label></div>
      <label class="query-field"><span>物料類別</span><input v-model="query" class="kd-input" @keyup.enter="loadTypes"></label><button class="win-btn" :disabled="loading" @click="loadTypes">{{ loading ? '查詢中' : '查詢' }}</button>
    </div>
    <p v-if="errorMessage" class="message error">{{ errorMessage }}</p>
    <main class="grid-panel"><div class="grid-scroll"><table><thead><tr><th class="check"></th><th>物料類別</th><th>物料類別名稱</th><th>說明</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th></tr></thead><tbody>
      <tr v-for="row in visibleRows" :key="row.materialType" :class="{ active: activeKey === row.materialType, selected: selectedKeys.includes(row.materialType) }" @click="selectType(row, $event)" @dblclick="openTypeEditor('edit')"><td class="check"><input type="checkbox" :checked="selectedKeys.includes(row.materialType)" @click.stop="activeKey = row.materialType" @change="selectedKeys = ($event.target as HTMLInputElement).checked ? [...new Set([...selectedKeys, row.materialType])] : selectedKeys.filter(key => key !== row.materialType)"></td><td>{{ row.materialType }}</td><td>{{ row.materialTypeName }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ dateText(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ dateText(row.reviseDate) }}</td></tr>
      <tr v-if="!loading && !visibleRows.length"><td colspan="8" class="empty">沒有符合條件的物料類別</td></tr>
    </tbody></table></div><aside class="actions"><button class="win-btn primary" :disabled="busy" @click="openTypeEditor('add')">新增(A)</button><button class="win-btn" :disabled="busy || activeType?.issueState !== 0" @click="openTypeEditor('edit')">修改(E)</button><button class="win-btn" :disabled="busy || !activeType" @click="openTypeEditor('copy')">複製(C)</button><button class="win-btn danger" :disabled="busy || !activeType || issueState === 1" @click="deleteTypes">刪除(D)</button><button class="win-btn" :disabled="busy || issueState !== 0 || !activeType" @click="approveTypes">核准</button><button class="win-btn" :disabled="!activeType" @click="openProperties">Property</button><button v-if="multiTimeControl" class="win-btn" :disabled="!activeType || !gumMaterialTypes.includes(activeType.materialType)" @click="openGumRule">GumHour</button></aside></main>
    <footer class="status-line"><span>資料總筆數：{{ visibleRows.length }}　{{ activeType ? '目前類別：' + activeType.materialType : '' }}</span><span>使用者：{{ currentUserNo || '—' }}</span><button class="win-btn" @click="emit('close')">關閉</button></footer>

    <div v-if="editorMode" class="backdrop" @click.self="closeTypeEditor"><form class="editor type-editor" @submit.prevent="saveType(true)"><header class="dialog-title"><h2>{{ typeTitle }}</h2><button type="button" class="x-button" @click="closeTypeEditor">×</button></header><fieldset class="fields"><legend>物料類別資料</legend>
      <label><span>物料類別</span><input v-model="typeForm.materialType" class="kd-input" maxlength="50" :readonly="editorMode === 'edit'" @blur="typeForm.materialType = typeForm.materialType.trim().toUpperCase()"></label><label><span>類別名稱</span><input v-model="typeForm.materialTypeName" class="kd-input" maxlength="50"></label><label class="wide"><span>說明</span><textarea v-model="typeForm.description" class="kd-input" maxlength="255"></textarea></label>
    </fieldset><fieldset class="audit"><legend>建立 / 修改資訊</legend><label>建立人<input :value="typeAudit.creator" readonly></label><label>建立日期<input :value="dateText(typeAudit.createDate)" readonly></label><label>修改人<input :value="typeAudit.reviser" readonly></label><label>修改日期<input :value="dateText(typeAudit.reviseDate)" readonly></label></fieldset><p v-if="typeError" class="message error">{{ typeError }}</p><footer class="dialog-actions"><button class="win-btn primary" :disabled="busy">確定</button><button type="button" class="win-btn" :disabled="busy" @click="closeTypeEditor">關閉</button><button type="button" class="win-btn" :disabled="busy" @click="saveType(false)">套用</button></footer></form></div>

    <div v-if="propertiesOpen" class="backdrop property-backdrop" @click.self="propertiesOpen = false"><section class="editor property-window"><header class="dialog-title"><h2>Property：{{ activeType?.materialType }}</h2><button type="button" class="x-button" @click="propertiesOpen = false">×</button></header><div class="issue-states property-states"><label v-for="state in states" :key="state.value"><input v-model.number="propertyState" type="radio" :value="state.value"><span>{{ state.label }}</span></label></div><div class="property-body"><div class="grid-scroll"><table><thead><tr><th>屬性編號</th><th>預設值</th><th>順序</th><th>說明</th><th>建立人</th><th>建立日期</th></tr></thead><tbody><tr v-for="row in visibleProperties" :key="row.propertyNo" :class="{ active: propertyKey === row.propertyNo }" @click="propertyKey = row.propertyNo" @dblclick="openPropertyEditor('edit')"><td>{{ row.propertyNo }}</td><td>{{ row.defaultValue }}</td><td>{{ row.propertySequence }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ dateText(row.createDate) }}</td></tr><tr v-if="!visibleProperties.length"><td colspan="6" class="empty">沒有屬性資料</td></tr></tbody></table></div><aside class="actions"><button class="win-btn primary" @click="openPropertyEditor('add')">新增</button><button class="win-btn" :disabled="!activeProperty || activeProperty.issueState !== 0" @click="openPropertyEditor('edit')">修改</button><button class="win-btn" :disabled="!activeProperty" @click="openPropertyEditor('copy')">複製</button><button class="win-btn danger" :disabled="!activeProperty || propertyState === 1" @click="deleteProperty">刪除</button><button class="win-btn" :disabled="!activeProperty || propertyState !== 0" @click="approveProperty">核准</button></aside></div></section></div>
    <div v-if="propertyMode" class="backdrop nested-backdrop" @click.self="propertyMode = null"><form class="editor property-editor" @submit.prevent="saveProperty(true)"><header class="dialog-title"><h2>{{ propertyTitle }}</h2><button type="button" class="x-button" @click="propertyMode = null">×</button></header><fieldset class="fields"><legend>物料屬性：{{ activeType?.materialType }}</legend><label><span>屬性編號</span><input v-model="propertyForm.propertyNo" class="kd-input" maxlength="20" :readonly="propertyMode === 'edit'"></label><label><span>預設值</span><input v-model="propertyForm.defaultValue" class="kd-input" maxlength="255"></label><label><span>順序</span><input v-model="propertyForm.propertySequence" class="kd-input" type="number" min="0" max="99"></label><label class="wide"><span>說明</span><textarea v-model="propertyForm.description" class="kd-input" maxlength="255"></textarea></label></fieldset><fieldset class="audit"><legend>建立 / 修改資訊</legend><label>建立人<input :value="propertyAudit.creator" readonly></label><label>建立日期<input :value="dateText(propertyAudit.createDate)" readonly></label><label>修改人<input :value="propertyAudit.reviser" readonly></label><label>修改日期<input :value="dateText(propertyAudit.reviseDate)" readonly></label></fieldset><p v-if="propertyError" class="message error">{{ propertyError }}</p><footer class="dialog-actions"><button class="win-btn primary" :disabled="busy">確定</button><button type="button" class="win-btn" @click="propertyMode = null">關閉</button><button type="button" class="win-btn" @click="saveProperty(false)">套用</button></footer></form></div>

    <div v-if="gumOpen" class="backdrop" @click.self="gumOpen = false"><form class="editor gum-editor" @submit.prevent="saveGumRule(true)"><header class="dialog-title"><h2>GumHour：{{ activeType?.materialType }}</h2><button type="button" class="x-button" @click="gumOpen = false">×</button></header><fieldset class="fields gum-fields" :disabled="activeType?.issueState !== 0"><legend>MTL Gum Data（全類別 MaterialNo=*）</legend><label><span>ACCMin（小時）</span><input v-model="gumForm.accMin" class="kd-input" type="number" min="0" max="999.99" step="0.01"></label><label><span>ACCMax（小時）</span><input v-model="gumForm.accMax" class="kd-input" type="number" min="0" max="999.99" step="0.01"></label><label class="flag"><span>回冰</span><input v-model="gumForm.refrozenFlag" type="checkbox">啟用</label><label><span>FROZENMIN（小時）</span><input v-model="gumForm.frozenMin" class="kd-input" type="number" min="0" max="999.99" step="0.01" :disabled="activeType?.issueState !== 0 || !gumForm.refrozenFlag"></label><label><span>ReForzenTimes</span><input v-model="gumForm.reforzenTimes" class="kd-input" type="number" min="1" max="999" step="1" :disabled="activeType?.issueState !== 0 || !gumForm.refrozenFlag"></label><label class="flag"><span>再次回溫</span><input v-model="gumForm.reacclimateFlag" type="checkbox">啟用</label><label><span>REACCMIN（小時）</span><input v-model="gumForm.reacclimateMin" class="kd-input" type="number" min="0" max="999.99" step="0.01" :disabled="activeType?.issueState !== 0 || !gumForm.reacclimateFlag"></label><label><span>REACCMAX（小時）</span><input v-model="gumForm.reacclimateMax" class="kd-input" type="number" min="0" max="999.99" step="0.01" :disabled="activeType?.issueState !== 0 || !gumForm.reacclimateFlag"></label></fieldset><fieldset class="audit"><legend>建立 / 修改資訊</legend><label>建立人<input :value="gumAudit.creator" readonly></label><label>建立日期<input :value="dateText(gumAudit.createDate)" readonly></label><label>修改人<input :value="gumAudit.reviser" readonly></label><label>修改日期<input :value="dateText(gumAudit.reviseDate)" readonly></label></fieldset><p v-if="gumError" class="message error">{{ gumError }}</p><footer class="dialog-actions"><button class="win-btn primary" :disabled="busy || activeType?.issueState !== 0">確定</button><button type="button" class="win-btn" :disabled="busy" @click="gumOpen = false">關閉</button><button type="button" class="win-btn" :disabled="busy || activeType?.issueState !== 0" @click="saveGumRule(false)">套用</button></footer></form></div>
  </section>
</template>

<style scoped>
.material-type-page{height:100%;min-height:0;display:flex;flex-direction:column;background:#fff;color:#292929;font:14px "Microsoft JhengHei",Arial,sans-serif}.page-title{height:50px;flex:none;display:flex;align-items:center;padding:0 24px;border-bottom:1px solid #ddd8ce;background:#f7f5ef}.page-title h1{margin:0;font-size:20px;font-weight:500}.search-strip{min-height:42px;flex:none;display:flex;align-items:center;gap:12px;padding:5px 12px;border-bottom:1px solid #ccc}.issue-states{display:flex;align-items:center;gap:10px}.issue-states label{display:flex;align-items:center;gap:4px;white-space:nowrap}.issue-states input,.check input{accent-color:#e4a02f}.query-field{display:flex;align-items:center;gap:6px;margin-left:auto}.kd-input{box-sizing:border-box;min-width:0;height:27px;border:1px solid #999;border-radius:0;padding:3px 6px;background:#ffffc0;color:#222;font:inherit}.kd-input:read-only,.kd-input:disabled{background:#eee}.win-btn{min-height:29px;padding:3px 10px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#f1efeb);font:inherit;white-space:nowrap;cursor:pointer}.win-btn:hover:not(:disabled){border-color:#e39a37;background:#fff8e8}.win-btn:disabled{opacity:.5;cursor:default}.win-btn.primary{border-color:#e69520;background:#f7a52a;color:#fff;font-weight:600}.win-btn.danger{color:#8b3030}.message{margin:5px 12px;padding:6px 9px;border:1px solid #ddd;background:#fff9e7}.message.error{color:#8d2222;background:#fff3f1}.grid-panel{flex:1;min-height:0;display:grid;grid-template-columns:minmax(0,1fr) 92px;gap:8px;padding:10px 12px 5px}.grid-scroll{min-width:0;min-height:0;overflow:auto;border:1px solid #aaa;background:#fff}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}th,td{height:24px;padding:3px 6px;border:1px solid #c9c9c9;text-align:left}th{position:sticky;top:0;z-index:1;background:#e7e5df;font-weight:500}tbody tr{cursor:pointer}tbody tr:hover{background:#fff5d8}tbody tr.active{background:#ffefbf;outline:1px solid #d5a13d;outline-offset:-1px}tbody tr.selected:not(.active){background:#f8f3e6}.check{width:24px;text-align:center;padding:2px}.empty{text-align:center;color:#777;padding:18px}.actions{display:flex;flex-direction:column;gap:6px}.status-line{height:34px;flex:none;display:flex;align-items:center;justify-content:space-between;padding:0 12px;border-top:1px solid #ccc;color:#666;font-size:12px}.backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:10px;background:#0005}.property-backdrop{z-index:1001}.nested-backdrop{z-index:1003}.editor{box-sizing:border-box;display:flex;flex-direction:column;border:1px solid #999;background:#f9f8f4;box-shadow:0 8px 28px #0004}.type-editor{width:min(496px,96vw);min-height:min(276px,94vh);padding:0 10px 7px}.property-window{width:min(700px,96vw);height:min(490px,94vh);padding:0 10px 7px}.property-editor{width:min(496px,96vw);max-height:92vh;padding:0 10px 7px}.gum-editor{width:min(484px,96vw);max-height:95vh;overflow:auto;padding:0 10px 7px}.dialog-title{height:36px;flex:none;display:flex;align-items:center;justify-content:space-between;margin:0 -10px 5px;padding:0 10px;border-bottom:1px solid #ccc;background:#f1efe9}.dialog-title h2{margin:0;font-size:15px;font-weight:500}.x-button{border:0;background:transparent;color:#555;font-size:24px;line-height:1;cursor:pointer}.fields{display:flex;flex-direction:column;gap:8px;margin:6px 0;padding:7px;border:1px solid #aaa}.fields legend,.audit legend{padding:0 5px;color:#555}.fields label{display:grid;grid-template-columns:125px minmax(0,1fr);align-items:center;gap:7px}.fields label>span{text-align:right}.fields .wide{align-items:start}.fields textarea{height:52px;resize:vertical}.audit{display:grid;grid-template-columns:1fr 1fr;gap:5px 10px;margin:0;padding:5px 7px;border:1px solid #aaa}.audit label{display:grid;grid-template-columns:58px minmax(0,1fr);align-items:center;gap:5px;font-size:11px;white-space:nowrap}.audit input{box-sizing:border-box;width:100%;min-width:0;height:22px;border:1px solid #aaa;background:#eee;padding:2px 5px;font:inherit}.dialog-actions{display:flex;justify-content:flex-end;gap:7px;padding:7px 0 1px}.property-states{padding:5px 3px}.property-body{display:grid;grid-template-columns:minmax(0,1fr) 80px;gap:8px;flex:1;min-height:0}.gum-fields{display:grid;grid-template-columns:1fr 1fr;gap:8px 10px}.gum-fields .flag{grid-template-columns:125px auto 1fr}.gum-fields .flag input{justify-self:start;accent-color:#e4a02f}@media(max-width:820px){.search-strip{flex-wrap:wrap}.query-field{margin-left:0}.grid-panel{grid-template-columns:minmax(0,1fr) 76px}.property-body{grid-template-columns:minmax(0,1fr) 68px}.property-window{height:90vh}.gum-fields{grid-template-columns:1fr}.type-editor{min-height:0}}
</style>
