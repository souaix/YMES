<script setup lang="ts">
import { computed, ref } from 'vue'
import { loadDevelopmentLabels } from './labelDataDevelopment'
import { loadDevelopmentProductRelations, productKey, rowsKey, seedRows, type ProductRelation } from './productBasisDevelopment'

type Mapping = { productNo: string; customerNo: string; opNo: string; labelSource: number; labelType: string; labelNo: string }
type MappingRow = Mapping & { creator: string; createDate: string; reviser?: string; reviseDate?: string; dataStamp: number; lastActionUserNo?: string; lastActionDate?: string }
type Snapshot = { rows: MappingRow[]; bindings: Record<string, string> }
type Mode = 'add' | 'edit' | 'copy' | ''
const props = defineProps<{ developmentPreview: boolean; currentUserNo: string }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.prd108-label-map.v1'
const audit = { creator: 'DEV001', createDate: '2026-11-19T08:00:00', dataStamp: 0 }
const seed: Snapshot = { rows: [{ ...audit, productNo: 'DEV-PRD-1002', customerNo: 'DEV-CUST-01', opNo: 'DEV-OP-10', labelSource: 2, labelType: 'SN', labelNo: 'DEV-PRD-POST-01' }], bindings: {} }
const customers = ['DEV-CUST-01', 'DEV-CUST-02']
const opCatalog = ['DEV-OP-10', 'DEV-OP-20', 'DEV-OP-30']

function read<T>(key: string, fallback: T): T {
  if (!props.developmentPreview) return fallback
  try { const value = localStorage.getItem(key); return value ? JSON.parse(value) : fallback }
  catch { return fallback }
}

const data = ref(read(storageKey, seed))
const productRows = props.developmentPreview ? read(rowsKey, seedRows) : []
const products = productRows.filter(row => Number(row.issueState) === 2 && Number(row.curVersion) === 1)
const relationLoadError = ref('')
let relations: Record<string, ProductRelation> = {}
try { if (props.developmentPreview) relations = loadDevelopmentProductRelations(productRows) }
catch (cause) { relationLoadError.value = cause instanceof Error ? cause.message : 'DEV 產品關聯無法載入。' }
const labels = props.developmentPreview ? loadDevelopmentLabels() : []
const user = computed(() => props.currentUserNo || 'DEV001')
const sources = [1, 2, 3]
const keyOf = (row: Mapping) => JSON.stringify([row.productNo, row.customerNo, row.labelType, row.opNo, row.labelSource])
const bindingKey = (product: string, version: string, op: string) => JSON.stringify([product, version, op])
const activeKey = ref('')
const marked = ref<string[]>([])
const fields = ['productNo', 'customerNo', 'opNo', 'labelType', 'labelNo'] as const
const filter = ref({ productNo: '', customerNo: '', opNo: '', labelType: '', labelNo: '', labelSource: 0 })
const query = ref({ ...filter.value })
const visible = computed(() => data.value.rows.filter(row => (!query.value.labelSource || row.labelSource === query.value.labelSource)
  && fields.every(field => String(row[field]).toUpperCase().includes(query.value[field].trim().toUpperCase()))))
const active = computed(() => visible.value.find(row => keyOf(row) === activeKey.value) ?? visible.value[0])
const mode = ref<Mode>('')
const draft = ref<Mapping>({ productNo: '', customerNo: '', opNo: 'N/A', labelSource: 1, labelType: '', labelNo: '' })
const labelOptions = computed(() => labels.filter(row => row.labelSource === draft.value.labelSource))
const options = computed(() => labelOptions.value.filter(row => row.labelType === draft.value.labelType))
const typeOptions = computed(() => [...new Set(labelOptions.value.map(row => row.labelType))])
const error = ref('')
const notice = ref('')
const batchOpen = ref(false)
const batchKeyword = ref('')
const batchQuery = ref('')
const batchSelected = ref<string[]>([])
const batchMessages = ref<Record<string, string>>({})
const batchRows = computed(() => data.value.rows.filter(row => row.labelSource === 2 && row.productNo.includes(batchQuery.value)))

function productVersion(productNo: string): string { return String(products.find(row => row.productNo === productNo)?.productVersion ?? '') }

function operations(productNo: string): string[] {
  if (relationLoadError.value) return []
  const version = productVersion(productNo)
  if (!version) return []
  const assigned = relations[productKey(productNo, version)]?.parameters?.ops ?? []
  return assigned.length ? [...new Set(assigned.map(row => String(row.opNo)).filter(Boolean))]
    : productNo === 'DEV-PRD-1002' ? opCatalog : []
}

function saveData(next: Snapshot): boolean {
  if (!props.developmentPreview) return false
  try { localStorage.setItem(storageKey, JSON.stringify(next)) }
  catch { error.value = '無法儲存本機 DEV 標籤對應。'; return false }
  data.value = next; error.value = ''; return true
}

function open(next: Mode): void {
  const source = next === 'add' ? undefined : active.value
  draft.value = source ? { ...source } : { productNo: '', customerNo: '', opNo: 'N/A', labelSource: 1, labelType: '', labelNo: '' }
  error.value = ''; mode.value = next
}

function changeSource(): void {
  Object.assign(draft.value, { productNo: '', customerNo: '', opNo: draft.value.labelSource === 2 ? '' : 'N/A', labelType: '', labelNo: '' })
}

function save(close: boolean): void {
  const record = draft.value
  if (!record.productNo || !record.customerNo || !record.opNo || !options.value.some(row => row.labelNo === record.labelNo)) { error.value = '請選取產品、客戶、作業站及來源相符的標籤。'; return }
  if (record.labelSource === 2 && (record.productNo === '*' || record.customerNo === '*')) { error.value = '過帳來源須指定個別產品與客戶。'; return }
  const key = keyOf(record)
  const existing = data.value.rows.find(row => keyOf(row) === key)
  if (mode.value !== 'edit' && existing) { error.value = '產品、客戶、作業站、類型與來源的組合已存在。'; return }
  if (mode.value === 'edit' && !existing) return
  const now = new Date().toISOString()
  const saved: MappingRow = existing ? { ...existing, labelNo: record.labelNo, reviser: user.value, reviseDate: now, dataStamp: existing.dataStamp + 1 }
    : { productNo: record.productNo, customerNo: record.customerNo, opNo: record.opNo, labelSource: record.labelSource, labelType: record.labelType, labelNo: record.labelNo, creator: user.value, createDate: now, dataStamp: 0 }
  const next = existing ? data.value.rows.map(row => keyOf(row) === key ? saved : row) : [...data.value.rows, saved]
  if (!saveData({ ...data.value, rows: next })) return
  filter.value = { productNo: '', customerNo: '', opNo: '', labelType: '', labelNo: '', labelSource: 0 }; query.value = { ...filter.value }
  marked.value = []; activeKey.value = key; mode.value = close ? '' : 'edit'; notice.value = '標籤對應已儲存。'
}

function remove(): void {
  if (!active.value || !window.confirm('確定刪除選取的標籤對應？')) return
  const keys = new Set([...marked.value, keyOf(active.value)])
  if (saveData({ ...data.value, rows: data.value.rows.filter(row => !keys.has(keyOf(row))) })) { marked.value = []; notice.value = '對應已刪除。' }
}

function targetOps(row: MappingRow): string[] {
  const ops = operations(row.productNo)
  if (row.opNo !== '*') return ops.includes(row.opNo) ? [row.opNo] : []
  const specific = new Set(data.value.rows.filter(other => other.labelSource === 2 && other.productNo === row.productNo
    && other.customerNo === row.customerNo && other.labelType === row.labelType && other.opNo !== '*').map(other => other.opNo))
  return ops.filter(op => !specific.has(op))
}

function currentBinding(row: MappingRow): string {
  const version = productVersion(row.productNo)
  return [...new Set(targetOps(row).map(op => data.value.bindings[bindingKey(row.productNo, version, op)]).filter(Boolean))].join('、')
}

function applyBatch(): void {
  const selected = new Set(batchSelected.value)
  if (!selected.size || !window.confirm('確定對選取的資料寫入 DEV 作業標籤？')) return
  const bindings = { ...data.value.bindings }
  const messages: Record<string, string> = {}
  const now = new Date().toISOString()
  const next = data.value.rows.map(row => {
    const key = keyOf(row)
    if (!selected.has(key)) return row
    const version = productVersion(row.productNo)
    const ops = targetOps(row)
    if (!version || !ops.length || !labels.some(label => label.labelNo === row.labelNo)) {
      messages[key] = '未找到目前核准版、可寫入作業或標籤。'; return row
    }
    for (const op of ops) bindings[bindingKey(row.productNo, version, op)] = row.labelNo
    messages[key] = `已更新 ${ops.length} 個作業站。`
    return { ...row, lastActionUserNo: user.value, lastActionDate: now }
  })
  if (saveData({ rows: next, bindings })) { batchMessages.value = messages; batchSelected.value = []; notice.value = 'DEV 批次預設已執行。' }
}

const titles = { productNo: 'ProductNo', customerNo: 'CustomerNo', opNo: 'OPNo', labelType: 'LabelType', labelNo: 'LabelNo' }
</script>

<template>
  <main class="map-master">
    <p v-if="relationLoadError" role="alert" class="error">{{ relationLoadError }}</p>
    <header><h1>產品標籤對應設定</h1><button @click="emit('close')">關閉</button></header>
    <p v-if="!developmentPreview">標籤對應等待正式 API 接入。</p>
    <template v-else>
      <form class="toolbar" @submit.prevent="query = { ...filter }; marked = []">
        <label>LabelSource <select v-model.number="filter.labelSource"><option :value="0">全部</option><option v-for="source in sources" :key="source" :value="source">{{ source }}</option></select></label>
        <label v-for="field in fields" :key="field">{{ titles[field] }} <input v-model="filter[field]" /></label><button>查詢</button>
      </form>
      <div class="toolbar"><button @click="open('add')">ADD</button><button :disabled="!active" @click="open('edit')">EDIT</button><button :disabled="!active" @click="remove">DEL</button><button :disabled="!active" @click="open('copy')">Copy</button><button @click="batchOpen = true; batchSelected = []">ParDefault</button></div>
      <div class="grid"><table><thead><tr><th>選取</th><th>LabelSource</th><th>ProductNo</th><th>CustomerNo</th><th>OPNo</th><th>LabelType</th><th>LabelNo</th><th>Creator</th><th>Reviser</th><th>LastActionUserNo</th></tr></thead><tbody>
        <tr v-for="row in visible" :key="keyOf(row)" :class="{ selected: active && keyOf(active) === keyOf(row) }" @click="activeKey = keyOf(row)"><td><input v-model="marked" type="checkbox" :value="keyOf(row)" :aria-label="`選取 ${row.productNo} ${row.opNo}`" /></td><td>{{ row.labelSource }}</td><td><button @click="activeKey = keyOf(row)">{{ row.productNo }}</button></td><td>{{ row.customerNo }}</td><td>{{ row.opNo }}</td><td>{{ row.labelType }}</td><td>{{ row.labelNo }}</td><td>{{ row.creator }}</td><td>{{ row.reviser }}</td><td>{{ row.lastActionUserNo }}</td></tr>
        <tr v-if="!visible.length"><td colspan="10">沒有符合的標籤對應。</td></tr></tbody></table></div>
      <p v-if="error && !mode && !batchOpen" role="alert" class="error">{{ error }}</p><p v-if="notice" role="status">{{ notice }}</p>
    </template>
    <div v-if="mode" class="overlay"><form class="dialog editor" role="dialog" aria-modal="true" aria-label="產品標籤對應" @submit.prevent="save(true)">
      <h2>標籤對應 {{ mode.toUpperCase() }}</h2>
      <label>LabelSource <select v-model.number="draft.labelSource" :disabled="mode === 'edit'" @change="changeSource"><option v-for="source in sources" :key="source" :value="source">{{ source === 2 ? '2 過帳' : source }}</option></select></label>
      <label>ProductNo <select v-model="draft.productNo" :disabled="mode === 'edit'" @change="draft.opNo = draft.labelSource === 2 ? '' : 'N/A'"><option value="">請選擇</option><option v-if="draft.labelSource !== 2">*</option><option v-for="row in products" :key="row.productNo" :value="row.productNo">{{ row.productNo }} / {{ row.productVersion }}</option></select></label>
      <label>CustomerNo <select v-model="draft.customerNo" :disabled="mode === 'edit'"><option value="">請選擇</option><option v-if="draft.labelSource !== 2">*</option><option v-for="code in customers" :key="code">{{ code }}</option></select></label>
      <label>OPNo <select v-model="draft.opNo" :disabled="mode === 'edit' || draft.labelSource !== 2"><option v-if="draft.labelSource !== 2">N/A</option><template v-else><option value="">請選擇</option><option>*</option><option v-for="op in operations(draft.productNo)" :key="op">{{ op }}</option></template></select></label>
      <label>LabelType <select v-model="draft.labelType" :disabled="mode === 'edit'" @change="draft.labelNo = ''"><option value="">請選擇</option><option v-for="type in typeOptions" :key="type">{{ type }}</option></select></label>
      <label>LabelNo <select v-model="draft.labelNo"><option value="">請選擇</option><option v-for="row in options" :key="row.labelNo" :value="row.labelNo">{{ row.labelNo }} · {{ row.description }}</option></select></label>
      <p v-if="error" role="alert" class="error">{{ error }}</p><footer><button>Confirm</button><button type="button" @click="save(false)">Apply</button><button type="button" @click="mode = ''; error = ''">Close</button></footer>
    </form></div>
    <div v-if="batchOpen" class="overlay"><section class="dialog batch" role="dialog" aria-modal="true" aria-label="產品列印標籤批次預設">
      <header><h2>產品列印標籤批次預設</h2><button @click="batchOpen = false; error = ''">關閉</button></header>
      <form class="toolbar" @submit.prevent="batchQuery = batchKeyword.trim().toUpperCase(); batchSelected = []"><label>ProductNo <input v-model="batchKeyword" /></label><button>查詢</button></form>
      <div class="grid"><table><thead><tr><th>選取</th><th>ProductNo</th><th>ProductVersion</th><th>CustomerNo</th><th>OPNo</th><th>LabelType</th><th>LabelNo</th><th>CurBindLabel</th><th>Message</th></tr></thead><tbody>
        <tr v-for="row in batchRows" :key="keyOf(row)"><td><input v-model="batchSelected" type="checkbox" :value="keyOf(row)" :aria-label="`預設 ${row.productNo} ${row.opNo}`" /></td><td>{{ row.productNo }}</td><td>{{ productVersion(row.productNo) }}</td><td>{{ row.customerNo }}</td><td>{{ row.opNo }}</td><td>{{ row.labelType }}</td><td>{{ row.labelNo }}</td><td>{{ currentBinding(row) }}</td><td>{{ batchMessages[keyOf(row)] }}</td></tr>
        <tr v-if="!batchRows.length"><td colspan="9">沒有符合的過帳對應。</td></tr></tbody></table></div>
      <p v-if="error" role="alert" class="error">{{ error }}</p><footer><button :disabled="!batchRows.length" @click="batchSelected = batchRows.map(keyOf)">全選</button><button :disabled="!batchSelected.length" @click="batchSelected = []">取消全選</button><button :disabled="!batchSelected.length" @click="applyBatch">Default</button></footer>
    </section></div>
  </main>
</template>

<style scoped>
.map-master{display:flex;flex-direction:column;gap:8px;min-width:0;padding:10px;background:#fff;color:#292820;font:12px "Segoe UI","Microsoft JhengHei",sans-serif}header,.toolbar,footer{display:flex;align-items:center;gap:8px}header{justify-content:space-between}.toolbar{flex-wrap:wrap;padding:6px;background:#f5f3eb;border:1px solid #dedbd0}.toolbar input{width:105px}h1,h2,p{margin:0}h1{font-size:17px}h2{font-size:14px}
.grid{overflow:auto;border:1px solid #d9d6cb}table{width:100%;border-collapse:collapse;text-align:left;white-space:nowrap}th,td{padding:6px;border:1px solid #e3e0d6}thead{background:#f5f3eb}.selected{background:#fff2c6}button,input,select{font:inherit}button{padding:4px 10px;border:1px solid #aaa797;border-radius:3px;background:#f8f7f2;color:inherit}button:disabled{opacity:.5}.error{color:#a32020}
.overlay{position:fixed;inset:0;z-index:91;display:grid;place-items:center;padding:12px;background:#0005}.dialog{display:grid;gap:10px;width:min(940px,95vw);max-height:90vh;overflow:auto;padding:16px;background:#fff;box-shadow:0 8px 30px #0004}.editor{width:min(560px,95vw)}.editor label{display:grid;gap:3px}.editor select{padding:5px;border:1px solid #aaa797;background:#fff5d6}footer{justify-content:flex-end}
</style>
