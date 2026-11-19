<script setup lang="ts">
import { computed, ref, watch } from 'vue'

const fields = [
  { key: 'minValue', label: 'MinValue' },
  { key: 'guideline1', label: 'guideline_1' },
  { key: 'guideline2', label: 'guideline_2' },
  { key: 'maxValue', label: 'MaxValue' },
] as const
type Field = typeof fields[number]['key']
type SpecValues = Record<Field, number>
type SavedSpec = SpecValues & { parameterNo: string; reviser: string; reviseDate: string }
type EquipmentSpecs = Record<string, SavedSpec[]>
type Parameter = { parameterNo: string; sequence: number; specType: number }
type Draft = Parameter & { values: Record<Field, string> }

const props = defineProps<{
  productNo: string
  productVersion: string
  processNo: string
  processVersion: string
  equipmentOptions: { equipmentNo: string; equipmentType: string }[]
  savedSpecs: EquipmentSpecs
  developmentPreview: boolean
  currentUser: string
  save: (equipmentNo: string, rows: SavedSpec[]) => boolean
}>()
const emit = defineEmits<{ close: [] }>()
const catalog: Record<string, Parameter[]> = {
  'DEV-DISPENSER': [
    { sequence: 1, parameterNo: 'DEV-DISP-PRESSURE', specType: 1 },
    { sequence: 2, parameterNo: 'DEV-DISP-TEMP', specType: 1 },
  ],
  'DEV-ASSEMBLY': [{ sequence: 1, parameterNo: 'DEV-TORQUE', specType: 1 }],
}
const equipmentNo = ref('')
const draftRows = ref<Draft[]>([])
const error = ref('')
const notice = ref('')
const activeEquipment = computed(() => props.equipmentOptions.find(item => item.equipmentNo === equipmentNo.value))

watch(equipmentNo, () => {
  const equipment = activeEquipment.value
  const saved = new Map(props.savedSpecs[equipmentNo.value]?.map(row => [row.parameterNo, row]))
  draftRows.value = (catalog[equipment?.equipmentType ?? ''] ?? []).map(parameter => {
    const values = saved.get(parameter.parameterNo)
    return { ...parameter, values: {
      minValue: String(values?.minValue ?? ''), maxValue: String(values?.maxValue ?? ''),
      guideline1: String(values?.guideline1 ?? ''), guideline2: String(values?.guideline2 ?? ''),
    } }
  })
  error.value = ''
  notice.value = ''
})

function validate(row: Draft): SpecValues {
  const { minValue, guideline1, guideline2, maxValue } = row.values
  if (![minValue, guideline1, guideline2, maxValue].every(value => /^-?\d{1,6}(?:\.\d{1,6})?$/.test(value.trim()))) {
    throw new Error(row.parameterNo + ' 的四個欄位皆必填，整數和小數各最多六位。')
  }
  const values = { minValue: Number(minValue), guideline1: Number(guideline1), guideline2: Number(guideline2), maxValue: Number(maxValue) }
  if (!(values.minValue < values.guideline1 && values.guideline1 < values.guideline2 && values.guideline2 < values.maxValue)) {
    throw new Error(row.parameterNo + ' 必須符合 MinValue < guideline_1 < guideline_2 < MaxValue。')
  }
  return values
}

function confirm(): void {
  if (!props.developmentPreview || !activeEquipment.value || !draftRows.value.length) return
  notice.value = ''
  error.value = ''
  try {
    const reviseDate = new Date().toISOString()
    const rows = draftRows.value.map(row => ({ parameterNo: row.parameterNo, ...validate(row), reviser: props.currentUser, reviseDate }))
    if (!props.save(equipmentNo.value, rows)) throw new Error('設備參數規格假資料儲存失敗。')
    notice.value = '已儲存設備參數規格假資料。'
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : '設備參數規格假資料儲存失敗。'
  }
}
</script>

<template>
  <section class="spec-window" role="dialog" aria-modal="true" aria-labelledby="spec-title">
    <header class="spec-bar">
      <h2 id="spec-title">Product EquipmentSpec</h2><small v-if="developmentPreview">DEV 假資料</small>
      <button class="spec-close" type="button" aria-label="關閉 EquipmentSpec" @click="emit('close')">×</button>
    </header>
    <form class="spec-body" @submit.prevent="confirm">
      <div class="spec-context">
        <span>ProductNo <strong>{{ productNo }}</strong></span><span>ProductVersion <strong>{{ productVersion }}</strong></span>
        <span>Process <strong>{{ processNo }} / {{ processVersion }}</strong></span>
        <label v-if="developmentPreview">EquipmentNo <select v-model="equipmentNo">
          <option value="">請選擇設備</option><option v-for="item in equipmentOptions" :key="item.equipmentNo" :value="item.equipmentNo">{{ item.equipmentNo }}</option>
        </select></label>
      </div>
      <p v-if="!developmentPreview">正式 EquipmentSpec API 尚未接線。</p>
      <p v-else-if="!equipmentOptions.length">所選流程尚無相符的 DEV 設備。</p>
      <div v-else class="spec-table"><table>
        <thead><tr><th scope="col">SeqNo</th><th scope="col">ParameterNo</th><th scope="col">SpecType</th><th v-for="field in fields" :key="field.key" scope="col">{{ field.label }}</th></tr></thead>
        <tbody>
          <tr v-for="row in draftRows" :key="row.parameterNo">
            <td>{{ row.sequence }}</td><td>{{ row.parameterNo }}</td><td>{{ row.specType }}</td>
            <td v-for="field in fields" :key="field.key"><input v-model="row.values[field.key]" inputmode="decimal" :aria-label="row.parameterNo + ' ' + field.label" @input="error = ''; notice = ''"></td>
          </tr>
          <tr v-if="!draftRows.length"><td colspan="7">{{ equipmentNo ? '此設備尚無 DEV 規格參數。' : '請先選擇設備。' }}</td></tr>
        </tbody>
      </table></div>
      <p v-if="error" class="spec-error" role="alert">{{ error }}</p><p v-else-if="notice" role="status">{{ notice }}</p>
      <footer class="spec-actions">
        <button type="submit" :disabled="!developmentPreview || !draftRows.length">Confirm</button>
        <button type="button" @click="emit('close')">Close</button>
      </footer>
    </form>
  </section>
</template>

<style scoped>
.spec-window { width: min(960px, 96vw); max-height: 92vh; overflow: auto; background: #f7f5ee; color: var(--ink, #26251f); border: 1px solid #a9a69d; box-shadow: 0 8px 30px #0004; }
.spec-bar, .spec-context, .spec-actions { display: flex; flex-wrap: wrap; align-items: center; gap: 7px 12px; }
.spec-bar, .spec-body { padding: 8px 12px; }
.spec-bar { border-bottom: 1px solid var(--line, #e5e1d5); }
.spec-bar h2 { margin: 0; font-size: 15px; }
.spec-close, .spec-context label { margin-left: auto; }
.spec-window button { min-height: 26px; border: 1px solid #b7ae90; background: #fff; color: inherit; padding: 3px 8px; font: inherit; cursor: pointer; }
.spec-window button:disabled { opacity: .5; cursor: default; }
.spec-window input, .spec-window select { min-width: 0; border: 1px solid #b7ae90; padding: 4px; background: #fff2bd; color: inherit; }
.spec-table { overflow: auto; max-height: 40vh; margin: 8px 0; background: white; }
.spec-table table { width: 100%; border-collapse: collapse; white-space: nowrap; font-size: 12px; }
.spec-table th, .spec-table td { padding: 5px; border: 1px solid var(--line, #e5e1d5); text-align: left; }
.spec-table th { position: sticky; top: 0; background: #eeece4; }
.spec-table input { width: 115px; text-align: right; box-sizing: border-box; }
.spec-actions { justify-content: flex-end; padding-top: 6px; }
.spec-error { color: #941b18; }
</style>
