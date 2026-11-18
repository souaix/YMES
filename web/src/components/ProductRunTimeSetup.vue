<script setup lang="ts">
import { computed, nextTick, ref } from 'vue'

const runtimeFields = [
  { key: 'stdUnitRunTime', label: 'StdUnitRunTime', quantity: false, equipment: false },
  { key: 'countOpUnitQty', label: 'CountOpUnitQty', quantity: true, equipment: false },
  { key: 'stdQueueTime', label: 'StdQueueTime', quantity: false, equipment: false },
  { key: 'stdUnitEmpTime', label: 'StdUnitEmpTime', quantity: false, equipment: true },
  { key: 'stdUnitEqpTime', label: 'StdUnitEQPTime', quantity: false, equipment: true },
  { key: 'countEqpUnitQty', label: 'CountEqpUnitQty', quantity: true, equipment: true },
] as const

type RuntimeField = typeof runtimeFields[number]['key']
type RuntimeValues = Record<RuntimeField, number>
type RuntimeRow = {
  opOrder: number
  opNo: string
  areaNo: string
  equipmentNo: string
  equipmentType: string
  values: RuntimeValues
}
type RuntimeDraft = Omit<RuntimeRow, 'values'> & { values: Record<RuntimeField, string> }
type RuntimeSetup = { rows: RuntimeRow[]; creator: string; createDate: string }

const props = defineProps<{
  productNo: string
  productVersion: string
  defaultRows: RuntimeRow[]
  setup?: RuntimeSetup
  developmentPreview: boolean
  currentUser: string
  saveSetup: (setup: RuntimeSetup) => boolean
}>()
const emit = defineEmits<{ close: [] }>()

function rowKey(row: RuntimeRow | RuntimeDraft): string {
  return JSON.stringify([row.areaNo, row.opNo, row.equipmentNo, row.equipmentType])
}

const savedByKey = new Map<string, RuntimeValues>(props.setup?.rows.map(row => [rowKey(row), row.values]))
const rows = ref<RuntimeDraft[]>(props.defaultRows.map(row => {
  const values = savedByKey.get(rowKey(row)) ?? row.values
  return {
    ...row,
    values: {
      stdUnitRunTime: String(values.stdUnitRunTime),
      countOpUnitQty: String(values.countOpUnitQty),
      stdQueueTime: String(values.stdQueueTime),
      stdUnitEmpTime: String(values.stdUnitEmpTime),
      stdUnitEqpTime: String(values.stdUnitEqpTime),
      countEqpUnitQty: String(values.countEqpUnitQty),
    },
  }
}).sort((left, right) => left.opOrder - right.opOrder))

const baseline = ref(JSON.stringify(rows.value))
const needsFirstSave = ref(props.developmentPreview && rows.value.length > 0 && !props.setup?.rows.length)
const dirty = computed(() => needsFirstSave.value || JSON.stringify(rows.value) !== baseline.value)
const selectedIndex = ref(rows.value.length ? 0 : -1)
const editor = ref<{ index: number; row: RuntimeDraft } | null>(null)
const editorForm = ref<HTMLFormElement | null>(null)
const editButton = ref<HTMLButtonElement | null>(null)
const error = ref('')
const editError = ref('')
const notice = ref('')
const creator = ref(props.setup?.creator ?? props.currentUser)
const createDate = ref(props.setup?.createDate ?? (props.developmentPreview ? new Date().toISOString() : ''))
const auditDate = computed(() => createDate.value ? new Date(createDate.value).toLocaleString('sv-SE') : '')
const canSave = computed(() => props.developmentPreview && rows.value.length > 0)

function validate(row: RuntimeDraft): RuntimeValues {
  const values = {} as RuntimeValues
  for (const field of runtimeFields) {
    const input = row.values[field.key].trim()
    const number = Number(input)
    const valid = field.quantity
      ? /^\d+$/.test(input) && number >= 1 && number <= 999999
      : /^\d{0,4}(?:\.\d{1,2})?$/.test(input) && input !== ''
    if (!valid) {
      const range = field.quantity ? '須為 1 至 999999 的整數' : '須為 0 至 9999.99，最多兩位小數'
      throw new Error(`${row.opNo} / ${row.areaNo} / ${row.equipmentNo} 的 ${field.label} ${range}。`)
    }
    values[field.key] = number
  }
  return values
}

function openEditor(index: number): void {
  const row = rows.value[index]
  if (!props.developmentPreview || !row) return
  selectedIndex.value = index
  editor.value = { index, row: { ...row, values: { ...row.values } } }
  editError.value = ''
  void nextTick(() => {
    const input = editorForm.value?.querySelector('input')
    input?.focus()
    input?.select()
  })
}

function closeEditor(): void {
  editor.value = null
  editButton.value?.focus()
}

function confirmEdit(): void {
  if (!editor.value) return
  const { index, row } = editor.value
  try {
    validate(row)
    rows.value[index] = row
    closeEditor()
    error.value = ''
    notice.value = ''
  } catch (cause) {
    editError.value = cause instanceof Error ? cause.message : '請檢查 RunTime 欄位。'
  }
}

function save(closeAfter: boolean): void {
  if (!canSave.value) return
  error.value = ''
  notice.value = ''
  try {
    const setup = {
      rows: rows.value.map(row => ({ ...row, values: validate(row) })),
      creator: props.currentUser,
      createDate: new Date().toISOString(),
    }
    if (!props.saveSetup(setup)) throw new Error('RunTime 假資料儲存失敗。')
    baseline.value = JSON.stringify(rows.value)
    needsFirstSave.value = false
    creator.value = setup.creator
    createDate.value = setup.createDate
    if (closeAfter) emit('close')
    else notice.value = 'RunTime 假資料已儲存。'
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : 'RunTime 假資料儲存失敗。'
  }
}

function close(): void {
  if (!dirty.value || window.confirm('RunTime 有尚未儲存的設定。確定關閉？')) emit('close')
}
</script>

<template>
  <section class="runtime-window" role="dialog" aria-modal="true" aria-labelledby="runtime-title" @keydown.esc.stop="close">
    <header class="runtime-header">
      <h2 id="runtime-title">RunTime</h2>
      <span v-if="developmentPreview" class="runtime-preview">DEV 假資料</span>
      <button class="runtime-close" type="button" aria-label="關閉 RunTime" @click="close">×</button>
    </header>

    <div class="runtime-context">
      <span>ProductNo <strong>{{ productNo }}</strong></span>
      <span>ProductVersion <strong>{{ productVersion }}</strong></span>
      <button ref="editButton" type="button" :disabled="!developmentPreview || selectedIndex < 0" @click="openEditor(selectedIndex)">EDIT</button>
    </div>

    <div class="runtime-table">
      <table>
        <thead><tr>
          <th scope="col">ProductNo</th><th scope="col">ProductVersion</th><th scope="col">OPOrder</th>
          <th scope="col">OPNo</th><th scope="col">AreaNo</th><th scope="col">EquipmentNo</th><th scope="col">EquipmentType</th>
          <th v-for="field in runtimeFields" :key="field.key" scope="col">{{ field.label }}<span v-if="!field.quantity"> (min)</span></th>
        </tr></thead>
        <tbody>
          <tr v-for="(row, index) in rows" :key="rowKey(row)" :class="{ selected: index === selectedIndex }" @click="selectedIndex = index" @focusin="selectedIndex = index">
            <td>{{ productNo }}</td><td>{{ productVersion }}</td><td>{{ row.opOrder }}</td>
            <td>{{ row.opNo }}</td><td>{{ row.areaNo }}</td><td>{{ row.equipmentNo }}</td><td>{{ row.equipmentType }}</td>
            <td v-for="field in runtimeFields" :key="field.key">
              <input v-model="row.values[field.key]" :aria-label="`${row.opNo} ${row.areaNo} ${row.equipmentNo} ${field.label}`"
                :inputmode="field.quantity ? 'numeric' : 'decimal'" :readonly="!developmentPreview" @input="notice = ''">
            </td>
          </tr>
          <tr v-if="!rows.length"><td colspan="13" class="runtime-empty">{{ developmentPreview ? '此產品版次沒有可設定的作業站／區域。' : '正式 RunTime API 尚未接線。' }}</td></tr>
        </tbody>
      </table>
    </div>

    <div v-if="developmentPreview" class="runtime-audit">
      <span>Creator <strong>{{ creator }}</strong></span><span>Create Date <strong>{{ auditDate }}</strong></span>
    </div>
    <p v-if="error" class="runtime-error" role="alert">{{ error }}</p>
    <p v-else-if="notice" class="runtime-notice" role="status">{{ notice }}</p>
    <footer class="runtime-actions">
      <button type="button" :disabled="!canSave" @click="save(false)">Apply</button>
      <button type="button" :disabled="!canSave" @click="save(true)">Confirm</button>
      <button type="button" @click="close">Close</button>
    </footer>

    <div v-if="editor" class="runtime-editor-overlay" @keydown.esc.stop="closeEditor">
      <form ref="editorForm" class="runtime-editor" role="dialog" aria-modal="true" aria-labelledby="runtime-editor-title" @submit.prevent="confirmEdit">
        <header class="runtime-header"><h2 id="runtime-editor-title">Product RunTime Definition</h2></header>
        <div class="runtime-editor-body">
          <p>{{ productNo }} / {{ productVersion }}　{{ editor.row.areaNo }}</p>
          <fieldset v-for="equipment in [false, true]" :key="String(equipment)">
            <legend>{{ equipment ? 'Equipment RunTime' : 'OP RunTime' }}</legend>
            <p>{{ equipment ? editor.row.equipmentNo : editor.row.opNo }}</p>
            <label v-for="field in runtimeFields.filter(field => field.equipment === equipment)" :key="field.key">
              <span>{{ field.label }}{{ field.quantity ? '' : ' (min)' }}</span>
              <input v-model="editor.row.values[field.key]" :inputmode="field.quantity ? 'numeric' : 'decimal'"
                :disabled="equipment && editor.row.equipmentNo === 'N/A'">
            </label>
          </fieldset>
          <p v-if="editError" class="runtime-error" role="alert">{{ editError }}</p>
        </div>
        <footer class="runtime-actions"><button type="submit">Confirm</button><button type="button" @click="closeEditor">Close</button></footer>
      </form>
    </div>
  </section>
</template>

<style scoped>
.runtime-window, .runtime-editor { background: #f7f5ee; border: 1px solid #a9a69d; color: var(--ink, #26251f); box-shadow: 0 8px 30px #0004; }
.runtime-window { position: relative; display: flex; flex-direction: column; width: min(1120px, 96vw); max-height: 92vh; min-height: min(425px, 92vh); }
.runtime-header, .runtime-context, .runtime-audit, .runtime-actions { display: flex; flex-wrap: wrap; align-items: center; gap: 10px 16px; padding: 8px 12px; }
.runtime-header, .runtime-actions { border-block: 1px solid var(--line, #e5e1d5); }
.runtime-header h2 { margin: 0; font-size: 16px; }
.runtime-preview, .runtime-audit { color: var(--muted, #777366); font-size: 12px; }
.runtime-context button, .runtime-close { margin-left: auto; }
.runtime-close { min-width: 32px; font-size: 17px; }
.runtime-table { flex: 1; min-height: 90px; overflow: auto; margin: 0 12px; background: #fff; border: 1px solid var(--line, #e5e1d5); }
.runtime-table table { width: 100%; border-collapse: collapse; font-size: 12px; white-space: nowrap; }
.runtime-table th, .runtime-table td { padding: 4px 6px; border: 1px solid var(--line, #e5e1d5); text-align: left; }
.runtime-table th { position: sticky; top: 0; background: #eeece4; }
.runtime-table th span { font-weight: normal; }
.runtime-table tr.selected td { background: #fff8d9; }
.runtime-table .runtime-empty { padding: 20px; text-align: center; color: var(--muted, #777366); }
.runtime-table input, .runtime-editor input { width: 100%; box-sizing: border-box; border: 1px solid #b7ae90; background: #fff2bd; padding: 3px 5px; font: inherit; text-align: right; }
.runtime-table input { min-width: 78px; }
input:focus { outline: 2px solid var(--yellow, #e7c600); outline-offset: -1px; }
input:disabled, input:read-only { background: #eeece4; }
.runtime-actions { justify-content: flex-end; }
.runtime-window button { min-height: 28px; border: 1px solid #b7ae90; background: #fff; color: inherit; padding: 3px 10px; font: inherit; cursor: pointer; }
.runtime-window button:disabled { opacity: .55; cursor: default; }
.runtime-window button:hover:not(:disabled) { background: #fff2bd; }
.runtime-error, .runtime-notice { margin: 4px 12px; font-size: 13px; }
.runtime-error { color: #941b18; }
.runtime-editor-overlay { position: absolute; inset: 0; z-index: 2; display: grid; place-items: center; background: #0005; padding: 8px; }
.runtime-editor { width: min(450px, 100%); max-height: 100%; overflow: auto; }
.runtime-editor-body { padding: 8px 12px; }
.runtime-editor-body p { margin: 4px 0 10px; }
.runtime-editor fieldset { margin: 0 0 8px; border: 1px solid var(--line, #e5e1d5); }
.runtime-editor label { display: grid; grid-template-columns: 1fr 90px; align-items: center; gap: 8px; margin: 5px 0; }
</style>
