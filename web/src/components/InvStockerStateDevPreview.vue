<script setup lang="ts">
import { computed, ref } from 'vue'

interface Query { stockerNo: string; areaNo: string; lotNo: string; carrierNo: string; enabled: { stocker: boolean; area: boolean; lot: boolean; carrier: boolean } }
interface StockRow { stockerNo: string; locatorNo: string; lotNo: string; carrierNo: string; areaNo: string; createDate: string; qtyLimit: number; vertical: number; horizontal: number }
interface StockerInfo { stockerNo: string; areaNo: string; vertical: number; horizontal: number; qtyLimit: number }

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: true })
const emit = defineEmits<{ close: []; openManager: [stockerNo: string] }>()
const previewLabel = computed(() => props.developmentPreview ? 'DEV 假資料預覽' : 'DEV 模擬')
const stockers: StockerInfo[] = [
  { stockerNo: 'STK-DEV-01', areaNo: 'AREA-DEV-01', vertical: 4, horizontal: 3, qtyLimit: 2 },
  { stockerNo: 'STK-DEV-02', areaNo: 'AREA-DEV-02', vertical: 3, horizontal: 2, qtyLimit: 1 },
]
const rows: StockRow[] = [
  { stockerNo: 'STK-DEV-01', locatorNo: 'STK-DEV-01-L01', lotNo: 'LOT-DEV-001', carrierNo: '', areaNo: 'AREA-DEV-01', createDate: '2026-11-24 08:20:00', qtyLimit: 2, vertical: 1, horizontal: 1 },
  { stockerNo: 'STK-DEV-01', locatorNo: 'STK-DEV-01-L01', lotNo: 'LOT-DEV-002', carrierNo: '', areaNo: 'AREA-DEV-01', createDate: '2026-11-24 09:10:00', qtyLimit: 2, vertical: 1, horizontal: 1 },
  { stockerNo: 'STK-DEV-01', locatorNo: 'STK-DEV-01-L02', lotNo: '', carrierNo: 'CAR-DEV-01', areaNo: 'AREA-DEV-01', createDate: '2026-11-23 14:35:00', qtyLimit: 2, vertical: 2, horizontal: 1 },
  { stockerNo: 'STK-DEV-01', locatorNo: 'STK-DEV-01-L07', lotNo: 'LOT-DEV-003', carrierNo: '', areaNo: 'AREA-DEV-01', createDate: '2026-11-22 11:00:00', qtyLimit: 2, vertical: 3, horizontal: 2 },
  { stockerNo: 'STK-DEV-02', locatorNo: 'STK-DEV-02-L01', lotNo: '', carrierNo: 'CAR-DEV-03', areaNo: 'AREA-DEV-02', createDate: '2026-11-22 08:15:00', qtyLimit: 1, vertical: 1, horizontal: 1 },
  { stockerNo: 'STK-DEV-02', locatorNo: 'STK-DEV-02-L03', lotNo: 'LOT-DEV-005', carrierNo: '', areaNo: 'AREA-DEV-02', createDate: '2026-11-21 10:30:00', qtyLimit: 1, vertical: 3, horizontal: 1 },
]
const criteria = ref<Query>({ stockerNo: '', areaNo: '', lotNo: '', carrierNo: '', enabled: { stocker: false, area: false, lot: false, carrier: false } })
const applied = ref<Query | null>(null)
const selectedKey = ref('')
const notice = ref('DEV 假資料：勾選查詢條件（可留白查全部），按 Query 載入現況。')
const stockerOptions = computed(() => Array.from(new Set(rows.map((row) => row.stockerNo))).sort())
const areaOptions = computed(() => Array.from(new Set(rows.map((row) => row.areaNo))).sort())
const visibleRows = computed(() => {
  const query = applied.value
  if (!query) return []
  return rows.filter((row) => (!query.enabled.stocker || !query.stockerNo || row.stockerNo === query.stockerNo)
    && (!query.enabled.area || !query.areaNo || row.areaNo === query.areaNo)
    && (!query.enabled.lot || !query.lotNo || row.lotNo.toUpperCase().includes(query.lotNo.toUpperCase()))
    && (!query.enabled.carrier || !query.carrierNo || row.carrierNo === query.carrierNo))
    .slice().sort((a, b) => a.stockerNo.localeCompare(b.stockerNo) || a.locatorNo.localeCompare(b.locatorNo) || a.lotNo.localeCompare(b.lotNo) || a.carrierNo.localeCompare(b.carrierNo))
})
const selectedRow = computed(() => visibleRows.value.find((row) => rowKey(row) === selectedKey.value) ?? null)
const selectedStocker = computed(() => stockers.find((item) => item.stockerNo === selectedRow.value?.stockerNo) ?? null)
const selectedStockerRows = computed(() => selectedRow.value ? rows.filter((row) => row.stockerNo === selectedRow.value?.stockerNo) : [])
const selectedSlots = computed(() => {
  if (!selectedStocker.value) return []
  const info = selectedStocker.value
  const slots: Array<{ locatorNo: string; vertical: number; horizontal: number; contents: StockRow[] }> = []
  for (let horizontal = 1; horizontal <= info.horizontal; horizontal += 1) {
    for (let vertical = 1; vertical <= info.vertical; vertical += 1) {
      const serial = (horizontal - 1) * info.vertical + vertical
      const locatorNo = `${info.stockerNo}-L${String(serial).padStart(2, '0')}`
      const contents = selectedStockerRows.value.filter((row) => row.locatorNo === locatorNo)
      slots.push({ locatorNo, vertical, horizontal, contents })
    }
  }
  return slots
})
const distinctStockers = computed(() => new Set(visibleRows.value.map((row) => row.stockerNo)).size)
const distinctAreas = computed(() => new Set(visibleRows.value.map((row) => row.areaNo)).size)

function rowKey(row: StockRow): string { return [row.stockerNo, row.locatorNo, row.lotNo, row.carrierNo, row.createDate].join('|') }
function selected(row: StockRow): void { selectedKey.value = rowKey(row); notice.value = `已選取 ${row.stockerNo} / ${row.locatorNo}；右側顯示該貨架格位圖。` }
function syncCheckbox(key: 'stocker' | 'area' | 'lot' | 'carrier'): void {
  const field = key === 'stocker' ? 'stockerNo' : key === 'area' ? 'areaNo' : key === 'lot' ? 'lotNo' : 'carrierNo'
  criteria.value.enabled[key] = criteria.value[field].length > 0
}
function clearIfDisabled(key: 'stocker' | 'area' | 'lot' | 'carrier'): void {
  if (criteria.value.enabled[key]) return
  const field = key === 'stocker' ? 'stockerNo' : key === 'area' ? 'areaNo' : key === 'lot' ? 'lotNo' : 'carrierNo'
  criteria.value[field] = ''
}
function runQuery(): void {
  applied.value = { ...criteria.value, enabled: { ...criteria.value.enabled } }
  selectedKey.value = visibleRows.value[0] ? rowKey(visibleRows.value[0]) : ''
  notice.value = `DEV 查詢完成：${visibleRows.value.length} 筆現況，${distinctStockers.value} 部貨架、${distinctAreas.value} 個 Area。`
}
function clearQuery(): void {
  criteria.value = { stockerNo: '', areaNo: '', lotNo: '', carrierNo: '', enabled: { stocker: false, area: false, lot: false, carrier: false } }
  applied.value = null; selectedKey.value = ''; notice.value = '已清除查詢條件與結果。'
}
function openManager(): void {
  if (!selectedRow.value) { notice.value = '請先選取查詢結果中的 StockerNo。'; return }
  emit('openManager', selectedRow.value.stockerNo)
}
</script>

<template>
  <main class="stocker-state" aria-label="在製貨架查詢作業">
    <header class="page-head">
      <div><p class="eyebrow">INV · IV306</p><h1>在製貨架查詢作業</h1><p class="subtitle">Stocker State Query · 貨架、格位、Lot 與 Carrier 現況</p></div>
      <div class="head-actions"><span class="dev-tag">{{ previewLabel }}</span><button type="button" class="close" @click="emit('close')">Close (X)</button></div>
    </header>

    <section class="query-card">
      <div class="query-title"><div><h2>Query</h2><p>條件皆可不勾選；LotNo 為包含比對，其他欄位為精確比對</p></div><span class="source-note">庫格容量 QtyLimit 由 Stocker Basis 提供</span></div>
      <div class="criteria-grid">
        <div class="criterion"><label class="check"><input v-model="criteria.enabled.stocker" type="checkbox" @change="clearIfDisabled('stocker')"><span>Stocker No</span></label><select v-model="criteria.stockerNo" @change="syncCheckbox('stocker')"><option value="">全部貨架</option><option v-for="value in stockerOptions" :key="value" :value="value">{{ value }}</option></select></div>
        <div class="criterion"><label class="check"><input v-model="criteria.enabled.area" type="checkbox" @change="clearIfDisabled('area')"><span>Area No</span></label><select v-model="criteria.areaNo" @change="syncCheckbox('area')"><option value="">全部 Area</option><option v-for="value in areaOptions" :key="value" :value="value">{{ value }}</option></select></div>
        <div class="criterion"><label class="check"><input v-model="criteria.enabled.lot" type="checkbox" @change="clearIfDisabled('lot')"><span>Lot No</span></label><input v-model="criteria.lotNo" placeholder="輸入 LotNo 片段" @input="syncCheckbox('lot')"></div>
        <div class="criterion"><label class="check"><input v-model="criteria.enabled.carrier" type="checkbox" @change="clearIfDisabled('carrier')"><span>Carrier No</span></label><input v-model="criteria.carrierNo" placeholder="輸入 CarrierNo" @input="syncCheckbox('carrier')"></div>
      </div>
      <div class="query-footer"><p class="hint">每列代表一個已上架 ContentNo。按 Query 載入；不勾條件會查全部現況。</p><div class="actions"><button type="button" class="secondary" @click="clearQuery">Clear</button><button type="button" class="primary" @click="runQuery">Query</button></div></div>
      <p class="notice" role="status">{{ notice }}</p>
    </section>

    <div class="result-layout">
      <section class="state-card">
        <div class="section-head"><div><h2>Stocker State</h2><p>{{ applied ? `${visibleRows.length} 筆現況` : '尚未查詢' }}</p></div><button type="button" class="manager-button" :disabled="!selectedRow" @click="openManager">Stocker In/Out</button></div>
        <div class="table-wrap"><table><thead><tr><th>StockerNo</th><th>LocatorNo</th><th>LotNo</th><th>CarrierNo</th><th>INV Stocker Date</th></tr></thead><tbody>
          <tr v-for="row in visibleRows" :key="rowKey(row)" :class="{ selected: rowKey(row) === selectedKey }" @click="selected(row)"><td class="code">{{ row.stockerNo }}</td><td class="code">{{ row.locatorNo }}</td><td class="code">{{ row.lotNo || '—' }}</td><td class="code">{{ row.carrierNo || '—' }}</td><td>{{ row.createDate }}</td></tr>
          <tr v-if="!visibleRows.length"><td colspan="5" class="empty">{{ applied ? '沒有符合的 Stocker State' : '按 Query 後顯示資料' }}</td></tr>
        </tbody></table></div>
        <p class="hint">Stocker In/Out 會開啟 IV305；此頁只查現況。</p>
      </section>

      <section class="rack-card">
        <div class="section-head"><div><h2>Stocker Locator Map</h2><p v-if="selectedStocker">{{ selectedStocker.stockerNo }} · {{ selectedStocker.areaNo }} · {{ selectedStocker.vertical }} × {{ selectedStocker.horizontal }}</p><p v-else>選取左側列後載入所屬貨架及容量狀態</p></div><span v-if="selectedStocker" class="read-only">唯讀</span></div>
        <div v-if="selectedStocker" class="rack-grid" :style="{ '--rack-columns': selectedStocker.vertical }">
          <div v-for="slot in selectedSlots" :key="slot.locatorNo" class="slot" :class="{ full: slot.contents.length >= selectedStocker.qtyLimit, chosen: slot.locatorNo === selectedRow?.locatorNo }"><span>{{ slot.locatorNo }}</span><strong>{{ slot.contents.length }} / {{ selectedStocker.qtyLimit }}</strong><small>{{ slot.contents.map((content) => content.lotNo || content.carrierNo).join(', ') || 'EMPTY' }}</small></div>
        </div>
        <div v-else class="rack-placeholder">目前沒有選取貨架。</div>
        <div class="legend"><span><i class="occupied-dot"></i>已上架</span><span><i class="full-dot"></i>達到 QtyLimit</span><span><i class="chosen-dot"></i>目前列</span></div>
        <p class="map-note">格位圖依 Stocker Detail 座標產生；舊畫面格位按鈕不綁 click event，因此只做容量／位置標示。</p>
      </section>
    </div>
    <footer class="page-footer"><span>DEV 假資料只在本頁記憶體，不呼叫正式 API 或資料庫。</span><span>IV306 · 來源表 `TBLINVSTOCKERSTATE` + Stocker Basis</span></footer>
  </main>
</template>

<style scoped>
.stocker-state { height: 100%; min-height: 0; overflow: auto; box-sizing: border-box; padding: 18px 22px 22px; background: #f1f2ef; color: #2f352f; font: 13px/1.45 "Segoe UI", "Microsoft JhengHei", sans-serif; }
.page-head,.head-actions,.query-title,.query-footer,.actions,.section-head,.page-footer { display: flex; align-items: center; justify-content: space-between; gap: 13px; }
.page-head { max-width: 1440px; margin: 0 auto 15px; }
.page-head h1 { margin: 0; color: #29372c; font-size: 22px; line-height: 1.25; }
.eyebrow { margin: 0 0 3px; color: #587e63; font-size: 11px; font-weight: 700; letter-spacing: .12em; }
.subtitle,.query-title p,.section-head p { margin: 3px 0 0; color: #767c73; font-size: 11px; }
.head-actions { flex-shrink: 0; }
.dev-tag,.read-only { display: inline-block; border: 1px solid #c0d5c4; border-radius: 999px; padding: 4px 9px; color: #3c6948; background: #edf5ed; font-size: 10px; font-weight: 700; }
.close,.secondary,.primary,.manager-button { min-height: 32px; border: 1px solid #cdd3cc; border-radius: 5px; padding: 5px 12px; background: white; color: #3d483f; font: inherit; cursor: pointer; }
.primary { border-color: #477c55; background: #477c55; color: white; font-weight: 700; }
.manager-button { border-color: #b8cfba; color: #3e7049; background: #f0f7ef; font-weight: 700; }
.manager-button:disabled { cursor: not-allowed; color: #9da49b; background: #f1f2ef; }
.query-card,.state-card,.rack-card { max-width: 1440px; margin: 0 auto 14px; border: 1px solid #d9ded7; border-radius: 8px; background: white; box-shadow: 0 2px 9px #252e260a; }
.query-card { padding: 14px 16px 11px; }
.query-title { margin-bottom: 12px; }
.query-title h2,.section-head h2 { margin: 0; color: #344538; font-size: 14px; }
.source-note,.hint { color: #7b8178; font-size: 10px; }
.criteria-grid { display: grid; grid-template-columns: repeat(4,minmax(180px,1fr)); gap: 11px; }
.criterion { display: grid; gap: 5px; min-width: 0; }
.check { display: flex; align-items: center; gap: 6px; color: #596257; font-size: 11px; font-weight: 650; }
.check input { accent-color: #4e845b; }
.criterion input:not([type="checkbox"]),.criterion select { width: 100%; min-height: 33px; box-sizing: border-box; border: 1px solid #d1d8d0; border-radius: 4px; padding: 5px 8px; background: white; color: #303830; font: inherit; font-size: 12px; }
.query-footer { margin-top: 12px; border-top: 1px solid #edf0eb; padding-top: 9px; }
.query-footer .hint { margin: 0; }
.actions { justify-content: flex-end; }
.notice { margin: 9px 0 0; border-left: 3px solid #77a47d; padding: 5px 9px; color: #5f6d60; background: #f4f8f3; font-size: 10px; }
.result-layout { display: grid; grid-template-columns: minmax(500px,1.1fr) minmax(340px,.9fr); gap: 14px; max-width: 1440px; margin: 0 auto 14px; }
.result-layout > section { min-width: 0; margin-bottom: 0; overflow: hidden; }
.section-head { padding: 12px 14px 10px; border-bottom: 1px solid #e8ece7; }
.table-wrap { overflow: auto; }
table { width: 100%; border-collapse: collapse; white-space: nowrap; font-size: 10px; }
th,td { border-bottom: 1px solid #e9ede8; padding: 8px 9px; text-align: left; }
th { background: #f4f7f3; color: #596759; font-weight: 700; }
tbody tr { cursor: pointer; }
tbody tr:hover { background: #f8faf7; }
tbody tr.selected { background: #eaf3e9; }
.code { color: #45684d; font-family: Consolas, "Courier New", monospace; }
.empty { height: 70px; color: #899087; text-align: center; vertical-align: middle; }
.state-card > .hint { margin: 9px 13px 12px; }
.rack-grid { display: grid; grid-template-columns: repeat(var(--rack-columns),minmax(62px,1fr)); gap: 8px; align-content: start; min-height: 210px; max-height: 340px; overflow: auto; padding: 13px; }
.slot { display: grid; gap: 2px; min-height: 69px; align-content: center; border: 1px solid #b8cfbd; border-radius: 5px; padding: 4px; background: #eaf4eb; color: #416c48; text-align: center; }
.slot.full { border-color: #d88f88; background: #fff0ed; color: #9a4d44; }
.slot.chosen { outline: 2px solid #5f8d68; outline-offset: 1px; }
.slot span { font-family: Consolas, "Courier New", monospace; font-size: 9px; }
.slot strong { font-size: 12px; }
.slot small { overflow: hidden; text-overflow: ellipsis; font-size: 8px; }
.rack-placeholder { display: grid; min-height: 220px; place-items: center; color: #8c938a; }
.legend { display: flex; gap: 12px; padding: 0 13px 8px; color: #788076; font-size: 9px; }
.legend span { display: inline-flex; align-items: center; gap: 4px; }
.legend i { width: 9px; height: 9px; border: 1px solid #aec7b3; border-radius: 3px; background: #eaf4eb; }
.legend .full-dot { border-color: #d88f88; background: #fff0ed; }
.legend .chosen-dot { outline: 1px solid #5f8d68; outline-offset: 1px; background: white; }
.map-note { margin: 0; border-top: 1px solid #edf0eb; padding: 8px 13px; color: #7e847a; font-size: 9px; }
.page-footer { max-width: 1440px; margin: 0 auto; color: #81877e; font-size: 9px; }
@media (max-width: 1080px) { .criteria-grid { grid-template-columns: repeat(2,minmax(180px,1fr)); } .result-layout { grid-template-columns: 1fr; } }
@media (max-width: 680px) { .stocker-state { padding: 12px; } .page-head { align-items: flex-start; } .head-actions { align-items: flex-end; flex-direction: column; gap: 6px; } .criteria-grid { grid-template-columns: 1fr; } .query-title,.query-footer,.section-head,.page-footer { align-items: flex-start; flex-direction: column; } .actions { width: 100%; } .actions button { flex: 1; } .rack-grid { grid-template-columns: repeat(3,minmax(62px,1fr)); } }
</style>
