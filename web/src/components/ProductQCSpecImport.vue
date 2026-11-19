<script setup lang="ts">
import { computed, ref } from 'vue'
import { createXlsxTemplate, parseFirstWorksheetXlsx } from './materialImportWorkbook'
import { rowsKey, seedRows } from './productBasisDevelopment'

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const columns = [
  'PRODUCTNO', 'PRODUCTVERSION', 'GROUPNO', 'QCITEMNO', 'QCITEMTYPE', 'OPNO', 'INLINE',
  'EXECUTEORDER', 'FIRSTINSP', 'INSPECT', 'PQC', 'Unit', 'PlanNo', 'SampleSize', 'target', 'cpk goal',
  ...['T', 'X', 'R', 'S', 'M'].flatMap(chart => ['US', 'CS', 'LS', 'UCL', 'CL', 'LCL', 'rule'].map(field => chart + field)),
]
const ruleColumns = columns.filter(column => column.endsWith('rule'))
const groupColumns = ['QCITEMTYPE', 'OPNO', 'INLINE', 'EXECUTEORDER', 'FIRSTINSP', 'INSPECT', 'PQC']
const devKey = 'ymes.dev.prd116-qc-spec-import.v1'
const storedRows = ref(readImportRows())
const products = props.developmentPreview ? readProducts() : []
const templateProduct = ref('')
const fileName = ref('')
const mode = ref<'new' | 'override'>('new')
const imported = ref<Array<{ line: number; fields: Record<string, string> }>>([])
const readError = ref('')
const notice = ref('')
const busy = ref(false)
const fieldKey = (fields: Record<string, string>) => JSON.stringify(['PRODUCTNO', 'PRODUCTVERSION', 'OPNO', 'GROUPNO', 'QCITEMNO'].map(column => fields[column]?.toUpperCase() ?? ''))
const existingKeys = computed(() => new Set(storedRows.value.map(fieldKey)))

function readProducts(): typeof seedRows {
  try {
    const saved = JSON.parse(localStorage.getItem(rowsKey) ?? 'null')
    if (Array.isArray(saved)) return saved
  } catch { /* Use seeded products when DEV storage is unavailable. */ }
  return seedRows
}

function readImportRows(): Array<Record<string, string>> {
  if (!props.developmentPreview) return []
  try {
    const saved = JSON.parse(localStorage.getItem(devKey) ?? 'null')
    if (Array.isArray(saved)) return saved
  } catch { /* An empty DEV import history is safe to use. */ }
  return []
}

const preview = computed(() => {
  const keys = new Set<string>()
  const groups = new Map<string, string>()
  return imported.value.map(({ line, fields }) => {
    const issues: string[] = []
    const currentKey = fieldKey(fields)
    if (!products.some(product => product.productNo === fields.PRODUCTNO && product.productVersion === fields.PRODUCTVERSION)) issues.push('DEV 查無產品／版次')
    if (!fields.QCITEMNO || !fields.OPNO) issues.push('品管項目及作業站必填')
    if (!['0', '1', '2', '3'].includes(fields.QCITEMTYPE)) issues.push('QCITEMTYPE 須為 0–3')
    if (!['0', '1'].includes(fields.INLINE) || (fields.EXECUTEORDER && !['0', '1'].includes(fields.EXECUTEORDER))) issues.push('INLINE 與非空白的 EXECUTEORDER 須為 0 或 1')
    if (['FIRSTINSP', 'INSPECT', 'PQC'].some(column => fields[column] && !['0', '1'].includes(fields[column]))) issues.push('檢驗類型須為空白、0 或 1')
    if (fields.INLINE === '1' && ['FIRSTINSP', 'INSPECT', 'PQC'].every(column => fields[column] === '0')) issues.push('INLINE 為 1 時至少須有一種檢驗')
    if (['2', '3'].includes(fields.QCITEMTYPE) && !fields.GROUPNO) issues.push('群組項目需填 GROUPNO')
    if (fields.GROUPNO) {
      const signature = JSON.stringify(groupColumns.map(column => fields[column]))
      const first = groups.get(fields.GROUPNO)
      if (first && first !== signature) issues.push('同群組的作業站、類型或檢驗設定不一致')
      if (!first) groups.set(fields.GROUPNO, signature)
    }
    for (const column of ruleColumns) {
      if (fields[column].split(';').some(rule => rule.trim() && !/^\s*[^,\s]+\s*,\s*(WARNING|WAIT|HOLD|ALERT)\s*$/i.test(rule))) issues.push(`${column} 請填 RuleNo,Action，多筆以分號分隔`)
    }
    if (keys.has(currentKey)) issues.push('檔內有重複的產品、作業站、群組和品管項目')
    keys.add(currentKey)
    const existing = existingKeys.value.has(currentKey)
    return { line, fields, issues, action: existing ? mode.value === 'override' ? '覆蓋' : '保留' : '新增' }
  })
})
const canApply = computed(() => props.developmentPreview && preview.value.length > 0 && !preview.value.some(row => row.issues.length))

async function chooseFile(event: Event): Promise<void> {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file || !props.developmentPreview) return
  imported.value = []
  readError.value = notice.value = ''
  fileName.value = file.name
  busy.value = true
  try {
    if (!file.name.toLowerCase().endsWith('.xlsx')) throw new Error('DEV 支援 .xlsx 格式。')
    const rows = await parseFirstWorksheetXlsx(file, true)
    imported.value = rows.slice(1).map((row, index) => ({
      line: index + 2,
      fields: Object.fromEntries(columns.map((column, fieldIndex) => [column, row[fieldIndex]?.trim() ?? ''])),
    })).filter(row => row.fields.PRODUCTNO)
    if (!imported.value.length) readError.value = '第一張工作表沒有可匯入的資料。'
  } catch (error) {
    readError.value = error instanceof Error ? error.message : '無法讀取工作簿。'
  } finally {
    busy.value = false
  }
}

function downloadTemplate(): void {
  const product = products.find(row => JSON.stringify([row.productNo, row.productVersion]) === templateProduct.value)
  const sample = product ? [product.productNo, product.productVersion, '', 'DEV-QC-01', '1', 'DEV-OP-10', '1', '1', '0', '1', '0'] : []
  const url = URL.createObjectURL(createXlsxTemplate(columns, 'ProductQCSpec', sample.length ? [sample] : []))
  const link = document.createElement('a')
  link.href = url
  link.download = 'PRDQCItemSPECImport-DEV.xlsx'
  link.click()
  window.setTimeout(() => URL.revokeObjectURL(url), 1000)
}

function apply(): void {
  if (!canApply.value || !window.confirm('確定將這批規格套用到本機 DEV？')) return
  const rows = new Map(storedRows.value.map(row => [fieldKey(row), row]))
  let written = 0
  for (const row of preview.value) {
    if (row.action === '保留') continue
    rows.set(fieldKey(row.fields), { ...row.fields, EXECUTEORDER: row.fields.EXECUTEORDER || '1' })
    written++
  }
  try {
    const next = [...rows.values()]
    localStorage.setItem(devKey, JSON.stringify(next))
    storedRows.value = next
    notice.value = `已套用 ${written} 筆。DEV 已儲存 ${next.length} 筆。`
    readError.value = ''
  } catch {
    readError.value = '無法儲存本機 DEV 匯入結果。'
  }
}
</script>

<template>
  <main class="qc-spec-import">
    <header><h1>產品品管項目規格匯入</h1><button @click="emit('close')">關閉</button></header>
    <template v-if="developmentPreview">
      <div class="toolbar">
        <label>範本產品
          <select v-model="templateProduct">
            <option value="">空白範本</option>
            <option v-for="product in products" :key="JSON.stringify([product.productNo, product.productVersion])" :value="JSON.stringify([product.productNo, product.productVersion])">{{ product.productNo }} / {{ product.productVersion }}</option>
          </select>
        </label>
        <button @click="downloadTemplate">下載 DEV 範本</button>
      </div>
      <div class="toolbar">
        <label>匯入方式 <select v-model="mode" :disabled="busy"><option value="new">New Plan</option><option value="override">Override</option></select></label>
        <label>Excel 檔案 <input type="file" accept=".xlsx" :disabled="busy" @change="chooseFile" /></label>
        <button :disabled="busy || !canApply" @click="apply">執行 DEV 匯入</button>
      </div>
      <p v-if="busy" role="status">正在讀取 {{ fileName }}</p>
      <p v-if="readError" role="alert">{{ readError }}</p>
      <p v-if="notice" role="status">{{ notice }}</p>
      <template v-if="preview.length">
        <p>{{ fileName }} · {{ preview.length }} 筆 · {{ preview.filter(row => row.issues.length).length }} 筆有錯誤</p>
        <div class="results">
          <table>
            <thead><tr><th scope="col">列</th><th scope="col">結果</th><th scope="col">錯誤</th><th v-for="column in columns" :key="column" scope="col">{{ column }}</th></tr></thead>
            <tbody>
              <tr v-for="row in preview" :key="row.line">
                <td>{{ row.line }}</td><td>{{ row.issues.length ? '待修正' : row.action }}</td><td>{{ row.issues.join('、') }}</td>
                <td v-for="column in columns" :key="column">{{ row.fields[column] }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </template>
    </template>
    <p v-else>匯入服務待接續。</p>
  </main>
</template>

<style scoped>
.qc-spec-import { display: grid; align-content: start; gap: 8px; min-width: 0; padding: 10px; background: #fff; color: #292820; font: 12px "Segoe UI", "Microsoft JhengHei", sans-serif; }
header, .toolbar { display: flex; align-items: center; flex-wrap: wrap; gap: 8px; }
header { justify-content: space-between; }
h1, p { margin: 0; }
h1 { font-size: 17px; }
.toolbar { padding: 6px; border: 1px solid #dedbd0; background: #f5f3eb; }
button, input, select { font: inherit; }
button { padding: 4px 10px; border: 1px solid #aaa797; border-radius: 3px; background: #f8f7f2; color: inherit; }
button:disabled { opacity: .5; }
.results { overflow: auto; max-width: 100%; max-height: 60vh; border: 1px solid #d9d6cb; }
table { border-collapse: collapse; text-align: left; white-space: nowrap; }
th, td { padding: 6px; border: 1px solid #e3e0d6; }
thead { position: sticky; top: 0; background: #f5f3eb; }
[role=alert] { color: #a32020; }
</style>
