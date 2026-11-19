<script setup lang="ts">
import { computed, ref, toRaw } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'

type IssueState = -1 | 0 | 1 | 2
type MoSource = 0 | 1 | 2 | 3
interface Process { psNo: string; processNo: string; processVersion: string; opNo: string; haveComponent: boolean }
interface Component { componentNo: string; goodQty: number; unitNo: string }
interface Property { propertyNo: string; propertyValue: string }
interface WorkOrder {
  moNo: string; moSource: MoSource; roNo: string; itemNo: number; returnNo: string; customerNo: string
  productNo: string; productVersion: string; engNo: string; engVersion: string; moQty: number; moUnitNo: string
  moTypeNo: number; priority: number; factoryNo: string; customerLotNo: string; planFinishDate: string
  description: string; belongToMoNo: string; issueState: IssueState; moState: number; dataStamp: number
  creator: string; createDate: string; reviser: string; reviseDate: string
  processes: Process[]; components: Component[]; properties: Property[]
}

const props = defineProps<{ currentUserNo?: string; developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const isDev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()
const storageKey = 'ymes-dev-oe201-work-orders-v1'
const sources: { value: MoSource; label: string }[] = [
  { value: 0, label: '訂單' }, { value: 1, label: '退貨' }, { value: 2, label: '無來源' }, { value: 3, label: '參考訂單' },
]
const states: { value: IssueState; label: string }[] = [
  { value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' },
]
const products = ['DEV-PRD-1001', 'DEV-PRD-1002']
const moStateLabels: Record<number, string> = { 0: '未確認', 1: '簽核中', 2: '已確認', 3: '已發放' }
const defaultProcess = (): Process => ({ psNo: '', processNo: '', processVersion: '01', opNo: '', haveComponent: false })
const operator = () => props.currentUserNo || 'DEV001'
const now = () => new Date().toISOString()

function blank(): WorkOrder {
  return {
    moNo: '', moSource: 2, roNo: 'N/A', itemNo: -1, returnNo: 'N/A', customerNo: '', productNo: '',
    productVersion: '01', engNo: 'N/A', engVersion: 'N/A', moQty: 1, moUnitNo: 'PCS', moTypeNo: 1, priority: 0,
    factoryNo: 'DEV-FAB', customerLotNo: '', planFinishDate: '', description: '', belongToMoNo: 'N/A',
    issueState: 0, moState: 0, dataStamp: 0, creator: operator(), createDate: now(), reviser: '',
    reviseDate: '', processes: [], components: [], properties: [],
  }
}

function readLocal(): WorkOrder[] {
  if (!isDev.value) return []
  try {
    const saved = localStorage.getItem(storageKey)
    if (saved) {
      const rows = JSON.parse(saved) as unknown
      if (Array.isArray(rows)) return rows as WorkOrder[]
    }
  } catch {
    // A fresh preview can use the example orders when local storage is unreadable.
  }
  const seed = { ...blank(), productNo: 'DEV-PRD-1001', moQty: 25, processes: [
    { ...defaultProcess(), psNo: 'DEV-PS01', processNo: 'DEV-PROC01', opNo: 'DEV-OP01' },
  ] }
  return [
    { ...seed, moNo: 'DEV-MO-001' },
    { ...seed, moNo: 'DEV-MO-002', issueState: 1, moState: 1 },
    { ...seed, moNo: 'DEV-MO-003', issueState: 2, moState: 2 },
    { ...seed, moNo: 'DEV-MO-004', issueState: -1 },
  ]
}

const rows = ref(readLocal())
const currentState = ref<IssueState>(0)
const search = ref('')
const appliedSearch = ref('')
const activeNo = ref('')
const error = ref('')
const editError = ref('')
const editor = ref<WorkOrder>()
const mode = ref<'add' | 'edit' | 'copy' | 'property'>('add')
const section = ref<'basis' | 'process' | 'components' | 'property'>('basis')
const materials = ref<WorkOrder>()
const visible = computed(() => rows.value.filter(row =>
  row.issueState === currentState.value && row.moNo.toUpperCase().includes(appliedSearch.value),
).sort((a, b) => a.moNo.localeCompare(b.moNo)))
const active = computed(() => visible.value.find(row => row.moNo === activeNo.value) ?? visible.value[0])

function saveLocal(next: WorkOrder[]): boolean {
  if (!isDev.value) return false
  try {
    localStorage.setItem(storageKey, JSON.stringify(next))
    rows.value = next
    return true
  } catch {
    return false
  }
}

function open(nextMode: typeof mode.value): void {
  const row = active.value
  if (nextMode !== 'add' && !row) return
  if (nextMode === 'edit' && row?.issueState !== 0) return
  if (nextMode === 'copy' && row) {
    editor.value = {
      ...structuredClone(toRaw(row)), moNo: '', issueState: 0, moState: 0, dataStamp: 0,
      creator: operator(), createDate: now(), reviser: '', reviseDate: '', belongToMoNo: 'N/A',
    }
  } else {
    editor.value = nextMode === 'add' || !row ? blank() : structuredClone(toRaw(row))
  }
  mode.value = nextMode
  section.value = nextMode === 'property' ? 'property' : 'basis'
  editError.value = ''
}

function save(close: boolean): void {
  if (!editor.value || mode.value === 'property') return
  const row = structuredClone(toRaw(editor.value))
  row.moNo = row.moNo.trim().toUpperCase()
  if (!row.moNo || !row.productNo || !row.factoryNo || !row.moUnitNo) {
    editError.value = '工單、產品、廠別及工單單位必填。'; return
  }
  if (!Number.isFinite(row.moQty) || row.moQty <= 0 || row.moQty > 99_999_999.9999 ||
    !Number.isInteger(row.priority) || !Number.isInteger(row.moTypeNo) ||
    row.priority < 0 || row.priority > 99 || row.moTypeNo < 0 || row.moTypeNo > 99) {
    editError.value = '請確認工單量、類別和優先序。'; return
  }
  if (row.moSource === 0 || row.moSource === 3) {
    row.roNo = row.roNo.trim().toUpperCase()
    if (!row.roNo || row.roNo === 'N/A' || !Number.isInteger(row.itemNo) || row.itemNo < 0 || row.itemNo > 99) {
      editError.value = '訂單來源需要訂單編號與有效項次。'; return
    }
  } else {
    row.roNo = 'N/A'
    row.itemNo = -1
  }
  if (row.moSource === 1 && (!row.returnNo.trim() || row.returnNo === 'N/A')) {
    editError.value = '退貨來源需要退貨編號。'; return
  }
  if (row.moSource !== 1) row.returnNo = 'N/A'
  if (row.processes.some(process => !process.psNo.trim() || !process.processNo.trim())) {
    editError.value = '每筆流程需要生產區段及流程編號。'; return
  }
  const componentNos = row.components.map(component => component.componentNo.trim().toUpperCase())
  const propertyNos = row.properties.map(property => property.propertyNo.trim().toUpperCase())
  if (componentNos.some(no => !no) || new Set(componentNos).size !== componentNos.length ||
    propertyNos.some(no => !no) || new Set(propertyNos).size !== propertyNos.length) {
    editError.value = '元件及屬性編號不可空白或重複。'; return
  }
  if (row.components.some(component =>
    !Number.isFinite(component.goodQty) || component.goodQty < 0 || component.goodQty > 99_999_999.9999 || !component.unitNo.trim(),
  )) {
    editError.value = '請輸入有效的元件良品量及單位。'; return
  }
  row.components.forEach((component, index) => { component.componentNo = componentNos[index]! })
  row.properties.forEach((property, index) => { property.propertyNo = propertyNos[index]! })
  const existing = rows.value.find(item => item.moNo === row.moNo)
  if (mode.value === 'edit' ? existing?.issueState !== 0 || existing.dataStamp !== row.dataStamp : !!existing) {
    editError.value = mode.value === 'edit' ? '工單狀態已變更。' : '工單編號已存在。'; return
  }
  if (existing) { row.dataStamp++; row.reviser = operator(); row.reviseDate = now() }
  if (!saveLocal(existing ? rows.value.map(item => item === existing ? row : item) : [...rows.value, row])) {
    editError.value = '無法儲存本機工單。'; return
  }
  currentState.value = 0
  search.value = ''
  appliedSearch.value = ''
  activeNo.value = row.moNo
  editor.value = close ? undefined : structuredClone(row)
  mode.value = 'edit'
  editError.value = ''
}

async function changeState(action: 'delete' | 'approve' | 'release'): Promise<void> {
  const row = active.value
  if (!row) return
  error.value = ''
  const isApproval = action === 'approve'
  const isRelease = action === 'release'
  if (isApproval && row.issueState !== 0) return
  if (isRelease && (row.issueState !== 2 || row.moState !== 2)) return
  if (action === 'delete' && (row.issueState === 1 || row.moState >= 3)) return
  if (action !== 'delete') {
    if (!row.processes.length) { error.value = '工單至少需要一個流程。'; return }
    if (row.processes.some(process => process.haveComponent) && !row.components.length) {
      error.value = '需要元件的流程尚未指定元件。'; return
    }
    if (row.components.length > row.moQty) { error.value = '元件數量不可超過工單量。'; return }
  }
  const label = isApproval ? '核准' : isRelease ? '發放' : row.issueState === 2 ? '停用' : '刪除'
  if (!await requestConfirmation('確定' + label + '工單 ' + row.moNo + '？')) return
  const next = rows.value.flatMap(item => {
    if (item !== row) return [item]
    if (action === 'delete' && row.issueState !== 2) return []
    return [{
      ...row, issueState: isApproval || isRelease ? 2 as const : -1 as const,
      moState: isApproval ? 2 : isRelease ? 3 : row.moState,
      dataStamp: row.dataStamp + 1, reviser: operator(), reviseDate: now(),
    }]
  })
  if (!saveLocal(next)) error.value = '無法儲存本機工單。'
}

function apply(event: MouseEvent): void {
  if ((event.currentTarget as HTMLButtonElement).form?.reportValidity()) save(false)
}
</script>

<template>
  <section class="mo-page">
    <h2>工單基本資料</h2>
    <template v-if="isDev">
      <form class="toolbar" @submit.prevent="appliedSearch = search.trim().toUpperCase(); activeNo = ''; error = ''">
        <label>工單編號 <input v-model="search" maxlength="50"></label><button type="submit">查詢</button>
        <button v-for="state in states" :key="state.value" type="button" :aria-pressed="currentState === state.value" @click="currentState = state.value; activeNo = ''; error = ''">{{ state.label }}</button>
      </form>
      <p v-if="error" role="alert">{{ error }}</p>
      <div class="workspace">
        <div class="grid"><table>
          <thead><tr><th>選取</th><th>工單編號</th><th>來源</th><th>訂單</th><th>廠別</th><th>客戶</th><th>產品</th><th>版本</th><th>工單量</th><th>單位</th><th>狀態</th><th>計畫完工</th><th>說明</th></tr></thead>
          <tbody>
            <tr v-for="row in visible" :key="row.moNo" :class="{ selected: active?.moNo === row.moNo }" @click="activeNo = row.moNo">
              <td><input type="radio" name="selected-work-order" :checked="active?.moNo === row.moNo" :aria-label="'選取 ' + row.moNo" @change="activeNo = row.moNo"></td>
              <td>{{ row.moNo }}</td><td>{{ sources.find(item => item.value === row.moSource)?.label }}</td><td>{{ row.roNo }}</td><td>{{ row.factoryNo }}</td><td>{{ row.customerNo }}</td><td>{{ row.productNo }}</td><td>{{ row.productVersion }}</td><td>{{ row.moQty }}</td><td>{{ row.moUnitNo }}</td><td>{{ moStateLabels[row.moState] }}</td><td>{{ row.planFinishDate }}</td><td>{{ row.description }}</td>
            </tr>
            <tr v-if="!visible.length"><td colspan="13">沒有符合條件的工單</td></tr>
          </tbody>
        </table></div>
        <div class="actions">
          <button type="button" @click="open('add')">新增</button>
          <button type="button" :disabled="active?.issueState !== 0" @click="open('edit')">修改</button>
          <button type="button" :disabled="!active || active.issueState === 1 || active.moState >= 3" @click="changeState('delete')">刪除</button>
          <button type="button" :disabled="!active" @click="open('copy')">複製</button>
          <button type="button" :disabled="active?.issueState !== 0" @click="changeState('approve')">核准</button>
          <button type="button" :disabled="!active" @click="materials = active">用料清單</button>
          <button type="button" :disabled="active?.issueState !== 2 || active?.moState !== 2" @click="changeState('release')">發放</button>
          <button type="button" :disabled="!active" @click="open('property')">屬性</button>
        </div>
      </div>
      <p>資料總筆數 {{ visible.length }}</p>
    </template>
    <p v-else role="status">工單基本資料尚未接入正式服務。</p>
    <button type="button" @click="emit('close')">關閉</button>

    <div v-if="editor" class="overlay">
      <form class="dialog" role="dialog" aria-modal="true" aria-label="工單定義" @submit.prevent="save(true)">
        <h3>{{ mode === 'property' ? '工單屬性' : '工單定義' }} {{ editor.moNo }}</h3>
        <nav v-if="mode !== 'property'" class="toolbar" aria-label="工單設定">
          <button type="button" :aria-pressed="section === 'basis'" @click="section = 'basis'">基本資料</button>
          <button type="button" :aria-pressed="section === 'process'" @click="section = 'process'">流程</button>
          <button type="button" :aria-pressed="section === 'components'" @click="section = 'components'">元件</button>
          <button type="button" :aria-pressed="section === 'property'" @click="section = 'property'">屬性</button>
        </nav>
        <div v-show="section === 'basis'" class="fields">
          <label>工單編號 <input v-model="editor.moNo" maxlength="50" :readonly="mode === 'edit'"></label>
          <label>來源 <select v-model.number="editor.moSource"><option v-for="source in sources" :key="source.value" :value="source.value">{{ source.label }}</option></select></label>
          <template v-if="editor.moSource === 0 || editor.moSource === 3">
            <label>訂單編號 <input v-model="editor.roNo" maxlength="25"></label>
            <label>訂單項次 <input v-model.number="editor.itemNo" type="number" min="0" max="99"></label>
          </template>
          <label v-if="editor.moSource === 1">退貨編號 <input v-model="editor.returnNo" maxlength="20"></label>
          <label>廠別 <input v-model="editor.factoryNo" maxlength="20"></label>
          <label>客戶編號 <input v-model="editor.customerNo" maxlength="50"></label>
          <label>產品 <select v-model="editor.productNo"><option value="">請選擇</option><option v-for="product in products" :key="product">{{ product }}</option></select></label>
          <label>產品版本 <input v-model="editor.productVersion" maxlength="5"></label>
          <label>工程編號 <input v-model="editor.engNo" maxlength="30"></label>
          <label>工程版本 <input v-model="editor.engVersion" maxlength="3"></label>
          <label>工單量 <input v-model.number="editor.moQty" type="number" min="0.0001" max="99999999.9999" step="0.0001"></label>
          <label>工單單位 <select v-model="editor.moUnitNo"><option>PCS</option><option>SET</option></select></label>
          <label>工單類別 <input v-model.number="editor.moTypeNo" type="number" min="0" max="99"></label>
          <label>優先序 <input v-model.number="editor.priority" type="number" min="0" max="99"></label>
          <label>客戶批號 <input v-model="editor.customerLotNo" maxlength="50"></label>
          <label>計畫完工日 <input v-model="editor.planFinishDate" type="date"></label>
          <label class="wide">說明 <textarea v-model="editor.description" maxlength="1000"></textarea></label>
        </div>
        <fieldset v-if="section === 'process'">
          <legend>流程（依加入次序）</legend>
          <div v-for="(process, index) in editor.processes" :key="index" class="child-row">
            <label>生產區段 <input v-model="process.psNo" maxlength="50"></label><label>流程 <input v-model="process.processNo" maxlength="30"></label>
            <label>版本 <input v-model="process.processVersion" maxlength="3"></label><label>作業站 <input v-model="process.opNo" maxlength="50"></label>
            <label><input v-model="process.haveComponent" type="checkbox"> 需要元件</label><button type="button" @click="editor.processes.splice(index, 1)">移除</button>
          </div>
          <button type="button" :disabled="editor.processes.length >= 99" @click="editor.processes.push(defaultProcess())">新增流程</button>
        </fieldset>
        <fieldset v-if="section === 'components'">
          <legend>元件</legend>
          <div v-for="(component, index) in editor.components" :key="index" class="child-row">
            <label>元件編號 <input v-model="component.componentNo" maxlength="50"></label><label>良品量 <input v-model.number="component.goodQty" type="number" min="0" max="99999999.9999" step="0.0001"></label>
            <label>單位 <input v-model="component.unitNo" maxlength="30"></label><button type="button" @click="editor.components.splice(index, 1)">移除</button>
          </div>
          <button type="button" @click="editor.components.push({ componentNo: '', goodQty: 1, unitNo: editor.moUnitNo })">新增元件</button>
        </fieldset>
        <fieldset v-if="section === 'property'">
          <legend>屬性</legend>
          <p v-if="!editor.properties.length && mode === 'property'">沒有屬性</p>
          <div v-for="(property, index) in editor.properties" :key="index" class="child-row">
            <label>屬性編號 <input v-model="property.propertyNo" maxlength="20" :readonly="mode === 'property'"></label>
            <label>屬性值 <input v-model="property.propertyValue" maxlength="255" :readonly="mode === 'property'"></label>
            <button v-if="mode !== 'property'" type="button" @click="editor.properties.splice(index, 1)">移除</button>
          </div>
          <button v-if="mode !== 'property'" type="button" @click="editor.properties.push({ propertyNo: '', propertyValue: '' })">新增屬性</button>
        </fieldset>
        <p v-if="editError" role="alert">{{ editError }}</p>
        <footer><button v-if="mode !== 'property'" type="submit">確認</button><button v-if="mode !== 'property'" type="button" @click="apply">套用</button><button type="button" @click="editor = undefined">關閉</button></footer>
      </form>
    </div>
    <div v-if="materials" class="overlay">
      <section class="dialog" role="dialog" aria-modal="true" aria-label="工單用料清單">
        <h3>工單用料清單 {{ materials.moNo }}</h3>
        <table><thead><tr><th>料號</th><th>標準用量</th><th>需求量</th><th>單位</th></tr></thead>
          <tbody><tr><td>DEV-MTL-001</td><td>1</td><td>{{ materials.moQty }}</td><td>{{ materials.moUnitNo }}</td></tr></tbody>
        </table>
        <p>DEV 示範用料</p><footer><button type="button" @click="materials = undefined">關閉</button></footer>
      </section>
    </div>
    <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
  </section>
</template>

<style scoped>
.mo-page { display: grid; align-content: start; gap: 12px; padding: 16px; color: #253244; }
h2, h3, p { margin: 0; }
.toolbar, .child-row, footer { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; }
.child-row { margin-bottom: 10px; }
.workspace { display: grid; grid-template-columns: minmax(0, 1fr) auto; gap: 12px; }
.actions { display: flex; flex-direction: column; gap: 8px; }
.grid { overflow: auto; border: 1px solid #bcc7d7; background: white; max-height: 54vh; }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
th, td { padding: 7px 9px; border-bottom: 1px solid #dce4ed; white-space: nowrap; text-align: left; }
th { background: #eef3f9; }
.selected td, button[aria-pressed="true"] { background: #d5e7fa; }
button, input, select, textarea { font: inherit; }
button { width: max-content; min-height: 30px; padding: 3px 10px; border: 1px solid #b8c5d6; border-radius: 4px; background: #f7f9fc; color: inherit; }
button:disabled { opacity: .55; }
.actions button { width: 100%; }
.overlay { position: fixed; inset: 0; z-index: 150; display: grid; place-items: center; background: #21334b55; padding: 18px; }
.dialog { width: min(760px, 100%); max-height: 90vh; overflow: auto; display: grid; gap: 14px; background: white; padding: 18px; border: 1px solid #8fa4bf; border-radius: 6px; }
.fields { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px; }
.fields label { display: grid; gap: 3px; }
.wide { grid-column: 1 / -1; }
.fields :is(input, select, textarea) { width: 100%; }
footer { justify-content: flex-end; }
[role="alert"] { color: #9d2727; }
</style>
