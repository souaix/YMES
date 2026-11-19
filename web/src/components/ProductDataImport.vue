<script setup lang="ts">
import { ref } from 'vue'
import { parseWorksheetsXlsx } from './materialImportWorkbook'

type ProductImportRow = {
  productNo: string
  productVersion: string
  productName: string
  productType: string
  unitType: string
  unitNo: string
  description: string
  erpUnitNo: string
}

const props = defineProps<{
  developmentPreview: boolean
  importProducts: (rows: ProductImportRow[]) => string[]
}>()
const emit = defineEmits<{ close: [] }>()

const productSections = ['[PRD Basis]', '[PRD Process]', '[PRD Property]', '[Recipe Group]', '[EQP Constraint]', '[Accessory]', '[Packing Set]', '[EQP Capitem]']
const opSections = ['[OP Description]', '[Sub OP]', '[Run Time]']
const parameterSections = ['[Attrib]', '[EQP Recipe]']

const productRows = ref<ProductImportRow[]>([])
const processRows = ref<{ processNo: string; processVersion: string }[]>([])
const messages = ref<string[]>([])
const fileName = ref('')
const busy = ref(false)
const error = ref('')

function findSections(sheet: string[][], markers: string[], name: string): number[] {
  const positions: number[] = []
  let previous = -1
  for (const marker of markers) {
    const position = sheet.findIndex((row, index) => index > previous && row[0]?.toUpperCase() === marker.toUpperCase())
    if (position < 0) throw new Error(`${name} 缺少區段 ${marker}，或區段順序錯誤。`)
    positions.push(position)
    previous = position
  }
  return positions
}

async function chooseFile(event: Event): Promise<void> {
  const file = (event.target as HTMLInputElement).files?.[0]
  productRows.value = []
  processRows.value = []
  messages.value = []
  error.value = ''
  fileName.value = file?.name ?? ''
  if (!file) return
  busy.value = true
  try {
    const [process, product, op, parameter] = await parseWorksheetsXlsx(file, 5)
    const [basisStart, basisEnd] = findSections(product, productSections, 'PRD Data')
    findSections(op, opSections, 'PRD OP Data')
    findSections(parameter, parameterSections, 'OP Parameter')
    const processes = new Map<string, { processNo: string; processVersion: string }>()
    for (const [index, row] of process.slice(1).entries()) {
      if (!row.some(Boolean)) continue
      const processNo = row[1]?.toUpperCase()
      const processVersion = row[2]?.toUpperCase()
      if (!processNo || !processVersion) throw new Error(`Process 第 ${index + 2} 個資料列缺少流程編號或版本。`)
      processes.set(JSON.stringify([processNo, processVersion]), { processNo, processVersion })
    }
    const products: ProductImportRow[] = []
    for (const [productNo, productVersion, productName, productType, unitType, unitNo, description = '', erpUnitNo] of product.slice(basisStart + 2, basisEnd)) {
      if (!productNo) continue
      if (!productVersion || !productName || !productType || !unitType || !unitNo || !erpUnitNo) {
        throw new Error(`產品 ${productNo} 缺少主檔必填欄位。`)
      }
      products.push({ productNo: productNo.toUpperCase(), productVersion: productVersion.toUpperCase(),
        productName, productType: productType.toUpperCase(), unitType, unitNo, description, erpUnitNo })
    }
    productRows.value = products
    processRows.value = [...processes.values()]
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : '無法讀取產品匯入工作簿。'
  } finally {
    busy.value = false
  }
}

function confirm(): void {
  if (!props.developmentPreview || !productRows.value.length || messages.value.length) return
  messages.value = props.importProducts(productRows.value)
}
</script>

<template>
  <section class="product-import" role="dialog" aria-modal="true" aria-labelledby="product-import-title">
    <header><h2 id="product-import-title">產品資料匯入</h2><button type="button" aria-label="關閉" @click="emit('close')">×</button></header>
    <p v-if="!developmentPreview">工作簿匯入等待 API 接入。</p>
    <template v-else>
      <p>DEV 可用測試 XLSX 預覽流程並匯入產品主檔。子表只檢查區段格式。</p>
      <label>測試工作簿 <input type="file" accept=".xlsx" :disabled="busy" @change="chooseFile" /></label>
      <p v-if="busy" role="status">正在讀取 {{ fileName }}…</p>
      <p v-if="error" class="error" role="alert">{{ error }}</p>
      <div class="results" v-if="processRows.length || productRows.length">
        <table>
          <caption>流程預覽</caption><thead><tr><th>流程編號</th><th>版本</th></tr></thead>
          <tbody><tr v-for="row in processRows" :key="JSON.stringify([row.processNo, row.processVersion])"><td>{{ row.processNo }}</td><td>{{ row.processVersion }}</td></tr></tbody>
        </table>
        <table>
          <caption>產品主檔</caption><thead><tr><th>產品編號</th><th>版本</th><th>結果</th></tr></thead>
          <tbody><tr v-for="(row, index) in productRows" :key="index"><td>{{ row.productNo }}</td><td>{{ row.productVersion }}</td><td>{{ messages[index] ?? '待匯入' }}</td></tr></tbody>
        </table>
      </div>
      <p v-else-if="fileName && !error && !busy">工作簿沒有可預覽的產品或流程資料。</p>
      <p v-if="messages.length" role="status">已處理 {{ messages.length }} 筆產品。</p>
    </template>
    <footer>
      <button type="button" :disabled="!developmentPreview || busy || !productRows.length || Boolean(messages.length)" @click="confirm">匯入 DEV 產品</button>
      <button type="button" @click="emit('close')">關閉</button>
    </footer>
  </section>
</template>

<style scoped>
.product-import{display:grid;gap:12px;width:min(750px,95vw);max-height:90vh;overflow:auto;padding:16px;background:#fff;border:1px solid #c9c7bd;border-radius:6px;box-shadow:0 12px 36px #28241738}
header,footer{display:flex;align-items:center;gap:8px}header{justify-content:space-between}footer{justify-content:flex-end}h2,p{margin:0}
.results{display:grid;grid-template-columns:repeat(auto-fit,minmax(240px,1fr));gap:12px;align-items:start}table{width:100%;border-collapse:collapse;text-align:left}caption{text-align:left;font-weight:600;padding-bottom:5px}thead{background:#f4f2eb}th,td{padding:6px;border:1px solid #dedbd1}
button{padding:4px 12px;border:1px solid #aaa797;border-radius:3px;background:#f8f7f2;color:inherit}button:disabled{opacity:.55}.error{color:#a32020}
</style>
