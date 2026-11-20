<script setup lang="ts">
import { computed, ref } from 'vue'

interface AreaRow { areaNo: string; areaName: string; departmentNo: string | null; issueState?: number; dataStamp?: number | null }
interface LayoutNode {
  containAreaNo: string; objectNo: string; objectType: number; objectText: string; objectX: number; objectY: number
  objectLength: number; objectWidth: number; objectColor: number; objectAreaType?: number; objectSequence?: number
  borderColor?: number; borderStyle?: number; textColor?: number
}
interface AreaShift {
  areaNo: string; departmentNo: string; shiftNo: string; managerNo: string; exceptionTime: number; description: string | null
  creator: string | null; createDate: string | null; reviser: string | null; reviseDate: string | null; dataStamp: number | null
}
interface UserOption { userNo: string; userName: string; departmentNo?: string }
interface Operator { userNo: string; userName?: string; description: string | null; creator?: string | null; createDate?: string | null }
interface LookupOption { value: string; label: string }
interface OperatorPayload { assigned: Operator[]; available: UserOption[] }
interface WipAreaOperatorState {
  areaNo: string; departmentNo: string; shiftNo: string; userNo: string; loginComment: string; loginTime: string; exceptionTime: number
}
interface WipAreaOperatorLog extends WipAreaOperatorState { logoutComment: string; logoutTime: string }
type ShiftFormMode = 'add' | 'edit' | 'copy'
type InfoMode = 'state' | 'history' | null

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; enterpriseNo?: string; developmentPreview?: boolean; workAreaMode?: boolean }>(), { developmentPreview: false, workAreaMode: false, enterpriseNo: '' })
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mockMode = computed(() => import.meta.env.DEV && props.developmentPreview)
const workAreaActionsVisible = computed(() => !props.workAreaMode || mockMode.value || props.enterpriseNo.trim().toUpperCase() === 'IMESTECH')
const areasKey = 'ymes-dev-mock:smd-area-show:areas:v1'
const layoutsKey = 'ymes-dev-mock:smd-area-show:layouts:v1'
const shiftsKey = 'ymes-dev-mock:smd-area-show:shifts:v1'
const operatorsKey = 'ymes-dev-mock:smd-area-show:operators:v1'
const workAreaStateKey = 'ymes-dev-mock:wip-area-operator-state:v1'
const workAreaLogKey = 'ymes-dev-mock:wip-area-operator-log:v1'
const zoomLevels = [100, 150, 75, 50]
const seedAreas: AreaRow[] = [
  { areaNo: 'DEV-BASE', areaName: '開發用基底區域', departmentNo: 'DEV-DEPT', issueState: 2, dataStamp: 2 },
  { areaNo: 'DEV-BASE-02', areaName: '開發用包裝廠區', departmentNo: 'DEV-DEPT', issueState: 2, dataStamp: 1 },
]
const seedNodes: LayoutNode[] = [
  { containAreaNo: 'DEV-BASE', objectNo: 'DEV-OP-01', objectType: 0, objectText: '作業區 A', objectX: 35, objectY: 35, objectWidth: 220, objectLength: 110, objectColor: 4294951175 },
  { containAreaNo: 'DEV-BASE', objectNo: 'DEV-LINE-01', objectType: 0, objectText: '包裝線', objectX: 315, objectY: 35, objectWidth: 240, objectLength: 110, objectColor: 4294951175, objectAreaType: 1 },
  { containAreaNo: 'DEV-BASE', objectNo: 'DEV-INV-01', objectType: 1, objectText: '原料庫', objectX: 70, objectY: 205, objectWidth: 115, objectLength: 70, objectColor: 4293914607 },
  { containAreaNo: 'DEV-OP-01', objectNo: 'EQ-DEV-01', objectType: 2, objectText: '設備 01', objectX: 95, objectY: 65, objectWidth: 90, objectLength: 55, objectColor: 4291611852 },
  { containAreaNo: 'DEV-LINE-01', objectNo: 'TYPE-DEV-01', objectType: 3, objectText: '設備類別', objectX: 350, objectY: 65, objectWidth: 115, objectLength: 52, objectColor: 4293914607 },
  { containAreaNo: 'DEV-BASE', objectNo: 'GROUP-DEV-01', objectType: 4, objectText: '設備群組', objectX: 630, objectY: 205, objectWidth: 145, objectLength: 65, objectColor: 4293914607 },
]
const seedShifts: AreaShift[] = [
  { areaNo: 'DEV-OP-01', departmentNo: 'DEV-DEPT', shiftNo: 'DAY', managerNo: 'DEV001', exceptionTime: 10, description: '日班', creator: 'DEV001', createDate: '2026-11-10T08:00:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
  { areaNo: 'DEV-OP-01', departmentNo: 'DEV-DEPT', shiftNo: 'NIGHT', managerNo: 'DEV002', exceptionTime: 10, description: '夜班', creator: 'DEV001', createDate: '2026-11-10T08:00:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
]
const seedUsers: UserOption[] = [
  { userNo: 'DEV001', userName: '開發人員甲', departmentNo: 'DEV-DEPT' },
  { userNo: 'DEV002', userName: '開發人員乙', departmentNo: 'DEV-DEPT' },
  { userNo: 'DEV003', userName: '開發人員丙', departmentNo: 'DEV-DEPT' },
]
const seedOperators: Record<string, Operator[]> = {
  'DEV-OP-01|DEV-DEPT|DAY': [{ userNo: 'DEV001', description: null, creator: 'DEV001' }],
  'DEV-OP-01|DEV-DEPT|NIGHT': [{ userNo: 'DEV002', description: null, creator: 'DEV001' }],
}
const seedWorkAreaStates: WipAreaOperatorState[] = []
const seedWorkAreaLogs: WipAreaOperatorLog[] = [
  { areaNo: 'DEV-OP-01', departmentNo: 'DEV-DEPT', shiftNo: 'DAY', userNo: 'DEV003', loginComment: '開發用歷程', loginTime: '2026-11-19T08:00:00+08:00', logoutComment: '交班', logoutTime: '2026-11-19T17:00:00+08:00', exceptionTime: 10 },
]

const areas = ref<AreaRow[]>([])
const activeArea = ref<AreaRow | null>(null)
const shiftArea = ref<AreaRow | null>(null)
const nodes = ref<LayoutNode[]>([])
const zoom = ref(100)
const gridVisible = ref(true)
const gridSettingsOpen = ref(false)
const gridSettingsError = ref('')
const gridSettings = ref({ draw: true, snap: true, color: '#e9e7e2', style: 'Lines' as 'Pixels' | 'Lines' | 'DottedLines', sizeX: 20, sizeY: 20 })
const gridSettingsDraft = ref({ ...gridSettings.value })
const canvasGridVisible = computed(() => props.workAreaMode ? gridSettings.value.draw : gridVisible.value)
const canvasGridStyle = computed((): Record<string, string> => {
  if (!props.workAreaMode || !gridSettings.value.draw) return {}
  const { color, style, sizeX, sizeY } = gridSettings.value
  const backgroundImage = style === 'Pixels'
    ? `radial-gradient(${color} 1px, transparent 1px)`
    : style === 'DottedLines'
      ? `repeating-linear-gradient(0deg, ${color} 0 1px, transparent 1px 3px), repeating-linear-gradient(90deg, ${color} 0 1px, transparent 1px 3px)`
      : `linear-gradient(${color} 1px, transparent 1px), linear-gradient(90deg, ${color} 1px, transparent 1px)`
  const backgroundSize = style === 'Pixels'
    ? `${sizeX}px ${sizeY}px`
    : style === 'DottedLines'
      ? `1px ${sizeY}px, ${sizeX}px 1px`
      : `${sizeX}px ${sizeY}px`
  return { backgroundImage, backgroundSize }
})
const loading = ref(false)
const errorMessage = ref('')
const areaPickerOpen = ref(false)
const areaSearch = ref('')
const contextMenu = ref<{ x: number; y: number; node: LayoutNode } | null>(null)
const infoMode = ref<InfoMode>(null)
const infoNode = ref<LayoutNode | null>(null)
const infoRows = ref<Record<string, string>[]>([])
const infoSourceRows = ref<Record<string, string>[]>([])
const infoBusy = ref(false)
const infoDateTab = ref<'filter' | 'dateFilter'>('filter')
const infoFilter = ref({ areaNo: '', departmentNo: '', shiftEnabled: false, shiftNo: '', userEnabled: false, userNo: '', startEnabled: false, startDate: '', endEnabled: false, endDate: '', loginStartEnabled: false, loginStartDate: '', loginEndEnabled: false, loginEndDate: '', logoutStartEnabled: false, logoutStartDate: '', logoutEndEnabled: false, logoutEndDate: '', exceptionEnabled: false, exceptionTime: 0 })
const infoLookupKind = ref<'shift' | 'user' | null>(null)
const infoLookupRows = ref<LookupOption[]>([])
const infoLookupBusy = ref(false)
const infoLookupSearch = ref('')
const workAreaDialogOpen = ref(false)
const workAreaBusy = ref(false)
const workAreaError = ref('')
const workAreaVerified = ref(false)
const workAreaShiftOptions = ref<string[]>([])
const workAreaForm = ref({ areaNo: '', areaName: '', departmentNo: '', shiftNo: '', userNo: '', comment: '' })
const workAreaState = ref<WipAreaOperatorState[]>([])

const shiftOpen = ref(false)
const shiftBusy = ref(false)
const shiftError = ref('')
const shiftRows = ref<AreaShift[]>([])
const selectedShiftKey = ref('')
const checkedShiftKeys = ref<string[]>([])
const operatorRows = ref<Operator[]>([])
const availableUsers = ref<UserOption[]>([])
const checkedOperatorNos = ref<string[]>([])
const checkedAvailableNos = ref<string[]>([])
const shiftNumberOptions = ref<LookupOption[]>([])
const managerOptions = ref<UserOption[]>([])
const shiftFormMode = ref<ShiftFormMode | null>(null)
const shiftForm = ref({ shiftNo: '', managerNo: '', exceptionTime: 10, description: '' })
const sourceCopyShift = ref('')
const shiftOriginal = ref<AreaShift | null>(null)
const shiftAudit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const shiftFormError = ref('')
const areaDepartmentNo = computed(() => shiftArea.value?.departmentNo ?? '')
const infoColumns = computed(() => props.workAreaMode
  ? infoMode.value === 'history'
    ? ['AreaNo', 'DepartmentNo', 'ShiftNo', 'UserNo', 'LoginTime', 'LogoutTime', 'ExceptionTime', 'LoginComment', 'LogoutComment']
    : ['AreaNo', 'DepartmentNo', 'ShiftNo', 'UserNo', 'LoginComment', 'LoginTime']
  : Object.keys(infoRows.value[0] ?? {}))
const workAreaCurrentUser = computed(() => workAreaState.value.find(row => row.userNo.toUpperCase() === workAreaForm.value.userNo.trim().toUpperCase()) ?? null)
const workAreaCanLogout = computed(() => workAreaCurrentUser.value?.areaNo === workAreaForm.value.areaNo)
const visibleAreas = computed(() => areas.value.filter(row => !areaSearch.value.trim() ||
  row.areaNo.toLocaleLowerCase().includes(areaSearch.value.trim().toLocaleLowerCase()) ||
  row.areaName.toLocaleLowerCase().includes(areaSearch.value.trim().toLocaleLowerCase())))
const activeShift = computed(() => shiftRows.value.find(row => shiftKey(row) === selectedShiftKey.value) ?? null)
const assignedUsers = computed(() => operatorRows.value.map(operator => ({
  ...operator, userName: availableUsers.value.find(user => user.userNo === operator.userNo)?.userName ?? operator.userNo,
})))
const remainingUsers = computed(() => availableUsers.value.filter(user => !operatorRows.value.some(operator => operator.userNo === user.userNo)))
const shiftFormTitle = computed(() => shiftFormMode.value === 'add' ? '班別設定 (新增)' : shiftFormMode.value === 'copy' ? '班別設定 (複製)' : '班別設定 (修改)')

function readStorage<T>(key: string, fallback: T): T {
  try { const value = window.localStorage.getItem(key); return value ? JSON.parse(value) as T : structuredClone(fallback) }
  catch { return structuredClone(fallback) }
}
function writeStorage<T>(key: string, value: T): void { window.localStorage.setItem(key, JSON.stringify(value)) }
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function requestJson<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init })
  if (response.status === 401) { window.location.assign(appBase + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    if (response.status === 404) throw new Error(props.workAreaMode && path.includes('/api/wip/areas/') ? 'WP307 區域作業 API 尚未建立；請依 todolist_homepc.md 完成 WIP WP307。' : '區域配置或班別 API 尚未建立；請依 todolist_homepc.md 完成 SMD SD108。')
    throw new Error(problem?.detail || problem?.message || problem?.title || ('API 回應錯誤 (' + response.status + ')。'))
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function areaPath(areaNo: string): string { return 'api/smd/areas/' + encodeURIComponent(areaNo) }
function shiftPath(areaNo: string): string { return areaPath(areaNo) + '/shifts' }
function shiftKey(row: Pick<AreaShift, 'areaNo' | 'departmentNo' | 'shiftNo'>): string { return row.areaNo + '|' + row.departmentNo + '|' + row.shiftNo }
function dataStampQuery(value: number | null): string { return value === null ? 'dataStamp=0&dataStampIsNull=true' : 'dataStamp=' + encodeURIComponent(String(value)) }
function formatDate(value: string | null | undefined): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat('zh-TW', { dateStyle: 'short', timeStyle: 'medium', hour12: false }).format(date)
}
function colorCss(value: number): string { return '#' + ((Number(value) >>> 0) & 0xffffff).toString(16).padStart(6, '0') }
function nodeShape(node: LayoutNode): string {
  if (node.objectType === 2) return 'ellipse'
  if (node.objectType === 1) return 'hex'
  if (node.objectType === 3 || node.objectType === 4) return 'round'
  return 'rect'
}
function nodeClass(node: LayoutNode): string { return 'node-' + node.objectType }
function areaNode(node: LayoutNode): boolean { return node.objectType === 0 }
function openAreaPicker(): void { areaPickerOpen.value = true; areaSearch.value = '' }
function openGridProperties(): void { gridSettingsDraft.value = { ...gridSettings.value }; gridSettingsError.value = ''; gridSettingsOpen.value = true }
function saveGridProperties(): void {
  const { sizeX, sizeY } = gridSettingsDraft.value
  if (!Number.isInteger(sizeX) || !Number.isInteger(sizeY) || sizeX < 1 || sizeY < 1 || sizeX > 500 || sizeY > 500) {
    gridSettingsError.value = 'Grid Size Width/Height 必須為 1 至 500 的整數。'
    return
  }
  gridSettingsError.value = ''
  gridSettings.value = { ...gridSettingsDraft.value }
  gridSettingsOpen.value = false
}
async function loadBaseAreas(): Promise<void> {
  loading.value = true; errorMessage.value = ''
  try {
    areas.value = mockMode.value
      ? readStorage(areasKey, seedAreas)
      : await requestJson<AreaRow[]>('api/smd/areas?issueState=2&areaClass=1')
    areaPickerOpen.value = true
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : 'Base Area 載入失敗。' }
  finally { loading.value = false }
}
async function chooseArea(row: AreaRow): Promise<void> {
  activeArea.value = row
  areaPickerOpen.value = false
  contextMenu.value = null
  loading.value = true; errorMessage.value = ''
  try {
    if (mockMode.value) nodes.value = readStorage<Record<string, LayoutNode[]>>(layoutsKey, { 'DEV-BASE': seedNodes })[row.areaNo] ?? []
    else {
      const result = await requestJson<{ relations: LayoutNode[] }>(areaPath(row.areaNo) + '/layout')
      nodes.value = result.relations
    }
  } catch (error) { nodes.value = []; errorMessage.value = error instanceof Error ? error.message : '區域配置載入失敗。' }
  finally { loading.value = false }
}
function showContext(event: MouseEvent, node: LayoutNode): void {
  if (!areaNode(node) || !workAreaActionsVisible.value) return
  contextMenu.value = { x: event.clientX, y: event.clientY, node }
}
function openShiftFor(node: LayoutNode): void {
  contextMenu.value = null
  if (!activeArea.value) return
  void openShift(node.objectNo, node.objectText)
}
async function openShift(areaNo: string, areaLabel: string): Promise<void> {
  shiftOpen.value = true; shiftBusy.value = true; shiftError.value = ''
  try {
    if (mockMode.value) {
      shiftArea.value = { areaNo, areaName: areaLabel, departmentNo: activeArea.value?.departmentNo || 'DEV-DEPT' }
      shiftRows.value = readStorage<AreaShift[]>(shiftsKey, seedShifts).filter(row => row.areaNo === areaNo && row.departmentNo === areaDepartmentNo.value)
      const allUsers = readStorage<UserOption[]>('ymes-dev-mock:smd-area-show:users:v1', seedUsers)
      availableUsers.value = allUsers.filter(user => !user.departmentNo || user.departmentNo === areaDepartmentNo.value)
      shiftNumberOptions.value = [{ value: 'DAY', label: 'DAY - 日班' }, { value: 'NIGHT', label: 'NIGHT - 夜班' }, { value: 'SWING', label: 'SWING - 中班' }]
      managerOptions.value = availableUsers.value
    } else {
      const context = await requestJson<{ areaNo: string; areaName: string; departmentNo: string | null }>(areaPath(areaNo) + '/shift-context')
      shiftArea.value = { ...context, departmentNo: context.departmentNo ?? '' }
      shiftRows.value = await requestJson<AreaShift[]>(shiftPath(areaNo) + '?departmentNo=' + encodeURIComponent(areaDepartmentNo.value))
      const options = await requestJson<{ shifts: LookupOption[]; managers: UserOption[]; users: UserOption[] }>(shiftPath(areaNo) + '/options?departmentNo=' + encodeURIComponent(areaDepartmentNo.value))
      shiftNumberOptions.value = options.shifts; managerOptions.value = options.managers; availableUsers.value = options.users
    }
    selectedShiftKey.value = shiftRows.value[0] ? shiftKey(shiftRows.value[0]) : ''
    checkedShiftKeys.value = []
    if (activeShift.value) await loadOperators(activeShift.value)
    else { operatorRows.value = []; checkedOperatorNos.value = []; checkedAvailableNos.value = [] }
  } catch (error) { shiftError.value = error instanceof Error ? error.message : '班別資料載入失敗。' }
  finally { shiftBusy.value = false }
}
async function loadOperators(row: AreaShift): Promise<void> {
  if (mockMode.value) {
    const store = readStorage<Record<string, Operator[]>>(operatorsKey, seedOperators)
    operatorRows.value = structuredClone(store[shiftKey(row)] ?? [])
  } else {
    const result = await requestJson<OperatorPayload>(shiftPath(row.areaNo) + '/' + encodeURIComponent(row.departmentNo) + '/' + encodeURIComponent(row.shiftNo) + '/operators')
    operatorRows.value = result.assigned
    availableUsers.value = result.available
  }
  checkedOperatorNos.value = []; checkedAvailableNos.value = []
}
async function activateShift(row: AreaShift): Promise<void> {
  selectedShiftKey.value = shiftKey(row)
  shiftBusy.value = true; shiftError.value = ''
  try { await loadOperators(row) }
  catch (error) { shiftError.value = error instanceof Error ? error.message : '班別成員載入失敗。' }
  finally { shiftBusy.value = false }
}
function toggleShift(row: AreaShift, checked: boolean): void {
  const key = shiftKey(row)
  checkedShiftKeys.value = checked ? [...new Set([...checkedShiftKeys.value, key])] : checkedShiftKeys.value.filter(item => item !== key)
  void activateShift(row)
}
function openShiftForm(mode: ShiftFormMode): void {
  const row = activeShift.value
  if ((mode !== 'add') && !row) return
  shiftFormMode.value = mode
  shiftFormError.value = ''
  shiftOriginal.value = mode === 'edit' ? row : null
  sourceCopyShift.value = mode === 'copy' ? row?.shiftNo ?? '' : ''
  shiftForm.value = row && mode !== 'add'
    ? { shiftNo: mode === 'edit' ? row.shiftNo : '', managerNo: row.managerNo, exceptionTime: row.exceptionTime, description: row.description ?? '' }
    : { shiftNo: '', managerNo: '', exceptionTime: 10, description: '' }
  shiftAudit.value = mode === 'edit' && row
    ? { creator: row.creator ?? '', createDate: row.createDate ?? '', reviser: row.reviser ?? '', reviseDate: row.reviseDate ?? '' }
    : { creator: props.currentUserNo || 'DEV001', createDate: new Date().toISOString(), reviser: '', reviseDate: '' }
}
function closeShiftForm(): void { shiftFormMode.value = null; shiftOriginal.value = null; shiftFormError.value = '' }
async function saveShift(closeAfter: boolean): Promise<void> {
  if (!shiftFormMode.value || !shiftArea.value) return
  shiftFormError.value = ''
  shiftForm.value.shiftNo = shiftForm.value.shiftNo.trim().toUpperCase()
  shiftForm.value.managerNo = shiftForm.value.managerNo.trim().toUpperCase()
  shiftForm.value.description = shiftForm.value.description.trim()
  if (!shiftForm.value.shiftNo) { shiftFormError.value = '請選擇班別編號。'; return }
  if (!shiftForm.value.managerNo) { shiftFormError.value = '請選擇管理者。'; return }
  if (!Number.isInteger(shiftForm.value.exceptionTime) || shiftForm.value.exceptionTime < 0 || shiftForm.value.exceptionTime > 9999) { shiftFormError.value = '異常時間必須為 0 至 9999 的整數。'; return }
  shiftBusy.value = true
  try {
    const mode = shiftFormMode.value
    const payload = { departmentNo: areaDepartmentNo.value, shiftNo: shiftForm.value.shiftNo, managerNo: shiftForm.value.managerNo, exceptionTime: shiftForm.value.exceptionTime, description: shiftForm.value.description }
    if (mockMode.value) {
      const all = readStorage<AreaShift[]>(shiftsKey, seedShifts)
      const now = new Date().toISOString()
      if (mode !== 'edit' && all.some(row => row.areaNo === shiftArea.value!.areaNo && row.departmentNo === areaDepartmentNo.value && row.shiftNo === payload.shiftNo)) throw new Error('此區域已存在相同班別。')
      const saved: AreaShift = mode === 'edit' && shiftOriginal.value
        ? { ...shiftOriginal.value, managerNo: payload.managerNo, exceptionTime: payload.exceptionTime, description: payload.description, reviser: props.currentUserNo || 'DEV001', reviseDate: now, dataStamp: (shiftOriginal.value.dataStamp ?? 0) + 1 }
        : { ...payload, areaNo: shiftArea.value.areaNo, creator: props.currentUserNo || 'DEV001', createDate: now, reviser: null, reviseDate: null, dataStamp: 0 }
      const next = mode === 'edit' && shiftOriginal.value
        ? all.map(row => shiftKey(row) === shiftKey(shiftOriginal.value!) ? saved : row)
        : [...all, saved]
      writeStorage(shiftsKey, next)
      if (mode === 'copy') {
        const map = readStorage<Record<string, Operator[]>>(operatorsKey, seedOperators)
        map[shiftKey(saved)] = structuredClone(map[activeShift.value ? shiftKey(activeShift.value) : sourceCopyShift.value] ?? operatorRows.value)
        writeStorage(operatorsKey, map)
      }
    } else if (mode === 'edit' && shiftOriginal.value) {
      await requestJson(shiftPath(shiftArea.value.areaNo) + '/' + encodeURIComponent(areaDepartmentNo.value) + '/' + encodeURIComponent(shiftOriginal.value.shiftNo) + '?' + dataStampQuery(shiftOriginal.value.dataStamp), {
        method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ managerNo: payload.managerNo, exceptionTime: payload.exceptionTime, description: payload.description }),
      })
    } else if (mode === 'copy') {
      await requestJson(shiftPath(shiftArea.value.areaNo) + '/' + encodeURIComponent(areaDepartmentNo.value) + '/' + encodeURIComponent(sourceCopyShift.value) + '/copy', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
    } else {
      await requestJson(shiftPath(shiftArea.value.areaNo), { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
    }
    const savedNo = payload.shiftNo
    await openShift(shiftArea.value.areaNo, shiftArea.value.areaName)
    const savedRow = shiftRows.value.find(row => row.shiftNo === savedNo)
    if (savedRow) await activateShift(savedRow)
    if (closeAfter) closeShiftForm()
    else { shiftFormMode.value = 'edit'; shiftOriginal.value = savedRow ?? null }
  } catch (error) { shiftFormError.value = error instanceof Error ? error.message : '班別資料儲存失敗。' }
  finally { shiftBusy.value = false }
}
async function deleteShifts(): Promise<void> {
  if (!activeShift.value || !shiftArea.value) return
  if (!checkedShiftKeys.value.includes(selectedShiftKey.value)) checkedShiftKeys.value = [selectedShiftKey.value]
  const targets = shiftRows.value.filter(row => checkedShiftKeys.value.includes(shiftKey(row)))
  if (!targets.length || !window.confirm('確定刪除選取的 ' + targets.length + ' 筆班別及成員設定？')) return
  shiftBusy.value = true; shiftError.value = ''
  try {
    if (mockMode.value) {
      const keys = new Set(targets.map(shiftKey))
      writeStorage(shiftsKey, readStorage<AreaShift[]>(shiftsKey, seedShifts).filter(row => !keys.has(shiftKey(row))))
      const operators = readStorage<Record<string, Operator[]>>(operatorsKey, seedOperators)
      for (const key of keys) delete operators[key]
      writeStorage(operatorsKey, operators)
    } else {
      for (const row of targets) await requestJson(shiftPath(row.areaNo) + '/' + encodeURIComponent(row.departmentNo) + '/' + encodeURIComponent(row.shiftNo) + '?' + dataStampQuery(row.dataStamp), { method: 'DELETE' })
    }
    await openShift(shiftArea.value.areaNo, shiftArea.value.areaName)
  } catch (error) { shiftError.value = error instanceof Error ? error.message : '刪除失敗，已停止後續處理。' }
  finally { shiftBusy.value = false }
}
function moveUsers(direction: 'add' | 'remove'): void {
  if (!activeShift.value) return
  if (direction === 'add') {
    const selected = availableUsers.value.filter(user => checkedAvailableNos.value.includes(user.userNo))
    for (const user of selected) if (!operatorRows.value.some(row => row.userNo === user.userNo)) operatorRows.value.push({ userNo: user.userNo, description: null, creator: props.currentUserNo ?? '' })
    checkedAvailableNos.value = []
  } else {
    operatorRows.value = operatorRows.value.filter(row => !checkedOperatorNos.value.includes(row.userNo))
    checkedOperatorNos.value = []
  }
}
async function saveOperators(): Promise<void> {
  if (!activeShift.value || !shiftArea.value) return
  shiftBusy.value = true; shiftError.value = ''
  try {
    const row = activeShift.value
    const payload = { operators: operatorRows.value.map(item => ({ userNo: item.userNo, description: item.description })) }
    if (mockMode.value) {
      const store = readStorage<Record<string, Operator[]>>(operatorsKey, seedOperators)
      store[shiftKey(row)] = structuredClone(operatorRows.value)
      writeStorage(operatorsKey, store)
    } else {
      await requestJson(shiftPath(row.areaNo) + '/' + encodeURIComponent(row.departmentNo) + '/' + encodeURIComponent(row.shiftNo) + '/operators?' + dataStampQuery(row.dataStamp), {
        method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload),
      })
      shiftRows.value = await requestJson<AreaShift[]>(shiftPath(row.areaNo) + '?departmentNo=' + encodeURIComponent(row.departmentNo))
      const refreshed = shiftRows.value.find(item => shiftKey(item) === shiftKey(row))
      if (refreshed) {
        selectedShiftKey.value = shiftKey(refreshed)
        await loadOperators(refreshed)
      }
    }
    if (mockMode.value) {
      shiftRows.value = shiftRows.value.map(item => shiftKey(item) === shiftKey(row) ? { ...item, dataStamp: (item.dataStamp ?? 0) + 1, reviser: props.currentUserNo || 'DEV001', reviseDate: new Date().toISOString() } : item)
      writeStorage(shiftsKey, readStorage<AreaShift[]>(shiftsKey, seedShifts).map(item => shiftKey(item) === shiftKey(row) ? { ...item, dataStamp: (item.dataStamp ?? 0) + 1, reviser: props.currentUserNo || 'DEV001', reviseDate: new Date().toISOString() } : item))
    }
  } catch (error) { shiftError.value = error instanceof Error ? error.message : '班別人員儲存失敗。' }
  finally { shiftBusy.value = false }
}
async function openInfo(mode: Exclude<InfoMode, null>, node: LayoutNode): Promise<void> {
  contextMenu.value = null; infoMode.value = mode; infoNode.value = node; infoRows.value = []
  infoSourceRows.value = []
  infoDateTab.value = 'filter'
  infoFilter.value = { areaNo: node.objectNo, departmentNo: activeArea.value?.departmentNo ?? '', shiftEnabled: false, shiftNo: '', userEnabled: false, userNo: '', startEnabled: false, startDate: '', endEnabled: false, endDate: '', loginStartEnabled: false, loginStartDate: '', loginEndEnabled: false, loginEndDate: '', logoutStartEnabled: false, logoutStartDate: '', logoutEndEnabled: false, logoutEndDate: '', exceptionEnabled: false, exceptionTime: 0 }
  if (mockMode.value) {
    if (props.workAreaMode) {
      if (mode === 'state') {
        workAreaState.value = readStorage<WipAreaOperatorState[]>(workAreaStateKey, seedWorkAreaStates)
        infoSourceRows.value = workAreaState.value.filter(row => row.areaNo === node.objectNo).map(row => ({
          AreaNo: row.areaNo, DepartmentNo: row.departmentNo, ShiftNo: row.shiftNo, UserNo: row.userNo,
          LoginComment: row.loginComment, LoginTime: formatDate(row.loginTime),
        }))
      } else {
        infoSourceRows.value = readStorage<WipAreaOperatorLog[]>(workAreaLogKey, seedWorkAreaLogs).filter(row => row.areaNo === node.objectNo).map(row => ({
          AreaNo: row.areaNo, DepartmentNo: row.departmentNo, ShiftNo: row.shiftNo, UserNo: row.userNo,
          LoginTime: formatDate(row.loginTime), LogoutTime: formatDate(row.logoutTime),
          ExceptionTime: String(row.exceptionTime), LoginComment: row.loginComment, LogoutComment: row.logoutComment,
        }))
      }
      return
    }
    infoRows.value = mode === 'state'
      ? [{ 使用者: 'DEV001', 姓名: '開發人員甲', 登入時間: '2026/11/16 08:00:00', 狀態: '作業中' }]
      : [{ 使用者: 'DEV001', 姓名: '開發人員甲', 登入時間: '2026/11/16 08:00:00', 登出時間: '2026/11/16 17:00:00' }, { 使用者: 'DEV002', 姓名: '開發人員乙', 登入時間: '2026/11/15 20:00:00', 登出時間: '2026/11/16 08:00:00' }]
    return
  }
  try {
    if (props.workAreaMode) {
      const context = await requestJson<{ areaNo: string; departmentNo: string | null }>(areaPath(node.objectNo) + '/shift-context')
      infoFilter.value.departmentNo = context.departmentNo ?? ''
    }
    else {
      const suffix = '/operator-' + (mode === 'state' ? 'state' : 'log')
      infoRows.value = await requestJson<Record<string, string>[]>('api/wip/areas/' + encodeURIComponent(node.objectNo) + suffix)
    }
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '作業人員資訊載入失敗。' }
}

function filterWorkInfoRows(): void {
  const filter = infoFilter.value
  const shiftNo = filter.shiftNo.trim().toUpperCase()
  const userNo = filter.userNo.trim().toUpperCase()
  infoRows.value = infoSourceRows.value.filter(row => {
    if (filter.shiftEnabled && shiftNo && (row.ShiftNo ?? '').toUpperCase() !== shiftNo) return false
    if (filter.userEnabled && userNo && (row.UserNo ?? '').toUpperCase() !== userNo) return false
    if (infoMode.value === 'history' && filter.exceptionEnabled && Number(row.ExceptionTime) !== Number(filter.exceptionTime)) return false
    const ranges = infoMode.value === 'history'
      ? [
          { key: 'LoginTime', enabledStart: filter.loginStartEnabled, start: filter.loginStartDate, enabledEnd: filter.loginEndEnabled, end: filter.loginEndDate },
          { key: 'LogoutTime', enabledStart: filter.logoutStartEnabled, start: filter.logoutStartDate, enabledEnd: filter.logoutEndEnabled, end: filter.logoutEndDate },
        ]
      : [{ key: 'LoginTime', enabledStart: filter.startEnabled, start: filter.startDate, enabledEnd: filter.endEnabled, end: filter.endDate }]
    for (const range of ranges) {
      const start = range.enabledStart && range.start ? new Date(range.start).valueOf() : null
      const end = range.enabledEnd && range.end ? new Date(range.end).valueOf() : null
      if (start === null && end === null) continue
      const value = new Date(row[range.key] ?? '').valueOf()
      if (Number.isNaN(value)) return false
      if (start !== null && value < start) return false
      if (end !== null && value > end) return false
    }
    return true
  })
}
function normalizeWorkInfoRows(rows: Array<Record<string, unknown>>, history: boolean): Record<string, string>[] {
  const value = (row: Record<string, unknown>, ...names: string[]): string => {
    const key = Object.keys(row).find(candidate => names.some(name => candidate.toLowerCase() === name.toLowerCase()))
    const item = key ? row[key] : ''
    return item === null || item === undefined ? '' : String(item)
  }
  return rows.map(row => {
    const mapped: Record<string, string> = {
      AreaNo: value(row, 'areaNo'), DepartmentNo: value(row, 'departmentNo'), ShiftNo: value(row, 'shiftNo'),
      UserNo: value(row, 'userNo'), LoginTime: formatDate(value(row, 'loginTime')), LoginComment: value(row, 'loginComment'),
    }
    if (history) Object.assign(mapped, { LogoutTime: formatDate(value(row, 'logoutTime')), ExceptionTime: value(row, 'exceptionTime'), LogoutComment: value(row, 'logoutComment') })
    return mapped
  })
}
async function queryWorkInfo(): Promise<void> {
  if (!infoNode.value || !infoMode.value) return
  if (mockMode.value) { filterWorkInfoRows(); return }
  infoBusy.value = true
  try {
    const filter = infoFilter.value
    const query = new URLSearchParams({ departmentNo: filter.departmentNo })
    if (filter.shiftEnabled && filter.shiftNo.trim()) query.set('shiftNo', filter.shiftNo.trim().toUpperCase())
    if (filter.userEnabled && filter.userNo.trim()) query.set('userNo', filter.userNo.trim().toUpperCase())
    if (infoMode.value === 'state') {
      if (filter.startEnabled && filter.startDate) query.set('startDate', new Date(filter.startDate).toISOString())
      if (filter.endEnabled && filter.endDate) query.set('endDate', new Date(filter.endDate).toISOString())
    } else {
      if (filter.loginStartEnabled && filter.loginStartDate) query.set('loginStartDate', new Date(filter.loginStartDate).toISOString())
      if (filter.loginEndEnabled && filter.loginEndDate) query.set('loginEndDate', new Date(filter.loginEndDate).toISOString())
      if (filter.logoutStartEnabled && filter.logoutStartDate) query.set('logoutStartDate', new Date(filter.logoutStartDate).toISOString())
      if (filter.logoutEndEnabled && filter.logoutEndDate) query.set('logoutEndDate', new Date(filter.logoutEndDate).toISOString())
      if (filter.exceptionEnabled) query.set('exceptionTime', String(filter.exceptionTime))
    }
    const suffix = infoMode.value === 'state' ? 'current' : 'history'
    const rows = await requestJson<Array<Record<string, unknown>>>(`api/wip/areas/${encodeURIComponent(infoNode.value.objectNo)}/operators/${suffix}?${query.toString()}`)
    infoRows.value = normalizeWorkInfoRows(rows, infoMode.value === 'history')
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '作業人員資料查詢失敗。' }
  finally { infoBusy.value = false }
}
const filteredInfoLookupRows = computed(() => {
  const search = infoLookupSearch.value.trim().toLocaleLowerCase()
  return infoLookupRows.value.filter(row => !search || row.value.toLocaleLowerCase().includes(search) || row.label.toLocaleLowerCase().includes(search))
})
async function openInfoLookup(kind: 'shift' | 'user'): Promise<void> {
  infoLookupKind.value = kind
  infoLookupRows.value = []
  infoLookupSearch.value = ''
  infoLookupBusy.value = true
  try {
    if (mockMode.value) {
      if (kind === 'shift') {
        const rows = readStorage<AreaShift[]>(shiftsKey, seedShifts).filter(row => row.departmentNo === infoFilter.value.departmentNo)
        infoLookupRows.value = [...new Map(rows.map(row => [row.shiftNo, { value: row.shiftNo, label: row.shiftNo + (row.description ? '　' + row.description : '') }])).values()]
      } else {
        infoLookupRows.value = readStorage<UserOption[]>('ymes-dev-mock:smd-area-show:users:v1', seedUsers)
          .filter(row => !row.departmentNo || row.departmentNo === infoFilter.value.departmentNo)
          .map(row => ({ value: row.userNo, label: row.userNo + '　' + row.userName }))
      }
      return
    }
    const query = new URLSearchParams({ departmentNo: infoFilter.value.departmentNo })
    const result = await requestJson<{ shifts: LookupOption[]; users: UserOption[] }>(
      `api/wip/areas/${encodeURIComponent(infoFilter.value.areaNo)}/operators/filter-options?${query.toString()}`,
    )
    infoLookupRows.value = kind === 'shift'
      ? result.shifts
      : result.users.map(row => ({ value: row.userNo, label: row.userNo + '　' + row.userName }))
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '篩選資料載入失敗。'
    infoLookupKind.value = null
  } finally { infoLookupBusy.value = false }
}
function chooseInfoLookup(row: LookupOption): void {
  if (infoLookupKind.value === 'shift') {
    infoFilter.value.shiftNo = row.value
    infoFilter.value.shiftEnabled = true
  } else if (infoLookupKind.value === 'user') {
    infoFilter.value.userNo = row.value
    infoFilter.value.userEnabled = true
  }
  infoLookupKind.value = null
}

function currentDevShiftNo(): string { const hour = new Date().getHours(); return hour >= 8 && hour < 20 ? 'DAY' : 'NIGHT' }
function openWorkAreaDialog(node: LayoutNode): void {
  contextMenu.value = null
  workAreaDialogOpen.value = true
  workAreaBusy.value = false
  workAreaError.value = ''
  workAreaVerified.value = false
  workAreaShiftOptions.value = []
  workAreaForm.value = { areaNo: node.objectNo, areaName: node.objectText, departmentNo: '', shiftNo: '', userNo: '', comment: '' }
  workAreaState.value = mockMode.value ? readStorage<WipAreaOperatorState[]>(workAreaStateKey, seedWorkAreaStates) : []
}
function resetWorkAreaVerification(): void {
  workAreaVerified.value = false
  workAreaShiftOptions.value = []
  workAreaForm.value.departmentNo = ''
  workAreaForm.value.shiftNo = ''
  workAreaError.value = ''
}
async function confirmWorkAreaUser(): Promise<void> {
  const userNo = workAreaForm.value.userNo.trim().toUpperCase()
  workAreaForm.value.userNo = userNo
  workAreaVerified.value = false
  workAreaError.value = ''
  if (!userNo) { workAreaError.value = '請輸入使用者編號。'; return }
  workAreaBusy.value = true
  try {
    if (mockMode.value) {
      workAreaState.value = readStorage<WipAreaOperatorState[]>(workAreaStateKey, seedWorkAreaStates)
      const active = workAreaState.value.find(row => row.userNo.toUpperCase() === userNo)
      if (active) {
        if (active.areaNo === workAreaForm.value.areaNo) {
          workAreaForm.value.departmentNo = active.departmentNo
          workAreaForm.value.shiftNo = active.shiftNo
          workAreaShiftOptions.value = [active.shiftNo]
          workAreaError.value = '此使用者已登入此區域；可直接登出。'
        } else workAreaError.value = '此使用者已在其他區域登入。'
        return
      }
      const shifts = readStorage<AreaShift[]>(shiftsKey, seedShifts).filter(row => row.areaNo === workAreaForm.value.areaNo)
      const operators = readStorage<Record<string, Operator[]>>(operatorsKey, seedOperators)
      const currentShift = currentDevShiftNo()
      const matches = shifts.filter(row => row.shiftNo === currentShift && (operators[shiftKey(row)] ?? []).some(operator => operator.userNo.toUpperCase() === userNo))
      if (!matches.length) { workAreaError.value = '此使用者不在目前時段的區域排班名單。'; return }
      workAreaForm.value.departmentNo = matches[0].departmentNo
      workAreaShiftOptions.value = [...new Set(matches.map(row => row.shiftNo))]
      workAreaForm.value.shiftNo = workAreaShiftOptions.value[0]
      workAreaVerified.value = true
      workAreaError.value = '已確認目前班別，可以登入。'
      return
    }
    const result = await requestJson<{ eligible: boolean; alreadyLoggedInAreaNo?: string; departmentNo?: string; shiftNo?: string; shiftNos?: string[] }>(
      'api/wip/areas/' + encodeURIComponent(workAreaForm.value.areaNo) + '/operators/eligible?userNo=' + encodeURIComponent(userNo),
    )
    if (result.alreadyLoggedInAreaNo) {
      workAreaError.value = result.alreadyLoggedInAreaNo === workAreaForm.value.areaNo ? '此使用者已登入此區域；可直接登出。' : '此使用者已在其他區域登入。'
      workAreaForm.value.departmentNo = result.departmentNo ?? ''
      workAreaForm.value.shiftNo = result.shiftNo ?? ''
      workAreaShiftOptions.value = result.shiftNo ? [result.shiftNo] : []
      workAreaState.value = [{ areaNo: result.alreadyLoggedInAreaNo, departmentNo: result.departmentNo ?? '', shiftNo: result.shiftNo ?? '', userNo, loginComment: '', loginTime: '', exceptionTime: 0 }]
      return
    }
    const shiftNos = result.shiftNos?.length ? result.shiftNos : (result.shiftNo ? [result.shiftNo] : [])
    if (!result.eligible || !result.departmentNo || !shiftNos.length) { workAreaError.value = '此使用者不在目前時段的區域排班名單。'; return }
    workAreaForm.value.departmentNo = result.departmentNo
    workAreaShiftOptions.value = shiftNos
    workAreaForm.value.shiftNo = shiftNos[0]
    workAreaVerified.value = true
    workAreaError.value = '已確認目前班別，可以登入。'
  } catch (error) { workAreaError.value = error instanceof Error ? error.message : '使用者班別確認失敗。' }
  finally { workAreaBusy.value = false }
}
async function loginWorkArea(): Promise<void> {
  if (!workAreaVerified.value || !workAreaForm.value.departmentNo || !workAreaForm.value.shiftNo) { workAreaError.value = '請先確認使用者及目前班別。'; return }
  workAreaBusy.value = true; workAreaError.value = ''
  try {
    const now = new Date().toISOString()
    if (mockMode.value) {
      const states = readStorage<WipAreaOperatorState[]>(workAreaStateKey, seedWorkAreaStates)
      if (states.some(row => row.userNo.toUpperCase() === workAreaForm.value.userNo)) throw new Error('此使用者已登入區域。')
      const policy = readStorage<AreaShift[]>(shiftsKey, seedShifts).find(row => row.areaNo === workAreaForm.value.areaNo && row.departmentNo === workAreaForm.value.departmentNo && row.shiftNo === workAreaForm.value.shiftNo)
      states.push({ areaNo: workAreaForm.value.areaNo, departmentNo: workAreaForm.value.departmentNo, shiftNo: workAreaForm.value.shiftNo, userNo: workAreaForm.value.userNo, loginComment: workAreaForm.value.comment.trim(), loginTime: now, exceptionTime: policy?.exceptionTime ?? 0 })
      writeStorage(workAreaStateKey, states)
    } else {
      await requestJson('api/wip/areas/' + encodeURIComponent(workAreaForm.value.areaNo) + '/operators/login', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ departmentNo: workAreaForm.value.departmentNo, shiftNo: workAreaForm.value.shiftNo, userNo: workAreaForm.value.userNo, loginComment: workAreaForm.value.comment.trim() }),
      })
    }
    workAreaDialogOpen.value = false
  } catch (error) { workAreaError.value = error instanceof Error ? error.message : '區域登入失敗。' }
  finally { workAreaBusy.value = false }
}
async function logoutWorkArea(): Promise<void> {
  const userNo = workAreaForm.value.userNo.trim().toUpperCase()
  if (!userNo) { workAreaError.value = '請輸入使用者編號並確認目前登入狀態。'; return }
  workAreaBusy.value = true; workAreaError.value = ''
  try {
    if (mockMode.value) {
      const states = readStorage<WipAreaOperatorState[]>(workAreaStateKey, seedWorkAreaStates)
      const active = states.find(row => row.userNo.toUpperCase() === userNo && row.areaNo === workAreaForm.value.areaNo)
      if (!active) throw new Error('此區域找不到該使用者的登入紀錄。')
      const policy = readStorage<AreaShift[]>(shiftsKey, seedShifts).find(row => row.areaNo === active.areaNo && row.departmentNo === active.departmentNo && row.shiftNo === active.shiftNo)
      const logs = readStorage<WipAreaOperatorLog[]>(workAreaLogKey, seedWorkAreaLogs)
      logs.unshift({ ...active, exceptionTime: policy?.exceptionTime ?? active.exceptionTime, logoutComment: workAreaForm.value.comment.trim(), logoutTime: new Date().toISOString() })
      writeStorage(workAreaLogKey, logs)
      writeStorage(workAreaStateKey, states.filter(row => row.userNo.toUpperCase() !== userNo))
    } else {
      await requestJson('api/wip/areas/' + encodeURIComponent(workAreaForm.value.areaNo) + '/operators/logout', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userNo, logoutComment: workAreaForm.value.comment.trim() }),
      })
    }
    workAreaDialogOpen.value = false
  } catch (error) { workAreaError.value = error instanceof Error ? error.message : '區域登出失敗。' }
  finally { workAreaBusy.value = false }
}

</script>

<template>
  <section class="area-show-page" @click="contextMenu = null">
    <header class="page-title"><h1>設備狀態監控_區域</h1></header>
    <div class="toolbar">
      <label class="base-select"><span>Base Area</span><input :value="activeArea ? activeArea.areaNo + '　' + activeArea.areaName : ''" readonly class="kd-input"><button class="win-btn picker" :disabled="loading" @click="loadBaseAreas">...</button></label>
      <span class="toolbar-spacer"></span>
      <label>縮放 <select v-model.number="zoom" class="plain-input"><option v-for="level in zoomLevels" :key="level" :value="level">{{ level }}%</option></select></label>
      <button v-if="workAreaMode" class="win-btn" @click="openGridProperties">Grid</button>
      <button v-else class="win-btn" :class="{ toggled: gridVisible }" @click="gridVisible = !gridVisible">網格線</button>
    </div>
    <p v-if="errorMessage" class="message error">{{ errorMessage }}</p>
    <main class="canvas-frame" :class="{ 'with-grid': canvasGridVisible }" :style="canvasGridStyle">
      <div v-if="!activeArea && !loading" class="canvas-empty">請選擇 Base Area 載入區域配置</div>
      <div v-else-if="loading" class="canvas-empty">載入中...</div>
      <svg v-else :width="984 * zoom / 100" :height="469 * zoom / 100" viewBox="0 0 984 469" class="layout-canvas" role="img" aria-label="區域配置圖">
        <g v-for="node in nodes" :key="node.objectType + ':' + node.objectNo" class="layout-node" :class="nodeClass(node)" @click.stop="contextMenu = null" @contextmenu.prevent.stop="showContext($event, node)" @dblclick.stop="areaNode(node) && workAreaActionsVisible && (workAreaMode ? openWorkAreaDialog(node) : openShiftFor(node))">
          <rect v-if="nodeShape(node) === 'rect'" :x="node.objectX" :y="node.objectY" :width="node.objectWidth" :height="node.objectLength" :fill="colorCss(node.objectColor)" stroke="#555" />
          <rect v-else-if="nodeShape(node) === 'round'" :x="node.objectX" :y="node.objectY" :width="node.objectWidth" :height="node.objectLength" rx="12" :fill="colorCss(node.objectColor)" stroke="#555" />
          <ellipse v-else-if="nodeShape(node) === 'ellipse'" :cx="node.objectX + node.objectWidth / 2" :cy="node.objectY + node.objectLength / 2" :rx="node.objectWidth / 2" :ry="node.objectLength / 2" :fill="colorCss(node.objectColor)" stroke="#555" />
          <polygon v-else :points="(node.objectX + 13)+','+node.objectY+' '+(node.objectX+node.objectWidth-13)+','+node.objectY+' '+(node.objectX+node.objectWidth)+','+(node.objectY+node.objectLength/2)+' '+(node.objectX+node.objectWidth-13)+','+(node.objectY+node.objectLength)+' '+(node.objectX+13)+','+(node.objectY+node.objectLength)+' '+node.objectX+','+(node.objectY+node.objectLength/2)" :fill="colorCss(node.objectColor)" stroke="#555" />
          <text :x="node.objectX + node.objectWidth / 2" :y="node.objectType === 0 ? node.objectY + 20 : node.objectY + node.objectLength / 2" text-anchor="middle" dominant-baseline="middle">{{ node.objectText }}</text>
          <text :x="node.objectX + node.objectWidth / 2" :y="node.objectY + node.objectLength - 8" text-anchor="middle" class="node-code">{{ node.objectNo }}</text>
        </g>
        <text v-if="nodes.length === 0" x="492" y="235" text-anchor="middle" class="canvas-placeholder">目前沒有配置節點</text>
      </svg>
      <div v-if="contextMenu" class="context-menu" :style="{ left: contextMenu.x + 'px', top: contextMenu.y + 'px' }" @click.stop>
        <template v-if="workAreaMode">
          <button @click="openWorkAreaDialog(contextMenu.node)">Login/Logout</button>
          <button @click="openInfo('state', contextMenu.node)">State</button>
          <button @click="openInfo('history', contextMenu.node)">History</button>
        </template>
        <template v-else>
          <button @click="openShiftFor(contextMenu.node)">班別設定</button>
          <button @click="openInfo('state', contextMenu.node)">目前作業人員</button>
          <button @click="openInfo('history', contextMenu.node)">歷史紀錄</button>
        </template>
      </div>
    </main>
    <footer class="status-line"><span>{{ activeArea ? activeArea.areaNo + '　' + activeArea.areaName : '未選擇區域' }}</span><span>節點數：{{ nodes.length }}</span><span>縮放 {{ zoom }}%</span><button class="win-btn footer-close" @click="emit('close')">關閉</button></footer>

    <div v-if="areaPickerOpen" class="backdrop" @click.self="areaPickerOpen = false">
      <section class="dialog area-dialog">
        <header class="dialog-title"><h2>選擇 Base Area</h2><button class="x-button" @click="areaPickerOpen = false">×</button></header>
        <div class="picker-search"><label>區域編號 / 名稱 <input v-model="areaSearch" class="kd-input"></label></div>
        <div class="table-wrap"><table><thead><tr><th>區域編號</th><th>區域名稱</th><th>部門</th></tr></thead><tbody>
          <tr v-for="row in visibleAreas" :key="row.areaNo" @click="chooseArea(row)"><td>{{ row.areaNo }}</td><td>{{ row.areaName }}</td><td>{{ row.departmentNo }}</td></tr>
          <tr v-if="visibleAreas.length === 0"><td colspan="3" class="empty">沒有已核准的 Base Area</td></tr>
        </tbody></table></div>
      </section>
    </div>
    <div v-if="workAreaMode && gridSettingsOpen" class="backdrop" @click.self="gridSettingsOpen = false">
      <section class="dialog grid-properties-dialog">
        <header class="dialog-title"><h2>Grid properties</h2><button class="x-button" @click="gridSettingsOpen = false">×</button></header>
        <fieldset class="grid-properties-fields"><legend>Properties</legend>
          <label><input v-model="gridSettingsDraft.draw" type="checkbox">Show Grid</label>
          <label><input v-model="gridSettingsDraft.snap" type="checkbox">Snap To Grid</label>
          <label class="grid-color-field"><span>Grid Color</span><input v-model="gridSettingsDraft.color" type="color"></label>
          <label><span>Grid Style</span><select v-model="gridSettingsDraft.style" class="kd-input"><option value="Pixels">Pixels</option><option value="Lines">Lines</option><option value="DottedLines">DottedLines</option></select></label>
          <label><span>Grid Size Width</span><input v-model.number="gridSettingsDraft.sizeX" type="number" min="1" max="500" class="kd-input"></label>
          <label><span>Grid Size Height</span><input v-model.number="gridSettingsDraft.sizeY" type="number" min="1" max="500" class="kd-input"></label>
        </fieldset>
        <p v-if="gridSettingsError" class="message error">{{ gridSettingsError }}</p>
        <footer class="dialog-actions"><button class="win-btn primary" @click="saveGridProperties">OK</button><button class="win-btn" @click="gridSettingsOpen = false">Close</button></footer>
      </section>
    </div>
    <div v-if="workAreaMode && workAreaDialogOpen" class="backdrop" @click.self="workAreaDialogOpen = false">
      <section class="dialog work-area-login-dialog">
        <header class="dialog-title"><h2>區域 Login / Logout｜{{ workAreaForm.areaName }}</h2><button class="x-button" @click="workAreaDialogOpen = false">×</button></header>
        <fieldset class="work-area-fields"><legend>作業區域人員</legend>
          <label><span>AreaNo</span><input :value="workAreaForm.areaNo" readonly class="kd-input"></label>
          <label><span>DepartmentNo</span><input :value="workAreaForm.departmentNo" readonly class="kd-input"></label>
          <label><span>UserNo</span><input v-model="workAreaForm.userNo" class="kd-input" maxlength="10" @input="resetWorkAreaVerification" @keydown.enter.prevent="confirmWorkAreaUser"></label>
          <button type="button" class="win-btn" :disabled="workAreaBusy" @click="confirmWorkAreaUser">Confirm</button>
          <label><span>ShiftNo</span><select v-model="workAreaForm.shiftNo" :disabled="!workAreaShiftOptions.length || workAreaCanLogout" class="kd-input"><option v-for="shiftNo in workAreaShiftOptions" :key="shiftNo" :value="shiftNo">{{ shiftNo }}</option></select></label>
          <label class="work-comment"><span>Comment</span><textarea v-model="workAreaForm.comment" class="kd-input" maxlength="255"></textarea></label>
        </fieldset>
        <p v-if="workAreaError" class="message" :class="{ error: !workAreaVerified }">{{ workAreaError }}</p>
        <footer class="dialog-actions"><button class="win-btn primary" :disabled="workAreaBusy || !workAreaVerified || workAreaCanLogout" @click="loginWorkArea">Login</button><button class="win-btn" :disabled="workAreaBusy || !workAreaCanLogout" @click="logoutWorkArea">Logout</button><button class="win-btn" :disabled="workAreaBusy" @click="workAreaDialogOpen = false">Close</button></footer>
      </section>
    </div>
    <div v-if="shiftOpen" class="backdrop" @click.self="shiftOpen = false">
      <section class="dialog shift-dialog">
        <header class="dialog-title"><h2>區域班別設定｜{{ shiftArea?.areaNo }}　{{ shiftArea?.areaName }}</h2><button class="x-button" @click="shiftOpen = false">×</button></header>
        <div class="area-fields"><label>區域編號<input :value="shiftArea?.areaNo" readonly></label><label>部門編號<input :value="areaDepartmentNo" readonly></label></div>
        <div class="shift-master">
          <div class="table-wrap"><table><thead><tr><th class="check-col"></th><th>班別</th><th>管理者</th><th>異常時間</th><th>說明</th><th>建立人</th><th>建立日期</th></tr></thead><tbody>
            <tr v-for="row in shiftRows" :key="shiftKey(row)" :class="{ active: selectedShiftKey === shiftKey(row) }" @click="activateShift(row)">
              <td class="check-col"><input type="checkbox" :checked="checkedShiftKeys.includes(shiftKey(row))" @click.stop @change="toggleShift(row, ($event.target as HTMLInputElement).checked)"></td>
              <td>{{ row.shiftNo }}</td><td>{{ row.managerNo }}</td><td>{{ row.exceptionTime }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td>
            </tr>
            <tr v-if="!shiftRows.length"><td colspan="7" class="empty">尚未設定班別</td></tr>
          </tbody></table></div>
          <aside class="shift-actions"><button class="win-btn primary" @click="openShiftForm('add')">新增(A)</button><button class="win-btn" :disabled="!activeShift" @click="openShiftForm('edit')">修改(E)</button><button class="win-btn" :disabled="!activeShift" @click="openShiftForm('copy')">複製(C)</button><button class="win-btn danger" :disabled="!activeShift" @click="deleteShifts">刪除(D)</button></aside>
        </div>
        <fieldset class="operators"><legend>班別作業人員設定　{{ activeShift?.shiftNo ?? '' }}</legend>
          <div class="operator-pane"><strong>可選擇人員</strong><div class="operator-list"><button v-for="user in remainingUsers" :key="user.userNo" :class="{ checked: checkedAvailableNos.includes(user.userNo) }" @click="checkedAvailableNos = checkedAvailableNos.includes(user.userNo) ? checkedAvailableNos.filter(x => x !== user.userNo) : [...checkedAvailableNos, user.userNo]">{{ user.userNo }}　{{ user.userName }}</button><span v-if="!remainingUsers.length" class="empty">沒有可加入人員</span></div></div>
          <div class="move-actions"><button class="win-btn" :disabled="!activeShift" @click="moveUsers('add')">&gt;</button><button class="win-btn" :disabled="!activeShift" @click="moveUsers('remove')">&lt;</button></div>
          <div class="operator-pane"><strong>已設定人員</strong><div class="operator-list"><button v-for="user in assignedUsers" :key="user.userNo" :class="{ checked: checkedOperatorNos.includes(user.userNo) }" @click="checkedOperatorNos = checkedOperatorNos.includes(user.userNo) ? checkedOperatorNos.filter(x => x !== user.userNo) : [...checkedOperatorNos, user.userNo]">{{ user.userNo }}　{{ user.userName }}</button><span v-if="!assignedUsers.length" class="empty">尚未加入人員</span></div></div>
          <button class="win-btn primary save-operators" :disabled="!activeShift || shiftBusy" @click="saveOperators">確定</button>
        </fieldset>
        <p v-if="shiftError" class="message error">{{ shiftError }}</p>
        <footer class="dialog-actions"><button class="win-btn" @click="shiftOpen = false">關閉</button></footer>
      </section>
    </div>
    <div v-if="shiftFormMode" class="backdrop nested" @click.self="closeShiftForm">
      <form class="dialog shift-editor" @submit.prevent="saveShift(true)">
        <header class="dialog-title"><h2>{{ shiftFormTitle }}</h2><button type="button" class="x-button" @click="closeShiftForm">×</button></header>
        <fieldset class="shift-fields"><legend>班別資料</legend>
          <label class="field-row"><span>區域編號</span><input :value="shiftArea?.areaNo" readonly></label>
          <label class="field-row"><span>部門編號</span><input :value="areaDepartmentNo" readonly></label>
          <label class="field-row"><span>班別編號</span><select v-model="shiftForm.shiftNo" class="kd-input" :disabled="shiftFormMode === 'edit'"><option value="">請選擇</option><option v-for="option in shiftNumberOptions" :key="option.value" :value="option.value">{{ option.label }}</option></select></label>
          <label class="field-row"><span>管理者</span><select v-model="shiftForm.managerNo" class="kd-input"><option value="">請選擇</option><option v-for="option in managerOptions" :key="option.userNo" :value="option.userNo">{{ option.userNo }}　{{ option.userName }}</option></select></label>
          <label class="field-row"><span>異常時間</span><input v-model.number="shiftForm.exceptionTime" class="kd-input" type="number" min="0" max="9999"></label>
          <label class="field-row description-row"><span>說明</span><textarea v-model="shiftForm.description" class="kd-input" maxlength="255"></textarea></label>
        </fieldset>
        <fieldset class="audit-fields"><legend>資料建立 / 修改資訊</legend><label>建立人<input :value="shiftAudit.creator" readonly></label><label>建立日期<input :value="formatDate(shiftAudit.createDate)" readonly></label><label>修改人<input :value="shiftAudit.reviser" readonly></label><label>修改日期<input :value="formatDate(shiftAudit.reviseDate)" readonly></label></fieldset>
        <p v-if="shiftFormError" class="message error">{{ shiftFormError }}</p>
        <footer class="dialog-actions"><button class="win-btn primary" :disabled="shiftBusy">確定</button><button type="button" class="win-btn" @click="closeShiftForm">關閉</button><button type="button" class="win-btn" :disabled="shiftBusy" @click="saveShift(false)">套用</button></footer>
      </form>
    </div>
    <div v-if="infoMode && infoNode" class="backdrop" @click.self="infoMode = null">
      <section class="dialog info-dialog" :class="{ 'work-info-dialog': workAreaMode }">
        <header class="dialog-title"><h2>{{ infoMode === 'state' ? '目前作業人員' : '作業歷史紀錄' }}｜{{ infoNode.objectText }}</h2><button class="x-button" @click="infoMode = null">×</button></header>
        <div v-if="workAreaMode" class="work-info-filters">
          <div class="work-info-identity"><label>AreaNo<input :value="infoFilter.areaNo" readonly></label><label>DepartmentNo<input :value="infoFilter.departmentNo" readonly></label></div>
          <template v-if="infoMode === 'history'">
            <div class="work-info-tabs"><button type="button" :class="{ selected: infoDateTab === 'filter' }" @click="infoDateTab = 'filter'">Filter</button><button type="button" :class="{ selected: infoDateTab === 'dateFilter' }" @click="infoDateTab = 'dateFilter'">DateFilter</button></div>
            <div v-if="infoDateTab === 'filter'" class="work-info-row">
              <div class="work-info-lookup"><label class="work-info-check"><input v-model="infoFilter.shiftEnabled" type="checkbox">ShiftNo</label><input v-model="infoFilter.shiftNo" :disabled="!infoFilter.shiftEnabled" class="kd-input"><button type="button" class="win-btn lookup-button" aria-label="查找班別" @click="openInfoLookup('shift')">...</button></div>
              <div class="work-info-lookup"><label class="work-info-check"><input v-model="infoFilter.userEnabled" type="checkbox">UserNo</label><input v-model="infoFilter.userNo" :disabled="!infoFilter.userEnabled" class="kd-input"><button type="button" class="win-btn lookup-button" aria-label="查找使用者" @click="openInfoLookup('user')">...</button></div>
              <div class="work-info-lookup"><label class="work-info-check"><input v-model="infoFilter.exceptionEnabled" type="checkbox">ExceptionTime</label><input v-model.number="infoFilter.exceptionTime" type="number" min="0" :disabled="!infoFilter.exceptionEnabled" class="kd-input"></div>
            </div>
            <div v-else class="work-date-filter">
              <label class="work-info-check"><input v-model="infoFilter.loginStartEnabled" type="checkbox">LoginStartDate</label><input v-model="infoFilter.loginStartDate" type="datetime-local" :disabled="!infoFilter.loginStartEnabled" class="kd-input"><label class="work-info-check"><input v-model="infoFilter.loginEndEnabled" type="checkbox">LoginEndDate</label><input v-model="infoFilter.loginEndDate" type="datetime-local" :disabled="!infoFilter.loginEndEnabled" class="kd-input">
              <label class="work-info-check"><input v-model="infoFilter.logoutStartEnabled" type="checkbox">LogoutStartDate</label><input v-model="infoFilter.logoutStartDate" type="datetime-local" :disabled="!infoFilter.logoutStartEnabled" class="kd-input"><label class="work-info-check"><input v-model="infoFilter.logoutEndEnabled" type="checkbox">LogoutEndDate</label><input v-model="infoFilter.logoutEndDate" type="datetime-local" :disabled="!infoFilter.logoutEndEnabled" class="kd-input">
            </div>
            <div class="work-info-query"><button class="win-btn primary" :disabled="infoBusy" @click="queryWorkInfo">查詢</button></div>
          </template>
          <template v-else>
            <div class="work-info-row">
              <div class="work-info-lookup"><label class="work-info-check"><input v-model="infoFilter.shiftEnabled" type="checkbox">ShiftNo</label><input v-model="infoFilter.shiftNo" :disabled="!infoFilter.shiftEnabled" class="kd-input"><button type="button" class="win-btn lookup-button" aria-label="查找班別" @click="openInfoLookup('shift')">...</button></div>
              <div class="work-info-lookup"><label class="work-info-check"><input v-model="infoFilter.userEnabled" type="checkbox">UserNo</label><input v-model="infoFilter.userNo" :disabled="!infoFilter.userEnabled" class="kd-input"><button type="button" class="win-btn lookup-button" aria-label="查找使用者" @click="openInfoLookup('user')">...</button></div>
            </div>
            <div class="work-info-row"><label class="work-info-check"><input v-model="infoFilter.startEnabled" type="checkbox">StartDateTime</label><input v-model="infoFilter.startDate" type="datetime-local" :disabled="!infoFilter.startEnabled" class="kd-input"><label class="work-info-check"><input v-model="infoFilter.endEnabled" type="checkbox">EndDateTime</label><input v-model="infoFilter.endDate" type="datetime-local" :disabled="!infoFilter.endEnabled" class="kd-input"></div>
            <div class="work-info-query"><button class="win-btn primary" :disabled="infoBusy" @click="queryWorkInfo">查詢</button></div>
          </template>
        </div>
        <div class="table-wrap"><table><thead><tr><th v-for="key in infoColumns" :key="key">{{ key }}</th></tr></thead><tbody><tr v-for="(row, index) in infoRows" :key="index"><td v-for="key in infoColumns" :key="key">{{ row[key] }}</td></tr><tr v-if="!infoRows.length"><td :colspan="Math.max(infoColumns.length, 1)" class="empty">目前沒有資料</td></tr></tbody></table></div>
        <footer class="dialog-actions"><button class="win-btn" @click="infoMode = null">{{ workAreaMode ? 'Close' : '關閉' }}</button></footer>
      </section>
      <div v-if="infoLookupKind" class="backdrop nested lookup-backdrop" @click.self="infoLookupKind = null">
        <section class="dialog info-lookup-dialog">
          <header class="dialog-title"><h2>{{ infoLookupKind === 'shift' ? '選擇班別' : '選擇使用者' }}</h2><button type="button" class="x-button" @click="infoLookupKind = null">×</button></header>
          <input v-model="infoLookupSearch" class="kd-input info-lookup-search" :placeholder="infoLookupKind === 'shift' ? '搜尋班別' : '搜尋編號或姓名'" autofocus>
          <p v-if="infoLookupBusy" class="empty">載入中…</p>
          <div v-else class="info-lookup-list"><button v-for="row in filteredInfoLookupRows" :key="row.value" type="button" @click="chooseInfoLookup(row)">{{ row.label }}</button><p v-if="!filteredInfoLookupRows.length" class="empty">沒有可選資料</p></div>
          <footer class="dialog-actions"><button type="button" class="win-btn" @click="infoLookupKind = null">關閉</button></footer>
        </section>
      </div>
    </div>
  </section>
</template>

<style scoped>
.work-area-login-dialog{width:min(420px,96vw);min-height:304px;padding:0 10px 7px}.work-area-login-dialog>.dialog-title{margin:0 -10px}.work-area-fields{display:grid;grid-template-columns:minmax(0,1fr) 82px;gap:7px 9px;margin:10px 0 4px;padding:10px;border:1px solid #aaa}.work-area-fields legend{padding:0 5px;color:#555}.work-area-fields label{display:grid;grid-template-columns:105px minmax(0,1fr);align-items:center;gap:6px}.work-area-fields label>span{text-align:right}.work-area-fields input{width:100%}.work-area-fields textarea{height:52px;resize:vertical}.work-area-fields .work-comment{grid-column:1/-1;grid-template-columns:105px minmax(0,1fr);align-items:start}.work-area-fields>button{align-self:center}.work-area-login-dialog>.message{margin:3px 0}.work-area-login-dialog>.dialog-actions{margin-top:auto}
.work-info-dialog{width:min(820px,98vw);height:min(560px,94vh);padding:0 10px 6px}.work-info-dialog>.dialog-title{margin:0 -10px}.work-info-filters{flex:none;display:flex;flex-direction:column;gap:5px;margin-top:7px}.work-info-identity,.work-info-row{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));align-items:center;gap:6px}.work-info-identity label{display:grid;grid-template-columns:88px minmax(0,1fr);align-items:center;gap:5px}.work-info-identity input{width:100%;height:24px;box-sizing:border-box;border:1px solid #aaa;background:#eee;padding:2px 5px}.work-info-row input.kd-input{width:100%;min-width:0}.work-info-lookup{display:grid;grid-template-columns:auto minmax(0,1fr) 27px;align-items:center;gap:4px;min-width:0}.work-info-lookup .lookup-button{height:24px;min-width:26px;padding:0 3px}.work-info-check{display:flex;align-items:center;gap:4px;white-space:nowrap}.work-info-check input{accent-color:#e7a137}.work-info-query{display:flex;justify-content:flex-end}.work-info-tabs{display:flex;gap:2px;border-bottom:1px solid #aaa}.work-info-tabs button{padding:4px 12px;border:1px solid #aaa;border-bottom:0;background:#efeee9;cursor:pointer}.work-info-tabs button.selected{background:#fff;font-weight:600}.work-date-filter{display:grid;grid-template-columns:126px minmax(0,1fr) 126px minmax(0,1fr);align-items:center;gap:5px}.work-info-dialog .table-wrap{flex:1;margin-top:7px}.work-info-dialog .dialog-actions{flex:none}.lookup-backdrop{z-index:20}.info-lookup-dialog{width:min(420px,94vw);max-height:min(480px,85vh);padding:0 10px 7px}.info-lookup-dialog>.dialog-title{margin:0 -10px}.info-lookup-search{width:100%;margin:9px 0 5px;box-sizing:border-box}.info-lookup-list{display:flex;flex-direction:column;overflow:auto;min-height:100px;max-height:330px;border:1px solid #aaa}.info-lookup-list button{padding:7px 9px;border:0;border-bottom:1px solid #ddd;background:#fff;text-align:left;cursor:pointer}.info-lookup-list button:hover{background:#ffe7a0}.info-lookup-list .empty{padding:12px;text-align:center}.info-lookup-dialog>.dialog-actions{flex:none;margin-top:7px}
.grid-properties-dialog{width:min(420px,96vw);min-height:285px;padding:0 10px 6px}.grid-properties-dialog>.dialog-title{margin:0 -10px}.grid-properties-fields{display:grid;grid-template-columns:1fr 1fr;gap:8px 12px;margin:10px 0 4px;padding:10px;border:1px solid #aaa}.grid-properties-fields legend{padding:0 5px;color:#555}.grid-properties-fields label{display:flex;align-items:center;gap:6px;min-width:0}.grid-properties-fields label>span{width:104px;flex:none;text-align:right}.grid-properties-fields input.kd-input,.grid-properties-fields select{width:90px;min-width:0}.grid-properties-fields .grid-color-field{grid-column:1/-1}.grid-properties-fields input[type=color]{width:42px;height:26px;padding:1px;border:1px solid #999;background:#eee}.grid-properties-dialog>.dialog-actions{margin-top:auto}
</style>
