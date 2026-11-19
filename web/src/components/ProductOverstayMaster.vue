<script setup lang="ts">
import { computed, ref } from 'vue'
import { parseFirstWorksheetXlsx } from './materialImportWorkbook'
import { loadDevelopmentProductRelations, productKey, rowsKey, seedRows } from './productBasisDevelopment'

type Rule = { product: string; version: string; opName: string; op: string; alarm: number | null; groups: string; hold: number | null }
type PreviewRow = { line: number; rule: Rule; errors: string[] }
type Snapshot = { rules: Rule[]; previous: Rule[] }

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.prd-overstay.v1'
const sample: Rule = { product: 'DEV-PRD-1002', version: '02', opName: '點膠作業', op: 'DEV-OP-10', alarm: 2, groups: 'DEV-ALARM', hold: 4 }
const alarmGroups = ['DEV-ALARM', 'DEV-SHIFT']
const conditions = ref({ product: '', version: '', op: '' })
const appliedConditions = ref<typeof conditions.value>()
const state = ref<Snapshot>(props.developmentPreview ? read(storageKey, { rules: [sample], previous: [] }) : { rules: [], previous: [] })
const products = computed(() => read(rowsKey, seedRows).filter(row => Number(row.issueState) === 2))
const ops = ['DEV-OP-10', 'DEV-OP-20']
const selected = ref<string[]>([])
const preview = ref<PreviewRow[]>([])
const busy = ref(false)
const fileName = ref('')
const notice = ref('')
const error = ref('')
const key = (row: Rule) => JSON.stringify([row.product, row.version, row.op])
const filtered = computed(() => {
  const filters = appliedConditions.value
  return filters ? state.value.rules.filter(row =>
    (!filters.product || row.product === filters.product) && (!filters.version || row.version === filters.version) && (!filters.op || row.op === filters.op),
  ) : []
})
const canApply = computed(() => props.developmentPreview && preview.value.length && !preview.value.some(row => row.errors.length))

function read<T>(name: string, fallback: T): T {
  if (!props.developmentPreview) return fallback
  try { return JSON.parse(localStorage.getItem(name) ?? 'null') ?? fallback }
  catch { return fallback }
}

function search(): void {
  const filter = conditions.value
  if (!filter.product && !filter.op && !window.confirm('確定查詢全部產品與作業站？')) return
  appliedConditions.value = { product: filter.product.toUpperCase(), version: filter.version.toUpperCase(), op: filter.op.toUpperCase() }
  selected.value = []
  preview.value = []
  error.value = notice.value = ''
}

function parseHours(value: string, errors: string[], label: string): number | null {
  if (!value) return null
  if (!/^\d{1,4}(?:\.\d{1,2})?$/.test(value)) {
    errors.push(`${label}須為 0 至 9999.99`)
    return null
  }
  return Number(value)
}

async function loadPreview(event: Event): Promise<void> {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file || !props.developmentPreview) return
  busy.value = true
  error.value = notice.value = ''
  preview.value = []
  fileName.value = file.name
  try {
    if (!/\.xlsx$/i.test(file.name)) throw new Error('DEV 支援 XLSX。')
    const sheet = await parseFirstWorksheetXlsx(file, true)
    const index = new Set<string>()
    const relations = loadDevelopmentProductRelations(read(rowsKey, seedRows))
    preview.value = sheet.slice(1).flatMap((cells, position): PreviewRow[] => {
      const [product = '', version = '', opName = '', opNo = '', alarm = '', alarmGroup = '', hold = ''] = cells
      if (!product) return []
      const errors: string[] = []
      const groups = [...new Set(alarmGroup.split(',').map(group => group.trim().toUpperCase()).filter(Boolean))]
      const rule = { product: product.toUpperCase(), version: version.toUpperCase(), opName, op: opNo.toUpperCase(),
        alarm: parseHours(alarm, errors, '警告工時'), hold: parseHours(hold, errors, 'Hold 工時'), groups: groups.join(',') }
      if (!rule.version || !rule.op) errors.push('產品版次與站點必填')
      if (!rule.product.includes('ALL')) {
        const approved = products.value.some(row => row.productNo === rule.product && row.productVersion === rule.version)
        if (!approved) errors.push('找不到核准的 DEV 產品／版次')
        const relation = relations[productKey(rule.product, rule.version)]
        const processOps = relation?.parameters?.ops.map(operation => operation.opNo) ?? (rule.product === sample.product && rule.version === sample.version ? ops : [])
        if (!processOps.includes(rule.op)) errors.push('站點不在此 DEV 產品流程')
      }
      if (groups.some(group => !alarmGroups.includes(group))) errors.push('找不到 DEV 異常群組')
      if (rule.groups.length > 20 || rule.op.length > 20 || rule.version.length > 5 || rule.product.length > 50) errors.push('主鍵或群組超出允許長度')
      if (index.has(key(rule))) errors.push('同工作簿的產品、版次與站點重複')
      index.add(key(rule))
      return [{ line: position + 2, rule, errors }]
    })
    if (!preview.value.length) error.value = '第一張工作表沒有可匯入資料。'
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : '無法讀取活頁簿。'
  } finally { busy.value = false }
}

function persist(next: Snapshot): boolean {
  if (!props.developmentPreview) return false
  try { localStorage.setItem(storageKey, JSON.stringify(next)) }
  catch { error.value = '無法儲存本機 DEV Overstay。'; return false }
  state.value = next
  error.value = ''
  return true
}

function apply(): void {
  if (!canApply.value || !window.confirm('確定套用通過預檢的 Overstay 設定？')) return
  const incoming = preview.value.map(row => row.rule)
  const keys = new Set(incoming.map(key))
  const old = state.value.rules.filter(row => keys.has(key(row)))
  if (!persist({ rules: [...state.value.rules.filter(row => !keys.has(key(row))), ...incoming], previous: [...state.value.previous, ...old] })) return
  notice.value = `已套用 ${incoming.length} 筆，其中 ${old.length} 筆舊資料保留於 DEV 歷史。`
  appliedConditions.value ??= { product: '', version: '', op: '' }
  preview.value = []
  selected.value = []
}

function remove(): void {
  const keys = new Set(selected.value)
  if (!keys.size || !window.confirm(`確定刪除 ${keys.size} 筆選取資料？`)) return
  if (persist({ ...state.value, rules: state.value.rules.filter(row => !keys.has(key(row))) })) {
    notice.value = `已刪除 ${keys.size} 筆。`
    selected.value = []
  }
}
</script>

<template>
  <main class="overstay">
    <header><h1>產品作業站 Overstay 維護</h1><button @click="emit('close')">關閉</button></header>
    <template v-if="developmentPreview">
      <form class="toolbar" @submit.prevent="search">
        <label>產品 <input v-model.trim="conditions.product" list="overstay-products" /></label>
        <datalist id="overstay-products"><option v-for="product in new Set(products.map(row => String(row.productNo)))" :key="product" :value="product" /></datalist>
        <label>版次 <input v-model.trim="conditions.version" /></label>
        <label>作業站 <input v-model.trim="conditions.op" list="overstay-ops" /></label>
        <datalist id="overstay-ops"><option v-for="op in ops" :key="op" :value="op" /></datalist>
        <button :disabled="busy">查詢</button><button type="button" @click="conditions = { product: '', version: '', op: '' }">清除條件</button>
        <button type="button" :disabled="!selected.length" @click="remove">刪除選取</button>
      </form>
      <div class="toolbar">
        <label>Excel 匯入 <input type="file" accept=".xlsx" :disabled="busy" @change="loadPreview" /></label>
        <button :disabled="busy || !canApply" @click="apply">套用 DEV 匯入</button>
      </div>
      <p v-if="busy" role="status">正在讀取 {{ fileName }}</p>
      <p v-if="error" role="alert">{{ error }}</p><p v-if="notice" role="status">{{ notice }}</p>
      <div v-if="preview.length" class="grid">
        <table><caption>{{ fileName }} · 匯入預覽</caption>
          <thead><tr><th>列</th><th>產品</th><th>版次</th><th>站點</th><th>Alarm (Hrs)</th><th>異常群組</th><th>Hold (Hrs)</th><th>驗證</th></tr></thead>
          <tbody><tr v-for="row in preview" :key="row.line"><td>{{ row.line }}</td><td>{{ row.rule.product }}</td><td>{{ row.rule.version }}</td><td>{{ row.rule.op }}</td><td>{{ row.rule.alarm }}</td><td>{{ row.rule.groups }}</td><td>{{ row.rule.hold }}</td><td>{{ row.errors.join('、') || '可套用' }}</td></tr></tbody>
        </table>
      </div>
      <div v-if="appliedConditions" class="grid">
        <table><caption>查詢結果</caption>
          <thead><tr><th>選取</th><th>產品</th><th>版次</th><th>作業站</th><th>Alarm (Hrs)</th><th>異常群組</th><th>Hold (Hrs)</th></tr></thead>
          <tbody>
            <tr v-for="row in filtered" :key="key(row)"><td><input v-model="selected" type="checkbox" :value="key(row)" :aria-label="`選取 ${row.product} ${row.version} ${row.op}`" /></td><td>{{ row.product }}</td><td>{{ row.version }}</td><td>{{ row.op }} {{ row.opName }}</td><td>{{ row.alarm }}</td><td>{{ row.groups }}</td><td>{{ row.hold }}</td></tr>
            <tr v-if="!filtered.length"><td colspan="7">無 Overstay 設定</td></tr>
          </tbody>
        </table>
      </div>
    </template>
    <p v-else>Overstay 服務待接續。</p>
  </main>
</template>

<style scoped>
.overstay { display: grid; align-content: start; gap: 8px; min-width: 0; padding: 10px; background: #fff; color: #292820; font: 12px "Segoe UI", "Microsoft JhengHei", sans-serif; }
header, .toolbar { display: flex; align-items: center; flex-wrap: wrap; gap: 8px; }
header { justify-content: space-between; }
h1, p { margin: 0; }
h1 { font-size: 17px; }
.toolbar, thead { background: #f5f3eb; }
.toolbar { padding: 6px; border: 1px solid #dedbd0; }
button, input { font: inherit; }
button { padding: 4px 10px; border: 1px solid #aaa797; border-radius: 3px; background: #f8f7f2; color: inherit; }
button:disabled { opacity: .5; }
.grid { overflow: auto; border: 1px solid #d9d6cb; }
table { width: 100%; border-collapse: collapse; text-align: left; white-space: nowrap; }
caption { text-align: left; padding: 6px; }
th, td { padding: 6px; border: 1px solid #e3e0d6; }
[role=alert] { color: #a32020; }
</style>
