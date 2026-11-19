<script setup lang="ts">
import { computed, ref } from 'vue'
import { rowsKey, seedRows } from './productBasisDevelopment'

type Condition = { source: string; reference: string; output: string; pass: boolean; message: string }
type Detail = { lotNo: string; slotNo: string; waferId: string; checksum: string; totalDice: boolean; goodDice: boolean; failDice: boolean; conditions: Condition[] }
type Rule = {
  customer: string; productType: string; product: string; version: string; op: string
  source: string; reference: string; type: number; moment: number; sourceOp: string; referenceOp: string
  description: string; state: number; stamp: number; creator: string; detail?: Detail
}
type Mode = 'add' | 'edit' | 'copy' | 'detail' | 'batch' | ''
const props = defineProps<{ developmentPreview: boolean; currentUserNo: string }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.pdp04-map-merge.v1'
const templates = [{ name: 'DEV-RAW-MAP', type: 1 }, { name: 'DEV-PROBED-MAP', type: 3 }, { name: 'DEV-AOI-MAP', type: 6 }]
const states = { '-1': '停用', 0: '未核准', 1: '簽核中', 2: '已核准' } as Record<string, string>
const moments = [{ id: 1, label: '進站前' }, { id: 2, label: '出站前' }, { id: 3, label: '出站後' }]
const products = props.developmentPreview ? read(rowsKey, seedRows).filter(row => Number(row.issueState) === 2) : []
const both = { product: '*', version: '*' }
const batchChoices = [both, ...products.map(row => ({ product: String(row.productNo), version: String(row.productVersion) }))]
const rows = ref<Rule[]>(props.developmentPreview ? read(storageKey, [{ ...blank(), creator: 'DEV001' }]) : [])
const filter = ref('')
const stateFilter = ref('all')
const selected = ref('')
const mode = ref<Mode>('')
const draft = ref<Rule>(blank())
const detail = ref<Detail>(newDetail())
const targets = ref<string[]>([])
const error = ref('')
const message = ref('')
const visible = computed(() => rows.value.filter(row =>
  [row.customer, row.productType, row.product, row.op].some(value => value.includes(filter.value.trim().toUpperCase()))
  && (stateFilter.value === 'all' || row.state.toString() === stateFilter.value),
))
const active = computed(() => visible.value.find(row => key(row) === selected.value) ?? visible.value[0])
const editable = computed(() => active.value?.state === 0)
const availableTemplates = computed(() => templates.filter(template => draft.value.type === 1 || [3, 6].includes(template.type)))
const currentUser = computed(() => props.currentUserNo || 'DEV001')

function productKey(row: { product: string; version: string }): string { return JSON.stringify([row.product, row.version]) }
function key(row: Rule): string { return JSON.stringify([row.customer, row.productType, row.product, row.version, row.op]) }

function newDetail(): Detail {
  return { lotNo: '', slotNo: '', waferId: '', checksum: '', totalDice: false, goodDice: false, failDice: false, conditions: [] }
}

function blank(): Rule {
  return { customer: '*', productType: '*', product: '*', version: '*', op: '*', source: templates[0].name, reference: templates[0].name,
    type: 1, moment: 1, sourceOp: '', referenceOp: '', description: '', state: 0, stamp: 0, creator: '' }
}

function read<T>(key: string, fallback: T): T {
  try { return JSON.parse(localStorage.getItem(key) ?? 'null') ?? fallback }
  catch { return fallback }
}

function persist(next: Rule[]): boolean {
  if (!props.developmentPreview) return false
  try { localStorage.setItem(storageKey, JSON.stringify(next)) }
  catch { error.value = '無法儲存本機 DEV 疊圖規則。'; return false }
  rows.value = next
  error.value = ''
  return true
}

function open(next: Exclude<Mode, ''>): void {
  const row = active.value
  if (next !== 'add' && !row) return
  if (next === 'edit' && !editable.value) return
  draft.value = next === 'add' || !row ? blank() : { ...row }
  const source = row?.detail
  detail.value = source ? { ...source, conditions: source.conditions.map(condition => ({ ...condition })) } : newDetail()
  targets.value = []
  error.value = ''
  mode.value = next
}

function changeType(): void {
  draft.value.moment = draft.value.type === 1 ? 1 : 2
  const first = availableTemplates.value[0]?.name ?? ''
  if (!availableTemplates.value.some(template => template.name === draft.value.source)) draft.value.source = first
  if (!availableTemplates.value.some(template => template.name === draft.value.reference)) draft.value.reference = first
}

function saveMaster(): void {
  const next = { ...draft.value }
  for (const field of ['customer', 'productType', 'product', 'version', 'op', 'sourceOp', 'referenceOp'] as const) next[field] = next[field].trim().toUpperCase()
  if (!next.customer || !next.productType || !next.product) { error.value = '客戶、產品類別與產品必填。'; return }
  if (next.product === '*') next.version = '*'
  if (!next.version) { error.value = '請填產品版次。'; return }
  if (!availableTemplates.value.some(item => item.name === next.source) || !availableTemplates.value.some(item => item.name === next.reference)) {
    error.value = '請選擇適用的來源與參考圖譜。'
    return
  }
  if (next.type === 2 && (!next.op || next.op === '*' || !next.sourceOp || !next.referenceOp || ![2, 3].includes(next.moment))) {
    error.value = '測後疊圖須指定站點及來源／參考站，並選出站前或出站後。'
    return
  }
  if (next.type === 1) {
    next.moment = 1
    next.sourceOp = next.referenceOp = ''
    next.op ||= '*'
  }
  const existing = rows.value.find(row => key(row) === key(next))
  if (mode.value === 'edit' && existing?.state !== 0) return
  if (mode.value !== 'edit' && existing) { error.value = '這組客戶、產品與作業站已有設定。'; return }
  if (existing) next.stamp = existing.stamp + 1
  else Object.assign(next, { state: 0, stamp: 0, creator: currentUser.value })
  const updated = existing ? rows.value.map(row => key(row) === key(next) ? next : row) : [...rows.value, next]
  if (persist(updated)) {
    selected.value = key(next)
    filter.value = ''
    stateFilter.value = 'all'
    mode.value = ''
    message.value = '已儲存疊圖規則。'
  }
}

function saveDetail(): void {
  const row = active.value
  if (!row || !editable.value) return
  const conditions = detail.value.conditions.map(item => ({ ...item, source: item.source.trim(), reference: item.reference.trim(), output: item.output.trim(), message: item.message.trim() }))
  if (conditions.some(item => !item.source || !item.reference || (item.pass ? !item.output : !item.message))) {
    error.value = '請填來源、參考值及對應輸出或失敗訊息。'
    return
  }
  const saved = { ...row, detail: { ...detail.value, conditions }, stamp: row.stamp + 1 }
  if (persist(rows.value.map(item => key(item) === key(row) ? saved : item))) {
    message.value = '已儲存規則明細。'
    mode.value = ''
  }
}

function batchCopy(): void {
  const row = active.value
  if (!row || !targets.value.length) return
  const choices = batchChoices.filter(choice => targets.value.includes(productKey(choice)))
  const drafts = choices.map(choice => ({ ...row, ...choice, state: 0, stamp: 0, creator: currentUser.value }))
  const keys = new Set(rows.value.map(key))
  const collisions = drafts.filter(item => keys.has(key(item)))
  if (collisions.length) { error.value = `目標已有規則 ${collisions.map(item => item.product + ' / ' + item.version).join('、')}`; return }
  if (persist([...rows.value, ...drafts])) {
    mode.value = ''
    message.value = `已複製到 ${drafts.length} 個產品版次。`
  }
}

function approve(): void {
  const row = active.value
  if (!row || !editable.value) return
  if (!row.detail) { error.value = '請先儲存疊圖規則明細。'; return }
  if (window.confirm('確定核准疊圖規則？') && persist(rows.value.map(item => key(item) === key(row) ? { ...item, state: 2, stamp: item.stamp + 1 } : item))) {
    stateFilter.value = 'all'
    message.value = '已核准疊圖規則。'
  }
}

function remove(): void {
  const row = active.value
  if (!row || ![-1, 0, 2].includes(row.state) || !window.confirm('確定刪除疊圖主檔與明細？')) return
  if (persist(rows.value.filter(item => key(item) !== key(row)))) message.value = '已刪除疊圖規則。'
}
</script>

<template>
  <main class="merge-map">
    <header><h1>產品晶圓圖疊圖規則設置</h1><button @click="emit('close')">關閉</button></header>
    <div class="toolbar">
      <label>客戶、產品類別、產品或站點 <input v-model="filter" /></label>
      <label>狀態 <select v-model="stateFilter"><option value="all">全部</option><option v-for="(label, value) in states" :key="value" :value="value">{{ label }}</option></select></label>
    </div>
    <div class="toolbar">
      <button :disabled="!developmentPreview" @click="open('add')">新增</button><button :disabled="!editable" @click="open('edit')">修改</button>
      <button :disabled="!active" @click="open('copy')">複製</button><button :disabled="!active" @click="open('batch')">批次複製</button>
      <button :disabled="!active" @click="open('detail')">規則明細</button><button :disabled="!editable" @click="approve">核准</button>
      <button :disabled="!active || ![-1, 0, 2].includes(active.state)" @click="remove">刪除</button>
    </div>
    <p v-if="message" role="status">{{ message }}</p><p v-if="error && !mode" role="alert">{{ error }}</p>
    <div class="grid"><table>
      <thead><tr><th>客戶</th><th>類別</th><th>產品</th><th>版次</th><th>站點</th><th>疊圖</th><th>時機</th><th>狀態</th><th>說明</th></tr></thead>
      <tbody>
        <tr v-for="row in visible" :key="key(row)" :class="{ selected: active === row }"><td><button :aria-pressed="active === row" @click="selected = key(row)">{{ row.customer }}</button></td><td>{{ row.productType }}</td><td>{{ row.product }}</td><td>{{ row.version }}</td><td>{{ row.op }}</td><td>{{ row.type === 1 ? '測前' : '測後' }}</td><td>{{ moments.find(moment => moment.id === row.moment)?.label }}</td><td>{{ states[row.state] }}</td><td>{{ row.description }}</td></tr>
        <tr v-if="!visible.length"><td colspan="9">無疊圖規則</td></tr>
      </tbody>
    </table></div>
    <div v-if="mode" class="overlay">
      <section class="editor" role="dialog" aria-modal="true" aria-label="疊圖規則">
        <header><h2>{{ mode === 'detail' ? '規則明細' : mode === 'batch' ? '批次複製' : '疊圖主檔' }}</h2><button @click="mode = ''; error = ''">關閉</button></header>
        <form v-if="mode === 'batch'" @submit.prevent="batchCopy">
          <fieldset><legend>目標產品與版次</legend>
            <label v-for="choice in batchChoices" :key="productKey(choice)"><input v-model="targets" type="checkbox" :value="productKey(choice)" />{{ choice.product }} / {{ choice.version }}</label>
          </fieldset><footer><button :disabled="!targets.length">複製</button></footer>
        </form>
        <form v-else-if="mode === 'detail'" @submit.prevent="editable && saveDetail()">
          <fieldset :disabled="!editable">
            <legend>識別碼與計數</legend>
            <div class="fields"><label>LotNo 規則 <input v-model="detail.lotNo" maxlength="100" /></label><label>SlotNo 規則 <input v-model="detail.slotNo" maxlength="100" /></label>
              <label>WaferID 規則 <input v-model="detail.waferId" maxlength="100" /></label><label>校驗碼 <select v-model="detail.checksum"><option value="">不附加</option><option value="1">DEV 校驗規則 1</option></select></label>
            </div>
            <div class="toolbar"><label><input v-model="detail.totalDice" type="checkbox" />重算總數</label><label><input v-model="detail.goodDice" type="checkbox" />重算良品</label><label><input v-model="detail.failDice" type="checkbox" />重算不良品</label></div>
          </fieldset>
          <fieldset :disabled="!editable"><legend>來源與參考條件</legend>
            <button type="button" @click="detail.conditions.push({ source: '', reference: '', output: '', pass: true, message: '' })">加入條件</button>
            <div v-for="(item, index) in detail.conditions" :key="index" class="fields">
              <label>來源值 <input v-model="item.source" maxlength="10" /></label><label>參考值 <input v-model="item.reference" maxlength="10" /></label>
              <label><input v-model="item.pass" type="checkbox" />有效</label>
              <label v-if="item.pass">輸出值 <input v-model="item.output" maxlength="10" /></label><label v-else>失敗訊息 <input v-model="item.message" maxlength="255" /></label>
              <button type="button" @click="detail.conditions.splice(index, 1)">移除</button>
            </div>
          </fieldset><footer v-if="editable"><button>儲存明細</button></footer>
        </form>
        <form v-else @submit.prevent="saveMaster">
          <div class="fields">
            <label>客戶 <input v-model="draft.customer" maxlength="50" required :readonly="mode === 'edit'" /></label>
            <label>產品類別 <input v-model="draft.productType" maxlength="50" required :readonly="mode === 'edit'" /></label>
            <label>產品 <input v-model="draft.product" maxlength="50" required :readonly="mode === 'edit'" /></label>
            <label>版次 <input v-model="draft.version" maxlength="5" :readonly="mode === 'edit' || draft.product === '*'" /></label>
            <label>作業站 <input v-model="draft.op" maxlength="20" :readonly="mode === 'edit'" /></label>
            <label>疊圖類型 <select v-model.number="draft.type" :disabled="mode !== 'add'" @change="changeType"><option :value="1">測前</option><option :value="2">測後</option></select></label>
            <label>疊圖時機 <select v-model.number="draft.moment"><option v-for="item in moments.filter(moment => draft.type === 1 ? moment.id === 1 : moment.id !== 1)" :key="item.id" :value="item.id">{{ item.label }}</option></select></label>
            <label>來源圖譜 <select v-model="draft.source" :disabled="mode !== 'add'"><option v-for="item in availableTemplates" :key="item.name">{{ item.name }}</option></select></label>
            <label>參考圖譜 <select v-model="draft.reference" :disabled="mode !== 'add'"><option v-for="item in availableTemplates" :key="item.name">{{ item.name }}</option></select></label>
            <template v-if="draft.type === 2"><label>來源站 <input v-model="draft.sourceOp" maxlength="20" required /></label><label>參考站 <input v-model="draft.referenceOp" maxlength="20" required /></label></template>
            <label>說明 <input v-model="draft.description" maxlength="255" /></label>
          </div><footer><button>儲存主檔</button></footer>
        </form>
        <p v-if="error" role="alert">{{ error }}</p>
      </section>
    </div>
  </main>
</template>

<style scoped>
.merge-map { display: grid; align-content: start; gap: 8px; min-width: 0; padding: 10px; background: #fff; color: #292820; font: 12px "Segoe UI", "Microsoft JhengHei", sans-serif; }
header, .toolbar, footer { display: flex; align-items: center; flex-wrap: wrap; gap: 8px; }
header { justify-content: space-between; }
h1, h2, p { margin: 0; }
h1 { font-size: 17px; }
.toolbar, thead { background: #f5f3eb; }
.toolbar { padding: 6px; border: 1px solid #dedbd0; }
button, input, select { font: inherit; }
button { padding: 4px 10px; border: 1px solid #aaa797; border-radius: 3px; background: #f8f7f2; color: inherit; }
button:disabled { opacity: .5; }
.grid { overflow: auto; border: 1px solid #d9d6cb; }
table { width: 100%; border-collapse: collapse; text-align: left; white-space: nowrap; }
th, td { padding: 6px; border: 1px solid #e3e0d6; }
.selected { background: #fff2c6; }
.overlay { position: fixed; inset: 0; z-index: 91; display: grid; place-items: center; padding: 12px; background: #0005; }
.editor { display: grid; gap: 12px; width: min(660px, 95vw); max-height: 90vh; overflow: auto; padding: 16px; background: #fff; box-shadow: 0 8px 30px #0004; }
.editor form { display: grid; gap: 9px; }
.fields { display: grid; align-items: end; grid-template-columns: repeat(auto-fit, minmax(120px, 1fr)); gap: 8px; margin: 5px 0; }
.fields label { display: grid; gap: 3px; }
.fields input { min-width: 0; }
footer { justify-content: flex-end; }
[role=alert] { color: #a32020; }
</style>
