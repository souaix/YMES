<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type LookupMode = 'inventory' | 'material' | null
interface InventoryBasis { inventoryNo: string; materialNo: string; curQty: number; unitNo: string }
interface InventoryLot { key: string; inventoryNo: string; materialNo: string; materialLotNo: string; locatorNo: string; vendorNo: string; locQty: number }
interface WaferComponent { key: string; inventoryNo: string; materialNo: string; materialLotNo: string; locatorNo: string; componentNo: string; componentLotNo: string; slotNo: string; cassetteNo: string; laserMark: string; fixId: string; bookingFlag: boolean; importDate: string; testType: string; thickness: number; sheetResistance: number }
interface Query { inventoryNo: { checked: boolean; value: string }; materialNo: { checked: boolean; value: string }; materialLotNo: { checked: boolean; value: string }; componentNo: { checked: boolean; value: string }; vendorNo: { checked: boolean; value: string } }
interface ReturnNote { no: string; inventoryNo: string; materialNo: string; lotNo: string; locatorNo: string; qty: number; createdAt: string }

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: true })
const emit = defineEmits<{ close: [] }>()
const previewLabel = computed(() => props.developmentPreview ? 'DEV 假資料預覽' : 'DEV 模擬')
const inventories = [
  { no: 'RAW-WAFER-01', name: '晶圓來料庫 A' },
  { no: 'RAW-WAFER-02', name: '晶圓暫存庫 B' },
]
const materials = [
  { no: 'WFR-SI-300', name: '矽晶圓 300mm' },
  { no: 'SUB-GLASS-12', name: '封裝玻璃基板' },
]
const recipeTypes = ['RW', 'COW']
const basisRows = ref<InventoryBasis[]>([
  { inventoryNo: 'RAW-WAFER-01', materialNo: 'WFR-SI-300', curQty: 350, unitNo: 'PCS' },
  { inventoryNo: 'RAW-WAFER-01', materialNo: 'SUB-GLASS-12', curQty: 60, unitNo: 'PCS' },
])
const lotRows = ref<InventoryLot[]>([
  { key: 'wlot-a', inventoryNo: 'RAW-WAFER-01', materialNo: 'WFR-SI-300', materialLotNo: 'WLOT-2601', locatorNo: 'W-A-01', vendorNo: 'V-DEV-01', locQty: 200 },
  { key: 'wlot-b', inventoryNo: 'RAW-WAFER-01', materialNo: 'WFR-SI-300', materialLotNo: 'WLOT-2602', locatorNo: 'W-A-02', vendorNo: 'V-DEV-02', locQty: 150 },
  { key: 'glass-a', inventoryNo: 'RAW-WAFER-01', materialNo: 'SUB-GLASS-12', materialLotNo: 'GLS-2601', locatorNo: 'W-B-01', vendorNo: 'V-DEV-01', locQty: 60 },
])
const componentRows = ref<WaferComponent[]>([
  { key: 'wafer-001', inventoryNo: 'RAW-WAFER-01', materialNo: 'WFR-SI-300', materialLotNo: 'WLOT-2601', locatorNo: 'W-A-01', componentNo: 'WFR-001', componentLotNo: 'CW-2601', slotNo: '01', cassetteNo: 'CAS-01', laserMark: 'LM001', fixId: '', bookingFlag: false, importDate: '2026-11-18 10:30:00', testType: 'RW', thickness: 728.2, sheetResistance: 4.13 },
  { key: 'wafer-002', inventoryNo: 'RAW-WAFER-01', materialNo: 'WFR-SI-300', materialLotNo: 'WLOT-2601', locatorNo: 'W-A-01', componentNo: 'WFR-002', componentLotNo: 'CW-2601', slotNo: '02', cassetteNo: 'CAS-01', laserMark: 'LM002', fixId: 'FIX-DEV-01', bookingFlag: false, importDate: '2026-11-18 10:30:00', testType: 'RW', thickness: 728.7, sheetResistance: 4.09 },
  { key: 'wafer-003', inventoryNo: 'RAW-WAFER-01', materialNo: 'WFR-SI-300', materialLotNo: 'WLOT-2602', locatorNo: 'W-A-02', componentNo: 'WFR-003', componentLotNo: 'CW-2602', slotNo: '01', cassetteNo: 'CAS-02', laserMark: 'LM003', fixId: '', bookingFlag: true, importDate: '2026-11-19 15:10:00', testType: 'COW', thickness: 727.9, sheetResistance: 4.18 },
  { key: 'glass-001', inventoryNo: 'RAW-WAFER-01', materialNo: 'SUB-GLASS-12', materialLotNo: 'GLS-2601', locatorNo: 'W-B-01', componentNo: 'SUB-001', componentLotNo: 'GLS-C-01', slotNo: '01', cassetteNo: 'GLS-CAS-01', laserMark: '', fixId: '', bookingFlag: false, importDate: '2026-11-17 08:45:00', testType: 'RW', thickness: 0, sheetResistance: 0 },
])
const query = ref<Query>({
  inventoryNo: { checked: true, value: 'RAW-WAFER-01' },
  materialNo: { checked: false, value: '' },
  materialLotNo: { checked: false, value: '' },
  componentNo: { checked: false, value: '' },
  vendorNo: { checked: false, value: '' },
})
const appliedQuery = ref<Query | null>(null)
const selectedBasisKey = ref('')
const selectedLotKey = ref('')
const selectedDeleteKeys = ref<string[]>([])
const notice = ref('DEV 假資料：按 Query 查詢庫存；Import／Delete 只模擬畫面狀態，不呼叫正式 API。')
const latestReturnNotes = ref<ReturnNote[]>([])
const importOpen = ref(false)
const deleteConfirmOpen = ref(false)
const lookupMode = ref<LookupMode>(null)
const lookupText = ref('')
const lookupSelection = ref('')
const importForm = ref({ inventoryNo: 'RAW-WAFER-01', locatorNo: 'W-A-03', vendorNo: 'V-DEV-01', psNo: 'PS-DEV-01', recipeType: 'RW', fileName: '', importDate: '2026-11-25T16:30', importer: 'DEV001' })
const inventoryLookupRows = computed(() => inventories.filter((row) => !lookupText.value || row.no.toLowerCase().includes(lookupText.value.toLowerCase()) || row.name.includes(lookupText.value)))
const materialLookupRows = computed(() => materials.filter((row) => !lookupText.value || row.no.toLowerCase().includes(lookupText.value.toLowerCase()) || row.name.includes(lookupText.value)))
const filterValue = (field: keyof Query): string => {
  const criteria = appliedQuery.value
  if (!criteria?.[field].checked) return ''
  return criteria[field].value.trim().toUpperCase()
}
const matchesText = (value: string, field: keyof Query): boolean => {
  const filter = filterValue(field)
  return !filter || value.toUpperCase() === filter
}
const visibleLots = computed(() => {
  if (!appliedQuery.value) return []
  return lotRows.value.filter((row) => matchesText(row.inventoryNo, 'inventoryNo')
    && matchesText(row.materialNo, 'materialNo')
    && matchesText(row.materialLotNo, 'materialLotNo')
    && matchesText(row.vendorNo, 'vendorNo')
    && (!filterValue('componentNo') || componentRows.value.some((component) => component.inventoryNo === row.inventoryNo && component.materialNo === row.materialNo && component.materialLotNo === row.materialLotNo && component.locatorNo === row.locatorNo && matchesText(component.componentNo, 'componentNo'))))
})
const visibleBasis = computed(() => {
  const keys = new Set(visibleLots.value.map((row) => `${row.inventoryNo}|${row.materialNo}`))
  return basisRows.value.filter((row) => keys.has(`${row.inventoryNo}|${row.materialNo}`)).sort((a, b) => `${a.inventoryNo}|${a.materialNo}`.localeCompare(`${b.inventoryNo}|${b.materialNo}`))
})
const activeBasis = computed(() => visibleBasis.value.find((row) => `${row.inventoryNo}|${row.materialNo}` === selectedBasisKey.value) ?? null)
const visibleLotsForBasis = computed(() => activeBasis.value ? visibleLots.value.filter((row) => row.inventoryNo === activeBasis.value?.inventoryNo && row.materialNo === activeBasis.value?.materialNo).sort((a, b) => a.materialLotNo.localeCompare(b.materialLotNo) || a.locatorNo.localeCompare(b.locatorNo)) : [])
const activeLot = computed(() => visibleLotsForBasis.value.find((row) => row.key === selectedLotKey.value) ?? null)
const visibleComponents = computed(() => activeLot.value ? componentRows.value.filter((row) => row.inventoryNo === activeLot.value?.inventoryNo && row.materialNo === activeLot.value?.materialNo && row.materialLotNo === activeLot.value?.materialLotNo && row.locatorNo === activeLot.value?.locatorNo && matchesText(row.componentNo, 'componentNo')).sort((a, b) => a.componentNo.localeCompare(b.componentNo)) : [])
const selectedDeleteCount = computed(() => selectedDeleteKeys.value.filter((key) => visibleLotsForBasis.value.some((row) => row.key === key)).length)
const totalComponentCount = computed(() => visibleComponents.value.length)

watch(visibleBasis, (rows) => {
  if (!rows.some((row) => `${row.inventoryNo}|${row.materialNo}` === selectedBasisKey.value)) selectedBasisKey.value = rows[0] ? `${rows[0].inventoryNo}|${rows[0].materialNo}` : ''
}, { immediate: true })
watch(activeBasis, () => { selectedLotKey.value = ''; selectedDeleteKeys.value = [] })

function qtyText(value: number): string { return new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 6 }).format(value) }
function runQuery(): void {
  appliedQuery.value = JSON.parse(JSON.stringify(query.value)) as Query
  selectedBasisKey.value = ''
  selectedLotKey.value = ''
  selectedDeleteKeys.value = []
  notice.value = `DEV 查詢完成：${visibleBasis.value.length} 筆庫存料號、${visibleLots.value.length} 筆批號／儲位。`
}
function clearQuery(): void {
  query.value = { inventoryNo: { checked: false, value: '' }, materialNo: { checked: false, value: '' }, materialLotNo: { checked: false, value: '' }, componentNo: { checked: false, value: '' }, vendorNo: { checked: false, value: '' } }
  appliedQuery.value = null; selectedBasisKey.value = ''; selectedLotKey.value = ''; selectedDeleteKeys.value = []
  notice.value = '查詢條件已清除；請按 Query 載入庫存。'
}
function selectBasis(row: InventoryBasis): void { selectedBasisKey.value = `${row.inventoryNo}|${row.materialNo}`; selectedLotKey.value = ''; selectedDeleteKeys.value = [] }
function selectLot(row: InventoryLot): void { selectedLotKey.value = row.key }
function toggleDeleteSelection(row: InventoryLot): void { selectedDeleteKeys.value = selectedDeleteKeys.value.includes(row.key) ? selectedDeleteKeys.value.filter((key) => key !== row.key) : [...selectedDeleteKeys.value, row.key] }
function openLookup(mode: Exclude<LookupMode, null>): void { lookupMode.value = mode; lookupText.value = ''; lookupSelection.value = mode === 'inventory' ? query.value.inventoryNo.value : query.value.materialNo.value }
function chooseLookup(): void {
  if (!lookupSelection.value || !lookupMode.value) return
  const field = lookupMode.value === 'inventory' ? 'inventoryNo' : 'materialNo'
  query.value[field] = { checked: true, value: lookupSelection.value }
  lookupMode.value = null
}
function setImportFile(event: Event): void {
  const file = (event.target as HTMLInputElement).files?.[0]
  importForm.value.fileName = file?.name ?? ''
}
function simulateTemplate(action: 'upload' | 'download'): void { notice.value = action === 'upload' ? 'DEV 模擬：模板上傳只顯示操作結果，不傳送檔案。' : 'DEV 模擬：模板下載功能未連模板服務。' }
function simulateImport(): void {
  const form = importForm.value
  if (!form.inventoryNo || !form.locatorNo || !form.vendorNo || !form.psNo || !form.recipeType || !form.fileName) {
    notice.value = '匯入資料不完整：請提供庫房、儲位、供應商、PS、TestRecipeType 與檔案。'
    return
  }
  const incomingComponents = ['WFR-DEV-901', 'WFR-DEV-902', 'WFR-DEV-903']
  const materialNo = 'WFR-SI-300'
  const lotNo = 'WLOT-DEV-IMPORT'
  if (componentRows.value.some((row) => row.inventoryNo === form.inventoryNo && row.materialNo === materialNo && row.materialLotNo === lotNo)) {
    notice.value = '匯入模擬失敗：此料號／料批已存在相同元件，符合舊 Core 的重複元件阻擋情境。'
    return
  }
  const capacity = 120
  if (incomingComponents.length > capacity) {
    notice.value = `匯入模擬失敗：${incomingComponents.length} 個元件超過目的儲位容量 ${capacity}。`
    return
  }
  const key = `${form.inventoryNo}|${materialNo}`
  const basis = basisRows.value.find((row) => `${row.inventoryNo}|${row.materialNo}` === key)
  if (basis) basis.curQty += incomingComponents.length
  else basisRows.value.push({ inventoryNo: form.inventoryNo, materialNo, curQty: incomingComponents.length, unitNo: 'PCS' })
  lotRows.value.push({ key: `import-${Date.now()}`, inventoryNo: form.inventoryNo, materialNo, materialLotNo: lotNo, locatorNo: form.locatorNo, vendorNo: form.vendorNo, locQty: incomingComponents.length })
  incomingComponents.forEach((componentNo, index) => componentRows.value.push({ key: `${componentNo}-${Date.now()}`, inventoryNo: form.inventoryNo, materialNo, materialLotNo: lotNo, locatorNo: form.locatorNo, componentNo, componentLotNo: `${lotNo}-C`, slotNo: String(index + 1).padStart(2, '0'), cassetteNo: 'CAS-DEV-09', laserMark: `LM90${index + 1}`, fixId: '', bookingFlag: false, importDate: `${form.importDate.replace('T', ' ')}:00`, testType: form.recipeType, thickness: 728 + index / 10, sheetResistance: 4.1 + index / 100 }))
  importOpen.value = false
  if (appliedQuery.value) {
    selectedBasisKey.value = key
    selectedLotKey.value = lotRows.value[lotRows.value.length - 1]?.key ?? ''
  }
  notice.value = `DEV 匯入成功：${form.fileName} 已模擬新增 ${incomingComponents.length} 個元件、${lotNo}；產生已確認的進料單 MATIN-DEV-${String(Date.now()).slice(-5)}。`
  form.fileName = ''
}
function deleteSelected(): void {
  const keys = selectedDeleteKeys.value.length ? [...selectedDeleteKeys.value] : activeLot.value ? [activeLot.value.key] : []
  if (!keys.length) { notice.value = '請先選取一筆批號／儲位明細。'; return }
  const notes: ReturnNote[] = []
  let stoppedAt = ''
  for (const key of keys) {
    const row = lotRows.value.find((item) => item.key === key)
    if (!row) continue
    const anyBookedInLot = componentRows.value.some((item) => item.inventoryNo === row.inventoryNo && item.materialNo === row.materialNo && item.materialLotNo === row.materialLotNo && item.bookingFlag)
    if (anyBookedInLot) {
      stoppedAt = `${row.materialLotNo} 尚有元件 BookingFlag=1`
      break
    }
    basisRows.value = basisRows.value.map((item) => item.inventoryNo === row.inventoryNo && item.materialNo === row.materialNo ? { ...item, curQty: item.curQty - row.locQty } : item).filter((item) => item.curQty > 0)
    lotRows.value = lotRows.value.filter((item) => item.key !== row.key)
    componentRows.value = componentRows.value.filter((item) => !(item.inventoryNo === row.inventoryNo && item.materialNo === row.materialNo && item.materialLotNo === row.materialLotNo && item.locatorNo === row.locatorNo))
    notes.push({ no: `MATRET-DEV-${String(Date.now() + notes.length).slice(-5)}`, inventoryNo: row.inventoryNo, materialNo: row.materialNo, lotNo: row.materialLotNo, locatorNo: row.locatorNo, qty: row.locQty, createdAt: new Date().toLocaleString('sv-SE') })
  }
  latestReturnNotes.value = [...notes, ...latestReturnNotes.value].slice(0, 4)
  selectedDeleteKeys.value = []; selectedLotKey.value = ''
  if (stoppedAt) notice.value = `刪除中止：${stoppedAt}。${notes.length ? `先前 ${notes.length} 筆已完成，並建立 ${notes.map((item) => item.no).join('、')}；舊 Service 不會復原已成功列。` : '未完成任何列。'}`
  else if (notes.length) notice.value = `DEV 已移除 ${notes.length} 筆儲位庫存，並模擬建立 State=2 已確認退料單：${notes.map((item) => item.no).join('、')}。`
}
</script>

<template>
  <main class="wafer-page" aria-label="來料庫房管理 DEV 預覽">
    <header class="page-head"><div><p class="eyebrow">INV / IVB01 · {{ previewLabel }}</p><h1>來料庫房管理</h1><p class="sub">原物料倉 · 晶圓批號、儲位及電性摘要</p></div><button type="button" class="close" title="Close (X)" @click="emit('close')">×</button></header>
    <p class="notice" role="status">{{ notice }}</p>
    <section class="query-panel" aria-label="庫存查詢條件">
      <label class="query-field"><span><input v-model="query.inventoryNo.checked" type="checkbox" /> InventoryNo</span><div><input v-model.trim="query.inventoryNo.value" placeholder="原料庫房" @keyup.enter="runQuery" /><button type="button" @click="openLookup('inventory')">…</button></div></label>
      <label class="query-field"><span><input v-model="query.materialNo.checked" type="checkbox" /> MaterialNo</span><div><input v-model.trim="query.materialNo.value" placeholder="物料編號" @keyup.enter="runQuery" /><button type="button" @click="openLookup('material')">…</button></div></label>
      <label class="query-field"><span><input v-model="query.materialLotNo.checked" type="checkbox" /> MaterialLotNo</span><input v-model.trim="query.materialLotNo.value" placeholder="料批" @keyup.enter="runQuery" /></label>
      <label class="query-field"><span><input v-model="query.componentNo.checked" type="checkbox" /> ComponentNo</span><input v-model.trim="query.componentNo.value" placeholder="元件片號" @keyup.enter="runQuery" /></label>
      <label class="query-field"><span><input v-model="query.vendorNo.checked" type="checkbox" /> VendorNo</span><input v-model.trim="query.vendorNo.value" placeholder="供應商" @keyup.enter="runQuery" /></label>
      <div class="query-actions"><span>勾選的條件以精確值查詢；未勾選的文字不會送出。</span><div><button type="button" @click="clearQuery">Clear</button><button type="button" class="primary" @click="runQuery">Query</button></div></div>
    </section>
    <section class="toolbar"><span>主檔 {{ visibleBasis.length }} 筆 · 儲位批號 {{ visibleLots.length }} 筆 · 所選儲位元件 {{ totalComponentCount }} 筆</span><div><button type="button" @click="importOpen = true">Import</button><button type="button" class="danger" :disabled="!activeLot && !selectedDeleteCount" @click="deleteConfirmOpen = true">Delete {{ selectedDeleteCount ? `(${selectedDeleteCount})` : '' }}</button></div></section>
    <section class="grid-card basis-card"><header><strong>Raw Inventory Basis</strong><span>{{ activeBasis ? `${activeBasis.inventoryNo} · ${activeBasis.materialNo}` : '選取料號' }}</span></header><div class="grid-scroll"><table><thead><tr><th>InventoryNo</th><th>MaterialNo</th><th class="num">CurQty</th><th>UnitNo</th></tr></thead><tbody><tr v-for="row in visibleBasis" :key="row.inventoryNo + row.materialNo" :class="{ active: activeBasis?.inventoryNo === row.inventoryNo && activeBasis?.materialNo === row.materialNo }" @click="selectBasis(row)"><td class="mono">{{ row.inventoryNo }}</td><td class="mono">{{ row.materialNo }}</td><td class="num">{{ qtyText(row.curQty) }}</td><td>{{ row.unitNo }}</td></tr><tr v-if="!appliedQuery"><td colspan="4" class="empty">設定條件後按 Query 載入。</td></tr><tr v-else-if="!visibleBasis.length"><td colspan="4" class="empty">沒有符合條件的庫存。</td></tr></tbody></table></div></section>
    <section class="detail-layout">
      <section class="grid-card detail-card"><header><strong>Raw Inventory Locator / Lot</strong><span>{{ visibleLotsForBasis.length }} 筆 · 可勾選多筆刪除</span></header><div class="grid-scroll"><table><thead><tr><th class="check"></th><th>MaterialLotNo</th><th>VendorNo</th><th>LocatorNo</th><th class="num">LocQty</th></tr></thead><tbody><tr v-for="row in visibleLotsForBasis" :key="row.key" :class="{ active: activeLot?.key === row.key }" @click="selectLot(row)"><td><input type="checkbox" :checked="selectedDeleteKeys.includes(row.key)" @click.stop="toggleDeleteSelection(row)" /></td><td class="mono">{{ row.materialLotNo }}</td><td>{{ row.vendorNo }}</td><td>{{ row.locatorNo }}</td><td class="num">{{ qtyText(row.locQty) }}</td></tr><tr v-if="!activeBasis"><td colspan="5" class="empty">選取原料庫存主檔。</td></tr><tr v-else-if="!visibleLotsForBasis.length"><td colspan="5" class="empty">沒有符合條件的批號／儲位。</td></tr></tbody></table></div></section>
      <section class="grid-card component-card"><header><strong>Wafer / Test Summary</strong><span>{{ visibleComponents.length }} 個元件 · 僅唯讀</span></header><div class="grid-scroll"><table><thead><tr><th>ComponentNo</th><th>ComponentLotNo</th><th>SlotNo</th><th>Cassette</th><th>LaserMark</th><th>FixID</th><th>ImportDate</th><th>TestType_Thickness</th><th>TestType_SheetResistance</th><th>Booking</th></tr></thead><tbody><tr v-for="row in visibleComponents" :key="row.key"><td class="mono">{{ row.componentNo }}</td><td class="mono">{{ row.componentLotNo }}</td><td>{{ row.slotNo }}</td><td>{{ row.cassetteNo || '—' }}</td><td>{{ row.laserMark || '—' }}</td><td>{{ row.fixId || '—' }}</td><td>{{ row.importDate }}</td><td class="num">{{ row.thickness ? qtyText(row.thickness) : '—' }}</td><td class="num">{{ row.sheetResistance ? qtyText(row.sheetResistance) : '—' }}</td><td><span class="booking" :class="row.bookingFlag ? 'booked' : 'free'">{{ row.bookingFlag ? 'Booked' : 'Free' }}</span></td></tr><tr v-if="appliedQuery && !activeLot"><td colspan="10" class="empty">選取一筆批號／儲位載入元件與測試摘要。</td></tr><tr v-else-if="activeLot && !visibleComponents.length"><td colspan="10" class="empty">此批號／儲位沒有元件。</td></tr><tr v-else-if="!appliedQuery"><td colspan="10" class="empty">按 Query 並選取批號／儲位。</td></tr></tbody></table></div></section>
    </section>
    <section v-if="latestReturnNotes.length" class="return-panel"><header><strong>DEV 模擬：刪除時自動建立的退料單</strong><span>舊 Core 將 State 直接設為 2</span></header><div class="return-list"><span v-for="row in latestReturnNotes" :key="row.no"><b>{{ row.no }}</b> {{ row.materialNo }} / {{ row.lotNo }} / {{ row.locatorNo }} · {{ qtyText(row.qty) }} PCS</span></div></section>
    <aside class="source-note"><strong>DEV 假資料</strong><span>查詢、匯入與刪除只變更本頁記憶體；不解析真實匯入格式、不寫庫存或正式退料單。</span></aside>

    <div v-if="importOpen" class="backdrop" role="presentation" @click.self="importOpen = false"><section class="dialog import-dialog" role="dialog" aria-modal="true" aria-label="晶圓來料匯入"><header><div><p class="eyebrow">IVB01 / IMPORT</p><h2>來料測試資料匯入</h2></div><button type="button" class="close" @click="importOpen = false">×</button></header><p class="dialog-intro">舊程式依供應商 AutoLoader 格式解析 CSV／Excel，驗證測試 Recipe 後建立已確認進料與原料庫存。</p>
        <div class="form-grid"><label>InventoryNo<select v-model="importForm.inventoryNo"><option v-for="row in inventories" :key="row.no" :value="row.no">{{ row.no }} · {{ row.name }}</option></select></label><label>LocatorNo<input v-model.trim="importForm.locatorNo" /></label><label>VendorNo<input v-model.trim="importForm.vendorNo" /></label><label>PSNo<input v-model.trim="importForm.psNo" /></label><label>TestRecipeType<select v-model="importForm.recipeType"><option v-for="item in recipeTypes" :key="item" :value="item">{{ item }}</option></select></label><label>Importer<input v-model="importForm.importer" readonly /></label><label>ImportDate<input v-model="importForm.importDate" type="datetime-local" /></label><label class="file-field">FileName<input type="file" accept=".csv,.xls,.xlsx,.txt" @change="setImportFile" /><small>{{ importForm.fileName || '選擇本機檔案（DEV 僅記錄檔名）' }}</small></label></div>
        <section class="import-preview"><header><strong>匯入預覽（虛構資料）</strong><span>Header 欄位依 vendor script 映射</span></header><table><thead><tr><th>ComponentNo</th><th>ComponentLotNo</th><th>SlotNo</th><th>Thickness</th><th>SheetResistance</th></tr></thead><tbody><tr><td>WFR-DEV-901</td><td>WLOT-DEV-IMPORT-C</td><td>01</td><td>728.0</td><td>4.10</td></tr><tr><td>WFR-DEV-902</td><td>WLOT-DEV-IMPORT-C</td><td>02</td><td>728.1</td><td>4.11</td></tr><tr><td>WFR-DEV-903</td><td>WLOT-DEV-IMPORT-C</td><td>03</td><td>728.2</td><td>4.12</td></tr></tbody></table><p>DEV 模擬容量上限 120；舊 Core 目前只比較本次匯入片數，未把儲位已有數量加總。</p></section>
        <div class="template-actions"><span>AutoLoader template</span><button type="button" @click="simulateTemplate('upload')">Upload Template (DEV)</button><button type="button" @click="simulateTemplate('download')">Download Template (DEV)</button></div>
        <footer><button type="button" @click="importOpen = false">Cancel</button><button type="button" class="primary" @click="simulateImport">Confirm Import (DEV)</button></footer></section></div>

    <div v-if="deleteConfirmOpen" class="backdrop" role="presentation" @click.self="deleteConfirmOpen = false"><section class="dialog confirm-dialog" role="dialog" aria-modal="true" aria-label="確認刪除來料庫存"><header><div><p class="eyebrow">IVB01 / DELETE</p><h2>確認刪除選取儲位</h2></div><button type="button" class="close" @click="deleteConfirmOpen = false">×</button></header><p class="dialog-intro">舊 Core 會扣除原料庫存、刪除料批元件與測試摘要，並逐筆建立 State=2 已確認退料單；若元件已 Booking，刪除會停止。DEV 只更新本頁假資料。</p><p class="confirm-count">將處理 {{ selectedDeleteCount || (activeLot ? 1 : 0) }} 筆儲位明細。</p><footer><button type="button" @click="deleteConfirmOpen = false">Cancel</button><button type="button" class="primary" @click="deleteConfirmOpen = false; deleteSelected()">Confirm Delete (DEV)</button></footer></section></div>

    <div v-if="lookupMode" class="backdrop" role="presentation" @click.self="lookupMode = null"><section class="dialog lookup-dialog" role="dialog" aria-modal="true" :aria-label="lookupMode === 'inventory' ? '選擇庫房' : '選擇物料'"><header><div><p class="eyebrow">INV / IVB01</p><h2>{{ lookupMode === 'inventory' ? 'Inventory Lookup' : 'Material Lookup' }}</h2></div><button type="button" class="close" @click="lookupMode = null">×</button></header><label class="lookup-search">搜尋<input v-model.trim="lookupText" placeholder="代碼或名稱" /></label><div class="lookup-results"><template v-if="lookupMode === 'inventory'"><button v-for="row in inventoryLookupRows" :key="row.no" type="button" :class="{ active: lookupSelection === row.no }" @click="lookupSelection = row.no"><strong>{{ row.no }}</strong><span>{{ row.name }}</span></button></template><template v-else><button v-for="row in materialLookupRows" :key="row.no" type="button" :class="{ active: lookupSelection === row.no }" @click="lookupSelection = row.no"><strong>{{ row.no }}</strong><span>{{ row.name }}</span></button></template></div><footer><button type="button" @click="lookupMode = null">Cancel</button><button type="button" class="primary" :disabled="!lookupSelection" @click="chooseLookup">Select</button></footer></section></div>
  </main>
</template>

<style scoped>
.wafer-page { height: 100%; min-height: 0; box-sizing: border-box; display: flex; flex-direction: column; gap: 8px; overflow: auto; padding: 10px 12px 14px; background: #f7f5ee; color: #26251f; font: inherit; }
.page-head, .grid-card > header, .return-panel > header, .query-actions, .toolbar, .dialog header, .dialog footer, .import-preview > header { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.page-head { flex: 0 0 auto; padding: 2px 2px 0; }.page-head h1 { margin: 2px 0; font-size: 21px; }.eyebrow { margin: 0; color: #777366; font-size: 11px; letter-spacing: .08em; text-transform: uppercase; }.sub { margin: 0; color: #777366; font-size: 12px; }.close { border: 0; background: transparent; color: #555247; font-size: 24px; cursor: pointer; }
.notice { flex: 0 0 auto; margin: 0; padding: 7px 10px; border: 1px solid #d9d1b8; border-radius: 4px; background: #fffbed; color: #5b543f; font-size: 12px; }.query-panel, .grid-card, .return-panel { border: 1px solid #d9d5c8; border-radius: 4px; background: #fff; box-shadow: 0 1px 2px #16130a0a; }.query-panel { flex: 0 0 auto; display: grid; grid-template-columns: repeat(5, minmax(150px, 1fr)); align-items: end; gap: 10px 12px; padding: 10px; }.query-field { display: flex; min-width: 0; flex-direction: column; gap: 4px; color: #5e5a4e; font-size: 12px; }.query-field > span { display: flex; align-items: center; gap: 5px; }.query-field input[type=checkbox], .detail-card input[type=checkbox] { accent-color: #536448; }.query-field > div { display: flex; gap: 5px; }.query-field > div input { flex: 1; min-width: 0; }.query-actions { grid-column: 1 / -1; }.query-actions > span, .grid-card > header span, .toolbar > span, .return-panel > header span { color: #777366; font-size: 12px; }.query-actions > div, .toolbar > div, .template-actions { display: flex; align-items: center; gap: 7px; }
input, select { min-width: 0; box-sizing: border-box; min-height: 30px; border: 1px solid #ccc7b8; border-radius: 3px; background: #fff; color: #26251f; padding: 5px 7px; font: inherit; }button { min-height: 29px; border: 1px solid #b9b3a3; border-radius: 3px; background: #f7f5ee; color: #302f29; padding: 4px 10px; font: inherit; cursor: pointer; }button:hover:not(:disabled) { background: #ede9dc; }button:disabled { cursor: not-allowed; opacity: .48; }button.primary { border-color: #454237; background: #454237; color: #fff; }button.danger { border-color: #ad7971; color: #8d443b; }
.toolbar { flex: 0 0 auto; padding: 0 2px; }.grid-card { display: flex; min-height: 150px; flex-direction: column; overflow: hidden; }.basis-card { flex: 0 0 155px; }.grid-card > header, .return-panel > header { flex: 0 0 auto; min-height: 34px; padding: 6px 10px; border-bottom: 1px solid #e5e1d6; }.grid-card > header strong, .return-panel > header strong { font-size: 13px; }.grid-scroll { min-height: 0; flex: 1 1 auto; overflow: auto; }table { width: 100%; border-collapse: collapse; white-space: nowrap; font-size: 12px; }th, td { padding: 6px 8px; border-bottom: 1px solid #ebe8df; text-align: left; }th { position: sticky; top: 0; z-index: 1; background: #f1efe8; color: #5b584e; font-weight: 600; }tbody tr { cursor: pointer; }tbody tr:hover { background: #faf8f0; }tbody tr.active { background: #e9f0e8; }.mono { font-family: Consolas, 'Courier New', monospace; }.num { text-align: right; font-variant-numeric: tabular-nums; }.empty { height: 48px; color: #898578; text-align: center; }.detail-layout { display: grid; min-height: 300px; flex: 1 1 auto; grid-template-columns: minmax(350px, .9fr) minmax(560px, 1.5fr); gap: 8px; }.detail-card, .component-card { min-height: 240px; }.booking { display: inline-block; border-radius: 10px; padding: 2px 8px; }.booking.free { background: #edf7eb; color: #37623a; }.booking.booked { background: #faece8; color: #91483e; }.return-panel { flex: 0 0 auto; }.return-list { display: grid; gap: 5px; padding: 8px 10px; color: #696658; font-size: 11px; }.return-list b { color: #443f32; }.source-note { flex: 0 0 auto; display: flex; gap: 10px; padding: 2px; color: #777366; font-size: 11px; }.source-note strong { color: #4b493f; }
.backdrop { position: fixed; inset: 0; z-index: 20; display: grid; place-items: center; padding: 16px; background: #211f1980; }.dialog { width: min(760px, 100%); max-height: min(90vh, 850px); overflow: auto; border: 1px solid #c9c3b3; border-radius: 5px; background: #fff; box-shadow: 0 18px 55px #17150f40; }.dialog > header { padding: 11px 15px; border-bottom: 1px solid #e5e1d6; }.dialog h2 { margin: 2px 0 0; font-size: 16px; }.dialog-intro { margin: 12px 15px; color: #6f6a5e; font-size: 12px; }.form-grid { display: grid; grid-template-columns: repeat(2, minmax(200px, 1fr)); gap: 10px 12px; padding: 4px 15px 12px; }.form-grid label, .lookup-search { display: flex; flex-direction: column; gap: 4px; color: #5e5a4e; font-size: 12px; }.file-field input { padding: 3px; }.file-field small { color: #898578; }.import-preview { margin: 0 15px 12px; border: 1px solid #e3dfd3; border-radius: 4px; overflow: hidden; }.import-preview > header { padding: 7px 9px; background: #f7f5ee; }.import-preview > header span, .import-preview p { color: #777366; font-size: 11px; }.import-preview p { margin: 7px 9px; }.template-actions { padding: 0 15px 12px; color: #777366; font-size: 11px; }.dialog footer { padding: 10px 15px; border-top: 1px solid #e5e1d6; }.lookup-dialog { width: min(520px, 100%); }.lookup-search { margin: 12px 15px; }.lookup-results { max-height: 320px; overflow: auto; padding: 0 15px 12px; }.lookup-results button { display: flex; width: 100%; min-height: 38px; align-items: center; justify-content: space-between; margin-bottom: 5px; text-align: left; }.lookup-results button.active { border-color: #68735a; background: #e9f0e8; }.lookup-results span { color: #777366; }
@media (max-width: 1100px) { .query-panel { grid-template-columns: repeat(3, minmax(150px, 1fr)); }.detail-layout { grid-template-columns: 1fr; } }
@media (max-width: 620px) { .query-panel { grid-template-columns: 1fr; }.query-actions { align-items: flex-start; flex-direction: column; }.form-grid { grid-template-columns: 1fr; }.template-actions { align-items: flex-start; flex-direction: column; }.toolbar { align-items: flex-start; flex-direction: column; } }
</style>
