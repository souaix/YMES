<script setup lang="ts">
import { computed, ref } from 'vue'

interface Criteria { stockerNo: string; lotNo: string; carrierNo: string; useTime: boolean; start: string; end: string; enabled: { stocker: boolean; lot: boolean; carrier: boolean } }
interface HistoryRow { stockerNo: string; desStockerNo: string; srcLocatorNo: string; desLocatorNo: string; contentNo: string; contentQty: number; opNo: string; areaNo: string; status: 'IN' | 'OUT'; creator: string; createDate: string; lotNo: string; carrierNo: string }

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: true })
const emit = defineEmits<{ close: [] }>()
const previewLabel = computed(() => props.developmentPreview ? 'DEV 假資料預覽' : 'DEV 模擬')
const stockerOptions = ['STK-DEV-01', 'STK-DEV-02', 'STK-DEV-EMPTY']
const rows: HistoryRow[] = [
  { stockerNo: 'STK-DEV-01', desStockerNo: 'STK-DEV-01', srcLocatorNo: '', desLocatorNo: 'STK-DEV-01-L01', contentNo: 'LOT-DEV-001', contentQty: 1, opNo: 'OP-ETCH-02', areaNo: 'AREA-DEV-01', status: 'IN', creator: 'DEV01', createDate: '2026-11-24T08:20:00', lotNo: 'LOT-DEV-001', carrierNo: '' },
  { stockerNo: 'STK-DEV-01', desStockerNo: '', srcLocatorNo: 'STK-DEV-01-L01', desLocatorNo: '', contentNo: 'LOT-DEV-001', contentQty: 0, opNo: 'OP-ETCH-02', areaNo: 'AREA-DEV-01', status: 'OUT', creator: 'DEV02', createDate: '2026-11-24T10:05:00', lotNo: 'LOT-DEV-001', carrierNo: '' },
  { stockerNo: 'STK-DEV-01', desStockerNo: 'STK-DEV-01', srcLocatorNo: '', desLocatorNo: 'STK-DEV-01-L02', contentNo: 'CAR-DEV-01', contentQty: 1, opNo: '', areaNo: 'AREA-DEV-01', status: 'IN', creator: 'DEV01', createDate: '2026-11-23T14:35:00', lotNo: '', carrierNo: 'CAR-DEV-01' },
  { stockerNo: 'STK-DEV-01', desStockerNo: '', srcLocatorNo: 'STK-DEV-01-L02', desLocatorNo: '', contentNo: 'CAR-DEV-01', contentQty: 0, opNo: '', areaNo: 'AREA-DEV-01', status: 'OUT', creator: 'DEV03', createDate: '2026-11-23T16:00:00', lotNo: '', carrierNo: 'CAR-DEV-01' },
  { stockerNo: 'STK-DEV-02', desStockerNo: 'STK-DEV-02', srcLocatorNo: '', desLocatorNo: 'STK-DEV-02-L01', contentNo: 'LOT-DEV-005', contentQty: 1, opNo: 'OP-TEST-01', areaNo: 'AREA-DEV-02', status: 'IN', creator: 'DEV02', createDate: '2026-11-22T10:30:00', lotNo: 'LOT-DEV-005', carrierNo: '' },
  { stockerNo: 'STK-DEV-EMPTY', desStockerNo: 'STK-DEV-EMPTY', srcLocatorNo: '', desLocatorNo: 'STK-DEV-EMPTY-L03', contentNo: 'CAR-DEV-EMPTY', contentQty: 1, opNo: '', areaNo: 'AREA-DEV-03', status: 'IN', creator: 'DEV01', createDate: '2026-11-21T09:00:00', lotNo: '', carrierNo: 'CAR-DEV-EMPTY' },
]

function localDateTime(value: Date): string {
  const pad = (part: number) => String(part).padStart(2, '0')
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}:${pad(value.getSeconds())}`
}
function defaultStart(): string {
  const now = new Date()
  const start = new Date(now)
  if (now.getHours() < 7 || (now.getHours() === 7 && now.getMinutes() < 30)) start.setDate(start.getDate() - 1)
  start.setHours(7, 30, 0, 0)
  return localDateTime(start)
}
const criteria = ref<Criteria>({ stockerNo: '', lotNo: '', carrierNo: '', useTime: false, start: defaultStart(), end: localDateTime(new Date()), enabled: { stocker: false, lot: false, carrier: false } })
const applied = ref<Criteria | null>(null)
const notice = ref('DEV 假資料：選填 Stocker／Lot／Carrier；勾選 TimeInterval 才套用日期，未勾選時顯示全部歷程。')
const visibleRows = computed(() => {
  const query = applied.value
  if (!query) return []
  const start = query.start ? new Date(query.start).getTime() : Number.NEGATIVE_INFINITY
  const endDate = query.end ? new Date(query.end) : null
  const endIsMidnight = !!endDate && endDate.getHours() === 0 && endDate.getMinutes() === 0 && endDate.getSeconds() === 0
  if (endIsMidnight && endDate) endDate.setHours(23, 59, 59, 0)
  const end = endDate ? endDate.getTime() : Number.POSITIVE_INFINITY
  return rows.filter((row) => (!query.enabled.stocker || !query.stockerNo || row.stockerNo === query.stockerNo)
    && (!query.enabled.lot || !query.lotNo || row.lotNo === query.lotNo)
    && (!query.enabled.carrier || !query.carrierNo || row.carrierNo === query.carrierNo)
    && (!query.useTime || (new Date(row.createDate).getTime() >= start && new Date(row.createDate).getTime() <= end)))
    .slice().sort((a, b) => b.createDate.localeCompare(a.createDate))
})
function syncCheckbox(key: 'stocker' | 'lot' | 'carrier'): void {
  const field = key === 'stocker' ? 'stockerNo' : key === 'lot' ? 'lotNo' : 'carrierNo'
  criteria.value.enabled[key] = criteria.value[field].length > 0
}
function clearIfDisabled(key: 'stocker' | 'lot' | 'carrier'): void {
  if (criteria.value.enabled[key]) return
  const field = key === 'stocker' ? 'stockerNo' : key === 'lot' ? 'lotNo' : 'carrierNo'
  criteria.value[field] = ''
}
function runQuery(): void {
  if (criteria.value.useTime && criteria.value.start && criteria.value.end && criteria.value.start > criteria.value.end) {
    notice.value = '起始時間不可晚於結束時間。'
    return
  }
  applied.value = { ...criteria.value, enabled: { ...criteria.value.enabled } }
  notice.value = `DEV 查詢完成：${visibleRows.value.length} 筆歷程，依 CreateDate 由新到舊排列。`
}
</script>

<template>
  <main class="stocker-history" aria-label="在製貨架歷程查詢">
    <header class="page-head">
      <div><p class="eyebrow">INV · IV307</p><h1>在製貨架歷程查詢</h1><p class="subtitle">Stocker History · StockOn／StockOff 事件記錄</p></div>
      <div class="head-actions"><span class="dev-tag">{{ previewLabel }}</span><button type="button" class="close" @click="emit('close')">Close (X)</button></div>
    </header>

    <section class="query-card">
      <div class="query-title"><div><h2>Query</h2><p>查詢條件可全留白；未啟用時間區間時，會查詢全部歷程</p></div><span class="source-note">StockerNo Lookup 來源為 Stocker Basis</span></div>
      <div class="criteria-grid">
        <div class="criterion"><label class="check"><input v-model="criteria.enabled.stocker" type="checkbox" @change="clearIfDisabled('stocker')"><span>Stocker No</span></label><select v-model="criteria.stockerNo" @change="syncCheckbox('stocker')"><option value="">全部貨架</option><option v-for="value in stockerOptions" :key="value" :value="value">{{ value }}</option></select></div>
        <div class="criterion"><label class="check"><input v-model="criteria.enabled.lot" type="checkbox" @change="clearIfDisabled('lot')"><span>Lot No</span></label><input v-model="criteria.lotNo" placeholder="輸入完整 LotNo" @input="syncCheckbox('lot')"></div>
        <div class="criterion"><label class="check"><input v-model="criteria.enabled.carrier" type="checkbox" @change="clearIfDisabled('carrier')"><span>Carrier No</span></label><input v-model="criteria.carrierNo" placeholder="輸入完整 CarrierNo" @input="syncCheckbox('carrier')"></div>
        <div class="time-criterion"><label class="check"><input v-model="criteria.useTime" type="checkbox"><span>TimeInterval</span></label><label><span>From</span><input v-model="criteria.start" type="datetime-local" step="1"></label><label><span>To</span><input v-model="criteria.end" type="datetime-local" step="1"></label></div>
      </div>
      <div class="query-footer"><p class="hint">日期預設為當班 07:30 至目前時間，但只有勾選 TimeInterval 才會送出。</p><button type="button" class="primary" @click="runQuery">Query</button></div>
      <p class="notice" role="status">{{ notice }}</p>
    </section>

    <section class="history-card">
      <div class="section-head"><div><h2>Stocker Detail Log</h2><p>{{ applied ? `${visibleRows.length} 筆歷程` : '尚未查詢' }}</p></div><span class="read-only">唯讀查詢</span></div>
      <div class="table-wrap"><table><thead><tr><th>StockerNo</th><th>DesStockerNo</th><th>SrcLocatorNo</th><th>DesLocatorNo</th><th>ContentNo</th><th>ContentQty</th><th>OPNo</th><th>AreaNo</th><th>Status</th><th>Creator</th><th>CreateDate</th><th>LotNo</th><th>CarrierNo</th></tr></thead><tbody>
        <tr v-for="(row, index) in visibleRows" :key="`${row.createDate}|${row.contentNo}|${index}`"><td class="code">{{ row.stockerNo }}</td><td class="code">{{ row.desStockerNo || '—' }}</td><td class="code">{{ row.srcLocatorNo || '—' }}</td><td class="code">{{ row.desLocatorNo || '—' }}</td><td class="code">{{ row.contentNo }}</td><td class="numeric">{{ row.contentQty }}</td><td class="code">{{ row.opNo || '—' }}</td><td class="code">{{ row.areaNo }}</td><td><span class="status" :class="row.status.toLowerCase()">{{ row.status }}</span></td><td>{{ row.creator }}</td><td>{{ row.createDate.replace('T', ' ') }}</td><td class="code">{{ row.lotNo || '—' }}</td><td class="code">{{ row.carrierNo || '—' }}</td></tr>
        <tr v-if="!visibleRows.length"><td colspan="13" class="empty">{{ applied ? '沒有符合的 Stocker Detail Log' : '按 Query 後顯示資料' }}</td></tr>
      </tbody></table></div>
      <footer class="card-footer"><span>歷程來源：TBLINVSTOCKERDETAILLOG，Lot OPNO 由 WIP Lot State 左連接。</span><span>DEV fixture 僅存於本頁記憶體，沒有 API／資料庫呼叫。</span></footer>
    </section>
  </main>
</template>

<style scoped>
.stocker-history { height: 100%; min-height: 0; overflow: auto; box-sizing: border-box; padding: 18px 22px 22px; background: #f1f2ef; color: #2f352f; font: 13px/1.45 "Segoe UI", "Microsoft JhengHei", sans-serif; }
.page-head,.head-actions,.query-title,.query-footer,.section-head,.card-footer { display: flex; align-items: center; justify-content: space-between; gap: 13px; }
.page-head { max-width: 1440px; margin: 0 auto 15px; }
.page-head h1 { margin: 0; color: #29372c; font-size: 22px; line-height: 1.25; }
.eyebrow { margin: 0 0 3px; color: #587e63; font-size: 11px; font-weight: 700; letter-spacing: .12em; }
.subtitle,.query-title p,.section-head p { margin: 3px 0 0; color: #767c73; font-size: 11px; }
.head-actions { flex-shrink: 0; }
.dev-tag,.read-only { display: inline-block; border: 1px solid #c0d5c4; border-radius: 999px; padding: 4px 9px; color: #3c6948; background: #edf5ed; font-size: 10px; font-weight: 700; }
.close,.primary { min-height: 32px; border: 1px solid #cdd3cc; border-radius: 5px; padding: 5px 12px; background: white; color: #3d483f; font: inherit; cursor: pointer; }
.primary { border-color: #477c55; background: #477c55; color: white; font-weight: 700; }
.query-card,.history-card { max-width: 1440px; margin: 0 auto 14px; border: 1px solid #d9ded7; border-radius: 8px; background: white; box-shadow: 0 2px 9px #252e260a; }
.query-card { padding: 14px 16px 11px; }
.query-title { margin-bottom: 12px; }
.query-title h2,.section-head h2 { margin: 0; color: #344538; font-size: 14px; }
.source-note,.hint { color: #7b8178; font-size: 10px; }
.criteria-grid { display: grid; grid-template-columns: repeat(3,minmax(190px,1fr)) minmax(320px,1.5fr); gap: 11px; }
.criterion,.time-criterion { display: grid; gap: 5px; min-width: 0; }
.check { display: flex; align-items: center; gap: 6px; color: #596257; font-size: 11px; font-weight: 650; }
.check input { accent-color: #4e845b; }
.criterion input:not([type="checkbox"]),.criterion select,.time-criterion input { width: 100%; min-height: 33px; box-sizing: border-box; border: 1px solid #d1d8d0; border-radius: 4px; padding: 5px 8px; background: white; color: #303830; font: inherit; font-size: 12px; }
.time-criterion { grid-template-columns: 1fr 1fr; align-items: end; }
.time-criterion > .check { grid-column: 1 / -1; }
.time-criterion > label:not(.check) { display: grid; gap: 5px; color: #596257; font-size: 10px; }
.query-footer { margin-top: 12px; border-top: 1px solid #edf0eb; padding-top: 9px; }
.query-footer .hint { margin: 0; }
.notice { margin: 9px 0 0; border-left: 3px solid #77a47d; padding: 5px 9px; color: #5f6d60; background: #f4f8f3; font-size: 10px; }
.section-head { padding: 12px 14px 10px; border-bottom: 1px solid #e8ece7; }
.table-wrap { overflow: auto; }
table { width: 100%; border-collapse: collapse; white-space: nowrap; font-size: 10px; }
th,td { border-bottom: 1px solid #e9ede8; padding: 8px 9px; text-align: left; }
th { position: sticky; top: 0; background: #f4f7f3; color: #596759; font-weight: 700; }
.code { color: #45684d; font-family: Consolas, "Courier New", monospace; }
.numeric { text-align: right; }
.status { display: inline-block; min-width: 28px; border-radius: 999px; padding: 2px 6px; text-align: center; font-size: 9px; font-weight: 700; }
.status.in { background: #e9f5eb; color: #3c7047; }
.status.out { background: #fff0e8; color: #a15d36; }
.empty { height: 90px; color: #899087; text-align: center; vertical-align: middle; }
.card-footer { padding: 8px 13px; color: #81877e; font-size: 9px; }
@media (max-width: 1120px) { .criteria-grid { grid-template-columns: repeat(2,minmax(190px,1fr)); } }
@media (max-width: 680px) { .stocker-history { padding: 12px; } .page-head { align-items: flex-start; } .head-actions { align-items: flex-end; flex-direction: column; gap: 6px; } .criteria-grid { grid-template-columns: 1fr; } .query-title,.query-footer,.section-head,.card-footer { align-items: flex-start; flex-direction: column; } }
</style>
