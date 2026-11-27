<script setup lang="ts">
import { computed, ref } from 'vue'

type RouteStep = {
  itemNo: number
  opNo: string
  opName: string
  opReference: number
  moduleNo: string
  moduleVersion: string
  nodeId: string
  areaNo: string
  defaultAreaNo: string
  stdQueueTime?: number
  stdUnitRunTime?: number
  countOpUnitQty?: number
}
type LotFixture = {
  lotNo: string
  productNo: string
  productVersion: string
  currentQty: number
  currentUnitNo: string
  route: RouteStep[]
}
type PlanRow = {
  itemNo: number
  opNo: string
  opName: string
  queueTime: number | null
  runTime: number | null
  planFinishTime: Date | null
  moduleNo: string
  moduleVersion: string
  areaNo: string
}
type PlanSort = keyof Pick<PlanRow, 'itemNo' | 'opNo' | 'opName' | 'queueTime' | 'runTime' | 'planFinishTime' | 'moduleNo' | 'moduleVersion' | 'areaNo'>

const emit = defineEmits<{ close: [] }>()
const lotNo = ref('')
const activeLot = ref<LotFixture | null>(null)
const rows = ref<PlanRow[]>([])
const queriedAt = ref<Date | null>(null)
const loading = ref(false)
const error = ref('')
const sortBy = ref<PlanSort>('itemNo')
const sortDirection = ref<'asc' | 'desc'>('asc')

const fixtures: LotFixture[] = [
  {
    lotNo: 'LOT-RT607-001',
    productNo: 'PRD-2200',
    productVersion: 'A',
    currentQty: 120,
    currentUnitNo: 'EA',
    route: [
      { itemNo: 1, opNo: 'OP110', opName: '進料檢驗', opReference: 0, moduleNo: 'N/A', moduleVersion: 'N/A', nodeId: 'NODE-110', areaNo: 'AREA-IQC', defaultAreaNo: 'AREA-IQC', stdQueueTime: 15, stdUnitRunTime: 24, countOpUnitQty: 120 },
      { itemNo: 2, opNo: 'OP210', opName: '貼合', opReference: 1, moduleNo: 'MOD-LAM', moduleVersion: '2', nodeId: 'NODE-210', areaNo: '', defaultAreaNo: 'AREA-LAM', stdQueueTime: 30, stdUnitRunTime: 12, countOpUnitQty: 24 },
      { itemNo: 3, opNo: 'OP330', opName: '電性測試', opReference: 0, moduleNo: 'N/A', moduleVersion: 'N/A', nodeId: 'NODE-330', areaNo: '', defaultAreaNo: 'AREA-TEST', stdQueueTime: 10, stdUnitRunTime: 7.5, countOpUnitQty: 30 },
      { itemNo: 4, opNo: 'OP440', opName: '最終檢驗', opReference: 0, moduleNo: 'N/A', moduleVersion: 'N/A', nodeId: 'NODE-440', areaNo: '', defaultAreaNo: 'AREA-FQC' },
    ],
  },
  {
    lotNo: 'LOT-RT607-002',
    productNo: 'PRD-3300',
    productVersion: 'B',
    currentQty: 48,
    currentUnitNo: 'PCS',
    route: [
      { itemNo: 1, opNo: 'OP120', opName: '外觀檢查', opReference: 0, moduleNo: 'N/A', moduleVersion: 'N/A', nodeId: 'NODE-120', areaNo: 'AREA-QC', defaultAreaNo: 'AREA-QC', stdQueueTime: 20, stdUnitRunTime: 6, countOpUnitQty: 12 },
      { itemNo: 2, opNo: 'OP260', opName: '外包加工', opReference: 1, moduleNo: 'MOD-OS-01', moduleVersion: '1', nodeId: 'NODE-260', areaNo: 'AREA-OS', defaultAreaNo: 'AREA-OS', stdQueueTime: 60, stdUnitRunTime: 45, countOpUnitQty: 48 },
      { itemNo: 3, opNo: 'OP390', opName: '出貨檢查', opReference: 0, moduleNo: 'N/A', moduleVersion: 'N/A', nodeId: 'NODE-390', areaNo: '', defaultAreaNo: '' , stdQueueTime: 5, stdUnitRunTime: 2, countOpUnitQty: 0 },
    ],
  },
]

const displayRows = computed(() => {
  const direction = sortDirection.value === 'asc' ? 1 : -1
  return [...rows.value].sort((left, right) => {
    const a = left[sortBy.value]
    const b = right[sortBy.value]
    const difference = a instanceof Date && b instanceof Date
      ? a.getTime() - b.getTime()
      : typeof a === 'number' && typeof b === 'number'
        ? a - b
        : String(a ?? '').localeCompare(String(b ?? ''), 'zh-TW', { numeric: true })
    return difference * direction
  })
})

function buildPlan(fixture: LotFixture, startTime: Date): PlanRow[] {
  let plannedTime = new Date(startTime)
  return fixture.route.map(step => {
    const eligibleReference = step.opReference === 0 || step.opReference === 1
    if (!eligibleReference || step.stdQueueTime === undefined || step.stdUnitRunTime === undefined || step.countOpUnitQty === undefined) {
      return {
        itemNo: step.itemNo, opNo: step.opNo, opName: step.opName, queueTime: null, runTime: null,
        planFinishTime: null, moduleNo: step.moduleNo, moduleVersion: step.moduleVersion, areaNo: '',
      }
    }
    const queueTime = step.stdQueueTime
    const runTime = step.countOpUnitQty <= 0
      ? step.stdUnitRunTime
      : step.stdUnitRunTime * fixture.currentQty / step.countOpUnitQty
    plannedTime = new Date(plannedTime.getTime() + (queueTime + runTime) * 60_000)
    const areaNo = step.areaNo || step.defaultAreaNo || '*'
    return {
      itemNo: step.itemNo, opNo: step.opNo, opName: step.opName, queueTime, runTime,
      planFinishTime: new Date(plannedTime), moduleNo: step.moduleNo, moduleVersion: step.moduleVersion, areaNo,
    }
  })
}

function toggleSort(field: PlanSort): void {
  if (sortBy.value === field) sortDirection.value = sortDirection.value === 'asc' ? 'desc' : 'asc'
  else { sortBy.value = field; sortDirection.value = 'asc' }
}
function sortMark(field: PlanSort): string { return sortBy.value === field ? (sortDirection.value === 'asc' ? ' ▲' : ' ▼') : '' }
function formatNumber(value: number | null): string { return value === null ? '' : value.toLocaleString('zh-TW', { maximumFractionDigits: 2 }) }
function formatDate(value: Date | null): string {
  if (value === null) return ''
  const date = [String(value.getFullYear()), String(value.getMonth() + 1).padStart(2, '0'), String(value.getDate()).padStart(2, '0')].join('/')
  const time = [value.getHours(), value.getMinutes(), value.getSeconds()].map(part => String(part).padStart(2, '0')).join(':')
  return date + ' ' + time
}

async function query(): Promise<void> {
  const value = lotNo.value.trim().toUpperCase()
  if (!value) {
    activeLot.value = null
    rows.value = []
    queriedAt.value = null
    error.value = '請輸入 Lot No。'
    return
  }
  lotNo.value = value
  activeLot.value = null
  rows.value = []
  queriedAt.value = null
  error.value = ''
  loading.value = true
  await new Promise<void>(resolve => window.setTimeout(resolve, 120))
  const fixture = fixtures.find(item => item.lotNo === value)
  if (!fixture) {
    error.value = '找不到此 Lot No；DEV 範例為 LOT-RT607-001 或 LOT-RT607-002。'
    loading.value = false
    return
  }
  const startTime = new Date()
  activeLot.value = fixture
  queriedAt.value = startTime
  rows.value = buildPlan(fixture, startTime)
  sortBy.value = 'itemNo'
  sortDirection.value = 'asc'
  loading.value = false
}
</script>

<template>
  <main class="plan-finish-page" aria-label="預計完工查詢 DEV 預覽">
    <header class="page-heading">
      <div><span class="eyebrow">RPT / RT607</span><h1>預計完工查詢</h1><p>依生產批目前站別與產品／模組標準工時累計預估。</p></div>
      <span class="dev-badge">DEV 假資料</span>
    </header>

    <form class="query-panel" @submit.prevent="query">
      <label for="rt607-lot-no">Lot No</label>
      <input id="rt607-lot-no" v-model="lotNo" maxlength="50" autocomplete="off" placeholder="LOT-RT607-001" @blur="lotNo = lotNo.trim().toUpperCase()">
      <div class="form-actions"><button class="primary" type="submit" :disabled="loading">{{ loading ? '查詢中…' : 'Query' }}</button><button type="button" @click="emit('close')">Close (X)</button></div>
      <p v-if="error" class="error" role="alert">{{ error }}</p>
    </form>

    <section class="lot-info" aria-label="Lot Information">
      <header>Lot Information</header>
      <dl>
        <div><dt>Product No</dt><dd>{{ activeLot?.productNo ?? '' }}</dd></div>
        <div><dt>Product Version</dt><dd>{{ activeLot?.productVersion ?? '' }}</dd></div>
        <div><dt>Cur Qty</dt><dd class="numeric">{{ activeLot ? formatNumber(activeLot.currentQty) : '' }}</dd></div>
        <div><dt>Cur Unit No</dt><dd>{{ activeLot?.currentUnitNo ?? '' }}</dd></div>
      </dl>
    </section>

    <section class="result-panel" aria-label="Plan Finish Time">
      <header><strong>Plan Finish Time</strong><span v-if="queriedAt">估算起點 {{ formatDate(queriedAt) }}</span></header>
      <div class="table-scroll"><table>
        <thead><tr>
          <th><button type="button" @click="toggleSort('itemNo')">ItemNo{{ sortMark('itemNo') }}</button></th>
          <th><button type="button" @click="toggleSort('opNo')">OP No{{ sortMark('opNo') }}</button></th>
          <th><button type="button" @click="toggleSort('opName')">OP Name{{ sortMark('opName') }}</button></th>
          <th class="numeric"><button type="button" @click="toggleSort('queueTime')">Queue Time{{ sortMark('queueTime') }}</button></th>
          <th class="numeric"><button type="button" @click="toggleSort('runTime')">Run Time{{ sortMark('runTime') }}</button></th>
          <th><button type="button" @click="toggleSort('planFinishTime')">Plan Finish Time{{ sortMark('planFinishTime') }}</button></th>
          <th><button type="button" @click="toggleSort('moduleNo')">Module No{{ sortMark('moduleNo') }}</button></th>
          <th><button type="button" @click="toggleSort('moduleVersion')">Module Version{{ sortMark('moduleVersion') }}</button></th>
          <th><button type="button" @click="toggleSort('areaNo')">Area No{{ sortMark('areaNo') }}</button></th>
        </tr></thead>
        <tbody v-if="displayRows.length"><tr v-for="row in displayRows" :key="row.itemNo">
          <td>{{ row.itemNo }}</td><td>{{ row.opNo }}</td><td>{{ row.opName }}</td>
          <td class="numeric">{{ formatNumber(row.queueTime) }}</td><td class="numeric">{{ formatNumber(row.runTime) }}</td>
          <td>{{ formatDate(row.planFinishTime) }}</td><td>{{ row.moduleNo }}</td><td>{{ row.moduleVersion }}</td><td>{{ row.areaNo }}</td>
        </tr></tbody>
        <tbody v-else><tr><td colspan="9" class="empty-state">{{ loading ? '載入中…' : activeLot ? '沒有可顯示的流程站別。' : '輸入 Lot No 後按 Query。' }}</td></tr></tbody>
      </table></div>
    </section>
    <footer class="page-footer">固定虛構資料，只在本機記憶體計算；不呼叫 API、Oracle 或 PostgreSQL。時間單位沿用來源的分鐘。</footer>
  </main>
</template>

<style scoped>
.plan-finish-page{box-sizing:border-box;display:flex;height:100%;min-height:0;flex-direction:column;gap:8px;overflow:hidden;padding:10px;background:#f3f2ed;color:#292820;font:inherit}.page-heading{display:flex;align-items:flex-start;justify-content:space-between;gap:12px}.eyebrow{color:#777366;font-size:9px;letter-spacing:.1em}.page-heading h1{margin:2px 0;font-size:17px}.page-heading p{margin:0;color:#777366;font-size:10px}.dev-badge{padding:4px 8px;border:1px solid #d6c995;border-radius:12px;background:#f4efdc;color:#705c17;font-size:10px;font-weight:700}.query-panel{display:grid;grid-template-columns:60px minmax(160px,270px) auto;align-items:center;gap:8px;padding:8px 10px;border:1px solid #d4d1c5;background:#fffefa}.query-panel>label{font-weight:700;font-size:10px}.query-panel>input{box-sizing:border-box;height:27px;padding:4px 7px;border:1px solid #c9c6bb;border-radius:2px;font:inherit;font-size:10px}.form-actions{display:flex;justify-content:flex-end;gap:6px}.form-actions button{min-height:27px;padding:4px 11px;border:1px solid #c9c6bb;border-radius:2px;background:#f3f2ed;color:#343329;font:inherit;font-size:10px;cursor:pointer}.form-actions .primary{border-color:#364d46;background:#364d46;color:#fff}.form-actions button:disabled{opacity:.6;cursor:wait}.error{grid-column:1/-1;margin:0;color:#a32121;font-size:10px}.lot-info{border:1px solid #d4d1c5;background:#fffefa}.lot-info>header{padding:5px 9px;border-bottom:1px solid #e6e4dc;font-size:10px;font-weight:700}.lot-info dl{display:grid;grid-template-columns:minmax(180px,2fr) minmax(100px,.7fr) minmax(110px,1fr) minmax(80px,.7fr);gap:8px;margin:0;padding:8px 10px}.lot-info dl>div{display:flex;align-items:center;gap:8px;min-width:0}.lot-info dt{color:#777366;font-size:10px;white-space:nowrap}.lot-info dd{flex:1;min-width:0;margin:0;padding:3px 6px;border:1px solid #e0ded6;background:#f6f5f0;font-size:10px;overflow:hidden;text-overflow:ellipsis}.numeric{text-align:right;font-variant-numeric:tabular-nums}.result-panel{display:flex;min-height:0;flex:1;flex-direction:column;overflow:hidden;border:1px solid #d4d1c5;background:#fffefa}.result-panel>header{display:flex;align-items:center;justify-content:space-between;padding:6px 9px;border-bottom:1px solid #e6e4dc;font-size:10px}.result-panel>header span{color:#777366;font-size:9px}.table-scroll{min-height:0;flex:1;overflow:auto}table{width:100%;border-collapse:collapse;font-size:10px;white-space:nowrap}th{position:sticky;top:0;z-index:1;background:#ecebe5;text-align:left}th,td{padding:5px 7px;border-bottom:1px solid #e6e4dc}th button{padding:0;border:0;background:none;color:inherit;font:inherit;font-weight:700;text-align:left;cursor:pointer}.empty-state{padding:18px;text-align:center;color:#777366}.page-footer{color:#777366;font-size:9px}@media(max-width:720px){.query-panel{grid-template-columns:60px minmax(120px,1fr)}.form-actions{grid-column:1/-1}.lot-info dl{grid-template-columns:1fr 1fr}.lot-info dl>div{flex-wrap:wrap;gap:3px}.result-panel{min-height:180px}}
</style>
