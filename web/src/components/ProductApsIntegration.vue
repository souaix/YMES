<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'

type Row = Record<string, unknown>
type NodeOption = { nodeId: string; opNo: string; equipmentNos: string[] }
const numberFields = [
  { name: 'apsFixEqpTime', label: 'APS Fix EQP Time', min: 0, max: 99999999, fallback: '0' },
  { name: 'apsVarEqpTime', label: 'APS Var EQP Time', min: 0, max: 99999999, fallback: '0' },
  { name: 'stdWorkTimeQty', label: 'STD Work Time Qty', min: 1, max: 99999999, fallback: '' },
  { name: 'transferQty', label: 'Transfer Qty', min: 0, max: 99999999, fallback: '0' },
  { name: 'lotRearTime', label: 'Lot Rear Time', min: 0, max: 99999999, fallback: '0' },
  { name: 'batchMaxQty', label: 'Batch Max Qty', min: 0, max: 999999999, fallback: '999999' },
] as const

const props = defineProps<{
  productNo: string
  productVersion: string
  processNo: string
  processVersion: string
  moTypeNo: number
  nodeOptions: NodeOption[]
  setups: Row[]
  details: Row[]
  developmentPreview: boolean
  currentUser: string
  save: (setups: Row[], details: Row[]) => boolean
}>()
const emit = defineEmits<{ close: [] }>()
const nodeId = ref('')
const equipmentNo = ref('N/A')
const tab = ref<'base' | 'detail'>('base')
const group = ref('N/A')
const subcontractor = ref('N/A')
const schedule = ref(false)
const batch = ref(false)
const description = ref('')
const numeric = ref<Record<string, string>>({})
const notice = ref('')
const error = ref('')
const activeNode = computed(() => props.nodeOptions.find(row => row.nodeId === nodeId.value))
const currentSetup = computed(() => props.setups.find(row => matchNode(row, nodeId.value)))
const currentDetail = computed(() => props.details.find(row => matchNode(row, nodeId.value) && row.equipmentNo === equipmentNo.value))
const visibleSetups = computed(() => props.setups.filter(row => matchesProcess(row)))
const visibleDetails = computed(() => props.details.filter(row => matchNode(row, nodeId.value)))
const equipmentOptions = computed(() => [...new Set([
  ...(activeNode.value?.equipmentNos ?? []),
  ...visibleDetails.value.map(row => string(row.equipmentNo)).filter(Boolean),
  string(currentSetup.value?.equipmentNo),
])].filter(item => item && item !== 'N/A'))
const canUpdate = computed(() => props.developmentPreview && !!activeNode.value
  && (tab.value === 'base' || (!!currentSetup.value && equipmentNo.value !== 'N/A')))

function string(value: unknown): string { return value == null ? '' : String(value) }
function matchesProcess(row: Row): boolean {
  return row.processNo === props.processNo && row.processVersion === props.processVersion && Number(row.moTypeNo) === props.moTypeNo
}
function matchNode(row: Row, key: string): boolean {
  return Number(row.moTypeNo) === props.moTypeNo && row.nodeId === key
}
function loadDraft(): void {
  const row = tab.value === 'base' ? currentSetup.value : currentDetail.value
  if (tab.value === 'base') equipmentNo.value = string(row?.equipmentNo ?? 'N/A')
  group.value = string(row?.equipmentGroup ?? currentSetup.value?.equipmentGroup ?? 'N/A')
  subcontractor.value = string(row?.subcontractorNo ?? 'N/A')
  schedule.value = Number(row?.schedule ?? 0) === 1
  batch.value = Number(row?.batchOp ?? 0) === 1
  description.value = string(row?.description)
  numeric.value = Object.fromEntries(numberFields.map(field => [field.name, string(row?.[field.name] ?? field.fallback)]))
  error.value = ''
  notice.value = ''
}
watch([nodeId, tab], loadDraft, { immediate: true })
watch(equipmentNo, () => {
  if (tab.value === 'detail') loadDraft()
})

function selectNode(value: string): void {
  nodeId.value = value
  equipmentNo.value = 'N/A'
  tab.value = 'base'
}

function openEquipmentTime(): void {
  if (!currentSetup.value) return
  equipmentNo.value = 'N/A'
  tab.value = 'detail'
}

function update(): void {
  if (!canUpdate.value || !activeNode.value) return
  const numbers: Record<string, number> = {}
  for (const field of numberFields) {
    const entered = numeric.value[field.name]?.trim() || field.fallback
    const value = Number(entered)
    if (!/^\d+$/.test(entered) || !Number.isInteger(value) || value < field.min || value > field.max) {
      error.value = field.label + ' 須為 ' + field.min + '–' + field.max + ' 的整數。'
      return
    }
    numbers[field.name] = value
  }
  const base = tab.value === 'base'
  const old = base ? currentSetup.value : currentDetail.value
  const now = new Date().toISOString()
  const row: Row = {
    ...old, ...numbers, productNo: props.productNo, productVersion: props.productVersion, processNo: props.processNo,
    processVersion: props.processVersion, moTypeNo: props.moTypeNo, opNo: activeNode.value.opNo, nodeId: nodeId.value,
    equipmentNo: equipmentNo.value, equipmentGroup: group.value || 'N/A', schedule: Number(schedule.value),
    batchOp: Number(batch.value), description: description.value.trim(),
    creator: old?.creator ?? props.currentUser, createDate: old?.createDate ?? now,
    reviser: old ? props.currentUser : '', reviseDate: old ? now : '',
  }
  const remainder = (rows: Row[]) => rows.filter(item => !matchNode(item, nodeId.value)
    || (!base && item.equipmentNo !== equipmentNo.value))
  const defaults = base ? activeNode.value.equipmentNos.map(equipment => ({
    ...row, equipmentNo: equipment, creator: props.currentUser, createDate: now, reviser: '', reviseDate: '',
  })) : []
  const saved = base ? props.save(
    [...remainder(props.setups), { ...row, subcontractorNo: subcontractor.value || 'N/A' }],
    [...remainder(props.details), ...defaults],
  )
    : props.save(props.setups, [...remainder(props.details), row])
  if (!saved) {
    error.value = 'APS 假資料儲存失敗。'
    return
  }
  error.value = ''
  notice.value = '已儲存 APS 假資料。'
}

function remove(): void {
  const base = tab.value === 'base'
  if (!props.developmentPreview || !(base ? currentSetup.value : currentDetail.value) || !window.confirm(
    base ? '刪除此節點的 APS 設定及其設備明細？' : '刪除此設備明細？')) return
  const remaining = props.details.filter(row => !matchNode(row, nodeId.value) || (!base && row.equipmentNo !== equipmentNo.value))
  if (!props.save(base ? props.setups.filter(row => !matchNode(row, nodeId.value)) : props.setups, remaining)) {
    error.value = 'APS 假資料儲存失敗。'
    return
  }
  void nextTick(() => {
    loadDraft()
    notice.value = '已刪除選取的 APS 假資料。'
  })
}
</script>

<template>
  <section class="aps-window" role="dialog" aria-modal="true" aria-labelledby="aps-title">
    <header class="aps-bar">
      <h2 id="aps-title">APS Integration</h2><small v-if="developmentPreview">DEV 假資料</small>
      <button class="aps-close" type="button" aria-label="關閉 APS Integration" @click="emit('close')">×</button>
    </header>
    <div class="aps-body">
      <p>{{ productNo }} / {{ productVersion }}　Process {{ processNo }} / {{ processVersion }}　MOType {{ moTypeNo }}</p>
      <p v-if="!developmentPreview">正式 APS Integration API 尚未接線。</p>
      <template v-else>
        <div class="aps-actions">
          <label>OP / Node
            <select :value="nodeId" @change="selectNode(($event.target as HTMLSelectElement).value)">
              <option value="">請選擇流程節點</option>
              <option v-for="row in nodeOptions" :key="row.nodeId" :value="row.nodeId">{{ row.opNo }} / {{ row.nodeId }}</option>
            </select>
          </label>
          <button type="button" :disabled="!currentSetup || tab === 'detail'" @click="openEquipmentTime">EQPTIME</button>
          <button v-if="tab === 'detail'" type="button" @click="tab = 'base'; equipmentNo = 'N/A'">返回 APS 設定</button>
        </div>
        <div class="aps-table"><table>
          <thead><tr><th scope="col">選取</th><th scope="col">OPNo</th><th scope="col">NodeID</th><th scope="col">EquipmentNo</th><th scope="col">EquipmentGroup</th>
            <th scope="col">Schedule</th><th scope="col">STD Work Time Qty</th><th scope="col">Description</th></tr></thead>
          <tbody>
            <tr v-for="row in (tab === 'base' ? visibleSetups : visibleDetails)" :key="String(row.nodeId) + '/' + String(row.equipmentNo)">
              <td><button type="button" @click="tab === 'base' ? selectNode(String(row.nodeId)) : equipmentNo = String(row.equipmentNo)">選取</button></td>
              <td>{{ row.opNo }}</td><td>{{ row.nodeId }}</td><td>{{ row.equipmentNo }}</td><td>{{ row.equipmentGroup }}</td>
              <td>{{ row.schedule }}</td><td>{{ row.stdWorkTimeQty }}</td><td>{{ row.description }}</td>
            </tr>
            <tr v-if="!(tab === 'base' ? visibleSetups : visibleDetails).length"><td colspan="8">目前無 APS 設定資料。</td></tr>
          </tbody>
        </table></div>
        <form v-if="activeNode" class="aps-editor" @submit.prevent="update">
          <div class="aps-fields">
            <label>Equipment No
              <select v-model="equipmentNo" @change="tab === 'base' && (notice = '')">
                <option value="N/A">{{ tab === 'base' ? 'N/A' : '請選擇設備' }}</option><option v-for="option in equipmentOptions" :key="option">{{ option }}</option>
              </select>
            </label>
            <label>Equipment Group <input v-model="group" maxlength="50" :readonly="tab === 'detail'"></label>
            <label v-if="tab === 'base'">Subcontractor <select v-model="subcontractor"><option>N/A</option><option>DEV-SUBCONTRACTOR-01</option></select></label>
            <label v-for="field in numberFields" :key="field.name">{{ field.label }} <input v-model="numeric[field.name]" inputmode="numeric"></label>
            <label><input v-model="schedule" type="checkbox"> Schedule</label>
            <label><input v-model="batch" type="checkbox"> Batch OP</label>
            <label>Description <input v-model="description" maxlength="255"></label>
          </div>
          <div class="aps-actions">
            <button type="submit" :disabled="!canUpdate">Update</button>
            <button type="button" :disabled="!(tab === 'base' ? currentSetup : currentDetail)" @click="remove">Delete</button>
            <button type="button" @click="tab === 'base' ? group = 'N/A' : equipmentNo = 'N/A'">Clear</button>
          </div>
        </form>
      </template>
      <p v-if="error" class="aps-error" role="alert">{{ error }}</p><p v-else-if="notice" role="status">{{ notice }}</p>
    </div>
    <footer class="aps-bar"><button class="aps-close" type="button" @click="emit('close')">Close</button></footer>
  </section>
</template>

<style scoped>
.aps-window { width: min(1040px, 96vw); max-height: 92vh; overflow: auto; background: #f7f5ee; color: var(--ink, #26251f); border: 1px solid #a9a69d; box-shadow: 0 8px 30px #0004; }
.aps-bar, .aps-actions { display: flex; flex-wrap: wrap; align-items: center; gap: 7px 12px; }
.aps-bar, .aps-body { padding: 8px 12px; }
.aps-bar { border-block: 1px solid var(--line, #e5e1d5); }
.aps-bar h2 { margin: 0; font-size: 15px; }
.aps-close { margin-left: auto; }
.aps-window button { min-height: 26px; border: 1px solid #b7ae90; background: #fff; color: inherit; padding: 3px 8px; font: inherit; cursor: pointer; }
.aps-window button:disabled { opacity: .55; cursor: default; }
.aps-window input:not([type="checkbox"]), .aps-window select { min-width: 0; border: 1px solid #b7ae90; padding: 4px; background: #fff2bd; color: inherit; }
.aps-table { overflow: auto; max-height: 36vh; margin: 8px 0; background: white; }
.aps-table table { width: 100%; border-collapse: collapse; white-space: nowrap; font-size: 12px; }
.aps-table th, .aps-table td { padding: 5px; border: 1px solid var(--line, #e5e1d5); text-align: left; }
.aps-editor { padding: 8px; border: 1px solid var(--line, #e5e1d5); }
.aps-fields { display: grid; grid-template-columns: repeat(auto-fit, minmax(155px, 1fr)); gap: 7px 10px; margin-bottom: 8px; }
.aps-fields label { display: grid; gap: 4px; }
.aps-fields label:has(input[type="checkbox"]) { display: flex; align-items: center; }
.aps-error { color: #941b18; }
</style>
