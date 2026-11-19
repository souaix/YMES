<script setup lang="ts">
import { computed, ref } from 'vue'
import { parseFirstWorksheetXlsx } from './materialImportWorkbook'
import { relationKey, rowsKey, seedRelations, seedRows } from './productBasisDevelopment'

type Rule = {
  product: string; version: string; station: string; stationName: string; serial: string
  ruleNo: string; ruleClass: number; phase: number; type: number; script: string
}
type History = { operation: string; old?: Rule; rule: Rule; user: string; date: string }
type Snapshot = { rules: Rule[]; history: History[] }
type Station = Pick<Rule, 'product' | 'version' | 'station' | 'stationName' | 'serial'>
type Imported = { line: number; data?: Rule; problem?: string; element?: string; elementPhase?: string }

const props = defineProps<{ developmentPreview: boolean; currentUserNo: string }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.prd-station-rules.v1'
const catalog = ['DEV-RULE-CHECK', 'DEV-RULE-HOLD', 'DEV-RULE-QTY']
const classes = ['一般', 'CP', 'FT']
const stages = ['DEV 0', 'DEV 1', 'DEV 2', 'DEV 3']
const relations = props.developmentPreview ? read(relationKey, seedRelations) : {}
const stations: Station[] = props.developmentPreview ? read(rowsKey, seedRows).flatMap(product => {
  const relation = relations[`${product.productNo}|${product.productVersion}`]
  return (relation?.parameters?.ops ?? []).map(op => ({ product: product.productNo, version: product.productVersion,
    station: op.opNo, stationName: op.opName, serial: op.serialNo })).filter(row => row.serial)
}) : []
const sampleStation = stations[0]
const stored = ref<Snapshot>(props.developmentPreview ? read(storageKey, {
  rules: sampleStation ? [{ ...sampleStation, ruleNo: catalog[0], ruleClass: 0, phase: 0, type: 0, script: '' }] : [],
  history: [],
}) : { rules: [], history: [] })
const query = ref({ product: '', station: '', rule: '' })
const filters = ref({ ...query.value })
const selected = ref<string[]>([])
const mode = ref<'add' | 'replace' | ''>('')
const draft = ref({ serial: '', ruleNo: '', ruleClass: 0, phase: 0, type: 0, script: '' })
const imported = ref<Imported[]>([])
const fileName = ref('')
const busy = ref(false)
const notice = ref('')
const error = ref('')
const key = (row: Rule) => JSON.stringify([row.serial, row.ruleNo])
const visible = computed(() => stored.value.rules.filter(row => row.product.includes(filters.value.product)
  && row.station.includes(filters.value.station) && row.ruleNo.includes(filters.value.rule)))
const picked = computed(() => visible.value.filter(row => selected.value.includes(key(row))))
const canReplace = computed(() => picked.value.length > 0 && picked.value.every(row => row.ruleClass === 0 && row.ruleNo === picked.value[0]?.ruleNo))
const canImport = computed(() => imported.value.length > 0 && imported.value.every(row => row.data))

function read<T>(name: string, fallback: T): T {
  try { return JSON.parse(localStorage.getItem(name) ?? 'null') ?? fallback }
  catch { return fallback }
}

function search(): void {
  filters.value = { product: query.value.product.trim().toUpperCase(), station: query.value.station.trim().toUpperCase(), rule: query.value.rule.trim().toUpperCase() }
  selected.value = []
}

function persist(rules: Rule[], entries: Array<Omit<History, 'user' | 'date'>>): boolean {
  if (!props.developmentPreview) return false
  const user = props.currentUserNo || 'DEV001'
  const date = new Date().toISOString()
  const next = { rules, history: [...stored.value.history, ...entries.map(entry => ({ ...entry, user, date }))] }
  try { localStorage.setItem(storageKey, JSON.stringify(next)) }
  catch { error.value = '無法儲存本機 DEV 作業站法則。'; return false }
  stored.value = next
  selected.value = []
  error.value = ''
  return true
}

function open(next: 'add' | 'replace'): void {
  if (next === 'replace' && !canReplace.value) return
  const first = next === 'replace' ? picked.value[0] : undefined
  draft.value = { serial: first?.serial ?? '', ruleNo: first?.ruleNo ?? '', ruleClass: 0, phase: first?.phase ?? 0, type: first?.type ?? 0, script: first?.script ?? '' }
  error.value = ''
  mode.value = next
}

function save(): void {
  const input = draft.value
  if (!catalog.includes(input.ruleNo)) { error.value = '請選法則。'; return }
  const original = mode.value === 'replace' ? picked.value : []
  const excluded = new Set(original.map(key))
  const remaining = stored.value.rules.filter(row => !excluded.has(key(row)))
  const station = stations.find(row => row.serial === input.serial)
  if (mode.value === 'add' && !station) { error.value = '請選目標作業站。'; return }
  const changes = mode.value === 'replace'
    ? original.map(row => ({ ...row, ruleNo: input.ruleNo, script: input.script }))
    : station ? [{ ...station, ruleNo: input.ruleNo, ruleClass: input.ruleClass, phase: input.phase, type: input.type, script: input.script }] : []
  const used = new Set(remaining.map(key))
  if (changes.some(row => used.has(key(row)))) { error.value = '目標站點已有此法則。'; return }
  if (!changes.length) return
  if (persist([...remaining, ...changes], changes.map((rule, index) => ({ operation: mode.value, old: original[index], rule })))) {
    notice.value = `已儲存 ${changes.length} 筆法則。`
    mode.value = ''
  }
}

function remove(): void {
  const old = picked.value
  if (!old.length || !window.confirm(`確定刪除 ${old.length} 筆法則？`)) return
  const keys = new Set(old.map(key))
  if (persist(stored.value.rules.filter(row => !keys.has(key(row))), old.map(rule => ({ operation: 'delete', rule })))) notice.value = `已刪除 ${old.length} 筆法則。`
}

async function importPreview(event: Event): Promise<void> {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file || !props.developmentPreview) return
  busy.value = true
  imported.value = []
  notice.value = error.value = ''
  fileName.value = file.name
  try {
    const [header, ...data] = await parseFirstWorksheetXlsx(file, true)
    if (!header?.length) throw new Error('第一張工作表缺少標題。')
    const used = new Set(stored.value.rules.map(key))
    imported.value = data.flatMap((cells, index): Imported[] => {
      if (!cells?.some(Boolean)) return []
      const [product, version, op, name, ruleNo, phase, element, elementPhase] = cells
      const line = index + 2
      const station = stations.find(row => row.product === product && row.version === version && row.station === op)
      if (!station || !name || !catalog.includes(ruleNo ?? '') || phase === undefined || !/^[0-3]$/.test(phase)) {
        return [{ line, problem: '請核對 DEV 產品、版次、站點、站名、法則與觸發階段。' }]
      }
      const row = { ...station, ruleNo, phase: Number(phase), ruleClass: 0, type: 0, script: '' }
      if (used.has(key(row))) return [{ line, problem: '目標站點已包含此法則。' }]
      used.add(key(row))
      return [{ line, data: row, element, elementPhase }]
    })
    if (!imported.value.length) error.value = '工作簿沒有法則資料。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '無法讀取工作簿。' }
  finally { busy.value = false }
}

function applyImport(): void {
  if (!canImport.value || !window.confirm('將這批一般法則套用到本機 DEV？')) return
  const batch = imported.value.flatMap(row => row.data ? [row.data] : [])
  const used = new Set(stored.value.rules.map(key))
  if (batch.some(rule => used.has(key(rule)))) { error.value = '有同鍵法則已儲存，請重新預檢。'; return }
  if (persist([...stored.value.rules, ...batch], batch.map(rule => ({ operation: 'import', rule })))) {
    notice.value = `已新增 ${batch.length} 筆 DEV 法則。`
    imported.value = []
  }
}
</script>

<template>
  <main class="station-rules">
    <header><h1>產品作業站法則維護</h1><button @click="emit('close')">關閉</button></header>
    <template v-if="developmentPreview">
      <form class="toolbar" @submit.prevent="search">
        <label>產品 <input v-model="query.product" /></label>
        <label>站點 <input v-model="query.station" /></label>
        <label>法則 <input v-model="query.rule" /></label>
        <button>查詢</button>
      </form>
      <div class="toolbar">
        <button @click="open('add')">新增法則</button>
        <button :disabled="!canReplace" @click="open('replace')">替換選取的一般法則</button>
        <button :disabled="!picked.length" @click="remove">刪除選取</button>
        <button :disabled="!visible.length" @click="selected = visible.map(key)">全選</button>
        <button :disabled="!picked.length" @click="selected = []">取消全選</button>
        <label>XLSX 法則匯入 <input type="file" accept=".xlsx" :disabled="busy" @change="importPreview" /></label>
        <button :disabled="busy || !canImport" @click="applyImport">套用 DEV 匯入</button>
      </div>
      <p v-if="busy" role="status">正在讀取 {{ fileName }}</p>
      <p v-if="notice" role="status">{{ notice }}</p>
      <p v-if="error && !mode" role="alert">{{ error }}</p>
      <div v-if="imported.length" class="grid">
        <table><caption>{{ fileName }} · 法則預覽</caption>
          <thead><tr><th>列</th><th>產品</th><th>版次</th><th>站點</th><th>法則</th><th>參數／階段</th><th>結果</th></tr></thead>
          <tbody><tr v-for="row in imported" :key="row.line"><td>{{ row.line }}</td><td>{{ row.data?.product }}</td><td>{{ row.data?.version }}</td><td>{{ row.data?.station }}</td><td>{{ row.data?.ruleNo }}</td><td>{{ [row.element, row.elementPhase].filter(Boolean).join(' / ') }}</td><td>{{ row.problem || '可套用' }}</td></tr></tbody>
        </table>
      </div>
      <div class="grid"><table>
        <thead><tr><th>選取</th><th>產品</th><th>版次</th><th>作業站</th><th>法則</th><th>類別</th><th>階段</th><th>類型</th></tr></thead>
        <tbody>
          <tr v-for="row in visible" :key="key(row)"><td><input v-model="selected" type="checkbox" :value="key(row)" :aria-label="`選取 ${row.station} ${row.ruleNo}`" /></td><td>{{ row.product }}</td><td>{{ row.version }}</td><td>{{ row.station }} {{ row.stationName }}</td><td>{{ row.ruleNo }}</td><td>{{ classes[row.ruleClass] }}</td><td>{{ row.phase }}</td><td>{{ row.type === 0 ? 'Lot' : 'Component' }}</td></tr>
          <tr v-if="!visible.length"><td colspan="8">無作業站法則</td></tr>
        </tbody>
      </table></div>
      <div v-if="mode" class="overlay">
        <form class="editor" role="dialog" aria-modal="true" aria-label="作業站法則" @submit.prevent="save">
          <header><h2>{{ mode === 'add' ? '新增法則' : `替換 ${picked.length} 筆一般法則` }}</h2><button type="button" @click="mode = ''; error = ''">關閉</button></header>
          <template v-if="mode === 'add'">
            <label>產品版次／站點 <select v-model="draft.serial" required><option value="">請選擇</option><option v-for="row in stations" :key="row.serial" :value="row.serial">{{ row.product }} / {{ row.version }} / {{ row.station }}</option></select></label>
            <label>類別 <select v-model.number="draft.ruleClass"><option v-for="(name, index) in classes" :key="name" :value="index">{{ name }}</option></select></label>
            <label>階段 <select v-model.number="draft.phase"><option v-for="(stage, index) in stages" :key="stage" :value="index">{{ stage }}</option></select></label>
            <label>類型 <select v-model.number="draft.type"><option :value="0">Lot</option><option :value="1">Component</option></select></label>
          </template>
          <label>法則 <select v-model="draft.ruleNo" required><option value="">請選擇</option><option v-for="rule in catalog" :key="rule">{{ rule }}</option></select></label>
          <label>法則腳本 <textarea v-model="draft.script" maxlength="3000" rows="5" /></label>
          <p v-if="error" role="alert">{{ error }}</p><footer><button>確定</button></footer>
        </form>
      </div>
    </template>
    <p v-else>作業站法則服務待接續。</p>
  </main>
</template>

<style scoped>
.station-rules { display: grid; align-content: start; gap: 8px; min-width: 0; padding: 10px; background: #fff; color: #292820; font: 12px "Segoe UI", "Microsoft JhengHei", sans-serif; }
header, .toolbar, footer { display: flex; align-items: center; flex-wrap: wrap; gap: 8px; }
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
caption { text-align: left; padding: 6px; }
th, td { padding: 6px; border: 1px solid #e3e0d6; }
.overlay { position: fixed; inset: 0; z-index: 91; display: grid; place-items: center; padding: 12px; background: #0005; }
.editor { display: grid; gap: 12px; width: min(580px, 95vw); padding: 16px; background: #fff; box-shadow: 0 8px 30px #0004; }
.editor label { display: grid; gap: 3px; }
footer { justify-content: flex-end; }
[role=alert] { color: #a32020; }
</style>
