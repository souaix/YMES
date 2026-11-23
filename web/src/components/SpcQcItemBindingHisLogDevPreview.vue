<script setup lang="ts">
import { computed, ref } from 'vue'

type BindType = 1 | 2 | 3 | 4
type QcItemType = 0 | 1 | 2 | 3 | 4
type QueryTab = 'PRODUCT' | 'OP' | 'MATERIALTYPE' | 'MATERIALNO'
interface QcItem { no: string; name: string; type: QcItemType; issueState: 2 }
interface BindingEvent {
  id: string; executeStatus: 0 | 1 | 2; executeDate: string; executor: string
  inLine: string; executeOrder: number | null; inspTypeFai: boolean | null; inspTypeIpqc: boolean | null; inspTypePqc: boolean | null
}
interface BindingParent {
  id: string; qcItemType: QcItemType; qcItemNo: string; qcItemName: string; bindType: BindType
  productNo: string; productVersion: string; opNo: string; materialType: string; materialNo: string
  events: BindingEvent[]
}
interface QueryState {
  qcItemTypeEnabled: boolean; qcItemType: QcItemType | ''
  qcItemNo: string; executeStatusEnabled: boolean; executeStatus: 0 | 1 | 2 | ''
  fromDate: string; endDate: string
}

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const qcTypeOptions: Array<{ value: QcItemType; label: string }> = [
  { value: 0, label: '計數' }, { value: 1, label: '計量' }, { value: 2, label: '計量群組' },
  { value: 3, label: '計數群組' }, { value: 4, label: '計數群組_減項' },
]
const executeStatusOptions: Array<{ value: 0 | 1 | 2; label: string }> = [
  { value: 0, label: '0 · 新增綁定' }, { value: 1, label: '1 · 更新綁定' }, { value: 2, label: '2 · 解除綁定' },
]
const qcItems: QcItem[] = [
  { no: 'ATT-DEV-201', name: '外觀抽驗', type: 0, issueState: 2 },
  { no: 'VAR-DEV-101', name: '軸心直徑', type: 1, issueState: 2 },
  { no: 'VAR-GRP-02', name: '軸心量測群組', type: 2, issueState: 2 },
  { no: 'ATT-GRP-01', name: '外觀順序群組', type: 3, issueState: 2 },
  { no: 'ATT-PMI-01', name: '外觀減項群組', type: 4, issueState: 2 },
]
const parents: BindingParent[] = [
  {
    id: 'product-1', qcItemType: 1, qcItemNo: 'VAR-DEV-101', qcItemName: '軸心直徑', bindType: 4,
    productNo: 'PRD-AX17', productVersion: 'A', opNo: 'OP-20', materialType: '', materialNo: '', events: [
      { id: 'p1-0', executeStatus: 0, executeDate: '2026-11-12T08:15:00', executor: 'DEV-QC-01', inLine: 'Y', executeOrder: 2, inspTypeFai: true, inspTypeIpqc: true, inspTypePqc: false },
      { id: 'p1-1', executeStatus: 1, executeDate: '2026-11-19T14:40:00', executor: 'DEV-QC-02', inLine: 'Y', executeOrder: 3, inspTypeFai: true, inspTypeIpqc: true, inspTypePqc: true },
      { id: 'p1-2', executeStatus: 2, executeDate: '2026-11-21T10:05:00', executor: 'DEV-QC-01', inLine: 'N', executeOrder: null, inspTypeFai: false, inspTypeIpqc: false, inspTypePqc: false },
    ],
  },
  {
    id: 'op-1', qcItemType: 0, qcItemNo: 'ATT-DEV-201', qcItemName: '外觀抽驗', bindType: 3,
    productNo: '', productVersion: '', opNo: 'OP-35', materialType: '', materialNo: '', events: [
      { id: 'o1-0', executeStatus: 0, executeDate: '2026-11-10T09:30:00', executor: 'DEV-QC-03', inLine: 'Y', executeOrder: 1, inspTypeFai: true, inspTypeIpqc: false, inspTypePqc: true },
      { id: 'o1-2', executeStatus: 2, executeDate: '2026-11-22T11:20:00', executor: 'DEV-QC-03', inLine: 'N', executeOrder: null, inspTypeFai: false, inspTypeIpqc: false, inspTypePqc: false },
    ],
  },
  {
    id: 'material-type-1', qcItemType: 2, qcItemNo: 'VAR-GRP-02', qcItemName: '軸心量測群組', bindType: 1,
    productNo: '', productVersion: '', opNo: '', materialType: 'MT-AL', materialNo: '', events: [
      { id: 'mt1-0', executeStatus: 0, executeDate: '2026-11-18T15:10:00', executor: 'DEV-MTL-01', inLine: '', executeOrder: null, inspTypeFai: null, inspTypeIpqc: null, inspTypePqc: null },
      { id: 'mt1-1', executeStatus: 1, executeDate: '2026-11-22T08:50:00', executor: 'DEV-MTL-02', inLine: '', executeOrder: null, inspTypeFai: null, inspTypeIpqc: null, inspTypePqc: null },
    ],
  },
  {
    id: 'material-no-1', qcItemType: 3, qcItemNo: 'ATT-GRP-01', qcItemName: '外觀順序群組', bindType: 2,
    productNo: '', productVersion: '', opNo: '', materialType: '', materialNo: 'MTL-SS31', events: [
      { id: 'mn1-0', executeStatus: 0, executeDate: '2026-11-17T13:05:00', executor: 'DEV-MTL-01', inLine: '', executeOrder: null, inspTypeFai: null, inspTypeIpqc: null, inspTypePqc: null },
      { id: 'mn1-2', executeStatus: 2, executeDate: '2026-11-22T16:25:00', executor: 'DEV-MTL-01', inLine: '', executeOrder: null, inspTypeFai: null, inspTypeIpqc: null, inspTypePqc: null },
    ],
  },
]

function toLocalInput(value: Date): string {
  const pad = (part: number) => String(part).padStart(2, '0')
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}:${pad(value.getSeconds())}`
}
const initialTo = new Date()
const initialFrom = new Date(initialTo)
initialFrom.setDate(initialFrom.getDate() - 7)
initialFrom.setHours(0, 0, 0, 0)
const query = ref<QueryState>({
  qcItemTypeEnabled: false, qcItemType: '', qcItemNo: '', executeStatusEnabled: false, executeStatus: '',
  fromDate: toLocalInput(initialFrom), endDate: toLocalInput(initialTo),
})
const appliedQuery = ref<QueryState | null>(null)
const activeTab = ref<QueryTab>('PRODUCT')
const searched = ref(false)
const notice = ref('設定日期區間後按 Query；此頁只使用 DEV 假資料。')
const noticeKind = ref<'info' | 'success' | 'error'>('info')

const itemOptions = computed(() => {
  const type = query.value.qcItemType
  return type === '' ? [] : qcItems.filter((item) => item.type === type && item.issueState === 2)
})
const filteredParents = computed(() => {
  const applied = appliedQuery.value
  if (!applied) return []
  return parents.filter((parent) => {
    if (activeTab.value === 'PRODUCT' && parent.bindType !== 4) return false
    if (activeTab.value === 'OP' && parent.bindType !== 3) return false
    if (activeTab.value === 'MATERIALTYPE' && parent.bindType !== 1) return false
    if (activeTab.value === 'MATERIALNO' && parent.bindType !== 2) return false
    if (applied.qcItemTypeEnabled && applied.qcItemType !== '' && parent.qcItemType !== applied.qcItemType) return false
    if (applied.qcItemTypeEnabled && applied.qcItemType !== '' && applied.qcItemNo && parent.qcItemNo !== applied.qcItemNo) return false
    return matchingEvents(parent, applied).length > 0
  })
})
const eventCount = computed(() => filteredParents.value.reduce((total, parent) => total + matchingEvents(parent, appliedQuery.value!).length, 0))

function matchingEvents(parent: BindingParent, applied: QueryState | null): BindingEvent[] {
  if (!applied) return []
  return parent.events.filter((event) => {
    // Legacy Core applies an inclusive range to both parent and child queries.
    if (event.executeDate < applied.fromDate || event.executeDate > applied.endDate) return false
    if (applied.executeStatusEnabled && applied.executeStatus !== '' && event.executeStatus !== applied.executeStatus) return false
    return true
  }).sort((left, right) => left.executeDate.localeCompare(right.executeDate))
}
function typeLabel(value: QcItemType): string { return qcTypeOptions.find((option) => option.value === value)?.label ?? `Type ${value}` }
function statusLabel(value: BindingEvent['executeStatus']): string { return executeStatusOptions.find((option) => option.value === value)?.label ?? String(value) }
function formatDate(value: string): string { return value.replace('T', ' ') }
function showValue(value: string | number | null): string { return value === null || value === '' ? '—' : String(value) }
function showBoolean(value: boolean | null): string { return value === null ? '—' : value ? '✓' : '□' }
function setType(value: string): void {
  query.value.qcItemType = value === '' ? '' : Number(value) as QcItemType
  if (value !== '') query.value.qcItemTypeEnabled = true
  query.value.qcItemNo = ''
}
function onTypeChange(event: Event): void { setType((event.target as HTMLSelectElement).value) }
function setStatus(value: string): void {
  query.value.executeStatus = value === '' ? '' : Number(value) as 0 | 1 | 2
  if (value !== '') query.value.executeStatusEnabled = true
}
function onStatusChange(event: Event): void { setStatus((event.target as HTMLSelectElement).value) }
function runQuery(): void {
  if (query.value.qcItemTypeEnabled && query.value.qcItemType !== '' && !query.value.qcItemNo) {
    notice.value = '已啟用 QC Item Type 篩選時，請先選擇 QC Item 或 Group。'
    noticeKind.value = 'error'
    return
  }
  appliedQuery.value = { ...query.value }
  searched.value = true
  notice.value = `查詢完成：${filteredParents.value.length} 組綁定，${eventCount.value} 筆歷程（DEV 假資料）。`
  noticeKind.value = 'success'
}
</script>

<template>
  <section v-if="props.developmentPreview" class="binding-page" aria-label="SPC 品管項目綁定紀錄">
    <header class="page-head"><div><p class="eyebrow">SPC / SC216</p><h1>品管項目綁定紀錄表</h1><p class="subtitle">檢視 QC Item 與產品、作業站、物料類別及物料編號的綁定歷程。</p></div><button class="button" type="button" @click="emit('close')">Close (X)</button></header>

    <section class="filter-panel panel">
      <div class="filter-grid">
        <label class="toggle-field"><span><input v-model="query.qcItemTypeEnabled" type="checkbox"> QC Item Type</span><select :value="query.qcItemType" @change="onTypeChange"><option value="">全部類型</option><option v-for="option in qcTypeOptions" :key="option.value" :value="option.value">{{ option.label }}</option></select></label>
        <label class="lookup-field"><span>QC Item / Group</span><select v-model="query.qcItemNo" :disabled="query.qcItemType === ''"><option value="">選擇 Issue 狀態的 QC Item</option><option v-for="item in itemOptions" :key="item.no" :value="item.no">{{ item.no }} — {{ item.name }}</option></select></label>
        <label class="toggle-field"><span><input v-model="query.executeStatusEnabled" type="checkbox"> ExecuteStatus</span><select :value="query.executeStatus" @change="onStatusChange"><option value="">全部狀態</option><option v-for="option in executeStatusOptions" :key="option.value" :value="option.value">{{ option.label }}</option></select></label>
        <label class="date-field"><span>Binding His Time · From</span><input v-model="query.fromDate" type="datetime-local" step="1"></label>
        <label class="date-field"><span>To</span><input v-model="query.endDate" type="datetime-local" step="1"></label>
      </div>
      <footer class="filter-actions"><span>預設最近 7 天（起日 00:00:00 至目前時間）；日期端點均包含。</span><button class="button primary" type="button" @click="runQuery">Query</button></footer>
    </section>

    <section class="history-panel panel">
      <nav class="tabs" aria-label="綁定類別">
        <button :class="{ active: activeTab === 'PRODUCT' }" type="button" @click="activeTab = 'PRODUCT'">Product</button>
        <button :class="{ active: activeTab === 'OP' }" type="button" @click="activeTab = 'OP'">OP</button>
        <button :class="{ active: activeTab === 'MATERIALTYPE' }" type="button" @click="activeTab = 'MATERIALTYPE'">Material Type</button>
        <button :class="{ active: activeTab === 'MATERIALNO' }" type="button" @click="activeTab = 'MATERIALNO'">MaterialNo</button>
        <span v-if="searched" class="result-count">{{ filteredParents.length }} 組 · {{ eventCount }} 筆明細</span>
      </nav>
      <div v-if="!searched" class="empty-state">設定查詢條件後按 Query 顯示綁定歷程。</div>
      <div v-else class="table-scroll">
        <table class="history-table">
          <thead><tr><th>QCItemType</th><th>QCItemNo</th><th v-if="activeTab === 'PRODUCT'">ProductNo</th><th v-if="activeTab === 'PRODUCT'">ProductVersion</th><th v-if="activeTab === 'PRODUCT' || activeTab === 'OP'">OPNo</th><th v-if="activeTab === 'MATERIALTYPE'">MTLTYPE</th><th v-if="activeTab === 'MATERIALNO'">MTLNO</th><th>History Detail</th></tr></thead>
          <tbody>
            <template v-for="parent in filteredParents" :key="parent.id">
              <tr class="parent-row">
                <td>{{ typeLabel(parent.qcItemType) }}</td><td>{{ parent.qcItemNo }} — {{ parent.qcItemName }}</td>
                <td v-if="activeTab === 'PRODUCT'">{{ parent.productNo }}</td><td v-if="activeTab === 'PRODUCT'">{{ parent.productVersion }}</td>
                <td v-if="activeTab === 'PRODUCT' || activeTab === 'OP'">{{ parent.opNo }}</td><td v-if="activeTab === 'MATERIALTYPE'">{{ parent.materialType }}</td><td v-if="activeTab === 'MATERIALNO'">{{ parent.materialNo }}</td>
                <td class="detail-cell"><details open><summary>{{ matchingEvents(parent, appliedQuery).length }} 筆綁定歷程</summary>
                  <table class="detail-table"><thead><tr><th v-if="activeTab === 'PRODUCT' || activeTab === 'OP'">InLine</th><th v-if="activeTab === 'PRODUCT' || activeTab === 'OP'">ExecuteOrder</th><th v-if="activeTab === 'PRODUCT' || activeTab === 'OP'">INSPTYPE_FAI</th><th v-if="activeTab === 'PRODUCT' || activeTab === 'OP'">INSPTYPE_IPQC</th><th v-if="activeTab === 'PRODUCT' || activeTab === 'OP'">INSPTYPE_PQC</th><th>ExecuteStatus</th><th>ExecuteDate</th><th>Executor</th></tr></thead>
                    <tbody><tr v-for="event in matchingEvents(parent, appliedQuery)" :key="event.id">
                      <td v-if="activeTab === 'PRODUCT' || activeTab === 'OP'">{{ showValue(event.inLine) }}</td><td v-if="activeTab === 'PRODUCT' || activeTab === 'OP'">{{ showValue(event.executeOrder) }}</td><td v-if="activeTab === 'PRODUCT' || activeTab === 'OP'">{{ showBoolean(event.inspTypeFai) }}</td><td v-if="activeTab === 'PRODUCT' || activeTab === 'OP'">{{ showBoolean(event.inspTypeIpqc) }}</td><td v-if="activeTab === 'PRODUCT' || activeTab === 'OP'">{{ showBoolean(event.inspTypePqc) }}</td>
                      <td><span :class="['status-pill', `status-${event.executeStatus}`]">{{ statusLabel(event.executeStatus) }}</span></td><td>{{ formatDate(event.executeDate) }}</td><td>{{ event.executor }}</td>
                    </tr><tr v-if="matchingEvents(parent, appliedQuery).length === 0"><td :colspan="activeTab === 'PRODUCT' || activeTab === 'OP' ? 8 : 3" class="empty-cell">沒有符合條件的明細</td></tr></tbody>
                  </table>
                </details></td>
              </tr>
            </template>
            <tr v-if="filteredParents.length === 0"><td :colspan="activeTab === 'PRODUCT' ? 6 : activeTab === 'OP' ? 4 : 4" class="empty-cell">此綁定類型沒有符合條件的 DEV 歷程</td></tr>
          </tbody>
        </table>
      </div>
      <footer class="grid-foot">上層依 QC Item／綁定對象彙整；展開列顯示 ExecuteDate 升冪的變更記錄。此頁唯讀。</footer>
    </section>
    <p class="notice" :class="noticeKind" role="status">{{ notice }}</p>
  </section>
  <section v-else class="pending">DEV 預覽已關閉。<button class="button" type="button" @click="emit('close')">Close</button></section>
</template>

<style scoped>
.binding-page{height:100%;min-height:0;overflow:auto;box-sizing:border-box;padding:14px;background:#f4f3ef;color:#292821;font:13px/1.45 "Segoe UI",Arial,sans-serif}.page-head{display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:12px}.eyebrow{margin:0;color:#9a6b00;font-size:11px;font-weight:700;letter-spacing:.08em}.page-head h1{margin:2px 0;font-size:22px}.subtitle{margin:0;color:#777366}.button{min-height:30px;padding:5px 11px;border:1px solid #aaa79a;border-radius:3px;background:#fff;color:#292821;font:inherit;cursor:pointer}.button:hover{background:#f9edbd;border-color:#c8a300}.button.primary{background:#e5c400;border-color:#c6a800;font-weight:700}.panel{margin-bottom:10px;border:1px solid #d4d1c7;border-radius:4px;background:#fff;overflow:hidden}.filter-panel{padding:10px}.filter-grid{display:grid;grid-template-columns:1.1fr 1.5fr 1.1fr 1.2fr 1.2fr;gap:10px;align-items:end}.toggle-field,.lookup-field,.date-field{display:flex;min-width:0;flex-direction:column;gap:5px;color:#625f55;font-size:11px}.toggle-field>span{display:flex;align-items:center;gap:5px;min-height:18px}.lookup-field>span,.date-field>span{min-height:18px}.filter-grid select,.filter-grid input[type=datetime-local]{width:100%;min-height:31px;box-sizing:border-box;border:1px solid #c8c5bb;border-radius:3px;padding:4px 6px;background:#fff;color:#292821;font:inherit}.filter-grid select:disabled{background:#efeee9;color:#999}.filter-actions{display:flex;justify-content:space-between;align-items:center;gap:8px;margin-top:11px;padding-top:9px;border-top:1px solid #e9e6dc}.filter-actions span{color:#777366;font-size:10px}.tabs{display:flex;align-items:center;gap:3px;padding:7px 8px;border-bottom:1px solid #e4e1d8;background:#faf9f6}.tabs button{padding:6px 12px;border:1px solid #d4d1c7;background:#fff;color:#555246;font:inherit;cursor:pointer}.tabs button.active{background:#fff0ad;border-color:#c6a800;color:#292821;font-weight:700}.result-count{margin-left:auto;color:#777366;font-size:11px}.table-scroll{max-height:560px;overflow:auto}.history-table,.detail-table{width:100%;border-collapse:collapse;font-size:12px}.history-table th,.history-table td,.detail-table th,.detail-table td{padding:7px 8px;border-bottom:1px solid #eceae4;text-align:left;vertical-align:top}.history-table>thead th{position:sticky;top:0;background:#f3f1ea;color:#5e5b50;font-weight:600}.parent-row{background:#fff}.parent-row>td:first-child{white-space:nowrap}.detail-cell{min-width:380px;padding:5px!important}.detail-cell details{width:100%}.detail-cell summary{padding:5px 7px;color:#655614;background:#fff9dc;cursor:pointer;font-size:11px}.detail-table{margin-top:4px;font-size:11px}.detail-table th{background:#f7f6f2;color:#625f55;font-weight:600}.detail-table td{white-space:nowrap}.status-pill{display:inline-block;padding:2px 6px;border:1px solid #d8d3c4;border-radius:3px;background:#fff;font-size:10px}.status-0{border-color:#abd0ae;background:#eff8ef;color:#346238}.status-1{border-color:#e2d394;background:#fff8d8;color:#63551b}.status-2{border-color:#e4b9b1;background:#fff0ed;color:#922f25}.empty-state,.empty-cell{text-align:center!important;color:#888!important;padding:22px!important}.grid-foot{padding:7px 10px;border-top:1px solid #eceae4;color:#777366;font-size:10px}.notice{position:sticky;bottom:0;margin:0;padding:8px 10px;border:1px solid #e3d9a8;border-radius:3px;background:#fff8d8;color:#63551b}.notice.success{border-color:#acd0ae;background:#eff8ef;color:#346238}.notice.error{border-color:#e0b2aa;background:#fff0ed;color:#922f25}.pending{padding:18px;color:#777366}.pending .button{margin-left:10px}
@media(max-width:1180px){.filter-grid{grid-template-columns:repeat(3,minmax(170px,1fr))}}
@media(max-width:800px){.filter-grid{grid-template-columns:repeat(2,minmax(160px,1fr))}.tabs{flex-wrap:wrap}.result-count{width:100%;margin-left:0}}
</style>
