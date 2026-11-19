<script setup lang="ts">
import { computed, ref } from 'vue'
import { createXlsxTemplate, parseFirstWorksheetXlsx } from './materialImportWorkbook'
import { rowsKey, seedRows } from './productBasisDevelopment'

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.pdp03-wafer-rules.v1'
const columns = ['ProductNo', 'DeviceNo', 'OPNo', 'LotHoldYield', 'LotHoldCode', 'WaferHoldYield', 'WaferYieldHoldCode',
  'WaferOSHoldRatio', 'WaferOSHoldCode', 'WaferOSHoldCount', 'WaferOSCountHoldCode', 'BinRule', 'BinRuleHoldCode',
  'BinRuleDescription', 'RatioLimit', 'LogicOperator', 'CountLimit', 'ControlBinNo', 'ControlBinDesc'] as const
type Values = Record<(typeof columns)[number], string>
type ImportRow = { line: number; values: Values; problems: string[] }
type Rule = { product: string; device: string; operation: string; no: string; category: string; limit: string; hold: string; note: string; file: string; line: number; values?: Values }
const sample = 'DEV-PRD-1001'
const rows = ref<Rule[]>(load())
const query = ref({ product: '', device: '', operation: '' })
const applied = ref<typeof query.value>()
const sourceRows = ref<ImportRow[]>([])
const detail = ref<Rule>()
const busy = ref(false)
const fileName = ref('')
const error = ref('')
const notice = ref('')
const products = props.developmentPreview ? read(rowsKey, seedRows).map(row => String(row.productNo)) : []
const visible = computed(() => {
  const filter = applied.value
  if (!filter) return []
  return rows.value.filter(row => row.product.includes(filter.product) && row.device.includes(filter.device) && row.operation.includes(filter.operation))
})
const canApply = computed(() => props.developmentPreview && sourceRows.value.length > 0 && !sourceRows.value.some(row => row.problems.length))
const keys = ['LotHoldYield', 'WaferHoldYield', 'WaferOSHoldRatio', 'WaferOSHoldCount', 'RatioLimit', 'CountLimit'] as const

function read<T>(key: string, fallback: T): T {
  try { return JSON.parse(localStorage.getItem(key) ?? 'null') ?? fallback }
  catch { return fallback }
}

function load(): Rule[] {
  if (!props.developmentPreview) return []
  return read(storageKey, [{ product: sample, device: 'DEV-DEVICE', operation: 'DEV-OP-10', no: 'LOT_LOW YIELD',
    category: '批號低良率', limit: '95', hold: 'DEV-HOLD', note: '', file: 'DEV', line: 3 }])
}

function runQuery(): void {
  const filters = { product: query.value.product.trim().toUpperCase(), device: query.value.device.trim().toUpperCase(), operation: query.value.operation.trim().toUpperCase() }
  if (!Object.values(filters).some(Boolean)) { error.value = '請輸入產品、Device 或作業站。'; return }
  applied.value = filters
  error.value = ''
}

function toRules(row: ImportRow, file: string): Rule[] {
  const value = row.values
  const common = { product: value.ProductNo.toUpperCase(), device: value.DeviceNo.toUpperCase(), operation: value.OPNo.toUpperCase(), file, line: row.line, values: value }
  const rules: Rule[] = []
  const thresholds = [
    { no: 'LOT_LOW YIELD', category: '批號低良率', limit: value.LotHoldYield, hold: value.LotHoldCode },
    { no: 'WF_LOW YIELD', category: '晶圓低良率', limit: value.WaferHoldYield, hold: value.WaferYieldHoldCode },
    { no: 'WB_OS RATIO', category: '晶圓 OS 比例', limit: value.WaferOSHoldRatio, hold: value.WaferOSHoldCode },
    { no: 'WB_OS QTY', category: '晶圓 OS 數量', limit: value.WaferOSHoldCount, hold: value.WaferOSCountHoldCode },
  ]
  for (const threshold of thresholds) {
    if (threshold.limit) rules.push({ ...common, ...threshold, note: '' })
  }
  if (value.BinRule) rules.push({ ...common, no: value.BinRule, category: 'Bin 法則', limit: [value.RatioLimit, value.LogicOperator, value.CountLimit].filter(Boolean).join(' '),
    hold: value.BinRuleHoldCode, note: value.BinRuleDescription || value.ControlBinDesc })
  return rules
}

async function chooseFile(event: Event): Promise<void> {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file || !props.developmentPreview) return
  sourceRows.value = []
  fileName.value = file.name
  error.value = notice.value = ''
  busy.value = true
  try {
    if (!/\.xlsx$/i.test(file.name)) throw new Error('DEV 支援 XLSX 工作簿。')
    const sheet = await parseFirstWorksheetXlsx(file, true)
    const seen = new Set<string>()
    sourceRows.value = sheet.slice(2).flatMap((cells, index): ImportRow[] => {
      if (!cells.slice(0, columns.length).some(Boolean)) return []
      const values = Object.fromEntries(columns.map((column, i) => [column, cells[i] ?? ''])) as Values
      const problems: string[] = []
      const entry = { line: index + 3, values, problems }
      if (!products.includes(values.ProductNo.toUpperCase()) || !values.OPNo) problems.push('請填 DEV 產品與作業站')
      if (values.DeviceNo.length > 50 || values.OPNo.length > 20) problems.push('Device 或站點超過長度')
      for (const key of keys) {
        if (values[key] && !/^\d+(?:\.\d+)?$/.test(values[key])) problems.push(`${key} 須為數字`)
      }
      if (values.LogicOperator && !['AND', 'OR'].includes(values.LogicOperator)) problems.push('LogicOperator 須為 AND 或 OR')
      const rules = toRules(entry, file.name)
      if (!rules.length) problems.push('請填至少一組法則')
      for (const rule of rules) {
        const ruleKey = JSON.stringify([rule.product, rule.operation, rule.no])
        if (seen.has(ruleKey)) problems.push(`同站法則重複 ${rule.no}`)
        seen.add(ruleKey)
      }
      return [entry]
    })
    if (!sourceRows.value.length) error.value = '工作簿第 3 列起沒有可匯入資料。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '無法讀取工作簿。' }
  finally { busy.value = false }
}

function apply(): void {
  if (!canApply.value || !window.confirm('確定以匯入資料替換這批產品的 DEV 法則？')) return
  const imported = sourceRows.value.flatMap(row => toRules(row, fileName.value))
  const products = new Set(imported.map(row => row.product))
  const next = [...rows.value.filter(row => !products.has(row.product)), ...imported]
  try { localStorage.setItem(storageKey, JSON.stringify(next)) }
  catch { error.value = '無法儲存本機 DEV 晶圓法則。'; return }
  rows.value = next
  const first = imported[0]
  if (first) applied.value = { product: first.product, device: '', operation: '' }
  sourceRows.value = []
  notice.value = `已套用 ${products.size} 個產品的 ${imported.length} 筆法則。`
}

function downloadTemplate(): void {
  const link = document.createElement('a')
  const sampleProduct = products.includes(sample) ? sample : products[0] || ''
  const url = URL.createObjectURL(createXlsxTemplate(columns, 'WaferRules', [['DEV 範例'], [sampleProduct, 'DEV-DEVICE', 'DEV-OP-10', '95', 'DEV-HOLD']]))
  link.href = url
  link.download = 'WaferRules-DEV.xlsx'
  link.click()
  window.setTimeout(() => URL.revokeObjectURL(url), 1000)
}
</script>

<template>
  <main class="wafer-rules">
    <header><h1>產品晶圓法則管制管理</h1><button @click="emit('close')">關閉</button></header>
    <template v-if="developmentPreview">
      <form class="toolbar" @submit.prevent="runQuery">
        <label>產品 <input v-model="query.product" /></label><label>Device <input v-model="query.device" /></label><label>作業站 <input v-model="query.operation" /></label>
        <button>查詢</button>
      </form>
      <div class="toolbar">
        <button @click="downloadTemplate">下載 DEV 範本</button>
        <label>晶圓法則 XLSX <input type="file" accept=".xlsx" :disabled="busy" @change="chooseFile" /></label>
        <button :disabled="busy || !canApply" @click="apply">套用 DEV 匯入</button>
      </div>
      <p v-if="busy" role="status">正在讀取 {{ fileName }}</p>
      <p v-if="error" role="alert">{{ error }}</p><p v-if="notice" role="status">{{ notice }}</p>
      <div v-if="sourceRows.length" class="grid">
        <table><caption>{{ fileName }} · 預覽</caption>
          <thead><tr><th>列</th><th>驗證</th><th v-for="column in columns" :key="column">{{ column }}</th></tr></thead>
          <tbody><tr v-for="row in sourceRows" :key="row.line"><td>{{ row.line }}</td><td>{{ row.problems.join('、') || '可套用' }}</td><td v-for="column in columns" :key="column">{{ row.values[column] }}</td></tr></tbody>
        </table>
      </div>
      <div v-if="applied" class="grid">
        <table><caption>查詢結果</caption>
          <thead><tr><th>產品</th><th>Device</th><th>作業站</th><th>類別</th><th>法則</th><th>限制</th><th>Hold 代碼</th><th>明細</th></tr></thead>
          <tbody>
            <tr v-for="row in visible" :key="JSON.stringify([row.product, row.operation, row.no])"><td>{{ row.product }}</td><td>{{ row.device }}</td><td>{{ row.operation }}</td><td>{{ row.category }}</td><td>{{ row.no }}</td><td>{{ row.limit }}</td><td>{{ row.hold }}</td><td><button @click="detail = row">檢視</button></td></tr>
            <tr v-if="!visible.length"><td colspan="8">無晶圓法則</td></tr>
          </tbody>
        </table>
      </div>
      <div v-if="detail" class="overlay">
        <section class="editor" role="dialog" aria-modal="true" aria-label="晶圓法則明細">
          <header><h2>{{ detail.no }}</h2><button @click="detail = undefined">關閉</button></header>
          <dl><dt>來源</dt><dd>{{ detail.file }} · 第 {{ detail.line }} 列</dd><dt>說明</dt><dd>{{ detail.note || '—' }}</dd>
            <template v-for="column in columns" :key="column"><template v-if="detail.values?.[column]"><dt>{{ column }}</dt><dd>{{ detail.values[column] }}</dd></template></template>
          </dl>
        </section>
      </div>
    </template>
    <p v-else>晶圓法則管理待接續。</p>
  </main>
</template>

<style scoped>
.wafer-rules { display: grid; align-content: start; gap: 8px; min-width: 0; padding: 10px; background: #fff; color: #292820; font: 12px "Segoe UI", "Microsoft JhengHei", sans-serif; }
header, .toolbar { display: flex; align-items: center; flex-wrap: wrap; gap: 8px; }
header { justify-content: space-between; }
h1, h2, p { margin: 0; }
h1 { font-size: 17px; }
.toolbar, thead { background: #f5f3eb; }
.toolbar { padding: 6px; border: 1px solid #dedbd0; }
button, input { font: inherit; }
button { padding: 4px 10px; border: 1px solid #aaa797; border-radius: 3px; background: #f8f7f2; color: inherit; }
button:disabled { opacity: .5; }
.grid { overflow: auto; border: 1px solid #d9d6cb; }
table { width: 100%; border-collapse: collapse; white-space: nowrap; text-align: left; }
th, td { padding: 6px; border: 1px solid #e3e0d6; }
caption { text-align: left; padding: 6px; }
.overlay { position: fixed; inset: 0; display: grid; place-items: center; z-index: 91; padding: 12px; background: #0005; }
.editor { display: grid; gap: 12px; width: min(580px, 95vw); max-height: 90vh; overflow: auto; padding: 16px; background: #fff; box-shadow: 0 8px 30px #0004; }
dl { display: grid; grid-template-columns: max-content 1fr; gap: 6px 12px; }
dd { margin: 0; overflow-wrap: anywhere; }
[role=alert] { color: #a32020; }
</style>
