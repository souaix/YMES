<script setup lang="ts">
import { computed, ref } from 'vue'

type GumStatus = 0 | 1 | 2
interface GumRule { materialType: string; materialNo: string; frozenMin: number | null; refrozenFlag: boolean; refrozenTimes: number | null }
interface RawLot {
  inventoryNo: string; materialNo: string; materialLotNo: string; locatorNo: string; unitNo: string; qty: number
  materialType: string; gumStatus: GumStatus; acclimationDate: string; frozenDate: string; executor: string
  acclimationTimes: number; frozenTimes: number; manufactureDate: string; expireDate: string; inputDate: string
}
interface EquipmentLot { materialNo: string; materialLotNo: string; qty: number }
interface Query { inventoryNo: string; materialNo: string; lotPrefix: string; gumStatus: string }
type Action = 'warm' | 'cool'

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: true })
const emit = defineEmits<{ close: [] }>()
const previewLabel = computed(() => props.developmentPreview ? 'DEV 假資料預覽' : 'DEV 模擬')
const multiTimeControl = true
const permittedMaterialTypes = ['GUM-DEV', 'ADH-DEV']
const approvedMaterialTypes = ['GUM-DEV', 'ADH-DEV']
const inventories = [
  { inventoryNo: 'RAW-DEV-01', inventoryName: '原料冷藏庫 A' },
  { inventoryNo: 'RAW-DEV-02', inventoryName: '原料冷藏庫 B' },
]
const materials = [
  { materialNo: 'MAT-GUM-001', materialName: '導熱膠 A', materialType: 'GUM-DEV' },
  { materialNo: 'MAT-GUM-002', materialName: '黏著劑 B', materialType: 'ADH-DEV' },
  { materialNo: 'MAT-GUM-003', materialName: '封裝膠 C', materialType: 'GUM-DEV' },
]
const rules: GumRule[] = [
  { materialType: 'GUM-DEV', materialNo: '*', frozenMin: 8, refrozenFlag: true, refrozenTimes: 3 },
  { materialType: 'GUM-DEV', materialNo: 'MAT-GUM-003', frozenMin: 12, refrozenFlag: false, refrozenTimes: 0 },
  { materialType: 'ADH-DEV', materialNo: '*', frozenMin: 6, refrozenFlag: true, refrozenTimes: 2 },
]
const equipmentLots: EquipmentLot[] = [{ materialNo: 'MAT-GUM-001', materialLotNo: 'GUM-LOT-300', qty: 2 }]
const initialLots: RawLot[] = [
  { inventoryNo: 'RAW-DEV-01', materialNo: 'MAT-GUM-001', materialLotNo: 'GUM-LOT-101', locatorNo: 'R-A-01', unitNo: 'KG', qty: 3.25, materialType: 'GUM-DEV', gumStatus: 0, acclimationDate: '', frozenDate: '', executor: '', acclimationTimes: 0, frozenTimes: 0, manufactureDate: '2026-10-01', expireDate: '2027-10-01', inputDate: '2026-10-04' },
  { inventoryNo: 'RAW-DEV-01', materialNo: 'MAT-GUM-001', materialLotNo: 'GUM-LOT-102', locatorNo: 'R-A-02', unitNo: 'KG', qty: 1.5, materialType: 'GUM-DEV', gumStatus: 2, acclimationDate: '', frozenDate: '2026-11-24 08:00:00', executor: 'DEV02', acclimationTimes: 1, frozenTimes: 1, manufactureDate: '2026-10-04', expireDate: '2027-10-04', inputDate: '2026-10-06' },
  { inventoryNo: 'RAW-DEV-01', materialNo: 'MAT-GUM-001', materialLotNo: 'GUM-LOT-300', locatorNo: 'R-A-03', unitNo: 'KG', qty: 2, materialType: 'GUM-DEV', gumStatus: 1, acclimationDate: '2026-11-24 15:20:00', frozenDate: '', executor: 'DEV01', acclimationTimes: 1, frozenTimes: 0, manufactureDate: '2026-10-10', expireDate: '2027-10-10', inputDate: '2026-10-12' },
  { inventoryNo: 'RAW-DEV-02', materialNo: 'MAT-GUM-001', materialLotNo: 'GUM-LOT-300', locatorNo: 'R-B-07', unitNo: 'KG', qty: 1, materialType: 'GUM-DEV', gumStatus: 1, acclimationDate: '2026-11-24 15:20:00', frozenDate: '', executor: 'DEV01', acclimationTimes: 1, frozenTimes: 0, manufactureDate: '2026-10-10', expireDate: '2027-10-10', inputDate: '2026-10-12' },
  { inventoryNo: 'RAW-DEV-01', materialNo: 'MAT-GUM-002', materialLotNo: 'ADH-LOT-210', locatorNo: 'R-A-04', unitNo: 'EA', qty: 12, materialType: 'ADH-DEV', gumStatus: 0, acclimationDate: '', frozenDate: '', executor: '', acclimationTimes: 0, frozenTimes: 0, manufactureDate: '2026-09-18', expireDate: '2027-03-18', inputDate: '2026-09-22' },
  { inventoryNo: 'RAW-DEV-02', materialNo: 'MAT-GUM-002', materialLotNo: 'ADH-LOT-211', locatorNo: 'R-B-02', unitNo: 'EA', qty: 8, materialType: 'ADH-DEV', gumStatus: 2, acclimationDate: '', frozenDate: '2026-11-22 10:00:00', executor: 'DEV03', acclimationTimes: 2, frozenTimes: 2, manufactureDate: '2026-09-21', expireDate: '2027-03-21', inputDate: '2026-09-25' },
  { inventoryNo: 'RAW-DEV-01', materialNo: 'MAT-GUM-003', materialLotNo: 'GUM-LOT-900', locatorNo: 'R-A-08', unitNo: 'KG', qty: 0.75, materialType: 'GUM-DEV', gumStatus: 0, acclimationDate: '', frozenDate: '', executor: '', acclimationTimes: 0, frozenTimes: 0, manufactureDate: '2026-09-10', expireDate: '2026-12-10', inputDate: '2026-09-14' },
  { inventoryNo: 'RAW-DEV-02', materialNo: 'MAT-GUM-001', materialLotNo: 'GUM-LOT-EXPIRED', locatorNo: 'R-B-99', unitNo: 'KG', qty: 2, materialType: 'GUM-DEV', gumStatus: 0, acclimationDate: '', frozenDate: '', executor: '', acclimationTimes: 0, frozenTimes: 0, manufactureDate: '2025-08-01', expireDate: '2026-10-01', inputDate: '2025-08-05' },
  { inventoryNo: 'RAW-DEV-02', materialNo: 'MAT-OTHER-001', materialLotNo: 'OTHER-LOT-01', locatorNo: 'R-B-10', unitNo: 'KG', qty: 7, materialType: 'OTHER-DEV', gumStatus: 0, acclimationDate: '', frozenDate: '', executor: '', acclimationTimes: 0, frozenTimes: 0, manufactureDate: '2026-10-01', expireDate: '', inputDate: '2026-10-06' },
]
const rows = ref<RawLot[]>(initialLots.map((row) => ({ ...row })))
const query = ref<Query>({ inventoryNo: '', materialNo: '', lotPrefix: '', gumStatus: '' })
const appliedQuery = ref<Query | null>(null)
const selectedKeys = ref<string[]>([])
const notice = ref('DEV 假資料：操作只更新此頁的本機狀態，不連 MES 或正式 API。設定條件後按 Query。')
const lookupOpen = ref(false)
const lookupText = ref('')
const lookupSelection = ref('')
const lookupMaterials = computed(() => materials.filter((row) => permittedMaterialTypes.includes(row.materialType) && (!lookupText.value || row.materialNo.toLowerCase().includes(lookupText.value.toLowerCase()) || row.materialName.includes(lookupText.value))))
const visibleRows = computed(() => appliedQuery.value ? applyQuery(appliedQuery.value) : [])
const allVisibleSelected = computed(() => visibleRows.value.length > 0 && visibleRows.value.every((row) => selectedKeys.value.includes(rowKey(row))))
const statusCounts = computed(() => ({ ready: visibleRows.value.filter((row) => row.gumStatus === 0).length, warm: visibleRows.value.filter((row) => row.gumStatus === 1).length, frozen: visibleRows.value.filter((row) => row.gumStatus === 2).length }))

function rowKey(row: RawLot): string { return `${row.inventoryNo}|${row.materialNo}|${row.materialLotNo}|${row.locatorNo}` }
function ruleFor(row: RawLot): GumRule | undefined { return rules.find((rule) => rule.materialType === row.materialType && rule.materialNo === row.materialNo) ?? rules.find((rule) => rule.materialType === row.materialType && rule.materialNo === '*') }
function dateText(value: string): string { return value || '—' }
function statusLabel(value: GumStatus): string { return value === 0 ? '待回溫' : value === 1 ? '已回溫' : '已回冰' }
function applyQuery(criteria: Query): RawLot[] {
  const now = Date.now()
  const visible = rows.value.filter((row) => permittedMaterialTypes.includes(row.materialType) && row.qty > 0 && (!row.expireDate || new Date(`${row.expireDate}T00:00:00`).getTime() > now))
  const filtered = visible.filter((row) => (!criteria.inventoryNo || row.inventoryNo === criteria.inventoryNo)
    && (!criteria.materialNo || row.materialNo === criteria.materialNo)
    && (!criteria.lotPrefix || row.materialLotNo.startsWith(criteria.lotPrefix.trim()))
    && (criteria.gumStatus === '' || String(row.gumStatus) === criteria.gumStatus))
  const seq = new Map<string, number>()
  const byMaterial = new Map<string, RawLot[]>()
  filtered.filter((row) => !row.acclimationDate).forEach((row) => byMaterial.set(row.materialNo, [...(byMaterial.get(row.materialNo) ?? []), row]))
  for (const [materialNo, lots] of byMaterial) {
    const distinct = [...new Set(lots.map((row) => `${row.manufactureDate}|${row.expireDate || '9999-12-31'}|${row.inputDate || '9999-12-31'}`))].sort()
    distinct.forEach((tuple, index) => seq.set(`${materialNo}|${tuple}`, index + 1))
  }
  return filtered.map((row) => ({ ...row, fifoSequence: row.acclimationDate ? null : seq.get(`${row.materialNo}|${row.manufactureDate}|${row.expireDate || '9999-12-31'}|${row.inputDate || '9999-12-31'}`) ?? null } as RawLot & { fifoSequence: number | null }))
    .sort((a, b) => a.materialNo.localeCompare(b.materialNo) || (a.fifoSequence ?? 9999) - (b.fifoSequence ?? 9999) || a.materialLotNo.localeCompare(b.materialLotNo) || a.inventoryNo.localeCompare(b.inventoryNo))
}
function runQuery(): void {
  appliedQuery.value = { ...query.value }
  selectedKeys.value = []
  notice.value = `DEV 假資料查詢完成：${visibleRows.value.length} 筆；已排除過期批與非膠類物料。`
}
function toggleRow(row: RawLot): void {
  const key = rowKey(row)
  selectedKeys.value = selectedKeys.value.includes(key) ? selectedKeys.value.filter((item) => item !== key) : [...selectedKeys.value, key]
}
function toggleAll(): void { selectedKeys.value = allVisibleSelected.value ? [] : visibleRows.value.map(rowKey) }
function openLookup(): void { lookupSelection.value = query.value.materialNo; lookupText.value = ''; lookupOpen.value = true }
function applyLookup(): void { if (lookupSelection.value) query.value.materialNo = lookupSelection.value; lookupOpen.value = false }
function clearQuery(): void {
  query.value = { inventoryNo: '', materialNo: '', lotPrefix: '', gumStatus: '' }
  appliedQuery.value = null
  selectedKeys.value = []
  notice.value = 'DEV 假資料：操作只更新此頁的本機狀態，不連 MES 或正式 API。設定條件後按 Query。'
}
function runAction(action: Action): void {
  if (!selectedKeys.value.length) { notice.value = '請先勾選一筆以上的批號。'; return }
  const now = new Date()
  const nowText = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')} ${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}:${String(now.getSeconds()).padStart(2, '0')}`
  const reasons: string[] = []
  let succeeded = 0
  for (const selected of selectedKeys.value) {
    const row = rows.value.find((candidate) => rowKey(candidate) === selected)
    if (!row) continue
    const rule = ruleFor(row)
    let reason = ''
    if (!approvedMaterialTypes.includes(row.materialType)) reason = '物料類別未核准'
    else if (action === 'warm') {
      if (row.gumStatus === 1) reason = '此批已回溫'
      else if (row.frozenDate && rule?.frozenMin !== null && rule?.frozenMin !== undefined && Date.now() < new Date(row.frozenDate.replace(' ', 'T')).getTime() + rule.frozenMin * 3600000) reason = `未達回冰最短時間 ${rule.frozenMin} 小時`
    } else if (equipmentLots.some((item) => item.materialNo === row.materialNo && item.materialLotNo === row.materialLotNo && item.qty > 0)) reason = '設備尚有上料數量'
    else if (row.gumStatus === 0) reason = '此批尚未回溫'
    else if (row.gumStatus === 2) reason = '此批已回冰'
    else if (rule && !rule.refrozenFlag) reason = '物料設定不允許回冰'
    else if (rule?.refrozenTimes !== null && rule?.refrozenTimes !== undefined && row.frozenTimes >= rule.refrozenTimes) reason = '已達回冰次數上限'
    if (reason) { reasons.push(`${row.materialLotNo} @ ${row.locatorNo}：${reason}`); continue }
    if (action === 'warm') {
      row.acclimationDate = nowText; row.executor = 'DEVUSER'; row.gumStatus = 1; row.acclimationTimes += 1; row.frozenDate = ''
    } else {
      row.frozenDate = nowText; row.executor = 'DEVUSER'; row.gumStatus = 2; row.acclimationDate = ''; row.frozenTimes += 1
    }
    succeeded += 1
  }
  selectedKeys.value = []
  if (appliedQuery.value) appliedQuery.value = { ...appliedQuery.value }
  const actionLabel = action === 'warm' ? '回溫' : '回冰'
  notice.value = `${actionLabel}模擬完成：成功 ${succeeded} 筆，略過 ${reasons.length} 筆。${reasons.slice(0, 3).join('；')}${reasons.length > 3 ? '；…' : ''}`
}
</script>

<template>
  <main class="acclimation-page" aria-label="回溫回冰作業 DEV 預覽">
    <header class="page-head"><div><p class="eyebrow">INV / IV212 · {{ previewLabel }}</p><h1>回溫回冰作業(膠)</h1><p class="sub">膠類原料批狀態維護 · {{ multiTimeControl ? '多次回冰控制已啟用' : '單次回溫模式' }}</p></div><button type="button" class="close" title="Close (X)" @click="emit('close')">×</button></header>
    <p class="notice" role="status">{{ notice }}</p>
    <section class="query-panel" aria-label="原料批查詢條件">
      <div class="query-grid">
        <label>Inventory No<select v-model="query.inventoryNo"><option value="">全部膠類原料庫房</option><option v-for="row in inventories" :key="row.inventoryNo" :value="row.inventoryNo">{{ row.inventoryNo }} · {{ row.inventoryName }}</option></select></label>
        <label>Material No<div class="lookup-field"><input v-model.trim="query.materialNo" list="gum-material-options" autocomplete="off" placeholder="輸入物料編號" /><datalist id="gum-material-options"><option v-for="item in materials.filter((row) => permittedMaterialTypes.includes(row.materialType))" :key="item.materialNo" :value="item.materialNo">{{ item.materialName }}</option></datalist><button type="button" @click="openLookup">Lookup</button></div></label>
        <label>Material Lot No<input v-model.trim="query.lotPrefix" placeholder="批號前綴" /></label>
        <label>Gum Type Status<select v-model="query.gumStatus"><option value="">全部狀態</option><option value="0">待回溫 (0)</option><option value="1">已回溫 (1)</option><option v-if="multiTimeControl" value="2">已回冰 (2)</option></select></label>
      </div>
      <footer class="query-actions"><span>僅列未過期、數量大於 0 且 MaterialType 屬於 GumMaterialType 的庫存。</span><div><button type="button" @click="clearQuery">Clear</button><button type="button" class="primary" @click="runQuery">Query</button></div></footer>
    </section>
    <section class="status-strip"><span>查詢結果 <strong>{{ visibleRows.length }}</strong></span><span>待回溫 <strong>{{ statusCounts.ready }}</strong></span><span>已回溫 <strong>{{ statusCounts.warm }}</strong></span><span v-if="multiTimeControl">已回冰 <strong>{{ statusCounts.frozen }}</strong></span><span class="hint">多儲位更新以 DEV 複合鍵精確定位</span></section>
    <section class="panel inventory-panel" aria-label="膠類原料批庫存">
      <header class="panel-title"><div><h2>WIP Inventory · Raw</h2><span>{{ appliedQuery ? '依料號、批號排序；同料號依製造／效期／入庫日期標示 FIFO 順序' : '設定條件後按 Query 載入' }}</span></div><small>{{ selectedKeys.length }} 筆已勾選</small></header>
      <div class="table-wrap"><table><thead><tr><th><input aria-label="全選查詢結果" type="checkbox" :checked="allVisibleSelected" :disabled="!visibleRows.length" @change="toggleAll" /></th><th>InventoryNo</th><th>MaterialNo</th><th>MaterialLotNo</th><th>LocatorNo</th><th>UnitNo</th><th>Qty</th><th>MaterialType</th><th>GumTypeStatus</th><th>AcclimationDate</th><th v-if="multiTimeControl">FrozenDate</th><th>AccTimes</th><th v-if="multiTimeControl">FrozenTimes</th><th>Executor</th></tr></thead>
        <tbody><tr v-for="row in visibleRows" :key="rowKey(row)" :class="[{ selected: selectedKeys.includes(rowKey(row)) }, `gum-${row.gumStatus}`]" @click="toggleRow(row)"><td><input aria-label="選取批號" type="checkbox" :checked="selectedKeys.includes(rowKey(row))" @click.stop @change="toggleRow(row)" /></td><td>{{ row.inventoryNo }}</td><td class="mono">{{ row.materialNo }}</td><td class="mono">{{ row.materialLotNo }}</td><td>{{ row.locatorNo }}</td><td>{{ row.unitNo }}</td><td class="numeric">{{ row.qty.toLocaleString('zh-TW', { maximumFractionDigits: 6 }) }}</td><td>{{ row.materialType }}</td><td><span class="status-pill" :class="`status-${row.gumStatus}`">{{ statusLabel(row.gumStatus) }}</span></td><td>{{ dateText(row.acclimationDate) }}</td><td v-if="multiTimeControl">{{ dateText(row.frozenDate) }}</td><td class="numeric">{{ row.acclimationTimes }}</td><td v-if="multiTimeControl" class="numeric">{{ row.frozenTimes }}</td><td>{{ dateText(row.executor) }}</td></tr>
          <tr v-if="!appliedQuery"><td :colspan="multiTimeControl ? 14 : 12" class="empty">設定條件後按 Query；未查詢時不載入庫存清單。</td></tr><tr v-else-if="!visibleRows.length"><td :colspan="multiTimeControl ? 14 : 12" class="empty">沒有符合條件的膠類原料批。</td></tr>
        </tbody>
      </table></div>
      <footer class="action-bar"><span>回溫：核准類別、未回溫、冷凍最短時數；回冰：設備無剩餘用量、已回溫、未超回冰限制。</span><div><button type="button" class="cool" :disabled="!multiTimeControl || !selectedKeys.length" @click="runAction('cool')">Cool Down · {{ selectedKeys.length }}</button><button type="button" class="warm" :disabled="!selectedKeys.length" @click="runAction('warm')">Warm Up · {{ selectedKeys.length }}</button></div></footer>
    </section>
    <aside class="source-note"><strong>DEV 模擬範圍</strong><span>狀態和計數僅在頁面記憶體更新。正式操作會寫入原料庫存及事件歷程；本頁不呼叫 API。</span></aside>

    <div v-if="lookupOpen" class="modal-backdrop" role="presentation" @click.self="lookupOpen = false"><section class="lookup-dialog" role="dialog" aria-modal="true" aria-label="查詢膠類物料"><header><h2>Material Lookup</h2><button type="button" @click="lookupOpen = false">×</button></header><label>Material No / Name<input v-model.trim="lookupText" autofocus placeholder="搜尋物料編號或名稱" /></label><div class="lookup-results"><button v-for="item in lookupMaterials" :key="item.materialNo" type="button" :class="{ active: lookupSelection === item.materialNo }" @click="lookupSelection = item.materialNo"><strong>{{ item.materialNo }}</strong><span>{{ item.materialName }} · {{ item.materialType }}</span></button><p v-if="!lookupMaterials.length" class="empty">沒有符合的膠類物料。</p></div><footer><button type="button" @click="lookupOpen = false">Cancel</button><button type="button" class="primary" :disabled="!lookupSelection" @click="applyLookup">Select</button></footer></section></div>
  </main>
</template>

<style scoped>
.acclimation-page { height: 100%; min-height: 0; display: flex; flex-direction: column; gap: 10px; padding: 14px; box-sizing: border-box; overflow: auto; background: #f3f1e9; color: #26251f; font: inherit; }
.page-head, .panel-title, .query-actions, .query-actions > div, .status-strip, .action-bar, .action-bar > div, .source-note, .lookup-dialog header, .lookup-dialog footer { display: flex; align-items: center; gap: 9px; }.page-head, .panel-title, .query-actions, .action-bar, .lookup-dialog header, .lookup-dialog footer { justify-content: space-between; }.page-head h1, .panel-title h2, .lookup-dialog h2 { margin: 0; font-weight: 600; }.page-head h1 { font-size: 20px; }.eyebrow { margin: 0 0 3px; color: #77715f; font-size: 10px; letter-spacing: .12em; }.sub, .panel-title span { margin: 4px 0 0; color: #6e6b60; font-size: 12px; }.close, .lookup-dialog header button { border: 0; background: transparent; color: #625f54; font-size: 22px; cursor: pointer; }.notice { min-height: 16px; margin: 0; color: #365b43; font-size: 12px; }.query-panel, .panel { border: 1px solid #cecabd; background: #fbfaf6; }.query-panel { padding: 9px 10px; }.query-grid { display: grid; grid-template-columns: repeat(4, minmax(160px, 1fr)); gap: 8px 10px; }.query-grid > label, .lookup-dialog > label { display: flex; flex-direction: column; gap: 4px; color: #555246; font-size: 11px; }.query-grid input, .query-grid select, .lookup-dialog input { min-height: 28px; min-width: 0; padding: 3px 7px; border: 1px solid #c7c3b7; border-radius: 3px; background: #fffefa; color: #26251f; font: inherit; }.lookup-field { display: flex; gap: 4px; }.lookup-field input { flex: 1; }.lookup-field button, .query-actions button, .lookup-dialog footer button { min-height: 28px; padding: 4px 9px; border: 1px solid #aaa698; border-radius: 3px; background: #fffefa; color: #343229; font: inherit; cursor: pointer; }.query-actions { margin-top: 8px; padding-top: 8px; border-top: 1px solid #e0ddd3; color: #716d60; font-size: 11px; }.query-actions .primary, .lookup-dialog footer .primary { border-color: #35342d; background: #35342d; color: white; }.query-actions > div { flex: 0 0 auto; }.status-strip { min-height: 31px; flex-wrap: wrap; padding: 0 3px; color: #5a574d; font-size: 11px; }.status-strip strong { margin-left: 3px; color: #292820; }.status-strip .hint { margin-left: auto; color: #777366; }.inventory-panel { flex: 1 1 auto; min-height: 300px; }.panel-title { min-height: 38px; padding: 5px 9px; border-bottom: 1px solid #dedbce; }.panel-title h2, .lookup-dialog h2 { font-size: 14px; }.panel-title small { color: #706d62; }.table-wrap { max-width: 100%; overflow: auto; }table { width: 100%; border-collapse: collapse; white-space: nowrap; font-size: 11px; }th, td { padding: 6px 8px; border: 1px solid #e1ded3; text-align: left; }th { position: sticky; top: 0; z-index: 1; background: #ece9df; color: #49463c; font-weight: 600; }tbody tr { cursor: pointer; }tbody tr:hover { background: #f5f1e2; }tbody tr.selected { background: #e8efdF; }td.numeric { text-align: right; font-variant-numeric: tabular-nums; }.mono { font-family: Consolas, 'Courier New', monospace; }.status-pill { display: inline-block; padding: 2px 6px; border-radius: 9px; font-size: 10px; }.status-0 { background: #ece7d5; color: #5d563e; }.status-1 { background: #dce9dd; color: #355b3b; }.status-2 { background: #dce7f0; color: #35516b; }.empty { padding: 18px; text-align: center; color: #858174; }.action-bar { padding: 8px 9px; border-top: 1px solid #dedbce; color: #706d62; font-size: 11px; }.action-bar > div { flex: 0 0 auto; }.action-bar button { min-height: 30px; padding: 4px 12px; border: 1px solid #555247; border-radius: 3px; font: inherit; font-weight: 600; cursor: pointer; }.action-bar button:disabled { opacity: .45; cursor: not-allowed; }.action-bar .warm { background: #7f5e31; color: #fffaf0; }.action-bar .cool { background: #4d6b7c; color: white; }.source-note { align-items: baseline; color: #777366; font-size: 10px; }.source-note strong { color: #5b584e; }.modal-backdrop { position: fixed; inset: 0; z-index: 20; display: grid; place-items: center; padding: 16px; background: #17171380; }.lookup-dialog { width: min(520px, 100%); max-height: 80vh; display: flex; flex-direction: column; gap: 12px; padding: 14px; border: 1px solid #bcb8aa; border-radius: 5px; background: #fbfaf6; box-shadow: 0 12px 40px #17171340; }.lookup-dialog header { padding-bottom: 7px; border-bottom: 1px solid #dedbce; }.lookup-dialog > label { display: flex; flex-direction: column; gap: 5px; }.lookup-results { min-height: 100px; max-height: 42vh; overflow: auto; border: 1px solid #dedbce; }.lookup-results button { width: 100%; display: flex; justify-content: space-between; gap: 10px; padding: 8px; border: 0; border-bottom: 1px solid #e5e1d7; background: transparent; text-align: left; color: #26251f; font: inherit; cursor: pointer; }.lookup-results button.active { background: #e8efdF; }.lookup-results button span { color: #777366; }.lookup-dialog footer button:disabled { opacity: .5; cursor: not-allowed; }
@media (max-width: 900px) { .query-grid { grid-template-columns: repeat(2, minmax(140px, 1fr)); }.status-strip .hint { margin-left: 0; }.action-bar { align-items: flex-start; flex-direction: column; } }
@media (max-width: 560px) { .acclimation-page { padding: 9px; }.query-grid { grid-template-columns: 1fr; }.query-actions { align-items: flex-start; flex-direction: column; }.query-actions > div { align-self: flex-end; }.lookup-results button { flex-direction: column; gap: 2px; } }
</style>
