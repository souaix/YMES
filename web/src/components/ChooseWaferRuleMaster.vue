<script setup lang="ts">
import { computed, ref } from 'vue'
import { rowsKey, seedRows } from './productBasisDevelopment'

type Rule = {
  no: string; name: string; segment: string; productType: string; product: string; inventories: string[]; vendors: string[]
  script: string; description: string; state: number; stamp: number; creator: string; reviser?: string
}
type Mode = 'add' | 'edit' | 'copy' | 'view' | ''
const props = defineProps<{ developmentPreview: boolean; currentUserNo: string }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.pdb01-choose-wafer-rules.v1'
const segmentOptions = ['DEV-PS-10', 'DEV-PS-20']
const inventoryOptions = [{ no: 'DEV-RAW-01', label: '原物料倉', type: 0 }, { no: 'DEV-RAW-02', label: '原物料倉', type: 0 }, { no: 'DEV-SEMI-01', label: '半成品倉', type: 2 }]
const vendorOptions = ['DEV-VENDOR-01', 'DEV-VENDOR-02']
const fields = [
  { no: 'PRODUCTNO', type: 'text', label: '產品編號' }, { no: 'LOTNO', type: 'text', label: '批號' },
  { no: 'DEV_WAFER.YIELD', type: 'number', label: '晶圓良率' }, { no: 'DEV_WAFER.GOODQTY', type: 'number', label: '良品數' },
  { no: 'DEV_SOURCE.RECEIVEDATE', type: 'date', label: '入庫日期' },
] as const
const productCatalog = props.developmentPreview ? read(rowsKey, seedRows) : []
const productTypes = [...new Set(productCatalog.map(item => String(item.productType || '')).filter(Boolean))]
const productChoices = computed(() => [...new Set(productCatalog.filter(item => !draft.value.productType || item.productType === draft.value.productType).map(item => String(item.productNo)))])
const states = { '-1': '停用', 0: '未核准', 1: '簽核中', 2: '已核准' } as Record<string, string>
const rows = ref<Rule[]>(props.developmentPreview ? read(storageKey, [{
  ...blank(), no: 'DEV-WAFER-01', name: '原物料挑片', segment: segmentOptions[0], inventories: [inventoryOptions[0].no], creator: 'DEV001',
}]) : [])
const keyword = ref('')
const state = ref('all')
const selected = ref('')
const mode = ref<Mode>('')
const draft = ref<Rule>(blank())
const condition = ref({ field: '', operator: '=', value: '', join: 'AND' })
const notice = ref('')
const error = ref('')
const visible = computed(() => rows.value.filter(row =>
  [row.no, row.name, row.segment, row.productType, row.product].some(value => value.toUpperCase().includes(keyword.value.trim().toUpperCase()))
  && (state.value === 'all' || row.state.toString() === state.value),
))
const active = computed(() => visible.value.find(row => row.no === selected.value) ?? visible.value[0])
const editable = computed(() => mode.value !== 'view')
const user = computed(() => props.currentUserNo || 'DEV001')

function blank(): Rule {
  return { no: '', name: '', segment: '', productType: '', product: '', inventories: [], vendors: [],
    script: '', description: '', state: 0, stamp: 0, creator: '' }
}

function read<T>(key: string, fallback: T): T {
  try { return JSON.parse(localStorage.getItem(key) ?? 'null') ?? fallback }
  catch { return fallback }
}

function persist(next: Rule[]): boolean {
  if (!props.developmentPreview) return false
  try { localStorage.setItem(storageKey, JSON.stringify(next)) }
  catch { error.value = '無法儲存本機 DEV 挑片規則。'; return false }
  rows.value = next
  error.value = ''
  return true
}

function open(next: Exclude<Mode, ''>): void {
  const row = active.value
  if (next === 'edit' && row?.state !== 0) return
  if (next !== 'add' && !row) return
  draft.value = next === 'add' || !row ? blank() : { ...row, inventories: [...row.inventories], vendors: [...row.vendors] }
  if (next === 'copy') draft.value.no = ''
  condition.value = { field: '', operator: '=', value: '', join: 'AND' }
  error.value = ''
  mode.value = next
}

function appendCondition(): void {
  if (!editable.value) return
  const { field, operator, join, value } = condition.value
  const choice = fields.find(item => item.no === field)
  const text = value.trim()
  const comparison = ['=', '<>', '>', '>=', '<', '<='].includes(operator)
  if (!choice || !text || (!comparison && !(choice.type === 'text' && operator === 'LIKE'))) { error.value = '請設定欄位、適用的比較方式與值。'; return }
  if (choice.type === 'number' && !/^-?\d+(?:\.\d+)?$/.test(text)) { error.value = '此欄位須填數字。'; return }
  if (choice.type === 'date' && (!/^\d{4}-\d{2}-\d{2}$/.test(text) || !Number.isFinite(Date.parse(text)))) { error.value = '日期格式須為 YYYY-MM-DD。'; return }
  const literal = choice.type === 'number' ? text : "'" + text.replaceAll("'", "''") + "'"
  const prior = draft.value.script.trim()
  draft.value.script = [prior, prior && join, choice.no, operator, literal].filter(Boolean).join(' ')
  error.value = ''
}

function save(close: boolean): void {
  const next = { ...draft.value, no: draft.value.no.trim().toUpperCase(), name: draft.value.name.trim(),
    inventories: [...draft.value.inventories], vendors: [...draft.value.vendors] }
  if (!next.no || !next.name || !segmentOptions.includes(next.segment)) { error.value = '生產區段、規則編號及名稱必填。'; return }
  const types = new Set(next.inventories.map(no => inventoryOptions.find(item => item.no === no)?.type))
  if (types.size !== 1 || types.has(undefined)) { error.value = '請選同一類型的原物料倉或半成品倉。'; return }
  const existing = rows.value.find(row => row.no === next.no)
  if (mode.value !== 'edit' && existing) { error.value = '規則編號已存在。'; return }
  if (mode.value === 'edit' && existing?.state !== 0) return
  const saved = existing ? { ...next, reviser: user.value, stamp: existing.stamp + 1 }
    : { ...next, creator: user.value, reviser: undefined, state: 0, stamp: 0 }
  if (persist(existing ? rows.value.map(row => row.no === saved.no ? saved : row) : [...rows.value, saved])) {
    draft.value = { ...saved, inventories: [...saved.inventories], vendors: [...saved.vendors] }
    keyword.value = ''
    state.value = 'all'
    selected.value = saved.no
    mode.value = close ? '' : 'edit'
    notice.value = '已儲存挑片規則。'
  }
}

function approve(): void {
  const row = active.value
  if (!row || row.state !== 0 || !window.confirm(`確定核准 ${row.no}？`)) return
  if (persist(rows.value.map(item => item.no === row.no ? { ...item, state: 2, stamp: item.stamp + 1, reviser: user.value } : item))) notice.value = '已核准挑片規則。'
}

function remove(): void {
  const row = active.value
  if (!row || ![-1, 0, 2].includes(row.state) || !window.confirm(`確定刪除 ${row.no}？`)) return
  if (persist(rows.value.filter(item => item.no !== row.no))) notice.value = '已刪除挑片規則。'
}
</script>

<template>
  <main class="choose-wafer">
    <header><h1>挑片規則設定</h1><button @click="emit('close')">關閉</button></header>
    <div class="toolbar">
      <label>規則、區段或產品 <input v-model="keyword" /></label>
      <label>狀態 <select v-model="state"><option value="all">全部</option><option v-for="(label, value) in states" :key="value" :value="value">{{ label }}</option></select></label>
      <button :disabled="!developmentPreview" @click="open('add')">新增</button><button :disabled="!active" @click="open('view')">檢視</button>
      <button :disabled="active?.state !== 0" @click="open('edit')">修改</button><button :disabled="!active" @click="open('copy')">複製</button>
      <button :disabled="active?.state !== 0" @click="approve">核准</button><button :disabled="!active || ![-1, 0, 2].includes(active.state)" @click="remove">刪除</button>
    </div>
    <p v-if="notice" role="status">{{ notice }}</p><p v-if="error && !mode" role="alert">{{ error }}</p>
    <div class="grid"><table>
      <thead><tr><th>規則編號</th><th>名稱</th><th>生產區段</th><th>產品</th><th>倉庫</th><th>狀態</th></tr></thead>
      <tbody>
        <tr v-for="row in visible" :key="row.no" :class="{ selected: row === active }">
          <td><button :aria-pressed="row === active" @click="selected = row.no">{{ row.no }}</button></td><td>{{ row.name }}</td><td>{{ row.segment }}</td><td>{{ row.product }}</td><td>{{ row.inventories.join('、') }}</td><td>{{ states[row.state] }}</td>
        </tr><tr v-if="!visible.length"><td colspan="6">無挑片規則</td></tr>
      </tbody>
    </table></div>
    <div v-if="mode" class="overlay">
      <form class="editor" role="dialog" aria-modal="true" aria-label="挑片規則" @submit.prevent="editable && save(true)">
        <header><h2>挑片規則</h2><button type="button" @click="mode = ''; error = ''">關閉</button></header>
        <fieldset :disabled="!editable">
          <legend>主檔</legend>
          <div class="fields">
            <label>規則編號 <input v-model="draft.no" maxlength="50" required :readonly="mode === 'edit'" /></label>
            <label>名稱 <input v-model="draft.name" maxlength="100" required /></label>
            <label>生產區段 <select v-model="draft.segment" required :disabled="mode === 'edit'"><option value="">請選擇</option><option v-for="segment in segmentOptions" :key="segment">{{ segment }}</option></select></label>
            <label>參照產品類別 <select v-model="draft.productType" @change="draft.product = ''"><option value="">不限</option><option v-for="item in productTypes" :key="item">{{ item }}</option></select></label>
            <label>參照產品 <select v-model="draft.product"><option value="">不限</option><option v-for="item in productChoices" :key="item">{{ item }}</option></select></label>
            <label>說明 <input v-model="draft.description" maxlength="255" /></label>
          </div>
        </fieldset>
        <fieldset :disabled="!editable"><legend>倉庫</legend><div class="checks"><label v-for="item in inventoryOptions" :key="item.no"><input v-model="draft.inventories" :value="item.no" type="checkbox" />{{ item.no }} {{ item.label }}</label></div></fieldset>
        <fieldset :disabled="!editable"><legend>供應商</legend><div class="checks"><label v-for="item in vendorOptions" :key="item"><input v-model="draft.vendors" :value="item" type="checkbox" />{{ item }}</label></div></fieldset>
        <fieldset :disabled="!editable">
          <legend>挑片條件</legend>
          <div v-if="editable" class="toolbar">
            <label>欄位 <select v-model="condition.field"><option value="">請選擇</option><option v-for="item in fields" :key="item.no" :value="item.no">{{ item.label }}</option></select></label>
            <label>比較 <select v-model="condition.operator"><option v-for="item in ['=', '<>', '>', '>=', '<', '<=', 'LIKE']" :key="item">{{ item }}</option></select></label>
            <label>值 <input v-model="condition.value" /></label>
            <label>連接 <select v-model="condition.join"><option>AND</option><option>OR</option></select></label>
            <button type="button" @click="appendCondition">加入條件</button>
          </div>
          <label>條件文字 <textarea v-model="draft.script" rows="4" /></label>
        </fieldset>
        <p v-if="error" role="alert">{{ error }}</p>
        <footer v-if="editable"><button>確定</button><button type="button" @click="save(false)">套用</button></footer>
      </form>
    </div>
  </main>
</template>

<style scoped>
.choose-wafer { display: grid; align-content: start; gap: 8px; min-width: 0; padding: 10px; background: #fff; color: #292820; font: 12px "Segoe UI", "Microsoft JhengHei", sans-serif; }
header, .toolbar, footer, .checks { display: flex; align-items: center; flex-wrap: wrap; gap: 8px; }
header { justify-content: space-between; }
h1, h2, p { margin: 0; }
h1 { font-size: 17px; }
.toolbar, thead { background: #f5f3eb; }
.toolbar { padding: 6px; border: 1px solid #dedbd0; }
button, input, select, textarea { font: inherit; }
button { padding: 4px 10px; border: 1px solid #aaa797; border-radius: 3px; background: #f8f7f2; color: inherit; }
button:disabled { opacity: .5; }
.grid { overflow: auto; border: 1px solid #d9d6cb; }
table { width: 100%; border-collapse: collapse; text-align: left; white-space: nowrap; }
th, td { padding: 6px; border: 1px solid #e3e0d6; }
.selected { background: #fff2c6; }
.overlay { position: fixed; inset: 0; z-index: 91; display: grid; place-items: center; padding: 12px; background: #0005; }
.editor { display: grid; gap: 12px; width: min(690px, 95vw); max-height: 90vh; overflow: auto; padding: 16px; background: #fff; box-shadow: 0 8px 30px #0004; }
.fields { display: grid; grid-template-columns: repeat(auto-fit, minmax(130px, 1fr)); gap: 8px; }
.fields label { display: grid; gap: 3px; }
textarea { display: block; width: 100%; }
footer { justify-content: flex-end; }
[role=alert] { color: #a32020; }
</style>
