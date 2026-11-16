<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type MasterMode = 'new' | 'edit' | 'copy' | null
type DetailType = 0 | 1 | 2 | 3
interface ValueOption { value: number; label: string }
interface EquipmentStateOption { equipmentState: number; stateName: string }
interface QcList {
  qcListNo: string; qcListName: string; qcType: number; unValidAction: number; afterEventQc: number
  description: string | null; creator: string; createDate: string; reviser: string | null; reviseDate: string | null; dataStamp: number
}
interface QcItem {
  qcListNo: string; qcOrder: number; qcItem: string; qcType: DetailType; inputDataCount: number
  stdValue: string | null; miniValue: string | null; maxiValue: string | null
  inspectionMethod: string | null; inspectionStandards: string | null
}
interface QcListForm { qcListNo: string; qcListName: string; qcType: number | null; unValidAction: number | null; afterEventQc: boolean; description: string }
interface QcItemForm {
  qcItem: string; qcType: DetailType | null; inputDataCount: number; stdValue: string
  miniValue: string; maxiValue: string; inspectionMethod: string; inspectionStandards: string
}

const props = defineProps<{ apiBaseUrl: string; currentUserNo?: string }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mock = import.meta.env.DEV
const keys = { lists: 'ymes-dev-mock:equipment-qc-lists:v1', items: 'ymes-dev-mock:equipment-qc-list-items:v1' }
const fallbackQcTypes: ValueOption[] = [
  { value: 0, label: '開始生產時點檢' }, { value: 2, label: '手動點檢' },
]
const detailTypeOptions: DetailType[] = [0, 1, 2, 3]
const typeLabels = ['標準值', '範圍', '顯示訊息', '輸入資料']
const seedLists: QcList[] = [
  { qcListNo: 'DEV-EQP-QC-01', qcListName: 'DEV 假資料：開機點檢表', qcType: 0, unValidAction: -1, afterEventQc: 1, description: 'DEV 假資料：設備啟動時檢查項目', creator: 'DEV001', createDate: '2026-11-01T08:00:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
  { qcListNo: 'DEV-EQP-QC-02', qcListName: 'DEV 假資料：設備巡檢表', qcType: 2, unValidAction: 83, afterEventQc: 0, description: 'DEV 假資料：週期性設備巡檢', creator: 'DEV001', createDate: '2026-10-19T09:10:00+08:00', reviser: 'DEV002', reviseDate: '2026-10-20T14:00:00+08:00', dataStamp: 1 },
]
const seedItems: Record<string, QcItem[]> = {
  'DEV-EQP-QC-01': [
    { qcListNo: 'DEV-EQP-QC-01', qcOrder: 1, qcItem: 'DEV 檢查急停按鈕', qcType: 0, inputDataCount: 1, stdValue: '1', miniValue: null, maxiValue: null, inspectionMethod: '目視及按壓確認', inspectionStandards: '按鈕可動作且復歸正常' },
    { qcListNo: 'DEV-EQP-QC-01', qcOrder: 2, qcItem: 'DEV 氣壓確認', qcType: 1, inputDataCount: 1, stdValue: null, miniValue: '0.8', maxiValue: '1.2', inspectionMethod: '讀取壓力表', inspectionStandards: '壓力需位於上下限內' },
  ],
  'DEV-EQP-QC-02': [
    { qcListNo: 'DEV-EQP-QC-02', qcOrder: 1, qcItem: 'DEV 記錄運轉時數', qcType: 3, inputDataCount: 2, stdValue: null, miniValue: null, maxiValue: null, inspectionMethod: '讀取設備面板', inspectionStandards: '輸入目前運轉時數' },
    { qcListNo: 'DEV-EQP-QC-02', qcOrder: 2, qcItem: 'DEV 注意安全訊息', qcType: 2, inputDataCount: 1, stdValue: null, miniValue: null, maxiValue: null, inspectionMethod: '', inspectionStandards: '點檢完成後確認周邊安全' },
  ],
}
const seedStates: EquipmentStateOption[] = [
  { equipmentState: 81, stateName: 'DEV 假資料：待機' },
  { equipmentState: 82, stateName: 'DEV 假資料：加工中' },
  { equipmentState: 83, stateName: 'DEV 假資料：維修中' },
]

const rows = ref<QcList[]>([])
const itemRows = ref<QcItem[]>([])
const qcTypes = ref<ValueOption[]>(fallbackQcTypes)
const equipmentStates = ref<EquipmentStateOption[]>(seedStates)
const queryNo = ref('')
const appliedQuery = ref('')
const currentNo = ref('')
const selectedNos = ref<string[]>([])
const selectedItemOrder = ref<number | null>(null)
const selectedItemOrders = ref<number[]>([])
const loading = ref(false)
const busy = ref(false)
const error = ref('')
const notice = ref('')
const mode = ref<MasterMode>(null)
const masterForm = ref<QcListForm>(emptyMaster())
const detailForm = ref<QcItemForm>(emptyItem())
const editingOrder = ref<number | null>(null)
const formItems = ref<QcItem[]>([])
const formItemsLoading = ref(false)
const formItemsLoadFailed = ref(false)
const formItemsRequestId = ref(0)
const formDataStamp = ref(0)

const visibleRows = computed(() => rows.value.filter((row) => !appliedQuery.value || row.qcListNo.toLowerCase().includes(appliedQuery.value.toLowerCase())))
const current = computed(() => visibleRows.value.find((row) => row.qcListNo === currentNo.value) ?? null)
const currentItems = computed(() => itemRows.value.slice().sort((a, b) => a.qcOrder - b.qcOrder))
const afterEventEnabled = computed(() => masterForm.value.qcType === 0)
const masterTitle = computed(() => mode.value === 'edit' ? '修改設備點檢表' : mode.value === 'copy' ? '複製設備點檢表' : '新增設備點檢表')

function emptyMaster(): QcListForm { return { qcListNo: '', qcListName: '', qcType: 2, unValidAction: -1, afterEventQc: false, description: '' } }
function emptyItem(): QcItemForm { return { qcItem: '', qcType: null, inputDataCount: 1, stdValue: '', miniValue: '', maxiValue: '', inspectionMethod: '', inspectionStandards: '' } }
function read<T>(key: string, initial: T): T {
  try { const raw = localStorage.getItem(key); return raw ? JSON.parse(raw) as T : structuredClone(initial) }
  catch { return structuredClone(initial) }
}
function write<T>(key: string, value: T): void { localStorage.setItem(key, JSON.stringify(value)) }
function normalizeDevQcTypes(lists: QcList[]): QcList[] {
  const normalized = lists.map((row) => row.qcType === 1 ? { ...row, qcType: 2, afterEventQc: 0 } : row)
  if (normalized.some((row, index) => row !== lists[index])) write(keys.lists, normalized)
  return normalized
}
function typeLabel(value: number): string { return qcTypes.value.find((item) => item.value === value)?.label || String(value) }
function itemTypeLabel(value: number): string { return typeLabels[value] || String(value) }
function dateText(value?: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.valueOf()) ? value : date.toLocaleString('zh-TW')
}
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  const root = new URL(base.endsWith('/') ? base : base + '/', window.location.origin)
  return new URL(path.replace(/^\/+/, ''), root).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init })
  if (response.status === 401) {
    window.location.assign(appBase + '/')
    throw new Error('登入狀態已逾期，請重新登入。')
  }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    throw new Error(problem?.detail || problem?.message || problem?.title || '設備點檢表 API 回應錯誤 (' + response.status + ')。')
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function closePage(): void { window.history.pushState({}, '', appBase || '/'); window.dispatchEvent(new PopStateEvent('popstate')) }
function setQuery(): void { appliedQuery.value = queryNo.value.trim(); void loadLists() }
function toggleSelected(no: string): void {
  selectedNos.value = selectedNos.value.includes(no) ? selectedNos.value.filter((x) => x !== no) : [...selectedNos.value, no]
}
function selectList(no: string): void {
  if (currentNo.value === no) return
  currentNo.value = no
  selectedItemOrder.value = null
  void loadItems()
}
function handleQcTypeChange(): void { if (!afterEventEnabled.value) masterForm.value.afterEventQc = false }
function resetDetail(): void {
  editingOrder.value = null
  selectedItemOrder.value = null
  selectedItemOrders.value = []
  detailForm.value = emptyItem()
}
function openMaster(nextMode: Exclude<MasterMode, null>): void {
  const row = current.value
  if (nextMode !== 'new' && !row) { error.value = '請先選取一筆設備點檢表。'; return }
  mode.value = nextMode
  error.value = ''
  notice.value = ''
  selectedItemOrder.value = null
  resetDetail()
  formItems.value = []
  formItemsLoading.value = false
  formItemsLoadFailed.value = false
  const requestId = ++formItemsRequestId.value
  if (nextMode === 'new') { masterForm.value = emptyMaster(); formDataStamp.value = 0; return }
  if (!row) return
  masterForm.value = {
    qcListNo: nextMode === 'edit' ? row.qcListNo : '',
    qcListName: row.qcListName,
    qcType: row.qcType,
    unValidAction: row.unValidAction,
    afterEventQc: row.afterEventQc === 1,
    description: row.description || '',
  }
  formDataStamp.value = row.dataStamp
  if (mock) {
    const sourceItems = read<Record<string, QcItem[]>>(keys.items, seedItems)[row.qcListNo] ?? []
    formItems.value = sourceItems.map((item) => ({ ...item, qcListNo: masterForm.value.qcListNo }))
  } else {
    void loadFormItems(row.qcListNo, requestId)
  }
}
async function loadFormItems(no: string, requestId: number): Promise<void> {
  formItemsLoading.value = true
  formItemsLoadFailed.value = false
  try {
    const data = await request<QcItem[]>('api/equipment/qc-lists/' + encodeURIComponent(no) + '/items')
    if (requestId === formItemsRequestId.value) formItems.value = data
  } catch (e) {
    if (requestId === formItemsRequestId.value) {
      formItemsLoadFailed.value = true
      error.value = e instanceof Error ? e.message : '點檢項目載入失敗。'
    }
  } finally {
    if (requestId === formItemsRequestId.value) formItemsLoading.value = false
  }
}
function selectDetail(order: number): void {
  const row = formItems.value.find((item) => item.qcOrder === order)
  if (!row) return
  selectedItemOrder.value = order
  selectedItemOrders.value = [order]
  editingOrder.value = order
  detailForm.value = {
    qcItem: row.qcItem, qcType: row.qcType, inputDataCount: row.inputDataCount,
    stdValue: row.stdValue === '*' ? '' : row.stdValue || '',
    miniValue: row.miniValue === '*' ? '' : row.miniValue || '',
    maxiValue: row.maxiValue === '*' ? '' : row.maxiValue || '',
    inspectionMethod: row.inspectionMethod || '', inspectionStandards: row.inspectionStandards || '',
  }
}
function toggleItemSelection(order: number): void {
  selectedItemOrders.value = selectedItemOrders.value.includes(order)
    ? selectedItemOrders.value.filter((item) => item !== order)
    : [...selectedItemOrders.value, order]
}
function changeDetailType(value: DetailType): void {
  detailForm.value.qcType = value
  if (value === 0) { detailForm.value.miniValue = ''; detailForm.value.maxiValue = '' }
  else if (value === 1) detailForm.value.stdValue = ''
  else { detailForm.value.stdValue = ''; detailForm.value.miniValue = ''; detailForm.value.maxiValue = '' }
}
function validateDetail(): string {
  if (!masterForm.value.qcListNo.trim()) return '設備點檢表編號必填，才能新增點檢項目。'
  if (!detailForm.value.qcItem.trim()) return 'Check Item 為必填。'
  if (detailForm.value.qcItem.trim().length > 1000) return 'Check Item 不可超過 1000 個字元。'
  if (detailForm.value.qcType === null) return '請選擇點檢項目 Type。'
  if (!Number.isInteger(detailForm.value.inputDataCount) || detailForm.value.inputDataCount < 1 || detailForm.value.inputDataCount > 10) return 'Input Data Count 需為 1 到 10 的整數。'
  if (detailForm.value.inspectionMethod.length > 100 || detailForm.value.inspectionStandards.length > 100) return 'Inspection Method / Standards 不可超過 100 個字元。'
  if (detailForm.value.qcType === 0) {
    if (!detailForm.value.stdValue.trim()) return 'Std Value 為必填。'
    if (!Number.isFinite(Number(detailForm.value.stdValue))) return 'Std Value 必須為數字。'
    if (detailForm.value.stdValue.length > 12) return 'Std Value 不可超過資料庫欄位長度 12。'
  }
  if (detailForm.value.qcType === 1) {
    const min = detailForm.value.miniValue.trim()
    const max = detailForm.value.maxiValue.trim()
    if (!min && !max) return 'Maximum 和 Minimum 至少需填一個。'
    if (min && !Number.isFinite(Number(min))) return 'Minimum 必須為數字。'
    if (max && !Number.isFinite(Number(max))) return 'Maximum 必須為數字。'
    if (min && max && Number(max) <= Number(min)) return 'Maximum 必須大於 Minimum。'
    if (min.length > 12 || max.length > 12) return '範圍值不可超過資料庫欄位長度 12。'
  }
  const duplicate = formItems.value.some((item) => item.qcItem.trim() === detailForm.value.qcItem.trim() && item.qcOrder !== editingOrder.value)
  if (duplicate) return '同一張點檢表不可重複使用相同 Check Item。'
  return ''
}
function commitDetail(): void {
  if (!mode.value) return
  const problem = validateDetail()
  if (problem) { error.value = problem; return }
  const order = editingOrder.value ?? formItems.value.length + 1
  if (editingOrder.value === null && formItems.value.length >= 99) { error.value = '點檢項目最多 99 筆。'; return }
  const row: QcItem = {
    qcListNo: masterForm.value.qcListNo.trim(), qcOrder: order, qcItem: detailForm.value.qcItem.trim(),
    qcType: detailForm.value.qcType as DetailType, inputDataCount: detailForm.value.inputDataCount,
    stdValue: detailForm.value.qcType === 0 ? detailForm.value.stdValue.trim() : null,
    miniValue: detailForm.value.qcType === 1 ? detailForm.value.miniValue.trim() || '*' : null,
    maxiValue: detailForm.value.qcType === 1 ? detailForm.value.maxiValue.trim() || '*' : null,
    inspectionMethod: detailForm.value.inspectionMethod.trim() || null,
    inspectionStandards: detailForm.value.inspectionStandards.trim() || null,
  }
  if (editingOrder.value !== null) formItems.value = formItems.value.map((item) => item.qcOrder === editingOrder.value ? row : item)
  else formItems.value = [...formItems.value, row]
  formItems.value.sort((a, b) => a.qcOrder - b.qcOrder)
  error.value = ''
  resetDetail()
}
function removeDetails(): void {
  const selected = selectedItemOrders.value
  if (selected.length === 0) { error.value = '請先選取要刪除的點檢項目。'; return }
  if (!window.confirm('確定刪除選取的 ' + selected.length + ' 筆點檢項目？')) return
  const selectedSet = new Set(selected)
  formItems.value = formItems.value.filter((item) => !selectedSet.has(item.qcOrder)).map((item, index) => ({ ...item, qcOrder: index + 1 }))
  error.value = ''
  resetDetail()
}
function moveDetail(direction: -1 | 1): void {
  const order = selectedItemOrder.value
  if (order === null) return
  const index = formItems.value.findIndex((item) => item.qcOrder === order)
  const target = index + direction
  if (index < 0 || target < 0 || target >= formItems.value.length) return
  const changed = [...formItems.value]
  const source = changed[index]
  changed[index] = changed[target]
  changed[target] = source
  formItems.value = changed.map((item, itemIndex) => ({ ...item, qcOrder: itemIndex + 1 }))
  selectedItemOrder.value = target + 1
  selectedItemOrders.value = [target + 1]
}
function validateMaster(): string {
  if (!masterForm.value.qcListNo.trim()) return 'QC List No 為必填。'
  if (masterForm.value.qcListNo.trim().length > 100) return 'QC List No 不可超過 100 個字元。'
  if (!masterForm.value.qcListName.trim()) return 'QC List Name 為必填。'
  if (masterForm.value.qcListName.trim().length > 100) return 'QC List Name 不可超過 100 個字元。'
  if (masterForm.value.qcType === null || !qcTypes.value.some((option) => option.value === masterForm.value.qcType)) return '請選擇有效的 QC Type。'
  if (masterForm.value.unValidAction === null) return 'UnValid Action 為必填。'
  if (formItems.value.length === 0) return '至少需要一筆點檢項目。'
  const code = masterForm.value.qcListNo.trim()
  if (rows.value.some((row) => row.qcListNo === code && !(mode.value === 'edit' && row.qcListNo === currentNo.value))) return 'QC List No 已存在。'
  return ''
}
function masterBody(): { qcListNo: string; qcListName: string; qcType: number; unValidAction: number; afterEventQc: number; description: string | null; dataStamp?: number; items: QcItem[] } {
  return {
    qcListNo: masterForm.value.qcListNo.trim(),
    qcListName: masterForm.value.qcListName.trim(),
    qcType: masterForm.value.qcType ?? 2,
    unValidAction: masterForm.value.unValidAction ?? -1,
    afterEventQc: afterEventEnabled.value && masterForm.value.afterEventQc ? 1 : 0,
    description: masterForm.value.description.trim() || null,
    ...(mode.value === 'edit' ? { dataStamp: formDataStamp.value } : {}),
    items: formItems.value.map((item, index) => ({ ...item, qcListNo: masterForm.value.qcListNo.trim(), qcOrder: index + 1 })),
  }
}
async function saveMaster(keepOpen: boolean): Promise<void> {
  if (!mode.value || busy.value || formItemsLoading.value) return
  if (formItemsLoadFailed.value) { error.value = '明細載入失敗，請關閉表單後重新載入再試。'; return }
  const problem = validateMaster()
  if (problem) { error.value = problem; return }
  const saveMode = mode.value
  const body = masterBody()
  busy.value = true
  error.value = ''
  try {
    let nextDataStamp = formDataStamp.value
    if (mock) {
      const allLists = read<QcList[]>(keys.lists, seedLists)
      const allItems = read<Record<string, QcItem[]>>(keys.items, seedItems)
      if (saveMode === 'edit') {
        const index = allLists.findIndex((row) => row.qcListNo === currentNo.value)
        if (index < 0) throw new Error('設備點檢表已不存在，請重新載入。')
        nextDataStamp = allLists[index].dataStamp + 1
        allLists[index] = { ...allLists[index], ...body, dataStamp: nextDataStamp, reviser: props.currentUserNo || 'DEV001', reviseDate: new Date().toISOString() }
        delete allItems[currentNo.value]
      } else {
        if (allLists.some((row) => row.qcListNo === body.qcListNo)) throw new Error('QC List No 已存在。')
        nextDataStamp = 0
        allLists.push({ ...body, creator: props.currentUserNo || 'DEV001', createDate: new Date().toISOString(), reviser: null, reviseDate: null, dataStamp: 0 })
      }
      allItems[body.qcListNo] = body.items
      write(keys.lists, allLists)
      write(keys.items, allItems)
    } else {
      const path = saveMode === 'edit' ? 'api/equipment/qc-lists/' + encodeURIComponent(currentNo.value) : 'api/equipment/qc-lists'
      const result = await request<{ dataStamp?: number }>(path, {
        method: saveMode === 'edit' ? 'PUT' : 'POST',
        headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body),
      })
      if (result?.dataStamp !== undefined) nextDataStamp = result.dataStamp
      else if (saveMode === 'edit') nextDataStamp += 1
    }
    currentNo.value = body.qcListNo
    if (!mock) await loadLists()
    else rows.value = read<QcList[]>(keys.lists, seedLists)
    itemRows.value = body.items.map((item) => ({ ...item }))
    formDataStamp.value = nextDataStamp
    notice.value = saveMode === 'edit' ? '設備點檢表已修改。' : saveMode === 'copy' ? '設備點檢表已複製。' : '設備點檢表已新增。'
    if (keepOpen && saveMode !== 'edit') mode.value = 'edit'
    if (!keepOpen) mode.value = null
  } catch (e) { error.value = e instanceof Error ? e.message : '設備點檢表儲存失敗。' }
  finally { busy.value = false }
}
async function deleteSelected(): Promise<void> {
  if (!selectedNos.value.length) { error.value = '請先勾選要刪除的設備點檢表。'; return }
  const targets = rows.value.filter((row) => selectedNos.value.includes(row.qcListNo))
  if (!window.confirm('確定刪除選取的 ' + targets.length + ' 張設備點檢表及其點檢項目？')) return
  busy.value = true
  error.value = ''
  notice.value = ''
  try {
    if (mock) {
      const remove = new Set(targets.map((row) => row.qcListNo))
      const remaining = read<QcList[]>(keys.lists, seedLists).filter((row) => !remove.has(row.qcListNo))
      const allItems = read<Record<string, QcItem[]>>(keys.items, seedItems)
      for (const no of remove) delete allItems[no]
      write(keys.lists, remaining)
      write(keys.items, allItems)
      rows.value = remaining
    } else {
      for (const row of targets) await request<void>('api/equipment/qc-lists/' + encodeURIComponent(row.qcListNo), { method: 'DELETE' })
      await loadLists()
    }
    selectedNos.value = []
    if (targets.some((row) => row.qcListNo === currentNo.value)) {
      currentNo.value = visibleRows.value[0]?.qcListNo ?? ''
      await loadItems()
    }
    notice.value = '已刪除 ' + targets.length + ' 張設備點檢表。'
  } catch (e) { error.value = e instanceof Error ? e.message : '設備點檢表刪除失敗。' }
  finally { busy.value = false }
}
async function loadItems(): Promise<void> {
  if (!currentNo.value) { itemRows.value = []; return }
  try {
    if (mock) itemRows.value = read<Record<string, QcItem[]>>(keys.items, seedItems)[currentNo.value] ?? []
    else itemRows.value = await request<QcItem[]>('api/equipment/qc-lists/' + encodeURIComponent(currentNo.value) + '/items')
  } catch (e) { itemRows.value = []; error.value = e instanceof Error ? e.message : '點檢項目載入失敗。' }
}
async function loadLists(): Promise<void> {
  loading.value = true
  error.value = ''
  notice.value = ''
  try {
    if (mock) {
      rows.value = normalizeDevQcTypes(read<QcList[]>(keys.lists, seedLists))
      qcTypes.value = read<ValueOption[]>('ymes-dev-mock:equipment-qc-types:v1', fallbackQcTypes).filter((option) => option.value === 0 || option.value === 2)
      equipmentStates.value = read<EquipmentStateOption[]>('ymes-dev-mock:equipment-states:v1', seedStates)
    } else {
      const query = new URLSearchParams()
      if (appliedQuery.value) query.set('qcListNo', appliedQuery.value)
      const suffix = query.toString()
      const [data, options] = await Promise.all([
        request<QcList[]>('api/equipment/qc-lists' + (suffix ? '?' + suffix : '')),
        request<{ qcTypes: ValueOption[]; equipmentStates: EquipmentStateOption[] }>('api/equipment/qc-lists/options'),
      ])
      rows.value = data
      qcTypes.value = options.qcTypes
      equipmentStates.value = options.equipmentStates
    }
    rows.value = rows.value.slice().sort((a, b) => a.qcListNo.localeCompare(b.qcListNo))
    selectedNos.value = selectedNos.value.filter((no) => rows.value.some((row) => row.qcListNo === no))
    if (!visibleRows.value.some((row) => row.qcListNo === currentNo.value)) currentNo.value = visibleRows.value[0]?.qcListNo ?? ''
    await loadItems()
  } catch (e) {
    rows.value = []
    currentNo.value = ''
    itemRows.value = []
    error.value = e instanceof Error ? e.message : '設備點檢表清單載入失敗。'
  } finally { loading.value = false }
}
onMounted(() => { void loadLists() })
</script>
<template>
  <section class="eqpqc-page">
    <div v-if="mock" class="eqpqc-dev-note">開發模式：目前資料為 DEV 假資料，儲存在此瀏覽器；不會寫入 MES 或正式資料庫。</div>
    <div class="eqpqc-search">
      <label for="qcListNoSearch">QC List No</label>
      <input id="qcListNoSearch" v-model="queryNo" type="text" maxlength="100" @keydown.enter="setQuery">
      <button type="button" class="eqpqc-primary" :disabled="loading" @click="setQuery">查詢</button>
    </div>
    <p v-if="error && !mode" class="eqpqc-message eqpqc-error" role="alert">{{ error }}</p>
    <p v-else-if="notice && !mode" class="eqpqc-message eqpqc-success" role="status">{{ notice }}</p>

    <div class="eqpqc-main">
      <div class="eqpqc-panels">
        <section class="eqpqc-panel">
          <header>設備點檢表主檔 <span>{{ visibleRows.length }} 筆</span></header>
          <div class="eqpqc-table-wrap">
            <table class="eqpqc-grid">
              <thead><tr>
                <th class="eqpqc-check"></th><th>QC List No</th><th>QC List Name</th><th>QC Type</th><th>UnValid Action</th><th>After Event QC</th><th>Description</th><th>Creator</th><th>Create Date</th><th>Reviser</th><th>Revise Date</th>
              </tr></thead>
              <tbody>
                <tr v-if="loading"><td colspan="11" class="eqpqc-empty">載入中…</td></tr>
                <tr v-else-if="visibleRows.length===0"><td colspan="11" class="eqpqc-empty">沒有符合的設備點檢表。</td></tr>
                <tr v-for="row in visibleRows" :key="row.qcListNo" :class="{ 'eqpqc-current': currentNo===row.qcListNo }" @click="selectList(row.qcListNo)" @dblclick="selectList(row.qcListNo); openMaster('edit')">
                  <td class="eqpqc-check" @click.stop><input type="checkbox" :checked="selectedNos.includes(row.qcListNo)" :aria-label="'選取 ' + row.qcListNo" @change="toggleSelected(row.qcListNo)"></td>
                  <td>{{ row.qcListNo }}</td><td>{{ row.qcListName }}</td><td>{{ typeLabel(row.qcType) }}</td>
                  <td>{{ row.unValidAction===-1 ? '不動作' : (equipmentStates.find((x)=>x.equipmentState===row.unValidAction)?.stateName || row.unValidAction) }}</td>
                  <td>{{ row.afterEventQc===1 ? '是' : '否' }}</td><td>{{ row.description || '' }}</td><td>{{ row.creator }}</td><td>{{ dateText(row.createDate) }}</td><td>{{ row.reviser || '' }}</td><td>{{ dateText(row.reviseDate) }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>
        <section class="eqpqc-panel">
          <header>點檢項目明細 <span>{{ current?.qcListNo || '未選取' }} · {{ currentItems.length }} 筆</span></header>
          <div class="eqpqc-table-wrap">
            <table class="eqpqc-grid">
              <thead><tr><th>QC Order</th><th>Check Item</th><th>Inspection Method</th><th>Inspection Standards</th><th>Type</th><th>Input Data Count</th><th>Std Value</th><th>Minimum</th><th>Maximum</th></tr></thead>
              <tbody>
                <tr v-if="!current"><td colspan="9" class="eqpqc-empty">選取上方設備點檢表以查看項目。</td></tr>
                <tr v-else-if="currentItems.length===0"><td colspan="9" class="eqpqc-empty">此點檢表沒有項目。</td></tr>
                <tr v-for="item in currentItems" :key="item.qcOrder">
                  <td>{{ item.qcOrder }}</td><td>{{ item.qcItem }}</td><td>{{ item.inspectionMethod }}</td><td>{{ item.inspectionStandards }}</td><td>{{ itemTypeLabel(item.qcType) }}</td><td>{{ item.inputDataCount }}</td><td>{{ item.stdValue || '' }}</td><td>{{ item.miniValue || '' }}</td><td>{{ item.maxiValue || '' }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>
      </div>
      <aside class="eqpqc-actions">
        <button type="button" class="eqpqc-primary" :disabled="busy" @click="openMaster('new')">新增(A)</button>
        <button type="button" :disabled="busy || !current" @click="openMaster('edit')">修改(E)</button>
        <button type="button" :disabled="busy || selectedNos.length===0" @click="deleteSelected">刪除(D)</button>
        <button type="button" :disabled="busy || !current" @click="openMaster('copy')">複製(C)</button>
        <button type="button" class="eqpqc-close-page" :disabled="busy" @click="closePage">關閉</button>
      </aside>
    </div>

    <div v-if="mode" class="eqpqc-overlay" @click.self="mode=null">
      <form class="eqpqc-dialog" @submit.prevent="saveMaster(false)">
        <header class="eqpqc-dialog-header"><strong>{{ masterTitle }}</strong><button type="button" aria-label="關閉" @click="mode=null">×</button></header>
        <div class="eqpqc-dialog-body">
          <section class="eqpqc-master-fields">
            <label><span>QC List No <b>*</b></span><input v-model="masterForm.qcListNo" type="text" maxlength="100" :readonly="mode==='edit'"></label>
            <label><span>QC List Name <b>*</b></span><input v-model="masterForm.qcListName" type="text" maxlength="100"></label>
            <label><span>QC Type <b>*</b></span><select v-model.number="masterForm.qcType" @change="handleQcTypeChange"><option v-for="option in qcTypes" :key="option.value" :value="option.value">{{ option.label }}</option></select></label>
            <label><span>UnValid Action <b>*</b></span><select v-model.number="masterForm.unValidAction"><option :value="-1">不動作</option><option v-for="state in equipmentStates" :key="state.equipmentState" :value="state.equipmentState">{{ state.stateName }}</option></select></label>
            <label class="eqpqc-after-event"><input v-model="masterForm.afterEventQc" type="checkbox" :disabled="!afterEventEnabled"><span>After Event QC</span></label>
            <label class="eqpqc-desc"><span>Description</span><textarea v-model="masterForm.description" maxlength="255" rows="2"></textarea></label>
          </section>

          <section class="eqpqc-detail-editor">
            <header>點檢項目設定</header>
            <fieldset class="eqpqc-detail-form" :disabled="formItemsLoading || formItemsLoadFailed">
              <label class="eqpqc-item-name"><span>Check Item <b>*</b></span><input v-model="detailForm.qcItem" type="text" maxlength="1000"></label>
              <label><span>Inspection Method</span><input v-model="detailForm.inspectionMethod" maxlength="100"></label>
              <label><span>Inspection Standards</span><input v-model="detailForm.inspectionStandards" maxlength="100"></label>
              <fieldset><legend>Type <b>*</b></legend>
                <label v-for="option in detailTypeOptions" :key="option"><input type="radio" :checked="detailForm.qcType===option" @change="changeDetailType(option)">{{ typeLabels[option] }}</label>
              </fieldset>
              <label><span>Input Data Count</span><input v-model.number="detailForm.inputDataCount" type="number" min="1" max="10" step="1"></label>
              <label><span>Std Value</span><input v-model="detailForm.stdValue" maxlength="12" :disabled="detailForm.qcType!==0"></label>
              <label><span>Minimum</span><input v-model="detailForm.miniValue" maxlength="12" :disabled="detailForm.qcType!==1"></label>
              <label><span>Maximum</span><input v-model="detailForm.maxiValue" maxlength="12" :disabled="detailForm.qcType!==1"></label>
              <div class="eqpqc-detail-actions">
                <button type="button" class="eqpqc-primary" @click="commitDetail">{{ editingOrder===null ? '新增項目' : '修改項目' }}</button>
                <button type="button" @click="resetDetail">清除</button>
                <button type="button" :disabled="selectedItemOrder===null" @click="moveDetail(-1)">上移</button>
                <button type="button" :disabled="selectedItemOrder===null" @click="moveDetail(1)">下移</button>
                <button type="button" :disabled="selectedItemOrders.length===0" @click="removeDetails">刪除項目</button>
              </div>
            </fieldset>
            <div class="eqpqc-table-wrap eqpqc-items-wrap">
              <table class="eqpqc-grid">
                <thead><tr><th class="eqpqc-check"></th><th>QC Order</th><th>Check Item</th><th>Inspection Method</th><th>Inspection Standards</th><th>Type</th><th>Input Data Count</th><th>Std Value</th><th>Minimum</th><th>Maximum</th></tr></thead>
                <tbody>
                  <tr v-if="formItemsLoading"><td colspan="10" class="eqpqc-empty">點檢項目載入中…</td></tr>
                  <tr v-else-if="formItems.length===0"><td colspan="10" class="eqpqc-empty">尚未加入點檢項目。</td></tr>
                  <tr v-for="item in formItems" :key="item.qcOrder" :class="{ 'eqpqc-current': selectedItemOrder===item.qcOrder }" @click="selectDetail(item.qcOrder)">
                    <td class="eqpqc-check" @click.stop><input type="checkbox" :checked="selectedItemOrders.includes(item.qcOrder)" :aria-label="'選取項目 ' + item.qcOrder" @change="toggleItemSelection(item.qcOrder)"></td>
                    <td>{{ item.qcOrder }}</td><td>{{ item.qcItem }}</td><td>{{ item.inspectionMethod }}</td><td>{{ item.inspectionStandards }}</td><td>{{ itemTypeLabel(item.qcType) }}</td><td>{{ item.inputDataCount }}</td><td>{{ item.stdValue || '' }}</td><td>{{ item.miniValue || '' }}</td><td>{{ item.maxiValue || '' }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </section>
          <p v-if="error" class="eqpqc-message eqpqc-error" role="alert">{{ error }}</p>
        </div>
        <footer class="eqpqc-dialog-footer">
          <span v-if="mode==='edit'">DataStamp: {{ formDataStamp }}</span>
          <button type="button" :disabled="busy || formItemsLoading || formItemsLoadFailed" @click="saveMaster(true)">套用</button>
          <button type="submit" class="eqpqc-primary" :disabled="busy || formItemsLoading || formItemsLoadFailed">確定</button>
          <button type="button" :disabled="busy" @click="mode=null">取消</button>
        </footer>
      </form>
    </div>
  </section>
</template>
<style scoped>
.eqpqc-page{display:flex;height:100%;min-height:0;flex-direction:column;gap:6px;color:#333;font-size:13px}
.eqpqc-dev-note{padding:7px 10px;border:1px solid #d8c577;background:#fff6c8;color:#66551b}
.eqpqc-search{display:flex;align-items:center;gap:7px;padding:5px 9px;border:1px solid #c9c9c9;background:#fafafa}
.eqpqc-search input,.eqpqc-detail-form input,.eqpqc-master-fields input,.eqpqc-master-fields select,.eqpqc-master-fields textarea{min-height:29px;padding:4px 7px;border:1px solid #a9a9a9;background:#fff0b3;color:#333;font:inherit}
.eqpqc-search input{width:min(270px,40vw)}
.eqpqc-search button,.eqpqc-actions button,.eqpqc-detail-actions button,.eqpqc-dialog-footer button{min-height:30px;padding:4px 12px;border:1px solid #bdbdbd;border-radius:3px;background:#fafafa;color:#333;font:inherit;cursor:pointer}
.eqpqc-search button:hover,.eqpqc-actions button:hover,.eqpqc-detail-actions button:hover,.eqpqc-dialog-footer button:hover{background:#fff2c3;border-color:#c8aa54}
.eqpqc-search button:disabled,.eqpqc-actions button:disabled,.eqpqc-detail-actions button:disabled,.eqpqc-dialog-footer button:disabled{opacity:.55;cursor:not-allowed}
.eqpqc-primary,.eqpqc-actions .eqpqc-primary,.eqpqc-dialog-footer .eqpqc-primary{background:#f5a623!important;border-color:#e49a24!important;color:#fff!important}
.eqpqc-message{margin:0;padding:7px 10px;border:1px solid}
.eqpqc-error{border-color:#dca7a2;background:#fff1ef;color:#942f24}
.eqpqc-success{border-color:#c8d8ac;background:#f4f8eb;color:#496622}
.eqpqc-main{display:grid;min-height:0;flex:1;grid-template-columns:minmax(0,1fr) 88px;gap:7px}
.eqpqc-panels{display:grid;min-height:0;grid-template-rows:minmax(0,1fr) minmax(0,1fr);gap:5px}
.eqpqc-panel{display:flex;min-height:0;flex-direction:column;border:1px solid #bdbdbd;background:#fff}
.eqpqc-panel>header,.eqpqc-detail-editor>header{display:flex;min-height:26px;align-items:center;justify-content:space-between;padding:3px 8px;border-bottom:1px solid #ccc;background:#f6f6f6;font-weight:600}
.eqpqc-panel>header span{font-size:11px;color:#777;font-weight:400}
.eqpqc-table-wrap{min-height:0;flex:1;overflow:auto}
.eqpqc-grid{width:100%;border-collapse:collapse;white-space:nowrap}
.eqpqc-grid th{position:sticky;top:0;z-index:1;padding:5px 7px;border:1px solid #d2c36d;background:#e7d978;color:#453d20;text-align:left;font-weight:600}
.eqpqc-grid td{height:27px;padding:4px 7px;border:1px solid #d6d6d6}
.eqpqc-grid tbody tr{cursor:pointer}
.eqpqc-grid tbody tr:nth-child(even){background:#fafafa}
.eqpqc-grid tbody tr:hover{background:#fff6d6}
.eqpqc-grid tbody tr.eqpqc-current{background:#fff0b7}
.eqpqc-check{width:28px;text-align:center}
.eqpqc-check input{margin:0;accent-color:#ad831b}
.eqpqc-empty{height:45px!important;color:#777;text-align:center}
.eqpqc-actions{display:flex;min-height:0;flex-direction:column;gap:6px;padding-top:16px}
.eqpqc-actions button{width:100%;min-height:32px}
.eqpqc-actions .eqpqc-close-page{margin-top:auto}
.eqpqc-overlay{position:fixed;inset:0;z-index:80;display:grid;place-items:center;padding:16px;background:rgba(0,0,0,.35)}
.eqpqc-dialog{display:flex;width:min(1260px,98vw);max-height:94vh;flex-direction:column;border:1px solid #999;background:#fdfdfd;box-shadow:0 5px 20px rgba(0,0,0,.24)}
.eqpqc-dialog-header{display:flex;min-height:40px;align-items:center;justify-content:space-between;padding:7px 13px;border-bottom:1px solid #c9c9c9;background:#f4f1e8;font-size:15px}
.eqpqc-dialog-header button{width:28px;height:26px;border:0;background:transparent;font-size:22px;cursor:pointer}
.eqpqc-dialog-body{min-height:0;overflow:auto;padding:10px}
.eqpqc-master-fields{display:grid;grid-template-columns:1fr 1fr;gap:8px 15px;margin:0 0 9px;padding:10px;border:1px solid #bdbdbd}
.eqpqc-master-fields>label{display:flex;min-width:0;align-items:center;gap:8px}
.eqpqc-master-fields>label>span{width:125px;flex:none;text-align:right;color:#555}
.eqpqc-master-fields>label>input,.eqpqc-master-fields>label>select,.eqpqc-master-fields>label>textarea{flex:1;min-width:0}
.eqpqc-master-fields b,.eqpqc-detail-form b{color:#99690e}
.eqpqc-master-fields .eqpqc-after-event{justify-content:flex-start;padding-left:133px}
.eqpqc-master-fields .eqpqc-after-event input{accent-color:#a77a1d}
.eqpqc-master-fields .eqpqc-desc{grid-column:1/-1;align-items:flex-start}
.eqpqc-detail-editor{display:flex;min-height:300px;flex-direction:column;border:1px solid #bdbdbd}
.eqpqc-detail-form{display:grid;grid-template-columns:2fr 1fr 1fr 1.4fr .75fr;gap:7px;padding:8px;margin:0;min-width:0;border:0}
.eqpqc-detail-form>label{display:flex;min-width:0;flex-direction:column;gap:4px}
.eqpqc-detail-form>label input{width:100%;min-width:0}
.eqpqc-detail-form input:disabled{background:#eee}
.eqpqc-detail-form fieldset{display:flex;min-width:0;flex-wrap:wrap;align-items:center;gap:4px 9px;margin:0;padding:3px 6px;border:1px solid #c4c4c4}
.eqpqc-detail-form legend{padding:0 3px;color:#555}
.eqpqc-detail-form fieldset label{display:flex;align-items:center;gap:3px;white-space:nowrap}
.eqpqc-detail-form fieldset input{accent-color:#a77a1d}
.eqpqc-item-name{grid-column:span 2}
.eqpqc-detail-actions{display:flex;grid-column:1/-1;flex-wrap:wrap;gap:5px}
.eqpqc-items-wrap{min-height:145px}
.eqpqc-dialog-footer{display:flex;min-height:47px;align-items:center;justify-content:flex-end;gap:7px;padding:7px 12px;border-top:1px solid #ccc}
.eqpqc-dialog-footer span{margin-right:auto;color:#777;font-size:12px}
@media(max-width:1000px){.eqpqc-panels{grid-template-rows:minmax(180px,1fr) minmax(180px,1fr)}.eqpqc-detail-form{grid-template-columns:1fr 1fr 1fr}.eqpqc-item-name{grid-column:span 2}}
@media(max-width:700px){.eqpqc-main{grid-template-columns:minmax(0,1fr) 76px}.eqpqc-actions{gap:4px}.eqpqc-actions button{font-size:11px;padding:3px 5px}.eqpqc-panels{grid-template-rows:minmax(190px,1fr) minmax(190px,1fr)}.eqpqc-grid{min-width:900px}.eqpqc-master-fields{grid-template-columns:1fr}.eqpqc-master-fields .eqpqc-desc{grid-column:auto}.eqpqc-detail-form{grid-template-columns:1fr 1fr}.eqpqc-item-name{grid-column:1/-1}.eqpqc-master-fields .eqpqc-after-event{padding-left:0}}
</style>
