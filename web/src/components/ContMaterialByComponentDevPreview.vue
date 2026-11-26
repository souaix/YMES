<script setup lang="ts">
import { computed, onUnmounted, ref } from 'vue'

interface ConsumptionRow {
  opNo: string
  materialType: string
  materialNo: string
  materialLotNo: string
  componentNo: string
  compQty: number
  unitNo: string
  moNo: string
  baseLotNo: string
  lotNo: string
}

const emit = defineEmits<{ close: [] }>()
const sampleRows: ConsumptionRow[] = [
  { opNo: 'OP010', materialType: 'MAT', materialNo: 'MAT-10028', materialLotNo: 'ML-260118-04', componentNo: 'CMP-000184', compQty: 0.125, unitNo: 'PCS', moNo: 'MO-260118-01', baseLotNo: 'BL-260118-01', lotNo: 'LOT-260118-A01' },
  { opNo: 'OP020', materialType: 'CHEM', materialNo: 'CH-00210', materialLotNo: 'CL-260116-02', componentNo: 'CMP-000184', compQty: 0.0025, unitNo: 'ML', moNo: 'MO-260118-01', baseLotNo: 'BL-260118-01', lotNo: 'LOT-260118-A01' },
  { opNo: 'OP020', materialType: 'MAT', materialNo: 'MAT-10028', materialLotNo: 'ML-260119-01', componentNo: 'CMP-000184', compQty: 0.125, unitNo: 'PCS', moNo: 'MO-260119-03', baseLotNo: 'BL-260119-03', lotNo: 'LOT-260119-C02' },
  { opNo: 'OP035', materialType: 'AUX', materialNo: 'AX-00076', materialLotNo: 'AL-260110-08', componentNo: 'CMP-000184', compQty: 1, unitNo: 'PCS', moNo: 'MO-260118-01', baseLotNo: 'BL-260118-01', lotNo: 'LOT-260118-A01' },
  { opNo: 'OP045', materialType: 'MAT', materialNo: 'MAT-20013', materialLotNo: 'ML-260120-06', componentNo: 'CMP-000184', compQty: 0.0625, unitNo: 'PCS', moNo: 'MO-260120-02', baseLotNo: 'BL-260120-02', lotNo: 'LOT-260120-B04' },
  { opNo: 'OP010', materialType: 'MAT', materialNo: 'MAT-30005', materialLotNo: 'ML-260114-02', componentNo: 'CMP-000287', compQty: 0.25, unitNo: 'PCS', moNo: 'MO-260114-04', baseLotNo: 'BL-260114-04', lotNo: 'LOT-260114-D03' },
  { opNo: 'OP025', materialType: 'CHEM', materialNo: 'CH-00102', materialLotNo: 'CL-260112-01', componentNo: 'CMP-000287', compQty: 0.015, unitNo: 'ML', moNo: 'MO-260114-04', baseLotNo: 'BL-260114-04', lotNo: 'LOT-260114-D03' },
  { opNo: 'OP040', materialType: 'AUX', materialNo: 'AX-00012', materialLotNo: 'AL-260108-03', componentNo: 'CMP-000287', compQty: 2, unitNo: 'PCS', moNo: 'MO-260114-04', baseLotNo: 'BL-260114-04', lotNo: 'LOT-260114-D03' },
  { opNo: 'OP015', materialType: 'MAT', materialNo: 'MAT-50061', materialLotNo: 'ML-260121-03', componentNo: 'CMP-000511', compQty: 0.5, unitNo: 'PCS', moNo: 'MO-260121-01', baseLotNo: 'BL-260121-01', lotNo: 'LOT-260121-A02' },
]

const componentNo = ref('')
const searchedComponentNo = ref<string | null>(null)
const rows = ref<ConsumptionRow[]>([])
const busy = ref(false)
const selectedKey = ref('')
const queriedAt = ref('')
const emptyQueryWarning = computed(() => searchedComponentNo.value === '' && rows.value.length > 0)
let queryTimer: ReturnType<typeof setTimeout> | undefined

const sortedRows = computed(() => rows.value.slice().sort((left, right) => left.opNo.localeCompare(right.opNo)))
const statusText = computed(() => searchedComponentNo.value === null
  ? '輸入 Component No 後按 Query；空白查詢會列出全部假資料。'
  : `Component No：${searchedComponentNo.value || '（空白，未套用元件條件）'}`)

function runQuery() {
  if (busy.value) return
  busy.value = true
  selectedKey.value = ''
  const needle = componentNo.value.trim().toUpperCase()
  componentNo.value = needle
  if (queryTimer) clearTimeout(queryTimer)
  queryTimer = setTimeout(() => {
    searchedComponentNo.value = needle
    // The legacy Core omits the ComponentNo predicate when its value is blank.
    rows.value = sampleRows.filter((row) => !needle || row.componentNo === needle)
    queriedAt.value = new Date().toLocaleString('zh-TW', { hour12: false })
    busy.value = false
    queryTimer = undefined
  }, 180)
}

function rowKey(row: ConsumptionRow) {
  return [row.opNo, row.materialNo, row.materialLotNo, row.lotNo].join('|')
}
function quantity(value: number) {
  return value.toFixed(4).replace(/0+$/, '').replace(/\.$/, '')
}

onUnmounted(() => { if (queryTimer) clearTimeout(queryTimer) })
</script>

<template>
  <main class="comp-material" aria-label="元件使用物料查詢 DEV">
    <header class="page-heading">
      <div>
        <div class="eyebrow">RPT / RT603</div>
        <h1>元件使用物料查詢</h1>
        <p>依 Component No 查詢該元件使用的物料與生產批資料。</p>
      </div>
      <span class="dev-badge">DEV 假資料</span>
    </header>

    <section class="query-panel" aria-label="查詢條件">
      <label for="component-no">Component No</label>
      <input
        id="component-no"
        v-model="componentNo"
        type="text"
        autocomplete="off"
        placeholder="例如 CMP-000184"
        @blur="componentNo = componentNo.toUpperCase()"
        @keydown.enter.prevent="runQuery"
      >
      <button class="query-button" type="button" :disabled="busy" @click="runQuery">
        {{ busy ? '查詢中…' : 'Query' }}
      </button>
      <span class="query-hint">離開欄位時轉為大寫</span>
    </section>

    <section class="result-panel" aria-label="查詢結果">
      <div class="result-heading">
        <div>
          <h2>元件用料明細</h2>
          <p>{{ statusText }}</p>
        </div>
        <div class="result-meta">
          <span>{{ searchedComponentNo === null ? '尚未查詢' : `${sortedRows.length} 筆` }}</span>
          <small v-if="queriedAt">{{ queriedAt }}</small>
        </div>
      </div>
      <p v-if="emptyQueryWarning" class="legacy-note">舊 Core 在 Component No 空白時不加查詢條件；此處以有限的 DEV 假資料模擬全量結果。</p>

      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th class="op-col" aria-sort="ascending">OP No <span aria-hidden="true">↑</span></th>
              <th>Material Type</th>
              <th>Material No</th>
              <th>MaterialLot No</th>
              <th class="qty-col">Comp Qty</th>
              <th>Unit No</th>
              <th>MO No</th>
              <th>BaseLot No</th>
            </tr>
          </thead>
          <tbody v-if="searchedComponentNo !== null && sortedRows.length">
            <tr
              v-for="row in sortedRows"
              :key="rowKey(row)"
              :class="{ selected: selectedKey === rowKey(row) }"
              @click="selectedKey = rowKey(row)"
            >
              <td>{{ row.opNo }}</td>
              <td>{{ row.materialType }}</td>
              <td class="mono">{{ row.materialNo }}</td>
              <td class="mono">{{ row.materialLotNo }}</td>
              <td class="qty">{{ quantity(row.compQty) }}</td>
              <td>{{ row.unitNo }}</td>
              <td class="mono">{{ row.moNo }}</td>
              <td class="mono">{{ row.baseLotNo }}</td>
            </tr>
          </tbody>
          <tbody v-else>
            <tr>
              <td colspan="8" class="empty-state">
                {{ searchedComponentNo === null ? '尚未執行查詢' : '沒有符合條件的用料紀錄' }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <footer class="grid-footer">
        <span>依 OP No 遞增排序</span>
        <span v-if="searchedComponentNo !== null">ComponentNo、LotNo 為隱藏欄位</span>
      </footer>
    </section>

    <footer class="page-footer">
      <span>查詢來源：DEV 記憶體假資料</span>
      <button type="button" class="close-button" @click="emit('close')">Close (X)</button>
    </footer>
  </main>
</template>

<style scoped>
.comp-material{box-sizing:border-box;display:flex;flex-direction:column;gap:12px;min-height:0;height:100%;padding:14px;background:#f4f3ee;color:#292820;font:inherit;overflow:auto}
.page-heading{display:flex;align-items:flex-start;justify-content:space-between;gap:16px}.eyebrow{margin-bottom:4px;color:#777366;font-size:11px;letter-spacing:.1em}.page-heading h1{margin:0;font-size:20px;font-weight:650}.page-heading p{margin:5px 0 0;color:#777366;font-size:12px}.dev-badge{flex:none;padding:5px 9px;border:1px solid #d3c79c;border-radius:3px;background:#f5efd8;color:#66571f;font-size:11px;font-weight:700}
.query-panel{display:grid;grid-template-columns:auto minmax(220px,360px) auto 1fr;align-items:center;gap:10px;padding:12px;border:1px solid #cbc9bd;background:#fbfaf6}.query-panel label{font-weight:600}.query-panel input{box-sizing:border-box;width:100%;height:32px;padding:5px 8px;border:1px solid #a9a69a;border-radius:2px;background:#fff;color:#292820;font:inherit}.query-panel input:focus{outline:2px solid #8d9d91;outline-offset:1px}.query-button,.close-button{min-height:30px;padding:4px 14px;border:1px solid #3b3930;border-radius:3px;background:#3b3930;color:white;font:inherit;cursor:pointer}.query-button:disabled{opacity:.65;cursor:wait}.query-hint{color:#777366;font-size:11px}
.result-panel{display:flex;flex:1;flex-direction:column;min-height:180px;border:1px solid #cbc9bd;background:#fbfaf6}.result-heading{display:flex;align-items:center;justify-content:space-between;gap:14px;padding:11px 12px;border-bottom:1px solid #d9d7cc}.result-heading h2{margin:0;font-size:14px}.result-heading p{margin:4px 0 0;color:#777366;font-size:11px}.result-meta{display:flex;flex-direction:column;align-items:flex-end;gap:3px;color:#555247;font-size:12px}.result-meta small{color:#858173;font-size:10px}.legacy-note{margin:8px 12px 0;padding:6px 8px;border-left:3px solid #c69b43;background:#fbf5e5;color:#6e5929;font-size:11px}
.table-wrap{flex:1;min-height:130px;overflow:auto}table{width:100%;border-collapse:collapse;font-size:12px;white-space:nowrap}th{position:sticky;top:0;z-index:1;padding:8px 9px;border-right:1px solid #d5d3c7;border-bottom:1px solid #c5c2b5;background:#eeece3;text-align:left;font-weight:650}td{padding:7px 9px;border-right:1px solid #e3e1d8;border-bottom:1px solid #e3e1d8}tbody tr{cursor:pointer}tbody tr:hover{background:#f1f2ea}tbody tr.selected{background:#e4eadf}.op-col{width:80px}.qty-col{width:85px}.table-wrap th:nth-child(2),.table-wrap td:nth-child(2){min-width:120px}.table-wrap th:nth-child(3),.table-wrap td:nth-child(3),.table-wrap th:nth-child(4),.table-wrap td:nth-child(4),.table-wrap th:nth-child(7),.table-wrap td:nth-child(7),.table-wrap th:nth-child(8),.table-wrap td:nth-child(8){min-width:130px}.table-wrap th:nth-child(5),.table-wrap td:nth-child(5),.table-wrap th:nth-child(6),.table-wrap td:nth-child(6){min-width:70px}.qty{text-align:right;font-variant-numeric:tabular-nums}.mono{font-family:ui-monospace,SFMono-Regular,Consolas,monospace;font-size:11px}.empty-state{height:112px;text-align:center;color:#858173;cursor:default}.grid-footer{display:flex;justify-content:space-between;gap:10px;padding:6px 10px;border-top:1px solid #d9d7cc;color:#777366;font-size:10px}.page-footer{display:flex;justify-content:space-between;align-items:center;color:#777366;font-size:11px}.close-button{min-width:80px}
@media(max-width:680px){.query-panel{grid-template-columns:1fr auto}.query-panel label{grid-column:1/-1}.query-panel input{grid-column:1}.query-hint{grid-column:1/-1}.page-heading h1{font-size:17px}.grid-footer{flex-direction:column;gap:3px}}
</style>
