<script setup lang="ts">
import { computed, ref } from 'vue'

type BasisMode = 'add' | 'edit' | 'copy'
type DetailMode = 'add' | 'edit' | 'copy'
interface BasisRow {
  id: number
  preOrderNo: string
  inventoryNo: string
  state: 0
  description: string
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
  dataStamp: number
}
interface DetailRow {
  id: number
  preOrderNo: string
  materialNo: string
  unitNo: string
  qty: number
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
  dataStamp: number
}
interface RawMaterial { inventoryNo: string; materialNo: string; materialName: string; unitNo: string }
interface ConfirmRequest { kind: 'basis' | 'detail'; rowId: number; label: string }

withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()

const storageKey = 'ymes.dev.inv.preorder.v1'
const fixtureBases: BasisRow[] = [
  { id: 1, preOrderNo: 'PRE-DEV-0001', inventoryNo: 'RAW-A', state: 0, description: 'DEV 假資料：樹脂備料。', creator: 'DEV001', createDate: '2026-11-20 09:12:00', reviser: '', reviseDate: '', dataStamp: 1 },
  { id: 2, preOrderNo: 'PRE-DEV-0002', inventoryNo: 'RAW-B', state: 0, description: 'DEV 假資料：銅箔與溶劑。', creator: 'DEV002', createDate: '2026-11-21 14:36:00', reviser: '', reviseDate: '', dataStamp: 1 },
  { id: 3, preOrderNo: 'PRE-DEV-0003', inventoryNo: 'RAW-A', state: 0, description: 'DEV 假資料：薄膜樣品需求。', creator: 'DEV003', createDate: '2026-11-23 08:04:00', reviser: '', reviseDate: '', dataStamp: 1 },
]
const fixtureDetails: DetailRow[] = [
  { id: 11, preOrderNo: 'PRE-DEV-0001', materialNo: 'RM-RESIN-01', unitNo: 'KG', qty: 18, creator: 'DEV001', createDate: '2026-11-20 09:16:00', reviser: '', reviseDate: '', dataStamp: 1 },
  { id: 12, preOrderNo: 'PRE-DEV-0001', materialNo: 'RM-SOLVENT-02', unitNo: 'L', qty: 6.5, creator: 'DEV001', createDate: '2026-11-20 09:18:00', reviser: '', reviseDate: '', dataStamp: 1 },
  { id: 13, preOrderNo: 'PRE-DEV-0002', materialNo: 'RM-COPPER-03', unitNo: 'RL', qty: 12, creator: 'DEV002', createDate: '2026-11-21 14:40:00', reviser: '', reviseDate: '', dataStamp: 1 },
  { id: 14, preOrderNo: 'PRE-DEV-0003', materialNo: 'RM-FILM-04', unitNo: 'M', qty: 240, creator: 'DEV003', createDate: '2026-11-23 08:10:00', reviser: '', reviseDate: '', dataStamp: 1 },
]
const inventories = [
  { inventoryNo: 'RAW-A', inventoryName: '一廠原物料倉' },
  { inventoryNo: 'RAW-B', inventoryName: '二廠原物料倉' },
]
const rawMaterials: RawMaterial[] = [
  { inventoryNo: 'RAW-A', materialNo: 'RM-RESIN-01', materialName: '工業樹脂', unitNo: 'KG' },
  { inventoryNo: 'RAW-A', materialNo: 'RM-SOLVENT-02', materialName: '清洗溶劑', unitNo: 'L' },
  { inventoryNo: 'RAW-A', materialNo: 'RM-FILM-04', materialName: '保護膜', unitNo: 'M' },
  { inventoryNo: 'RAW-B', materialNo: 'RM-COPPER-03', materialName: '銅箔捲料', unitNo: 'RL' },
  { inventoryNo: 'RAW-B', materialNo: 'RM-SOLVENT-02', materialName: '清洗溶劑', unitNo: 'L' },
]

function readSaved<T>(key: 'bases' | 'details', fallback: T[]): T[] {
  try {
    if (typeof window === 'undefined') return clone(fallback)
    const saved = localStorage.getItem(storageKey)
    if (!saved) return clone(fallback)
    const parsed = JSON.parse(saved) as Partial<Record<'bases' | 'details', unknown>>
    const value = parsed[key]
    return Array.isArray(value) ? value as T[] : clone(fallback)
  } catch { return clone(fallback) }
}
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
const bases = ref<BasisRow[]>(readSaved('bases', fixtureBases))
const details = ref<DetailRow[]>(readSaved('details', fixtureDetails))
const selectedPreOrderNo = ref('')
const selectedDetailId = ref(0)
const searchText = ref('')
const appliedSearch = ref('')
const notice = ref('DEV 假資料：只維護 State=0 預訂草稿；確認與庫存預約由 IV201 處理。')
const basisDialogMode = ref<BasisMode | null>(null)
const detailDialogMode = ref<DetailMode | null>(null)
const basisDraft = ref({ preOrderNo: '', inventoryNo: '', description: '' })
const detailDraft = ref({ materialNo: '', unitNo: '', qty: '' })
const basisTargetId = ref(0)
const detailTargetId = ref(0)
const lookupKind = ref<'inventory' | 'material' | null>(null)
const confirmRequest = ref<ConfirmRequest | null>(null)
const serialCandidate = ref('')
let nextBasisId = Math.max(0, ...bases.value.map((row) => row.id)) + 1
let nextDetailId = Math.max(0, ...details.value.map((row) => row.id)) + 1
let nextSerial = Math.max(0, ...bases.value.map((row) => Number(row.preOrderNo.match(/(\d+)$/)?.[1] ?? 0))) + 1

const visibleBases = computed(() => bases.value
  .filter((row) => row.state === 0 && (!appliedSearch.value || row.preOrderNo === appliedSearch.value))
  .sort((a, b) => a.preOrderNo.localeCompare(b.preOrderNo)))
const activeBasis = computed(() => visibleBases.value.find((row) => row.preOrderNo === selectedPreOrderNo.value) ?? visibleBases.value[0] ?? null)
const visibleDetails = computed(() => activeBasis.value
  ? details.value.filter((row) => row.preOrderNo === activeBasis.value?.preOrderNo).sort((a, b) => a.materialNo.localeCompare(b.materialNo))
  : [])
const activeDetail = computed(() => visibleDetails.value.find((row) => row.id === selectedDetailId.value) ?? null)
const lookupOptions = computed(() => lookupKind.value === 'inventory'
  ? inventories.map((row) => ({ value: row.inventoryNo, label: `${row.inventoryNo} · ${row.inventoryName}` }))
  : rawMaterials.filter((row) => row.inventoryNo === (basisDialogMode.value ? basisDraft.value.inventoryNo : activeBasis.value?.inventoryNo))
    .map((row) => ({ value: row.materialNo, label: `${row.materialNo} · ${row.materialName} · ${row.unitNo}` })))
const lookupTitle = computed(() => lookupKind.value === 'inventory' ? 'Inventory No' : 'Material No')
const activeInventoryName = computed(() => inventories.find((row) => row.inventoryNo === activeBasis.value?.inventoryNo)?.inventoryName ?? activeBasis.value?.inventoryNo ?? '')

function timestamp(): string { return new Date().toLocaleString('sv-SE', { hour12: false }).replace('T', ' ') }
function persist(): void {
  try { localStorage.setItem(storageKey, JSON.stringify({ bases: bases.value, details: details.value })); notice.value = 'DEV 假資料已更新；目前操作只存在此瀏覽器。' }
  catch { notice.value = 'DEV 假資料已在目前畫面更新，但瀏覽器無法保存 localStorage。' }
}
function runQuery(): void {
  appliedSearch.value = searchText.value.trim().toUpperCase()
  selectedPreOrderNo.value = visibleBases.value[0]?.preOrderNo ?? ''
  selectedDetailId.value = 0
  notice.value = `DEV 假資料查詢完成：${visibleBases.value.length} 張未確認預訂單。`
}
function selectBasis(row: BasisRow): void { selectedPreOrderNo.value = row.preOrderNo; selectedDetailId.value = 0 }
function openBasis(mode: BasisMode, source?: BasisRow): void {
  basisTargetId.value = mode === 'edit' ? source?.id ?? 0 : 0
  serialCandidate.value = ''
  basisDraft.value = { preOrderNo: mode === 'edit' ? source?.preOrderNo ?? '' : '', inventoryNo: source?.inventoryNo ?? '', description: source?.description ?? '' }
  basisDialogMode.value = mode
}
function takeSerial(): void {
  serialCandidate.value = `PRE-DEV-${String(nextSerial).padStart(4, '0')}`
  basisDraft.value.preOrderNo = serialCandidate.value
}
function saveBasis(): void {
  const current = basisDialogMode.value
  if (!current) return
  const orderNo = basisDraft.value.preOrderNo.trim().toUpperCase()
  if (!orderNo || !basisDraft.value.inventoryNo) { notice.value = '請輸入預訂單號並選取原料庫房。'; return }
  if (current !== 'edit' && orderNo !== serialCandidate.value) { notice.value = '請先按「取號」取得目前有效的 DEV 預訂單號。'; return }
  if (current !== 'edit' && bases.value.some((row) => row.preOrderNo === orderNo)) { notice.value = '此預訂單號已存在。'; return }
  if (current === 'edit') {
    const index = bases.value.findIndex((row) => row.id === basisTargetId.value && row.state === 0)
    if (index < 0) { notice.value = '預訂單已變更，請重新載入後編輯。'; return }
    bases.value[index] = { ...bases.value[index], description: basisDraft.value.description, reviser: 'DEV001', reviseDate: timestamp(), dataStamp: bases.value[index].dataStamp + 1 }
  } else {
    bases.value.push({ id: nextBasisId++, preOrderNo: orderNo, inventoryNo: basisDraft.value.inventoryNo, state: 0, description: basisDraft.value.description, creator: 'DEV001', createDate: timestamp(), reviser: '', reviseDate: '', dataStamp: 1 })
    nextSerial += 1
    selectedPreOrderNo.value = orderNo
  }
  basisDialogMode.value = null
  persist()
}
function requestBasisDelete(row: BasisRow): void { confirmRequest.value = { kind: 'basis', rowId: row.id, label: row.preOrderNo } }
function openDetail(mode: DetailMode, source?: DetailRow): void {
  if (!activeBasis.value) return
  detailTargetId.value = mode === 'edit' ? source?.id ?? 0 : 0
  selectedDetailId.value = source?.id ?? 0
  detailDraft.value = { materialNo: source?.materialNo ?? '', unitNo: source?.unitNo ?? '', qty: source ? String(source.qty) : '' }
  detailDialogMode.value = mode
}
function openSelectedDetail(mode: 'edit' | 'copy'): void { if (activeDetail.value) openDetail(mode, activeDetail.value) }
function chooseMaterial(materialNo: string): void {
  const material = rawMaterials.find((row) => row.materialNo === materialNo && row.inventoryNo === activeBasis.value?.inventoryNo)
  if (!material) return
  detailDraft.value.materialNo = material.materialNo
  detailDraft.value.unitNo = material.unitNo
  lookupKind.value = null
}
function saveDetail(): void {
  const mode = detailDialogMode.value
  const parent = activeBasis.value
  const qtyValue = Number(detailDraft.value.qty)
  if (!mode || !parent) return
  if (!detailDraft.value.materialNo || !detailDraft.value.unitNo || !Number.isFinite(qtyValue) || qtyValue <= 0) { notice.value = '請選物料、確認單位並輸入大於 0 的數量。'; return }
  if (mode !== 'edit' && details.value.some((row) => row.preOrderNo === parent.preOrderNo && row.materialNo === detailDraft.value.materialNo)) { notice.value = '同一預訂單已有此物料；請編輯原明細。'; return }
  if (mode === 'edit') {
    const index = details.value.findIndex((row) => row.id === detailTargetId.value && row.preOrderNo === parent.preOrderNo)
    if (index < 0) { notice.value = '預訂明細已變更，請重新載入後編輯。'; return }
    details.value[index] = { ...details.value[index], qty: qtyValue, reviser: 'DEV001', reviseDate: timestamp(), dataStamp: details.value[index].dataStamp + 1 }
  } else {
    const now = timestamp()
    details.value.push({ id: nextDetailId++, preOrderNo: parent.preOrderNo, materialNo: detailDraft.value.materialNo, unitNo: detailDraft.value.unitNo, qty: qtyValue, creator: 'DEV001', createDate: now, reviser: '', reviseDate: '', dataStamp: 1 })
  }
  selectedDetailId.value = details.value.find((row) => row.preOrderNo === parent.preOrderNo && row.materialNo === detailDraft.value.materialNo)?.id ?? 0
  detailDialogMode.value = null
  persist()
}
function requestDetailDelete(row: DetailRow): void { confirmRequest.value = { kind: 'detail', rowId: row.id, label: `${row.preOrderNo} / ${row.materialNo}` } }
function deleteSelectedDetail(): void { if (activeDetail.value) requestDetailDelete(activeDetail.value) }
function confirmDelete(): void {
  const request = confirmRequest.value
  if (!request) return
  if (request.kind === 'basis') {
    const row = bases.value.find((item) => item.id === request.rowId)
    if (row?.state === 0) {
      bases.value = bases.value.filter((item) => item.id !== request.rowId)
      details.value = details.value.filter((item) => item.preOrderNo !== row.preOrderNo)
      selectedPreOrderNo.value = visibleBases.value[0]?.preOrderNo ?? ''
    }
  } else {
    details.value = details.value.filter((item) => item.id !== request.rowId)
    selectedDetailId.value = 0
  }
  confirmRequest.value = null
  persist()
}
function chooseLookup(value: string): void {
  if (lookupKind.value === 'inventory') basisDraft.value.inventoryNo = value
  else chooseMaterial(value)
  lookupKind.value = null
}
function stateLabel(state: number): string { return state === 0 ? 'UnConfirm' : String(state) }
function qty(value: number): string { return new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 6 }).format(value) }
</script>

<template>
  <main class="preorder-page" aria-label="開立預訂單 DEV">
    <header class="page-heading"><div><p>INVENTORY · IV205</p><h1>Material PreOrder <span>開立預訂單</span></h1><small>維護 State=0 預訂草稿；確認與庫存預約由 IV201 處理</small></div><button type="button" class="close-button" @click="emit('close')">Close (X)</button></header>
    <p class="notice" role="status">{{ notice }}</p>

    <section class="query-panel" aria-label="預訂單查詢">
      <form class="query-form" @submit.prevent="runQuery"><label>PreOrder No<input v-model="searchText" autocomplete="off" placeholder="精確單號；空白顯示所有草稿" /></label><button class="primary" type="submit">Query</button></form>
    </section>

    <section class="panel basis-panel" aria-label="預訂單主檔">
      <header class="panel-heading"><div><h2>PreOrder Basis</h2><span>{{ visibleBases.length }} 張未確認草稿</span></div><small>{{ activeBasis?.preOrderNo ?? '尚未選取單據' }}</small></header>
      <div class="toolbar"><button type="button" class="primary" @click="openBasis('add')">ADD</button><button type="button" :disabled="!activeBasis" @click="activeBasis && openBasis('edit', activeBasis)">EDIT</button><button type="button" :disabled="!activeBasis" @click="activeBasis && requestBasisDelete(activeBasis)">DEL</button><button type="button" :disabled="!activeBasis" @click="activeBasis && openBasis('copy', activeBasis)">COPY</button></div>
      <div class="table-wrap basis-table-wrap"><table><thead><tr><th>PreOrderNo</th><th>InventoryNo</th><th>State</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead><tbody>
        <tr v-for="row in visibleBases" :key="row.id" :class="{ active: activeBasis?.id === row.id }" @click="selectBasis(row)"><td class="mono">{{ row.preOrderNo }}</td><td>{{ row.inventoryNo }}</td><td><span class="state-badge">{{ stateLabel(row.state) }}</span></td><td class="description">{{ row.description || '—' }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser || '—' }}</td><td>{{ row.reviseDate || '—' }}</td></tr>
        <tr v-if="visibleBases.length === 0"><td colspan="8" class="empty">沒有符合條件的未確認預訂單。</td></tr>
      </tbody></table></div>
      <footer class="grid-footer"><span>{{ visibleBases.length }} 張單據</span><span>此功能只列出 State=0</span></footer>
    </section>

    <section class="panel detail-panel" aria-label="預訂單明細">
      <header class="panel-heading"><div><h2>PreOrder Detail</h2><span>{{ activeBasis?.preOrderNo ?? '請先選取預訂單' }}</span></div><small>{{ visibleDetails.length }} 筆明細 · {{ activeInventoryName }}</small></header>
      <div class="toolbar"><button type="button" class="primary" :disabled="!activeBasis" @click="openDetail('add')">ADD</button><button type="button" :disabled="!activeDetail" @click="openSelectedDetail('edit')">EDIT</button><button type="button" :disabled="!activeDetail" @click="deleteSelectedDetail">DEL</button><button type="button" :disabled="!activeDetail" @click="openSelectedDetail('copy')">COPY</button></div>
      <div class="table-wrap detail-table-wrap"><table><thead><tr><th>MaterialNo</th><th>UnitNo</th><th class="numeric">Qty</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead><tbody>
        <tr v-for="row in visibleDetails" :key="row.id" :class="{ active: selectedDetailId === row.id }" @click="selectedDetailId = row.id"><td class="mono">{{ row.materialNo }}</td><td>{{ row.unitNo }}</td><td class="numeric">{{ qty(row.qty) }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser || '—' }}</td><td>{{ row.reviseDate || '—' }}</td></tr>
        <tr v-if="activeBasis && visibleDetails.length === 0"><td colspan="7" class="empty">此預訂單尚無明細。</td></tr><tr v-else-if="!activeBasis"><td colspan="7" class="empty">選取上方預訂單後載入明細。</td></tr>
      </tbody></table></div>
      <footer class="grid-footer"><span>{{ visibleDetails.length }} 筆明細</span><span>Qty 顯示最多六位小數</span></footer>
    </section>

    <div v-if="basisDialogMode" class="backdrop" @click.self="basisDialogMode = null"><form class="dialog" role="dialog" aria-modal="true" aria-label="預訂單主檔" @submit.prevent="saveBasis">
      <header><div><p>IV205 · PREORDER BASIS</p><h2>{{ basisDialogMode === 'edit' ? '編輯預訂單' : basisDialogMode === 'copy' ? '複製預訂單' : '新增預訂單' }}</h2></div><button type="button" class="icon" aria-label="關閉" @click="basisDialogMode = null">×</button></header>
      <label>PreOrder No <span class="query-control"><input v-model="basisDraft.preOrderNo" readonly :placeholder="basisDialogMode === 'edit' ? '' : '按右側按鈕取號'" /><button v-if="basisDialogMode !== 'edit'" type="button" class="lookup-button" @click="takeSerial">取號</button></span></label>
      <label>Inventory No <span class="query-control"><input v-model="basisDraft.inventoryNo" :readonly="basisDialogMode === 'edit'" placeholder="原物料庫房" /><button v-if="basisDialogMode !== 'edit'" type="button" class="lookup-button" @click="lookupKind = 'inventory'">…</button></span></label>
      <label>Description<textarea v-model="basisDraft.description" rows="3" /></label>
      <p class="dialog-note">ADD／COPY 以 PREORDERNO DEV 序號取號；COPY 只複製庫房與說明，不帶明細。</p>
      <footer><button type="button" @click="basisDialogMode = null">Cancel</button><button class="primary" type="submit">{{ basisDialogMode === 'edit' ? 'Apply' : 'Save' }}</button></footer>
    </form></div>

    <div v-if="detailDialogMode" class="backdrop" @click.self="detailDialogMode = null"><form class="dialog" role="dialog" aria-modal="true" aria-label="預訂單明細" @submit.prevent="saveDetail">
      <header><div><p>IV205 · PREORDER DETAIL</p><h2>{{ detailDialogMode === 'edit' ? '編輯明細' : detailDialogMode === 'copy' ? '複製明細' : '新增明細' }}</h2></div><button type="button" class="icon" aria-label="關閉" @click="detailDialogMode = null">×</button></header>
      <label>PreOrder No<input :value="activeBasis?.preOrderNo" readonly /></label><label>Inventory No<input :value="activeBasis?.inventoryNo" readonly /></label>
      <label>Material No <span class="query-control"><input v-model="detailDraft.materialNo" readonly placeholder="從庫房原料挑選" /><button v-if="detailDialogMode !== 'edit'" type="button" class="lookup-button" @click="lookupKind = 'material'">…</button></span></label>
      <label>Unit No<input v-model="detailDraft.unitNo" readonly /></label><label>Qty<input v-model="detailDraft.qty" type="number" min="0" step="any" /></label>
      <p class="dialog-note">物料及單位由所選庫房原料帶入；數量必須大於 0。</p>
      <footer><button type="button" @click="detailDialogMode = null">Cancel</button><button class="primary" type="submit">{{ detailDialogMode === 'edit' ? 'Apply' : 'Save' }}</button></footer>
    </form></div>

    <div v-if="lookupKind" class="backdrop" @click.self="lookupKind = null"><section class="dialog lookup-dialog" role="dialog" aria-modal="true" :aria-label="lookupTitle"><header><div><p>DEV LOOKUP</p><h2>{{ lookupTitle }}</h2></div><button type="button" class="icon" aria-label="關閉" @click="lookupKind = null">×</button></header><button v-for="option in lookupOptions" :key="option.value" type="button" class="lookup-option" @click="chooseLookup(option.value)">{{ option.label }}</button><p v-if="lookupOptions.length === 0" class="dialog-note">此庫房沒有 DEV 原料選項。</p><footer><button type="button" @click="lookupKind = null">Close (X)</button></footer></section></div>

    <div v-if="confirmRequest" class="backdrop" @click.self="confirmRequest = null"><section class="dialog confirm-dialog" role="dialog" aria-modal="true" aria-label="確認刪除"><header><div><p>IV205 · DELETE</p><h2>確認刪除</h2></div><button type="button" class="icon" aria-label="關閉" @click="confirmRequest = null">×</button></header><p>刪除 {{ confirmRequest.label }}？{{ confirmRequest.kind === 'basis' ? '主檔與其明細會一起移除。' : '' }}</p><footer><button type="button" @click="confirmRequest = null">Cancel</button><button class="danger" type="button" @click="confirmDelete">Delete</button></footer></section></div>
  </main>
</template>

<style scoped>
.preorder-page { display:flex; flex-direction:column; gap:12px; width:100%; height:100%; min-height:0; padding:14px; box-sizing:border-box; overflow:auto; background:#f1efe6; color:#28271f; font:inherit; }
.page-heading { display:flex; align-items:center; justify-content:space-between; gap:16px; }
.page-heading p,.dialog header p { margin:0 0 4px; color:#827d6c; font-size:11px; font-weight:800; letter-spacing:.12em; }
.page-heading h1 { margin:0; font-size:22px; }.page-heading h1 span { margin-left:8px; color:#696655; font-size:15px; font-weight:500; }.page-heading small { display:block; margin-top:4px; color:#777366; }
.close-button,button { min-height:31px; padding:5px 11px; border:1px solid #c4c0b2; border-radius:4px; background:#fffdf7; color:#39372e; font:inherit; cursor:pointer; }button:disabled { opacity:.45; cursor:not-allowed; }
.notice { margin:0; padding:8px 10px; border:1px solid #d7d1bd; border-radius:4px; background:#faf7ed; color:#625d4d; font-size:12px; }
.query-panel,.panel { border:1px solid #c9c5b7; border-radius:5px; background:#fbfaf5; box-shadow:0 1px 3px #302d2112; }.query-panel { padding:10px 12px; }.query-form { display:flex; align-items:end; gap:10px; }.query-form label,.dialog label { display:flex; flex:1; flex-direction:column; gap:4px; color:#676353; font-size:11px; font-weight:700; }.query-form input,.dialog input,.dialog textarea { width:100%; min-width:0; min-height:32px; padding:5px 7px; box-sizing:border-box; border:1px solid #c9c5b7; border-radius:3px; background:white; color:#2d2b23; font:inherit; font-size:12px; }.dialog textarea { resize:vertical; }.query-form .primary,.primary { border-color:#3c4c3b; background:#3c4c3b; color:white; }.panel { display:flex; flex-direction:column; min-height:0; overflow:hidden; }.basis-panel { flex:1.1 1 230px; }.detail-panel { flex:.9 1 190px; }.panel-heading { display:flex; align-items:center; justify-content:space-between; gap:12px; min-height:43px; padding:7px 10px; border-bottom:1px solid #d8d4c7; background:#f7f5ee; }.panel-heading h2 { display:inline; margin:0; font-size:14px; }.panel-heading span,.panel-heading small { color:#777366; font-size:11px; }.panel-heading span { margin-left:8px; }.toolbar { display:flex; gap:6px; padding:7px 9px; border-bottom:1px solid #e4e0d5; }.table-wrap { flex:1; min-height:0; overflow:auto; }table { width:100%; border-collapse:collapse; white-space:nowrap; font-size:12px; }th { position:sticky; top:0; z-index:1; padding:7px 9px; border-bottom:1px solid #c9c5b7; background:#efede4; color:#625e50; text-align:left; font-weight:700; }td { padding:7px 9px; border-bottom:1px solid #ece9df; }tbody tr { cursor:pointer; }tbody tr:hover { background:#f3f0e6; }tbody tr.active { background:#e7eadf; }.mono { font-family:ui-monospace,SFMono-Regular,Consolas,monospace; }.numeric { text-align:right; font-variant-numeric:tabular-nums; }.description { max-width:260px; white-space:normal; }.state-badge { display:inline-flex; min-width:74px; justify-content:center; padding:2px 6px; border-radius:10px; background:#f5e6c6; color:#77551d; font-size:10px; }.empty { padding:18px; color:#827d6c; text-align:center; }.grid-footer { display:flex; justify-content:space-between; gap:12px; padding:7px 10px; border-top:1px solid #e4e0d5; color:#777366; font-size:10px; }
.backdrop { position:fixed; inset:0; z-index:50; display:grid; place-items:center; padding:16px; background:#1f211dcc; }.dialog { display:flex; flex-direction:column; gap:11px; width:min(490px,100%); max-height:90vh; overflow:auto; padding:15px; border:1px solid #d2cebf; border-radius:6px; background:#fbfaf5; box-shadow:0 16px 50px #0004; }.dialog header { display:flex; align-items:center; justify-content:space-between; }.dialog header h2 { margin:0; font-size:16px; }.dialog .icon { width:29px; min-height:29px; padding:2px; font-size:18px; }.dialog footer { display:flex; justify-content:flex-end; gap:8px; margin-top:3px; }.dialog-note { margin:0; color:#777366; font-size:11px; line-height:1.55; }.query-control { display:flex; gap:4px; }.query-control input { flex:1; }.lookup-button { flex:0 0 54px; }.lookup-option { text-align:left; }.danger { border-color:#8a3f38; background:#8a3f38; color:white; }.confirm-dialog > p { margin:0; line-height:1.5; }.lookup-dialog { width:min(400px,100%); }.lookup-dialog header { margin-bottom:4px; }
@media(max-width:780px) { .page-heading h1 { font-size:18px; }.panel-heading { align-items:flex-start; flex-direction:column; }.grid-footer { flex-direction:column; gap:3px; } }
</style>
