<script setup lang="ts">
import { computed, ref } from 'vue'

type Scope = 'basic' | 'process' | 'station'
type Change = {
  product: string; version: string; scope: Scope; no: string; type: string; state: number; start: string; end: string
  target: string; value: string; lots: string; clean: string; abort: string; creator: string; stamp: number
}
const props = defineProps<{ product: string; version: string; user: string }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.pdb02-product-changes.v1'
const scopes: Array<{ id: Scope; label: string; target: string; value: string }> = [
  { id: 'basic', label: '基礎設定', target: '項目', value: '設定值' },
  { id: 'process', label: '流程', target: '流程編號', value: '流程版次' },
  { id: 'station', label: '作業站參數', target: '作業站', value: '參數內容' },
]
const states = { '-1': '作廢', 0: '待確認', 1: '簽核中', 2: '使用中' } as Record<string, string>
const scope = ref<Scope>('basic')
const filter = ref('all')
const rows = ref(load())
const selected = ref('')
const mode = ref<'' | 'add' | 'edit' | 'copy' | 'sen' | 'clean' | 'abort'>('')
const draft = ref<Change>(blank())
const message = ref('')
const error = ref('')
const scopeInfo = computed(() => scopes.find(item => item.id === scope.value))
const visible = computed(() => rows.value.filter(row => row.product === props.product && row.version === props.version && row.scope === scope.value
  && (filter.value === 'all' || row.state.toString() === filter.value)))
const active = computed(() => visible.value.find(row => row.no === selected.value) ?? visible.value[0])

function blank(): Change {
  return { product: props.product, version: props.version, scope: scope.value, no: '', type: 'ECN', state: 0, start: '', end: '',
    target: '', value: '', lots: '', clean: '', abort: '', creator: props.user || 'DEV001', stamp: 0 }
}

function load(): Change[] {
  try {
    const saved = JSON.parse(localStorage.getItem(storageKey) ?? 'null')
    if (Array.isArray(saved)) return saved
  } catch { /* Fall back to an empty DEV change list. */ }
  return []
}

function key(row: Change): string { return JSON.stringify([row.product, row.version, row.scope, row.no]) }

function persist(saved: Change): boolean {
  const next = [...rows.value.filter(row => key(row) !== key(saved)), saved]
  try { localStorage.setItem(storageKey, JSON.stringify(next)) }
  catch { error.value = '無法儲存本機 DEV 工程變更。'; return false }
  rows.value = next
  error.value = ''
  selected.value = saved.no
  filter.value = 'all'
  mode.value = ''
  return true
}

function open(next: Exclude<typeof mode.value, ''>): void {
  const row = active.value
  if (next !== 'add' && !row) return
  if (next === 'edit' && row?.state !== 0) return
  draft.value = next === 'add' || !row ? blank() : { ...row }
  if (next === 'copy') Object.assign(draft.value, { no: '', state: 0, stamp: 0, abort: '', clean: '', creator: props.user || 'DEV001' })
  error.value = ''
  mode.value = next
}

function save(): void {
  const row = { ...draft.value, no: draft.value.no.trim().toUpperCase(), target: draft.value.target.trim(), value: draft.value.value.trim() }
  if (!row.no || !row.target) { error.value = '請填變更單號與目標。'; return }
  if (row.type === 'TECN' && (!row.start || !row.end || row.start > row.end)) { error.value = 'TECN 須填正確的起訖時間。'; return }
  if (row.type !== 'TECN') row.start = row.end = ''
  if (row.type !== 'SEN') row.lots = ''
  const previous = rows.value.find(item => key(item) === key(row))
  if (mode.value !== 'edit' && previous) { error.value = '此產品版次已有同類別變更單號。'; return }
  if (mode.value === 'edit' && previous?.state !== 0) return
  row.stamp = previous ? previous.stamp + 1 : 0
  if (persist(row)) message.value = '已儲存 DEV 變更單。'
}

function approve(): void {
  const row = active.value
  if (!row || row.state !== 0) return
  if (!row.value || (row.type === 'SEN' && !row.lots.trim())) {
    error.value = '核准前須填變更內容，SEN 另須指定批號。'
    return
  }
  if (!window.confirm(`確定核准 ${row.no}？`)) return
  const saved = { ...row, state: 2, stamp: row.stamp + 1, clean: row.type === 'TECN' ? '' : '待清線' }
  if (persist(saved)) message.value = '已核准 DEV 變更單。'
}

function abort(): void {
  const row = active.value
  const reason = draft.value.abort.trim()
  if (!row || row.state === -1 || key(row) !== key(draft.value) || !reason) { error.value = '請填作廢原因。'; return }
  if (persist({ ...row, state: -1, abort: reason, stamp: row.stamp + 1 })) message.value = '已作廢 DEV 變更單。'
}
</script>

<template>
  <section class="changes" role="dialog" aria-modal="true" aria-label="封測工程變更">
    <header><h2>封測工程變更 {{ product }} / {{ version }}</h2><button @click="emit('close')">關閉</button></header>
    <div class="toolbar">
      <label>類別 <select v-model="scope" @change="mode = ''; selected = ''; error = ''"><option v-for="item in scopes" :key="item.id" :value="item.id">{{ item.label }}</option></select></label>
      <label>狀態 <select v-model="filter" @change="mode = ''; selected = ''; error = ''"><option value="all">全部</option><option v-for="(label, key) in states" :key="key" :value="key">{{ label }}</option></select></label>
      <button @click="open('add')">新增</button><button :disabled="active?.state !== 0" @click="open('edit')">修改</button><button :disabled="!active" @click="open('copy')">複製</button>
      <button :disabled="active?.state !== 0" @click="approve">核准</button><button :disabled="!active || active.state === -1" @click="open('abort')">作廢</button>
      <button :disabled="active?.type !== 'SEN'" @click="open('sen')">SEN 批號</button><button :disabled="!active" @click="open('clean')">清線查詢</button>
    </div>
    <p v-if="message" role="status">{{ message }}</p><p v-if="error" role="alert">{{ error }}</p>
    <div class="grid"><table>
      <thead><tr><th>單號</th><th>類型</th><th>狀態</th><th>目標</th><th>變更內容</th></tr></thead>
      <tbody>
        <tr v-for="row in visible" :key="key(row)"><td><button :aria-pressed="active === row" @click="selected = row.no; mode = ''; error = ''">{{ row.no }}</button></td><td>{{ row.type }}</td><td>{{ states[row.state] }}</td><td>{{ row.target }}</td><td>{{ row.value }}</td></tr>
        <tr v-if="!visible.length"><td colspan="5">無工程變更</td></tr>
      </tbody>
    </table></div>
    <form v-if="['add', 'edit', 'copy'].includes(mode)" class="form" @submit.prevent="save">
      <label>變更單號 <input v-model="draft.no" maxlength="20" required :readonly="mode === 'edit'" /></label>
      <label>變更類型 <select v-model="draft.type" :disabled="mode === 'edit'"><option>ECN</option><option>TECN</option><option>SEN</option></select></label>
      <label>{{ scopeInfo?.target }} <input v-model="draft.target" maxlength="50" required /></label>
      <label>{{ scopeInfo?.value }} <input v-model="draft.value" maxlength="255" /></label>
      <template v-if="draft.type === 'TECN'"><label>開始 <input v-model="draft.start" type="datetime-local" required /></label><label>結束 <input v-model="draft.end" type="datetime-local" required /></label></template>
      <label v-if="draft.type === 'SEN'">限定批號 <textarea v-model.trim="draft.lots" rows="2" /></label>
      <footer><button>儲存變更單</button><button type="button" @click="mode = ''; error = ''">取消</button></footer>
    </form>
    <form v-else-if="mode === 'abort'" class="form" @submit.prevent="abort">
      <label>作廢原因 <input v-model="draft.abort" required maxlength="255" /></label>
      <footer><button>確認作廢</button><button type="button" @click="mode = ''; error = ''">取消</button></footer>
    </form>
    <section v-else-if="mode === 'sen' || mode === 'clean'" class="form">
      <h3>{{ mode === 'sen' ? 'SEN 批號' : 'DEV 清線狀態' }}</h3>
      <p>{{ mode === 'sen' ? (draft.lots || '無限定批號') : (draft.clean || '無清線資料') }}</p>
      <button @click="mode = ''; error = ''">收合</button>
    </section>
  </section>
</template>

<style scoped>
.changes { display: grid; gap: 10px; width: min(850px, 95vw); max-height: 90vh; overflow: auto; padding: 16px; background: #fff; color: #292820; font: 12px "Segoe UI", "Microsoft JhengHei", sans-serif; }
header, .toolbar, footer { display: flex; align-items: center; flex-wrap: wrap; gap: 8px; }
header { justify-content: space-between; }
h2, h3, p { margin: 0; }
.toolbar, thead { background: #f5f3eb; }
.toolbar, .form { padding: 6px; border: 1px solid #dedbd0; }
.form { display: flex; align-items: center; flex-wrap: wrap; gap: 8px; }
button, input, select, textarea { font: inherit; }
button { padding: 4px 10px; border: 1px solid #aaa797; border-radius: 3px; background: #f8f7f2; color: inherit; }
button:disabled { opacity: .5; }
.grid { overflow: auto; border: 1px solid #d9d6cb; }
table { width: 100%; border-collapse: collapse; text-align: left; white-space: nowrap; }
th, td { padding: 6px; border: 1px solid #e3e0d6; }
[role=alert] { color: #a32020; }
</style>
