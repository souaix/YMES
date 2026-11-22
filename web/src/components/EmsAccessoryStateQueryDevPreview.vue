<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)

interface AccessoryReason { reasonNo: string; reasonName: string }
interface AccessoryStateRow {
  accSerialNo: string
  accessoryType: string
  accessoryNo: string
  accessoryVersion: string
  accessoryState: number
  stateName: string
  startTime: number
  userNo: string
  description: string
  applyTime: number | null
  accumulateQty: number
  accessorySpareQty: number
  repairCycle: number | null
  accessoryRepairQty: number
  accessoryTotalUsedQty: number
  timeCycleStart: number | null
  reasons: AccessoryReason[]
}
type PickerKind = 'type' | 'accessory' | null

function at(daysAgo: number, hour: number, minute = 0): number {
  const date = new Date()
  date.setDate(date.getDate() - daysAgo)
  date.setHours(hour, minute, 0, 0)
  return date.getTime()
}

function displayTime(value: number | null): string {
  return value === null ? '—' : new Intl.DateTimeFormat('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false }).format(value)
}

function formatQuantity(value: number): string {
  return new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 4 }).format(value)
}

// 狀態、配件、原因與數量都是 DEV fixture，不來自任何 MES 系統或資料庫。
const accessoryRows: AccessoryStateRow[] = [
  { accSerialNo: 'DEV-ACC-CUR-001', accessoryType: 'PROBE', accessoryNo: 'PC-PRB-01-001', accessoryVersion: 'A01', accessoryState: 2, stateName: '使用中', startTime: at(0, 8, 10), userNo: '11019', description: '晶圓測試機上機使用', applyTime: at(0, 8, 8), accumulateQty: 128.5, accessorySpareQty: 0, repairCycle: 500, accessoryRepairQty: 14.25, accessoryTotalUsedQty: 428.5, timeCycleStart: at(0, 8, 10), reasons: [{ reasonNo: 'R-001', reasonName: '生產使用' }] },
  { accSerialNo: 'DEV-ACC-CUR-002', accessoryType: 'PROBE', accessoryNo: 'PC-PRB-02-004', accessoryVersion: 'A02', accessoryState: 3, stateName: '維修中', startTime: at(0, 7, 35), userNo: '10428', description: '探針卡接觸檢查', applyTime: null, accumulateQty: 31.5, accessorySpareQty: 0, repairCycle: 300, accessoryRepairQty: 31.5, accessoryTotalUsedQty: 331.5, timeCycleStart: at(0, 7, 35), reasons: [{ reasonNo: 'R-022', reasonName: '接觸異常檢查' }, { reasonNo: 'R-023', reasonName: '探針保養' }] },
  { accSerialNo: 'DEV-ACC-CUR-003', accessoryType: 'PROBE', accessoryNo: 'PC-PRB-03-002', accessoryVersion: 'A01', accessoryState: 1, stateName: '庫存', startTime: at(1, 15, 0), userNo: '10211', description: '配件庫點收完成', applyTime: at(1, 14, 58), accumulateQty: 246, accessorySpareQty: 0, repairCycle: 600, accessoryRepairQty: 0, accessoryTotalUsedQty: 246, timeCycleStart: null, reasons: [] },
  { accSerialNo: 'DEV-ACC-CUR-004', accessoryType: 'MASK', accessoryNo: 'RET-ETCH-01-003', accessoryVersion: 'B01', accessoryState: 2, stateName: '使用中', startTime: at(0, 6, 55), userNo: '10806', description: '蝕刻製程批次使用', applyTime: at(0, 6, 52), accumulateQty: 84.25, accessorySpareQty: 0, repairCycle: 250, accessoryRepairQty: 12, accessoryTotalUsedQty: 334.25, timeCycleStart: at(0, 6, 55), reasons: [{ reasonNo: 'R-031', reasonName: '製程領用' }] },
  { accSerialNo: 'DEV-ACC-CUR-005', accessoryType: 'MASK', accessoryNo: 'RET-ETCH-01-003', accessoryVersion: 'A01', accessoryState: 1, stateName: '庫存', startTime: at(9, 14, 20), userNo: '10211', description: '舊版備品仍有現況列', applyTime: null, accumulateQty: 0, accessorySpareQty: 0, repairCycle: 250, accessoryRepairQty: 0, accessoryTotalUsedQty: 0, timeCycleStart: null, reasons: [] },
  { accSerialNo: 'DEV-ACC-CUR-006', accessoryType: 'MASK', accessoryNo: 'RET-ETCH-02-001', accessoryVersion: 'A02', accessoryState: 1, stateName: '庫存', startTime: at(2, 17, 15), userNo: '10211', description: '完成清潔後歸還', applyTime: at(2, 17, 15), accumulateQty: 195.75, accessorySpareQty: 0, repairCycle: -1, accessoryRepairQty: 0, accessoryTotalUsedQty: 195.75, timeCycleStart: null, reasons: [{ reasonNo: 'R-014', reasonName: '清潔完成' }] },
  { accSerialNo: 'DEV-ACC-CUR-007', accessoryType: 'MOLD', accessoryNo: 'JIG-MOL-01-009', accessoryVersion: 'A03', accessoryState: 5, stateName: '待驗收', startTime: at(1, 16, 20), userNo: '10211', description: '保養後等待驗收', applyTime: null, accumulateQty: 18, accessorySpareQty: 0, repairCycle: null, accessoryRepairQty: 4, accessoryTotalUsedQty: 118, timeCycleStart: null, reasons: [{ reasonNo: 'R-042', reasonName: '保養完成' }] },
  { accSerialNo: 'DEV-ACC-CUR-008', accessoryType: 'MOLD', accessoryNo: 'JIG-MOL-02-001', accessoryVersion: 'A01', accessoryState: 2, stateName: '使用中', startTime: at(2, 9, 30), userNo: '10806', description: '成型機生產使用', applyTime: at(2, 9, 28), accumulateQty: 72, accessorySpareQty: 0, repairCycle: 400, accessoryRepairQty: 22, accessoryTotalUsedQty: 872, timeCycleStart: at(2, 9, 30), reasons: [{ reasonNo: 'R-001', reasonName: '生產使用' }] },
  { accSerialNo: 'DEV-ACC-CUR-009', accessoryType: 'SEAL', accessoryNo: 'SL-OVN-02-005', accessoryVersion: 'C02', accessoryState: 4, stateName: '報廢', startTime: at(2, 14, 45), userNo: '10428', description: '壽命到期，工程確認', applyTime: null, accumulateQty: 500, accessorySpareQty: 0, repairCycle: 500, accessoryRepairQty: 0, accessoryTotalUsedQty: 500, timeCycleStart: at(0, 14, 45), reasons: [{ reasonNo: 'R-058', reasonName: '壽命到期' }, { reasonNo: 'R-060', reasonName: '工程確認報廢' }] },
  { accSerialNo: 'DEV-ACC-CUR-010', accessoryType: 'SEAL', accessoryNo: 'SL-OVN-03-001', accessoryVersion: 'A01', accessoryState: 1, stateName: '庫存', startTime: at(3, 13, 5), userNo: '11019', description: '新件驗收入庫', applyTime: at(3, 13, 0), accumulateQty: 0, accessorySpareQty: 0, repairCycle: null, accessoryRepairQty: 0, accessoryTotalUsedQty: 0, timeCycleStart: null, reasons: [] },
]

const criteria = reactive({ useType: false, accessoryType: '', useAccessory: false, accessoryNo: '' })
const submittedCriteria = ref<{ useType: boolean; accessoryType: string; useAccessory: boolean; accessoryNo: string } | null>(null)
const expandedRows = ref<Set<string>>(new Set())
const pickerKind = ref<PickerKind>(null)
const message = ref('請選擇查詢條件並按 Query；未指定條件時會顯示所有 DEV 假資料。')

const typeOptions = [...new Set(accessoryRows.map((row) => row.accessoryType))].sort().map((code) => ({ code, name: `${code} 配件` }))
const accessoryOptions = computed(() => [...new Set(accessoryRows
  .filter((row) => !criteria.accessoryType || row.accessoryType === criteria.accessoryType)
  .map((row) => row.accessoryNo))].sort().map((code) => ({ code, name: code })))
const pickerOptions = computed(() => pickerKind.value === 'type' ? typeOptions : pickerKind.value === 'accessory' ? accessoryOptions.value : [])
const pickerTitle = computed(() => pickerKind.value === 'type' ? '選擇配件類型' : '選擇配件編號')
const visibleRows = computed(() => {
  const search = submittedCriteria.value
  if (!search) return []
  return accessoryRows
    .filter((row) => (!search.useType || !search.accessoryType || row.accessoryType === search.accessoryType)
      && (!search.useAccessory || !search.accessoryNo || row.accessoryNo === search.accessoryNo))
    .sort((left, right) => left.accessoryType.localeCompare(right.accessoryType)
      || left.accessoryNo.localeCompare(right.accessoryNo)
      || left.startTime - right.startTime)
})

function spareQtyLimit(row: AccessoryStateRow): number | null {
  if (row.repairCycle === null || row.repairCycle === -1) return row.repairCycle
  return row.repairCycle - row.accumulateQty
}

function displaySpareLimit(row: AccessoryStateRow): string {
  const value = spareQtyLimit(row)
  if (value === null) return '—'
  return value === -1 ? '不適用' : formatQuantity(value)
}

function toggleFilter(kind: 'type' | 'accessory') {
  if (kind === 'type' && !criteria.useType) criteria.accessoryType = ''
  if (kind === 'accessory' && !criteria.useAccessory) criteria.accessoryNo = ''
}

function openPicker(kind: Exclude<PickerKind, null>) {
  pickerKind.value = kind
}

function chooseOption(code: string) {
  if (pickerKind.value === 'type') {
    criteria.accessoryType = code
    criteria.useType = true
  } else if (pickerKind.value === 'accessory') {
    criteria.accessoryNo = code
    criteria.useAccessory = true
  }
  pickerKind.value = null
}

function runQuery() {
  submittedCriteria.value = { ...criteria }
  expandedRows.value = new Set(visibleRows.value.map((row) => row.accSerialNo))
  message.value = `查詢完成，共 ${visibleRows.value.length} 筆配件現況。所有欄位均為 DEV 假資料。`
}

function toggleExpanded(serialNo: string) {
  const next = new Set(expandedRows.value)
  if (next.has(serialNo)) next.delete(serialNo)
  else next.add(serialNo)
  expandedRows.value = next
}
</script>

<template>
  <section v-if="dev" class="accessory-state-page" aria-label="配件狀態查詢 DEV">
    <header class="page-heading">
      <div class="title-line"><h1>配件狀態查詢</h1><span class="dev-badge">DEV 假資料</span></div>
      <p>查詢配件現況、壽命數量與已記錄原因；查詢後可展開現況列檢視原因。</p>
    </header>

    <section class="filters" aria-label="查詢條件">
      <div class="filter-field">
        <label class="filter-check"><input v-model="criteria.useType" type="checkbox" @change="toggleFilter('type')" />Accessory Type</label>
        <div class="picker-input"><input v-model="criteria.accessoryType" type="text" maxlength="50" aria-label="Accessory Type" @input="criteria.useType = Boolean(criteria.accessoryType.trim())" @blur="criteria.accessoryType = criteria.accessoryType.toUpperCase()" /><button type="button" aria-label="選擇配件類型" @click="openPicker('type')">…</button></div>
      </div>
      <div class="filter-field">
        <label class="filter-check"><input v-model="criteria.useAccessory" type="checkbox" @change="toggleFilter('accessory')" />Accessory No</label>
        <div class="picker-input"><input v-model="criteria.accessoryNo" type="text" maxlength="50" aria-label="Accessory No" @input="criteria.useAccessory = Boolean(criteria.accessoryNo.trim())" @blur="criteria.accessoryNo = criteria.accessoryNo.toUpperCase()" /><button type="button" aria-label="選擇配件編號" @click="openPicker('accessory')">…</button></div>
      </div>
      <button type="button" class="query-button" @click="runQuery">Query</button>
    </section>

    <section class="result-section" aria-label="配件現況結果">
      <div class="section-heading"><h2>Accessory State</h2><span>{{ visibleRows.length }} 筆</span></div>
      <div class="table-wrap">
        <table class="state-table">
          <thead><tr><th class="expand-heading"></th><th>Accessory Type</th><th>Accessory No</th><th>State</th><th>Start Time</th><th>User No</th><th>Description</th><th>Apply Time</th><th>Accumulate Qty</th><th>Acc Spare Qty Limit</th><th>Acc Repair Qty</th><th>Acc Total Used Qty</th><th>Time Cycle Start</th></tr></thead>
          <tbody>
            <template v-for="row in visibleRows" :key="row.accSerialNo">
              <tr class="state-row">
                <td class="expand-cell"><button v-if="row.reasons.length" type="button" :aria-label="expandedRows.has(row.accSerialNo) ? '收合原因' : '展開原因'" @click="toggleExpanded(row.accSerialNo)">{{ expandedRows.has(row.accSerialNo) ? '−' : '+' }}</button></td>
                <td>{{ row.accessoryType }}</td><td>{{ row.accessoryNo }}</td><td><span class="state-label">{{ row.stateName }}</span></td><td>{{ displayTime(row.startTime) }}</td><td>{{ row.userNo }}</td><td class="description-cell">{{ row.description }}</td><td>{{ displayTime(row.applyTime) }}</td><td class="number-cell">{{ formatQuantity(row.accumulateQty) }}</td><td class="number-cell">{{ displaySpareLimit(row) }}</td><td class="number-cell">{{ formatQuantity(row.accessoryRepairQty) }}</td><td class="number-cell">{{ formatQuantity(row.accessoryTotalUsedQty) }}</td><td>{{ displayTime(row.timeCycleStart) }}</td>
              </tr>
              <tr v-if="expandedRows.has(row.accSerialNo) && row.reasons.length" class="reason-row">
                <td></td><td colspan="12"><div class="reason-children"><strong>Accessory Reasons</strong><table><thead><tr><th>Reason No</th><th>Reason Name</th></tr></thead><tbody><tr v-for="reason in row.reasons" :key="`${reason.reasonNo}-${reason.reasonName}`"><td>{{ reason.reasonNo }}</td><td>{{ reason.reasonName }}</td></tr></tbody></table></div></td>
              </tr>
            </template>
            <tr v-if="submittedCriteria && visibleRows.length === 0"><td class="empty-cell" colspan="13">沒有符合條件的 DEV 假資料。</td></tr>
            <tr v-if="!submittedCriteria"><td class="empty-cell" colspan="13">請設定條件並按 Query 載入配件現況假資料。</td></tr>
          </tbody>
        </table>
      </div>
    </section>

    <footer class="page-footer"><p role="status">{{ message }}</p><button type="button" class="close-button" @click="emit('close')">Close (X)</button></footer>

    <div v-if="pickerKind" class="modal-backdrop" @click.self="pickerKind = null">
      <section class="picker-dialog" role="dialog" aria-modal="true" :aria-label="pickerTitle">
        <header><div><h2>{{ pickerTitle }}</h2><p>選擇器中的項目均為 DEV 假資料。</p></div><button class="close-picker" type="button" aria-label="關閉" @click="pickerKind = null">×</button></header>
        <div class="picker-options"><button v-for="option in pickerOptions" :key="option.code" type="button" @click="chooseOption(option.code)"><strong>{{ option.code }}</strong><span>{{ option.name }}</span></button></div>
      </section>
    </div>
  </section>
  <section v-else class="not-available" role="status">DEV 預覽只在開發環境提供。</section>
</template>

<style scoped>
.accessory-state-page { display: flex; flex-direction: column; gap: 9px; height: 100%; min-height: 0; padding: 12px 14px; box-sizing: border-box; overflow: auto; background: #f7f5ee; color: #26251f; font: inherit; }
.page-heading { flex: none; }
.title-line { display: flex; align-items: center; gap: 9px; }
h1 { margin: 0; font-size: 19px; }
.page-heading p { margin: 4px 0 0; color: #777366; font-size: 12px; }
.dev-badge { padding: 3px 8px; border-radius: 12px; background: #fff0bd; color: #6d5713; font-size: 10px; font-weight: 700; }
.filters { display: flex; flex: none; align-items: end; gap: 12px; padding: 9px; border: 1px solid #d8d3c5; border-radius: 5px; background: #fffdf7; }
.filter-field { display: grid; width: min(310px, 35%); gap: 4px; }
.filter-check { display: flex; align-items: center; gap: 6px; color: #555247; font-size: 11px; font-weight: 600; }
.picker-input { display: flex; min-width: 0; }
.picker-input input { flex: 1; width: 100%; min-width: 0; height: 26px; padding: 3px 6px; border: 1px solid #c8c2b4; border-radius: 3px; background: white; color: #26251f; font: inherit; font-size: 11px; }
.picker-input button { width: 29px; margin-left: 4px; border: 1px solid #b9b4a6; border-radius: 3px; background: #f8f6ef; color: #37352f; cursor: pointer; }
.query-button, .close-button { min-height: 29px; padding: 4px 12px; border: 1px solid #3b3930; border-radius: 4px; background: #3b3930; color: white; font: inherit; font-size: 12px; cursor: pointer; }
.query-button { margin-left: auto; }
.result-section { display: flex; flex: 1 1 auto; flex-direction: column; min-height: 0; border: 1px solid #d8d3c5; border-radius: 5px; background: #fffdf7; }
.section-heading { display: flex; flex: none; align-items: center; justify-content: space-between; padding: 6px 9px; border-bottom: 1px solid #e7e3d8; background: #ece9df; }
.section-heading h2 { margin: 0; color: #555247; font-size: 11px; font-weight: 700; }
.section-heading span { color: #777366; font-size: 10px; }
.table-wrap { min-height: 0; overflow: auto; }
.state-table { width: max-content; min-width: 100%; border-collapse: collapse; font-size: 11px; white-space: nowrap; }
thead { position: sticky; top: 0; z-index: 1; background: #f5f3eb; color: #625f55; text-align: left; }
th, td { padding: 6px 8px; border-bottom: 1px solid #eeeae0; }
th { font-size: 10px; font-weight: 700; }
.expand-heading, .expand-cell { width: 24px; min-width: 24px; padding-right: 3px; padding-left: 3px; text-align: center; }
.expand-cell button { width: 19px; height: 19px; padding: 0; border: 1px solid #b9b4a6; border-radius: 3px; background: #f8f6ef; color: #37352f; cursor: pointer; }
.state-row:hover { background: #f8f6ef; }
.state-label { display: inline-block; padding: 2px 6px; border-radius: 9px; background: #e9efe9; color: #3d684c; }
.description-cell { max-width: 180px; overflow: hidden; text-overflow: ellipsis; }
.number-cell { text-align: right; font-variant-numeric: tabular-nums; }
.reason-row td { background: #f9f7f1; }
.reason-children { display: flex; align-items: flex-start; gap: 14px; padding: 2px 8px 6px; }
.reason-children strong { padding-top: 7px; color: #777366; font-size: 10px; }
.reason-children table { min-width: 350px; border-collapse: collapse; background: white; }
.reason-children th, .reason-children td { padding: 4px 7px; border: 1px solid #e7e3d8; text-align: left; }
.empty-cell { padding: 18px 8px; color: #8a867a; text-align: center; }
.page-footer { display: flex; flex: none; align-items: center; justify-content: space-between; min-height: 29px; gap: 10px; }
.page-footer p { margin: 0; color: #686457; font-size: 11px; }
.close-button { min-width: 78px; min-height: 26px; }
button:focus-visible, input:focus-visible { outline: 2px solid #d8b622; outline-offset: 2px; }
.modal-backdrop { position: fixed; inset: 0; z-index: 20; display: grid; place-items: center; padding: 18px; background: #18171277; }
.picker-dialog { width: min(460px, 100%); max-height: min(500px, 90vh); display: flex; flex-direction: column; padding: 14px; border: 1px solid #c9c3b4; border-radius: 7px; background: #fffdf7; box-shadow: 0 16px 48px #0003; }
.picker-dialog header { display: flex; justify-content: space-between; gap: 14px; margin-bottom: 10px; }
.picker-dialog h2 { margin: 0; font-size: 16px; }
.picker-dialog p { margin: 4px 0 0; color: #777366; font-size: 11px; }
.close-picker { width: 28px; height: 28px; border: 0; border-radius: 4px; background: transparent; color: #625f55; font-size: 21px; cursor: pointer; }
.picker-options { display: grid; gap: 5px; overflow: auto; }
.picker-options button { display: flex; align-items: center; gap: 12px; min-height: 35px; padding: 5px 8px; border: 1px solid #e3dfd4; border-radius: 4px; background: white; color: #36342e; text-align: left; font: inherit; cursor: pointer; }
.picker-options button:hover { border-color: #88742e; background: #fff9df; }
.picker-options strong { min-width: 130px; font-size: 11px; }
.not-available { padding: 12px; background: #f7f5ee; color: #777366; }
@media (max-width: 650px) { .filters { align-items: stretch; flex-direction: column; } .filter-field { width: 100%; } .query-button { margin-left: 0; } .reason-children { flex-direction: column; gap: 4px; } }
</style>
