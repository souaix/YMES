<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'

const props = defineProps<{ developmentPreview: boolean }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const unavailableMessage = "正式環境尚未開放此功能。"
defineEmits<{ close: [] }>()

const lots = ref(dev.value ? ([
  { no: 'DEV-BD001', source: 'DEV-ORG01', mo: 'DEV-MO01', product: 'DEV-IC1', op: 'WB', status: '排隊', qty: 100 },
  { no: 'DEV-BD002', source: 'DEV-ORG01', mo: 'DEV-MO01', product: 'DEV-IC1', op: 'FT', status: '完成', qty: 80, blocked: '有分批來源' },
  { no: 'DEV-BD003', source: 'DEV-ORG02', mo: 'DEV-MO02', product: 'DEV-IC2', op: 'PK', status: '執行中', qty: 75, packaged: true },
  { no: 'DEV-BD004', source: 'DEV-ORG03', mo: 'DEV-MO02', product: 'DEV-IC2', op: 'FT', status: '完成', qty: 60, blocked: '回復歷程已提交' },
]) : ([]))
const filterFields = [
  { key: 'source', label: '來源批號' },
  { key: 'mo', label: '工單' },
  { key: 'product', label: '產品' },
] as const
const enabled = reactive({ source: false, mo: false, product: false })
const inputs = reactive({ source: '', mo: '', product: '' })
const criteria = ref({ ...inputs })
const queried = ref(false)
const selected = ref<string[]>([])
const pending = ref<string[]>([])
const confirmation = ref<string | null>(null)
const error = ref('')
const result = ref('')
const failures = ref<{ no: string; reason: string }[]>([])
const visible = computed(() => dev.value ? (queried.value ? lots.value.filter(lot =>
  filterFields.every(({ key }) => !criteria.value[key] || lot[key] === criteria.value[key]),
) : []) : ([]))

function search(): void {
  if (!dev.value) return

  for (const { key } of filterFields) {
    criteria.value[key] = enabled[key] ? inputs[key].trim().toUpperCase() : ''
  }
  queried.value = true
  selected.value = []
  error.value = ''
  result.value = ''
  failures.value = []
}

function requestDelete(): void {
  if (!dev.value) return

  pending.value = visible.value.filter(row => selected.value.includes(row.no)).map(row => row.no)
  if (pending.value.length) confirmation.value = `確定刪除選取的 ${pending.value.length} 個生產批？`
}

function confirmDelete(accepted: boolean): void {
  if (!dev.value) return

  confirmation.value = null
  const planned = pending.value
  pending.value = []
  if (!accepted) return
  const targets = lots.value.filter(row => planned.includes(row.no))
  const packaged = targets.find(row => row.packaged)
  failures.value = []
  result.value = ''
  error.value = packaged ? `生產批 ${packaged.no} 已包裝，本批次未執行刪除。` : ''
  if (packaged) return
  const deleted = new Set(targets.filter(row => !row.blocked).map(row => row.no))
  failures.value = targets.flatMap(row => row.blocked ? [{ no: row.no, reason: row.blocked }] : [])
  lots.value = lots.value.filter(row => !deleted.has(row.no))
  selected.value = []
  result.value = `DEV 刪除 ${deleted.size} 批，失敗 ${failures.value.length} 批。`
}
</script>

<template>
  <section class="batch-delete" aria-labelledby="batch-delete-title">
    <h2 id="batch-delete-title">批次刪除已下線生產批</h2>
    <template v-if="dev">
      <form class="toolbar" @submit.prevent="search">
        <div v-for="field in filterFields" :key="field.key" class="filter">
          <label><input v-model="enabled[field.key]" type="checkbox" @change="!enabled[field.key] && (inputs[field.key] = '')"> {{ field.label }}</label>
          <input v-model="inputs[field.key]" :aria-label="field.label" :list="'batch-delete-' + field.key" maxlength="50" :disabled="!enabled[field.key]">
          <datalist :id="'batch-delete-' + field.key">
            <option v-for="value in new Set(lots.map(row => row[field.key]))" :key="value" :value="value" />
          </datalist>
        </div>
        <button type="submit">查詢</button>
      </form>
      <div class="toolbar">
        <button type="button" :disabled="!visible.length" @click="selected = visible.map(row => row.no)">全選</button>
        <button type="button" :disabled="!selected.length" @click="selected = []">取消全選</button>
        <button type="button" :disabled="!selected.length" @click="requestDelete">刪除</button>
        <button type="button" @click="$emit('close')">關閉</button>
      </div>
      <div class="grid">
        <table aria-label="已下線生產批">
          <thead><tr><th scope="col">選取</th><th scope="col">生產批號</th><th scope="col">來源批號</th><th scope="col">工單</th><th scope="col">產品</th><th scope="col">作業站</th><th scope="col">狀態</th><th scope="col">現有數量</th></tr></thead>
          <tbody>
            <tr v-for="row in visible" :key="row.no" :class="{ selected: selected.includes(row.no) }">
              <td><input v-model="selected" type="checkbox" :value="row.no" :aria-label="'選取 ' + row.no"></td>
              <td>{{ row.no }}</td><td>{{ row.source }}</td><td>{{ row.mo }}</td><td>{{ row.product }}</td><td>{{ row.op }}</td><td>{{ row.status }}</td><td>{{ row.qty }}</td>
            </tr>
            <tr v-if="!visible.length"><td colspan="8">{{ queried ? '查無資料。' : '請先查詢已下線生產批。' }}</td></tr>
          </tbody>
        </table>
      </div>
      <p v-if="error" role="alert">{{ error }}</p>
      <p v-if="result" role="status">{{ result }}</p>
      <ul v-if="failures.length">
        <li v-for="failure in failures" :key="failure.no">{{ failure.no }}（{{ failure.reason }}）</li>
      </ul>
      <ActionConfirmationDialog :message="confirmation" @resolve="confirmDelete" />
    </template>
    <template v-else>
      <p role="status">{{ unavailableMessage }}</p>
      <button type="button" @click="$emit('close')">關閉</button>
    </template>
  </section>
</template>

<style scoped>
.batch-delete { display: grid; align-content: start; gap: 12px; padding: 16px; color: var(--ink); }
h2, p, ul { margin: 0; }
.toolbar, .filter { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.grid { overflow: auto; border: 1px solid var(--line); background: white; }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
td, th { padding: 6px 9px; text-align: left; white-space: nowrap; border-bottom: 1px solid var(--line); }
th { background: #f7f5ee; }
.selected td { background: #fff6c5; }
button { width: max-content; min-height: 30px; }
[role="alert"] { color: #9d2727; }
</style>
