<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type AdjustType = 0 | 1
type Mode = 'basis-add' | 'basis-edit' | 'basis-copy' | 'detail-add' | 'detail-edit' | 'detail-copy' | 'delete-basis' | 'delete-detail' | null
interface Basis { no: string; inventoryNo: string; adjustType: AdjustType; state: number; description: string; creator: string; createDate: string; reviser: string; reviseDate: string; dataStamp: number }
interface ComponentRow { componentNo: string; cassetteNo: string; slotNo: string }
interface Detail { key: string; no: string; materialNo: string; materialName: string; lotNo: string; locatorNo: string; finalLocatorNo: string; unitNo: string; orgQty: number; finalQty: number; diffQty: number; creator: string; createDate: string; reviser: string; reviseDate: string; dataStamp: number; components: ComponentRow[] }
interface RawLot { key: string; inventoryNo: string; materialNo: string; materialName: string; lotNo: string; locatorNo: string; unitNo: string; locQty: number; hasComponents: boolean; pendingIssue: boolean; components: ComponentRow[] }
interface DraftBasis { no: string; inventoryNo: string; adjustType: AdjustType; description: string }
interface DraftDetail { stockKey: string; finalQty: string; finalLocatorNo: string }
interface SavedState { bases: Basis[]; details: Detail[]; nextNumber: number; nextDetail: number }

const props = withDefaults(defineProps<{ developmentPreview?: boolean; currentUserNo?: string }>(), { developmentPreview: true, currentUserNo: '' })
const emit = defineEmits<{ close: [] }>()
const previewLabel = computed(() => props.developmentPreview ? 'DEV 假資料預覽' : 'DEV 模擬')
const storageKey = 'ymes.dev.iv204-material-adjust.v1'
const inventories = [
  { no: 'RAW-DEV-01', name: '原料庫 A' },
  { no: 'RAW-DEV-02', name: '原料冷藏庫 B' },
]
const locators = [
  { inventoryNo: 'RAW-DEV-01', no: 'A-01', name: '原料架 A-01', issueState: 2, capacity: 30 },
  { inventoryNo: 'RAW-DEV-01', no: 'A-02', name: '原料架 A-02', issueState: 2, capacity: 0 },
  { inventoryNo: 'RAW-DEV-01', no: 'A-03', name: '原料架 A-03', issueState: 2, capacity: 12 },
  { inventoryNo: 'RAW-DEV-01', no: 'A-HOLD', name: '停用儲位', issueState: 0, capacity: 10 },
  { inventoryNo: 'RAW-DEV-02', no: 'B-01', name: '原料架 B-01', issueState: 2, capacity: 0 },
  { inventoryNo: 'RAW-DEV-02', no: 'B-02', name: '原料架 B-02', issueState: 2, capacity: 18 },
]
const rawLots: RawLot[] = [
  { key: 'ra|resin|lot-001|a01', inventoryNo: 'RAW-DEV-01', materialNo: 'RM-RESIN-01', materialName: '環氧樹脂 A', lotNo: 'RES-2611-001', locatorNo: 'A-01', unitNo: 'KG', locQty: 26.5, hasComponents: false, pendingIssue: false, components: [] },
  { key: 'ra|resin|lot-001|a02', inventoryNo: 'RAW-DEV-01', materialNo: 'RM-RESIN-01', materialName: '環氧樹脂 A', lotNo: 'RES-2611-001', locatorNo: 'A-02', unitNo: 'KG', locQty: 3.5, hasComponents: false, pendingIssue: false, components: [] },
  { key: 'ra|sensor|lot-005|a03', inventoryNo: 'RAW-DEV-01', materialNo: 'RM-SENSOR-05', materialName: '感測器晶圓 S', lotNo: 'SNS-2611-005', locatorNo: 'A-03', unitNo: 'EA', locQty: 4, hasComponents: true, pendingIssue: false, components: [
    { componentNo: 'COMP-SNS-A', cassetteNo: 'CAS-01', slotNo: '01' },
    { componentNo: 'COMP-SNS-B', cassetteNo: 'CAS-01', slotNo: '02' },
    { componentNo: 'COMP-SNS-C', cassetteNo: 'CAS-02', slotNo: '01' },
  ] },
  { key: 'ra|adhesive|lot-211|a02', inventoryNo: 'RAW-DEV-01', materialNo: 'RM-ADHESIVE-02', materialName: '導電膠 B', lotNo: 'ADH-2611-211', locatorNo: 'A-02', unitNo: 'KG', locQty: 8, hasComponents: false, pendingIssue: true, components: [] },
  { key: 'rb|copper|lot-020|b01', inventoryNo: 'RAW-DEV-02', materialNo: 'RM-COPPER-03', materialName: '銅箔 C', lotNo: 'CU-2611-020', locatorNo: 'B-01', unitNo: 'RL', locQty: 32, hasComponents: false, pendingIssue: false, components: [] },
]

function initialState(): SavedState {
  return {
    bases: [
      { no: 'IA-DEV-26001', inventoryNo: 'RAW-DEV-01', adjustType: 0, state: 0, description: '盤點後調整樹脂數量。', creator: 'DEV001', createDate: '2026-11-21 09:30:00', reviser: '', reviseDate: '', dataStamp: 0 },
      { no: 'IA-DEV-26002', inventoryNo: 'RAW-DEV-01', adjustType: 1, state: 0, description: '感測器批次移至新儲位。', creator: 'DEV002', createDate: '2026-11-22 14:10:00', reviser: '', reviseDate: '', dataStamp: 0 },
      { no: 'IA-DEV-26003', inventoryNo: 'RAW-DEV-02', adjustType: 0, state: 0, description: 'DEV 草稿：另一庫房調帳。', creator: 'DEV001', createDate: '2026-11-24 11:05:00', reviser: '', reviseDate: '', dataStamp: 0 },
      { no: 'IA-DEV-25980', inventoryNo: 'RAW-DEV-01', adjustType: 0, state: 2, description: '已確認歷史假資料；IV204 不載入。', creator: 'DEV003', createDate: '2026-11-15 10:20:00', reviser: 'DEV003', reviseDate: '2026-11-15 12:00:00', dataStamp: 1 },
    ],
    details: [
      { key: 'd-26001-resin', no: 'IA-DEV-26001', materialNo: 'RM-RESIN-01', materialName: '環氧樹脂 A', lotNo: 'RES-2611-001', locatorNo: 'A-01', finalLocatorNo: 'A-01', unitNo: 'KG', orgQty: 26.5, finalQty: 24, diffQty: -2.5, creator: 'DEV001', createDate: '2026-11-21 09:35:00', reviser: '', reviseDate: '', dataStamp: 0, components: [] },
      { key: 'd-26002-sensor', no: 'IA-DEV-26002', materialNo: 'RM-SENSOR-05', materialName: '感測器晶圓 S', lotNo: 'SNS-2611-005', locatorNo: 'A-03', finalLocatorNo: 'A-02', unitNo: 'EA', orgQty: 4, finalQty: 0, diffQty: 0, creator: 'DEV002', createDate: '2026-11-22 14:16:00', reviser: '', reviseDate: '', dataStamp: 0, components: rawLots[2].components.map((item) => ({ ...item })) },
      { key: 'd-26003-copper', no: 'IA-DEV-26003', materialNo: 'RM-COPPER-03', materialName: '銅箔 C', lotNo: 'CU-2611-020', locatorNo: 'B-01', finalLocatorNo: 'B-01', unitNo: 'RL', orgQty: 32, finalQty: 30, diffQty: -2, creator: 'DEV001', createDate: '2026-11-24 11:12:00', reviser: '', reviseDate: '', dataStamp: 0, components: [] },
    ],
    nextNumber: 26004,
    nextDetail: 1,
  }
}
function loadState(): SavedState {
  try {
    const saved = localStorage.getItem(storageKey)
    if (saved) return JSON.parse(saved) as SavedState
  } catch { /* use DEV fixtures when local storage is unavailable */ }
  return initialState()
}
function nowText(): string { return new Date().toLocaleString('sv-SE', { hour12: false }).replace('T', ' ') }
function inventoryName(no: string): string { return inventories.find((row) => row.no === no)?.name ?? no }
function typeName(type: AdjustType): string { return type === 0 ? '調帳' : '儲位調撥' }
function stateName(state: number): string { return ({ 0: '未確認', 1: '處理中', 2: '已確認', 3: '失敗' } as Record<number, string>)[state] ?? String(state) }
function qtyText(value: number): string { return new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 8 }).format(value) }
function rowKey(row: Detail): string { return [row.no, row.materialNo, row.lotNo, row.locatorNo].join('|') }

const records = ref<SavedState>(loadState())
const userNo = computed(() => props.currentUserNo || 'DEVUSER')
const searchText = ref('')
const appliedSearch = ref('')
const selectedBasisNo = ref('IA-DEV-26001')
const selectedBasisNos = ref<string[]>([])
const selectedDetailKeys = ref<string[]>([])
const expandedDetailKey = ref('')
const dialogMode = ref<Mode>(null)
const notice = ref('DEV 假資料：此頁只模擬 IV204 未確認調整單，不呼叫 MES、API 或資料庫。庫存過帳由 IV201 Source=3 確認。')
const basisDraft = ref<DraftBasis>({ no: '', inventoryNo: '', adjustType: 0, description: '' })
const detailDraft = ref<DraftDetail>({ stockKey: '', finalQty: '', finalLocatorNo: '' })
const editingDetailKey = ref('')
const pendingDeleteNos = ref<string[]>([])
const pendingDeleteKeys = ref<string[]>([])

const visibleBases = computed(() => records.value.bases.filter((row) => row.state === 0 && (!appliedSearch.value || row.no.toLowerCase().includes(appliedSearch.value.toLowerCase())))
  .slice().sort((a, b) => a.no.localeCompare(b.no)))
const activeBasis = computed(() => visibleBases.value.find((row) => row.no === selectedBasisNo.value) ?? visibleBases.value[0] ?? null)
const visibleDetails = computed(() => activeBasis.value ? records.value.details.filter((row) => row.no === activeBasis.value?.no)
  .slice().sort((a, b) => [a.materialNo, a.lotNo, a.locatorNo].join('|').localeCompare([b.materialNo, b.lotNo, b.locatorNo].join('|'))) : [])
const selectedDetail = computed(() => visibleDetails.value.find((row) => rowKey(row) === selectedDetailKeys.value[0]) ?? visibleDetails.value[0] ?? null)
const inventoryOptions = computed(() => inventories)
const modalLots = computed(() => rawLots.filter((row) => row.inventoryNo === basisForDetail.value?.inventoryNo)
  .filter((row) => basisForDetail.value?.adjustType === 1 ? !row.pendingIssue : !row.hasComponents)
  .slice().sort((a, b) => [a.materialNo, a.lotNo, a.locatorNo].join('|').localeCompare([b.materialNo, b.lotNo, b.locatorNo].join('|'))))
const basisForDetail = computed(() => activeBasis.value)
const chosenLot = computed(() => modalLots.value.find((row) => row.key === detailDraft.value.stockKey) ?? null)
const destinationOptions = computed(() => locators.filter((row) => row.inventoryNo === basisForDetail.value?.inventoryNo && row.issueState === 2))
const activeModalTitle = computed(() => {
  if (dialogMode.value?.startsWith('basis-')) return dialogMode.value === 'basis-edit' ? 'Edit MaterialAdjust Basis' : dialogMode.value === 'basis-copy' ? 'Copy MaterialAdjust Basis' : 'Add MaterialAdjust Basis'
  if (dialogMode.value?.startsWith('detail-')) return dialogMode.value === 'detail-edit' ? 'Edit MaterialAdjust Detail' : dialogMode.value === 'detail-copy' ? 'Copy MaterialAdjust Detail' : 'Add MaterialAdjust Detail'
  return dialogMode.value === 'delete-basis' ? 'Delete MaterialAdjust Basis' : 'Delete MaterialAdjust Detail'
})
const isBasisMode = computed(() => dialogMode.value?.startsWith('basis-') ?? false)
const isDetailMode = computed(() => dialogMode.value?.startsWith('detail-') ?? false)
const detailType = computed(() => basisForDetail.value?.adjustType ?? 0)
const detailOrgQty = computed(() => {
  if (dialogMode.value === 'detail-edit') return records.value.details.find((row) => row.key === editingDetailKey.value)?.orgQty ?? 0
  return chosenLot.value?.locQty ?? 0
})

watch(visibleBases, (rows) => {
  if (!rows.some((row) => row.no === selectedBasisNo.value)) selectedBasisNo.value = rows[0]?.no ?? ''
}, { immediate: true })
watch(activeBasis, () => { selectedDetailKeys.value = []; expandedDetailKey.value = '' })
watch(() => detailDraft.value.stockKey, () => { if (dialogMode.value !== 'detail-edit') detailDraft.value.finalLocatorNo = '' })

function persist(): void {
  try { localStorage.setItem(storageKey, JSON.stringify(records.value)) } catch { /* state remains available for this page session */ }
}
function runQuery(): void { appliedSearch.value = searchText.value.trim(); selectedBasisNos.value = []; notice.value = 'DEV 查詢完成：僅列 State=0 未確認草稿。' }
function clearQuery(): void { searchText.value = ''; appliedSearch.value = ''; selectedBasisNos.value = []; notice.value = 'DEV 查詢條件已清除；只載入未確認草稿。' }
function selectBasis(row: Basis): void { selectedBasisNo.value = row.no; selectedBasisNos.value = selectedBasisNos.value.includes(row.no) ? selectedBasisNos.value.filter((no) => no !== row.no) : [...selectedBasisNos.value, row.no] }
function toggleBasis(row: Basis): void { selectedBasisNo.value = row.no }
function toggleBasisAll(): void { selectedBasisNos.value = selectedBasisNos.value.length === visibleBases.value.length ? [] : visibleBases.value.map((row) => row.no) }
function toggleDetail(row: Detail): void { const key = rowKey(row); selectedDetailKeys.value = selectedDetailKeys.value.includes(key) ? selectedDetailKeys.value.filter((item) => item !== key) : [key]; expandedDetailKey.value = expandedDetailKey.value === key ? '' : key }
function generateDocumentNo(): void { basisDraft.value.no = 'IA-DEV-' + String(records.value.nextNumber).padStart(5, '0') }

function openBasis(mode: 'add' | 'edit' | 'copy'): void {
  const row = activeBasis.value
  if (mode !== 'add' && !row) return
  if (mode === 'edit' && row?.state !== 0) return
  dialogMode.value = ('basis-' + mode) as Mode
  basisDraft.value = mode === 'add'
    ? { no: '', inventoryNo: '', adjustType: 0, description: '' }
    : { no: mode === 'edit' ? row!.no : '', inventoryNo: row!.inventoryNo, adjustType: row!.adjustType, description: row!.description }
}
function saveBasis(): void {
  const draft = basisDraft.value
  const no = draft.no.trim()
  if (!no || !draft.inventoryNo) { notice.value = '請輸入調整單號並選擇原料庫房。'; return }
  const mode = dialogMode.value
  const existing = records.value.bases.find((row) => row.no === no)
  if (mode !== 'basis-edit' && existing) { notice.value = '調整單號已存在，請使用不同單號。'; return }
  const time = nowText()
  if (mode === 'basis-edit') {
    const row = records.value.bases.find((item) => item.no === selectedBasisNo.value)
    if (!row || row.state !== 0) { notice.value = '只有未確認單據可編輯。'; return }
    row.description = draft.description; row.reviser = userNo.value; row.reviseDate = time; row.dataStamp += 1
  } else {
    records.value.bases.push({ no, inventoryNo: draft.inventoryNo, adjustType: draft.adjustType, state: 0, description: draft.description, creator: userNo.value, createDate: time, reviser: '', reviseDate: '', dataStamp: 0 })
    records.value.nextNumber += 1
    selectedBasisNo.value = no
    appliedSearch.value = ''
    searchText.value = ''
  }
  persist(); dialogMode.value = null; notice.value = mode === 'basis-copy' ? '主檔已複製為新草稿；明細不會隨主檔 Copy。' : mode === 'basis-edit' ? '主檔描述已更新。' : '未確認庫存調整單已建立。'
}
function requestDeleteBasis(): void {
  const targets = selectedBasisNos.value.length ? selectedBasisNos.value : activeBasis.value ? [activeBasis.value.no] : []
  if (!targets.length) return
  pendingDeleteNos.value = targets.filter((no) => records.value.bases.some((row) => row.no === no && row.state === 0))
  if (!pendingDeleteNos.value.length) { notice.value = '只有未確認單據可刪除。'; return }
  dialogMode.value = 'delete-basis'
}
function confirmDeleteBasis(): void {
  const set = new Set(pendingDeleteNos.value)
  records.value.bases = records.value.bases.filter((row) => !set.has(row.no))
  records.value.details = records.value.details.filter((row) => !set.has(row.no))
  selectedBasisNos.value = []; selectedBasisNo.value = visibleBases.value[0]?.no ?? ''
  persist(); dialogMode.value = null; notice.value = '已刪除選取的未確認主檔與明細。舊 Core 不清除 Component 子表；DEV 清除頁面內對應子列。'
}

function openDetail(mode: 'add' | 'edit' | 'copy'): void {
  const basis = activeBasis.value
  if (!basis || basis.state !== 0) return
  const row = mode === 'add' ? null : selectedDetail.value
  if (mode !== 'add' && !row) return
  editingDetailKey.value = mode === 'edit' ? row!.key : ''
  dialogMode.value = ('detail-' + mode) as Mode
  detailDraft.value = {
    stockKey: row ? rawLots.find((stock) => stock.inventoryNo === basis.inventoryNo && stock.materialNo === row.materialNo && stock.lotNo === row.lotNo && stock.locatorNo === row.locatorNo)?.key ?? '' : '',
    finalQty: row && basis.adjustType === 0 ? String(row.finalQty) : '',
    finalLocatorNo: row && basis.adjustType === 1 ? row.finalLocatorNo : '',
  }
}
function saveDetail(): void {
  const basis = activeBasis.value
  const mode = dialogMode.value
  const existingDetail = mode === 'detail-edit' ? records.value.details.find((row) => row.key === editingDetailKey.value) : null
  const source = existingDetail ? rawLots.find((row) => row.inventoryNo === basis?.inventoryNo && row.materialNo === existingDetail.materialNo && row.lotNo === existingDetail.lotNo && row.locatorNo === existingDetail.locatorNo) : chosenLot.value
  if (!basis || basis.state !== 0 || !source) { notice.value = '請選擇有效庫存批次。'; return }
  if (basis.adjustType === 0 && (!detailDraft.value.finalQty || !Number.isFinite(Number(detailDraft.value.finalQty)) || Number(detailDraft.value.finalQty) <= 0)) { notice.value = '調帳 FinalQty 必須是大於 0 的數值。'; return }
  if (basis.adjustType === 1 && (!detailDraft.value.finalLocatorNo || detailDraft.value.finalLocatorNo === source.locatorNo)) { notice.value = '儲位調撥必須選擇與來源不同的有效儲位。'; return }
  if (mode !== 'detail-edit') {
    const duplicate = records.value.details.some((row) => {
      const parent = records.value.bases.find((item) => item.no === row.no)
      return parent && parent.state !== 2 && parent.inventoryNo === basis.inventoryNo && row.locatorNo === source.locatorNo && row.materialNo === source.materialNo && row.lotNo === source.lotNo
    })
    if (duplicate) { notice.value = '相同庫房／來源儲位／料號／批號已有未確認或未完成的調整明細。'; return }
  }
  const time = nowText()
  if (existingDetail) {
    const finalQty = basis.adjustType === 0 ? Number(detailDraft.value.finalQty) : 0
    existingDetail.finalQty = finalQty; existingDetail.diffQty = basis.adjustType === 0 ? finalQty - existingDetail.orgQty : 0
    existingDetail.finalLocatorNo = basis.adjustType === 0 ? existingDetail.locatorNo : detailDraft.value.finalLocatorNo
    existingDetail.reviser = userNo.value; existingDetail.reviseDate = time; existingDetail.dataStamp += 1
  } else {
    const finalQty = basis.adjustType === 0 ? Number(detailDraft.value.finalQty) : 0
    const detail: Detail = {
      key: 'd-' + String(records.value.nextDetail++), no: basis.no, materialNo: source.materialNo, materialName: source.materialName, lotNo: source.lotNo,
      locatorNo: source.locatorNo, finalLocatorNo: basis.adjustType === 0 ? source.locatorNo : detailDraft.value.finalLocatorNo,
      unitNo: source.unitNo, orgQty: source.locQty, finalQty, diffQty: basis.adjustType === 0 ? finalQty - source.locQty : 0,
      creator: userNo.value, createDate: time, reviser: '', reviseDate: '', dataStamp: 0,
      components: basis.adjustType === 1 ? source.components.map((item) => ({ ...item })) : [],
    }
    records.value.details.push(detail)
  }
  persist(); dialogMode.value = null; notice.value = mode === 'detail-edit' ? '明細已更新；DEV 以本地狀態模擬。' : mode === 'detail-copy' ? '明細已複製為新的調整列。' : '調整明細已新增。'
}
function requestDeleteDetail(): void {
  if (activeBasis.value?.state !== 0) return
  pendingDeleteKeys.value = selectedDetailKeys.value.length ? selectedDetailKeys.value : selectedDetail.value ? [rowKey(selectedDetail.value)] : []
  if (!pendingDeleteKeys.value.length) return
  dialogMode.value = 'delete-detail'
}
function confirmDeleteDetail(): void {
  const keys = new Set(pendingDeleteKeys.value)
  records.value.details = records.value.details.filter((row) => !keys.has(rowKey(row)))
  selectedDetailKeys.value = []; expandedDetailKey.value = ''
  persist(); dialogMode.value = null; notice.value = '已刪除選取的調整明細。'
}
function closeDialog(): void { dialogMode.value = null; pendingDeleteNos.value = []; pendingDeleteKeys.value = [] }
</script>

<template>
  <main class="adjust-page" aria-label="開立庫存調整單 DEV 預覽">
    <header class="page-head">
      <div><p class="eyebrow">INV / IV204 · {{ previewLabel }}</p><h1>開立庫存調整單</h1><p class="sub">原物料倉 · State=0 未確認主檔與明細維護</p></div>
      <button type="button" class="close" title="Close (X)" @click="emit('close')">×</button>
    </header>
    <p class="notice" role="status">{{ notice }}</p>

    <section class="query-panel" aria-label="庫存調整單查詢">
      <label>Material Adjust No<input v-model.trim="searchText" placeholder="輸入調整單號" @keyup.enter="runQuery" /></label>
      <div class="query-actions"><span>WinForms 啟動與 Query 固定限制 State=0。</span><div><button type="button" @click="clearQuery">Clear</button><button type="button" class="primary" @click="runQuery">Query</button></div></div>
    </section>

    <section class="basis-panel panel" aria-label="庫存調整單主檔">
      <header class="panel-title"><div><h2>MaterialAdjust Basis</h2><span>{{ visibleBases.length }} 筆未確認單據</span></div><small>{{ selectedBasisNos.length }} 筆勾選</small></header>
      <div class="table-wrap"><table class="basis-table"><thead><tr><th><input aria-label="全選主檔" type="checkbox" :checked="visibleBases.length > 0 && selectedBasisNos.length === visibleBases.length" :disabled="!visibleBases.length" @change="toggleBasisAll" /></th><th>MaterialAdjustNo</th><th>InventoryNo</th><th>AdjustType</th><th>State</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
        <tbody><tr v-for="row in visibleBases" :key="row.no" :class="{ active: activeBasis?.no === row.no }" @click="toggleBasis(row)"><td><input aria-label="選取主檔" type="checkbox" :checked="selectedBasisNos.includes(row.no)" @click.stop @change="selectBasis(row)" /></td><td class="mono">{{ row.no }}</td><td>{{ row.inventoryNo }}</td><td>{{ typeName(row.adjustType) }}</td><td><span class="state">{{ stateName(row.state) }}</span></td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser || '—' }}</td><td>{{ row.reviseDate || '—' }}</td></tr>
          <tr v-if="!visibleBases.length"><td colspan="10" class="empty">沒有符合條件的未確認庫存調整單。</td></tr></tbody></table></div>
      <footer class="action-bar"><span>主檔編輯只允許修改 Description；Copy 會開新主檔，不複製明細。</span><div><button type="button" @click="openBasis('add')">ADD</button><button type="button" :disabled="!activeBasis" @click="openBasis('edit')">EDIT</button><button type="button" :disabled="!activeBasis" @click="requestDeleteBasis">DEL</button><button type="button" :disabled="!activeBasis" @click="openBasis('copy')">Copy</button></div></footer>
    </section>

    <section class="detail-panel panel" aria-label="庫存調整單明細">
      <header class="panel-title"><div><h2>MaterialAdjust Detail</h2><span v-if="activeBasis">{{ activeBasis.no }} · {{ inventoryName(activeBasis.inventoryNo) }} · {{ typeName(activeBasis.adjustType) }}</span><span v-else>請先選擇未確認主檔</span></div><small>{{ visibleDetails.length }} 筆明細</small></header>
      <div class="table-wrap"><table class="detail-table"><thead><tr><th><input aria-label="全選明細" type="checkbox" :checked="visibleDetails.length > 0 && selectedDetailKeys.length === visibleDetails.length" :disabled="!visibleDetails.length" @change="selectedDetailKeys = selectedDetailKeys.length === visibleDetails.length ? [] : visibleDetails.map(rowKey)" /></th><th>MaterialNo</th><th>MaterialName</th><th>MaterialLotNo</th><th>LocatorNo</th><th>FinalLocatorNo</th><th>UnitNo</th><th>OrgQty</th><th>FinalQty</th><th>DiffQty</th><th>Creator / CreateDate</th></tr></thead>
        <tbody><template v-for="row in visibleDetails" :key="row.key"><tr :class="{ active: selectedDetailKeys.includes(rowKey(row)) }" @click="toggleDetail(row)"><td><input aria-label="選取明細" type="checkbox" :checked="selectedDetailKeys.includes(rowKey(row))" @click.stop @change="toggleDetail(row)" /></td><td class="mono">{{ row.materialNo }}</td><td>{{ row.materialName }}</td><td class="mono">{{ row.lotNo }}</td><td>{{ row.locatorNo }}</td><td>{{ row.finalLocatorNo }}</td><td>{{ row.unitNo }}</td><td class="num">{{ qtyText(row.orgQty) }}</td><td class="num">{{ qtyText(row.finalQty) }}</td><td class="num" :class="row.diffQty < 0 ? 'negative' : row.diffQty > 0 ? 'positive' : ''">{{ qtyText(row.diffQty) }}</td><td>{{ row.creator }}<br /><small>{{ row.createDate }}</small></td></tr>
            <tr v-if="expandedDetailKey === rowKey(row) && row.components.length" class="component-row"><td></td><td colspan="10"><div class="component-list"><strong>Component Snapshot</strong><span v-for="item in row.components" :key="item.componentNo + item.slotNo">{{ item.componentNo }} · Cassette {{ item.cassetteNo || '—' }} · Slot {{ item.slotNo || '—' }}</span></div></td></tr>
          </template><tr v-if="activeBasis && !visibleDetails.length"><td colspan="11" class="empty">此調整單尚無明細。</td></tr><tr v-if="!activeBasis"><td colspan="11" class="empty">選取一筆主檔後載入明細。</td></tr></tbody></table></div>
      <footer class="action-bar"><span>{{ activeBasis ? (activeBasis.adjustType === 0 ? '調帳：輸入 FinalQty (>0)，DiffQty=FinalQty−OrgQty。' : '儲位調撥：指定不同目的儲位；過帳以 OrgQty 移轉。') : '—' }}</span><div><button type="button" :disabled="!activeBasis" @click="openDetail('add')">ADD</button><button type="button" :disabled="!selectedDetail" @click="openDetail('edit')">EDIT</button><button type="button" :disabled="!selectedDetail" @click="requestDeleteDetail">DEL</button><button type="button" :disabled="!selectedDetail" @click="openDetail('copy')">Copy</button></div></footer>
    </section>
    <aside class="source-note"><strong>DEV 範圍</strong><span>新增、編輯、刪除與 Copy 只更新此瀏覽器的 DEV 假資料。此表單沒有確認／過帳按鈕，實際調整由 IV201 Source=3 處理。</span></aside>

    <div v-if="dialogMode" class="backdrop" role="presentation" @click.self="closeDialog">
      <section class="dialog" role="dialog" aria-modal="true" :aria-label="activeModalTitle">
        <header><div><p class="eyebrow">INV / IV204</p><h2>{{ activeModalTitle }}</h2></div><button type="button" class="close" @click="closeDialog">×</button></header>
        <template v-if="isBasisMode">
          <div class="form-grid">
            <label>MaterialAdjustNo<div class="number-field"><input v-model.trim="basisDraft.no" :readonly="dialogMode === 'basis-edit'" placeholder="輸入或取得單號" /><button v-if="dialogMode !== 'basis-edit'" type="button" @click="generateDocumentNo">Get No.</button></div></label>
            <label>InventoryNo<select v-model="basisDraft.inventoryNo" :disabled="dialogMode === 'basis-edit'"><option value="">選擇原料庫房</option><option v-for="row in inventoryOptions" :key="row.no" :value="row.no">{{ row.no }} · {{ row.name }}</option></select></label>
            <fieldset class="type-choice"><legend>AdjustType</legend><label><input v-model.number="basisDraft.adjustType" type="radio" :value="0" :disabled="dialogMode === 'basis-edit'" /> Adjust · 數量調帳</label><label><input v-model.number="basisDraft.adjustType" type="radio" :value="1" :disabled="dialogMode === 'basis-edit'" /> LocatorAdjust · 儲位調撥</label></fieldset>
            <label class="wide">Description<textarea v-model="basisDraft.description" rows="3" /></label>
          </div>
          <p class="dialog-hint">Inventory lookup 僅列 IssueState=2 且 InventoryType=0 的原料庫房。DEV 單號由本機序號產生器模擬。</p>
          <footer><button type="button" @click="closeDialog">Cancel</button><button type="button" class="primary" @click="saveBasis">Save</button></footer>
        </template>
        <template v-else-if="isDetailMode">
          <div class="form-grid">
            <label>MaterialAdjustNo<input :value="activeBasis?.no || ''" readonly /></label>
            <label>InventoryNo<input :value="activeBasis?.inventoryNo || ''" readonly /></label>
            <label class="wide">Material / Lot / Source Locator
              <select v-model="detailDraft.stockKey" :disabled="dialogMode === 'detail-edit'"><option value="">選擇庫存批次</option><option v-for="row in modalLots" :key="row.key" :value="row.key">{{ row.materialNo }} · {{ row.materialName }} · {{ row.lotNo }} · {{ row.locatorNo }} · {{ qtyText(row.locQty) }} {{ row.unitNo }}</option></select>
            </label>
            <label>UnitNo<input :value="chosenLot?.unitNo || (dialogMode === 'detail-edit' ? selectedDetail?.unitNo : '')" readonly /></label>
            <label>OrgQty<input :value="qtyText(detailOrgQty)" readonly /></label>
            <label v-if="detailType === 0">FinalQty<input v-model.trim="detailDraft.finalQty" inputmode="decimal" placeholder="必須大於 0" /></label>
            <label v-else>FinalLocatorNo<select v-model="detailDraft.finalLocatorNo"><option value="">選擇目的儲位</option><option v-for="row in destinationOptions" :key="row.no" :value="row.no" :disabled="row.no === (chosenLot?.locatorNo || selectedDetail?.locatorNo)">{{ row.no }} · {{ row.name }}<template v-if="row.capacity"> · Capacity {{ row.capacity }}</template></option></select></label>
            <label v-if="detailType === 0">DiffQty<input :value="qtyText((Number(detailDraft.finalQty) || 0) - detailOrgQty)" readonly /></label>
          </div>
          <p class="dialog-hint">{{ detailType === 0 ? '調帳候選會排除 RawInventoryComponent 物料。' : '儲位調撥候選會排除有未確認領料單的物料；容量與領料檢查在 IV201 過帳時執行。' }}<template v-if="chosenLot?.components.length">此批有 {{ chosenLot.components.length }} 筆元件快照，新增後附在明細子列。</template></p>
          <footer><button type="button" @click="closeDialog">Cancel</button><button type="button" class="primary" @click="saveDetail">Save</button></footer>
        </template>
        <template v-else>
          <p class="delete-copy" v-if="dialogMode === 'delete-basis'">確定刪除 {{ pendingDeleteNos.length }} 筆未確認主檔及其明細？這只會改變 DEV 假資料。</p>
          <p class="delete-copy" v-else>確定刪除 {{ pendingDeleteKeys.length }} 筆調整明細？這只會改變 DEV 假資料。</p>
          <footer><button type="button" @click="closeDialog">Cancel</button><button type="button" class="danger" @click="dialogMode === 'delete-basis' ? confirmDeleteBasis() : confirmDeleteDetail()">Delete</button></footer>
        </template>
      </section>
    </div>
  </main>
</template>

<style scoped>
.adjust-page { height: 100%; min-height: 0; box-sizing: border-box; display: flex; flex-direction: column; gap: 9px; overflow: auto; padding: 10px 12px 14px; background: #f7f5ee; color: #26251f; font: inherit; }
.page-head, .panel-title, .action-bar, .query-actions, .dialog header, .dialog footer { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.page-head { flex: 0 0 auto; padding: 2px 2px 0; }
.page-head h1 { margin: 2px 0; font-size: 21px; }
.eyebrow { margin: 0; color: #777366; font-size: 11px; letter-spacing: .08em; text-transform: uppercase; }
.sub, .panel-title span, .action-bar > span, .query-actions > span { color: #777366; font-size: 12px; }
.sub { margin: 0; }
.close { border: 0; background: transparent; color: #555247; font-size: 24px; cursor: pointer; }
.notice { flex: 0 0 auto; margin: 0; padding: 7px 10px; border: 1px solid #d9d1b8; border-radius: 4px; background: #fffbed; color: #5b543f; font-size: 12px; }
.query-panel, .panel { border: 1px solid #d9d5c8; border-radius: 4px; background: #fff; box-shadow: 0 1px 2px #16130a0a; }
.query-panel { flex: 0 0 auto; display: grid; grid-template-columns: minmax(220px, 1fr) 2fr; align-items: end; gap: 12px; padding: 10px; }
label { display: flex; flex-direction: column; gap: 4px; color: #5e5a4e; font-size: 12px; }
input, select, textarea { min-width: 0; box-sizing: border-box; min-height: 30px; border: 1px solid #ccc7b8; border-radius: 3px; background: #fff; color: #26251f; padding: 5px 7px; font: inherit; }
input[readonly] { background: #f3f1eb; color: #777366; }
button { min-height: 29px; border: 1px solid #b9b3a3; border-radius: 3px; background: #f7f5ee; color: #302f29; padding: 4px 10px; font: inherit; cursor: pointer; }
button:hover:not(:disabled) { background: #ede9dc; }
button:disabled { cursor: not-allowed; opacity: .48; }
button.primary { border-color: #454237; background: #454237; color: #fff; }
button.danger { border-color: #a14035; background: #a14035; color: #fff; }
.query-actions { align-self: end; }
.query-actions > div, .action-bar > div { display: flex; gap: 6px; }
.panel { flex: 1 1 0; display: flex; min-height: 158px; flex-direction: column; overflow: hidden; }
.panel-title { flex: 0 0 auto; min-height: 41px; padding: 6px 10px; border-bottom: 1px solid #e5e1d6; }
.panel-title h2 { display: inline-block; margin: 0 8px 0 0; font-size: 14px; }
.panel-title small { color: #777366; font-size: 11px; }
.table-wrap { min-height: 0; flex: 1 1 auto; overflow: auto; }
table { width: 100%; border-collapse: collapse; white-space: nowrap; font-size: 12px; }
th, td { padding: 6px 8px; border-bottom: 1px solid #ebe8df; text-align: left; }
th { position: sticky; top: 0; z-index: 1; background: #f1efe8; color: #5b584e; font-weight: 600; }
tbody tr { cursor: pointer; }
tbody tr:hover { background: #faf8f0; }
tbody tr.active { background: #e9f0e8; }
td input[type=checkbox], th input[type=checkbox] { min-height: auto; margin: 0; accent-color: #525b43; }
.mono { font-family: Consolas, 'Courier New', monospace; }
.num { text-align: right; font-variant-numeric: tabular-nums; }
.negative { color: #a04437; }
.positive { color: #3b6b45; }
.state { display: inline-block; border: 1px solid #d3c89e; border-radius: 9px; background: #f9f2d8; padding: 2px 8px; color: #63562b; }
.action-bar { flex: 0 0 auto; min-height: 38px; padding: 5px 9px; border-top: 1px solid #e5e1d6; background: #fbfaf6; }
.action-bar button { min-width: 56px; }
.empty { height: 55px; color: #898578; text-align: center; }
.component-row td { background: #f8f7f2; }
.component-list { display: flex; flex-wrap: wrap; gap: 8px 18px; color: #625f53; }
.component-list strong { color: #36342e; }
.source-note { flex: 0 0 auto; display: flex; gap: 10px; padding: 5px 2px; color: #777366; font-size: 11px; }
.source-note strong { color: #4b493f; }
.backdrop { position: fixed; inset: 0; z-index: 20; display: grid; place-items: center; padding: 16px; background: #211f1980; }
.dialog { width: min(620px, 100%); max-height: min(85vh, 720px); overflow: auto; border: 1px solid #c9c3b3; border-radius: 5px; background: #fff; box-shadow: 0 18px 55px #17150f40; }
.dialog > header { padding: 11px 15px; border-bottom: 1px solid #e5e1d6; }
.dialog h2 { margin: 2px 0 0; font-size: 17px; }
.form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; padding: 15px; }
.form-grid .wide { grid-column: 1 / -1; }
.number-field { display: flex; gap: 5px; }
.number-field input { flex: 1; }
.type-choice { grid-column: 1 / -1; display: flex; flex-wrap: wrap; gap: 18px; margin: 0; border: 1px solid #ddd8ca; border-radius: 3px; padding: 8px 10px 10px; }
.type-choice legend { color: #5e5a4e; font-size: 12px; }
.type-choice label { display: flex; flex-direction: row; align-items: center; gap: 5px; }
.type-choice input { min-height: auto; accent-color: #525b43; }
.dialog-hint, .delete-copy { margin: 0; padding: 0 15px 13px; color: #777366; font-size: 12px; }
.dialog footer { padding: 10px 15px; border-top: 1px solid #e5e1d6; }
@media (max-width: 900px) { .query-panel { grid-template-columns: 1fr; } .panel { min-height: 185px; } .action-bar { align-items: flex-start; flex-direction: column; } .action-bar > div { flex-wrap: wrap; } }
@media (max-width: 560px) { .form-grid { grid-template-columns: 1fr; } .form-grid .wide, .type-choice { grid-column: auto; } .type-choice { flex-direction: column; gap: 7px; } }
</style>
