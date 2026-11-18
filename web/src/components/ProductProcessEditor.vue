<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type ProcessRow = Record<string, any>

const props = defineProps<{
  product: ProcessRow
  processes: ProcessRow[]
  developmentPreview: boolean
}>()
const emit = defineEmits<{
  close: []
  save: [processes: ProcessRow[]]
}>()

const segments = [
  { moTypeNo: '*', moTypeName: '全部工單類型', psNo: 'DEV-PS-10', psOrder: 10 },
  { moTypeNo: '1', moTypeName: '標準生產', psNo: 'DEV-PS-10', psOrder: 10 },
  { moTypeNo: '1', moTypeName: '標準生產', psNo: 'DEV-PS-20', psOrder: 20 },
  { moTypeNo: '2', moTypeName: '試產工單', psNo: 'DEV-PS-30', psOrder: 30 },
]
const processOptions: Record<string, Array<{ processNo: string; processVersion: string }>> = {
  'DEV-PS-10': [{ processNo: 'DEV-PROC-01', processVersion: '01' }, { processNo: 'DEV-PROC-02', processVersion: '03' }],
  'DEV-PS-20': [{ processNo: 'DEV-PROC-20', processVersion: '02' }],
  'DEV-PS-30': [{ processNo: 'DEV-PROC-30', processVersion: '01' }],
}
const working = ref<ProcessRow[]>([])
const selectedIndex = ref(-1)
const dirty = ref(false)
const error = ref('')
const draft = ref<ProcessRow>(blankDraft())

function blankDraft(): ProcessRow {
  return { moTypeNo: '', moTypeName: '', psNo: '', psOrder: 0, processNo: '', processVersion: '', versionMode: 'current', defaultProcess: 1 }
}
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
const availableProcesses = computed(() => processOptions[draft.value.psNo] ?? [])
const moTypeOptions = computed(() => [...new Map(segments.map(row => [row.moTypeNo, row] as const)).values()])
const segmentOptions = computed(() => [...new Map(segments.map(row => [row.psNo, row] as const)).values()])
const readOnly = computed(() => !props.developmentPreview)

watch(() => props.processes, value => {
  working.value = clone(value ?? [])
  selectedIndex.value = -1
  dirty.value = false
}, { immediate: true, deep: true })

function chooseMoType(value: string): void {
  const segment = segments.find(item => item.moTypeNo === value)
  draft.value.moTypeNo = value
  draft.value.moTypeName = segment?.moTypeName ?? ''
}
function chooseSegment(value: string): void {
  const segment = segments.find(item => item.psNo === value)
  draft.value.psNo = value
  draft.value.psOrder = segment?.psOrder ?? 0
  draft.value.processNo = ''
  draft.value.processVersion = ''
}
function chooseProcess(value: string): void {
  const process = availableProcesses.value.find(item => item.processNo === value)
  draft.value.processNo = value
  draft.value.processVersion = process?.processVersion ?? ''
}
function addProcess(): void {
  error.value = ''
  if (readOnly.value) return
  if (!draft.value.moTypeNo) { error.value = '請選擇 MOType No。'; return }
  if (!draft.value.psNo) { error.value = '請選擇 PS. No。'; return }
  if (!draft.value.processNo) { error.value = '請選擇 Process No。'; return }
  if (!draft.value.versionMode) { error.value = '請選擇 ProcessVersion。'; return }
  const moTypeNo = draft.value.moTypeNo === '*' ? -1 : Number(draft.value.moTypeNo)
  const duplicate = working.value.some(item => String(item.moTypeNo) === String(moTypeNo) && item.psNo === draft.value.psNo && item.processNo === draft.value.processNo)
  if (duplicate) { error.value = `MOType ${draft.value.moTypeNo} / ${draft.value.psNo} / ${draft.value.processNo} 已存在。`; return }
  if (Number(draft.value.defaultProcess) === 1 && working.value.some(item => String(item.moTypeNo) === String(moTypeNo) && item.psNo === draft.value.psNo && Number(item.defaultProcess) === 1)) {
    error.value = `MOType ${draft.value.moTypeNo} / ${draft.value.psNo} 已有 Default Process。`
    return
  }
  const added = {
    moTypeNo,
    psNo: draft.value.psNo,
    psOrder: Number(draft.value.psOrder || 0),
    processNo: draft.value.processNo,
    processVersion: draft.value.versionMode === 'current' ? '*' : draft.value.processVersion,
    defaultProcess: Number(draft.value.defaultProcess),
    haveComponent: 0,
    haveLevel: 0,
    equipmentGroup: '',
  }
  working.value = [...working.value, added].sort((a, b) => Number(a.psOrder) - Number(b.psOrder) || Number(b.defaultProcess) - Number(a.defaultProcess))
  selectedIndex.value = working.value.findIndex(item => String(item.moTypeNo) === String(added.moTypeNo) && item.psNo === added.psNo && item.processNo === added.processNo)
  dirty.value = true
  draft.value = blankDraft()
}
function deleteProcess(): void {
  if (readOnly.value || selectedIndex.value < 0) return
  working.value.splice(selectedIndex.value, 1)
  selectedIndex.value = -1
  dirty.value = true
}
function save(closeAfter: boolean): void {
  error.value = ''
  if (readOnly.value || !dirty.value) {
    if (closeAfter) emit('close')
    return
  }
  if (closeAfter) {
    const netFlowSegments = new Set(['DEV-PS-30'])
    const usedNetFlow = working.value.filter(row => netFlowSegments.has(String(row.psNo)))
    if (usedNetFlow.length && working.value.some(row => !netFlowSegments.has(String(row.psNo)))) {
      error.value = 'Net flow can not have multiple production segment。'
      return
    }
  }
  emit('save', clone(working.value))
  dirty.value = false
  if (closeAfter) emit('close')
}
</script>

<template>
  <section class="process-window" role="dialog" aria-modal="true" aria-label="Product process setting">
    <header class="window-title"><span>Product process setting</span><button type="button" aria-label="Close" @click="emit('close')">×</button></header>
    <div class="product-identity">
      <label><span>ProductNo</span><input readonly :value="product.productNo" /></label>
      <label v-if="product.productVersion"><span>Product Version</span><input readonly :value="product.productVersion" /></label>
    </div>
    <fieldset class="process-entry"><legend>Process Define</legend>
      <label class="process-field"><span>MOType No</span><select :value="draft.moTypeNo" :disabled="readOnly" @change="chooseMoType(($event.target as HTMLSelectElement).value)"><option value="">請選擇</option><option v-for="item in moTypeOptions" :key="item.moTypeNo" :value="item.moTypeNo">{{ item.moTypeNo }}　{{ item.moTypeName }}</option></select></label>
      <label class="process-field"><span>PS. No</span><select :value="draft.psNo" :disabled="readOnly" @change="chooseSegment(($event.target as HTMLSelectElement).value)"><option value="">請選擇</option><option v-for="item in segmentOptions" :key="item.psNo" :value="item.psNo">{{ item.psNo }}</option></select></label>
      <label class="process-field ps-order"><span>PS. Order</span><input readonly :value="draft.psOrder" /></label>
      <label class="process-field"><span>Process No</span><select :value="draft.processNo" :disabled="readOnly || !draft.psNo" @change="chooseProcess(($event.target as HTMLSelectElement).value)"><option value="">請選擇</option><option v-for="item in availableProcesses" :key="item.processNo" :value="item.processNo">{{ item.processNo }}</option></select></label>
      <div class="process-field process-version"><span>ProcessVersion</span><div><label><input v-model="draft.versionMode" :disabled="readOnly" type="radio" value="current" />Current</label><label><input v-model="draft.versionMode" :disabled="readOnly" type="radio" value="specified" />Specified</label></div></div>
      <label class="process-field"><span>Version</span><input readonly :value="draft.versionMode === 'current' ? 'Current (*)' : draft.processVersion" /></label>
      <div class="process-field default-process"><span>Default Process</span><label><input v-model.number="draft.defaultProcess" :disabled="readOnly" type="radio" :value="1" />Yes</label><label><input v-model.number="draft.defaultProcess" :disabled="readOnly" type="radio" :value="0" />No</label></div>
      <button class="prd-button add-process" type="button" :disabled="readOnly" @click="addProcess">ADD</button>
    </fieldset>
    <fieldset class="process-grid-group"><legend>Defined Product Process</legend>
      <div class="process-grid-wrap"><table><thead><tr><th>MOTypeNo</th><th>PSNo</th><th>PSOrder</th><th>ProcessNo</th><th>ProcessVersion</th><th>DefaultProcess</th><th>HaveComponent</th><th>HaveLevel</th><th>EquipmentGroup</th></tr></thead>
        <tbody><tr v-for="(item,index) in working" :key="`${item.moTypeNo}|${item.psNo}|${item.processNo}`" :class="{ selected: selectedIndex === index }" @click="selectedIndex=index"><td>{{ Number(item.moTypeNo) === -1 ? '*' : item.moTypeNo }}</td><td>{{ item.psNo }}</td><td>{{ item.psOrder }}</td><td>{{ item.processNo }}</td><td>{{ item.processVersion }}</td><td>{{ Number(item.defaultProcess) ? 'Yes' : 'No' }}</td><td>{{ item.haveComponent ? 'Yes' : 'No' }}</td><td>{{ item.haveLevel ? 'Yes' : 'No' }}</td><td>{{ item.equipmentGroup }}</td></tr><tr v-if="!working.length"><td class="empty" colspan="9">尚未定義產品流程。</td></tr></tbody>
      </table></div>
      <button class="prd-button delete-process" type="button" :disabled="readOnly || selectedIndex < 0" @click="deleteProcess">DEL</button>
    </fieldset>
    <p v-if="error" class="process-error">{{ error }}</p>
    <footer class="process-footer"><button class="prd-button" type="button" @click="emit('close')">Close</button><button class="prd-button" type="button" :disabled="readOnly" @click="save(true)">Confirm</button><button class="prd-button" type="button" :disabled="readOnly || !dirty" @click="save(false)">Apply</button></footer>
  </section>
</template>

<style scoped>
.prd-button{min-height:28px;padding:4px 8px;border:1px solid #aaa79c;border-radius:3px;background:linear-gradient(#fff,#f3f1eb);color:#25241e;font:inherit;white-space:nowrap;cursor:pointer}.prd-button:hover:not(:disabled){border-color:#c49a35;background:#fff5d6}.prd-button:disabled{opacity:.5;cursor:not-allowed}
.process-entry{grid-template-rows:repeat(5,24px);gap:2px 7px}.process-field:nth-of-type(1){grid-column:1;grid-row:1}.process-field:nth-of-type(2){grid-column:1;grid-row:2}.process-field:nth-of-type(4){grid-column:1;grid-row:3}.process-field:nth-of-type(5){grid-column:2;grid-row:3}.process-version{grid-column:1/3;grid-row:4}.process-version>div{display:flex;gap:11px}.default-process{grid-column:1/3;grid-row:5}
.process-window{width:min(544px,calc(100vw - 24px));height:min(462px,calc(100vh - 24px));display:flex;flex-direction:column;gap:5px;padding:5px 8px;background:#fdfcf9;border:1px solid #99968d;box-shadow:0 8px 30px #0005;color:#28271f;font:12px "Segoe UI","Microsoft JhengHei",sans-serif;overflow:hidden}.window-title{height:23px;display:flex;justify-content:space-between;align-items:center;padding:0 3px;font-size:13px}.window-title button{border:0;background:transparent;font-size:19px;line-height:18px;cursor:pointer}.product-identity{display:flex;gap:20px;padding:2px 8px}.product-identity label{display:flex;align-items:center;gap:8px}.product-identity label span{width:94px;text-align:right}.product-identity input{width:150px;height:22px;border:1px solid #aaa797;background:#efeee9;padding:2px 5px;font:inherit}.product-identity label+label input{width:52px}.process-entry,.process-grid-group{position:relative;margin:0 7px;padding:10px;border:1px solid #aaa79f;min-width:0}.process-entry{height:171px;display:grid;grid-template-columns:1fr 1fr 80px;grid-template-rows:repeat(3,32px);gap:5px 7px}.process-entry legend,.process-grid-group legend{padding:0 5px;color:#514d40}.process-field{display:flex;align-items:center;gap:5px;min-width:0}.process-field>span{width:78px;text-align:right;flex:none}.process-field select,.process-field>input{min-width:0;width:100%;height:24px;border:1px solid #99968d;background:#fff2bd;padding:2px 5px;color:#28271f;font:inherit}.ps-order input{width:48px}.process-version{grid-column:1/3;grid-row:3}.process-version>div{display:flex;gap:11px}.process-version>div label,.default-process label{display:flex;align-items:center;gap:3px;white-space:nowrap}.process-version input,.default-process input{accent-color:#b4831d}.default-process{grid-column:2;grid-row:3}.default-process>span{width:87px}.add-process{grid-column:3;grid-row:1/3;align-self:center;justify-self:end;width:75px;height:32px}.process-grid-group{flex:1;min-height:100px;padding:8px}.process-grid-wrap{position:absolute;top:8px;left:8px;right:8px;bottom:41px;overflow:auto}.process-grid-wrap table{border-collapse:collapse;white-space:nowrap;font:11px "Segoe UI","Microsoft JhengHei",sans-serif}.process-grid-wrap th{position:sticky;top:0;padding:4px 6px;background:#d8d5cd;border:1px solid #c2beb4;text-align:left}.process-grid-wrap td{padding:4px 6px;border:1px solid #d4d1c8;cursor:pointer}.process-grid-wrap tbody tr:nth-child(even) td{background:#faf9f5}.process-grid-wrap tbody tr.selected td{background:#ffeaa1}.process-grid-wrap td.empty{text-align:center;color:#777;cursor:default}.delete-process{position:absolute;right:8px;bottom:5px;width:80px;height:30px}.process-footer{display:flex;justify-content:flex-end;gap:7px;padding:0 8px}.process-footer .prd-button{width:80px;height:32px}.process-error{margin:0 14px;padding:4px 7px;background:#fff1ed;border:1px solid #d29a8e;color:#7c281d}.process-readonly{margin:0;padding:4px 8px;background:#fff8dc;border:1px solid #e4d7a3;color:#5f5427}
.process-entry{grid-template-rows:repeat(5,24px);gap:2px 7px}.process-field:nth-of-type(1){grid-column:1;grid-row:1}.process-field:nth-of-type(2){grid-column:1;grid-row:2}.process-field:nth-of-type(4){grid-column:1;grid-row:3}.process-field:nth-of-type(5){grid-column:2;grid-row:3}.process-version{grid-column:1/3;grid-row:4}.default-process{grid-column:1/3;grid-row:5}.ps-order{grid-column:2;grid-row:2}
</style>
