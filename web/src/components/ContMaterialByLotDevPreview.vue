<script setup lang="ts">
import { computed, ref } from 'vue'

type MaterialUsage = {
  rowNo: number
  lotNo: string
  opNo: string
  opName: string
  moNo: string
  materialType: string
  materialNo: string
  materialName: string
  useQty: number
  unitNo: string
  materialLotNo: string
  productNo: string
  productName: string
  lotQty: number
  equipmentNo: string
  description: string
  checkInTime: string
  checkOutTime: string
}
type SortField = Exclude<keyof MaterialUsage, 'rowNo' | 'lotNo'>

const emit = defineEmits<{ close: [] }>()
const exampleLotNo = 'LOT-RT601-001'
const lotNo = ref('')
const searchedLotNo = ref('')
const hasSearched = ref(false)
const loading = ref(false)
const error = ref('')
const sortField = ref<SortField | ''>('')
const sortDirection = ref<'asc' | 'desc'>('asc')

// Fixed fictional rows let the DEV page demonstrate the original Lot-only report without calling a service.
const sampleRows: MaterialUsage[] = [
  { rowNo: 1, lotNo: exampleLotNo, opNo: 'OP210', opName: '晶圓貼合', moNo: 'MO-RT601-021', materialType: 'MAT', materialNo: 'MAT-ADH-010', materialName: '導電膠 A', useQty: 0.82, unitNo: 'G', materialLotNo: 'ADH-26001', productNo: 'PD-1001', productName: '控制器模組 A', lotQty: 24, equipmentNo: 'EQP-LAM-02', description: '主線耗用', checkInTime: '2026-11-24T09:08:12', checkOutTime: '2026-11-24T09:24:35' },
  { rowNo: 2, lotNo: exampleLotNo, opNo: 'OP210', opName: '晶圓貼合', moNo: 'MO-RT601-021', materialType: 'MAT', materialNo: 'MAT-ADH-010', materialName: '導電膠 A', useQty: 0.07, unitNo: 'G', materialLotNo: 'ADH-26002', productNo: 'PD-1001', productName: '控制器模組 A', lotQty: 24, equipmentNo: 'EQP-LAM-02', description: '補料批次', checkInTime: '2026-11-24T09:25:11', checkOutTime: '2026-11-24T09:28:02' },
  { rowNo: 3, lotNo: exampleLotNo, opNo: 'OP340', opName: '電性測試', moNo: 'MO-RT601-021', materialType: 'MAT', materialNo: 'MAT-FILM-006', materialName: '保護膜 6 吋', useQty: 1, unitNo: 'EA', materialLotNo: 'FILM-26114', productNo: 'PD-1001', productName: '控制器模組 A', lotQty: 24, equipmentNo: 'EQP-TEST-07', description: '', checkInTime: '2026-11-24T14:44:05', checkOutTime: '2026-11-24T15:02:19' },
  { rowNo: 4, lotNo: 'LOT-RT601-002', opNo: 'OP115', opName: '晶圓清洗', moNo: 'MO-RT601-022', materialType: 'MAT', materialNo: 'MAT-SOLV-002', materialName: '清洗液 B', useQty: 0.36, unitNo: 'L', materialLotNo: 'SOL-1126', productNo: 'PD-1002', productName: '感測器基板 B', lotQty: 16, equipmentNo: 'EQP-WASH-03', description: 'DEV 範例資料', checkInTime: '2026-11-25T08:15:00', checkOutTime: '2026-11-25T08:29:48' },
]

const visibleRows = computed(() => {
  const rows = sampleRows.filter(row => row.lotNo === searchedLotNo.value)
  if (!sortField.value) return rows
  const field = sortField.value
  return [...rows].sort((left, right) => {
    const a = left[field]
    const b = right[field]
    const result = typeof a === 'number' && typeof b === 'number'
      ? a - b
      : String(a ?? '').localeCompare(String(b ?? ''), 'zh-TW', { numeric: true })
    return sortDirection.value === 'asc' ? result : -result
  })
})

function toggleSort(field: SortField): void {
  if (sortField.value === field) sortDirection.value = sortDirection.value === 'asc' ? 'desc' : 'asc'
  else { sortField.value = field; sortDirection.value = 'asc' }
}
function sortMark(field: SortField): string {
  return sortField.value === field ? (sortDirection.value === 'asc' ? ' ▲' : ' ▼') : ''
}
function formatQty(value: number): string {
  return value.toLocaleString('zh-TW', { maximumFractionDigits: 4 })
}
function formatTime(value: string): string {
  return new Date(value).toLocaleString('zh-TW', { hour12: false })
}
async function query(): Promise<void> {
  const value = lotNo.value.trim().toUpperCase()
  if (!value) {
    error.value = '請輸入 Lot No。'
    hasSearched.value = false
    searchedLotNo.value = ''
    return
  }
  loading.value = true
  error.value = ''
  lotNo.value = value
  await new Promise<void>(resolve => window.setTimeout(resolve, 160))
  searchedLotNo.value = value
  hasSearched.value = true
  loading.value = false
}
</script>

<template>
  <main class="material-lot-page" aria-label="生產批使用物料查詢 DEV 預覽">
    <header class="page-heading">
      <div>
        <span class="eyebrow">RPT / RT601</span>
        <h1>生產批使用物料查詢</h1>
        <p>依 Lot No 查詢製程物料耗用與物料批次。</p>
      </div>
      <span class="dev-badge">DEV 假資料</span>
    </header>

    <form class="query-panel" @submit.prevent="query">
      <label class="lot-field">
        <span>Lot No</span>
        <input v-model="lotNo" type="text" maxlength="50" autocomplete="off" placeholder="輸入完整生產批號" @blur="lotNo = lotNo.trim().toUpperCase()">
      </label>
      <div class="query-actions">
        <span class="sample-hint">範例：{{ exampleLotNo }}</span>
        <button class="query-button" type="submit" :disabled="loading">{{ loading ? '查詢中…' : 'Query' }}</button>
        <button class="close-button" type="button" @click="emit('close')">Close (X)</button>
      </div>
      <p v-if="error" class="error-message" role="alert">{{ error }}</p>
    </form>

    <section class="result-panel" aria-label="物料使用結果">
      <div class="result-toolbar">
        <strong>Material Usage</strong>
        <span v-if="hasSearched">{{ searchedLotNo }} · {{ visibleRows.length }} 筆</span>
        <span v-else>尚未查詢</span>
      </div>
      <div class="table-scroll">
        <table>
          <thead>
            <tr>
              <th><button type="button" @click="toggleSort('opNo')">OPNo{{ sortMark('opNo') }}</button></th>
              <th><button type="button" @click="toggleSort('opName')">OPName{{ sortMark('opName') }}</button></th>
              <th><button type="button" @click="toggleSort('moNo')">MONo{{ sortMark('moNo') }}</button></th>
              <th><button type="button" @click="toggleSort('materialType')">MaterialType{{ sortMark('materialType') }}</button></th>
              <th><button type="button" @click="toggleSort('materialNo')">MaterialNo{{ sortMark('materialNo') }}</button></th>
              <th><button type="button" @click="toggleSort('materialName')">MaterialName{{ sortMark('materialName') }}</button></th>
              <th class="numeric"><button type="button" @click="toggleSort('useQty')">UseQty{{ sortMark('useQty') }}</button></th>
              <th><button type="button" @click="toggleSort('unitNo')">UnitNo{{ sortMark('unitNo') }}</button></th>
              <th><button type="button" @click="toggleSort('materialLotNo')">MaterialLotNo{{ sortMark('materialLotNo') }}</button></th>
              <th><button type="button" @click="toggleSort('productNo')">ProductNo{{ sortMark('productNo') }}</button></th>
              <th><button type="button" @click="toggleSort('productName')">ProductName{{ sortMark('productName') }}</button></th>
              <th class="numeric"><button type="button" @click="toggleSort('lotQty')">LotQty{{ sortMark('lotQty') }}</button></th>
              <th><button type="button" @click="toggleSort('equipmentNo')">EquipmentNo{{ sortMark('equipmentNo') }}</button></th>
              <th><button type="button" @click="toggleSort('description')">Description{{ sortMark('description') }}</button></th>
              <th><button type="button" @click="toggleSort('checkInTime')">CheckInTime{{ sortMark('checkInTime') }}</button></th>
              <th><button type="button" @click="toggleSort('checkOutTime')">CheckOutTime{{ sortMark('checkOutTime') }}</button></th>
            </tr>
          </thead>
          <tbody v-if="visibleRows.length">
            <tr v-for="row in visibleRows" :key="row.rowNo">
              <td>{{ row.opNo }}</td><td>{{ row.opName }}</td><td>{{ row.moNo }}</td><td>{{ row.materialType }}</td>
              <td>{{ row.materialNo }}</td><td>{{ row.materialName }}</td><td class="numeric">{{ formatQty(row.useQty) }}</td>
              <td>{{ row.unitNo }}</td><td>{{ row.materialLotNo }}</td><td>{{ row.productNo }}</td><td>{{ row.productName }}</td>
              <td class="numeric">{{ formatQty(row.lotQty) }}</td><td>{{ row.equipmentNo }}</td><td>{{ row.description }}</td>
              <td>{{ formatTime(row.checkInTime) }}</td><td>{{ formatTime(row.checkOutTime) }}</td>
            </tr>
          </tbody>
          <tbody v-else>
            <tr><td colspan="16" class="empty-state">{{ loading ? '載入中…' : hasSearched ? '查無符合資料。' : '輸入 Lot No 後按 Query。' }}</td></tr>
          </tbody>
        </table>
      </div>
      <footer class="table-footer">固定虛構資料，只在本機記憶體篩選；不呼叫 API、Oracle 或 PostgreSQL。</footer>
    </section>
  </main>
</template>

<style scoped>
.material-lot-page{box-sizing:border-box;display:flex;height:100%;min-height:0;flex-direction:column;gap:10px;overflow:hidden;padding:12px;background:#f3f2ed;color:#292820;font:inherit}.page-heading{display:flex;align-items:flex-start;justify-content:space-between;gap:12px}.eyebrow{color:#777366;font-size:9px;letter-spacing:.1em}.page-heading h1{margin:2px 0;font-size:18px}.page-heading p{margin:0;color:#777366;font-size:10px}.dev-badge{padding:4px 8px;border:1px solid #d6c995;border-radius:12px;background:#f4efdc;color:#705c17;font-size:10px;font-weight:700}.query-panel,.result-panel{border:1px solid #d4d1c5;background:#fffefa;box-shadow:0 1px 2px #26251f0a}.query-panel{display:grid;grid-template-columns:minmax(240px,480px) 1fr;align-items:end;gap:8px 12px;padding:10px}.lot-field{display:grid;gap:4px}.lot-field span{color:#565347;font-size:10px;font-weight:700}.lot-field input{box-sizing:border-box;width:100%;height:30px;padding:4px 8px;border:1px solid #cbc8bc;background:#fff;color:#292820;font:inherit}.query-actions{display:flex;align-items:center;gap:7px}.sample-hint{margin-right:auto;color:#858071;font-size:10px}.query-button,.close-button{min-height:30px;padding:4px 13px;border:1px solid #3c6a54;border-radius:3px;background:#3c6a54;color:#fff;font:inherit;font-weight:700;cursor:pointer}.close-button{border-color:#777366;background:#fffefa;color:#4e4a40}.query-button:disabled{opacity:.6;cursor:wait}.error-message{grid-column:1/-1;margin:0;color:#b03939;font-size:10px}.result-panel{display:flex;min-height:0;flex:1;flex-direction:column}.result-toolbar{display:flex;align-items:center;justify-content:space-between;gap:12px;padding:9px 10px;border-bottom:1px solid #e0ddd2}.result-toolbar strong{font-size:12px}.result-toolbar span{color:#777366;font-size:10px}.table-scroll{min-height:0;flex:1;overflow:auto}table{width:max-content;min-width:100%;border-collapse:collapse;white-space:nowrap;font-size:10px}thead th{position:sticky;top:0;z-index:1;padding:0;background:#eeece4;color:#514e43;text-align:left;font-weight:700}th button{width:100%;height:30px;padding:6px;border:0;background:transparent;color:inherit;text-align:inherit;font:inherit;font-weight:700;cursor:pointer}th button:hover{background:#e5e2d8}td{max-width:260px;padding:7px 6px;border-top:1px solid #eeece6;color:#3b3932;overflow:hidden;text-overflow:ellipsis}.numeric{text-align:right;font-variant-numeric:tabular-nums}.empty-state{height:130px;text-align:center;color:#827d6d}.table-footer{padding:7px 9px;border-top:1px solid #e4e1d7;color:#777366;font-size:9px}@media(max-width:760px){.material-lot-page{overflow:auto}.query-panel{grid-template-columns:1fr}.query-actions{flex-wrap:wrap}.sample-hint{width:100%}.result-panel{min-height:360px}.table-scroll{max-height:65vh}}
</style>
