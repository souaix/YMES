<script setup lang="ts">
import { computed, ref, shallowRef } from 'vue'
import { parseFirstWorksheetXlsx } from './materialImportWorkbook'
import { rowsKey, seedRows } from './productBasisDevelopment'

type Link = { line: number; sequence: number; from: string; to: string; path: string; sap: string; description: string; columns: string[] }
type Result = { file: string; product: string; version: string; process: string; error: string; links: Link[]; applied?: boolean }

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.prd117-gbom-batch.v1'
const files = shallowRef<File[]>([])
const results = ref<Result[]>([])
const selected = ref(0)
const active = computed(() => results.value[selected.value])
const eligible = computed(() => results.value.filter(result => !result.error && !result.applied))
const busy = ref(false)
const checked = ref(0)
const status = ref('')
const storageError = ref('')

function loadProducts(): typeof seedRows {
  try {
    const raw = JSON.parse(localStorage.getItem(rowsKey) ?? 'null')
    if (Array.isArray(raw)) return raw
  } catch { /* Use the same fixture as PD102. */ }
  return seedRows
}

function selectFiles(event: Event, directory: boolean): void {
  const input = event.target as HTMLInputElement
  const chosen = Array.from(input.files ?? [])
  input.value = ''
  files.value = directory ? chosen.filter(file => file.webkitRelativePath.split('/').length === 2 && /\.xls[xm]?$/i.test(file.name)) : chosen
  results.value = []
  selected.value = checked.value = 0
  status.value = storageError.value = ''
}

function readLinks(sheet: string[][]): Link[] {
  const links: Link[] = []
  for (const [index, row] of sheet.slice(4).entries()) {
    if (!row?.slice(0, 22).some(Boolean)) continue
    const prior = links.at(-1)
    const from = row[6] || prior?.from || ''
    const to = row[7] || prior?.to || ''
    const sequence = row[4] ? Number(row[4]) : !prior ? 0 : from === prior.from ? prior.sequence : prior.sequence + 1
    const path = row[3] || (from.toUpperCase().includes('START') ? 'pass' : '')
    if (!from) throw new Error(`第 ${index + 5} 列缺少起始站。`)
    if (!Number.isSafeInteger(sequence) || sequence < 0) throw new Error(`第 ${index + 5} 列流程順序須為非負整數。`)
    links.push({ line: index + 5, sequence, from, to, path, sap: row[5] || prior?.sap || '', description: row[2] || '', columns: Array.from({ length: 22 }, (_, col) => row[col] ?? '') })
  }
  if (!links.length) throw new Error('第 5 列起沒有 GBOM 明細。')
  return links
}

async function inspect(file: File, products: typeof seedRows): Promise<Result> {
  const base = file.name.replace(/\.[^.]*$/, '')
  const parts = base.split('-')
  const version = parts.pop() ?? ''
  const product = parts.join('-')
  const result: Result = { file: file.name, product, version, process: '', error: '', links: [] }
  try {
    if (!/\.xlsx$/i.test(file.name)) throw new Error('DEV 只讀取 XLSX 工作簿。')
    if (!product || !version) throw new Error('檔名須為產品編號-版次.xlsx。')
    if (!products.some(item => `${item.productNo}-${item.productVersion}`.toUpperCase() === base.toUpperCase() && Number(item.issueState) === 0)) throw new Error('找不到未簽核的 DEV 產品版次。')
    const sheet = await parseFirstWorksheetXlsx(file, true)
    const header = sheet[0] ?? []
    const values = sheet[1] ?? []
    const metadata = (label: string) => values[header.findIndex(cell => cell.toUpperCase() === label.toUpperCase())] ?? ''
    if ([values[0], values[1]].some(value => value?.toUpperCase() !== base.toUpperCase())) throw new Error('A2 與 B2 必須同時等於檔名。')
    if (metadata('版次REV').toUpperCase() !== version.toUpperCase()) throw new Error('版次REV 與檔名版次不符。')
    result.process = metadata('流程編號')
    if (!result.process) throw new Error('缺少流程編號。')
    result.links = readLinks(sheet)
  } catch (error) {
    result.error = error instanceof Error ? error.message : '讀取工作簿失敗。'
  }
  return result
}

async function validateBatch(): Promise<void> {
  if (!props.developmentPreview || busy.value || !files.value.length) return
  busy.value = true
  results.value = []
  selected.value = checked.value = 0
  status.value = storageError.value = ''
  const products = loadProducts()
  const keys = new Set<string>()
  const next: Result[] = []
  try {
    for (const file of files.value) {
      const result = await inspect(file, products)
      const key = JSON.stringify([result.product.toUpperCase(), result.version.toUpperCase()])
      if (!result.error && keys.has(key)) result.error = '這批已有相同產品版次的工作簿。'
      if (!result.error) keys.add(key)
      next.push(result)
      checked.value++
    }
    results.value = next
  } finally {
    busy.value = false
  }
}

function applyBatch(): void {
  if (!props.developmentPreview || !eligible.value.length || !window.confirm(`套用 ${eligible.value.length} 份通過預檢的工作簿？`)) return
  try {
    const saved: Record<string, Result> = JSON.parse(localStorage.getItem(storageKey) ?? '{}')
    for (const result of eligible.value) {
      const key = JSON.stringify([result.product.toUpperCase(), result.version.toUpperCase()])
      saved[key] = { ...result, applied: true }
    }
    localStorage.setItem(storageKey, JSON.stringify(saved))
    status.value = `已將 ${eligible.value.length} 份工作簿套用到本機 DEV。`
    results.value = results.value.map(result => result.error ? result : { ...result, applied: true })
    storageError.value = ''
  } catch {
    storageError.value = '無法儲存本機 DEV 匯入結果。'
  }
}
</script>

<template>
  <main class="gbom-batch">
    <header><h1>GBOM 批次匯入</h1><button @click="emit('close')">關閉</button></header>
    <template v-if="developmentPreview">
      <div class="toolbar">
        <label>來源目錄 <input type="file" webkitdirectory multiple :disabled="busy" @change="selectFiles($event, true)" /></label>
        <label>XLSX 檔案 <input type="file" accept=".xlsx" multiple :disabled="busy" @change="selectFiles($event, false)" /></label>
      </div>
      <div class="toolbar">
        <span>{{ files.length }} 份工作簿</span>
        <button :disabled="busy || !files.length" @click="validateBatch">批次預檢</button>
        <button :disabled="busy || !eligible.length" @click="applyBatch">套用有效資料</button>
      </div>
      <p v-if="busy"><progress :value="checked" :max="files.length" /> {{ checked }} / {{ files.length }}</p>
      <p v-if="status" role="status">{{ status }}</p>
      <p v-if="storageError" role="alert">{{ storageError }}</p>
      <div class="grid">
        <table>
          <thead><tr><th scope="col">檔案</th><th scope="col">產品編號</th><th scope="col">版次</th><th scope="col">流程</th><th scope="col">結果</th></tr></thead>
          <tbody>
            <tr v-for="(row, index) in results" :key="index">
              <td><button :aria-pressed="selected === index" @click="selected = index">{{ row.file }}</button></td>
              <td>{{ row.product }}</td><td>{{ row.version }}</td><td>{{ row.process }}</td>
              <td>{{ row.error || (row.applied ? '已套用' : '預檢通過') }}</td>
            </tr>
            <tr v-if="!results.length"><td colspan="5">目前無預檢結果</td></tr>
          </tbody>
        </table>
      </div>
      <div v-if="active?.links.length" class="grid">
        <table>
          <caption>{{ active.file }} 流程明細</caption>
          <thead><tr><th scope="col">列</th><th scope="col">順序</th><th scope="col">起始站</th><th scope="col">目的站</th><th scope="col">路徑</th><th scope="col">SAP 控制碼</th><th scope="col">說明</th><th scope="col">物料</th></tr></thead>
          <tbody><tr v-for="row in active.links" :key="row.line"><td>{{ row.line }}</td><td>{{ row.sequence }}</td><td>{{ row.from }}</td><td>{{ row.to }}</td><td>{{ row.path }}</td><td>{{ row.sap }}</td><td>{{ row.description }}</td><td>{{ row.columns[11] }}</td></tr></tbody>
        </table>
      </div>
    </template>
    <p v-else>GBOM 批次匯入服務待接續。</p>
  </main>
</template>

<style scoped>
.gbom-batch { display: grid; align-content: start; gap: 8px; min-width: 0; padding: 10px; background: #fff; color: #292820; font: 12px "Segoe UI", "Microsoft JhengHei", sans-serif; }
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
caption { padding: 6px; text-align: left; }
th, td { padding: 6px; border: 1px solid #e3e0d6; }
[role=alert] { color: #a32020; }
</style>
