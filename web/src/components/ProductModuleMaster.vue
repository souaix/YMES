<script setup lang="ts">
import { computed, ref } from 'vue'
import ProductModuleProcesses from './ProductModuleProcesses.vue'
import ProductProcessExpansion from './ProductProcessExpansion.vue'
import ProductPropertyMaster from './ProductPropertyMaster.vue'
import ProductParameterSetup from './ProductParameterSetup.vue'
import ProductBusinessRuleSetup from './ProductBusinessRuleSetup.vue'
import ProductOpDescriptionSetup from './ProductOpDescriptionSetup.vue'
import ProductRecipeGroupSetup from './ProductRecipeGroupSetup.vue'
import ProductResourceSetup from './ProductResourceSetup.vue'
import ProductSubOpSetup from './ProductSubOpSetup.vue'
import ProductQCItemSetup from './ProductQCItemSetup.vue'
import ProductDefaultSetup from './ProductDefaultSetup.vue'
import ProductModuleImport from './ProductModuleImport.vue'


type Row = Record<string, any>
type State = -1 | 0 | 1 | 2
type Mode = 'add' | 'edit' | 'copy' | 'version' | ''
type Process = InstanceType<typeof ProductModuleProcesses>['$props']['rows'][number]
type ParameterData = InstanceType<typeof ProductParameterSetup>['$props']['parameters']
type BusinessRuleData = InstanceType<typeof ProductBusinessRuleSetup>['$props']['businessRules']
type RecipeRow = NonNullable<InstanceType<typeof ProductRecipeGroupSetup>['$props']['recipeGroups']>[number]
type ResourceRow = NonNullable<InstanceType<typeof ProductResourceSetup>['$props']['opAreas']>[number]
type ModuleData = {
  moduleNo: string; moduleVersion: string; moduleName: string; moduleType: string; psNo: string; description: string
}
type Detail = { processes?: Process[]; properties?: Row[]; parameters?: ParameterData; rules?: BusinessRuleData; descriptions?: Row[]; recipes?: RecipeRow[]; areas?: ResourceRow[]; subOps?: Row[]; qcItems?: Row[] }
type ModuleRow = ModuleData & { issueState: State; curVersion: boolean; creator: string; createDate: string; reviser?: string; reviseDate?: string; dataStamp: number; detail: Detail }

const props = defineProps<{ currentUserNo: string; developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.prd103-module-basis.v1'
const types = ['DEV-MODULE-ASSY', 'DEV-MODULE-TEST']
const segments = ['DEV-PS-10', 'DEV-PS-20', 'DEV-PS-30']
const operations: Record<string, { opNo: string; opName: string }[]> = {
  'DEV-PS-10': [{ opNo: 'DEV-OP-10', opName: '點膠作業' }, { opNo: 'DEV-OP-20', opName: '組裝作業' }],
  'DEV-PS-20': [{ opNo: 'DEV-OP-30', opName: '終檢作業' }],
  'DEV-PS-30': [{ opNo: 'DEV-OP-30', opName: '試產檢驗' }],
}
const seed: ModuleRow[] = [
  { moduleNo: 'DEV-MODULE-10', moduleVersion: '01', moduleName: 'DEV 點膠模組', moduleType: types[0], psNo: segments[0], description: '', issueState: 0, curVersion: false, creator: 'DEV001', createDate: '2026-11-19T08:00:00', dataStamp: 0,
    detail: { processes: [{ psNo: segments[0], processNo: 'DEV-PROC-01', processVersion: '*', defaultProcess: 1 }], areas: operations[segments[0]].map(op => ({ ...op, areaNo: 'DEV-AREA-01', defaultArea: 1 })) } },
  { moduleNo: 'DEV-MODULE-20', moduleVersion: '01', moduleName: 'DEV 組裝模組', moduleType: types[0], psNo: segments[1], description: '', issueState: 2, curVersion: true, creator: 'DEV001', createDate: '2026-11-19T08:00:00', dataStamp: 1,
    detail: { processes: [{ psNo: segments[1], processNo: 'DEV-PROC-20', processVersion: '02', defaultProcess: 1 }], areas: [{ opNo: 'DEV-OP-30', areaNo: 'DEV-AREA-03', defaultArea: 1 }] } },
]

function load(): ModuleRow[] {
  if (!props.developmentPreview) return []
  try {
    const stored = localStorage.getItem(storageKey)
    const parsed = stored ? JSON.parse(stored) : null
    if (Array.isArray(parsed)) return parsed
  } catch { /* Use fixtures when no DEV data can be read. */ }
  return seed
}

const rows = ref(load())
const state = ref<State>(0)
const states = [{ key: 0, label: '未核准' }, { key: 1, label: '簽核中' }, { key: 2, label: '已核准' }, { key: -1, label: '不使用' }] as const
const keyword = ref('')
const query = ref('')
const currentOnly = ref(false)
const selected = ref('')
const marked = ref<string[]>([])
const keyOf = (row: Pick<ModuleData, 'moduleNo' | 'moduleVersion'>) => JSON.stringify([row.moduleNo, row.moduleVersion])
const visible = computed(() => rows.value.filter(row => row.issueState === state.value && (!currentOnly.value || row.curVersion) && row.moduleNo.includes(query.value)))
const active = computed(() => visible.value.find(row => keyOf(row) === selected.value) ?? visible.value[0])
const canEdit = computed(() => active.value?.issueState === 0)
const detail = computed(() => active.value?.detail ?? {})
const context = computed(() => ({ productNo: active.value?.moduleNo, productVersion: active.value?.moduleVersion, productName: active.value?.moduleName, issueState: active.value?.issueState }))
const currentUser = computed(() => props.currentUserNo || 'DEV001')
const mode = ref<Mode>('')
const form = ref<ModuleData>({ moduleNo: '', moduleVersion: '01', moduleName: '', moduleType: '', psNo: '', description: '' })
const target = ref<ModuleRow>()
const sourceKey = ref('')
const child = ref('')
const processIndex = ref(0)
const process = computed(() => detail.value.processes?.[processIndex.value])
const operationOptions = computed(() => detail.value.processes?.length ? operations[active.value?.psNo ?? ''] ?? [] : [])
const error = ref('')
const notice = ref('')
const childActions = [{ key: 'property', label: 'Property' }, { key: 'parameter', label: 'Parameter', needsProcess: true },
  { key: 'rules', label: 'Business Rule', needsProcess: true }, { key: 'description', label: 'OPDescription', needsProcess: true },
  { key: 'recipe', label: 'Recipe Group' }, { key: 'area', label: 'Resource', needsProcess: true }, { key: 'default', label: 'Default', needsProcess: true },
  { key: 'qc', label: 'QC Item' }, { key: 'subop', label: 'SubOP' }]

function saveRows(next: ModuleRow[]): boolean {
  if (!props.developmentPreview) return false
  try { localStorage.setItem(storageKey, JSON.stringify(next)) }
  catch { error.value = '無法儲存本機 DEV 設定。'; return false }
  rows.value = next
  error.value = ''
  return true
}

function select(row: ModuleRow): void { selected.value = keyOf(row); processIndex.value = 0 }

function open(nextMode: Mode): void {
  const original = nextMode === 'add' ? undefined : active.value
  const version = original?.moduleVersion ?? '01'
  const nextVersion = /^\d+$/.test(version) ? String(Number(version) + 1).padStart(2, '0') : ''
  form.value = { moduleNo: nextMode === 'copy' ? '' : original?.moduleNo ?? '', moduleVersion: nextMode === 'version' ? nextVersion : nextMode === 'copy' ? '01' : version,
    moduleName: original?.moduleName ?? '', moduleType: original?.moduleType ?? '', psNo: original?.psNo ?? '', description: original?.description ?? '' }
  target.value = original
  sourceKey.value = original && (nextMode === 'copy' || nextMode === 'version') ? keyOf(original) : ''
  mode.value = nextMode
  error.value = ''
}

function saveForm(close: boolean): void {
  const record = { ...form.value, moduleNo: form.value.moduleNo.trim().toUpperCase(), moduleVersion: form.value.moduleVersion.trim().toUpperCase(), moduleName: form.value.moduleName.trim() }
  const editing = mode.value === 'edit'
  const original = target.value
  const source = rows.value.find(row => keyOf(row) === sourceKey.value)
  if (!record.moduleNo || !record.moduleVersion || record.moduleVersion.length > 3 || !record.moduleName || !record.moduleType || !record.psNo) { error.value = '請完整輸入模組資料及有效版次。'; return }
  if (editing && (!original || original.issueState !== 0 || keyOf(record) !== keyOf(original))) return
  if (!editing && rows.value.some(row => row.moduleNo === record.moduleNo && (mode.value !== 'version' || row.moduleVersion === record.moduleVersion))) { error.value = '模組或版次已存在。'; return }
  if ((source ?? original)?.psNo && (source ?? original)?.psNo !== record.psNo) { error.value = '複製來源或既有流程須屬於相同生產區段。'; return }
  if (editing && source && !window.confirm('覆寫此模組版次的流程及明細？')) return
  const now = new Date().toISOString()
  const nextDetail: Detail = source ? JSON.parse(JSON.stringify(source.detail)) : editing ? original?.detail ?? {} : {}
  if (source) {
    for (const list of [nextDetail.properties, nextDetail.parameters?.ops, nextDetail.descriptions, nextDetail.recipes, nextDetail.areas, nextDetail.subOps, nextDetail.qcItems]) {
      for (const row of list ?? []) { row.productNo = record.moduleNo; row.productVersion = record.moduleVersion }
    }
  }
  const saved: ModuleRow = editing && original
    ? { ...original, ...record, detail: nextDetail, dataStamp: original.dataStamp + 1, reviser: currentUser.value, reviseDate: now }
    : { ...record, detail: nextDetail, issueState: 0, curVersion: false, creator: currentUser.value, createDate: now, dataStamp: 0 }
  if (!saveRows(editing ? rows.value.map(row => keyOf(row) === keyOf(saved) ? saved : row) : [...rows.value, saved])) return
  state.value = 0; currentOnly.value = false; keyword.value = ''; query.value = ''; marked.value = []
  select(saved); target.value = saved; form.value = record; sourceKey.value = ''; mode.value = close ? '' : 'edit'
  notice.value = '製程模組已儲存。'
}

function changeState(action: 'approve' | 'delete'): void {
  if (!active.value || state.value === 1 || (action === 'approve' && !canEdit.value)) return
  const targets = new Set([...marked.value, keyOf(active.value)])
  const chosen = visible.value.filter(row => targets.has(keyOf(row)))
  if (action === 'approve') {
    const invalid = chosen.find(row => !row.detail.processes?.length || (operations[row.psNo] ?? [])
      .some(op => !(row.detail.areas ?? []).some(area => area.opNo === op.opNo && area.areaNo)))
    if (invalid) { error.value = `${invalid.moduleNo} 尚未設定流程或作業區域。`; return }
    if (new Set(chosen.map(row => row.moduleNo)).size !== chosen.length) { error.value = '同一模組請逐版核准。'; return }
  }
  if (!window.confirm(action === 'approve' ? '確定核准選取的模組版次？' : '確定刪除或停用選取的模組版次？')) return
  const now = new Date().toISOString()
  const approved = new Set(chosen.map(row => row.moduleNo))
  const next = rows.value.flatMap(row => {
    if (!targets.has(keyOf(row)) || row.issueState !== state.value) return [action === 'approve' && approved.has(row.moduleNo) ? { ...row, curVersion: false } : row]
    if (action === 'delete' && row.issueState !== 2) return []
    const issueState: State = action === 'approve' ? 2 : -1
    return [{ ...row, issueState, curVersion: action === 'approve', dataStamp: row.dataStamp + 1, reviser: currentUser.value, reviseDate: now }]
  })
  if (saveRows(next)) { marked.value = []; notice.value = action === 'approve' ? '模組版次已核准。' : '模組版次已刪除或停用。' }
}

function saveDetail(patch: Detail): boolean {
  const key = active.value && keyOf(active.value)
  if (!key || !canEdit.value) return false
  const next = rows.value.map(row => keyOf(row) === key ? { ...row, detail: { ...row.detail, ...JSON.parse(JSON.stringify(patch)) } } : row)
  if (!saveRows(next)) return false
  notice.value = '模組明細已儲存。'
  return true
}

const saveQC: InstanceType<typeof ProductQCItemSetup>['$props']['saveAssignments'] = (opNo, list) => saveDetail({ qcItems: [...(detail.value.qcItems ?? []).filter(row => row.opNo !== opNo), ...list] })

function importModules(entries: ModuleData[]): string[] {
  const known = new Set(rows.value.map(keyOf))
  const now = new Date().toISOString()
  const additions: ModuleRow[] = []
  const results = entries.map(record => {
    if (known.has(keyOf(record))) return '模組版次已存在。'
    if (!types.includes(record.moduleType) || !segments.includes(record.psNo)) return '類別或區段不在 DEV 清單。'
    if (record.moduleNo.length > 45 || record.moduleVersion.length > 3 || record.moduleName.length > 100 || record.description.length > 255) return '主檔欄位長度超出限制。'
    known.add(keyOf(record))
    additions.push({ ...record, issueState: 0, curVersion: false, creator: currentUser.value, createDate: now, dataStamp: 0, detail: {} })
    return ''
  })
  if (additions.length) {
    if (!saveRows([...rows.value, ...additions])) return results.map(message => message || '無法儲存 DEV 主檔。')
    state.value = 0; currentOnly.value = false; keyword.value = ''; query.value = ''; marked.value = []
    select(additions[0]); notice.value = `已匯入 ${additions.length} 筆 DEV 模組。`
  }
  return results.map(message => message || '已匯入 DEV 主檔。')
}

const applyDefaults: NonNullable<InstanceType<typeof ProductDefaultSetup>['$props']['onApply']> = plan => {
  const row = active.value
  const route = detail.value.processes?.find(item => item.defaultProcess) ?? detail.value.processes?.[0]
  if (!row || !route) return
  const catalog = operations[row.psNo] ?? []
  const targets = plan.defaultOP === 2 && route.processNo === 'DEV-PROC-02' ? catalog.slice(0, 1) : catalog
  const id = { productNo: row.moduleNo, productVersion: row.moduleVersion }
  const patch: Detail = {}
  if (plan.updateParameter) patch.parameters = { ops: targets.map((op, index) => ({ ...id, ...op, psNo: row.psNo, opOrder: (index + 1) * 10, serialNo: `${row.moduleNo}:${row.moduleVersion}:${op.opNo}` })), attributes: [], rules: [], equipments: [] }
  if (plan.updateBusinessRule) patch.rules = { operations: targets.map(op => ({ ...op, processNo: route.processNo, processVersion: route.processVersion, nodes: [], links: [] })) }
  if (plan.updateDescription) patch.descriptions = targets.map(op => ({ ...id, ...op, description: op.opName }))
  if (plan.updateOPResource) patch.areas = targets.map(op => {
    const code = String(Number(op.opNo.split('-').pop()) / 10).padStart(2, '0')
    return { ...id, ...op, areaNo: 'DEV-AREA-' + code, defaultArea: 1 }
  })
  if (plan.updateQCItem) patch.qcItems = targets.map(op => ({ ...id, opNo: op.opNo, qcItemNo: 'DEV-QC-DEFAULT', qcItemName: 'DEV 外觀檢查', qcItemType: 0, executeOrder: 1, inLine: 0 }))
  if (plan.updateSubOP) patch.subOps = targets.map(op => ({ ...id, opNo: op.opNo, subOpSequence: 1, subOpNo: `${op.opNo}-01`, subOpName: `${op.opName}準備` }))
  if (saveDetail(patch)) notice.value = '模組 DEV 預設設定已帶入。'
}
</script>

<template>
  <main class="module-master">
    <header><h1>製程模組資料</h1><button @click="emit('close')">關閉</button></header>
    <p v-if="!developmentPreview">製程模組資料等待正式 API 接入。</p>
    <template v-else>
      <form class="toolbar" @submit.prevent="query = keyword.trim().toUpperCase(); marked = []; processIndex = 0">
        <label v-for="item in states" :key="item.key"><input v-model="state" type="radio" :value="item.key" @change="marked = []; processIndex = 0" />{{ item.label }}</label>
        <label><input v-model="currentOnly" type="checkbox" @change="marked = []; processIndex = 0" />目前版</label>
        <input v-model="keyword" aria-label="製程模組編號" placeholder="模組編號" /><button>查詢</button>
      </form>
      <div class="toolbar"><button @click="open('add')">ADD</button><button :disabled="!canEdit" @click="open('edit')">EDIT</button><button :disabled="!active || state === 1" @click="changeState('delete')">DEL</button>
        <button :disabled="!active" @click="open('copy')">Copy</button><button :disabled="active?.issueState !== 2 || !active.curVersion" @click="open('version')">Version</button><button :disabled="!canEdit" @click="changeState('approve')">Approve</button>
        <button v-for="action in childActions" :key="action.key" :disabled="!active || (action.needsProcess && !detail.processes?.length) || (action.key === 'qc' && !canEdit)" @click="child = action.key">{{ action.label }}</button>
        <button @click="child = 'import'">Import</button>
      </div>
      <div class="grid"><table><thead><tr><th>選取</th><th>ModuleNo</th><th>ModuleVersion</th><th>CurVersion</th><th>ModuleName</th><th>ModuleType</th><th>PSNo</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
        <tbody><tr v-for="row in visible" :key="keyOf(row)" :class="{ active: active && keyOf(active) === keyOf(row) }" @click="select(row)">
          <td><input v-model="marked" type="checkbox" :value="keyOf(row)" :aria-label="`選取 ${row.moduleNo} ${row.moduleVersion}`" /></td><td><button @click="select(row)">{{ row.moduleNo }}</button></td><td>{{ row.moduleVersion }}</td><td>{{ row.curVersion ? 'Yes' : 'No' }}</td><td>{{ row.moduleName }}</td><td>{{ row.moduleType }}</td><td>{{ row.psNo }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ row.createDate?.replace('T', ' ').slice(0, 19) }}</td><td>{{ row.reviser }}</td><td>{{ row.reviseDate?.replace('T', ' ').slice(0, 19) }}</td>
        </tr><tr v-if="!visible.length"><td colspan="12">沒有符合的製程模組。</td></tr></tbody></table></div>
      <section v-if="active"><header><h2>Product Process</h2><div><button @click="child = 'process'">Set …</button> <button :disabled="!process" @click="child = 'expand'">Expand All</button></div></header>
        <div class="grid"><table><thead><tr><th>ProcessNo</th><th>ProcessVersion</th><th>DefaultProcess</th></tr></thead><tbody>
          <tr v-for="(assignment, index) in detail.processes ?? []" :key="assignment.processNo" :class="{ active: processIndex === index }"><td><button @click="processIndex = index">{{ assignment.processNo }}</button></td><td>{{ assignment.processVersion }}</td><td>{{ assignment.defaultProcess ? 'Yes' : 'No' }}</td></tr>
          <tr v-if="!detail.processes?.length"><td colspan="3">沒有設定流程。</td></tr></tbody></table></div></section>
      <p v-if="error && !mode" role="alert" class="error">{{ error }}</p><p v-if="notice" role="status">{{ notice }}</p>
    </template>
    <div v-if="mode" class="overlay"><form class="editor" @submit.prevent="saveForm(true)">
      <h2>製程模組 {{ mode.toUpperCase() }}</h2>
      <label>ModuleNo<input v-model="form.moduleNo" :readonly="mode === 'edit' || mode === 'version'" required maxlength="45" /></label>
      <label>ModuleVersion<input v-model="form.moduleVersion" :readonly="mode === 'edit'" required maxlength="3" /></label>
      <label>ModuleName<input v-model="form.moduleName" required maxlength="100" /></label>
      <label>ModuleType<select v-model="form.moduleType" :disabled="mode === 'version'" required><option value="">請選擇</option><option v-for="type in types" :key="type">{{ type }}</option></select></label>
      <label>PSNo<select v-model="form.psNo" required><option value="">請選擇</option><option v-for="segment in segments" :key="segment">{{ segment }}</option></select></label>
      <label>Description<textarea v-model="form.description" maxlength="255" /></label>
      <label>Detail Source<select v-model="sourceKey"><option value="">None</option><option v-for="row in rows.filter(row => mode !== 'edit' || keyOf(row) !== keyOf(form))" :key="keyOf(row)" :value="keyOf(row)">{{ row.moduleNo }} / {{ row.moduleVersion }}</option></select></label>
      <p v-if="error" role="alert" class="error">{{ error }}</p><footer><button>Confirm</button><button type="button" @click="saveForm(false)">Apply</button><button type="button" @click="mode = ''; error = ''">Close</button></footer>
    </form></div>
    <div v-if="child === 'import'" class="overlay"><ProductModuleImport :import-modules="importModules" @close="child = ''" /></div>
    <div v-else-if="child && active" class="overlay">
      <ProductModuleProcesses v-if="child === 'process'" :ps-no="active.psNo" :rows="detail.processes ?? []" :can-edit="canEdit" :save="value => saveDetail({ processes: value })" @close="child = ''" />
      <ProductProcessExpansion v-else-if="child === 'expand' && process" :product-no="active.moduleNo" :product-version="active.moduleVersion" :process-no="process.processNo" :process-version="process.processVersion" :ps-no="active.psNo" mo-type-no="*" :development-preview="developmentPreview" module-mode @close="child = ''" />
      <ProductPropertyMaster v-else-if="child === 'property'" :product="context" :properties="detail.properties ?? []" :can-edit="canEdit" :development-preview="developmentPreview" @save="saveDetail({ properties: $event })" @close="child = ''" />
      <ProductParameterSetup v-else-if="child === 'parameter'" :product="context" :processes="detail.processes ?? []" :parameters="detail.parameters" :can-edit="canEdit" :development-preview="developmentPreview" @save="saveDetail({ parameters: $event })" @close="child = ''" />
      <ProductBusinessRuleSetup v-else-if="child === 'rules'" :product="context" :processes="detail.processes ?? []" :business-rules="detail.rules" :can-edit="canEdit" :development-preview="developmentPreview" @save="saveDetail({ rules: $event })" @close="child = ''" />
      <ProductOpDescriptionSetup v-else-if="child === 'description'" :product="context" :processes="detail.processes ?? []" :op-descriptions="detail.descriptions ?? null" :can-edit="canEdit" :development-preview="developmentPreview" @save="saveDetail({ descriptions: $event })" @close="child = ''" />
      <ProductRecipeGroupSetup v-else-if="child === 'recipe'" :product="context" :recipe-groups="detail.recipes ?? null" :development-preview="canEdit" @save="saveDetail({ recipes: $event })" @close="child = ''" />
      <ProductResourceSetup v-else-if="child === 'area'" :product="context" :processes="detail.processes ?? []" :op-areas="detail.areas ?? null" :can-edit="canEdit" :development-preview="developmentPreview" @save="saveDetail({ areas: $event })" @close="child = ''" />
      <ProductSubOpSetup v-else-if="child === 'subop'" :product="context" :sub-ops="detail.subOps ?? null" :operation-options="operationOptions" :development-preview="canEdit" :current-user-no="currentUser" @save="saveDetail({ subOps: $event })" @close="child = ''" />
      <ProductDefaultSetup v-else-if="child === 'default'" :product="context" :has-product-process="Boolean(detail.processes?.length)" :development-preview="developmentPreview" :qc-item-auto-checked="false" :aps-enabled="false" @apply="applyDefaults" @close="child = ''" />
      <ProductQCItemSetup v-else-if="child === 'qc'" :product-no="active.moduleNo" :product-version="active.moduleVersion" :product-name="active.moduleName" :saved-rows="detail.qcItems ?? []" :development-preview="canEdit" :current-user="currentUser" :save-assignments="saveQC" @close="child = ''" />
    </div>
  </main>
</template>

<style scoped>
.module-master{display:flex;flex-direction:column;gap:8px;min-width:0;padding:10px;background:#fff;color:#292820;font:12px "Segoe UI","Microsoft JhengHei",sans-serif}header,.toolbar,footer{display:flex;align-items:center;gap:8px}header{justify-content:space-between}.toolbar{flex-wrap:wrap;padding:6px;background:#f5f3eb;border:1px solid #dedbd0}h1,h2,p{margin:0}h1{font-size:17px}h2{font-size:14px}
.grid{overflow:auto;border:1px solid #d9d6cb}table{width:100%;border-collapse:collapse;text-align:left;white-space:nowrap}th,td{padding:6px;border:1px solid #e3e0d6}thead{background:#f5f3eb}.active{background:#fff2c6}button,input,select,textarea{font:inherit}button{padding:4px 10px;border:1px solid #aaa797;border-radius:3px;background:#f8f7f2;color:inherit}button:disabled{opacity:.5}.error{color:#a32020}
.overlay{position:fixed;inset:0;z-index:91;display:grid;place-items:center;padding:12px;background:#0005}.editor{display:grid;gap:12px;width:min(560px,95vw);max-height:90vh;overflow:auto;padding:16px;background:#fff;box-shadow:0 8px 30px #0004}.editor label{display:grid;gap:4px}.editor input,.editor select,.editor textarea{padding:5px;border:1px solid #aaa797;background:#fff5d6}.editor input:read-only{background:#f4f3ec}footer{justify-content:flex-end}
</style>
