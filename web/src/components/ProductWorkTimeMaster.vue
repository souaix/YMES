<script setup lang="ts">
import { computed, ref } from 'vue'
import { parseFirstWorksheetXlsx } from './materialImportWorkbook'

type WorkTime = {
  tech: string; type: string; product: string; sapVersion: string; gbom: string; current: boolean
  sequence: number; op: string; opName: string; customer: string; unitTime: number | null; employeeTime: number | null
  equipmentTime: number | null; created: string; completed: string; updateDate: string; remark: string
}
type BatchLine = { line: number; data?: WorkTime; error?: string }
type Snapshot = { rows: WorkTime[]; history: WorkTime[] }

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.prd901-work-time.v1'
const technologies = ['DEV SMT', 'DEV 組裝']
const types = ['成品', '半成品']
const products = [
  { product: 'DEV-PRD-1001', sapVersion: 'DEV-PRD-1001-01', gbom: 'DEV-GBOM-1001', tech: technologies[0], type: types[0], current: true },
  { product: 'DEV-PRD-1002', sapVersion: 'DEV-PRD-1002-02', gbom: 'DEV-GBOM-1002', tech: technologies[1], type: types[1], current: true },
  { product: 'DEV-PRD-1002', sapVersion: 'DEV-PRD-1002-01', gbom: 'DEV-GBOM-1002', tech: technologies[1], type: types[1], current: false },
]
const fixture = products.map((product, index): WorkTime => ({
  ...product, sequence: 10, op: 'DEV-OP-10', opName: '組裝', customer: 'DEV',
  unitTime: index === 0 ? null : 1.25, employeeTime: null, equipmentTime: null,
  created: '2026-11-14T08:00', completed: '2026-11-18T15:00', updateDate: '', remark: '',
}))
const query = ref({ tech: '', product: '', gbom: '', operation: '', productType: '', dateField: '', start: '', end: '', noTime: false, currentOnly: false })
const snapshot = ref(load())
const result = ref<WorkTime[]>([])
const searched = ref(false)
const batch = ref<BatchLine[]>([])
const busy = ref(false)
const fileName = ref('')
const error = ref('')
const message = ref('')
const sapPreview = ref<Array<{ product: string; version: string }>>([])
const canApply = computed(() => props.developmentPreview && batch.value.length > 0 && batch.value.every(line => line.data))

const productKey = (product: string, version: string) => JSON.stringify([product.toUpperCase(), version.toUpperCase()])
const rowKey = (row: WorkTime) => JSON.stringify([productKey(row.product, row.sapVersion), row.op.toUpperCase(), row.sequence])

function load(): Snapshot {
  if (!props.developmentPreview) return { rows: [], history: [] }
  try {
    const saved = JSON.parse(localStorage.getItem(storageKey) ?? 'null')
    if (Array.isArray(saved?.rows) && Array.isArray(saved?.history)) return saved
  } catch { /* Use DEV fixtures when storage is unavailable. */ }
  return { rows: fixture, history: [] }
}

function search(): void {
  const form = query.value
  error.value = message.value = ''
  sapPreview.value = []
  if (!form.tech) { error.value = '請選科技類別。'; return }
  if (form.currentOnly && !form.product && !form.gbom) { error.value = '勾選最新版次時請輸入 TH 料號或 GBOM。'; return }
  if (form.dateField) {
    const start = Date.parse(form.start)
    const end = Date.parse(form.end)
    if (!Number.isFinite(start) || !Number.isFinite(end) || end < start || end - start > 30 * 24 * 60 * 60 * 1000) {
      error.value = '請輸入正確起訖時間，範圍上限為 30 天。'
      return
    }
  }
  result.value = snapshot.value.rows.filter(row => {
    const date = form.dateField === 'completed' ? row.completed : row.created
    return row.tech === form.tech && row.product.toUpperCase().includes(form.product.toUpperCase())
      && row.gbom.toUpperCase().includes(form.gbom.toUpperCase()) && row.op.toUpperCase().includes(form.operation.toUpperCase())
      && (!form.productType || row.type === form.productType) && (!form.noTime || row.unitTime === null)
      && (!form.currentOnly || row.current) && (!form.dateField || (date >= form.start && date <= form.end))
  })
  searched.value = true
}

function parseTime(value: string): number | null {
  if (!value) return null
  if (!/^\d{1,3}(?:\.\d{1,3})?$/.test(value)) throw new Error('工時須為 0 至 999.999。')
  return Number(value)
}

function parseLine(line: number, cells: string[]): WorkTime {
  const [, customer = '', product = '', sapVersion = '', sequence = '', op = '', opName = '', , unit = '', employee = '', equipment = '', , updateDate = '', remark = ''] = cells
  if (!product || !sapVersion || !/^\d{1,4}$/.test(sequence) || !op || !unit) throw new Error('C、D、E、F、I 欄必填，E 欄須為四位以內的整數。')
  if (op.length > 20 || customer.length > 100 || opName.length > 200 || remark.length > 2000) throw new Error('站點、客戶、站名或備註超出長度。')
  const target = fixture.find(row => productKey(row.product, row.sapVersion) === productKey(product, sapVersion))
  if (!target) throw new Error('找不到 DEV 料號和 SAP 版次。')
  return { ...target, sequence: Number(sequence), op: op.toUpperCase(), opName, customer, unitTime: parseTime(unit), employeeTime: parseTime(employee),
    equipmentTime: parseTime(equipment), updateDate: updateDate || new Date().toISOString(), remark }
}

async function readWorkbook(event: Event): Promise<void> {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file || !props.developmentPreview) return
  busy.value = true
  batch.value = []
  error.value = message.value = ''
  fileName.value = file.name
  try {
    if (!file.name.toLowerCase().endsWith('.xlsx')) throw new Error('DEV 支援 XLSX。')
    const rows = await parseFirstWorksheetXlsx(file, true)
    const seen = new Set<string>()
    batch.value = rows.slice(1).flatMap((cells, index): BatchLine[] => {
      if (!cells[0]) return []
      const line = index + 2
      try {
        const data = parseLine(line, cells)
        const key = rowKey(data)
        if (seen.has(key)) throw new Error('同一料號、版次、站點和順序重複。')
        seen.add(key)
        return [{ line, data }]
      } catch (cause) {
        return [{ line, error: cause instanceof Error ? cause.message : '欄位值錯誤。' }]
      }
    })
    if (!batch.value.length) error.value = '第一個工作表沒有可匯入的資料。'
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : '無法讀取活頁簿。'
  } finally { busy.value = false }
}

function applyImport(): void {
  if (!canApply.value || !window.confirm('確定套用這批標工到本機 DEV？')) return
  const pending = batch.value.flatMap(line => line.data ? [line.data] : [])
  const replacement = new Set(pending.map(rowKey))
  const previous = snapshot.value.rows.filter(row => replacement.has(rowKey(row)))
  const next = { rows: [...snapshot.value.rows.filter(row => !replacement.has(rowKey(row))), ...pending], history: [...snapshot.value.history, ...previous] }
  try {
    localStorage.setItem(storageKey, JSON.stringify(next))
    snapshot.value = next
    batch.value = []
    if (searched.value) search()
    message.value = `已套用 ${pending.length} 筆 DEV 標工，${previous.length} 筆舊資料存入歷史。`
  } catch { error.value = '無法儲存本機 DEV 標工。' }
}

function previewSap(): void {
  const unique = new Map(result.value.map(row => [productKey(row.product, row.sapVersion), { product: row.product, version: row.sapVersion }]))
  sapPreview.value = [...unique.values()]
}
</script>

<template>
  <main class="work-time">
    <header><h1>產品作業站標工維護</h1><button @click="emit('close')">關閉</button></header>
    <template v-if="developmentPreview">
      <form class="toolbar" @submit.prevent="search">
        <label>科技類別 <select v-model="query.tech" required><option value="">請選擇</option><option v-for="tech in technologies" :key="tech">{{ tech }}</option></select></label>
        <label>TH 料號 <input v-model.trim="query.product" /></label>
        <label>GBOM <input v-model.trim="query.gbom" /></label>
        <label>作業站 <input v-model.trim="query.operation" /></label>
        <label>產品類別 <select v-model="query.productType"><option value="">全部</option><option v-for="type in types" :key="type">{{ type }}</option></select></label>
        <label>時間區間 <select v-model="query.dateField"><option value="">不限</option><option value="completed">下線時間</option><option value="created">建立時間</option></select></label>
        <template v-if="query.dateField"><label>起 <input v-model="query.start" type="datetime-local" required /></label><label>迄 <input v-model="query.end" type="datetime-local" required /></label></template>
        <label><input v-model="query.noTime" type="checkbox" />未設定工時</label>
        <label><input v-model="query.currentOnly" type="checkbox" />最新版次</label>
        <button>查詢</button>
      </form>
      <div class="toolbar">
        <label>工時 Excel <input type="file" accept=".xlsx" :disabled="busy" @change="readWorkbook" /></label>
        <button :disabled="busy || !canApply" @click="applyImport">套用 DEV 匯入</button>
        <button :disabled="!result.length" @click="previewSap">轉 SAP 預覽</button>
      </div>
      <p v-if="busy" role="status">正在讀取 {{ fileName }}</p>
      <p v-if="error" role="alert">{{ error }}</p>
      <p v-if="message" role="status">{{ message }}</p>
      <div v-if="batch.length" class="grid">
        <table><caption>{{ fileName }} · 匯入預覽</caption>
          <thead><tr><th>列</th><th>料號</th><th>SAP 版次</th><th>站點</th><th>工時</th><th>結果</th></tr></thead>
          <tbody><tr v-for="row in batch" :key="row.line"><td>{{ row.line }}</td><td>{{ row.data?.product }}</td><td>{{ row.data?.sapVersion }}</td><td>{{ row.data?.op }}</td><td>{{ row.data?.unitTime }}</td><td>{{ row.error || '可套用' }}</td></tr></tbody>
        </table>
      </div>
      <div v-if="searched" class="grid">
        <table><caption>查詢結果</caption>
          <thead><tr><th>TH 料號</th><th>SAP 版次</th><th>GBOM</th><th>順序</th><th>作業站</th><th>標準工時</th><th>人時</th><th>機時</th><th>備註</th></tr></thead>
          <tbody>
            <tr v-for="row in result" :key="rowKey(row)"><td>{{ row.product }}</td><td>{{ row.sapVersion }}</td><td>{{ row.gbom }}</td><td>{{ row.sequence }}</td><td>{{ row.op }} {{ row.opName }}</td><td>{{ row.unitTime ?? '未設定' }}</td><td>{{ row.employeeTime }}</td><td>{{ row.equipmentTime }}</td><td>{{ row.remark }}</td></tr>
            <tr v-if="!result.length"><td colspan="9">查無資料</td></tr>
          </tbody>
        </table>
      </div>
      <section v-if="sapPreview.length">
        <h2>轉 SAP 預覽</h2>
        <p>DEV 預覽待傳送的料號與版次。</p>
        <ul><li v-for="item in sapPreview" :key="productKey(item.product, item.version)">{{ item.product }} / {{ item.version }}</li></ul>
      </section>
    </template>
    <p v-else>標工服務待接續。</p>
  </main>
</template>

<style scoped>
.work-time { display: grid; align-content: start; gap: 8px; min-width: 0; padding: 10px; background: #fff; color: #292820; font: 12px "Segoe UI", "Microsoft JhengHei", sans-serif; }
header, .toolbar { display: flex; align-items: center; flex-wrap: wrap; gap: 8px; }
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
caption { text-align: left; padding: 6px; }
th, td { padding: 6px; border: 1px solid #e3e0d6; }
[role=alert] { color: #a32020; }
</style>
