<script setup lang="ts">
import { computed, ref } from 'vue'

type LogRow = {
  inventoryNo: string
  materialNo: string
  materialLotNo: string
  unitNo: string
  materialType: string
  eventTime: Date
  gumTypeStatus: number
  qty: number
}
type ParentRow = {
  key: string
  inventoryNo: string
  materialNo: string
  materialLotNo: string
  unitNo: string
  materialType: string
  logs: LogRow[]
}
type MaterialOption = { materialNo: string; materialType: string; name: string }

const emit = defineEmits<{ close: [] }>()
const now = new Date()
const today = dateInput(now)
const inventoryOptions = ['INV-WIP-DEV-01', 'INV-WIP-DEV-02']
const materialOptions: MaterialOption[] = [
  { materialNo: 'GUM-DEV-1001', materialType: 'DEV-GUM-A', name: '假資料膠料 A' },
  { materialNo: 'GUM-DEV-1002', materialType: 'DEV-GUM-A', name: '假資料膠料 B' },
  { materialNo: 'GUM-DEV-2001', materialType: 'DEV-GUM-B', name: '假資料膠料 C' },
]
const logRows: LogRow[] = [
  { inventoryNo: inventoryOptions[0], materialNo: 'GUM-DEV-1001', materialLotNo: 'GL-DEV-24001', unitNo: 'KG', materialType: 'DEV-GUM-A', eventTime: hoursAgo(2), gumTypeStatus: 1, qty: 6 },
  { inventoryNo: inventoryOptions[0], materialNo: 'GUM-DEV-1001', materialLotNo: 'GL-DEV-24001', unitNo: 'KG', materialType: 'DEV-GUM-A', eventTime: hoursAgo(25), gumTypeStatus: 0, qty: 8 },
  { inventoryNo: inventoryOptions[0], materialNo: 'GUM-DEV-1001', materialLotNo: 'GL-DEV-24001', unitNo: 'KG', materialType: 'DEV-GUM-A', eventTime: hoursAgo(50), gumTypeStatus: 2, qty: 8 },
  { inventoryNo: inventoryOptions[0], materialNo: 'GUM-DEV-1002', materialLotNo: 'GL-DEV-24002', unitNo: 'KG', materialType: 'DEV-GUM-A', eventTime: hoursAgo(4), gumTypeStatus: 0, qty: 3.5 },
  { inventoryNo: inventoryOptions[1], materialNo: 'GUM-DEV-2001', materialLotNo: 'GL-DEV-24008', unitNo: 'EA', materialType: 'DEV-GUM-B', eventTime: hoursAgo(7), gumTypeStatus: 2, qty: 12 },
  { inventoryNo: inventoryOptions[1], materialNo: 'GUM-DEV-2001', materialLotNo: 'GL-DEV-24009', unitNo: 'EA', materialType: 'DEV-GUM-B', eventTime: hoursAgo(29), gumTypeStatus: 1, qty: 4 },
]

const inventoryNo = ref('')
const materialNo = ref('')
const materialLotNo = ref('')
const gumTypeStatus = ref('0')
const fromDate = ref(today)
const endDate = ref(today)
const useGumTypeStatus = ref(false)
const useMaterialLotNo = ref(false)
const useEventTime = ref(false)
const materialPickerOpen = ref(false)
const materialSearch = ref('')
const expandedKeys = ref<Set<string>>(new Set())
const hasSearched = ref(false)
const loading = ref(false)

const availableMaterials = computed(() => {
  const query = materialSearch.value.trim().toUpperCase()
  return materialOptions.filter(row => !query || row.materialNo.includes(query) || row.name.includes(query))
})
const visibleParents = computed<ParentRow[]>(() => {
  if (!hasSearched.value) return []
  const from = useEventTime.value && fromDate.value ? dayStart(fromDate.value) : null
  const to = useEventTime.value && endDate.value ? dayStart(endDate.value) : null
  const prefix = useMaterialLotNo.value ? materialLotNo.value.trim() : ''
  const rows = logRows.filter(row =>
    (!inventoryNo.value || row.inventoryNo === inventoryNo.value)
    && (!materialNo.value.trim() || row.materialNo === materialNo.value.trim())
    && (!useGumTypeStatus.value || row.gumTypeStatus === Number(gumTypeStatus.value))
    && (!prefix || row.materialLotNo.startsWith(prefix))
    && (!from || !to || (row.eventTime >= from && row.eventTime <= to)),
  )
  const grouped = new Map<string, ParentRow>()
  for (const row of rows) {
    const key = JSON.stringify([row.inventoryNo, row.materialNo, row.materialLotNo, row.unitNo, row.materialType])
    let parent = grouped.get(key)
    if (!parent) {
      parent = {
        key,
        inventoryNo: row.inventoryNo,
        materialNo: row.materialNo,
        materialLotNo: row.materialLotNo,
        unitNo: row.unitNo,
        materialType: row.materialType,
        logs: [],
      }
      grouped.set(key, parent)
    }
    parent.logs.push(row)
  }
  for (const parent of grouped.values()) parent.logs.sort((a, b) => b.eventTime.getTime() - a.eventTime.getTime())
  return [...grouped.values()]
})

function hoursAgo(hours: number): Date { return new Date(Date.now() - hours * 60 * 60 * 1000) }
function dayStart(value: string): Date { return new Date(value + 'T00:00:00') }
function dateInput(value: Date): string {
  const pad = (part: number) => String(part).padStart(2, '0')
  return value.getFullYear() + '-' + pad(value.getMonth() + 1) + '-' + pad(value.getDate())
}
function formatDate(value: Date): string {
  const pad = (part: number) => String(part).padStart(2, '0')
  return value.getFullYear() + '/' + pad(value.getMonth() + 1) + '/' + pad(value.getDate()) + ' ' + pad(value.getHours()) + ':' + pad(value.getMinutes()) + ':' + pad(value.getSeconds())
}
function formatQty(value: number): string { return value.toLocaleString('zh-TW', { maximumFractionDigits: 6 }) }
function onMaterialLotInput(event: Event): void {
  if (event.target instanceof HTMLInputElement && event.target.value.trim()) useMaterialLotNo.value = true
}
function chooseMaterial(row: MaterialOption): void {
  materialNo.value = row.materialNo
  materialPickerOpen.value = false
}
function toggleExpanded(row: ParentRow): void {
  const next = new Set(expandedKeys.value)
  if (next.has(row.key)) next.delete(row.key)
  else next.add(row.key)
  expandedKeys.value = next
}
async function query(): Promise<void> {
  loading.value = true
  hasSearched.value = true
  expandedKeys.value = new Set()
  await new Promise<void>(resolve => window.setTimeout(resolve, 100))
  loading.value = false
}
</script>

<template>
  <main class="gum-query-page" aria-label="膠類歷程資料查詢 DEV 預覽">
    <header class="page-heading">
      <div><span class="eyebrow">RPT / RT612</span><h1>膠類歷程資料查詢</h1><p>以庫房、膠料、膠料批號、狀態及事件日期查詢歷程快照。</p></div>
      <span class="dev-badge">DEV 假資料</span>
    </header>

    <form class="query-panel" @submit.prevent="query">
      <div class="field inventory-field">
        <label for="gum-inventory">Select Inventory No</label>
        <select id="gum-inventory" v-model="inventoryNo">
          <option value="">全部庫房</option>
          <option v-for="option in inventoryOptions" :key="option" :value="option">{{ option }}</option>
        </select>
      </div>
      <div class="field material-field">
        <label for="gum-material">Material No</label>
        <div class="input-with-button">
          <input id="gum-material" v-model="materialNo" :readonly="!inventoryNo" :class="{ enabled: inventoryNo }" placeholder="先選庫房，再選膠料">
          <button type="button" :disabled="!inventoryNo" aria-label="選擇膠料" @click="materialPickerOpen = true">...</button>
        </div>
      </div>
      <div class="field lot-field">
        <label class="check-label"><input v-model="useMaterialLotNo" type="checkbox"> Lot No</label>
        <input v-model="materialLotNo" placeholder="批號前綴" @input="onMaterialLotInput">
      </div>
      <div class="field status-field">
        <label class="check-label"><input v-model="useGumTypeStatus" type="checkbox"> GumTypeStatus</label>
        <select v-model="gumTypeStatus" aria-label="GumTypeStatus 假資料代碼">
          <option value="0">0</option><option value="1">1</option><option value="2">2</option>
        </select>
      </div>
      <div class="field date-field">
        <label class="check-label"><input v-model="useEventTime" type="checkbox"> Event Time</label>
        <div class="date-inputs">
          <label>From <input v-model="fromDate" type="date"></label>
          <label>To <input v-model="endDate" type="date"></label>
        </div>
      </div>
      <button class="primary query-button" type="submit" :disabled="loading">{{ loading ? '查詢中…' : 'Query' }}</button>
    </form>

    <section class="grid-panel" aria-label="膠類歷程資料">
      <header><strong>Raw Inventory History</strong><span>{{ visibleParents.length }} lots</span></header>
      <div class="table-scroll">
        <table>
          <thead><tr><th class="expand-column"></th><th>InventoryNo</th><th>MaterialNo</th><th>MaterialLotNo</th><th>UnitNo</th><th>MaterialType</th><th class="qty-column">History rows</th></tr></thead>
          <tbody v-if="visibleParents.length">
            <template v-for="row in visibleParents" :key="row.key">
              <tr class="parent-row">
                <td><button class="expand-button" type="button" :aria-expanded="expandedKeys.has(row.key)" :aria-label="expandedKeys.has(row.key) ? '收合歷程' : '展開歷程'" @click="toggleExpanded(row)">{{ expandedKeys.has(row.key) ? '−' : '+' }}</button></td>
                <td>{{ row.inventoryNo }}</td><td>{{ row.materialNo }}</td><td>{{ row.materialLotNo }}</td><td>{{ row.unitNo }}</td><td>{{ row.materialType }}</td><td class="qty-column">{{ row.logs.length }}</td>
              </tr>
              <tr v-if="expandedKeys.has(row.key)" class="detail-row"><td colspan="7">
                <table class="child-table"><thead><tr><th>EventTime</th><th>GumTypeStatus</th><th class="qty-column">Qty</th></tr></thead>
                  <tbody><tr v-for="(entry, index) in row.logs" :key="row.key + '-' + index">
                    <td>{{ formatDate(entry.eventTime) }}</td><td>{{ entry.gumTypeStatus }}</td><td class="qty-column">{{ formatQty(entry.qty) }}</td>
                  </tr></tbody>
                </table>
              </td></tr>
            </template>
          </tbody>
          <tbody v-else><tr><td colspan="7" class="empty-state">{{ loading ? '載入中…' : hasSearched ? '查無膠類歷程。' : '設定條件後按 Query；來源未設定至少一項條件。' }}</td></tr></tbody>
        </table>
      </div>
    </section>
    <footer class="page-footer"><span>固定虛構資料；查詢只讀取本機記憶體。狀態以數值顯示，來源 ValueList 文字未取得；本預覽不呼叫 MES API、Oracle 或 PostgreSQL。</span><button type="button" @click="emit('close')">Close (X)</button></footer>

    <div v-if="materialPickerOpen" class="modal-backdrop" @click.self="materialPickerOpen = false">
      <section class="material-picker" role="dialog" aria-modal="true" aria-label="選擇膠料">
        <header><strong>選擇膠料</strong><button type="button" aria-label="關閉" @click="materialPickerOpen = false">×</button></header>
        <p>DEV 假設定 GumMaterialType；只列 IssueState=2 的假資料。</p>
        <input v-model="materialSearch" autofocus placeholder="搜尋 MaterialNo 或名稱">
        <div class="material-list">
          <button v-for="item in availableMaterials" :key="item.materialNo" type="button" @click="chooseMaterial(item)">
            <span>{{ item.materialNo }}</span><small>{{ item.materialType }} · {{ item.name }}</small>
          </button>
          <p v-if="!availableMaterials.length" class="empty-state">沒有符合的膠料。</p>
        </div>
        <footer><button type="button" @click="materialPickerOpen = false">Close</button></footer>
      </section>
    </div>
  </main>
</template>

<style scoped>
.gum-query-page{box-sizing:border-box;display:flex;height:100%;min-height:0;flex-direction:column;gap:8px;overflow:hidden;padding:10px;background:#f3f2ed;color:#292820;font:inherit}.page-heading{display:flex;align-items:flex-start;justify-content:space-between;gap:12px}.eyebrow{color:#777366;font-size:9px;letter-spacing:.1em}.page-heading h1{margin:2px 0;font-size:17px}.page-heading p{margin:0;color:#777366;font-size:10px}.dev-badge{padding:4px 8px;border:1px solid #d6c995;border-radius:12px;background:#f4efdc;color:#705c17;font-size:10px;font-weight:700}.query-panel{display:grid;grid-template-columns:minmax(145px,1fr) minmax(205px,1.25fr) minmax(135px,.85fr) minmax(145px,.9fr) minmax(300px,1.65fr) 76px;align-items:end;gap:8px;padding:8px 10px;border:1px solid #d4d1c5;background:#fffefa}.field{display:flex;min-width:0;flex-direction:column;gap:4px}.field>label{font-size:10px;white-space:nowrap}.field input,.field select{box-sizing:border-box;width:100%;height:27px;min-width:0;padding:3px 6px;border:1px solid #c9c6bb;border-radius:2px;background:#fff;font:inherit;font-size:10px}.input-with-button{display:flex;gap:4px}.input-with-button input{flex:1}.input-with-button input.enabled{background:#fff9da}.input-with-button button{width:28px;height:27px;border:1px solid #c9c6bb;background:#f3f2ed;font:inherit;cursor:pointer}.input-with-button button:disabled{opacity:.5;cursor:not-allowed}.check-label{display:flex;align-items:center;gap:4px;font-size:10px;white-space:nowrap}.date-inputs{display:grid;grid-template-columns:1fr 1fr;gap:5px}.date-inputs label{display:flex;align-items:center;gap:4px;font-size:9px}.date-inputs input{height:27px;padding:3px 4px}.query-button{height:28px;border:1px solid #364d46;border-radius:2px;background:#364d46;color:#fff;font:inherit;font-size:10px;cursor:pointer}.query-button:disabled{opacity:.6;cursor:wait}.grid-panel{display:flex;min-height:0;flex:1;flex-direction:column;overflow:hidden;border:1px solid #d4d1c5;background:#fffefa}.grid-panel>header{display:flex;align-items:center;justify-content:space-between;padding:6px 9px;border-bottom:1px solid #e6e4dc;font-size:10px}.grid-panel>header span{color:#777366;font-size:9px}.table-scroll{min-height:0;flex:1;overflow:auto}table{width:100%;border-collapse:collapse;font-size:10px;white-space:nowrap}th{position:sticky;top:0;z-index:1;background:#ecebe5;text-align:left}th,td{padding:5px 7px;border-bottom:1px solid #e6e4dc}.expand-column{width:32px}.expand-button{width:20px;height:20px;border:1px solid #ccc8bc;border-radius:2px;background:#f7f6f1;color:#38372e;font:inherit;cursor:pointer}.parent-row:hover{background:#f8f8f3}.qty-column{text-align:right;font-variant-numeric:tabular-nums}.detail-row>td{padding:0 10px 8px 37px;background:#f8f7f2}.child-table{width:min(680px,100%);margin-top:5px;background:#fff}.child-table th{position:static}.empty-state{padding:18px;text-align:center;color:#777366}.page-footer{display:flex;align-items:center;justify-content:space-between;gap:10px;color:#777366;font-size:9px}.page-footer button{flex:none;min-height:25px;padding:3px 9px;border:1px solid #c9c6bb;border-radius:2px;background:#f8f7f2;color:#343329;font:inherit;font-size:9px;cursor:pointer}.modal-backdrop{position:fixed;inset:0;z-index:20;display:grid;place-items:center;padding:20px;background:rgba(26,28,24,.42)}.material-picker{display:flex;width:min(440px,100%);max-height:min(540px,90vh);flex-direction:column;gap:9px;padding:12px;border:1px solid #cbc8bd;background:#fffefa;box-shadow:0 12px 32px rgba(0,0,0,.2)}.material-picker>header{display:flex;align-items:center;justify-content:space-between;font-size:12px}.material-picker>header button{width:25px;height:25px;border:1px solid #d4d1c5;background:#f4f3ee;font:inherit;cursor:pointer}.material-picker>p{margin:0;color:#777366;font-size:10px}.material-picker>input{height:30px;padding:4px 7px;border:1px solid #c9c6bb;font:inherit;font-size:11px}.material-list{min-height:0;overflow:auto;border:1px solid #e1dfd7}.material-list>button{display:flex;width:100%;flex-direction:column;gap:3px;padding:8px;border:0;border-bottom:1px solid #e6e4dc;background:transparent;text-align:left;font:inherit;font-size:11px;cursor:pointer}.material-list>button:hover{background:#edf1eb}.material-list small{color:#777366;font-size:9px}.material-list .empty-state{margin:0}.material-picker>footer{display:flex;justify-content:flex-end}.material-picker>footer button{padding:5px 10px;border:1px solid #c9c6bb;background:#f4f3ee;font:inherit;font-size:10px;cursor:pointer}@media(max-width:1050px){.query-panel{grid-template-columns:repeat(3,minmax(0,1fr))}.query-button{grid-column:3;justify-self:end;width:76px}}@media(max-width:680px){.query-panel{grid-template-columns:repeat(2,minmax(0,1fr))}.date-field{grid-column:1 / -1}.query-button{grid-column:2}}
</style>
