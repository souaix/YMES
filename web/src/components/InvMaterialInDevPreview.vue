<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type FormMode = 'add' | 'edit' | 'copy' | null
type IssueState = 0 | 2 | 3
interface InventoryOption { inventoryNo: string; inventoryName: string; inventoryType: 0 }
interface VendorOption { vendorNo: string; vendorName: string }
interface MaterialOption {
  materialNo: string
  materialName: string
  vendorNo: string
  unitNo: string
  checkLotNo: boolean
  expireDateRequired: boolean
  qcItemCount: number
}
interface BasisRow {
  materialInNo: string
  inventoryNo: string
  vendorNo: string
  description: string
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
  state: IssueState
  dataStamp: number
}
interface DetailRow {
  materialInNo: string
  materialNo: string
  materialName: string
  materialLotNo: string
  locatorNo: string
  unitNo: string
  qty: number
  qcFormNo: string
  qcStatus: number | null
  expireDate: string
  manufactureDate: string
  graceDate: string
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
  dataStamp: number
}
interface DevState { version: 1; serial: number; qcSerial: number; generateIncomingQc: boolean; bases: BasisRow[]; details: DetailRow[] }
interface BasisDraft { materialInNo: string; inventoryNo: string; vendorNo: string; description: string }
interface DetailDraft {
  materialNo: string
  materialLotNo: string
  locatorNo: string
  unitNo: string
  qty: string
  expireDate: string
  manufactureDate: string
  graceDate: string
}

const props = withDefaults(defineProps<{ developmentPreview?: boolean; currentUserNo?: string }>(), { developmentPreview: false, currentUserNo: '' })
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.inv.material-in.v1'
const inventories: InventoryOption[] = [
  { inventoryNo: 'RAW-A', inventoryName: '一廠原物料倉', inventoryType: 0 },
  { inventoryNo: 'RAW-B', inventoryName: '二廠原物料倉', inventoryType: 0 },
]
const vendors: VendorOption[] = [
  { vendorNo: 'V-DEV-01', vendorName: '原料供應商 A' },
  { vendorNo: 'V-DEV-02', vendorName: '金屬材料供應商 B' },
]
const materials: MaterialOption[] = [
  { materialNo: 'RM-RESIN-01', materialName: '環氧樹脂 A', vendorNo: 'V-DEV-01', unitNo: 'KG', checkLotNo: true, expireDateRequired: true, qcItemCount: 2 },
  { materialNo: 'RM-SOLVENT-02', materialName: '清洗溶劑 B', vendorNo: 'V-DEV-01', unitNo: 'L', checkLotNo: false, expireDateRequired: false, qcItemCount: 0 },
  { materialNo: 'RM-COPPER-03', materialName: '銅箔 C', vendorNo: 'V-DEV-02', unitNo: 'RL', checkLotNo: true, expireDateRequired: false, qcItemCount: 1 },
]
const locators: Record<string, string[]> = { 'RAW-A': ['A-01-01', 'A-01-02'], 'RAW-B': [] }

function makeSeed(): DevState {
  const bases: BasisRow[] = [
    { materialInNo: 'MIN-DEV-26001', inventoryNo: 'RAW-A', vendorNo: 'V-DEV-01', description: 'DEV 假資料：一般進料與待檢批號。', creator: 'DEV001', createDate: '2026-11-18 09:12:00', reviser: '', reviseDate: '', state: 0, dataStamp: 1 },
    { materialInNo: 'MIN-DEV-26002', inventoryNo: 'RAW-B', vendorNo: 'V-DEV-02', description: 'DEV 假資料：沒有 Locator 管理的進料。', creator: 'DEV002', createDate: '2026-11-20 14:36:00', reviser: 'DEV002', reviseDate: '2026-11-21 10:02:00', state: 0, dataStamp: 2 },
  ]
  const details: DetailRow[] = [
    { materialInNo: 'MIN-DEV-26001', materialNo: 'RM-RESIN-01', materialName: '環氧樹脂 A', materialLotNo: 'RES-2611-001', locatorNo: 'A-01-01', unitNo: 'KG', qty: 18, qcFormNo: 'QCF-DEV-0001', qcStatus: 0, expireDate: '2027-11-18', manufactureDate: '2026-11-01', graceDate: '2027-10-18', creator: 'DEV001', createDate: '2026-11-18 09:14:00', reviser: '', reviseDate: '', dataStamp: 1 },
    { materialInNo: 'MIN-DEV-26001', materialNo: 'RM-SOLVENT-02', materialName: '清洗溶劑 B', materialLotNo: 'N/A', locatorNo: 'A-01-02', unitNo: 'L', qty: 6, qcFormNo: '', qcStatus: null, expireDate: '', manufactureDate: '', graceDate: '', creator: 'DEV001', createDate: '2026-11-18 09:18:00', reviser: '', reviseDate: '', dataStamp: 1 },
    { materialInNo: 'MIN-DEV-26002', materialNo: 'RM-COPPER-03', materialName: '銅箔 C', materialLotNo: 'CU-2611-009', locatorNo: 'N/A', unitNo: 'RL', qty: 4, qcFormNo: 'QCF-DEV-0002', qcStatus: 1, expireDate: '', manufactureDate: '2026-11-17', graceDate: '', creator: 'DEV002', createDate: '2026-11-20 14:40:00', reviser: '', reviseDate: '', dataStamp: 1 },
  ]
  return { version: 1, serial: 3, qcSerial: 3, generateIncomingQc: true, bases, details }
}
function readState(): DevState {
  try {
    const raw = window.localStorage.getItem(storageKey)
    if (raw) {
      const value = JSON.parse(raw) as DevState
      if (value?.version === 1 && Array.isArray(value.bases) && Array.isArray(value.details)) return value
    }
  } catch { /* use DEV fixtures when local storage is unavailable */ }
  return makeSeed()
}

const state = ref<DevState>(readState())
const queryMaterialInNo = ref('')
const selectedBasisNo = ref(state.value.bases[0]?.materialInNo ?? '')
const selectedBasisNos = ref<string[]>([])
const selectedDetailId = ref('')
const selectedDetailIds = ref<string[]>([])
const notice = ref('DEV 假資料：只在本機瀏覽器模擬，不呼叫 MES 或正式 API。')
const basisMode = ref<FormMode>(null)
const basisDraft = ref<BasisDraft>(blankBasisDraft())
const basisError = ref('')
const detailMode = ref<FormMode>(null)
const detailDraft = ref<DetailDraft>(blankDetailDraft())
const detailError = ref('')
const printOpen = ref(false)

const visibleBases = computed(() => {
  const query = queryMaterialInNo.value.trim().toLocaleUpperCase()
  return state.value.bases.filter((row) => row.state === 0 && (!query || row.materialInNo.toLocaleUpperCase().includes(query)))
    .sort((a, b) => a.materialInNo.localeCompare(b.materialInNo))
})
const activeBasis = computed(() => visibleBases.value.find((row) => row.materialInNo === selectedBasisNo.value) ?? visibleBases.value[0] ?? null)
const currentDetails = computed(() => activeBasis.value
  ? state.value.details.filter((row) => row.materialInNo === activeBasis.value?.materialInNo)
    .sort((a, b) => a.materialNo.localeCompare(b.materialNo) || a.materialLotNo.localeCompare(b.materialLotNo) || a.locatorNo.localeCompare(b.locatorNo))
  : [])
const activeDetail = computed(() => currentDetails.value.find((row) => detailId(row) === selectedDetailId.value) ?? currentDetails.value[0] ?? null)
const selectedBasisRows = computed(() => visibleBases.value.filter((row) => selectedBasisNos.value.includes(row.materialInNo)))
const basisActionRows = computed(() => {
  const selected = [...selectedBasisRows.value]
  if (activeBasis.value && !selected.some((row) => row.materialInNo === activeBasis.value?.materialInNo)) selected.push(activeBasis.value)
  return selected
})
const selectedDetailRows = computed(() => currentDetails.value.filter((row) => selectedDetailIds.value.includes(detailId(row))))
const detailActionRows = computed(() => {
  const selected = [...selectedDetailRows.value]
  const active = activeDetail.value
  if (active && !selected.some((row) => detailId(row) === detailId(active))) selected.push(active)
  return selected
})
const materialCount = computed(() => currentDetails.value.length)
const totalDetailQty = computed(() => currentDetails.value.reduce((sum, row) => sum + row.qty, 0))
const linkedQcCount = computed(() => currentDetails.value.filter((row) => row.qcFormNo).length)
const masterAudit = computed(() => activeBasis.value ? `${activeBasis.value.creator} · ${activeBasis.value.createDate}${activeBasis.value.reviser ? ` · 修改 ${activeBasis.value.reviser} ${activeBasis.value.reviseDate}` : ''}` : '—')
const availableMaterials = computed(() => materials.filter((item) => item.vendorNo === (activeBasis.value?.vendorNo ?? basisDraft.value.vendorNo)))
const draftMaterial = computed(() => materials.find((item) => item.materialNo === detailDraft.value.materialNo) ?? null)
const locatorOptions = computed(() => locators[detailMode.value ? (activeBasis.value?.inventoryNo ?? '') : ''] ?? [])
const printRows = computed(() => {
  const rows = basisActionRows.value.length ? basisActionRows.value : (activeBasis.value ? [activeBasis.value] : [])
  return rows.map((basis) => ({ basis, details: state.value.details.filter((row) => row.materialInNo === basis.materialInNo) }))
})

watch(visibleBases, (rows) => {
  if (!rows.some((row) => row.materialInNo === selectedBasisNo.value)) selectedBasisNo.value = rows[0]?.materialInNo ?? ''
  selectedBasisNos.value = selectedBasisNos.value.filter((no) => rows.some((row) => row.materialInNo === no))
})
watch(currentDetails, (rows) => {
  if (!rows.some((row) => detailId(row) === selectedDetailId.value)) selectedDetailId.value = rows[0] ? detailId(rows[0]) : ''
  selectedDetailIds.value = selectedDetailIds.value.filter((id) => rows.some((row) => detailId(row) === id))
})
watch(state, (value) => {
  try { window.localStorage.setItem(storageKey, JSON.stringify(value)) } catch { /* DEV may run with storage disabled */ }
}, { deep: true })

function blankBasisDraft(): BasisDraft { return { materialInNo: '', inventoryNo: '', vendorNo: '', description: '' } }
function blankDetailDraft(): DetailDraft { return { materialNo: '', materialLotNo: '', locatorNo: '', unitNo: '', qty: '', expireDate: '', manufactureDate: '', graceDate: '' } }
function detailId(row: DetailRow): string { return `${row.materialInNo}|${row.materialNo}|${row.materialLotNo}|${row.locatorNo}` }
function serialNo(prefix: string, value: number): string { return `${prefix}-DEV-${String(value).padStart(5, '0')}` }
function vendorName(vendorNo: string): string { return vendors.find((row) => row.vendorNo === vendorNo)?.vendorName ?? vendorNo }
function inventoryName(inventoryNo: string): string { return inventories.find((row) => row.inventoryNo === inventoryNo)?.inventoryName ?? inventoryNo }
function formatQty(value: number): string { return new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 6 }).format(value) }
function qcStatusLabel(status: number | null): string { return status === null ? '未產生' : status === 0 ? '待檢' : status === 1 ? '檢驗中' : '已完成' }
function setBasisSelection(row: BasisRow, checked: boolean): void {
  selectedBasisNos.value = checked ? [...new Set([...selectedBasisNos.value, row.materialInNo])] : selectedBasisNos.value.filter((no) => no !== row.materialInNo)
  selectedBasisNo.value = row.materialInNo
}
function setDetailSelection(row: DetailRow, checked: boolean): void {
  const id = detailId(row)
  selectedDetailIds.value = checked ? [...new Set([...selectedDetailIds.value, id])] : selectedDetailIds.value.filter((value) => value !== id)
  selectedDetailId.value = id
}
function toggleQcSimulation(): void {
  state.value.generateIncomingQc = !state.value.generateIncomingQc
  notice.value = `DEV 系統參數 MaterialInGenerateQCForm=${state.value.generateIncomingQc ? '1' : '0'}。`
}
function refresh(): void { notice.value = 'DEV 假資料已重新整理；未確認的進料單依單號排序。' }

function openBasis(mode: Exclude<FormMode, null>): void {
  if ((mode === 'edit' || mode === 'copy') && !activeBasis.value) return
  if (mode === 'edit' && activeBasis.value?.state !== 0) return
  basisMode.value = mode
  basisError.value = ''
  if (mode === 'add') {
    const next = state.value.serial++
    basisDraft.value = { ...blankBasisDraft(), materialInNo: serialNo('MIN', next) }
  } else if (activeBasis.value) {
    basisDraft.value = {
      materialInNo: mode === 'copy' ? serialNo('MIN', state.value.serial++) : activeBasis.value.materialInNo,
      inventoryNo: activeBasis.value.inventoryNo, vendorNo: activeBasis.value.vendorNo, description: activeBasis.value.description,
    }
  }
}
function saveBasis(): void {
  const draft = basisDraft.value
  const materialInNo = draft.materialInNo.trim()
  if (!materialInNo || !draft.inventoryNo || !draft.vendorNo) { basisError.value = '進料單號、原料庫房及供應商必填。'; return }
  const duplicate = state.value.bases.some((row) => row.materialInNo === materialInNo && row !== (basisMode.value === 'edit' ? activeBasis.value : null))
  if (duplicate) { basisError.value = 'DEV 進料單號重複。'; return }
  if (basisMode.value === 'edit' && activeBasis.value) {
    activeBasis.value.description = draft.description.trim()
    activeBasis.value.reviser = currentUser.value
    activeBasis.value.reviseDate = new Date().toLocaleString('zh-TW', { hour12: false })
    activeBasis.value.dataStamp += 1
    selectedBasisNo.value = activeBasis.value.materialInNo
  } else {
    const now = new Date().toLocaleString('zh-TW', { hour12: false })
    const row: BasisRow = { materialInNo, inventoryNo: draft.inventoryNo, vendorNo: draft.vendorNo, description: draft.description.trim(), creator: currentUser.value, createDate: now, reviser: '', reviseDate: '', state: 0, dataStamp: 1 }
    state.value.bases.unshift(row)
    selectedBasisNo.value = materialInNo
    selectedBasisNos.value = []
  }
  notice.value = `DEV 已${basisMode.value === 'edit' ? '修改' : basisMode.value === 'copy' ? '複製' : '新增'}進料單 ${materialInNo}；尚未送出入庫確認。`
  basisMode.value = null
}
function deleteBasisRows(): void {
  const targets = basisActionRows.value
  if (targets.length === 0) return
  if (targets.some((row) => row.state !== 0)) { notice.value = '只有 State=0 的進料單可以刪除。'; return }
  const targetNos = new Set(targets.map((row) => row.materialInNo))
  const locked = state.value.details.find((row) => targetNos.has(row.materialInNo) && row.qcFormNo && row.qcStatus !== 0)
  if (locked) { notice.value = `進料單 ${locked.materialInNo} 的 QC ${locked.qcFormNo} 已非待檢狀態，不能刪除。`; return }
  if (!window.confirm(`確定刪除 ${targets.length} 張進料單及其明細與待檢 DEV QC 假資料？`)) return
  state.value.bases = state.value.bases.filter((row) => !targetNos.has(row.materialInNo))
  state.value.details = state.value.details.filter((row) => !targetNos.has(row.materialInNo))
  selectedBasisNos.value = []
  notice.value = `DEV 已刪除 ${targets.length} 張 State=0 進料單。`
}

function setMaterialDefaults(materialNo: string): void {
  const item = materials.find((row) => row.materialNo === materialNo)
  if (!item) return
  detailDraft.value.unitNo = item.unitNo
  detailDraft.value.materialLotNo = item.checkLotNo ? '' : 'N/A'
  detailDraft.value.locatorNo = locatorOptions.value[0] ?? 'N/A'
  detailDraft.value.expireDate = ''
  detailDraft.value.manufactureDate = ''
  detailDraft.value.graceDate = ''
}
function openDetail(mode: Exclude<FormMode, null>): void {
  if (!activeBasis.value || activeBasis.value.state !== 0) { notice.value = '請先選取 State=0 的進料單。'; return }
  if ((mode === 'edit' || mode === 'copy') && !activeDetail.value) { notice.value = '請先選取一筆進料明細。'; return }
  if (mode === 'edit' && activeDetail.value?.qcFormNo && activeDetail.value.qcStatus !== 0) { notice.value = `QC ${activeDetail.value.qcFormNo} 已非待檢狀態，明細不可修改。`; return }
  detailMode.value = mode
  detailError.value = ''
  if (mode === 'add') detailDraft.value = blankDetailDraft()
  else if (activeDetail.value) {
    detailDraft.value = {
      materialNo: activeDetail.value.materialNo, materialLotNo: activeDetail.value.materialLotNo,
      locatorNo: activeDetail.value.locatorNo, unitNo: activeDetail.value.unitNo, qty: String(activeDetail.value.qty),
      expireDate: activeDetail.value.expireDate, manufactureDate: activeDetail.value.manufactureDate, graceDate: activeDetail.value.graceDate,
    }
  }
}
function closeDetail(): void { detailMode.value = null }
function saveDetail(): void {
  const basis = activeBasis.value
  const material = draftMaterial.value
  const draft = detailDraft.value
  if (!basis || !material || material.vendorNo !== basis.vendorNo) { detailError.value = '物料必須屬於此進料單的供應商。'; return }
  const qty = Number(draft.qty)
  if (!Number.isFinite(qty) || qty <= 0) { detailError.value = '數量必須大於 0。'; return }
  if (material.checkLotNo && (!draft.materialLotNo.trim() || draft.materialLotNo.trim() === 'N/A')) { detailError.value = '此物料須填有效批號。'; return }
  if (!material.checkLotNo && draft.materialLotNo.trim() !== 'N/A') { detailError.value = '此物料不檢查批號，批號須為 N/A。'; return }
  const validLocators = locators[basis.inventoryNo] ?? []
  if (validLocators.length && !validLocators.includes(draft.locatorNo)) { detailError.value = '請選擇此庫房的 Locator。'; return }
  if (!validLocators.length && draft.locatorNo !== 'N/A') { detailError.value = '此庫房未設定 Locator，明細 Locator 必須為 N/A。'; return }
  if (material.expireDateRequired && !draft.expireDate) { detailError.value = '此物料必須填寫有效期限。'; return }
  if (draft.expireDate && draft.graceDate && draft.expireDate < draft.graceDate) { detailError.value = '有效期限必須晚於寬限期。'; return }
  if (draft.expireDate && draft.manufactureDate && draft.expireDate < draft.manufactureDate) { detailError.value = '有效期限必須晚於製造日期。'; return }
  if (draft.graceDate && draft.manufactureDate && draft.graceDate < draft.manufactureDate) { detailError.value = '寬限期必須晚於製造日期。'; return }

  const sourceDetail = activeDetail.value
  const editing = detailMode.value === 'edit' && sourceDetail
  const effectiveLocator = validLocators.length ? draft.locatorNo : 'N/A'
  const effectiveLot = material.checkLotNo ? draft.materialLotNo.trim() : 'N/A'
  if (!editing && state.value.details.some((row) => row.materialInNo === basis.materialInNo && row.materialNo === material.materialNo && row.materialLotNo === effectiveLot && row.locatorNo === effectiveLocator)) {
    detailError.value = '相同物料、批號及 Locator 的明細已存在；請先編輯原列。'
    return
  }
  const now = new Date().toLocaleString('zh-TW', { hour12: false })
  if (editing && sourceDetail) {
    sourceDetail.qty = qty
    sourceDetail.expireDate = draft.expireDate
    sourceDetail.manufactureDate = draft.manufactureDate
    sourceDetail.graceDate = draft.graceDate
    sourceDetail.reviser = currentUser.value
    sourceDetail.reviseDate = now
    sourceDetail.dataStamp += 1
  } else {
    const qcFormNo = state.value.generateIncomingQc && material.qcItemCount > 0 ? `QCF-DEV-${String(state.value.qcSerial++).padStart(4, '0')}` : ''
    state.value.details.push({
      materialInNo: basis.materialInNo, materialNo: material.materialNo, materialName: material.materialName,
      materialLotNo: effectiveLot, locatorNo: effectiveLocator, unitNo: material.unitNo, qty,
      qcFormNo, qcStatus: qcFormNo ? 0 : null,
      expireDate: draft.expireDate, manufactureDate: draft.manufactureDate, graceDate: draft.graceDate,
      creator: currentUser.value, createDate: now, reviser: '', reviseDate: '', dataStamp: 1,
    })
  }
  selectedDetailId.value = `${basis.materialInNo}|${material.materialNo}|${effectiveLot}|${effectiveLocator}`
  selectedDetailIds.value = []
  notice.value = `DEV 已${editing ? '修改' : detailMode.value === 'copy' ? '複製' : '新增'} ${material.materialNo} 明細${state.value.generateIncomingQc && material.qcItemCount > 0 && !editing ? '，並模擬建立待檢 Incoming QC' : ''}。`
  closeDetail()
}
function deleteDetailRows(): void {
  const targets = detailActionRows.value
  if (targets.length === 0) return
  const locked = targets.find((row) => row.qcFormNo && row.qcStatus !== 0)
  if (locked) { notice.value = `QC ${locked.qcFormNo} 已非待檢狀態，明細不可刪除。`; return }
  if (!window.confirm(`確定刪除選取的 ${targets.length} 筆明細與待檢 Incoming QC DEV 假資料？`)) return
  const targetIds = new Set(targets.map(detailId))
  state.value.details = state.value.details.filter((row) => !targetIds.has(detailId(row)))
  selectedDetailIds.value = []
  notice.value = `DEV 已刪除 ${targets.length} 筆進料明細。`
}
function openPrint(): void {
  if (!activeBasis.value) { notice.value = '請先選擇要列印的進料單。'; return }
  if (basisActionRows.value.some((row) => !state.value.details.some((detail) => detail.materialInNo === row.materialInNo))) {
    notice.value = '選取的進料單有明細為空，WinForms 報表不會列印。'
    return
  }
  printOpen.value = true
}
function finishPrint(): void { printOpen.value = false; notice.value = `DEV 已模擬列印 ${printRows.value.length} 張進料單；未連接印表機。` }
const currentUser = computed(() => props.currentUserNo.trim() || 'DEVUSER')
</script>

<template>
  <main class="material-in" aria-label="開立進料單 DEV" :data-development-preview="props.developmentPreview ? 'true' : 'false'">
    <header class="page-heading"><div><p>INVENTORY · IV202</p><h1>開立進料單</h1></div><span class="dev-badge">DEV MOCK</span></header>
    <section class="toolbar">
      <label>MaterialInNo<input v-model="queryMaterialInNo" placeholder="輸入單號篩選" /></label>
      <button type="button" @click="refresh">Refresh</button>
      <label class="qc-setting" title="模擬受保護的 SysInfo MaterialInGenerateQCForm"><input type="checkbox" :checked="state.generateIncomingQc" @change="toggleQcSimulation" /> DEV 自動產生 Incoming QC</label>
      <span class="notice" role="status">{{ notice }}</span>
    </section>
    <section class="summary">
      <article><span>待確認進料單</span><strong>{{ visibleBases.length }}</strong><small>原表單固定查 State=0</small></article>
      <article><span>目前單據明細</span><strong>{{ materialCount }}</strong><small>{{ activeBasis?.materialInNo ?? '未選擇單據' }}</small></article>
      <article><span>明細總量</span><strong>{{ formatQty(totalDetailQty) }}</strong><small>各 Unit 分開展示</small></article>
      <article><span>Incoming QC</span><strong>{{ linkedQcCount }}</strong><small>資料列含 QCFormNo</small></article>
    </section>

    <section class="master-section">
      <div class="panel">
        <header class="panel-heading"><div><h2>MaterialIn Basis</h2><span>未確認單據 · {{ visibleBases.length }} 筆</span></div><small>{{ activeBasis?.inventoryNo ?? '—' }} · {{ activeBasis?.vendorNo ?? '—' }}</small></header>
        <div class="table-wrap"><table>
          <thead><tr><th class="selector">選取</th><th>MaterialInNo</th><th>InventoryNo</th><th>VendorNo</th><th>State</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
          <tbody>
            <tr v-for="row in visibleBases" :key="row.materialInNo" :class="{ active: activeBasis?.materialInNo === row.materialInNo, checked: selectedBasisNos.includes(row.materialInNo) }" @click="selectedBasisNo = row.materialInNo">
              <td class="selector" @click.stop><input type="checkbox" :checked="selectedBasisNos.includes(row.materialInNo)" @change="setBasisSelection(row, ($event.target as HTMLInputElement).checked)" /></td>
              <td class="mono">{{ row.materialInNo }}</td><td>{{ row.inventoryNo }}</td><td>{{ vendorName(row.vendorNo) }}</td><td><span class="state-pill">State 0 · 未確認</span></td><td class="description">{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser || '—' }}</td><td>{{ row.reviseDate || '—' }}</td>
            </tr>
            <tr v-if="visibleBases.length === 0"><td colspan="10" class="empty">沒有符合單號條件的 State=0 進料單。</td></tr>
          </tbody>
        </table></div>
        <footer class="grid-footer"><span>{{ visibleBases.length }} 筆 · State=0</span><span>{{ masterAudit }}</span></footer>
      </div>
      <aside class="action-rail" aria-label="進料單主檔操作"><h2>Basis</h2><button type="button" @click="openBasis('add')">ADD</button><button type="button" :disabled="!activeBasis" @click="openBasis('edit')">EDIT</button><button type="button" :disabled="basisActionRows.length === 0" class="danger" @click="deleteBasisRows">DEL</button><button type="button" :disabled="!activeBasis" @click="openBasis('copy')">COPY</button><button type="button" :disabled="!activeBasis" @click="openPrint">PRINT</button><button type="button" class="close-button" @click="emit('close')">Close (X)</button></aside>
    </section>

    <section class="detail-section">
      <div class="panel">
        <header class="panel-heading"><div><h2>MaterialIn Detail</h2><span>{{ activeBasis?.materialInNo ?? '請先選取單據' }}</span></div><small>{{ currentDetails.length }} 筆明細 · Vendor {{ activeBasis?.vendorNo ?? '—' }}</small></header>
        <div class="table-wrap"><table>
          <thead><tr><th class="selector">選取</th><th>MaterialNo</th><th>MaterialName</th><th>MaterialLotNo</th><th>LocatorNo</th><th>UnitNo</th><th>Qty</th><th>QCFormNo</th><th>ExpireDate</th><th>ManufactureDate</th><th>GraceDate</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
          <tbody>
            <tr v-for="row in currentDetails" :key="detailId(row)" :class="{ active: activeDetail && detailId(activeDetail) === detailId(row), checked: selectedDetailIds.includes(detailId(row)) }" @click="selectedDetailId = detailId(row)">
              <td class="selector" @click.stop><input type="checkbox" :checked="selectedDetailIds.includes(detailId(row))" @change="setDetailSelection(row, ($event.target as HTMLInputElement).checked)" /></td><td class="mono">{{ row.materialNo }}</td><td>{{ row.materialName }}</td><td class="mono">{{ row.materialLotNo }}</td><td class="mono">{{ row.locatorNo }}</td><td>{{ row.unitNo }}</td><td class="number">{{ formatQty(row.qty) }}</td>
              <td><span v-if="row.qcFormNo" class="qc-link" :title="`Incoming QC ${qcStatusLabel(row.qcStatus)}`">{{ row.qcFormNo }} · {{ qcStatusLabel(row.qcStatus) }}</span><span v-else>—</span></td><td>{{ row.expireDate || '—' }}</td><td>{{ row.manufactureDate || '—' }}</td><td>{{ row.graceDate || '—' }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser || '—' }}</td><td>{{ row.reviseDate || '—' }}</td>
            </tr>
            <tr v-if="currentDetails.length === 0"><td colspan="15" class="empty">此進料單尚無明細，請使用下方 ADD 建立。</td></tr>
          </tbody>
        </table></div>
        <footer class="grid-footer"><span>{{ currentDetails.length }} 筆明細</span><span>確認入庫由 IV201 Material In 執行</span></footer>
      </div>
      <aside class="action-rail detail-actions" aria-label="進料單明細操作"><h2>Detail</h2><button type="button" :disabled="!activeBasis" @click="openDetail('add')">ADD</button><button type="button" :disabled="!activeDetail" @click="openDetail('edit')">EDIT</button><button type="button" :disabled="detailActionRows.length === 0" class="danger" @click="deleteDetailRows">DEL</button><button type="button" :disabled="!activeDetail" @click="openDetail('copy')">COPY</button></aside>
    </section>

    <div v-if="basisMode" class="modal-backdrop" @click.self="basisMode = null">
      <section class="editor" role="dialog" aria-modal="true" :aria-label="`${basisMode} MaterialIn Basis`">
        <header><div><p>DEV BASIS {{ basisMode.toUpperCase() }}</p><h2>{{ basisMode === 'add' ? '新增進料單' : basisMode === 'edit' ? '修改進料單' : '複製進料單' }}</h2></div><button type="button" aria-label="關閉" @click="basisMode = null">×</button></header>
        <fieldset><legend>MaterialIn Basis Data</legend>
          <label>MaterialInNo<input v-model="basisDraft.materialInNo" :readonly="basisMode === 'edit'" autocomplete="off" /></label>
          <label>InventoryNo<select v-model="basisDraft.inventoryNo" :disabled="basisMode === 'edit'"><option value="">選擇原料庫房</option><option v-for="row in inventories" :key="row.inventoryNo" :value="row.inventoryNo">{{ row.inventoryNo }} · {{ row.inventoryName }}</option></select></label>
          <label>VendorNo<select v-model="basisDraft.vendorNo" :disabled="basisMode === 'edit'"><option value="">選擇已核准供應商</option><option v-for="row in vendors" :key="row.vendorNo" :value="row.vendorNo">{{ row.vendorNo }} · {{ row.vendorName }}</option></select></label>
          <label class="wide">Description<textarea v-model="basisDraft.description" rows="3" /></label>
        </fieldset>
        <p v-if="basisError" class="form-error" role="alert">{{ basisError }}</p>
        <p class="audit-note">{{ basisMode === 'edit' ? masterAudit : `DEV Creator ${currentUser} · 單號僅為本機假序號` }}</p>
        <footer><button type="button" @click="basisMode = null">Cancel</button><button type="button" class="primary" @click="saveBasis">{{ basisMode === 'edit' ? 'Apply' : 'OK' }}</button></footer>
      </section>
    </div>

    <div v-if="detailMode" class="modal-backdrop detail-backdrop" @click.self="closeDetail">
      <section class="editor detail-editor" role="dialog" aria-modal="true" :aria-label="`${detailMode} MaterialIn Detail`">
        <header><div><p>DEV DETAIL {{ detailMode.toUpperCase() }} · {{ activeBasis?.materialInNo }}</p><h2>{{ detailMode === 'add' ? '新增進料明細' : detailMode === 'edit' ? '修改進料明細' : '複製進料明細' }}</h2></div><button type="button" aria-label="關閉" @click="closeDetail">×</button></header>
        <fieldset><legend>MaterialIn Detail Data</legend>
          <label>MaterialNo<select v-model="detailDraft.materialNo" :disabled="detailMode === 'edit'" @change="setMaterialDefaults(detailDraft.materialNo)"><option value="">選擇此供應商的核准物料</option><option v-for="row in availableMaterials" :key="row.materialNo" :value="row.materialNo">{{ row.materialNo }} · {{ row.materialName }}</option></select></label>
          <label>UnitNo<input :value="draftMaterial?.unitNo ?? detailDraft.unitNo" readonly /></label>
          <label>MaterialLotNo<input v-model="detailDraft.materialLotNo" :readonly="detailMode === 'edit' || !draftMaterial?.checkLotNo" :placeholder="draftMaterial?.checkLotNo ? '必填批號' : '不檢查批號時固定 N/A'" /></label>
          <label>LocatorNo<select v-if="locatorOptions.length" v-model="detailDraft.locatorNo" :disabled="detailMode === 'edit'"><option v-for="locator in locatorOptions" :key="locator" :value="locator">{{ locator }}</option></select><input v-else value="N/A" readonly /></label>
          <label>Qty<input v-model="detailDraft.qty" type="number" min="0.000001" step="any" /></label>
          <label>ExpireDate <span class="date-field"><input v-model="detailDraft.expireDate" type="date" /><small>{{ draftMaterial?.expireDateRequired ? '此物料必填' : '選填' }}</small></span></label>
          <label>ManufactureDate<input v-model="detailDraft.manufactureDate" type="date" /></label>
          <label>GraceDate<input v-model="detailDraft.graceDate" type="date" /></label>
        </fieldset>
        <p class="audit-note">{{ draftMaterial?.checkLotNo ? '物料主檔要求追蹤批號。' : draftMaterial ? '物料主檔不要求批號，使用 N/A。' : '選擇物料後載入批號與保存期限規則。' }} {{ locatorOptions.length ? '庫房有 Locator，需指定儲位。' : '此庫房無 Locator，固定使用 N/A。' }}</p>
        <p v-if="detailError" class="form-error" role="alert">{{ detailError }}</p>
        <footer><button type="button" @click="closeDetail">Cancel</button><button type="button" class="primary" @click="saveDetail">{{ detailMode === 'edit' ? 'Apply' : 'OK' }}</button></footer>
      </section>
    </div>

    <div v-if="printOpen" class="modal-backdrop print-backdrop" @click.self="printOpen = false">
      <section class="print-preview" role="dialog" aria-modal="true" aria-label="進料單列印預覽">
        <header><div><p>DEV REPORT · arMaterialIn</p><h2>Material In</h2></div><button type="button" @click="printOpen = false">×</button></header>
        <article v-for="item in printRows" :key="item.basis.materialInNo" class="report-sheet">
          <h3>Material In Receipt <span>{{ item.basis.materialInNo }}</span></h3><p>Inventory: {{ item.basis.inventoryNo }}　 Vendor: {{ item.basis.vendorNo }}</p><p>Description: {{ item.basis.description || '—' }}</p>
          <table><thead><tr><th>MaterialNo</th><th>Lot</th><th>Locator</th><th>Unit</th><th>Qty</th><th>QCFormNo</th></tr></thead><tbody><tr v-for="row in item.details" :key="detailId(row)"><td>{{ row.materialNo }}</td><td>{{ row.materialLotNo }}</td><td>{{ row.locatorNo }}</td><td>{{ row.unitNo }}</td><td>{{ formatQty(row.qty) }}</td><td>{{ row.qcFormNo || '—' }}</td></tr></tbody></table>
          <footer>Creator: {{ item.basis.creator }}　 CreateDate: {{ item.basis.createDate }}</footer>
        </article>
        <footer class="print-actions"><span>DEV 預覽；不連接真實印表機</span><button type="button" @click="printOpen = false">Close</button><button type="button" class="primary" @click="finishPrint">Print</button></footer>
      </section>
    </div>
  </main>
</template>

<style scoped>
.material-in{display:flex;flex-direction:column;width:100%;height:100%;min-height:0;gap:7px;padding:9px;box-sizing:border-box;background:#f4f2eb;color:#292820;font:12px/1.35 "Segoe UI","Microsoft JhengHei",sans-serif}.page-heading{display:flex;align-items:center;justify-content:space-between;padding:0 2px 5px;border-bottom:1px solid #cbc8bb}.page-heading p{margin:0;color:#777366;font-size:10px;letter-spacing:.07em}.page-heading h1{margin:0;font-size:19px;font-weight:600}.dev-badge{padding:3px 8px;border:1px solid #8e946e;background:#e8ecd9;color:#4c5c39;font-size:10px;font-weight:700;letter-spacing:.08em}.toolbar{display:flex;align-items:center;gap:9px;min-height:31px;flex-wrap:wrap}.toolbar>label:first-child{display:flex;align-items:center;gap:6px;font-weight:600}.toolbar input:not([type=checkbox]){box-sizing:border-box;min-height:26px;padding:3px 7px;border:1px solid #999587;background:#fffef8;color:#292820;font:inherit}.toolbar>label:first-child input{width:180px}.qc-setting{display:flex;align-items:center;gap:5px;padding-left:8px;border-left:1px solid #cbc8bb;color:#746d53;font-size:10px}.qc-setting input{margin:0;accent-color:#56704f}.material-in button{min-height:27px;padding:3px 8px;border:1px solid #8c887b;border-radius:2px;background:#fbfaf5;color:#292820;font:inherit;cursor:pointer;white-space:nowrap}.material-in button:hover:not(:disabled){background:#e7e4d9}.material-in button:disabled{opacity:.5;cursor:not-allowed}.material-in button.primary{border-color:#48634d;background:#506c54;color:#fff}.material-in button.danger{color:#8d302a}.notice{flex:1;min-width:220px;color:#625f55;font-size:10px}.summary{display:grid;grid-template-columns:repeat(4,minmax(120px,1fr));gap:6px}.summary article{display:grid;grid-template-columns:1fr auto;align-items:center;gap:0 8px;padding:5px 8px;border:1px solid #d2cfc4;background:#fbfaf5}.summary article>span{color:#716d62;font-size:10px}.summary article strong{grid-column:2;grid-row:1/3;font-size:18px;font-variant-numeric:tabular-nums}.summary article small{font-size:9px;color:#8a867b}.master-section,.detail-section{display:flex;flex:1;min-height:125px;gap:7px}.panel{display:flex;flex:1;flex-direction:column;min-width:0;min-height:0;border:1px solid #c6c2b6;background:#fffef9}.panel-heading{display:flex;align-items:center;justify-content:space-between;gap:8px;padding:5px 8px;border-bottom:1px solid #d6d3c8;background:#eeece4}.panel-heading h2{margin:0;font-size:12px;font-weight:600}.panel-heading span,.panel-heading small{color:#777366;font-size:9px}.panel-heading>div{display:flex;align-items:baseline;gap:8px}.table-wrap{flex:1;min-height:0;overflow:auto}.material-in table{width:max-content;min-width:100%;border-collapse:collapse;white-space:nowrap;font-size:10px}.material-in th,.material-in td{height:24px;padding:2px 6px;border-right:1px solid #e0ded6;border-bottom:1px solid #e3e1da;text-align:left}.material-in th{position:sticky;top:0;z-index:1;background:#e8e6dd;color:#514f46;font-weight:600}.material-in tbody tr{cursor:pointer}.material-in tbody tr:hover{background:#f5f2e5}.material-in tbody tr.active{background:#e4ecde}.material-in tbody tr.checked:not(.active){background:#f5efd9}.selector{width:28px;text-align:center!important}.selector input{margin:0;accent-color:#56704f}.mono{font-family:Consolas,"Courier New",monospace}.number{text-align:right!important;font-variant-numeric:tabular-nums}.description{max-width:200px;overflow:hidden;text-overflow:ellipsis}.state-pill{padding:2px 5px;border:1px solid #c5bc83;background:#f4efcf;color:#685c2d}.qc-link{color:#385d7c}.empty{text-align:center;color:#817d72;padding:14px!important}.grid-footer{display:flex;justify-content:space-between;gap:8px;min-height:18px;padding:2px 7px;border-top:1px solid #d6d3c8;color:#777366;font-size:9px}.action-rail{display:flex;flex:none;flex-direction:column;gap:4px;width:72px;padding:6px;border:1px solid #c6c2b6;background:#eeece4}.action-rail h2{margin:0 0 1px;font-size:10px}.action-rail button{width:100%;min-height:25px;text-align:center}.action-rail .close-button{margin-top:auto}.detail-actions{width:70px}.modal-backdrop{position:fixed;inset:0;z-index:40;display:flex;align-items:center;justify-content:center;padding:15px;background:rgb(25 24 20 / 44%)}.editor,.print-preview{display:flex;flex-direction:column;gap:9px;width:min(650px,96vw);max-height:93vh;overflow:auto;padding:12px;border:1px solid #898578;background:#f8f6ef;box-shadow:0 8px 24px #0005}.editor>header,.print-preview>header{display:flex;align-items:center;justify-content:space-between;padding-bottom:7px;border-bottom:1px solid #cfccbf}.editor>header p,.print-preview>header p{margin:0;color:#777366;font-size:9px;letter-spacing:.06em}.editor h2,.print-preview h2{margin:0;font-size:16px;font-weight:600}.editor>header button,.print-preview>header button{width:28px;height:26px;padding:0;font-size:19px}.editor fieldset{display:grid;grid-template-columns:1fr 1fr;gap:8px 12px;margin:0;padding:10px;border:1px solid #c8c4b7}.editor legend{padding:0 5px}.editor fieldset>label{display:flex;flex-direction:column;gap:3px}.editor input,.editor select,.editor textarea{box-sizing:border-box;min-height:27px;width:100%;padding:4px 7px;border:1px solid #999587;background:#fffef8;color:#292820;font:inherit}.editor input[readonly]{background:#eeece4;color:#666256}.editor textarea{resize:vertical}.editor .wide{grid-column:1/-1}.editor .date-field{display:flex;align-items:center;gap:6px}.editor .date-field small{color:#8a5238;font-size:9px}.audit-note{margin:0;color:#777366;font-size:10px}.form-error{margin:0;color:#9a2924;font-size:11px}.editor>footer{display:flex;justify-content:flex-end;gap:6px;padding-top:7px;border-top:1px solid #cfccbf}.detail-editor{width:min(680px,96vw)}.detail-editor fieldset{grid-template-columns:1fr 1fr}.print-preview{width:min(900px,96vw);background:#f1efe8}.report-sheet{padding:15px;border:1px solid #c6c2b6;background:#fff}.report-sheet h3{display:flex;justify-content:space-between;margin:0 0 10px;padding-bottom:7px;border-bottom:1px solid #aaa;font-size:15px}.report-sheet p{margin:4px 0;font-size:11px}.report-sheet table{width:100%;min-width:0;white-space:normal;margin-top:10px}.report-sheet td,.report-sheet th{height:auto;padding:6px}.report-sheet footer{margin-top:12px;padding-top:6px;border-top:1px solid #aaa;font-size:10px}.print-actions{display:flex;align-items:center;justify-content:flex-end;gap:6px}.print-actions span{flex:1;color:#777366;font-size:10px}@media(max-width:900px){.summary{grid-template-columns:repeat(2,minmax(120px,1fr))}.action-rail{width:65px}.panel-heading small{display:none}}@media(max-width:600px){.material-in{height:auto;min-height:100%;overflow:auto}.master-section,.detail-section{min-height:300px}.toolbar{align-items:flex-start}.notice{min-width:100%}.action-rail{width:100%;flex-direction:row;flex-wrap:wrap}.action-rail h2{width:100%}.action-rail button{width:auto;flex:1}.action-rail .close-button{margin:0}.master-section,.detail-section{flex-direction:column}.editor fieldset{grid-template-columns:1fr}.editor .wide{grid-column:auto}.detail-editor fieldset{grid-template-columns:1fr}.summary{grid-template-columns:repeat(2,1fr)}}
</style>
