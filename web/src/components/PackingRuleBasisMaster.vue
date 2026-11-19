<script setup lang="ts">
import { computed, ref } from 'vue'
import ProductPackingSerialRules from './ProductPackingSerialRules.vue'
import ProductPackingLabelRules from './ProductPackingLabelRules.vue'
import { loadDevelopmentPackingRules, packingRuleStorageKey, type Level, type PackingRuleBasis } from './packingRuleDevelopment'

type Mode = 'add' | 'edit' | 'copy' | 'version' | ''
const props = defineProps<{ developmentPreview: boolean; currentUserNo: string }>()
const emit = defineEmits<{ close: [] }>()
const rows = ref(props.developmentPreview ? loadDevelopmentPackingRules() : [])
const state = ref<PackingRuleBasis['issueState']>(0)
const states = [{ key: 0, label: '未核准' }, { key: 1, label: '簽核中' }, { key: 2, label: '已核准' }, { key: -1, label: '不使用' }] as const
const keyword = ref('')
const query = ref('')
const keyOf = (row: Pick<PackingRuleBasis, 'no' | 'version'>) => JSON.stringify([row.no, row.version])
const activeKey = ref('')
const visible = computed(() => rows.value.filter(row => row.issueState === state.value && row.no.includes(query.value)))
const active = computed(() => visible.value.find(row => keyOf(row) === activeKey.value) ?? visible.value[0])
const editable = computed(() => active.value?.issueState === 0)
const marked = ref<string[]>([])
const mode = ref<Mode>('')
const target = ref<PackingRuleBasis>()
const draft = ref({ no: '', version: '01', inline: true, levels: [] as Level[] })
const tab = ref<'serial' | 'label' | 'accessory' | ''>('')
const accessory = ref({ barcode: '', description: '' })
const accessoryKey = ref('')
const serialTypes = computed(() => (active.value?.levels ?? []).map((level, index) => ({ key: 11 + index, name: level.name })))
const labelTypes = computed(() => (active.value?.levels ?? []).flatMap((level, index) => [1, 2].map(type => ({ key: `L${index + 1}_T${type}`, label: `${level.name} Type${type}` }))))
const error = ref('')
const notice = ref('')
const user = computed(() => props.currentUserNo || 'DEV001')
const blankLevel = (): Level => ({ name: '', count: '', quantity: '', unit: '', sop: '' })

function save(next: PackingRuleBasis[]): boolean {
  if (!props.developmentPreview) return false
  try { localStorage.setItem(packingRuleStorageKey, JSON.stringify(next)) }
  catch { error.value = '無法儲存本機 DEV 規則。'; return false }
  rows.value = next; error.value = ''; return true
}

function open(next: Mode): void {
  const row = next === 'add' ? undefined : active.value
  draft.value = { no: next === 'copy' ? '' : row?.no ?? '', version: next === 'version' ? '' : next === 'copy' ? '01' : row?.version ?? '01',
    inline: row?.inline ?? true, levels: row ? row.levels.map(level => ({ ...level })) : [blankLevel()] }
  target.value = row; mode.value = next; error.value = ''
}

function validateLevels(levels: Level[]): string {
  if (new Set(levels.map(level => level.name.toUpperCase())).size !== levels.length) return '包裝層名稱不可重複。'
  for (const [index, level] of levels.entries()) {
    if (!level.name || !level.unit || (!level.count && !level.quantity)) return `第 ${index + 1} 層需有名稱、單位及箱數或數量。`
    if (level.count && !/^[1-9]\d{0,2}$/.test(level.count)) return `第 ${index + 1} 層箱數須為 1–999 的整數。`
    const validQty = index === 0 ? /^\d{1,8}(\.\d{1,4})?$/ : /^\d{1,8}$/
    if (level.quantity && (!validQty.test(level.quantity) || Number(level.quantity) <= 0)) return `第 ${index + 1} 層數量不符範圍。`
  }
  return ''
}

function saveRule(close: boolean): void {
  const record = { ...draft.value, no: draft.value.no.trim().toUpperCase(), version: draft.value.version.trim().toUpperCase(), levels: draft.value.levels.map(level => ({ name: level.name.trim(), count: level.count.trim(), quantity: level.quantity.trim(), unit: level.unit.trim(), sop: level.sop.trim() })) }
  if (!record.no || !record.version) { error.value = '規則編號與版次必填。'; return }
  error.value = validateLevels(record.levels)
  if (error.value) return
  const original = target.value
  const editing = mode.value === 'edit'
  if (editing && (!original || original.issueState !== 0 || keyOf(record) !== keyOf(original))) return
  if (!editing && rows.value.some(row => keyOf(row) === keyOf(record))) { error.value = '包裝規則版次已存在。'; return }
  const children = original && mode.value !== 'add' ? JSON.parse(JSON.stringify({ serials: original.serials, labels: original.labels, accessories: original.accessories })) as Pick<PackingRuleBasis, 'serials' | 'labels' | 'accessories'> : { serials: [], labels: [], accessories: [] }
  if (children.serials.some(row => row.serialType - 10 > record.levels.length) || children.labels.some(row => Number(/^L(\d)_/.exec(row.labelType)?.[1]) > record.levels.length)) { error.value = '請先移除要刪除層級的序號與標籤規則。'; return }
  const now = new Date().toISOString()
  const row: PackingRuleBasis = editing && original ? { ...original, ...record, ...children, reviser: user.value, reviseDate: now, dataStamp: original.dataStamp + 1 }
    : { ...record, ...children, issueState: 0, curVersion: false, creator: user.value, createDate: now, dataStamp: 0 }
  if (!save(editing ? rows.value.map(item => keyOf(item) === keyOf(row) ? row : item) : [...rows.value, row])) return
  state.value = 0; query.value = ''; keyword.value = ''; marked.value = []; activeKey.value = keyOf(row); target.value = row; draft.value = record
  notice.value = '包裝規則已儲存。'; mode.value = close ? '' : 'edit'
}

function changeState(action: 'approve' | 'delete'): void {
  if (!active.value || state.value === 1 || (action === 'approve' && !editable.value)) return
  const keys = new Set([...marked.value, keyOf(active.value)])
  const chosen = visible.value.filter(row => keys.has(keyOf(row)))
  const numbers = new Set(chosen.map(row => row.no))
  if (action === 'approve' && numbers.size !== chosen.length) { error.value = '同一規則請逐版核准。'; return }
  if (!window.confirm(action === 'approve' ? '確定核准選取的包裝規則？' : '確定刪除或停用選取的包裝規則？')) return
  const now = new Date().toISOString()
  const next = rows.value.flatMap<PackingRuleBasis>(row => {
    if (!keys.has(keyOf(row)) || row.issueState !== state.value) return [action === 'approve' && numbers.has(row.no) ? { ...row, curVersion: false } : row]
    if (action === 'delete' && row.issueState !== 2) return []
    return [{ ...row, curVersion: action === 'approve', issueState: action === 'approve' ? 2 : -1, reviser: user.value, reviseDate: now, dataStamp: row.dataStamp + 1 }]
  })
  if (save(next)) { marked.value = []; notice.value = action === 'approve' ? '規則已核准。' : '規則已刪除或停用。' }
}

function saveDetail(detail: Partial<Pick<PackingRuleBasis, 'serials' | 'labels' | 'accessories'>>): boolean {
  const key = active.value && keyOf(active.value)
  if (!key || !editable.value) return false
  return save(rows.value.map(row => keyOf(row) === key ? { ...row, ...JSON.parse(JSON.stringify(detail)) } : row))
}

function editAccessory(row?: PackingRuleBasis['accessories'][number]): void {
  accessoryKey.value = row?.barcode ?? ''
  accessory.value = row ? { ...row } : { barcode: '', description: '' }
}

function saveAccessory(): void {
  const barcode = accessory.value.barcode.trim().toUpperCase()
  const previous = accessoryKey.value
  const data = active.value?.accessories ?? []
  if (!barcode || (!previous && data.some(row => row.barcode === barcode))) { error.value = '配件條碼必填且不可重複。'; return }
  const saved = { barcode, description: accessory.value.description.trim() }
  if (saveDetail({ accessories: previous ? data.map(row => row.barcode === previous ? saved : row) : [...data, saved] })) editAccessory()
}
</script>

<template>
  <main class="packing-rules">
    <header><h1>包裝規則設定</h1><button @click="emit('close')">關閉</button></header>
    <p v-if="!developmentPreview">包裝規則等待正式 API 接入。</p>
    <template v-else>
      <form class="toolbar" @submit.prevent="query = keyword.trim().toUpperCase(); marked = []"><label v-for="item in states" :key="item.key"><input v-model="state" type="radio" :value="item.key" @change="marked = []" />{{ item.label }}</label><input v-model="keyword" aria-label="包裝規則編號" placeholder="規則編號"/><button>查詢</button></form>
      <div class="toolbar"><button @click="open('add')">ADD</button><button :disabled="!editable" @click="open('edit')">EDIT</button><button :disabled="!active || state === 1" @click="changeState('delete')">DEL</button><button :disabled="!active" @click="open('copy')">Copy</button><button :disabled="!editable" @click="changeState('approve')">Approve</button><button :disabled="active?.issueState !== 2 || !active.curVersion" @click="open('version')">Version</button><button :disabled="!active" @click="tab = 'serial'; error = ''">Detail</button></div>
      <div class="grid"><table><thead><tr><th>選取</th><th>PackingRuleNo</th><th>PackingRuleVersion</th><th>CurVersion</th><th>InlinePacking</th><th>LevelMax</th><th>各層描述</th><th>Creator</th><th>Reviser</th></tr></thead><tbody>
        <tr v-for="row in visible" :key="keyOf(row)" :class="{ selected: active && keyOf(active) === keyOf(row) }" @click="activeKey = keyOf(row)"><td><input v-model="marked" type="checkbox" :value="keyOf(row)" :aria-label="`選取 ${row.no} ${row.version}`" /></td><td><button @click="activeKey = keyOf(row)">{{ row.no }}</button></td><td>{{ row.version }}</td><td>{{ row.curVersion ? 'Yes' : 'No' }}</td><td>{{ row.inline ? 'Yes' : 'No' }}</td><td>{{ row.levels.length }}</td><td>{{ row.levels.map(level => level.name).join(' / ') }}</td><td>{{ row.creator }}</td><td>{{ row.reviser }}</td></tr><tr v-if="!visible.length"><td colspan="9">沒有符合的包裝規則。</td></tr>
      </tbody></table></div><p v-if="error && !mode && !tab" role="alert" class="error">{{ error }}</p><p v-if="notice" role="status">{{ notice }}</p>
    </template>
    <div v-if="mode" class="overlay"><form class="dialog" role="dialog" aria-modal="true" aria-label="包裝規則定義" @submit.prevent="saveRule(true)"><h2>包裝規則 {{ mode.toUpperCase() }}</h2>
      <div class="fields"><label>PackingRuleNo<input v-model="draft.no" :readonly="mode === 'edit' || mode === 'version'" maxlength="50" required /></label><label>PackingRuleVersion<input v-model="draft.version" :readonly="mode === 'edit'" maxlength="5" required /></label><label class="checkbox"><input v-model="draft.inline" type="checkbox" />線上包裝</label></div>
      <fieldset v-for="(level, index) in draft.levels" :key="index" class="fields"><legend>Level {{ index + 1 }}</legend><label>名稱<input v-model="level.name" maxlength="50" /></label><label>箱數<input v-model="level.count" inputmode="numeric" maxlength="3" /></label><label>數量<input v-model="level.quantity" inputmode="decimal" maxlength="13" /></label><label>單位<input v-model="level.unit" maxlength="30" /></label><label>SOPPath<input v-model="level.sop" maxlength="500" /></label></fieldset>
      <div class="toolbar"><button type="button" :disabled="draft.levels.length === 6" @click="draft.levels.push(blankLevel())">新增層級</button><button type="button" :disabled="draft.levels.length === 1" @click="draft.levels.pop()">移除末層</button></div>
      <p v-if="error" role="alert" class="error">{{ error }}</p><footer><button>Confirm</button><button type="button" @click="saveRule(false)">Apply</button><button type="button" @click="mode = ''; error = ''">Close</button></footer>
    </form></div>
    <div v-if="tab && active" class="overlay"><section class="dialog detail" role="dialog" aria-modal="true" aria-label="包裝規則明細">
      <header><h2>{{ active.no }} / {{ active.version }}</h2><button @click="tab = ''; error = ''">關閉</button></header>
      <div class="toolbar"><button @click="tab = 'serial'">序號</button><button @click="tab = 'label'">標籤</button><button @click="tab = 'accessory'; editAccessory()">配件</button></div>
      <ProductPackingSerialRules v-if="tab === 'serial'" :rules="active.serials" :level-types="serialTypes" :can-edit="editable" :current-user="user" :save="value => saveDetail({ serials: value })" />
      <ProductPackingLabelRules v-else-if="tab === 'label'" :rules="active.labels" :level-types="labelTypes" :product-no="active.no" :can-edit="editable" :current-user="user" :save="value => saveDetail({ labels: value })" />
      <template v-else><form v-if="editable" class="toolbar" @submit.prevent="saveAccessory"><label>AccessoryBarcode <input v-model="accessory.barcode" :readonly="!!accessoryKey" maxlength="50" required /></label><label>Description <input v-model="accessory.description" maxlength="255" /></label><button>儲存配件</button><button type="button" @click="editAccessory()">清除</button></form>
        <div class="grid"><table><thead><tr><th>AccessoryBarcode</th><th>Description</th><th v-if="editable">操作</th></tr></thead><tbody><tr v-for="row in active.accessories" :key="row.barcode"><td>{{ row.barcode }}</td><td>{{ row.description }}</td><td v-if="editable"><button @click="editAccessory(row)">EDIT</button><button @click="saveDetail({ accessories: active.accessories.filter(item => item.barcode !== row.barcode) }); editAccessory()">DEL</button></td></tr><tr v-if="!active.accessories.length"><td :colspan="editable ? 3 : 2">沒有配件。</td></tr></tbody></table></div>
      </template><p v-if="error" role="alert" class="error">{{ error }}</p>
    </section></div>
  </main>
</template>

<style scoped>
.packing-rules{display:flex;flex-direction:column;gap:8px;min-width:0;padding:10px;background:#fff;color:#292820;font:12px "Segoe UI","Microsoft JhengHei",sans-serif}header,.toolbar,footer{display:flex;align-items:center;gap:8px}header{justify-content:space-between}.toolbar{flex-wrap:wrap;padding:6px;background:#f5f3eb;border:1px solid #dedbd0}h1,h2,p{margin:0}h1{font-size:17px}h2{font-size:14px}
.grid{overflow:auto;border:1px solid #d9d6cb}table{width:100%;border-collapse:collapse;text-align:left;white-space:nowrap}th,td{padding:6px;border:1px solid #e3e0d6}thead{background:#f5f3eb}.selected{background:#fff2c6}button,input{font:inherit}button{padding:4px 10px;border:1px solid #aaa797;border-radius:3px;background:#f8f7f2;color:inherit}button:disabled{opacity:.5}.error{color:#a32020}
.overlay{position:fixed;inset:0;z-index:91;display:grid;place-items:center;padding:12px;background:#0005}.dialog{display:grid;gap:10px;width:min(900px,95vw);max-height:90vh;overflow:auto;padding:16px;background:#fff;box-shadow:0 8px 30px #0004}.detail{width:min(1080px,95vw)}.fields{display:grid;grid-template-columns:repeat(auto-fit,minmax(130px,1fr));gap:8px}.fields label{display:grid;gap:3px}.fields .checkbox{display:flex;align-items:center}.fields input:not([type=checkbox]){min-width:0;padding:5px;border:1px solid #aaa797;background:#fff5d6}footer{justify-content:flex-end}
</style>
