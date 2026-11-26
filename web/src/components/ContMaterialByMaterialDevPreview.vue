<script setup lang="ts">
import { computed, onUnmounted, ref } from 'vue'

type SearchMode = 'material' | 'product'
interface ConsumptionRow {
  moNo: string
  baseLotNo: string
  opNo: string
  componentNo: string
  materialLotNo: string
  compQty: number
  unitNo: string
  materialType: string
  lotNo: string
  materialNo: string
  materialLevel: 0 | 1
}

const emit = defineEmits<{ close: [] }>()
const materialOptions = ['MAT-10028', 'MAT-20013', 'MAT-30005', 'MAT-50061']
const productOptions = ['PRD-4001', 'PRD-5002', 'PRD-7100']
const sampleRows: ConsumptionRow[] = [
  { moNo: 'MO-260118-01', baseLotNo: 'BL-260118-01', opNo: 'OP010', componentNo: 'CMP-000184', materialLotNo: 'ML-260118-04', compQty: 0.125, unitNo: 'PCS', materialType: 'MAT', lotNo: 'LOT-260118-A01', materialNo: 'MAT-10028', materialLevel: 0 },
  { moNo: 'MO-260118-01', baseLotNo: 'BL-260118-01', opNo: 'OP020', componentNo: 'CMP-000184', materialLotNo: 'ML-260116-02', compQty: 0.0025, unitNo: 'ML', materialType: 'CHEM', lotNo: 'LOT-260118-A01', materialNo: 'PRD-4001', materialLevel: 1 },
  { moNo: 'MO-260118-01', baseLotNo: 'BL-260118-01', opNo: 'OP035', componentNo: 'CMP-000287', materialLotNo: 'ML-260110-08', compQty: 1, unitNo: 'PCS', materialType: 'AUX', lotNo: 'LOT-260118-A01', materialNo: 'MAT-30005', materialLevel: 0 },
  { moNo: 'MO-260119-03', baseLotNo: 'BL-260119-03', opNo: 'OP015', componentNo: 'CMP-000419', materialLotNo: 'ML-260119-01', compQty: 0.25, unitNo: 'PCS', materialType: 'MAT', lotNo: 'LOT-260119-C02', materialNo: 'MAT-10028', materialLevel: 0 },
  { moNo: 'MO-260119-03', baseLotNo: 'BL-260119-03', opNo: 'OP030', componentNo: 'CMP-000511', materialLotNo: 'PL-260119-02', compQty: 1, unitNo: 'PCS', materialType: 'PRD', lotNo: 'LOT-260119-C02', materialNo: 'PRD-4001', materialLevel: 1 },
  { moNo: 'MO-260120-02', baseLotNo: 'BL-260120-02', opNo: 'OP045', componentNo: 'CMP-000184', materialLotNo: 'ML-260120-06', compQty: 0.0625, unitNo: 'PCS', materialType: 'MAT', lotNo: 'LOT-260120-B04', materialNo: 'MAT-20013', materialLevel: 0 },
  { moNo: 'MO-260121-01', baseLotNo: 'BL-260121-01', opNo: 'OP025', componentNo: 'CMP-000624', materialLotNo: 'PL-260121-03', compQty: 0.5, unitNo: 'PCS', materialType: 'PRD', lotNo: 'LOT-260121-A02', materialNo: 'PRD-5002', materialLevel: 1 },
  { moNo: 'MO-260122-05', baseLotNo: 'BL-260122-05', opNo: 'OP050', componentNo: 'CMP-000287', materialLotNo: 'ML-260122-02', compQty: 2, unitNo: 'PCS', materialType: 'MAT', lotNo: 'LOT-260122-D01', materialNo: 'MAT-30005', materialLevel: 0 },
]

const mode = ref<SearchMode>('material')
const materialNo = ref('')
const productNo = ref('')
const materialLotNo = ref('')
const lookupOpen = ref(false)
const lookupText = ref('')
const rows = ref<ConsumptionRow[]>([])
const searched = ref(false)
const busy = ref(false)
const selectedKey = ref('')
const notice = ref('')
let queryTimer: ReturnType<typeof setTimeout> | undefined

const activeQuery = computed(() => mode.value === 'material' ? materialNo.value : productNo.value)
const lookupOptions = computed(() => (mode.value === 'material' ? materialOptions : productOptions)
  .filter((value) => value.toUpperCase().includes(lookupText.value.trim().toUpperCase())))
const sortedRows = computed(() => rows.value.slice().sort((left, right) =>
  left.moNo.localeCompare(right.moNo)
  || left.baseLotNo.localeCompare(right.baseLotNo)
  || left.opNo.localeCompare(right.opNo)
  || left.componentNo.localeCompare(right.componentNo)))
const modeLabel = computed(() => mode.value === 'material' ? 'Material No' : 'Product No')

function setMode(next: SearchMode) {
  if (mode.value === next) return
  mode.value = next
  materialNo.value = ''
  productNo.value = ''
  notice.value = ''
}
function openLookup() {
  lookupText.value = ''
  lookupOpen.value = true
}
function chooseOption(value: string) {
  if (mode.value === 'material') materialNo.value = value
  else productNo.value = value
  lookupOpen.value = false
  notice.value = ''
}
function runQuery() {
  if (busy.value) return
  busy.value = true
  selectedKey.value = ''
  const needle = activeQuery.value.trim().toUpperCase()
  const lotNeedle = materialLotNo.value.trim().toUpperCase()
  if (mode.value === 'material') materialNo.value = needle
  else productNo.value = needle
  materialLotNo.value = lotNeedle
  if (queryTimer) clearTimeout(queryTimer)
  queryTimer = setTimeout(() => {
    const level = mode.value === 'material' ? 0 : 1
    rows.value = sampleRows.filter((row) => row.materialLevel === level
      && (!needle || row.materialNo === needle)
      && (!lotNeedle || row.materialLotNo === lotNeedle))
    searched.value = true
    notice.value = needle ? '' : `舊 Core 只省略 ${modeLabel.value} 條件，仍限制 MaterialLevel=${level}；DEV 只顯示有限假資料。`
    busy.value = false
    queryTimer = undefined
  }, 180)
}
function rowKey(row: ConsumptionRow) {
  return [row.moNo, row.baseLotNo, row.opNo, row.componentNo, row.materialLotNo].join('|')
}
function quantity(value: number) {
  return value.toFixed(4).replace(/0+$/, '').replace(/\.$/, '')
}

onUnmounted(() => { if (queryTimer) clearTimeout(queryTimer) })
</script>

<template>
  <main class="material-query" aria-label="物料用於元件查詢 DEV">
    <header class="page-heading">
      <div>
        <div class="eyebrow">RPT / RT604</div>
        <h1>物料用於元件查詢</h1>
        <p>依物料或產品及物料批號，查詢用於哪些元件與生產批。</p>
      </div>
      <span class="dev-badge">DEV 假資料</span>
    </header>

    <section class="query-panel" aria-label="查詢條件">
      <div class="mode-options" role="radiogroup" aria-label="查詢類型">
        <label><input type="radio" name="material-search-mode" :checked="mode === 'material'" @change="setMode('material')">Material No</label>
        <label><input type="radio" name="material-search-mode" :checked="mode === 'product'" @change="setMode('product')">Product No</label>
      </div>
      <label class="lookup-label material-label" for="material-no">Material No</label>
      <div class="lookup-input material-input">
        <input id="material-no" v-model="materialNo" type="text" autocomplete="off" placeholder="輸入或選擇物料" :readonly="mode !== 'material'" @focus="($event.target as HTMLInputElement).select()" @blur="materialNo = materialNo.toUpperCase()">
        <button type="button" aria-label="選擇物料" :disabled="mode !== 'material'" @click="openLookup">…</button>
      </div>
      <label class="lookup-label product-label" for="product-no">Product No</label>
      <div class="lookup-input product-input">
        <input id="product-no" v-model="productNo" type="text" autocomplete="off" placeholder="輸入或選擇產品" :readonly="mode !== 'product'" @focus="($event.target as HTMLInputElement).select()" @blur="productNo = productNo.toUpperCase()">
        <button type="button" aria-label="選擇產品" :disabled="mode !== 'product'" @click="openLookup">…</button>
      </div>
      <label class="lot-label" for="material-lot-no">MaterialLot No</label>
      <input id="material-lot-no" v-model="materialLotNo" type="text" autocomplete="off" @focus="($event.target as HTMLInputElement).select()" @blur="materialLotNo = materialLotNo.toUpperCase()">
      <button class="query-button" type="button" :disabled="busy" @click="runQuery">{{ busy ? '查詢中…' : 'Query' }}</button>
    </section>

    <section class="result-panel" aria-label="查詢結果">
      <div class="result-heading">
        <div><h2>使用元件明細</h2><p>{{ searched ? `${modeLabel}：${activeQuery || '（空白）'}${materialLotNo ? `；MaterialLot No：${materialLotNo}` : ''}` : '請選擇 Material No 或 Product No 後執行查詢。' }}</p></div>
        <span class="result-count">{{ searched ? `${sortedRows.length} 筆` : '尚未查詢' }}</span>
      </div>
      <p v-if="notice" class="legacy-note">{{ notice }}</p>
      <div class="table-wrap">
        <table>
          <thead><tr><th>MO No</th><th>BaseLot No</th><th class="op-col">OP No</th><th>Component No</th><th>MaterialLot No</th><th class="qty-col">Comp Qty</th><th>Unit No</th><th>Material Type</th></tr></thead>
          <tbody v-if="searched && sortedRows.length">
            <tr v-for="row in sortedRows" :key="rowKey(row)" :class="{ selected: selectedKey === rowKey(row) }" @click="selectedKey = rowKey(row)">
              <td class="mono">{{ row.moNo }}</td><td class="mono">{{ row.baseLotNo }}</td><td>{{ row.opNo }}</td><td class="mono">{{ row.componentNo }}</td><td class="mono">{{ row.materialLotNo }}</td><td class="qty">{{ quantity(row.compQty) }}</td><td>{{ row.unitNo }}</td><td>{{ row.materialType }}</td>
            </tr>
          </tbody>
          <tbody v-else><tr><td colspan="8" class="empty-state">{{ searched ? '沒有符合條件的用料紀錄' : '尚未執行查詢' }}</td></tr></tbody>
        </table>
      </div>
      <footer class="grid-footer"><span>排序：MO No → BaseLot No → OP No → Component No</span><span v-if="searched">MaterialNo、LotNo 為隱藏欄位</span></footer>
    </section>
    <footer class="page-footer"><span>查詢來源：DEV 記憶體假資料</span><button class="close-button" type="button" @click="emit('close')">Close (X)</button></footer>

    <div v-if="lookupOpen" class="modal-backdrop" @click.self="lookupOpen = false">
      <section class="lookup-dialog" role="dialog" aria-modal="true" :aria-label="`${modeLabel} 選擇`">
        <header><div><h2>選擇{{ modeLabel }}</h2><p>清單以 DEV 假資料模擬 {{ mode === 'material' ? 'LoadMaterialBasis' : 'LoadProduct_Dist' }}。</p></div><button type="button" aria-label="關閉" @click="lookupOpen = false">×</button></header>
        <input v-model="lookupText" type="search" placeholder="搜尋編號" autofocus>
        <ul v-if="lookupOptions.length"><li v-for="value in lookupOptions" :key="value"><button type="button" @click="chooseOption(value)">{{ value }}</button></li></ul>
        <p v-else class="no-options">沒有符合的假資料。</p>
      </section>
    </div>
  </main>
</template>

<style scoped>
.material-query{box-sizing:border-box;display:flex;flex-direction:column;gap:12px;min-height:0;height:100%;padding:14px;background:#f4f3ee;color:#292820;font:inherit;overflow:auto}.page-heading{display:flex;justify-content:space-between;gap:16px}.eyebrow{margin-bottom:4px;color:#777366;font-size:11px;letter-spacing:.1em}.page-heading h1{margin:0;font-size:20px;font-weight:650}.page-heading p{margin:5px 0 0;color:#777366;font-size:12px}.dev-badge{height:max-content;padding:5px 9px;border:1px solid #d3c79c;border-radius:3px;background:#f5efd8;color:#66571f;font-size:11px;font-weight:700}
.query-panel{display:grid;grid-template-columns:155px auto minmax(165px,230px) auto auto minmax(165px,230px) auto auto;align-items:center;gap:8px;padding:11px;border:1px solid #cbc9bd;background:#fbfaf6}.mode-options{display:flex;flex-direction:column;gap:5px;grid-row:1/3}.mode-options label{display:flex;align-items:center;gap:6px;font-size:12px}.mode-options input{margin:0}.lookup-label,.lot-label{font-weight:600}.lookup-input{display:flex;min-width:0}.lookup-input input,.query-panel>input{box-sizing:border-box;width:100%;min-width:0;height:30px;padding:4px 7px;border:1px solid #a9a69a;border-radius:2px;background:#fff;color:#292820;font:inherit}.lookup-input button{width:29px;border:1px solid #a9a69a;border-left:0;background:#efeee8;color:#292820;cursor:pointer}.lookup-input input:focus,.query-panel>input:focus{outline:2px solid #8d9d91;outline-offset:1px}.query-button,.close-button{min-height:30px;padding:4px 14px;border:1px solid #3b3930;border-radius:3px;background:#3b3930;color:white;font:inherit;cursor:pointer}.query-button:disabled{opacity:.65;cursor:wait}
.result-panel{display:flex;flex:1;flex-direction:column;min-height:170px;border:1px solid #cbc9bd;background:#fbfaf6}.result-heading{display:flex;align-items:center;justify-content:space-between;gap:14px;padding:11px 12px;border-bottom:1px solid #d9d7cc}.result-heading h2{margin:0;font-size:14px}.result-heading p{margin:4px 0 0;color:#777366;font-size:11px}.result-count{color:#555247;font-size:12px}.legacy-note{margin:8px 12px 0;padding:6px 8px;border-left:3px solid #c69b43;background:#fbf5e5;color:#6e5929;font-size:11px}.table-wrap{flex:1;min-height:125px;overflow:auto}table{width:100%;border-collapse:collapse;font-size:12px;white-space:nowrap}th{position:sticky;top:0;z-index:1;padding:8px 9px;border-right:1px solid #d5d3c7;border-bottom:1px solid #c5c2b5;background:#eeece3;text-align:left;font-weight:650}td{padding:7px 9px;border-right:1px solid #e3e1d8;border-bottom:1px solid #e3e1d8}tbody tr{cursor:pointer}tbody tr:hover{background:#f1f2ea}tbody tr.selected{background:#e4eadf}.table-wrap th:nth-child(1),.table-wrap td:nth-child(1),.table-wrap th:nth-child(2),.table-wrap td:nth-child(2){min-width:130px}.table-wrap th:nth-child(3),.table-wrap td:nth-child(3){width:80px}.table-wrap th:nth-child(4),.table-wrap td:nth-child(4),.table-wrap th:nth-child(5),.table-wrap td:nth-child(5){min-width:120px}.table-wrap th:nth-child(6),.table-wrap td:nth-child(6),.table-wrap th:nth-child(7),.table-wrap td:nth-child(7){min-width:70px}.qty{text-align:right;font-variant-numeric:tabular-nums}.mono{font-family:ui-monospace,SFMono-Regular,Consolas,monospace;font-size:11px}.empty-state{height:110px;text-align:center;color:#858173;cursor:default}.grid-footer{display:flex;justify-content:space-between;gap:10px;padding:6px 10px;border-top:1px solid #d9d7cc;color:#777366;font-size:10px}.page-footer{display:flex;justify-content:space-between;align-items:center;color:#777366;font-size:11px}
.modal-backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:18px;background:#1d1d1980}.lookup-dialog{box-sizing:border-box;width:min(430px,100%);max-height:min(600px,85vh);display:flex;flex-direction:column;gap:10px;padding:14px;border:1px solid #aaa797;border-radius:4px;background:#fbfaf6;box-shadow:0 14px 40px #0003}.lookup-dialog header{display:flex;justify-content:space-between;align-items:flex-start}.lookup-dialog h2{margin:0;font-size:15px}.lookup-dialog p{margin:4px 0 0;color:#777366;font-size:11px}.lookup-dialog header button{width:28px;height:28px;border:1px solid #c8c5b9;background:#f2f1ec;font-size:19px;cursor:pointer}.lookup-dialog>input{height:30px;padding:4px 7px;border:1px solid #aaa797;font:inherit}.lookup-dialog ul{margin:0;padding:0;overflow:auto;list-style:none}.lookup-dialog li+li{border-top:1px solid #e2e0d7}.lookup-dialog li button{width:100%;padding:9px;text-align:left;border:0;background:transparent;font:inherit;cursor:pointer}.lookup-dialog li button:hover{background:#eeeee5}.no-options{padding:20px;text-align:center}@media(max-width:960px){.query-panel{grid-template-columns:130px 1fr auto;}.mode-options{grid-row:1/3;grid-column:1}.lookup-label{grid-column:2}.lookup-input{grid-column:3}.lot-label{grid-column:2}.query-panel>input{grid-column:3}.query-button{grid-column:3}}@media(max-width:620px){.query-panel{grid-template-columns:1fr auto}.mode-options{grid-column:1/-1;grid-row:auto;flex-direction:row}.lookup-label,.lot-label{grid-column:1/-1}.lookup-input,.query-panel>input{grid-column:1}.query-button{grid-column:2}.page-heading h1{font-size:17px}.grid-footer{flex-direction:column;gap:3px}}
.close-button{min-width:80px}
.query-panel{grid-template-columns:150px 100px minmax(165px,230px) auto 105px minmax(150px,220px) auto;grid-template-rows:repeat(2,minmax(30px,auto))}.mode-options{grid-column:1;grid-row:1/3}.material-label{grid-column:2;grid-row:1}.material-input{grid-column:3/5;grid-row:1}.product-label{grid-column:2;grid-row:2}.product-input{grid-column:3/5;grid-row:2}.lot-label{grid-column:5;grid-row:1}.query-panel>#material-lot-no{grid-column:6;grid-row:1}.query-panel>.query-button{grid-column:7;grid-row:2}.lookup-input input[readonly]{background:#f0efe9;color:#777366}.lookup-input button:disabled{opacity:.5;cursor:not-allowed}
@media(max-width:960px){.query-panel{grid-template-columns:125px 105px minmax(150px,1fr) auto;grid-template-rows:repeat(3,minmax(30px,auto))}.mode-options{grid-column:1;grid-row:1/3}.material-label{grid-column:2;grid-row:1}.material-input{grid-column:3/5;grid-row:1}.product-label{grid-column:2;grid-row:2}.product-input{grid-column:3/5;grid-row:2}.lot-label{grid-column:2;grid-row:3}.query-panel>#material-lot-no{grid-column:3;grid-row:3}.query-panel>.query-button{grid-column:4;grid-row:3}}
@media(max-width:620px){.query-panel{grid-template-columns:105px minmax(140px,1fr) auto;grid-template-rows:auto repeat(4,minmax(30px,auto))}.mode-options{grid-column:1/-1;grid-row:1;flex-direction:row}.material-label{grid-column:1;grid-row:2}.material-input{grid-column:2/4;grid-row:2}.product-label{grid-column:1;grid-row:3}.product-input{grid-column:2/4;grid-row:3}.lot-label{grid-column:1;grid-row:4}.query-panel>#material-lot-no{grid-column:2/4;grid-row:4}.query-panel>.query-button{grid-column:3;grid-row:5}}
</style>
