<script setup lang="ts">
import { computed, ref } from 'vue'
import { rowsKey, seedRows } from './productBasisDevelopment'

type Rule = {
  product: string; equipment: string; operation: string; module: 'WIP' | 'SPC'; method: number
  count: number | ''; shiftA: string; shiftB: string; item: string; itemType: number | null; description: string
  stamp: number; creator: string; reviser?: string
}
type Mode = 'add' | 'edit' | 'copy' | ''
const props = defineProps<{ developmentPreview: boolean; currentUserNo: string }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.prd206-qc-frequency.v1'
const rows = ref<Rule[]>(props.developmentPreview ? read(storageKey, [{ ...blank(), product: 'DEV-PRD-1002', equipment: '*', operation: 'DEV-OP-10', count: 5, description: 'DEV 每五批抽一批', creator: 'DEV001' }]) : [])
const products = [...new Set(read(rowsKey, seedRows).filter(row => Number(row.issueState) === 2).map(row => String(row.productNo)))]
const equipment = ['DEV-EQP-01', 'DEV-EQP-02']
const operations = ['DEV-OP-10', 'DEV-OP-20', 'DEV-OP-30']
const methods = ['多批抽一批', '每天第一批', '每班第一批']
const selected = ref('')
const filter = ref('')
const mode = ref<Mode>('')
const draft = ref<Rule>(blank())
const error = ref('')
const notice = ref('')
const key = (row: Rule) => JSON.stringify([row.product, row.equipment, row.operation])
const filtered = computed(() => rows.value.filter(row =>
  [row.product, row.equipment, row.operation, row.description].some(value => value.toUpperCase().includes(filter.value.trim().toUpperCase())),
))
const active = computed(() => filtered.value.find(row => key(row) === selected.value) ?? filtered.value[0])

function read<T>(name: string, fallback: T): T {
  if (!props.developmentPreview) return fallback
  try {
    const stored = JSON.parse(localStorage.getItem(name) ?? 'null')
    if (Array.isArray(stored)) return stored as T
  } catch { /* Use the same fixture data if DEV storage is unavailable. */ }
  return fallback
}

function blank(): Rule {
  return { product: '*', equipment: '*', operation: '*', module: 'WIP', method: 0, count: '', shiftA: '', shiftB: '',
    item: '', itemType: null, description: '', stamp: 0, creator: '' }
}

function begin(next: Exclude<Mode, ''>): void {
  const row = active.value
  if (next !== 'add' && !row) return
  draft.value = next === 'add' || !row ? blank() : { ...row }
  if (next === 'copy' && row?.module === 'SPC') {
    draft.value.module = 'WIP'
    draft.value.item = ''
    draft.value.itemType = null
  }
  error.value = ''
  mode.value = next
}

function persist(next: Rule[]): boolean {
  if (!props.developmentPreview) return false
  try {
    localStorage.setItem(storageKey, JSON.stringify(next))
  } catch {
    error.value = '無法儲存本機 DEV 抽樣頻率。'
    return false
  }
  rows.value = next
  error.value = ''
  return true
}

function validShift(value: string): boolean {
  return /^(?:[01]\d|2[0-4]):[0-5]\d$/.test(value)
}

function save(close: boolean): void {
  const saved = { ...draft.value }
  const existing = rows.value.find(row => key(row) === key(saved))
  if (mode.value !== 'edit' && existing) { error.value = '這組產品、設備與作業站已有規則。'; return }
  if (mode.value === 'edit' && !existing) return
  if (saved.method === 0 && (typeof saved.count !== 'number' || !Number.isInteger(saved.count) || saved.count < 1 || saved.count > 99)) {
    error.value = '抽樣批次數須為 1 到 99 的整數。'
    return
  }
  if (saved.method !== 0 && !validShift(saved.shiftA)) { error.value = '請輸入 A 班時分，格式 HH:mm。'; return }
  if (saved.method === 2 && !validShift(saved.shiftB)) { error.value = '請輸入 B 班時分，格式 HH:mm。'; return }
  if (saved.method !== 0) saved.count = ''
  if (saved.method === 0) saved.shiftA = ''
  if (saved.method !== 2) saved.shiftB = ''
  const user = props.currentUserNo || 'DEV001'
  if (existing) {
    saved.stamp = existing.stamp + 1
    saved.reviser = user
  } else {
    saved.stamp = 0
    saved.creator = user
    saved.reviser = undefined
  }
  const next = existing ? rows.value.map(row => key(row) === key(saved) ? saved : row) : [...rows.value, saved]
  if (!persist(next)) return
  selected.value = key(saved)
  filter.value = ''
  draft.value = { ...saved }
  notice.value = '已儲存抽樣頻率規則。'
  mode.value = close ? '' : 'edit'
}

function remove(): void {
  const row = active.value
  if (row && window.confirm('確定刪除此產品、設備與作業站的抽樣頻率？') && persist(rows.value.filter(item => key(item) !== key(row)))) {
    notice.value = '已刪除抽樣頻率規則。'
  }
}
</script>

<template>
  <main class="frequency">
    <header><h1>產品抽樣頻率規則</h1><button @click="emit('close')">關閉</button></header>
    <div class="toolbar">
      <label>產品、設備、作業站或說明 <input v-model="filter" /></label>
      <button :disabled="!developmentPreview" @click="begin('add')">新增</button>
      <button :disabled="!active" @click="begin('edit')">修改</button>
      <button :disabled="!active" @click="begin('copy')">複製</button>
      <button :disabled="!active" @click="remove">刪除</button>
    </div>
    <p v-if="notice" role="status">{{ notice }}</p>
    <p v-if="error && !mode" role="alert">{{ error }}</p>
    <div class="grid">
      <table>
        <thead><tr><th>產品</th><th>設備</th><th>作業站</th><th>模組</th><th>頻率</th><th>抽樣批次數</th><th>A 班</th><th>B 班</th><th>說明</th></tr></thead>
        <tbody>
          <tr v-for="row in filtered" :key="key(row)" :class="{ selected: row === active }">
            <td><button :aria-pressed="row === active" @click="selected = key(row)">{{ row.product }}</button></td>
            <td>{{ row.equipment }}</td><td>{{ row.operation }}</td><td>{{ row.module }}</td><td>{{ methods[row.method] }}</td><td>{{ row.count }}</td><td>{{ row.shiftA }}</td><td>{{ row.shiftB }}</td><td>{{ row.description }}</td>
          </tr>
          <tr v-if="!filtered.length"><td colspan="9">無抽樣頻率規則</td></tr>
        </tbody>
      </table>
    </div>
    <div v-if="mode" class="overlay">
      <form class="editor" role="dialog" aria-modal="true" aria-label="抽樣頻率規則" @submit.prevent="save(true)">
        <header><h2>抽樣頻率規則</h2><button type="button" @click="mode = ''; error = ''">關閉</button></header>
        <label>模組 <input :value="draft.module" readonly /></label>
        <label>產品
          <input v-if="mode === 'edit'" :value="draft.product" readonly />
          <select v-else v-model="draft.product"><option value="*">*（全部）</option><option v-for="value in products" :key="value">{{ value }}</option></select>
        </label>
        <label>設備
          <input v-if="mode === 'edit'" :value="draft.equipment" readonly />
          <select v-else v-model="draft.equipment"><option value="*">*（全部）</option><option v-for="value in equipment" :key="value">{{ value }}</option></select>
        </label>
        <label>作業站
          <input v-if="mode === 'edit'" :value="draft.operation" readonly />
          <select v-else v-model="draft.operation"><option value="*">*（全部）</option><option v-for="value in operations" :key="value">{{ value }}</option></select>
        </label>
        <label v-if="draft.module === 'SPC'">品管項目 <input :value="draft.item" readonly /></label>
        <label>頻率 <select v-model.number="draft.method"><option v-for="(method, index) in methods" :key="method" :value="index">{{ method }}</option></select></label>
        <label v-if="draft.method === 0">抽樣批次數 <input v-model.number="draft.count" type="number" min="1" max="99" step="1" required /></label>
        <label v-else>A 班開始 <input v-model.trim="draft.shiftA" placeholder="HH:mm" maxlength="5" required /></label>
        <label v-if="draft.method === 2">B 班開始 <input v-model.trim="draft.shiftB" placeholder="HH:mm" maxlength="5" required /></label>
        <label>說明 <input v-model="draft.description" maxlength="255" /></label>
        <p v-if="error" role="alert">{{ error }}</p>
        <footer><button>確定</button><button type="button" @click="save(false)">套用</button></footer>
      </form>
    </div>
  </main>
</template>

<style scoped>
.frequency { display: grid; align-content: start; gap: 8px; min-width: 0; padding: 10px; background: #fff; color: #292820; font: 12px "Segoe UI", "Microsoft JhengHei", sans-serif; }
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
footer { justify-content: flex-end; }
[role=alert] { color: #a32020; }
</style>
