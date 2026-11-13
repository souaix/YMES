<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import EquipmentPropertyMaster from './EquipmentPropertyMaster.vue'
import EquipmentParameterMaster from './EquipmentParameterMaster.vue'
import EquipmentQCRelationMaster from './EquipmentQCRelationMaster.vue'
import EquipmentDataImport from './EquipmentDataImport.vue'

interface EquipmentClassOption { value: string; code: string; label: string }
const equipmentClassOptions: EquipmentClassOption[] = [
  { value: '0', code: 'GENERAL', label: '一般設備' }, { value: '1', code: 'VIRTUAL', label: '虛擬設備' },
  { value: '3', code: 'TESTER', label: '測試設備' }, { value: '4', code: 'HANDLER', label: '搬送設備' },
  { value: '5', code: 'PROBER', label: '探針設備' }, { value: '6', code: 'MOCVD', label: 'MOCVD' },
]

interface Equipment {
  equipmentNo: string
  equipmentType: string
  equipmentName: string
  engineerGroupNo: string
  vendorNo: string
  modelNo: string
  assetNo: string
  equipmentClass: string
  equipmentClassNo: string
  description: string
  capacity: number
  loadPort: number
  autoFlag: number
  eaController: string
  eqpRecipe: number
  qcListNo: string
  maxTime: number | null
  fixEqpTime: number | null
  varEqpTime: number | null
  countEqpUnitQty: number
  countEquipmentUnitQty: number
  queueServer: string
  requestMsgQueue: string
  responseMsgQueue: string
  requestMsgQueueMes: string
  responseMsgQueueEap: string
  chamberFlag: boolean
  belongEquipmentNo: string | null
  issueState: number
  dataStamp: number
  creator?: string | null
  createdAt: string | null
  reviser?: string | null
  revisedAt: string | null
}
type EquipmentWrite = Omit<Equipment, 'chamberFlag' | 'belongEquipmentNo' | 'issueState' | 'createdAt' | 'revisedAt' | 'creator' | 'reviser'>

const props = defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>()
const mock = import.meta.env.DEV
const mockKeys = { equipment: 'ymes-dev-mock:equipment:v1', chambers: 'ymes-dev-mock:equipment-chambers:v1' }
const seedEquipment: Equipment[] = [
  { equipmentNo: 'DEV-1AP-01', equipmentType: 'DEV-EQP-LATHE', equipmentName: '開發用超純水防靜電裝置', engineerGroupNo: 'DEV-EG-01', vendorNo: 'DEV-VENDOR-01', modelNo: 'DEV-MODEL-A', assetNo: 'DEV-ASSET-001', equipmentClass: 'GENERAL', equipmentClassNo: '0', description: 'DEV 假資料：設備基本資料範例', capacity: -1, loadPort: -1, autoFlag: 0, eaController: 'N/A', eqpRecipe: 0, qcListNo: 'DEV-EQP-QC-01', maxTime: 2.5, fixEqpTime: 0, varEqpTime: 0, countEqpUnitQty: 1, countEquipmentUnitQty: 1, queueServer: '', requestMsgQueue: '', responseMsgQueue: '', requestMsgQueueMes: '', responseMsgQueueEap: '', chamberFlag: false, belongEquipmentNo: null, issueState: 0, dataStamp: 0, creator: 'DEV001', createdAt: '2026-11-01T08:00:00+08:00', reviser: 'DEV002', revisedAt: '2026-11-05T09:30:00+08:00' },
  { equipmentNo: 'DEV-1AP-02', equipmentType: 'DEV-EQP-ROBOT', equipmentName: '開發用自動搬送設備', engineerGroupNo: 'DEV-EG-02', vendorNo: 'DEV-VENDOR-02', modelNo: 'DEV-MODEL-B', assetNo: 'DEV-ASSET-002', equipmentClass: 'HANDLER', equipmentClassNo: '4', description: 'DEV 假資料：自動設備範例', capacity: 8, loadPort: 2, autoFlag: 1, eaController: 'DEV-EAP-01', eqpRecipe: 2, qcListNo: 'DEV-EQP-QC-02', maxTime: 1, fixEqpTime: 2, varEqpTime: 0.5, countEqpUnitQty: 1, countEquipmentUnitQty: 1, queueServer: 'DEV-MQ-01', requestMsgQueue: 'DEV-EQP-REQ', responseMsgQueue: 'DEV-EQP-RES', requestMsgQueueMes: 'DEV-MES-REQ', responseMsgQueueEap: 'DEV-EAP-RES', chamberFlag: false, belongEquipmentNo: null, issueState: 2, dataStamp: 1, creator: 'DEV001', createdAt: '2026-10-15T09:00:00+08:00', reviser: 'DEV002', revisedAt: '2026-10-16T11:20:00+08:00' },
]
const seedChambers: Equipment[] = [
  { equipmentNo: 'DEV-1AP-01-C01', equipmentType: 'DEV-EQP-CHAMBER', equipmentName: '開發用腔體一', engineerGroupNo: 'DEV-EG-01', vendorNo: 'DEV-VENDOR-01', modelNo: '', assetNo: '', equipmentClass: 'CHAMBER', equipmentClassNo: '2', description: 'DEV 假資料 Chamber', capacity: -1, loadPort: -1, autoFlag: 0, eaController: 'N/A', eqpRecipe: 0, qcListNo: '', maxTime: null, fixEqpTime: 0, varEqpTime: 0, countEqpUnitQty: 1, countEquipmentUnitQty: 1, queueServer: '', requestMsgQueue: '', responseMsgQueue: '', requestMsgQueueMes: '', responseMsgQueueEap: '', chamberFlag: true, belongEquipmentNo: 'DEV-1AP-01', issueState: 0, dataStamp: 0, creator: 'DEV001', createdAt: '2026-11-01T08:00:00+08:00', reviser: null, revisedAt: null },
  { equipmentNo: 'DEV-1AP-01-C02', equipmentType: 'DEV-EQP-CHAMBER', equipmentName: '開發用腔體二', engineerGroupNo: 'DEV-EG-01', vendorNo: 'DEV-VENDOR-01', modelNo: '', assetNo: '', equipmentClass: 'CHAMBER', equipmentClassNo: '2', description: 'DEV 假資料 Chamber', capacity: -1, loadPort: -1, autoFlag: 0, eaController: 'N/A', eqpRecipe: 0, qcListNo: '', maxTime: null, fixEqpTime: 0, varEqpTime: 0, countEqpUnitQty: 1, countEquipmentUnitQty: 1, queueServer: '', requestMsgQueue: '', responseMsgQueue: '', requestMsgQueueMes: '', responseMsgQueueEap: '', chamberFlag: true, belongEquipmentNo: 'DEV-1AP-01', issueState: 2, dataStamp: 1, creator: 'DEV001', createdAt: '2026-10-01T08:00:00+08:00', reviser: 'DEV002', revisedAt: '2026-10-03T08:00:00+08:00' },
]
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const equipment = ref<Equipment[]>([])
const chambers = ref<Equipment[]>([])
const selectedEquipmentNo = ref('')
const issueState = ref(0)
const keyword = ref('')
const loading = ref(false)
const chamberLoading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const chamberError = ref('')
const equipmentFormMode = ref<'new' | 'edit' | 'copy' | null>(null)
const equipmentForm = ref<EquipmentWrite>({} as EquipmentWrite)
const propertyWindowOpen = ref(false)
const parameterWindowOpen = ref(false)
const qcRelationWindowOpen = ref(false)
const importWindowOpen = ref(false)
const selected = computed(() => equipment.value.find((item) => item.equipmentNo === selectedEquipmentNo.value) ?? null)
const equipmentDirty = ref(false)
const equipmentAudit = ref<{ creator: string; createdAt: string | null; reviser: string; revisedAt: string | null }>({ creator: '', createdAt: null, reviser: '', revisedAt: null })
const defaultQueueServer = ref(false)
const chamberIssueState = ref(0)
const chamberFormMode = ref<'new' | 'edit' | null>(null)
const chamberForm = ref({ equipmentNo: '', belongEquipmentNo: '', equipmentName: '', description: '', dataStamp: 0 })
let initialSelectedEquipmentNo = ''
let chamberRequestId = 0

const visibleChambers = computed(() => chambers.value.filter((item) => item.issueState === chamberIssueState.value))
function readMock<T>(key: string, seed: T): T {
  try { const raw = localStorage.getItem(key); return raw ? JSON.parse(raw) as T : structuredClone(seed) }
  catch { return structuredClone(seed) }
}
function writeMock<T>(key: string, value: T): void { localStorage.setItem(key, JSON.stringify(value)) }
function filteredMockEquipment(): Equipment[] {
  const search = keyword.value.trim().toLocaleLowerCase()
  return readMock<Equipment[]>(mockKeys.equipment, seedEquipment).filter((item) => !item.chamberFlag && item.issueState === issueState.value && (!search || item.equipmentNo.toLocaleLowerCase().includes(search)))
}

function toEquipmentWrite(item: Equipment, equipmentNo = item.equipmentNo, dataStamp = item.dataStamp): EquipmentWrite {
  return {
    equipmentNo, equipmentType: item.equipmentType, equipmentName: item.equipmentName,
    engineerGroupNo: item.engineerGroupNo, vendorNo: item.vendorNo, modelNo: item.modelNo,
    assetNo: item.assetNo, equipmentClass: item.equipmentClass, equipmentClassNo: item.equipmentClassNo,
    description: item.description, capacity: item.capacity, loadPort: item.loadPort, autoFlag: item.autoFlag,
    eaController: item.eaController, eqpRecipe: item.eqpRecipe, qcListNo: item.qcListNo,
    maxTime: item.maxTime, fixEqpTime: item.fixEqpTime, varEqpTime: item.varEqpTime,
    countEqpUnitQty: item.countEqpUnitQty, countEquipmentUnitQty: item.countEquipmentUnitQty,
    queueServer: item.queueServer, requestMsgQueue: item.requestMsgQueue, responseMsgQueue: item.responseMsgQueue,
    requestMsgQueueMes: item.requestMsgQueueMes, responseMsgQueueEap: item.responseMsgQueueEap, dataStamp,
  }
}
function markEquipmentDirty(): void { equipmentDirty.value = true }
function clearQueueFields(): void {
  equipmentForm.value.queueServer = ''
  equipmentForm.value.requestMsgQueue = ''
  equipmentForm.value.responseMsgQueue = ''
  equipmentForm.value.requestMsgQueueMes = ''
  equipmentForm.value.responseMsgQueueEap = ''
}
function changeAutoFlag(): void {
  markEquipmentDirty()
  defaultQueueServer.value = equipmentForm.value.autoFlag === 1
  if (defaultQueueServer.value) clearQueueFields()
}
function changeDefaultQueueServer(): void {
  markEquipmentDirty()
  if (defaultQueueServer.value) clearQueueFields()
}
function closeEquipmentForm(): void { equipmentFormMode.value = null; equipmentDirty.value = false }
function auditDate(value: string | null): string {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.valueOf()) ? value : date.toLocaleString('zh-TW')
}
function validateEquipmentForm(): string {
  if (!equipmentForm.value.equipmentNo.trim()) return '設備編號為必填。'
  if (!equipmentForm.value.equipmentType.trim()) return '設備類別資料為必填。'
  if (!equipmentForm.value.equipmentName.trim()) return '設備名稱為必填。'
  if (!equipmentForm.value.engineerGroupNo.trim()) return '管理群組為必填。'
  if (!equipmentForm.value.vendorNo.trim()) return '供應商編號為必填。'
  if (!Number.isInteger(equipmentForm.value.capacity) || equipmentForm.value.capacity < -1) return '批量限制必須為大於或等於 -1 的整數。'
  if (!Number.isInteger(equipmentForm.value.loadPort) || equipmentForm.value.loadPort < -1) return '批數限制必須為大於或等於 -1 的整數。'
  if (equipmentForm.value.maxTime !== null && (equipmentForm.value.maxTime < 0 || equipmentForm.value.maxTime > 24)) return '預設產能需介於 0 到 24 小時。'
  if ((equipmentForm.value.fixEqpTime ?? 0) < 0 || (equipmentForm.value.varEqpTime ?? 0) < 0) return '固定機時與變動機時不可小於 0。'
  if (equipmentForm.value.countEqpUnitQty < 0) return '機時單位批量不可小於 0。'
  if (equipmentForm.value.autoFlag === 1 && !defaultQueueServer.value && !equipmentForm.value.queueServer.trim()) return '未使用預設伺服器時，佇列伺服器為必填。'
  return ''
}
function makeApiUrl(path: string): string {
  const configuredBase = props.apiBaseUrl.trim()
  if (!configuredBase) throw new Error('目前環境尚未設定 API 位址。')
  const base = new URL(configuredBase, window.location.origin)
  const baseWithSlash = base.toString().replace(/\/+$/, '') + '/'
  return new URL(path.replace(/^\/+/, ''), baseWithSlash).toString()
}

async function requestJson<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(makeApiUrl(path), {
    credentials: 'include',
    ...init,
  })

  if (response.status === 401) {
    window.location.assign(appBase + '/')
    throw new Error('登入狀態已逾期，請重新登入。')
  }

  if (!response.ok) {
    const problem = await response.json().catch(() => null) as {
      title?: string
      detail?: string
      message?: string
    } | null
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 回應錯誤 (${response.status})`)
  }

  if (response.status === 204) return undefined as T
  return await response.json() as T
}

function updateFilterAddress(): void {
  const url = new URL(window.location.href)
  url.search = ''
  url.searchParams.set('issueState', String(issueState.value))
  if (keyword.value.trim()) url.searchParams.set('keyword', keyword.value.trim())
  window.history.replaceState(window.history.state, '', url.pathname + url.search)
}

function newEquipmentValues(): EquipmentWrite {
  return {
    equipmentNo: '', equipmentType: '', equipmentName: '', engineerGroupNo: '', vendorNo: '', modelNo: '', assetNo: '',
    equipmentClass: 'GENERAL', equipmentClassNo: '0', description: '', capacity: -1, loadPort: -1, autoFlag: 0,
    eaController: 'N/A', eqpRecipe: 0, qcListNo: '', maxTime: null, fixEqpTime: 0, varEqpTime: 0,
    countEqpUnitQty: 1, countEquipmentUnitQty: 1, queueServer: '', requestMsgQueue: '', responseMsgQueue: '',
    requestMsgQueueMes: '', responseMsgQueueEap: '', dataStamp: 0,
  }
}

function saveEquipmentMock(mode: 'new' | 'edit' | 'copy'): Equipment {
  const rows = readMock<Equipment[]>(mockKeys.equipment, seedEquipment)
  const number = equipmentForm.value.equipmentNo.trim().toUpperCase()
  const index = rows.findIndex((item) => item.equipmentNo === number)
  if (mode !== 'edit' && index >= 0) throw new Error(`設備編號 ${number} 已存在。`)
  if (mode === 'edit' && index < 0) throw new Error(`找不到設備 ${number}。`)
  const now = new Date().toISOString()
  const previous = index >= 0 ? rows[index] : null
  const saved: Equipment = {
    ...equipmentForm.value, equipmentNo: number, description: equipmentForm.value.description || '',
    chamberFlag: false, belongEquipmentNo: null, issueState: 0,
    dataStamp: (previous?.dataStamp ?? -1) + 1,
    creator: previous?.creator || props.currentUserNo || 'DEV001',
    createdAt: previous?.createdAt || now,
    reviser: previous ? (props.currentUserNo || 'DEV001') : null,
    revisedAt: previous ? now : null,
  }
  if (index >= 0) rows[index] = saved
  else rows.push(saved)
  writeMock(mockKeys.equipment, rows)
  return saved
}

function openEquipmentForm(mode: 'new' | 'edit' | 'copy'): void {
  const item = selected.value
  equipmentFormMode.value = mode
  if (mode === 'new' || !item) {
    equipmentForm.value = newEquipmentValues()
    const now = new Date().toISOString()
    equipmentAudit.value = { creator: props.currentUserNo || (props.developmentPreview ? 'DEV001' : ''), createdAt: now, reviser: '', revisedAt: null }
    defaultQueueServer.value = false
  } else {
    equipmentForm.value = toEquipmentWrite(item, mode === 'copy' ? '' : item.equipmentNo, mode === 'copy' ? 0 : item.dataStamp)
    const now = new Date().toISOString()
    equipmentAudit.value = mode === 'edit'
      ? { creator: item.creator || '', createdAt: item.createdAt, reviser: props.currentUserNo || '', revisedAt: now }
      : { creator: props.currentUserNo || (props.developmentPreview ? 'DEV001' : ''), createdAt: now, reviser: '', revisedAt: null }
    defaultQueueServer.value = item.autoFlag === 1 && !item.queueServer
  }
  equipmentDirty.value = false
  errorMessage.value = ''
}

function openProperties(): void {
  if (!selected.value) return
  propertyWindowOpen.value = true
}
function openParameters(): void {
  if (!selected.value) return
  parameterWindowOpen.value = true
}
function openQcRelations(): void {
  if (!selected.value) return
  qcRelationWindowOpen.value = true
}
function openEquipmentImport(): void {
  if (mock) {
    if (!localStorage.getItem(mockKeys.equipment)) writeMock(mockKeys.equipment, seedEquipment)
    if (!localStorage.getItem(mockKeys.chambers)) writeMock(mockKeys.chambers, seedChambers)
  }
  importWindowOpen.value = true
}

function syncEquipmentClass(): void {
  const option = equipmentClassOptions.find((item) => item.value === equipmentForm.value.equipmentClassNo)
  if (option) equipmentForm.value.equipmentClass = option.code
  markEquipmentDirty()
}

function openChamberForm(mode: 'new' | 'edit', chamber?: Equipment): void {
  const parent = selected.value
  if (!parent) return
  chamberFormMode.value = mode
  chamberForm.value = mode === 'new'
    ? { equipmentNo: '', belongEquipmentNo: parent.equipmentNo, equipmentName: '', description: '', dataStamp: 0 }
    : { equipmentNo: chamber?.equipmentNo ?? '', belongEquipmentNo: parent.equipmentNo, equipmentName: chamber?.equipmentName ?? '', description: chamber?.description ?? '', dataStamp: chamber?.dataStamp ?? 0 }
}

async function saveEquipment(keepOpen = false): Promise<void> {
  const saveMode = equipmentFormMode.value
  if (!saveMode || busy.value) return
  const validation = validateEquipmentForm()
  if (validation) { errorMessage.value = validation; return }
  const isEdit = saveMode === 'edit'
  if (defaultQueueServer.value) clearQueueFields()
  busy.value = true; errorMessage.value = ''
  try {
    const item = mock
      ? saveEquipmentMock(saveMode)
      : await requestJson<Equipment>(isEdit ? `api/equipment/${encodeURIComponent(String(equipmentForm.value.equipmentNo))}` : 'api/equipment', {
        method: isEdit ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(equipmentForm.value),
      })
    selectedEquipmentNo.value = item.equipmentNo
    if (keepOpen) {
      equipmentFormMode.value = 'edit'
      equipmentForm.value = toEquipmentWrite(item)
      equipmentAudit.value = { creator: item.creator || equipmentAudit.value.creator, createdAt: item.createdAt || equipmentAudit.value.createdAt, reviser: item.reviser || props.currentUserNo || '', revisedAt: item.revisedAt || new Date().toISOString() }
      equipmentDirty.value = false
    } else closeEquipmentForm()
    await loadEquipment()
    successMessage.value = `設備 ${item.equipmentNo} 已${isEdit ? '更新' : saveMode === 'copy' ? '複製' : '建立'}。`
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '設備資料儲存失敗。' }
  finally { busy.value = false }
}

async function saveChamber(): Promise<void> {
  const parent = selected.value
  if (!parent) return
  const isEdit = chamberFormMode.value === 'edit'
  busy.value = true; chamberError.value = ''
  try {
    if (mock) {
      const all = readMock<Equipment[]>(mockKeys.chambers, seedChambers)
      const no = chamberForm.value.equipmentNo.trim().toUpperCase()
      const index = all.findIndex((item) => item.equipmentNo === no)
      if (isEdit && index < 0) throw new Error(`找不到 Chamber ${no}。`)
      if (!isEdit && index >= 0) throw new Error(`Chamber 編號 ${no} 已存在。`)
      const old = index >= 0 ? all[index] : null
      const now = new Date().toISOString()
      const row: Equipment = { ...seedChambers[0], ...chamberForm.value, equipmentNo: no, belongEquipmentNo: parent.equipmentNo, chamberFlag: true, issueState: 0, dataStamp: (old?.dataStamp ?? -1) + 1, creator: old?.creator || props.currentUserNo || 'DEV001', createdAt: old?.createdAt || now, reviser: old ? props.currentUserNo || 'DEV001' : null, revisedAt: old ? now : null }
      if (index >= 0) all[index] = row
      else all.push(row)
      writeMock(mockKeys.chambers, all)
    } else {
      const root = `api/equipment/${encodeURIComponent(parent.equipmentNo)}/chambers`
      const endpoint = isEdit ? `${root}/${encodeURIComponent(chamberForm.value.equipmentNo)}` : root
      await requestJson<Equipment>(endpoint, { method: isEdit ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(chamberForm.value) })
    }
    chamberFormMode.value = null
    successMessage.value = `Chamber ${chamberForm.value.equipmentNo} 已${isEdit ? '更新' : '建立'}。`
    await loadChambers(parent.equipmentNo)
  } catch (error) { chamberError.value = error instanceof Error ? error.message : 'Chamber 資料儲存失敗。' }
  finally { busy.value = false }
}

async function loadChambers(equipmentNo: string): Promise<void> {
  const requestId = ++chamberRequestId
  chamberError.value = ''
  chambers.value = []
  if (!equipmentNo) return

  chamberLoading.value = true
  try {
    const result = mock
      ? readMock<Equipment[]>(mockKeys.chambers, seedChambers).filter((item) => item.belongEquipmentNo === equipmentNo)
      : await requestJson<Equipment[]>(`api/equipment/${encodeURIComponent(equipmentNo)}/chambers`)
    if (requestId === chamberRequestId) chambers.value = result
  } catch (error) {
    if (requestId === chamberRequestId) {
      chamberError.value = error instanceof Error ? error.message : 'Chamber 資料載入失敗。'
    }
  } finally {
    if (requestId === chamberRequestId) chamberLoading.value = false
  }
}

async function loadEquipment(): Promise<void> {
  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''
  try {
    if (mock) {
      const rows = filteredMockEquipment()
      equipment.value = rows
      const previous = selectedEquipmentNo.value
      const requested = initialSelectedEquipmentNo
      const next = rows.find((item) => item.equipmentNo === previous) ?? rows.find((item) => item.equipmentNo === requested) ?? rows[0]
      selectedEquipmentNo.value = next?.equipmentNo ?? ''
      initialSelectedEquipmentNo = ''
      await loadChambers(selectedEquipmentNo.value)
      updateFilterAddress()
      return
    }
    const query = new URLSearchParams({ issueState: String(issueState.value) })
    if (keyword.value.trim()) query.set('keyword', keyword.value.trim())
    const rows = await requestJson<Equipment[]>(`api/equipment?${query.toString()}`)
    equipment.value = rows

    const previous = selectedEquipmentNo.value
    const requested = initialSelectedEquipmentNo
    const next = rows.find((item) => item.equipmentNo === previous)
      ?? rows.find((item) => item.equipmentNo === requested)
      ?? rows[0]
    selectedEquipmentNo.value = next?.equipmentNo ?? ''
    initialSelectedEquipmentNo = ''
    await loadChambers(selectedEquipmentNo.value)
    updateFilterAddress()
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '設備資料載入失敗。'
  } finally {
    loading.value = false
  }
}

async function selectEquipment(item: Equipment): Promise<void> {
  if (selectedEquipmentNo.value === item.equipmentNo) return
  selectedEquipmentNo.value = item.equipmentNo
  successMessage.value = ''
  await loadChambers(item.equipmentNo)
}

async function approveSelected(): Promise<void> {
  const item = selected.value
  if (!item || busy.value) return

  busy.value = true
  errorMessage.value = ''
  try {
    if (mock) {
      const rows = readMock<Equipment[]>(mockKeys.equipment, seedEquipment)
      const index = rows.findIndex((row) => row.equipmentNo === item.equipmentNo)
      if (index < 0) throw new Error('找不到要核准的設備。')
      rows[index] = { ...rows[index], issueState: 2, dataStamp: rows[index].dataStamp + 1, reviser: props.currentUserNo || 'DEV001', revisedAt: new Date().toISOString() }
      writeMock(mockKeys.equipment, rows)
    } else await requestJson<Equipment>(`api/equipment/${encodeURIComponent(item.equipmentNo)}/approve`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ dataStamp: item.dataStamp }),
    })
    successMessage.value = `設備 ${item.equipmentNo} 已核准。`
    selectedEquipmentNo.value = ''
    await loadEquipment()
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '設備核准失敗。'
  } finally {
    busy.value = false
  }
}

async function deleteSelected(): Promise<void> {
  const item = selected.value
  if (!item || busy.value) return
  const action = item.issueState === 2 ? '停用' : '刪除'
  if (!window.confirm(`確定${action}設備 ${item.equipmentNo}？`)) return

  busy.value = true
  errorMessage.value = ''
  try {
    if (mock) {
      const rows = readMock<Equipment[]>(mockKeys.equipment, seedEquipment)
      const index = rows.findIndex((row) => row.equipmentNo === item.equipmentNo)
      if (index < 0) throw new Error('找不到要處理的設備。')
      if (item.issueState === 2) rows[index] = { ...rows[index], issueState: -1, dataStamp: rows[index].dataStamp + 1, reviser: props.currentUserNo || 'DEV001', revisedAt: new Date().toISOString() }
      else rows.splice(index, 1)
      writeMock(mockKeys.equipment, rows)
    } else {
      const query = new URLSearchParams({ issueState: String(item.issueState), dataStamp: String(item.dataStamp) })
      await requestJson<Equipment>(`api/equipment/${encodeURIComponent(item.equipmentNo)}?${query.toString()}`, { method: 'DELETE' })
    }
    successMessage.value = `設備 ${item.equipmentNo} 已${action}。`
    selectedEquipmentNo.value = ''
    await loadEquipment()
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : `設備${action}失敗。`
  } finally {
    busy.value = false
  }
}

async function approveChamber(chamber: Equipment): Promise<void> {
  const parent = selected.value
  if (!parent || busy.value) return
  busy.value = true
  chamberError.value = ''
  try {
    if (mock) {
      const rows = readMock<Equipment[]>(mockKeys.chambers, seedChambers)
      const index = rows.findIndex((row) => row.equipmentNo === chamber.equipmentNo)
      if (index < 0) throw new Error('找不到要核准的 Chamber。')
      rows[index] = { ...rows[index], issueState: 2, dataStamp: rows[index].dataStamp + 1, reviser: props.currentUserNo || 'DEV001', revisedAt: new Date().toISOString() }
      writeMock(mockKeys.chambers, rows)
    } else await requestJson<Equipment>(`api/equipment/${encodeURIComponent(parent.equipmentNo)}/chambers/${encodeURIComponent(chamber.equipmentNo)}/approve`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ dataStamp: chamber.dataStamp }),
    })
    successMessage.value = `Chamber ${chamber.equipmentNo} 已核准。`
    await loadChambers(parent.equipmentNo)
  } catch (error) {
    chamberError.value = error instanceof Error ? error.message : 'Chamber 核准失敗。'
  } finally {
    busy.value = false
  }
}

async function deleteChamber(chamber: Equipment): Promise<void> {
  const parent = selected.value
  if (!parent || busy.value) return
  const action = chamber.issueState === 2 ? '停用' : '刪除'
  if (!window.confirm(`確定${action} Chamber ${chamber.equipmentNo}？`)) return

  busy.value = true
  chamberError.value = ''
  try {
    if (mock) {
      const rows = readMock<Equipment[]>(mockKeys.chambers, seedChambers)
      const index = rows.findIndex((row) => row.equipmentNo === chamber.equipmentNo)
      if (index < 0) throw new Error('找不到要處理的 Chamber。')
      if (chamber.issueState === 2) rows[index] = { ...rows[index], issueState: -1, dataStamp: rows[index].dataStamp + 1, reviser: props.currentUserNo || 'DEV001', revisedAt: new Date().toISOString() }
      else rows.splice(index, 1)
      writeMock(mockKeys.chambers, rows)
    } else {
      const query = new URLSearchParams({ issueState: String(chamber.issueState), dataStamp: String(chamber.dataStamp) })
      await requestJson<Equipment>(`api/equipment/${encodeURIComponent(parent.equipmentNo)}/chambers/${encodeURIComponent(chamber.equipmentNo)}?${query.toString()}`, { method: 'DELETE' })
    }
    successMessage.value = `Chamber ${chamber.equipmentNo} 已${action}。`
    await loadChambers(parent.equipmentNo)
  } catch (error) {
    chamberError.value = error instanceof Error ? error.message : `Chamber ${action}失敗。`
  } finally {
    busy.value = false
  }
}

function stateName(state: number): string {
  return ({ 0: '未簽核', 1: '簽核中', 2: '已簽核', '-1': '不使用' } as Record<string, string>)[String(state)] ?? '未知'
}

function formatNumber(value: number | null | undefined): string {
  return value === null || value === undefined ? '' : Number(value).toLocaleString('zh-TW', { maximumFractionDigits: 3 })
}

function navigateHome(): void {
  window.history.pushState({}, '', appBase + '/')
  window.dispatchEvent(new PopStateEvent('popstate'))
}

function printPage(): void {
  window.print()
}

function downloadCsv(): void {
  const headers = ['設備編號', '設備類別資料', '設備名稱', '管理群組', '設備分類', '供應商編號', '設備型號', '財產編號', '批量限制', '批數限制', '預設產能', '機時單位批量', '固定機時', '變動機時', '點檢表編號', '自動設備', '說明']
  const rows = equipment.value.map((item) => [item.equipmentNo, item.equipmentType, item.equipmentName, item.engineerGroupNo, item.equipmentClass, item.vendorNo, item.modelNo, item.assetNo, item.capacity, item.loadPort, item.maxTime, item.countEqpUnitQty, item.fixEqpTime, item.varEqpTime, item.qcListNo, item.autoFlag === 1 ? '是' : '否', item.description])
  const csv = [headers, ...rows].map((row) => row.map((value) => `"${String(value ?? '').replaceAll('"', '""')}"`).join(',')).join('\r\n')
  const url = URL.createObjectURL(new Blob(['\uFEFF', csv], { type: 'text/csv;charset=utf-8' }))
  const link = document.createElement('a')
  link.href = url
  link.download = 'equipment.csv'
  link.click()
  URL.revokeObjectURL(url)
}

function handlePopState(): void {
  const params = new URLSearchParams(window.location.search)
  issueState.value = Number(params.get('issueState') ?? 0)
  keyword.value = params.get('keyword') ?? ''
  initialSelectedEquipmentNo = params.get('selectedEquipmentNo') ?? ''
  void loadEquipment()
}

onMounted(() => {
  const params = new URLSearchParams(window.location.search)
  issueState.value = Number(params.get('issueState') ?? 0)
  keyword.value = params.get('keyword') ?? ''
  initialSelectedEquipmentNo = params.get('selectedEquipmentNo') ?? ''
  window.addEventListener('popstate', handlePopState)
  void loadEquipment()
})

onBeforeUnmount(() => window.removeEventListener('popstate', handlePopState))
</script>

<template>
  <div class="equipment-master-page">
    <div v-if="mock" class="equipment-dev-note">Development 假資料｜操作結果僅保存在此瀏覽器，不會呼叫 API 或寫入資料庫。</div>
    <form class="equipment-query" @submit.prevent="loadEquipment">
      <fieldset class="equipment-state-filter">
        <legend>設備</legend>
        <label><input v-model.number="issueState" type="radio" name="issueState" :value="0" />未簽核</label>
        <label><input v-model.number="issueState" type="radio" name="issueState" :value="1" />簽核中</label>
        <label><input v-model.number="issueState" type="radio" name="issueState" :value="2" />已簽核</label>
        <label><input v-model.number="issueState" type="radio" name="issueState" :value="-1" />不使用</label>
      </fieldset>
      <span class="equipment-query-label">設備</span>
      <select aria-label="查詢欄位" disabled><option>設備編號</option></select>
      <input v-model="keyword" class="equipment-keyword" type="search" aria-label="設備編號" />
      <span class="equipment-like">%</span>
      <button class="equipment-query-button" type="submit" :disabled="loading">查詢</button>
    </form>

    <div v-if="errorMessage" class="equipment-message error" role="alert">{{ errorMessage }}</div>
    <div v-if="successMessage" class="equipment-message success" role="status">{{ successMessage }}</div>

    <section class="equipment-list-panel" :aria-busy="loading">
      <header class="equipment-panel-heading"><h2>設備</h2><span>設備基本資料</span></header>
      <div class="equipment-table-scroll">
        <table class="equipment-table">
          <thead><tr>
            <th>設備編號</th><th>設備類別資料</th><th>設備名稱</th><th>管理群組</th><th>設備分類</th><th>供應商編號</th><th>設備型號</th><th>財產編號</th><th>批量限制</th><th>批數限制</th><th>預設產能</th><th>機時單位批量</th><th>固定機時</th><th>變動機時</th><th>點檢表編號</th><th>自動設備</th><th>說明</th>
          </tr></thead>
          <tbody>
            <tr
              v-for="item in equipment"
              :key="item.equipmentNo"
              :class="{ 'selected-row': selectedEquipmentNo === item.equipmentNo }"
              :aria-selected="selectedEquipmentNo === item.equipmentNo"
              tabindex="0"
              @click="selectEquipment(item)"
              @keydown.enter.prevent="selectEquipment(item)"
              @keydown.space.prevent="selectEquipment(item)"
            >
              <td class="equipment-code">{{ item.equipmentNo }}</td><td>{{ item.equipmentType }}</td><td>{{ item.equipmentName }}</td><td>{{ item.engineerGroupNo }}</td><td>{{ item.equipmentClass }}</td><td>{{ item.vendorNo }}</td><td>{{ item.modelNo }}</td><td>{{ item.assetNo }}</td><td>{{ formatNumber(item.capacity) }}</td><td>{{ item.loadPort }}</td><td>{{ formatNumber(item.maxTime) }}</td><td>{{ item.countEqpUnitQty }}</td><td>{{ formatNumber(item.fixEqpTime) }}</td><td>{{ formatNumber(item.varEqpTime) }}</td><td>{{ item.qcListNo }}</td><td>{{ item.autoFlag === 1 ? '是' : '否' }}</td><td>{{ item.description }}</td>
            </tr>
            <tr v-if="!loading && equipment.length === 0"><td colspan="17" class="equipment-empty">沒有符合條件的設備資料</td></tr>
            <tr v-if="loading && equipment.length === 0"><td colspan="17" class="equipment-empty">設備資料載入中…</td></tr>
          </tbody>
        </table>
      </div>

      <aside class="equipment-actions" aria-label="設備主檔功能">
        <button class="equipment-action primary" type="button" @click="openEquipmentForm('new')">新增(A)</button>
        <button v-if="selected && selected.issueState === 0" class="equipment-action" type="button" @click="openEquipmentForm('edit')">修改(E)</button>
        <button v-if="selected && [0, 2, -1].includes(selected.issueState)" class="equipment-action danger" type="button" :disabled="busy" @click="deleteSelected">{{ selected.issueState === 2 ? '停用(D)' : '刪除(D)' }}</button>
        <button v-if="selected" class="equipment-action" type="button" @click="openEquipmentForm('copy')">複製(C)</button>
        <button v-if="selected && selected.issueState === 0" class="equipment-action" type="button" :disabled="busy" @click="approveSelected">核准</button>
        <button class="equipment-action" type="button" :disabled="!selected" @click="openProperties">屬性</button>
        <button class="equipment-action" type="button" :disabled="!selected" @click="openParameters">參數</button>
        <button class="equipment-action" type="button" :disabled="!selected" @click="openQcRelations">點檢表</button>
        <button class="equipment-action" type="button" @click="downloadCsv">Download</button>
        <button class="equipment-action" type="button" @click="printPage">Print</button>
        <button class="equipment-action" type="button" @click="openEquipmentImport">設備匯入</button>
        <button class="equipment-action" type="button" @click="navigateHome">關閉</button>
      </aside>
      <footer class="equipment-list-footer">資料總筆數 <strong>{{ equipment.length }}</strong></footer>
    </section>

    <details class="equipment-chamber-panel" open>
      <summary>設備附屬 Chamber 資料（{{ selected?.equipmentNo ?? '未選取設備' }}）</summary>
      <div v-if="selected" class="equipment-detail-panel">
        <div class="equipment-detail-header">
          <div><h2>{{ selected.equipmentNo }}　{{ selected.equipmentName }}</h2><p>Chamber 子設備</p></div>
          <button v-if="[0, 2].includes(selected.issueState)" class="equipment-small-action" type="button" @click="openChamberForm('new')">新增 Chamber</button>
        </div>
        <fieldset class="equipment-state-filter equipment-chamber-filter"><legend>Chamber 狀態</legend><label><input v-model.number="chamberIssueState" type="radio" name="chamberIssueState" :value="0" />UNFROZEN</label><label><input v-model.number="chamberIssueState" type="radio" name="chamberIssueState" :value="1" />PENDING</label><label><input v-model.number="chamberIssueState" type="radio" name="chamberIssueState" :value="2" />ACTIVE</label><label><input v-model.number="chamberIssueState" type="radio" name="chamberIssueState" :value="-1" />UNUSED</label></fieldset>
        <div v-if="chamberError" class="equipment-message error" role="alert">{{ chamberError }}</div>
        <div v-else-if="chamberLoading" class="equipment-detail-note">Chamber 資料載入中…</div>
        <div v-else-if="visibleChambers.length === 0" class="equipment-detail-note">此狀態沒有 Chamber 子設備。</div>
        <div v-else class="equipment-chamber-scroll">
          <table class="equipment-table equipment-chamber-table">
            <thead><tr><th>Chamber 編號</th><th>名稱</th><th>狀態</th><th>操作</th></tr></thead>
            <tbody>
              <tr v-for="chamber in visibleChambers" :key="chamber.equipmentNo">
                <td>{{ chamber.equipmentNo }}</td><td>{{ chamber.equipmentName }}</td><td>{{ stateName(chamber.issueState) }}</td>
                <td class="equipment-chamber-actions">
                  <template v-if="chamber.issueState === 0">
                    <button type="button" @click="openChamberForm('edit', chamber)">修改</button>
                    <button type="button" :disabled="busy" @click="approveChamber(chamber)">核准</button>
                  </template>
                  <button v-if="[0, 2, -1].includes(chamber.issueState)" type="button" :disabled="busy" @click="deleteChamber(chamber)">{{ chamber.issueState === 2 ? '停用' : '刪除' }}</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </details>

    <div v-if="equipmentFormMode" class="equipment-form-overlay" @click.self="closeEquipmentForm">
      <form class="equipment-win-form" @submit.prevent="saveEquipment(false)" @input="markEquipmentDirty" @change="markEquipmentDirty">
        <header><strong>設備基本資料定義 ({{ equipmentFormMode === 'edit' ? 'EDIT' : equipmentFormMode === 'copy' ? 'COPY' : 'ADD' }}) (EP104)</strong><button type="button" aria-label="關閉" @click="closeEquipmentForm">×</button></header>
        <p v-if="errorMessage" class="equipment-message error" role="alert">{{ errorMessage }}</p>
        <fieldset><legend>設備主檔資料</legend>
          <div class="equipment-form-columns">
            <div class="equipment-form-column">
              <label><span>設備編號</span><input v-model="equipmentForm.equipmentNo" required :readonly="equipmentFormMode === 'edit'" /></label>
              <label><span>設備名稱</span><input v-model="equipmentForm.equipmentName" required /></label>
              <label><span>設備分類</span><select class="equipment-kd-field" v-model="equipmentForm.equipmentClassNo" @change="syncEquipmentClass"><option v-for="option in equipmentClassOptions" :key="option.value" :value="option.value">{{ option.label }}</option></select></label>
              <label><span>設備型號</span><input v-model="equipmentForm.modelNo" /></label>
              <label><span>批量限制</span><input class="equipment-kd-field" v-model.number="equipmentForm.capacity" type="number" min="-1" step="1" /><small>(-1 代表不限制)</small></label>
              <label><span>預設產能</span><span class="equipment-number-unit"><input v-model.number="equipmentForm.maxTime" type="number" min="0" max="24" step="0.01" /><small>小時</small></span></label>
              <label><span>固定機時</span><span class="equipment-number-unit"><input v-model.number="equipmentForm.fixEqpTime" type="number" min="0" step="0.01" /><small>分鐘</small></span></label>
              <label><span>點檢表編號</span><input v-model="equipmentForm.qcListNo" /></label>
              <label class="equipment-long-field"><span>說明</span><textarea v-model="equipmentForm.description" rows="5" /></label>
            </div>
            <div class="equipment-form-column">
              <label><span>設備類別資料</span><input v-model="equipmentForm.equipmentType" required /></label>
              <label><span>管理群組</span><input v-model="equipmentForm.engineerGroupNo" required /></label>
              <label><span>供應商編號</span><input v-model="equipmentForm.vendorNo" required /></label>
              <label><span>財產編號</span><input v-model="equipmentForm.assetNo" /></label>
              <label><span>批數限制</span><input class="equipment-kd-field" v-model.number="equipmentForm.loadPort" type="number" min="-1" step="1" /><small>(-1 代表不限制)</small></label>
              <label><span>機時單位批量</span><input v-model.number="equipmentForm.countEqpUnitQty" type="number" min="0" /></label>
              <label><span>變動機時</span><span class="equipment-number-unit"><input v-model.number="equipmentForm.varEqpTime" type="number" min="0" step="0.01" /><small>分鐘</small></span></label>
              <div class="equipment-auto-field"><span>自動設備</span><span class="equipment-radio-set"><label><input v-model.number="equipmentForm.autoFlag" type="radio" :value="1" @change="changeAutoFlag" />是</label><label><input v-model.number="equipmentForm.autoFlag" type="radio" :value="0" @change="changeAutoFlag" />否</label></span></div>
              <fieldset v-if="equipmentForm.autoFlag === 1" class="equipment-message-settings"><legend>訊息佇列設定</legend><label class="equipment-check"><input v-model="defaultQueueServer" type="checkbox" @change="changeDefaultQueueServer" /><span>預設伺服器</span></label><label><span>佇列伺服器</span><input v-model="equipmentForm.queueServer" :disabled="defaultQueueServer" :required="!defaultQueueServer" /></label><label><span>請求訊息佇列</span><input v-model="equipmentForm.requestMsgQueue" :disabled="defaultQueueServer" /></label><label><span>回應訊息佇列</span><input v-model="equipmentForm.responseMsgQueue" :disabled="defaultQueueServer" /></label><label><span>MES 請求佇列</span><input v-model="equipmentForm.requestMsgQueueMes" :disabled="defaultQueueServer" /></label><label><span>EAP 回應佇列</span><input v-model="equipmentForm.responseMsgQueueEap" :disabled="defaultQueueServer" /></label><label><span>EAP Controller</span><input v-model="equipmentForm.eaController" /></label><label><span>Recipe 管理</span><select v-model.number="equipmentForm.eqpRecipe"><option :value="0">UnLimited</option><option :value="1">Single Recipe</option><option :value="2">Multi Recipe</option><option :value="3">Batch Recipe</option></select></label></fieldset>
            </div>
          </div>
        </fieldset>
        <div class="equipment-form-audit"><span>建立人　{{ equipmentAudit.creator || '—' }}</span><span>建立日期　{{ auditDate(equipmentAudit.createdAt) }}</span><span>修改人　{{ equipmentAudit.reviser || '—' }}</span><span>修改日期　{{ auditDate(equipmentAudit.revisedAt) }}</span></div>
        <footer><button class="equipment-form-confirm" type="submit" :disabled="busy">確定</button><button type="button" @click="closeEquipmentForm">關閉</button><button type="button" :disabled="busy || !equipmentDirty" @click="saveEquipment(true)">套用</button></footer>
      </form>
    </div>

    <div v-if="chamberFormMode" class="equipment-form-overlay" @click.self="chamberFormMode = null">
      <form class="equipment-chamber-form" @submit.prevent="saveChamber">
        <header><strong>Chamber 基本資料 ({{ chamberFormMode === 'edit' ? 'EDIT' : 'ADD' }})</strong><button type="button" aria-label="關閉" @click="chamberFormMode = null">×</button></header>
        <label><span>設備編號</span><input v-model="chamberForm.equipmentNo" required :readonly="chamberFormMode === 'edit'" /></label>
        <label><span>所屬設備</span><input v-model="chamberForm.belongEquipmentNo" readonly /></label>
        <label><span>設備名稱</span><input v-model="chamberForm.equipmentName" required /></label>
        <label><span>說明</span><textarea v-model="chamberForm.description" rows="3" /></label>
        <footer><button class="equipment-form-confirm" type="submit" :disabled="busy">確定</button><button type="button" @click="chamberFormMode = null">關閉</button></footer>
      </form>
    </div>

    <EquipmentPropertyMaster
      v-if="propertyWindowOpen && selected"
      :equipment-no="selected.equipmentNo"
      :api-base-url="props.apiBaseUrl"
      :current-user-no="props.currentUserNo"
      @close="propertyWindowOpen = false"
      @updated="void loadEquipment()"
    />
    <EquipmentParameterMaster
      v-if="parameterWindowOpen && selected"
      :equipment-no="selected.equipmentNo"
      :api-base-url="props.apiBaseUrl"
      :current-user-no="props.currentUserNo"
      @close="parameterWindowOpen = false"
      @updated="void loadEquipment()"
    />
    <EquipmentQCRelationMaster
      v-if="qcRelationWindowOpen && selected"
      :equipment-no="selected.equipmentNo"
      :equipment-type="selected.equipmentType"
      :equipment-name="selected.equipmentName"
      :api-base-url="props.apiBaseUrl"
      :current-user-no="props.currentUserNo"
      @close="qcRelationWindowOpen = false"
      @updated="void loadEquipment()"
    />
    <EquipmentDataImport
      v-if="importWindowOpen"
      :api-base-url="props.apiBaseUrl"
      :current-user-no="props.currentUserNo"
      @close="importWindowOpen = false"
      @updated="void loadEquipment()"
    />
  </div>
</template>

<style scoped>
.equipment-dev-note{margin:0 0 6px;padding:7px 10px;border:1px solid #d8c577;background:#fff6c8;color:#66551b}
.equipment-chamber-filter{margin:0 0 6px;padding:5px 8px;border:1px solid #ccc}
.equipment-master-page{min-width:0;padding:10px 14px 24px;background:#fff;color:#30363b;font-family:'Segoe UI','Microsoft JhengHei',sans-serif;font-size:12px}
.equipment-action,.equipment-small-action{font-family:inherit;cursor:pointer}
.equipment-form-overlay{position:fixed;inset:0;z-index:30;display:grid;place-items:center;padding:14px;background:#0005}
.equipment-win-form,.equipment-chamber-form{display:flex;width:min(1220px,98vw);max-height:96vh;flex-direction:column;gap:10px;overflow:auto;padding:12px 16px;border:1px solid #aaa;background:#fff;color:#444;box-shadow:0 10px 35px #0004;font:14px 'Segoe UI','Microsoft JhengHei',sans-serif}
.equipment-win-form>header,.equipment-chamber-form>header{display:flex;align-items:center;justify-content:space-between;font-size:16px}
.equipment-win-form>header button,.equipment-chamber-form>header button{width:32px;height:30px;border:1px solid #ccc;background:#fafafa;font-size:20px}
.equipment-win-form>fieldset{min-width:0;margin:0;padding:12px;border:1px solid #aaa}
.equipment-win-form>fieldset>legend,.equipment-message-settings>legend{padding:0 6px;color:#555}
.equipment-form-columns{display:grid;grid-template-columns:1fr 1fr;gap:26px}
.equipment-form-column{display:grid;align-content:start;gap:9px}
.equipment-form-column>label,.equipment-chamber-form>label{display:grid;grid-template-columns:minmax(115px,30%) minmax(0,1fr) auto;align-items:center;gap:7px}
.equipment-form-column input,.equipment-form-column select,.equipment-form-column textarea,.equipment-chamber-form input,.equipment-chamber-form textarea{width:100%;min-width:0;min-height:32px;padding:4px 7px;border:1px solid #888;border-radius:0;background:#fff;font:inherit}
.equipment-form-column input:not([readonly]),.equipment-form-column select{border-color:#b3a765}.equipment-form-column input[required],.equipment-form-column input.equipment-kd-field,.equipment-form-column select.equipment-kd-field,.equipment-chamber-form input[required]{background:#fff1aa}
.equipment-form-column input[readonly],.equipment-chamber-form input[readonly]{background:#eee}
.equipment-number-unit{display:flex;align-items:center;gap:8px}.equipment-number-unit input{max-width:150px}.equipment-form-column small{white-space:nowrap;color:#666}
.equipment-auto-field{display:grid;grid-template-columns:minmax(115px,30%) minmax(0,1fr);align-items:center;gap:7px}.equipment-radio-set{display:flex;gap:16px}.equipment-radio-set label{display:flex;align-items:center;gap:4px}.equipment-radio-set input,.equipment-check input{width:auto}
.equipment-message-settings{display:grid;grid-template-columns:minmax(0,1fr);gap:7px;margin-top:2px;padding:9px;border:1px solid #bbb}
.equipment-message-settings>label:not(.equipment-check){display:grid;grid-template-columns:minmax(110px,32%) minmax(0,1fr);align-items:center;gap:7px}
.equipment-message-settings input,.equipment-message-settings select{height:30px;border:1px solid #888;background:#fff;padding:4px;font:inherit}
.equipment-message-settings .equipment-check{display:flex;align-items:center;gap:7px;margin-left:32%}
.equipment-form-audit{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:8px;padding:9px;border:1px solid #c7c7c7;color:#777}
.equipment-win-form>footer,.equipment-chamber-form>footer{display:flex;justify-content:flex-end;gap:9px;padding-top:2px}
.equipment-win-form>footer button,.equipment-chamber-form>footer button{min-width:100px;height:38px;border:1px solid #ccc;background:#fafafa;font:inherit}
.equipment-win-form>footer .equipment-form-confirm,.equipment-chamber-form>footer .equipment-form-confirm{border-color:#f0a126;background:#ffa62b;color:white}
.equipment-chamber-form{width:min(600px,95vw)}.equipment-chamber-form>label{grid-template-columns:130px minmax(0,1fr)}
.equipment-query{display:flex;min-height:52px;align-items:center;gap:8px 10px;margin:0 0 6px;padding:4px 7px;border:1px solid #dedede;background:#fff;white-space:nowrap}
.equipment-state-filter{display:flex;align-items:center;gap:10px 16px;margin:0;padding:0 4px;border:0}
.equipment-state-filter legend{position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0)}
.equipment-state-filter label{display:inline-flex;align-items:center;gap:4px}
.equipment-state-filter input{width:14px;height:14px;margin:0;accent-color:#444}
.equipment-query-label{font-size:12px}
.equipment-query select,.equipment-keyword{height:29px;border:1px solid #888;border-radius:0;background:#fff;color:#333;font:inherit}
.equipment-query select{width:205px;padding:3px 7px}
.equipment-keyword{width:min(210px,24vw);padding:4px 7px}
.equipment-like{font-size:12px}
.equipment-query-button{min-width:82px;height:29px;padding:4px 16px;border:1px solid #aaa;border-radius:3px;background:linear-gradient(#fff,#eee);color:#333;font:inherit}
.equipment-list-panel{display:grid;grid-template-columns:minmax(0,1fr) 116px;grid-template-rows:auto minmax(260px,1fr) auto;min-width:0;max-height:62vh;overflow:hidden;padding:5px;border:1px solid #c7c7c7;background:#fff}
.equipment-panel-heading{grid-column:1/-1;display:flex;align-items:center;gap:10px;min-height:26px;padding:2px 4px 5px}
.equipment-panel-heading h2{margin:0;color:#333;font-size:13px;font-weight:500}
.equipment-panel-heading span{color:#777;font-size:11px}
.equipment-table-scroll,.equipment-chamber-scroll{min-width:0;overflow:auto;border:1px solid #c7c7c7}
.equipment-table{width:100%;border-collapse:collapse;font-size:11px;white-space:nowrap}
.equipment-table th{position:sticky;top:0;z-index:1;height:31px;padding:5px 8px;border:1px solid #c7b852;background:#e7c600;color:#26251f;font-weight:500;text-align:left}
.equipment-table th:first-child{background:#e7c600;color:#26251f}
.equipment-table td{height:33px;padding:5px 9px;border:1px solid #d0d0d0;line-height:1.25;text-align:left}
.equipment-table tbody tr{cursor:pointer}
.equipment-table tbody tr:hover{background:#f3f8fd}
.equipment-table tbody tr.selected-row{background:#ffe69a;box-shadow:inset 3px 0 #f0a126}
.equipment-table tbody tr:focus-visible{outline:2px solid #278bdc;outline-offset:-2px}
.equipment-code{color:#273b50;font:12px 'Segoe UI',sans-serif}
.equipment-actions{display:flex;grid-column:2;grid-row:2;flex-direction:column;gap:5px;min-width:0;max-height:60vh;margin:0;padding:4px 0 4px 8px;overflow-y:auto;border-left:1px solid #c7c7c7;background:#fff}
.equipment-action{display:flex;min-height:34px;align-items:center;justify-content:center;padding:4px 6px;border:1px solid #c4c4c4;border-radius:3px;background:linear-gradient(#fff,#f5f5f5);color:#333;font:inherit;text-align:center;text-decoration:none;cursor:pointer}
.equipment-action:hover:not(:disabled){background:#f2f2f2}
.equipment-action.primary{border-color:#f2a532;background:#ffb33b;color:#fff;font-weight:600}
.equipment-action.danger{color:#7e2929}
.equipment-action:disabled{color:#aaa;cursor:not-allowed}
.equipment-list-footer{grid-column:1/-1;min-height:24px;padding:5px 3px 0;color:#777;font-size:11px}
.equipment-list-footer strong{margin-left:8px;color:#555;font-weight:500}
.equipment-message{margin:5px 0;padding:7px 10px;border:1px solid;font-size:12px}
.equipment-message.error{border-color:#d5a3a3;background:#fff0f0;color:#7a2424}
.equipment-message.success{border-color:#9bc3a3;background:#eef8ef;color:#275d33}
.equipment-chamber-panel{margin-top:8px;border:1px solid #bcbcbc;background:#fff}
.equipment-chamber-panel>summary{padding:8px 10px;background:#f1f1f1;color:#444;font-weight:600;cursor:pointer}
.equipment-chamber-panel[open]>summary{border-bottom:1px solid #ccc}
.equipment-detail-panel{margin:0;padding:7px;border:0}
.equipment-detail-header{display:flex;align-items:center;justify-content:space-between;gap:12px;padding:3px 4px 8px}
.equipment-detail-header h2{margin:0;color:#30363b;font-size:14px;font-weight:600}
.equipment-detail-header p{margin:3px 0 0;color:#777;font-size:11px}
.equipment-small-action,.equipment-chamber-actions a,.equipment-chamber-actions button{padding:4px 9px;border:1px solid #c4c4c4;border-radius:3px;background:linear-gradient(#fff,#f5f5f5);color:#333;font:inherit;text-decoration:none;white-space:nowrap}
.equipment-detail-note{padding:10px;color:#777}
.equipment-chamber-table th:first-child{background:#e7c600;color:#26251f}
.equipment-chamber-actions{display:flex;gap:5px}
.equipment-chamber-actions button{cursor:pointer}
@media(max-width:900px){.equipment-query{flex-wrap:wrap}.equipment-list-panel{grid-template-columns:minmax(0,1fr) 96px}.equipment-actions{padding-left:5px}.equipment-table{min-width:1050px}}
@media(max-width:580px){.equipment-master-page{padding:7px}.equipment-list-panel{grid-template-columns:minmax(0,1fr) 82px}.equipment-action{min-height:32px;padding:4px;font-size:10px}.equipment-table{min-width:900px}.equipment-state-filter{flex-wrap:wrap;gap:7px 10px}}
</style>
