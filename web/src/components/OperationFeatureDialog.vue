<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

type Feature = 'property' | 'resource' | 'business-rule' | 'parameter' | 'qc-item' | 'error' | 'copy-op-data' | 'sub-op' | 'import'
type Cell = string | number | boolean
type FeatureRow = Record<string, Cell>

const props = defineProps<{ feature: Feature; operationNo: string; operationName: string }>()
const emit = defineEmits<{ close: []; saved: [] }>()
const labels: Record<Feature, string> = {
  property: 'Property', resource: 'Resource', 'business-rule': 'Business Rule', parameter: 'Parameter',
  'qc-item': 'QC Item', error: 'Error', 'copy-op-data': '複製作業資料', 'sub-op': 'SubOP', import: '作業站匯入',
}
const requiredFields: Partial<Record<Feature, string[]>> = {
  property: ['propertyNo'], 'business-rule': ['fromNode', 'ruleNo', 'toNode'], parameter: ['parameterNo'],
  'qc-item': ['qcItemNo'], error: ['errorNo'], 'sub-op': ['subOperationNo'],
}
const layouts: Partial<Record<Feature, { fields: { key: string; label: string; type?: 'text' | 'number' | 'select'; options?: string[] }[] }>> = {
  property: { fields: [{ key: 'propertyNo', label: '屬性編號' }, { key: 'propertyValue', label: '屬性值' }, { key: 'description', label: '說明' }] },
  'business-rule': { fields: [{ key: 'sequence', label: '順序', type: 'number' }, { key: 'fromNode', label: '來源節點' }, { key: 'ruleNo', label: '規則編號' }, { key: 'toNode', label: '目標節點' }] },
  parameter: { fields: [{ key: 'parameterType', label: '類型', type: 'select', options: ['Attrib', 'Rule', 'EQP Recipe'] }, { key: 'parameterNo', label: '參數編號' }, { key: 'parameterValue', label: '值' }, { key: 'sequence', label: '順序', type: 'number' }, { key: 'description', label: '說明' }] },
  'qc-item': { fields: [{ key: 'opportunity', label: '檢驗時機', type: 'select', options: ['Inline', 'Offline'] }, { key: 'itemType', label: '項目類型', type: 'select', options: ['Attribute', 'Group', 'Variable'] }, { key: 'qcItemNo', label: 'QC Item' }, { key: 'sequence', label: '順序', type: 'number' }] },
  error: { fields: [{ key: 'errorNo', label: 'Error 編號' }, { key: 'errorName', label: 'Error 名稱' }] },
  'sub-op': { fields: [{ key: 'sequence', label: 'SubOP 順序', type: 'number' }, { key: 'subOperationNo', label: '子作業站編號' }, { key: 'subOperationName', label: '子作業站名稱' }] },
}
const headings: Record<string, string> = {
  propertyNo: '屬性編號', propertyValue: '屬性值', description: '說明', sequence: '順序', fromNode: '來源節點', ruleNo: '規則編號', toNode: '目標節點',
  parameterType: '類型', parameterNo: '參數編號', parameterValue: '值', opportunity: '檢驗時機', itemType: '項目類型', qcItemNo: 'QC Item',
  subOperationNo: '子作業站編號', subOperationName: '子作業站名稱', errorNo: 'Error 編號', errorName: 'Error 名稱', enabled: '套用',
  resourceNo: '資源編號', resourceName: '資源名稱', resourceType: '資源類型',
}
const seedRows: Partial<Record<Feature, FeatureRow[]>> = {
  property: [{ id: 'P-01', propertyNo: 'OP_CATEGORY', propertyValue: 'ASSEMBLY', description: 'DEV 範例' }],
  resource: [{ id: 'A-01', resourceType: 'Area', resourceNo: 'AREA-01', resourceName: 'DEV 區域' }],
  'business-rule': [{ id: 'BR-01', sequence: 10, fromNode: 'START', ruleNo: 'BR-DEV-01', toNode: 'END' }],
  parameter: [{ id: 'PA-01', parameterType: 'Attrib', parameterNo: 'CPass', parameterValue: 'Y', sequence: 10, description: 'DEV 範例' }],
  'qc-item': [{ id: 'QC-01', opportunity: 'Inline', itemType: 'Attribute', qcItemNo: 'QC-DEV-01', sequence: 10 }],
  error: [{ id: 'E-01', errorNo: 'ERR-DEV-01', errorName: 'DEV 範例錯誤', enabled: true }],
  'sub-op': [{ id: 'S-01', sequence: 10, subOperationNo: 'OP-DEV-SUB', subOperationName: 'DEV 子作業站' }],
}
const storageKey = computed(() => `ymes.dev.operation.${props.operationNo}.${props.feature}`)
const rows = ref<FeatureRow[]>([])
const selectedIds = ref<string[]>([])
const notice = ref('')
const draft = ref<Record<string, string | number>>({})
const errorOnlySelected = ref(false)
const resourceKind = ref('Area')
const resourceSelection = ref<string[]>([])
const includeOpArea = ref(true)
const includePdLineArea = ref(true)
const parameterTab = ref('Attrib')
const qcOpportunity = ref('Inline')
const qcItemCategory = ref('Attribute')
const copySourceNo = ref('')
const copyOptions = ref<Record<string, boolean>>({ Property: true, Resource: true, BusinessRule: true, Parameter: true, QCItem: true, Error: true, SubOP: true })
const importRows = ref<{ line: number; raw: string; status: string }[]>([])
const fileName = ref('')
const fields = computed(() => layouts[props.feature]?.fields ?? [])
const title = computed(() => `${labels[props.feature]} — ${props.operationNo} ${props.operationName}`)
const columns = computed(() => props.feature === 'resource' ? ['resourceType', 'resourceNo', 'resourceName'] : props.feature === 'error' ? ['enabled', 'errorNo', 'errorName'] : fields.value.map((field) => field.key))
const visibleRows = computed(() => {
  if (props.feature === 'error' && errorOnlySelected.value) return rows.value.filter((row) => Boolean(row.enabled))
  if (props.feature === 'parameter') return rows.value.filter((row) => row.parameterType === parameterTab.value)
  if (props.feature === 'qc-item') return rows.value.filter((row) => row.opportunity === qcOpportunity.value && row.itemType === qcItemCategory.value)
  return rows.value
})
const availableResources = computed(() => {
  const source = resourceKind.value === 'Area'
    ? [
        ...(includeOpArea.value ? [{ resourceNo: 'AREA-01', resourceName: 'DEV 區域一', resourceScope: 'OP Area' }, { resourceNo: 'AREA-02', resourceName: 'DEV 區域二', resourceScope: 'OP Area' }] : []),
        ...(includePdLineArea.value ? [{ resourceNo: 'PD-01', resourceName: 'DEV 產線區域', resourceScope: 'PDLine Area' }] : []),
      ]
    : [{ resourceNo: 'TYPE-01', resourceName: 'DEV 設備類別一', resourceScope: 'Equipment Type' }, { resourceNo: 'TYPE-02', resourceName: 'DEV 設備類別二', resourceScope: 'Equipment Type' }]
  return source.filter((item) => !rows.value.some((row) => row.resourceType === resourceKind.value && row.resourceNo === item.resourceNo))
})

function loadRows(): void {
  if (props.feature === 'import') {
    rows.value = []; selectedIds.value = []; draft.value = {}; notice.value = ''; errorOnlySelected.value = false
    return
  }
  try {
    const stored = localStorage.getItem(storageKey.value)
    rows.value = stored ? JSON.parse(stored) as FeatureRow[] : structuredClone(seedRows[props.feature] ?? [])
    if (!stored) persist()
  } catch { rows.value = structuredClone(seedRows[props.feature] ?? []) }
  selectedIds.value = []; errorOnlySelected.value = false; parameterTab.value = 'Attrib'; qcOpportunity.value = 'Inline'; qcItemCategory.value = 'Attribute'
  const next: Record<string, string | number> = {}
  for (const field of fields.value) next[field.key] = field.type === 'number' ? 10 : field.options?.[0] ?? ''
  draft.value = next
  notice.value = ''
}
function persist(): void { localStorage.setItem(storageKey.value, JSON.stringify(rows.value)) }
function addRow(): void {
  if (props.feature === 'resource') {
    if (!resourceSelection.value.length) { notice.value = '請先從可選資源清單選取項目。'; return }
    for (const item of resourceSelection.value) {
      const source = availableResources.value.find((candidate) => candidate.resourceNo === item)
      if (source) rows.value.push({ id: crypto.randomUUID(), resourceType: resourceKind.value, ...source })
    }
    resourceSelection.value = []
  } else {
    const missing = (requiredFields[props.feature] ?? []).filter((key) => !String(draft.value[key] ?? '').trim())
    if (missing.length) {
      const names = missing.map((key) => fields.value.find((field) => field.key === key)?.label ?? key)
      notice.value = `請填寫：${names.join('、')}`
      return
    }
    rows.value.push({ id: crypto.randomUUID(), ...draft.value, ...(props.feature === 'error' ? { enabled: true } : {}) })
    for (const field of fields.value) draft.value[field.key] = field.type === 'number' ? Number(draft.value[field.key] ?? 0) + 10 : ''
  }
  persist(); notice.value = '已加入開發假資料。'
}
function removeSelected(): void {
  rows.value = rows.value.filter((row) => !selectedIds.value.includes(String(row.id)))
  selectedIds.value = []; persist(); notice.value = '已移除所選開發假資料。'
}
function toggleAll(enabled: boolean): void { rows.value = rows.value.map((row) => ({ ...row, enabled })); persist() }
function setParameterTab(tab: string): void { parameterTab.value = tab; draft.value.parameterType = tab }
function setQcFilter(opportunity: string, category: string): void { qcOpportunity.value = opportunity; qcItemCategory.value = category; draft.value.opportunity = opportunity; draft.value.itemType = category }
function displayCell(value: unknown): string { return typeof value === 'boolean' ? (value ? '是' : '否') : String(value ?? '') }
function copyOperationData(): void {
  if (!copySourceNo.value.trim()) { notice.value = '請輸入來源作業站編號。'; return }
  const typeMap: Record<string, Feature> = { Property: 'property', Resource: 'resource', BusinessRule: 'business-rule', Parameter: 'parameter', QCItem: 'qc-item', Error: 'error', SubOP: 'sub-op' }
  let count = 0
  for (const [name, feature] of Object.entries(typeMap)) {
    if (!copyOptions.value[name]) continue
    let source: FeatureRow[] = []
    try { source = JSON.parse(localStorage.getItem(`ymes.dev.operation.${copySourceNo.value.trim()}.${feature}`) ?? '[]') as FeatureRow[] } catch { source = [] }
    localStorage.setItem(`ymes.dev.operation.${props.operationNo}.${feature}`, JSON.stringify(source.map((row) => ({ ...row, id: crypto.randomUUID() }))))
    count += source.length
  }
  notice.value = `已在瀏覽器假資料中複製所選設定，共 ${count} 筆；未呼叫正式 API。`
}
async function readImport(event: Event): Promise<void> {
  const file = (event.target as HTMLInputElement).files?.[0]
  if (!file) return
  fileName.value = file.name
  const lines = (await file.text()).split(/\r?\n/).filter((line) => line.trim())
  importRows.value = lines.slice(1).map((raw, index) => ({ line: index + 2, raw, status: raw.split(',')[0]?.trim() ? '待匯入' : '缺少作業編號' }))
  notice.value = `已讀取 ${importRows.value.length} 筆預覽資料；首列視為欄位標題。`
}
function saveImport(): void {
  const accepted = importRows.value.filter((row) => row.status === '待匯入')
  localStorage.setItem(`ymes.dev.operation.${props.operationNo}.import`, JSON.stringify({ fileName: fileName.value, rows: accepted }))
  notice.value = `DEV 假資料匯入預覽完成：${accepted.length} 筆可匯入，${importRows.value.length - accepted.length} 筆有錯誤。`
}
function confirm(): void {
  if (props.feature !== 'import') persist()
  emit('saved'); emit('close')
}
onMounted(loadRows)
watch(storageKey, loadRows)
</script>

<template>
  <div class="op-feature-backdrop" @click.self="emit('close')">
    <section class="op-feature-dialog" role="dialog" aria-modal="true" :aria-label="title">
      <header><div><small>開發區假資料</small><h2>{{ title }}</h2></div><button type="button" aria-label="關閉" @click="emit('close')">×</button></header>
      <p class="op-feature-note">WinForms 子功能畫面復刻中。此頁資料只存在瀏覽器 localStorage，不會寫入 MES/ YMES 資料庫。</p>

      <template v-if="feature === 'resource'">
        <div class="op-resource-switch"><label><input v-model="resourceKind" type="radio" value="Area" />By Area</label><label><input v-model="resourceKind" type="radio" value="EquipmentType" />By EQPType</label><label v-if="resourceKind==='Area'"><input v-model="includeOpArea" type="checkbox" />OP Area</label><label v-if="resourceKind==='Area'"><input v-model="includePdLineArea" type="checkbox" />PDLine Area</label></div>
        <div class="op-resource-columns"><fieldset><legend>可選資源</legend><select v-model="resourceSelection" multiple size="8"><option v-for="item in availableResources" :key="item.resourceNo" :value="item.resourceNo">{{ item.resourceNo }} — {{ item.resourceName }}</option></select></fieldset><div class="op-resource-transfer"><button type="button" class="op-mini-button" @click="addRow">&gt;&gt;</button><button type="button" class="op-mini-button" :disabled="!selectedIds.length" @click="removeSelected">&lt;&lt;</button></div><fieldset><legend>已選資源</legend><label v-for="row in rows" :key="String(row.id)" class="op-resource-item"><input v-model="selectedIds" type="checkbox" :value="String(row.id)" />{{ row.resourceNo }} — {{ row.resourceName }} ({{ row.resourceType }})</label><p v-if="!rows.length">尚未選取資源</p></fieldset></div>
      </template>

      <template v-else-if="feature === 'copy-op-data'">
        <div class="op-copy-form"><label>來源作業站編號<input v-model="copySourceNo" placeholder="輸入來源 OPNo" /></label><fieldset><legend>複製資料類別</legend><label v-for="key in Object.keys(copyOptions)" :key="key"><input v-model="copyOptions[key]" type="checkbox" />{{ key }}</label></fieldset><p>來源資料需先在本瀏覽器的 DEV 假資料中建立，否則對應類別會複製 0 筆。</p><button class="op-mini-button" type="button" @click="copyOperationData">複製所選資料</button></div>
      </template>

      <template v-else-if="feature === 'import'">
        <div class="op-import"><label>選擇 CSV 檔<input type="file" accept=".csv,text/csv" @change="readImport" /></label><p v-if="fileName">{{ fileName }}：{{ importRows.length }} 筆</p><div class="op-feature-grid"><table><thead><tr><th>列</th><th>CSV 內容</th><th>檢查結果</th></tr></thead><tbody><tr v-for="row in importRows" :key="row.line"><td>{{ row.line }}</td><td>{{ row.raw }}</td><td>{{ row.status }}</td></tr><tr v-if="!importRows.length"><td colspan="3">選取 CSV 後顯示預覽。</td></tr></tbody></table></div><button class="op-mini-button" type="button" :disabled="!importRows.length" @click="saveImport">執行 DEV 預覽匯入</button></div>
      </template>

      <template v-else>
        <div v-if="feature==='parameter'" class="op-feature-tabs"><button v-for="tab in ['Attrib','Rule','EQP Recipe']" :key="tab" type="button" :class="{active:parameterTab===tab}" @click="setParameterTab(tab)">{{ tab }}</button></div>
        <div v-if="feature==='qc-item'" class="op-feature-tabs"><button v-for="opportunity in ['Inline','Offline']" :key="opportunity" type="button" :class="{active:qcOpportunity===opportunity}" @click="setQcFilter(opportunity,qcItemCategory)">{{ opportunity }}</button><span></span><button v-for="category in ['Group','Attribute','Variable']" :key="category" type="button" :class="{active:qcItemCategory===category}" @click="setQcFilter(qcOpportunity,category)">{{ category }}</button></div>
        <div v-if="feature === 'error'" class="op-error-actions"><button class="op-mini-button" type="button" @click="toggleAll(true)">Select All</button><button class="op-mini-button" type="button" @click="toggleAll(false)">UnSelect All</button><label><input v-model="errorOnlySelected" type="checkbox" />僅顯示已選取錯誤</label></div>
        <form v-if="fields.length" class="op-feature-add" @submit.prevent="addRow">
          <label v-for="field in fields" :key="field.key">{{ field.label }}
            <select v-if="field.type==='select'" v-model="draft[field.key]"><option v-for="option in field.options" :key="option" :value="option">{{ option }}</option></select>
            <input v-else v-model="draft[field.key]" :type="field.type==='number'?'number':'text'" :min="field.type==='number'?0:undefined" />
          </label>
          <button class="op-mini-button" type="submit">新增</button><button class="op-mini-button" type="button" :disabled="!selectedIds.length" @click="removeSelected">刪除選取</button>
        </form>
        <div class="op-feature-grid"><table><thead><tr><th>選取</th><th v-for="column in columns" :key="column">{{ headings[column] ?? column }}</th></tr></thead><tbody><tr v-for="row in visibleRows" :key="String(row.id)"><td><input v-model="selectedIds" type="checkbox" :value="String(row.id)" /></td><td v-for="column in columns" :key="column"><label v-if="column==='enabled'" class="op-cell-check"><input v-model="row[column]" type="checkbox" @change="persist" />{{ displayCell(row[column]) }}</label><input v-else v-model="row[column]" :type="column==='sequence'?'number':'text'" @change="persist" /></td></tr><tr v-if="!visibleRows.length"><td :colspan="columns.length+1">尚未設定資料</td></tr></tbody></table></div>
      </template>
      <p v-if="notice" class="op-feature-notice" role="status">{{ notice }}</p>
      <footer><button class="op-mini-button primary" type="button" @click="confirm">確定</button><button class="op-mini-button" type="button" @click="emit('close')">關閉</button></footer>
    </section>
  </div>
</template>

<style scoped>
.op-feature-backdrop{position:fixed;inset:0;z-index:1100;display:grid;place-items:center;padding:18px;background:#17161166}.op-feature-dialog{width:min(980px,96vw);max-height:min(88vh,900px);display:flex;flex-direction:column;gap:12px;padding:16px;background:#fff;border:1px solid var(--line);box-shadow:0 14px 45px #0003;color:var(--ink);font:12px 'Segoe UI','Microsoft JhengHei',sans-serif}.op-feature-dialog>header{display:flex;justify-content:space-between;align-items:start;border-bottom:1px solid var(--line);padding-bottom:10px}.op-feature-dialog h2{margin:4px 0;font-size:19px}.op-feature-dialog small,.op-feature-note,.op-copy-form p{color:var(--muted)}.op-feature-dialog>header button{width:30px;height:30px;border:1px solid var(--line);background:#faf9f5;font-size:20px}.op-feature-note{margin:0;padding:7px 9px;background:#fbf7e9}.op-feature-dialog label{display:flex;align-items:center;gap:6px}.op-resource-switch,.op-error-actions{display:flex;align-items:center;gap:16px;flex-wrap:wrap}.op-resource-columns{display:grid;grid-template-columns:1fr 54px 1fr;gap:12px;min-height:250px}.op-resource-columns fieldset,.op-copy-form fieldset{min-width:0;border:1px solid var(--line);padding:10px}.op-resource-columns select{width:100%;height:100%;border:1px solid var(--line);background:#fff9df}.op-resource-columns option{padding:5px}.op-resource-transfer{display:flex;flex-direction:column;justify-content:center;gap:8px}.op-resource-item{margin:7px 0}.op-copy-form{display:grid;gap:12px}.op-copy-form>label{display:grid;gap:5px;max-width:320px}.op-copy-form fieldset{display:flex;gap:14px;flex-wrap:wrap}.op-feature-add{display:flex;align-items:end;gap:8px;flex-wrap:wrap}.op-feature-add label{display:grid;align-items:start;gap:4px}.op-feature-add input,.op-feature-add select,.op-copy-form input,.op-import input[type=file],.op-feature-grid input:not([type=checkbox]){min-width:120px;height:28px;padding:4px 7px;border:1px solid #c9c4b7;background:#fff9df;color:var(--ink);font:inherit}.op-feature-grid{overflow:auto;max-height:45vh;border:1px solid var(--line)}.op-feature-grid table{width:100%;border-collapse:collapse;white-space:nowrap}.op-feature-grid th,.op-feature-grid td{padding:6px 8px;border:1px solid #d8d3c8;text-align:left}.op-feature-grid th{background:#eee9dc}.op-feature-grid td>input:not([type=checkbox]){width:100%}.op-cell-check{justify-content:center}.op-mini-button{min-height:30px;padding:5px 11px;border:1px solid #c9c4b7;border-radius:3px;background:#fff;color:var(--ink);font:inherit;cursor:pointer}.op-mini-button:hover:not(:disabled){background:#fff7da;border-color:#ad9600}.op-mini-button:disabled{opacity:.5;cursor:not-allowed}.op-mini-button.primary{background:#3b3930;color:#fff}.op-feature-notice{margin:0;color:#645400}.op-feature-dialog>footer{display:flex;justify-content:flex-end;gap:8px;border-top:1px solid var(--line);padding-top:10px}.op-import{display:grid;gap:9px}@media(max-width:680px){.op-feature-backdrop{padding:6px}.op-feature-dialog{max-height:95vh;padding:10px}.op-resource-columns{grid-template-columns:1fr}.op-resource-transfer{flex-direction:row}.op-copy-form fieldset{display:grid;grid-template-columns:1fr 1fr}}
</style>
