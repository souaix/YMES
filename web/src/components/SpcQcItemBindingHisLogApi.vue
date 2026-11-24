<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

type BindType = 1 | 2 | 3 | 4
type QcItemType = 0 | 1 | 2 | 3 | 4
type ExecuteStatus = 0 | 1 | 2
type TabKey = 'MATERIALTYPE' | 'MATERIALNO' | 'OP' | 'PRODUCT'
interface ItemOption { no: string; name: string }
interface ItemOptionsResponse { totalCount: number; page: number; pageSize: number; pageCount: number; items: ItemOption[] }
interface BindingEvent {
  id: string; executeStatus: ExecuteStatus; executeDate: string; executor: string
  inLine: number | null; executeOrder: number | null; inspTypeFai: number | null; inspTypeIpqc: number | null; inspTypePqc: number | null
}
interface BindingParent {
  id: string; qcItemType: QcItemType; qcItemNo: string; qcItemName: string; bindType: BindType
  productNo: string; productVersion: string; opNo: string; materialType: string; materialNo: string; events: BindingEvent[]
}
interface HistoryResponse { bindType: BindType; totalCount: number; page: number; pageSize: number; pageCount: number; parents: BindingParent[] }
interface QueryState {
  qcItemTypeEnabled: boolean; qcItemType: QcItemType | ''
  qcItemNo: string; executeStatusEnabled: boolean; executeStatus: ExecuteStatus | ''
  fromDate: string; endDate: string
}
interface TabData { page: number; response: HistoryResponse | null }

const props = defineProps<{ apiBaseUrl: string }>()
const emit = defineEmits<{ close: [] }>()
const typeOptions: Array<{ value: QcItemType; label: string }> = [
  { value: 0, label: 'Attribute (0)' }, { value: 1, label: 'Variable (1)' }, { value: 2, label: 'Variable Group (2)' },
  { value: 3, label: 'Attribute Order Group (3)' }, { value: 4, label: 'PMI Group (4)' },
]
const statusOptions: Array<{ value: ExecuteStatus; label: string }> = [
  { value: 0, label: '0 · 新增綁定' }, { value: 1, label: '1 · 更新綁定' }, { value: 2, label: '2 · 解除綁定' },
]
const tabs: Array<{ key: TabKey; label: string; bindType: BindType }> = [
  { key: 'PRODUCT', label: 'Product', bindType: 4 }, { key: 'OP', label: 'OP', bindType: 3 },
  { key: 'MATERIALTYPE', label: 'Material Type', bindType: 1 }, { key: 'MATERIALNO', label: 'MaterialNo', bindType: 2 },
]

function localInput(value: Date): string {
  const pad = (part: number) => String(part).padStart(2, '0')
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}:${pad(value.getSeconds())}`
}
const initialTo = new Date()
const initialFrom = new Date(initialTo)
initialFrom.setDate(initialFrom.getDate() - 7)
initialFrom.setHours(0, 0, 0, 0)
const query = ref<QueryState>({
  qcItemTypeEnabled: false, qcItemType: '', qcItemNo: '', executeStatusEnabled: false, executeStatus: '',
  fromDate: localInput(initialFrom), endDate: localInput(initialTo),
})
const appliedQuery = ref<QueryState | null>(null)
const activeTab = ref<TabKey>('PRODUCT')
const dataByTab = reactive<Record<TabKey, TabData>>({
  PRODUCT: { page: 1, response: null }, OP: { page: 1, response: null },
  MATERIALTYPE: { page: 1, response: null }, MATERIALNO: { page: 1, response: null },
})
const itemSearch = ref('')
const itemPage = ref(1)
const itemOptions = ref<ItemOption[]>([])
const itemPageCount = ref(0)
const itemTotal = ref(0)
const searched = ref(false)
const loading = ref(false)
const itemLoading = ref(false)
const error = ref('')
const notice = ref('設定日期區間後按 Query；查詢結果來自 YMES 綁定歷程。')
const noticeKind = ref<'info' | 'success' | 'error'>('info')
let requestVersion = 0
let itemRequestVersion = 0

const currentTab = computed(() => tabs.find((tab) => tab.key === activeTab.value)!)
const currentData = computed(() => dataByTab[activeTab.value])
const currentResponse = computed(() => currentData.value.response)
const parentCount = computed(() => currentResponse.value?.totalCount ?? 0)
const childCount = computed(() => currentResponse.value?.parents.reduce((sum, parent) => sum + parent.events.length, 0) ?? 0)
const tabCountLabel = computed(() => currentResponse.value ? `${parentCount.value} 組 · 本頁 ${childCount.value} 筆明細` : '')

function apiUrl(path: string, values: Record<string, string | number | null | undefined>): URL {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  const root = new URL(base, window.location.origin)
  if (root.protocol !== 'https:' && root.hostname !== 'localhost' && root.hostname !== '127.0.0.1')
    throw new Error('API 連線必須使用 HTTPS。')
  root.pathname = `${root.pathname.replace(/\/+$/, '')}/api/spc/qc-item-binding-history${path}`
  root.search = ''
  for (const [key, value] of Object.entries(values)) if (value !== null && value !== undefined && value !== '') root.searchParams.set(key, String(value))
  return root
}

async function readJson<T>(response: Response): Promise<T> {
  const payload = await response.json().catch(() => ({})) as { detail?: string; title?: string }
  if (!response.ok) throw new Error(payload.detail || payload.title || `API 回應 ${response.status}`)
  return payload as T
}

function onTypeChange(event: Event): void {
  const value = (event.target as HTMLSelectElement).value
  query.value.qcItemType = value === '' ? '' : Number(value) as QcItemType
  if (value !== '') query.value.qcItemTypeEnabled = true
  query.value.qcItemNo = ''
  itemSearch.value = ''
  itemOptions.value = []
  itemPage.value = 1
  itemPageCount.value = 0
  itemTotal.value = 0
}

async function searchItems(page = 1): Promise<void> {
  error.value = ''
  if (query.value.qcItemType === '') {
    error.value = '請先選擇 QC Item Type。'
    notice.value = error.value
    noticeKind.value = 'error'
    return
  }
  const version = ++itemRequestVersion
  itemLoading.value = true
  try {
    const url = apiUrl('/items', { qcItemType: query.value.qcItemType, search: itemSearch.value.trim(), page, pageSize: 100 })
    const response = await readJson<ItemOptionsResponse>(await fetch(url, { credentials: 'include', headers: { Accept: 'application/json' } }))
    if (version !== itemRequestVersion) return
    itemPage.value = response.page
    itemPageCount.value = response.pageCount
    itemTotal.value = response.totalCount
    itemOptions.value = response.items
    if (query.value.qcItemNo && !response.items.some((item) => item.no === query.value.qcItemNo)) {
      itemOptions.value = [{ no: query.value.qcItemNo, name: '目前選取' }, ...response.items]
    }
  } catch (caught) {
    if (version !== itemRequestVersion) return
    error.value = caught instanceof Error ? caught.message : 'QC Item 查詢失敗。'
    notice.value = error.value
    noticeKind.value = 'error'
  } finally {
    if (version === itemRequestVersion) itemLoading.value = false
  }
}

function valuesFor(queryState: QueryState, bindType: BindType, page: number): Record<string, string | number | null> {
  const useType = queryState.qcItemTypeEnabled && queryState.qcItemType !== ''
  return {
    bindType,
    qcItemType: useType ? queryState.qcItemType : null,
    qcItemNo: useType && queryState.qcItemNo ? queryState.qcItemNo : null,
    executeStatus: queryState.executeStatusEnabled && queryState.executeStatus !== '' ? queryState.executeStatus : null,
    fromDate: queryState.fromDate,
    endDate: queryState.endDate,
    page,
    pageSize: 25,
  }
}

async function loadHistory(tab: TabKey, page: number, criteria: QueryState): Promise<void> {
  error.value = ''
  const target = tabs.find((item) => item.key === tab)!
  const version = ++requestVersion
  loading.value = true
  try {
    const url = apiUrl('', valuesFor(criteria, target.bindType, page))
    const response = await readJson<HistoryResponse>(await fetch(url, { credentials: 'include', headers: { Accept: 'application/json' } }))
    if (version !== requestVersion) return
    dataByTab[tab] = { page: response.page, response }
    searched.value = true
    notice.value = `${target.label} 查詢完成：${response.totalCount} 組綁定，本頁 ${response.parents.reduce((sum, parent) => sum + parent.events.length, 0)} 筆歷程。`
    noticeKind.value = 'success'
  } catch (caught) {
    if (version !== requestVersion) return
    error.value = caught instanceof Error ? caught.message : '綁定紀錄查詢失敗。'
    notice.value = error.value
    noticeKind.value = 'error'
  } finally {
    if (version === requestVersion) loading.value = false
  }
}

async function runQuery(): Promise<void> {
  error.value = ''
  if (!query.value.fromDate || !query.value.endDate) error.value = 'From Date 與 To Date 為必填條件。'
  else if (query.value.fromDate > query.value.endDate) error.value = 'From Date 必須早於或等於 To Date。'
  else if (new Date(query.value.endDate).getTime() - new Date(query.value.fromDate).getTime() > 366 * 24 * 60 * 60 * 1000) error.value = '日期區間不可超過 366 天。'
  else if (query.value.qcItemTypeEnabled && query.value.qcItemType !== '' && !query.value.qcItemNo) error.value = '已啟用 QC Item Type 篩選時，請先搜尋並選擇 QC Item 或 Group。'
  if (error.value) {
    notice.value = error.value
    noticeKind.value = 'error'
    return
  }
  appliedQuery.value = { ...query.value }
  for (const tab of tabs) dataByTab[tab.key] = { page: 1, response: null }
  await loadHistory(activeTab.value, 1, appliedQuery.value)
}

async function selectTab(tab: TabKey): Promise<void> {
  activeTab.value = tab
  if (!appliedQuery.value) return
  await loadHistory(tab, dataByTab[tab].page, appliedQuery.value)
}

async function changePage(page: number): Promise<void> {
  if (!appliedQuery.value || !currentResponse.value || page < 1 || page > currentResponse.value.pageCount) return
  dataByTab[activeTab.value].page = page
  await loadHistory(activeTab.value, page, appliedQuery.value)
}

function typeLabel(value: QcItemType): string { return typeOptions.find((option) => option.value === value)?.label ?? String(value) }
function statusLabel(value: ExecuteStatus): string { return statusOptions.find((option) => option.value === value)?.label ?? String(value) }
function showValue(value: number | string | null): string { return value === null || value === '' ? '—' : String(value) }
function formatDate(value: string): string { return value.replace('T', ' ') }
</script>

<template>
  <section class="binding-page" aria-label="SPC 品管項目綁定紀錄">
    <header class="page-head"><div><p class="eyebrow">SPC / SC216</p><h1>品管項目綁定紀錄表</h1><p class="subtitle">查詢 YMES QC Item 與產品、作業站及物料的唯讀綁定歷程。</p></div><button class="button" type="button" @click="emit('close')">Close (X)</button></header>

    <section class="filter-panel panel">
      <div class="filter-grid">
        <label class="toggle-field"><span><input v-model="query.qcItemTypeEnabled" type="checkbox"> QC Item Type</span><select :value="query.qcItemType" @change="onTypeChange"><option value="">全部類型</option><option v-for="option in typeOptions" :key="option.value" :value="option.value">{{ option.label }}</option></select></label>
        <div class="lookup-field"><label for="qc-item-search">QC Item / Group</label><div class="lookup-row"><input id="qc-item-search" v-model="itemSearch" type="search" maxlength="100" :disabled="query.qcItemType === ''" placeholder="輸入編號或名稱"><button class="button" type="button" :disabled="query.qcItemType === '' || itemLoading" @click="searchItems(1)">{{ itemLoading ? '搜尋中…' : '搜尋' }}</button></div><select v-model="query.qcItemNo" :disabled="query.qcItemType === ''"><option value="">選擇 Issue = 2 的項目</option><option v-for="item in itemOptions" :key="item.no" :value="item.no">{{ item.no }} — {{ item.name }}</option></select><span v-if="itemPageCount > 1" class="lookup-pages"><button class="text-button" type="button" :disabled="itemPage <= 1 || itemLoading" @click="searchItems(itemPage - 1)">上一頁</button>{{ itemPage }} / {{ itemPageCount }} · {{ itemTotal }} 筆<button class="text-button" type="button" :disabled="itemPage >= itemPageCount || itemLoading" @click="searchItems(itemPage + 1)">下一頁</button></span></div>
        <label class="toggle-field"><span><input v-model="query.executeStatusEnabled" type="checkbox"> ExecuteStatus</span><select v-model="query.executeStatus"><option value="">全部狀態</option><option v-for="option in statusOptions" :key="option.value" :value="option.value">{{ option.label }}</option></select></label>
        <label class="date-field"><span>Binding His Time · From</span><input v-model="query.fromDate" type="datetime-local" step="1"></label>
        <label class="date-field"><span>To</span><input v-model="query.endDate" type="datetime-local" step="1"></label>
      </div>
      <footer class="filter-actions"><span>預設最近 7 天；From／To 日期端點皆包含，單次最多 366 天。</span><button class="button primary" type="button" :disabled="loading" @click="runQuery">{{ loading ? '查詢中…' : 'Query' }}</button></footer>
    </section>

    <section class="history-panel panel">
      <nav class="tabs" aria-label="綁定類別">
        <button v-for="tab in tabs" :key="tab.key" :class="{ active: activeTab === tab.key }" type="button" @click="selectTab(tab.key)">{{ tab.label }}</button>
        <span v-if="searched" class="result-count">{{ tabCountLabel }}</span>
      </nav>
      <div v-if="!searched && !loading" class="empty-state">設定日期區間後按 Query 顯示綁定歷程。</div>
      <div v-else-if="loading" class="empty-state" role="status">正在讀取 YMES 綁定紀錄…</div>
      <div v-else class="table-scroll">
        <table class="history-table">
          <thead><tr><th>QC Item Type</th><th>QC Item No</th><th v-if="activeTab === 'PRODUCT'">Product No</th><th v-if="activeTab === 'PRODUCT'">Product Version</th><th v-if="activeTab === 'PRODUCT' || activeTab === 'OP'">OP No</th><th v-if="activeTab === 'MATERIALTYPE'">Material Type</th><th v-if="activeTab === 'MATERIALNO'">Material No</th><th>History Detail</th></tr></thead>
          <tbody>
            <template v-for="parent in currentResponse?.parents ?? []" :key="parent.id">
              <tr class="parent-row">
                <td>{{ typeLabel(parent.qcItemType) }}</td><td>{{ parent.qcItemNo }}<span v-if="parent.qcItemName"> — {{ parent.qcItemName }}</span></td>
                <td v-if="activeTab === 'PRODUCT'">{{ showValue(parent.productNo) }}</td><td v-if="activeTab === 'PRODUCT'">{{ showValue(parent.productVersion) }}</td>
                <td v-if="activeTab === 'PRODUCT' || activeTab === 'OP'">{{ showValue(parent.opNo) }}</td><td v-if="activeTab === 'MATERIALTYPE'">{{ showValue(parent.materialType) }}</td><td v-if="activeTab === 'MATERIALNO'">{{ showValue(parent.materialNo) }}</td>
                <td class="detail-cell"><details open><summary>{{ parent.events.length }} 筆綁定歷程</summary>
                  <table class="detail-table"><thead><tr><th v-if="activeTab === 'PRODUCT' || activeTab === 'OP'">InLine</th><th v-if="activeTab === 'PRODUCT' || activeTab === 'OP'">ExecuteOrder</th><th v-if="activeTab === 'PRODUCT' || activeTab === 'OP'">InspectType FAI</th><th v-if="activeTab === 'PRODUCT' || activeTab === 'OP'">InspectType IPQC</th><th v-if="activeTab === 'PRODUCT' || activeTab === 'OP'">InspectType PQC</th><th>ExecuteStatus</th><th>ExecuteDate</th><th>Executor</th></tr></thead>
                    <tbody><tr v-for="event in parent.events" :key="`${parent.id}-${event.id}`">
                      <td v-if="activeTab === 'PRODUCT' || activeTab === 'OP'">{{ showValue(event.inLine) }}</td><td v-if="activeTab === 'PRODUCT' || activeTab === 'OP'">{{ showValue(event.executeOrder) }}</td><td v-if="activeTab === 'PRODUCT' || activeTab === 'OP'">{{ showValue(event.inspTypeFai) }}</td><td v-if="activeTab === 'PRODUCT' || activeTab === 'OP'">{{ showValue(event.inspTypeIpqc) }}</td><td v-if="activeTab === 'PRODUCT' || activeTab === 'OP'">{{ showValue(event.inspTypePqc) }}</td>
                      <td><span :class="['status-pill', `status-${event.executeStatus}`]">{{ statusLabel(event.executeStatus) }}</span></td><td>{{ formatDate(event.executeDate) }}</td><td>{{ event.executor }}</td>
                    </tr><tr v-if="parent.events.length === 0"><td :colspan="activeTab === 'PRODUCT' || activeTab === 'OP' ? 8 : 3" class="empty-cell">沒有符合條件的明細</td></tr></tbody>
                  </table>
                </details></td>
              </tr>
            </template>
            <tr v-if="currentResponse && currentResponse.parents.length === 0"><td :colspan="activeTab === 'PRODUCT' ? 6 : activeTab === 'OP' ? 4 : 4" class="empty-cell">此綁定類型沒有符合條件的歷程</td></tr>
          </tbody>
        </table>
      </div>
      <footer class="grid-foot"><span>每頁最多 25 組綁定對象；事件依 ExecuteDate 升冪，父子對象以完整綁定鍵關聯。</span><span v-if="currentResponse?.pageCount && currentResponse.pageCount > 1" class="pager"><button class="text-button" type="button" :disabled="currentResponse.page <= 1 || loading" @click="changePage(currentResponse.page - 1)">上一頁</button>{{ currentResponse.page }} / {{ currentResponse.pageCount }}<button class="text-button" type="button" :disabled="currentResponse.page >= currentResponse.pageCount || loading" @click="changePage(currentResponse.page + 1)">下一頁</button></span></footer>
    </section>
    <p class="notice" :class="noticeKind" role="status">{{ error || notice }}</p>
  </section>
</template>

<style scoped>
.binding-page{height:100%;min-height:0;overflow:auto;box-sizing:border-box;padding:14px;background:#f4f3ef;color:#292821;font:13px/1.45 "Segoe UI",Arial,sans-serif}.page-head{display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:12px}.eyebrow{margin:0;color:#9a6b00;font-size:11px;font-weight:700;letter-spacing:.08em}.page-head h1{margin:2px 0;font-size:22px}.subtitle{margin:0;color:#777366}.button{min-height:30px;padding:5px 11px;border:1px solid #aaa79a;border-radius:3px;background:#fff;color:#292821;font:inherit;cursor:pointer}.button:hover:not(:disabled){background:#f9edbd;border-color:#c8a300}.button:disabled{opacity:.55;cursor:default}.button.primary{background:#e5c400;border-color:#c6a800;font-weight:700}.panel{margin-bottom:10px;border:1px solid #d4d1c7;border-radius:4px;background:#fff;overflow:hidden}.filter-panel{padding:10px}.filter-grid{display:grid;grid-template-columns:1.1fr 1.7fr 1.1fr 1.2fr 1.2fr;gap:10px;align-items:end}.toggle-field,.lookup-field,.date-field{display:flex;min-width:0;flex-direction:column;gap:5px;color:#625f55;font-size:11px}.toggle-field>span{display:flex;align-items:center;gap:5px;min-height:18px}.lookup-field>label,.date-field>span{min-height:18px}.filter-grid select,.filter-grid input[type=datetime-local],.filter-grid input[type=search]{width:100%;min-height:31px;box-sizing:border-box;border:1px solid #c8c5bb;border-radius:3px;padding:4px 6px;background:#fff;color:#292821;font:inherit}.filter-grid select:disabled,.filter-grid input:disabled{background:#efeee9;color:#999}.lookup-field{gap:5px}.lookup-row{display:flex;gap:5px}.lookup-row input{min-width:0;flex:1}.lookup-row button{padding-inline:8px}.lookup-pages,.pager{display:flex;align-items:center;justify-content:center;gap:7px;color:#777366;font-size:10px}.text-button{padding:2px 5px;border:0;background:transparent;color:#735f06;font:inherit;cursor:pointer}.text-button:disabled{color:#aaa;cursor:default}.filter-actions{display:flex;justify-content:space-between;align-items:center;gap:8px;margin-top:11px;padding-top:9px;border-top:1px solid #e9e6dc}.filter-actions span{color:#777366;font-size:10px}.tabs{display:flex;align-items:center;gap:3px;padding:7px 8px;border-bottom:1px solid #e4e1d8;background:#faf9f6}.tabs button{padding:6px 12px;border:1px solid #d4d1c7;background:#fff;color:#555246;font:inherit;cursor:pointer}.tabs button.active{background:#fff0ad;border-color:#c6a800;color:#292821;font-weight:700}.result-count{margin-left:auto;color:#777366;font-size:11px}.table-scroll{max-height:560px;overflow:auto}.history-table,.detail-table{width:100%;border-collapse:collapse;font-size:12px}.history-table th,.history-table td,.detail-table th,.detail-table td{padding:7px 8px;border-bottom:1px solid #eceae4;text-align:left;vertical-align:top}.history-table>thead th{position:sticky;top:0;background:#f3f1ea;color:#5e5b50;font-weight:600}.parent-row{background:#fff}.parent-row>td:first-child{white-space:nowrap}.detail-cell{min-width:380px;padding:5px!important}.detail-cell details{width:100%}.detail-cell summary{padding:5px 7px;color:#655614;background:#fff9dc;cursor:pointer;font-size:11px}.detail-table{margin-top:4px;font-size:11px}.detail-table th{background:#f7f6f2;color:#625f55;font-weight:600}.detail-table td{white-space:nowrap}.status-pill{display:inline-block;padding:2px 6px;border:1px solid #d8d3c4;border-radius:3px;background:#fff;font-size:10px}.status-0{border-color:#abd0ae;background:#eff8ef;color:#346238}.status-1{border-color:#e2d394;background:#fff8d8;color:#63551b}.status-2{border-color:#e4b9b1;background:#fff0ed;color:#922f25}.empty-state,.empty-cell{text-align:center!important;color:#888!important;padding:22px!important}.grid-foot{display:flex;justify-content:space-between;align-items:center;gap:8px;padding:7px 10px;border-top:1px solid #eceae4;color:#777366;font-size:10px}.notice{position:sticky;bottom:0;margin:0;padding:8px 10px;border:1px solid #e3d9a8;border-radius:3px;background:#fff8d8;color:#63551b}.notice.success{border-color:#acd0ae;background:#eff8ef;color:#346238}.notice.error{border-color:#e0b2aa;background:#fff0ed;color:#922f25}
@media(max-width:1180px){.filter-grid{grid-template-columns:repeat(3,minmax(170px,1fr))}}
@media(max-width:800px){.filter-grid{grid-template-columns:repeat(2,minmax(150px,1fr))}.tabs{flex-wrap:wrap}.result-count{width:100%;margin-left:0}.grid-foot{align-items:flex-start;flex-direction:column}}
@media(max-width:520px){.filter-grid{grid-template-columns:1fr}.page-head h1{font-size:19px}}
</style>
