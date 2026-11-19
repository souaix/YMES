<script setup lang="ts">
import { computed, ref } from 'vue'

type Rule = {
  no: string; type: number; option: number; sampleQty: number | ''; maxBinQty: number | ''; bin: string; ordered: string[]
  sampleFunction: string; description: string; state: number; stamp: number; creator: string; reviser?: string
}
type Mode = 'add' | 'edit' | 'copy' | 'view' | ''

const props = defineProps<{ developmentPreview: boolean; currentUserNo: string }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.prd204-eqc-sampling.v1'
const binCatalog = ['BIN1', 'BIN2', 'BIN3', 'BIN4', 'BIN5', 'BIN6', 'BIN7', 'BIN8']
const options = [
  { id: 10, label: '各 Good Bin' }, { id: 11, label: '指定單一 Bin' }, { id: 12, label: '指定多個 Bin' },
  { id: 13, label: '最大 Bin' }, { id: 14, label: '最多數個 Bin' }, { id: 15, label: '手動抽樣' },
  { id: 20, label: '指定單一 Bin' }, { id: 21, label: '最大 Bin' }, { id: 22, label: '依 Bin 順序補足數量' },
  { id: 23, label: '依順序找足量的 Bin' }, { id: 24, label: '手動抽樣' },
]
const states = { '-1': '停用', 0: '未核准', 1: '簽核中', 2: '已核准' } as Record<string, string>
const rows = ref(load())
const selected = ref('')
const keyword = ref('')
const state = ref('all')
const mode = ref<Mode>('')
const draft = ref(blank())
const binToAdd = ref('')
const error = ref('')
const notice = ref('')
const visible = computed(() => rows.value.filter(row =>
  row.no.includes(keyword.value.trim().toUpperCase()) && (state.value === 'all' || row.state.toString() === state.value),
))
const active = computed(() => visible.value.find(row => row.no === selected.value) ?? visible.value[0])
const editable = computed(() => mode.value !== 'view')
const optionsByType = computed(() => options.filter(option => Math.floor(option.id / 10) === draft.value.type + 1))
const availableBins = computed(() => binCatalog.filter(bin => !draft.value.ordered.includes(bin)))
const single = computed(() => [11, 20].includes(draft.value.option))
const multi = computed(() => [12, 22, 23].includes(draft.value.option))
const custom = computed(() => [16, 25].includes(draft.value.option))
const user = computed(() => props.currentUserNo || 'DEV001')

function blank(): Rule {
  return { no: '', type: 0, option: 10, sampleQty: '', maxBinQty: '', bin: '', ordered: [], sampleFunction: '', description: '', state: 0, stamp: 0, creator: '' }
}

function load(): Rule[] {
  if (!props.developmentPreview) return []
  try {
    const saved = JSON.parse(localStorage.getItem(storageKey) ?? 'null')
    if (Array.isArray(saved)) return saved
  } catch { /* Fall back to DEV fixtures. */ }
  return [
    { ...blank(), no: 'DEV-EQC-AQL', description: 'DEV 各 Good Bin 抽樣', creator: 'DEV001', state: 2 },
    { ...blank(), no: 'DEV-EQC-FIXED', type: 1, option: 22, sampleQty: 25, ordered: ['BIN1', 'BIN2'], creator: 'DEV001' },
  ]
}

function optionLabel(id: number): string {
  return options.find(option => option.id === id)?.label ?? ([16, 25].includes(id) ? '既有抽樣函式' : String(id))
}

function persist(next: Rule[]): boolean {
  if (!props.developmentPreview) return false
  try {
    localStorage.setItem(storageKey, JSON.stringify(next))
  } catch {
    error.value = '無法儲存本機 DEV 抽樣規則。'
    return false
  }
  error.value = ''
  rows.value = next
  return true
}

function open(next: Exclude<Mode, ''>): void {
  const row = active.value
  if (next !== 'add' && !row) return
  if (next === 'edit' && row?.state !== 0) return
  draft.value = next === 'add' || !row ? blank() : { ...row, ordered: [...row.ordered] }
  if (next === 'copy') draft.value.no = ''
  binToAdd.value = ''
  error.value = ''
  mode.value = next
}

function move(index: number, offset: number): void {
  const target = index + offset
  const bins = draft.value.ordered
  if (target < 0 || target >= bins.length) return
  const item = bins[index]
  bins[index] = bins[target]
  bins[target] = item
}

function addBin(): void {
  if (!availableBins.value.includes(binToAdd.value)) return
  draft.value.ordered.push(binToAdd.value)
  binToAdd.value = ''
}

function within(value: number | '', max: number): boolean {
  return typeof value === 'number' && Number.isInteger(value) && value >= 1 && value <= max
}

function save(): void {
  const rule = { ...draft.value, no: draft.value.no.trim().toUpperCase(), ordered: multi.value ? [...draft.value.ordered] : [] }
  const existing = rows.value.find(row => row.no === rule.no)
  if (!rule.no) { error.value = '規則編號必填。'; return }
  if (rule.type === 1 && !within(rule.sampleQty, 99999)) { error.value = '抽樣量須為 1 到 99999 的整數。'; return }
  if (rule.option === 14 && !within(rule.maxBinQty, 99)) { error.value = '最大 Bin 數須為 1 到 99 的整數。'; return }
  if (single.value && !binCatalog.includes(rule.bin)) { error.value = '請選擇一個 Bin。'; return }
  if (multi.value && !rule.ordered.length) { error.value = '請至少加入一個 Bin。'; return }
  if (custom.value && !rule.sampleFunction) { error.value = '既有抽樣函式須有名稱。'; return }
  if (mode.value === 'edit' && existing?.state !== 0) return
  if (mode.value !== 'edit' && existing) { error.value = '規則編號已存在。'; return }
  if (rule.type === 0) rule.sampleQty = ''
  if (rule.option !== 14) rule.maxBinQty = ''
  if (!single.value) rule.bin = ''
  if (!custom.value) rule.sampleFunction = ''
  const saved = existing
    ? { ...rule, reviser: user.value, stamp: existing.stamp + 1 }
    : { ...rule, creator: user.value, reviser: undefined, state: 0, stamp: 0 }
  if (persist(existing ? rows.value.map(row => row.no === saved.no ? saved : row) : [...rows.value, saved])) {
    selected.value = saved.no
    state.value = 'all'
    keyword.value = mode.value = ''
    notice.value = '已儲存抽樣規則。'
  }
}

function approve(): void {
  const row = active.value
  if (!row || row.state !== 0 || !window.confirm(`確定核准 ${row.no}？`)) return
  const updated = { ...row, state: 2, reviser: user.value, stamp: row.stamp + 1 }
  if (persist(rows.value.map(item => item.no === row.no ? updated : item))) notice.value = '已核准抽樣規則。'
}

function remove(): void {
  const row = active.value
  if (row && [-1, 0, 2].includes(row.state) && window.confirm(`確定刪除 ${row.no} 及 Bin 明細？`)
    && persist(rows.value.filter(item => item.no !== row.no))) notice.value = '已刪除抽樣規則。'
}
</script>

<template>
  <main class="eqc">
    <header><h1>電性 (EQC) 抽樣規則</h1><button @click="emit('close')">關閉</button></header>
    <div class="toolbar">
      <label>規則編號 <input v-model="keyword" /></label>
      <label>狀態 <select v-model="state"><option value="all">全部</option><option v-for="(name, code) in states" :key="code" :value="code">{{ name }}</option></select></label>
      <button :disabled="!developmentPreview" @click="open('add')">新增</button>
      <button :disabled="!active" @click="open('view')">檢視</button>
      <button :disabled="active?.state !== 0" @click="open('edit')">修改</button>
      <button :disabled="!active" @click="open('copy')">複製</button>
      <button :disabled="active?.state !== 0" @click="approve">核准</button>
      <button :disabled="!active || ![-1, 0, 2].includes(active.state)" @click="remove">刪除</button>
    </div>
    <p v-if="notice" role="status">{{ notice }}</p>
    <p v-if="error && !mode" role="alert">{{ error }}</p>
    <div class="grid">
      <table>
        <thead><tr><th scope="col">規則編號</th><th scope="col">抽樣方式</th><th scope="col">規則選項</th><th scope="col">抽樣量</th><th scope="col">狀態</th><th scope="col">說明</th></tr></thead>
        <tbody>
          <tr v-for="row in visible" :key="row.no" :class="{ selected: row === active }">
            <td><button :aria-pressed="row === active" @click="selected = row.no">{{ row.no }}</button></td>
            <td>{{ row.type === 0 ? 'AQL' : '指定抽樣量' }}</td><td>{{ optionLabel(row.option) }}</td><td>{{ row.sampleQty }}</td><td>{{ states[row.state] }}</td><td>{{ row.description }}</td>
          </tr>
          <tr v-if="!visible.length"><td colspan="6">無抽樣規則</td></tr>
        </tbody>
      </table>
    </div>
    <div v-if="mode" class="overlay">
      <form class="editor" role="dialog" aria-modal="true" aria-label="抽樣規則" @submit.prevent="editable && save()">
        <header><h2>抽樣規則</h2><button type="button" @click="mode = ''; error = ''">關閉</button></header>
        <label>規則編號 <input v-model="draft.no" maxlength="55" required :readonly="!editable || mode === 'edit'" /></label>
        <label>抽樣方式 <select v-model.number="draft.type" :disabled="!editable" @change="draft.option = draft.type === 0 ? 10 : 20"><option :value="0">AQL</option><option :value="1">指定抽樣量</option></select></label>
        <label>規則選項
          <select v-model.number="draft.option" :disabled="!editable">
            <option v-if="custom" :value="draft.option">{{ optionLabel(draft.option) }}</option>
            <option v-for="option in optionsByType" :key="option.id" :value="option.id">{{ option.label }}</option>
          </select>
        </label>
        <label v-if="draft.type === 1">抽樣量 <input v-model.number="draft.sampleQty" type="number" min="1" max="99999" :readonly="!editable" required /></label>
        <label v-if="draft.option === 14">最大 Bin 數 <input v-model.number="draft.maxBinQty" type="number" min="1" max="99" :readonly="!editable" required /></label>
        <label v-if="single">指定 Bin <select v-model="draft.bin" :disabled="!editable"><option value="">請選擇</option><option v-for="bin in binCatalog" :key="bin">{{ bin }}</option></select></label>
        <fieldset v-if="multi">
          <legend>指定 Bin 順序</legend>
          <div v-if="editable" class="toolbar"><label>候選 Bin <select v-model="binToAdd"><option value="">請選擇</option><option v-for="bin in availableBins" :key="bin">{{ bin }}</option></select></label><button type="button" :disabled="!binToAdd" @click="addBin">加入</button></div>
          <ol><li v-for="(bin, index) in draft.ordered" :key="bin">{{ bin }}
            <template v-if="editable"><button type="button" :disabled="index === 0" @click="move(index, -1)">上移</button><button type="button" :disabled="index === draft.ordered.length - 1" @click="move(index, 1)">下移</button><button type="button" @click="draft.ordered.splice(index, 1)">移除</button></template>
          </li></ol>
        </fieldset>
        <label v-if="custom">既有抽樣函式 <input v-model="draft.sampleFunction" readonly /></label>
        <label>說明 <input v-model="draft.description" maxlength="255" :readonly="!editable" /></label>
        <p v-if="error" role="alert">{{ error }}</p>
        <footer v-if="editable"><button>儲存</button></footer>
      </form>
    </div>
  </main>
</template>

<style scoped>
.eqc { display: grid; align-content: start; gap: 8px; min-width: 0; padding: 10px; background: #fff; color: #292820; font: 12px "Segoe UI", "Microsoft JhengHei", sans-serif; }
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
.editor { display: grid; gap: 12px; width: min(540px, 95vw); max-height: 90vh; overflow: auto; padding: 16px; background: #fff; box-shadow: 0 8px 30px #0004; }
.editor > label { display: flex; gap: 8px; align-items: center; }
li > button { margin-inline-start: 4px; }
footer { justify-content: flex-end; }
[role=alert] { color: #a32020; }
</style>
