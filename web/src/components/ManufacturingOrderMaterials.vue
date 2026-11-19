<script setup lang="ts">
import { computed, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'

interface WorkOrderChoice { moNo: string; moQty: number; moState: number }
interface Material {
  moNo: string; materialNo: string; substituteNo: string; opNo: string; materialLevel: 0 | 1
  standardQty: number; substituteQty: number; decreaseRate: number; putInPlaceType: number
  unitNo: string; specified: boolean; description: string
}
interface Issue { moNo: string; actualMaterialNo: string; lotNo: string; qty: number; unitNo: string }
interface Preview { materials: Material[]; issues: Issue[] }

const props = defineProps<{ orders: WorkOrderChoice[]; initialMoNo?: string; developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const isDev = computed(() => import.meta.env.DEV && props.developmentPreview)
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()
const storageKey = 'ymes-dev-oe206-materials-v1'
const fixtures: Preview = {
  materials: [
    { moNo: 'DEV-MO-001', materialNo: 'DEV-MTL-001', substituteNo: 'N/A', opNo: 'DEV-OP01', materialLevel: 0, standardQty: 1, substituteQty: 1, decreaseRate: 0, putInPlaceType: 3, unitNo: 'PCS', specified: false, description: '' },
    { moNo: 'DEV-MO-003', materialNo: 'DEV-MTL-001', substituteNo: 'DEV-MTL-002', opNo: 'DEV-OP01', materialLevel: 0, standardQty: 2, substituteQty: 2, decreaseRate: 0, putInPlaceType: 3, unitNo: 'PCS', specified: false, description: '' },
    { moNo: 'DEV-MO-003', materialNo: 'DEV-PRD-1002', substituteNo: 'N/A', opNo: 'DEV-OP01', materialLevel: 1, standardQty: 1, substituteQty: 1, decreaseRate: 0, putInPlaceType: 3, unitNo: 'PCS', specified: false, description: '' },
  ],
  issues: [],
}
const choices = computed(() => props.orders.filter(order => [0, 1, 2, 3, 6].includes(order.moState))
  .sort((a, b) => a.moNo.localeCompare(b.moNo)))
const selectedMo = ref(props.initialMoNo ?? '')
const level = ref<0 | 1>(0)
const storage = ref<Preview>(readPreview())
const selectedKeys = ref<string[]>([])
const error = ref('')
const dialogError = ref('')
const editor = ref<Material>()
const editorAction = ref<'add' | 'edit' | 'replace'>('add')
const sourceKey = ref('')
const issueFrom = ref<Material>()
const lotNo = ref('')
const issuedQty = ref(0)
const actionMessage = ref('')

const order = computed(() => choices.value.find(item => item.moNo === selectedMo.value))
const rowKey = (row: Material) => JSON.stringify([row.moNo, row.materialNo, row.opNo, row.substituteNo])
const rowMaterial = (row: Material) => row.substituteNo === 'N/A' ? row.materialNo : row.substituteNo
const visible = computed(() => storage.value.materials.filter(row => row.moNo === order.value?.moNo && row.materialLevel === level.value)
  .sort((a, b) => a.materialNo.localeCompare(b.materialNo) || a.opNo.localeCompare(b.opNo)))
const active = computed(() => visible.value.find(row => selectedKeys.value.includes(rowKey(row))) ?? visible.value[0])
const errorForEdit = (row: Material) => row.specified ? '指定料不可修改。' : currentQty(row) !== 0 ? '已有存料，無法修改。' : ''

function readPreview(): Preview {
  if (!isDev.value) return { materials: [], issues: [] }
  try {
    const raw = localStorage.getItem(storageKey)
    if (raw) return JSON.parse(raw) as Preview
  } catch {
    // Use the example records when the stored preview is unavailable.
  }
  return fixtures
}

function persist(next: Preview): boolean {
  try {
    localStorage.setItem(storageKey, JSON.stringify(next))
    storage.value = next
    return true
  } catch {
    return false
  }
}

function requirement(row: Material): number {
  return Number(((order.value?.moQty ?? 0) * row.standardQty * (1 + row.decreaseRate)).toFixed(9))
}

function currentQty(row: Material): number {
  const qty = storage.value.issues.filter(issue => issue.moNo === row.moNo && issue.actualMaterialNo === rowMaterial(row))
    .reduce((total, issue) => total + issue.qty, 0)
  return Number(qty.toFixed(8))
}

function changeSelection(): void {
  selectedKeys.value = []
  error.value = ''
  actionMessage.value = ''
}

function openEditor(action: typeof editorAction.value): void {
  if (!order.value) return
  error.value = ''
  dialogError.value = ''
  const row = active.value
  if (action !== 'add') {
    if (!row) return
    error.value = errorForEdit(row)
    if (error.value) return
    if (action === 'replace' && (row.materialLevel !== 0 || order.value.moState < 2)) {
      error.value = '已確認的原物料才可指定替代料。'
      return
    }
  }
  editorAction.value = action
  sourceKey.value = action === 'add' || !row ? '' : rowKey(row)
  editor.value = action !== 'add' && row ? { ...row } : {
    moNo: order.value.moNo, materialNo: '', substituteNo: 'N/A', opNo: '', materialLevel: level.value,
    standardQty: 1, substituteQty: 1, decreaseRate: 0, putInPlaceType: 3, unitNo: 'PCS', specified: false, description: '',
  }
}

function saveEditor(): void {
  if (!editor.value || !order.value || !isDev.value) return
  const row = { ...editor.value }
  row.materialNo = row.materialNo.trim().toUpperCase()
  row.substituteNo = row.substituteNo.trim().toUpperCase() || 'N/A'
  row.opNo = row.opNo.trim().toUpperCase()
  row.unitNo = row.unitNo.trim().toUpperCase()
  if (!row.materialNo || !row.opNo || !row.unitNo) { dialogError.value = '料號、作業站及單位必填。'; return }
  if (row.substituteNo === row.materialNo && row.substituteNo !== 'N/A') {
    dialogError.value = '替代料與原料必須不同。'; return
  }
  const duplicate = storage.value.materials.some(item => rowKey(item) === rowKey(row) && rowKey(item) !== sourceKey.value)
  if (duplicate) { dialogError.value = '相同工單、物料、作業站及替代料已存在。'; return }
  const materials = sourceKey.value
    ? storage.value.materials.map(item => rowKey(item) === sourceKey.value ? row : item)
    : [...storage.value.materials, row]
  if (!persist({ ...storage.value, materials })) { dialogError.value = '無法儲存本機用料。'; return }
  selectedKeys.value = [rowKey(row)]
  editor.value = undefined
}

async function deleteMaterials(): Promise<void> {
  if (!isDev.value) return
  const rows = visible.value.filter(row => selectedKeys.value.includes(rowKey(row)))
  const selection = rows.length ? rows : active.value ? [active.value] : []
  if (!selection.length || !await requestConfirmation('確定刪除 ' + selection.length + ' 筆用料？')) return
  const removed = new Set<string>()
  error.value = ''
  for (const row of selection) {
    error.value = errorForEdit(row)
    if (error.value) break
    removed.add(rowKey(row))
  }
  if (removed.size && !persist({ ...storage.value, materials: storage.value.materials.filter(row => !removed.has(rowKey(row))) })) {
    error.value = '無法儲存本機用料。'; return
  }
  actionMessage.value = '已刪除 ' + removed.size + ' 筆。'
  selectedKeys.value = selectedKeys.value.filter(key => !removed.has(key))
}

function openIssue(): void {
  error.value = ''
  if (!order.value || order.value.moState < 2 || !active.value) {
    error.value = '工單確認後才可發料。'; return
  }
  issueFrom.value = active.value
  lotNo.value = ''
  issuedQty.value = 0
  dialogError.value = ''
}

function issueMaterial(): void {
  const row = issueFrom.value
  if (!row || !isDev.value || !lotNo.value.trim() || !Number.isFinite(issuedQty.value) || issuedQty.value <= 0) {
    dialogError.value = '批號及正數發料量為必填。'; return
  }
  const target = lotNo.value.trim().toUpperCase()
  const matching = storage.value.issues.find(issue =>
    issue.moNo === row.moNo && issue.actualMaterialNo === rowMaterial(row) && issue.lotNo === target,
  )
  const qty = Number(((matching?.qty ?? 0) + issuedQty.value).toFixed(8))
  if (qty > 99_999_999.99999999) { dialogError.value = '此批累計存料量超出允許範圍。'; return }
  const issued: Issue = { moNo: row.moNo, actualMaterialNo: rowMaterial(row), lotNo: target, qty, unitNo: row.unitNo }
  const issues = matching ? storage.value.issues.map(issue => issue === matching ? issued : issue) : [...storage.value.issues, issued]
  if (!persist({ ...storage.value, issues })) { dialogError.value = '無法儲存本機發料。'; return }
  issueFrom.value = undefined
  actionMessage.value = 'DEV 發料完成。'
}
</script>

<template>
  <section class="material-page">
    <h2>工單用料清單</h2>
    <template v-if="isDev">
      <div class="toolbar">
        <label>工單 <select v-model="selectedMo" @change="changeSelection">
          <option value="">請選擇</option><option v-for="item in choices" :key="item.moNo">{{ item.moNo }}</option>
        </select></label>
        <fieldset class="toolbar">
          <legend>用料類別</legend>
          <label><input v-model="level" type="radio" :value="0" @change="changeSelection"> 原物料</label>
          <label><input v-model="level" type="radio" :value="1" @change="changeSelection"> 半成品</label>
        </fieldset>
      </div>
      <div class="toolbar">
        <button type="button" :disabled="!order" @click="openEditor('add')">新增</button>
        <button type="button" :disabled="!active" @click="openEditor('edit')">修改</button>
        <button type="button" :disabled="!active" @click="deleteMaterials">刪除</button>
        <button type="button" :disabled="!active" @click="openEditor('replace')">替代料</button>
        <button type="button" :disabled="!active" @click="openIssue">模擬發料</button>
      </div>
      <p v-if="error" role="alert">{{ error }}</p><p v-if="actionMessage" role="status">{{ actionMessage }}</p>
      <div class="grid"><table>
        <thead><tr><th>選取</th><th>料號</th><th>替代料</th><th>作業站</th><th>標準量</th><th>替代料標準量</th><th>現有量</th><th>損耗率</th><th>需求量</th><th>單位</th><th>指定料</th><th>說明</th></tr></thead>
        <tbody>
          <tr v-for="row in visible" :key="rowKey(row)">
            <td><input v-model="selectedKeys" type="checkbox" :value="rowKey(row)" :aria-label="'選取 ' + row.materialNo + ' ' + row.opNo"></td>
            <td>{{ row.materialNo }}</td><td>{{ row.substituteNo }}</td><td>{{ row.opNo }}</td><td>{{ row.standardQty }}</td><td>{{ row.substituteQty }}</td><td>{{ currentQty(row) }}</td><td>{{ row.decreaseRate }}</td><td>{{ requirement(row) }}</td><td>{{ row.unitNo }}</td><td>{{ row.specified ? '是' : '否' }}</td><td>{{ row.description }}</td>
          </tr>
          <tr v-if="!visible.length"><td colspan="12">{{ order ? '此工單沒有該類別的用料' : '請選擇工單' }}</td></tr>
        </tbody>
      </table></div>
      <p>用料筆數 {{ visible.length }}</p>
    </template>
    <p v-else role="status">工單用料清單尚未接入正式服務。</p>
    <button type="button" @click="emit('close')">關閉</button>

    <div v-if="editor" class="overlay">
      <form class="dialog" role="dialog" aria-modal="true" aria-label="工單用料定義" @submit.prevent="saveEditor">
        <h3>{{ editorAction === 'replace' ? '替代料' : '工單用料定義' }}</h3>
        <div class="fields">
          <label>料號 <input v-model="editor.materialNo" maxlength="50" :readonly="editorAction !== 'add'" required></label>
          <label>作業站 <input v-model="editor.opNo" maxlength="20" :readonly="editorAction !== 'add'" required></label>
          <label>替代料 <input v-model="editor.substituteNo" maxlength="50" :readonly="editor.materialLevel === 1"></label>
          <label>標準量 <input v-model.number="editor.standardQty" type="number" min="0.000000001" max="99999999.999999999" step="0.000000001" required></label>
          <label>替代料標準量 <input v-model.number="editor.substituteQty" type="number" min="0" max="99999999.999999999" step="0.000000001" required></label>
          <label>損耗率 <input v-model.number="editor.decreaseRate" type="number" min="0" max="9.99" step="0.01" required></label>
          <label>投料點類型 <input v-model.number="editor.putInPlaceType" type="number" min="0" max="9" required></label>
          <label>單位 <input v-model="editor.unitNo" maxlength="30" required></label>
          <label><input v-model="editor.specified" type="checkbox"> 指定料</label>
          <label>說明 <input v-model="editor.description" maxlength="255"></label>
        </div>
        <p v-if="dialogError" role="alert">{{ dialogError }}</p>
        <footer><button type="submit">確認</button><button type="button" @click="editor = undefined">取消</button></footer>
      </form>
    </div>
    <div v-if="issueFrom" class="overlay">
      <form class="dialog" role="dialog" aria-modal="true" aria-label="DEV 模擬發料" @submit.prevent="issueMaterial">
        <h3>DEV 模擬發料</h3>
        <p>{{ issueFrom.moNo }} · {{ rowMaterial(issueFrom) }}</p>
        <label>批號 <input v-model="lotNo" maxlength="50" required></label>
        <label>數量 <input v-model.number="issuedQty" type="number" min="0.00000001" max="99999999.99999999" step="0.00000001" required></label>
        <p v-if="dialogError" role="alert">{{ dialogError }}</p>
        <footer><button type="submit">確認</button><button type="button" @click="issueFrom = undefined">取消</button></footer>
      </form>
    </div>
    <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
  </section>
</template>

<style scoped>
.material-page { display: grid; gap: 12px; align-content: start; }
h2, h3, p { margin: 0; }
.toolbar, footer { display: flex; align-items: center; flex-wrap: wrap; gap: 10px; }
.grid { overflow: auto; border: 1px solid #bcc7d7; background: white; }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
th, td { padding: 7px 9px; border-bottom: 1px solid #dce4ed; white-space: nowrap; text-align: left; }
th { background: #eef3f9; }
button { width: max-content; min-height: 30px; }
.overlay { position: fixed; inset: 0; z-index: 151; display: grid; place-items: center; background: #21334b55; padding: 18px; }
.dialog { width: min(600px, 100%); display: grid; gap: 12px; max-height: 90vh; overflow: auto; background: white; padding: 18px; border: 1px solid #8fa4bf; border-radius: 6px; }
.fields { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 9px; }
.fields label { display: grid; gap: 3px; }
footer { justify-content: flex-end; }
[role="alert"] { color: #9d2727; }
</style>
