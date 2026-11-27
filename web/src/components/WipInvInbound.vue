<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type InvLot = {
  lotNo: string
  woNo: string
  productNo: string
  goodQty: number
  customerNo: string
  wipMLotNo: string
  remark: string
  packGood: number
  packBad: number
  remainder: number
  liteOn: boolean
  checked: boolean
}
type DetailItem = { key: string; value: string }
type InboundLog = { time: string; lotNo: string; goodQty: number; badQty: number; result: string }

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const dev = import.meta.env.DEV && props.developmentPreview
const mode = ref<'Normal' | 'Lite-On'>('Normal')
const filters = ref({ productNo: '', lotNo: '' })
const scenario = ref<'success' | 'second-fails'>('success')
const busy = ref(false)
const error = ref('')
const message = ref('DEV 假資料已載入，可依產品或 Lot 查詢。')
const allLots: InvLot[] = [
  { lotNo: 'WIP26001101', woNo: 'MO260011', productNo: 'TH-PN-1001', goodQty: 24, customerNo: 'CUST-01', wipMLotNo: 'WIP-M-26001101', remark: '一般入庫批', packGood: 0, packBad: 0, remainder: 0, liteOn: false, checked: false },
  { lotNo: 'WIP26001102', woNo: 'MO260011', productNo: 'TH-PN-1001', goodQty: 18, customerNo: 'CUST-01', wipMLotNo: 'WIP-M-26001102', remark: '', packGood: 0, packBad: 0, remainder: 0, liteOn: false, checked: false },
  { lotNo: 'LO26002201', woNo: 'MO260022', productNo: 'TH-PN-2200', goodQty: 30, customerNo: 'CUST-02', wipMLotNo: 'WIP-M-26002201', remark: 'Lite-On 包裝資料', packGood: 28, packBad: 1, remainder: 1, liteOn: true, checked: false },
  { lotNo: 'LO26002202', woNo: 'MO260022', productNo: 'TH-PN-2200', goodQty: 16, customerNo: 'CUST-02', wipMLotNo: 'WIP-M-26002202', remark: '待入庫', packGood: 15, packBad: 0, remainder: 1, liteOn: true, checked: false },
]
const rows = ref<InvLot[]>([])
const inbounded = ref(new Set<string>())
const activeLotNo = ref('')
const goodDetails = ref<DetailItem[]>([])
const badDetails = ref<DetailItem[]>([])
const history = ref<InboundLog[]>([])
const selectedRows = computed(() => rows.value.filter(row => row.checked))
const activeRow = computed(() => rows.value.find(row => row.lotNo === activeLotNo.value) || null)

function searchLots(): void {
  if (!dev) return
  error.value = ''
  const productNo = filters.value.productNo.trim().toUpperCase()
  const lotNo = filters.value.lotNo.trim().toUpperCase()
  rows.value = allLots.filter(row => row.liteOn === (mode.value === 'Lite-On') &&
    !inbounded.value.has(row.lotNo) &&
    (!productNo || row.productNo.toUpperCase().includes(productNo)) &&
    (!lotNo || row.lotNo.toUpperCase().includes(lotNo)))
    .map(row => ({ ...row, checked: false }))
  activeLotNo.value = ''
  goodDetails.value = []
  badDetails.value = []
  message.value = `查得 ${rows.value.length} 筆待入庫 Lot。`
}

function loadDetails(row: InvLot): void {
  if (!dev) return
  activeLotNo.value = row.lotNo
  // These illustrative fixture keys are not source response columns. The old API client
  // displays arbitrary returned fields; the actual Good/Bad schemas remain unknown.
  goodDetails.value = [
    { key: 'Detail', value: 'GOOD component summary (DEV)' },
    { key: 'Quantity', value: String(row.liteOn ? row.packGood : row.goodQty) },
    { key: 'Unit', value: 'PCS' },
  ]
  badDetails.value = row.liteOn && row.packBad > 0
    ? [{ key: 'Detail', value: 'BAD component summary (DEV)' }, { key: 'Quantity', value: String(row.packBad) }, { key: 'Unit', value: 'PCS' }]
    : []
}

async function inboundSelected(): Promise<void> {
  if (!dev || busy.value) return
  const targets = selectedRows.value
  if (!targets.length) { error.value = '請至少勾選一筆資料。'; return }
  busy.value = true
  error.value = ''
  let completed = 0
  try {
    for (let index = 0; index < targets.length; index += 1) {
      const row = targets[index]
      await new Promise(resolve => window.setTimeout(resolve, 120))
      if (scenario.value === 'second-fails' && targets.length > 1 && index === 1) {
        history.value.unshift({ time: new Date().toLocaleString(), lotNo: row.lotNo, goodQty: 0, badQty: 0, result: '模擬失敗' })
        error.value = `LOTNO ${row.lotNo} 入庫失敗；前 ${completed} 筆已成功，本次不自動重查。`
        message.value = '來源逐筆處理，前筆成功不會因後續失敗回復。請先確認實際入庫狀態，再決定是否重送。'
        return
      }
      const isLiteOn = mode.value === 'Lite-On'
      const goodQty = isLiteOn ? row.packGood : row.goodQty
      const badQty = isLiteOn ? row.packBad : 0
      inbounded.value.add(row.lotNo)
      history.value.unshift({ time: new Date().toLocaleString(), lotNo: row.lotNo, goodQty, badQty, result: 'DEV 成功' })
      completed += 1
    }
    searchLots()
    message.value = `${completed} 筆 DEV 假資料已模擬入庫。`
  } finally {
    busy.value = false
  }
}

onMounted(searchLots)
</script>

<template>
  <section v-if="dev" class="wip-inbound" aria-label="入庫作業" :aria-busy="busy">
    <header class="page-head">
      <div><p class="eyebrow">WIP · WIPINV_CS · INVENTORY INBOUND</p><h1>入庫作業</h1><p>查詢待入庫 Lot、檢視 GOOD／BAD 明細，再逐筆模擬入庫。</p></div>
      <div class="head-actions"><span class="dev-pill">DEV 假資料</span><button type="button" class="quiet" @click="emit('close')">Close (X)</button></div>
    </header>
    <p v-if="error" class="feedback error" role="alert">{{ error }}</p><p v-else class="feedback" role="status">{{ message }}</p>

    <section class="card query-card">
      <label>INV ITEM<select v-model="mode" :disabled="busy"><option>Normal</option><option>Lite-On</option></select></label>
      <label>TH PN<input v-model.trim="filters.productNo" maxlength="40" :disabled="busy" @keydown.enter.prevent="searchLots"></label>
      <label>LOTNO<input v-model.trim="filters.lotNo" maxlength="40" :disabled="busy" @keydown.enter.prevent="searchLots"></label>
      <div class="query-actions"><button type="button" class="primary" :disabled="busy" @click="searchLots">{{ busy ? '處理中…' : 'Search' }}</button><button type="button" class="primary" :disabled="busy || !selectedRows.length" @click="inboundSelected">Inbound ({{ selectedRows.length }})</button></div>
      <label class="scenario">DEV 入庫情境<select v-model="scenario" :disabled="busy"><option value="success">全部成功</option><option value="second-fails">第二筆失敗（須選至少兩筆）</option></select></label>
    </section>

    <section class="card list-card"><div class="section-title"><div><h2>待入庫 Lot</h2><p>點列載入明細；勾選列後按 Inbound。欄位依 Form 綁定名稱。</p></div><span>{{ rows.length }} 筆 · 已勾 {{ selectedRows.length }} 筆</span></div>
      <div class="table-scroll"><table class="lots"><thead><tr><th>選取</th><th>WONO</th><th>TH PN</th><th>LOTNO</th><th>Good QTY</th><th>CUSTOMERNO</th><th>WIP_M_LOTNO</th><th>REMARK</th><th>pack_good</th><th>pack_bad</th><th>remainder</th></tr></thead><tbody>
        <tr v-for="row in rows" :key="row.lotNo" :class="{ selected: activeLotNo === row.lotNo }" @click="loadDetails(row)"><td><input v-model="row.checked" type="checkbox" :aria-label="`選取 ${row.lotNo}`" @click.stop></td><td>{{ row.woNo }}</td><td>{{ row.productNo }}</td><td class="mono">{{ row.lotNo }}</td><td class="num">{{ row.goodQty }}</td><td>{{ row.customerNo }}</td><td class="mono">{{ row.wipMLotNo }}</td><td>{{ row.remark }}</td><td class="num">{{ row.packGood || '' }}</td><td class="num">{{ row.packBad || '' }}</td><td class="num">{{ row.remainder || '' }}</td></tr>
        <tr v-if="!rows.length"><td colspan="11" class="empty">沒有符合條件的待入庫 Lot</td></tr>
      </tbody></table></div>
    </section>

    <section class="details-grid">
      <article class="card"><div class="section-title"><div><h2>GOOD</h2><p>{{ activeRow ? `${activeRow.lotNo} · DEV 示意欄位` : '請選取一筆 Lot' }}</p></div></div><div class="table-scroll"><table><thead><tr><th>回傳欄位</th><th>值</th></tr></thead><tbody><tr v-for="item in goodDetails" :key="item.key"><td>{{ item.key }}</td><td>{{ item.value }}</td></tr><tr v-if="!goodDetails.length"><td colspan="2" class="empty">無 GOOD 明細</td></tr></tbody></table></div></article>
      <article class="card"><div class="section-title"><div><h2>BAD</h2><p>{{ activeRow ? `${activeRow.lotNo} · DEV 示意欄位` : '請選取一筆 Lot' }}</p></div></div><div class="table-scroll"><table><thead><tr><th>回傳欄位</th><th>值</th></tr></thead><tbody><tr v-for="item in badDetails" :key="item.key"><td>{{ item.key }}</td><td>{{ item.value }}</td></tr><tr v-if="!badDetails.length"><td colspan="2" class="empty">無 BAD 明細</td></tr></tbody></table></div></article>
    </section>

    <section class="card history"><div class="section-title"><div><h2>DEV 入庫結果</h2><p>逐筆處理；若中途失敗，前面成功的列不回復。</p></div><span>{{ history.length }} 筆</span></div><div class="table-scroll"><table><thead><tr><th>時間</th><th>LOTNO</th><th>Good Qty</th><th>Bad Qty</th><th>結果</th></tr></thead><tbody><tr v-for="(item, index) in history" :key="item.time + item.lotNo + index"><td>{{ item.time }}</td><td class="mono">{{ item.lotNo }}</td><td class="num">{{ item.goodQty }}</td><td class="num">{{ item.badQty }}</td><td>{{ item.result }}</td></tr><tr v-if="!history.length"><td colspan="5" class="empty">尚無模擬入庫</td></tr></tbody></table></div></section>
    <footer class="source-note">Form：frmWP_INV_THEIL_BU1 · Normal 送 Good QTY／BadQty=0；Lite-On 送 pack_good／pack_bad · GOOD／BAD 回傳欄位尚待服務端來源確認</footer>
  </section>
  <section v-else class="pending" aria-label="入庫作業正式環境狀態"><p role="status">正式入庫服務尚未完成來源核對與安全接線；此頁不送出資料。</p><button type="button" class="quiet" @click="emit('close')">Close (X)</button></section>
</template>

<style scoped>
.wip-inbound,.pending{box-sizing:border-box;min-height:0;height:100%;padding:12px;background:#f4f6f8;color:#1d2939;font:inherit;overflow:auto}.page-head,.section-title,.head-actions,.query-actions{display:flex;align-items:center;justify-content:space-between;gap:12px}.page-head{margin-bottom:10px}.page-head h1{margin:0;font-size:22px}.page-head p,.section-title p{margin:3px 0;color:#667085;font-size:12px}.eyebrow{font-size:10px!important;letter-spacing:.08em;color:#475467!important}.head-actions{flex-shrink:0}.dev-pill{padding:4px 9px;border-radius:999px;background:#dcfae6;color:#067647;font-size:11px;font-weight:700}.quiet,.primary{min-height:30px;padding:5px 12px;border:1px solid #98a2b3;border-radius:5px;background:white;color:#344054;font:inherit;cursor:pointer}.primary{border-color:#175cd3;background:#175cd3;color:white}.primary:disabled{opacity:.55;cursor:not-allowed}.feedback{margin:6px 0;padding:8px 10px;border-radius:5px;background:#eff8ff;color:#175cd3;font-size:12px}.feedback.error{background:#fef3f2;color:#b42318}.card{min-width:0;margin:8px 0;padding:10px;border:1px solid #d0d5dd;border-radius:7px;background:#fff}.query-card{display:grid;grid-template-columns:minmax(140px,.8fr) minmax(180px,1fr) minmax(180px,1fr) auto minmax(220px,1fr);align-items:end;gap:10px}.query-card label{display:grid;gap:4px;color:#475467;font-size:11px;font-weight:600}.query-card input,.query-card select{box-sizing:border-box;width:100%;height:30px;padding:4px 7px;border:1px solid #d0d5dd;border-radius:4px;background:white;color:#101828;font:inherit}.query-actions{justify-content:flex-start}.scenario{max-width:260px}.section-title{margin-bottom:7px}.section-title h2{margin:0;font-size:14px}.section-title>span{color:#667085;font-size:11px;white-space:nowrap}.table-scroll{overflow:auto;max-height:250px}table{width:100%;border-collapse:collapse;font-size:11px;white-space:nowrap}th,td{padding:6px 8px;border-bottom:1px solid #eaecf0;text-align:left}th{position:sticky;top:0;background:#f9fafb;color:#475467;font-weight:700;z-index:1}tbody tr{cursor:pointer}tbody tr:hover{background:#f8fafc}tbody tr.selected{background:#eff8ff}.lots{min-width:1120px}.num{text-align:right;font-variant-numeric:tabular-nums}.mono{font-family:ui-monospace,SFMono-Regular,Consolas,monospace}.empty{text-align:center!important;color:#98a2b3;padding:16px}.details-grid{display:grid;grid-template-columns:1fr 1fr;gap:8px}.details-grid .card{margin:0}.details-grid .table-scroll{max-height:130px}.history .table-scroll{max-height:150px}.source-note{display:block;padding:5px 2px;color:#667085;font-size:10px}.pending{display:flex;align-items:center;justify-content:space-between;gap:12px}.pending p{margin:0;color:#667085}.pending .quiet{flex-shrink:0}@media(max-width:1100px){.query-card{grid-template-columns:repeat(2,minmax(140px,1fr))}.query-actions{align-self:end}.details-grid{grid-template-columns:1fr}}@media(max-width:640px){.page-head{align-items:flex-start}.page-head h1{font-size:18px}.query-card{grid-template-columns:1fr}.head-actions{flex-direction:column;align-items:flex-end}.pending{align-items:flex-start;flex-direction:column}}
</style>
