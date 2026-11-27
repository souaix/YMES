<script setup lang="ts">
import { computed, ref } from 'vue'

type QueryMode = 'lot' | 'lotOp'
type ResourceRow = { id: string; opNo: string; opName: string; resClass: number; resType: string; resValue: number; stdValue: number; inputQty: number }
type MaterialRow = { id: string; opNo: string; opName: string; materialLevel: number; materialNo: string; materialLotNo: string; resValue: number; stdValue: number; unitNo: string }
type ResourceSort = keyof Pick<ResourceRow, 'opNo' | 'resClass' | 'resType' | 'resValue' | 'stdValue' | 'inputQty'>
type MaterialSort = keyof Pick<MaterialRow, 'opNo' | 'materialLevel' | 'materialNo' | 'materialLotNo' | 'resValue' | 'stdValue' | 'unitNo'>

const emit = defineEmits<{ close: [] }>()
const lotNo = ref('')
const queryMode = ref<QueryMode>('lot')
const activeLotNo = ref('')
const activeMode = ref<QueryMode>('lot')
const hasSearched = ref(false)
const loading = ref(false)
const error = ref('')
const resourceSort = ref<ResourceSort>('resClass')
const resourceDirection = ref<'asc' | 'desc'>('asc')
const materialSort = ref<MaterialSort>('materialLevel')
const materialDirection = ref<'asc' | 'desc'>('asc')

const resourceSource: Array<ResourceRow & { lotNo: string }> = [
  { id: 'R1', lotNo: 'LOT-RT606-001', opNo: 'OP210', opName: '貼合', resClass: 0, resType: 'EMP-018', resValue: 1.25, stdValue: 1.1, inputQty: 24 },
  { id: 'R2', lotNo: 'LOT-RT606-001', opNo: 'OP210', opName: '貼合', resClass: 1, resType: 'EQP-LAM-02', resValue: 0.8, stdValue: 0.75, inputQty: 24 },
  { id: 'R3', lotNo: 'LOT-RT606-001', opNo: 'OP210', opName: '貼合', resClass: 2, resType: 'OS-PLATING-01', resValue: 24, stdValue: 24, inputQty: 24 },
  { id: 'R4', lotNo: 'LOT-RT606-001', opNo: 'OP340', opName: '電性測試', resClass: 0, resType: 'EMP-021', resValue: 0.92, stdValue: 0.85, inputQty: 24 },
  { id: 'R5', lotNo: 'LOT-RT606-001', opNo: 'OP340', opName: '電性測試', resClass: 1, resType: 'EQP-TEST-07', resValue: 0.67, stdValue: 0.72, inputQty: 24 },
  { id: 'R6', lotNo: 'LOT-RT606-002', opNo: 'OP210', opName: '貼合', resClass: 0, resType: 'EMP-014', resValue: 1.4, stdValue: 1.2, inputQty: 36 },
  { id: 'R7', lotNo: 'LOT-RT606-002', opNo: 'OP210', opName: '貼合', resClass: 1, resType: 'EQP-LAM-01', resValue: 0.93, stdValue: 0.8, inputQty: 36 },
]
const materialSource: Array<MaterialRow & { lotNo: string }> = [
  { id: 'M1', lotNo: 'LOT-RT606-001', opNo: 'OP210', opName: '貼合', materialLevel: 0, materialNo: 'MAT-ADH-010', materialLotNo: 'ADH-26001', resValue: 19.68, stdValue: 18, unitNo: 'G' },
  { id: 'M2', lotNo: 'LOT-RT606-001', opNo: 'OP210', opName: '貼合', materialLevel: 0, materialNo: 'MAT-FILM-006', materialLotNo: 'FILM-26114', resValue: 24, stdValue: 24, unitNo: 'EA' },
  { id: 'M3', lotNo: 'LOT-RT606-001', opNo: 'OP210', opName: '貼合', materialLevel: 1, materialNo: 'PD-1001', materialLotNo: 'SUB-26041-01', resValue: 24, stdValue: 24, unitNo: 'EA' },
  { id: 'M4', lotNo: 'LOT-RT606-001', opNo: 'OP340', opName: '電性測試', materialLevel: 0, materialNo: 'MAT-SOLV-002', materialLotNo: 'SOL-1126', resValue: 0.36, stdValue: 0.32, unitNo: 'L' },
  { id: 'M5', lotNo: 'LOT-RT606-001', opNo: 'OP340', opName: '電性測試', materialLevel: 1, materialNo: 'PD-1001', materialLotNo: 'SUB-26041-01', resValue: 24, stdValue: 24, unitNo: 'EA' },
  { id: 'M6', lotNo: 'LOT-RT606-002', opNo: 'OP210', opName: '貼合', materialLevel: 0, materialNo: 'MAT-ADH-010', materialLotNo: 'ADH-26002', resValue: 29.52, stdValue: 27, unitNo: 'G' },
  { id: 'M7', lotNo: 'LOT-RT606-002', opNo: 'OP210', opName: '貼合', materialLevel: 1, materialNo: 'PD-1002', materialLotNo: 'SUB-26058-01', resValue: 36, stdValue: 36, unitNo: 'EA' },
]

function aggregateResources(rows: ResourceRow[]): ResourceRow[] {
  const groups = new Map<string, ResourceRow>()
  for (const row of rows) {
    const key = `${row.resClass}|${row.resType}`
    const current = groups.get(key)
    if (current) { current.resValue += row.resValue; current.stdValue += row.stdValue; current.inputQty += row.inputQty }
    else groups.set(key, { ...row, id: key, opNo: '', opName: '' })
  }
  return [...groups.values()]
}
function aggregateMaterials(rows: MaterialRow[]): MaterialRow[] {
  const groups = new Map<string, MaterialRow>()
  for (const row of rows) {
    const key = `${row.materialLevel}|${row.materialNo}|${row.materialLotNo}|${row.unitNo}`
    const current = groups.get(key)
    if (current) { current.resValue += row.resValue; current.stdValue += row.stdValue }
    else groups.set(key, { ...row, id: key, opNo: '', opName: '' })
  }
  return [...groups.values()]
}
const resourceRows = computed(() => {
  if (!hasSearched.value) return []
  const rows = resourceSource.filter(row => row.lotNo === activeLotNo.value)
  const source = activeMode.value === 'lotOp' ? rows : aggregateResources(rows)
  return [...source].sort((a, b) => {
    const fields: ResourceSort[] = activeMode.value === 'lotOp' ? ['opNo', resourceSort.value] : [resourceSort.value]
    return compareFields(a, b, fields, resourceDirection.value)
  })
})
const materialRows = computed(() => {
  if (!hasSearched.value) return []
  const rows = materialSource.filter(row => row.lotNo === activeLotNo.value)
  const source = activeMode.value === 'lotOp' ? rows : aggregateMaterials(rows)
  return [...source].sort((a, b) => {
    const fields: MaterialSort[] = activeMode.value === 'lotOp' ? ['opNo', materialSort.value] : [materialSort.value]
    return compareFields(a, b, fields, materialDirection.value)
  })
})
const classTotals = computed(() => [0, 1, 2].map(resClass => ({
  resClass,
  total: resourceRows.value.filter(row => row.resClass === resClass).reduce((sum, row) => sum + row.resValue, 0),
})).filter(item => resourceRows.value.some(row => row.resClass === item.resClass)))

function compareFields<T extends object>(a: T, b: T, fields: string[], direction: 'asc' | 'desc'): number {
  for (const field of fields) {
    const left = Reflect.get(a, field) as unknown
    const right = Reflect.get(b, field) as unknown
    const difference = typeof left === 'number' && typeof right === 'number'
      ? left - right
      : String(left ?? '').localeCompare(String(right ?? ''), 'zh-TW', { numeric: true })
    if (difference) return direction === 'asc' ? difference : -difference
  }
  return 0
}
function toggleResourceSort(field: ResourceSort): void {
  if (resourceSort.value === field) resourceDirection.value = resourceDirection.value === 'asc' ? 'desc' : 'asc'
  else { resourceSort.value = field; resourceDirection.value = 'asc' }
}
function toggleMaterialSort(field: MaterialSort): void {
  if (materialSort.value === field) materialDirection.value = materialDirection.value === 'asc' ? 'desc' : 'asc'
  else { materialSort.value = field; materialDirection.value = 'asc' }
}
function resourceMark(field: ResourceSort): string { return resourceSort.value === field ? (resourceDirection.value === 'asc' ? ' ▲' : ' ▼') : '' }
function materialMark(field: MaterialSort): string { return materialSort.value === field ? (materialDirection.value === 'asc' ? ' ▲' : ' ▼') : '' }
function resClassName(value: number): string { return ['EMP', 'EQP', 'OS'][value] ?? String(value) }
function materialLevelName(value: number): string { return value === 0 ? 'Material' : 'Product' }
function formatNumber(value: number, digits = 6): string { return value.toLocaleString('zh-TW', { maximumFractionDigits: digits }) }
async function query(): Promise<void> {
  const value = lotNo.value.trim().toUpperCase()
  if (!value) { error.value = '請輸入 Lot No。'; hasSearched.value = false; return }
  loading.value = true
  error.value = ''
  await new Promise<void>(resolve => window.setTimeout(resolve, 150))
  activeLotNo.value = value
  activeMode.value = queryMode.value
  resourceSort.value = queryMode.value === 'lotOp' ? 'opNo' : 'resClass'
  resourceDirection.value = 'asc'
  materialSort.value = queryMode.value === 'lotOp' ? 'opNo' : 'materialLevel'
  materialDirection.value = 'asc'
  lotNo.value = value
  hasSearched.value = true
  loading.value = false
}
</script>

<template>
  <main class="lot-resource-page" aria-label="生產批資源檢視 DEV 預覽">
    <header class="page-heading"><div><span class="eyebrow">RPT / RT606</span><h1>生產批資源檢視</h1><p>依 Lot 檢視勞務、設備、外包與物料耗用。</p></div><span class="dev-badge">DEV 假資料</span></header>

    <form class="query-panel" @submit.prevent="query">
      <label class="lot-label" for="rt606-lot-no">Lot No</label>
      <input id="rt606-lot-no" v-model="lotNo" type="text" maxlength="50" autocomplete="off" placeholder="例如 LOT-RT606-001" @blur="lotNo = lotNo.trim().toUpperCase()">
      <fieldset><legend>Calculate Type</legend><label><input v-model="queryMode" type="radio" value="lot"> by Lot</label><label><input v-model="queryMode" type="radio" value="lotOp"> by Lot + OP</label></fieldset>
      <div class="form-actions"><button class="primary" type="submit" :disabled="loading">{{ loading ? '查詢中…' : 'Query' }}</button><button type="button" @click="emit('close')">Close (X)</button></div>
      <p v-if="error" class="error" role="alert">{{ error }}</p>
    </form>

    <div class="result-panels">
      <section class="result-panel resource-panel" aria-label="Resource">
        <header><strong>Resource</strong><span>{{ activeMode === 'lotOp' ? 'by Lot + OP' : 'by Lot' }}</span></header>
        <div class="table-scroll"><table><thead><tr>
          <th v-if="activeMode === 'lotOp'"><button type="button" @click="toggleResourceSort('opNo')">OPNo{{ resourceMark('opNo') }}</button></th>
          <th v-if="activeMode === 'lotOp'">OPName</th>
          <th><button type="button" @click="toggleResourceSort('resClass')">RESClass{{ resourceMark('resClass') }}</button></th>
          <th><button type="button" @click="toggleResourceSort('resType')">RESType{{ resourceMark('resType') }}</button></th>
          <th class="numeric"><button type="button" @click="toggleResourceSort('resValue')">RESValue{{ resourceMark('resValue') }}</button></th>
          <th class="numeric"><button type="button" @click="toggleResourceSort('stdValue')">STDValue{{ resourceMark('stdValue') }}</button></th>
          <th class="numeric"><button type="button" @click="toggleResourceSort('inputQty')">InputQty{{ resourceMark('inputQty') }}</button></th>
        </tr></thead><tbody v-if="resourceRows.length"><tr v-for="row in resourceRows" :key="row.id">
          <td v-if="activeMode === 'lotOp'">{{ row.opNo }}</td><td v-if="activeMode === 'lotOp'">{{ row.opName }}</td><td><span class="class-tag" :class="`class-${row.resClass}`">{{ resClassName(row.resClass) }}</span></td><td>{{ row.resType }}</td><td class="numeric">{{ formatNumber(row.resValue, 2) }}</td><td class="numeric">{{ formatNumber(row.stdValue, 2) }}</td><td class="numeric">{{ formatNumber(row.inputQty, 4) }}</td>
        </tr></tbody><tbody v-else><tr><td :colspan="activeMode === 'lotOp' ? 7 : 5" class="empty-state">{{ loading ? '載入中…' : hasSearched ? '查無 Resource 資料。' : '輸入 Lot No 後按 Query。' }}</td></tr></tbody></table></div>
        <footer class="summary-footer"><span v-for="item in classTotals" :key="item.resClass">{{ resClassName(item.resClass) }} Sum = {{ formatNumber(item.total, 2) }}</span></footer>
      </section>

      <section class="result-panel material-panel" aria-label="Material">
        <header><strong>Material</strong><span>{{ activeMode === 'lotOp' ? 'by Lot + OP' : 'by Lot' }}</span></header>
        <div class="table-scroll"><table><thead><tr>
          <th v-if="activeMode === 'lotOp'"><button type="button" @click="toggleMaterialSort('opNo')">OPNo{{ materialMark('opNo') }}</button></th>
          <th v-if="activeMode === 'lotOp'">OPName</th>
          <th><button type="button" @click="toggleMaterialSort('materialLevel')">MaterialLevel{{ materialMark('materialLevel') }}</button></th>
          <th><button type="button" @click="toggleMaterialSort('materialNo')">MaterialNo{{ materialMark('materialNo') }}</button></th>
          <th><button type="button" @click="toggleMaterialSort('materialLotNo')">MaterialLotNo{{ materialMark('materialLotNo') }}</button></th>
          <th class="numeric"><button type="button" @click="toggleMaterialSort('resValue')">RESValue{{ materialMark('resValue') }}</button></th>
          <th class="numeric"><button type="button" @click="toggleMaterialSort('stdValue')">STDValue{{ materialMark('stdValue') }}</button></th>
          <th><button type="button" @click="toggleMaterialSort('unitNo')">UnitNo{{ materialMark('unitNo') }}</button></th>
        </tr></thead><tbody v-if="materialRows.length"><tr v-for="row in materialRows" :key="row.id">
          <td v-if="activeMode === 'lotOp'">{{ row.opNo }}</td><td v-if="activeMode === 'lotOp'">{{ row.opName }}</td><td>{{ materialLevelName(row.materialLevel) }}</td><td>{{ row.materialNo }}</td><td>{{ row.materialLotNo }}</td><td class="numeric">{{ formatNumber(row.resValue) }}</td><td class="numeric">{{ formatNumber(row.stdValue) }}</td><td>{{ row.unitNo }}</td>
        </tr></tbody><tbody v-else><tr><td :colspan="activeMode === 'lotOp' ? 8 : 6" class="empty-state">{{ loading ? '載入中…' : hasSearched ? '查無 Material 資料。' : '等待查詢。' }}</td></tr></tbody></table></div>
      </section>
    </div>
    <footer class="page-footer">固定虛構資料，只在本機記憶體篩選；不呼叫 API、Oracle 或 PostgreSQL。</footer>
  </main>
</template>

<style scoped>
.lot-resource-page{box-sizing:border-box;display:flex;height:100%;min-height:0;flex-direction:column;gap:8px;overflow:hidden;padding:10px;background:#f3f2ed;color:#292820;font:inherit}.page-heading{display:flex;align-items:flex-start;justify-content:space-between;gap:12px}.eyebrow{color:#777366;font-size:9px;letter-spacing:.1em}.page-heading h1{margin:2px 0;font-size:17px}.page-heading p{margin:0;color:#777366;font-size:10px}.dev-badge{padding:4px 8px;border:1px solid #d6c995;border-radius:12px;background:#f4efdc;color:#705c17;font-size:10px;font-weight:700}.query-panel{display:grid;grid-template-columns:60px minmax(160px,250px) 1fr auto;align-items:center;gap:8px;padding:8px 10px;border:1px solid #d4d1c5;background:#fffefa}.lot-label{font-weight:700;font-size:10px}.query-panel>input{box-sizing:border-box;height:27px;padding:4px 7px;border:1px solid #c9c6bb;border-radius:2px;font:inherit;font-size:10px}.query-panel fieldset{display:flex;align-items:center;gap:12px;margin:0;padding:0;border:0;font-size:10px}.query-panel legend{float:left;margin-right:8px;font-weight:700}.query-panel fieldset label{display:flex;align-items:center;gap:4px}.query-panel input[type=radio]{accent-color:#344b45}.form-actions{display:flex;gap:6px}.form-actions button{min-height:27px;padding:4px 11px;border:1px solid #c9c6bb;border-radius:2px;background:#f3f2ed;color:#343329;font:inherit;font-size:10px;cursor:pointer}.form-actions .primary{border-color:#364d46;background:#364d46;color:#fff}.form-actions button:disabled{opacity:.6;cursor:wait}.error{grid-column:1/-1;margin:0;color:#a32121;font-size:10px}.result-panels{display:grid;grid-template-rows:minmax(0,45fr) minmax(0,55fr);gap:8px;min-height:0;flex:1}.result-panel{display:flex;min-height:0;flex-direction:column;overflow:hidden;border:1px solid #d4d1c5;background:#fffefa}.result-panel>header{display:flex;align-items:center;justify-content:space-between;padding:6px 9px;border-bottom:1px solid #e6e4dc;font-size:10px}.result-panel>header span{color:#777366;font-size:9px}.table-scroll{min-height:0;flex:1;overflow:auto}table{width:100%;border-collapse:collapse;font-size:10px;white-space:nowrap}th{position:sticky;top:0;z-index:1;background:#ecebe5;text-align:left}th,td{padding:4px 7px;border-bottom:1px solid #e6e4dc}th button{padding:0;border:0;background:none;color:inherit;font:inherit;font-weight:700;text-align:left;cursor:pointer}.numeric{text-align:right;font-variant-numeric:tabular-nums}.class-tag{display:inline-block;min-width:27px;padding:2px 4px;border-radius:2px;text-align:center;font-weight:700}.class-0{background:#e9f0e9;color:#375c42}.class-1{background:#e7edf4;color:#365a78}.class-2{background:#f2e9df;color:#835322}.empty-state{padding:16px;text-align:center;color:#777366}.summary-footer{display:flex;justify-content:flex-end;gap:14px;padding:5px 9px;border-top:1px solid #e6e4dc;color:#615a43;font-size:9px;font-weight:700}.page-footer{color:#777366;font-size:9px}@media(max-width:720px){.query-panel{grid-template-columns:60px minmax(120px,1fr)}.query-panel fieldset{grid-column:1/-1;flex-wrap:wrap}.form-actions{grid-column:1/-1;justify-content:flex-end}.result-panels{grid-template-rows:minmax(0,1fr) minmax(0,1.2fr)}.summary-footer{gap:7px;flex-wrap:wrap}}
</style>
