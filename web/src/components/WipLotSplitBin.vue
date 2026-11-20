<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type Action = 0 | 1 | 2
type ReturnType = 0 | 1 | 2
type BinNo = 'BIN001' | 'BIN002' | 'BIN003'
type Lot = {
  lotNo: string; baseLotNo: string; curQty: number; status: string; lotStamp: number
  productNo: string; productVersion: string; processNo: string; opNo: string
  goodBin: Record<BinNo, number> | null
}
type SplitRow = {
  splitLotNo: string; splitLotQty: number; mother: boolean; action: Action; returnType: ReturnType
  processNo: string; processVersion: string; opNo: string; nodeId: string
  productNo: string; productVersion: string
}
type BinAllocation = { splitLotNo: string; bins: Record<BinNo, number> }
type SplitRecord = { lotNo: string; lotStamp: number; rows: SplitRow[]; bins: BinAllocation[]; motherElectricBox: boolean; childElectricBoxes: string[]; savedAt: string }

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview !== false)
const bins: BinNo[] = ['BIN001', 'BIN002', 'BIN003']
const storageKey = 'ymes-dev-mock:wip-lot-split-bin:state:v1'
const fixtures: Lot[] = [
  { lotNo: 'DEV-LOT-BIN-001', baseLotNo: 'DEV-LOT-BIN-001', curQty: 12, status: 'Running', lotStamp: 1901, productNo: 'DEV-PRODUCT-A', productVersion: '01', processNo: 'DEV-PROC-FT', opNo: 'DEV-OP-FT-10', goodBin: { BIN001: 7, BIN002: 3, BIN003: 2 } },
  { lotNo: 'DEV-LOT-BIN-002', baseLotNo: 'DEV-LOT-BIN-002', curQty: 8, status: 'Queue', lotStamp: 1902, productNo: 'DEV-PRODUCT-B', productVersion: '01', processNo: 'DEV-PROC-CP', opNo: 'DEV-OP-CP-10', goodBin: { BIN001: 4, BIN002: 4, BIN003: 0 } },
  { lotNo: 'DEV-LOT-BIN-NO-GOODBINS', baseLotNo: 'DEV-LOT-BIN-NO-GOODBINS', curQty: 4, status: 'Queue', lotStamp: 1903, productNo: 'DEV-PRODUCT-A', productVersion: '01', processNo: 'DEV-PROC-FT', opNo: 'DEV-OP-FT-10', goodBin: null },
]
const routeOptions = [
  { action: 1 as Action, processNo: 'DEV-PROC-FT', processVersion: '02', opNo: 'DEV-OP-FT-30', nodeId: 'DEV-NODE-FT-30', label: 'Jump OP · DEV-OP-FT-30' },
  { action: 2 as Action, processNo: 'DEV-PROC-PACK', processVersion: '01', opNo: 'DEV-OP-PACK-10', nodeId: 'DEV-NODE-PACK-10', label: 'Jump Process · DEV-PROC-PACK' },
]
const products = [
  { productNo: 'DEV-PRODUCT-A', productVersion: '01' },
  { productNo: 'DEV-PRODUCT-B', productVersion: '01' },
]
// These two DEV fixtures represent IC_FT_PKG and AllowLotProductChange enabled.
const electricBoxEnabled = computed(() => dev.value)
const productChangeEnabled = computed(() => dev.value)

const lotNoInput = ref('')
const lot = ref<Lot | null>(null)
const mode = ref<'count' | 'qty'>('count')
const amountInput = ref('2')
const includeMother = ref(false)
const rows = ref<SplitRow[]>([])
const allocations = ref<BinAllocation[]>([])
const selectedLotNo = ref('')
const dialog = ref<'bin' | 'route' | 'product' | 'motherBox' | 'childBox' | null>(null)
const binDraft = ref<Record<BinNo, string>>({ BIN001: '0', BIN002: '0', BIN003: '0' })
const motherElectricBox = ref(false)
const childElectricBoxes = ref<string[]>([])
const settingsSnapshot = ref('')
const records = ref<SplitRecord[]>([])
const nextSequence = ref(1)
const submitted = ref(false)
const errorText = ref('')
const message = ref('請輸入有 GoodBinState 的參考批號，查詢後設定分批。')

const activeRow = computed(() => rows.value.find(row => row.splitLotNo === selectedLotNo.value) ?? null)
const activeAllocation = computed(() => allocations.value.find(item => item.splitLotNo === selectedLotNo.value) ?? null)
const activeBinTotal = computed(() => bins.reduce((sum, bin) => sum + (activeAllocation.value?.bins[bin] ?? 0), 0))
const canEdit = computed(() => dev.value && lot.value !== null && !submitted.value)
const totalLotQty = computed(() => rows.value.reduce((sum, row) => sum + row.splitLotQty, 0))
const remaining = computed(() => {
  const result = { BIN001: 0, BIN002: 0, BIN003: 0 } as Record<BinNo, number>
  if (!lot.value?.goodBin) return result
  for (const bin of bins) result[bin] = lot.value.goodBin[bin] - allocations.value.filter(item => item.splitLotNo !== selectedLotNo.value).reduce((sum, item) => sum + item.bins[bin], 0)
  return result
})

function readState(): { records: SplitRecord[]; nextSequence: number } {
  const stored = JSON.parse(window.localStorage.getItem(storageKey) || 'null') as { records?: SplitRecord[]; nextSequence?: number } | null
  const rawSequence = stored?.nextSequence
  const rawRecords = stored?.records
  return {
    records: Array.isArray(rawRecords) ? rawRecords : [],
    nextSequence: typeof rawSequence === 'number' && Number.isSafeInteger(rawSequence) && rawSequence > 0 ? rawSequence : 1,
  }
}

function loadState(): void {
  if (!dev.value) return
  try {
    const stored = readState()
    records.value = stored.records
    nextSequence.value = stored.nextSequence
  } catch { records.value = []; nextSequence.value = 1 }
}

function saveState(): void {
  window.localStorage.setItem(storageKey, JSON.stringify({ records: records.value, nextSequence: nextSequence.value }))
}

function clearDraft(): void {
  rows.value = []
  allocations.value = []
  selectedLotNo.value = ''
  childElectricBoxes.value = []
  motherElectricBox.value = false
  settingsSnapshot.value = ''
  dialog.value = null
  submitted.value = false
}

function queryLot(): void {
  errorText.value = ''
  clearDraft()
  if (!dev.value) { errorText.value = '此頁目前僅提供 DEV 假資料預覽。'; return }
  const source = fixtures.find(item => item.lotNo === lotNoInput.value.trim().toUpperCase())
  if (!source) { lot.value = null; errorText.value = '找不到 DEV Lot。可輸入 DEV-LOT-BIN-001。'; return }
  if (!source.goodBin) { lot.value = null; errorText.value = '此 Lot 沒有 GoodBinState，請使用一般分批處理。'; return }
  if (records.value.some(item => item.lotNo === source.lotNo && item.lotStamp === source.lotStamp)) { lot.value = null; errorText.value = '此 DEV Lot 已在本瀏覽器模擬分批，請改用另一筆假資料。'; return }
  lot.value = { ...source, goodBin: { ...source.goodBin } }
  lotNoInput.value = source.lotNo
  message.value = '已載入 Good Bin 資料；請建立分批列。'
}

function childLotNo(sequence: number): string { return `DEV-BIN-SPLIT-${String(sequence).padStart(6, '0')}` }

function split(): void {
  errorText.value = ''
  if (!canEdit.value || !lot.value) { errorText.value = '請先查詢有效的參考批。'; return }
  const amount = Number(amountInput.value)
  if (!Number.isInteger(amount) || amount <= 0) { errorText.value = 'DEV Count／Qty 請輸入大於 0 的整數。'; return }
  const quantities: number[] = []
  if (mode.value === 'count') {
    if (amount < 2 || amount > lot.value.curQty) { errorText.value = '批數至少 2，且不可超過 CurQty。'; return }
    const base = Math.floor(lot.value.curQty / amount)
    for (let i = 0; i < amount; i++) quantities.push(i === amount - 1 ? lot.value.curQty - base * (amount - 1) : base)
  } else {
    if (amount >= lot.value.curQty) { errorText.value = 'Split Qty 必須小於 CurQty。'; return }
    const full = Math.floor(lot.value.curQty / amount)
    for (let i = 0; i < full; i++) quantities.push(amount)
    const last = lot.value.curQty - full * amount
    if (last > 0) quantities.push(last)
  }
  if (quantities.length < 2 || quantities.length > 100) { errorText.value = 'DEV 預覽最多 100 列，至少 2 列。'; return }
  const source = lot.value
  let sequence = nextSequence.value
  rows.value = quantities.map((qty, index) => ({
    splitLotNo: includeMother.value && index === 0 ? source.lotNo : childLotNo(sequence++),
    splitLotQty: qty, mother: includeMother.value && index === 0, action: 0 as Action, returnType: 0 as ReturnType,
    processNo: '', processVersion: '', opNo: '', nodeId: '', productNo: source.productNo, productVersion: source.productVersion,
  }))
  allocations.value = []
  childElectricBoxes.value = []
  selectedLotNo.value = rows.value[0]?.splitLotNo || ''
  settingsSnapshot.value = `${mode.value}|${amountInput.value}|${includeMother.value}`
  message.value = `已建立 ${rows.value.length} 列；請逐列按 Bin Assign。`
}

function openBinDialog(): void {
  if (!canEdit.value || !activeRow.value || !lot.value?.goodBin) return
  const current = activeAllocation.value?.bins
  binDraft.value = Object.fromEntries(bins.map(bin => [bin, String(current?.[bin] ?? 0)])) as Record<BinNo, string>
  dialog.value = 'bin'
  errorText.value = ''
}

function confirmBinDialog(): void {
  const row = activeRow.value
  if (!row || !lot.value) return
  const values = {} as Record<BinNo, number>
  for (const bin of bins) {
    const qty = Number(binDraft.value[bin])
    if (!Number.isInteger(qty) || qty < 0 || qty > remaining.value[bin]) {
      errorText.value = `${bin} 的 Split Bin 必須是 0 至 Remaining ${remaining.value[bin]} 的整數。`
      return
    }
    values[bin] = qty
  }
  const index = allocations.value.findIndex(item => item.splitLotNo === selectedLotNo.value)
  const allocation = { splitLotNo: selectedLotNo.value, bins: values }
  if (index < 0) allocations.value.push(allocation)
  else allocations.value[index] = allocation
  row.splitLotQty = bins.reduce((sum, bin) => sum + values[bin], 0)
  dialog.value = null
  errorText.value = ''
  message.value = `${selectedLotNo.value} 已分配 ${row.splitLotQty} 個 Good Bin。`
}

function changeAction(row: SplitRow, value: string): void {
  row.action = Number(value) as Action
  row.processNo = ''; row.processVersion = ''; row.opNo = ''; row.nodeId = ''
}
function changeReturnType(row: SplitRow, value: string): void { row.returnType = Number(value) as ReturnType }
function chooseRoute(option: (typeof routeOptions)[number]): void {
  if (!activeRow.value) return
  activeRow.value.action = option.action
  activeRow.value.processNo = option.processNo
  activeRow.value.processVersion = option.processVersion
  activeRow.value.opNo = option.opNo
  activeRow.value.nodeId = option.nodeId
  dialog.value = null
}
function chooseProduct(option: (typeof products)[number]): void {
  if (!activeRow.value) return
  activeRow.value.productNo = option.productNo
  activeRow.value.productVersion = option.productVersion
  changeAction(activeRow.value, '0')
  dialog.value = null
}

function bindElectricBox(): void {
  if (dialog.value === 'motherBox') motherElectricBox.value = true
  if (dialog.value === 'childBox' && selectedLotNo.value && !childElectricBoxes.value.includes(selectedLotNo.value)) childElectricBoxes.value.push(selectedLotNo.value)
  message.value = 'DEV 靜電箱綁定已模擬；未寫入 MES。'
  dialog.value = null
}

function confirmSplit(): void {
  errorText.value = ''
  if (!dev.value || !lot.value || rows.value.length < 2 || submitted.value) { errorText.value = '請先查詢參考批並建立分批草稿。'; return }
  if (settingsSnapshot.value !== `${mode.value}|${amountInput.value}|${includeMother.value}`) { errorText.value = '分批條件已變更，請重新按 Split。'; return }
  for (const row of rows.value) {
    const allocation = allocations.value.find(item => item.splitLotNo === row.splitLotNo)
    if (!allocation) { errorText.value = `${row.splitLotNo} 尚未執行 Bin Assign。`; return }
    const binSum = bins.reduce((sum, bin) => sum + allocation.bins[bin], 0)
    if (row.splitLotQty <= 0 || binSum !== row.splitLotQty) { errorText.value = `${row.splitLotNo} 的 Split Lot Qty 必須等於 Bin 合計且大於 0。`; return }
    if ((row.action === 0 || row.action === 1) && row.returnType !== 0) { errorText.value = `${row.splitLotNo} 的 Go／Jump OP 只能選 Not Return。`; return }
    if (row.action !== 0 && !row.nodeId) { errorText.value = `${row.splitLotNo} 請用 Change Routing 指定目標節點。`; return }
  }
  if (totalLotQty.value !== lot.value.curQty) { errorText.value = `子批數量合計須等於 CurQty ${lot.value.curQty}。`; return }
  for (const bin of bins) {
    const sum = allocations.value.reduce((acc, item) => acc + item.bins[bin], 0)
    if (sum !== lot.value.goodBin?.[bin]) { errorText.value = `${bin} 子批分配合計須等於來源 Good Bin。`; return }
  }
  let persisted: { records: SplitRecord[]; nextSequence: number }
  try { persisted = readState() } catch { errorText.value = '無法讀取 DEV 模擬狀態。'; return }
  const currentSequence = persisted.nextSequence
  const children = rows.value.filter(row => !row.mother)
  if (currentSequence !== nextSequence.value || children.some((row, index) => row.splitLotNo !== childLotNo(currentSequence + index))) {
    nextSequence.value = currentSequence; errorText.value = 'DEV 預覽子批號已變動，請重新按 Split。'; return
  }
  if (persisted.records.some(item => item.lotNo === lot.value?.lotNo && item.lotStamp === lot.value?.lotStamp)) {
    errorText.value = '此 DEV Lot 已在另一個分頁模擬分批，請重新查詢。'; return
  }
  records.value = persisted.records
  records.value.push({ lotNo: lot.value.lotNo, lotStamp: lot.value.lotStamp, rows: rows.value.map(row => ({ ...row })), bins: allocations.value.map(item => ({ splitLotNo: item.splitLotNo, bins: { ...item.bins } })), motherElectricBox: motherElectricBox.value, childElectricBoxes: [...childElectricBoxes.value], savedAt: new Date().toISOString() })
  nextSequence.value += children.length
  try { saveState() } catch {
    records.value.pop()
    nextSequence.value -= children.length
    errorText.value = '瀏覽器無法儲存 DEV 模擬結果；請確認 localStorage 可用後重試。'
    return
  }
  submitted.value = true
  message.value = 'DEV 已模擬分批；結果只保存在此瀏覽器，未更新 MES。'
}

onMounted(loadState)
</script>

<template>
  <section class="wp209-page">
    <div v-if="dev" class="dev-banner">DEV 假資料預覽；Good Bin 分配與子批號只寫入此瀏覽器 localStorage。</div>
    <h1>分批處理_等級 <small>LOT SPLIT_BIN · WP209</small></h1>
    <p v-if="errorText" class="notice error" role="alert">{{ errorText }}</p>
    <p v-else class="notice" role="status">{{ message }}</p>

    <fieldset class="mother-frame">
      <legend>MLot</legend>
      <div class="query-row"><label for="wp209-lot">Lot No</label><input id="wp209-lot" v-model="lotNoInput" class="mes-input editable" :disabled="!dev" @keydown.enter.prevent="queryLot"><button type="button" class="mes-button" :disabled="!dev" @click="queryLot">Query</button><button v-if="electricBoxEnabled" type="button" class="mes-button" :disabled="!canEdit" @click="dialog = 'motherBox'">Eletricbox M.Lot</button></div>
      <div class="grid-wrap mother-grid"><table><thead><tr><th>Lot No</th><th>Cur Qty</th><th>Process No</th><th>OP No</th><th>Base Lot No</th><th>Product No</th><th>Version</th><th>Status</th></tr></thead><tbody><tr v-if="lot"><td>{{ lot.lotNo }}</td><td class="number">{{ lot.curQty }}</td><td>{{ lot.processNo }}</td><td>{{ lot.opNo }}</td><td>{{ lot.baseLotNo }}</td><td>{{ lot.productNo }}</td><td>{{ lot.productVersion }}</td><td>{{ lot.status }}</td></tr><tr v-else><td colspan="8" class="empty">請輸入 Lot No 並按 Query。</td></tr></tbody></table></div>
    </fieldset>

    <fieldset class="split-frame">
      <legend>Split</legend>
      <div class="split-controls"><span class="field-label">Split Type</span><fieldset class="radios"><label><input v-model="mode" type="radio" value="count" :disabled="!canEdit">Split by Count</label><label><input v-model="mode" type="radio" value="qty" :disabled="!canEdit">Split by Qty</label></fieldset><label for="wp209-amount">Split Count / Qty</label><input id="wp209-amount" v-model="amountInput" class="mes-input editable amount" :disabled="!canEdit" inputmode="numeric"></div>
      <div class="split-controls"><span class="field-label">Include Mother Lot No</span><fieldset class="radios"><label><input v-model="includeMother" type="radio" :value="true" :disabled="!canEdit">Yes</label><label><input v-model="includeMother" type="radio" :value="false" :disabled="!canEdit">No</label></fieldset><span class="spacer"></span><button type="button" class="mes-button" :disabled="!canEdit" @click="split">Split</button><button type="button" class="mes-button" :disabled="!canEdit || !activeRow" @click="openBinDialog">Bin Assign</button><button type="button" class="mes-button" :disabled="!canEdit || !activeRow" @click="dialog = 'route'">Change Routing</button><button v-if="electricBoxEnabled" type="button" class="mes-button" :disabled="!canEdit || !activeRow" @click="dialog = 'childBox'">Eletricbox</button></div>
      <div class="grid-wrap split-grid"><table><thead><tr><th>Split Lot No</th><th>Split Lot Qty</th><th>Action</th><th>Process No</th><th>Process Ver.</th><th>OP No</th><th>Return Type</th><th>Product No</th><th>Product Ver.</th></tr></thead><tbody><tr v-for="row in rows" :key="row.splitLotNo" :class="{ selected: selectedLotNo === row.splitLotNo }" @click="selectedLotNo = row.splitLotNo"><td>{{ row.splitLotNo }} <small v-if="row.mother">母批</small></td><td class="number readonly">{{ row.splitLotQty }}</td><td><select class="grid-select" :value="row.action" :disabled="!canEdit" @change="changeAction(row, ($event.target as HTMLSelectElement).value)"><option value="0">Go</option><option value="1">Jump OP</option><option value="2">Jump Process</option></select></td><td class="readonly">{{ row.processNo }}</td><td class="readonly">{{ row.processVersion }}</td><td class="readonly">{{ row.opNo }}</td><td><select class="grid-select" :value="row.returnType" :disabled="!canEdit" @change="changeReturnType(row, ($event.target as HTMLSelectElement).value)"><option value="0">Not Return</option><option value="1">Return to Current OP</option><option value="2">Return to Next OP</option></select></td><td><button v-if="productChangeEnabled" type="button" class="cell-button" :disabled="!canEdit" @click.stop="selectedLotNo = row.splitLotNo; dialog = 'product'">{{ row.productNo }}</button><span v-else>{{ row.productNo }}</span></td><td class="readonly">{{ row.productVersion }}</td></tr><tr v-if="!rows.length"><td colspan="9" class="empty">按 Split 建立分批列。</td></tr></tbody></table></div>
      <div class="grid-wrap bin-grid"><table><thead><tr><th>Split Lot No</th><th v-for="bin in bins" :key="bin">{{ bin }}</th><th>合計</th></tr></thead><tbody><tr v-if="activeAllocation"><td>{{ activeAllocation.splitLotNo }}</td><td v-for="bin in bins" :key="bin" class="number">{{ activeAllocation.bins[bin] }}</td><td class="number">{{ activeBinTotal }}</td></tr><tr v-else><td :colspan="bins.length + 2" class="empty">選取子批並按 Bin Assign 後顯示 Bin 分配。</td></tr></tbody></table></div>
      <div class="status-row">{{ rows.length }} row(s) · 子批 Qty {{ totalLotQty }} / {{ lot?.curQty ?? 0 }}</div>
    </fieldset>
    <footer class="form-footer"><span>{{ submitted ? 'DEV 模擬完成' : '確認前檢查 Bin 與子批數量守恆。' }}</span><button type="button" class="mes-button confirm" :disabled="!canEdit || rows.length === 0" @click="confirmSplit">Confirm(&amp;C)</button><button type="button" class="mes-button" @click="emit('close')">Close (&amp;X)</button></footer>

    <div v-if="dialog" class="modal-backdrop" @click.self="dialog = null">
      <section v-if="dialog === 'bin'" class="dialog" role="dialog" aria-modal="true" aria-label="Lot Split Bin Assign"><header><strong>Lot Split Bin Assign</strong><span>{{ selectedLotNo }}</span></header><div class="grid-wrap assign-grid"><table><thead><tr><th></th><th v-for="bin in bins" :key="bin">{{ bin }}</th></tr></thead><tbody><tr><th>Good Bin</th><td v-for="bin in bins" :key="bin" class="number">{{ lot?.goodBin?.[bin] ?? 0 }}</td></tr><tr><th>Remaining Bin</th><td v-for="bin in bins" :key="bin" class="number">{{ remaining[bin] }}</td></tr><tr><th>Split Bin</th><td v-for="bin in bins" :key="bin"><input v-model="binDraft[bin]" class="mes-input editable bin-input" inputmode="numeric"></td></tr></tbody></table></div><footer><button type="button" class="mes-button confirm" @click="confirmBinDialog">Confirm</button><button type="button" class="mes-button" @click="dialog = null">Close</button></footer></section>
      <section v-else-if="dialog === 'route'" class="dialog" role="dialog" aria-modal="true" aria-label="Change Routing"><header><strong>Change Routing</strong><span>{{ selectedLotNo }}</span></header><button v-for="option in routeOptions" :key="option.nodeId" type="button" class="option" @click="chooseRoute(option)">{{ option.label }}<small>{{ option.processNo }} / {{ option.processVersion }} · {{ option.nodeId }}</small></button><footer><button type="button" class="mes-button" @click="dialog = null">Close</button></footer></section>
      <section v-else-if="dialog === 'product'" class="dialog" role="dialog" aria-modal="true" aria-label="Change Product"><header><strong>Change Product</strong><span>{{ selectedLotNo }}</span></header><button v-for="option in products" :key="option.productNo" type="button" class="option" @click="chooseProduct(option)">{{ option.productNo }} / {{ option.productVersion }}</button><footer><button type="button" class="mes-button" @click="dialog = null">Close</button></footer></section>
      <section v-else class="dialog electric-dialog" role="dialog" aria-modal="true" aria-label="Eletricbox"><header><strong>{{ dialog === 'motherBox' ? 'Eletricbox M.Lot' : 'Eletricbox' }}</strong><span>{{ dialog === 'motherBox' ? lot?.lotNo : selectedLotNo }}</span></header><p>此視窗僅模擬靜電箱綁定。正式資料由 MES 服務管理。</p><footer><button type="button" class="mes-button confirm" @click="bindElectricBox">模擬確認</button><button type="button" class="mes-button" @click="dialog = null">Close</button></footer></section>
    </div>
  </section>
</template>

<style scoped>
.wp209-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:7px;padding:9px;box-sizing:border-box;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif}.dev-banner{padding:6px 9px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}h1{margin:0;font-size:18px;font-weight:600}h1 small{margin-left:8px;color:#777;font-size:12px;font-weight:400}.notice{margin:0;min-height:18px;color:#555}.notice.error{padding:5px 8px;border:1px solid #d6aaa6;background:#fff0ef;color:#99251e}
.mother-frame,.split-frame{min-width:0;margin:0;padding:5px 8px 8px;box-sizing:border-box;border:1px solid #bdbdbd;background:#fff}.mother-frame{height:151px;flex:0 0 151px}.split-frame{flex:1;min-height:285px;display:flex;flex-direction:column}legend{padding:0 5px}.query-row{min-height:42px;display:flex;align-items:center;gap:7px}.query-row label{width:120px;text-align:right}.mes-input{height:25px;padding:3px 6px;border:1px solid #888;border-radius:2px;background:#fff;color:#333;font:inherit;box-sizing:border-box}.query-row .mes-input{width:150px}.editable{background:#fff0b3}.mes-button{min-height:29px;padding:3px 10px;border:1px solid #b9b9b9;border-radius:3px;background:linear-gradient(#fff,#f3f3f3);color:#333;font:inherit;white-space:nowrap;cursor:pointer}.mes-button:hover:not(:disabled){background:#fff5dc}.mes-button:disabled{color:#999;cursor:default}.confirm{color:#fff;background:#f3a62a;border-color:#e99512}.confirm:hover:not(:disabled){background:#e99a1b}
.grid-wrap{min-width:0;overflow:auto;border:1px solid #a7a7a7;background:#fff}.mother-grid{height:90px}.grid-wrap table{width:100%;border-collapse:collapse;white-space:nowrap}.grid-wrap th{position:sticky;top:0;z-index:1;padding:5px 7px;border:1px solid #d4d4d4;background:#ead591;color:#393326;font-weight:500;text-align:left}.grid-wrap td{padding:4px 6px;border:1px solid #d2d2d2;background:#ffe89c}.grid-wrap tr.selected td{background:#ffedb3}.number{text-align:right;font-variant-numeric:tabular-nums}.empty{text-align:center!important;background:#fff!important;color:#777!important;height:40px}
.split-controls{min-height:40px;display:flex;align-items:center;gap:8px;flex-wrap:wrap}.field-label{width:120px;text-align:right}.radios{display:flex;gap:12px;margin:0;padding:4px 8px;border:1px solid #ccc}.radios label{display:flex;align-items:center;gap:3px;white-space:nowrap}.amount{width:65px;text-align:right}.spacer{flex:1}.split-grid{flex:1;min-height:100px}.split-grid tr{cursor:pointer}.split-grid tr:hover td{background:#fff0bd}.split-grid td{height:27px}.split-grid small{padding:1px 4px;border:1px solid #d2ad55;background:#fff4c7;color:#765e20}.readonly{background:#ececec!important}.grid-select{width:100%;min-width:88px;max-width:160px;height:25px;border:1px solid #999;background:#fff;color:#333;font:inherit}.cell-button{padding:2px 4px;border:0;background:transparent;color:#6b5222;font:inherit;text-decoration:underline;cursor:pointer}.cell-button:disabled{color:#777;text-decoration:none;cursor:default}.bin-grid{height:91px;flex:0 0 91px;margin-top:6px}.status-row{min-height:22px;padding:4px 6px;border:1px solid #c5c5c5;background:#eee;text-align:right;color:#555;box-sizing:border-box}.form-footer{min-height:36px;display:flex;justify-content:flex-end;align-items:center;gap:8px}.form-footer span{margin-right:auto;color:#777}
.modal-backdrop{position:fixed;inset:0;z-index:1300;display:grid;place-items:center;padding:18px;background:#0005}.dialog{width:min(680px,94vw);max-height:84vh;display:flex;flex-direction:column;gap:10px;padding:14px;border:1px solid #999;background:#fff;box-shadow:0 8px 28px #0004;box-sizing:border-box}.dialog header{display:flex;justify-content:space-between;gap:12px}.dialog header span{color:#666}.dialog footer{display:flex;justify-content:flex-end;gap:8px}.assign-grid{overflow:auto}.assign-grid tbody th{position:static;background:#eee}.assign-grid td{min-width:90px}.bin-input{width:75px;text-align:right}.option{display:flex;flex-direction:column;gap:4px;padding:9px;border:1px solid #ccc;background:#fff;text-align:left;color:#333;font:inherit;cursor:pointer}.option:hover{background:#fff3cc}.option small{color:#666}.electric-dialog{width:min(420px,94vw)}.electric-dialog p{margin:0;color:#666}@media(max-width:720px){.mother-frame{height:132px;flex-basis:132px}.mother-grid{height:72px}.field-label{width:90px}.split-controls{gap:5px}.form-footer span{display:none}}
</style>
