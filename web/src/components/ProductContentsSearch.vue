<script setup lang="ts">
import { computed, ref } from 'vue'
import { loadDevelopmentProductRelations, productKey, rowsKey, seedRows, type ProductRelation, type ProductRow } from './productBasisDevelopment'

type Filter = 'material' | 'process' | 'property'
type Option = { code: string; detail: string }
const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

function readStored<T extends object>(key: string, fallback: T): T {
  if (!props.developmentPreview) return fallback
  try {
    const saved = localStorage.getItem(key)
    const parsed = saved ? JSON.parse(saved) : null
    if (parsed && typeof parsed === 'object' && Array.isArray(fallback) === Array.isArray(parsed)) return parsed
  } catch { /* Retain the DEV fixture when storage is unavailable. */ }
  return fallback
}

const products = props.developmentPreview ? readStored(rowsKey, seedRows) : []
const relationLoadError = ref('')
let relations: Record<string, ProductRelation> = {}
try { if (props.developmentPreview) relations = loadDevelopmentProductRelations(products) }
catch (cause) { relationLoadError.value = cause instanceof Error ? cause.message : 'DEV 產品關聯無法載入。' }
const sources = Object.values(relations)
const fields: { key: Filter; name: string }[] = [{ key: 'material', name: 'MaterialNo' }, { key: 'process', name: 'ProcessNo' }, { key: 'property', name: 'PropertyNo' }]
const filters = ref<Record<Filter, string>>({ material: '', process: '', property: '' })
const checked = ref<Record<Filter, boolean>>({ material: false, process: false, property: false })
const propertyValue = ref('')
const results = ref<ProductRow[] | null>(null)
const error = ref('')
const lookup = ref<Filter | ''>('')
const lookupQuery = ref('')
const columns: { key: string; label: string }[] = [
  { key: 'productNo', label: 'ProductNo' }, { key: 'productVersion', label: 'ProductVersion' }, { key: 'productName', label: 'ProductName' },
  { key: 'productType', label: 'ProductType' }, { key: 'issueState', label: 'IssueState' }, { key: 'curVersion', label: 'CurVersion' },
  { key: 'description', label: 'Description' }, { key: 'unitType', label: 'UnitType' }, { key: 'unitNo', label: 'UnitNo' },
  { key: 'specNo', label: 'SpecNo' }, { key: 'productCode', label: 'ProductCode' }, { key: 'cartonQty', label: 'CartonQty' },
  { key: 'palletQty', label: 'PalletQty' }, { key: 'boxQty', label: 'BoxQty' }, { key: 'creator', label: 'Creator' },
  { key: 'createDate', label: 'CreateDate' }, { key: 'reviser', label: 'Reviser' }, { key: 'reviseDate', label: 'ReviseDate' },
]
const statusLabels: Record<number, string> = { 0: '未核准', 1: '簽核中', 2: '已核准', [-1]: '不使用' }

function unique(options: Option[]): Option[] {
  return [...new Map(options.filter(row => row.code).map(row => [JSON.stringify([row.code, row.detail]), row])).values()]
    .sort((a, b) => a.code.localeCompare(b.code) || a.detail.localeCompare(b.detail))
}

const catalog: Record<Filter, Option[]> = {
  material: unique(sources.flatMap(source => (source.bom ?? []).map(row => ({ code: String(row.nodeNo ?? ''), detail: String(row.nodeType ?? '') })))),
  process: unique(sources.flatMap(source => (source.processes ?? []).map(row => ({ code: String(row.processNo ?? ''), detail: String(row.processVersion ?? '') })))),
  property: unique(sources.flatMap(source => (source.properties ?? []).map(row => ({ code: String(row.propertyNo ?? ''), detail: '' })))),
}
const options = computed(() => lookup.value ? catalog[lookup.value].filter(row => row.code.toUpperCase().includes(lookupQuery.value.trim().toUpperCase())) : [])

function setChecked(key: Filter, event: Event): void {
  checked.value[key] = (event.target as HTMLInputElement).checked
  if (checked.value[key]) return
  filters.value[key] = ''
  if (key === 'property') propertyValue.value = ''
}

function choose(code: string): void {
  const key = lookup.value
  if (!key) return
  checked.value[key] = true
  filters.value[key] = code
  lookup.value = ''
}

function search(): void {
  if (!props.developmentPreview) return
  if (relationLoadError.value) { error.value = relationLoadError.value; results.value = null; return }
  const missing = fields.find(field => checked.value[field.key] && !filters.value[field.key])
  const value = propertyValue.value.trim()
  error.value = missing ? `請選取 ${missing.name}。` : checked.value.property && !value ? '請輸入 PropertyValue。' : ''
  if (error.value) { results.value = null; return }
  results.value = products.filter(row => {
    const relation = relations[productKey(row.productNo, row.productVersion)]
    return (!checked.value.material || relation?.bom?.some(item => item.nodeNo === filters.value.material))
      && (!checked.value.process || relation?.processes?.some(item => item.processNo === filters.value.process))
      && (!checked.value.property || relation?.properties?.some(item => item.propertyNo === filters.value.property && String(item.defaultValue ?? item.value ?? '') === value))
  })
}

function cell(row: ProductRow, key: string): string {
  if (key === 'issueState') return statusLabels[Number(row.issueState)] ?? ''
  if (key === 'curVersion') return Number(row.curVersion) === 1 ? 'Yes' : 'No'
  const value = String(row[key] ?? '')
  return key.endsWith('Date') ? value.replace('T', ' ').slice(0, 19) : value
}
</script>

<template>
  <main class="product-search">
    <header><h1>產品內容查詢</h1><button @click="emit('close')">關閉</button></header>
    <p v-if="!developmentPreview">產品內容查詢等待正式 API 接入。</p>
    <template v-else>
      <form class="query" @submit.prevent="search">
        <div v-for="field in fields" :key="field.key" class="condition">
          <label><input type="checkbox" :checked="checked[field.key]" @change="setChecked(field.key, $event)" /> {{ field.name }}</label>
          <input :value="filters[field.key]" readonly :aria-label="field.name" />
          <button type="button" :aria-label="`選取 ${field.name}`" @click="lookup = field.key; lookupQuery = ''">…</button>
          <label v-if="field.key === 'property'">PropertyValue <input v-model="propertyValue" :disabled="!checked.property" /></label>
        </div>
        <button type="submit">Query</button>
      </form>
      <p v-if="error || relationLoadError" class="error" role="alert">{{ error || relationLoadError }}</p>
      <p v-if="results" role="status">共 {{ results.length }} 筆產品版次</p>
      <div class="grid"><table><thead><tr><th v-for="column in columns" :key="column.key">{{ column.label }}</th></tr></thead>
        <tbody><tr v-for="row in results ?? []" :key="productKey(row.productNo, row.productVersion)"><td v-for="column in columns" :key="column.key" :class="{ quantity: column.key.endsWith('Qty') }">{{ cell(row, column.key) }}</td></tr>
        <tr v-if="!results?.length"><td :colspan="columns.length">{{ results ? '沒有符合條件的產品。' : '請執行查詢。' }}</td></tr></tbody>
      </table></div>
    </template>
    <div v-if="lookup" class="overlay"><section class="lookup" role="dialog" aria-modal="true" aria-labelledby="product-search-lookup-title">
      <header><h2 id="product-search-lookup-title">選取 {{ fields.find(field => field.key === lookup)?.name }}</h2><button @click="lookup = ''">關閉</button></header>
      <input v-model="lookupQuery" type="search" placeholder="搜尋編號" aria-label="搜尋編號" />
      <div class="grid"><table><tbody>
        <tr v-for="option in options" :key="JSON.stringify([option.code, option.detail])"><td><button @click="choose(option.code)">{{ option.code }}</button></td><td v-if="lookup !== 'property'">{{ option.detail }}</td></tr>
        <tr v-if="!options.length"><td>沒有符合的項目。</td></tr>
      </tbody></table></div>
    </section></div>
  </main>
</template>

<style scoped>
.product-search{display:flex;flex-direction:column;gap:8px;min-width:0;padding:10px;background:#fff;color:#292820;font:12px "Segoe UI","Microsoft JhengHei",sans-serif}header,.query,.condition{display:flex;align-items:center;gap:8px}header{justify-content:space-between}.query{flex-wrap:wrap;padding:8px;background:#f5f3eb;border:1px solid #dedbd0}.condition{flex-wrap:wrap}.condition label{white-space:nowrap}h1,h2,p{margin:0}h1{font-size:17px}h2{font-size:14px}
.grid{overflow:auto;border:1px solid #d9d6cb}table{width:100%;border-collapse:collapse;text-align:left;white-space:nowrap}th,td{padding:6px;border:1px solid #e3e0d6}thead{background:#f5f3eb}.quantity{text-align:right}input,button{font:inherit}input{min-width:0;padding:4px;border:1px solid #aaa797}input:read-only{background:#f4f3ec}button{padding:4px 10px;border:1px solid #aaa797;border-radius:3px;background:#f8f7f2;color:inherit}.error{color:#a32020}
.overlay{position:fixed;inset:0;z-index:91;display:grid;place-items:center;padding:12px;background:#0005}.lookup{display:grid;gap:12px;width:min(480px,95vw);max-height:90vh;padding:16px;background:#fff;box-shadow:0 8px 30px #0004}
</style>
