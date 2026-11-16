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
type ShiftFormMode = 'add' | 'edit' | 'copy'
type InfoMode = 'state' | 'history' | null

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mockMode = computed(() => import.meta.env.DEV && props.developmentPreview)
const areasKey = 'ymes-dev-mock:smd-area-show:areas:v1'
const layoutsKey = 'ymes-dev-mock:smd-area-show:layouts:v1'
const shiftsKey = 'ymes-dev-mock:smd-area-show:shifts:v1'
const operatorsKey = 'ymes-dev-mock:smd-area-show:operators:v1'
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

const areas = ref<AreaRow[]>([])
const activeArea = ref<AreaRow | null>(null)
const shiftArea = ref<AreaRow | null>(null)
const nodes = ref<LayoutNode[]>([])
const zoom = ref(100)
const gridVisible = ref(true)
const loading = ref(false)
const errorMessage = ref('')
const areaPickerOpen = ref(false)
const areaSearch = ref('')
const contextMenu = ref<{ x: number; y: number; node: LayoutNode } | null>(null)
const infoMode = ref<InfoMode>(null)
const infoNode = ref<LayoutNode | null>(null)
const infoRows = ref<Record<string, string>[]>([])

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
    if (response.status === 404) throw new Error('區域配置或班別 API 尚未建立；請依 todolist_homepc.md 完成 SMD SD108。')
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
  if (!areaNode(node)) return
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
  if (mockMode.value) {
    infoRows.value = mode === 'state'
      ? [{ 使用者: 'DEV001', 姓名: '開發人員甲', 登入時間: '2026/11/16 08:00:00', 狀態: '作業中' }]
      : [{ 使用者: 'DEV001', 姓名: '開發人員甲', 登入時間: '2026/11/16 08:00:00', 登出時間: '2026/11/16 17:00:00' }, { 使用者: 'DEV002', 姓名: '開發人員乙', 登入時間: '2026/11/15 20:00:00', 登出時間: '2026/11/16 08:00:00' }]
    return
  }
  try {
    infoRows.value = await requestJson<Record<string, string>[]>('api/wip/areas/' + encodeURIComponent(node.objectNo) + '/operator-' + (mode === 'state' ? 'state' : 'log'))
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '作業人員資訊載入失敗。' }
}

</script>

<template>
  <section class="area-show-page" @click="contextMenu = null">
    <header class="page-title"><h1>設備狀態監控_區域</h1></header>
    <div class="toolbar">
      <label class="base-select"><span>Base Area</span><input :value="activeArea ? activeArea.areaNo + '　' + activeArea.areaName : ''" readonly class="kd-input"><button class="win-btn picker" :disabled="loading" @click="loadBaseAreas">...</button></label>
      <span class="toolbar-spacer"></span>
      <label>縮放 <select v-model.number="zoom" class="plain-input"><option v-for="level in zoomLevels" :key="level" :value="level">{{ level }}%</option></select></label>
      <button class="win-btn" :class="{ toggled: gridVisible }" @click="gridVisible = !gridVisible">網格線</button>
    </div>
    <p v-if="errorMessage" class="message error">{{ errorMessage }}</p>
    <main class="canvas-frame" :class="{ 'with-grid': gridVisible }">
      <div v-if="!activeArea && !loading" class="canvas-empty">請選擇 Base Area 載入區域配置</div>
      <div v-else-if="loading" class="canvas-empty">載入中...</div>
      <svg v-else :width="984 * zoom / 100" :height="469 * zoom / 100" viewBox="0 0 984 469" class="layout-canvas" role="img" aria-label="區域配置圖">
        <g v-for="node in nodes" :key="node.objectType + ':' + node.objectNo" class="layout-node" :class="nodeClass(node)" @click.stop="contextMenu = null" @contextmenu.prevent.stop="showContext($event, node)" @dblclick.stop="areaNode(node) && openShiftFor(node)">
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
        <button @click="openShiftFor(contextMenu.node)">班別設定</button>
        <button @click="openInfo('state', contextMenu.node)">目前作業人員</button>
        <button @click="openInfo('history', contextMenu.node)">歷史紀錄</button>
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
      <section class="dialog info-dialog">
        <header class="dialog-title"><h2>{{ infoMode === 'state' ? '目前作業人員' : '作業歷史紀錄' }}｜{{ infoNode.objectText }}</h2><button class="x-button" @click="infoMode = null">×</button></header>
        <div class="table-wrap"><table><thead><tr><th v-for="key in (infoRows[0] ? Object.keys(infoRows[0]) : [])" :key="key">{{ key }}</th></tr></thead><tbody><tr v-for="(row, index) in infoRows" :key="index"><td v-for="value in Object.values(row)" :key="value">{{ value }}</td></tr><tr v-if="!infoRows.length"><td class="empty">目前沒有資料</td></tr></tbody></table></div>
        <footer class="dialog-actions"><button class="win-btn" @click="infoMode = null">關閉</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.area-show-page{height:100%;min-height:0;display:flex;flex-direction:column;background:#fff;color:#292929;font:14px "Microsoft JhengHei",Arial,sans-serif}.page-title{height:50px;flex:none;display:flex;align-items:center;padding:0 24px;border-bottom:1px solid #ddd8ce;background:#f7f5ef}.page-title h1{margin:0;font-size:20px;font-weight:500}.toolbar{flex:none;min-height:42px;display:flex;align-items:center;gap:10px;padding:5px 12px;border-bottom:1px solid #ccc}.base-select{display:flex;align-items:center;gap:7px;min-width:320px}.base-select .kd-input{width:340px}.toolbar-spacer{flex:1}.toolbar label:not(.base-select){display:flex;align-items:center;gap:5px}.kd-input,.plain-input{box-sizing:border-box;height:27px;border:1px solid #999;border-radius:0;padding:3px 6px;font:inherit;color:#222}.kd-input{background:#ffffc0}.kd-input:read-only,.kd-input:disabled{background:#eee}.plain-input{background:#fff}.win-btn{min-height:29px;padding:3px 10px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#f1efeb);font:inherit;white-space:nowrap;cursor:pointer}.win-btn:hover:not(:disabled){border-color:#e39a37;background:#fff8e8}.win-btn:disabled{opacity:.5;cursor:default}.win-btn.primary{border-color:#e69520;background:#f7a52a;color:#fff;font-weight:600}.win-btn.danger{color:#8b3030}.win-btn.toggled{background:#fff1c2;border-color:#d4a13b}.win-btn.picker{min-width:32px}.message{margin:5px 12px;padding:6px 9px;border:1px solid #ddd}.message.error{color:#8d2222;background:#fff3f1}.canvas-frame{position:relative;flex:1;min-height:0;margin:8px 12px;border:1px solid #aaa;overflow:auto;background:#fff}.canvas-frame.with-grid{background-image:linear-gradient(#e9e7e2 1px,transparent 1px),linear-gradient(90deg,#e9e7e2 1px,transparent 1px);background-size:20px 20px}.layout-canvas{display:block;min-width:984px;min-height:469px}.layout-node{cursor:default}.layout-node.node-0{cursor:pointer}.layout-node:hover>rect,.layout-node:hover>ellipse,.layout-node:hover>polygon{stroke:#a36f25;stroke-width:2}.layout-node text{fill:#222;font-size:13px;pointer-events:none}.layout-node .node-code{fill:#555;font-size:10px}.canvas-empty{height:100%;min-height:240px;display:grid;place-items:center;color:#777}.canvas-placeholder{fill:#777;font-size:16px}.status-line{height:34px;flex:none;display:flex;align-items:center;justify-content:space-between;padding:0 12px;border-top:1px solid #ccc;color:#666;font-size:12px}.status-line .footer-close{min-height:24px;padding:1px 12px}.backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:10px;background:#0005}.backdrop.nested{z-index:1020}.dialog{box-sizing:border-box;display:flex;flex-direction:column;max-height:96vh;border:1px solid #999;background:#f9f8f4;box-shadow:0 8px 28px #0004}.dialog-title{height:37px;flex:none;display:flex;align-items:center;justify-content:space-between;padding:0 10px;border-bottom:1px solid #ccc;background:#f1efe9}.dialog-title h2{margin:0;font-size:15px;font-weight:500}.x-button{border:0;background:transparent;color:#555;font-size:24px;line-height:1;cursor:pointer}.area-dialog{width:min(660px,96vw);height:min(490px,92vh)}.picker-search{flex:none;padding:9px 12px}.picker-search label{display:flex;align-items:center;gap:8px}.picker-search input{flex:1}.table-wrap{min-height:0;overflow:auto;border:1px solid #aaa;background:white}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}th,td{height:25px;padding:3px 7px;border:1px solid #c9c9c9;text-align:left}th{position:sticky;top:0;background:#e7e5df;font-weight:500;z-index:1}tbody tr{cursor:pointer}tbody tr:hover{background:#fff4d1}.empty{text-align:center;color:#777;padding:20px}.context-menu{position:fixed;z-index:1100;display:flex;flex-direction:column;min-width:150px;padding:4px;border:1px solid #999;background:#fff;box-shadow:0 3px 12px #0004}.context-menu button{border:0;padding:7px 10px;text-align:left;background:#fff;font:inherit;cursor:pointer}.context-menu button:hover{background:#fff1c2}.shift-dialog{width:min(1008px,98vw);height:min(591px,95vh);padding:0 9px 5px}.shift-dialog>.dialog-title{margin:0 -9px}.area-fields{height:38px;flex:none;display:flex;align-items:center;gap:24px}.area-fields label{display:grid;grid-template-columns:75px 170px;align-items:center;gap:6px}.area-fields input,.audit-fields input{height:22px;box-sizing:border-box;border:1px solid #aaa;background:#eee;padding:2px 5px}.shift-master{height:235px;flex:none;display:grid;grid-template-columns:minmax(0,1fr) 88px;gap:8px}.shift-master .table-wrap{height:100%}.shift-master tbody tr.active{background:#fff0c4;outline:1px solid #d5a13d;outline-offset:-1px}.check-col{width:28px;text-align:center}.check-col input{accent-color:#e7a137}.shift-actions{display:flex;flex-direction:column;gap:6px}.operators{position:relative;flex:1;min-height:150px;display:grid;grid-template-columns:minmax(0,1fr) 54px minmax(0,1fr) 76px;align-items:stretch;gap:8px;margin:7px 0 3px;padding:8px;border:1px solid #aaa}.operators legend,.shift-fields legend,.audit-fields legend{padding:0 5px;color:#555}.operator-pane{min-width:0;display:flex;flex-direction:column;gap:5px}.operator-pane strong{font-weight:500}.operator-list{min-height:0;flex:1;overflow:auto;border:1px solid #aaa;background:white;display:flex;flex-direction:column}.operator-list button{border:0;border-bottom:1px solid #ddd;padding:5px 7px;text-align:left;background:#fff;font:inherit;cursor:pointer}.operator-list button.checked{background:#fff0c4}.move-actions{display:flex;flex-direction:column;align-items:center;justify-content:center;gap:8px}.move-actions button{min-width:42px}.save-operators{align-self:center;padding-inline:8px}.dialog-actions{display:flex;justify-content:flex-end;gap:7px;padding:5px 0 2px}.shift-editor{width:min(492px,96vw);height:min(326px,94vh);padding:0 10px}.shift-editor>.dialog-title{margin:0 -10px}.shift-fields{flex:none;height:180px;display:grid;grid-template-columns:1fr 1fr;gap:5px 10px;margin:7px 0 4px;padding:7px;border:1px solid #aaa}.shift-fields .field-row{display:grid;grid-template-columns:85px minmax(0,1fr);align-items:center;gap:5px}.field-row>span{text-align:right}.shift-fields input,.shift-fields select{min-width:0;width:100%}.description-row{grid-column:1 / -1;align-items:start}.description-row textarea{height:40px;resize:none}.audit-fields{flex:none;height:53px;display:grid;grid-template-columns:1fr 1fr;gap:4px 8px;margin:0;padding:4px 7px;border:1px solid #aaa}.audit-fields label{display:grid;grid-template-columns:48px minmax(0,1fr);align-items:center;gap:4px;font-size:10px;white-space:nowrap}.audit-fields input{min-width:0;width:100%;font-size:10px}.shift-editor .dialog-actions{margin-top:auto}.shift-editor .message{margin:2px 0}.info-dialog{width:min(760px,96vw);height:min(420px,90vh);padding:0 10px 5px}.info-dialog>.dialog-title{margin:0 -10px}.info-dialog .table-wrap{margin-top:10px}.context-menu,.dialog button,.toolbar button{user-select:none}@media(max-width:760px){.base-select{min-width:0;flex:1}.base-select .kd-input{min-width:80px;width:100%}.toolbar{flex-wrap:wrap}.shift-dialog{overflow:auto;height:95vh}.shift-master{height:210px;grid-template-columns:minmax(0,1fr) 75px}.operators{grid-template-columns:minmax(0,1fr) 40px minmax(0,1fr);min-height:300px}.save-operators{grid-column:1/-1}.shift-fields{height:auto;grid-template-columns:1fr}.description-row{grid-column:auto}.shift-editor{height:auto;min-height:326px}.area-fields{gap:8px}.area-fields label{grid-template-columns:65px minmax(90px,1fr)}}
</style>
