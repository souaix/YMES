<script setup lang="ts">
import { computed, ref } from 'vue'
import { rowsKey, relationKey, seedRows, seedRelations, type ProductRow, type ProductRelation } from './productBasisDevelopment'
import { applyOperationDefaults, copyOperationSettings, defaultOperations, operationTargets, type DefaultMode, type DefaultSelection } from './productOperationDefaults'

const props = defineProps<{ developmentPreview: boolean; amendOnly?: boolean; copyOnly?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const filter = ref({ productNo: '', processNo: '', opNo: '' })
const flags = ref<DefaultSelection>({ parameter: true, description: true, qc: Boolean(props.amendOnly || props.copyOnly), businessRule: true, resource: true, subOp: true })
const flagFields: { key: keyof DefaultSelection; label: string }[] = [
  { key: 'parameter', label: 'Parameter' }, { key: 'description', label: 'Description' }, { key: 'qc', label: 'QC Item' },
  { key: 'businessRule', label: 'Business Rule' }, { key: 'resource', label: 'OP Resource' }, { key: 'subOp', label: 'SubOP' },
]
const allSegment = ref(false)
const selectedOperations = ref<string[]>([])
const results = ref<ProductRow[] | null>(null)
const selected = ref<string[]>([])
const messages = ref<Record<string, string>>({})
const error = ref('')
const processing = ref(false)
const cancelled = ref(false)
const done = ref(0)
const total = ref(0)
const keyOf = (row: ProductRow) => `${row.productNo}|${row.productVersion}`
const sourceKey = ref('')
const sourceOptions = ref<ProductRow[]>([])
const copyProcess = ref(true)
const canRun = computed(() => !processing.value && selected.value.length > 0
  && (Object.values(flags.value).some(Boolean) || (props.copyOnly && copyProcess.value))
  && (!props.copyOnly || Boolean(sourceKey.value)))
const title = computed(() => props.copyOnly ? '產品作業站批次複製' : props.amendOnly ? '產品作業站修正' : '產品作業站預設')
const blankRelation: ProductRelation = { processes: [], bom: [], constraints: [], properties: [] }

function eligibleForCopy(row: ProductRow): boolean {
  return !row.ecnControl || String(row.ecnControl).toUpperCase() === 'N'
}

function readSnapshot(): { products: ProductRow[]; relations: Record<string, ProductRelation> } {
  const storedProducts = localStorage.getItem(rowsKey)
  const storedRelations = localStorage.getItem(relationKey)
  const products = storedProducts ? JSON.parse(storedProducts) : seedRows
  const relations = storedRelations ? JSON.parse(storedRelations) : seedRelations
  if (!Array.isArray(products) || !relations || Array.isArray(relations) || typeof relations !== 'object') throw new Error('本機 DEV 資料格式無法讀取。')
  return { products, relations }
}

function search(): void {
  if (!props.developmentPreview || processing.value) return
  error.value = ''; selected.value = []; messages.value = {}; total.value = 0
  try {
    const { products, relations } = readSnapshot()
    if (props.copyOnly) {
      sourceOptions.value = products.filter(row => Number(row.issueState) === 2 && eligibleForCopy(row))
      if (!sourceOptions.value.some(row => keyOf(row) === sourceKey.value)) sourceKey.value = ''
    }
    const match = (actual: unknown, prefix: string) => String(actual ?? '').toUpperCase().startsWith(prefix.trim().toUpperCase())
    const query = filter.value
    results.value = products.filter(row => {
      if (Number(row.issueState) !== 0 || !match(row.productNo, query.productNo) || (props.copyOnly && !eligibleForCopy(row))) return false
      if (props.copyOnly) return true
      const relation = relations[keyOf(row)]
      return (!query.processNo.trim() || relation?.processes?.some(process => match(process.processNo, query.processNo)))
        && (!query.opNo.trim() || relation?.parameters?.ops.some(op => match(op.opNo, query.opNo)))
    }).sort((a, b) => String(a.productNo).localeCompare(String(b.productNo)) || String(b.productVersion).localeCompare(String(a.productVersion)))
  } catch (cause) { results.value = null; error.value = cause instanceof Error ? cause.message : '無法讀取本機 DEV 資料。' }
}

async function run(mode: DefaultMode | 'copy'): Promise<void> {
  if (!props.developmentPreview || !canRun.value) return
  if (mode === 'default' && !window.confirm('所選作業站的已選項目會被預設資料取代，確定執行？')) return
  if (mode === 'copy' && !window.confirm('來源的已選項目會取代目標對應設定，確定複製？')) return
  const targets = [...selected.value]
  const scope = [...selectedOperations.value]
  const selection = { ...flags.value }
  const useAll = allSegment.value
  const source = sourceKey.value
  const includeProcess = copyProcess.value
  processing.value = true; cancelled.value = false; messages.value = {}; error.value = ''; done.value = 0; total.value = targets.length
  try {
    for (const key of targets) {
      await new Promise<void>(resolve => window.setTimeout(resolve, 0))
      if (cancelled.value) break
      try {
        const { products, relations } = readSnapshot()
        const product = products.find(row => keyOf(row) === key)
        if (!product || Number(product.issueState) !== 0) { messages.value[key] = '產品版次不存在或已變更狀態。'; continue }
        const relation = relations[key]
        if (mode === 'copy') {
          const copySource = products.find(row => keyOf(row) === source)
          if (!copySource || Number(copySource.issueState) !== 2 || source === key || !eligibleForCopy(copySource) || !eligibleForCopy(product)) {
            messages.value[key] = '來源或目標不符版次狀態與 ECN 條件。'; continue
          }
          const updated = copyOperationSettings(product, relations[source] ?? blankRelation, relation ?? blankRelation, selection, includeProcess)
          localStorage.setItem(relationKey, JSON.stringify({ ...relations, [key]: updated }))
          messages.value[key] = '已複製所選項目。'
          continue
        }
        const operations = relation ? operationTargets(relation, mode, useAll, scope) : []
        if (!relation || !operations.length) { messages.value[key] = '沒有符合範圍的 DEV 流程作業。'; continue }
        const updated = applyOperationDefaults(product, relation, mode, selection, operations)
        localStorage.setItem(relationKey, JSON.stringify({ ...relations, [key]: updated }))
        messages.value[key] = `${mode === 'default' ? '已帶入預設' : '已補齊缺項'}，${operations.length} 個作業站。`
      } catch (cause) { messages.value[key] = cause instanceof Error ? cause.message : '無法儲存本機 DEV 資料。' }
      finally { done.value++ }
    }
  } finally {
    if (cancelled.value) for (const key of targets) messages.value[key] ??= '已取消，尚未執行。'
    processing.value = false
  }
}
if (props.copyOnly && props.developmentPreview) search()
</script>

<template>
  <main class="batch-page">
    <header><h1>{{ title }}</h1><button :disabled="processing" @click="emit('close')">關閉</button></header>
    <p v-if="!developmentPreview">批次作業站設定等待正式 API 接入。</p>
    <template v-else>
      <div v-if="copyOnly" class="toolbar"><label>來源產品版次 <select v-model="sourceKey" :disabled="processing"><option value="">請選擇已核准來源</option><option v-for="row in sourceOptions" :key="keyOf(row)" :value="keyOf(row)">{{ row.productNo }} / {{ row.productVersion }} {{ row.productName }}</option></select></label></div>
      <form class="toolbar" @submit.prevent="search"><label>ProductNo <input v-model="filter.productNo" :disabled="processing" /></label><template v-if="!copyOnly"><label>ProcessNo <input v-model="filter.processNo" :disabled="processing" /></label><label>OPNo <input v-model="filter.opNo" :disabled="processing" /></label></template><button :disabled="processing">查詢</button></form>
      <template v-if="!amendOnly">
        <fieldset class="toolbar" :disabled="processing"><legend>更新項目</legend><label v-if="copyOnly"><input v-model="copyProcess" type="checkbox" />Product Process</label><label v-for="item in flagFields" :key="item.key"><input v-model="flags[item.key]" type="checkbox" />{{ item.label }}</label></fieldset>
        <fieldset v-if="!copyOnly" class="toolbar" :disabled="processing"><legend>Default OP</legend><label><input v-model="allSegment" type="radio" :value="false" />Product Process OP</label><label><input v-model="allSegment" type="radio" :value="true" />All PS OP</label></fieldset>
        <fieldset v-if="!copyOnly" class="toolbar" :disabled="processing"><legend>指定作業站</legend><label v-for="op in defaultOperations" :key="op.opNo"><input v-model="selectedOperations" type="checkbox" :value="op.opNo" />{{ op.opNo }} {{ op.opName }}</label><span v-if="!selectedOperations.length">套用至範圍內全部作業站</span></fieldset>
      </template>
      <div class="toolbar"><button :disabled="processing || !results?.length" @click="selected = results?.map(keyOf) ?? []">全選</button><button :disabled="processing || !selected.length" @click="selected = []">取消全選</button><template v-if="copyOnly"><button :disabled="!canRun" @click="run('copy')">Confirm</button></template><template v-else><button v-if="!amendOnly" :disabled="!canRun" @click="run('default')">Default</button><button :disabled="!canRun" @click="run('amend')">Amend</button></template><button v-if="processing" @click="cancelled = true">取消</button></div>
      <div v-if="total" class="progress" role="status"><progress :value="done" :max="total" /> {{ done }} / {{ total }}</div>
      <p v-if="error" role="alert" class="error">{{ error }}</p>
      <div class="grid"><table><thead><tr><th>選取</th><th>ProductNo</th><th>ProductVersion</th><th>ProductName</th><th>目前版</th><th>Message</th></tr></thead><tbody>
        <tr v-for="row in results ?? []" :key="keyOf(row)"><td><input v-model="selected" type="checkbox" :value="keyOf(row)" :disabled="processing" :aria-label="`選取 ${row.productNo} ${row.productVersion}`" /></td><td>{{ row.productNo }}</td><td>{{ row.productVersion }}</td><td>{{ row.productName }}</td><td>{{ row.curVersion ? 'Yes' : 'No' }}</td><td>{{ messages[keyOf(row)] }}</td></tr>
        <tr v-if="!results?.length"><td colspan="6">{{ results ? '沒有符合條件的未核准產品版次。' : '請執行查詢。' }}</td></tr>
      </tbody></table></div>
    </template>
  </main>
</template>

<style scoped>
.batch-page{display:flex;flex-direction:column;gap:8px;min-width:0;padding:10px;background:#fff;color:#292820;font:12px "Segoe UI","Microsoft JhengHei",sans-serif}header,.toolbar,.progress{display:flex;align-items:center;gap:8px}header{justify-content:space-between}.toolbar{flex-wrap:wrap;padding:6px;background:#f5f3eb;border:1px solid #dedbd0}.toolbar label{white-space:nowrap}h1,p{margin:0}h1{font-size:17px}
.grid{overflow:auto;border:1px solid #d9d6cb}table{width:100%;border-collapse:collapse;text-align:left;white-space:nowrap}th,td{padding:6px;border:1px solid #e3e0d6}thead{background:#f5f3eb}button,input{font:inherit}button{padding:4px 10px;border:1px solid #aaa797;border-radius:3px;background:#f8f7f2;color:inherit}button:disabled{opacity:.5}.error{color:#a32020}
</style>
