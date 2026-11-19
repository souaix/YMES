<script setup lang="ts">
import { ref } from 'vue'
import ProductPackingSet from './ProductPackingSet.vue'
import { loadDevelopmentProductRelations, productKey, rowsKey, saveDevelopmentProductRelations, seedRows, type ProductRelation, type ProductRow } from './productBasisDevelopment'

type Editor = InstanceType<typeof ProductPackingSet>['$props']
const props = defineProps<{ developmentPreview: boolean; currentUserNo: string }>()
const emit = defineEmits<{ close: [] }>()
const blank: ProductRelation = { processes: [], bom: [], constraints: [], properties: [] }

function readRows(): ProductRow[] {
  if (!props.developmentPreview) return []
  try {
    const stored = JSON.parse(localStorage.getItem(rowsKey) ?? 'null')
    if (Array.isArray(stored)) return stored
  } catch { /* Use the shared fixture when DEV data is unavailable. */ }
  return seedRows
}

function readRelations(): Record<string, ProductRelation> {
  if (!props.developmentPreview) return {}
  return loadDevelopmentProductRelations(readRows())
}

const relations = ref<Record<string, ProductRelation>>({})
const loadError = ref('')
try { relations.value = readRelations() }
catch { loadError.value = 'DEV 產品關聯無法載入。' }

const products: Editor['products'] = readRows().map(row => ({ key: productKey(row.productNo, row.productVersion), productNo: String(row.productNo), productVersion: String(row.productVersion),
  productName: String(row.productName ?? ''), issueState: Number(row.issueState), settings: { boxQty: Number(row.boxQty ?? 0), cartonQty: Number(row.cartonQty ?? 0), palletQty: Number(row.palletQty ?? 0), packOiPath: String(row.packOiPath ?? '') } }))
const product = products[0] ?? { key: '', productNo: '', productVersion: '', productName: '', issueState: 0, settings: { boxQty: 0, cartonQty: 0, palletQty: 0, packOiPath: '' } }

const saveRelations: Editor['saveRelations'] = changes => {
  if (!props.developmentPreview || loadError.value || !changes.length) return false
  try {
    const next = { ...readRelations() }
    if (changes.some(change => !products.some(row => row.key === change.key))) return false
    for (const { key, packingRuleMap } of changes) {
      next[key] = { ...(next[key] ?? blank), packingRuleMap }
    }
    saveDevelopmentProductRelations(next)
    relations.value = next
    return true
  } catch { return false }
}
</script>

<template>
  <main class="mapping-page">
    <p v-if="loadError" role="alert">{{ loadError }}</p>
    <ProductPackingSet map-only :product="product" :products="products" :relations="relations" :current-user="currentUserNo || 'DEV001'"
      :development-preview="developmentPreview" :save-relations="saveRelations" @close="emit('close')" />
  </main>
</template>

<style scoped>
.mapping-page{min-width:0;padding:12px;overflow:auto}.mapping-page :deep(.pack-window){width:100%;min-height:220px;max-height:none;box-shadow:none}
</style>
