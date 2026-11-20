<script setup lang="ts">
import { computed, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'

const props = defineProps<{ developmentPreview: boolean }>()
defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview)
const jobs = dev.value ? [
  { group: 'DEV-G001', lot: 'DEV-L001', mo: 'DEV-MO01', op: 'WB', serial: 'DEV-L001-02' },
  { group: 'DEV-G002', lot: 'DEV-L002', mo: 'DEV-MO01', op: 'PK', serial: 'DEV-L002-03' },
] : []
const options = [
  { id: 'DEV-AU', name: '金線', type: 'RAW', unit: 'M', level: 0, needsBatch: true },
  { id: 'DEV-TAPE', name: '膠帶', type: 'SUPPLY', unit: 'PCS', level: 0, needsBatch: false },
  { id: 'DEV-IC', name: '半成品', type: 'WIP', unit: 'PCS', level: 1, needsBatch: true },
]
interface Row { id: number; group: string; material: string; batch: string; qty: number; description: string }
const rows = ref<Row[]>(dev.value ? [
  { id: 1, group: 'DEV-G001', material: 'DEV-AU', batch: 'DEV-AU01', qty: 3, description: '' },
  { id: 2, group: 'DEV-G001', material: 'DEV-AU', batch: 'DEV-AU02', qty: 2, description: '' },
  { id: 3, group: 'DEV-G002', material: 'DEV-TAPE', batch: '', qty: 1, description: '' },
] : [])
let nextId = 4
const mo = ref('')
const lot = ref('')
const visible = ref<typeof jobs>([])
const queried = ref(false)
const report = ref('')
const detail = ref('')
const selected = ref<number[]>([])
const active = computed(() => visible.value.find(job => job.group === detail.value))
const detailRows = computed(() => rows.value.filter(row => row.group === active.value?.group))
const totals = computed(() => {
  const result = new Map<string, number>()
  for (const row of detailRows.value) result.set(row.material, (result.get(row.material) ?? 0) + row.qty)
  return [...result]
})
const draft = ref<{ id: number | null; level: number; material: string; batch: string; qty: number | string; description: string }>()
const chosen = computed(() => options.find(option => option.id === draft.value?.material))
const confirmation = ref<string | null>(null)
const pending = ref<number[]>([])
const error = ref('')
const result = ref('')

function query(): void {
  visible.value = jobs.filter(job => (!mo.value.trim() || job.mo === mo.value.trim().toUpperCase()) && (!lot.value.trim() || job.lot === lot.value.trim().toUpperCase()))
  queried.value = true
  report.value = detail.value = ''
  selected.value = []
  draft.value = undefined
  error.value = result.value = ''
}
function showDetail(): void {
  detail.value = report.value
  selected.value = []
  error.value = result.value = ''
}
function define(edit: boolean): void {
  const row = edit ? detailRows.value.find(item => item.id === selected.value[0]) : undefined
  if (edit && !row) return
  draft.value = row ? { ...row, level: options.find(option => option.id === row.material)?.level ?? 0 } : { id: null, level: 0, material: '', batch: '', qty: '', description: '' }
  error.value = result.value = ''
}
function save(apply: boolean): void {
  const data = draft.value
  const job = active.value
  if (!data || !job) return
  const qty = Number(data.qty)
  const batch = data.batch.trim().toUpperCase()
  if (!chosen.value || (chosen.value.needsBatch && (!batch || batch === 'N/A')) || !Number.isFinite(qty) || qty <= 0) {
    error.value = '請選擇用料、填寫必填批號，並輸入大於零的數量。'
    return
  }
  if (data.id === null && !batch && detailRows.value.some(row => row.material === data.material)) {
    error.value = '該料號已有記錄，請編輯既有用量。'
    return
  }
  const row = { id: data.id ?? nextId++, group: job.group, material: data.material, batch, qty, description: data.description.trim() }
  rows.value = data.id === null ? [...rows.value, row] : rows.value.map(item => item.id === data.id ? row : item)
  error.value = ''
  result.value = 'DEV 已保存用料。'
  selected.value = []
  if (apply && data.id === null) {
    draft.value = { id: null, level: data.level, material: '', batch: '', qty: '', description: '' }
  } else draft.value = undefined
}
function requestDelete(): void {
  pending.value = detailRows.value.filter(row => selected.value.includes(row.id)).map(row => row.id)
  if (pending.value.length) confirmation.value = `刪除選取的 ${pending.value.length} 筆用料？`
}
function resolve(accepted: boolean): void {
  confirmation.value = null
  if (accepted) {
    rows.value = rows.value.filter(row => !pending.value.includes(row.id))
    selected.value = []
    error.value = ''
    result.value = `DEV 已刪除 ${pending.value.length} 筆用料。`
  }
  pending.value = []
}
</script>

<template>
  <section class="material-page" aria-label="生產批作業站用料記錄調整">
    <template v-if="dev">
      <form v-if="!draft" class="toolbar" @submit.prevent="query">
        <label>工單 <input v-model="mo" list="adjust-mos" maxlength="50"></label>
        <datalist id="adjust-mos"><option>DEV-MO01</option></datalist>
        <label>批號 <input v-model="lot" list="adjust-lots" maxlength="50"></label>
        <datalist id="adjust-lots"><option v-for="job in jobs" :key="job.lot">{{ job.lot }}</option></datalist>
        <button type="submit" class="query-button">查詢</button>
        <button type="button" :disabled="!report" @click="showDetail">明細</button>
        <button type="button" @click="$emit('close')">關閉</button>
      </form>
      <div v-if="!draft" class="grid">
        <table aria-label="報工歷程">
          <thead><tr><th>選取</th><th>報工序號</th><th>工單</th><th>批號</th><th>站別</th><th>批序號</th></tr></thead>
          <tbody>
            <tr v-for="job in visible" :key="job.group" :class="{ selected: report === job.group }">
              <td><input v-model="report" type="radio" name="material-report" :value="job.group" :aria-label="'選取 ' + job.group"></td>
              <td>{{ job.group }}</td><td>{{ job.mo }}</td><td>{{ job.lot }}</td><td>{{ job.op }}</td><td>{{ job.serial }}</td>
            </tr>
            <tr v-if="!visible.length"><td colspan="6">{{ queried ? '查無資料。' : '請先查詢。' }}</td></tr>
          </tbody>
        </table>
      </div>
      <template v-if="active && !draft">
        <div class="toolbar"><strong>{{ active.group }} 用料明細</strong>
          <button type="button" @click="define(false)">新增</button><button type="button" :disabled="selected.length !== 1" @click="define(true)">修改</button>
          <button type="button" :disabled="!selected.length" @click="requestDelete">刪除</button>
        </div>
        <div class="grid">
          <table aria-label="用料明細">
            <thead><tr><th>選取</th><th>料號</th><th>材料批號</th><th>用量</th><th>說明</th></tr></thead>
            <tbody>
              <tr v-for="row in detailRows" :key="row.id" :class="{ selected: selected.includes(row.id) }">
                <td><input v-model="selected" type="checkbox" :value="row.id" :aria-label="'選取用料 ' + row.id"></td>
                <td>{{ row.material }}</td><td>{{ row.batch || '—' }}</td><td>{{ row.qty }}</td><td>{{ row.description }}</td>
              </tr>
              <tr v-if="!detailRows.length"><td colspan="5">沒有用料明細。</td></tr>
            </tbody>
          </table>
        </div>
        <p v-if="totals.length">用量合計 <span v-for="[material, sum] in totals" :key="material" class="total">{{ material }} {{ Number(sum.toFixed(8)) }}</span></p>
      </template>
      <form v-if="draft" class="editor" @submit.prevent="save(false)">
        <h2>{{ draft.id === null ? '新增用料' : '修改用料' }} · {{ active?.group }}</h2>
        <div class="toolbar">
          <label>用料類別 <select v-model.number="draft.level" :disabled="draft.id !== null" @change="draft.material = ''; draft.batch = ''"><option :value="0">材料</option><option :value="1">產品</option></select></label>
          <label>料號 <select v-model="draft.material" :disabled="draft.id !== null" @change="draft.batch = ''"><option value="">請選擇</option><option v-for="item in options.filter(option => option.level === draft?.level)" :key="item.id" :value="item.id">{{ item.id }} {{ item.name }}</option></select></label>
          <label>料號類型 <input :value="chosen?.type || ''" readonly></label>
          <label>材料／產品批號 <input v-model="draft.batch" :readonly="draft.id !== null" maxlength="50"></label>
          <label>用量 <input v-model.number="draft.qty" type="number" min="0" step="0.00000001"></label>
          <label>單位 <input :value="chosen?.unit || ''" readonly></label>
          <label>說明 <input v-model="draft.description" maxlength="255"></label>
        </div>
        <div class="toolbar"><button v-if="draft.id === null" type="button" @click="save(true)">套用</button><button type="submit" class="primary">確定</button><button type="button" @click="draft = undefined; error = ''">取消</button></div>
      </form>
      <p v-if="error" role="alert">{{ error }}</p><p v-if="result" role="status">{{ result }}</p>
      <ActionConfirmationDialog :message="confirmation" @resolve="resolve" />
    </template>
    <template v-else>
      <p role="status">正式環境尚未開放此功能。</p>
      <button type="button" @click="$emit('close')">關閉</button>
    </template>
  </section>
</template>

<style scoped>
.material-page, .editor { display: grid; align-content: start; gap: 12px; padding: 16px; color: var(--ink); }
.editor { padding: 14px; border: 1px solid var(--line); background: white; }
.toolbar, label { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.grid { overflow: auto; border: 1px solid var(--line); background: white; }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
td, th { padding: 6px 9px; text-align: left; white-space: nowrap; border-bottom: 1px solid var(--line); }
p, h2 { margin: 0; }
.total { margin-inline-start: 12px; }
button { width: max-content; min-height: 30px; }
[role='alert'] { color: #9d2727; }
</style>
