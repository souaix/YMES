<script setup lang="ts">
import { computed, ref } from 'vue'

const categories = [
  { type: 0, label: 'Attributes' },
  { type: 1, label: 'Variables' },
  { type: 2, label: 'Variable Groups' },
  { type: 3, label: 'Attribute Groups' },
] as const
const inspectionFlags = [
  { key: 'inspTypeFAI', label: 'FAI' },
  { key: 'inspTypeIPQC', label: 'IPQC' },
  { key: 'inspTypePQC', label: 'PQC' },
] as const
type QCItemType = typeof categories[number]['type']
type InspectionFlag = typeof inspectionFlags[number]['key']
type InspectionFlags = Record<InspectionFlag, 0 | 1>
type Assignment = InspectionFlags & {
  opNo: string
  qcItemNo: string
  qcItemName: string
  qcItemType: QCItemType
  executeOrder: number | string
  inLine: 0 | 1
  reviser: string
  reviseDate: string
}
type CatalogItem = Pick<Assignment, 'qcItemNo' | 'qcItemName' | 'qcItemType'> & { lotIsValid: boolean }

const props = defineProps<{
  productNo: string
  productVersion: string
  productName: string
  savedRows: Record<string, unknown>[]
  developmentPreview: boolean
  currentUser: string
  saveAssignments: (opNo: string, assignments: Assignment[]) => boolean
}>()
const emit = defineEmits<{ close: [] }>()

const operationCatalog = [
  { opNo: 'DEV-OP-10', opName: '點膠作業' },
  { opNo: 'DEV-OP-20', opName: '組裝作業' },
  { opNo: 'DEV-OP-30', opName: '終檢作業' },
]
const itemCatalog: Record<string, CatalogItem[]> = {
  'DEV-OP-10': [
    { qcItemNo: 'DEV-QC-VISUAL', qcItemName: '外觀檢查', qcItemType: 0, lotIsValid: true },
    { qcItemNo: 'DEV-QC-WEIGHT', qcItemName: '點膠重量', qcItemType: 1, lotIsValid: true },
    { qcItemNo: 'DEV-QC-DIM', qcItemName: '尺寸量測群組', qcItemType: 2, lotIsValid: true },
    { qcItemNo: 'DEV-QC-DOT', qcItemName: '點膠外觀群組', qcItemType: 3, lotIsValid: false },
  ],
  'DEV-OP-20': [
    { qcItemNo: 'DEV-QC-ASSEMBLY', qcItemName: '組裝外觀', qcItemType: 0, lotIsValid: true },
    { qcItemNo: 'DEV-QC-TORQUE', qcItemName: '鎖附扭力', qcItemType: 1, lotIsValid: true },
  ],
  'DEV-OP-30': [
    { qcItemNo: 'DEV-QC-FINAL', qcItemName: '終檢外觀群組', qcItemType: 3, lotIsValid: true },
  ],
}

const opNo = ref('')
const working = ref<Assignment[]>([])
const catalogSelection = ref<string[]>([])
const assignedSelection = ref<string[]>([])
const dirty = ref(false)
const error = ref('')
const notice = ref('')
const importPanel = ref<'QC Import' | 'QC Chart' | null>(null)
const operations = computed(() => {
  if (!props.developmentPreview) return []
  const choices = new Map(operationCatalog.map(row => [row.opNo, row.opName]))
  for (const row of props.savedRows) {
    const savedOp = String(row.opNo ?? '')
    if (savedOp && !choices.has(savedOp)) choices.set(savedOp, '')
  }
  return [...choices]
})
const available = computed(() => (itemCatalog[opNo.value] ?? []).filter(item =>
  !working.value.some(row => row.qcItemNo === item.qcItemNo)))
const catalogGroups = computed(() => categories.map(category => ({
  ...category, items: available.value.filter(item => item.qcItemType === category.type),
})))
const canEdit = computed(() => props.developmentPreview && !!opNo.value)

function checkFlag(value: unknown): 0 | 1 {
  return Number(value) === 1 ? 1 : 0
}

function chooseOperation(event: Event): void {
  const select = event.target as HTMLSelectElement
  if (dirty.value && !window.confirm('切換作業站會捨棄尚未儲存的品管設定。確定切換？')) {
    select.value = opNo.value
    return
  }
  opNo.value = select.value
  working.value = props.savedRows.filter(row => row.opNo === opNo.value).flatMap(row => {
    const category = categories.find(entry => entry.type === Number(row.qcItemType))
    if (!category || !row.qcItemNo) return []
    const qcItemNo = String(row.qcItemNo)
    const catalogItem = itemCatalog[opNo.value]?.find(item => item.qcItemNo === qcItemNo)
    const inLine = checkFlag(row.inLine)
    return [{
      opNo: opNo.value, qcItemNo, qcItemName: String(row.qcItemName ?? catalogItem?.qcItemName ?? ''), qcItemType: category.type,
      inLine, executeOrder: Number(row.executeOrder ?? 0),
      inspTypeFAI: inLine ? checkFlag(row.inspTypeFAI) : 0,
      inspTypeIPQC: inLine ? checkFlag(row.inspTypeIPQC) : 0,
      inspTypePQC: inLine ? checkFlag(row.inspTypePQC) : 0,
      reviser: String(row.reviser ?? ''), reviseDate: String(row.reviseDate ?? ''),
    }]
  }).sort((left, right) => Number(left.executeOrder) - Number(right.executeOrder))
  catalogSelection.value = []
  assignedSelection.value = []
  dirty.value = false
  error.value = ''
  notice.value = ''
}

function change(row?: Assignment): void {
  if (row) {
    row.reviser = ''
    row.reviseDate = ''
  }
  dirty.value = true
  error.value = ''
  notice.value = ''
}

function assign(items: CatalogItem[], inLine: 0 | 1): void {
  if (!canEdit.value || !items.length) return
  const invalid = inLine ? items.find(item => !item.lotIsValid) : undefined
  if (invalid) {
    error.value = invalid.qcItemNo + ' 未啟用 Lot 檢驗，不能加入 InLine。'
    return
  }
  for (const item of items) {
    if (working.value.some(row => row.qcItemNo === item.qcItemNo)) continue
    working.value.push({
      opNo: opNo.value, qcItemNo: item.qcItemNo, qcItemName: item.qcItemName, qcItemType: item.qcItemType,
      executeOrder: 0, inLine, inspTypeFAI: 0, inspTypeIPQC: 0, inspTypePQC: 0, reviser: '', reviseDate: '',
    })
  }
  catalogSelection.value = catalogSelection.value.filter(value => !items.some(item => item.qcItemNo === value))
  change()
}

function removeSelected(): void {
  if (!canEdit.value || !assignedSelection.value.length) return
  working.value = working.value.filter(row => !assignedSelection.value.includes(row.qcItemNo))
  assignedSelection.value = []
  change()
}

function save(closeAfter: boolean): void {
  if (!canEdit.value) return
  const missingFlags = working.value.find(row => row.inLine && !inspectionFlags.some(flag => row[flag.key]))
  if (missingFlags) {
    error.value = missingFlags.qcItemNo + ' 至少要選擇一種 InLine 檢驗類型。'
    return
  }
  const invalidOrder = working.value.find(row => row.inLine && !/^\d{1,2}$/.test(String(row.executeOrder)))
  if (invalidOrder) {
    error.value = invalidOrder.qcItemNo + ' 的 ExecuteOrder 必須是 0 至 99 的整數。'
    return
  }
  const savedDate = new Date().toISOString()
  const assignments = working.value.map(row => ({
    ...row, executeOrder: Number(row.executeOrder), reviser: row.reviser || props.currentUser, reviseDate: row.reviseDate || savedDate,
  }))
  if (!props.saveAssignments(opNo.value, assignments)) {
    error.value = '品管設定假資料儲存失敗。'
    return
  }
  working.value = assignments
  dirty.value = false
  error.value = ''
  notice.value = '品管設定假資料已儲存。'
  if (closeAfter) emit('close')
}

function cancel(): void {
  if (!dirty.value || window.confirm('捨棄尚未儲存的品管設定？')) emit('close')
}
</script>

<template>
  <section class="qc-window" role="dialog" aria-modal="true" aria-labelledby="product-qc-title" @keydown.esc.stop="cancel">
    <header class="qc-header">
      <h2 id="product-qc-title">QC Item</h2>
      <span v-if="developmentPreview" class="qc-muted">DEV 假資料</span>
      <button class="qc-close" type="button" aria-label="關閉 QC Item" @click="cancel">×</button>
    </header>
    <div class="qc-context">
      <span>ProductNo <strong>{{ productNo }}</strong></span>
      <span>ProductVersion <strong>{{ productVersion }}</strong></span>
      <span>{{ productName }}</span>
      <label v-if="developmentPreview">OP No
        <select :value="opNo" @change="chooseOperation">
          <option value="">請選擇作業站</option>
          <option v-for="[number, name] in operations" :key="number" :value="number">{{ number }} {{ name }}</option>
        </select>
      </label>
    </div>
    <div v-if="canEdit" class="qc-workspace">
      <div class="qc-catalog">
        <fieldset v-for="group in catalogGroups" :key="group.type">
          <legend>{{ group.label }}</legend>
          <div class="qc-options">
            <label v-for="item in group.items" :key="item.qcItemNo" @dblclick="assign([item], 1)">
              <input v-model="catalogSelection" type="checkbox" :value="item.qcItemNo">
              {{ item.qcItemNo }} {{ item.qcItemName }}
            </label>
            <span v-if="!group.items.length" class="qc-muted">無可指派項目。</span>
          </div>
          <div class="qc-buttons">
            <button v-for="mode in ([1, 0] as const)" :key="mode" type="button"
              :disabled="!group.items.some(item => catalogSelection.includes(item.qcItemNo))"
              @click="assign(group.items.filter(item => catalogSelection.includes(item.qcItemNo)), mode)">{{ mode ? 'InLine' : 'OffLine' }} →</button>
          </div>
        </fieldset>
      </div>
      <fieldset class="qc-assigned">
        <legend>PRDQCITEM</legend>
        <button type="button" :disabled="!assignedSelection.length" @click="removeSelected">← 移除選取</button>
        <div class="qc-table">
          <table>
            <thead><tr><th scope="col">選取</th><th scope="col">ExecuteStatus</th><th scope="col">QCItemNo</th><th scope="col">QCItemName</th><th scope="col">QCItemType</th><th scope="col">ExecuteOrder</th>
              <th v-for="flag in inspectionFlags" :key="flag.key" scope="col">{{ flag.label }}</th><th scope="col">Reviser</th><th scope="col">ReviseDate</th></tr></thead>
            <tbody>
              <tr v-for="row in working" :key="row.qcItemNo">
                <td><input v-model="assignedSelection" type="checkbox" :value="row.qcItemNo" :aria-label="'選取 ' + row.qcItemNo"></td>
                <td>{{ row.inLine ? 'InLine' : 'OffLine' }}</td><td>{{ row.qcItemNo }}</td><td>{{ row.qcItemName }}</td><td>{{ categories.find(item => item.type === row.qcItemType)?.label }}</td>
                <td><input v-model="row.executeOrder" :aria-label="row.qcItemNo + ' ExecuteOrder'" inputmode="numeric" :disabled="!row.inLine" @input="change(row)"></td>
                <td v-for="flag in inspectionFlags" :key="flag.key"><input v-model="row[flag.key]" type="checkbox" :true-value="1" :false-value="0"
                  :aria-label="row.qcItemNo + ' ' + flag.label" :disabled="!row.inLine" @change="change(row)"></td>
                <td>{{ row.reviser }}</td><td>{{ row.reviseDate ? new Date(row.reviseDate).toLocaleString('sv-SE') : '' }}</td>
              </tr>
              <tr v-if="!working.length"><td colspan="11" class="qc-muted">這個作業站尚未指派品管項目。</td></tr>
            </tbody>
          </table>
        </div>
      </fieldset>
    </div>
    <p v-else class="qc-status">{{ developmentPreview ? '選擇 OP No 後可設定品管項目。' : '正式 QC Item API 尚未接線。' }}</p>
    <p v-if="error" class="qc-status qc-error" role="alert">{{ error }}</p>
    <p v-else-if="notice" class="qc-status" role="status">{{ notice }}</p>
    <footer class="qc-footer">
      <button type="button" :disabled="!developmentPreview" @click="importPanel = 'QC Import'">QC Import</button>
      <button type="button" :disabled="!developmentPreview || dirty" @click="importPanel = 'QC Chart'">QC Chart</button>
      <span></span>
      <button type="button" :disabled="!canEdit || !dirty" @click="save(false)">Apply</button>
      <button type="button" :disabled="!canEdit" @click="save(true)">Confirm</button>
      <button type="button" @click="cancel">Cancel</button>
    </footer>
    <div v-if="importPanel" class="qc-import-overlay" @keydown.esc.stop="importPanel = null">
      <div class="qc-import" role="dialog" aria-modal="true" aria-labelledby="qc-import-title">
        <h3 id="qc-import-title">{{ importPanel }}</h3>
        <label>File <input type="file" accept=".xlsx,.xls"></label>
        <p>DEV 預覽供選檔。Excel 檢查與匯入功能待正式串接。</p>
        <button type="button" autofocus @click="importPanel = null">Close</button>
      </div>
    </div>
  </section>
</template>

<style scoped>
.qc-window { position: relative; width: min(1190px, 96vw); max-height: 92vh; overflow: auto; background: #f7f5ee; color: var(--ink, #26251f); border: 1px solid #a9a69d; box-shadow: 0 8px 30px #0004; }
.qc-header, .qc-context, .qc-footer { display: flex; align-items: center; flex-wrap: wrap; gap: 8px 14px; padding: 8px 12px; border-bottom: 1px solid var(--line, #e5e1d5); }
.qc-header h2 { margin: 0; font-size: 16px; }
.qc-close { margin-left: auto; }
.qc-window button { min-height: 27px; border: 1px solid #b7ae90; background: #fff; padding: 3px 8px; color: inherit; font: inherit; cursor: pointer; }
.qc-window button:disabled { opacity: .5; cursor: default; }
.qc-window button:hover:not(:disabled) { background: #fff2bd; }
.qc-window select, .qc-table input:not([type="checkbox"]) { border: 1px solid #b7ae90; background: #fff2bd; padding: 4px; font: inherit; }
.qc-table input:not([type="checkbox"]) { width: 58px; }
.qc-table input:disabled { opacity: .55; }
.qc-window input[type="checkbox"] { accent-color: #9a8000; }
.qc-workspace { display: grid; grid-template-columns: minmax(200px, 1fr) minmax(0, 2fr); align-items: start; gap: 10px; padding: 8px 12px; }
.qc-workspace fieldset { margin: 0 0 6px; min-width: 0; border: 1px solid var(--line, #e5e1d5); }
.qc-options { display: grid; gap: 5px; margin-bottom: 6px; }
.qc-options label { cursor: pointer; }
.qc-buttons, .qc-footer { display: flex; gap: 6px; }
.qc-footer { border-top: 1px solid var(--line, #e5e1d5); }
.qc-footer span { flex: 1; }
.qc-table { margin-top: 7px; overflow: auto; background: #fff; max-height: 42vh; }
.qc-table table { width: 100%; border-collapse: collapse; white-space: nowrap; }
.qc-table th, .qc-table td { border: 1px solid var(--line, #e5e1d5); padding: 4px 6px; text-align: left; font-size: 12px; }
.qc-table th { position: sticky; top: 0; background: #eeece4; }
.qc-muted { font-size: 12px; color: var(--muted, #777366); }
.qc-status { margin: 8px 12px; }
.qc-error { color: #941b18; }
.qc-import-overlay { position: absolute; inset: 0; display: grid; place-items: center; background: #0005; }
.qc-import { width: min(385px, 94%); background: #fff; border: 1px solid #a9a69d; padding: 10px 14px; }
.qc-import h3 { margin: 0 0 8px; }
@media (max-width: 650px) { .qc-workspace { grid-template-columns: 1fr; } }
</style>
