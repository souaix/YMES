<script setup lang="ts">
import { computed, ref } from 'vue'

type ModuleProcess = { psNo: string; processNo: string; processVersion: string; defaultProcess: number }
const props = defineProps<{ psNo: string; rows: ModuleProcess[]; canEdit: boolean; save: (rows: ModuleProcess[]) => boolean }>()
const emit = defineEmits<{ close: [] }>()
const options = [
  { psNo: 'DEV-PS-10', processNo: 'DEV-PROC-01', version: '01' }, { psNo: 'DEV-PS-10', processNo: 'DEV-PROC-02', version: '03' },
  { psNo: 'DEV-PS-20', processNo: 'DEV-PROC-20', version: '02' }, { psNo: 'DEV-PS-30', processNo: 'DEV-PROC-30', version: '01' },
]
const available = computed(() => options.filter(row => row.psNo === props.psNo))
const rows = ref(props.rows.map(row => ({ ...row })))
const processNo = ref('')
const currentVersion = ref(true)
const defaultProcess = ref(1)
const error = ref('')

function add(): void {
  const process = available.value.find(row => row.processNo === processNo.value)
  if (!props.canEdit || !process) return
  if (rows.value.some(row => row.processNo === process.processNo)) { error.value = '流程編號已存在。'; return }
  if (defaultProcess.value === 1 && rows.value.some(row => row.defaultProcess === 1)) { error.value = '同一製程模組僅能使用一個預設流程。'; return }
  rows.value.push({ psNo: props.psNo, processNo: process.processNo, processVersion: currentVersion.value ? '*' : process.version, defaultProcess: defaultProcess.value })
  error.value = ''
}

function save(close: boolean): void {
  if (!props.canEdit || !props.save(rows.value)) return
  if (close) emit('close')
}
</script>

<template>
  <section class="process-dialog" role="dialog" aria-modal="true" aria-labelledby="module-process-title">
    <header><h2 id="module-process-title">製程模組流程設定</h2><button type="button" @click="emit('close')">關閉</button></header>
    <p>生產區段 {{ psNo }}</p>
    <form v-if="canEdit" @submit.prevent="add">
      <label>流程 <select v-model="processNo" required><option value="">請選擇</option><option v-for="row in available" :key="row.processNo" :value="row.processNo">{{ row.processNo }} / {{ row.version }}</option></select></label>
      <label><input v-model="currentVersion" type="radio" :value="true" />目前版</label><label><input v-model="currentVersion" type="radio" :value="false" />指定版</label>
      <label>預設 <select v-model.number="defaultProcess"><option :value="1">Yes</option><option :value="0">No</option></select></label><button>ADD</button>
    </form>
    <table><thead><tr><th>流程編號</th><th>版本</th><th>預設流程</th><th v-if="canEdit">操作</th></tr></thead>
      <tbody><tr v-for="(row, index) in rows" :key="row.processNo"><td>{{ row.processNo }}</td><td>{{ row.processVersion }}</td><td>{{ row.defaultProcess === 1 ? 'Yes' : 'No' }}</td><td v-if="canEdit"><button @click="rows.splice(index, 1); error = ''">DEL</button></td></tr>
      <tr v-if="!rows.length"><td :colspan="canEdit ? 4 : 3">沒有設定流程。</td></tr></tbody></table>
    <p v-if="error" role="alert">{{ error }}</p>
    <footer><button v-if="canEdit" @click="save(false)">Apply</button><button v-if="canEdit" @click="save(true)">Confirm</button><button @click="emit('close')">Close</button></footer>
  </section>
</template>

<style scoped>
.process-dialog{width:min(760px,95vw);max-height:90vh;overflow:auto;display:grid;gap:12px;padding:16px;background:white;border:1px solid #b8b3a2}header,form,footer{display:flex;flex-wrap:wrap;align-items:center;gap:10px}header{justify-content:space-between}footer{justify-content:flex-end}h2,p{margin:0}table{width:100%;border-collapse:collapse;text-align:left}th,td{padding:5px;border:1px solid #dedbd1}th{background:#f4f2eb}button{padding:4px 10px}
</style>
